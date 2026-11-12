using System.Collections.Generic;
using UnityEngine;
using static EverythingWorld.World.PlaceTools;

namespace EverythingWorld.World
{
    /// The Funk Tropics (WORLD_GUIDE.md): a neon jungle at night, and three kinds of funk in it. You arrive in a
    /// clearing on the One, a path of light-up paving that runs straight to the Mothership: a flying saucer landed
    /// in the jungle on three legs, its ramp let down as a stage, lights racing round its rim (the cosmic, silly
    /// side). The other way the One becomes Funk Street: a short strip of neon half swallowed by the jungle, a club,
    /// a roller rink, a clothes shop, a long low car at the kerb (the cool, sly side). And all round, the jungle
    /// itself keeps the groove: glowing purple, green and orange plants that pulse on the beat, palms hung with
    /// mirror-ball fruit, vines strung like bass strings that you can pluck, giant platform shoes left as stepping
    /// stones. Dark, humid and saturated: everything that lights, lights in colour.
    /// Laid out on purpose (the build-land standard): FunkPlaces.cs has the places and the people.
    public partial class FunkKit : LandKit
    {
        public override string Land => "The Funk Tropics";
        public override Vector3 ArtistFront(IslandWorldData data, IslandWorldData.Artist a) => facing.TryGetValue(a.name, out var f) ? f : base.ArtistFront(data, a);

        public static readonly Color Purple = new Color(.66f, .3f, 1f), Orange = new Color(1f, .5f, .15f), Lime = new Color(.45f, 1f, .4f), Gold = new Color(1f, .78f, .3f), Pink = new Color(1f, .3f, .7f), Cyan = new Color(.3f, .9f, 1f), Ink = new Color(.96f, .92f, .98f);
        static readonly Color[] Neon = { new Color(.66f, .3f, 1f), new Color(1f, .5f, .15f), new Color(.45f, 1f, .4f), new Color(1f, .3f, .7f), new Color(.3f, .9f, 1f), new Color(1f, .78f, .3f) };
        const float PathW = 3.2f, SaucerR = 15f, SaucerH = 7.5f, StreetLen = 96f, StreetW = 8f;

        Material trunk, leafDark, leaf, earth, stone, chrome, dark, white, mirror, paveDark;
        readonly Material[] floraM = new Material[3];   // purple, lime, orange: every glowing plant; the life script pulses them on the beat
        readonly Material[] tileM = new Material[4];    // the One's light-up paving
        Material fruitM;                                // the mirror-ball fruit lanterns
        GlowHalos halos; Vector3 center, toShip, sideways; FunkLife life;
        readonly Dictionary<(int, int), List<Vector3>> pathCells = new Dictionary<(int, int), List<Vector3>>();
        readonly List<(Vector3 p, int w)> walkPoints = new List<(Vector3, int)>();
        readonly Dictionary<string, Vector3> facing = new Dictionary<string, Vector3>();
        readonly List<Vector3> noGrass = new List<Vector3>();   // x, radius, z
        Vector2 heart2, ship2, dir2, side2, streetEnd2; float waterY; bool hasShip;

        void MakeMaterials()
        {
            trunk = WorldMaterials.Solid(new Color(.36f, .26f, .4f)); leafDark = WorldMaterials.Solid(new Color(.1f, .36f, .28f)); leaf = WorldMaterials.Solid(new Color(.16f, .52f, .3f));
            earth = WorldMaterials.Glow(new Color(.5f, .32f, .62f), .16f); stone = WorldMaterials.Solid(new Color(.3f, .28f, .36f)); chrome = WorldMaterials.Solid(new Color(.7f, .72f, .8f), .9f);   // (lanes, pads and walls here are faintly self-lit, so the night never swallows them)
            dark = WorldMaterials.Solid(new Color(.06f, .05f, .09f)); white = WorldMaterials.Solid(new Color(.95f, .94f, .98f)); mirror = WorldMaterials.Solid(new Color(.85f, .88f, .95f), .95f); paveDark = WorldMaterials.Glow(new Color(.3f, .2f, .5f), .12f);
            Color[] fc = { Purple, Lime, Orange };
            for (var i = 0; i < 3; i++) floraM[i] = new Material(WorldMaterials.Glow(fc[i], 2.2f)) { name = "Funk flora " + i };
            for (var i = 0; i < 4; i++) tileM[i] = new Material(WorldMaterials.Glow(Neon[i], 1f)) { name = "The One " + i };
            fruitM = new Material(WorldMaterials.Glow(new Color(.9f, .92f, 1f), 1.5f)) { name = "Mirror-ball fruit" };
        }

        Material GlowM(Color c, float s = 2f) => WorldMaterials.Glow(c, s * .65f);
        static bool HasStation(IslandWorldData.Artist a) => a.stationId != 0;
        static float Foot(IslandWorldData.Artist a)
        {
            switch (a.name)
            {
                case "Parliament": case "Prince": case "The Meters": return 15f;
                case "Funkadelic": case "James Brown": return 13f;
                case "Thundercat": return 12f;
                case "Commodores": return 10f;
                default: return HasStation(a) ? 12f : 8f;
            }
        }

        public override bool Ground(float height, float slope, float patch, out Color color)
        {
            if (height < waterY + 2f) color = Color.Lerp(new Color(.2f, .16f, .26f), new Color(.26f, .2f, .32f), patch);                 // dark violet sand
            else if (slope > 20f) color = Color.Lerp(new Color(.22f, .16f, .32f), new Color(.3f, .2f, .4f), patch);                    // wet black rock
            else color = Color.Lerp(new Color(.08f, .3f, .24f), new Color(.14f, .42f, .22f), Mathf.SmoothStep(0f, 1f, patch));          // deep jungle floor
            color.a = 1f;
            return true;
        }

        static void BoxCol(Transform parent, Vector3 center, Vector3 size, Quaternion rot)
        {
            var go = new GameObject("Box collider"); go.transform.SetParent(parent, false); go.transform.SetPositionAndRotation(center, rot);
            go.AddComponent<BoxCollider>().size = size; go.isStatic = true;
        }

        static float Ease(float a, float b, float x) { var t = Mathf.Clamp01((x - a) / (b - a)); return t * t * (3f - 2f * t); }

        // ---------------------------------------------------------------- shaping the ground
        public override void ShapeGround(IslandWorldData data, int index)
        {
            var isl = data.layout.islands[index]; heart2 = new Vector2(isl.labelX, isl.labelZ); waterY = data.layout.waterLevel;
            bool Inside(Vector2 p, float m) { if (data.IslandAt(p.x, p.y) != isl || data.HeightAt(p.x, p.y) < waterY + 2f) return false; for (var k = 0; k < 16; k++) { var a = k * Mathf.PI / 8f; if (data.IslandAt(p.x + Mathf.Cos(a) * m, p.y + Mathf.Sin(a) * m) != isl) return false; } return true; }
            // the Mothership lands where there is room for her and for Funk Street the other way
            hasShip = false; dir2 = new Vector2(0, 1);
            foreach (var dist in new[] { 92f, 80f, 104f })
            {
                for (var k = 0; k < 24 && !hasShip; k++)
                {
                    var a = (90f + (k % 2 == 0 ? 1f : -1f) * (k / 2) * 15f) * Mathf.Deg2Rad; var d = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                    if (Inside(heart2 + d * dist, SaucerR + 44f) && Inside(heart2 - d * (24f + StreetLen * .5f), 46f) && Inside(heart2 - d * (24f + StreetLen), 40f)) { dir2 = d; ship2 = heart2 + d * dist; hasShip = true; }
                }
                if (hasShip) break;
            }
            if (!hasShip) ship2 = heart2 + dir2 * 80f;
            side2 = new Vector2(dir2.y, -dir2.x); streetEnd2 = heart2 - dir2 * (24f + StreetLen);

            // Parliament and Funkadelic either side of the saucer
            var keep = new List<string>();
            void Put(string name, Vector2 p, float room) { if (!Inside(p, room)) return; foreach (var a in data.layout.artists) if (a.sceneId == isl.sceneId && a.name == name) { a.x = p.x; a.z = p.y; keep.Add(name); } }
            Put("Parliament", ship2 + side2 * 50f + dir2 * 6f, 30f); Put("Funkadelic", ship2 - side2 * 50f + dir2 * 6f, 30f);
            var avoid = new List<Vector3> { new Vector3(ship2.x, SaucerR + 16f, ship2.y) };
            for (var d = 18f; d <= 24f + StreetLen + 26f; d += 16f) avoid.Add(new Vector3(heart2.x - dir2.x * d, d > 24f + StreetLen ? 34f : 26f, heart2.y - dir2.y * d));
            for (var d = 20f; d < Vector2.Distance(heart2, ship2) - SaucerR; d += 16f) avoid.Add(new Vector3(heart2.x + dir2.x * d, 9f, heart2.y + dir2.y * d));
            ArtistPlacement.Spread(data, index, 56f, heart2, 44f, 40f, avoid, keep, Land, 330f);

            Level(data, index, heart2, 20f, 34f);
            Level(data, index, ship2, SaucerR + 10f, SaucerR + 28f);
            for (var d = 30f; d <= 24f + StreetLen; d += 11f) Level(data, index, heart2 - dir2 * d, 19f, 31f);
            Level(data, index, streetEnd2 - dir2 * 22f, 26f, 40f);   // the rink at the end of the street
            foreach (var a in data.layout.artists)
            {
                if (a.sceneId != isl.sceneId) continue;
                a.r = Mathf.Max(a.r, Foot(a) + 6f);
                Level(data, index, new Vector2(a.x, a.z), Foot(a) + 1f, Foot(a) + 10f);
            }
        }

        void Level(IslandWorldData data, int index, Vector2 c, float flat, float blend)
        {
            var level = Mathf.Max(data.HeightAt(c.x, c.y), data.layout.waterLevel + 2.6f);
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
            center = ctx.Plaza; toShip = new Vector3(dir2.x, 0, dir2.y); sideways = new Vector3(side2.x, 0, side2.y);
            life = new GameObject("Funk life").AddComponent<FunkLife>();
            life.transform.SetParent(ctx.Root, false);
            life.center = center; life.flora = floraM; life.tiles = tileM; life.fruit = fruitM;
            var mk = new MeshKit(); var solid = new MeshKit();
            Remember(ctx.OnGround(center.x, center.z), 0);
            PlacesStart(ctx);
            Arrival(ctx, mk, solid);
            TheOne(ctx, mk, solid);
            Mothership(ctx, mk, solid);
            FunkStreet(ctx, mk, solid);
            Paths(ctx, mk, solid);
            Wilds(ctx, mk, solid);
            mk.Build("The Funk Tropics", ctx.Root, false);
            solid.BuildCollider("The Funk Tropics walls", ctx.Root);
        }

        public override void Finish(LandContext ctx) { Undergrowth(ctx); halos.Build("Tropics glow", ctx.Root); }

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

        bool Open(LandContext ctx, float x, float z, float pad) =>
            ctx.IsLand(x, z, 5f) && ctx.Ground(x, z) > waterY + 2.2f && !InFootprint(ctx, x, z, pad) && !NearPath(x, z, pad + 2f) && !InNoGrass(x, z, pad);

        void Strip(MeshKit mk, List<Vector3> pts, Material m, float w, float lift, LandContext ground)
        {
            var n = pts.Count; var L = new Vector3[n]; var R = new Vector3[n];
            Vector3 Flat(Vector3 v) { v.y = 0; return v.normalized; }
            for (var i = 0; i < n; i++)
            {
                var a = i > 0 ? Flat(pts[i] - pts[i - 1]) : Flat(pts[1] - pts[0]);
                var b = i < n - 1 ? Flat(pts[i + 1] - pts[i]) : Flat(pts[n - 1] - pts[n - 2]);
                var t = (a + b).normalized; var side = Vector3.Cross(Vector3.up, t);
                var miter = 1f / Mathf.Max(.6f, Vector3.Dot(side, Vector3.Cross(Vector3.up, b)));
                L[i] = pts[i] - side * (w / 2f * miter); R[i] = pts[i] + side * (w / 2f * miter);
                float gl = ground.Ground(L[i].x, L[i].z), gr = ground.Ground(R[i].x, R[i].z), bulge = Mathf.Clamp(pts[i].y - (gl + gr) / 2f, 0f, .3f);
                L[i].y = gl + bulge + lift; R[i].y = gr + bulge + lift;
            }
            for (var i = 0; i + 1 < n; i++) mk.Quad(m, L[i], R[i], R[i + 1], L[i + 1], (pts[i] + pts[i + 1]) / 2f - Vector3.up);
        }

        /// A mirror-ball fruit hanging from a bent stalk: the land's lamp.
        void FruitLamp(LandContext ctx, MeshKit mk, Vector3 g, Vector3 lean, bool light = false, int colour = 0)
        {
            var up = Vector3.up; lean.y = 0; lean = lean.sqrMagnitude > .01f ? lean.normalized : Vector3.forward;
            mk.Prism(trunk, g - up * .2f, up, .09f, 3.2f, 6); mk.Prism(trunk, g + up * 3f, (up * .5f + lean).normalized, .07f, 1.3f, 6); var tip = g + up * 3.6f + lean * 1.1f; mk.Prism(trunk, tip, -up, .02f, .5f, 4);
            mk.Blob(fruitM, tip - up * .75f, Vector3.one * .3f, new System.Random(3), .12f); mk.Blob(leaf, g + up * 3.2f, new Vector3(.5f, .12f, .5f), new System.Random(5), .3f);
            halos.Add(tip - up * .75f, 2.2f, Neon[colour % Neon.Length], .55f); ArtistBits.PostCollider(ctx.Root, g, 3.2f, .12f);
            if (light) ArtistBits.PointLight(ctx.Root, tip - up * 1f, Color.Lerp(Neon[colour % Neon.Length], Color.white, .4f), 17f, 2.4f);
        }

        /// A palm: a leaning ringed trunk, a crown of dark fronds, some with a glowing rib; mirror-ball fruit under the crown.
        void Palm(LandContext ctx, MeshKit mk, Vector3 g, float s, System.Random rng, bool fruit)
        {
            var up = Vector3.up; var lean = Quaternion.Euler(0, (float)rng.NextDouble() * 360f, 0) * Vector3.forward * (.1f + (float)rng.NextDouble() * .16f); var axis = (up + lean).normalized; var h = (5.5f + (float)rng.NextDouble() * 3f) * s;
            mk.Prism(trunk, g - up * .3f, axis, .26f * s, h + .3f, 7, .6f); ArtistBits.PostCollider(ctx.Root, g, Mathf.Min(h, 4f), .28f * s);
            var top = g + axis * h; var glowRib = floraM[rng.Next(3)];
            for (var k = 0; k < 7; k++)
            {
                var d = Quaternion.Euler(0, k * 51f + (float)rng.NextDouble() * 20f, 0) * Vector3.forward; var droop = (d + up * (.35f - (k % 3) * .25f)).normalized; var len = (2.6f + (float)rng.NextDouble()) * s;
                mk.Box(k % 2 == 0 ? leafDark : leaf, top + droop * len * .5f, new Vector3(.9f * s, .06f, len), Quaternion.LookRotation(droop, up));
                if (k % 2 == 0) mk.Box(glowRib, top + droop * len * .5f + up * .04f, new Vector3(.12f, .04f, len * .9f), Quaternion.LookRotation(droop, up));
            }
            if (fruit) for (var k = 0; k < 3; k++) { var fp = top - up * (.5f + k * .25f) + Quaternion.Euler(0, k * 120f, 0) * Vector3.forward * .4f * s; mk.Blob(fruitM, fp, Vector3.one * .2f, rng, .12f); if (k == 0) halos.Add(fp, 1.4f, Neon[rng.Next(Neon.Length)], .4f); }
        }

        /// A glowing plant: a fan of broad leaves round a bright bud. kind 0 purple, 1 lime, 2 orange.
        void GlowPlant(MeshKit mk, Vector3 g, float s, int kind, System.Random rng)
        {
            var up = Vector3.up; kind = ((kind % 3) + 3) % 3; var m = floraM[kind];
            for (var k = 0; k < 5; k++) { var d = Quaternion.Euler(0, k * 72f + (float)rng.NextDouble() * 30f, 0) * Vector3.forward; var out1 = (d + up * .8f).normalized; mk.Box(leafDark, g + out1 * .6f * s, new Vector3(.5f * s, .04f, 1.2f * s), Quaternion.LookRotation(out1, up)); mk.Box(m, g + out1 * .6f * s + up * .03f, new Vector3(.1f, .03f, 1.05f * s), Quaternion.LookRotation(out1, up)); }
            mk.Prism(leaf, g, up, .04f * s, .9f * s, 5); mk.Blob(m, g + up * 1f * s, Vector3.one * .26f * s, rng, .15f); halos.Add(g + up * 1f * s, 1.1f * s, Neon[kind == 0 ? 0 : kind == 1 ? 2 : 1], .4f);
        }

        /// A giant platform shoe left in the jungle, sole up enough to step or sit on.
        void PlatformShoe(MeshKit mk, MeshKit solid, Vector3 g, Vector3 face, int i)
        {
            var up = Vector3.up; var rot = Quaternion.LookRotation(face); var c = WorldMaterials.Solid(Neon[i % Neon.Length] * .75f, .5f); var sole = WorldMaterials.Solid(new Color(.9f, .85f, .7f));
            mk.Box(sole, g + up * .35f, new Vector3(1.3f, .7f, 3.2f), rot); solid.Box(sole, g + up * .35f, new Vector3(1.3f, .7f, 3.2f), rot); mk.Box(sole, g + up * 1f - face * 1f, new Vector3(1.2f, .7f, 1.1f), rot); solid.Box(sole, g + up * .7f - face * 1f, new Vector3(1.2f, 1.4f, 1.1f), rot);
            mk.Box(c, g + up * 1.1f + face * .5f, new Vector3(1.25f, .7f, 2f), rot * Quaternion.Euler(14f, 0, 0)); mk.Box(c, g + up * 1.9f - face * 1.1f, new Vector3(1.2f, 1.1f, .9f), rot); mk.Blob(GlowM(Gold, 1.4f), g + up * 1.5f + face * .2f, new Vector3(.2f, .2f, .2f), new System.Random(i), 0f);
            Interact.Seat(plife.transform, g + up * .72f + face * 1.3f, face, "Sit on the giant shoe");
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
                var pts = new List<Vector3>();
                for (var d = PathW / 2f + .1f; ; d += 1.5f)
                {
                    var dd = Mathf.Min(d, end); var q = from + dir * dd + side * Mathf.Sin(dd / 12f) * Mathf.Min(3f, end * .05f);
                    if (!ctx.IsLand(q.x, q.z, 0f)) break;
                    pts.Add(ctx.OnGround(q.x, q.z)); if (dd >= end) break;
                }
                if (pts.Count < 2) return;
                Strip(mk, pts, earth, PathW, .14f, ctx);
                foreach (var p in pts) Remember(p, walk);
                // glowing studs down both edges, a fruit lamp now and then
                for (var i = 2; i < pts.Count; i += 3) { var a = pts[Mathf.Min(i + 1, pts.Count - 1)] - pts[i - 1]; a.y = 0; if (a.sqrMagnitude < .01f) continue; var sd = Vector3.Cross(Vector3.up, a.normalized); foreach (var e in new[] { -1f, 1f }) { var sp = pts[i] + sd * e * (PathW / 2f - .15f); mk.Blob(tileM[(i / 3 + walk) % 4], new Vector3(sp.x, ctx.Ground(sp.x, sp.z) + .17f, sp.z), new Vector3(.1f, .04f, .1f), new System.Random(i), 0f); } }
                for (var i = 8; i < pts.Count; i += 13)
                {
                    var s = (i / 13) % 2 == 0 ? 1f : -1f; var lp = pts[i] + side * s * (PathW / 2f + .9f);
                    var near = false; foreach (var o in lamps) if ((o - lp).sqrMagnitude < 17f * 17f) near = true;
                    if (near || InFootprint(ctx, lp.x, lp.z, 1f) || InNoGrass(lp.x, lp.z, 1f)) continue;
                    lamps.Add(lp); FruitLamp(ctx, mk, ctx.OnGround(lp.x, lp.z), -side * s, true, lamps.Count);
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

        // ---------------------------------------------------------------- the jungle
        /// Palms (some hung with mirror-ball fruit), glowing plants in drifts of one colour, vines strung between posts
        /// like bass strings to pluck, giant platform shoes, old speaker stacks the jungle has grown over.
        void Wilds(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            int palms = 0, plants = 0, vines = 0, shoes = 0, stacks = 0; var rng = new System.Random(1975);
            const float cell = 17f;
            for (var x = ctx.MinX; x < ctx.MaxX; x += cell)
            for (var z = ctx.MinZ; z < ctx.MaxZ; z += cell)
            {
                float px = x + (float)rng.NextDouble() * cell, pz = z + (float)rng.NextDouble() * cell; var roll = (float)rng.NextDouble(); var yaw = (float)rng.NextDouble() * 360f; var s = (float)rng.NextDouble();
                if (!Open(ctx, px, pz, 3f)) continue;
                var p = ctx.OnGround(px, pz); var face = Quaternion.Euler(0, yaw, 0) * Vector3.forward;
                var kind = Mathf.Clamp(Mathf.FloorToInt(Mathf.PerlinNoise(px * .012f + 5f, pz * .012f + 9f) * 2.999f), 0, 2);   // the colour of this part of the jungle
                if (roll < .42f) { Palm(ctx, mk, p, .85f + s * .5f, rng, roll < .14f); palms++; }
                else if (roll < .72f) { for (var k = 0; k < 3; k++) { var q = ctx.OnGround(px + ((float)rng.NextDouble() - .5f) * 7f, pz + ((float)rng.NextDouble() - .5f) * 7f); if (Open(ctx, q.x, q.z, 1f)) { GlowPlant(mk, q, .8f + (float)rng.NextDouble() * .8f, kind, rng); plants++; } } }
                else if (roll < .76f && vines < 14 && Open(ctx, px, pz, 6f)) { BassVines(ctx, mk, p, face, vines); vines++; }
                else if (roll < .79f && shoes < 10) { PlatformShoe(mk, solid, p, face, shoes); shoes++; noGrass.Add(new Vector3(p.x, 2.5f, p.z)); }
                else if (roll < .83f && stacks < 12) { SpeakerStack(ctx, mk, p, FaceTo(p, NearestWalk(p)), stacks); stacks++; }
            }
            Debug.Log($"The Funk Tropics: mothership {(hasShip ? "yes" : "no")}, {palms} palms, {plants} glowing plants, {vines} bass vines, {shoes} platform shoes, {stacks} speaker stacks");
        }

        Vector3 NearestWalk(Vector3 p) { var best = center; var bd = float.MaxValue; foreach (var (q, _) in walkPoints) { var d = (q - p).sqrMagnitude; if (d < bd) { bd = d; best = q; } } return best; }

        /// Four vines strung taut between two leaning trunks, thick to thin like a bass: pluck them.
        void BassVines(LandContext ctx, MeshKit mk, Vector3 g, Vector3 face, int i)
        {
            var up = Vector3.up; var side = Vector3.Cross(up, face); var a = ctx.OnGround(g.x - side.x * 3f, g.z - side.z * 3f); var b = ctx.OnGround(g.x + side.x * 3f, g.z + side.z * 3f);
            foreach (var e in new[] { a, b }) { mk.Prism(trunk, e - up * .3f, up, .2f, 3.6f, 6); ArtistBits.PostCollider(ctx.Root, e, 3.4f, .22f); mk.Blob(leaf, e + up * 3.5f, new Vector3(.8f, .4f, .8f), new System.Random(i), .3f); }
            var vm = new MeshKit(); var gm = new Material(WorldMaterials.Glow(Neon[(i * 2 + 2) % Neon.Length], .9f)) { name = "Bass vine" };
            for (var k = 0; k < 4; k++) { var y = .9f + k * .6f; var p0 = a + up * y - g; var p1 = b + up * y - g; vm.Prism(gm, p0, (p1 - p0).normalized, .05f - k * .009f, Vector3.Distance(p0, p1), 5); }
            var vgo = vm.BuildSingle("Bass vines", plife.transform, false); vgo.transform.position = g; plife.vines.Add((gm, Neon[(i * 2 + 2) % Neon.Length], -99f));
            string[] notes = { "Dooom. The low one. You feel it in the soles of your feet.", "Bow-bow-bip. Somewhere in the dark a drummer you can't see comes in right on the one.", "You slap the thick vine and pop the thin one. The plants all lean in.", "One note, held. The whole jungle waits for the next one." }; var idx = plife.vines.Count - 1; var nn = i;
            Interact.Toy(vgo, Toy.Kind.Custom, "Pluck the bass vines", new Vector3(6f, 3f, 1.2f)).onUse = _ => { var v = plife.vines[idx]; plife.vines[idx] = (v.m, v.c, Time.time); life.plucked = Time.time; Say(plife.transform, g + up * 3.6f, notes[nn++ % notes.Length], 4f); };
            noGrass.Add(new Vector3(g.x, 4f, g.z));
        }

        /// An old speaker stack the jungle has half taken: two cabinets, cones still glowing faintly, leaves over the top.
        void SpeakerStack(LandContext ctx, MeshKit mk, Vector3 g, Vector3 face, int i)
        {
            var up = Vector3.up; var rot = Quaternion.LookRotation(face) * Quaternion.Euler(0, 0, (i % 3 - 1) * 3f);
            for (var k = 0; k < 2; k++) { mk.Box(dark, g + up * (.75f + k * 1.5f), new Vector3(1.3f, 1.45f, .8f), rot); mk.Prism(floraM[(i + k) % 3], g + up * (.75f + k * 1.5f) + face * .41f, face, .46f, .03f, 14); mk.Prism(dark, g + up * (.75f + k * 1.5f) + face * .43f, face, .2f, .03f, 10); }
            BoxCol(ctx.Root, g + up * 1.5f, new Vector3(1.3f, 3f, .8f), rot); mk.Blob(leaf, g + up * 3.1f - face * .2f, new Vector3(1f, .3f, .7f), new System.Random(i), .35f); halos.Add(g + up * 1.5f + face * .8f, 1.6f, Neon[i % 3 == 0 ? 0 : i % 3 == 1 ? 2 : 1], .35f);
        }

        Vector3 G0(Vector3 p) => ctx0.OnGround(p.x, p.z);

        /// The jungle floor: ferns and broad dark leaves that sway, with glowing tips in the colour of that part of the jungle.
        void Undergrowth(LandContext ctx)
        {
            var mat = WindMesh.Material(.28f, .8f, new Color(.08f, .26f, .16f));
            var meshes = new Dictionary<(int, int), WindMesh>(); var rng = new System.Random(1979);
            Color[] tips = { new Color(.75f, .45f, 1f), new Color(.5f, 1f, .5f), new Color(1f, .6f, .25f) };
            const float st = 2.4f;
            for (var x = ctx.MinX; x < ctx.MaxX; x += st)
            for (var z = ctx.MinZ; z < ctx.MaxZ; z += st)
            {
                var px = x + ((float)rng.NextDouble() - .5f) * st; var pz = z + ((float)rng.NextDouble() - .5f) * st;
                if (!ctx.IsLand(px, pz, 1f) || ctx.Ground(px, pz) < waterY + 2.2f || NearPath(px, pz, 2.4f) || InNoGrass(px, pz, 0f) || InFootprint(ctx, px, pz, -2f)) continue;
                if (Mathf.PerlinNoise(px * .05f + 1f, pz * .05f + 4f) < .3f) continue;
                var key = (Mathf.FloorToInt(px / 70f), Mathf.FloorToInt(pz / 70f));
                if (!meshes.TryGetValue(key, out var wm)) meshes[key] = wm = new WindMesh();
                var kind = Mathf.Clamp(Mathf.FloorToInt(Mathf.PerlinNoise(px * .012f + 5f, pz * .012f + 9f) * 2.999f), 0, 2);
                var col = Color.Lerp(new Color(.05f, .2f, .14f), new Color(.1f, .32f, .18f), (float)rng.NextDouble());
                wm.Tuft(new Vector3(px, ctx.Ground(px, pz) - .04f, pz), .4f + (float)rng.NextDouble() * .5f, col, rng.NextDouble() < .35 ? tips[kind] : Color.Lerp(col, tips[kind], .25f), rng, 3, .07f);
            }
            foreach (var kv in meshes) kv.Value.Build("Tropics undergrowth", ctx.Root, mat);
        }
    }
}
