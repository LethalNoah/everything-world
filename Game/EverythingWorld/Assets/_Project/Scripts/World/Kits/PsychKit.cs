using System.Collections.Generic;
using UnityEngine;

namespace EverythingWorld.World
{
    /// The Psychedelic Wilds (WORLD_GUIDE.md): a bright, hot, heat-hazed day, the sky slowly cycling through colours.
    /// Desert ground with mesas striped in bands of colour melts into a forest of giant mushrooms whose undersides
    /// glow. Where you arrive a giant spiral mushroom towers over everything. Rainbow-painted paths wind to every
    /// artist. The Grateful Dead have a whole district (PsychDead); Queens of the Stone Age have a desert road and
    /// a generator party (PsychArtists). Lava-lamp plants, crystal clusters, painted camper vans, colour motes.
    /// Files: PsychKit (ground, layout, paths, the spiral mushroom, the wilds), PsychDead, PsychArtists, PsychLife.
    public partial class PsychKit : LandKit
    {
        public override string Land => "The Psychedelic Wilds";
        public override Vector3 ArtistFront(IslandWorldData data, IslandWorldData.Artist a) => facing.TryGetValue(a.name, out var f) ? f : base.ArtistFront(data, a);

        public const string BoxOfRain = "7x2xjJV3YAPeLQJ7u3Kjet", Sunshine = "00uZCwUxIpjqo2imIkjP8s", EyesOfTheWorld = "5pHVAQ5LD3RgDG5m11r2Jt", Franklins = "0l1rayoUd1Se4E4OqA4hdc",
            Shakedown = "1lNRVjK8MukRZpeurYssIx", SongForTheDeaf = "6aCZqq6JkwrfbeXU71VNk0";
        public override (string song, string label, string artist)[] SpecialSongs => new[]
        {
            (BoxOfRain, "Box of Rain", "Grateful Dead"), (Sunshine, "Here Comes Sunshine", "Grateful Dead"), (EyesOfTheWorld, "Eyes of the World", "Grateful Dead"),
            (Franklins, "Franklin's Tower", "Grateful Dead"), (Shakedown, "Shakedown Street", "Grateful Dead"), (SongForTheDeaf, "Song For The Deaf", "Queens of the Stone Age"),
        };

        static readonly Color[] Rainbow = { new Color(1f, .3f, .4f), new Color(1f, .6f, .15f), new Color(1f, .9f, .2f), new Color(.35f, .9f, .4f), new Color(.25f, .65f, 1f), new Color(.65f, .4f, 1f) };
        static readonly Color[] Bands = { new Color(.85f, .45f, .4f), new Color(.92f, .7f, .45f), new Color(.75f, .4f, .6f), new Color(.95f, .82f, .55f), new Color(.55f, .45f, .75f), new Color(.88f, .58f, .35f) };
        static readonly Color Ink = new Color(1f, .97f, .92f);
        const float PathW = 3.4f;

        Material dark, white, sandM, stalk, metal, wood;
        GlowHalos halos; Vector3 center; PsychLife life; MeshKit soft;
        readonly Dictionary<(int, int), List<Vector3>> pathCells = new Dictionary<(int, int), List<Vector3>>();
        readonly List<(Vector3 p, int w)> walkPoints = new List<(Vector3, int)>();
        readonly Dictionary<string, Vector3> facing = new Dictionary<string, Vector3>();
        readonly List<Vector3> noGrass = new List<Vector3>();
        Material[] capGlow;

        void MakeMaterials()
        {
            dark = WorldMaterials.Solid(new Color(.1f, .08f, .12f)); white = WorldMaterials.Solid(new Color(.96f, .95f, .92f));
            sandM = WorldMaterials.Solid(new Color(.9f, .72f, .6f)); stalk = WorldMaterials.Solid(new Color(.95f, .9f, .82f));
            metal = WorldMaterials.Solid(new Color(.75f, .76f, .8f), .8f); wood = WorldMaterials.Solid(new Color(.5f, .36f, .25f));
            capGlow = new Material[Rainbow.Length];
            for (var i = 0; i < Rainbow.Length; i++) capGlow[i] = new Material(WorldMaterials.Glow(Rainbow[i], 1.1f)) { name = "Mushroom glow" };
        }

        Material GlowM(Color c, float s = 2f) => WorldMaterials.Glow(c, s * .65f);
        static bool HasStation(IslandWorldData.Artist a) => a.station || a.stationId != 0;
        static float Foot(IslandWorldData.Artist a) => a.name == "Grateful Dead" ? 30f : HasStation(a) ? 16f : 8f;

        /// Flat ground is pale desert pink; slopes show bands of colour stacked by height, like striped mesas.
        public override bool Ground(float height, float slope, float patch, out Color color)
        {
            if (slope > 6f) { var b = Mathf.FloorToInt(height / 2.2f); color = Bands[((b % Bands.Length) + Bands.Length) % Bands.Length]; }
            else if (height < 2.5f) color = new Color(.95f, .85f, .7f);
            else color = patch > .7f ? Color.Lerp(new Color(.92f, .62f, .62f), new Color(.75f, .55f, .8f), (patch - .7f) / .3f) : Color.Lerp(new Color(.95f, .8f, .5f), new Color(.92f, .7f, .48f), Mathf.SmoothStep(.1f, .7f, patch));
            color.a = 1f;
            return true;
        }

        public override void ShapeGround(IslandWorldData data, int index)
        {
            var isl = data.layout.islands[index]; var heart = new Vector2(isl.labelX, isl.labelZ);
            ArtistPlacement.Spread(data, index, 62f, heart, 48f, 48f, null, null, Land, 310f);
            foreach (var a in data.layout.artists)
            {
                if (a.sceneId != isl.sceneId) continue;
                a.r = Mathf.Max(a.r, Foot(a) + 6f);
                Level(data, index, new Vector2(a.x, a.z), Foot(a) + 1f, Foot(a) + 12f);
            }
            Level(data, index, heart, 16f, 34f);
            // a few striped mesas pushed up out of the desert, away from the artists and the heart
            var rng = new System.Random(1967); var made = 0;
            for (var t = 0; t < 400 && made < 7; t++)
            {
                var r = data.Nz * (float)rng.NextDouble(); var col = data.Nx * (float)rng.NextDouble();
                var x = (int)col * data.Step; var z = data.layout.depth - (int)r * data.Step; var p = new Vector2(x, z);
                if (data.IslandAt(x, z) != isl || Vector2.Distance(p, heart) < 90f) continue;
                var ok = true;
                foreach (var a in data.layout.artists) if (a.sceneId == isl.sceneId && Vector2.Distance(p, new Vector2(a.x, a.z)) < a.r + 40f) ok = false;
                for (var k = 0; k < 8 && ok; k++) { var an = k * Mathf.PI / 4f; if (data.IslandAt(x + Mathf.Cos(an) * 60f, z + Mathf.Sin(an) * 60f) != isl) ok = false; }
                if (!ok) continue;
                Mesa(data, index, p, 14f + (float)rng.NextDouble() * 12f, 12f + (float)rng.NextDouble() * 14f);
                made++;
            }
        }

        static void Mesa(IslandWorldData data, int index, Vector2 c, float rad, float h)
        {
            for (var r = 0; r < data.Nz; r++)
            {
                var z = data.layout.depth - r * data.Step; if (Mathf.Abs(z - c.y) > rad + 12f) continue;
                for (var col = 0; col < data.Nx; col++)
                {
                    var x = col * data.Step; if (Mathf.Abs(x - c.x) > rad + 12f) continue;
                    var i = r * data.Nx + col; if (data.islandIds[i] != index) continue;
                    var d = Vector2.Distance(new Vector2(x, z), c);
                    if (d < rad) data.heights[i] += h;
                    else if (d < rad + 12f) data.heights[i] += h * (1f - Mathf.SmoothStep(0, 1, (d - rad) / 12f));
                }
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
            life = new GameObject("Psychedelic life").AddComponent<PsychLife>();
            life.transform.SetParent(ctx.Root, false);
            life.center = center; life.capGlow = capGlow;
            var mk = new MeshKit(); var solid = new MeshKit();
            SpiralMushroom(ctx, mk, solid);
            Paths(ctx, mk, solid);
            Wilds(ctx, mk, solid);
            mk.Build("Wilds", ctx.Root, false);
            soft.Build("Soft wilds", ctx.Root, false);
            solid.BuildCollider("Wilds walls", ctx.Root);
        }

        public override void Finish(LandContext ctx) { GroundCover(ctx); halos.Build("Wilds glow", ctx.Root); }

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
            && Vector2.Distance(new Vector2(x, z), new Vector2(center.x, center.z)) > 26f + pad;

        /// A mushroom: a pale stalk, a domed cap in a bright colour with white spots, and a glowing underside.
        void Mushroom(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 at, float s, int colour)
        {
            var up = Vector3.up; var h = 3.2f * s; var capR = 2.2f * s;
            mk.Prism(stalk, at - up * .3f, up, .35f * s, h + .3f, 8, .8f);
            var top = at + up * h;
            soft.Blob(WorldMaterials.Solid(Rainbow[colour % Rainbow.Length], .5f), top + up * .45f * s, new Vector3(capR, .9f * s, capR), ctx.Rng, .1f);
            mk.Prism(capGlow[colour % capGlow.Length], top - up * .05f, up, capR * .92f, .1f, 14);
            for (var k = 0; k < 6; k++) { var d = Quaternion.Euler(0, k * 60f + ctx.Range(-20f, 20f), 0) * Vector3.forward; soft.Blob(white, top + up * (.75f * s) + d * capR * ctx.Range(.35f, .7f), new Vector3(.3f, .12f, .3f) * s, ctx.Rng, .2f); }
            ArtistBits.PostCollider(ctx.Root, at, h, .4f * s);
            noGrass.Add(new Vector3(at.x, capR * .5f, at.z));
        }

        void Strip(MeshKit mk, List<Vector3> pts, float w, float lift, int startColour)
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
            // painted in bands of colour, a few metres each, cycling through the rainbow
            for (var i = 0; i + 1 < n; i++) mk.Quad(WorldMaterials.Solid(Color.Lerp(Rainbow[(startColour + i / 2) % Rainbow.Length], Color.white, .25f), .2f), L[i], R[i], R[i + 1], L[i + 1], (pts[i] + pts[i + 1]) / 2f - Vector3.up);
        }

        // ---------------------------------------------------------------- where you arrive
        /// The spiral mushroom: a stalk that twists up forty metres, ringed with glowing bands, under a huge spotted
        /// cap whose glowing gills light the plaza; little mushrooms round its foot, a ring of benches.
        void SpiralMushroom(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var c = ctx.OnGround(center.x, center.z) + Vector3.Cross(up, Vector3.forward) * 12f;
            var segs = 16;
            for (var k = 0; k < segs; k++)
            {
                var y = k * 2.4f; var off = new Vector3(Mathf.Cos(k * .5f), 0, Mathf.Sin(k * .5f)) * 1.2f;
                var r = Mathf.Lerp(3.2f, 1.8f, k / (float)segs);
                mk.Prism(stalk, c + off + up * y - up * .2f, up, r, 2.7f, 12, .95f, k * 22f);
                mk.Prism(capGlow[k % capGlow.Length], c + off + up * (y + 1.2f), up, r + .08f, .35f, 12, 1f, k * 22f);
            }
            var top = c + new Vector3(Mathf.Cos(segs * .5f), 0, Mathf.Sin(segs * .5f)) * 1.2f + up * (segs * 2.4f);
            soft.Blob(WorldMaterials.Solid(new Color(.95f, .3f, .55f), .5f), top + up * 3f, new Vector3(16f, 6f, 16f), ctx.Rng, .08f);
            mk.Prism(capGlow[4], top + up * .2f, up, 14.5f, .3f, 24);
            for (var k = 0; k < 14; k++) { var d = Quaternion.Euler(0, k * 26f, 0) * Vector3.forward; soft.Blob(white, top + up * (4.6f + (k % 3) * .6f) + d * ctx.Range(5f, 12f), new Vector3(1.6f, .5f, 1.6f), ctx.Rng, .2f); }
            for (var k = 0; k < 24; k++) { var d = Quaternion.Euler(0, k * 15f, 0) * Vector3.forward; mk.Box(capGlow[k % capGlow.Length], top + d * 8f, new Vector3(.25f, .4f, 12f), Quaternion.LookRotation(d)); }
            halos.Add(top - up * 1f, 14f, new Color(.6f, .45f, 1f), .35f);
            ArtistBits.PointLight(ctx.Root, top - up * 3f, new Color(.8f, .6f, 1f), 40f, 2f);
            solid.Prism(stalk, c - up * .3f, up, 3.2f, 38f, 10);
            for (var k = 0; k < 10; k++) { var d = Quaternion.Euler(0, k * 36f + 10f, 0) * Vector3.forward; Mushroom(ctx, mk, solid, ctx.OnGround(c.x + d.x * ctx.Range(6f, 9f), c.z + d.z * ctx.Range(6f, 9f)), ctx.Range(.35f, .7f), k); }
            ArtistBits.Label(ctx.Root, "THE PSYCHEDELIC WILDS", c + up * 2.2f + Vector3.forward * 3.4f, Vector3.forward, .35f, new Color(.25f, .1f, .3f), TextAnchor.MiddleCenter);
            life.spiral = top;
            noGrass.Add(new Vector3(c.x, 12f, c.z));
            Remember(ctx.OnGround(center.x, center.z), 0);
        }

        // ---------------------------------------------------------------- paths
        void Paths(LandContext ctx, MeshKit mk, MeshKit solid)
        {
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
                for (var d = walk == 1 ? 10f : PathW / 2f + .1f; ; d += 2.5f)
                {
                    var dd = Mathf.Min(d, end);
                    var q = from + dir * dd + side * Mathf.Sin(dd / 18f) * Mathf.Min(4f, end * .05f);
                    if (!ctx.IsLand(q.x, q.z, 0f)) break;
                    pts.Add(ctx.OnGround(q.x, q.z)); if (dd >= end) break;
                }
                if (pts.Count < 2) continue;
                Strip(mk, pts, PathW, .05f, walk);
                foreach (var p in pts) Remember(p, walk);
                for (var i = 5; i < pts.Count; i += 9)
                {
                    var lp = pts[i] + side * ((i / 9) % 2 == 0 ? 1f : -1f) * (PathW / 2f + .9f);
                    var near = false; foreach (var o in lamps) if ((o - lp).sqrMagnitude < 22f * 22f) near = true;
                    if (near) continue;
                    lamps.Add(lp); LavaLamp(ctx, mk, ctx.OnGround(lp.x, lp.z), .5f, (i + walk) % Rainbow.Length);
                }
                walk++;
            }
        }

        /// A lava-lamp plant: a tall glass bulb on a chrome base, blobs of glowing colour floating in it.
        void LavaLamp(LandContext ctx, MeshKit mk, Vector3 g, float s, int colour)
        {
            var up = Vector3.up;
            mk.Prism(metal, g - up * .2f, up, .7f * s * 2f, 1.4f * s * 2f, 10, .6f);
            mk.Prism(WorldMaterials.Glow(Color.Lerp(Rainbow[colour], Color.white, .15f), 1f), g + up * 2.6f * s, up, .62f * s * 2f, 4f * s * 2f, 12, .55f);
            for (var k = 0; k < 4; k++) mk.Blob(GlowM(Rainbow[(colour + 3) % Rainbow.Length], 2.6f), g + up * (3.6f + k * 1.6f) * s + new Vector3(ctx.Range(-.3f, .3f), 0, ctx.Range(-.3f, .3f)) * s, Vector3.one * ctx.Range(.4f, .6f) * s * 2f, ctx.Rng, .2f);
            mk.Prism(metal, g + up * 10.6f * s, up, .5f * s, .6f * s, 10, .6f);
            halos.Add(g + up * 6f * s, 2f * s * 2f, Rainbow[colour], .6f);
            ArtistBits.PostCollider(ctx.Root, g, 11f * s, .6f * s * 2f);
        }

        // ---------------------------------------------------------------- the wilds
        /// The mushroom forest (thicker the further you go from the heart), crystal clusters, painted camper vans,
        /// lava-lamp plants, swirling spiral plants.
        void Wilds(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            int shrooms = 0, crystals = 0, vans = 0, lamps = 0, spirals = 0;
            const float cell = 26f;
            for (var x = ctx.MinX; x < ctx.MaxX; x += cell)
            for (var z = ctx.MinZ; z < ctx.MaxZ; z += cell)
            {
                float px = x + ctx.Range(0f, cell), pz = z + ctx.Range(0f, cell);
                if (!Open(ctx, px, pz, 4f)) continue;
                var p = ctx.OnGround(px, pz); var roll = ctx.Range(0f, 1f);
                var forest = Mathf.PerlinNoise(px * .006f + 3f, pz * .006f + 1f);
                if (roll < .25f + forest * .4f) { for (var k = 0; k < ctx.Rng.Next(2, 6); k++) Mushroom(ctx, mk, solid, ctx.OnGround(px + ctx.Range(-9f, 9f), pz + ctx.Range(-9f, 9f)), ctx.Range(.7f, 2.4f) * (.6f + forest), ctx.Rng.Next(Rainbow.Length)); shrooms++; }
                else if (roll < .72f) { Crystals(ctx, mk, solid, p); crystals++; }
                else if (roll < .78f && vans < 6 && NearPath(px, pz, 14f)) { Camper(ctx, mk, solid, p, vans); vans++; }
                else if (roll < .86f && lamps < 10) { LavaLamp(ctx, mk, p, ctx.Range(.6f, 1f), lamps % Rainbow.Length); lamps++; }
                else if (roll < .93f) { Spiral(ctx, mk, p); spirals++; }
                else continue;
                noGrass.Add(new Vector3(px, 5f, pz));
            }
            Debug.Log($"The Psychedelic Wilds: {shrooms} mushroom groups, {crystals} crystal clusters, {vans} camper vans, {lamps} lava lamps, {spirals} spiral plants");
        }

        void Crystals(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 at)
        {
            var c = Rainbow[ctx.Rng.Next(Rainbow.Length)];
            for (var k = 0; k < ctx.Rng.Next(3, 7); k++)
            {
                var d = (Vector3.up + new Vector3(ctx.Range(-.5f, .5f), 0, ctx.Range(-.5f, .5f))).normalized; var h = ctx.Range(1.2f, 3.5f);
                var p = at + new Vector3(ctx.Range(-1.2f, 1.2f), -.3f, ctx.Range(-1.2f, 1.2f));
                mk.Prism(WorldMaterials.Glow(Color.Lerp(c, Color.white, .3f), .6f), p, d, ctx.Range(.25f, .5f), h, 6, .5f);
                mk.Cone(WorldMaterials.Glow(Color.Lerp(c, Color.white, .5f), .9f), p + d * h, ctx.Range(.13f, .25f), .6f, 6);
            }
            halos.Add(at + Vector3.up * 1.5f, 1.8f, c, .5f);
            ArtistBits.PostCollider(ctx.Root, at, 2.5f, 1.2f);
        }

        void Camper(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 at, int k)
        {
            var up = Vector3.up; var dir = Quaternion.Euler(0, ctx.Range(0, 360f), 0) * Vector3.forward; var rot = Quaternion.LookRotation(dir); var side = Vector3.Cross(up, dir);
            var top = WorldMaterials.Solid(new Color(.96f, .94f, .88f), .5f);
            mk.Box(top, at + up * 2f, new Vector3(2f, 1.2f, 4.4f), rot);
            for (var b = 0; b < 6; b++) mk.Box(WorldMaterials.Solid(Rainbow[(b + k) % Rainbow.Length], .6f), at + up * (.7f + b * .14f) - dir * .1f, new Vector3(2.02f, .15f, 4.3f), rot);
            mk.Box(WorldMaterials.Solid(new Color(.2f, .3f, .4f), .9f), at + up * 2.1f + dir * 1.9f, new Vector3(1.8f, .8f, .6f), rot * Quaternion.Euler(-12f, 0, 0));
            mk.Prism(WorldMaterials.Solid(Rainbow[k % Rainbow.Length], .6f), at + up * 1.5f + dir * 2.21f, dir, .45f, .04f, 16);
            foreach (var s in new[] { -1f, 1f }) foreach (var z in new[] { -1.4f, 1.4f }) mk.Prism(dark, at + side * s * .95f + dir * z + up * .4f, side * s, .4f, .25f, 12);
            for (var f = 0; f < 5; f++) mk.Blob(WorldMaterials.Solid(Rainbow[(f + k + 2) % Rainbow.Length], .4f), at + up * (1f + (f % 2) * .6f) + side * 1.02f + dir * (-1.5f + f * .7f), new Vector3(.02f, .25f, .25f), ctx.Rng, .1f);
            solid.Box(top, at + up * 1.3f, new Vector3(2.1f, 2.6f, 4.6f), rot);
        }

        void Spiral(LandContext ctx, MeshKit mk, Vector3 at)
        {
            var c = Rainbow[ctx.Rng.Next(Rainbow.Length)]; var m = WorldMaterials.Solid(c, .5f);
            for (var k = 0; k < 18; k++)
            {
                var a = k * .55f; var r = 1.4f - k * .07f;
                var p = at + Vector3.up * (k * .28f) + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * r;
                soft.Blob(m, p, Vector3.one * (.32f - k * .012f), ctx.Rng, .1f);
            }
            soft.Blob(GlowM(Color.Lerp(c, Color.white, .4f), 2f), at + Vector3.up * 5.2f, Vector3.one * .35f, ctx.Rng, 0f);
        }

        /// Short desert grass, sparse, with bright little flowers.
        void GroundCover(LandContext ctx)
        {
            var mat = WindMesh.Material(.3f, 1f, new Color(.75f, .65f, .5f));
            var meshes = new Dictionary<(int, int), WindMesh>(); var rng = new System.Random(1969);
            const float step = 2.6f;
            for (var x = ctx.MinX; x < ctx.MaxX; x += step)
            for (var z = ctx.MinZ; z < ctx.MaxZ; z += step)
            {
                var px = x + ((float)rng.NextDouble() - .5f) * step; var pz = z + ((float)rng.NextDouble() - .5f) * step;
                if (!ctx.IsLand(px, pz, 1f) || NearPath(px, pz, 2.6f) || InNoGrass(px, pz, 0f) || InFootprint(ctx, px, pz, -2f)) continue;
                if (Mathf.PerlinNoise(px * .03f + 2f, pz * .03f + 7f) < .42f) continue;
                var key = (Mathf.FloorToInt(px / 70f), Mathf.FloorToInt(pz / 70f));
                if (!meshes.TryGetValue(key, out var wm)) meshes[key] = wm = new WindMesh();
                var col = Color.Lerp(new Color(.6f, .7f, .4f), new Color(.85f, .7f, .45f), (float)rng.NextDouble());
                var at = new Vector3(px, ctx.Ground(px, pz) - .04f, pz);
                wm.Tuft(at, .3f + (float)rng.NextDouble() * .3f, col, Color.Lerp(col, Color.white, .3f), rng, 3, .07f);
                if (rng.NextDouble() < .12) wm.Flower(at + new Vector3(.1f, 0, .1f), .35f, Rainbow[rng.Next(Rainbow.Length)], rng);
            }
            foreach (var kv in meshes) kv.Value.Build("Wilds grass", ctx.Root, mat);
        }
    }
}
