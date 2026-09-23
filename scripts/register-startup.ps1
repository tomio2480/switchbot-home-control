<#
.SYNOPSIS
    Registers SwitchBotHomeControl.exe to run at Windows sign-in.

.DESCRIPTION
    Creates a shortcut in the current user's Startup folder
    (shell:startup). No administrator rights or registry changes are needed.
    Use -Unregister to remove the shortcut.

.PARAMETER ExePath
    Path to SwitchBotHomeControl.exe. Defaults to the release build of the
    repository that contains this script (the output of build.bat).

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File scripts\register-startup.ps1

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File scripts\register-startup.ps1 -Unregister
#>
[CmdletBinding()]
param(
    [string]$ExePath,
    [switch]$Unregister
)

$ErrorActionPreference = 'Stop'

$shortcutName = 'SwitchBot Home Control.lnk'
$startupDir = [Environment]::GetFolderPath('Startup')
$shortcutPath = Join-Path $startupDir $shortcutName

if ($Unregister) {
    if (Test-Path -LiteralPath $shortcutPath) {
        Remove-Item -LiteralPath $shortcutPath
        Write-Output "Removed: $shortcutPath"
    } else {
        Write-Output "Not registered: $shortcutPath"
    }
    return
}

if (-not $ExePath) {
    $repoRoot = Split-Path -Parent $PSScriptRoot
    $ExePath = Join-Path $repoRoot 'bin\Release\net8.0-windows\win-x64\publish\SwitchBotHomeControl.exe'
}
$ExePath = [System.IO.Path]::GetFullPath($ExePath)

if (-not (Test-Path -LiteralPath $ExePath -PathType Leaf)) {
    # The shortcut still works once build.bat produces the exe at this path
    Write-Warning "Executable not found yet: $ExePath (run build.bat to create it)"
}

$shell = New-Object -ComObject WScript.Shell
$shortcut = $shell.CreateShortcut($shortcutPath)
$shortcut.TargetPath = $ExePath
$shortcut.WorkingDirectory = Split-Path -Parent $ExePath
$shortcut.Description = 'SwitchBot Home Control'
$shortcut.Save()

Write-Output "Registered: $shortcutPath -> $ExePath"
