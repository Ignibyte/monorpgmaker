using System;
using System.Collections.Generic;

namespace MonoRpgMaker.Editor.Scaffolding;

/// <summary>The pair of files a scaffold emits: a handler <c>.cs</c> and its <c>.expect</c> stub.</summary>
public sealed record ScaffoldFiles(string CsFileName, string CsText, string ExpectFileName, string ExpectText);

/// <summary>
/// Deterministically renders an <c>event</c> seam-kind skeleton (D-0017 / §3) — no LLM, no eval, pure
/// <c>{{var}}</c> substitution over a committed template that mirrors the tracer fixture. The emitted handler
/// is <em>gate-clean by construction</em>: it returns the shared <c>Outcome</c> vocabulary (so raw state
/// mutation won't compile — #9), uses no determinism-hostile constructs (#7), and its <c>.expect</c> stub
/// parses under the oracle (#10). The author fills only the <c>// fill:</c> hole.
/// </summary>
public static class EventScaffold
{
    private const string CsTemplate =
        """
        using System.Collections.Generic;
        using MonoRpgMaker.Abstractions;

        namespace {{Namespace}};

        /// <summary>The {{Name}} map event — fill in its behaviour.</summary>
        public sealed class {{Name}} : IMapEvent
        {
            /// <summary>Create the event at <paramref name="cell"/>.</summary>
            public {{Name}}(GridPoint cell) => Cell = cell;

            /// <inheritdoc />
            public GridPoint Cell { get; }

            /// <inheritdoc />
            public EventTrigger Trigger => EventTrigger.StepOn;

            /// <inheritdoc />
            public IReadOnlyList<Outcome> Run(IEventContext context)
            {
                // fill: read context.GetSwitch/GetCounter, then return the outcomes to apply —
                //       e.g. [new ShowMessage("..."), new SetSwitch("a_flag", true)].
                return [];
            }
        }
        """;

    private const string ExpectTemplate =
        """
        # {{Name}}.expect — seed game-state => expected Outcome rows. Replace with your real rows.
        => (none)
        """;

    /// <summary>
    /// Render the skeleton for <paramref name="name"/> in <paramref name="namespace"/>. Pure + deterministic
    /// — equal inputs yield byte-identical output. Throws only on a precondition violation
    /// (the CLI validates the identifier first, so the happy path never throws).
    /// </summary>
    public static ScaffoldFiles Render(string name, string @namespace)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(@namespace);
        if (!IsValidIdentifier(name))
        {
            throw new ArgumentException("name must be a valid C# identifier", nameof(name));
        }

        if (!IsValidNamespace(@namespace))
        {
            throw new ArgumentException("namespace must be dot-separated valid C# identifiers", nameof(@namespace));
        }

        string cs = CsTemplate
            .Replace("{{Namespace}}", @namespace, StringComparison.Ordinal)
            .Replace("{{Name}}", name, StringComparison.Ordinal) + "\n";

        string expect = ExpectTemplate
            .Replace("{{Name}}", name, StringComparison.Ordinal) + "\n";

        return new ScaffoldFiles(name + ".cs", cs, name + ".expect", expect);
    }

    /// <summary>Whether <paramref name="name"/> is a valid C# identifier (letter/<c>_</c> start, alphanum/<c>_</c> rest).</summary>
    public static bool IsValidIdentifier(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        if (name.Length == 0 || !(char.IsLetter(name[0]) || name[0] == '_'))
        {
            return false;
        }

        for (int i = 1; i < name.Length; i++)
        {
            if (!(char.IsLetterOrDigit(name[i]) || name[i] == '_'))
            {
                return false;
            }
        }

        // A reserved keyword is a lexically-valid identifier but cannot be a bare type name (it would need
        // @-escaping), so a scaffolded skeleton using one would not compile — breaking gate-clean-by-construction.
        return !ReservedKeywords.Contains(name);
    }

    /// <summary>Whether <paramref name="namespace"/> is one or more dot-separated valid C# identifiers.</summary>
    public static bool IsValidNamespace(string @namespace)
    {
        ArgumentNullException.ThrowIfNull(@namespace);
        if (@namespace.Length == 0)
        {
            return false;
        }

        foreach (string segment in @namespace.Split('.'))
        {
            if (!IsValidIdentifier(segment))
            {
                return false;
            }
        }

        return true;
    }

    private static readonly HashSet<string> ReservedKeywords = new(StringComparer.Ordinal)
    {
        "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked", "class",
        "const", "continue", "decimal", "default", "delegate", "do", "double", "else", "enum", "event",
        "explicit", "extern", "false", "finally", "fixed", "float", "for", "foreach", "goto", "if",
        "implicit", "in", "int", "interface", "internal", "is", "lock", "long", "namespace", "new", "null",
        "object", "operator", "out", "override", "params", "private", "protected", "public", "readonly",
        "ref", "return", "sbyte", "sealed", "short", "sizeof", "stackalloc", "static", "string", "struct",
        "switch", "this", "throw", "true", "try", "typeof", "uint", "ulong", "unchecked", "unsafe", "ushort",
        "using", "virtual", "void", "volatile", "while",
    };
}
