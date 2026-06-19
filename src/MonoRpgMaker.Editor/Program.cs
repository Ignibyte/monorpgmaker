using System;
using System.Diagnostics.CodeAnalysis;
using MonoRpgMaker.Editor.Expectations;

namespace MonoRpgMaker.Editor;

/// <summary>
/// The maker-tool CLI entry point (the <c>monorpg</c>-style host). A thin composition root that
/// dispatches to the testable <see cref="OracleCli"/>; the real logic + its exit code live there.
/// </summary>
[ExcludeFromCodeCoverage]
internal static class Program
{
    private static int Main(string[] args) => OracleCli.Run(args, Console.Out);
}
