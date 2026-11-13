using System.Collections.Generic;
using UnityEngine;
using static EverythingWorld.World.PlaceTools;

namespace EverythingWorld.World
{
    /// The Delta Crossroads (WORLD_GUIDE.md): flat cotton country on a hot, white, dusty afternoon, with a storm
    /// stacked up dark on the horizon that never quite arrives. Raw and haunted. You arrive where two dirt roads
    /// cross, under a four-armed signpost. The Highway runs north over the railroad and south into the fields; the
    /// cross road runs east down to the river landing, where an old steamboat lies aground, and west past the
    /// graveyard. On one corner is the juke joint, tin-roofed, a blue neon guitar over its door. Telegraph poles
    /// follow the Highway. Out in the cotton: scarecrows, bottle trees hung with blue glass, empty shacks.
    /// When Robert Johnson plays, the afternoon goes dark, and something stands up at the crossroads.
    /// Laid out on purpose (the build-land standard): DeltaTown.cs has the places and the people.
    public partial class DeltaKit : LandKit
    {
        public override string Land => "The Delta Crossroads";
        public override Vector3 ArtistFront(IslandWorldData data, IslandWorldData.Artist a) => facing.TryGetValue(a.name, out var f) ? f : base.ArtistFront(data, a);

        public static readonly Color Blues = new Color(.22f, .42f, 1f), Bulb = new Color(1f, .82f, .5f), Dust = new Color(.85f, .72f, .48f), Cream = new Color(.93f, .9f, .8f), Ink = new Color(.1f, .09f, .08f), Rust = new Color(.58f, .3f, .16f);
        static readonly Color[] Planks = { new Color(.52f, .46f, .38f), new Color(.44f, .38f, .32f), new Color(.6f, .54f, .46f), new Color(.38f, .33f, .29f) };
        const float PathW = 3f, RoadW = 6f, RailOff = 105f;

        Material dustM, plank, plankDark, tin, rustM, iron, dark, white, cottonM, leafM, leafDry, trunkM, mossM, steel, bulbM, neonM, bottleM, eyesM, stormM, shadowM;
        GlowHalos halos; Vector3 center, U, R; DeltaLife life;
        readonly Dictionary<(int, int), List<Vector3>> pathCells = new Dictionary<(int, int), List<Vector3>>();
        readonly List<(Vector3 p, int w)> walkPoints = new List<(Vector3, int)>();
        readonly Dictionary<string, Vector3> facing = new Dictionary<string, Vector3>();
        // x, radius, z
        readonly List<Vector3> noGrass = new List<Vector3>();
        Vector2 heart2, U2 = new Vector2(0, 1), R2 = new Vector2(1, 0), landing2, grave2, store2; float plainY, waterY, northL, southL, westL, eastL, railW, railE;

        void MakeMaterials()
        {
            dustM = WorldMaterials.Solid(new Color(.86f, .74f, .52f)); plank = WorldMaterials.Solid(Planks[0]); plankDark = WorldMaterials.Solid(Planks[3]);
            tin = WorldMaterials.Solid(new Color(.66f, .67f, .66f), .7f); rustM = WorldMaterials.Solid(Rust); iron = WorldMaterials.Solid(new Color(.16f, .15f, .15f), .5f); dark = WorldMaterials.Solid(new Color(.07f, .06f, .06f));
            white = WorldMaterials.Solid(new Color(.93f, .91f, .84f)); cottonM = WorldMaterials.Solid(new Color(.97f, .96f, .92f));
            leafM = WorldMaterials.Solid(new Color(.4f, .46f, .24f)); leafDry = WorldMaterials.Solid(new Color(.56f, .5f, .26f)); trunkM = WorldMaterials.Solid(new Color(.34f, .27f, .2f)); mossM = WorldMaterials.Solid(new Color(.52f, .56f, .44f));
            steel = WorldMaterials.Solid(new Color(.4f, .4f, .42f), .8f);
            bulbM = new Material(WorldMaterials.Glow(Bulb, 2f)) { name = "Delta bulbs" };
            neonM = new Material(WorldMaterials.Glow(Blues, 2.4f)) { name = "Delta neon" };
            bottleM = new Material(WorldMaterials.Glow(new Color(.2f, .4f, 1f), .9f)) { name = "Delta bottles" };
            eyesM = new Material(WorldMaterials.Glow(new Color(1f, .15f, .08f), 0f)) { name = "Delta eyes" };
            stormM = new Material(WorldMaterials.Glow(new Color(.16f, .19f, .3f), .35f)) { name = "Delta storm" };
            shadowM = WorldMaterials.Solid(new Color(.02f, .02f, .03f));
        }

        Material GlowM(Color c, float s = 2f) => WorldMaterials.Glow(c, s * .65f);
        static bool HasStation(IslandWorldData.Artist a) => a.stationId != 0;
        static float Foot(IslandWorldData.Artist a)
        {
            switch (a.name)
            {
                case "Muddy Waters": case "Howlin' Wolf": return 14f;
                case "Robert Johnson": return 11f;
                case "B.B. King": return 12f;
                case "John Lee Hooker": case "Etta James": case "Albert King": return 10f;
                default: return HasStation(a) ? 12f : 9f;
            }
        }

        public override bool Ground(float height, float slope, float patch, out Color color)
        {
            // pale dust and dry grass under a white sky
            if (slope > 26f) color = Color.Lerp(new Color(.6f, .46f, .32f), new Color(.7f, .56f, .38f), patch);
            else color = Color.Lerp(new Color(.8f, .68f, .44f), new Color(.7f, .66f, .38f), Mathf.SmoothStep(0f, 1f, patch));
            color.a = 1f;
            return true;
        }

        static float Ease(float a, float b, float x) { var t = Mathf.Clamp01((x - a) / (b - a)); return t * t * (3f - 2f * t); }
        Vector2 P2(float u, float r) => heart2 + U2 * u + R2 * r;
        Vector3 W(float u, float r) => center + U * u + R * r;
        Vector3 G(LandContext ctx, float u, float r) { var p = W(u, r); return ctx.OnGround(p.x, p.z); }

        // ---------------------------------------------------------------- shaping the ground
        public override void ShapeGround(IslandWorldData data, int index)
        {
            var isl = data.layout.islands[index]; heart2 = new Vector2(isl.labelX, isl.labelZ); waterY = data.layout.waterLevel;
            bool Inside(Vector2 p, float m) { if (data.IslandAt(p.x, p.y) != isl) return false; for (var k = 0; k < 12; k++) { var a = k * Mathf.PI / 6f; if (data.IslandAt(p.x + Mathf.Cos(a) * m, p.y + Mathf.Sin(a) * m) != isl) return false; } return true; }
            float Run(Vector2 dir, float max, float room) { var d = 30f; while (d < max && Inside(heart2 + dir * (d + 10f), room)) d += 10f; return d; }
            northL = Run(U2, 170f, 24f); southL = Run(-U2, 210f, 24f); westL = Run(-R2, 210f, 24f);
            // east, the cross road runs down to the water: find the shore
            eastL = 40f; while (eastL < 320f && data.IslandAt(heart2.x + eastL + 8f, heart2.y) == isl) eastL += 8f;
            landing2 = heart2 + R2 * (eastL - 14f);
            railW = 30f; while (railW < 210f && Inside(P2(RailOff, -(railW + 10f)), 20f)) railW += 10f;
            railE = 30f; while (railE < 170f && Inside(P2(RailOff, railE + 10f), 20f)) railE += 10f;
            plainY = Mathf.Max(data.HeightAt(heart2.x, heart2.y), waterY + 6f);
            grave2 = P2(-62f, -128f); store2 = P2(22f, 96f);

            var keep = new List<string>();
            void Put(string name, float u, float rr, float room) { var p = P2(u, rr); if (!Inside(p, room)) return; foreach (var a in data.layout.artists) if (a.sceneId == isl.sceneId && a.name == name) { a.x = p.x; a.z = p.y; keep.Add(name); } }
            // the four corners of the crossroads
            Put("Robert Johnson", -19f, -19f, 16f); Put("Muddy Waters", 36f, 40f, 28f); Put("John Lee Hooker", -42f, 42f, 26f); Put("B.B. King", 42f, -72f, 28f);
            // up at the railroad
            Put("Howlin' Wolf", RailOff - 13f, -48f, 26f); Put("Sister Rosetta Tharpe", RailOff + 13f, 58f, 24f); Put("Gary Clark Jr.", 152f, 36f, 24f); Put("Lead Belly", 152f, -96f, 26f);
            // south down the Highway, and west along the cross road
            Put("Etta James", -84f, -52f, 26f); Put("Albert King", -92f, 46f, 26f); Put("Big Mama Thornton", -140f, 34f, 24f); Put("Fats Domino", -152f, -72f, 26f);
            Put("Stevie Ray Vaughan", -16f, -112f, 24f); Put("R.L. Burnside", 62f, -142f, 24f); Put("Junior Kimbrough", 14f, -172f, 24f);
            var avoid = new List<Vector3> { new Vector3(grave2.x, 22f, grave2.y), new Vector3(store2.x, 18f, store2.y), new Vector3(landing2.x, 26f, landing2.y) };
            for (var d = -railW; d <= railE; d += 24f) avoid.Add(new Vector3(heart2.x + d, 14f, heart2.y + RailOff));
            for (var d = -southL; d <= northL; d += 24f) avoid.Add(new Vector3(heart2.x, 12f, heart2.y + d));
            for (var d = -westL; d <= eastL; d += 24f) avoid.Add(new Vector3(heart2.x + d, 12f, heart2.y));
            ArtistPlacement.Spread(data, index, 56f, heart2, 44f, 42f, avoid, keep, Land, 330f);

            // one wide flat plain round the crossroads; the landing levelled just above the water
            Level(data, index, heart2, 115f, 235f, plainY);
            Level(data, index, landing2, 12f, 34f, waterY + 1.7f);
            foreach (var a in data.layout.artists)
            {
                if (a.sceneId != isl.sceneId) continue;
                a.r = Mathf.Max(a.r, Foot(a) + 6f);
                Level(data, index, new Vector2(a.x, a.z), Foot(a) + 1f, Foot(a) + 10f, Mathf.Max(data.HeightAt(a.x, a.z), waterY + 2.6f));
            }
            Level(data, index, grave2, 13f, 24f, Mathf.Max(data.HeightAt(grave2.x, grave2.y), waterY + 2.6f)); Level(data, index, store2, 10f, 20f, Mathf.Max(data.HeightAt(store2.x, store2.y), waterY + 2.6f));
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
            center = ctx.Plaza; U = new Vector3(0, 0, 1); R = new Vector3(1, 0, 0);
            life = new GameObject("Delta life").AddComponent<DeltaLife>();
            life.transform.SetParent(ctx.Root, false);
            life.center = center; life.bulbs = bulbM; life.neon = neonM; life.bottles = bottleM; life.eyes = eyesM; life.storm = stormM;
            var mk = new MeshKit(); var solid = new MeshKit();
            Remember(ctx.OnGround(center.x, center.z), 0);
            TownStart(ctx);
            Roads(ctx, mk, solid);
            Crossroads(ctx, mk, solid);
            Railroad(ctx, mk, solid);
            Landing(ctx, mk, solid);
            Graveyard(ctx, mk, solid);
            Store(ctx, mk, solid);
            Paths(ctx, mk, solid);
            Wilds(ctx, mk, solid);
            StormBank(ctx, mk);
            mk.Build("The Delta Crossroads", ctx.Root, false);
            solid.BuildCollider("The Delta Crossroads walls", ctx.Root);
        }

        public override void Finish(LandContext ctx) { Verge(ctx); halos.Build("Delta glow", ctx.Root); }

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

        /// On the railroad's bed, plus 'pad'.
        bool OnRail(float x, float z, float pad) => Mathf.Abs(z - (center.z + RailOff)) < 3f + pad && x - center.x > -railW - 4f - pad && x - center.x < railE + 4f + pad;

        bool Open(LandContext ctx, float x, float z, float pad) =>
            ctx.IsLand(x, z, 5f) && !OnRail(x, z, pad) && !InFootprint(ctx, x, z, pad) && !NearPath(x, z, pad + 2f) && !InNoGrass(x, z, pad);

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

        /// A bare bulb on a leaning pole with a tin shade: the Delta's only kind of lamp.
        void BulbPole(LandContext ctx, MeshKit mk, Vector3 g, Vector3 arm, bool light = false)
        {
            var up = Vector3.up; arm.y = 0; arm = arm.sqrMagnitude > .01f ? arm.normalized : Vector3.forward; var lean = (up + arm * .05f).normalized;
            mk.Prism(trunkM, g - up * .3f, lean, .09f, 4.4f, 5, .8f); mk.Prism(iron, g + lean * 3.9f, (arm + up * .15f).normalized, .025f, .9f, 4);
            var lp = g + lean * 3.9f + arm * .85f; mk.Cone(tin, lp + up * .04f, .3f, .16f, 8); mk.Blob(bulbM, lp - up * .1f, new Vector3(.08f, .11f, .08f), new System.Random(5), 0f); halos.Add(lp - up * .1f, 1.1f, Bulb, .45f); ArtistBits.PostCollider(ctx.Root, g, 4f, .12f);
            if (light) ArtistBits.PointLight(ctx.Root, lp - up * .5f, Bulb, 12f, 1.4f);
        }

        /// A shack on short piers: plank walls, a tin gable roof, a porch under a lean-to with posts and a step, a door,
        /// a window. 'f' sits on the ground at its middle, facing the way the porch looks. Returns the porch floor frame.
        PlaceFrame Shack(LandContext ctx, MeshKit mk, MeshKit solid, PlaceFrame f, float W, float D, int i, bool rusty = false, bool lit = false)
        {
            var up = Vector3.up; const float H = 2.7f, lift = .5f; var wall = WorldMaterials.Solid(Planks[i % Planks.Length]); var roof = rusty ? rustM : tin; var fz = D / 2f;
            foreach (var (x, zz) in new[] { (-1f, -1f), (1f, -1f), (1f, 1f), (-1f, 1f) }) mk.Box(plankDark, f.P(x * (W / 2f - .3f), lift / 2f, zz * (D / 2f - .3f)), new Vector3(.4f, lift + .6f, .4f), f.r);
            mk.Box(wall, f.P(0, lift + H / 2f, 0), new Vector3(W, H, D), f.r); solid.Box(wall, f.P(0, lift / 2f + H / 2f, 0), new Vector3(W, H + lift, D), f.r);
            mk.GableRoof(roof, f.P(0, lift + H, 0), new Vector3(D + .8f, 0, W + .7f), 1.3f, Quaternion.LookRotation(f.Right)); mk.Box(wall, f.P(-W / 2f + .02f, lift + H + .4f, 0), new Vector3(.04f, .8f, D * .5f), f.r); mk.Box(wall, f.P(W / 2f - .02f, lift + H + .4f, 0), new Vector3(.04f, .8f, D * .5f), f.r);
            // the porch
            const float pd = 2.2f; mk.Box(plankDark, f.P(0, lift - .06f, fz + pd / 2f), new Vector3(W, .12f, pd), f.r); solid.Box(plankDark, f.P(0, lift / 2f, fz + pd / 2f), new Vector3(W, lift, pd), f.r); mk.Box(plankDark, f.P(0, lift / 2f - .06f, fz + pd + .3f), new Vector3(1.6f, lift / 2f, .6f), f.r); solid.Box(plankDark, f.P(0, lift / 4f, fz + pd + .3f), new Vector3(1.6f, lift / 2f, .6f), f.r);
            mk.Box(roof, f.P(0, lift + H - .28f, fz + pd / 2f + .1f), new Vector3(W + .5f, .06f, pd + .5f), f.r * Quaternion.Euler(9f, 0, 0)); foreach (var e in new[] { -1f, 1f }) { mk.Prism(plankDark, f.P(e * (W / 2f - .2f), lift, fz + pd - .2f), up, .07f, H - .5f, 4); ArtistBits.PostCollider(ctx.Root, f.P(e * (W / 2f - .2f), lift, fz + pd - .2f), H - .5f, .1f); }
            var dx = (i % 2 == 0 ? -1f : 1f) * W * .2f; mk.Box(dark, f.P(dx, lift + 1.05f, fz + .02f), new Vector3(.95f, 2.1f, .05f), f.r);
            mk.Box(lit ? (Material)bulbM : WorldMaterials.Solid(new Color(.16f, .17f, .2f), .8f), f.P(-dx * 1.3f, lift + 1.5f, fz + .02f), new Vector3(.9f, .9f, .05f), f.r); mk.Box(plankDark, f.P(-dx * 1.3f, lift + 1.5f, fz + .04f), new Vector3(.06f, .9f, .05f), f.r); mk.Box(plankDark, f.P(-dx * 1.3f, lift + 1.5f, fz + .04f), new Vector3(.9f, .06f, .05f), f.r);
            return new PlaceFrame(f.P(0, lift, fz + pd / 2f), f.Fwd);
        }

        /// A rocking chair to sit in.
        void Rocker(MeshKit mk, Vector3 g, Vector3 face)
        {
            var up = Vector3.up; var rot = Quaternion.LookRotation(face); var side = Vector3.Cross(up, face);
            mk.Box(plank, g + up * .42f, new Vector3(.5f, .05f, .5f), rot); mk.Box(plank, g + up * .85f - face * .24f, new Vector3(.5f, .85f, .05f), rot * Quaternion.Euler(-8f, 0, 0)); foreach (var e in new[] { -1f, 1f }) { mk.Box(plankDark, g + up * .04f + side * e * .24f, new Vector3(.05f, .05f, .9f), rot); mk.Box(plankDark, g + up * .22f + side * e * .24f + face * .2f, new Vector3(.04f, .4f, .04f), rot); mk.Box(plankDark, g + up * .6f + side * e * .27f, new Vector3(.04f, .04f, .5f), rot); }
            Interact.Seat(tlife.transform, g + up * .48f, face, "Sit in the rocking chair");
        }

        /// A guitar leaning on something: 'o' is its foot.
        void Guitar(MeshKit mk, Vector3 o, Quaternion rot, Material body, System.Random rng, float s = 1f)
        {
            var up = Vector3.up; mk.Blob(body, o + up * .28f * s, new Vector3(.2f, .26f, .06f) * s, rng, .04f); mk.Blob(dark, o + up * .3f * s + rot * Vector3.forward * .05f * s, new Vector3(.05f, .05f, .02f) * s, rng, 0f); mk.Box(plankDark, o + up * .86f * s - rot * Vector3.forward * .1f * s, new Vector3(.05f, .72f, .03f) * s, rot * Quaternion.Euler(-9f, 0, 0)); mk.Box(body, o + up * 1.26f * s - rot * Vector3.forward * .17f * s, new Vector3(.07f, .14f, .03f) * s, rot * Quaternion.Euler(-9f, 0, 0));
        }

        /// A dead tree, bleached and bare.
        void DeadTree(LandContext ctx, MeshKit mk, Vector3 g, float s, System.Random rng, Material m = null)
        {
            var up = Vector3.up; m = m ?? WorldMaterials.Solid(new Color(.6f, .56f, .48f)); mk.Prism(m, g - up * .3f, up, .2f * s, 3f * s, 6, .6f); ArtistBits.PostCollider(ctx.Root, g, 2.8f * s, .22f * s);
            for (var k = 0; k < 4; k++) { var d = (Quaternion.Euler(0, k * 90f + (float)rng.NextDouble() * 60f, 0) * Vector3.forward + up * (.6f + (float)rng.NextDouble() * .7f)).normalized; var b = g + up * (2f + k * .25f) * s; var len = (1.6f + (float)rng.NextDouble() * 1.3f) * s; mk.Prism(m, b, d, .07f * s, len, 4, .3f); mk.Prism(m, b + d * len * .55f, (d + Quaternion.Euler(0, 80f, 0) * d * .7f).normalized, .04f * s, len * .55f, 3, .3f); }
        }

        /// A bottle tree: a bare tree with blue glass bottles on the ends of its branches, to catch what walks at night.
        void BottleTree(LandContext ctx, MeshKit mk, Vector3 g, float s, System.Random rng)
        {
            var up = Vector3.up; mk.Prism(trunkM, g - up * .3f, up, .12f * s, 2.2f * s, 6, .6f); ArtistBits.PostCollider(ctx.Root, g, 2f * s, .14f * s);
            for (var k = 0; k < 9; k++) { var d = (Quaternion.Euler(0, k * 40f + (float)rng.NextDouble() * 20f, 0) * Vector3.forward + up * (.5f + (k % 3) * .5f)).normalized; var b = g + up * (1.2f + (k % 3) * .35f) * s; var len = (.9f + (float)rng.NextDouble() * .7f) * s; mk.Prism(trunkM, b, d, .035f * s, len, 3, .5f); mk.Prism(bottleM, b + d * (len - .1f), d, .05f * s, .3f * s, 6, .5f); }
            halos.Add(g + up * 2.2f * s, 1.6f * s, Blues, .25f);
        }

        /// A scarecrow on a cross of poles: a sack head, a hat, a coat that hangs.
        void Scarecrow(LandContext ctx, MeshKit mk, Vector3 g, Vector3 face, System.Random rng)
        {
            var up = Vector3.up; var rot = Quaternion.LookRotation(face); mk.Prism(trunkM, g - up * .3f, up, .05f, 2.5f, 4); mk.Box(trunkM, g + up * 1.6f, new Vector3(1.9f, .07f, .07f), rot * Quaternion.Euler(0, 0, (float)rng.NextDouble() * 10f - 5f));
            mk.Box(WorldMaterials.Solid(Planks[rng.Next(Planks.Length)] * .7f), g + up * 1.2f, new Vector3(.6f, .95f, .22f), rot); mk.Blob(WorldMaterials.Solid(new Color(.76f, .68f, .5f)), g + up * 1.98f, Vector3.one * .19f, rng, .1f); mk.Prism(dark, g + up * 2.1f, up, .34f, .03f, 8); mk.Prism(dark, g + up * 2.12f, up, .19f, .2f, 8, .8f);
            foreach (var e in new[] { -1f, 1f }) mk.Box(leafDry, g + up * 1.52f + rot * Vector3.right * e * .95f, new Vector3(.2f, .3f, .08f), rot); ArtistBits.PostCollider(ctx.Root, g, 2.2f, .2f);
        }

        // ---------------------------------------------------------------- the lanes to the artists
        void Paths(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var walk = 10; var lamps = new List<Vector3>();
            void Lay(Vector3 join, Vector3 to, float stopShort, string artist)
            {
                var from = new Vector3(join.x, 0, join.z); to.y = 0;
                var dir = to - from; var total = dir.magnitude; if (total < 2f) return; dir /= total;
                if (artist != null) facing[artist] = -dir;
                var side = Vector3.Cross(Vector3.up, dir); var end = total - stopShort;
                if (end < PathW) return;
                var pts = new List<Vector3>();
                for (var d = PathW / 2f + .1f; ; d += 1.5f)
                {
                    var dd = Mathf.Min(d, end); var q = from + dir * dd + side * Mathf.Sin(dd / 14f) * Mathf.Min(2f, end * .04f);
                    if (!ctx.IsLand(q.x, q.z, 0f)) break;
                    pts.Add(ctx.OnGround(q.x, q.z)); if (dd >= end) break;
                }
                if (pts.Count < 2) return;
                Strip(mk, pts, dustM, PathW, .12f, ctx);
                foreach (var p in pts) Remember(p, walk);
                for (var i = 9; i < pts.Count; i += 16)
                {
                    var s = (i / 16) % 2 == 0 ? 1f : -1f; var lp = pts[i] + side * s * (PathW / 2f + .9f);
                    var near = false; foreach (var o in lamps) if ((o - lp).sqrMagnitude < 20f * 20f) near = true;
                    if (near || InFootprint(ctx, lp.x, lp.z, 1f) || InNoGrass(lp.x, lp.z, 1f) || OnRail(lp.x, lp.z, 1f)) continue;
                    lamps.Add(lp); BulbPole(ctx, mk, ctx.OnGround(lp.x, lp.z), -side * s, true);
                }
                walk++;
            }
            var todo = new List<IslandWorldData.Artist>(ctx.Artists);
            while (todo.Count > 0)
            {
                IslandWorldData.Artist next = null; Vector3 join = default; var best = float.MaxValue;
                foreach (var a in todo)
                    foreach (var (p, w) in walkPoints)
                    {
                        if (w == 5) continue;
                        // a lane never crosses the railroad: it joins from its own side (Howlin' Wolf sits on the line itself)
                        var sideA = a.z > center.z + RailOff + 4f; var sideP = p.z > center.z + RailOff + 4f; if (sideA != sideP && Mathf.Abs(a.z - (center.z + RailOff)) > 4f) continue;
                        var d = Vector2.Distance(new Vector2(p.x, p.z), new Vector2(a.x, a.z)) - Foot(a); if (d < best) { best = d; next = a; join = p; }
                    }
                if (next == null) break;
                todo.Remove(next);
                Lay(join, new Vector3(next.x, 0, next.z), Foot(next) * .8f, next.name);
            }
        }

        // ---------------------------------------------------------------- out in the fields
        /// Scarecrows in the cotton, bottle trees, dead trees, live oaks hung with moss on the low ground, empty shacks.
        void Wilds(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            int crows = 0, bottles = 0, dead = 0, oaks = 0, shacks = 0, wagons = 0; var rng = new System.Random(1936);
            const float cell = 22f;
            for (var x = ctx.MinX; x < ctx.MaxX; x += cell)
            for (var z = ctx.MinZ; z < ctx.MaxZ; z += cell)
            {
                float px = x + (float)rng.NextDouble() * cell, pz = z + (float)rng.NextDouble() * cell; var roll = (float)rng.NextDouble(); var s = (float)rng.NextDouble();
                if (!Open(ctx, px, pz, 4f)) continue;
                var p = ctx.OnGround(px, pz); var low = p.y < plainY - 5f; var field = Field(px, pz);
                if (field)
                {
                    if (roll < .12f) { Scarecrow(ctx, mk, p, Quaternion.Euler(0, s * 360f, 0) * Vector3.forward, rng); crows++; }
                    else if (roll < .15f) { if (wagons < 5) { Wagon(ctx, mk, p, Quaternion.Euler(0, s * 360f, 0) * Vector3.forward, rng); wagons++; } }
                }
                else if (roll < .1f) { BottleTree(ctx, mk, p, .9f + s * .5f, rng); bottles++; }
                else if (roll < .2f) { DeadTree(ctx, mk, p, .9f + s * .8f, rng); dead++; }
                else if (roll < (low ? .5f : .27f)) { Oak(ctx, mk, p, 1f + s * .6f, rng); oaks++; }
                else if (roll < .3f) { if (shacks < 7) { EmptyShack(ctx, mk, solid, p, FaceTo(p, NearestWalk(p)), shacks, rng); shacks++; } }
            }
            Debug.Log($"The Delta Crossroads: Highway {northL + southL:0} m, cross road {westL + eastL:0} m, railroad {railW + railE:0} m, {crows} scarecrows, {bottles} bottle trees, {dead} dead trees, {oaks} oaks, {shacks} empty shacks, {wagons} wagons");
        }

        /// Whether this spot is a cotton field (broad patches of the plain).
        bool Field(float x, float z) => Mathf.PerlinNoise(x * .012f + 31f, z * .012f + 7f) > .5f;

        Vector3 NearestWalk(Vector3 p) { var best = center; var bd = float.MaxValue; foreach (var (q, _) in walkPoints) { var d = (q - p).sqrMagnitude; if (d < bd) { bd = d; best = q; } } return best; }

        /// A live oak, low and wide, grey moss hanging from it.
        void Oak(LandContext ctx, MeshKit mk, Vector3 g, float s, System.Random rng)
        {
            var up = Vector3.up; mk.Prism(trunkM, g - up * .3f, up, .34f * s, 2.6f * s, 7, .7f); ArtistBits.PostCollider(ctx.Root, g, 2.4f * s, .36f * s);
            for (var k = 0; k < 4; k++) { var d = Quaternion.Euler(0, k * 90f + (float)rng.NextDouble() * 50f, 0) * Vector3.forward; var c = g + up * (3.2f + (float)rng.NextDouble() * .5f) * s + d * 1.7f * s; mk.Blob(k % 2 == 0 ? leafM : leafDry, c, new Vector3(2.3f, 1.3f, 2.3f) * s, rng, .25f); for (var q = 0; q < 2; q++) { var m = c + Quaternion.Euler(0, q * 140f + k * 30f, 0) * Vector3.forward * 1.3f * s - up * .6f * s; mk.Box(mossM, m - up * .5f * s, new Vector3(.12f, 1.1f * s, .12f), Quaternion.Euler(0, k * 40f, 0)); } }
            mk.Blob(leafM, g + up * 4.1f * s, new Vector3(2.2f, 1.2f, 2.2f) * s, rng, .25f);
        }

        /// A cotton wagon left in the field, heaped white.
        void Wagon(LandContext ctx, MeshKit mk, Vector3 g, Vector3 face, System.Random rng)
        {
            var up = Vector3.up; var rot = Quaternion.LookRotation(face); var side = Vector3.Cross(up, face);
            mk.Box(plank, g + up * .9f, new Vector3(1.7f, .1f, 3.2f), rot); foreach (var e in new[] { -1f, 1f }) { mk.Box(plankDark, g + up * 1.3f + side * e * .85f, new Vector3(.06f, .8f, 3.2f), rot); foreach (var q in new[] { -1.1f, 1.1f }) mk.Prism(plankDark, g + up * .55f + side * e * .9f + face * q, side * e, .55f, .08f, 10); }
            mk.Blob(cottonM, g + up * 1.5f, new Vector3(.8f, .5f, 1.5f), rng, .3f); mk.Box(plankDark, g + up * .7f + face * 2.3f, new Vector3(.08f, .08f, 1.6f), rot * Quaternion.Euler(14f, 0, 0)); BoxCol(ctx.Root, g + up * .9f, new Vector3(1.9f, 1.8f, 3.3f), rot); noGrass.Add(new Vector3(g.x, 2.4f, g.z));
        }

        /// An empty shack, roof rusted through, door hanging; something left on the porch.
        void EmptyShack(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 g, Vector3 face, int i, System.Random rng)
        {
            var f = new PlaceFrame(g, face); var po = Shack(ctx, mk, solid, f, 5f, 4f, i + 1, true);
            if (i % 3 == 0) Rocker(mk, po.P(1.4f, 0, 0), po.Fwd); else if (i % 3 == 1) Guitar(mk, po.P(-1.6f, 0, -.9f), po.r, WorldMaterials.Solid(new Color(.5f, .34f, .2f)), rng); else mk.Box(rustM, po.P(1.5f, .25f, -.4f), new Vector3(.5f, .5f, .5f), po.r);
            noGrass.Add(new Vector3(g.x, 5.5f, g.z));
        }

        /// The storm stacked on the western horizon: a wall of dark cloud that DeltaLife sets flickering.
        void StormBank(LandContext ctx, MeshKit mk)
        {
            var rng = new System.Random(1958);
            for (var k = 0; k < 46; k++)
            {
                var a = (150f + (float)rng.NextDouble() * 150f) * Mathf.Deg2Rad; var d = 620f + (float)rng.NextDouble() * 260f; var h = 60f + (float)rng.NextDouble() * 260f;
                mk.Blob(stormM, center + new Vector3(Mathf.Sin(a) * d, h, Mathf.Cos(a) * d), new Vector3(120f + (float)rng.NextDouble() * 90f, 50f + (float)rng.NextDouble() * 50f, 120f + (float)rng.NextDouble() * 90f), rng, .3f);
            }
        }

        /// The fields: cotton in rows, white with bolls; dry grass everywhere else.
        void Verge(LandContext ctx)
        {
            var mat = WindMesh.Material(.35f, 1f, new Color(.7f, .62f, .36f));
            var meshes = new Dictionary<(int, int), WindMesh>(); var rng = new System.Random(1937);
            const float st = 2.4f;
            for (var x = ctx.MinX; x < ctx.MaxX; x += st)
            for (var z = ctx.MinZ; z < ctx.MaxZ; z += st)
            {
                var field = Field(x, z);
                var px = x + (field ? 0f : ((float)rng.NextDouble() - .5f) * st); var pz = z + ((float)rng.NextDouble() - .5f) * st;
                if (!ctx.IsLand(px, pz, 1f) || OnRail(px, pz, 1f) || NearPath(px, pz, 3.6f) || InNoGrass(px, pz, 0f) || InFootprint(ctx, px, pz, -2f)) continue;
                if (!field && Mathf.PerlinNoise(px * .05f + 3f, pz * .05f + 9f) < .42f) continue;
                var key = (Mathf.FloorToInt(px / 70f), Mathf.FloorToInt(pz / 70f));
                if (!meshes.TryGetValue(key, out var wm)) meshes[key] = wm = new WindMesh();
                if (field) { var col = Color.Lerp(new Color(.36f, .34f, .2f), new Color(.46f, .42f, .24f), (float)rng.NextDouble()); wm.Tuft(new Vector3(px, ctx.Ground(px, pz) - .04f, pz), .5f + (float)rng.NextDouble() * .25f, col, new Color(.98f, .97f, .93f), rng, 4, .13f); }
                else { var col = Color.Lerp(new Color(.62f, .54f, .3f), new Color(.76f, .68f, .4f), (float)rng.NextDouble()); wm.Tuft(new Vector3(px, ctx.Ground(px, pz) - .04f, pz), .26f + (float)rng.NextDouble() * .34f, col, Color.Lerp(col, Cream, .4f), rng, 3, .05f); }
            }
            foreach (var kv in meshes) kv.Value.Build("Delta fields", ctx.Root, mat);
        }
    }
}
