using System.Collections.Generic;
using System.IO;
using EverythingWorld.Music;
using EverythingWorld.World;
using UnityEngine;
using UnityEngine.InputSystem;

namespace EverythingWorld.Player
{
    /// The passport: press P to open it. A stamp for every land you visit (in the land's colour, with the date),
    /// a stamp for every artist whose about card you read (E at their plaque), and a stamp for every song you listen
    /// to for 30 seconds, wherever you hear it. The left page lists the lands by realm with how much of each you've
    /// collected; the right page shows one land: its stamp, its artists and their songs. Saved on this computer
    /// (passport.json in the game's data folder). P or Esc closes it.
    public class Passport : MonoBehaviour
    {
        [System.Serializable] class Save { public List<string> lands = new List<string>(), landDates = new List<string>(), artists = new List<string>(), songs = new List<string>(); }

        class Land { public string name, realm; public int sceneId; public Color color; public List<Artist> artists = new List<Artist>(); public int songs; }
        class Artist { public string name; public List<(string id, string title)> songs = new List<(string, string)>(); }

        public static Passport Instance { get; private set; }
        /// Set by the photo tour, so test runs never touch the real passport.
        public static bool NoSave;
        public Key key = Key.P;

        Save save = new Save();
        readonly HashSet<string> lands = new HashSet<string>(), artists = new HashSet<string>(), songs = new HashSet<string>();
        readonly Dictionary<string, string> landDate = new Dictionary<string, string>();
        List<Land> book; int totalArtists, totalSongs;
        bool open; int selected; Vector2 leftScroll, rightScroll; float nextCheck, lastListen; string lastTrack; readonly Dictionary<string, float> listened = new Dictionary<string, float>();
        SimplePlayerController walker; PlayerInteractor hands;
        readonly Queue<string> news = new Queue<string>(); string showing; float showUntil;

        static string SavePath => Path.Combine(Application.persistentDataPath, "passport.json");

        void Awake()
        {
            Instance = this;
            lastListen = Time.time;
            walker = GetComponent<SimplePlayerController>(); hands = GetComponent<PlayerInteractor>();
            if (NoSave) return;
            try { if (File.Exists(SavePath)) save = JsonUtility.FromJson<Save>(File.ReadAllText(SavePath)) ?? new Save(); } catch { save = new Save(); }
            for (var i = 0; i < save.lands.Count; i++) { lands.Add(save.lands[i]); landDate[save.lands[i]] = i < save.landDates.Count ? save.landDates[i] : ""; }
            foreach (var a in save.artists) artists.Add(a);
            foreach (var s in save.songs) songs.Add(s);
            walker = GetComponent<SimplePlayerController>(); hands = GetComponent<PlayerInteractor>();
        }

        void Write()
        {
            save.lands.Clear(); save.landDates.Clear();
            foreach (var l in lands) { save.lands.Add(l); save.landDates.Add(landDate.TryGetValue(l, out var d) ? d : ""); }
            save.artists = new List<string>(artists); save.songs = new List<string>(songs);
            if (NoSave) return;
            try { File.WriteAllText(SavePath, JsonUtility.ToJson(save)); } catch { }
        }

        // ---------------------------------------------------------------- earning stamps
        void Stamp(string what) { news.Enqueue(what); Write(); }

        public void VisitLand(string land)
        {
            if (string.IsNullOrEmpty(land) || !lands.Add(land)) return;
            landDate[land] = System.DateTime.Now.ToString("d MMM yyyy"); Stamp("Passport stamp:  " + land);
        }

        /// Wipes every stamp (the terminal's reset code). The land you're standing in is stamped again next check.
        public void ResetAll()
        {
            lands.Clear(); landDate.Clear(); artists.Clear(); songs.Clear(); listened.Clear(); news.Clear(); showing = null; lastTrack = null;
            Write();
        }

        /// Called when an artist's about card is opened.
        public static void ReadArtist(string artist)
        {
            if (Instance == null || string.IsNullOrEmpty(artist) || !Instance.artists.Add(artist)) return;
            Instance.Stamp("Read about  " + artist);
        }

        void HeardSong(TrackInfo t)
        {
            if (t == null || string.IsNullOrEmpty(t.id) || !songs.Add(t.id)) return;
            Stamp("♪  " + Clip(t.name, 34) + "  stamped");
        }

        void Update()
        {
            if (Time.time >= nextCheck)
            {
                nextCheck = Time.time + .5f;
                if (SkyDirector.Instance != null) VisitLand(SkyDirector.Instance.CurrentLand);
                var d = MusicDirector.Instance;
                // a song's stamp takes 30 seconds of listening to it (they add up if you come and go while it plays)
                if (d != null && d.PlayingScene != null && d.CurrentSlot.IsValid && !songs.Contains(d.CurrentSlot.track.id))
                {
                    var id = d.CurrentSlot.track.id; listened.TryGetValue(id, out var sec); sec += Time.time - lastListen; listened[id] = sec;
                    if (sec >= 30f) { listened.Remove(id); HeardSong(d.CurrentSlot.track); }
                }
                lastListen = Time.time;
            }
            var kb = Keyboard.current; if (kb == null) return;
            if (kb[key].wasPressedThisFrame && !Typing()) Toggle();
            else if (open && kb.escapeKey.wasPressedThisFrame) Toggle();
            if (showing == null || Time.time > showUntil) { showing = news.Count > 0 ? news.Dequeue() : null; showUntil = Time.time + 3f; }
        }

        // the terminal and the about card take the keyboard for themselves
        bool Typing() => walker != null && !walker.enabled && !open;

        void Toggle()
        {
            open = !open;
            if (open)
            {
                Build();
                var here = SkyDirector.Instance != null ? SkyDirector.Instance.CurrentLand : null;
                if (book != null) for (var i = 0; i < book.Count; i++) if (book[i].name == here) selected = i;
                rightScroll = Vector2.zero;
                if (walker != null) walker.enabled = false; if (hands != null) hands.enabled = false;
                Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
            }
            else
            {
                StartCoroutine(GiveBack());
            }
        }

        System.Collections.IEnumerator GiveBack()
        {
            yield return null;   // a frame later, so the Esc that closed the book doesn't also free the cursor
            if (walker != null) walker.enabled = true; if (hands != null) hands.enabled = true;
            Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false;
        }

        /// A line about what's in the passport (for the photo tour's check).
        public string Summary()
        {
            Build(); if (book == null) return "passport: no book yet";
            var lines = $"passport: {lands.Count} of {book.Count} lands, {artists.Count} of {totalArtists} artists read, {songs.Count} of {totalSongs} songs heard";
            foreach (var l in book) if (l.name == "The Liquid Rapids") lines += $"; Liquid Rapids has {l.artists.Count} artists and {l.songs} songs, first {l.artists[0].name} ({l.artists[0].songs.Count})";
            return lines;
        }

        /// The book's contents: every land (the hub first, then realm by realm), its artists (most songs first)
        /// and their songs, from the world layout and the playlist's stations.
        void Build()
        {
            if (book != null) return;
            var data = IslandWorldBuilder.Instance != null ? IslandWorldBuilder.Instance.Data : null; var cat = MusicDirector.Instance != null ? MusicDirector.Instance.Catalog : null;
            if (data == null || cat == null) return;
            book = new List<Land>(); totalArtists = 0; totalSongs = 0;
            foreach (var isl in data.layout.islands)
            {
                var t = isl.tintA; var fb = t != null && t.Length >= 3 ? Color.Lerp(new Color(t[0] / 255f, t[1] / 255f, t[2] / 255f), Color.white, .2f) : new Color(.6f, .4f, .3f);
                var scene = cat.Get(isl.sceneId);
                var land = new Land { name = isl.name, realm = isl.sceneId == 1000 ? "" : isl.realm, sceneId = isl.sceneId, color = ZoneFields.VividColor(scene?.palette, fb) };
                if (isl.sceneId != 1000 && scene != null && scene.tracks != null)
                {
                    var by = new Dictionary<string, Artist>();
                    foreach (var tr in scene.tracks) { if (!by.TryGetValue(tr.artist ?? "", out var ar)) by[tr.artist ?? ""] = ar = new Artist { name = tr.artist }; ar.songs.Add((tr.id, tr.name)); }
                    land.artists.AddRange(by.Values);
                    land.artists.Sort((x, y) => x.songs.Count != y.songs.Count ? y.songs.Count.CompareTo(x.songs.Count) : string.Compare(x.name, y.name, System.StringComparison.OrdinalIgnoreCase));
                    foreach (var ar in land.artists) land.songs += ar.songs.Count;
                    totalArtists += land.artists.Count; totalSongs += land.songs;
                }
                book.Add(land);
            }
            book.Sort((x, y) => x.sceneId == 1000 ? -1 : y.sceneId == 1000 ? 1 : x.realm != y.realm ? string.CompareOrdinal(x.realm, y.realm) : string.CompareOrdinal(x.name, y.name));
        }

        // ---------------------------------------------------------------- the book
        GUIStyle title, head, body, small, smallR, smallC, row, rowOn, stampText, toast; Texture2D paper, paperD, ring, dot, ink, cover;
        static readonly Color Ink = new Color(.18f, .13f, .1f), Faint = new Color(.45f, .4f, .34f);

        static Texture2D Solid(Color c) { var t = new Texture2D(1, 1); t.SetPixel(0, 0, c); t.Apply(); return t; }

        /// A round rubber-stamp mark: a double ring, a little rough, white (tinted when drawn).
        static Texture2D Ring(bool filled)
        {
            const int n = 128; var t = new Texture2D(n, n, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Clamp };
            for (var y = 0; y < n; y++) for (var x = 0; x < n; x++)
            {
                var d = Vector2.Distance(new Vector2(x + .5f, y + .5f), new Vector2(n / 2f, n / 2f)) / (n / 2f);
                var a = filled ? Mathf.Clamp01((1f - d) * 20f) : Mathf.Max(Band(d, .9f, .055f), Band(d, .74f, .025f));
                a *= .75f + .25f * Mathf.PerlinNoise(x * .21f, y * .21f);   // uneven ink
                t.SetPixel(x, y, new Color(1, 1, 1, a));
            }
            t.Apply(); return t;
        }
        static float Band(float d, float at, float w) => Mathf.Clamp01(1f - Mathf.Abs(d - at) / w);

        void Styles()
        {
            if (title != null) return;
            cover = Solid(new Color(.24f, .12f, .1f)); toast = GameFonts.Box(16, TextAnchor.MiddleCenter); paper = Solid(new Color(.93f, .89f, .8f)); paperD = Solid(new Color(.86f, .81f, .7f)); ink = Solid(new Color(.18f, .13f, .1f, .25f));
            ring = Ring(false); dot = Ring(true);
            title = new GUIStyle { font = GameFonts.Sign, fontSize = 30, normal = { textColor = Ink } };
            head = new GUIStyle { font = GameFonts.Sign, fontSize = 19, normal = { textColor = Ink }, wordWrap = true };
            body = new GUIStyle { font = GameFonts.Speech, fontSize = 15, normal = { textColor = Ink }, wordWrap = true };
            small = new GUIStyle(body) { fontSize = 13, normal = { textColor = Faint } }; smallR = new GUIStyle(small) { alignment = TextAnchor.UpperRight }; smallC = new GUIStyle(small) { alignment = TextAnchor.MiddleCenter };
            row = new GUIStyle(body) { fontSize = 15, padding = new RectOffset(30, 4, 3, 3), normal = { textColor = Ink }, hover = { textColor = new Color(.55f, .2f, .1f), background = Solid(new Color(0, 0, 0, .05f)) } };
            rowOn = new GUIStyle(row) { normal = { textColor = Ink, background = Solid(new Color(.55f, .35f, .2f, .16f)) } };
            stampText = new GUIStyle { font = GameFonts.Sign, fontSize = 15, alignment = TextAnchor.MiddleCenter, wordWrap = true };
        }

        void StampAt(Rect r, Color c, string words, float angle, int size = 15)
        {
            var m = GUI.matrix; GUIUtility.RotateAroundPivot(angle, r.center);
            var old = GUI.color; GUI.color = new Color(c.r, c.g, c.b, .85f); GUI.DrawTexture(r, ring); GUI.color = old;
            stampText.fontSize = size; stampText.normal.textColor = new Color(c.r * .8f, c.g * .8f, c.b * .8f, .9f);
            GUI.Label(new Rect(r.x + r.width * .16f, r.y + r.height * .16f, r.width * .68f, r.height * .68f), words, stampText);
            GUI.matrix = m;
        }

        static string Clip(string s, int max) => string.IsNullOrEmpty(s) || s.Length <= max ? s ?? "" : s.Substring(0, max - 1) + "…";

        void OnGUI()
        {
            Styles();
            if (showing != null && !open)
            {
                var w = Mathf.Min(520f, Screen.width - 40f); var r = new Rect((Screen.width - w) / 2f, 54f, w, 34f);
                GUI.Box(r, showing, toast);
            }
            if (!open || book == null) return;
            float W = Mathf.Min(1060f, Screen.width - 50f), H = Mathf.Min(680f, Screen.height - 50f);
            var bk = new Rect((Screen.width - W) / 2f, (Screen.height - H) / 2f, W, H);
            GUI.DrawTexture(new Rect(bk.x - 8f, bk.y - 8f, bk.width + 16f, bk.height + 16f), cover);   // the cover
            var left = new Rect(bk.x, bk.y, bk.width * .4f, bk.height); var right = new Rect(left.xMax, bk.y, bk.width - left.width, bk.height);
            GUI.DrawTexture(left, paperD); GUI.DrawTexture(right, paper); GUI.DrawTexture(new Rect(left.xMax - 2f, bk.y, 4f, bk.height), ink);
            const float pad = 22f;
            // left page: the totals and every land
            int lv = 0; foreach (var l in book) if (lands.Contains(l.name)) lv++;
            GUI.Label(new Rect(left.x + pad, left.y + pad - 6f, left.width - 2 * pad, 38f), "PASSPORT", title);
            GUI.Label(new Rect(left.x + pad, left.y + pad + 32f, left.width - 2 * pad, 40f), $"{lv} of {book.Count} lands  ·  {artists.Count} of {totalArtists} artists read  ·  {songs.Count} of {totalSongs} songs heard", small);
            var list = new Rect(left.x + 8f, left.y + pad + 72f, left.width - 16f, left.height - pad - 92f);
            var rowsH = book.Count * 26f + 200f;
            leftScroll = GUI.BeginScrollView(list, leftScroll, new Rect(0, 0, list.width - 18f, rowsH));
            var y = 0f; string realm = null;
            for (var i = 0; i < book.Count; i++)
            {
                var l = book[i];
                if (l.realm != realm && !string.IsNullOrEmpty(l.realm)) { realm = l.realm; y += 6f; GUI.Label(new Rect(8f, y, list.width - 30f, 22f), realm.ToUpperInvariant(), small); y += 20f; }
                var rr = new Rect(0, y, list.width - 20f, 26f);
                if (GUI.Button(rr, l.name, i == selected ? rowOn : row)) { selected = i; rightScroll = Vector2.zero; }
                var old = GUI.color; GUI.color = lands.Contains(l.name) ? l.color : new Color(.5f, .45f, .4f, .5f);
                GUI.DrawTexture(new Rect(8f, y + 5f, 16f, 16f), lands.Contains(l.name) ? dot : ring); GUI.color = old;
                y += 26f;
            }
            GUI.EndScrollView();
            GUI.Label(new Rect(left.x + pad, left.yMax - 24f, left.width - 2 * pad, 20f), "P or Esc to close", small);
            // right page: one land
            var land = book[Mathf.Clamp(selected, 0, book.Count - 1)];
            int ra = 0, rs = 0; foreach (var a in land.artists) { if (artists.Contains(a.name)) ra++; foreach (var s in a.songs) if (songs.Contains(s.id)) rs++; }
            GUI.Label(new Rect(right.x + pad, right.y + pad - 6f, right.width - 2 * pad - 150f, 40f), land.name, title);
            GUI.Label(new Rect(right.x + pad, right.y + pad + 34f, right.width - 2 * pad - 150f, 40f),
                land.sceneId == 1000 ? "The whole playlist meets here." : $"{(string.IsNullOrEmpty(land.realm) ? "" : land.realm + "  ·  ")}{ra} of {land.artists.Count} artists read  ·  {rs} of {land.songs} songs heard", small);
            var big = new Rect(right.xMax - pad - 128f, right.y + 10f, 128f, 128f);
            if (lands.Contains(land.name)) StampAt(big, land.color, "VISITED\n" + (landDate.TryGetValue(land.name, out var dd) ? dd : ""), -14f, 16);
            else { var old = GUI.color; GUI.color = new Color(.5f, .45f, .4f, .35f); GUI.DrawTexture(big, ring); GUI.color = old; GUI.Label(new Rect(big.x + 20f, big.y + 44f, 88f, 40f), "not yet\nvisited", smallC); }
            var view = new Rect(right.x + pad, right.y + 150f, right.width - 2 * pad, right.height - 170f);
            if (land.artists.Count == 0) { GUI.Label(view, "Every song on the playlist plays here, so every song you hear counts toward the lands they come from.", body); return; }
            var contentH = 0f; foreach (var a in land.artists) contentH += 34f + a.songs.Count * 21f + 8f;
            rightScroll = GUI.BeginScrollView(view, rightScroll, new Rect(0, 0, view.width - 18f, contentH));
            y = 0f; var cw = view.width - 18f;
            foreach (var a in land.artists)
            {
                var read = artists.Contains(a.name);
                GUI.Label(new Rect(0, y, cw - 90f, 30f), a.name, head);
                if (read) StampAt(new Rect(cw - 70f, y - 8f, 46f, 46f), land.color, "READ", 12f, 11);
                else GUI.Label(new Rect(cw - 170f, y + 6f, 168f, 20f), "read their plaque", smallR);
                y += 34f;
                foreach (var s in a.songs)
                {
                    var heard = songs.Contains(s.id);
                    var old = GUI.color; GUI.color = heard ? land.color : new Color(.5f, .45f, .4f, .45f); GUI.DrawTexture(new Rect(14f, y + 3f, 14f, 14f), heard ? dot : ring); GUI.color = old;
                    GUI.Label(new Rect(36f, y, cw - 40f, 21f), Clip(s.title, 70), heard ? body : small);
                    y += 21f;
                }
                y += 8f;
            }
            GUI.EndScrollView();
        }
    }
}
