# Installs the mirra-porting package into a target Unity project by copying the
# package folder. The target project's Packages/manifest.json is never modified.
#
# Example:
#   powershell -ExecutionPolicy Bypass -File tools\install.ps1 -Project D:\ports\game
#
# Prefer running this with the target project's Unity editor closed.

param([Parameter(Mandatory = $true)][string]$Project)

$ErrorActionPreference = 'Stop'

$packageName = 'com.luqmus.mirra-porting'
$excludeLine = "Packages/$packageName/"

function Fail([string]$message)
{
    Write-Host "ERROR: $message"
    exit 1
}

function Normalize([string]$path)
{
    return $path.TrimEnd('\', '/')
}

# --- checks, all before any change ---

$source = Join-Path $PSScriptRoot "..\Packages\$packageName"
if (-not (Test-Path -LiteralPath $source -PathType Container))
{
    Fail "Package source not found: $source"
}
$source = (Resolve-Path -LiteralPath $source).Path

if (-not (Test-Path -LiteralPath $Project -PathType Container))
{
    Fail "Project folder not found: $Project"
}
$projectPath = (Resolve-Path -LiteralPath $Project).Path

$manifest = Join-Path $projectPath 'Packages\manifest.json'
if (-not (Test-Path -LiteralPath $manifest -PathType Leaf))
{
    Fail "Not a Unity project, Packages/manifest.json is missing: $projectPath"
}

$target = Join-Path $projectPath "Packages\$packageName"
if ([string]::Equals((Normalize $target), (Normalize $source), 'OrdinalIgnoreCase'))
{
    Fail "Target is the package source itself: $target"
}

# --- copy ---

if (Test-Path -LiteralPath $target)
{
    Remove-Item -LiteralPath $target -Recurse -Force
    Write-Host "Removed the previous copy at $target"
}

Copy-Item -LiteralPath $source -Destination $target -Recurse -Force

# --- local git ignore ---

$excludeStatus = ''
$gitDir = Join-Path $projectPath '.git'
if (-not (Test-Path -LiteralPath $gitDir))
{
    $excludeStatus = "WARNING: no .git in the project, .git/info/exclude was not touched"
}
elseif (-not (Test-Path -LiteralPath $gitDir -PathType Container))
{
    $excludeStatus = "WARNING: .git is a file (worktree or submodule), .git/info/exclude was not touched"
}
else
{
    $infoDir = Join-Path $gitDir 'info'
    if (-not (Test-Path -LiteralPath $infoDir -PathType Container))
    {
        New-Item -ItemType Directory -Path $infoDir | Out-Null
    }

    $excludeFile = Join-Path $infoDir 'exclude'
    $utf8NoBom = New-Object System.Text.UTF8Encoding($false)

    $text = ''
    if (Test-Path -LiteralPath $excludeFile -PathType Leaf)
    {
        $text = [IO.File]::ReadAllText($excludeFile)
    }

    $alreadyListed = $false
    foreach ($line in ($text -split '\r?\n'))
    {
        if ($line.Trim() -eq $excludeLine)
        {
            $alreadyListed = $true
            break
        }
    }

    if ($alreadyListed)
    {
        $excludeStatus = "already listed in .git/info/exclude"
    }
    else
    {
        $prefix = ''
        if ($text.Length -gt 0 -and -not $text.EndsWith("`n"))
        {
            $prefix = "`n"
        }
        [IO.File]::AppendAllText($excludeFile, "$prefix$excludeLine`n", $utf8NoBom)
        $excludeStatus = "added '$excludeLine' to .git/info/exclude"
    }
}

# --- report ---

Write-Host "Installed the package:"
Write-Host "  from: $source"
Write-Host "  to:   $target"
Write-Host "  git:  $excludeStatus"
Write-Host "  Packages/manifest.json was not modified."
Write-Host "Switch to the Unity editor of the project so it imports the embedded package."
