using System.Collections.Generic;
using UnityEngine;
using static EverythingWorld.World.PlaceTools;

namespace EverythingWorld.World
{
    /// The Heartbreak Boardwalk (WORLD_GUIDE.md): a faded seaside resort at dusk. A pink-orange sunset low over the
    /// sea, the sky going violet above it; weathered boards, shuttered stalls, globe lamps coming on one by one, a
    /// motel with half its sign lit, a wooden rollercoaster standing still. You arrive on the Promenade, by the old
    /// bandstand. The Boardwalk runs down to the sea and out along the Pier. Sad and glamorous.
    /// Laid out on purpose (the build-land standard): HeartTown.cs has the places and the people.
    public partial class HeartKit : LandKit
    {
        public override string Land => "The Heartbreak Boardwalk";
        public override Vector3 ArtistFront(IslandWorldData data, IslandWorldData.Artist a) => facing.TryGetValue(a.name, out var f) ? f : base.ArtistFront(data, a);

        public static readonly Color Rose = new Color(.94f, .56f, .62f), Peach = new Color(1f, .7f, .52f), Violet = new Color(.42f, .3f, .58f), SeaBlue = new Color(.34f, .6f, .74f), Cream = new Color(.96f, .9f, .82f), Ink = new Color(.12f, .1f, .16f), Neon = new Color(1f, .3f, .56f), Mint = new Color(.56f, .86f, .76f), White = new Color(.96f, .95f, .94f);
        static readonly Color[] Faded = { new Color(.86f, .7f, .7f), new Color(.66f, .78f, .82f), new Color(.92f, .84f, .66f), new Color(.72f, .7f, .84f), new Color(.7f, .82f, .72f), new Color(.9f, .76f, .62f) };
        const float PathW = 2.8f, WalkW = 7f;

        Material boards, boardsDark, sand, woodM, iron, dark, white, leafM, leafDark, trunkM, windowM, lampM, neonM, seaM, stripeA, stripeB;
        GlowHalos halos; Vector3 center, D, S; HeartLife life;
        readonly Dictionary<(int, int), List<Vector3>> pathCells = new Dictionary<(int, int), List<Vector3>>();
        readonly List<(Vector3 p, int w)> walkPoints = new List<(Vector3, int)>();
        readonly Dictionary<string, Vector3> facing = new Dictionary<string, Vector3>();
        // x, radius, z
        readonly List<Vector3> noGrass = new List<Vector3>();
        Vector2 heart2, D2 = new Vector2(0, -1), S2 = new Vector2(1, 0); float plazaY, waterY, streetL, coastL; bool toSea;

        void MakeMaterials()
        {
            // a dusk land: everything faintly self-lit so it never goes muddy
            boards = WorldMaterials.Glow(new Color(.66f, .54f, .46f), .16f); boardsDark = WorldMaterials.Glow(new Color(.46f, .38f, .36f), .14f); sand = WorldMaterials.Glow(new Color(.86f, .74f, .64f), .16f); woodM = WorldMaterials.Glow(new Color(.5f, .4f, .34f), .12f);
            iron = WorldMaterials.Solid(new Color(.16f, .14f, .18f), .5f); dark = WorldMaterials.Solid(new Color(.08f, .07f, .1f)); white = WorldMaterials.Glow(new Color(.94f, .9f, .86f), .16f);
            leafM = WorldMaterials.Glow(new Color(.34f, .5f, .42f), .14f); leafDark = WorldMaterials.Glow(new Color(.24f, .38f, .36f), .12f); trunkM = WorldMaterials.Glow(new Color(.42f, .32f, .28f), .1f);
            stripeA = WorldMaterials.Glow(Rose, .2f); stripeB = WorldMaterials.Glow(Cream, .2f);
            windowM = new Material(WorldMaterials.Glow(new Color(1f, .76f, .5f), 1.1f)) { name = "Heartbreak windows" }; lampM = new Material(WorldMaterials.Glow(new Color(1f, .86f, .64f), 1.5f)) { name = "Heartbreak lamps" };
            neonM = new Material(WorldMaterials.Glow(Neon, 2f)) { name = "Heartbreak neon" }; seaM = new Material(WorldMaterials.Glow(SeaBlue, .5f)) { name = "Heartbreak sea glow" };
        }

        Material GlowM(Color c, float s = 2f) => WorldMaterials.Glow(c, s * .65f);
        Material Paint(Color c) => WorldMaterials.Glow(c, .16f);
        static bool HasStation(IslandWorldData.Artist a) => a.stationId != 0;
        static float Foot(IslandWorldData.Artist a)
        {
            switch (a.name)
            {
                case "Lana Del Rey": return 16f;
                case "Olivia Rodrigo": case "Billie Eilish": case "Ashe": return 12f;
                default: return 8.5f;
            }
        }

        public override bool Ground(float height, float slope, float patch, out Color color)
        {
            // dune grass and pale sand in the dusk; low ground near the sea goes to sand
            if (height < 4f) color = Color.Lerp(new Color(.82f, .7f, .6f), new Color(.88f, .76f, .66f), patch);
            else if (slope > 26f) color = Color.Lerp(new Color(.6f, .5f, .5f), new Color(.7f, .58f, .56f), patch);
            else color = Color.Lerp(new Color(.48f, .58f, .48f), new Color(.66f, .66f, .52f), Mathf.SmoothStep(0f, 1f, patch));
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
            // the Boardwalk runs toward the nearest open sea, if there's one close enough, and becomes the Pier
            toSea = false; coastL = 9999f;
            for (var k = 0; k < 16; k++)
            {
                var dir = new Vector2(Mathf.Sin(k * Mathf.PI / 8f), Mathf.Cos(k * Mathf.PI / 8f));
                for (var d = 30f; d < 700f; d += 6f) { var p = heart2 + dir * d; var at = data.IslandAt(p.x, p.y); if (at == isl) continue; if (at == null && d < coastL) { coastL = d; D2 = dir; toSea = true; } break; }
            }
            if (toSea) streetL = Mathf.Max(40f, coastL - 8f);
            else { streetL = 0f; for (var k = 0; k < 8; k++) { var dir = new Vector2(Mathf.Sin(k * Mathf.PI / 4f), Mathf.Cos(k * Mathf.PI / 4f)); var len = 40f; while (len < 140f && Inside(heart2 + dir * (len + 10f), 40f)) len += 10f; if (len > streetL) { streetL = len; D2 = dir; } } }
            S2 = new Vector2(-D2.y, D2.x); plazaY = Mathf.Max(data.HeightAt(heart2.x, heart2.y), waterY + 5f);

            var keep = new List<string>();
            void Put(string name, float d, float s, float room) { var p = P2(d, s); if (!Inside(p, room)) return; foreach (var a in data.layout.artists) if (a.sceneId == isl.sceneId && a.name == name) { a.x = p.x; a.z = p.y; keep.Add(name); } }
            Put("Lana Del Rey", Mathf.Min(70f, streetL * .6f), 44f, 30f); Put("Olivia Rodrigo", -10f, -48f, 26f); Put("Billie Eilish", -50f, 20f, 26f); Put("Ashe", Mathf.Min(80f, streetL * .7f), -44f, 26f);
            var avoid = new List<Vector3>(); for (var d = 0f; d <= streetL; d += 20f) avoid.Add(new Vector3(heart2.x + D2.x * d, 14f, heart2.y + D2.y * d));
            ArtistPlacement.Spread(data, index, 54f, heart2, 44f, 40f, avoid, keep, Land, 330f);
            Level(data, index, heart2, 20f, 38f, plazaY);
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
            life = new GameObject("Heartbreak life").AddComponent<HeartLife>();
            life.transform.SetParent(ctx.Root, false);
            life.center = center; life.lamps = lampM; life.neon = neonM; life.windows = windowM; life.sea = seaM;
            var mk = new MeshKit(); var solid = new MeshKit();
            Remember(ctx.OnGround(center.x, center.z), 0);
            TownStart(ctx);
            Promenade(ctx, mk, solid);
            Boardwalk(ctx, mk, solid);
            Pier(ctx, mk, solid);
            Paths(ctx, mk, solid);
            Motel(ctx, mk, solid);
            BeachHouses(ctx, mk, solid);
            Wilds(ctx, mk, solid);
            mk.Build("The Heartbreak Boardwalk", ctx.Root, false);
            solid.BuildCollider("The Heartbreak Boardwalk walls", ctx.Root);
        }

        public override void Finish(LandContext ctx) { Verge(ctx); halos.Build("Heartbreak glow", ctx.Root); }

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

        /// A globe lamp on a curly iron post.
        void GlobeLamp(LandContext ctx, MeshKit mk, Vector3 g, bool light = false)
        {
            var up = Vector3.up; mk.Prism(iron, g - up * .2f, up, .07f, 3.8f, 6, .6f); mk.Prism(iron, g, up, .16f, .3f, 6); mk.Blob(lampM, g + up * 3.8f, Vector3.one * .24f, new System.Random(1), 0f); mk.Prism(iron, g + up * 3.55f, up, .1f, .08f, 6);
            halos.Add(g + up * 3.8f, 1.8f, new Color(1f, .86f, .64f), .5f); ArtistBits.PostCollider(ctx.Root, g, 3.6f, .09f); if (light) ArtistBits.PointLight(ctx.Root, g + up * 3.3f, new Color(1f, .82f, .6f), 14f, 2f);
        }

        /// A palm tree, leaning toward the sea.
        void Palm(LandContext ctx, MeshKit mk, Vector3 g, float s, System.Random rng)
        {
            var up = Vector3.up; var lean = (D * .3f + new Vector3((float)rng.NextDouble() - .5f, 0, (float)rng.NextDouble() - .5f) * .3f); var prev = g; var dir = up;
            for (var k = 0; k < 6; k++) { dir = (up + lean * (k / 5f)).normalized; mk.Prism(trunkM, prev, dir, (.2f - k * .02f) * s, 1.2f * s, 6); prev += dir * 1.2f * s; }
            for (var k = 0; k < 8; k++) { var fd = Quaternion.Euler(0, k * 45f + (float)rng.NextDouble() * 20f, 0) * Vector3.forward; var tip = prev + fd * 2.2f * s - up * .9f * s; var mid = prev + fd * 1.1f * s + up * .3f * s; var side = Vector3.Cross(up, fd) * .3f * s; mk.Tri(k % 2 == 0 ? leafM : leafDark, prev, mid + side, tip, prev + up); mk.Tri(k % 2 == 0 ? leafM : leafDark, prev, mid - side, tip, prev + up); mk.Tri(k % 2 == 0 ? leafM : leafDark, prev, mid + side, tip, prev - up * 3f); mk.Tri(k % 2 == 0 ? leafM : leafDark, prev, mid - side, tip, prev - up * 3f); }
            ArtistBits.PostCollider(ctx.Root, g, 3f * s, .22f * s); noGrass.Add(new Vector3(g.x, .8f, g.z));
        }

        /// A bench to sit on.
        void Bench(Transform parent, MeshKit mk, Vector3 g, Vector3 face, float len = 2f)
        {
            var up = Vector3.up; var rot = Quaternion.LookRotation(face); var side = Vector3.Cross(up, face); for (var k = 0; k < 3; k++) mk.Box(woodM, g + up * .46f + face * (-.16f + k * .16f), new Vector3(len, .05f, .13f), rot); for (var k = 0; k < 2; k++) mk.Box(woodM, g + up * (.7f + k * .2f) - face * .26f, new Vector3(len, .13f, .05f), rot); foreach (var e in new[] { -1f, 1f }) mk.Box(iron, g + up * .3f + side * e * (len / 2f - .15f), new Vector3(.06f, .6f, .5f), rot);
            BoxCol(parent, g + up * .45f, new Vector3(len, .9f, .5f), rot); Interact.Bench(tlife.transform, g + up * .52f - side * (len / 2f - .5f), g + up * .52f + side * (len / 2f - .5f), face);
        }

        /// A chair to sit on.
        void Chair(MeshKit mk, Vector3 g, Vector3 face, string verb = "Sit")
        {
            var up = Vector3.up; var rot = Quaternion.LookRotation(face); mk.Box(white, g + up * .45f, new Vector3(.46f, .05f, .46f), rot); mk.Box(white, g + up * .8f - face * .21f, new Vector3(.46f, .7f, .05f), rot); foreach (var (x, zz) in new[] { (-.2f, -.2f), (.2f, -.2f), (.2f, .2f), (-.2f, .2f) }) mk.Box(white, g + up * .22f + rot * new Vector3(x, 0, zz), new Vector3(.04f, .44f, .04f), rot);
            Interact.Seat(tlife.transform, g + up * .5f, face, verb);
        }

        /// A shuttered stall: a little striped-roof hut with its shutter down and a faded sign.
        void Stall(LandContext ctx, MeshKit mk, PlaceFrame f, int i)
        {
            var up = Vector3.up; string[] signs = { "SALTWATER TAFFY", "FORTUNES", "HOT DOGS", "PRIZES", "SNOW CONES", "PHOTOS 50¢", "ARCADE", "COTTON CANDY" };
            mk.Box(Paint(Faded[i % Faded.Length]), f.P(0, 1.3f, 0), new Vector3(2.8f, 2.6f, 2.2f), f.r); Col(f.P(0, 1.3f, 0), new Vector3(2.8f, 2.6f, 2.2f), f.r); mk.Box(Paint(new Color(.5f, .5f, .54f)), f.P(0, 1.3f, 1.12f), new Vector3(2.2f, 1.2f, .04f), f.r); for (var k = 0; k < 6; k++) mk.Box(dark, f.P(0, .8f + k * .2f, 1.15f), new Vector3(2.2f, .02f, .02f), f.r);
            for (var k = 0; k < 6; k++) mk.Box(k % 2 == 0 ? stripeA : stripeB, f.P(-1.25f + k * .5f, 2.75f + (k % 2) * .01f, .3f), new Vector3(.5f, .06f, 2.8f), f.r * Quaternion.Euler(12f, 0, 0));
            mk.Box(Paint(Cream), f.P(0, 2.2f, 1.14f), new Vector3(2.4f, .34f, .03f), f.r); Sign(f.P(0, 2.2f, 1.17f), f.Fwd, signs[i % signs.Length], .14f, Ink);
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
                    var dd = Mathf.Min(d, end); var q = from + dir * dd + side * Mathf.Sin(dd / 14f) * Mathf.Min(2f, end * .04f);
                    if (!ctx.IsLand(q.x, q.z, 0f)) break;
                    pts.Add(ctx.OnGround(q.x, q.z)); if (dd >= end) break;
                }
                if (pts.Count < 2) return;
                Strip(mk, pts, boards, PathW, .13f, ctx);
                foreach (var p in pts) Remember(p, walk);
                for (var i = 6; i < pts.Count; i += 9, n++)
                {
                    var s = n % 2 == 0 ? 1f : -1f; var lp = pts[i] + side * s * (PathW / 2f + .7f);
                    if (InFootprint(ctx, lp.x, lp.z, 1f) || InNoGrass(lp.x, lp.z, 1f)) continue;
                    GlobeLamp(ctx, mk, ctx.OnGround(lp.x, lp.z), n % 3 == 0);
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

        // ---------------------------------------------------------------- the dunes and the old town
        /// Palms, dune fences, benches under a globe lamp, beach huts, a closed stall here and there, deck chairs.
        void Wilds(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            int palms = 0, huts = 0, benches = 0, stalls = 0, fences = 0; var rng = new System.Random(2012);
            const float cell = 20f;
            for (var x = ctx.MinX; x < ctx.MaxX; x += cell)
            for (var z = ctx.MinZ; z < ctx.MaxZ; z += cell)
            {
                float px = x + (float)rng.NextDouble() * cell, pz = z + (float)rng.NextDouble() * cell; var roll = (float)rng.NextDouble(); var s = (float)rng.NextDouble();
                if (!Open(ctx, px, pz, 4f)) continue;
                var p = ctx.OnGround(px, pz); var f = FaceTo(p, NearestWalk(p));
                if (roll < .14f) { if (!Open(ctx, px, pz, 7f) || !ctx.IsLand(px, pz, 8f) || Mathf.Abs(ctx.Ground(px + 4f, pz) - ctx.Ground(px - 4f, pz)) > 2f || Mathf.Abs(ctx.Ground(px, pz + 4f) - ctx.Ground(px, pz - 4f)) > 2f) continue; BeachHouse(ctx, mk, solid, new PlaceFrame(p, f), huts + 30, rng); huts++; }
                else if (roll < .44f) { Palm(ctx, mk, p, .9f + s * .6f, rng); palms++; }
                else if (roll < .52f) { Bench(ctx.Root, mk, p, f); GlobeLamp(ctx, mk, p - f * 1f + Vector3.Cross(Vector3.up, f) * 1.4f, benches % 2 == 0); noGrass.Add(new Vector3(p.x, 2f, p.z)); benches++; }
                else if (roll < .57f) { Stall(ctx, mk, new PlaceFrame(p, f), stalls + 2); Mark(p, 2.4f); stalls++; }
                else if (roll < .66f) { var a = p - Vector3.Cross(Vector3.up, f) * 3f; for (var k = 0; k < 9; k++) { var q = ctx.OnGround(a.x + Vector3.Cross(Vector3.up, f).x * k * .75f, a.z + Vector3.Cross(Vector3.up, f).z * k * .75f); mk.Box(woodM, q + Vector3.up * (.4f + (k % 2) * .08f), new Vector3(.1f, .9f, .05f), Quaternion.LookRotation(f) * Quaternion.Euler(0, 0, (k % 3 - 1) * 5f)); } fences++; }
            }
            Debug.Log($"The Heartbreak Boardwalk: Boardwalk {streetL:0} m{(toSea ? ", pier out to sea" : ", no sea close enough for a pier")}, {huts} beach houses, {palms} palms, {benches} benches, {stalls} shut stalls, {fences} dune fences");
        }

        Vector3 NearestWalk(Vector3 p) { var best = center; var bd = float.MaxValue; foreach (var (q, _) in walkPoints) { var d = (q - p).sqrMagnitude; if (d < bd) { bd = d; best = q; } } return best; }

        /// Dune grass.
        void Verge(LandContext ctx)
        {
            var mat = WindMesh.Material(.4f, .8f, new Color(.6f, .66f, .5f));
            var meshes = new Dictionary<(int, int), WindMesh>(); var rng = new System.Random(2013);
            const float st = 2.6f;
            for (var x = ctx.MinX; x < ctx.MaxX; x += st)
            for (var z = ctx.MinZ; z < ctx.MaxZ; z += st)
            {
                var px = x + ((float)rng.NextDouble() - .5f) * st; var pz = z + ((float)rng.NextDouble() - .5f) * st;
                if (!ctx.IsLand(px, pz, 1f) || NearPath(px, pz, 3.4f) || InNoGrass(px, pz, 0f) || InFootprint(ctx, px, pz, -2f)) continue;
                if (Mathf.PerlinNoise(px * .04f + 8f, pz * .04f + 4f) < .42f) continue;
                var key = (Mathf.FloorToInt(px / 70f), Mathf.FloorToInt(pz / 70f));
                if (!meshes.TryGetValue(key, out var wm)) meshes[key] = wm = new WindMesh();
                var col = Color.Lerp(new Color(.56f, .62f, .46f), new Color(.74f, .72f, .54f), (float)rng.NextDouble());
                wm.Tuft(new Vector3(px, ctx.Ground(px, pz) - .04f, pz), .3f + (float)rng.NextDouble() * .4f, col, rng.NextDouble() < .08 ? Rose : Color.Lerp(col, new Color(.9f, .86f, .7f), .4f), rng, 3, .07f);
            }
            foreach (var kv in meshes) kv.Value.Build("Heartbreak dune grass", ctx.Root, mat);
        }
    }
}
