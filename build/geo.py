"""Step 1: landmass + region partition. Saves geo.npz and a preview PNG.
Land = union of discs and land bridges, roughened by noise.
Regions grow outward across land from their seed until they hit a quota proportional to song count,
so every region is one connected piece."""
import numpy as np,json,collections,csv,sys,heapq
from scipy import ndimage as ndi
from PIL import Image,ImageDraw
from layout import REGIONS
sys.stdout.reconfigure(encoding='utf-8')
GW,GH=2400,1600            # grid; 1 cell = 2 world units; norm y 0..1 maps to GH
SEED=int(sys.argv[1]) if len(sys.argv)>1 else 21
keys=list(REGIONS); idx={k:i for i,k in enumerate(keys)}
art=json.load(open('artist_region.json',encoding='utf-8'))
rows=list(csv.DictReader(open('playlist.csv',encoding='utf-8-sig')))
cnt=collections.Counter(art[r['Artist Name(s)'].split(';')[0].strip()] for r in rows)
assert not [k for k in cnt if k not in REGIONS]

def fbm(seed,beta,fmin=2.0,gw=GW,gh=GH):
    rng=np.random.default_rng(seed)
    fx=np.fft.rfftfreq(gw)[None,:]*gw/gh; fy=np.fft.fftfreq(gh)[:,None]
    f=np.sqrt(fx*fx+fy*fy); f[0,0]=1
    amp=f**(-beta/2)*(1-np.exp(-(f*gh/fmin)**4)); amp[0,0]=0
    a=rng.normal(size=f.shape)+1j*rng.normal(size=f.shape)
    n=np.fft.irfft2(a*amp,s=(gh,gw)); return (n-n.mean())/n.std()

LAND_FRAC=.335
weight=np.array([max(cnt[k],14)**.8 for k in keys],float)
target=weight/weight.sum()*LAND_FRAC*1.5
sx=np.array([REGIONS[k][0] for k in keys]); sy=np.array([REGIONS[k][1] for k in keys])
BRIDGES=[('blues','soul',.030),('soul','funk',.040),('funk','fusion',.032),('fusion','xjazz',.032),('soul','rap',.045),
 ('rap','lofi',.040),('lofi','soul',.03),('jazz','fusion',.035),('jazz','lofi',.022),('jam','blues',.05),('southern','jam',.05),('southern','country',.05),
 ('southern','grass',.04),('country','folk',.05),('grass','folk',.03),('grass','blues',.03),('jam','yacht',.04),('yacht','pop',.05),('pop','bedroom',.05),
 ('screamo','emo',.05),('emo','punk',.05),('core','metal',.06),('metal','stoner',.05),('stoner','psych',.04),('psych','prog',.04),('prog','math',.035),
 ('math','gaze',.03),('gaze','indie',.06),('indie','classic',.07),('classic','jam',.07),('classic','grunge',.06),('grunge','punk',.05),('punk','metal',.04),
 ('screamo','core',.04),('emo','southern',.025),('psych','classic',.05),('prog','indie',.03),('gaze','bedroom',.04),('indie','pop',.05),('classic','yacht',.05),
 ('mbass','dubstep',.05),('dubstep','dance',.05),('mbass','dance',.04),('dance','synth',.05),('synth','idm',.05),('idm','ambient',.05),('mbass','idm',.04),('synth','ambient',.03),
 ('score','classical',.010),('jrock','vgm',.006),('afro','latin',.006),('latin','reggae',.006)]
def seg_d(x,y,x1,y1,x2,y2):
    dx,dy=x2-x1,y2-y1; t=np.clip(((x-x1)*dx+(y-y1)*dy)/(dx*dx+dy*dy),0,1)
    return np.hypot(x-(x1+t*dx),y-(y1+t*dy))

y,x=np.mgrid[0:GH,0:GW]; x=x/GH; y=y/GH
w1=fbm(SEED+1,3.0,2.5); w2=fbm(SEED+2,3.0,2.5)
wx=x+w1*.024; wy=y+w2*.024
rad=np.sqrt(target/np.pi)*1.0
S=np.full(x.shape,-9.0)
for i in range(len(keys)): S=np.maximum(S,rad[i]-np.hypot(wx-sx[i],wy-sy[i]))
for a,b,hw in BRIDGES: S=np.maximum(S,hw-seg_d(wx,wy,sx[idx[a]],sy[idx[a]],sx[idx[b]],sy[idx[b]]))
S=ndi.gaussian_filter(S,10,mode='nearest')
c=fbm(SEED,2.9,1.6); r=1-np.abs(fbm(SEED+5,2.8,4.0))*1.1; f=fbm(SEED+6,2.2,12.0)
F=S/.052+c*.62+r*.22+f*.09-.14
# every region keeps solid ground around its seed
for i in range(len(keys)):
    Ri=np.sqrt(target[i]/np.pi)
    F+=1.7*np.exp(-((wx-sx[i])**2+(wy-sy[i])**2)/((.95 if keys[i] not in('core','novelty','medieval','afro') else 1.5)*Ri)**2)
# sea channels keep realms apart
CHANNELS=[([(1.052,.0),(1.052,.30),(1.03,.47),(1.03,.72)],.020),
 ([(1.335,.56),(1.30,.70),(1.30,1.0)],.016),
 ([(1.33,.56),(1.40,.545),(1.5,.56)],.012),
 ([(.07,.80),(.25,.80),(.262,.91),(.40,.945),(.41,1.0)],.015),
 ([(.975,.70),(1.0,.86),(1.02,1.0)],.016),
 ([(.78,.90),(.985,.850)],.012),
 ([(.02,.27),(.15,.245),(.195,.05)],.014),
 ([(.0,.73),(.13,.72)],.012)]
for pts,hw in CHANNELS:
    for (x1,y1),(x2,y2) in zip(pts,pts[1:]):
        F-=3.5*np.exp(-(seg_d(wx,wy,x1,y1,x2,y2)/hw)**2)
e=np.minimum.reduce([x/.045,(1.5-x)/.045,y/.045,(1-y)/.045]); F=F-np.clip(1-e,0,1)**2*5
land=F>0
lab,n=ndi.label(land); sz=ndi.sum(land,lab,range(1,n+1))
tiny=np.isin(lab,np.where(sz<70)[0]+1); land&=~tiny; F[tiny]=-.02
hl,nh=ndi.label(~land); sz=ndi.sum(~land,hl,range(1,nh+1))
for j in np.where(sz<160)[0]: F[hl==j+1]=.02
land=F>0
print('land fraction',round(land.sum()/(GW*GH),3),'landmasses',ndi.label(land)[1])

# --- quota growth at half res ---
lm=land[::2,::2]; h,w=lm.shape
bump=ndi.zoom(fbm(SEED+9,2.8,6.0),.5,order=1)[:h,:w]
wxl=wx[::2,::2]; wyl=wy[::2,::2]
total=lm.sum()
quota=(target/target.sum()*total).astype(int)
R=np.sqrt(target/np.pi)
lab=-np.ones(lm.shape,int); heap=[]
yy,xx=np.nonzero(lm)
for i in range(len(keys)):
    d=(xx/h-sx[i])**2+(yy/h-sy[i])**2; j=np.argmin(d)
    heapq.heappush(heap,(0.0,i,int(yy[j]),int(xx[j])))
filled=np.zeros(len(keys),int)
def grow(heap,quota):
    leftovers=[]
    while heap:
        k,i,a,b=heapq.heappop(heap)
        if lab[a,b]>=0: continue
        if filled[i]>=quota[i]: leftovers.append((k,i,a,b)); continue
        lab[a,b]=i; filled[i]+=1
        for da,db in ((1,0),(-1,0),(0,1),(0,-1),(1,1),(-1,-1),(1,-1),(-1,1)):
            na,nb=a+da,b+db
            if 0<=na<h and 0<=nb<w and lm[na,nb] and lab[na,nb]<0:
                d=float(np.hypot(wxl[na,nb]-sx[i],wyl[na,nb]-sy[i])/R[i])
                heapq.heappush(heap,(d+float(bump[na,nb])*.09,i,na,nb))
    return leftovers
left=grow(heap,quota)
for scale in (1.1,1.25,1.5,2,4,99):
    heap=left; heapq.heapify(heap)
    left=grow(heap,(quota*scale).astype(int))
# detached islands: give to the nearest claimed land
un=lm&(lab<0)
if un.any():
    ind=ndi.distance_transform_edt(lab<0,return_distances=False,return_indices=True)
    comp,nc=ndi.label(un)
    for j in range(1,nc+1):
        m=comp==j; yy2,xx2=np.nonzero(m); a,b=yy2[len(yy2)//2],xx2[len(xx2)//2]
        lab[m]=lab[ind[0][a,b],ind[1][a,b]]
# smooth borders with a per-region blur vote
best=np.full(lm.shape,-1.0); sm=np.zeros(lm.shape,int)
for i in range(len(keys)):
    v=ndi.gaussian_filter((lab==i).astype('f4'),2.2)
    m=v>best; best[m]=v[m]; sm[m]=i
L=np.repeat(np.repeat(sm,2,0),2,1)[:GH,:GW]
area=np.bincount(sm[lm],minlength=len(keys))/(h*h)
print('area ratio',np.round(area/(target*area.sum()/target.sum()),2))
np.savez_compressed('geo.npz',F=F.astype('f4'),L=L.astype('i2'))
json.dump({'keys':keys,'count':[cnt[k] for k in keys]},open('geo_meta.json','w'))
pal=np.array([[(37*i*7)%200+55,(91*i*3)%200+55,(151*i*5)%200+55] for i in range(len(keys))],np.uint8)
img=np.zeros((GH,GW,3),np.uint8); img[:]=[150,175,190]; img[land]=pal[L[land]]
edge=land^ndi.binary_erosion(land); img[edge]=0
im=Image.fromarray(img); d=ImageDraw.Draw(im)
for i,k in enumerate(keys):
    m=land&(L==i)
    if m.any():
        yy3,xx3=np.nonzero(m); d.text((xx3.mean(),yy3.mean()),f"{k} {cnt[k]}",fill=(0,0,0))
im.resize((1500,1000)).save('preview_geo.png')
