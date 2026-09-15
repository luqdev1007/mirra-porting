# Removes the mirra-porting package from a target Unity project.
# The target project's Packages/manifest.json is never modified.
#
# Example:
#   powershell -ExecutionPolicy Bypass -File tools\uninstall.ps1 -Project D:\ports\game
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
    Fail "Refusing to delete the package source itself: $target"
}

if (-not (Test-Path -LiteralPath $target))
{
    Write-Host "The package is not installed in $projectPath, nothing to remove."
    exit 0
}

# --- remove ---

Remove-Item -LiteralPath $target -Recurse -Force
Write-Host "Removed $target"

# --- local git ignore ---

$gitDir = Join-Path $projectPath '.git'
$excludeFile = Join-Path $gitDir 'info\exclude'
if (Test-Path -LiteralPath $excludeFile -PathType Leaf)
{
    $utf8NoBom = New-Object System.Text.UTF8Encoding($false)

    $text = [IO.File]::ReadAllText($excludeFile)
    if ($text -match "`r`n")
    {
        $newline = "`r`n"
    }
    else
    {
        $newline = "`n"
    }
    $endedWithNewline = ($text.Length -eq 0) -or $text.EndsWith("`n")

    $lines = @($text -split '\r?\n')
    if ($lines.Count -gt 0 -and $lines[$lines.Count - 1] -eq '')
    {
        if ($lines.Count -eq 1)
        {
            $lines = @()
        }
        else
        {
            $lines = $lines[0..($lines.Count - 2)]
        }
    }

    $kept = @($lines | Where-Object { $_.Trim() -ne $excludeLine })
    if ($kept.Count -ne $lines.Count)
    {
        $out = ($kept -join $newline)
        if ($endedWithNewline -and $out.Length -gt 0)
        {
            $out += $newline
        }
        [IO.File]::WriteAllText($excludeFile, $out, $utf8NoBom)
        Write-Host "Removed '$excludeLine' from .git/info/exclude"
    }
    else
    {
        Write-Host "'$excludeLine' was not listed in .git/info/exclude"
    }
}
else
{
    Write-Host "WARNING: no .git/info/exclude in the project, nothing to clean up there"
}

# --- report ---

if (-not (Test-Path -LiteralPath $gitDir))
{
    Write-Host "WARNING: no .git in the project, cannot show git status"
}
elseif ($null -eq (Get-Command git -ErrorAction SilentlyContinue))
{
    Write-Host "WARNING: git was not found in PATH, cannot show git status"
}
else
{
    Write-Host "git status --short:"
    & git -C $projectPath status --short
}

Write-Host "Check Packages/packages-lock.json: it must not carry an entry for $packageName."
