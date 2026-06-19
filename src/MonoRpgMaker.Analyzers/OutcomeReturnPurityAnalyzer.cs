using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace MonoRpgMaker.Analyzers;

/// <summary>
/// Enforces outcome-return purity (D-0017): a simulation handler that returns
/// <c>IReadOnlyList&lt;Outcome&gt;</c> must not mutate game state. Calling a
/// <c>[MonoRpgMaker.Abstractions.StateMutator]</c>-marked method (e.g. <c>GameState.Set</c>/<c>Add</c>)
/// from inside such a handler is flagged <c>MRM1006</c> — the change must instead be <em>returned</em> as
/// an <c>Outcome</c> for an applier to apply. The applier (which returns <see langword="void"/>) is never
/// a handler, so it is not flagged. Scoped to the simulation namespaces via <see cref="SimScope"/>; a call
/// inside a handler's local function or lambda still resolves (via its containing member) to the handler.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class OutcomeReturnPurityAnalyzer : DiagnosticAnalyzer
{
    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(DeterminismDiagnostics.OutcomeReturnPurity);

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(OnCompilationStart);
    }

    private static void OnCompilationStart(CompilationStartAnalysisContext context)
    {
        INamedTypeSymbol? stateMutator =
            context.Compilation.GetTypeByMetadataName("MonoRpgMaker.Abstractions.StateMutatorAttribute");
        INamedTypeSymbol? outcome =
            context.Compilation.GetTypeByMetadataName("MonoRpgMaker.Abstractions.Outcome");
        INamedTypeSymbol? readOnlyList =
            context.Compilation.GetTypeByMetadataName("System.Collections.Generic.IReadOnlyList`1");

        // The rule needs all three types; if any is absent from this compilation, there is nothing to enforce.
        if (stateMutator is null || outcome is null || readOnlyList is null)
        {
            return;
        }

        context.RegisterOperationAction(
            c => AnalyzeInvocation(c, stateMutator, outcome, readOnlyList),
            OperationKind.Invocation);
    }

    private static void AnalyzeInvocation(
        OperationAnalysisContext context,
        INamedTypeSymbol stateMutator,
        INamedTypeSymbol outcome,
        INamedTypeSymbol readOnlyList)
    {
        if (!SimScope.IsInSimNamespace(context.ContainingSymbol))
        {
            return;
        }

        var invocation = (IInvocationOperation)context.Operation;

        if (!SimScope.HasAttribute(invocation.TargetMethod, stateMutator))
        {
            return;
        }

        if (!SimScope.IsInsideOutcomeHandler(context.ContainingSymbol, readOnlyList, outcome))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            DeterminismDiagnostics.OutcomeReturnPurity,
            invocation.Syntax.GetLocation(),
            invocation.TargetMethod.Name));
    }
}
