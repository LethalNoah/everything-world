"""Draw the island world as an old parchment map for the web: one island per scene, grouped in 8 realms.

Ways to travel, as planned for the game:
- walking: bridges join neighbouring islands inside a realm; great causeways join the realms, so every
  island can be reached on foot
- boats: sea lanes between the realms' towers
- fast travel: one tower per realm (names in scene_art.json)

Reads scenes_layout.json, scene_art.json, playlist.csv (gitignored) and cover_manifest.json.
Writes ../world/assets/world.js and ../world/assets/base.jpg. The page reuses ../assets/atlas.jpg,
the album covers, fonts and map symbols of the original map.
Built on worldgen.py (the original map), with the geography replaced.
"""
import numpy as np, json, csv, collections, math, sys, os, heapq, hashlib
from scipy import ndimage as ndi
from scipy.spatial import cKDTree
from scipy.sparse.csgraph import minimum_spanning_tree
from PIL import Image
import contourpy

sys.stdout.reconfigure(encoding='utf-8')
Image.MAX_IMAGE_PIXELS = None
HERE = os.path.dirname(os.path.abspath(__file__)); os.chdir(HERE)
OUT = '../world/assets'
WW, WH = 4800, 3200; CELL = 2; GW, GH = WW // CELL, WH // CELL
rng = np.random.default_rng(7)

lay = json.load(open('scenes_layout.json', encoding='utf-8'))
art = json.load(open('scene_art.json', encoding='utf-8'))
man = json.load(open('cover_manifest.json', encoding='utf-8'))
rows = list(csv.DictReader(open('playlist.csv', encoding='utf-8-sig')))
SCN = lay['scenes']; K = len(SCN)
ART = {a['id']: a for a in art['scenes']}; RLM = {r['id']: r for r in art['realms']}
keys = [f's{s["id"]}' for s in SCN]
index_of_id = {s['id']: i for i, s in enumerate(SCN)}
realm_of = np.array([s['realm'] for s in SCN])

def fbm(seed, beta, fmin=2.0, gw=GW, gh=GH):
    r = np.random.default_rng(seed)
    fx = np.fft.rfftfreq(gw)[None, :] * gw / gh; fy = np.fft.fftfreq(gh)[:, None]
    f = np.sqrt(fx * fx + fy * fy); f[0, 0] = 1
    amp = f ** (-beta / 2) * (1 - np.exp(-(f * gh / fmin) ** 4)); amp[0, 0] = 0
    n = np.fft.irfft2((r.normal(size=f.shape) + 1j * r.normal(size=f.shape)) * amp, s=(gh, gw))
    return ((n - n.mean()) / n.std()).astype('f4')

# Map symbols per island: hi = high ground, lo = lowland cover, dens = lowland density, rug = ruggedness.
# Chosen from each scene's look brief.
B = lambda hi, lo, dens, rug: dict(hi=hi, lo=lo, dens=dens, rug=rug)
BIOME_BY_ID = {
 0: B('hill', 'deciduous', 1.0, .05), 1: B('volcano', 'scrub', .25, .6), 2: B('hill', 'deciduous', .45, .1),
 3: B('hill', 'wheat', .7, 0), 4: B('hill', 'scrub', .45, .15), 5: B('mesa', 'mushroom', .7, .15),
 6: B('mesa', 'scrub', .5, .2), 7: B('mesa', 'deciduous', .4, .35), 8: B('hill', 'marsh', .85, 0),
 9: B('hill', 'deciduous', .5, 0), 10: B('hill', 'flower', .5, 0), 11: B('mesa', 'scrub', .35, .15),
 12: B('mountain', 'conifer', .95, .2), 13: B('hill', 'palm', .55, 0), 14: B('hill', 'conifer', .9, .05),
 15: B('hill', 'palm', .45, 0), 16: B('mountain', 'deciduous', .4, .3), 17: B('mountain', 'conifer', 1.0, .15),
 18: B('crystal', 'scrub', .3, .6), 19: B('mountain', 'conifer', .35, .5), 20: B('hill', 'marsh', .35, 0),
 21: B('hill', 'scrub', .3, .05), 22: B('hill', 'palm', .35, 0), 23: B('hill', 'wheat', .55, 0),
 24: B('crystal', 'crystal', .3, .6), 25: B('hill', 'palm', .4, .05), 26: B('hill', 'mushroom', .45, 0),
 27: B('peak', 'conifer', .3, .7), 28: B('mountain', 'deciduous', .55, .35), 29: B('hill', 'willow', .55, 0),
 30: B('hill', 'palm', 1.0, 0), 31: B('hill', 'flower', .5, 0), 32: B('mesa', 'scrub', .3, .35),
 33: B('crystal', 'scrub', .45, .2), 34: B('hill', 'willow', .75, 0), 35: B('hill', 'deciduous', .6, .05),
 36: B('hill', 'scrub', .3, .05), 37: B('hill', 'deciduous', .4, .15), 38: B('mesa', 'dune', .45, .15),
 39: B('mountain', 'deciduous', .4, .35), 40: B('mesa', 'dune', .5, .05), 41: B('hill', 'palm', .45, .05),
 42: B('hill', 'scrub', .35, .05), 43: B('peak', 'conifer', .4, .65), 44: B('hill', 'palm', .6, 0),
 45: B('hill', 'scrub', .25, .05), 46: B('hill', 'palm', .5, .05), 47: B('mesa', 'scrub', .25, .1),
 48: B('hill', 'deciduous', .35, .1), 49: B('hill', 'flower', .8, 0), 50: B('hill', 'flower', .55, 0),
 51: B('hill', 'flower', .6, .05), 52: B('mountain', 'conifer', .35, .45), 53: B('hill', 'deciduous', .45, .05),
 54: B('hill', 'marsh', .35, .1), 55: B('mesa', 'scrub', .2, .2), 56: B('hill', 'palm', .65, .05),
 57: B('peak', 'conifer', .4, .55), 58: B('hill', 'dune', .5, 0)}
BIOME = [BIOME_BY_ID[s['id']] for s in SCN]

# ---------- where the islands go ----------
# The scene layout is about square; stretch it sideways to fill a 3:2 sheet. Island sizes stay round.
P0 = np.array([[s['x'], s['y']] for s in SCN], float); R0 = np.array([s['r'] for s in SCN], float)
SX = 1.45; MX, MY = 230, 200; GROW = 1.4
xs = P0[:, 0] * SX
x0, x1 = (xs - R0 * GROW).min(), (xs + R0 * GROW).max(); y0, y1 = (P0[:, 1] - R0 * GROW).min(), (P0[:, 1] + R0 * GROW).max()
S = min((WW - 2 * MX) / (x1 - x0), (WH - 2 * MY) / (y1 - y0))
ox = (WW - (x1 - x0) * S) / 2; oy = (WH - (y1 - y0) * S) / 2
cx = (xs - x0) * S + ox; cy = (P0[:, 1] - y0) * S + oy; Rpx = R0 * S * GROW
print(f'scale {S:.1f} px per layout unit, island radius {Rpx.min():.0f}-{Rpx.max():.0f} px')

# ---------- land ----------
yy, xx = np.mgrid[0:GH, 0:GW].astype('f4')
wx = xx + fbm(31, 3.2, 2.5) * 22; wy = yy + fbm(32, 3.2, 2.5) * 22
gcx, gcy, gR = cx / CELL, cy / CELL, Rpx / CELL
best = np.full(xx.shape, -9., 'f4'); second = np.full(xx.shape, -9., 'f4'); L = np.zeros(xx.shape, 'i2')
for i in range(K):
    # an island is a main body plus a few lobes (peninsulas), seeded by the scene id so its shape is stable
    lr = np.random.default_rng(1000 + SCN[i]['id'])
    sc = 1 - np.hypot(wx - gcx[i], wy - gcy[i]) / gR[i]
    for _ in range(lr.integers(2, 5)):
        th = lr.uniform(0, 2 * np.pi); off = gR[i] * lr.uniform(.35, .62); rl = gR[i] * lr.uniform(.38, .6)
        sc = np.maximum(sc, (1 - np.hypot(wx - gcx[i] - np.cos(th) * off, wy - gcy[i] - np.sin(th) * off) / rl) * .9)
    m = sc > best
    second = np.where(m, best, np.maximum(second, sc)); L = np.where(m, i, L).astype('i2'); best = np.where(m, sc, best)
coarse = fbm(33, 2.9, 2.0); fine = fbm(34, 2.3, 10.0); ridged = 1 - np.abs(fbm(35, 2.7, 3.0))
F = ndi.gaussian_filter(best, 3) * 2.1 + coarse * .42 + ridged * .07 + fine * .03 - .12
# a strait between neighbouring islands, so each scene is its own island
F -= np.exp(-((best - second) / .15) ** 2) * 1.9 * (second > -.7)
edge = np.minimum.reduce([xx / 50, (GW - xx) / 50, yy / 50, (GH - yy) / 50]); F -= np.clip(1 - edge, 0, 1) ** 2 * 6
land = F > 0
lab, n = ndi.label(land); sz = ndi.sum(land, lab, range(1, n + 1))
tiny = np.isin(lab, np.where(sz < 45)[0] + 1); land &= ~tiny; F[tiny] = -.02
hl, nh = ndi.label(~land); sz = ndi.sum(~land, hl, range(1, nh + 1))
for j in np.where(sz < 140)[0]: F[hl == j + 1] = .02
land = F > 0
for i in range(K): assert (land & (L == i)).sum() > 200, f'scene {SCN[i]["id"]} has almost no land'
print('land fraction', round(land.sum() / (GW * GH), 3), 'landmasses', ndi.label(land)[1])
dland = ndi.distance_transform_edt(land); dsea = ndi.distance_transform_edt(~land)
del wx, wy, best, second

# ---------- songs ----------
artists = lay['artists']
songs = []
for r in rows:
    arts = [a.strip() for a in r['Artist Name(s)'].split(';')]; prim = arts[0]
    songs.append(dict(uri=r['Track URI'].split(':')[-1], t=r['Track Name'], a=arts, al=r['Album Name'], yr=r['Release Date'][:4],
        ms=int(r['Duration (ms)'] or 0), pop=int(r['Popularity'] or 0), g=[x.strip() for x in r['Genres'].split(',') if x.strip()],
        e=float(r['Energy']), v=float(r['Valence']), ac=float(r['Acousticness']), d=float(r['Danceability']), tempo=float(r['Tempo']),
        reg=index_of_id[artists[prim]['scene']], prim=prim, iid=man.get(r['Album Name'] + '|' + prim)))

# ---------- placement: artists sit where the scene layout put them, songs around their artist ----------
towns = []
for i in range(K):
    ss = [s for s in songs if s['reg'] == i]
    by = collections.defaultdict(list)
    for s in ss: by[s['prim']].append(s)
    mask = land & (L == i)
    ys, xs_ = np.nonzero(mask & (dland >= 5))
    if len(ys) < len(ss) * 3: ys, xs_ = np.nonzero(mask)
    area = len(ys) * CELL * CELL
    pts = np.stack([xs_, ys], 1) * CELL + rng.uniform(0, CELL, (len(ys), 2)); pts = pts[rng.permutation(len(pts))]
    d0 = min(56, max(26, math.sqrt(area / len(ss)) * .8))
    while True:
        chosen = []; grid = {}; c = d0
        for p in pts:
            gx, gy = int(p[0] // c), int(p[1] // c)
            if any(np.hypot(*(p - q)) < d0 for ax in (-1, 0, 1) for ay in (-1, 0, 1) for q in grid.get((gx + ax, gy + ay), [])): continue
            grid.setdefault((gx, gy), []).append(p); chosen.append(p)
            if len(chosen) >= len(ss) * 1.6: break
        if len(chosen) >= len(ss): break
        d0 *= .92
    Sp = np.array(chosen); free = np.ones(len(Sp), bool)
    def nearest(pt):
        d = np.hypot(*(Sp - pt).T); d[~free] = 1e9; j = int(np.argmin(d)); free[j] = False; return Sp[j]
    order = sorted(by, key=lambda a: -len(by[a]))
    for rank, a in enumerate(order):
        v = by[a]; ap = artists[a]
        target = np.array([cx[i], cy[i]]) + (np.array([ap['x'], ap['y']]) - P0[i]) * S * GROW * .95
        home = nearest(target)
        if len(v) >= 3: towns.append(dict(n=a, x=float(home[0]), y=float(home[1]), c=len(v), r=i, cap=int(rank == 0)))
        first = True
        for s in sorted(v, key=lambda s: -s['pop']):
            p = home if first else nearest(home); first = False
            s['x'], s['y'] = float(p[0]), float(p[1])
print('songs placed', len(songs), 'towns', len(towns))

# ---------- terrain from the music ----------
sxg = np.array([s['x'] for s in songs]) / CELL; syg = np.array([s['y'] for s in songs]) / CELL
def kernel(vals, sigma):
    a = np.zeros(F.shape, 'f4'); n = np.zeros(F.shape, 'f4')
    ix = sxg.astype(int).clip(0, GW - 1); iy = syg.astype(int).clip(0, GH - 1)
    np.add.at(a, (iy, ix), vals); np.add.at(n, (iy, ix), 1)
    a = ndi.gaussian_filter(a, sigma); n = ndi.gaussian_filter(n, sigma)
    return a / np.maximum(n, 1e-5)
En = kernel(np.array([s['e'] for s in songs]), 22)
Ac = kernel(np.array([s['ac'] for s in songs]), 26)
rug = ndi.gaussian_filter(np.array([b['rug'] for b in BIOME], 'f4')[L], 10)
ridge = np.clip(1 - np.abs(fbm(101, 2.6, 5.0)) * .9, 0, 1); n2 = fbm(102, 2.4, 8.0)
coast = np.clip(dland / 26, 0, 1)
h = coast * (.10 + np.clip(En - .45, 0, .55) * .75 + rug * .55) + ridge * (.10 + rug * .55 + np.clip(En - .55, 0, .45) * .55) * coast ** .5 + n2 * .03
h = ndi.gaussian_filter(h, 1.5); h[~land] = 0
HI = float(np.percentile(h[land], 84)); MID = float(np.percentile(h[land], 70))

# ---------- ways to travel ----------
coast_pts, ctree = [], []
for i in range(K):
    m = land & (L == i); e = m & ~ndi.binary_erosion(m)
    ys, xs_ = np.nonzero(e); p = np.c_[xs_, ys] * CELL + CELL / 2.
    coast_pts.append(p); ctree.append(cKDTree(p))

def crossing_ok(p, q, a, b):
    """True if the straight line p->q only touches land of islands a and b (and only near its ends)."""
    n = max(2, int(np.hypot(*(q - p)) / 3))
    for t in np.linspace(0, 1, n)[1:-1]:
        x, y = p + (q - p) * t; gx, gy = int(x / CELL), int(y / CELL)
        if not (0 <= gx < GW and 0 <= gy < GH): return False
        if land[gy, gx] and L[gy, gx] not in (a, b): return False
    return True

GAP = np.full((K, K), np.inf); ENDS = {}
for a in range(K):
    for b in range(a + 1, K):
        if np.hypot(cx[a] - cx[b], cy[a] - cy[b]) - Rpx[a] - Rpx[b] > 900: continue
        d, j = ctree[b].query(coast_pts[a]); order = np.argsort(d)
        for k in order[:40:4]:
            p, q = coast_pts[a][k], coast_pts[b][j[k]]
            if crossing_ok(p, q, a, b):
                GAP[a, b] = GAP[b, a] = d[k]; ENDS[(a, b)] = (p, q); break

def mst(nodes, W):
    # +1 so touching islands (gap 0) still count as an edge; 0 means "no edge" to scipy
    M = np.array([[W[a, b] + 1 if a != b and np.isfinite(W[a, b]) else 0 for b in nodes] for a in nodes])
    T = minimum_spanning_tree(M).tocoo()
    return [(nodes[u], nodes[v]) for u, v in zip(T.row, T.col)]

# One walking network for the whole world: the shortest set of crossings that reaches every island.
# Crossings between realms count double, so realms join up through their nearest shores.
COST = GAP * np.where(realm_of[:, None] == realm_of[None, :], 1.0, 2.0)
links = set((min(a, b), max(a, b)) for a, b in mst(list(range(K)), COST))
# a few extra short bridges between musically close neighbours, so walks can loop
for a in range(K):
    for nb in SCN[a]['neighbours'][:2]:
        b = index_of_id[nb]
        if realm_of[a] == realm_of[b] and GAP[a, b] < 70: links.add((min(a, b), max(a, b)))
bridges = []
for a, b in sorted(links):
    p, q = ENDS[(a, b)]
    # land the bridge a little way onto each shore
    u = (q - p) / (np.hypot(*(q - p)) + 1e-9); p2, q2 = p - u * 7, q + u * 7
    kind = 'causeway' if realm_of[a] != realm_of[b] or GAP[a, b] > 120 else 'bridge'
    bridges.append(dict(a=a, b=b, k=kind, p=[round(float(v), 1) for v in (*p2, *q2)]))
print('bridges', collections.Counter(b['k'] for b in bridges))
assert len(bridges) >= K - 1

# towers: one per realm, on its best-connected island, toward the middle of the realm
deg = collections.Counter(); [deg.update([b['a'], b['b']]) for b in bridges]
towers = []
for r in range(len(RLM)):
    mem = [i for i in range(K) if realm_of[i] == r]
    rcx = np.average(cx[mem], weights=R0[mem] ** 2); rcy = np.average(cy[mem], weights=R0[mem] ** 2)
    isl = max(mem, key=lambda i: deg[i] * 2 + R0[i] / 3 - np.hypot(cx[i] - rcx, cy[i] - rcy) / 150)
    m = land & (L == isl) & (dland >= 10)
    ys, xs_ = np.nonzero(m); pts = np.c_[xs_, ys] * CELL
    dirv = np.array([rcx - cx[isl], rcy - cy[isl]]); dirv = dirv / (np.hypot(*dirv) + 1e-9)
    goal = np.array([cx[isl], cy[isl]]) + dirv * Rpx[isl] * .45 + np.array([0, -Rpx[isl] * .15])
    tp = pts[np.argmin(np.hypot(*(pts - goal).T))]
    towers.append(dict(n=RLM[r]['tower'], realm=RLM[r]['name'], r=r, isl=isl, x=float(tp[0]), y=float(tp[1])))

# sea lanes for boats: each realm's tower to its two nearest realms' towers, routed around land
def curve_ok(p, c, q, a, b):
    for t in np.linspace(0, 1, 90)[3:-3]:
        x, y = (1 - t) ** 2 * p + 2 * (1 - t) * t * c + t * t * q; gx, gy = int(x / CELL), int(y / CELL)
        if not (0 <= gx < GW and 0 <= gy < GH): return False
        if land[gy, gx] and L[gy, gx] not in (a, b): return False
        if land[gy, gx] and .2 < t < .8: return False
    return True
lanes = []; done = set()
tw = np.array([[t['x'], t['y']] for t in towers])
for r, t in enumerate(towers):
    for r2 in np.argsort(np.hypot(*(tw - tw[r]).T))[1:3]:
        pair = (min(r, r2), max(r, r2))
        if pair in done: continue
        done.add(pair)
        a, b = towers[pair[0]]['isl'], towers[pair[1]]['isl']
        # harbours: coast points of each tower island that face the other one
        ca, cb = np.array([cx[a], cy[a]]), np.array([cx[b], cy[b]]); dv = (cb - ca) / np.hypot(*(cb - ca))
        ha = coast_pts[a][np.argsort(-(coast_pts[a] - ca) @ dv)[:60:12]]
        hb = coast_pts[b][np.argsort((coast_pts[b] - cb) @ dv)[:60:12]]
        found = None
        for off in (0, .15, -.15, .3, -.3, .45, -.45, .6, -.6):
            for p in ha:
                for q in hb:
                    mid = (p + q) / 2; nrm = np.array([-(q - p)[1], (q - p)[0]]) / (np.hypot(*(q - p)) + 1e-9)
                    c = mid + nrm * off * np.hypot(*(q - p))
                    if curve_ok(p, c, q, a, b): found = (p, c, q); break
                if found: break
            if found: break
        if found: lanes.append(dict(a=pair[0], b=pair[1], p=[round(float(v), 1) for v in (*found[0], *found[1], *found[2])]))
print('sea lanes', len(lanes))

# ---------- symbols ----------
GL = ['mountain', 'peak', 'volcano', 'mesa', 'crystal', 'hill', 'conifer', 'deciduous', 'palm', 'willow', 'mushroom', 'marsh', 'dune', 'wheat', 'flower', 'scrub']
gidx = {g: i for i, g in enumerate(GL)}
stree = cKDTree(np.c_[sxg * CELL, syg * CELL])
keep_clear = [(t['x'], t['y'], 30) for t in towers] + [(b['p'][0], b['p'][1], 12) for b in bridges] + [(b['p'][2], b['p'][3], 12) for b in bridges]
patch = fbm(103, 2.8, 8.0)
glyphs = []; SP = 15
for gy in np.arange(0, WH, SP):
    for gx in np.arange(0, WW, SP):
        x = gx + rng.uniform(-SP * .45, SP * .45); y = gy + rng.uniform(-SP * .45, SP * .45)
        ccx, ccy = int(x / CELL), int(y / CELL)
        if not (0 <= ccx < GW and 0 <= ccy < GH) or not land[ccy, ccx] or dland[ccy, ccx] < 4: continue
        if any(math.hypot(x - kx, y - ky) < kr for kx, ky, kr in keep_clear): continue
        b = BIOME[L[ccy, ccx]]; hh = h[ccy, ccx]; u = rng.random(); dmin = stree.query((x, y))[0]
        if hh > HI and dmin >= 16 and u < .30: g = b['hi']; sc = 1.2 + min(1, (hh - HI) * 3.2)
        elif hh > MID and dmin >= 13 and u < .12: g = 'hill'; sc = .9 + rng.random() * .35
        else:
            if dmin < 11: continue
            p = b['dens'] * (.34 + .46 * np.clip(patch[ccy, ccx] + .4, 0, 1.6)) * (.75 + Ac[ccy, ccx] * .8)
            if u > p * .60: continue
            g = b['lo']; sc = .8 + rng.random() * .5
        glyphs.append((y, gidx[g], round(x, 1), round(y, 1), round(sc, 2), int(rng.integers(0, 999))))
glyphs.sort()
print('glyphs', len(glyphs))

# ---------- vector lines ----------
def simplify(pts, eps):
    if len(pts) < 3: return pts
    keep = np.zeros(len(pts), bool); keep[0] = keep[-1] = True; st = [(0, len(pts) - 1)]
    while st:
        a, b = st.pop()
        if b <= a + 1: continue
        ab = pts[b] - pts[a]; L2 = (ab * ab).sum()
        d = np.abs(ab[0] * (pts[a + 1:b, 1] - pts[a, 1]) - ab[1] * (pts[a + 1:b, 0] - pts[a, 0])) / math.sqrt(L2) if L2 > 0 else np.hypot(*(pts[a + 1:b] - pts[a]).T)
        j = int(np.argmax(d))
        if d[j] > eps: j += a + 1; keep[j] = True; st += [(a, j), (j, b)]
    return pts[keep]
def lines(field, level, eps, minlen=0):
    g = contourpy.contour_generator(z=field.astype('f8'), name='serial', line_type='Separate')
    return [simplify(l * CELL + CELL / 2, eps) for l in g.lines(level) if len(l) >= minlen]
def chunk(polys, n=48):
    res = []
    for p in polys:
        for a in range(0, len(p) - 1, n):
            q = p[a:a + n + 1]
            if len(q) < 2: continue
            res.append(dict(b=[round(float(q[:, 0].min())), round(float(q[:, 1].min())), round(float(q[:, 0].max())), round(float(q[:, 1].max()))],
                            p=[round(float(v), 1) for v in q.ravel()]))
    return res
coast_l = lines(F, 0, .55, 6)
smooth_sea = ndi.gaussian_filter(dsea, 1.6)
rip = [dict(a=al, l=chunk(lines(smooth_sea, lvl, 1.1, 16))) for lvl, al in ((5, .5), (10.5, .32), (17, .2))]
# islands that touch get a dashed border
borders = []
for i in range(K):
    sm = ndi.gaussian_filter((L == i).astype('f4'), 1.6)
    for l in contourpy.contour_generator(z=sm.astype('f8'), name='serial', line_type='Separate').lines(.5):
        run = []
        for p in l:
            ccx, ccy = int(p[0] + .5), int(p[1] + .5)
            ok = 3 <= ccx < GW - 3 and 3 <= ccy < GH - 3 and dland[ccy, ccx] > 1.5 and (set(np.unique(L[ccy - 3:ccy + 4, ccx - 3:ccx + 4]).tolist()) - {i}) and min(set(np.unique(L[ccy - 3:ccy + 4, ccx - 3:ccx + 4]).tolist()) - {i}) > i
            if ok: run.append(p)
            else:
                if len(run) > 6: borders.append(simplify(np.array(run) * CELL + CELL / 2, .8))
                run = []
        if len(run) > 6: borders.append(simplify(np.array(run) * CELL + CELL / 2, .8))

# rivers on the bigger islands
SCf = 4
ch = ndi.zoom(h, 1 / SCf, order=1); lc = land[::SCf, ::SCf]; ch = ch[:lc.shape[0], :lc.shape[1]]
ch = ch + dland[::SCf, ::SCf] / 4000 + fbm(104, 2.5, 10.0, lc.shape[1], lc.shape[0]) * .012
hh_, ww_ = lc.shape; seen = ~lc.copy(); recv = -np.ones((hh_, ww_, 2), int); heap = []
for a, b in zip(*np.nonzero(lc & ~ndi.binary_erosion(lc))):
    heapq.heappush(heap, (float(ch[a, b]), int(a), int(b))); seen[a, b] = True
order = []
while heap:
    z, a, b = heapq.heappop(heap); order.append((a, b))
    for da, db in ((1, 0), (-1, 0), (0, 1), (0, -1), (1, 1), (-1, -1), (1, -1), (-1, 1)):
        na, nb = a + da, b + db
        if 0 <= na < hh_ and 0 <= nb < ww_ and not seen[na, nb]:
            seen[na, nb] = True; recv[na, nb] = (a, b); heapq.heappush(heap, (max(z, float(ch[na, nb])), na, nb))
acc = np.ones((hh_, ww_))
for a, b in reversed(order):
    ra, rb = recv[a, b]
    if ra >= 0: acc[ra, rb] += acc[a, b]
T = 45; isr = acc >= T; donors = np.zeros((hh_, ww_), int)
for a, b in order:
    ra, rb = recv[a, b]
    if isr[a, b] and ra >= 0: donors[ra, rb] += 1
rivers = []; vis = np.zeros((hh_, ww_), bool)
for _, a, b in sorted([(acc[a, b], a, b) for a, b in zip(*np.nonzero(isr)) if donors[a, b] == 0], reverse=True):
    path = []; ca, cb = a, b; first = True
    while ca >= 0:
        path.append((ca, cb, acc[ca, cb]))
        if vis[ca, cb] and not first: break
        vis[ca, cb] = True; first = False; ca, cb = recv[ca, cb]
    if len(path) < 8: continue
    p = np.array([[pb * SCf * CELL + SCf, pa * SCf * CELL + SCf] for pa, pb, _ in path], float)
    p[1:-1] += rng.uniform(-3.5, 3.5, (len(p) - 2, 2))
    for _r in range(2):
        q = np.empty((len(p) * 2 - 2, 2)); q[0::2] = p[:-1] * .75 + p[1:] * .25; q[1::2] = p[:-1] * .25 + p[1:] * .75; p = q
    w = min(1, math.log10(path[-1][2] / T + 1) * .9)
    rivers.append(dict(w=round(.7 + w * 1.3, 2), l=chunk([simplify(p, .5)], 64)))
print('rivers', len(rivers))

# ---------- place names from genres (as on the original map) ----------
def pretty(g):
    sp = {'idm': 'IDM', 'edm': 'EDM', 'aor': 'AOR', 'mpb': 'MPB', 'r&b': 'R&B', 'uk': 'UK', 'k-pop': 'K-Pop', 'j-pop': 'J-Pop', 'j-rock': 'J-Rock', 'k-rock': 'K-Rock', 'j-rap': 'J-Rap', 'lo-fi': 'Lo-Fi', 'vgm': 'VGM'}
    if g in sp: return sp[g]
    return ' '.join(sp.get(w, '-'.join(x.capitalize() for x in w.split('-'))) for w in g.split())
NOUNS = dict(mtn=['Peaks', 'Crags', 'Spires', 'Ridge', 'Heights', 'Summit', 'Pass', 'Tor'], hill=['Hills', 'Downs', 'Knolls', 'Rise', 'Fells', 'Brae'],
    tree=['Wood', 'Grove', 'Thicket', 'Forest', 'Weald', 'Copse', 'Holt'], plain=['Fields', 'Heath', 'Plain', 'Moor', 'Lea', 'Vale', 'Flats'],
    marsh=['Fen', 'Marsh', 'Mire', 'Bog', 'Wetlands', 'Sloughs'], sand=['Dunes', 'Sands', 'Flats', 'Barrens', 'Wastes'],
    sea=['Bay', 'Cove', 'Sound', 'Gulf', 'Bight', 'Inlet', 'Harbour', 'Shoals', 'Strait', 'Lagoon'])
HANDN = {'djent': 'The Djent Crags', 'riddim': 'Riddim Ridge', 'deathstep': 'Mount Deathstep', 'drumstep': 'The Drumstep Stairs', 'shoegaze': 'The Great Gaze', 'slowcore': 'Slowcore Shallows', 'lo-fi': 'Lo-Fi Lagoon', 'sea shanties': 'Shanty Cove', 'midwest emo': 'The Twinkling Midwest', 'screamo': 'Throatsplitter Point', 'yacht rock': 'Regatta Reach', 'jam band': 'The Long Jam', 'newgrass': 'Newgrass Gap', 'bossa nova': 'Bossa Nova Bay', 'krautrock': 'The Motorik Road', 'surf rock': 'Surf Rock Point', 'dub': 'Dub Echo Cove', 'chillstep': 'Chillstep Falls', 'vaporwave': 'Vaporwave Mirage', 'synthwave': 'The Neon Strand', 'ambient': 'The Quiet Hours', 'drone': 'Mount Drone', 'doom metal': 'Doom Tor', 'power metal': 'Dragonforce Pass', 'thrash metal': 'Thrash Gorge', 'glam metal': 'Hairspray Heights', 'grunge': 'Old Flannel Wood', 'post-rock': 'Crescendo Cliffs', 'math rock': 'Tapping Falls', 'emo': 'Lake Sad', 'pop punk': 'Skate Park Flats', 'visual kei': 'Mirror Spires', 'city pop': 'Night Tempo Bay', 'experimental jazz': 'The Unresolved Coast', 'free jazz': 'Squonk Marsh', 'hard bop': 'Hard Bop Row', 'cool jazz': 'Cool Jazz Cove', 'classic soul': 'Old Hitsville', 'motown': 'Motor City Docks', 'east coast hip hop': 'The East Coast', 'boom bap': 'Boom Bap Bluffs', 'outlaw country': "Outlaw's Gulch", 'honky tonk': 'Honky Tonk Flats', 'gnawa': 'Gnawa Gate', 'progressive rock': 'The Long Suite', 'space rock': 'Stargazer Tor', 'acid rock': 'The Acid Springs', 'psychedelic rock': 'The Kaleidoscope', 'classic rock': 'The Old Radio Hills', 'christmas': 'Yuletide Point', 'comedy': 'Laughing Bay', 'opera': 'The High Note', 'ballet': 'Swan Lake', 'musicals': 'Broadway Cove'}
WATERY = ('bossa nova', 'sea shanties', 'surf rock', 'dub', 'slowcore', 'lo-fi', 'city pop', 'experimental jazz', 'cool jazz', 'motown', 'ballet', 'musicals', 'comedy', 'christmas')
DL_IND = ndi.distance_transform_edt(land, return_indices=True)[1]
features = []; used = collections.Counter(); g_all = collections.defaultdict(list)
for j, s in enumerate(songs):
    for g in s['g']: g_all[g].append(j)
for g, js in sorted(g_all.items(), key=lambda kv: -len(kv[1])):
    rk = collections.Counter(songs[j]['reg'] for j in js).most_common(1)[0][0]
    jj = [j for j in js if songs[j]['reg'] == rk]
    x = np.median([songs[j]['x'] for j in jj]) + rng.uniform(-12, 12); y = np.median([songs[j]['y'] for j in jj]) + rng.uniform(-12, 12)
    ccx, ccy = int(np.clip(x / CELL, 0, GW - 1)), int(np.clip(y / CELL, 0, GH - 1))
    hsh = int(hashlib.md5(g.encode()).hexdigest(), 16)
    water = (dland[ccy, ccx] < 30 and hsh % 3 != 0) or g in WATERY
    if water:
        sa, sb = DL_IND[0][ccy, ccx], DL_IND[1][ccy, ccx]; vx, vy = sb - ccx, sa - ccy; nn = math.hypot(vx, vy) or 1
        px, py = int(np.clip(sb + vx / nn * 14, 1, GW - 2)), int(np.clip(sa + vy / nn * 14, 1, GH - 2))
        if land[py, px] or dsea[py, px] < 6: water = False
        else: x, y = px * CELL, py * CELL
    if water: kind = 'sea'
    else:
        hh2 = h[ccy, ccx]; lo = BIOME[L[ccy, ccx]]['lo']
        kind = 'mtn' if hh2 > HI else 'hill' if hh2 > MID else 'tree' if lo in ('conifer', 'deciduous', 'palm', 'willow', 'mushroom') else 'marsh' if lo == 'marsh' else 'sand' if lo == 'dune' else 'plain'
    ns = NOUNS[kind]; al = [n for n in ns if n[0] == g[0].upper()]
    noun = al[0] if al and used[(kind, al[0])] < 4 else ns[hsh % len(ns)]; used[(kind, noun)] += 1
    features.append(dict(n=HANDN.get(g) or f'{pretty(g)} {noun}', g=g, x=round(float(x), 1), y=round(float(y), 1), c=len(js), k=kind))
print('features', len(features))

# ---------- islands (regions), realms, seas ----------
def hexrgb(c): c = c.split()[0].lstrip('#'); return [int(c[k:k + 2], 16) for k in (0, 2, 4)]
regions = []
for i in range(K):
    m = land & (L == i); ss = [s for s in songs if s['reg'] == i]; a = ART[SCN[i]['id']]
    dt = ndi.distance_transform_edt(ndi.binary_opening(m, iterations=2) if m.sum() > 4000 else m)
    pa, pb = np.unravel_index(np.argmax(dt), dt.shape)
    ys, xs_ = np.nonzero(m); Pm = np.stack([xs_, ys], 1).astype(float); c0 = Pm.mean(0)
    _, _, Vt = np.linalg.svd(Pm - c0, full_matrices=False); ang = math.degrees(math.atan2(Vt[0, 1], Vt[0, 0]))
    ang = ang - 180 if ang > 90 else ang + 180 if ang < -90 else ang
    gc = collections.Counter(g for s in ss for g in s['g']); acn = collections.Counter(s['prim'] for s in ss)
    yrs = [int(s['yr']) for s in ss if s['yr'].isdigit()]
    regions.append(dict(k=keys[i], sid=SCN[i]['id'], n=a['name'], realm=RLM[SCN[i]['realm']]['name'], desc=a['terrain'],
        brief=dict(landmark=a['landmark'], props=a['props'], mood=a['mood'], palette=a['palette']),
        x=float(pb * CELL), y=float(pa * CELL), ang=round(float(np.clip(ang, -22, 22)), 1), w=round(float(dt.max() * CELL * 2), 1),
        area=int(m.sum() * CELL * CELL), c=len(ss),
        e=round(float(np.mean([s['e'] for s in ss])), 2), v=round(float(np.mean([s['v'] for s in ss])), 2),
        ac=round(float(np.mean([s['ac'] for s in ss])), 2), d=round(float(np.mean([s['d'] for s in ss])), 2),
        tempo=round(float(np.mean([s['tempo'] for s in ss]))), y0=min(yrs) if yrs else 0, y1=max(yrs) if yrs else 0,
        topA=[n for n, _ in acn.most_common(6)], topG=[g for g, _ in gc.most_common(6)], col=0))
realms = []
for r in range(len(RLM)):
    rr = [g for g, i in zip(regions, range(K)) if realm_of[i] == r]; Wt = sum(g['c'] for g in rr)
    realms.append(dict(n=RLM[r]['name'], x=sum(g['x'] * g['c'] for g in rr) / Wt, y=sum(g['y'] * g['c'] for g in rr) / Wt, c=Wt))

# sea names: a few belong next to a particular island, the rest go to the widest open water
title_box = np.zeros(dsea.shape, bool); title_box[:200, :600] = True
def open_spot_near(px, py, reach=110):
    best_, bv = None, -1
    for ry in range(-reach, reach + 1, 6):
        for rx in range(-reach, reach + 1, 6):
            u, v = int(py / CELL) + ry, int(px / CELL) + rx
            if 0 <= u < GH and 0 <= v < GW and not title_box[u, v]:
                s_ = min(dsea[u, v], 45) - .22 * math.hypot(rx, ry)
                if s_ > bv: bv = s_; best_ = (v * CELL, u * CELL)
    return best_
SEA_NEAR = [('Gulf of Yacht Rock', 13), ('Bay of Bossa Nova', 40), ('Tape Hiss Shoals', 37), ('The Shanty Channel', 14)]
SEA_OPEN = ['The Everything Ocean', 'Sea of Reverb', 'The Feedback Sea', 'Sea of Sustain', 'The Long Silence', 'The Great Drop', 'The Fade-Out Deep', 'The Drone Expanse']
seas = []
blocked = np.zeros(dsea.shape, bool)
yo, xo = np.ogrid[:GH, :GW]
frame = np.minimum.reduce([xx, GW - 1 - xx, yy, GH - 1 - yy])
# realm names first: open water beside their own islands, with room to the left and right for a long name
wide = ndi.distance_transform_edt(~land, sampling=(1, .33))
for r, rm in enumerate(realms):
    own = ndi.distance_transform_edt(~(land & (realm_of[L] == r)))
    other = ndi.distance_transform_edt(~(land & (realm_of[L] != r)))
    ok = ~land & ~title_box & ~blocked & (own < other * .7) & (own < 75) & (frame > 60)
    mem = [i for i in range(K) if realm_of[i] == r]
    mx_ = np.average(gcx[mem], weights=R0[mem] ** 2); my_ = np.average(gcy[mem], weights=R0[mem] ** 2)
    score = np.where(ok, np.minimum(wide, 26) + np.minimum(dsea, 30) * .5 - .1 * own - .06 * np.hypot(xo - mx_, yo - my_), -1e9)
    a, b = np.unravel_index(np.argmax(score), score.shape)
    if score[a, b] > -1e8:
        rm['x'], rm['y'] = float(b * CELL), float(a * CELL)
        blocked |= (yo - a) ** 2 + ((xo - b) / 3.5) ** 2 < 32 ** 2
for name, sid in SEA_NEAR:
    i = index_of_id[sid]; p = open_spot_near(cx[i] + Rpx[i] * .9, cy[i] + Rpx[i] * .5)
    seas.append(dict(n=name, x=p[0], y=p[1], z=.45, a=0)); blocked |= (yo - p[1] / CELL) ** 2 + ((xo - p[0] / CELL) / 2.4) ** 2 < 50 ** 2
# the big seas go to the widest open water, kept off the very edge of the sheet
openness = ndi.gaussian_filter(np.minimum(dsea, frame * .7), 6) * ~title_box
for z, name in zip((.8, .72, .7, .62, .58, .55, .5, .5), SEA_OPEN):
    o = openness * ~blocked; a, b = np.unravel_index(np.argmax(o), o.shape)
    if o[a, b] < 22: break
    side = b < GW * .08 or b > GW * .92
    seas.append(dict(n=name, x=float(b * CELL), y=float(a * CELL), z=z, a=-80 if side else 0))
    blocked |= (yo - a) ** 2 + ((xo - b) / 2.0) ** 2 < (o[a, b] * 1.1 + 40) ** 2
print('seas', [s['n'] for s in seas])
# the one asked for on the original map
kp = next((s for s in songs if s['prim'] == 'Krystyna Prońko'), None)
if kp:
    p = open_spot_near(kp['x'], kp['y'], 60)
    if p: features.append(dict(n='Polish Blues Bay', g='', x=float(p[0]), y=float(p[1]), c=60, k='sea'))

# ---------- parchment ----------
print('painting parchment...')
up = lambda a: ndi.zoom(a, 2, order=1)
paper = np.array([236, 221, 182], 'f4')
tone = 1 + fbm(201, 3.0, 2.0) * .035 + fbm(202, 2.2, 12.0) * .016 + fbm(203, 1.1, 80.0) * .006
# each island is washed with a muted version of its look brief's colours
earth = np.array([176, 146, 98], 'f4')
tints = []
for i in range(K):
    pal = np.array([hexrgb(c) for c in ART[SCN[i]['id']]['palette']], 'f4')
    lum = pal.mean(1); pal = pal[(lum > 50) & (lum < 225)] if ((lum > 50) & (lum < 225)).any() else pal
    c = pal.mean(0); grey = c.mean(); c = grey + (c - grey) * .7
    tints.append(c * .62 + earth * .38)
tint = np.array(tints, 'f4')[L]
tint = np.stack([ndi.gaussian_filter(tint[..., k], 1.2) for k in range(3)], -1)
wash = .16 + np.exp(-dland / 9.) * .5
landc = paper * (1 - wash[..., None]) + tint * wash[..., None]
hs = ndi.gaussian_filter(h, 7); gyy, gxx = np.gradient(hs)
landc = landc * (1 + np.clip(-(gxx * .75 + gyy * .65) * 170, -1, 1)[..., None] * .055)
landc *= (1 - np.exp(-dland / 5.)[..., None] * .16)
s_ = np.exp(-dsea / 11.)
seac = np.array([186, 194, 168], 'f4') * (1 - s_[..., None]) + np.array([214, 212, 178], 'f4') * s_[..., None]
seac *= 1 - np.exp(-dsea / 2.2)[..., None] * .13
img = np.where(land[..., None], landc, seac) * tone[..., None]
ex = np.minimum(xx, GW - 1 - xx) / GW; ey = np.minimum(yy, GH - 1 - yy) / GH
img = img * (.74 + .26 * (np.clip(np.minimum(ex / .05, ey / .075), 0, 1) ** .55))[..., None]
for fx in (GW / 3, 2 * GW / 3): img *= (1 - .045 * np.exp(-((xx - fx) / 2.2) ** 2) + .02 * np.exp(-((xx - fx - 5) / 4) ** 2))[..., None]
img *= (1 - .045 * np.exp(-((yy - GH / 2) / 2.2) ** 2) + .02 * np.exp(-((yy - GH / 2 - 5) / 4) ** 2))[..., None]
fox = np.zeros(F.shape, 'f4')
for _ in range(70): fox[rng.integers(0, GH), rng.integers(0, GW)] = rng.uniform(.5, 3)
img *= (1 - np.clip(ndi.gaussian_filter(fox, 6) * 22, 0, .07))[..., None]
final = np.stack([up(img[..., k]) for k in range(3)], -1)[:WH, :WW]
final = np.clip(final + np.random.default_rng(5).normal(0, 1.3, final.shape[:2])[..., None], 0, 255).astype(np.uint8)
os.makedirs(OUT, exist_ok=True)
Image.fromarray(final).save(f'{OUT}/base.jpg', quality=86, optimize=True)
print('base', round(os.path.getsize(f'{OUT}/base.jpg') / 1e6, 2), 'MB')

# ---------- lookups and decoration spots ----------
ids = sorted({s['iid'] for s in songs if s['iid']}); aid = {v: i for i, v in enumerate(ids)}   # same order as ../assets/atlas.jpg
GRW, GRH = 300, 200
rg = [''.join(chr(48 + int(L[int((a + .5) / GRH * GH), int((b + .5) / GRW * GW)])) if land[int((a + .5) / GRH * GH), int((b + .5) / GRW * GW)] else '~'
              for b in range(GRW)) for a in range(GRH)]
sea = np.minimum(dsea.copy(), np.minimum.reduce([xx, GW - 1 - xx, yy, GH - 1 - yy]))
sea[title_box] = 0
for lb in seas: sea[(yo - lb['y'] / CELL) ** 2 + ((xo - lb['x'] / CELL) / 2.4) ** 2 < 55 ** 2] = 0
for b in bridges + lanes:
    p = b['p']
    for t in np.linspace(0, 1, 30):
        if len(p) == 4: x, y = p[0] + (p[2] - p[0]) * t, p[1] + (p[3] - p[1]) * t
        else: x, y = (1 - t) ** 2 * p[0] + 2 * (1 - t) * t * p[2] + t * t * p[4], (1 - t) ** 2 * p[1] + 2 * (1 - t) * t * p[3] + t * t * p[5]
        sea[(yo - y / CELL) ** 2 + (xo - x / CELL) ** 2 < 40 ** 2] = 0
openSea = []
for _ in range(9):
    a, b = np.unravel_index(np.argmax(sea), sea.shape); v = float(sea[a, b])
    if v < 24: break
    openSea.append(dict(x=float(b * CELL), y=float(a * CELL), r=round(v * CELL, 1)))
    sea[(yo - a) ** 2 + (xo - b) ** 2 < (v * 1.35) ** 2] = 0

# ---------- export ----------
out = dict(W=WW, H=WH, glyphTypes=GL, keys=keys, regions=regions, realms=realms, seas=seas, features=features,
    towns=[dict(n=t['n'], x=round(t['x'], 1), y=round(t['y'], 1), c=t['c'], r=t['r'], cap=t['cap']) for t in towns],
    songs=[[s['uri'], s['t'], s['a'], s['al'], s['yr'], s['ms'], s['pop'], s['g'], s['e'], s['v'], s['ac'], s['d'], round(s['tempo']), '', s['reg'],
            round(s['x'], 1), round(s['y'], 1), aid.get(s['iid'], -1), s['iid'] or ''] for s in songs],
    glyphs=[[g[1], g[2], g[3], g[4], g[5]] for g in glyphs],
    coast=chunk(coast_l, 48), ripples=rip, borders=chunk(borders, 40), rivers=rivers,
    bridges=bridges, lanes=lanes, towers=towers,
    atlasCols=40, atlasCell=96, regionGrid=rg, grw=GRW, grh=GRH, openSea=openSea)
js = 'window.WORLD=' + json.dumps(out, ensure_ascii=False, separators=(',', ':'), default=lambda o: o.item()) + ';'
open(f'{OUT}/world.js', 'w', encoding='utf-8').write(js)
print('world.js', round(len(js) / 1e6, 2), 'MB')

# quick overview picture for checking
prev = Image.fromarray(final).resize((1600, 1067))
from PIL import ImageDraw
d = ImageDraw.Draw(prev); k = 1600 / WW
for b in bridges: p = b['p']; d.line([p[0] * k, p[1] * k, p[2] * k, p[3] * k], fill=(120, 40, 20) if b['k'] == 'causeway' else (40, 30, 20), width=3 if b['k'] == 'causeway' else 2)
for l in lanes:
    p = l['p']; pts = [((1 - t) ** 2 * p[0] + 2 * (1 - t) * t * p[2] + t * t * p[4]) * k for t in np.linspace(0, 1, 30)]
    pts2 = [((1 - t) ** 2 * p[1] + 2 * (1 - t) * t * p[3] + t * t * p[5]) * k for t in np.linspace(0, 1, 30)]
    d.line(list(zip(pts, pts2)), fill=(40, 70, 110), width=1)
for t in towers: d.rectangle([t['x'] * k - 4, t['y'] * k - 9, t['x'] * k + 4, t['y'] * k + 2], fill=(140, 40, 25))
for r in regions: d.text((r['x'] * k - 30, r['y'] * k), r['n'][:22], fill=(30, 20, 10))
prev.save('preview_islands.jpg', quality=85)
