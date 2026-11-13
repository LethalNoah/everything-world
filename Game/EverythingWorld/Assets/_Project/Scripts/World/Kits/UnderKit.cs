using System.Collections.Generic;
using UnityEngine;
using static EverythingWorld.World.PlaceTools;

namespace EverythingWorld.World
{
    /// The Trip-Hop Undercity (WORLD_GUIDE.md): a city underground, always night and close fog. You arrive on the Platform,
    /// an old subway station under a vaulted concrete ceiling: white tiles with a blue stripe, the station's name in enamel,
    /// columns lit cold blue and warning red, a train standing at the platform with its windows lit. The Flooded Tunnel runs
    /// on along a walkway beside a channel of black water under arched ribs and hanging lamps, to the Control Room: a
    /// spy-film room of consoles and screens, a world map wall glowing, and a red button. Out in the dark: concrete pillars,
    /// stairwells, steam vents, puddles, signal lights, old carriages. Cold blue and red light on concrete.
    /// Laid out on purpose (the build-land standard): UnderTown.cs has the places and the people.
    public partial class UnderKit : LandKit
    {
        public override string Land => "The Trip-Hop Undercity";
        public override Vector3 ArtistFront(IslandWorldData data, IslandWorldData.Artist a) => facing.TryGetValue(a.name, out var f) ? f : base.ArtistFront(data, a);

        public static readonly Color Cold = new Color(.4f, .6f, 1f), Red = new Color(1f, .16f, .14f), Tile = new Color(.6f, .62f, .6f), Stripe = new Color(.16f, .3f, .6f), Ink = new Color(.06f, .06f, .08f), Amber = new Color(1f, .7f, .36f), Green = new Color(.3f, 1f, .5f);
        const float PathW = 3f, WalkW = 4f;

        Material concrete, concreteD, tileM, stripeM, iron, dark, white, waterM, lampM, redM, coldM, screenM, leafM;
        GlowHalos halos; Vector3 center, D, S; UnderLife life;
        readonly Dictionary<(int, int), List<Vector3>> pathCells = new Dictionary<(int, int), List<Vector3>>();
        readonly List<(Vector3 p, int w)> walkPoints = new List<(Vector3, int)>();
        readonly Dictionary<string, Vector3> facing = new Dictionary<string, Vector3>();
        // x, radius, z
        readonly List<Vector3> noGrass = new List<Vector3>();
        Vector2 heart2, D2 = new Vector2(0, -1), S2 = new Vector2(1, 0); float plazaY, waterY, streetL;

        void MakeMaterials()
        {
            concrete = WorldMaterials.Glow(new Color(.34f, .34f, .36f), .08f); concreteD = WorldMaterials.Glow(new Color(.2f, .2f, .22f), .06f); tileM = WorldMaterials.Glow(Tile, .14f); stripeM = WorldMaterials.Glow(Stripe, .3f); iron = WorldMaterials.Solid(new Color(.2f, .2f, .22f), .6f); dark = WorldMaterials.Solid(new Color(.04f, .04f, .05f)); white = WorldMaterials.Glow(new Color(.86f, .86f, .88f), .16f); leafM = WorldMaterials.Glow(new Color(.2f, .32f, .26f), .1f);
            waterM = new Material(WorldMaterials.Glow(new Color(.1f, .16f, .26f), .7f)) { name = "Under water" }; lampM = new Material(WorldMaterials.Glow(Amber, 1.6f)) { name = "Under lamps" }; redM = new Material(WorldMaterials.Glow(Red, 1.8f)) { name = "Under red" }; coldM = new Material(WorldMaterials.Glow(Cold, 1.6f)) { name = "Under cold" }; screenM = new Material(WorldMaterials.Glow(new Color(.4f, .8f, 1f), 1.2f)) { name = "Under screens" };
        }

        Material GlowM(Color c, float s = 2f) => WorldMaterials.Glow(c, s * .65f);
        Material Paint(Color c) => WorldMaterials.Glow(c, .14f);
        static bool HasStation(IslandWorldData.Artist a) => a.stationId != 0;
        static float Foot(IslandWorldData.Artist a) => a.name == "Portishead" ? 13f : a.name == "Massive Attack" ? 10f : 8f;

        public override bool Ground(float height, float slope, float patch, out Color color)
        {
            // wet concrete and rubble, with dark patches of moss
            if (slope > 22f) color = Color.Lerp(new Color(.24f, .24f, .26f), new Color(.3f, .3f, .32f), patch);
            else color = patch < .7f ? Color.Lerp(new Color(.22f, .22f, .24f), new Color(.28f, .28f, .3f), patch / .7f) : Color.Lerp(new Color(.18f, .24f, .2f), new Color(.22f, .28f, .22f), (patch - .7f) / .3f);
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
            for (var k = 0; k < 8; k++) { var dir = new Vector2(Mathf.Sin(k * Mathf.PI / 4f), Mathf.Cos(k * Mathf.PI / 4f)); var len = 40f; while (len < 90f && Inside(heart2 + dir * (len + 18f), 24f) && Inside(heart2 - dir * 30f, 20f)) len += 10f; if (len > streetL) { streetL = len; D2 = dir; } }
            S2 = new Vector2(-D2.y, D2.x); plazaY = Mathf.Max(data.HeightAt(heart2.x, heart2.y), waterY + 6f);
            var keep = new List<string>();
            void Put(string name, float d, float s, float room) { var p = P2(d, s); if (!Inside(p, room)) return; foreach (var a in data.layout.artists) if (a.sceneId == isl.sceneId && a.name == name) { a.x = p.x; a.z = p.y; keep.Add(name); } }
            Put("Portishead", 6f, -44f, 20f); Put("Massive Attack", 6f, 42f, 18f);
            var room = P2(streetL + 14f, 0);
            var avoid = new List<Vector3>(); for (var d = -30f; d <= streetL; d += 18f) avoid.Add(new Vector3(heart2.x + D2.x * d, 18f, heart2.y + D2.y * d)); avoid.Add(new Vector3(room.x, 16f, room.y));
            ArtistPlacement.Spread(data, index, 42f, heart2, 38f, 32f, avoid, keep, Land, 300f);
            Level(data, index, heart2 - D2 * 6f, 26f, 38f, plazaY);
            for (var d = 18f; d <= streetL + 14f; d += 6f) { var p = P2(d, 0); Level(data, index, p, 7f, 13f, plazaY); }
            Level(data, index, room, 12f, 20f, plazaY);
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
            life = new GameObject("Under life").AddComponent<UnderLife>();
            life.transform.SetParent(ctx.Root, false);
            life.center = center; life.lamps = lampM; life.reds = redM; life.colds = coldM; life.screens = screenM; life.water = waterM;
            var mk = new MeshKit(); var solid = new MeshKit();
            Remember(ctx.OnGround(center.x, center.z), 0);
            TownStart(ctx);
            Platform(ctx, mk, solid);
            Tunnel(ctx, mk, solid);
            ControlRoom(ctx, mk, solid);
            Paths(ctx, mk, solid);
            Wilds(ctx, mk, solid);
            mk.Build("The Trip-Hop Undercity", ctx.Root, false);
            solid.BuildCollider("The Trip-Hop Undercity walls", ctx.Root);
        }

        public override void Finish(LandContext ctx) { Verge(ctx); halos.Build("Under glow", ctx.Root); }

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

        /// An old station bench of wooden slats on iron legs.
        void Bench(Transform parent, MeshKit mk, Vector3 g, Vector3 face, float len = 2f)
        {
            var up = Vector3.up; var rot = Quaternion.LookRotation(face); var side = Vector3.Cross(up, face); for (var k = 0; k < 3; k++) mk.Box(Paint(new Color(.4f, .28f, .2f)), g + up * .46f + face * (-.16f + k * .16f), new Vector3(len, .05f, .13f), rot); mk.Box(Paint(new Color(.4f, .28f, .2f)), g + up * .8f - face * .26f, new Vector3(len, .2f, .05f), rot); foreach (var e in new[] { -1f, 1f }) mk.Box(iron, g + up * .3f + side * e * (len / 2f - .15f), new Vector3(.06f, .6f, .5f), rot);
            BoxCol(parent, g + up * .45f, new Vector3(len, .9f, .5f), rot); Interact.Bench(tlife.transform, g + up * .52f - side * (len / 2f - .5f), g + up * .52f + side * (len / 2f - .5f), face);
        }

        /// A post with a caged bulb, cold or warm.
        void Lamp(LandContext ctx, MeshKit mk, Vector3 g, bool light = false)
        {
            var up = Vector3.up; var warm = ((int)(g.x + g.z)) % 3 != 0; mk.Prism(iron, g - up * .2f, up, .06f, 3f, 6); mk.Blob(warm ? lampM : coldM, g + up * 3f, Vector3.one * .16f, new System.Random(1), 0f); for (var k = 0; k < 4; k++) mk.Box(iron, g + up * 3f + Quaternion.Euler(0, k * 90f, 0) * Vector3.forward * .18f, new Vector3(.02f, .4f, .02f), Quaternion.identity); halos.Add(g + up * 3f, 1.4f, warm ? Amber : Cold, .45f); ArtistBits.PostCollider(ctx.Root, g, 3f, .09f); if (light) ArtistBits.PointLight(ctx.Root, g + up * 2.8f, warm ? Amber : Cold, 12f, 1.5f);
        }

        /// A square concrete column with a light strip down one face.
        void Column(LandContext ctx, MeshKit mk, Vector3 g, float h, bool red)
        {
            var up = Vector3.up; mk.Box(concrete, g + up * h / 2f, new Vector3(.9f, h, .9f), Quaternion.LookRotation(D)); mk.Box(red ? redM : coldM, g + up * h / 2f + D * .46f, new Vector3(.08f, h * .7f, .02f), Quaternion.LookRotation(D)); ArtistBits.PostCollider(ctx.Root, g, h, .5f);
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
                    var dd = Mathf.Min(d, end); var q = from + dir * dd;
                    if (!ctx.IsLand(q.x, q.z, 0f)) break;
                    pts.Add(ctx.OnGround(q.x, q.z)); if (dd >= end) break;
                }
                if (pts.Count < 2) return;
                Strip(mk, pts, concrete, PathW, .1f, ctx); var e1 = new List<Vector3>(); foreach (var p in pts) e1.Add(p + side * (PathW / 2f - .2f)); Strip(mk, e1, Paint(new Color(.8f, .7f, .2f)), .1f, .12f, ctx);
                foreach (var p in pts) Remember(p, walk);
                for (var i = 6; i < pts.Count; i += 9, n++)
                {
                    var s = n % 2 == 0 ? 1f : -1f; var lp = pts[i] + side * s * (PathW / 2f + .8f);
                    if (InFootprint(ctx, lp.x, lp.z, 1f) || InNoGrass(lp.x, lp.z, 1f)) continue;
                    Lamp(ctx, mk, ctx.OnGround(lp.x, lp.z), n % 2 == 0);
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

        // ---------------------------------------------------------------- the rest of the undercity
        /// Concrete pillars holding up broken slabs of ceiling, stairwells going down into light, steam vents, puddles, signal lights, abandoned carriages, graffiti walls, benches under strip lights.
        void Wilds(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            int pillars = 0, stairs = 0, vents = 0, puddles = 0, signals = 0, cars = 0, walls = 0, benches = 0; var rng = new System.Random(1994);
            const float cell = 20f;
            for (var x = ctx.MinX; x < ctx.MaxX; x += cell)
            for (var z = ctx.MinZ; z < ctx.MaxZ; z += cell)
            {
                float px = x + (float)rng.NextDouble() * cell, pz = z + (float)rng.NextDouble() * cell; var roll = (float)rng.NextDouble(); var s = (float)rng.NextDouble();
                if (!Open(ctx, px, pz, 3f)) continue;
                var p = ctx.OnGround(px, pz); var f = FaceTo(p, NearestWalk(p)); var rot = Quaternion.LookRotation(f); var side = Vector3.Cross(Vector3.up, f); var up = Vector3.up;
                if (roll < .2f) { var h = 7f + s * 5f; foreach (var e in new[] { -1f, 1f }) { var cp = p + side * e * 3f; mk.Box(concrete, cp + up * h / 2f, new Vector3(1f, h, 1f), rot); ArtistBits.PostCollider(ctx.Root, cp, h, .6f); if (s > .4f) mk.Box(e > 0 ? redM : coldM, cp + up * 2.4f + f * .51f, new Vector3(.6f, .06f, .02f), rot); } mk.Box(concreteD, p + up * (h + .4f), new Vector3(8f, .8f, 3f), rot * Quaternion.Euler(0, 0, (s - .5f) * 8f)); Mark(p, 3.6f); pillars++; }
                else if (roll < .3f) { if (!Open(ctx, px, pz, 4f)) continue; mk.Box(concreteD, p + up * .5f, new Vector3(3.4f, 1f, 4f), rot); solid.Box(concreteD, p + up * .5f, new Vector3(3.4f, 1f, 4f), rot); mk.Box(dark, p + up * 1.01f, new Vector3(2.4f, .02f, 3.2f), rot); for (var k = 0; k < 6; k++) mk.Box(tileM, p + up * (.9f - k * .14f) + f * (1.2f - k * .4f), new Vector3(2.2f, .04f, .36f), rot); mk.Box(GlowM(Amber, 1f), p + up * .3f - f * 1.2f, new Vector3(2f, .02f, .8f), rot); foreach (var e in new[] { -1.2f, 1.2f }) { mk.Box(iron, p + up * 1.5f + side * e, new Vector3(.06f, 1f, 4f), rot); BoxCol(ctx.Root, p + up * 1.5f + side * e, new Vector3(.2f, 1f, 4f), rot); } mk.Box(stripeM, p + up * 2.4f + f * 2f, new Vector3(2.4f, .5f, .1f), rot); halos.Add(p + up * 1.2f, 2f, Amber, .3f); Mark(p, 2.8f); stairs++; }
                else if (roll < .4f) { mk.Box(iron, p + up * .1f, new Vector3(1.2f, .2f, 1.2f), rot); for (var k = 0; k < 5; k++) mk.Box(dark, p + up * .21f + side * (k - 2f) * .2f, new Vector3(.08f, .01f, 1f), rot); life.vents.Add(p + up * .3f); BoxCol(ctx.Root, p + up * .1f, new Vector3(1.2f, .2f, 1.2f), rot); vents++; }
                else if (roll < .5f) { mk.Prism(waterM, p + up * .03f, up, 1f + s * 1.6f, .01f, 14, 1f, s * 30f); puddles++; }
                else if (roll < .58f) { mk.Prism(iron, p, up, .06f, 3.2f, 6); mk.Box(dark, p + up * 3f, new Vector3(.4f, .9f, .3f), rot); mk.Blob(redM, p + up * 3.3f + f * .16f, Vector3.one * .1f, rng, 0f); mk.Blob(GlowM(Green, 1.6f), p + up * 2.8f + f * .16f, Vector3.one * .1f, rng, 0f); life.signals.Add(p + up * 3.05f); ArtistBits.PostCollider(ctx.Root, p, 3.2f, .1f); signals++; }
                else if (roll < .64f) { if (!Open(ctx, px, pz, 6f)) continue; var cr = Quaternion.LookRotation(side); mk.Box(Paint(new Color(.5f, .52f, .56f)), p + up * 1.6f, new Vector3(2.6f, 2.6f, 9f), cr); solid.Box(concrete, p + up * 1.6f, new Vector3(2.6f, 2.6f, 9f), cr); mk.Box(Paint(new Color(.6f, .14f, .14f)), p + up * .8f, new Vector3(2.62f, .4f, 9.02f), cr); for (var k = 0; k < 6; k++) mk.Box(k % 3 == 0 ? coldM : windowM(), p + up * 2f + side * (-3.5f + k * 1.4f) + f * 1.31f, new Vector3(1f, .7f, .02f), rot); for (var k = 0; k < 6; k++) { var gp = p + up * (.9f + (k % 2) * .5f) + side * (-3.6f + k * 1.5f) + f * 1.32f; mk.Box(GlowM(new[] { Red, Cold, Amber, Green }[k % 4], .7f), gp, new Vector3(.8f, .2f, .01f), rot * Quaternion.Euler(0, 0, (k % 3 - 1) * 10f)); } Mark(p, 5f); cars++; }
                else if (roll < .72f) { var wl = new PlaceFrame(p, f); mk.Box(concrete, wl.P(0, 1.6f, -.2f), new Vector3(6f, 3.2f, .4f), wl.r); solid.Box(concrete, wl.P(0, 1.6f, -.2f), new Vector3(6f, 3.2f, .4f), wl.r); for (var k = 0; k < 8; k++) mk.Box(GlowM(new[] { Red, Cold, Amber, Green, new Color(.9f, .4f, 1f) }[(k + (int)(s * 9f)) % 5], .6f + (k % 2) * .4f), wl.P(-2.4f + (k * 7 % 8) * .66f, .6f + (k * 5 % 6) * .4f, .01f), new Vector3(.5f + (k % 3) * .3f, .2f + (k % 2) * .3f, .01f), wl.r * Quaternion.Euler(0, 0, (k * 37) % 50 - 25f)); walls++; }
                else if (roll < .78f) { Bench(ctx.Root, mk, p, f); var sl = p - f * .6f + up * 3f; mk.Prism(iron, p - f * .6f - side * 1.2f, up, .05f, 3f, 5); mk.Box(coldM, sl, new Vector3(2.4f, .06f, .1f), rot); halos.Add(sl, 2f, Cold, .3f); ArtistBits.PostCollider(ctx.Root, p - f * .6f - side * 1.2f, 3f, .08f); if (benches % 2 == 0) ArtistBits.PointLight(ctx.Root, sl - up * .3f, Cold, 10f, 1.2f); Mark(p, 2.2f); benches++; }
            }
            Debug.Log($"The Trip-Hop Undercity: the Flooded Tunnel {streetL:0} m, {pillars} pillar pairs, {stairs} stairwells, {vents} steam vents, {puddles} puddles, {signals} signal lights, {cars} old carriages, {walls} graffiti walls, {benches} benches");
        }

        Material windowM() => GlowM(Amber, 1f);
        Vector3 NearestWalk(Vector3 p) { var best = center; var bd = float.MaxValue; foreach (var (q, _) in walkPoints) { var d = (q - p).sqrMagnitude; if (d < bd) { bd = d; best = q; } } return best; }

        /// Weeds in the cracks, dark green going grey.
        void Verge(LandContext ctx)
        {
            var mat = WindMesh.Material(.15f, .8f, new Color(.2f, .26f, .22f));
            var meshes = new Dictionary<(int, int), WindMesh>(); var rng = new System.Random(1995);
            const float st = 3.4f;
            for (var x = ctx.MinX; x < ctx.MaxX; x += st)
            for (var z = ctx.MinZ; z < ctx.MaxZ; z += st)
            {
                var px = x + ((float)rng.NextDouble() - .5f) * st; var pz = z + ((float)rng.NextDouble() - .5f) * st;
                if (!ctx.IsLand(px, pz, 1f) || NearPath(px, pz, 3.6f) || InNoGrass(px, pz, 0f) || InFootprint(ctx, px, pz, -2f)) continue;
                if (Mathf.PerlinNoise(px * .05f + 2f, pz * .05f + 7f) < .55f) continue;
                var key = (Mathf.FloorToInt(px / 70f), Mathf.FloorToInt(pz / 70f));
                if (!meshes.TryGetValue(key, out var wm)) meshes[key] = wm = new WindMesh();
                var col = Color.Lerp(new Color(.18f, .24f, .2f), new Color(.26f, .3f, .26f), (float)rng.NextDouble());
                wm.Tuft(new Vector3(px, ctx.Ground(px, pz) - .04f, pz), .2f + (float)rng.NextDouble() * .3f, col, Color.Lerp(col, Cold, .3f), rng, 3, .08f);
            }
            foreach (var kv in meshes) kv.Value.Build("Under weeds", ctx.Root, mat);
        }
    }
}
