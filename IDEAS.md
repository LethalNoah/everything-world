# Ideas and open questions

Things to remember that aren't built or decided yet. Newest first.

## Surprise and spontaneity (2026-11-09, core to the theme, first version built)

Built (SurpriseDirector, SharedChance, all on the radio's shared clock, so friends on the same station share them): lights and fires follow the song's energy and pulse when a new song starts; on starry lands a comet in every song, a meteor shower in about one song in three, and the odd shooting star; the aurora with about one song in four (Lanternwood); five wandering lanterns, a new set with each song; snowmen that move each day (days start 5 am US Eastern). Test keys in play: F7 shooting star, F8 aurora, F9 a one-minute meteor shower. More ideas below.

The playlist is meant to be surprising and spontaneous, so the world shouldn't be the same every visit. Idea: drive surprises from the same shared clock as the radio, so friends see them together. Rare moments (a shooting star, the aurora on some nights, lanterns pulsing when a certain song starts), small things that shift by day or week (where snowmen stand, which fire is lit, heavier snow some days), and lights or weather reacting to the song playing (energy, mood).

## Claude sees the world itself (2026-11-09, next up)

- Run Unity from the command line (batch mode) to build the world, run the land audit and save screenshots from where you arrive, each artist area and above, so Claude can check a land without the owner testing every round. Unity has to be closed while it runs. The land audit and the `build-land` skill are already done.

## Saved for other lands (2026-11-10)

- **A crumbling stadium standing like a castle** (battlement stands, floodlight towers, a stage on the pitch): first built for the Kingdom of Classic Rock, taken out at the owner's request to save it for the stadium/arena rock land (Arena Rock Ridge).
- The Kingdom took a gentle "heavenly" touch (golden light through the Hall's dome, gold dust). If another land needs heaven as its main idea, keep the Kingdom's version understated.

## Social spaces and minigames (2026-11-09, later)

- A big Christmas lodge (probably in or near Lanternwood) you can go inside, sit down in and talk with friends.
- Pub songs and Christmas songs might get an area of their own one day. For now the sea shanties and pub songs (Drunken Sailor, Wellerman, Ratlin' Bog) and A Holly Jolly Christmas live on the Isle of Memes & Musicals.
- Minigames somewhere in the world.

## Performance (2026-11-10, first pass done)

- **Measured** with `python build/take_shots.py perf` (Unity closed): it times rendering from the hub and two busy lands, eight ways each, and tries switching suspects off, writing `Game/EverythingWorld/Logs/perf.txt`.
- **The cause was the terrain**, not the scenery: every distinct ground colour was its own material, and the built lands blend colours smoothly, so a terrain chunk had thousands of materials. Ground colours are now rounded to 24 shades per channel (`IslandWorldBuilder.Quantise`). The hub's view toward the Indie Isles went from 140 ms to 4 ms a frame; the busiest lands from about 58 ms to 3 ms.
- Also done: land-wide scenery and glow meshes are split into 80 m chunks (`MeshKit.Build`; use `BuildSingle` for a mesh that's moved as a whole), and `LandCuller` hides grass beyond 220 m, small props 400 m, glows 650 m, mid-sized scenery 900 m, big pieces 1.6 km, and lights out of reach. Triangles drawn from the hub fell from about a million to 50 thousand.
- **Memory (2026-11-10):** with eleven lands built, the meshes took 4.1 GB on the 8 GB graphics card, and frame times swung between 3 and 20 ms depending on what else was using it. Unity's static batching was keeping a second copy of all the scenery; it is now off (Player Settings, Standalone), which halved that to 2.1 GB at the same speed. `take_shots.py perf` now prints the memory and the biggest meshes.
- The grass is now most of what's left (about 1.5 GB: each land's swaying ground cover is 1-2.6 million vertices). With ~45 lands to go, that needs a fix before long: build grass only near the player, or thinner grass far from paths.
- Next, if it's ever needed: clouds (about 100 separate meshes per built land), simpler far versions of lands.

## A quest across the lands (2026-11-09, wanted, not designed yet)

- **One activity or minigame per land**, made for that land like everything else (its own idea, fitting its music), with something you have to do or finish in it.
- **Pedestals in the hub:** an empty pedestal for every land. Finishing a land's activity fills its pedestal (with something from that land). Filling every pedestal does something big (to be decided).
- **A passport (Sean's idea):** it gets stamped for each land you visit, each stamp designed for its land.
- **Collectibles or a scavenger hunt** hidden across the lands.
- Open questions: is progress per player or shared by friends playing together? Saved between sessions? What happens when every pedestal is full? Where does the passport show (a book you open, a page on the hub map)?
- Fits with what's built: the daily "something moves" happenings (the Driftwood bottle, the Fens heron) could become hunt items; the build-land skill should get a step for each land's activity once the system exists.
- Generally: buildings sized for groups (doorways 2.5 m+, rooms for 4-6 people), and places to sit and reflect in every land.

## Artist descriptions (2026-11-09, wanted, later)

Each artist area should have something to read about the artist, not just the plaque facts. Likely source: Last.fm's artist bio (`artist.getInfo`, already our tag source), trimmed to a few sentences. Last.fm bios are user-written wiki text under CC BY-SA, so show a small "from Last.fm" credit. Bigger artists (halls, temples) could get a longer text inside; shrines a line or two. Would need a fetch step in `build/` and a text field in the Unity export.

## Sky per land (2026-11-08, decided)

Each land keeps its own sky, light, fog and weather (its look brief's "mood"), always at its best time of day. There is no shared day and night. Crossing a border blends to the next land's sky over a few seconds, like the music switching. Descriptions of every land are in `WORLD_GUIDE.md`.

## World shape and scale (2026-11-08, decided, next to build)

After walking the first Unity blockout:
- **Scale 2.5×:** the game world becomes about 12 × 8 km (2.5 m per map pixel).
- **Every artist gets an area**, sized by song count: shrine (1 song), monument (2–5), hall (6–15), grand temple (16+). Plots are saved in `build/artist_plots.json` so they stay put when the playlist grows; islands keep spare land for new artists and a plaza for the genre landmark.
- **Artist radio** for artists with 3+ songs while you stand in their area; smaller shrines keep the island's genre radio.
- **Continents in the middle, islands at the edges** (liked, details to confirm): the big central realms become shared landmasses where scenes border each other like countries (borders marked by rivers, ridges or roads), small realms stay island chains at the edge. Proposed: The Old Kingdoms + Riffland as one central continent; The Indie Isles, The Chart Isles and The Groovelands as medium landmasses; The Bass Reaches, The Boroughs and The Isles of Story as island chains. Music similarity still decides who borders whom.
- **Land sized by need, not one fixed scale** (decided after measuring the 2.5× build, where small islands were packed full): each scene gets its artist plots, about 2× that again kept free for future artists, its landmark plaza, and at least ~0.4 km² of open land to wander. About 25–35 km² of land in total; the world's outer size follows from that.
- Built so far (uncommitted, will be redone after the playlist update): the 2.5× export with artist plots in `build/islandmap.py`, and the Unity island world (`Scripts/World/`, menu Everything World > Build Island World Scene).

## Sound beyond Spotify (2026-11-08, low priority)

Not seen as necessary, just possible:
- **Game ambience on top of Spotify:** Unity can play its own fully 3D sounds (waves at the Driftwood Shore, lava at the Metalcore Caldera, crowds near stages, footsteps) alongside the Spotify music.
- **Volume dips at zone edges:** soften the cut between stations with a quick volume dip. Only volume can be changed through the API. It's steppy, it changes the player's real Spotify volume, and some devices (iPhones) refuse it.

Not possible: EQ, effects or spatial/3D placement of the Spotify music itself, since the audio never passes through the game (and Spotify's terms forbid capturing it).

## Artist areas (2026-11-08)

Every artist gets their own area inside their scene, showing:
- some of their cover art and song names,
- a plaque with info about the artist.

This replaces the older idea of forming "kingdoms" from how many songs an artist has, which no longer feels right.

Facts that shape this (from `build/scenes_layout.json` and the playlist export):
- **Each artist lives in exactly one scene.** All their songs are in that scene, so an artist area never has to be split.
- **Artists already have a position** (x, y) inside their scene from `layout_scenes.py`, with similar artists placed near each other. Artist areas can sit at those spots.
- **Most artists are small:** 871 of 1,154 artists have 1 song, 154 have 2, and only 30 have 6 or more. Phish alone has 66 songs, then The Beatles (20), Grateful Dead (17) and Radiohead (15). Areas need to scale, from a single display for a one-song artist up to a large area for Phish.
- **29 songs cross scenes through a featured artist,** for example Skrillex feat. Ellie Goulding, or Rihanna feat. JAY-Z. 257 artists only appear as features and have no area of their own.

Open questions:
- What does an area look like at each size? For example a single display or stall for 1 song, a small plot for 2–5, a building or landmark for the biggest.
- What goes on the plaque: genre tags, years, the friend who added the most songs, a short bio from Last.fm?
- Where do cover images come from? `build/fetch_covers.py` and `build/cover_manifest.json` already exist from the 2D map.
- Should cross-scene features become visible links, for example a signpost or a path between the two artists' areas?
- Does an artist area have its own music, for example the radio playing only that artist's songs while you stand there, or does it stay part of the scene's station?

## Done

- **Zone radio** (2026-11-08): every scene is an always-on station on a shared clock, so friends hear the same part of the same song.
