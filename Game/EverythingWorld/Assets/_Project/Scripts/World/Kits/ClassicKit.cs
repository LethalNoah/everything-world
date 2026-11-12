using System.Collections.Generic;
using UnityEngine;

namespace EverythingWorld.World
{
    /// The Kingdom of Classic Rock (WORLD_GUIDE.md): legendary, timeless, a place everyone already knows. A bright,
    /// clear, warm afternoon (the brightest land yet: every land before it is dusk or night) over old green hills
    /// worn smooth, two-lane country roads between dry-stone walls, hedgerows and big oaks. On the highest hill a
    /// crumbling stadium stands like a castle; standing stones shaped like records, jukeboxes in lay-bys, vintage
    /// cars parked up. Each legend evokes its own world without copying a cover: a yellow submarine on the pond and
    /// a zebra crossing for the Beatles, an airship moored on a mast and a stair into the sky for Led Zeppelin, a
    /// lightning bolt and a rocket for Bowie, and so on (ClassicArtists). Songs set things off (ClassicLife).
    /// Files: ClassicKit (ground, layout, roads, stadium, the country), ClassicArtists, ClassicLife.
    public partial class ClassicKit : LandKit
    {
        public override string Land => "Kingdom of Classic Rock";
        public override Vector3 ArtistFront(IslandWorldData data, IslandWorldData.Artist a) => facing.TryGetValue(a.name, out var f) ? f : base.ArtistFront(data, a);

        public const string Lucy = "25yQPHgC35WNnnOUqFhgVR", Stairway = "5CQ30WqJwcep0pYcV4AMNc", SpaceOddity = "72Z17vmmeQKAg8bptWvpVG", PurpleHaze = "0wJoRiX5K5BxlqZTolB2LD", Bohemian = "7tFiyTwD0nx5a1eklYtX2J";
        public override (string song, string label, string artist)[] SpecialSongs => new[] { (Lucy, "Lucy in the Sky with Diamonds", "The Beatles"), (Stairway, "Stairway to Heaven", "Led Zeppelin"), (SpaceOddity, "Space Oddity", "David Bowie"), (PurpleHaze, "Purple Haze", "Jimi Hendrix"), (Bohemian, "Bohemian Rhapsody", "Queen"),
            (Walrus, "I Am The Walrus", "@the egg on the wall"), (Immigrant, "Immigrant Song", "@the longship"), (LifeOnMars, "Life on Mars?", "@the red planet"), (Watchtower, "All Along the Watchtower", "@the watchtower") };

        static readonly Color Grass = new Color(.42f, .62f, .3f), GrassB = new Color(.58f, .66f, .32f), Gold = new Color(.8f, .72f, .38f), Stone = new Color(.72f, .7f, .64f), Ink = new Color(.97f, .96f, .92f);
        static readonly Color[] Paint = { new Color(.85f, .2f, .2f), new Color(.2f, .45f, .8f), new Color(.95f, .8f, .2f), new Color(.2f, .6f, .4f), new Color(.95f, .95f, .9f), new Color(.15f, .15f, .18f) };
        const float RoadW = 5f;

        Material stoneM, wallM, asphalt, wood, dark, metal, hedge, oakLeaf, bark, white;
        GlowHalos halos;
        Vector3 center;
        readonly Dictionary<(int, int), List<Vector3>> pathCells = new Dictionary<(int, int), List<Vector3>>();
        readonly List<(Vector3 p, int w)> walkPoints = new List<(Vector3, int)>();
        readonly Dictionary<string, Vector3> facing = new Dictionary<string, Vector3>();
        readonly List<Vector3> noGrass = new List<Vector3>();
        ClassicLife life;
        MeshKit soft;
        // the pond by the Beatles (ShapeGround)
        bool hasPond; Vector2 pondC; float pondR = 22f, pondLevel;
        bool hasStadium; Vector2 stadiumC;

        void MakeMaterials()
        {
            stoneM = WorldMaterials.Solid(Stone); wallM = WorldMaterials.Solid(new Color(.62f, .6f, .54f));
            asphalt = WorldMaterials.Solid(new Color(.36f, .36f, .38f), .2f); wood = WorldMaterials.Solid(new Color(.5f, .38f, .26f));
            dark = WorldMaterials.Solid(new Color(.1f, .1f, .11f)); metal = WorldMaterials.Solid(new Color(.75f, .76f, .78f), .7f);
            hedge = WorldMaterials.Solid(new Color(.25f, .45f, .22f)); oakLeaf = WorldMaterials.Solid(new Color(.32f, .52f, .24f));
            bark = WorldMaterials.Solid(new Color(.38f, .3f, .24f)); white = WorldMaterials.Solid(new Color(.96f, .95f, .92f));
        }

        Material GlowM(Color c, float s = 2f) => WorldMaterials.Glow(c, s * .65f);
        static bool HasStation(IslandWorldData.Artist a) => a.station || a.stationId != 0;
        static float Foot(IslandWorldData.Artist a) => a.name == "The Beatles" ? 24f : a.name == "Led Zeppelin" ? 22f : HasStation(a) ? 15f : 9f;

        public override bool Ground(float height, float slope, float patch, out Color color)
        {
            if (slope > 7f) color = Color.Lerp(new Color(.55f, .55f, .5f), new Color(.62f, .6f, .55f), patch);
            else if (height < 3f) color = new Color(.78f, .72f, .55f);
            else color = Color.Lerp(Grass, patch > .62f ? Gold : GrassB, Mathf.SmoothStep(.2f, .9f, patch));
            color.a = 1f;
            return true;
        }

        /// Artists spread over the hills about 100 m apart round where you arrive; level pads for the big sets; a
        /// round pond dug next to the Beatles for the submarine.
        public override void ShapeGround(IslandWorldData data, int index)
        {
            var isl = data.layout.islands[index];
            var heart = new Vector2(isl.labelX, isl.labelZ);
            ArtistPlacement.Spread(data, index, 100f, heart, 50f, 45f, null, null, Land);
            IslandWorldData.Artist beatles = null;
            foreach (var a in data.layout.artists)
            {
                if (a.sceneId != isl.sceneId) continue;
                a.r = Mathf.Max(a.r, Foot(a) + 8f);
                if (a.name == "The Beatles") beatles = a;
                if (HasStation(a)) ElectricKitLevel(data, index, new Vector2(a.x, a.z), Foot(a) + 2f, Foot(a) + 18f);
            }
            ElectricKitLevel(data, index, heart, 12f, 30f);
            hasPond = false;
            if (beatles != null)
            {
                // the pond sits just behind the Beatles' area, away from where you arrive
                var away = new Vector2(beatles.x, beatles.z) - heart; away = away.sqrMagnitude < 1f ? Vector2.up : away.normalized;
                pondC = new Vector2(beatles.x, beatles.z) + away * (Foot(beatles) + pondR + 4f);
                var rim = new List<float>();
                for (var k = 0; k < 32; k++) { var a = k * Mathf.PI / 16f; rim.Add(data.HeightAt(pondC.x + Mathf.Cos(a) * pondR, pondC.y + Mathf.Sin(a) * pondR)); }
                rim.Sort(); pondLevel = rim[4];
                var isl0 = data.IslandAt(pondC.x, pondC.y);
                if (isl0 == isl && pondLevel > 3f)
                {
                    hasPond = true;
                    for (var r = 0; r < data.Nz; r++)
                    {
                        var z = data.layout.depth - r * data.Step; if (Mathf.Abs(z - pondC.y) > pondR + 16f) continue;
                        for (var c = 0; c < data.Nx; c++)
                        {
                            var x = c * data.Step; if (Mathf.Abs(x - pondC.x) > pondR + 16f) continue;
                            var i = r * data.Nx + c; if (data.islandIds[i] != index) continue;
                            var d = Vector2.Distance(new Vector2(x, z), pondC);
                            if (d < pondR) data.heights[i] = pondLevel - 1.4f;
                            else if (d < pondR + 14f) data.heights[i] = Mathf.Lerp(pondLevel + .2f, data.heights[i], Mathf.SmoothStep(0, 1, (d - pondR) / 14f));
                        }
                    }
                }
            }
            PickStadium(data, index, heart);
            foreach (var (at, flat, blend) in new[] { (BalloonAt, 9f, 16f), (MazeAt, 15f, 24f), (FeteAt, 17f, 26f), (CricketAt, 28f, 40f) }) ElectricKitLevel(data, index, at, flat, blend);
        }

        static void ElectricKitLevel(IslandWorldData data, int index, Vector2 c, float flat, float blend)
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
            halos = new GlowHalos();
            pathCells.Clear(); walkPoints.Clear(); facing.Clear(); noGrass.Clear(); hasCastle = false;
            center = ctx.Plaza;
            life = new GameObject("Classic life").AddComponent<ClassicLife>();
            life.transform.SetParent(ctx.Root, false);
            life.center = center;
            var mk = new MeshKit(); var solid = new MeshKit(); soft = new MeshKit();
            Village(ctx, mk, solid);
            Roads(ctx, mk, solid);
            Pond(ctx, mk);
            Hall(ctx, mk, solid);
            Pub(ctx, mk, solid);
            Castle(ctx, mk, solid);
            Legends(ctx, mk, solid);
            KingdomPlaces(ctx, mk, solid);   // after everything that was here, before the scatter (which then keeps clear)
            Country(ctx, mk, solid);
            mk.Build("Kingdom", ctx.Root, false);
            solid.BuildCollider("Kingdom walls", ctx.Root);
        }

        public override void Finish(LandContext ctx)
        {
            GroundCover(ctx);
            halos.Build("Kingdom glow", ctx.Root);
            soft.Build("Soft kingdom", ctx.Root, false);   // last, so the oaks the legends' places plant keep their leaves
        }

        // ---------------------------------------------------------------- helpers
        void Remember(Vector3 p, int walk)
        {
            var key = (Mathf.FloorToInt(p.x / 8f), Mathf.FloorToInt(p.z / 8f));
            if (!pathCells.TryGetValue(key, out var l)) pathCells[key] = l = new List<Vector3>();
            l.Add(p); walkPoints.Add((p, walk));
        }

        bool NearPath(float x, float z, float within)
        {
            int cx = Mathf.FloorToInt(x / 8f), cz = Mathf.FloorToInt(z / 8f);
            var n = Mathf.CeilToInt(within / 8f);
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

        bool InFootprint(LandContext ctx, float x, float z, float pad, float scale = 1f)
        {
            foreach (var a in ctx.Artists) { float dx = x - a.x, dz = z - a.z, r = Foot(a) * scale + pad; if (dx * dx + dz * dz < r * r) return true; }
            return false;
        }

        bool InPond(float x, float z, float pad) => hasPond && Vector2.Distance(new Vector2(x, z), pondC) < pondR + pad;

        bool Open(LandContext ctx, float x, float z, float pad) =>
            ctx.IsLand(x, z, 4f) && !InFootprint(ctx, x, z, pad) && !NearPath(x, z, pad + 2f) && !InNoGrass(x, z, pad) && !InPond(x, z, pad + 4f)
            && Vector2.Distance(new Vector2(x, z), new Vector2(center.x, center.z)) > 22f + pad;

        /// A tree: a broad oak with a few clumps of leaves on spreading limbs.
        void Oak(LandContext ctx, MeshKit mk, Vector3 at, float scale)
        {
            var up = Vector3.up; var h = 4f * scale;
            mk.Prism(bark, at - up * .3f, up, .45f * scale, h, 8, .7f);
            var crown = at + up * h;
            for (var k = 0; k < 5; k++)
            {
                var d = (Quaternion.Euler(0, k * 72f + ctx.Range(-15f, 15f), 0) * Vector3.forward + up * .5f).normalized;
                mk.Prism(bark, crown - up * .4f, d, .18f * scale, 2.4f * scale, 6, .5f);
                soft.Blob(oakLeaf, crown + d * 2.6f * scale + up * .6f * scale, new Vector3(2.2f, 1.6f, 2.2f) * scale, ctx.Rng, .25f);
            }
            soft.Blob(oakLeaf, crown + up * 1.8f * scale, new Vector3(2.8f, 2f, 2.8f) * scale, ctx.Rng, .25f);
            ArtistBits.PostCollider(ctx.Root, at, h, .5f * scale);
            noGrass.Add(new Vector3(at.x, 1.4f * scale, at.z));
        }

        /// A lamp post of the old kind: a black iron post with a glass lantern.
        Renderer Lamp(LandContext ctx, MeshKit mk, Transform parent, Vector3 g, float h = 3.2f)
        {
            mk.Prism(dark, g - Vector3.up * .3f, Vector3.up, .07f, h + .3f, 6);
            var top = g + Vector3.up * h;
            mk.Box(dark, top + Vector3.up * .45f, new Vector3(.45f, .08f, .45f), Quaternion.identity);
            var go = new GameObject("Lamp"); go.transform.SetParent(parent, false);
            var lk = new MeshKit(); lk.Box(GlowM(new Color(1f, .9f, .65f), 2.2f), Vector3.zero, new Vector3(.35f, .45f, .35f), Quaternion.identity);
            var built = lk.BuildSingle("Glass", go.transform, false); go.transform.position = top + Vector3.up * .2f;
            halos.Add(top + Vector3.up * .2f, 1.3f, new Color(1f, .9f, .65f), .7f);
            ArtistBits.PostCollider(ctx.Root, g, h, .1f);
            return built.GetComponent<Renderer>();
        }

        // ---------------------------------------------------------------- where you arrive: a village green
        /// A village green where you arrive: a red phone box, a bench, a signpost, a war-memorial-style stone
        /// cross with a guitar instead, a bandstand-sized oak.
        void Village(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var c = ctx.OnGround(center.x, center.z); var up = Vector3.up;
            Oak(ctx, mk, ctx.OnGround(c.x + 17f, c.z + 13f), 1.7f);
            var box = ctx.OnGround(c.x - 8f, c.z + 4f); var red = WorldMaterials.Solid(new Color(.8f, .12f, .12f));
            mk.Box(red, box + up * 1.3f, new Vector3(1.1f, 2.6f, 1.1f), Quaternion.identity);
            mk.Box(red, box + up * 2.75f, new Vector3(1.2f, .3f, 1.2f), Quaternion.identity);
            foreach (var d in new[] { Vector3.forward, Vector3.back, Vector3.left, Vector3.right }) mk.Box(GlowM(new Color(1f, .95f, .8f), 1.6f), box + up * 1.6f + d * .56f, new Vector3(Mathf.Abs(d.z) > 0 ? .7f : .02f, 1.2f, Mathf.Abs(d.x) > 0 ? .7f : .02f), Quaternion.identity);
            solid.Box(red, box + up * 1.3f, new Vector3(1.1f, 2.6f, 1.1f), Quaternion.identity);
            // a stone column topped with a stone guitar, the land's name round it
            mk.Prism(stoneM, c - up * .2f, up, 1.6f, .6f, 8);
            mk.Prism(stoneM, c + up * .4f, up, .5f, 3.6f, 8, .8f);
            Guitar(mk, ctx.Rng, c + up * 4f, Vector3.forward, new Color(.95f, .76f, .3f), 2f, 0f);
            for (var k = 0; k < 6; k++) { var a = k * Mathf.PI / 3f + .3f; Banner(ctx, mk, ctx.OnGround(c.x + Mathf.Cos(a) * 14f, c.z + Mathf.Sin(a) * 14f), k % 2 == 0); }
            solid.Prism(stoneM, c - up * .2f, up, 1.6f, 4.4f, 8);
            ArtistBits.Label(ctx.Root, "THE KINGDOM OF CLASSIC ROCK", c + up * 1f + Vector3.forward * 1.65f, Vector3.forward, .3f, new Color(.25f, .22f, .2f), TextAnchor.MiddleCenter);
            noGrass.Add(new Vector3(c.x, 5f, c.z));
        }

        // ---------------------------------------------------------------- roads
        /// Two-lane country roads from the village to every artist, branching where each joins (so they never
        /// cross), a white line down the middle, dry-stone walls along stretches of them, lamps now and then.
        void Roads(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            Remember(center, 0);
            var todo = new List<IslandWorldData.Artist>(ctx.Artists);
            var walls = new List<(Vector3 a, Vector3 b, int road, float side)>(); var road = 0;   // wall stretches so far, so two roads' walls never cross
            var walk = 1; var lamps = new List<Vector3>();
            var line = WorldMaterials.Solid(new Color(.95f, .94f, .9f));
            while (todo.Count > 0)
            {
                IslandWorldData.Artist next = null; Vector3 join = default; var best = float.MaxValue;
                foreach (var a in todo)
                    foreach (var (p, _) in walkPoints)
                    {
                        var d = Vector2.Distance(new Vector2(p.x, p.z), new Vector2(a.x, a.z)) - Foot(a);
                        if (d < best) { best = d; next = a; join = p; }
                    }
                if (next == null) break;
                todo.Remove(next);
                var from = new Vector3(join.x, 0, join.z); var to = new Vector3(next.x, 0, next.z);
                var dir = to - from; var total = dir.magnitude; if (total < 2f) continue;
                dir /= total;
                facing[next.name] = -dir;
                var side = Vector3.Cross(Vector3.up, dir);
                var end = total - Foot(next) * .85f;
                var pts = new List<Vector3>();
                for (var d = walk == 1 ? 6f : RoadW / 2f + .1f; ; d += 3f)
                {
                    var dd = Mathf.Min(d, end);
                    var q = from + dir * dd + side * Mathf.Sin(dd / Mathf.Max(end, 1f) * Mathf.PI) * Mathf.Min(8f, end * .06f);
                    if (!ctx.IsLand(q.x, q.z, 0f) || InPond(q.x, q.z, 2f)) break;
                    pts.Add(ctx.OnGround(q.x, q.z));
                    if (dd >= end) break;
                }
                if (pts.Count < 2) continue;
                Strip(mk, pts, asphalt, RoadW, .06f);
                for (var i = 1; i < pts.Count; i += 2) { var a = pts[i - 1]; var b = pts[i]; mk.Box(line, (a + b) / 2f + Vector3.up * .075f, new Vector3(.15f, .02f, Vector3.Distance(a, b) * .6f), Quaternion.LookRotation(b - a)); }
                foreach (var p in pts) Remember(p, walk);
                // dry-stone walls along the middle of longer roads, both sides, with gaps
                road++;
                if (pts.Count > 12)
                    foreach (var s in new[] { -1f, 1f })
                        for (var i = 3; i < pts.Count - 4; i++)
                        {
                            if ((i / 6) % 3 == 2) continue;
                            var a = pts[i] + side * s * (RoadW / 2f + 1.6f); var b = pts[i + 1] + side * s * (RoadW / 2f + 1.6f);
                            a = ctx.OnGround(a.x, a.z); b = ctx.OnGround(b.x, b.z);
                            var cross = false; foreach (var w in walls) if ((w.road != road || w.side != s) && WallGap(w.a, w.b, a, b) < 3f) cross = true;   // segment to segment, so two walls can't cross mid-stretch either (their tops met in one plane and flickered)
                            if (cross) continue;
                            walls.Add((a, b, road, s));
                            var mid = (a + b) / 2f + Vector3.up * .45f;
                            mk.Box(wallM, mid, new Vector3(.7f, .9f, Vector3.Distance(a, b) + .05f), Quaternion.LookRotation(b - a));
                            // the coping sits along the wall's own up (on a slope, world up slid it along the wall until its
                            // ends met the wall's ends in one plane) and overhangs each end by 6 cm
                            var wr = Quaternion.LookRotation(b - a);
                            mk.Box(stoneM, mid + wr * Vector3.up * .5f, new Vector3(.8f, .12f, Vector3.Distance(a, b) + .17f), wr);
                            solid.Box(wallM, mid + Vector3.up * .1f, new Vector3(.7f, 1.1f, Vector3.Distance(a, b) + .05f), Quaternion.LookRotation(b - a));
                        }
                for (var i = 4; i < pts.Count; i += 9)
                {
                    var lp = pts[i] + side * ((i / 9) % 2 == 0 ? 1f : -1f) * (RoadW / 2f + .7f);
                    var near = false; foreach (var o in lamps) if ((o - lp).sqrMagnitude < 30f * 30f) near = true;
                    if (near) continue;
                    lamps.Add(lp);
                    Lamp(ctx, mk, ctx.Root, ctx.OnGround(lp.x, lp.z));
                }
                for (var i = 8; i < pts.Count - 3; i += 9)
                {
                    var bp = pts[i] + side * ((i / 9) % 2 == 0 ? -1f : 1f) * (RoadW / 2f + 1f);
                    Banner(ctx, mk, ctx.OnGround(bp.x, bp.z), (i / 9 + walk) % 2 == 0);
                }
                walk++;
            }
        }

        /// How close two wall stretches come on the ground (0 when they cross).
        static float WallGap(Vector3 p1, Vector3 p2, Vector3 q1, Vector3 q2)
        {
            Vector2 a = new Vector2(p1.x, p1.z), b = new Vector2(p2.x, p2.z), c = new Vector2(q1.x, q1.z), d = new Vector2(q2.x, q2.z);
            float Side(Vector2 o, Vector2 e, Vector2 p) => (e.x - o.x) * (p.y - o.y) - (e.y - o.y) * (p.x - o.x);
            if (Side(a, b, c) * Side(a, b, d) < 0f && Side(c, d, a) * Side(c, d, b) < 0f) return 0f;
            float ToSeg(Vector2 p, Vector2 o, Vector2 e) { var oe = e - o; var t = oe.sqrMagnitude < 1e-6f ? 0f : Mathf.Clamp01(Vector2.Dot(p - o, oe) / oe.sqrMagnitude); return Vector2.Distance(p, o + oe * t); }
            return Mathf.Min(Mathf.Min(ToSeg(a, c, d), ToSeg(b, c, d)), Mathf.Min(ToSeg(c, a, b), ToSeg(d, a, b)));
        }

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

        // ---------------------------------------------------------------- the pond
        void Pond(LandContext ctx, MeshKit mk)
        {
            if (!hasPond) return;
            var water = new Material(WorldMaterials.Glow(new Color(.3f, .52f, .62f), .1f)) { name = "Pond" };
            water.SetFloat("_Smoothness", .6f);
            var c = new Vector3(pondC.x, pondLevel - .3f, pondC.y);
            const int n = 40;
            for (var k = 0; k < n; k++)
            {
                float a0 = k * Mathf.PI * 2f / n, a1 = (k + 1) * Mathf.PI * 2f / n;
                mk.Tri(water, c, c + new Vector3(Mathf.Cos(a0), 0, Mathf.Sin(a0)) * (pondR + 3f), c + new Vector3(Mathf.Cos(a1), 0, Mathf.Sin(a1)) * (pondR + 3f), c - Vector3.up);
            }
            for (var k = 0; k < 30; k++)
            {
                var a = ctx.Range(0, Mathf.PI * 2f); var r = pondR + ctx.Range(1f, 3.5f);
                var p = ctx.OnGround(pondC.x + Mathf.Cos(a) * r, pondC.y + Mathf.Sin(a) * r);
                for (var j = 0; j < 3; j++) soft.Box(hedge, p + new Vector3(ctx.Range(-.3f, .3f), .7f, ctx.Range(-.3f, .3f)), new Vector3(.05f, 1.4f, .05f), Quaternion.Euler(ctx.Range(-10f, 10f), 0, ctx.Range(-10f, 10f)));
            }
            life.pond = c;
            noGrass.Add(new Vector3(pondC.x, pondR + 3f, pondC.y));
        }

        // ---------------------------------------------------------------- the stadium on the hill
        const float StadiumR = 20f;

        /// The stadium's hill: the highest spot well inside the land and clear of the artists, its top cut flat.
        void PickStadium(IslandWorldData data, int index, Vector2 heart)
        {
            var isl = data.layout.islands[index]; hasStadium = false; var bestH = float.MinValue;
            for (var r = 0; r < data.Nz; r += 3)
            for (var col = 0; col < data.Nx; col += 3)
            {
                var i = r * data.Nx + col; if (data.islandIds[i] != index) continue;
                float x = col * data.Step, z = data.layout.depth - r * data.Step; var p = new Vector2(x, z);
                if (data.heights[i] <= bestH || Vector2.Distance(p, heart) < 100f) continue;
                var ok = true;
                for (var k = 0; k < 12 && ok; k++) { var a = k * Mathf.PI / 6f; if (data.IslandAt(x + Mathf.Cos(a) * (StadiumR + 45f), z + Mathf.Sin(a) * (StadiumR + 45f)) != isl) ok = false; }
                foreach (var a in data.layout.artists) if (ok && a.sceneId == isl.sceneId && Vector2.Distance(p, new Vector2(a.x, a.z)) < a.r + StadiumR + 14f) ok = false;
                if (ok && hasPond && Vector2.Distance(p, pondC) < pondR + StadiumR + 20f) ok = false;
                if (!ok) continue;
                bestH = data.heights[i]; stadiumC = p; hasStadium = true;
            }
            if (hasStadium) ElectricKitLevel(data, index, stadiumC, StadiumR + 4f, StadiumR + 30f);
        }

        /// The Hall of the Legends on the highest hill, for the ones who were cut short: an open marble rotunda on
        /// three steps, a ring of white columns under a gilded dome with an open eye at the top, an eternal
        /// flame in a bronze bowl in the middle, and round it a ring of plinths, each holding an instrument left
        /// behind with a candle burning by it. A soft shaft of golden light rises through the dome (ClassicLife).
        /// A royal road with banners leads up to it.
        void Hall(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            if (!hasStadium) return;
            var c = ctx.OnGround(stadiumC.x, stadiumC.y);
            var up = Vector3.up; const float R = 12f; const int n = 12;
            var marble = WorldMaterials.Solid(new Color(.95f, .93f, .88f), .5f); var gilt = WorldMaterials.Solid(new Color(.95f, .76f, .3f), .85f);
            var bronze = WorldMaterials.Solid(new Color(.55f, .38f, .2f), .7f);
            // the road up, with banners
            Vector3 from = center; var bd = float.MaxValue;
            foreach (var (p, w) in walkPoints) { if (w >= 990) continue; var d = (p.x - c.x) * (p.x - c.x) + (p.z - c.z) * (p.z - c.z); if (d < bd) { bd = d; from = p; } }
            var face = new Vector3(from.x - c.x, 0, from.z - c.z).normalized; if (face.sqrMagnitude < .1f) face = Vector3.forward;
            var pts = new List<Vector3>(); var total = Vector3.Distance(new Vector3(from.x, 0, from.z), new Vector3(c.x, 0, c.z)) - R - 3.5f;
            for (var d = RoadW / 2f; d < total; d += 3f) { var q = from - face * d; pts.Add(ctx.OnGround(q.x, q.z)); }
            if (pts.Count > 1)
            {
                Strip(mk, pts, asphalt, RoadW, .06f);
                foreach (var p in pts) Remember(p, 990);
                var sd = Vector3.Cross(up, face);
                for (var k = 3; k < pts.Count; k += 5) foreach (var s in new[] { -1f, 1f }) Banner(ctx, mk, ctx.OnGround(pts[k].x + sd.x * s * (RoadW / 2f + 1f), pts[k].z + sd.z * s * (RoadW / 2f + 1f)), k / 5 % 2 == 0);
            }
            // three marble steps all round
            for (var t = 0; t < 3; t++) { mk.Prism(marble, c - up * 1f, up, R + 3.2f - t * .9f, 1.2f + t * .3f, 24); }
            solid.Prism(marble, c - up * 1f, up, R + 1.4f, 1.9f, 24);
            for (var t = 0; t < 2; t++) solid.Prism(marble, c - up * 1f, up, R + 3.2f - t * .9f, 1.2f + t * .3f, 24);
            var floor = c + up * .9f;
            mk.Prism(gilt, floor + up * .01f, up, 3.4f, .02f, 32);                         // a gold record inlaid in the floor
            mk.Prism(WorldMaterials.Solid(new Color(.15f, .12f, .1f), .8f), floor + up * .02f, up, 3f, .02f, 32);
            mk.Prism(gilt, floor + up * .03f, up, 1.1f, .02f, 20);
            // the columns, the ring of gilded beams and the dome
            for (var k = 0; k < n; k++)
            {
                var a = k * Mathf.PI * 2f / n; var d = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)); var b = floor + d * R;
                mk.Box(marble, b + up * .25f, new Vector3(1.4f, .5f, 1.4f), Quaternion.LookRotation(d));
                mk.Prism(marble, b + up * .5f, up, .5f, 8f, 12, .85f);
                mk.Box(gilt, b + up * 8.7f, new Vector3(1.3f, .4f, 1.3f), Quaternion.LookRotation(d));
                ArtistBits.PostCollider(ctx.Root, b, 9f, .55f);
            }
            for (var k = 0; k < n; k++)
            {
                float a0 = k * Mathf.PI * 2f / n, a1 = (k + 1) * Mathf.PI * 2f / n;
                var p0 = floor + new Vector3(Mathf.Cos(a0), 0, Mathf.Sin(a0)) * R + up * 9.4f; var p1 = floor + new Vector3(Mathf.Cos(a1), 0, Mathf.Sin(a1)) * R + up * 9.4f;
                mk.Box(gilt, (p0 + p1) / 2f, new Vector3(1.2f, 1f, Vector3.Distance(p0, p1) + .6f), Quaternion.LookRotation(p1 - p0));
            }
            var domeBase = floor + up * 9.9f;
            for (var ring = 0; ring < 6; ring++)
            {
                float t0 = ring / 6f, t1 = (ring + 1) / 6f; if (ring == 5) break;      // the top stays open: the oculus
                float r0 = R * Mathf.Cos(t0 * Mathf.PI / 2f), r1 = R * Mathf.Cos(t1 * Mathf.PI / 2f), y0 = R * .8f * Mathf.Sin(t0 * Mathf.PI / 2f), y1 = R * .8f * Mathf.Sin(t1 * Mathf.PI / 2f);
                for (var k = 0; k < 24; k++)
                {
                    float a0 = k * Mathf.PI * 2f / 24f, a1 = (k + 1) * Mathf.PI * 2f / 24f;
                    Vector3 P(float rr, float yy, float aa) => domeBase + new Vector3(Mathf.Cos(aa) * rr, yy, Mathf.Sin(aa) * rr);
                    var mid = (P(r0, y0, a0) + P(r1, y1, a1)) / 2f;
                    mk.Quad(gilt, P(r0, y0, a0), P(r0, y0, a1), P(r1, y1, a1), P(r1, y1, a0), domeBase + (mid - domeBase) * .5f);
                    mk.Quad(gilt, P(r0, y0, a1), P(r0, y0, a0), P(r1, y1, a0), P(r1, y1, a1), mid + (mid - domeBase) * 2f);
                }
            }
            var eye = domeBase + up * R * .8f * Mathf.Sin(5f / 6f * Mathf.PI / 2f);
            mk.Prism(gilt, eye - up * .1f, up, R * Mathf.Cos(5f / 6f * Mathf.PI / 2f) + .3f, .5f, 24);
            // the eternal flame in a bronze bowl on a stem
            mk.Prism(bronze, floor, up, .35f, 1f, 10);
            mk.Prism(bronze, floor + up * 1f, up, .4f, .5f, 14, 2.6f);
            solid.Prism(bronze, floor, up, .9f, 1.5f, 10);
            life.hallFlame = floor + up * 1.55f;
            var fl = ArtistBits.PointLight(ctx.Root, floor + up * 2.4f, new Color(1f, .75f, .4f), 16f, 2f); fl.gameObject.AddComponent<FireFlicker>().baseIntensity = 2f;
            // the plinths, each with what was left behind and a candle
            for (var k = 0; k < 8; k++)
            {
                var a = (k + .5f) * Mathf.PI * 2f / 8f; var d = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                if (Vector3.Dot(d, face) > .85f) continue;                                  // leave the way in clear
                var pl = floor + d * 7.5f; var rot = Quaternion.LookRotation(-d);
                mk.Box(marble, pl + up * .55f, new Vector3(1.4f, 1.1f, 1.4f), rot); mk.Box(gilt, pl + up * 1.12f, new Vector3(1.5f, .06f, 1.5f), rot);
                solid.Box(marble, pl + up * .55f, new Vector3(1.4f, 1.1f, 1.4f), rot);
                var top = pl + up * 1.15f;
                switch (k % 4)
                {
                    case 0: Guitar(mk, ctx.Rng, top, -d, Paint[k % Paint.Length], 1.1f, 6f); break;
                    case 1: mk.Prism(metal, top, up, .04f, 1.2f, 6); mk.Blob(metal, top + up * 1.35f, new Vector3(.12f, .18f, .12f), ctx.Rng, 0f); break;     // a microphone
                    case 2: mk.Prism(white, top + up * .02f, up, .4f, .5f, 14); mk.Prism(WorldMaterials.Solid(new Color(.85f, .7f, .3f), .9f), top + up * .9f + d * .3f, up, .35f, .02f, 14); mk.Box(metal, top + up * .45f + d * .3f, new Vector3(.03f, .9f, .03f), rot); break; // a drum and cymbal
                    default: mk.Box(white, top + up * .05f, new Vector3(1f, .1f, .5f), rot); for (var key = 0; key < 7; key++) mk.Box(dark, top + up * .11f + Vector3.Cross(up, d) * ((key - 3f) * .13f) + d * .08f, new Vector3(.05f, .02f, .25f), rot); break;  // a keyboard
                }
                var candle = pl + up * 1.15f + Vector3.Cross(up, d) * .5f + d * .4f;
                mk.Prism(white, candle, up, .06f, .3f, 8);
                mk.Blob(GlowM(new Color(1f, .75f, .35f), 3f), candle + up * .38f, new Vector3(.04f, .08f, .04f), ctx.Rng, 0f);
                halos.Add(candle + up * .4f, .7f, new Color(1f, .75f, .35f), .9f);
                life.candles.Add(candle + up * .38f);
            }
            ArtistBits.Label(ctx.Root, "THE HALL OF THE LEGENDS", floor + up * 9.4f + face * (R + .7f), face, .4f, new Color(.35f, .25f, .1f), TextAnchor.MiddleCenter);
            ArtistBits.Label(ctx.Root, "for the ones who left too soon", floor + up * 8.6f + face * (R + .7f), face, .2f, new Color(.35f, .25f, .1f), TextAnchor.MiddleCenter);
            ArtistBits.PointLight(ctx.Root, floor + up * 6f, new Color(1f, .85f, .6f), 22f, 1.2f, true);
            halos.Add(eye + up * 1f, 4f, new Color(1f, .85f, .5f), .5f);
            life.hall = c; life.hallEye = eye;
            noGrass.Add(new Vector3(c.x, R + 5f, c.z));
        }

        /// A royal banner on a tall pole: crimson or deep blue cloth with a gold band, a gold finial on top.
        void Banner(LandContext ctx, MeshKit mk, Vector3 g, bool crimson)
        {
            var up = Vector3.up; var gilt = WorldMaterials.Solid(new Color(.95f, .76f, .3f), .85f);
            var cloth = WorldMaterials.Solid(crimson ? new Color(.7f, .08f, .12f) : new Color(.12f, .2f, .55f));
            var face = new Vector3(center.x - g.x, 0, center.z - g.z).normalized; var rot = Quaternion.LookRotation(face);
            mk.Prism(dark, g - up * .3f, up, .07f, 5.4f, 6);
            mk.Blob(gilt, g + up * 5.2f, Vector3.one * .14f, ctx.Rng, 0f);
            mk.Box(gilt, g + up * 4.85f + Vector3.Cross(up, face) * .45f, new Vector3(1.1f, .06f, .06f), rot);
            var c = g + up * 3.7f + Vector3.Cross(up, face) * .45f;
            mk.Box(cloth, c, new Vector3(.9f, 2.2f, .03f), rot);
            mk.Box(gilt, c - up * .7f, new Vector3(.92f, .14f, .05f), rot);
            mk.Tri(cloth, c - up * 1.1f - Vector3.Cross(up, face) * .45f, c - up * 1.1f + Vector3.Cross(up, face) * .45f, c - up * 1.55f, face);
            mk.Tri(cloth, c - up * 1.1f + Vector3.Cross(up, face) * .45f, c - up * 1.1f - Vector3.Cross(up, face) * .45f, c - up * 1.55f, -face);
            ArtistBits.PostCollider(ctx.Root, g, 5f, .1f);
        }
    }
}
