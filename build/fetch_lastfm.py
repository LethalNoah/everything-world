"""Fetch Last.fm data for the playlist: per-song tags, per-artist tags and similar artists.

Needs build/lastfm_key.txt (one line, the API key). Results are cached in
build/lastfm_cache.json, so rerunning only fetches songs and artists that are new.

Usage: python fetch_lastfm.py [limit]   (limit = only fetch this many new items, for testing)
"""
import csv, json, os, sys, time, urllib.parse, urllib.request, urllib.error

sys.stdout.reconfigure(encoding='utf-8')
HERE = os.path.dirname(os.path.abspath(__file__))
KEY = open(os.path.join(HERE, 'lastfm_key.txt'), encoding='utf-8').read().strip()
CACHE = os.path.join(HERE, 'lastfm_cache.json')
API = 'https://ws.audioscrobbler.com/2.0/'
UA = 'EverythingWorld/0.1 (personal playlist map)'
DELAY = 0.25  # Last.fm asks for no more than ~5 requests a second

cache = json.load(open(CACHE, encoding='utf-8')) if os.path.exists(CACHE) else {}
for k in ('track_tags', 'artist_tags', 'artist_similar'):
    cache.setdefault(k, {})

def call(method, **params):
    q = urllib.parse.urlencode(dict(method=method, api_key=KEY, format='json', autocorrect=1, **params))
    for attempt in range(4):
        try:
            req = urllib.request.Request(API + '?' + q, headers={'User-Agent': UA})
            with urllib.request.urlopen(req, timeout=20) as r:
                data = json.load(r)
            time.sleep(DELAY)
            return data
        except urllib.error.HTTPError as e:
            body = e.read().decode('utf-8', 'replace')
            try:
                data = json.loads(body)
            except ValueError:
                data = {}
            if data.get('error') in (6, 7):  # not found
                time.sleep(DELAY)
                return data
            if data.get('error') in (10, 26):  # bad or suspended key: stop now
                sys.exit(f'Last.fm rejected the API key: {data.get("message")}')
            time.sleep(2 ** attempt)
        except (urllib.error.URLError, TimeoutError, ValueError):
            time.sleep(2 ** attempt)
    return None  # leave uncached so a rerun retries it

def tags(data, root):
    if data is None:
        return None
    t = (data or {}).get(root, {}).get('tag', [])
    if isinstance(t, dict):
        t = [t]
    return [[x['name'].lower(), int(x.get('count', 0))] for x in t[:25] if int(x.get('count', 0)) > 0]

def similar(data):
    if data is None:
        return None
    a = (data or {}).get('similarartists', {}).get('artist', [])
    if isinstance(a, dict):
        a = [a]
    return [[x['name'], round(float(x.get('match', 0)), 3)] for x in a[:50]]

rows = list(csv.DictReader(open(os.path.join(HERE, 'playlist.csv'), encoding='utf-8-sig')))
from overrides import prim   # hand corrections in overrides.json
artists = sorted({prim(r) for r in rows})
jobs = []
for a in artists:
    if a not in cache['artist_tags']:
        jobs.append(('artist_tags', a, lambda a=a: tags(call('artist.gettoptags', artist=a), 'toptags')))
    if a not in cache['artist_similar']:
        jobs.append(('artist_similar', a, lambda a=a: similar(call('artist.getsimilar', artist=a, limit=50))))
for r in rows:
    uri = r['Track URI'].split(':')[-1]
    if uri not in cache['track_tags']:
        a = r['Artist Name(s)'].split(';')[0].strip()
        jobs.append(('track_tags', uri, lambda a=a, t=r['Track Name']: tags(call('track.gettoptags', artist=a, track=t), 'toptags')))

limit = int(sys.argv[1]) if len(sys.argv) > 1 else len(jobs)
jobs = jobs[:limit]
print(f'{len(jobs)} requests to make (~{len(jobs) * DELAY / 60:.0f}+ min)', flush=True)

def save():
    tmp = CACHE + '.tmp'
    json.dump(cache, open(tmp, 'w', encoding='utf-8'), ensure_ascii=False)
    os.replace(tmp, CACHE)

failed = 0
for i, (kind, key, fn) in enumerate(jobs, 1):
    res = fn()
    if res is None:
        failed += 1
        continue
    cache[kind][key] = res
    if i % 100 == 0:
        save()
        print(f'{i}/{len(jobs)} done, {failed} failed', flush=True)
save()
print(f'finished: {len(jobs) - failed} saved, {failed} failed (rerun to retry)')
