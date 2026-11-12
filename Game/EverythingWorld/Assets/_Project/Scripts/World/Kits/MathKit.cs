using System.Collections.Generic;
using UnityEngine;

namespace EverythingWorld.World
{
    /// The Math Rock Escarpment (WORLD_GUIDE.md): crisp autumn, long shadows, quiet between bursts of noise. One long
    /// escarpment runs across the land and splits it in two. Above it, where you arrive, the math rock side: warm and
    /// busy, red and gold trees, string lights, wind chimes, little wooden cabins with lit windows, benches and bikes.
    /// Below it, out toward the sea, the post-rock side: wide and quiet, pale tall grass, lone trees, standing stones,
    /// mist, and MONO at its heart. Streams run off the high ground and pour over the escarpment in waterfalls; by the
    /// biggest one a lone wooden house stands at the cliff edge. Reads by a clear blue sky against orange and gold
    /// leaves (blue/yellow for the colourblind), unlike the dark sunset Peaks and the pale ice Spires next door.
    /// Files: MathKit (ground, arrival, streams and falls, paths, wilds), MathArtists, MathMore, MathSongIds, MathLife.
    public partial class MathKit : LandKit
    {
        public override string Land => "The Math Rock Escarpment";
        public override Vector3 ArtistFront(IslandWorldData data, IslandWorldData.Artist a) => facing.TryGetValue(a.name, out var f) ? f : base.ArtistFront(data, a);

        public static readonly Color Warm = new Color(1f, .78f, .45f), Leaf = new Color(.95f, .5f, .15f), Gold = new Color(1f, .8f, .3f), Mist = new Color(.85f, .9f, 1f), Ink = new Color(.18f, .14f, .1f), Water = new Color(.6f, .85f, 1f);
        static readonly Color[] LeafC = { new Color(.92f, .45f, .12f), new Color(.78f, .2f, .12f), new Color(.95f, .72f, .2f), new Color(.85f, .55f, .15f), new Color(.6f, .55f, .2f) };
        const float PathW = 2.8f, ScarpDrop = 16f, ScarpW = 24f;

        Material wood, woodDark, stone, bark, leafPile, metal, waterM, foamM, fallM, bulbM, lanternM, windowM, grassPale, cloth;
        GlowHalos halos; Vector3 center; MathLife life;
        readonly Dictionary<(int, int), List<Vector3>> pathCells = new Dictionary<(int, int), List<Vector3>>();
        readonly List<(Vector3 p, int w)> walkPoints = new List<(Vector3, int)>();
        readonly Dictionary<string, Vector3> facing = new Dictionary<string, Vector3>();
        readonly List<Vector3> noGrass = new List<Vector3>();
        readonly List<Vector3> waterPts = new List<Vector3>();
        Vector2 heart2, sea2; float scarpT = 70f; bool hasScarp;
        readonly System.Random bulbRng = new System.Random(11);

        void MakeMaterials()
        {
            wood = WorldMaterials.Solid(new Color(.55f, .38f, .24f)); woodDark = WorldMaterials.Solid(new Color(.3f, .2f, .14f));
            stone = WorldMaterials.Solid(new Color(.55f, .53f, .5f)); bark = WorldMaterials.Solid(new Color(.28f, .2f, .16f));
            leafPile = WorldMaterials.Solid(new Color(.85f, .45f, .15f)); metal = WorldMaterials.Solid(new Color(.7f, .72f, .75f), .85f);
            waterM = new Material(WorldMaterials.Glow(new Color(.35f, .6f, .8f), .25f)) { name = "Stream" };
            foamM = new Material(WorldMaterials.Glow(new Color(.92f, .96f, 1f), .7f)) { name = "Foam" };
            fallM = new Material(WorldMaterials.Glow(new Color(.85f, .93f, 1f), .9f)) { name = "Falls" };
            bulbM = new Material(WorldMaterials.Glow(Warm, 2.2f)) { name = "String lights" };
            lanternM = new Material(WorldMaterials.Glow(Warm, 1.6f)) { name = "Lanterns" };
            windowM = new Material(WorldMaterials.Glow(new Color(1f, .75f, .42f), 1.6f)) { name = "Windows" };
            grassPale = WorldMaterials.Solid(new Color(.78f, .72f, .52f)); cloth = WorldMaterials.Solid(new Color(.9f, .87f, .8f));
        }

        Material GlowM(Color c, float s = 2f) => WorldMaterials.Glow(c, s * .65f);
        static bool HasStation(IslandWorldData.Artist a) => a.stationId != 0;
        static readonly HashSet<string> PostRock = new HashSet<string> { "MONO", "This Will Destroy You", "Explosions In The Sky", "Mogwai", "God Is An Astronaut", "65daysofstatic", "Do Make Say Think", "Saxon Shore", "Infinity Shred" };
        static float Foot(IslandWorldData.Artist a)
        {
            switch (a.name)
            {
                case "MONO": return 18f;
                case "tricot": return 15f;
                case "toe": return 13f;
                default: return HasStation(a) ? 12f : 8.5f;
            }
        }

        public override bool Ground(float height, float slope, float patch, out Color color)
        {
            if (height < 2.6f) color = Color.Lerp(new Color(.6f, .56f, .48f), new Color(.68f, .63f, .54f), patch);                          // the shore
            else if (slope > 28f) color = Color.Lerp(new Color(.42f, .38f, .34f), new Color(.52f, .48f, .42f), patch);                      // the cliff
            else color = Color.Lerp(new Color(.52f, .56f, .3f), new Color(.74f, .62f, .32f), Mathf.SmoothStep(0f, 1f, patch));             // autumn grass
            color.a = 1f;
            return true;
        }

        static void BoxCol(Transform parent, Vector3 center, Vector3 size, Quaternion rot)
        {
            var go = new GameObject("Box collider"); go.transform.SetParent(parent, false); go.transform.SetPositionAndRotation(center, rot);
            go.AddComponent<BoxCollider>().size = size; go.isStatic = true;
        }

        static float Ease(float a, float b, float x) { var t = Mathf.Clamp01((x - a) / (b - a)); return t * t * (3f - 2f * t); }
        float SeaT(float x, float z) => Vector2.Dot(new Vector2(x, z) - heart2, sea2);
        bool Below(float x, float z) => hasScarp && SeaT(x, z) > scarpT + ScarpW * .5f;

        // ---------------------------------------------------------------- shaping the ground
        public override void ShapeGround(IslandWorldData data, int index)
        {
            var isl = data.layout.islands[index]; heart2 = new Vector2(isl.labelX, isl.labelZ);
            // the way to the nearest open sea
            var bestS = float.MaxValue; sea2 = Vector2.left;
            for (var k = 0; k < 32; k++)
            {
                var a = k * Mathf.PI / 16f; var d = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                for (var s = 20f; s < 700f; s += 10f) { var p = heart2 + d * s; var id = data.IslandAt(p.x, p.y); if (id == isl) continue; if (data.HeightAt(p.x, p.y) < data.layout.waterLevel && s < bestS) { bestS = s; sea2 = d; } break; }
            }
            hasScarp = bestS < 700f;
            scarpT = Mathf.Clamp(bestS * .25f, 55f, 95f);
            // the escarpment: everything seaward of the line lowered, the drop easing over the cliff, fading out near
            // other lands so no cliff opens at a border
            if (hasScarp)
            {
                for (var r = 0; r < data.Nz; r++)
                {
                    var z = data.layout.depth - r * data.Step;
                    for (var col = 0; col < data.Nx; col++)
                    {
                        var i = r * data.Nx + col; if (data.islandIds[i] != index) continue;
                        var x = col * data.Step; var t = SeaT(x, z); if (t < scarpT - ScarpW * .5f) continue;
                        var w = Ease(scarpT - ScarpW * .5f, scarpT + ScarpW * .5f, t);
                        var border = 1f;
                        for (var k = 0; k < 12 && border > 0f; k++) { var a = k * Mathf.PI / 6f; foreach (var m in new[] { 20f, 40f }) { var o = data.IslandAt(x + Mathf.Cos(a) * m, z + Mathf.Sin(a) * m); if (o != null && o != isl) border = Mathf.Min(border, (m - 20f) / 20f); } }
                        w *= border; if (w <= 0f) continue;
                        var h = data.heights[i]; data.heights[i] = Mathf.Max(h - ScarpDrop * w, Mathf.Min(h, 2.8f));
                    }
                }
            }
            // MONO below the escarpment, tricot and toe above it; the rest settle, then trade places so the post-rock
            // bands are below and the math rock bands above
            var keep = new List<string>();
            bool Inside(Vector2 p, float m) { if (data.IslandAt(p.x, p.y) != isl) return false; for (var k = 0; k < 16; k++) { var a = k * Mathf.PI / 8f; if (data.IslandAt(p.x + Mathf.Cos(a) * m, p.y + Mathf.Sin(a) * m) != isl || data.HeightAt(p.x + Mathf.Cos(a) * m, p.y + Mathf.Sin(a) * m) < 2f) return false; } return true; }
            var across = new Vector2(-sea2.y, sea2.x);
            foreach (var a in data.layout.artists)
            {
                if (a.sceneId != isl.sceneId) continue;
                Vector2[] tries = a.name == "MONO" ? new[] { sea2 * (scarpT + 60f), sea2 * (scarpT + 50f) + across * 40f, sea2 * (scarpT + 50f) - across * 40f, sea2 * (scarpT + 80f) }
                    : a.name == "tricot" ? new[] { -sea2 * 40f + across * 50f, -sea2 * 60f + across * 30f, across * 70f, -sea2 * 40f - across * 50f }
                    : a.name == "toe" ? new[] { -sea2 * 45f - across * 55f, -sea2 * 70f - across * 25f, -across * 75f, -sea2 * 90f } : null;
                if (tries == null) continue;
                foreach (var o in tries) { var p = heart2 + o; if (Inside(p, Foot(a) + 30f)) { a.x = p.x; a.z = p.y; keep.Add(a.name); break; } }
            }
            // the rest of the post-rock bands below the escarpment too, gathered near MONO on the open low ground
            var avoidBelow = new List<Vector3>();
            if (hasScarp)
            {
                var mono = heart2 + sea2 * (scarpT + 60f);
                foreach (var a in data.layout.artists) if (a.sceneId == isl.sceneId && a.name == "MONO") mono = new Vector2(a.x, a.z);
                var cands = new List<Vector2>();
                for (var t = scarpT + 34f; t < scarpT + 300f; t += 12f)
                    for (var u = -420f; u <= 420f; u += 12f) cands.Add(heart2 + sea2 * t + across * u);
                cands.Sort((p, q) => Vector2.Distance(p, mono).CompareTo(Vector2.Distance(q, mono)));
                var placedBelow = new List<IslandWorldData.Artist>();
                foreach (var a in data.layout.artists) if (a.sceneId == isl.sceneId && keep.Contains(a.name)) placedBelow.Add(a);
                foreach (var a in data.layout.artists)
                {
                    if (a.sceneId != isl.sceneId || !PostRock.Contains(a.name) || keep.Contains(a.name)) continue;
                    foreach (var p in cands)
                    {
                        if (!Inside(p, Foot(a) + 12f)) continue;
                        var ok = true; foreach (var o in placedBelow) if (Vector2.Distance(p, new Vector2(o.x, o.z)) < Foot(a) + Foot(o) + 16f) { ok = false; break; }
                        if (!ok) continue;
                        a.x = p.x; a.z = p.y; keep.Add(a.name); placedBelow.Add(a); break;
                    }
                }
                // keep everyone else above the escarpment
                for (var t = scarpT + 20f; t < scarpT + 400f; t += 40f)
                    for (var u = -440f; u <= 440f; u += 40f) { var p = heart2 + sea2 * t + across * u; if (data.IslandAt(p.x, p.y) == isl) avoidBelow.Add(new Vector3(p.x, 26f, p.y)); }
            }
            ArtistPlacement.Spread(data, index, 48f, heart2, 46f, 38f, avoidBelow, keep, Land, 400f);
            if (hasScarp)
            {
                var arts = new List<IslandWorldData.Artist>(); foreach (var a in data.layout.artists) if (a.sceneId == isl.sceneId && !keep.Contains(a.name)) arts.Add(a);
                for (var pass = 0; pass < 3; pass++)
                    foreach (var p in arts)
                    {
                        var wantBelow = PostRock.Contains(p.name); if (Below(p.x, p.z) == wantBelow) continue;
                        IslandWorldData.Artist best = null; var bd = float.MaxValue;
                        foreach (var q in arts) { if (q == p || PostRock.Contains(q.name) == wantBelow || Below(q.x, q.z) != wantBelow) continue; var dd = (p.x - q.x) * (p.x - q.x) + (p.z - q.z) * (p.z - q.z); if (dd < bd) { bd = dd; best = q; } }
                        if (best != null) { (p.x, best.x) = (best.x, p.x); (p.z, best.z) = (best.z, p.z); }
                    }
            }
            foreach (var a in data.layout.artists)
            {
                if (a.sceneId != isl.sceneId) continue;
                a.r = Mathf.Max(a.r, Foot(a) + 6f);
                Level(data, index, new Vector2(a.x, a.z), Foot(a) + 1f, Foot(a) + 10f);
            }
            Level(data, index, heart2, 16f, 28f);
        }

        void Level(IslandWorldData data, int index, Vector2 c, float flat, float blend)
        {
            var level = Mathf.Max(data.HeightAt(c.x, c.y), data.layout.waterLevel + 2.4f);
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
            pathCells.Clear(); walkPoints.Clear(); facing.Clear(); noGrass.Clear(); waterPts.Clear();
            center = ctx.Plaza;
            life = new GameObject("Escarpment life").AddComponent<MathLife>();
            life.transform.SetParent(ctx.Root, false);
            life.center = center; life.bulbs = bulbM; life.lanterns = lanternM; life.windows = windowM; life.falls = fallM; life.foam = foamM; life.seaDir = new Vector3(sea2.x, 0, sea2.y);
            var mk = new MeshKit(); var solid = new MeshKit();
            Remember(ctx.OnGround(center.x, center.z), 0);
            Arrival(ctx, mk, solid);
            Streams(ctx, mk, solid);
            Paths(ctx, mk, solid);
            Wilds(ctx, mk, solid);
            mk.Build("The Math Rock Escarpment", ctx.Root, false);
            solid.BuildCollider("The Math Rock Escarpment walls", ctx.Root);
        }

        public override void Finish(LandContext ctx) { Grasses(ctx); halos.Build("Escarpment glow", ctx.Root); }

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

        bool NearWater(float x, float z, float within) { foreach (var p in waterPts) { float dx = p.x - x, dz = p.z - z; if (dx * dx + dz * dz < within * within) return true; } return false; }

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
            ctx.IsLand(x, z, 4f) && !InFootprint(ctx, x, z, pad) && !NearPath(x, z, pad + 2f) && !InNoGrass(x, z, pad) && !NearWater(x, z, pad + 3f)
            && Vector2.Distance(new Vector2(x, z), new Vector2(center.x, center.z)) > 20f + pad;

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
                    L[i].y = Mathf.Max(gl, top - .15f) + lift; R[i].y = Mathf.Max(gr, top - .15f) + lift;
                }
            }
            for (var i = 0; i + 1 < n; i++) mk.Quad(m, L[i], R[i], R[i + 1], L[i + 1], (pts[i] + pts[i + 1]) / 2f - Vector3.up);
        }

        /// A lantern on a wooden post: a little glass box with a warm light.
        void Lantern(LandContext ctx, MeshKit mk, Vector3 g, float h = 2.7f)
        {
            var up = Vector3.up;
            mk.Box(woodDark, g + up * (h / 2f - .1f), new Vector3(.14f, h + .2f, .14f), Quaternion.identity);
            mk.Box(woodDark, g + up * h + new Vector3(.25f, 0, 0), new Vector3(.6f, .08f, .08f), Quaternion.identity);
            mk.Box(lanternM, g + up * (h - .35f) + new Vector3(.45f, 0, 0), new Vector3(.24f, .32f, .24f), Quaternion.identity);
            halos.Add(g + up * (h - .35f) + new Vector3(.45f, 0, 0), 1.5f, Warm, .6f); life.lanternAt.Add(g + up * (h - .35f));
            ArtistBits.PostCollider(ctx.Root, g, h, .12f);
            if (h < 2.5f) BoxCol(ctx.Root, g + up * (h - .35f) + new Vector3(.3f, 0, 0), new Vector3(.8f, .5f, .35f), Quaternion.identity);
        }

        /// A wind chime hanging from a little gallows post: five metal tubes and a paper tail (MathLife sways it).
        void WindChime(LandContext ctx, MeshKit mk, Vector3 g, Material glow = null)
        {
            var up = Vector3.up; var dir = Quaternion.Euler(0, ctx.Range(0, 360f), 0) * Vector3.forward;
            mk.Box(woodDark, g + up * 1.7f, new Vector3(.12f, 3.4f, .12f), Quaternion.identity);
            mk.Box(woodDark, g + up * 3.35f + dir * .4f, new Vector3(.08f, .08f, .9f), Quaternion.LookRotation(dir));
            ArtistBits.PostCollider(ctx.Root, g, 3.2f, .12f);
            var ck = new MeshKit();
            ck.Prism(wood, -up * .05f, up, .16f, .05f, 10);
            for (var k = 0; k < 5; k++) { var a = k * Mathf.PI * 2f / 5f; ck.Prism(glow ?? metal, new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * .11f - up * (.15f + .45f - k * .06f), up, .025f, .45f - k * .06f + .1f, 6); }
            ck.Box(cloth, -up * .85f, new Vector3(.12f, .3f, .01f), Quaternion.identity);
            var go = ck.BuildSingle("Wind chime", life.transform, false);
            if (go != null) { go.transform.position = g + up * 3.3f + dir * .8f; life.chimes.Add(go.transform); }
        }

        /// A string of warm bulbs sagging between two points.
        void StringLights(MeshKit mk, Vector3 a, Vector3 b, float sag = .8f)
        {
            var n = Mathf.Max(5, Mathf.RoundToInt(Vector3.Distance(a, b) / .8f));
            for (var j = 0; j <= n; j++) { var t = j / (float)n; var p = Vector3.Lerp(a, b, t) - Vector3.up * Mathf.Sin(t * Mathf.PI) * sag; mk.Blob(bulbM, p, Vector3.one * .07f, bulbRng, 0f); }
            for (var j = 0; j < n; j++) { float t0 = j / (float)n, t1 = (j + 1) / (float)n; var p0 = Vector3.Lerp(a, b, t0) - Vector3.up * Mathf.Sin(t0 * Mathf.PI) * sag + Vector3.up * .06f; var p1 = Vector3.Lerp(a, b, t1) - Vector3.up * Mathf.Sin(t1 * Mathf.PI) * sag + Vector3.up * .06f; mk.Box(woodDark, (p0 + p1) / 2f, new Vector3(.015f, .015f, Vector3.Distance(p0, p1)), Quaternion.LookRotation(p1 - p0)); }
            halos.Add(Vector3.Lerp(a, b, .5f) - Vector3.up * sag, Vector3.Distance(a, b) * .35f + .5f, Warm, .3f);
        }

        /// An autumn tree: a trunk and a few round crowns in reds, oranges and golds, leaves fallen round its foot.
        void AutumnTree(LandContext ctx, MeshKit mk, Vector3 at, float h)
        {
            var up = Vector3.up; var col = LeafC[ctx.Rng.Next(LeafC.Length)];
            mk.Prism(bark, at - up * .3f, (up + new Vector3(ctx.Range(-.1f, .1f), 0, ctx.Range(-.1f, .1f))).normalized, h * .05f + .1f, h * .6f, 6, .7f);
            var m = WorldMaterials.Solid(col);
            for (var k = 0; k < 4; k++) mk.Blob(k == 3 ? WorldMaterials.Solid(LeafC[(ctx.Rng.Next(LeafC.Length))]) : m, at + up * h * (.74f + k * .06f) + new Vector3(ctx.Range(-1f, 1f), 0, ctx.Range(-1f, 1f)) * h * .12f, Vector3.one * h * ctx.Range(.16f, .21f), ctx.Rng, .3f);
            var fallen = WorldMaterials.Solid(Color.Lerp(col, new Color(.5f, .3f, .15f), .3f));
            for (var k = 0; k < 6; k++) { var a = k * Mathf.PI / 3f + ctx.Range(0f, .8f); var r = ctx.Range(.5f, h * .3f); var q = ctx.OnGround(at.x + Mathf.Cos(a) * r, at.z + Mathf.Sin(a) * r); mk.Blob(fallen, q + up * .01f, new Vector3(ctx.Range(.4f, .8f), .03f, ctx.Range(.4f, .8f)), ctx.Rng, .3f); }
            ArtistBits.PostCollider(ctx.Root, at, h * .45f, h * .05f + .15f);
        }

        // ---------------------------------------------------------------- arrival
        /// Where you arrive: a round wooden deck at the top of the land, string lights strung across it from four
        /// posts, wind chimes, a sign carved in a slab of wood, and the escarpment's edge just ahead with the sea and
        /// the falls beyond.
        void Arrival(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var c = ctx.OnGround(center.x, center.z); var toSea = new Vector3(sea2.x, 0, sea2.y); var sd = Vector3.Cross(up, toSea);
            mk.Prism(wood, c - up * .3f, up, 11f, .5f, 20); solid.Prism(wood, c - up * .3f, up, 11f, .5f, 20);
            for (var k = 0; k < 20; k++) { var a = k * Mathf.PI / 10f; mk.Box(woodDark, c + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * 5.5f + up * .21f, new Vector3(.06f, .02f, 11f), Quaternion.Euler(0, 90f - a * Mathf.Rad2Deg, 0)); }
            var posts = new Vector3[4];
            for (var k = 0; k < 4; k++) { var a = k * Mathf.PI / 2f + Mathf.PI / 4f; var g = ctx.OnGround(c.x + Mathf.Cos(a) * 11.5f, c.z + Mathf.Sin(a) * 11.5f); mk.Box(woodDark, g + up * 2.4f, new Vector3(.22f, 5f, .22f), Quaternion.identity); ArtistBits.PostCollider(ctx.Root, g, 4.8f, .15f); posts[k] = g + up * 4.8f; }
            for (var k = 0; k < 4; k++) StringLights(mk, posts[k], posts[(k + 2) % 4 == k ? (k + 1) % 4 : (k + 1) % 4], 1f);
            StringLights(mk, posts[0], posts[2], 1.6f); StringLights(mk, posts[1], posts[3], 1.6f);
            foreach (var e in new[] { -1f, 1f }) WindChime(ctx, mk, ctx.OnGround(c.x + sd.x * e * 13.5f, c.z + sd.z * e * 13.5f));
            var s = c - toSea * 15f; s.y = ctx.Ground(s.x, s.z); var rot = Quaternion.LookRotation(toSea);
            mk.Box(wood, s + up * 1.6f, new Vector3(9f, 1.6f, .3f), rot); solid.Box(wood, s + up * 1.6f, new Vector3(9f, 1.6f, .4f), rot);
            foreach (var e in new[] { -1f, 1f }) mk.Box(woodDark, s + sd * e * 4.2f + up * 1.1f, new Vector3(.25f, 2.6f, .25f), rot);
            ArtistBits.Label(ctx.Root, "THE MATH ROCK ESCARPMENT", s + up * 1.62f + toSea * .17f, toSea, .34f, Ink, TextAnchor.MiddleCenter);
            Lantern(ctx, mk, s + sd * 5.4f);
            ArtistBits.PointLight(ctx.Root, c + up * 5f, Warm, 22f, 1.6f);
            noGrass.Add(new Vector3(c.x, 14f, c.z));
            var mark = new GameObject("Shot: the arrival and the escarpment").transform; mark.SetParent(ctx.Root, false);
            var cam = c - toSea * 8f + up * 1.7f; mark.SetPositionAndRotation(cam, Quaternion.LookRotation((toSea * 100f - up * 8f).normalized));
        }

        // ---------------------------------------------------------------- streams and falls
        /// Streams from the high ground down to the sea: each walks downhill, leaning seaward so it never pools on a
        /// flat, as a strip of water with stones on its banks; where it drops steeply it becomes a fall, a sheet of white
        /// water with foam and mist at its foot. By the biggest fall, at the top of the cliff, a lone wooden house.
        void Streams(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var seaV = new Vector3(sea2.x, 0, sea2.y);
            var springs = new List<Vector3>();
            for (var tries = 0; tries < 800 && springs.Count < 5; tries++)
            {
                var x = ctx.Range(ctx.MinX, ctx.MaxX); var z = ctx.Range(ctx.MinZ, ctx.MaxZ);
                if (!ctx.IsLand(x, z, 30f) || Below(x, z) || SeaT(x, z) > scarpT - 30f || SeaT(x, z) < scarpT - 220f) continue;
                if (Vector2.Distance(new Vector2(x, z), heart2) < 30f || InFootprint(ctx, x, z, 6f)) continue;
                var far = true; foreach (var s in springs) if (Vector2.Distance(new Vector2(s.x, s.z), new Vector2(x, z)) < 70f) far = false;
                if (!far) continue;
                springs.Add(ctx.OnGround(x, z));
            }
            float bestDrop = 0f; Vector3 bestTop = default, bestDir = default;
            foreach (var spring in springs)
            {
                var pts = new List<Vector3>(); var q = new Vector2(spring.x, spring.z); var heading = sea2;
                for (var k = 0; k < 260; k++)
                {
                    var g = ctx.Ground(q.x, q.y); pts.Add(new Vector3(q.x, g, q.y));
                    if (g < ctx.Data.layout.waterLevel + .3f || !ctx.IsLand(q.x, q.y, 0f)) break;
                    Vector2 best = heading; var bh = float.MaxValue;
                    for (var j = -3; j <= 3; j++)
                    {
                        var d = (Vector2)(Quaternion.Euler(0, 0, j * 20f) * new Vector3(heading.x, heading.y, 0)); var p2 = q + d * 3f;
                        var h = ctx.Ground(p2.x, p2.y) + Mathf.Abs(j) * .08f - Vector2.Dot(d, sea2) * .15f;
                        if (InFootprint(ctx, p2.x, p2.y, 3f)) h += 50f;
                        if (h < bh) { bh = h; best = d; }
                    }
                    heading = (heading * .5f + best * .5f).normalized; q += heading * 3f;
                }
                if (pts.Count < 8) continue;
                // the stream bed, then the falls wherever it drops
                for (var i = 0; i + 1 < pts.Count; i++)
                {
                    var a = pts[i]; var b = pts[i + 1]; var drop = a.y - b.y; var d = b - a; d.y = 0; if (d.sqrMagnitude < .01f) continue; var sd = Vector3.Cross(up, d.normalized);
                    if (drop > 1.1f)
                    {
                        var aw = a + up * .1f; var bw = b + up * .1f;
                        mk.Quad(fallM, aw - sd * 1.4f, aw + sd * 1.4f, bw + sd * 1.6f, bw - sd * 1.6f, (aw + bw) / 2f - up + d.normalized);
                        mk.Quad(fallM, aw - sd * 1.4f, bw - sd * 1.6f, bw + sd * 1.6f, aw + sd * 1.4f, (aw + bw) / 2f + up - d.normalized);
                        life.fallFoot.Add(b + up * .3f);
                        // track the whole run of falls as one drop
                        var run = drop; var j2 = i + 1; while (j2 + 1 < pts.Count && pts[j2].y - pts[j2 + 1].y > 1.1f) { run += pts[j2].y - pts[j2 + 1].y; j2++; }
                        if (run > bestDrop) { bestDrop = run; bestTop = a; bestDir = d.normalized; }
                    }
                    else mk.Quad(waterM, a + up * .09f - sd * 1.3f, a + up * .09f + sd * 1.3f, b + up * .09f + sd * 1.3f, b + up * .09f - sd * 1.3f, (a + b) / 2f - up);
                    if (i % 3 == 0) foreach (var e in new[] { -1f, 1f }) { var st = a + sd * e * ctx.Range(1.5f, 2f); mk.Blob(stone, ctx.OnGround(st.x, st.z) + up * .1f, new Vector3(ctx.Range(.3f, .6f), ctx.Range(.2f, .35f), ctx.Range(.3f, .6f)), ctx.Rng, .3f); }
                    waterPts.Add(a);
                }
                // a spring pool at the top
                mk.Prism(waterM, spring + up * .08f, up, 2.4f, .02f, 12); mk.Prism(stone, spring - up * .2f, up, 2.9f, .3f, 12, 1f);
                halos.Add(spring + up * .5f, 2f, Water, .25f);
            }
            if (bestDrop > 2f)
            {
                halos.Add(bestTop - up * bestDrop * .5f + bestDir * 4f, 8f, Mist, .35f);
                ArtistBits.PointLight(ctx.Root, bestTop - up * bestDrop * .4f + bestDir * 6f, new Color(.8f, .9f, 1f), 24f, 1.4f);
                life.bigFall = bestTop - up * bestDrop + bestDir * 6f;
                // the lone wooden house at the top of the cliff beside the big fall
                var sd = Vector3.Cross(up, bestDir); var hs = bestTop - bestDir * 10f + sd * 11f;
                if (ctx.IsLand(hs.x, hs.z, 6f) && !InFootprint(ctx, hs.x, hs.z, 6f)) { House(ctx, mk, solid, ctx.OnGround(hs.x, hs.z), bestDir); }
                var mark = new GameObject("Shot: the big fall and the house").transform; mark.SetParent(ctx.Root, false);
                var cam = bestTop + bestDir * 34f + sd * 14f; cam = ctx.OnGround(cam.x, cam.z) + up * 2f;
                mark.SetPositionAndRotation(cam, Quaternion.LookRotation((bestTop - up * bestDrop * .3f - cam).normalized));
            }
            Debug.Log($"The Math Rock Escarpment: escarpment {(hasScarp ? "yes" : "no")} at {scarpT:F0} m, {springs.Count} streams, {life.fallFoot.Count} fall pieces, biggest fall {bestDrop:F1} m");
        }

        /// The lone house: a small wooden house with a pitched roof and a porch, one upstairs window lit, facing the
        /// sea over the falls; a bench on the porch.
        void House(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 g, Vector3 face)
        {
            var up = Vector3.up; var rot = Quaternion.LookRotation(face); var sd = Vector3.Cross(up, face); const float W = 7f, D = 6f, H = 5.6f;
            var lo = g.y; foreach (var (x, z) in new[] { (-1f, -1f), (1f, -1f), (1f, 1f), (-1f, 1f) }) { var q = g + sd * x * W / 2f + face * z * D / 2f; lo = Mathf.Min(lo, ctx.Ground(q.x, q.z)); }
            var b = new Vector3(g.x, Mathf.Max(g.y, lo) + .4f, g.z);
            mk.Box(stone, new Vector3(b.x, (lo + b.y) / 2f - .3f, b.z), new Vector3(W + .3f, b.y - lo + .6f, D + .3f), rot);
            mk.Box(wood, b + up * H / 2f, new Vector3(W, H, D), rot); solid.Box(wood, b + up * (H / 2f - .5f), new Vector3(W, H + 1f, D), rot);
            mk.GableRoof(woodDark, b + up * H, new Vector3(D + 1f, 0, W + 1f), 2.8f, Quaternion.LookRotation(sd));
            var front = b + face * (D / 2f + .02f);
            mk.Box(windowM, front + up * 4f + sd * 1.4f, new Vector3(1.1f, 1f, .04f), rot);
            mk.Box(WorldMaterials.Solid(new Color(.15f, .13f, .12f)), front + up * 4f - sd * 1.4f, new Vector3(1.1f, 1f, .04f), rot);
            mk.Box(woodDark, front + up * 1.1f, new Vector3(1f, 2.1f, .05f), rot);
            mk.Box(wood, front + face * 1.2f + up * .1f, new Vector3(W, .2f, 2.4f), rot); solid.Box(wood, front + face * 1.2f, new Vector3(W, .4f, 2.4f), rot);
            foreach (var e in new[] { -1f, 1f }) { mk.Box(woodDark, front + face * 2.3f + sd * e * (W / 2f - .2f) + up * 1.4f, new Vector3(.16f, 2.8f, .16f), rot); ArtistBits.PostCollider(ctx.Root, front + face * 2.3f + sd * e * (W / 2f - .2f), 2.6f, .12f); }
            mk.Box(woodDark, front + face * 1.3f + up * 2.9f, new Vector3(W + .2f, .15f, 2.6f), rot * Quaternion.Euler(8f, 0, 0));
            mk.Box(wood, front + face * .7f + up * .6f + sd * 1.8f, new Vector3(1.6f, .1f, .5f), rot);
            halos.Add(front + up * 4f + sd * 1.4f + face * .8f, 1.8f, Warm, .6f);
            ArtistBits.PointLight(ctx.Root, front + face * 2f + up * 3f, Warm, 12f, 1.4f);
            noGrass.Add(new Vector3(b.x, 6f, b.z)); ctx.AddKeepout(b.x, b.z, 7f);
        }

        // ---------------------------------------------------------------- paths
        /// Paths of packed earth edged with fallen leaves, branching from the arrival to every artist; lanterns along
        /// them, and where a path crosses the escarpment a set of timber steps.
        void Paths(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var path = WorldMaterials.Solid(new Color(.5f, .4f, .3f));
            var posts = new List<Vector3>(); var walk = 1;
            void Lay(Vector3 join, Vector3 to, float stopShort, string artist)
            {
                var from = new Vector3(join.x, 0, join.z); to.y = 0;
                var dir = to - from; var total = dir.magnitude; if (total < 2f) return; dir /= total;
                if (artist != null) facing[artist] = -dir;
                var side = Vector3.Cross(Vector3.up, dir); var end = total - stopShort;
                var pts = new List<Vector3>();
                for (var d = walk == 1 ? 12f : PathW / 2f + .1f; ; d += 1.5f)
                {
                    var dd = Mathf.Min(d, end); var q = from + dir * dd + side * Mathf.Sin(dd / 12f) * Mathf.Min(3f, end * .05f);
                    if (!ctx.IsLand(q.x, q.z, 0f)) break;
                    pts.Add(ctx.OnGround(q.x, q.z)); if (dd >= end) break;
                }
                if (pts.Count < 2) return;
                Strip(mk, pts, path, PathW, .16f, ctx);
                for (var i = 1; i < pts.Count; i++)
                {
                    var rise = Mathf.Abs(pts[i].y - pts[i - 1].y);
                    if (rise > .6f)   // steep: a timber tread
                    {
                        var d = pts[i] - pts[i - 1]; d.y = 0; var sdd = Vector3.Cross(Vector3.up, d.normalized);
                        mk.Box(woodDark, (pts[i] + pts[i - 1]) / 2f + Vector3.up * .25f, new Vector3(PathW + .2f, .14f, .3f), Quaternion.LookRotation(d));
                    }
                    if (i % 2 == 0) foreach (var s in new[] { -1f, 1f }) { var lp = pts[i] + side * s * (PathW / 2f + .3f); mk.Blob(WorldMaterials.Solid(LeafC[(i + (s > 0 ? 1 : 0)) % LeafC.Length]), ctx.OnGround(lp.x, lp.z) + Vector3.up * .03f, new Vector3(.4f, .04f, .35f), ctx.Rng, .4f); }
                }
                foreach (var p in pts) Remember(p, walk);
                for (var i = 7; i < pts.Count; i += 12)
                {
                    var lp = pts[i] + side * ((i / 12) % 2 == 0 ? 1f : -1f) * (PathW / 2f + .8f);
                    var near = false; foreach (var o in posts) if ((o - lp).sqrMagnitude < 15f * 15f) near = true;
                    if (near || InFootprint(ctx, lp.x, lp.z, 1f) || NearWater(lp.x, lp.z, 2.5f)) continue;
                    posts.Add(lp); Lantern(ctx, mk, ctx.OnGround(lp.x, lp.z));
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

        // ---------------------------------------------------------------- the wilds
        /// Above the escarpment: autumn trees in clumps, leaf piles, benches with a lantern, wind chimes, little sheds
        /// with lit windows, string lights between trees, bikes leaning on posts. Below it: tall pale grass, lone bare
        /// trees, standing stones, benches facing the sea, piles of driftwood.
        void Wilds(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            int trees = 0, benches = 0, chimes = 0, sheds = 0, strings = 0, bikes = 0, stones = 0, lone = 0, leaves = 0;
            const float cell = 17f;
            for (var x = ctx.MinX; x < ctx.MaxX; x += cell)
            for (var z = ctx.MinZ; z < ctx.MaxZ; z += cell)
            {
                float px = x + ctx.Range(0f, cell), pz = z + ctx.Range(0f, cell);
                if (!Open(ctx, px, pz, 3f)) continue;
                var p = ctx.OnGround(px, pz); var roll = ctx.Range(0f, 1f); var below = Below(px, pz);
                var toSea = new Vector3(sea2.x, 0, sea2.y);
                if (!below)
                {
                    if (roll < .34f) { var n = ctx.Rng.Next(1, 4); for (var k = 0; k < n; k++) { var q = ctx.OnGround(px + ctx.Range(-4f, 4f), pz + ctx.Range(-4f, 4f)); if (!NearPath(q.x, q.z, 2.5f)) AutumnTree(ctx, mk, q, ctx.Range(5f, 9f)); } trees++; }
                    else if (roll < .42f) { mk.Blob(leafPile, p, new Vector3(ctx.Range(1f, 2f), .3f, ctx.Range(1f, 2f)), ctx.Rng, .3f); leaves++; }
                    else if (roll < .47f) { Bench(ctx, mk, p, Quaternion.Euler(0, ctx.Range(0, 360f), 0) * Vector3.forward, true); benches++; }
                    else if (roll < .51f) { WindChime(ctx, mk, p); chimes++; }
                    else if (roll < .545f && sheds < 14) { Shed(ctx, mk, solid, p); sheds++; }
                    else if (roll < .6f)
                    {
                        var d = Quaternion.Euler(0, ctx.Range(0, 180f), 0) * Vector3.forward; var a = ctx.OnGround(px - d.x * 4f, pz - d.z * 4f); var b = ctx.OnGround(px + d.x * 4f, pz + d.z * 4f);
                        foreach (var e in new[] { a, b }) { mk.Box(woodDark, e + Vector3.up * 1.6f, new Vector3(.12f, 3.2f, .12f), Quaternion.identity); ArtistBits.PostCollider(ctx.Root, e, 3f, .1f); }
                        StringLights(mk, a + Vector3.up * 3.1f, b + Vector3.up * 3.1f, .6f); strings++;
                    }
                    else if (roll < .62f) { Bike(ctx, mk, p); bikes++; }
                }
                else
                {
                    if (roll < .05f) { LoneTree(ctx, mk, p); lone++; }
                    else if (roll < .09f) { StandingStone(ctx, mk, p); stones++; }
                    else if (roll < .12f) { Bench(ctx, mk, p, -toSea, false); benches++; }
                    else if (roll < .14f) { for (var k = 0; k < 4; k++) mk.Prism(WorldMaterials.Solid(new Color(.75f, .7f, .62f)), p + new Vector3(ctx.Range(-1f, 1f), .15f, ctx.Range(-1f, 1f)), Quaternion.Euler(0, ctx.Range(0, 180f), 0) * Vector3.forward, .12f, ctx.Range(1.5f, 3f), 5); }
                }
            }
            Debug.Log($"The Math Rock Escarpment: {trees} tree clumps, {leaves} leaf piles, {benches} benches, {chimes} wind chimes, {sheds} sheds, {strings} string lights, {bikes} bikes, {lone} lone trees, {stones} standing stones");
        }

        void Bench(LandContext ctx, MeshKit mk, Vector3 g, Vector3 face, bool lantern)
        {
            var up = Vector3.up; var rot = Quaternion.LookRotation(face); var sd = Vector3.Cross(up, face);
            mk.Box(wood, g + up * .45f, new Vector3(1.8f, .08f, .45f), rot); mk.Box(wood, g + up * .8f - face * .22f, new Vector3(1.8f, .4f, .06f), rot);
            foreach (var e in new[] { -.8f, .8f }) mk.Box(woodDark, g + up * .22f + sd * e, new Vector3(.08f, .45f, .4f), rot);
            BoxCol(ctx.Root, g + up * .45f, new Vector3(1.8f, .9f, .5f), rot);
            if (lantern) Lantern(ctx, mk, g + sd * 1.4f, 1.6f);
        }

        void Shed(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 g)
        {
            var up = Vector3.up; var face = new Vector3(center.x - g.x, 0, center.z - g.z).normalized; var rot = Quaternion.LookRotation(face); var sd = Vector3.Cross(up, face);
            var lo = g.y; foreach (var (x, z) in new[] { (-1f, -1f), (1f, -1f), (1f, 1f), (-1f, 1f) }) lo = Mathf.Min(lo, ctx.Ground(g.x + sd.x * x * 1.8f + face.x * z * 1.5f, g.z + sd.z * x * 1.8f + face.z * z * 1.5f));
            mk.Box(stone, new Vector3(g.x, lo + (g.y - lo) / 2f, g.z), new Vector3(3.8f, g.y - lo + .4f, 3.2f), rot);
            var paint = WorldMaterials.Solid(new[] { new Color(.6f, .25f, .2f), new Color(.35f, .45f, .5f), new Color(.75f, .7f, .55f) }[ctx.Rng.Next(3)]);
            mk.Box(paint, g + up * 1.4f, new Vector3(3.6f, 2.8f, 3f), rot); solid.Box(paint, g + up * 1.4f, new Vector3(3.6f, 2.8f, 3f), rot);
            mk.GableRoof(woodDark, g + up * 2.8f, new Vector3(3.6f, 0, 4.2f), 1.2f, rot);
            mk.Box(windowM, g + face * 1.52f + up * 1.6f + sd * .8f, new Vector3(.8f, .7f, .04f), rot);
            mk.Box(woodDark, g + face * 1.52f + up * 1f - sd * .6f, new Vector3(.9f, 2f, .04f), rot);
            halos.Add(g + face * 2f + up * 1.6f + sd * .8f, 1.4f, Warm, .5f);
        }

        void Bike(LandContext ctx, MeshKit mk, Vector3 g)
        {
            var up = Vector3.up; var dir = Quaternion.Euler(0, ctx.Range(0, 360f), 0) * Vector3.forward; var sd = Vector3.Cross(up, dir);
            var frame = WorldMaterials.Solid(new[] { new Color(.2f, .45f, .6f), new Color(.7f, .2f, .15f), new Color(.85f, .75f, .3f) }[ctx.Rng.Next(3)]);
            mk.Box(woodDark, g + up * .6f - sd * .3f, new Vector3(.12f, 1.2f, .12f), Quaternion.identity); ArtistBits.PostCollider(ctx.Root, g - sd * .3f, 1.1f, .1f);
            var tilt = Quaternion.LookRotation(dir) * Quaternion.Euler(0, 0, 12f);
            foreach (var e in new[] { -.55f, .55f }) { var w = g + dir * e + up * .35f; for (var k = 0; k < 10; k++) { var a = k * Mathf.PI / 5f; mk.Box(WorldMaterials.Solid(new Color(.1f, .1f, .1f)), w + tilt * new Vector3(0, Mathf.Sin(a), Mathf.Cos(a)) * .33f, new Vector3(.04f, .12f, .12f), tilt * Quaternion.Euler(-a * Mathf.Rad2Deg, 0, 0)); } }
            mk.Box(frame, g + up * .6f, new Vector3(.05f, .05f, 1f), tilt); BoxCol(ctx.Root, g + up * .5f, new Vector3(.3f, 1f, 1.5f), Quaternion.LookRotation(dir)); mk.Box(frame, g + up * .7f - dir * .15f, new Vector3(.05f, .5f, .05f), tilt);
        }

        void LoneTree(LandContext ctx, MeshKit mk, Vector3 g)
        {
            var up = Vector3.up; var h = ctx.Range(6f, 10f);
            mk.Prism(bark, g - up * .3f, up, .3f, h * .55f, 6, .6f);
            for (var k = 0; k < 6; k++) { var d = (Quaternion.Euler(0, k * 60f + ctx.Range(-15f, 15f), 0) * Quaternion.Euler(ctx.Range(30f, 55f), 0, 0) * up).normalized; mk.Prism(bark, g + up * h * (.4f + k * .03f), d, .1f, h * ctx.Range(.25f, .4f), 5, .3f); }
            if (ctx.Chance(.5f)) mk.Blob(WorldMaterials.Solid(LeafC[2]), g + up * h * .7f, Vector3.one * h * .15f, ctx.Rng, .3f);
            ArtistBits.PostCollider(ctx.Root, g, h * .5f, .35f);
        }

        void StandingStone(LandContext ctx, MeshKit mk, Vector3 g)
        {
            var h = ctx.Range(2f, 4.5f);
            mk.Box(stone, g + Vector3.up * (h / 2f - .3f), new Vector3(ctx.Range(.8f, 1.3f), h, ctx.Range(.4f, .7f)), Quaternion.Euler(ctx.Range(-5f, 5f), ctx.Range(0, 180f), ctx.Range(-5f, 5f)));
            BoxCol(ctx.Root, g + Vector3.up * (h / 2f - .3f), new Vector3(1.1f, h, .7f), Quaternion.identity);
        }

        void Grasses(LandContext ctx)
        {
            var mat = WindMesh.Material(.45f, 1f, new Color(.6f, .55f, .3f));
            var meshes = new Dictionary<(int, int), WindMesh>(); var rng = new System.Random(2002);
            const float st = 2.2f;
            for (var x = ctx.MinX; x < ctx.MaxX; x += st)
            for (var z = ctx.MinZ; z < ctx.MaxZ; z += st)
            {
                var px = x + ((float)rng.NextDouble() - .5f) * st; var pz = z + ((float)rng.NextDouble() - .5f) * st;
                if (!ctx.IsLand(px, pz, 1f) || NearPath(px, pz, 2.2f) || InNoGrass(px, pz, 0f) || InFootprint(ctx, px, pz, -2f) || NearWater(px, pz, 2.2f)) continue;
                var below = Below(px, pz);
                if (Mathf.PerlinNoise(px * .03f + 5f, pz * .03f + 1f) < (below ? .2f : .45f)) continue;
                var key = (Mathf.FloorToInt(px / 70f), Mathf.FloorToInt(pz / 70f));
                if (!meshes.TryGetValue(key, out var wm)) meshes[key] = wm = new WindMesh();
                var col = below ? Color.Lerp(new Color(.68f, .62f, .42f), new Color(.85f, .8f, .6f), (float)rng.NextDouble()) : Color.Lerp(new Color(.5f, .42f, .2f), new Color(.75f, .45f, .18f), (float)rng.NextDouble());
                wm.Tuft(new Vector3(px, ctx.Ground(px, pz) - .04f, pz), below ? .8f + (float)rng.NextDouble() * .7f : .35f + (float)rng.NextDouble() * .35f, col, Color.Lerp(col, new Color(1f, .9f, .7f), .4f), rng, below ? 5 : 4, below ? .05f : .06f);
            }
            foreach (var kv in meshes) kv.Value.Build("Escarpment grass", ctx.Root, mat);
        }
    }
}
