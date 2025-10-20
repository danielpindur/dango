; Unshipped analyzer releases
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

### New Rules
Rule ID | Category       | Severity | Notes                                  
---------|----------------|----------|----------------------------------------
TOFU001 | Dango.Generator | Warning  | Missing IDangoMapperRegistrar interface 
TOFU002 | Dango.Generator | Error    | Missing Register method                
TOFU003 | Dango.Generator | Warning  | Duplicate enum mapping                 
TOFU004 | Dango.Generator | Error    | Invalid enum type                      
TOFU005 | Dango.Generator | Error | DiagnosticDescriptors
