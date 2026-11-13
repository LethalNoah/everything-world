using System.Collections.Generic;
using UnityEngine;
using static EverythingWorld.World.PlaceTools;

namespace EverythingWorld.World
{
    /// The IDM Glitchlands (WORLD_GUIDE.md): a glitching computer world. You arrive on the Kernel, a plaza of dark tiles
    /// laced with glowing grid lines round a tall monolith whose face is pixel-sorted into running columns of colour; lines
    /// of code float up out of the floor and blocks hang in the air, jumping to new places on the beat. The Bus, a road of
    /// glowing circuit traces with pulses of data racing along it, runs to the Stack, a crooked tower of floating blocks that
    /// keeps slipping out of line. Dark slate and black, terminal green, white, and sudden red.
    /// Laid out on purpose (the build-land standard): GlitchTown.cs has the places and the people.
    public partial class GlitchKit : LandKit
    {
        public override string Land => "The IDM Glitchlands";
        public override Vector3 ArtistFront(IslandWorldData data, IslandWorldData.Artist a) => facing.TryGetValue(a.name, out var f) ? f : base.ArtistFront(data, a);

        public static readonly Color Term = new Color(.3f, 1f, .5f), Red = new Color(1f, .2f, .2f), Cyan = new Color(.3f, .9f, 1f), White = new Color(.92f, .96f, 1f), Ink = new Color(.03f, .05f, .05f), Amber = new Color(1f, .7f, .2f);
        static readonly Color[] Sort = { new Color(.1f, .9f, .5f), new Color(.2f, .6f, 1f), new Color(1f, .2f, .4f), new Color(1f, .8f, .2f), new Color(.8f, .3f, 1f), new Color(.9f, .95f, 1f) };
        const float PathW = 3f, BusW = 6f;

        Material tile, tileD, dark, iron, white, gridM, lampM, codeM, blockM;
        GlowHalos halos; Vector3 center, D, S; GlitchLife life;
        readonly Dictionary<(int, int), List<Vector3>> pathCells = new Dictionary<(int, int), List<Vector3>>();
        readonly List<(Vector3 p, int w)> walkPoints = new List<(Vector3, int)>();
        readonly Dictionary<string, Vector3> facing = new Dictionary<string, Vector3>();
        // x, radius, z
        readonly List<Vector3> noGrass = new List<Vector3>();
        Vector2 heart2, D2 = new Vector2(0, -1), S2 = new Vector2(1, 0); float plazaY, waterY, streetL;

        void MakeMaterials()
        {
            tile = WorldMaterials.Glow(new Color(.12f, .14f, .16f), .06f); tileD = WorldMaterials.Glow(new Color(.07f, .08f, .1f), .04f); dark = WorldMaterials.Solid(new Color(.03f, .04f, .05f)); iron = WorldMaterials.Solid(new Color(.2f, .22f, .24f), .6f); white = WorldMaterials.Glow(new Color(.86f, .9f, .92f), .2f);
            gridM = new Material(WorldMaterials.Glow(Term, 1.1f)) { name = "Glitch grid" }; lampM = new Material(WorldMaterials.Glow(White, 1.6f)) { name = "Glitch lamps" }; codeM = new Material(WorldMaterials.Glow(Term, 1.6f)) { name = "Glitch data" }; blockM = new Material(WorldMaterials.Glow(Cyan, .9f)) { name = "Glitch blocks" };
        }

        Material GlowM(Color c, float s = 2f) => WorldMaterials.Glow(c, s * .65f);
        Material Paint(Color c) => WorldMaterials.Glow(c, .14f);
        static bool HasStation(IslandWorldData.Artist a) => a.stationId != 0;
        static float Foot(IslandWorldData.Artist a) => a.name == "Mr. Bill" ? 16f : 8f;

        public override bool Ground(float height, float slope, float patch, out Color color)
        {
            // dark slate in hard steps, like a low-resolution terrain
            var q = Mathf.Floor(patch * 4f) / 3f;
            if (slope > 24f) color = Color.Lerp(new Color(.2f, .22f, .26f), new Color(.28f, .3f, .34f), q);
            else color = Color.Lerp(new Color(.16f, .2f, .22f), new Color(.24f, .28f, .3f), q);
            color.a = 1f;
            return true;
        }

        static float Ease(float a, float b, float x) { var t = Mathf.Clamp01((x - a) / (b - a)); return t * t * (3f - 2f * t); }
        Vector2 P2(float d, float s) => heart2 + D2 * d + S2 * s;
        Vector3 Wp(float d, float s) => center + D * d + S * s;
        Vector3 G(LandContext ctx, float d, float s) { var p = Wp(d, s); return ctx.OnGround(p.x, p.z); }

        // ---------------------------------------------------------------- shaping the ground
        public override void ShapeGround(IslandWorldData data, int index)
        {
            var isl = data.layout.islands[index]; heart2 = new Vector2(isl.labelX, isl.labelZ); waterY = data.layout.waterLevel;
            bool Inside(Vector2 p, float m) { if (data.IslandAt(p.x, p.y) != isl) return false; for (var k = 0; k < 12; k++) { var a = k * Mathf.PI / 6f; if (data.IslandAt(p.x + Mathf.Cos(a) * m, p.y + Mathf.Sin(a) * m) != isl) return false; } return true; }
            streetL = 0f;
            for (var k = 0; k < 8; k++) { var dir = new Vector2(Mathf.Sin(k * Mathf.PI / 4f), Mathf.Cos(k * Mathf.PI / 4f)); var len = 40f; while (len < 100f && Inside(heart2 + dir * (len + 20f), 26f)) len += 10f; if (len > streetL) { streetL = len; D2 = dir; } }
            S2 = new Vector2(-D2.y, D2.x); plazaY = Mathf.Max(data.HeightAt(heart2.x, heart2.y), waterY + 6f);
            var keep = new List<string>();
            void Put(string name, float d, float s, float room) { var p = P2(d, s); if (!Inside(p, room)) return; foreach (var a in data.layout.artists) if (a.sceneId == isl.sceneId && a.name == name) { a.x = p.x; a.z = p.y; keep.Add(name); } }
            Put("Mr. Bill", 4f, -56f, 26f);
            var stack = P2(streetL + 14f, 0);
            var avoid = new List<Vector3>(); for (var d = 0f; d <= streetL; d += 20f) avoid.Add(new Vector3(heart2.x + D2.x * d, 18f, heart2.y + D2.y * d)); avoid.Add(new Vector3(stack.x, 18f, stack.y));
            ArtistPlacement.Spread(data, index, 46f, heart2, 40f, 36f, avoid, keep, Land, 320f);
            Level(data, index, heart2, 20f, 32f, plazaY);
            for (var d = 20f; d <= streetL; d += 8f) { var p = P2(d, 0); Level(data, index, p, 5f, 11f, Mathf.Lerp(plazaY, Mathf.Max(data.HeightAt(p.x, p.y), waterY + 3f), Mathf.Clamp01((d - 20f) / 30f))); }
            Level(data, index, stack, 12f, 20f, Mathf.Max(data.HeightAt(stack.x, stack.y), waterY + 3f));
            foreach (var a in data.layout.artists)
            {
                if (a.sceneId != isl.sceneId) continue;
                a.r = Mathf.Max(a.r, Foot(a) + 6f);
                Level(data, index, new Vector2(a.x, a.z), Foot(a) + 1f, Foot(a) + 9f, Mathf.Max(data.HeightAt(a.x, a.z), waterY + 2.6f));
            }
        }

        void Level(IslandWorldData data, int index, Vector2 c, float flat, float blend, float level)
        {
            for (var r = 0; r < data.Nz; r++)
            {
                var z = data.layout.depth - r * data.Step; if (Mathf.Abs(z - c.y) > blend) continue;
                for (var col = 0; col < data.Nx; col++)
                {
                    var x = col * data.Step; if (Mathf.Abs(x - c.x) > blend) continue;
                    var i = r * data.Nx + col; if (data.islandIds[i] != index) continue;
                    var d = Vector2.Distance(new Vector2(x, z), c);
                    if (d < flat) data.heights[i] = level;
                    else if (d < blend) data.heights[i] = Mathf.Lerp(level, data.heights[i], Ease(0f, 1f, (d - flat) / (blend - flat)));
                }
            }
        }

        // ---------------------------------------------------------------- dressing
        public override void Dress(LandContext ctx)
        {
            MakeMaterials();
            halos = new GlowHalos();
            pathCells.Clear(); walkPoints.Clear(); facing.Clear(); noGrass.Clear();
            center = ctx.Plaza; D = new Vector3(D2.x, 0, D2.y); S = new Vector3(S2.x, 0, S2.y);
            life = new GameObject("Glitch life").AddComponent<GlitchLife>();
            life.transform.SetParent(ctx.Root, false);
            life.center = center; life.grid = gridM; life.lamps = lampM; life.code = codeM; life.blocks = blockM;
            var mk = new MeshKit(); var solid = new MeshKit();
            Remember(ctx.OnGround(center.x, center.z), 0);
            TownStart(ctx);
            Kernel(ctx, mk, solid);
            Bus(ctx, mk, solid);
            Stack(ctx, mk, solid);
            Paths(ctx, mk, solid);
            Wilds(ctx, mk, solid);
            mk.Build("The IDM Glitchlands", ctx.Root, false);
            solid.BuildCollider("The IDM Glitchlands walls", ctx.Root);
        }

        public override void Finish(LandContext ctx) { Verge(ctx); halos.Build("Glitch glow", ctx.Root); }

        // ---------------------------------------------------------------- helpers
        void Remember(Vector3 p, int walk)
        {
            var key = (Mathf.FloorToInt(p.x / 8f), Mathf.FloorToInt(p.z / 8f));
            if (!pathCells.TryGetValue(key, out var l)) pathCells[key] = l = new List<Vector3>();
            l.Add(p); walkPoints.Add((p, walk));
        }

        bool NearPath(float x, float z, float within)
        {
            int cx = Mathf.FloorToInt(x / 8f), cz = Mathf.FloorToInt(z / 8f); var n = Mathf.CeilToInt(within / 8f);
            for (var i = -n; i <= n; i++)
            for (var j = -n; j <= n; j++)
                if (pathCells.TryGetValue((cx + i, cz + j), out var l))
                    foreach (var p in l) { float dx = p.x - x, dz = p.z - z; if (dx * dx + dz * dz < within * within) return true; }
            return false;
        }

        bool LaneNear(float x, float z, float within) { foreach (var (p, w) in walkPoints) { if (w < 10) continue; float dx = p.x - x, dz = p.z - z; if (dx * dx + dz * dz < within * within) return true; } return false; }

        bool InNoGrass(float x, float z, float pad)
        {
            foreach (var c in noGrass) { float dx = x - c.x, dz = z - c.z, r = c.y + pad; if (dx * dx + dz * dz < r * r) return true; }
            return false;
        }

        bool InFootprint(LandContext ctx, float x, float z, float pad)
        {
            foreach (var a in ctx.Artists) { float dx = x - a.x, dz = z - a.z, r = Foot(a) + pad; if (dx * dx + dz * dz < r * r) return true; }
            return false;
        }

        bool Open(LandContext ctx, float x, float z, float pad) =>
            ctx.IsLand(x, z, 5f) && !InFootprint(ctx, x, z, pad) && !NearPath(x, z, pad + 2f) && !InNoGrass(x, z, pad);

        void Strip(MeshKit mk, List<Vector3> pts, Material m, float w, float lift, LandContext ground)
        {
            var n = pts.Count; var L = new Vector3[n]; var Rr = new Vector3[n];
            Vector3 Flat(Vector3 v) { v.y = 0; return v.normalized; }
            for (var i = 0; i < n; i++)
            {
                var a = i > 0 ? Flat(pts[i] - pts[i - 1]) : Flat(pts[1] - pts[0]);
                var b = i < n - 1 ? Flat(pts[i + 1] - pts[i]) : Flat(pts[n - 1] - pts[n - 2]);
                var t = (a + b).normalized; var side = Vector3.Cross(Vector3.up, t);
                var miter = 1f / Mathf.Max(.6f, Vector3.Dot(side, Vector3.Cross(Vector3.up, b)));
                L[i] = pts[i] - side * (w / 2f * miter); Rr[i] = pts[i] + side * (w / 2f * miter);
                float gl = ground.Ground(L[i].x, L[i].z), gr = ground.Ground(Rr[i].x, Rr[i].z), bulge = Mathf.Clamp(pts[i].y - (gl + gr) / 2f, 0f, .3f);
                L[i].y = gl + bulge + lift; Rr[i].y = gr + bulge + lift;
            }
            for (var i = 0; i + 1 < n; i++) mk.Quad(m, L[i], Rr[i], Rr[i + 1], L[i + 1], (pts[i] + pts[i + 1]) / 2f - Vector3.up);
        }

        /// A block bench to sit on.
        void Bench(Transform parent, MeshKit mk, Vector3 g, Vector3 face, float len = 2f)
        {
            var up = Vector3.up; var rot = Quaternion.LookRotation(face); var side = Vector3.Cross(up, face); mk.Box(tile, g + up * .25f, new Vector3(len, .5f, .6f), rot); mk.Box(gridM, g + up * .51f + face * .29f, new Vector3(len, .02f, .02f), rot);
            BoxCol(parent, g + up * .25f, new Vector3(len, .5f, .6f), rot); Interact.Bench(tlife.transform, g + up * .5f - side * (len / 2f - .5f), g + up * .5f + side * (len / 2f - .5f), face);
        }

        /// A post with a floating glowing cube.
        void Lamp(LandContext ctx, MeshKit mk, Vector3 g, bool light = false)
        {
            var up = Vector3.up; mk.Box(dark, g + up * 1.3f, new Vector3(.14f, 2.6f, .14f), Quaternion.identity); mk.Box(lampM, g + up * 3f, Vector3.one * .4f, Quaternion.Euler(45f, 45f, 0)); halos.Add(g + up * 3f, 1.4f, White, .4f); ArtistBits.PostCollider(ctx.Root, g, 2.6f, .1f); if (light) ArtistBits.PointLight(ctx.Root, g + up * 2.8f, new Color(.8f, 1f, .9f), 12f, 1.3f);
        }

        /// The twelve edges of a cube, as glowing bars.
        static void WireCube(MeshKit mk, Material m, Vector3 c, float sz, Quaternion rot)
        {
            var h = sz / 2f;
            foreach (var y in new[] { -h, h }) foreach (var z in new[] { -h, h }) mk.Box(m, c + rot * new Vector3(0, y, z), new Vector3(sz, .06f, .06f), rot);
            foreach (var x in new[] { -h, h }) foreach (var z in new[] { -h, h }) mk.Box(m, c + rot * new Vector3(x, 0, z), new Vector3(.06f, sz, .06f), rot);
            foreach (var x in new[] { -h, h }) foreach (var y in new[] { -h, h }) mk.Box(m, c + rot * new Vector3(x, y, 0), new Vector3(.06f, .06f, sz), rot);
        }

        /// A floating block that jumps to a new spot on the beat (GlitchLife).
        void Jumper(Transform parent, Vector3 at, float s, Material m)
        {
            var b = new MeshKit(); b.Box(m, Vector3.zero, Vector3.one * s, Quaternion.identity); var g = b.BuildSingle("Glitch block", parent, false); g.transform.position = at; life.jumpers.Add((g.transform, at, (at.x * 3.1f + at.z * 1.7f) % 9f));
        }

        // ---------------------------------------------------------------- the lanes to the artists
        void Paths(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var walk = 10; var n = 0;
            void Lay(Vector3 join, Vector3 to, float stopShort, string artist)
            {
                var from = new Vector3(join.x, 0, join.z); to.y = 0;
                var dir = to - from; var total = dir.magnitude; if (total < 2f) return; dir /= total;
                if (artist != null) facing[artist] = -dir;
                var side = Vector3.Cross(Vector3.up, dir); var end = total - stopShort; if (end < PathW) return;
                var pts = new List<Vector3>();
                for (var d = PathW / 2f + .1f; ; d += 1.5f)
                {
                    var dd = Mathf.Min(d, end); var q = from + dir * dd;
                    if (!ctx.IsLand(q.x, q.z, 0f)) break;
                    pts.Add(ctx.OnGround(q.x, q.z)); if (dd >= end) break;
                }
                if (pts.Count < 2) return;
                Strip(mk, pts, tileD, PathW, .1f, ctx); var e1 = new List<Vector3>(); var e2 = new List<Vector3>(); foreach (var p in pts) { e1.Add(p + side * (PathW / 2f - .1f)); e2.Add(p - side * (PathW / 2f - .1f)); } Strip(mk, e1, gridM, .06f, .12f, ctx); Strip(mk, e2, gridM, .06f, .12f, ctx);
                foreach (var p in pts) Remember(p, walk);
                for (var i = 6; i < pts.Count; i += 10, n++)
                {
                    var s = n % 2 == 0 ? 1f : -1f; var lp = pts[i] + side * s * (PathW / 2f + .8f);
                    if (InFootprint(ctx, lp.x, lp.z, 1f) || InNoGrass(lp.x, lp.z, 1f)) continue;
                    Lamp(ctx, mk, ctx.OnGround(lp.x, lp.z), n % 3 == 0);
                }
                walk++;
            }
            var todo = new List<IslandWorldData.Artist>(ctx.Artists);
            while (todo.Count > 0)
            {
                IslandWorldData.Artist next = null; Vector3 join = default; var best = float.MaxValue;
                foreach (var a in todo)
                    foreach (var (p, w) in walkPoints) { var d = Vector2.Distance(new Vector2(p.x, p.z), new Vector2(a.x, a.z)) - Foot(a); if (d < best) { best = d; next = a; join = p; } }
                if (next == null) break;
                todo.Remove(next);
                Lay(join, new Vector3(next.x, 0, next.z), Foot(next) * .8f, next.name);
            }
        }

        // ---------------------------------------------------------------- the rest of the world
        /// Wireframe cubes, floating jumping blocks, corrupted slabs of striped colour, grid tiles, giant cursors, half-loaded trees, benches.
        void Wilds(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            int wires = 0, jumps = 0, corrupt = 0, grids = 0, cursors = 0, trees = 0, benches = 0; var rng = new System.Random(2001);
            const float cell = 20f;
            for (var x = ctx.MinX; x < ctx.MaxX; x += cell)
            for (var z = ctx.MinZ; z < ctx.MaxZ; z += cell)
            {
                float px = x + (float)rng.NextDouble() * cell, pz = z + (float)rng.NextDouble() * cell; var roll = (float)rng.NextDouble(); var s = (float)rng.NextDouble();
                if (!Open(ctx, px, pz, 3f)) continue;
                var p = ctx.OnGround(px, pz); var f = FaceTo(p, NearestWalk(p)); var rot = Quaternion.LookRotation(f); var side = Vector3.Cross(Vector3.up, f); var up = Vector3.up;
                if (roll < .16f) { var sz = 2f + s * 3f; var cc = p + up * (sz / 2f + .2f); var m = s > .5f ? gridM : GlowM(Cyan, 1.2f); WireCube(mk, m, cc, sz, rot); BoxCol(ctx.Root, cc, Vector3.one * (sz + .1f), rot); Mark(p, sz * .7f); wires++; }
                else if (roll < .32f) { for (var k = 0; k < 3 + (int)(s * 4f); k++) Jumper(ctx.Root, p + up * (2f + k * .8f + s * 3f) + side * (k - 2f) * 1.2f, .4f + (k % 3) * .3f, k % 3 == 0 ? blockM : GlowM(Sort[k % Sort.Length], 1f)); jumps++; }
                else if (roll < .44f) { var h = 2f + s * 4f; for (var k = 0; k < 10; k++) mk.Box(GlowM(Sort[(k + (int)(s * 9f)) % Sort.Length], .6f + (k % 3) * .3f), p + up * (h / 2f) + side * (k - 4.5f) * .3f + f * ((k * 7 % 3) - 1f) * .1f, new Vector3(.3f, h * (.5f + (k * 13 % 7) * .08f), .3f), rot); BoxCol(ctx.Root, p + up * h / 2f, new Vector3(3.2f, h, .6f), rot); Mark(p, 2f); corrupt++; }
                else if (roll < .52f) { for (var i = -3; i <= 3; i++) { mk.Box(gridM, p + up * .06f + side * i * 1f, new Vector3(.04f, .02f, 6f), rot); mk.Box(gridM, p + up * .06f + f * i * 1f, new Vector3(6f, .02f, .04f), rot); } grids++; }
                else if (roll < .57f) { var cp = p + up * .1f; var cm = s > .5f ? white : GlowM(White, 1f); var pts = new[] { new Vector3(0, 0, 0), new Vector3(0, 3.6f, 0), new Vector3(1f, 2.6f, 0), new Vector3(1.9f, 3.9f, 0), new Vector3(2.3f, 3.6f, 0), new Vector3(1.5f, 2.4f, 0), new Vector3(2.6f, 2.4f, 0) }; for (var k = 0; k < pts.Length - 1; k++) { var a = cp + rot * pts[k] * .8f + up * 0f; var b = cp + rot * pts[k + 1] * .8f; mk.Box(cm, (a + b) / 2f, new Vector3(.12f, .12f, (b - a).magnitude), Quaternion.LookRotation(b - a == Vector3.zero ? f : b - a)); } mk.Tri(cm, cp, cp + rot * new Vector3(0, 3.6f, 0) * .8f, cp + rot * new Vector3(2.6f, 2.4f, 0) * .8f, cp - f); mk.Tri(cm, cp, cp + rot * new Vector3(0, 3.6f, 0) * .8f, cp + rot * new Vector3(2.6f, 2.4f, 0) * .8f, cp + f); BoxCol(ctx.Root, cp + up * 1.4f, new Vector3(2f, 2.8f, .3f), rot); cursors++; }
                else if (roll < .7f) { mk.Box(Paint(new Color(.3f, .24f, .2f)), p + up * 1.2f, new Vector3(.4f, 2.4f, .4f), Quaternion.identity); var lc = p + up * 3.2f; for (var k = 0; k < 7; k++) { var o = new Vector3((k * 37 % 5) - 2f, (k * 13 % 3), (k * 23 % 5) - 2f) * .5f; mk.Box(k % 3 == 0 ? GlowM(Term, .9f) : Paint(new Color(.2f, .5f, .3f)), lc + o, Vector3.one * (.7f - (k % 3) * .15f), Quaternion.identity); } ArtistBits.PostCollider(ctx.Root, p, 2.4f, .3f); Mark(p, 1.6f); trees++; }
                else if (roll < .76f) { Bench(ctx.Root, mk, p, f); Lamp(ctx, mk, p - f * .8f + side * 1.6f, benches % 2 == 0); Mark(p, 2.2f); benches++; }
            }
            Debug.Log($"The IDM Glitchlands: the Bus {streetL:0} m, {wires} wireframe cubes, {jumps} floating block groups, {corrupt} corrupted slabs, {grids} grid patches, {cursors} giant cursors, {trees} half-loaded trees, {benches} benches");
        }

        Vector3 NearestWalk(Vector3 p) { var best = center; var bd = float.MaxValue; foreach (var (q, _) in walkPoints) { var d = (q - p).sqrMagnitude; if (d < bd) { bd = d; best = q; } } return best; }

        /// Short square tufts in two greens, a few tips lit like pixels.
        void Verge(LandContext ctx)
        {
            var mat = WindMesh.Material(.2f, .8f, new Color(.2f, .3f, .26f));
            var meshes = new Dictionary<(int, int), WindMesh>(); var rng = new System.Random(2002);
            const float st = 3f;
            for (var x = ctx.MinX; x < ctx.MaxX; x += st)
            for (var z = ctx.MinZ; z < ctx.MaxZ; z += st)
            {
                var px = x + ((float)rng.NextDouble() - .5f) * st; var pz = z + ((float)rng.NextDouble() - .5f) * st;
                if (!ctx.IsLand(px, pz, 1f) || NearPath(px, pz, 3.6f) || InNoGrass(px, pz, 0f) || InFootprint(ctx, px, pz, -2f)) continue;
                if (Mathf.PerlinNoise(px * .05f + 3f, pz * .05f + 6f) < .5f) continue;
                var key = (Mathf.FloorToInt(px / 70f), Mathf.FloorToInt(pz / 70f));
                if (!meshes.TryGetValue(key, out var wm)) meshes[key] = wm = new WindMesh();
                var col = (float)rng.NextDouble() < .5f ? new Color(.16f, .26f, .22f) : new Color(.2f, .32f, .26f);
                wm.Tuft(new Vector3(px, ctx.Ground(px, pz) - .04f, pz), .2f + (float)rng.NextDouble() * .2f, col, Color.Lerp(col, Term, .5f), rng, 3, .1f);
            }
            foreach (var kv in meshes) kv.Value.Build("Glitch grass", ctx.Root, mat);
        }
    }
}
