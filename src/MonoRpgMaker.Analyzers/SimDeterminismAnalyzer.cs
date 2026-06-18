using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace MonoRpgMaker.Analyzers;

/// <summary>
/// Bans determinism-hostile constructs in the simulation namespaces (D-0016): floating-point
/// (<c>float</c>/<c>double</c>/<c>MathF</c>/double-returning <c>Math</c>), float-backed XNA math
/// (the <c>Vector2</c> family), <c>foreach</c> over a <c>Dictionary</c>, <see cref="System.Random"/>,
/// and ambient clocks (<see cref="System.DateTime"/>/<see cref="System.DateTimeOffset"/>). The
/// host/renderer namespaces are deliberately out of scope (they use these legitimately). Members
/// marked <c>[MonoRpgMaker.Abstractions.DeterminismExempt]</c> are skipped — the sanctioned boundary.
/// Detection is by resolved <em>type</em>, so integer overloads such as <c>Math.Max(int, int)</c> and
/// the integer XNA <c>Point</c> are never flagged.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class SimDeterminismAnalyzer : DiagnosticAnalyzer
{
    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(
            DeterminismDiagnostics.FloatingPoint,
            DeterminismDiagnostics.XnaFloatMath,
            DeterminismDiagnostics.DictionaryIteration,
            DeterminismDiagnostics.NonDeterministicRandom,
            DeterminismDiagnostics.AmbientClock);

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(OnCompilationStart);
    }

    private static void OnCompilationStart(CompilationStartAnalysisContext context)
    {
        BannedSymbols banned = BannedSymbols.Resolve(context.Compilation);

        context.RegisterSymbolAction(c => AnalyzeDeclaration(c, banned),
            SymbolKind.Field, SymbolKind.Property, SymbolKind.Method);

        context.RegisterOperationAction(c => AnalyzeLocal(c, banned), OperationKind.VariableDeclarator);
        context.RegisterOperationAction(c => AnalyzeObjectCreation(c, banned), OperationKind.ObjectCreation);
        context.RegisterOperationAction(c => AnalyzeInvocation(c, banned), OperationKind.Invocation);
        context.RegisterOperationAction(c => AnalyzeMemberReference(c, banned),
            OperationKind.PropertyReference, OperationKind.FieldReference);
        context.RegisterOperationAction(c => AnalyzeArithmetic(c, banned),
            OperationKind.Binary, OperationKind.Unary);
        context.RegisterOperationAction(c => AnalyzeLoop(c, banned), OperationKind.Loop);
    }

    private static bool InScope(ISymbol? symbol, BannedSymbols banned)
        => SimScope.IsInSimNamespace(symbol) && !SimScope.IsExempt(symbol, banned.ExemptAttribute);

    // Declarations: a field/property/method-return/parameter whose TYPE is banned.
    private static void AnalyzeDeclaration(SymbolAnalysisContext context, BannedSymbols banned)
    {
        ISymbol symbol = context.Symbol;
        if (symbol.IsImplicitlyDeclared || !InScope(symbol, banned))
        {
            return;
        }

        switch (symbol)
        {
            case IFieldSymbol field:
                ReportType(context.ReportDiagnostic, banned, field.Type, SymbolLocation(field));
                break;
            case IPropertySymbol property:
                ReportType(context.ReportDiagnostic, banned, property.Type, SymbolLocation(property));
                break;
            case IMethodSymbol method when method.AssociatedSymbol is null:
                // Skip accessors/operators-as-accessors; check the return type and each parameter.
                ReportType(context.ReportDiagnostic, banned, method.ReturnType, SymbolLocation(method));
                foreach (IParameterSymbol parameter in method.Parameters)
                {
                    ReportType(context.ReportDiagnostic, banned, parameter.Type, SymbolLocation(parameter));
                }

                break;
            default:
                break;
        }
    }

    // Locals (incl. `var`): the resolved declared type is banned.
    private static void AnalyzeLocal(OperationAnalysisContext context, BannedSymbols banned)
    {
        if (!InScope(context.ContainingSymbol, banned))
        {
            return;
        }

        var declarator = (IVariableDeclaratorOperation)context.Operation;
        ReportType(context.ReportDiagnostic, banned, declarator.Symbol.Type, context.Operation.Syntax.GetLocation());
    }

    // `new Vector2()` / `new Random()` / `new DateTime(...)`.
    private static void AnalyzeObjectCreation(OperationAnalysisContext context, BannedSymbols banned)
    {
        if (!InScope(context.ContainingSymbol, banned))
        {
            return;
        }

        ReportType(context.ReportDiagnostic, banned, context.Operation.Type, context.Operation.Syntax.GetLocation());
    }

    // `MathF.Sin(x)` / `Math.Sqrt(x)` (double-returning) — caught even behind `var`.
    private static void AnalyzeInvocation(OperationAnalysisContext context, BannedSymbols banned)
    {
        if (!InScope(context.ContainingSymbol, banned))
        {
            return;
        }

        var invocation = (IInvocationOperation)context.Operation;
        IMethodSymbol target = invocation.TargetMethod;
        if (banned.IsFloatMathCall(target))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                DeterminismDiagnostics.FloatingPoint,
                invocation.Syntax.GetLocation(),
                $"{target.ContainingType?.Name}.{target.Name}"));
        }
    }

    // Static ambient reads with no local: `DateTime.UtcNow`, `Math.PI`, `MathF.PI`.
    private static void AnalyzeMemberReference(OperationAnalysisContext context, BannedSymbols banned)
    {
        if (!InScope(context.ContainingSymbol, banned))
        {
            return;
        }

        ISymbol? member = context.Operation switch
        {
            IPropertyReferenceOperation property => property.Property,
            IFieldReferenceOperation field => field.Field,
            _ => null,
        };

        if (member is null || !member.IsStatic)
        {
            return;
        }

        DiagnosticDescriptor? descriptor = banned.ClassifyStaticMember(member);
        if (descriptor is not null)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                descriptor,
                context.Operation.Syntax.GetLocation(),
                $"{member.ContainingType?.Name}.{member.Name}"));
        }
    }

    // `foreach` over an unordered collection (Dictionary/HashSet, or Dictionary Keys/Values).
    private static void AnalyzeLoop(OperationAnalysisContext context, BannedSymbols banned)
    {
        if (context.Operation is not IForEachLoopOperation forEach || !InScope(context.ContainingSymbol, banned))
        {
            return;
        }

        IOperation collection = forEach.Collection;
        while (collection is IConversionOperation conversion)
        {
            collection = conversion.Operand;
        }

        if (banned.IsUnorderedEnumeration(collection.Type))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                DeterminismDiagnostics.DictionaryIteration,
                collection.Syntax.GetLocation(),
                collection.Type?.Name ?? "an unordered collection"));
        }
    }

    // Inline float arithmetic with no typed destination, e.g. `(int)(x * 1.5)` — the operation
    // result is float/double even though it is immediately truncated back to an integer.
    private static void AnalyzeArithmetic(OperationAnalysisContext context, BannedSymbols banned)
    {
        if (!InScope(context.ContainingSymbol, banned))
        {
            return;
        }

        ITypeSymbol? type = context.Operation.Type;
        if (banned.IsFloat(type))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                DeterminismDiagnostics.FloatingPoint,
                context.Operation.Syntax.GetLocation(),
                type!.ToDisplayString()));
        }
    }

    private static void ReportType(System.Action<Diagnostic> report, BannedSymbols banned, ITypeSymbol? type, Location location)
    {
        (DiagnosticDescriptor? descriptor, string name) = banned.ClassifyType(type);
        if (descriptor is not null)
        {
            report(Diagnostic.Create(descriptor, location, name));
        }
    }

    private static Location SymbolLocation(ISymbol symbol)
        => symbol.Locations.Length > 0 ? symbol.Locations[0] : Location.None;

    /// <summary>
    /// The banned symbols resolved once per compilation. Types absent from a given compilation
    /// (e.g. MonoGame's <c>Vector2</c> in the pure Abstractions build) resolve to <see langword="null"/>
    /// and simply never match — no rule, no crash.
    /// </summary>
    private sealed class BannedSymbols
    {
        private readonly INamedTypeSymbol? _single;
        private readonly INamedTypeSymbol? _double;
        private readonly INamedTypeSymbol? _math;
        private readonly INamedTypeSymbol? _mathF;
        private readonly INamedTypeSymbol? _random;
        private readonly INamedTypeSymbol? _dateTime;
        private readonly INamedTypeSymbol? _dateTimeOffset;
        private readonly INamedTypeSymbol? _dictionary;
        private readonly INamedTypeSymbol? _dictionaryKeys;
        private readonly INamedTypeSymbol? _dictionaryValues;
        private readonly INamedTypeSymbol? _hashSet;
        private readonly INamedTypeSymbol? _environment;
        private readonly ImmutableArray<INamedTypeSymbol> _xnaFloatTypes;

        private BannedSymbols(Compilation compilation)
        {
            _single = compilation.GetTypeByMetadataName("System.Single");
            _double = compilation.GetTypeByMetadataName("System.Double");
            _math = compilation.GetTypeByMetadataName("System.Math");
            _mathF = compilation.GetTypeByMetadataName("System.MathF");
            _random = compilation.GetTypeByMetadataName("System.Random");
            _dateTime = compilation.GetTypeByMetadataName("System.DateTime");
            _dateTimeOffset = compilation.GetTypeByMetadataName("System.DateTimeOffset");
            _dictionary = compilation.GetTypeByMetadataName("System.Collections.Generic.Dictionary`2");
            _dictionaryKeys = compilation.GetTypeByMetadataName("System.Collections.Generic.Dictionary`2+KeyCollection");
            _dictionaryValues = compilation.GetTypeByMetadataName("System.Collections.Generic.Dictionary`2+ValueCollection");
            _hashSet = compilation.GetTypeByMetadataName("System.Collections.Generic.HashSet`1");
            _environment = compilation.GetTypeByMetadataName("System.Environment");
            ExemptAttribute = compilation.GetTypeByMetadataName("MonoRpgMaker.Abstractions.DeterminismExemptAttribute");

            ImmutableArray<INamedTypeSymbol>.Builder xna = ImmutableArray.CreateBuilder<INamedTypeSymbol>();
            foreach (string name in new[]
            {
                "Microsoft.Xna.Framework.Vector2",
                "Microsoft.Xna.Framework.Vector3",
                "Microsoft.Xna.Framework.Vector4",
                "Microsoft.Xna.Framework.Matrix",
                "Microsoft.Xna.Framework.Quaternion",
            })
            {
                INamedTypeSymbol? symbol = compilation.GetTypeByMetadataName(name);
                if (symbol is not null)
                {
                    xna.Add(symbol);
                }
            }

            _xnaFloatTypes = xna.ToImmutable();
        }

        internal INamedTypeSymbol? ExemptAttribute { get; }

        internal static BannedSymbols Resolve(Compilation compilation) => new(compilation);

        // Maps a banned TYPE to its diagnostic; null for everything allowed (incl. Dictionary as a
        // type, the integer XNA Point, int/long, FixedPoint, GridPoint, …). Arrays and Nullable<T>
        // are unwrapped first, so `float[]`, `double?`, and `Vector2?` are caught at the core type.
        internal (DiagnosticDescriptor? Descriptor, string Name) ClassifyType(ITypeSymbol? type)
        {
            if (type is null)
            {
                return (null, string.Empty);
            }

            ITypeSymbol core = type;
            while (core is IArrayTypeSymbol array)
            {
                core = array.ElementType;
            }

            if (core is INamedTypeSymbol nullable
                && nullable.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T
                && nullable.TypeArguments.Length == 1)
            {
                core = nullable.TypeArguments[0];
            }

            if (IsFloat(core))
            {
                return (DeterminismDiagnostics.FloatingPoint, type.ToDisplayString());
            }

            foreach (INamedTypeSymbol xna in _xnaFloatTypes)
            {
                if (Eq(core, xna))
                {
                    return (DeterminismDiagnostics.XnaFloatMath, core.Name);
                }
            }

            if (Eq(core, _random))
            {
                return (DeterminismDiagnostics.NonDeterministicRandom, core.Name);
            }

            if (Eq(core, _dateTime) || Eq(core, _dateTimeOffset))
            {
                return (DeterminismDiagnostics.AmbientClock, core.Name);
            }

            return (null, string.Empty);
        }

        // True for any MathF member, or a double/float-returning System.Math member (so the integer
        // Math.Max(int, int) / Math.Abs(int) overloads stay allowed).
        internal bool IsFloatMathCall(IMethodSymbol method)
            => Eq(method.ContainingType, _mathF)
               || (Eq(method.ContainingType, _math) && IsFloat(method.ReturnType));

        // Static ambient members used without a local: MathF.PI, Math.PI (double), DateTime.UtcNow, …
        internal DiagnosticDescriptor? ClassifyStaticMember(ISymbol member)
        {
            INamedTypeSymbol? containing = member.ContainingType;
            if (Eq(containing, _mathF))
            {
                return DeterminismDiagnostics.FloatingPoint;
            }

            if (Eq(containing, _math) && IsFloat(MemberType(member)))
            {
                return DeterminismDiagnostics.FloatingPoint;
            }

            if (Eq(containing, _dateTime) || Eq(containing, _dateTimeOffset))
            {
                return DeterminismDiagnostics.AmbientClock;
            }

            if (Eq(containing, _environment) && (member.Name == "TickCount" || member.Name == "TickCount64"))
            {
                return DeterminismDiagnostics.AmbientClock;
            }

            return null;
        }

        // Concrete BCL collections with unspecified enumeration order. SortedDictionary/SortedSet/
        // List/arrays are ordered and deliberately absent (they must not be flagged).
        internal bool IsUnorderedEnumeration(ITypeSymbol? type)
        {
            if (type is null)
            {
                return false;
            }

            ITypeSymbol definition = type.OriginalDefinition;
            return Eq(definition, _dictionary)
                || Eq(definition, _dictionaryKeys)
                || Eq(definition, _dictionaryValues)
                || Eq(definition, _hashSet);
        }

        internal bool IsFloat(ITypeSymbol? type) => Eq(type, _single) || Eq(type, _double);

        private static ITypeSymbol? MemberType(ISymbol member) => member switch
        {
            IPropertySymbol property => property.Type,
            IFieldSymbol field => field.Type,
            _ => null,
        };

        private static bool Eq(ITypeSymbol? a, INamedTypeSymbol? b)
            => b is not null && a is not null && SymbolEqualityComparer.Default.Equals(a, b);
    }
}
