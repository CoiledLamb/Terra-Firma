# -*- coding: utf-8 -*-
"""
Dump every Furniture-tagged blueprint in the game to JSON for the warren lab's
furniture panel.

Why this exists: the engine's own dwelling tables pick contents by TAG
INTERSECTION (`*Seating,*Furniture` in Villages_BuildingContents_Dwelling_*),
never by blueprint list. The lab had been carrying hand-authored pools of
palette ids instead, which meant every vocabulary decision was a conversation.
With this dump the pools can go back to being tag queries, and the panel gives
the user a direct on/off per blueprint on top of that.

Inheritance is resolved here, not in the browser: a blueprint's tile, colours and
tags mostly live on its ancestors, and `Chest` says almost nothing about itself.

Writes design/furniture.json. Sprite masks are a separate step:
    python extract-qud-walls.py $(python extract-furniture.py --tiles)

Freehold Games assets are not ours to commit; the JSON records names and paths,
the sprites stay gitignored like the wall masks.
"""
import io
import json
import os
import re
import sys
import xml.etree.ElementTree as ET

# Qud writes CP437 glyphs as raw numeric refs - RenderString="&#3;" is a heart -
# and XML 1.0 forbids most of those code points outright, so ElementTree refuses
# the whole file. The glyph is cosmetic here (the panel draws real sprites), so
# bad refs are rewritten to a readable literal instead of losing the file.
BADREF = re.compile(r"&#(x[0-9a-fA-F]+|[0-9]+);")


def _ok(ref):
    n = int(ref[1:], 16) if ref[0] in "xX" else int(ref)
    return n in (9, 10, 13) or 32 <= n <= 0xD7FF or 0xE000 <= n <= 0xFFFD


def sanitize(text):
    return BADREF.sub(lambda m: m.group(0) if _ok(m.group(1)) else "#" + m.group(1), text)

QUD = r"C:\Program Files (x86)\Steam\steamapps\common\Caves of Qud\CoQ_Data"
BP = os.path.join(QUD, "StreamingAssets", "Base", "ObjectBlueprints")
HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.join(HERE, "furniture.json")

# the categories the engine's dwelling tables actually query on, plus the two
# render facts the articulation guard cares about
CATS = ["LightSource", "Seating", "Storage", "Sleep", "Centerpiece", "LiquidStorage",
        "Crafts", "Water", "Luxury", "Professional", "Scholarly", "Child", "Medical",
        "Torture", "Cheap", "Death", "Historical", "Contemporary", "Geological"]

objs = {}          # name -> element
order = []
for fn in sorted(os.listdir(BP)):
    if not fn.endswith(".xml"):
        continue
    try:
        with io.open(os.path.join(BP, fn), encoding="utf-8", errors="replace") as fh:
            root = ET.fromstring(sanitize(fh.read()))
    except ET.ParseError as e:
        sys.stderr.write("skip %s: %s\n" % (fn, e))
        continue
    for o in root.iter("object"):
        n = o.get("Name")
        if not n:
            continue
        objs[n] = (o, fn)
        order.append(n)


def chain(name, seen=None):
    """ancestors first, self last"""
    seen = seen or set()
    if name in seen or name not in objs:
        return []
    seen.add(name)
    o, _ = objs[name]
    parent = o.get("Inherits")
    return (chain(parent, seen) if parent else []) + [name]


def resolve(name):
    tags, stags, parts, builders = {}, set(), {}, []
    for step in chain(name):
        o, _ = objs[step]
        for t in o.findall("tag"):
            tags[t.get("Name")] = t.get("Value", "")
        for t in o.findall("stag"):
            stags.add(t.get("Name"))
        for t in o.findall("removepart"):
            parts.pop(t.get("Name"), None)
        for pt in o.findall("part"):
            pn = pt.get("Name")
            merged = dict(parts.get(pn, {}))
            merged.update({k: v for k, v in pt.attrib.items() if k != "Name"})
            parts[pn] = merged
        for b in o.findall("builder"):
            if b.get("Name") == "RandomTile" and b.get("Tiles"):
                builders = b.get("Tiles").split(",")
    return tags, stags, parts, builders


def tile_key(path):
    """`Terrain/sw_monument1.bmp` -> `sw_monument1`, matching the extractor's keys"""
    if not path:
        return None
    base = path.replace("\\", "/").split("/")[-1]
    for ext in (".bmp", ".png"):
        if base.lower().endswith(ext):
            base = base[:-4]
    return base.lower()


out = []
for name in order:
    o, fn = objs[name]
    own_tags = set(t.get("Name") for t in o.findall("tag"))
    if "BaseObject" in own_tags:
        continue                                    # abstract, never spawned
    tags, stags, parts, builders = resolve(name)
    if "Furniture" not in tags:
        continue
    render = parts.get("Render", {})
    physics = parts.get("Physics", {})
    desc = parts.get("Description", {}).get("Short", "")
    tiles = [t for t in ([render.get("Tile")] + builders) if t]
    cats = [c for c in CATS if c in tags or c in stags]
    out.append({
        "name": name,
        "display": render.get("DisplayName") or name,
        "file": fn,
        "tiles": [tile_key(t) for t in tiles],
        "tilePaths": tiles,
        "color": render.get("ColorString", ""),
        "detail": render.get("DetailColor", ""),
        "glyph": render.get("RenderString", ""),
        "cats": cats,
        "solid": physics.get("Solid", "false").lower() == "true",
        "occluding": render.get("Occluding", "false").lower() == "true",
        "takeable": physics.get("Takeable", "false").lower() == "true",
        "desc": desc[:220],
    })

if "--tiles" in sys.argv:
    seen = []
    for e in out:
        for t in e["tiles"]:
            if t and t not in seen:
                seen.append(t)
    print(" ".join(seen))
    sys.exit(0)

with open(OUT, "w") as f:
    json.dump(out, f, indent=1, sort_keys=True)
ntiles = len(set(t for e in out for t in e["tiles"] if t))
print("%d furniture blueprints, %d distinct tiles -> %s" % (len(out), ntiles, OUT))
by = {}
for e in out:
    for c in e["cats"]:
        by[c] = by.get(c, 0) + 1
print("categories: " + ", ".join("%s %d" % (k, by[k]) for k in sorted(by, key=lambda k: -by[k])))
print("solid: %d   occluding: %d   uncategorised: %d"
      % (sum(1 for e in out if e["solid"]),
         sum(1 for e in out if e["occluding"]),
         sum(1 for e in out if not e["cats"])))
