# generate-asset-report.ps1 -ground-truth asset report for Terra Firma (2026-07-25)
# Scans the mod source + deployed folder and regenerates asset-debt-tracker.html from FACTS:
#   - sprites on disk (source + deployed Textures)
#   - every Tile=/Tiles=/"*.png" reference in XML and C#
#   - cross-reference: USED / MISSING (referenced, no file) / UNWIRED (file, no reference)
#   - Cleo_* blueprint inventory
#   - source<->deploy drift (Textures, XML, Scripts)
#   - rooms.json manifest (the one hand-curated input)
# Run any time:  powershell -NoProfile -ExecutionPolicy Bypass -File "design\generate-asset-report.ps1"

$Src = "C:\Users\venob\terra firma revival\CoQ_TerraFirma-dev\TerraFirma"
$Dep = Join-Path $env:USERPROFILE "AppData\LocalLow\Freehold Games\CavesOfQud\Mods\TerraFirma"
$OutHtml = "C:\Users\venob\terra firma revival\CoQ_TerraFirma-dev\design\asset-debt-tracker.html"
$RoomsJson = "C:\Users\venob\terra firma revival\CoQ_TerraFirma-dev\design\rooms.json"

function Get-RelTextures($root) {
    $tex = Join-Path $root "Textures"
    $map = @{}
    if (Test-Path $tex) {
        Get-ChildItem $tex -Recurse -File -Include *.png | ForEach-Object {
            $rel = $_.FullName.Substring($tex.Length + 1).Replace('\', '/')
            $map[$rel.ToLower()] = $rel
        }
    }
    return $map
}

$srcTex = Get-RelTextures $Src
$depTex = Get-RelTextures $Dep

# --- collect references: XML Tile="..."/Tiles="...", and any "*.png" literal in C# ---
$refs = @{}   # lower-name -> list of "file (kind)"
function Add-Ref($name, $where) {
    $k = $name.ToLower()
    if (-not $refs.ContainsKey($k)) { $refs[$k] = New-Object System.Collections.ArrayList }
    if (-not $refs[$k].Contains($where)) { [void]$refs[$k].Add($where) }
}
Get-ChildItem (Join-Path $Src "XML") -Filter *.xml | ForEach-Object {
    $c = Get-Content $_.FullName -Raw -Encoding UTF8
    foreach ($m in [regex]::Matches($c, 'Tile="([^"]+)"'))  { Add-Ref $m.Groups[1].Value $_.Name }
    foreach ($m in [regex]::Matches($c, 'Tiles="([^"]+)"')) {
        foreach ($t in $m.Groups[1].Value.Split(',')) { Add-Ref $t.Trim() $_.Name }
    }
}
Get-ChildItem (Join-Path $Src "Scripts") -Filter *.cs | ForEach-Object {
    $c = Get-Content $_.FullName -Raw -Encoding UTF8
    foreach ($m in [regex]::Matches($c, '"([A-Za-z0-9_\-/ ]+\.png)"')) { Add-Ref $m.Groups[1].Value $_.Name }
}

# --- classify: a ref is MOD-LOCAL if the file exists in either Textures, or it has no base-game folder prefix ---
$used = @();  $missing = @();  $baseRefs = 0
foreach ($k in $refs.Keys) {
    $inSrc = $srcTex.ContainsKey($k); $inDep = $depTex.ContainsKey($k)
    if ($inSrc -or $inDep) {
        $where = @()
        if ($inSrc) { $where += "src" }
        if ($inDep) { $where += "deploy" }
        $used += [pscustomobject]@{ Sprite = $k; On = ($where -join "+"); RefBy = ($refs[$k] -join ", ") }
    }
    elseif ($k -match '/' -or $k -match '^assets_content_') { $baseRefs++ }   # base-game path (Items/... or legacy underscore-flattened Assets_Content_...)
    else {
        $missing += [pscustomobject]@{ Sprite = $k; RefBy = ($refs[$k] -join ", ") }
    }
}
$unwired = @()
$allTexKeys = ($srcTex.Keys + $depTex.Keys) | Sort-Object -Unique
foreach ($k in $allTexKeys) {
    $isRef = $refs.ContainsKey($k)
    if (-not $isRef) {
        # also match bare-name refs to files sitting in subfolders (e.g. Abilities/x.png referenced as x.png)
        $bare = ($k -split '/')[-1]
        if ($refs.ContainsKey($bare)) { continue }
        $on = @()
        if ($srcTex.ContainsKey($k)) { $on += "src" }
        if ($depTex.ContainsKey($k)) { $on += "deploy" }
        $unwired += [pscustomobject]@{ Sprite = $k; On = ($on -join "+") }
    }
}

# --- blueprint inventory ---
$blueprints = @()
Get-ChildItem (Join-Path $Src "XML") -Filter *.xml | ForEach-Object {
    $c = Get-Content $_.FullName -Raw -Encoding UTF8
    foreach ($m in [regex]::Matches($c, '<object Name="(Cleo_[^"]+)"')) {
        $blueprints += [pscustomobject]@{ Blueprint = $m.Groups[1].Value; File = $_.Name }
    }
}

# --- source<->deploy drift (XML + Scripts by hash, Textures by presence) ---
$drift = @()
foreach ($sub in @("XML", "Scripts")) {
    $s = Join-Path $Src $sub; $d = Join-Path $Dep $sub
    $sFiles = @{}; $dFiles = @{}
    if (Test-Path $s) { Get-ChildItem $s -File | ForEach-Object { $sFiles[$_.Name] = $_.FullName } }
    if (Test-Path $d) { Get-ChildItem $d -File | ForEach-Object { $dFiles[$_.Name] = $_.FullName } }
    foreach ($n in ($sFiles.Keys + $dFiles.Keys | Sort-Object -Unique)) {
        if (-not $dFiles.ContainsKey($n)) { $drift += [pscustomobject]@{ File = "$sub/$n"; State = "source only (not deployed)" } }
        elseif (-not $sFiles.ContainsKey($n)) { $drift += [pscustomobject]@{ File = "$sub/$n"; State = "DEPLOYED ONLY (no source copy!)" } }
        else {
            $h1 = (Get-FileHash $sFiles[$n]).Hash; $h2 = (Get-FileHash $dFiles[$n]).Hash
            if ($h1 -ne $h2) {
                $newer = "source newer"
                if ((Get-Item $dFiles[$n]).LastWriteTime -gt (Get-Item $sFiles[$n]).LastWriteTime) { $newer = "deployed newer" }
                $drift += [pscustomobject]@{ File = "$sub/$n"; State = "differs ($newer)" }
            }
        }
    }
}
foreach ($k in $allTexKeys) {
    if (-not $srcTex.ContainsKey($k)) { $drift += [pscustomobject]@{ File = "Textures/$k"; State = "DEPLOYED ONLY (no source copy!)" } }
    elseif (-not $depTex.ContainsKey($k)) { $drift += [pscustomobject]@{ File = "Textures/$k"; State = "source only (not deployed)" } }
}

# --- rooms manifest ---
$rooms = @()
if (Test-Path $RoomsJson) { $rooms = Get-Content $RoomsJson -Raw -Encoding UTF8 | ConvertFrom-Json }

# --- sprites-needed manifest: hand-curated QUEUE, auto-derived STATUS ---
$NeededJson = "C:\Users\venob\terra firma revival\CoQ_TerraFirma-dev\design\sprites-needed.json"
$needed = @(); $toDraw = 0
if (Test-Path $NeededJson) {
    foreach ($n in (Get-Content $NeededJson -Raw -Encoding UTF8 | ConvertFrom-Json)) {
        $k = $n.file.ToLower()
        $onDisk = $srcTex.ContainsKey($k) -or $depTex.ContainsKey($k)
        $isWired = $refs.ContainsKey($k)
        $state = "TO DRAW"
        if ($onDisk -and $isWired) { $state = "done (drawn + wired)" }
        elseif ($onDisk) { $state = "drawn, not wired yet" }
        if ($state -eq "TO DRAW") { $toDraw++ }
        $needed += [pscustomobject]@{ File = $n.file; For = $n.for; Source = $n.source; State = $state }
    }
}

# --- emit HTML ---
function Esc($s) { if ($null -eq $s) { return "" } return [System.Web.HttpUtility]::HtmlEncode([string]$s) }
Add-Type -AssemblyName System.Web
$sb = New-Object System.Text.StringBuilder
[void]$sb.AppendLine('<!doctype html><html lang="en"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width, initial-scale=1">')
[void]$sb.AppendLine('<title>Terra Firma -Asset Report (generated)</title><style>')
[void]$sb.AppendLine(':root{--bg:#faf9f7;--surface:#fff;--ink:#1f2430;--ink2:#5b6272;--ink3:#8a90a0;--line:#e6e4df;--ok:#1f5c33;--okbg:#e3efe6;--warn:#7a2727;--warnbg:#f6e3e3;--info:#274f80;--infobg:#e3ecf7}')
[void]$sb.AppendLine('@media (prefers-color-scheme:dark){:root{--bg:#14161c;--surface:#1d2028;--ink:#e8e9ee;--ink2:#aab0bf;--ink3:#7c8291;--line:#2c303b;--ok:#9fd4ae;--okbg:#1e3a28;--warn:#e3a1a1;--warnbg:#452222;--info:#a9c8ef;--infobg:#203752}}')
[void]$sb.AppendLine('*{box-sizing:border-box}body{margin:0;padding:24px;background:var(--bg);color:var(--ink);font:14px/1.5 "Segoe UI",system-ui,sans-serif}')
[void]$sb.AppendLine('h1{font-size:20px;margin:0 0 4px}.sub{color:var(--ink2);margin-bottom:20px}')
[void]$sb.AppendLine('.tiles{display:grid;grid-template-columns:repeat(auto-fit,minmax(150px,1fr));gap:10px;margin-bottom:24px}')
[void]$sb.AppendLine('.tile{background:var(--surface);border:1px solid var(--line);border-radius:8px;padding:12px 14px}.tile .n{font-size:26px;font-weight:650}.tile .l{color:var(--ink2);font-size:12px}')
[void]$sb.AppendLine('section{background:var(--surface);border:1px solid var(--line);border-radius:8px;padding:16px;margin-bottom:16px;overflow-x:auto}')
[void]$sb.AppendLine('h2{font-size:14px;margin:0 0 10px}table{border-collapse:collapse;width:100%;font-size:13px}')
[void]$sb.AppendLine('th{text-align:left;color:var(--ink3);font-weight:600;padding:4px 10px 6px 0;border-bottom:1px solid var(--line)}td{padding:5px 10px 5px 0;border-bottom:1px solid var(--line);vertical-align:top}tr:last-child td{border-bottom:none}')
[void]$sb.AppendLine('.chip{display:inline-block;padding:1px 8px;border-radius:10px;font-size:11.5px;font-weight:600;white-space:nowrap}.ok{background:var(--okbg);color:var(--ok)}.warn{background:var(--warnbg);color:var(--warn)}.info{background:var(--infobg);color:var(--info)}')
[void]$sb.AppendLine('.foot{color:var(--ink3);font-size:12px;margin-top:8px}</style></head><body>')
[void]$sb.AppendLine('<h1>Terra Firma -asset report</h1>')
[void]$sb.AppendLine(('<div class="sub">Generated {0} by scanning the mod source and deployed folder. Nothing on this page is hand-maintained; rerun <b>design\generate-asset-report.ps1</b> to refresh. Design/ideation lives in asset-debt-tracker.md and TODO.md, not here.</div>' -f (Esc (Get-Date -Format "yyyy-MM-dd HH:mm"))))
[void]$sb.AppendLine('<div class="tiles">')
[void]$sb.AppendLine(('<div class="tile"><div class="n">{0}</div><div class="l">mod sprites on disk</div></div>' -f $allTexKeys.Count))
[void]$sb.AppendLine(('<div class="tile"><div class="n">{0}</div><div class="l">wired to blueprints/code</div></div>' -f $used.Count))
[void]$sb.AppendLine(('<div class="tile"><div class="n">{0}</div><div class="l">referenced but MISSING</div></div>' -f $missing.Count))
[void]$sb.AppendLine(('<div class="tile"><div class="n">{0}</div><div class="l">drawn but unwired</div></div>' -f $unwired.Count))
[void]$sb.AppendLine(('<div class="tile"><div class="n">{0}</div><div class="l">Cleo_* blueprints</div></div>' -f $blueprints.Count))
[void]$sb.AppendLine(('<div class="tile"><div class="n">{0}</div><div class="l">source&harr;deploy drift files</div></div>' -f $drift.Count))
[void]$sb.AppendLine(('<div class="tile"><div class="n">{0}</div><div class="l">sprites still TO DRAW</div></div>' -f $toDraw))
[void]$sb.AppendLine('</div>')

[void]$sb.AppendLine('<section><h2>Sprite queue -what still needs drawing (from sprites-needed.json; status auto-derived: drop the file in Textures and this flips on the next run)</h2><table><tr><th>Planned file</th><th>For</th><th>Source</th><th>Status</th></tr>')
foreach ($n in $needed) {
    $cls = "warn"
    if ($n.State -match "^done") { $cls = "ok" } elseif ($n.State -match "not wired") { $cls = "info" }
    [void]$sb.AppendLine(('<tr><td>{0}</td><td>{1}</td><td>{2}</td><td><span class="chip {3}">{4}</span></td></tr>' -f (Esc $n.File), (Esc $n.For), (Esc $n.Source), $cls, (Esc $n.State)))
}
[void]$sb.AppendLine('</table></section>')

[void]$sb.AppendLine('<section><h2>Missing sprites -referenced by the mod, no file on disk</h2><table><tr><th>Expected file</th><th>Referenced in</th></tr>')
if ($missing.Count -eq 0) { [void]$sb.AppendLine('<tr><td colspan="2"><span class="chip ok">none -every reference resolves</span></td></tr>') }
foreach ($m in ($missing | Sort-Object Sprite)) { [void]$sb.AppendLine(('<tr><td><span class="chip warn">{0}</span></td><td>{1}</td></tr>' -f (Esc $m.Sprite), (Esc $m.RefBy))) }
[void]$sb.AppendLine('</table></section>')

[void]$sb.AppendLine('<section><h2>Wired sprites -file exists and something uses it</h2><table><tr><th>Sprite</th><th>Present</th><th>Referenced in</th></tr>')
foreach ($u in ($used | Sort-Object Sprite)) { [void]$sb.AppendLine(('<tr><td>{0}</td><td><span class="chip ok">{1}</span></td><td>{2}</td></tr>' -f (Esc $u.Sprite), (Esc $u.On), (Esc $u.RefBy))) }
[void]$sb.AppendLine('</table></section>')

[void]$sb.AppendLine('<section><h2>Unwired sprites -drawn, nothing references them yet</h2><table><tr><th>Sprite</th><th>Present</th></tr>')
if ($unwired.Count -eq 0) { [void]$sb.AppendLine('<tr><td colspan="2"><span class="chip ok">none</span></td></tr>') }
foreach ($u in ($unwired | Sort-Object Sprite)) { [void]$sb.AppendLine(('<tr><td><span class="chip info">{0}</span></td><td>{1}</td></tr>' -f (Esc $u.Sprite), (Esc $u.On))) }
[void]$sb.AppendLine('</table></section>')

[void]$sb.AppendLine('<section><h2>Source &harr; deploy drift</h2><table><tr><th>File</th><th>State</th></tr>')
if ($drift.Count -eq 0) { [void]$sb.AppendLine('<tr><td colspan="2"><span class="chip ok">in sync</span></td></tr>') }
foreach ($d in ($drift | Sort-Object File)) {
    $cls = "info"; if ($d.State -match "DEPLOYED ONLY") { $cls = "warn" }
    [void]$sb.AppendLine(('<tr><td>{0}</td><td><span class="chip {1}">{2}</span></td></tr>' -f (Esc $d.File), $cls, (Esc $d.State)))
}
[void]$sb.AppendLine('</table></section>')

[void]$sb.AppendLine('<section><h2>Rooms (from rooms.json -the one hand-curated input)</h2><table><tr><th>Room</th><th>Source</th><th>Status</th><th>Needs</th></tr>')
foreach ($r in $rooms) { [void]$sb.AppendLine(('<tr><td>{0}</td><td>{1}</td><td>{2}</td><td>{3}</td></tr>' -f (Esc $r.room), (Esc $r.source), (Esc $r.status), (Esc $r.needs))) }
[void]$sb.AppendLine('</table></section>')

[void]$sb.AppendLine(('<section><h2>Cleo_* blueprint inventory ({0})</h2><table><tr><th>Blueprint</th><th>File</th></tr>' -f $blueprints.Count))
foreach ($b in ($blueprints | Sort-Object Blueprint)) { [void]$sb.AppendLine(('<tr><td>{0}</td><td>{1}</td></tr>' -f (Esc $b.Blueprint), (Esc $b.File))) }
[void]$sb.AppendLine('</table></section>')
[void]$sb.AppendLine(('<div class="foot">{0} base-game tile references (Items/…, Creatures/…) ignored -they resolve from game assets.</div>' -f $baseRefs))
[void]$sb.AppendLine('</body></html>')

$sb.ToString() | Out-File -FilePath $OutHtml -Encoding utf8
Write-Host ("REPORT: {0} sprites on disk | {1} wired | {2} MISSING | {3} unwired | {4} blueprints | {5} drift files" -f $allTexKeys.Count, $used.Count, $missing.Count, $unwired.Count, $blueprints.Count, $drift.Count)
Write-Host ("written: " + $OutHtml)
