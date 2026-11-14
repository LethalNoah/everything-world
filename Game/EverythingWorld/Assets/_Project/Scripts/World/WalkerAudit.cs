using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace EverythingWorld.World
{
    /// The walker's eye: the part of the land audit that looks at a land the way someone walking round it does,
    /// not from the tidy spots the photo tour already uses. It reads what was built (no land kit needs to tell it
    /// anything), on a half-metre grid:
    /// - the paths: flat built surfaces lying on the ground in long, narrow strips (plazas, floors and rooms aren't
    ///   paths);
    /// - something standing on a path: anything there at body height (a solid collider the size of the player, or
    ///   built geometry you'd walk through). Where it splits a path in two, the path is BLOCKED; a big thing you
    ///   can still get round is ON A PATH;
    /// - a path too steep to walk or with a ledge in it;
    /// - FLOATING buildings: walls whose bottom edge hangs in the air with nothing under it (a house on a hillside
    ///   built on level ground that isn't there).
    /// Each of the worst gets a mark for the photo tour, at eye height a few metres off, looking at it
    /// ("walker 3: blocked path"), and a handful of plain views along the paths ("walker view 2"), so Claude sees
    /// what a player walking there sees. A picture of it all: Logs/LandAudit/<Land>_walk.png.
    public static class WalkerAudit
    {
        const float C = .5f;                                // grid cell, metres
        const float WalkLow = -.3f, WalkHigh = .45f;         // a flat surface this close to the ground is walked on
        const float BodyLow = .5f, BodyHigh = 1.7f;           // where a body would bump into things
        const float PathFill = .6f;                           // a strip: under this much of the 9.5 m round it is walkable
        const float Steep = .42f;                             // height change over one cell (.5 m) that's a ledge or too steep
        const float HangMin = .5f, HangFly = 6f;               // a wall bottom this far up with nothing below hangs; past HangFly it's meant to fly
        const int MaxShots = 12, Views = 8;

        class Problem { public string kind, what; public Vector3 at; public float size, gap; public int shot; }

        /// Adds the walker's section to the land's report and returns a short summary for the console.
        public static string Run(IslandWorldData data, LandContext ctx, string land, StringBuilder sb, System.Func<Vector3, string> where, string pictureFile)
        {
            float x0 = ctx.MinX - 10f, z0 = ctx.MinZ - 10f;
            int nx = Mathf.CeilToInt((ctx.MaxX - ctx.MinX + 20f) / C), nz = Mathf.CeilToInt((ctx.MaxZ - ctx.MinZ + 20f) / C);
            if (nx <= 0 || nz <= 0 || (long)nx * nz > 12_000_000) return "walker's eye skipped (land too big)";
            var N = nx * nz;
            int Cell(float x, float z) { int i = Mathf.FloorToInt((x - x0) / C), j = Mathf.FloorToInt((z - z0) / C); return i < 0 || j < 0 || i >= nx || j >= nz ? -1 : i + j * nx; }
            Vector3 Centre(int k) => new Vector3(x0 + (k % nx + .5f) * C, 0f, z0 + (k / nx + .5f) * C);

            // ---------------------------------------------------------------- the ground, per cell
            var gnd = new float[N]; var wet = new float[N]; var mine = new bool[N]; var indoors = new bool[N];
            for (var k = 0; k < N; k++)
            {
                var c = Centre(k);
                gnd[k] = data.HeightAt(c.x, c.z); wet[k] = LandAudit.WaterAt(land, c.x, c.z);
                mine[k] = gnd[k] > 1.2f && data.IslandAt(c.x, c.z) == ctx.Island;
            }
            foreach (var r in IndoorSpaces.All)
            {
                if (data.IslandAt(r.center.x, r.center.z) != ctx.Island) continue;
                var reach = new Vector2(r.size.x, r.size.z).magnitude / 2f + 1f; var inv = Quaternion.Inverse(r.rotation);
                for (var z = r.center.z - reach; z <= r.center.z + reach; z += C)
                for (var x = r.center.x - reach; x <= r.center.x + reach; x += C)
                {
                    var k = Cell(x, z); if (k < 0) continue;
                    var l = inv * (Centre(k) - new Vector3(r.center.x, 0f, r.center.z));
                    if (Mathf.Abs(l.x) <= r.size.x / 2f + .5f && Mathf.Abs(l.z) <= r.size.z / 2f + .5f) indoors[k] = true;
                }
            }

            // ---------------------------------------------------------------- read what was built
            var surf = new float[N]; var low = new float[N]; var body = new int[N]; var roof = new bool[N];   // roof: something built 2.2-9 m over the ground (a roof, a canopy)
            for (var k = 0; k < N; k++) { surf[k] = float.NegativeInfinity; low[k] = float.PositiveInfinity; body[k] = -1; }
            var names = new List<string>(); var nameIx = new Dictionary<string, int>();
            int Name(string s) { if (!nameIx.TryGetValue(s, out var i)) { i = names.Count; names.Add(s); nameIx[s] = i; } return i; }
            var hang = new List<(Vector3 p, int what)>();
            foreach (var mf in ctx.Root.GetComponentsInChildren<MeshFilter>())
            {
                if (mf.name.StartsWith("Soft")) continue;                                       // leaves, walked through on purpose
                var rend = mf.GetComponent<MeshRenderer>(); var mesh = mf.sharedMesh;
                if (rend == null || !rend.enabled || mesh == null || !mesh.isReadable) continue;
                var mats = rend.sharedMaterials; var skip = false;
                foreach (var m in mats) if (m != null && m.shader != null && m.shader.name.Contains("Wind")) skip = true;   // swaying grass
                if (skip) continue;
                var verts = mesh.vertices; var tf = mf.transform;
                var objName = mf.transform.parent != null ? mf.transform.parent.name + " / " + mf.name : mf.name;
                for (var s = 0; s < mesh.subMeshCount; s++)
                {
                    var mat = s < mats.Length ? mats[s] : null;
                    if (mat == null || mat.renderQueue >= 3000 || (mat.shader != null && mat.shader.name.Contains("Particles"))) continue;   // glass, water, glows
                    var what = Name(objName + " (" + mat.name.Replace(" (Instance)", "") + ")");
                    var glowing = mat.IsKeywordEnabled("_EMISSION") && mat.HasProperty("_EmissionColor") && mat.GetColor("_EmissionColor").maxColorComponent > .15f;   // lava, neon streams, glowing cracks: never a path (a faint sheen on steel still can be); neon signs never a floating wall
                    var tris = mesh.GetTriangles(s);
                    for (var t = 0; t < tris.Length; t += 3)
                    {
                        Vector3 a = tf.TransformPoint(verts[tris[t]]), b = tf.TransformPoint(verts[tris[t + 1]]), c = tf.TransformPoint(verts[tris[t + 2]]);
                        var n = Vector3.Cross(b - a, c - a); if (n.sqrMagnitude < 1e-8f) continue; n.Normalize();
                        if (Mathf.Abs(n.y) > .9f) Flat(a, b, c, glowing || n.y < 0f);   // a face looking down (the bottom of a box on the ground) is never walked on
                        else Sloped(a, b, c, n, what, glowing);
                    }
                }
            }

            // a flat triangle: lying on the ground it is somewhere you walk; anywhere, it is something under a wall
            void Flat(Vector3 a, Vector3 b, Vector3 c, bool glowing)
            {
                int i0 = Mathf.FloorToInt((Mathf.Min(a.x, Mathf.Min(b.x, c.x)) - x0) / C), i1 = Mathf.FloorToInt((Mathf.Max(a.x, Mathf.Max(b.x, c.x)) - x0) / C);
                int j0 = Mathf.FloorToInt((Mathf.Min(a.z, Mathf.Min(b.z, c.z)) - z0) / C), j1 = Mathf.FloorToInt((Mathf.Max(a.z, Mathf.Max(b.z, c.z)) - z0) / C);
                i0 = Mathf.Max(i0, 0); j0 = Mathf.Max(j0, 0); i1 = Mathf.Min(i1, nx - 1); j1 = Mathf.Min(j1, nz - 1);
                var den = (b.z - c.z) * (a.x - c.x) + (c.x - b.x) * (a.z - c.z); if (Mathf.Abs(den) < 1e-8f) return;
                var hit = false;
                for (var j = j0; j <= j1; j++)
                for (var i = i0; i <= i1; i++)
                {
                    float px = x0 + (i + .5f) * C, pz = z0 + (j + .5f) * C;
                    var u = ((b.z - c.z) * (px - c.x) + (c.x - b.x) * (pz - c.z)) / den;
                    var v = ((c.z - a.z) * (px - c.x) + (a.x - c.x) * (pz - c.z)) / den;
                    if (u < -.001f || v < -.001f || u + v > 1.001f) continue;
                    hit = true; Mark(i + j * nx, u * a.y + v * b.y + (1f - u - v) * c.y, glowing);
                }
                if (!hit) { var m = (a + b + c) / 3f; var k = Cell(m.x, m.z); if (k >= 0) Mark(k, m.y, glowing); }   // smaller than a cell
            }
            void Mark(int k, float y, bool glowing)
            {
                var g = Mathf.Max(gnd[k], wet[k]);
                low[k] = Mathf.Min(low[k], y - g); if (y - gnd[k] > 2.2f && y - gnd[k] < 9f) roof[k] = true;
                var h = y - gnd[k];
                if (!glowing && h >= WalkLow && h <= WalkHigh && y > wet[k] + .05f) surf[k] = Mathf.Max(surf[k], y);
            }
            // anything else: body height marks the cell; the bottom edge of a tall wall is checked for hanging
            void Sloped(Vector3 a, Vector3 b, Vector3 c, Vector3 n, int what, bool glowing)
            {
                var longest = Mathf.Max((b - a).magnitude, Mathf.Max((c - b).magnitude, (a - c).magnitude));
                var m = Mathf.Clamp(Mathf.CeilToInt(longest / .35f), 1, 80);
                for (var u = 0; u <= m; u++)
                for (var v = 0; v <= m - u; v++)
                {
                    var p = a + (b - a) * ((float)u / m) + (c - a) * ((float)v / m);
                    var k = Cell(p.x, p.z); if (k < 0) continue;
                    low[k] = Mathf.Min(low[k], p.y - Mathf.Max(gnd[k], wet[k])); { var hr = p.y - gnd[k]; if (hr > 2.2f && hr < 9f) roof[k] = true; }
                    var h = p.y - gnd[k];
                    if (h >= BodyLow && h <= BodyHigh) body[k] = what;
                }
                if (Mathf.Abs(n.y) > .1f || glowing) return;         // only upright walls (not neon signs)
                Vector3 p0 = a, p1 = b, p2 = c;                       // sort by height: p0, p1 the bottom edge
                if (p1.y < p0.y) (p0, p1) = (p1, p0); if (p2.y < p0.y) (p0, p2) = (p2, p0); if (p2.y < p1.y) (p1, p2) = (p2, p1);
                if (p1.y - p0.y > .05f || p2.y - p0.y < 1.2f) return;  // a level bottom edge under a wall at least 1.2 m tall
                var len = (p1 - p0).magnitude; var steps = Mathf.Max(1, Mathf.RoundToInt(len / .25f));
                for (var q = 0; q <= steps; q++) hang.Add((Vector3.Lerp(p0, p1, (float)q / steps), what));
            }

            // ---------------------------------------------------------------- which walkable cells are paths
            var walk = new bool[N];
            for (var k = 0; k < N; k++) walk[k] = mine[k] && !indoors[k] && !float.IsNegativeInfinity(surf[k]);
            var walkComp = Components(walk, nx, nz, true, out var walkSize);
            var sat = new int[(nx + 1) * (nz + 1)];                // summed area of the walkable cells
            for (var j = 0; j < nz; j++) { var row = 0; for (var i = 0; i < nx; i++) { row += walk[i + j * nx] ? 1 : 0; sat[(i + 1) + (j + 1) * (nx + 1)] = sat[(i + 1) + j * (nx + 1)] + row; } }
            const int R = 9;
            float Fill(int k)
            {
                int i = k % nx, j = k / nx, a0 = Mathf.Max(0, i - R), b0 = Mathf.Max(0, j - R), a1 = Mathf.Min(nx, i + R + 1), b1 = Mathf.Min(nz, j + R + 1);
                var s = sat[a1 + b1 * (nx + 1)] - sat[a0 + b1 * (nx + 1)] - sat[a1 + b0 * (nx + 1)] + sat[a0 + b0 * (nx + 1)];
                return s / (float)((2 * R + 1) * (2 * R + 1));
            }
            var path = new bool[N]; var fill = new float[N]; int pathCells = 0, walkCells = 0;
            for (var k = 0; k < N; k++) if (walk[k]) fill[k] = Fill(k);
            // near a plaza or a round pad (somewhere mostly walkable), its rim isn't a path either
            var wide = new int[(nx + 1) * (nz + 1)];
            for (var j = 0; j < nz; j++) { var row = 0; for (var i = 0; i < nx; i++) { row += walk[i + j * nx] && fill[i + j * nx] >= .72f ? 1 : 0; wide[(i + 1) + (j + 1) * (nx + 1)] = wide[(i + 1) + j * (nx + 1)] + row; } }
            bool NearWide(int k)
            {
                const int W = 6; int i = k % nx, j = k / nx, a0 = Mathf.Max(0, i - W), b0 = Mathf.Max(0, j - W), a1 = Mathf.Min(nx, i + W + 1), b1 = Mathf.Min(nz, j + W + 1);
                return wide[a1 + b1 * (nx + 1)] - wide[a0 + b1 * (nx + 1)] - wide[a1 + b0 * (nx + 1)] + wide[a0 + b0 * (nx + 1)] > 0;
            }
            for (var k = 0; k < N; k++)
            {
                if (!walk[k]) continue; walkCells++;
                // a strip, more than a rug, not the rim of a plaza or pad, and not under a roof (a house's floor or foundation)
                path[k] = fill[k] < PathFill && walkSize[walkComp[k]] >= 60 && !NearWide(k) && !roof[k];
                if (path[k]) pathCells++;
            }

            // ---------------------------------------------------------------- what stands on the paths
            var blocked = new bool[N]; var blockWhat = new int[N]; var buf = new Collider[16];
            for (var k = 0; k < N; k++)
            {
                if (!path[k]) continue;
                { int ei = k % nx, ej = k / nx;                       // the outermost strip of a path: a rail or a kerb along its edge is part of it
                  if (ei == 0 || ej == 0 || ei == nx - 1 || ej == nz - 1 || !walk[k - 1] || !walk[k + 1] || !walk[k - nx] || !walk[k + nx]) continue; }
                blockWhat[k] = body[k];
                if (body[k] >= 0) { blocked[k] = true; continue; }
                var c = Centre(k); const float r = .25f;
                var cnt = Physics.OverlapCapsuleNonAlloc(new Vector3(c.x, surf[k] + BodyLow + r, c.z), new Vector3(c.x, surf[k] + BodyHigh - r, c.z), r, buf, ~0, QueryTriggerInteraction.Ignore);
                for (var q = 0; q < cnt; q++)
                {
                    var col = buf[q];
                    if (col.name.StartsWith("Terrain") || col is CharacterController || col.GetComponentInParent<Resident>() != null) continue;
                    blocked[k] = true; blockWhat[k] = Name(col.transform.parent != null ? col.transform.parent.name + " / " + col.name : col.name); break;
                }
            }
            var free = new bool[N]; for (var k = 0; k < N; k++) free[k] = path[k] && !blocked[k];
            var freeComp = Components(free, nx, nz, false, out var freeSize);
            var problems = new List<Problem>();
            var blockComp = Components(blocked, nx, nz, true, out var blockSize);
            var extent = new Dictionary<int, Vector4>();               // each free piece of path: its extent (min x, min z, max x, max z)
            for (var k = 0; k < N; k++)
            {
                if (!free[k]) continue; var c = Centre(k); var id = freeComp[k];
                extent[id] = extent.TryGetValue(id, out var b) ? new Vector4(Mathf.Min(b.x, c.x), Mathf.Min(b.y, c.z), Mathf.Max(b.z, c.x), Mathf.Max(b.w, c.z)) : new Vector4(c.x, c.z, c.x, c.z);
            }
            bool GoesOn(int id) { var b = extent[id]; return new Vector2(b.z - b.x, b.w - b.y).magnitude >= 14f; }
            var groups = new Dictionary<int, List<int>>();
            for (var k = 0; k < N; k++) if (blocked[k]) { if (!groups.TryGetValue(blockComp[k], out var l)) groups[blockComp[k]] = l = new List<int>(); l.Add(k); }
            foreach (var g in groups.Values)
            {
                var sides = new HashSet<int>(); var sum = Vector3.zero; var whats = new Dictionary<int, int>();
                foreach (var k in g)
                {
                    sum += Centre(k) + Vector3.up * surf[k];
                    if (blockWhat[k] >= 0) whats[blockWhat[k]] = whats.TryGetValue(blockWhat[k], out var w) ? w + 1 : 1;
                    int i = k % nx, j = k / nx;
                    for (var dj = -2; dj <= 2; dj++) for (var di = -2; di <= 2; di++)
                    {
                        int ii = i + di, jj = j + dj; if (ii < 0 || jj < 0 || ii >= nx || jj >= nz) continue;
                        var kk = ii + jj * nx; if (free[kk] && freeSize[freeComp[kk]] >= 16 && GoesOn(freeComp[kk])) sides.Add(freeComp[kk]);
                    }
                }
                var area = g.Count * C * C;
                var kind = sides.Count >= 2 ? "blocked path" : area >= 4f ? "on a path" : null;
                if (kind == null) continue;
                problems.Add(new Problem { kind = kind, what = Most(whats, names), at = sum / g.Count, size = area });
            }

            // ---------------------------------------------------------------- too steep, or a ledge, along a path
            var steep = new bool[N];
            for (var k = 0; k < N; k++)
            {
                if (!free[k]) continue; int i = k % nx, j = k / nx;
                if (i + 1 < nx && free[k + 1] && Mathf.Abs(surf[k + 1] - surf[k]) > Steep) steep[k] = steep[k + 1] = true;
                if (j + 1 < nz && free[k + nx] && Mathf.Abs(surf[k + nx] - surf[k]) > Steep) steep[k] = steep[k + nx] = true;
            }
            var steepComp = Components(steep, nx, nz, true, out var steepSize);
            var steepSeen = new Dictionary<int, (Vector3 sum, int n, float dh)>();
            for (var k = 0; k < N; k++)
            {
                if (!steep[k]) continue;
                var id = steepComp[k]; steepSeen.TryGetValue(id, out var e);
                var dh = 0f; int i = k % nx, j = k / nx;
                foreach (var kk in new[] { i + 1 < nx ? k + 1 : -1, i > 0 ? k - 1 : -1, j + 1 < nz ? k + nx : -1, j > 0 ? k - nx : -1 }) if (kk >= 0 && free[kk]) dh = Mathf.Max(dh, Mathf.Abs(surf[kk] - surf[k]));
                steepSeen[id] = (e.sum + Centre(k) + Vector3.up * surf[k], e.n + 1, Mathf.Max(e.dh, dh));
            }
            foreach (var e in steepSeen.Values) if (e.n >= 3) problems.Add(new Problem { kind = "steep or a ledge", what = $"a {e.dh:0.0} m step within half a metre", at = e.sum / e.n, size = e.n * C * C, gap = e.dh });

            // ---------------------------------------------------------------- floating walls
            var hangCells = new Dictionary<(int, int), List<(Vector3 p, int what, float gap)>>(); var flying = 0;
            foreach (var (p, what) in hang)
            {
                var k = Cell(p.x, p.z); if (k < 0 || !mine[k]) continue;
                var gap = p.y - Mathf.Max(gnd[k], wet[k]);
                if (gap < HangMin) continue;                          // on the ground
                var under = false; int hi = k % nx, hj = k / nx;      // something under it, here or in the next cell (a window sits just off its wall)
                for (var dj = -1; dj <= 1 && !under; dj++) for (var di = -1; di <= 1 && !under; di++)
                { int ii = hi + di, jj = hj + dj; if (ii >= 0 && jj >= 0 && ii < nx && jj < nz && low[ii + jj * nx] < gap - .1f) under = true; }
                if (under) continue;
                if (gap > HangFly) { flying++; continue; }
                var key = (Mathf.FloorToInt(p.x), Mathf.FloorToInt(p.z));
                if (!hangCells.TryGetValue(key, out var l)) hangCells[key] = l = new List<(Vector3, int, float)>();
                l.Add((p, what, gap));
            }
            var hangSeen = new HashSet<(int, int)>();
            foreach (var start in hangCells.Keys)
            {
                if (!hangSeen.Add(start)) continue;
                var q = new Queue<(int, int)>(); q.Enqueue(start); var pts = 0; var sum = Vector3.zero; var maxGap = 0f; var minGap = float.MaxValue; var whats = new Dictionary<int, int>();
                while (q.Count > 0)
                {
                    var key = q.Dequeue();
                    foreach (var (p, what, gap) in hangCells[key]) { pts++; sum += p; maxGap = Mathf.Max(maxGap, gap); minGap = Mathf.Min(minGap, gap); whats[what] = whats.TryGetValue(what, out var w) ? w + 1 : 1; }
                    for (var di = -1; di <= 1; di++) for (var dj = -1; dj <= 1; dj++) { var nk = (key.Item1 + di, key.Item2 + dj); if (hangCells.ContainsKey(nk) && hangSeen.Add(nk)) q.Enqueue(nk); }
                }
                var length = pts * .25f;
                if (length >= 2f && minGap > 2f) { flying += pts; continue; }   // all of it well up: a board or a tank raised on posts or legs, not a building off a slope
                if (length >= 2f) problems.Add(new Problem { kind = "floating", what = Most(whats, names), at = sum / pts, size = length, gap = maxGap });
            }

            // ---------------------------------------------------------------- photos: the worst of each, then views along the paths
            float Score(Problem p) => p.kind == "blocked path" ? 1000f + p.size : p.kind == "floating" ? 500f + p.size * p.gap : p.kind == "steep or a ledge" ? 200f + p.size : p.size;
            problems.Sort((a, b) => Score(b).CompareTo(Score(a)));
            var shots = 0; var byKind = new Dictionary<string, int>();
            // a fair share for each kind first (so twenty floating sheds don't crowd out the one blocked path), then the rest by score
            foreach (var pass in new[] { 3, MaxShots })
                foreach (var p in problems)
                {
                    if (shots >= MaxShots) break;
                    if (p.shot > 0) continue;
                    byKind.TryGetValue(p.kind, out var had); if (had >= pass) continue;
                    if (!View(p.at, out var feet, out var look)) continue;
                    shots++; byKind[p.kind] = had + 1; p.shot = shots;
                    Mark2($"walker {shots}: {p.kind}", feet, look);
                }
            bool View(Vector3 target, out Vector3 feet, out Vector3 look)
            {
                var aim = target + Vector3.up * 1.1f;
                for (var pass = 0; pass < 2; pass++)
                    foreach (var dist in new[] { 7f, 10f, 5f, 14f })
                        for (var a = 0; a < 16; a++)
                        {
                            var ang = (a * 22.5f + 11f) * Mathf.Deg2Rad; var q = target + new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang)) * dist;
                            var k = Cell(q.x, q.z); if (k < 0 || !mine[k] || indoors[k] || (!walk[k] && wet[k] > gnd[k] - .1f)) continue;   // not under water
                            if (pass == 0 ? !free[k] : (body[k] >= 0 || (path[k] && blocked[k]))) continue;
                            var stand = walk[k] ? surf[k] : gnd[k];
                            var eye = new Vector3(q.x, stand + 1.6f, q.z); var to = aim - eye;
                            if (Physics.CheckSphere(eye, .3f, ~0, QueryTriggerInteraction.Ignore) || Physics.Raycast(eye, Vector3.up, 6f, ~0, QueryTriggerInteraction.Ignore)) continue;
                            if ((low[k] < 1.9f && low[k] > .3f) || roof[k]) continue;     // something built over where you'd stand
                            if (Physics.Linecast(eye, aim - to.normalized * Mathf.Min(2.5f, to.magnitude * .4f), ~0, QueryTriggerInteraction.Ignore)) continue;
                            feet = new Vector3(q.x, stand + .95f, q.z); look = to; return true;
                        }
                feet = look = Vector3.zero; return false;
            }
            void Mark2(string name, Vector3 feet, Vector3 look)
            {
                look.y = 0f; if (look.sqrMagnitude < .01f) look = Vector3.forward;
                var m = new GameObject("Shot: " + name).transform; m.SetParent(ctx.Root, false);
                m.SetPositionAndRotation(feet, Quaternion.LookRotation(look.normalized));
            }
            // views along the paths, spread out: each the farthest path spot from where you arrive and the views so far
            var picks = new List<Vector3> { ctx.Plaza };
            var cands = new List<int>();
            for (var k = 0; k < N; k += 3) if (free[k] && fill[k] > .15f && fill[k] < .5f && freeSize[freeComp[k]] >= 200) cands.Add(k);
            var views = 0;
            while (views < Views && cands.Count > 0)
            {
                int best = -1; var bd = 0f;
                foreach (var k in cands) { var c = Centre(k); var d = float.MaxValue; foreach (var p in picks) d = Mathf.Min(d, (new Vector2(c.x - p.x, c.z - p.z)).sqrMagnitude); if (d > bd) { bd = d; best = k; } }
                if (best < 0 || bd < 25f * 25f) break;
                var at = Centre(best); picks.Add(at);
                // which way the path runs here (the long axis of the path round it), facing away from where you arrive
                float sxx = 0, szz = 0, sxz = 0; int bi = best % nx, bj = best / nx;
                for (var dj = -6; dj <= 6; dj++) for (var di = -6; di <= 6; di++)
                {
                    int ii = bi + di, jj = bj + dj; if (ii < 0 || jj < 0 || ii >= nx || jj >= nz || !path[ii + jj * nx]) continue;
                    sxx += di * di; szz += dj * dj; sxz += di * dj;
                }
                var ang = .5f * Mathf.Atan2(2f * sxz, sxx - szz); var dir = new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang));
                if (Vector3.Dot(dir, at - new Vector3(ctx.Plaza.x, 0f, ctx.Plaza.z)) < 0f) dir = -dir;
                views++; Mark2($"walker view {views}", new Vector3(at.x, surf[best] + .95f, at.z), dir);
            }

            // ---------------------------------------------------------------- the report
            int nBlocked = 0, nOn = 0, nSteep = 0, nFloat = 0;
            foreach (var p in problems) { if (p.kind == "blocked path") nBlocked++; else if (p.kind == "on a path") nOn++; else if (p.kind == "steep or a ledge") nSteep++; else nFloat++; }
            sb.AppendLine();
            sb.AppendLine("WALKER'S EYE (the land as someone walking round it sees it; photos named \"walker N\" in the tour):");
            sb.AppendLine($"  About {pathCells * C * C:0} m2 of path, {(walkCells - pathCells) * C * C:0} m2 of other built ground (plazas, decks, floors).");
            void Section(string kind, string title, int count, System.Func<Problem, string> line)
            {
                sb.AppendLine($"  {title}: {count}" + (count > 0 ? ":" : "."));
                var shown = 0;
                foreach (var p in problems) { if (p.kind != kind) continue; if (++shown > 25) { sb.AppendLine($"    ... and {count - 25} more"); break; } sb.AppendLine($"    {line(p)}{(p.shot > 0 ? $"  [photo: walker {p.shot}]" : "")}"); }
            }
            Section("blocked path", "BLOCKED PATHS (something splits a path in two)", nBlocked, p => $"{p.what}: {where(p.at)}, about {p.size:0.0} m2 of path covered");
            Section("floating", "FLOATING (a wall's bottom edge in the air with nothing under it)", nFloat, p => $"{p.what}: {where(p.at)}, {p.size:0.0} m of wall up to {p.gap:0.0} m off the ground");
            if (flying > 0) sb.AppendLine($"    (and {flying * .25f:0} m of wall more than {HangFly:0} m up, or raised on posts or legs all along, probably meant to)");
            Section("steep or a ledge", "STEEP OR A LEDGE on a path", nSteep, p => $"{where(p.at)}: {p.what}, {p.size:0.0} m2");
            Section("on a path", "ON A PATH (a big thing standing on it you can get round)", nOn, p => $"{p.what}: {where(p.at)}, about {p.size:0.0} m2");

            Picture(ctx, data, nx, nz, x0, z0, walk, path, blocked, steep, problems, pictureFile);
            return $"walker: {nBlocked} blocked paths, {nFloat} floating walls, {nSteep} steep spots, {nOn} things on paths";
        }

        static string Most(Dictionary<int, int> counts, List<string> names)
        {
            int best = -1, bn = 0; foreach (var kv in counts) if (kv.Value > bn) { bn = kv.Value; best = kv.Key; }
            return best >= 0 ? names[best] : "something";
        }

        /// Connected groups of marked cells (eight neighbours, or four), each cell's group number and each group's size.
        static int[] Components(bool[] on, int nx, int nz, bool eight, out List<int> size)
        {
            var id = new int[on.Length]; for (var k = 0; k < id.Length; k++) id[k] = -1;
            size = new List<int>(); var stack = new Stack<int>();
            for (var s = 0; s < on.Length; s++)
            {
                if (!on[s] || id[s] >= 0) continue;
                var g = size.Count; var n = 0; id[s] = g; stack.Push(s);
                while (stack.Count > 0)
                {
                    var k = stack.Pop(); n++; int i = k % nx, j = k / nx;
                    for (var dj = -1; dj <= 1; dj++) for (var di = -1; di <= 1; di++)
                    {
                        if ((di == 0 && dj == 0) || (!eight && di != 0 && dj != 0)) continue;
                        int ii = i + di, jj = j + dj; if (ii < 0 || jj < 0 || ii >= nx || jj >= nz) continue;
                        var kk = ii + jj * nx; if (on[kk] && id[kk] < 0) { id[kk] = g; stack.Push(kk); }
                    }
                }
                size.Add(n);
            }
            return id;
        }

        /// One pixel a metre, north up: the land in dark green-grey, other built ground dim blue, paths pale,
        /// what stands on them red, steep spots orange, floating walls yellow rings, blocked paths magenta rings,
        /// where you arrive a cyan cross. Each problem with a photo has a ring twice as big.
        static void Picture(LandContext ctx, IslandWorldData data, int nx, int nz, float x0, float z0, bool[] walk, bool[] path, bool[] blocked, bool[] steep, List<Problem> problems, string file)
        {
            int w = Mathf.CeilToInt(nx * C), h = Mathf.CeilToInt(nz * C);
            if (w <= 0 || h <= 0 || w > 4000 || h > 4000) return;
            var px = new Color32[w * h];
            for (var y = 0; y < h; y++)
            for (var x = 0; x < w; x++)
            {
                float wx = x0 + x + .5f, wz = z0 + y + .5f;
                var isl = data.IslandAt(wx, wz); var g = data.HeightAt(wx, wz);
                Color c = isl == null || g < .2f ? new Color(.1f, .16f, .26f) : isl != ctx.Island ? new Color(.3f, .3f, .3f) : Color.Lerp(new Color(.2f, .28f, .2f), new Color(.38f, .44f, .34f), Mathf.Clamp01(g / 30f));
                var k = Mathf.FloorToInt(x / C) + Mathf.FloorToInt(y / C) * nx;
                if (k >= 0 && k < walk.Length && walk[k]) c = path[k] ? new Color(.92f, .9f, .82f) : new Color(.35f, .45f, .65f);
                if (k >= 0 && k < walk.Length && blocked[k]) c = new Color(1f, .15f, .1f);
                if (k >= 0 && k < walk.Length && steep[k]) c = new Color(1f, .55f, 0f);
                c.a = 1f; px[y * w + x] = c;
            }
            void Ring(Vector3 p, int r, Color c, int thick)
            {
                int cx = Mathf.RoundToInt(p.x - x0), cy = Mathf.RoundToInt(p.z - z0);
                for (var dy = -r; dy <= r; dy++) for (var dx = -r; dx <= r; dx++)
                {
                    var d2 = dx * dx + dy * dy; if (d2 > r * r || d2 < (r - thick) * (r - thick)) continue;
                    int x = cx + dx, y = cy + dy; if (x >= 0 && y >= 0 && x < w && y < h) px[y * w + x] = c;
                }
            }
            foreach (var p in problems)
            {
                var col = p.kind == "blocked path" ? new Color(1f, 0f, 1f) : p.kind == "floating" ? new Color(1f, .95f, .1f) : p.kind == "steep or a ledge" ? new Color(1f, .55f, 0f) : new Color(1f, .3f, .3f);
                Ring(p.at, p.shot > 0 ? 9 : 5, col, 2);
            }
            var pz = ctx.Plaza;
            for (var k = -10; k <= 10; k++)
                foreach (var (x, y) in new[] { (Mathf.RoundToInt(pz.x - x0) + k, Mathf.RoundToInt(pz.z - z0)), (Mathf.RoundToInt(pz.x - x0), Mathf.RoundToInt(pz.z - z0) + k) })
                    if (x >= 0 && y >= 0 && x < w && y < h) px[y * w + x] = new Color(.2f, 1f, 1f);
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.SetPixels32(px); tex.Apply();
            System.IO.File.WriteAllBytes(file, tex.EncodeToPNG());
            Object.Destroy(tex);
        }
    }
}
