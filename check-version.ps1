#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Checks if the package version in .csproj matches the latest package in artifacts.

.DESCRIPTION
    Verifies version alignment between:
    - PackageVersion in Lintelligent.Analyzers.Basic.Package.csproj
    - Latest .nupkg file in artifacts folder
#>

$ErrorActionPreference = 'Stop'

# Paths
$projectFile = Join-Path $PSScriptRoot "src\Lintelligent.Analyzers.Basic.Package\Lintelligent.Analyzers.Basic.Package.csproj"
$artifactsDir = Join-Path $PSScriptRoot "artifacts"

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "Version Alignment Check" -ForegroundColor Cyan
Write-Host "========================================`n" -ForegroundColor Cyan

# Read version from project file
if (-not (Test-Path $projectFile)) {
    Write-Error "Project file not found: $projectFile"
    exit 1
}

$xml = [xml](Get-Content $projectFile)
$projectVersion = $xml.Project.PropertyGroup.PackageVersion

if (-not $projectVersion) {
    Write-Error "PackageVersion not found in project file"
    exit 1
}

Write-Host "Project Version: $projectVersion" -ForegroundColor Yellow

# Find all packages in artifacts
if (Test-Path $artifactsDir) {
    $packages = Get-ChildItem -Path $artifactsDir -Filter "Lintelligent.Analyzers.Basic.*.nupkg" -File | 
        ForEach-Object {
            if ($_.Name -match 'Lintelligent\.Analyzers\.Basic\.(\d+\.\d+\.\d+)\.nupkg') {
                [PSCustomObject]@{
                    Version = $matches[1]
                    File = $_.Name
                    Path = $_.FullName
                    Created = $_.CreationTime
                }
            }
        } | Sort-Object { [Version]$_.Version } -Descending
    
    if ($packages) {
        $latestPackage = $packages[0]
        Write-Host "Latest Package:  $($latestPackage.Version) (Created: $($latestPackage.Created))" -ForegroundColor Cyan
        
        Write-Host "`nAll packages in artifacts:" -ForegroundColor Gray
        foreach ($pkg in $packages) {
            $marker = if ($pkg.Version -eq $projectVersion) { " ← CURRENT" } else { "" }
            Write-Host "  - $($pkg.Version)$marker" -ForegroundColor Gray
        }
        
        # Compare versions
        if ($latestPackage.Version -eq $projectVersion) {
            Write-Host "`n✓ Versions are aligned!" -ForegroundColor Green
            Write-Host "  Project version matches latest package" -ForegroundColor Green
            exit 0
        } else {
            Write-Host "`n⚠ Version mismatch detected!" -ForegroundColor Yellow
            Write-Host "  Project: $projectVersion" -ForegroundColor Yellow
            Write-Host "  Latest:  $($latestPackage.Version)" -ForegroundColor Yellow
            
            $projVer = [Version]$projectVersion
            $pkgVer = [Version]$latestPackage.Version
            
            if ($projVer -gt $pkgVer) {
                Write-Host "`n→ Project version is NEWER than latest package" -ForegroundColor Cyan
                Write-Host "  Run: dotnet pack to build version $projectVersion" -ForegroundColor Gray
            } else {
                Write-Host "`n→ Latest package is NEWER than project version" -ForegroundColor Magenta
                Write-Host "  This is unusual - project should be at least $($latestPackage.Version)" -ForegroundColor Gray
            }
            exit 1
        }
    } else {
        Write-Host "Latest Package:  None found" -ForegroundColor Red
        Write-Host "`n⚠ No packages found in artifacts folder" -ForegroundColor Yellow
        Write-Host "  Run: dotnet pack to create version $projectVersion" -ForegroundColor Gray
        exit 1
    }
} else {
    Write-Host "Artifacts folder not found: $artifactsDir" -ForegroundColor Red
    Write-Host "`n⚠ No artifacts directory exists" -ForegroundColor Yellow
    Write-Host "  Run: dotnet pack to create version $projectVersion" -ForegroundColor Gray
    exit 1
}
