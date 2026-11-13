using System.Collections.Generic;
using UnityEngine;
using static EverythingWorld.World.PlaceTools;

namespace EverythingWorld.World
{
    /// The Latin Coast (WORLD_GUIDE.md): a colourful harbour town at sunset. Houses painted every colour there is,
    /// balconies of flowers, papel picado strung across every street, cobbles still warm from the day, a church with a
    /// yellow bell tower, and at the bottom of the hill the harbour with its painted boats and a striped lighthouse. You
    /// arrive in La Plaza, under a jacaranda wrapped in lights, where people are dancing. Calle Mayor runs down to the
    /// harbour. Warm, loud, joyful, a party that started hours ago.
    /// Laid out on purpose (the build-land standard): LatinTown.cs has the places and the people.
    public partial class LatinKit : LandKit
    {
        public override string Land => "The Latin Coast";
        public override Vector3 ArtistFront(IslandWorldData data, IslandWorldData.Artist a) => facing.TryGetValue(a.name, out var f) ? f : base.ArtistFront(data, a);

        public static readonly Color Marigold = new Color(1f, .62f, .1f), Fuchsia = new Color(.92f, .2f, .5f), Turquoise = new Color(.1f, .7f, .7f), Cobalt = new Color(.16f, .3f, .76f), Lime = new Color(.56f, .82f, .2f), Cream = new Color(.98f, .92f, .8f), Ink = new Color(.12f, .08f, .06f), Terra = new Color(.74f, .36f, .22f), Jacaranda = new Color(.6f, .44f, .9f);
        static readonly Color[] Walls = { new Color(.96f, .4f, .5f), new Color(1f, .72f, .26f), new Color(.24f, .66f, .7f), new Color(.36f, .44f, .86f), new Color(.6f, .8f, .36f), new Color(.98f, .86f, .62f), new Color(.86f, .34f, .26f), new Color(.7f, .44f, .84f) };
        const float PathW = 3f, StreetW = 6f;

        Material cobble, cobbleDark, tile, woodM, iron, dark, white, leafM, leafDark, purpleM, trunkM, windowM, lampM, seaGlow;
        GlowHalos halos; Vector3 center, D, S; LatinLife life;
        readonly Dictionary<(int, int), List<Vector3>> pathCells = new Dictionary<(int, int), List<Vector3>>();
        readonly List<(Vector3 p, int w)> walkPoints = new List<(Vector3, int)>();
        readonly Dictionary<string, Vector3> facing = new Dictionary<string, Vector3>();
        // x, radius, z
        readonly List<Vector3> noGrass = new List<Vector3>();
        Vector2 heart2, D2 = new Vector2(0, -1), S2 = new Vector2(1, 0); float plazaY, waterY, streetL; bool toSea;

        void MakeMaterials()
        {
            cobble = WorldMaterials.Solid(new Color(.74f, .62f, .5f)); cobbleDark = WorldMaterials.Solid(new Color(.56f, .46f, .38f)); tile = WorldMaterials.Solid(Terra); woodM = WorldMaterials.Solid(new Color(.5f, .34f, .22f)); iron = WorldMaterials.Solid(new Color(.16f, .14f, .14f), .5f); dark = WorldMaterials.Solid(new Color(.08f, .06f, .06f)); white = WorldMaterials.Solid(new Color(.98f, .94f, .88f));
            leafM = WorldMaterials.Solid(new Color(.24f, .54f, .26f)); leafDark = WorldMaterials.Solid(new Color(.16f, .4f, .22f)); purpleM = WorldMaterials.Glow(Jacaranda, .3f); trunkM = WorldMaterials.Solid(new Color(.42f, .32f, .24f));
            windowM = new Material(WorldMaterials.Glow(new Color(1f, .74f, .4f), 1.1f)) { name = "Latin windows" }; lampM = new Material(WorldMaterials.Glow(new Color(1f, .8f, .5f), 1.5f)) { name = "Latin lamps" }; seaGlow = new Material(WorldMaterials.Glow(Turquoise, .6f)) { name = "Latin sea glow" };
        }

        Material GlowM(Color c, float s = 2f) => WorldMaterials.Glow(c, s * .65f);
        Material Paint(Color c) => WorldMaterials.Solid(c);
        static bool HasStation(IslandWorldData.Artist a) => a.stationId != 0;
        static float Foot(IslandWorldData.Artist a) => a.name == "Shakira" ? 14f : a.name == "Bad Bunny" || a.name == "Los Lobos" || a.name == "Natalia Lafourcade" ? 10f : 8.5f;

        public override bool Ground(float height, float slope, float patch, out Color color)
        {
            // warm red earth on the slopes, dry green hills, sand down by the water
            if (height < 3.5f) color = Color.Lerp(new Color(.9f, .78f, .58f), new Color(.94f, .82f, .62f), patch);
            else if (slope > 24f) color = Color.Lerp(new Color(.66f, .38f, .26f), new Color(.74f, .46f, .3f), patch);
            else color = Color.Lerp(new Color(.46f, .58f, .3f), new Color(.66f, .64f, .34f), Mathf.SmoothStep(0f, 1f, patch));
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
            // Calle Mayor runs down to the open sea, if it's near enough, and ends at the harbour
            toSea = false; var coastL = 9999f;
            for (var k = 0; k < 16; k++) { var dir = new Vector2(Mathf.Sin(k * Mathf.PI / 8f), Mathf.Cos(k * Mathf.PI / 8f)); for (var d = 60f; d < 520f; d += 6f) { var p = heart2 + dir * d; var at = data.IslandAt(p.x, p.y); if (at == isl) continue; if (at == null && d < coastL) { coastL = d; D2 = dir; toSea = true; } break; } }
            if (toSea) streetL = Mathf.Max(60f, coastL - 30f);
            else { streetL = 0f; for (var k = 0; k < 8; k++) { var dir = new Vector2(Mathf.Sin(k * Mathf.PI / 4f), Mathf.Cos(k * Mathf.PI / 4f)); var len = 40f; while (len < 130f && Inside(heart2 + dir * (len + 10f), 40f)) len += 10f; if (len > streetL) { streetL = len; D2 = dir; } } }
            S2 = new Vector2(-D2.y, D2.x); plazaY = Mathf.Max(data.HeightAt(heart2.x, heart2.y), waterY + 6f);
            var hb = P2(streetL + 12f, 0);
            var keep = new List<string>();
            void Put(string name, float d, float s, float room) { var p = P2(d, s); if (!Inside(p, room)) return; foreach (var a in data.layout.artists) if (a.sceneId == isl.sceneId && a.name == name) { a.x = p.x; a.z = p.y; keep.Add(name); } }
            Put("Shakira", -6f, 52f, 28f); Put("Bad Bunny", 0f, -48f, 22f);
            var avoid = new List<Vector3>(); for (var d = 0f; d <= streetL; d += 20f) avoid.Add(new Vector3(heart2.x + D2.x * d, 14f, heart2.y + D2.y * d)); avoid.Add(new Vector3(hb.x, 22f, hb.y));
            ArtistPlacement.Spread(data, index, 54f, heart2, 44f, 40f, avoid, keep, Land, 330f);
            Level(data, index, heart2, 22f, 40f, plazaY);
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
            life = new GameObject("Latin life").AddComponent<LatinLife>();
            life.transform.SetParent(ctx.Root, false);
            life.center = center; life.lamps = lampM; life.windows = windowM; life.sea = seaGlow;
            var mk = new MeshKit(); var solid = new MeshKit();
            Remember(ctx.OnGround(center.x, center.z), 0);
            TownStart(ctx);
            LaPlaza(ctx, mk, solid);
            CalleMayor(ctx, mk, solid);
            Harbour(ctx, mk, solid);
            Paths(ctx, mk, solid);
            Houses(ctx, mk, solid);
            Wilds(ctx, mk, solid);
            mk.Build("The Latin Coast", ctx.Root, false);
            solid.BuildCollider("The Latin Coast walls", ctx.Root);
        }

        public override void Finish(LandContext ctx) { Verge(ctx); halos.Build("Latin glow", ctx.Root); }

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

        /// A wrought-iron lantern on a bracket post.
        void Lantern(LandContext ctx, MeshKit mk, Vector3 g, Vector3 toward, bool light = false)
        {
            var up = Vector3.up; toward.y = 0; toward.Normalize(); mk.Prism(iron, g - up * .2f, up, .07f, 3.8f, 6, .7f); mk.Prism(iron, g + up * 3.5f, (toward + up * .2f).normalized, .03f, .7f, 4); var h = g + up * 3.3f + toward * .7f; mk.Box(lampM, h, new Vector3(.24f, .36f, .24f), Quaternion.identity); mk.Cone(iron, h + up * .2f, .2f, .2f, 4, 45f);
            halos.Add(h, 1.5f, new Color(1f, .8f, .5f), .45f); ArtistBits.PostCollider(ctx.Root, g, 3.6f, .09f); if (light) ArtistBits.PointLight(ctx.Root, h - up * .6f, new Color(1f, .78f, .5f), 14f, 1.8f);
        }

        /// A line of papel picado: cut paper flags in bright colours, sagging from a to b.
        void Picado(MeshKit mk, Vector3 a, Vector3 b, int phase)
        {
            var len = Vector3.Distance(a, b); var n = Mathf.Max(3, Mathf.RoundToInt(len / .7f)); var sag = Mathf.Min(1f, len * .06f); var prev = a; var along = (b - a).normalized; var face = Vector3.Cross(Vector3.up, along);
            var cols = new[] { Fuchsia, Marigold, Turquoise, Lime, Cobalt, new Color(1f, .9f, .3f) };
            for (var i = 1; i <= n; i++) { var t = i / (float)n; var p = Vector3.Lerp(a, b, t) - Vector3.up * Mathf.Sin(t * Mathf.PI) * sag; mk.Prism(dark, prev, (p - prev).normalized, .008f, Vector3.Distance(prev, p), 3); var mid = (prev + p) / 2f; mk.Box(Paint(cols[(i + phase) % cols.Length]), mid - Vector3.up * .26f, new Vector3(.52f, .5f, .01f), Quaternion.LookRotation(face)); prev = p; }
        }

        /// A palm.
        void Palm(LandContext ctx, MeshKit mk, Vector3 g, float s, System.Random rng)
        {
            var up = Vector3.up; var lean = new Vector3((float)rng.NextDouble() - .5f, 0, (float)rng.NextDouble() - .5f) * .35f; var prev = g; var dir = up;
            for (var k = 0; k < 6; k++) { dir = (up + lean * (k / 5f)).normalized; mk.Prism(trunkM, prev, dir, (.2f - k * .02f) * s, 1.3f * s, 6); prev += dir * 1.3f * s; }
            for (var k = 0; k < 8; k++) { var fd = Quaternion.Euler(0, k * 45f + (float)rng.NextDouble() * 20f, 0) * Vector3.forward; var tip = prev + fd * 2.4f * s - up * 1f * s; var mid = prev + fd * 1.2f * s + up * .3f * s; var side = Vector3.Cross(up, fd) * .3f * s; var m = k % 2 == 0 ? leafM : leafDark; foreach (var o in new[] { up, -up * 3f }) { mk.Tri(m, prev, mid + side, tip, prev + o); mk.Tri(m, prev, mid - side, tip, prev + o); } }
            ArtistBits.PostCollider(ctx.Root, g, 3f * s, .22f * s); noGrass.Add(new Vector3(g.x, .8f, g.z));
        }

        /// A flowering tree: bougainvillea pink or jacaranda purple.
        void Blossom(LandContext ctx, MeshKit mk, Vector3 g, float s, System.Random rng, bool purple)
        {
            var up = Vector3.up; mk.Prism(trunkM, g - up * .2f, up, .18f * s, 2.6f * s, 6, .6f); ArtistBits.PostCollider(ctx.Root, g, 2.4f * s, .22f * s); var m = purple ? purpleM : Paint(new Color(.94f, .3f, .56f));
            for (var k = 0; k < 5; k++) mk.Blob(k % 2 == 0 ? m : leafM, g + up * (3f + (float)rng.NextDouble() * .8f) * s + Quaternion.Euler(0, k * 72f, 0) * Vector3.forward * (k == 0 ? 0f : 1.2f * s), new Vector3(1.4f, 1f, 1.4f) * s, rng, .2f);
            noGrass.Add(new Vector3(g.x, 1f * s, g.z));
        }

        /// A bench to sit on, tiled.
        void Bench(Transform parent, MeshKit mk, Vector3 g, Vector3 face, float len = 2f)
        {
            var up = Vector3.up; var rot = Quaternion.LookRotation(face); var side = Vector3.Cross(up, face); mk.Box(white, g + up * .22f, new Vector3(len, .44f, .5f), rot); mk.Box(Paint(Cobalt), g + up * .46f, new Vector3(len + .04f, .04f, .54f), rot); mk.Box(white, g + up * .7f - face * .24f, new Vector3(len, .5f, .1f), rot); for (var k = 0; k < Mathf.RoundToInt(len / .25f); k++) mk.Box(Paint(k % 2 == 0 ? Cobalt : Marigold), g + up * .2f + side * (-len / 2f + .125f + k * .25f) + face * .26f, new Vector3(.22f, .22f, .01f), rot);
            BoxCol(parent, g + up * .45f, new Vector3(len, .9f, .5f), rot); Interact.Bench(tlife.transform, g + up * .5f - side * (len / 2f - .5f), g + up * .5f + side * (len / 2f - .5f), face);
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
                Strip(mk, pts, cobble, PathW, .13f, ctx);
                foreach (var p in pts) Remember(p, walk);
                Vector3 last = default; var have = false;
                for (var i = 6; i < pts.Count; i += 9, n++)
                {
                    var s = n % 2 == 0 ? 1f : -1f; var lp = pts[i] + side * s * (PathW / 2f + .7f);
                    if (InFootprint(ctx, lp.x, lp.z, 1f) || InNoGrass(lp.x, lp.z, 1f)) continue;
                    var g = ctx.OnGround(lp.x, lp.z); Lantern(ctx, mk, g, -side * s, n % 3 == 0);
                    if (have && Vector3.Distance(g, last) < 20f) Picado(mk, last + Vector3.up * 3.6f, g + Vector3.up * 3.6f, n);
                    last = g; have = true;
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

        // ---------------------------------------------------------------- the hills round the town
        /// Painted houses, palms, flowering trees, agaves, tiled benches under a lantern, little shrines, fruit carts.
        void Wilds(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            int houses = 0, palms = 0, blossoms = 0, agaves = 0, benches = 0, carts = 0; var rng = new System.Random(1999);
            const float cell = 20f;
            for (var x = ctx.MinX; x < ctx.MaxX; x += cell)
            for (var z = ctx.MinZ; z < ctx.MaxZ; z += cell)
            {
                float px = x + (float)rng.NextDouble() * cell, pz = z + (float)rng.NextDouble() * cell; var roll = (float)rng.NextDouble(); var s = (float)rng.NextDouble();
                if (!Open(ctx, px, pz, 4f)) continue;
                var p = ctx.OnGround(px, pz); var f = FaceTo(p, NearestWalk(p)); var up = Vector3.up;
                if (roll < .2f) { if (!Open(ctx, px, pz, 7f) || !ctx.IsLand(px, pz, 8f) || Mathf.Abs(ctx.Ground(px + 4f, pz) - ctx.Ground(px - 4f, pz)) > 2.2f || Mathf.Abs(ctx.Ground(px, pz + 4f) - ctx.Ground(px, pz - 4f)) > 2.2f) continue; House(ctx, mk, solid, new PlaceFrame(p, f), houses + 30, rng); houses++; }
                else if (roll < .36f) { Palm(ctx, mk, p, 1f + s * .5f, rng); palms++; }
                else if (roll < .5f) { Blossom(ctx, mk, p, .9f + s * .5f, rng, roll > .44f); blossoms++; }
                else if (roll < .6f) { for (var k = 0; k < 9; k++) { var d = (up + Quaternion.Euler(0, k * 40f, 0) * Vector3.forward * .7f).normalized; mk.Prism(Paint(new Color(.36f, .56f, .52f)), p, d, .12f, 1.2f + (k % 3) * .3f, 3, .1f); } if (s > .7f) { mk.Prism(Paint(new Color(.4f, .5f, .4f)), p, up, .04f, 3f, 4); mk.Blob(Paint(Marigold), p + up * 3f, Vector3.one * .2f, rng, .3f); } ArtistBits.PostCollider(ctx.Root, p, 1f, .5f); noGrass.Add(new Vector3(p.x, 1.2f, p.z)); agaves++; }
                else if (roll < .67f) { Bench(ctx.Root, mk, p, f); Lantern(ctx, mk, p - f * 1f + Vector3.Cross(up, f) * 1.4f, f, benches % 2 == 0); noGrass.Add(new Vector3(p.x, 2f, p.z)); benches++; }
                else if (roll < .71f) { var rot = Quaternion.LookRotation(f); mk.Box(Paint(Walls[carts % Walls.Length]), p + up * .7f, new Vector3(1.6f, .8f, .9f), rot); foreach (var e in new[] { -1f, 1f }) mk.Prism(dark, p + up * .3f + Vector3.Cross(up, f) * e * .8f, Vector3.Cross(up, f) * e, .3f, .06f, 10); for (var k = 0; k < 6; k++) mk.Blob(Paint(new[] { Marigold, Fuchsia, Lime, new Color(.9f, .2f, .2f) }[k % 4]), p + up * 1.16f + Vector3.Cross(up, f) * (-.6f + k * .24f), Vector3.one * .1f, rng, .2f); mk.Prism(white, p - Vector3.Cross(up, f) * .7f + up * 1.1f, up, .02f, 1.2f, 4); for (var k = 0; k < 4; k++) mk.Box(k % 2 == 0 ? Paint(Fuchsia) : white, p + up * 2.3f + Vector3.Cross(up, f) * (-.6f + k * .4f), new Vector3(.4f, .04f, 1.2f), rot * Quaternion.Euler(10f, 0, 0)); BoxCol(ctx.Root, p + up * .6f, new Vector3(1.8f, 1.2f, 1f), rot); carts++; }
            }
            Debug.Log($"The Latin Coast: Calle Mayor {streetL:0} m{(toSea ? " to the sea" : "")}, {houses} painted houses, {palms} palms, {blossoms} flowering trees, {agaves} agaves, {benches} benches, {carts} fruit carts");
        }

        Vector3 NearestWalk(Vector3 p) { var best = center; var bd = float.MaxValue; foreach (var (q, _) in walkPoints) { var d = (q - p).sqrMagnitude; if (d < bd) { bd = d; best = q; } } return best; }

        /// Dry hillside grass with flowers in it.
        void Verge(LandContext ctx)
        {
            var mat = WindMesh.Material(.35f, .8f, new Color(.56f, .62f, .34f));
            var meshes = new Dictionary<(int, int), WindMesh>(); var rng = new System.Random(2000);
            var flowers = new[] { Fuchsia, Marigold, new Color(1f, .9f, .3f), Jacaranda, Color.white };
            const float st = 2.6f;
            for (var x = ctx.MinX; x < ctx.MaxX; x += st)
            for (var z = ctx.MinZ; z < ctx.MaxZ; z += st)
            {
                var px = x + ((float)rng.NextDouble() - .5f) * st; var pz = z + ((float)rng.NextDouble() - .5f) * st;
                if (!ctx.IsLand(px, pz, 1f) || NearPath(px, pz, 3.6f) || InNoGrass(px, pz, 0f) || InFootprint(ctx, px, pz, -2f)) continue;
                if (Mathf.PerlinNoise(px * .04f + 8f, pz * .04f + 4f) < .42f) continue;
                var key = (Mathf.FloorToInt(px / 70f), Mathf.FloorToInt(pz / 70f));
                if (!meshes.TryGetValue(key, out var wm)) meshes[key] = wm = new WindMesh();
                var col = Color.Lerp(new Color(.48f, .58f, .3f), new Color(.7f, .66f, .36f), (float)rng.NextDouble());
                wm.Tuft(new Vector3(px, ctx.Ground(px, pz) - .04f, pz), .26f + (float)rng.NextDouble() * .34f, col, rng.NextDouble() < .2 ? flowers[rng.Next(flowers.Length)] : Color.Lerp(col, Cream, .4f), rng, 3, .08f);
            }
            foreach (var kv in meshes) kv.Value.Build("Latin hillside grass", ctx.Root, mat);
        }
    }
}
