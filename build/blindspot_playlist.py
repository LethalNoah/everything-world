"""Make a private Spotify playlist of songs that fill the Everything playlist's blind spots.

Songs come from blindspot_songs.py, in order: lands that have no artist with 3+ songs, then genre gaps.
Each song is looked up on Spotify; songs already in playlist.csv are skipped.
Opens your browser once to allow playlist access (Spotify app from the game, PKCE login, no secret).
Writes blindspots.md: the playlist link and what was found per group.

    python build/blindspot_playlist.py
"""
import base64, csv, hashlib, http.server, json, os, secrets, sys, time, unicodedata, urllib.parse, urllib.request, webbrowser
from blindspot_songs import LANDS, GENRES

sys.stdout.reconfigure(encoding='utf-8')
HERE = os.path.dirname(os.path.abspath(__file__))
CONFIG = os.path.join(HERE, '..', 'Game', 'EverythingWorld', 'Assets', 'StreamingAssets', 'spotify_config.json')
cfg = json.load(open(CONFIG, encoding='utf-8'))
CLIENT, PORT = cfg['clientId'].strip(), cfg.get('port', 8888)
REDIRECT = f'http://127.0.0.1:{PORT}/callback'
API = 'https://api.spotify.com/v1'
NAME = 'Everything World: Blind Spots'

# ---------- login ----------
verifier = secrets.token_urlsafe(64)
challenge = base64.urlsafe_b64encode(hashlib.sha256(verifier.encode()).digest()).rstrip(b'=').decode()
state = secrets.token_urlsafe(12)
got = {}
class Callback(http.server.BaseHTTPRequestHandler):
    def do_GET(self):
        q = urllib.parse.parse_qs(urllib.parse.urlparse(self.path).query)
        if urllib.parse.urlparse(self.path).path != '/callback':
            self.send_response(404); self.end_headers(); return
        got.update({k: v[0] for k, v in q.items()})
        self.send_response(200); self.send_header('Content-Type', 'text/html; charset=utf-8'); self.end_headers()
        self.wfile.write('<p style="font-family:sans-serif;padding:3em">Done. You can close this tab.</p>'.encode())
    def log_message(self, *a): pass
server = http.server.HTTPServer(('127.0.0.1', PORT), Callback); server.timeout = 300
url = 'https://accounts.spotify.com/authorize?' + urllib.parse.urlencode(dict(client_id=CLIENT, response_type='code', redirect_uri=REDIRECT,
    code_challenge_method='S256', code_challenge=challenge, scope='playlist-modify-private playlist-modify-public', state=state))
print('Opening Spotify in your browser: click Agree.', flush=True)
webbrowser.open(url)
while 'code' not in got and 'error' not in got: server.handle_request()
if got.get('state') != state or 'code' not in got: sys.exit(f'Login failed: {got}')
tok = json.load(urllib.request.urlopen(urllib.request.Request('https://accounts.spotify.com/api/token', data=urllib.parse.urlencode(dict(
    grant_type='authorization_code', code=got['code'], redirect_uri=REDIRECT, client_id=CLIENT, code_verifier=verifier)).encode())))
TOKEN = tok['access_token']
print('Logged in.', flush=True)

def call(method, path, body=None, query=None):
    u = (path if path.startswith('http') else API + path) + ('?' + urllib.parse.urlencode(query) if query else '')
    req = urllib.request.Request(u, method=method, data=json.dumps(body).encode() if body is not None else None,
                                 headers={'Authorization': 'Bearer ' + TOKEN, 'Content-Type': 'application/json'})
    for attempt in range(4):
        try:
            with urllib.request.urlopen(req) as r:
                txt = r.read().decode(); return r.status, (json.loads(txt) if txt else {})
        except urllib.error.HTTPError as e:
            if e.code == 429: time.sleep(int(e.headers.get('Retry-After', '2')) + 1); continue
            return e.code, e.read().decode()
    return 429, 'rate limited'

# ---------- find the songs ----------
def norm(t): return ''.join(c for c in unicodedata.normalize('NFD', t.lower()) if unicodedata.category(c) != 'Mn').replace('&', 'and')
rows = list(csv.DictReader(open(os.path.join(HERE, 'playlist.csv'), encoding='utf-8-sig')))
have_uri = {r['Track URI'] for r in rows}
have_name = {(norm(r['Artist Name(s)'].split(';')[0]), norm(r['Track Name']).split(' - ')[0]) for r in rows}

def artist_ok(want, track):
    w = norm(want).replace('the ', '')
    return any(w in norm(a['name']) or norm(a['name']).replace('the ', '') in w for a in track['artists'])

def find(artist, song):
    queries = [f'track:"{song}" artist:"{artist}"', f'{artist} {song}'] if song else [f'artist:"{artist}"']
    for q in queries:
        code, res = call('GET', '/search', query=dict(q=q, type='track', limit=10))
        if code != 200: continue
        for t in res.get('tracks', {}).get('items', []):
            if not artist_ok(artist, t): continue
            if t['uri'] in have_uri or (norm(t['artists'][0]['name']), norm(t['name']).split(' - ')[0]) in have_name: continue
            return t
    return None

found, report, uris = {}, [], []
for group, items in list(LANDS.items()) + list(GENRES.items()):
    lines = []
    for artist, song in items:
        t = find(artist, song)
        if t and t['uri'] not in uris:
            uris.append(t['uri'])
            lines.append(f"- {t['artists'][0]['name']} - [{t['name']}](https://open.spotify.com/track/{t['id']})")
        else:
            lines.append(f"- *not found or already in the playlist:* {artist}{' - ' + song if song else ''}")
        time.sleep(.08)
    report.append((group, lines))
    print(f'{group}: {sum(1 for l in lines if not l.startswith("- *"))}/{len(items)}', flush=True)

# ---------- make the playlist ----------
desc = ('Songs to fill blind spots in the Everything playlist, in order: lands without an artist of 3+ songs, then genre gaps '
        '(reggaeton, K-pop, Afrobeats, South Asian, Arabic/Turkish/Persian, UK rap, drum & bass, gospel, ska, hip-hop, R&B, blues, reggae, pre-1960, bluegrass, classical).')
code, pl = call('POST', '/me/playlists', dict(name=NAME, description=desc, public=False))
if code not in (200, 201):
    _, me = call('GET', '/me')
    code, pl = call('POST', f"/users/{me['id']}/playlists", dict(name=NAME, description=desc, public=False))
if code not in (200, 201): sys.exit(f'Could not create the playlist ({code}): {pl}')
for i in range(0, len(uris), 100):
    code, res = call('POST', f"/playlists/{pl['id']}/items", dict(uris=uris[i:i + 100]))
    if code not in (200, 201):
        code, res = call('POST', f"/playlists/{pl['id']}/tracks", dict(uris=uris[i:i + 100]))
    if code not in (200, 201): sys.exit(f'Could not add songs ({code}): {res}')
link = pl['external_urls']['spotify']
with open(os.path.join(HERE, 'blindspots.md'), 'w', encoding='utf-8') as f:
    f.write(f'# {NAME}\n\n[Open the playlist on Spotify]({link}) ({len(uris)} songs, private).\n\n'
            'Made by `build/blindspot_playlist.py` from `build/blindspot_songs.py`. Groups appear in this order in the playlist.\n\n')
    for group, lines in report: f.write(f'## {group}\n\n' + '\n'.join(lines) + '\n\n')
print(f'Playlist made: {link} ({len(uris)} songs)')
