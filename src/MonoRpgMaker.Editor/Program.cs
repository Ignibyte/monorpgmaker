using System;
using System.Diagnostics.CodeAnalysis;

namespace MonoRpgMaker.Editor;

/// <summary>
/// The maker-tool CLI entry point (the <c>monorpg</c>-style host). A thin composition root that
/// dispatches to the testable <see cref="MonorpgCli"/> verb router; the real logic + exit codes live there.
/// </summary>
[ExcludeFromCodeCoverage]
internal static class Program
{
    private static int Main(string[] args) => MonorpgCli.Run(args, Console.Out);
}
