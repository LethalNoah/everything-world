using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace EverythingWorld.World
{
    /// A check of each hand-built land, run in the editor right after the land is built (IslandWorldBuilder,
    /// "Audit Lands"). It looks for the problems we kept fixing by hand:
    /// - things you'd walk into that have no collider (solid-looking geometry between knee and head height);
    /// - artists too close to the border with another land, standing in water, crowding each other, or sitting
    ///   inside another artist's radio ring; how far each is from where you arrive;
    /// - empty stretches of the land with nothing built on them;
    /// - how much light there is.
    /// It writes a report and a top-down picture for each land to Logs/LandAudit/ in the Unity project (not
    /// committed), and a one-line summary to the console. The picture: dark blue water, grey other lands, this
    /// land shaded by height, scenery as dark specks, yellow cells for empty stretches, orange dots for lights,
    /// artist rings (gold for radio stations, white otherwise, red when something's wrong), a cyan cross where
    /// you arrive, and red squares where something has no collider.
    public static class LandAudit
    {
        const float BandLow = .45f, BandHigh = 2f, Probe = .3f, Px = 2f;

        class Cluster { public Vector3 sum; public int points; public float minY = float.MaxValue, maxY = float.MinValue; public string what; public Vector3 At => sum / points; }

        public static void Run(IslandWorldData data, LandContext ctx, string land)
        {
            Physics.SyncTransforms();
            var sb = new StringBuilder();
            var plaza = ctx.Plaza;
            string Where(Vector3 p)
            {
                var d = new Vector2(p.x - plaza.x, p.z - plaza.z);
                string[] dirs = { "E", "NE", "N", "NW", "W", "SW", "S", "SE" };
                var ang = Mathf.Repeat(Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg + 22.5f, 360f);
                return $"{d.magnitude:0} m {dirs[(int)(ang / 45f) % 8]} of arrival (x {p.x:0}, z {p.z:0}; picture {Pix(ctx, p).x}, {Pix(ctx, p).y})";
            }
            bool ThisLand(float x, float z) => data.IslandAt(x, z) == ctx.Island && data.HeightAt(x, z) > 1.5f;

            // ---------------------------------------------------------------- sample the built geometry
            var solidCells = new HashSet<(int, int, int)>();
            var scenery = new HashSet<(int, int)>();       // 10 m cells with something built in them
            var bare = new List<(Vector3 p, string what)>();
            int lights = 0, glows = 0;
            foreach (var l in ctx.Root.GetComponentsInChildren<Light>()) if (l.enabled) lights++;
            var lightAt = new List<Vector3>();
            foreach (var l in ctx.Root.GetComponentsInChildren<Light>()) if (l.enabled && l.type != LightType.Directional) lightAt.Add(l.transform.position);
            foreach (var mf in ctx.Root.GetComponentsInChildren<MeshFilter>())
            {
                if (mf.name.StartsWith("Soft")) continue;                                      // leaves and blossom, walked through on purpose
                var rend = mf.GetComponent<MeshRenderer>(); var mesh = mf.sharedMesh;
                if (rend == null || !rend.enabled || mesh == null || !mesh.isReadable) continue;
                var mats = rend.sharedMaterials;
                var skip = false;
                foreach (var m in mats)
                {
                    if (m == null) continue;
                    if (m.shader != null && m.shader.name.Contains("Wind")) skip = true;           // swaying grass and flowers
                    if (m.IsKeywordEnabled("_EMISSION")) glows++;
                }
                if (skip) continue;
                var solidMat = new bool[mesh.subMeshCount];
                for (var s = 0; s < mesh.subMeshCount; s++)
                {
                    var m = s < mats.Length ? mats[s] : null;
                    solidMat[s] = m != null && m.renderQueue < 3000 && (m.shader == null || !m.shader.name.Contains("Particles"));
                }
                var what = mf.transform.parent != null ? mf.transform.parent.name + " / " + mf.name : mf.name;
                var verts = mesh.vertices; var tf = mf.transform;
                for (var s = 0; s < mesh.subMeshCount; s++)
                {
                    if (!solidMat[s]) continue;
                    var tris = mesh.GetTriangles(s);
                    for (var i = 0; i < tris.Length; i += 3)
                    {
                        Vector3 v0 = tf.TransformPoint(verts[tris[i]]), v1 = tf.TransformPoint(verts[tris[i + 1]]), v2 = tf.TransformPoint(verts[tris[i + 2]]);
                        var p = (v0 + v1 + v2) / 3f;
                        var flat = Mathf.Abs(Vector3.Cross(v1 - v0, v2 - v0).normalized.y) > .9f;   // water, pads, rugs, decks: nothing to walk into
                        var g = data.HeightAt(p.x, p.z);
                        var h = p.y - g;
                        if (h > .3f) scenery.Add((Mathf.FloorToInt(p.x / 10f), Mathf.FloorToInt(p.z / 10f)));
                        if (flat || h < BandLow || h > BandHigh || g < 1.2f) continue;
                        var key = (Mathf.FloorToInt(p.x / .5f), Mathf.FloorToInt(p.y / .5f), Mathf.FloorToInt(p.z / .5f));
                        if (!solidCells.Add(key)) continue;
                        if (!Covered(p)) bare.Add((p, what));
                    }
                }
            }

            // group the bare points into things
            var cells = new Dictionary<(int, int), List<(Vector3 p, string what)>>();
            foreach (var b in bare)
            {
                var k = (Mathf.FloorToInt(b.p.x), Mathf.FloorToInt(b.p.z));
                if (!cells.TryGetValue(k, out var l)) cells[k] = l = new List<(Vector3, string)>();
                l.Add(b);
            }
            var seen = new HashSet<(int, int)>(); var clusters = new List<Cluster>();
            foreach (var start in cells.Keys)
            {
                if (seen.Contains(start)) continue;
                var c = new Cluster(); var q = new Queue<(int, int)>(); q.Enqueue(start); seen.Add(start);
                var names = new Dictionary<string, int>();
                while (q.Count > 0)
                {
                    var k = q.Dequeue();
                    foreach (var b in cells[k])
                    {
                        c.sum += b.p; c.points++; c.minY = Mathf.Min(c.minY, b.p.y); c.maxY = Mathf.Max(c.maxY, b.p.y);
                        names[b.what] = names.TryGetValue(b.what, out var n) ? n + 1 : 1;
                    }
                    for (var i = -1; i <= 1; i++) for (var j = -1; j <= 1; j++)
                    {
                        var nk = (k.Item1 + i, k.Item2 + j);
                        if (cells.ContainsKey(nk) && seen.Add(nk)) q.Enqueue(nk);
                    }
                }
                var best = 0; foreach (var kv in names) if (kv.Value > best) { best = kv.Value; c.what = kv.Key; }
                if (c.points >= 3) clusters.Add(c);
            }
            clusters.Sort((a, b) => b.points.CompareTo(a.points));

            // ---------------------------------------------------------------- the artists
            var artists = ctx.Artists;
            var problems = new HashSet<string>();
            var artistLines = new List<string>();
            float farthest = 0f;
            foreach (var a in artists)
            {
                var p = new Vector3(a.x, data.HeightAt(a.x, a.z), a.z);
                var notes = new List<string>();
                var border = BorderDistance(data, ctx, a.x, a.z);
                if (border < 45f) notes.Add($"only {border:0} m from another land");
                if (data.HeightAt(a.x, a.z) < 1.5f) notes.Add("standing in water or on the wet beach");
                foreach (var o in artists)
                {
                    if (o == a) continue;
                    var d = Vector2.Distance(new Vector2(a.x, a.z), new Vector2(o.x, o.z));
                    if (d < 18f) notes.Add($"crowding {o.name} ({d:0} m apart)");
                    if (o.stationId != 0 && d < o.r + 3f) notes.Add($"inside {o.name}'s radio ring");
                }
                var fromPlaza = Vector2.Distance(new Vector2(a.x, a.z), new Vector2(plaza.x, plaza.z));
                farthest = Mathf.Max(farthest, fromPlaza);
                if (notes.Count > 0) problems.Add(a.name);
                artistLines.Add($"  {a.name}{(a.stationId != 0 ? " (radio)" : "")}: {Where(p)}{(notes.Count > 0 ? "  <-- " + string.Join("; ", notes) : "")}");
            }

            // ---------------------------------------------------------------- empty stretches
            const float cell = 20f;
            var empty = new HashSet<(int, int)>(); var landCells = 0;
            for (var x = ctx.MinX; x <= ctx.MaxX; x += cell)
            for (var z = ctx.MinZ; z <= ctx.MaxZ; z += cell)
            {
                float cx = x + cell / 2f, cz = z + cell / 2f;
                if (!ThisLand(cx, cz)) continue;
                landCells++;
                var any = false;
                int i0 = Mathf.FloorToInt((cx - 25f) / 10f), i1 = Mathf.FloorToInt((cx + 25f) / 10f), j0 = Mathf.FloorToInt((cz - 25f) / 10f), j1 = Mathf.FloorToInt((cz + 25f) / 10f);
                for (var i = i0; i <= i1 && !any; i++) for (var j = j0; j <= j1 && !any; j++) any = scenery.Contains((i, j));
                if (!any) empty.Add((Mathf.FloorToInt(x / cell), Mathf.FloorToInt(z / cell)));
            }
            var patches = new List<(Vector3 at, float area)>(); var seenE = new HashSet<(int, int)>();
            foreach (var s in empty)
            {
                if (seenE.Contains(s)) continue;
                var q = new Queue<(int, int)>(); q.Enqueue(s); seenE.Add(s);
                var sum = Vector2.zero; var n = 0;
                while (q.Count > 0)
                {
                    var k = q.Dequeue(); sum += new Vector2((k.Item1 + .5f) * cell, (k.Item2 + .5f) * cell); n++;
                    foreach (var nk in new[] { (k.Item1 + 1, k.Item2), (k.Item1 - 1, k.Item2), (k.Item1, k.Item2 + 1), (k.Item1, k.Item2 - 1) })
                        if (empty.Contains(nk) && seenE.Add(nk)) q.Enqueue(nk);
                }
                if (n >= 6) { var c = sum / n; patches.Add((new Vector3(c.x, data.HeightAt(c.x, c.y), c.y), n * cell * cell)); }
            }
            patches.Sort((a, b) => b.area.CompareTo(a.area));

            // ---------------------------------------------------------------- the report
            var km2 = landCells * cell * cell / 1e6f;
            var emptyPct = landCells > 0 ? 100f * empty.Count / landCells : 0f;
            sb.AppendLine($"Land audit: {land}   ({System.DateTime.Now:yyyy-MM-dd HH:mm})");
            sb.AppendLine($"Land about {km2:0.00} km2, {artists.Count} artists, the farthest {farthest:0} m from where you arrive.");
            sb.AppendLine();
            sb.AppendLine($"NO COLLIDER: {clusters.Count} things between knee and head height you could walk through" + (clusters.Count > 0 ? " (biggest first):" : "."));
            for (var i = 0; i < clusters.Count && i < 25; i++)
            {
                var c = clusters[i];
                sb.AppendLine($"  {c.what}: {Where(c.At)}, {c.minY - data.HeightAt(c.At.x, c.At.z):0.0}-{c.maxY - data.HeightAt(c.At.x, c.At.z):0.0} m up, {c.points} samples");
            }
            sb.AppendLine();
            sb.AppendLine($"ARTISTS ({problems.Count} with problems):");
            foreach (var l in artistLines) sb.AppendLine(l);
            sb.AppendLine();
            sb.AppendLine($"EMPTY: {emptyPct:0}% of the land has nothing built within 25 m" + (patches.Count > 0 ? "; the biggest empty stretches:" : "."));
            for (var i = 0; i < patches.Count && i < 10; i++) sb.AppendLine($"  {patches[i].area / 1e4f:0.0} ha round {Where(patches[i].at)}");
            sb.AppendLine();
            sb.AppendLine($"LIGHT: {lights} lights and {glows} glowing pieces ({(km2 > 0 ? lights / km2 : 0):0} lights per km2).");

            var dir = Path.Combine(Application.dataPath, "..", "Logs", "LandAudit");
            Directory.CreateDirectory(dir);
            var file = land.Replace("The ", "").Replace(" ", "_").Replace("&", "and");
            File.WriteAllText(Path.Combine(dir, file + ".txt"), sb.ToString());
            Picture(data, ctx, scenery, empty, cell, lightAt, clusters, problems, Path.Combine(dir, file + ".png"));
            try { Photo(ctx, dir, file); } catch (System.Exception e) { Debug.LogWarning("Land audit photo failed: " + e.Message); }
            var summary = $"Land audit, {land}: {clusters.Count} things without a collider, {problems.Count} artists with problems, {emptyPct:0}% empty, {lights} lights. Report: Logs/LandAudit/{file}.txt";
            if (clusters.Count > 0 || problems.Count > 0 || emptyPct > 25f) Debug.LogWarning(summary); else Debug.Log(summary);
        }

        /// A photo of the whole land from straight above (north up, no fog), plus where every artist is, for
        /// build/land_map.py to label: Logs/LandAudit/<Land>_top.png and <Land>_places.json.
        static void Photo(LandContext ctx, string dir, string file)
        {
            float w = ctx.MaxX - ctx.MinX + 80f, h = ctx.MaxZ - ctx.MinZ + 80f;
            int pw = 2400, ph = Mathf.RoundToInt(2400 * h / w);
            if (ph > 2400) { ph = 2400; pw = Mathf.RoundToInt(2400 * w / h); }
            float cx = (ctx.MinX + ctx.MaxX) / 2f, cz = (ctx.MinZ + ctx.MaxZ) / 2f;
            var go = new GameObject("Land audit camera");
            var cam = go.AddComponent<Camera>();
            cam.enabled = false; cam.orthographic = true; cam.orthographicSize = h / 2f; cam.aspect = w / h;
            cam.transform.SetPositionAndRotation(new Vector3(cx, 700f, cz), Quaternion.Euler(90f, 0f, 0f));
            cam.nearClipPlane = 1f; cam.farClipPlane = 1500f;
            cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(.09f, .13f, .22f);
            var rt = new RenderTexture(pw, ph, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
            cam.targetTexture = rt;
            var fog = RenderSettings.fog; RenderSettings.fog = false;
            try { cam.Render(); }
            finally { RenderSettings.fog = fog; }
            var prev = RenderTexture.active; RenderTexture.active = rt;
            var tex = new Texture2D(pw, ph, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, pw, ph), 0, 0); tex.Apply();
            RenderTexture.active = prev;
            File.WriteAllBytes(Path.Combine(dir, file + "_top.png"), tex.EncodeToPNG());
            cam.targetTexture = null; Object.Destroy(rt); Object.Destroy(tex); Object.Destroy(go);

            var sb = new StringBuilder();
            sb.Append("{\"land\":\"").Append(ctx.Island.name).Append("\",");
            sb.Append($"\"left\":{cx - w / 2f:0.0},\"top\":{cz + h / 2f:0.0},\"metresPerPixel\":{w / pw:0.0000},");
            sb.Append($"\"arrival\":[{ctx.Plaza.x:0.0},{ctx.Plaza.z:0.0}],\"artists\":[");
            for (var i = 0; i < ctx.Artists.Count; i++)
            {
                var a = ctx.Artists[i];
                if (i > 0) sb.Append(',');
                sb.Append("{\"name\":\"").Append(a.name.Replace("\\", "\\\\").Replace("\"", "\\\"")).Append("\",");
                sb.Append($"\"x\":{a.x:0.0},\"z\":{a.z:0.0},\"r\":{a.r:0.0},\"songs\":{a.songs},\"radio\":{(a.stationId != 0 ? "true" : "false")}}}");
            }
            sb.Append("]}");
            File.WriteAllText(Path.Combine(dir, file + "_places.json"), sb.ToString());
        }

        static bool Covered(Vector3 p)
        {
            foreach (var c in Physics.OverlapSphere(p, Probe, ~0, QueryTriggerInteraction.Ignore))
                if (!c.name.StartsWith("Terrain")) return true;
            return false;
        }

        static float BorderDistance(IslandWorldData data, LandContext ctx, float x, float z)
        {
            for (var r = 5f; r <= 150f; r += 5f)
                for (var k = 0; k < 32; k++)
                {
                    var a = k * Mathf.PI / 16f; float qx = x + Mathf.Cos(a) * r, qz = z + Mathf.Sin(a) * r;
                    var isl = data.IslandAt(qx, qz);
                    if (isl != null && isl != ctx.Island && data.HeightAt(qx, qz) > 1.5f) return r;
                }
            return 999f;
        }

        static Vector2Int Pix(LandContext ctx, Vector3 p) =>
            new Vector2Int(Mathf.RoundToInt((p.x - ctx.MinX + 40f) / Px), Mathf.RoundToInt((ctx.MaxZ + 40f - p.z) / Px));

        static void Picture(IslandWorldData data, LandContext ctx, HashSet<(int, int)> scenery, HashSet<(int, int)> empty, float cell,
                            List<Vector3> lights, List<Cluster> bare, HashSet<string> problems, string path)
        {
            int w = Mathf.CeilToInt((ctx.MaxX - ctx.MinX + 80f) / Px), h = Mathf.CeilToInt((ctx.MaxZ - ctx.MinZ + 80f) / Px);
            if (w <= 0 || h <= 0 || w > 4000 || h > 4000) return;
            var px = new Color32[w * h];
            for (var y = 0; y < h; y++)
            for (var x = 0; x < w; x++)
            {
                float wx = ctx.MinX - 40f + x * Px, wz = ctx.MaxZ + 40f - y * Px;
                var isl = data.IslandAt(wx, wz); var g = data.HeightAt(wx, wz);
                Color c;
                if (isl == null || g < .2f) c = new Color(.12f, .2f, .32f);
                else if (isl != ctx.Island) c = new Color(.42f, .4f, .38f);
                else if (g < 1.5f) c = new Color(.6f, .58f, .5f);
                else c = Color.Lerp(new Color(.45f, .6f, .42f), new Color(.75f, .8f, .65f), Mathf.Clamp01(g / 30f));
                if (isl == ctx.Island && empty.Contains((Mathf.FloorToInt(wx / cell), Mathf.FloorToInt(wz / cell))) && ((x + y) / 3) % 2 == 0) c = Color.Lerp(c, new Color(1f, .85f, .2f), .55f);
                if (scenery.Contains((Mathf.FloorToInt(wx / 10f), Mathf.FloorToInt(wz / 10f))) && (x * 7 + y * 13) % 5 == 0) c *= .55f;
                c.a = 1f;
                px[(h - 1 - y) * w + x] = c;
            }
            void Dot(Vector3 p, int r, Color c, bool fill)
            {
                var q = Pix(ctx, p);
                for (var dy = -r; dy <= r; dy++) for (var dx = -r; dx <= r; dx++)
                {
                    var d2 = dx * dx + dy * dy;
                    if (fill ? d2 > r * r : (d2 > r * r || d2 < (r - 1) * (r - 1))) continue;
                    int x = q.x + dx, y = q.y + dy;
                    if (x >= 0 && y >= 0 && x < w && y < h) px[(h - 1 - y) * w + x] = c;
                }
            }
            foreach (var l in lights) Dot(l, 1, new Color(1f, .6f, .1f), true);
            foreach (var a in ctx.Artists)
            {
                var col = problems.Contains(a.name) ? new Color(1f, .1f, .1f) : a.stationId != 0 ? new Color(1f, .8f, .2f) : Color.white;
                Dot(new Vector3(a.x, 0, a.z), Mathf.Max(3, Mathf.RoundToInt(a.r / Px)), col, false);
                Dot(new Vector3(a.x, 0, a.z), 1, col, true);
            }
            var pz = Pix(ctx, ctx.Plaza);
            for (var k = -8; k <= 8; k++)
                foreach (var (x, y) in new[] { (pz.x + k, pz.y), (pz.x, pz.y + k) })
                    if (x >= 0 && y >= 0 && x < w && y < h) px[(h - 1 - y) * w + x] = new Color(.2f, 1f, 1f);
            foreach (var c in bare) Dot(c.At, 2, new Color(1f, 0f, 0f), true);
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.SetPixels32(px); tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.Destroy(tex);
        }
    }
}
