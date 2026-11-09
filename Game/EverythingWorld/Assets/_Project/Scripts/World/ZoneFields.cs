using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace EverythingWorld.World
{
    /// Builds the shimmering curtains that mark where the music changes (shader: Everything World/Zone Field):
    /// - along every border where two lands touch on a landmass,
    /// - across both ends of every bridge and causeway,
    /// - round the area of every artist with their own radio station.
    /// Each side of a curtain shows the colour of the place you'd walk into, and only that side can see it.
    public static class ZoneFields
    {
        public const float BorderHeight = 7f, GateHeight = 6f, RingHeight = 3.2f;
        // the shader draws each kind with its own pattern, told apart by the colour's alpha
        const float KindBorder = 1f, KindGate = .66f, KindRing = .33f;
        static Color Kind(Color c, float kind) => new Color(c.r, c.g, c.b, kind);
        public static readonly Color ArtistGold = new Color(1f, .78f, .42f);

        class Builder
        {
            public readonly List<Vector3> v = new List<Vector3>();
            public readonly List<Color> c = new List<Color>();
            public readonly List<Vector2> uv = new List<Vector2>();
            public readonly List<Vector2> motif = new List<Vector2>();
            public readonly List<int> t = new List<int>();

            /// One upright strip from p to q, seen only from the 'towards' side, in the given colour.
            public void Strip(Vector3 p, Vector3 q, float pGround, float qGround, float height, Vector3 towards, Color col, float along, float motifId = 0f)
            {
                Vector3 a = new Vector3(p.x, pGround - .6f, p.z), b = new Vector3(q.x, qGround - .6f, q.z);
                Vector3 c2 = new Vector3(q.x, qGround + height, q.z), d = new Vector3(p.x, pGround + height, p.z);
                var len = Vector3.Distance(new Vector3(p.x, 0, p.z), new Vector3(q.x, 0, q.z));
                var i = v.Count;
                v.Add(a); v.Add(b); v.Add(c2); v.Add(d);
                for (var k = 0; k < 4; k++) c.Add(col);
                uv.Add(new Vector2(along, 0)); uv.Add(new Vector2(along + len, 0)); uv.Add(new Vector2(along + len, 1)); uv.Add(new Vector2(along, 1));
                for (var k = 0; k < 4; k++) motif.Add(new Vector2(motifId, 0));
                // face the viewer on the 'towards' side
                if (Vector3.Dot(Vector3.Cross(b - a, c2 - a), towards) > 0) { t.Add(i); t.Add(i + 1); t.Add(i + 2); t.Add(i); t.Add(i + 2); t.Add(i + 3); }
                else { t.Add(i); t.Add(i + 2); t.Add(i + 1); t.Add(i); t.Add(i + 3); t.Add(i + 2); }
            }

            public void Build(string name, Transform parent, Material mat)
            {
                if (v.Count == 0) return;
                var mesh = new Mesh { name = name, indexFormat = IndexFormat.UInt32 };
                mesh.SetVertices(v); mesh.SetColors(c); mesh.SetUVs(0, uv); mesh.SetUVs(1, motif); mesh.SetTriangles(t, 0);
                mesh.RecalculateBounds();
                var go = new GameObject(name);
                go.transform.SetParent(parent, false);
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                var r = go.AddComponent<MeshRenderer>();
                r.sharedMaterial = mat;
                r.shadowCastingMode = ShadowCastingMode.Off;
                r.receiveShadows = false;
            }
        }

        public static void Build(IslandWorldData data, Transform parent, Color[] islandColor, System.Func<int, bool> landBuilt)
        {
            var shader = Shader.Find("Everything World/Zone Field");
            if (shader == null) { Debug.LogWarning("Zone Field shader not found: no border curtains."); return; }
            var mat = new Material(shader) { name = "Zone field" };
            var root = new GameObject("Zone fields").transform;
            root.SetParent(parent, false);

            var labels = root.gameObject.AddComponent<ZoneLabels>();
            Borders(data, root, mat, islandColor, labels);
            Gates(data, root, mat, islandColor, labels);
            Rings(data, root, mat, islandColor, landBuilt, labels);
        }

        // ---------- borders between lands on one landmass (marching squares on the island grid) ----------
        static void Borders(IslandWorldData data, Transform root, Material mat, Color[] islandColor, ZoneLabels labels)
        {
            var tiles = new Dictionary<(int, int), Builder>();
            float step = data.Step, depth = data.layout.depth;
            Vector3 V(int col, int row) => new Vector3(col * step, 0, depth - row * step);
            var segments = 0;
            for (var r = 0; r < data.Nz - 1; r++)
            for (var col = 0; col < data.Nx - 1; col++)
            {
                byte l0 = data.IslandIndexAtVertex(col, r), l1 = data.IslandIndexAtVertex(col + 1, r);
                byte l2 = data.IslandIndexAtVertex(col + 1, r + 1), l3 = data.IslandIndexAtVertex(col, r + 1);
                if (l0 == 255 || l1 == 255 || l2 == 255 || l3 == 255) continue;
                if (l0 == l1 && l1 == l2 && l2 == l3) continue;
                // the two lands here (skip the rare corners where three meet)
                byte a = l0, b = l0 != l1 ? l1 : l0 != l2 ? l2 : l3;
                if ((l1 != a && l1 != b) || (l2 != a && l2 != b) || (l3 != a && l3 != b)) continue;
                var corners = new[] { V(col, r), V(col + 1, r), V(col + 1, r + 1), V(col, r + 1) };
                var lab = new[] { l0, l1, l2, l3 };
                var mids = new List<Vector3>();
                for (var e = 0; e < 4; e++)
                    if (lab[e] != lab[(e + 1) % 4]) mids.Add((corners[e] + corners[(e + 1) % 4]) / 2f);
                if (mids.Count != 2) continue;
                Vector3 p = mids[0], q = mids[1];
                var dir = (q - p).normalized;
                var n = Vector3.Cross(Vector3.up, dir);
                // which land lies on the +n side
                var aSide = 0f;
                for (var k = 0; k < 4; k++) if (lab[k] == a) aSide += Vector3.Dot(corners[k] - (p + q) / 2f, n);
                byte plus = aSide > 0 ? a : b, minus = aSide > 0 ? b : a;
                float gp = data.HeightAt(p.x, p.z), gq = data.HeightAt(q.x, q.z);
                var key = (Mathf.FloorToInt(p.x / 256f), Mathf.FloorToInt(p.z / 256f));
                if (!tiles.TryGetValue(key, out var bld)) tiles[key] = bld = new Builder();
                var along = p.x * .7f + p.z * .7f;
                bld.Strip(p, q, gp, gq, BorderHeight, n, Kind(islandColor[minus], KindBorder), along, minus + 1);   // seen from the + side: the land beyond
                bld.Strip(p, q, gp, gq, BorderHeight, -n, Kind(islandColor[plus], KindBorder), along, plus + 1);    // seen from the - side
                labels.Add(p, q, gp, n, data.layout.islands[minus].name, islandColor[minus], data.layout.islands[plus].name, islandColor[plus]);
                segments++;
            }
            foreach (var kv in tiles) kv.Value.Build($"Border {kv.Key.Item1},{kv.Key.Item2}", root, mat);
            Debug.Log($"Zone fields: {segments} border pieces");
        }

        // ---------- a curtain across each end of every bridge ----------
        static void Gates(IslandWorldData data, Transform root, Material mat, Color[] islandColor, ZoneLabels labels)
        {
            var index = new Dictionary<int, int>();
            for (var i = 0; i < data.layout.islands.Length; i++) index[data.layout.islands[i].sceneId] = i;
            var bld = new Builder();
            foreach (var b in data.layout.bridges)
            {
                if (!index.TryGetValue(b.a, out var ia) || !index.TryGetValue(b.b, out var ib)) continue;
                var A = new Vector3(b.ax, 0, b.az); var B = new Vector3(b.bx, 0, b.bz);
                var along = (B - A).normalized;
                var half = (b.causeway ? 6f : 3.5f) / 2f + .4f;
                foreach (var (end, inward, here, there) in new[] { (A, along, ia, ib), (B, -along, ib, ia) })
                {
                    var c = end + inward * 4f;                        // a few metres out onto the bridge
                    var side = Vector3.Cross(Vector3.up, inward) * half;
                    var g = Mathf.Max(data.HeightAt(end.x, end.z), 1f) + .15f;
                    // coming off the bridge you see this land's colour; leaving it you see where the bridge leads
                    bld.Strip(c - side, c + side, g, g, GateHeight, inward, Kind(islandColor[here], KindGate), 0);
                    bld.Strip(c - side, c + side, g, g, GateHeight, -inward, Kind(islandColor[there], KindGate), 0);
                    // seen from the bridge (the inward side) you're entering 'here'; from the land, the bridge to 'there'
                    labels.Add(c - side, c + side, g, -inward, data.layout.islands[here].name, islandColor[here],
                               "Bridge to " + data.layout.islands[there].name, islandColor[there]);
                }
            }
            bld.Build("Bridge gates", root, mat);
        }

        // ---------- rings round artists with their own station ----------
        static void Rings(IslandWorldData data, Transform root, Material mat, Color[] islandColor, System.Func<int, bool> landBuilt, ZoneLabels labels)
        {
            var index = new Dictionary<int, int>();
            for (var i = 0; i < data.layout.islands.Length; i++) index[data.layout.islands[i].sceneId] = i;
            var bld = new Builder();
            var rings = 0;
            foreach (var a in data.layout.artists ?? new IslandWorldData.Artist[0])
            {
                if (a.stationId == 0 || !landBuilt(a.sceneId) || !index.TryGetValue(a.sceneId, out var land)) continue;
                var center = new Vector3(a.x, 0, a.z);
                var n = Mathf.Max(20, Mathf.CeilToInt(2f * Mathf.PI * a.r / 4f));
                for (var k = 0; k < n; k++)
                {
                    float t0 = k * Mathf.PI * 2f / n, t1 = (k + 1) * Mathf.PI * 2f / n;
                    var p = center + new Vector3(Mathf.Cos(t0), 0, Mathf.Sin(t0)) * a.r;
                    var q = center + new Vector3(Mathf.Cos(t1), 0, Mathf.Sin(t1)) * a.r;
                    var outward = ((p + q) / 2f - center).normalized;
                    float gp = data.HeightAt(p.x, p.z), gq = data.HeightAt(q.x, q.z);
                    var along = a.r * t0;
                    bld.Strip(p, q, gp, gq, RingHeight, outward, Kind(ArtistGold, KindRing), along);       // from outside: the artist's station
                    bld.Strip(p, q, gp, gq, RingHeight, -outward, Kind(islandColor[land], KindRing), along); // from inside: back to the land
                    labels.Add(p, q, gp, outward, "\u266a " + a.name, ArtistGold, data.layout.islands[land].name, islandColor[land]);
                }
                rings++;
            }
            bld.Build("Artist radio rings", root, mat);
            Debug.Log($"Zone fields: {rings} artist radio rings");
        }

        /// A land's curtain colour: the most vivid colour in its look-brief palette.
        public static Color VividColor(string[] palette, Color fallback)
        {
            var best = fallback; var bestScore = -1f;
            foreach (var p in palette ?? new string[0])
            {
                if (!ColorUtility.TryParseHtmlString(p.Split(' ')[0], out var c)) continue;
                Color.RGBToHSV(c, out _, out var s, out var v);
                if (v < .35f) continue;
                var score = s * v;
                if (score > bestScore) { bestScore = score; best = c; }
            }
            if (bestScore < 0) return fallback;
            Color.RGBToHSV(best, out var h2, out var s2, out var v2);
            return Color.HSVToRGB(h2, Mathf.Max(s2, .7f), Mathf.Max(v2, .9f)); // bold and bright enough to read at dusk
        }
    }
}
