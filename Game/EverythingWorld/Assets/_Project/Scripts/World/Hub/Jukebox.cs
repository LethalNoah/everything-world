using System.Collections.Generic;
using EverythingWorld.Music;
using UnityEngine;

namespace EverythingWorld.World
{
    /// The hub's jukebox: shows the song on the radio, its cover and the land it comes from, and turns a
    /// pointer on top toward that land, so the hub keeps sending you out to discover the rest.
    public class Jukebox : MonoBehaviour
    {
        public TextMesh title, detail;
        public MeshFilter cover;
        public Transform pointer;
        public TextMesh[] sign;          // the land's name, written on both sides of the arrow
        public Dictionary<string, int> coverOf = new Dictionary<string, int>();
        public System.Func<int, Rect> coverRect;
        public Dictionary<string, (string land, Vector3 at)> homeOf = new Dictionary<string, (string, Vector3)>();
        string shownId;
        Vector3 target;

        void Update()
        {
            var d = MusicDirector.Instance;
            if (d != null && d.CurrentSlot.IsValid && d.CurrentSlot.track.id != shownId)
            {
                var t = d.CurrentSlot.track;
                shownId = t.id;
                title.text = Clip(t.name, 30);
                var home = homeOf.TryGetValue(t.artist ?? "", out var h) ? h : (null, Vector3.zero);
                detail.text = Clip(t.artist, 30) + (home.land != null ? "\nfrom " + home.land : "");
                target = home.land != null ? home.at : Vector3.zero;
                if (sign != null)
                {
                    var words = home.land != null ? $"{home.land}\n{Vector3.Distance(new Vector3(pointer.position.x, 0, pointer.position.z), home.at) / 1000f:0.0} km" : "";
                    foreach (var w in sign) if (w != null) w.text = words;
                }
                SetCover(coverOf.TryGetValue(t.id, out var c) ? c : -1);
            }
            if (pointer != null && target != Vector3.zero)
            {
                var dir = target - pointer.position; dir.y = 0;
                if (dir.sqrMagnitude > 1f)
                    pointer.rotation = Quaternion.Slerp(pointer.rotation, Quaternion.LookRotation(dir.normalized), Time.deltaTime * 1.5f);
            }
        }

        void SetCover(int index)
        {
            if (cover == null || coverRect == null) return;
            var r = index >= 0 ? coverRect(index) : new Rect(0, 0, 0, 0);
            cover.mesh.SetUVs(0, new List<Vector2> { new Vector2(r.xMin, r.yMin), new Vector2(r.xMax, r.yMin), new Vector2(r.xMax, r.yMax), new Vector2(r.xMin, r.yMax) });
        }

        static string Clip(string s, int max) => string.IsNullOrEmpty(s) || s.Length <= max ? s ?? "" : s.Substring(0, max - 1) + "…";
    }
}
