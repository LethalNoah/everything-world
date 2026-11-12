using System.Collections.Generic;
using UnityEngine;
using static EverythingWorld.World.PlaceTools;

namespace EverythingWorld.World
{
    /// The Fusion Isthmus (WORLD_GUIDE.md): a little brick canal town at blue hour, smooth and warm. You arrive in a
    /// small brick square; Brass Lane leads north to the canal, a teal ribbon of water cut straight through the town
    /// between two quays lined with tall narrow brick houses, their windows lit, pots of plants on every sill, strings
    /// of lights over the water. The Piano Bridge crosses it, its deck laid out as keys. Beside the bridge the Rooftop,
    /// the town's club, is up an outside stair on the flat roof of the tallest house: a trio, mismatched chairs, the
    /// sky going from teal to indigo. A houseboat is moored below. Out past the houses: plane trees strung with
    /// lights, planters, benches, buskers on the corners.
    /// (The plan called this a strip between two seas; the map has no such seas, so the water is the canal.)
    /// Laid out on purpose (the build-land standard): FusionTown.cs has the places and the people.
    public partial class FusionKit : LandKit
    {
        public override string Land => "The Fusion Isthmus";
        public override Vector3 ArtistFront(IslandWorldData data, IslandWorldData.Artist a) => facing.TryGetValue(a.name, out var f) ? f : base.ArtistFront(data, a);

        public static readonly Color Teal = new Color(.2f, .78f, .8f), Brass = new Color(1f, .78f, .38f), Brick = new Color(.66f, .36f, .27f), Indigo = new Color(.35f, .36f, .78f), Cream = new Color(.96f, .92f, .82f), Ink = new Color(.1f, .1f, .14f);
        static readonly Color[] Paints = { new Color(.66f, .36f, .27f), new Color(.56f, .3f, .24f), new Color(.2f, .46f, .5f), new Color(.3f, .32f, .58f), new Color(.86f, .8f, .68f), new Color(.72f, .46f, .3f) };
        static readonly Color[] Bloom = { new Color(1f, .5f, .4f), new Color(1f, .82f, .35f), new Color(.95f, .5f, .75f), new Color(.6f, .8f, 1f) };
        const float PathW = 3.2f, CanalHalf = 4.6f, QuayW = 7f, CanalOff = 44f, HouseD = 8f;

        Material brick, brickDark, paving, stoneM, brassM, iron, dark, white, leafM, leafDark, woodM, waterM, lampM, windowM, glassDark;
        // the strings of lights: three sets, so they can twinkle in turn
        readonly Material[] bulbM = new Material[3];
        GlowHalos halos; Vector3 center, A, Nrm, canalC; FusionLife life;
        readonly Dictionary<(int, int), List<Vector3>> pathCells = new Dictionary<(int, int), List<Vector3>>();
        readonly List<(Vector3 p, int w)> walkPoints = new List<(Vector3, int)>();
        readonly Dictionary<string, Vector3> facing = new Dictionary<string, Vector3>();
        // x, radius, z
        readonly List<Vector3> noGrass = new List<Vector3>();
        Vector2 heart2, A2, N2, canal2; float canalL, quayY, waterY;

        void MakeMaterials()
        {
            brick = WorldMaterials.Solid(Brick); brickDark = WorldMaterials.Solid(new Color(.42f, .24f, .2f));
            // paving and stone are faintly self-lit, so dusk never swallows the streets
            paving = WorldMaterials.Glow(new Color(.7f, .56f, .48f), .1f); stoneM = WorldMaterials.Glow(new Color(.72f, .72f, .74f), .1f);
            brassM = WorldMaterials.Solid(new Color(.86f, .66f, .28f), .8f); iron = WorldMaterials.Solid(new Color(.14f, .15f, .18f), .5f); dark = WorldMaterials.Solid(new Color(.08f, .08f, .1f));
            white = WorldMaterials.Solid(new Color(.95f, .94f, .9f)); woodM = WorldMaterials.Solid(new Color(.55f, .38f, .24f));
            leafM = WorldMaterials.Solid(new Color(.3f, .52f, .34f)); leafDark = WorldMaterials.Solid(new Color(.2f, .4f, .32f)); glassDark = WorldMaterials.Solid(new Color(.2f, .28f, .36f), .8f);
            waterM = new Material(WorldMaterials.Glow(new Color(.1f, .55f, .6f), .3f)) { name = "Canal water" };
            lampM = new Material(WorldMaterials.Glow(Brass, 1.6f)) { name = "Fusion lamps" };
            windowM = new Material(WorldMaterials.Glow(new Color(1f, .8f, .5f), 1.15f)) { name = "Fusion windows" };
            for (var i = 0; i < 3; i++) bulbM[i] = new Material(WorldMaterials.Glow(new Color(1f, .86f, .58f), 1.6f)) { name = "Fusion bulbs " + i };
        }

        Material GlowM(Color c, float s = 2f) => WorldMaterials.Glow(c, s * .65f);
        static bool HasStation(IslandWorldData.Artist a) => a.stationId != 0;
        static float Foot(IslandWorldData.Artist a)
        {
            switch (a.name)
            {
                case "Tom Misch": return 17f;
                case "Yussef Dayes": case "Ezra Collective": return 13f;
                case "Yussef Kamaal": case "CASIOPEA": case "Pat Metheny Group": case "Allan Holdsworth": case "Victor Wooten": case "Medeski, Martin & Wood": return 10f;
                default: return HasStation(a) ? 12f : 8f;
            }
        }

        public override bool Ground(float height, float slope, float patch, out Color color)
        {
            // dusk-blue grass with warmer patches; the canal bed is dark teal
            if (slope > 26f) color = Color.Lerp(new Color(.3f, .26f, .28f), new Color(.38f, .32f, .32f), patch);
            else color = Color.Lerp(new Color(.26f, .46f, .42f), new Color(.42f, .54f, .38f), Mathf.SmoothStep(0f, 1f, patch));
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
            bool Inside(Vector2 p, float m) { if (data.IslandAt(p.x, p.y) != isl) return false; for (var k = 0; k < 12; k++) { var a = k * Mathf.PI / 6f; if (data.IslandAt(p.x + Mathf.Cos(a) * m, p.y + Mathf.Sin(a) * m) != isl) return false; } return true; }
            // the canal runs east-west, north of the square, as long as the land allows
            A2 = new Vector2(1, 0); N2 = new Vector2(0, 1); canal2 = heart2 + N2 * CanalOff; canalL = 130f;
            while (canalL > 60f && !(Inside(canal2 + A2 * (canalL + 20f), 30f) && Inside(canal2 - A2 * (canalL + 20f), 30f))) canalL -= 10f;
            quayY = Mathf.Max(data.HeightAt(heart2.x, heart2.y), waterY + 5f);

            var keep = new List<string>();
            void Put(string name, Vector2 p, float room) { if (!Inside(p, room)) return; foreach (var a in data.layout.artists) if (a.sceneId == isl.sceneId && a.name == name) { a.x = p.x; a.z = p.y; keep.Add(name); } }
            Put("Tom Misch", heart2 - A2 * 72f - N2 * 18f, 34f); Put("Yussef Dayes", heart2 + A2 * 68f - N2 * 20f, 30f); Put("Yussef Kamaal", heart2 + A2 * 118f - N2 * 34f, 26f); Put("Ezra Collective", heart2 - N2 * 84f + A2 * 6f, 30f);
            // the older fusion players, north of the canal beyond the houses
            var north = CanalOff + CanalHalf + QuayW + HouseD + 44f;
            Put("CASIOPEA", heart2 - A2 * 84f + N2 * north, 26f); Put("Pat Metheny Group", heart2 - A2 * 28f + N2 * (north + 14f), 26f); Put("Allan Holdsworth", heart2 + A2 * 30f + N2 * (north + 14f), 26f); Put("Victor Wooten", heart2 + A2 * 86f + N2 * north, 26f); Put("Medeski, Martin & Wood", heart2 + A2 * 142f + N2 * (north - 12f), 26f);
            var avoid = new List<Vector3>();
            for (var d = -canalL - 10f; d <= canalL + 10f; d += 26f) avoid.Add(new Vector3(canal2.x + A2.x * d, 36f, canal2.y + A2.y * d));
            avoid.Add(new Vector3(heart2.x, 22f, heart2.y + CanalOff + 60f));
            ArtistPlacement.Spread(data, index, 56f, heart2, 44f, 40f, avoid, keep, Land, 330f);

            // level the square and the whole canal corridor to one height, then cut the canal into it
            Level(data, index, heart2, 20f, 34f, quayY);
            var half = CanalHalf + QuayW + HouseD + 4f;
            for (var r = 0; r < data.Nz; r++)
            {
                var z = data.layout.depth - r * data.Step;
                for (var col = 0; col < data.Nx; col++)
                {
                    var i = r * data.Nx + col; if (data.islandIds[i] != index) continue;
                    var x = col * data.Step; var rel = new Vector2(x, z) - canal2; var al = Mathf.Abs(Vector2.Dot(rel, A2)); var pe = Mathf.Abs(Vector2.Dot(rel, N2));
                    if (al > canalL + 34f || pe > half + 16f) continue;
                    var w = (1f - Ease(half, half + 16f, pe)) * (1f - Ease(canalL + 16f, canalL + 34f, al));
                    data.heights[i] = Mathf.Lerp(data.heights[i], quayY, w);
                    if (al < canalL && pe < CanalHalf + 1.2f) data.heights[i] = Mathf.Lerp(quayY - 2.8f, data.heights[i], Ease(CanalHalf - .2f, CanalHalf + 1.2f, pe));
                }
            }
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
            center = ctx.Plaza; A = new Vector3(A2.x, 0, A2.y); Nrm = new Vector3(N2.x, 0, N2.y); canalC = new Vector3(canal2.x, quayY, canal2.y);
            life = new GameObject("Fusion life").AddComponent<FusionLife>();
            life.transform.SetParent(ctx.Root, false);
            life.center = center; life.lamps = lampM; life.windows = windowM; life.bulbs = bulbM; life.water = waterM;
            var mk = new MeshKit(); var solid = new MeshKit();
            Remember(ctx.OnGround(center.x, center.z), 0);
            TownStart(ctx);
            Square(ctx, mk, solid);
            Canal(ctx, mk, solid);
            Houses(ctx, mk, solid);
            Paths(ctx, mk, solid);
            Wilds(ctx, mk, solid);
            mk.Build("The Fusion Isthmus", ctx.Root, false);
            solid.BuildCollider("The Fusion Isthmus walls", ctx.Root);
        }

        public override void Finish(LandContext ctx) { Verge(ctx); halos.Build("Isthmus glow", ctx.Root); }

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

        /// Inside the canal corridor (the water, the quays and the houses), plus 'pad'.
        bool InTown(float x, float z, float pad)
        {
            var rel = new Vector2(x, z) - canal2; return Mathf.Abs(Vector2.Dot(rel, A2)) < canalL + 14f + pad && Mathf.Abs(Vector2.Dot(rel, N2)) < CanalHalf + QuayW + HouseD + 3f + pad;
        }

        bool Open(LandContext ctx, float x, float z, float pad) =>
            ctx.IsLand(x, z, 5f) && !InTown(x, z, pad) && !InFootprint(ctx, x, z, pad) && !NearPath(x, z, pad + 2f) && !InNoGrass(x, z, pad);

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

        /// A brass street lamp: an iron post, a curled arm, a warm lantern. (All the lanterns are one material.)
        void BrassLamp(LandContext ctx, MeshKit mk, Vector3 g, Vector3 arm, bool light = false)
        {
            var up = Vector3.up; arm.y = 0; arm = arm.sqrMagnitude > .01f ? arm.normalized : Vector3.forward;
            mk.Prism(iron, g - up * .2f, up, .08f, 3.8f, 6); mk.Prism(iron, g, up, .2f, .3f, 8, .5f); mk.Prism(brassM, g + up * 3.5f, (arm + up * .3f).normalized, .04f, .8f, 5);
            var lp = g + up * 3.5f + arm * .75f; mk.Box(lampM, lp, new Vector3(.3f, .42f, .3f), Quaternion.LookRotation(arm)); mk.Cone(brassM, lp + up * .21f, .26f, .2f, 4, 45f); halos.Add(lp, 2f, Brass, .6f); ArtistBits.PostCollider(ctx.Root, g, 3.6f, .12f);
            if (light) ArtistBits.PointLight(ctx.Root, lp - up * .4f, Brass, 16f, 2f);
        }

        /// A string of lights from a to b, sagging; the bulbs take the three bulb materials in turn.
        void BulbString(MeshKit mk, Vector3 a, Vector3 b, int phase = 0)
        {
            var len = Vector3.Distance(a, b); var n = Mathf.Max(3, Mathf.RoundToInt(len / 1.1f)); var sag = Mathf.Min(1.2f, len * .07f); var prev = a;
            for (var i = 1; i <= n; i++)
            {
                var t = i / (float)n; var p = Vector3.Lerp(a, b, t) - Vector3.up * Mathf.Sin(t * Mathf.PI) * sag;
                mk.Prism(dark, prev, (p - prev).normalized, .012f, Vector3.Distance(prev, p), 3); prev = p;
                if (i < n) { mk.Blob(bulbM[(i + phase) % 3], p - Vector3.up * .08f, Vector3.one * .075f, new System.Random(i), 0f); if (i % 4 == 0) halos.Add(p - Vector3.up * .08f, .7f, Brass, .32f); }
            }
        }

        /// A plane tree: a pale mottled trunk, a rounded crown, a few bulbs in its branches.
        void PlaneTree(LandContext ctx, MeshKit mk, Vector3 g, float s, System.Random rng)
        {
            var up = Vector3.up; mk.Prism(WorldMaterials.Solid(new Color(.6f, .58f, .5f)), g - up * .3f, up, .26f * s, 3.4f * s, 7, .7f); ArtistBits.PostCollider(ctx.Root, g, 3f * s, .28f * s);
            for (var k = 0; k < 3; k++) { var d = Quaternion.Euler(0, k * 120f + (float)rng.NextDouble() * 60f, 0) * Vector3.forward; mk.Blob(k % 2 == 0 ? leafM : leafDark, g + up * (4.4f * s + (float)rng.NextDouble() * .6f) + d * 1.3f * s, new Vector3(2.2f, 1.7f, 2.2f) * s, rng, .2f); }
            mk.Blob(leafM, g + up * 5.5f * s, new Vector3(2.2f, 1.5f, 2.2f) * s, rng, .2f);
            for (var k = 0; k < 6; k++) { var d = Quaternion.Euler((float)rng.NextDouble() * 60f - 10f, k * 60f, 0) * Vector3.forward; mk.Blob(bulbM[k % 3], g + up * 4.6f * s + d * 2.4f * s, Vector3.one * .08f, rng, 0f); }
        }

        /// A pot with a plant in it.
        void Pot(MeshKit mk, Vector3 g, float s, System.Random rng, bool flower = true)
        {
            var up = Vector3.up; mk.Prism(WorldMaterials.Solid(new Color(.72f, .42f, .3f)), g, up, .2f * s, .34f * s, 8, 1.3f); mk.Blob(rng.Next(2) == 0 ? leafM : leafDark, g + up * .6f * s, new Vector3(.34f, .34f, .34f) * s, rng, .3f);
            if (flower) mk.Blob(WorldMaterials.Glow(Bloom[rng.Next(Bloom.Length)], .35f), g + up * .86f * s, Vector3.one * .12f * s, rng, .2f);
        }

        /// A bench to sit on: wooden slats on iron ends.
        void Bench(MeshKit mk, Vector3 g, Vector3 face, float len = 2f)
        {
            var up = Vector3.up; var rot = Quaternion.LookRotation(face); var side = Vector3.Cross(up, face);
            mk.Box(woodM, g + up * .45f, new Vector3(len, .07f, .5f), rot); mk.Box(woodM, g + up * .82f - face * .24f, new Vector3(len, .4f, .06f), rot); foreach (var e in new[] { -1f, 1f }) mk.Box(iron, g + up * .32f + side * e * (len / 2f - .12f) - face * .08f, new Vector3(.06f, .95f, .5f), rot);
            BoxCol(rootT, g + up * .5f, new Vector3(len, 1f, .6f), rot); Interact.Bench(tlife.transform, g + up * .52f - side * (len / 2f - .4f), g + up * .52f + side * (len / 2f - .4f), face);
        }

        /// A mismatched chair to sit on: i picks which kind.
        void Chair(MeshKit mk, Vector3 g, Vector3 face, int i)
        {
            var up = Vector3.up; var rot = Quaternion.LookRotation(face); var m = i % 3 == 0 ? woodM : i % 3 == 1 ? WorldMaterials.Solid(Paints[(i + 2) % Paints.Length]) : iron;
            mk.Box(m, g + up * .45f, new Vector3(.46f, .06f, .46f), rot); mk.Box(m, g + up * (.75f + (i % 2) * .1f) - face * .21f, new Vector3(.46f, .5f + (i % 2) * .2f, .05f), rot); mk.Prism(m, g, up, .03f, .45f, 4); foreach (var e in new[] { -.18f, .18f }) mk.Box(m, g + up * .22f + Vector3.Cross(up, face) * e + face * .18f, new Vector3(.04f, .44f, .04f), rot);
            Interact.Seat(tlife.transform, g + up * .5f, face);
        }

        // ---------------------------------------------------------------- the lanes to the artists
        void Paths(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var walk = 10; var lamps = new List<Vector3>();
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
                Strip(mk, pts, paving, PathW, .14f, ctx);
                foreach (var p in pts) Remember(p, walk);
                for (var i = 8; i < pts.Count; i += 13)
                {
                    var s = (i / 13) % 2 == 0 ? 1f : -1f; var lp = pts[i] + side * s * (PathW / 2f + .8f);
                    var near = false; foreach (var o in lamps) if ((o - lp).sqrMagnitude < 17f * 17f) near = true;
                    if (near || InFootprint(ctx, lp.x, lp.z, 1f) || InNoGrass(lp.x, lp.z, 1f) || InTown(lp.x, lp.z, 0f)) continue;
                    lamps.Add(lp); BrassLamp(ctx, mk, ctx.OnGround(lp.x, lp.z), -side * s, true);
                }
                walk++;
            }
            var todo = new List<IslandWorldData.Artist>(ctx.Artists);
            while (todo.Count > 0)
            {
                IslandWorldData.Artist next = null; Vector3 join = default; var best = float.MaxValue;
                foreach (var a in todo)
                    foreach (var (p, w) in walkPoints) { if (w == 5) continue; var d = Vector2.Distance(new Vector2(p.x, p.z), new Vector2(a.x, a.z)) - Foot(a); if (d < best) { best = d; next = a; join = p; } }
                if (next == null) break;
                todo.Remove(next);
                Lay(join, new Vector3(next.x, 0, next.z), Foot(next) * .8f, next.name);
            }
        }

        // ---------------------------------------------------------------- out of town
        /// Plane trees strung with a few lights, planters of flowers, benches, buskers on the corners.
        void Wilds(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            int trees = 0, planters = 0, benches = 0, buskers = 0; var rng = new System.Random(1982);
            const float cell = 20f;
            for (var x = ctx.MinX; x < ctx.MaxX; x += cell)
            for (var z = ctx.MinZ; z < ctx.MaxZ; z += cell)
            {
                float px = x + (float)rng.NextDouble() * cell, pz = z + (float)rng.NextDouble() * cell; var roll = (float)rng.NextDouble(); var s = (float)rng.NextDouble();
                if (!Open(ctx, px, pz, 4f)) continue;
                var p = ctx.OnGround(px, pz);
                if (roll < .2f) { PlaneTree(ctx, mk, p, .9f + s * .5f, rng); trees++; }
                else if (roll < .25f) { Planter(ctx, mk, p, rng); planters++; }
                else if (roll < .275f) { Bench(mk, p, FaceTo(p, NearestWalk(p))); benches++; }
                else if (roll < .34f) { if (buskers < 7 && NearPath(px, pz, 14f)) { Busker(ctx, mk, p, FaceTo(p, NearestWalk(p)), buskers); buskers++; } }
            }
            Debug.Log($"The Fusion Isthmus: canal {canalL * 2f:0} m long, {trees} plane trees, {planters} planters, {benches} benches, {buskers} buskers");
        }

        Vector3 NearestWalk(Vector3 p) { var best = center; var bd = float.MaxValue; foreach (var (q, _) in walkPoints) { var d = (q - p).sqrMagnitude; if (d < bd) { bd = d; best = q; } } return best; }

        /// A raised brick bed of plants and flowers.
        void Planter(LandContext ctx, MeshKit mk, Vector3 g, System.Random rng)
        {
            var up = Vector3.up; var rot = Quaternion.Euler(0, (float)rng.NextDouble() * 180f, 0); mk.Box(brick, g + up * .25f, new Vector3(2.6f, .5f, 1.2f), rot); BoxCol(ctx.Root, g + up * .25f, new Vector3(2.6f, .5f, 1.2f), rot);
            for (var k = 0; k < 5; k++) { var q = g + rot * new Vector3(-1f + k * .5f, .5f, ((float)rng.NextDouble() - .5f) * .5f); mk.Blob(k % 2 == 0 ? leafM : leafDark, q + up * .2f, new Vector3(.34f, .3f, .34f), rng, .3f); mk.Blob(WorldMaterials.Glow(Bloom[rng.Next(Bloom.Length)], .4f), q + up * .5f, Vector3.one * .1f, rng, .2f); }
        }

        Vector3 G0(Vector3 p) => ctx0.OnGround(p.x, p.z);

        /// The verges: soft dusk-blue grass, small pale flowers.
        void Verge(LandContext ctx)
        {
            var mat = WindMesh.Material(.25f, .8f, new Color(.3f, .46f, .4f));
            var meshes = new Dictionary<(int, int), WindMesh>(); var rng = new System.Random(1983);
            Color[] flowers = { new Color(1f, .9f, .6f), new Color(.95f, .7f, .8f), new Color(.75f, .9f, 1f) };
            const float st = 2.6f;
            for (var x = ctx.MinX; x < ctx.MaxX; x += st)
            for (var z = ctx.MinZ; z < ctx.MaxZ; z += st)
            {
                var px = x + ((float)rng.NextDouble() - .5f) * st; var pz = z + ((float)rng.NextDouble() - .5f) * st;
                if (!ctx.IsLand(px, pz, 1f) || InTown(px, pz, 1f) || NearPath(px, pz, 2.6f) || InNoGrass(px, pz, 0f) || InFootprint(ctx, px, pz, -2f)) continue;
                if (Mathf.PerlinNoise(px * .04f + 7f, pz * .04f + 2f) < .36f) continue;
                var key = (Mathf.FloorToInt(px / 70f), Mathf.FloorToInt(pz / 70f));
                if (!meshes.TryGetValue(key, out var wm)) meshes[key] = wm = new WindMesh();
                var col = Color.Lerp(new Color(.22f, .4f, .38f), new Color(.36f, .5f, .38f), (float)rng.NextDouble());
                wm.Tuft(new Vector3(px, ctx.Ground(px, pz) - .04f, pz), .26f + (float)rng.NextDouble() * .3f, col, rng.NextDouble() < .16 ? flowers[rng.Next(flowers.Length)] : Color.Lerp(col, new Color(.8f, .85f, .7f), .35f), rng, 3, .05f);
            }
            foreach (var kv in meshes) kv.Value.Build("Isthmus verge", ctx.Root, mat);
        }
    }
}
