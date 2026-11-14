using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Networking;

namespace EverythingWorld.World
{
    /// An artist's "about" card: press E at an artist's plaque and a card opens on the screen with their name, a
    /// picture, a few lines about them, and what's on the playlist (songs, years, genres, who added them).
    /// The words come from Wikipedia when the artist has a page there (fetched when you open the card, credited,
    /// and kept on this computer so it opens straight away next time and works offline); otherwise from a short
    /// paragraph written for the game (Resources/ArtistAbout.json, which also names each artist's Wikipedia page,
    /// matched and checked by hand-run script so the right page opens). E, Esc or walking away closes it.
    public class AboutCard : MonoBehaviour
    {
        [System.Serializable] class Entry { public string name, wiki, text; }
        [System.Serializable] class EntryList { public Entry[] artists; }
        [System.Serializable] class Thumb { public string source; }
        [System.Serializable] class Summary { public string title, extract, description; public Thumb thumbnail; }

        static AboutCard instance;
        static Dictionary<string, Entry> entries;

        IslandWorldData.Artist artist; Vector3 openedAt; float openedTime;
        string body, credit, wikiTitle; Texture2D photo; bool loading;
        Vector2 scroll;
        GUIStyle card, title, sub, text, small, close;
        Texture2D bg, line;

        public static bool IsOpen => instance != null && instance.artist != null;
        /// The frame a card was last closed with Esc (so the same Esc doesn't also open the pause menu).
        public static int ClosedFrame = -1;
        /// What the open card says and where it's from (for the photo tour's check), or null while it's loading.
        public static string Showing => IsOpen && !instance.loading ? instance.body + "  [" + instance.credit + "]" : null;

        /// Opens the card for this artist (or closes it, if it's already showing them).
        public static void Toggle(IslandWorldData.Artist a, Vector3 where)
        {
            if (instance == null) instance = new GameObject("About card").AddComponent<AboutCard>();
            if (instance.artist == a) { instance.Close(); return; }
            instance.Open(a, where);
        }

        static Entry EntryFor(string name)
        {
            if (entries == null)
            {
                entries = new Dictionary<string, Entry>();
                var ta = Resources.Load<TextAsset>("ArtistAbout");
                if (ta != null) foreach (var e in JsonUtility.FromJson<EntryList>(ta.text).artists) entries[e.name] = e;
            }
            return entries.TryGetValue(name, out var x) ? x : null;
        }

        void Open(IslandWorldData.Artist a, Vector3 where)
        {
            EverythingWorld.Player.Passport.ReadArtist(a.name);
            artist = a; openedAt = where; openedTime = Time.time; scroll = Vector2.zero; photo = null; body = null; credit = null;
            var e = EntryFor(a.name); wikiTitle = e != null && !string.IsNullOrEmpty(e.wiki) ? e.wiki : null;
            if (wikiTitle != null) { StopAllCoroutines(); StartCoroutine(FromWikipedia(a, wikiTitle, e)); }
            else { body = e != null && !string.IsNullOrEmpty(e.text) ? e.text : Fallback(a); credit = e != null && !string.IsNullOrEmpty(e.text) ? "Written for Everything World" : null; }
        }

        void Close() { artist = null; StopAllCoroutines(); loading = false; }

        static string Fallback(IslandWorldData.Artist a) =>
            $"{a.name} {(a.songs == 1 ? "has one song" : $"has {a.songs} songs")} on the playlist" + (string.IsNullOrEmpty(a.genres) ? "." : $", in the {a.genres.Split(',')[0].Trim()} corner of the world.");

        static string CacheDir => Path.Combine(Application.persistentDataPath, "About");
        static string Safe(string s) { var c = s.ToCharArray(); for (var i = 0; i < c.Length; i++) if (!char.IsLetterOrDigit(c[i])) c[i] = '_'; return new string(c); }

        IEnumerator FromWikipedia(IslandWorldData.Artist a, string page, Entry e)
        {
            loading = true;
            var file = Path.Combine(CacheDir, Safe(page) + ".json"); string json = null;
            if (File.Exists(file)) json = File.ReadAllText(file);
            else
            {
                using (var req = UnityWebRequest.Get("https://en.wikipedia.org/api/rest_v1/page/summary/" + UnityWebRequest.EscapeURL(page.Replace(' ', '_')).Replace("+", "%20")))
                {
                    req.SetRequestHeader("Api-User-Agent", "EverythingWorldGame/1.0 (personal music game)");
                    req.timeout = 10;
                    yield return req.SendWebRequest();
                    if (req.result == UnityWebRequest.Result.Success) { json = req.downloadHandler.text; try { Directory.CreateDirectory(CacheDir); File.WriteAllText(file, json); } catch { } }
                }
            }
            if (artist != a) yield break;
            var s = json != null ? JsonUtility.FromJson<Summary>(json) : null;
            if (s != null && !string.IsNullOrEmpty(s.extract)) { body = s.extract; credit = "From Wikipedia, the free encyclopedia (CC BY-SA 4.0): " + s.title; }
            else { body = e != null && !string.IsNullOrEmpty(e.text) ? e.text : Fallback(a); credit = e != null && !string.IsNullOrEmpty(e.text) ? "Written for Everything World" : "(Wikipedia couldn't be reached)"; }
            loading = false;
            // the photo, if Wikipedia has one
            if (s != null && s.thumbnail != null && !string.IsNullOrEmpty(s.thumbnail.source))
            {
                var pfile = Path.Combine(CacheDir, Safe(page) + ".img");
                if (File.Exists(pfile)) { var t = new Texture2D(2, 2); if (t.LoadImage(File.ReadAllBytes(pfile))) photo = t; yield break; }
                using (var req = UnityWebRequestTexture.GetTexture(s.thumbnail.source))
                {
                    req.SetRequestHeader("Api-User-Agent", "EverythingWorldGame/1.0 (personal music game)");
                    yield return req.SendWebRequest();
                    if (req.result == UnityWebRequest.Result.Success && artist == a) { photo = DownloadHandlerTexture.GetContent(req); try { File.WriteAllBytes(pfile, req.downloadHandler.data); } catch { } }
                }
            }
        }

        void Update()
        {
            if (artist == null) return;
            var kb = Keyboard.current;
            if (kb != null && kb.escapeKey.wasPressedThisFrame) { ClosedFrame = Time.frameCount; Close(); }
            // walking away closes it
            var cam = Camera.main;
            if (cam != null && Time.time - openedTime > .5f && (cam.transform.position - openedAt).sqrMagnitude > 9f * 9f) Close();
        }

        void Styles()
        {
            if (card != null) return;
            bg = new Texture2D(1, 1); bg.SetPixel(0, 0, new Color(.07f, .065f, .08f, .94f)); bg.Apply();
            line = new Texture2D(1, 1); line.SetPixel(0, 0, new Color(1f, .78f, .42f, .8f)); line.Apply();
            card = new GUIStyle { normal = { background = bg } };
            title = new GUIStyle { font = GameFonts.Sign, fontSize = 34, wordWrap = true, normal = { textColor = new Color(1f, .85f, .55f) } };
            sub = new GUIStyle { font = GameFonts.Speech, fontSize = 15, wordWrap = true, normal = { textColor = new Color(.78f, .76f, .72f) } };
            text = new GUIStyle { font = GameFonts.Speech, fontSize = 18, wordWrap = true, richText = false, normal = { textColor = new Color(.96f, .95f, .92f) } };
            small = new GUIStyle(sub) { fontSize = 13, normal = { textColor = new Color(.62f, .6f, .58f) } };
            close = new GUIStyle(sub) { alignment = TextAnchor.UpperRight };
        }

        void OnGUI()
        {
            if (artist == null) return;
            Styles();
            float w = Mathf.Min(820f, Screen.width - 60f), h = Mathf.Min(560f, Screen.height - 80f);
            var r = new Rect((Screen.width - w) / 2f, (Screen.height - h) / 2f, w, h);
            GUI.Box(r, GUIContent.none, card);
            GUI.DrawTexture(new Rect(r.x, r.y, r.width, 3f), line);
            const float pad = 26f, pic = 200f;
            // the picture: their Wikipedia photo, or an album cover from the playlist
            var picRect = new Rect(r.x + pad, r.y + pad + 4f, pic, pic);
            if (photo != null)
            {
                var aspect = photo.width / (float)photo.height; var pr = picRect;
                if (aspect > 1f) { pr.height = pic / aspect; pr.y += (pic - pr.height) / 2f; } else { pr.width = pic * aspect; pr.x += (pic - pr.width) / 2f; }
                GUI.DrawTexture(pr, photo, ScaleMode.StretchToFill);
            }
            else if (artist.songList != null && artist.songList.Length > 0 && artist.songList[0].cover >= 0 && IslandWorldBuilder.Instance != null && IslandWorldBuilder.Instance.coverAtlas != null)
                GUI.DrawTextureWithTexCoords(picRect, IslandWorldBuilder.Instance.coverAtlas, CoverRect(artist.songList[0].cover));
            // the playlist facts under the picture
            var facts = new List<string>();
            facts.Add(artist.songs == 1 ? "1 song on the playlist" : $"{artist.songs} songs on the playlist");
            if (!string.IsNullOrEmpty(artist.years)) facts.Add("Added " + artist.years.Replace("-", "–"));
            if (!string.IsNullOrEmpty(artist.addedBy)) facts.Add("By " + artist.addedBy);
            if (artist.station) facts.Add("♪ Has their own radio");
            GUI.Label(new Rect(picRect.x, picRect.yMax + 12f, pic, 120f), string.Join("\n", facts), sub);
            // the name, where they are, the words
            var tx = picRect.xMax + pad; var tw = r.xMax - pad - tx;
            GUI.Label(new Rect(tx, r.y + pad - 4f, tw - 120f, 44f), artist.name, title);
            GUI.Label(new Rect(r.xMax - pad - 120f, r.y + 14f, 120f, 20f), "E or Esc to close", close);
            var land = Land(); var genres = string.IsNullOrEmpty(artist.genres) ? "" : artist.genres;
            GUI.Label(new Rect(tx, r.y + pad + 44f, tw, 22f), (land != null ? land : "") + (land != null && genres.Length > 0 ? "  ·  " : "") + genres, sub);
            var words = loading ? "Looking them up…" : body ?? "";
            var songs = "";
            if (artist.songList != null && artist.songList.Length > 0)
            {
                songs = "\n\nOn the playlist\n";
                for (var i = 0; i < artist.songList.Length && i < 12; i++) songs += "•  " + artist.songList[i].title + "\n";
                if (artist.songList.Length > 12) songs += $"…and {artist.songList.Length - 12} more\n";
            }
            var all = words + songs; var view = new Rect(tx, r.y + pad + 76f, tw, r.yMax - pad - 22f - (r.y + pad + 76f));
            var contentH = text.CalcHeight(new GUIContent(all), tw - 18f);
            scroll = GUI.BeginScrollView(view, scroll, new Rect(0, 0, tw - 18f, contentH));
            GUI.Label(new Rect(0, 0, tw - 18f, contentH), all, text);
            GUI.EndScrollView();
            if (!string.IsNullOrEmpty(credit)) GUI.Label(new Rect(tx, r.yMax - pad - 14f, tw, 18f), credit, small);
        }

        string Land()
        {
            var d = IslandWorldBuilder.Instance != null ? IslandWorldBuilder.Instance.Data : null; if (d == null) return null;
            foreach (var isl in d.layout.islands) if (isl.sceneId == artist.sceneId) return isl.name;
            return null;
        }

        static Rect CoverRect(int index)
        {
            var l = IslandWorldBuilder.Instance.Data.layout;
            int cols = l.coverCols > 0 ? l.coverCols : 40, cell = l.coverCell > 0 ? l.coverCell : 96;
            float sheetW = l.coverSheetWidth > 0 ? l.coverSheetWidth : 3840f, sheetH = l.coverSheetHeight > 0 ? l.coverSheetHeight : 3744f;
            int col = index % cols, row = index / cols; float du = cell / sheetW, dv = cell / sheetH; const float inset = .004f;
            return new Rect((col + inset) * du, 1f - (row + 1 - inset) * dv, du * (1 - 2 * inset), dv * (1 - 2 * inset));
        }
    }
}
