#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Bumps the version of Lintelligent.Analyzers.Basic.Package and publishes to local NuGet.

.DESCRIPTION
    This script simplifies version management by:
    - Reading the current version from the .csproj file
    - Incrementing major, minor, or patch version
    - Updating the .csproj file
    - Building and packing the NuGet package
    - Optionally publishing to a local NuGet source

.PARAMETER BumpType
    The type of version bump: Major/ma, Minor/mi, or Patch/pa (default: Patch)

.PARAMETER ReleaseNotes
    Release notes for the new version. If not specified, uses a default message.

.PARAMETER LocalSource
    Path to local NuGet source directory. If not specified, only builds the package.

.EXAMPLE
    .\bump-version.ps1 pa
    Bumps from 0.3.4 to 0.3.5

.EXAMPLE
    .\bump-version.ps1 pa -ReleaseNotes "Fixed bug in LINT003 analyzer"
    Bumps patch version with custom release notes

.EXAMPLE
    .\bump-version.ps1 mi -LocalSource "C:\LocalNuGet" -ReleaseNotes "Added new analyzer LINT004"
    Bumps from 0.3.4 to 0.4.0 with release notes and publishes to local source

.EXAMPLE
    .\bump-version.ps1 ma
    Bumps from 0.3.4 to 1.0.0
#>

[CmdletBinding()]
param(
    [Parameter(Position = 0)]
    [ValidateSet('Major', 'Minor', 'Patch', 'ma', 'mi', 'pa')]
    [string]$BumpType = 'Patch',
    
    [Parameter()]
    [string]$ReleaseNotes,
    
    [Parameter()]
    [string]$LocalSource
)

$ErrorActionPreference = 'Stop'

# Normalize shorthand to full names
$BumpType = switch ($BumpType) {
    'ma' { 'Major' }
    'mi' { 'Minor' }
    'pa' { 'Patch' }
    default { $BumpType }
}

# Paths
$projectFile = Join-Path $PSScriptRoot "src\Lintelligent.Analyzers.Basic.Package\Lintelligent.Analyzers.Basic.Package.csproj"
$outputDir = Join-Path $PSScriptRoot "artifacts\packages"

# Verify project file exists
if (-not (Test-Path $projectFile)) {
    Write-Error "Project file not found: $projectFile"
    exit 1
}

# Read current version
Write-Host "Reading current version..." -ForegroundColor Cyan
$xml = [xml](Get-Content $projectFile)
$currentVersion = $xml.Project.PropertyGroup.PackageVersion

if (-not $currentVersion) {
    Write-Error "PackageVersion not found in project file"
    exit 1
}

Write-Host "Current version: $currentVersion" -ForegroundColor Yellow

# Parse version
$versionParts = $currentVersion -split '\.'
if ($versionParts.Count -ne 3) {
    Write-Error "Version format invalid. Expected format: Major.Minor.Patch"
    exit 1
}

$major = [int]$versionParts[0]
$minor = [int]$versionParts[1]
$patch = [int]$versionParts[2]

# Bump version
switch ($BumpType) {
    'Major' {
        $major++
        $minor = 0
        $patch = 0
    }
    'Minor' {
        $minor++
        $patch = 0
    }
    'Patch' {
        $patch++
    }
}

$newVersion = "$major.$minor.$patch"
Write-Host "New version: $newVersion" -ForegroundColor Green

# Check if this version already exists in artifacts
$existingPackages = @()
$artifactsDir = Join-Path $PSScriptRoot "artifacts"
if (Test-Path $artifactsDir) {
    $existingPackages = Get-ChildItem -Path $artifactsDir -Filter "Lintelligent.Analyzers.Basic.$newVersion.nupkg" -File -ErrorAction SilentlyContinue
}

if ($existingPackages.Count -gt 0) {
    Write-Warning "Package version $newVersion already exists in artifacts folder!"
    Write-Host "Existing package: $($existingPackages[0].FullName)" -ForegroundColor Yellow
    Write-Host "Created: $($existingPackages[0].CreationTime)" -ForegroundColor Yellow
    
    $response = Read-Host "Do you want to rebuild this version? (y/N)"
    if ($response -notmatch '^[Yy]') {
        Write-Host "`nOperation cancelled." -ForegroundColor Yellow
        exit 0
    }
    Write-Host "Proceeding with rebuild..." -ForegroundColor Cyan
}

# Generate release notes if not provided
if (-not $ReleaseNotes) {
    $ReleaseNotes = "v$newVersion`: Includes LINT001 (Avoid Empty Catch), LINT002 (Complex Conditionals), and LINT003 (Prefer Option Monad) analyzers with code fixes."
}

Write-Host "Release notes: $ReleaseNotes" -ForegroundColor Cyan

# Update project file
Write-Host "Updating project file..." -ForegroundColor Cyan
$content = Get-Content $projectFile -Raw
$content = $content -replace "<PackageVersion>$currentVersion</PackageVersion>", "<PackageVersion>$newVersion</PackageVersion>"

# Escape XML special characters in release notes
$escapedNotes = $ReleaseNotes -replace '&', '&amp;' -replace '<', '&lt;' -replace '>', '&gt;' -replace '"', '&quot;' -replace "'", '&apos;'

# Update release notes (match any existing content between tags)
$content = $content -replace '<PackageReleaseNotes>.*?</PackageReleaseNotes>', "<PackageReleaseNotes>$escapedNotes</PackageReleaseNotes>"

Set-Content $projectFile -Value $content -NoNewline

Write-Host "✓ Version and release notes updated in project file" -ForegroundColor Green

# Clean previous builds in output directory
Write-Host "Cleaning build output directory..." -ForegroundColor Cyan
if (Test-Path $outputDir) {
    Remove-Item $outputDir -Recurse -Force
}

# Build and pack
Write-Host "Building and packing NuGet package..." -ForegroundColor Cyan
$packArgs = @(
    'pack'
    $projectFile
    '--configuration', 'Release'
    '--output', $outputDir
    '/p:PackageVersion=' + $newVersion
)

dotnet @packArgs

if ($LASTEXITCODE -ne 0) {
    Write-Error "Pack failed with exit code $LASTEXITCODE"
    exit $LASTEXITCODE
}

$packageFile = Join-Path $outputDir "Lintelligent.Analyzers.Basic.$newVersion.nupkg"

if (-not (Test-Path $packageFile)) {
    Write-Error "Package file not found: $packageFile"
    exit 1
}

Write-Host "✓ Package created: $packageFile" -ForegroundColor Green

# Copy to artifacts folder for version tracking
if (-not (Test-Path $artifactsDir)) {
    New-Item -ItemType Directory -Path $artifactsDir -Force | Out-Null
}

$artifactPackage = Join-Path $artifactsDir "Lintelligent.Analyzers.Basic.$newVersion.nupkg"
Copy-Item $packageFile -Destination $artifactPackage -Force
Write-Host "✓ Package copied to artifacts folder" -ForegroundColor Green

# Publish to local source if specified
if ($LocalSource) {
    Write-Host "Publishing to local NuGet source: $LocalSource" -ForegroundColor Cyan
    
    if (-not (Test-Path $LocalSource)) {
        Write-Host "Creating local source directory: $LocalSource" -ForegroundColor Yellow
        New-Item -ItemType Directory -Path $LocalSource -Force | Out-Null
    }
    
    Copy-Item $packageFile -Destination $LocalSource -Force
    Write-Host "✓ Package published to local source" -ForegroundColor Green
}

# Summary
Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "Version Bump Complete!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Old Version:    $currentVersion" -ForegroundColor Yellow
Write-Host "New Version:    $newVersion" -ForegroundColor Green
Write-Host "Release Notes:  $ReleaseNotes" -ForegroundColor White
Write-Host "Package:        $packageFile" -ForegroundColor White
Write-Host "Artifacts:      $artifactPackage" -ForegroundColor White

if ($LocalSource) {
    Write-Host "Local Source:   $LocalSource" -ForegroundColor White
    Write-Host "`nTo use in a project, run:" -ForegroundColor Cyan
    Write-Host "  dotnet add package Lintelligent.Analyzers.Basic --version $newVersion --source `"$LocalSource`"" -ForegroundColor Gray
} else {
    Write-Host "`nTo publish to a local source, run:" -ForegroundColor Cyan
    Write-Host "  .\bump-version.ps1 $BumpType -LocalSource `"C:\LocalNuGet`"" -ForegroundColor Gray
}

Write-Host "`nTo install as global tool for testing:" -ForegroundColor Cyan
Write-Host "  dotnet tool install --global --add-source `"$outputDir`" Lintelligent.Analyzers.Basic --version $newVersion" -ForegroundColor Gray
