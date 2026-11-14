using System.Collections.Generic;
using UnityEngine;

namespace EverythingWorld.World
{
    /// The Jam Band Commonwealth (WORLD_GUIDE.md): rolling woods at blue hour, the sky deep blue with a last band of
    /// orange, warm festival lights everywhere. Unhurried: long trails loop through the trees, lit by lanterns on
    /// strings. At the heart, Phish's forest amphitheatre: a big shell stage with a light rig of moving lights that
    /// paints the trees, trampolines on the stage, a hot-air balloon tethered above, a donut garden and a giant hot
    /// dog beside it. Near where you arrive, the Lot: a row of vendor tents, tapestries, buses and string lights.
    /// Out in the woods, through a glowing Rhombus, Gamehendge; along the trails, things from every Phish album.
    /// Files: JamKit (ground, layout, trails, the amphitheatre, the Lot, the woods), JamGamehendge, JamArtists,
    /// JamLife.
    public partial class JamKit : LandKit
    {
        public override string Land => "The Jam Band Commonwealth";
        public override Vector3 ArtistFront(IslandWorldData data, IslandWorldData.Artist a) => facing.TryGetValue(a.name, out var f) ? f : base.ArtistFront(data, a);

        public const string Hood = "4y4qe0vmDdRv87O1jHN15l", DividedSky = "7FcShHdjNAE2ENmcZMzW6m", DividedSky2 = "6v9oZPFvhZH5lUFOWZw34a", Antelope = "7zJlSL4rNqr8dzI55XSp2t", Free = "4WQHQEwcGFiNPoEwo6U6bQ";
        public override (string song, string label, string artist)[] SpecialSongs => new[]
        {
            (DividedSky, "The Divided Sky", "@Gamehendge"), (Hood, "Harry Hood", "@the lawn"), (Antelope, "Run Like An Antelope", "@the meadow"), (JamLife.Reba, "Reba", "@the lawn"),
            (JamLife.Coil, "The Squirming Coil", "@the lawn"), (JamLife.Melt, "Split Open and Melt", "@the lawn"), (JamLife.Ice, "It's Ice", "@the lawn"), (JamLife.Velvet, "Wading in the Velvet Sea", "@the lawn"),
            (JamLife.Traffic, "Slave to the Traffic Light", "@the lawn"),
        };

        static readonly Color Warm = new Color(1f, .78f, .45f), Ink = new Color(1f, .96f, .88f);
        static readonly Color[] Dye = { new Color(1f, .3f, .35f), new Color(1f, .62f, .2f), new Color(1f, .9f, .25f), new Color(.3f, .85f, .45f), new Color(.25f, .6f, 1f), new Color(.7f, .4f, 1f) };
        const float TrailW = 3f;

        Material dirt, wood, dark, metal, leaf, pineM, bark, white, canvas;
        GlowHalos halos; Vector3 center; JamLife life; MeshKit soft;
        readonly Dictionary<(int, int), List<Vector3>> pathCells = new Dictionary<(int, int), List<Vector3>>();
        readonly List<(Vector3 p, int w)> walkPoints = new List<(Vector3, int)>();
        readonly Dictionary<string, Vector3> facing = new Dictionary<string, Vector3>();
        readonly List<Vector3> noGrass = new List<Vector3>();

        /// A plain box collider on its own object (a sign board, a stall's back cloth: things the merged walls don't cover).
        static void BoxCol(Transform parent, Vector3 center, Vector3 size, Quaternion rot)
        {
            var go = new GameObject("Box collider"); go.transform.SetParent(parent, false); go.transform.SetPositionAndRotation(center, rot);
            go.AddComponent<BoxCollider>().size = size; go.isStatic = true;
        }

        void MakeMaterials()
        {
            dirt = WorldMaterials.Solid(new Color(.45f, .36f, .26f)); wood = WorldMaterials.Solid(new Color(.5f, .37f, .25f));
            dark = WorldMaterials.Solid(new Color(.08f, .08f, .1f)); metal = WorldMaterials.Solid(new Color(.7f, .72f, .76f), .8f);
            leaf = WorldMaterials.Solid(new Color(.2f, .4f, .24f)); pineM = WorldMaterials.Solid(new Color(.12f, .3f, .22f));
            bark = WorldMaterials.Solid(new Color(.35f, .27f, .2f)); white = WorldMaterials.Solid(new Color(.95f, .94f, .9f));
            canvas = WorldMaterials.Solid(new Color(.92f, .88f, .78f));
        }

        Material GlowM(Color c, float s = 2f) => WorldMaterials.Glow(c, s * .65f);
        static bool HasStation(IslandWorldData.Artist a) => a.station || a.stationId != 0;
        static float Foot(IslandWorldData.Artist a) => a.name == "Phish" ? 34f : HasStation(a) ? 15f : 8f;

        public override bool Ground(float height, float slope, float patch, out Color color)
        {
            if (slope > 8f) color = Color.Lerp(new Color(.38f, .33f, .28f), new Color(.45f, .4f, .33f), patch);
            else if (height < 2.5f) color = new Color(.62f, .56f, .42f);
            else color = Color.Lerp(new Color(.22f, .42f, .26f), new Color(.32f, .48f, .28f), Mathf.SmoothStep(.2f, .9f, patch));
            color.a = 1f;
            return true;
        }

        public override void ShapeGround(IslandWorldData data, int index)
        {
            var isl = data.layout.islands[index]; var heart = new Vector2(isl.labelX, isl.labelZ);
            ArtistPlacement.Spread(data, index, 75f, heart, 50f, 50f, PlaceAvoid(heart), null, Land, 300f);
            foreach (var a in data.layout.artists)
            {
                if (a.sceneId != isl.sceneId) continue;
                a.r = Mathf.Max(a.r, Foot(a) + 8f);
                Level(data, index, new Vector2(a.x, a.z), Foot(a) + 1f, Foot(a) + 16f);
            }
            Level(data, index, heart, 16f, 34f);
            foreach (var (at, flat, blend) in new[] { (CampAt, 28f, 42f), (SwimAt, 24f, 36f), (WheelAt, 11f, 20f), (DomeAt, 8f, 15f), (GalleryAt, 8f, 14f) }) Level(data, index, at, flat, blend);
            // the festival roads lie level from side to side (on a hillside a road's flat strip stood half a metre
            // proud of the ground at one edge, a ledge where the trails and spokes meet it)
            var roads = new List<(Vector2[] way, bool closed)> { (LoopWay, true), (SouthWay, false), (NorthWay, false) };
            foreach (var s in Spokes) { var to = LoopWay[s]; var dir = (to - heart).normalized; var sd = new Vector2(-dir.y, dir.x); roads.Add((new[] { heart + dir * 14f, Vector2.Lerp(heart, to, .5f) + sd * 12f, to }, false)); }
            LevelAcross(data, index, roads, RoadW / 2f + .8f, RoadW / 2f + 5f);
        }

        /// Sets the ground across each road to the road's own height, blending back to the hillside beside it.
        static void LevelAcross(IslandWorldData data, int index, List<(Vector2[] way, bool closed)> roads, float flat, float blend)
        {
            var line = new List<Vector3>();   // x, height, z along the roads, a point a metre
            foreach (var (way, closed) in roads)
            {
                var n = way.Length; var segs = closed ? n : n - 1;
                for (var i = 0; i < segs; i++)
                {
                    Vector2 p0 = way[closed ? (i - 1 + n) % n : Mathf.Max(i - 1, 0)], p1 = way[i], p2 = way[(i + 1) % n], p3 = way[closed ? (i + 2) % n : Mathf.Min(i + 2, n - 1)];
                    var k = Mathf.Max(2, Mathf.CeilToInt(Vector2.Distance(p1, p2)));
                    for (var j = 0; j <= k; j++)
                    {
                        var t = j / (float)k; var t2 = t * t; var t3 = t2 * t;
                        var q = .5f * (2f * p1 + (-p0 + p2) * t + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 + (-p0 + 3f * p1 - 3f * p2 + p3) * t3);
                        line.Add(new Vector3(q.x, data.HeightAt(q.x, q.y), q.y));
                    }
                }
            }
            var best = new Dictionary<int, (float d, float h)>();
            foreach (var p in line)
            {
                int c0 = Mathf.FloorToInt((p.x - blend) / data.Step), c1 = Mathf.CeilToInt((p.x + blend) / data.Step);
                int r0 = Mathf.FloorToInt((data.layout.depth - p.z - blend) / data.Step), r1 = Mathf.CeilToInt((data.layout.depth - p.z + blend) / data.Step);
                for (var r = Mathf.Max(0, r0); r <= r1 && r < data.Nz; r++)
                for (var col = Mathf.Max(0, c0); col <= c1 && col < data.Nx; col++)
                {
                    var i = r * data.Nx + col; if (data.islandIds[i] != index) continue;
                    float dx = col * data.Step - p.x, dz = data.layout.depth - r * data.Step - p.z, d = Mathf.Sqrt(dx * dx + dz * dz);
                    if (d < blend && (!best.TryGetValue(i, out var b) || d < b.d)) best[i] = (d, p.y);
                }
            }
            foreach (var kv in best)
            {
                var (d, h) = kv.Value;
                data.heights[kv.Key] = d < flat ? h : Mathf.Lerp(h, data.heights[kv.Key], Mathf.SmoothStep(0, 1, (d - flat) / (blend - flat)));
            }
        }

        static void Level(IslandWorldData data, int index, Vector2 c, float flat, float blend)
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
            life = new GameObject("Jam Band life").AddComponent<JamLife>();
            life.transform.SetParent(ctx.Root, false);
            life.center = center;
            trailMats.Clear(); for (var k = 0; k < 6; k++) { var m = new Material(GlowM(Warm, 2.6f)) { name = "Trail lantern " + k }; trailMats.Add(m); life.trailLights.Add(m); }
            var mk = new MeshKit(); var solid = new MeshKit();
            Arrival(ctx, mk, solid);
            FestivalRoads(ctx, mk, solid);
            Trails(ctx, mk, solid);
            Lot(ctx, mk, solid);
            Gamehendge(ctx, mk, solid);
            AlbumTrail(ctx, mk, solid);
            Meadow(ctx, mk);
            Places(ctx, mk, solid);
            Woods(ctx, mk, solid);
            mk.Build("Commonwealth", ctx.Root, false);
            soft.Build("Soft commonwealth", ctx.Root, false);
            solid.BuildCollider("Commonwealth walls", ctx.Root);
        }

        public override void Finish(LandContext ctx) { GroundCover(ctx); halos.Build("Commonwealth glow", ctx.Root); }

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
            ctx.IsLand(x, z, 4f) && !InFootprint(ctx, x, z, pad) && !NearPath(x, z, pad + 2f) && !InNoGrass(x, z, pad)
            && Vector2.Distance(new Vector2(x, z), new Vector2(center.x, center.z)) > 24f + pad;

        /// Find an open spot of the given radius, preferring ones near a trail and between the given distances
        /// from where you arrive.
        bool FindSpot(LandContext ctx, float rad, float nearMin, float nearMax, out Vector3 at)
        {
            at = default; var best = float.MaxValue;
            for (var t = 0; t < 500; t++)
            {
                float x = ctx.Range(ctx.MinX, ctx.MaxX), z = ctx.Range(ctx.MinZ, ctx.MaxZ);
                var dh = Vector2.Distance(new Vector2(x, z), new Vector2(center.x, center.z)); if (dh < nearMin || dh > nearMax) continue;
                if (!ctx.IsLand(x, z, rad + 10f) || !Open(ctx, x, z, rad)) continue;
                var score = NearPath(x, z, rad + 14f) ? 0f : 100f; score += Mathf.Abs(dh - (nearMin + nearMax) / 2f) * .1f;
                if (score < best) { best = score; at = ctx.OnGround(x, z); }
            }
            return best < float.MaxValue;
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

        /// A string of warm lantern bulbs sagging between two points (trail strings use the show-driven materials).
        void StringLights(MeshKit mk, Vector3 a, Vector3 b, Color c, int n = 10, bool trail = false)
        {
            for (var k = 1; k < n; k++)
            {
                var t = k / (float)n; var p = Vector3.Lerp(a, b, t) - Vector3.up * Mathf.Sin(t * Mathf.PI) * .7f;
                var m = trail && trailMats.Count > 0 ? trailMats[trailCount++ % trailMats.Count] : GlowM(k % 3 == 0 ? Dye[k % Dye.Length] : c, 2.6f);
                mk.Blob(m, p, Vector3.one * (trail ? .13f : .1f), new System.Random(k), 0f);
                if (k % 3 == 0) halos.Add(p, trail ? .8f : .5f, c, .6f);
            }
        }
        readonly List<Material> trailMats = new List<Material>(); int trailCount;

        void Tree(LandContext ctx, MeshKit mk, Vector3 at, float s)
        {
            var up = Vector3.up; var h = ctx.Range(6f, 9f) * s;
            mk.Prism(bark, at - up * .3f, up, .35f * s, h, 7, .7f);
            for (var k = 0; k < 4; k++) soft.Blob(leaf, at + up * (h * (.75f + k * .1f)) + new Vector3(ctx.Range(-1.6f, 1.6f), 0, ctx.Range(-1.6f, 1.6f)) * s, new Vector3(2.6f, 1.9f, 2.6f) * s, ctx.Rng, .3f);
            ArtistBits.PostCollider(ctx.Root, at, h * .6f, .4f * s);
            noGrass.Add(new Vector3(at.x, 1.2f * s, at.z));
        }

        void Pine(LandContext ctx, MeshKit mk, Vector3 at, float s)
        {
            var up = Vector3.up; var h = 9f * s;
            mk.Prism(bark, at - up * .3f, up, .3f * s, h * .3f, 6);
            for (var k = 0; k < 3; k++) soft.Cone(pineM, at + up * (h * (.22f + k * .22f)), (2.6f - k * .7f) * s, h * .42f, 7, ctx.Range(0, 60f));
            ArtistBits.PostCollider(ctx.Root, at, h * .5f, .35f * s);
        }

        // ---------------------------------------------------------------- where you arrive
        /// A wooden festival gate strung with lights and tapestries, a ticket hut, a big wooden sign carved with
        /// the land's name, and the main trail leading off into the trees.
        void Arrival(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var c = ctx.OnGround(center.x, center.z);
            IslandWorldData.Artist phish = null; foreach (var a in ctx.Artists) if (a.name == "Phish") phish = a;
            var face = phish != null ? new Vector3(phish.x - c.x, 0, phish.z - c.z).normalized : Vector3.forward; if (face.sqrMagnitude < .1f) face = Vector3.forward;
            var rot = Quaternion.LookRotation(face); var side = Vector3.Cross(up, face);
            var gate = c + face * 9f;
            foreach (var s in new[] { -1f, 1f }) { var p = gate + side * s * 5f; mk.Prism(wood, p - up * .3f, up, .35f, 8f, 8); ArtistBits.PostCollider(ctx.Root, p, 8f, .4f); }
            mk.Box(wood, gate + up * 7.4f, new Vector3(11.5f, .7f, .7f), rot);
            mk.Box(wood, gate + up * 6.2f, new Vector3(8f, 1.4f, .2f), rot);
            ArtistBits.Label(ctx.Root, "THE JAM BAND COMMONWEALTH", gate + up * 6.2f - face * .12f, -face, .38f, Ink, TextAnchor.MiddleCenter);
            ArtistBits.Label(ctx.Root, "THE JAM BAND COMMONWEALTH", gate + up * 6.2f + face * .12f, face, .38f, Ink, TextAnchor.MiddleCenter);   // lettered on the back too, so it isn't blank walking out
            StringLights(mk, gate + up * 7f - side * 5f, gate + up * 7f + side * 5f, Warm, 14);
            for (var k = 0; k < 3; k++) { var tp = gate + side * ((k - 1) * 3.2f) + up * 4.6f; for (var b = 0; b < 5; b++) mk.Box(WorldMaterials.Solid(Dye[(b + k * 2) % Dye.Length], .3f), tp - up * b * .4f, new Vector3(1.6f, .4f, .04f + (b % 2) * .01f), rot); }
            var hut = c + face * 4f + side * 9f;
            mk.Box(wood, hut + up * 1.4f, new Vector3(3f, 2.8f, 2.6f), rot); solid.Box(wood, hut + up * 1.4f, new Vector3(3f, 2.8f, 2.6f), rot);
            mk.GableRoof(WorldMaterials.Solid(new Color(.45f, .3f, .22f)), hut + up * 2.8f, new Vector3(3.6f, 0, 3.4f), 1f, rot);
            mk.Box(GlowM(Warm, 1.6f), hut + up * 1.7f - face * 1.31f, new Vector3(1.8f, .8f, .02f), rot);
            ArtistBits.PointLight(ctx.Root, gate + up * 5f - face * 2f, Warm, 20f, 2f);
            life.gate = gate;
            noGrass.Add(new Vector3(c.x, 14f, c.z));
            Remember(c, 0);
        }

        // ---------------------------------------------------------------- trails
        /// Dirt trails that wander through the trees to every artist, lantern strings on posts along them.
        void Trails(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var todo = new List<IslandWorldData.Artist>(ctx.Artists); var walk = 1; var posts = new List<Vector3>();
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
                for (var d = (new Vector2(join.x - center.x, join.z - center.z)).sqrMagnitude < 4f ? 12f : RoadW / 2f + .1f; ; d += 2.5f)
                {
                    var dd = Mathf.Min(d, end);
                    var q = from + dir * dd + side * Mathf.Sin(dd / 22f) * Mathf.Min(6f, end * .06f);
                    if (!ctx.IsLand(q.x, q.z, 0f)) break;
                    pts.Add(ctx.OnGround(q.x, q.z)); if (dd >= end) break;
                }
                if (pts.Count < 2) continue;
                Strip(mk, pts, dirt, TrailW, .04f);
                foreach (var p in pts) Remember(p, walk);
                Vector3? prev = null;
                for (var i = 3; i < pts.Count; i += 6)
                {
                    var lp = pts[i] + side * ((i / 6) % 2 == 0 ? 1f : -1f) * (TrailW / 2f + .6f); var g = ctx.OnGround(lp.x, lp.z);
                    mk.Prism(wood, g - Vector3.up * .3f, Vector3.up, .1f, 3.6f, 6); ArtistBits.PostCollider(ctx.Root, g, 3.3f, .12f);
                    if (prev.HasValue && Vector3.Distance(prev.Value, g) < 22f) StringLights(mk, prev.Value + Vector3.up * 3.2f, g + Vector3.up * 3.2f, Warm, 8, true);
                    mk.Box(trailMats[(i / 6) % trailMats.Count], g + Vector3.up * 3.45f, new Vector3(.3f, .38f, .3f), Quaternion.identity); halos.Add(g + Vector3.up * 3.45f, 1.3f, Warm, .7f);
                    prev = g;
                    if ((i / 6) % 2 == 0) ArtistBits.PointLight(ctx.Root, g + Vector3.up * 3f, Warm, 12f, 1.2f);
                }
                walk++;
            }
        }

        // ---------------------------------------------------------------- the Lot
        /// A parking-lot vendor row: canopy tents in two rows with tapestries hung behind, string lights between,
        /// a grilled-cheese stand with a griddle glow, glowsticks for sale, painted buses parked at the ends, and a
        /// hand-lettered sign over the way in.
        void Lot(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            if (!FindSpot(ctx, 22f, 40f, 140f, out var c)) return;
            var up = Vector3.up;
            // the Lot is long (buses 28 m out each way): slide it until none of it, buses and sign included, sits on a road
            bool Clear(Vector3 at)
            {
                var th = new Vector3(center.x - at.x, 0, center.z - at.z).normalized; var dr = Vector3.Cross(up, th);
                for (var a = -29f; a <= 29f; a += 2f) for (var b = -8f; b <= 11f; b += 2f)
                {
                    var q = at + dr * a + th * b;
                    if (!ctx.IsLand(q.x, q.z, 2f) || NearPath(q.x, q.z, TrailW / 2f + .5f) || InFootprint(ctx, q.x, q.z, 2f)) return false;
                }
                return true;
            }
            if (!Clear(c))
                for (var r = 2f; r <= 40f; r += 2f)
                {
                    var moved = false;
                    for (var a = 0f; a < 360f; a += 15f) { var q = c + Quaternion.Euler(0, a, 0) * Vector3.forward * r; if (Clear(q)) { c = ctx.OnGround(q.x, q.z); moved = true; break; } }
                    if (moved) break;
                }
            var toHeart = new Vector3(center.x - c.x, 0, center.z - c.z).normalized; var dir = Vector3.Cross(up, toHeart); var side = toHeart;
            var rot = Quaternion.LookRotation(dir);
            var tarmac = WorldMaterials.Solid(new Color(.28f, .27f, .3f), .3f);   // laid on the ground in 2 m squares, so it follows the slope
            Vector3 T(float a, float b) { var q = c + dir * a + side * b; return ctx.OnGround(q.x, q.z) + up * .04f; }
            for (var a = -22f; a < 22f; a += 2f) for (var b = -8f; b < 8f; b += 2f) mk.Quad(tarmac, T(a, b), T(a + 2f, b), T(a + 2f, b + 2f), T(a, b + 2f), (T(a, b) + T(a + 2f, b + 2f)) / 2f - up);
            for (var k = 0; k < 7; k++) foreach (var s in new[] { -1f, 1f })
            {
                var p = ctx.OnGround(c.x + dir.x * (k - 3f) * 5.6f + side.x * s * 5f, c.z + dir.z * (k - 3f) * 5.6f + side.z * s * 5f); var col = Dye[(k * 2 + (s > 0 ? 1 : 0)) % Dye.Length];
                var trot = Quaternion.LookRotation(-side * s);
                foreach (var a in new[] { -1f, 1f }) foreach (var b in new[] { -1f, 1f }) { var pp = p + dir * a * 1.9f + side * s * b * 1.9f; var pg = ctx.Ground(pp.x, pp.z); var ph = p.y + 2.5f - pg; pp.y = pg; mk.Prism(metal, pp - up * .2f, up, .04f, ph + .2f, 6); ArtistBits.PostCollider(ctx.Root, pp, ph, .06f); }   // each pole on its own ground, up to the canopy
                mk.Box(WorldMaterials.Solid(col, .3f), p + up * 2.5f, new Vector3(4f, .12f, 4f), trot);
                mk.Cone(WorldMaterials.Solid(col, .3f), p + up * 2.55f, 2.8f, .9f, 4, 45f);
                for (var b = 0; b < 6; b++) mk.Box(WorldMaterials.Solid(Dye[(b + k) % Dye.Length], .3f), p + side * s * 2f + up * (.5f + b * .32f), new Vector3(3.6f, .32f, .03f + (b % 2) * .01f), trot);
                solid.Box(wood, p + side * s * 2f + up * 1.2f, new Vector3(3.6f, 1.9f, .12f), trot);   // the striped back cloth
                mk.Box(wood, p + up * .45f - side * s * .8f, new Vector3(3f, .9f, .8f), trot); solid.Box(wood, p + up * .45f - side * s * .8f, new Vector3(3f, .9f, .8f), trot);
                if (k == 2 && s > 0) { mk.Box(GlowM(new Color(1f, .5f, .15f), 2f), p + up * .95f - side * s * .8f, new Vector3(1.4f, .05f, .6f), trot); ArtistBits.Label(ctx.Root, "GRILLED CHEESE $5", p + up * 2.1f - side * s * 2.05f, -side * s, .2f, Ink, TextAnchor.MiddleCenter); }
                if (k == 4 && s < 0) for (var g = 0; g < 12; g++) mk.Box(GlowM(Dye[g % Dye.Length], 2.6f), p + up * (1f + (g % 3) * .1f) - side * s * .8f + dir * ((g - 6f) * .2f), new Vector3(.03f, .3f, .03f), Quaternion.Euler(0, 0, 20f));
                ArtistBits.PointLight(ctx.Root, p + up * 2.2f, Warm, 8f, 1.1f);
            }
            for (var k = 0; k < 6; k++) StringLights(mk, c + dir * ((k - 3f) * 5.6f) + side * -5f + up * 3.2f, c + dir * ((k - 3f) * 5.6f) + side * 5f + up * 3.2f, Warm, 9);
            // buses at the ends
            foreach (var e in new[] { -1f, 1f })
            {
                var bp = ctx.OnGround(c.x + dir.x * e * 25f, c.z + dir.z * e * 25f); var brot = Quaternion.LookRotation(side);
                for (var b = 0; b < 5; b++) mk.Box(WorldMaterials.Solid(Dye[(b + (e > 0 ? 2 : 0)) % Dye.Length], .4f), bp + up * (.9f + b * .38f), new Vector3(2.4f + (b % 2) * .03f, .39f, 6f + (b % 2) * .03f), brot);   // alternate stripes step out 15 mm, more than Box's growth can take back
                mk.Box(white, bp + up * 2.9f, new Vector3(2.4f, .5f, 6f), brot);
                mk.Box(WorldMaterials.Solid(new Color(.15f, .2f, .3f), .9f), bp + up * 2.3f, new Vector3(2.48f, .7f, 5f), brot);
                foreach (var s in new[] { -1f, 1f }) foreach (var z in new[] { -2f, 2f }) mk.Prism(dark, bp + dir * s * 1.1f + side * z + up * .45f, dir * s, .45f, .25f, 12);
                solid.Box(white, bp + up * 1.6f, new Vector3(2.5f, 3.2f, 6.1f), brot);
            }
            // the sign over the way in
            var sp = c + side * 9.5f;
            foreach (var s in new[] { -1f, 1f }) { mk.Prism(wood, sp + dir * s * 3f - up * .3f, up, .12f, 4.6f, 6); ArtistBits.PostCollider(ctx.Root, sp + dir * s * 3f, 4.3f, .15f); }
            mk.Box(wood, sp + up * 3.9f, new Vector3(6.4f, 1f, .15f), rot * Quaternion.Euler(0, 90f, 0));
            ArtistBits.Label(ctx.Root, "THE LOT", sp + up * 3.9f + side * .1f, side, .4f, new Color(1f, .85f, .5f), TextAnchor.MiddleCenter);
            noGrass.Add(new Vector3(c.x, 28f, c.z));
            life.lot = c;
            Footpath(ctx, mk, c + side * 10f);
        }

        /// A short trail from the nearest trail to a place.
        void Footpath(LandContext ctx, MeshKit mk, Vector3 to)
        {
            Vector3 from = center; var bd = float.MaxValue;
            foreach (var (p, w) in walkPoints) { if (w >= 900) continue; var d = (p.x - to.x) * (p.x - to.x) + (p.z - to.z) * (p.z - to.z); if (d < bd) { bd = d; from = p; } }
            var dir = new Vector3(to.x - from.x, 0, to.z - from.z); var total = dir.magnitude; if (total < 4f) return; dir /= total;
            var pts = new List<Vector3>();
            for (var d = TrailW / 2f; ; d += 2.5f) { var dd = Mathf.Min(d, total); var q = from + dir * dd; pts.Add(ctx.OnGround(q.x, q.z)); if (dd >= total) break; }
            if (pts.Count < 2) return;
            Strip(mk, pts, dirt, TrailW * .8f, .04f);
            foreach (var p in pts) Remember(p, 900);
        }

        // ---------------------------------------------------------------- the meadow
        /// A long open meadow where the antelope run ("Run Like An Antelope"): find it and mark it.
        void Meadow(LandContext ctx, MeshKit mk)
        {
            if (!FindSpot(ctx, 30f, 120f, 280f, out var c)) return;
            noGrass.Add(new Vector3(c.x, 0f, c.z));
            life.meadow = c;
            var mark = new GameObject("Shot: the meadow").transform; mark.SetParent(ctx.Root, false);
            var look = new Vector3(c.x - center.x, 0, c.z - center.z).normalized; var feet = c - look * 30f; feet.y = ctx.Ground(feet.x, feet.z);
            mark.SetPositionAndRotation(feet, Quaternion.LookRotation(look));
            // keep it open: the woods leave it alone
            meadowC = c;
        }
        Vector3 meadowC;

        // ---------------------------------------------------------------- the woods
        void Woods(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            int trees = 0, pines = 0, camps = 0, hammocks = 0;
            const float cell = 22f;
            for (var x = ctx.MinX; x < ctx.MaxX; x += cell)
            for (var z = ctx.MinZ; z < ctx.MaxZ; z += cell)
            {
                float px = x + ctx.Range(0f, cell), pz = z + ctx.Range(0f, cell);
                if (!Open(ctx, px, pz, 3f)) continue;
                if (meadowC != Vector3.zero && Vector2.Distance(new Vector2(px, pz), new Vector2(meadowC.x, meadowC.z)) < 34f) continue;
                var p = ctx.OnGround(px, pz); var roll = ctx.Range(0f, 1f);
                if (roll < .05f && camps < 0 && NearPath(px, pz, 16f)) { Campsite(ctx, mk, solid, p); camps++; }
                else if (roll < .08f && hammocks < 0) { Hammock(ctx, mk, p); hammocks++; }
                else if (roll < .6f) { var s = ctx.Range(.9f, 1.4f); Tree(ctx, mk, p, s); trees++; if (NearPath(px, pz, 14f) && ctx.Chance(.5f)) { var lp = p + Vector3.up * 3.4f * s + new Vector3(ctx.Range(-.8f, .8f), 0, ctx.Range(-.8f, .8f)); mk.Box(GlowM(Dye[ctx.Rng.Next(Dye.Length)], 2.4f), lp, new Vector3(.3f, .45f, .3f), Quaternion.Euler(0, ctx.Range(0, 90f), 0)); mk.Box(dark, lp + Vector3.up * .4f, new Vector3(.02f, .4f, .02f), Quaternion.identity); halos.Add(lp, 1.4f, Warm, .7f); } }
                else if (roll < .9f) { Pine(ctx, mk, p, ctx.Range(.9f, 1.4f)); pines++; }
                else continue;
            }
            Debug.Log($"The Jam Band Commonwealth: {trees} trees, {pines} pines, {camps} campsites, {hammocks} hammocks");
        }

        void Campsite(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 at)
        {
            var up = Vector3.up;
            for (var k = 0; k < 2; k++)
            {
                var tp = at + Quaternion.Euler(0, k * 140f + ctx.Range(0, 40f), 0) * Vector3.forward * 4f; var tr = Quaternion.Euler(0, ctx.Range(0, 360f), 0);
                var col = WorldMaterials.Solid(Dye[ctx.Rng.Next(Dye.Length)], .3f); var tc = tp + up * .8f; tp.y = ctx.Ground(tp.x, tp.z);
                Vector3 T(float x, float y, float z) => tp + tr * new Vector3(x, y, z);
                mk.Quad(col, T(-1.2f, 0, -1.4f), T(0, 1.6f, -1.4f), T(0, 1.6f, 1.4f), T(-1.2f, 0, 1.4f), tc); mk.Quad(col, T(1.2f, 0, 1.4f), T(0, 1.6f, 1.4f), T(0, 1.6f, -1.4f), T(1.2f, 0, -1.4f), tc);
                mk.Tri(col, T(-1.2f, 0, -1.4f), T(1.2f, 0, -1.4f), T(0, 1.6f, -1.4f), tc); mk.Tri(dark, T(-1.2f, 0, 1.4f), T(0, 1.6f, 1.4f), T(1.2f, 0, 1.4f), tc);
                mk.Box(GlowM(Warm, 1.2f), T(0, .5f, 1.38f), new Vector3(.5f, .6f, .02f), tr);
                solid.Box(col, tp + up * .8f, new Vector3(2.4f, 1.6f, 2.8f), tr);
            }
            for (var k = 0; k < 8; k++) mk.Blob(WorldMaterials.Solid(new Color(.45f, .43f, .4f)), at + Quaternion.Euler(0, k * 45f, 0) * Vector3.forward * .9f + up * .15f, new Vector3(.25f, .18f, .22f), ctx.Rng, .3f);
            var fl = ArtistBits.PointLight(ctx.Root, at + up * 1f, new Color(1f, .55f, .2f), 9f, 1.6f); fl.gameObject.AddComponent<FireFlicker>().baseIntensity = 1.6f;
            ArtistBits.PostCollider(ctx.Root, at, .5f, .9f);
            life.fires.Add(at + up * .25f);
            noGrass.Add(new Vector3(at.x, 6f, at.z));
        }

        void Hammock(LandContext ctx, MeshKit mk, Vector3 at)
        {
            var d = Quaternion.Euler(0, ctx.Range(0, 180f), 0) * Vector3.forward; var up = Vector3.up;
            var a = ctx.OnGround(at.x - d.x * 3.5f, at.z - d.z * 3.5f); var b = ctx.OnGround(at.x + d.x * 3.5f, at.z + d.z * 3.5f);
            Tree(ctx, mk, a, 1f); Tree(ctx, mk, b, .9f);
            var cloth = WorldMaterials.Solid(Dye[ctx.Rng.Next(Dye.Length)], .3f);
            for (var k = 0; k < 6; k++)
            {
                float t0 = .12f + k * .127f, t1 = t0 + .127f;
                var p0 = Vector3.Lerp(a, b, t0) + up * (1.6f - Mathf.Sin(t0 * Mathf.PI) * .8f); var p1 = Vector3.Lerp(a, b, t1) + up * (1.6f - Mathf.Sin(t1 * Mathf.PI) * .8f);
                mk.Box(cloth, (p0 + p1) / 2f, new Vector3(1f, .05f, Vector3.Distance(p0, p1) + .02f), Quaternion.LookRotation(p1 - p0));
                BoxCol(ctx.Root, (p0 + p1) / 2f, new Vector3(1f, .3f, Vector3.Distance(p0, p1) + .02f), Quaternion.LookRotation(p1 - p0));   // the hammock
            }
            StringLights(mk, a + up * 2.6f, b + up * 2.6f, Warm, 8);
        }

        void GroundCover(LandContext ctx)
        {
            var mat = WindMesh.Material(.3f, 1f, new Color(.25f, .45f, .28f));
            var meshes = new Dictionary<(int, int), WindMesh>(); var rng = new System.Random(1983);
            const float step = 2.3f;
            for (var x = ctx.MinX; x < ctx.MaxX; x += step)
            for (var z = ctx.MinZ; z < ctx.MaxZ; z += step)
            {
                var px = x + ((float)rng.NextDouble() - .5f) * step; var pz = z + ((float)rng.NextDouble() - .5f) * step;
                if (!ctx.IsLand(px, pz, 1f) || NearPath(px, pz, 3f) || InNoGrass(px, pz, 0f) || InFootprint(ctx, px, pz, -3f)) continue;
                if (Mathf.PerlinNoise(px * .025f + 1f, pz * .025f + 4f) < .35f) continue;
                var key = (Mathf.FloorToInt(px / 70f), Mathf.FloorToInt(pz / 70f));
                if (!meshes.TryGetValue(key, out var wm)) meshes[key] = wm = new WindMesh();
                var glade = InGlade(px, pz);   // round each place the grass is brighter and full of flowers
                var col = glade ? Color.Lerp(new Color(.45f, .68f, .32f), new Color(.62f, .78f, .38f), (float)rng.NextDouble()) : Color.Lerp(new Color(.24f, .44f, .28f), new Color(.38f, .5f, .3f), (float)rng.NextDouble());
                var at = new Vector3(px, ctx.Ground(px, pz) - .04f, pz);
                wm.Tuft(at, .4f + (float)rng.NextDouble() * .4f, col, Color.Lerp(col, new Color(.8f, .85f, .6f), .3f), rng, 4, .07f);
                if (rng.NextDouble() < (glade ? .3 : .05)) wm.Flower(at + new Vector3(.15f, 0, .1f), .4f, Dye[rng.Next(Dye.Length)], rng);
            }
            foreach (var kv in meshes) kv.Value.Build("Commonwealth grass", ctx.Root, mat);
        }
    }
}
