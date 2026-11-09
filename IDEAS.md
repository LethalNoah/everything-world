# Ideas and open questions

Things to remember that aren't built or decided yet. Newest first.

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
