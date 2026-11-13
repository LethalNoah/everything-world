using System.Collections.Generic;
using UnityEngine;
using static EverythingWorld.World.PlaceTools;

namespace EverythingWorld.World
{
    /// The Synthwave Coast (WORLD_GUIDE.md): a neon night drive on the coast. A deep violet night, a giant striped
    /// sun sitting on the sea at the end of the road, palms with neon bands, chrome cars, a glowing grid laid over the
    /// dark ground. You arrive at the Neon Diner. The Coast Road runs from it straight toward the sun, to Sunset
    /// Point on the cliff. Driving at night with the windows down, a little nostalgic for a time you never had.
    /// Laid out on purpose (the build-land standard): SynthTown.cs has the places and the people.
    public partial class SynthKit : LandKit
    {
        public override string Land => "The Synthwave Coast";
        public override Vector3 ArtistFront(IslandWorldData data, IslandWorldData.Artist a) => facing.TryGetValue(a.name, out var f) ? f : base.ArtistFront(data, a);

        public static readonly Color Magenta = new Color(1f, .2f, .7f), Cyan = new Color(.2f, .9f, 1f), SunGold = new Color(1f, .74f, .26f), Violet = new Color(.36f, .16f, .6f), Chrome = new Color(.8f, .82f, .9f), Ink = new Color(.06f, .04f, .1f), Cream = new Color(.96f, .92f, 1f);
        const float PathW = 3f, RoadW = 9f;

        Material asphalt, ground, chrome, dark, white, palmM, trunkM, windowM, gridM, pinkN, cyanN, sunM, lampM;
        GlowHalos halos; Vector3 center, D, S; SynthLife life;
        readonly Dictionary<(int, int), List<Vector3>> pathCells = new Dictionary<(int, int), List<Vector3>>();
        readonly List<(Vector3 p, int w)> walkPoints = new List<(Vector3, int)>();
        readonly Dictionary<string, Vector3> facing = new Dictionary<string, Vector3>();
        // x, radius, z
        readonly List<Vector3> noGrass = new List<Vector3>();
        Vector2 heart2, D2 = new Vector2(0, -1), S2 = new Vector2(1, 0); float plazaY, waterY, streetL; bool toSea;

        void MakeMaterials()
        {
            asphalt = WorldMaterials.Glow(new Color(.1f, .08f, .16f), .2f); ground = WorldMaterials.Glow(new Color(.14f, .1f, .22f), .2f); chrome = WorldMaterials.Glow(Chrome, .35f); dark = WorldMaterials.Solid(new Color(.04f, .03f, .06f)); white = WorldMaterials.Glow(Cream, .3f);
            palmM = WorldMaterials.Glow(new Color(.1f, .16f, .2f), .2f); trunkM = WorldMaterials.Glow(new Color(.18f, .12f, .2f), .2f);
            windowM = new Material(WorldMaterials.Glow(new Color(1f, .6f, .8f), 1.2f)) { name = "Synthwave windows" }; gridM = new Material(WorldMaterials.Glow(Magenta, 1.3f)) { name = "Synthwave grid" };
            pinkN = new Material(WorldMaterials.Glow(Magenta, 2f)) { name = "Synthwave pink neon" }; cyanN = new Material(WorldMaterials.Glow(Cyan, 2f)) { name = "Synthwave cyan neon" }; sunM = new Material(WorldMaterials.Glow(new Color(1f, .5f, .4f), 1.6f)) { name = "Synthwave sun" }; lampM = new Material(WorldMaterials.Glow(new Color(.8f, .9f, 1f), 1.6f)) { name = "Synthwave lamps" };
        }

        Material GlowM(Color c, float s = 2f) => WorldMaterials.Glow(c, s * .65f);
        Material Paint(Color c) => WorldMaterials.Glow(c, .3f);
        static bool HasStation(IslandWorldData.Artist a) => a.stationId != 0;
        static float Foot(IslandWorldData.Artist a) => a.name == "The Midnight" ? 14f : 8.5f;

        public override bool Ground(float height, float slope, float patch, out Color color)
        {
            // a dark violet night ground, a little lighter on the slopes so the hills still read
            color = slope > 24f ? Color.Lerp(new Color(.22f, .14f, .32f), new Color(.3f, .18f, .4f), patch) : Color.Lerp(new Color(.12f, .1f, .22f), new Color(.2f, .14f, .3f), Mathf.SmoothStep(0f, 1f, patch));
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
            // the Coast Road runs to the open sea, if it's near enough, toward where the sun sits
            toSea = false; var coastL = 9999f;
            for (var k = 0; k < 16; k++) { var dir = new Vector2(Mathf.Sin(k * Mathf.PI / 8f), Mathf.Cos(k * Mathf.PI / 8f)); for (var d = 60f; d < 520f; d += 6f) { var p = heart2 + dir * d; var at = data.IslandAt(p.x, p.y); if (at == isl) continue; if (at == null && d < coastL) { coastL = d; D2 = dir; toSea = true; } break; } }
            if (toSea) streetL = Mathf.Max(60f, coastL - 34f);
            else { streetL = 0f; for (var k = 0; k < 8; k++) { var dir = new Vector2(Mathf.Sin(k * Mathf.PI / 4f), Mathf.Cos(k * Mathf.PI / 4f)); var len = 40f; while (len < 140f && Inside(heart2 + dir * (len + 10f), 50f)) len += 10f; if (len > streetL) { streetL = len; D2 = dir; } } }
            S2 = new Vector2(-D2.y, D2.x); plazaY = Mathf.Max(data.HeightAt(heart2.x, heart2.y), waterY + 6f);
            var pt = P2(streetL + 16f, 0);
            var keep = new List<string>();
            void Put(string name, float d, float s, float room) { var p = P2(d, s); if (!Inside(p, room)) return; foreach (var a in data.layout.artists) if (a.sceneId == isl.sceneId && a.name == name) { a.x = p.x; a.z = p.y; keep.Add(name); } }
            Put("The Midnight", -8f, 52f, 28f);
            var avoid = new List<Vector3>(); for (var d = 0f; d <= streetL; d += 22f) avoid.Add(new Vector3(heart2.x + D2.x * d, 14f, heart2.y + D2.y * d)); avoid.Add(new Vector3(pt.x, 22f, pt.y));
            ArtistPlacement.Spread(data, index, 54f, heart2, 44f, 40f, avoid, keep, Land, 330f);
            Level(data, index, heart2, 22f, 40f, plazaY); Level(data, index, pt, 18f, 32f, Mathf.Max(data.HeightAt(pt.x, pt.y), waterY + 4f));
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
            life = new GameObject("Synthwave life").AddComponent<SynthLife>();
            life.transform.SetParent(ctx.Root, false);
            life.center = center; life.grid = gridM; life.pink = pinkN; life.cyan = cyanN; life.sun = sunM; life.windows = windowM;
            var mk = new MeshKit(); var solid = new MeshKit();
            Remember(ctx.OnGround(center.x, center.z), 0);
            TownStart(ctx);
            NeonDiner(ctx, mk, solid);
            CoastRoad(ctx, mk, solid);
            SunsetPoint(ctx, mk, solid);
            TheSun(ctx);
            Paths(ctx, mk, solid);
            Wilds(ctx, mk, solid);
            mk.Build("The Synthwave Coast", ctx.Root, false);
            solid.BuildCollider("The Synthwave Coast walls", ctx.Root);
        }

        public override void Finish(LandContext ctx) { Verge(ctx); halos.Build("Synthwave glow", ctx.Root); }

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

        /// A street lamp: a slim chrome post with a long cyan tube on top.
        void TubeLamp(LandContext ctx, MeshKit mk, Vector3 g, Vector3 toward, bool light = false)
        {
            var up = Vector3.up; toward.y = 0; toward.Normalize(); mk.Prism(chrome, g - up * .2f, up, .06f, 5f, 6); mk.Prism(chrome, g + up * 4.8f, toward, .05f, 1.4f, 4); var h = g + up * 4.74f + toward * .8f; mk.Box(cyanN, h, new Vector3(.12f, .06f, 1.2f), Quaternion.LookRotation(toward));
            halos.Add(h, 1.6f, Cyan, .4f); ArtistBits.PostCollider(ctx.Root, g, 4.8f, .08f); if (light) ArtistBits.PointLight(ctx.Root, h - up * 1f, new Color(.5f, .9f, 1f), 14f, 2f);
        }

        /// A palm tree with a neon band round its trunk.
        void NeonPalm(LandContext ctx, MeshKit mk, Vector3 g, float s, System.Random rng, int i)
        {
            var up = Vector3.up; var lean = new Vector3((float)rng.NextDouble() - .5f, 0, (float)rng.NextDouble() - .5f) * .3f; var prev = g; var dir = up;
            for (var k = 0; k < 6; k++) { dir = (up + lean * (k / 5f)).normalized; mk.Prism(trunkM, prev, dir, (.2f - k * .02f) * s, 1.3f * s, 6); if (k == 2) mk.Prism(i % 2 == 0 ? pinkN : cyanN, prev + dir * .6f * s, dir, (.18f - k * .02f) * s + .02f, .12f, 6); prev += dir * 1.3f * s; }
            for (var k = 0; k < 8; k++) { var fd = Quaternion.Euler(0, k * 45f + (float)rng.NextDouble() * 20f, 0) * Vector3.forward; var tip = prev + fd * 2.4f * s - up * 1f * s; var mid = prev + fd * 1.2f * s + up * .3f * s; var side = Vector3.Cross(up, fd) * .3f * s; foreach (var o in new[] { up, -up * 3f }) { mk.Tri(palmM, prev, mid + side, tip, prev + o); mk.Tri(palmM, prev, mid - side, tip, prev + o); } }
            ArtistBits.PostCollider(ctx.Root, g, 3f * s, .22f * s); noGrass.Add(new Vector3(g.x, .8f, g.z));
        }

        /// A chrome sports car, low and wedge-shaped, light strips under it.
        void SportsCar(Transform parent, MeshKit mk, Vector3 g, Quaternion rot, Color body, bool seat = true)
        {
            var up = Vector3.up; var m = Paint(body); var f = rot * Vector3.forward; var r = rot * Vector3.right;
            mk.Box(m, g + up * .45f, new Vector3(1.9f, .4f, 4.4f), rot); mk.Box(m, g + up * .7f + f * 1.6f, new Vector3(1.8f, .14f, 1.2f), rot * Quaternion.Euler(8f, 0, 0)); mk.Box(Paint(new Color(.12f, .14f, .22f)), g + up * .82f - f * .2f, new Vector3(1.6f, .4f, 1.8f), rot); mk.Box(chrome, g + up * 1.02f - f * .3f, new Vector3(1.5f, .04f, 1.2f), rot);
            foreach (var (x, zz) in new[] { (-.95f, 1.4f), (.95f, 1.4f), (.95f, -1.4f), (-.95f, -1.4f) }) mk.Prism(dark, g + up * .32f + rot * new Vector3(x - Mathf.Sign(x) * .1f, 0, zz), r * Mathf.Sign(x), .32f, .22f, 12);
            mk.Box(pinkN, g + up * .44f - f * 2.21f, new Vector3(1.6f, .08f, .02f), rot); foreach (var e in new[] { -.6f, .6f }) mk.Box(lampM, g + up * .5f + f * 2.21f + r * e, new Vector3(.4f, .06f, .02f), rot); mk.Box(cyanN, g + up * .12f, new Vector3(1.6f, .02f, 3.8f), rot);
            BoxCol(parent, g + up * .55f, new Vector3(1.9f, 1.1f, 4.4f), rot); if (seat) Interact.Seat(tlife.transform, g + up * .7f - f * .2f - r * .4f, f, "Sit in the car").seatHeight = .4f;
        }

        /// A square of glowing grid laid on the ground.
        void GridPad(LandContext ctx, MeshKit mk, Vector3 c, Quaternion rot, float size, float step)
        {
            var up = Vector3.up; var n = Mathf.RoundToInt(size / step);
            for (var k = 0; k <= n; k++) { var o = -size / 2f + k * step; foreach (var (a, b) in new[] { (rot * new Vector3(o, 0, -size / 2f), rot * new Vector3(o, 0, size / 2f)), (rot * new Vector3(-size / 2f, 0, o), rot * new Vector3(size / 2f, 0, o)) }) { var pts = new List<Vector3>(); for (var t = 0f; t <= 1.001f; t += .25f) { var p = c + Vector3.Lerp(a, b, t); pts.Add(ctx.OnGround(p.x, p.z)); } Strip(mk, pts, gridM, .08f, .05f + (a.x == b.x ? 0f : .005f), ctx); } }
            noGrass.Add(new Vector3(c.x, size * .6f, c.z));
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
                    var dd = Mathf.Min(d, end); var q = from + dir * dd + side * Mathf.Sin(dd / 18f) * Mathf.Min(1.6f, end * .03f);
                    if (!ctx.IsLand(q.x, q.z, 0f)) break;
                    pts.Add(ctx.OnGround(q.x, q.z)); if (dd >= end) break;
                }
                if (pts.Count < 2) return;
                Strip(mk, pts, asphalt, PathW, .12f, ctx); var edge = new List<Vector3>(); foreach (var p in pts) edge.Add(p + side * (PathW / 2f - .1f)); Strip(mk, edge, n % 2 == 0 ? pinkN : cyanN, .08f, .14f, ctx);
                foreach (var p in pts) Remember(p, walk);
                for (var i = 6; i < pts.Count; i += 10, n++)
                {
                    var s = n % 2 == 0 ? 1f : -1f; var lp = pts[i] + side * s * (PathW / 2f + .8f);
                    if (InFootprint(ctx, lp.x, lp.z, 1f) || InNoGrass(lp.x, lp.z, 1f)) continue;
                    TubeLamp(ctx, mk, ctx.OnGround(lp.x, lp.z), -side * s, n % 3 == 0);
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

        // ---------------------------------------------------------------- the night coast
        /// Neon palms, grid pads, parked chrome cars, wireframe pyramids, neon billboards, motels, benches under tube lamps.
        void Wilds(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            int palms = 0, grids = 0, cars = 0, pyramids = 0, boards = 0, benches = 0; var rng = new System.Random(1984);
            const float cell = 22f; string[] words = { "NIGHT DRIVE", "1986", "TURBO", "MIDNIGHT", "VHS", "SUNSET", "ARCADE", "NEON" };
            for (var x = ctx.MinX; x < ctx.MaxX; x += cell)
            for (var z = ctx.MinZ; z < ctx.MaxZ; z += cell)
            {
                float px = x + (float)rng.NextDouble() * cell, pz = z + (float)rng.NextDouble() * cell; var roll = (float)rng.NextDouble(); var s = (float)rng.NextDouble();
                if (!Open(ctx, px, pz, 4f)) continue;
                var p = ctx.OnGround(px, pz); var f = FaceTo(p, NearestWalk(p)); var rot = Quaternion.LookRotation(f); var up = Vector3.up;
                if (roll < .34f) { NeonPalm(ctx, mk, p, .9f + s * .7f, rng, palms); palms++; }
                else if (roll < .46f) { if (!Open(ctx, px, pz, 10f) || Mathf.Abs(ctx.Ground(px + 6f, pz) - ctx.Ground(px - 6f, pz)) > 3f) continue; GridPad(ctx, mk, p, rot, 16f, 4f); grids++; }
                else if (roll < .52f) { SportsCar(ctx.Root, mk, p + up * .02f, Quaternion.LookRotation(Vector3.Cross(up, f)), new[] { Magenta, Cyan, Chrome, new Color(1f, .5f, .2f), Violet }[cars % 5]); noGrass.Add(new Vector3(p.x, 3f, p.z)); cars++; }
                else if (roll < .58f) { var h = 4f + s * 6f; var m = pyramids % 2 == 0 ? pinkN : cyanN; var c0 = p + up * h; for (var k = 0; k < 4; k++) { var a = p + Quaternion.Euler(0, k * 90f + 45f, 0) * Vector3.forward * h * .8f; var b = p + Quaternion.Euler(0, k * 90f + 135f, 0) * Vector3.forward * h * .8f; mk.Prism(m, a, (b - a).normalized, .05f, (b - a).magnitude, 4); mk.Prism(m, a, (c0 - a).normalized, .05f, (c0 - a).magnitude, 4); } halos.Add(c0, 1.4f, pyramids % 2 == 0 ? Magenta : Cyan, .3f); noGrass.Add(new Vector3(p.x, h * .8f, p.z)); pyramids++; }
                else if (roll < .63f) { var side = Vector3.Cross(up, f); foreach (var e in new[] { -1.8f, 1.8f }) { mk.Prism(chrome, p + side * e - up * .3f, up, .1f, 4.6f, 5); ArtistBits.PostCollider(ctx.Root, p + side * e, 4.4f, .12f); } mk.Box(dark, p + up * 5.4f, new Vector3(5.4f, 2.4f, .2f), rot); mk.Box(boards % 2 == 0 ? pinkN : cyanN, p + up * 5.4f + f * .11f, new Vector3(5.2f, .06f, .02f), rot); mk.Box(boards % 2 == 0 ? pinkN : cyanN, p + up * 6.5f + f * .11f, new Vector3(5.2f, .06f, .02f), rot); Sign(p + up * 5.7f + f * .12f, f, words[boards % words.Length], .6f, boards % 2 == 0 ? Magenta : Cyan); noGrass.Add(new Vector3(p.x, 3f, p.z)); boards++; }
                else if (roll < .7f) { var side = Vector3.Cross(up, f); for (var k = 0; k < 3; k++) mk.Box(chrome, p + up * .46f + f * (-.16f + k * .16f), new Vector3(2f, .05f, .13f), rot); mk.Box(chrome, p + up * .8f - f * .26f, new Vector3(2f, .3f, .05f), rot); BoxCol(ctx.Root, p + up * .45f, new Vector3(2f, .9f, .5f), rot); Interact.Bench(tlife.transform, p + up * .52f - side * .5f, p + up * .52f + side * .5f, f); TubeLamp(ctx, mk, p - f * 1f + side * 1.4f, f, benches % 2 == 0); noGrass.Add(new Vector3(p.x, 2f, p.z)); benches++; }
            }
            Debug.Log($"The Synthwave Coast: Coast Road {streetL:0} m{(toSea ? " to the sea" : "")}, {palms} neon palms, {grids} grid pads, {cars} chrome cars, {pyramids} wireframe pyramids, {boards} billboards, {benches} benches");
        }

        Vector3 NearestWalk(Vector3 p) { var best = center; var bd = float.MaxValue; foreach (var (q, _) in walkPoints) { var d = (q - p).sqrMagnitude; if (d < bd) { bd = d; best = q; } } return best; }

        /// Short dark grass with a few glowing tips.
        void Verge(LandContext ctx)
        {
            var mat = WindMesh.Material(.3f, .8f, new Color(.2f, .14f, .3f));
            var meshes = new Dictionary<(int, int), WindMesh>(); var rng = new System.Random(1985);
            const float st = 2.8f;
            for (var x = ctx.MinX; x < ctx.MaxX; x += st)
            for (var z = ctx.MinZ; z < ctx.MaxZ; z += st)
            {
                var px = x + ((float)rng.NextDouble() - .5f) * st; var pz = z + ((float)rng.NextDouble() - .5f) * st;
                if (!ctx.IsLand(px, pz, 1f) || NearPath(px, pz, 3.6f) || InNoGrass(px, pz, 0f) || InFootprint(ctx, px, pz, -2f)) continue;
                if (Mathf.PerlinNoise(px * .04f + 8f, pz * .04f + 4f) < .46f) continue;
                var key = (Mathf.FloorToInt(px / 70f), Mathf.FloorToInt(pz / 70f));
                if (!meshes.TryGetValue(key, out var wm)) meshes[key] = wm = new WindMesh();
                var col = Color.Lerp(new Color(.16f, .12f, .26f), new Color(.26f, .16f, .36f), (float)rng.NextDouble());
                wm.Tuft(new Vector3(px, ctx.Ground(px, pz) - .04f, pz), .26f + (float)rng.NextDouble() * .3f, col, rng.NextDouble() < .12 ? (rng.NextDouble() < .5 ? Magenta : Cyan) : Color.Lerp(col, Violet, .5f), rng, 3, .07f);
            }
            foreach (var kv in meshes) kv.Value.Build("Synthwave grass", ctx.Root, mat);
        }
    }
}
