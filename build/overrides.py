"""Hand corrections to how songs are grouped. Edit overrides.json, not this file."""
import json, os

_o = json.load(open(os.path.join(os.path.dirname(os.path.abspath(__file__)), 'overrides.json'), encoding='utf-8'))
ALBUM_ARTIST = _o.get('album_artist', {})   # album name prefix -> the one artist its songs count as
SAME_SCENE_AS = _o.get('same_scene_as', {}) # artist -> artist whose scene it joins


def prim(row):
    """The artist a playlist row counts as: its first credited artist, unless its album is listed in album_artist."""
    album = row['Album Name'].lower()
    for prefix, name in ALBUM_ARTIST.items():
        if album.startswith(prefix.lower()):
            return name
    return row['Artist Name(s)'].split(';')[0].strip()
