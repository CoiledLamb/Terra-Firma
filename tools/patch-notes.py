"""Keep design/workshop/patch-notes.md current.

Lists commits since the last release that haven't been reviewed for the patch notes yet,
across the branches features land on, and prints the next version's notes for pasting.

  python tools/patch-notes.py                  unreviewed commits (noise flagged with ~)
  python tools/patch-notes.py skip-noise       mark every flagged ~ commit as reviewed
  python tools/patch-notes.py note <hash>...   mark commits as covered by a notes line
  python tools/patch-notes.py skip <hash>...   mark commits as nothing a player sees
  python tools/patch-notes.py release          print the next version's notes
  python tools/patch-notes.py since <hash>     start a new cycle after a release commit

Ledger: design/workshop/patch-notes-reviewed.txt. Branches: BRANCHES below.
"""
import re
import subprocess
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
NOTES = ROOT / "design" / "workshop" / "patch-notes.md"
LEDGER = ROOT / "design" / "workshop" / "patch-notes-reviewed.txt"
BRANCHES = ["items", "creatures"]

# Commits a player never sees: tracker edits, dev wishes, diagnostics, release bookkeeping.
NOISE = re.compile(
    r"^(TODO|R0\d|Design|Merge branch|Patch notes)\b|^Dev wish|^Dev:|diagnostic Chance|spawn trace|"
    r"harness|^tfpatch",
    re.IGNORECASE,
)


def git(*args):
    return subprocess.run(["git", *args], cwd=ROOT, capture_output=True, text=True,
                          check=True).stdout


def read_ledger():
    since, seen = None, {}
    for line in LEDGER.read_text(encoding="utf-8").splitlines():
        if not line.strip() or line.startswith("#"):
            continue
        parts = line.split(None, 2)
        if parts[0] == "since":
            since = parts[1]
        else:
            seen[parts[0]] = parts[1]
    return since, seen


def commits(since):
    refs = [b for b in BRANCHES if git("branch", "--list", b).strip()]
    out = git("log", "--no-merges", "--reverse", "--format=%h\t%ad\t%s", "--date=short",
              *refs, "^" + since)
    rows = []
    for line in out.splitlines():
        h, date, subject = line.split("\t", 2)
        branches = git("branch", "--contains", h, "--format=%(refname:short)").split()
        rows.append((h, date, subject, [b for b in BRANCHES if b in branches]))
    return rows


def mark(status, hashes, rows):
    by = {h: s for h, _, s, _ in rows}
    with LEDGER.open("a", encoding="utf-8") as f:
        for want in hashes:
            match = [h for h in by if h.startswith(want[:7]) or want.startswith(h)]
            if not match:
                print(f"  {want}: not an unreviewed commit since the last release")
                continue
            f.write(f"{match[0]} {status} {by[match[0]]}\n")
            print(f"  {match[0]} {status}")


def release_text():
    text = NOTES.read_text(encoding="utf-8")
    text = re.sub(r"<!--.*?-->", "", text, flags=re.DOTALL)
    m = re.search(r"^## (.+?)\n(.*?)(?=^## |\Z)", text, flags=re.DOTALL | re.MULTILINE)
    version, body = m.group(1), m.group(2)
    body = re.split(r"^### not landed yet", body, flags=re.MULTILINE)[0].strip()
    bad = [l for l in body.splitlines() if "—" in l or any(ord(c) > 126 for c in l)]
    return version, body, bad


def main(argv):
    since, seen = read_ledger()
    rows = [r for r in commits(since) if r[0] not in seen]
    cmd = argv[0] if argv else "list"

    if cmd == "list":
        if not rows:
            print(f"nothing unreviewed since {since}.")
        for h, date, subject, br in rows:
            flag = "~" if NOISE.search(subject) else " "
            print(f"{flag} {h} {date} [{','.join(br) or '?'}] {subject}")
        if rows:
            print("\n~ = probably nothing a player sees (skip-noise marks them all)")
    elif cmd == "skip-noise":
        mark("skip", [h for h, _, s, _ in rows if NOISE.search(s)], rows)
    elif cmd in ("note", "skip"):
        mark("noted" if cmd == "note" else "skip", argv[1:], rows)
    elif cmd == "release":
        version, body, bad = release_text()
        print(f"terra firma {version.split(' (')[0]}\n\n{body}")
        for l in bad:
            print(f"\n!! non-ASCII or em dash, fix before shipping: {l}", file=sys.stderr)
    elif cmd == "since":
        LEDGER.write_text(LEDGER.read_text(encoding="utf-8").split("since ")[0]
                          + f"since {argv[1]}\n", encoding="utf-8")
        print(f"new cycle after {argv[1]}; ledger cleared.")
    else:
        print(__doc__)


if __name__ == "__main__":
    main(sys.argv[1:])
