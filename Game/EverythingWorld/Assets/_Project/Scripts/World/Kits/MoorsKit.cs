using System.Collections.Generic;
using UnityEngine;

namespace EverythingWorld.World
{
    /// The Alt Rock Moors (WORLD_GUIDE.md): grey-green moorland under a moody overcast sky, with sudden breaks of sun
    /// drifting across it as golden shafts. Heather in purple drifts, dry-stone walls running over the low hills,
    /// power-line pylons striding across, a red phone box and a lonely bus stop at the arrival, a little stone hamlet
    /// or two. On the tallest hill stands a giant lattice radio transmitter, its orange warning lights blinking. The
    /// moor is full of strange things from Radiohead's records (MoorsRadiohead). Files: MoorsKit (ground, the
    /// transmitter, walls, pylons, hamlets, trails, the moor), MoorsRadiohead, MoorsArtists, MoorsLife.
    public partial class MoorsKit : LandKit
    {
        public override string Land => "The Alt Rock Moors";
        public override Vector3 ArtistFront(IslandWorldData data, IslandWorldData.Artist a) => facing.TryGetValue(a.name, out var f) ? f : base.ArtistFront(data, a);

        public const string RightPlace = "2kRFrWaLWiKq48YYVdGcm8", PyramidSong = "55q3Ro66yXWi9rsEddeEN4", WeirdFishes = "4wajJ1o7jWIg62YqpkHC7S", KarmaPolice = "63OQupATfueTdZMWTxW03A",
            Disappear = "2rtGaCAeYtmcIvuZsvgTf6", Videotape = "4T1iiabe7G0UjWQJCY6NE2", FeelGood = "0d28khcov6AiegSCpG5TuT", Supernova = "40bynawzslg9U7ACq07fAj";
        public override (string song, string label, string artist)[] SpecialSongs => new[]
        {
            (RightPlace, "Everything In Its Right Place", "@the cubes"), (PyramidSong, "Pyramid Song", "@the sunken pool"), (WeirdFishes, "Weird Fishes / Arpeggi", "@the sunken pool"),
            (KarmaPolice, "Karma Police", "@the lonely road"), (Disappear, "How to Disappear Completely", "Radiohead"), (Videotape, "Videotape", "Radiohead"),
            (FeelGood, "Feel Good Inc.", "Gorillaz"), (Supernova, "Champagne Supernova", "Oasis"),
        };

        public static readonly Color Signal = new Color(1f, .48f, .2f), Ink = new Color(.95f, .95f, .92f), Lamp = new Color(1f, .8f, .5f);
        const float TrailW = 2.4f;

        Material stone, wallStone, slate, dark, wood, steel, redPaint, white, concrete;
        GlowHalos halos; Vector3 center; MoorsLife life; MeshKit soft;
        readonly Dictionary<(int, int), List<Vector3>> pathCells = new Dictionary<(int, int), List<Vector3>>();
        readonly List<(Vector3 p, int w)> walkPoints = new List<(Vector3, int)>();
        readonly Dictionary<string, Vector3> facing = new Dictionary<string, Vector3>();
        readonly List<Vector3> noGrass = new List<Vector3>();
        Vector2 heart2, hill2; bool hasHill;

        void MakeMaterials()
        {
            stone = WorldMaterials.Solid(new Color(.55f, .54f, .5f)); wallStone = WorldMaterials.Solid(new Color(.5f, .49f, .46f)); slate = WorldMaterials.Solid(new Color(.3f, .33f, .37f));
            dark = WorldMaterials.Solid(new Color(.07f, .07f, .08f)); wood = WorldMaterials.Solid(new Color(.42f, .33f, .25f)); steel = WorldMaterials.Solid(new Color(.6f, .62f, .64f), .7f);
            redPaint = WorldMaterials.Solid(new Color(.8f, .1f, .08f), .7f); white = WorldMaterials.Solid(new Color(.92f, .92f, .9f)); concrete = WorldMaterials.Solid(new Color(.62f, .62f, .6f));
        }

        Material GlowM(Color c, float s = 2f) => WorldMaterials.Glow(c, s * .65f);
        static bool HasStation(IslandWorldData.Artist a) => a.stationId != 0;
        static float Foot(IslandWorldData.Artist a) => a.name == "Radiohead" ? 20f : HasStation(a) ? 11f : 7f;

        public override bool Ground(float height, float slope, float patch, out Color color)
        {
            if (slope > 10f) color = Color.Lerp(new Color(.45f, .45f, .43f), new Color(.55f, .55f, .52f), patch);
            else
            {
                color = Color.Lerp(new Color(.42f, .5f, .36f), new Color(.55f, .58f, .42f), Mathf.SmoothStep(.1f, .9f, patch));   // grey-green moor; the heather is in the grass
            }
            color.a = 1f;
            return true;
        }

        public override void ShapeGround(IslandWorldData data, int index)
        {
            var isl = data.layout.islands[index]; heart2 = new Vector2(isl.labelX, isl.labelZ);
            bool Inside(Vector2 p, float m) { if (data.IslandAt(p.x, p.y) != isl) return false; for (var k = 0; k < 12; k++) { var a = k * Mathf.PI / 6f; if (data.IslandAt(p.x + Mathf.Cos(a) * m, p.y + Mathf.Sin(a) * m) != isl) return false; } return true; }
            // the transmitter hill: the highest ground well inside the land, raised into a proper hill
            hasHill = false; var best = float.MinValue;
            for (var a = 0; a < 24; a++) for (var r = 90f; r <= 260f; r += 20f)
            {
                var p = heart2 + new Vector2(Mathf.Cos(a * Mathf.PI / 12f), Mathf.Sin(a * Mathf.PI / 12f)) * r;
                if (!Inside(p, 60f)) continue; var h = data.HeightAt(p.x, p.y); if (h > best) { best = h; hill2 = p; hasHill = true; }
            }
            if (hasHill)
                for (var r = 0; r < data.Nz; r++)
                {
                    var z = data.layout.depth - r * data.Step; if (Mathf.Abs(z - hill2.y) > 70f) continue;
                    for (var col = 0; col < data.Nx; col++)
                    {
                        var x = col * data.Step; if (Mathf.Abs(x - hill2.x) > 70f) continue;
                        var i = r * data.Nx + col; if (data.islandIds[i] != index) continue;
                        var d = Vector2.Distance(new Vector2(x, z), hill2) / 70f; if (d >= 1f) continue;
                        data.heights[i] += 26f * Mathf.SmoothStep(1f, 0f, d);
                    }
                }
            var avoid = new List<Vector3>(); if (hasHill) avoid.Add(new Vector3(hill2.x, 26f, hill2.y));
            PickPool(data, index, avoid);
            ArtistPlacement.Spread(data, index, 48f, heart2, 40f, 34f, avoid, null, Land, 320f);
            foreach (var a in data.layout.artists)
            {
                if (a.sceneId != isl.sceneId) continue;
                a.r = Mathf.Max(a.r, Foot(a) + 5f);
                Level(data, index, new Vector2(a.x, a.z), Foot(a) + 1f, Foot(a) + 10f);
            }
            Level(data, index, heart2, 14f, 28f);
            if (hasHill) Level(data, index, hill2, 9f, 16f);
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
            life = new GameObject("Moors life").AddComponent<MoorsLife>();
            life.transform.SetParent(ctx.Root, false);
            life.center = center;
            var mk = new MeshKit(); var solid = new MeshKit();
            Arrival(ctx, mk, solid);
            if (hasHill) Transmitter(ctx, mk, solid);
            Trails(ctx, mk, solid);
            Marvels(ctx, mk, solid);
            Moor(ctx, mk, solid);
            mk.Build("Alt Rock Moors", ctx.Root, false);
            soft.Build("Moors heather and gorse", ctx.Root, false);   // not "Soft…": LandCuller hides those at grass distance
            solid.BuildCollider("Alt Rock Moors walls", ctx.Root);
        }

        public override void Finish(LandContext ctx) { GroundCover(ctx); halos.Build("Moors glow", ctx.Root); }

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
            && Vector2.Distance(new Vector2(x, z), new Vector2(center.x, center.z)) > 22f + pad;

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

        /// A street lamp: a grey post with a sodium-orange lantern.
        void StreetLamp(LandContext ctx, MeshKit mk, Vector3 g)
        {
            var up = Vector3.up;
            mk.Prism(steel, g - up * .3f, up, .08f, 4.3f, 6); mk.Box(steel, g + up * 3.95f + Vector3.right * .45f, new Vector3(1f, .1f, .12f), Quaternion.identity);
            mk.Box(GlowM(Lamp, 2.4f), g + up * 3.82f + Vector3.right * .85f, new Vector3(.4f, .14f, .25f), Quaternion.identity);
            halos.Add(g + up * 3.7f + Vector3.right * .85f, 1.4f, Lamp, .75f);
            ArtistBits.PostCollider(ctx.Root, g, 4f, .1f);
        }

        /// A dry-stone wall from a to b, following the ground, with a little gap now and then.
        void DryWall(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 a, Vector3 b)
        {
            var dir = b - a; dir.y = 0; var len = dir.magnitude; if (len < 4f) return; dir /= len;
            for (var d = 0f; d < len; d += 2f)
            {
                var p = a + dir * d; if (!ctx.IsLand(p.x, p.z, 1f) || NearPath(p.x, p.z, 2.4f) || InFootprint(ctx, p.x, p.z, 1f) || InNoGrass(p.x, p.z, 1f)) continue;
                var q = a + dir * Mathf.Min(d + 2f, len); var g0 = ctx.OnGround(p.x, p.z); var g1 = ctx.OnGround(q.x, q.z);
                var mid = (g0 + g1) / 2f + Vector3.up * .55f; var rot = Quaternion.LookRotation(g1 - g0);
                mk.Box(wallStone, mid, new Vector3(.7f, 1.1f, Vector3.Distance(g0, g1) + .05f), rot); mk.Box(stone, mid + Vector3.up * .6f, new Vector3(.8f, .14f, Vector3.Distance(g0, g1) + .05f), rot);
                solid.Box(wallStone, mid, new Vector3(.7f, 1.4f, Vector3.Distance(g0, g1)), rot);
            }
        }

        /// A red phone box with lit windows.
        void PhoneBox(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 at, Vector3 face)
        {
            var up = Vector3.up; var rot = Quaternion.LookRotation(face);
            mk.Box(redPaint, at + up * 1.3f, new Vector3(1f, 2.6f, 1f), rot); mk.Box(redPaint, at + up * 2.7f, new Vector3(1.1f, .25f, 1.1f), rot); mk.Prism(redPaint, at + up * 2.82f, up, .5f, .2f, 4, .6f);
            foreach (var d in new[] { face, -face, Vector3.Cross(up, face), -Vector3.Cross(up, face) }) mk.Box(GlowM(new Color(1f, .92f, .7f), 1.2f), at + up * 1.55f + d * .51f, new Vector3(Mathf.Abs(Vector3.Dot(d, Vector3.Cross(up, face))) > .5f ? .02f : .7f, 1.3f, Mathf.Abs(Vector3.Dot(d, face)) > .5f ? .02f : .7f), rot);
            mk.Box(GlowM(Color.white, 1.2f), at + up * 2.45f + face * .52f, new Vector3(.7f, .16f, .02f), rot);
            solid.Box(redPaint, at + up * 1.3f, new Vector3(1f, 2.6f, 1f), rot);
            halos.Add(at + up * 1.6f + face * .8f, 1.2f, new Color(1f, .92f, .7f), .5f);
        }

        /// A bus shelter: a glass and steel box with a bench and a timetable that glows.
        void BusShelter(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 at, Vector3 face)
        {
            var up = Vector3.up; var rot = Quaternion.LookRotation(face); var side = Vector3.Cross(up, face);
            foreach (var s in new[] { -1f, 1f }) { mk.Box(steel, at + side * s * 1.6f + up * 1.2f, new Vector3(.08f, 2.4f, 1.4f), rot); ArtistBits.PostCollider(ctx.Root, at + side * s * 1.6f, 2.4f, .1f); }
            mk.Box(steel, at + up * 2.45f, new Vector3(3.4f, .1f, 1.6f), rot); mk.Box(WorldMaterials.Solid(new Color(.6f, .7f, .72f), .9f), at + up * 1.2f - face * .7f, new Vector3(3.2f, 2.2f, .04f), rot);
            mk.Box(wood, at + up * .5f - face * .45f, new Vector3(2.4f, .08f, .4f), rot);
            mk.Box(GlowM(new Color(.9f, .95f, 1f), 1.3f), at + up * 1.4f - face * .67f + side * 1.1f, new Vector3(.6f, .8f, .02f), rot);
            solid.Box(steel, at + up * 1.2f - face * .7f, new Vector3(3.2f, 2.4f, .2f), rot);
            var sign = at + side * 2.2f; mk.Prism(steel, sign, up, .05f, 2.6f, 6); mk.Prism(GlowM(new Color(.95f, .95f, .95f), .9f), sign + up * 2.7f - face * .03f, face, .3f, .06f, 14); ArtistBits.PostCollider(ctx.Root, sign, 2.6f, .1f);
        }

        /// A stone cottage with a slate roof, a chimney and a lit window or two.
        void Cottage(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 at, Vector3 face)
        {
            var up = Vector3.up; var rot = Quaternion.LookRotation(face); var side = Vector3.Cross(up, face);
            var w = ctx.Range(6f, 8f); mk.Box(stone, at + up * 1.6f, new Vector3(w, 3.2f, 4.6f), rot); solid.Box(stone, at + up * 1.6f, new Vector3(w, 3.2f, 4.6f), rot);
            mk.GableRoof(slate, at + up * 3.2f, new Vector3(5.2f, 0, w + .6f), 1.8f, Quaternion.LookRotation(side));
            mk.Box(dark, at + up * 1f + face * 2.31f, new Vector3(.9f, 2f, .04f), rot);
            foreach (var s in new[] { -1f, 1f }) mk.Box(ctx.Chance(.7f) ? GlowM(Lamp, 1.4f) : dark, at + up * 1.7f + face * 2.31f + side * s * (w / 2f - 1.2f), new Vector3(.9f, .8f, .04f), rot);
            var ch = at + up * 4f + side * (w / 2f - .5f); mk.Box(stone, ch, new Vector3(.7f, 2f, .7f), rot); life.chimneys.Add(ch + up * 1.1f);
            noGrass.Add(new Vector3(at.x, w / 2f + 1f, at.z));
        }

        // ---------------------------------------------------------------- the arrival
        /// Where you arrive: a crossroads of moor roads with a red phone box, a lonely bus stop, a fingerpost and street
        /// lamps; dry-stone walls running off over the hills.
        void Arrival(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var c = ctx.OnGround(center.x, center.z);
            mk.Prism(WorldMaterials.Solid(new Color(.36f, .36f, .38f)), c - up * .25f, up, 9f, .35f, 16);
            var toHill = hasHill ? new Vector3(hill2.x - c.x, 0, hill2.y - c.z).normalized : Vector3.forward; var sd = Vector3.Cross(up, toHill);
            PhoneBox(ctx, mk, solid, ctx.OnGround(c.x + sd.x * 6f, c.z + sd.z * 6f), -sd);
            BusShelter(ctx, mk, solid, ctx.OnGround(c.x - sd.x * 7f - toHill.x * 2f, c.z - sd.z * 7f - toHill.z * 2f), sd);
            var fp = ctx.OnGround(c.x - toHill.x * 6f + sd.x * 3f, c.z - toHill.z * 6f + sd.z * 3f); mk.Prism(white, fp, up, .1f, 3f, 6); ArtistBits.PostCollider(ctx.Root, fp, 3f, .1f);
            mk.Box(white, fp + up * 2.7f + toHill * .7f, new Vector3(.1f, .3f, 1.4f), Quaternion.LookRotation(toHill));
            ArtistBits.Label(ctx.Root, "THE ALT ROCK MOORS", fp + up * 2.7f + toHill * .7f + sd * .06f, sd, .1f, new Color(.1f, .1f, .1f), TextAnchor.MiddleCenter);
            for (var k = 0; k < 4; k++) { var a = (k * 90f + 45f) * Mathf.Deg2Rad; StreetLamp(ctx, mk, ctx.OnGround(c.x + Mathf.Cos(a) * 10f, c.z + Mathf.Sin(a) * 10f)); }
            noGrass.Add(new Vector3(c.x, 10f, c.z));
            Remember(c, 0);
        }

        // ---------------------------------------------------------------- the transmitter
        /// A giant lattice radio mast 90 m tall on the top of the tallest hill, guyed with cables, orange warning lights up
        /// its height that blink, a small concrete hut at its foot with a humming light.
        void Transmitter(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var c = ctx.OnGround(hill2.x, hill2.y); const float H = 90f;
            var red = WorldMaterials.Solid(new Color(.8f, .25f, .18f)); var whiteM = WorldMaterials.Solid(new Color(.88f, .88f, .86f));
            var legs = new Vector3[3]; for (var k = 0; k < 3; k++) { var a = k * 120f * Mathf.Deg2Rad; legs[k] = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)); }
            const int bands = 18;
            for (var b = 0; b < bands; b++)
            {
                float y0 = b * H / bands, y1 = (b + 1) * H / bands; float w0 = Mathf.Lerp(4f, 1f, y0 / H), w1 = Mathf.Lerp(4f, 1f, y1 / H); var m = b % 2 == 0 ? red : whiteM;
                for (var k = 0; k < 3; k++)
                {
                    var p0 = c + legs[k] * w0 + up * y0; var p1 = c + legs[k] * w1 + up * y1; mk.Box(m, (p0 + p1) / 2f, new Vector3(.25f, .25f, Vector3.Distance(p0, p1)), Quaternion.LookRotation(p1 - p0));
                    var q1 = c + legs[(k + 1) % 3] * w1 + up * y1; mk.Box(m, (p0 + q1) / 2f, new Vector3(.12f, .12f, Vector3.Distance(p0, q1)), Quaternion.LookRotation(q1 - p0));
                    var r0 = c + legs[(k + 1) % 3] * w0 + up * y0; mk.Box(m, (p0 + r0) / 2f, new Vector3(.12f, .12f, Vector3.Distance(p0, r0)), Quaternion.LookRotation(r0 - p0));
                }
                if (b % 3 == 2) { var lp = c + up * y1; life.warning.Add(lp + legs[0] * w1); life.warning.Add(lp + legs[1] * w1); life.warning.Add(lp + legs[2] * w1); }
            }
            mk.Prism(steel, c + up * H, up, .2f, 12f, 6); life.warning.Add(c + up * (H + 12f));
            solid.Prism(steel, c - up * .3f, up, 4.2f, 8f, 3);
            foreach (var l in legs) { var anchor = ctx.OnGround(c.x + l.x * 40f, c.z + l.z * 40f); mk.Box(concrete, anchor + up * .4f, new Vector3(1.4f, .8f, 1.4f), Quaternion.identity); foreach (var y in new[] { H * .35f, H * .7f }) { var top = c + l * Mathf.Lerp(4f, 1f, y / H) + up * y; mk.Box(dark, (anchor + top) / 2f, new Vector3(.05f, .05f, Vector3.Distance(anchor, top)), Quaternion.LookRotation(top - anchor)); } }
            var hut = ctx.OnGround(c.x + 8f, c.z - 6f); mk.Box(concrete, hut + up * 1.5f, new Vector3(5f, 3f, 4f), Quaternion.identity); solid.Box(concrete, hut + up * 1.5f, new Vector3(5f, 3f, 4f), Quaternion.identity);
            mk.Box(GlowM(new Color(.7f, .85f, 1f), 1.4f), hut + up * 2f + Vector3.forward * 2.01f, new Vector3(1.2f, .6f, .04f), Quaternion.identity);
            for (var k = 0; k < 3; k++) { var dsh = hut + up * 3.4f + Vector3.right * (k - 1f) * 1.4f; mk.Prism(steel, dsh, up, .05f, .6f, 6); mk.Prism(white, dsh + up * .9f - Vector3.forward * .1f, (Vector3.forward + up * .8f).normalized, .6f, .12f, 14, .4f); }
            life.mast = c + up * (H + 12f);
            noGrass.Add(new Vector3(c.x, 10f, c.z));
            var mark = new GameObject("Shot: the transmitter").transform; mark.SetParent(ctx.Root, false);
            var back = new Vector3(c.x - center.x, 0, c.z - center.z).normalized; var cam = ctx.OnGround(c.x - back.x * 140f, c.z - back.z * 140f); var look = c + up * 45f - cam;
            mark.SetPositionAndRotation(cam, Quaternion.LookRotation(new Vector3(look.x, look.y * .55f, look.z).normalized));
        }

        // ---------------------------------------------------------------- trails
        void Trails(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var todo = new List<IslandWorldData.Artist>(ctx.Artists); var walk = 1; var lamps = new List<Vector3>();
            var road = WorldMaterials.Solid(new Color(.38f, .38f, .4f)); var longest = new List<Vector3>();
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
                for (var d = walk == 1 ? 9f : TrailW / 2f + .1f; ; d += 1.5f)
                {
                    var dd = Mathf.Min(d, end); var q = from + dir * dd + side * Mathf.Sin(dd / 17f) * Mathf.Min(3f, end * .05f);
                    if (!ctx.IsLand(q.x, q.z, 0f)) break;
                    pts.Add(ctx.OnGround(q.x, q.z)); if (dd >= end) break;
                }
                if (pts.Count < 2) continue;
                Strip(mk, pts, road, TrailW, .2f, ctx);
                foreach (var p in pts) Remember(p, walk);
                if (pts.Count > longest.Count) longest = pts;
                for (var i = 7; i < pts.Count; i += 14)
                {
                    var lp = pts[i] + side * ((i / 14) % 2 == 0 ? 1f : -1f) * (TrailW / 2f + .6f);
                    var near = false; foreach (var o in lamps) if ((o - lp).sqrMagnitude < 22f * 22f) near = true;
                    if (near) continue;
                    lamps.Add(lp); StreetLamp(ctx, mk, ctx.OnGround(lp.x, lp.z));
                }
                walk++;
            }
            life.road = longest.ToArray();
            if (longest.Count > 10)
            {
                var m = longest[longest.Count / 2]; var d2 = longest[longest.Count / 2 + 1] - m; d2.y = 0; d2.Normalize(); var sd = Vector3.Cross(Vector3.up, d2);
                var mark = new GameObject("Shot: the lonely road").transform; mark.SetParent(ctx.Root, false); var cam = ctx.OnGround(m.x + sd.x * 9f, m.z + sd.z * 9f); mark.SetPositionAndRotation(cam, Quaternion.LookRotation((d2 - sd * .5f).normalized));
            }
        }

        // ---------------------------------------------------------------- the moor
        /// Dry-stone walls over the hills, power-line pylons striding across with sagging wires, little stone hamlets,
        /// gorse and heather, boulders and tors, lone hawthorns bent by the wind, sheep.
        void Moor(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            // walls: long lines across the land at a few angles
            for (var k = 0; k < 9; k++)
            {
                var a = (k * 37f + 11f) * Mathf.Deg2Rad; var dir = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)); var off = new Vector3(-dir.z, 0, dir.x) * ((k - 4f) * 55f);
                var mid = new Vector3(center.x, 0, center.z) + off; DryWall(ctx, mk, solid, mid - dir * 400f, mid + dir * 400f);
            }
            // pylons
            var pd = hasHill ? new Vector3(hill2.x - center.x, 0, hill2.y - center.z).normalized : Vector3.forward; pd = Quaternion.Euler(0, 70f, 0) * pd; var po = new Vector3(center.x, 0, center.z) + Vector3.Cross(Vector3.up, pd) * 60f;
            var pylons = new List<Vector3>();
            for (var t = -400f; t <= 400f; t += 70f) { var p = po + pd * t; if (!ctx.IsLand(p.x, p.z, 8f) || InFootprint(ctx, p.x, p.z, 6f) || NearPath(p.x, p.z, 4f)) { if (pylons.Count > 0) Wires(mk, pylons); pylons.Clear(); continue; } var g = ctx.OnGround(p.x, p.z); Pylon(ctx, mk, solid, g, pd); pylons.Add(g); }
            if (pylons.Count > 0) Wires(mk, pylons);
            // hamlets: two little clusters of cottages near trails
            var hamlets = 0;
            for (var tries = 0; tries < 300 && hamlets < 2; tries++)
            {
                var px = ctx.Range(ctx.MinX, ctx.MaxX); var pz = ctx.Range(ctx.MinZ, ctx.MaxZ);
                if (!Open(ctx, px, pz, 18f) || !NearPath(px, pz, 30f) || Vector2.Distance(new Vector2(px, pz), new Vector2(center.x, center.z)) < 60f) continue;
                for (var k = 0; k < 5; k++) { var a = k * 72f * Mathf.Deg2Rad + ctx.Range(-.3f, .3f); var at = ctx.OnGround(px + Mathf.Cos(a) * 10f, pz + Mathf.Sin(a) * 10f); Cottage(ctx, mk, solid, at, new Vector3(px - at.x, 0, pz - at.z).normalized); }
                StreetLamp(ctx, mk, ctx.OnGround(px, pz)); hamlets++;
            }
            int tors = 0, thorns = 0, gorse = 0, sheep = 0;
            const float cell = 18f; var thornBark = WorldMaterials.Solid(new Color(.3f, .26f, .22f)); var thornLeaf = WorldMaterials.Solid(new Color(.32f, .4f, .26f)); var gorseM = WorldMaterials.Solid(new Color(.3f, .4f, .22f)); var gorseFlower = WorldMaterials.Solid(new Color(1f, .82f, .2f));
            var wool = WorldMaterials.Solid(new Color(.88f, .86f, .8f));
            for (var x = ctx.MinX; x < ctx.MaxX; x += cell)
            for (var z = ctx.MinZ; z < ctx.MaxZ; z += cell)
            {
                float px = x + ctx.Range(0f, cell), pz = z + ctx.Range(0f, cell);
                if (!Open(ctx, px, pz, 3f)) continue;
                var p = ctx.OnGround(px, pz); var roll = ctx.Range(0f, 1f);
                if (roll < .08f) { var n = ctx.Rng.Next(2, 5); for (var k = 0; k < n; k++) mk.Box(stone, p + new Vector3(ctx.Range(-2f, 2f), .8f + k * .5f, ctx.Range(-2f, 2f)), new Vector3(ctx.Range(1.5f, 3f), ctx.Range(.8f, 1.6f), ctx.Range(1.4f, 2.6f)), Quaternion.Euler(0, ctx.Range(0, 90f), ctx.Range(-6f, 6f))); solid.Box(stone, p + Vector3.up * 1.3f, new Vector3(4f, 2.6f, 4f), Quaternion.identity); tors++; }
                else if (roll < .18f) { var lean = (Quaternion.Euler(0, 40f, 0) * Vector3.forward + Vector3.up * 1.6f).normalized; mk.Prism(thornBark, p - Vector3.up * .2f, lean, .25f, 3.2f, 6, .6f); var top = p + lean * 3f; for (var k = 0; k < 4; k++) soft.Blob(thornLeaf, top + Quaternion.Euler(0, k * 90f, 0) * Vector3.forward * 1f + Vector3.Cross(lean, Vector3.up) * .3f, new Vector3(1.4f, .9f, 1.2f), ctx.Rng, .35f); ArtistBits.PostCollider(ctx.Root, p, 3f, .3f); thorns++; }
                else if (roll < .3f) { for (var k = 0; k < 3; k++) { var gp = p + new Vector3(ctx.Range(-1.2f, 1.2f), .5f, ctx.Range(-1.2f, 1.2f)); soft.Blob(gorseM, gp, new Vector3(.9f, .7f, .9f), ctx.Rng, .35f); for (var f = 0; f < 4; f++) soft.Blob(gorseFlower, gp + new Vector3(ctx.Range(-.7f, .7f), ctx.Range(-.2f, .6f), ctx.Range(-.7f, .7f)), Vector3.one * .12f, ctx.Rng, 0f); } gorse++; }
                else if (roll < .34f) { var face = Quaternion.Euler(0, ctx.Range(0, 360f), 0) * Vector3.forward; mk.Blob(wool, p + Vector3.up * .7f, new Vector3(.55f, .45f, .8f), ctx.Rng, .2f); mk.Blob(dark, p + Vector3.up * .85f + face * .7f, new Vector3(.18f, .2f, .25f), ctx.Rng, 0f); foreach (var s in new[] { -.2f, .2f }) foreach (var f in new[] { -.4f, .4f }) mk.Box(dark, p + Vector3.up * .25f + Vector3.Cross(Vector3.up, face) * s + face * f, new Vector3(.08f, .5f, .08f), Quaternion.identity); sheep++; }
            }
            Debug.Log($"The Alt Rock Moors: hill {(hasHill ? "yes" : "no")}, {tors} tors, {thorns} hawthorns, {gorse} gorse, {sheep} sheep, {hamlets} hamlets");
        }

        void Pylon(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 g, Vector3 along)
        {
            var up = Vector3.up; var side = Vector3.Cross(up, along); const float H = 24f;
            foreach (var s in new[] { -1f, 1f }) foreach (var f in new[] { -1f, 1f }) { var foot = g + side * s * 2.6f + along * f * 2.6f; var top = g + side * s * .7f + along * f * .7f + up * H; mk.Box(steel, (foot + top) / 2f, new Vector3(.18f, .18f, Vector3.Distance(foot, top)), Quaternion.LookRotation(top - foot)); }
            for (var y = 4f; y < H; y += 5f) { var w = Mathf.Lerp(2.6f, .7f, y / H); mk.Box(steel, g + up * y, new Vector3(w * 2f, .1f, w * 2f), Quaternion.LookRotation(along)); }
            foreach (var y in new[] { H * .7f, H * .88f }) mk.Box(steel, g + up * y, new Vector3(9f, .25f, .4f), Quaternion.LookRotation(along));
            solid.Box(steel, g + up * 3f, new Vector3(5f, 6f, 5f), Quaternion.LookRotation(along));
        }

        void Wires(MeshKit mk, List<Vector3> pylons)
        {
            for (var i = 0; i + 1 < pylons.Count; i++)
                foreach (var y in new[] { 24f * .7f, 24f * .88f })
                    foreach (var s in new[] { -4.2f, 4.2f })
                    {
                        var a = pylons[i]; var b = pylons[i + 1]; var along = (b - a); along.y = 0; along.Normalize(); var side = Vector3.Cross(Vector3.up, along);
                        var p0 = a + side * s + Vector3.up * y; var p1 = b + side * s + Vector3.up * y; Vector3 prev = p0;
                        for (var k = 1; k <= 8; k++) { var t = k / 8f; var p = Vector3.Lerp(p0, p1, t) - Vector3.up * Mathf.Sin(t * Mathf.PI) * 2.4f; mk.Box(dark, (prev + p) / 2f, new Vector3(.05f, .05f, Vector3.Distance(prev, p) + .02f), Quaternion.LookRotation(p - prev)); prev = p; }
                    }
        }

        void GroundCover(LandContext ctx)
        {
            var mat = WindMesh.Material(.3f, 1f, new Color(.4f, .47f, .32f));
            var meshes = new Dictionary<(int, int), WindMesh>(); var rng = new System.Random(1997);
            const float step = 2.3f; Color[] flowers = { new Color(.7f, .5f, .9f), new Color(.62f, .45f, .8f), new Color(1f, .85f, .25f) };
            for (var x = ctx.MinX; x < ctx.MaxX; x += step)
            for (var z = ctx.MinZ; z < ctx.MaxZ; z += step)
            {
                var px = x + ((float)rng.NextDouble() - .5f) * step; var pz = z + ((float)rng.NextDouble() - .5f) * step;
                if (!ctx.IsLand(px, pz, 1f) || NearPath(px, pz, 2.4f) || InNoGrass(px, pz, 0f) || InFootprint(ctx, px, pz, -2f)) continue;
                if (Mathf.PerlinNoise(px * .03f + 7f, pz * .03f + 3f) < .32f) continue;
                var key = (Mathf.FloorToInt(px / 70f), Mathf.FloorToInt(pz / 70f));
                if (!meshes.TryGetValue(key, out var wm)) meshes[key] = wm = new WindMesh();
                var col = Color.Lerp(new Color(.38f, .45f, .3f), new Color(.52f, .54f, .38f), (float)rng.NextDouble());
                var at = new Vector3(px, ctx.Ground(px, pz) - .04f, pz);
                wm.Tuft(at, .4f + (float)rng.NextDouble() * .45f, col, Color.Lerp(col, new Color(.8f, .8f, .65f), .3f), rng, 4, .06f);
                if (rng.NextDouble() < (Mathf.PerlinNoise(px * .02f + 1f, pz * .02f + 9f) > .55f ? .45 : .06)) wm.Flower(at + new Vector3(.1f, 0, .1f), .3f, flowers[rng.Next(flowers.Length)], rng);   // heather in drifts
            }
            foreach (var kv in meshes) kv.Value.Build("Moors grass", ctx.Root, mat);
        }
    }
}
