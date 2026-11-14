import json, glob, sys, os
S = sys.argv[1]; out = sys.argv[2]
wiki = json.load(open(S + '/artist_wiki.json', encoding='utf-8'))
written = {}
for f in sorted([S + '/artist_about_written.json']): written.update(json.load(open(f, encoding='utf-8')))
arts = []
for n in sorted(wiki, key=str.lower):
    w = wiki[n]['title'] if wiki.get(n) else ''
    t = written.get(n, '')
    arts.append(dict(name=n, wiki=w, text=t))
missing = [a['name'] for a in arts if not a['wiki'] and not a['text']]
json.dump(dict(artists=arts), open(out, 'w', encoding='utf-8'), ensure_ascii=False, indent=0)
print(len(arts), 'artists;', sum(1 for a in arts if a['wiki']), 'from Wikipedia;', sum(1 for a in arts if a['text']), 'written;', len(missing), 'with neither:', missing[:20])
