# Lintelligent.Analyzers.Basic Package

This folder contains the NuGet package project and package output for the
`Lintelligent.Analyzers.Basic` analyzer set. The package delivers Roslyn
analyzers and code fixes to be consumed by Visual Studio and `dotnet` builds.

## What's in this folder

- `Lintelligent.Analyzers.Basic.Package.csproj` — package project used to build the
  NuGet package.
- `bin/` and `obj/` — build outputs and generated nuspec/package artifacts.
- `tools/install.ps1` and `tools/uninstall.ps1` — optional PowerShell helpers for
  installing/uninstalling the package into a machine or repo-local feed.

## Installation

Install the analyzer package from NuGet (replace `1.0.0` with the desired version):

```powershell
dotnet add <YourProject>.csproj package Lintelligent.Analyzers.Basic --version 1.0.0
```

After adding the package, analyzers and code fixes are applied automatically at
build time in Visual Studio and `dotnet build`.

### Using the provided PowerShell helpers

The `tools/install.ps1` script can be used to copy package contents into a local
tools folder or help register the analyzers in non-standard environments. Run
from a PowerShell prompt (use elevation if the script requires it). Example
dot-sourcing or invocation from the repository root:

```powershell
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope Process
. .\src\Lintelligent.Analyzers.Basic.Package\tools\install.ps1
# or
& .\src\Lintelligent.Analyzers.Basic.Package\tools\install.ps1
```

To undo what the installer does, run the uninstall script the same way:

```powershell
. .\src\Lintelligent.Analyzers.Basic.Package\tools\uninstall.ps1
# or
& .\src\Lintelligent.Analyzers.Basic.Package\tools\uninstall.ps1
```

## Building the package locally

To build and produce the `.nupkg` locally from the package project (run from
the repository root):

```powershell
dotnet pack .\src\Lintelligent.Analyzers.Basic.Package\Lintelligent.Analyzers.Basic.Package.csproj -c Release
```

The resulting `.nupkg` is emitted under `src\Lintelligent.Analyzers.Basic.Package\bin`.

## How the package is consumed

- The package places analyzer DLLs under the `analyzers/dotnet/cs` folder inside
  the NuGet package so the analyzers run as part of normal compilation. These
  analyzers and code fixes are build-time tooling — they affect compilation and
  the IDE experience (diagnostics/code fixes) but are not included in runtime
  application binaries.
- Code fixes appear in Visual Studio when the corresponding analyzer produces a
  diagnostic and a fix is available.

## Contributing / Extending

- To update the package contents, edit the projects under `src/` (analyzers and
  code-fix projects) and update the package project as needed.
- Run tests in `tests/` (see the repository root for test project details) and
  verify analyzers before incrementing package versions.

### Adding the package to a project

You can add the package via the `dotnet` CLI or by editing your project file.

From the project folder (convenient when running inside the project directory):

```powershell
dotnet add package Lintelligent.Analyzers.Basic --version 1.0.0
```

Or add a `PackageReference` to a `.csproj` (recommended for repository control):

```xml
<ItemGroup>
  <PackageReference Include="Lintelligent.Analyzers.Basic" Version="1.0.0" PrivateAssets="all" />
</ItemGroup>
```

Using `PrivateAssets="all"` prevents the analyzer package reference from flowing
to downstream consumers when creating libraries.

## Useful links

- Package project: [src/Lintelligent.Analyzers.Basic.Package/Lintelligent.Analyzers.Basic.Package.csproj](src/Lintelligent.Analyzers.Basic.Package/Lintelligent.Analyzers.Basic.Package.csproj)
- Installer script: [src/Lintelligent.Analyzers.Basic.Package/tools/install.ps1](src/Lintelligent.Analyzers.Basic.Package/tools/install.ps1)
- Uninstall script: [src/Lintelligent.Analyzers.Basic.Package/tools/uninstall.ps1](src/Lintelligent.Analyzers.Basic.Package/tools/uninstall.ps1)

---
Created to help consume and build the analyzer package. If you'd like a
longer README with publishing, CI, or versioning guidance, tell me and I will
expand it.
