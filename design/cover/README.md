# Terra Firma cover

Source and renders for the workshop `preview.png` (512 x 512). Jam session 2026-10-14.

**Plate.** Gabriel Rollenhagen, *Selectorum emblematum centuria secunda* (Utrecht, 1613), engraved by Crispijn van de Passe, plate 81 "LABORI PARS SIT FORTUNA" (a spade driven into a winged globe on a plinth). Scan: University of Illinois copy on archive.org, item `gabrielisrollenh00rol`, leaf 0183, 500 ppi, Public Domain Mark. The raw leaf is in `src/` for provenance; `src/spade_src1024.png` is the roundel cropped from it and `src/spade_ringmask1024.png` is a per-angle fit of the engraved ring's outer line (the ring is hand cut and about 6 px off a true circle at 1024).

**Palette.** Caves of Qud's named colors, verified against the wiki's Modding: Colors page: k #0f3b3a, K #155352, y #b1c9c3, Y #ffffff, c #40a4b9, C #77bfcf. Nothing outside the palette except blends between two palette colors.

**Build.** `cover.py` does everything from the two inputs: tritone mapping of the engraving (light lines on the dark field), a white highlight confined to the spade, the title warped into the four spandrels (Siegfried, hollow letters, outline drawn after the warp so its weight is constant), stepped inks lit from the upper left with stippled seams, and a hard posterise to the band colors. Every choice is a knob at the top of the file; the agreed configurations are the named `PRESETS` at the bottom.

```
python cover.py                      # renders every preset into out/
python cover.py bookmark_A1_stipple  # one preset
```

Needs Pillow and numpy. Renders at 4x internally, so expect a few seconds and ~1 GB of RAM per preset.

**Font.** Siegfried by Typographer Mediengestaltung (2001), from 1001fonts under their Free For Commercial Use licence; the licence text is in `fonts/`. Marvin Visions and Hoffmann were tried and rejected; they are not redistributable and are not included.

**Open idea** (bookmarked, not built): raise the cyan band so it starts at the base of the orb and wings, and curve the band seams like the segments of a lit sphere rather than straight diagonals.
