import json,sys,math,re,collections
import numpy as np
from scipy.spatial import cKDTree
sys.stdout.reconfigure(encoding='utf-8')
txt=open('../assets/world.js',encoding='utf-8').read()
W=json.loads(txt[len('window.WORLD='):-1])
S=W['songs']; print('songs',len(S))
pts=np.array([[s[15],s[16]] for s in S])
d,_=cKDTree(pts).query(pts,k=2); print('min spacing',d[:,1].min().round(1),'<26:',(d[:,1]<26).sum())
g=W['regionGrid']; bad=0;sea=0
for s in S:
    ch=g[int(s[16]/W['H']*W['grh'])][int(s[15]/W['W']*W['grw'])]
    if ch=='~': sea+=1
    elif ord(ch)-48!=s[14]: bad+=1
print('on coarse sea cell',sea,'other-region cell',bad)
print('with cover',sum(1 for s in S if s[17]>=0),'unique uris',len({s[0] for s in S}))
print(collections.Counter(f['k'] for f in W['features']))
fs=sorted(W['features'],key=lambda f:-f['c'])
print([f['n'] for f in fs[:60]])
print([f['n'] for f in fs if f['k']=='sea'][:45])
print([f['n'] for f in fs if f['c']==1][:40])
dup=[n for n,c in collections.Counter(f['n'] for f in fs).items() if c>1]; print('dup names',dup)
