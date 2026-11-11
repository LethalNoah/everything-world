using System.Collections.Generic;
using UnityEngine;

namespace EverythingWorld.World
{
    /// The Southern Rock Bayou (WORLD_GUIDE.md): cypress swamp and a slow brown river under a hazy, humid,
    /// golden late afternoon. Swampy and natural, green and wet, nothing like the dry red dirt the Delta
    /// Crossroads will have. A slow river winds through from the hills down to the sea, with sloughs off it ending
    /// in still lagoons; cypress stand in the shallows with their knees, live oaks hang with Spanish moss, palmettos
    /// crowd the banks. Where you arrive a juke joint stands on stilts under a giant oak. Dirt roads and plank
    /// bridges lead to every artist's porch; the big ones sit on stilts over their own fishing holes.
    /// Files: BayouKit (ground, river, juke joint, roads), BayouWilds, BayouArtists, BayouLife.
    public partial class BayouKit : LandKit
    {
        public override string Land => "The Southern Rock Bayou";
        public override Vector3 ArtistFront(IslandWorldData data, IslandWorldData.Artist a) => facing.TryGetValue(a.name, out var f) ? f : base.ArtistFront(data, a);

        public const string FreeBird = "5EWPGh7jbTNO2wakv8LjUI", BadMoon = "20OFwXhEXf12DzwXmaV7fj", GhostRiders = "4ncVYIs3UFvitjvc8RIPkM",
            ProudMary = "6pxWv6GV35VGmcPf5dh6CH", ElizabethReed = "4c9FL7GiiiB3HB6XnLERws", FireOnTheMountain = "5YUIDMCtzUflSrONeYMWac", BornOnTheBayou = "1Hz7lFNnz61BWmlYF10T2K";
        public override (string song, string label, string artist)[] SpecialSongs => new[]
        {
            (FreeBird, "Free Bird", "Lynyrd Skynyrd"), (BadMoon, "Bad Moon Rising", "Creedence Clearwater Revival"),
            (GhostRiders, "(Ghost) Riders in the Sky", "The Outlaws"), (ProudMary, "Proud Mary", "Creedence Clearwater Revival"),
            (FireOnTheMountain, "Fire on the Mountain", "The Marshall Tucker Band"), (BornOnTheBayou, "Born on the Bayou", "Creedence Clearwater Revival"),
            (ElizabethReed, "In Memory of Elizabeth Reed", "@the old cypress"),
            (Jessica, "Jessica", "@the open road"), (SimpleMan, "Simple Man", "@the farmhouse"), (CantYouSee, "Can't You See", "@the crossing"),
        };

        static readonly Color Cypress = new Color(.31f, .42f, .23f), Olive = new Color(.45f, .48f, .26f), Moss = new Color(.55f, .58f, .45f),
            Mud = new Color(.4f, .33f, .22f), Sand = new Color(.68f, .6f, .44f), Ink = new Color(.97f, .93f, .82f), Rust = new Color(.6f, .23f, .16f);
        const float RoadW = 4f, RiverW = 16f, SloughW = 10f;

        Material plank, greyWood, tin, dark, metal, bark, leaf, mossM, white, water, rope;
        GlowHalos halos;
        Vector3 center;
        BayouLife life;
        MeshKit soft;
        readonly Dictionary<(int, int), List<Vector3>> pathCells = new Dictionary<(int, int), List<Vector3>>();
        readonly List<(Vector3 p, int w)> walkPoints = new List<(Vector3, int)>();
        readonly Dictionary<string, Vector3> facing = new Dictionary<string, Vector3>();
        readonly List<Vector3> noGrass = new List<Vector3>();

        // the water (ShapeGround): channels as centre points (x, surface height, z) with a width; still ponds
        class Channel { public List<Vector3> pts = new List<Vector3>(); public float w; public bool main; }
        readonly List<Channel> channels = new List<Channel>();
        readonly List<(Vector3 c, float r)> ponds = new List<(Vector3, float)>();
        float seaLevel;

        void MakeMaterials()
        {
            plank = WorldMaterials.Solid(new Color(.52f, .4f, .27f)); greyWood = WorldMaterials.Solid(new Color(.55f, .52f, .46f));
            tin = WorldMaterials.Solid(new Color(.62f, .6f, .56f), .6f); dark = WorldMaterials.Solid(new Color(.1f, .09f, .08f));
            metal = WorldMaterials.Solid(new Color(.7f, .7f, .72f), .7f); bark = WorldMaterials.Solid(new Color(.42f, .36f, .3f));
            leaf = WorldMaterials.Solid(new Color(.33f, .45f, .22f)); mossM = WorldMaterials.Solid(new Color(.6f, .64f, .52f));
            white = WorldMaterials.Solid(new Color(.93f, .91f, .85f)); rope = WorldMaterials.Solid(new Color(.75f, .65f, .45f));
            water = new Material(WorldMaterials.Glow(new Color(.2f, .2f, .12f), .06f)) { name = "Bayou water" };     // slow dark brown-green: shinier reads white in the haze
            water.SetFloat("_Smoothness", .45f);
        }

        Material GlowM(Color c, float s = 2f) => WorldMaterials.Glow(c, s * .65f);
        static bool HasStation(IslandWorldData.Artist a) => a.station || a.stationId != 0;
        static float Foot(IslandWorldData.Artist a) => HasStation(a) ? 16f : 9f;

        public override bool Ground(float height, float slope, float patch, out Color color)
        {
            if (height < seaLevel + 1.2f) color = Color.Lerp(Sand, Mud, patch);
            else if (slope > 9f) color = Color.Lerp(Mud, new Color(.5f, .42f, .3f), patch);
            else if (patch > .72f) color = Color.Lerp(Olive, Moss, (patch - .72f) / .28f);
            else color = Color.Lerp(Cypress, Olive, Mathf.SmoothStep(.1f, .7f, patch));
            color.a = 1f;
            return true;
        }

        // ---------------------------------------------------------------- shaping: river, sloughs, porches
        public override void ShapeGround(IslandWorldData data, int index)
        {
            var isl = data.layout.islands[index];
            var heart = new Vector2(isl.labelX, isl.labelZ);
            seaLevel = data.layout.waterLevel;
            channels.Clear(); ponds.Clear();
            int Id(float x, float z)
            {
                var cc = Mathf.RoundToInt(x / data.Step); var rr = Mathf.RoundToInt((data.layout.depth - z) / data.Step);
                if (cc < 0 || rr < 0 || cc >= data.Nx || rr >= data.Nz) return 255;
                return data.islandIds[rr * data.Nx + cc];
            }
            bool Inland(Vector2 p, float m)
            {
                if (Id(p.x, p.y) != index) return false;
                for (var k = 0; k < 12; k++) { var a = k * Mathf.PI / 6f; if (Id(p.x + Mathf.Cos(a) * m, p.y + Mathf.Sin(a) * m) != index) return false; }
                return true;
            }

            // downstream: the shortest way from the heart to the open sea (not into another land)
            var down = Vector2.down; var bestLen = float.MaxValue;
            for (var k = 0; k < 24; k++)
            {
                var a = k * Mathf.PI / 12f; var d = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                for (var t = 20f; t < 900f; t += 10f)
                {
                    var id = Id(heart.x + d.x * t, heart.y + d.y * t);
                    if (id == index) continue;
                    if (id == 255 && t < bestLen) { bestLen = t; down = d; }
                    break;
                }
            }
            var perp = new Vector2(-down.y, down.x);
            // upstream end: back into the hills as far as stays 70 m inside the land
            var up = 0f; while (up < 400f && Inland(heart - down * (up + 10f) + perp * 30f, 70f)) up += 10f;
            var main = new Channel { w = RiverW, main = true };
            for (var t = -up; t < 1200f; t += 4f)
            {
                var q = heart + down * t + perp * (30f + Mathf.Sin((t + up) / 170f * Mathf.PI * 2f) * 24f * Mathf.Clamp01((t + up) / 60f));
                var id = Id(q.x, q.y);
                if (t > 0f && id != index) { main.pts.Add(new Vector3(q.x, 0, q.y)); if (id == 255 && data.HeightAt(q.x, q.y) < seaLevel - 1f) break; continue; }
                main.pts.Add(new Vector3(q.x, 0, q.y));
            }
            Profile(data, main, float.MaxValue);
            channels.Add(main);

            // sloughs: dead-end side channels off the upper river, each ending in a still lagoon
            var tries = new List<int>();
            for (var i = 0; i < main.pts.Count; i += 6) { var p = main.pts[i]; if (Vector2.Distance(new Vector2(p.x, p.z), heart) < 200f && Vector2.Distance(new Vector2(p.x, p.z), heart) > 50f) tries.Add(i); }
            var rnd = new System.Random(1973);
            for (var n = 0; n < tries.Count && channels.Count < 4; n++)
            {
                var i = tries[rnd.Next(tries.Count)]; var j = main.pts[i];
                var along = new Vector2(main.pts[Mathf.Min(i + 1, main.pts.Count - 1)].x - j.x, main.pts[Mathf.Min(i + 1, main.pts.Count - 1)].z - j.z).normalized;
                foreach (var s in new[] { 1f, -1f })
                {
                    var dir = (new Vector2(-along.y, along.x) * s + along * .35f).normalized; var len = 70f + (float)rnd.NextDouble() * 50f;
                    var ok = true; var sl = new Channel { w = SloughW };
                    for (var t = 0f; t <= len; t += 4f)
                    {
                        var q = new Vector2(j.x, j.z) + dir * t + new Vector2(-dir.y, dir.x) * Mathf.Sin(t / 60f * Mathf.PI) * 9f;
                        if (!Inland(q, 55f) || Vector2.Distance(q, heart) < 45f || data.HeightAt(q.x, q.y) > j.y + 10f) { ok = false; break; }
                        if (t > 45f) foreach (var c in channels) foreach (var o in c.pts) if (Vector2.Distance(q, new Vector2(o.x, o.z)) < 35f) { ok = false; break; }
                        if (!ok) break;
                        sl.pts.Add(new Vector3(q.x, j.y, q.y));
                    }
                    if (!ok || sl.pts.Count < 10) continue;
                    channels.Add(sl);
                    var end = sl.pts[sl.pts.Count - 1]; ponds.Add((end, 17f));
                    break;
                }
            }

            FindSwamp(data, index, heart);
            Carve(data, index, heart);
            RaiseTreeIsland(data, index);

            // the artists: round the heart, clear of the water
            var avoid = new List<Vector3>();
            foreach (var c in channels) for (var i = 0; i < c.pts.Count; i += 4) avoid.Add(new Vector3(c.pts[i].x, c.w / 2f + 9f, c.pts[i].z));
            foreach (var (c, r) in ponds) avoid.Add(new Vector3(c.x, r + 9f, c.z));
            ArtistPlacement.Spread(data, index, 80f, heart, 50f, 48f, avoid, null, Land, 280f);
            // Creedence sit right on the river bank, so the steamer is moored behind their porch
            IslandWorldData.Artist ccr = null;
            foreach (var a in data.layout.artists) if (a.sceneId == isl.sceneId && a.name == "Creedence Clearwater Revival") ccr = a;
            if (ccr != null)
            {
                var bestD = float.MaxValue; var bestP = Vector2.zero; var need = Foot(ccr) + 8f;
                for (var i = 2; i + 2 < main.pts.Count; i += 2)
                {
                    var q = new Vector2(main.pts[i].x, main.pts[i].z); var dh = Vector2.Distance(q, heart);
                    if (dh < 70f || dh > 230f) continue;
                    var along = new Vector2(main.pts[i + 2].x - main.pts[i - 2].x, main.pts[i + 2].z - main.pts[i - 2].z).normalized; var n = new Vector2(-along.y, along.x);
                    foreach (var s in new[] { 1f, -1f })
                    {
                        var cand = q + n * s * (RiverW / 2f + Foot(ccr) + 6f);
                        if (!Inland(cand, 50f) || Vector2.Distance(cand, heart) < 60f) continue;
                        var ok = true;
                        foreach (var o in data.layout.artists) if (o != ccr && o.sceneId == isl.sceneId && Vector2.Distance(cand, new Vector2(o.x, o.z)) < Mathf.Max(o.r, Foot(o) + 8f) + need + 6f) ok = false;
                        foreach (var c in channels) foreach (var o in c.pts) if (Vector2.Distance(cand, new Vector2(o.x, o.z)) < c.w / 2f + Foot(ccr) + 4f) ok = false;
                        foreach (var (pc, pr) in ponds) if (Vector2.Distance(cand, new Vector2(pc.x, pc.z)) < pr + Foot(ccr) + 6f) ok = false;
                        if (!ok) continue;
                        var d = Mathf.Abs(dh - 130f);
                        if (d < bestD) { bestD = d; bestP = cand; }
                    }
                }
                if (bestD < float.MaxValue) { ccr.x = bestP.x; ccr.z = bestP.y; }
            }
            foreach (var a in data.layout.artists)
            {
                if (a.sceneId != isl.sceneId) continue;
                a.r = Mathf.Max(a.r, Foot(a) + 8f);
                Level(data, index, new Vector2(a.x, a.z), Foot(a) + 1f, Foot(a) + 14f);
                if (!HasStation(a) || a == ccr) continue;
                // a fishing hole behind the big porches, so they stand on stilts over water
                var away = new Vector2(a.x, a.z) - heart; away = away.sqrMagnitude < 1f ? Vector2.up : away.normalized;
                var pc = new Vector2(a.x, a.z) + away * 8f; var lvl = data.HeightAt(a.x, a.z) - .9f;
                Dig(data, index, pc, 7.5f, lvl);
                ponds.Add((new Vector3(pc.x, lvl, pc.y), 7.5f));
            }
            Level(data, index, heart, 14f, 32f);
        }

        /// The water surface along a channel: never above the ground it runs through (a metre under it), never
        /// rising downstream, never below the sea.
        void Profile(IslandWorldData data, Channel c, float start)
        {
            var s = start;
            for (var i = 0; i < c.pts.Count; i++)
            {
                var p = c.pts[i]; var dir = i + 1 < c.pts.Count ? (c.pts[i + 1] - p).normalized : Vector3.forward; var side = Vector3.Cross(Vector3.up, dir) * (c.w / 2f + 2f);
                var g = Mathf.Min(data.HeightAt(p.x, p.z), Mathf.Min(data.HeightAt(p.x + side.x, p.z + side.z), data.HeightAt(p.x - side.x, p.z - side.z)));
                s = Mathf.Max(seaLevel + .05f, Mathf.Min(s, g - 1f));
                p.y = s; c.pts[i] = p;
            }
        }

        static float SegDist(Vector2 p, Vector2 a, Vector2 b, out float t)
        {
            var ab = b - a; t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / Mathf.Max(ab.sqrMagnitude, .0001f));
            return Vector2.Distance(p, a + ab * t);
        }

        /// Dig every channel and lagoon: a smooth bed under the water, low banks just above it blending out to
        /// the land over 12 m.
        void Carve(IslandWorldData data, int index, Vector2 heart)
        {
            float minX = float.MaxValue, maxX = float.MinValue, minZ = float.MaxValue, maxZ = float.MinValue;
            foreach (var c in channels) foreach (var p in c.pts) { minX = Mathf.Min(minX, p.x); maxX = Mathf.Max(maxX, p.x); minZ = Mathf.Min(minZ, p.z); maxZ = Mathf.Max(maxZ, p.z); }
            foreach (var (p, pr) in ponds) { minX = Mathf.Min(minX, p.x - pr); maxX = Mathf.Max(maxX, p.x + pr); minZ = Mathf.Min(minZ, p.z - pr); maxZ = Mathf.Max(maxZ, p.z + pr); }
            const float bank = 12f;
            for (var r = 0; r < data.Nz; r++)
            {
                var z = data.layout.depth - r * data.Step; if (z < minZ - 80f || z > maxZ + 80f) continue;
                for (var col = 0; col < data.Nx; col++)
                {
                    var x = col * data.Step; if (x < minX - 80f || x > maxX + 80f) continue;
                    var i = r * data.Nx + col; if (data.islandIds[i] != index) continue;
                    var p = new Vector2(x, z);
                    float best = float.MaxValue, s = 0f, hw = 0f;
                    foreach (var c in channels)
                        for (var k = 0; k + 1 < c.pts.Count; k++)
                        {
                            var a = c.pts[k]; var b = c.pts[k + 1];
                            if (Mathf.Abs(a.x - x) > 90f || Mathf.Abs(a.z - z) > 90f) continue;
                            var d = SegDist(p, new Vector2(a.x, a.z), new Vector2(b.x, b.z), out var t) - c.w / 2f;
                            if (d < best) { best = d; s = Mathf.Lerp(a.y, b.y, t); hw = c.w / 2f; }
                        }
                    foreach (var (pc, pr) in ponds)
                    {
                        var d = Vector2.Distance(p, new Vector2(pc.x, pc.z)) - pr;
                        if (d < best) { best = d; s = pc.y; hw = pr; }
                    }
                    if (best > 80f) continue;
                    var h = data.heights[i];
                    if (best < 0f) { var f = 1f + best / hw; data.heights[i] = s - .3f - 1.4f * (1f - f * f); }
                    // low ground beside the water: a gentle levee blending out; high ground: a wide valley sloping
                    // down to the water at about 1 in 4, so the river never runs in a steep ravine
                    else if (h < s + .35f) { if (best < bank) data.heights[i] = Mathf.Lerp(s + .35f, h, Mathf.SmoothStep(0, 1, best / bank)); }
                    else data.heights[i] = Mathf.Min(h, s + .35f + best * .24f);
                }
            }
        }

        void Dig(IslandWorldData data, int index, Vector2 c, float rad, float level)
        {
            for (var r = 0; r < data.Nz; r++)
            {
                var z = data.layout.depth - r * data.Step; if (Mathf.Abs(z - c.y) > rad + 8f) continue;
                for (var col = 0; col < data.Nx; col++)
                {
                    var x = col * data.Step; if (Mathf.Abs(x - c.x) > rad + 8f) continue;
                    var i = r * data.Nx + col; if (data.islandIds[i] != index) continue;
                    var d = Vector2.Distance(new Vector2(x, z), c);
                    if (d < rad) { var f = d / rad; data.heights[i] = level - .3f - 1.4f * (1f - f * f); }
                    else if (d < rad + 8f) data.heights[i] = Mathf.Lerp(level + .35f, data.heights[i], Mathf.SmoothStep(0, 1, (d - rad) / 8f));
                }
            }
        }

        /// Flatten ground for a building, leaving water alone.
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
                    if (WaterAt(x, z, 3f, out _)) continue;
                    var d = Vector2.Distance(new Vector2(x, z), c);
                    if (d < flat) data.heights[i] = level;
                    else if (d < blend) data.heights[i] = Mathf.Lerp(level, data.heights[i], Mathf.SmoothStep(0, 1, (d - flat) / (blend - flat)));
                }
            }
        }

        /// Is (x, z) over water (within pad of a channel or pond), and the water's height there.
        bool WaterAt(float x, float z, float pad, out float surface)
        {
            surface = 0f; var p = new Vector2(x, z); var best = float.MaxValue;
            foreach (var c in channels)
                for (var k = 0; k + 1 < c.pts.Count; k++)
                {
                    var a = c.pts[k]; var b = c.pts[k + 1];
                    if (Mathf.Abs(a.x - x) > 40f || Mathf.Abs(a.z - z) > 40f) continue;
                    var d = SegDist(p, new Vector2(a.x, a.z), new Vector2(b.x, b.z), out var t) - c.w / 2f;
                    if (d < best) { best = d; surface = Mathf.Lerp(a.y, b.y, t); }
                }
            foreach (var (pc, pr) in ponds) { var d = Vector2.Distance(p, new Vector2(pc.x, pc.z)) - pr; if (d < best) { best = d; surface = pc.y; } }
            return best < pad;
        }

        // ---------------------------------------------------------------- dressing
        public override void Dress(LandContext ctx)
        {
            MakeMaterials();
            halos = new GlowHalos();
            pathCells.Clear(); walkPoints.Clear(); facing.Clear(); noGrass.Clear();
            center = ctx.Plaza;
            life = new GameObject("Bayou life").AddComponent<BayouLife>();
            life.transform.SetParent(ctx.Root, false);
            life.center = center;
            var mk = new MeshKit(); var solid = new MeshKit(); soft = new MeshKit();
            Water(ctx, mk);
            JukeJoint(ctx, mk, solid);
            Roads(ctx, mk, solid);
            Swamp(ctx, mk, solid);
            SongPlaces(ctx, mk, solid);
            Wilds(ctx, mk, solid);
            mk.Build("Bayou", ctx.Root, false);
            solid.BuildCollider("Bayou walls", ctx.Root);
        }

        public override void Finish(LandContext ctx)
        {
            GroundCover(ctx);
            halos.Build("Bayou glow", ctx.Root);
            soft.Build("Soft bayou", ctx.Root, false);   // last, so the artists' leaves are in it
        }

        void Water(LandContext ctx, MeshKit mk)
        {
            foreach (var c in channels)
            {
                var pts = new List<Vector3>();
                foreach (var p in c.pts) pts.Add(p + Vector3.down * .1f);
                if (pts.Count > 1) Strip(mk, pts, water, c.w + 6f, 0f);
                if (c.main) life.river = c.pts.ToArray();
            }
            foreach (var (pc, pr) in ponds)
            {
                const int n = 28; var cc = pc + Vector3.down * .1f;
                for (var k = 0; k < n; k++)
                {
                    float a0 = k * Mathf.PI * 2f / n, a1 = (k + 1) * Mathf.PI * 2f / n;
                    mk.Tri(water, cc, cc + new Vector3(Mathf.Cos(a0), 0, Mathf.Sin(a0)) * (pr + 3f), cc + new Vector3(Mathf.Cos(a1), 0, Mathf.Sin(a1)) * (pr + 3f), cc - Vector3.up);
                }
                life.pools.Add(pc);
            }
            // lily pads, some flowering, in the still water
            var pad = WorldMaterials.Solid(new Color(.35f, .5f, .25f)); var flower = WorldMaterials.Solid(new Color(.98f, .92f, .85f));
            foreach (var c in channels)
                for (var i = 0; i < c.pts.Count; i += 3)
                {
                    if (!ctx.Chance(c.main ? .25f : .6f)) continue;
                    var p = c.pts[i]; var side = Vector3.Cross(Vector3.up, (c.pts[Mathf.Min(i + 1, c.pts.Count - 1)] - p).normalized); if (side.sqrMagnitude < .1f) continue;
                    var q = p + side * (ctx.Chance(.5f) ? 1 : -1) * ctx.Range(c.w * .2f, c.w * .45f) + Vector3.up * -.07f;
                    for (var k = 0; k < ctx.Rng.Next(2, 6); k++)
                    {
                        var lp = q + new Vector3(ctx.Range(-1.5f, 1.5f), 0, ctx.Range(-1.5f, 1.5f));
                        soft.Prism(pad, lp, Vector3.up, ctx.Range(.3f, .6f), .02f, 8);
                        if (ctx.Chance(.15f)) soft.Blob(flower, lp + Vector3.up * .1f, new Vector3(.15f, .1f, .15f), ctx.Rng, .3f);
                    }
                }
        }

        // ---------------------------------------------------------------- helpers
        void Remember(Vector3 p, int walk)
        {
            var key = (Mathf.FloorToInt(p.x / 8f), Mathf.FloorToInt(p.z / 8f));
            if (!pathCells.TryGetValue(key, out var l)) pathCells[key] = l = new List<Vector3>();
            l.Add(p); walkPoints.Add((p, walk));
        }

        bool NearPath(float x, float z, float within)
        {
            int cx = Mathf.FloorToInt(x / 8f), cz = Mathf.FloorToInt(z / 8f);
            var n = Mathf.CeilToInt(within / 8f);
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

        bool InFootprint(LandContext ctx, float x, float z, float pad, float scale = 1f)
        {
            foreach (var a in ctx.Artists) { float dx = x - a.x, dz = z - a.z, r = Foot(a) * scale + pad; if (dx * dx + dz * dz < r * r) return true; }
            return false;
        }

        bool Open(LandContext ctx, float x, float z, float pad) =>
            ctx.IsLand(x, z, 4f) && !InFootprint(ctx, x, z, pad) && !NearPath(x, z, pad + 2f) && !InNoGrass(x, z, pad) && !WaterAt(x, z, pad, out _)
            && Vector2.Distance(new Vector2(x, z), new Vector2(center.x, center.z)) > 24f + pad;

        /// A hurricane lantern hung on a crooked wooden post.
        Renderer Lamp(LandContext ctx, MeshKit mk, Transform parent, Vector3 g, float h = 2.8f)
        {
            mk.Prism(greyWood, g - Vector3.up * .3f, Vector3.up, .1f, h + .5f, 6);
            mk.Box(greyWood, g + Vector3.up * (h + .1f) + Vector3.right * .3f, new Vector3(.7f, .1f, .1f), Quaternion.identity);
            var go = new GameObject("Lantern"); go.transform.SetParent(parent, false);
            var lk = new MeshKit(); lk.Box(GlowM(new Color(1f, .78f, .4f), 2.4f), Vector3.zero, new Vector3(.25f, .35f, .25f), Quaternion.identity);
            var built = lk.BuildSingle("Glass", go.transform, false); go.transform.position = g + Vector3.up * (h - .3f) + Vector3.right * .55f;
            halos.Add(go.transform.position, 1.4f, new Color(1f, .75f, .35f), .8f);
            ArtistBits.PostCollider(ctx.Root, g, h, .12f);
            life.lanterns.Add(built.GetComponent<Renderer>());
            return built.GetComponent<Renderer>();
        }

        void Strip(MeshKit mk, List<Vector3> pts, Material m, float w, float lift)
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
            }
            for (var i = 0; i + 1 < n; i++) mk.Quad(m, L[i], R[i], R[i + 1], L[i + 1], (pts[i] + pts[i + 1]) / 2f - Vector3.up);
        }

        // ---------------------------------------------------------------- where you arrive: the juke joint
        /// A juke joint on stilts under a giant live oak hung with Spanish moss: a tin-roofed shack with a deep
        /// porch, rocking chairs and a slide guitar on it, string lights, hand-painted signs, a stage inside,
        /// motorcycles and a pickup out front.
        void JukeJoint(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var c = ctx.OnGround(center.x, center.z);
            // the giant oak, on the far side of the plaza from the river
            var river = channels.Count > 0 ? NearestOn(channels[0], c) : c + Vector3.forward * 30f;
            var toRiver = new Vector3(river.x - c.x, 0, river.z - c.z).normalized; if (toRiver.sqrMagnitude < .1f) toRiver = Vector3.forward;
            var oakAt = ctx.OnGround(c.x - toRiver.x * 16f, c.z - toRiver.z * 16f);
            LiveOak(ctx, mk, oakAt, 2.6f);
            // the shack faces the plaza, its back towards the oak
            var face = -Vector3.Cross(Vector3.up, toRiver); var rot = Quaternion.LookRotation(face); var side = Vector3.Cross(up, face);
            var at = ctx.OnGround(c.x - toRiver.x * 7f - face.x * 10f, c.z - toRiver.z * 7f - face.z * 10f);
            const float W = 12f, D = 9f, H = 4f, F = 1.6f;
            var floor = at + up * F;
            for (var i = -1; i <= 1; i++) for (var j = -1; j <= 1; j++) { var s = at + side * i * (W / 2f - .3f) + face * j * (D / 2f + 1.2f); mk.Prism(greyWood, s - up * .5f, up, .18f, F + .5f, 6); }
            mk.Box(plank, floor - up * .1f + face * 1.2f, new Vector3(W, .2f, D + 3.6f), rot);
            solid.Box(plank, floor - up * (F / 2f) + face * 1.2f, new Vector3(W, F, D + 3.6f), rot);
            // steps up to the porch
            for (var k = 0; k < 5; k++) { var sp = at + face * (D / 2f + 3.4f + (4 - k) * .45f) + up * (k * .32f + .16f); mk.Box(plank, sp, new Vector3(3f, .32f, .5f), rot); solid.Box(plank, sp, new Vector3(3f, .32f, .5f), rot); }
            // walls: back, two sides, front with a wide doorway and windows
            var wallM = WorldMaterials.Solid(new Color(.5f, .45f, .36f));
            mk.Box(wallM, floor + up * H / 2f - face * (D / 2f - .1f), new Vector3(W, H, .2f), rot);
            foreach (var s in new[] { -1f, 1f }) mk.Box(wallM, floor + up * H / 2f + side * s * (W / 2f - .1f), new Vector3(.2f, H, D - .4f), rot);
            foreach (var s in new[] { -1f, 1f }) mk.Box(wallM, floor + up * H / 2f + face * (D / 2f - .1f) + side * s * (W / 4f + .7f), new Vector3(W / 2f - 1.4f, H, .2f), rot);
            mk.Box(wallM, floor + up * (H - .5f) + face * (D / 2f - .1f), new Vector3(2.8f, 1f, .2f), rot);
            solid.Box(wallM, floor + up * H / 2f - face * (D / 2f - .1f), new Vector3(W, H, .2f), rot);
            foreach (var s in new[] { -1f, 1f }) solid.Box(wallM, floor + up * H / 2f + side * s * (W / 2f - .1f), new Vector3(.2f, H, D), rot);
            foreach (var s in new[] { -1f, 1f }) solid.Box(wallM, floor + up * H / 2f + face * (D / 2f - .1f) + side * s * (W / 4f + .7f), new Vector3(W / 2f - 1.4f, H, .2f), rot);
            foreach (var s in new[] { -1f, 1f }) mk.Box(GlowM(new Color(1f, .7f, .35f), 1.8f), floor + up * 2f + face * (D / 2f + .01f) + side * s * (W / 4f + .7f), new Vector3(1.4f, 1f, .04f), rot);
            // tin roof over the shack and the porch
            mk.GableRoof(tin, floor + up * H + face * 1.2f, new Vector3(D + 4.4f, 0, W + 1f), 2.2f, Quaternion.LookRotation(side));
            foreach (var s in new[] { -1f, 1f }) { var post = floor + face * (D / 2f + 3.2f) + side * s * (W / 2f - .3f); mk.Prism(greyWood, post, up, .12f, H, 6); ArtistBits.PostCollider(ctx.Root, post, H, .15f); }
            // the porch rail, open at the steps
            foreach (var s in new[] { -1f, 1f }) { var rp = floor + face * (D / 2f + 3.3f) + side * s * (W / 4f + .9f) + up * .9f; mk.Box(greyWood, rp, new Vector3(W / 2f - 1.8f, .1f, .1f), rot); solid.Box(greyWood, rp - up * .4f, new Vector3(W / 2f - 1.8f, 1f, .2f), rot); }
            // rocking chairs and a slide guitar on the porch
            foreach (var s in new[] { -3.6f, -2.2f, 3f }) RockingChair(mk, solid, floor + face * (D / 2f + 1.6f) + side * s, face);
            var gtr = floor + face * (D / 2f + .6f) + side * 1.6f;
            mk.Blob(WorldMaterials.Solid(new Color(.75f, .45f, .2f), .7f), gtr + up * .5f, new Vector3(.38f, .45f, .12f), ctx.Rng, 0f);
            mk.Box(dark, gtr + up * 1.25f, new Vector3(.07f, 1f, .05f), rot * Quaternion.Euler(0, 0, -12f));
            mk.Prism(metal, gtr + up * .9f + face * .1f, up, .05f, .2f, 8);
            // inside: a little stage, a bar, a lamp
            mk.Box(plank, floor + up * .25f - face * (D / 2f - 1.8f), new Vector3(W - 1f, .5f, 3.2f), rot);
            solid.Box(plank, floor + up * .25f - face * (D / 2f - 1.8f), new Vector3(W - 1f, .5f, 3.2f), rot);
            mk.Box(dark, floor + up * .55f + side * (W / 2f - 1f), new Vector3(1f, 1.1f, D - 3f), rot); solid.Box(dark, floor + up * .55f + side * (W / 2f - 1f), new Vector3(1f, 1.1f, D - 3f), rot);
            ArtistBits.PointLight(ctx.Root, floor + up * 3f, new Color(1f, .65f, .35f), 12f, 1.6f, true);
            IndoorSpaces.Add(floor + up * H / 2f, rot, new Vector3(W, H, D));
            // signs over the porch
            ArtistBits.Label(ctx.Root, "JUKE JOINT", floor + up * (H + .7f) + face * (D / 2f + 3.35f), face, .45f, new Color(1f, .55f, .25f), TextAnchor.MiddleCenter);
            mk.Box(GlowM(new Color(1f, .5f, .2f), 1.6f), floor + up * (H + .7f) + face * (D / 2f + 3.3f), new Vector3(5f, .9f, .04f), rot);
            ArtistBits.Label(ctx.Root, "THE SOUTHERN ROCK BAYOU", floor + up * (H - .4f) + face * (D / 2f + 3.4f), face, .22f, Ink, TextAnchor.MiddleCenter);
            // string lights from the porch to the oak and round the plaza
            var bulbs = new List<Vector3>();
            for (var k = 0; k < 4; k++)
            {
                var a = floor + face * (D / 2f + 3.2f) + side * ((k - 1.5f) * 3.2f) + up * (H - .2f);
                var b = ctx.OnGround(c.x + side.x * (k - 1.5f) * 6f + face.x * 6f, c.z + side.z * (k - 1.5f) * 6f + face.z * 6f) + up * 4f;
                if (k % 3 == 0) { mk.Prism(greyWood, b - up * 4.3f, up, .1f, 4.3f, 6); ArtistBits.PostCollider(ctx.Root, b - up * 4f, 4f, .12f); }
                for (var t = .1f; t < 1f; t += .14f) bulbs.Add(Vector3.Lerp(a, b, t) - up * Mathf.Sin(t * Mathf.PI) * .9f);
            }
            foreach (var b in bulbs) { mk.Blob(GlowM(new Color(1f, .82f, .45f), 2.6f), b, Vector3.one * .1f, ctx.Rng, 0f); }
            foreach (var b in bulbs) halos.Add(b, .45f, new Color(1f, .8f, .45f), .7f);
            // motorcycles and a pickup out front
            Motorcycle(ctx, mk, solid, ctx.OnGround(c.x + side.x * 9f + face.x * 3f, c.z + side.z * 9f + face.z * 3f), Quaternion.Euler(0, 70f, 0) * face, new Color(.15f, .15f, .16f));
            Motorcycle(ctx, mk, solid, ctx.OnGround(c.x + side.x * 10.5f + face.x * 1.5f, c.z + side.z * 10.5f + face.z * 1.5f), Quaternion.Euler(0, 75f, 0) * face, new Color(.6f, .15f, .12f));
            Pickup(ctx, mk, solid, ctx.OnGround(c.x - side.x * 11f + face.x * 2f, c.z - side.z * 11f + face.z * 2f), Quaternion.Euler(0, -100f, 0) * face, new Color(.35f, .5f, .6f));
            // a fire pit with logs round it in the plaza
            var pit = ctx.OnGround(c.x + face.x * 4f, c.z + face.z * 4f);
            for (var k = 0; k < 8; k++) { var d = Quaternion.Euler(0, k * 45f, 0) * Vector3.forward; mk.Blob(greyWood, pit + d * .9f + up * .15f, new Vector3(.25f, .2f, .25f), ctx.Rng, .3f); }
            for (var k = 0; k < 4; k++) { var d = Quaternion.Euler(0, k * 90f + 45f, 0) * Vector3.forward; var lg = pit + d * 3f + up * .25f; mk.Prism(bark, lg - Vector3.Cross(up, d) * 1f, Vector3.Cross(up, d), .25f, 2f, 8); solid.Box(bark, lg, new Vector3(.5f, .5f, 2f), Quaternion.LookRotation(Vector3.Cross(up, d))); }
            ArtistBits.PostCollider(ctx.Root, pit, .5f, 1f);
            var fl = ArtistBits.PointLight(ctx.Root, pit + up * 1f, new Color(1f, .55f, .2f), 12f, 1.8f); fl.gameObject.AddComponent<FireFlicker>().baseIntensity = 1.8f;
            life.fires.Add(pit + up * .3f);
            noGrass.Add(new Vector3(c.x, 16f, c.z));
            Remember(c, 0);
        }

        static Vector3 NearestOn(Channel ch, Vector3 p)
        {
            var best = ch.pts[0]; var bd = float.MaxValue;
            foreach (var q in ch.pts) { var d = (q.x - p.x) * (q.x - p.x) + (q.z - p.z) * (q.z - p.z); if (d < bd) { bd = d; best = q; } }
            return best;
        }

        void RockingChair(MeshKit mk, MeshKit solid, Vector3 at, Vector3 face)
        {
            var up = Vector3.up; var rot = Quaternion.LookRotation(face); var side = Vector3.Cross(up, face);
            foreach (var s in new[] { -.28f, .28f }) mk.Box(plank, at + side * s + up * .08f, new Vector3(.06f, .08f, .9f), rot * Quaternion.Euler(0, 0, 0));
            mk.Box(plank, at + up * .45f, new Vector3(.6f, .06f, .55f), rot);
            mk.Box(plank, at + up * .9f - face * .27f, new Vector3(.6f, .9f, .06f), rot * Quaternion.Euler(-10f, 0, 0));
            foreach (var s in new[] { -.28f, .28f }) mk.Box(plank, at + side * s + up * .65f, new Vector3(.05f, .05f, .5f), rot);
            solid.Box(plank, at + up * .5f, new Vector3(.7f, 1f, .7f), rot);
        }

        void Motorcycle(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 at, Vector3 dir, Color paint)
        {
            var up = Vector3.up; dir.y = 0; dir.Normalize(); var rot = Quaternion.LookRotation(dir); var side = Vector3.Cross(up, dir);
            var lean = Quaternion.AngleAxis(10f, dir); var body = WorldMaterials.Solid(paint, .85f);
            foreach (var z in new[] { -.75f, .8f }) mk.Prism(dark, at + dir * z + up * .35f - side * .06f, side, .35f, .12f, 12);
            mk.Box(body, at + lean * (up * .75f) + dir * .2f, new Vector3(.3f, .25f, .6f), rot);
            mk.Box(dark, at + lean * (up * .8f) - dir * .35f, new Vector3(.3f, .1f, .6f), rot);
            mk.Box(metal, at + lean * (up * .55f), new Vector3(.25f, .3f, .4f), rot);
            mk.Box(metal, at + lean * (up * 1.05f) + dir * .7f, new Vector3(.8f, .05f, .05f), rot);
            mk.Box(metal, at + lean * (up * .7f) + dir * .75f, new Vector3(.05f, .7f, .05f), rot * Quaternion.Euler(25f, 0, 0));
            mk.Prism(GlowM(new Color(1f, .9f, .7f), 1.6f), at + lean * (up * .85f) + dir * .85f, dir, .1f, .05f, 8);
            solid.Box(dark, at + up * .6f, new Vector3(.5f, 1.2f, 2f), rot);
        }

        void Pickup(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 at, Vector3 dir, Color paint)
        {
            var up = Vector3.up; dir.y = 0; dir.Normalize(); var rot = Quaternion.LookRotation(dir); var side = Vector3.Cross(up, dir);
            var body = WorldMaterials.Solid(paint, .6f); var rusty = WorldMaterials.Solid(Rust);
            mk.Box(body, at + up * .85f, new Vector3(2f, .7f, 5f), rot);
            mk.Box(body, at + up * 1.5f + dir * .5f, new Vector3(1.9f, .7f, 1.6f), rot);
            mk.Box(WorldMaterials.Solid(new Color(.15f, .18f, .2f), .9f), at + up * 1.55f + dir * .5f, new Vector3(1.92f, .5f, 1.5f), rot);
            mk.Box(rusty, at + up * 1.2f - dir * 1.5f + side * .99f, new Vector3(.03f, .3f, 1.6f), rot);
            mk.Box(dark, at + up * .7f - dir * 1.5f, new Vector3(1.8f, .1f, 1.9f), rot);
            foreach (var s in new[] { -1f, 1f }) foreach (var z in new[] { -1.6f, 1.6f }) mk.Prism(dark, at + side * s * .9f + dir * z + up * .45f, side * s, .45f, .28f, 12);
            foreach (var s in new[] { -.7f, .7f }) mk.Prism(GlowM(new Color(1f, .95f, .8f), 1.4f), at + up * .95f + dir * 2.5f + side * s, dir, .15f, .05f, 10);
            solid.Box(body, at + up * 1f, new Vector3(2.1f, 2f, 5.1f), rot);
        }

        // ---------------------------------------------------------------- roads
        /// Dirt roads from the juke joint to every artist, branching (so they never cross), with plank bridges
        /// on posts where they cross water and lanterns on posts along the way.
        void Roads(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var todo = new List<IslandWorldData.Artist>(ctx.Artists);
            var walk = 1; var lamps = new List<Vector3>();
            var dirt = WorldMaterials.Solid(new Color(.52f, .45f, .33f));
            while (todo.Count > 0)
            {
                IslandWorldData.Artist next = null; Vector3 join = default; var best = float.MaxValue;
                foreach (var a in todo)
                    foreach (var (p, _) in walkPoints)
                    {
                        var d = Vector2.Distance(new Vector2(p.x, p.z), new Vector2(a.x, a.z)) - Foot(a);
                        if (d < best) { best = d; next = a; join = p; }
                    }
                if (next == null) break;
                todo.Remove(next);
                var from = new Vector3(join.x, 0, join.z); var to = new Vector3(next.x, 0, next.z);
                var dir = to - from; var total = dir.magnitude; if (total < 2f) continue;
                dir /= total;
                facing[next.name] = -dir;
                var side = Vector3.Cross(Vector3.up, dir);
                var end = total - Foot(next) * .8f;
                var pts = new List<Vector3>(); var wet = new List<bool>();
                for (var d = walk == 1 ? 8f : RoadW / 2f + .1f; ; d += 3f)
                {
                    var dd = Mathf.Min(d, end);
                    var q = from + dir * dd + side * Mathf.Sin(dd / Mathf.Max(end, 1f) * Mathf.PI) * Mathf.Min(7f, end * .05f);
                    if (!ctx.IsLand(q.x, q.z, 0f)) break;
                    var g = ctx.Ground(q.x, q.z);
                    var over = WaterAt(q.x, q.z, 2.5f, out var s);
                    pts.Add(new Vector3(q.x, over ? Mathf.Max(g, s + .7f) : g, q.z)); wet.Add(over);
                    if (dd >= end) break;
                }
                if (pts.Count < 2) continue;
                // the dirt road on land, planks on the bridges
                var run = new List<Vector3>(); var runWet = wet[0];
                for (var i = 0; i <= pts.Count; i++)
                {
                    if (i < pts.Count && (run.Count == 0 || wet[i] == runWet)) { run.Add(pts[i]); runWet = wet[i]; continue; }
                    if (run.Count > 1) { if (runWet) Bridge(ctx, mk, solid, run); else Strip(mk, run, dirt, RoadW, .05f); }
                    if (i < pts.Count) { var last = run[run.Count - 1]; run = new List<Vector3> { last, pts[i] }; runWet = wet[i]; }
                }
                foreach (var p in pts) Remember(p, walk);
                for (var i = 5; i < pts.Count; i += 8)
                {
                    if (wet[i]) continue;
                    var lp = pts[i] + side * ((i / 8) % 2 == 0 ? 1f : -1f) * (RoadW / 2f + .8f);
                    var near = false; foreach (var o in lamps) if ((o - lp).sqrMagnitude < 24f * 24f) near = true;
                    if (near || WaterAt(lp.x, lp.z, 1f, out _)) continue;
                    lamps.Add(lp);
                    Lamp(ctx, mk, ctx.Root, ctx.OnGround(lp.x, lp.z));
                }
                walk++;
            }
        }

        /// A plank bridge on posts, rails both sides, ramps down to the ground at each end.
        void Bridge(LandContext ctx, MeshKit mk, MeshKit solid, List<Vector3> run)
        {
            var up = Vector3.up; var topY = 0f; foreach (var p in run) topY = Mathf.Max(topY, p.y);
            var pts = new List<Vector3>();
            for (var i = 0; i < run.Count; i++)
            {
                var p = run[i]; var g = ctx.Ground(p.x, p.z);
                var y = (i == 0 || i == run.Count - 1) ? Mathf.Max(g, topY - 1.2f) : topY;
                pts.Add(new Vector3(p.x, y, p.z));
            }
            // ease onto the bank either end
            var a0 = pts[0] - (pts[1] - pts[0]).normalized * 3f; a0.y = ctx.Ground(a0.x, a0.z); pts.Insert(0, a0);
            var n = pts.Count; var b0 = pts[n - 1] + (pts[n - 1] - pts[n - 2]).normalized * 3f; b0.y = ctx.Ground(b0.x, b0.z); pts.Add(b0);
            Strip(mk, pts, plank, RoadW, .1f);
            for (var i = 0; i + 1 < pts.Count; i++)
            {
                var a = pts[i]; var b = pts[i + 1]; var d = b - a; var flat = new Vector3(d.x, 0, d.z).normalized; var side = Vector3.Cross(up, flat);
                solid.Box(plank, (a + b) / 2f + up * (.1f - .2f), new Vector3(RoadW, .4f, d.magnitude + .1f), Quaternion.LookRotation(d));
                if (i == 0 || i == pts.Count - 2) continue;
                foreach (var s in new[] { -1f, 1f })
                {
                    var ra = a + side * s * (RoadW / 2f - .1f) + up * 1.05f; var rb = b + side * s * (RoadW / 2f - .1f) + up * 1.05f;
                    mk.Box(greyWood, (ra + rb) / 2f, new Vector3(.1f, .1f, Vector3.Distance(ra, rb) + .05f), Quaternion.LookRotation(rb - ra));
                    solid.Box(greyWood, (ra + rb) / 2f - up * .5f, new Vector3(.15f, 1.1f, Vector3.Distance(ra, rb) + .05f), Quaternion.LookRotation(rb - ra));
                    var post = a + side * s * (RoadW / 2f - .1f); var g = ctx.Ground(post.x, post.z);
                    mk.Prism(greyWood, new Vector3(post.x, g - .5f, post.z), up, .12f, post.y - g + 1.6f, 6);
                }
            }
            life.bridges.Add(pts[pts.Count / 2]);
        }
    }
}
