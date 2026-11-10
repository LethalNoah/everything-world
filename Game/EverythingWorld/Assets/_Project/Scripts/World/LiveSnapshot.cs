using System.IO;
using System.Text;
using EverythingWorld.Music;
using UnityEngine;
using UnityEngine.InputSystem;

namespace EverythingWorld.World
{
    /// So Claude can see what the owner sees while playing: press P (or F12) and the game view is saved to
    /// Logs/Live/ in the Unity project, with a line in Logs/Live/notes.txt saying where you were: the land,
    /// position, which way you faced, the nearest artist, the song playing, and whether you were indoors.
    /// Then tell Claude "look at my snapshots". Nothing here is committed (Logs is ignored).
    public class LiveSnapshot : MonoBehaviour
    {
        string dir; int count; float flash; string flashText; bool pending;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            var go = new GameObject("Live snapshots");
            DontDestroyOnLoad(go);
            go.AddComponent<LiveSnapshot>();
        }

        void Start()
        {
            dir = Path.Combine(Application.dataPath, "..", "Logs", "Live");
            Directory.CreateDirectory(dir);
            count = Directory.GetFiles(dir, "*.png").Length;
        }

        void Update()
        {
            flash -= Time.unscaledDeltaTime;
            if (pending) { pending = false; flash = 2f; }   // a frame later, so the message isn't in the picture
            var kb = Keyboard.current;
            if (kb == null || !(kb.pKey.wasPressedThisFrame || kb.f12Key.wasPressedThisFrame)) return;
            var cam = Camera.main; if (cam == null) return;
            var land = SkyDirector.Instance != null ? SkyDirector.Instance.CurrentLand : "?";
            var stamp = System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            var file = $"{++count:000}_{stamp}.png";
            ScreenCapture.CaptureScreenshot(Path.Combine(dir, file));

            var p = cam.transform.position; var f = cam.transform.forward;
            string[] dirs = { "east", "north-east", "north", "north-west", "west", "south-west", "south", "south-east" };
            var facing = dirs[(int)(Mathf.Repeat(Mathf.Atan2(f.z, f.x) * Mathf.Rad2Deg + 22.5f, 360f) / 45f) % 8];
            string near = "none"; var nd = float.MaxValue;
            var data = IslandWorldBuilder.Instance != null ? IslandWorldBuilder.Instance.Data : null;
            if (data != null)
                foreach (var a in data.layout.artists)
                {
                    var d = Vector2.Distance(new Vector2(a.x, a.z), new Vector2(p.x, p.z));
                    if (d < nd) { nd = d; near = a.name; }
                }
            var md = MusicDirector.Instance;
            var song = md != null && md.CurrentSlot.IsValid ? md.CurrentSlot.track.name + " by " + md.CurrentSlot.track.artist : "none";
            var ground = data != null ? data.HeightAt(p.x, p.z) : 0f;
            var sb = new StringBuilder();
            sb.Append($"{file}: {land}, at x {p.x:0} z {p.z:0}, eyes {p.y - ground:0.0} m above the ground, facing {facing} ");
            sb.Append($"(looking {(f.y > .25f ? "up" : f.y < -.25f ? "down" : "level")}), nearest artist {near} ({nd:0} m), ");
            sb.Append($"song: {song}{(IndoorSpaces.Contains(p) ? ", indoors" : "")}");
            File.AppendAllText(Path.Combine(dir, "notes.txt"), sb.ToString() + "\n");
            Debug.Log("Snapshot for Claude: " + sb);
            pending = true; flashText = "Snapshot " + count + " saved for Claude";
        }

        void OnGUI()
        {
            if (flash <= 0f) return;
            var style = new GUIStyle(GUI.skin.label) { fontSize = 22, alignment = TextAnchor.MiddleCenter };
            style.normal.textColor = new Color(1f, 1f, 1f, Mathf.Clamp01(flash));
            GUI.Label(new Rect(0, Screen.height - 70, Screen.width, 40), flashText, style);
        }
    }
}
