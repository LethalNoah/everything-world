"""Export the written descriptions of the lands built so far, for the map on the website.

Reads ../WORLD_GUIDE.md and ../world/assets/world.js (for the lands' names) and writes
../world/assets/guide.js: for every land marked "built in Unity" in the guide, how you arrive, the vibe,
what happens there and its named places. The map (world/app.js) flags those lands and shows the text when
you click one. Words only: no pictures of the game are published.

    python guide_export.py

Quick (no playlist needed); run it whenever the guide changes.
"""
import json, os, re, sys

sys.stdout.reconfigure(encoding='utf-8')
HERE = os.path.dirname(os.path.abspath(__file__))
GUIDE = os.path.join(HERE, '..', 'WORLD_GUIDE.md')
WORLD = os.path.join(HERE, '..', 'world', 'assets', 'world.js')
OUT = os.path.join(HERE, '..', 'world', 'assets', 'guide.js')


def key(name):
    return re.sub(r'^the\s+', '', name.strip().lower())


def plain(text):
    """Markdown to plain text: the page escapes it and shows it as it is."""
    text = re.sub(r'\*\*(.+?)\*\*', r'\1', text)
    text = re.sub(r'(?<!\*)\*(?!\*)(.+?)\*', r'\1', text)
    text = re.sub(r'`(.+?)`', r'\1', text)
    return re.sub(r'\s+', ' ', text).strip()


def main():
    js = open(WORLD, encoding='utf-8').read()
    world = json.loads(js[js.index('{'):js.rindex('}') + 1])
    names = {key(r['n']): r['n'] for r in world['regions']}

    guide = open(GUIDE, encoding='utf-8').read()
    out, unmatched = {}, []
    for part in re.split(r'\n### ', guide)[1:]:
        title, _, body = part.partition('\n')
        body = re.split(r'\n## ', body)[0]          # stop at the next chapter of the guide
        first = body.strip().split('\n', 1)[0]
        if 'built in Unity' not in first:
            continue
        name = names.get(key(title))
        if name is None:
            unmatched.append(title.strip())
            continue
        land = {}
        # paragraphs that start "**Label:** text"
        for label, text in re.findall(r'^\*\*(.+?):\*\*[ \t]*(.*)$', body, flags=re.M):
            low = label.lower()
            if low == 'arriving':
                land['arriving'] = plain(text)
            elif low == 'the vibe':
                land['vibe'] = plain(text)
            elif low == 'happenings':
                land['happenings'] = plain(text)
            elif 'places' in low:
                land['roads'] = plain(text)
        # the named places: "- **Name:** what it is"
        places = []
        for pname, text in re.findall(r'^- \*\*(.+?)\*\*[ \t]*(.*)$', body, flags=re.M):
            pname = pname.strip().rstrip(':.,')
            places.append([plain(pname), plain(text).lstrip(':,. ')])
        if places:
            land['places'] = places
        out[name] = land

    with open(OUT, 'w', encoding='utf-8', newline='\n') as f:
        f.write('window.GUIDE=' + json.dumps(out, ensure_ascii=False, separators=(',', ':')) + ';\n')
    print(f'{len(out)} built lands written to world/assets/guide.js ({os.path.getsize(OUT) // 1024} KB)')
    for n, l in out.items():
        print(f"  {n}: {len(l.get('places', []))} places{'' if 'arriving' in l else ', NO ARRIVING'}{'' if 'vibe' in l else ', NO VIBE'}")
    if unmatched:
        print('NOT ON THE MAP (name differs from world.js):', ', '.join(unmatched))


if __name__ == '__main__':
    main()
