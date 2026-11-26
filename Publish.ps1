<#
.SYNOPSIS
    Packs all NuGet projects below -Root, collects the packages in _Publish,
    opens the nuget.org upload page and bumps the minor version afterwards.

.DESCRIPTION
    A project counts as NuGet project if its csproj contains <PackageId>.
    Keep Publish.ps1 and Publish.bat next to the .sln, or keep Publish.ps1 in a
    central folder and pass the repository root via -Root.
    The version is read from the csproj (or the nearest Directory.Build.props above it).
    The released version is the one found BEFORE the bump, so the version in the
    csproj always is the one that will be released next.
#>
[CmdletBinding()]
param(
    [string]$Root = $PSScriptRoot,
    [switch]$NoOpen,
    [switch]$NoBump
)

$ErrorActionPreference = 'Stop'

$Root = (Resolve-Path -LiteralPath $Root).Path.TrimEnd('\')
$publishDir = Join-Path $Root '_Publish'
$uploadUrl = 'https://www.nuget.org/packages/manage/upload'
$excluded = '\\(bin|obj|node_modules|\.git|\.vs|_Publish)\\'

function Find-VersionFile {
    param([System.IO.FileInfo]$Project)

    if (Select-String -LiteralPath $Project.FullName -Pattern '<Version>' -Quiet) {
        return $Project.FullName
    }

    $dir = $Project.Directory

    while ($null -ne $dir -and $dir.FullName.StartsWith($Root, [System.StringComparison]::OrdinalIgnoreCase)) {
        $candidate = Join-Path $dir.FullName 'Directory.Build.props'

        if ((Test-Path -LiteralPath $candidate) -and (Select-String -LiteralPath $candidate -Pattern '<Version>' -Quiet)) {
            return $candidate
        }

        $dir = $dir.Parent
    }

    return $null
}

function Update-MinorVersion {
    param([string]$Path)

    $bytes = [System.IO.File]::ReadAllBytes($Path)
    $hasBom = $bytes.Length -ge 3 -and $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF
    $content = [System.IO.File]::ReadAllText($Path)

    # Keeps a prerelease suffix, resets patch to 0
    $match = [regex]::Match($content, '(?<=<Version>)(\d+)\.(\d+)\.(\d+)([^<]*)(?=</Version>)')

    if (-not $match.Success) {
        Write-Warning "No <Version> in x.y.z format found in $Path, not bumped."
        return
    }

    $old = $match.Value
    $new = '{0}.{1}.0{2}' -f $match.Groups[1].Value, ([int]$match.Groups[2].Value + 1), $match.Groups[4].Value

    $content = $content.Remove($match.Index, $match.Length).Insert($match.Index, $new)
    [System.IO.File]::WriteAllText($Path, $content, (New-Object System.Text.UTF8Encoding($hasBom)))

    Write-Host "Version bumped: $old -> $new ($Path)"
}

$projects = @(Get-ChildItem -LiteralPath $Root -Filter '*.csproj' -Recurse -File |
    Where-Object { $_.FullName -notmatch $excluded } |
    Where-Object { Select-String -LiteralPath $_.FullName -Pattern '<PackageId>' -Quiet })

if ($projects.Count -eq 0) {
    throw "No project with <PackageId> found below $Root."
}

Write-Host '[1/4] Cleaning build and test artifacts'

if (Get-Command cstidy -ErrorAction SilentlyContinue) {
    Push-Location -LiteralPath $Root

    try {
        cstidy clean . --yes

        if ($LASTEXITCODE -ne 0) {
            Write-Warning 'Cleanup reported errors, continuing.'
        }
    }
    finally {
        Pop-Location
    }
}
else {
    Write-Warning 'cstidy not found on PATH, skipping cleanup.'
}

# Clean the publish folder
if (Test-Path -LiteralPath $publishDir) {
    Get-ChildItem -LiteralPath $publishDir -Force | Remove-Item -Recurse -Force
}
else {
    New-Item -ItemType Directory -Path $publishDir | Out-Null
}

Write-Host '[2/4] Building and packing (Release)'

# Build (Release) and pack; GeneratePackageOnBuild is switched off to avoid packing twice
foreach ($project in $projects) {
    Write-Host "Packing $($project.Name) ..."

    dotnet pack $project.FullName -c Release -o $publishDir -p:GeneratePackageOnBuild=false

    if ($LASTEXITCODE -ne 0) {
        throw "dotnet pack failed for $($project.Name)."
    }
}

$packages = @(Get-ChildItem -LiteralPath $publishDir -File)

if ($packages.Count -eq 0) {
    throw "No packages were created in $publishDir."
}

Write-Host ''
Write-Host 'Created packages:'
$packages | ForEach-Object { Write-Host "  $($_.Name)" }

if ($NoOpen) {
    Write-Host '[3/4] Opening upload page skipped'
}
else {
    Write-Host '[3/4] Opening _Publish and nuget.org upload page'

    Invoke-Item -LiteralPath $publishDir
    Start-Process $uploadUrl
}

if ($NoBump) {
    Write-Host '[4/4] Version bump skipped'
}
else {
    Write-Host '[4/4] Bumping minor version'

    $versionFiles = @($projects |
        ForEach-Object { Find-VersionFile -Project $_ } |
        Where-Object { $_ } |
        Select-Object -Unique)

    if ($versionFiles.Count -eq 0) {
        Write-Warning 'No <Version> found, nothing bumped.'
    }

    foreach ($versionFile in $versionFiles) {
        Update-MinorVersion -Path $versionFile
    }
}
