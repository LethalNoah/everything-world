using System.Collections.Generic;
using UnityEngine;

namespace EverythingWorld.World
{
    /// The Metalcore Caldera (WORLD_GUIDE.md): cathartic release. A basin of black obsidian and pale ash ringed by
    /// volcanoes, their craters and lava running not orange but neon: cyan, violet and magenta. In the basin, where you
    /// arrive, the pit arena: a round floor ringed with neon, a stage of amp stacks in the middle, stands rising round it
    /// and a crowd running a circle pit. The land builds and breaks like the music: during about half the songs, at
    /// the breakdown, the ground's cracks throb faster, then the volcanoes erupt, throwing neon sparks and smoke into a
    /// magenta sky while the lava streams blaze, and afterwards everything glows quietly while ash drifts down. Obsidian
    /// spires, neon crystals, steam vents, toppled amps, chain-link pens; basalt paths edged with cyan studs and neon
    /// posts. Reads by bright cold blue and violet against black and pale ash (the Iron Range is orange and black).
    /// Files: CalderaKit (ground, arena, volcanoes, paths, wilds), CalderaArtists, CalderaMore, CalderaSongIds,
    /// CalderaLife.
    public partial class CalderaKit : LandKit
    {
        public override string Land => "Metalcore Caldera";
        public override Vector3 ArtistFront(IslandWorldData data, IslandWorldData.Artist a) => facing.TryGetValue(a.name, out var f) ? f : base.ArtistFront(data, a);

        public static readonly Color Cyan = new Color(.2f, .92f, 1f), Violet = new Color(.6f, .3f, 1f), Magenta = new Color(1f, .25f, .75f), Ink = new Color(.9f, .95f, 1f);
        const float PathW = 3.2f;

        Material obsidian, basalt, ash, ashDark, steel, chain, dark, grille, lavaCyan, lavaViolet, crackM, neonPost;
        GlowHalos halos; Vector3 center; CalderaLife life;
        readonly Dictionary<(int, int), List<Vector3>> pathCells = new Dictionary<(int, int), List<Vector3>>();
        readonly List<(Vector3 p, int w)> walkPoints = new List<(Vector3, int)>();
        readonly Dictionary<string, Vector3> facing = new Dictionary<string, Vector3>();
        readonly List<Vector3> noGrass = new List<Vector3>();
        readonly List<Vector3> volcanoes = new List<Vector3>();   // x, R, z
        Vector2 heart2, arena2; bool hasArena;
        const float ArenaR = 34f;

        void MakeMaterials()
        {
            obsidian = WorldMaterials.Solid(new Color(.07f, .07f, .09f), .8f); basalt = WorldMaterials.Solid(new Color(.17f, .17f, .19f), .3f);
            ash = WorldMaterials.Solid(new Color(.52f, .52f, .55f)); ashDark = WorldMaterials.Solid(new Color(.3f, .3f, .33f));
            steel = WorldMaterials.Solid(new Color(.4f, .42f, .46f), .7f); chain = WorldMaterials.Solid(new Color(.45f, .46f, .5f), .8f);
            dark = WorldMaterials.Solid(new Color(.04f, .04f, .05f)); grille = WorldMaterials.Solid(new Color(.15f, .15f, .16f));
            lavaCyan = new Material(WorldMaterials.Glow(Cyan, 1.4f)) { name = "Neon lava cyan" };
            lavaViolet = new Material(WorldMaterials.Glow(Violet, 1.6f)) { name = "Neon lava violet" };
            crackM = new Material(WorldMaterials.Glow(Magenta, 1.2f)) { name = "Glowing cracks" };
            neonPost = new Material(WorldMaterials.Glow(Cyan, 2.2f)) { name = "Neon posts" };
        }

        Material GlowM(Color c, float s = 2f) => WorldMaterials.Glow(c, s * .65f);
        static bool HasStation(IslandWorldData.Artist a) => a.stationId != 0;
        static float Foot(IslandWorldData.Artist a)
        {
            switch (a.name)
            {
                case "blessthefall": return 22f;
                case "Killswitch Engage": case "Like Moths To Flames": return 19f;
                case "Dayseeker": case "Alexisonfire": return 15f;
                default: return HasStation(a) ? 13f : 7.5f;
            }
        }

        public override bool Ground(float height, float slope, float patch, out Color color)
        {
            if (slope > 24f) color = Color.Lerp(new Color(.08f, .08f, .1f), new Color(.15f, .14f, .17f), patch);
            else if (slope > 12f) color = Color.Lerp(new Color(.2f, .2f, .23f), new Color(.38f, .37f, .41f), patch);
            else color = Color.Lerp(new Color(.34f, .33f, .37f), new Color(.58f, .57f, .61f), Mathf.SmoothStep(0f, 1f, patch));   // pale ash on the flats
            color.a = 1f;
            return true;
        }

        static void BoxCol(Transform parent, Vector3 center, Vector3 size, Quaternion rot)
        {
            var go = new GameObject("Box collider"); go.transform.SetParent(parent, false); go.transform.SetPositionAndRotation(center, rot);
            go.AddComponent<BoxCollider>().size = size; go.isStatic = true;
        }

        static float Ease(float a, float b, float x) { var t = Mathf.Clamp01((x - a) / (b - a)); return t * t * (3f - 2f * t); }
        bool OnVolcano(float x, float z, float pad) { foreach (var v in volcanoes) if (Vector2.Distance(new Vector2(x, z), new Vector2(v.x, v.z)) < v.y + pad) return true; return false; }

        // ---------------------------------------------------------------- shaping the ground
        void ShapeCaldera(IslandWorldData data, int index)
        {
            var isl = data.layout.islands[index]; heart2 = new Vector2(isl.labelX, isl.labelZ);
            volcanoes.Clear();
            bool Inside(Vector2 p, float m) { if (data.IslandAt(p.x, p.y) != isl) return false; for (var k = 0; k < 16; k++) { var a = k * Mathf.PI / 8f; if (data.IslandAt(p.x + Mathf.Cos(a) * m, p.y + Mathf.Sin(a) * m) != isl) return false; } return true; }
            // the arena: beside the arrival, on the flattest ground that fits
            hasArena = false; var best = float.MaxValue;
            for (var k = 0; k < 16; k++)
            {
                var a = k * Mathf.PI / 8f; var c = heart2 + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 64f; if (!Inside(c, ArenaR + 12f)) continue;
                float lo = float.MaxValue, hi = float.MinValue; for (var j = 0; j < 12; j++) { var b = j * Mathf.PI / 6f; var h = data.HeightAt(c.x + Mathf.Cos(b) * ArenaR, c.y + Mathf.Sin(b) * ArenaR); lo = Mathf.Min(lo, h); hi = Mathf.Max(hi, h); }
                if (hi - lo < best) { best = hi - lo; arena2 = c; hasArena = true; }
            }
            // the volcanoes: on the high ground round the basin, as tall cones with craters
            var cands = new List<(Vector2 p, float h)>();
            for (var r = 0; r < data.Nz; r += 2)
            {
                var z = data.layout.depth - r * data.Step; if (Mathf.Abs(z - heart2.y) > 560f) continue;
                for (var col = 0; col < data.Nx; col += 2)
                {
                    var x = col * data.Step; if (Mathf.Abs(x - heart2.x) > 560f) continue;
                    var i = r * data.Nx + col; if (data.islandIds[i] != index) continue;
                    var d = Vector2.Distance(new Vector2(x, z), heart2); if (d < 190f || d > 540f) continue;
                    cands.Add((new Vector2(x, z), data.heights[i]));
                }
            }
            cands.Sort((a, b) => b.h.CompareTo(a.h));
            foreach (var (p, h) in cands)
            {
                if (volcanoes.Count >= 5) break;
                var far = true; foreach (var v in volcanoes) if (Vector2.Distance(p, new Vector2(v.x, v.z)) < 200f) far = false;
                if (!far) continue;
                for (var R = 80f; R >= 50f; R -= 10f) if (Inside(p, R)) { volcanoes.Add(new Vector3(p.x, R, p.y)); break; }
            }
            foreach (var v in volcanoes)
            {
                var R = v.y; var H = R * .5f;
                for (var r = 0; r < data.Nz; r++)
                {
                    var z = data.layout.depth - r * data.Step; if (Mathf.Abs(z - v.z) > R) continue;
                    for (var col = 0; col < data.Nx; col++)
                    {
                        var x = col * data.Step; if (Mathf.Abs(x - v.x) > R) continue;
                        var i = r * data.Nx + col; if (data.islandIds[i] != index) continue;
                        var ang = Mathf.Atan2(z - v.z, x - v.x); var d = Vector2.Distance(new Vector2(x, z), new Vector2(v.x, v.z)) / (R * (1f + .07f * Mathf.Sin(ang * 5f)));
                        if (d >= 1f) continue;
                        var add = H * Mathf.Pow(1f - d, 1.25f);
                        if (d < .2f) add -= H * .45f * (1f - d / .2f);   // the crater
                        data.heights[i] += add;
                    }
                }
            }
            var avoid = new List<Vector3>();
            foreach (var v in volcanoes) avoid.Add(new Vector3(v.x, v.y * .85f, v.z));
            if (hasArena) avoid.Add(new Vector3(arena2.x, ArenaR + 14f, arena2.y));
            ArtistPlacement.Spread(data, index, 50f, heart2, 46f, 42f, avoid, null, Land, 420f);
            foreach (var a in data.layout.artists)
            {
                if (a.sceneId != isl.sceneId) continue;
                a.r = Mathf.Max(a.r, Foot(a) + 6f);
                Level(data, index, new Vector2(a.x, a.z), Foot(a) + 1f, Foot(a) + 12f);
            }
            Level(data, index, heart2, 18f, 32f);
            if (hasArena) Level(data, index, arena2, ArenaR + 4f, ArenaR + 22f);
        }

        void Level(IslandWorldData data, int index, Vector2 c, float flat, float blend)
        {
            var level = data.HeightAt(c.x, c.y);
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
            center = ctx.Plaza;
            life = new GameObject("Caldera life").AddComponent<CalderaLife>();
            life.transform.SetParent(ctx.Root, false);
            life.center = center; life.lavaCyan = lavaCyan; life.lavaViolet = lavaViolet; life.cracks = crackM; life.neon = neonPost;
            var mk = new MeshKit(); var solid = new MeshKit();
            Remember(ctx.OnGround(center.x, center.z), 0);
            Arrival(ctx, mk, solid);
            if (hasArena) Arena(ctx, mk, solid);
            foreach (var v in volcanoes) Volcano(ctx, mk, solid, v);
            Paths(ctx, mk, solid);
            BasinPlaces(ctx, mk, solid);
            Wilds(ctx, mk, solid);
            mk.Build("Metalcore Caldera", ctx.Root, false);
            solid.BuildCollider("Metalcore Caldera walls", ctx.Root);
        }

        public override void Finish(LandContext ctx) { AshTufts(ctx); halos.Build("Caldera glow", ctx.Root); }

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
            ctx.IsLand(x, z, 5f) && !InFootprint(ctx, x, z, pad) && !NearPath(x, z, pad + 2f) && !InNoGrass(x, z, pad)
            && Vector2.Distance(new Vector2(x, z), new Vector2(center.x, center.z)) > 24f + pad
            && (!hasArena || Vector2.Distance(new Vector2(x, z), arena2) > ArenaR + 6f + pad);

        void Strip(MeshKit mk, List<Vector3> pts, Material m, float w, float lift, LandContext ground = null)
        {
            var n = pts.Count; var L = new Vector3[n]; var R = new Vector3[n];
            Vector3 Flat(Vector3 v) { v.y = 0; return v.normalized; }
            for (var i = 0; i < n; i++)
            {
                var a = i > 0 ? Flat(pts[i] - pts[i - 1]) : Flat(pts[1] - pts[0]);
                var b = i < n - 1 ? Flat(pts[i + 1] - pts[i]) : Flat(pts[n - 1] - pts[n - 2]);
                var t = (a + b).normalized; var side = Vector3.Cross(Vector3.up, t);
                var miter = 1f / Mathf.Max(.6f, Vector3.Dot(side, Vector3.Cross(Vector3.up, b)));
                L[i] = pts[i] + Vector3.up * lift - side * (w / 2f * miter); R[i] = pts[i] + Vector3.up * lift + side * (w / 2f * miter);
                if (ground != null)
                {
                    float gl = ground.Ground(L[i].x, L[i].z), gr = ground.Ground(R[i].x, R[i].z), top = Mathf.Max(pts[i].y, Mathf.Max(gl, gr) - .05f);
                    // level across where it can be, but an edge never stands more than .25 m over its own ground (on a
                    // cross-slope the downhill edge used to float a metre or more over the hillside)
                    L[i].y = Mathf.Clamp(top - .15f, gl, gl + .25f) + lift; R[i].y = Mathf.Clamp(top - .15f, gr, gr + .25f) + lift;
                }
            }
            for (var i = 0; i + 1 < n; i++) mk.Quad(m, L[i], R[i], R[i + 1], L[i + 1], (pts[i] + pts[i + 1]) / 2f - Vector3.up);
        }

        /// A neon post: a black pole with a vertical cyan tube, its light on the ash.
        void NeonPost(LandContext ctx, MeshKit mk, Vector3 g)
        {
            var up = Vector3.up;
            mk.Box(dark, g + up * 1.9f, new Vector3(.22f, 3.8f, .22f), Quaternion.identity);
            mk.Box(neonPost, g + up * 2.5f + new Vector3(.12f, 0, 0), new Vector3(.06f, 2f, .06f), Quaternion.identity);
            mk.Box(neonPost, g + up * 2.5f - new Vector3(.12f, 0, 0), new Vector3(.06f, 2f, .06f), Quaternion.identity);
            halos.Add(g + up * 2.5f, 2.2f, Cyan, .45f); life.posts.Add(g + up * 2.5f);
            ArtistBits.PostCollider(ctx.Root, g, 3.8f, .2f);
        }

        /// A toppled stack of amps: cabinets lying at angles, grilles up, a cable snaking off.
        void ToppledAmps(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 at)
        {
            var up = Vector3.up; var yaw = ctx.Range(0, 360f);
            // seated on the lowest ground under the stack, so on a slope the cabinets sink into the hill instead of
            // hanging in the air on the downhill side
            for (var k = 0; k < 8; k++) { var a = k * Mathf.PI / 4f; at.y = Mathf.Min(at.y, ctx.Ground(at.x + Mathf.Cos(a) * 3f, at.z + Mathf.Sin(a) * 3f)); }
            for (var k = 0; k < 4; k++)
            {
                var rot = Quaternion.Euler(0, yaw + k * 23f, 0) * Quaternion.Euler(ctx.Range(-80f, -10f) * (k % 2), 0, ctx.Range(-15f, 15f));
                var p = at + Quaternion.Euler(0, yaw, 0) * new Vector3((k - 1.5f) * 1.5f, .7f + (k % 2) * .3f, (k % 2) * 1.1f);
                mk.Box(dark, p, new Vector3(1.4f, 1.4f, .8f), rot); mk.Box(grille, p + rot * Vector3.forward * .41f, new Vector3(1.2f, 1.2f, .02f), rot);
            }
            solid.Box(dark, at + up * .9f + Quaternion.Euler(0, yaw, 0) * new Vector3(0, 0, .55f), new Vector3(6.4f, 1.8f, 2.9f), Quaternion.Euler(0, yaw, 0));   // the cabinets stand 0 and 1.1 m back
        }

        // ---------------------------------------------------------------- arrival
        /// Where you arrive: a basalt circle on the ash with a ring of neon posts, an obsidian slab with the land's name
        /// in cyan, a toppled amp stack, steam vents, and the arena's ring of light just beyond.
        void Arrival(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var c = ctx.OnGround(center.x, center.z);
            var toArena = hasArena ? new Vector3(arena2.x - c.x, 0, arena2.y - c.z).normalized : Vector3.forward; var sd = Vector3.Cross(up, toArena);
            mk.Prism(basalt, c - up * .25f, up, 12f, .33f, 20);
            for (var k = 0; k < 20; k++) { var a = k * Mathf.PI / 10f; mk.Box(lavaCyan, c + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * 11.6f + up * .1f, new Vector3(.15f, .04f, 2.6f), Quaternion.Euler(0, 90f - a * Mathf.Rad2Deg, 0)); }
            for (var k = 0; k < 6; k++) { var a = k * Mathf.PI / 3f + .5f; NeonPost(ctx, mk, ctx.OnGround(c.x + Mathf.Cos(a) * 13.5f, c.z + Mathf.Sin(a) * 13.5f)); }
            var g = c - toArena * 14f; g.y = ctx.Ground(g.x, g.z);
            mk.Box(obsidian, g + up * 2.2f, new Vector3(9f, 4.4f, 1.2f), Quaternion.LookRotation(toArena)); solid.Box(obsidian, g + up * 2.2f, new Vector3(9f, 4.4f, 1.2f), Quaternion.LookRotation(toArena));
            foreach (var y in new[] { .1f, 4.3f }) mk.Box(lavaViolet, g + up * y + toArena * .62f, new Vector3(9f, .1f, .04f), Quaternion.LookRotation(toArena));
            mk.Cone(obsidian, g + up * 4.4f + sd * 3.8f, .8f, 2.6f, 4); mk.Cone(obsidian, g + up * 4.4f - sd * 3.2f, .6f, 1.8f, 4);
            ArtistBits.Label(ctx.Root, "METALCORE CALDERA", g + up * 2.3f + toArena * .64f, toArena, .42f, Cyan, TextAnchor.MiddleCenter);
            halos.Add(g + up * 2.3f + toArena * 1.4f, 4f, Cyan, .25f);
            ToppledAmps(ctx, mk, solid, ctx.OnGround(c.x + sd.x * 16f + toArena.x * 4f, c.z + sd.z * 16f + toArena.z * 4f));
            foreach (var s in new[] { -1f, 1f }) { var v = ctx.OnGround(c.x - sd.x * s * 9f - toArena.x * 6f, c.z - sd.z * s * 9f - toArena.z * 6f); Vent(ctx, mk, v); }
            noGrass.Add(new Vector3(c.x, 15f, c.z));
            var mark = new GameObject("Shot: the arrival and the arena").transform; mark.SetParent(ctx.Root, false);
            var cam = c - toArena * 10f + sd * 4f + up * 1.7f; var look = c + toArena * 60f + up * 4f - cam; mark.SetPositionAndRotation(cam, Quaternion.LookRotation(look.normalized));
        }

        /// A steam vent: a ring of cracked rock round a glowing hole (DocksLife breathes steam from it).
        void Vent(LandContext ctx, MeshKit mk, Vector3 at)
        {
            for (var k = 0; k < 7; k++) { var a = k * Mathf.PI * 2f / 7f; mk.Blob(basalt, at + new Vector3(Mathf.Cos(a), .2f, Mathf.Sin(a)) * .9f, new Vector3(.5f, .35f, .5f), ctx.Rng, .3f); }
            mk.Prism(crackM, at + Vector3.up * .1f, Vector3.up, .6f, .06f, 10); ArtistBits.PostCollider(ctx.Root, at, .9f, 1.3f);
            halos.Add(at + Vector3.up * .6f, 1.4f, Magenta, .4f); life.vents.Add(at + Vector3.up * .3f);
        }

        // ---------------------------------------------------------------- the pit arena
        /// The pit arena: a round floor of black stone with a neon ring set into it, a round stage of amp stacks in the
        /// middle under a lighting truss, stands of obsidian steps rising round it with a gap toward the arrival, four
        /// light towers, and a crowd running a circle pit round the stage (CalderaLife).
        void Arena(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var c = ctx.OnGround(arena2.x, arena2.y);
            var toHeart = new Vector3(center.x - c.x, 0, center.z - c.z).normalized;
            mk.Prism(obsidian, c - up * .2f, up, 22f, .3f, 40);
            foreach (var r in new[] { 8f, 16f, 21.6f })
                for (var k = 0; k < 48; k++) { float a0 = k * Mathf.PI / 24f, a1 = (k + 1) * Mathf.PI / 24f; var p0 = c + new Vector3(Mathf.Cos(a0), 0, Mathf.Sin(a0)) * r + up * .11f; var p1 = c + new Vector3(Mathf.Cos(a1), 0, Mathf.Sin(a1)) * r + up * .11f; mk.Box(r < 10f ? lavaViolet : lavaCyan, (p0 + p1) / 2f, new Vector3(.25f, .04f, Vector3.Distance(p0, p1) + .02f), Quaternion.LookRotation(p1 - p0)); }
            // the stage in the middle: round, amp stacks in a ring at the back half, a drum riser, mics
            mk.Prism(basalt, c, up, 5f, 1.2f, 24); solid.Prism(basalt, c, up, 5f, 1.2f, 16);
            mk.Prism(lavaViolet, c + up * 1.2f, up, 5.05f, .05f, 24);
            for (var k = 0; k < 8; k++)
            {
                var a = Mathf.PI + (k - 3.5f) * .32f + Mathf.Atan2(toHeart.z, toHeart.x); var d = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)); var rot = Quaternion.LookRotation(-d);
                for (var h = 0; h < 2; h++) { var p = c + d * 4.2f + up * (1.2f + .7f + h * 1.4f); mk.Box(dark, p, new Vector3(1.4f, 1.4f, .8f), rot); mk.Box(grille, p - d * .41f, new Vector3(1.2f, 1.2f, .02f), rot); }
                solid.Box(dark, c + d * 4.2f + up * 3.3f, new Vector3(1.5f, 2.9f, .9f), rot);
                mk.Blob(GlowM(Magenta, 3f), c + d * 3.79f + up * 2.4f + Vector3.Cross(up, d) * .5f, Vector3.one * .05f, ctx.Rng, 0f);
            }
            mk.Box(dark, c - toHeart * 1.8f + up * 1.6f, new Vector3(2.6f, .8f, 2f), Quaternion.LookRotation(toHeart));
            foreach (var x in new[] { -.7f, 0f, .7f }) mk.Prism(steel, c - toHeart * 1.8f + Vector3.Cross(up, toHeart) * x + up * 2f, up, .35f, .4f, 12);
            foreach (var x in new[] { -1.8f, 0f, 1.8f }) mk.Prism(steel, c + toHeart * 2.5f + Vector3.Cross(up, toHeart) * x + up * 1.2f, up, .03f, 1.6f, 6);
            // the truss over the stage
            var tr = c + up * 9f; foreach (var k in new[] { 0, 1, 2, 3 }) { var a = k * Mathf.PI / 2f + Mathf.PI / 4f; var leg = c + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * 6.5f; mk.Box(steel, leg + up * 4.5f, new Vector3(.4f, 9f, .4f), Quaternion.identity); ArtistBits.PostCollider(ctx.Root, leg, 9f, .3f); }
            for (var k = 0; k < 24; k++) { float a0 = k * Mathf.PI / 12f, a1 = (k + 1) * Mathf.PI / 12f; var p0 = tr + new Vector3(Mathf.Cos(a0), 0, Mathf.Sin(a0)) * 6.5f; var p1 = tr + new Vector3(Mathf.Cos(a1), 0, Mathf.Sin(a1)) * 6.5f; mk.Box(steel, (p0 + p1) / 2f, new Vector3(.35f, .35f, Vector3.Distance(p0, p1)), Quaternion.LookRotation(p1 - p0)); if (k % 2 == 0) { var lp = p0 - up * .4f; mk.Box(dark, lp, new Vector3(.35f, .45f, .35f), Quaternion.identity); var col = k % 4 == 0 ? Cyan : Magenta; mk.Prism(GlowM(col, 3f), lp - up * .25f, -up, .14f, .05f, 8); halos.Add(lp - up * .4f, 1.2f, col, .5f); } }
            life.stageAt = c + up * 3f;
            ArtistBits.PointLight(ctx.Root, c + up * 7f, Violet, 26f, 2f);
            ArtistBits.PointLight(ctx.Root, c + up * 5f + toHeart * 10f, Cyan, 22f, 1.4f);
            // the stands: rings of obsidian steps rising round the floor, open toward the arrival
            var gap = Mathf.Atan2(toHeart.z, toHeart.x);
            for (var ring = 0; ring < 4; ring++)
            {
                var r0 = 23f + ring * 2.6f; var y = .5f + ring * .9f;
                for (var k = 0; k < 64; k++)
                {
                    float a0 = k * Mathf.PI / 32f, a1 = (k + 1) * Mathf.PI / 32f, am = (a0 + a1) / 2f;
                    if (Mathf.Abs(Mathf.DeltaAngle(am * Mathf.Rad2Deg, gap * Mathf.Rad2Deg)) < 14f) continue;
                    var d = new Vector3(Mathf.Cos(am), 0, Mathf.Sin(am)); var p = c + d * (r0 + 1.3f); var gy = ctx.Ground(p.x, p.z);
                    var top = c.y + y; var hgt = Mathf.Max(.3f, top - gy + .4f);
                    mk.Box(k % 2 == 0 ? obsidian : basalt, new Vector3(p.x, top - hgt / 2f, p.z), new Vector3(2.6f, hgt, r0 * (a1 - a0) + .05f), Quaternion.LookRotation(Vector3.Cross(up, d)));
                    solid.Box(obsidian, new Vector3(p.x, top - hgt / 2f, p.z), new Vector3(2.6f, hgt, r0 * (a1 - a0) + .05f), Quaternion.LookRotation(Vector3.Cross(up, d)));
                    if (ring == 3 && k % 4 == 0) { mk.Box(lavaCyan, new Vector3(p.x, top + .05f, p.z) + d * 1.25f, new Vector3(.1f, .05f, 1.2f), Quaternion.LookRotation(Vector3.Cross(up, d))); }
                }
            }
            // light towers
            for (var k = 0; k < 4; k++)
            {
                var a = gap + Mathf.PI / 4f + k * Mathf.PI / 2f; var d = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)); var tp = ctx.OnGround(c.x + d.x * 36f, c.z + d.z * 36f);
                foreach (var e in new[] { -.6f, .6f }) mk.Box(steel, tp + Vector3.Cross(up, d) * e + up * 8f, new Vector3(.2f, 16f, .2f), Quaternion.identity);
                for (var j = 1; j < 8; j++) mk.Box(steel, tp + up * (j * 2f), new Vector3(1.4f, .1f, .1f), Quaternion.LookRotation(d));
                var head = tp + up * 16.5f; mk.Box(dark, head, new Vector3(3f, 1.4f, .6f), Quaternion.LookRotation(-d));
                for (var j = 0; j < 4; j++) mk.Prism(GlowM(j % 2 == 0 ? Cyan : Ink, 3f), head + Vector3.Cross(up, d) * ((j - 1.5f) * .7f) - d * .31f, -d, .25f, .04f, 10);
                halos.Add(head - d * 1f, 3.6f, Ink, .5f); ArtistBits.PostCollider(ctx.Root, tp, 16f, .9f);
                ArtistBits.PointLight(ctx.Root, head - d * 6f - up * 4f, new Color(.8f, .9f, 1f), 26f, 1.2f);
            }
            // the crowd running the circle pit
            var body = WorldMaterials.Solid(new Color(.05f, .05f, .06f));
            for (var k = 0; k < 40; k++)
            {
                var fk = new MeshKit(); var s = ctx.Range(.9f, 1.1f);
                fk.Box(body, new Vector3(0, .95f * s, 0), new Vector3(.45f, .8f, .28f) * s, Quaternion.identity); fk.Blob(body, new Vector3(0, 1.55f * s, 0), Vector3.one * .16f * s, ctx.Rng, 0f);
                foreach (var e in new[] { -.13f, .13f }) fk.Box(body, new Vector3(e, .3f * s, 0), new Vector3(.16f, .6f, .18f) * s, Quaternion.Euler(e * 120f, 0, 0));
                if (k % 3 == 0) fk.Box(body, new Vector3(.3f, 1.55f * s, 0), new Vector3(.1f, .55f, .1f) * s, Quaternion.Euler(0, 0, -20f));
                var go = fk.BuildSingle("Circle pit", life.transform, false); if (go != null) { life.crowd.Add(go.transform); life.crowdR.Add(ctx.Range(9.5f, 14.5f)); }
            }
            life.arena = c;
            noGrass.Add(new Vector3(c.x, ArenaR + 6f, c.z));
            var mark = new GameObject("Shot: the pit arena").transform; mark.SetParent(ctx.Root, false);
            var cam = c + toHeart * 28f + Vector3.Cross(up, toHeart) * 6f + up * 4.5f; var look = c + up * 3f - cam; mark.SetPositionAndRotation(cam, Quaternion.LookRotation(look.normalized));
        }

        // ---------------------------------------------------------------- the volcanoes
        /// A volcano: a neon lava lake in its crater, a rim of glowing cracks, and three lava streams running down its
        /// flanks between crusted banks.
        void Volcano(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 v)
        {
            var up = Vector3.up; var R = v.y; var top = ctx.OnGround(v.x, v.z);
            var lake = top + up * 1.2f; var lakeR = R * .14f;
            for (var k = 0; k < 24; k++) { float a0 = k * Mathf.PI / 12f, a1 = (k + 1) * Mathf.PI / 12f; mk.Tri(lavaViolet, lake, lake + new Vector3(Mathf.Cos(a0), 0, Mathf.Sin(a0)) * lakeR, lake + new Vector3(Mathf.Cos(a1), 0, Mathf.Sin(a1)) * lakeR, lake - up); }
            halos.Add(lake + up * 3f, lakeR * 1.6f, Violet, .5f);
            ArtistBits.PointLight(ctx.Root, lake + up * 8f, Violet, R * .6f, 2.4f);
            life.craters.Add(lake);
            // glowing cracks round the rim
            for (var k = 0; k < 10; k++)
            {
                var a = k * Mathf.PI / 5f + ctx.Range(-.2f, .2f); var q = new Vector3(v.x + Mathf.Cos(a) * R * .24f, 0, v.z + Mathf.Sin(a) * R * .24f); var p = ctx.OnGround(q.x, q.z);
                var dir = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)); for (var j = 0; j < 4; j++) { var nd = Quaternion.Euler(0, ctx.Range(-30f, 30f), 0) * dir; var r2 = ctx.OnGround(p.x + nd.x * 2.4f, p.z + nd.z * 2.4f); mk.Box(crackM, (p + r2) / 2f + up * .04f, new Vector3(.2f, .05f, 2.5f), Quaternion.LookRotation(r2 - p)); p = r2; }
            }
            // three lava streams, each walking downhill from the rim
            for (var s = 0; s < 3; s++)
            {
                var a = s * Mathf.PI * 2f / 3f + ctx.Range(0f, 1f); var q = new Vector2(v.x + Mathf.Cos(a) * R * .2f, v.z + Mathf.Sin(a) * R * .2f);
                var pts = new List<Vector3>();
                for (var k = 0; k < 70; k++)
                {
                    pts.Add(ctx.OnGround(q.x, q.y) + up * .08f);
                    Vector2 bestD = Vector2.zero; var bestH = ctx.Ground(q.x, q.y);
                    for (var j = 0; j < 12; j++) { var b = j * Mathf.PI / 6f; var o = new Vector2(Mathf.Cos(b), Mathf.Sin(b)) * 2.5f; var h = ctx.Ground(q.x + o.x, q.y + o.y); if (h < bestH) { bestH = h; bestD = o; } }
                    if (bestD == Vector2.zero || !ctx.IsLand(q.x, q.y, 3f) || InFootprint(ctx, q.x, q.y, 4f) || (hasArena && Vector2.Distance(q, arena2) < ArenaR + 10f)) break;
                    if (Vector2.Distance(q, new Vector2(v.x, v.z)) > R * 1.05f) break;
                    q += bestD;
                }
                if (pts.Count < 4) continue;
                var mat = s % 2 == 0 ? lavaCyan : lavaViolet;
                Strip(mk, pts, mat, 2.6f, .06f, ctx);
                for (var i = 0; i + 1 < pts.Count; i += 2)
                {
                    var d = pts[i + 1] - pts[i]; d.y = 0; if (d.sqrMagnitude < .01f) continue; var sd = Vector3.Cross(up, d.normalized);
                    foreach (var e in new[] { -1f, 1f })
                    {
                        // a crusted bank rock, seated on the lowest ground under it (on the steep flanks its downhill end
                        // used to hang a metre over the slope), with a collider: it stands knee high
                        var bk = pts[i] + sd * e * 1.8f; var lo = ctx.Ground(bk.x, bk.z);
                        foreach (var o in new[] { new Vector2(.7f, 0), new Vector2(-.7f, 0), new Vector2(0, 1.2f), new Vector2(0, -1.2f) }) lo = Mathf.Min(lo, ctx.Ground(bk.x + o.x, bk.z + o.y));
                        bk.y = lo + .12f; mk.Blob(obsidian, bk, new Vector3(.8f, .4f, 1.4f), ctx.Rng, .35f);
                        solid.Box(obsidian, bk, new Vector3(1.4f, .8f, 2.4f), Quaternion.identity);
                    }
                }
                halos.Add(pts[pts.Count / 2] + up * 1f, 4f, s % 2 == 0 ? Cyan : Violet, .25f);
                life.streams.Add(pts.ToArray());
                foreach (var p in pts) noGrass.Add(new Vector3(p.x, 3f, p.z));
            }
            noGrass.Add(new Vector3(v.x, R * .3f, v.z));
            if (life.craters.Count == 1)
            {
                var mark = new GameObject("Shot: a neon volcano").transform; mark.SetParent(ctx.Root, false);
                var away = new Vector3(center.x - v.x, 0, center.z - v.z).normalized; var cam = ctx.OnGround(v.x + away.x * R * 3f, v.z + away.z * R * 3f) + up * 1.7f; var look = top + up * 14f - cam;
                mark.SetPositionAndRotation(cam, Quaternion.LookRotation(look.normalized));
            }
        }

        // ---------------------------------------------------------------- paths
        void Paths(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var posts = new List<Vector3>(); var walk = 1;
            void Lay(Vector3 join, Vector3 to, float stopShort, string artist)
            {
                var from = new Vector3(join.x, 0, join.z); to.y = 0;
                var dir = to - from; var total = dir.magnitude; if (total < 2f) return; dir /= total;
                if (artist != null) facing[artist] = -dir;
                var side = Vector3.Cross(Vector3.up, dir); var end = total - stopShort;
                var pts = new List<Vector3>();
                for (var d = walk == 1 ? 13f : PathW / 2f + .1f; ; d += 1.5f)
                {
                    var dd = Mathf.Min(d, end); var q = from + dir * dd + side * Mathf.Sin(dd / 14f) * Mathf.Min(3f, end * .05f);
                    if (!ctx.IsLand(q.x, q.z, 0f)) break;
                    pts.Add(ctx.OnGround(q.x, q.z)); if (dd >= end) break;
                }
                if (pts.Count < 2) return;
                Strip(mk, pts, basalt, PathW, .2f, ctx);
                for (var i = 2; i < pts.Count; i += 4) foreach (var s in new[] { -1f, 1f }) { var st = pts[i] + side * s * (PathW / 2f + .15f); st.y = ctx.Ground(st.x, st.z) + .08f; mk.Box(lavaCyan, st, new Vector3(.18f, .12f, .18f), Quaternion.LookRotation(dir)); }
                foreach (var p in pts) Remember(p, walk);
                for (var i = 9; i < pts.Count; i += 15)
                {
                    var lp = pts[i] + side * ((i / 15) % 2 == 0 ? 1f : -1f) * (PathW / 2f + 1f);
                    var near = false; foreach (var o in posts) if ((o - lp).sqrMagnitude < 20f * 20f) near = true;
                    if (near) continue;
                    posts.Add(lp); NeonPost(ctx, mk, ctx.OnGround(lp.x, lp.z));
                }
                walk++;
            }
            if (hasArena) Lay(ctx.OnGround(center.x, center.z), new Vector3(arena2.x, 0, arena2.y), ArenaR - 10f, null);
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

        // ---------------------------------------------------------------- the wilds
        /// Obsidian spires with neon tips, clusters of neon crystals, steam vents, toppled amps, chain-link pens, glowing
        /// cracks, cinder boulders, drifts of pale ash.
        void Wilds(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            int spires = 0, crystals = 0, vents = 0, amps = 0, pens = 0, cracks = 0, rocks = 0, drifts = 0;
            const float cell = 21f;
            for (var x = ctx.MinX; x < ctx.MaxX; x += cell)
            for (var z = ctx.MinZ; z < ctx.MaxZ; z += cell)
            {
                float px = x + ctx.Range(0f, cell), pz = z + ctx.Range(0f, cell);
                if (!Open(ctx, px, pz, 3f)) continue;
                var p = ctx.OnGround(px, pz); var roll = ctx.Range(0f, 1f); var steep = OnVolcano(px, pz, -10f);
                if (roll < .14f) { Spire(ctx, mk, solid, p); spires++; }
                else if (roll < .24f) { Crystals(ctx, mk, solid, p); crystals++; }
                else if (roll < .29f && !steep) { Vent(ctx, mk, p); vents++; }
                else if (roll < .32f && !steep && amps < 14) { ToppledAmps(ctx, mk, solid, p); amps++; }
                else if (roll < .36f && !steep && pens < 14) { Pen(ctx, mk, solid, p); pens++; }
                else if (roll < .5f) { var dir = Quaternion.Euler(0, ctx.Range(0, 180f), 0) * Vector3.forward; var q = p; var m = ctx.Chance(.5f) ? crackM : lavaCyan; for (var k = 0; k < 5; k++) { var nd = Quaternion.Euler(0, ctx.Range(-35f, 35f), 0) * dir; var r = ctx.OnGround(q.x + nd.x * 2.4f, q.z + nd.z * 2.4f); mk.Box(m, (q + r) / 2f + Vector3.up * .04f, new Vector3(.16f, .05f, 2.5f), Quaternion.LookRotation(r - q)); q = r; } cracks++; }
                else if (roll < .62f)
                {
                    // seated on the lowest ground under it, so on a slope it sinks into the hill instead of overhanging
                    // the downhill side (from above, those read as black shapes floating over the ash)
                    var s = ctx.Range(1f, 2.8f); var lo = p.y;
                    for (var k = 0; k < 6; k++) { var a = k * Mathf.PI / 3f; lo = Mathf.Min(lo, ctx.Ground(px + Mathf.Cos(a) * s * .9f, pz + Mathf.Sin(a) * s * .9f)); }
                    p.y = lo; mk.Blob(ctx.Chance(.5f) ? basalt : obsidian, p + Vector3.up * s * .35f, new Vector3(s, s * .7f, s * .9f), ctx.Rng, .3f); BoxCol(ctx.Root, p + Vector3.up * s * .35f, new Vector3(s * 1.9f, s * 1.3f, s * 1.7f), Quaternion.identity); rocks++; }
                else if (roll < .72f)
                {
                    // a low drift of ash, only where the ground is gentle and seated on the lowest ground under it (on the
                    // slopes they stood out as pale slabs hanging over the hillside, a ledge to walk off)
                    float sx = ctx.Range(2f, 4f), sz = ctx.Range(2f, 4f), lo = p.y, hi = p.y;
                    for (var k = 0; k < 8; k++) { var a = k * Mathf.PI / 4f; var h = ctx.Ground(px + Mathf.Cos(a) * sx, pz + Mathf.Sin(a) * sz); lo = Mathf.Min(lo, h); hi = Mathf.Max(hi, h); }
                    if (hi - lo < .7f) { p.y = lo; mk.Blob(ash, p - Vector3.up * .08f, new Vector3(sx, ctx.Range(.15f, .28f), sz), ctx.Rng, .2f); drifts++; }
                }
            }
            Debug.Log($"Metalcore Caldera: arena {(hasArena ? "yes" : "no")}, {volcanoes.Count} volcanoes, {spires} spires, {crystals} crystal clusters, {vents} vents, {amps} toppled amps, {pens} pens, {cracks} cracks, {rocks} rocks, {drifts} ash drifts");
        }

        /// An obsidian spire: a tall faceted black blade, leaning, with a neon tip.
        void Spire(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 at)
        {
            var h = ctx.Range(5f, 14f); var tilt = Quaternion.Euler(ctx.Range(-10f, 10f), ctx.Range(0, 360f), ctx.Range(-10f, 10f)); var dir = tilt * Vector3.up;
            var tw = ctx.Range(0, 60f); mk.Prism(obsidian, at - Vector3.up * .5f, dir, h * .14f, h, 5, .08f, tw);
            solid.Prism(obsidian, at - Vector3.up * .5f, dir, h * .14f, h, 5, .08f, tw);   // the collider leans with the blade
            if (ctx.Chance(.6f))
            {
                var bs = at - Vector3.up * .3f + tilt * new Vector3(h * .12f, 0, 0); var bd = (dir + tilt * Vector3.right * .3f).normalized;
                mk.Prism(obsidian, bs, bd, h * .07f, h * .5f, 5, .1f); solid.Prism(obsidian, bs, bd, h * .07f, h * .5f, 5, .1f);
            }
            var tip = at + dir * (h - .6f); var col = ctx.Chance(.5f) ? Cyan : Violet;
            mk.Prism(GlowM(col, 2.6f), tip - dir * .3f, dir, h * .03f, .9f, 5, .1f); halos.Add(tip, 1.2f, col, .5f);
        }

        /// A cluster of neon crystals growing out of the ash.
        void Crystals(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 at)
        {
            var col = ctx.Chance(.5f) ? Cyan : (ctx.Chance(.5f) ? Violet : Magenta); var m = GlowM(col, 1.3f);
            var n = ctx.Rng.Next(4, 9);
            for (var k = 0; k < n; k++)
            {
                var d = (Quaternion.Euler(ctx.Range(-35f, 35f), ctx.Range(0, 360f), 0) * Vector3.up).normalized; var len = ctx.Range(.8f, 2.6f);
                mk.Prism(m, at + new Vector3(ctx.Range(-.6f, .6f), -.2f, ctx.Range(-.6f, .6f)), d, len * .18f, len, 6, .05f);
            }
            halos.Add(at + Vector3.up * 1f, 2f, col, .4f);
            BoxCol(ctx.Root, at + Vector3.up * .8f, new Vector3(2.6f, 1.6f, 2.6f), Quaternion.identity);
        }

        /// A chain-link pen: four posts and fencing round a patch of ash, the gate hanging open.
        void Pen(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 at)
        {
            var up = Vector3.up; var rot = Quaternion.Euler(0, ctx.Range(0, 90f), 0); var s = ctx.Range(2.5f, 4f);
            var corners = new[] { new Vector3(-s, 0, -s), new Vector3(s, 0, -s), new Vector3(s, 0, s), new Vector3(-s, 0, s) };
            for (var k = 0; k < 4; k++)
            {
                var a = at + rot * corners[k]; var b = at + rot * corners[(k + 1) % 4]; a.y = ctx.Ground(a.x, a.z); b.y = ctx.Ground(b.x, b.z);
                mk.Prism(steel, a - up * .2f, up, .06f, 2.4f, 6);
                if (k == 0) continue;   // the open side
                var mid = (a + b) / 2f; var len = Vector3.Distance(a, b);
                foreach (var y in new[] { .2f, 1.1f, 2.1f }) mk.Box(chain, mid + up * y, new Vector3(.03f, .03f, len), Quaternion.LookRotation(b - a));
                mk.Box(WorldMaterials.Solid(new Color(.35f, .36f, .4f), .6f), mid + up * 1.15f, new Vector3(.01f, 1.9f, len), Quaternion.LookRotation(b - a));
                solid.Box(chain, mid + up * 1.1f, new Vector3(.2f, 2.2f, len), Quaternion.LookRotation(b - a));
            }
        }

        void AshTufts(LandContext ctx)
        {
            var mat = WindMesh.Material(.2f, 1f, new Color(.4f, .4f, .42f));
            var meshes = new Dictionary<(int, int), WindMesh>(); var rng = new System.Random(2004);
            const float st = 3.4f;
            for (var x = ctx.MinX; x < ctx.MaxX; x += st)
            for (var z = ctx.MinZ; z < ctx.MaxZ; z += st)
            {
                var px = x + ((float)rng.NextDouble() - .5f) * st; var pz = z + ((float)rng.NextDouble() - .5f) * st;
                if (!ctx.IsLand(px, pz, 1f) || NearPath(px, pz, 2.6f) || InNoGrass(px, pz, 0f) || InFootprint(ctx, px, pz, -2f) || OnVolcano(px, pz, -20f)) continue;
                if (hasArena && Vector2.Distance(new Vector2(px, pz), arena2) < ArenaR + 6f) continue;
                if (Mathf.PerlinNoise(px * .04f + 1f, pz * .04f + 4f) < .55f) continue;
                var key = (Mathf.FloorToInt(px / 70f), Mathf.FloorToInt(pz / 70f));
                if (!meshes.TryGetValue(key, out var wm)) meshes[key] = wm = new WindMesh();
                var col = Color.Lerp(new Color(.3f, .3f, .33f), new Color(.55f, .55f, .58f), (float)rng.NextDouble());
                wm.Tuft(new Vector3(px, ctx.Ground(px, pz) - .04f, pz), .3f + (float)rng.NextDouble() * .35f, col, Color.Lerp(col, new Color(.75f, .72f, .9f), .3f), rng, 3, .05f);
            }
            foreach (var kv in meshes) kv.Value.Build("Caldera dead grass", ctx.Root, mat);
        }
    }
}
