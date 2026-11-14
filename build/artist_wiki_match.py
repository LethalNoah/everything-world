import json, urllib.request, urllib.parse, re, time, sys
UA = {'User-Agent': 'EverythingWorldGame/1.0 (personal music game; artist summaries)'}
MUSIC = re.compile(r'\b(band|singer|rapper|musician|music|DJ|disc jockey|producer|composer|songwriter|group|duo|trio|orchestra|vocalist|guitarist|pianist|drummer|hip hop|record|ensemble|choir|YouTuber|comedian|entertainer|conductor|artist|act)\b', re.I)
def api(params):
    url = 'https://en.wikipedia.org/w/api.php?' + urllib.parse.urlencode(dict(params, format='json', formatversion=2))
    for k in range(8):
        try:
            with urllib.request.urlopen(urllib.request.Request(url, headers=UA), timeout=30) as r: return json.load(r)
        except urllib.error.HTTPError as e:
            wait = int(e.headers.get('Retry-After') or 0) or 20 * (k + 1); print('  HTTP', e.code, 'waiting', wait, flush=True); time.sleep(wait)
        except Exception as e: print('  error', e, flush=True); time.sleep(10)
    raise SystemExit('Wikipedia keeps refusing; try later')
def lookup(titles):
    """title asked -> (final title, description, disambiguation?) for pages that exist"""
    out = {}
    for i in range(0, len(titles), 50):
        chunk = titles[i:i + 50]
        r = api(dict(action='query', titles='|'.join(chunk), redirects=1, prop='description|pageprops', ppprop='disambiguation'))
        q = r.get('query', {}); fwd = {}
        for n in q.get('normalized', []): fwd[n['from']] = n['to']
        red = {x['from']: x['to'] for x in q.get('redirects', [])}
        pages = {p['title']: p for p in q.get('pages', []) if not p.get('missing') and not p.get('invalid')}
        for t in chunk:
            f = fwd.get(t, t); f = red.get(f, f)
            if f in pages: p = pages[f]; out[t] = (p['title'], p.get('description', ''), 'pageprops' in p and 'disambiguation' in p['pageprops'])
        time.sleep(1.0)
    return out
L = json.load(open(sys.argv[1], encoding='utf-8'))
names = sorted({a['name'] for a in L['artists']})
found = {}
for suffix in ['', ' (band)', ' (musician)', ' (rapper)', ' (singer)', ' (DJ)', ' (producer)', ' (group)', ' (composer)', ' (musical group)', ' (duo)']:
    todo = [n for n in names if n not in found]
    res = lookup([n + suffix for n in todo])
    for n in todo:
        v = res.get(n + suffix)
        if v and not v[2] and MUSIC.search(v[1] or ''): found[n] = dict(title=v[0], description=v[1])
    print(f'after "{suffix}": {len(found)} matched', flush=True)
json.dump({n: found.get(n) for n in names}, open(sys.argv[2], 'w', encoding='utf-8'), ensure_ascii=False, indent=1, sort_keys=True)
print('matched', len(found), 'of', len(names))
