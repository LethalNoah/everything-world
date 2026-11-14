using System.Collections.Generic;
using UnityEngine;
using static EverythingWorld.World.PlaceTools;

namespace EverythingWorld.World
{
    /// The Liquid Rapids (WORLD_GUIDE.md): rivers and waterfalls on a summer evening. You arrive at the Falls, a terrace
    /// of warm stone beside a waterfall pouring down a rock face into a glowing pool, spray in the air, stepping stones over
    /// the stream. The River Walk follows a long river of rapids, rocks and footbridges to the Weir, where the water drops
    /// over stone steps beside a turning mill wheel. Willows, reeds, river stones and little cascades everywhere; canoes,
    /// jetties and fireflies. Warm gold light on cool blue water.
    /// Laid out on purpose (the build-land standard): RapidsTown.cs has the places and the people.
    public partial class RapidsKit : LandKit
    {
        public override string Land => "The Liquid Rapids";
        public override Vector3 ArtistFront(IslandWorldData data, IslandWorldData.Artist a) => facing.TryGetValue(a.name, out var f) ? f : base.ArtistFront(data, a);

        public static readonly Color Water = new Color(.3f, .62f, .86f), Foam = new Color(.86f, .94f, 1f), Gold = new Color(1f, .78f, .44f), Leaf = new Color(.34f, .54f, .3f), Ink = new Color(.08f, .08f, .1f), Ember = new Color(1f, .5f, .2f);
        const float PathW = 3f, WalkW = 4f;

        Material stone, stoneD, path, wood, woodD, iron, dark, white, leafM, willowM, trunkM, waterM, foamM, lampM;
        GlowHalos halos; Vector3 center, D, S; RapidsLife life;
        readonly Dictionary<(int, int), List<Vector3>> pathCells = new Dictionary<(int, int), List<Vector3>>();
        readonly List<(Vector3 p, int w)> walkPoints = new List<(Vector3, int)>();
        readonly Dictionary<string, Vector3> facing = new Dictionary<string, Vector3>();
        // x, radius, z
        readonly List<Vector3> noGrass = new List<Vector3>();
        Vector2 heart2, D2 = new Vector2(0, -1), S2 = new Vector2(1, 0); float plazaY, waterY, streetL;

        void MakeMaterials()
        {
            stone = WorldMaterials.Glow(new Color(.46f, .42f, .38f), .1f); stoneD = WorldMaterials.Glow(new Color(.34f, .32f, .31f), .08f); path = WorldMaterials.Glow(new Color(.48f, .42f, .34f), .08f); wood = WorldMaterials.Glow(new Color(.52f, .38f, .26f), .1f); woodD = WorldMaterials.Glow(new Color(.36f, .26f, .18f), .08f); iron = WorldMaterials.Solid(new Color(.2f, .2f, .22f), .5f); dark = WorldMaterials.Solid(new Color(.06f, .06f, .07f)); white = WorldMaterials.Glow(new Color(.88f, .88f, .86f), .14f);
            leafM = WorldMaterials.Glow(Leaf, .12f); willowM = WorldMaterials.Glow(new Color(.44f, .6f, .34f), .12f); trunkM = WorldMaterials.Glow(new Color(.36f, .28f, .22f), .1f);
            waterM = new Material(WorldMaterials.Glow(Water, .7f)) { name = "Rapids water" }; foamM = new Material(WorldMaterials.Glow(Foam, .9f)) { name = "Rapids foam" }; lampM = new Material(WorldMaterials.Glow(Gold, 1.6f)) { name = "Rapids lamps" };
        }

        Material GlowM(Color c, float s = 2f) => WorldMaterials.Glow(c, s * .65f);
        Material Paint(Color c) => WorldMaterials.Glow(c, .14f);
        static bool HasStation(IslandWorldData.Artist a) => a.stationId != 0;
        static float Foot(IslandWorldData.Artist a) => (a.songList?.Length ?? 1) > 1 ? 10f : 8f;

        public override bool Ground(float height, float slope, float patch, out Color color)
        {
            // lush summer riverbank: deep green, warm in the evening light, stony on the slopes
            if (slope > 24f) color = Color.Lerp(new Color(.46f, .44f, .4f), new Color(.54f, .5f, .44f), patch);
            else color = Color.Lerp(new Color(.32f, .5f, .3f), new Color(.42f, .56f, .32f), patch);
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
            for (var k = 0; k < 8; k++) { var dir = new Vector2(Mathf.Sin(k * Mathf.PI / 4f), Mathf.Cos(k * Mathf.PI / 4f)); var len = 36f; while (len < 100f && Inside(heart2 + dir * (len + 18f), 24f) && Inside(heart2 - dir * 26f, 18f)) len += 10f; if (len > streetL) { streetL = len; D2 = dir; } }
            S2 = new Vector2(-D2.y, D2.x); plazaY = Mathf.Max(data.HeightAt(heart2.x, heart2.y), waterY + 6f);
            var weir = P2(streetL + 14f, 0);
            var avoid = new List<Vector3>(); for (var d = -24f; d <= streetL; d += 18f) avoid.Add(new Vector3(heart2.x + D2.x * d + S2.x * 4f, 18f, heart2.y + D2.y * d + S2.y * 4f)); avoid.Add(new Vector3(weir.x, 16f, weir.y));
            ArtistPlacement.Spread(data, index, 42f, heart2, 36f, 32f, avoid, new List<string>(), Land, 320f);
            Level(data, index, heart2 - D2 * 6f, 20f, 32f, plazaY);
            for (var d = 16f; d <= streetL + 14f; d += 6f) { var p = P2(d, 4f); Level(data, index, p, 8f, 14f, Mathf.Lerp(plazaY, Mathf.Max(data.HeightAt(p.x, p.y), waterY + 3f), Mathf.Clamp01((d - 16f) / 30f))); }
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
            life = new GameObject("Rapids life").AddComponent<RapidsLife>();
            life.transform.SetParent(ctx.Root, false);
            life.center = center; life.water = waterM; life.foam = foamM; life.lamps = lampM;
            var mk = new MeshKit(); var solid = new MeshKit();
            Remember(ctx.OnGround(center.x, center.z), 0);
            TownStart(ctx);
            Falls(ctx, mk, solid);
            RiverWalk(ctx, mk, solid);
            Paths(ctx, mk, solid);
            Wilds(ctx, mk, solid);
            mk.Build("The Liquid Rapids", ctx.Root, false);
            solid.BuildCollider("The Liquid Rapids walls", ctx.Root);
        }

        public override void Finish(LandContext ctx) { Verge(ctx); halos.Build("Rapids glow", ctx.Root); }

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

        /// A picnic bench of planks.
        void Bench(Transform parent, MeshKit mk, Vector3 g, Vector3 face, float len = 2f)
        {
            var up = Vector3.up; var rot = Quaternion.LookRotation(face); var side = Vector3.Cross(up, face); for (var k = 0; k < 3; k++) mk.Box(wood, g + up * .46f + face * (-.16f + k * .16f), new Vector3(len, .05f, .13f), rot); mk.Box(wood, g + up * .8f - face * .26f, new Vector3(len, .2f, .05f), rot); foreach (var e in new[] { -1f, 1f }) mk.Box(woodD, g + up * .23f + side * e * (len / 2f - .15f), new Vector3(.08f, .46f, .5f), rot);
            BoxCol(parent, g + up * .45f, new Vector3(len, .9f, .5f), rot); Interact.Bench(tlife.transform, g + up * .52f - side * (len / 2f - .5f), g + up * .52f + side * (len / 2f - .5f), face);
        }

        /// A lantern on a wooden post.
        void Lamp(LandContext ctx, MeshKit mk, Vector3 g, bool light = false)
        {
            var up = Vector3.up; mk.Prism(woodD, g - up * .2f, up, .07f, 2.8f, 6); mk.Prism(lampM, g + up * 2.6f, up, .14f, .34f, 6, .8f); mk.Cone(iron, g + up * 2.94f, .2f, .16f, 6); halos.Add(g + up * 2.75f, 1.3f, Gold, .45f); ArtistBits.PostCollider(ctx.Root, g, 2.8f, .08f); if (light) ArtistBits.PointLight(ctx.Root, g + up * 2.6f, Gold, 11f, 1.3f);
        }

        /// A rock pool: a ring of mossy boulders round a small glowing pool, a spring bubbling up in it (spray).
        void Cascade(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 g, Vector3 face, float s, System.Random rng)
        {
            var up = Vector3.up; var r = 1.3f * s; var side = Vector3.Cross(up, face);
            mk.Prism(stoneD, g - up * .15f, up, r, .4f, 16); mk.Prism(waterM, g + up * .26f, up, r * .82f, .02f, 16); mk.Prism(foamM, g + up * .285f, up, r * .18f, .01f, 10);
            for (var k = 0; k < 8; k++)
            {
                var a = k * 45f + (float)rng.NextDouble() * 20f; var d = Quaternion.Euler(0, a, 0) * face; var big = k == 0 || k == 3 ? 1.6f : 1f;
                var bp = g + d * (r + .15f) + up * (.15f * big); var sz = new Vector3(.45f, .35f, .4f) * s * big * (.8f + (float)rng.NextDouble() * .4f);
                mk.Blob(k % 3 == 0 ? stoneD : stone, bp, sz, rng, .3f);
                if (k % 2 == 0) mk.Blob(leafM, bp + up * sz.y * .8f, new Vector3(sz.x * .8f, sz.y * .25f, sz.z * .8f), rng, .3f);   // moss
                if (big > 1f) ArtistBits.PostCollider(ctx.Root, bp, sz.y * 2f, sz.x);
            }
            for (var k = 0; k < 5; k++) { var rp = g + Quaternion.Euler(0, 200f + k * 14f, 0) * face * (r + .5f); rp = ctx.OnGround(rp.x, rp.z); mk.Prism(Paint(new Color(.5f, .56f, .3f)), rp, (up + side * ((k % 3) - 1) * .1f).normalized, .02f, .9f + k * .12f, 3); }
            life.sprays.Add(g + up * .3f);
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
                    var dd = Mathf.Min(d, end); var q = from + dir * dd + side * Mathf.Sin(dd / 14f) * Mathf.Min(1.6f, end * .04f);
                    if (!ctx.IsLand(q.x, q.z, 0f)) break;
                    pts.Add(ctx.OnGround(q.x, q.z)); if (dd >= end) break;
                }
                if (pts.Count < 2) return;
                Strip(mk, pts, path, PathW, .1f, ctx);
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

        // ---------------------------------------------------------------- the rest of the riverlands
        /// Weeping willows, reed beds, little cascades, river stones, canoes pulled up on the bank, picnic benches under lanterns, wildflowers.
        void Wilds(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            int willows = 0, reeds = 0, cascades = 0, stones = 0, canoes = 0, benches = 0, flowers = 0; var rng = new System.Random(2004);
            const float cell = 20f;
            for (var x = ctx.MinX; x < ctx.MaxX; x += cell)
            for (var z = ctx.MinZ; z < ctx.MaxZ; z += cell)
            {
                float px = x + (float)rng.NextDouble() * cell, pz = z + (float)rng.NextDouble() * cell; var roll = (float)rng.NextDouble(); var s = (float)rng.NextDouble();
                if (!Open(ctx, px, pz, 3f)) continue;
                var p = ctx.OnGround(px, pz); var f = FaceTo(p, NearestWalk(p)); var rot = Quaternion.LookRotation(f); var side = Vector3.Cross(Vector3.up, f); var up = Vector3.up;
                if (roll < .22f) { var h = 4f + s * 2f; mk.Prism(trunkM, p, (up + side * (s - .5f) * .2f).normalized, .34f, h, 7, .6f); var top = p + up * h; for (var k = 0; k < 10; k++) { var d = Quaternion.Euler(0, k * 36f + s * 30f, 0) * f; mk.Box(willowM, top + d * 1.4f - up * 1.2f, new Vector3(.5f, 2.6f + (k % 3) * .6f, .14f), Quaternion.LookRotation(d) * Quaternion.Euler(-10f, 0, 0)); } mk.Blob(willowM, top + up * .4f, new Vector3(2f, 1f, 2f), rng, .3f); ArtistBits.PostCollider(ctx.Root, p, h, .4f); Mark(p, 2.6f); willows++; }
                else if (roll < .34f) { for (var k = 0; k < 14; k++) { var rp = p + new Vector3((float)rng.NextDouble() - .5f, 0, (float)rng.NextDouble() - .5f) * 3f; rp = ctx.OnGround(rp.x, rp.z); var h = 1f + (float)rng.NextDouble() * .8f; mk.Prism(Paint(new Color(.5f, .56f, .3f)), rp, (up + new Vector3((float)rng.NextDouble() - .5f, 0, (float)rng.NextDouble() - .5f) * .2f).normalized, .02f, h, 3); if (k % 3 == 0) mk.Prism(Paint(new Color(.4f, .28f, .18f)), rp + up * h, up, .05f, .3f, 5); } reeds++; }
                else if (roll < .44f) { if (!Open(ctx, px, pz, 5f)) continue; Cascade(ctx, mk, solid, p, f, .8f + s * .5f, rng); Mark(p, 3.6f); cascades++; }
                else if (roll < .54f) { for (var k = 0; k < 5; k++) mk.Blob(k % 2 == 0 ? stone : stoneD, p + Quaternion.Euler(0, k * 72f + s * 50f, 0) * f * (k == 0 ? 0f : 1.2f) + up * .2f, new Vector3(.8f, .5f, .7f) * (1f - k * .1f), rng, .3f); ArtistBits.PostCollider(ctx.Root, p, .8f, 1.6f); stones++; }
                else if (roll < .6f) { var cr = Quaternion.LookRotation(side); var col = Paint(s > .5f ? new Color(.8f, .3f, .2f) : new Color(.2f, .5f, .7f)); mk.Blob(col, p + up * .25f, new Vector3(.4f, .25f, 2f), rng, 0f); mk.Blob(dark, p + up * .38f, new Vector3(.3f, .08f, 1.7f), rng, 0f); mk.Box(woodD, p + up * .5f + side * .2f, new Vector3(.06f, .06f, 1.6f), cr * Quaternion.Euler(0, 30f, 0)); BoxCol(ctx.Root, p + up * .25f, new Vector3(.8f, .5f, 4f), cr); canoes++; }
                else if (roll < .68f) { Bench(ctx.Root, mk, p, f); Lamp(ctx, mk, p - f * .8f + side * 1.6f, benches % 2 == 0); Mark(p, 2.2f); benches++; }
                else if (roll < .74f) { var cols = new[] { new Color(1f, .8f, .3f), new Color(.9f, .5f, .7f), new Color(.6f, .6f, 1f), Color.white }; for (var k = 0; k < 12; k++) { var fp = p + new Vector3((float)rng.NextDouble() - .5f, 0, (float)rng.NextDouble() - .5f) * 4f; fp = ctx.OnGround(fp.x, fp.z); mk.Prism(leafM, fp, up, .015f, .4f, 3); mk.Blob(Paint(cols[k % 4]), fp + up * .42f, Vector3.one * .06f, rng, 0f); } flowers++; }
            }
            Debug.Log($"The Liquid Rapids: the River Walk {streetL:0} m, {willows} willows, {reeds} reed beds, {cascades} rock pools, {stones} river stones, {canoes} canoes, {benches} benches, {flowers} wildflower patches");
        }

        Vector3 NearestWalk(Vector3 p) { var best = center; var bd = float.MaxValue; foreach (var (q, _) in walkPoints) { var d = (q - p).sqrMagnitude; if (d < bd) { bd = d; best = q; } } return best; }

        /// Long riverbank grass, lit gold at the tips by the low sun.
        void Verge(LandContext ctx)
        {
            var mat = WindMesh.Material(.35f, .8f, new Color(.36f, .52f, .3f));
            var meshes = new Dictionary<(int, int), WindMesh>(); var rng = new System.Random(2005);
            const float st = 2.2f;
            for (var x = ctx.MinX; x < ctx.MaxX; x += st)
            for (var z = ctx.MinZ; z < ctx.MaxZ; z += st)
            {
                var px = x + ((float)rng.NextDouble() - .5f) * st; var pz = z + ((float)rng.NextDouble() - .5f) * st;
                if (!ctx.IsLand(px, pz, 1f) || NearPath(px, pz, 3.6f) || InNoGrass(px, pz, 0f) || InFootprint(ctx, px, pz, -2f)) continue;
                if (Mathf.PerlinNoise(px * .04f + 2f, pz * .04f + 8f) < .35f) continue;
                var key = (Mathf.FloorToInt(px / 70f), Mathf.FloorToInt(pz / 70f));
                if (!meshes.TryGetValue(key, out var wm)) meshes[key] = wm = new WindMesh();
                var col = Color.Lerp(new Color(.32f, .5f, .3f), new Color(.46f, .58f, .32f), (float)rng.NextDouble());
                wm.Tuft(new Vector3(px, ctx.Ground(px, pz) - .04f, pz), .4f + (float)rng.NextDouble() * .5f, col, Color.Lerp(col, Gold, .5f), rng, 4, .08f);
            }
            foreach (var kv in meshes) kv.Value.Build("Rapids grass", ctx.Root, mat);
        }
    }
}
