using System.Collections.Generic;
using UnityEngine;

namespace EverythingWorld.World
{
    /// The Prog Marches (WORLD_GUIDE.md): a wide, strange marchland under a total eclipse, a black sun ringed by its
    /// corona, the sky deep violet overhead and amber at the horizon, stars out. Where you arrive stands the Prism, a
    /// huge crystal on the plain: a white beam falls into it from the eclipse and leaves split into a spectrum that
    /// runs across the valley. A road loops round it in a long figure of eight; standing stones are set out in 7/8
    /// (two, two, three); giant brass gears turn half-sunk in the ground beside a fallen clock face; pipe organs stand
    /// in the fields with their pipes glowing; a chrome Möbius ribbon; monoliths float over the marsh. Files: ProgKit
    /// (ground, the Prism, the loop road, the marvels, trails, the marches), ProgArtists, ProgLife.
    public partial class ProgKit : LandKit
    {
        public override string Land => "The Prog Marches";
        public override Vector3 ArtistFront(IslandWorldData data, IslandWorldData.Artist a) => facing.TryGetValue(a.name, out var f) ? f : base.ArtistFront(data, a);

        public const string TimeSong = "3TO7bbrUKrOSPGRTB5MeCz", BlueSky = "2RlgNHKcydI9sayD2Df2xp", WishYouWereHere = "6mFkJmJqdDVQ1REhVfGgd1",
            AnyColour = "6FBPOJLxUZEair6x4kLDhf", YYZ = "3lpDrxUkr0tIe1kmJvdK7d", IncaRoads = "1OLT5z6hQdZuNdljDPhWbh";
        public override (string song, string label, string artist)[] SpecialSongs => new[]
        {
            (TimeSong, "Time", "@the hall of clocks"), (BlueSky, "Mr. Blue Sky", "Electric Light Orchestra"), (WishYouWereHere, "Wish You Were Here", "Pink Floyd"),
            (AnyColour, "Any Colour You Like", "@the Prism"), (YYZ, "YYZ", "Rush"), (IncaRoads, "Inca Roads", "Frank Zappa"),
            (ComfortablyNumb, "Comfortably Numb", "@the wall"), (SheepSong, "Sheep", "@the sheep pen"), (HaveACigar, "Have a Cigar", "@the cigar office"),
            (AstronomyDomine, "Astronomy Domine", "@the orrery"), (Fearless, "Fearless", "@the tor"), (JoesGarage, "Joe's Garage", "Frank Zappa"),
            (Starless, "Starless", "King Crimson"), (SpiritOfRadio, "The Spirit Of Radio", "Rush"), (SiriusSong, "Sirius", "The Alan Parsons Project"),
        };

        public static readonly Color Amber = new Color(1f, .66f, .2f), Violet = new Color(.55f, .32f, .9f), Ink = new Color(.95f, .93f, .98f);
        public static readonly Color[] Spectrum = { new Color(1f, .15f, .12f), new Color(1f, .5f, .1f), new Color(1f, .9f, .15f), new Color(.2f, .9f, .3f), new Color(.15f, .45f, 1f), new Color(.55f, .2f, 1f) };
        const float TrailW = 2.6f;

        Material stone, darkStone, chrome, brass, dark, wood, paving, glass, reed;
        GlowHalos halos; Vector3 center; ProgLife life; MeshKit soft;
        readonly Dictionary<(int, int), List<Vector3>> pathCells = new Dictionary<(int, int), List<Vector3>>();
        readonly List<(Vector3 p, int w)> walkPoints = new List<(Vector3, int)>();
        readonly Dictionary<string, Vector3> facing = new Dictionary<string, Vector3>();
        readonly List<Vector3> noGrass = new List<Vector3>();
        readonly List<Vector2> loop = new List<Vector2>();
        Vector2 heart2;

        void MakeMaterials()
        {
            stone = WorldMaterials.Solid(new Color(.5f, .48f, .52f)); darkStone = WorldMaterials.Solid(new Color(.3f, .28f, .34f));
            chrome = WorldMaterials.Solid(new Color(.78f, .8f, .84f), .95f); brass = WorldMaterials.Solid(new Color(.8f, .6f, .25f), .85f);
            dark = WorldMaterials.Solid(new Color(.07f, .06f, .09f)); wood = WorldMaterials.Solid(new Color(.42f, .3f, .22f));
            paving = WorldMaterials.Solid(new Color(.55f, .53f, .58f)); reed = WorldMaterials.Solid(new Color(.55f, .5f, .35f));
            glass = new Material(WorldMaterials.Glow(new Color(.42f, .55f, .85f), .07f)) { name = "Prism glass" }; glass.SetFloat("_Smoothness", .95f);
        }

        Material GlowM(Color c, float s = 2f) => WorldMaterials.Glow(c, s * .65f);
        static bool HasStation(IslandWorldData.Artist a) => a.stationId != 0;
        static float Foot(IslandWorldData.Artist a) => a.name == "Pink Floyd" ? 20f : a.name == "Frank Zappa" ? 18f : a.name == "Rush" ? 14f : HasStation(a) || a.name == "The Alan Parsons Project" ? 12f : 8f;

        public override bool Ground(float height, float slope, float patch, out Color color)
        {
            if (slope > 9f) color = Color.Lerp(new Color(.4f, .38f, .44f), new Color(.5f, .48f, .52f), patch);
            else color = Color.Lerp(new Color(.3f, .38f, .3f), new Color(.46f, .46f, .34f), Mathf.SmoothStep(.2f, .9f, patch));
            color.a = 1f;
            return true;
        }

        /// The figure-of-eight road round the Prism: a long lazy loop crossing at the heart.
        Vector2 Loop(float t, float A, float B) => heart2 + new Vector2(Mathf.Sin(t) * A, Mathf.Sin(2f * t) * B);

        public override void ShapeGround(IslandWorldData data, int index)
        {
            var isl = data.layout.islands[index]; heart2 = new Vector2(isl.labelX, isl.labelZ);
            // as big a loop as the land holds
            loop.Clear(); float A = 150f, B = 70f;
            for (; A > 60f; A -= 10f, B -= 4f)
            {
                var ok = true;
                for (var k = 0; k < 64 && ok; k++) { var p = Loop(k * Mathf.PI * 2f / 64f, A, B); if (data.IslandAt(p.x, p.y) != isl) ok = false; else for (var j = 0; j < 8; j++) { var a = j * Mathf.PI / 4f; if (data.IslandAt(p.x + Mathf.Cos(a) * 30f, p.y + Mathf.Sin(a) * 30f) != isl) { ok = false; break; } } }
                if (ok) break;
            }
            for (var k = 0; k <= 200; k++) loop.Add(Loop(k * Mathf.PI * 2f / 200f, A, B));
            var avoid = new List<Vector3>();
            for (var k = 0; k < loop.Count; k += 4) avoid.Add(new Vector3(loop[k].x, 7f, loop[k].y));
            ArtistPlacement.Spread(data, index, 60f, heart2, 46f, 44f, avoid, null, Land, 320f);
            foreach (var a in data.layout.artists)
            {
                if (a.sceneId != isl.sceneId) continue;
                a.r = Mathf.Max(a.r, Foot(a) + 6f);
                Level(data, index, new Vector2(a.x, a.z), Foot(a) + 1f, Foot(a) + 12f);
            }
            Level(data, index, heart2, 26f, 46f);
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
                    var i = r * data.Nx + col; if (data.islandIds[i] != index) continue;
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
            life = new GameObject("Prog life").AddComponent<ProgLife>();
            life.transform.SetParent(ctx.Root, false);
            life.center = center;
            var mk = new MeshKit(); var solid = new MeshKit();
            Prism(ctx, mk, solid);
            LoopRoad(ctx, mk, solid);
            Trails(ctx, mk, solid);
            FloydWorld(ctx, mk, solid);
            Marvels(ctx, mk, solid);
            Marches(ctx, mk, solid);
            mk.Build("Prog Marches", ctx.Root, false);
            soft.Build("Soft prog reeds and heather", ctx.Root, false);   // "Soft…": walked through, so the land audit skips it
            solid.BuildCollider("Prog Marches walls", ctx.Root);
        }

        public override void Finish(LandContext ctx) { GroundCover(ctx); halos.Build("Prog glow", ctx.Root); }

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

        bool InBeam(float x, float z, float pad)
        {
            var p = new Vector2(x, z) - new Vector2(center.x, center.z); var along = Vector2.Dot(p, beamDir2);
            if (along < 0f || along > 170f) return false;
            return Mathf.Abs(Vector2.Dot(p, new Vector2(-beamDir2.y, beamDir2.x))) < along * .2f + 6f + pad;
        }

        bool Open(LandContext ctx, float x, float z, float pad) =>
            ctx.IsLand(x, z, 4f) && !InFootprint(ctx, x, z, pad) && !NearPath(x, z, pad + 2f) && !InNoGrass(x, z, pad) && !InBeam(x, z, pad)
            && Vector2.Distance(new Vector2(x, z), new Vector2(center.x, center.z)) > 34f + pad;

        void Strip(MeshKit mk, List<Vector3> pts, Material m, float w, float lift, LandContext ground = null)
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
                if (ground != null)
                {
                    float gl = ground.Ground(L[i].x, L[i].z), gr = ground.Ground(R[i].x, R[i].z), top = Mathf.Max(pts[i].y, Mathf.Max(gl, gr) - .05f);
                    L[i].y = Mathf.Max(gl, top - .15f) + lift; R[i].y = Mathf.Max(gr, top - .15f) + lift;
                }
            }
            for (var i = 0; i + 1 < n; i++) mk.Quad(m, L[i], R[i], R[i + 1], L[i + 1], (pts[i] + pts[i + 1]) / 2f - Vector3.up);
        }

        /// A tall chrome lamp with an amber globe.
        void Lamp(LandContext ctx, MeshKit mk, Vector3 g)
        {
            var up = Vector3.up;
            mk.Prism(chrome, g - up * .3f, up, .08f, 3.3f, 8); mk.Prism(chrome, g + up * 2.95f, up, .2f, .08f, 10);
            mk.Blob(GlowM(Amber, 2.6f), g + up * 3.25f, Vector3.one * .22f, ctx.Rng, 0f);
            halos.Add(g + up * 3.25f, 1.3f, Amber, .8f);
            ArtistBits.PostCollider(ctx.Root, g, 3f, .1f);
        }

        // ---------------------------------------------------------------- the Prism
        Vector2 beamDir2 = Vector2.right;

        /// The Prism: a crystal triangular prism 20 m tall on a dark stone dais. A white beam falls into it out of the
        /// sky from the eclipse; the spectrum leaves the far face and runs across the valley in six bands.
        void Prism(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var c = ctx.OnGround(center.x, center.z) ;
            // the beam runs toward the land's widest open stretch from the heart
            var best = 0f;
            for (var k = 0; k < 16; k++)
            {
                var a = k * Mathf.PI / 8f; var d = new Vector2(Mathf.Cos(a), Mathf.Sin(a)); var run = 0f;
                while (run < 200f && ctx.IsLand(center.x + d.x * (run + 10f), center.z + d.y * (run + 10f), 10f) && !InFootprint(ctx, center.x + d.x * (run + 10f), center.z + d.y * (run + 10f), 4f)) run += 10f;
                if (run > best) { best = run; beamDir2 = d; }
            }
            var bd = new Vector3(beamDir2.x, 0, beamDir2.y); var bs = Vector3.Cross(up, bd);
            var pc = c - bd * 14f; pc.y = c.y;   // the Prism stands back from where you arrive, the beam fans past you
            mk.Prism(darkStone, pc - up * .6f, up, 13f, 1.4f, 3, 1f, 0f); solid.Prism(darkStone, pc - up * .6f, up, 13f, 1.4f, 3);
            mk.Prism(stone, pc + up * .65f, up, 12f, .2f, 3);
            // the crystal: a triangle in plan, one edge facing the beam's way
            const float side = 15f, H = 20f;
            var corners = new Vector3[3];
            for (var k = 0; k < 3; k++) { var a = (k * 120f + 90f) * Mathf.Deg2Rad; corners[k] = pc + (bd * Mathf.Cos(a + Mathf.PI / 2f) + bs * Mathf.Sin(a + Mathf.PI / 2f)) * (side / Mathf.Sqrt(3f)) + up * .8f; }
            // order the corners so the face between 0 and 1 faces the beam out
            var mid = (corners[0] + corners[1] + corners[2]) / 3f;
            var gv = new List<Vector3>(); var gc = new List<Color>(); var gt = new List<int>();
            void GlassTri(Vector3 p0, Vector3 p1, Vector3 p2) { var b0 = gv.Count; gv.Add(p0); gv.Add(p1); gv.Add(p2); gc.Add(Color.white); gc.Add(new Color(1, 1, 1, .7f)); gc.Add(Color.white); gt.AddRange(new[] { b0, b0 + 1, b0 + 2 }); }
            for (var k = 0; k < 3; k++)
            {
                var a = corners[k]; var b = corners[(k + 1) % 3];
                GlassTri(a, b, b + up * H); GlassTri(a, b + up * H, a + up * H);
                mk.Box(GlowM(new Color(.75f, .85f, 1f), 1.6f), a + up * H / 2f, new Vector3(.12f, H, .12f), Quaternion.identity);
                mk.Prism(chrome, a, up, .25f, H, 6);
                mk.Box(chrome, (a + b) / 2f + up * H, new Vector3(.4f, .4f, Vector3.Distance(a, b)), Quaternion.LookRotation(b - a));
                mk.Box(chrome, (a + b) / 2f, new Vector3(.4f, .4f, Vector3.Distance(a, b)), Quaternion.LookRotation(b - a));
            }
            GlassTri(corners[0] + up * H, corners[1] + up * H, corners[2] + up * H);
            FadeMesh(ctx.Root, "Prism glass", gv, gc, gt, SeeThroughM(new Color(.7f, .82f, 1f, .2f), false));
            for (var k = 0; k < 4; k++) mk.Prism(GlowM(new Color(.8f, .88f, 1f), .8f), mid + up * (2f + k * 4.5f), up, (side / Mathf.Sqrt(3f)) * .5f, .05f, 3);   // faint inner light planes
            solid.Prism(glass, mid - up * .2f, up, side / Mathf.Sqrt(3f), H + 1f, 3);
            halos.Add(mid + up * H * .55f, 7f, new Color(.75f, .8f, 1f), .3f);
            ArtistBits.PointLight(ctx.Root, mid + up * 8f + bd * 10f, new Color(.9f, .9f, 1f), 30f, 1.6f);
            // the white beam in from the sky behind, and the spectrum out the front, running down to the ground
            var into = mid + up * H * .55f - bd * 3f; var from = into - bd * 120f + up * 140f;
            life.beamInto = into; life.beamFrom = (from - into).normalized;   // ProgLife draws it: a soft shaft out of the eclipse that fades into the sky
            var outOf = mid + up * H * .55f + bd * 4f; life.spectrum.Clear();
            for (var k = 0; k < 6; k++)
            {
                var m = SeeThroughM(Spectrum[k], true); m.name = "Spectrum " + k; life.spectrum.Add(m);
                var spread = (k - 2.5f) * .045f; var dir = (bd + bs * spread).normalized; var len = Mathf.Max(60f, best - 20f);
                var end = outOf + dir * len; end.y = Mathf.Max(ctx.Ground(end.x, end.z) + 5f, outOf.y - len * .08f);
                var s0 = outOf + bs * (k - 2.5f) * .5f;
                Ribbon(ctx.Root, "Spectrum band", s0, end, 1.3f, m);
            }
            life.prism = mid + up * H * .55f; life.beamDir = bd;
            noGrass.Add(new Vector3(pc.x, 14f, pc.z));
            ArtistBits.Label(ctx.Root, "THE PROG MARCHES", c + bs * 9f + up * 1.6f, -bd, .32f, Ink, TextAnchor.MiddleCenter);
            mk.Box(darkStone, c + bs * 9f + up * 1.6f + bd * .08f, new Vector3(4.6f, .7f, .1f), Quaternion.LookRotation(-bd));
            mk.Box(darkStone, c + bs * 9f + up * .6f + bd * .08f, new Vector3(.2f, 1.4f, .2f), Quaternion.LookRotation(-bd));
            var mark = new GameObject("Shot: the Prism").transform; mark.SetParent(ctx.Root, false);
            var cam = c + bd * 40f + bs * 22f; cam = ctx.OnGround(cam.x, cam.z); var look = mid - cam; look.y = 0; mark.SetPositionAndRotation(cam, Quaternion.LookRotation(look.normalized + up * .25f));
            Remember(c, 0);
        }

        // ---------------------------------------------------------------- the loop road
        void LoopRoad(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var pts = new List<Vector3>(); var lamps = 0;
            void Flush()
            {
                if (pts.Count > 3) { Strip(mk, pts, paving, 3.4f, .2f, ctx); foreach (var p in pts) Remember(p, 300); }
                pts.Clear();
            }
            for (var k = 0; k < loop.Count; k++)
            {
                var q = loop[k];
                if (!ctx.IsLand(q.x, q.y, 3f) || InFootprint(ctx, q.x, q.y, 2f) || Vector2.Distance(q, new Vector2(center.x, center.z)) < 16f) { Flush(); continue; }
                var p = ctx.OnGround(q.x, q.y); pts.Add(p);
                if (k % 12 == 6) { var nx = loop[Mathf.Min(k + 1, loop.Count - 1)] - loop[Mathf.Max(k - 1, 0)]; var sd = new Vector3(-nx.y, 0, nx.x).normalized * (lamps % 2 == 0 ? 2.6f : -2.6f); var lp = p + sd; Lamp(ctx, mk, ctx.OnGround(lp.x, lp.z)); lamps++; }
            }
            Flush();
        }

        // ---------------------------------------------------------------- trails
        void Trails(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var todo = new List<IslandWorldData.Artist>(ctx.Artists); var walk = 1; var lamps = new List<Vector3>();
            var dirt = WorldMaterials.Solid(new Color(.5f, .46f, .42f));
            while (todo.Count > 0)
            {
                IslandWorldData.Artist next = null; Vector3 join = default; var best = float.MaxValue;
                foreach (var a in todo)
                    foreach (var (p, w) in walkPoints) { if (w >= 400) continue; var d = Vector2.Distance(new Vector2(p.x, p.z), new Vector2(a.x, a.z)) - Foot(a); if (d < best) { best = d; next = a; join = p; } }
                if (next == null) break;
                todo.Remove(next);
                var from = new Vector3(join.x, 0, join.z); var to = new Vector3(next.x, 0, next.z);
                var dir = to - from; var total = dir.magnitude; if (total < 2f) continue; dir /= total;
                facing[next.name] = -dir;
                var side = Vector3.Cross(Vector3.up, dir); var end = total - Foot(next) * .8f;
                var pts = new List<Vector3>();
                for (var d = walk == 1 ? 10f : TrailW / 2f + .1f; ; d += 1.5f)
                {
                    var dd = Mathf.Min(d, end); var q = from + dir * dd + side * Mathf.Sin(dd / 14f) * Mathf.Min(4f, end * .06f);
                    if (!ctx.IsLand(q.x, q.z, 0f)) break;
                    pts.Add(ctx.OnGround(q.x, q.z)); if (dd >= end) break;
                }
                if (pts.Count < 2) continue;
                Strip(mk, pts, dirt, TrailW, .2f, ctx);
                foreach (var p in pts) Remember(p, walk);
                for (var i = 7; i < pts.Count; i += 13)
                {
                    var lp = pts[i] + side * ((i / 13) % 2 == 0 ? 1f : -1f) * (TrailW / 2f + .6f);
                    var near = false; foreach (var o in lamps) if ((o - lp).sqrMagnitude < 20f * 20f) near = true;
                    if (near) continue;
                    lamps.Add(lp); Lamp(ctx, mk, ctx.OnGround(lp.x, lp.z));
                }
                walk++;
            }
        }

        // ---------------------------------------------------------------- the marvels
        /// Standing stones in 7/8, the clockwork (giant gears and a fallen clock), pipe organs, the Möbius ribbon,
        /// floating monoliths.
        void Marvels(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var spots = new List<Vector3>();
            Vector3 Spot(float clear)
            {
                for (var tries = 0; tries < 600; tries++)
                {
                    var px = ctx.Range(ctx.MinX, ctx.MaxX); var pz = ctx.Range(ctx.MinZ, ctx.MaxZ);
                    if (!ctx.IsLand(px, pz, clear + 10f) || !Open(ctx, px, pz, clear)) continue;
                    var ok = Vector2.Distance(new Vector2(px, pz), new Vector2(center.x, center.z)) < 260f;
                    foreach (var s in spots) if ((new Vector2(s.x, s.z) - new Vector2(px, pz)).magnitude < clear + 40f) ok = false;
                    if (!ok) continue;
                    var p = ctx.OnGround(px, pz); spots.Add(p); noGrass.Add(new Vector3(p.x, clear * .8f, p.z)); return p;
                }
                return Vector3.positiveInfinity;
            }
            var stones = Spot(18f); if (!float.IsInfinity(stones.x)) Stones78(ctx, mk, solid, stones);
            var clock = Spot(20f); if (!float.IsInfinity(clock.x)) Clockwork(ctx, mk, solid, clock);
            for (var k = 0; k < 3; k++) { var o = Spot(8f); if (!float.IsInfinity(o.x)) Organ(ctx, mk, solid, o, k); }
            var mob = Spot(12f); if (!float.IsInfinity(mob.x)) Mobius(ctx, mk, solid, mob);
            for (var k = 0; k < 4; k++) { var f = Spot(7f); if (!float.IsInfinity(f.x)) Floater(ctx, mk, solid, f, k); }
        }

        void Stones78(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 c)
        {
            // three bars of 7/8 round a ring: 2 + 2 + 3 stones a bar, wider gaps between the groups
            var beats = new List<float>(); var at = 0f;
            for (var bar = 0; bar < 3; bar++) foreach (var g in new[] { 2, 2, 3 }) { for (var k = 0; k < g; k++) { beats.Add(at); at += 1f; } at += .9f; }
            for (var i = 0; i < beats.Count; i++)
            {
                var a = beats[i] / at * Mathf.PI * 2f; var p = ctx.OnGround(c.x + Mathf.Cos(a) * 14f, c.z + Mathf.Sin(a) * 14f);
                var h = (i % 7 == 0) ? 5.2f : ctx.Range(3f, 4.2f); var rot = Quaternion.LookRotation(new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)));
                mk.Box(darkStone, p + Vector3.up * (h / 2f - .3f), new Vector3(1.4f, h, .8f), rot * Quaternion.Euler(ctx.Range(-3f, 3f), 0, ctx.Range(-3f, 3f)));
                solid.Box(darkStone, p + Vector3.up * (h / 2f - .3f), new Vector3(1.4f, h, .8f), rot);
                mk.Box(GlowM(Violet, 2f), p + Vector3.up * (h * .65f) - rot * Vector3.forward * .41f, new Vector3(.12f, .9f, .02f), rot);
                life.runes.Add(p + Vector3.up * h * .65f);
            }
            mk.Box(darkStone, c + Vector3.up * .2f, new Vector3(3f, .5f, 1.6f), Quaternion.identity); solid.Box(darkStone, c + Vector3.up * .2f, new Vector3(3f, .5f, 1.6f), Quaternion.identity);
            halos.Add(c + Vector3.up * 1f, 3f, Violet, .4f);
            var mark = new GameObject("Shot: the stones in 7/8").transform; mark.SetParent(ctx.Root, false); var cam = ctx.OnGround(c.x + 24f, c.z + 10f); mark.SetPositionAndRotation(cam, Quaternion.LookRotation((c - cam).normalized));
        }

        /// The clockwork: giant brass gears standing half-sunk in the ground, meshing, turned by ProgLife; a fallen
        /// clock face beside them showing the real time.
        void Clockwork(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 c)
        {
            var up = Vector3.up; var face = new Vector3(center.x - c.x, 0, center.z - c.z).normalized; var side = Vector3.Cross(up, face);
            float[] radii = { 9f, 5.5f, 3.6f }; var x = -6f;
            for (var k = 0; k < 3; k++)
            {
                var r = radii[k]; var gc = c + side * x + up * (r * .45f) - face * k * .4f; x += r + (k + 1 < 3 ? radii[k + 1] : 0f) - .6f;
                var g = new MeshKit(); var teeth = Mathf.RoundToInt(r * 3f);
                g.Prism(k == 1 ? chrome : brass, new Vector3(0, 0, -.4f), Vector3.forward, r - .4f, .8f, teeth);
                g.Prism(dark, new Vector3(0, 0, -.45f), Vector3.forward, r * .25f, .9f, 10);
                for (var t = 0; t < teeth; t++) { var a = t * Mathf.PI * 2f / teeth; g.Box(k == 1 ? chrome : brass, new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0) * (r - .1f), new Vector3(.7f, .7f, .8f), Quaternion.Euler(0, 0, a * Mathf.Rad2Deg)); }
                for (var s = 0; s < 6; s++) { var a = s * 60f; g.Box(k == 1 ? chrome : brass, Quaternion.Euler(0, 0, a) * Vector3.right * r * .55f, new Vector3(r * .5f, .5f, .5f), Quaternion.Euler(0, 0, a)); }
                var go = g.BuildSingle("Gear", life.transform, false).transform; go.SetPositionAndRotation(gc, Quaternion.LookRotation(face));
                life.gears.Add(go); life.gearSpeed.Add((k % 2 == 0 ? 1f : -1f) * 9f / r);
                solid.Box(brass, gc, new Vector3(r * 1.6f, r * 2f, 1.2f), Quaternion.LookRotation(face));
            }
            // the fallen clock face, leaning back on the ground
            var cc = c + face * 12f + up * 1.2f; var crot = Quaternion.LookRotation(face) * Quaternion.Euler(-65f, 0, 0);
            mk.Prism(brass, cc - crot * Vector3.forward * .3f, crot * Vector3.forward, 6f, .3f, 32); mk.Prism(WorldMaterials.Solid(new Color(.95f, .92f, .82f)), cc + crot * Vector3.forward * .01f, crot * Vector3.forward, 5.6f, .02f, 32);
            for (var h = 0; h < 12; h++) { var a = h * 30f; mk.Box(dark, cc + crot * (Quaternion.Euler(0, 0, a) * Vector3.up * 4.9f) + crot * Vector3.forward * .04f, new Vector3(.2f, h % 3 == 0 ? 1f : .5f, .02f), crot * Quaternion.Euler(0, 0, a)); }
            solid.Box(brass, cc, new Vector3(12f, 1.6f, 6f), Quaternion.LookRotation(face));
            var hands = new MeshKit(); hands.Box(dark, new Vector3(0, 1.6f, 0), new Vector3(.35f, 3.2f, .06f), Quaternion.identity);
            var hourHand = hands.BuildSingle("Hour hand", life.transform, false).transform;
            var mm = new MeshKit(); mm.Box(dark, new Vector3(0, 2.3f, 0), new Vector3(.25f, 4.6f, .06f), Quaternion.identity);
            var minuteHand = mm.BuildSingle("Minute hand", life.transform, false).transform;
            life.clockCenter = cc + crot * Vector3.forward * .08f; life.clockRot = crot; life.hourHand = hourHand; life.minuteHand = minuteHand;
            ArtistBits.PointLight(ctx.Root, c + up * 8f + face * 8f, Amber, 26f, 1.6f);
            halos.Add(cc + up * 1f, 5f, Amber, .3f);
            var mark = new GameObject("Shot: the clockwork").transform; mark.SetParent(ctx.Root, false); var cam = c + face * 30f + side * 6f; cam = ctx.OnGround(cam.x, cam.z); var look = c + up * 3f - cam; mark.SetPositionAndRotation(cam, Quaternion.LookRotation(new Vector3(look.x, 0, look.z).normalized));
        }

        /// A pipe organ left in a field: a wooden console with keys, a rank of pipes rising behind it that glow amber.
        void Organ(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 c, int n)
        {
            var up = Vector3.up; var face = new Vector3(center.x - c.x, 0, center.z - c.z).normalized; var rot = Quaternion.LookRotation(face); var side = Vector3.Cross(up, face);
            mk.Box(wood, c + up * .6f, new Vector3(3f, 1.2f, 1.4f), rot); mk.Box(WorldMaterials.Solid(new Color(.95f, .93f, .88f)), c + up * 1.22f + face * .4f, new Vector3(2.6f, .06f, .4f), rot);
            mk.Box(dark, c + up * 1.26f + face * .45f, new Vector3(2.6f, .04f, .25f), rot); mk.Box(wood, c + up * 1.8f - face * .4f, new Vector3(3f, 1.2f, .3f), rot);
            solid.Box(wood, c + up * .9f, new Vector3(3.2f, 1.8f, 1.6f), rot);
            var pipeGlow = new Material(WorldMaterials.Glow(Amber, .5f)) { name = "Organ pipes " + n }; life.pipes.Add(pipeGlow);
            for (var k = 0; k < 13; k++)
            {
                var x = (k - 6f) * .55f; var h = 4f + (6f - Mathf.Abs(k - 6f)) * .9f; var pp = c - face * 1.6f + side * x;
                mk.Prism(chrome, pp, up, .22f, h, 8); mk.Prism(pipeGlow, pp + up * (h * .15f) + face * .01f, up, .23f, .35f, 8);
            }
            solid.Box(chrome, c - face * 1.6f + up * 4f, new Vector3(7.5f, 8f, .6f), rot);
            halos.Add(c - face * 1.2f + up * 2f, 3f, Amber, .4f);
            ArtistBits.PointLight(ctx.Root, c + up * 3f + face * 2f, Amber, 12f, 1.2f);
            life.organs.Add(c + up * 6f);
        }

        /// A chrome Möbius ribbon, 9 m across, standing on its edge on a plinth.
        void Mobius(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 c)
        {
            var up = Vector3.up; var o = c + up * 6f; const int n = 72; const float R = 4.5f, W = 1.6f;
            Vector3 P(float u, float v) { var a = u * Mathf.PI * 2f; var half = a / 2f; var radial = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)); var w = radial * Mathf.Cos(half) + up * Mathf.Sin(half); var p = radial * R + w * v; return o + Quaternion.Euler(90f, 0, 0) * p; }
            for (var i = 0; i < n; i++)
            {
                float u0 = i / (float)n, u1 = (i + 1) / (float)n;
                var a0 = P(u0, -W / 2f); var b0 = P(u0, W / 2f); var a1 = P(u1, -W / 2f); var b1 = P(u1, W / 2f);
                var inward = (a0 + b0) / 2f + Vector3.Cross(b0 - a0, a1 - a0).normalized * .2f;
                mk.Quad(chrome, a0, b0, b1, a1, inward); mk.Quad(chrome, a0, a1, b1, b0, (a0 + b0) / 2f - Vector3.Cross(b0 - a0, a1 - a0).normalized * .2f);
            }
            mk.Prism(darkStone, c - up * .3f, up, 2.4f, 1.4f, 8); solid.Prism(darkStone, c - up * .3f, up, 2.4f, 11f, 8);
            halos.Add(o, 6f, new Color(.7f, .75f, 1f), .2f);
            ArtistBits.PointLight(ctx.Root, o + Vector3.forward * 6f, new Color(.8f, .8f, 1f), 16f, 1.2f);
        }

        /// A monolith floating over the marsh, a ring of small stones turning under it (moved by ProgLife).
        void Floater(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 c, int k)
        {
            var g = new MeshKit(); g.Box(darkStone, Vector3.zero, new Vector3(2f, 6f + k, 1f), Quaternion.identity);
            g.Box(GlowM(Spectrum[(k * 2) % 6], 2f), new Vector3(0, 0, .51f), new Vector3(.15f, 4f + k, .02f), Quaternion.identity);
            var t = g.BuildSingle("Floating monolith", life.transform, false).transform; t.position = c + Vector3.up * (5f + k);
            life.floaters.Add(t); life.floatBase.Add(c + Vector3.up * (5f + k));
            for (var s = 0; s < 6; s++) { var a = s * 60f * Mathf.Deg2Rad; mk.Blob(stone, c + new Vector3(Mathf.Cos(a), .2f, Mathf.Sin(a)) * 2.4f, new Vector3(.45f, .3f, .4f), ctx.Rng, .35f); }
            mk.Prism(GlowM(Spectrum[(k * 2) % 6], 1.2f), c + Vector3.up * .02f, Vector3.up, 1.6f, .03f, 16);
        }

        // ---------------------------------------------------------------- the marches
        /// Mostly open marchland: heather and reeds in the low ground, twisted bare trees, boulders, small cairns,
        /// little pools of still water reflecting the sky, odd old signposts.
        void Marches(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            int trees = 0, rocks = 0, pools = 0, cairns = 0;
            const float cell = 19f; var bark = WorldMaterials.Solid(new Color(.25f, .22f, .26f)); var pool = new Material(WorldMaterials.Glow(new Color(.16f, .13f, .3f), .1f)) { name = "Marsh pool" }; pool.SetFloat("_Smoothness", .55f);
            for (var x = ctx.MinX; x < ctx.MaxX; x += cell)
            for (var z = ctx.MinZ; z < ctx.MaxZ; z += cell)
            {
                float px = x + ctx.Range(0f, cell), pz = z + ctx.Range(0f, cell);
                if (!Open(ctx, px, pz, 3f)) continue;
                var p = ctx.OnGround(px, pz); var roll = ctx.Range(0f, 1f);
                if (roll < .22f) { TwistedTree(ctx, mk, bark, p); trees++; }
                else if (roll < .3f) { mk.Blob(stone, p + Vector3.up * .5f, new Vector3(ctx.Range(1f, 2.4f), ctx.Range(.6f, 1.4f), ctx.Range(1f, 2f)), ctx.Rng, .4f); solid.Box(stone, p + Vector3.up * .6f, new Vector3(2.6f, 1.3f, 2.4f), Quaternion.identity); rocks++; }
                else if (roll < .36f && FlatEnough(ctx, p, 5f)) { var r = ctx.Range(2.5f, 5f); for (var k = 0; k < 16; k++) { float a0 = k * Mathf.PI / 8f, a1 = (k + 1) * Mathf.PI / 8f; var w = p + Vector3.up * .05f; mk.Tri(pool, w, w + new Vector3(Mathf.Cos(a0), 0, Mathf.Sin(a0)) * r, w + new Vector3(Mathf.Cos(a1), 0, Mathf.Sin(a1)) * r, w - Vector3.up); } for (var k = 0; k < 8; k++) { var a = ctx.Range(0, Mathf.PI * 2f); var q = p + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * (r + .3f); soft.Box(reed, q + Vector3.up * .6f, new Vector3(.05f, 1.2f, .05f), Quaternion.Euler(ctx.Range(-8f, 8f), 0, ctx.Range(-8f, 8f))); } noGrass.Add(new Vector3(p.x, r + .5f, p.z)); pools++; }
                else if (roll < .39f) { for (var k = 0; k < 5; k++) mk.Blob(stone, p + Vector3.up * (.3f + k * .45f), Vector3.one * (.6f - k * .09f), ctx.Rng, .3f); solid.Box(stone, p + Vector3.up * 1f, new Vector3(1.2f, 2f, 1.2f), Quaternion.identity); cairns++; }
                else if (roll < .55f) { for (var k = 0; k < 4; k++) soft.Blob(WorldMaterials.Solid(new Color(.45f, .3f, .5f)), p + new Vector3(ctx.Range(-1.2f, 1.2f), .3f, ctx.Range(-1.2f, 1.2f)), new Vector3(.7f, .35f, .7f), ctx.Rng, .3f); }
            }
            Debug.Log($"The Prog Marches: {loop.Count} loop points, {trees} twisted trees, {rocks} boulders, {pools} pools, {cairns} cairns");
        }

        void TwistedTree(LandContext ctx, MeshKit mk, Material bark, Vector3 at)
        {
            var up = Vector3.up; var p = at - up * .3f; var dir = up; var s = ctx.Range(.8f, 1.3f);
            for (var k = 0; k < 4; k++)
            {
                var nd = (up + new Vector3(ctx.Range(-.6f, .6f), 0, ctx.Range(-.6f, .6f))).normalized; mk.Prism(bark, p, nd, (.4f - k * .08f) * s, 2f * s, 6, .8f); p += nd * 1.9f * s;
                if (k >= 1) { var bd = (new Vector3(ctx.Range(-1f, 1f), .4f, ctx.Range(-1f, 1f))).normalized; mk.Prism(bark, p, bd, .12f * s, 2f * s, 5, .2f); }
            }
            ArtistBits.PostCollider(ctx.Root, at, 4f, .4f * s);
        }

        void GroundCover(LandContext ctx)
        {
            var mat = WindMesh.Material(.3f, 1f, new Color(.38f, .42f, .32f));
            var meshes = new Dictionary<(int, int), WindMesh>(); var rng = new System.Random(1973);
            const float step = 2.4f; Color[] flowers = { new Color(.7f, .45f, 1f), new Color(1f, .7f, .25f), new Color(.85f, .85f, 1f) };
            for (var x = ctx.MinX; x < ctx.MaxX; x += step)
            for (var z = ctx.MinZ; z < ctx.MaxZ; z += step)
            {
                var px = x + ((float)rng.NextDouble() - .5f) * step; var pz = z + ((float)rng.NextDouble() - .5f) * step;
                if (!ctx.IsLand(px, pz, 1f) || NearPath(px, pz, 2.6f) || InNoGrass(px, pz, 0f) || InFootprint(ctx, px, pz, -2f)) continue;
                if (Mathf.PerlinNoise(px * .03f + 4f, pz * .03f + 8f) < .36f) continue;
                var key = (Mathf.FloorToInt(px / 70f), Mathf.FloorToInt(pz / 70f));
                if (!meshes.TryGetValue(key, out var wm)) meshes[key] = wm = new WindMesh();
                var col = Color.Lerp(new Color(.36f, .42f, .3f), new Color(.52f, .5f, .36f), (float)rng.NextDouble());
                var at = new Vector3(px, ctx.Ground(px, pz) - .04f, pz);
                wm.Tuft(at, .5f + (float)rng.NextDouble() * .5f, col, Color.Lerp(col, new Color(.85f, .8f, .6f), .3f), rng, 4, .06f);
                if (rng.NextDouble() < .06) wm.Flower(at + new Vector3(.1f, 0, .1f), .35f, flowers[rng.Next(flowers.Length)], rng);
            }
            foreach (var kv in meshes) kv.Value.Build("Prog grass", ctx.Root, mat);
        }
    }
}
