using System.Collections.Generic;
using EverythingWorld.Music;
using UnityEngine;

namespace EverythingWorld.World
{
    /// The departures board round the hub's mast: one row for every land, showing the song its station is playing
    /// right now (worked out from the shared radio clock, so it is always right and friends see the same board),
    /// with the cover, the land's colour and a bar for how far through the song it is. A row flips over like a
    /// split-flap board when its song changes. The land the jukebox's song comes from lights up, row and flag.
    public class HubBoard : MonoBehaviour
    {
        public class Row
        {
            public int sceneId; public string land;
            public TextMesh name, song; public Transform bar; public MeshFilter cover; public Material tab; public Color tabColor;
            public Material flag; public Color flagColor;
            public string shownId; public float flip = 1f; public SceneInfo scene;
        }

        public readonly List<Row> rows = new List<Row>();
        public Dictionary<string, int> coverOf = new Dictionary<string, int>();
        public System.Func<int, Rect> coverRect;
        public Jukebox jukebox;
        public float barLength = 6f;
        float next;

        void Update()
        {
            foreach (var r in rows)
                if (r.flip < 1f)
                {
                    r.flip = Mathf.Min(1f, r.flip + Time.deltaTime * 3f);
                    var s = new Vector3(1f, Mathf.Abs(Mathf.Cos(r.flip * Mathf.PI)), 1f);
                    r.name.transform.localScale = s; r.song.transform.localScale = s;
                }
            if (Time.time < next) return;
            next = Time.time + .25f;
            var d = MusicDirector.Instance; if (d == null || d.Catalog == null) return;
            var now = RadioSchedule.NowSeconds; var hubLand = jukebox != null ? jukebox.CurrentLand : null;
            foreach (var r in rows)
            {
                if (r.scene == null) r.scene = d.Catalog.Get(r.sceneId);
                if (r.scene == null) continue;
                var slot = RadioSchedule.At(r.scene, now); if (!slot.IsValid) continue;
                if (slot.track.id != r.shownId)
                {
                    r.shownId = slot.track.id; r.flip = r.name.text.Length > 0 && r.song.text.Length > 0 ? 0f : 1f;
                    r.song.text = Clip(slot.track.name, 34) + "  ·  " + Clip(slot.track.artist, 22);
                    SetCover(r.cover, coverOf.TryGetValue(slot.track.id, out var c) ? c : -1);
                }
                var p = Mathf.Clamp01((float)((now - slot.startsAt) / System.Math.Max(1.0, slot.endsAt - slot.startsAt)));
                if (r.bar != null) r.bar.localScale = new Vector3(Mathf.Max(.001f, p * barLength), 1f, 1f);
                var lit = hubLand != null && hubLand == r.land;
                var pulse = lit ? 2.2f + Mathf.Sin(Time.time * 4f) * .8f : 1f;
                if (r.tab != null) r.tab.SetColor("_EmissionColor", r.tabColor * pulse);
                if (r.flag != null) r.flag.SetColor("_EmissionColor", r.flagColor * (lit ? pulse : 1f));
                r.name.color = lit ? Color.white : new Color(1f, .8f, .42f);
            }
        }

        void SetCover(MeshFilter f, int index)
        {
            if (f == null || coverRect == null) return;
            var r = index >= 0 ? coverRect(index) : new Rect(0, 0, 0, 0);
            f.mesh.SetUVs(0, new List<Vector2> { new Vector2(r.xMin, r.yMin), new Vector2(r.xMax, r.yMin), new Vector2(r.xMax, r.yMax), new Vector2(r.xMin, r.yMax) });
        }

        static string Clip(string s, int max) => string.IsNullOrEmpty(s) || s.Length <= max ? s ?? "" : s.Substring(0, max - 1) + "…";

        /// A random land and what it's playing, for anyone who asks the board to choose.
        public string Pick()
        {
            if (rows.Count == 0) return "The board is still waking up.";
            var r = rows[Random.Range(0, rows.Count)];
            return r.shownId == null ? $"Go to {r.land}." : $"{r.land}. Right now: {r.song.text.Replace("  ·  ", " by ")}. Go!";
        }
    }
}
