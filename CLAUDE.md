# Everything World

A low-poly 3D world built from a shared Spotify playlist (1,856 songs and growing). Friends load in together, travel to an island that fits their mood, and walk around while that area's music plays. Each area's look is art-directed by its music. Exploring with music is the core; activities are secondary.

The owner is fairly new to Git and Unity: explain Git and Unity steps plainly, and ask before big decisions. Suggest a commit before big changes.

## Decisions (settled, don't relitigate)

- **Engine:** Unity 6 (6000.3), URP, low-poly style. No VR.
- **Multiplayer:** friends play together, but music is per player: each person hears the area they are standing in.
- **Music:** every player has Spotify Premium. The game controls each player's own Spotify app through the Spotify Web API (Spotify Connect). Sound comes from Spotify, never from Unity audio.
- **Grouping:** an artist's songs always stay together. An artist belongs to a "scene" (more than a strict genre). Similar scenes sit next to each other.
- **Growth:** the playlist keeps growing, so the world regenerates from data without reshuffling existing places.

## Repo layout

- `Game/EverythingWorld/` — the Unity project (open this folder in Unity Hub). Its own `.gitignore` excludes Library, Temp, Logs, UserSettings, Obj, Build(s).
- `build/` — Python data pipeline:
  - `fetch_lastfm.py` → `lastfm_cache.json` (Last.fm artist/song tags, similar artists; cached, reruns only fetch new items).
  - `scenes.py` → `scenes.json`: 1,154 artists into 59 scenes (Louvain on tag + similar-artist + sound similarity; oversized/loose scenes split again).
  - `layout_scenes.py` → `scenes_layout.json` and `review/data.js`: scenes into 8 realms (island chains), similar scenes adjacent, artists placed inside each scene.
  - Older scripts (`layout.py`, `geo.py`, `worldgen.py`, `taxonomy.py`, `classify.py`) built the original 2D map.
- `review/index.html` — browsable review of the scene groupings (approved by the owner). Live at sealteamtwo.github.io/everything-world/review/.
- `index.html`, `assets/`, `map_poster.png` — the original 2D parchment map on GitHub Pages. Reference only; keep it working.

Scene names and look briefs live in `build/scene_art.json` (readable version: `ART_PLAN.md`). `WORLD_GUIDE.md` describes how each area, the hub and the artist areas should feel. **The owner loves these descriptions: build every land in Unity from its WORLD_GUIDE.md entry** (arrival, vibe, artist areas) together with its scene_art.json palette and landmark, and keep the guide updated when a design changes. **Every land is built individually, as its own kit, never a shared template** (the owner's decision, 2026-11-09: "some of this music means so much to us and I want to give it all its due respect"). For each land: first ask the owner what the music means to them and look at its guide entry and artists together, then build its own landmark, scenery, sky and artist areas (the biggest artists get their own designs), then refine with the owner in play. Shared systems (sky, glows, radio, zone fields, surprises, floors, plaques, HumanScale) are reused; the look is not. A big inspiration is Radiohead's *Kid A Mnesia Exhibition*: music-led spaces with a strong mood, meant for wandering. Take that feeling as inspiration only, not its look or content. Ideas not yet built or decided are in `IDEAS.md`; add new ones there when the owner mentions them. **To build a land, use the `build-land` skill (`.claude/skills/build-land/SKILL.md`)**: the full checklist from the lands so far, and the land audit (`LandAudit.cs`) that writes a report and picture to `Game/EverythingWorld/Logs/LandAudit/` when you press Play.

## Music in the game

- Each scene is a zone **radio station** (`RadioSchedule.cs`): an always-on, shuffled schedule computed from the scene's tracks and a shared clock, so every player hears the same song at the same position without networking.
- Spotify is controlled through the Web API (`SpotifyMusicOutput.cs`), off by default behind "Use Spotify" on the Music object. Songs start "from their album" because the desktop app ignored plain track plays in testing. The game re-syncs if Spotify drifts more than 1 s from the station.
- Spotify development mode: max 5 users per app (owner needs Premium); users must be added in the dashboard's User Management.

## Secrets and private data (never commit)

- `build/lastfm_key.txt` — Last.fm API key.
- `build/playlist.csv` — Exportify export; contains friends' Spotify usernames.
- `build/display_names.json` — maps those usernames to the display names shown on artist plaques.
- Spotify app config and saved tokens (`spotify_config.json`, `spotify_tokens.json`) — ignored in `Game/EverythingWorld/.gitignore`. Use PKCE auth so no client secret is needed.
- Before any commit, check `git status` for keys, tokens or the CSV.

## Git LFS

`.gitattributes` stores models, textures, audio, fonts and other binaries under `Game/` in Git LFS. LFS is scoped to `Game/` on purpose: GitHub Pages can't serve LFS files, so the site's PNGs must stay plain Git. Unity text assets (`.unity`, `.prefab`, `.asset`, `.meta`, `.cs`…) are plain Git with LF line endings.

## Asset Store packages

The repo is public, so purchased Asset Store content must never be committed (store licences forbid sharing it, and it would fill Git LFS). `Game/EverythingWorld/.gitignore` uses an allow-list: everything directly in `Assets/` and every folder in `Packages/` is ignored except the project's own items. Keep our files under `Assets/_Project/`; to track a new top-level item on purpose, add a `!` line for it and its `.meta`. Anyone else opening the project imports the same packages from the Asset Store.

Owned packages (downloaded copies in `%APPDATA%/Unity/Asset Store-5.x`), and where they import:
- Look and light: Toon Shaders Pro (`Assets/Toon Shaders Pro`), Shader Toolbox for URP, Snapshot Shaders 2 (`Assets/DanielIlett`), Snapshot Shaders Pro, Retro Shaders Pro, Hologram Shaders Pro (all by Daniel Ilett); Lumen Stylized Light FX 2 (`Packages/com.distantlands.lumen`).
- Gameplay: Photon Fusion (`Assets/Photon`, multiplayer); Starter Assets First/Third Person URP (`Assets/StarterAssets`).
- Props, sound and effects: Sci-Fi Props Low Poly (`Assets/ithappy`), Universal Sound FX (Imphenzia), Elemental Orbs (`Assets/Piloto Studio`), 70 Fantasy Spells.
- Probably unused (photoreal or off-theme): the Leartes interiors, Tirgames sci-fi rooms and statues, PBR Materials Sampler, RPG Monster Bundle, UFPS.

When importing, untick `Packages/manifest.json` if the package includes it (most Daniel Ilett packages, ithappy, Tirgames and Starter Assets do), and add any dependency it needs through the Package Manager instead. The VintageBar package also tries to overwrite `Assets/Settings`, the URP global settings and the Readme: untick those.

## Unity conventions

- Asset Serialization is Force Text and meta files are visible (Unity defaults; keep them).
- Always commit a `.meta` file together with its asset; never delete `.meta` files by hand.
- Keep project code under `Assets/_Project/` (Scripts, Scenes, Prefabs, Materials, Art).
