using System.Collections.Generic;
using UnityEngine;
using static EverythingWorld.World.PlaceTools;

namespace EverythingWorld.World
{
    /// The Roots Bay (WORLD_GUIDE.md): a bright morning after rain. The hills are wet and very green with mist still
    /// lying in them, the red-earth road is full of puddles, and a rainbow stands over the bay. You arrive in the Yard,
    /// under a great spreading tree, among painted board houses. Bay Road runs from it down to the beach, shacks in
    /// red, gold, green and blue on either side, and at the bottom, on the sand, stands the Sound System: speaker
    /// boxes stacked as tall as a house, hand-painted. Easy and uplifting; and proud.
    /// Laid out on purpose (the build-land standard): RootsTown.cs has the places and the people.
    public partial class RootsKit : LandKit
    {
        public override string Land => "The Roots Bay";
        public override Vector3 ArtistFront(IslandWorldData data, IslandWorldData.Artist a) => facing.TryGetValue(a.name, out var f) ? f : base.ArtistFront(data, a);

        public static readonly Color Red = new Color(.82f, .18f, .14f), Gold = new Color(1f, .8f, .2f), Green = new Color(.14f, .6f, .28f), Bay = new Color(.12f, .5f, .62f), Cream = new Color(.97f, .94f, .84f), Ink = new Color(.09f, .08f, .08f), Earth = new Color(.66f, .34f, .2f);
        static readonly Color[] Paints = { new Color(.82f, .18f, .14f), new Color(1f, .8f, .2f), new Color(.14f, .6f, .28f), new Color(.12f, .5f, .62f), new Color(.96f, .93f, .84f), new Color(.95f, .5f, .2f), new Color(.5f, .7f, .85f), new Color(.9f, .5f, .6f) };
        const float PathW = 3f, RoadW = 6f;

        Material earthM, sandM, zinc, rustM, woodM, plank, iron, dark, white, leafM, leafDark, trunkM, puddleM, coneM, fireM;
        readonly Material[] bowM = new Material[6];
        GlowHalos halos; Vector3 center, D, S; RootsLife life;
        readonly Dictionary<(int, int), List<Vector3>> pathCells = new Dictionary<(int, int), List<Vector3>>();
        readonly List<(Vector3 p, int w)> walkPoints = new List<(Vector3, int)>();
        readonly Dictionary<string, Vector3> facing = new Dictionary<string, Vector3>();
        // x, radius, z
        readonly List<Vector3> noGrass = new List<Vector3>();
        Vector2 heart2, D2 = new Vector2(0, 1), S2 = new Vector2(-1, 0), beach2; float plazaY, waterY, roadL;

        void MakeMaterials()
        {
            earthM = WorldMaterials.Solid(new Color(.7f, .38f, .22f)); sandM = WorldMaterials.Solid(new Color(.92f, .84f, .64f)); zinc = WorldMaterials.Solid(new Color(.68f, .7f, .72f), .7f); rustM = WorldMaterials.Solid(new Color(.6f, .32f, .18f));
            woodM = WorldMaterials.Solid(new Color(.5f, .36f, .24f)); plank = WorldMaterials.Solid(new Color(.62f, .5f, .38f)); iron = WorldMaterials.Solid(new Color(.14f, .14f, .16f), .5f); dark = WorldMaterials.Solid(new Color(.07f, .07f, .08f)); white = WorldMaterials.Solid(new Color(.96f, .95f, .9f));
            leafM = WorldMaterials.Solid(new Color(.16f, .56f, .26f)); leafDark = WorldMaterials.Solid(new Color(.1f, .42f, .24f)); trunkM = WorldMaterials.Solid(new Color(.42f, .32f, .24f));
            puddleM = WorldMaterials.Solid(new Color(.62f, .78f, .9f), .95f);
            coneM = new Material(WorldMaterials.Glow(Gold, .6f)) { name = "Roots speaker cones" }; fireM = new Material(WorldMaterials.Glow(new Color(1f, .55f, .12f), 1.8f)) { name = "Roots fires" };
            var bow = new[] { new Color(1f, .2f, .2f), new Color(1f, .6f, .1f), new Color(1f, .9f, .2f), new Color(.2f, .8f, .3f), new Color(.2f, .5f, 1f), new Color(.6f, .3f, .9f) };
            for (var i = 0; i < 6; i++) bowM[i] = new Material(WorldMaterials.Glow(bow[i], .55f)) { name = "Roots rainbow " + i };
        }

        Material GlowM(Color c, float s = 2f) => WorldMaterials.Glow(c, s * .65f);
        Material Paint(Color c) => WorldMaterials.Solid(c);
        static bool HasStation(IslandWorldData.Artist a) => a.stationId != 0;
        static float Foot(IslandWorldData.Artist a)
        {
            switch (a.name)
            {
                case "Bob Marley & The Wailers": return 16f;
                case "Toots & The Maytals": case "The Specials": return 13f;
                case "Peter Tosh": case "Burning Spear": case "Lee \"Scratch\" Perry": case "Jimmy Cliff": case "Desmond Dekker & The Aces": return 10.5f;
                default: return HasStation(a) ? 12f : 9f;
            }
        }

        public override bool Ground(float height, float slope, float patch, out Color color)
        {
            // wet, very green hills; red earth showing through; sand by the water
            if (height < 3.2f) color = Color.Lerp(new Color(.92f, .84f, .64f), new Color(.86f, .76f, .56f), patch);
            else if (slope > 28f) color = Color.Lerp(new Color(.56f, .3f, .2f), new Color(.64f, .36f, .22f), patch);
            else color = patch < .2f ? Color.Lerp(new Color(.66f, .36f, .22f), new Color(.5f, .5f, .26f), patch / .2f) : Color.Lerp(new Color(.22f, .62f, .26f), new Color(.36f, .72f, .3f), Mathf.SmoothStep(0f, 1f, (patch - .2f) / .8f));
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
            // Bay Road runs downhill to the nearest shore that is far enough away to hold a village
            var best = float.MaxValue;
            for (var k = 0; k < 8; k++)
            {
                var dir = new Vector2(Mathf.Sin(k * Mathf.PI / 4f), Mathf.Cos(k * Mathf.PI / 4f)); var len = 60f;
                while (len < 340f && data.IslandAt(heart2.x + dir.x * (len + 34f), heart2.y + dir.y * (len + 34f)) == isl && data.HeightAt(heart2.x + dir.x * len, heart2.y + dir.y * len) > waterY + 3f) len += 6f;
                if (len < 150f || len >= 340f) continue;
                // prefer the bay to the north
                var score = len - Mathf.Max(0f, dir.y) * 60f; if (score < best) { best = score; D2 = dir; roadL = len; }
            }
            if (best == float.MaxValue) { D2 = new Vector2(0, 1); roadL = 60f; while (roadL < 300f && Inside(heart2 + D2 * (roadL + 10f), 30f)) roadL += 10f; }
            S2 = new Vector2(-D2.y, D2.x); beach2 = heart2 + D2 * (roadL + 12f);
            plazaY = Mathf.Max(data.HeightAt(heart2.x, heart2.y), waterY + 6f);

            var keep = new List<string>();
            void Put(string name, float d, float s, float room) { var p = P2(d, s); if (!Inside(p, room)) return; foreach (var a in data.layout.artists) if (a.sceneId == isl.sceneId && a.name == name) { a.x = p.x; a.z = p.y; keep.Add(name); } }
            // the Sound System on the beach; the dance hall and the grey street by the Yard
            Put("Bob Marley & The Wailers", roadL - 12f, -38f, 24f); Put("Toots & The Maytals", -10f, 52f, 28f); Put("The Specials", -14f, -54f, 28f);
            // down Bay Road, either side
            Put("Jimmy Cliff", 60f, 38f, 24f); Put("Desmond Dekker & The Aces", 64f, -38f, 24f);
            if (roadL > 170f) { Put("Peter Tosh", 120f, 40f, 24f); Put("Burning Spear", 122f, -40f, 24f); }
            Put("Lee \"Scratch\" Perry", -70f, 6f, 24f);
            var avoid = new List<Vector3> { new Vector3(beach2.x, 30f, beach2.y) };
            for (var d = 0f; d <= roadL; d += 22f) avoid.Add(new Vector3(heart2.x + D2.x * d, 12f, heart2.y + D2.y * d));
            ArtistPlacement.Spread(data, index, 56f, heart2, 44f, 42f, avoid, keep, Land, 330f);

            Level(data, index, heart2, 22f, 40f, plazaY);
            Level(data, index, beach2, 20f, 46f, waterY + 1.7f);
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
            life = new GameObject("Roots Bay life").AddComponent<RootsLife>();
            life.transform.SetParent(ctx.Root, false);
            life.center = center; life.cones = coneM; life.fire = fireM; life.rainbow = bowM;
            var mk = new MeshKit(); var solid = new MeshKit();
            Remember(ctx.OnGround(center.x, center.z), 0);
            TownStart(ctx);
            Yard(ctx, mk, solid);
            BayRoad(ctx, mk, solid);
            Beach(ctx, mk, solid);
            Paths(ctx, mk, solid);
            Village(ctx, mk, solid);
            Wilds(ctx, mk, solid);
            Rainbow(ctx, mk);
            mk.Build("The Roots Bay", ctx.Root, false);
            solid.BuildCollider("The Roots Bay walls", ctx.Root);
        }

        public override void Finish(LandContext ctx) { Verge(ctx); halos.Build("Roots Bay glow", ctx.Root); }

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

        /// A pole with three little flags in red, gold and green.
        void FlagPole(LandContext ctx, MeshKit mk, Vector3 g, Vector3 fly)
        {
            var up = Vector3.up; fly.y = 0; fly.Normalize(); mk.Prism(woodM, g - up * .3f, up, .06f, 4.6f, 5, .7f); ArtistBits.PostCollider(ctx.Root, g, 4.2f, .08f);
            for (var k = 0; k < 3; k++) { var a = g + up * (4.1f - k * .5f); var m = Paint(new[] { Red, Gold, Green }[k]); mk.Tri(m, a, a - up * .4f, a - up * .2f + fly * .9f, a + Vector3.Cross(up, fly)); mk.Tri(m, a - up * .4f, a, a - up * .2f + fly * .9f, a - Vector3.Cross(up, fly)); }
        }

        /// A painted board house on short stilts with a zinc roof and a little verandah. 'f' sits on the ground at its
        /// middle, facing the way the verandah looks. Returns the verandah floor frame.
        PlaceFrame Shack(LandContext ctx, MeshKit mk, MeshKit solid, PlaceFrame f, float W, float Dp, int i, bool shut = false)
        {
            var up = Vector3.up; const float H = 2.6f, lift = .6f; var wall = Paint(shut ? new Color(.5f, .5f, .52f) : Paints[i % Paints.Length]); var trim = Paint(shut ? new Color(.36f, .36f, .38f) : Paints[(i + 3) % Paints.Length]); var fz = Dp / 2f;
            foreach (var (x, zz) in new[] { (-1f, -1f), (1f, -1f), (1f, 1f), (-1f, 1f) }) mk.Prism(woodM, f.P(x * (W / 2f - .3f), -.4f, zz * (Dp / 2f - .3f)), up, .1f, lift + .5f, 5);
            mk.Box(wall, f.P(0, lift + H / 2f, 0), new Vector3(W, H, Dp), f.r); solid.Box(wall, f.P(0, lift / 2f + H / 2f, 0), new Vector3(W, H + lift, Dp), f.r); for (var k = 1; k < 6; k++) mk.Box(trim, f.P(0, lift + k * H / 6f, fz + .01f), new Vector3(W, .03f, .02f), f.r);
            mk.GableRoof(i % 3 == 0 ? rustM : zinc, f.P(0, lift + H, 0), new Vector3(Dp + .9f, 0, W + .8f), 1.2f, Quaternion.LookRotation(f.Right)); mk.Box(wall, f.P(-W / 2f + .02f, lift + H + .36f, 0), new Vector3(.04f, .7f, Dp * .5f), f.r); mk.Box(wall, f.P(W / 2f - .02f, lift + H + .36f, 0), new Vector3(.04f, .7f, Dp * .5f), f.r);
            const float pd = 1.8f; mk.Box(plank, f.P(0, lift - .06f, fz + pd / 2f), new Vector3(W, .12f, pd), f.r); solid.Box(plank, f.P(0, lift / 2f, fz + pd / 2f), new Vector3(W, lift, pd), f.r); foreach (var k in new[] { 0, 1 }) { mk.Box(plank, f.P(0, lift * (1 - k) / 2f + .08f, fz + pd + .2f + k * .34f), new Vector3(1.4f, lift * (2 - k) / 2f * .5f + .1f, .34f), f.r); solid.Box(plank, f.P(0, lift * (2 - k) / 4f, fz + pd + .2f + k * .34f), new Vector3(1.4f, lift * (2 - k) / 2f, .34f), f.r); }
            mk.Box(i % 3 == 0 ? rustM : zinc, f.P(0, lift + H - .3f, fz + pd / 2f + .1f), new Vector3(W + .4f, .05f, pd + .5f), f.r * Quaternion.Euler(10f, 0, 0)); foreach (var e in new[] { -1f, 1f }) { mk.Prism(trim, f.P(e * (W / 2f - .15f), lift, fz + pd - .15f), up, .06f, H - .5f, 4); ArtistBits.PostCollider(ctx.Root, f.P(e * (W / 2f - .15f), lift, fz + pd - .15f), H - .5f, .08f); mk.Box(trim, f.P(e * (W / 4f + .3f), lift + .8f, fz + pd - .15f), new Vector3(W / 2f - .9f, .05f, .05f), f.r); }
            var dx = (i % 2 == 0 ? -1f : 1f) * W * .22f; mk.Box(shut ? dark : trim, f.P(dx, lift + 1.05f, fz + .02f), new Vector3(.9f, 2.1f, .05f), f.r); var wx = -dx * 1.2f; mk.Box(shut ? (Material)plank : dark, f.P(wx, lift + 1.45f, fz + .02f), new Vector3(.9f, .9f, .05f), f.r); if (!shut) foreach (var e in new[] { -1f, 1f }) mk.Box(trim, f.P(wx + e * .66f, lift + 1.45f, fz + .05f), new Vector3(.42f, .9f, .04f), f.r * Quaternion.Euler(0, e * 24f, 0));
            return new PlaceFrame(f.P(0, lift, fz + pd / 2f), f.Fwd);
        }

        /// A coconut palm.
        void Palm(LandContext ctx, MeshKit mk, Vector3 g, float s, System.Random rng)
        {
            var up = Vector3.up; var lean = Quaternion.Euler(0, (float)rng.NextDouble() * 360f, 0) * Vector3.forward * (.1f + (float)rng.NextDouble() * .22f); var p = g - up * .3f; var d = (up + lean).normalized;
            for (var k = 0; k < 5; k++) { var len = 1.5f * s; mk.Prism(trunkM, p, d, (.2f - k * .02f) * s, len + .05f, 6, .9f); p += d * len; d = (d + lean * .3f).normalized; }
            for (var k = 0; k < 8; k++) { var fd = Quaternion.Euler(0, k * 45f + (float)rng.NextDouble() * 20f, 0) * Vector3.forward; mk.Box(k % 2 == 0 ? leafM : leafDark, p + fd * 1.2f * s - up * .2f * s, new Vector3(.5f * s, .05f, 2.6f * s), Quaternion.LookRotation(fd) * Quaternion.Euler(22f, 0, 0)); }
            ArtistBits.PostCollider(ctx.Root, g, 3f * s, .22f * s);
        }

        /// A broad spreading tree (breadfruit, almond): a thick trunk, wide layers of big leaves.
        void ShadeTree(LandContext ctx, MeshKit mk, Vector3 g, float s, System.Random rng)
        {
            var up = Vector3.up; mk.Prism(trunkM, g - up * .3f, up, .34f * s, 3f * s, 7, .7f); ArtistBits.PostCollider(ctx.Root, g, 2.8f * s, .36f * s);
            for (var k = 0; k < 5; k++) { var d = Quaternion.Euler(0, k * 72f + (float)rng.NextDouble() * 40f, 0) * Vector3.forward; mk.Prism(trunkM, g + up * 2.4f * s, (d + up * .5f).normalized, .12f * s, 2.2f * s, 5, .4f); mk.Blob(k % 2 == 0 ? leafM : leafDark, g + up * (3.6f + (float)rng.NextDouble() * .6f) * s + d * 2.2f * s, new Vector3(2.4f, 1.1f, 2.4f) * s, rng, .25f); }
            mk.Blob(leafM, g + up * 4.6f * s, new Vector3(2.6f, 1.2f, 2.6f) * s, rng, .25f);
        }

        /// A hammock between two posts, to lie in.
        void Hammock(LandContext ctx, MeshKit mk, Vector3 g, Vector3 along, int i)
        {
            var up = Vector3.up; along.y = 0; along.Normalize();
            foreach (var e in new[] { -1f, 1f }) { var p = g + along * e * 1.8f; p.y = ctx0.Ground(p.x, p.z); mk.Prism(trunkM, p - up * .2f, (up + along * e * .12f).normalized, .08f, 2f, 5); ArtistBits.PostCollider(ctx.Root, p, 1.8f, .1f); }
            for (var k = 0; k < 7; k++) { var t = (k - 3) / 3f; mk.Box(Paint(new[] { Red, Gold, Green }[k % 3]), g + along * t * 1.4f + up * (.62f + t * t * .5f), new Vector3(.9f, .04f, .5f), Quaternion.LookRotation(along) * Quaternion.Euler(-t * 26f, 0, 0)); }
            Interact.Seat(tlife.transform, g + up * .7f, Vector3.Cross(up, along), "Lie in the hammock").seatHeight = .55f; noGrass.Add(new Vector3(g.x, 2.2f, g.z));
        }

        /// A stack of hand-painted speaker boxes, 'wide' across and 'high' up, in red, gold and green.
        void SpeakerStack(Transform parent, MeshKit mk, MeshKit solid, Vector3 g, Quaternion rot, int wide, int high, int seed)
        {
            var up = Vector3.up; var f = rot * Vector3.forward; var r = rot * Vector3.right;
            for (var x = 0; x < wide; x++) for (var y = 0; y < high; y++)
            {
                var p = g + r * ((x - (wide - 1) / 2f) * 1.5f) + up * (.7f + y * 1.4f); var c = new[] { Red, Gold, Green, Ink }[(x + y * 2 + seed) % 4]; mk.Box(Paint(c), p, new Vector3(1.46f, 1.36f, 1f), rot); mk.Box(dark, p + f * .5f, new Vector3(1.3f, 1.2f, .02f), rot);
                if ((x + y) % 2 == 0) { mk.Prism(dark, p + f * .5f, f, .5f, .04f, 14); mk.Prism(coneM, p + f * .53f, f, .2f, .03f, 12); } else { foreach (var e in new[] { -.3f, .3f }) { mk.Prism(dark, p + f * .5f + up * e, f, .26f, .04f, 10); mk.Prism(coneM, p + f * .53f + up * e, f, .1f, .03f, 8); } }
            }
            solid.Box(dark, g + up * (high * .7f), new Vector3(wide * 1.5f, high * 1.4f, 1f), rot);
        }

        /// A guitar on a stand.
        void Guitar(MeshKit mk, Vector3 o, Quaternion rot, Material body, System.Random rng, float s = 1f)
        {
            var up = Vector3.up; mk.Prism(iron, o, up, .12f * s, .03f, 6); mk.Blob(body, o + up * .3f * s, new Vector3(.2f, .26f, .06f) * s, rng, .04f); mk.Box(woodM, o + up * .88f * s - rot * Vector3.forward * .1f * s, new Vector3(.05f, .72f, .03f) * s, rot * Quaternion.Euler(-9f, 0, 0)); mk.Box(body, o + up * 1.28f * s - rot * Vector3.forward * .17f * s, new Vector3(.07f, .14f, .03f) * s, rot * Quaternion.Euler(-9f, 0, 0));
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
                Strip(mk, pts, earthM, PathW, .13f, ctx);
                foreach (var p in pts) Remember(p, walk);
                for (var i = 8; i < pts.Count; i += 13, n++)
                {
                    var s = n % 2 == 0 ? 1f : -1f; var lp = pts[i] + side * s * (PathW / 2f + .8f);
                    if (InFootprint(ctx, lp.x, lp.z, 1f) || InNoGrass(lp.x, lp.z, 1f)) continue;
                    if (n % 2 == 0) FlagPole(ctx, mk, ctx.OnGround(lp.x, lp.z), dir); else mk.Prism(puddleM, pts[i] + side * s * .5f + Vector3.up * .02f, Vector3.up, .7f, .012f, 9);
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

        // ---------------------------------------------------------------- the hills
        /// Palms on the shore, shade trees and banana on the hills, hibiscus, hammocks, goats, puddles, a zinc fence.
        void Wilds(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            int palms = 0, trees = 0, bananas = 0, bushes = 0, hammocks = 0, goats = 0, puddles = 0; var rng = new System.Random(1973);
            const float cell = 20f;
            for (var x = ctx.MinX; x < ctx.MaxX; x += cell)
            for (var z = ctx.MinZ; z < ctx.MaxZ; z += cell)
            {
                float px = x + (float)rng.NextDouble() * cell, pz = z + (float)rng.NextDouble() * cell; var roll = (float)rng.NextDouble(); var s = (float)rng.NextDouble();
                if (!Open(ctx, px, pz, 4f)) continue;
                var p = ctx.OnGround(px, pz); var shore = p.y < waterY + 4.5f;
                if (shore) { if (roll < .44f) { Palm(ctx, mk, p, .9f + s * .5f, rng); palms++; } continue; }
                if (roll < .16f) { ShadeTree(ctx, mk, p, .9f + s * .6f, rng); trees++; }
                else if (roll < .26f) { Palm(ctx, mk, p, .9f + s * .4f, rng); palms++; }
                else if (roll < .36f) { Banana(ctx, mk, p, .9f + s * .4f, rng); bananas++; }
                else if (roll < .46f) { mk.Blob(rng.Next(2) == 0 ? leafM : leafDark, p + Vector3.up * .5f, new Vector3(.9f, .6f, .9f), rng, .3f); var fm = Paint(Paints[new[] { 0, 1, 7, 5 }[rng.Next(4)]]); for (var k = 0; k < 5; k++) mk.Blob(fm, p + Vector3.up * (.6f + (float)rng.NextDouble() * .5f) + Quaternion.Euler(0, k * 72f, 0) * Vector3.forward * .7f, Vector3.one * .12f, rng, .3f); bushes++; }
                else if (roll < .5f) { if (hammocks < 7) { Hammock(ctx, mk, p, Quaternion.Euler(0, s * 180f, 0) * Vector3.forward, hammocks); hammocks++; } }
                else if (roll < .53f) { if (goats < 8) { Goat(p, Quaternion.Euler(0, s * 360f, 0), rng, goats); goats++; } }
                else if (roll < .62f) { mk.Prism(puddleM, p + Vector3.up * .05f, Vector3.up, 1f + s * 1.4f, .012f, 9); puddles++; }
            }
            Debug.Log($"The Roots Bay: Bay Road {roadL:0} m, {palms} palms, {trees} shade trees, {bananas} banana clumps, {bushes} hibiscus, {hammocks} hammocks, {goats} goats, {puddles} puddles");
        }

        void Banana(LandContext ctx, MeshKit mk, Vector3 g, float s, System.Random rng)
        {
            var up = Vector3.up; mk.Prism(Paint(new Color(.4f, .56f, .28f)), g - up * .2f, up, .14f * s, 1.8f * s, 6, .8f); ArtistBits.PostCollider(ctx.Root, g, 1.8f * s, .16f * s);
            for (var k = 0; k < 6; k++) { var fd = Quaternion.Euler(0, k * 60f + (float)rng.NextDouble() * 30f, 0) * Vector3.forward; mk.Box(k % 2 == 0 ? leafM : leafDark, g + up * 2f * s + fd * .8f * s, new Vector3(.6f * s, .04f, 1.8f * s), Quaternion.LookRotation(fd) * Quaternion.Euler(-30f + k * 6f, 0, 0)); }
            if (rng.Next(2) == 0) for (var k = 0; k < 5; k++) mk.Blob(Paint(new Color(.9f, .86f, .25f)), g + up * (1.5f - k * .08f) * s + Vector3.right * (.2f + (k % 2) * .08f) * s, new Vector3(.05f, .12f, .05f) * s, rng, 0f);
        }

        /// A goat. Say good morning.
        void Goat(Vector3 g, Quaternion rot, System.Random rng, int i)
        {
            var m = new MeshKit(); var fur = Paint(i % 3 == 0 ? new Color(.92f, .9f, .82f) : i % 3 == 1 ? new Color(.44f, .32f, .24f) : new Color(.2f, .18f, .18f));
            m.Blob(fur, new Vector3(0, .5f, 0), new Vector3(.2f, .2f, .38f), rng, .04f); m.Blob(fur, new Vector3(0, .72f, .42f), new Vector3(.1f, .12f, .16f), rng, 0f); foreach (var e in new[] { -.06f, .06f }) { m.Cone(dark, new Vector3(e, .82f, .38f), .02f, .14f, 4); foreach (var q in new[] { -.24f, .24f }) m.Box(fur, new Vector3(e * 2f, .18f, q), new Vector3(.05f, .36f, .05f), Quaternion.identity); }
            var go = m.BuildSingle("Goat", tlife.transform, false); go.transform.SetPositionAndRotation(g, rot); string[] meh = { "\"Meeh.\" It is eating a hibiscus and has no regrets.", "The goat gives you a long level look, as from a great philosopher.", "It shakes the rain off. Some of it gets you." }; var n = i;
            Interact.Toy(go, Toy.Kind.Custom, "Say good morning to the goat", new Vector3(.7f, 1f, 1.1f)).onUse = _ => Say(tlife.transform, g + Vector3.up * 1.6f, meh[n++ % meh.Length], 4f);
        }

        /// The rainbow over the bay, beyond the beach: six great bands (RootsLife brightens them).
        void Rainbow(LandContext ctx, MeshKit mk)
        {
            var c = center + D * (roadL + 170f); c.y = waterY - 6f;
            for (var b = 0; b < 6; b++) { var r = 150f - b * 5f; var prev = c - S * r; for (var k = 1; k <= 40; k++) { var a = k / 40f * Mathf.PI; var p = c - S * Mathf.Cos(a) * r + Vector3.up * Mathf.Sin(a) * r; mk.Prism(bowM[b], prev, (p - prev).normalized, 2.6f, Vector3.Distance(prev, p) + .4f, 5); prev = p; } }
        }

        /// Wet green grass, thick, with small flowers; nothing on the sand.
        void Verge(LandContext ctx)
        {
            var mat = WindMesh.Material(.3f, .9f, new Color(.22f, .62f, .26f));
            var meshes = new Dictionary<(int, int), WindMesh>(); var rng = new System.Random(1974);
            Color[] flowers = { Gold, new Color(1f, .4f, .4f), Cream, new Color(.9f, .5f, .8f) };
            const float st = 2.4f;
            for (var x = ctx.MinX; x < ctx.MaxX; x += st)
            for (var z = ctx.MinZ; z < ctx.MaxZ; z += st)
            {
                var px = x + ((float)rng.NextDouble() - .5f) * st; var pz = z + ((float)rng.NextDouble() - .5f) * st;
                if (!ctx.IsLand(px, pz, 1f) || NearPath(px, pz, 4f) || InNoGrass(px, pz, 0f) || InFootprint(ctx, px, pz, -2f)) continue;
                var gy = ctx.Ground(px, pz); if (gy < waterY + 3.4f) continue;
                if (Mathf.PerlinNoise(px * .04f + 6f, pz * .04f + 3f) < .3f) continue;
                var key = (Mathf.FloorToInt(px / 70f), Mathf.FloorToInt(pz / 70f));
                if (!meshes.TryGetValue(key, out var wm)) meshes[key] = wm = new WindMesh();
                var col = Color.Lerp(new Color(.16f, .54f, .22f), new Color(.34f, .7f, .28f), (float)rng.NextDouble());
                wm.Tuft(new Vector3(px, gy - .04f, pz), .3f + (float)rng.NextDouble() * .36f, col, rng.NextDouble() < .14 ? flowers[rng.Next(flowers.Length)] : Color.Lerp(col, new Color(.8f, .95f, .6f), .4f), rng, 3, .06f);
            }
            foreach (var kv in meshes) kv.Value.Build("Roots Bay grass", ctx.Root, mat);
        }
    }
}
