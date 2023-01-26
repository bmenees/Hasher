# Use "get-help -detailed .\SetupHasher.ps1" to see parameter help. (https://stackoverflow.com/a/54652886/1882616)

<#
.SYNOPSIS
Sets up "Send To Hasher" shortcuts.
.DESCRIPTION
The SetupHasher.ps1 script can be run once to make sure Hasher is available as a target
in the Windows File Explorer's Send To menu. It's safe to re-run the script, but it often
only needs to be run once initially.
#>
param(
    [string[]]$algorithms = @('MD5', 'SHA1', 'SHA256', 'SHA512') # The hash algorithms to create shortcuts for. These should be HashAlgorithm-derived types from the System.Security.Cryptography namespace.
    ,[bool]$autoStart = $true # Whether Hasher should begin hashing automatically at startup. This defaults to $true.
    ,[switch]$uninstall # Whether Send To Hasher shortcuts should be installed uninstalled.
    ,[string]$hasherExePath = $null # Used to specify a custom path to Hasher.exe during development.
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"
$scriptPath = [IO.Path]::GetDirectoryName($MyInvocation.MyCommand.Definition)

function Main()
{
    $sendToPath = [Environment]::GetFolderPath([Environment+SpecialFolder]::SendTo)

    if ($uninstall)
    {
        DeleteShortcuts $sendToPath
    }
    else
    {
        SetupShortcuts $sendToPath
    }
}

function DeleteShortcuts([string]$sendToPath)
{
    foreach ($algorithm in $algorithms)
    {
        $link = Join-Path $sendToPath "Hasher ($algorithm).lnk"
        if (Test-Path $link)
        {
            Write-Host "Deleting $link."
            Remove-Item $link -Force
        }
    }
}

function SetupShortcuts([string]$sendToPath)
{
    if (!$hasherExePath)
    {
        $exeName = 'Hasher.exe'
        $hasherExePath = Join-Path $scriptPath $exeName
        if (!(Test-Path $hasherExePath))
        {
            throw "Unable to find $exeName."
        }
    }

    # Delete any existing shortcuts first since we always create new .lnk files and don't diff old vs new.
    DeleteShortcuts $sendToPath
    CreateShortcuts $sendToPath $hasherExePath
}

function CreateShortcuts([string]$sendToPath, [string]$hasherExePath)
{
    # https://stackoverflow.com/a/9701907/1882616
    $shell = New-Object -ComObject WScript.Shell
    foreach ($algorithm in $algorithms)
    {
        $linkPath = Join-Path $sendToPath "Hasher ($algorithm).lnk"
        Write-Host "Creating $linkPath"

        $shortcut = $shell.CreateShortcut($linkPath)
        $shortcut.TargetPath = "`"$hasherExePath`""

        $arguments = "/algorithm $algorithm"
        if ($autoStart)
        {
            $arguments += " /compareTo Clipboard /start"
        }

        # When invoked by the shell's Send To handler, an additional %L argument
        # (for the long file name) will also be passed at the end. https://superuser.com/a/473602/430448
        $shortcut.Arguments = $arguments
        $shortcut.Save()
    }
}

Main