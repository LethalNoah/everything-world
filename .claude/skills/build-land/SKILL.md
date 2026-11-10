---
name: build-land
description: Build the next land of Everything World in Unity in one thorough pass. Use when the owner asks to build, start or do a land (for example "let's do the Shoegaze Fens"), or to redo one. Covers asking what the music means, the land kit, artist areas, happenings, the land audit, and the checks to run before handing it over.
---

# Building a land

Every land is its own hand-made kit (see CLAUDE.md and WORLD_GUIDE.md). The look is never shared; the
systems are. This skill is the whole process, written from what kept needing fixes on the first lands
(Hub, Indie Downs, Bedroom Pop Meadows, Small Hours, Driftwood Shore). The goal: get the first version
right in one pass, so the owner only needs one or two checks in play.

## 1. Before building: ask, then read

- Ask the owner what this music means to them (AskUserQuestion is fine, but let them talk). Ask about the
  biggest artists and any songs that should trigger something. Keep personal stories out of the repo,
  the guide and commits: write the design in general terms.
- Read the land's WORLD_GUIDE.md entry and its `build/scene_art.json` palette and landmark.
- List its artists from `Game/EverythingWorld/Assets/_Project/Data/world_layout.json` (songs, song titles,
  radio station or not). Radio-station artists (`stationId != 0`) are the big ones.
- Map the land in Python first (cells of this land, other lands, water; where you arrive = `labelX/labelZ`;
  heights). Know where the sea is, where the borders with other lands are, and how big it is.
- If songs or artists belong elsewhere, fix it in `build/overrides.json` (`same_scene_as`, `album_artist`)
  and rebuild in the background: `scenes.py && layout_scenes.py && export_unity.py && islandmap.py`
  (about 6 minutes). Check no other land's counts changed.

## 2. Build it in one pass: the checklist

Write `Kits/<Land>Kit.cs` plus partial files (artists, places, life) and register it in `LandKit.For`.
Every item below is something that went wrong before.

**Layout and zones**
- Spread the artists round where you arrive, with room to wander between them: neighbours about 90-110 m apart,
  radio-station artists nearer the heart, nobody out at the borders. Too spread (the first Driftwood Shore:
  main artists on the borders, an empty middle) and too packed (the second Driftwood Shore and Lanternwood:
  everyone in one knot) both felt wrong. Use `ArtistPlacement.Spread` in ShapeGround (it keeps the layout's
  shape, so similar artists stay together, and centres it on the arrival point); pass `avoid` for the
  centrepiece and any long path or view to keep clear. A land with a reason to be compact (the Fens' islands
  round the cathedral) can pack tighter.
- Keep artists 45 m or more from other lands, out of water, and outside every radio ring (a radio ring is
  the artist's `r`; space artists by `r1 + r2 + 6`). Keep a corridor clear for any long path or view.
- Level the ground where a centrepiece needs it (pattern: `ShapeGround` height blending, as in the Hub and
  Driftwood).
- Nothing may cross the land's border: big buildings go where there's room (Small Hours lesson).

**Paths**
- Branch paths like a tree from the heart (each new path joins the nearest existing point), so they never
  cross. Lay each path as one seamless strip, mitred at bends, so nothing overlaps or flickers.
  Pattern: `DriftwoodKit.Walk` and `Boardwalk`.
- Start a joining path at the edge of the one it joins, not its centre. Leave railings open at junctions.

**Collision (the most common fix)**
- Everything you could walk into gets a collider: buildings, walls, furniture, rails and ropes, posts,
  trunks, rocks, and small props on the ground (buoys, pots, crates). Add a `solid.Box` or
  `ArtistBits.PostCollider` as you place each thing, not afterwards.
- Doorways at `HumanScale` sizes; interiors listed with `IndoorSpaces`.
- Leaves, blossom and anything meant to be walked through go in a separate MeshKit built with a name starting
  "Soft" (for example "Soft foliage"): the land audit skips those, so its "no collider" list stays real.

**Look and light (the owner cares a lot)**
- Bloom on (`land_moods.json` `bloom`), lots of light: lanterns along paths, glowing windows, fires,
  point lights at every artist area, and `GlowHalos` round glows. Keep glow colours saturated.
- Never put a halo inside a solid object (it shows as hard straight lines).
- Walls that meet must not overlap (fit side walls between front and back walls); no doubled floors.
  Windows: check `GlassBox` rotation.
- Its own swaying ground cover (`WindMesh`), and set `_EW_Wind` / `_EW_WindDir` only while you're on this
  land.
- Its own sky: `CloudLayer`, `SkyDirector.SetSongTint` / `SetSongRain`, a `land_moods.json` entry.
- Keep glow strengths low (about 1.5-2 for `WorldMaterials.Glow`) under heavy bloom, or colours blow out to white
  (the Fens cathedral glass did).
- `MeshKit.Prism` has no bottom cap: anything seen from below (platforms, floating things) needs a second
  prism pointing down, or a box.
- Water you add (lakes, marshes, pools) must follow the shape of the basin it sits in, never a big circle: where
  the ground beyond is lower, a circle hangs in the air like a dark floor you can walk under (Fens).
- The centrepiece must read clearly from a distance: a recognisable shape (an arch, a tree, a tower), not
  an abstract pile (the Driftwood A-frame was unreadable).

**Density**
- Fill the whole land, not just round the artists: scatter several kinds of things over the wilds on a
  jittered grid (pattern: `DriftwoodWilds.Wilds`): trees, small structures, benches with lanterns, ruins,
  things to discover. Landmarks at the edges (coast, hills) should be reachable by a path.

**Artist areas**
- Each artist gets its own small scene built from their actual songs and covers, plus plaque, cover
  board and lantern placed so they don't overlap the scene.
- Radio-station artists get a full scene with plenty around it (lights, props, things referencing song
  titles): the owner always asked for more here.
- Keep the owner's special requests exactly (song-triggered events, named designs).

**Variety and happenings**
- Things that happen now and then, decided by the shared clock so friends see the same thing
  (`SharedChance`: per song with `CurrentSong`, per day with `Day`): animals, passing things, weather
  events, light changes, something that moves each day. Pattern: `DriftwoodHappenings.cs`, `DownsLife`.
- Lights and glows answer the music (`SurpriseDirector.Energy`, song valence).
- Any song-triggered event (like the Driftwood rainbow) checks the track id.

## 3. Before handing it over

1. Compile with Roslyn (the `check.rsp` in the session scratchpad, or recreate it: all project scripts
   plus Unity and URP dlls). Add new files to it.
2. Simulate the artist placement in Python and print distances from the arrival point and from borders.
3. Update the land's WORLD_GUIDE.md entry (counts, arrival, vibe, artist areas, happenings) in general
   terms, and IDEAS.md for anything deferred.
4. Ask the owner to press Ctrl+R in Unity and Play. The **land audit** runs automatically (IslandWorldBuilder
   "Audit Lands") and writes `Game/EverythingWorld/Logs/LandAudit/<Land>.txt` and `.png`. Read both
   yourself (the picture with the Read tool): fix every "no collider" item that's real, every artist with
   problems, and big empty stretches, before asking the owner to look.
   The audit also photographs the land from above (`<Land>_top.png`); run `python build/land_map.py "<Land>"`
   to label it (`<Land>_map.png`) and look at it: every artist scene, path and landmark should be visible.
5. Then the owner's quality check in play. Ask for screenshots of anything unclear.
6. Commit when the owner approves: check `git status` for secrets first (keys, tokens, playlist.csv,
   display_names.json, Spotify config), commit the `.meta` files with new scripts, and push only when asked.
