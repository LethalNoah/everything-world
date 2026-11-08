"""Export the scene groupings to a JSON file Unity can read.

Reads build/scenes_layout.json and writes
Game/EverythingWorld/Assets/_Project/Data/scenes.json with, per scene:
id, realm, tags, position/radius, sound averages and its tracks (name, artist, Spotify ID).

Only public info goes in (track names, artists, IDs), never the "Added By" usernames.
Rerun after scenes.py / layout_scenes.py:  python build/export_unity.py
"""
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
SRC = ROOT / "build" / "scenes_layout.json"
DST = ROOT / "Game" / "EverythingWorld" / "Assets" / "_Project" / "Data" / "scenes.json"


def main():
    layout = json.loads(SRC.read_text(encoding="utf-8"))
    artist_scene = {name: a["scene"] for name, a in layout["artists"].items()}

    tracks = {s["id"]: [] for s in layout["scenes"]}
    missing = 0
    for name, artist, track_id in layout["songs"]:
        scene = artist_scene.get(artist)
        if scene is None:
            missing += 1
            continue
        tracks[scene].append({"name": name, "artist": artist, "id": track_id})

    scenes = []
    for s in layout["scenes"]:
        scenes.append({
            "id": s["id"],
            "realm": s["realm"],
            "tags": s["tags"],
            "x": s["x"],
            "y": s["y"],
            "r": s["r"],
            "energy": s["sound"]["energy"],
            "acousticness": s["sound"]["acousticness"],
            "valence": s["sound"]["valence"],
            "danceability": s["sound"]["danceability"],
            "tempo": s["sound"]["tempo"],
            "neighbours": s["neighbours"],
            "tracks": sorted(tracks[s["id"]], key=lambda t: (t["artist"].lower(), t["name"].lower())),
        })

    DST.parent.mkdir(parents=True, exist_ok=True)
    DST.write_text(json.dumps({"scenes": scenes}, ensure_ascii=False, indent=1) + "\n", encoding="utf-8")
    total = sum(len(s["tracks"]) for s in scenes)
    print(f"Wrote {len(scenes)} scenes, {total} tracks to {DST.relative_to(ROOT)}"
          + (f" ({missing} songs had no scene)" if missing else ""))


if __name__ == "__main__":
    main()
