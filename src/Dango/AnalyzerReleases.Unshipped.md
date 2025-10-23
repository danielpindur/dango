; Unshipped analyzer releases
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

### New Rules
Rule ID | Category       | Severity | Notes                                  
---------|----------------|----------|----------------------------------------
DANGO001 | Dango.Generator | Warning  | Missing IDangoMapperRegistrar interface 
DANGO002 | Dango.Generator | Error    | Missing Register method                
DANGO003 | Dango.Generator | Warning  | Duplicate enum mapping                 
DANGO004 | Dango.Generator | Error    | Invalid enum type                      
DANGO005 | Dango.Generator | Error | DiagnosticDescriptors
