# Progress dashboard generator for the Terra Firma polish worklist.
# Usage:  python design/progress.py     (from the repo root, or anywhere)
# Parses design/polish-worklist-2026-07-23.md checkbox state per section and
# writes design/progress.html — a self-contained dashboard with three finish
# lines (release gate / polish passes / optional tail) so "how done are we"
# stops being one misleading number. Regenerate after any worklist update.

import re
import datetime
from pathlib import Path

HERE = Path(__file__).parent
WORKLIST = HERE / "polish-worklist-2026-07-23.md"
OUT = HERE / "progress.html"

LANES = {
    "release": "Release gate",
    "pass": "Polish passes",
    "side": "Side content (optional)",
}

def lane_for(header: str) -> str:
    h = header.lower()
    if h.startswith("pass "):
        return "pass"
    if "cross-cutting" in h or "release" in h:
        return "release"
    if "side content" in h:
        return "side"
    return ""

def main():
    text = WORKLIST.read_text(encoding="utf-8")
    sections = []   # (header, lane, done, open_, partial, closed_today)
    header, lane = None, ""
    done = open_ = partial = 0
    tags_open = {}
    appendix_items = 0
    in_appendix = False
    stamp_today = datetime.date.today().isoformat()
    closed_recent = 0

    def flush():
        nonlocal done, open_, partial
        if header and lane:
            sections.append((header, lane, done, open_, partial))
        done = open_ = partial = 0

    for line in text.splitlines():
        m = re.match(r"^## (.+)$", line)
        if m:
            flush()
            header = m.group(1).strip()
            lane = lane_for(header)
            in_appendix = header.lower().startswith("appendix")
            continue
        if in_appendix:
            appendix_items += line.count("·")
            continue
        box = re.match(r"^- \[( |x|~)\]", line)
        if box and lane:
            state = box.group(1)
            if state == "x":
                done += 1
                if "2026-07-2" in line:
                    closed_recent += 1
            elif state == "~":
                partial += 1
            else:
                open_ += 1
                t = re.search(r"`\[(design|ratify|polish|text|bug|release)\]`", line)
                key = t.group(1) if t else "other"
                tags_open[key] = tags_open.get(key, 0) + 1
    flush()

    def lane_totals(lane_key):
        d = sum(s[2] for s in sections if s[1] == lane_key)
        o = sum(s[3] for s in sections if s[1] == lane_key)
        p = sum(s[4] for s in sections if s[1] == lane_key)
        return d, o, p

    def bar(d, o, p):
        total = d + o + p
        if total == 0:
            return "<em>nothing tracked</em>"
        pct = round(100 * (d + 0.5 * p) / total)
        return (
            f'<div class="track"><div class="fill" style="width:{pct}%"></div></div>'
            f'<span class="pct">{pct}% &middot; {d}/{total}</span>'
        )

    rows = []
    for lane_key in ("release", "pass", "side"):
        d, o, p = lane_totals(lane_key)
        rows.append(f'<h2>{LANES[lane_key]}</h2>{bar(d, o, p)}<table>')
        for h, lk, sd, so, sp in sections:
            if lk == lane_key:
                rows.append(f"<tr><td>{h}</td><td>{bar(sd, so, sp)}</td></tr>")
        rows.append("</table>")

    tag_bits = " &middot; ".join(
        f"{k}: {v}" for k, v in sorted(tags_open.items(), key=lambda kv: -kv[1])
    )
    total_d = sum(s[2] for s in sections)
    total_o = sum(s[3] for s in sections)
    total_p = sum(s[4] for s in sections)

    html = f"""<!doctype html><html><head><meta charset="utf-8">
<title>Terra Firma — progress</title>
<style>
  :root {{ color-scheme: light dark; font-family: system-ui, sans-serif; }}
  body {{ max-width: 760px; margin: 2rem auto; padding: 0 1rem; line-height: 1.5; }}
  h1 {{ font-size: 1.4rem; }} h2 {{ font-size: 1.05rem; margin: 1.4rem 0 .3rem; }}
  .track {{ display: inline-block; width: 220px; height: 10px; border-radius: 5px;
           background: rgba(128,128,128,.25); vertical-align: middle; margin-right: .6em; }}
  .fill {{ height: 100%; border-radius: 5px; background: #2f9e73; }}
  .pct {{ font-variant-numeric: tabular-nums; color: color-mix(in srgb, currentColor 70%, transparent); }}
  table {{ border-collapse: collapse; margin-top: .4rem; width: 100%; }}
  td {{ padding: .2rem .6rem .2rem 0; font-size: .92rem; }}
  td:first-child {{ max-width: 340px; }}
  .meta {{ color: color-mix(in srgb, currentColor 60%, transparent); font-size: .88rem; }}
</style></head><body>
<h1>Terra Firma — how done are we?</h1>
<p class="meta">Generated {stamp_today} from the polish worklist ({total_d + total_o + total_p} tracked items;
partial items count half). Regenerate: <code>python design/progress.py</code></p>
<p><strong>Overall tracked work:</strong> {bar(total_d, total_o, total_p)}</p>
<p class="meta">Closed with a July-2026 stamp: {closed_recent} items &middot;
verify backlog (appendix, not counted above): ~{appendix_items} in-game checks &middot;
open items by kind: {tag_bits}</p>
{"".join(rows)}
<p class="meta">The three finish lines are deliberate: the release gate is what "shippable" means;
the passes are the quality sweep; side content never blocks anything. If the top bar feels stuck,
look at which lane actually moved.</p>
</body></html>"""
    OUT.write_text(html, encoding="utf-8")
    print(f"wrote {OUT}")
    print(f"overall: {total_d} done / {total_o} open / {total_p} partial; recent closes: {closed_recent}")
    for lane_key in ("release", "pass", "side"):
        d, o, p = lane_totals(lane_key)
        print(f"  {LANES[lane_key]}: {d} done / {o} open / {p} partial")

if __name__ == "__main__":
    main()
