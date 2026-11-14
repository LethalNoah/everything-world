"""Radio station ids, shared by islandmap.py (world_layout.json) and export_unity.py (scenes.json).

Scenes use their scene id, the Everything Hub is 1000, and every artist with 3+ songs has a station of
their own whose id comes from their name, so it stays the same as the playlist grows.
"""
import json
import zlib
from pathlib import Path

HUB_ID = 1000
ARTIST_MIN_SONGS = 3

def artist_station(name):
    return 100000 + zlib.crc32(name.encode('utf-8')) % 900000

# Shared stations (station_groups.json): several artists, one station named for the group.
_groups_file = Path(__file__).with_name('station_groups.json')
GROUPS = {k: v for k, v in (json.loads(_groups_file.read_text(encoding='utf-8')) if _groups_file.exists() else {}).items() if not k.startswith('_')}
GROUP_OF = {a: g for g, members in GROUPS.items() for a in members}

def station_for(artist, songs):
    """The station an artist's area plays: their shared station, else their own (3+ songs), else none (0)."""
    if artist in GROUP_OF:
        return artist_station(GROUP_OF[artist])
    return artist_station(artist) if songs >= ARTIST_MIN_SONGS else 0
