using System.Collections.Generic;
using UnityEngine;
using static EverythingWorld.World.PlaceTools;

namespace EverythingWorld.World
{
    /// The 8-Bit Isle (WORLD_GUIDE.md): a video-game world at a pixel sunset, where every game is its own little world.
    /// You arrive at the Arcade: a checkered floor of blue tiles, a giant arcade cabinet with a glowing screen, a ring of
    /// cabinets to sit at, PRESS START blinking in the air and gold coins spinning everywhere. The Level Road, a road of
    /// brick blocks with gold blocks floating over it and green pipes beside it, runs to the Ring: a vast silver ring rising
    /// out of the ground into the sky. Round the island: a farm in four seasons, a neon stage, a blocky hillside with a mine,
    /// and a little world for every game. Everything is made of blocks.
    /// Laid out on purpose (the build-land standard): BitTown.cs has the places and the people.
    public partial class BitKit : LandKit
    {
        public override string Land => "The 8-Bit Isle";
        public override Vector3 ArtistFront(IslandWorldData data, IslandWorldData.Artist a) => facing.TryGetValue(a.name, out var f) ? f : base.ArtistFront(data, a);

        public static readonly Color Gold = new Color(1f, .82f, .2f), Brick = new Color(.76f, .36f, .2f), Pipe = new Color(.2f, .7f, .3f), Sky = new Color(.36f, .6f, 1f), Pink = new Color(1f, .4f, .7f), Cyan = new Color(.3f, .9f, 1f), Ink = new Color(.06f, .06f, .12f), Leaf = new Color(.24f, .62f, .26f);
        const float PathW = 3f, RoadW = 6f;

        Material brick, brickD, tileA, tileB, pipeM, goldM, iron, dark, white, leafM, trunkM, dirt, lampM, coinM, screenM, blockM, stoneM;
        GlowHalos halos; Vector3 center, D, S; BitLife life;
        readonly Dictionary<(int, int), List<Vector3>> pathCells = new Dictionary<(int, int), List<Vector3>>();
        readonly List<(Vector3 p, int w)> walkPoints = new List<(Vector3, int)>();
        readonly Dictionary<string, Vector3> facing = new Dictionary<string, Vector3>();
        // x, radius, z
        readonly List<Vector3> noGrass = new List<Vector3>();
        Vector2 heart2, D2 = new Vector2(0, -1), S2 = new Vector2(1, 0); float plazaY, waterY, streetL;

        void MakeMaterials()
        {
            brick = WorldMaterials.Glow(Brick, .14f); brickD = WorldMaterials.Glow(new Color(.5f, .22f, .14f), .12f); tileA = WorldMaterials.Glow(new Color(.16f, .24f, .6f), .2f); tileB = WorldMaterials.Glow(new Color(.3f, .44f, .86f), .2f); pipeM = WorldMaterials.Glow(Pipe, .25f); goldM = WorldMaterials.Glow(Gold, .5f);
            iron = WorldMaterials.Solid(new Color(.16f, .16f, .2f), .5f); dark = WorldMaterials.Solid(new Color(.05f, .05f, .08f)); white = WorldMaterials.Glow(new Color(.9f, .9f, .94f), .16f); leafM = WorldMaterials.Glow(Leaf, .14f); trunkM = WorldMaterials.Glow(new Color(.5f, .32f, .18f), .12f); dirt = WorldMaterials.Glow(new Color(.5f, .34f, .2f), .12f); stoneM = WorldMaterials.Glow(new Color(.46f, .46f, .5f), .12f);
            lampM = new Material(WorldMaterials.Glow(new Color(1f, .9f, .6f), 1.6f)) { name = "Bit lamps" }; coinM = new Material(WorldMaterials.Glow(Gold, 1.4f)) { name = "Bit coins" }; screenM = new Material(WorldMaterials.Glow(Cyan, 1.2f)) { name = "Bit screens" }; blockM = new Material(WorldMaterials.Glow(Gold, .9f)) { name = "Bit gold blocks" };
        }

        Material GlowM(Color c, float s = 2f) => WorldMaterials.Glow(c, s * .65f);
        Material Paint(Color c) => WorldMaterials.Glow(c, .14f);
        static bool HasStation(IslandWorldData.Artist a) => a.stationId != 0;
        static float Foot(IslandWorldData.Artist a) => a.name == "ConcernedApe" ? 18f : a.name == "meganeko" ? 14f : 8f;

        public override bool Ground(float height, float slope, float patch, out Color color)
        {
            // blocky pixel grass: three flat greens in steps, stone on the slopes
            var q = Mathf.Floor(patch * 3f) / 2f;
            if (slope > 26f) color = Color.Lerp(new Color(.44f, .44f, .48f), new Color(.52f, .5f, .54f), q);
            else color = Color.Lerp(new Color(.22f, .56f, .24f), new Color(.34f, .68f, .28f), q);
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
            for (var k = 0; k < 8; k++) { var dir = new Vector2(Mathf.Sin(k * Mathf.PI / 4f), Mathf.Cos(k * Mathf.PI / 4f)); var len = 40f; while (len < 110f && Inside(heart2 + dir * (len + 10f), 44f)) len += 10f; if (len > streetL) { streetL = len; D2 = dir; } }
            S2 = new Vector2(-D2.y, D2.x); plazaY = Mathf.Max(data.HeightAt(heart2.x, heart2.y), waterY + 6f);
            var keep = new List<string>();
            void Put(string name, float d, float s, float room) { var p = P2(d, s); if (!Inside(p, room)) return; foreach (var a in data.layout.artists) if (a.sceneId == isl.sceneId && a.name == name) { a.x = p.x; a.z = p.y; keep.Add(name); } }
            Put("ConcernedApe", 0f, -58f, 28f); Put("meganeko", 4f, 54f, 24f);
            Put("Martin O'Donnell", streetL + 14f, -20f, 14f); Put("Halo", streetL + 30f, 0f, 14f); Put("Jack Wall", streetL + 14f, 20f, 14f);
            Put("C418", -40f, -26f, 14f); Put("Lena Raine", -52f, -6f, 14f); Put("Captainsparklez", -42f, 16f, 14f); Put("Brad Knauber", -60f, 28f, 14f);
            var avoid = new List<Vector3>(); for (var d = 0f; d <= streetL; d += 22f) avoid.Add(new Vector3(heart2.x + D2.x * d, 20f, heart2.y + D2.y * d)); avoid.Add(new Vector3(heart2.x + D2.x * (streetL + 10f), 24f, heart2.y + D2.y * (streetL + 10f)));
            ArtistPlacement.Spread(data, index, 50f, heart2, 44f, 40f, avoid, keep, Land, 330f);
            Level(data, index, heart2, 24f, 40f, plazaY);
            for (var d = 26f; d <= streetL + 10f; d += 10f) { var p = P2(d, 0); Level(data, index, p, 5f, 12f, Mathf.Lerp(plazaY, Mathf.Max(data.HeightAt(p.x, p.y), waterY + 3f), Mathf.Clamp01((d - 26f) / 30f))); }
            foreach (var a in data.layout.artists)
            {
                if (a.sceneId != isl.sceneId) continue;
                a.r = Mathf.Max(a.r, Foot(a) + 6f);
                Level(data, index, new Vector2(a.x, a.z), Foot(a) + 1f, Foot(a) + 10f, Mathf.Max(data.HeightAt(a.x, a.z), waterY + 2.6f));
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
            life = new GameObject("Bit life").AddComponent<BitLife>();
            life.transform.SetParent(ctx.Root, false);
            life.center = center; life.lamps = lampM; life.coins = coinM; life.screens = screenM; life.blocks = blockM;
            var mk = new MeshKit(); var solid = new MeshKit();
            Remember(ctx.OnGround(center.x, center.z), 0);
            TownStart(ctx);
            Arcade(ctx, mk, solid);
            LevelRoad(ctx, mk, solid);
            TheRing(ctx, mk, solid);
            Paths(ctx, mk, solid);
            Wilds(ctx, mk, solid);
            mk.Build("The 8-Bit Isle", ctx.Root, false);
            solid.BuildCollider("The 8-Bit Isle walls", ctx.Root);
        }

        public override void Finish(LandContext ctx) { Verge(ctx); halos.Build("Bit glow", ctx.Root); }

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
            var up = Vector3.up; var rot = Quaternion.LookRotation(face); var side = Vector3.Cross(up, face); mk.Box(trunkM, g + up * .4f, new Vector3(len, .2f, .5f), rot); foreach (var e in new[] { -1f, 1f }) mk.Box(stoneM, g + up * .15f + side * e * (len / 2f - .25f), new Vector3(.5f, .3f, .5f), rot);
            BoxCol(parent, g + up * .25f, new Vector3(len, .5f, .5f), rot); Interact.Bench(tlife.transform, g + up * .5f - side * (len / 2f - .5f), g + up * .5f + side * (len / 2f - .5f), face);
        }

        /// A pixel lamp: a block of light on a black post.
        void Lamp(LandContext ctx, MeshKit mk, Vector3 g, bool light = false)
        {
            var up = Vector3.up; mk.Box(iron, g + up * 1.6f, new Vector3(.16f, 3.4f, .16f), Quaternion.identity); mk.Box(lampM, g + up * 3.5f, new Vector3(.5f, .5f, .5f), Quaternion.identity); mk.Box(iron, g + up * 3.8f, new Vector3(.6f, .1f, .6f), Quaternion.identity); halos.Add(g + up * 3.5f, 1.4f, new Color(1f, .9f, .6f), .45f); ArtistBits.PostCollider(ctx.Root, g, 3.4f, .12f); if (light) ArtistBits.PointLight(ctx.Root, g + up * 3f, new Color(1f, .88f, .6f), 12f, 1.4f);
        }

        /// A spinning gold coin in the air (it turns in BitLife).
        void Coin(Transform parent, Vector3 at)
        {
            var c = new MeshKit(); c.Prism(coinM, -Vector3.forward * .04f, Vector3.forward, .34f, .08f, 10); c.Box(GlowM(new Color(1f, .96f, .6f), 1.6f), Vector3.forward * .05f, new Vector3(.1f, .36f, .02f), Quaternion.identity);
            var go = c.BuildSingle("Coin", parent, false); go.transform.position = at; life.spinners.Add((go.transform, at));
        }

        /// A block tree: a square trunk and two cubes of leaves.
        void BlockTree(LandContext ctx, MeshKit mk, Vector3 g, float s, Material leaves)
        {
            var up = Vector3.up; mk.Box(trunkM, g + up * 1.5f * s, new Vector3(.6f, 3f, .6f) * s, Quaternion.identity); mk.Box(leaves, g + up * 3.4f * s, new Vector3(2.6f, 1.6f, 2.6f) * s, Quaternion.identity); mk.Box(leaves, g + up * 4.6f * s, new Vector3(1.6f, 1f, 1.6f) * s, Quaternion.identity); ArtistBits.PostCollider(ctx.Root, g, 3f * s, .4f * s);
        }

        /// A green pipe standing out of the ground.
        void GreenPipe(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 g, float h)
        {
            var up = Vector3.up; mk.Prism(pipeM, g - up * .3f, up, .9f, h + .3f, 12); mk.Prism(pipeM, g + up * h, up, 1.1f, .7f, 12); mk.Prism(dark, g + up * (h + .7f), up, .8f, .02f, 12); mk.Box(GlowM(new Color(.6f, 1f, .6f), .6f), g + up * (h + .35f) + Vector3.forward * 1.1f + Vector3.right * .4f, new Vector3(.2f, .5f, .02f), Quaternion.identity); solid.Prism(pipeM, g - up * .3f, up, 1.1f, h + 1f, 8);
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
                Strip(mk, pts, Paint(new Color(.82f, .7f, .46f)), PathW, .13f, ctx);
                foreach (var p in pts) Remember(p, walk);
                for (var i = 6; i < pts.Count; i += 10, n++)
                {
                    var s = n % 2 == 0 ? 1f : -1f; var lp = pts[i] + side * s * (PathW / 2f + .8f);
                    if (InFootprint(ctx, lp.x, lp.z, 1f) || InNoGrass(lp.x, lp.z, 1f)) continue;
                    Lamp(ctx, mk, ctx.OnGround(lp.x, lp.z), n % 3 == 0);
                    if (n % 2 == 0) Coin(ctx.Root, pts[i] + Vector3.up * 1.6f);
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

        // ---------------------------------------------------------------- the rest of the island
        /// Block trees, green pipes, gold blocks floating in the air, rows of coins, red mushrooms, stacked-block hills, benches under pixel lamps, the odd arcade cabinet.
        void Wilds(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            int trees = 0, pipes = 0, blocks = 0, coins = 0, shrooms = 0, hills = 0, benches = 0, cabs = 0; var rng = new System.Random(1985);
            const float cell = 20f; var leafs = new[] { leafM, Paint(new Color(.3f, .7f, .3f)), Paint(new Color(.9f, .5f, .7f)), Paint(new Color(.96f, .66f, .2f)) };
            for (var x = ctx.MinX; x < ctx.MaxX; x += cell)
            for (var z = ctx.MinZ; z < ctx.MaxZ; z += cell)
            {
                float px = x + (float)rng.NextDouble() * cell, pz = z + (float)rng.NextDouble() * cell; var roll = (float)rng.NextDouble(); var s = (float)rng.NextDouble();
                if (!Open(ctx, px, pz, 3f)) continue;
                var p = ctx.OnGround(px, pz); var f = FaceTo(p, NearestWalk(p)); var rot = Quaternion.LookRotation(f); var side = Vector3.Cross(Vector3.up, f); var up = Vector3.up;
                if (roll < .3f) { for (var k = 0; k < 1 + (int)(s * 3f); k++) BlockTree(ctx, mk, ctx.OnGround(p.x + k * 2.6f * side.x - (k % 2) * 2f * f.x, p.z + k * 2.6f * side.z - (k % 2) * 2f * f.z), .8f + s * .5f, leafs[(int)(s * 7f + k) % (s > .8f ? 4 : 2)]); Mark(p, 2.4f); trees++; }
                else if (roll < .38f) { GreenPipe(ctx, mk, solid, p, 1f + s * 2f); Mark(p, 1.6f); pipes++; }
                else if (roll < .48f) { var bp = p + up * (3.4f + s * 1.4f); for (var k = 0; k < 3 + (int)(s * 3f); k++) { var b = bp + side * (k * 1.02f); var gold = (k + (int)(s * 5f)) % 3 == 1; mk.Box(gold ? blockM : brick, b, Vector3.one, rot); if (!gold) for (var j = 0; j < 2; j++) mk.Box(brickD, b + f * .51f + up * (j * .5f - .25f), new Vector3(1f, .04f, .02f), rot); else mk.Box(GlowM(new Color(1f, .96f, .7f), 1.4f), b + f * .51f, new Vector3(.2f, .5f, .02f), rot); } blocks++; }
                else if (roll < .56f) { for (var k = 0; k < 5; k++) Coin(ctx.Root, p + side * (k - 2) * 1.2f + up * (1.2f + Mathf.Sin(k / 4f * Mathf.PI) * 1.2f)); coins++; }
                else if (roll < .64f) { for (var k = 0; k < 2 + (int)(s * 3f); k++) { var mp = p + Quaternion.Euler(0, k * 100f, 0) * f * (k * .9f); var h = .4f + (k % 2) * .4f; mk.Box(white, mp + up * h / 2f, new Vector3(.3f, h, .3f), Quaternion.identity); mk.Box(Paint(new Color(.9f, .16f, .14f)), mp + up * (h + .2f), new Vector3(.9f, .4f, .9f), Quaternion.identity); for (var j = 0; j < 4; j++) mk.Box(white, mp + up * (h + .41f) + Quaternion.Euler(0, j * 90f, 0) * Vector3.forward * .26f, new Vector3(.18f, .02f, .18f), Quaternion.identity); } ArtistBits.PostCollider(ctx.Root, p, 1.4f, 1f); shrooms++; }
                else if (roll < .72f) { if (!Open(ctx, px, pz, 5f)) continue; for (var k = 0; k < 4; k++) { var w = 5f - k * 1.2f; mk.Box(k == 3 ? Paint(new Color(.34f, .7f, .3f)) : dirt, p + up * (k + .5f - .4f), new Vector3(w, 1f, w), rot); mk.Box(Paint(new Color(.34f, .7f, .3f)), p + up * (k + .94f - .4f), new Vector3(w + .02f, .14f, w + .02f), rot); solid.Box(dirt, p + up * (k + .5f - .4f), new Vector3(w, 1f, w), rot); } Mark(p, 3f); hills++; }
                else if (roll < .8f) { Bench(ctx.Root, mk, p, f); Lamp(ctx, mk, p - f * .8f + side * 1.6f, benches % 2 == 0); Mark(p, 2.2f); benches++; }
                else if (roll < .83f && cabs < 10) { Cabinet(ctx, mk, solid, p, f, cabs); Mark(p, 1.6f); cabs++; }
            }
            Debug.Log($"The 8-Bit Isle: Level Road {streetL:0} m, {trees} block groves, {pipes} pipes, {blocks} rows of blocks, {coins} coin arcs, {shrooms} mushroom patches, {hills} block hills, {benches} benches, {cabs} arcade cabinets");
        }

        static readonly Color[] CabCols = { new Color(.9f, .2f, .3f), new Color(.2f, .4f, .9f), new Color(.96f, .7f, .1f), new Color(.5f, .2f, .8f), new Color(.1f, .7f, .5f) };
        /// An arcade cabinet with a glowing screen, a joystick and buttons, and a stool in front.
        void Cabinet(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 g, Vector3 face, int n, float s = 1f)
        {
            var up = Vector3.up; var rot = Quaternion.LookRotation(face); var side = Vector3.Cross(up, face); var col = Paint(CabCols[n % CabCols.Length]);
            mk.Box(col, g + up * .9f * s, new Vector3(.9f, 1.8f, .8f) * s, rot); mk.Box(dark, g + up * 1.95f * s + face * .1f * s, new Vector3(.9f, .3f, .6f) * s, rot); mk.Box(GlowM(CabCols[(n + 2) % CabCols.Length], 1.6f), g + up * 2f * s + face * .41f * s, new Vector3(.8f, .2f, .02f) * s, rot);
            mk.Box(screenM, g + up * 1.5f * s + face * .5f * s, new Vector3(.7f, .55f, .02f) * s, rot * Quaternion.Euler(-12f, 0, 0)); mk.Box(dark, g + up * 1.05f * s + face * .5f * s, new Vector3(.9f, .1f, .3f) * s, rot);
            mk.Prism(dark, g + up * 1.1f * s + face * .5f * s - side * .2f * s, up, .02f * s, .16f * s, 4); mk.Blob(GlowM(new Color(1f, .2f, .2f), 1.4f), g + up * 1.28f * s + face * .5f * s - side * .2f * s, Vector3.one * .05f * s, new System.Random(n), 0f); for (var k = 0; k < 3; k++) mk.Prism(GlowM(CabCols[(n + k + 1) % CabCols.Length], 1.4f), g + up * 1.1f * s + face * .5f * s + side * (.05f + k * .1f) * s, up, .03f * s, .03f * s, 6);
            solid.Box(col, g + up * 1f * s, new Vector3(.9f, 2f, .9f) * s, rot);
            if (s <= 1f) { var st = g + face * 1f; mk.Box(iron, st + up * .3f, new Vector3(.1f, .6f, .1f), rot); mk.Box(Paint(new Color(.9f, .2f, .3f)), st + up * .64f, new Vector3(.44f, .1f, .44f), rot); Interact.Seat(tlife.transform, st + up * .68f, -face, "Play a game").seatHeight = .65f; }
        }

        Vector3 NearestWalk(Vector3 p) { var best = center; var bd = float.MaxValue; foreach (var (q, _) in walkPoints) { var d = (q - p).sqrMagnitude; if (d < bd) { bd = d; best = q; } } return best; }

        /// Short square tufts of pixel grass.
        void Verge(LandContext ctx)
        {
            var mat = WindMesh.Material(.25f, .8f, new Color(.28f, .62f, .26f));
            var meshes = new Dictionary<(int, int), WindMesh>(); var rng = new System.Random(1986);
            const float st = 2.6f;
            for (var x = ctx.MinX; x < ctx.MaxX; x += st)
            for (var z = ctx.MinZ; z < ctx.MaxZ; z += st)
            {
                var px = x + ((float)rng.NextDouble() - .5f) * st; var pz = z + ((float)rng.NextDouble() - .5f) * st;
                if (!ctx.IsLand(px, pz, 1f) || NearPath(px, pz, 3.6f) || InNoGrass(px, pz, 0f) || InFootprint(ctx, px, pz, -2f)) continue;
                if (Mathf.PerlinNoise(px * .05f + 1f, pz * .05f + 6f) < .5f) continue;
                var key = (Mathf.FloorToInt(px / 70f), Mathf.FloorToInt(pz / 70f));
                if (!meshes.TryGetValue(key, out var wm)) meshes[key] = wm = new WindMesh();
                var col = (float)rng.NextDouble() < .5f ? new Color(.26f, .6f, .24f) : new Color(.38f, .72f, .3f);
                wm.Tuft(new Vector3(px, ctx.Ground(px, pz) - .04f, pz), .26f + (float)rng.NextDouble() * .2f, col, Color.Lerp(col, Gold, .3f), rng, 3, .1f);
            }
            foreach (var kv in meshes) kv.Value.Build("Bit grass", ctx.Root, mat);
        }
    }
}
