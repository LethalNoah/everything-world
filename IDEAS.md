# Ideas and open questions

Things to remember that aren't built or decided yet. Newest first.

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
