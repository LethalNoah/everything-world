using System.Collections.Generic;
using UnityEngine;
using static EverythingWorld.World.PlaceTools;

namespace EverythingWorld.World
{
    /// The Neo Soul Quarter (WORLD_GUIDE.md): a candlelit evening in a quarter the houseplants have taken over. The
    /// sky is plum; the houses are terracotta, honey and clay, their windows warm; candles burn on every step and
    /// strings of honey-coloured lights cross the lanes; and the plants have grown as big as trees: monsteras leaning
    /// over the rooftops, ferns arching across the street, snake plants as tall as lamp posts, all still in their
    /// pots. You arrive in the Courtyard. Honey Street runs out of it between the houses. Slow, warm, intimate.
    /// Laid out on purpose (the build-land standard): NeoTown.cs has the places and the people.
    public partial class NeoKit : LandKit
    {
        public override string Land => "The Neo Soul Quarter";
        public override Vector3 ArtistFront(IslandWorldData data, IslandWorldData.Artist a) => facing.TryGetValue(a.name, out var f) ? f : base.ArtistFront(data, a);

        public static readonly Color Clay = new Color(.72f, .4f, .26f), Honey = new Color(1f, .78f, .36f), Plant = new Color(.2f, .5f, .28f), Plum = new Color(.36f, .2f, .4f), Cream = new Color(.97f, .92f, .82f), Ink = new Color(.1f, .07f, .1f), Rose = new Color(.9f, .45f, .5f);
        static readonly Color[] Walls = { new Color(.72f, .4f, .26f), new Color(.82f, .56f, .34f), new Color(.6f, .32f, .24f), new Color(.86f, .7f, .5f), new Color(.5f, .3f, .32f), new Color(.66f, .46f, .3f) };
        const float PathW = 3f, StreetW = 5f, HouseW = 9f, HouseD = 8f;

        Material tile, tileDark, pot, woodM, iron, dark, white, leafM, leafDark, leafPale, stemM, candleM, windowM, lampM, clothM;
        readonly Material[] bulbM = new Material[3];
        GlowHalos halos; Vector3 center, D, S; NeoLife life;
        readonly Dictionary<(int, int), List<Vector3>> pathCells = new Dictionary<(int, int), List<Vector3>>();
        readonly List<(Vector3 p, int w)> walkPoints = new List<(Vector3, int)>();
        readonly Dictionary<string, Vector3> facing = new Dictionary<string, Vector3>();
        // x, radius, z
        readonly List<Vector3> noGrass = new List<Vector3>();
        Vector2 heart2, D2 = new Vector2(0, -1), S2 = new Vector2(1, 0); float plazaY, waterY, streetL;

        void MakeMaterials()
        {
            // paving, walls and leaves are faintly self-lit, so the dusk never swallows them
            tile = WorldMaterials.Glow(new Color(.66f, .42f, .32f), .16f); tileDark = WorldMaterials.Glow(new Color(.46f, .28f, .26f), .14f); pot = WorldMaterials.Glow(new Color(.74f, .42f, .28f), .14f);
            woodM = WorldMaterials.Glow(new Color(.42f, .28f, .2f), .1f); iron = WorldMaterials.Solid(new Color(.14f, .12f, .14f), .5f); dark = WorldMaterials.Solid(new Color(.08f, .06f, .08f)); white = WorldMaterials.Glow(new Color(.95f, .92f, .86f), .12f);
            leafM = WorldMaterials.Glow(new Color(.16f, .46f, .26f), .16f); leafDark = WorldMaterials.Glow(new Color(.1f, .32f, .22f), .12f); leafPale = WorldMaterials.Glow(new Color(.36f, .6f, .32f), .16f); stemM = WorldMaterials.Glow(new Color(.3f, .4f, .22f), .1f);
            clothM = WorldMaterials.Glow(new Color(.5f, .2f, .3f), .16f);
            candleM = new Material(WorldMaterials.Glow(new Color(1f, .72f, .3f), 2.2f)) { name = "Neo candles" }; windowM = new Material(WorldMaterials.Glow(new Color(1f, .76f, .42f), 1.1f)) { name = "Neo windows" }; lampM = new Material(WorldMaterials.Glow(Honey, 1.6f)) { name = "Neo lamps" };
            for (var i = 0; i < 3; i++) bulbM[i] = new Material(WorldMaterials.Glow(new Color(1f, .8f, .45f), 1.6f)) { name = "Neo bulbs " + i };
        }

        Material GlowM(Color c, float s = 2f) => WorldMaterials.Glow(c, s * .65f);
        Material Paint(Color c) => WorldMaterials.Glow(c, .15f);
        static bool HasStation(IslandWorldData.Artist a) => a.stationId != 0;
        static float Foot(IslandWorldData.Artist a)
        {
            switch (a.name)
            {
                case "Maxwell": return 15f;
                case "Ms. Lauryn Hill": case "Erykah Badu": return 13f;
                case "Solange": case "D'Angelo": return 11f;
                default: return HasStation(a) ? 12f : 9f;
            }
        }

        public override bool Ground(float height, float slope, float patch, out Color color)
        {
            // dark moss and warm earth in the dusk
            if (slope > 26f) color = Color.Lerp(new Color(.4f, .27f, .3f), new Color(.5f, .34f, .34f), patch);
            else color = patch < .3f ? Color.Lerp(new Color(.58f, .4f, .36f), new Color(.48f, .42f, .36f), patch / .3f) : Color.Lerp(new Color(.3f, .5f, .38f), new Color(.4f, .6f, .42f), Mathf.SmoothStep(0f, 1f, (patch - .3f) / .7f));
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
            // Honey Street runs whichever way the land runs longest
            streetL = 0f;
            for (var k = 0; k < 8; k++) { var dir = new Vector2(Mathf.Sin(k * Mathf.PI / 4f), Mathf.Cos(k * Mathf.PI / 4f)); var len = 40f; while (len < 230f && Inside(heart2 + dir * (len + 10f), 40f)) len += 10f; if (len > streetL) { streetL = len; D2 = dir; } }
            S2 = new Vector2(-D2.y, D2.x); plazaY = Mathf.Max(data.HeightAt(heart2.x, heart2.y), waterY + 6f);

            var keep = new List<string>();
            void Put(string name, float d, float s, float room) { var p = P2(d, s); if (!Inside(p, room)) return; foreach (var a in data.layout.artists) if (a.sceneId == isl.sceneId && a.name == name) { a.x = p.x; a.z = p.y; keep.Add(name); } }
            // the lounge on the Courtyard; the big courtyards off Honey Street
            Put("Maxwell", -8f, 46f, 28f); Put("Ms. Lauryn Hill", 2f, -50f, 28f);
            Put("Erykah Badu", 78f, 40f, 26f); Put("D'Angelo", 80f, -40f, 24f); if (streetL > 170f) Put("Solange", 150f, 38f, 24f);
            var avoid = new List<Vector3>(); for (var d = 0f; d <= streetL; d += 22f) avoid.Add(new Vector3(heart2.x + D2.x * d, 12f, heart2.y + D2.y * d));
            ArtistPlacement.Spread(data, index, 56f, heart2, 44f, 42f, avoid, keep, Land, 330f);
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
            life = new GameObject("Neo Soul life").AddComponent<NeoLife>();
            life.transform.SetParent(ctx.Root, false);
            life.center = center; life.candles = candleM; life.windows = windowM; life.lamps = lampM; life.bulbs = bulbM;
            var mk = new MeshKit(); var solid = new MeshKit();
            Remember(ctx.OnGround(center.x, center.z), 0);
            TownStart(ctx);
            Courtyard(ctx, mk, solid);
            HoneyStreet(ctx, mk, solid);
            Paths(ctx, mk, solid);
            Houses(ctx, mk, solid);
            Wilds(ctx, mk, solid);
            mk.Build("The Neo Soul Quarter", ctx.Root, false);
            solid.BuildCollider("The Neo Soul Quarter walls", ctx.Root);
        }

        public override void Finish(LandContext ctx) { Verge(ctx); halos.Build("Neo Soul glow", ctx.Root); }

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

        /// A candle: a pale stub with a flame.
        void Candle(MeshKit mk, Vector3 g, float h = .2f, bool halo = false)
        {
            mk.Prism(white, g, Vector3.up, .04f, h, 6); mk.Blob(candleM, g + Vector3.up * (h + .06f), new Vector3(.03f, .06f, .03f), new System.Random(1), 0f); if (halo) halos.Add(g + Vector3.up * (h + .1f), .8f, Honey, .5f);
        }

        /// A cluster of candles of different heights on the ground or a step.
        void Candles(MeshKit mk, Vector3 g, int n, System.Random rng)
        {
            for (var k = 0; k < n; k++) Candle(mk, g + new Vector3(((float)rng.NextDouble() - .5f) * .7f, 0, ((float)rng.NextDouble() - .5f) * .7f), .12f + (float)rng.NextDouble() * .3f, k == 0);
        }

        /// A tall lantern on a post: a honey-coloured glass box.
        void Lantern(LandContext ctx, MeshKit mk, Vector3 g, bool light = false)
        {
            var up = Vector3.up; mk.Prism(iron, g - up * .2f, up, .05f, 2.9f, 5); mk.Box(lampM, g + up * 2.9f, new Vector3(.3f, .44f, .3f), Quaternion.identity); mk.Cone(iron, g + up * 3.12f, .28f, .2f, 4, 45f); halos.Add(g + up * 2.9f, 1.8f, Honey, .55f); ArtistBits.PostCollider(ctx.Root, g, 2.8f, .08f);
            if (light) ArtistBits.PointLight(ctx.Root, g + up * 2.6f, new Color(1f, .78f, .5f), 16f, 2.4f);
        }

        /// A string of honey-coloured lights from a to b, sagging.
        void BulbString(MeshKit mk, Vector3 a, Vector3 b, int phase = 0)
        {
            var len = Vector3.Distance(a, b); var n = Mathf.Max(3, Mathf.RoundToInt(len / 1.1f)); var sag = Mathf.Min(1.2f, len * .07f); var prev = a;
            for (var i = 1; i <= n; i++)
            {
                var t = i / (float)n; var p = Vector3.Lerp(a, b, t) - Vector3.up * Mathf.Sin(t * Mathf.PI) * sag;
                mk.Prism(dark, prev, (p - prev).normalized, .012f, Vector3.Distance(prev, p), 3); prev = p;
                if (i < n) { mk.Blob(bulbM[(i + phase) % 3], p - Vector3.up * .08f, Vector3.one * .075f, new System.Random(i), 0f); if (i % 4 == 0) halos.Add(p - Vector3.up * .08f, .7f, Honey, .3f); }
            }
        }

        /// A giant terracotta pot. Returns the top of the soil.
        Vector3 Pot(LandContext ctx, MeshKit mk, Vector3 g, float s)
        {
            var up = Vector3.up; mk.Prism(pot, g - up * .1f, up, .7f * s, 1f * s, 10, 1.3f); mk.Prism(pot, g + up * .82f * s, up, .98f * s, .14f * s, 10); mk.Prism(dark, g + up * .96f * s, up, .86f * s, .02f, 10); ArtistBits.PostCollider(ctx.Root, g, 1f * s, .9f * s); return g + up * (.96f * s + .02f);
        }

        /// A monstera as big as a tree: long stems, great split leaves.
        void Monstera(LandContext ctx, MeshKit mk, Vector3 g, float s, System.Random rng)
        {
            var up = Vector3.up; var top = Pot(ctx, mk, g, s);
            for (var k = 0; k < 7; k++)
            {
                var d = (Quaternion.Euler(0, k * 51f + (float)rng.NextDouble() * 30f, 0) * Vector3.forward * (.5f + (float)rng.NextDouble() * .5f) + up).normalized; var len = (2.4f + (float)rng.NextDouble() * 2.6f) * s; mk.Prism(stemM, top, d, .06f * s, len, 4, .6f); var tip = top + d * len; var flat = new Vector3(d.x, 0, d.z).normalized; var lr = Quaternion.LookRotation(flat) * Quaternion.Euler(30f + (float)rng.NextDouble() * 30f, 0, 0); var m = k % 3 == 0 ? leafPale : k % 3 == 1 ? leafM : leafDark;
                mk.Box(m, tip + flat * .6f * s, new Vector3(.3f * s, .04f, 1.9f * s), lr); foreach (var e in new[] { -1f, 1f }) for (var q = 0; q < 3; q++) mk.Box(m, tip + flat * (.2f + q * .5f) * s + lr * Vector3.right * e * .52f * s, new Vector3(.8f * s, .04f, .34f * s), lr * Quaternion.Euler(0, e * -22f, 0));
            }
            noGrass.Add(new Vector3(g.x, 1.4f * s, g.z));
        }

        /// A fern as big as a tree: arching fronds.
        void Fern(LandContext ctx, MeshKit mk, Vector3 g, float s, System.Random rng)
        {
            var up = Vector3.up; var top = Pot(ctx, mk, g, s * .8f);
            for (var k = 0; k < 11; k++) { var flat = Quaternion.Euler(0, k * 33f + (float)rng.NextDouble() * 14f, 0) * Vector3.forward; var m = k % 2 == 0 ? leafM : leafPale; for (var q = 0; q < 6; q++) { var t = q / 5f; var p = top + flat * (t * 3.2f * s) + up * (Mathf.Sin(t * 2.2f) * 2f * s); mk.Box(m, p, new Vector3((1.1f - t * .8f) * s, .03f, .7f * s), Quaternion.LookRotation(flat) * Quaternion.Euler(-50f + t * 110f, 0, 0)); } }
            noGrass.Add(new Vector3(g.x, 1.2f * s, g.z));
        }

        /// A snake plant: tall stiff blades, as tall as a lamp post.
        void SnakePlant(LandContext ctx, MeshKit mk, Vector3 g, float s, System.Random rng)
        {
            var up = Vector3.up; var top = Pot(ctx, mk, g, s * .7f);
            for (var k = 0; k < 8; k++) { var lean = Quaternion.Euler(0, k * 45f, 0) * Vector3.forward * (.1f + (float)rng.NextDouble() * .2f); var d = (up + lean).normalized; var len = (2.6f + (float)rng.NextDouble() * 2.4f) * s; mk.Prism(k % 2 == 0 ? leafDark : leafM, top + lean * .6f * s, d, .16f * s, len, 4, .2f); mk.Prism(leafPale, top + lean * .6f * s + Vector3.Cross(up, lean).normalized * .02f, d, .17f * s, len * .96f, 3, .2f); }
            noGrass.Add(new Vector3(g.x, 1f * s, g.z));
        }

        /// A fiddle-leaf fig: a slim trunk and big paddle leaves.
        void Fig(LandContext ctx, MeshKit mk, Vector3 g, float s, System.Random rng)
        {
            var up = Vector3.up; var top = Pot(ctx, mk, g, s * .8f); mk.Prism(woodM, top, up, .1f * s, 4.4f * s, 5, .6f);
            for (var k = 0; k < 14; k++) { var flat = Quaternion.Euler(0, k * 137f, 0) * Vector3.forward; var h = (1.8f + k * .22f) * s; mk.Box(k % 3 == 0 ? leafPale : k % 3 == 1 ? leafM : leafDark, top + up * h + flat * .7f * s, new Vector3(.8f * s, .04f, 1.2f * s), Quaternion.LookRotation(flat) * Quaternion.Euler(-24f + (float)rng.NextDouble() * 30f, 0, 0)); }
            noGrass.Add(new Vector3(g.x, 1.2f * s, g.z));
        }

        /// A low sofa or bench to sit on, in velvet.
        void Sofa(Transform parent, MeshKit mk, Vector3 g, Vector3 face, Material m, float len = 2.2f)
        {
            var up = Vector3.up; var rot = Quaternion.LookRotation(face); var side = Vector3.Cross(up, face); mk.Box(m, g + up * .28f, new Vector3(len, .4f, .9f), rot); mk.Box(m, g + up * .66f - face * .36f, new Vector3(len, .6f, .2f), rot); foreach (var e in new[] { -1f, 1f }) mk.Box(m, g + up * .46f + side * e * (len / 2f - .1f), new Vector3(.2f, .46f, .9f), rot);
            BoxCol(parent, g + up * .45f, new Vector3(len, .9f, .9f), rot); Interact.Bench(tlife.transform, g + up * .52f - side * (len / 2f - .6f) + face * .1f, g + up * .52f + side * (len / 2f - .6f) + face * .1f, face);
        }

        /// A wooden chair to sit on.
        void Chair(MeshKit mk, Vector3 g, Vector3 face)
        {
            var up = Vector3.up; var rot = Quaternion.LookRotation(face); mk.Box(woodM, g + up * .45f, new Vector3(.46f, .05f, .46f), rot); mk.Box(woodM, g + up * .8f - face * .21f, new Vector3(.46f, .7f, .05f), rot); foreach (var (x, zz) in new[] { (-.2f, -.2f), (.2f, -.2f), (.2f, .2f), (-.2f, .2f) }) mk.Box(woodM, g + up * .22f + rot * new Vector3(x, 0, zz), new Vector3(.04f, .44f, .04f), rot);
            Interact.Seat(tlife.transform, g + up * .5f, face);
        }

        // ---------------------------------------------------------------- the lanes to the artists
        void Paths(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var walk = 10; var n = 0; var rng = new System.Random(30);
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
                Strip(mk, pts, tile, PathW, .13f, ctx);
                foreach (var p in pts) Remember(p, walk);
                for (var i = 6; i < pts.Count; i += 9, n++)
                {
                    var s = n % 2 == 0 ? 1f : -1f; var lp = pts[i] + side * s * (PathW / 2f + .7f);
                    if (InFootprint(ctx, lp.x, lp.z, 1f) || InNoGrass(lp.x, lp.z, 1f)) continue;
                    if (n % 2 == 0) Lantern(ctx, mk, ctx.OnGround(lp.x, lp.z), true); else Candles(mk, ctx.OnGround(lp.x, lp.z) + Vector3.up * .02f, 4, rng);
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

        // ---------------------------------------------------------------- the overgrown quarter
        /// Houseplants as big as trees in their pots, benches with a lantern, clusters of candles, hanging baskets.
        void Wilds(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            int monsteras = 0, ferns = 0, snakes = 0, figs = 0, benches = 0, candles = 0; var rng = new System.Random(1996);
            const float cell = 19f;
            for (var x = ctx.MinX; x < ctx.MaxX; x += cell)
            for (var z = ctx.MinZ; z < ctx.MaxZ; z += cell)
            {
                float px = x + (float)rng.NextDouble() * cell, pz = z + (float)rng.NextDouble() * cell; var roll = (float)rng.NextDouble(); var s = (float)rng.NextDouble();
                if (!Open(ctx, px, pz, 4f)) continue;
                var p = ctx.OnGround(px, pz);
                if (roll < .16f) { Monstera(ctx, mk, p, 1.2f + s * .9f, rng); monsteras++; }
                else if (roll < .3f) { Fern(ctx, mk, p, 1f + s * .7f, rng); ferns++; }
                else if (roll < .42f) { SnakePlant(ctx, mk, p, 1f + s * .8f, rng); snakes++; }
                else if (roll < .54f) { Fig(ctx, mk, p, 1.1f + s * .7f, rng); figs++; }
                else if (roll < .6f) { var f = FaceTo(p, NearestWalk(p)); Sofa(ctx.Root, mk, p, f, clothM, 2f); Lantern(ctx, mk, p - f * 1.2f + Vector3.Cross(Vector3.up, f) * 1.4f, benches % 2 == 0); noGrass.Add(new Vector3(p.x, 2f, p.z)); benches++; }
                else if (roll < .72f) { Candles(mk, p + Vector3.up * .02f, 5 + (int)(s * 5f), rng); candles++; }
            }
            Debug.Log($"The Neo Soul Quarter: Honey Street {streetL:0} m, {monsteras} monsteras, {ferns} ferns, {snakes} snake plants, {figs} fig trees, {benches} sofas with lanterns, {candles} clusters of candles");
        }

        Vector3 NearestWalk(Vector3 p) { var best = center; var bd = float.MaxValue; foreach (var (q, _) in walkPoints) { var d = (q - p).sqrMagnitude; if (d < bd) { bd = d; best = q; } } return best; }

        /// Moss and small trailing leaves between the pots.
        void Verge(LandContext ctx)
        {
            var mat = WindMesh.Material(.2f, .7f, new Color(.2f, .4f, .28f));
            var meshes = new Dictionary<(int, int), WindMesh>(); var rng = new System.Random(1997);
            const float st = 2.6f;
            for (var x = ctx.MinX; x < ctx.MaxX; x += st)
            for (var z = ctx.MinZ; z < ctx.MaxZ; z += st)
            {
                var px = x + ((float)rng.NextDouble() - .5f) * st; var pz = z + ((float)rng.NextDouble() - .5f) * st;
                if (!ctx.IsLand(px, pz, 1f) || NearPath(px, pz, 3.6f) || InNoGrass(px, pz, 0f) || InFootprint(ctx, px, pz, -2f)) continue;
                if (Mathf.PerlinNoise(px * .04f + 8f, pz * .04f + 4f) < .4f) continue;
                var key = (Mathf.FloorToInt(px / 70f), Mathf.FloorToInt(pz / 70f));
                if (!meshes.TryGetValue(key, out var wm)) meshes[key] = wm = new WindMesh();
                var col = Color.Lerp(new Color(.16f, .34f, .26f), new Color(.28f, .46f, .3f), (float)rng.NextDouble());
                wm.Tuft(new Vector3(px, ctx.Ground(px, pz) - .04f, pz), .2f + (float)rng.NextDouble() * .3f, col, rng.NextDouble() < .1 ? new Color(1f, .8f, .5f) : Color.Lerp(col, new Color(.7f, .8f, .5f), .3f), rng, 3, .08f);
            }
            foreach (var kv in meshes) kv.Value.Build("Neo Soul moss", ctx.Root, mat);
        }
    }
}
