using System;
using System.Collections.Generic;
using System.IO;

namespace MonoRpgMaker.Editor.Scaffolding;

/// <summary>
/// The <c>scaffold event &lt;Name&gt; &lt;out-dir&gt; [--namespace &lt;ns&gt;]</c> verb: validates the name,
/// renders via <see cref="EventScaffold"/> (pure), and writes the <c>.cs</c> + <c>.expect</c> pair — refusing
/// to overwrite an existing file. The rendering is pure; this is the IO boundary.
/// </summary>
public static class ScaffoldCli
{
    private const string DefaultNamespace = "MonoRpgMaker.Engine.Sim.Tracer";

    /// <summary>Run the verb. Returns a process exit code (0 = ok, 1 = refused, 2 = usage/invalid).</summary>
    public static int Run(IReadOnlyList<string> args, TextWriter output)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentNullException.ThrowIfNull(output);

        if (args.Count < 4 || args[0] != "scaffold" || args[1] != "event")
        {
            output.WriteLine("usage: scaffold event <Name> <out-dir> [--namespace <ns>]");
            return 2;
        }

        string name = args[2];
        string outDir = args[3];

        if (!EventScaffold.IsValidIdentifier(name))
        {
            output.WriteLine("error: '" + name + "' is not a valid C# identifier");
            return 2;
        }

        string targetNamespace = ParseNamespace(args) ?? DefaultNamespace;
        if (!EventScaffold.IsValidNamespace(targetNamespace))
        {
            output.WriteLine("error: '" + targetNamespace + "' is not a valid namespace");
            return 2;
        }

        ScaffoldFiles files = EventScaffold.Render(name, targetNamespace);
        string csPath = Path.Combine(outDir, files.CsFileName);
        string expectPath = Path.Combine(outDir, files.ExpectFileName);

        if (File.Exists(csPath) || File.Exists(expectPath))
        {
            output.WriteLine("error: refusing to overwrite an existing file in " + outDir);
            return 1;
        }

        Directory.CreateDirectory(outDir);
        File.WriteAllText(csPath, files.CsText);
        File.WriteAllText(expectPath, files.ExpectText);
        output.WriteLine("scaffolded " + files.CsFileName + " + " + files.ExpectFileName + " in " + outDir);
        return 0;
    }

    private static string? ParseNamespace(IReadOnlyList<string> args)
    {
        for (int i = 4; i < args.Count - 1; i++)
        {
            if (args[i] == "--namespace")
            {
                return args[i + 1];
            }
        }

        return null;
    }
}
