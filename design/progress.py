# Progress dashboard generator for the Terra Firma polish worklist.
# Usage:  python design/progress.py     (from the repo root, or anywhere)
# Parses design/polish-worklist-2026-07-23.md checkbox state per section and
# writes design/progress.html — a self-contained dashboard with three finish
# lines (release gate / polish passes / optional tail) so "how done are we"
# stops being one misleading number. Each section is a dropdown listing its
# open items (done ones dimmed below). Regenerate after any worklist update.

import html
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

def item_title(line: str) -> tuple[str, str]:
    """(tag, short title) for a checkbox line."""
    t = re.search(r"`\[(design|ratify|polish|text|bug|release)\]`", line)
    tag = t.group(1) if t else ""
    b = re.search(r"\*\*(.+?)\*\*", line)
    if b:
        title = b.group(1)
    else:
        title = re.sub(r"^- \[.\]\s*", "", line)
        title = re.sub(r"[🆕📜]|`\[[a-z]+\]`|\*\(housekeeping\)\*", "", title).strip(" —-·*")
    title = title.replace("**", "").strip()
    if len(title) > 110:
        title = title[:107] + "..."
    return tag, title

def main():
    text = WORKLIST.read_text(encoding="utf-8")
    sections = []   # (header, lane, [(state, tag, title), ...])
    header, lane = None, ""
    items = []
    tags_open = {}
    appendix_items = 0
    in_appendix = False
    closed_recent = 0

    def flush():
        nonlocal items
        if header and lane:
            sections.append((header, lane, items))
        items = []

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
            tag, title = item_title(line)
            items.append((state, tag, title))
            if state == "x" and "2026-07-2" in line:
                closed_recent += 1
            if state == " ":
                tags_open[tag or "other"] = tags_open.get(tag or "other", 0) + 1
    flush()

    def counts(item_list):
        d = sum(1 for s, _, _ in item_list if s == "x")
        o = sum(1 for s, _, _ in item_list if s == " ")
        p = sum(1 for s, _, _ in item_list if s == "~")
        return d, o, p

    def bar(d, o, p):
        total = d + o + p
        if total == 0:
            return "<em>nothing tracked</em>"
        pct = round(100 * (d + 0.5 * p) / total)
        return (
            f'<span class="track"><span class="fill" style="width:{pct}%"></span></span>'
            f'<span class="pct">{pct}% &middot; {d}/{total}</span>'
        )

    def item_li(state, tag, title):
        cls = {"x": "done", "~": "partial", " ": "open"}[state]
        mark = {"x": "&#10003;", "~": "&#189;", " ": "&#9675;"}[state]
        tag_html = f'<span class="tag">{tag}</span>' if tag else ""
        return f'<li class="{cls}"><span class="mark">{mark}</span>{tag_html}{html.escape(title)}</li>'

    rows = []
    for lane_key in ("release", "pass", "side"):
        lane_items = [it for _, lk, its in sections if lk == lane_key for it in its]
        d, o, p = counts(lane_items)
        rows.append(f"<h2>{LANES[lane_key]}</h2><p>{bar(d, o, p)}</p>")
        for h, lk, its in sections:
            if lk != lane_key:
                continue
            sd, so, sp = counts(its)
            open_lis = "".join(item_li(s, t, ti) for s, t, ti in its if s != "x")
            done_lis = "".join(item_li(s, t, ti) for s, t, ti in its if s == "x")
            rows.append(
                f'<details><summary><span class="sect">{html.escape(h)}</span>{bar(sd, so, sp)}</summary>'
                f"<ul>{open_lis}{done_lis}</ul></details>"
            )

    tag_bits = " &middot; ".join(
        f"{k}: {v}" for k, v in sorted(tags_open.items(), key=lambda kv: -kv[1])
    )
    all_items = [it for _, _, its in sections for it in its]
    total_d, total_o, total_p = counts(all_items)
    stamp = datetime.date.today().isoformat()

    html_out = f"""<!doctype html><html><head><meta charset="utf-8">
<title>Terra Firma — progress</title>
<style>
  :root {{ color-scheme: light dark; font-family: system-ui, sans-serif; }}
  body {{ max-width: 800px; margin: 2rem auto; padding: 0 1rem; line-height: 1.5; }}
  h1 {{ font-size: 1.4rem; }} h2 {{ font-size: 1.05rem; margin: 1.5rem 0 .3rem; }}
  .track {{ display: inline-block; width: 200px; height: 10px; border-radius: 5px;
           background: rgba(128,128,128,.25); vertical-align: middle; margin: 0 .6em; }}
  .fill {{ display: block; height: 100%; border-radius: 5px; background: #2f9e73; }}
  .pct {{ font-variant-numeric: tabular-nums; color: color-mix(in srgb, currentColor 70%, transparent); }}
  details {{ margin: .35rem 0; }}
  summary {{ cursor: pointer; padding: .25rem 0; }}
  summary::marker {{ color: color-mix(in srgb, currentColor 50%, transparent); }}
  .sect {{ display: inline-block; min-width: 300px; font-size: .95rem; }}
  ul {{ list-style: none; margin: .3rem 0 .6rem; padding-left: 1.4rem; }}
  li {{ font-size: .88rem; padding: .12rem 0; }}
  li .mark {{ display: inline-block; width: 1.3em; }}
  li.done {{ color: color-mix(in srgb, currentColor 45%, transparent); text-decoration: line-through; }}
  li.partial {{ color: color-mix(in srgb, currentColor 75%, transparent); }}
  .tag {{ display: inline-block; font-size: .72rem; border: 1px solid color-mix(in srgb, currentColor 35%, transparent);
         border-radius: 8px; padding: 0 .45em; margin-right: .45em; vertical-align: middle;
         color: color-mix(in srgb, currentColor 70%, transparent); }}
  .meta {{ color: color-mix(in srgb, currentColor 60%, transparent); font-size: .88rem; }}
</style></head><body>
<h1>Terra Firma — how done are we?</h1>
<p class="meta">Generated {stamp} from the polish worklist ({total_d + total_o + total_p} tracked items;
partial items count half). Regenerate: <code>python design/progress.py</code></p>
<p><strong>Overall tracked work:</strong> {bar(total_d, total_o, total_p)}</p>
<p class="meta">Closed with a July-2026 stamp: {closed_recent} items &middot;
verify backlog (appendix, not counted above): ~{appendix_items} in-game checks &middot;
open items by kind: {tag_bits}</p>
{"".join(rows)}
<p class="meta">The three finish lines are deliberate: the release gate is what "shippable" means;
the passes are the quality sweep; side content never blocks anything. If the top bar feels stuck,
look at which lane actually moved. Open items list first inside each dropdown; done items are
struck through below them.</p>
</body></html>"""
    OUT.write_text(html_out, encoding="utf-8")
    print(f"wrote {OUT}")
    print(f"overall: {total_d} done / {total_o} open / {total_p} partial; recent closes: {closed_recent}")
    for lane_key in ("release", "pass", "side"):
        lane_items = [it for _, lk, its in sections if lk == lane_key for it in its]
        d, o, p = counts(lane_items)
        print(f"  {LANES[lane_key]}: {d} done / {o} open / {p} partial")

if __name__ == "__main__":
    main()
