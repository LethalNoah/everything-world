using System.Collections.Generic;
using UnityEngine;
using static EverythingWorld.World.PlaceTools;

namespace EverythingWorld.World
{
    /// The Sample Gardens (WORLD_GUIDE.md): an overgrown record-collector's garden in warm late-afternoon light. You arrive
    /// in a brick-paved garden in front of the Greenhouse Studio, a white iron greenhouse open to the air with ferns, a
    /// mixing desk, turntables and speakers growing among the plants; a turntable on a tree stump to drop the needle on;
    /// crates of records and old radios half-buried in the flower beds. The Crate Walk, a brick path under arches of
    /// climbing roses, past stacks of crates and washing lines hung with records, leads to Wintergatan's marble machine.
    /// Warm gold light, deep greens, terracotta and cream.
    /// Laid out on purpose (the build-land standard): GardenTown.cs has the places and the people.
    public partial class GardenKit : LandKit
    {
        public override string Land => "The Sample Gardens";
        public override Vector3 ArtistFront(IslandWorldData data, IslandWorldData.Artist a) => facing.TryGetValue(a.name, out var f) ? f : base.ArtistFront(data, a);

        public static readonly Color Gold = new Color(1f, .78f, .42f), Terracotta = new Color(.76f, .42f, .3f), Cream = new Color(.94f, .9f, .8f), Leaf = new Color(.26f, .5f, .26f), Rose = new Color(.9f, .36f, .44f), Ink = new Color(.1f, .08f, .06f), Vinyl = new Color(.06f, .06f, .07f);
        const float PathW = 2.6f, WalkW = 4f;

        Material brick, brickD, wood, woodD, iron, dark, white, leafM, leafD, trunkM, lampM, glassM, vinylM, dialM, soil;
        GlowHalos halos; Vector3 center, D, S; GardenLife life;
        readonly Dictionary<(int, int), List<Vector3>> pathCells = new Dictionary<(int, int), List<Vector3>>();
        readonly List<(Vector3 p, int w)> walkPoints = new List<(Vector3, int)>();
        readonly Dictionary<string, Vector3> facing = new Dictionary<string, Vector3>();
        // x, radius, z
        readonly List<Vector3> noGrass = new List<Vector3>();
        Vector2 heart2, D2 = new Vector2(0, -1), S2 = new Vector2(1, 0); float plazaY, waterY, streetL;

        void MakeMaterials()
        {
            brick = WorldMaterials.Glow(new Color(.62f, .36f, .28f), .1f); brickD = WorldMaterials.Glow(new Color(.46f, .26f, .2f), .08f); wood = WorldMaterials.Glow(new Color(.56f, .4f, .26f), .1f); woodD = WorldMaterials.Glow(new Color(.36f, .26f, .18f), .08f); iron = WorldMaterials.Solid(new Color(.86f, .86f, .82f), .5f); dark = WorldMaterials.Solid(new Color(.06f, .06f, .07f)); white = WorldMaterials.Glow(new Color(.9f, .88f, .82f), .14f);
            leafM = WorldMaterials.Glow(Leaf, .12f); leafD = WorldMaterials.Glow(new Color(.18f, .36f, .2f), .1f); trunkM = WorldMaterials.Glow(new Color(.4f, .3f, .22f), .1f); soil = WorldMaterials.Glow(new Color(.34f, .24f, .18f), .08f);
            lampM = new Material(WorldMaterials.Glow(Gold, 1.6f)) { name = "Garden lamps" }; glassM = new Material(WorldMaterials.Glow(new Color(.66f, .82f, .78f), .35f)) { name = "Garden glass" }; vinylM = WorldMaterials.Solid(Vinyl, .2f); dialM = new Material(WorldMaterials.Glow(new Color(1f, .7f, .3f), 1.3f)) { name = "Garden radio dials" };
        }

        Material GlowM(Color c, float s = 2f) => WorldMaterials.Glow(c, s * .65f);
        Material Paint(Color c) => WorldMaterials.Glow(c, .14f);
        static bool HasStation(IslandWorldData.Artist a) => a.stationId != 0;
        static float Foot(IslandWorldData.Artist a) => a.name == "Bibio" ? 13f : a.name == "Wintergatan" ? 12f : 8f;

        public override bool Ground(float height, float slope, float patch, out Color color)
        {
            // lush late-summer lawn, going wild and golden in patches
            if (slope > 24f) color = Color.Lerp(new Color(.46f, .4f, .32f), new Color(.54f, .46f, .36f), patch);
            else color = patch < .6f ? Color.Lerp(new Color(.32f, .5f, .26f), new Color(.4f, .56f, .28f), patch / .6f) : Color.Lerp(new Color(.48f, .56f, .3f), new Color(.6f, .58f, .32f), (patch - .6f) / .4f);
            color.a = 1f;
            return true;
        }

        static float Ease(float a, float b, float x) { var t = Mathf.Clamp01((x - a) / (b - a)); return t * t * (3f - 2f * t); }
        Vector2 P2(float d, float s) => heart2 + D2 * d + S2 * s;
        Vector3 Wp(float d, float s) => center + D * d + S * s;
        Vector3 G(LandContext ctx, float d, float s) { var p = Wp(d, s); return ctx.OnGround(p.x, p.z); }

        // ---------------------------------------------------------------- shaping the ground
        public override void ShapeGround(IslandWorldData data, int index)
        {
            var isl = data.layout.islands[index]; heart2 = new Vector2(isl.labelX, isl.labelZ); waterY = data.layout.waterLevel;
            bool Inside(Vector2 p, float m) { if (data.IslandAt(p.x, p.y) != isl) return false; for (var k = 0; k < 12; k++) { var a = k * Mathf.PI / 6f; if (data.IslandAt(p.x + Mathf.Cos(a) * m, p.y + Mathf.Sin(a) * m) != isl) return false; } return true; }
            streetL = 0f;
            for (var k = 0; k < 8; k++) { var dir = new Vector2(Mathf.Sin(k * Mathf.PI / 4f), Mathf.Cos(k * Mathf.PI / 4f)); var len = 36f; while (len < 90f && Inside(heart2 + dir * (len + 16f), 22f)) len += 10f; if (len > streetL) { streetL = len; D2 = dir; } }
            S2 = new Vector2(-D2.y, D2.x); plazaY = Mathf.Max(data.HeightAt(heart2.x, heart2.y), waterY + 6f);
            var keep = new List<string>();
            void Put(string name, float d, float s, float room) { var p = P2(d, s); if (!Inside(p, room)) return; foreach (var a in data.layout.artists) if (a.sceneId == isl.sceneId && a.name == name) { a.x = p.x; a.z = p.y; keep.Add(name); } }
            Put("Bibio", 4f, -46f, 20f); Put("Wintergatan", streetL + 14f, 0f, 16f);
            var avoid = new List<Vector3>(); for (var d = -14f; d <= streetL; d += 18f) avoid.Add(new Vector3(heart2.x + D2.x * d, 16f, heart2.y + D2.y * d));
            ArtistPlacement.Spread(data, index, 42f, heart2, 36f, 32f, avoid, keep, Land, 300f);
            Level(data, index, heart2 - D2 * 4f, 18f, 30f, plazaY);
            for (var d = 16f; d <= streetL; d += 8f) { var p = P2(d, 0); Level(data, index, p, 4f, 10f, Mathf.Lerp(plazaY, Mathf.Max(data.HeightAt(p.x, p.y), waterY + 3f), Mathf.Clamp01((d - 16f) / 30f))); }
            foreach (var a in data.layout.artists)
            {
                if (a.sceneId != isl.sceneId) continue;
                a.r = Mathf.Max(a.r, Foot(a) + 6f);
                Level(data, index, new Vector2(a.x, a.z), Foot(a) + 1f, Foot(a) + 9f, Mathf.Max(data.HeightAt(a.x, a.z), waterY + 2.6f));
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
            center = ctx.Plaza; D = new Vector3(D2.x, 0, D2.y); S = new Vector3(S2.x, 0, S2.y);
            life = new GameObject("Garden life").AddComponent<GardenLife>();
            life.transform.SetParent(ctx.Root, false);
            life.center = center; life.lamps = lampM; life.dials = dialM; life.glass = glassM;
            var mk = new MeshKit(); var solid = new MeshKit();
            Remember(ctx.OnGround(center.x, center.z), 0);
            TownStart(ctx);
            Greenhouse(ctx, mk, solid);
            CrateWalk(ctx, mk, solid);
            Paths(ctx, mk, solid);
            Wilds(ctx, mk, solid);
            mk.Build("The Sample Gardens", ctx.Root, false);
            solid.BuildCollider("The Sample Gardens walls", ctx.Root);
        }

        public override void Finish(LandContext ctx) { Verge(ctx); halos.Build("Garden glow", ctx.Root); }

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

        bool LaneNear(float x, float z, float within) { foreach (var (p, w) in walkPoints) { if (w < 10) continue; float dx = p.x - x, dz = p.z - z; if (dx * dx + dz * dz < within * within) return true; } return false; }

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

        /// A weathered garden bench.
        void Bench(Transform parent, MeshKit mk, Vector3 g, Vector3 face, float len = 2f)
        {
            var up = Vector3.up; var rot = Quaternion.LookRotation(face); var side = Vector3.Cross(up, face); for (var k = 0; k < 3; k++) mk.Box(wood, g + up * .46f + face * (-.16f + k * .16f), new Vector3(len, .05f, .13f), rot); for (var k = 0; k < 2; k++) mk.Box(wood, g + up * (.7f + k * .2f) - face * .26f, new Vector3(len, .1f, .05f), rot); foreach (var e in new[] { -1f, 1f }) mk.Box(iron, g + up * .3f + side * e * (len / 2f - .15f), new Vector3(.06f, .6f, .5f), rot);
            BoxCol(parent, g + up * .45f, new Vector3(len, .9f, .5f), rot); Interact.Bench(tlife.transform, g + up * .52f - side * (len / 2f - .5f), g + up * .52f + side * (len / 2f - .5f), face);
        }

        /// A post hung with a string of warm bulbs.
        void Lamp(LandContext ctx, MeshKit mk, Vector3 g, bool light = false)
        {
            var up = Vector3.up; mk.Prism(woodD, g - up * .2f, up, .07f, 3.2f, 6); mk.Box(woodD, g + up * 2.9f, new Vector3(.06f, .06f, .9f), Quaternion.LookRotation(S)); for (var k = 0; k < 4; k++) mk.Blob(lampM, g + up * (2.8f - (k % 2) * .1f) + S * (k * .3f - .45f), Vector3.one * .07f, new System.Random(k), 0f); halos.Add(g + up * 2.8f, 1.2f, Gold, .4f); ArtistBits.PostCollider(ctx.Root, g, 3f, .09f); if (light) ArtistBits.PointLight(ctx.Root, g + up * 2.6f, Gold, 10f, 1.2f);
        }

        /// A crate of records: a wooden box with the spines showing and one sleeve propped up in front.
        void Crate(Transform parent, MeshKit mk, Vector3 g, Vector3 face, System.Random rng, bool col = true)
        {
            var up = Vector3.up; var rot = Quaternion.LookRotation(face); var side = Vector3.Cross(up, face); mk.Box(wood, g + up * .2f, new Vector3(.4f, .4f, .4f), rot); for (var k = 0; k < 6; k++) mk.Box(Paint(Color.HSVToRGB((float)rng.NextDouble(), .5f, .7f)), g + up * .44f + face * (-.15f + k * .06f), new Vector3(.34f, .3f, .02f), rot * Quaternion.Euler(-8f, 0, 0)); if (col) BoxCol(parent, g + up * .25f, new Vector3(.42f, .5f, .42f), rot);
        }

        /// An old radio with a glowing dial, half buried in a flower bed.
        void Radio(MeshKit mk, Vector3 g, Vector3 face)
        {
            var up = Vector3.up; var rot = Quaternion.LookRotation(face); mk.Box(Paint(new Color(.56f, .36f, .24f)), g + up * .2f, new Vector3(.6f, .4f, .26f), rot * Quaternion.Euler(-10f, 0, 6f)); mk.Box(dialM, g + up * .24f + face * .14f, new Vector3(.3f, .1f, .01f), rot * Quaternion.Euler(-10f, 0, 6f)); mk.Prism(Paint(new Color(.3f, .26f, .2f)), g + up * .2f + face * .14f + Vector3.Cross(up, face) * .2f, face, .08f, .01f, 10);
        }

        /// A flower bed of mixed blooms.
        void Flowers(MeshKit mk, Vector3 g, float r, System.Random rng)
        {
            var up = Vector3.up; var cols = new[] { Rose, Gold, new Color(.9f, .86f, .96f), new Color(.6f, .4f, .9f), new Color(1f, .6f, .3f) };
            for (var k = 0; k < (int)(r * 6f); k++) { var p = g + new Vector3((float)rng.NextDouble() - .5f, 0, (float)rng.NextDouble() - .5f) * r * 2f; var h = .3f + (float)rng.NextDouble() * .5f; mk.Prism(leafD, p, up, .02f, h, 3); mk.Blob(Paint(cols[k % cols.Length]), p + up * h, Vector3.one * (.07f + (float)rng.NextDouble() * .05f), rng, 0f); }
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
                    var dd = Mathf.Min(d, end); var q = from + dir * dd + side * Mathf.Sin(dd / 12f) * Mathf.Min(1.6f, end * .04f);
                    if (!ctx.IsLand(q.x, q.z, 0f)) break;
                    pts.Add(ctx.OnGround(q.x, q.z)); if (dd >= end) break;
                }
                if (pts.Count < 2) return;
                Strip(mk, pts, Paint(new Color(.66f, .58f, .46f)), PathW, .1f, ctx);
                foreach (var p in pts) Remember(p, walk);
                for (var i = 6; i < pts.Count; i += 10, n++)
                {
                    var s = n % 2 == 0 ? 1f : -1f; var lp = pts[i] + side * s * (PathW / 2f + .8f);
                    if (InFootprint(ctx, lp.x, lp.z, 1f) || InNoGrass(lp.x, lp.z, 1f)) continue;
                    Lamp(ctx, mk, ctx.OnGround(lp.x, lp.z), n % 3 == 0);
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

        // ---------------------------------------------------------------- the rest of the garden
        /// Fruit trees, overgrown flower beds with radios in them, stacks of record crates, tape machines on garden tables, washing lines hung with records, sunflowers, wheelbarrows, benches under festoon lights.
        void Wilds(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            int trees = 0, beds = 0, crates = 0, tapes = 0, lines = 0, suns = 0, barrows = 0, benches = 0; var rng = new System.Random(2000);
            const float cell = 18f;
            for (var x = ctx.MinX; x < ctx.MaxX; x += cell)
            for (var z = ctx.MinZ; z < ctx.MaxZ; z += cell)
            {
                float px = x + (float)rng.NextDouble() * cell, pz = z + (float)rng.NextDouble() * cell; var roll = (float)rng.NextDouble(); var s = (float)rng.NextDouble();
                if (!Open(ctx, px, pz, 3f)) continue;
                var p = ctx.OnGround(px, pz); var f = FaceTo(p, NearestWalk(p)); var rot = Quaternion.LookRotation(f); var side = Vector3.Cross(Vector3.up, f); var up = Vector3.up;
                if (roll < .22f) { var h = 2.6f + s * 1.6f; mk.Prism(trunkM, p, (up + side * (s - .5f) * .3f).normalized, .22f, h, 6, .6f); for (var k = 0; k < 6; k++) mk.Blob(k % 2 == 0 ? leafM : leafD, p + up * (h + .4f) + Quaternion.Euler(0, k * 60f, 0) * f * 1.3f + up * (k % 2) * .4f, new Vector3(1.4f, 1f, 1.4f), rng, .3f); for (var k = 0; k < 6; k++) mk.Blob(Paint(s > .5f ? new Color(.9f, .3f, .2f) : new Color(1f, .7f, .2f)), p + up * (h + (k % 3) * .3f) + Quaternion.Euler(0, k * 60f + 30f, 0) * f * 1.6f, Vector3.one * .1f, rng, 0f); ArtistBits.PostCollider(ctx.Root, p, h, .3f); Mark(p, 2.4f); trees++; }
                else if (roll < .38f) { mk.Prism(brickD, p - up * .2f, up, 2f, .4f, 12); mk.Prism(soil, p + up * .2f, up, 1.8f, .02f, 12); Flowers(mk, p + up * .2f, 1.6f, rng); if (s > .4f) Radio(mk, p + up * .2f + side * .4f, f); BoxCol(ctx.Root, p + up * .45f, new Vector3(3.6f, 1.1f, 3.6f), Quaternion.identity); Mark(p, 2.2f); beds++; }
                else if (roll < .5f) { for (var k = 0; k < 3 + (int)(s * 5f); k++) Crate(ctx.Root, mk, p + side * (k % 3 - 1) * .44f + up * (k / 3) * .42f, f, rng); crates++; }
                else if (roll < .58f) { mk.Box(wood, p + up * .75f, new Vector3(1.4f, .06f, .8f), rot); foreach (var e in new[] { -.6f, .6f }) foreach (var q in new[] { -.3f, .3f }) mk.Box(woodD, p + up * .37f + side * e + f * q, new Vector3(.06f, .74f, .06f), rot); mk.Box(Paint(new Color(.5f, .48f, .44f)), p + up * .95f, new Vector3(.9f, .3f, .5f), rot); foreach (var e in new[] { -.24f, .24f }) { mk.Prism(dark, p + up * 1.12f + side * e + f * .05f, f, .18f, .03f, 14); mk.Prism(Paint(new Color(.6f, .56f, .5f)), p + up * 1.12f + side * e + f * .08f, f, .05f, .02f, 8); } mk.Box(dialM, p + up * .95f + f * .26f, new Vector3(.3f, .06f, .01f), rot); BoxCol(ctx.Root, p + up * .5f, new Vector3(1.4f, 1f, .8f), rot); Mark(p, 1.4f); tapes++; }
                else if (roll < .66f) { var a0 = p - side * 3f; var a1 = p + side * 3f; foreach (var ap in new[] { a0, a1 }) { mk.Prism(woodD, ap, up, .05f, 2.2f, 5); ArtistBits.PostCollider(ctx.Root, ap, 2.2f, .08f); } for (var k = 0; k < 6; k++) { var lp = Vector3.Lerp(a0, a1, (k + .5f) / 6f) + up * (2.1f - Mathf.Sin((k + .5f) / 6f * Mathf.PI) * .3f); mk.Prism(vinylM, lp - up * .2f - f * .01f, f, .2f, .02f, 16); mk.Prism(Paint(Color.HSVToRGB((k * .17f + s) % 1f, .5f, .8f)), lp - up * .2f + f * .012f, f, .07f, .01f, 10); } BoxCol(ctx.Root, (a0 + a1) / 2f + up * 1.9f, new Vector3(.3f, .7f, 6f), Quaternion.LookRotation(side)); lines++; }
                else if (roll < .72f) { for (var k = 0; k < 4 + (int)(s * 4f); k++) { var sp = p + side * (k - 2f) * .6f + f * (k % 2) * .4f; var h = 1.6f + (k % 3) * .3f; mk.Prism(leafD, sp, up, .04f, h, 5); mk.Prism(Paint(Gold), sp + up * h, f, .26f, .04f, 12); mk.Prism(Paint(new Color(.36f, .24f, .14f)), sp + up * h + f * .03f, f, .1f, .02f, 10); } ArtistBits.PostCollider(ctx.Root, p, 2.4f, 1.6f); suns++; }
                else if (roll < .76f) { mk.Box(Paint(new Color(.4f, .5f, .4f)), p + up * .5f, new Vector3(.7f, .3f, 1.1f), rot * Quaternion.Euler(-10f, 0, 0)); mk.Prism(dark, p + up * .25f + f * .6f - side * .06f, side, .25f, .12f, 10); foreach (var e in new[] { -.25f, .25f }) mk.Box(woodD, p + up * .5f - f * .8f + side * e, new Vector3(.05f, .05f, .7f), rot * Quaternion.Euler(-20f, 0, 0)); for (var k = 0; k < 5; k++) mk.Prism(vinylM, p + up * .7f + side * ((k % 3) - 1f) * .18f + f * ((k / 3) * .3f - .1f), (up + f * .3f).normalized, .16f, .01f, 14); BoxCol(ctx.Root, p + up * .45f, new Vector3(.8f, .9f, 1.4f), rot); barrows++; }
                else if (roll < .83f) { Bench(ctx.Root, mk, p, f); Lamp(ctx, mk, p - f * .8f + side * 1.6f, benches % 2 == 0); Flowers(mk, p - f * .8f - side * 1.4f, .6f, rng); Mark(p, 2.2f); benches++; }
            }
            Debug.Log($"The Sample Gardens: the Crate Walk {streetL:0} m, {trees} fruit trees, {beds} flower beds, {crates} crate stacks, {tapes} tape machines, {lines} washing lines of records, {suns} sunflower clumps, {barrows} wheelbarrows of vinyl, {benches} benches");
        }

        Vector3 NearestWalk(Vector3 p) { var best = center; var bd = float.MaxValue; foreach (var (q, _) in walkPoints) { var d = (q - p).sqrMagnitude; if (d < bd) { bd = d; best = q; } } return best; }

        /// Long, seeding summer grass, gold at the tips.
        void Verge(LandContext ctx)
        {
            var mat = WindMesh.Material(.3f, .8f, new Color(.4f, .54f, .3f));
            var meshes = new Dictionary<(int, int), WindMesh>(); var rng = new System.Random(2001);
            const float st = 2.2f;
            for (var x = ctx.MinX; x < ctx.MaxX; x += st)
            for (var z = ctx.MinZ; z < ctx.MaxZ; z += st)
            {
                var px = x + ((float)rng.NextDouble() - .5f) * st; var pz = z + ((float)rng.NextDouble() - .5f) * st;
                if (!ctx.IsLand(px, pz, 1f) || NearPath(px, pz, 3.2f) || InNoGrass(px, pz, 0f) || InFootprint(ctx, px, pz, -2f)) continue;
                if (Mathf.PerlinNoise(px * .04f + 6f, pz * .04f + 1f) < .35f) continue;
                var key = (Mathf.FloorToInt(px / 70f), Mathf.FloorToInt(pz / 70f));
                if (!meshes.TryGetValue(key, out var wm)) meshes[key] = wm = new WindMesh();
                var col = Color.Lerp(new Color(.36f, .52f, .28f), new Color(.56f, .58f, .32f), (float)rng.NextDouble());
                wm.Tuft(new Vector3(px, ctx.Ground(px, pz) - .04f, pz), .4f + (float)rng.NextDouble() * .5f, col, Color.Lerp(col, Gold, .5f), rng, 4, .08f);
            }
            foreach (var kv in meshes) kv.Value.Build("Garden grass", ctx.Root, mat);
        }
    }
}
