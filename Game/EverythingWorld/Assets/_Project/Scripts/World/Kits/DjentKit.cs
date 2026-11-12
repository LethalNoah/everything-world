using System.Collections.Generic;
using UnityEngine;

namespace EverythingWorld.World
{
    /// The Djent Spires (WORLD_GUIDE.md): precise and dazzling. A high land of pale terraced cliffs with clean stepped
    /// edges, crystal spires growing out of them, in bright, cold, clear light (the brightest of the metal lands, where
    /// the Caldera is neon in the dark and the Iron Range a dark forge). On the highest summit a great cluster of ice
    /// crystal, and floating far above it the fractal citadel: a diamond of crystal cubes, each face carrying smaller
    /// copies of itself, turning slowly with a gold heart, joined to the spire's tip by a shaft of light. Paths are
    /// fretboards: dark slate with gold frets and pearl inlays. Floating cubes hang in precise grids, Tesla coils
    /// crackle, footbridges strung with glowing strings span the hollows. Reads by bright pale ice and white against
    /// warm gold (blue/yellow and light/dark for the colourblind). Files: DjentKit (ground, arrival, citadel, paths,
    /// wilds), DjentArtists, DjentMore (the smaller artists' pieces), DjentSongIds, DjentLife.
    public partial class DjentKit : LandKit
    {
        public override string Land => "The Djent Spires";
        public override Vector3 ArtistFront(IslandWorldData data, IslandWorldData.Artist a) => facing.TryGetValue(a.name, out var f) ? f : base.ArtistFront(data, a);

        public static readonly Color Ice = new Color(.72f, .9f, 1f), Gold = new Color(1f, .8f, .32f), Cyan = new Color(.35f, .85f, 1f), Ink = new Color(.12f, .14f, .2f), Pearl = new Color(.95f, .97f, 1f);
        const float PathW = 3.4f;

        Material stone, stoneLight, slate, crystal, crystalDeep, goldM, fretM, pearlM, metal, dark, iceGlow, goldGlow;
        GlowHalos halos; Vector3 center; DjentLife life;
        readonly Dictionary<(int, int), List<Vector3>> pathCells = new Dictionary<(int, int), List<Vector3>>();
        readonly List<(Vector3 p, int w)> walkPoints = new List<(Vector3, int)>();
        readonly Dictionary<string, Vector3> facing = new Dictionary<string, Vector3>();
        readonly List<Vector3> noGrass = new List<Vector3>();
        Vector2 heart2, spire2; bool hasSpire;
        const float SpireR = 16f;

        void MakeMaterials()
        {
            stone = WorldMaterials.Solid(new Color(.74f, .77f, .82f)); stoneLight = WorldMaterials.Solid(new Color(.76f, .79f, .84f));
            slate = WorldMaterials.Solid(new Color(.13f, .14f, .19f), .5f);
            crystal = new Material(WorldMaterials.Glow(new Color(.78f, .92f, 1f), .55f)) { name = "Crystal" };
            crystalDeep = new Material(WorldMaterials.Glow(new Color(.45f, .7f, 1f), .7f)) { name = "Crystal deep" };
            goldM = WorldMaterials.Solid(new Color(.95f, .76f, .3f), .85f);
            fretM = new Material(WorldMaterials.Glow(Gold, .9f)) { name = "Frets" };
            pearlM = WorldMaterials.Glow(Pearl, 1.2f);
            metal = WorldMaterials.Solid(new Color(.55f, .58f, .64f), .8f);
            dark = WorldMaterials.Solid(new Color(.08f, .09f, .12f));
            iceGlow = new Material(WorldMaterials.Glow(Ice, 1.8f)) { name = "Ice glow" };
            goldGlow = new Material(WorldMaterials.Glow(Gold, 2f)) { name = "Gold glow" };
        }

        Material GlowM(Color c, float s = 2f) => WorldMaterials.Glow(c, s * .65f);
        static bool HasStation(IslandWorldData.Artist a) => a.stationId != 0;
        static float Foot(IslandWorldData.Artist a)
        {
            switch (a.name)
            {
                case "Plini": case "Leprous": return 17f;
                case "Polyphia": case "Animals As Leaders": case "Intervals": case "Sleep Token": return 12f;
                default: return HasStation(a) ? 12f : 7.5f;
            }
        }

        public override bool Ground(float height, float slope, float patch, out Color color)
        {
            if (height < 3f) color = Color.Lerp(new Color(.7f, .7f, .66f), new Color(.78f, .77f, .72f), patch);                  // pale shingle by the sea
            else if (slope > 26f) color = Color.Lerp(new Color(.5f, .55f, .63f), new Color(.6f, .65f, .73f), patch);              // the cliff faces, cool blue-grey
            else if (slope > 12f) color = Color.Lerp(new Color(.64f, .68f, .75f), new Color(.74f, .78f, .84f), patch);
            else color = Color.Lerp(new Color(.6f, .67f, .66f), new Color(.75f, .8f, .8f), Mathf.SmoothStep(0f, 1f, patch));    // the terraces, frosted pale
            color.a = 1f;
            return true;
        }

        static void BoxCol(Transform parent, Vector3 center, Vector3 size, Quaternion rot)
        {
            var go = new GameObject("Box collider"); go.transform.SetParent(parent, false); go.transform.SetPositionAndRotation(center, rot);
            go.AddComponent<BoxCollider>().size = size; go.isStatic = true;
        }

        static float Ease(float a, float b, float x) { var t = Mathf.Clamp01((x - a) / (b - a)); return t * t * (3f - 2f * t); }

        // ---------------------------------------------------------------- shaping the ground
        public override void ShapeGround(IslandWorldData data, int index)
        {
            var isl = data.layout.islands[index]; heart2 = new Vector2(isl.labelX, isl.labelZ);
            bool Inside(Vector2 p, float m) { if (data.IslandAt(p.x, p.y) != isl) return false; for (var k = 0; k < 16; k++) { var a = k * Mathf.PI / 8f; if (data.IslandAt(p.x + Mathf.Cos(a) * m, p.y + Mathf.Sin(a) * m) != isl) return false; } return true; }
            // the citadel's summit: the highest ground well inside the land, not too close to where you arrive
            hasSpire = false; var bestH = float.MinValue;
            for (var r = 0; r < data.Nz; r += 2)
            {
                var z = data.layout.depth - r * data.Step; if (Mathf.Abs(z - heart2.y) > 420f) continue;
                for (var col = 0; col < data.Nx; col += 2)
                {
                    var x = col * data.Step; if (Mathf.Abs(x - heart2.x) > 420f) continue;
                    var i = r * data.Nx + col; if (data.islandIds[i] != index) continue;
                    var d = Vector2.Distance(new Vector2(x, z), heart2); if (d < 110f || d > 330f) continue;
                    if (data.heights[i] <= bestH || !Inside(new Vector2(x, z), 60f)) continue;
                    bestH = data.heights[i]; spire2 = new Vector2(x, z); hasSpire = true;
                }
            }
            // terraces: the slopes stepped into flat treads and short steep risers, clean-edged, 5 m apart (the shore
            // left as it is)
            const float T = 5f;
            for (var r = 0; r < data.Nz; r++)
            {
                for (var col = 0; col < data.Nx; col++)
                {
                    var i = r * data.Nx + col; if (data.islandIds[i] != index) continue;
                    var h = data.heights[i]; var w = Ease(5f, 12f, h); if (w <= 0f) continue;
                    var f = h / T; var fi = Mathf.Floor(f); var fr = f - fi;
                    var stepped = (fi + Ease(.55f, 1f, fr)) * T;   // risers soft enough to walk up (at .72 a path over one stood up like a ladder)
                    data.heights[i] = Mathf.Lerp(h, stepped, .9f * w);
                }
            }
            var avoid = new List<Vector3>();
            if (hasSpire) avoid.Add(new Vector3(spire2.x, SpireR + 22f, spire2.y));
            ArtistPlacement.Spread(data, index, 52f, heart2, 46f, 40f, avoid, null, Land, 400f);
            foreach (var a in data.layout.artists)
            {
                if (a.sceneId != isl.sceneId) continue;
                a.r = Mathf.Max(a.r, Foot(a) + 6f);
                Level(data, index, new Vector2(a.x, a.z), Foot(a) + 1f, Foot(a) + 10f);
            }
            Level(data, index, heart2, 16f, 28f);
            if (hasSpire) Level(data, index, spire2, SpireR, SpireR + 40f);   // a wide shoulder, so the path up to the citadel can be walked
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
            life = new GameObject("Djent life").AddComponent<DjentLife>();
            life.transform.SetParent(ctx.Root, false);
            life.center = center; life.crystal = crystal; life.crystalDeep = crystalDeep; life.frets = fretM; life.iceGlow = iceGlow; life.goldGlow = goldGlow;
            var mk = new MeshKit(); var solid = new MeshKit();
            Remember(ctx.OnGround(center.x, center.z), 0);
            Arrival(ctx, mk, solid);
            if (hasSpire) Citadel(ctx, mk, solid);
            Paths(ctx, mk, solid);
            Wilds(ctx, mk, solid);
            mk.Build("The Djent Spires", ctx.Root, false);
            solid.BuildCollider("The Djent Spires walls", ctx.Root);
        }

        public override void Finish(LandContext ctx) { FrostGrass(ctx); halos.Build("Spires glow", ctx.Root); }

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
            && Vector2.Distance(new Vector2(x, z), new Vector2(center.x, center.z)) > 22f + pad
            && (!hasSpire || Vector2.Distance(new Vector2(x, z), spire2) > SpireR + 6f + pad);

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
                    // follow the ground across the path (a flat path on a cross-slope floated up to a metre on its
                    // low side); lift both edges only if the middle bulges above the line between them
                    float gl = ground.Ground(L[i].x, L[i].z), gr = ground.Ground(R[i].x, R[i].z), bulge = Mathf.Clamp(pts[i].y - (gl + gr) / 2f, 0f, .35f);
                    L[i].y = gl + bulge + lift; R[i].y = gr + bulge + lift;
                }
            }
            for (var i = 0; i + 1 < n; i++) mk.Quad(m, L[i], R[i], R[i + 1], L[i + 1], (pts[i] + pts[i + 1]) / 2f - Vector3.up);
        }

        /// A pointed hexagonal crystal (the land's building block).
        void Crystal(MeshKit mk, Material m, Vector3 at, Vector3 dir, float radius, float length, float twist = 0f)
        {
            var body = length * .78f;
            mk.Prism(m, at, dir, radius, body, 6, 1f, twist);
            mk.Prism(m, at + dir * body, dir, radius, length - body, 6, .02f, twist);
        }

        /// A crystal lantern: a slim pale post with a glowing hexagonal crystal on top, a gold collar under it.
        void Lantern(LandContext ctx, MeshKit mk, Vector3 g, float h = 3.2f)
        {
            var up = Vector3.up;
            mk.Prism(stoneLight, g - up * .2f, up, .12f, h + .2f, 6);
            mk.Prism(goldM, g + up * (h - .05f), up, .2f, .12f, 6);
            Crystal(mk, iceGlow, g + up * (h + .05f), up, .17f, .75f);
            halos.Add(g + up * (h + .4f), 1.8f, Ice, .55f); life.lanterns.Add(g + up * (h + .4f));
            ArtistBits.PostCollider(ctx.Root, g, h, .16f);
        }

        // ---------------------------------------------------------------- arrival
        /// Where you arrive: a hexagonal plaza of white stone inlaid with gold, concentric hexagons narrowing in toward
        /// a small crystal at the centre; six crystal lanterns round it; a slab of dark slate with the land's name in
        /// gold, framed like a fretboard; the citadel high above, straight ahead.
        void Arrival(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var c = ctx.OnGround(center.x, center.z);
            var toSpire = hasSpire ? new Vector3(spire2.x - c.x, 0, spire2.y - c.z).normalized : Vector3.forward; var sd = Vector3.Cross(up, toSpire);
            var yaw = Quaternion.LookRotation(toSpire).eulerAngles.y + 30f;
            mk.Prism(stoneLight, c - up * .3f, up, 13f, .4f, 6, 1f, yaw);
            foreach (var r in new[] { 12.2f, 8.6f, 5.2f, 2.4f })
                for (var k = 0; k < 6; k++)
                {
                    float a0 = (k * 60f + yaw) * Mathf.Deg2Rad, a1 = ((k + 1) * 60f + yaw) * Mathf.Deg2Rad;
                    var p0 = c + new Vector3(Mathf.Sin(a0), 0, Mathf.Cos(a0)) * r + up * .11f; var p1 = c + new Vector3(Mathf.Sin(a1), 0, Mathf.Cos(a1)) * r + up * .11f;
                    mk.Box(fretM, (p0 + p1) / 2f, new Vector3(.16f, .04f, Vector3.Distance(p0, p1) + .1f), Quaternion.LookRotation(p1 - p0));
                }
            Crystal(mk, iceGlow, c + up * .05f, up, .5f, 2.4f, 15f); halos.Add(c + up * 1.4f, 2.4f, Ice, .5f); ArtistBits.PostCollider(ctx.Root, c, 2f, .55f);
            for (var k = 0; k < 6; k++) { var a = (k * 60f + yaw + 30f) * Mathf.Deg2Rad; Lantern(ctx, mk, ctx.OnGround(c.x + Mathf.Sin(a) * 14.5f, c.z + Mathf.Cos(a) * 14.5f)); }
            // the name slab, behind you as you face the citadel
            var g = c - toSpire * 16f; g.y = ctx.Ground(g.x, g.z); var rot = Quaternion.LookRotation(toSpire);
            mk.Box(slate, g + up * 2.2f, new Vector3(10f, 3.6f, .9f), rot); solid.Box(slate, g + up * 1.6f, new Vector3(10f, 4.4f, 1f), rot);
            mk.Box(stoneLight, g + up * .2f, new Vector3(10.6f, .8f, 1.4f), rot);
            for (var k = 0; k < 11; k++) mk.Box(fretM, g + up * 2.2f + sd * ((k - 5f) * .92f) + toSpire * .47f, new Vector3(.06f, 3.4f, .03f), rot);
            foreach (var y in new[] { 1.1f, 3.3f }) mk.Box(pearlM, g + up * y + toSpire * .48f, new Vector3(10f, .05f, .03f), rot);
            mk.Box(slate, g + up * 2.2f + toSpire * .48f, new Vector3(8.4f, 1.1f, .02f), rot);
            ArtistBits.Label(ctx.Root, "THE DJENT SPIRES", g + up * 2.25f + toSpire * .5f, toSpire, .48f, Gold, TextAnchor.MiddleCenter);
            halos.Add(g + up * 2.3f + toSpire * 1.4f, 4f, Gold, .25f);
            foreach (var s in new[] { -1f, 1f }) { Crystal(mk, crystal, g + sd * s * 5.6f - up * .3f, (up + sd * s * .15f).normalized, .55f, 5.4f, 10f); ArtistBits.PostCollider(ctx.Root, g + sd * s * 5.8f, 3f, .6f); }
            ArtistBits.PointLight(ctx.Root, c + up * 6f, new Color(.85f, .93f, 1f), 26f, 1.4f);
            noGrass.Add(new Vector3(c.x, 15f, c.z));
            var mark = new GameObject("Shot: the arrival and the citadel").transform; mark.SetParent(ctx.Root, false);
            var cam = c - toSpire * 9f + sd * 3f + up * 1.7f; var look = (hasSpire ? new Vector3(spire2.x, c.y + 60f, spire2.y) : c + toSpire * 60f + up * 10f) - cam; mark.SetPositionAndRotation(cam, Quaternion.LookRotation(look.normalized));
        }

        // ---------------------------------------------------------------- the citadel
        /// On the summit: a hexagonal base of white stone, a great cluster of ice crystals rising from it (one huge
        /// spire with smaller ones leaning out round it), and far above, the fractal citadel: a diamond of crystal with a
        /// gold heart, each face carrying a smaller copy of the diamond, and each of those smaller copies again, turning
        /// slowly (DjentLife). A shaft of light joins the spire's tip to the citadel.
        void Citadel(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var c = ctx.OnGround(spire2.x, spire2.y);
            mk.Prism(stoneLight, c - up * .5f, up, SpireR - 1f, 1.1f, 6, 1f, 30f);
            for (var k = 0; k < 6; k++)
            {
                float a0 = (k * 60f + 30f) * Mathf.Deg2Rad, a1 = ((k + 1) * 60f + 30f) * Mathf.Deg2Rad; var r = SpireR - 1.3f;
                var p0 = c + new Vector3(Mathf.Sin(a0), 0, Mathf.Cos(a0)) * r + up * .64f; var p1 = c + new Vector3(Mathf.Sin(a1), 0, Mathf.Cos(a1)) * r + up * .64f;
                mk.Box(fretM, (p0 + p1) / 2f, new Vector3(.25f, .06f, Vector3.Distance(p0, p1)), Quaternion.LookRotation(p1 - p0));
            }
            const float H = 46f;
            Crystal(mk, crystal, c - up * .5f, up, 4.6f, H, 8f); ArtistBits.PostCollider(ctx.Root, c, 20f, 4.8f);
            mk.Prism(crystalDeep, c + up * 2f, up, 2.2f, H * .7f, 6, .3f, 8f);   // a deeper core showing through (lit from inside)
            for (var k = 0; k < 9; k++)
            {
                var a = k * 40f + ctx.Range(-10f, 10f); var d = Quaternion.Euler(0, a, 0) * Vector3.forward; var at = c + d * ctx.Range(6f, 10.5f); at.y = c.y;
                var dir = (up + d * ctx.Range(.25f, .55f)).normalized; var len = ctx.Range(9f, 24f); var rad = len * .1f;
                Crystal(mk, k % 3 == 0 ? crystalDeep : crystal, at - up * .6f, dir, rad, len, ctx.Range(0, 60f));
                ArtistBits.PostCollider(ctx.Root, at, Mathf.Min(len, 6f), rad + .2f);
            }
            var tip = c + up * (H - .5f);
            halos.Add(tip, 7f, Ice, .7f); halos.Add(c + up * 8f, 12f, Ice, .25f);
            ArtistBits.PointLight(ctx.Root, c + up * 14f, new Color(.8f, .92f, 1f), 50f, 2.2f);
            ArtistBits.PointLight(ctx.Root, c + up * 3f, Gold, 24f, 1.4f);
            // the shaft of light up to the citadel
            var citadelAt = tip + up * 64f;
            var shaft = new Material(WorldMaterials.Glow(Ice, 1.6f)) { name = "Citadel shaft" }; life.shaft = shaft;
            mk.Prism(shaft, tip, up, .35f, citadelAt.y - tip.y - 6f, 6);
            // the citadel itself, a separate piece so it can turn
            var ck = new MeshKit();
            void Diamond(Vector3 o, float s, int depth, Vector3 from)
            {
                var rot = Quaternion.LookRotation(Vector3.forward, Vector3.up) * Quaternion.Euler(45f, 0, 45f);
                ck.Box(depth == 0 ? crystalDeep : crystal, o, Vector3.one * s, rot);
                // gold edges: a thin frame round the middle
                if (depth < 2) foreach (var e in new[] { Vector3.right, Vector3.forward }) ck.Box(goldGlow, o, new Vector3(e.x * s * 1.42f + .12f, .12f, e.z * s * 1.42f + .12f), Quaternion.identity);
                if (depth >= 2) return;
                foreach (var d in new[] { Vector3.up, Vector3.down, Vector3.left, Vector3.right, Vector3.forward, Vector3.back })
                {
                    if (d == -from) continue;
                    ck.Box(goldM, o + d * s * .62f, Vector3.one * s * .12f, Quaternion.identity);   // a gold joint where the copy hangs
                    Diamond(o + d * s * 1.05f, s * .45f, depth + 1, d);
                }
            }
            Diamond(Vector3.zero, 11f, 0, Vector3.zero);
            ck.Blob(goldGlow, Vector3.zero, Vector3.one * 3.2f, ctx.Rng, 0f);   // the gold heart
            var cg = ck.BuildSingle("The fractal citadel", life.transform, false);
            if (cg != null) { cg.transform.position = citadelAt; life.citadel = cg.transform; }
            halos.Add(citadelAt, 22f, Ice, .35f); halos.Add(citadelAt, 8f, Gold, .45f);
            var lc = ArtistBits.PointLight(ctx.Root, citadelAt - up * 14f, new Color(.9f, .95f, 1f), 90f, 1.6f);
            life.citadelAt = citadelAt; life.spireTip = tip;
            noGrass.Add(new Vector3(c.x, SpireR + 4f, c.z));
            var mark = new GameObject("Shot: the citadel").transform; mark.SetParent(ctx.Root, false);
            var away = new Vector3(center.x - c.x, 0, center.z - c.z).normalized; var cam = ctx.OnGround(c.x + away.x * 70f, c.z + away.z * 70f) + up * 1.7f;
            mark.SetPositionAndRotation(cam, Quaternion.LookRotation((c + up * 55f - cam).normalized));
        }

        // ---------------------------------------------------------------- paths
        /// Fretboard paths: dark slate with a gold fret across it every couple of metres and pearl inlays at every
        /// twelfth, branching like a tree from the arrival to every artist; crystal lanterns along them.
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
                for (var d = walk == 1 ? 14f : PathW / 2f + .1f; ; d += 1.5f)
                {
                    var dd = Mathf.Min(d, end); var q = from + dir * dd + side * Mathf.Sin(dd / 16f) * Mathf.Min(2.5f, end * .04f);
                    if (!ctx.IsLand(q.x, q.z, 0f)) break;
                    pts.Add(ctx.OnGround(q.x, q.z)); if (dd >= end) break;
                }
                if (pts.Count < 2) return;
                Strip(mk, pts, slate, PathW, .2f, ctx);
                // frets across, a pearl dot between every 12th pair
                var fretN = 0;
                for (var i = 1; i < pts.Count; i++)
                {
                    var a = pts[i]; var b = pts[i - 1]; var dl = a - b; dl.y = 0; if (dl.sqrMagnitude < .01f) continue; var sdd = Vector3.Cross(Vector3.up, dl.normalized);
                    // the fret lies on the path's surface, tilted with it across a slope (it used to sit level at the
                    // path's highest point and float over the low side)
                    float gl = ctx.Ground(a.x - sdd.x * PathW / 2f, a.z - sdd.z * PathW / 2f), gr = ctx.Ground(a.x + sdd.x * PathW / 2f, a.z + sdd.z * PathW / 2f);
                    var bulge = Mathf.Clamp(a.y - (gl + gr) / 2f, 0f, .35f); var y = (gl + gr) / 2f + bulge;
                    var fl = new Vector3(a.x, gl + bulge + .24f, a.z) - sdd * (PathW / 2f); var fr = new Vector3(a.x, gr + bulge + .24f, a.z) + sdd * (PathW / 2f);
                    mk.Box(fretM, (fl + fr) / 2f, new Vector3(.12f, .05f, PathW - .2f), Quaternion.LookRotation(fr - fl));
                    fretN++;
                    if (fretN % 12 == 6) foreach (var s in new[] { -.6f, .6f }) mk.Prism(pearlM, new Vector3(a.x, y + .22f, a.z) - dl.normalized * .75f + sdd * s, Vector3.up, .16f, .04f, 10);
                }
                foreach (var p in pts) Remember(p, walk);
                for (var i = 8; i < pts.Count; i += 14)
                {
                    var lp = pts[i] + side * ((i / 14) % 2 == 0 ? 1f : -1f) * (PathW / 2f + 1f);
                    var near = false; foreach (var o in posts) if ((o - lp).sqrMagnitude < 19f * 19f) near = true;
                    if (near || InFootprint(ctx, lp.x, lp.z, 1f)) continue;
                    posts.Add(lp); Lantern(ctx, mk, ctx.OnGround(lp.x, lp.z));
                }
                walk++;
            }
            if (hasSpire) Lay(ctx.OnGround(center.x, center.z), new Vector3(spire2.x, 0, spire2.y), SpireR - 2f, null);
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
        /// Crystal clusters, floating cubes in precise grids, Tesla coils, string bridges over the hollows, fallen
        /// crystal columns, gold survey markers, frosted boulders.
        void Wilds(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            int clusters = 0, cubes = 0, coils = 0, bridges = 0, columns = 0, markers = 0, rocks = 0;
            // string bridges first: they need the hollows
            for (var tries = 0; tries < 900 && bridges < 5; tries++)
            {
                var ax = ctx.Range(ctx.MinX, ctx.MaxX); var az = ctx.Range(ctx.MinZ, ctx.MaxZ);
                if (!Open(ctx, ax, az, 4f)) continue;
                var dir = Quaternion.Euler(0, ctx.Range(0, 360f), 0) * Vector3.forward; var L = ctx.Range(26f, 46f);
                var bx = ax + dir.x * L; var bz = az + dir.z * L;
                if (!Open(ctx, bx, bz, 4f)) continue;
                float ha = ctx.Ground(ax, az), hb = ctx.Ground(bx, bz);
                if (Mathf.Abs(ha - hb) > 3f) continue;
                var low = float.MaxValue; var ok = true;
                for (var k = 1; k < 10; k++) { var t = k / 10f; var x = Mathf.Lerp(ax, bx, t); var z = Mathf.Lerp(az, bz, t); if (!ctx.IsLand(x, z, 2f) || InFootprint(ctx, x, z, 3f) || NearPath(x, z, 3f)) { ok = false; break; } low = Mathf.Min(low, ctx.Ground(x, z)); }
                if (!ok || Mathf.Min(ha, hb) - low < 6f) continue;
                var near = false; foreach (var c in noGrass) if (c.y > 20f && Vector2.Distance(new Vector2(c.x, c.z), new Vector2(ax, az)) < 60f) near = true;
                if (near) continue;
                StringBridge(ctx, mk, solid, ctx.OnGround(ax, az), ctx.OnGround(bx, bz)); bridges++;
            }
            const float cell = 22f;
            for (var x = ctx.MinX; x < ctx.MaxX; x += cell)
            for (var z = ctx.MinZ; z < ctx.MaxZ; z += cell)
            {
                float px = x + ctx.Range(0f, cell), pz = z + ctx.Range(0f, cell);
                if (!Open(ctx, px, pz, 3f)) continue;
                var p = ctx.OnGround(px, pz); var roll = ctx.Range(0f, 1f);
                if (roll < .15f) { Cluster(ctx, mk, p); clusters++; }
                else if (roll < .21f) { FloatingCubes(ctx, mk, p); cubes++; }
                else if (roll < .31f && coils < 9) { TeslaCoil(ctx, mk, solid, p); coils++; }
                else if (roll < .36f) { FallenColumn(ctx, mk, p); columns++; }
                else if (roll < .41f) { Marker(ctx, mk, p); markers++; }
                else if (roll < .52f)
                {
                    var s = ctx.Range(1f, 2.6f); var lo = p.y;
                    for (var k = 0; k < 6; k++) { var a = k * Mathf.PI / 3f; lo = Mathf.Min(lo, ctx.Ground(px + Mathf.Cos(a) * s * .9f, pz + Mathf.Sin(a) * s * .9f)); }
                    p.y = lo; mk.Prism(ctx.Chance(.5f) ? stone : stoneLight, p - Vector3.up * .3f, Vector3.up, s, s * .9f, 6, .8f, ctx.Range(0, 60f));
                    BoxCol(ctx.Root, p + Vector3.up * s * .4f, new Vector3(s * 1.7f, s * .9f + .3f, s * 1.7f), Quaternion.Euler(0, 0, 0)); rocks++;
                }
            }
            Debug.Log($"The Djent Spires: citadel {(hasSpire ? "yes" : "no")}, {clusters} crystal clusters, {cubes} floating cube grids, {coils} Tesla coils, {bridges} string bridges, {columns} fallen columns, {markers} markers, {rocks} rocks");
        }

        /// A cluster of pointed ice crystals, the tallest in the middle, sometimes one gold among them.
        void Cluster(LandContext ctx, MeshKit mk, Vector3 at)
        {
            var n = ctx.Rng.Next(3, 8); var big = ctx.Range(3f, 11f); var gold = ctx.Chance(.2f);
            for (var k = 0; k < n; k++)
            {
                var len = k == 0 ? big : big * ctx.Range(.25f, .65f);
                var d = k == 0 ? Vector3.up : (Quaternion.Euler(ctx.Range(15f, 40f), ctx.Range(0, 360f), 0) * Vector3.up).normalized;
                var off = k == 0 ? Vector3.zero : new Vector3(ctx.Range(-1.2f, 1.2f), 0, ctx.Range(-1.2f, 1.2f)) * (big * .15f);
                var foot = at + off; foot.y = Mathf.Min(at.y, ctx.Ground(foot.x, foot.z));
                Crystal(mk, gold && k == 1 ? goldGlow : (k % 4 == 3 ? crystalDeep : crystal), foot - Vector3.up * .4f, d, len * .13f, len, ctx.Range(0, 60f));
                // each leaning crystal gets its own collider up to head height (the one box in the middle missed the outer ones)
                if (k > 0) { var cl = Mathf.Min(len, 3f / Mathf.Max(.3f, d.y)); BoxCol(ctx.Root, foot - Vector3.up * .4f + d * cl / 2f, new Vector3(len * .26f, len * .26f, cl), Quaternion.LookRotation(d)); }
            }
            halos.Add(at + Vector3.up * big * .5f, big * .35f + 1f, Ice, .3f);
            BoxCol(ctx.Root, at + Vector3.up * Mathf.Min(big, 4f) * .5f, new Vector3(big * .45f + .6f, Mathf.Min(big, 4f), big * .45f + .6f), Quaternion.identity);
        }

        /// Cubes hanging in the air in a precise grid, each turning slowly in step (DjentLife), a gold one among them.
        void FloatingCubes(LandContext ctx, MeshKit mk, Vector3 at)
        {
            var n = ctx.Rng.Next(2, 4); var gap = ctx.Range(2.2f, 3.4f); var s = ctx.Range(.6f, 1.3f); var y0 = ctx.Range(3.2f, 5f);
            var yaw = Quaternion.Euler(0, ctx.Range(0, 90f), 0); var goldK = ctx.Rng.Next(n * n);
            for (var i = 0; i < n; i++) for (var j = 0; j < n; j++)
            {
                var k = i * n + j; var ck = new MeshKit();
                ck.Box(k == goldK ? goldGlow : crystal, Vector3.zero, Vector3.one * s, Quaternion.identity);
                var go = ck.BuildSingle("Floating cube", life.transform, false); if (go == null) continue;
                var p = at + yaw * new Vector3((i - (n - 1) / 2f) * gap, y0 + ((i + j) % 2) * gap * .5f, (j - (n - 1) / 2f) * gap);
                p.y += Mathf.Max(0f, ctx.Ground(p.x, p.z) - at.y);   // on a slope, keep the uphill cubes above head height too
                go.transform.position = p; life.cubes.Add(go.transform); life.cubeAt.Add(p);
            }
            halos.Add(at + Vector3.up * (y0 + gap * .3f), gap * n * .6f, Ice, .3f);
            mk.Prism(goldM, at - Vector3.up * .1f, Vector3.up, .5f, .2f, 6);
        }

        /// A Tesla coil: a stack of pale insulator rings on a slate base, a polished torus on top; arcs crack from it
        /// (DjentLife), more often in loud songs.
        void TeslaCoil(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 at)
        {
            var up = Vector3.up;
            mk.Prism(slate, at - up * .3f, up, 1.4f, 1.3f, 6); solid.Prism(slate, at - up * .3f, up, 1.4f, 1.3f, 6);
            mk.Prism(metal, at + up, up, .35f, 6.5f, 10);
            for (var k = 0; k < 9; k++) mk.Prism(k % 2 == 0 ? stoneLight : crystal, at + up * (1.4f + k * .65f), up, .7f, .25f, 12);
            var top = at + up * 8f;
            for (var k = 0; k < 16; k++) { var a = k * Mathf.PI / 8f; var a2 = (k + 1) * Mathf.PI / 8f; var p0 = top + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * 1.4f; var p1 = top + new Vector3(Mathf.Cos(a2), 0, Mathf.Sin(a2)) * 1.4f; mk.Box(metal, (p0 + p1) / 2f, new Vector3(.7f, .7f, Vector3.Distance(p0, p1) + .1f), Quaternion.LookRotation(p1 - p0)); }
            mk.Blob(iceGlow, top, Vector3.one * .35f, ctx.Rng, 0f);
            halos.Add(top, 2.4f, Cyan, .5f);
            ArtistBits.PostCollider(ctx.Root, at, 7f, .8f);
            life.coils.Add(top);
        }

        /// A footbridge across a hollow, strung like a guitar: a dark deck with gold frets laid between two crystal
        /// pylons, four glowing strings either side for handrails, shimmering with the music.
        void StringBridge(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 a, Vector3 b)
        {
            var up = Vector3.up; var d = b - a; d.y = 0; var len = d.magnitude; d /= len; var sd = Vector3.Cross(up, d); var rot = Quaternion.LookRotation(b - a);
            var deckA = a + up * .35f; var deckB = b + up * .35f; var mid = (deckA + deckB) / 2f;
            var deckRot = Quaternion.LookRotation(deckB - deckA);
            mk.Box(slate, mid, new Vector3(2.6f, .3f, Vector3.Distance(deckA, deckB) + 1f), deckRot); solid.Box(slate, mid, new Vector3(2.6f, .3f, Vector3.Distance(deckA, deckB) + 1f), deckRot);
            for (var t = 1.5f; t < len - 1f; t += 2.2f) mk.Box(fretM, Vector3.Lerp(deckA, deckB, t / len) + up * .17f, new Vector3(2.4f, .04f, .1f), deckRot);
            var strings = new Material(WorldMaterials.Glow(Pearl, 1.3f)) { name = "Bridge strings" }; life.strings.Add(strings);
            foreach (var s in new[] { -1f, 1f })
            {
                foreach (var end in new[] { a, b })
                {
                    var py = end + sd * s * 1.45f;
                    Crystal(mk, crystal, py - up * .5f, up, .28f, 3.4f);
                    mk.Box(goldM, py + up * 1.25f, new Vector3(.25f, .9f, .25f), Quaternion.identity);
                    ArtistBits.PostCollider(ctx.Root, py, 2.6f, .3f);
                }
                for (var k = 0; k < 4; k++)
                {
                    var y = .55f + k * .22f; var p0 = a + sd * s * 1.35f + up * (.35f + y); var p1 = b + sd * s * 1.35f + up * (.35f + y);
                    mk.Box(strings, (p0 + p1) / 2f, new Vector3(.03f + k * .008f, .03f + k * .008f, Vector3.Distance(p0, p1)), Quaternion.LookRotation(p1 - p0));
                }
                solid.Box(slate, mid + sd * s * 1.35f + up * .8f, new Vector3(.12f, 1.2f, Vector3.Distance(deckA, deckB)), deckRot);
            }
            halos.Add(mid + up * 1.2f, len * .4f, Pearl, .18f);
            foreach (var e in new[] { a, b }) { noGrass.Add(new Vector3(e.x, 4f, e.z)); ctx.AddKeepout(e.x, e.z, 5f); }
            if (life.strings.Count == 1)
            {
                var mark = new GameObject("Shot: a string bridge").transform; mark.SetParent(ctx.Root, false);
                var cam = a - d * 6f + sd * 2f + up * 1.7f; mark.SetPositionAndRotation(cam, Quaternion.LookRotation((b + up * 1f - cam).normalized));
            }
        }

        /// A fallen crystal column lying in pieces, as if broken off a spire.
        void FallenColumn(LandContext ctx, MeshKit mk, Vector3 at)
        {
            var dir = Quaternion.Euler(0, ctx.Range(0, 360f), 0) * Vector3.forward; var r = ctx.Range(.5f, .9f);
            for (var k = 0; k < 3; k++)
            {
                var p = at + dir * (k * 2.6f); p = ctx.OnGround(p.x, p.z) + Vector3.up * (r * .6f);
                var d = Quaternion.Euler(0, ctx.Range(-12f, 12f), 0) * dir;
                mk.Prism(k == 2 ? crystalDeep : crystal, p - d * 1.1f, d, r, 2.3f, 6, 1f, k * 20f);
                BoxCol(ctx.Root, p, new Vector3(r * 1.8f, r * 1.8f, 2.4f), Quaternion.LookRotation(d));
            }
        }

        /// A gold survey marker: a slim gold obelisk on a white plinth with a glowing tip, the kind of precise thing
        /// someone left to measure the land.
        void Marker(LandContext ctx, MeshKit mk, Vector3 at)
        {
            var up = Vector3.up;
            mk.Box(stoneLight, at + up * .3f, new Vector3(1.1f, .6f, 1.1f), Quaternion.Euler(0, 45f, 0));
            mk.Prism(goldM, at + up * .6f, up, .22f, 2.6f, 4, .2f, 45f);
            mk.Blob(goldGlow, at + up * 3.3f, Vector3.one * .14f, ctx.Rng, 0f); halos.Add(at + up * 3.3f, 1.2f, Gold, .5f);
            ArtistBits.PostCollider(ctx.Root, at, 3f, .5f);
        }

        void FrostGrass(LandContext ctx)
        {
            var mat = WindMesh.Material(.25f, 1f, new Color(.7f, .78f, .76f));
            var meshes = new Dictionary<(int, int), WindMesh>(); var rng = new System.Random(808);
            const float st = 2.6f;
            for (var x = ctx.MinX; x < ctx.MaxX; x += st)
            for (var z = ctx.MinZ; z < ctx.MaxZ; z += st)
            {
                var px = x + ((float)rng.NextDouble() - .5f) * st; var pz = z + ((float)rng.NextDouble() - .5f) * st;
                if (!ctx.IsLand(px, pz, 1f) || NearPath(px, pz, 2.6f) || InNoGrass(px, pz, 0f) || InFootprint(ctx, px, pz, -2f)) continue;
                if (hasSpire && Vector2.Distance(new Vector2(px, pz), spire2) < SpireR + 2f) continue;
                if (Mathf.PerlinNoise(px * .035f + 7f, pz * .035f + 2f) < .45f) continue;
                var key = (Mathf.FloorToInt(px / 70f), Mathf.FloorToInt(pz / 70f));
                if (!meshes.TryGetValue(key, out var wm)) meshes[key] = wm = new WindMesh();
                var col = Color.Lerp(new Color(.55f, .66f, .62f), new Color(.8f, .86f, .84f), (float)rng.NextDouble());
                wm.Tuft(new Vector3(px, ctx.Ground(px, pz) - .04f, pz), .3f + (float)rng.NextDouble() * .4f, col, Color.Lerp(col, Pearl, .5f), rng, 4, .05f);
            }
            foreach (var kv in meshes) kv.Value.Build("Frost grass", ctx.Root, mat);
        }
    }
}
