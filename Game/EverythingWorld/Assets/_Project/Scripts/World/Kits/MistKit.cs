using System.Collections.Generic;
using UnityEngine;
using static EverythingWorld.World.PlaceTools;

namespace EverythingWorld.World
{
    /// The Ambient Mists (WORLD_GUIDE.md): foggy dawn fields in faded home-video colours. You arrive in a meadow by a lone
    /// tree with a rope swing, an old television left in the grass showing colour bars, and a line of electricity pylons
    /// marching away into the haze. The Pylon Walk follows the wires to a still lake with a lone tree on an island and an
    /// old radio tower behind it, a jetty and a rowing boat. Sheep wander; hay bales, fences and old TVs dot the fields.
    /// Faded teal, washed orange, haze and muted green; soft and a little sad, like a memory of a summer.
    /// Laid out on purpose (the build-land standard): MistTown.cs has the places and the people.
    public partial class MistKit : LandKit
    {
        public override string Land => "The Ambient Mists";
        public override Vector3 ArtistFront(IslandWorldData data, IslandWorldData.Artist a) => facing.TryGetValue(a.name, out var f) ? f : base.ArtistFront(data, a);

        public static readonly Color Teal = new Color(.5f, .66f, .64f), Orange = new Color(.9f, .65f, .5f), Haze = new Color(.8f, .78f, .74f), Moss = new Color(.54f, .6f, .42f), Ink = new Color(.12f, .12f, .12f), Bulb = new Color(1f, .8f, .55f);
        const float PathW = 2.6f, WalkW = 3.4f;

        Material dirtM, wood, iron, dark, white, leafM, trunkM, lampM, screenM, waterM, hay, wool;
        GlowHalos halos; Vector3 center, D, S; MistLife life;
        readonly Dictionary<(int, int), List<Vector3>> pathCells = new Dictionary<(int, int), List<Vector3>>();
        readonly List<(Vector3 p, int w)> walkPoints = new List<(Vector3, int)>();
        readonly Dictionary<string, Vector3> facing = new Dictionary<string, Vector3>();
        // x, radius, z
        readonly List<Vector3> noGrass = new List<Vector3>();
        Vector2 heart2, D2 = new Vector2(0, -1), S2 = new Vector2(1, 0); float plazaY, waterY, streetL;

        void MakeMaterials()
        {
            dirtM = WorldMaterials.Glow(new Color(.5f, .44f, .36f), .1f); wood = WorldMaterials.Glow(new Color(.46f, .36f, .28f), .1f); iron = WorldMaterials.Solid(new Color(.3f, .3f, .32f), .5f); dark = WorldMaterials.Solid(new Color(.1f, .1f, .1f)); white = WorldMaterials.Glow(new Color(.82f, .8f, .76f), .14f);
            leafM = WorldMaterials.Glow(new Color(.42f, .5f, .38f), .1f); trunkM = WorldMaterials.Glow(new Color(.34f, .28f, .24f), .1f); hay = WorldMaterials.Glow(new Color(.8f, .68f, .44f), .12f); wool = WorldMaterials.Glow(new Color(.86f, .84f, .8f), .14f);
            lampM = new Material(WorldMaterials.Glow(Bulb, 1.4f)) { name = "Mist lamps" }; screenM = new Material(WorldMaterials.Glow(new Color(.7f, .8f, .9f), 1f)) { name = "Mist screens" }; waterM = new Material(WorldMaterials.Glow(new Color(.46f, .6f, .62f), .5f)) { name = "Mist water" };
        }

        Material GlowM(Color c, float s = 2f) => WorldMaterials.Glow(c, s * .65f);
        Material Paint(Color c) => WorldMaterials.Glow(c, .14f);
        static bool HasStation(IslandWorldData.Artist a) => a.stationId != 0;
        static float Foot(IslandWorldData.Artist a) => a.name == "Boards of Canada" ? 15f : a.name == "Aphex Twin" ? 13f : 8f;

        public override bool Ground(float height, float slope, float patch, out Color color)
        {
            // faded meadow: muted green going teal, washed out
            if (slope > 24f) color = Color.Lerp(new Color(.5f, .5f, .46f), new Color(.58f, .56f, .5f), patch);
            else color = Color.Lerp(new Color(.46f, .54f, .4f), new Color(.52f, .58f, .48f), patch);
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
            for (var k = 0; k < 8; k++) { var dir = new Vector2(Mathf.Sin(k * Mathf.PI / 4f), Mathf.Cos(k * Mathf.PI / 4f)); var len = 40f; while (len < 100f && Inside(heart2 + dir * (len + 28f), 30f)) len += 10f; if (len > streetL) { streetL = len; D2 = dir; } }
            S2 = new Vector2(-D2.y, D2.x); plazaY = Mathf.Max(data.HeightAt(heart2.x, heart2.y), waterY + 6f);
            var keep = new List<string>();
            void Put(string name, float d, float s, float room) { var p = P2(d, s); if (!Inside(p, room)) return; foreach (var a in data.layout.artists) if (a.sceneId == isl.sceneId && a.name == name) { a.x = p.x; a.z = p.y; keep.Add(name); } }
            Put("Boards of Canada", 8f, -56f, 24f); Put("Aphex Twin", 4f, 52f, 22f);
            var lake = P2(streetL + 24f, 0);
            var avoid = new List<Vector3>(); for (var d = 0f; d <= streetL; d += 20f) avoid.Add(new Vector3(heart2.x + D2.x * d, 14f, heart2.y + D2.y * d)); avoid.Add(new Vector3(lake.x, 30f, lake.y));
            ArtistPlacement.Spread(data, index, 46f, heart2, 40f, 36f, avoid, keep, Land, 320f);
            Level(data, index, heart2, 14f, 30f, plazaY);
            var lakeY = Mathf.Max(data.HeightAt(lake.x, lake.y), waterY + 2.5f); Level(data, index, lake, 22f, 34f, lakeY);
            for (var d = 16f; d <= streetL; d += 8f) { var p = P2(d, 0); Level(data, index, p, 3f, 9f, Mathf.Lerp(plazaY, lakeY, Mathf.Clamp01((d - 16f) / Mathf.Max(1f, streetL - 16f)))); }
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
            life = new GameObject("Mist life").AddComponent<MistLife>();
            life.transform.SetParent(ctx.Root, false);
            life.center = center; life.lamps = lampM; life.screens = screenM; life.water = waterM;
            var mk = new MeshKit(); var solid = new MeshKit();
            Remember(ctx.OnGround(center.x, center.z), 0);
            TownStart(ctx);
            Meadow(ctx, mk, solid);
            PylonWalk(ctx, mk, solid);
            TheLake(ctx, mk, solid);
            Paths(ctx, mk, solid);
            Wilds(ctx, mk, solid);
            mk.Build("The Ambient Mists", ctx.Root, false);
            solid.BuildCollider("The Ambient Mists walls", ctx.Root);
        }

        public override void Finish(LandContext ctx) { Verge(ctx); halos.Build("Mist glow", ctx.Root); }

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

        /// A wooden bench.
        void Bench(Transform parent, MeshKit mk, Vector3 g, Vector3 face, float len = 2f)
        {
            var up = Vector3.up; var rot = Quaternion.LookRotation(face); var side = Vector3.Cross(up, face); for (var k = 0; k < 3; k++) mk.Box(wood, g + up * .46f + face * (-.16f + k * .16f), new Vector3(len, .05f, .13f), rot); mk.Box(wood, g + up * .8f - face * .26f, new Vector3(len, .2f, .05f), rot); foreach (var e in new[] { -1f, 1f }) mk.Box(wood, g + up * .23f + side * e * (len / 2f - .15f), new Vector3(.08f, .46f, .5f), rot);
            BoxCol(parent, g + up * .45f, new Vector3(len, .9f, .5f), rot); Interact.Bench(tlife.transform, g + up * .52f - side * (len / 2f - .5f), g + up * .52f + side * (len / 2f - .5f), face);
        }

        /// An old wooden lamp post with a warm bulb.
        void Lamp(LandContext ctx, MeshKit mk, Vector3 g, bool light = false)
        {
            var up = Vector3.up; mk.Prism(wood, g - up * .2f, up, .08f, 3.4f, 6, .8f); mk.Box(wood, g + up * 3.1f + S * .3f, new Vector3(.06f, .06f, .7f), Quaternion.LookRotation(S)); mk.Blob(lampM, g + up * 2.9f + S * .6f, new Vector3(.12f, .16f, .12f), new System.Random(1), 0f); mk.Cone(iron, g + up * 3.02f + S * .6f, .2f, .12f, 8); halos.Add(g + up * 2.9f + S * .6f, 1.4f, Bulb, .4f); ArtistBits.PostCollider(ctx.Root, g, 3.2f, .1f); if (light) ArtistBits.PointLight(ctx.Root, g + up * 2.6f + S * .6f, Bulb, 11f, 1.2f);
        }

        /// An old box television left in the grass, its screen showing faded colour bars.
        void OldTV(Transform parent, MeshKit mk, Vector3 g, Vector3 face, float s = 1f)
        {
            var up = Vector3.up; var rot = Quaternion.LookRotation(face); var side = Vector3.Cross(up, face); mk.Box(Paint(new Color(.42f, .34f, .26f)), g + up * .4f * s, new Vector3(.9f, .7f, .7f) * s, rot); mk.Box(screenM, g + up * .42f * s + face * .351f * s, new Vector3(.62f, .48f, .02f) * s, rot);
            var bars = new[] { new Color(.8f, .8f, .7f), new Color(.8f, .76f, .4f), new Color(.4f, .7f, .7f), new Color(.4f, .66f, .4f), new Color(.7f, .4f, .6f), new Color(.7f, .36f, .3f), new Color(.3f, .34f, .6f) };
            for (var k = 0; k < 7; k++) mk.Box(GlowM(bars[k], .7f), g + up * .46f * s + face * .362f * s + side * (-.26f + k * .087f) * s, new Vector3(.084f, .34f, .01f) * s, rot);
            foreach (var e in new[] { -1f, 1f }) mk.Prism(iron, g + up * .76f * s, (up + side * e * .5f).normalized, .01f, .5f * s, 3); BoxCol(parent, g + up * .4f * s, new Vector3(.9f, .8f, .7f) * s, rot);
        }

        /// An electricity pylon: a tall lattice tower with crossarms and insulators.
        void Pylon(LandContext ctx, MeshKit mk, Vector3 g, Vector3 along, float h = 22f)
        {
            var up = Vector3.up; var side = Vector3.Cross(up, along);
            foreach (var (a, b) in new[] { (-1f, -1f), (1f, -1f), (1f, 1f), (-1f, 1f) }) { var foot = g + side * a * 2.4f + along * b * 2.4f; var top = g + side * a * .5f + along * b * .5f + up * h; mk.Prism(iron, foot - up * .3f, (top - foot).normalized, .1f, (top - foot).magnitude + .3f, 4); ArtistBits.PostCollider(ctx.Root, foot, 3f, .2f); }
            for (var k = 1; k < 7; k++) { var y = k * h / 7f; var w = Mathf.Lerp(2.4f, .5f, y / h); foreach (var e in new[] { -1f, 1f }) { mk.Box(iron, g + up * y + along * e * w, new Vector3(.06f, .06f, w * 2f), Quaternion.LookRotation(side)); mk.Box(iron, g + up * y + side * e * w, new Vector3(.06f, .06f, w * 2f), Quaternion.LookRotation(along)); } }
            foreach (var y in new[] { h * .72f, h * .9f }) { mk.Box(iron, g + up * y, new Vector3(.12f, .12f, 9f), Quaternion.LookRotation(side)); foreach (var e in new[] { -4.2f, 4.2f }) mk.Prism(white, g + up * y + side * e, -up, .08f, .8f, 6); }
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
                    var dd = Mathf.Min(d, end); var q = from + dir * dd + side * Mathf.Sin(dd / 14f) * Mathf.Min(1.8f, end * .04f);
                    if (!ctx.IsLand(q.x, q.z, 0f)) break;
                    pts.Add(ctx.OnGround(q.x, q.z)); if (dd >= end) break;
                }
                if (pts.Count < 2) return;
                Strip(mk, pts, dirtM, PathW, .1f, ctx);
                foreach (var p in pts) Remember(p, walk);
                for (var i = 8; i < pts.Count; i += 14, n++)
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

        // ---------------------------------------------------------------- the rest of the fields
        /// Lone trees, hay bales, wooden fences, old TVs in the grass, sheep, telegraph poles, benches under lamps.
        void Wilds(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            int trees = 0, bales = 0, fences = 0, tvs = 0, sheep = 0, poles = 0, benches = 0; var rng = new System.Random(1998);
            const float cell = 22f;
            for (var x = ctx.MinX; x < ctx.MaxX; x += cell)
            for (var z = ctx.MinZ; z < ctx.MaxZ; z += cell)
            {
                float px = x + (float)rng.NextDouble() * cell, pz = z + (float)rng.NextDouble() * cell; var roll = (float)rng.NextDouble(); var s = (float)rng.NextDouble();
                if (!Open(ctx, px, pz, 3f)) continue;
                var p = ctx.OnGround(px, pz); var f = FaceTo(p, NearestWalk(p)); var rot = Quaternion.LookRotation(f); var side = Vector3.Cross(Vector3.up, f); var up = Vector3.up;
                if (roll < .2f) { var h = 3f + s * 2f; mk.Prism(trunkM, p, (up + side * (s - .5f) * .2f).normalized, .26f, h, 6, .6f); for (var k = 0; k < 4; k++) mk.Blob(leafM, p + up * (h + .4f) + Quaternion.Euler(0, k * 90f + s * 40f, 0) * f * 1.4f + up * (k % 2) * .6f, new Vector3(1.8f, 1.2f, 1.8f), rng, .3f); ArtistBits.PostCollider(ctx.Root, p, h, .35f); Mark(p, 2.4f); trees++; }
                else if (roll < .32f) { for (var k = 0; k < 2 + (int)(s * 3f); k++) { var bp = p + side * (k * 1.8f) + f * (k % 2) * .6f; mk.Prism(hay, bp + up * .7f - side * .7f, side, .7f, 1.4f, 12); mk.Prism(Paint(new Color(.7f, .58f, .36f)), bp + up * .7f - side * .72f, side, .5f, .02f, 12); BoxCol(ctx.Root, bp + up * .7f, new Vector3(1.4f, 1.4f, 1.4f), Quaternion.LookRotation(f)); } Mark(p, 3f); bales++; }
                else if (roll < .42f) { var len = 8f + s * 8f; var n = (int)(len / 2.4f); for (var k = 0; k <= n; k++) { var fp = ctx.OnGround(p.x + side.x * (k * 2.4f - len / 2f), p.z + side.z * (k * 2.4f - len / 2f)); mk.Box(wood, fp + up * .6f, new Vector3(.12f, 1.2f, .12f), rot * Quaternion.Euler(0, 0, (k % 3 - 1) * 4f)); if (k < n) { var np = ctx.OnGround(p.x + side.x * ((k + 1) * 2.4f - len / 2f), p.z + side.z * ((k + 1) * 2.4f - len / 2f)); foreach (var hh in new[] { .5f, .95f }) mk.Box(wood, (fp + np) / 2f + up * hh, new Vector3(.05f, .1f, (np - fp).magnitude), Quaternion.LookRotation(np - fp)); BoxCol(ctx.Root, (fp + np) / 2f + up * .6f, new Vector3(.2f, 1.2f, (np - fp).magnitude), Quaternion.LookRotation(np - fp)); } } fences++; }
                else if (roll < .5f) { OldTV(ctx.Root, mk, p, f, .9f + s * .4f); if (s > .5f) OldTV(ctx.Root, mk, p + side * 1.2f + up * 0f, Quaternion.Euler(0, 20f, 0) * f, .8f); Mark(p, 1.6f); tvs++; }
                else if (roll < .6f) { for (var k = 0; k < 2 + (int)(s * 3f); k++) { var sm = new MeshKit(); sm.Blob(wool, new Vector3(0, .55f, 0), new Vector3(.4f, .32f, .56f), rng, .25f); sm.Box(dark, new Vector3(0, .62f, .56f), new Vector3(.2f, .24f, .26f), Quaternion.identity); foreach (var (lx, lz) in new[] { (-.2f, .3f), (.2f, .3f), (-.2f, -.3f), (.2f, -.3f) }) sm.Box(dark, new Vector3(lx, .15f, lz), new Vector3(.08f, .3f, .08f), Quaternion.identity); var sg = sm.BuildSingle("Sheep", ctx.Root, false); var home = ctx.OnGround(p.x + side.x * k * 2f, p.z + side.z * k * 2f + f.z * (k % 2)); sg.transform.position = home; life.sheep.Add((sg.transform, home)); } sheep++; }
                else if (roll < .68f) { mk.Prism(wood, p, up, .14f, 7f, 6); mk.Box(wood, p + up * 6.4f, new Vector3(2f, .14f, .14f), Quaternion.LookRotation(f)); foreach (var e in new[] { -.8f, .8f }) mk.Prism(white, p + up * 6.5f + side * e, up, .05f, .2f, 5); ArtistBits.PostCollider(ctx.Root, p, 7f, .18f); poles++; }
                else if (roll < .74f) { Bench(ctx.Root, mk, p, f); Lamp(ctx, mk, p - f * .8f + side * 1.6f, benches % 2 == 0); Mark(p, 2.2f); benches++; }
            }
            Debug.Log($"The Ambient Mists: Pylon Walk {streetL:0} m, {trees} lone trees, {bales} rows of hay bales, {fences} fences, {tvs} old TVs, {sheep} flocks, {poles} telegraph poles, {benches} benches");
        }

        Vector3 NearestWalk(Vector3 p) { var best = center; var bd = float.MaxValue; foreach (var (q, _) in walkPoints) { var d = (q - p).sqrMagnitude; if (d < bd) { bd = d; best = q; } } return best; }

        /// Tall meadow grass, pale and dewy at the tips.
        void Verge(LandContext ctx)
        {
            var mat = WindMesh.Material(.3f, .9f, new Color(.5f, .56f, .42f));
            var meshes = new Dictionary<(int, int), WindMesh>(); var rng = new System.Random(1999);
            const float st = 2f;
            for (var x = ctx.MinX; x < ctx.MaxX; x += st)
            for (var z = ctx.MinZ; z < ctx.MaxZ; z += st)
            {
                var px = x + ((float)rng.NextDouble() - .5f) * st; var pz = z + ((float)rng.NextDouble() - .5f) * st;
                if (!ctx.IsLand(px, pz, 1f) || NearPath(px, pz, 3f) || InNoGrass(px, pz, 0f) || InFootprint(ctx, px, pz, -2f)) continue;
                if (Mathf.PerlinNoise(px * .03f + 2f, pz * .03f + 5f) < .3f) continue;
                var key = (Mathf.FloorToInt(px / 70f), Mathf.FloorToInt(pz / 70f));
                if (!meshes.TryGetValue(key, out var wm)) meshes[key] = wm = new WindMesh();
                var col = Color.Lerp(new Color(.48f, .56f, .4f), new Color(.66f, .64f, .46f), (float)rng.NextDouble());
                wm.Tuft(new Vector3(px, ctx.Ground(px, pz) - .04f, pz), .5f + (float)rng.NextDouble() * .6f, col, Color.Lerp(col, Haze, .5f), rng, 4, .08f);
            }
            foreach (var kv in meshes) kv.Value.Build("Mist grass", ctx.Root, mat);
        }
    }
}
