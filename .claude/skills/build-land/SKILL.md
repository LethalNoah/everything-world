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

- Look at the neighbouring lands (built ones and their guide entries) and make this one clearly different from them: time of day, brightness, palette and its big idea. Give it one strong imaginative central idea (fantasy, sci-fi, supernatural or dreamlike imagery is welcome); realistic countryside alone reads as bland.

## The heart of it: a designed, lived-in land (the owner's standard from 2026-11-12)

Lands must not feel generated. "You walk to the middle, see the artist areas, and there's a ton of empty space outside that... it can't really be random if it's going to feel right." So every land is composed, like the Oddball Backroads (`OddballPlaces.cs`, `OddballPlacesMore.cs`):
- **A road or path network over the whole land**, not just round the heart: a loop round the artists plus roads out to the far corners and the coast. Check the overhead picture (`Logs/LandAudit/<Land>_top.png`): no big region without a road.
- **Places made to stop at, all over the land** (10 to 16 of them, picked from the land's real heights: the highest hill, a flat strip by the sea...). Each has a purpose and a vibe, **somewhere to sit** (`Interact.Seat`/`Bench`), **something to play with** (`Interact.Toy`, `Interact.Carry` for things to pick up and throw, `Pushable`), a light, a sign, and **residents** (`Residents.Make`: who lives there and what they do, with lines to say). Keep the artists clear of them (`ArtistPlacement.Spread` avoid list).
- **The countryside between them designed too:** fields or gardens along the roads, fences, hedgerows, one tree per hilltop, groves in the hollows. Use random scatter only as texture inside those shapes.
- **Strong contrast** by brightness and blue/yellow (the owner is colourblind): bright surfaces against dark ones (the Metalcore Caldera is the model; the Djent Spires lack it).
- **Shot markers** ("Shot: ...") at every place, so the photo tour shows each one.
- Plants you would walk into (corn, sunflowers) go in a mesh named "Soft ..." so the audit treats them as walk-through on purpose.

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
- Shaping ground: Unity's `Mathf.SmoothStep(a, b, t)` blends between a and b, it is not GLSL smoothstep (a hill shaped with it came out 6 m instead of 24 m): write your own ease (pattern: `GrungeKit.Ease`).
- Level the ground where a centrepiece needs it (pattern: `ShapeGround` height blending, as in the Hub and
  Driftwood).
- Nothing may cross the land's border: big buildings go where there's room (Small Hours lesson).

**Paths**
- Branch paths like a tree from the heart (each new path joins the nearest existing point), so they never
  cross. Lay each path as one seamless strip, mitred at bends, so nothing overlaps or flickers.
  Pattern: `DriftwoodKit.Walk` and `Boardwalk`.
- Start a joining path at the edge of the one it joins, not its centre. Leave railings open at junctions.

- An artist's radio ring (their `r`) must contain their whole scene: a pier, a garden, a jetty. If the scene
  is bigger, grow `r` in ShapeGround; a ring that cuts across the scene feels like the zone ends abruptly
  (Phoebe Bridgers' pier).
- Anything big built relative to an artist (a house, a pier, a lake, stairs) levels its own ground in
  ShapeGround, and keeps working wherever the artist ends up: artists move when spacing changes, and a
  house that lands on a slope rises on stilts and throws its ramps over everything (Lanternwood's boathouse).
- Ramps and stairs never overlap another walkway; if a pier or path starts at a door, there's no ramp there.

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

- Ground colours by height must only catch the feature they're for: a "below the water is mud" rule also
  caught the low coast of the Fens and made a black band. Limit each band to its own place (or a narrow
  height range), and check the top-down photo.
- Nothing floats: tables, benches, shelves, platforms and signs need legs, posts or brackets down to what
  holds them (the Electric Fields' picnic tables were legless planks).
- Curved things (hulls, canopies, arches, domes) are built as continuous surfaces (`MeshKit.Quad` strips over
  a profile), not boxes turned at angles: the first boat sheds and willows were boxes and looked strange.
  Trees and other hero props deserve real shape: a trunk that splits into limbs, foliage hanging from them.

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

- Every artist scene must read from 20 m away: something at least 2.5 m tall or wide and lit. Small props on
  their own vanish (the Fens' microphone and toy car); make them giant or put them on a stage.

**Variety and happenings**
- Things that happen now and then, decided by the shared clock so friends see the same thing
  (`SharedChance`: per song with `CurrentSong`, per day with `Day`): animals, passing things, weather
  events, light changes, something that moves each day. Pattern: `DriftwoodHappenings.cs`, `DownsLife`.
- Lights and glows answer the music (`SurpriseDirector.Energy`, song valence).
- Any song-triggered event (like the Driftwood rainbow) checks `MusicDirector.ForcedTrackId ?? the track id`,
  and the kit lists it in `SpecialSongs` (song, label, the artist where it happens) so the photo tour can force
  it and photograph it there. Make song moments steady and big while the song plays (not quick bursts), or a
  photo (and a player arriving mid-song) misses them.
- Kits whose artist scenes don't face where you arrive override `ArtistFront` (garages face the street), so
  the tour photographs their fronts.

**Shared systems and performance**
- When you change a shared system (placement, sky, glows, the audit), re-check every land that uses it:
  its photo, and the owner's next play-test. One change to spacing moved two lands at once.
- Performance: never give a land's `Ground()` its own unlimited colours per spot for materials elsewhere
  (the terrain rounds them; a material per distinct colour was the big slowdown). Build scenery through
  `MeshKit.Build` (it chunks land-wide meshes so far parts can be skipped) and name walk-through foliage
  "Soft..." (the land audit skips those for colliders; only swaying grass is culled at 220 m). After building a land, run `python build/take_shots.py perf` and
  check the hub and the land stay under about 10 ms (see IDEAS.md, Performance).

**Performance and memory**
- After building, run `python build/take_shots.py perf` (Unity closed) and add the new land to its list. Keep frames under about 10 ms and watch the memory line: mesh memory near half the graphics card makes timings swing wildly. Keep ground cover modest (under about 1.5 million vertices per land).

## 3. Before handing it over

**The standard every land must meet** (the audit's numbers; check them before handing over, and after any
big change):
- **Things you could walk through: under 50.** Read the audit's "All of them by kind" list (and the full
  `<Land>_colliders.txt`), match each material's hex colour to the kit's `Color` literals, and sort every big
  group into one of three: *real* (a solid thing with no collider: add `solid.Box`/`PostCollider`/`BoxCol`
  sized to the shape), *visual* (a flat thing floating, see below: fix the look, never give a decal a
  collider), or *harmless* (glows, water, grass, light, things above head height: leave). Whatever is left must
  be harmless, and say what it is.
- **Overlapping surfaces: 0** (or each one explained).
- **Artists with problems: 0.** **Empty: under 10%** of the land.
- **No flat thing laid on a slope.** A disc, decal, leaf patch, rug or path piece placed at its centre's
  ground height floats over the downhill side, up to a metre in the air (this was most of the Escarpment's 474
  reports). Build ground cover from small pieces each set with `ctx.OnGround`, or skip steep ground.
- **Nothing hangs at head height** (lanterns on arms, chimes, signs, branches, foliage between 0.45 and 2 m):
  hang it higher or make it solid.
- **Inland water:** if the land has its own water above sea level (a marsh, a swamp, a lake), register each
  piece with `LandAudit.AddWater(Land, x, z, radius, surfaceY)` (radius 0 = the whole land), so lily pads and
  things standing in it are measured from the water, not the bed.
- **Every path walkable:** no stretch steeper than about 40 degrees (shaped ground like terraces or a summit
  can make one stand up like a ladder: soften the shaping or widen the blend).
- To audit every land at once, run the photo tour for a single small land (e.g. `python build/take_shots.py
  "The Everything Hub"`): the world loads every land and audits all of them.

1. Compile with Roslyn (the `check.rsp` in the session scratchpad, or recreate it: all project scripts
   plus Unity and URP dlls). Add new files to it.
2. Simulate the artist placement in Python and print distances from the arrival point and from borders.
3. Update the land's WORLD_GUIDE.md entry (counts, arrival, vibe, artist areas, happenings) in general
   terms, and IDEAS.md for anything deferred.
4. Ask the owner to press Ctrl+R in Unity and Play. The **land audit** runs automatically (IslandWorldBuilder
   "Audit Lands") and writes `Game/EverythingWorld/Logs/LandAudit/<Land>.txt` and `.png`. Read both
   yourself (the picture with the Read tool): fix every "no collider" item that's real, every
   **overlapping surface** (two different materials in the same plane: they flicker in play; magenta on the
   picture), every artist with problems, and big empty stretches, before asking the owner to look.
   The audit also photographs the land from above (`<Land>_top.png`); run `python build/land_map.py "<Land>"`
   to label it (`<Land>_map.png`) and look at it: every artist scene, path and landmark should be visible.
   **Look at it yourself from eye level** before the owner does: with the Unity editor closed (ask the owner
   to close it), run `python build/take_shots.py "<Land>"`. Unity photographs the land in the background with
   Spotify off (from where you arrive looking four ways, in front of every artist area, one high view, and
   inside every room the kit registered with `IndoorSpaces`, so register every interior; it also reruns the audit) and makes contact sheets in `Game/EverythingWorld/Logs/Shots/<Land>/sheet_*.png`. Read
   the sheets (and single photos for detail): fix anything that looks wrong, odd or unreadable (props built
   from boxes at angles, overlaps, floating things, blown-out glows), and shoot again.
5. Then the owner's quality check in play. They press **P** (or F12) at anything odd: the game view goes to
   `Game/EverythingWorld/Logs/Live/` with a line in `notes.txt` (land, position, facing, nearest artist, song,
   indoors). Read the newest ones when they say "look at my snapshots".
6. Commit when the owner approves: check `git status` for secrets first (keys, tokens, playlist.csv,
   display_names.json, Spotify config), commit the `.meta` files with new scripts, and push only when asked.
