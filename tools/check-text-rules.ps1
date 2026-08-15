# check-text-rules.ps1 (2026-08-15): fails closed on the mod's two punctuation rules.
# Hardened at the user's direction ("i feel like we should harden that ruling") after the
# XML-comment curse struck for the tenth time. The two rules look alike and are NOT the same:
#
#   RULE 1  no em dashes in SHIPPED TEXT (anything a player reads). Code comments, commit
#           messages and tracker prose are exempt by ruling; the publish strip deletes
#           comments from released builds anyway.
#   RULE 2  no "--" inside an XML comment. Not style, syntax: XML forbids it and the file
#           will not parse. Strict validation already catches this at load time; this check
#           catches it earlier and points at the exact line.
#
#   powershell -NoProfile -ExecutionPolicy Bypass -File tools\check-text-rules.ps1
#
# Exit 0 = clean. Exit 1 = at least one violation, printed with file and line.
# Wired into publish-beta.ps1 so a release cannot ship one.

param(
    [string]$RepoRoot = (Split-Path -Parent $PSScriptRoot)
)

$ErrorActionPreference = "Stop"
$src = Join-Path $RepoRoot "TerraFirma"
if (-not (Test-Path $src)) { throw "mod folder not found: $src" }

$emDash = [char]0x2014
$violations = 0

function Report([string]$rule, [string]$file, [int]$line, [string]$text) {
    $script:violations++
    $rel = $file.Replace($RepoRoot, "").TrimStart("\")
    $snippet = $text.Trim()
    if ($snippet.Length -gt 110) { $snippet = $snippet.Substring(0, 110) + "..." }
    Write-Host "$rule  ${rel}:${line}"
    Write-Host "    $snippet"
}

# ---------------------------------------------------------------- RULE 2: -- in XML comments
# Comment bodies are extracted whole (they span lines), then the offending line is located by
# counting newlines, so the report points at the line a human has to go fix.
foreach ($f in Get-ChildItem $src -Recurse -Filter *.xml) {
    $raw = [System.IO.File]::ReadAllText($f.FullName)
    foreach ($m in [regex]::Matches($raw, '<!--([\s\S]*?)(?:-->|$)')) {
        $body = $m.Groups[1].Value
        if ($body -notmatch '--') { continue }
        $bodyStart = $m.Groups[1].Index
        foreach ($hit in [regex]::Matches($body, '--')) {
            $abs = $bodyStart + $hit.Index
            $line = ([regex]::Matches($raw.Substring(0, $abs), "`n")).Count + 1
            $lineText = ($raw -split "\r?\n")[$line - 1]
            Report "RULE 2  '--' inside an XML comment (file will not parse)" $f.FullName $line $lineText
        }
    }
}

# ---------------------------------------------------------------- RULE 1: em dash in shipped text
# XML: only attributes a player can read. Deliberately narrow -- an em dash in a comment is fine.
$playerFacingAttrs = @("Short", "Long", "DisplayName", "Description", "Text", "Value")
foreach ($f in Get-ChildItem $src -Recurse -Filter *.xml) {
    $lines = [System.IO.File]::ReadAllLines($f.FullName)
    $inComment = $false
    for ($i = 0; $i -lt $lines.Count; $i++) {
        $line = $lines[$i]
        # crude but sufficient comment tracking: we only need to EXEMPT comment lines
        if ($inComment) {
            if ($line -match '-->') { $inComment = $false }
            continue
        }
        if ($line -match '<!--' -and $line -notmatch '-->') { $inComment = $true; continue }
        $stripped = [regex]::Replace($line, '<!--.*?-->', '')
        if ($stripped.IndexOf($emDash) -lt 0) { continue }
        foreach ($attr in $playerFacingAttrs) {
            foreach ($m in [regex]::Matches($stripped, "$attr=`"([^`"]*)`"")) {
                if ($m.Groups[1].Value.IndexOf($emDash) -ge 0) {
                    Report "RULE 1  em dash in shipped text ($attr)" $f.FullName ($i + 1) $line
                }
            }
        }
    }
}

# C#: string literals only. A comment may hold as many em dashes as it likes.
foreach ($f in Get-ChildItem $src -Recurse -Filter *.cs) {
    $lines = [System.IO.File]::ReadAllLines($f.FullName)
    $inBlockComment = $false
    for ($i = 0; $i -lt $lines.Count; $i++) {
        $line = $lines[$i]
        if ($inBlockComment) {
            if ($line -match '\*/') { $inBlockComment = $false }
            continue
        }
        if ($line -match '/\*' -and $line -notmatch '\*/') { $inBlockComment = $true; continue }
        $code = [regex]::Replace($line, '//.*$', '')
        if ($code.IndexOf($emDash) -lt 0) { continue }
        foreach ($m in [regex]::Matches($code, '"(?:[^"\\]|\\.)*"')) {
            if ($m.Value.IndexOf($emDash) -ge 0) {
                Report "RULE 1  em dash in a runtime string" $f.FullName ($i + 1) $line
            }
        }
    }
}

# manifest.json: the description is the storefront line, all of it player-facing.
$manifest = Join-Path $src "manifest.json"
if (Test-Path $manifest) {
    $lines = [System.IO.File]::ReadAllLines($manifest)
    for ($i = 0; $i -lt $lines.Count; $i++) {
        if ($lines[$i].IndexOf($emDash) -ge 0) {
            Report "RULE 1  em dash in the manifest" $manifest ($i + 1) $lines[$i]
        }
    }
}

if ($violations -gt 0) {
    Write-Host ""
    Write-Host "text-rule check FAILED: $violations violation(s)."
    Write-Host "Rule 1 fix: rewrite the sentence, or use a comma / colon / semicolon."
    Write-Host "Rule 2 fix: inside XML comments write 'i.e.', a semicolon, or a real em dash character."
    exit 1
}

Write-Host "text-rule check OK: no em dashes in shipped text, no '--' inside any XML comment"
