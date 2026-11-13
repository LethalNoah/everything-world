using System.Collections.Generic;
using UnityEngine;
using static EverythingWorld.World.PlaceTools;

namespace EverythingWorld.World
{
    /// The Far Shores (WORLD_GUIDE.md): a bright blue tropical afternoon on a coast of many countries. You arrive at
    /// the Compass, a mosaic compass rose under a signpost of far-off cities. West, the Promenade runs downhill to the
    /// sea: a wide walk of black and white waves between two rows of palms, with a port of call for each artist on
    /// either side, and at the bottom a white beach, umbrellas, and a pier with a banana boat. East, the green gives
    /// out and the Caravan Track goes off into golden dunes, past cairns and camels, to a camp of tents round a fire.
    /// Laid out on purpose (the build-land standard): FarTown.cs has the places and the people.
    public partial class FarKit : LandKit
    {
        public override string Land => "The Far Shores";
        public override Vector3 ArtistFront(IslandWorldData data, IslandWorldData.Artist a) => facing.TryGetValue(a.name, out var f) ? f : base.ArtistFront(data, a);

        public static readonly Color Teal = new Color(.18f, .65f, .63f), Ochre = new Color(.84f, .64f, .33f), Coral = new Color(.94f, .48f, .35f), Palm = new Color(.3f, .54f, .23f), Sun = new Color(1f, .86f, .4f), Cream = new Color(.98f, .95f, .86f), Ink = new Color(.1f, .1f, .12f), Indigo = new Color(.2f, .24f, .5f);
        static readonly Color[] Paints = { new Color(.18f, .65f, .63f), new Color(.94f, .48f, .35f), new Color(1f, .8f, .3f), new Color(.96f, .94f, .88f), new Color(.3f, .5f, .8f), new Color(.9f, .4f, .55f), new Color(.4f, .66f, .36f) };
        const float PathW = 3f, PromW = 9f;

        Material tileW, tileB, sandM, duneM, stone, woodM, bleached, thatch, iron, dark, white, trunkM, leafM, leafDark, clothM, fireM, glassM, lampM;
        GlowHalos halos; Vector3 center, U, R; FarLife life;
        readonly Dictionary<(int, int), List<Vector3>> pathCells = new Dictionary<(int, int), List<Vector3>>();
        readonly List<(Vector3 p, int w)> walkPoints = new List<(Vector3, int)>();
        readonly Dictionary<string, Vector3> facing = new Dictionary<string, Vector3>();
        // x, radius, z
        readonly List<Vector3> noGrass = new List<Vector3>();
        Vector2 heart2, U2 = new Vector2(0, 1), R2 = new Vector2(1, 0), beach2, camp2; float plazaY, waterY, roadL, trackL;

        void MakeMaterials()
        {
            tileW = WorldMaterials.Solid(new Color(.9f, .88f, .82f)); tileB = WorldMaterials.Solid(new Color(.16f, .17f, .2f)); sandM = WorldMaterials.Solid(new Color(.92f, .86f, .7f)); duneM = WorldMaterials.Solid(new Color(.86f, .72f, .46f));
            stone = WorldMaterials.Solid(new Color(.8f, .76f, .68f)); woodM = WorldMaterials.Solid(new Color(.56f, .4f, .26f)); bleached = WorldMaterials.Solid(new Color(.8f, .74f, .62f)); thatch = WorldMaterials.Solid(new Color(.78f, .64f, .36f));
            iron = WorldMaterials.Solid(new Color(.16f, .16f, .18f), .5f); dark = WorldMaterials.Solid(new Color(.08f, .08f, .1f)); white = WorldMaterials.Solid(new Color(.97f, .96f, .93f));
            trunkM = WorldMaterials.Solid(new Color(.56f, .46f, .36f)); leafM = WorldMaterials.Solid(new Color(.26f, .56f, .26f)); leafDark = WorldMaterials.Solid(new Color(.16f, .42f, .26f));
            clothM = WorldMaterials.Solid(Indigo); glassM = WorldMaterials.Solid(new Color(.6f, .8f, .86f), .9f);
            fireM = new Material(WorldMaterials.Glow(new Color(1f, .55f, .12f), 2.2f)) { name = "Far fires" }; lampM = new Material(WorldMaterials.Glow(new Color(1f, .95f, .8f), .9f)) { name = "Far lamps" };
        }

        Material GlowM(Color c, float s = 2f) => WorldMaterials.Glow(c, s * .65f);
        Material Paint(Color c) => WorldMaterials.Solid(c);
        static bool HasStation(IslandWorldData.Artist a) => a.stationId != 0;
        static float Foot(IslandWorldData.Artist a)
        {
            switch (a.name)
            {
                case "Tinariwen": return 16f;
                case "Jorge Ben Jor": return 14f;
                case "Mulatu Astatke": return 13f;
                default: return HasStation(a) ? 12f : 9.5f;
            }
        }

        public override bool Ground(float height, float slope, float patch, out Color color)
        {
            // white sand by the sea, green on the slope, golden dunes on the high ground inland
            if (height < 3.4f) color = Color.Lerp(new Color(.9f, .84f, .68f), new Color(.86f, .78f, .62f), patch);
            else if (slope > 28f) color = Color.Lerp(new Color(.7f, .56f, .36f), new Color(.78f, .62f, .4f), patch);
            else
            {
                var dune = Mathf.Clamp01((height - duneFrom) / 5f); var green = Color.Lerp(new Color(.4f, .66f, .32f), new Color(.56f, .72f, .36f), Mathf.SmoothStep(0f, 1f, patch)); var gold = Color.Lerp(new Color(.94f, .84f, .6f), new Color(.9f, .78f, .52f), patch);
                color = Color.Lerp(green, gold, Mathf.Clamp01(dune + (patch > .7f ? .25f : 0f)));
            }
            color.a = 1f;
            return true;
        }

        static float duneFrom = 14f;
        static float Ease(float a, float b, float x) { var t = Mathf.Clamp01((x - a) / (b - a)); return t * t * (3f - 2f * t); }
        Vector2 P2(float u, float r) => heart2 + U2 * u + R2 * r;
        Vector3 Wp(float u, float r) => center + U * u + R * r;
        Vector3 G(LandContext ctx, float u, float r) { var p = Wp(u, r); return ctx.OnGround(p.x, p.z); }

        // ---------------------------------------------------------------- shaping the ground
        public override void ShapeGround(IslandWorldData data, int index)
        {
            var isl = data.layout.islands[index]; heart2 = new Vector2(isl.labelX, isl.labelZ); waterY = data.layout.waterLevel;
            bool Inside(Vector2 p, float m) { if (data.IslandAt(p.x, p.y) != isl) return false; for (var k = 0; k < 12; k++) { var a = k * Mathf.PI / 6f; if (data.IslandAt(p.x + Mathf.Cos(a) * m, p.y + Mathf.Sin(a) * m) != isl) return false; } return true; }
            // the Promenade runs west, downhill, until it reaches the sand; the Caravan Track runs east into the dunes
            roadL = 60f; while (roadL < 300f && data.IslandAt(heart2.x - roadL - 34f, heart2.y) == isl && data.HeightAt(heart2.x - roadL, heart2.y) > waterY + 3f) roadL += 6f;
            beach2 = heart2 - R2 * (roadL + 16f);
            trackL = 60f; while (trackL < 170f && Inside(heart2 + R2 * (trackL + 10f) - U2 * 14f, 46f)) trackL += 10f;
            camp2 = heart2 + R2 * trackL - U2 * 14f;
            plazaY = Mathf.Max(data.HeightAt(heart2.x, heart2.y), waterY + 6f); duneFrom = plazaY - 3f;

            // dunes: long soft ridges on the inland side
            for (var r = 0; r < data.Nz; r++)
            {
                var z = data.layout.depth - r * data.Step;
                for (var col = 0; col < data.Nx; col++)
                {
                    var i = r * data.Nx + col; if (data.islandIds[i] != index) continue; var x = col * data.Step; var east = x - heart2.x; if (east < 30f) continue;
                    var w = Ease(30f, 90f, east); data.heights[i] += w * (Mathf.Sin(x * .11f + Mathf.Sin(z * .05f) * 1.6f) * 1.6f + Mathf.PerlinNoise(x * .03f, z * .03f) * 2.4f);
                }
            }

            var keep = new List<string>();
            void Put(string name, float u, float rr, float room) { var p = P2(u, rr); if (!Inside(p, room)) return; foreach (var a in data.layout.artists) if (a.sceneId == isl.sceneId && a.name == name) { a.x = p.x; a.z = p.y; keep.Add(name); } }
            // the beach end of the Promenade
            Put("Jorge Ben Jor", -40f, -(roadL - 8f), 26f); Put("Harry Belafonte", 36f, -(roadL - 4f), 22f);
            // ports of call down the Promenade, either side
            Put("Elis Regina", -36f, -62f, 24f); Put("Marcos Valle", 36f, -56f, 24f);
            if (roadL > 170f) { Put("Rodrigo y Gabriela", -36f, -122f, 24f); Put("Herb Alpert & The Tijuana Brass", 36f, -114f, 24f); }
            if (roadL > 240f) { Put("Santo & Johnny", -36f, -182f, 24f); Put("Tony Bennett", 36f, -172f, 24f); }
            // by the Compass
            Put("Keith Mansfield", 62f, 6f, 24f); Put("Dave Richmond", -62f, 4f, 24f); Put("Esperanza Spalding", 98f, -52f, 24f); Put("Robert Stillman", -98f, -46f, 24f); Put("Bossario", -100f, -110f, 24f);
            // inland: the pavilion on the edge of the dunes, the camp out in them
            Put("Mulatu Astatke", 48f, 74f, 28f); Put("Tinariwen", -14f, trackL, 34f);
            var avoid = new List<Vector3> { new Vector3(beach2.x, 36f, beach2.y) };
            for (var d = 0f; d <= roadL; d += 22f) avoid.Add(new Vector3(heart2.x - d, 13f, heart2.y)); for (var d = 0f; d <= trackL; d += 22f) avoid.Add(new Vector3(heart2.x + d, 10f, heart2.y - d / trackL * 14f));
            ArtistPlacement.Spread(data, index, 56f, heart2, 44f, 42f, avoid, keep, Land, 330f);

            Level(data, index, heart2, 22f, 40f, plazaY);
            Level(data, index, beach2, 22f, 48f, waterY + 1.7f);
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
            life = new GameObject("Far Shores life").AddComponent<FarLife>();
            life.transform.SetParent(ctx.Root, false);
            life.center = center; life.fire = fireM; life.lamps = lampM;
            var mk = new MeshKit(); var solid = new MeshKit();
            Remember(ctx.OnGround(center.x, center.z), 0);
            TownStart(ctx);
            Compass(ctx, mk, solid);
            Promenade(ctx, mk, solid);
            Beach(ctx, mk, solid);
            CaravanTrack(ctx, mk, solid);
            Paths(ctx, mk, solid);
            Wilds(ctx, mk, solid);
            mk.Build("The Far Shores", ctx.Root, false);
            solid.BuildCollider("The Far Shores walls", ctx.Root);
        }

        public override void Finish(LandContext ctx) { Verge(ctx); halos.Build("Far Shores glow", ctx.Root); }

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

        /// A white globe lamp on a slim post.
        void GlobeLamp(LandContext ctx, MeshKit mk, Vector3 g)
        {
            var up = Vector3.up; mk.Prism(white, g - up * .2f, up, .06f, 3.4f, 6); mk.Prism(white, g, up, .16f, .3f, 8, .5f); mk.Blob(lampM, g + up * 3.4f, Vector3.one * .26f, new System.Random(4), 0f); ArtistBits.PostCollider(ctx.Root, g, 3.2f, .08f);
        }

        /// A coconut palm.
        void PalmTree(LandContext ctx, MeshKit mk, Vector3 g, float s, System.Random rng, bool straight = false)
        {
            var up = Vector3.up; var lean = straight ? Vector3.zero : Quaternion.Euler(0, (float)rng.NextDouble() * 360f, 0) * Vector3.forward * (.1f + (float)rng.NextDouble() * .2f); var p = g - up * .3f; var d = (up + lean).normalized;
            for (var k = 0; k < 5; k++) { var len = 1.5f * s; mk.Prism(trunkM, p, d, (.2f - k * .02f) * s, len + .05f, 6, .9f); p += d * len; d = (d + lean * .3f).normalized; }
            for (var k = 0; k < 8; k++) { var fd = Quaternion.Euler(0, k * 45f + (float)rng.NextDouble() * 20f, 0) * Vector3.forward; mk.Box(k % 2 == 0 ? leafM : leafDark, p + fd * 1.2f * s - up * .2f * s, new Vector3(.5f * s, .05f, 2.6f * s), Quaternion.LookRotation(fd) * Quaternion.Euler(22f, 0, 0)); }
            for (var k = 0; k < 3; k++) mk.Blob(woodM, p - up * .3f * s + Quaternion.Euler(0, k * 120f, 0) * Vector3.forward * .2f * s, Vector3.one * .15f * s, rng, 0f);
            ArtistBits.PostCollider(ctx.Root, g, 3f * s, .22f * s);
        }

        /// A bench to sit on: white slats.
        void Bench(MeshKit mk, Vector3 g, Vector3 face, float len = 2f)
        {
            var up = Vector3.up; var rot = Quaternion.LookRotation(face); var side = Vector3.Cross(up, face);
            mk.Box(white, g + up * .45f, new Vector3(len, .07f, .5f), rot); mk.Box(white, g + up * .82f - face * .24f, new Vector3(len, .4f, .06f), rot); foreach (var e in new[] { -1f, 1f }) mk.Box(Paint(Teal), g + up * .32f + side * e * (len / 2f - .12f) - face * .08f, new Vector3(.06f, .95f, .5f), rot);
            BoxCol(rootT, g + up * .5f, new Vector3(len, 1f, .6f), rot); Interact.Bench(tlife.transform, g + up * .52f - side * (len / 2f - .4f), g + up * .52f + side * (len / 2f - .4f), face);
        }

        /// A beach umbrella with a lounger under it, to lie on.
        void Umbrella(LandContext ctx, MeshKit mk, Vector3 g, Vector3 face, int i)
        {
            var up = Vector3.up; var rot = Quaternion.LookRotation(face); var c = Paint(Paints[i % Paints.Length]);
            mk.Prism(white, g, (up + face * .06f).normalized, .03f, 2.3f, 5); mk.Cone(c, g + up * 2f, 1.5f, .5f, 8); mk.Cone(white, g + up * 2.2f, .8f, .32f, 8, 22.5f); ArtistBits.PostCollider(ctx.Root, g, 2f, .06f);
            var lg = g + Vector3.Cross(up, face) * 1.1f; mk.Box(bleached, lg + up * .3f, new Vector3(.7f, .08f, 1.9f), rot); mk.Box(c, lg + up * .35f, new Vector3(.62f, .04f, 1.3f), rot); mk.Box(c, lg + up * .56f - face * .75f, new Vector3(.62f, .04f, .7f), rot * Quaternion.Euler(-40f, 0, 0)); foreach (var e in new[] { -.7f, .7f }) mk.Box(bleached, lg + up * .15f + face * e, new Vector3(.6f, .3f, .06f), rot);
            BoxCol(rootT, lg + up * .25f, new Vector3(.7f, .5f, 1.9f), rot); Interact.Seat(tlife.transform, lg + up * .42f, face, "Lie on the lounger").seatHeight = .4f;
        }

        /// A hammock between two posts, to lie in.
        void Hammock(LandContext ctx, MeshKit mk, Vector3 g, Vector3 along, int i)
        {
            var up = Vector3.up; along.y = 0; along.Normalize(); var c = Paint(Paints[(i + 1) % Paints.Length]);
            foreach (var e in new[] { -1f, 1f }) { var p = g + along * e * 1.8f; p.y = ctx0.Ground(p.x, p.z); mk.Prism(trunkM, p - up * .2f, (up + along * e * .12f).normalized, .08f, 2f, 5); ArtistBits.PostCollider(ctx.Root, p, 1.8f, .1f); }
            for (var k = 0; k < 7; k++) { var t = (k - 3) / 3f; mk.Box(k % 2 == 0 ? c : white, g + along * t * 1.4f + up * (.62f + t * t * .5f), new Vector3(.9f, .04f, .5f), Quaternion.LookRotation(along) * Quaternion.Euler(-t * 26f, 0, 0)); }
            Interact.Seat(tlife.transform, g + up * .7f, Vector3.Cross(up, along), "Lie in the hammock").seatHeight = .55f; noGrass.Add(new Vector3(g.x, 2.2f, g.z));
        }

        /// A guitar on a stand.
        void Guitar(MeshKit mk, Vector3 o, Quaternion rot, Material body, System.Random rng, float s = 1f)
        {
            var up = Vector3.up; mk.Blob(body, o + up * .28f * s, new Vector3(.2f, .26f, .06f) * s, rng, .04f); mk.Blob(dark, o + up * .3f * s + rot * Vector3.forward * .05f * s, new Vector3(.05f, .05f, .02f) * s, rng, 0f); mk.Box(woodM, o + up * .86f * s - rot * Vector3.forward * .1f * s, new Vector3(.05f, .72f, .03f) * s, rot * Quaternion.Euler(-9f, 0, 0)); mk.Box(body, o + up * 1.26f * s - rot * Vector3.forward * .17f * s, new Vector3(.07f, .14f, .03f) * s, rot * Quaternion.Euler(-9f, 0, 0));
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
                Strip(mk, pts, stone, PathW, .13f, ctx);
                foreach (var p in pts) Remember(p, walk);
                for (var i = 7; i < pts.Count; i += 12, n++)
                {
                    var s = n % 2 == 0 ? 1f : -1f; var lp = pts[i] + side * s * (PathW / 2f + .8f);
                    if (InFootprint(ctx, lp.x, lp.z, 1f) || InNoGrass(lp.x, lp.z, 1f)) continue;
                    GlobeLamp(ctx, mk, ctx.OnGround(lp.x, lp.z));
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

        // ---------------------------------------------------------------- out along the shore, and out in the dunes
        void Wilds(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            int palms = 0, bushes = 0, hammocks = 0, rocks = 0, acacias = 0, tents = 0, cairns = 0; var rng = new System.Random(1963);
            const float cell = 20f;
            for (var x = ctx.MinX; x < ctx.MaxX; x += cell)
            for (var z = ctx.MinZ; z < ctx.MaxZ; z += cell)
            {
                float px = x + (float)rng.NextDouble() * cell, pz = z + (float)rng.NextDouble() * cell; var roll = (float)rng.NextDouble(); var s = (float)rng.NextDouble();
                if (!Open(ctx, px, pz, 4f)) continue;
                var p = ctx.OnGround(px, pz); var desert = p.y > duneFrom + 2f && px > center.x + 20f; var shore = p.y < waterY + 4.5f;
                if (desert)
                {
                    if (roll < .07f) { Acacia(ctx, mk, p, 1f + s * .5f, rng); acacias++; }
                    else if (roll < .17f) { mk.Blob(Paint(new Color(.74f, .58f, .4f)), p + Vector3.up * .2f, new Vector3(.7f + s, .5f + s * .4f, .7f + s * .6f), rng, .25f); ArtistBits.PostCollider(ctx.Root, p, .8f, .6f + s * .4f); rocks++; }
                    else if (roll < .2f) { Cairn(mk, p, rng); cairns++; }
                    else if (roll < .22f) { if (tents < 4) { SmallTent(ctx, mk, p, FaceTo(p, NearestWalk(p)), tents); tents++; } }
                    else if (roll < .26f) { PalmTree(ctx, mk, p, .8f + s * .3f, rng); palms++; }
                }
                else if (roll < (shore ? .46f : .26f)) { PalmTree(ctx, mk, p, .9f + s * .5f, rng); palms++; }
                else if (shore) continue;
                else if (roll < .4f) { Bush(mk, p, rng); bushes++; }
                else if (roll < .44f) { if (hammocks < 8) { Hammock(ctx, mk, p, Quaternion.Euler(0, s * 180f, 0) * Vector3.forward, hammocks); hammocks++; } }
            }
            Debug.Log($"The Far Shores: Promenade {roadL:0} m, Caravan Track {trackL:0} m, {palms} palms, {bushes} flowering bushes, {hammocks} hammocks, {acacias} acacias, {rocks} rocks, {cairns} cairns, {tents} small tents");
        }

        Vector3 NearestWalk(Vector3 p) { var best = center; var bd = float.MaxValue; foreach (var (q, _) in walkPoints) { var d = (q - p).sqrMagnitude; if (d < bd) { bd = d; best = q; } } return best; }

        void Bush(MeshKit mk, Vector3 g, System.Random rng)
        {
            var up = Vector3.up; mk.Blob(rng.Next(2) == 0 ? leafM : leafDark, g + up * .5f, new Vector3(.9f, .6f, .9f), rng, .3f); var c = Paints[new[] { 1, 2, 5 }[rng.Next(3)]]; var fm = Paint(c);
            for (var k = 0; k < 5; k++) mk.Blob(fm, g + up * (.6f + (float)rng.NextDouble() * .5f) + Quaternion.Euler(0, k * 72f, 0) * Vector3.forward * .7f, Vector3.one * .12f, rng, .3f);
        }

        void Acacia(LandContext ctx, MeshKit mk, Vector3 g, float s, System.Random rng)
        {
            var up = Vector3.up; mk.Prism(trunkM, g - up * .3f, (up + Vector3.right * .1f).normalized, .16f * s, 2.6f * s, 6, .6f); ArtistBits.PostCollider(ctx.Root, g, 2.4f * s, .18f * s);
            for (var k = 0; k < 3; k++) { var d = (Quaternion.Euler(0, k * 120f + 20f, 0) * Vector3.forward + up * .7f).normalized; mk.Prism(trunkM, g + up * 2.2f * s, d, .07f * s, 1.6f * s, 4, .4f); }
            mk.Blob(Paint(new Color(.42f, .52f, .26f)), g + up * 3.6f * s, new Vector3(2.6f, .5f, 2.6f) * s, rng, .2f);
        }

        void Cairn(MeshKit mk, Vector3 g, System.Random rng)
        {
            for (var k = 0; k < 4; k++) mk.Blob(stone, g + Vector3.up * (.14f + k * .24f), new Vector3(.34f - k * .06f, .14f, .3f - k * .05f), rng, .2f);
        }

        void SmallTent(LandContext ctx, MeshKit mk, Vector3 g, Vector3 face, int i)
        {
            var up = Vector3.up; var rot = Quaternion.LookRotation(face); var cloth = Paint(i % 2 == 0 ? Indigo : new Color(.5f, .2f, .18f));
            mk.GableRoof(cloth, g + up * .1f, new Vector3(3.4f, 0, 3f), 1.7f, rot); foreach (var e in new[] { -1f, 1f }) mk.Prism(woodM, g + rot * Vector3.forward * e * 1.5f, up, .04f, 1.84f, 4); BoxCol(ctx.Root, g + up * .6f, new Vector3(2.4f, 1.2f, 2.6f), rot); noGrass.Add(new Vector3(g.x, 3f, g.z));
        }

        /// The verges: lush grass and flowers on the green slope; a few dry tufts in the dunes; nothing on the sand.
        void Verge(LandContext ctx)
        {
            var mat = WindMesh.Material(.3f, .9f, new Color(.4f, .66f, .32f));
            var meshes = new Dictionary<(int, int), WindMesh>(); var rng = new System.Random(1964);
            Color[] flowers = { Coral, Sun, new Color(.95f, .5f, .75f), Cream };
            const float st = 2.6f;
            for (var x = ctx.MinX; x < ctx.MaxX; x += st)
            for (var z = ctx.MinZ; z < ctx.MaxZ; z += st)
            {
                var px = x + ((float)rng.NextDouble() - .5f) * st; var pz = z + ((float)rng.NextDouble() - .5f) * st;
                if (!ctx.IsLand(px, pz, 1f) || NearPath(px, pz, 5f) || InNoGrass(px, pz, 0f) || InFootprint(ctx, px, pz, -2f)) continue;
                var gy = ctx.Ground(px, pz); if (gy < waterY + 3.6f) continue; var desert = gy > duneFrom + 1f;
                if (Mathf.PerlinNoise(px * .04f + 2f, pz * .04f + 8f) < (desert ? .7f : .36f)) continue;
                var key = (Mathf.FloorToInt(px / 70f), Mathf.FloorToInt(pz / 70f));
                if (!meshes.TryGetValue(key, out var wm)) meshes[key] = wm = new WindMesh();
                var col = desert ? Color.Lerp(new Color(.7f, .62f, .36f), new Color(.6f, .6f, .34f), (float)rng.NextDouble()) : Color.Lerp(new Color(.3f, .6f, .28f), new Color(.46f, .7f, .32f), (float)rng.NextDouble());
                wm.Tuft(new Vector3(px, gy - .04f, pz), .26f + (float)rng.NextDouble() * .34f, col, !desert && rng.NextDouble() < .16 ? flowers[rng.Next(flowers.Length)] : Color.Lerp(col, Cream, .3f), rng, 3, .06f);
            }
            foreach (var kv in meshes) kv.Value.Build("Far Shores verge", ctx.Root, mat);
        }
    }
}
