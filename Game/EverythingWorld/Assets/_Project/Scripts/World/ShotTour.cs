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

            if (lands.Length == 1 && lands[0] == "perf") { yield return Perf(data, player, pivot, cam); Quit(0); yield break; }
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
                    var culler = FindAnyObjectByType<LandCuller>(); if (culler != null) culler.Refresh();
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
                // places the land marks for the tour itself: empty objects named "Shot: <what>" (at the feet, facing the view)
                var marks = new List<Transform>();
                var landRoot = GameObject.Find("Land: " + isl.name);
                if (landRoot != null) foreach (var t in landRoot.GetComponentsInChildren<Transform>(true)) if (t.name.StartsWith("Shot: ")) marks.Add(t);
                foreach (var m in marks) yield return Shot(m.name.Substring(6), m.position, m.eulerAngles.y, 6f, 1f);
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
                        else if (artist != null && artist.StartsWith("@") && marks.Exists(m => m.name == "Shot: " + artist.Substring(1)))
                        {
                            var m = marks.Find(x => x.name == "Shot: " + artist.Substring(1));
                            yield return Shot($"{label}, at {artist.Substring(1)}", m.position, m.eulerAngles.y, 4f, 12f);
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

        /// Performance: from where you arrive at the hub and at the busiest land, look eight ways and time the
        /// rendering (CPU, average of several frames) and count what is drawn. Writes Logs/perf.txt.
        IEnumerator Perf(IslandWorldData data, SimplePlayerController player, Transform pivot, Camera cam)
        {
            var sb = new StringBuilder($"Performance check, {System.DateTime.Now:yyyy-MM-dd HH:mm}\n");
            {
                // memory: the graphics card's size, and how much the built world's meshes and materials take
                long meshBytes = 0; var meshCount = 0; var vertCount = 0L;
                foreach (var m in Resources.FindObjectsOfTypeAll<Mesh>()) { meshBytes += UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(m); meshCount++; vertCount += m.vertexCount; }
                var mats = Resources.FindObjectsOfTypeAll<Material>().Length;
                var renderers = FindObjectsByType<Renderer>(FindObjectsSortMode.None).Length;
                var byName = new Dictionary<string, (long v, int n, long bytes)>();
                foreach (var m in Resources.FindObjectsOfTypeAll<Mesh>())
                {
                    var key = m.name; byName.TryGetValue(key, out var e);
                    byName[key] = (e.v + m.vertexCount, e.n + 1, e.bytes + UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(m));
                }
                var top = new List<KeyValuePair<string, (long v, int n, long bytes)>>(byName); top.Sort((x, y) => y.Value.bytes.CompareTo(x.Value.bytes));
                for (var i = 0; i < Mathf.Min(25, top.Count); i++) sb.AppendLine($"  mesh '{top[i].Key}': {top[i].Value.n} of them, {top[i].Value.v / 1000} thousand vertices, {top[i].Value.bytes / (1024 * 1024)} MB");
                // per land: vertices under each land's root
                foreach (Transform land in GameObject.Find("Lands") != null ? GameObject.Find("Lands").transform : transform)
                {
                    long lv = 0; foreach (var mf in land.GetComponentsInChildren<MeshFilter>(true)) if (mf.sharedMesh != null) lv += mf.sharedMesh.vertexCount;
                    if (lv > 300000) sb.AppendLine($"  land '{land.name}': {lv / 1000} thousand vertices");
                }
                sb.AppendLine($"Graphics: {SystemInfo.graphicsDeviceName}, {SystemInfo.graphicsMemorySize} MB. Meshes: {meshCount} using {meshBytes / (1024 * 1024)} MB, {vertCount / 1000} thousand vertices. Materials: {mats}. Renderers: {renderers}. Allocated: {UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong() / (1024 * 1024)} MB");
            }
            var meshTris = new Dictionary<Mesh, int>();
            foreach (var place in new[] { "The Everything Hub", "Kingdom of Classic Rock", "The Hair Metal Strip", "Arena Rock Ridge", "The Psychedelic Wilds" })
            {
                var isl = Find(data, place); if (isl == null) continue;
                var feet = Feet(new Vector3(isl.labelX, 0f, isl.labelZ));
                float totalMs = 0f; long totalTris = 0; var worst = 0f;
                for (var k = 0; k < 8; k++)
                {
                    player.transform.SetPositionAndRotation(feet, Quaternion.Euler(0f, k * 45f, 0f));
                    if (pivot != null) pivot.localEulerAngles = new Vector3(2f, 0f, 0f);
                    var culler = FindAnyObjectByType<LandCuller>(); if (culler != null) culler.Refresh();
                    var until = Time.realtimeSinceStartup + (k == 0 ? 6f : 1f);
                    while (Time.realtimeSinceStartup < until) yield return null;
                    var rt = RenderTexture.GetTemporary(W, H, 24); var prev = cam.targetTexture; cam.targetTexture = rt;
                    cam.Render();
                    var sw = System.Diagnostics.Stopwatch.StartNew();
                    for (var f = 0; f < 8; f++) cam.Render();
                    var ms = (float)sw.Elapsed.TotalMilliseconds / 8f;
                    cam.targetTexture = prev; RenderTexture.ReleaseTemporary(rt);
                    var planes = GeometryUtility.CalculateFrustumPlanes(cam);
                    long tris = 0; var drawn = 0;
                    foreach (var r in FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
                    {
                        if (!r.enabled || !r.gameObject.activeInHierarchy || !GeometryUtility.TestPlanesAABB(planes, r.bounds)) continue;
                        var mf = r.GetComponent<MeshFilter>(); if (mf == null || mf.sharedMesh == null) continue;
                        if (!meshTris.TryGetValue(mf.sharedMesh, out var n)) meshTris[mf.sharedMesh] = n = mf.sharedMesh.isReadable ? mf.sharedMesh.triangles.Length / 3 : 0;
                        tris += n; drawn++;
                    }
                    sb.AppendLine($"  {place}, looking {k * 45}°: {ms:0.0} ms to render, {drawn} meshes, {tris / 1000} thousand triangles");
                    totalMs += ms; totalTris += tris; worst = Mathf.Max(worst, ms);
                }
                sb.AppendLine($"{place}: average {totalMs / 8f:0.0} ms, worst {worst:0.0} ms, average {totalTris / 8000} thousand triangles\n");
            }
            // what costs the most: from the hub's slowest view, switch each suspect off in turn and time again
            var hub = Find(data, "The Everything Hub");
            if (hub != null)
            {
                player.transform.SetPositionAndRotation(Feet(new Vector3(hub.labelX, 0f, hub.labelZ)), Quaternion.Euler(0f, 45f, 0f));
                if (pivot != null) pivot.localEulerAngles = new Vector3(2f, 0f, 0f);
                var wait = Time.realtimeSinceStartup + 2f; while (Time.realtimeSinceStartup < wait) yield return null;
                float Time8()
                {
                    var rt = RenderTexture.GetTemporary(W, H, 24); var prev = cam.targetTexture; cam.targetTexture = rt;
                    cam.Render();
                    var sw = System.Diagnostics.Stopwatch.StartNew();
                    for (var f = 0; f < 8; f++) cam.Render();
                    cam.targetTexture = prev; RenderTexture.ReleaseTemporary(rt);
                    return (float)sw.Elapsed.TotalMilliseconds / 8f;
                }
                sb.AppendLine($"Hub looking 45°, everything on: {Time8():0.0} ms");
                var clouds = new List<GameObject>();
                foreach (var cl in FindObjectsByType<CloudLayer>(FindObjectsSortMode.None)) { var c = cl.transform.Find("Clouds"); if (c != null && c.gameObject.activeSelf) { c.gameObject.SetActive(false); clouds.Add(c.gameObject); } }
                sb.AppendLine($"  without the clouds ({clouds.Count} cloud layers): {Time8():0.0} ms");
                foreach (var c in clouds) c.SetActive(true);
                var lightsOff = new List<Light>();
                foreach (var l in FindObjectsByType<Light>(FindObjectsSortMode.None)) if (l.enabled && l.type != LightType.Directional) { l.enabled = false; lightsOff.Add(l); }
                sb.AppendLine($"  without point and spot lights ({lightsOff.Count} lit): {Time8():0.0} ms");
                foreach (var l in lightsOff) l.enabled = true;
                var halos = new List<Renderer>();
                foreach (var r in FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None)) if (r.enabled && r.sharedMaterial != null && r.sharedMaterial.shader != null && r.sharedMaterial.shader.name.Contains("Glow Halo")) { r.enabled = false; halos.Add(r); }
                sb.AppendLine($"  without glow halos ({halos.Count}): {Time8():0.0} ms");
                foreach (var r in halos) r.enabled = true;
                var parts = new List<ParticleSystemRenderer>();
                foreach (var r in FindObjectsByType<ParticleSystemRenderer>(FindObjectsSortMode.None)) if (r.enabled) { r.enabled = false; parts.Add(r); }
                sb.AppendLine($"  without particles ({parts.Count}): {Time8():0.0} ms");
                foreach (var r in parts) r.enabled = true;
                var zone = GameObject.Find("Zone fields");
                var zoneR = new List<Renderer>();
                foreach (var r in FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None)) if (r.enabled && r.sharedMaterial != null && r.sharedMaterial.shader != null && r.sharedMaterial.shader.name.Contains("Zone")) { r.enabled = false; zoneR.Add(r); }
                sb.AppendLine($"  without zone curtains ({zoneR.Count}): {Time8():0.0} ms");
                foreach (var r in zoneR) r.enabled = true;
                var terrain = new List<Renderer>();
                foreach (var r in FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None)) if (r.enabled && r.name.StartsWith("Terrain")) { r.enabled = false; terrain.Add(r); }
                sb.AppendLine($"  without the terrain ({terrain.Count} chunks): {Time8():0.0} ms");
                foreach (var r in terrain) r.enabled = true;
                var shadows = QualitySettings.shadows; QualitySettings.shadows = ShadowQuality.Disable;
                sb.AppendLine($"  without shadows: {Time8():0.0} ms");
                QualitySettings.shadows = shadows;
            }
            var path = Path.Combine(Application.dataPath, "..", "Logs", "perf.txt");
            File.WriteAllText(path, sb.ToString());
            Debug.Log("Performance check written to " + path + "\n" + sb);
        }

        // never leave Unity running if something goes wrong (the editor's own timer is reset when Play starts)
        void Update()
        {
            if (Time.realtimeSinceStartup > 9f * 60f) { Debug.LogError("Shot tour: took too long, giving up"); Quit(2); }
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
