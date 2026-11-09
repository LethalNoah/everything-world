"""Radio station ids, shared by islandmap.py (world_layout.json) and export_unity.py (scenes.json).

Scenes use their scene id, the Everything Hub is 1000, and every artist with 3+ songs has a station of
their own whose id comes from their name, so it stays the same as the playlist grows.
"""
import zlib

HUB_ID = 1000
ARTIST_MIN_SONGS = 3

def artist_station(name):
    return 100000 + zlib.crc32(name.encode('utf-8')) % 900000
