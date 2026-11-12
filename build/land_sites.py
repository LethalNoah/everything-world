"""Map a land for laying it out on purpose (the build-land skill): prints its extent, heart and heights, a
coarse character map (40 m a character: ' .:-=+*#%@' = height in 5 m steps, ~ sea, letters = other lands),
its artists, and the land's width at each row. Run: python build/land_sites.py "The Alt Rock Moors"
Optional extra arguments x,z print the height, slope and distances at those points."""
import json, sys, os
sys.stdout.reconfigure(encoding='utf-8', errors='replace')
import numpy as np
from scipy import ndimage as ndi
HERE = os.path.dirname(os.path.abspath(__file__))
UD = os.path.join(HERE, '..', 'Game', 'EverythingWorld', 'Assets', '_Project', 'Data')
d = json.load(open(f'{UD}/world_layout.json', encoding='utf-8'))
nx, nz, st, D = d['nx'], d['nz'], d['step'], d['depth']
ids = np.fromfile(f'{UD}/world_island.bytes', np.uint8).reshape(nz, nx)
H = np.fromfile(f'{UD}/world_height.bytes', '<i2').reshape(nz, nx) / 10
names = [i['name'] for i in d['islands']]
land = sys.argv[1]; i = names.index(land); isl = d['islands'][i]
m = ids == i; r, c = np.nonzero(m); sea = ids == 255
dsea = ndi.distance_transform_edt(~sea) * st
dother = ndi.distance_transform_edt(~((ids != i) & (ids != 255))) * st
dedge = ndi.distance_transform_edt(m) * st
gy, gx = np.gradient(H, st); flat = ndi.uniform_filter(np.degrees(np.arctan(np.hypot(gx, gy))), 5)
print(f"{land}: heart {isl['labelX']:.0f},{isl['labelZ']:.0f}  x {c.min()*st:.0f}..{c.max()*st:.0f}  z {D-r.max()*st:.0f}..{D-r.min()*st:.0f}  "
      f"{m.sum()*st*st/1e6:.2f} km2  heights 5/50/95% {np.percentile(H[m],[5,50,95]).round(1)} max {H[m].max():.1f}")
S = 5; x0 = (c.min() - 3) * st; ztop = D - (r.min() - 3) * st
print(f'map: first column x {x0:.0f}, first row z {ztop:.0f}, {S*st:.0f} m a character')
for rr in range(r.min() - 3, r.max() + 4, S):
    row = ''
    for cc in range(c.min() - 3, c.max() + 4, S):
        v = ids[rr, cc] if 0 <= rr < nz and 0 <= cc < nx else 255
        row += ' .:-=+*#%@'[min(9, int(max(H[rr, cc], 0) / 5))] if v == i else '~' if v == 255 else names[v].replace('The ', '')[0].lower()
    print(f'{D - rr*st:6.0f} {row}')
print('artists (songs, x, z, radio):')
for a in sorted((a for a in d['artists'] if a['sceneId'] == isl['sceneId']), key=lambda a: -a['songs']):
    print(f"  {a['name']}: {a['songs']}  {a['x']:.0f},{a['z']:.0f}{'  radio' if a.get('stationId') else ''}")
for arg in sys.argv[2:]:
    x, z = map(float, arg.split(',')); rr, cc = int(round((D - z) / st)), int(round(x / st))
    print(f'{x:.0f},{z:.0f}: {"this land" if ids[rr,cc]==i else "NOT this land"} h {H[rr,cc]:.1f} slope {flat[rr,cc]:.1f} sea {dsea[rr,cc]:.0f} m other land {dother[rr,cc]:.0f} m edge {dedge[rr,cc]:.0f} m')
