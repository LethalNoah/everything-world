"""Step 2: place songs, build terrain from the music, export world.js + assets/base.jpg + assets/atlas.jpg."""
import numpy as np,json,csv,collections,math,sys,re,os,heapq,hashlib
from scipy import ndimage as ndi
from scipy.spatial import cKDTree
from PIL import Image
import contourpy
from layout import REGIONS,BIOME,SEAS
sys.stdout.reconfigure(encoding='utf-8')
Image.MAX_IMAGE_PIXELS=None
WW,WH=4800,3200; CELL=2
npz=np.load('geo.npz'); F=ndi.gaussian_filter(npz['F'],1.0); L=npz['L']; GH,GW=F.shape
land=F>0
keys=list(REGIONS); idx={k:i for i,k in enumerate(keys)}
rng=np.random.default_rng(7)
art=json.load(open('artist_region.json',encoding='utf-8'))
man=json.load(open('cover_manifest.json',encoding='utf-8'))
rows=list(csv.DictReader(open('playlist.csv',encoding='utf-8-sig')))
def fbm(seed,beta,fmin=2.0,gw=GW,gh=GH):
    r=np.random.default_rng(seed)
    fx=np.fft.rfftfreq(gw)[None,:]*gw/gh; fy=np.fft.fftfreq(gh)[:,None]
    f=np.sqrt(fx*fx+fy*fy); f[0,0]=1
    amp=f**(-beta/2)*(1-np.exp(-(f*gh/fmin)**4)); amp[0,0]=0
    n=np.fft.irfft2((r.normal(size=f.shape)+1j*r.normal(size=f.shape))*amp,s=(gh,gw)); return (n-n.mean())/n.std()
dland=ndi.distance_transform_edt(land); dsea,ind=ndi.distance_transform_edt(~land,return_indices=True)

# ---------- songs ----------
songs=[]
for r in rows:
    arts=[a.strip() for a in r['Artist Name(s)'].split(';')]
    prim=arts[0]
    iid=man.get(r['Album Name']+'|'+r['Artist Name(s)'].split(';')[0])
    songs.append(dict(uri=r['Track URI'].split(':')[-1],t=r['Track Name'],a=arts,al=r['Album Name'],yr=r['Release Date'][:4],
      ms=int(r['Duration (ms)'] or 0),pop=int(r['Popularity'] or 0),g=[x.strip() for x in r['Genres'].split(',') if x.strip()],
      e=float(r['Energy']),v=float(r['Valence']),ac=float(r['Acousticness']),d=float(r['Danceability']),tempo=float(r['Tempo']),
      ins=float(r['Instrumentalness']),by=r['Added By'],reg=art[prim],prim=prim,iid=iid))
print('songs',len(songs),'no cover',sum(1 for s in songs if not s['iid']))

# ---------- placement ----------
towns=[]
GG=32.0; ggrid={}
def near_used(p,d=30.0):
    gx,gy=int(p[0]//GG),int(p[1]//GG)
    return any(math.hypot(p[0]-q[0],p[1]-q[1])<d for ax in (-1,0,1) for ay in (-1,0,1) for q in ggrid.get((gx+ax,gy+ay),[]))
for k in keys:
    i=idx[k]; ss=[s for s in songs if s['reg']==k]
    if not ss: continue
    by=collections.defaultdict(list)
    for s in ss: by[s['prim']].append(s)
    big=[a for a,v in by.items() if len(v)>=3]
    need=len(ss)+len(big)
    mask=land&(L==i)
    yy,xx=np.nonzero(mask&(dland>=6))
    if len(yy)<need*3: yy,xx=np.nonzero(mask)
    area=len(yy)*CELL*CELL
    pts=np.stack([xx,yy],1)*CELL+rng.uniform(0,CELL,(len(yy),2))
    pts=pts[rng.permutation(len(pts))]
    d0=min(62,max(31,math.sqrt(area/need)*.8))
    while True:
        chosen=[]; grid={}; c=d0
        for p in pts:
            if near_used(p): continue
            gx,gy=int(p[0]//c),int(p[1]//c)
            if any(np.hypot(*(p-q))<d0 for ax in (-1,0,1) for ay in (-1,0,1) for q in grid.get((gx+ax,gy+ay),[])): continue
            grid.setdefault((gx,gy),[]).append(p); chosen.append(p)
            if len(chosen)>=need*1.6: break
        if len(chosen)>=need: break
        d0*=.92
    S=np.array(chosen)
    c0=S.mean(0); U,sv,Vt=np.linalg.svd(S-c0,full_matrices=False)
    sigma=(S-c0)@Vt.T; sstd=sigma.std(0)+1e-6
    # artist embedding from local genres + sound
    gc=collections.Counter(g for s in ss for g in s['g']); top=[g for g,_ in gc.most_common(10)]
    arts=list(by); X=[]
    for a in arts:
        v=by[a]; X.append([sum(g in s['g'] for s in v)/len(v)*1.0 for g in top]+[np.mean([s[f] for s in v])*1.2 for f in ('e','v','ac','d')])
    X=np.array(X); X=X-X.mean(0)
    if len(arts)>2:
        u,sg,vt=np.linalg.svd(X,full_matrices=False); E=u[:,:2]*sg[:2]
        if E.shape[1]<2: E=np.c_[E,np.zeros(len(E))]
    else: E=rng.normal(size=(len(arts),2))
    E=(E-E.mean(0))/(E.std(0)+1e-6)
    tg=c0+(np.clip(E,-2,2)*sstd*.9)@Vt
    free=np.ones(len(S),bool); tree=cKDTree(S)
    order=sorted(range(len(arts)),key=lambda j:-len(by[arts[j]]))
    def nearest(pt):
        d=np.hypot(*(S-pt).T); d[~free]=1e9; j=int(np.argmin(d)); free[j]=False; return S[j]
    for rank,j in enumerate(order):
        a=arts[j]; v=by[a]
        t0=nearest(tg[j])
        if len(v)>=3:
            towns.append(dict(n=a,x=float(t0[0]),y=float(t0[1]),c=len(v),r=i,cap=int(rank==0)))
            home=t0
            for s in sorted(v,key=lambda s:-s['pop']):
                p=nearest(home); s['x'],s['y']=float(p[0]),float(p[1])
        else:
            home=t0; v[0]['x'],v[0]['y']=float(t0[0]),float(t0[1])
            for s in v[1:]:
                p=nearest(home); s['x'],s['y']=float(p[0]),float(p[1])
    for p in S[~free]: ggrid.setdefault((int(p[0]//GG),int(p[1]//GG)),[]).append(p)
    print(f"{k:9} songs {len(ss):4} spacing {d0:5.1f} towns {len(big)}")
print('towns',len(towns))

# ---------- terrain from music ----------
sx=np.array([s['x'] for s in songs])/CELL; sy=np.array([s['y'] for s in songs])/CELL
def kernel(vals,sigma):
    a=np.zeros(F.shape,'f4'); n=np.zeros(F.shape,'f4')
    ix=sx.astype(int).clip(0,GW-1); iy=sy.astype(int).clip(0,GH-1)
    np.add.at(a,(iy,ix),vals); np.add.at(n,(iy,ix),1)
    a=ndi.gaussian_filter(a,sigma); n=ndi.gaussian_filter(n,sigma)
    return a/np.maximum(n,1e-5),n
En,dens=kernel(np.array([s['e'] for s in songs]),24)
Ac,_=kernel(np.array([s['ac'] for s in songs]),30)
rug=np.array([BIOME[k]['rug'] for k in keys],'f4')[L]
rug=ndi.gaussian_filter(rug,14)
ridge=1-np.abs(fbm(101,2.6,5.0))*.9
ridge=np.clip(ridge,0,1)
n2=fbm(102,2.4,8.0)
coast=np.clip(dland/34,0,1)
h=coast*(.10+np.clip(En-.45,0,.55)*.75+rug*.55)+ridge*(.10+rug*.55+np.clip(En-.55,0,.45)*.55)*coast**.5+n2*.03
h=ndi.gaussian_filter(h,1.5); h[~land]=0
print('h pct',np.percentile(h[land],[10,50,80,90,97]).round(3))
HI=float(np.percentile(h[land],84)); MID=float(np.percentile(h[land],70))
print('HI',HI,'MID',MID)

# ---------- glyphs ----------
GL=['mountain','peak','volcano','mesa','crystal','hill','conifer','deciduous','palm','willow','mushroom','marsh','dune','wheat','flower','scrub']
gidx={g:i for i,g in enumerate(GL)}
tree=cKDTree(np.c_[sx*CELL,sy*CELL])
tx=np.array([t['x'] for t in towns]); ty=np.array([t['y'] for t in towns])
ttree=cKDTree(np.c_[tx,ty])
patch=fbm(103,2.8,8.0)
glyphs=[]
SP=15
for gy in np.arange(0,WH,SP):
    for gx in np.arange(0,WW,SP):
        x=gx+rng.uniform(-SP*.45,SP*.45); y=gy+rng.uniform(-SP*.45,SP*.45)
        cx,cy=int(x/CELL),int(y/CELL)
        if not(0<=cx<GW and 0<=cy<GH) or not land[cy,cx] or dland[cy,cx]<5: continue
        k=keys[L[cy,cx]]; b=BIOME[k]; hh=h[cy,cx]; u=rng.random()
        dmin=tree.query((x,y))[0]
        if hh>HI and dmin>=16 and u<.30:
            g=b['hi']; sc=1.2+min(1,(hh-HI)*3.2)*1.0
        elif hh>MID and dmin>=13 and u<.12:
            g='hill'; sc=.9+rng.random()*.35
        else:
            if dmin<11: continue
            p=b['dens']*(.34+.46*np.clip(patch[cy,cx]+.4,0,1.6))*(.75+Ac[cy,cx]*.8)
            if u>p*.60: continue
            g=b['lo']; sc=.8+rng.random()*.5
        glyphs.append((y,gidx[g],round(x,1),round(y,1),round(sc,2),int(rng.integers(0,999))))
glyphs.sort()
print('glyphs',len(glyphs),collections.Counter(GL[g[1]] for g in glyphs).most_common())

# ---------- vector lines ----------
def simplify(pts,eps):
    if len(pts)<3: return pts
    keep=np.zeros(len(pts),bool); keep[0]=keep[-1]=True; st=[(0,len(pts)-1)]
    while st:
        a,b=st.pop()
        if b<=a+1: continue
        ab=pts[b]-pts[a]; L2=(ab*ab).sum()
        d=np.abs(ab[0]*(pts[a+1:b,1]-pts[a,1])-ab[1]*(pts[a+1:b,0]-pts[a,0]))/math.sqrt(L2) if L2>0 else np.hypot(*(pts[a+1:b]-pts[a]).T)
        j=int(np.argmax(d))
        if d[j]>eps: j+=a+1; keep[j]=True; st+=[(a,j),(j,b)]
    return pts[keep]
def lines(field,level,eps,minlen=0):
    g=contourpy.contour_generator(z=field.astype('f8'),name='serial',line_type='Separate')
    out=[]
    for l in g.lines(level):
        if len(l)<minlen: continue
        out.append(simplify(l*CELL+CELL/2,eps))
    return out
def chunk(polys,n=48):
    """cut into short runs with bboxes so tiles can skip most of them"""
    res=[]
    for p in polys:
        for a in range(0,len(p)-1,n):
            q=p[a:a+n+1]
            if len(q)<2: continue
            res.append(dict(b=[round(float(q[:,0].min())),round(float(q[:,1].min())),round(float(q[:,0].max())),round(float(q[:,1].max()))],
                            p=[round(float(v),1) for v in q.ravel()]))
    return res
coast_l=lines(F,0,.55,6)
print('coast polylines',len(coast_l),'pts',sum(len(c) for c in coast_l))
smooth_sea=ndi.gaussian_filter(dsea,1.6)
rip=[]
for lvl,al in ((5,.5),(10.5,.32),(17,.2)):
    rip.append(dict(a=al,l=chunk(lines(smooth_sea,lvl,1.1,16))))
    print('ripple',lvl,sum(len(c['p']) for c in rip[-1]['l'])//2)

# region borders: contour of each region, keep runs on land where we own the lower index
borders=[]
adj=collections.Counter()
for i,k in enumerate(keys):
    sm=ndi.gaussian_filter((L==i).astype('f4'),1.6)
    for l in contourpy.contour_generator(z=sm.astype('f8'),name='serial',line_type='Separate').lines(.5):
        pts=l; run=[]
        for p in pts:
            cx,cy=int(p[0]+.5),int(p[1]+.5)
            ok=False
            if 3<=cx<GW-3 and 3<=cy<GH-3 and dland[cy,cx]>1.5:
                nb=set(np.unique(L[cy-3:cy+4,cx-3:cx+4]).tolist())-{i}
                if nb:
                    adj[(min(i,min(nb)),max(i,min(nb)))]+=1
                    ok=min(nb)>i
            if ok: run.append(p)
            else:
                if len(run)>6: borders.append(simplify(np.array(run)*CELL+CELL/2,.8))
                run=[]
        if len(run)>6: borders.append(simplify(np.array(run)*CELL+CELL/2,.8))
print('border runs',len(borders))

# rivers: priority-flood on a coarse grid
SC=4
ch=ndi.zoom(h,1/SC,order=1); lc=land[::SC,::SC]; ch=ch[:lc.shape[0],:lc.shape[1]]
cd=dland[::SC,::SC]; ch=ch+cd/4000+fbm(104,2.5,10.0,lc.shape[1],lc.shape[0])*.012
hh,ww=lc.shape; seen=~lc.copy(); recv=-np.ones((hh,ww,2),int); heap=[]
boundary=lc&~ndi.binary_erosion(lc)
for a,b in zip(*np.nonzero(boundary)):
    heapq.heappush(heap,(float(ch[a,b]),int(a),int(b))); seen[a,b]=True
order=[]
while heap:
    z,a,b=heapq.heappop(heap); order.append((a,b))
    for da,db in ((1,0),(-1,0),(0,1),(0,-1),(1,1),(-1,-1),(1,-1),(-1,1)):
        na,nb=a+da,b+db
        if 0<=na<hh and 0<=nb<ww and not seen[na,nb]:
            seen[na,nb]=True; recv[na,nb]=(a,b); heapq.heappush(heap,(max(z,float(ch[na,nb])),na,nb))
acc=np.ones((hh,ww))
for a,b in reversed(order):
    ra,rb=recv[a,b]
    if ra>=0: acc[ra,rb]+=acc[a,b]
T=70
isr=acc>=T
donors=np.zeros((hh,ww),int)
for a,b in order:
    ra,rb=recv[a,b]
    if isr[a,b] and ra>=0: donors[ra,rb]+=1
rivers=[]; vis=np.zeros((hh,ww),bool)
wob=fbm(105,2.3,16.0)
starts=sorted([(acc[a,b],a,b) for a,b in zip(*np.nonzero(isr)) if donors[a,b]==0],reverse=True)
for _,a,b in starts:
    path=[]; ca,cb=a,b; first=True; ended=None
    while ca>=0:
        path.append((ca,cb,acc[ca,cb]))
        if vis[ca,cb] and not first: break
        vis[ca,cb]=True; first=False
        ca,cb=recv[ca,cb]
    if len(path)<9: continue
    p=np.array([[pb*SC*CELL+SC,pa*SC*CELL+SC] for pa,pb,_ in path],float)
    p[1:-1,0]+=rng.uniform(-3.5,3.5,len(p)-2); p[1:-1,1]+=rng.uniform(-3.5,3.5,len(p)-2)
    # meander smoothing (Chaikin x2)
    for _r in range(2):
        q=np.empty((len(p)*2-2,2)); q[0::2]=p[:-1]*.75+p[1:]*.25; q[1::2]=p[:-1]*.25+p[1:]*.75; p=q
    w=min(1,math.log10(path[-1][2]/T+1)*.9)
    rivers.append(dict(w=round(.7+w*1.3,2),l=chunk([simplify(p,.5)],64)))
print('rivers',len(rivers))

# ---------- labels ----------
def pretty(g):
    sp={'idm':'IDM','edm':'EDM','aor':'AOR','mpb':'MPB','r&b':'R&B','uk':'UK','k-pop':'K-Pop','j-pop':'J-Pop','j-rock':'J-Rock','k-rock':'K-Rock','j-rap':'J-Rap','lo-fi':'Lo-Fi','vgm':'VGM'}
    if g in sp: return sp[g]
    return ' '.join(sp.get(w,'-'.join(x.capitalize() for x in w.split('-'))) for w in g.split())
NOUNS=dict(mtn=['Peaks','Crags','Spires','Ridge','Heights','Summit','Pass','Tor'],hill=['Hills','Downs','Knolls','Rise','Fells','Brae'],
 tree=['Wood','Grove','Thicket','Forest','Weald','Copse','Holt'],plain=['Fields','Heath','Plain','Moor','Lea','Vale','Flats'],
 marsh=['Fen','Marsh','Mire','Bog','Wetlands','Sloughs'],sand=['Dunes','Sands','Flats','Barrens','Wastes'],
 sea=['Bay','Cove','Sound','Gulf','Bight','Inlet','Harbour','Shoals','Strait','Lagoon'])
HANDN={'djent':'The Djent Crags','riddim':'Riddim Ridge','deathstep':'Mount Deathstep','drumstep':'The Drumstep Stairs','shoegaze':'The Great Gaze','slowcore':'Slowcore Shallows','lo-fi':'Lo-Fi Lagoon','sea shanties':'Shanty Cove','midwest emo':'The Twinkling Midwest','screamo':'Throatsplitter Point','yacht rock':'Regatta Reach','jam band':'The Long Jam','newgrass':'Newgrass Gap','bossa nova':'Bossa Nova Bay','krautrock':'The Motorik Road','surf rock':'Surf Rock Point','dub':'Dub Echo Cove','chillstep':'Chillstep Falls','vaporwave':'Vaporwave Mirage','synthwave':'The Neon Strand','ambient':'The Quiet Hours','drone':'Mount Drone','doom metal':'Doom Tor','power metal':'Dragonforce Pass','thrash metal':'Thrash Gorge','glam metal':'Hairspray Heights','grunge':'Old Flannel Wood','post-rock':'Crescendo Cliffs','math rock':'Tapping Falls','emo':'Lake Sad','pop punk':'Skate Park Flats','visual kei':'Mirror Spires','city pop':'Night Tempo Bay','experimental jazz':'The Unresolved Coast','free jazz':'Squonk Marsh','hard bop':'Hard Bop Row','cool jazz':'Cool Jazz Cove','classic soul':'Old Hitsville','motown':'Motor City Docks','east coast hip hop':'The East Coast','boom bap':'Boom Bap Bluffs','outlaw country':"Outlaw's Gulch",'honky tonk':'Honky Tonk Flats','gnawa':'Gnawa Gate','progressive rock':'The Long Suite','space rock':'Stargazer Tor','acid rock':'The Acid Springs','psychedelic rock':'The Kaleidoscope','classic rock':'The Old Radio Hills','christmas':'Yuletide Point','comedy':'Laughing Bay','opera':'The High Note','ballet':'Swan Lake','musicals':'Broadway Cove'}
features=[]
used=collections.Counter()
g_all=collections.defaultdict(list)
for j,s in enumerate(songs):
    for g in s['g']: g_all[g].append(j)
for g,js in sorted(g_all.items(),key=lambda kv:-len(kv[1])):
    c=collections.Counter(songs[j]['reg'] for j in js); rk=c.most_common(1)[0][0]
    jj=[j for j in js if songs[j]['reg']==rk]
    x=np.median([songs[j]['x'] for j in jj]); y=np.median([songs[j]['y'] for j in jj])
    x+=rng.uniform(-12,12); y+=rng.uniform(-12,12)
    cx,cy=int(np.clip(x/CELL,0,GW-1)),int(np.clip(y/CELL,0,GH-1))
    hsh=int(hashlib.md5(g.encode()).hexdigest(),16)
    nearcoast=dland[cy,cx]<42
    pref=g[0].upper()
    if 'DL_IND' not in globals():
        DL_IND=ndi.distance_transform_edt(land,return_indices=True)
    # walk out to sea along the shortest way, then a little further
    water=(nearcoast and hsh%3!=0) or g in('bossa nova','sea shanties','surf rock','dub','slowcore','lo-fi','city pop','experimental jazz','cool jazz','motown','ballet','musicals','comedy','christmas')
    if water:
        sa,sb=DL_IND[1][0][cy,cx],DL_IND[1][1][cy,cx]
        vx,vy=sb-cx,sa-cy; n=math.hypot(vx,vy) or 1
        px,py=sb+vx/n*14,sa+vy/n*14
        px=int(np.clip(px,1,GW-2)); py=int(np.clip(py,1,GH-2))
        if land[py,px] or dsea[py,px]<6: water=False
        else: x,y=px*CELL,py*CELL
    if water: kind='sea'
    else:
        hh2=h[cy,cx]; lo=BIOME[keys[L[cy,cx]]]['lo']
        if hh2>HI: kind='mtn'
        elif hh2>MID: kind='hill'
        elif lo in('conifer','deciduous','palm','willow','mushroom'): kind='tree'
        elif lo=='marsh': kind='marsh'
        elif lo=='dune': kind='sand'
        else: kind='plain'
    ns=NOUNS[kind]
    al=[n for n in ns if n[0]==pref]
    noun=al[0] if al and used[(kind,al[0])]<4 else ns[hsh%len(ns)]
    used[(kind,noun)]+=1
    name=HANDN.get(g) or f"{pretty(g)} {noun}"
    features.append(dict(n=name,g=g,x=round(float(x),1),y=round(float(y),1),c=len(js),k=kind))
# the one you asked for
kp=[s for s in songs if s['prim']=='Krystyna Prońko'][0]
cx,cy=int(kp['x']/CELL),int(kp['y']/CELL)
if 'DL_IND' not in globals(): DL_IND=ndi.distance_transform_edt(land,return_indices=True)
sa,sb=DL_IND[1][0][cy,cx],DL_IND[1][1][cy,cx]; vx,vy=sb-cx,sa-cy; n=math.hypot(vx,vy) or 1
features.append(dict(n='Polish Blues Bay',g='',x=float(np.clip(sb+vx/n*18,1,GW-2)*CELL),y=float(np.clip(sa+vy/n*18,1,GH-2)*CELL),c=60,k='sea'))
print('features',len(features),collections.Counter(f['k'] for f in features))

# region label anchors + stats
regions=[]
for i,k in enumerate(keys):
    m=land&(L==i); ss=[s for s in songs if s['reg']==k]
    dt=ndi.distance_transform_edt(ndi.binary_opening(m,iterations=2) if m.sum()>4000 else m)
    # pole of inaccessibility, but only the biggest piece
    a,b=np.unravel_index(np.argmax(dt),dt.shape)
    yy,xx=np.nonzero(m); P=np.stack([xx,yy],1).astype(float); c=P.mean(0)
    U,sv,Vt=np.linalg.svd(P-c,full_matrices=False); ang=math.degrees(math.atan2(Vt[0,1],Vt[0,0]))
    if ang>90: ang-=180
    if ang<-90: ang+=180
    ang=float(np.clip(ang,-22,22))
    areaW=m.sum()*CELL*CELL
    gc=collections.Counter(g for s in ss for g in s['g'])
    ac=collections.Counter(s['prim'] for s in ss)
    yrs=[int(s['yr']) for s in ss if s['yr'].isdigit()]
    regions.append(dict(k=k,n=REGIONS[k][2],realm=REGIONS[k][3],desc=REGIONS[k][4],x=float(b*CELL),y=float(a*CELL),ang=round(ang,1),
      w=round(float(dt.max()*CELL*2),1),area=int(areaW),c=len(ss),
      e=round(float(np.mean([s['e'] for s in ss])),2),v=round(float(np.mean([s['v'] for s in ss])),2),ac=round(float(np.mean([s['ac'] for s in ss])),2),
      d=round(float(np.mean([s['d'] for s in ss])),2),tempo=round(float(np.mean([s['tempo'] for s in ss]))),
      y0=min(yrs) if yrs else 0,y1=max(yrs) if yrs else 0,yMed=int(np.median(yrs)) if yrs else 0,
      topA=[a2 for a2,_ in ac.most_common(6)],topG=[g for g,_ in gc.most_common(6)]))
# realm anchors = centroid of member regions weighted by count
realms=[]
for rn in dict.fromkeys(r['realm'] for r in regions):
    rr=[r for r in regions if r['realm']==rn]
    W=sum(r['c'] for r in rr)
    realms.append(dict(n=rn,x=sum(r['x']*r['c'] for r in rr)/W,y=sum(r['y']*r['c'] for r in rr)/W,c=W))
seas=[]
for n,x,y,z,a in SEAS:
    px,py=x*WH,y*WH; cx,cy=int(px/CELL),int(py/CELL)
    if dsea[cy,cx]<12:
        # find a nearby open-water spot
        best=None;bv=-1
        for ry in range(-90,91,6):
            for rx in range(-90,91,6):
                u,v=cy+ry,cx+rx
                if 0<=u<GH and 0<=v<GW:
                    sc=dsea[u,v]-.25*math.hypot(rx,ry)
                    if sc>bv: bv=sc;best=(v,u)
        px,py=best[0]*CELL,best[1]*CELL
    seas.append(dict(n=n,x=px,y=py,z=z,a=a))

# ---------- colouring ----------
PAL=[(142,158,104),(186,146,92),(170,112,92),(120,146,142),(166,140,150),(196,172,104),(128,124,92),(150,120,72)]
col=[-1]*len(keys); nbrs=collections.defaultdict(set)
for (a,b),c in adj.items():
    if c>3: nbrs[a].add(b); nbrs[b].add(a)
for i in sorted(range(len(keys)),key=lambda i:-len(nbrs[i])):
    u={col[j] for j in nbrs[i]}
    for c in rng.permutation(len(PAL)):
        if int(c) not in u: col[i]=int(c); break
    if col[i]<0: col[i]=i%len(PAL)
for r,c in zip(regions,col): r['col']=c

# ---------- base raster ----------
print('painting parchment...')
up=lambda a:ndi.zoom(a,2,order=1)
paper=np.array([236,221,182],'f4')
st=fbm(201,3.0,2.0); st2=fbm(202,2.2,12.0); gr=fbm(203,1.1,80.0)
tone=1+st*.035+st2*.016+gr*.006
tint=np.array(PAL,'f4')[np.array(col)][L]
tint=np.stack([ndi.gaussian_filter(tint[...,c],1.2) for c in range(3)],-1)
# ribbon along region edges
bdist=np.full(F.shape,99.,'f4')
edge=np.zeros(F.shape,bool)
edge[:-1]|=(L[:-1]!=L[1:]); edge[:,:-1]|=(L[:,:-1]!=L[:,1:]); edge&=land
bd=ndi.distance_transform_edt(~edge)
ribbon=np.exp(-bd/7.)*.55
wash=.20+ribbon*.75
landc=paper*(1-wash[...,None])+tint*wash[...,None]
# hill shading from the music-derived height
hs=ndi.gaussian_filter(h,7)
gy,gx=np.gradient(hs)
shade=np.clip(-(gx*.75+gy*.65)*170,-1,1)
landc=landc*(1+shade[...,None]*.055)
landc*= (1-np.exp(-dland/5.)[...,None]*.16)
s=np.exp(-dsea/11.)
deep=np.array([186,194,168],'f4'); shore=np.array([214,212,178],'f4')
seac=deep*(1-s[...,None])+shore*s[...,None]
seac*=1-np.exp(-dsea/2.2)[...,None]*.13
img=np.where(land[...,None],landc,seac)
img=img*tone[...,None]
# edges of the sheet: burn, stains, folds
yy,xx=np.mgrid[0:GH,0:GW]; ex=np.minimum(xx,GW-1-xx)/GW; ey=np.minimum(yy,GH-1-yy)/GH
vig=np.clip(np.minimum(ex/.05,ey/.075),0,1)
img=img*(.74+.26*(vig**.55))[...,None]
for fx in (GW/3,2*GW/3): img*=(1-.045*np.exp(-((xx-fx)/2.2)**2)+.02*np.exp(-((xx-fx-5)/4)**2))[...,None]
img*=(1-.045*np.exp(-((yy-GH/2)/2.2)**2)+.02*np.exp(-((yy-GH/2-5)/4)**2))[...,None]
fox=np.zeros(F.shape,'f4')
for _ in range(70):
    a,b=rng.integers(0,GH),rng.integers(0,GW); fox[a,b]=rng.uniform(.5,3)
fox=ndi.gaussian_filter(fox,6)*22
img*=(1-np.clip(fox,0,.07))[...,None]
final=np.stack([up(img[...,c]) for c in range(3)],-1)[:WH,:WW]
fine=np.random.default_rng(5).normal(0,1.3,final.shape[:2])
final=np.clip(final+fine[...,None],0,255).astype(np.uint8)
os.makedirs('../assets',exist_ok=True)
Image.fromarray(final).save('../assets/base.jpg',quality=86,optimize=True)
print('base',os.path.getsize('../assets/base.jpg')/1e6,'MB')

# ---------- atlas (96px covers) ----------
ids=sorted({s['iid'] for s in songs if s['iid']})
aid={v:i for i,v in enumerate(ids)}
COLS=40; C=96; RW=math.ceil(len(ids)/COLS)
atlas=Image.new('RGB',(COLS*C,RW*C),(180,160,120))
bad=0
for v,i in aid.items():
    try:
        im=Image.open(f'../assets/covers/{v}.jpg').convert('RGB').resize((C,C),Image.LANCZOS)
        atlas.paste(im,((i%COLS)*C,(i//COLS)*C))
    except Exception as ex: bad+=1
atlas.save('../assets/atlas.jpg',quality=80,optimize=True)
print('atlas',atlas.size,os.path.getsize('../assets/atlas.jpg')/1e6,'MB bad',bad)

# ---------- lookup helpers ----------
GRW,GRH=300,200
rg=[]
for a in range(GRH):
    cy=int((a+.5)/GRH*GH)
    rg.append(''.join(chr(48+int(L[cy,int((b+.5)/GRW*GW)])) if land[cy,int((b+.5)/GRW*GW)] else '~' for b in range(GRW)))
sea=dsea.copy()
framep=np.minimum.reduce([xx,GW-1-xx,yy,GH-1-yy])
sea=np.minimum(sea,framep)
sea[:170,:560]=0   # the title box sits here
for lb in seas:
    a,b=int(lb['y']/CELL),int(lb['x']/CELL)
    yy2,xx2=np.ogrid[:GH,:GW]; sea[(yy2-a)**2+((xx2-b)/2.4)**2<55**2]=0
openSea=[]
for _ in range(9):
    a,b=np.unravel_index(np.argmax(sea),sea.shape); v=float(sea[a,b])
    if v<26: break
    openSea.append(dict(x=float(b*CELL),y=float(a*CELL),r=round(v*CELL,1)))
    yy2,xx2=np.ogrid[:GH,:GW]; sea[(yy2-a)**2+(xx2-b)**2<(v*1.35)**2]=0
print('open sea',[(round(o['x']),round(o['y']),o['r']) for o in openSea])
# ---------- export ----------
out=dict(W=WW,H=WH,glyphTypes=GL,keys=keys,regions=regions,realms=realms,seas=seas,features=features,
 towns=[dict(n=t['n'],x=round(t['x'],1),y=round(t['y'],1),c=t['c'],r=t['r'],cap=t['cap']) for t in towns],
 songs=[[s['uri'],s['t'],s['a'],s['al'],s['yr'],s['ms'],s['pop'],s['g'],s['e'],s['v'],s['ac'],s['d'],round(s['tempo']),'',idx[s['reg']],round(s['x'],1),round(s['y'],1),aid.get(s['iid'],-1),s['iid'] or ''] for s in songs],
 glyphs=[[g[1],g[2],g[3],g[4],g[5]] for g in glyphs],
 coast=chunk(coast_l,48),ripples=rip,borders=chunk(borders,40),rivers=rivers,
 atlasCols=COLS,atlasCell=C,regionGrid=rg,grw=GRW,grh=GRH,openSea=openSea)
js='window.WORLD='+json.dumps(out,ensure_ascii=False,separators=(',',':'))+';'
open('../assets/world.js','w',encoding='utf-8').write(js)
print('world.js',len(js)/1e6,'MB')
