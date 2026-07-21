[CmdletBinding()]
param(
    [string]$ChineseResourcePath,
    [string]$EnglishResourcePath
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($ChineseResourcePath)) {
    $ChineseResourcePath = Join-Path $PSScriptRoot "..\Strings\zh-Hans\Resources.resw"
}

if ([string]::IsNullOrWhiteSpace($EnglishResourcePath)) {
    $EnglishResourcePath = Join-Path $PSScriptRoot "..\Strings\en-US\Resources.resw"
}

function Get-ResourceValues {
    param([string]$Path)

    $document = [System.Xml.XmlDocument]::new()
    $document.Load($Path)
    $values = @{}
    foreach ($entry in @($document.root.data)) {
        if ($values.ContainsKey($entry.name)) {
            throw "Duplicate resource key '$($entry.name)' in '$Path'."
        }

        $values[$entry.name] = [string]$entry.value
    }

    return $values
}

function Get-FormatPlaceholders {
    param([string]$Value)

    [string[]]$placeholders = @([regex]::Matches($Value, "(?<!\{)\{\d+(?:,[^}:]+)?(?::[^}]*)?\}(?!\})") |
        ForEach-Object Value |
        Sort-Object -Unique)
    return ,$placeholders
}

$chinese = Get-ResourceValues $ChineseResourcePath
$english = Get-ResourceValues $EnglishResourcePath

$missingFromEnglish = @($chinese.Keys | Where-Object { -not $english.ContainsKey($_) } | Sort-Object)
$missingFromChinese = @($english.Keys | Where-Object { -not $chinese.ContainsKey($_) } | Sort-Object)
if ($missingFromEnglish.Count -gt 0 -or $missingFromChinese.Count -gt 0) {
    $message = @()
    if ($missingFromEnglish.Count -gt 0) {
        $message += "Missing from English: $($missingFromEnglish -join ', ')"
    }

    if ($missingFromChinese.Count -gt 0) {
        $message += "Missing from Chinese: $($missingFromChinese -join ', ')"
    }

    throw ($message -join [Environment]::NewLine)
}

foreach ($key in $chinese.Keys) {
    $chinesePlaceholders = Get-FormatPlaceholders $chinese[$key]
    $englishPlaceholders = Get-FormatPlaceholders $english[$key]
    if ((Compare-Object $chinesePlaceholders $englishPlaceholders -SyncWindow 0)) {
        throw "Format placeholders differ for resource key '$key'."
    }
}

Write-Host "Localization resources are valid: $($chinese.Count) keys in zh-Hans and en-US."
