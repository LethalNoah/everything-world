using System.Collections.Generic;
using UnityEngine;
using static EverythingWorld.World.PlaceTools;

namespace EverythingWorld.World
{
    /// Mount Virtuoso (WORLD_GUIDE.md): one mountain, calm below and wild on top. You arrive on a stone terrace in misty
    /// northern foothills with a grand piano looking out to sea; birches, heather, mossy stones and cairns all round. A
    /// path cut into the mountain spirals up past ledges where the guitar heroes play, each with an amp stack and a giant
    /// guitar, into cloud, wind and rain, to the Summit: a bare rock top with a wall of amps and a lightning rod, where the
    /// storm never stops. Cool silver-blue and moss below; slate grey, snow and lightning above.
    /// Laid out on purpose (the build-land standard): VirtTown.cs has the places and the people.
    public partial class VirtKit : LandKit
    {
        public override string Land => "Mount Virtuoso";
        public override Vector3 ArtistFront(IslandWorldData data, IslandWorldData.Artist a) => facing.TryGetValue(a.name, out var f) ? f : base.ArtistFront(data, a);

        public static readonly Color Moss = new Color(.36f, .48f, .34f), Heather = new Color(.56f, .4f, .62f), Slate = new Color(.4f, .42f, .48f), Snow = new Color(.78f, .8f, .86f), Lightning = new Color(.8f, .86f, 1f), Ember = new Color(1f, .5f, .2f), Ink = new Color(.08f, .08f, .1f), Glow = new Color(1f, .82f, .56f);
        static readonly string[] Shredders = { "Eric Johnson", "Nili Brosh", "Joe Satriani", "Richie Kotzen", "Buckethead" };
        const float PathW = 3f, ClimbW = 4f, Rm = 72f, Hm = 58f;

        Material rock, rockD, path, wood, iron, dark, white, leafM, birchM, trunkM, lampM, ampM, snowM, waterM;
        GlowHalos halos; Vector3 center, D, S; VirtLife life;
        readonly Dictionary<(int, int), List<Vector3>> pathCells = new Dictionary<(int, int), List<Vector3>>();
        readonly List<(Vector3 p, int w)> walkPoints = new List<(Vector3, int)>();
        readonly Dictionary<string, Vector3> facing = new Dictionary<string, Vector3>();
        // x, radius, z
        readonly List<Vector3> noGrass = new List<Vector3>();
        Vector2 heart2, D2 = new Vector2(0, -1), S2 = new Vector2(1, 0), peak2; float plazaY, waterY, streetL, baseY, snowLine, a0;
        readonly List<Vector3> climb = new List<Vector3>();
        readonly Dictionary<string, (Vector3 at, Vector3 outward)> ledges = new Dictionary<string, (Vector3, Vector3)>();

        void MakeMaterials()
        {
            rock = WorldMaterials.Glow(new Color(.44f, .46f, .5f), .08f); rockD = WorldMaterials.Glow(new Color(.3f, .32f, .36f), .06f); path = WorldMaterials.Glow(new Color(.46f, .44f, .4f), .08f); wood = WorldMaterials.Glow(new Color(.46f, .36f, .28f), .1f); iron = WorldMaterials.Solid(new Color(.2f, .2f, .22f), .5f); dark = WorldMaterials.Solid(new Color(.05f, .05f, .06f)); white = WorldMaterials.Glow(new Color(.86f, .86f, .88f), .14f);
            leafM = WorldMaterials.Glow(new Color(.46f, .58f, .36f), .1f); birchM = WorldMaterials.Glow(new Color(.84f, .84f, .8f), .12f); trunkM = WorldMaterials.Glow(new Color(.3f, .26f, .24f), .1f); snowM = WorldMaterials.Glow(Snow, .14f);
            lampM = new Material(WorldMaterials.Glow(Glow, 1.6f)) { name = "Virt lamps" }; ampM = new Material(WorldMaterials.Glow(Ember, 1.4f)) { name = "Virt amp lights" }; waterM = new Material(WorldMaterials.Glow(new Color(.3f, .46f, .56f), .5f)) { name = "Virt water" };
        }

        Material GlowM(Color c, float s = 2f) => WorldMaterials.Glow(c, s * .65f);
        Material Paint(Color c) => WorldMaterials.Glow(c, .14f);
        static bool HasStation(IslandWorldData.Artist a) => a.stationId != 0;
        static float Foot(IslandWorldData.Artist a) => a.name == "Ólafur Arnalds" ? 13f : 8f;
        static bool IsShredder(string n) => System.Array.IndexOf(Shredders, n) >= 0;

        public override bool Ground(float height, float slope, float patch, out Color color)
        {
            // moss and heather in the foothills; slate rock higher up; snow near the top
            if (snowLine > 0f && height > snowLine + patch * 6f) color = Color.Lerp(new Color(.6f, .62f, .68f), Snow, patch * .6f);
            else if (snowLine > 0f && height > snowLine - 22f) color = Color.Lerp(new Color(.36f, .38f, .42f), new Color(.46f, .46f, .5f), patch);
            else if (slope > 26f) color = Color.Lerp(new Color(.4f, .42f, .44f), new Color(.48f, .48f, .5f), patch);
            else color = patch < .7f ? Color.Lerp(new Color(.34f, .46f, .34f), new Color(.42f, .52f, .38f), patch / .7f) : Color.Lerp(new Color(.46f, .38f, .48f), new Color(.52f, .42f, .54f), (patch - .7f) / .3f);
            color.a = 1f;
            return true;
        }

        static float Ease(float a, float b, float x) { var t = Mathf.Clamp01((x - a) / (b - a)); return t * t * (3f - 2f * t); }
        Vector2 P2(float d, float s) => heart2 + D2 * d + S2 * s;
        Vector3 Wp(float d, float s) => center + D * d + S * s;
        Vector3 G(LandContext ctx, float d, float s) { var p = Wp(d, s); return ctx.OnGround(p.x, p.z); }

        float Surface(float r) => baseY + Hm * Mathf.Pow(Mathf.Clamp01(1f - r / Rm), 1.3f);

        // ---------------------------------------------------------------- shaping the ground: raising the mountain and cutting the path into it
        public override void ShapeGround(IslandWorldData data, int index)
        {
            var isl = data.layout.islands[index]; heart2 = new Vector2(isl.labelX, isl.labelZ); waterY = data.layout.waterLevel;
            bool Inside(Vector2 p, float m) { if (data.IslandAt(p.x, p.y) != isl) return false; for (var k = 0; k < 12; k++) { var a = k * Mathf.PI / 6f; if (data.IslandAt(p.x + Mathf.Cos(a) * m, p.y + Mathf.Sin(a) * m) != isl) return false; } return true; }
            // the mountain goes where there's most room, far enough from where you arrive
            var best = -1f;
            for (var k = 0; k < 16; k++) { var dir = new Vector2(Mathf.Sin(k * Mathf.PI / 8f), Mathf.Cos(k * Mathf.PI / 8f)); for (var d = 150f; d >= 95f; d -= 5f) { var p = heart2 + dir * d; if (!Inside(p, 50f)) continue; var room = 0f; for (var r = 50f; r <= 110f && Inside(p, r); r += 10f) room = r; if (room + d * .1f > best) { best = room + d * .1f; D2 = dir; streetL = d; } break; } }
            if (best < 0f) { D2 = new Vector2(0, 1); streetL = 100f; }
            S2 = new Vector2(-D2.y, D2.x); peak2 = heart2 + D2 * streetL; plazaY = Mathf.Max(data.HeightAt(heart2.x, heart2.y), waterY + 6f);
            baseY = Mathf.Max(data.HeightAt(peak2.x, peak2.y), waterY + 3f); snowLine = baseY + Hm * .62f;
            for (var r = 0; r < data.Nz; r++)
            {
                var z = data.layout.depth - r * data.Step; if (Mathf.Abs(z - peak2.y) > Rm) continue;
                for (var col = 0; col < data.Nx; col++)
                {
                    var x = col * data.Step; if (Mathf.Abs(x - peak2.x) > Rm) continue;
                    var i = r * data.Nx + col; if (data.islandIds[i] != index) continue;
                    var dd = Vector2.Distance(new Vector2(x, z), peak2); if (dd > Rm) continue;
                    var h = Surface(dd) + (Mathf.PerlinNoise(x * .05f, z * .05f) - .5f) * 6f * Mathf.Clamp01(dd / 20f);
                    if (h > data.heights[i]) data.heights[i] = h;
                }
            }
            // the climb: a spiral from the foot (on the side facing where you arrive) to just under the top
            climb.Clear(); a0 = Mathf.Atan2(-D2.y, -D2.x); const float turns = 1.6f;
            for (var t = 0f; t <= 1.0001f; t += .004f) { var a = a0 + t * turns * Mathf.PI * 2f; var rr = Mathf.Lerp(Rm - 6f, 11f, t); var p = peak2 + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * rr; climb.Add(new Vector3(p.x, Surface(rr), p.y)); }
            var ledgeT = new[] { .22f, .38f, .54f, .7f, .86f }; ledges.Clear();
            for (var k = 0; k < Shredders.Length; k++) { var ci = Mathf.RoundToInt(ledgeT[k] * (climb.Count - 1)); var cp = climb[ci]; var outw = new Vector3(cp.x - peak2.x, 0, cp.z - peak2.y).normalized; ledges[Shredders[k]] = (cp + outw * 11f, outw); }
            // cut the path as a shelf, the ledges as flat pads, the summit as a plateau
            void Flat(Vector2 c, float rad, float y) { for (var r = 0; r < data.Nz; r++) { var z = data.layout.depth - r * data.Step; if (Mathf.Abs(z - c.y) > rad) continue; for (var col = 0; col < data.Nx; col++) { var x = col * data.Step; if (Mathf.Abs(x - c.x) > rad) continue; var i = r * data.Nx + col; if (data.islandIds[i] != index) continue; if (Vector2.Distance(new Vector2(x, z), c) < rad) data.heights[i] = y; } } }
            for (var k = 0; k < climb.Count; k += 2) Flat(new Vector2(climb[k].x, climb[k].z), 7f, climb[k].y);
            foreach (var kv in ledges) { var (at, _) = kv.Value; var ci = 0; var bd = float.MaxValue; for (var k = 0; k < climb.Count; k++) { var dd = (climb[k] - at).sqrMagnitude; if (dd < bd) { bd = dd; ci = k; } } Flat(new Vector2(at.x, at.z), 12f, climb[ci].y); var mid = (at + climb[ci]) / 2f; Flat(new Vector2(mid.x, mid.z), 7f, climb[ci].y); }
            Flat(peak2, 12f, Surface(11f));
            // put the guitar heroes on their ledges; everyone else in the foothills
            var keep = new List<string>();
            foreach (var a in data.layout.artists) { if (a.sceneId != isl.sceneId) continue; if (ledges.TryGetValue(a.name, out var l)) { a.x = l.at.x; a.z = l.at.z; keep.Add(a.name); } }
            foreach (var a in data.layout.artists) if (a.sceneId == isl.sceneId && a.name == "Ólafur Arnalds") { var p = heart2 - D2 * 30f; if (Inside(p, 16f)) { a.x = p.x; a.z = p.y; keep.Add(a.name); } }
            var avoid = new List<Vector3> { new Vector3(peak2.x, Rm + 12f, peak2.y), new Vector3(heart2.x, 20f, heart2.y) };
            ArtistPlacement.Spread(data, index, 40f, heart2, 36f, 30f, avoid, keep, Land, 320f);
            Level(data, index, heart2, 14f, 26f, plazaY);
            foreach (var a in data.layout.artists)
            {
                if (a.sceneId != isl.sceneId) continue;
                a.r = Mathf.Max(a.r, Foot(a) + 6f);
                if (!IsShredder(a.name)) Level(data, index, new Vector2(a.x, a.z), Foot(a) + 1f, Foot(a) + 9f, Mathf.Max(data.HeightAt(a.x, a.z), waterY + 2.6f));
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
            life = new GameObject("Virt life").AddComponent<VirtLife>();
            life.transform.SetParent(ctx.Root, false);
            life.center = center; life.lamps = lampM; life.amps = ampM; life.water = waterM; life.stormFrom = baseY + Hm * .45f; life.summit = new Vector3(peak2.x, Surface(11f), peak2.y);
            var mk = new MeshKit(); var solid = new MeshKit();
            Remember(ctx.OnGround(center.x, center.z), 0);
            TownStart(ctx);
            Terrace(ctx, mk, solid);
            Climb(ctx, mk, solid);
            Summit(ctx, mk, solid);
            Paths(ctx, mk, solid);
            Wilds(ctx, mk, solid);
            mk.Build("Mount Virtuoso", ctx.Root, false);
            solid.BuildCollider("Mount Virtuoso walls", ctx.Root);
        }

        public override void Finish(LandContext ctx) { Verge(ctx); halos.Build("Virt glow", ctx.Root); }

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

        /// A rough plank bench.
        void Bench(Transform parent, MeshKit mk, Vector3 g, Vector3 face, float len = 2f)
        {
            var up = Vector3.up; var rot = Quaternion.LookRotation(face); var side = Vector3.Cross(up, face); mk.Box(wood, g + up * .44f, new Vector3(len, .1f, .45f), rot); foreach (var e in new[] { -1f, 1f }) mk.Box(rockD, g + up * .2f + side * e * (len / 2f - .25f), new Vector3(.4f, .4f, .45f), rot);
            BoxCol(parent, g + up * .25f, new Vector3(len, .5f, .5f), rot); Interact.Bench(tlife.transform, g + up * .5f - side * (len / 2f - .5f), g + up * .5f + side * (len / 2f - .5f), face);
        }

        /// A lantern hung from a crooked wooden post.
        void Lamp(LandContext ctx, MeshKit mk, Vector3 g, bool light = false)
        {
            var up = Vector3.up; mk.Prism(wood, g - up * .2f, up, .07f, 2.8f, 6, .8f); mk.Box(wood, g + up * 2.5f + S * .3f, new Vector3(.05f, .05f, .7f), Quaternion.LookRotation(S)); mk.Prism(lampM, g + up * 2.1f + S * .55f, up, .1f, .26f, 6); mk.Cone(iron, g + up * 2.36f + S * .55f, .14f, .12f, 6); halos.Add(g + up * 2.2f + S * .55f, 1.3f, Glow, .45f); ArtistBits.PostCollider(ctx.Root, g, 2.6f, .08f); if (light) ArtistBits.PointLight(ctx.Root, g + up * 2f + S * .55f, Glow, 11f, 1.3f);
        }

        /// A giant guitar standing on its end, propped on a rock.
        void Guitar(MeshKit mk, Transform root, Vector3 g, Vector3 face, Color body, Material glow, float s)
        {
            var up = Vector3.up; var rot = Quaternion.LookRotation(face); var side = Vector3.Cross(up, face); var lean = Quaternion.AngleAxis(-12f, side);
            var b = g + up * 1.1f * s; mk.Blob(Paint(body), b, new Vector3(1f, 1.1f, .3f) * s, new System.Random(3), 0f); mk.Blob(Paint(body), b + lean * up * 1.1f * s, new Vector3(.8f, .8f, .28f) * s, new System.Random(4), 0f); mk.Prism(dark, b + lean * up * .6f * s + face * .16f * s, face, .26f * s, .02f, 14);
            mk.Box(Paint(new Color(.3f, .2f, .14f)), b + lean * up * 2.8f * s, new Vector3(.2f, 2.6f, .12f) * s, rot * lean); mk.Box(Paint(new Color(.2f, .16f, .12f)), b + lean * up * 4.3f * s, new Vector3(.36f, .6f, .1f) * s, rot * lean);
            for (var k = 0; k < 6; k++) mk.Box(glow, b + lean * up * (2.6f * s) + side * (-.08f + k * .032f) * s + face * .08f * s, new Vector3(.01f, 3.4f, .01f) * s, rot * lean);
            ArtistBits.PostCollider(root, g, 3f * s, .6f * s);
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
                    var dd = Mathf.Min(d, end); var q = from + dir * dd + side * Mathf.Sin(dd / 16f) * Mathf.Min(1.6f, end * .04f);
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
            // the guitar heroes face their stretch of the climb
            foreach (var a in ctx.Artists) if (ledges.TryGetValue(a.name, out var l)) { facing[a.name] = -l.outward; todo.Remove(a); }
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

        // ---------------------------------------------------------------- the rest of the mountain
        /// Birch groves, mossy boulders, cairns, heather, a turf-roofed cabin or two and benches in the foothills; bare rocks, snow and dead trees higher up.
        void Wilds(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            int birches = 0, boulders = 0, cairns = 0, heather = 0, cabins = 0, benches = 0, crags = 0; var rng = new System.Random(1990);
            const float cell = 18f;
            for (var x = ctx.MinX; x < ctx.MaxX; x += cell)
            for (var z = ctx.MinZ; z < ctx.MaxZ; z += cell)
            {
                float px = x + (float)rng.NextDouble() * cell, pz = z + (float)rng.NextDouble() * cell; var roll = (float)rng.NextDouble(); var s = (float)rng.NextDouble();
                if (!Open(ctx, px, pz, 3f)) continue;
                var p = ctx.OnGround(px, pz); var f = FaceTo(p, NearestWalk(p)); var rot = Quaternion.LookRotation(f); var side = Vector3.Cross(Vector3.up, f); var up = Vector3.up;
                if (p.y > baseY + Hm * .35f) { if (roll < .4f) { mk.Blob(roll < .2f ? rock : rockD, p + up * .4f, new Vector3(1.4f + s, 1f + s * .6f, 1.2f + s), rng, .3f); if (p.y > snowLine) mk.Blob(snowM, p + up * (1.2f + s * .5f), new Vector3(1.1f + s * .6f, .3f, 1f + s * .6f), rng, .2f); ArtistBits.PostCollider(ctx.Root, p, 1.4f, 1.2f + s * .6f); crags++; } else if (roll < .5f) { var prev = p; for (var k = 0; k < 3; k++) { var dir = (up + new Vector3(Mathf.Sin(k + s * 5f), 0, Mathf.Cos(k * 2f)) * .4f).normalized; mk.Prism(trunkM, prev, dir, .12f - k * .03f, 1.2f, 5); prev += dir * 1.2f; } ArtistBits.PostCollider(ctx.Root, p, 2.4f, .2f); crags++; } continue; }
                if (roll < .26f) { for (var k = 0; k < 3 + (int)(s * 4f); k++) { var bp = ctx.OnGround(p.x + ((k * 37) % 5 - 2f) * 1.4f, p.z + ((k * 23) % 5 - 2f) * 1.4f); var h = 4f + (k % 3) * 1.4f; mk.Prism(birchM, bp, up, .14f, h, 6, .7f); for (var j = 0; j < 4; j++) mk.Box(dark, bp + up * (1f + j * h * .2f), new Vector3(.3f, .04f, .02f), Quaternion.Euler(0, k * 40f + j * 70f, 0)); mk.Blob(leafM, bp + up * (h + .4f), new Vector3(1.1f, 1.4f, 1.1f), rng, .3f); ArtistBits.PostCollider(ctx.Root, bp, h, .2f); } Mark(p, 4f); birches++; }
                else if (roll < .4f) { mk.Blob(rock, p + up * .5f, new Vector3(1.6f + s, 1.1f, 1.4f + s), rng, .25f); mk.Blob(GlowM(Moss, .3f), p + up * (1.2f + s * .2f), new Vector3(1.2f + s * .6f, .3f, 1f + s * .6f), rng, .2f); ArtistBits.PostCollider(ctx.Root, p, 1.4f, 1.4f + s * .5f); Mark(p, 2f); boulders++; }
                else if (roll < .48f) { for (var k = 0; k < 5; k++) mk.Blob(k % 2 == 0 ? rock : rockD, p + up * (.2f + k * .36f), new Vector3(.6f - k * .08f, .22f, .5f - k * .07f), rng, .2f); ArtistBits.PostCollider(ctx.Root, p, 1.8f, .5f); cairns++; }
                else if (roll < .6f) { for (var k = 0; k < 10; k++) mk.Blob(Paint(k % 3 == 0 ? Heather * 1.15f : Heather), ctx.OnGround(p.x + ((float)rng.NextDouble() - .5f) * 6f, p.z + ((float)rng.NextDouble() - .5f) * 6f) + up * .2f, new Vector3(.5f, .3f, .5f), rng, .3f); heather++; }
                else if (roll < .64f && cabins < 8) { if (!Open(ctx, px, pz, 5f)) continue; var cf = new PlaceFrame(p, f); var wl = Paint(new Color(.36f, .26f, .2f)); cf = new PlaceFrame(p, f); mk.Box(wl, cf.P(0, 1.3f, 0), new Vector3(4f, 2.6f, 3.4f), cf.r); solid.Box(wl, cf.P(0, 1.3f, 0), new Vector3(4f, 2.6f, 3.4f), cf.r); mk.Prism(GlowM(Moss, .2f), cf.P(0, 2.6f, 0), Vector3.up, 2.9f, 1.4f, 4, .05f, 45f); mk.Box(GlowM(Glow, 1.2f), cf.P(1f, 1.4f, 1.71f), new Vector3(.7f, .6f, .02f), cf.r); mk.Box(dark, cf.P(-.8f, 1f, 1.71f), new Vector3(.8f, 2f, .02f), cf.r); mk.Prism(rockD, cf.P(1.6f, 2.6f, -1f), Vector3.up, .3f, 1.4f, 6); life.chimneys.Add(cf.P(1.6f, 4.1f, -1f)); halos.Add(cf.P(1f, 1.4f, 2.2f), 1.2f, Glow, .4f); Mark(p, 3.4f); cabins++; }
                else if (roll < .7f) { Bench(ctx.Root, mk, p, f); Lamp(ctx, mk, p - f * .8f + side * 1.6f, benches % 2 == 0); Mark(p, 2.2f); benches++; }
            }
            Debug.Log($"Mount Virtuoso: the climb {climb.Count} steps, mountain {Hm:0} m high, {birches} birch groves, {boulders} mossy boulders, {cairns} cairns, {heather} heather patches, {cabins} cabins, {benches} benches, {crags} crags and dead trees");
        }

        Vector3 NearestWalk(Vector3 p) { var best = center; var bd = float.MaxValue; foreach (var (q, _) in walkPoints) { var d = (q - p).sqrMagnitude; if (d < bd) { bd = d; best = q; } } return best; }

        /// Moor grass, silver-green, thinning out up the mountain.
        void Verge(LandContext ctx)
        {
            var mat = WindMesh.Material(.4f, .8f, new Color(.4f, .5f, .38f));
            var meshes = new Dictionary<(int, int), WindMesh>(); var rng = new System.Random(1991);
            const float st = 2.4f;
            for (var x = ctx.MinX; x < ctx.MaxX; x += st)
            for (var z = ctx.MinZ; z < ctx.MaxZ; z += st)
            {
                var px = x + ((float)rng.NextDouble() - .5f) * st; var pz = z + ((float)rng.NextDouble() - .5f) * st;
                if (!ctx.IsLand(px, pz, 1f) || NearPath(px, pz, 3.6f) || InNoGrass(px, pz, 0f) || InFootprint(ctx, px, pz, -2f)) continue;
                var gy = ctx.Ground(px, pz); if (gy > snowLine - 10f) continue; if (gy > baseY + Hm * .3f && rng.NextDouble() < .6f) continue;
                if (Mathf.PerlinNoise(px * .04f + 3f, pz * .04f + 4f) < .35f) continue;
                var key = (Mathf.FloorToInt(px / 70f), Mathf.FloorToInt(pz / 70f));
                if (!meshes.TryGetValue(key, out var wm)) meshes[key] = wm = new WindMesh();
                var col = Color.Lerp(new Color(.38f, .48f, .36f), new Color(.5f, .54f, .42f), (float)rng.NextDouble());
                wm.Tuft(new Vector3(px, gy - .04f, pz), .3f + (float)rng.NextDouble() * .4f, col, Color.Lerp(col, Snow, .4f), rng, 3, .08f);
            }
            foreach (var kv in meshes) kv.Value.Build("Virt grass", ctx.Root, mat);
        }
    }
}
