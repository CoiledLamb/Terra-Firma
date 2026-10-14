# Terra Firma cover

Source and renders for the workshop `preview.png` (512 x 512). Jam session 2026-10-14.

**Plate.** Gabriel Rollenhagen, *Selectorum emblematum centuria secunda* (Utrecht, 1613), engraved by Crispijn van de Passe, plate 81 "LABORI PARS SIT FORTUNA" (a spade driven into a winged globe on a plinth). Scan: University of Illinois copy on archive.org, item `gabrielisrollenh00rol`, leaf 0183, 500 ppi, Public Domain Mark. The raw leaf is in `src/` for provenance; `src/spade_src1024.png` is the roundel cropped from it and `src/spade_ringmask1024.png` is a per-angle fit of the engraved ring's outer line (the ring is hand cut and about 6 px off a true circle at 1024).

**Palette.** Caves of Qud's named colors, verified against the wiki's Modding: Colors page: k #0f3b3a, K #155352, y #b1c9c3, Y #ffffff, c #40a4b9, C #77bfcf. Nothing outside the palette except blends between two palette colors.

**Build.** `cover.py` does everything from the two inputs. The `final` preset (picked 2026-10-14): the engraving mapped as light lines on the dark field, a white highlight confined to the spade, a continuous ink drift from white at the upper left through grey to shadow cyan at the lower right that runs through the seal, the title and the field alike, a hand-ruled hatch over the black field, the title warped into the four spandrels (Siegfried, solid black letters, grey outline drawn after the warp so its weight is constant), then a hard posterise to the base tones plus eight samples of the drift. Every choice is a knob at the top of the file; the agreed configurations are the named `PRESETS` at the bottom (`final`, and the earlier four-band `bookmark_A1_stipple`). `out/<preset>_512.png` is the cover; `_smooth_512` and `_smooth_1024` are the un-posterised renders.

```
python cover.py          # renders every preset into out/
python cover.py final    # one preset
```

Needs Pillow and numpy. Renders at 4x internally, so expect a few seconds and ~1 GB of RAM per preset.

**Font.** Siegfried by Typographer Mediengestaltung (2001), from 1001fonts under their Free For Commercial Use licence; the licence text is in `fonts/`. Marvin Visions and Hoffmann were tried and rejected; they are not redistributable and are not included.

**Tried and dropped** (all still available as knobs): stepped four-ink bands with stippled seams, a cast shadow of the spade, a dark-cyan halftone shadow pass, curved band geometry, outline breaks, hollow letters over the hatched field.
