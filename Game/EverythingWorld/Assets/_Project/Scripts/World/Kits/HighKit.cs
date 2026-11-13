using System.Collections.Generic;
using UnityEngine;
using static EverythingWorld.World.PlaceTools;

namespace EverythingWorld.World
{
    /// The Melodic Bass Highlands (WORLD_GUIDE.md): fantasy highlands at twilight under floating islands. You arrive in
    /// the Rune Circle, a ring of tall standing stones carved with glowing runes, under a great floating island whose
    /// waterfall pours down into a pool beside it. The Rune Road runs out to the Sky Stair, a spiral of floating stone
    /// steps you can climb to a lookout island high above the land. Floating islands drift over everything, some
    /// pouring waterfalls into the air; lanterns float; crystals grow out of the hills. Pink, violet, gold and indigo.
    /// Laid out on purpose (the build-land standard): HighTown.cs has the places and the people.
    public partial class HighKit : LandKit
    {
        public override string Land => "The Melodic Bass Highlands";
        public override Vector3 ArtistFront(IslandWorldData data, IslandWorldData.Artist a) => facing.TryGetValue(a.name, out var f) ? f : base.ArtistFront(data, a);

        public static readonly Color Pink = new Color(1f, .44f, .57f), Violet = new Color(.52f, .37f, .76f), Gold = new Color(1f, .78f, .37f), Indigo = new Color(.12f, .1f, .3f), Rune = new Color(.7f, .5f, 1f), Water = new Color(.5f, .75f, 1f), Ink = new Color(.08f, .06f, .14f);
        const float PathW = 3f, RoadW = 5f;

        Material rock, rockD, grassTop, path, iron, dark, white, leafM, trunkM, runeM, lampM, waterM, crystalM;
        GlowHalos halos; Vector3 center, D, S; HighLife life;
        readonly Dictionary<(int, int), List<Vector3>> pathCells = new Dictionary<(int, int), List<Vector3>>();
        readonly List<(Vector3 p, int w)> walkPoints = new List<(Vector3, int)>();
        readonly Dictionary<string, Vector3> facing = new Dictionary<string, Vector3>();
        // x, radius, z
        readonly List<Vector3> noGrass = new List<Vector3>();
        Vector2 heart2, D2 = new Vector2(0, -1), S2 = new Vector2(1, 0); float plazaY, waterY, streetL;

        void MakeMaterials()
        {
            rock = WorldMaterials.Glow(new Color(.42f, .38f, .5f), .1f); rockD = WorldMaterials.Glow(new Color(.28f, .25f, .36f), .08f); grassTop = WorldMaterials.Glow(new Color(.34f, .48f, .42f), .1f); path = WorldMaterials.Glow(new Color(.36f, .32f, .42f), .08f);
            iron = WorldMaterials.Solid(new Color(.14f, .12f, .18f), .5f); dark = WorldMaterials.Solid(new Color(.05f, .04f, .08f)); white = WorldMaterials.Glow(new Color(.86f, .84f, .9f), .16f); leafM = WorldMaterials.Glow(new Color(.3f, .46f, .4f), .12f); trunkM = WorldMaterials.Glow(new Color(.3f, .22f, .24f), .1f);
            runeM = new Material(WorldMaterials.Glow(Rune, 1.6f)) { name = "High runes" }; lampM = new Material(WorldMaterials.Glow(Gold, 1.6f)) { name = "High lanterns" }; waterM = new Material(WorldMaterials.Glow(Water, .6f)) { name = "High water" }; crystalM = new Material(WorldMaterials.Glow(Pink, 1.1f)) { name = "High crystals" };
        }

        Material GlowM(Color c, float s = 2f) => WorldMaterials.Glow(c, s * .65f);
        Material Paint(Color c) => WorldMaterials.Glow(c, .14f);
        static bool HasStation(IslandWorldData.Artist a) => a.stationId != 0;
        static float Foot(IslandWorldData.Artist a) => a.name == "Seven Lions" ? 16f : a.name == "Mr FijiWiji" ? 13f : 8f;

        public override bool Ground(float height, float slope, float patch, out Color color)
        {
            // twilight highland: blue-green turf with a violet cast, violet-grey rock on the slopes
            if (slope > 22f) color = Color.Lerp(new Color(.48f, .42f, .58f), new Color(.56f, .5f, .64f), patch);
            else color = Color.Lerp(new Color(.34f, .48f, .44f), new Color(.44f, .54f, .48f), patch);
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
            for (var k = 0; k < 8; k++) { var dir = new Vector2(Mathf.Sin(k * Mathf.PI / 4f), Mathf.Cos(k * Mathf.PI / 4f)); var len = 40f; while (len < 110f && Inside(heart2 + dir * (len + 20f), 30f)) len += 10f; if (len > streetL) { streetL = len; D2 = dir; } }
            S2 = new Vector2(-D2.y, D2.x); plazaY = Mathf.Max(data.HeightAt(heart2.x, heart2.y), waterY + 6f);
            var keep = new List<string>();
            void Put(string name, float d, float s, float room) { var p = P2(d, s); if (!Inside(p, room)) return; foreach (var a in data.layout.artists) if (a.sceneId == isl.sceneId && a.name == name) { a.x = p.x; a.z = p.y; keep.Add(name); } }
            Put("Seven Lions", 6f, -58f, 26f); Put("Mr FijiWiji", 2f, 54f, 22f);
            var stair = P2(streetL + 12f, 0);
            var avoid = new List<Vector3>(); for (var d = 0f; d <= streetL; d += 20f) avoid.Add(new Vector3(heart2.x + D2.x * d, 18f, heart2.y + D2.y * d)); avoid.Add(new Vector3(stair.x, 18f, stair.y)); avoid.Add(new Vector3(heart2.x - D2.x * 18f, 14f, heart2.y - D2.y * 18f));
            ArtistPlacement.Spread(data, index, 48f, heart2, 42f, 38f, avoid, keep, Land, 320f);
            Level(data, index, heart2, 20f, 34f, plazaY);
            for (var d = 22f; d <= streetL; d += 8f) { var p = P2(d, 0); Level(data, index, p, 4f, 10f, Mathf.Lerp(plazaY, Mathf.Max(data.HeightAt(p.x, p.y), waterY + 3f), Mathf.Clamp01((d - 22f) / 30f))); }
            Level(data, index, stair, 12f, 22f, Mathf.Max(data.HeightAt(stair.x, stair.y), waterY + 3f));
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
            life = new GameObject("High life").AddComponent<HighLife>();
            life.transform.SetParent(ctx.Root, false);
            life.center = center; life.runes = runeM; life.lamps = lampM; life.water = waterM; life.crystals = crystalM;
            var mk = new MeshKit(); var solid = new MeshKit();
            Remember(ctx.OnGround(center.x, center.z), 0);
            TownStart(ctx);
            RuneCircle(ctx, mk, solid);
            RuneRoad(ctx, mk, solid);
            SkyStair(ctx, mk, solid);
            Paths(ctx, mk, solid);
            Wilds(ctx, mk, solid);
            mk.Build("The Melodic Bass Highlands", ctx.Root, false);
            solid.BuildCollider("The Melodic Bass Highlands walls", ctx.Root);
        }

        public override void Finish(LandContext ctx) { Verge(ctx); halos.Build("High glow", ctx.Root); }

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

        /// A stone bench.
        void Bench(Transform parent, MeshKit mk, Vector3 g, Vector3 face, float len = 2f)
        {
            var up = Vector3.up; var rot = Quaternion.LookRotation(face); var side = Vector3.Cross(up, face); mk.Box(rock, g + up * .42f, new Vector3(len, .16f, .5f), rot); foreach (var e in new[] { -1f, 1f }) mk.Box(rockD, g + up * .2f + side * e * (len / 2f - .25f), new Vector3(.3f, .4f, .44f), rot);
            BoxCol(parent, g + up * .25f, new Vector3(len, .5f, .5f), rot); Interact.Bench(tlife.transform, g + up * .5f - side * (len / 2f - .5f), g + up * .5f + side * (len / 2f - .5f), face);
        }

        /// A floating lantern held over a carved stone post.
        void Lamp(LandContext ctx, MeshKit mk, Vector3 g, bool light = false)
        {
            var up = Vector3.up; mk.Box(rock, g + up * .7f, new Vector3(.36f, 1.4f, .36f), Quaternion.Euler(0, 45f, 0)); mk.Box(runeM, g + up * 1f + new Vector3(.13f, 0, .13f), new Vector3(.02f, .5f, .14f), Quaternion.Euler(0, 45f, 0)); mk.Prism(lampM, g + up * 2.1f, up, .2f, .5f, 6, .7f); halos.Add(g + up * 2.35f, 1.4f, Gold, .45f); ArtistBits.PostCollider(ctx.Root, g, 1.4f, .26f); if (light) ArtistBits.PointLight(ctx.Root, g + up * 2.2f, Gold, 12f, 1.4f);
        }

        /// A cluster of glowing crystals growing out of the ground.
        void Crystals(Transform parent, MeshKit mk, Vector3 g, float s, Material m, System.Random rng)
        {
            var up = Vector3.up; for (var k = 0; k < 5; k++) { var dir = (up + new Vector3((float)rng.NextDouble() - .5f, 0, (float)rng.NextDouble() - .5f) * .8f).normalized; mk.Prism(m, g - up * .2f + new Vector3((float)rng.NextDouble() - .5f, 0, (float)rng.NextDouble() - .5f) * s, dir, (.18f + (float)rng.NextDouble() * .14f) * s, (1f + (float)rng.NextDouble() * 1.6f) * s, 6, .05f); } ArtistBits.PostCollider(parent, g, 1.6f * s, .6f * s);
        }

        /// A floating island: a turned-down cone of rock with turf on top, a tree, lanterns, sometimes a waterfall off its edge. It bobs (HighLife).
        void Floater(LandContext ctx, Vector3 at, float r, System.Random rng, bool fall, float fallLen)
        {
            var m = new MeshKit(); var up = Vector3.up;
            m.Prism(rock, Vector3.zero, -up, r, r * 1.4f, 9, .05f, 20f); m.Prism(rockD, -up * r * .5f, -up, r * .6f, r * 1.2f, 7, .05f); m.Prism(grassTop, Vector3.zero, up, r * 1.02f, .3f, 9, .96f, 20f);
            var tp = new Vector3(r * .2f, .3f, -r * .1f); m.Prism(trunkM, tp, up, .2f * r / 5f + .1f, r * .7f, 6, .6f); m.Blob(GlowM(new Color(1f, .6f, .74f), .5f), tp + up * (r * .7f + .6f), new Vector3(r * .5f, r * .36f, r * .5f), rng, .25f);
            m.Prism(lampM, new Vector3(-r * .5f, .5f, r * .3f), up, .14f, .4f, 6, .7f);
            if (fall) { var e = new Vector3(r * .95f, .2f, 0); m.Box(waterM, e + new Vector3(.1f, -fallLen / 2f, 0), new Vector3(.06f, fallLen, r * .22f), Quaternion.identity); }
            var go = m.BuildSingle("Floating island", ctx.Root, false); go.transform.position = at; go.transform.rotation = Quaternion.Euler(0, (float)rng.NextDouble() * 360f, 0); life.floaters.Add((go.transform, at, (float)rng.NextDouble() * 6f));
            halos.Add(at + up * 1f, r * .8f, Pink, .12f);
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
                Strip(mk, pts, path, PathW, .13f, ctx);
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

        // ---------------------------------------------------------------- the rest of the highlands
        /// Floating islands overhead, crystal clusters, rune stones, twisted trees with pink blossom, benches under lanterns, little pools.
        void Wilds(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            int floaters = 0, crystals = 0, stones = 0, trees = 0, benches = 0, pools = 0; var rng = new System.Random(2013);
            const float cell = 22f;
            for (var x = ctx.MinX; x < ctx.MaxX; x += cell)
            for (var z = ctx.MinZ; z < ctx.MaxZ; z += cell)
            {
                float px = x + (float)rng.NextDouble() * cell, pz = z + (float)rng.NextDouble() * cell; var roll = (float)rng.NextDouble(); var s = (float)rng.NextDouble();
                if (ctx.IsLand(px, pz, 2f) && roll < .09f) { var h = 18f + s * 22f; var g = ctx.OnGround(px, pz); Floater(ctx, g + Vector3.up * h, 3f + s * 4f, rng, s > .75f, h - 2f); floaters++; continue; }
                if (!Open(ctx, px, pz, 3f)) continue;
                var p = ctx.OnGround(px, pz); var f = FaceTo(p, NearestWalk(p)); var rot = Quaternion.LookRotation(f); var side = Vector3.Cross(Vector3.up, f); var up = Vector3.up;
                if (roll < .3f) { Crystals(ctx.Root, mk, p, .8f + s, s > .5f ? crystalM : runeM, rng); Mark(p, 1.6f); crystals++; }
                else if (roll < .42f) { var h = 2.4f + s * 2.4f; mk.Box(rock, p + up * (h / 2f - .3f), new Vector3(1f, h, .7f), rot * Quaternion.Euler(0, 0, (s - .5f) * 10f)); for (var k = 0; k < 4; k++) mk.Box(runeM, p + up * (.6f + k * h / 5f) + f * .36f, new Vector3(.3f + (k % 2) * .2f, .06f, .02f), rot * Quaternion.Euler(0, 0, k * 30f)); BoxCol(ctx.Root, p + up * h / 2f, new Vector3(1f, h, .7f), rot); Mark(p, 1.2f); stones++; }
                else if (roll < .6f) { var prev = p; for (var k = 0; k < 5; k++) { var dir = (up + new Vector3(Mathf.Sin(k + s * 9f), 0, Mathf.Cos(k * 1.3f)) * .4f).normalized; mk.Prism(trunkM, prev, dir, .26f - k * .03f, 1.2f, 6); prev += dir * 1.2f; } for (var k = 0; k < 4; k++) mk.Blob(k % 2 == 0 ? GlowM(new Color(1f, .6f, .74f), .5f) : leafM, prev + Quaternion.Euler(0, k * 90f, 0) * f * 1.2f + up * (k % 2) * .5f, new Vector3(1.6f, 1.1f, 1.6f), rng, .25f); ArtistBits.PostCollider(ctx.Root, p, 2.4f, .4f); var mid = (p + prev) / 2f; ArtistBits.PostCollider(ctx.Root, new Vector3(mid.x, p.y, mid.z), 4f, .5f); Mark(p, 2f); trees++; }
                else if (roll < .68f) { Bench(ctx.Root, mk, p, f); Lamp(ctx, mk, p - f * .8f + side * 1.6f, benches % 2 == 0); Mark(p, 2.2f); benches++; }
                else if (roll < .73f) { if (!Open(ctx, px, pz, 4f)) continue; mk.Prism(rockD, p - up * .3f, up, 3f, .5f, 14); mk.Prism(waterM, p + up * .18f, up, 2.7f, .02f, 14); BoxCol(ctx.Root, p, new Vector3(5.6f, .5f, 5.6f), Quaternion.identity); for (var k = 0; k < 6; k++) { var rp = p + Quaternion.Euler(0, k * 60f, 0) * f * 3f; mk.Blob(rock, rp + up * .1f, new Vector3(.6f, .4f, .6f), rng, .3f); ArtistBits.PostCollider(ctx.Root, rp, .6f, .6f); } Mark(p, 3.4f); pools++; }
            }
            Debug.Log($"The Melodic Bass Highlands: Rune Road {streetL:0} m, {floaters} floating islands, {crystals} crystal clusters, {stones} rune stones, {trees} blossom trees, {benches} benches, {pools} pools");
        }

        Vector3 NearestWalk(Vector3 p) { var best = center; var bd = float.MaxValue; foreach (var (q, _) in walkPoints) { var d = (q - p).sqrMagnitude; if (d < bd) { bd = d; best = q; } } return best; }

        /// Long highland grass, its tips touched pink by the dusk.
        void Verge(LandContext ctx)
        {
            var mat = WindMesh.Material(.3f, .8f, new Color(.24f, .36f, .32f));
            var meshes = new Dictionary<(int, int), WindMesh>(); var rng = new System.Random(2014);
            const float st = 2.4f;
            for (var x = ctx.MinX; x < ctx.MaxX; x += st)
            for (var z = ctx.MinZ; z < ctx.MaxZ; z += st)
            {
                var px = x + ((float)rng.NextDouble() - .5f) * st; var pz = z + ((float)rng.NextDouble() - .5f) * st;
                if (!ctx.IsLand(px, pz, 1f) || NearPath(px, pz, 3.6f) || InNoGrass(px, pz, 0f) || InFootprint(ctx, px, pz, -2f)) continue;
                if (Mathf.PerlinNoise(px * .04f + 7f, pz * .04f + 3f) < .4f) continue;
                var key = (Mathf.FloorToInt(px / 70f), Mathf.FloorToInt(pz / 70f));
                if (!meshes.TryGetValue(key, out var wm)) meshes[key] = wm = new WindMesh();
                var col = Color.Lerp(new Color(.32f, .46f, .42f), new Color(.42f, .52f, .46f), (float)rng.NextDouble());
                wm.Tuft(new Vector3(px, ctx.Ground(px, pz) - .04f, pz), .3f + (float)rng.NextDouble() * .4f, col, Color.Lerp(col, Pink, .5f), rng, 3, .08f);
            }
            foreach (var kv in meshes) kv.Value.Build("High grass", ctx.Root, mat);
        }
    }
}
