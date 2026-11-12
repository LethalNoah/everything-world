using System.Collections.Generic;
using UnityEngine;

namespace EverythingWorld.World
{
    /// The Bluegrass Hollows (WORLD_GUIDE.md): an Appalachian holler, steep green ridges either side of a creek bottom, on a bright, clear summer afternoon, blue haze on
    /// the far ridges and a pale moon hanging in the daytime sky. Where you arrive: a red barn with its doors open
    /// on a porch stage, and in front of it a picking circle of rocking chairs where a banjo, a fiddle, a mandolin,
    /// a guitar and an upright bass float and play themselves. A creek winds through the hollow under a red
    /// covered bridge, past a mill with a turning water wheel; split-rail fences, hay bales, porch cabins with
    /// rockers, beehives, a mule and wagon on the trails. Files: BluegrassKit (ground, the creek, the barn and
    /// the picking circle, bridges, the mill, trails, the hollows), BluegrassTown (the ridges, the mountain town on
    /// its main street, the trestle, the coal mine, the stills), BluegrassInstruments, BluegrassArtists, BluegrassBilly
    /// (Billy Strings' stage and the classics' additions), BluegrassLife.
    public partial class BluegrassKit : LandKit
    {
        public override string Land => "The Bluegrass Hollows";
        public override Vector3 ArtistFront(IslandWorldData data, IslandWorldData.Artist a) => facing.TryGetValue(a.name, out var f) ? f : base.ArtistFront(data, a);

        public const string FoggyMountain = "29qFlNOssruDfoEN8vN2Uu", BlueMoon = "6XHbYnwTB7YoSV4TLeycuB", CandyMountain = "3T9cpQ45gpWghQrm74jFeX",
            WagonWheel = "5aAHai9OzUlkYPk2TJ4LLp", ConstantSorrow = "283WX8NobrMqpOScYvg0hy", SuperMoon = "7a87b0XfUmeTYBrrjX50AH";
        public override (string song, string label, string artist)[] SpecialSongs => new[]
        {
            (FoggyMountain, "Foggy Mountain Breakdown", "@the picking circle"), (BlueMoon, "Blue Moon Of Kentucky", "Bill Monroe & His Blue Grass Boys"),
            (CandyMountain, "Big Rock Candy Mountain", "Harry McClintock"), (WagonWheel, "Wagon Wheel", "Old Crow Medicine Show"),
            (ConstantSorrow, "I'm a Man of Constant Sorrow", "The Stanley Brothers"), (SuperMoon, "Super Moon", "Molly Tuttle"),
            (DustInABaggie, "Dust in a Baggie", "Billy Strings"), (BlackMountainRag, "Black Mountain Rag", "Doc Watson"), (ChurchStreet, "Church Street Blues", "Tony Rice"),
        };

        static readonly Color Warm = new Color(1f, .78f, .4f), Ink = new Color(.97f, .94f, .86f), Cream = new Color(.95f, .91f, .82f);
        const float TrailW = 2.4f, CreekW = 7f;

        Material wood, plank, barnRed, trim, roofM, stone, dark, metal, water, bark, cream, hay;
        Material[] leaves;
        GlowHalos halos; Vector3 center; BluegrassLife life; MeshKit crowns;
        readonly Dictionary<(int, int), List<Vector3>> pathCells = new Dictionary<(int, int), List<Vector3>>();
        readonly List<(Vector3 p, int w)> walkPoints = new List<(Vector3, int)>();
        readonly Dictionary<string, Vector3> facing = new Dictionary<string, Vector3>();
        readonly List<Vector3> noGrass = new List<Vector3>();
        readonly List<Vector3> creek = new List<Vector3>();   // the creek's bed, upstream to downstream
        readonly List<(Vector3 c, float r)> pools = new List<(Vector3, float)>();
        Vector2 heart2, perp2;

        void MakeMaterials()
        {
            wood = WorldMaterials.Solid(new Color(.6f, .45f, .3f)); plank = WorldMaterials.Solid(new Color(.55f, .44f, .33f)); barnRed = WorldMaterials.Solid(new Color(.62f, .16f, .12f));
            trim = WorldMaterials.Solid(new Color(.95f, .93f, .88f)); roofM = WorldMaterials.Solid(new Color(.32f, .33f, .36f)); stone = WorldMaterials.Solid(new Color(.58f, .57f, .54f));
            dark = WorldMaterials.Solid(new Color(.08f, .08f, .09f)); metal = WorldMaterials.Solid(new Color(.6f, .6f, .62f), .7f); bark = WorldMaterials.Solid(new Color(.36f, .29f, .24f));
            cream = WorldMaterials.Solid(Cream); hay = WorldMaterials.Solid(new Color(.86f, .74f, .42f));
            water = new Material(WorldMaterials.Glow(new Color(.28f, .5f, .58f), .08f)) { name = "Creek water" }; water.SetFloat("_Smoothness", .85f);
            leaves = new[] { WorldMaterials.Solid(new Color(.3f, .5f, .22f)), WorldMaterials.Solid(new Color(.38f, .56f, .24f)), WorldMaterials.Solid(new Color(.24f, .44f, .26f)), WorldMaterials.Solid(new Color(.45f, .6f, .26f)) };
        }

        Material GlowM(Color c, float s = 2f) => WorldMaterials.Glow(c, s * .65f);
        static readonly System.Random Smooth = new System.Random(0);   // for collider blobs: no lumps, and the land's own random stays as it was

        /// A plain box collider on its own object (a hanging jar, a slanted leg: things the merged walls don't cover).
        static void BoxCol(Transform parent, Vector3 center, Vector3 size, Quaternion rot)
        {
            var go = new GameObject("Box collider"); go.transform.SetParent(parent, false); go.transform.SetPositionAndRotation(center, rot);
            go.AddComponent<BoxCollider>().size = size; go.isStatic = true;
        }

        static float Foot(IslandWorldData.Artist a) => a.name == "Billy Strings" ? 20f : a.name == "Harry McClintock" || a.name == "Flatt & Scruggs" || a.name == "Trampled by Turtles" ? 13f : 9f;

        public override bool Ground(float height, float slope, float patch, out Color color)
        {
            if (slope > 9f) color = Color.Lerp(new Color(.46f, .48f, .46f), new Color(.55f, .56f, .52f), patch);
            else color = Color.Lerp(new Color(.38f, .56f, .24f), new Color(.56f, .66f, .3f), Mathf.SmoothStep(.15f, .9f, patch));
            color.a = 1f;
            return true;
        }

        // ---------------------------------------------------------------- shaping: the creek
        static float SegDist(Vector2 p, Vector2 a, Vector2 b, out float t)
        {
            var ab = b - a; t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / Mathf.Max(ab.sqrMagnitude, .0001f));
            return Vector2.Distance(p, a + ab * t);
        }

        /// Distance from (x, z) to the creek's middle line (or a pool's edge), and the water bed there.
        float CreekDist(float x, float z, out float bed)
        {
            bed = 0f; var best = float.MaxValue; var p = new Vector2(x, z);
            for (var k = 0; k + 1 < creek.Count; k++)
            {
                var a = creek[k]; var b = creek[k + 1];
                if (Mathf.Abs(a.x - x) > 60f || Mathf.Abs(a.z - z) > 60f) continue;
                var d = SegDist(p, new Vector2(a.x, a.z), new Vector2(b.x, b.z), out var t) - CreekW / 2f;
                if (d < best) { best = d; bed = Mathf.Lerp(a.y, b.y, t); }
            }
            foreach (var (c, r) in pools) { var d = Vector2.Distance(p, new Vector2(c.x, c.z)) - r; if (d < best) { best = d; bed = c.y; } }
            return best;
        }

        bool InCreek(float x, float z, float pad) => creek.Count > 1 && CreekDist(x, z, out _) < pad;

        public override void ShapeGround(IslandWorldData data, int index)
        {
            var isl = data.layout.islands[index]; heart2 = new Vector2(isl.labelX, isl.labelZ);
            creek.Clear(); pools.Clear();
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
            float Run(Vector2 from, Vector2 d) { var t = 0f; while (t < 600f && Inland(from + d * (t + 10f), 40f)) t += 10f; return t; }
            // the creek runs through the hollow 34 m to one side of where you arrive, along the land's longest line
            var bestScore = 0f; var dir = Vector2.right; perp2 = Vector2.up; float run1 = 0, run2 = 0;
            for (var k = 0; k < 12; k++)
            {
                var a = k * Mathf.PI / 12f; var d = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                foreach (var s in new[] { 1f, -1f })
                {
                    var pp = new Vector2(-d.y, d.x) * s; var o = heart2 + pp * 34f; if (!Inland(o, 30f)) continue;
                    float r1 = Run(o, d), r2 = Run(o, -d); var score = Mathf.Min(r1, r2) * 2f + r1 + r2;
                    if (score > bestScore) { bestScore = score; dir = d; perp2 = pp; run1 = r1; run2 = r2; }
                }
            }
            if (bestScore > 0f)
            {
                var o = heart2 + perp2 * 34f;
                for (var t = -run2; t <= run1; t += 4f)
                {
                    var q = o + dir * t + perp2 * Mathf.Sin(t / 110f * Mathf.PI) * 14f * Mathf.Clamp01(Mathf.Abs(t) / 40f);
                    creek.Add(new Vector3(q.x, 0, q.y));
                }
                // upstream is the higher end
                if (creek.Count > 2 && data.HeightAt(creek[0].x, creek[0].z) < data.HeightAt(creek[creek.Count - 1].x, creek[creek.Count - 1].z)) creek.Reverse();
                // the bed follows the hollow 2.2 m under the lower bank (a creek this small can't be seen to run uphill,
                // and forcing it always downhill dug a ravine wherever the ground rose)
                for (var i = 0; i < creek.Count; i++)
                {
                    var p = creek[i]; var nx = creek[Mathf.Min(i + 1, creek.Count - 1)] - creek[Mathf.Max(i - 1, 0)]; var side = Vector3.Cross(Vector3.up, nx.normalized) * (CreekW / 2f + 3f);
                    var g = Mathf.Min(data.HeightAt(p.x, p.z), Mathf.Min(data.HeightAt(p.x + side.x, p.z + side.z), data.HeightAt(p.x - side.x, p.z - side.z)));
                    p.y = Mathf.Max(g - 2.2f, data.layout.waterLevel + .3f); creek[i] = p;
                }
                for (var pass = 0; pass < 8; pass++) for (var i = 1; i + 1 < creek.Count; i++) { var p = creek[i]; p.y = Mathf.Min(p.y, (creek[i - 1].y + p.y * 2f + creek[i + 1].y) / 4f); creek[i] = p; }
                pools.Add((creek[0], 9f)); pools.Add((creek[creek.Count - 1], 14f));
                Carve(data, index);
                creekDir2 = dir; Ridges(data, index); StreetLine(data, index, Id);
            }
            var avoid = new List<Vector3>();
            for (var i = 0; i < creek.Count; i += 4) avoid.Add(new Vector3(creek[i].x, CreekW / 2f + 9f, creek[i].z));
            foreach (var (c, r) in pools) avoid.Add(new Vector3(c.x, r + 10f, c.z));
            for (var i = 0; i < street.Count; i += 2) avoid.Add(new Vector3(street[i].x, 22f, street[i].z));
            foreach (var a in data.layout.artists) if (a.sceneId == isl.sceneId) a.r = Mathf.Max(a.r, Foot(a) + 6f);
            ArtistPlacement.Spread(data, index, 54f, heart2, 44f, 46f, avoid, null, Land, 320f);
            foreach (var a in data.layout.artists)
            {
                if (a.sceneId != isl.sceneId) continue;
                a.r = Mathf.Max(a.r, Foot(a) + 6f);
                Level(data, index, new Vector2(a.x, a.z), Foot(a) + 1f, Foot(a) + 12f);
            }
            Level(data, index, heart2 - perp2 * 10f, 24f, 40f);
        }

        void Carve(IslandWorldData data, int index)
        {
            float minX = float.MaxValue, maxX = float.MinValue, minZ = float.MaxValue, maxZ = float.MinValue;
            foreach (var p in creek) { minX = Mathf.Min(minX, p.x); maxX = Mathf.Max(maxX, p.x); minZ = Mathf.Min(minZ, p.z); maxZ = Mathf.Max(maxZ, p.z); }
            const float bank = 22f;
            for (var r = 0; r < data.Nz; r++)
            {
                var z = data.layout.depth - r * data.Step; if (z < minZ - 40f || z > maxZ + 40f) continue;
                for (var col = 0; col < data.Nx; col++)
                {
                    var x = col * data.Step; if (x < minX - 40f || x > maxX + 40f) continue;
                    var i = r * data.Nx + col; if (data.islandIds[i] != index) continue;
                    var d = CreekDist(x, z, out var bed); if (d > bank) continue;
                    if (d < 0f) data.heights[i] = Mathf.Min(data.heights[i], bed - Mathf.Clamp01(-d / 3f) * .4f);
                    else data.heights[i] = Mathf.Min(data.heights[i], Mathf.Lerp(bed + 1.6f, data.heights[i], Mathf.SmoothStep(0f, 1f, d / bank)));
                }
            }
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
                    var i = r * data.Nx + col; if (data.islandIds[i] != index || InCreek(x, z, 10f)) continue;
                    var d = Vector2.Distance(new Vector2(x, z), c);
                    if (d < flat) data.heights[i] = level;
                    else if (d < blend) data.heights[i] = Mathf.Lerp(level, data.heights[i], Mathf.SmoothStep(0, 1, (d - flat) / (blend - flat)));
                }
            }
        }

        // ---------------------------------------------------------------- dressing
        public override void Dress(LandContext ctx)
        {
            MakeMaterials();
            halos = new GlowHalos(); crowns = new MeshKit();
            pathCells.Clear(); walkPoints.Clear(); facing.Clear(); noGrass.Clear(); millAt = Vector3.zero; TownMaterials();
            center = ctx.Plaza;
            life = new GameObject("Bluegrass life").AddComponent<BluegrassLife>();
            life.transform.SetParent(ctx.Root, false);
            life.center = center;
            var mk = new MeshKit(); var solid = new MeshKit();
            Barn(ctx, mk, solid);
            if (creek.Count > 1) { Creek(ctx, mk, solid); Mill(ctx, mk, solid); }
            Street(ctx, mk, solid);
            Trails(ctx, mk, solid);
            Town(ctx, mk, solid);
            if (creek.Count > 1) Trestle(ctx, mk, solid);
            Mine(ctx, mk, solid);
            Hollows(ctx, mk, solid);
            mk.Build("Bluegrass Hollows", ctx.Root, false);
            crowns.Build("Soft bluegrass crowns and leaves", ctx.Root, false);   // "Soft…": walked through, so the land audit skips it
            solid.BuildCollider("Bluegrass Hollows walls", ctx.Root);
        }

        public override void Finish(LandContext ctx) { GroundCover(ctx); halos.Build("Bluegrass glow", ctx.Root); }

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
            ctx.IsLand(x, z, 4f) && !InFootprint(ctx, x, z, pad) && !NearPath(x, z, pad + 2f) && !InNoGrass(x, z, pad) && !InCreek(x, z, pad + 3f)
            && Vector2.Distance(new Vector2(x, z), new Vector2(center.x, center.z)) > 34f + pad;

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

        /// A post with a mason jar lantern hanging from an arm.
        void JarLamp(LandContext ctx, MeshKit mk, Vector3 g)
        {
            var up = Vector3.up;
            mk.Prism(wood, g - up * .3f, up, .09f, 2.6f, 6);
            mk.Box(wood, g + up * 2.25f + Vector3.right * .3f, new Vector3(.7f, .08f, .08f), Quaternion.identity);
            var j = g + up * 1.9f + Vector3.right * .55f;
            mk.Prism(GlowM(Warm, 2.4f), j - up * .16f, up, .13f, .3f, 8); mk.Prism(metal, j + up * .14f, up, .14f, .05f, 8);
            halos.Add(j, 1.1f, Warm, .8f);
            ArtistBits.PostCollider(ctx.Root, g, 2.4f, .1f); BoxCol(ctx.Root, j, new Vector3(.32f, .5f, .32f), Quaternion.identity);   // the jar hangs at head height
        }

        /// A broadleaf tree: a trunk with a few limbs and a crown of lumps in one of the summer greens.
        void Hardwood(LandContext ctx, MeshKit mk, Vector3 at, float s)
        {
            var up = Vector3.up; var h = 5.5f * s; var leaf = leaves[ctx.Rng.Next(leaves.Length)];
            mk.Prism(bark, at - up * .3f, up, .45f * s, h, 7, .65f);
            for (var k = 0; k < 6; k++) { var d = Quaternion.Euler(0, k * 60f + ctx.Range(-20f, 20f), 0) * Vector3.forward; crowns.Blob(leaf, at + up * (h + .8f * s + ctx.Range(-.6f, .8f)) + d * 2.3f * s, new Vector3(2.4f, 2f, 2.4f) * s, ctx.Rng, .3f); }
            crowns.Blob(leaf, at + up * (h + 2.6f * s), new Vector3(3f, 2.2f, 3f) * s, ctx.Rng, .3f);
            ArtistBits.PostCollider(ctx.Root, at, h, .45f * s);
        }

        void Rhododendron(LandContext ctx, Vector3 at)
        {
            var bush = WorldMaterials.Solid(new Color(.22f, .4f, .24f)); var bloom = WorldMaterials.Solid(new Color(.95f, .6f, .8f));
            for (var k = 0; k < 3; k++) crowns.Blob(bush, at + new Vector3(ctx.Range(-1f, 1f), .7f, ctx.Range(-1f, 1f)), new Vector3(1.3f, .9f, 1.3f), ctx.Rng, .3f);
            for (var k = 0; k < 7; k++) crowns.Blob(bloom, at + new Vector3(ctx.Range(-1.4f, 1.4f), ctx.Range(1f, 1.7f), ctx.Range(-1.4f, 1.4f)), Vector3.one * .25f, ctx.Rng, .2f);
        }

        /// A zig-zag split-rail fence from a to b.
        void Fence(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 a, Vector3 b)
        {
            var dir = b - a; dir.y = 0; var len = dir.magnitude; if (len < 3f) return; dir /= len; var side = Vector3.Cross(Vector3.up, dir);
            var n = Mathf.Max(1, Mathf.RoundToInt(len / 3.2f)); Vector3 prev = default;
            for (var i = 0; i <= n; i++)
            {
                var p = a + dir * (len * i / n) + side * ((i % 2 == 0) ? .5f : -.5f); p = ctx.OnGround(p.x, p.z);
                if (i > 0) foreach (var hgt in new[] { .45f, .85f, 1.25f }) { var m = (prev + p) / 2f + Vector3.up * hgt; mk.Box(wood, m, new Vector3(.12f, .12f, Vector3.Distance(prev, p) + .5f), Quaternion.LookRotation(p - prev)); }
                if (i > 0) solid.Box(wood, (prev + p) / 2f + Vector3.up * .7f, new Vector3(.3f, 1.4f, Vector3.Distance(prev, p)), Quaternion.LookRotation(p - prev));
                prev = p;
            }
        }

        // ---------------------------------------------------------------- the barn, the porch stage and the picking circle
        /// A big red barn with a gambrel roof and white trim, its great doors swung open on a porch stage with string
        /// lights; in front, a picking circle of rocking chairs round a lantern, with the floating instruments.
        void Barn(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var toCreek = new Vector3(perp2.x, 0, perp2.y); if (toCreek.sqrMagnitude < .1f) toCreek = Vector3.forward;
            var c = ctx.OnGround(center.x - toCreek.x * 22f, center.z - toCreek.z * 22f); var face = toCreek; var rot = Quaternion.LookRotation(face); var side = Vector3.Cross(up, face);
            const float W = 13f, D = 18f, H = 5.5f;
            Vector3 P(float x, float y, float z) => c + side * x + up * y + face * z;
            // walls: back, sides, front with a great doorway
            mk.Box(barnRed, P(0, H / 2f, -D / 2f), new Vector3(W, H, .3f), rot);
            foreach (var s in new[] { -1f, 1f }) mk.Box(barnRed, P(s * (W / 2f - .15f), H / 2f, 0), new Vector3(.3f, H, D - .6f), rot);
            foreach (var s in new[] { -1f, 1f }) mk.Box(barnRed, P(s * (W / 4f + 1.6f), H / 2f, D / 2f), new Vector3(W / 2f - 3.2f, H, .3f), rot);
            mk.Box(barnRed, P(0, H - .5f, D / 2f), new Vector3(6.4f, 1f, .3f), rot);
            solid.Box(barnRed, P(0, H / 2f, -D / 2f), new Vector3(W, H, .4f), rot);
            foreach (var s in new[] { -1f, 1f }) { solid.Box(barnRed, P(s * (W / 2f - .15f), H / 2f, 0), new Vector3(.4f, H, D), rot); solid.Box(barnRed, P(s * (W / 4f + 1.6f), H / 2f, D / 2f), new Vector3(W / 2f - 3.2f, H, .4f), rot); }
            // gambrel roof and gable ends
            float e = W / 2f + .4f, k = 3.6f, kh = H + 3.4f, top = H + 5f;
            foreach (var zz in new[] { -1f, 1f })
            {
                var z0 = zz * (D / 2f); var inside = P(0, H + 1f, 0);
                var g0 = P(-W / 2f, H, z0); var g1 = P(-k, kh, z0); var g2 = P(0, top, z0); var g3 = P(k, kh, z0); var g4 = P(W / 2f, H, z0);
                mk.Quad(barnRed, g0, g1, g3, g4, inside); mk.Tri(barnRed, g1, g2, g3, inside);
                var outside = P(0, H + 1f, z0 * 4f); mk.Quad(barnRed, g0, g1, g3, g4, outside); mk.Tri(barnRed, g1, g2, g3, outside);   // seen from inside too
                if (zz > 0) { mk.Box(trim, P(0, H + 2.2f, D / 2f + .17f), new Vector3(1.6f, 1.6f, .05f), rot); mk.Box(dark, P(0, H + 2.2f, D / 2f + .19f), new Vector3(1.3f, 1.3f, .02f), rot); }
            }
            foreach (var s in new[] { -1f, 1f })
            {
                var inside = P(0, H, 0);
                mk.Quad(roofM, P(s * e, H - .2f, -D / 2f - .4f), P(s * e, H - .2f, D / 2f + .4f), P(s * k, kh, D / 2f + .4f), P(s * k, kh, -D / 2f - .4f), inside);
                mk.Quad(roofM, P(s * k, kh, -D / 2f - .4f), P(s * k, kh, D / 2f + .4f), P(0, top + .05f, D / 2f + .4f), P(0, top + .05f, -D / 2f - .4f), inside);
                // the underside, so the roof is closed when you look up from inside the barn
                var sky = P(0, top + 30f, 0); var under = WorldMaterials.Solid(new Color(.42f, .3f, .22f));
                mk.Quad(under, P(s * (W / 2f - .3f), H - .15f, -D / 2f + .3f), P(s * (W / 2f - .3f), H - .15f, D / 2f - .3f), P(s * k, kh - .15f, D / 2f - .3f), P(s * k, kh - .15f, -D / 2f + .3f), sky);
                mk.Quad(under, P(s * k, kh - .15f, -D / 2f + .3f), P(s * k, kh - .15f, D / 2f - .3f), P(0, top - .1f, D / 2f - .3f), P(0, top - .1f, -D / 2f + .3f), sky);
            }
            // white trim on the corners and round the doorway; the two great doors swung open with white X braces
            foreach (var s in new[] { -1f, 1f }) { mk.Box(trim, P(s * W / 2f, H / 2f, D / 2f + .05f), new Vector3(.3f, H, .3f), rot); mk.Box(trim, P(s * 3.2f, (H - 1f) / 2f, D / 2f + .17f), new Vector3(.25f, H - 1f, .05f), rot); }
            mk.Box(trim, P(0, H - 1f, D / 2f + .17f), new Vector3(6.6f, .25f, .05f), rot);
            foreach (var s in new[] { -1f, 1f })
            {
                var hinge = P(s * 3.2f, 0, D / 2f + .2f); var drot = rot * Quaternion.Euler(0, s * 100f, 0); var dc = hinge + drot * new Vector3(s * 1.6f, (H - 1f) / 2f + .05f, 0);
                mk.Box(barnRed, dc, new Vector3(3.2f, H - 1f, .15f), drot); solid.Box(barnRed, dc, new Vector3(3.2f, H - 1f, .25f), drot);
                foreach (var a in new[] { -50f, 50f }) mk.Box(trim, dc + drot * new Vector3(0, 0, .09f * (1f)), new Vector3(.18f, Mathf.Sqrt(3.2f * 3.2f + (H - 1f) * (H - 1f)) - .3f, .03f), drot * Quaternion.Euler(0, 0, a > 0 ? 38f : -38f));
                mk.Box(trim, dc + drot * new Vector3(0, 0, .09f), new Vector3(3.2f, .18f, .03f), drot);
            }
            // inside: plank floor, hay bales, a warm light, a hayloft
            mk.Box(plank, P(0, .08f, 0), new Vector3(W - .6f, .16f, D - .6f), rot);
            for (var i = 0; i < 6; i++) { var hp = P(-W / 2f + 1.3f + (i % 2) * 1.2f, .55f + (i / 2 % 2) * .8f, -D / 2f + 1.5f + i * .9f); mk.Box(hay, hp, new Vector3(1.1f, .8f, 1.6f), rot); }
            solid.Box(hay, P(-W / 2f + 1.9f, .9f, -D / 2f + 3.8f), new Vector3(2.4f, 1.8f, 5.6f), rot);
            mk.Box(plank, P(0, 3.4f, -D / 2f + 3f), new Vector3(W - .6f, .15f, 6f), rot);
            IndoorSpaces.Add(P(0, H / 2f, 0), rot, new Vector3(W - .6f, H, D - .6f));
            ArtistBits.PointLight(ctx.Root, P(0, 4f, 0), Warm, 16f, 1.4f, true);
            // the porch stage: a plank deck before the doors with a little roof on posts, string lights, a mic, chairs
            var st = P(0, 0, D / 2f + 3.4f);
            mk.Box(plank, st + up * .35f, new Vector3(W, .7f, 6.4f), rot); solid.Box(plank, st + up * .35f, new Vector3(W, .7f, 6.4f), rot);
            mk.Box(plank, P(0, .18f, D / 2f + 6.95f), new Vector3(4f, .35f, .7f), rot); solid.Box(plank, P(0, .18f, D / 2f + 6.95f), new Vector3(4f, .35f, .7f), rot);
            foreach (var s in new[] { -1f, 1f }) foreach (var zz in new[] { .4f, 6.2f }) { mk.Box(wood, P(s * (W / 2f - .3f), 2.4f, D / 2f + zz), new Vector3(.25f, 3.4f, .25f), rot); ArtistBits.PostCollider(ctx.Root, P(s * (W / 2f - .3f), 0, D / 2f + zz), 4f, .15f); }
            mk.Box(roofM, P(0, 4.25f, D / 2f + 3.3f), new Vector3(W + .6f, .15f, 7f), rot * Quaternion.Euler(-8f, 0, 0));
            for (var i = 0; i < 18; i++) { var t = i / 17f; var bp = P(Mathf.Lerp(-W / 2f + .3f, W / 2f - .3f, t), 3.9f - Mathf.Sin(t * Mathf.PI) * .5f, D / 2f + 6.2f); mk.Blob(GlowM(i % 3 == 0 ? new Color(1f, .55f, .3f) : Warm, 3f), bp, Vector3.one * .1f, ctx.Rng, 0f); if (i % 3 == 0) halos.Add(bp, .7f, Warm, .7f); }
            var mic = P(0, .7f, D / 2f + 5f); mk.Prism(dark, mic, up, .03f, 1.5f, 6); mk.Blob(dark, mic + up * 1.55f, Vector3.one * .08f, ctx.Rng, 0f);
            foreach (var s in new[] { -2.5f, 2.5f }) Rocker(mk, solid, ctx, P(s, .7f, D / 2f + 3.6f), face);
            ArtistBits.PointLight(ctx.Root, P(0, 3.6f, D / 2f + 4f), Warm, 18f, 1.6f);
            ArtistBits.Label(ctx.Root, "THE BLUEGRASS HOLLOWS", P(0, H + .6f, D / 2f + .2f), face, .3f, Ink, TextAnchor.MiddleCenter);
            noGrass.Add(new Vector3(c.x, D / 2f + 2f, c.z)); noGrass.Add(new Vector3(st.x, 6f, st.z));
            // the picking circle between the stage and the creek
            var pc = ctx.OnGround(center.x + toCreek.x * 2f, center.z + toCreek.z * 2f);
            mk.Prism(WorldMaterials.Solid(new Color(.55f, .45f, .34f)), pc - up * .25f, up, 5.6f, .3f, 18);
            for (var i = 0; i < 7; i++) { var d = Quaternion.Euler(0, i * 360f / 7f, 0) * Vector3.forward; Rocker(mk, solid, ctx, ctx.OnGround(pc.x + d.x * 4.2f, pc.z + d.z * 4.2f), -d); }
            mk.Prism(wood, pc, up, .12f, 1.2f, 6); mk.Prism(GlowM(Warm, 3f), pc + up * 1.2f, up, .22f, .4f, 8); mk.Prism(metal, pc + up * 1.6f, up, .25f, .06f, 8);
            halos.Add(pc + up * 1.4f, 1.6f, Warm, .9f); ArtistBits.PostCollider(ctx.Root, pc, 1.6f, .3f);
            ArtistBits.PointLight(ctx.Root, pc + up * 2.5f, Warm, 14f, 1.4f);
            life.circle = pc;
            life.instruments.Add(Floating(mk2 => Banjo(mk2, Vector3.zero, Quaternion.identity, 1f, false), "Floating banjo"));
            life.instruments.Add(Floating(mk2 => Fiddle(mk2, Vector3.zero, Quaternion.identity, 1f), "Floating fiddle"));
            life.instruments.Add(Floating(mk2 => Mandolin(mk2, Vector3.zero, Quaternion.identity, 1f), "Floating mandolin"));
            life.instruments.Add(Floating(mk2 => Guitar(mk2, Vector3.zero, Quaternion.identity, 1f), "Floating guitar"));
            life.instruments.Add(Floating(mk2 => UprightBass(mk2, Vector3.zero, Quaternion.identity, 1f), "Floating bass"));
            noGrass.Add(new Vector3(pc.x, 6.5f, pc.z));
            var mark = new GameObject("Shot: the picking circle").transform; mark.SetParent(ctx.Root, false);
            var cam = pc + toCreek * 13f + side * 4f; mark.SetPositionAndRotation(ctx.OnGround(cam.x, cam.z), Quaternion.LookRotation((pc - cam).normalized * 1f + up * .05f));
            Remember(ctx.OnGround(center.x, center.z), 0);
        }

        Transform Floating(System.Action<MeshKit> build, string name)
        {
            var mk = new MeshKit(); build(mk);
            var go = mk.BuildSingle(name, life.transform, false); return go.transform;
        }

        /// A rocking chair facing dir.
        void Rocker(MeshKit mk, MeshKit solid, LandContext ctx, Vector3 at, Vector3 dir)
        {
            var up = Vector3.up; var rot = Quaternion.LookRotation(dir); var side = Vector3.Cross(up, dir);
            mk.Box(wood, at + up * .48f, new Vector3(.6f, .06f, .55f), rot); mk.Box(wood, at + up * .95f - dir * .3f, new Vector3(.6f, .9f, .06f), rot * Quaternion.Euler(-12f, 0, 0));
            foreach (var s in new[] { -.28f, .28f })
            {
                mk.Box(wood, at + side * s + up * .1f, new Vector3(.05f, .06f, .9f), rot); mk.Box(wood, at + side * s + up * .3f, new Vector3(.05f, .4f, .05f), rot);
                mk.Box(wood, at + side * s + up * .68f + dir * .05f, new Vector3(.05f, .05f, .5f), rot);
            }
            solid.Box(wood, at + up * .5f, new Vector3(.7f, 1f, .8f), rot);
        }

        // ---------------------------------------------------------------- the creek, bridges, the mill
        void Creek(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var surf = new List<Vector3>(); foreach (var p in creek) surf.Add(p + Vector3.up * 1.05f);
            // the water lies level: each 4 m reach is flat at its lower end's height, dropping to the next over a little
            // fall, so where the creek climbs a hillside it steps down in pools instead of a tilted sheet of water
            {
                var n = surf.Count; var L = new Vector3[n]; var R = new Vector3[n]; var hw = (CreekW + 2.4f) / 2f;
                Vector3 Flat(Vector3 v) { v.y = 0; return v.normalized; }
                for (var i = 0; i < n; i++)
                {
                    var a = i > 0 ? Flat(surf[i] - surf[i - 1]) : Flat(surf[1] - surf[0]); var b = i < n - 1 ? Flat(surf[i + 1] - surf[i]) : Flat(surf[n - 1] - surf[n - 2]);
                    var sd = Vector3.Cross(Vector3.up, (a + b).normalized); var miter = 1f / Mathf.Max(.6f, Vector3.Dot(sd, Vector3.Cross(Vector3.up, b)));
                    L[i] = surf[i] - sd * hw * miter; R[i] = surf[i] + sd * hw * miter;
                }
                for (var i = 0; i + 1 < n; i++)
                {
                    var y = Mathf.Min(surf[i].y, surf[i + 1].y); Vector3 At(Vector3 v) { v.y = y; return v; }
                    mk.Quad(water, At(L[i]), At(R[i]), At(R[i + 1]), At(L[i + 1]), (surf[i] + surf[i + 1]) / 2f - Vector3.up * 50f);
                    if (i + 2 < n)
                    {
                        var y2 = Mathf.Min(surf[i + 1].y, surf[i + 2].y);
                        if (Mathf.Abs(y - y2) > .05f) { var hi = Mathf.Max(y, y2); var lo = Mathf.Min(y, y2); var up2 = y > y2 ? surf[i] : surf[i + 2];   // the fall faces downstream
                            mk.Quad(water, new Vector3(L[i + 1].x, hi, L[i + 1].z), new Vector3(R[i + 1].x, hi, R[i + 1].z), new Vector3(R[i + 1].x, lo, R[i + 1].z), new Vector3(L[i + 1].x, lo, L[i + 1].z), new Vector3(up2.x, (hi + lo) / 2f, up2.z)); }
                    }
                }
            }
            foreach (var (c, r) in pools)
            {
                const int n = 28; var wc = c + Vector3.up * 1.05f;
                for (var k = 0; k < n; k++) { float a0 = k * Mathf.PI * 2f / n, a1 = (k + 1) * Mathf.PI * 2f / n; mk.Tri(water, wc, wc + new Vector3(Mathf.Cos(a0), 0, Mathf.Sin(a0)) * (r + 2f), wc + new Vector3(Mathf.Cos(a1), 0, Mathf.Sin(a1)) * (r + 2f), wc - Vector3.up); }
            }
            // stones and ferns along the banks
            var fern = WorldMaterials.Solid(new Color(.3f, .5f, .25f));
            for (var i = 2; i < creek.Count - 2; i += 3)
            {
                var p = creek[i]; var d = (creek[i + 1] - creek[i - 1]); d.y = 0; d.Normalize(); var sd = Vector3.Cross(Vector3.up, d);
                foreach (var s in new[] { -1f, 1f })
                {
                    if (!ctx.Chance(.45f)) continue;
                    var q = p + sd * s * (CreekW / 2f + ctx.Range(-.5f, 1.5f)); if (!ctx.IsLand(q.x, q.z, 1f)) continue; q.y = ctx.Ground(q.x, q.z);
                    if (ctx.Chance(.5f)) { var sr = new Vector3(ctx.Range(.4f, 1f), .45f, ctx.Range(.4f, .9f)); mk.Blob(stone, q + Vector3.up * .2f, sr, ctx.Rng, .35f); solid.Blob(stone, q + Vector3.up * .2f, sr * 1.1f, Smooth, 0f); }
                    else for (var f = 0; f < 4; f++) crowns.Box(fern, q + new Vector3(ctx.Range(-.5f, .5f), .35f, ctx.Range(-.5f, .5f)), new Vector3(.08f, .05f, .9f), Quaternion.Euler(-35f, f * 90f + ctx.Range(-20f, 20f), 0));
                }
            }
            life.creek = surf.ToArray();
            var mark = new GameObject("Shot: the creek").transform; mark.SetParent(ctx.Root, false);
            var mid = creek[creek.Count / 2]; var md = (creek[creek.Count / 2 + 1] - creek[creek.Count / 2 - 1]); md.y = 0; md.Normalize(); var ms = Vector3.Cross(Vector3.up, md);
            var cam = mid + ms * (CreekW / 2f + 7f) - md * 14f; mark.SetPositionAndRotation(ctx.OnGround(cam.x, cam.z), Quaternion.LookRotation(md * 1f - ms * .5f));
            foreach (var (c, r) in pools) noGrass.Add(new Vector3(c.x, r + 1f, c.z));
        }

        /// A bridge from a to b over the creek: covered (red, with a shingle roof and white portals) or a plank
        /// footbridge with rails.
        void Bridge(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 a, Vector3 b, bool covered)
        {
            var up = Vector3.up; var dir = b - a; var len = dir.magnitude; dir /= len; var flat = new Vector3(dir.x, 0, dir.z).normalized; var side = Vector3.Cross(up, flat);
            var rot = Quaternion.LookRotation(dir); var mid = (a + b) / 2f; var w = covered ? 4.4f : 2.8f;
            mk.Box(plank, mid, new Vector3(w, .25f, len + .4f), rot); solid.Box(plank, mid - up * .1f, new Vector3(w, .45f, len + .4f), rot);
            foreach (var t in new[] { -.5f, .5f }) { var pp = mid + dir * len * t * .8f; var g = ctx.Ground(pp.x, pp.z); if (pp.y - g > .3f) foreach (var s in new[] { -1f, 1f }) mk.Prism(wood, new Vector3(pp.x, g - .4f, pp.z) + side * s * (w / 2f - .2f), up, .18f, pp.y - g + .3f, 6); }
            if (covered)
            {
                const float h = 3.6f;
                foreach (var s in new[] { -1f, 1f })
                {
                    mk.Box(barnRed, mid + side * s * w / 2f + up * .75f, new Vector3(.15f, 1.3f, len + .4f), rot); mk.Box(barnRed, mid + side * s * w / 2f + up * (h - .45f), new Vector3(.15f, .9f, len + .4f), rot);
                    for (var i = 0; i <= 4; i++) mk.Box(barnRed, mid + side * s * w / 2f + dir * ((i / 4f - .5f) * (len + .2f)) + up * 2.05f, new Vector3(.17f, .9f, .3f), rot);
                    solid.Box(barnRed, mid + side * s * w / 2f + up * 1.2f, new Vector3(.3f, 2.4f, len + .4f), rot);
                }
                foreach (var e in new[] { -1f, 1f })
                {
                    var end = mid + dir * e * (len / 2f + .2f);
                    mk.Box(trim, end + up * (h - .1f), new Vector3(w + .3f, .5f, .2f), rot);
                    foreach (var s in new[] { -1f, 1f }) mk.Box(trim, end + side * s * (w / 2f) + up * (h / 2f), new Vector3(.3f, h, .22f), rot);
                    mk.Tri(barnRed, end + up * h - side * (w / 2f + .3f), end + up * (h + 1.5f), end + up * h + side * (w / 2f + .3f), end - dir * e * 2f + up * h);
                    mk.Tri(barnRed, end + up * h - side * (w / 2f + .3f), end + up * (h + 1.5f), end + up * h + side * (w / 2f + .3f), end + dir * e * 2f + up * h);   // and its inner face, so the gable shows from inside the bridge too
                }
                // the roof slopes with the deck and walls (a level roof sank into the walls at the high end and hung off the low one)
                mk.GableRoof(roofM, mid + up * h, new Vector3(w + 1f, 0, len + 1.2f), 1.5f, rot);
                var lamp = mid + up * (h - .3f); mk.Prism(GlowM(Warm, 2.4f), lamp - up * .2f, up, .14f, .3f, 8); halos.Add(lamp - up * .05f, 1f, Warm, .7f);
                ArtistBits.PointLight(ctx.Root, lamp - up * .5f, Warm, 9f, 1f);
            }
            else
            {
                foreach (var s in new[] { -1f, 1f })
                {
                    mk.Box(wood, mid + side * s * (w / 2f - .05f) + up * 1f, new Vector3(.1f, .1f, len + .4f), rot);
                    for (var i = 0; i <= 3; i++) mk.Box(wood, mid + side * s * (w / 2f - .05f) + dir * ((i / 3f - .5f) * len) + up * .55f, new Vector3(.1f, 1f, .1f), rot);
                    solid.Box(wood, mid + side * s * (w / 2f) + up * .6f, new Vector3(.2f, 1.2f, len + .4f), rot);
                }
            }
        }

        /// A grist mill on the bank: a stone ground floor, a clapboard upper floor, a big wooden water wheel turning
        /// in the creek beside it.
        void Mill(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var best = -1;
            for (var i = 6; i < creek.Count - 6; i++)
            {
                var p = creek[i]; var dd = Vector2.Distance(new Vector2(p.x, p.z), new Vector2(center.x, center.z)); if (dd < 70f || dd > 170f) continue;
                var d = (creek[i + 1] - creek[i - 1]); d.y = 0; d.Normalize(); var sd = Vector3.Cross(up, d);
                foreach (var s in new[] { 1f, -1f }) { var h = p + sd * s * (CreekW / 2f + 5.5f); if (ctx.IsLand(h.x, h.z, 8f) && !InFootprint(ctx, h.x, h.z, 6f)) { best = i; millSide = s; break; } }
                if (best >= 0) break;
            }
            if (best < 0) return;
            var c0 = creek[best]; var dir = (creek[best + 1] - creek[best - 1]); dir.y = 0; dir.Normalize(); var side = Vector3.Cross(up, dir) * millSide;
            var hc = c0 + side * (CreekW / 2f + 5.5f); hc.y = ctx.Ground(hc.x, hc.z); millAt = hc; var rot = Quaternion.LookRotation(dir);
            mk.Box(stone, hc + up * 1.6f, new Vector3(7f, 3.2f, 8f), Quaternion.LookRotation(side)); solid.Box(stone, hc + up * 3f, new Vector3(7f, 6f, 8f), Quaternion.LookRotation(side));
            mk.Box(WorldMaterials.Solid(new Color(.78f, .7f, .55f)), hc + up * 4.4f, new Vector3(7f, 2.4f, 8f), Quaternion.LookRotation(side));
            mk.GableRoof(roofM, hc + up * 5.6f, new Vector3(7.8f, 0, 8.8f), 2.4f, rot);
            foreach (var s in new[] { -2f, 2f }) mk.Box(GlowM(Warm, 1.6f), hc + up * 4.4f - side * 3.52f + dir * s, new Vector3(.9f, .9f, .04f), Quaternion.LookRotation(side));
            mk.Box(dark, hc + up * 1.1f - side * 3.52f, new Vector3(1.2f, 2.2f, .04f), Quaternion.LookRotation(side));
            // the wheel, turned by BluegrassLife
            var wc = c0 + side * (CreekW / 2f - 1.2f) + up * 3f; wc.y = c0.y + 3f;
            var wk = new MeshKit(); var wheelWood = WorldMaterials.Solid(new Color(.45f, .34f, .24f));
            for (var k = 0; k < 16; k++)
            {
                float a0 = k * Mathf.PI / 8f, a1 = (k + 1) * Mathf.PI / 8f;
                foreach (var x in new[] { -.6f, .6f }) { var p0 = new Vector3(x, Mathf.Cos(a0), Mathf.Sin(a0)) * 1f; var q0 = new Vector3(x, Mathf.Cos(a0) * 3.4f, Mathf.Sin(a0) * 3.4f); var q1 = new Vector3(x, Mathf.Cos(a1) * 3.4f, Mathf.Sin(a1) * 3.4f); wk.Box(wheelWood, (q0 + q1) / 2f, new Vector3(.12f, .18f, Vector3.Distance(q0, q1) + .05f), Quaternion.LookRotation(q1 - q0)); }
                var radial = new Vector3(0, Mathf.Cos(a0), Mathf.Sin(a0)); var tangent = new Vector3(0, -Mathf.Sin(a0), Mathf.Cos(a0)); var turn = Quaternion.LookRotation(tangent, radial);
                wk.Box(wheelWood, radial * 3.2f, new Vector3(1.3f, .5f, .08f), turn);
                if (k % 2 == 0) foreach (var x in new[] { -.6f, .6f }) wk.Box(wheelWood, new Vector3(x, 0, 0) + radial * 1.7f, new Vector3(.1f, 3.4f, .12f), turn);
            }
            wk.Prism(wheelWood, new Vector3(-1.2f, 0, 0), Vector3.right, .35f, 2.4f, 8);
            var wheel = wk.BuildSingle("Water wheel", life.transform, false).transform;
            wheel.SetPositionAndRotation(wc, Quaternion.FromToRotation(Vector3.right, side.normalized));
            life.wheel = wheel; life.wheelAxis = side;
            ArtistBits.PostCollider(ctx.Root, wc - up * 3f, 6.5f, 3.2f);
            noGrass.Add(new Vector3(hc.x, 6f, hc.z));
            var mark = new GameObject("Shot: the mill").transform; mark.SetParent(ctx.Root, false);
            var cam = c0 - dir * 16f + side * (CreekW / 2f + 12f); cam = ctx.OnGround(cam.x, cam.z); var look = wc - cam; look.y = 0; mark.SetPositionAndRotation(cam, Quaternion.LookRotation(look.normalized));
            Remember(hc - side * 4.5f, 402);
        }
        float millSide = 1f;

        // ---------------------------------------------------------------- trails
        void Trails(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var todo = new List<IslandWorldData.Artist>(ctx.Artists); var walk = 1; var lamps = new List<Vector3>(); var bridges = 0;
            var dirt = WorldMaterials.Solid(new Color(.66f, .55f, .38f)); var longest = new List<Vector3>();
            while (todo.Count > 0)
            {
                IslandWorldData.Artist next = null; Vector3 join = default; var best = float.MaxValue; var joinW = 0;
                foreach (var a in todo)
                    foreach (var (p, w) in walkPoints) { if (w >= 400) continue; var d = Vector2.Distance(new Vector2(p.x, p.z), new Vector2(a.x, a.z)) - Foot(a); if (d < best) { best = d; next = a; join = p; joinW = w; } }
                if (next == null) break;
                todo.Remove(next);
                var from = new Vector3(join.x, 0, join.z); var to = new Vector3(next.x, 0, next.z);
                var dir = to - from; var total = dir.magnitude; if (total < 2f) continue; dir /= total;
                facing[next.name] = -dir;
                var side = Vector3.Cross(Vector3.up, dir); var end = total - Foot(next) * .8f;
                var pts = new List<Vector3>(); var wet = new List<bool>();
                var atHeart = Vector2.Distance(new Vector2(join.x, join.z), new Vector2(center.x, center.z)) < 1f;
                for (var d = atHeart ? 8f : joinW == 0 ? 3.1f : TrailW / 2f + .1f; ; d += 1.5f)
                {
                    var dd = Mathf.Min(d, end); var q = from + dir * dd + side * Mathf.Sin(dd / 18f) * Mathf.Min(3f, end * .05f);
                    if (!ctx.IsLand(q.x, q.z, 0f)) break;
                    pts.Add(ctx.OnGround(q.x, q.z)); wet.Add(InCreek(q.x, q.z, 2.5f)); if (dd >= end) break;
                }
                if (pts.Count < 2) continue;
                // where the trail crosses the creek: lift it onto a bridge
                for (var i = 0; i < pts.Count; i++)
                {
                    if (!wet[i]) continue;
                    var j = i; while (j + 1 < pts.Count && wet[j + 1]) j++;
                    var i0 = Mathf.Max(0, i - 1); var i1 = Mathf.Min(pts.Count - 1, j + 1);
                    // the deck runs from bank to bank (sloping if one bank is higher), always clear of the water
                    float ya = pts[i0].y + .35f, yb = pts[i1].y + .35f;
                    for (var k = i0; k <= i1; k++)
                    {
                        var p = pts[k]; CreekDist(p.x, p.z, out var bedHere);
                        p.y = Mathf.Max(Mathf.Lerp(ya, yb, (k - i0) / (float)Mathf.Max(1, i1 - i0)), bedHere + 1.7f); pts[k] = p;
                    }
                    ya = pts[i0].y; yb = pts[i1].y;
                    var a = pts[i0] - dir * 1f; var b = pts[i1] + dir * 1f; a.y = ya; b.y = yb;
                    var covered = bridges == 0; Bridge(ctx, mk, solid, a, b, covered); bridges++;
                    if (covered) { var mark = new GameObject("Shot: the covered bridge").transform; mark.SetParent(ctx.Root, false); var cam = (a + b) / 2f - side * 14f - dir * 9f; mark.SetPositionAndRotation(ctx.OnGround(cam.x, cam.z), Quaternion.LookRotation(((a + b) / 2f - cam).normalized)); }
                    i = j + 1;
                }
                Strip(mk, pts, dirt, TrailW, .2f, ctx);
                foreach (var p in pts) Remember(p, walk);
                if (pts.Count > longest.Count) longest = pts;
                for (var i = 7; i < pts.Count; i += 14)
                {
                    var lp = pts[i] + side * ((i / 14) % 2 == 0 ? 1f : -1f) * (TrailW / 2f + .6f);
                    var near = false; foreach (var o in lamps) if ((o - lp).sqrMagnitude < 20f * 20f) near = true;
                    if (near || InCreek(lp.x, lp.z, 2f)) continue;
                    lamps.Add(lp); JarLamp(ctx, mk, ctx.OnGround(lp.x, lp.z));
                }
                // a split-rail fence along part of some trails
                if (walk % 3 == 1 && pts.Count > 16) { var fs = pts[4] + side * (TrailW / 2f + 2f); var fe = pts[Mathf.Min(pts.Count - 5, 4 + 22)] + side * (TrailW / 2f + 2f); if (!InCreek(fs.x, fs.z, 3f) && !InCreek(fe.x, fe.z, 3f)) Fence(ctx, mk, solid, fs, fe); }
                walk++;
            }
            life.route = longest.ToArray();
        }

        // ---------------------------------------------------------------- the hollows
        /// Hardwoods thick in the hollows and on the slopes, rhododendrons, porch cabins with rockers, hay bales in the
        /// meadows, beehives, a laundry line, corn patches, an old pickup, mossy boulders.
        void Hollows(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            int trees = 0, cabins = 0, bales = 0, other = 0, stills = 0;
            const float cell = 17f;
            for (var x = ctx.MinX; x < ctx.MaxX; x += cell)
            for (var z = ctx.MinZ; z < ctx.MaxZ; z += cell)
            {
                float px = x + ctx.Range(0f, cell), pz = z + ctx.Range(0f, cell);
                if (!Open(ctx, px, pz, 3f)) continue;
                var p = ctx.OnGround(px, pz); var roll = ctx.Range(0f, 1f);
                // meadows in the open (Perlin), woods elsewhere
                var meadow = Mathf.PerlinNoise(px * .006f + 11f, pz * .006f + 7f) > .55f;
                if (roll < .035f && cabins < 8 && NearPath(px, pz, 22f)) { Cabin(ctx, mk, solid, p); cabins++; }
                else if (meadow)
                {
                    if (roll < .12f) { HayBale(mk, solid, p, ctx.Range(0f, 360f)); bales++; }
                    else if (roll < .15f) { Beehives(ctx, mk, solid, p); other++; }
                    else if (roll < .17f) { Corn(ctx, mk, p); other++; }
                    else if (roll < .19f) { Pickup(ctx, mk, solid, p); other++; }
                    else if (roll < .3f) { Hardwood(ctx, mk, p, ctx.Range(1.1f, 1.6f)); trees++; }
                    else if (roll < .36f) { Rhododendron(ctx, p); other++; }
                }
                else
                {
                    if (roll < .72f) { Hardwood(ctx, mk, p, ctx.Range(.85f, 1.45f)); trees++; }
                    else if (roll < .84f) { Rhododendron(ctx, p); other++; }
                    else if (roll < .925f && stills < 5 && !NearPath(px, pz, 10f)) { Moonshine(ctx, mk, solid, p); stills++; }
                    else if (roll < .9f) { var br = new Vector3(ctx.Range(1f, 2.2f), ctx.Range(.6f, 1.3f), ctx.Range(1f, 2f)); mk.Blob(stone, p + Vector3.up * .5f, br, ctx.Rng, .35f); crowns.Blob(leaves[2], p + Vector3.up * 1.2f, new Vector3(1.1f, .2f, 1f), ctx.Rng, .3f); solid.Blob(stone, p + Vector3.up * .5f, br * 1.15f, Smooth, 0f); other++; }   // the boulder's own shape (a box was far smaller)
                }
            }
            Debug.Log($"The Bluegrass Hollows: creek {creek.Count * 4} m, {trees} hardwoods, {cabins} cabins, {bales} hay bales, {stills} stills, {other} other things, street {street.Count * 6} m");
        }

        void HayBale(MeshKit mk, MeshKit solid, Vector3 at, float yaw)
        {
            var d = Quaternion.Euler(0, yaw, 0) * Vector3.right;
            mk.Prism(hay, at + Vector3.up * .75f - d * .7f, d, .75f, 1.4f, 12); solid.Box(hay, at + Vector3.up * .75f, new Vector3(1.5f, 1.5f, 1.4f), Quaternion.LookRotation(d));
        }

        void Beehives(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 at)
        {
            for (var k = 0; k < 3; k++)
            {
                var p = at + Vector3.right * (k * 1.4f - 1.4f); p = ctx.OnGround(p.x, p.z);
                for (var l = 0; l < 3; l++) mk.Box(l % 2 == 0 ? trim : WorldMaterials.Solid(new Color(.95f, .85f, .5f)), p + Vector3.up * (.3f + l * .3f), new Vector3(.6f, .28f, .5f), Quaternion.identity);
                mk.Box(roofM, p + Vector3.up * 1.2f, new Vector3(.75f, .08f, .65f), Quaternion.identity);
            }
            solid.Box(trim, at + Vector3.up * .6f, new Vector3(4f, 1.2f, .7f), Quaternion.identity);
        }

        void Corn(LandContext ctx, MeshKit mk, Vector3 at)
        {
            var stalk = WorldMaterials.Solid(new Color(.5f, .62f, .28f)); var tassel = WorldMaterials.Solid(new Color(.88f, .78f, .4f));
            for (var i = 0; i < 4; i++) for (var j = 0; j < 6; j++)
            {
                var p = at + new Vector3(i * 1.1f - 1.6f, 0, j * .8f - 2f); p = ctx.OnGround(p.x, p.z);
                crowns.Box(stalk, p + Vector3.up * .9f, new Vector3(.08f, 1.8f, .08f), Quaternion.Euler(ctx.Range(-6f, 6f), 0, ctx.Range(-6f, 6f)));
                crowns.Box(stalk, p + Vector3.up * 1.1f, new Vector3(.05f, .05f, .8f), Quaternion.Euler(-30f, ctx.Range(0, 180f), 0));
                crowns.Box(tassel, p + Vector3.up * 1.9f, new Vector3(.06f, .3f, .06f), Quaternion.identity);
            }
            noGrass.Add(new Vector3(at.x, 3f, at.z));
        }

        void Pickup(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 at)
        {
            var up = Vector3.up; var dir = Quaternion.Euler(0, ctx.Range(0, 360f), 0) * Vector3.forward; var rot = Quaternion.LookRotation(dir); var side = Vector3.Cross(up, dir);
            var paint = WorldMaterials.Solid(new Color(.3f, .5f, .6f), .6f); var rust = WorldMaterials.Solid(new Color(.55f, .3f, .18f));
            mk.Box(paint, at + up * .8f + dir * .9f, new Vector3(1.8f, .7f, 2f), rot); mk.Box(paint, at + up * 1.45f + dir * .2f, new Vector3(1.6f, .8f, 1.3f), rot);
            mk.Box(WorldMaterials.Solid(new Color(.2f, .25f, .3f), .9f), at + up * 1.5f + dir * .2f, new Vector3(1.66f, .45f, 1.2f), rot);
            mk.Box(rust, at + up * .75f - dir * 1.3f, new Vector3(1.8f, .6f, 2.2f), rot);
            foreach (var s in new[] { -1f, 1f }) foreach (var zz in new[] { -1.3f, 1.3f }) mk.Prism(dark, at + side * s * .9f + dir * zz + up * .38f, side * s, .38f, .25f, 10);
            solid.Box(paint, at + up * .9f, new Vector3(2f, 1.8f, 4.6f), rot);
        }

        /// A little cabin with a porch, two rockers on it, lit windows, a stone chimney with smoke.
        void Cabin(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 at)
        {
            var up = Vector3.up; var face = new Vector3(center.x - at.x, 0, center.z - at.z).normalized; var rot = Quaternion.LookRotation(face); var side = Vector3.Cross(up, face);
            var logs = WorldMaterials.Solid(new Color(.5f, .38f, .27f));
            mk.Box(logs, at + up * 1.5f, new Vector3(6f, 3f, 5f), rot); solid.Box(logs, at + up * 1.5f, new Vector3(6f, 3f, 5f), rot);
            for (var k = 1; k < 6; k++) mk.Box(WorldMaterials.Solid(new Color(.4f, .3f, .22f)), at + up * (.25f + k * .5f) + face * 2.52f, new Vector3(6.05f, .08f, .04f), rot);
            mk.GableRoof(roofM, at + up * 3f, new Vector3(6.8f, 0, 6.6f), 1.8f, Quaternion.LookRotation(side));
            mk.Box(plank, at + up * .3f + face * 3.706f, new Vector3(6f, .2f, 2.4f), rot); solid.Box(plank, at + up * .2f + face * 3.7f, new Vector3(6f, .4f, 2.4f), rot);
            foreach (var s in new[] { -2.8f, 2.8f }) mk.Box(wood, at + up * 1.6f + face * 4.7f + side * s, new Vector3(.15f, 2.6f, .15f), rot);
            mk.Box(roofM, at + up * 2.85f + face * 3.8f, new Vector3(6.4f, .1f, 2.6f), rot * Quaternion.Euler(10f, 0, 0));
            mk.Box(dark, at + up * 1.2f + face * 2.53f, new Vector3(1f, 2f, .04f), rot);
            foreach (var s in new[] { -1.9f, 1.9f }) mk.Box(GlowM(Warm, 1.6f), at + up * 1.7f + face * 2.53f + side * s, new Vector3(.9f, .8f, .04f), rot);
            foreach (var s in new[] { -1.2f, 1.2f }) Rocker(mk, solid, ctx, at + up * .4f + face * 3.6f + side * (s + .1f), face);
            mk.Prism(GlowM(Warm, 2.6f), at + up * 2.3f + face * 4.6f, up, .12f, .25f, 8); halos.Add(at + up * 2.4f + face * 4.6f, .9f, Warm, .8f);
            var ch = at + up * 3.8f - side * 2.4f; mk.Box(stone, ch - up * 1.3f, new Vector3(.9f, 5.2f, .9f), rot);
            life.chimneys.Add(ch + up * 1.4f);
            noGrass.Add(new Vector3(at.x, 6f, at.z));
        }

        void GroundCover(LandContext ctx)
        {
            var mat = WindMesh.Material(.3f, 1f, new Color(.4f, .58f, .26f));
            var meshes = new Dictionary<(int, int), WindMesh>(); var rng = new System.Random(1946);
            const float step = 2.2f; Color[] flowers = { new Color(1f, .85f, .2f), new Color(.95f, .95f, .95f), new Color(.55f, .6f, 1f), new Color(1f, .55f, .2f) };
            for (var x = ctx.MinX; x < ctx.MaxX; x += step)
            for (var z = ctx.MinZ; z < ctx.MaxZ; z += step)
            {
                var px = x + ((float)rng.NextDouble() - .5f) * step; var pz = z + ((float)rng.NextDouble() - .5f) * step;
                if (!ctx.IsLand(px, pz, 1f) || NearPath(px, pz, 2.4f) || InNoGrass(px, pz, 0f) || InFootprint(ctx, px, pz, -2f) || InCreek(px, pz, 1f)) continue;
                if (Mathf.PerlinNoise(px * .03f + 9f, pz * .03f + 2f) < .33f) continue;
                var key = (Mathf.FloorToInt(px / 70f), Mathf.FloorToInt(pz / 70f));
                if (!meshes.TryGetValue(key, out var wm)) meshes[key] = wm = new WindMesh();
                var col = Color.Lerp(new Color(.4f, .58f, .26f), new Color(.58f, .68f, .3f), (float)rng.NextDouble());
                var at = new Vector3(px, ctx.Ground(px, pz) - .04f, pz);
                wm.Tuft(at, .45f + (float)rng.NextDouble() * .45f, col, Color.Lerp(col, new Color(.9f, .92f, .6f), .3f), rng, 4, .07f);
                if (rng.NextDouble() < .09) wm.Flower(at + new Vector3(.1f, 0, .1f), .4f, flowers[rng.Next(flowers.Length)], rng);
            }
            foreach (var kv in meshes) kv.Value.Build("Bluegrass grass", ctx.Root, mat);
        }
    }
}
