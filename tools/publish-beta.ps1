# publish-beta.ps1 (2026-08-05): builds the comment-stripped publish copy of the mod.
# Ruling: full mechanical strip for published builds ("i just don't want them to be
# obviously ai, and it's probably easier to just pull them out than find that voice").
# The repo keeps every comment; this emits publish\TerraFirma + a dated zip.
#
#   powershell -NoProfile -ExecutionPolicy Bypass -File tools\publish-beta.ps1 [-Mod TF1|TF2|Both] [-CheckOnly]
#
# -CheckOnly: strip, validate and compile-check, then test the manifest rewrites on a temp
# copy. No zip, and the staging folders (the upload sources) are left untouched.
#
# Two mods since the 2026-09-27 split (R02, 2026-09-29): the same pipeline runs per mod.
# TF2 compile-checks against the STRIPPED TF1 (the pair subscribers load), so any TF2 run
# prepares TF1's stripped copy first; -Mod TF2 skips only TF1's zip and staging. TF2 stages
# to Mods\TerraFirma2Beta as Cleo_TerraFirma2Beta, and its dependency on Cleo_TerraFirma is
# rewritten to Cleo_TerraFirmaBeta, the id the uploaded TF1 carries. Titles are never
# rewritten: the staged manifest ships to subscribers (the workshop page title comes from
# workshop.json instead, SteamWorkshopUploaderView), so a staging-only title would leak.
#
# Steps: copy tree -> strip .cs (real lexer: strings, verbatim strings, interpolation
# holes with nested strings all handled; line count preserved) -> scrub date tokens from
# the surviving .cs text (RULED 2026-08-05: the 7 date-bearing runtime strings scrub in
# the published copy, repo keeps them; generic yyyy-mm-dd removal + a hard guard so new
# ones cannot slip through) -> strip .xml (textual <!-- --> removal; legal because --
# cannot appear inside a well-formed XML comment) -> tidy blank lines -> strict-validate
# every XML -> compile-check the stripped copy with the repo csproj -> zip -> stage the
# stripped tree as the standing Mods\TerraFirmaBeta upload source (workshop plan
# 2026-08-06; beta manifest id, workshop.json/preview.png preserved). Any failure aborts
# before a zip is produced.

param(
    [ValidateSet("TF1", "TF2", "Both")]
    [string]$Mod = "Both",
    [switch]$CheckOnly,
    [string]$RepoRoot = (Split-Path -Parent $PSScriptRoot),
    [string]$QudLib = "C:\Program Files (x86)\Steam\steamapps\common\Caves of Qud\CoQ_Data\Managed"
)

$ErrorActionPreference = "Stop"
$pubRoot = Join-Path $RepoRoot "publish"
$modsRoot = Join-Path $env:USERPROFILE "AppData\LocalLow\Freehold Games\CavesOfQud\Mods"

# per-mod settings; DepRewrites maps a dev dependency id to the id its staged copy carries
$Mods = @{
    TF1 = @{ Src = "TerraFirma";  ZipName = "CoQ_TerraFirma_beta";  Stage = "TerraFirmaBeta";
             IdFrom = "Cleo_TerraFirma";  IdTo = "Cleo_TerraFirmaBeta";  DepRewrites = @{} }
    TF2 = @{ Src = "TerraFirma2"; ZipName = "CoQ_TerraFirma2_beta"; Stage = "TerraFirma2Beta";
             IdFrom = "Cleo_TerraFirma2"; IdTo = "Cleo_TerraFirma2Beta";
             DepRewrites = @{ "Cleo_TerraFirma" = "Cleo_TerraFirmaBeta" } }
}
foreach ($k in $Mods.Keys) {
    $p = Join-Path $RepoRoot $Mods[$k].Src
    if (-not (Test-Path $p)) { throw "source folder not found: $p" }
}

# ---------------------------------------------------------------- text rules (fail closed)
# Runs against the REPO source, not the stripped copy, so violations report real file/line
# numbers you can go fix. Rule 1 (no em dashes in shipped text) would otherwise survive the
# strip and ship; rule 2 ("--" in an XML comment) would abort the strict-validate step later
# with a parse error and no context. Added 2026-08-15 at the user's direction.
# Run as its OWN PROCESS on purpose: a &-invoked .ps1 does not set $LASTEXITCODE (it reflects
# the last NATIVE command), so the in-process form read a stale code and aborted a clean tree
# on its first real run. Failing closed was the right direction to be wrong in, but wrong.
$checkOutput = & powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot "check-text-rules.ps1") -RepoRoot $RepoRoot
$checkCode = $LASTEXITCODE
$checkOutput | ForEach-Object { Write-Host $_ }
if ($checkCode -ne 0) { throw "text-rule check FAILED -- publish aborted, nothing zipped" }

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

# ---------------------------------------------------------------- strip one mod
# copy tree -> strip .cs + date scrub + leak guard -> strip .xml + strict validate, into
# publish\<Src>.
function Strip-Mod($cfg) {
    $src = Join-Path $RepoRoot $cfg.Src
    $dst = Join-Path $pubRoot $cfg.Src
    if (Test-Path $dst) { Remove-Item -Recurse -Force $dst }
    New-Item -ItemType Directory -Force $dst | Out-Null
    Copy-Item -Recurse "$src\*" $dst

    $csCount = 0; $xmlCount = 0
    $dateToken = ' ?\b20\d{2}-\d{2}-\d{2}\b'  # optional leading space so "cut 2026-07-25)" tidies to "cut)"
    foreach ($f in Get-ChildItem $dst -Recurse -Filter *.cs) {
        $raw = [System.IO.File]::ReadAllText($f.FullName)
        $strippedText = [TfCommentStripper]::Strip($raw)
        $tidy = Tidy-Lines ($raw -split "\r?\n") ($strippedText -split "\r?\n")
        # date scrub: comments are gone, so any surviving date lives in a runtime string
        $scrubbed = New-Object System.Collections.Generic.List[string]
        foreach ($line in $tidy) { $scrubbed.Add(($line -replace $dateToken, '')) }
        [System.IO.File]::WriteAllLines($f.FullName, $scrubbed, (New-Object System.Text.UTF8Encoding $true))
        $csCount++
    }

    # leak guard: a date surviving the scrub means the pattern missed something -- abort
    foreach ($f in Get-ChildItem $dst -Recurse -Filter *.cs) {
        $leak = Select-String -Path $f.FullName -Pattern '\b20\d{2}-\d{2}-\d{2}\b'
        if ($leak) {
            $leak | ForEach-Object { Write-Host $_ }
            throw "date token survived the scrub in $($f.Name) -- publish aborted"
        }
    }

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
    Write-Host "$($cfg.Src): $csCount .cs stripped, $xmlCount .xml stripped + validated"
}

# ---------------------------------------------------------------- compile check
# Mirrors the repo layout so both csproj files resolve unchanged: TerraFirma.csproj beside
# TerraFirma\, and build\TerraFirma2\TerraFirma2.csproj reaching ..\..\TerraFirma2 and
# ..\..\TerraFirma.csproj. TF2 therefore builds against the stripped TF1.
function Compile-Check([bool]$withTF2) {
    $buildDir = Join-Path $pubRoot "_compilecheck"
    if (Test-Path $buildDir) { Remove-Item -Recurse -Force $buildDir }
    New-Item -ItemType Directory -Force $buildDir | Out-Null
    Copy-Item (Join-Path $RepoRoot "TerraFirma.csproj") $buildDir
    Copy-Item -Recurse (Join-Path $pubRoot "TerraFirma") (Join-Path $buildDir "TerraFirma")
    $projects = @("TerraFirma.csproj")
    if ($withTF2) {
        $tf2Proj = Join-Path $buildDir "build\TerraFirma2"
        New-Item -ItemType Directory -Force $tf2Proj | Out-Null
        Copy-Item (Join-Path $RepoRoot "build\TerraFirma2\TerraFirma2.csproj") $tf2Proj
        Copy-Item -Recurse (Join-Path $pubRoot "TerraFirma2") (Join-Path $buildDir "TerraFirma2")
        $projects += "build\TerraFirma2\TerraFirma2.csproj"
    }
    Push-Location $buildDir
    try {
        foreach ($proj in $projects) {
            $build = & dotnet build $proj -p:QudLibPath="$QudLib" 2>&1
            $errors = $build | Select-String -Pattern "error CS\d|error MSB\d"
            if ($LASTEXITCODE -ne 0 -or $errors) {
                $errors | ForEach-Object { Write-Host $_ }
                throw "stripped copy of $proj FAILED to compile -- publish aborted, nothing zipped"
            }
            Write-Host "compile check green: $proj"
        }
    }
    finally { Pop-Location }
    Remove-Item -Recurse -Force $buildDir
}

# ---------------------------------------------------------------- zip + stage one mod
# (2026-08-06, workshop plan): the stripped tree also lands as a STANDING second mod with
# its manifest id rewritten -- two local mods cannot share an id (ModManager.RegisterMod
# skips the duplicate), and this one exists solely as the in-game uploader's source. It
# stays unapproved in the mod manager, so it never loads content. workshop.json (the item
# identity, written by the game's first upload) and preview.png survive refreshes.
function Release-Mod($cfg) {
    $dst = Join-Path $pubRoot $cfg.Src
    $stamp = Get-Date -Format "yyyy-MM-dd"
    $zip = Join-Path $pubRoot "$($cfg.ZipName)_$stamp.zip"
    if (Test-Path $zip) { Remove-Item -Force $zip }
    Compress-Archive -Path $dst -DestinationPath $zip

    $stage = Join-Path $modsRoot $cfg.Stage
    $preserve = @{}
    foreach ($p in @("workshop.json", "preview.png")) {
        $pp = Join-Path $stage $p
        if (Test-Path $pp) { $preserve[$p] = [System.IO.File]::ReadAllBytes($pp) }
    }
    if (Test-Path $stage) { Remove-Item -Recurse -Force $stage }
    New-Item -ItemType Directory -Force $stage | Out-Null
    Copy-Item -Recurse "$dst\*" $stage

    Rewrite-Manifest (Join-Path $stage "manifest.json") $cfg
    foreach ($k in $preserve.Keys) { [System.IO.File]::WriteAllBytes((Join-Path $stage $k), $preserve[$k]) }
    $workshopNote = if ($preserve.ContainsKey("workshop.json")) { "workshop.json preserved" } else { "no workshop.json yet (the game's first upload creates it)" }
    Write-Host "zip:    $zip"
    Write-Host "staged: $stage (manifest id $($cfg.IdTo), $workshopNote)"
}

# staged id + dependency rewrites, failing closed
function Rewrite-Manifest([string]$manifestPath, $cfg) {
    $manifestText = [System.IO.File]::ReadAllText($manifestPath)
    $manifestText = $manifestText -replace ('"id"\s*:\s*"' + [regex]::Escape($cfg.IdFrom) + '"'), ('"id": "' + $cfg.IdTo + '"')
    foreach ($dep in $cfg.DepRewrites.Keys) {
        # only a dependency KEY matches: the exact quoted id followed by a colon
        $manifestText = $manifestText -replace ('"' + [regex]::Escape($dep) + '"(\s*:)'), ('"' + $cfg.DepRewrites[$dep] + '"$1')
    }
    [System.IO.File]::WriteAllText($manifestPath, $manifestText, (New-Object System.Text.UTF8Encoding $false))

    # fail closed: the staged manifest must carry the staged id and every rewritten dependency
    $check = Get-Content $manifestPath -Raw | ConvertFrom-Json
    if ($check.id -ne $cfg.IdTo) { throw "manifest id rewrite failed for $($cfg.Src) -- staging aborted" }
    foreach ($dep in $cfg.DepRewrites.Keys) {
        $deps = $check.dependencies
        if ($null -eq $deps -or $null -eq $deps.($cfg.DepRewrites[$dep]) -or $null -ne $deps.$dep) {
            throw "dependency rewrite $dep -> $($cfg.DepRewrites[$dep]) failed for $($cfg.Src) -- staging aborted"
        }
    }
    Write-Host "manifest ok: $($cfg.Src) -> id $($check.id)$(if ($check.dependencies) { ', deps ' + (($check.dependencies.PSObject.Properties | % { $_.Name }) -join ',') })"
}

# ---------------------------------------------------------------- run
$wantTF1 = $Mod -ne "TF2"
$wantTF2 = $Mod -ne "TF1"
Strip-Mod $Mods.TF1                     # always: TF2 compile-checks against it
if ($wantTF2) { Strip-Mod $Mods.TF2 }
Compile-Check $wantTF2
if ($CheckOnly) {
    foreach ($k in @("TF1", "TF2")) {
        if (($k -eq "TF1" -and -not $wantTF1) -or ($k -eq "TF2" -and -not $wantTF2)) { continue }
        $tmp = Join-Path $pubRoot "_manifestcheck_$k.json"
        Copy-Item (Join-Path $pubRoot "$($Mods[$k].Src)\manifest.json") $tmp -Force
        Rewrite-Manifest $tmp $Mods[$k]
        Remove-Item -Force $tmp
    }
    Write-Host "check OK ($Mod): nothing zipped, nothing staged"
    return
}
if ($wantTF1) { Release-Mod $Mods.TF1 }
if ($wantTF2) { Release-Mod $Mods.TF2 }
Write-Host "publish OK ($Mod)"
