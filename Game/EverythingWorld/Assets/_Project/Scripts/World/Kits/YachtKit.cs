using System.Collections.Generic;
using UnityEngine;

namespace EverythingWorld.World
{
    /// The Yacht Rock Riviera (WORLD_GUIDE.md): an endless golden-pink sunset over a calm bay, smooth and a little
    /// classy (and a little cheesy, on purpose). The land has no sea of its own, so a bay is dug into its heart:
    /// still water with a marina of moored yachts, a pier laid out like a piano keyboard, a lighthouse on a point,
    /// a white yacht club with blue awnings and terraces, a sandy beach with white grand pianos and deck chairs,
    /// and a lamp-lit promenade with date palms round it. The artists sit along the waterfront.
    /// Files: YachtKit (ground, the bay, the promenade, the marina, the club), YachtArtists, YachtLife.
    public partial class YachtKit : LandKit
    {
        public override string Land => "The Yacht Rock Riviera";
        public override Vector3 ArtistFront(IslandWorldData data, IslandWorldData.Artist a) => facing.TryGetValue(a.name, out var f) ? f : base.ArtistFront(data, a);

        public const string RocketMan = "3gdewACMIVMEWVbyb8O9sY", Africa = "2374M0fQpWi3dLnB54qaLX", HotelCalifornia = "40riOy7x9W7GXjyGp4pjAv", Margaritaville = "4EEjMyQub6tgFVshlM9j1M", DangerZone = "34x6hEJgGAOQvmlMql5Ige";
        public override (string song, string label, string artist)[] SpecialSongs => new[]
        {
            (RocketMan, "Rocket Man", "Elton John"), (Africa, "Africa", "TOTO"), (HotelCalifornia, "Hotel California", "Eagles"),
            (Margaritaville, "Margaritaville", "Jimmy Buffett"), (DangerZone, "Danger Zone", "@the bay"),
        };

        static readonly Color Sunset = new Color(1f, .7f, .45f), Navy = new Color(.12f, .2f, .42f), Ink = new Color(1f, .97f, .92f);
        const float PromW = 5f;

        Material white, navy, teak, sand, dark, metal, stone, water, palmLeaf, palmBark;
        GlowHalos halos; Vector3 center; YachtLife life; MeshKit soft;
        readonly Dictionary<(int, int), List<Vector3>> pathCells = new Dictionary<(int, int), List<Vector3>>();
        readonly List<(Vector3 p, int w)> walkPoints = new List<(Vector3, int)>();
        readonly Dictionary<string, Vector3> facing = new Dictionary<string, Vector3>();
        readonly List<Vector3> noGrass = new List<Vector3>();
        // the bay (ShapeGround)
        bool hasBay; Vector2 bayC; float bayR = 95f, bayY; readonly List<Vector2> rim = new List<Vector2>();

        void MakeMaterials()
        {
            white = WorldMaterials.Solid(new Color(.96f, .95f, .92f)); navy = WorldMaterials.Solid(Navy); teak = WorldMaterials.Solid(new Color(.62f, .45f, .3f), .5f);
            sand = WorldMaterials.Solid(new Color(.95f, .86f, .7f)); dark = WorldMaterials.Solid(new Color(.08f, .08f, .1f)); metal = WorldMaterials.Solid(new Color(.8f, .82f, .85f), .9f);
            stone = WorldMaterials.Solid(new Color(.88f, .84f, .76f)); palmLeaf = WorldMaterials.Solid(new Color(.25f, .45f, .28f)); palmBark = WorldMaterials.Solid(new Color(.55f, .45f, .35f));
            water = new Material(WorldMaterials.Glow(new Color(.2f, .35f, .55f), .12f)) { name = "Bay water" }; water.SetFloat("_Smoothness", .85f);
        }

        Material GlowM(Color c, float s = 2f) => WorldMaterials.Glow(c, s * .65f);
        static bool HasStation(IslandWorldData.Artist a) => a.station || a.stationId != 0;
        static float Foot(IslandWorldData.Artist a) => HasStation(a) ? 15f : 8f;

        public override bool Ground(float height, float slope, float patch, out Color color)
        {
            if (slope > 8f) color = Color.Lerp(new Color(.7f, .6f, .5f), new Color(.78f, .68f, .55f), patch);
            else color = Color.Lerp(new Color(.45f, .58f, .35f), new Color(.6f, .62f, .38f), Mathf.SmoothStep(.2f, .9f, patch));
            color.a = 1f;
            return true;
        }

        bool InBay(float x, float z, float pad) => hasBay && Vector2.Distance(new Vector2(x, z), bayC) < BayEdge(x, z) + pad;
        float BayEdge(float x, float z) { var a = Mathf.Atan2(z - bayC.y, x - bayC.x); return bayR * (1f + .12f * Mathf.Sin(a * 3f) + .07f * Mathf.Sin(a * 5f + 1f)); }

        /// The bay: dug beside where you arrive, as big as the land allows, a gently wavy shore; the artists spread
        /// round it.
        public override void ShapeGround(IslandWorldData data, int index)
        {
            var isl = data.layout.islands[index]; var heart = new Vector2(isl.labelX, isl.labelZ);
            bool Inside(Vector2 p, float m) { if (data.IslandAt(p.x, p.y) != isl) return false; for (var k = 0; k < 16; k++) { var a = k * Mathf.PI / 8f; if (data.IslandAt(p.x + Mathf.Cos(a) * m, p.y + Mathf.Sin(a) * m) != isl) return false; } return true; }
            hasBay = false; var bestR = 0f;
            for (var k = 0; k < 16; k++)
            {
                var a = k * Mathf.PI / 8f; var dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                for (var r = 110f; r >= 60f; r -= 10f) { var c = heart + dir * (r + 25f); if (Inside(c, r * 1.2f + 45f)) { if (r > bestR) { bestR = r; bayC = c; } break; } }
            }
            if (bestR < 60f) { Debug.LogWarning("The Yacht Rock Riviera: no room for the bay"); }
            else
            {
                hasBay = true; bayR = bestR;
                bayY = data.HeightAt(heart.x, heart.y) - 1.6f;
                for (var r = 0; r < data.Nz; r++)
                {
                    var z = data.layout.depth - r * data.Step; if (Mathf.Abs(z - bayC.y) > bayR * 1.3f + 60f) continue;
                    for (var col = 0; col < data.Nx; col++)
                    {
                        var x = col * data.Step; if (Mathf.Abs(x - bayC.x) > bayR * 1.3f + 60f) continue;
                        var i = r * data.Nx + col; if (data.islandIds[i] != index) continue;
                        var d = Vector2.Distance(new Vector2(x, z), bayC) - BayEdge(x, z);
                        if (d < 0f) { var f = Mathf.Clamp01(-d / 25f); data.heights[i] = bayY - .4f - 4f * f; }
                        else if (d < 50f) data.heights[i] = Mathf.Lerp(bayY + .5f, data.heights[i], Mathf.SmoothStep(0, 1, d / 50f)) * (d < 6f ? 1f : 1f);
                    }
                }
            }
            var avoid = new List<Vector3>(); if (hasBay) avoid.Add(new Vector3(bayC.x, bayR * 1.2f + 14f, bayC.y));
            ArtistPlacement.Spread(data, index, 62f, heart, 46f, 40f, avoid, null, Land, 300f);
            foreach (var a in data.layout.artists)
            {
                if (a.sceneId != isl.sceneId) continue;
                a.r = Mathf.Max(a.r, Foot(a) + 6f);
                Level(data, index, new Vector2(a.x, a.z), Foot(a) + 1f, Foot(a) + 12f);
            }
        }

        void Level(IslandWorldData data, int index, Vector2 c, float flat, float blend)
        {
            var level = data.HeightAt(c.x, c.y);
            for (var r = 0; r < data.Nz; r++)
            {
                var z = data.layout.depth - r * data.Step; if (Mathf.Abs(z - c.y) > blend) continue;
                for (var col = 0; col < data.Nx; col++)
                {
                    var x = col * data.Step; if (Mathf.Abs(x - c.x) > blend) continue;
                    var i = r * data.Nx + col; if (data.islandIds[i] != index || InBay(x, z, 2f)) continue;
                    var d = Vector2.Distance(new Vector2(x, z), c);
                    if (d < flat) data.heights[i] = level;
                    else if (d < blend) data.heights[i] = Mathf.Lerp(level, data.heights[i], Mathf.SmoothStep(0, 1, (d - flat) / (blend - flat)));
                }
            }
        }

        public override void Dress(LandContext ctx)
        {
            MakeMaterials();
            halos = new GlowHalos(); soft = new MeshKit();
            pathCells.Clear(); walkPoints.Clear(); facing.Clear(); noGrass.Clear();
            center = ctx.Plaza;
            life = new GameObject("Yacht Rock life").AddComponent<YachtLife>();
            life.transform.SetParent(ctx.Root, false);
            life.center = center;
            var mk = new MeshKit(); var solid = new MeshKit();
            if (hasBay) { Bay(ctx, mk, solid); Promenade(ctx, mk, solid); Marina(ctx, mk, solid); YachtClub(ctx, mk, solid); Beach(ctx, mk, solid); Lighthouse(ctx, mk, solid); }
            Paths(ctx, mk, solid);
            Hills(ctx, mk, solid);
            mk.Build("Riviera", ctx.Root, false);
            soft.Build("Soft riviera", ctx.Root, false);
            solid.BuildCollider("Riviera walls", ctx.Root);
        }

        public override void Finish(LandContext ctx) { GroundCover(ctx); halos.Build("Riviera glow", ctx.Root); }

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
            ctx.IsLand(x, z, 4f) && !InFootprint(ctx, x, z, pad) && !NearPath(x, z, pad + 2f) && !InNoGrass(x, z, pad) && !InBay(x, z, pad + 10f)
            && Vector2.Distance(new Vector2(x, z), new Vector2(center.x, center.z)) > 20f + pad;

        void Strip(MeshKit mk, List<Vector3> pts, Material m, float w, float lift)
        {
            var n = pts.Count; var L = new Vector3[n]; var R = new Vector3[n];
            Vector3 Flat(Vector3 v) { v.y = 0; return v.normalized; }
            for (var i = 0; i < n; i++)
            {
                var a = i > 0 ? Flat(pts[i] - pts[i - 1]) : Flat(pts[1] - pts[0]);
                var b = i < n - 1 ? Flat(pts[i + 1] - pts[i]) : Flat(pts[n - 1] - pts[n - 2]);
                var t = (a + b).normalized; var side = Vector3.Cross(Vector3.up, t);
                var miter = 1f / Mathf.Max(.6f, Vector3.Dot(side, Vector3.Cross(Vector3.up, b)));
                L[i] = pts[i] + Vector3.up * lift - side * (w / 2f * miter); R[i] = pts[i] + Vector3.up * lift + side * (w / 2f * miter);
            }
            for (var i = 0; i + 1 < n; i++) mk.Quad(m, L[i], R[i], R[i + 1], L[i + 1], (pts[i] + pts[i + 1]) / 2f - Vector3.up);
        }

        void Palm(LandContext ctx, MeshKit mk, Vector3 at, float h)
        {
            var up = Vector3.up; var lean = (up + new Vector3(ctx.Range(-.1f, .1f), 0, ctx.Range(-.1f, .1f))).normalized;
            mk.Prism(palmBark, at - up * .3f, lean, .3f, h, 7, .75f);
            var top = at + lean * h;
            for (var k = 0; k < 9; k++)
            {
                var d = Quaternion.Euler(0, k * 40f + ctx.Range(-10f, 10f), 0) * Vector3.forward; var w = Vector3.Cross(up, d) * .45f;
                var mid = top + d * 2f + up * .5f; var tip = top + d * 4.2f - up * 1f;
                soft.Tri(palmLeaf, top, mid + w, mid - w, top - up * 3f); soft.Tri(palmLeaf, top, mid - w, mid + w, top + up * 3f);
                soft.Tri(palmLeaf, mid + w, tip, mid - w, mid - up * 3f); soft.Tri(palmLeaf, mid - w, tip, mid + w, mid + up * 3f);
            }
            ArtistBits.PostCollider(ctx.Root, at, h, .3f);
        }

        /// A lamp of the old seaside kind: a cream post with a glass globe.
        void Lamp(LandContext ctx, MeshKit mk, Vector3 g)
        {
            mk.Prism(stone, g - Vector3.up * .2f, Vector3.up, .1f, 3.4f, 8);
            mk.Blob(GlowM(new Color(1f, .85f, .6f), 2.4f), g + Vector3.up * 3.5f, Vector3.one * .28f, ctx.Rng, 0f);
            halos.Add(g + Vector3.up * 3.5f, 1.3f, new Color(1f, .8f, .55f), .7f);
            ArtistBits.PostCollider(ctx.Root, g, 3.4f, .12f);
        }

        // ---------------------------------------------------------------- the bay
        void Bay(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            const int n = 64; var c = new Vector3(bayC.x, bayY - .05f, bayC.y);
            for (var k = 0; k < n; k++)
            {
                float a0 = k * Mathf.PI * 2f / n, a1 = (k + 1) * Mathf.PI * 2f / n;
                Vector3 R(float a) { var x = bayC.x + Mathf.Cos(a); var z = bayC.y + Mathf.Sin(a); var r = BayEdge(x, z) + 6f; return new Vector3(bayC.x + Mathf.Cos(a) * r, c.y, bayC.y + Mathf.Sin(a) * r); }
                mk.Tri(water, c, R(a0), R(a1), c - Vector3.up);
            }
            life.bay = c; life.bayR = bayR;
            noGrass.Add(new Vector3(bayC.x, bayR * 1.25f, bayC.y));
            var mark = new GameObject("Shot: the bay").transform; mark.SetParent(ctx.Root, false);
            var look = new Vector3(bayC.x - center.x, 0, bayC.y - center.z).normalized; var feet = ctx.OnGround(center.x, center.z);
            mark.SetPositionAndRotation(feet, Quaternion.LookRotation(look));
        }

        /// A promenade round the bay just above the water: pale stone, a balustrade on the water side, seaside
        /// lamps, date palms and benches.
        void Promenade(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var pts = new List<Vector3>();
            for (var k = 0; k <= 96; k++)
            {
                var a = k * Mathf.PI * 2f / 96f; var dx = Mathf.Cos(a); var dz = Mathf.Sin(a);
                var r = BayEdge(bayC.x + dx, bayC.y + dz) + PromW / 2f + 1.5f;
                var x = bayC.x + dx * r; var z = bayC.y + dz * r;
                if (!ctx.IsLand(x, z, 2f)) { if (pts.Count > 1) { Strip(mk, pts, stone, PromW, .08f); } pts.Clear(); continue; }
                pts.Add(new Vector3(x, Mathf.Max(ctx.Ground(x, z), bayY + .6f), z));
            }
            if (pts.Count > 1) Strip(mk, pts, stone, PromW, .08f);
            for (var k = 0; k < 96; k++)
            {
                var a = k * Mathf.PI * 2f / 96f; var d = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                var r = BayEdge(bayC.x + d.x, bayC.y + d.z) + 1.4f; var p = new Vector3(bayC.x + d.x * r, 0, bayC.y + d.z * r);
                if (!ctx.IsLand(p.x, p.z, 2f)) continue;
                p.y = Mathf.Max(ctx.Ground(p.x, p.z), bayY + .6f);
                Remember(p + d * 2.5f, 500);
                var a1 = (k + 1) * Mathf.PI * 2f / 96f; var d1 = new Vector3(Mathf.Cos(a1), 0, Mathf.Sin(a1)); var r1 = BayEdge(bayC.x + d1.x, bayC.y + d1.z) + 1.4f; var p1 = new Vector3(bayC.x + d1.x * r1, p.y, bayC.y + d1.z * r1);
                if (k % 8 != 3) { mk.Box(white, (p + p1) / 2f + Vector3.up * .95f, new Vector3(.2f, .12f, Vector3.Distance(p, p1) + .05f), Quaternion.LookRotation(p1 - p)); solid.Box(white, (p + p1) / 2f + Vector3.up * .5f, new Vector3(.25f, 1f, Vector3.Distance(p, p1) + .05f), Quaternion.LookRotation(p1 - p)); for (var b = 0; b < 3; b++) mk.Prism(white, Vector3.Lerp(p, p1, b / 3f) + Vector3.up * .05f, Vector3.up, .08f, .85f, 6, .6f); }
                var land = p + d * (PromW + .6f); land.y = Mathf.Max(ctx.Ground(land.x, land.z), bayY + .6f);
                if (k % 4 == 0) Lamp(ctx, mk, p + d * .4f);
                if (k % 6 == 2) Palm(ctx, mk, land + d * 1.2f, ctx.Range(9f, 12f));
                if (k % 12 == 7) { var br = Quaternion.LookRotation(-d); mk.Box(teak, land + Vector3.up * .45f, new Vector3(1.8f, .08f, .5f), br); mk.Box(teak, land + Vector3.up * .8f + d * .25f, new Vector3(1.8f, .5f, .06f), br); solid.Box(teak, land + Vector3.up * .5f, new Vector3(1.9f, 1f, .6f), br); }
            }
        }

        /// The marina: a main pier laid out like a piano keyboard (white planks with black keys raised between),
        /// finger jetties, sailing yachts with tall masts and motor yachts moored along them.
        void Marina(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var toBay = new Vector3(bayC.x - center.x, 0, bayC.y - center.z).normalized; var side = Vector3.Cross(Vector3.up, toBay);
            var start = new Vector3(bayC.x, 0, bayC.y) - toBay * (BayEdge(bayC.x - toBay.x, bayC.y - toBay.z) + 3f); var y = bayY + .5f;
            var len = bayR * .75f;
            for (var k = 0; k < len / .9f; k++)
            {
                var p = start + toBay * (k * .9f); p.y = y;
                mk.Box(white, p, new Vector3(4f, .25f, .85f), Quaternion.LookRotation(toBay));
                if (k % 7 != 2 && k % 7 != 6) mk.Box(dark, p + Vector3.up * .2f + side * .9f, new Vector3(2.2f, .2f, .5f), Quaternion.LookRotation(toBay));
            }
            var mid = start + toBay * (len / 2f); mid.y = y; solid.Box(white, mid - Vector3.up * .2f, new Vector3(4f, .6f, len), Quaternion.LookRotation(toBay));
            for (var k = 0f; k < len; k += 6f) foreach (var s in new[] { -2f, 2f }) { var p = start + toBay * k + side * s; mk.Prism(teak, new Vector3(p.x, bayY - 3f, p.z), Vector3.up, .12f, 3.8f, 6); }
            for (var k = 1; k < 5; k++) Lamp(ctx, mk, new Vector3(start.x, y + .1f, start.z) + toBay * (k * len / 5f) + side * 1.8f);
            Remember(new Vector3(start.x, y, start.z) + toBay * 3f, 501);
            // finger jetties with boats
            var boats = new List<Transform>();
            for (var j = 0; j < 4; j++)
            {
                var jp = start + toBay * (len * (.2f + j * .2f)); jp.y = y;
                foreach (var s in new[] { -1f, 1f })
                {
                    var f0 = jp + side * s * 2f; var f1 = f0 + side * s * 10f;
                    mk.Box(teak, (f0 + f1) / 2f, new Vector3(1.4f, .2f, 10f), Quaternion.LookRotation(side)); solid.Box(teak, (f0 + f1) / 2f - Vector3.up * .2f, new Vector3(1.4f, .6f, 10f), Quaternion.LookRotation(side));
                    var bp = f0 + side * s * 6f + toBay * 3.2f; bp.y = bayY - .1f;
                    boats.Add((j + (s > 0 ? 1 : 0)) % 2 == 0 ? Sailboat(ctx.Root, bp, side * s, j) : MotorYacht(ctx.Root, bp, side * s, j));
                }
            }
            life.boats = boats.ToArray();
            ArtistBits.Label(ctx.Root, "THE MARINA", new Vector3(start.x, y + 3.4f, start.z) - toBay * .5f, -toBay, .3f, Navy, TextAnchor.MiddleCenter);
            mk.Box(white, new Vector3(start.x, y + 3.4f, start.z), new Vector3(4.6f, .8f, .1f), Quaternion.LookRotation(toBay));
            foreach (var s in new[] { -2f, 2f }) mk.Prism(white, new Vector3(start.x, y, start.z) + side * s, Vector3.up, .1f, 3.4f, 6);
        }

        Transform Sailboat(Transform parent, Vector3 at, Vector3 dir, int k)
        {
            var mk = new MeshKit(); var hull = WorldMaterials.Solid(k % 2 == 0 ? new Color(.95f, .95f, .93f) : Navy, .7f);
            mk.Box(hull, new Vector3(0, .5f, 0), new Vector3(2.4f, 1f, 8f), Quaternion.identity); mk.Prism(hull, new Vector3(0, .5f, 4f), Vector3.forward, 1.2f, 2f, 4, .1f, 45f);
            mk.Box(teak, new Vector3(0, 1.05f, -.5f), new Vector3(2.2f, .1f, 6f), Quaternion.identity);
            mk.Box(white, new Vector3(0, 1.4f, -1f), new Vector3(1.6f, .6f, 2.4f), Quaternion.identity);
            mk.Box(metal, new Vector3(0, 7f, .8f), new Vector3(.12f, 12f, .12f), Quaternion.identity);
            mk.Box(metal, new Vector3(0, 2.3f, -1.4f), new Vector3(.08f, .08f, 4.4f), Quaternion.identity);
            mk.Box(white, new Vector3(0, 2.4f, -1.4f), new Vector3(.3f, .3f, 4.2f), Quaternion.identity);
            mk.Blob(WorldMaterials.Glow(new Color(1f, .3f, .2f), 2.6f), new Vector3(0, 13f, .8f), Vector3.one * .12f, new System.Random(k), 0f);
            var t = mk.BuildSingle("Sailboat", parent, false).transform; t.SetPositionAndRotation(at, Quaternion.LookRotation(dir)); return t;
        }

        Transform MotorYacht(Transform parent, Vector3 at, Vector3 dir, int k)
        {
            var mk = new MeshKit();
            mk.Box(white, new Vector3(0, .7f, 0), new Vector3(3f, 1.4f, 10f), Quaternion.identity); mk.Prism(white, new Vector3(0, .7f, 5f), Vector3.forward, 1.5f, 2.6f, 4, .1f, 45f);
            mk.Box(navy, new Vector3(0, .45f, 0), new Vector3(3.02f, .3f, 10.02f), Quaternion.identity);
            mk.Box(white, new Vector3(0, 1.9f, -.5f), new Vector3(2.6f, 1.2f, 5f), Quaternion.identity);
            mk.Box(WorldMaterials.Solid(new Color(.15f, .2f, .3f), .95f), new Vector3(0, 2f, -.5f), new Vector3(2.62f, .5f, 4.6f), Quaternion.identity);
            mk.Box(white, new Vector3(0, 2.8f, -1f), new Vector3(2f, .6f, 2.6f), Quaternion.identity);
            for (var w = 0; w < 6; w++) mk.Box(WorldMaterials.Glow(new Color(1f, .85f, .6f), 1.6f), new Vector3(1.32f, 1.2f, -3f + w * 1.1f), new Vector3(.02f, .3f, .6f), Quaternion.identity);
            var t = mk.BuildSingle("Motor yacht", parent, false).transform; t.SetPositionAndRotation(at, Quaternion.LookRotation(dir)); return t;
        }

        /// The yacht club: a long white building on the shore with a blue-striped awning over a terrace, round
        /// tables and chairs, string lights, a flagpole, glowing windows.
        void YachtClub(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var toBay = new Vector3(bayC.x - center.x, 0, bayC.y - center.z).normalized; var side = Vector3.Cross(Vector3.up, toBay);
            var dir = -side; var a = new Vector3(bayC.x, 0, bayC.y) + dir * (BayEdge(bayC.x + dir.x, bayC.y + dir.z) + PromW + 14f);
            if (!ctx.IsLand(a.x, a.z, 14f)) { dir = side; a = new Vector3(bayC.x, 0, bayC.y) + dir * (BayEdge(bayC.x + dir.x, bayC.y + dir.z) + PromW + 14f); }
            a.y = Mathf.Max(ctx.Ground(a.x, a.z), bayY + .6f); var face = -dir; var rot = Quaternion.LookRotation(face); var fs = Vector3.Cross(Vector3.up, face);
            const float W = 22f, D = 10f, H = 6f;
            mk.Box(white, a + Vector3.up * H / 2f - face * 2f, new Vector3(W, H, D), rot); solid.Box(white, a + Vector3.up * H / 2f - face * 2f, new Vector3(W, H, D), rot);
            mk.Box(navy, a + Vector3.up * (H + .2f) - face * 2f, new Vector3(W + .4f, .4f, D + .4f), rot);
            for (var k = 0; k < 8; k++) mk.Box(GlowM(new Color(1f, .85f, .6f), 1.8f), a + Vector3.up * 3.2f + face * (D / 2f - 1.99f) + fs * ((k - 3.5f) * 2.6f), new Vector3(1.4f, 2.6f, .04f), rot);
            var terrace = a + face * 6f;
            mk.Box(teak, terrace + Vector3.up * .15f, new Vector3(W, .3f, 7f), rot); solid.Box(teak, terrace + Vector3.up * .15f, new Vector3(W, .3f, 7f), rot);
            for (var k = 0; k < 11; k++) mk.Box(k % 2 == 0 ? white : navy, terrace + Vector3.up * 3.6f + fs * ((k - 5f) * 2f), new Vector3(2f, .1f, 7.4f), rot * Quaternion.Euler(-8f, 0, 0));
            foreach (var s in new[] { -1f, 1f }) { var p = terrace + fs * s * (W / 2f - .3f) + face * 3.4f; mk.Prism(white, p, Vector3.up, .1f, 3.4f, 6); ArtistBits.PostCollider(ctx.Root, p, 3.4f, .12f); }
            for (var k = 0; k < 5; k++)
            {
                var tp = terrace + fs * ((k - 2f) * 4f) + Vector3.up * .3f;
                mk.Prism(white, tp + Vector3.up * .75f, Vector3.up, .7f, .05f, 12); mk.Prism(metal, tp, Vector3.up, .06f, .75f, 6);
                foreach (var s in new[] { -1f, 1f }) { mk.Box(white, tp + fs * s * 1f + Vector3.up * .45f, new Vector3(.45f, .06f, .45f), rot); mk.Box(white, tp + fs * s * 1.2f + Vector3.up * .75f, new Vector3(.06f, .6f, .45f), rot); }
                mk.Blob(GlowM(new Color(1f, .8f, .5f), 2.8f), tp + Vector3.up * .85f, new Vector3(.06f, .1f, .06f), ctx.Rng, 0f);
                solid.Box(white, tp + Vector3.up * .45f, new Vector3(2.6f, .9f, 1.4f), rot);
            }
            for (var k = 0; k < 14; k++) { var t0 = k / 14f; var p = terrace + fs * Mathf.Lerp(-W / 2f, W / 2f, t0) + face * 3.4f + Vector3.up * (3f - Mathf.Sin(t0 * Mathf.PI * 4f) * .3f); mk.Blob(GlowM(new Color(1f, .85f, .55f), 2.6f), p, Vector3.one * .09f, ctx.Rng, 0f); }
            var flag = a + face * 9.5f + fs * (W / 2f + 2f); mk.Prism(white, flag, Vector3.up, .1f, 10f, 6); mk.Box(navy, flag + Vector3.up * 9.3f + fs * .7f, new Vector3(1.4f, .9f, .02f), rot); ArtistBits.PostCollider(ctx.Root, flag, 10f, .12f);
            ArtistBits.Label(ctx.Root, "RIVIERA YACHT CLUB", a + Vector3.up * (H + 1f) + face * (D / 2f - 1.9f), face, .45f, Navy, TextAnchor.MiddleCenter);
            mk.Box(white, a + Vector3.up * (H + 1f) + face * (D / 2f - 2f), new Vector3(10f, 1.2f, .1f), rot);
            ArtistBits.PointLight(ctx.Root, terrace + Vector3.up * 2.8f, new Color(1f, .8f, .55f), 18f, 1.8f);
            IndoorSpaces.Add(a + Vector3.up * H / 2f - face * 2f, rot, new Vector3(W, H, D));
            noGrass.Add(new Vector3(a.x, 18f, a.z));
            life.club = a;
        }

        /// A sandy beach on the far side of the bay: white grand pianos on the sand, striped deck chairs and
        /// parasols.
        void Beach(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var toBay = new Vector3(bayC.x - center.x, 0, bayC.y - center.z).normalized; var dir = toBay;
            var a = new Vector3(bayC.x, 0, bayC.y) + dir * (BayEdge(bayC.x + dir.x, bayC.y + dir.z) + PromW + 8f);
            if (!ctx.IsLand(a.x, a.z, 10f)) return;
            a.y = ctx.Ground(a.x, a.z); var face = -dir; var side = Vector3.Cross(Vector3.up, face);
            for (var k = 0; k < 2; k++) { var p = ctx.OnGround(a.x + side.x * (k * 10f - 5f), a.z + side.z * (k * 10f - 5f)); Piano(ctx, mk, solid, p, Quaternion.Euler(0, 30f * (k * 2 - 1), 0) * face); }
            Color[] stripes = { new Color(1f, .45f, .45f), new Color(.3f, .55f, .9f), new Color(1f, .8f, .3f) };
            for (var k = 0; k < 6; k++)
            {
                var p = ctx.OnGround(a.x + side.x * ((k - 2.5f) * 4f) - face.x * 5f, a.z + side.z * ((k - 2.5f) * 4f) - face.z * 5f); var r = Quaternion.LookRotation(face);
                for (var s = 0; s < 4; s++) mk.Box(WorldMaterials.Solid(s % 2 == 0 ? stripes[k % 3] : new Color(.96f, .95f, .9f)), p + Vector3.up * .4f + face * ((s - 1.5f) * .3f), new Vector3(.7f, .04f, .3f), r * Quaternion.Euler(-20f, 0, 0));
                mk.Prism(white, p + side * 1.4f, Vector3.up, .05f, 2.6f, 6); mk.Cone(WorldMaterials.Solid(stripes[(k + 1) % 3], .3f), p + side * 1.4f + Vector3.up * 2.4f, 1.6f, .7f, 8);
                solid.Box(white, p + Vector3.up * .3f, new Vector3(.8f, .6f, 1.4f), r);
            }
            noGrass.Add(new Vector3(a.x, 16f, a.z));
        }

        void Piano(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 at, Vector3 face)
        {
            var up = Vector3.up; var rot = Quaternion.LookRotation(face); var side = Vector3.Cross(up, face);
            mk.Blob(white, at + up * 1f, new Vector3(1.1f, .28f, 1.7f), ctx.Rng, 0f);
            foreach (var s in new[] { -1f, 1f }) mk.Prism(white, at + side * s * .7f + face * .7f, up, .08f, .85f, 6);
            mk.Prism(white, at - face * 1.1f, up, .08f, .85f, 6);
            mk.Box(white, at + up * 1.6f + side * .3f - face * .2f, new Vector3(1.6f, .05f, 1.8f), rot * Quaternion.Euler(-35f, 0, 0));
            mk.Box(dark, at + up * 1.05f + face * 1.2f, new Vector3(1.6f, .06f, .3f), rot); mk.Box(white, at + up * 1.09f + face * 1.25f, new Vector3(1.5f, .02f, .2f), rot);
            mk.Box(white, at + up * .45f + face * 1.9f, new Vector3(1.1f, .1f, .4f), rot);
            solid.Box(white, at + up * .8f, new Vector3(1.8f, 1.6f, 2.6f), rot);
        }

        /// A white lighthouse with a red band on the point of the bay nearest the sunset, its lamp turning.
        void Lighthouse(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var toBay = new Vector3(bayC.x - center.x, 0, bayC.y - center.z).normalized; var side = Vector3.Cross(Vector3.up, toBay);
            var dir = (toBay + side).normalized; var a = new Vector3(bayC.x, 0, bayC.y) + dir * (BayEdge(bayC.x + dir.x, bayC.y + dir.z) + 3f);
            if (!ctx.IsLand(a.x, a.z, 6f)) { dir = (toBay - side).normalized; a = new Vector3(bayC.x, 0, bayC.y) + dir * (BayEdge(bayC.x + dir.x, bayC.y + dir.z) + 3f); }
            a.y = Mathf.Max(ctx.Ground(a.x, a.z), bayY + .4f); var up = Vector3.up;
            mk.Prism(white, a - up * .5f, up, 2.6f, 16f, 12, .7f); solid.Prism(white, a - up * .5f, up, 2.6f, 16f, 12, .7f);
            mk.Prism(WorldMaterials.Solid(new Color(.8f, .15f, .15f)), a + up * 7f, up, 2.15f, 2.4f, 12, .95f);
            mk.Prism(dark, a + up * 15.5f, up, 2.2f, .3f, 12);
            var lamp = new Material(GlowM(new Color(1f, .9f, .6f), 2.4f)) { name = "Lighthouse lamp" };
            mk.Prism(lamp, a + up * 15.8f, up, 1.3f, 1.6f, 12);
            mk.Cone(WorldMaterials.Solid(new Color(.8f, .15f, .15f)), a + up * 17.4f, 1.6f, 1.6f, 12);
            halos.Add(a + up * 16.6f, 3f, new Color(1f, .9f, .6f), .7f);
            life.lighthouse = a + up * 16.6f;
            noGrass.Add(new Vector3(a.x, 5f, a.z));
        }

        // ---------------------------------------------------------------- paths
        /// Pale stone paths from the promenade (or where you arrive) to every artist, lamps along them.
        void Paths(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            Remember(ctx.OnGround(center.x, center.z), 0);
            var todo = new List<IslandWorldData.Artist>(ctx.Artists); var walk = 1; var lamps = new List<Vector3>();
            while (todo.Count > 0)
            {
                IslandWorldData.Artist next = null; Vector3 join = default; var best = float.MaxValue;
                foreach (var a in todo)
                    foreach (var (p, _) in walkPoints) { var d = Vector2.Distance(new Vector2(p.x, p.z), new Vector2(a.x, a.z)) - Foot(a); if (d < best) { best = d; next = a; join = p; } }
                if (next == null) break;
                todo.Remove(next);
                var from = new Vector3(join.x, 0, join.z); var to = new Vector3(next.x, 0, next.z);
                var dir = to - from; var total = dir.magnitude; if (total < 2f) continue; dir /= total;
                facing[next.name] = -dir;
                var side = Vector3.Cross(Vector3.up, dir); var end = total - Foot(next) * .8f;
                var pts = new List<Vector3>();
                for (var d = 2.5f; ; d += 2.5f)
                {
                    var dd = Mathf.Min(d, end); var q = from + dir * dd;
                    if (!ctx.IsLand(q.x, q.z, 0f) || InBay(q.x, q.z, 1f)) break;
                    pts.Add(ctx.OnGround(q.x, q.z)); if (dd >= end) break;
                }
                if (pts.Count < 2) continue;
                Strip(mk, pts, stone, 3.2f, .05f);
                foreach (var p in pts) Remember(p, walk);
                for (var i = 4; i < pts.Count; i += 8)
                {
                    var lp = pts[i] + side * ((i / 8) % 2 == 0 ? 1f : -1f) * 2.3f;
                    var near = false; foreach (var o in lamps) if ((o - lp).sqrMagnitude < 20f * 20f) near = true;
                    if (near) continue;
                    lamps.Add(lp); Lamp(ctx, mk, ctx.OnGround(lp.x, lp.z));
                }
                walk++;
            }
        }

        // ---------------------------------------------------------------- the hills round it
        /// White villas with terracotta roofs and lit windows on the slopes, cypress trees, olive trees,
        /// bougainvillea, vineyards in rows.
        void Hills(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            int villas = 0, cypress = 0, olives = 0, vines = 0;
            const float cell = 30f;
            for (var x = ctx.MinX; x < ctx.MaxX; x += cell)
            for (var z = ctx.MinZ; z < ctx.MaxZ; z += cell)
            {
                float px = x + ctx.Range(0f, cell), pz = z + ctx.Range(0f, cell);
                if (!Open(ctx, px, pz, 5f)) continue;
                var p = ctx.OnGround(px, pz); var roll = ctx.Range(0f, 1f);
                if (roll < .12f && villas < 12) { Villa(ctx, mk, solid, p); villas++; }
                else if (roll < .42f) { for (var k = 0; k < ctx.Rng.Next(2, 5); k++) { var q = ctx.OnGround(px + ctx.Range(-6f, 6f), pz + ctx.Range(-6f, 6f)); soft.Prism(WorldMaterials.Solid(new Color(.18f, .32f, .22f)), q - Vector3.up * .2f, Vector3.up, .9f, ctx.Range(6f, 9f), 7, .15f); ArtistBits.PostCollider(ctx.Root, q, 3f, .4f); } cypress++; }
                else if (roll < .62f) { Olive(ctx, mk, p); olives++; }
                else if (roll < .7f && vines < 6) { Vines(ctx, mk, p); vines++; }
                else continue;
                noGrass.Add(new Vector3(px, 5f, pz));
            }
            Debug.Log($"The Yacht Rock Riviera: bay {(hasBay ? bayR.ToString("0") + " m" : "none")}, {villas} villas, {cypress} cypress groups, {olives} olive trees, {vines} vineyards");
        }

        void Villa(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 at)
        {
            var up = Vector3.up; var face = hasBay ? new Vector3(bayC.x - at.x, 0, bayC.y - at.z).normalized : Vector3.forward; var rot = Quaternion.LookRotation(face); var side = Vector3.Cross(up, face);
            var roof = WorldMaterials.Solid(new Color(.78f, .38f, .25f));
            mk.Box(white, at + up * 2f, new Vector3(8f, 4f, 6f), rot); solid.Box(white, at + up * 2f, new Vector3(8f, 4f, 6f), rot);
            mk.Box(roof, at + up * 4.2f, new Vector3(8.6f, .4f, 6.6f), rot);
            mk.Box(white, at + up * 5.2f - side * 1.5f, new Vector3(4f, 2f, 4f), rot); mk.Box(roof, at + up * 6.3f - side * 1.5f, new Vector3(4.4f, .3f, 4.4f), rot);
            for (var k = 0; k < 3; k++) mk.Box(GlowM(new Color(1f, .82f, .55f), 1.6f), at + up * 2.2f + face * 3.01f + side * ((k - 1f) * 2.4f), new Vector3(1f, 1.6f, .02f), rot);
            for (var k = 0; k < 8; k++) soft.Blob(WorldMaterials.Solid(new Color(.9f, .25f, .55f), .3f), at + up * (1f + (k % 3) * .7f) + face * 3.1f + side * (3.4f + (k / 3) * .3f), Vector3.one * .4f, ctx.Rng, .3f);
            mk.Box(WorldMaterials.Glow(new Color(.3f, .7f, .85f), .5f), at + face * 6f + up * .05f, new Vector3(5f, .1f, 3f), rot);
            mk.Box(white, at + face * 6f, new Vector3(5.6f, .3f, 3.6f), rot);
        }

        void Olive(LandContext ctx, MeshKit mk, Vector3 at)
        {
            var up = Vector3.up; var bark = WorldMaterials.Solid(new Color(.45f, .4f, .35f)); var leaf = WorldMaterials.Solid(new Color(.5f, .58f, .45f));
            mk.Prism(bark, at - up * .2f, (up + new Vector3(ctx.Range(-.2f, .2f), 0, ctx.Range(-.2f, .2f))).normalized, .35f, 2.4f, 7, .7f);
            for (var k = 0; k < 4; k++) soft.Blob(leaf, at + up * (2.8f + ctx.Range(0f, .6f)) + new Vector3(ctx.Range(-1.3f, 1.3f), 0, ctx.Range(-1.3f, 1.3f)), new Vector3(1.6f, 1f, 1.6f), ctx.Rng, .3f);
            ArtistBits.PostCollider(ctx.Root, at, 2.4f, .4f);
        }

        void Vines(LandContext ctx, MeshKit mk, Vector3 at)
        {
            var up = Vector3.up; var dir = Quaternion.Euler(0, ctx.Range(0, 180f), 0) * Vector3.forward; var side = Vector3.Cross(up, dir);
            for (var r = 0; r < 5; r++) for (var k = 0; k < 8; k++) { var p = ctx.OnGround(at.x + side.x * (r - 2f) * 2.2f + dir.x * (k - 3.5f) * 1.6f, at.z + side.z * (r - 2f) * 2.2f + dir.z * (k - 3.5f) * 1.6f); soft.Blob(WorldMaterials.Solid(new Color(.35f, .5f, .25f)), p + up * .8f, new Vector3(.6f, .6f, .7f), ctx.Rng, .3f); if (k % 3 == 0) soft.Blob(WorldMaterials.Solid(new Color(.4f, .15f, .35f)), p + up * .5f + side * .4f, Vector3.one * .15f, ctx.Rng, .2f); }
        }

        void GroundCover(LandContext ctx)
        {
            var mat = WindMesh.Material(.25f, .9f, new Color(.45f, .58f, .35f));
            var meshes = new Dictionary<(int, int), WindMesh>(); var rng = new System.Random(1977);
            const float step = 2.4f;
            for (var x = ctx.MinX; x < ctx.MaxX; x += step)
            for (var z = ctx.MinZ; z < ctx.MaxZ; z += step)
            {
                var px = x + ((float)rng.NextDouble() - .5f) * step; var pz = z + ((float)rng.NextDouble() - .5f) * step;
                if (!ctx.IsLand(px, pz, 1f) || NearPath(px, pz, 3f) || InNoGrass(px, pz, 0f) || InFootprint(ctx, px, pz, -2f) || InBay(px, pz, 8f)) continue;
                if (Mathf.PerlinNoise(px * .025f + 6f, pz * .025f + 2f) < .4f) continue;
                var key = (Mathf.FloorToInt(px / 70f), Mathf.FloorToInt(pz / 70f));
                if (!meshes.TryGetValue(key, out var wm)) meshes[key] = wm = new WindMesh();
                var col = Color.Lerp(new Color(.45f, .58f, .35f), new Color(.65f, .65f, .4f), (float)rng.NextDouble());
                var at = new Vector3(px, ctx.Ground(px, pz) - .04f, pz);
                wm.Tuft(at, .35f + (float)rng.NextDouble() * .35f, col, Color.Lerp(col, new Color(1f, .85f, .6f), .3f), rng, 3, .07f);
                if (rng.NextDouble() < .06) wm.Flower(at + new Vector3(.1f, 0, .1f), .4f, new Color(.95f, .35f, .6f), rng);
            }
            foreach (var kv in meshes) kv.Value.Build("Riviera grass", ctx.Root, mat);
        }
    }
}
