using System.Collections.Generic;
using UnityEngine;
using static EverythingWorld.World.PlaceTools;

namespace EverythingWorld.World
{
    /// The Soul Lowlands (WORLD_GUIDE.md): a wooden river town at golden hour, warm and joyful. You arrive in the
    /// town square: The Royal, a theatre with a lit marquee of chasing bulbs, stands on one side with a white chapel
    /// beside it, doors open, the choir inside. Main Street runs from the square down toward the river between
    /// pastel shopfronts under strings of bulbs (a record shop and a diner to walk into, a barbershop, a radio
    /// station on the air), then River Road carries on under oaks to the landing, where a paddle steamer is moored
    /// with her wheel turning. Out in the meadows: oaks and magnolias, pastel houses with porch swings, brick-red
    /// lanes with globe lamps. Long shadows, honey light, blue overhead.
    /// The town is laid out on purpose (the build-land standard): everything is on a street, every place has
    /// somewhere to sit, something to use, something to carry and its people (SoulTown.cs).
    public partial class SoulKit : LandKit
    {
        public override string Land => "The Soul Lowlands";
        public override Vector3 ArtistFront(IslandWorldData data, IslandWorldData.Artist a) => facing.TryGetValue(a.name, out var f) ? f : base.ArtistFront(data, a);

        public static readonly Color Gold = new Color(1f, .78f, .35f), Velvet = new Color(.62f, .12f, .2f), River = new Color(.3f, .5f, .68f), Cream = new Color(.97f, .93f, .82f), Ink = new Color(.16f, .1f, .08f);
        static readonly Color[] Pastel = { new Color(.62f, .82f, .74f), new Color(.98f, .78f, .62f), new Color(.66f, .8f, .93f), new Color(.98f, .9f, .6f), new Color(.93f, .68f, .7f), new Color(.8f, .74f, .92f) };
        const float PathW = 3.2f, StreetW = 9f, StreetFrom = 28f, StreetTo = 138f;

        Material brick, brickDark, wood, woodDark, white, iron, dark, gold, velvet, cream, boardM, lampM, windowM, waterBlue;
        readonly Material[] bulbM = new Material[3];
        GlowHalos halos; Vector3 center, E, N; SoulLife life;
        readonly Dictionary<(int, int), List<Vector3>> pathCells = new Dictionary<(int, int), List<Vector3>>();
        readonly List<(Vector3 p, int w)> walkPoints = new List<(Vector3, int)>();
        readonly Dictionary<string, Vector3> facing = new Dictionary<string, Vector3>();
        readonly List<Vector3> noGrass = new List<Vector3>();   // x, radius, z
        Vector2 heart2, E2, N2, theatre2, chapel2, landing2; float riverDist, waterY; bool hasRiver;

        void MakeMaterials()
        {
            brick = WorldMaterials.Solid(new Color(.74f, .47f, .36f)); brickDark = WorldMaterials.Solid(new Color(.52f, .3f, .23f));
            wood = WorldMaterials.Solid(new Color(.6f, .42f, .27f)); woodDark = WorldMaterials.Solid(new Color(.36f, .24f, .16f)); boardM = WorldMaterials.Solid(new Color(.7f, .56f, .4f));
            white = WorldMaterials.Solid(new Color(.97f, .96f, .92f)); cream = WorldMaterials.Solid(Cream); iron = WorldMaterials.Solid(new Color(.14f, .13f, .13f), .5f); dark = WorldMaterials.Solid(new Color(.08f, .07f, .07f));
            gold = WorldMaterials.Solid(new Color(.85f, .64f, .22f), .6f); velvet = WorldMaterials.Solid(Velvet); waterBlue = WorldMaterials.Solid(River, .7f);
            lampM = new Material(WorldMaterials.Glow(Gold, 1.5f)) { name = "Soul lamps" };
            windowM = new Material(WorldMaterials.Glow(new Color(1f, .82f, .5f), 1.1f)) { name = "Soul windows" };
            for (var i = 0; i < 3; i++) bulbM[i] = new Material(WorldMaterials.Glow(new Color(1f, .86f, .55f), 1.6f)) { name = "Soul bulbs " + i };
        }

        Material GlowM(Color c, float s = 2f) => WorldMaterials.Glow(c, s * .65f);
        Material Paint(int i) => WorldMaterials.Solid(Pastel[((i % Pastel.Length) + Pastel.Length) % Pastel.Length]);
        static bool HasStation(IslandWorldData.Artist a) => a.stationId != 0;
        static float Foot(IslandWorldData.Artist a)
        {
            switch (a.name)
            {
                case "Stevie Wonder": return 17f;
                case "Otis Redding": return 13f;
                case "Ray Charles": return 12f;
                case "Marvin Gaye": return 11f;
                case "Sam Cooke": case "Ben E. King": return 10f;
                default: return HasStation(a) ? 12f : 8f;
            }
        }

        public override bool Ground(float height, float slope, float patch, out Color color)
        {
            if (height < waterY + 2.1f) color = Color.Lerp(new Color(.86f, .76f, .54f), new Color(.92f, .84f, .62f), patch);                 // the sandy river shore
            else if (slope > 20f) color = Color.Lerp(new Color(.56f, .36f, .24f), new Color(.64f, .42f, .28f), patch);                     // red-brown earth banks
            else color = Color.Lerp(new Color(.46f, .56f, .24f), new Color(.7f, .66f, .3f), Mathf.SmoothStep(0f, 1f, patch));               // warm meadow, green going to gold
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
            bool Firm(Vector2 p) => data.IslandAt(p.x, p.y) == isl && data.HeightAt(p.x, p.y) > waterY + 1.3f;
            bool Inside(Vector2 p, float m) { if (!Firm(p)) return false; for (var k = 0; k < 12; k++) { var a = k * Mathf.PI / 6f; if (data.IslandAt(p.x + Mathf.Cos(a) * m, p.y + Mathf.Sin(a) * m) != isl) return false; } return true; }
            // the river is the water to the east: Main Street runs toward the nearest shore that way
            E2 = new Vector2(1, 0); riverDist = 1e9f; hasRiver = false;
            for (var ang = -50f; ang <= 50f; ang += 5f)
            {
                var dir = new Vector2(Mathf.Cos(ang * Mathf.Deg2Rad), Mathf.Sin(ang * Mathf.Deg2Rad));
                for (var d = 60f; d < 520f; d += 4f)
                {
                    var p = heart2 + dir * d;
                    if (Firm(p)) continue;
                    if (data.IslandAt(p.x, p.y) != isl && data.HeightAt(p.x, p.y) > waterY + 1.3f) break;   // another land, not water
                    if (d < riverDist) { riverDist = d; E2 = dir; hasRiver = true; }
                    break;
                }
            }
            if (!hasRiver) riverDist = 240f;
            riverDist = Mathf.Max(riverDist, StreetTo + 40f);
            N2 = new Vector2(-E2.y, E2.x);
            theatre2 = heart2 - E2 * 48f; chapel2 = heart2 + N2 * 56f - E2 * 16f; landing2 = heart2 + E2 * (riverDist - 8f);

            // where the people who matter most live: Stevie Wonder south of the square, Otis Redding on the shore (his
            // dock goes out over the water), the gospel singers round the chapel
            var keep = new List<string>();
            void Put(string name, Vector2 p, float room) { if (!Inside(p, room)) return; foreach (var a in data.layout.artists) if (a.sceneId == isl.sceneId && a.name == name) { a.x = p.x; a.z = p.y; keep.Add(name); } }
            Put("Stevie Wonder", heart2 - N2 * 92f + E2 * 6f, 40f);
            if (hasRiver)
            {   // along the shore north of the landing: walk east from there until the water, then step back
                var from = heart2 + N2 * 84f + E2 * 80f; var d = 0f; while (d < 420f && Firm(from + E2 * (d + 4f))) d += 4f;
                Put("Otis Redding", from + E2 * Mathf.Max(0f, d - 15f), 6f);
            }
            Put("Kirk Franklin", chapel2 + N2 * 50f - E2 * 6f, 34f); Put("The Staple Singers", chapel2 + N2 * 26f + E2 * 50f, 34f); Put("Mahalia Jackson", chapel2 - E2 * 50f + N2 * 22f, 34f);
            Put("The Edwin Hawkins Singers", chapel2 + N2 * 74f - E2 * 46f, 34f); Put("Jubalaires", chapel2 + N2 * 86f + E2 * 34f, 34f);

            var avoid = new List<Vector3> { new Vector3(theatre2.x, 30f, theatre2.y), new Vector3(chapel2.x, 26f, chapel2.y), new Vector3(landing2.x, 30f, landing2.y) };
            for (var d = StreetFrom; d <= riverDist - 20f; d += 18f) avoid.Add(new Vector3(heart2.x + E2.x * d, d <= StreetTo + 6f ? 27f : 12f, heart2.y + E2.y * d));
            ArtistPlacement.Spread(data, index, 56f, heart2, 44f, 42f, avoid, keep, Land, 330f);

            // level the town: the square, the theatre and chapel, Main Street step by step, the landing
            Level(data, index, heart2, 30f, 46f);
            var squareY = data.HeightAt(heart2.x, heart2.y);
            LevelTo(data, index, theatre2, 24f, 38f, squareY + .3f); Level(data, index, chapel2, 17f, 32f);
            for (var d = StreetFrom + 6f; d <= StreetTo; d += 11f) Level(data, index, heart2 + E2 * d, 20f, 32f);
            foreach (var a in data.layout.artists)
            {
                if (a.sceneId != isl.sceneId) continue;
                a.r = Mathf.Max(a.r, Foot(a) + 6f);
                Level(data, index, new Vector2(a.x, a.z), Foot(a) + 1f, Foot(a) + 10f);
            }
        }

        void Level(IslandWorldData data, int index, Vector2 c, float flat, float blend) => LevelTo(data, index, c, flat, blend, Mathf.Max(data.HeightAt(c.x, c.y), data.layout.waterLevel + 2.6f));

        void LevelTo(IslandWorldData data, int index, Vector2 c, float flat, float blend, float level)
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
            center = ctx.Plaza; E = new Vector3(E2.x, 0, E2.y); N = new Vector3(N2.x, 0, N2.y);
            life = new GameObject("Soul life").AddComponent<SoulLife>();
            life.transform.SetParent(ctx.Root, false);
            life.center = center; life.lamps = lampM; life.windows = windowM; life.bulbs = bulbM;
            var mk = new MeshKit(); var solid = new MeshKit();
            Remember(ctx.OnGround(center.x, center.z), 0);
            TownStart(ctx);
            Square(ctx, mk, solid);
            Theatre(ctx, mk, solid);
            Chapel(ctx, mk, solid);
            MainStreet(ctx, mk, solid);
            RiverRoad(ctx, mk, solid);
            Paths(ctx, mk, solid);
            Wilds(ctx, mk, solid);
            mk.Build("The Soul Lowlands", ctx.Root, false);
            solid.BuildCollider("The Soul Lowlands walls", ctx.Root);
        }

        public override void Finish(LandContext ctx) { Meadow(ctx); halos.Build("Lowlands glow", ctx.Root); }

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

        /// A black iron lamp post with a warm globe (all the globes are one material: the life script plays with them).
        void GlobeLamp(LandContext ctx, MeshKit mk, Vector3 g, bool light = false)
        {
            var up = Vector3.up; mk.Prism(iron, g - up * .2f, up, .09f, 3.9f, 6); mk.Prism(iron, g, up, .2f, .3f, 8, .5f); mk.Prism(iron, g + up * 3.6f, up, .16f, .1f, 8);
            mk.Blob(lampM, g + up * 4f, Vector3.one * .3f, new System.Random(7), 0f); halos.Add(g + up * 4f, 2f, Gold, .6f); ArtistBits.PostCollider(ctx.Root, g, 3.8f, .12f);
            if (light) ArtistBits.PointLight(ctx.Root, g + up * 3.6f, Gold, 15f, 1.5f);
        }

        /// A string of bulbs from a to b, sagging; the bulbs take the three bulb materials in turn so they can chase.
        void BulbString(MeshKit mk, Vector3 a, Vector3 b, int phase = 0)
        {
            var len = Vector3.Distance(a, b); var n = Mathf.Max(3, Mathf.RoundToInt(len / 1.1f)); var sag = Mathf.Min(1.4f, len * .08f); var prev = a;
            for (var i = 1; i <= n; i++)
            {
                var t = i / (float)n; var p = Vector3.Lerp(a, b, t) - Vector3.up * Mathf.Sin(t * Mathf.PI) * sag;
                mk.Prism(dark, prev, (p - prev).normalized, .012f, Vector3.Distance(prev, p), 3); prev = p;
                if (i < n) { mk.Blob(bulbM[(i + phase) % 3], p - Vector3.up * .08f, Vector3.one * .075f, new System.Random(i), 0f); if (i % 4 == 0) halos.Add(p - Vector3.up * .08f, .7f, Gold, .35f); }
            }
        }

        /// A spreading oak: a short thick trunk, a broad canopy catching the gold light.
        void Oak(LandContext ctx, MeshKit mk, Vector3 g, float s, System.Random rng)
        {
            var up = Vector3.up; var bark = WorldMaterials.Solid(new Color(.34f, .25f, .18f));
            mk.Prism(bark, g - up * .3f, up, .34f * s, 3.2f * s, 7, .7f); ArtistBits.PostCollider(ctx.Root, g, 3f * s, .36f * s);
            for (var k = 0; k < 4; k++)
            {
                var a = k * 90f + (float)rng.NextDouble() * 40f; var d = Quaternion.Euler(0, a, 0) * Vector3.forward;
                var leaf = WorldMaterials.Solid(Color.Lerp(new Color(.36f, .5f, .2f), new Color(.62f, .62f, .22f), (float)rng.NextDouble()));
                mk.Blob(leaf, g + up * (4.2f * s + (float)rng.NextDouble() * .8f) + d * 1.9f * s, new Vector3(2.6f, 1.7f, 2.6f) * s, rng, .22f);
            }
            mk.Blob(WorldMaterials.Solid(new Color(.5f, .58f, .22f)), g + up * 5.4f * s, new Vector3(2.8f, 1.8f, 2.8f) * s, rng, .2f);
        }

        /// A magnolia: dark glossy leaves, big white blossoms.
        void Magnolia(LandContext ctx, MeshKit mk, Vector3 g, float s, System.Random rng)
        {
            var up = Vector3.up; mk.Prism(WorldMaterials.Solid(new Color(.4f, .32f, .26f)), g - up * .3f, up, .2f * s, 2.6f * s, 6, .7f); ArtistBits.PostCollider(ctx.Root, g, 2.4f * s, .22f * s);
            var leaf = WorldMaterials.Solid(new Color(.2f, .38f, .22f)); var c = g + up * 3.8f * s; mk.Blob(leaf, c, new Vector3(1.9f, 2.1f, 1.9f) * s, rng, .2f);
            for (var k = 0; k < 9; k++) { var d = Quaternion.Euler((float)rng.NextDouble() * 120f - 60f, k * 40f, 0) * Vector3.forward; mk.Blob(white, c + Vector3.Scale(d, new Vector3(1.8f, 2f, 1.8f) * s), Vector3.one * .26f * s, rng, .15f); }
        }

        /// A wooden park bench you can sit on.
        void TownBench(MeshKit mk, Vector3 g, Vector3 face, float len = 2f)
        {
            var up = Vector3.up; var rot = Quaternion.LookRotation(face); var side = Vector3.Cross(up, face);
            mk.Box(wood, g + up * .45f, new Vector3(len, .07f, .5f), rot); mk.Box(wood, g + up * .82f - face * .24f, new Vector3(len, .4f, .06f), rot); foreach (var e in new[] { -1f, 1f }) mk.Box(iron, g + up * .32f + side * e * (len / 2f - .12f) - face * .08f, new Vector3(.06f, .95f, .5f), rot);
            BoxCol(rootT, g + up * .5f, new Vector3(len, 1f, .6f), rot); Interact.Bench(tlife.transform, g + up * .52f - side * (len / 2f - .4f), g + up * .52f + side * (len / 2f - .4f), face);
        }

        // ---------------------------------------------------------------- the lanes to the artists
        void Paths(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var walk = 10; var lamps = new List<Vector3>(); var earth = WorldMaterials.Solid(new Color(.86f, .74f, .52f));   // pale sandy lanes, against the brick of the town
            void Lay(Vector3 join, Vector3 to, float stopShort, string artist)
            {
                var from = new Vector3(join.x, 0, join.z); to.y = 0;
                var dir = to - from; var total = dir.magnitude; if (total < 2f) return; dir /= total;
                if (artist != null) facing[artist] = -dir;
                var side = Vector3.Cross(Vector3.up, dir); var end = total - stopShort;
                var pts = new List<Vector3>();
                for (var d = PathW / 2f + .1f; ; d += 1.5f)
                {
                    var dd = Mathf.Min(d, end); var q = from + dir * dd + side * Mathf.Sin(dd / 14f) * Mathf.Min(2.5f, end * .04f);
                    if (!ctx.IsLand(q.x, q.z, 0f)) break;
                    pts.Add(ctx.OnGround(q.x, q.z)); if (dd >= end) break;
                }
                if (pts.Count < 2) return;
                Strip(mk, pts, earth, PathW, .14f, ctx);
                foreach (var p in pts) Remember(p, walk);
                for (var i = 8; i < pts.Count; i += 14)
                {
                    var lp = pts[i] + side * ((i / 14) % 2 == 0 ? 1f : -1f) * (PathW / 2f + .8f);
                    var near = false; foreach (var o in lamps) if ((o - lp).sqrMagnitude < 18f * 18f) near = true;
                    if (near || InFootprint(ctx, lp.x, lp.z, 1f) || InNoGrass(lp.x, lp.z, 1f)) continue;
                    lamps.Add(lp); GlobeLamp(ctx, mk, ctx.OnGround(lp.x, lp.z));
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

        // ---------------------------------------------------------------- the meadows
        /// Out of town: spreading oaks and magnolias, pastel houses with a porch swing and a lit window, picket gardens,
        /// picnic tables, benches under the trees. Laid on a jittered grid so the whole land is filled.
        void Wilds(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            int oaks = 0, mags = 0, houses = 0, tables = 0, benches = 0; var rng = new System.Random(1962);
            const float cell = 23f;
            for (var x = ctx.MinX; x < ctx.MaxX; x += cell)
            for (var z = ctx.MinZ; z < ctx.MaxZ; z += cell)
            {
                float px = x + (float)rng.NextDouble() * cell, pz = z + (float)rng.NextDouble() * cell; var roll = (float)rng.NextDouble(); var yaw = (float)rng.NextDouble() * 360f;
                if (!Open(ctx, px, pz, 4f)) continue;
                var p = ctx.OnGround(px, pz); var face = Quaternion.Euler(0, yaw, 0) * Vector3.forward;
                if (roll < .17f) { Oak(ctx, mk, p, .9f + (float)rng.NextDouble() * .5f, rng); oaks++; }
                else if (roll < .26f) { Magnolia(ctx, mk, p, .9f + (float)rng.NextDouble() * .4f, rng); mags++; }
                else if (roll < .38f) { if (houses < 18 && Open(ctx, px, pz, 9f)) { House(ctx, mk, solid, p, FaceTo(p, NearestWalk(p)), houses, rng); houses++; } }
                else if (roll < .40f) { PicnicTable(mk, p, face); tables++; }
                else if (roll < .43f) { TownBench(mk, p, FaceTo(p, NearestWalk(p))); benches++; }
            }
            Debug.Log($"The Soul Lowlands: river {(hasRiver ? "yes" : "no")} at {riverDist:0} m, {oaks} oaks, {mags} magnolias, {houses} houses, {tables} picnic tables, {benches} benches");
        }

        Vector3 NearestWalk(Vector3 p) { var best = center; var bd = float.MaxValue; foreach (var (q, _) in walkPoints) { var d = (q - p).sqrMagnitude; if (d < bd) { bd = d; best = q; } } return best; }

        /// A small pastel wooden house: a porch across the front with a swing you can sit on, a lit window, a tin roof.
        void House(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 g, Vector3 face, int i, System.Random rng)
        {
            var up = Vector3.up; var f = new PlaceFrame(g + up * .3f, face); var paint = Paint(i); const float W = 5.4f, D = 6.4f, H = 2.9f;
            mk.Box(brickDark, g + up * .05f - f.Fwd * .2f, new Vector3(W + .4f, .9f, D + 3f), f.r); solid.Box(brickDark, g + up * .05f - f.Fwd * .2f, new Vector3(W + .4f, .9f, D + 3f), f.r);
            mk.Box(paint, f.P(0, H / 2f + .2f, -1.2f), new Vector3(W, H, D), f.r); solid.Box(paint, f.P(0, H / 2f + .2f, -1.2f), new Vector3(W, H, D), f.r);
            foreach (var e in new[] { -1f, 1f }) mk.Box(WorldMaterials.Solid(new Color(.5f, .52f, .54f), .5f), f.P(e * W / 4f, H + .95f, -.5f), new Vector3(W / 2f + .5f, .1f, D + 3.2f), f.r * Quaternion.Euler(0, 0, -e * 28f));
            mk.Prism(paint, f.P(0, H + .2f, D / 2f - 1.25f), f.Fwd, W / 2f, .08f, 3); mk.Prism(paint, f.P(0, H + .2f, -D / 2f - 1.25f), f.Fwd, W / 2f, .08f, 3);
            mk.Box(white, f.P(-1.2f, 1.25f, D / 2f - 1.18f), new Vector3(.95f, 2.1f, .06f), f.r); mk.Box(windowM, f.P(1.2f, 1.7f, D / 2f - 1.18f), new Vector3(1.2f, 1.2f, .06f), f.r); mk.Box(white, f.P(1.2f, 1.7f, D / 2f - 1.16f), new Vector3(.06f, 1.2f, .06f), f.r);
            foreach (var e in new[] { -1f, 1f }) { mk.Box(white, f.P(e * (W / 2f - .15f), 1.3f, D / 2f + .5f), new Vector3(.14f, 2.3f, .14f), f.r); ArtistBits.PostCollider(ctx.Root, f.P(e * (W / 2f - .15f), 0, D / 2f + .5f), 2.4f, .1f); }
            halos.Add(f.P(1.2f, 1.7f, D / 2f - .7f), 1.5f, Gold, .4f);
            // the porch swing
            var sw = f.P(1.3f, .55f, D / 2f - .3f); mk.Box(white, sw, new Vector3(1.6f, .06f, .5f), f.r); mk.Box(white, sw + up * .35f - f.Fwd * .24f, new Vector3(1.6f, .5f, .05f), f.r); foreach (var e in new[] { -.75f, .75f }) mk.Box(iron, sw + f.Right * e + up * 1.05f, new Vector3(.02f, 2f, .02f), f.r);
            Interact.Bench(tlife.transform, sw + up * .06f - f.Right * .45f, sw + up * .06f + f.Right * .45f, f.Fwd);
            // a step down, a picket fence each side of the path
            mk.Box(wood, f.P(-1.2f, -.05f, D / 2f + 1.5f), new Vector3(1.4f, .3f, .5f), f.r);
            for (var k = 0; k < 14; k++) { var x = -W / 2f - .6f + k * .55f; if (Mathf.Abs(x + 1.2f) < .9f) continue; mk.Box(white, G0(f.P(x, 0, D / 2f + 3.2f)) + up * .45f, new Vector3(.1f, .8f, .04f), f.r); }
            noGrass.Add(new Vector3(g.x, 6f, g.z));
            if (i % 3 == 0) Folk(G0(f.P(-1.2f, 0, D / 2f + 2.2f)), f.Fwd, Resident.Pose.Wave, PorchTalk);
            else if (i % 3 == 1) Folk(sw + up * .06f + f.Right * .45f, f.Fwd, Resident.Pose.Sit, PorchTalk, null, null, .6f);
            Magnolia(ctx, mk, G0(f.P(-W / 2f - 2.4f, 0, D / 2f + 1.5f)), .8f, rng);
        }

        void PicnicTable(MeshKit mk, Vector3 g, Vector3 face)
        {
            var up = Vector3.up; var rot = Quaternion.LookRotation(face); var side = Vector3.Cross(up, face);
            mk.Box(wood, g + up * .76f, new Vector3(1.9f, .06f, .8f), rot); mk.Box(WorldMaterials.Solid(new Color(.85f, .25f, .25f)), g + up * .795f, new Vector3(1.5f, .01f, .82f), rot);
            foreach (var e in new[] { -1f, 1f }) { mk.Box(wood, g + up * .45f + face * e * .75f, new Vector3(1.9f, .06f, .3f), rot); Interact.Bench(tlife.transform, g + up * .5f + face * e * .75f - side * .5f, g + up * .5f + face * e * .75f + side * .5f, -face * e); mk.Box(woodDark, g + up * .38f + side * e * .75f, new Vector3(.08f, .76f, 1.7f), rot); }
            BoxCol(rootT, g + up * .4f, new Vector3(1.9f, .8f, 1.8f), rot);
        }

        Vector3 G0(Vector3 p) => ctx0.OnGround(p.x, p.z);

        /// The meadow: tall grass going gold, wildflowers in yellow, white and red.
        void Meadow(LandContext ctx)
        {
            var mat = WindMesh.Material(.32f, 1f, new Color(.6f, .62f, .3f));
            var meshes = new Dictionary<(int, int), WindMesh>(); var rng = new System.Random(1971);
            Color[] flowers = { new Color(1f, .85f, .3f), new Color(.98f, .96f, .9f), new Color(.85f, .25f, .3f), new Color(1f, .6f, .35f) };
            const float st = 2.6f;
            for (var x = ctx.MinX; x < ctx.MaxX; x += st)
            for (var z = ctx.MinZ; z < ctx.MaxZ; z += st)
            {
                var px = x + ((float)rng.NextDouble() - .5f) * st; var pz = z + ((float)rng.NextDouble() - .5f) * st;
                if (!ctx.IsLand(px, pz, 1f) || ctx.Ground(px, pz) < waterY + 2.2f || NearPath(px, pz, 2.6f) || InNoGrass(px, pz, 0f) || InFootprint(ctx, px, pz, -2f)) continue;
                var n = Mathf.PerlinNoise(px * .035f + 3f, pz * .035f + 8f); if (n < .34f) continue;
                var key = (Mathf.FloorToInt(px / 70f), Mathf.FloorToInt(pz / 70f));
                if (!meshes.TryGetValue(key, out var wm)) meshes[key] = wm = new WindMesh();
                var col = Color.Lerp(new Color(.44f, .56f, .24f), new Color(.82f, .72f, .32f), (float)rng.NextDouble() * n * 1.3f);
                var flower = rng.NextDouble() < .16;
                wm.Tuft(new Vector3(px, ctx.Ground(px, pz) - .04f, pz), .3f + (float)rng.NextDouble() * .4f, col, flower ? flowers[rng.Next(flowers.Length)] : Color.Lerp(col, new Color(.95f, .86f, .5f), .4f), rng, 3, .05f);
            }
            foreach (var kv in meshes) kv.Value.Build("Lowlands meadow", ctx.Root, mat);
        }
    }
}
