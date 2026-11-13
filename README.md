# The Everything World

### 🗺️ [Explore the map: sealteamtwo.github.io/everything-world](https://sealteamtwo.github.io/everything-world/)

A shared Spotify playlist of 2,166 songs (and growing), turned into a world. It started as a parchment map. It is now becoming a low-poly 3D world to walk around with friends, where every land looks and feels like the music that lives there, and that music plays while you are in it.

[![The map of the Everything World: 67 isles in 8 realms, with a flag on each land built so far](map_poster.png)](https://sealteamtwo.github.io/everything-world/)

## Where the project is now

*November 2026*

- **The world is charted.** 1,322 artists are grouped into 66 "scenes" (looser than genres: an artist's songs always stay together, and similar scenes are neighbours). Each scene is a land. The lands form 8 realms round a central Hub, 67 places in all.
- **62 lands and the Hub are built** in Unity. The blue flags on the map mark them.
- **Every built land is made by hand for its own music**, not from a shared template: its own landmark, sky, light, ground and weather; an area for every artist, built from their actual songs; and things that happen when particular songs come on.
- **Each land is laid out to be wandered.** Roads lead out from where you arrive to named places: a pizza parlour in the Punk Badlands, a summer festival in the J-Rock Isles, a flooded quarry in the Nu Metal Quarry. There are places to sit, things to use, things to pick up and throw, and small residents with something to say.
- **Not done yet:** the other 4 lands, playing together online, and a lot of polish. So far it has only been walked by the person building it.

There are no pictures of the game here yet, on purpose. The map has the written description of every built land: click one.

## Using the map

- **Zoom in** with the scroll wheel or a pinch. Artists appear first, then songs, then album covers.
- **Click a land** for what it is like in the game (if it is built), the plan for how it should look, and every song that lives there.
- **Click a song** for a Spotify player and its neighbours.
- **Search** for any song, artist, land or tower. **Wander** flies you to a random song.
- **Blue flags** mark the lands built so far. Towers are the fast-travel points, one per realm.

The [original map](https://sealteamtwo.github.io/everything-world/original/) from the start of the project is still there: the same playlist when it was 1,856 songs, as one continent of 44 lands.

## How the music shapes the world

- **Where a song lives** comes from its artist. Artists are grouped by their Last.fm tags, by which artists listeners find similar, and by how the songs sound. A few were placed by hand, so some of the geography is a matter of opinion.
- **Neighbours are similar music.** Grunge sits beside alt rock, shoegaze beside bedroom pop, and the lands of one realm share a family resemblance.
- **Each land's look comes from its music:** its palette, landmark, light and weather were written for that scene before anything was built. The plans are in [ART_PLAN.md](ART_PLAN.md) and the descriptions of the built lands in [WORLD_GUIDE.md](WORLD_GUIDE.md).
- **The playlist keeps growing**, so the world is generated from data and new songs find their place without reshuffling the lands that exist.

## How the music plays in the game

- Every land is a **radio station**: an always-on shuffle of that land's songs, worked out from a shared clock, so friends standing in the same land hear the same song at the same moment.
- The sound comes from **Spotify itself**. The game controls each player's own Spotify app, so every player needs Spotify Premium. Music is per player: you hear the land you are standing in.
- Artists with enough songs have their own station inside their area.

## What is in this repository

- `index.html`, `world/`, `assets/`: the map website (plain HTML, CSS and JavaScript, no build step). `original/` is the first map.
- `build/`: the Python scripts that group the artists into scenes, lay out the world, draw the map and export the world for Unity.
- `Game/EverythingWorld/`: the Unity project (Unity 6, URP). The world is built by code when you press Play, from the exported data.
- `WORLD_GUIDE.md`, `ART_PLAN.md`, `IDEAS.md`: what each land is like, the plan each was built from, and ideas not yet built.

Not included: the playlist export itself, API keys, and the Asset Store packages the Unity project uses (their licences don't allow sharing them), so the Unity project will not open cleanly from a fresh download.

## Charting your own playlist

The original parchment map can be made from any playlist. The scripts need Python with numpy, scipy, pillow and contourpy.

1. Export a playlist as CSV with [Exportify](https://exportify.net) and save it as `build/playlist.csv`.
2. From the `build` folder, run these in order:

```bash
python fetch_covers.py
```

```bash
python classify.py
```

```bash
python geo.py
```

```bash
python worldgen.py
```

`classify.py` lists any artist it can't place. Add those to `build/taxonomy.py`. Land positions, names and terrain recipes are in `build/layout.py`, and those will need adapting to your own music. `geo.py` takes an optional seed number for a different coastline.

## Credits

Album covers belong to their respective artists and labels, and are shown here to identify the music. Map fonts are [IM Fell English](https://fonts.google.com/specimen/IM+Fell+English) and [Cinzel Decorative](https://fonts.google.com/specimen/Cinzel+Decorative), both under the SIL Open Font License. Not affiliated with Spotify.
