# publish-beta.ps1 (2026-08-05): builds the comment-stripped publish copy of the mod.
# Ruling: full mechanical strip for published builds ("i just don't want them to be
# obviously ai, and it's probably easier to just pull them out than find that voice").
# The repo keeps every comment; this emits publish\TerraFirma + a dated zip.
#
#   powershell -NoProfile -ExecutionPolicy Bypass -File tools\publish-beta.ps1
#
# Steps: copy tree -> strip .cs (real lexer: strings, verbatim strings, interpolation
# holes with nested strings all handled; line count preserved) -> strip .xml (textual
# <!-- --> removal; legal because -- cannot appear inside a well-formed XML comment)
# -> tidy blank lines -> strict-validate every XML -> compile-check the stripped copy
# with the repo csproj -> zip. Any failure aborts before a zip is produced.

param(
    [string]$RepoRoot = (Split-Path -Parent $PSScriptRoot),
    [string]$QudLib = "C:\Program Files (x86)\Steam\steamapps\common\Caves of Qud\CoQ_Data\Managed"
)

$ErrorActionPreference = "Stop"
$src = Join-Path $RepoRoot "TerraFirma"
$pubRoot = Join-Path $RepoRoot "publish"
$dst = Join-Path $pubRoot "TerraFirma"

if (-not (Test-Path $src)) { throw "source folder not found: $src" }

# ---------------------------------------------------------------- C# comment lexer
Add-Type -TypeDefinition @'
using System.Text;

public static class TfCommentStripper
{
    // Strips // and /* */ comments while preserving every string form C# 9 offers:
    // "...", @"...", $"...", $@"/@$"...", chars, and interpolation holes containing
    // nested strings (lexed recursively as code). Newlines inside block comments are
    // kept so the output has the same line count as the input (the tidy pass keys
    // off line indices).
    public static string Strip(string s)
    {
        StringBuilder o = new StringBuilder(s.Length);
        int i = 0;
        LexCode(s, ref i, o, false);
        return o.ToString();
    }

    private static void LexCode(string s, ref int i, StringBuilder o, bool stopAtCloseBrace)
    {
        int depth = 0; // nested { } blocks inside an interpolation hole (initializers, lambdas)
        bool lineHasCode = false;
        while (i < s.Length)
        {
            char c = s[i];
            if (c == '\n') { lineHasCode = false; o.Append(c); i++; continue; }
            if (c == '#' && !lineHasCode)
            {
                // preprocessor directive: free text, not code (a #region name containing an
                // apostrophe once put the lexer into char-literal mode for 100 lines).
                // #region/#endregion lines strip entirely; anything else passes verbatim.
                int start = i;
                while (i < s.Length && s[i] != '\n') i++;
                string directive = s.Substring(start, i - start).TrimEnd();
                if (!directive.StartsWith("#region") && !directive.StartsWith("#endregion"))
                    o.Append(directive);
                continue; // the newline is emitted by the loop above
            }
            if (!char.IsWhiteSpace(c))
                lineHasCode = true;
            if (stopAtCloseBrace && c == '{') { depth++; o.Append(c); i++; continue; }
            if (stopAtCloseBrace && c == '}')
            {
                if (depth == 0)
                    return; // the hole's own closer -- caller consumes it
                depth--; o.Append(c); i++; continue;
            }
            if (c == '/' && i + 1 < s.Length && s[i + 1] == '/')
            {
                while (i < s.Length && s[i] != '\n') i++;
                continue; // newline itself is emitted by the loop
            }
            if (c == '/' && i + 1 < s.Length && s[i + 1] == '*')
            {
                i += 2;
                while (i < s.Length && !(s[i] == '*' && i + 1 < s.Length && s[i + 1] == '/'))
                {
                    if (s[i] == '\n') o.Append('\n'); // preserve line count
                    i++;
                }
                i += 2;
                continue;
            }
            if (c == '\'')
            {
                o.Append(c); i++;
                while (i < s.Length && s[i] != '\'')
                {
                    if (s[i] == '\\' && i + 1 < s.Length) { o.Append(s[i]); i++; }
                    o.Append(s[i]); i++;
                }
                if (i < s.Length) { o.Append(s[i]); i++; }
                continue;
            }
            bool interp = false, verbatim = false; int q = i;
            if (c == '$' && i + 1 < s.Length && s[i + 1] == '"') { interp = true; q = i + 1; }
            else if (c == '@' && i + 1 < s.Length && s[i + 1] == '"') { verbatim = true; q = i + 1; }
            else if (c == '$' && i + 2 < s.Length && s[i + 1] == '@' && s[i + 2] == '"') { interp = true; verbatim = true; q = i + 2; }
            else if (c == '@' && i + 2 < s.Length && s[i + 1] == '$' && s[i + 2] == '"') { interp = true; verbatim = true; q = i + 2; }
            if (c == '"' || ((interp || verbatim) && q > i))
            {
                if (c != '"') { for (int k = i; k <= q; k++) o.Append(s[k]); i = q + 1; }
                else { o.Append(c); i++; }
                LexString(s, ref i, o, interp, verbatim);
                continue;
            }
            o.Append(c); i++;
        }
    }

    private static void LexString(string s, ref int i, StringBuilder o, bool interp, bool verbatim)
    {
        while (i < s.Length)
        {
            char c = s[i];
            if (verbatim && c == '"' && i + 1 < s.Length && s[i + 1] == '"') { o.Append('"'); o.Append('"'); i += 2; continue; }
            if (!verbatim && c == '\\' && i + 1 < s.Length) { o.Append(c); o.Append(s[i + 1]); i += 2; continue; }
            if (c == '"') { o.Append(c); i++; return; }
            if (interp && c == '{' && i + 1 < s.Length && s[i + 1] == '{') { o.Append('{'); o.Append('{'); i += 2; continue; }
            if (interp && c == '}' && i + 1 < s.Length && s[i + 1] == '}') { o.Append('}'); o.Append('}'); i += 2; continue; }
            if (interp && c == '{')
            {
                o.Append(c); i++;
                LexCode(s, ref i, o, true); // the hole is code: nested strings, ternaries, all of it
                if (i < s.Length && s[i] == '}') { o.Append('}'); i++; }
                continue;
            }
            o.Append(c); i++;
        }
    }
}
'@

function Tidy-Lines([string[]]$original, [string[]]$stripped) {
    # drop lines a strip emptied (comment-only lines); collapse blank runs the drop creates
    $keep = New-Object System.Collections.Generic.List[string]
    for ($i = 0; $i -lt $stripped.Count; $i++) {
        $wasBlank = ($i -ge $original.Count) -or ($original[$i].Trim() -eq "")
        $isBlank = $stripped[$i].Trim() -eq ""
        if ($isBlank -and -not $wasBlank) { continue }
        $line = $stripped[$i].TrimEnd()
        if ($line -eq "" -and $keep.Count -gt 0 -and $keep[$keep.Count - 1] -eq "") { continue }
        $keep.Add($line)
    }
    while ($keep.Count -gt 0 -and $keep[$keep.Count - 1] -eq "") { $keep.RemoveAt($keep.Count - 1) }
    return $keep
}

# ---------------------------------------------------------------- copy tree
if (Test-Path $dst) { Remove-Item -Recurse -Force $dst }
New-Item -ItemType Directory -Force $dst | Out-Null
Copy-Item -Recurse "$src\*" $dst

$csCount = 0; $xmlCount = 0

# ---------------------------------------------------------------- strip .cs
foreach ($f in Get-ChildItem $dst -Recurse -Filter *.cs) {
    $raw = [System.IO.File]::ReadAllText($f.FullName)
    $strippedText = [TfCommentStripper]::Strip($raw)
    $tidy = Tidy-Lines ($raw -split "\r?\n") ($strippedText -split "\r?\n")
    [System.IO.File]::WriteAllLines($f.FullName, $tidy, (New-Object System.Text.UTF8Encoding $true))
    $csCount++
}

# ---------------------------------------------------------------- strip .xml
foreach ($f in Get-ChildItem $dst -Recurse -Filter *.xml) {
    $raw = [System.IO.File]::ReadAllText($f.FullName)
    $strippedText = [regex]::Replace($raw, '<!--[\s\S]*?-->', {
        param($m)
        # preserve line count for the tidy pass
        ("`n" * ([regex]::Matches($m.Value, "`n").Count))
    })
    $tidy = Tidy-Lines ($raw -split "\r?\n") ($strippedText -split "\r?\n")
    [System.IO.File]::WriteAllLines($f.FullName, $tidy, (New-Object System.Text.UTF8Encoding $true))
    $x = New-Object System.Xml.XmlDocument
    $x.Load($f.FullName)   # strict-validate or abort
    $xmlCount++
}

# ---------------------------------------------------------------- compile check
$buildDir = Join-Path $pubRoot "_compilecheck"
if (Test-Path $buildDir) { Remove-Item -Recurse -Force $buildDir }
New-Item -ItemType Directory -Force $buildDir | Out-Null
Copy-Item (Join-Path $RepoRoot "TerraFirma.csproj") $buildDir
Copy-Item -Recurse $dst (Join-Path $buildDir "TerraFirma")
Push-Location $buildDir
try {
    $build = & dotnet build TerraFirma.csproj -p:QudLibPath="$QudLib" 2>&1
    $errors = $build | Select-String -Pattern "error CS\d|error MSB\d"
    if ($LASTEXITCODE -ne 0 -or $errors) {
        $errors | ForEach-Object { Write-Host $_ }
        throw "stripped copy FAILED to compile -- publish aborted, nothing zipped"
    }
}
finally { Pop-Location }
Remove-Item -Recurse -Force $buildDir

# ---------------------------------------------------------------- zip
$stamp = Get-Date -Format "yyyy-MM-dd"
$zip = Join-Path $pubRoot "CoQ_TerraFirma_beta_$stamp.zip"
if (Test-Path $zip) { Remove-Item -Force $zip }
Compress-Archive -Path $dst -DestinationPath $zip

Write-Host "publish OK: $csCount .cs stripped, $xmlCount .xml stripped + validated, compile check green"
Write-Host "folder: $dst"
Write-Host "zip:    $zip"
