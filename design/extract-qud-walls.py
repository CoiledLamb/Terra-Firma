"""Extract Qud's painted-wall tilesets into loose PNGs for warren-lab.html.

WHY THIS EXISTS
    ZoneManager.PaintWall picks a wall's sprite from its 8 neighbours:
        tile = PaintedWallAtlas + paint + "-" + mask + PaintedWallExtension
    where mask is 8 binary digits in the order N NE E SE S SW W NW. The lab needs
    the real sprites to show what the game will actually draw.

WHY IT IS NOT A ONE-LINER
    The tiles are not loose files. They live in runtime atlas pages
    (Kobold_DynamicAtlas_Walls*), and the name -> rect index is ~27k exTextureInfo
    MonoBehaviours whose Unity type trees are stripped from the release build. So we
    regenerate the type trees from Assembly-CSharp.dll (a Mono build, so this works)
    and use them to deserialise the index, then crop each rect out of the atlas.

COLOUR
    Sprites are two-tone masks: opaque BLACK is the body (drawn in the blueprint's
    TileColor) and opaque WHITE is the detail (drawn in DetailColor). Shale is
    TileColor="&r" (dark red) / DetailColor="w" (brown). We bake a tint in because
    the lab runs from file://, where recolouring on a canvas taints it and
    getImageData throws. Re-run with different colours to reskin.

USAGE
    python extract-qud-walls.py                     # wall_rock, shale tint
    python extract-qud-walls.py wall_rock wall_brick
    python extract-qud-walls.py --body '#7a8a6a' --detail '#c8c0a8' wall_rock
    python extract-qud-walls.py --swap wall_rock    # if the two-tone read is backwards

Output lands in design/tiles/walls/ and is gitignored -- these are Freehold Games'
assets, fine as a local design aid, not ours to commit.
"""
import UnityPy, os, json, sys, collections
from UnityPy.helpers.TypeTreeGenerator import TypeTreeGenerator
from PIL import Image

QUD = r"C:\Program Files (x86)\Steam\steamapps\common\Caves of Qud\CoQ_Data"
OUT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "tiles", "walls")
UNITY = "6000.0.77f1"

body, detail, swap, wants = "#8c3f2a", "#b09070", False, []
args = sys.argv[1:]
while args:
    a = args.pop(0)
    if a == "--body": body = args.pop(0)
    elif a == "--detail": detail = args.pop(0)
    elif a == "--swap": swap = True
    elif a == "--qud": QUD = args.pop(0)
    else: wants.append(a)
wants = wants or ["wall_rock"]
hexrgb = lambda s: tuple(int(s.lstrip("#")[i:i + 2], 16) for i in (0, 2, 4))
BODY, DETAIL = hexrgb(body), hexrgb(detail)
if swap: BODY, DETAIL = DETAIL, BODY

print(f"reading {QUD}")
env = UnityPy.load(os.path.join(QUD, "resources.assets"))
gen = TypeTreeGenerator(UNITY)
gen.load_local_dll_folder(os.path.join(QUD, "Managed"))
nodes = json.loads(gen.get_nodes_as_json("Assembly-CSharp.dll", "exTextureInfo"))

atlas, atlas_name = {}, {}
for o in env.objects:
    if o.type.name == "Texture2D":
        d = o.read()
        atlas_name[o.path_id] = d.m_Name
        if "Atlas" in d.m_Name:
            atlas[o.path_id] = d.image

os.makedirs(OUT, exist_ok=True)
found, manifest = collections.Counter(), {}
for o in env.objects:
    if o.type.name != "MonoBehaviour":
        continue
    try:
        t = o.read_typetree(nodes)
    except Exception:
        continue
    name = str(t.get("m_Name", ""))
    low = name.lower()
    hit = next((w for w in wants if "_" + w + "-" in low or "_" + w + "." in low), None)
    if not hit:
        continue
    src = atlas.get(t["texture"]["m_PathID"])
    if src is None:
        found[hit + " (no atlas)"] += 1
        continue
    x, y, w, h = t["x"], t["y"], t["width"], t["height"]
    # Unity atlases are bottom-up; UnityPy returns a top-down PIL image
    piece = src.crop((x, src.height - y - h, x + w, src.height - y))
    if t.get("rotated"):
        piece = piece.transpose(Image.ROTATE_270)
    rw, rh = t.get("rawWidth", w), t.get("rawHeight", h)
    if t.get("trim") and (rw, rh) != piece.size:
        canvas = Image.new("RGBA", (rw, rh), (0, 0, 0, 0))
        canvas.paste(piece, (t.get("trim_x", 0), t.get("trim_y", 0)))
        piece = canvas
    piece = piece.convert("RGBA")
    px = piece.load()
    for yy in range(piece.height):
        for xx in range(piece.width):
            r, g, b, a = px[xx, yy]
            if a == 0:
                px[xx, yy] = (0, 0, 0, 0)
            elif r > 127:                      # white -> detail
                px[xx, yy] = (*DETAIL, 255)
            else:                              # black -> body
                px[xx, yy] = (*BODY, 255)
    # keep the game's own key: drop the Assets_Content_Textures_<Dir>_ prefix
    key = os.path.splitext(name)[0]
    if key.startswith("Assets_Content_Textures_"):
        rest = key[len("Assets_Content_Textures_"):]
        key = rest.split("_", 1)[1] if "_" in rest else rest
    piece.save(os.path.join(OUT, key + ".png"))
    manifest[key] = {"w": piece.width, "h": piece.height}
    found[hit] += 1

json.dump({"tint": {"body": body, "detail": detail, "swapped": swap},
           "tiles": manifest}, open(os.path.join(OUT, "manifest.json"), "w"),
          indent=1, sort_keys=True)
for k in sorted(found):
    print(f"  {k}: {found[k]}")
print("wrote", OUT)
