using System.Collections.Generic;
using UnityEngine;

namespace EverythingWorld.World
{
    /// Arena Rock Ridge (WORLD_GUIDE.md): night under stadium lights, big and earnest. The one deep-night land among
    /// its neighbours (the Hair Metal Strip's violet dusk, the Kingdom's gold): a dark blue sky full of stars,
    /// white searchlights sweeping it, warm orange lighter flames. On the highest part of the ridge a crumbling
    /// stadium stands like a castle, its top cut flat: battlement stands packed with thousands of lighter flames,
    /// floodlight towers, a stage with a light rig and screens. Roads lit by tall white lamps wind round the ridge
    /// to every band; tour buses wait in a lot; tailgate parties, road cases and speaker stacks on the slopes.
    /// Files: ArenaKit (ground, layout, roads, the stadium, the ridge), ArenaArtists, ArenaLife.
    public partial class ArenaKit : LandKit
    {
        public override string Land => "Arena Rock Ridge";
        public override Vector3 ArtistFront(IslandWorldData data, IslandWorldData.Artist a) => facing.TryGetValue(a.name, out var f) ? f : base.ArtistFront(data, a);

        public const string Reaper = "5QTxFnGygVM4jFQiBovmRo", Believin = "4bHsxqR3GMrXTxEPLuK5ue", SailAway = "11G0yprQtMU53BLPMSr093", Tiger = "2HHtWyy5CgaQbC7XSoOb0e", Feeling = "1QEEqeFIZktqIpPI4jSVSF";
        public override (string song, string label, string artist)[] SpecialSongs => new[]
        {
            (Reaper, "(Don't Fear) The Reaper", "Blue Öyster Cult"), (Believin, "Don't Stop Believin'", "Journey"), (SailAway, "Come Sail Away", "Styx"),
            (Tiger, "Eye of the Tiger", "Survivor"), (Feeling, "More Than a Feeling", "Boston"),
            (Renegade, "Renegade", "@the show"), (BurninForYou, "Burnin' for You", "Blue Öyster Cult"), (WheelInTheSky, "Wheel in the Sky", "Journey"),
            (PeaceOfMind, "Peace of Mind", "Boston"), (CarryOn, "Carry on Wayward Son", "Kansas"),
        };

        static readonly Color Flame = new Color(1f, .62f, .25f), Spot = new Color(.9f, .95f, 1f), Ink = new Color(.92f, .94f, 1f), Orange = new Color(1f, .55f, .16f);
        static readonly Color[] Paint = { new Color(.75f, .15f, .15f), new Color(.2f, .4f, .8f), new Color(.95f, .8f, .2f), new Color(.85f, .85f, .88f), new Color(.15f, .15f, .18f) };
        const float RoadW = 5f, StadiumR = 38f;

        Material stone, asphalt, dark, metal, wood, pine, bark, white;
        GlowHalos halos; Vector3 center; ArenaLife life; MeshKit soft;
        readonly Dictionary<(int, int), List<Vector3>> pathCells = new Dictionary<(int, int), List<Vector3>>();
        readonly List<(Vector3 p, int w)> walkPoints = new List<(Vector3, int)>();
        readonly Dictionary<string, Vector3> facing = new Dictionary<string, Vector3>();
        readonly List<Vector3> noGrass = new List<Vector3>();
        bool hasStadium; Vector2 stadiumC;

        void MakeMaterials()
        {
            stone = WorldMaterials.Solid(new Color(.55f, .55f, .6f)); asphalt = WorldMaterials.Solid(new Color(.2f, .2f, .23f), .4f);
            dark = WorldMaterials.Solid(new Color(.07f, .07f, .09f)); metal = WorldMaterials.Solid(new Color(.72f, .74f, .78f), .8f);
            wood = WorldMaterials.Solid(new Color(.4f, .3f, .22f)); pine = WorldMaterials.Solid(new Color(.12f, .26f, .2f));
            bark = WorldMaterials.Solid(new Color(.3f, .24f, .2f)); white = WorldMaterials.Solid(new Color(.92f, .92f, .95f));
        }

        Material GlowM(Color c, float s = 2f) => WorldMaterials.Glow(c, s * .65f);
        static bool HasStation(IslandWorldData.Artist a) => a.station || a.stationId != 0;
        static float Foot(IslandWorldData.Artist a) => a.name == "Journey" || a.name == "Boston" || a.name == "Blue Öyster Cult" ? 18f : HasStation(a) ? 16f : 11f;

        public override bool Ground(float height, float slope, float patch, out Color color)
        {
            if (slope > 8f) color = Color.Lerp(new Color(.42f, .43f, .48f), new Color(.5f, .5f, .55f), patch);
            else if (height < 3f) color = new Color(.5f, .48f, .42f);
            else color = Color.Lerp(new Color(.22f, .36f, .3f), new Color(.3f, .4f, .32f), Mathf.SmoothStep(.2f, .9f, patch));
            color.a = 1f;
            return true;
        }

        public override void ShapeGround(IslandWorldData data, int index)
        {
            var isl = data.layout.islands[index]; var heart = new Vector2(isl.labelX, isl.labelZ);
            // the stadium: the highest spot along the ridge with room round it
            hasStadium = false; var bestH = float.MinValue;
            foreach (var margin in new[] { 48f, 32f, 20f, 10f })
            {
            if (hasStadium) break;
            for (var r = 0; r < data.Nz; r += 3)
            for (var col = 0; col < data.Nx; col += 3)
            {
                var i = r * data.Nx + col; if (data.islandIds[i] != index) continue;
                float x = col * data.Step, z = data.layout.depth - r * data.Step; var p = new Vector2(x, z);
                if (data.heights[i] <= bestH || Vector2.Distance(p, heart) < StadiumR + 40f) continue;
                var ok = true;
                for (var k = 0; k < 16 && ok; k++) { var a = k * Mathf.PI / 8f; if (data.IslandAt(x + Mathf.Cos(a) * (StadiumR + margin), z + Mathf.Sin(a) * (StadiumR + margin)) != isl) ok = false; }
                if (!ok) continue;
                bestH = data.heights[i]; stadiumC = p; hasStadium = true;
            }
            }
            Debug.Log(hasStadium ? $"Arena Rock Ridge: the stadium at {stadiumC.x:0},{stadiumC.y:0}" : "Arena Rock Ridge: no room for the stadium");
            if (hasStadium) Level(data, index, stadiumC, StadiumR + 6f, StadiumR + 34f);
            var avoid = new List<Vector3>();
            if (hasStadium) avoid.Add(new Vector3(stadiumC.x, StadiumR + 22f, stadiumC.y));
            ArtistPlacement.Spread(data, index, 85f, heart, 50f, 45f, avoid, null, Land, 300f);
            foreach (var a in data.layout.artists)
            {
                if (a.sceneId != isl.sceneId) continue;
                a.r = Mathf.Max(a.r, Foot(a) + 8f);
                Level(data, index, new Vector2(a.x, a.z), Foot(a) + 1f, Foot(a) + 14f);
            }
            Level(data, index, heart, 14f, 32f);
            foreach (var (at, r) in PlaceRings) Level(data, index, at, r, r + 16f);
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
            life = new GameObject("Arena life").AddComponent<ArenaLife>();
            life.transform.SetParent(ctx.Root, false);
            life.center = center;
            var mk = new MeshKit(); var solid = new MeshKit();
            Arrival(ctx, mk, solid);
            Roads(ctx, mk, solid);
            Stadium(ctx, mk, solid);
            RidgePlaces(ctx, mk, solid);
            Ridge(ctx, mk, solid);
            mk.Build("Arena", ctx.Root, false);
            soft.Build("Soft arena", ctx.Root, false);
            solid.BuildCollider("Arena walls", ctx.Root);
        }

        public override void Finish(LandContext ctx) { GroundCover(ctx); halos.Build("Arena glow", ctx.Root); }

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
            && Vector2.Distance(new Vector2(x, z), new Vector2(center.x, center.z)) > 24f + pad
            && (!hasStadium || Vector2.Distance(new Vector2(x, z), stadiumC) > StadiumR + 10f + pad);

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

        /// A tall stage lamp: a black truss post with a white floodlight head.
        Renderer Lamp(LandContext ctx, MeshKit mk, Transform parent, Vector3 g, float h = 6f)
        {
            mk.Box(dark, g + Vector3.up * (h / 2f - .3f), new Vector3(.25f, h + .6f, .25f), Quaternion.identity);
            var go = new GameObject("Lamp"); go.transform.SetParent(parent, false);
            var lk = new MeshKit(); lk.Box(GlowM(new Color(.95f, .97f, 1f), 2.4f), Vector3.zero, new Vector3(.6f, .35f, .45f), Quaternion.identity);
            var built = lk.BuildSingle("Light", go.transform, false); go.transform.position = g + Vector3.up * h;
            halos.Add(g + Vector3.up * h, 1.6f, Spot, .7f);
            ArtistBits.PostCollider(ctx.Root, g, h, .15f);
            return built.GetComponent<Renderer>();
        }

        // ---------------------------------------------------------------- where you arrive
        /// Ticket booths and a row of turnstiles under a lit arch, a giant cowbell on a plinth, tour posters.
        void Arrival(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var c = ctx.OnGround(center.x, center.z);
            var face = hasStadium ? new Vector3(stadiumC.x - c.x, 0, stadiumC.y - c.z).normalized : Vector3.forward;
            var rot = Quaternion.LookRotation(face); var side = Vector3.Cross(up, face);
            var gate = c + face * 10f;
            foreach (var s in new[] { -1f, 1f })
            {
                var b = gate + side * s * 7f;
                mk.Box(WorldMaterials.Solid(new Color(.6f, .1f, .12f)), b + up * 1.6f, new Vector3(3f, 3.2f, 2.4f), rot); solid.Box(WorldMaterials.Solid(new Color(.6f, .1f, .12f)), b + up * 1.6f, new Vector3(3f, 3.2f, 2.4f), rot);
                mk.Box(GlowM(new Color(1f, .9f, .7f), 1.8f), b + up * 1.8f - face * 1.21f, new Vector3(2f, .8f, .02f), rot);
                ArtistBits.Label(ctx.Root, "TICKETS", b + up * 2.9f - face * 1.22f, -face, .25f, Ink, TextAnchor.MiddleCenter);
            }
            for (var k = -2; k <= 2; k++) { var t = gate + side * k * 1.4f; mk.Box(metal, t + up * .5f, new Vector3(.15f, 1f, 1.2f), rot); mk.Box(metal, t + up * .9f + side * .35f, new Vector3(.6f, .06f, .06f), rot); }
            foreach (var s in new[] { -1f, 1f }) { mk.Box(dark, gate + side * s * 9.5f + up * 4f, new Vector3(.5f, 8f, .5f), rot); ArtistBits.PostCollider(ctx.Root, gate + side * s * 9.5f, 8f, .3f); }
            mk.Box(dark, gate + up * 8.2f, new Vector3(19.5f, 1.6f, .5f), rot);
            mk.Box(GlowM(Orange, 2.2f), gate + up * 8.2f - face * .29f, new Vector3(18.5f, 1.2f, .02f), rot);
            ArtistBits.Label(ctx.Root, "ARENA ROCK RIDGE  ·  TONIGHT", gate + up * 8.2f - face * .32f, -face, .5f, new Color(.15f, .05f, .02f), TextAnchor.MiddleCenter);
            halos.Add(gate + up * 8f - face * 2f, 6f, Orange, .5f);
            // tour posters on boards
            for (var k = 0; k < 4; k++)
            {
                var p = c - face * 6f + side * ((k - 1.5f) * 4f);
                mk.Box(dark, p + up * 1.4f, new Vector3(2.4f, 2.8f, .1f), rot);
                mk.Box(WorldMaterials.Solid(Paint[k % Paint.Length]), p + up * 1.6f + face * .12f, new Vector3(2f, 2.2f, .02f), rot);
                mk.Box(GlowM(new Color(1f, .95f, .85f), 1.4f), p + up * 2.95f + face * .1f, new Vector3(2f, .08f, .08f), rot);
                solid.Box(dark, p + up * 1.4f, new Vector3(2.4f, 2.8f, .2f), rot);
            }
            ArtistBits.PointLight(ctx.Root, gate + up * 6f - face * 3f, new Color(1f, .8f, .6f), 22f, 2f);
            noGrass.Add(new Vector3(c.x, 16f, c.z));
            Remember(c, 0);
        }

        // ---------------------------------------------------------------- roads
        void Roads(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var todo = new List<IslandWorldData.Artist>(ctx.Artists);
            var walk = 1; var lamps = new List<Vector3>();
            var line = WorldMaterials.Solid(new Color(.95f, .85f, .4f));
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
                for (var d = walk == 1 ? 8f : RoadW / 2f + .1f; ; d += 3f)
                {
                    var dd = Mathf.Min(d, end);
                    var q = from + dir * dd + side * Mathf.Sin(dd / Mathf.Max(end, 1f) * Mathf.PI) * Mathf.Min(8f, end * .06f);
                    if (!ctx.IsLand(q.x, q.z, 0f)) break;
                    pts.Add(ctx.OnGround(q.x, q.z));
                    if (dd >= end) break;
                }
                if (pts.Count < 2) continue;
                Strip(mk, pts, asphalt, RoadW, .06f);
                for (var i = 1; i < pts.Count; i += 2) { var a = pts[i - 1]; var b = pts[i]; mk.Box(line, (a + b) / 2f + Vector3.up * .075f, new Vector3(.15f, .02f, Vector3.Distance(a, b) * .6f), Quaternion.LookRotation(b - a)); }
                foreach (var p in pts) Remember(p, walk);
                for (var i = 4; i < pts.Count; i += 8)
                {
                    var lp = pts[i] + side * ((i / 8) % 2 == 0 ? 1f : -1f) * (RoadW / 2f + .8f);
                    var near = false; foreach (var o in lamps) if ((o - lp).sqrMagnitude < 26f * 26f) near = true;
                    if (near) continue;
                    lamps.Add(lp); Lamp(ctx, mk, ctx.Root, ctx.OnGround(lp.x, lp.z));
                }
                walk++;
            }
        }

        // ---------------------------------------------------------------- the stadium
        /// A crumbling stadium standing like a castle on the ridge: a ring of tall stands with battlement tops and
        /// a gateway facing the road in, banks of seats stepping down inside, packed with lighter flames (ArenaLife
        /// brightens them), four floodlight towers, a stage with a truss light rig and two screens, searchlights.
        void Stadium(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            if (!hasStadium) return;
            var best = ctx.OnGround(stadiumC.x, stadiumC.y); var up = Vector3.up; const float R = StadiumR; const int n = 32;
            Vector3 from = center; var bd = float.MaxValue;
            foreach (var (p, w) in walkPoints) { if (w >= 990) continue; var d = (p.x - best.x) * (p.x - best.x) + (p.z - best.z) * (p.z - best.z); if (d < bd) { bd = d; from = p; } }
            var gateDir = new Vector3(from.x - best.x, 0, from.z - best.z).normalized; if (gateDir.sqrMagnitude < .1f) gateDir = Vector3.forward;
            var off = Mathf.Atan2(gateDir.z, gateDir.x) - .5f * Mathf.PI * 2f / n;
            var pts = new List<Vector3>(); var total = Vector3.Distance(new Vector3(from.x, 0, from.z), new Vector3(best.x, 0, best.z)) - R + 3f;
            for (var d = RoadW / 2f; d < total; d += 3f) { var q = from - gateDir * d; pts.Add(ctx.OnGround(q.x, q.z)); }
            if (pts.Count > 1) { Strip(mk, pts, asphalt, RoadW + 2f, .06f); foreach (var p in pts) Remember(p, 990); for (var i = 3; i < pts.Count; i += 7) Lamp(ctx, mk, ctx.Root, ctx.OnGround(pts[i].x + Vector3.Cross(up, gateDir).x * 4.5f, pts[i].z + Vector3.Cross(up, gateDir).z * 4.5f)); }
            var standM = WorldMaterials.Solid(new Color(.52f, .5f, .55f)); var seat = WorldMaterials.Solid(new Color(.18f, .2f, .35f));
            var crowd = new Material(GlowM(Flame, 1.6f)) { name = "Crowd lights" };
            var rng = new System.Random(1977);
            for (var k = 0; k < n; k++)
            {
                float a0 = k * Mathf.PI * 2f / n + off, a1 = (k + 1) * Mathf.PI * 2f / n + off, am = (a0 + a1) / 2f;
                var d = new Vector3(Mathf.Cos(am), 0, Mathf.Sin(am)); var gate = k % 8 == 0;
                var h = gate ? 7f : 15f + (k % 3 == 1 ? -4f : 0f) + ctx.Range(-2f, .5f);
                var p0 = best + new Vector3(Mathf.Cos(a0), 0, Mathf.Sin(a0)) * R; var p1 = best + new Vector3(Mathf.Cos(a1), 0, Mathf.Sin(a1)) * R;
                var mid = (p0 + p1) / 2f; var len = Vector3.Distance(p0, p1) + .3f; var rot = Quaternion.LookRotation(p1 - p0);
                if (gate)
                {
                    foreach (var s in new[] { -.38f, .38f }) { var pier = Vector3.Lerp(p0, p1, .5f + s); mk.Box(standM, pier + up * 3.5f, new Vector3(2.4f, 9f, 2.2f), rot); solid.Box(standM, pier + up * 3.5f, new Vector3(2.4f, 9f, 2.2f), rot); }
                    mk.Box(standM, mid + up * 8.4f, new Vector3(2.2f, 1.6f, len), rot);
                    continue;
                }
                mk.Box(standM, mid + up * (h / 2f - 1.5f), new Vector3(1.8f, h + 3f, len), rot); solid.Box(standM, mid + up * (h / 2f - 1.5f), new Vector3(1.8f, h + 3f, len), rot);
                for (var b = -1; b <= 1; b += 2) mk.Box(standM, mid + up * (h + .7f) + (p1 - p0).normalized * b * len * .25f, new Vector3(1.8f, 1.4f, len * .3f), rot);
                for (var t = 0; t < 7; t++)
                {
                    var top = h - 2.5f - t * 1.8f; if (top < .6f) break;
                    var c = mid - d * (1.6f + t * 1.5f);
                    mk.Box(standM, c + up * (top / 2f - .5f), new Vector3(1.5f, top + 1f, len), rot);
                    mk.Box(seat, c + up * (top + .08f) - d * .3f, new Vector3(.6f, .16f, len * .96f), rot);
                    solid.Box(standM, c + up * (top / 2f - .5f), new Vector3(1.5f, top + 1f, len), rot);
                    // the crowd: a lighter flame (or a phone) held up every metre or so
                    for (var q = 0; q < 6; q++)
                    {
                        if (rng.NextDouble() < .25) continue;
                        var lp = Vector3.Lerp(p0, p1, (q + .5f) / 6f) - d * (1.6f + t * 1.5f) + up * (top + 1.3f + (float)rng.NextDouble() * .3f);
                        mk.Box(crowd, lp, new Vector3(.08f, .16f, .08f), rot);
                    }
                }
            }
            life.crowd = crowd;
            for (var k = 0; k < 4; k++)
            {
                var a = k * Mathf.PI / 2f + Mathf.PI / 4f + off; var p = best + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * (R + 5f); p.y = ctx.Ground(p.x, p.z);
                mk.Box(metal, p + up * 13f, new Vector3(.9f, 27f, .9f), Quaternion.identity);
                mk.Box(dark, p + up * 27f, new Vector3(5f, 3f, .5f), Quaternion.LookRotation(best - p));
                for (var i = 0; i < 9; i++) mk.Box(GlowM(Spot, 2.6f), p + up * (26.1f + (i / 3) * .95f) + Vector3.Cross(up, (best - p).normalized) * ((i % 3 - 1) * 1.5f) + (best - p).normalized * .3f, new Vector3(1.1f, .8f, .05f), Quaternion.LookRotation(best - p));
                halos.Add(p + up * 27f + (best - p).normalized * 3f, 4f, Spot, .55f);
                ArtistBits.PostCollider(ctx.Root, p, 27f, .7f);
            }
            // the pitch and the stage at the far end from the gate
            mk.Prism(WorldMaterials.Solid(new Color(.2f, .38f, .22f)), best + up * .05f, up, R - 10f, .05f, n);
            var st = best - gateDir * (R - 13f); var srot = Quaternion.LookRotation(gateDir); var sside = Vector3.Cross(up, gateDir);
            mk.Box(dark, st + up * 1.1f, new Vector3(22f, 2.2f, 10f), srot); solid.Box(dark, st + up * 1.1f, new Vector3(22f, 2.2f, 10f), srot);
            foreach (var s in new[] { -1f, 1f }) foreach (var b in new[] { -1f, 1f }) { var t = st + sside * s * 10.5f + gateDir * b * 4.5f; mk.Box(metal, t + up * 7f, new Vector3(.4f, 12f, .4f), srot); ArtistBits.PostCollider(ctx.Root, t, 13f, .3f); }
            for (var b = -1; b <= 1; b += 2) mk.Box(metal, st + up * 13f + gateDir * b * 4.5f, new Vector3(21.5f, .5f, .5f), srot);
            var rig = new List<Material>();
            for (var k = 0; k < 12; k++)
            {
                var m = new Material(GlowM(new[] { Spot, Orange, new Color(.4f, .6f, 1f), new Color(1f, .3f, .3f) }[k % 4], 2.4f)) { name = "Rig light" }; rig.Add(m);
                mk.Prism(m, st + up * 12.6f + sside * ((k - 5.5f) * 1.7f) + gateDir * (k % 2 == 0 ? 4.5f : -4.5f), (gateDir - up).normalized, .35f, .5f, 10);
            }
            life.rig = rig.ToArray();
            foreach (var s in new[] { -1f, 1f })
            {
                var scr = st + sside * s * 15f + up * 8f;
                mk.Box(dark, scr, new Vector3(7.5f, 5.2f, .5f), srot);
                var screen = new Material(GlowM(new Color(.5f, .6f, 1f), 1.2f)) { name = "Stage screen" };
                mk.Box(screen, scr + gateDir * .26f, new Vector3(7f, 4.6f, .02f), srot);
                life.screens.Add(screen);
                // the live feed: a band member in silhouette against the lights (singer on one, guitarist on the
                // other), a ticker bar along the bottom; they were blank white before
                var fp = scr + gateDir * .29f; var ink = WorldMaterials.Solid(new Color(.03f, .03f, .05f));
                var face = fp - up * .5f + sside * s * -.6f;
                mk.Box(ink, face - up * .85f, new Vector3(1.5f, 1.1f, .02f), srot);
                mk.Blob(ink, face + up * .35f, new Vector3(.45f, .5f, .02f), rng, 0f);
                mk.Blob(ink, face + up * .55f - sside * s * .35f, new Vector3(.35f, .55f, .02f), rng, .2f);      // hair
                if (s < 0) { mk.Box(ink, face + up * .2f + sside * .9f, new Vector3(.3f, 1.8f, .02f), srot * Quaternion.Euler(0, 0, -35f)); mk.Box(ink, face + up * .05f - sside * .55f, new Vector3(.08f, 1.4f, .02f), srot * Quaternion.Euler(0, 0, 20f)); }   // fist up, mic
                else { mk.Box(ink, face - up * 1f + sside * .2f, new Vector3(2.4f, .5f, .02f), srot * Quaternion.Euler(0, 0, 25f)); mk.Blob(ink, face - up * 1.4f - sside * .5f, new Vector3(.6f, .5f, .02f), rng, 0f); }   // guitar
                mk.Box(ink, fp - up * 1.95f, new Vector3(7f, .5f, .02f), srot);
                ArtistBits.Label(ctx.Root, "LIVE  •  ARENA ROCK RIDGE", fp - up * 1.95f + gateDir * .02f + sside * 1.2f, gateDir, .2f, Spot, TextAnchor.MiddleCenter);
                mk.Box(dark, scr - up * 4f, new Vector3(.4f, 8f, .4f), srot); ArtistBits.PostCollider(ctx.Root, scr - up * 8f, 8f, .3f);
            }
            for (var k = 0; k < 4; k++) foreach (var s in new[] { -1f, 1f }) { var amp = st + up * (2.2f + .9f + (k / 2) * 1.8f) + sside * s * (7f + (k % 2) * 2f) - gateDir * 2f; mk.Box(dark, amp, new Vector3(1.9f, 1.75f, 1f), srot); }
            ArtistBits.PointLight(ctx.Root, st + up * 10f + gateDir * 6f, Spot, 40f, 2.4f);
            Show(ctx, mk, solid, best, st, gateDir, sside, crowd);
            life.stadium = best; life.stage = st + up * 2.2f; life.searchFrom = st + up * 14f;
            var m1 = new GameObject("Shot: the stadium").transform; m1.SetParent(ctx.Root, false);
            var outside = best + gateDir * (R + 30f); outside.y = ctx.Ground(outside.x, outside.z); m1.SetPositionAndRotation(outside, Quaternion.LookRotation(-gateDir));
            var m2 = new GameObject("Shot: inside the stadium").transform; m2.SetParent(ctx.Root, false);
            m2.SetPositionAndRotation(best + gateDir * (R - 14f) + up * .1f, Quaternion.LookRotation(-gateDir));
            noGrass.Add(new Vector3(best.x, R + 6f, best.z));
        }

        // ---------------------------------------------------------------- the ridge round it all
        /// Pines and rock outcrops on the slopes, a lot of tour buses, tailgate parties with string lights and a
        /// cooler, road cases, speaker stacks facing out over the valley, lone lamp-lit benches.
        void Ridge(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            int pines = 0, rocks = 0, tails = 0, cases = 0, stacks = 0;
            if (hasStadium) TourBuses(ctx, mk, solid);
            const float cell = 32f;
            for (var x = ctx.MinX; x < ctx.MaxX; x += cell)
            for (var z = ctx.MinZ; z < ctx.MaxZ; z += cell)
            {
                float px = x + ctx.Range(0f, cell), pz = z + ctx.Range(0f, cell);
                if (!Open(ctx, px, pz, 5f)) continue;
                var p = ctx.OnGround(px, pz); var nearRoad = NearPath(px, pz, 14f); var roll = ctx.Range(0f, 1f);
                if (nearRoad && tails < 7 && roll < .22f) { Tailgate(ctx, mk, solid, p); tails++; }
                else if (nearRoad && cases < 6 && roll < .34f) { RoadCases(ctx, mk, solid, p); cases++; }
                else if (roll < .5f) { for (var k = 0; k < ctx.Rng.Next(2, 6); k++) Pine(ctx, mk, ctx.OnGround(px + ctx.Range(-8f, 8f), pz + ctx.Range(-8f, 8f)), ctx.Range(.8f, 1.4f)); pines++; }
                else if (roll < .62f) { Rock(ctx, mk, solid, p); rocks++; }
                else if (roll < .67f && stacks < 5) { SpeakerStack(ctx, mk, solid, p); stacks++; }
                else continue;
                noGrass.Add(new Vector3(px, 5f, pz));
            }
            Debug.Log($"Arena Rock Ridge: {pines} pine groups, {rocks} rocks, {tails} tailgates, {cases} road-case piles, {stacks} speaker stacks");
        }

        void Pine(LandContext ctx, MeshKit mk, Vector3 at, float s)
        {
            var up = Vector3.up; var h = 7f * s;
            mk.Prism(bark, at - up * .3f, up, .25f * s, h * .3f, 6);
            for (var k = 0; k < 3; k++) soft.Cone(pine, at + up * (h * (.2f + k * .22f)), (2.2f - k * .55f) * s, h * .4f, 7, ctx.Range(0, 60f));
            ArtistBits.PostCollider(ctx.Root, at, h * .5f, .3f * s);
        }

        void Rock(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 at)
        {
            var s = ctx.Range(1.5f, 3.5f);
            mk.Blob(stone, at + Vector3.up * s * .4f, new Vector3(s * 1.3f, s * .8f, s), ctx.Rng, .35f);
            solid.Box(stone, at + Vector3.up * s * .4f, new Vector3(s * 2f, s * 1.4f, s * 1.6f), Quaternion.identity);
        }

        void TourBuses(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            // a lot beside the stadium road: find the open patch nearest the stadium gate road
            var up = Vector3.up; Vector3 best = default; var bd = float.MaxValue;
            for (var t = 0; t < 300; t++)
            {
                float x = stadiumC.x + ctx.Range(-110f, 110f), z = stadiumC.y + ctx.Range(-110f, 110f);
                if (!Open(ctx, x, z, 16f)) continue;
                var d = Vector2.Distance(new Vector2(x, z), stadiumC); if (d < bd) { bd = d; best = ctx.OnGround(x, z); }
            }
            if (bd == float.MaxValue) return;
            var dir = new Vector3(stadiumC.x - best.x, 0, stadiumC.y - best.z).normalized; var side = Vector3.Cross(up, dir);
            Color[] liveries = { new Color(.1f, .1f, .12f), new Color(.85f, .85f, .88f), new Color(.5f, .1f, .12f), new Color(.15f, .2f, .4f) };
            for (var k = 0; k < 4; k++)
            {
                var p = ctx.OnGround(best.x + side.x * (k - 1.5f) * 4.5f, best.z + side.z * (k - 1.5f) * 4.5f); var rot = Quaternion.LookRotation(dir);
                var body = WorldMaterials.Solid(liveries[k], .7f);
                mk.Box(body, p + up * 2.1f, new Vector3(2.6f, 3.4f, 13f), rot);
                mk.Box(WorldMaterials.Solid(new Color(.1f, .14f, .2f), .95f), p + up * 2.6f, new Vector3(2.74f, 1f, 10.5f), rot);
                mk.Box(GlowM(Orange, 1.4f), p + up * 1.2f, new Vector3(2.68f, .1f, 12.6f), rot);
                foreach (var s in new[] { -1f, 1f }) foreach (var z in new[] { -4.5f, 4.5f }) mk.Prism(dark, p + side * s * 1.2f + dir * z + up * .55f, side * s, .55f, .3f, 12);
                foreach (var s in new[] { -.8f, .8f }) mk.Prism(GlowM(new Color(1f, .95f, .85f), 2f), p + up * 1f + dir * 6.52f + side * s, dir, .18f, .05f, 10);
                solid.Box(body, p + up * 2f, new Vector3(2.7f, 4f, 13.2f), rot);
            }
            Lamp(ctx, mk, ctx.Root, ctx.OnGround(best.x + side.x * 12f, best.z + side.z * 12f), 7f);
            noGrass.Add(new Vector3(best.x, 14f, best.z));
            life.buses = best;
        }

        void Tailgate(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 at)
        {
            var up = Vector3.up; var dir = Quaternion.Euler(0, ctx.Range(0, 360f), 0) * Vector3.forward; var rot = Quaternion.LookRotation(dir); var side = Vector3.Cross(up, dir);
            var body = WorldMaterials.Solid(Paint[ctx.Rng.Next(Paint.Length)], .7f);
            mk.Box(body, at + up * .85f, new Vector3(2f, .8f, 4.8f), rot); mk.Box(body, at + up * 1.5f + dir * .4f, new Vector3(1.9f, .7f, 1.8f), rot);
            mk.Box(dark, at + up * .7f - dir * 1.5f, new Vector3(1.8f, .1f, 1.9f), rot);
            foreach (var s in new[] { -1f, 1f }) foreach (var z in new[] { -1.6f, 1.6f }) mk.Prism(dark, at + side * s * .9f + dir * z + up * .45f, side * s, .45f, .28f, 12);
            solid.Box(body, at + up * 1f, new Vector3(2.1f, 2f, 5f), rot);
            var tg = at - dir * 3.4f;
            mk.Box(WorldMaterials.Solid(new Color(.85f, .2f, .2f)), tg + up * .25f + side * 1.2f, new Vector3(.7f, .5f, .45f), rot);
            for (var k = 0; k < 2; k++) { var ch = tg + side * (k == 0 ? -1.2f : .2f) - dir * .6f; mk.Box(WorldMaterials.Solid(new Color(.2f, .4f, .7f)), ch + up * .35f, new Vector3(.6f, .06f, .5f), rot); mk.Box(WorldMaterials.Solid(new Color(.2f, .4f, .7f)), ch + up * .7f - dir * .25f, new Vector3(.6f, .6f, .05f), rot); Interact.Seat(life.transform, ch + up * .4f, dir, "Sit in the camp chair"); }
            for (var k = 0; k < 8; k++) { var b = at + up * 2.2f - dir * (2.4f + k * .4f) + side * Mathf.Sin(k) * .3f - up * Mathf.Sin(k / 7f * Mathf.PI) * .4f; mk.Blob(GlowM(new Color(1f, .85f, .5f), 2.6f), b, Vector3.one * .08f, ctx.Rng, 0f); }
            halos.Add(at + up * 2f - dir * 3.4f, 1.6f, new Color(1f, .8f, .5f), .7f);
            ArtistBits.PointLight(ctx.Root, at + up * 2f - dir * 3.5f, new Color(1f, .8f, .5f), 8f, 1.2f);
        }

        void RoadCases(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 at)
        {
            var up = Vector3.up; var r = Quaternion.Euler(0, ctx.Range(0, 360f), 0);
            for (var k = 0; k < ctx.Rng.Next(3, 7); k++)
            {
                var p = at + r * new Vector3((k % 3) * 1.3f, .45f + (k / 3) * .9f, 0); var rot = r * Quaternion.Euler(0, ctx.Range(-6f, 6f), 0);
                mk.Box(dark, p, new Vector3(1.2f, .9f, .9f), rot);
                mk.Box(metal, p, new Vector3(1.24f, .08f, .94f), rot);
                mk.Box(white, p + rot * Vector3.forward * .46f, new Vector3(.6f, .2f, .02f), rot);
            }
            solid.Box(dark, at + r * new Vector3(1.3f, .9f, 0), new Vector3(4f, 1.8f, 1f), r);
        }

        void SpeakerStack(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 at)
        {
            var up = Vector3.up; var face = (at - (hasStadium ? new Vector3(stadiumC.x, at.y, stadiumC.y) : center)); face.y = 0; face = face.sqrMagnitude > .1f ? face.normalized : Vector3.forward;
            var rot = Quaternion.LookRotation(face); var side = Vector3.Cross(up, face); var grille = WorldMaterials.Solid(new Color(.25f, .25f, .28f));
            for (var x = 0; x < 3; x++) for (var y = 0; y < 3; y++)
            {
                var p = at + side * ((x - 1f) * 1.9f) + up * (.9f + y * 1.8f);
                mk.Box(dark, p, new Vector3(1.8f, 1.7f, 1.1f), rot); mk.Prism(grille, p + face * .55f, face, .65f, .05f, 14);
            }
            solid.Box(dark, at + up * 2.7f, new Vector3(5.6f, 5.4f, 1.1f), rot);
        }

        void GroundCover(LandContext ctx)
        {
            var mat = WindMesh.Material(.25f, .9f, new Color(.25f, .38f, .3f));
            var meshes = new Dictionary<(int, int), WindMesh>(); var rng = new System.Random(1978);
            const float step = 2.4f;
            for (var x = ctx.MinX; x < ctx.MaxX; x += step)
            for (var z = ctx.MinZ; z < ctx.MaxZ; z += step)
            {
                var px = x + ((float)rng.NextDouble() - .5f) * step; var pz = z + ((float)rng.NextDouble() - .5f) * step;
                if (!ctx.IsLand(px, pz, 1f) || NearPath(px, pz, 3.4f) || InNoGrass(px, pz, 0f) || InFootprint(ctx, px, pz, -2f)) continue;
                if (Mathf.PerlinNoise(px * .025f + 4f, pz * .025f + 6f) < .38f) continue;
                var key = (Mathf.FloorToInt(px / 70f), Mathf.FloorToInt(pz / 70f));
                if (!meshes.TryGetValue(key, out var wm)) meshes[key] = wm = new WindMesh();
                var col = Color.Lerp(new Color(.22f, .36f, .3f), new Color(.34f, .42f, .34f), (float)rng.NextDouble());
                wm.Tuft(new Vector3(px, ctx.Ground(px, pz) - .04f, pz), .35f + (float)rng.NextDouble() * .4f, col, Color.Lerp(col, new Color(.7f, .75f, .9f), .3f), rng, 3, .07f);
            }
            foreach (var kv in meshes) kv.Value.Build("Ridge grass", ctx.Root, mat);
        }
    }
}
