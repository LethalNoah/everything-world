using System.Collections.Generic;
using UnityEngine;
using static EverythingWorld.World.PlaceTools;

namespace EverythingWorld.World
{
    /// The Afrobeat Coast (WORLD_GUIDE.md): a coastal city at a blazing sunset, a street party that is turning into a
    /// beach party. You arrive at the roundabout on the hilltop, round the Big Drum, among bright painted houses.
    /// Market Road runs west from it, downhill all the way to the sea, straight at a huge orange sun going into the
    /// water under a purple sky: stalls of printed cloth, fruit, drums and grilling meat down both sides, strings of
    /// coloured bulbs over the red-earth road, a yellow bus going up and down. Beside the roundabout is the Shrine,
    /// the open-air club. At the bottom of the road is the beach: bonfires, drummers in a circle, painted canoes,
    /// a football pitch in the sand.
    /// Laid out on purpose (the build-land standard): AfroTown.cs has the places and the people.
    public partial class AfroKit : LandKit
    {
        public override string Land => "The Afrobeat Coast";
        public override Vector3 ArtistFront(IslandWorldData data, IslandWorldData.Artist a) => facing.TryGetValue(a.name, out var f) ? f : base.ArtistFront(data, a);

        public static readonly Color Orange = new Color(1f, .52f, .12f), Gold = new Color(1f, .82f, .25f), Green = new Color(.15f, .62f, .3f), Purple = new Color(.55f, .28f, .75f), Pink = new Color(1f, .36f, .6f), Cyan = new Color(.2f, .8f, .85f), Cream = new Color(.97f, .93f, .82f), Ink = new Color(.1f, .07f, .1f);
        static readonly Color[] Paints = { new Color(.95f, .76f, .2f), new Color(.2f, .7f, .68f), new Color(.92f, .4f, .5f), new Color(.94f, .92f, .84f), new Color(.3f, .62f, .32f), new Color(.9f, .5f, .2f), new Color(.45f, .5f, .85f) };
        static readonly Color[] Cloth = { new Color(.95f, .3f, .15f), new Color(1f, .8f, .15f), new Color(.1f, .55f, .3f), new Color(.2f, .3f, .75f), new Color(.6f, .2f, .6f), new Color(1f, .5f, .1f), new Color(.1f, .1f, .12f), new Color(.95f, .95f, .9f) };
        const float PathW = 3f, RoadW = 7f;

        Material earth, sand, concrete, tin, rustM, iron, dark, white, woodM, palmTrunk, leafM, leafDark, fireM, emberM, sunM, sunRoadM, windowM;
        // the strings of bulbs: three colours, so they can chase
        readonly Material[] bulbM = new Material[3];
        GlowHalos halos; Vector3 center, U, R; AfroLife life;
        readonly Dictionary<(int, int), List<Vector3>> pathCells = new Dictionary<(int, int), List<Vector3>>();
        readonly List<(Vector3 p, int w)> walkPoints = new List<(Vector3, int)>();
        readonly Dictionary<string, Vector3> facing = new Dictionary<string, Vector3>();
        // x, radius, z
        readonly List<Vector3> noGrass = new List<Vector3>();
        Vector2 heart2, U2 = new Vector2(0, 1), R2 = new Vector2(1, 0), beach2; float plazaY, waterY, roadL;

        void MakeMaterials()
        {
            // the road, the sand and the walls are faintly self-lit, so dusk never swallows the town
            earth = WorldMaterials.Glow(new Color(.72f, .36f, .2f), .16f); sand = WorldMaterials.Glow(new Color(.92f, .76f, .5f), .14f); concrete = WorldMaterials.Glow(new Color(.7f, .66f, .62f), .1f);
            tin = WorldMaterials.Solid(new Color(.62f, .62f, .64f), .7f); rustM = WorldMaterials.Solid(new Color(.6f, .32f, .18f)); iron = WorldMaterials.Solid(new Color(.14f, .13f, .15f), .5f); dark = WorldMaterials.Solid(new Color(.07f, .06f, .08f));
            white = WorldMaterials.Glow(new Color(.95f, .93f, .88f), .1f); woodM = WorldMaterials.Solid(new Color(.5f, .34f, .22f)); palmTrunk = WorldMaterials.Solid(new Color(.46f, .36f, .28f));
            leafM = WorldMaterials.Glow(new Color(.16f, .5f, .26f), .08f); leafDark = WorldMaterials.Solid(new Color(.1f, .34f, .22f));
            fireM = new Material(WorldMaterials.Glow(new Color(1f, .55f, .12f), 2.4f)) { name = "Afro fires" }; emberM = new Material(WorldMaterials.Glow(new Color(1f, .25f, .05f), 1.4f)) { name = "Afro embers" };
            sunM = new Material(WorldMaterials.Glow(new Color(1f, .5f, .14f), 2.6f)) { name = "Afro sun" }; sunRoadM = new Material(WorldMaterials.Glow(new Color(1f, .5f, .2f), .9f)) { name = "Afro sun road" };
            windowM = new Material(WorldMaterials.Glow(new Color(1f, .8f, .45f), 1.2f)) { name = "Afro windows" };
            var cols = new[] { Orange, Gold, Pink }; for (var i = 0; i < 3; i++) bulbM[i] = new Material(WorldMaterials.Glow(cols[i], 1.9f)) { name = "Afro bulbs " + i };
        }

        Material GlowM(Color c, float s = 2f) => WorldMaterials.Glow(c, s * .65f);
        Material Paint(Color c) => WorldMaterials.Glow(c, .12f);
        static bool HasStation(IslandWorldData.Artist a) => a.stationId != 0;
        static float Foot(IslandWorldData.Artist a)
        {
            switch (a.name)
            {
                case "Fela Kuti": return 16f;
                case "Burna Boy": return 13f;
                case "Wizkid": return 12f;
                case "William Onyeabor": return 11f;
                default: return HasStation(a) ? 12f : 10f;
            }
        }

        public override bool Ground(float height, float slope, float patch, out Color color)
        {
            // red earth with green patches; sand down by the water
            if (height < 3.2f) color = Color.Lerp(new Color(.9f, .72f, .46f), new Color(.82f, .62f, .4f), patch);
            else if (slope > 26f) color = Color.Lerp(new Color(.5f, .24f, .16f), new Color(.6f, .3f, .18f), patch);
            else color = patch > .62f ? Color.Lerp(new Color(.26f, .44f, .22f), new Color(.2f, .38f, .22f), patch) : Color.Lerp(new Color(.66f, .32f, .18f), new Color(.74f, .4f, .2f), Mathf.SmoothStep(0f, 1f, patch / .62f));
            color.a = 1f;
            return true;
        }

        static float Ease(float a, float b, float x) { var t = Mathf.Clamp01((x - a) / (b - a)); return t * t * (3f - 2f * t); }
        Vector2 P2(float u, float r) => heart2 + U2 * u + R2 * r;
        Vector3 Wp(float u, float r) => center + U * u + R * r;
        Vector3 G(LandContext ctx, float u, float r) { var p = Wp(u, r); return ctx.OnGround(p.x, p.z); }

        // ---------------------------------------------------------------- shaping the ground
        public override void ShapeGround(IslandWorldData data, int index)
        {
            var isl = data.layout.islands[index]; heart2 = new Vector2(isl.labelX, isl.labelZ); waterY = data.layout.waterLevel;
            bool Inside(Vector2 p, float m) { if (data.IslandAt(p.x, p.y) != isl) return false; for (var k = 0; k < 12; k++) { var a = k * Mathf.PI / 6f; if (data.IslandAt(p.x + Mathf.Cos(a) * m, p.y + Mathf.Sin(a) * m) != isl) return false; } return true; }
            // Market Road runs west, downhill, until it reaches the sand
            roadL = 60f; while (roadL < 340f && data.IslandAt(heart2.x - roadL - 30f, heart2.y) == isl && data.HeightAt(heart2.x - roadL, heart2.y) > waterY + 3f) roadL += 6f;
            beach2 = heart2 - R2 * (roadL + 14f);
            plazaY = Mathf.Max(data.HeightAt(heart2.x, heart2.y), waterY + 6f);

            var keep = new List<string>();
            void Put(string name, float u, float rr, float room) { var p = P2(u, rr); if (!Inside(p, room)) return; foreach (var a in data.layout.artists) if (a.sceneId == isl.sceneId && a.name == name) { a.x = p.x; a.z = p.y; keep.Add(name); } }
            // round the roundabout: the Shrine, and the biggest stage
            Put("Fela Kuti", 50f, 28f, 30f); Put("Burna Boy", -46f, 30f, 28f);
            // down Market Road, either side, as far as it goes
            Put("Wizkid", -36f, -62f, 26f); Put("William Onyeabor", 36f, -54f, 26f);
            if (roadL > 150f) { Put("CKay", -33f, -124f, 24f); Put("Ayra Starr", 34f, -114f, 24f); }
            if (roadL > 210f) { Put("Davido", -34f, -184f, 24f); Put("Manu Dibango", 34f, -174f, 24f); }
            if (roadL > 270f) { Put("Letta Mbulu", -32f, -242f, 22f); Put("Diamond Platnumz", 33f, -234f, 22f); }
            Put("I Hate My Village", 98f, -32f, 24f); Put("Nomo", -98f, -22f, 24f);
            var avoid = new List<Vector3> { new Vector3(beach2.x, 40f, beach2.y) };
            for (var d = 0f; d <= roadL; d += 22f) avoid.Add(new Vector3(heart2.x - d, 12f, heart2.y));
            ArtistPlacement.Spread(data, index, 56f, heart2, 44f, 42f, avoid, keep, Land, 330f);

            Level(data, index, heart2, 24f, 44f, plazaY);
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
            center = ctx.Plaza; U = new Vector3(0, 0, 1); R = new Vector3(1, 0, 0);
            life = new GameObject("Afro life").AddComponent<AfroLife>();
            life.transform.SetParent(ctx.Root, false);
            life.center = center; life.bulbs = bulbM; life.fire = fireM; life.embers = emberM; life.sun = sunM; life.sunRoad = sunRoadM; life.windows = windowM;
            var mk = new MeshKit(); var solid = new MeshKit();
            Remember(ctx.OnGround(center.x, center.z), 0);
            TownStart(ctx);
            Roundabout(ctx, mk, solid);
            MarketRoad(ctx, mk, solid);
            Beach(ctx, mk, solid);
            Paths(ctx, mk, solid);
            Stalls(ctx, mk, solid);
            Houses(ctx, mk, solid);
            Wilds(ctx, mk, solid);
            Sunset(ctx, mk);
            mk.Build("The Afrobeat Coast", ctx.Root, false);
            solid.BuildCollider("The Afrobeat Coast walls", ctx.Root);
        }

        public override void Finish(LandContext ctx) { Verge(ctx); halos.Build("Afrobeat glow", ctx.Root); }

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

        /// A pole with a coloured bulb on it.
        void BulbPole(LandContext ctx, MeshKit mk, Vector3 g, int phase, bool light = false)
        {
            var up = Vector3.up; mk.Prism(woodM, g - up * .3f, up, .07f, 3.6f, 5, .8f); mk.Blob(bulbM[phase % 3], g + up * 3.4f, Vector3.one * .16f, new System.Random(phase), 0f); halos.Add(g + up * 3.4f, 1.5f, phase % 3 == 0 ? Orange : phase % 3 == 1 ? Gold : Pink, .5f); ArtistBits.PostCollider(ctx.Root, g, 3.3f, .1f);
            if (light) ArtistBits.PointLight(ctx.Root, g + up * 3f, new Color(1f, .7f, .45f), 14f, 2f);
        }

        /// A string of coloured bulbs from a to b, sagging; they take the three bulb materials in turn.
        void BulbString(MeshKit mk, Vector3 a, Vector3 b, int phase = 0)
        {
            var len = Vector3.Distance(a, b); var n = Mathf.Max(3, Mathf.RoundToInt(len / 1.1f)); var sag = Mathf.Min(1.2f, len * .07f); var prev = a;
            for (var i = 1; i <= n; i++)
            {
                var t = i / (float)n; var p = Vector3.Lerp(a, b, t) - Vector3.up * Mathf.Sin(t * Mathf.PI) * sag;
                mk.Prism(dark, prev, (p - prev).normalized, .012f, Vector3.Distance(prev, p), 3); prev = p;
                if (i < n) { mk.Blob(bulbM[(i + phase) % 3], p - Vector3.up * .08f, Vector3.one * .085f, new System.Random(i), 0f); if (i % 4 == 0) halos.Add(p - Vector3.up * .08f, .7f, (i + phase) % 3 == 0 ? Orange : Gold, .3f); }
            }
        }

        /// A coconut palm, leaning, black against the sunset.
        void Palm(LandContext ctx, MeshKit mk, Vector3 g, float s, System.Random rng)
        {
            var up = Vector3.up; var lean = Quaternion.Euler(0, (float)rng.NextDouble() * 360f, 0) * Vector3.forward * (.12f + (float)rng.NextDouble() * .2f); var p = g - up * .3f; var d = (up + lean).normalized;
            for (var k = 0; k < 5; k++) { var len = 1.5f * s; mk.Prism(palmTrunk, p, d, (.2f - k * .02f) * s, len + .05f, 6, .9f); p += d * len; d = (d + lean * .3f).normalized; }
            for (var k = 0; k < 8; k++) { var fd = Quaternion.Euler(0, k * 45f + (float)rng.NextDouble() * 20f, 0) * Vector3.forward; mk.Box(k % 2 == 0 ? leafM : leafDark, p + fd * 1.2f * s - up * .2f * s, new Vector3(.5f * s, .05f, 2.6f * s), Quaternion.LookRotation(fd) * Quaternion.Euler(22f, 0, 0)); }
            for (var k = 0; k < 3; k++) mk.Blob(woodM, p - up * .3f * s + Quaternion.Euler(0, k * 120f, 0) * Vector3.forward * .2f * s, Vector3.one * .15f * s, rng, 0f);
            ArtistBits.PostCollider(ctx.Root, g, 3f * s, .22f * s);
        }

        /// A bench to sit on: a plank on two blocks, painted.
        void Bench(MeshKit mk, Vector3 g, Vector3 face, int i, float len = 2f)
        {
            var up = Vector3.up; var rot = Quaternion.LookRotation(face); var side = Vector3.Cross(up, face);
            mk.Box(Paint(Paints[i % Paints.Length]), g + up * .45f, new Vector3(len, .08f, .46f), rot); foreach (var e in new[] { -1f, 1f }) mk.Box(concrete, g + up * .21f + side * e * (len / 2f - .25f), new Vector3(.3f, .42f, .4f), rot);
            BoxCol(rootT, g + up * .25f, new Vector3(len, .5f, .46f), rot); Interact.Bench(tlife.transform, g + up * .5f - side * (len / 2f - .4f), g + up * .5f + side * (len / 2f - .4f), face);
        }

        /// A plastic chair to sit on.
        void Chair(MeshKit mk, Vector3 g, Vector3 face, int i)
        {
            var up = Vector3.up; var rot = Quaternion.LookRotation(face); var m = Paint(i % 2 == 0 ? Cream : Paints[i % Paints.Length]);
            mk.Box(m, g + up * .44f, new Vector3(.46f, .05f, .46f), rot); mk.Box(m, g + up * .76f - face * .21f, new Vector3(.46f, .6f, .05f), rot); foreach (var (x, zz) in new[] { (-.2f, -.2f), (.2f, -.2f), (.2f, .2f), (-.2f, .2f) }) mk.Box(m, g + up * .22f + rot * new Vector3(x, 0, zz), new Vector3(.04f, .44f, .04f), rot);
            Interact.Seat(tlife.transform, g + up * .5f, face);
        }

        /// A talking drum: an hourglass with cords. 'o' is its foot.
        void Drum(MeshKit mk, Vector3 o, float s, Material skin = null)
        {
            var up = Vector3.up; mk.Prism(woodM, o, up, .2f * s, .3f * s, 10, .55f); mk.Prism(woodM, o + up * .3f * s, up, .11f * s, .3f * s, 10, 1.82f); mk.Prism(skin ?? white, o + up * .6f * s, up, .21f * s, .03f * s, 10);
            for (var k = 0; k < 8; k++) { var d = Quaternion.Euler(0, k * 45f, 0) * Vector3.forward; mk.Prism(dark, o + d * .2f * s + up * .02f, (up * .6f * s - d * .0f).normalized, .008f, .6f * s, 3); }
        }

        /// A bonfire: a ring of stones, crossed logs, flames, embers. Returns the top of the flames.
        Vector3 Bonfire(LandContext ctx, MeshKit mk, Vector3 g, float s, System.Random rng)
        {
            var up = Vector3.up; for (var k = 0; k < 8; k++) mk.Blob(concrete, g + Quaternion.Euler(0, k * 45f, 0) * Vector3.forward * .9f * s + up * .1f, Vector3.one * .2f * s, rng, .2f);
            for (var k = 0; k < 4; k++) { var d = Quaternion.Euler(0, k * 45f + 10f, 0) * Vector3.forward; mk.Prism(woodM, g - d * .8f * s + up * .12f, (d + up * .25f).normalized, .1f * s, 1.6f * s, 5); }
            mk.Blob(emberM, g + up * .16f, new Vector3(.5f, .08f, .5f) * s, rng, .1f); mk.Cone(fireM, g + up * .14f, .46f * s, 1.5f * s, 6); mk.Cone(fireM, g + up * .14f + Vector3.right * .22f * s, .28f * s, 1f * s, 5, 30f); mk.Cone(fireM, g + up * .14f - Vector3.forward * .2f * s, .24f * s, .8f * s, 5, 60f);
            halos.Add(g + up * .8f * s, 3.4f * s, Orange, .6f); ArtistBits.PointLight(ctx.Root, g + up * 1.4f, new Color(1f, .6f, .25f), 20f * s, 3f); ArtistBits.PostCollider(ctx.Root, g, 1f, .7f * s); life.sparks.Add(g + up * 1.2f * s);
            return g + up * 1.5f * s;
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
                Strip(mk, pts, earth, PathW, .13f, ctx);
                foreach (var p in pts) Remember(p, walk);
                for (var i = 7; i < pts.Count; i += 11, n++)
                {
                    var s = n % 2 == 0 ? 1f : -1f; var lp = pts[i] + side * s * (PathW / 2f + .8f);
                    if (InFootprint(ctx, lp.x, lp.z, 1f) || InNoGrass(lp.x, lp.z, 1f)) continue;
                    BulbPole(ctx, mk, ctx.OnGround(lp.x, lp.z), n, true);
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

        // ---------------------------------------------------------------- out of town
        /// Palms, plantain clumps, hibiscus, kiosks, goats, a motorbike left leaning.
        void Wilds(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            int palms = 0, plantains = 0, bushes = 0, goats = 0, bikes = 0, kiosks = 0; var rng = new System.Random(1970);
            const float cell = 20f;
            for (var x = ctx.MinX; x < ctx.MaxX; x += cell)
            for (var z = ctx.MinZ; z < ctx.MaxZ; z += cell)
            {
                float px = x + (float)rng.NextDouble() * cell, pz = z + (float)rng.NextDouble() * cell; var roll = (float)rng.NextDouble(); var s = (float)rng.NextDouble();
                if (!Open(ctx, px, pz, 4f)) continue;
                var p = ctx.OnGround(px, pz); var low = p.y < waterY + 5f;
                if (roll < (low ? .5f : .24f)) { Palm(ctx, mk, p, .9f + s * .5f, rng); palms++; }
                else if (low) continue;
                else if (roll < .34f) { Plantain(ctx, mk, p, .9f + s * .4f, rng); plantains++; }
                else if (roll < .44f) { Hibiscus(mk, p, rng); bushes++; }
                else if (roll < .47f) { if (goats < 9) { Goat(mk, p, Quaternion.Euler(0, s * 360f, 0), rng, goats); goats++; } }
                else if (roll < .56f) { if (bikes < 5 && NearPath(px, pz, 40f)) { Okada(ctx, mk, p, Quaternion.Euler(0, s * 360f, 0), bikes); bikes++; } }
                else if (roll < .66f) { if (kiosks < 6 && NearPath(px, pz, 44f)) { Kiosk(ctx, mk, solid, p, FaceTo(p, NearestWalk(p)), kiosks); kiosks++; } }
            }
            Debug.Log($"The Afrobeat Coast: Market Road {roadL:0} m, {palms} palms, {plantains} plantain clumps, {bushes} hibiscus, {goats} goats, {bikes} motorbikes, {kiosks} kiosks");
        }

        Vector3 NearestWalk(Vector3 p) { var best = center; var bd = float.MaxValue; foreach (var (q, _) in walkPoints) { var d = (q - p).sqrMagnitude; if (d < bd) { bd = d; best = q; } } return best; }

        void Plantain(LandContext ctx, MeshKit mk, Vector3 g, float s, System.Random rng)
        {
            var up = Vector3.up; mk.Prism(WorldMaterials.Solid(new Color(.4f, .5f, .26f)), g - up * .2f, up, .14f * s, 1.8f * s, 6, .8f); ArtistBits.PostCollider(ctx.Root, g, 1.8f * s, .16f * s);
            for (var k = 0; k < 6; k++) { var fd = Quaternion.Euler(0, k * 60f + (float)rng.NextDouble() * 30f, 0) * Vector3.forward; mk.Box(k % 2 == 0 ? leafM : leafDark, g + up * 2f * s + fd * .8f * s, new Vector3(.6f * s, .04f, 1.8f * s), Quaternion.LookRotation(fd) * Quaternion.Euler(-30f + k * 6f, 0, 0)); }
            if (rng.Next(2) == 0) for (var k = 0; k < 5; k++) mk.Blob(GlowM(new Color(.8f, .8f, .2f), .3f), g + up * (1.5f - k * .08f) * s + Vector3.right * (.2f + (k % 2) * .08f) * s, new Vector3(.05f, .12f, .05f) * s, rng, 0f);
        }

        void Hibiscus(MeshKit mk, Vector3 g, System.Random rng)
        {
            var up = Vector3.up; mk.Blob(rng.Next(2) == 0 ? leafM : leafDark, g + up * .5f, new Vector3(.9f, .6f, .9f), rng, .3f); var c = rng.Next(2) == 0 ? Pink : new Color(1f, .3f, .2f); var fm = GlowM(c, .7f);
            for (var k = 0; k < 5; k++) mk.Blob(fm, g + up * (.6f + (float)rng.NextDouble() * .5f) + Quaternion.Euler(0, k * 72f, 0) * Vector3.forward * .7f, Vector3.one * .12f, rng, .3f);
        }

        /// A goat, standing about. Say hello.
        void Goat(MeshKit unused, Vector3 g, Quaternion rot, System.Random rng, int i)
        {
            var m = new MeshKit(); var fur = WorldMaterials.Solid(i % 3 == 0 ? new Color(.9f, .88f, .8f) : i % 3 == 1 ? new Color(.4f, .3f, .24f) : new Color(.2f, .18f, .18f));
            m.Blob(fur, new Vector3(0, .5f, 0), new Vector3(.2f, .2f, .38f), rng, .04f); m.Blob(fur, new Vector3(0, .72f, .42f), new Vector3(.1f, .12f, .16f), rng, 0f); foreach (var e in new[] { -.06f, .06f }) { m.Cone(dark, new Vector3(e, .82f, .38f), .02f, .14f, 4); foreach (var q in new[] { -.24f, .24f }) m.Box(fur, new Vector3(e * 2f, .18f, q), new Vector3(.05f, .36f, .05f), Quaternion.identity); }
            m.Blob(fur, new Vector3(0, .62f, .54f), new Vector3(.03f, .07f, .03f), rng, 0f);
            var go = m.BuildSingle("Goat", tlife.transform, false); go.transform.SetPositionAndRotation(g, rot); string[] meh = { "\"Meeeh.\" It has seen everything on this road and is not impressed.", "The goat chews in six-eight.", "It looks at your shoes as if they might be food. They might be." }; var n = i;
            Interact.Toy(go, Toy.Kind.Custom, "Greet the goat", new Vector3(.7f, 1f, 1.1f)).onUse = _ => Say(tlife.transform, g + Vector3.up * 1.6f, meh[n++ % meh.Length], 4f);
        }

        /// An okada: a motorbike leaning on its stand.
        void Okada(LandContext ctx, MeshKit mk, Vector3 g, Quaternion rot, int i)
        {
            var up = Vector3.up; var f = rot * Vector3.forward; var s = rot * Vector3.right; var paint = Paint(Paints[(i * 2 + 2) % Paints.Length]);
            foreach (var e in new[] { -.65f, .65f }) { mk.Prism(dark, g + up * .32f + f * e - s * .05f, s, .32f, .1f, 12); mk.Prism(tin, g + up * .32f + f * e - s * .06f, s, .12f, .12f, 8); }
            mk.Box(paint, g + up * .62f, new Vector3(.24f, .26f, .8f), rot); mk.Box(dark, g + up * .8f - f * .25f, new Vector3(.26f, .1f, .7f), rot); mk.Box(tin, g + up * .95f + f * .55f, new Vector3(.6f, .04f, .04f), rot); mk.Prism(tin, g + up * .4f + f * .62f, (up - f * .3f).normalized, .03f, .6f, 4); mk.Blob(windowM, g + up * .84f + f * .68f, Vector3.one * .07f, new System.Random(i), 0f);
            BoxCol(ctx.Root, g + up * .5f, new Vector3(.4f, 1f, 1.7f), rot); Interact.Seat(tlife.transform, g + up * .86f - f * .25f, f, "Sit on the okada").seatHeight = .8f; noGrass.Add(new Vector3(g.x, 1.6f, g.z));
        }

        /// A little painted kiosk selling one thing.
        void Kiosk(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 g, Vector3 face, int i)
        {
            var up = Vector3.up; var f = new PlaceFrame(g, face); var paint = Paint(Paints[(i * 3 + 1) % Paints.Length]); string[] sells = { "RECHARGE CARDS", "PURE WATER", "SUYA", "BARBING SALON", "TAILOR", "COLD MINERALS" };
            mk.Box(paint, f.P(0, 1.2f, 0), new Vector3(2.4f, 2.4f, 2f), f.r); solid.Box(paint, f.P(0, 1.2f, 0), new Vector3(2.4f, 2.4f, 2f), f.r); mk.Box(tin, f.P(0, 2.5f, .3f), new Vector3(2.8f, .06f, 2.9f), f.r * Quaternion.Euler(8f, 0, 0)); mk.Box(dark, f.P(0, 1.4f, 1.01f), new Vector3(1.7f, 1f, .04f), f.r); mk.Box(windowM, f.P(0, 1.4f, 1.02f), new Vector3(1.5f, .8f, .03f), f.r); mk.Box(woodM, f.P(0, .86f, 1.2f), new Vector3(1.8f, .06f, .4f), f.r);
            ArtistBits.Label(rootT, sells[i % sells.Length], f.P(0, 2.14f, 1.02f), f.Fwd, .15f, Ink, TextAnchor.MiddleCenter); halos.Add(f.P(0, 1.4f, 1.6f), 1.4f, Gold, .35f); noGrass.Add(new Vector3(g.x, 3f, g.z));
        }

        /// The sun going into the sea in the west: a great orange disc, rings of glow round it, and its road of light
        /// across the water to the beach.
        void Sunset(LandContext ctx, MeshKit mk)
        {
            var sc = center - R * (roadL + 620f); sc.y = waterY + 34f;
            mk.Prism(sunM, sc, R, 78f, 2f, 40); mk.Prism(GlowM(new Color(1f, .42f, .16f), 1.5f), sc - R * 6f, R, 130f, 2f, 40); mk.Prism(GlowM(new Color(.95f, .3f, .3f), .9f), sc - R * 12f, R, 210f, 2f, 40); mk.Prism(GlowM(new Color(.75f, .25f, .5f), .55f), sc - R * 18f, R, 330f, 2f, 40);
            for (var k = 0; k < 14; k++) { var d = roadL + 40f + k * 40f; var w = 9f + k * 2.4f; mk.Box(sunRoadM, new Vector3(center.x - d, waterY + .16f, center.z + Mathf.Sin(k * 1.7f) * 3f), new Vector3(26f, .02f, w)); }
            life.sunAt = sc;
        }

        /// The verges: tough green grass on the red earth, nothing on the sand.
        void Verge(LandContext ctx)
        {
            var mat = WindMesh.Material(.3f, .9f, new Color(.24f, .44f, .24f));
            var meshes = new Dictionary<(int, int), WindMesh>(); var rng = new System.Random(1971);
            Color[] flowers = { new Color(1f, .8f, .2f), Pink, new Color(1f, .5f, .2f) };
            const float st = 2.6f;
            for (var x = ctx.MinX; x < ctx.MaxX; x += st)
            for (var z = ctx.MinZ; z < ctx.MaxZ; z += st)
            {
                var px = x + ((float)rng.NextDouble() - .5f) * st; var pz = z + ((float)rng.NextDouble() - .5f) * st;
                if (!ctx.IsLand(px, pz, 1f) || NearPath(px, pz, 4.2f) || InNoGrass(px, pz, 0f) || InFootprint(ctx, px, pz, -2f)) continue;
                var gy = ctx.Ground(px, pz); if (gy < waterY + 3.4f) continue;
                if (Mathf.PerlinNoise(px * .04f + 5f, pz * .04f + 1f) < .4f) continue;
                var key = (Mathf.FloorToInt(px / 70f), Mathf.FloorToInt(pz / 70f));
                if (!meshes.TryGetValue(key, out var wm)) meshes[key] = wm = new WindMesh();
                var col = Color.Lerp(new Color(.18f, .4f, .22f), new Color(.3f, .5f, .24f), (float)rng.NextDouble());
                wm.Tuft(new Vector3(px, gy - .04f, pz), .26f + (float)rng.NextDouble() * .34f, col, rng.NextDouble() < .1 ? flowers[rng.Next(flowers.Length)] : Color.Lerp(col, Gold, .3f), rng, 3, .06f);
            }
            foreach (var kv in meshes) kv.Value.Build("Afrobeat verge", ctx.Root, mat);
        }
    }
}
