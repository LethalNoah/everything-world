using System.Collections.Generic;
using UnityEngine;
using static EverythingWorld.World.PlaceTools;

namespace EverythingWorld.World
{
    /// The Four-on-the-Floor Steppe (WORLD_GUIDE.md): a festival on an open plain at night. You arrive in the crowd in front
    /// of the Main Stage: truss towers, a huge LED wall, speaker stacks, a DJ booth, moving lights sweeping the sky and
    /// lasers fanning over your head, everyone dancing. Festival Avenue runs past banners and food trucks to a lit-up Ferris
    /// wheel turning slowly against the stars. All round the plain: tents and campfires, flags, glow sticks in the grass,
    /// chill-out corners with bean bags, a little stage for every artist. Black night, laser green, electric blue, white.
    /// Laid out on purpose (the build-land standard): FloorTown.cs has the places and the people.
    public partial class FloorKit : LandKit
    {
        public override string Land => "The Four-on-the-Floor Steppe";
        public override Vector3 ArtistFront(IslandWorldData data, IslandWorldData.Artist a) => facing.TryGetValue(a.name, out var f) ? f : base.ArtistFront(data, a);

        public static readonly Color Laser = new Color(.3f, 1f, .4f), Blue = new Color(.2f, .5f, 1f), Magenta = new Color(1f, .2f, .8f), Amber = new Color(1f, .7f, .3f), White = new Color(.94f, .96f, 1f), Ink = new Color(.04f, .04f, .06f);
        static readonly Color[] Neon = { new Color(.3f, 1f, .4f), new Color(.2f, .5f, 1f), new Color(1f, .2f, .8f), new Color(1f, .7f, .3f), new Color(.4f, .9f, 1f) };
        const float PathW = 3f, AveW = 7f;

        Material ground, truss, dark, white, canvas, wood, lampM, screenM, stickM, fireM, leafM;
        GlowHalos halos; Vector3 center, D, S; FloorLife life;
        readonly Dictionary<(int, int), List<Vector3>> pathCells = new Dictionary<(int, int), List<Vector3>>();
        readonly List<(Vector3 p, int w)> walkPoints = new List<(Vector3, int)>();
        readonly Dictionary<string, Vector3> facing = new Dictionary<string, Vector3>();
        // x, radius, z
        readonly List<Vector3> noGrass = new List<Vector3>();
        Vector2 heart2, D2 = new Vector2(0, -1), S2 = new Vector2(1, 0); float plazaY, waterY, streetL;

        void MakeMaterials()
        {
            ground = WorldMaterials.Glow(new Color(.17f, .17f, .16f), .05f); truss = WorldMaterials.Solid(new Color(.5f, .52f, .56f), .7f); dark = WorldMaterials.Solid(new Color(.04f, .04f, .05f)); white = WorldMaterials.Glow(new Color(.86f, .86f, .9f), .16f); canvas = WorldMaterials.Glow(new Color(.8f, .78f, .72f), .14f); wood = WorldMaterials.Glow(new Color(.46f, .36f, .26f), .1f); leafM = WorldMaterials.Glow(new Color(.2f, .3f, .22f), .1f);
            lampM = new Material(WorldMaterials.Glow(Amber, 1.6f)) { name = "Floor lamps" }; screenM = new Material(WorldMaterials.Glow(Blue, 1.4f)) { name = "Floor screens" }; stickM = new Material(WorldMaterials.Glow(Laser, 2f)) { name = "Floor glow sticks" }; fireM = new Material(WorldMaterials.Glow(new Color(1f, .5f, .16f), 2f)) { name = "Floor fires" };
        }

        Material GlowM(Color c, float s = 2f) => WorldMaterials.Glow(c, s * .65f);
        Material Paint(Color c) => WorldMaterials.Glow(c, .14f);
        static bool HasStation(IslandWorldData.Artist a) => a.stationId != 0;
        static float Foot(IslandWorldData.Artist a) => a.name == "deadmau5" ? 14f : a.name == "Astronaut" ? 13f : a.name == "Zero Eden" ? 12f : 8f;

        public override bool Ground(float height, float slope, float patch, out Color color)
        {
            // steppe grass at night: dry olive and straw, trampled paler near the stages
            if (slope > 24f) color = Color.Lerp(new Color(.42f, .4f, .34f), new Color(.5f, .46f, .4f), patch);
            else color = Color.Lerp(new Color(.4f, .44f, .3f), new Color(.5f, .5f, .34f), patch);
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
            for (var k = 0; k < 8; k++) { var dir = new Vector2(Mathf.Sin(k * Mathf.PI / 4f), Mathf.Cos(k * Mathf.PI / 4f)); var len = 40f; while (len < 110f && Inside(heart2 + dir * (len + 22f), 30f) && Inside(heart2 - dir * 26f, 24f)) len += 10f; if (len > streetL) { streetL = len; D2 = dir; } }
            S2 = new Vector2(-D2.y, D2.x); plazaY = Mathf.Max(data.HeightAt(heart2.x, heart2.y), waterY + 6f);
            var keep = new List<string>();
            void Put(string name, float d, float s, float room) { var p = P2(d, s); if (!Inside(p, room)) return; foreach (var a in data.layout.artists) if (a.sceneId == isl.sceneId && a.name == name) { a.x = p.x; a.z = p.y; keep.Add(name); } }
            Put("deadmau5", 4f, -56f, 24f); Put("Astronaut", 4f, 54f, 22f); Put("Zero Eden", streetL + 18f, -34f, 20f);
            var wheel = P2(streetL + 16f, 0);
            var avoid = new List<Vector3>(); for (var d = -20f; d <= streetL; d += 20f) avoid.Add(new Vector3(heart2.x + D2.x * d, 20f, heart2.y + D2.y * d)); avoid.Add(new Vector3(wheel.x, 20f, wheel.y));
            ArtistPlacement.Spread(data, index, 50f, heart2, 44f, 38f, avoid, keep, Land, 330f);
            Level(data, index, heart2 - D2 * 6f, 26f, 40f, plazaY);
            for (var d = 22f; d <= streetL + 16f; d += 8f) { var p = P2(d, 0); Level(data, index, p, 6f, 13f, Mathf.Lerp(plazaY, Mathf.Max(data.HeightAt(p.x, p.y), waterY + 3f), Mathf.Clamp01((d - 22f) / 30f))); }
            Level(data, index, wheel, 14f, 24f, Mathf.Max(data.HeightAt(wheel.x, wheel.y), waterY + 3f));
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
            life = new GameObject("Floor life").AddComponent<FloorLife>();
            life.transform.SetParent(ctx.Root, false);
            life.center = center; life.lamps = lampM; life.screens = screenM; life.sticks = stickM; life.fires = fireM;
            var mk = new MeshKit(); var solid = new MeshKit();
            Remember(ctx.OnGround(center.x, center.z), 0);
            TownStart(ctx);
            MainStage(ctx, mk, solid);
            Avenue(ctx, mk, solid);
            Wheel(ctx, mk, solid);
            Paths(ctx, mk, solid);
            Wilds(ctx, mk, solid);
            mk.Build("The Four-on-the-Floor Steppe", ctx.Root, false);
            solid.BuildCollider("The Four-on-the-Floor Steppe walls", ctx.Root);
        }

        public override void Finish(LandContext ctx) { Verge(ctx); halos.Build("Floor glow", ctx.Root); }

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

        /// A hay bale to sit on.
        void Bench(Transform parent, MeshKit mk, Vector3 g, Vector3 face, float len = 2f)
        {
            var up = Vector3.up; var rot = Quaternion.LookRotation(face); var side = Vector3.Cross(up, face); mk.Box(Paint(new Color(.78f, .66f, .42f)), g + up * .3f, new Vector3(len, .6f, .6f), rot); for (var k = 0; k < 3; k++) mk.Box(Paint(new Color(.6f, .5f, .3f)), g + up * .3f + side * (-len / 3f + k * len / 3f), new Vector3(.03f, .62f, .62f), rot);
            BoxCol(parent, g + up * .3f, new Vector3(len, .6f, .6f), rot); Interact.Bench(tlife.transform, g + up * .62f - side * (len / 2f - .5f), g + up * .62f + side * (len / 2f - .5f), face);
        }

        /// A festoon pole: a pole with a string of warm bulbs.
        void Lamp(LandContext ctx, MeshKit mk, Vector3 g, bool light = false)
        {
            var up = Vector3.up; mk.Prism(wood, g - up * .2f, up, .07f, 4.2f, 6); for (var k = 0; k < 5; k++) mk.Blob(lampM, g + up * (3.9f - k * .2f) + S * (k * .35f - .7f), Vector3.one * .08f, new System.Random(k), 0f); mk.Box(dark, g + up * 3.6f, new Vector3(.02f, .02f, 1.6f), Quaternion.LookRotation(S) * Quaternion.Euler(-10f, 0, 0)); halos.Add(g + up * 3.6f, 1.6f, Amber, .4f); ArtistBits.PostCollider(ctx.Root, g, 4f, .1f); if (light) ArtistBits.PointLight(ctx.Root, g + up * 3.4f, Amber, 12f, 1.3f);
        }

        /// A dome tent in a festival colour, a lantern by the door.
        void Tent(LandContext ctx, MeshKit mk, Vector3 g, Vector3 face, Color col, System.Random rng)
        {
            var up = Vector3.up; mk.Blob(Paint(col), g + up * .5f, new Vector3(1.4f, 1.1f, 1.6f), rng, 0f); mk.Box(dark, g + up * .5f + face * 1.3f, new Vector3(.6f, .8f, .3f), Quaternion.LookRotation(face)); mk.Prism(lampM, g + face * 1.8f + Vector3.Cross(up, face) * .6f, up, .08f, .2f, 6); BoxCol(ctx.Root, g + up * .5f, new Vector3(2.6f, 1.1f, 3f), Quaternion.LookRotation(face));
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
                Strip(mk, pts, ground, PathW, .1f, ctx);
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

        // ---------------------------------------------------------------- the rest of the festival
        /// Tents round campfires, flag poles, glow sticks in the grass, chill-out corners with bean bags, food stalls, light towers, hay-bale seats.
        void Wilds(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            int camps = 0, flags = 0, sticks = 0, chill = 0, stalls = 0, towers = 0, bales = 0; var rng = new System.Random(2011);
            const float cell = 20f;
            for (var x = ctx.MinX; x < ctx.MaxX; x += cell)
            for (var z = ctx.MinZ; z < ctx.MaxZ; z += cell)
            {
                float px = x + (float)rng.NextDouble() * cell, pz = z + (float)rng.NextDouble() * cell; var roll = (float)rng.NextDouble(); var s = (float)rng.NextDouble();
                if (!Open(ctx, px, pz, 3f)) continue;
                var p = ctx.OnGround(px, pz); var f = FaceTo(p, NearestWalk(p)); var rot = Quaternion.LookRotation(f); var side = Vector3.Cross(Vector3.up, f); var up = Vector3.up;
                if (roll < .17f) { if (!Open(ctx, px, pz, 5f)) continue; var n = 3 + (int)(s * 3f); for (var k = 0; k < n; k++) { var d = Quaternion.Euler(0, k * 360f / n + s * 40f, 0) * f; Tent(ctx, mk, p + d * 4f, -d, Neon[(k + (int)(s * 9f)) % Neon.Length] * .8f, rng); } for (var k = 0; k < 6; k++) mk.Blob(Paint(new Color(.4f, .4f, .4f)), p + Quaternion.Euler(0, k * 60f, 0) * f * .7f, new Vector3(.2f, .14f, .2f), rng, .2f); mk.Cone(fireM, p, .4f, .9f, 6); life.fireSpots.Add(p + up * .7f); if (camps % 3 == 0) ArtistBits.PointLight(ctx.Root, p + up * 1f, new Color(1f, .6f, .3f), 9f, 1.6f); halos.Add(p + up * .8f, 2f, new Color(1f, .6f, .3f), .35f); ArtistBits.PostCollider(ctx.Root, p, .8f, .6f); Mark(p, 5.6f); camps++; }
                else if (roll < .3f) { for (var k = 0; k < 3; k++) { var fp = p + side * (k - 1) * 1.6f; var h = 5f + k * .6f; mk.Prism(truss, fp, up, .04f, h, 4); ArtistBits.PostCollider(ctx.Root, fp, h, .08f); mk.Tri(GlowM(Neon[(k + (int)(s * 5f)) % Neon.Length], .9f), fp + up * h, fp + up * (h - 1.2f), fp + up * (h - .6f) + f * 1.6f, fp + side); mk.Tri(GlowM(Neon[(k + (int)(s * 5f)) % Neon.Length], .9f), fp + up * h, fp + up * (h - 1.2f), fp + up * (h - .6f) + f * 1.6f, fp - side); } flags++; }
                else if (roll < .44f) { for (var k = 0; k < 8; k++) { var sp = p + new Vector3((float)rng.NextDouble() - .5f, 0, (float)rng.NextDouble() - .5f) * 6f; sp = ctx.OnGround(sp.x, sp.z); mk.Prism(k % 3 == 0 ? stickM : GlowM(Neon[k % Neon.Length], 2f), sp + up * .04f, (side * Mathf.Sin(k) + f * Mathf.Cos(k)).normalized, .02f, .3f, 5); } sticks++; }
                else if (roll < .52f) { var rug = p; mk.Box(Paint(new Color(.5f, .2f, .4f)), rug + up * .02f, new Vector3(4f, .02f, 3f), rot); for (var k = 0; k < 4; k++) { var bp = rug + side * (k - 1.5f) * .9f + f * ((k % 2) * .5f); mk.Blob(Paint(Neon[k % Neon.Length] * .7f), bp + up * .3f, new Vector3(.4f, .3f, .4f), rng, .1f); Interact.Seat(tlife.transform, bp + up * .4f, f, "Sink into a bean bag").seatHeight = .35f; } mk.Prism(truss, rug - f * 1.6f, up, .04f, 2.6f, 4); mk.Blob(GlowM(Magenta, 1.4f), rug - f * 1.6f + up * 2.7f, Vector3.one * .2f, rng, 0f); ArtistBits.PostCollider(ctx.Root, rug - f * 1.6f, 2.6f, .1f); Mark(p, 2.6f); chill++; }
                else if (roll < .58f) { if (!Open(ctx, px, pz, 4f)) continue; var col = Neon[(int)(s * 5f) % Neon.Length]; mk.Box(white, p + up * 1.3f, new Vector3(3.6f, 2.6f, 2.2f), rot); solid.Box(white, p + up * 1.3f, new Vector3(3.6f, 2.6f, 2.2f), rot); mk.Box(GlowM(col, 1.2f), p + up * 1.6f + f * 1.11f, new Vector3(2.4f, .9f, .02f), rot); mk.Box(Paint(col * .7f), p + up * 2.7f + f * 1.5f, new Vector3(3.6f, .06f, 1f), rot * Quaternion.Euler(-15f, 0, 0)); mk.Box(wood, p + up * 1.05f + f * 1.3f, new Vector3(3f, .08f, .4f), rot); foreach (var e in new[] { -1.3f, 1.3f }) mk.Prism(dark, p + up * .3f + side * e + f * .6f, side * Mathf.Sign(e), .3f, .2f, 10); Folk(p + f * 2.4f, -f, Resident.Pose.Stand, new[] { "Chips? Chips.", "Festival food. Don't ask what's in it. It's great." }, Local()); Mark(p, 3f); stalls++; }
                else if (roll < .63f && towers < 14) { var h = 9f; foreach (var (a, b) in new[] { (-1f, -1f), (1f, -1f), (1f, 1f), (-1f, 1f) }) { var leg = p + side * a * .7f + f * b * .7f; mk.Prism(truss, leg, up, .06f, h, 4); } ArtistBits.PostCollider(ctx.Root, p, h, 1f); for (var k = 0; k < 4; k++) mk.Box(GlowM(White, 1.6f), p + up * (h + .2f) + Quaternion.Euler(0, k * 90f, 0) * f * .5f, new Vector3(.4f, .4f, .2f), Quaternion.Euler(0, k * 90f, 0) * rot); var beam = new GameObject("Light tower beam"); beam.transform.SetParent(ctx.Root, false); beam.transform.position = p + up * (h + .4f); var l = beam.AddComponent<Light>(); l.type = LightType.Spot; l.range = 120f; l.spotAngle = 8f; l.intensity = 4f; l.color = Neon[towers % Neon.Length]; life.beams.Add((beam.transform, towers * 1.7f)); Mark(p, 1.6f); towers++; }
                else if (roll < .7f) { Bench(ctx.Root, mk, p, f); Lamp(ctx, mk, p - f * 1f + side * 1.6f, bales % 2 == 0); Mark(p, 2.2f); bales++; }
            }
            Debug.Log($"The Four-on-the-Floor Steppe: Festival Avenue {streetL:0} m, {camps} camps, {flags} flag rows, {sticks} glow-stick patches, {chill} chill-out rugs, {stalls} food stalls, {towers} light towers, {bales} hay bales");
        }

        Vector3 NearestWalk(Vector3 p) { var best = center; var bd = float.MaxValue; foreach (var (q, _) in walkPoints) { var d = (q - p).sqrMagnitude; if (d < bd) { bd = d; best = q; } } return best; }

        /// Dry steppe grass, catching the stage lights at the tips.
        void Verge(LandContext ctx)
        {
            var mat = WindMesh.Material(.3f, .8f, new Color(.44f, .46f, .32f));
            var meshes = new Dictionary<(int, int), WindMesh>(); var rng = new System.Random(2012);
            const float st = 2.4f;
            for (var x = ctx.MinX; x < ctx.MaxX; x += st)
            for (var z = ctx.MinZ; z < ctx.MaxZ; z += st)
            {
                var px = x + ((float)rng.NextDouble() - .5f) * st; var pz = z + ((float)rng.NextDouble() - .5f) * st;
                if (!ctx.IsLand(px, pz, 1f) || NearPath(px, pz, 3.6f) || InNoGrass(px, pz, 0f) || InFootprint(ctx, px, pz, -2f)) continue;
                if (Mathf.PerlinNoise(px * .04f + 4f, pz * .04f + 8f) < .45f) continue;
                var key = (Mathf.FloorToInt(px / 70f), Mathf.FloorToInt(pz / 70f));
                if (!meshes.TryGetValue(key, out var wm)) meshes[key] = wm = new WindMesh();
                var col = Color.Lerp(new Color(.42f, .44f, .3f), new Color(.56f, .52f, .34f), (float)rng.NextDouble());
                wm.Tuft(new Vector3(px, ctx.Ground(px, pz) - .04f, pz), .3f + (float)rng.NextDouble() * .3f, col, Color.Lerp(col, Laser, .25f), rng, 3, .08f);
            }
            foreach (var kv in meshes) kv.Value.Build("Floor grass", ctx.Root, mat);
        }
    }
}
