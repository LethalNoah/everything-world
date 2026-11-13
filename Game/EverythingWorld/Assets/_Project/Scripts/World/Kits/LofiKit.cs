using System.Collections.Generic;
using UnityEngine;
using static EverythingWorld.World.PlaceTools;

namespace EverythingWorld.World
{
    /// The Lo-Fi Hills (WORLD_GUIDE.md): a rainy blue dusk on soft green hills. Steady rain, puddles holding the
    /// lamplight, small houses each with one warm window, vending machines glowing by the road, power lines from
    /// pole to pole, and cats asleep on everything. You arrive at the Corner: a late-night shop and a bus stop.
    /// Lamp Lane climbs from it to the Study, a tower with one enormous lit window, and a desk inside to sit at.
    /// For studying, for feeling safe, for being a little sad at two in the morning.
    /// Laid out on purpose (the build-land standard): LofiTown.cs has the places and the people.
    public partial class LofiKit : LandKit
    {
        public override string Land => "The Lo-Fi Hills";
        public override Vector3 ArtistFront(IslandWorldData data, IslandWorldData.Artist a) => facing.TryGetValue(a.name, out var f) ? f : base.ArtistFront(data, a);

        public static readonly Color Lamp = new Color(1f, .78f, .46f), Mauve = new Color(.46f, .38f, .54f), RainBlue = new Color(.5f, .64f, .86f), Sage = new Color(.56f, .66f, .52f), Cream = new Color(.95f, .9f, .8f), Ink = new Color(.1f, .1f, .16f), Pink = new Color(1f, .6f, .7f);
        static readonly Color[] Walls = { new Color(.8f, .76f, .68f), new Color(.52f, .6f, .52f), new Color(.5f, .44f, .58f), new Color(.46f, .54f, .66f), new Color(.7f, .56f, .5f), new Color(.6f, .62f, .66f) };
        static readonly Color[] Roofs = { new Color(.3f, .3f, .42f), new Color(.42f, .28f, .3f), new Color(.24f, .34f, .36f), new Color(.34f, .3f, .28f) };
        static readonly Color[] Fur = { new Color(.9f, .6f, .3f), new Color(.2f, .2f, .22f), new Color(.92f, .9f, .86f), new Color(.56f, .56f, .6f), new Color(.7f, .5f, .36f) };
        const float PathW = 2.6f, StreetW = 4.4f;

        Material road, paving, woodM, iron, dark, white, leafM, leafDark, leafMauve, trunkM, windowM, lampM, vendM, puddleM, curtain, quilt;
        GlowHalos halos; Vector3 center, D, S; LofiLife life;
        readonly Dictionary<(int, int), List<Vector3>> pathCells = new Dictionary<(int, int), List<Vector3>>();
        readonly List<(Vector3 p, int w)> walkPoints = new List<(Vector3, int)>();
        readonly Dictionary<string, Vector3> facing = new Dictionary<string, Vector3>();
        // x, radius, z
        readonly List<Vector3> noGrass = new List<Vector3>();
        Vector2 heart2, D2 = new Vector2(0, -1), S2 = new Vector2(1, 0); float plazaY, waterY, streetL, studyY; int cats;

        void MakeMaterials()
        {
            // everything is faintly self-lit, so the dusk never swallows it
            road = WorldMaterials.Glow(new Color(.3f, .32f, .42f), .12f); paving = WorldMaterials.Glow(new Color(.5f, .5f, .58f), .12f); woodM = WorldMaterials.Glow(new Color(.46f, .34f, .28f), .12f); iron = WorldMaterials.Solid(new Color(.16f, .16f, .2f), .5f); dark = WorldMaterials.Solid(new Color(.07f, .07f, .1f)); white = WorldMaterials.Glow(new Color(.92f, .9f, .86f), .14f);
            leafM = WorldMaterials.Glow(new Color(.3f, .48f, .4f), .14f); leafDark = WorldMaterials.Glow(new Color(.2f, .36f, .36f), .12f); leafMauve = WorldMaterials.Glow(new Color(.56f, .44f, .62f), .16f); trunkM = WorldMaterials.Glow(new Color(.3f, .26f, .28f), .1f); curtain = WorldMaterials.Glow(new Color(.9f, .56f, .4f), .5f); quilt = WorldMaterials.Glow(new Color(.5f, .42f, .62f), .16f);
            windowM = new Material(WorldMaterials.Glow(new Color(1f, .74f, .4f), 1.3f)) { name = "Lo-Fi windows" }; lampM = new Material(WorldMaterials.Glow(Lamp, 1.7f)) { name = "Lo-Fi lamps" };
            vendM = new Material(WorldMaterials.Glow(new Color(.8f, .92f, 1f), 1.4f)) { name = "Lo-Fi vending" }; puddleM = new Material(WorldMaterials.Glow(new Color(.5f, .62f, .86f), .5f)) { name = "Lo-Fi puddles" };
        }

        Material GlowM(Color c, float s = 2f) => WorldMaterials.Glow(c, s * .65f);
        Material Paint(Color c) => WorldMaterials.Glow(c, .14f);
        static bool HasStation(IslandWorldData.Artist a) => a.stationId != 0;
        static float Foot(IslandWorldData.Artist a)
        {
            switch (a.name)
            {
                case "Idealism": return 14f;
                case "digitalluc": return 13f;
                case "Michael Lementi": return 10f;
                default: return 8.5f;
            }
        }

        public override bool Ground(float height, float slope, float patch, out Color color)
        {
            // wet grass, blue in the dusk
            if (slope > 28f) color = Color.Lerp(new Color(.36f, .38f, .46f), new Color(.42f, .42f, .5f), patch);
            else color = Color.Lerp(new Color(.26f, .44f, .42f), new Color(.38f, .54f, .44f), Mathf.SmoothStep(0f, 1f, patch));
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
            // Lamp Lane climbs toward the highest ground it can reach, and ends at the Study
            streetL = 0f; var bestScore = float.MinValue;
            for (var k = 0; k < 8; k++) { var dir = new Vector2(Mathf.Sin(k * Mathf.PI / 4f), Mathf.Cos(k * Mathf.PI / 4f)); var len = 40f; while (len < 120f && Inside(heart2 + dir * (len + 10f), 50f)) len += 10f; var end = heart2 + dir * (len + 12f); var score = len * .5f + data.HeightAt(end.x, end.y); if (len >= 70f && score > bestScore) { bestScore = score; streetL = len; D2 = dir; } }
            if (streetL < 1f) { streetL = 60f; D2 = new Vector2(0, -1); }
            S2 = new Vector2(-D2.y, D2.x); plazaY = Mathf.Max(data.HeightAt(heart2.x, heart2.y), waterY + 6f);
            var st = P2(streetL + 12f, 0); studyY = Mathf.Max(data.HeightAt(st.x, st.y), waterY + 4f);

            var keep = new List<string>();
            void Put(string name, float d, float s, float room) { var p = P2(d, s); if (!Inside(p, room)) return; foreach (var a in data.layout.artists) if (a.sceneId == isl.sceneId && a.name == name) { a.x = p.x; a.z = p.y; keep.Add(name); } }
            Put("Idealism", -6f, 48f, 26f); Put("digitalluc", 0f, -50f, 26f); Put("Michael Lementi", 56f, 42f, 22f); Put("Felipe Jara", 58f, -44f, 22f);
            var avoid = new List<Vector3>(); for (var d = 0f; d <= streetL; d += 22f) avoid.Add(new Vector3(heart2.x + D2.x * d, 11f, heart2.y + D2.y * d)); avoid.Add(new Vector3(st.x, 24f, st.y));
            ArtistPlacement.Spread(data, index, 56f, heart2, 44f, 42f, avoid, keep, Land, 330f);
            Level(data, index, heart2, 20f, 38f, plazaY); Level(data, index, st, 18f, 36f, studyY);
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
            pathCells.Clear(); walkPoints.Clear(); facing.Clear(); noGrass.Clear(); cats = 0;
            center = ctx.Plaza; D = new Vector3(D2.x, 0, D2.y); S = new Vector3(S2.x, 0, S2.y);
            life = new GameObject("Lo-Fi life").AddComponent<LofiLife>();
            life.transform.SetParent(ctx.Root, false);
            life.center = center; life.windows = windowM; life.lamps = lampM; life.vending = vendM; life.puddles = puddleM;
            var mk = new MeshKit(); var solid = new MeshKit();
            Remember(ctx.OnGround(center.x, center.z), 0);
            TownStart(ctx);
            TheCorner(ctx, mk, solid);
            LampLane(ctx, mk, solid);
            TheStudy(ctx, mk, solid);
            Paths(ctx, mk, solid);
            Cottages(ctx, mk, solid);
            Wilds(ctx, mk, solid);
            mk.Build("The Lo-Fi Hills", ctx.Root, false);
            solid.BuildCollider("The Lo-Fi Hills walls", ctx.Root);
        }

        public override void Finish(LandContext ctx) { Verge(ctx); halos.Build("Lo-Fi glow", ctx.Root); }

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

        /// A street lamp: a post, a curved neck, a warm glass head.
        void StreetLamp(LandContext ctx, MeshKit mk, Vector3 g, Vector3 toward, bool light = false)
        {
            var up = Vector3.up; toward.y = 0; toward.Normalize(); mk.Prism(iron, g - up * .2f, up, .06f, 4.2f, 6, .7f); mk.Prism(iron, g + up * 3.9f, (toward + up * .3f).normalized, .04f, .8f, 4); var h = g + up * 4.1f + toward * .8f; mk.Cone(iron, h, .26f, .2f, 8); mk.Blob(lampM, h - up * .06f, new Vector3(.16f, .12f, .16f), new System.Random(1), 0f);
            halos.Add(h - up * .1f, 1.8f, Lamp, .5f); ArtistBits.PostCollider(ctx.Root, g, 4f, .09f); if (light) ArtistBits.PointLight(ctx.Root, h - up * .9f, new Color(1f, .8f, .52f), 15f, 2.3f);
        }

        /// A utility pole with a crossbar and a grey box on it.
        Vector3 Pole(LandContext ctx, MeshKit mk, Vector3 g, Vector3 across)
        {
            var up = Vector3.up; across.y = 0; across.Normalize(); mk.Prism(trunkM, g - up * .3f, up, .11f, 7.3f, 6, .7f); mk.Box(trunkM, g + up * 6.5f, new Vector3(1.8f, .1f, .1f), Quaternion.LookRotation(Vector3.Cross(up, across))); mk.Box(Paint(new Color(.56f, .58f, .62f)), g + up * 5.2f + across * .16f, new Vector3(.3f, .5f, .24f), Quaternion.LookRotation(across)); foreach (var e in new[] { -.8f, 0f, .8f }) mk.Prism(white, g + up * 6.55f + across * e, up, .03f, .12f, 5);
            ArtistBits.PostCollider(ctx.Root, g, 6f, .14f); return g + up * 6.66f;
        }

        /// Three sagging wires between two pole tops.
        void Wires(MeshKit mk, Vector3 a, Vector3 b, Vector3 across)
        {
            across.y = 0; across.Normalize(); var len = Vector3.Distance(a, b); if (len > 34f || len < 3f) return;
            foreach (var e in new[] { -.8f, 0f, .8f }) { var prev = a + across * e; for (var i = 1; i <= 6; i++) { var t = i / 6f; var p = Vector3.Lerp(a, b, t) + across * e - Vector3.up * Mathf.Sin(t * Mathf.PI) * len * .05f; mk.Prism(dark, prev, (p - prev).normalized, .012f, Vector3.Distance(prev, p), 3); prev = p; } }
        }

        /// A puddle holding the light.
        void Puddle(MeshKit mk, Vector3 g, float r) { mk.Prism(puddleM, g + Vector3.up * .02f, Vector3.up, r, .012f, 9); }

        /// A vending machine glowing in the rain.
        void Vending(LandContext ctx, MeshKit mk, Vector3 g, Vector3 face, int i, bool light = true)
        {
            var up = Vector3.up; var rot = Quaternion.LookRotation(face); var body = Paint(new[] { new Color(.9f, .9f, .92f), new Color(.3f, .46f, .76f), new Color(.8f, .3f, .32f) }[i % 3]);
            mk.Box(body, g + up * .95f, new Vector3(1f, 1.9f, .8f), rot); mk.Box(vendM, g + up * 1.2f + face * .41f, new Vector3(.84f, 1.1f, .02f), rot); for (var row = 0; row < 3; row++) for (var k = 0; k < 5; k++) mk.Box(Paint(Color.HSVToRGB(((i * 3 + row * 5 + k * 2) % 10) / 10f, .6f, .9f)), g + up * (.86f + row * .34f) + rot * new Vector3(-.3f + k * .15f, 0, .43f), new Vector3(.09f, .2f, .02f), rot);
            mk.Box(dark, g + up * .3f + face * .41f, new Vector3(.6f, .2f, .02f), rot); mk.Box(lampM, g + up * 1.86f + face * .41f, new Vector3(.84f, .08f, .02f), rot); BoxCol(ctx.Root, g + up * .95f, new Vector3(1f, 1.9f, .8f), rot); halos.Add(g + up * 1.2f + face * .8f, 1.8f, new Color(.8f, .92f, 1f), .36f);
            if (light) ArtistBits.PointLight(ctx.Root, g + up * 1.3f + face * 1.2f, new Color(.8f, .9f, 1f), 8f, 2f);
        }

        /// A cat. Asleep in a loaf, or sitting up. Some can be stroked.
        void Cat(MeshKit mk, Vector3 g, Vector3 face, bool asleep = true, bool pet = false)
        {
            var up = Vector3.up; var i = cats++; var m = Paint(Fur[i % Fur.Length]); var rng = new System.Random(i); face.y = 0; face.Normalize(); var side = Vector3.Cross(up, face); var rot = Quaternion.LookRotation(face);
            if (asleep) { mk.Blob(m, g + up * .1f, new Vector3(.17f, .11f, .24f), rng, 0f); var h = g + up * .12f + face * .2f + side * .06f; mk.Blob(m, h, Vector3.one * .095f, rng, 0f); foreach (var e in new[] { -1f, 1f }) mk.Cone(m, h + up * .07f + side * e * .05f, .03f, .06f, 4); for (var k = 0; k < 5; k++) mk.Blob(m, g + up * .05f - face * (.2f - k * .06f) + side * (.14f + Mathf.Sin(k * .6f) * .06f), Vector3.one * .04f, rng, 0f); }
            else { mk.Blob(m, g + up * .16f, new Vector3(.12f, .17f, .14f), rng, 0f); var h = g + up * .38f + face * .04f; mk.Blob(m, h, Vector3.one * .095f, rng, 0f); foreach (var e in new[] { -1f, 1f }) { mk.Cone(m, h + up * .07f + side * e * .05f, .03f, .07f, 4); mk.Blob(GlowM(new Color(.7f, 1f, .5f), 1f), h + face * .085f + side * e * .035f + up * .01f, Vector3.one * .014f, rng, 0f); } mk.Prism(m, g + up * .04f - face * .1f, (side + up * .3f).normalized, .025f, .3f, 4); }
            if (!pet) return;
            string[] purr = { "Prrrrr. One eye opens, decides you're acceptable, closes again.", "The cat leans into your hand like it's been waiting all evening. It has.", "Purring. It's in time with the rain, or the rain's in time with it.", "A slow blink. In cat, that's 'you can stay.'" }; var pn = i;
            Spot("Cat", g + up * .3f, "Stroke the cat", new Vector3(.6f, .5f, .6f)).onUse = _ => Say(tlife.transform, g + up * 1.1f, purr[pn++ % purr.Length], 4.5f);
        }

        /// A round soft tree.
        void Tree(LandContext ctx, MeshKit mk, Vector3 g, float s, System.Random rng, bool mauve = false)
        {
            var up = Vector3.up; mk.Prism(trunkM, g - up * .2f, up, .16f * s, 2.4f * s, 6, .6f); ArtistBits.PostCollider(ctx.Root, g, 2.2f * s, .2f * s); var m = mauve ? leafMauve : leafM;
            mk.Blob(m, g + up * 3.2f * s, new Vector3(1.6f, 1.4f, 1.6f) * s, rng, .18f); mk.Blob(mauve ? leafMauve : leafDark, g + up * 2.6f * s + new Vector3(.8f, 0, .3f) * s, new Vector3(1.1f, .9f, 1.1f) * s, rng, .18f); mk.Blob(m, g + up * 2.8f * s - new Vector3(.7f, 0, .5f) * s, new Vector3(1f, .9f, 1f) * s, rng, .18f);
            noGrass.Add(new Vector3(g.x, .8f * s, g.z));
        }

        /// A wooden bench to sit on.
        void Bench(Transform parent, MeshKit mk, Vector3 g, Vector3 face, float len = 1.8f)
        {
            var up = Vector3.up; var rot = Quaternion.LookRotation(face); var side = Vector3.Cross(up, face); for (var k = 0; k < 3; k++) mk.Box(woodM, g + up * .46f + face * (-.16f + k * .16f), new Vector3(len, .05f, .13f), rot); for (var k = 0; k < 2; k++) mk.Box(woodM, g + up * (.7f + k * .2f) - face * .26f, new Vector3(len, .13f, .05f), rot); foreach (var e in new[] { -1f, 1f }) mk.Box(iron, g + up * .3f + side * e * (len / 2f - .15f), new Vector3(.06f, .6f, .5f), rot);
            BoxCol(parent, g + up * .45f, new Vector3(len, .9f, .5f), rot); Interact.Bench(tlife.transform, g + up * .52f - side * (len / 2f - .5f), g + up * .52f + side * (len / 2f - .5f), face);
        }

        /// A chair to sit on.
        void Chair(MeshKit mk, Vector3 g, Vector3 face, string verb = "Sit")
        {
            var up = Vector3.up; var rot = Quaternion.LookRotation(face); mk.Box(woodM, g + up * .45f, new Vector3(.46f, .05f, .46f), rot); mk.Box(woodM, g + up * .8f - face * .21f, new Vector3(.46f, .7f, .05f), rot); foreach (var (x, zz) in new[] { (-.2f, -.2f), (.2f, -.2f), (.2f, .2f), (-.2f, .2f) }) mk.Box(woodM, g + up * .22f + rot * new Vector3(x, 0, zz), new Vector3(.04f, .44f, .04f), rot);
            Interact.Seat(tlife.transform, g + up * .5f, face, verb);
        }

        /// A plant in a pot.
        void PotPlant(Transform parent, MeshKit mk, Vector3 g, float s, int i)
        {
            var up = Vector3.up; mk.Prism(Paint(new Color(.7f, .46f, .36f)), g, up, .2f * s, .34f * s, 8, 1.25f); for (var q = 0; q < 6; q++) mk.Prism(q % 2 == 0 ? leafM : leafDark, g + up * .3f * s, (up + Quaternion.Euler(0, q * 60f + i * 20f, 0) * Vector3.forward * .34f).normalized, .06f * s, (.6f + (q % 3) * .22f) * s, 4, .2f); ArtistBits.PostCollider(parent, g, .5f * s, .24f * s);
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
                    var dd = Mathf.Min(d, end); var q = from + dir * dd + side * Mathf.Sin(dd / 12f) * Mathf.Min(2.4f, end * .05f);
                    if (!ctx.IsLand(q.x, q.z, 0f)) break;
                    pts.Add(ctx.OnGround(q.x, q.z)); if (dd >= end) break;
                }
                if (pts.Count < 2) return;
                Strip(mk, pts, paving, PathW, .13f, ctx);
                foreach (var p in pts) Remember(p, walk);
                Vector3 last = default; var have = false;
                for (var i = 5; i < pts.Count; i += 8, n++)
                {
                    var s = n % 2 == 0 ? 1f : -1f; var lp = pts[i] + side * s * (PathW / 2f + .7f);
                    if (InFootprint(ctx, lp.x, lp.z, 1f) || InNoGrass(lp.x, lp.z, 1f)) continue;
                    var g = ctx.OnGround(lp.x, lp.z);
                    if (n % 2 == 0) StreetLamp(ctx, mk, g, -side * s, n % 4 == 0);
                    else { var top = Pole(ctx, mk, g, side); if (have) Wires(mk, last, top, side); last = top; have = true; }
                    if (n % 3 == 0) Puddle(mk, pts[i] - side * s * .4f + Vector3.up * .13f, .5f + (float)rng.NextDouble() * .5f);
                    if (n % 5 == 2) Cat(mk, ctx.OnGround(lp.x + dir.x * 1.4f, lp.z + dir.z * 1.4f), -side * s, n % 2 == 0, true);
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
        /// Houses with one lit window, trees, vending machines, benches under a lamp, puddles, and cats.
        void Wilds(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            int houses = 0, trees = 0, vends = 0, benches = 0, puddles = 0; var catsBefore = cats; var rng = new System.Random(1999);
            const float cell = 20f;
            for (var x = ctx.MinX; x < ctx.MaxX; x += cell)
            for (var z = ctx.MinZ; z < ctx.MaxZ; z += cell)
            {
                float px = x + (float)rng.NextDouble() * cell, pz = z + (float)rng.NextDouble() * cell; var roll = (float)rng.NextDouble(); var s = (float)rng.NextDouble();
                if (!Open(ctx, px, pz, 4f)) continue;
                var p = ctx.OnGround(px, pz); var f = FaceTo(p, NearestWalk(p));
                if (roll < .13f) { if (!Open(ctx, px, pz, 7f) || !ctx.IsLand(px, pz, 8f) || Mathf.Abs(ctx.Ground(px + 4f, pz) - ctx.Ground(px - 4f, pz)) > 2f || Mathf.Abs(ctx.Ground(px, pz + 4f) - ctx.Ground(px, pz - 4f)) > 2f) continue; Cottage(ctx, mk, solid, new PlaceFrame(p, f), houses + 20, rng); houses++; }
                else if (roll < .5f) { Tree(ctx, mk, p, .9f + s * .7f, rng, roll > .44f); trees++; if (s > .8f) Cat(mk, p + f * 1.2f, f, true, false); }
                else if (roll < .55f) { Vending(ctx, mk, p, f, vends, vends % 2 == 0); if (s > .4f) Cat(mk, p + Vector3.up * 1.9f, f, true, false); noGrass.Add(new Vector3(p.x, 1.6f, p.z)); vends++; }
                else if (roll < .62f) { Bench(ctx.Root, mk, p, f); StreetLamp(ctx, mk, p - f * 1f + Vector3.Cross(Vector3.up, f) * 1.4f, f, benches % 2 == 0); if (s > .5f) Cat(mk, p + Vector3.up * .5f + Vector3.Cross(Vector3.up, f) * .5f, f, true, true); noGrass.Add(new Vector3(p.x, 2f, p.z)); benches++; }
                else if (roll < .7f) { Puddle(mk, p, 1f + s * 1.6f); noGrass.Add(new Vector3(p.x, 1.2f + s * 1.6f, p.z)); puddles++; }
            }
            Debug.Log($"The Lo-Fi Hills: Lamp Lane {streetL:0} m, {houses} houses, {trees} trees, {vends} vending machines, {benches} benches, {puddles} puddles, {cats} cats ({cats - catsBefore} out in the hills)");
        }

        Vector3 NearestWalk(Vector3 p) { var best = center; var bd = float.MaxValue; foreach (var (q, _) in walkPoints) { var d = (q - p).sqrMagnitude; if (d < bd) { bd = d; best = q; } } return best; }

        /// Wet grass.
        void Verge(LandContext ctx)
        {
            var mat = WindMesh.Material(.24f, .8f, new Color(.3f, .5f, .44f));
            var meshes = new Dictionary<(int, int), WindMesh>(); var rng = new System.Random(2000);
            const float st = 2.4f;
            for (var x = ctx.MinX; x < ctx.MaxX; x += st)
            for (var z = ctx.MinZ; z < ctx.MaxZ; z += st)
            {
                var px = x + ((float)rng.NextDouble() - .5f) * st; var pz = z + ((float)rng.NextDouble() - .5f) * st;
                if (!ctx.IsLand(px, pz, 1f) || NearPath(px, pz, 3.2f) || InNoGrass(px, pz, 0f) || InFootprint(ctx, px, pz, -2f)) continue;
                if (Mathf.PerlinNoise(px * .04f + 8f, pz * .04f + 4f) < .36f) continue;
                var key = (Mathf.FloorToInt(px / 70f), Mathf.FloorToInt(pz / 70f));
                if (!meshes.TryGetValue(key, out var wm)) meshes[key] = wm = new WindMesh();
                var col = Color.Lerp(new Color(.24f, .42f, .4f), new Color(.36f, .54f, .44f), (float)rng.NextDouble());
                wm.Tuft(new Vector3(px, ctx.Ground(px, pz) - .04f, pz), .24f + (float)rng.NextDouble() * .3f, col, rng.NextDouble() < .06 ? new Color(.8f, .7f, 1f) : Color.Lerp(col, new Color(.6f, .8f, .8f), .3f), rng, 3, .08f);
            }
            foreach (var kv in meshes) kv.Value.Build("Lo-Fi grass", ctx.Root, mat);
        }
    }
}
