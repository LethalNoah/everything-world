"""Labelled maps of the lands built in Unity.

The land audit (Game/EverythingWorld/Assets/_Project/Scripts/World/LandAudit.cs) photographs every built land
from straight above when you press Play, and writes where its artists are. This script labels those photos:
every artist's name (radio stations in gold, with their ring), where you arrive, a scale bar and the title.

    python land_map.py                 # every land with a photo
    python land_map.py "Shoegaze Fens" # just one (the photo's file name, spaces allowed)

Output: Game/EverythingWorld/Logs/LandAudit/<Land>_map.png (not committed).
"""
import glob, json, os, sys
from PIL import Image, ImageDraw, ImageFont

HERE = os.path.dirname(os.path.abspath(__file__))
DIR = os.path.join(HERE, '..', 'Game', 'EverythingWorld', 'Logs', 'LandAudit')
FONTS = 'C:/Windows/Fonts/'


def font(name, size):
    try:
        return ImageFont.truetype(FONTS + name, size)
    except OSError:
        return ImageFont.load_default()


def outlined(dr, xy, text, f, fill, anchor='mm', width=3):
    dr.text(xy, text, font=f, fill=fill, anchor=anchor, stroke_width=width, stroke_fill=(12, 10, 18))


def make(base):
    photo = os.path.join(DIR, base + '_top.png'); places = os.path.join(DIR, base + '_places.json')
    if not (os.path.exists(photo) and os.path.exists(places)):
        print('no photo for', base); return
    info = json.load(open(places, encoding='utf-8'))
    im = Image.open(photo).convert('RGB')
    W, H = im.size
    mpp = info['metresPerPixel']
    dr = ImageDraw.Draw(im, 'RGBA')

    def P(x, z):
        return ((x - info['left']) / mpp, (info['top'] - z) / mpp)

    name_f = font('arialbd.ttf', max(14, W // 110)); radio_f = font('arialbd.ttf', max(18, W // 80))
    title_f = font('georgiab.ttf', max(28, W // 45)); small_f = font('arial.ttf', max(14, W // 120))

    # rings and dots first, names on top
    artists = sorted(info['artists'], key=lambda a: a['radio'])
    for a in artists:
        x, y = P(a['x'], a['z']); r = a['r'] / mpp
        col = (255, 205, 90, 230) if a['radio'] else (255, 255, 255, 150)
        dr.ellipse((x - r, y - r, x + r, y + r), outline=col, width=3 if a['radio'] else 2)
        dr.ellipse((x - 4, y - 4, x + 4, y + 4), fill=col)
    taken = []
    for a in sorted(artists, key=lambda a: (not a['radio'], -a['songs'])):
        x, y = P(a['x'], a['z'])
        f = radio_f if a['radio'] else name_f
        label = a['name'] + (f'  ({a["songs"]})' if a['songs'] > 1 else '')
        # under the dot, or above it if that's where another name already is
        for dy in (18, -18, 34, -34, 50):
            box = dr.textbbox((x, y + dy), label, font=f, anchor='mm')
            if not any(box[0] < t[2] and box[2] > t[0] and box[1] < t[3] and box[3] > t[1] for t in taken):
                break
        taken.append(box)
        outlined(dr, (x, y + dy), label, f, (255, 215, 110) if a['radio'] else (255, 255, 255))

    ax, ay = P(*info['arrival'])
    dr.ellipse((ax - 10, ay - 10, ax + 10, ay + 10), outline=(90, 255, 255), width=4)
    outlined(dr, (ax, ay - 26), 'you arrive here', small_f, (120, 255, 255))

    # title, key, scale bar, north
    dr.rectangle((0, 0, W, 64 + W // 60), fill=(10, 10, 18, 200))
    dr.text((24, 18), info['land'], font=title_f, fill=(255, 255, 255))
    radio = sum(1 for a in artists if a['radio'])
    dr.text((24, 26 + W // 45), f'{len(artists)} artists ({radio} with their own radio, in gold; songs in brackets)   ·   photographed from above in Unity, no fog',
            font=small_f, fill=(215, 210, 225))
    bar = 100 / mpp; bx, by = W - bar - 40, H - 40
    dr.rectangle((bx, by, bx + bar, by + 8), fill=(255, 255, 255, 230))
    outlined(dr, (bx + bar / 2, by - 16), '100 m', small_f, (255, 255, 255))
    outlined(dr, (W - 40, 64 + W // 60 + 30), 'N', title_f, (255, 255, 255))
    out = os.path.join(DIR, base + '_map.png')
    im.save(out)
    print(out, im.size)


if __name__ == '__main__':
    if len(sys.argv) > 1:
        make(sys.argv[1].replace(' ', '_'))
    else:
        for p in sorted(glob.glob(os.path.join(DIR, '*_top.png'))):
            make(os.path.basename(p)[:-8])
