"""Photograph built lands in Unity, so Claude can look at them without the owner testing every round.

Close the Unity editor first (Unity allows one copy of a project open at a time). Then:

    python take_shots.py "The Shoegaze Fens"        # one land
    python take_shots.py "Lanternwood" "The Driftwood Shore"
    python take_shots.py all                         # every built land (slow)

Unity starts in the background (no window), loads the world with Spotify switched off, walks the player round
each land and photographs through the game's own camera (ShotTour.cs): from where you arrive looking four
ways, in front of every artist area, and one high view. It also runs the land audit (Logs/LandAudit/).
Photos: Game/EverythingWorld/Logs/Shots/<Land>/, plus contact sheets (sheet_1.png, ...) of 12 photos each.
Unity's log: Game/EverythingWorld/Logs/shots.log.
"""
import json, os, subprocess, sys, time
from PIL import Image, ImageDraw, ImageFont

HERE = os.path.dirname(os.path.abspath(__file__))
PROJECT = os.path.normpath(os.path.join(HERE, '..', 'Game', 'EverythingWorld'))
SHOTS = os.path.join(PROJECT, 'Logs', 'Shots')


def unity_exe():
    ver = None
    for line in open(os.path.join(PROJECT, 'ProjectSettings', 'ProjectVersion.txt'), encoding='utf-8'):
        if line.startswith('m_EditorVersion:'):
            ver = line.split(':', 1)[1].strip()
    exe = os.path.join('C:/Program Files/Unity/Hub/Editor', ver, 'Editor', 'Unity.exe')
    if not os.path.exists(exe):
        sys.exit(f'Unity {ver} not found at {exe}')
    return exe


def unity_open():
    lock = os.path.join(PROJECT, 'Temp', 'UnityLockfile')
    if not os.path.exists(lock):
        return False
    try:
        with open(lock, 'a'):
            return False
    except OSError:
        return True


def sheets(folder):
    listing = os.path.join(folder, 'shots.json')
    if not os.path.exists(listing):
        return []
    shots = json.load(open(listing, encoding='utf-8'))
    try:
        f = ImageFont.truetype('C:/Windows/Fonts/arialbd.ttf', 20)
    except OSError:
        f = ImageFont.load_default()
    out = []
    TW, TH, COLS, PER = 640, 360, 3, 12
    for s0 in range(0, len(shots), PER):
        part = shots[s0:s0 + PER]
        rows = (len(part) + COLS - 1) // COLS
        sheet = Image.new('RGB', (COLS * TW, rows * (TH + 30)), (16, 16, 22))
        dr = ImageDraw.Draw(sheet)
        for i, s in enumerate(part):
            im = Image.open(os.path.join(folder, s['file'])).convert('RGB').resize((TW, TH), Image.LANCZOS)
            x, y = (i % COLS) * TW, (i // COLS) * (TH + 30)
            sheet.paste(im, (x, y + 30))
            dr.text((x + 8, y + 4), f"{s0 + i + 1}. {s['name']}", font=f, fill=(235, 230, 245))
        p = os.path.join(folder, f'sheet_{s0 // PER + 1}.png')
        sheet.save(p)
        out.append(p)
    return out


def main():
    lands = sys.argv[1:] or ['all']
    if unity_open():
        sys.exit('Unity is open on this project: close the Unity editor first, then run this again.')
    log = os.path.join(PROJECT, 'Logs', 'shots.log')
    os.makedirs(os.path.dirname(log), exist_ok=True)
    cmd = [unity_exe(), '-batchmode', '-projectPath', PROJECT, '-executeMethod', 'EverythingWorld.EditorTools.ShotRunner.Run',
           '-ewShots', ';'.join(lands), '-logFile', log]
    print('Unity is photographing', ', '.join(lands), '(no window; this takes a few minutes)...')
    t0 = time.time()
    try:
        code = subprocess.run(cmd, timeout=12 * 60).returncode
    except subprocess.TimeoutExpired:
        sys.exit('Unity took too long; see ' + log)
    print(f'Unity finished in {time.time() - t0:.0f} s (exit code {code}).')
    if code != 0:
        print('Something went wrong; the end of the log:')
        print(''.join(open(log, encoding='utf-8', errors='replace').readlines()[-30:]))
    for d in sorted(os.listdir(SHOTS)) if os.path.isdir(SHOTS) else []:
        folder = os.path.join(SHOTS, d)
        if os.path.getmtime(folder) < t0 - 5:
            continue
        for p in sheets(folder):
            print(p)
    sys.exit(code)


if __name__ == '__main__':
    main()
