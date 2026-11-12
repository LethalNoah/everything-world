using System.Collections.Generic;
using UnityEngine;

namespace EverythingWorld.World
{
    /// The Iron Range (WORLD_GUIDE.md): black mountains under a thunderstorm, lit from below by forge glow. Behind where
    /// you arrive rises the Forge Peak, the tallest mountain in the world, its forge mouth (a great iron-framed cave
    /// with an anvil the size of a house before it) pouring a glowing river of molten metal down the mountain into a
    /// lava pool, crossed by riveted iron bridges. Mine portals honeycomb the slopes; mine-cart tracks on trestles,
    /// hanging chains, rusted gates, skull-shaped rocks, cracks in the black ground glowing orange, dead pines,
    /// lightning. Files: IronKit (ground, the peak and the forge, the molten river, trails, the range), IronArtists,
    /// IronLife.
    public partial class IronKit : LandKit
    {
        public override string Land => "The Iron Range";
        public override Vector3 ArtistFront(IslandWorldData data, IslandWorldData.Artist a) => facing.TryGetValue(a.name, out var f) ? f : base.ArtistFront(data, a);

        public const string Sandman = "3VqHuw0wFlIHcIPWkhIbdQ", Puppets = "54bm2e3tk8cliUz3VSdCPZ", WarPigs = "2rd9ETlulTbz6BYZcdvIE1",
            Element115 = "725NSbIej5lP3GfhLC7So3", CrazyTrain = "7ACxUo21jtTHzy7ZEV56vU", AceOfSpades = "6EPRKhUOdiFSQwGBRBbvsZ";
        public override (string song, string label, string artist)[] SpecialSongs => new[]
        {
            (Sandman, "Enter Sandman", "@the forge"), (Puppets, "Master of Puppets", "Metallica"), (WarPigs, "War Pigs", "Black Sabbath"),
            (Element115, "115", "Elena Siegman"), (CrazyTrain, "Crazy Train", "Ozzy Osbourne"), (AceOfSpades, "Ace of Spades", "Motörhead"),
            (Beauty, "Beauty Of Annihilation", "Elena Siegman"), (Lullaby, "Lullaby Of A Deadman", "Kevin Sherwood"), (One, "One", "Metallica"), (Paranoid, "Paranoid", "Black Sabbath"),
        };

        public static readonly Color Molten = new Color(1f, .45f, .08f), Ink = new Color(.95f, .9f, .85f);
        const float TrailW = 2.6f, RiverW = 4.5f;

        Material rock, darkRock, iron, rust, dark, timber, chain, lava;
        GlowHalos halos; Vector3 center; IronLife life; MeshKit soft;
        readonly Dictionary<(int, int), List<Vector3>> pathCells = new Dictionary<(int, int), List<Vector3>>();
        readonly List<(Vector3 p, int w)> walkPoints = new List<(Vector3, int)>();
        readonly Dictionary<string, Vector3> facing = new Dictionary<string, Vector3>();
        readonly List<Vector3> noGrass = new List<Vector3>();
        Vector2 heart2, peak2, forge2, pool2; float peakR = 120f; bool hasPeak;

        void MakeMaterials()
        {
            rock = WorldMaterials.Solid(new Color(.3f, .29f, .3f)); darkRock = WorldMaterials.Solid(new Color(.16f, .15f, .16f));
            iron = WorldMaterials.Solid(new Color(.3f, .31f, .33f), .7f); rust = WorldMaterials.Solid(new Color(.52f, .25f, .13f), .3f);
            dark = WorldMaterials.Solid(new Color(.06f, .05f, .05f)); timber = WorldMaterials.Solid(new Color(.36f, .26f, .18f));
            chain = WorldMaterials.Solid(new Color(.4f, .4f, .42f), .8f);
            lava = new Material(WorldMaterials.Glow(Molten, 1.6f)) { name = "Molten metal" };
        }

        Material GlowM(Color c, float s = 2f) => WorldMaterials.Glow(c, s * .65f);
        static bool HasStation(IslandWorldData.Artist a) => a.stationId != 0;
        static float Foot(IslandWorldData.Artist a) => a.name == "Metallica" ? 20f : a.name == "Black Sabbath" ? 16f : a.name == "Ozzy Osbourne" || a.name == "Elena Siegman" ? 14f : HasStation(a) ? 13f : 8f;

        public override bool Ground(float height, float slope, float patch, out Color color)
        {
            if (slope > 10f) color = Color.Lerp(new Color(.3f, .28f, .29f), new Color(.4f, .37f, .36f), patch);
            else
            {   // read by height: black ash by the sea, rust on the slopes, paler rock up high
                var t = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(20f, 115f, height));
                var low = Color.Lerp(new Color(.13f, .12f, .13f), new Color(.2f, .17f, .16f), patch); var mid = Color.Lerp(new Color(.3f, .2f, .15f), new Color(.5f, .3f, .18f), patch); var high = Color.Lerp(new Color(.5f, .42f, .38f), new Color(.62f, .5f, .42f), patch);
                color = t < .5f ? Color.Lerp(low, mid, t * 2f) : Color.Lerp(mid, high, t * 2f - 1f);
            }
            color.a = 1f;
            return true;
        }

        static float SegDist(Vector2 p, Vector2 a, Vector2 b, out float t)
        {
            var ab = b - a; t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / Mathf.Max(ab.sqrMagnitude, .0001f));
            return Vector2.Distance(p, a + ab * t);
        }

        bool InRiver(float x, float z, float pad) => hasPeak && (SegDist(new Vector2(x, z), forge2, pool2, out _) < RiverW / 2f + pad || Vector2.Distance(new Vector2(x, z), pool2) < 11f + pad);
        bool OnPeak(float x, float z, float pad) => hasPeak && Vector2.Distance(new Vector2(x, z), peak2) < peakR * .7f + pad;

        public override void ShapeGround(IslandWorldData data, int index)
        {
            var isl = data.layout.islands[index]; heart2 = new Vector2(isl.labelX, isl.labelZ);
            bool Inside(Vector2 p, float m) { if (data.IslandAt(p.x, p.y) != isl) return false; for (var k = 0; k < 16; k++) { var a = k * Mathf.PI / 8f; if (data.IslandAt(p.x + Mathf.Cos(a) * m, p.y + Mathf.Sin(a) * m) != isl) return false; } return true; }
            // the Forge Peak: as big as fits, behind the arrival
            hasPeak = false;
            for (var R = 140f; R >= 70f && !hasPeak; R -= 10f)
                for (var k = 0; k < 16 && !hasPeak; k++)
                {
                    var a = k * Mathf.PI / 8f; var d = new Vector2(Mathf.Cos(a), Mathf.Sin(a)); var c = heart2 + d * (R * .75f + 45f);
                    if (Inside(c, R * .75f)) { hasPeak = true; peak2 = c; peakR = R; }
                }
            if (hasPeak)
            {
                var toHeart = (heart2 - peak2).normalized; var H = peakR * 1.15f;
                for (var r = 0; r < data.Nz; r++)
                {
                    var z = data.layout.depth - r * data.Step; if (Mathf.Abs(z - peak2.y) > peakR) continue;
                    for (var col = 0; col < data.Nx; col++)
                    {
                        var x = col * data.Step; if (Mathf.Abs(x - peak2.x) > peakR) continue;
                        var i = r * data.Nx + col; if (data.islandIds[i] != index) continue;
                        var v = new Vector2(x, z) - peak2; var d = v.magnitude / peakR; if (d >= 1f) continue;
                        var ang = Mathf.Atan2(v.y, v.x); var ridges = 1f + .18f * Mathf.Sin(ang * 5f + 1f) + .1f * Mathf.Sin(ang * 11f);
                        data.heights[i] += H * Mathf.Pow(Mathf.Max(0f, 1f - d / ridges), 1.7f);
                    }
                }
                forge2 = peak2 + toHeart * peakR * .55f; pool2 = heart2 + (peak2 - heart2).normalized * 32f + new Vector2(-toHeart.y, toHeart.x) * 18f;
            }
            var avoid = new List<Vector3>();
            if (hasPeak)
            {
                avoid.Add(new Vector3(peak2.x, peakR * .8f, peak2.y)); avoid.Add(new Vector3(pool2.x, 20f, pool2.y));
                for (var t = 0f; t <= 1f; t += .1f) { var p = Vector2.Lerp(forge2, pool2, t); avoid.Add(new Vector3(p.x, 12f, p.y)); }
            }
            ArtistPlacement.Spread(data, index, 56f, heart2, 44f, 44f, avoid, null, Land, 320f);
            foreach (var a in data.layout.artists)
            {
                if (a.sceneId != isl.sceneId) continue;
                a.r = Mathf.Max(a.r, Foot(a) + 6f);
                Level(data, index, new Vector2(a.x, a.z), Foot(a) + 1f, Foot(a) + 12f);
            }
            Level(data, index, heart2, 18f, 34f);
            foreach (var (at, r) in PlaceRings) Level(data, index, at, r, r + 16f);
            if (hasPeak) Level(data, index, pool2, 13f, 22f, -1.6f);
        }

        void Level(IslandWorldData data, int index, Vector2 c, float flat, float blend, float drop = 0f)
        {
            var level = data.HeightAt(c.x, c.y) + drop;
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
            life = new GameObject("Iron life").AddComponent<IronLife>();
            life.transform.SetParent(ctx.Root, false);
            life.center = center; life.lava = lava;
            var mk = new MeshKit(); var solid = new MeshKit();
            Remember(ctx.OnGround(center.x, center.z), 0);
            if (hasPeak) { Forge(ctx, mk, solid); River(ctx, mk, solid); }
            Arrival(ctx, mk, solid);
            Trails(ctx, mk, solid);
            RangePlaces(ctx, mk, solid);
            Range(ctx, mk, solid);
            mk.Build("Iron Range", ctx.Root, false);
            soft.Build("Iron dead pines", ctx.Root, false);
            solid.BuildCollider("Iron Range walls", ctx.Root);
        }

        public override void Finish(LandContext ctx) { GroundCover(ctx); halos.Build("Iron glow", ctx.Root); }

        // ---------------------------------------------------------------- helpers
        /// A plain box collider on its own object (shapes whose look and collider differ, branches, rings).
        static void BoxCol(Transform parent, Vector3 center, Vector3 size, Quaternion rot)
        {
            var go = new GameObject("Box collider"); go.transform.SetParent(parent, false); go.transform.SetPositionAndRotation(center, rot);
            go.AddComponent<BoxCollider>().size = size; go.isStatic = true;
        }

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
            ctx.IsLand(x, z, 4f) && !InFootprint(ctx, x, z, pad) && !NearPath(x, z, pad + 2f) && !InNoGrass(x, z, pad) && !InRiver(x, z, pad + 2f)
            && Vector2.Distance(new Vector2(x, z), new Vector2(center.x, center.z)) > 26f + pad;

        void Strip(MeshKit mk, List<Vector3> pts, Material m, float w, float lift, LandContext ground = null, bool followSlope = false)
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
                    if (!followSlope || pts[i].y > Mathf.Max(gl, gr) + .3f) { L[i].y = Mathf.Max(gl, top - .15f) + lift; R[i].y = Mathf.Max(gr, top - .15f) + lift; }   // a deck in the air: keep it level
                    else { var bump = Mathf.Max(0f, pts[i].y - (gl + gr) / 2f); L[i].y = gl + bump + lift; R[i].y = gr + bump + lift; }   // on the ground: follow a sidehill, lift over a crown
                }
            }
            for (var i = 0; i + 1 < n; i++) mk.Quad(m, L[i], R[i], R[i + 1], L[i + 1], (pts[i] + pts[i + 1]) / 2f - Vector3.up);
        }

        /// An iron brazier on a tripod: a bowl of fire.
        void Brazier(LandContext ctx, MeshKit mk, Vector3 g)
        {
            var up = Vector3.up;
            for (var k = 0; k < 3; k++) { var d = Quaternion.Euler(0, k * 120f, 0) * Vector3.forward; mk.Box(iron, g + d * .3f + up * .6f, new Vector3(.08f, 1.3f, .08f), Quaternion.LookRotation(d) * Quaternion.Euler(-12f, 0, 0)); }
            mk.Prism(iron, g + up * 1.15f, up, .45f, .3f, 8, 1.3f); mk.Prism(GlowM(Molten, 2.6f), g + up * 1.42f, up, .5f, .05f, 8);
            halos.Add(g + up * 1.7f, 1.6f, Molten, .8f); life.fires.Add(g + up * 1.5f);
            ArtistBits.PostCollider(ctx.Root, g, 1.4f, .45f);
        }

        /// A sagging chain between two points.
        void Chain(MeshKit mk, Vector3 a, Vector3 b, float sag)
        {
            var n = Mathf.Max(4, Mathf.RoundToInt(Vector3.Distance(a, b) / .45f));
            for (var i = 0; i < n; i++)
            {
                var t0 = i / (float)n; var t1 = (i + 1) / (float)n;
                var p0 = Vector3.Lerp(a, b, t0) - Vector3.up * Mathf.Sin(t0 * Mathf.PI) * sag; var p1 = Vector3.Lerp(a, b, t1) - Vector3.up * Mathf.Sin(t1 * Mathf.PI) * sag;
                mk.Box(chain, (p0 + p1) / 2f, new Vector3(i % 2 == 0 ? .14f : .05f, i % 2 == 0 ? .05f : .14f, Vector3.Distance(p0, p1) + .05f), Quaternion.LookRotation(p1 - p0));
            }
        }

        // ---------------------------------------------------------------- the forge
        /// The forge mouth in the flank of the peak: a great cave framed in riveted iron girders, glowing from within,
        /// an anvil the size of a house before it with a hammer resting on it, chains hung across the mouth, smoke and
        /// embers rising.
        void Forge(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var f = ctx.OnGround(forge2.x, forge2.y); var face = new Vector3(heart2.x - peak2.x, 0, heart2.y - peak2.y).normalized; var side = Vector3.Cross(up, face); var rot = Quaternion.LookRotation(face);
            // the cave: a dark recess walled in rock with an iron frame
            mk.Box(dark, f + up * 6f - face * 3f, new Vector3(12f, 12f, .5f), rot);
            mk.Box(GlowM(Molten, 2.4f), f + up * 4f - face * 2.7f, new Vector3(9f, 6f, .1f), rot);
            foreach (var s in new[] { -1f, 1f }) { mk.Box(rock, f + up * 6f + side * s * 7.5f, new Vector3(3f, 14f, 8f), rot); solid.Box(rock, f + up * 6f + side * s * 7.5f, new Vector3(3f, 14f, 8f), rot); mk.Box(iron, f + up * 6f + side * s * 6f + face * 3.6f, new Vector3(1f, 13f, 1f), rot); }
            mk.Box(rock, f + up * 14f, new Vector3(18f, 4f, 8f), rot); mk.Box(iron, f + up * 12.4f + face * 3.6f, new Vector3(13f, 1.2f, 1f), rot);
            for (var k = 0; k < 7; k++) { var rv = f + up * (1f + k * 1.8f) + face * 4.15f; foreach (var s in new[] { -1f, 1f }) mk.Prism(chain, rv + side * s * 6f, face, .12f, .1f, 6); }
            solid.Box(dark, f + up * 6f - face * 3f, new Vector3(12f, 12f, 1f), rot);
            foreach (var s in new[] { -1f, 1f }) Chain(mk, f + up * 11.5f + side * s * 5.5f + face * 3.8f, f + up * 11.5f + face * 3.8f, 2.2f);
            // the anvil and hammer
            var an = ctx.OnGround(f.x + face.x * 14f, f.z + face.z * 14f);
            mk.Box(iron, an + up * 1.5f, new Vector3(5f, 3f, 4f), rot); mk.Box(iron, an + up * 3.5f, new Vector3(3f, 1f, 2.4f), rot); mk.Box(iron, an + up * 4.9f, new Vector3(10f, 1.8f, 3.2f), rot);
            mk.Cone(iron, an + up * 4.9f + side * 5f, 1.4f, 4f, 6); // the horn, pointing sideways
            solid.Box(iron, an + up * 3f, new Vector3(10f, 6f, 4f), rot);
            var hh = an + up * 6.4f - side * 2f; mk.Box(timber, hh + side * 1.5f + face * .5f, new Vector3(4.5f, .4f, .4f), Quaternion.LookRotation(face) * Quaternion.Euler(0, 75f, 0)); mk.Box(iron, hh - side * 1f, new Vector3(1.8f, 1.2f, 1.2f), rot);
            ArtistBits.PointLight(ctx.Root, f + up * 4f + face * 6f, Molten, 40f, 3f);
            halos.Add(f + up * 4f + face * 1f, 9f, Molten, .5f);
            life.forge = f + up * 6f + face * 2f; life.forgeFace = face;
            noGrass.Add(new Vector3(f.x, 16f, f.z)); noGrass.Add(new Vector3(an.x, 7f, an.z));
            var mark = new GameObject("Shot: the forge").transform; mark.SetParent(ctx.Root, false);
            var cam = ctx.OnGround(center.x - face.x * 4f, center.z - face.z * 4f); var look = f + up * 6f - cam; mark.SetPositionAndRotation(cam, Quaternion.LookRotation(new Vector3(look.x, look.y * .6f, look.z).normalized));
        }

        /// The molten river: a glowing channel from the forge mouth down the mountain to the lava pool by the arrival,
        /// edged with dark stone, crusted at the sides.
        void River(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var pts = new List<Vector3>(); var len = Vector2.Distance(forge2, pool2); var dir2 = (pool2 - forge2).normalized; var perp = new Vector2(-dir2.y, dir2.x);
            for (var d = 6f; d <= len; d += 2f) { var q = forge2 + dir2 * d + perp * Mathf.Sin(d / 30f) * 4f; pts.Add(ctx.OnGround(q.x, q.y) + Vector3.up * .15f); }
            if (pts.Count < 2) return;
            Strip(mk, pts, lava, RiverW, .1f, ctx);
            var crust = WorldMaterials.Solid(new Color(.1f, .08f, .08f));
            for (var i = 0; i + 1 < pts.Count; i++)
            {
                var a = pts[i]; var b = pts[i + 1]; var d = b - a; d.y = 0; var sd = Vector3.Cross(Vector3.up, d.normalized);
                foreach (var s in new[] { -1f, 1f }) { mk.Box(darkRock, (a + b) / 2f + sd * s * (RiverW / 2f + .4f) + Vector3.up * .2f, new Vector3(.9f, .7f, Vector3.Distance(a, b) + .3f), Quaternion.LookRotation(b - a)); solid.Box(darkRock, (a + b) / 2f + sd * s * (RiverW / 2f + .4f) + Vector3.up * .5f, new Vector3(.9f, 1.2f, Vector3.Distance(a, b) + .3f), Quaternion.LookRotation(b - a)); }
                if (i % 5 == 2) mk.Blob(crust, a + sd * (i % 2 == 0 ? 1.2f : -1.2f) + Vector3.up * .35f, new Vector3(.6f, .15f, .9f), ctx.Rng, .3f);
            }
            // the lava pool
            var pc = ctx.OnGround(pool2.x, pool2.y) + Vector3.up * .45f;
            for (var k = 0; k < 28; k++) { float a0 = k * Mathf.PI * 2f / 28f, a1 = (k + 1) * Mathf.PI * 2f / 28f; mk.Tri(lava, pc, pc + new Vector3(Mathf.Cos(a0), 0, Mathf.Sin(a0)) * 11f, pc + new Vector3(Mathf.Cos(a1), 0, Mathf.Sin(a1)) * 11f, pc - Vector3.up); }
            for (var k = 0; k < 22; k++) { var a = k * Mathf.PI * 2f / 22f; var rp = ctx.OnGround(pool2.x + Mathf.Cos(a) * 11.6f, pool2.y + Mathf.Sin(a) * 11.6f); mk.Blob(darkRock, rp + Vector3.up * .5f, new Vector3(1.4f, .9f, 1.2f), ctx.Rng, .35f); }
            solid.Prism(darkRock, pc - Vector3.up * 1.5f, Vector3.up, 13.4f, 2.6f, 16);
            halos.Add(pc + Vector3.up * 1f, 9f, Molten, .45f); ArtistBits.PointLight(ctx.Root, pc + Vector3.up * 4f, Molten, 26f, 2.2f);
            life.river = pts.ToArray(); life.pool = pc;
            noGrass.Add(new Vector3(pool2.x, 14f, pool2.y));
            var mark = new GameObject("Shot: the molten river").transform; mark.SetParent(ctx.Root, false);
            var mid = pts[pts.Count / 2]; var md = pts[Mathf.Min(pts.Count - 1, pts.Count / 2 + 1)] - mid; md.y = 0; md.Normalize(); var ms = Vector3.Cross(Vector3.up, md);
            var pc2 = ctx.OnGround(pool2.x, pool2.y); var cam = pc2 + ms * 18f + md * 8f; cam = ctx.OnGround(cam.x, cam.z); var look = mid - cam; mark.SetPositionAndRotation(cam, Quaternion.LookRotation(new Vector3(look.x, look.y * .6f, look.z).normalized));
        }

        /// Where you arrive: an iron sign, braziers round a stone ring, rusted gates hung open.
        void Arrival(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var c = ctx.OnGround(center.x, center.z);
            mk.Prism(darkRock, c - up * .25f, up, 9f, .35f, 16);
            for (var k = 0; k < 6; k++) { var a = k * 60f * Mathf.Deg2Rad + .3f; Brazier(ctx, mk, ctx.OnGround(c.x + Mathf.Cos(a) * 9.5f, c.z + Mathf.Sin(a) * 9.5f)); }
            var toPeak = hasPeak ? new Vector3(peak2.x - c.x, 0, peak2.y - c.z).normalized : Vector3.forward; var sd = Vector3.Cross(up, toPeak);
            // rusted gates hung open on stone posts, framing the way to the forge
            var g = c + toPeak * 12f; g.y = ctx.Ground(g.x, g.z);
            foreach (var s in new[] { -1f, 1f })
            {
                var post = g + sd * s * 3.4f; mk.Box(darkRock, post + up * 2.5f, new Vector3(1.2f, 5f, 1.2f), Quaternion.LookRotation(toPeak)); solid.Box(darkRock, post + up * 2.5f, new Vector3(1.2f, 5f, 1.2f), Quaternion.LookRotation(toPeak));
                mk.Cone(iron, post + up * 5f, .7f, 1.2f, 4, 45f);
                var hinge = post - sd * s * .6f; var grot = Quaternion.LookRotation(toPeak) * Quaternion.Euler(0, s * -70f, 0); var gc = hinge + grot * new Vector3(-s * 1.4f, 2f, 0);
                for (var b = 0; b < 6; b++) mk.Box(rust, gc + grot * new Vector3((b - 2.5f) * .5f, 0, 0), new Vector3(.1f, 3.8f, .1f), grot);
                foreach (var h in new[] { -1.5f, 0f, 1.5f }) mk.Box(rust, gc + grot * new Vector3(0, h, 0), new Vector3(2.9f, .14f, .12f), grot);
                for (var b = 0; b < 6; b++) mk.Cone(rust, gc + grot * new Vector3((b - 2.5f) * .5f, 1.9f, 0), .08f, .4f, 4);
                solid.Box(rust, gc, new Vector3(3f, 3.8f, .3f), grot);
            }
            mk.Box(iron, g + up * 5.3f, new Vector3(8f, .5f, .5f), Quaternion.LookRotation(toPeak));
            ArtistBits.Label(ctx.Root, "THE IRON RANGE", g + up * 5.3f - toPeak * .3f, -toPeak, .32f, new Color(1f, .7f, .4f), TextAnchor.MiddleCenter);
            noGrass.Add(new Vector3(c.x, 11f, c.z));
        }

        // ---------------------------------------------------------------- trails
        void Trails(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var todo = new List<IslandWorldData.Artist>(ctx.Artists); var walk = 1; var lamps = new List<Vector3>(); var gravel = WorldMaterials.Solid(new Color(.34f, .3f, .28f));
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
                var pts = new List<Vector3>(); var hot = new List<bool>();
                for (var d = walk == 1 ? 10f : TrailW / 2f + .1f; ; d += 1.5f)
                {
                    var dd = Mathf.Min(d, end); var q = from + dir * dd + side * Mathf.Sin(dd / 15f) * Mathf.Min(3f, end * .05f);
                    if (!ctx.IsLand(q.x, q.z, 0f)) break;
                    pts.Add(ctx.OnGround(q.x, q.z)); hot.Add(InRiver(q.x, q.z, 1.5f)); if (dd >= end) break;
                }
                if (pts.Count < 2) continue;
                for (var i = 0; i < pts.Count; i++)
                {
                    if (!hot[i]) continue;
                    var j = i; while (j + 1 < pts.Count && hot[j + 1]) j++;
                    var i0 = Mathf.Max(0, i - 1); var i1 = Mathf.Min(pts.Count - 1, j + 1);
                    var deck = Mathf.Max(pts[i0].y, pts[i1].y) + 1.2f;
                    for (var k = i0; k <= i1; k++) { var p = pts[k]; p.y = deck; pts[k] = p; }
                    var a = pts[i0] - dir * 1.5f; var b = pts[i1] + dir * 1.5f; a.y = b.y = deck; IronBridge(ctx, mk, solid, a, b);
                    if (i0 > 0) Ramp(solid, pts[i0 - 1], pts[i0]);                   // the ramps up onto the deck, walkable
                    if (i1 + 1 < pts.Count) Ramp(solid, pts[i1], pts[i1 + 1]);
                    i = j + 1;
                }
                Strip(mk, pts, gravel, TrailW, .2f, ctx, true);
                foreach (var p in pts) Remember(p, walk);
                for (var i = 7; i < pts.Count; i += 13)
                {
                    var lp = pts[i] + side * ((i / 13) % 2 == 0 ? 1f : -1f) * (TrailW / 2f + .7f);
                    var near = false; foreach (var o in lamps) if ((o - lp).sqrMagnitude < 20f * 20f) near = true;
                    if (near || InRiver(lp.x, lp.z, 1f)) continue;
                    lamps.Add(lp); Brazier(ctx, mk, ctx.OnGround(lp.x, lp.z));
                }
                walk++;
            }
        }

        /// A solid slab under a trail ramp, so you walk up it rather than through it.
        void Ramp(MeshKit solid, Vector3 a, Vector3 b)
        {
            var d = b - a; if (d.sqrMagnitude < .01f) return;
            var rot = Quaternion.LookRotation(d); solid.Box(iron, (a + b) / 2f + rot * Vector3.down * .2f + Vector3.up * .2f, new Vector3(TrailW, .4f, d.magnitude + .4f), rot);
        }

        /// A riveted iron bridge with a lattice of girders along each side.
        void IronBridge(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 a, Vector3 b)
        {
            var up = Vector3.up; var dir = (b - a); var len = dir.magnitude; dir /= len; var rot = Quaternion.LookRotation(dir); var side = Vector3.Cross(up, new Vector3(dir.x, 0, dir.z).normalized); var mid = (a + b) / 2f;
            mk.Box(iron, mid, new Vector3(3.4f, .3f, len + .5f), rot); solid.Box(iron, mid - up * .1f, new Vector3(3.4f, .5f, len + .5f), rot);
            foreach (var s in new[] { -1f, 1f })
            {
                mk.Box(iron, mid + side * s * 1.7f + up * 1.2f, new Vector3(.15f, .15f, len + .5f), rot); mk.Box(iron, mid + side * s * 1.7f + up * .2f, new Vector3(.2f, .3f, len + .5f), rot);
                var n = Mathf.Max(2, Mathf.RoundToInt(len / 1.2f));
                for (var i = 0; i < n; i++) { var t0 = (i / (float)n - .5f) * len; var t1 = ((i + 1) / (float)n - .5f) * len; var p0 = mid + side * s * 1.7f + dir * t0 + up * .3f; var p1 = mid + side * s * 1.7f + dir * t1 + up * 1.15f; mk.Box(rust, (p0 + p1) / 2f, new Vector3(.08f, .08f, Vector3.Distance(p0, p1)), Quaternion.LookRotation(p1 - p0)); }
                solid.Box(iron, mid + side * s * 1.75f + up * .8f, new Vector3(.25f, 1.4f, len + .5f), rot);
            }
        }

        // ---------------------------------------------------------------- the range
        /// Black rocks and crags, dead pines, mine portals in the slopes with tracks running out on trestles and a cart
        /// left on them, hanging chains between posts, skull rocks with glowing eyes, glowing cracks in the ground,
        /// old anvils and slag heaps.
        void Range(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            int pines = 0, crags = 0, mines = 0, skulls = 0, cracks = 0, anvils = 0;
            const float cell = 18f; var pine = WorldMaterials.Solid(new Color(.14f, .16f, .15f)); var deadBark = WorldMaterials.Solid(new Color(.2f, .17f, .16f));
            var crackM = GlowM(Molten, 1.6f);
            for (var x = ctx.MinX; x < ctx.MaxX; x += cell)
            for (var z = ctx.MinZ; z < ctx.MaxZ; z += cell)
            {
                float px = x + ctx.Range(0f, cell), pz = z + ctx.Range(0f, cell);
                if (!Open(ctx, px, pz, 3f)) continue;
                var p = ctx.OnGround(px, pz); var roll = ctx.Range(0f, 1f); var steep = OnPeak(px, pz, 0f);
                if (roll < .03f && mines < 9 && !steep) { MinePortal(ctx, mk, solid, p); mines++; }
                else if (roll < .3f) { DeadPine(ctx, pine, deadBark, p); pines++; }
                else if (roll < .46f) { var s = ctx.Range(1.5f, 4.5f); var cm = ctx.Chance(.5f) ? rock : darkRock; var ch = s * ctx.Range(1.2f, 2.6f); var tw = ctx.Range(0, 60f); mk.Cone(cm, p - Vector3.up * .5f, s, ch, 5, tw); solid.Cone(rock, p - Vector3.up * .5f, s, ch, 5, tw); crags++; }
                else if (roll < .5f && !steep) { SkullRock(ctx, mk, solid, p); skulls++; }
                else if (roll < .6f && !steep) { var dir = Quaternion.Euler(0, ctx.Range(0, 180f), 0) * Vector3.forward; var q = p; for (var k = 0; k < 5; k++) { var nd = Quaternion.Euler(0, ctx.Range(-35f, 35f), 0) * dir; var r = ctx.OnGround(q.x + nd.x * 2.4f, q.z + nd.z * 2.4f); mk.Box(crackM, (q + r) / 2f + Vector3.up * .03f, new Vector3(.18f, .04f, 2.5f), Quaternion.LookRotation(r - q)); q = r; } cracks++; }
                else if (roll < .63f && !steep) { var r0 = Quaternion.Euler(0, ctx.Range(0, 180f), 0); var r1 = Quaternion.Euler(0, ctx.Range(0, 180f), 0); mk.Box(iron, p + Vector3.up * .4f, new Vector3(1f, .8f, .7f), r0); mk.Box(iron, p + Vector3.up * 1f, new Vector3(1.8f, .4f, .7f), r1); BoxCol(ctx.Root, p + Vector3.up * .2f, new Vector3(1f, 1.4f, .7f), r0); BoxCol(ctx.Root, p + Vector3.up * 1f, new Vector3(1.8f, .4f, .7f), r1); anvils++; }
                else if (roll < .66f && !steep) { var b = p + new Vector3(ctx.Range(4f, 7f), 0, ctx.Range(-2f, 2f)); b = ctx.OnGround(b.x, b.z); foreach (var e in new[] { p, b }) { mk.Box(timber, e + Vector3.up * 2f, new Vector3(.3f, 4f, .3f), Quaternion.identity); ArtistBits.PostCollider(ctx.Root, e, 4f, .2f); } Chain(mk, p + Vector3.up * 3.8f, b + Vector3.up * 3.8f, 1.2f); }
            }
            Debug.Log($"The Iron Range: peak {(hasPeak ? peakR.ToString("0") + " m" : "none")}, {pines} dead pines, {crags} crags, {mines} mine portals, {skulls} skull rocks, {cracks} glowing cracks, {anvils} anvils");
        }

        void DeadPine(LandContext ctx, Material pine, Material bark, Vector3 at)
        {
            var up = Vector3.up; var s = ctx.Range(.8f, 1.4f); var h = 9f * s;
            soft.Prism(bark, at - up * .3f, up, .3f * s, h, 6, .2f);
            for (var k = 0; k < 6; k++)
            {
                var d = Quaternion.Euler(0, k * 61f + ctx.Range(0, 30f), 0) * Vector3.forward; var bs = at + up * (h * (.3f + k * .1f)); var bd = (d + Vector3.down * .2f).normalized; var bl = (2f - k * .25f) * s;
                soft.Prism(bark, bs, bd, .07f * s, bl, 4, .3f);
                var tip = bs + bd * bl; if (tip.y - ctx.Ground(tip.x, tip.z) < 2.4f) BoxCol(ctx.Root, bs + bd * bl / 2f, new Vector3(.2f, .2f, bl), Quaternion.LookRotation(bd));   // a branch at head height
            }
            if (ctx.Chance(.5f)) for (var k = 0; k < 3; k++) soft.Cone(pine, at + up * (h * (.35f + k * .2f)), (1.8f - k * .45f) * s, h * .25f, 6, ctx.Range(0, 60f));
            ArtistBits.PostCollider(ctx.Root, at, h * .6f, .3f * s);
        }

        void SkullRock(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 at)
        {
            var up = Vector3.up; var face = new Vector3(center.x - at.x, 0, center.z - at.z).normalized; var side = Vector3.Cross(up, face); var s = ctx.Range(1.6f, 2.8f);
            mk.Blob(rock, at + up * 1.2f * s, new Vector3(1.3f, 1.2f, 1.3f) * s, ctx.Rng, .15f); mk.Box(rock, at + up * .45f * s + face * .5f * s, new Vector3(1.4f, .7f, 1f) * s, Quaternion.LookRotation(face));
            foreach (var k in new[] { -1f, 1f }) { mk.Blob(dark, at + up * 1.25f * s + face * 1.15f * s + side * k * .45f * s, Vector3.one * .3f * s, ctx.Rng, 0f); mk.Blob(GlowM(Molten, 3f), at + up * 1.25f * s + face * 1.3f * s + side * k * .45f * s, Vector3.one * .1f * s, ctx.Rng, 0f); }
            mk.Box(dark, at + up * .85f * s + face * 1.2f * s, new Vector3(.25f, .3f, .1f) * s, Quaternion.LookRotation(face));
            for (var k = 0; k < 4; k++) mk.Box(WorldMaterials.Solid(new Color(.55f, .52f, .48f)), at + up * .5f * s + face * 1.01f * s + side * (k - 1.5f) * .28f * s, new Vector3(.2f, .3f, .05f) * s, Quaternion.LookRotation(face));
            BoxCol(ctx.Root, at + up * 1.2f * s + face * .1f * s, new Vector3(2.8f, 2.6f, 2.8f) * s, Quaternion.LookRotation(face));   // round the whole head, teeth and eyes too
            halos.Add(at + up * 1.25f * s + face * 1.5f * s, .8f * s, Molten, .6f);
        }

        /// A mine portal: a timber-framed tunnel mouth in a rock mound, a lantern over it, tracks running out on a short
        /// trestle with an ore cart left at the end.
        void MinePortal(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 at)
        {
            var up = Vector3.up; var face = new Vector3(center.x - at.x, 0, center.z - at.z).normalized; var rot = Quaternion.LookRotation(face); var side = Vector3.Cross(up, face);
            mk.Blob(rock, at + up * 2f - face * 2f, new Vector3(6f, 4.5f, 5f), ctx.Rng, .25f); BoxCol(ctx.Root, at + up * 2f - face * 2f, new Vector3(10.4f, 5f, 8.6f), Quaternion.identity);   // the blob's own axes
            mk.Box(dark, at + up * 1.6f + face * 1.5f, new Vector3(2.8f, 3.2f, .1f), rot);
            foreach (var s in new[] { -1.6f, 1.6f }) mk.Box(timber, at + up * 1.7f + face * 1.6f + side * s, new Vector3(.35f, 3.4f, .35f), rot);
            mk.Box(timber, at + up * 3.45f + face * 1.6f, new Vector3(3.8f, .4f, .4f), rot);
            BoxCol(ctx.Root, at + up * 1.8f + face * .6f, new Vector3(3.9f, 3.6f, 2.2f), rot);   // the frame and the black mouth
            mk.Prism(GlowM(Molten, 2.4f), at + up * 2.8f + face * 1.9f, up, .15f, .3f, 6); halos.Add(at + up * 3f + face * 2f, .9f, Molten, .8f);
            for (var k = 0; k < 6; k++) { var tp = at + face * (2f + k * 1.2f); tp.y = Mathf.Max(ctx.Ground(tp.x, tp.z), at.y) + .1f; mk.Box(timber, tp, new Vector3(1.4f, .12f, .25f), rot); foreach (var s in new[] { -.4f, .4f }) mk.Box(iron, tp + side * s + up * .1f, new Vector3(.07f, .08f, 1.25f), rot); }
            var cart = at + face * 6f; cart.y = Mathf.Max(ctx.Ground(cart.x, cart.z), at.y) + .2f;
            mk.Box(rust, cart + up * .6f, new Vector3(1.1f, .8f, 1.5f), rot); mk.Blob(darkRock, cart + up * 1.1f, new Vector3(.5f, .25f, .7f), ctx.Rng, .4f);
            foreach (var s in new[] { -.5f, .5f }) foreach (var zz in new[] { -.45f, .45f }) mk.Prism(iron, cart + side * s * 1.1f + face * zz + up * .2f, side * Mathf.Sign(s), .2f, .1f, 8);
            solid.Box(rust, cart + up * .6f, new Vector3(1.2f, 1.2f, 1.6f), rot);
            noGrass.Add(new Vector3(at.x, 7f, at.z));
        }

        void GroundCover(LandContext ctx)
        {
            var mat = WindMesh.Material(.3f, 1f, new Color(.26f, .24f, .2f));
            var meshes = new Dictionary<(int, int), WindMesh>(); var rng = new System.Random(1970);
            const float step = 3f;
            for (var x = ctx.MinX; x < ctx.MaxX; x += step)
            for (var z = ctx.MinZ; z < ctx.MaxZ; z += step)
            {
                var px = x + ((float)rng.NextDouble() - .5f) * step; var pz = z + ((float)rng.NextDouble() - .5f) * step;
                if (!ctx.IsLand(px, pz, 1f) || NearPath(px, pz, 2.6f) || InNoGrass(px, pz, 0f) || InFootprint(ctx, px, pz, -2f) || InRiver(px, pz, 2f) || OnPeak(px, pz, -peakR * .2f)) continue;
                if (Mathf.PerlinNoise(px * .03f + 2f, pz * .03f + 6f) < .5f) continue;
                var key = (Mathf.FloorToInt(px / 70f), Mathf.FloorToInt(pz / 70f));
                if (!meshes.TryGetValue(key, out var wm)) meshes[key] = wm = new WindMesh();
                var col = Color.Lerp(new Color(.24f, .22f, .18f), new Color(.4f, .3f, .2f), (float)rng.NextDouble());
                var at = new Vector3(px, ctx.Ground(px, pz) - .04f, pz);
                wm.Tuft(at, .4f + (float)rng.NextDouble() * .4f, col, Color.Lerp(col, new Color(.6f, .4f, .25f), .3f), rng, 3, .06f);
            }
            foreach (var kv in meshes) kv.Value.Build("Iron grass", ctx.Root, mat);
        }
    }
}
