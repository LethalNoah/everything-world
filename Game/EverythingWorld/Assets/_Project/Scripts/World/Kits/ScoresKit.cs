using System.Collections.Generic;
using UnityEngine;
using static EverythingWorld.World.PlaceTools;

namespace EverythingWorld.World
{
    /// The Isle of Scores (WORLD_GUIDE.md): an orchestra under the stars. You arrive in an open-air amphitheatre at night,
    /// stone tiers rising round an orchestra of music stands with little gold lights, a great blue-and-gold shell behind
    /// them, and a conductor's podium you can step up to. The Star Road runs from the aisle out to the far corner where a
    /// starship has landed in the desert under two suns. Round the hills: a Halloween town under a huge moon, a circle of
    /// grave markers in the sand, swans on a lake, a grand piano on the lawn. Royal blue night, gold light, velvet red.
    /// Laid out on purpose (the build-land standard): ScoresTown.cs has the places and the people.
    public partial class ScoresKit : LandKit
    {
        public override string Land => "The Isle of Scores";
        public override Vector3 ArtistFront(IslandWorldData data, IslandWorldData.Artist a) => facing.TryGetValue(a.name, out var f) ? f : base.ArtistFront(data, a);

        public static readonly Color Gold = new Color(1f, .78f, .3f), Velvet = new Color(.5f, .1f, .16f), Royal = new Color(.18f, .28f, .6f), Ink = new Color(.06f, .06f, .12f), Pale = new Color(.86f, .86f, .92f), Moon = new Color(.86f, .9f, 1f), Ember = new Color(1f, .5f, .16f);
        const float PathW = 3f, RoadW = 6f;

        Material stone, stoneD, navy, goldM, velvet, iron, dark, white, leafM, trunkM, sand, lampM, standM, starM, windowM, waterM;
        GlowHalos halos; Vector3 center, D, S; ScoresLife life;
        readonly Dictionary<(int, int), List<Vector3>> pathCells = new Dictionary<(int, int), List<Vector3>>();
        readonly List<(Vector3 p, int w)> walkPoints = new List<(Vector3, int)>();
        readonly Dictionary<string, Vector3> facing = new Dictionary<string, Vector3>();
        // x, radius, z
        readonly List<Vector3> noGrass = new List<Vector3>();
        Vector2 heart2, D2 = new Vector2(0, -1), S2 = new Vector2(1, 0); float plazaY, waterY, streetL;

        void MakeMaterials()
        {
            stone = WorldMaterials.Glow(new Color(.24f, .26f, .36f), .08f); stoneD = WorldMaterials.Glow(new Color(.16f, .17f, .26f), .06f); navy = WorldMaterials.Glow(Royal, .2f); goldM = WorldMaterials.Glow(Gold, .5f); velvet = WorldMaterials.Glow(Velvet, .2f); iron = WorldMaterials.Solid(new Color(.14f, .14f, .18f), .5f); dark = WorldMaterials.Solid(new Color(.05f, .05f, .08f)); white = WorldMaterials.Glow(new Color(.86f, .86f, .9f), .16f);
            leafM = WorldMaterials.Glow(new Color(.12f, .26f, .24f), .1f); trunkM = WorldMaterials.Glow(new Color(.26f, .2f, .18f), .1f); sand = WorldMaterials.Glow(new Color(.66f, .54f, .4f), .14f);
            lampM = new Material(WorldMaterials.Glow(new Color(1f, .86f, .56f), 1.6f)) { name = "Scores lamps" }; standM = new Material(WorldMaterials.Glow(Gold, 1.4f)) { name = "Scores music stands" }; starM = new Material(WorldMaterials.Glow(new Color(.9f, .9f, 1f), 1.6f)) { name = "Scores stars" };
            windowM = new Material(WorldMaterials.Glow(new Color(1f, .6f, .2f), 1.2f)) { name = "Scores windows" }; waterM = new Material(WorldMaterials.Glow(new Color(.16f, .24f, .5f), .6f)) { name = "Scores water" };
        }

        Material GlowM(Color c, float s = 2f) => WorldMaterials.Glow(c, s * .65f);
        Material Paint(Color c) => WorldMaterials.Glow(c, .14f);
        static bool HasStation(IslandWorldData.Artist a) => a.stationId != 0;
        static float Foot(IslandWorldData.Artist a) => a.name == "John Williams" ? 18f : a.name == "Danny Elfman" ? 15f : 8f;

        public override bool Ground(float height, float slope, float patch, out Color color)
        {
            // night meadow: deep blue-green grass, grey-blue rock on the slopes
            if (slope > 24f) color = Color.Lerp(new Color(.3f, .32f, .4f), new Color(.38f, .4f, .48f), patch);
            else color = Color.Lerp(new Color(.18f, .3f, .3f), new Color(.24f, .36f, .32f), patch);
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
            for (var k = 0; k < 8; k++) { var dir = new Vector2(Mathf.Sin(k * Mathf.PI / 4f), Mathf.Cos(k * Mathf.PI / 4f)); var len = 40f; while (len < 120f && Inside(heart2 + dir * (len + 10f), 46f)) len += 10f; if (len > streetL) { streetL = len; D2 = dir; } }
            S2 = new Vector2(-D2.y, D2.x); plazaY = Mathf.Max(data.HeightAt(heart2.x, heart2.y), waterY + 6f);
            var keep = new List<string>();
            void Put(string name, float d, float s, float room) { var p = P2(d, s); if (!Inside(p, room)) return; foreach (var a in data.layout.artists) if (a.sceneId == isl.sceneId && a.name == name) { a.x = p.x; a.z = p.y; keep.Add(name); } }
            Put("John Williams", streetL + 16f, 0f, 30f); Put("Danny Elfman", -4f, -60f, 28f); Put("Ennio Morricone", 6f, 56f, 18f);
            var avoid = new List<Vector3>(); for (var d = 0f; d <= streetL; d += 22f) avoid.Add(new Vector3(heart2.x + D2.x * d, 20f, heart2.y + D2.y * d)); avoid.Add(new Vector3(heart2.x - D2.x * 14f, 20f, heart2.y - D2.y * 14f));
            ArtistPlacement.Spread(data, index, 52f, heart2, 44f, 40f, avoid, keep, Land, 330f);
            Level(data, index, heart2 - D2 * 4f, 28f, 46f, plazaY);
            for (var d = 30f; d <= streetL; d += 10f) { var p = P2(d, 0); Level(data, index, p, 5f, 12f, Mathf.Lerp(plazaY, Mathf.Max(data.HeightAt(p.x, p.y), waterY + 3f), Mathf.Clamp01((d - 30f) / 30f))); }
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
            center = ctx.Plaza; D = new Vector3(D2.x, 0, D2.y); S = new Vector3(S2.x, 0, S2.y);
            life = new GameObject("Scores life").AddComponent<ScoresLife>();
            life.transform.SetParent(ctx.Root, false);
            life.center = center; life.lamps = lampM; life.stands = standM; life.stars = starM; life.windows = windowM; life.water = waterM;
            var mk = new MeshKit(); var solid = new MeshKit();
            Remember(ctx.OnGround(center.x, center.z), 0);
            TownStart(ctx);
            Amphitheatre(ctx, mk, solid);
            StarRoad(ctx, mk, solid);
            Paths(ctx, mk, solid);
            Lake(ctx, mk, solid);
            Wilds(ctx, mk, solid);
            mk.Build("The Isle of Scores", ctx.Root, false);
            solid.BuildCollider("The Isle of Scores walls", ctx.Root);
        }

        public override void Finish(LandContext ctx) { Verge(ctx); halos.Build("Scores glow", ctx.Root); }

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

        /// A bench to sit on.
        void Bench(Transform parent, MeshKit mk, Vector3 g, Vector3 face, float len = 2f)
        {
            var up = Vector3.up; var rot = Quaternion.LookRotation(face); var side = Vector3.Cross(up, face); mk.Box(stone, g + up * .4f, new Vector3(len, .14f, .5f), rot); foreach (var e in new[] { -1f, 1f }) mk.Box(stoneD, g + up * .2f + side * e * (len / 2f - .2f), new Vector3(.24f, .4f, .44f), rot);
            BoxCol(parent, g + up * .25f, new Vector3(len, .5f, .5f), rot); Interact.Bench(tlife.transform, g + up * .48f - side * (len / 2f - .5f), g + up * .48f + side * (len / 2f - .5f), face);
        }

        /// A lantern on a black iron post, gold glass.
        void Lamp(LandContext ctx, MeshKit mk, Vector3 g, bool light = false)
        {
            var up = Vector3.up; mk.Prism(iron, g - up * .2f, up, .06f, 3.4f, 6, .7f); mk.Prism(iron, g + up * 3.1f, up, .14f, .1f, 6); mk.Prism(lampM, g + up * 3.2f, up, .16f, .44f, 6); mk.Cone(iron, g + up * 3.64f, .22f, .26f, 6); halos.Add(g + up * 3.4f, 1.4f, new Color(1f, .86f, .56f), .45f); ArtistBits.PostCollider(ctx.Root, g, 3.4f, .09f); if (light) ArtistBits.PointLight(ctx.Root, g + up * 3f, new Color(1f, .84f, .56f), 12f, 1.4f);
        }

        /// A music stand with a little gold light on it.
        void Stand(MeshKit mk, Vector3 g, Vector3 face)
        {
            var up = Vector3.up; var rot = Quaternion.LookRotation(face); mk.Prism(iron, g, up, .02f, 1.1f, 4); mk.Box(iron, g + up * 1.2f, new Vector3(.5f, .36f, .02f), rot * Quaternion.Euler(-20f, 0, 0)); mk.Box(standM, g + up * 1.42f + face * .06f, new Vector3(.12f, .04f, .04f), rot);
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
                    var dd = Mathf.Min(d, end); var q = from + dir * dd + side * Mathf.Sin(dd / 18f) * Mathf.Min(1.4f, end * .03f);
                    if (!ctx.IsLand(q.x, q.z, 0f)) break;
                    pts.Add(ctx.OnGround(q.x, q.z)); if (dd >= end) break;
                }
                if (pts.Count < 2) return;
                Strip(mk, pts, stoneD, PathW, .13f, ctx);
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

        // ---------------------------------------------------------------- the lake of swans
        void Lake(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; Vector3 best = Vector3.zero; var found = false;
            for (var k = 0; k < 16 && !found; k++) for (var r = 50f; r <= 110f && !found; r += 15f) { var p = center + Quaternion.Euler(0, k * 22.5f + 45f, 0) * D * r; if (Open(ctx, p.x, p.z, 12f) && Mathf.Abs(ctx.Ground(p.x + 8f, p.z) - ctx.Ground(p.x - 8f, p.z)) < 1.6f && Mathf.Abs(ctx.Ground(p.x, p.z + 8f) - ctx.Ground(p.x, p.z - 8f)) < 1.6f) { best = ctx.OnGround(p.x, p.z); found = true; } }
            if (!found) return;
            var lc = best; mk.Prism(stoneD, lc - up * .4f, up, 9.4f, .6f, 28); mk.Prism(waterM, lc + up * .22f, up, 8.8f, .02f, 28); solid.Prism(stone, lc - up * .4f, up, 9.4f, .9f, 16); for (var k = 0; k < 16; k++) Remember(lc + Quaternion.Euler(0, k * 22.5f, 0) * D * 10.4f, 1);
            life.lake = lc + up * .26f; Mark(lc, 10.5f);
            var rng = new System.Random(5); for (var k = 0; k < 5; k++) { var sw = new MeshKit(); sw.Blob(white, new Vector3(0, .25f, 0), new Vector3(.3f, .22f, .5f), rng, .05f); sw.Prism(white, new Vector3(0, .3f, .3f), (Vector3.up + Vector3.forward * .2f).normalized, .05f, .6f, 6); sw.Blob(white, new Vector3(0, .9f, .42f), new Vector3(.08f, .08f, .12f), rng, 0f); sw.Cone(Paint(Ember), new Vector3(0, .9f, .52f), .03f, .1f, 4); var g = sw.BuildSingle("Swan", ctx.Root, false); life.swans.Add(g.transform); }
            for (var k = 0; k < 8; k++) { var bp = lc + Quaternion.Euler(0, k * 45f + 22f, 0) * D * 11.5f; if (k % 2 == 0) Lamp(ctx, mk, ctx.OnGround(bp.x, bp.z), k == 0); else if (Open(ctx, bp.x, bp.z, 1f)) Bench(ctx.Root, mk, ctx.OnGround(bp.x, bp.z), FaceTo(bp, lc)); }
            Shot(ctx, "the lake of swans", lc + Quaternion.Euler(0, 30f, 0) * D * 14f + up * 2.4f, lc);
        }

        // ---------------------------------------------------------------- the rest of the island
        /// Dark cypresses, marble statues on plinths, red-and-gold banners, music stands left out on the grass, benches under lanterns, the odd grand piano.
        void Wilds(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            int trees = 0, statues = 0, banners = 0, stands = 0, benches = 0, pianos = 0; var rng = new System.Random(1977);
            const float cell = 20f;
            for (var x = ctx.MinX; x < ctx.MaxX; x += cell)
            for (var z = ctx.MinZ; z < ctx.MaxZ; z += cell)
            {
                float px = x + (float)rng.NextDouble() * cell, pz = z + (float)rng.NextDouble() * cell; var roll = (float)rng.NextDouble(); var s = (float)rng.NextDouble();
                if (!Open(ctx, px, pz, 3f)) continue;
                var p = ctx.OnGround(px, pz); var f = FaceTo(p, NearestWalk(p)); var rot = Quaternion.LookRotation(f); var side = Vector3.Cross(Vector3.up, f); var up = Vector3.up;
                if (roll < .34f) { var h = 5f + s * 4f; for (var k = 0; k < 1 + (int)(s * 3f); k++) { var tp = p + side * (k * 1.6f) - f * (k % 2) * 1.2f; mk.Prism(trunkM, tp, up, .16f, 1f, 5); mk.Cone(leafM, tp + up * .8f, .9f, h, 8); ArtistBits.PostCollider(ctx.Root, tp, 2f, .5f); } noGrass.Add(new Vector3(p.x, 2f, p.z)); trees++; }
                else if (roll < .44f) { mk.Box(stone, p + up * .7f, new Vector3(1.2f, 1.4f, 1.2f), rot); mk.Box(goldM, p + up * 1.42f, new Vector3(1.3f, .06f, 1.3f), rot); mk.Prism(white, p + up * 1.44f, up, .26f, 1.2f, 8, .7f); mk.Blob(white, p + up * 2.8f, new Vector3(.26f, .32f, .26f), rng, .05f); mk.Box(white, p + up * 2.2f + side * .4f * (s > .5f ? 1f : -1f), new Vector3(.12f, .8f, .12f), rot * Quaternion.Euler(0, 0, s > .5f ? -50f : 50f)); BoxCol(ctx.Root, p + up * 1.5f, new Vector3(1.2f, 3f, 1.2f), rot); noGrass.Add(new Vector3(p.x, 1.4f, p.z)); statues++; }
                else if (roll < .52f) { foreach (var e in new[] { -1f, 1f }) { var bp = p + side * e * 1.2f; mk.Prism(iron, bp, up, .05f, 4.6f, 5); mk.Cone(goldM, bp + up * 4.6f, .1f, .3f, 5); ArtistBits.PostCollider(ctx.Root, bp, 4.6f, .08f); mk.Box(velvet, bp + up * 3.2f + f * .02f, new Vector3(.9f, 2.2f, .04f), rot); mk.Box(goldM, bp + up * 4.28f + f * .03f, new Vector3(.9f, .1f, .04f), rot); mk.Prism(goldM, bp + up * 3.4f + f * .05f, f, .2f, .02f, 5); } banners++; }
                else if (roll < .62f) { for (var k = 0; k < 3; k++) { var a = (k - 1) * 30f; Stand(mk, p + Quaternion.Euler(0, a, 0) * f * 1.6f, -(Quaternion.Euler(0, a, 0) * f)); ArtistBits.PostCollider(ctx.Root, p + Quaternion.Euler(0, a, 0) * f * 1.6f, 1.3f, .2f); } mk.Prism(stone, p - up * .7f, up, 2.2f, .8f, 16); Mark(p, 2.4f); stands++; }
                else if (roll < .7f) { Bench(ctx.Root, mk, p, f); Lamp(ctx, mk, p - f * .8f + side * 1.6f, benches % 2 == 0); Mark(p, 2.2f); benches++; }
                else if (roll < .73f && pianos < 6 && Open(ctx, px, pz, 5f)) { Piano(ctx, mk, p, f); pianos++; }
            }
            Debug.Log($"The Isle of Scores: Star Road {streetL:0} m, {trees} cypress groves, {statues} statues, {banners} banners, {stands} music stands, {benches} benches, {pianos} grand pianos");
        }

        /// A black grand piano on a round of stone, lid up, its bench to sit on.
        void Piano(LandContext ctx, MeshKit mk, Vector3 g, Vector3 face)
        {
            var up = Vector3.up; var rot = Quaternion.LookRotation(face); var side = Vector3.Cross(up, face); mk.Prism(stone, g - up * .1f, up, 2.4f, .24f, 18);
            var body = g + up * .14f - face * .6f; mk.Box(dark, body + up * .8f, new Vector3(1.5f, .3f, 1.8f), rot); mk.Blob(dark, body + up * .8f - face * .9f, new Vector3(.75f, .15f, .6f), new System.Random(2), 0f); foreach (var (x, z) in new[] { (-.6f, .8f), (.6f, .8f), (0f, -1.1f) }) mk.Prism(dark, body + side * x + face * z, up, .06f, .66f, 6);
            mk.Box(dark, body + up * 1.4f - side * .1f, new Vector3(.04f, 1f, 1.6f), rot * Quaternion.Euler(0, 0, -30f)); mk.Box(white, body + up * .88f + face * .92f, new Vector3(1.3f, .06f, .2f), rot); mk.Box(standM, body + up * 1.1f + face * .82f, new Vector3(.5f, .3f, .02f), rot * Quaternion.Euler(-15f, 0, 0));
            BoxCol(ctx.Root, body + up * 1f, new Vector3(1.6f, 2f, 2.6f), rot); var bp = g + up * .14f + face * .9f; mk.Box(dark, bp + up * .25f, new Vector3(.9f, .5f, .4f), rot); BoxCol(ctx.Root, bp + up * .25f, new Vector3(.9f, .5f, .4f), rot); Interact.Seat(tlife.transform, bp + up * .5f, -face, "Sit at the piano").seatHeight = .5f;
            Mark(g, 2.6f);
        }

        Vector3 NearestWalk(Vector3 p) { var best = center; var bd = float.MaxValue; foreach (var (q, _) in walkPoints) { var d = (q - p).sqrMagnitude; if (d < bd) { bd = d; best = q; } } return best; }

        /// Long night grass, blue-green with pale tips.
        void Verge(LandContext ctx)
        {
            var mat = WindMesh.Material(.3f, .8f, new Color(.2f, .34f, .32f));
            var meshes = new Dictionary<(int, int), WindMesh>(); var rng = new System.Random(1978);
            const float st = 2.4f;
            for (var x = ctx.MinX; x < ctx.MaxX; x += st)
            for (var z = ctx.MinZ; z < ctx.MaxZ; z += st)
            {
                var px = x + ((float)rng.NextDouble() - .5f) * st; var pz = z + ((float)rng.NextDouble() - .5f) * st;
                if (!ctx.IsLand(px, pz, 1f) || NearPath(px, pz, 3.6f) || InNoGrass(px, pz, 0f) || InFootprint(ctx, px, pz, -2f)) continue;
                if (Mathf.PerlinNoise(px * .04f + 3f, pz * .04f + 9f) < .4f) continue;
                var key = (Mathf.FloorToInt(px / 70f), Mathf.FloorToInt(pz / 70f));
                if (!meshes.TryGetValue(key, out var wm)) meshes[key] = wm = new WindMesh();
                var col = Color.Lerp(new Color(.16f, .3f, .3f), new Color(.24f, .4f, .36f), (float)rng.NextDouble());
                wm.Tuft(new Vector3(px, ctx.Ground(px, pz) - .04f, pz), .3f + (float)rng.NextDouble() * .4f, col, Color.Lerp(col, Moon, .5f), rng, 3, .08f);
            }
            foreach (var kv in meshes) kv.Value.Build("Scores grass", ctx.Root, mat);
        }
    }
}
