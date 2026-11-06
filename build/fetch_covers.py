"""Look up each album's cover via Spotify's public oEmbed endpoint, then download 300px JPEGs.
Covers land in ../covers_raw (data only, never executed). Safe to re-run: resumes from manifest."""
import csv,json,os,time,threading,urllib.request,urllib.parse,re
from concurrent.futures import ThreadPoolExecutor
OUT='../assets/covers'; os.makedirs(OUT,exist_ok=True)
MAN='cover_manifest.json'
rows=list(csv.DictReader(open('playlist.csv',encoding='utf-8-sig')))
albums={}
for r in rows:
    albums.setdefault(r['Album Name']+'|'+r['Artist Name(s)'].split(';')[0],r['Track URI'])
man=json.load(open(MAN,encoding='utf-8')) if os.path.exists(MAN) else {}
lock=threading.Lock(); UA={'User-Agent':'Mozilla/5.0 (personal playlist map)'}
def get(url,timeout=20):
    for a in range(6):
        try:
            return urllib.request.urlopen(urllib.request.Request(url,headers=UA),timeout=timeout).read()
        except urllib.error.HTTPError as e:
            if e.code==404: return None
            time.sleep(2**a if e.code!=429 else 15*(a+1))
        except Exception: time.sleep(2**a)
    return None
done=[0]
def work(item):
    key,uri=item
    if key in man and os.path.exists(f"{OUT}/{man[key]}.jpg"): return
    b=get('https://open.spotify.com/oembed?url='+urllib.parse.quote(uri,safe=':'))
    iid=None
    if b:
        m=re.search(r'/image/([0-9a-f]{40})',json.loads(b).get('thumbnail_url',''))
        if m:
            iid=m.group(1); p=f"{OUT}/{iid}.jpg"
            if not os.path.exists(p):
                img=get('https://i.scdn.co/image/'+iid)
                if img and img[:2]==b'\xff\xd8':
                    open(p,'wb').write(img)
                else: iid=None
    with lock:
        if iid: man[key]=iid
        done[0]+=1
        if done[0]%100==0:
            json.dump(man,open(MAN,'w',encoding='utf-8')); print(done[0],len(man),flush=True)
    time.sleep(0.05)
with ThreadPoolExecutor(5) as ex: list(ex.map(work,albums.items()))
json.dump(man,open(MAN,'w',encoding='utf-8'))
print('DONE albums',len(albums),'covers',len(man),flush=True)
