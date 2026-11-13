using System.Collections.Generic;
using UnityEngine;
using static EverythingWorld.World.PlaceTools;

namespace EverythingWorld.World
{
    /// The Hip-Hop Boroughs (WORLD_GUIDE.md): a rooftop city on the mesa tops at golden hour. The sun is low and
    /// gold and every shadow is long; the blocks are concrete and red brick with gold in every window; pennants
    /// cross the streets, sneakers hang from the wires, and at the end of Victory Avenue stands the Tower with a
    /// boombox the size of a house on its roof and the Stage at its foot. You arrive on the Block. Big, loud,
    /// triumphant. Laid out on purpose (the build-land standard): HipTown.cs has the places and the people.
    public partial class HipKit : LandKit
    {
        public override string Land => "The Hip-Hop Boroughs";
        public override Vector3 ArtistFront(IslandWorldData data, IslandWorldData.Artist a) => facing.TryGetValue(a.name, out var f) ? f : base.ArtistFront(data, a);

        public static readonly Color Gold = new Color(1f, .76f, .2f), Brick = new Color(.62f, .25f, .18f), Concrete = new Color(.62f, .6f, .56f), Purple = new Color(.3f, .2f, .46f), Cream = new Color(.97f, .93f, .84f), Ink = new Color(.08f, .07f, .09f), Red = new Color(.86f, .16f, .14f), Sky = new Color(.2f, .55f, .9f);
        static readonly Color[] Walls = { new Color(.62f, .25f, .18f), new Color(.64f, .62f, .58f), new Color(.5f, .2f, .16f), new Color(.74f, .68f, .56f), new Color(.44f, .42f, .44f), new Color(.7f, .36f, .22f) };
        static readonly Color[] Tags = { new Color(1f, .76f, .2f), new Color(.9f, .2f, .3f), new Color(.2f, .6f, .95f), new Color(.3f, .8f, .45f), new Color(.95f, .5f, .15f), new Color(.7f, .35f, .9f), new Color(.97f, .93f, .84f) };
        const float PathW = 3f, StreetW = 7f, BlockW = 10f, BlockD = 9f;

        Material asphalt, concrete, concreteDark, brickM, woodM, iron, dark, white, goldM, leafM, leafGold, trunkM, windowM, darkGlass, lampM, coneM, stageM, redM;
        GlowHalos halos; Vector3 center, D, S; HipLife life;
        readonly Dictionary<(int, int), List<Vector3>> pathCells = new Dictionary<(int, int), List<Vector3>>();
        readonly List<(Vector3 p, int w)> walkPoints = new List<(Vector3, int)>();
        readonly Dictionary<string, Vector3> facing = new Dictionary<string, Vector3>();
        // x, radius, z
        readonly List<Vector3> noGrass = new List<Vector3>();
        Vector2 heart2, D2 = new Vector2(0, -1), S2 = new Vector2(1, 0); float plazaY, waterY, streetL, stageY;

        void MakeMaterials()
        {
            asphalt = WorldMaterials.Solid(new Color(.24f, .23f, .26f)); concrete = WorldMaterials.Solid(Concrete); concreteDark = WorldMaterials.Solid(new Color(.42f, .41f, .42f)); brickM = WorldMaterials.Solid(Brick);
            woodM = WorldMaterials.Solid(new Color(.44f, .3f, .2f)); iron = WorldMaterials.Solid(new Color(.14f, .13f, .15f), .5f); dark = WorldMaterials.Solid(new Color(.07f, .06f, .08f)); white = WorldMaterials.Solid(new Color(.95f, .93f, .88f));
            goldM = WorldMaterials.Glow(new Color(1f, .7f, .12f), .22f); leafM = WorldMaterials.Solid(new Color(.36f, .5f, .22f)); leafGold = WorldMaterials.Glow(new Color(.95f, .7f, .2f), .2f); trunkM = WorldMaterials.Solid(new Color(.34f, .26f, .2f)); darkGlass = WorldMaterials.Solid(new Color(.16f, .18f, .24f), .7f); redM = WorldMaterials.Solid(Red);
            windowM = new Material(WorldMaterials.Glow(new Color(1f, .68f, .24f), .8f)) { name = "Hip-Hop windows" }; lampM = new Material(WorldMaterials.Glow(new Color(1f, .82f, .5f), 1.4f)) { name = "Hip-Hop lamps" };
            coneM = new Material(WorldMaterials.Glow(Gold, .9f)) { name = "Hip-Hop boombox cones" }; stageM = new Material(WorldMaterials.Glow(new Color(1f, .8f, .4f), 1.6f)) { name = "Hip-Hop stage lights" };
        }

        Material GlowM(Color c, float s = 2f) => WorldMaterials.Glow(c, s * .65f);
        Material Paint(Color c) => WorldMaterials.Solid(c);
        static bool HasStation(IslandWorldData.Artist a) => a.stationId != 0;
        static bool Uk(string n) => n == "Stormzy" || n == "Little Simz" || n == "Dave" || n == "Dizzee Rascal" || n == "The Streets" || n == "Loyle Carner" || n == "Central Cee" || n == "Bakar";
        static float Foot(IslandWorldData.Artist a)
        {
            switch (a.name)
            {
                case "Kendrick Lamar": return 15f;
                case "Logic": return 13f;
                case "Eminem": case "Kanye West": case "Kid Cudi": return 12f;
                default: return 8.5f;
            }
        }

        public override bool Ground(float height, float slope, float patch, out Color color)
        {
            // mesa rock on the slopes, dry gold scrub and dusty concrete on the tops
            if (slope > 24f) color = Color.Lerp(new Color(.62f, .4f, .28f), new Color(.74f, .5f, .32f), patch);
            else color = patch < .4f ? Color.Lerp(new Color(.6f, .56f, .5f), new Color(.68f, .6f, .46f), patch / .4f) : Color.Lerp(new Color(.72f, .62f, .38f), new Color(.6f, .62f, .36f), Mathf.SmoothStep(0f, 1f, (patch - .4f) / .6f));
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
            // Victory Avenue runs whichever way the land runs longest, and ends at the Stage
            streetL = 0f;
            for (var k = 0; k < 8; k++) { var dir = new Vector2(Mathf.Sin(k * Mathf.PI / 4f), Mathf.Cos(k * Mathf.PI / 4f)); var len = 40f; while (len < 130f && Inside(heart2 + dir * (len + 10f), 60f)) len += 10f; if (len > streetL) { streetL = len; D2 = dir; } }
            S2 = new Vector2(-D2.y, D2.x); plazaY = Mathf.Max(data.HeightAt(heart2.x, heart2.y), waterY + 6f);
            var st = P2(streetL + 14f, 0); stageY = Mathf.Max(data.HeightAt(st.x, st.y), waterY + 4f);

            var keep = new List<string>();
            void Put(string name, float d, float s, float room) { var p = P2(d, s); if (!Inside(p, room)) return; foreach (var a in data.layout.artists) if (a.sceneId == isl.sceneId && a.name == name) { a.x = p.x; a.z = p.y; keep.Add(name); } }
            Put("Kendrick Lamar", -8f, 50f, 28f); Put("Logic", -2f, -52f, 26f); Put("Eminem", 62f, 46f, 24f); Put("Kanye West", 64f, -46f, 24f); Put("Kid Cudi", -52f, -4f, 24f);
            var avoid = new List<Vector3>(); for (var d = 0f; d <= streetL; d += 22f) avoid.Add(new Vector3(heart2.x + D2.x * d, 13f, heart2.y + D2.y * d)); avoid.Add(new Vector3(st.x, 34f, st.y));
            ArtistPlacement.Spread(data, index, 52f, heart2, 44f, 40f, avoid, keep, Land, 330f);
            Level(data, index, heart2, 22f, 40f, plazaY); Level(data, index, st, 30f, 48f, stageY);
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
            life = new GameObject("Hip-Hop life").AddComponent<HipLife>();
            life.transform.SetParent(ctx.Root, false);
            life.center = center; life.cones = coneM; life.windows = windowM; life.lamps = lampM; life.stage = stageM;
            var mk = new MeshKit(); var solid = new MeshKit();
            Remember(ctx.OnGround(center.x, center.z), 0);
            TownStart(ctx);
            TheBlock(ctx, mk, solid);
            VictoryAvenue(ctx, mk, solid);
            TheStage(ctx, mk, solid);
            Paths(ctx, mk, solid);
            Blocks(ctx, mk, solid);
            Wilds(ctx, mk, solid);
            mk.Build("The Hip-Hop Boroughs", ctx.Root, false);
            solid.BuildCollider("The Hip-Hop Boroughs walls", ctx.Root);
        }

        public override void Finish(LandContext ctx) { Verge(ctx); halos.Build("Hip-Hop glow", ctx.Root); }

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

        /// A street lamp: a tall post with an arm and a warm head.
        void StreetLamp(LandContext ctx, MeshKit mk, Vector3 g, Vector3 toward, bool light = false)
        {
            var up = Vector3.up; toward.y = 0; toward.Normalize(); mk.Prism(iron, g - up * .2f, up, .08f, 5.4f, 6, .7f); mk.Prism(iron, g + up * 5.1f, (toward + up * .15f).normalized, .05f, 1.3f, 4); var h = g + up * 5.2f + toward * 1.3f; mk.Box(iron, h, new Vector3(.3f, .12f, .6f), Quaternion.LookRotation(toward)); mk.Box(lampM, h - up * .08f, new Vector3(.24f, .05f, .5f), Quaternion.LookRotation(toward));
            halos.Add(h - up * .2f, 1.4f, new Color(1f, .82f, .5f), .4f); ArtistBits.PostCollider(ctx.Root, g, 5f, .1f); if (light) ArtistBits.PointLight(ctx.Root, h - up * 1.2f, new Color(1f, .82f, .5f), 14f, 1.2f);
        }

        /// A fire hydrant.
        void Hydrant(LandContext ctx, MeshKit mk, Vector3 g, bool gold = false)
        {
            var up = Vector3.up; var m = gold ? goldM : redM; mk.Prism(m, g, up, .13f, .56f, 8); mk.Blob(m, g + up * .6f, new Vector3(.14f, .1f, .14f), new System.Random(1), 0f); mk.Prism(m, g + up * .36f - Vector3.right * .2f, Vector3.right, .06f, .4f, 6); mk.Prism(m, g, up, .17f, .06f, 8); ArtistBits.PostCollider(ctx.Root, g, .7f, .16f);
        }

        /// A wire from a to b, sagging, with a pair of sneakers hanging from it by their laces.
        void Wire(MeshKit mk, Vector3 a, Vector3 b, int sneakers, System.Random rng)
        {
            var len = Vector3.Distance(a, b); const int n = 8; var sag = Mathf.Min(1f, len * .06f); var prev = a; var along = (b - a).normalized;
            for (var i = 1; i <= n; i++) { var t = i / (float)n; var p = Vector3.Lerp(a, b, t) - Vector3.up * Mathf.Sin(t * Mathf.PI) * sag; mk.Prism(dark, prev, (p - prev).normalized, .012f, Vector3.Distance(prev, p), 3); prev = p; }
            for (var k = 0; k < sneakers; k++)
            {
                var t = .25f + (float)rng.NextDouble() * .5f; var p = Vector3.Lerp(a, b, t) - Vector3.up * Mathf.Sin(t * Mathf.PI) * sag; var col = Paint(Tags[rng.Next(Tags.Length)]);
                foreach (var e in new[] { -1f, 1f }) { var drop = .4f + (e > 0 ? .16f : 0f); var sp = p + along * e * .1f - Vector3.up * drop; mk.Prism(white, p, (sp - p).normalized, .006f, drop, 3); var r = Quaternion.LookRotation(Vector3.down, along) * Quaternion.Euler(0, e * 14f, 0); mk.Box(col, sp - Vector3.up * .14f, new Vector3(.1f, .11f, .3f), r); mk.Box(white, sp - Vector3.up * .14f + r * Vector3.down * .065f, new Vector3(.11f, .03f, .31f), r); }
            }
        }

        /// A string of gold pennants from a to b.
        void Pennants(MeshKit mk, Vector3 a, Vector3 b, int phase)
        {
            var len = Vector3.Distance(a, b); var n = Mathf.Max(4, Mathf.RoundToInt(len / .9f)); var sag = Mathf.Min(1f, len * .06f); var prev = a; var along = (b - a).normalized; var face = Vector3.Cross(Vector3.up, along);
            for (var i = 1; i <= n; i++)
            {
                var t = i / (float)n; var p = Vector3.Lerp(a, b, t) - Vector3.up * Mathf.Sin(t * Mathf.PI) * sag; mk.Prism(dark, prev, (p - prev).normalized, .01f, Vector3.Distance(prev, p), 3);
                var m = (i + phase) % 3 == 0 ? goldM : (i + phase) % 3 == 1 ? Paint(Cream) : Paint(Purple); var mid = (prev + p) / 2f; mk.Tri(m, mid - along * .3f, mid + along * .3f, mid - Vector3.up * .6f, mid + face); mk.Tri(m, mid - along * .3f, mid + along * .3f, mid - Vector3.up * .6f, mid - face); prev = p;
            }
        }

        /// A street tree in a concrete planter, its leaves gone gold in the low sun.
        void StreetTree(LandContext ctx, MeshKit mk, Vector3 g, float s, System.Random rng)
        {
            var up = Vector3.up; mk.Box(concrete, g + up * .25f, new Vector3(1.6f, .5f, 1.6f) * s, Quaternion.identity); mk.Prism(trunkM, g + up * .4f, up, .16f * s, 3.4f * s, 6, .6f); BoxCol(ctx.Root, g + up * .3f, new Vector3(1.6f, .6f, 1.6f) * s, Quaternion.identity); ArtistBits.PostCollider(ctx.Root, g, 3f * s, .2f * s);
            for (var k = 0; k < 5; k++) mk.Blob(k % 2 == 0 ? leafGold : leafM, g + up * (3.6f + (float)rng.NextDouble() * 1.6f) * s + Quaternion.Euler(0, k * 72f, 0) * Vector3.forward * (k == 0 ? 0f : 1.1f * s), new Vector3(1.5f, 1.1f, 1.5f) * s, rng, .25f);
            noGrass.Add(new Vector3(g.x, 1.4f * s, g.z));
        }

        /// A bench to sit on.
        void Bench(Transform parent, MeshKit mk, Vector3 g, Vector3 face, float len = 2f)
        {
            var up = Vector3.up; var rot = Quaternion.LookRotation(face); var side = Vector3.Cross(up, face); for (var k = 0; k < 3; k++) mk.Box(woodM, g + up * .46f + face * (-.16f + k * .16f), new Vector3(len, .05f, .13f), rot); for (var k = 0; k < 2; k++) mk.Box(woodM, g + up * (.7f + k * .2f) - face * .26f, new Vector3(len, .13f, .05f), rot); foreach (var e in new[] { -1f, 1f }) mk.Box(iron, g + up * .3f + side * e * (len / 2f - .15f), new Vector3(.06f, .6f, .5f), rot);
            BoxCol(parent, g + up * .45f, new Vector3(len, .9f, .5f), rot); Interact.Bench(tlife.transform, g + up * .52f - side * (len / 2f - .5f), g + up * .52f + side * (len / 2f - .5f), face);
        }

        /// A parked car: long, low, two-tone. Gold rims.
        void Car(Transform parent, MeshKit mk, Vector3 g, Quaternion rot, Color body, bool low = true)
        {
            var up = Vector3.up; var m = Paint(body); var y = low ? .34f : .5f; mk.Box(m, g + up * (y + .22f), new Vector3(1.8f, .5f, 4.8f), rot); mk.Box(Paint(Cream), g + up * (y + .64f) - rot * Vector3.forward * .3f, new Vector3(1.6f, .44f, 2.2f), rot); mk.Box(darkGlass, g + up * (y + .64f) - rot * Vector3.forward * .3f, new Vector3(1.64f, .34f, 2f), rot); mk.Box(goldM, g + up * (y + .06f), new Vector3(1.86f, .06f, 4.86f), rot);
            foreach (var (x, zz) in new[] { (-.9f, 1.5f), (.9f, 1.5f), (.9f, -1.5f), (-.9f, -1.5f) }) { var w = g + up * .3f + rot * new Vector3(x, 0, zz); mk.Prism(dark, w - rot * Vector3.right * .1f, rot * Vector3.right, .3f, .2f, 12); mk.Prism(goldM, w + rot * Vector3.right * Mathf.Sign(x) * .11f - rot * Vector3.right * .01f, rot * Vector3.right, .16f, .02f, 8); }
            foreach (var e in new[] { -1f, 1f }) { mk.Blob(lampM, g + up * (y + .26f) + rot * new Vector3(e * .6f, 0, 2.41f), new Vector3(.14f, .09f, .03f), new System.Random(2), 0f); mk.Box(redM, g + up * (y + .26f) + rot * new Vector3(e * .6f, 0, -2.41f), new Vector3(.3f, .12f, .03f), rot); }
            BoxCol(parent, g + up * .7f, new Vector3(1.9f, 1.4f, 4.9f), rot);
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
                    var dd = Mathf.Min(d, end); var q = from + dir * dd + side * Mathf.Sin(dd / 14f) * Mathf.Min(1.4f, end * .03f);
                    if (!ctx.IsLand(q.x, q.z, 0f)) break;
                    pts.Add(ctx.OnGround(q.x, q.z)); if (dd >= end) break;
                }
                if (pts.Count < 2) return;
                Strip(mk, pts, concreteDark, PathW, .13f, ctx);
                foreach (var p in pts) Remember(p, walk);
                Vector3 lastPost = default; var have = false;
                for (var i = 6; i < pts.Count; i += 10, n++)
                {
                    var s = n % 2 == 0 ? 1f : -1f; var lp = pts[i] + side * s * (PathW / 2f + .7f);
                    if (InFootprint(ctx, lp.x, lp.z, 1f) || InNoGrass(lp.x, lp.z, 1f)) continue;
                    var g = ctx.OnGround(lp.x, lp.z); StreetLamp(ctx, mk, g, -side * s, n % 2 == 0);
                    if (have && Vector3.Distance(lastPost, g) < 22f) Wire(mk, lastPost + Vector3.up * 4.6f, g + Vector3.up * 4.6f, n % 3 == 0 ? 1 : 0, rng);
                    lastPost = g; have = true;
                    if (n % 4 == 1) { var hp = pts[i] - side * s * (PathW / 2f + .6f); if (!InFootprint(ctx, hp.x, hp.z, 1f)) Hydrant(ctx, mk, ctx.OnGround(hp.x, hp.z), n % 8 == 1); }
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

        // ---------------------------------------------------------------- the rest of the city
        /// Blocks, billboards, street trees, parked cars, benches under a lamp, hydrants, stacks of crates.
        void Wilds(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            int blocks = 0, boards = 0, trees = 0, cars = 0, benches = 0, crates = 0; var rng = new System.Random(1988);
            const float cell = 22f;
            for (var x = ctx.MinX; x < ctx.MaxX; x += cell)
            for (var z = ctx.MinZ; z < ctx.MaxZ; z += cell)
            {
                float px = x + (float)rng.NextDouble() * cell, pz = z + (float)rng.NextDouble() * cell; var roll = (float)rng.NextDouble(); var s = (float)rng.NextDouble();
                if (!Open(ctx, px, pz, 4f)) continue;
                var p = ctx.OnGround(px, pz); var f = FaceTo(p, NearestWalk(p));
                if (roll < .2f) { if (!Open(ctx, px, pz, 8f) || !ctx.IsLand(px, pz, 9f) || Mathf.Abs(ctx.Ground(px + 5f, pz) - ctx.Ground(px - 5f, pz)) > 2.2f || Mathf.Abs(ctx.Ground(px, pz + 5f) - ctx.Ground(px, pz - 5f)) > 2.2f) continue; Block(ctx, mk, solid, new PlaceFrame(p, f), blocks + 40, rng, 8f, 7f); blocks++; }
                else if (roll < .27f) { Billboard(ctx, mk, p, f, boards, rng); boards++; }
                else if (roll < .47f) { StreetTree(ctx, mk, p, .9f + s * .5f, rng); trees++; }
                else if (roll < .53f) { Car(ctx.Root, mk, p + Vector3.up * .02f, Quaternion.LookRotation(Vector3.Cross(Vector3.up, f)), Tags[cars % Tags.Length], cars % 2 == 0); noGrass.Add(new Vector3(p.x, 3f, p.z)); cars++; }
                else if (roll < .61f) { Bench(ctx.Root, mk, p, f); StreetLamp(ctx, mk, p - f * 1f + Vector3.Cross(Vector3.up, f) * 1.5f, f, benches % 2 == 0); noGrass.Add(new Vector3(p.x, 2f, p.z)); benches++; }
                else if (roll < .68f) { for (var k = 0; k < 4; k++) { var cp = p + new Vector3((k % 2) * .66f, (k / 2) * .5f, (k % 3) * .2f); mk.Box(Paint(Tags[(k + crates) % Tags.Length]), cp + Vector3.up * .25f, new Vector3(.6f, .46f, .44f), Quaternion.Euler(0, k * 9f, 0)); } BoxCol(ctx.Root, p + new Vector3(.33f, .5f, .2f), new Vector3(1.4f, 1f, .9f), Quaternion.identity); crates++; }
            }
            Debug.Log($"The Hip-Hop Boroughs: Victory Avenue {streetL:0} m, {blocks} blocks, {boards} billboards, {trees} street trees, {cars} cars, {benches} benches, {crates} stacks of crates");
        }

        /// A billboard on two legs: a block of colour, a gold bar, a word.
        void Billboard(LandContext ctx, MeshKit mk, Vector3 g, Vector3 face, int i, System.Random rng)
        {
            var up = Vector3.up; var rot = Quaternion.LookRotation(face); var side = Vector3.Cross(up, face); string[] words = { "LOUDER", "YOU MADE IT", "TURN IT UP", "WE UP", "ALL GOLD", "NO SLEEP", "BARS", "ENCORE" };
            foreach (var e in new[] { -2f, 2f }) { mk.Prism(iron, g + side * e - up * .3f, up, .12f, 5.4f, 5); ArtistBits.PostCollider(ctx.Root, g + side * e, 5f, .14f); }
            mk.Box(dark, g + up * 6.4f, new Vector3(6.6f, 3.2f, .16f), rot); mk.Box(Paint(Tags[(i * 3 + 1) % Tags.Length]), g + up * 6.4f + face * .1f, new Vector3(6.2f, 2.8f, .04f), rot); mk.Box(goldM, g + up * 5.36f + face * .14f, new Vector3(6.2f, .3f, .03f), rot);
            Sign(g + up * 6.6f + face * .16f, face, words[i % words.Length], .7f, i % 2 == 0 ? Ink : Cream); mk.Box(lampM, g + up * 8.1f + face * .5f, new Vector3(5f, .08f, .14f), rot);
        }

        Vector3 NearestWalk(Vector3 p) { var best = center; var bd = float.MaxValue; foreach (var (q, _) in walkPoints) { var d = (q - p).sqrMagnitude; if (d < bd) { bd = d; best = q; } } return best; }

        /// Dry gold scrub between the blocks.
        void Verge(LandContext ctx)
        {
            var mat = WindMesh.Material(.3f, .8f, new Color(.7f, .6f, .34f));
            var meshes = new Dictionary<(int, int), WindMesh>(); var rng = new System.Random(1989);
            const float st = 2.8f;
            for (var x = ctx.MinX; x < ctx.MaxX; x += st)
            for (var z = ctx.MinZ; z < ctx.MaxZ; z += st)
            {
                var px = x + ((float)rng.NextDouble() - .5f) * st; var pz = z + ((float)rng.NextDouble() - .5f) * st;
                if (!ctx.IsLand(px, pz, 1f) || NearPath(px, pz, 4.4f) || InNoGrass(px, pz, 0f) || InFootprint(ctx, px, pz, -2f)) continue;
                if (Mathf.PerlinNoise(px * .04f + 8f, pz * .04f + 4f) < .46f) continue;
                var key = (Mathf.FloorToInt(px / 70f), Mathf.FloorToInt(pz / 70f));
                if (!meshes.TryGetValue(key, out var wm)) meshes[key] = wm = new WindMesh();
                var col = Color.Lerp(new Color(.66f, .56f, .3f), new Color(.8f, .68f, .34f), (float)rng.NextDouble());
                wm.Tuft(new Vector3(px, ctx.Ground(px, pz) - .04f, pz), .26f + (float)rng.NextDouble() * .36f, col, Color.Lerp(col, new Color(1f, .9f, .5f), .4f), rng, 3, .08f);
            }
            foreach (var kv in meshes) kv.Value.Build("Hip-Hop scrub", ctx.Root, mat);
        }
    }
}
