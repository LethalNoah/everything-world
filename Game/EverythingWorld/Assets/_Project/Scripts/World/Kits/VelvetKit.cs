using System.Collections.Generic;
using UnityEngine;
using static EverythingWorld.World.PlaceTools;

namespace EverythingWorld.World
{
    /// The Velvet Rooftops (WORLD_GUIDE.md): a drowned city, late at night. The island is the tops of towers standing
    /// out of a dark, still sea; you walk from roof to roof over lit glass bridges, and all round more towers rise out
    /// of the water with their windows lit. You arrive beside the Pool, glowing turquoise on the tallest roof, under a
    /// tall VELVET sign: loungers, a bar, palms in pots. Out on the roofs: water towers, skylights glowing from below,
    /// air-conditioners, antenna masts with a red light on top, neon billboards, roof gardens, a helipad.
    /// Late and lush: deep red, velvet purple, gold, night blue.
    /// Laid out on purpose (the build-land standard): VelvetTown.cs has the places and the people.
    public partial class VelvetKit : LandKit
    {
        public override string Land => "The Velvet Rooftops";
        public override Vector3 ArtistFront(IslandWorldData data, IslandWorldData.Artist a) => facing.TryGetValue(a.name, out var f) ? f : base.ArtistFront(data, a);

        public static readonly Color Rose = new Color(1f, .25f, .45f), Violet = new Color(.6f, .3f, 1f), Gold = new Color(1f, .78f, .3f), Aqua = new Color(.2f, .9f, .9f), Cream = new Color(.97f, .93f, .86f), Ink = new Color(.07f, .06f, .12f), Wine = new Color(.5f, .08f, .16f);
        static readonly Color[] Neons = { new Color(1f, .25f, .45f), new Color(.6f, .3f, 1f), new Color(1f, .78f, .3f), new Color(.2f, .9f, .9f), new Color(1f, .5f, .2f) };
        const float PathW = 3f;

        Material roofM, deckM, glassWalk, concrete, steel, dark, white, velvet, woodM, leafM, trunkM, poolM, windowWarm, windowCool, windowRose, beaconM, skylightM, lampM;
        readonly Material[] neonM = new Material[5];
        GlowHalos halos; Vector3 center, U, R; VelvetLife life;
        readonly Dictionary<(int, int), List<Vector3>> pathCells = new Dictionary<(int, int), List<Vector3>>();
        readonly List<(Vector3 p, int w)> walkPoints = new List<(Vector3, int)>();
        readonly Dictionary<string, Vector3> facing = new Dictionary<string, Vector3>();
        // x, radius, z
        readonly List<Vector3> noGrass = new List<Vector3>();
        Vector2 heart2; float plazaY, waterY;

        void MakeMaterials()
        {
            // the roofs and decks are faintly self-lit, so the night never swallows them
            roofM = WorldMaterials.Glow(new Color(.2f, .2f, .3f), .16f); deckM = WorldMaterials.Glow(new Color(.36f, .26f, .3f), .18f); concrete = WorldMaterials.Glow(new Color(.42f, .42f, .5f), .12f);
            glassWalk = new Material(WorldMaterials.Glow(new Color(.26f, .2f, .55f), .5f)) { name = "Velvet bridges" };
            steel = WorldMaterials.Solid(new Color(.5f, .52f, .6f), .8f); dark = WorldMaterials.Solid(new Color(.06f, .06f, .1f)); white = WorldMaterials.Glow(new Color(.95f, .93f, .9f), .14f); velvet = WorldMaterials.Glow(Wine, .2f);
            woodM = WorldMaterials.Glow(new Color(.4f, .28f, .22f), .1f); leafM = WorldMaterials.Glow(new Color(.14f, .42f, .3f), .14f); trunkM = WorldMaterials.Solid(new Color(.3f, .24f, .22f));
            poolM = new Material(WorldMaterials.Glow(new Color(.15f, .85f, .9f), 1.3f)) { name = "Velvet pool" };
            windowWarm = new Material(WorldMaterials.Glow(new Color(1f, .8f, .45f), 1.1f)) { name = "Velvet windows warm" }; windowCool = new Material(WorldMaterials.Glow(new Color(.55f, .75f, 1f), 1f)) { name = "Velvet windows cool" }; windowRose = new Material(WorldMaterials.Glow(new Color(1f, .4f, .6f), 1f)) { name = "Velvet windows rose" };
            beaconM = new Material(WorldMaterials.Glow(new Color(1f, .1f, .1f), 2.4f)) { name = "Velvet beacons" }; skylightM = new Material(WorldMaterials.Glow(new Color(.8f, .9f, 1f), .9f)) { name = "Velvet skylights" }; lampM = new Material(WorldMaterials.Glow(Gold, 1.6f)) { name = "Velvet lamps" };
            for (var i = 0; i < 5; i++) neonM[i] = new Material(WorldMaterials.Glow(Neons[i], 2.2f)) { name = "Velvet neon " + i };
        }

        Material GlowM(Color c, float s = 2f) => WorldMaterials.Glow(c, s * .65f);
        Material Paint(Color c) => WorldMaterials.Glow(c, .14f);
        static bool HasStation(IslandWorldData.Artist a) => a.stationId != 0;
        static float Foot(IslandWorldData.Artist a)
        {
            switch (a.name)
            {
                case "Rihanna": case "Frank Ocean": return 14f;
                case "The Weeknd": case "SZA": return 12f;
                default: return HasStation(a) ? 12f : 9f;
            }
        }

        public override bool Ground(float height, float slope, float patch, out Color color)
        {
            // roofing: blue-black tar with patches of pale gravel
            if (slope > 26f) color = Color.Lerp(new Color(.1f, .1f, .16f), new Color(.14f, .13f, .2f), patch);
            else color = patch > .6f ? Color.Lerp(new Color(.26f, .26f, .34f), new Color(.3f, .29f, .38f), patch) : Color.Lerp(new Color(.15f, .15f, .23f), new Color(.2f, .19f, .28f), Mathf.SmoothStep(0f, 1f, patch / .6f));
            color.a = 1f;
            return true;
        }

        static float Ease(float a, float b, float x) { var t = Mathf.Clamp01((x - a) / (b - a)); return t * t * (3f - 2f * t); }
        Vector2 P2(float u, float r) => heart2 + new Vector2(r, u);
        Vector3 Wp(float u, float r) => center + U * u + R * r;
        Vector3 G(LandContext ctx, float u, float r) { var p = Wp(u, r); return ctx.OnGround(p.x, p.z); }

        // ---------------------------------------------------------------- shaping the ground
        public override void ShapeGround(IslandWorldData data, int index)
        {
            var isl = data.layout.islands[index]; heart2 = new Vector2(isl.labelX, isl.labelZ); waterY = data.layout.waterLevel;
            bool Inside(Vector2 p, float m) { if (data.IslandAt(p.x, p.y) != isl) return false; for (var k = 0; k < 12; k++) { var a = k * Mathf.PI / 6f; if (data.IslandAt(p.x + Mathf.Cos(a) * m, p.y + Mathf.Sin(a) * m) != isl) return false; } return true; }
            plazaY = Mathf.Max(data.HeightAt(heart2.x, heart2.y), waterY + 6f);
            var keep = new List<string>();
            void Put(string name, float u, float rr, float room) { var p = P2(u, rr); if (!Inside(p, room)) return; foreach (var a in data.layout.artists) if (a.sceneId == isl.sceneId && a.name == name) { a.x = p.x; a.z = p.y; keep.Add(name); } }
            // the four big roofs round the Pool; the Punjabi terrace together, east
            Put("Rihanna", 52f, 30f, 28f); Put("Frank Ocean", -52f, 28f, 28f); Put("The Weeknd", 16f, -58f, 26f); Put("SZA", -44f, -48f, 26f);
            Put("Diljit Dosanjh", 6f, 98f, 24f); Put("Karan Aujla", 44f, 118f, 22f); Put("Prabh Singh", -34f, 120f, 22f);
            ArtistPlacement.Spread(data, index, 54f, heart2, 44f, 42f, new List<Vector3>(), keep, Land, 330f);
            Level(data, index, heart2, 24f, 42f, plazaY);
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
            center = ctx.Plaza; U = new Vector3(0, 0, 1); R = new Vector3(1, 0, 0);
            life = new GameObject("Velvet life").AddComponent<VelvetLife>();
            life.transform.SetParent(ctx.Root, false);
            life.center = center; life.neon = neonM; life.pool = poolM; life.beacons = beaconM; life.bridges = glassWalk; life.windows = new[] { windowWarm, windowCool, windowRose };
            var mk = new MeshKit(); var solid = new MeshKit();
            Remember(ctx.OnGround(center.x, center.z), 0);
            TownStart(ctx);
            PoolRoof(ctx, mk, solid);
            Paths(ctx, mk, solid);
            Wilds(ctx, mk, solid);
            Skyline(ctx, mk);
            mk.Build("The Velvet Rooftops", ctx.Root, false);
            solid.BuildCollider("The Velvet Rooftops walls", ctx.Root);
        }

        public override void Finish(LandContext ctx) { halos.Build("Velvet glow", ctx.Root); }

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

        /// A slim gold lamp on a post.
        void Lamp(LandContext ctx, MeshKit mk, Vector3 g, bool light = false)
        {
            var up = Vector3.up; mk.Prism(steel, g - up * .2f, up, .05f, 2.8f, 5); mk.Prism(lampM, g + up * 2.6f, up, .12f, .5f, 8); mk.Prism(steel, g + up * 3.1f, up, .16f, .04f, 8); halos.Add(g + up * 2.85f, 1.6f, Gold, .5f); ArtistBits.PostCollider(ctx.Root, g, 2.8f, .08f);
            if (light) ArtistBits.PointLight(ctx.Root, g + up * 2.6f, new Color(1f, .8f, .55f), 15f, 2.2f);
        }

        /// A sun lounger to lie on.
        void Lounger(MeshKit mk, Vector3 g, Vector3 face, Material cushion)
        {
            var up = Vector3.up; var rot = Quaternion.LookRotation(face); mk.Box(white, g + up * .3f, new Vector3(.7f, .08f, 1.9f), rot); mk.Box(cushion, g + up * .36f, new Vector3(.62f, .06f, 1.3f), rot); mk.Box(cushion, g + up * .58f - face * .75f, new Vector3(.62f, .06f, .7f), rot * Quaternion.Euler(-40f, 0, 0)); foreach (var e in new[] { -.7f, .7f }) mk.Box(white, g + up * .15f + face * e, new Vector3(.6f, .3f, .06f), rot);
            BoxCol(rootT, g + up * .25f, new Vector3(.7f, .5f, 1.9f), rot); Interact.Seat(tlife.transform, g + up * .44f, face, "Lie on the lounger").seatHeight = .4f;
        }

        /// A low velvet sofa to sit on.
        void Sofa(Transform parent, MeshKit mk, Vector3 g, Vector3 face, Material m, float len = 2.4f)
        {
            var up = Vector3.up; var rot = Quaternion.LookRotation(face); var side = Vector3.Cross(up, face); mk.Box(m, g + up * .28f, new Vector3(len, .4f, .9f), rot); mk.Box(m, g + up * .66f - face * .36f, new Vector3(len, .6f, .2f), rot); foreach (var e in new[] { -1f, 1f }) mk.Box(m, g + up * .46f + side * e * (len / 2f - .1f), new Vector3(.2f, .46f, .9f), rot);
            BoxCol(parent, g + up * .45f, new Vector3(len, .9f, .9f), rot); Interact.Bench(tlife.transform, g + up * .52f - side * (len / 2f - .6f) + face * .1f, g + up * .52f + side * (len / 2f - .6f) + face * .1f, face);
        }

        /// A potted palm.
        void PotPalm(LandContext ctx, MeshKit mk, Vector3 g, float s, System.Random rng)
        {
            var up = Vector3.up; mk.Prism(concrete, g, up, .36f * s, .6f * s, 8, 1.2f); mk.Prism(trunkM, g + up * .5f * s, up, .08f * s, 1.6f * s, 5, .8f); for (var k = 0; k < 7; k++) { var fd = Quaternion.Euler(0, k * 51f + (float)rng.NextDouble() * 20f, 0) * Vector3.forward; mk.Box(leafM, g + up * 2.2f * s + fd * .7f * s, new Vector3(.3f * s, .04f, 1.5f * s), Quaternion.LookRotation(fd) * Quaternion.Euler(24f, 0, 0)); }
            ArtistBits.PostCollider(ctx.Root, g, 1f * s, .4f * s);
        }

        /// A neon word on a frame.
        void NeonSign(Transform labels, MeshKit mk, Vector3 c, Vector3 face, string text, float size, int colour, float w, float h, bool posts = true)
        {
            var rot = Quaternion.LookRotation(face); var side = rot * Vector3.right; mk.Box(dark, c - face * .04f, new Vector3(w, h, .06f), rot); mk.Box(neonM[colour % 5], c - Vector3.up * (h / 2f - .06f) + face * .01f, new Vector3(w - .2f, .04f, .03f), rot); mk.Box(neonM[colour % 5], c + Vector3.up * (h / 2f - .06f) + face * .01f, new Vector3(w - .2f, .04f, .03f), rot);
            ArtistBits.Label(labels, text, c + face * .02f, face, size, Neons[colour % 5], TextAnchor.MiddleCenter); halos.Add(c + face * .6f, Mathf.Max(w, h) * .6f, Neons[colour % 5], .4f);
            if (posts) foreach (var e in new[] { -1f, 1f }) mk.Prism(steel, c + side * e * (w / 2f - .2f) - Vector3.up * (h / 2f + 2.2f) - face * .1f, Vector3.up, .05f, 2.2f, 4);
        }

        // ---------------------------------------------------------------- the sky bridges to the artists
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
                // a walk of lit glass with a darker edge, and a row of small lights down each side
                Strip(mk, pts, dark, PathW + .5f, .08f, ctx); Strip(mk, pts, glassWalk, PathW - .4f, .15f, ctx);
                foreach (var p in pts) Remember(p, walk);
                for (var i = 1; i < pts.Count; i += 3, n++) foreach (var e in new[] { -1f, 1f }) { var lp = pts[i] + side * e * (PathW / 2f + .1f); lp.y = ctx.Ground(lp.x, lp.z); mk.Prism(steel, lp, Vector3.up, .03f, .5f, 4); mk.Blob(neonM[(n / 6) % 5], lp + Vector3.up * .54f, Vector3.one * .06f, new System.Random(n), 0f); }
                for (var i = 6; i < pts.Count; i += 14) { var lp = pts[i] + side * (PathW / 2f + 1f); if (InFootprint(ctx, lp.x, lp.z, 1f) || InNoGrass(lp.x, lp.z, 1f)) continue; Lamp(ctx, mk, ctx.OnGround(lp.x, lp.z), true); }
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

        // ---------------------------------------------------------------- out on the roofs
        /// Water towers, skylights, air-conditioners, antenna masts, stair huts, roof gardens, billboards, a helipad.
        void Wilds(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            int tanks = 0, lights = 0, units = 0, masts = 0, huts = 0, gardens = 0, boards = 0, pads = 0; var rng = new System.Random(2016);
            const float cell = 25f;
            for (var x = ctx.MinX; x < ctx.MaxX; x += cell)
            for (var z = ctx.MinZ; z < ctx.MaxZ; z += cell)
            {
                float px = x + (float)rng.NextDouble() * cell, pz = z + (float)rng.NextDouble() * cell; var roll = (float)rng.NextDouble(); var s = (float)rng.NextDouble();
                if (!Open(ctx, px, pz, 4f)) continue;
                var p = ctx.OnGround(px, pz); var rot = Quaternion.Euler(0, Mathf.Round(s * 4f) * 90f + 12f, 0);
                if (roll < .09f) { WaterTower(ctx, mk, p, 1f + s * .4f, tanks); tanks++; }
                else if (roll < .22f) { Skylight(ctx, mk, p, rot, 2.4f + s * 2f); lights++; }
                else if (roll < .36f) { for (var k = 0; k < 2 + (int)(s * 3f); k++) { var q = p + rot * new Vector3(k * 1.5f, 0, (k % 2) * .4f); q.y = ctx.Ground(q.x, q.z); mk.Box(concrete, q + Vector3.up * .5f, new Vector3(1.2f, 1f, 1.2f), rot); mk.Prism(dark, q + Vector3.up * 1f, Vector3.up, .42f, .06f, 10); BoxCol(ctx.Root, q + Vector3.up * .5f, new Vector3(1.2f, 1f, 1.2f), rot); } units++; }
                else if (roll < .42f) { Mast(ctx, mk, p, 9f + s * 9f); masts++; }
                else if (roll < .5f) { StairHut(ctx, mk, solid, p, rot, huts); huts++; }
                else if (roll < .62f) { Garden(ctx, mk, p, rot, rng); gardens++; }
                else if (roll < .67f) { if (boards < 9) { Billboard(ctx, mk, p, FaceTo(p, NearestWalk(p)), boards); boards++; } }
                else if (roll < .69f) { if (pads < 2) { var (plo, phi) = Under(ctx, p, 7f); p.y = phi; if (phi - plo > .05f) mk.Prism(concrete, new Vector3(p.x, plo - .2f, p.z), Vector3.up, 7f, phi - plo + .25f, 24); mk.Prism(concrete, p + Vector3.up * .05f, Vector3.up, 7f, .06f, 24); mk.Prism(neonM[2], p + Vector3.up * .11f, Vector3.up, 6.6f, .01f, 24); mk.Prism(concrete, p + Vector3.up * .115f, Vector3.up, 6.2f, .01f, 24); foreach (var e in new[] { -1.2f, 1.2f }) mk.Box(white, p + Vector3.up * .13f + Vector3.right * e, new Vector3(.5f, .01f, 4f)); mk.Box(white, p + Vector3.up * .13f, new Vector3(2.4f, .01f, .5f)); noGrass.Add(new Vector3(p.x, 8f, p.z)); pads++; } }
            }
            Debug.Log($"The Velvet Rooftops: {tanks} water towers, {lights} skylights, {units} air-conditioner banks, {masts} masts, {huts} stair huts, {gardens} roof gardens, {boards} billboards, {pads} helipads");
        }

        /// The lowest and highest ground under a round footprint, so a roof thing can reach down to the ground on a slope.
        static (float lo, float hi) Under(LandContext ctx, Vector3 g, float r)
        {
            float lo = ctx.Ground(g.x, g.z), hi = lo;
            for (var ring = 1; ring <= 2; ring++) for (var k = 0; k < 8; k++) { var a = k * Mathf.PI / 4f; var h = ctx.Ground(g.x + Mathf.Cos(a) * r * ring / 2f, g.z + Mathf.Sin(a) * r * ring / 2f); lo = Mathf.Min(lo, h); hi = Mathf.Max(hi, h); }
            return (lo, hi);
        }

        Vector3 NearestWalk(Vector3 p) { var best = center; var bd = float.MaxValue; foreach (var (q, _) in walkPoints) { var d = (q - p).sqrMagnitude; if (d < bd) { bd = d; best = q; } } return best; }

        void WaterTower(LandContext ctx, MeshKit mk, Vector3 g, float s, int i)
        {
            var up = Vector3.up; foreach (var (x, zz) in new[] { (-1f, -1f), (1f, -1f), (1f, 1f), (-1f, 1f) }) { var lg = g + new Vector3(x * 1.1f * s, 0, zz * 1.1f * s); var gy = ctx.Ground(lg.x, lg.z); mk.Prism(steel, new Vector3(lg.x, gy - .3f, lg.z), up, .07f, g.y + 3.2f * s - gy, 4); ArtistBits.PostCollider(ctx.Root, g + new Vector3(x * 1.1f * s, 0, zz * 1.1f * s), 3f * s, .1f); }
            mk.Prism(woodM, g + up * 2.9f * s, up, 1.6f * s, 2.6f * s, 12); mk.Cone(dark, g + up * 5.5f * s, 1.8f * s, 1.1f * s, 12); for (var k = 0; k < 3; k++) mk.Prism(steel, g + up * (3.3f + k * .9f) * s, up, 1.63f * s, .07f, 12); if (i % 2 == 0) mk.Blob(beaconM, g + up * 6.7f * s, Vector3.one * .12f, new System.Random(i), 0f); noGrass.Add(new Vector3(g.x, 2.4f * s, g.z));
        }

        void Skylight(LandContext ctx, MeshKit mk, Vector3 g, Quaternion rot, float w)
        {
            var up = Vector3.up; var lo = Under(ctx, g, w * .55f + .2f).lo; var curb = g.y + .4f - (lo - .1f); mk.Box(concrete, new Vector3(g.x, g.y + .4f - curb / 2f, g.z), new Vector3(w + .3f, curb, 1.9f), rot); mk.GableRoof(skylightM, g + up * .4f, new Vector3(1.6f, 0, w), .5f, rot * Quaternion.Euler(0, 90f, 0)); BoxCol(ctx.Root, g + up * .4f, new Vector3(w + .3f, .9f, 1.9f), rot); halos.Add(g + up * 1f, w * .5f, new Color(.7f, .85f, 1f), .22f); noGrass.Add(new Vector3(g.x, w * .7f, g.z));
        }

        void Mast(LandContext ctx, MeshKit mk, Vector3 g, float h)
        {
            var up = Vector3.up; mk.Prism(steel, g - up * .3f, up, .12f, h, 5, .3f); for (var k = 1; k < 4; k++) mk.Box(steel, g + up * h * k / 4f, new Vector3(1.4f - k * .3f, .04f, .04f), Quaternion.Euler(0, k * 50f, 0)); mk.Blob(beaconM, g + up * (h - .2f), Vector3.one * .16f, new System.Random(3), 0f); halos.Add(g + up * (h - .2f), 1.4f, new Color(1f, .15f, .15f), .6f); ArtistBits.PostCollider(ctx.Root, g, 4f, .14f);
        }

        void StairHut(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 g, Quaternion rot, int i)
        {
            var up = Vector3.up; var f = rot * Vector3.forward; var hh = g.y + 2.6f - (Under(ctx, g, 2.2f).lo - .2f); var hc = new Vector3(g.x, g.y + 2.6f - hh / 2f, g.z); mk.Box(concrete, hc, new Vector3(2.6f, hh, 3.4f), rot); solid.Box(concrete, hc, new Vector3(2.6f, hh, 3.4f), rot); mk.Box(dark, g + up * 2.66f, new Vector3(2.9f, .14f, 3.7f), rot * Quaternion.Euler(8f, 0, 0)); mk.Box(i % 2 == 0 ? (Material)windowWarm : dark, g + up * 1.1f + f * 1.71f, new Vector3(1f, 2.1f, .04f), rot); mk.Blob(lampM, g + up * 2.4f + f * 1.8f, Vector3.one * .09f, new System.Random(i), 0f); halos.Add(g + up * 2.2f + f * 2.2f, 1.2f, Gold, .4f); noGrass.Add(new Vector3(g.x, 3f, g.z));
        }

        void Garden(LandContext ctx, MeshKit mk, Vector3 g, Quaternion rot, System.Random rng)
        {
            var up = Vector3.up; var ph = g.y + .5f - (Under(ctx, g, 2f).lo - .1f); mk.Box(woodM, new Vector3(g.x, g.y + .5f - ph / 2f, g.z), new Vector3(3.6f, ph, 1.6f), rot); BoxCol(ctx.Root, g + up * .25f, new Vector3(3.6f, .5f, 1.6f), rot); for (var k = 0; k < 5; k++) { var q = g + rot * new Vector3(-1.4f + k * .7f, .5f, ((float)rng.NextDouble() - .5f) * .7f); mk.Blob(leafM, q + up * .3f, new Vector3(.5f, .44f, .5f), rng, .3f); if (k % 2 == 0) mk.Blob(GlowM(Neons[rng.Next(3)], .6f), q + up * .7f, Vector3.one * .12f, rng, .2f); }
            if (rng.Next(3) == 0) PotPalm(ctx, mk, g + rot * new Vector3(2.4f, 0, 0), 1.2f, rng); noGrass.Add(new Vector3(g.x, 2.4f, g.z));
        }

        void Billboard(LandContext ctx, MeshKit mk, Vector3 g, Vector3 face, int i)
        {
            var up = Vector3.up; string[] words = { "SLOW\nDOWN", "LATE\nCHECK-OUT", "ROOM\nSERVICE", "ONE\nMORE", "STAY", "AFTER\nHOURS", "TOP\nFLOOR", "NO\nSLEEP", "OPEN\nLATE" }; var side = Vector3.Cross(up, face);
            foreach (var e in new[] { -2f, 2f }) { var bp = g + side * e; var by = ctx.Ground(bp.x, bp.z); mk.Prism(steel, new Vector3(bp.x, by - .3f, bp.z), up, .1f, g.y + 6.7f - by + .3f, 5); ArtistBits.PostCollider(ctx.Root, g + side * e, 4f, .12f); } NeonSign(rootT, mk, g + up * 5.2f, face, words[i % words.Length], .7f, i, 5.4f, 2.8f, false); noGrass.Add(new Vector3(g.x, 3f, g.z));
        }

        /// The drowned city: towers standing out of the sea all round the island, their windows lit in bands.
        void Skyline(LandContext ctx, MeshKit mk)
        {
            var rng = new System.Random(2020); var n = 0; var body = WorldMaterials.Solid(new Color(.07f, .07f, .12f));
            for (var k = 0; k < 260 && n < 64; k++)
            {
                var a = (float)rng.NextDouble() * Mathf.PI * 2f; var d = 300f + (float)rng.NextDouble() * 420f; var x = center.x + Mathf.Cos(a) * d; var z = center.z + Mathf.Sin(a) * d;
                var clear = true; for (var q = 0; q < 8 && clear; q++) if (ctx.Data.IslandAt(x + Mathf.Cos(q * .785f) * 34f, z + Mathf.Sin(q * .785f) * 34f) != null) clear = false; if (!clear || ctx.Data.IslandAt(x, z) != null) continue;
                var w = 16f + (float)rng.NextDouble() * 22f; var dp = 16f + (float)rng.NextDouble() * 22f; var h = 24f + (float)rng.NextDouble() * (d > 500f ? 130f : 70f); var rot = Quaternion.Euler(0, (float)rng.NextDouble() * 90f, 0); var b = new Vector3(x, waterY, z);
                mk.Box(body, b + Vector3.up * (h / 2f - 4f), new Vector3(w, h + 8f, dp), rot); var win = n % 3 == 0 ? windowCool : n % 5 == 0 ? windowRose : windowWarm;
                for (var y = 4f; y < h - 2f; y += 5f) if (rng.NextDouble() < .62) mk.Box(win, b + Vector3.up * y, new Vector3(w + .3f, 1.4f, dp + .3f), rot);
                if (h > 70f) { mk.Prism(body, b + Vector3.up * h, Vector3.up, .5f, 12f, 4, .2f); mk.Blob(beaconM, b + Vector3.up * (h + 12f), Vector3.one * .9f, rng, 0f); }
                n++;
            }
            Debug.Log($"The Velvet Rooftops: {n} drowned towers round the island");
        }
    }
}
