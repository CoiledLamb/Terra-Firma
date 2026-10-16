# publish-beta.ps1 (2026-08-05): builds the comment-stripped publish copy of the mod.
# Ruling: full mechanical strip for published builds ("i just don't want them to be
# obviously ai, and it's probably easier to just pull them out than find that voice").
# The repo keeps every comment; this emits publish\TerraFirma + a dated zip.
#
#   powershell -NoProfile -ExecutionPolicy Bypass -File tools\publish-beta.ps1 [-Mod TF1|TF2|Both|Fused|Release] [-CheckOnly]
#
# -CheckOnly: strip, validate and compile-check, then test the manifest rewrites on a temp
# copy. No zip, and the staging folders (the upload sources) are left untouched.
#
# Two mods since the 2026-09-27 split (R02, 2026-09-29): the same pipeline runs per mod.
# TF2 compile-checks against the STRIPPED TF1 (the pair subscribers load), so any TF2 run
# prepares TF1's stripped copy first; -Mod TF2 skips only TF1's zip and staging. TF2 stages
# to Mods\TerraFirma2Beta as Cleo_TerraFirma2Beta, and its dependency on Cleo_TerraFirma is
# rewritten to Cleo_TerraFirmaBeta, the id the uploaded TF1 carries. The staged manifest
# ships to subscribers (the workshop page title comes from workshop.json instead,
# SteamWorkshopUploaderView), so the dev/staging distinction lives on the dev side: dev
# manifests are titled "... (dev)" and staging strips the suffix (2026-09-29).
#
# Player builds leave the dev tooling out (2026-09-30): each mod's Exclude list is deleted from
# the stripped copy, a missing listed path aborts (a rename must not ship a harness), and any
# WishCommand surviving the strip aborts. The repo and dev deploys keep every file. Since the
# scripts were sorted into folders, the list is each mod's whole Scripts\Dev folder, so a new
# wish or harness file is excluded by living there.
#
# -Mod Fused (2026-09-30): a single-folder testing build, Cleo_TerraFirmaTest, staged to
# Mods\TerraFirmaTest. It unions the two stripped trees. Same-named XML files are fused
# TF1-nodes-first, because the engine discards a merge whose target has not loaded yet
# (ObjectBlueprintLoader.ReadObjectsNode, "unknown blueprint, node discarded"); any other
# file present in both aborts. It compile-checks as one assembly and never touches the
# TF1/TF2 staging folders. Testers must not enable it alongside TF1 or TF2.
#
# -Mod Release (2026-09-30, user: the fused build ships as the update to the existing workshop
# item): the same fused tree, staged into Mods\TerraFirmaBeta under Cleo_TerraFirmaBeta, so its
# workshop.json and preview.png carry over. Front page is TF2's title and short in-game
# description; the full text lives in design\workshop\release-steam-description.txt and the
# staged workshop.json (Steam) and design\workshop\release-discord-announcement.md. It
# overwrites the TF1 staging that -Mod TF1/Both writes; use one or the other.
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
    [ValidateSet("TF1", "TF2", "Both", "Fused", "Release")]
    [string]$Mod = "Both",
    [switch]$CheckOnly,
    [string]$RepoRoot = (Split-Path -Parent $PSScriptRoot),
    [string]$QudLib = "C:\Program Files (x86)\Steam\steamapps\common\Caves of Qud\CoQ_Data\Managed"
)

$ErrorActionPreference = "Stop"
$pubRoot = Join-Path $RepoRoot "publish"
$modsRoot = Join-Path $env:USERPROFILE "AppData\LocalLow\Freehold Games\CavesOfQud\Mods"

# per-mod settings; DepRewrites maps a dev dependency id to the id its staged copy carries;
# Exclude lists dev-only paths (relative to the mod folder) left out of the player build
$Mods = @{
    TF1 = @{ Src = "TerraFirma";  ZipName = "CoQ_TerraFirma_beta";  Stage = "TerraFirmaBeta";
             IdFrom = "Cleo_TerraFirma";  IdTo = "Cleo_TerraFirmaBeta";  DepRewrites = @{};
             Exclude = @("Scripts\Dev") }
    TF2 = @{ Src = "TerraFirma2"; ZipName = "CoQ_TerraFirma2_beta"; Stage = "TerraFirma2Beta";
             IdFrom = "Cleo_TerraFirma2"; IdTo = "Cleo_TerraFirma2Beta";
             DepRewrites = @{ "Cleo_TerraFirma" = "Cleo_TerraFirmaBeta" };
             Exclude = @("Scripts\Dev") }
    # generated from the two stripped trees, no repo source of its own
    Fused = @{ Src = "TerraFirmaTest"; ZipName = "CoQ_TerraFirma_fusedtest"; Stage = "TerraFirmaTest";
             IdFrom = "Cleo_TerraFirmaTest"; IdTo = "Cleo_TerraFirmaTest"; DepRewrites = @{};
             Generated = $true; Title = "Terra Firma (fused test)" }
    Release = @{ Src = "TerraFirmaRelease"; ZipName = "CoQ_TerraFirma_release"; Stage = "TerraFirmaBeta";
             IdFrom = "Cleo_TerraFirmaBeta"; IdTo = "Cleo_TerraFirmaBeta"; DepRewrites = @{};
             Generated = $true; FrontPage = "TF2" }
}
foreach ($k in $Mods.Keys) {
    if ($Mods[$k].Generated) { continue }
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

# ---------------------------------------------------------------- verification switch
# Helpers.VERIFY_WATCH is the one switch for [TF-VERIFY] logging in both mods; dev keeps it
# true, player builds must not. A real run aborts on true, -CheckOnly warns and carries on so
# a dev tree can still be checked. An unreadable switch fails closed either way.
$helpersPath = Join-Path $RepoRoot "TerraFirma\Scripts\Core\_Helpers.cs"
$watchLine = if (Test-Path $helpersPath) { Select-String -Path $helpersPath -Pattern 'const bool VERIFY_WATCH\s*=\s*(true|false)\s*;' | Select-Object -First 1 }
if (-not $watchLine) { throw "could not read Helpers.VERIFY_WATCH in $helpersPath -- publish aborted" }
if ($watchLine.Matches[0].Groups[1].Value -eq "true") {
    if ($CheckOnly) { Write-Warning "VERIFY_WATCH is true in _Helpers.cs:$($watchLine.LineNumber) -- a real run would abort; set it false before staging" }
    else { throw "VERIFY_WATCH is true in _Helpers.cs:$($watchLine.LineNumber) -- set it false before staging; publish aborted, nothing zipped" }
}

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

    # dev tooling stays out of player builds; a listed file that is gone means the list is stale
    foreach ($rel in $cfg.Exclude) {
        $ex = Join-Path $dst $rel
        if (-not (Test-Path $ex)) { throw "exclude list names $rel but $($cfg.Src) has no such path -- publish aborted" }
        Remove-Item -Recurse -Force $ex
    }

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

    # wish guard: a dev wish outside the excluded files would ship to players
    $wish = Get-ChildItem $dst -Recurse -Filter *.cs | Select-String -Pattern 'WishCommand'
    if ($wish) {
        $wish | ForEach-Object { Write-Host $_ }
        throw "a wish survived the exclude list in $($cfg.Src) -- move it into the wish file or exclude its file; publish aborted"
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

# ---------------------------------------------------------------- fused testing build
# Union of the stripped TF1 and TF2 trees into publish\<Fused.Src>. Same-named XML fuses
# TF1-first (TF2 merges onto TF1 blueprints and the Stonecarver calling need their targets
# loaded); any other shared file aborts. Manifest: TF1's version, author and description under
# the fused id and title, no dependencies.
function Fuse-Mods($cfg) {
    $tf1 = Join-Path $pubRoot $Mods.TF1.Src
    $tf2 = Join-Path $pubRoot $Mods.TF2.Src
    $dst = Join-Path $pubRoot $cfg.Src
    if (Test-Path $dst) { Remove-Item -Recurse -Force $dst }
    New-Item -ItemType Directory -Force $dst | Out-Null
    Copy-Item -Recurse "$tf1\*" $dst
    $fused = 0; $added = 0
    foreach ($f in Get-ChildItem $tf2 -Recurse -File) {
        $rel = $f.FullName.Substring($tf2.Length).TrimStart('\')
        if ($rel -eq "manifest.json") { continue }
        $target = Join-Path $dst $rel
        if (-not (Test-Path $target)) {
            New-Item -ItemType Directory -Force (Split-Path $target) | Out-Null
            Copy-Item $f.FullName $target
            $added++
            continue
        }
        if ($f.Extension -ne ".xml") { throw "fused build: $rel exists in both mods and is not XML -- fuse aborted" }
        $a = New-Object System.Xml.XmlDocument; $a.Load($target)
        $b = New-Object System.Xml.XmlDocument; $b.Load($f.FullName)
        if ($a.DocumentElement.Name -ne $b.DocumentElement.Name) {
            throw "fused build: $rel roots differ ($($a.DocumentElement.Name) vs $($b.DocumentElement.Name)) -- fuse aborted"
        }
        foreach ($n in @($b.DocumentElement.ChildNodes)) { [void]$a.DocumentElement.AppendChild($a.ImportNode($n, $true)) }
        $a.Save($target)
        $check = New-Object System.Xml.XmlDocument; $check.Load($target)   # strict-validate or abort
        $fused++
    }
    $m = Get-Content (Join-Path $tf1 "manifest.json") -Raw | ConvertFrom-Json
    $title = $cfg.Title
    $description = $m.description
    if ($cfg.FrontPage -eq "TF2") {
        $front = Get-Content (Join-Path $tf2 "manifest.json") -Raw | ConvertFrom-Json
        $title = $front.title
        $description = $front.description
    }
    $manifest = [ordered]@{ id = $cfg.IdTo; title = $title; description = $description; version = $m.version; author = $m.author }
    [System.IO.File]::WriteAllText((Join-Path $dst "manifest.json"), ($manifest | ConvertTo-Json), (New-Object System.Text.UTF8Encoding $false))
    Write-Host "$($cfg.Src): fused $fused shared XML files TF1-first, added $added TF2 files"
}

# one assembly, as the game compiles a single mod: the repo csproj globs the fused tree
function Compile-Fused($cfg) {
    $buildDir = Join-Path $pubRoot "_compilecheck_fused"
    if (Test-Path $buildDir) { Remove-Item -Recurse -Force $buildDir }
    New-Item -ItemType Directory -Force $buildDir | Out-Null
    Copy-Item (Join-Path $RepoRoot "TerraFirma.csproj") $buildDir
    Copy-Item -Recurse (Join-Path $pubRoot $cfg.Src) (Join-Path $buildDir "TerraFirma")
    Push-Location $buildDir
    try {
        $build = & dotnet build "TerraFirma.csproj" -p:QudLibPath="$QudLib" 2>&1
        $errors = $build | Select-String -Pattern "error CS\d|error MSB\d"
        if ($LASTEXITCODE -ne 0 -or $errors) {
            $errors | ForEach-Object { Write-Host $_ }
            throw "fused build FAILED to compile as one assembly -- nothing zipped"
        }
        Write-Host "compile check green: fused $($cfg.Src)"
    }
    finally { Pop-Location }
    Remove-Item -Recurse -Force $buildDir
}

# ---------------------------------------------------------------- workshop cover
# (2026-10-14): Mods\TerraFirmaBeta's preview.png is rendered fresh from design\cover\cover.py on every run
# that stages there (TF1, Both, Release), so the cover's "CL - {version}" catalogue line follows
# TerraFirma\manifest.json instead of going stale. It renders before anything is zipped or staged; a
# missing Python, Pillow or numpy, or an unreadable manifest (cover.py --to fails closed), aborts the publish.
$coverStage = "TerraFirmaBeta"
function Render-Cover {
    $script = Join-Path $RepoRoot "design\cover\cover.py"
    if (-not (Test-Path $script)) { throw "cover script missing at $script -- publish aborted, nothing zipped" }
    $py = Get-Command python -ErrorAction SilentlyContinue; $pyArgs = @()
    if (-not $py) { $py = Get-Command py -ErrorAction SilentlyContinue; $pyArgs = @("-3") }
    if (-not $py) { throw "python not found (the workshop cover render needs it, with Pillow and numpy) -- publish aborted, nothing zipped" }
    $out = Join-Path $pubRoot "_cover_preview.png"
    if (Test-Path $out) { Remove-Item -Force $out }
    # python warnings arrive on stderr; under "Stop" Windows PowerShell would turn the first one into a terminating error
    $prev = $ErrorActionPreference; $ErrorActionPreference = "Continue"
    try { $log = & $py.Source @pyArgs $script --to $out final 2>&1; $code = $LASTEXITCODE }
    finally { $ErrorActionPreference = $prev }
    if ($code -ne 0 -or -not (Test-Path $out)) {
        $log | ForEach-Object { Write-Host $_ }
        throw "workshop cover render FAILED -- publish aborted, nothing zipped"
    }
    Write-Host "cover:  $(($log | Select-Object -Last 1))"
    return $out
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
    if ($cfg.Stage -eq $coverStage) { Copy-Item -Force $coverPng (Join-Path $stage "preview.png"); Write-Host "cover:  $stage\preview.png re-rendered" }
    $workshopNote = if ($preserve.ContainsKey("workshop.json")) { "workshop.json preserved" } else { "no workshop.json yet (the game's first upload creates it)" }
    Write-Host "zip:    $zip"
    Write-Host "staged: $stage (manifest id $($cfg.IdTo), $workshopNote)"
}

# staged id + dependency rewrites, failing closed
function Rewrite-Manifest([string]$manifestPath, $cfg) {
    $manifestText = [System.IO.File]::ReadAllText($manifestPath)
    $manifestText = $manifestText -replace ('"id"\s*:\s*"' + [regex]::Escape($cfg.IdFrom) + '"'), ('"id": "' + $cfg.IdTo + '"')
    $manifestText = $manifestText -replace '("title"\s*:\s*"[^"]*?) \(dev\)"', '$1"'
    foreach ($dep in $cfg.DepRewrites.Keys) {
        # only a dependency KEY matches: the exact quoted id followed by a colon
        $manifestText = $manifestText -replace ('"' + [regex]::Escape($dep) + '"(\s*:)'), ('"' + $cfg.DepRewrites[$dep] + '"$1')
    }
    [System.IO.File]::WriteAllText($manifestPath, $manifestText, (New-Object System.Text.UTF8Encoding $false))

    # fail closed: the staged manifest must carry the staged id and every rewritten dependency
    $check = Get-Content $manifestPath -Raw | ConvertFrom-Json
    if ($check.id -ne $cfg.IdTo) { throw "manifest id rewrite failed for $($cfg.Src) -- staging aborted" }
    if ($check.title -match '\(dev\)') { throw "dev title survived staging for $($cfg.Src) -- staging aborted" }
    foreach ($dep in $cfg.DepRewrites.Keys) {
        $deps = $check.dependencies
        if ($null -eq $deps -or $null -eq $deps.($cfg.DepRewrites[$dep]) -or $null -ne $deps.$dep) {
            throw "dependency rewrite $dep -> $($cfg.DepRewrites[$dep]) failed for $($cfg.Src) -- staging aborted"
        }
    }
    Write-Host "manifest ok: $($cfg.Src) -> '$($check.title)', id $($check.id)$(if ($check.dependencies) { ', deps ' + (($check.dependencies.PSObject.Properties | % { $_.Name }) -join ',') })"
}

# ---------------------------------------------------------------- save-layout guard
# (2026-09-30, after a 0.1.2.5 save lost its statues): the engine loads a part's public fields
# by position with no end check (IComponent.Read; design/ground-truth.md), so a shipped class
# whose saved fields change corrupts every old save that holds it. Before a stage folder is
# overwritten, every class in the build already staged there is compared with the new tree:
# public instance fields, in order, skipping [NonSerialized]. Any drift aborts. Classes in
# SaveCompatStubs.cs are exempt (they skip their saved data by design). Classes that are gone
# are listed, not fatal: their parts load with the engine's load-error popup unless stubbed.
function Get-SavedLayouts([string]$scriptsDir) {
    $layouts = @{}
    $stubs = @{}
    if (-not (Test-Path $scriptsDir)) { return @{ Layouts = $layouts; Stubs = $stubs } }
    foreach ($file in Get-ChildItem $scriptsDir -Recurse -Filter *.cs) {
        $cls = $null
        $pendingNonSerialized = $false
        foreach ($line in [System.IO.File]::ReadAllLines($file.FullName)) {
            if ($line -match '^\s*public (?:sealed |abstract |partial )*class (\w+)') {
                $cls = $Matches[1]
                $layouts[$cls] = New-Object System.Collections.Generic.List[string]
                if ($file.Name -eq "SaveCompatStubs.cs") { $stubs[$cls] = $true }
                $pendingNonSerialized = $false
                continue
            }
            if ($line -match '^\s*\[NonSerialized\]\s*$') { $pendingNonSerialized = $true; continue }
            if ($cls -and $line -match '^\t\t(\[NonSerialized\] )?public (?!override |static |const |class |abstract |virtual |event |readonly )([\w<>\[\],\.\?]+) (\w+)( =[^;]*)?;') {
                if (-not $Matches[1] -and -not $pendingNonSerialized) { $layouts[$cls].Add("$($Matches[2]) $($Matches[3])") }
            }
            if ($line.Trim()) { $pendingNonSerialized = $false }
        }
    }
    return @{ Layouts = $layouts; Stubs = $stubs }
}

function Check-SaveLayout([string]$newTree, [string]$stageName) {
    $staged = Join-Path (Join-Path $modsRoot $stageName) "Scripts"
    if (-not (Test-Path $staged)) { Write-Host "save layout: nothing staged in $stageName yet, no shipped layouts to hold"; return }
    $old = Get-SavedLayouts $staged
    $new = Get-SavedLayouts (Join-Path $newTree "Scripts")
    $drift = @(); $gone = @(); $held = 0
    foreach ($cls in $old.Layouts.Keys) {
        if (-not $new.Layouts.ContainsKey($cls)) { $gone += $cls; continue }
        if ($new.Stubs.ContainsKey($cls)) { continue }
        $a = $old.Layouts[$cls] -join ", "; $b = $new.Layouts[$cls] -join ", "
        if ($a -ne $b) { $drift += "  $cls`n    staged: [$a]`n    new:    [$b]" } else { $held++ }
    }
    if ($gone.Count) { Write-Host "save layout note: $($gone.Count) staged class(es) gone (their parts load with an error unless stubbed): $(($gone | Sort-Object) -join ', ')" }
    if ($drift.Count) { throw "save layout DRIFT against the build staged in ${stageName}: old saves would misread these parts. Move new data to [NonSerialized] fields or object properties, or stub the class.`n$($drift -join "`n")" }
    Write-Host "save layout ok: $held class(es) keep the layout staged in $stageName"
}

# ---------------------------------------------------------------- run
$wantTF1 = $Mod -eq "TF1" -or $Mod -eq "Both"
$wantTF2 = $Mod -eq "TF2" -or $Mod -eq "Both"
$wantFused = $Mod -eq "Fused"
$wantRelease = $Mod -eq "Release"
Strip-Mod $Mods.TF1                     # always: TF2 and the fused build compile against it
if ($wantTF2 -or $wantFused -or $wantRelease) { Strip-Mod $Mods.TF2 }
Compile-Check ($wantTF2 -or $wantFused -or $wantRelease)
# placeholder guard: scans the STRIPPED trees, so only text that would ship counts (comments are
# gone); the fused and release builds are unions of these. A real run aborts, -CheckOnly warns.
$placeholderTrees = @(Join-Path $pubRoot $Mods.TF1.Src)
if ($wantTF2 -or $wantFused -or $wantRelease) { $placeholderTrees += Join-Path $pubRoot $Mods.TF2.Src }
$placeholders = foreach ($t in $placeholderTrees) {
    Get-ChildItem $t -Recurse -File -Include *.cs, *.xml, *.json, *.txt | Select-String -SimpleMatch -CaseSensitive -Pattern '[PLACEHOLDER'
}
if ($placeholders) {
    $where = $placeholders | ForEach-Object { "$($_.Path.Substring($pubRoot.Length + 1)):$($_.LineNumber): $($_.Line.Trim())" }
    if ($CheckOnly) { $where | ForEach-Object { Write-Warning "placeholder text would ship -- $_" } }
    else { throw "placeholder text would ship -- replace it before staging; publish aborted, nothing zipped`n$($where -join "`n")" }
}
if ($wantFused) { Fuse-Mods $Mods.Fused; Compile-Fused $Mods.Fused }
if ($wantRelease) { Fuse-Mods $Mods.Release; Compile-Fused $Mods.Release }
# no stage folder may be overwritten by a build that would misread its saves
if ($wantTF1) { Check-SaveLayout (Join-Path $pubRoot $Mods.TF1.Src) $Mods.TF1.Stage }
if ($wantTF2) { Check-SaveLayout (Join-Path $pubRoot $Mods.TF2.Src) $Mods.TF2.Stage }
if ($wantFused) { Check-SaveLayout (Join-Path $pubRoot $Mods.Fused.Src) $Mods.Fused.Stage }
if ($wantRelease) { Check-SaveLayout (Join-Path $pubRoot $Mods.Release.Src) $Mods.Release.Stage }
# the workshop cover, rendered once for whichever run stages TerraFirmaBeta (also under -CheckOnly, to prove it renders)
if ($wantTF1 -or $wantRelease) { $coverPng = Render-Cover }
if ($CheckOnly) {
    foreach ($k in @("TF1", "TF2", "Fused", "Release")) {
        if (($k -eq "TF1" -and -not $wantTF1) -or ($k -eq "TF2" -and -not $wantTF2) -or ($k -eq "Fused" -and -not $wantFused) -or ($k -eq "Release" -and -not $wantRelease)) { continue }
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
if ($wantFused) { Release-Mod $Mods.Fused }
if ($wantRelease) { Release-Mod $Mods.Release }
Write-Host "publish OK ($Mod)"
