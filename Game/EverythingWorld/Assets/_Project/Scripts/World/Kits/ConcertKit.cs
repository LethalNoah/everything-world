using System.Collections.Generic;
using UnityEngine;
using static EverythingWorld.World.PlaceTools;

namespace EverythingWorld.World
{
    /// The Concert Isles (WORLD_GUIDE.md): moonlit palace gardens. You arrive on the Moon Terrace, a marble terrace with a
    /// balustrade round a tiered fountain, clipped hedges in a parterre, and a grand piano at the edge under a huge pale
    /// moon. The Palace Walk, a gravel allée between cone topiaries under iron arches hung with candle chandeliers, leads
    /// to the Concert Hall: a white colonnade under a dome, its golden organ pipes glowing at the back. Every composer has
    /// a garden room of their own walled in hedges. Silver-teal moonlight, white marble, dark green, warm candles.
    /// Laid out on purpose (the build-land standard): ConcertTown.cs has the places and the people.
    public partial class ConcertKit : LandKit
    {
        public override string Land => "The Concert Isles";
        public override Vector3 ArtistFront(IslandWorldData data, IslandWorldData.Artist a) => facing.TryGetValue(a.name, out var f) ? f : base.ArtistFront(data, a);

        public static readonly Color Candle = new Color(1f, .78f, .42f), Marble = new Color(.84f, .84f, .86f), Hedge = new Color(.12f, .28f, .18f), Silver = new Color(.8f, .9f, 1f), Ink = new Color(.08f, .1f, .12f), Gold = new Color(1f, .8f, .36f), Water = new Color(.3f, .6f, .7f);
        const float PathW = 3f, WalkW = 6f;

        Material marble, marbleD, gravel, hedgeM, iron, dark, white, leafM, trunkM, candleM, waterM, windowM, goldM;
        GlowHalos halos; Vector3 center, D, S; ConcertLife life;
        readonly Dictionary<(int, int), List<Vector3>> pathCells = new Dictionary<(int, int), List<Vector3>>();
        readonly List<(Vector3 p, int w)> walkPoints = new List<(Vector3, int)>();
        readonly Dictionary<string, Vector3> facing = new Dictionary<string, Vector3>();
        // x, radius, z
        readonly List<Vector3> noGrass = new List<Vector3>();
        Vector2 heart2, D2 = new Vector2(0, -1), S2 = new Vector2(1, 0); float plazaY, waterY, streetL;

        void MakeMaterials()
        {
            marble = WorldMaterials.Glow(new Color(.4f, .42f, .46f), .08f); marbleD = WorldMaterials.Glow(new Color(.28f, .3f, .34f), .06f); gravel = WorldMaterials.Glow(new Color(.24f, .25f, .26f), .06f); hedgeM = WorldMaterials.Glow(Hedge, .1f); goldM = WorldMaterials.Glow(Gold, .5f);
            iron = WorldMaterials.Solid(new Color(.12f, .12f, .14f), .5f); dark = WorldMaterials.Solid(new Color(.04f, .04f, .06f)); white = WorldMaterials.Glow(new Color(.86f, .86f, .88f), .16f); leafM = WorldMaterials.Glow(new Color(.14f, .3f, .2f), .1f); trunkM = WorldMaterials.Glow(new Color(.26f, .2f, .16f), .1f);
            candleM = new Material(WorldMaterials.Glow(Candle, 1.8f)) { name = "Concert candles" }; waterM = new Material(WorldMaterials.Glow(Water, .6f)) { name = "Concert water" }; windowM = new Material(WorldMaterials.Glow(new Color(1f, .8f, .5f), 1.1f)) { name = "Concert windows" };
        }

        Material GlowM(Color c, float s = 2f) => WorldMaterials.Glow(c, s * .65f);
        Material Paint(Color c) => WorldMaterials.Glow(c, .14f);
        static bool HasStation(IslandWorldData.Artist a) => a.stationId != 0;
        static float Foot(IslandWorldData.Artist a) => (a.songList?.Length ?? 1) > 1 ? 10f : 8f;

        public override bool Ground(float height, float slope, float patch, out Color color)
        {
            // moonlit lawn: deep green, grey rock on the slopes
            if (slope > 24f) color = Color.Lerp(new Color(.3f, .34f, .36f), new Color(.38f, .42f, .44f), patch);
            else color = Color.Lerp(new Color(.14f, .3f, .22f), new Color(.2f, .36f, .26f), patch);
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
            for (var k = 0; k < 8; k++) { var dir = new Vector2(Mathf.Sin(k * Mathf.PI / 4f), Mathf.Cos(k * Mathf.PI / 4f)); var len = 40f; while (len < 100f && Inside(heart2 + dir * (len + 24f), 30f)) len += 10f; if (len > streetL) { streetL = len; D2 = dir; } }
            S2 = new Vector2(-D2.y, D2.x); plazaY = Mathf.Max(data.HeightAt(heart2.x, heart2.y), waterY + 6f);
            var hall = P2(streetL + 14f, 0);
            var avoid = new List<Vector3>(); for (var d = 0f; d <= streetL; d += 20f) avoid.Add(new Vector3(heart2.x + D2.x * d, 18f, heart2.y + D2.y * d)); avoid.Add(new Vector3(hall.x, 26f, hall.y));
            ArtistPlacement.Spread(data, index, 46f, heart2, 40f, 36f, avoid, new List<string>(), Land, 320f);
            Level(data, index, heart2, 22f, 36f, plazaY);
            for (var d = 22f; d <= streetL; d += 8f) { var p = P2(d, 0); Level(data, index, p, 7f, 14f, Mathf.Lerp(plazaY, Mathf.Max(data.HeightAt(p.x, p.y), waterY + 3f), Mathf.Clamp01((d - 22f) / 30f))); }
            Level(data, index, hall, 18f, 30f, Mathf.Max(data.HeightAt(hall.x, hall.y), waterY + 3f));
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
            life = new GameObject("Concert life").AddComponent<ConcertLife>();
            life.transform.SetParent(ctx.Root, false);
            life.center = center; life.candles = candleM; life.water = waterM; life.windows = windowM;
            var mk = new MeshKit(); var solid = new MeshKit();
            Remember(ctx.OnGround(center.x, center.z), 0);
            TownStart(ctx);
            MoonTerrace(ctx, mk, solid);
            PalaceWalk(ctx, mk, solid);
            ConcertHall(ctx, mk, solid);
            Paths(ctx, mk, solid);
            Wilds(ctx, mk, solid);
            mk.Build("The Concert Isles", ctx.Root, false);
            solid.BuildCollider("The Concert Isles walls", ctx.Root);
        }

        public override void Finish(LandContext ctx) { Verge(ctx); halos.Build("Concert glow", ctx.Root); }

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

        /// A marble bench.
        void Bench(Transform parent, MeshKit mk, Vector3 g, Vector3 face, float len = 2f)
        {
            var up = Vector3.up; var rot = Quaternion.LookRotation(face); var side = Vector3.Cross(up, face); mk.Box(marble, g + up * .42f, new Vector3(len, .12f, .5f), rot); foreach (var e in new[] { -1f, 1f }) mk.Box(marbleD, g + up * .2f + side * e * (len / 2f - .25f), new Vector3(.3f, .4f, .4f), rot);
            BoxCol(parent, g + up * .25f, new Vector3(len, .5f, .5f), rot); Interact.Bench(tlife.transform, g + up * .48f - side * (len / 2f - .5f), g + up * .48f + side * (len / 2f - .5f), face);
        }

        /// A garden lantern: a candle in a glass box on a black post.
        void Lamp(LandContext ctx, MeshKit mk, Vector3 g, bool light = false)
        {
            var up = Vector3.up; mk.Prism(iron, g - up * .2f, up, .05f, 2.8f, 6, .7f); mk.Box(iron, g + up * 2.6f, new Vector3(.36f, .06f, .36f), Quaternion.identity); mk.Box(GlowM(new Color(1f, .9f, .7f), .4f), g + up * 2.85f, new Vector3(.3f, .44f, .3f), Quaternion.identity); mk.Prism(candleM, g + up * 2.7f, up, .05f, .24f, 6); mk.Cone(iron, g + up * 3.07f, .26f, .24f, 4, 45f); halos.Add(g + up * 2.85f, 1.3f, Candle, .45f); ArtistBits.PostCollider(ctx.Root, g, 2.8f, .08f); if (light) ArtistBits.PointLight(ctx.Root, g + up * 2.6f, Candle, 11f, 1.3f);
        }

        /// A clipped cone of hedge in a stone pot.
        void Topiary(LandContext ctx, MeshKit mk, Vector3 g, float s = 1f)
        {
            var up = Vector3.up; mk.Prism(marbleD, g - up * .1f, up, .6f * s, .7f * s, 8, 1.2f); mk.Cone(hedgeM, g + up * .6f * s, .7f * s, 2.6f * s, 8); ArtistBits.PostCollider(ctx.Root, g, 2.4f * s, .6f * s);
        }

        /// A chandelier of candles hung in the air.
        void Chandelier(MeshKit mk, Vector3 at, float r = .8f)
        {
            var up = Vector3.up; mk.Prism(iron, at, up, .02f, 2f, 4); for (var k = 0; k < 16; k++) { var a = k * Mathf.PI / 8f; var p = at + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * r; mk.Box(iron, (p + at) / 2f, new Vector3(.03f, .03f, r), Quaternion.LookRotation(p - at)); } for (var k = 0; k < 8; k++) { var a = k * Mathf.PI / 4f; var p = at + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * r; mk.Prism(white, p, up, .03f, .16f, 5); mk.Blob(candleM, p + up * .22f, new Vector3(.04f, .07f, .04f), new System.Random(k), 0f); }
            halos.Add(at + up * .2f, r * 2.4f, Candle, .4f);
        }

        // ---------------------------------------------------------------- the lanes to the gardens
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
                Strip(mk, pts, gravel, PathW, .13f, ctx);
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

        // ---------------------------------------------------------------- the rest of the gardens
        /// Topiary cones, cypresses, rose beds, marble urns and broken columns, small fountains, benches under lanterns, a chandelier hung from an old tree.
        void Wilds(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            int topiary = 0, trees = 0, roses = 0, urns = 0, fountains = 0, benches = 0, chandeliers = 0; var rng = new System.Random(1791);
            const float cell = 20f;
            for (var x = ctx.MinX; x < ctx.MaxX; x += cell)
            for (var z = ctx.MinZ; z < ctx.MaxZ; z += cell)
            {
                float px = x + (float)rng.NextDouble() * cell, pz = z + (float)rng.NextDouble() * cell; var roll = (float)rng.NextDouble(); var s = (float)rng.NextDouble();
                if (!Open(ctx, px, pz, 3f)) continue;
                var p = ctx.OnGround(px, pz); var f = FaceTo(p, NearestWalk(p)); var rot = Quaternion.LookRotation(f); var side = Vector3.Cross(Vector3.up, f); var up = Vector3.up;
                if (roll < .18f) { for (var k = 0; k < 3; k++) Topiary(ctx, mk, p + side * (k - 1) * 2.4f, .8f + s * .4f); Mark(p, 3.6f); topiary++; }
                else if (roll < .4f) { var h = 6f + s * 5f; for (var k = 0; k < 1 + (int)(s * 3f); k++) { var tp = p + side * (k * 1.8f) - f * (k % 2) * 1.2f; mk.Prism(trunkM, tp, up, .16f, 1f, 5); mk.Cone(leafM, tp + up * .8f, .9f, h, 8); ArtistBits.PostCollider(ctx.Root, tp, 2f, .5f); } Mark(p, 2f); trees++; }
                else if (roll < .5f) { mk.Box(marbleD, p + up * .2f, new Vector3(4f, .4f, 1.6f), rot); BoxCol(ctx.Root, p + up * .3f, new Vector3(4f, .6f, 1.6f), rot); for (var k = 0; k < 10; k++) { var rp = p + side * (-1.7f + (k % 5) * .85f) + f * ((k / 5) * .7f - .35f) + up * .55f; mk.Blob(hedgeM, rp, new Vector3(.4f, .3f, .4f), rng, .2f); mk.Blob(Paint(k % 3 == 0 ? new Color(.9f, .86f, .9f) : new Color(.8f, .2f, .3f)), rp + up * .28f, Vector3.one * .12f, rng, 0f); } Mark(p, 2.2f); roses++; }
                else if (roll < .6f) { if (s < .5f) { mk.Box(marble, p + up * .5f, new Vector3(.9f, 1f, .9f), rot); mk.Prism(marble, p + up * 1f, up, .4f, .6f, 10, 1.4f); mk.Prism(marble, p + up * 1.6f, up, .56f, .1f, 10); BoxCol(ctx.Root, p + up * .85f, new Vector3(1.1f, 1.7f, 1.1f), rot); } else { mk.Box(marble, p + up * .2f, new Vector3(1.4f, .4f, 1.4f), rot); mk.Prism(marble, p + up * .4f, up, .4f, 1.6f + s * 2f, 10); mk.Box(marble, p + up * .3f + f * 1.4f + side * .6f, new Vector3(.8f, .6f, 1.6f), rot * Quaternion.Euler(0, 30f, 80f)); ArtistBits.PostCollider(ctx.Root, p, 3f, .5f); BoxCol(ctx.Root, p + up * .3f + f * 1.4f + side * .6f, new Vector3(1.6f, .8f, 1.6f), rot); } Mark(p, 1.6f); urns++; }
                else if (roll < .66f) { if (!Open(ctx, px, pz, 4f)) continue; mk.Prism(marble, p - up * .3f, up, 2.4f, .8f, 16); mk.Prism(waterM, p + up * .48f, up, 2.1f, .02f, 16); mk.Prism(marble, p, up, .2f, 1.4f, 8); mk.Prism(marble, p + up * 1.4f, up, .7f, .14f, 12); mk.Prism(waterM, p + up * 1.55f, up, .6f, .02f, 12); BoxCol(ctx.Root, p + up * .2f, new Vector3(4.8f, .8f, 4.8f), Quaternion.identity); ArtistBits.PostCollider(ctx.Root, p, 1.7f, .8f); life.fountains.Add(p + up * 1.6f); Mark(p, 2.6f); fountains++; }
                else if (roll < .74f) { Bench(ctx.Root, mk, p, f); Lamp(ctx, mk, p - f * .8f + side * 1.6f, benches % 2 == 0); Mark(p, 2.2f); benches++; }
                else if (roll < .78f && chandeliers < 14) { if (!Open(ctx, px, pz, 4f)) continue; var tr = p; mk.Prism(trunkM, tr, up, .4f, 4f, 7, .7f); var br = tr + up * 4f; mk.Prism(trunkM, br, (f + up).normalized, .2f, 3f, 6, .6f); for (var k = 0; k < 4; k++) mk.Blob(leafM, br + Quaternion.Euler(0, k * 90f, 0) * f * 1.6f + up * 1.4f, new Vector3(2f, 1.2f, 2f), rng, .3f); ArtistBits.PostCollider(ctx.Root, tr, 4f, .5f); var ch = br + (f + up).normalized * 2.6f - up * 2.2f; Chandelier(mk, ch, .7f); ArtistBits.PointLight(ctx.Root, ch, Candle, 10f, 1.4f); Mark(p, 3f); chandeliers++; }
            }
            Debug.Log($"The Concert Isles: Palace Walk {streetL:0} m, {topiary} topiary rows, {trees} cypress groves, {roses} rose beds, {urns} urns and columns, {fountains} fountains, {benches} benches, {chandeliers} chandelier trees");
        }

        Vector3 NearestWalk(Vector3 p) { var best = center; var bd = float.MaxValue; foreach (var (q, _) in walkPoints) { var d = (q - p).sqrMagnitude; if (d < bd) { bd = d; best = q; } } return best; }

        /// Soft lawn grass, silvered at the tips by the moon.
        void Verge(LandContext ctx)
        {
            var mat = WindMesh.Material(.25f, .8f, new Color(.16f, .32f, .24f));
            var meshes = new Dictionary<(int, int), WindMesh>(); var rng = new System.Random(1792);
            const float st = 2.6f;
            for (var x = ctx.MinX; x < ctx.MaxX; x += st)
            for (var z = ctx.MinZ; z < ctx.MaxZ; z += st)
            {
                var px = x + ((float)rng.NextDouble() - .5f) * st; var pz = z + ((float)rng.NextDouble() - .5f) * st;
                if (!ctx.IsLand(px, pz, 1f) || NearPath(px, pz, 3.6f) || InNoGrass(px, pz, 0f) || InFootprint(ctx, px, pz, -2f)) continue;
                if (Mathf.PerlinNoise(px * .04f + 5f, pz * .04f + 2f) < .45f) continue;
                var key = (Mathf.FloorToInt(px / 70f), Mathf.FloorToInt(pz / 70f));
                if (!meshes.TryGetValue(key, out var wm)) meshes[key] = wm = new WindMesh();
                var col = Color.Lerp(new Color(.14f, .3f, .22f), new Color(.22f, .38f, .28f), (float)rng.NextDouble());
                wm.Tuft(new Vector3(px, ctx.Ground(px, pz) - .04f, pz), .24f + (float)rng.NextDouble() * .3f, col, Color.Lerp(col, Silver, .5f), rng, 3, .08f);
            }
            foreach (var kv in meshes) kv.Value.Build("Concert grass", ctx.Root, mat);
        }
    }
}
