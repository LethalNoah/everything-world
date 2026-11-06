import csv,collections,json,math,sys
from taxonomy import *
sys.stdout.reconfigure(encoding='utf-8')
rows=list(csv.DictReader(open('playlist.csv',encoding='utf-8-sig')))
g2r={}
for k,v in GENRES.items():
    for g in v.split(','): g2r[g.strip()]=k
allg=collections.Counter(x.strip() for r in rows for x in r['Genres'].split(',') if x.strip())
print('unmapped genres',[g for g in allg if g not in g2r and g not in GENERIC])
idf={g:1+math.log(len(rows)/c)*.35 for g,c in allg.items()}
hand={a.strip():k for k,v in HAND.items() for a in v.split(';')}
hand.update({a.strip():k for k,v in OVERRIDE.items() for a in v.split(';')})
score=collections.defaultdict(collections.Counter)
for r in rows:
    prim=r['Artist Name(s)'].split(';')[0].strip()
    for g in (x.strip() for x in r['Genres'].split(',') if x.strip()):
        if g in GENERIC: reg,w=GENERIC[g]
        elif g in g2r: reg,w=g2r[g],1
        else: continue
        score[prim][reg]+=w*idf[g]
art={}
missing=[]
for r in rows:
    prim=r['Artist Name(s)'].split(';')[0].strip()
    if prim in art or prim in missing: continue
    if prim in hand: art[prim]=hand[prim]
    elif score[prim]: art[prim]=score[prim].most_common(1)[0][0]
    else: missing.append(prim)
# featured-artist recovery
for r in rows:
    prim=r['Artist Name(s)'].split(';')[0].strip()
    if prim in missing:
        for a in r['Artist Name(s)'].split(';')[1:]:
            if a.strip() in art: art[prim]=art[a.strip()]; missing.remove(prim); break
print('still missing',missing)
json.dump(art,open('artist_region.json','w',encoding='utf-8'),ensure_ascii=False,indent=0)
cnt=collections.Counter(); ex=collections.defaultdict(collections.Counter)
for r in rows:
    prim=r['Artist Name(s)'].split(';')[0].strip()
    if prim in art: cnt[art[prim]]+=1; ex[art[prim]][prim]+=1
print(sum(cnt.values()),len(cnt))
for k,c in cnt.most_common():
    print(f"{k:9}{c:4} ", ', '.join(a for a,_ in ex[k].most_common(14)))
