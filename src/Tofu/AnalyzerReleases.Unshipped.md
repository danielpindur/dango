; Unshipped analyzer releases
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

### New Rules
Rule ID | Category       | Severity | Notes                                  
---------|----------------|----------|----------------------------------------
TOFU001 | Tofu.Generator | Warning  | Missing ITofuMapperRegistrar interface 
TOFU002 | Tofu.Generator | Error    | Missing Register method                
TOFU003 | Tofu.Generator | Warning  | Duplicate enum mapping                 
TOFU004 | Tofu.Generator | Error    | Invalid enum type                      
