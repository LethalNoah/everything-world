using System.Collections.Generic;
using UnityEngine;
using static EverythingWorld.World.PlaceTools;

namespace EverythingWorld.World
{
    /// The Country Plains (WORLD_GUIDE.md): a prairie town at golden hour going into dusk. A low gold sun, long
    /// shadows, wheat to the horizon behind split-rail fences, red barns, a windmill turning, a water tower over the
    /// town. You arrive on the Square under the water tower. Main Street runs out of it past the honky-tonk, the
    /// general store and the diner to the Bonfire Field, where the pickups are parked in a ring with their tailgates
    /// down. Easy, warm, a little bit wistful.
    /// Laid out on purpose (the build-land standard): CountryTown.cs has the places and the people.
    public partial class CountryKit : LandKit
    {
        public override string Land => "The Country Plains";
        public override Vector3 ArtistFront(IslandWorldData data, IslandWorldData.Artist a) => facing.TryGetValue(a.name, out var f) ? f : base.ArtistFront(data, a);

        public static readonly Color Wheat = new Color(.92f, .76f, .4f), BarnRed = new Color(.62f, .2f, .16f), Denim = new Color(.28f, .38f, .56f), Cream = new Color(.96f, .9f, .78f), Ink = new Color(.12f, .1f, .08f), Rust = new Color(.6f, .34f, .2f), Sky = new Color(.5f, .66f, .86f), Fire = new Color(1f, .56f, .2f);
        static readonly Color[] Paints = { new Color(.62f, .2f, .16f), new Color(.92f, .88f, .8f), new Color(.5f, .6f, .48f), new Color(.86f, .72f, .46f), new Color(.46f, .56f, .66f), new Color(.7f, .5f, .36f) };
        const float PathW = 2.8f, StreetW = 8f;

        Material dirt, gravel, woodM, plank, iron, dark, white, leafM, leafGold, trunkM, windowM, lampM, fireM, tin, hay;
        GlowHalos halos; Vector3 center, D, S; CountryLife life;
        readonly Dictionary<(int, int), List<Vector3>> pathCells = new Dictionary<(int, int), List<Vector3>>();
        readonly List<(Vector3 p, int w)> walkPoints = new List<(Vector3, int)>();
        readonly Dictionary<string, Vector3> facing = new Dictionary<string, Vector3>();
        // x, radius, z
        readonly List<Vector3> noGrass = new List<Vector3>();
        readonly List<(Vector3 c, Quaternion r, float w, float d)> fields = new List<(Vector3, Quaternion, float, float)>();
        Vector2 heart2, D2 = new Vector2(0, -1), S2 = new Vector2(1, 0); float plazaY, waterY, streetL;

        void MakeMaterials()
        {
            dirt = WorldMaterials.Solid(new Color(.62f, .48f, .34f)); gravel = WorldMaterials.Solid(new Color(.56f, .52f, .46f)); woodM = WorldMaterials.Solid(new Color(.5f, .36f, .24f)); plank = WorldMaterials.Solid(new Color(.64f, .5f, .36f)); iron = WorldMaterials.Solid(new Color(.2f, .2f, .22f), .5f); dark = WorldMaterials.Solid(new Color(.08f, .07f, .06f)); white = WorldMaterials.Solid(new Color(.94f, .9f, .82f)); tin = WorldMaterials.Solid(new Color(.6f, .62f, .64f), .6f); hay = WorldMaterials.Solid(new Color(.86f, .72f, .4f));
            leafM = WorldMaterials.Solid(new Color(.36f, .5f, .26f)); leafGold = WorldMaterials.Solid(new Color(.7f, .6f, .26f)); trunkM = WorldMaterials.Solid(new Color(.36f, .28f, .22f));
            windowM = new Material(WorldMaterials.Glow(new Color(1f, .78f, .46f), 1.1f)) { name = "Country windows" }; lampM = new Material(WorldMaterials.Glow(new Color(1f, .82f, .52f), 1.4f)) { name = "Country lamps" }; fireM = new Material(WorldMaterials.Glow(Fire, 2.2f)) { name = "Country fire" };
        }

        Material GlowM(Color c, float s = 2f) => WorldMaterials.Glow(c, s * .65f);
        Material Paint(Color c) => WorldMaterials.Solid(c);
        static bool HasStation(IslandWorldData.Artist a) => a.stationId != 0;
        static float Foot(IslandWorldData.Artist a)
        {
            switch (a.name)
            {
                case "Zac Brown Band": case "Tyler Childers": return 13f;
                case "Jason Aldean": case "Lady A": return 11f;
                default: return 8.5f;
            }
        }

        public override bool Ground(float height, float slope, float patch, out Color color)
        {
            // prairie: gold grass and green, dusty where it's steep
            if (slope > 24f) color = Color.Lerp(new Color(.6f, .46f, .34f), new Color(.68f, .54f, .4f), patch);
            else color = Color.Lerp(new Color(.56f, .6f, .32f), new Color(.8f, .68f, .38f), Mathf.SmoothStep(0f, 1f, patch));
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
            for (var k = 0; k < 8; k++) { var dir = new Vector2(Mathf.Sin(k * Mathf.PI / 4f), Mathf.Cos(k * Mathf.PI / 4f)); var len = 40f; while (len < 130f && Inside(heart2 + dir * (len + 10f), 50f)) len += 10f; if (len > streetL) { streetL = len; D2 = dir; } }
            S2 = new Vector2(-D2.y, D2.x); plazaY = Mathf.Max(data.HeightAt(heart2.x, heart2.y), waterY + 6f);
            var bon = P2(streetL + 20f, 0);
            var keep = new List<string>();
            void Put(string name, float d, float s, float room) { var p = P2(d, s); if (!Inside(p, room)) return; foreach (var a in data.layout.artists) if (a.sceneId == isl.sceneId && a.name == name) { a.x = p.x; a.z = p.y; keep.Add(name); } }
            Put("Zac Brown Band", -6f, 52f, 28f); Put("Tyler Childers", 0f, -52f, 28f); Put("Jason Aldean", 70f, 46f, 24f); Put("Lady A", 72f, -46f, 24f);
            var avoid = new List<Vector3>(); for (var d = 0f; d <= streetL; d += 22f) avoid.Add(new Vector3(heart2.x + D2.x * d, 14f, heart2.y + D2.y * d)); avoid.Add(new Vector3(bon.x, 28f, bon.y));
            ArtistPlacement.Spread(data, index, 52f, heart2, 44f, 40f, avoid, keep, Land, 330f);
            Level(data, index, heart2, 22f, 40f, plazaY); Level(data, index, bon, 22f, 38f, Mathf.Max(data.HeightAt(bon.x, bon.y), waterY + 4f));
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
            pathCells.Clear(); walkPoints.Clear(); facing.Clear(); noGrass.Clear(); fields.Clear();
            center = ctx.Plaza; D = new Vector3(D2.x, 0, D2.y); S = new Vector3(S2.x, 0, S2.y);
            life = new GameObject("Country life").AddComponent<CountryLife>();
            life.transform.SetParent(ctx.Root, false);
            life.center = center; life.windows = windowM; life.lamps = lampM; life.fire = fireM;
            var mk = new MeshKit(); var solid = new MeshKit();
            Remember(ctx.OnGround(center.x, center.z), 0);
            TownStart(ctx);
            TheSquare(ctx, mk, solid);
            MainStreet(ctx, mk, solid);
            BonfireField(ctx, mk, solid);
            Paths(ctx, mk, solid);
            Storefronts(ctx, mk, solid);
            Wilds(ctx, mk, solid);
            mk.Build("The Country Plains", ctx.Root, false);
            solid.BuildCollider("The Country Plains walls", ctx.Root);
        }

        public override void Finish(LandContext ctx) { Verge(ctx); halos.Build("Country glow", ctx.Root); }

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

        /// A wooden lamp post: a post, a crossarm, a hanging lantern.
        void PostLamp(LandContext ctx, MeshKit mk, Vector3 g, Vector3 toward, bool light = false)
        {
            var up = Vector3.up; toward.y = 0; toward.Normalize(); mk.Prism(woodM, g - up * .2f, up, .09f, 4f, 5); mk.Box(woodM, g + up * 3.7f + toward * .4f, new Vector3(.08f, .08f, .9f), Quaternion.LookRotation(toward)); var h = g + up * 3.4f + toward * .75f; mk.Prism(iron, h + up * .3f, -up, .01f, .2f, 3); mk.Box(lampM, h, new Vector3(.2f, .3f, .2f), Quaternion.identity); mk.Cone(iron, h + up * .17f, .16f, .14f, 4, 45f);
            halos.Add(h, 1.4f, new Color(1f, .82f, .52f), .45f); ArtistBits.PostCollider(ctx.Root, g, 3.8f, .1f); if (light) ArtistBits.PointLight(ctx.Root, h - up * .5f, new Color(1f, .8f, .5f), 13f, 1.8f);
        }

        /// A split-rail fence from a to b.
        void Fence(LandContext ctx, MeshKit mk, Vector3 a, Vector3 b)
        {
            var dir = b - a; dir.y = 0; var len = dir.magnitude; if (len < 1f) return; dir /= len; var n = Mathf.Max(1, Mathf.RoundToInt(len / 2.6f));
            for (var k = 0; k <= n; k++) { var p = a + dir * (len * k / n); p = ctx.OnGround(p.x, p.z); mk.Prism(woodM, p - Vector3.up * .2f, Vector3.up, .06f, 1.3f, 4); if (k < n) { var q = a + dir * (len * (k + 1) / n); q = ctx.OnGround(q.x, q.z); foreach (var h in new[] { .5f, 1f }) mk.Prism(plank, p + Vector3.up * h, (q - p).normalized, .04f, Vector3.Distance(p, q), 4); } }
            var mid = ctx.OnGround((a.x + b.x) / 2f, (a.z + b.z) / 2f); BoxCol(ctx.Root, mid + Vector3.up * .6f, new Vector3(.2f, 1.2f, len), Quaternion.LookRotation(dir));
        }

        /// A hay bale: a big round roll.
        void Bale(LandContext ctx, MeshKit mk, Vector3 g, Vector3 axis)
        {
            axis.y = 0; axis.Normalize(); mk.Prism(hay, g + Vector3.up * .7f - axis * .7f, axis, .7f, 1.4f, 12); for (var k = 0; k < 3; k++) mk.Prism(Paint(new Color(.74f, .6f, .32f)), g + Vector3.up * .7f - axis * (.71f - k * .7f), axis, .5f - k * .1f, .01f, 10);
            BoxCol(ctx.Root, g + Vector3.up * .7f, new Vector3(1.4f, 1.4f, 1.4f), Quaternion.LookRotation(axis)); Interact.Seat(tlife.transform, g + Vector3.up * 1.45f, Vector3.Cross(Vector3.up, axis), "Sit on the hay bale").seatHeight = .1f;
        }

        /// A pickup truck, its tailgate down to sit on.
        void Pickup(Transform parent, MeshKit mk, Vector3 g, Quaternion rot, Color body, bool tailgate = true)
        {
            var up = Vector3.up; var m = Paint(body); var f = rot * Vector3.forward; var r = rot * Vector3.right;
            mk.Box(m, g + up * .8f + f * 1.2f, new Vector3(1.9f, .7f, 2f), rot); mk.Box(m, g + up * 1.4f + f * .9f, new Vector3(1.8f, .7f, 1.4f), rot); mk.Box(Paint(new Color(.56f, .66f, .74f)), g + up * 1.44f + f * 1.61f, new Vector3(1.6f, .5f, .02f), rot * Quaternion.Euler(-8f, 0, 0));
            mk.Box(m, g + up * .6f - f * 1.1f, new Vector3(1.9f, .3f, 2.6f), rot); foreach (var e in new[] { -.92f, .92f }) mk.Box(m, g + up * .95f - f * 1.1f + r * e, new Vector3(.08f, .5f, 2.6f), rot); mk.Box(m, g + up * .95f + f * .2f, new Vector3(1.9f, .5f, .08f), rot);
            if (tailgate) { mk.Box(m, g + up * .62f - f * 2.6f, new Vector3(1.9f, .06f, .5f), rot); Interact.Bench(tlife.transform, g + up * .78f - f * 2.5f - r * .5f, g + up * .78f - f * 2.5f + r * .5f, -f); } else mk.Box(m, g + up * .95f - f * 2.4f, new Vector3(1.9f, .5f, .08f), rot);
            foreach (var (x, zz) in new[] { (-.95f, 1.5f), (.95f, 1.5f), (.95f, -1.6f), (-.95f, -1.6f) }) mk.Prism(dark, g + up * .42f + rot * new Vector3(x - Mathf.Sign(x) * .12f, 0, zz), r * Mathf.Sign(x), .42f, .26f, 12);
            foreach (var e in new[] { -.7f, .7f }) mk.Blob(lampM, g + up * .9f + f * 2.21f + r * e, new Vector3(.14f, .1f, .03f), new System.Random(2), 0f);
            BoxCol(parent, g + up * .9f, new Vector3(1.9f, 1.8f, 4.6f), rot);
        }

        /// A rocking chair to sit in.
        void Rocker(MeshKit mk, Vector3 g, Vector3 face)
        {
            var up = Vector3.up; var rot = Quaternion.LookRotation(face); var side = Vector3.Cross(up, face); mk.Box(woodM, g + up * .45f, new Vector3(.5f, .05f, .5f), rot); mk.Box(woodM, g + up * .9f - face * .24f, new Vector3(.5f, .8f, .05f), rot * Quaternion.Euler(-10f, 0, 0)); foreach (var e in new[] { -.24f, .24f }) { mk.Box(woodM, g + up * .1f + side * e, new Vector3(.04f, .06f, .8f), rot); mk.Prism(woodM, g + up * .1f + side * e, up, .02f, .36f, 4); }
            Interact.Seat(tlife.transform, g + up * .5f, face, "Sit in the rocking chair");
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
                    var dd = Mathf.Min(d, end); var q = from + dir * dd + side * Mathf.Sin(dd / 18f) * Mathf.Min(2.4f, end * .05f);
                    if (!ctx.IsLand(q.x, q.z, 0f)) break;
                    pts.Add(ctx.OnGround(q.x, q.z)); if (dd >= end) break;
                }
                if (pts.Count < 2) return;
                Strip(mk, pts, dirt, PathW, .12f, ctx);
                foreach (var p in pts) Remember(p, walk);
                for (var i = 6; i < pts.Count; i += 10, n++)
                {
                    var s = n % 2 == 0 ? 1f : -1f; var lp = pts[i] + side * s * (PathW / 2f + .8f);
                    if (InFootprint(ctx, lp.x, lp.z, 1f) || InNoGrass(lp.x, lp.z, 1f)) continue;
                    PostLamp(ctx, mk, ctx.OnGround(lp.x, lp.z), -side * s, n % 3 == 0);
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

        // ---------------------------------------------------------------- the prairie
        /// Wheat fields behind split-rail fences, red barns, windmills, hay bales, cottonwoods, old tractors, mailboxes.
        void Wilds(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            int fieldsN = 0, barns = 0, mills = 0, bales = 0, trees = 0, tractors = 0; var rng = new System.Random(1990);
            const float cell = 24f;
            for (var x = ctx.MinX; x < ctx.MaxX; x += cell)
            for (var z = ctx.MinZ; z < ctx.MaxZ; z += cell)
            {
                float px = x + (float)rng.NextDouble() * cell, pz = z + (float)rng.NextDouble() * cell; var roll = (float)rng.NextDouble(); var s = (float)rng.NextDouble();
                if (!Open(ctx, px, pz, 4f)) continue;
                var p = ctx.OnGround(px, pz); var f = FaceTo(p, NearestWalk(p)); var rot = Quaternion.LookRotation(f); var side = Vector3.Cross(Vector3.up, f);
                if (roll < .26f) { if (!Open(ctx, px, pz, 10f) || !ctx.IsLand(px, pz, 12f)) continue; const float w = 18f, dp = 14f; var c0 = p - side * w / 2f - f * dp / 2f; var c1 = p + side * w / 2f - f * dp / 2f; var c2 = p + side * w / 2f + f * dp / 2f; var c3 = p - side * w / 2f + f * dp / 2f; Fence(ctx, mk, c0, c1); Fence(ctx, mk, c1, c2); Fence(ctx, mk, c2, c3 + side * 3f); Fence(ctx, mk, c3, c0); fields.Add((p, rot, w - 1f, dp - 1f)); noGrass.Add(new Vector3(p.x, 11f, p.z)); fieldsN++; }
                else if (roll < .34f) { if (!Open(ctx, px, pz, 8f) || Mathf.Abs(ctx.Ground(px + 4f, pz) - ctx.Ground(px - 4f, pz)) > 2f) continue; Barn(ctx, mk, solid, new PlaceFrame(p, f), barns); barns++; }
                else if (roll < .39f) { Windmill(ctx, mk, p, mills); mills++; }
                else if (roll < .5f) { for (var k = 0; k < 1 + rng.Next(3); k++) Bale(ctx, mk, p + side * (k * 1.8f) + f * ((k % 2) * 1.2f), side); noGrass.Add(new Vector3(p.x, 3f, p.z)); bales++; }
                else if (roll < .72f) { Cottonwood(ctx, mk, p, .9f + s * .6f, rng); trees++; }
                else if (roll < .76f) { Tractor(ctx, mk, p, rot, tractors % 2 == 0 ? new Color(.3f, .46f, .24f) : BarnRed); tractors++; }
            }
            Debug.Log($"The Country Plains: Main Street {streetL:0} m, {fieldsN} wheat fields, {barns} barns, {mills} windmills, {bales} hay bales, {trees} cottonwoods, {tractors} tractors");
        }

        /// A red barn with a white-trimmed door and a hayloft.
        void Barn(LandContext ctx, MeshKit mk, MeshKit solid, PlaceFrame f, int i)
        {
            var up = Vector3.up; float W = 8f, Dp = 10f, H = 4.4f; var wall = Paint(i % 3 == 2 ? new Color(.66f, .6f, .52f) : BarnRed);
            mk.Box(wall, f.P(0, H / 2f - .3f, 0), new Vector3(W, H + .6f, Dp), f.r); solid.Box(wall, f.P(0, H / 2f - .3f, 0), new Vector3(W, H + .6f, Dp), f.r); mk.GableRoof(tin, f.P(0, H, 0), new Vector3(W + .6f, 0, Dp + .6f), 3f, f.r);
            mk.Box(wall, f.P(0, H + 1f, Dp / 2f - .02f), new Vector3(1f, .1f, .1f), f.r); mk.Box(white, f.P(0, 1.6f, Dp / 2f + .03f), new Vector3(3.2f, 3.2f, .06f), f.r); mk.Box(wall, f.P(0, 1.6f, Dp / 2f + .07f), new Vector3(2.9f, 2.9f, .04f), f.r); foreach (var e in new[] { 1f, -1f }) mk.Box(white, f.P(0, 1.6f, Dp / 2f + .1f), new Vector3(.14f, 4.1f, .03f), f.r * Quaternion.Euler(0, 0, e * 45f)); mk.Box(i % 2 == 0 ? windowM : dark, f.P(0, H + .4f, Dp / 2f + .03f), new Vector3(1.2f, 1f, .05f), f.r);
            if (i % 2 == 0) halos.Add(f.P(0, H + .4f, Dp / 2f + .5f), 1.2f, new Color(1f, .78f, .46f), .3f); Mark(f.o, 7f);
        }

        /// A water-pumping windmill on a lattice tower; its wheel turns.
        void Windmill(LandContext ctx, MeshKit mk, Vector3 g, int i)
        {
            var up = Vector3.up; const float H = 9f; foreach (var (x, zz) in new[] { (-1f, -1f), (1f, -1f), (1f, 1f), (-1f, 1f) }) { var foot = g + new Vector3(x, 0, zz) * 1.2f; mk.Prism(tin, foot, (g + up * H - foot).normalized, .05f, (g + up * H - foot).magnitude, 4); }
            for (var h = 1.5f; h < H; h += 2f) { var w = 1.2f * (1f - h / H); mk.Box(tin, g + up * h, new Vector3(w * 2f, .04f, .04f), Quaternion.Euler(0, 0, 0)); mk.Box(tin, g + up * h, new Vector3(.04f, .04f, w * 2f), Quaternion.identity); }
            ArtistBits.PostCollider(ctx.Root, g, 3f, 1.2f); var wk = new MeshKit(); for (var k = 0; k < 16; k++) { var a = Quaternion.Euler(0, 0, k * 22.5f); wk.Box(tin, a * Vector3.up * .9f, new Vector3(.3f, 1.4f, .02f), a * Quaternion.Euler(0, 20f, 0)); } wk.Prism(tin, new Vector3(0, 0, -.1f), Vector3.forward, .2f, .3f, 8);
            var wheel = wk.BuildSingle("Windmill wheel", ctx.Root, false); wheel.transform.SetPositionAndRotation(g + up * (H + .3f), Quaternion.LookRotation(D)); life.wheels.Add(wheel.transform); mk.Box(tin, g + up * (H + .3f) - D * 1.6f, new Vector3(.04f, .8f, 1.6f), Quaternion.LookRotation(D));
            Mark(g, 2.4f);
        }

        /// A cottonwood: a broad trunk and a big round crown going gold at the edges.
        void Cottonwood(LandContext ctx, MeshKit mk, Vector3 g, float s, System.Random rng)
        {
            var up = Vector3.up; mk.Prism(trunkM, g - up * .2f, up, .3f * s, 3f * s, 7, .6f); ArtistBits.PostCollider(ctx.Root, g, 2.6f * s, .36f * s);
            for (var k = 0; k < 5; k++) mk.Blob(k % 2 == 0 ? leafM : leafGold, g + up * (3.8f + (float)rng.NextDouble() * 1.2f) * s + Quaternion.Euler(0, k * 72f, 0) * Vector3.forward * (k == 0 ? 0f : 1.6f * s), new Vector3(2f, 1.4f, 2f) * s, rng, .2f);
            noGrass.Add(new Vector3(g.x, 1.4f * s, g.z));
        }

        /// An old tractor, rusting where it was left.
        void Tractor(LandContext ctx, MeshKit mk, Vector3 g, Quaternion rot, Color body)
        {
            var up = Vector3.up; var m = Paint(body); var f = rot * Vector3.forward; var r = rot * Vector3.right; mk.Box(m, g + up * 1f + f * .6f, new Vector3(.9f, .8f, 2f), rot); mk.Box(m, g + up * 1.5f - f * .6f, new Vector3(1.2f, .3f, 1f), rot); mk.Prism(dark, g + up * 1.4f + f * 1.2f, up, .06f, 1f, 5);
            foreach (var e in new[] { -1f, 1f }) { mk.Prism(dark, g + up * .8f - f * .6f + r * e * .7f - r * e * .2f, r * e, .8f, .4f, 14); mk.Prism(dark, g + up * .4f + f * 1.3f + r * e * .5f - r * e * .1f, r * e, .4f, .2f, 10); }
            Interact.Seat(tlife.transform, g + up * 1.7f - f * .6f, f, "Sit on the tractor").seatHeight = .3f; BoxCol(ctx.Root, g + up * 1f, new Vector3(1.8f, 2f, 3f), rot); Mark(g, 2.6f);
        }

        Vector3 NearestWalk(Vector3 p) { var best = center; var bd = float.MaxValue; foreach (var (q, _) in walkPoints) { var d = (q - p).sqrMagnitude; if (d < bd) { bd = d; best = q; } } return best; }

        /// Prairie grass everywhere, and wheat standing tall and gold in the fenced fields.
        void Verge(LandContext ctx)
        {
            var mat = WindMesh.Material(.5f, .9f, new Color(.74f, .66f, .36f));
            var meshes = new Dictionary<(int, int), WindMesh>(); var rng = new System.Random(1991);
            WindMesh Mesh(float px, float pz) { var key = (Mathf.FloorToInt(px / 70f), Mathf.FloorToInt(pz / 70f)); if (!meshes.TryGetValue(key, out var wm)) meshes[key] = wm = new WindMesh(); return wm; }
            foreach (var (c, r, w, dp) in fields)
                for (var x = -w / 2f; x < w / 2f; x += .9f)
                for (var zz = -dp / 2f; zz < dp / 2f; zz += .7f)
                {
                    var p = c + r * new Vector3(x + ((float)rng.NextDouble() - .5f) * .3f, 0, zz); if (!ctx.IsLand(p.x, p.z, 0f)) continue;
                    var col = Color.Lerp(new Color(.82f, .66f, .32f), new Color(.96f, .82f, .46f), (float)rng.NextDouble());
                    Mesh(p.x, p.z).Tuft(new Vector3(p.x, ctx.Ground(p.x, p.z) - .04f, p.z), .9f + (float)rng.NextDouble() * .3f, col, new Color(1f, .9f, .6f), rng, 4, .05f);
                }
            const float st = 2.6f;
            for (var x = ctx.MinX; x < ctx.MaxX; x += st)
            for (var z = ctx.MinZ; z < ctx.MaxZ; z += st)
            {
                var px = x + ((float)rng.NextDouble() - .5f) * st; var pz = z + ((float)rng.NextDouble() - .5f) * st;
                if (!ctx.IsLand(px, pz, 1f) || NearPath(px, pz, 3.6f) || InNoGrass(px, pz, 0f) || InFootprint(ctx, px, pz, -2f)) continue;
                if (Mathf.PerlinNoise(px * .04f + 8f, pz * .04f + 4f) < .38f) continue;
                var col = Color.Lerp(new Color(.62f, .6f, .32f), new Color(.82f, .7f, .4f), (float)rng.NextDouble());
                Mesh(px, pz).Tuft(new Vector3(px, ctx.Ground(px, pz) - .04f, pz), .3f + (float)rng.NextDouble() * .4f, col, rng.NextDouble() < .06 ? new Color(.6f, .5f, .9f) : Color.Lerp(col, Cream, .4f), rng, 3, .07f);
            }
            foreach (var kv in meshes) kv.Value.Build("Prairie grass and wheat", ctx.Root, mat);
        }
    }
}
