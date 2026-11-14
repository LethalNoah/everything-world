"""Draw the world as an old parchment map for the web, and export the same world for Unity.

Every scene gets land sized by what it needs (its artists' plots, room for new artists, a plaza and open
land to wander). The central realms form one continent, mid-sized realms are their own landmasses, and
the rest are island chains toward the edge; similar scenes stay neighbours.

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
import overrides   # hand corrections in overrides.json
from stations import artist_station, ARTIST_MIN_SONGS

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
 57: B('peak', 'conifer', .4, .55), 58: B('hill', 'dune', .5, 0),
 59: B('hill', 'flower', .5, .05), 60: B('hill', 'deciduous', .45, .05), 61: B('mountain', 'conifer', .5, .45),
 62: B('hill', 'palm', .7, .1), 63: B('hill', 'marsh', .7, 0), 64: B('mountain', 'deciduous', .9, .35),
 65: B('hill', 'palm', .5, .05)}
BIOME = [BIOME_BY_ID[s['id']] for s in SCN]

# ---------- how much land each scene needs ----------
# Land is sized by need, not by one fixed scale: every scene gets room for its artists' plots, twice that
# again for artists still to come, a plaza for its landmark, and open land just to wander. The world's
# scale (metres per map pixel) then follows from the total, so the map sheet stays the same size.
P0 = np.array([[s['x'], s['y']] for s in SCN], float); R0 = np.array([s['r'] for s in SCN], float)
MX, MY = 200, 170                       # sheet margins in map pixels (room for the title and frame)
def tier(n): return 'shrine' if n == 1 else 'monument' if n <= 5 else 'hall' if n <= 15 else 'temple'
PLOT_R = {'shrine': 15, 'monument': 22, 'hall': 45, 'temple': 70}
def plot_r(n): return PLOT_R[tier(n)] if n != 2 else 18
PLOT_GAP, COAST, PLAZA = 6, 12, 55      # metres
SPARE = 2.0                             # free land for future artists, as a multiple of today's plots
OPEN_M2 = 400_000                       # open land per scene to wander in (about 650 m across)
plot_area = collections.defaultdict(float)
for a_ in lay['artists'].values(): plot_area[a_['scene']] += math.pi * (plot_r(a_["songs"]) + PLOT_GAP / 2) ** 2
need_m2 = np.array([plot_area[s_['id']] * (1 + SPARE) + math.pi * PLAZA ** 2 + OPEN_M2 for s_ in SCN])

# ---------- continents and island chains ----------
# The big central realms share one continent, mid-sized realms are each their own landmass, and the rest
# stay island chains near the edge. Realm ids are the ones in scene_art.json. Scenes on one landmass touch
# and are split by borders; everywhere else there is sea between scenes.
LANDMASS = {0: 0, 1: 0, 2: 1, 4: 2, 5: 3}            # realm -> landmass; realms not listed are island chains
group = np.array([LANDMASS.get(r, 100 + r) for r in realm_of])
is_chain = np.array([r not in LANDMASS for r in realm_of])
PULL = np.where(group == 0, 1.0, np.where(is_chain, .25, .55))   # how hard each scene is drawn to the middle
STRAIT_M, SEA_M = 110, 330              # sea between islands of one chain / between landmasses, in metres
nbrs = [[index_of_id[n] for n in s['neighbours'][:3]] for s in SCN]

# turn the scene layout so its long side runs along the wide sheet
_w = R0 ** 2; _c = P0 - np.average(P0, 0, _w)
_ev, _evec = np.linalg.eigh((_c * _w[:, None]).T @ _c); _ax = _evec[:, -1]
_rot = np.array([[_ax[0], _ax[1]], [-_ax[1], _ax[0]]])
P0r = _c @ _rot.T

def arrange(mpp, area):
    """Place the scenes: start from the music-similarity layout, then let them settle so each landmass is
    one solid piece, chains keep their straits, and the continent sits in the middle of the sheet."""
    R = np.sqrt(area / math.pi) / mpp
    st, sea = STRAIT_M / mpp, SEA_M / mpp
    same = group[:, None] == group[None, :]
    chain_pair = is_chain[:, None] & same
    Dmin = np.where(same & ~chain_pair, (R[:, None] + R[None, :]) * .93,
           np.where(chain_pair, R[:, None] + R[None, :] + st, R[:, None] + R[None, :] + sea))
    np.fill_diagonal(Dmin, 0)
    d = np.hypot(*(P0r[:, None] - P0r[None]).transpose(2, 0, 1)) + np.eye(K) * 1e9
    nn = d.argmin(1); k = np.median((R + R[nn]) / d[np.arange(K), nn])
    P = P0r * k * np.array([1.2, 1.0])
    C = np.zeros(2)
    for it in range(1500):
        delta = P[:, None] - P[None]; dist = np.hypot(delta[..., 0], delta[..., 1]) + np.eye(K)
        u = delta / dist[..., None]
        over = np.clip(Dmin - dist, 0, None); np.fill_diagonal(over, 0)
        mv = (u * over[..., None]).sum(1) * .5
        # similar scenes on the same landmass (or chain) stay side by side
        for i in range(K):
            for j in nbrs[i]:
                if group[i] == group[j] and dist[i, j] > Dmin[i, j] * 1.03:
                    mv[i] -= u[i, j] * (dist[i, j] - Dmin[i, j]) * .08
        # every landmass is drawn to the middle; wider than tall, to fill the sheet
        g = (P - C) * np.array([.62, 1.0])
        mv -= g * PULL[:, None] * .012 * (1 - it / 2000)
        # island chains drift outward, so they end up around the edge rather than in the middle
        cont = P[group == 0].mean(0); out = P - cont; on = np.hypot(*out.T)[:, None] + 1e-9
        mv += np.where(is_chain[:, None], out / on * .7 * (1 - it / 1500), 0)
        P += np.clip(mv, -8, 8)
    lo = (P - R[:, None]).min(0); hi = (P + R[:, None]).max(0)
    return P - (lo + hi) / 2, R, hi - lo

# ---------- land ----------
yy, xx = np.mgrid[0:GH, 0:GW].astype('f4')
def make_land(Rpx):
    wx = xx + fbm(31, 3.2, 2.5) * 22; wy = yy + fbm(32, 3.2, 2.5) * 22
    gcx, gcy, gR = cx / CELL, cy / CELL, Rpx / CELL
    best = np.full(xx.shape, -9., 'f4'); second = np.full(xx.shape, -9., 'f4')
    L = np.zeros(xx.shape, 'i2'); L2 = np.zeros(xx.shape, 'i2')
    for i in range(K):
        # a scene is a main body plus a few lobes (peninsulas), seeded by the scene id so its shape is stable
        lr = np.random.default_rng(1000 + SCN[i]['id'])
        sc = 1 - np.hypot(wx - gcx[i], wy - gcy[i]) / gR[i]
        for _ in range(lr.integers(2, 5)):
            th = lr.uniform(0, 2 * np.pi); off = gR[i] * lr.uniform(.35, .62); rl = gR[i] * lr.uniform(.38, .6)
            sc = np.maximum(sc, (1 - np.hypot(wx - gcx[i] - np.cos(th) * off, wy - gcy[i] - np.sin(th) * off) / rl) * .9)
        m = sc > best; m2 = ~m & (sc > second)
        second = np.where(m, best, np.where(m2, sc, second)); L2 = np.where(m, L, np.where(m2, i, L2)).astype('i2')
        L = np.where(m, i, L).astype('i2'); best = np.where(m, sc, best)
    coarse = fbm(33, 2.9, 2.0); fine = fbm(34, 2.3, 10.0); ridged = 1 - np.abs(fbm(35, 2.7, 3.0))
    F = ndi.gaussian_filter(best, 3) * 2.1 + coarse * .42 + ridged * .07 + fine * .03 - .12
    # sea between scenes on different landmasses, and between the islands of a chain
    split = (group[L] != group[L2]) | is_chain[L]
    F -= np.exp(-((best - second) / .15) ** 2) * 1.9 * (second > -.7) * split
    # scenes on one landmass grow into each other, so the land between them is solid
    F = np.where(~split & (second > -.6) & (best > -.25), np.maximum(F, .25), F)
    # never carve into the heart of a scene
    F = np.where((best > .5) & (best - second > .12), np.maximum(F, .3), F)
    edge = np.minimum.reduce([xx / 50, (GW - xx) / 50, yy / 50, (GH - yy) / 50]); F -= np.clip(1 - edge, 0, 1) ** 2 * 6
    land = F > 0
    lab, n = ndi.label(land); sz = ndi.sum(land, lab, range(1, n + 1))
    tiny = np.isin(lab, np.where(sz < 45)[0] + 1); land &= ~tiny; F[tiny] = -.02
    hl, nh = ndi.label(~land); sz = ndi.sum(~land, hl, range(1, nh + 1))
    for j in np.where(sz < 140)[0]: F[hl == j + 1] = .02
    land = F > 0
    return F, L, land, best, second, wx, wy
# Scenes squeezed by their neighbours can end up with less land than they need. Measure each one's land,
# give the short ones more room and lay the world out again (moving neighbours aside, not into them).
def place(area):
    global MPP
    for attempt in range(6):
        P, Rpx, size = arrange(MPP, area)
        fit = max(size[0] / (WW - 2 * MX), size[1] / (WH - 2 * MY))
        if .93 < fit <= 1.0: break
        MPP *= fit * 1.02 if fit > 1 else fit ** .9 * 1.02
    return P, Rpx

MPP = math.sqrt(need_m2.sum() / (.30 * WW * WH))     # first guess: land on about 30% of the sheet
# Lay the scenes out once, with a little extra room for coastline, then keep them where they are and only
# adjust how far each scene reaches until every one has the land it needs (bigger reach takes land from
# its neighbours on a landmass; on a chain it just grows the island).
# The finished layout is saved in world_plan.json and reused on later runs, so a growing playlist never
# reshuffles the map. Run with --relayout to lay the whole world out again from scratch.
PLAN_FILE = 'world_plan.json'
plan = json.load(open(PLAN_FILE, encoding='utf-8')) if os.path.exists(PLAN_FILE) and '--relayout' not in sys.argv else None
if plan:
    sp = {s['id']: s for s in plan['scenes']}
    missing = [ART[s['id']]['name'] for s in SCN if s['id'] not in sp]
    if missing: sys.exit(f'new scenes not in {PLAN_FILE}: {missing}. Run with --relayout to place them.')
    MPP = plan['mpp']
    cx = np.array([sp[s['id']]['x'] for s in SCN]); cy = np.array([sp[s['id']]['y'] for s in SCN])
    Rpx = np.array([sp[s['id']]['r'] for s in SCN])
    print(f'using the saved layout in {PLAN_FILE}')
else:
    P, Rpx = place(need_m2 * 1.25)
    cx, cy = P[:, 0] + WW / 2, P[:, 1] + WH / 2
reach = np.ones(K)
for attempt in range(10):
    F, L, land, best, second, wx, wy = make_land(Rpx * reach)
    have = np.bincount(L[land].ravel(), minlength=K) * (MPP * CELL) ** 2
    ratio = need_m2 / np.maximum(have, 1)
    print(f'  pass {attempt}: {int((ratio > 1.03).sum())} scenes short of land, worst',
          ', '.join(f'{ART[SCN[i]["id"]]["name"]} {1 / ratio[i]:.0%}' for i in np.argsort(-ratio)[:3]))
    if (ratio <= 1.03).all(): break
    # landmass scenes never shrink, so landmasses stay whole; with a saved layout nothing shrinks.
    # Land grows about with the square of the reach.
    reach *= np.clip(ratio, np.where(is_chain & (plan is None), .85, 1.0), 1.4) ** .5
Rpx = Rpx * reach
json.dump(dict(mpp=MPP, scenes=[dict(id=s['id'], x=round(float(cx[i]), 3), y=round(float(cy[i]), 3), r=round(float(Rpx[i]), 3))
                                for i, s in enumerate(SCN)]), open(PLAN_FILE, 'w', encoding='utf-8'), indent=0)
need_cells = need_m2 / (MPP * CELL) ** 2
ASC = Rpx / np.maximum(R0, 1e-6) * .8                 # layout units -> map px, for spots inside a scene
print(f'scale {MPP:.2f} m per map pixel: world {WW * MPP / 1000:.1f} x {WH * MPP / 1000:.1f} km, land needed {need_m2.sum() / 1e6:.1f} km2')
gcx, gcy = cx / CELL, cy / CELL
for i in range(K): assert (land & (L == i)).sum() > 200, f'scene {SCN[i]["id"]} has almost no land'
have_m2 = have
print(f'land {have_m2.sum() / 1e6:.1f} km2 ({land.sum() / (GW * GH):.0%} of the sheet), landmasses {ndi.label(land)[1]}, '
      f'smallest scene {have_m2.min() / 1e6:.2f} km2, short of need: {int((ratio > 1.05).sum())}')

# ---------- the Everything Hub ----------
# A small island in open water between a few scenes. It belongs to no scene: its station plays the whole
# playlist (scene id HUB_ID in the game's scene data). It is placed after the scenes, so it moves nothing.
HUB_ID, HUB_NAME, HUB_TOWER = 1000, 'The Everything Hub', 'The Everything Mast'
HUB_NEAR = ['The Bluegrass Hollows', 'The Prog Marches', 'The Iron Range', 'Studio Lot Island', 'The Indie Downs']
HUB_R_M, HUB_CLEAR_M = 240, 70          # island radius (room for the stands and the cover ring), and open water round it
HUB = K                                 # its number in L, after the scenes
name_idx = {ART[s_['id']]['name']: i for i, s_ in enumerate(SCN)}
near_hub = [name_idx[n] for n in HUB_NEAR]
hub_rc = HUB_R_M / MPP / CELL
far = np.maximum.reduce([ndi.distance_transform_edt(~(land & (L == i))) for i in near_hub])
sea_d = ndi.distance_transform_edt(~land)
ok = sea_d > hub_rc + HUB_CLEAR_M / MPP / CELL
# out in the most open water among those scenes: no further from any of them than 1.3x the closest-to-all point
among = ok & (far <= np.where(ok, far, np.inf).min() * 1.3)
hy, hx = np.unravel_index(np.argmax(np.where(among, sea_d, -1)), far.shape)
rr = np.hypot(xx - hx, yy - hy) * (1 + fbm(41, 3.0, 3.0) * .07)
F = np.maximum(F, np.where(rr < hub_rc * 2, (1 - rr / hub_rc) * .5, -1))
hub_mask = rr < hub_rc; land |= hub_mask; L = np.where(hub_mask, HUB, L).astype('i2')
hub_x, hub_y = float(hx * CELL), float(hy * CELL)
print(f'hub at {hub_x:.0f},{hub_y:.0f}: ' + ', '.join(f'{n} {far_d:.0f} m' for n, far_d in
      zip(HUB_NEAR, [float(ndi.distance_transform_edt(~(land & (L == i)))[hy, hx]) * CELL * MPP for i in near_hub])))
BIOME = BIOME + [B('hill', 'flower', .25, 0)]
realm_of_L = np.append(realm_of, -1)    # realm of each L value (the hub has none)
dland = ndi.distance_transform_edt(land); dsea = ndi.distance_transform_edt(~land)
del wx, wy, best, second

# ---------- songs ----------
artists = lay['artists']
songs = []
for r in rows:
    arts = [a.strip() for a in r['Artist Name(s)'].split(';')]; prim = overrides.prim(r)
    songs.append(dict(uri=r['Track URI'].split(':')[-1], t=r['Track Name'], a=arts, al=r['Album Name'], yr=r['Release Date'][:4],
        ms=int(r['Duration (ms)'] or 0), pop=int(r['Popularity'] or 0), g=[x.strip() for x in r['Genres'].split(',') if x.strip()],
        e=float(r['Energy']), v=float(r['Valence']), ac=float(r['Acousticness']), d=float(r['Danceability']), tempo=float(r['Tempo']),
        reg=index_of_id[artists[prim]['scene']], prim=prim, iid=man.get(r['Album Name'] + '|' + arts[0])))

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
        target = np.array([cx[i], cy[i]]) + (np.array([ap['x'], ap['y']]) - P0[i]) * ASC[i]
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
base_links = set(links)   # towers are placed by these, so new crossings never move a tower
# More ways across (added 2026-11-13): every island gets at least two ways in, and close shores that are far
# apart on foot (four or more crossings) get a shortcut, so walks can loop instead of doubling back.
# two islands count as joined on foot only along a real stretch of shared border (a corner or a few cells
# isn't enough: on the game's coarser grid that can turn into a strip of water)
BORDER = np.zeros((K, K), int)
for A_, B_, LA, LB in ((L[:, :-1], L[:, 1:], land[:, :-1], land[:, 1:]), (L[:-1, :], L[1:, :], land[:-1, :], land[1:, :])):
    mm = (A_ != B_) & LA & LB
    np.add.at(BORDER, (A_[mm], B_[mm]), 1); np.add.at(BORDER, (B_[mm], A_[mm]), 1)
def touching(a, b): return GAP[a, b] < 6 and group[a] == group[b] and BORDER[a, b] >= 6
def hops(src):
    dist = {src: 0}; q = [src]
    while q:
        u = q.pop(0)
        for v in range(K):
            if v not in dist and ((min(u, v), max(u, v)) in links or touching(u, v)): dist[v] = dist[u] + 1; q.append(v)
    return dist
added = []
for a in range(K):
    ways = sum(1 for b in range(K) if b != a and ((min(a, b), max(a, b)) in links or touching(a, b)))
    if ways >= 2: continue
    for b in np.argsort(GAP[a]):
        b = int(b)
        if b == a or not np.isfinite(GAP[a, b]) or GAP[a, b] > 260: break
        if (min(a, b), max(a, b)) in links or touching(a, b): continue
        links.add((min(a, b), max(a, b))); added.append((a, b, 'second way in')); break
pairs = sorted(((GAP[a, b], a, b) for a in range(K) for b in range(a + 1, K) if np.isfinite(GAP[a, b]) and 6 <= GAP[a, b] < 120))
for g, a, b in pairs:
    if (a, b) in links: continue
    if hops(a).get(b, 99) >= 4: links.add((a, b)); added.append((a, b, 'shortcut'))
for a, b, why in added: print('extra crossing:', ART[SCN[a]['id']]['name'], '<->', ART[SCN[b]['id']]['name'], f'{GAP[a, b]:.0f} m', why)
bridges = []
for a, b in sorted(links):
    p, q = ENDS[(a, b)]
    # land the bridge a little way onto each shore
    u = (q - p) / (np.hypot(*(q - p)) + 1e-9); p2, q2 = p - u * 7, q + u * 7
    if touching(a, b): continue   # neighbours on one landmass with a real shared border: cross on land
    kind = 'causeway' if realm_of[a] != realm_of[b] or GAP[a, b] > 120 else 'bridge'
    bridges.append(dict(a=a, b=b, k=kind, p=[round(float(v), 1) for v in (*p2, *q2)]))
hub_c = np.array([hub_x, hub_y])
for i in near_hub:
    q = coast_pts[i][np.argmin(np.hypot(*(coast_pts[i] - hub_c).T))]
    u = (q - hub_c) / np.hypot(*(q - hub_c)); p = hub_c + u * (hub_rc * CELL - 2)
    if not crossing_ok(p, q, HUB, i): continue
    p2, q2 = p - u * 7, q + u * 7
    bridges.append(dict(a=HUB, b=i, k='causeway' if np.hypot(*(q - p)) > 120 else 'bridge', p=[round(float(v), 1) for v in (*p2, *q2)]))
print('bridges', collections.Counter(b['k'] for b in bridges))
assert len(links) >= K - 1

# towers: one per realm, on its best-connected island, toward the middle of the realm
deg = collections.Counter(); [deg.update(l) for l in base_links]
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
towers.append(dict(n=HUB_TOWER, realm=HUB_NAME, r=-1, isl=HUB, x=hub_x, y=hub_y - hub_rc * CELL * .3))

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
allg = collections.Counter(g for s in songs for g in s['g']); alla = collections.Counter(s['prim'] for s in songs)
ally = [int(s['yr']) for s in songs if s['yr'].isdigit()]
regions.append(dict(k='hub', sid=HUB_ID, n=HUB_NAME, realm='The Open Sea',
    desc='A small island alone in the open sea, where causeways from five lands meet.',
    brief=dict(landmark='A tall radio mast over a round plaza, broadcasting the whole Everything playlist.',
               props='Benches and a seating area round the mast, lanterns, a jukebox, signposts to every land.',
               mood='A crossroads: a little of everything.',
               palette=['#E8D9B0 sand', '#3E7C8C sea teal', '#C8553D signal red', '#2B2D42 night']),
    x=hub_x, y=hub_y - hub_rc * CELL - 46, ang=0, w=220,   # name in the water just above, clear of the tower area=int(hub_mask.sum() * CELL * CELL), c=len(songs),
    e=round(float(np.mean([s['e'] for s in songs])), 2), v=round(float(np.mean([s['v'] for s in songs])), 2),
    ac=round(float(np.mean([s['ac'] for s in songs])), 2), d=round(float(np.mean([s['d'] for s in songs])), 2),
    tempo=round(float(np.mean([s['tempo'] for s in songs]))), y0=min(ally), y1=max(ally),
    topA=[n for n, _ in alla.most_common(6)], topG=[g for g, _ in allg.most_common(6)], col=0))
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
    own = ndi.distance_transform_edt(~(land & (realm_of_L[L] == r)))
    other = ndi.distance_transform_edt(~(land & (realm_of_L[L] != r) & (L != HUB)))
    mem = [i for i in range(K) if realm_of[i] == r]
    if r in LANDMASS:
        # a realm on a landmass is named across its own land, like a country on a continent
        inside = ndi.distance_transform_edt(ndi.binary_opening(land & (realm_of_L[L] == r), iterations=3), sampling=(1, .45))
        a, b = np.unravel_index(np.argmax(inside), inside.shape)
        rm['x'], rm['y'] = float(b * CELL), float(a * CELL)
        continue
    ok = ~land & ~title_box & ~blocked & (own < other * .7) & (own < 75) & (frame > 60)
    mx_ = np.average(gcx[mem], weights=R0[mem] ** 2); my_ = np.average(gcy[mem], weights=R0[mem] ** 2)
    score = np.where(ok, np.minimum(wide, 26) + np.minimum(dsea, 30) * .5 - .1 * own - .06 * np.hypot(xo - mx_, yo - my_), -1e9)
    a, b = np.unravel_index(np.argmax(score), score.shape)
    if score[a, b] > -1e8:
        rm['x'], rm['y'] = float(b * CELL), float(a * CELL)
        blocked |= (yo - a) ** 2 + ((xo - b) / 3.5) ** 2 < 32 ** 2
for name, sid in SEA_NEAR:
    i = index_of_id[sid]; p = open_spot_near(cx[i] + Rpx[i] * .9, cy[i] + Rpx[i] * .5)
    if p is None or dsea[int(p[1] / CELL), int(p[0] / CELL)] < 14: continue   # no open water beside it
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
tints.append(np.array([200, 178, 130], 'f4'))   # the hub
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
ids = sorted({s['iid'] for s in songs if s['iid']}); aid = {v: i for i, v in enumerate(ids)}
# The cover sheet for this map and the game, built here in the same order as aid, so the numbers always match.
# (The original map keeps its own ../assets/atlas.jpg.) Covers come from fetch_covers.py.
COVER_COLS, COVER_CELL = 40, 96
cover_rows = max(1, math.ceil(len(ids) / COVER_COLS))
sheet = Image.new('RGB', (COVER_COLS * COVER_CELL, cover_rows * COVER_CELL), (40, 36, 40))
missing_covers = 0
for v, i in aid.items():
    try:
        im = Image.open(f'../assets/covers/{v}.jpg').convert('RGB').resize((COVER_CELL, COVER_CELL), Image.LANCZOS)
        sheet.paste(im, ((i % COVER_COLS) * COVER_CELL, (i // COVER_COLS) * COVER_CELL))
    except OSError:
        missing_covers += 1
sheet.save(f'{OUT}/covers.jpg', quality=82, optimize=True)
os.makedirs('../Game/EverythingWorld/Assets/_Project/Art', exist_ok=True)
sheet.save('../Game/EverythingWorld/Assets/_Project/Art/CoverAtlas.jpg', quality=90)
print(f'cover sheet: {len(ids)} covers, {sheet.size[0]}x{sheet.size[1]} px, {missing_covers} missing files, '
      f'{sum(1 for s in songs if not s["iid"])} songs without a cover')
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
    atlasCols=COVER_COLS, atlasCell=COVER_CELL, regionGrid=rg, grw=GRW, grh=GRH, openSea=openSea)
js = 'window.WORLD=' + json.dumps(out, ensure_ascii=False, separators=(',', ':'), default=lambda o: o.item()) + ';'
open(f'{OUT}/world.js', 'w', encoding='utf-8').write(js)
print('world.js', round(len(js) / 1e6, 2), 'MB')

# ---------- the same world for Unity ----------
# The game world is the map scaled up: MPP metres per map pixel. Heights and island ids sit on a grid of
# vertices STEP metres apart, in map order (row 0 = north edge). Unity x = metres east, z = metres north.
UD = '../Game/EverythingWorld/Assets/_Project/Data'
STEP = 8.0
WM, DM = WW * MPP, WH * MPP
NX, NZ = int(WM / STEP) + 1, int(DM / STEP) + 1
vz_m, vx_m = np.mgrid[0:NZ, 0:NX].astype('f4') * STEP          # metres from the west and north edges
gy_, gx_ = np.clip(vz_m / MPP / CELL - .5, 0, GH - 1), np.clip(vx_m / MPP / CELL - .5, 0, GW - 1)
sample = lambda a, order=1: ndi.map_coordinates(a.astype('f4'), [gy_, gx_], order=order, mode='nearest')
toU = lambda px, py: (float(px * MPP), float(DM - py * MPP))   # map pixel -> Unity x, z
hn = h / max(float(np.percentile(h[land], 99)), 1e-6)
dl_m, ds_m = dland * CELL * MPP, dsea * CELL * MPP
elev_land = .6 + 2.6 * np.clip(dl_m / 25, 0, 1) + np.clip(hn, 0, 1.2) * 80 * np.clip(dl_m / 70, 0, 1)
elev_sea = -.3 - np.minimum(ds_m * .2, 10)
E = sample(np.where(land, elev_land, elev_sea))
landv = sample(land, 0) > .5
idv = np.where(landv, sample(L, 0), 255).astype(np.uint8)
# No pockets: a piece of one land cut off from the rest of it and touching another land joins the land it
# shares the longest border with. (Pieces that touch only the sea are that land's own islets and stay.)
_k8 = np.ones((3, 3), bool); _moved = 0
for _pass in range(6):
    changed = False
    for i in np.unique(idv[idv != 255]):
        lab, n = ndi.label(idv == i, structure=_k8)
        if n <= 1: continue
        main = int(np.bincount(lab.ravel())[1:].argmax()) + 1
        for k in range(1, n + 1):
            if k == main: continue
            m = lab == k; ring = ndi.binary_dilation(m, _k8) & ~m
            nb = idv[ring]; nb = nb[(nb != 255) & (nb != i)]
            if len(nb) == 0: continue
            idv[m] = np.bincount(nb).argmax(); _moved += int(m.sum()); changed = True
    if not changed: break
print(f'pockets joined to their neighbours: {_moved * STEP * STEP / 1e4:.1f} ha')
coastv = sample(dl_m)                                            # metres to the shore, on land

# ---------- artist plots ----------
# Every artist gets a round plot sized by song count, near the similar artists the scene layout put
# around them. Plots are saved in artist_plots.json and kept on later runs, so a growing playlist only
# adds plots on free land instead of moving everyone. Each island keeps a plaza for its genre landmark.
plots_file = 'artist_plots.json'
saved = json.load(open(plots_file, encoding='utf-8')) if os.path.exists(plots_file) else {}
by_scene = collections.defaultdict(list)
for a, ap in artists.items(): by_scene[ap['scene']].append(a)
songs_by_artist = collections.defaultdict(list)
for s in songs: songs_by_artist[s['prim']].append(s)
reserved = []   # (x, z, r): plazas, towers, bridge ends
for i in range(K):
    x, z = toU(regions[i]['x'], regions[i]['y']); reserved.append((x, z, PLAZA, i))
for t in towers:
    x, z = toU(t['x'], t['y']); reserved.append((x, z, 40, t['isl']))
for b in bridges:
    for (px, py), isl in (((b['p'][0], b['p'][1]), b['a']), ((b['p'][2], b['p'][3]), b['b'])):
        x, z = toU(px, py); reserved.append((x, z, 30, isl))
cand_rows, cand_cols = np.nonzero(landv)
plots = {}; spare_area = {}
for i in range(K):
    sid = SCN[i]['id']
    on = idv[cand_rows, cand_cols] == i
    cx_u = cand_cols[on] * STEP; cz_u = DM - cand_rows[on] * STEP; cd = coastv[cand_rows[on], cand_cols[on]]
    placed = [(x, z, r) for x, z, r, isl in reserved if isl == i]
    def fits(x, z, r):
        return all((x - px) ** 2 + (z - pz) ** 2 >= (r + pr + PLOT_GAP) ** 2 for px, pz, pr in placed)
    names = sorted(by_scene[sid], key=lambda a: (-artists[a]['songs'], a))
    # artists placed before keep their spot if it still works
    for a in names:
        sv = saved.get(a)
        if sv and sv['scene'] == sid and sv['r'] == plot_r(artists[a]['songs']):
            c, r_ = int(round(sv['x'] / STEP)), int(round((DM - sv['z']) / STEP))
            if 0 <= r_ < NZ and 0 <= c < NX and idv[r_, c] == i and coastv[r_, c] >= sv['r'] + COAST * .5 and fits(sv['x'], sv['z'], sv['r']):
                plots[a] = dict(sv); placed.append((sv['x'], sv['z'], sv['r']))
    for a in names:
        if a in plots: continue
        n = artists[a]['songs']; r = plot_r(n); ap = artists[a]
        tx, tz = toU(*(np.array([cx[i], cy[i]]) + (np.array([ap['x'], ap['y']]) - P0[i]) * ASC[i]))
        spot = None
        for rr in (r, r * .8, r * .65):
            ok = cd >= rr + COAST
            order = np.argsort((cx_u[ok] - tx) ** 2 + (cz_u[ok] - tz) ** 2)
            xs_ok, zs_ok = cx_u[ok][order], cz_u[ok][order]
            for x, z in zip(xs_ok, zs_ok):
                if fits(x, z, rr): spot = (float(x), float(z), float(rr)); break
            if spot: break
        if not spot: print(f'warning: no room for {a} on {regions[i]["n"]}'); continue
        plots[a] = dict(scene=sid, x=spot[0], z=spot[1], r=spot[2]); placed.append(spot)
    # how much open, plot-sized land is left for new artists
    free = sum(1 for x, z, d in zip(cx_u, cz_u, cd) if d >= 27 and fits(x, z, 15))
    spare_area[sid] = free * STEP * STEP
json.dump(dict(sorted(plots.items())), open(plots_file, 'w', encoding='utf-8'), ensure_ascii=False, indent=0)
tiers_ = collections.Counter(tier(artists[a]['songs']) for a in plots)
# spare room, counted in free shrine-sized plots (a shrine plot plus its spacing is about 1,100 square metres)
spare_plots = {s: int(v / 1100) for s, v in spare_area.items()}
print('artist plots', len(plots), dict(tiers_), '| spare shrine plots: fewest',
      sorted((v, regions[index_of_id[s]]['n']) for s, v in spare_plots.items())[:4], 'total', sum(spare_plots.values()))

# level the ground under every plot and plaza, blending into the hills around it
def flatten(x, z, r, edge=14):
    c0, r0 = x / STEP, (DM - z) / STEP; R = (r + edge) / STEP
    rs, cs = slice(max(0, int(r0 - R) - 1), min(NZ, int(r0 + R) + 2)), slice(max(0, int(c0 - R) - 1), min(NX, int(c0 + R) + 2))
    zz, xx_ = np.mgrid[rs, cs]; d = np.hypot((xx_ - c0) * STEP, (zz - r0) * STEP)
    inner = d <= r
    if not inner.any(): return
    target = max(float(np.median(E[rs, cs][inner])), 1.2)
    w = np.clip(1 - (d - r) / edge, 0, 1)
    E[rs, cs] = np.where(landv[rs, cs], E[rs, cs] * (1 - w) + target * w, E[rs, cs])
for p in plots.values(): flatten(p['x'], p['z'], p['r'])
flatten(*toU(hub_x, hub_y), HUB_R_M * .75)   # the hub's plaza
for x, z, r, isl in reserved[:K]: flatten(x, z, r)

os.makedirs(UD, exist_ok=True)
np.clip(np.round(E * 10), -32768, 32767).astype('<i2').tofile(f'{UD}/world_height.bytes')
idv.tofile(f'{UD}/world_island.bytes')
def sid_of(i): return HUB_ID if i == HUB else SCN[i]['id']
def mix(c, d, t): return [round(float(c[k] * (1 - t) + d[k] * t)) for k in range(3)]
islands_u = []
for i in range(K):
    pal = sorted((hexrgb(c) for c in ART[SCN[i]['id']]['palette']), key=lambda c: sum(c))
    base = [178, 168, 120]
    a = mix(pal[len(pal) // 2], base, .35); b = mix(pal[min(len(pal) - 1, len(pal) // 2 + 1)], base, .45)
    x, z = toU(regions[i]['x'], regions[i]['y'])
    islands_u.append(dict(sceneId=SCN[i]['id'], name=regions[i]['n'], realm=regions[i]['realm'], labelX=x, labelZ=z,
        tintA=a, tintB=b, accent=mix(pal[0], [40, 30, 20], .2), hi=BIOME[i]['hi'], lo=BIOME[i]['lo']))
x, z = toU(hub_x, hub_y)
islands_u.append(dict(sceneId=HUB_ID, name=HUB_NAME, realm='', labelX=x, labelZ=z,
    tintA=[196, 182, 132], tintB=[172, 160, 118], accent=[200, 85, 61], hi='hill', lo='flower'))
def years(ss):
    y = sorted(int(s['yr']) for s in ss if s['yr'].isdigit())
    return '' if not y else str(y[0]) if y[0] == y[-1] else f'{y[0]}-{y[-1]}'
# who added each artist's songs, by the display names in display_names.json (gitignored: the playlist's
# 'Added By' column holds Spotify usernames, which never leave this computer)
shown = {k: v.strip() for k, v in (json.load(open('display_names.json', encoding='utf-8')) if os.path.exists('display_names.json') else {}).items()
         if not k.startswith('_')}
adders = collections.defaultdict(collections.Counter)
for r in rows:
    if shown.get(r['Added By']): adders[overrides.prim(r)][shown[r['Added By']]] += 1
artists_u = []
for a, p in sorted(plots.items()):
    ss = songs_by_artist[a]; gc = collections.Counter(g for s in ss for g in s['g'])
    artists_u.append(dict(name=a, sceneId=p['scene'], x=p['x'], z=p['z'], r=p['r'], songs=len(ss), tier=tier(len(ss)),
        station=len(ss) >= ARTIST_MIN_SONGS, stationId=artist_station(a) if len(ss) >= ARTIST_MIN_SONGS else 0,
        genres=', '.join(g for g, _ in gc.most_common(3)), years=years(ss),
        addedBy=' and '.join(n for n, _ in adders[a].most_common()),
        songList=[dict(title=s_['t'], cover=aid.get(s_['iid'], -1)) for s_ in sorted(ss, key=lambda s_: -s_['pop'])]))
spawn = towers[0]
sx_, sz_ = toU(spawn['x'], spawn['y'])
# The Everything Hub's timeline: every song in the order it was added, with its cover and who added it
# (as an index into hubAdders, the display names from display_names.json; empty names are left out).
adder_names = [n for n, _ in collections.Counter(shown.get(r['Added By'], '') for r in rows if shown.get(r['Added By'])).most_common()]
timeline = []
for r, s_ in sorted(zip(rows, songs), key=lambda rs: rs[0]['Added At']):
    who = shown.get(r['Added By'], '')
    timeline.append(dict(id=s_['uri'], c=aid.get(s_['iid'], -1), a=adder_names.index(who) if who in adder_names else -1, y=int(r['Added At'][:4])))
hub_info = dict(songs=len(rows), artists=len({s_['prim'] for s_ in songs}), since=min(int(r['Added At'][:4]) for r in rows))
unity = dict(width=WM, depth=DM, step=STEP, nx=NX, nz=NZ, waterLevel=0.0, metresPerMapPixel=MPP,
    hubTimeline=timeline, hubAdders=adder_names, hubSongs=hub_info['songs'], hubArtists=hub_info['artists'], hubSince=hub_info['since'],
    hubRadius=HUB_R_M,
    coverCols=COVER_COLS, coverCell=COVER_CELL, coverSheetWidth=sheet.size[0], coverSheetHeight=sheet.size[1],
    islands=islands_u, artists=artists_u,
    bridges=[dict(a=sid_of(b['a']), b=sid_of(b['b']), causeway=b['k'] == 'causeway',
                  ax=toU(b['p'][0], b['p'][1])[0], az=toU(b['p'][0], b['p'][1])[1],
                  bx=toU(b['p'][2], b['p'][3])[0], bz=toU(b['p'][2], b['p'][3])[1]) for b in bridges],
    towers=[dict(name=t['n'], realm=t['realm'], sceneId=sid_of(t['isl']), x=toU(t['x'], t['y'])[0], z=toU(t['x'], t['y'])[1]) for t in towers],
    spawnX=sx_, spawnZ=sz_ - 30)
json.dump(unity, open(f'{UD}/world_layout.json', 'w', encoding='utf-8'), ensure_ascii=False, indent=1, default=lambda o: o.item())
print(f'unity world {WM / 1000:.1f} x {DM / 1000:.1f} km, grid {NX} x {NZ}, height {E.min():.1f} to {E.max():.1f} m')

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
