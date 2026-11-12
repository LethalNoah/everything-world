using System.Collections.Generic;
using UnityEngine;

namespace EverythingWorld.World
{
    /// The Folk Woods (WORLD_GUIDE.md): old-growth forest round a mountain lake on a clear, gentle, misty morning.
    /// Where you arrive stands the Great Tree, a huge old oak with a little stage built round its roots. A small
    /// steam train runs a loop of track round the lake, stopping at a wooden station (the Peace Train); a long
    /// wooden footbridge crosses the lake. Canoes pulled up on the shore, log cabins with smoke from the chimneys,
    /// campfires, a harmonica left on a stump, birds. Files: FolkKit (ground, the lake, the railway, the Great
    /// Tree, trails, the woods), FolkArtists, FolkLife.
    public partial class FolkKit : LandKit
    {
        public override string Land => "The Folk Woods";
        public override Vector3 ArtistFront(IslandWorldData data, IslandWorldData.Artist a) => facing.TryGetValue(a.name, out var f) ? f : base.ArtistFront(data, a);

        public const string PeaceTrain = "4d0d2llNcQIHmaCgYTrODQ", RockyMountainHigh = "1ne9wOtDF2jM6Cm8WBkaER", EdmundFitzgerald = "536L9C0N7vhYdibCJx3cI2", BridgeOverTroubledWater = "5LFtkeNjLpZAey3Arj3h4M";
        public override (string song, string label, string artist)[] SpecialSongs => new[]
        {
            (PeaceTrain, "Peace Train", "Yusuf / Cat Stevens"), (RockyMountainHigh, "Rocky Mountain High", "John Denver"),
            (EdmundFitzgerald, "The Wreck of the Edmund Fitzgerald", "@the lake"), (BridgeOverTroubledWater, "Bridge over Troubled Water", "@the bridge"),
            (Werewolves, "Werewolves of London", "Warren Zevon"), (RunningUpThatHill, "Running Up That Hill", "Kate Bush"), (EarthMove, "I Feel the Earth Move", "Carole King"),
            (CountryRoads, "Take Me Home, Country Roads", "John Denver"), (SweetCaroline, "Sweet Caroline", "@the village"),
        };

        static readonly Color Warm = new Color(1f, .82f, .55f), Ink = new Color(.97f, .95f, .9f);
        const float TrailW = 2.6f;

        Material wood, logM, bark, leaf, pineM, stone, dark, metal, water, rail;
        GlowHalos halos; Vector3 center; FolkLife life; MeshKit soft;
        readonly Dictionary<(int, int), List<Vector3>> pathCells = new Dictionary<(int, int), List<Vector3>>();
        readonly List<(Vector3 p, int w)> walkPoints = new List<(Vector3, int)>();
        readonly Dictionary<string, Vector3> facing = new Dictionary<string, Vector3>();
        readonly List<Vector3> noGrass = new List<Vector3>();
        bool hasLake; Vector2 lakeC; float lakeR = 70f, lakeY;

        void MakeMaterials()
        {
            wood = WorldMaterials.Solid(new Color(.55f, .42f, .3f)); logM = WorldMaterials.Solid(new Color(.48f, .35f, .24f)); bark = WorldMaterials.Solid(new Color(.38f, .3f, .24f));
            leaf = WorldMaterials.Solid(new Color(.3f, .48f, .28f)); pineM = WorldMaterials.Solid(new Color(.16f, .34f, .26f)); stone = WorldMaterials.Solid(new Color(.55f, .55f, .55f));
            dark = WorldMaterials.Solid(new Color(.08f, .08f, .09f)); metal = WorldMaterials.Solid(new Color(.5f, .5f, .52f), .7f); rail = WorldMaterials.Solid(new Color(.45f, .42f, .4f), .8f);
            water = new Material(WorldMaterials.Glow(new Color(.25f, .42f, .5f), .08f)) { name = "Lake water" }; water.SetFloat("_Smoothness", .8f);
        }

        Material GlowM(Color c, float s = 2f) => WorldMaterials.Glow(c, s * .65f);
        static bool HasStation(IslandWorldData.Artist a) => a.station || a.stationId != 0;
        static float Foot(IslandWorldData.Artist a) => HasStation(a) ? 18f : 11f;

        public override bool Ground(float height, float slope, float patch, out Color color)
        {
            if (slope > 8f) color = Color.Lerp(new Color(.5f, .48f, .44f), new Color(.58f, .56f, .5f), patch);
            else if (height < 2f) color = new Color(.68f, .64f, .52f);
            else color = Color.Lerp(new Color(.36f, .54f, .28f), new Color(.56f, .64f, .3f), Mathf.SmoothStep(.2f, .9f, patch));
            color.a = 1f;
            return true;
        }

        float LakeEdge(float x, float z) { var a = Mathf.Atan2(z - lakeC.y, x - lakeC.x); return lakeR * (1f + .14f * Mathf.Sin(a * 2f + .5f) + .08f * Mathf.Sin(a * 5f)); }
        bool InLake(float x, float z, float pad) => hasLake && Vector2.Distance(new Vector2(x, z), lakeC) < LakeEdge(x, z) + pad;

        public override void ShapeGround(IslandWorldData data, int index)
        {
            var isl = data.layout.islands[index]; var heart = new Vector2(isl.labelX, isl.labelZ);
            bool Inside(Vector2 p, float m) { if (data.IslandAt(p.x, p.y) != isl) return false; for (var k = 0; k < 16; k++) { var a = k * Mathf.PI / 8f; if (data.IslandAt(p.x + Mathf.Cos(a) * m, p.y + Mathf.Sin(a) * m) != isl) return false; } return true; }
            hasLake = false; var bestR = 0f;
            for (var k = 0; k < 16; k++)
            {
                var a = k * Mathf.PI / 8f; var dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                for (var r = 85f; r >= 50f; r -= 5f) { var c = heart + dir * (r + 40f); if (Inside(c, r * 1.2f + 50f)) { if (r > bestR) { bestR = r; lakeC = c; } break; } }
            }
            if (bestR >= 50f)
            {
                hasLake = true; lakeR = bestR; lakeY = data.HeightAt(lakeC.x, lakeC.y) - 1.2f;
                for (var r = 0; r < data.Nz; r++)
                {
                    var z = data.layout.depth - r * data.Step; if (Mathf.Abs(z - lakeC.y) > lakeR * 1.3f + 50f) continue;
                    for (var col = 0; col < data.Nx; col++)
                    {
                        var x = col * data.Step; if (Mathf.Abs(x - lakeC.x) > lakeR * 1.3f + 50f) continue;
                        var i = r * data.Nx + col; if (data.islandIds[i] != index) continue;
                        var d = Vector2.Distance(new Vector2(x, z), lakeC) - LakeEdge(x, z);
                        if (d < 0f) data.heights[i] = lakeY - .4f - 3f * Mathf.Clamp01(-d / 20f);
                        else if (d < 40f) { var target = lakeY + .5f + d * .2f; data.heights[i] = Mathf.Lerp(Mathf.Min(data.heights[i], target), data.heights[i], Mathf.SmoothStep(0, 1, d / 40f)); if (data.heights[i] < lakeY + .4f) data.heights[i] = lakeY + .4f; }
                    }
                }
            }
            var avoid = new List<Vector3>(); if (hasLake) avoid.Add(new Vector3(lakeC.x, lakeR * 1.2f + 18f, lakeC.y));
            ArtistPlacement.Spread(data, index, 58f, heart, 46f, 40f, avoid, null, Land, 300f);
            foreach (var a in data.layout.artists)
            {
                if (a.sceneId != isl.sceneId) continue;
                a.r = Mathf.Max(a.r, Foot(a) + 6f);
                Level(data, index, new Vector2(a.x, a.z), Foot(a) + 1f, Foot(a) + 12f);
            }
            Level(data, index, heart, 14f, 30f);
            Level(data, index, OrchardAt, 14f, 22f);
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
                    var i = r * data.Nx + col; if (data.islandIds[i] != index || InLake(x, z, 4f)) continue;
                    var d = Vector2.Distance(new Vector2(x, z), c);
                    if (d < flat) data.heights[i] = level;
                    else if (d < blend) data.heights[i] = Mathf.Lerp(level, data.heights[i], Mathf.SmoothStep(0, 1, (d - flat) / (blend - flat)));
                }
            }
        }

        public override void Dress(LandContext ctx)
        {
            MakeMaterials();
            halos = new GlowHalos(); soft = new MeshKit();
            pathCells.Clear(); walkPoints.Clear(); facing.Clear(); noGrass.Clear();
            center = ctx.Plaza;
            life = new GameObject("Folk life").AddComponent<FolkLife>();
            life.transform.SetParent(ctx.Root, false);
            life.center = center;
            var mk = new MeshKit(); var solid = new MeshKit();
            GreatTree(ctx, mk, solid);
            if (hasLake) { Lake(ctx, mk, solid); Railway(ctx, mk, solid); Footbridge(ctx, mk, solid); }
            Trails(ctx, mk, solid);
            Village(ctx, mk, solid);
            WoodsPlaces(ctx, mk, solid);   // after everything that was here, before the trees are scattered
            Woods(ctx, mk, solid);
            mk.Build("Folk Woods", ctx.Root, false);
            soft.Build("Soft folk woods crowns and reeds", ctx.Root, false);   // "Soft…": walked through, so the land audit skips it
            solid.BuildCollider("Folk Woods walls", ctx.Root);
        }

        public override void Finish(LandContext ctx) { GroundCover(ctx); halos.Build("Folk glow", ctx.Root); }

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

        bool OnRail(float x, float z, float pad) => hasLake && Mathf.Abs(Vector2.Distance(new Vector2(x, z), lakeC) - (LakeEdge(x, z) + 14f)) < 3f + pad;

        bool Open(LandContext ctx, float x, float z, float pad) =>
            ctx.IsLand(x, z, 4f) && !InFootprint(ctx, x, z, pad) && !NearPath(x, z, pad + 2f) && !InNoGrass(x, z, pad) && !InLake(x, z, pad + 4f) && !OnRail(x, z, pad)
            && Vector2.Distance(new Vector2(x, z), new Vector2(center.x, center.z)) > 22f + pad;

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
                    // on bumpy ground, lift each edge to the ground under it so no terrain pokes through
                    float gl = ground.Ground(L[i].x, L[i].z), gr = ground.Ground(R[i].x, R[i].z), top = Mathf.Max(pts[i].y, Mathf.Max(gl, gr) - .05f);
                    L[i].y = Mathf.Max(gl, top - .15f) + lift; R[i].y = Mathf.Max(gr, top - .15f) + lift;
                }
            }
            for (var i = 0; i + 1 < n; i++) mk.Quad(m, L[i], R[i], R[i + 1], L[i + 1], (pts[i] + pts[i + 1]) / 2f - Vector3.up);
        }

        void Lantern(LandContext ctx, MeshKit mk, Vector3 g)
        {
            mk.Prism(wood, g - Vector3.up * .3f, Vector3.up, .1f, 2.9f, 6);
            mk.Box(wood, g + Vector3.up * 2.55f + Vector3.right * .3f, new Vector3(.7f, .08f, .08f), Quaternion.identity);
            mk.Box(GlowM(Warm, 2.2f), g + Vector3.up * 2.2f + Vector3.right * .55f, new Vector3(.22f, .32f, .22f), Quaternion.identity);
            halos.Add(g + Vector3.up * 2.2f + Vector3.right * .55f, 1.1f, Warm, .7f);
            ArtistBits.PostCollider(ctx.Root, g, 2.6f, .1f);
        }

        void Pine(LandContext ctx, MeshKit mk, Vector3 at, float s)
        {
            var up = Vector3.up; var h = 12f * s;
            mk.Prism(bark, at - up * .3f, up, .4f * s, h * .35f, 7);
            for (var k = 0; k < 4; k++) soft.Cone(pineM, at + up * (h * (.2f + k * .18f)), (3f - k * .65f) * s, h * .35f, 8, ctx.Range(0, 60f));
            ArtistBits.PostCollider(ctx.Root, at, h * .5f, .45f * s);
        }

        void Oak(LandContext ctx, MeshKit mk, Vector3 at, float s)
        {
            var up = Vector3.up; var h = 5f * s;
            mk.Prism(bark, at - up * .3f, up, .55f * s, h, 8, .7f);
            for (var k = 0; k < 5; k++) { var d = Quaternion.Euler(0, k * 72f + ctx.Range(-20f, 20f), 0) * Vector3.forward; soft.Blob(leaf, at + up * (h + 1f * s) + d * 2.2f * s, new Vector3(2.4f, 1.8f, 2.4f) * s, ctx.Rng, .3f); }
            soft.Blob(leaf, at + up * (h + 2.4f * s), new Vector3(3f, 2f, 3f) * s, ctx.Rng, .3f);
            ArtistBits.PostCollider(ctx.Root, at, h, .55f * s);
        }

        // ---------------------------------------------------------------- the Great Tree
        /// A huge old oak, its roots spreading wide, a round wooden stage built round the trunk with benches facing
        /// it, lanterns hung in its lower branches, a harmonica left on a stump.
        void GreatTree(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var c = ctx.OnGround(center.x + 14f, center.z + 4f);
            for (var k = 0; k < 9; k++) { var d = Quaternion.Euler(0, k * 40f, 0) * Vector3.forward; mk.Prism(bark, c + d * 2.4f - up * .4f, (d + up * .25f).normalized, .9f, 5f, 6, .3f); }
            mk.Prism(bark, c - up * .3f, up, 2.6f, 9f, 10, .7f);
            for (var k = 0; k < 7; k++)
            {
                var d = Quaternion.Euler(0, k * 51f + 10f, 0) * Vector3.forward; var dir = (d + up * (.35f + (k % 3) * .15f)).normalized;
                mk.Prism(bark, c + up * 8f, dir, .9f, 10f, 7, .4f);
                var tip = c + up * 8f + dir * 10f;
                soft.Blob(leaf, tip + up * 1f, new Vector3(5f, 3.4f, 5f), ctx.Rng, .3f);
                if (k % 2 == 0) { var lp = c + up * 8f + dir * 5f - up * 1.2f; mk.Box(GlowM(Warm, 2.4f), lp, new Vector3(.3f, .4f, .3f), Quaternion.identity); mk.Box(dark, lp + up * .55f, new Vector3(.02f, .7f, .02f), Quaternion.identity); halos.Add(lp, 1.3f, Warm, .8f); }
            }
            soft.Blob(leaf, c + up * 18f, new Vector3(9f, 5f, 9f), ctx.Rng, .3f);
            solid.Prism(bark, c - up * .3f, up, 2.6f, 10f, 8);
            // the round stage round the trunk
            mk.Prism(wood, c + up * .1f, up, 7f, .6f, 20); mk.Prism(wood, c + up * .1f, up, 7.05f, .1f, 20);
            solid.Prism(wood, c - up * .3f, up, 7f, 1f, 16);
            for (var k = 0; k < 5; k++) { var d = Quaternion.Euler(0, -60f + k * 30f, 0) * (new Vector3(center.x - c.x, 0, center.z - c.z).normalized); var bp = ctx.OnGround(c.x + d.x * 11f, c.z + d.z * 11f); var br = Quaternion.LookRotation(-d); mk.Box(logM, bp + up * .4f, new Vector3(3f, .35f, .5f), br); solid.Box(logM, bp + up * .4f, new Vector3(3f, .8f, .6f), br); }
            var stump = ctx.OnGround(c.x - 9f, c.z + 3f); mk.Prism(bark, stump - up * .2f, up, .6f, .9f, 10); solid.Prism(bark, stump - up * .2f, up, .6f, .9f, 10);
            mk.Box(metal, stump + up * .75f, new Vector3(.5f, .08f, .14f), Quaternion.Euler(0, 30f, 0)); mk.Box(dark, stump + up * .77f + Vector3.forward * .03f, new Vector3(.46f, .02f, .02f), Quaternion.Euler(0, 30f, 0));
            ArtistBits.PointLight(ctx.Root, c + up * 6f, Warm, 22f, 1.6f);
            ArtistBits.Label(ctx.Root, "THE FOLK WOODS", ctx.OnGround(center.x - 4f, center.z - 6f) + up * 1.6f, Vector3.forward, .3f, Ink, TextAnchor.MiddleCenter);
            mk.Box(wood, ctx.OnGround(center.x - 4f, center.z - 6f) + up * 1.6f - Vector3.forward * .06f, new Vector3(4f, .7f, .1f), Quaternion.identity);
            mk.Prism(wood, ctx.OnGround(center.x - 4f, center.z - 6f) - up * .3f, up, .1f, 1.6f, 6);
            noGrass.Add(new Vector3(c.x, 9f, c.z));
            Remember(ctx.OnGround(center.x, center.z), 0);
            life.tree = c;
        }

        // ---------------------------------------------------------------- the lake
        void Lake(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            const int n = 56; var c = new Vector3(lakeC.x, lakeY - .05f, lakeC.y);
            for (var k = 0; k < n; k++)
            {
                float a0 = k * Mathf.PI * 2f / n, a1 = (k + 1) * Mathf.PI * 2f / n;
                Vector3 R(float a) { var r = LakeEdge(lakeC.x + Mathf.Cos(a), lakeC.y + Mathf.Sin(a)) + 5f; return new Vector3(lakeC.x + Mathf.Cos(a) * r, c.y, lakeC.y + Mathf.Sin(a) * r); }
                mk.Tri(water, c, R(a0), R(a1), c - Vector3.up);
            }
            // rocks, reeds and canoes round the shore
            for (var k = 0; k < 40; k++)
            {
                var a = ctx.Range(0, Mathf.PI * 2f); var dx = Mathf.Cos(a); var dz = Mathf.Sin(a); var r = LakeEdge(lakeC.x + dx, lakeC.y + dz) + ctx.Range(-1f, 2f);
                var p = new Vector3(lakeC.x + dx * r, 0, lakeC.y + dz * r); if (!ctx.IsLand(p.x, p.z, 1f)) continue; p.y = ctx.Ground(p.x, p.z);
                if (k % 3 == 0) { var rr = new Vector3(ctx.Range(.6f, 1.4f), .6f, ctx.Range(.6f, 1.2f)); mk.Blob(stone, p + Vector3.up * .3f, rr, ctx.Rng, .35f); solid.Box(stone, p + Vector3.up * .3f, rr * 1.6f, Quaternion.identity); }
                else for (var j = 0; j < 4; j++) soft.Box(WorldMaterials.Solid(new Color(.45f, .55f, .3f)), p + new Vector3(ctx.Range(-.4f, .4f), .7f, ctx.Range(-.4f, .4f)), new Vector3(.05f, 1.4f, .05f), Quaternion.Euler(ctx.Range(-10f, 10f), 0, ctx.Range(-10f, 10f)));
            }
            Color[] canoe = { new Color(.75f, .2f, .15f), new Color(.2f, .45f, .6f), new Color(.85f, .65f, .2f) };
            for (var k = 0; k < 6; k++)
            {
                var a = k * Mathf.PI / 3f + .4f; var dx = Mathf.Cos(a); var dz = Mathf.Sin(a); var r = LakeEdge(lakeC.x + dx, lakeC.y + dz) + 1.5f;
                var p = new Vector3(lakeC.x + dx * r, 0, lakeC.y + dz * r); if (!ctx.IsLand(p.x, p.z, 1f)) continue;
                // afloat at the water's edge: find where the drawn ground first rises out of the lake along this line and
                // sit the canoe just inside it, on the water, rather than up on the grass bank
                var edge = r; for (var rr = r - 8f; rr < r + 8f; rr += .5f) if (ctx.Ground(lakeC.x + dx * rr, lakeC.y + dz * rr) > lakeY) { edge = rr; break; }
                p = new Vector3(lakeC.x + dx * (edge - 1.3f), lakeY - .25f, lakeC.y + dz * (edge - 1.3f));
                var rot = Quaternion.LookRotation(new Vector3(-dz, 0, dx)) * Quaternion.Euler(0, 20f, 0);
                Canoe(mk, p + Vector3.up * .3f, rot * Vector3.forward, canoe[k % 3]);
                mk.Box(wood, p + Vector3.up * .2f + rot * Vector3.right * 1f, new Vector3(.1f, .05f, 1.4f), rot * Quaternion.Euler(0, 25f, 0));
                solid.Box(wood, p + Vector3.up * .3f, new Vector3(1f, .6f, 4.4f), rot);
            }
            life.lake = c; life.lakeR = lakeR;
            var mark = new GameObject("Shot: the lake").transform; mark.SetParent(ctx.Root, false);
            var look = new Vector3(lakeC.x - center.x, 0, lakeC.y - center.z).normalized; var shore = new Vector3(lakeC.x, 0, lakeC.y) - look * (LakeEdge(lakeC.x - look.x, lakeC.y - look.z) + 6f); mark.SetPositionAndRotation(ctx.OnGround(shore.x, shore.z), Quaternion.LookRotation(look));
            noGrass.Add(new Vector3(lakeC.x, lakeR * 1.2f, lakeC.y));
        }

        /// A loop of narrow-gauge track round the lake on sleepers, a little wooden station with a platform, a
        /// water tower and a lamp; the train itself runs in FolkLife.
        void Railway(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            const int n = 160; var pts = new List<Vector3>();
            for (var k = 0; k <= n; k++)
            {
                var a = k * Mathf.PI * 2f / n; var dx = Mathf.Cos(a); var dz = Mathf.Sin(a);
                var r = LakeEdge(lakeC.x + dx, lakeC.y + dz) + 14f; var p = new Vector3(lakeC.x + dx * r, 0, lakeC.y + dz * r);
                p.y = ctx.IsLand(p.x, p.z, 0f) ? Mathf.Max(ctx.Ground(p.x, p.z), lakeY + .6f) : lakeY + .6f; pts.Add(p);
            }
            // smooth the heights so the line runs gently
            // (two passes: six lifted the track up to a metre over every dip, sleepers and rails floating in the air)
            for (var pass = 0; pass < 2; pass++) for (var k = 1; k < pts.Count - 1; k++) { var p = pts[k]; p.y = (pts[k - 1].y + pts[k].y * 2f + pts[k + 1].y) / 4f; pts[k] = p; }
            // then keep it hugging the ground on land (smoothing still lifted it over dips, rails at knee height)
            for (var k = 0; k < pts.Count; k++) { var p = pts[k]; if (!ctx.IsLand(p.x, p.z, 0f)) continue; var g = Mathf.Max(ctx.Ground(p.x, p.z), lakeY + .6f); p.y = Mathf.Clamp(p.y, g, g + .1f); pts[k] = p; }
            for (var k = 0; k < pts.Count - 1; k++)
            {
                var a = pts[k]; var b = pts[k + 1]; var dir = (b - a); var side = Vector3.Cross(Vector3.up, new Vector3(dir.x, 0, dir.z).normalized);
                mk.Box(logM, (a + b) / 2f + Vector3.up * .04f, new Vector3(2.4f, .14f, .4f), Quaternion.LookRotation(side));
                foreach (var s in new[] { -.6f, .6f }) mk.Box(rail, (a + b) / 2f + side * s + Vector3.up * .16f, new Vector3(.09f, .1f, dir.magnitude + .05f), Quaternion.LookRotation(dir));
                var g = ctx.Ground((a.x + b.x) / 2f, (a.z + b.z) / 2f); var top = (a.y + b.y) / 2f;
                if (top - g > .15f)
                {   // a stone embankment under any stretch that rides above the ground (it used to start only at .6 m, so lower gaps floated)
                    var emb = new Vector3((a.x + b.x) / 2f, (top + g) / 2f, (a.z + b.z) / 2f); var es = new Vector3(2.6f, top - g + .2f, dir.magnitude + .1f);
                    mk.Box(stone, emb, es, Quaternion.LookRotation(dir));
                    if (top - g > .45f) solid.Box(stone, emb, es, Quaternion.LookRotation(dir));
                }
            }
            life.track = pts.ToArray();
            // the station on the side nearest where you arrive
            var toLake = new Vector3(lakeC.x - center.x, 0, lakeC.y - center.z).normalized; var st = pts[0]; var bd = float.MaxValue; var si = 0;
            for (var k = 0; k < pts.Count; k++) { var d = (pts[k] - (new Vector3(lakeC.x, 0, lakeC.y) - toLake * (lakeR + 14f))).sqrMagnitude; if (d < bd) { bd = d; st = pts[k]; si = k; } }
            var tdir = (pts[Mathf.Min(si + 1, pts.Count - 1)] - pts[Mathf.Max(si - 1, 0)]); tdir.y = 0; tdir.Normalize(); var out_ = -Vector3.Cross(Vector3.up, tdir); if (Vector3.Dot(out_, -toLake) < 0) out_ = -out_;
            var plat = st + out_ * 3.4f; plat.y = st.y;
            mk.Box(stone, plat + Vector3.up * .3f, new Vector3(3f, .7f, 14f), Quaternion.LookRotation(tdir)); solid.Box(stone, plat + Vector3.up * .3f, new Vector3(3f, .7f, 14f), Quaternion.LookRotation(tdir));
            var hut = plat + out_ * 4f; hut.y = Mathf.Max(ctx.Ground(hut.x, hut.z), plat.y);
            mk.Box(wood, hut + Vector3.up * 1.6f, new Vector3(4f, 3.2f, 6f), Quaternion.LookRotation(tdir)); solid.Box(wood, hut + Vector3.up * 1.6f, new Vector3(4f, 3.2f, 6f), Quaternion.LookRotation(tdir));
            mk.GableRoof(WorldMaterials.Solid(new Color(.45f, .2f, .18f)), hut + Vector3.up * 3.2f, new Vector3(5f, 0, 7f), 1.4f, Quaternion.LookRotation(tdir));
            mk.Box(GlowM(Warm, 1.6f), hut + Vector3.up * 1.6f - out_ * 2.01f, new Vector3(.02f, 1f, 1.4f), Quaternion.LookRotation(tdir));
            mk.Box(wood, plat + Vector3.up * 3.2f, new Vector3(.1f, .7f, 3f), Quaternion.LookRotation(tdir));
            ArtistBits.Label(ctx.Root, "PEACE TRAIN", plat + Vector3.up * 3.2f - out_ * .07f, -out_, .22f, Ink, TextAnchor.MiddleCenter);
            var wt = plat + tdir * 9f + out_ * 2f; wt.y = ctx.Ground(wt.x, wt.z);
            foreach (var a in new[] { -1f, 1f }) foreach (var b in new[] { -1f, 1f }) mk.Box(logM, wt + new Vector3(a, 2f, b) * .8f, new Vector3(.2f, 4f, .2f), Quaternion.identity);
            mk.Prism(logM, wt + Vector3.up * 4f, Vector3.up, 1.4f, 2.4f, 10); mk.Cone(WorldMaterials.Solid(new Color(.45f, .2f, .18f)), wt + Vector3.up * 6.4f, 1.5f, 1f, 10);
            ArtistBits.PostCollider(ctx.Root, wt, 6f, 1.4f);
            Lantern(ctx, mk, plat + tdir * 5f + out_ * 1.2f + Vector3.up * .65f);
            Remember(plat, 400);
            life.station = si;
        }

        /// A long wooden footbridge across the lake on piles, rails both sides, lanterns along it.
        void Footbridge(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var toLake = new Vector3(lakeC.x - center.x, 0, lakeC.y - center.z).normalized; var across = Vector3.Cross(Vector3.up, toLake);
            var c = new Vector3(lakeC.x, 0, lakeC.y);
            var a = c - across * (LakeEdge(lakeC.x - across.x, lakeC.y - across.z) + 4f); var b = c + across * (LakeEdge(lakeC.x + across.x, lakeC.y + across.z) + 4f);
            if (!ctx.IsLand(a.x, a.z, 2f) || !ctx.IsLand(b.x, b.z, 2f)) return;
            var y = lakeY + 1.4f; var len = Vector3.Distance(a, b); var dir = (b - a).normalized; var side = Vector3.Cross(Vector3.up, dir);
            var pts = new List<Vector3>();
            for (var d = 0f; d <= len; d += 2f) { var p = a + dir * d; var arch = Mathf.Sin(d / len * Mathf.PI) * 2.2f; p.y = Mathf.Max(ctx.Ground(p.x, p.z), y + arch); pts.Add(p); }
            Strip(mk, pts, wood, 3f, .1f);
            for (var i = 0; i + 1 < pts.Count; i++)
            {
                var p0 = pts[i]; var p1 = pts[i + 1]; var mid = (p0 + p1) / 2f;
                solid.Box(wood, mid - Vector3.up * .1f, new Vector3(3f, .4f, Vector3.Distance(p0, p1) + .1f), Quaternion.LookRotation(p1 - p0));
                foreach (var s in new[] { -1.45f, 1.45f })
                {
                    mk.Box(logM, mid + side * s + Vector3.up * 1.05f, new Vector3(.1f, .1f, Vector3.Distance(p0, p1) + .05f), Quaternion.LookRotation(p1 - p0));
                    solid.Box(logM, mid + side * s + Vector3.up * .6f, new Vector3(.15f, 1f, Vector3.Distance(p0, p1) + .05f), Quaternion.LookRotation(p1 - p0));
                    if (i % 2 == 0) { mk.Prism(logM, p0 + side * s, Vector3.up, .07f, 1.1f, 6); var g = ctx.Ground(p0.x, p0.z); if (p0.y - g > .5f) { mk.Prism(logM, new Vector3(p0.x, g - .5f, p0.z) + side * s, Vector3.up, .16f, p0.y - g + .5f, 6); ArtistBits.PostCollider(ctx.Root, new Vector3(p0.x, g, p0.z) + side * s, p0.y - g, .18f); } }
                }
                if (i % 8 == 4) Lantern(ctx, mk, mid + side * 1.6f);
            }
            foreach (var p in pts) Remember(p, 401);
            var glow = new Material(WorldMaterials.Glow(new Color(.55f, .75f, 1f), .2f)) { name = "Bridge glow" };
            for (var i = 0; i + 1 < pts.Count; i++) foreach (var s in new[] { -1.45f, 1.45f }) mk.Box(glow, (pts[i] + pts[i + 1]) / 2f + side * s + Vector3.up * 1.13f, new Vector3(.12f, .09f, Vector3.Distance(pts[i], pts[i + 1]) + .02f), Quaternion.LookRotation(pts[i + 1] - pts[i]));
            life.bridgeGlow = glow;
            var mark = new GameObject("Shot: the bridge").transform; mark.SetParent(ctx.Root, false);
            var feet = a - dir * 6f; feet.y = ctx.Ground(feet.x, feet.z); mark.SetPositionAndRotation(feet, Quaternion.LookRotation(dir));
        }

        // ---------------------------------------------------------------- trails
        void Trails(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var todo = new List<IslandWorldData.Artist>(ctx.Artists); var walk = 1; var lamps = new List<Vector3>();
            var dirt = WorldMaterials.Solid(new Color(.62f, .5f, .34f));
            while (todo.Count > 0)
            {
                IslandWorldData.Artist next = null; Vector3 join = default; var best = float.MaxValue;
                foreach (var a in todo)
                    foreach (var (p, w) in walkPoints) { if (w >= 400) continue; var d = Vector2.Distance(new Vector2(p.x, p.z), new Vector2(a.x, a.z)) - Foot(a); if (d < best) { best = d; next = a; join = p; } }
                if (next == null) break;
                todo.Remove(next);
                var from = new Vector3(join.x, 0, join.z); var to = new Vector3(next.x, 0, next.z);
                var dir = to - from; var total = dir.magnitude; if (total < 2f) continue; dir /= total;
                facing[next.name] = -dir;
                var side = Vector3.Cross(Vector3.up, dir); var end = total - Foot(next) * .8f;
                var pts = new List<Vector3>();
                for (var d = walk == 1 ? 10f : TrailW / 2f + .1f; ; d += 1.5f)
                {
                    var dd = Mathf.Min(d, end); var q = from + dir * dd + side * Mathf.Sin(dd / 16f) * Mathf.Min(3f, end * .05f);
                    if (!ctx.IsLand(q.x, q.z, 0f) || InLake(q.x, q.z, 1f)) break;
                    pts.Add(ctx.OnGround(q.x, q.z)); if (dd >= end) break;
                }
                if (pts.Count < 2) continue;
                Strip(mk, pts, dirt, TrailW, .2f, ctx);
                foreach (var p in pts) Remember(p, walk);
                for (var i = 7; i < pts.Count; i += 13)
                {
                    var lp = pts[i] + side * ((i / 13) % 2 == 0 ? 1f : -1f) * (TrailW / 2f + .6f);
                    var near = false; foreach (var o in lamps) if ((o - lp).sqrMagnitude < 20f * 20f) near = true;
                    if (near || OnRail(lp.x, lp.z, 0f)) continue;
                    lamps.Add(lp); Lantern(ctx, mk, ctx.OnGround(lp.x, lp.z));
                }
                walk++;
            }
        }

        // ---------------------------------------------------------------- the woods
        /// Old-growth pines and oaks, log cabins with smoke from their chimneys, campfires with logs round them,
        /// mossy boulders, a fallen log, wildflowers.
        void Woods(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            int pines = 0, oaks = 0, cabins = 0, fires = 0, rocks = 0;
            Giants(ctx, mk, solid);
            const float cell = 20f;
            for (var x = ctx.MinX; x < ctx.MaxX; x += cell)
            for (var z = ctx.MinZ; z < ctx.MaxZ; z += cell)
            {
                float px = x + ctx.Range(0f, cell), pz = z + ctx.Range(0f, cell);
                if (!Open(ctx, px, pz, 3f)) continue;
                var p = ctx.OnGround(px, pz); var roll = ctx.Range(0f, 1f);
                if (roll < .06f && cabins < 8 && NearPath(px, pz, 24f)) { Cabin(ctx, mk, solid, p); cabins++; }
                else if (roll < .085f && fires < 6) { Campfire(ctx, mk, solid, p); fires++; }
                else if (roll < .12f && life.shafts.Count < 26 && NearPath(px, pz, 30f)) { life.shafts.Add(p); if (life.shafts.Count == 1) { var mark = new GameObject("Shot: a sun shaft").transform; mark.SetParent(ctx.Root, false); var cam = p; for (var k = 0; k < 8; k++) { var q = ctx.OnGround(px + Mathf.Cos(k * .785f) * 18f, pz + Mathf.Sin(k * .785f) * 18f); if (ctx.IsLand(q.x, q.z, 2f) && (cam == p || q.y > cam.y)) cam = q; } mark.SetPositionAndRotation(cam, Quaternion.LookRotation(new Vector3(p.x - cam.x, 0, p.z - cam.z).normalized + Vector3.up * .2f)); } }
                else if (roll < .55f) { Pine(ctx, mk, p, ctx.Range(.9f, 1.5f)); pines++; }
                else if (roll < .8f) { Oak(ctx, mk, p, ctx.Range(.9f, 1.4f)); oaks++; }
                else if (roll < .86f) { mk.Blob(stone, p + Vector3.up * .6f, new Vector3(ctx.Range(1f, 2.4f), ctx.Range(.7f, 1.4f), ctx.Range(1f, 2f)), ctx.Rng, .35f); soft.Blob(WorldMaterials.Solid(new Color(.35f, .5f, .25f)), p + Vector3.up * 1.3f, new Vector3(1.2f, .25f, 1f), ctx.Rng, .3f); solid.Box(stone, p + Vector3.up * .6f, new Vector3(3f, 1.4f, 2.6f), Quaternion.identity); rocks++; }
                else continue;
            }
            Debug.Log($"The Folk Woods: {life.giants} giant sequoias, {life.shafts.Count} sun shafts, lake {(hasLake ? lakeR.ToString("0") + " m" : "none")}, {pines} pines, {oaks} oaks, {cabins} cabins, {fires} campfires, {rocks} boulders");
        }

        /// The Old Giants: a handful of colossal sequoias, 40 to 55 m tall, red trunks rising far above the pines
        /// with their crowns high up, a ring of lanterns round each foot. They are the land's skyline.
        void Giants(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var redBark = WorldMaterials.Solid(new Color(.58f, .3f, .2f)); var crown = WorldMaterials.Solid(new Color(.2f, .38f, .26f));
            var placed = new List<Vector3>();
            for (var tries = 0; tries < 900 && placed.Count < 11; tries++)
            {
                var px = ctx.Range(ctx.MinX, ctx.MaxX); var pz = ctx.Range(ctx.MinZ, ctx.MaxZ);
                if (!ctx.IsLand(px, pz, 25f) || !Open(ctx, px, pz, 9f)) continue;
                var ok = Vector2.Distance(new Vector2(px, pz), new Vector2(center.x, center.z)) > 45f;
                foreach (var o in placed) if ((new Vector2(o.x, o.z) - new Vector2(px, pz)).sqrMagnitude < 55f * 55f) ok = false;
                if (!ok) continue;
                var at = ctx.OnGround(px, pz); placed.Add(at); var s = ctx.Range(.85f, 1.15f); var h = 46f * s; var r = 2.4f * s;
                for (var k = 0; k < 6; k++) { var d = Quaternion.Euler(0, k * 60f + 15f, 0) * Vector3.forward; mk.Prism(redBark, at + d * r * .8f - up * .5f, (d * .5f + up).normalized, r * .55f, 4f * s, 6, .2f); solid.Prism(redBark, at + d * r * .8f - up * .5f, (d * .5f + up).normalized, r * .55f, 4f * s, 6, .2f); }
                mk.Prism(redBark, at - up * .5f, up, r, h, 10, .35f);
                for (var k = 0; k < 6; k++) { var d = Quaternion.Euler(0, k * 67f, 0) * Vector3.forward; mk.Prism(redBark, at + up * (h * .55f + k * 2.2f), (d + up * .3f).normalized, .35f * s, 5f * s, 5, .3f); }
                for (var k = 0; k < 5; k++) soft.Cone(crown, at + up * (h * .5f + k * h * .1f), (7f - k * 1.2f) * s, h * .2f, 9, ctx.Range(0, 60f));
                solid.Prism(redBark, at - up * .5f, up, r, 12f, 10);
                for (var k = 0; k < 4; k++) { var d = Quaternion.Euler(0, k * 90f + 45f, 0) * Vector3.forward; Lantern(ctx, mk, ctx.OnGround(at.x + d.x * (r + 3f), at.z + d.z * (r + 3f))); }
                noGrass.Add(new Vector3(at.x, r + 2f, at.z));
                if (placed.Count == 1) { var mark = new GameObject("Shot: the old giants").transform; mark.SetParent(ctx.Root, false); var back = new Vector3(at.x - center.x, 0, at.z - center.z).normalized; var cam = at - back * 32f; mark.SetPositionAndRotation(ctx.OnGround(cam.x, cam.z), Quaternion.LookRotation(back + up * .5f)); }
            }
            life.giants = placed.Count;
        }

        /// A canoe: a long pointed hull with a dark rim, lying along dir.
        void Canoe(MeshKit mk, Vector3 mid, Vector3 dir, Color paint)
        {
            dir = Vector3.ProjectOnPlane(dir, Vector3.up).normalized; var hull = WorldMaterials.Solid(paint, .6f);
            var rot = Quaternion.LookRotation(dir);
            mk.Box(hull, mid, new Vector3(.8f, .4f, 2.4f), rot);
            foreach (var s in new[] { -1f, 1f }) mk.Prism(hull, mid + dir * s * 1.2f + Vector3.up * .02f, dir * s, .36f, 1.1f, 6, .08f);
            mk.Box(dark, mid + Vector3.up * .21f, new Vector3(.84f, .04f, 2.5f), rot);
        }

        /// Sets a cabin (and its porch) on a slope: the floor goes at the highest ground under it, so no wall sinks into
        /// the hill, and a fieldstone foundation and log stilts under the porch run down to the ground on the low side.
        Vector3 SettleCabin(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 at, Vector3 face)
        {
            var up = Vector3.up; face.y = 0; face.Normalize(); var side = Vector3.Cross(up, face); var rot = Quaternion.LookRotation(face);
            float hi = float.MinValue, lo = float.MaxValue;
            foreach (var f in new[] { -2.7f, 0f, 2.5f, 4.4f }) foreach (var s in new[] { -3.1f, 0f, 3.1f })
            { var p = at + face * f + side * s; var g = ctx.Ground(p.x, p.z); hi = Mathf.Max(hi, g); lo = Mathf.Min(lo, g); }
            at.y = hi;
            var drop = hi - lo;
            if (drop > .1f)
            {
                var fd = drop + .5f;   // foundation under the cabin's walls, a little into the ground
                mk.Box(stone, at + up * (.05f - fd / 2f), new Vector3(6.2f, fd, 5f), rot); solid.Box(stone, at + up * (.05f - fd / 2f), new Vector3(6.2f, fd, 5f), rot);
                foreach (var f in new[] { 2.6f, 4.2f }) foreach (var s in new[] { -2.9f, 0f, 2.9f })
                {   // log stilts under the porch deck
                    var p = at + face * f + side * s; var g = ctx.Ground(p.x, p.z);
                    if (at.y - g > .05f) mk.Prism(logM, new Vector3(p.x, g - .3f, p.z), up, .13f, at.y - g + .45f, 6);
                    if (at.y - g > .4f) ArtistBits.PostCollider(ctx.Root, new Vector3(p.x, g, p.z), at.y - g, .15f);
                }
            }
            return at;
        }

        void Cabin(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 at)
        {
            var up = Vector3.up; var face = new Vector3(center.x - at.x, 0, center.z - at.z).normalized; var rot = Quaternion.LookRotation(face); var side = Vector3.Cross(up, face);
            at = SettleCabin(ctx, mk, solid, at, face);
            for (var k = 0; k < 7; k++) { mk.Prism(logM, at + up * (.25f + k * .45f) - side * 3f + face * 2.2f, side, .24f, 6f, 8); mk.Prism(logM, at + up * (.25f + k * .45f) - side * 3f - face * 2.2f, side, .24f, 6f, 8); mk.Prism(logM, at + up * (.45f + k * .45f) - face * 2.4f + side * 2.8f, face, .24f, 4.8f, 8); mk.Prism(logM, at + up * (.45f + k * .45f) - face * 2.4f - side * 2.8f, face, .24f, 4.8f, 8); }
            solid.Box(logM, at + up * 1.6f, new Vector3(6f, 3.2f, 4.8f), rot);
            mk.GableRoof(WorldMaterials.Solid(new Color(.35f, .32f, .3f)), at + up * 3.2f, new Vector3(5.6f, 0, 6.8f), 1.8f, Quaternion.LookRotation(side));
            mk.Box(dark, at + up * 1.1f + face * 2.45f, new Vector3(1f, 2f, .05f), rot);
            foreach (var s in new[] { -1.8f, 1.8f }) mk.Box(GlowM(Warm, 1.6f), at + up * 1.7f + face * 2.45f + side * s, new Vector3(.9f, .8f, .04f), rot);
            var ch = at + up * 4f + side * 2f; mk.Box(stone, ch, new Vector3(.8f, 2.2f, .8f), rot);
            life.chimneys.Add(ch + up * 1.2f);
            Porch(ctx, mk, solid, at, face, side);
            noGrass.Add(new Vector3(at.x, 5f, at.z));
        }

        /// A porch along a cabin's front: a plank deck, posts and a little roof, a rocking chair, a lantern by the door.
        void Porch(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 at, Vector3 face, Vector3 side)
        {
            var up = Vector3.up; var rot = Quaternion.LookRotation(face); var pc = at + face * 3.4f;
            mk.Box(wood, pc + up * .25f, new Vector3(6.2f, .3f, 1.9f), rot); solid.Box(wood, pc + up * .2f, new Vector3(6.2f, .4f, 1.9f), rot);
            foreach (var s in new[] { -3f, 3f }) { mk.Prism(logM, pc + side * s + face * .8f, up, .1f, 2.7f, 6); ArtistBits.PostCollider(ctx.Root, pc + side * s + face * .8f, 2.7f, .12f); }
            mk.Box(WorldMaterials.Solid(new Color(.35f, .32f, .3f)), pc + up * 2.75f, new Vector3(6.4f, .1f, 2.1f), rot * Quaternion.Euler(10f, 0, 0));
            var chair = pc + up * .4f + side * 1.8f; mk.Box(wood, chair + up * .45f, new Vector3(.65f, .06f, .6f), rot); mk.Box(wood, chair + up * .9f - face * .28f, new Vector3(.65f, .8f, .06f), rot * Quaternion.Euler(-10f, 0, 0));
            foreach (var s in new[] { -.28f, .28f }) mk.Box(wood, chair + up * .12f + side * s, new Vector3(.05f, .08f, .9f), rot);
            solid.Box(wood, chair + up * .5f, new Vector3(.7f, 1f, .7f), rot);
            var lamp = at + face * 2.6f + up * 2.2f - side * 1.1f; mk.Box(GlowM(Warm, 2.4f), lamp, new Vector3(.22f, .3f, .22f), Quaternion.identity); halos.Add(lamp, 1.2f, Warm, .7f);
        }

        /// The village: a few cabins with porches round a little green near where you arrive, a general store with a
        /// lit window and a bench out front, a stone well, a washing line, a fire circle of log benches, string lights
        /// between the porches.
        void Village(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; Vector3 green = default; var found = false;
            for (var r = 34f; r <= 70f && !found; r += 6f)
                for (var k = 0; k < 16 && !found; k++)
                {
                    var a = k * Mathf.PI / 8f + .2f; var px = center.x + Mathf.Cos(a) * r; var pz = center.z + Mathf.Sin(a) * r;
                    if (!ctx.IsLand(px, pz, 30f) || InLake(px, pz, 24f) || InFootprint(ctx, px, pz, 24f) || OnRail(px, pz, 14f)) continue;
                    green = ctx.OnGround(px, pz); found = true;
                }
            if (!found) return;
            var toHeart = new Vector3(center.x - green.x, 0, center.z - green.z).normalized; var gside = Vector3.Cross(up, toHeart);
            // cabins round the green, facing it
            var porchLamps = new List<Vector3>();
            for (var k = 0; k < 4; k++)
            {
                var a = (k * 90f + 45f) * Mathf.Deg2Rad; var at = green + (toHeart * Mathf.Cos(a) + gside * Mathf.Sin(a)) * 15f; at = ctx.OnGround(at.x, at.z); at = SettleCabin(ctx, mk, solid, at, green - at);
                VillageCabin(ctx, mk, solid, at, (green - at).normalized); porchLamps.Add(at + (green - at).normalized * 4.4f + up * 2.6f);
            }
            for (var k = 0; k < porchLamps.Count; k++) { var a = porchLamps[k]; var b = porchLamps[(k + 1) % porchLamps.Count]; for (var j = 1; j < 12; j++) { var t = j / 12f; var p = Vector3.Lerp(a, b, t) - up * Mathf.Sin(t * Mathf.PI) * 1f; mk.Blob(GlowM(Warm, 2.4f), p, Vector3.one * .1f, ctx.Rng, 0f); } halos.Add((a + b) / 2f - up, 2f, Warm, .4f); }
            // the general store on the far side
            var gs = green - toHeart * 20f; gs = ctx.OnGround(gs.x, gs.z); var grot = Quaternion.LookRotation(toHeart); var white = WorldMaterials.Solid(new Color(.9f, .88f, .82f));
            mk.Box(white, gs + up * 2f, new Vector3(8f, 4f, 6f), grot); solid.Box(white, gs + up * 2f, new Vector3(8f, 4f, 6f), grot);
            mk.Box(white, gs + up * 4.6f + toHeart * 3f, new Vector3(8f, 1.4f, .2f), grot); ArtistBits.Label(ctx.Root, "GENERAL STORE", gs + up * 4.6f + toHeart * 3.12f, toHeart, .3f, new Color(.25f, .15f, .1f), TextAnchor.MiddleCenter);
            mk.GableRoof(WorldMaterials.Solid(new Color(.4f, .3f, .28f)), gs + up * 4f, new Vector3(6.6f, 0, 8.6f), 1.8f, Quaternion.LookRotation(gside));
            mk.Box(dark, gs + up * 1.1f + toHeart * 3.01f, new Vector3(1.2f, 2.2f, .04f), grot);
            foreach (var s in new[] { -2.4f, 2.4f }) mk.Box(GlowM(Warm, 1.6f), gs + up * 1.8f + toHeart * 3.01f + gside * s, new Vector3(1.8f, 1.4f, .04f), grot);
            Porch(ctx, mk, solid, gs, toHeart, gside);
            ArtistBits.PointLight(ctx.Root, gs + up * 3f + toHeart * 5f, Warm, 14f, 1.4f); noGrass.Add(new Vector3(gs.x, 8f, gs.z));
            // a stone well, a washing line, a fire circle with log benches in the middle of the green
            var well = green + gside * 6f; well = ctx.OnGround(well.x, well.z); mk.Prism(stone, well, up, 1f, .9f, 12); mk.Prism(dark, well + up * .9f, up, .8f, .02f, 12); foreach (var s in new[] { -.85f, .85f }) mk.Box(wood, well + gside * s + up * 1.6f, new Vector3(.12f, 1.6f, .12f), Quaternion.identity);
            mk.Box(wood, well + up * 2.4f, new Vector3(2f, .1f, .1f), Quaternion.LookRotation(toHeart) * Quaternion.Euler(0, 90f, 0)); mk.GableRoof(WorldMaterials.Solid(new Color(.4f, .3f, .28f)), well + up * 2.5f, new Vector3(1.6f, 0, 2.2f), .5f, Quaternion.LookRotation(gside)); solid.Prism(stone, well, up, 1f, 1f, 8);
            var wl = green - gside * 7f; var wa = ctx.OnGround(wl.x + toHeart.x * 3f, wl.z + toHeart.z * 3f); var wb = ctx.OnGround(wl.x - toHeart.x * 3f, wl.z - toHeart.z * 3f);
            foreach (var p in new[] { wa, wb }) { mk.Prism(wood, p, up, .06f, 2f, 6); ArtistBits.PostCollider(ctx.Root, p, 2f, .1f); }
            Color[] cloth = { new Color(.95f, .95f, .9f), new Color(.6f, .75f, .95f), new Color(.95f, .7f, .6f), new Color(.8f, .9f, .7f) };
            for (var k = 0; k < 5; k++) { var p = Vector3.Lerp(wa, wb, .15f + k * .17f) + up * 1.55f; soft.Box(WorldMaterials.Solid(cloth[k % cloth.Length], .2f), p, new Vector3(.6f, .7f, .03f), Quaternion.LookRotation(gside)); }
            Campfire(ctx, mk, solid, green);
            for (var k = 0; k < 4; k++) { var d = Quaternion.Euler(0, k * 90f, 0) * toHeart; var lg = green + d * 3.6f; lg = ctx.OnGround(lg.x, lg.z) + up * .3f; mk.Prism(logM, lg - Vector3.Cross(up, d) * 1.3f, Vector3.Cross(up, d), .3f, 2.6f, 8); }
            noGrass.Add(new Vector3(green.x, 6f, green.z));
            var mark = new GameObject("Shot: the village").transform; mark.SetParent(ctx.Root, false); var cam = green + toHeart * 22f; cam = ctx.OnGround(cam.x, cam.z); mark.SetPositionAndRotation(cam, Quaternion.LookRotation(-toHeart));
            life.village = green;
        }

        void VillageCabin(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 at, Vector3 face)
        {
            face.y = 0; face.Normalize(); var up = Vector3.up; var rot = Quaternion.LookRotation(face); var side = Vector3.Cross(up, face);
            for (var k = 0; k < 7; k++) { mk.Prism(logM, at + up * (.25f + k * .45f) - side * 3f + face * 2.2f, side, .24f, 6f, 8); mk.Prism(logM, at + up * (.25f + k * .45f) - side * 3f - face * 2.2f, side, .24f, 6f, 8); mk.Prism(logM, at + up * (.45f + k * .45f) - face * 2.4f + side * 2.8f, face, .24f, 4.8f, 8); mk.Prism(logM, at + up * (.45f + k * .45f) - face * 2.4f - side * 2.8f, face, .24f, 4.8f, 8); }
            solid.Box(logM, at + up * 1.6f, new Vector3(6f, 3.2f, 4.8f), rot);
            mk.GableRoof(WorldMaterials.Solid(new Color(.35f, .32f, .3f)), at + up * 3.2f, new Vector3(5.6f, 0, 6.8f), 1.8f, Quaternion.LookRotation(side));
            mk.Box(dark, at + up * 1.1f + face * 2.45f, new Vector3(1f, 2f, .05f), rot);
            foreach (var s in new[] { -1.8f, 1.8f }) mk.Box(GlowM(Warm, 1.6f), at + up * 1.7f + face * 2.45f + side * s, new Vector3(.9f, .8f, .04f), rot);
            var ch = at + up * 4f + side * 2f; mk.Box(stone, ch, new Vector3(.8f, 2.2f, .8f), rot); life.chimneys.Add(ch + up * 1.2f);
            Porch(ctx, mk, solid, at, face, side);
            noGrass.Add(new Vector3(at.x, 6f, at.z));
        }

        void Campfire(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 at)
        {
            var up = Vector3.up;
            for (var k = 0; k < 8; k++) mk.Blob(stone, at + Quaternion.Euler(0, k * 45f, 0) * Vector3.forward * .9f + up * .15f, new Vector3(.25f, .18f, .22f), ctx.Rng, .3f);
            for (var k = 0; k < 3; k++) { var d = Quaternion.Euler(0, k * 120f + 30f, 0) * Vector3.forward; var lg = at + d * 2.6f + up * .25f; mk.Prism(logM, lg - Vector3.Cross(up, d), Vector3.Cross(up, d), .25f, 2f, 8); Interact.Bench(ctx.Root, lg + up * .3f - Vector3.Cross(up, d) * .6f, lg + up * .3f + Vector3.Cross(up, d) * .6f, -d); solid.Box(logM, lg, new Vector3(.5f, .5f, 2f), Quaternion.LookRotation(Vector3.Cross(up, d))); }
            var fl = ArtistBits.PointLight(ctx.Root, at + up * 1f, new Color(1f, .55f, .2f), 9f, 1.4f); fl.gameObject.AddComponent<FireFlicker>().baseIntensity = 1.4f;
            ArtistBits.PostCollider(ctx.Root, at, .5f, .9f);
            life.fires.Add(at + up * .25f);
            noGrass.Add(new Vector3(at.x, 4f, at.z));
        }

        void GroundCover(LandContext ctx)
        {
            var mat = WindMesh.Material(.3f, 1f, new Color(.32f, .5f, .3f));
            var meshes = new Dictionary<(int, int), WindMesh>(); var rng = new System.Random(1971);
            const float step = 2.3f; Color[] flowers = { new Color(.95f, .95f, .9f), new Color(.6f, .55f, 1f), new Color(1f, .85f, .3f) };
            for (var x = ctx.MinX; x < ctx.MaxX; x += step)
            for (var z = ctx.MinZ; z < ctx.MaxZ; z += step)
            {
                var px = x + ((float)rng.NextDouble() - .5f) * step; var pz = z + ((float)rng.NextDouble() - .5f) * step;
                if (!ctx.IsLand(px, pz, 1f) || NearPath(px, pz, 2.6f) || InNoGrass(px, pz, 0f) || InFootprint(ctx, px, pz, -2f) || InLake(px, pz, 2f) || OnRail(px, pz, 0f)) continue;
                if (Mathf.PerlinNoise(px * .025f + 3f, pz * .025f + 5f) < .38f) continue;
                var key = (Mathf.FloorToInt(px / 70f), Mathf.FloorToInt(pz / 70f));
                if (!meshes.TryGetValue(key, out var wm)) meshes[key] = wm = new WindMesh();
                var col = Color.Lerp(new Color(.32f, .5f, .3f), new Color(.45f, .58f, .35f), (float)rng.NextDouble());
                var at = new Vector3(px, ctx.Ground(px, pz) - .04f, pz);
                wm.Tuft(at, .4f + (float)rng.NextDouble() * .4f, col, Color.Lerp(col, new Color(.85f, .9f, .7f), .3f), rng, 4, .07f);
                if (rng.NextDouble() < .07) wm.Flower(at + new Vector3(.1f, 0, .1f), .35f, flowers[rng.Next(flowers.Length)], rng);
            }
            foreach (var kv in meshes) kv.Value.Build("Folk grass", ctx.Root, mat);
        }
    }
}
