; Unshipped analyzer release
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
MRM1001 | Determinism | Warning | Floating-point math banned in simulation code (D-0016)
MRM1002 | Determinism | Warning | XNA float-backed math types banned in simulation code (D-0016)
MRM1003 | Determinism | Warning | foreach over an unordered collection (Dictionary/HashSet) banned in simulation code (D-0016)
MRM1004 | Determinism | Warning | System.Random banned in simulation code (D-0016)
MRM1005 | Determinism | Warning | DateTime/DateTimeOffset banned in simulation code (D-0016)
