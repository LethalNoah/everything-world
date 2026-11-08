"""Group the playlist's artists into scenes.

An artist's songs always stay together. Artists are joined by how alike they are:
  - Last.fm tags (artist tags + song tags) and Spotify genres, compared as weighted tag lists
  - Last.fm "similar artists" links between artists that are both in the playlist
  - average sound (energy, acousticness, ...) from the Spotify export
Then a community-detection pass splits that similarity network into scenes.

Writes scenes.json. Needs lastfm_cache.json from fetch_lastfm.py.
Usage: python scenes.py [resolution]   (higher resolution = more, smaller scenes)
"""
import csv, json, math, os, re, sys, collections
import numpy as np
import networkx as nx

sys.stdout.reconfigure(encoding='utf-8')
HERE = os.path.dirname(os.path.abspath(__file__))
RES = float(sys.argv[1]) if len(sys.argv) > 1 else 1.0

W_TAGS, W_LINKS, W_SOUND = .55, .35, .10   # how much each kind of evidence counts
K_EDGES = 12                               # strongest links kept per artist
MIN_SONGS = 12                             # smaller scenes are merged into their closest neighbour
MAX_SONGS = 90                             # bigger scenes are split again
SPLIT_RES = 1.0
LOOSE = .25                                # scenes of 48+ songs less alike than this are split again

# ---------- load ----------
rows = list(csv.DictReader(open(os.path.join(HERE, 'playlist.csv'), encoding='utf-8-sig')))
cache = json.load(open(os.path.join(HERE, 'lastfm_cache.json'), encoding='utf-8'))
old_region = json.load(open(os.path.join(HERE, 'artist_region.json'), encoding='utf-8'))

def prim(r): return r['Artist Name(s)'].split(';')[0].strip()
by_artist = collections.defaultdict(list)
for r in rows: by_artist[prim(r)].append(r)
artists = sorted(by_artist)
ai = {a: i for i, a in enumerate(artists)}
N = len(artists)
nsongs = np.array([len(by_artist[a]) for a in artists])

# ---------- tags ----------
JUNK = {'seen live', 'favorites', 'favourites', 'favorite', 'love', 'awesome', 'beautiful', 'all',
        'male vocalists', 'female vocalists', 'male vocalist', 'female vocalist', 'my top songs',
        'american', 'usa', 'united states', 'british', 'uk', 'english', 'england', 'canadian', 'canada',
        'australian', 'australia', 'new york', 'california', 'los angeles', 'spotify', 'vocal', 'vocalists',
        'under 2000 listeners', 'favourite', 'cool', 'good', 'great', 'best', 'amazing', 'sexy', 'chill',
        'guitar', 'guitarist', 'composer', 'producer', 'band', 'singer', 'musician', 'american band', 'albums i own'}
SYN = {'rnb': 'r&b', 'rhythm and blues': 'r&b', 'hiphop': 'hip hop', 'electronica': 'electronic',
       'trip hop': 'trip hop', 'drum n bass': 'drum and bass', 'dnb': 'drum and bass', 'post punk': 'post punk',
       'alt rock': 'alternative rock', 'alternative': 'alternative rock', 'prog rock': 'progressive rock',
       'prog': 'progressive rock', 'indie': 'indie rock', 'jam': 'jam band', 'post rock': 'post rock',
       'soundtracks': 'soundtrack', 'ost': 'soundtrack', 'video game music': 'video game', 'vgm': 'video game',
       'game music': 'video game', 'videogame': 'video game', 'chiptune': 'video game'}
DECADE = re.compile(r"^(19|20)?\d0'?s$")

def norm(t):
    t = t.lower().strip().replace('-', ' ').replace('_', ' ')
    t = re.sub(r'\s+', ' ', t)
    return SYN.get(t, t)

raw = [collections.Counter() for _ in artists]
for a in artists:
    i = ai[a]; low = a.lower()
    def add(t, w):
        t = norm(t)
        if not t or t in JUNK or t == low or len(t) > 30: return
        if DECADE.match(t): w *= .3          # decades link unrelated music, so they count for little
        raw[i][t] += w
    for t, c in cache['artist_tags'].get(a) or []:
        add(t, c / 100)
    songs = by_artist[a]
    for r in songs:
        for t, c in cache['track_tags'].get(r['Track URI'].split(':')[-1]) or []:
            add(t, .7 * c / 100 / len(songs))
    sp = collections.Counter(g.strip() for r in songs for g in r['Genres'].split(',') if g.strip())
    for g in sp:
        add(g, .6)
    if not raw[i] and a in old_region:      # nothing known: fall back to the old map's hand placement
        raw[i]['region:' + old_region[a]] += 1

df = collections.Counter(t for c in raw for t in c)
vocab = sorted(t for t, n in df.items() if n >= 2)
vi = {t: j for j, t in enumerate(vocab)}
idf = np.array([math.log(N / df[t]) for t in vocab])
X = np.zeros((N, len(vocab)))
for i, c in enumerate(raw):
    for t, w in c.items():
        if t in vi: X[i, vi[t]] = w
X *= idf
X /= np.linalg.norm(X, axis=1, keepdims=True) + 1e-9
T = X @ X.T

# ---------- Last.fm similar-artist links ----------
low2i = {a.lower(): i for i, a in enumerate(artists)}
L = np.zeros((N, N))
for a in artists:
    for b, m in cache['artist_similar'].get(a) or []:
        j = low2i.get(b.lower())
        if j is not None and j != ai[a]:
            L[ai[a], j] = max(L[ai[a], j], m)
L = np.maximum(L, L.T)
L = np.sqrt(L)   # Last.fm match scores drop off fast; this keeps weaker real links meaningful

# ---------- sound ----------
FEATS = ['Energy', 'Acousticness', 'Valence', 'Danceability', 'Instrumentalness', 'Speechiness']
def fnum(x):
    try: return float(x)
    except ValueError: return np.nan
F = np.array([[np.nanmean([fnum(r[f]) for r in by_artist[a]]) for f in FEATS] for a in artists])
F = np.where(np.isnan(F), np.nanmean(F, 0), F)
F = (F - F.mean(0)) / (F.std(0) + 1e-9)
D2 = ((F[:, None, :] - F[None, :, :]) ** 2).sum(-1)
A = np.exp(-D2 / (2 * 2.0 ** 2))

S = W_TAGS * T + W_LINKS * L + W_SOUND * A * (T > 0)
np.fill_diagonal(S, 0)

# Artists we know almost nothing about (no shared tags, or only the old map's guess) would clump
# together just for being unknown. Leave them out of the clustering and attach each one to the
# known artist it is closest to: same old-map region if possible, then closest sound.
known_tags = (X[:, [vi[t] for t in vocab if not t.startswith('region:')]] > 0).sum(1)
weak = (known_tags == 0) & (L.max(1) == 0)

# ---------- graph + communities ----------
G = nx.Graph(); G.add_nodes_from(range(N))
strong = np.where(~weak)[0]
G = nx.Graph(); G.add_nodes_from(strong)
for i in strong:
    for j in strong[np.argsort(-S[i, strong])[:K_EDGES]]:
        if S[i, j] > 0: G.add_edge(int(i), int(j), weight=float(S[i, j]))
comms = nx.community.louvain_communities(G, weight='weight', resolution=RES, seed=7)
label = np.full(N, -1)
for k, c in enumerate(comms):
    for i in c: label[i] = k
for i in np.where(weak)[0]:
    same = [j for j in strong if old_region.get(artists[j]) == old_region.get(artists[i])] if artists[i] in old_region else []
    pool = np.array(same if same else strong)
    label[i] = label[pool[np.argmax(A[i, pool])]]

def cohesion_of(m):
    if len(m) < 2: return 1.0
    sub = S[np.ix_(m, m)]; w = nsongs[m]
    return float((sub * np.outer(w, w)).sum() / (np.outer(w, w).sum() - (w * w).sum()))

# split scenes that are too big or too loose: rerun the community pass inside them
def split_big():
    global label
    changed = True
    while changed:
        changed = False
        for k in sorted(set(label)):
            m = np.where(label == k)[0]
            sz = nsongs[m].sum()
            loose = sz > 2 * MIN_SONGS * 2 and cohesion_of(m) < LOOSE
            if (sz <= MAX_SONGS and not loose) or len(m) < 6: continue
            sub = G.subgraph([int(i) for i in m if not weak[i]])
            parts = nx.community.louvain_communities(sub, weight='weight', resolution=SPLIT_RES, seed=7)
            parts = [p for p in parts if nsongs[list(p)].sum() >= MIN_SONGS]
            if len(parts) < 2: continue
            nxt = label.max() + 1
            for n, p in enumerate(parts[1:]):
                for i in p: label[i] = nxt + n
            # leftovers (tiny parts, weak artists) join the part they are most linked to
            new_ks = [k] + [nxt + n for n in range(len(parts) - 1)]
            for i in m:
                if any(i in p for p in parts): continue
                label[i] = max(new_ks, key=lambda q: S[i, label == q].mean() if (label == q).any() else -1)
            changed = True
split_big()

def songs_in(k): return int(nsongs[label == k].sum())
def centroid(k):
    m = label == k
    return (X[m] * nsongs[m, None]).sum(0)

# merge scenes that are too small into the scene they are most linked to
while True:
    ks = sorted(set(label), key=songs_in)
    small = [k for k in ks if songs_in(k) < MIN_SONGS]
    if not small or len(ks) == 1: break
    k = small[0]; m = label == k
    link = collections.Counter()
    for k2 in ks:
        if k2 != k: link[k2] = S[np.ix_(m, label == k2)].sum() / math.sqrt(songs_in(k2))
    label[m] = link.most_common(1)[0][0]
ks = sorted(set(label), key=lambda k: -songs_in(k))
label = np.array([ks.index(k) for k in label])
K = len(ks)

# ---------- describe ----------
gmean = (X * nsongs[:, None]).sum(0) / nsongs.sum()
def top_tags(k, n=8):
    c = centroid(k) / songs_in(k)
    score = c - .5 * gmean
    return [vocab[j] for j in np.argsort(-score)[:n] if score[j] > 0]

def cohesion(k): return cohesion_of(np.where(label == k)[0])

scenes = []
for k in range(K):
    m = np.where(label == k)[0]
    members = sorted(m, key=lambda i: -nsongs[i])
    old = collections.Counter(old_region.get(artists[i], '?') for i in m for _ in range(nsongs[i]))
    scenes.append(dict(id=k, songs=songs_in(k), artists=[artists[i] for i in members],
                       tags=top_tags(k), cohesion=round(cohesion(k), 3), old_regions=old.most_common(4)))

# scene-to-scene similarity, used later for the layout
C = np.array([centroid(k) for k in range(K)])
C /= np.linalg.norm(C, axis=1, keepdims=True) + 1e-9
SS = np.zeros((K, K))
for a in range(K):
    for b in range(K):
        if a != b:
            ma, mb = label == a, label == b
            SS[a, b] = .6 * float(C[a] @ C[b]) + .4 * L[np.ix_(ma, mb)].sum() / math.sqrt(ma.sum() * mb.sum())

json.dump(dict(resolution=RES, scenes=scenes, scene_similarity=np.round(SS, 4).tolist(),
               artist_scene={artists[i]: int(label[i]) for i in range(N)}),
          open(os.path.join(HERE, 'scenes.json'), 'w', encoding='utf-8'), ensure_ascii=False, indent=1)

print(f'{N} artists, {len(rows)} songs -> {K} scenes (resolution {RES})')
for s in scenes:
    print(f"\n#{s['id']:2} {s['songs']:4} songs {len(s['artists']):3} artists  cohesion {s['cohesion']:.2f}  old: {', '.join(f'{r}:{c}' for r, c in s['old_regions'])}")
    print('    tags:', ', '.join(s['tags']))
    print('    ', '; '.join(s['artists'][:14]))
