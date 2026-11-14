using System.Collections.Generic;
using UnityEngine;
using static EverythingWorld.World.PlaceTools;

namespace EverythingWorld.World
{
    /// The Electro Foundry (WORLD_GUIDE.md): a neon factory working all night. You arrive on the Assembly Floor, an open
    /// steel shed where robot arms swing to the beat over conveyor belts, welders throw sparks, warning beacons turn and a
    /// big lever runs the line. The Line, a walkway beside a long conveyor under a travelling gantry crane, runs to the
    /// Furnace, glowing orange under its chimney. Smokestacks with blinking lights, steel stacks, cable drums and hazard
    /// stripes all round. Steel grey, hazard yellow, neon green and electric blue, with furnace orange.
    /// Laid out on purpose (the build-land standard): FoundryTown.cs has the places and the people.
    public partial class FoundryKit : LandKit
    {
        public override string Land => "The Electro Foundry";
        public override Vector3 ArtistFront(IslandWorldData data, IslandWorldData.Artist a) => facing.TryGetValue(a.name, out var f) ? f : base.ArtistFront(data, a);

        public static readonly Color Green = new Color(.3f, 1f, .3f), Blue = new Color(.2f, .6f, 1f), Hazard = new Color(1f, .8f, .1f), Furnace = new Color(1f, .45f, .1f), Ink = new Color(.05f, .05f, .06f), Pink = new Color(1f, .3f, .7f), White = new Color(.92f, .96f, 1f);
        const float PathW = 3f, LineW = 5f;

        Material steel, steelD, plate, hazard, dark, white, greenM, blueM, furnaceM, beaconM, lampM, leafM;
        GlowHalos halos; Vector3 center, D, S; FoundryLife life;
        readonly Dictionary<(int, int), List<Vector3>> pathCells = new Dictionary<(int, int), List<Vector3>>();
        readonly List<(Vector3 p, int w)> walkPoints = new List<(Vector3, int)>();
        readonly Dictionary<string, Vector3> facing = new Dictionary<string, Vector3>();
        // x, radius, z
        readonly List<Vector3> noGrass = new List<Vector3>();
        Vector2 heart2, D2 = new Vector2(0, -1), S2 = new Vector2(1, 0); float plazaY, waterY, streetL;

        void MakeMaterials()
        {
            steel = WorldMaterials.Solid(new Color(.42f, .44f, .48f), .7f); steelD = WorldMaterials.Solid(new Color(.22f, .23f, .26f), .6f); plate = WorldMaterials.Glow(new Color(.26f, .27f, .3f), .08f); hazard = WorldMaterials.Glow(Hazard, .4f); dark = WorldMaterials.Solid(new Color(.05f, .05f, .06f)); white = WorldMaterials.Glow(new Color(.86f, .88f, .9f), .16f); leafM = WorldMaterials.Glow(new Color(.2f, .3f, .22f), .1f);
            greenM = new Material(WorldMaterials.Glow(Green, 1.6f)) { name = "Foundry green" }; blueM = new Material(WorldMaterials.Glow(Blue, 1.6f)) { name = "Foundry blue" }; furnaceM = new Material(WorldMaterials.Glow(Furnace, 2f)) { name = "Foundry furnace" }; beaconM = new Material(WorldMaterials.Glow(new Color(1f, .6f, .1f), 2f)) { name = "Foundry beacons" }; lampM = new Material(WorldMaterials.Glow(White, 1.4f)) { name = "Foundry lamps" };
        }

        Material GlowM(Color c, float s = 2f) => WorldMaterials.Glow(c, s * .65f);
        Material Paint(Color c) => WorldMaterials.Glow(c, .14f);
        static bool HasStation(IslandWorldData.Artist a) => a.stationId != 0;
        static float Foot(IslandWorldData.Artist a) => HasStation(a) ? 13f : 8f;

        public override bool Ground(float height, float slope, float patch, out Color color)
        {
            // cinder and oily earth, rusty on the slopes
            if (slope > 22f) color = Color.Lerp(new Color(.34f, .28f, .24f), new Color(.42f, .34f, .28f), patch);
            else color = Color.Lerp(new Color(.24f, .25f, .24f), new Color(.32f, .32f, .3f), patch);
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
            for (var k = 0; k < 8; k++) { var dir = new Vector2(Mathf.Sin(k * Mathf.PI / 4f), Mathf.Cos(k * Mathf.PI / 4f)); var len = 36f; while (len < 90f && Inside(heart2 + dir * (len + 18f), 22f)) len += 10f; if (len > streetL) { streetL = len; D2 = dir; } }
            S2 = new Vector2(-D2.y, D2.x); plazaY = Mathf.Max(data.HeightAt(heart2.x, heart2.y), waterY + 6f);
            var keep = new List<string>();
            void Put(string name, float d, float s, float room) { var p = P2(d, s); if (!Inside(p, room)) return; foreach (var a in data.layout.artists) if (a.sceneId == isl.sceneId && a.name == name) { a.x = p.x; a.z = p.y; keep.Add(name); } }
            Put("Farleon", 4f, -46f, 20f); Put("Insan3lik3", 4f, 44f, 20f); Put("meganeko", streetL + 10f, -34f, 20f);
            var furnace = P2(streetL + 14f, 0);
            var avoid = new List<Vector3>(); for (var d = -16f; d <= streetL; d += 18f) avoid.Add(new Vector3(heart2.x + D2.x * d, 18f, heart2.y + D2.y * d)); avoid.Add(new Vector3(furnace.x, 16f, furnace.y));
            ArtistPlacement.Spread(data, index, 44f, heart2, 38f, 34f, avoid, keep, Land, 300f);
            Level(data, index, heart2 - D2 * 4f, 22f, 34f, plazaY);
            for (var d = 18f; d <= streetL + 14f; d += 6f) { var p = P2(d, 0); Level(data, index, p, 6f, 12f, Mathf.Lerp(plazaY, Mathf.Max(data.HeightAt(p.x, p.y), waterY + 3f), Mathf.Clamp01((d - 18f) / 30f))); }
            foreach (var a in data.layout.artists)
            {
                if (a.sceneId != isl.sceneId) continue;
                a.r = Mathf.Max(a.r, Foot(a) + 6f);
                Level(data, index, new Vector2(a.x, a.z), Foot(a) + 1f, Foot(a) + 9f, Mathf.Max(data.HeightAt(a.x, a.z), waterY + 2.6f));
            }
            PlanLanes(data, isl);
            foreach (var l in plan) SmoothLane(data, index, isl, l);
        }

        // ---------------------------------------------------------------- planning the lanes (here, so the ground under them can be smoothed)
        class Lane { public string name; public Vector2 from; public float stop; public List<Vector2> pts = new List<Vector2>(); }
        readonly List<Lane> plan = new List<Lane>();

        /// Where a lane may not run: across another artist's plate, the furnace or the Assembly Floor.
        bool Crosses(IslandWorldData data, IslandWorldData.Island isl, Vector2 from, IslandWorldData.Artist to)
        {
            var t = new Vector2(to.x, to.z); var dir = t - from; var total = dir.magnitude; if (total < 2f) return false; dir /= total;
            var furnace = P2(streetL + 16f, 0);
            for (var d = PathW / 2f + 1f; d < total - Foot(to) * .8f; d += 1.5f)
            {
                var q = from + dir * d;
                foreach (var b in data.layout.artists) { if (b.sceneId != isl.sceneId || b == to) continue; if (Vector2.Distance(q, new Vector2(b.x, b.z)) < Foot(b) * .8f) return true; }
                if (Vector2.Distance(q, furnace) < 7f) return true;
                var o = q - heart2; float dd = Vector2.Dot(o, D2), ss = Vector2.Dot(o, S2); if (dd > -22f && dd < 9f && Mathf.Abs(ss) < 15f) return true;
            }
            return false;
        }

        /// The lanes, nearest artist first, each from the nearest walk already laid; a lane never runs across another artist's plate.
        void PlanLanes(IslandWorldData data, IslandWorldData.Island isl)
        {
            plan.Clear();
            var seeds = new List<Vector2> { heart2 };
            for (var k = -3; k <= 3; k++) { seeds.Add(P2(9f, k * 3.6f)); seeds.Add(P2(-6f - k * 3f, 15.4f)); seeds.Add(P2(-6f - k * 3f, -15.4f)); }
            for (var d = 9.5f; d <= streetL + 6.01f; d += 2f) seeds.Add(P2(d, -2.5f));
            for (var k = 0; k < 6; k++) seeds.Add(P2(streetL + 7f, -6f + k * 2.4f));
            var todo = new List<IslandWorldData.Artist>(); foreach (var a in data.layout.artists) if (a.sceneId == isl.sceneId) todo.Add(a);
            while (todo.Count > 0)
            {
                IslandWorldData.Artist next = null; Vector2 join = default; var best = float.MaxValue;
                foreach (var pass in new[] { true, false })
                {
                    foreach (var a in todo)
                        foreach (var p in seeds) { var d = Vector2.Distance(p, new Vector2(a.x, a.z)) - Foot(a); if (d < best && (!pass || !Crosses(data, isl, p, a))) { best = d; next = a; join = p; } }
                    if (next != null) break;
                }
                if (next == null) break;
                todo.Remove(next);
                var l = new Lane { name = next.name, from = join, stop = Foot(next) * .8f }; plan.Add(l);
                var to = new Vector2(next.x, next.z); var dir = to - join; var total = dir.magnitude; if (total < 2f) continue; dir /= total; var end = total - l.stop; if (end < PathW) continue;
                for (var d = PathW / 2f + .1f; ; d += .75f) { var dd = Mathf.Min(d, end); var q = join + dir * dd; if (data.IslandAt(q.x, q.y) != isl) break; l.pts.Add(q); if (dd >= end) break; }
                seeds.AddRange(l.pts);
            }
        }

        /// Smooth the ground along a lane, so it climbs a bank or a hill gently instead of running straight up it.
        /// The plates, the Assembly Floor, the Line and the furnace keep their own levelled ground.
        void SmoothLane(IslandWorldData data, int index, IslandWorldData.Island isl, Lane l)
        {
            var n = l.pts.Count; if (n < 3) return;
            var h = new float[n]; for (var i = 0; i < n; i++) h[i] = data.HeightAt(l.pts[i].x, l.pts[i].y);
            // ends pinned (they meet a plate, the Line or another lane), the climb in between spread out evenly over about 20 m
            var sm = (float[])h.Clone(); var nx = new float[n];
            for (var it = 0; it < 400; it++) { nx[0] = sm[0]; nx[n - 1] = sm[n - 1]; for (var i = 1; i < n - 1; i++) nx[i] = (sm[i - 1] + sm[i] * 2f + sm[i + 1]) / 4f; (sm, nx) = (nx, sm); }
            float minX = float.MaxValue, maxX = float.MinValue, minZ = float.MaxValue, maxZ = float.MinValue; foreach (var p in l.pts) { minX = Mathf.Min(minX, p.x); maxX = Mathf.Max(maxX, p.x); minZ = Mathf.Min(minZ, p.y); maxZ = Mathf.Max(maxZ, p.y); }
            const float flat = 2.6f, blend = 8f; var furnace = P2(streetL + 16f, 0);
            for (var r = 0; r < data.Nz; r++)
            {
                var z = data.layout.depth - r * data.Step; if (z < minZ - blend || z > maxZ + blend) continue;
                for (var col = 0; col < data.Nx; col++)
                {
                    var x = col * data.Step; if (x < minX - blend || x > maxX + blend) continue;
                    var i = r * data.Nx + col; if (data.islandIds[i] != index) continue;
                    var c = new Vector2(x, z);
                    // nearest point on the lane, and the smoothed height there
                    var bd = float.MaxValue; var target = 0f;
                    for (var k = 0; k + 1 < n; k++)
                    {
                        var ab = l.pts[k + 1] - l.pts[k]; var t = Mathf.Clamp01(Vector2.Dot(c - l.pts[k], ab) / Mathf.Max(ab.sqrMagnitude, 1e-4f));
                        var dist = Vector2.Distance(c, l.pts[k] + ab * t); if (dist < bd) { bd = dist; target = Mathf.Lerp(sm[k], sm[k + 1], t); }
                    }
                    if (bd >= blend) continue;
                    var keep = false;
                    foreach (var b in data.layout.artists) if (b.sceneId == isl.sceneId && Vector2.Distance(c, new Vector2(b.x, b.z)) < Foot(b) + 1f) { keep = true; break; }
                    var o = c - heart2; float dd = Vector2.Dot(o, D2), ss = Vector2.Dot(o, S2);
                    if ((dd > -24f && dd < 11f && Mathf.Abs(ss) < 18f) || (dd > 8f && dd < streetL + 22f && Mathf.Abs(ss) < 7f) || Vector2.Distance(c, furnace) < 11f) keep = true;
                    if (keep) continue;
                    var orig = data.heights[i]; var want = bd < flat ? target : Mathf.Lerp(target, orig, Ease(0f, 1f, (bd - flat) / (blend - flat)));
                    if (orig > waterY + .5f) want = Mathf.Max(want, waterY + .8f);
                    data.heights[i] = want;
                }
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
            life = new GameObject("Foundry life").AddComponent<FoundryLife>();
            life.transform.SetParent(ctx.Root, false);
            life.center = center; life.greens = greenM; life.blues = blueM; life.furnace = furnaceM; life.beacons = beaconM; life.lamps = lampM;
            var mk = new MeshKit(); var solid = new MeshKit();
            Remember(ctx.OnGround(center.x, center.z), 0);
            TownStart(ctx);
            Assembly(ctx, mk, solid);
            TheLine(ctx, mk, solid);
            Paths(ctx, mk, solid);
            LineBelt(ctx, mk);
            Wilds(ctx, mk, solid);
            mk.Build("The Electro Foundry", ctx.Root, false);
            solid.BuildCollider("The Electro Foundry walls", ctx.Root);
        }

        public override void Finish(LandContext ctx) { Verge(ctx); halos.Build("Foundry glow", ctx.Root); }

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

        /// A steel bench with a hazard stripe.
        void Bench(Transform parent, MeshKit mk, Vector3 g, Vector3 face, float len = 2f)
        {
            var up = Vector3.up; var rot = Quaternion.LookRotation(face); var side = Vector3.Cross(up, face); mk.Box(steel, g + up * .44f, new Vector3(len, .08f, .5f), rot); mk.Box(hazard, g + up * .49f + face * .24f, new Vector3(len, .02f, .04f), rot); foreach (var e in new[] { -1f, 1f }) mk.Box(steelD, g + up * .22f + side * e * (len / 2f - .2f), new Vector3(.1f, .44f, .44f), rot);
            BoxCol(parent, g + up * .25f, new Vector3(len, .5f, .5f), rot); Interact.Bench(tlife.transform, g + up * .5f - side * (len / 2f - .5f), g + up * .5f + side * (len / 2f - .5f), face);
        }

        /// A steel post with a caged work lamp.
        void Lamp(LandContext ctx, MeshKit mk, Vector3 g, bool light = false)
        {
            var up = Vector3.up; mk.Prism(steelD, g - up * .2f, up, .08f, 3.4f, 6); mk.Box(steelD, g + up * 3.1f + S * .4f, new Vector3(.06f, .06f, .9f), Quaternion.LookRotation(S)); mk.Box(lampM, g + up * 2.95f + S * .8f, new Vector3(.3f, .14f, .4f), Quaternion.LookRotation(S)); halos.Add(g + up * 2.9f + S * .8f, 1.4f, White, .4f); ArtistBits.PostCollider(ctx.Root, g, 3.2f, .1f); if (light) ArtistBits.PointLight(ctx.Root, g + up * 2.7f + S * .8f, new Color(.86f, .94f, 1f), 12f, 1.4f);
        }

        /// A robot arm on a turntable base: shoulder, elbow, a glowing tool tip. It swings on the beat (FoundryLife).
        void RobotArm(LandContext ctx, Transform parent, Vector3 g, float s, Material tip, int n)
        {
            var up = Vector3.up; var b = new MeshKit(); b.Prism(hazard, Vector3.zero, Vector3.up, .7f * s, .4f * s, 12); b.Prism(steelD, Vector3.up * .4f * s, Vector3.up, .5f * s, .5f * s, 10); var bg = b.BuildSingle("Robot base", parent, false); bg.transform.position = g;
            var arm = new MeshKit(); arm.Box(steel, Vector3.up * 1.2f * s, new Vector3(.4f, 2.4f, .4f) * s, Quaternion.identity); arm.Prism(steelD, Vector3.up * 2.4f * s - Vector3.right * .3f * s, Vector3.right, .3f * s, .6f * s, 10); var ag = arm.BuildSingle("Robot arm", parent, false); ag.transform.position = g + up * .9f * s;
            var fore = new MeshKit(); fore.Box(steel, Vector3.forward * 1f * s, new Vector3(.3f, .3f, 2f) * s, Quaternion.identity); fore.Prism(tip, Vector3.forward * 2f * s, Vector3.forward, .12f * s, .3f * s, 8, .3f); var fg = fore.BuildSingle("Robot forearm", ag.transform, false); fg.transform.localPosition = Vector3.up * 2.4f * s;
            life.arms.Add((ag.transform, fg.transform, n * .7f)); ArtistBits.PostCollider(ctx.Root, g, 2.6f * s, .7f * s); life.weldTips.Add(fg.transform);
        }

        /// A smokestack with a blinking red light.
        void Stack(LandContext ctx, MeshKit mk, Vector3 g, float h)
        {
            var up = Vector3.up; mk.Prism(steelD, g, up, 1.2f, h, 12, .7f); for (var k = 0; k < 3; k++) mk.Prism(k == 1 ? hazard : steel, g + up * (h * .3f + k * h * .2f), up, 1.2f - (h * .3f + k * h * .2f) / h * .36f + .05f, .4f, 12); mk.Blob(GlowM(new Color(1f, .1f, .1f), 2f), g + up * (h + .2f), Vector3.one * .2f, new System.Random(1), 0f); life.smoke.Add(g + up * (h + .2f)); ArtistBits.PostCollider(ctx.Root, g, h, 1.2f);
        }

        // ---------------------------------------------------------------- the lanes to the artists
        void Paths(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var walk = 10; var n = 0; var lamps = new List<(Vector3 p, bool light)>();
            void Lay(Vector3 join, Vector3 to, float stopShort, string artist)
            {
                var from = new Vector3(join.x, 0, join.z); to.y = 0;
                var dir = to - from; var total = dir.magnitude; if (total < 2f) return; dir /= total;
                if (artist != null) facing[artist] = -dir;
                var side = Vector3.Cross(Vector3.up, dir); var end = total - stopShort; if (end < PathW) return;
                var pts = new List<Vector3>();
                for (var d = PathW / 2f + .1f; ; d += .75f)   // in short pieces, so the lane follows the ground and nothing pokes through it
                {
                    var dd = Mathf.Min(d, end); var q = from + dir * dd;
                    if (!ctx.IsLand(q.x, q.z, 0f)) break;
                    pts.Add(ctx.OnGround(q.x, q.z)); if (dd >= end) break;
                }
                if (pts.Count < 2) return;
                var steep = 0f; for (var i = 1; i < pts.Count; i++) steep = Mathf.Max(steep, Mathf.Abs(pts[i].y - pts[i - 1].y) / Mathf.Max(.1f, Vector2.Distance(new Vector2(pts[i].x, pts[i].z), new Vector2(pts[i - 1].x, pts[i - 1].z))));
                if (steep > .4f) Debug.Log($"The Electro Foundry: the lane to {artist} climbs {Mathf.Atan(steep) * Mathf.Rad2Deg:0} degrees at its steepest");
                Strip(mk, pts, plate, PathW, .1f, ctx); foreach (var e in new[] { -1f, 1f }) { var ed = new List<Vector3>(); foreach (var p in pts) ed.Add(p + side * e * (PathW / 2f - .1f)); Strip(mk, ed, e > 0 ? greenM : blueM, .06f, .12f, ctx); }
                foreach (var p in pts) Remember(p, walk);
                for (var i = 12; i < pts.Count; i += 20, n++) { var s = n % 2 == 0 ? 1f : -1f; lamps.Add((pts[i] + side * s * (PathW / 2f + .8f), n % 3 == 0)); }
                walk++;
            }
            // the lanes planned in ShapeGround (the ground under them is smoothed), nearest artist first, none across another artist's plate
            foreach (var l in plan)
            {
                IslandWorldData.Artist next = null; foreach (var a in ctx.Artists) if (a.name == l.name) next = a;
                if (next == null) continue;
                Lay(new Vector3(l.from.x, 0, l.from.y), new Vector3(next.x, 0, next.z), l.stop, next.name);
            }
            // the lamps beside the lanes, once every lane is down, so none stands on another walk
            foreach (var (lp, light) in lamps)
            {
                if (InFootprint(ctx, lp.x, lp.z, 1f) || InNoGrass(lp.x, lp.z, 1f) || NearPath(lp.x, lp.z, PathW / 2f + .5f)) continue;
                Lamp(ctx, mk, ctx.OnGround(lp.x, lp.z), light);
            }
        }

        // ---------------------------------------------------------------- the rest of the works
        /// Smokestacks, lone robot arms, stacks of steel girders, cable drums, oil drums, pylons of neon tubes, benches under work lamps.
        void Wilds(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            int stacks = 0, arms = 0, girders = 0, drums = 0, oil = 0, tubes = 0, benches = 0, signs = 0, floods = 0, pipes = 0; var rng = new System.Random(2015);
            // the night shift's neon: magenta, cyan, yellow, orange, violet, green and blue (bright and dark, blue and yellow, so they tell apart)
            Color[] neon = { new Color(1f, .2f, .75f), new Color(.15f, 1f, 1f), new Color(1f, .9f, .15f), new Color(1f, .45f, .1f), new Color(.6f, .3f, 1f), Green, Blue };
            var neonM = new Material[neon.Length]; for (var k = 0; k < neon.Length; k++) neonM[k] = GlowM(neon[k], 2.2f);
            const float cell = 20f;
            for (var x = ctx.MinX; x < ctx.MaxX; x += cell)
            for (var z = ctx.MinZ; z < ctx.MaxZ; z += cell)
            {
                float px = x + (float)rng.NextDouble() * cell, pz = z + (float)rng.NextDouble() * cell; var roll = (float)rng.NextDouble(); var s = (float)rng.NextDouble();
                if (!Open(ctx, px, pz, 3f)) continue;
                var p = ctx.OnGround(px, pz); var f = FaceTo(p, NearestWalk(p)); var rot = Quaternion.LookRotation(f); var side = Vector3.Cross(Vector3.up, f); var up = Vector3.up;
                if (roll < .1f && stacks < 24) { if (!Open(ctx, px, pz, 4f)) continue; Stack(ctx, mk, p, 14f + s * 12f); Mark(p, 2f); stacks++; }
                else if (roll < .2f && arms < 30) { mk.Box(plate, p + up * .05f, new Vector3(4f, .1f, 4f), rot); RobotArm(ctx, ctx.Root, p, .8f + s * .4f, s > .5f ? greenM : blueM, arms); Mark(p, 2.4f); arms++; }
                else if (roll < .34f) { for (var k = 0; k < 4 + (int)(s * 4f); k++) { var gp = p + up * (.15f + (k / 3) * .3f) + f * ((k % 3) - 1f) * .5f; mk.Box(steel, gp, new Vector3(5f, .3f, .3f), Quaternion.LookRotation(side) * Quaternion.Euler(0, 90f, 0)); } BoxCol(ctx.Root, p + up * .5f, new Vector3(5f, 1f, 1.6f), Quaternion.LookRotation(f)); Mark(p, 2.6f); girders++; }
                else if (roll < .44f) { for (var k = 0; k < 2; k++) { var dp = p + side * (k * 1.6f - .8f); mk.Prism(Paint(new Color(.5f, .36f, .2f)), dp + up * .7f - f * .4f, f, .7f, .1f, 14); mk.Prism(Paint(new Color(.5f, .36f, .2f)), dp + up * .7f + f * .3f, f, .7f, .1f, 14); mk.Prism(k == 0 ? Paint(new Color(.1f, .3f, .1f)) : Paint(new Color(.1f, .2f, .4f)), dp + up * .7f - f * .3f, f, .5f, .6f, 14); } BoxCol(ctx.Root, p + up * .7f, new Vector3(3.2f, 1.4f, .9f), rot); drums++; }
                else if (roll < .54f) { for (var k = 0; k < 3 + (int)(s * 4f); k++) { var op = p + new Vector3((k % 3) - 1f, 0, (k / 3) - .5f) * .7f; var col = (k + (int)(s * 5f)) % 3 == 0 ? hazard : (k % 2 == 0 ? Paint(new Color(.2f, .4f, .7f)) : Paint(new Color(.6f, .14f, .1f))); mk.Prism(col, op, up, .3f, .9f, 12); mk.Prism(steelD, op + up * .9f, up, .3f, .02f, 12); } BoxCol(ctx.Root, p + up * .45f, new Vector3(2.4f, .9f, 1.8f), rot); oil++; }
                else if (roll < .62f) { var h = 4f + s * 3f; mk.Box(steelD, p + up * h / 2f, new Vector3(.3f, h, .3f), rot); var tc = (int)(s * 97f) % neon.Length; for (var k = 0; k < 3; k++) mk.Box(k % 2 == 0 ? neonM[tc] : neonM[(tc + 3) % neon.Length], p + up * (1f + k * h * .28f) + side * .2f, new Vector3(.08f, h * .22f, .08f), rot); halos.Add(p + up * h * .5f + side * .4f, 1.2f, neon[tc], .5f); ArtistBits.PostCollider(ctx.Root, p, h, .3f); tubes++; }
                else if (roll < .69f) { Bench(ctx.Root, mk, p, f); Lamp(ctx, mk, p - f * .8f + side * 1.6f, benches % 2 == 0); Mark(p, 2.2f); benches++; }
                else if (roll < .79f)
                {
                    // a neon sign on two posts: a glowing frame and stripes, facing the nearest walk, lighting the ground
                    var c1 = (int)(s * 7f) % neon.Length; var c2 = (c1 + 2) % neon.Length; var h = 3.4f + s * 2f; var w = 3f + s * 1.5f;
                    foreach (var e in new[] { -1f, 1f }) { mk.Prism(steelD, p + side * e * w * .45f, up, .09f, h + 1.2f, 6); ArtistBits.PostCollider(ctx.Root, p + side * e * w * .45f, h + 1.2f, .12f); }
                    var sc = p + up * (h + .4f); mk.Box(dark, sc, new Vector3(w, 1.6f, .2f), rot);
                    foreach (var e in new[] { -1f, 1f }) { mk.Box(neonM[c1], sc + up * e * .72f + f * .12f, new Vector3(w - .1f, .07f, .04f), rot); mk.Box(neonM[c1], sc + side * e * (w / 2f - .05f) + f * .12f, new Vector3(.07f, 1.5f, .04f), rot); }
                    for (var k = 0; k < 3; k++) mk.Box(neonM[k == 1 ? c2 : c1], sc + side * ((k - 1f) * w * .26f) + f * .12f + up * ((k % 2) * .2f - .1f), new Vector3(w * .18f, .12f, .04f), rot * Quaternion.Euler(0, 0, (k - 1f) * 12f));
                    halos.Add(sc + f * .8f, 2.6f, neon[c1], .45f); if (signs % 2 == 0) ArtistBits.PointLight(ctx.Root, sc + f * 2f - up * 1.2f, neon[c1], 12f, 1.8f);
                    Mark(p, 2.6f); signs++;
                }
                else if (roll < .85f && floods < 16)
                {
                    // a floodlight mast: a lattice tower with four coloured lamps throwing pools of light
                    if (!Open(ctx, px, pz, 4f)) continue; var h = 9f + s * 4f;
                    foreach (var a in new[] { 45f, 135f, 225f, 315f }) { var lp = p + Quaternion.Euler(0, a, 0) * f * .5f; mk.Prism(steelD, lp, (up - (lp - p) * .03f).normalized, .07f, h, 5); }
                    for (var k = 1; k < 6; k++) mk.Prism(steel, p + up * (k * h / 6f), up, .55f, .06f, 4, 1f, 45f);
                    mk.Box(steelD, p + up * h, new Vector3(2.4f, .15f, .4f), rot);
                    for (var k = 0; k < 4; k++) { var c = neon[(k + (int)(s * 9f)) % neon.Length]; var lp = p + up * (h - .25f) + side * ((k - 1.5f) * .55f) + f * .15f; mk.Box(GlowM(c, 2.6f), lp, new Vector3(.4f, .3f, .2f), rot * Quaternion.Euler(35f, 0, 0)); halos.Add(lp + f * .3f, 1f, c, .6f); }
                    ArtistBits.PointLight(ctx.Root, p + up * (h - 1.5f) + f * 3f, neon[(int)(s * 9f) % neon.Length], 22f, 2.2f); ArtistBits.PointLight(ctx.Root, p + up * (h - 1.5f) - f * 3f, neon[((int)(s * 9f) + 2) % neon.Length], 22f, 2.2f);
                    ArtistBits.PostCollider(ctx.Root, p, h, .6f); Mark(p, 2.4f); floods++;
                }
                else if (roll < .91f)
                {
                    // a pipe run on trestles, wrapped in bright bands of neon
                    var len = 8f + s * 6f; var c = (int)(s * 13f) % neon.Length;
                    for (var k = 0; k < 3; k++) { var tp = p + side * ((k - 1f) * len * .4f); mk.Box(steelD, tp + up * .7f, new Vector3(.2f, 1.4f, .2f), rot); mk.Box(steelD, tp + up * 1.4f, new Vector3(.2f, .12f, 1.2f), rot); }
                    foreach (var e in new[] { -.3f, .3f }) mk.Prism(steel, p + up * 1.62f + f * e - side * len / 2f, side, .2f, len, 10);
                    for (var k = 0; k < 6; k++) foreach (var e in new[] { -.3f, .3f }) mk.Prism(neonM[(c + k) % neon.Length], p + up * 1.62f + f * e - side * (len / 2f - .8f - k * (len - 1.6f) / 5f), side, .215f, .14f, 10);
                    halos.Add(p + up * 1.7f + f * .8f, 2f, neon[c], .35f);
                    BoxCol(ctx.Root, p + up * 1f, new Vector3(len, 2f, 1.2f), Quaternion.LookRotation(f)); Mark(p, 2.6f); pipes++;
                }
            }
            Debug.Log($"The Electro Foundry: the Line {streetL:0} m, {stacks} smokestacks, {arms} robot arms, {girders} girder stacks, {drums} cable drums, {oil} oil drum groups, {tubes} neon pylons, {benches} benches, {signs} neon signs, {floods} floodlight masts, {pipes} pipe runs");
        }

        Vector3 NearestWalk(Vector3 p) { var best = center; var bd = float.MaxValue; foreach (var (q, _) in walkPoints) { var d = (q - p).sqrMagnitude; if (d < bd) { bd = d; best = q; } } return best; }

        /// Scrubby weeds in the cinders.
        void Verge(LandContext ctx)
        {
            var mat = WindMesh.Material(.2f, .8f, new Color(.26f, .3f, .24f));
            var meshes = new Dictionary<(int, int), WindMesh>(); var rng = new System.Random(2016);
            const float st = 3.2f;
            for (var x = ctx.MinX; x < ctx.MaxX; x += st)
            for (var z = ctx.MinZ; z < ctx.MaxZ; z += st)
            {
                var px = x + ((float)rng.NextDouble() - .5f) * st; var pz = z + ((float)rng.NextDouble() - .5f) * st;
                if (!ctx.IsLand(px, pz, 1f) || NearPath(px, pz, 3.6f) || InNoGrass(px, pz, 0f) || InFootprint(ctx, px, pz, -2f)) continue;
                if (Mathf.PerlinNoise(px * .05f + 9f, pz * .05f + 2f) < .5f) continue;
                var key = (Mathf.FloorToInt(px / 70f), Mathf.FloorToInt(pz / 70f));
                if (!meshes.TryGetValue(key, out var wm)) meshes[key] = wm = new WindMesh();
                var col = Color.Lerp(new Color(.24f, .28f, .22f), new Color(.34f, .34f, .26f), (float)rng.NextDouble());
                wm.Tuft(new Vector3(px, ctx.Ground(px, pz) - .04f, pz), .24f + (float)rng.NextDouble() * .3f, col, Color.Lerp(col, Green, .3f), rng, 3, .08f);
            }
            foreach (var kv in meshes) kv.Value.Build("Foundry weeds", ctx.Root, mat);
        }
    }
}
