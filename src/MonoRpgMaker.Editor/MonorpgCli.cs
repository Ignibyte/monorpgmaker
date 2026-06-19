using System;
using System.Collections.Generic;
using System.IO;
using MonoRpgMaker.Editor.Expectations;
using MonoRpgMaker.Editor.Scaffolding;

namespace MonoRpgMaker.Editor;

/// <summary>The <c>monorpg</c> maker-tool verb router: dispatches to the scaffolder or the .expect oracle.</summary>
public static class MonorpgCli
{
    /// <summary>Route a verb. Returns a process exit code.</summary>
    public static int Run(IReadOnlyList<string> args, TextWriter output)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentNullException.ThrowIfNull(output);

        if (args.Count >= 1 && args[0] == "scaffold")
        {
            return ScaffoldCli.Run(args, output);
        }

        if (args.Count >= 1 && args[0] == "check-expectations")
        {
            return OracleCli.Run(args, output);
        }

        output.WriteLine("usage: monorpg <scaffold|check-expectations> ...");
        return 2;
    }
}
