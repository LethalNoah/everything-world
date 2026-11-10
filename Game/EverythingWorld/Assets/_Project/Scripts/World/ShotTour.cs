using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using EverythingWorld.Music;
using EverythingWorld.Player;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EverythingWorld.World
{
    /// A photo tour of the built lands, so Claude can look at them without the owner testing every round.
    /// It only runs when Unity is started with "-ewShots <land>;<land>" (or "-ewShots all"): see
    /// build/take_shots.py, which runs Unity in the background with the editor closed.
    ///
    /// With Spotify switched off, it walks the real player round each land and photographs through the real
    /// camera (the land's own sky, fog, bloom and lights): from where you arrive looking four ways, from in front
    /// of every artist area at eye height, and one high view over the whole land. Photos go to
    /// Logs/Shots/<Land>/ with a list (shots.json); then Unity closes.
    public class ShotTour : MonoBehaviour
    {
        string[] lands;
        const int W = 1600, H = 900;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Boot()
        {
            var args = System.Environment.GetCommandLineArgs();
            for (var i = 0; i < args.Length - 1; i++)
                if (args[i] == "-ewShots")
                {
                    var go = new GameObject("Shot tour");
                    DontDestroyOnLoad(go);
                    go.AddComponent<ShotTour>().lands = args[i + 1].Split(';');
                    SceneManager.sceneLoaded += (_, __) => QuietSpotify();
                    return;
                }
        }

        /// Never touch anyone's Spotify while taking photos.
        static void QuietSpotify()
        {
            foreach (var sw in FindObjectsByType<MusicOutputSwitch>(FindObjectsSortMode.None)) sw.useSpotify = false;
            foreach (var mb in FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (mb != null && mb.GetType().Name == "SpotifyMusicOutput") mb.enabled = false;
        }

        IEnumerator Start()
        {
            QuietSpotify();
            // wait for the world to be built
            while (IslandWorldBuilder.Instance == null || IslandWorldBuilder.Instance.Data == null) yield return null;
            for (var i = 0; i < 30; i++) yield return null;
            var data = IslandWorldBuilder.Instance.Data;
            var player = FindAnyObjectByType<SimplePlayerController>();
            if (player == null) { Debug.LogError("Shot tour: no player"); Quit(1); yield break; }
            var controller = player.GetComponent<CharacterController>();
            player.enabled = false;
            if (controller != null) controller.enabled = false;
            var pivot = player.cameraPivot;
            var cam = player.GetComponentInChildren<Camera>();
            Cursor.lockState = CursorLockMode.None;

            var wanted = new List<string>();
            foreach (var l in lands)
            {
                if (l == "all") { foreach (var isl in data.layout.islands) if (IslandWorldBuilder.Instance.IsDressed(isl.sceneId)) wanted.Add(isl.name); }
                else wanted.Add(l.StartsWith("The ") || Find(data, l) != null ? l : "The " + l);
            }
            foreach (var land in wanted)
            {
                var isl = Find(data, land) ?? Find(data, "The " + land);
                if (isl == null) { Debug.LogWarning("Shot tour: no land called " + land); continue; }
                var dir = Path.Combine(Application.dataPath, "..", "Logs", "Shots", isl.name.Replace("The ", "").Replace(" ", "_").Replace("&", "and"));
                if (Directory.Exists(dir)) foreach (var f in Directory.GetFiles(dir, "*.png")) File.Delete(f);
                Directory.CreateDirectory(dir);
                var list = new StringBuilder("[");
                var n = 0;
                IEnumerator Shot(string name, Vector3 feet, float yaw, float pitch, float settle)
                {
                    player.transform.SetPositionAndRotation(feet, Quaternion.Euler(0f, yaw, 0f));
                    if (pivot != null) pivot.localEulerAngles = new Vector3(pitch, 0f, 0f);
                    var until = Time.realtimeSinceStartup + settle;
                    while (Time.realtimeSinceStartup < until) yield return null;      // (no WaitForEndOfFrame: it never comes when Unity runs without a window)
                    var file = $"{n:00}_{Safe(name)}.png";
                    Capture(cam, Path.Combine(dir, file));
                    if (n > 0) list.Append(',');
                    list.Append("{\"file\":\"").Append(file).Append("\",\"name\":\"").Append(name.Replace("\"", "'")).Append("\"}");
                    n++;
                }
                var plaza = new Vector3(isl.labelX, 0f, isl.labelZ);
                var feetAt = Feet(plaza);
                // where you arrive, looking four ways (the first one waits for the land's sky to settle)
                string[] compass = { "north", "east", "south", "west" };
                for (var k = 0; k < 4; k++) yield return Shot("arrival, looking " + compass[k], feetAt, k * 90f, 4f, k == 0 ? 6f : .8f);
                // in front of every artist area, looking at it
                var kit0 = LandKit.For(isl.name);
                foreach (var a in data.layout.artists)
                {
                    if (a.sceneId != isl.sceneId) continue;
                    var c = new Vector3(a.x, 0f, a.z);
                    var toward = kit0 != null ? kit0.ArtistFront(data, a) : (plaza - c).normalized;
                    var stand = c + toward * Mathf.Clamp(a.r * .75f, 11f, 28f);
                    var look = c - stand; look.y = 0f;
                    yield return Shot(a.name, Feet(stand), Quaternion.LookRotation(look).eulerAngles.y, 10f, .8f);
                }
                // one high view over the whole land
                var high = plaza - Vector3.forward * 140f; high.y = data.HeightAt(plaza.x, plaza.z) + 110f;
                yield return Shot("over the land", high, 0f, 32f, 1.2f);
                // the land's song-triggered moments (each kit lists its own in SpecialSongs)
                var kit = LandKit.For(isl.name);
                if (kit != null)
                    foreach (var (song, label, artist) in kit.SpecialSongs)
                    {
                        MusicDirector.ForcedTrackId = song;
                        IslandWorldData.Artist where = null;
                        foreach (var a in data.layout.artists) if (a.sceneId == isl.sceneId && a.name == artist) where = a;
                        if (where != null)
                        {
                            var c = new Vector3(where.x, 0f, where.z);
                            var toward = kit.ArtistFront(data, where);
                            var stand = c + toward * Mathf.Clamp(where.r * .9f, 14f, 40f);
                            var look = c - stand; look.y = 0f;
                            yield return Shot($"{label}, at {artist}", Feet(stand), Quaternion.LookRotation(look).eulerAngles.y, 0f, 12f);
                            var behind = c + toward * Mathf.Clamp(where.r * 1.8f, 30f, 70f); behind.y = data.HeightAt(behind.x, behind.z) + 22f;
                            yield return Shot($"{label}, at {artist} from higher", behind, Quaternion.LookRotation(look).eulerAngles.y, 14f, 2f);
                        }
                        else
                            for (var k = 0; k < 4; k++) yield return Shot($"{label}, looking {compass[k]}", feetAt, k * 90f, 8f, k == 0 ? 12f : 1f);
                        yield return Shot($"{label}, from above", high, 0f, 32f, 1f);
                        MusicDirector.ForcedTrackId = null;
                        var calm = Time.realtimeSinceStartup + 10f;
                        while (Time.realtimeSinceStartup < calm) yield return null;
                    }
                list.Append(']');
                File.WriteAllText(Path.Combine(dir, "shots.json"), list.ToString());
                Debug.Log($"Shot tour: {n} photos of {isl.name} in {dir}");
            }
            Quit(0);
        }

        static IslandWorldData.Island Find(IslandWorldData data, string name)
        {
            foreach (var isl in data.layout.islands) if (isl.name == name) return isl;
            return null;
        }

        /// Where the player's feet go: on whatever is solid there (a deck, a floor, the ground).
        static Vector3 Feet(Vector3 p)
        {
            var ground = IslandWorldBuilder.Instance.Data.HeightAt(p.x, p.z);
            // stand on a deck or a floor if there is one, but never on a roof or a treetop
            foreach (var hit in Physics.RaycastAll(new Vector3(p.x, ground + 40f, p.z), Vector3.down, 80f, ~0, QueryTriggerInteraction.Ignore))
                if (hit.point.y > ground && hit.point.y < ground + 3f) ground = hit.point.y;
            return new Vector3(p.x, ground + .95f, p.z);                          // the player stands from its middle (1.8 m tall)
        }

        static string Safe(string s)
        {
            var sb = new StringBuilder();
            foreach (var ch in s) sb.Append(char.IsLetterOrDigit(ch) ? ch : '_');
            return sb.Length > 40 ? sb.ToString(0, 40) : sb.ToString();
        }

        static void Capture(Camera cam, string path)
        {
            var rt = RenderTexture.GetTemporary(W, H, 24, RenderTextureFormat.ARGB32);
            var prevTarget = cam.targetTexture; var prevAspect = cam.aspect;
            cam.targetTexture = rt; cam.aspect = W / (float)H;
            cam.Render();
            var prev = RenderTexture.active; RenderTexture.active = rt;
            var tex = new Texture2D(W, H, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, W, H), 0, 0); tex.Apply();
            RenderTexture.active = prev;
            cam.targetTexture = prevTarget; cam.aspect = prevAspect; cam.ResetAspect();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Destroy(tex); RenderTexture.ReleaseTemporary(rt);
        }

        static void Quit(int code)
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.Exit(code);
#else
            Application.Quit(code);
#endif
        }
    }
}
