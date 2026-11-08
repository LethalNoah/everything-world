"""Place the scenes from scenes.json on a 2D world map so similar scenes are neighbours.

1. Scenes are grouped into realms (island chains) by how alike they are.
2. A spring layout pulls similar scenes together; then overlaps are pushed apart,
   with a wider gap between realms (open sea) than inside one.
3. Inside each scene, artists get spots the same way, so similar artists sit close.
Also scores how alike map neighbours are, for this map and the old 2D map.

Writes scenes_layout.json and ../review/data.js (for the review page).
"""
import csv, json, math, os, sys, collections
import numpy as np
import networkx as nx

sys.stdout.reconfigure(encoding='utf-8')
HERE = os.path.dirname(os.path.abspath(__file__))
sys.argv = sys.argv[:1] + ['1.4']
import scenes as SC   # reruns the scene grouping and gives access to its similarity data

sc = json.load(open(os.path.join(HERE, 'scenes.json'), encoding='utf-8'))
scenes = sc['scenes']; K = len(scenes)
SS = np.array(sc['scene_similarity'])
size = np.array([s['songs'] for s in scenes], float)
rad = np.sqrt(size) * 1.0

# ---------- realms ----------
RG = nx.Graph(); RG.add_nodes_from(range(K))
for a in range(K):
    for b in np.argsort(-SS[a])[:4]:
        if SS[a, b] > 0: RG.add_edge(a, int(b), weight=float(SS[a, b]))
realms = nx.community.louvain_communities(RG, weight='weight', resolution=1.0, seed=3)
realms = sorted(realms, key=lambda r: -size[list(r)].sum())
realm_of = np.zeros(K, int)
for r, mem in enumerate(realms):
    for k in mem: realm_of[k] = r

# ---------- scene positions ----------
pos = nx.spring_layout(RG, weight='weight', seed=11, iterations=500, k=1.2 / math.sqrt(K))
P = np.array([pos[k] for k in range(K)]) * 100

GAP_IN, GAP_OUT = 3.0, 14.0
for it in range(4000):
    moved = 0.0
    for a in range(K):
        for b in range(a + 1, K):
            d = P[b] - P[a]; dist = math.hypot(*d) + 1e-6
            need = rad[a] + rad[b] + (GAP_IN if realm_of[a] == realm_of[b] else GAP_OUT)
            if dist < need:
                push = (need - dist) / 2 * d / dist
                P[a] -= push; P[b] += push; moved += need - dist
    # gentle pull back toward strongly linked neighbours, so pushing doesn't scatter them
    for a, b, w in RG.edges(data='weight'):
        d = P[b] - P[a]; dist = math.hypot(*d) + 1e-6
        need = rad[a] + rad[b] + (GAP_IN if realm_of[a] == realm_of[b] else GAP_OUT)
        if dist > need * 1.15:
            pull = .02 * w * (dist - need) * d / dist
            P[a] += pull; P[b] -= pull
    if moved < 1e-3 and it > 200: break
P -= P.min(0) - 10

# ---------- artist positions inside each scene ----------
artists, S, label, nsongs = SC.artists, SC.S, SC.label, SC.nsongs
apos = {}
for k in range(K):
    m = np.where(label == k)[0]
    g = nx.Graph(); g.add_nodes_from(m)
    for i in m:
        for j in m[np.argsort(-S[i, m])[:4]]:
            if j != i and S[i, j] > 0: g.add_edge(int(i), int(j), weight=float(S[i, j]))
    lp = nx.spring_layout(g, weight='weight', seed=5, iterations=200) if len(m) > 1 else {m[0]: np.zeros(2)}
    Q = np.array([lp[i] for i in m], float)
    Q -= Q.mean(0)
    Q /= (np.abs(Q).max() + 1e-9)
    Q *= rad[k] * .8
    for i, q in zip(m, Q):
        apos[artists[i]] = (P[k] + q).tolist()

# ---------- how alike are map neighbours? ----------
def neighbour_score(names, xy, n=5):
    """Average similarity between each artist and its n nearest artists on the map."""
    idx = [SC.ai[a] for a in names]
    xy = np.array(xy)
    out = []
    for r, i in enumerate(idx):
        d = np.hypot(*(xy - xy[r]).T); d[r] = 1e9
        near = np.argsort(d)[:n]
        out.append(np.mean([S[i, idx[j]] for j in near]))
    return float(np.mean(out))

def random_score(n=5):
    rng = np.random.default_rng(0)
    return float(np.mean([S[i, rng.choice(SC.N, n)].mean() for i in range(SC.N)]))

new_names = list(apos)
score_new = neighbour_score(new_names, [apos[a] for a in new_names])
score_old = None
world = os.path.join(HERE, '..', 'assets', 'world.js')
if os.path.exists(world):
    txt = open(world, encoding='utf-8').read()
    data = json.loads(txt[txt.index('{'):txt.rindex('}') + 1])
    # songs: [uri, title, artists, album, year, ms, pop, genres, e, v, ac, d, tempo, '', region, x, y, ...]
    old_xy = collections.defaultdict(list)
    for s in data['songs']:
        a = s[2].split(';')[0].strip() if isinstance(s[2], str) else s[2][0]
        if a in SC.ai: old_xy[a].append((s[15], s[16]))
    old_names = list(old_xy)
    score_old = neighbour_score(old_names, [np.mean(old_xy[a], 0) for a in old_names])
print(f'neighbour similarity  new map: {score_new:.3f}   old map: {score_old}   random: {random_score():.3f}')

# ---------- write ----------
feats = ['Energy', 'Acousticness', 'Valence', 'Danceability', 'Tempo']
rows = SC.rows
def fnum(x):
    try: return float(x)
    except ValueError: return None
scene_sound = []
for k in range(K):
    songs = [r for r in rows if label[SC.ai[SC.prim(r)]] == k]
    scene_sound.append({f.lower(): round(float(np.mean([v for v in (fnum(r[f]) for r in songs) if v is not None] or [0])), 3) for f in feats})

out = dict(
    realms=[dict(id=r, scenes=sorted(int(k) for k in mem)) for r, mem in enumerate(realms)],
    scenes=[dict(s, realm=int(realm_of[k]), x=round(float(P[k, 0]), 2), y=round(float(P[k, 1]), 2),
                 r=round(float(rad[k]), 2), sound=scene_sound[k],
                 neighbours=[int(b) for b in np.argsort(-SS[k])[:4]]) for k, s in enumerate(scenes)],
    artists={a: dict(scene=int(label[SC.ai[a]]), songs=int(nsongs[SC.ai[a]]),
                     x=round(xy[0], 2), y=round(xy[1], 2)) for a, xy in apos.items()},
    songs=[[r['Track Name'], SC.prim(r), r['Track URI'].split(':')[-1]] for r in rows],
    score=dict(new=round(score_new, 3), old=round(score_old, 3) if score_old else None, random=round(random_score(), 3)),
)
json.dump(out, open(os.path.join(HERE, 'scenes_layout.json'), 'w', encoding='utf-8'), ensure_ascii=False)
os.makedirs(os.path.join(HERE, '..', 'review'), exist_ok=True)
with open(os.path.join(HERE, '..', 'review', 'data.js'), 'w', encoding='utf-8') as f:
    f.write('window.SCENES = ' + json.dumps(out, ensure_ascii=False) + ';\n')
print(f'{K} scenes in {len(realms)} realms')
for r, mem in enumerate(realms):
    print(f'realm {r}:', ' | '.join(', '.join(scenes[k]['tags'][:2]) for k in sorted(mem, key=lambda k: -size[k])))
