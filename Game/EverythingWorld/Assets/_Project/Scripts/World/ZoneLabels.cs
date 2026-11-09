using System.Collections.Generic;
using UnityEngine;

namespace EverythingWorld.World
{
    /// Names on the zone curtains: as you walk up to a curtain, the name of what's beyond it fades in on the
    /// curtain itself, in its colour (a land, "Bridge to ...", or "♪ artist" for an artist's station). Where
    /// several curtains are close together, each shows its own name, so you can tell them apart.
    public class ZoneLabels : MonoBehaviour
    {
        public float showWithin = 26f, fullWithin = 8f;
        const int MaxLabels = 3;
        const float Cell = 32f;

        struct Edge
        {
            public Vector3 a, b, n;            // ends at ground level, and the + side normal
            public float ground;
            public string plusText, minusText; // what you'd enter, seen from the + side / the - side
            public Color plusColor, minusColor;
        }

        readonly List<Edge> edges = new List<Edge>();
        readonly Dictionary<(int, int), List<int>> grid = new Dictionary<(int, int), List<int>>();
        readonly TextMesh[] shown = new TextMesh[MaxLabels];
        float nextUpdate;

        /// A curtain piece from a to b; plusNormal points to the side whose viewers see plusText.
        public void Add(Vector3 a, Vector3 b, float ground, Vector3 plusNormal, string plusText, Color plusColor, string minusText, Color minusColor)
        {
            var i = edges.Count;
            edges.Add(new Edge { a = a, b = b, n = plusNormal, ground = ground, plusText = plusText, minusText = minusText, plusColor = plusColor, minusColor = minusColor });
            var key = (Mathf.FloorToInt((a.x + b.x) / 2f / Cell), Mathf.FloorToInt((a.z + b.z) / 2f / Cell));
            if (!grid.TryGetValue(key, out var list)) grid[key] = list = new List<int>();
            list.Add(i);
        }

        void Start()
        {
            for (var k = 0; k < MaxLabels; k++)
            {
                var t = ArtistBits.Label(transform, "", Vector3.zero, Vector3.forward, .5f, Color.white, TextAnchor.MiddleCenter);
                t.gameObject.name = "Zone name";
                t.gameObject.SetActive(false);
                shown[k] = t;
            }
        }

        void LateUpdate()
        {
            var cam = Camera.main;
            if (cam == null || Time.time < nextUpdate) return;
            nextUpdate = Time.time + .1f;
            var eye = cam.transform.position;
            var cx = Mathf.FloorToInt(eye.x / Cell); var cz = Mathf.FloorToInt(eye.z / Cell);

            // the nearest piece of each distinct curtain name around you
            var best = new Dictionary<string, (float d, Vector3 p, Color c)>();
            for (var dx = -1; dx <= 1; dx++)
            for (var dz = -1; dz <= 1; dz++)
            {
                if (!grid.TryGetValue((cx + dx, cz + dz), out var list)) continue;
                foreach (var i in list)
                {
                    var e = edges[i];
                    var p = Closest(e.a, e.b, eye);
                    var d = Vector2.Distance(new Vector2(p.x, p.z), new Vector2(eye.x, eye.z));
                    if (d > showWithin) continue;
                    var plus = Vector3.Dot(new Vector3(eye.x - p.x, 0, eye.z - p.z), e.n) >= 0;
                    var text = plus ? e.plusText : e.minusText;
                    if (string.IsNullOrEmpty(text)) continue;
                    if (!best.TryGetValue(text, out var cur) || d < cur.d)
                        best[text] = (d, new Vector3(p.x, Mathf.Max(e.ground, p.y) + 2.6f, p.z), plus ? e.plusColor : e.minusColor);
                }
            }
            var order = new List<KeyValuePair<string, (float d, Vector3 p, Color c)>>(best);
            order.Sort((x, y) => x.Value.d.CompareTo(y.Value.d));
            for (var k = 0; k < MaxLabels; k++)
            {
                var t = shown[k];
                if (k >= order.Count) { t.gameObject.SetActive(false); continue; }
                var (d, p, c) = order[k].Value;
                p.y = Mathf.Max(p.y, eye.y + .4f);
                t.gameObject.SetActive(true);
                t.text = order[k].Key;
                t.transform.position = p;
                t.transform.rotation = Quaternion.LookRotation(p - eye);
                var a = 1f - Mathf.InverseLerp(fullWithin, showWithin, d);
                t.color = new Color(Mathf.Lerp(c.r, 1f, .35f), Mathf.Lerp(c.g, 1f, .35f), Mathf.Lerp(c.b, 1f, .35f), a);
            }
        }

        static Vector3 Closest(Vector3 a, Vector3 b, Vector3 p)
        {
            var ab = b - a; ab.y = 0;
            var len2 = ab.sqrMagnitude;
            if (len2 < 1e-4f) return a;
            var t = Mathf.Clamp01(Vector3.Dot(new Vector3(p.x - a.x, 0, p.z - a.z), ab) / len2);
            return a + ab * t;
        }
    }
}
