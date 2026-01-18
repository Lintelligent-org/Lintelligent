# LINT003 Regression Testing

## Historical Issues & Lessons Learned

During development of the PreferOptionMonad analyzer (LINT003), we encountered several critical issues that prevented the analyzer from working correctly. This document captures those issues and the tests we created to prevent regressions.

### Issue #1: Incorrect Diagnostic Severity (CRITICAL)

**Problem**: The Core analyzer (`PreferOptionMonadAnalyzer.cs`) had `DiagnosticSeverity.Info` hardcoded in 6 locations instead of `Warning`. Info-level diagnostics do not appear in `dotnet build` output, making the analyzer effectively invisible to users building from the command line.

**Root Cause**: The severity was defined in the Roslyn adapter correctly as `Warning`, but the Core analyzer was creating diagnostics with `Info` severity, overriding the adapter's configuration.

**Fix**: Changed ALL instances of `DiagnosticSeverity.Info` to `DiagnosticSeverity.Warning` in lines 75, 98, 127, 145, 166, 188 of `PreferOptionMonadAnalyzer.cs`.

**Regression Test**: `Regression_SeverityMustBeWarning_NotInfo` - Verifies the diagnostic uses Warning severity.

**Lesson Learned**:
- Always use Warning or Error severity for diagnostics that should be visible in build output
- Info severity is only appropriate for informational messages that don't require action
- Test severity level explicitly in regression tests

### Issue #2: Message Format Placeholders Not Replaced

**Problem**: The diagnostic message format in `Resources.resx` contained `{0}`, `{1}`, `{2}` placeholders, but these were being displayed literally instead of being replaced with method names and types.

**Root Cause**: The Roslyn adapter was creating diagnostics using `Diagnostic.Create(Rule, location)` without providing message arguments. The resource string format didn't match how the diagnostic was being created.

**Fix**: Simplified the message format in `Resources.resx` to a non-parameterized string: `"Consider using Option<T> instead of nullable types to make absence of value explicit"`

**Regression Test**: `Regression_MessageMustNotContainPlaceholders` - Checks that the diagnostic descriptor's message doesn't contain `{0}`, `{1}`, or `{2}`.

**Lesson Learned**:
- If using parameterized messages, ensure `Diagnostic.Create()` receives the correct arguments
- Simpler, non-parameterized messages are more maintainable for generic suggestions
- Test message content to ensure placeholders are handled correctly

### Issue #3: Package Missing Core.dll Dependency

**Problem**: The NuGet package (`Lintelligent.Analyzers.Basic`) didn't include `Lintelligent.Core.dll`, causing runtime failures when the analyzer tried to instantiate Core analyzers.

**Root Cause**: The package project (`Lintelligent.Analyzers.Basic.Package.csproj`) only referenced the Roslyn adapter DLL, not the Core library it depends on.

**Fix**: Added `<TfmSpecificPackageFile Include="$(OutputPath)\Lintelligent.Core.dll" PackagePath="analyzers/dotnet/cs" />` to package all dependencies.

**Verification**: Manual inspection of package contents using `unzip` or `7zip` to verify all three DLLs are present.

**Lesson Learned**:
- Always verify NuGet package contents after building
- Roslyn analyzers require ALL dependencies to be packaged
- Use `TfmSpecificPackageFile` for explicit control over packaged files

### Issue #4: Build Caching of Stale DLLs

**Problem**: After fixing issues, `dotnet pack` was packaging old DLLs with incorrect code, even though the projects had been rebuilt.

**Root Cause**: The package project's output path still contained stale DLLs from previous builds.

**Fix**: Full clean rebuild sequence:
```powershell
dotnet clean
dotnet build src\Lintelligent.Core\
dotnet build src\Lintelligent.Analyzers.Basic\
dotnet build src\Lintelligent.Analyzers.Basic.CodeFixes\
dotnet pack src\Lintelligent.Analyzers.Basic.Package\
```

**Lesson Learned**:
- Always do a clean build when diagnosing analyzer issues
- Verify DLL timestamps match expected build times
- Use `dotnet clean` before critical packaging operations

### Issue #5: Generic Nullable Detection (T?)

**Problem**: Generic nullable types like `T?` were not being detected in some scenarios.

**Root Cause**: The semantic model can fail to resolve generic type parameters in certain contexts. The analyzer already had syntax-based detection that handles this case.

**Fix**: The Core analyzer uses BOTH syntax-based detection (`NullableTypeSyntax`) AND semantic analysis, ensuring generic nullables are caught.

**Regression Test**: `Regression_GenericNullable_MustBeDetected` - Verifies `T? where T : class` is detected.

**Lesson Learned**:
- Use syntax-based detection for patterns that are visible in source code
- Supplement with semantic analysis for complex type resolution
- Test edge cases like generic constraints

## Current Test Coverage

The `PreferOptionMonadRegressionTests` class contains the following tests:

1. **Regression_SeverityMustBeWarning_NotInfo** - Ensures diagnostics use Warning severity
2. **Regression_MessageMustNotContainPlaceholders** - Verifies message format is correct
3. **Regression_GenericNullable_MustBeDetected** - Tests T? detection
4. **Regression_NullableValueTypes_MustBeDetected** - Tests int?, decimal?, etc.
5. **Regression_ExpressionBodiedMethod_MustBeDetected** - Tests expression-bodied methods
6. **Regression_NonNullableTypes_ShouldNotTrigger** - Ensures non-nullable types don't trigger
7. **Regression_DiagnosticCategory_MustBeWarning** - Verifies diagnostic descriptor category
8. **Regression_MultipleNullableMethods_AllDetected** - Tests multiple methods

### Known Limitation

The analyzer currently produces duplicate diagnostics for some patterns due to running both syntax-based and semantic-based checks. This is acceptable as long as at least one diagnostic appears. The tests account for this by using `VerifyCS.Diagnostic("LINT003").WithSpan(...)` which accepts extra diagnostics at the same location.

## Future Improvements

1. **Deduplicate Diagnostics**: Modify the Core analyzer to avoid reporting the same diagnostic twice
2. **Parameterized Messages**: If we want more specific messages, implement proper argument passing from Roslyn adapter
3. **Build Integration Tests**: Add tests that verify diagnostics appear in `dotnet build` output (currently manual)
4. **Package Validation**: Automate package content verification in CI/CD

## Version History

- **0.2.0** - Initial release (broken: missing Core.dll)
- **0.3.0** - Added Core.dll to package (broken: wrong severity)
- **0.3.1** - Removed sample analyzer noise (broken: still wrong severity)
- **0.3.2** - Fixed message format placeholders (broken: still Info severity)
- **0.3.3** - Fixed severity in Roslyn adapter only (broken: Core still had Info)
- **0.3.4** - Fixed severity in Core analyzer ✅ **WORKING**

## Testing Checklist

Before releasing a new version of LINT003:

- [ ] All regression tests pass (`dotnet test --filter PreferOptionMonadRegressionTests`)
- [ ] Package contains all 3 DLLs (Core, Analyzers.Basic, CodeFixes)
- [ ] Diagnostics appear in `dotnet build` output (manual test in NeuraPOS or similar project)
- [ ] Message format is correct (no placeholders shown)
- [ ] Severity is Warning (not Info)
- [ ] All 3 project DLLs have matching timestamps
- [ ] Clean rebuild performed before packaging

## References

- [LINT003 Specification](LINT003.md)
- [Roslyn Analyzer Testing Documentation](https://github.com/dotnet/roslyn-sdk/tree/main/src/Microsoft.CodeAnalysis.Testing)
- [NuGet Package for Analyzers](https://learn.microsoft.com/en-us/nuget/guides/analyzers-conventions)
