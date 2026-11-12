using System.Collections.Generic;
using UnityEngine;

namespace EverythingWorld.World
{
    /// The Power Metal Peaks (WORLD_GUIDE.md): over-the-top fun, epic and fierce all at once. Dark fantasy mountains,
    /// sharpened into jagged snow-capped peaks, under a blazing sunset. On the highest peak a castle with a dragon
    /// coiled round its keep (it breathes fire in "Through the Fire and Flames"); below, on the coast, a stormy pirate
    /// cove with a ship at anchor, a pier and a tavern (and a giant rubber duck). Paths are lit by torches; the slopes
    /// carry dark pines, war banners, swords in stones, treasure chests, braziers, ruined watchtowers, and here and there
    /// a pop of kawaii neon, stone lanterns and cherry trees, because half the artists are Japanese metal bands.
    /// Reads by dark silhouettes against a fiery orange sky (the Djent Spires next door is pale ice in daylight).
    /// Files: PowerKit (ground, arrival, castle, cove, paths, wilds), PowerArtists, PowerSongIds, PowerLife.
    public partial class PowerKit : LandKit
    {
        public override string Land => "The Power Metal Peaks";
        public override Vector3 ArtistFront(IslandWorldData data, IslandWorldData.Artist a) => facing.TryGetValue(a.name, out var f) ? f : base.ArtistFront(data, a);

        public static readonly Color Fire = new Color(1f, .5f, .12f), Ember = new Color(1f, .3f, .08f), Gold = new Color(1f, .78f, .3f), Pink = new Color(1f, .4f, .75f), NeonCyan = new Color(.3f, .9f, 1f), Ink = new Color(1f, .92f, .78f), Royal = new Color(.45f, .2f, .7f);
        const float PathW = 3f;

        Material rock, rockDark, wallStone, wallDark, wood, woodDark, goldM, iron, snow, bannerRed, bannerPurple, fireM, emberM, lanternM, neonPink, neonCyan, cloth;
        GlowHalos halos; Vector3 center; PowerLife life;
        readonly Dictionary<(int, int), List<Vector3>> pathCells = new Dictionary<(int, int), List<Vector3>>();
        readonly List<(Vector3 p, int w)> walkPoints = new List<(Vector3, int)>();
        readonly Dictionary<string, Vector3> facing = new Dictionary<string, Vector3>();
        readonly List<Vector3> noGrass = new List<Vector3>();
        readonly List<Vector3> peaks = new List<Vector3>();   // x, R, z
        Vector2 heart2, castle2, cove2, seaDir2; bool hasCastle, hasCove;
        const float CastleR = 24f;

        void MakeMaterials()
        {
            rock = WorldMaterials.Solid(new Color(.3f, .27f, .3f)); rockDark = WorldMaterials.Solid(new Color(.16f, .14f, .17f));
            wallStone = WorldMaterials.Solid(new Color(.38f, .34f, .36f)); wallDark = WorldMaterials.Solid(new Color(.22f, .19f, .21f));
            wood = WorldMaterials.Solid(new Color(.4f, .27f, .18f)); woodDark = WorldMaterials.Solid(new Color(.22f, .15f, .11f));
            goldM = WorldMaterials.Solid(new Color(.95f, .72f, .25f), .85f); iron = WorldMaterials.Solid(new Color(.25f, .25f, .28f), .6f);
            snow = WorldMaterials.Solid(new Color(.95f, .86f, .86f));
            bannerRed = WorldMaterials.Solid(new Color(.65f, .1f, .12f)); bannerPurple = WorldMaterials.Solid(new Color(.35f, .15f, .55f));
            cloth = WorldMaterials.Solid(new Color(.88f, .84f, .74f));
            fireM = new Material(WorldMaterials.Glow(Fire, 2.2f)) { name = "Fire" };
            emberM = new Material(WorldMaterials.Glow(Ember, 1.6f)) { name = "Embers" };
            lanternM = new Material(WorldMaterials.Glow(new Color(1f, .72f, .4f), 1.8f)) { name = "Lantern" };
            neonPink = new Material(WorldMaterials.Glow(Pink, 2f)) { name = "Kawaii neon pink" };
            neonCyan = new Material(WorldMaterials.Glow(NeonCyan, 2f)) { name = "Kawaii neon cyan" };
        }

        Material GlowM(Color c, float s = 2f) => WorldMaterials.Glow(c, s * .65f);
        static bool HasStation(IslandWorldData.Artist a) => a.stationId != 0;
        static float Foot(IslandWorldData.Artist a)
        {
            switch (a.name)
            {
                case "BRIDEAR": return 17f;
                case "DragonForce": return 15f;
                case "Alestorm": case "Wind Rose": return 11f;
                case "HANABIE.": case "LADYBABY": case "Mary's Blood": case "NEMOPHILA": case "BABYMETAL": return 10f;
                default: return HasStation(a) ? 13f : 8f;
            }
        }

        public override bool Ground(float height, float slope, float patch, out Color color)
        {
            if (height < 2.6f) color = Color.Lerp(new Color(.55f, .48f, .4f), new Color(.62f, .55f, .45f), patch);                       // the shore
            else if (height > 112f && slope < 34f) color = Color.Lerp(new Color(.85f, .76f, .78f), new Color(.95f, .86f, .86f), patch);    // snow, pink in the sunset
            else if (slope > 30f) color = Color.Lerp(new Color(.2f, .18f, .21f), new Color(.3f, .27f, .3f), patch);                        // dark crags
            else if (slope > 15f) color = Color.Lerp(new Color(.26f, .25f, .24f), new Color(.34f, .31f, .3f), patch);
            else color = Color.Lerp(new Color(.17f, .22f, .17f), new Color(.26f, .3f, .2f), Mathf.SmoothStep(0f, 1f, patch));            // dark pine meadow
            color.a = 1f;
            return true;
        }

        static void BoxCol(Transform parent, Vector3 center, Vector3 size, Quaternion rot)
        {
            var go = new GameObject("Box collider"); go.transform.SetParent(parent, false); go.transform.SetPositionAndRotation(center, rot);
            go.AddComponent<BoxCollider>().size = size; go.isStatic = true;
        }

        static float Ease(float a, float b, float x) { var t = Mathf.Clamp01((x - a) / (b - a)); return t * t * (3f - 2f * t); }

        // ---------------------------------------------------------------- shaping the ground
        public override void ShapeGround(IslandWorldData data, int index)
        {
            var isl = data.layout.islands[index]; heart2 = new Vector2(isl.labelX, isl.labelZ); peaks.Clear();
            bool Inside(Vector2 p, float m) { if (data.IslandAt(p.x, p.y) != isl) return false; for (var k = 0; k < 16; k++) { var a = k * Mathf.PI / 8f; if (data.IslandAt(p.x + Mathf.Cos(a) * m, p.y + Mathf.Sin(a) * m) != isl) return false; } return true; }
            // the castle: the highest ground well inside the land; then two more summits sharpened into jagged peaks
            var cands = new List<(Vector2 p, float h)>();
            for (var r = 0; r < data.Nz; r += 2)
            {
                var z = data.layout.depth - r * data.Step; if (Mathf.Abs(z - heart2.y) > 460f) continue;
                for (var col = 0; col < data.Nx; col += 2)
                {
                    var x = col * data.Step; if (Mathf.Abs(x - heart2.x) > 460f) continue;
                    var i = r * data.Nx + col; if (data.islandIds[i] != index) continue;
                    var d = Vector2.Distance(new Vector2(x, z), heart2); if (d < 110f || d > 400f) continue;
                    cands.Add((new Vector2(x, z), data.heights[i]));
                }
            }
            cands.Sort((a, b) => b.h.CompareTo(a.h));
            hasCastle = false;
            foreach (var (p, h) in cands) if (Inside(p, 52f)) { castle2 = p; hasCastle = true; break; }
            foreach (var (p, h) in cands)
            {
                if (peaks.Count >= 3) break;
                if (hasCastle && Vector2.Distance(p, castle2) < 150f) continue;
                var far = true; foreach (var v in peaks) if (Vector2.Distance(p, new Vector2(v.x, v.z)) < 140f) far = false;
                if (!far) continue;
                for (var R = 60f; R >= 40f; R -= 10f) if (Inside(p, R)) { peaks.Add(new Vector3(p.x, R, p.y)); break; }
            }
            foreach (var v in peaks)
            {
                var R = v.y; var H = R * .6f;
                for (var r = 0; r < data.Nz; r++)
                {
                    var z = data.layout.depth - r * data.Step; if (Mathf.Abs(z - v.z) > R) continue;
                    for (var col = 0; col < data.Nx; col++)
                    {
                        var x = col * data.Step; if (Mathf.Abs(x - v.x) > R) continue;
                        var i = r * data.Nx + col; if (data.islandIds[i] != index) continue;
                        var ang = Mathf.Atan2(z - v.z, x - v.x); var d = Vector2.Distance(new Vector2(x, z), new Vector2(v.x, v.z)) / (R * (1f + .18f * Mathf.Sin(ang * 3f + v.x)));
                        if (d >= 1f) continue;
                        data.heights[i] += H * Mathf.Pow(1f - d, 1.6f);
                    }
                }
            }
            // the cove: the nearest stretch of low coast, with the open sea beyond
            hasCove = false; var bestD = float.MaxValue;
            for (var r = 3; r < data.Nz - 3; r++)
            {
                var z = data.layout.depth - r * data.Step; if (Mathf.Abs(z - heart2.y) > 420f) continue;
                for (var col = 3; col < data.Nx - 3; col++)
                {
                    var x = col * data.Step; if (Mathf.Abs(x - heart2.x) > 420f) continue;
                    var i = r * data.Nx + col; if (data.islandIds[i] != index || data.heights[i] > 6f) continue;
                    var d = Vector2.Distance(new Vector2(x, z), heart2); if (d < 110f || d >= bestD) continue;
                    // open sea: deep water a few cells away in some direction, land behind
                    for (var k = 0; k < 8; k++)
                    {
                        var a = k * Mathf.PI / 4f; var dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                        var sea = new Vector2(x, z) + dir * 40f; var back = new Vector2(x, z) - dir * 40f;
                        if (data.HeightAt(sea.x, sea.y) < data.layout.waterLevel - 1.5f && data.HeightAt(x + dir.x * 20f, z + dir.y * 20f) < 1.6f && data.IslandAt(back.x, back.y) == isl && data.IslandAt(x - dir.x * 16f, z - dir.y * 16f) == isl)
                        { bestD = d; cove2 = new Vector2(x, z); seaDir2 = dir; hasCove = true; break; }
                    }
                }
            }
            var keep = new List<string>();
            if (hasCove)
                foreach (var a in data.layout.artists)
                    if (a.sceneId == isl.sceneId && a.name == "Alestorm") { var p = cove2 - seaDir2 * 34f + new Vector2(-seaDir2.y, seaDir2.x) * 16f; a.x = p.x; a.z = p.y; keep.Add(a.name); }
            var avoid = new List<Vector3>();
            if (hasCastle) avoid.Add(new Vector3(castle2.x, CastleR + 26f, castle2.y));
            if (hasCove) avoid.Add(new Vector3(cove2.x, 30f, cove2.y));
            foreach (var v in peaks) avoid.Add(new Vector3(v.x, v.y * .7f, v.z));
            ArtistPlacement.Spread(data, index, 56f, heart2, 46f, 40f, avoid, keep, Land, 400f);
            foreach (var a in data.layout.artists)
            {
                if (a.sceneId != isl.sceneId) continue;
                a.r = Mathf.Max(a.r, Foot(a) + 6f);
                Level(data, index, new Vector2(a.x, a.z), Foot(a) + 1f, Foot(a) + 12f);
            }
            Level(data, index, heart2, 16f, 30f);
            if (hasCastle) Level(data, index, castle2, CastleR + 6f, CastleR + 26f);
        }

        void Level(IslandWorldData data, int index, Vector2 c, float flat, float blend)
        {
            var level = Mathf.Max(data.HeightAt(c.x, c.y), data.layout.waterLevel + 2f);
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
            center = ctx.Plaza;
            life = new GameObject("Power life").AddComponent<PowerLife>();
            life.transform.SetParent(ctx.Root, false);
            life.center = center; life.fire = fireM; life.embers = emberM; life.neonPink = neonPink; life.neonCyan = neonCyan; life.lanterns = lanternM;
            var mk = new MeshKit(); var solid = new MeshKit();
            Remember(ctx.OnGround(center.x, center.z), 0);
            Arrival(ctx, mk, solid);
            if (hasCastle) Castle(ctx, mk, solid);
            if (hasCove) Cove(ctx, mk, solid);
            Paths(ctx, mk, solid);
            Wilds(ctx, mk, solid);
            mk.Build("The Power Metal Peaks", ctx.Root, false);
            solid.BuildCollider("The Power Metal Peaks walls", ctx.Root);
        }

        public override void Finish(LandContext ctx) { MountainGrass(ctx); halos.Build("Peaks glow", ctx.Root); }

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
            ctx.IsLand(x, z, 5f) && !InFootprint(ctx, x, z, pad) && !NearPath(x, z, pad + 2f) && !InNoGrass(x, z, pad)
            && Vector2.Distance(new Vector2(x, z), new Vector2(center.x, center.z)) > 22f + pad
            && (!hasCastle || Vector2.Distance(new Vector2(x, z), castle2) > CastleR + 8f + pad)
            && (!hasCove || Vector2.Distance(new Vector2(x, z), cove2) > 26f + pad);

        void Strip(MeshKit mk, List<Vector3> pts, Material m, float w, float lift, LandContext ground = null)
        {
            var n = pts.Count; var L = new Vector3[n]; var R = new Vector3[n];
            Vector3 Flat(Vector3 v) { v.y = 0; return v.normalized; }
            for (var i = 0; i < n; i++)
            {
                var a = i > 0 ? Flat(pts[i] - pts[i - 1]) : Flat(pts[1] - pts[0]);
                var b = i < n - 1 ? Flat(pts[i + 1] - pts[i]) : Flat(pts[n - 1] - pts[n - 2]);
                var t = (a + b).normalized; var side = Vector3.Cross(Vector3.up, t);
                var miter = 1f / Mathf.Max(.6f, Vector3.Dot(side, Vector3.Cross(Vector3.up, b)));
                L[i] = pts[i] + Vector3.up * lift - side * (w / 2f * miter); R[i] = pts[i] + Vector3.up * lift + side * (w / 2f * miter);
                if (ground != null)
                {
                    float gl = ground.Ground(L[i].x, L[i].z), gr = ground.Ground(R[i].x, R[i].z), top = Mathf.Max(pts[i].y, Mathf.Max(gl, gr) - .05f);
                    L[i].y = Mathf.Max(gl, top - .15f) + lift; R[i].y = Mathf.Max(gr, top - .15f) + lift;
                }
            }
            for (var i = 0; i + 1 < n; i++) mk.Quad(m, L[i], R[i], R[i + 1], L[i + 1], (pts[i] + pts[i + 1]) / 2f - Vector3.up);
        }

        /// A torch on an iron pole: a flame in a cage, its light warm on the path (PowerLife makes it flicker and spark).
        void Torch(LandContext ctx, MeshKit mk, Vector3 g, float h = 2.8f)
        {
            var up = Vector3.up;
            mk.Prism(iron, g - up * .2f, up, .07f, h + .2f, 6);
            mk.Prism(iron, g + up * (h - .1f), up, .2f, .35f, 6, 1.4f);
            mk.Blob(fireM, g + up * (h + .35f), new Vector3(.18f, .32f, .18f), ctx.Rng, .2f);
            halos.Add(g + up * (h + .4f), 1.8f, Fire, .7f); life.torches.Add(g + up * (h + .35f));
            ArtistBits.PostCollider(ctx.Root, g, h, .12f);
        }

        /// A war banner on a tall pole: a long cloth hanging from a crossbar, gold finial on top.
        void Banner(LandContext ctx, MeshKit mk, Vector3 g, Vector3 face, Material cloth_, float h = 6f)
        {
            var up = Vector3.up; var rot = Quaternion.LookRotation(face); var side = Vector3.Cross(up, face);
            mk.Prism(woodDark, g - up * .3f, up, .1f, h + .3f, 6);
            mk.Box(woodDark, g + up * (h - .3f), new Vector3(1.6f, .08f, .08f), Quaternion.LookRotation(side));
            mk.Box(cloth_, g + up * (h - 1.9f) + face * .06f, new Vector3(1.4f, 3f, .04f), rot);
            mk.Box(cloth_, g + up * (h - 3.55f) + face * .06f, new Vector3(1f, .4f, .04f), rot * Quaternion.Euler(0, 0, 45f));
            mk.Box(goldM, g + up * (h - 1.9f) + face * .09f, new Vector3(.5f, .5f, .02f), rot * Quaternion.Euler(0, 0, 45f));
            mk.Prism(goldM, g + up * h, up, .14f, .4f, 4, .05f);
            ArtistBits.PostCollider(ctx.Root, g, h, .12f);
        }

        /// A brazier: an iron bowl of fire on three legs.
        void Brazier(LandContext ctx, MeshKit mk, Vector3 g, float s = 1f)
        {
            var up = Vector3.up;
            for (var k = 0; k < 3; k++) { var a = k * Mathf.PI * 2f / 3f; mk.Box(iron, g + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * .35f * s + up * .5f * s, new Vector3(.08f, 1f, .08f) * s, Quaternion.Euler(Mathf.Sin(a) * 12f, 0, -Mathf.Cos(a) * 12f)); }
            mk.Prism(iron, g + up * .95f * s, up, .5f * s, .35f * s, 10, 1.3f);
            mk.Blob(fireM, g + up * 1.45f * s, new Vector3(.45f, .5f, .45f) * s, ctx.Rng, .25f);
            halos.Add(g + up * 1.6f * s, 2.6f * s, Fire, .8f); life.fires.Add(g + up * 1.4f * s);
            ArtistBits.PostCollider(ctx.Root, g, 1.3f * s, .6f * s);
        }

        // ---------------------------------------------------------------- arrival
        /// Where you arrive: a round courtyard of flagstones ringed with braziers and banners, a standing stone carved
        /// with the land's name and a great sword driven into it, the castle and its dragon high on the peak ahead.
        void Arrival(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var c = ctx.OnGround(center.x, center.z);
            var toCastle = hasCastle ? new Vector3(castle2.x - c.x, 0, castle2.y - c.z).normalized : Vector3.forward; var sd = Vector3.Cross(up, toCastle);
            mk.Prism(wallStone, c - up * .3f, up, 13f, .42f, 24);
            for (var k = 0; k < 24; k++) { var a = k * Mathf.PI / 12f; mk.Box(wallDark, c + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * 8f + up * .13f, new Vector3(.12f, .03f, 2.1f), Quaternion.Euler(0, 90f - a * Mathf.Rad2Deg, 0)); }
            for (var k = 0; k < 6; k++)
            {
                var a = k * Mathf.PI / 3f + .26f; var g = ctx.OnGround(c.x + Mathf.Cos(a) * 14f, c.z + Mathf.Sin(a) * 14f);
                if (k % 2 == 0) Brazier(ctx, mk, g); else Banner(ctx, mk, g, new Vector3(c.x - g.x, 0, c.z - g.z).normalized, k % 4 == 1 ? bannerRed : bannerPurple, 7f);
            }
            // the name stone, a great sword driven into its top
            var s = c - toCastle * 15f; s.y = ctx.Ground(s.x, s.z); var rot = Quaternion.LookRotation(toCastle);
            mk.Box(rock, s + up * 1.8f, new Vector3(8f, 3.6f, 1.6f), rot); solid.Box(rock, s + up * 1.8f, new Vector3(8f, 3.6f, 1.6f), rot);
            mk.Box(rockDark, s + up * 3.8f, new Vector3(8.6f, .5f, 2f), rot);
            ArtistBits.Label(ctx.Root, "THE POWER METAL PEAKS", s + up * 1.9f + toCastle * .82f, toCastle, .46f, Gold, TextAnchor.MiddleCenter);
            var steel = WorldMaterials.Solid(new Color(.75f, .76f, .8f), .9f);
            mk.Box(steel, s + up * 6.2f, new Vector3(.7f, 5f, .12f), rot); mk.Prism(steel, s + up * 8.7f, up, .36f, .9f, 4, .02f, 45f);
            mk.Box(goldM, s + up * 8.9f, new Vector3(2.6f, .3f, .3f), rot); mk.Box(woodDark, s + up * 9.8f, new Vector3(.3f, 1.6f, .3f), rot); mk.Blob(goldM, s + up * 10.7f, Vector3.one * .3f, ctx.Rng, 0f);
            halos.Add(s + up * 2f + toCastle * 1.6f, 4.6f, Fire, .3f);
            ArtistBits.PointLight(ctx.Root, c + up * 6f, Fire, 26f, 1.6f);
            noGrass.Add(new Vector3(c.x, 15f, c.z));
            var mark = new GameObject("Shot: the arrival and the castle").transform; mark.SetParent(ctx.Root, false);
            var cam = c - toCastle * 9f + sd * 3f + up * 1.7f; var look = (hasCastle ? ctx.OnGround(castle2.x, castle2.y) + up * 20f : c + toCastle * 60f + up * 10f) - cam;
            mark.SetPositionAndRotation(cam, Quaternion.LookRotation(look.normalized));
        }

        // ---------------------------------------------------------------- the castle and its dragon
        /// On the peak: a ring of curtain walls with six round towers and a gatehouse facing the arrival, a tall keep
        /// in the middle, banners, braziers along the battlements, lit windows; and coiled round the keep, a dragon:
        /// a long scaled body spiralling up, wings spread, head over the battlements with glowing eyes (PowerLife makes
        /// it breathe fire).
        void Castle(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var c = ctx.OnGround(castle2.x, castle2.y);
            var toHeart = new Vector3(center.x - c.x, 0, center.z - c.z).normalized; var gateA = Mathf.Atan2(toHeart.z, toHeart.x);
            const float R = CastleR - 2f, WH = 9f, TH = 14f;
            var win = GlowM(new Color(1f, .7f, .35f), 2f);
            // the curtain wall, open at the gate
            for (var k = 0; k < 36; k++)
            {
                float a0 = k * Mathf.PI / 18f, a1 = (k + 1) * Mathf.PI / 18f, am = (a0 + a1) / 2f;
                if (Mathf.Abs(Mathf.DeltaAngle(am * Mathf.Rad2Deg, gateA * Mathf.Rad2Deg)) < 8f) continue;
                var p0 = c + new Vector3(Mathf.Cos(a0), 0, Mathf.Sin(a0)) * R; var p1 = c + new Vector3(Mathf.Cos(a1), 0, Mathf.Sin(a1)) * R;
                var mid = (p0 + p1) / 2f; var len = Vector3.Distance(p0, p1) + .3f; var wr = Quaternion.LookRotation(p1 - p0);
                mk.Box(wallStone, mid + up * (WH / 2f - 1f), new Vector3(2f, WH + 2f, len), wr); solid.Box(wallStone, mid + up * (WH / 2f - 1f), new Vector3(2f, WH + 2f, len), wr);
                for (var j = 0; j < 2; j++) mk.Box(wallStone, mid + up * (WH + .5f) + (p1 - p0).normalized * ((j - .5f) * len * .5f) + (mid - c).normalized * .7f, new Vector3(.6f, 1f, len * .3f), wr);
            }
            // six towers, the two either side of the gate taller
            for (var k = 0; k < 6; k++)
            {
                var a = gateA + Mathf.PI / 6f + k * Mathf.PI / 3f;
                var p = c + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * R; var h = k == 0 || k == 5 ? TH + 2f : TH;
                mk.Prism(wallStone, p - up * 2f, up, 3.2f, h + 2f, 12); solid.Prism(wallStone, p - up * 2f, up, 3.2f, h + 2f, 12);
                mk.Prism(wallDark, p + up * h, up, 3.6f, .7f, 12);
                mk.Cone(k % 2 == 0 ? bannerRed : bannerPurple, p + up * (h + .7f), 3.4f, 5.5f, 12);
                var flagAt = p + up * (h + 6.2f); mk.Prism(woodDark, flagAt - up * .4f, up, .05f, 2.4f, 5); mk.Box(k % 2 == 0 ? bannerPurple : bannerRed, flagAt + up * 1.5f + new Vector3(.6f, 0, 0), new Vector3(1.2f, .7f, .04f), Quaternion.identity);
                var outD = (p - c).normalized; foreach (var y in new[] { 4f, 8.5f }) mk.Box(win, p + outD * 3.15f + up * y, new Vector3(.5f, 1f, .1f), Quaternion.LookRotation(outD));
                halos.Add(p + outD * 3.6f + up * 6f, 2.4f, Fire, .35f);
            }
            // the gatehouse
            var gate = c + toHeart * R; var gRot = Quaternion.LookRotation(toHeart); var gs = Vector3.Cross(up, toHeart);
            foreach (var e in new[] { -1f, 1f }) { mk.Box(wallStone, gate + gs * e * 3f + up * (WH / 2f), new Vector3(2f, WH + 2f, 4f), gRot); solid.Box(wallStone, gate + gs * e * 3f + up * (WH / 2f), new Vector3(2f, WH + 2f, 4f), gRot); }
            mk.Box(wallStone, gate + up * (WH - .8f), new Vector3(8f, 3.6f, 4f), gRot); solid.Box(wallStone, gate + up * (WH - .8f), new Vector3(8f, 3.6f, 4f), gRot);
            for (var k = 0; k < 5; k++) mk.Box(iron, gate + gs * ((k - 2f) * .9f) + up * 6.2f + toHeart * 1.6f, new Vector3(.12f, 1.4f, .12f), gRot);   // the raised portcullis
            foreach (var e in new[] { -1f, 1f }) Brazier(ctx, mk, ctx.OnGround(gate.x + toHeart.x * 4f + gs.x * e * 4.6f, gate.z + toHeart.z * 4f + gs.z * e * 4.6f), 1.2f);
            Banner(ctx, mk, gate + toHeart * 2.1f + up * 2f, toHeart, bannerRed, 5f);
            // the keep
            var keepH = 30f; const float KW = 9f;
            mk.Box(wallStone, c + up * (keepH / 2f - 2f), new Vector3(KW, keepH + 4f, KW), gRot); solid.Box(wallStone, c + up * (keepH / 2f - 2f), new Vector3(KW, keepH + 4f, KW), gRot);
            for (var j = 0; j < 4; j++) { var d = Quaternion.Euler(0, j * 90f, 0) * toHeart; for (var y = 6f; y < keepH - 2f; y += 6f) mk.Box(win, c + d * (KW / 2f + .02f) + up * y, new Vector3(1f, 1.8f, .06f), Quaternion.LookRotation(d)); for (var e = -1; e <= 1; e += 2) mk.Box(wallStone, c + d * (KW / 2f - .3f) + Vector3.Cross(up, d) * e * 3f + up * (keepH + .6f), new Vector3(1.4f, 1.2f, .8f), Quaternion.LookRotation(d)); }
            mk.Box(wallDark, c + up * keepH, new Vector3(KW + 1f, .6f, KW + 1f), gRot);
            Banner(ctx, mk, c + up * (keepH + .3f), toHeart, bannerPurple, 7f);
            ArtistBits.PointLight(ctx.Root, c + up * 12f + toHeart * 14f, Fire, 40f, 2f);
            ArtistBits.PointLight(ctx.Root, c + up * (keepH + 6f), new Color(1f, .6f, .3f), 30f, 1.4f);
            // braziers along the inside of the walls
            for (var k = 0; k < 8; k++) { var a = gateA + Mathf.PI / 8f + k * Mathf.PI / 4f; Brazier(ctx, mk, ctx.OnGround(c.x + Mathf.Cos(a) * (R - 4f), c.z + Mathf.Sin(a) * (R - 4f))); }
            // the dragon: a scaled body spiralling up round the keep, wings spread, head out over the battlements
            var scale = WorldMaterials.Solid(new Color(.38f, .1f, .12f), .6f); var belly = WorldMaterials.Solid(new Color(.75f, .5f, .25f));
            var eye = new Material(GlowM(new Color(1f, .85f, .2f), 3f)) { name = "Dragon eyes" }; life.dragonEyes = eye;
            Vector3 Coil(float t) { var a = gateA + Mathf.PI + t * Mathf.PI * 3.2f; var r = KW * .72f + 1.6f; return c + new Vector3(Mathf.Cos(a) * r, 2f + t * (keepH - 1f), Mathf.Sin(a) * r); }
            var steps = 60;
            for (var k = 0; k < steps; k++)
            {
                var t0 = k / (float)steps; var t1 = (k + 1) / (float)steps; var p0 = Coil(t0); var p1 = Coil(t1);
                var thick = Mathf.Lerp(.6f, 1.6f, Mathf.Sin(t0 * Mathf.PI * .9f + .2f));
                var r0 = Quaternion.LookRotation(p1 - p0);
                mk.Box(scale, (p0 + p1) / 2f, new Vector3(thick * 1.6f, thick * 1.4f, Vector3.Distance(p0, p1) + .3f), r0);
                mk.Box(belly, (p0 + p1) / 2f - (r0 * Vector3.up) * thick * .55f, new Vector3(thick * 1.1f, .2f, Vector3.Distance(p0, p1) + .2f), r0);
                if (k % 3 == 0) mk.Prism(rockDark, (p0 + p1) / 2f + (r0 * Vector3.up) * thick * .65f, r0 * Vector3.up, thick * .25f, thick * .8f, 4, .05f);
            }
            var neckBase = Coil(1f); var headDir = toHeart; var head = neckBase + headDir * 6f + up * 2.5f;
            for (var k = 0; k < 6; k++) { var p0 = Vector3.Lerp(neckBase, head, k / 6f) + up * Mathf.Sin(k / 6f * Mathf.PI) * 1.5f; var p1 = Vector3.Lerp(neckBase, head, (k + 1) / 6f) + up * Mathf.Sin((k + 1) / 6f * Mathf.PI) * 1.5f; mk.Box(scale, (p0 + p1) / 2f, new Vector3(1.4f, 1.3f, Vector3.Distance(p0, p1) + .3f), Quaternion.LookRotation(p1 - p0)); }
            var hRot = Quaternion.LookRotation(headDir); var hs = Vector3.Cross(up, headDir);
            mk.Box(scale, head + headDir * .6f, new Vector3(1.8f, 1.5f, 2.6f), hRot);
            mk.Box(scale, head + headDir * 2.4f - up * .3f, new Vector3(1.3f, .8f, 2.2f), hRot);   // the snout
            mk.Box(scale, head + headDir * 2.3f - up * .85f, new Vector3(1.1f, .3f, 2f), hRot * Quaternion.Euler(12f, 0, 0));   // the jaw, open
            foreach (var e in new[] { -1f, 1f })
            {
                mk.Box(eye, head + headDir * 1.4f + hs * e * .72f + up * .35f, new Vector3(.12f, .22f, .35f), hRot);
                mk.Prism(rockDark, head + hs * e * .6f + up * .6f, (up - headDir * .7f + hs * e * .2f).normalized, .22f, 2.2f, 5, .05f);   // horns
                // the wings, spread from the coil's top
                var root = Coil(.82f); var wDir = (hs * e + up * .4f).normalized;
                for (var j = 0; j < 4; j++)
                {
                    var tip = root + wDir * (9f - j * 1.2f) + up * (3f - j * 1.6f) - headDir * j * 1.4f;
                    mk.Box(rockDark, (root + tip) / 2f, new Vector3(.3f, .3f, Vector3.Distance(root, tip)), Quaternion.LookRotation(tip - root));
                    if (j < 3)
                    {
                        var tip2 = root + wDir * (9f - (j + 1) * 1.2f) + up * (3f - (j + 1) * 1.6f) - headDir * (j + 1) * 1.4f;
                        mk.Tri(WorldMaterials.Solid(new Color(.55f, .15f, .14f)), root, tip, tip2, root - up * 3f);
                        mk.Tri(WorldMaterials.Solid(new Color(.55f, .15f, .14f)), root, tip2, tip, root + up * 3f);
                    }
                }
            }
            halos.Add(head + headDir * 1.4f + up * .3f, 1.4f, Gold, .7f);
            life.dragonMouth = head + headDir * 3.4f - up * .5f; life.dragonDir = (headDir - up * .25f).normalized;
            noGrass.Add(new Vector3(c.x, CastleR + 6f, c.z));
            var mark = new GameObject("Shot: the dragon castle").transform; mark.SetParent(ctx.Root, false);
            var cam = c + toHeart * (R + 30f) + gs * 12f; cam = new Vector3(cam.x, Mathf.Max(ctx.Ground(cam.x, cam.z), c.y) + 10f, cam.z);
            mark.SetPositionAndRotation(cam, Quaternion.LookRotation((c + up * 22f - cam).normalized));
        }

        // ---------------------------------------------------------------- the pirate cove
        /// On the coast: a plank pier running out into the water on posts, lanterns on it, a pirate ship at anchor
        /// beyond (black hull, three masts, patched sails, a skull flag, lanterns), barrels and nets, a beached rowing
        /// boat. Alestorm's tavern stands just inland.
        void Cove(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var sea = new Vector3(seaDir2.x, 0, seaDir2.y); var sd = Vector3.Cross(up, sea);
            var shore = ctx.OnGround(cove2.x, cove2.y); var water = ctx.Data.layout.waterLevel;
            var deckY = Mathf.Max(water + 1.2f, shore.y + .4f);
            var pierLen = 32f; var p0 = new Vector3(shore.x, deckY, shore.z) - sea * 6f; var p1 = p0 + sea * pierLen;
            var pr = Quaternion.LookRotation(sea);
            mk.Box(wood, (p0 + p1) / 2f, new Vector3(3.2f, .25f, pierLen), pr); solid.Box(wood, (p0 + p1) / 2f, new Vector3(3.2f, .25f, pierLen), pr);
            for (var t = 0f; t <= pierLen; t += 4f)
                foreach (var e in new[] { -1f, 1f })
                {
                    var q = p0 + sea * t + sd * e * 1.5f; var g = Mathf.Min(ctx.Ground(q.x, q.z), water - 3f);
                    mk.Prism(woodDark, new Vector3(q.x, g, q.z), up, .18f, deckY - g + .9f, 6);
                    ArtistBits.PostCollider(ctx.Root, new Vector3(q.x, deckY, q.z), .9f, .2f);
                    if (Mathf.Repeat(t, 8f) < .1f && e > 0) { mk.Blob(lanternM, q + up * 1.15f, new Vector3(.18f, .25f, .18f), ctx.Rng, 0f); halos.Add(q + up * 1.2f, 1.4f, new Color(1f, .7f, .4f), .6f); }
                }
            ArtistBits.PointLight(ctx.Root, p1 + up * 3f, new Color(1f, .7f, .4f), 18f, 1.8f);
            // the ship, at anchor off the end of the pier, side on
            var ship = p1 + sea * 14f + sd * 4f; ship.y = water; var sr = Quaternion.LookRotation(sd); var hull = WorldMaterials.Solid(new Color(.12f, .09f, .08f));
            var sk = new MeshKit();
            const float SL = 26f;
            sk.Box(hull, up * .6f, new Vector3(6f, 3.4f, SL), Quaternion.identity);
            sk.Prism(hull, up * .6f + Vector3.forward * SL / 2f, Vector3.forward, 3f, 5f, 4, .05f, 45f);   // the bow
            sk.Box(hull, up * 3.2f - Vector3.forward * (SL / 2f - 3f), new Vector3(6.2f, 2.4f, 6f), Quaternion.identity);   // the stern castle
            sk.Box(WorldMaterials.Solid(new Color(.55f, .4f, .2f)), up * 2.35f, new Vector3(5.6f, .1f, SL - 1f), Quaternion.identity);
            for (var k = 0; k < 6; k++) foreach (var e in new[] { -1f, 1f }) sk.Box(GlowM(new Color(1f, .7f, .35f), 2f), new Vector3(e * 3.02f, 1.3f, (k - 2.5f) * 3.6f), new Vector3(.05f, .5f, .7f), Quaternion.identity);
            foreach (var (z, h) in new[] { (-6f, 15f), (2f, 19f), (9f, 14f) })
            {
                sk.Prism(woodDark, new Vector3(0, 2.3f, z), up, .3f, h, 8);
                for (var j = 0; j < 2; j++) { var y = 6f + j * (h * .38f); var w = 8f - j * 2.4f; sk.Box(cloth, new Vector3(0, y + 2f, z + .3f), new Vector3(w, h * .3f, .06f), Quaternion.Euler(-6f, 0, 0)); sk.Box(woodDark, new Vector3(0, y + 2f + h * .15f, z), new Vector3(w + .6f, .18f, .18f), Quaternion.identity); }
            }
            sk.Box(rockDark, new Vector3(0, 21.6f, 2f), new Vector3(1.8f, 1.1f, .04f), Quaternion.identity);   // the black flag
            sk.Blob(WorldMaterials.Solid(new Color(.92f, .9f, .85f)), new Vector3(0, 21.7f, 1.96f), new Vector3(.3f, .32f, .05f), ctx.Rng, 0f);
            sk.Blob(lanternM, new Vector3(0, 5f, -SL / 2f), Vector3.one * .3f, ctx.Rng, 0f);
            var shipGo = sk.BuildSingle("The pirate ship", life.transform, true);
            if (shipGo != null) { shipGo.transform.SetPositionAndRotation(ship, sr); life.ship = shipGo.transform; life.shipAt = ship; life.shipRot = sr; }
            halos.Add(ship + up * 3f, 10f, new Color(1f, .7f, .4f), .25f);
            ArtistBits.PointLight(ctx.Root, ship + up * 6f - sea * 4f, new Color(1f, .7f, .4f), 24f, 1.6f);
            // a beached rowing boat, barrels and a chest on the shore
            var boat = ctx.OnGround(shore.x - sd.x * 8f - sea.x * 2f, shore.z - sd.z * 8f - sea.z * 2f);
            mk.Box(wood, boat + up * .3f, new Vector3(1.4f, .6f, 3.6f), Quaternion.LookRotation(sea) * Quaternion.Euler(0, 20f, 8f)); BoxCol(ctx.Root, boat + up * .3f, new Vector3(1.4f, .7f, 3.6f), Quaternion.LookRotation(sea) * Quaternion.Euler(0, 20f, 0));
            for (var k = 0; k < 5; k++) { var b = ctx.OnGround(shore.x + sd.x * (5f + k * .9f) - sea.x * (3f + (k % 2) * .9f), shore.z + sd.z * (5f + k * .9f) - sea.z * (3f + (k % 2) * .9f)); mk.Prism(wood, b, up, .42f, 1.1f, 10); mk.Prism(iron, b + up * .25f, up, .44f, .08f, 10); ArtistBits.PostCollider(ctx.Root, b, 1.1f, .42f); }
            TreasureChest(ctx, mk, ctx.OnGround(shore.x + sd.x * 4f - sea.x * 6f, shore.z + sd.z * 4f - sea.z * 6f), sea);
            noGrass.Add(new Vector3(shore.x, 14f, shore.z));
            life.coveAt = shore;
            var mark = new GameObject("Shot: the pirate cove").transform; mark.SetParent(ctx.Root, false);
            var cam = shore - sea * 18f + sd * 10f; cam = ctx.OnGround(cam.x, cam.z) + up * 3f;
            mark.SetPositionAndRotation(cam, Quaternion.LookRotation((ship + up * 6f - cam).normalized));
        }

        /// A treasure chest, lid open, gold glowing inside.
        void TreasureChest(LandContext ctx, MeshKit mk, Vector3 g, Vector3 face)
        {
            var up = Vector3.up; var rot = Quaternion.LookRotation(face);
            mk.Box(wood, g + up * .35f, new Vector3(1.2f, .7f, .8f), rot);
            foreach (var e in new[] { -.45f, .45f }) mk.Box(goldM, g + up * .35f + Vector3.Cross(up, face) * e, new Vector3(.08f, .72f, .82f), rot);
            mk.Box(wood, g + up * .95f - face * .55f, new Vector3(1.2f, .7f, .12f), rot * Quaternion.Euler(-20f, 0, 0));
            mk.Blob(GlowM(Gold, 2.2f), g + up * .72f, new Vector3(.5f, .14f, .32f), ctx.Rng, .2f);
            halos.Add(g + up * 1f, 1.4f, Gold, .6f);
            BoxCol(ctx.Root, g + up * .4f, new Vector3(1.2f, .8f, .8f), rot);
        }

        // ---------------------------------------------------------------- paths
        /// Paths of worn flagstones, branching from the arrival to every artist, up to the castle gate and down to the
        /// cove; torches along them.
        void Paths(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var path = WorldMaterials.Solid(new Color(.42f, .37f, .34f));
            var posts = new List<Vector3>(); var walk = 1;
            void Lay(Vector3 join, Vector3 to, float stopShort, string artist)
            {
                var from = new Vector3(join.x, 0, join.z); to.y = 0;
                var dir = to - from; var total = dir.magnitude; if (total < 2f) return; dir /= total;
                if (artist != null) facing[artist] = -dir;
                var side = Vector3.Cross(Vector3.up, dir); var end = total - stopShort;
                var pts = new List<Vector3>();
                for (var d = walk == 1 ? 14f : PathW / 2f + .1f; ; d += 1.5f)
                {
                    var dd = Mathf.Min(d, end); var q = from + dir * dd + side * Mathf.Sin(dd / 13f) * Mathf.Min(3.5f, end * .05f);
                    if (!ctx.IsLand(q.x, q.z, 0f)) break;
                    pts.Add(ctx.OnGround(q.x, q.z)); if (dd >= end) break;
                }
                if (pts.Count < 2) return;
                Strip(mk, pts, path, PathW, .18f, ctx);
                foreach (var p in pts) Remember(p, walk);
                for (var i = 8; i < pts.Count; i += 12)
                {
                    var lp = pts[i] + side * ((i / 12) % 2 == 0 ? 1f : -1f) * (PathW / 2f + .9f);
                    var near = false; foreach (var o in posts) if ((o - lp).sqrMagnitude < 16f * 16f) near = true;
                    if (near || InFootprint(ctx, lp.x, lp.z, 1f)) continue;
                    posts.Add(lp); Torch(ctx, mk, ctx.OnGround(lp.x, lp.z));
                }
                walk++;
            }
            if (hasCastle) Lay(ctx.OnGround(center.x, center.z), new Vector3(castle2.x, 0, castle2.y), CastleR - 2f, null);
            if (hasCove) Lay(ctx.OnGround(center.x, center.z), new Vector3(cove2.x, 0, cove2.y), 6f, null);
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

        // ---------------------------------------------------------------- the wilds
        /// Dark pines in stands, war banners, swords driven into stones, treasure chests, braziers, ruined
        /// watchtowers, boulders; and here and there kawaii neon (a heart or a star on a post), a stone lantern or a
        /// cherry tree in blossom.
        void Wilds(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            int pines = 0, banners = 0, swords = 0, chests = 0, braziers = 0, ruins = 0, neons = 0, toro = 0, cherries = 0, rocks = 0;
            const float cell = 18f;
            for (var x = ctx.MinX; x < ctx.MaxX; x += cell)
            for (var z = ctx.MinZ; z < ctx.MaxZ; z += cell)
            {
                float px = x + ctx.Range(0f, cell), pz = z + ctx.Range(0f, cell);
                if (!Open(ctx, px, pz, 3f)) continue;
                var p = ctx.OnGround(px, pz); var roll = ctx.Range(0f, 1f); var high = p.y > 108f;
                if (roll < .36f && !high) { Pines(ctx, mk, p); pines++; }
                else if (roll < .42f) continue;
                else if (roll < .44f) { Banner(ctx, mk, p, Quaternion.Euler(0, ctx.Range(0, 360f), 0) * Vector3.forward, ctx.Chance(.5f) ? bannerRed : bannerPurple, ctx.Range(5f, 8f)); banners++; }
                else if (roll < .51f) { SwordInStone(ctx, mk, p); swords++; }
                else if (roll < .53f) { TreasureChest(ctx, mk, p, Quaternion.Euler(0, ctx.Range(0, 360f), 0) * Vector3.forward); chests++; }
                else if (roll < .56f) { Brazier(ctx, mk, p); braziers++; }
                else if (roll < .58f && ruins < 8) { Watchtower(ctx, mk, solid, p); ruins++; }
                else if (roll < .61f) { KawaiiSign(ctx, mk, p); neons++; }
                else if (roll < .64f && !high) { StoneLantern(ctx, mk, p); toro++; }
                else if (roll < .67f && !high) { Cherry(ctx, mk, p); cherries++; }
                else if (roll < .78f)
                {
                    var s = ctx.Range(1.2f, 3.4f); var lo = p.y;
                    for (var k = 0; k < 6; k++) { var a = k * Mathf.PI / 3f; lo = Mathf.Min(lo, ctx.Ground(px + Mathf.Cos(a) * s * .9f, pz + Mathf.Sin(a) * s * .9f)); }
                    p.y = lo; mk.Blob(ctx.Chance(.5f) ? rock : rockDark, p + Vector3.up * s * .35f, new Vector3(s, s * .8f, s * .9f), ctx.Rng, .35f);
                    BoxCol(ctx.Root, p + Vector3.up * s * .35f, new Vector3(s * 1.8f, s * 1.4f, s * 1.7f), Quaternion.identity); rocks++;
                }
            }
            Debug.Log($"The Power Metal Peaks: castle {(hasCastle ? "yes" : "no")}, cove {(hasCove ? "yes" : "no")}, {peaks.Count} peaks, {pines} pine stands, {banners} banners, {swords} swords, {chests} chests, {braziers} braziers, {ruins} watchtowers, {neons} neon signs, {toro} stone lanterns, {cherries} cherry trees, {rocks} rocks");
        }

        void Pines(LandContext ctx, MeshKit mk, Vector3 at)
        {
            var needles = WorldMaterials.Solid(new Color(.1f, .17f, .13f)); var n = ctx.Rng.Next(2, 6);
            for (var k = 0; k < n; k++)
            {
                var q = ctx.OnGround(at.x + ctx.Range(-5f, 5f), at.z + ctx.Range(-5f, 5f)); var h = ctx.Range(6f, 13f);
                mk.Prism(woodDark, q - Vector3.up * .3f, Vector3.up, .25f, h * .3f, 6);
                for (var j = 0; j < 3; j++) mk.Cone(needles, q + Vector3.up * (h * (.2f + j * .22f)), h * (.26f - j * .06f), h * .42f, 7);
                ArtistBits.PostCollider(ctx.Root, q, h * .4f, .3f);
            }
        }

        void SwordInStone(LandContext ctx, MeshKit mk, Vector3 at)
        {
            var up = Vector3.up; var tilt = Quaternion.Euler(ctx.Range(-8f, 8f), ctx.Range(0, 180f), ctx.Range(-8f, 8f));
            mk.Blob(rock, at + up * .4f, new Vector3(1.3f, .9f, 1.1f), ctx.Rng, .3f);
            var steel = WorldMaterials.Solid(new Color(.78f, .8f, .84f), .9f);
            mk.Box(steel, at + up * 1.8f, new Vector3(.22f, 2.6f, .05f), tilt);
            mk.Box(goldM, at + up * 3.1f, new Vector3(1f, .14f, .14f), tilt); mk.Box(woodDark, at + up * 3.5f, new Vector3(.14f, .7f, .14f), tilt);
            mk.Blob(GlowM(new Color(.5f, .8f, 1f), 2.2f), at + up * 3.9f, Vector3.one * .14f, ctx.Rng, 0f); halos.Add(at + up * 3.9f, 1.2f, new Color(.5f, .8f, 1f), .5f);
            BoxCol(ctx.Root, at + up * .6f, new Vector3(2.2f, 1.4f, 2f), Quaternion.identity);
        }

        void Watchtower(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 at)
        {
            var up = Vector3.up; var h = ctx.Range(7f, 12f);
            mk.Prism(wallStone, at - up * 1f, up, 2.4f, h, 8, .9f); solid.Prism(wallStone, at - up * 1f, up, 2.4f, h, 8, .9f);
            for (var k = 0; k < 8; k += 2) { var a = k * Mathf.PI / 4f; mk.Box(wallStone, at + up * (h - .5f) + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * 2f, new Vector3(.7f, ctx.Range(.4f, 1.4f), .7f), Quaternion.Euler(0, -a * Mathf.Rad2Deg, 0)); }
            mk.Box(GlowM(new Color(1f, .7f, .35f), 1.8f), at + up * (h * .55f) + new Vector3(0, 0, 2.2f), new Vector3(.4f, .9f, .1f), Quaternion.identity);
            Brazier(ctx, mk, at + up * (h - 1f), .8f);
        }

        /// Kawaii neon: a pink heart or a cyan star on a slim post, a dash of the idol-metal bands across the slopes.
        void KawaiiSign(LandContext ctx, MeshKit mk, Vector3 at)
        {
            var up = Vector3.up; var heart = ctx.Chance(.55f); var m = heart ? neonPink : neonCyan; var face = new Vector3(center.x - at.x, 0, center.z - at.z).normalized; var rot = Quaternion.LookRotation(face); var sd = Vector3.Cross(up, face);
            mk.Prism(iron, at - up * .2f, up, .06f, 3.2f, 6); ArtistBits.PostCollider(ctx.Root, at, 3f, .1f);
            var c = at + up * 3.9f;
            if (heart) { foreach (var e in new[] { -1f, 1f }) mk.Box(m, c + sd * e * .32f, new Vector3(.22f, 1.1f, .06f), rot * Quaternion.Euler(0, 0, e * 40f)); foreach (var e in new[] { -1f, 1f }) mk.Box(m, c + sd * e * .38f + up * .5f, new Vector3(.5f, .22f, .06f), rot * Quaternion.Euler(0, 0, e * -20f)); }
            else for (var k = 0; k < 5; k++) { var a = k * 72f; var d = Quaternion.AngleAxis(a, face) * up; mk.Box(m, c + d * .4f, new Vector3(.16f, .8f, .06f), Quaternion.LookRotation(face, d)); }
            halos.Add(c + face * .5f, 1.6f, heart ? Pink : NeonCyan, .6f); life.neonSigns.Add(c);
        }

        /// A stone lantern (toro): a stacked granite lantern with a warm light in its window.
        void StoneLantern(LandContext ctx, MeshKit mk, Vector3 at)
        {
            var up = Vector3.up; var g = WorldMaterials.Solid(new Color(.5f, .48f, .45f));
            mk.Prism(g, at - up * .1f, up, .45f, .3f, 6); mk.Prism(g, at + up * .2f, up, .16f, .9f, 6);
            mk.Box(g, at + up * 1.25f, new Vector3(.75f, .5f, .75f), Quaternion.identity);
            mk.Box(lanternM, at + up * 1.25f, new Vector3(.4f, .3f, .78f), Quaternion.identity);
            mk.Prism(g, at + up * 1.5f, up, .65f, .35f, 6, .2f);
            halos.Add(at + up * 1.25f, 1.1f, new Color(1f, .72f, .4f), .6f);
            ArtistBits.PostCollider(ctx.Root, at, 1.6f, .45f);
        }

        void Cherry(LandContext ctx, MeshKit mk, Vector3 at)
        {
            var up = Vector3.up; var h = ctx.Range(4f, 6f); var blossom = WorldMaterials.Solid(new Color(1f, .7f, .82f));
            mk.Prism(woodDark, at - up * .3f, (up + new Vector3(ctx.Range(-.2f, .2f), 0, ctx.Range(-.2f, .2f))).normalized, .22f, h * .6f, 6, .7f);
            for (var k = 0; k < 4; k++) mk.Blob(blossom, at + up * h * .75f + new Vector3(ctx.Range(-1.4f, 1.4f), ctx.Range(-.4f, .6f), ctx.Range(-1.4f, 1.4f)), Vector3.one * ctx.Range(1f, 1.6f), ctx.Rng, .3f);
            halos.Add(at + up * h * .75f, 2.6f, new Color(1f, .6f, .75f), .18f);
            ArtistBits.PostCollider(ctx.Root, at, h * .5f, .25f);
        }

        void MountainGrass(LandContext ctx)
        {
            var mat = WindMesh.Material(.35f, 1f, new Color(.24f, .3f, .2f));
            var meshes = new Dictionary<(int, int), WindMesh>(); var rng = new System.Random(1999);
            const float st = 2.6f;
            for (var x = ctx.MinX; x < ctx.MaxX; x += st)
            for (var z = ctx.MinZ; z < ctx.MaxZ; z += st)
            {
                var px = x + ((float)rng.NextDouble() - .5f) * st; var pz = z + ((float)rng.NextDouble() - .5f) * st;
                if (!ctx.IsLand(px, pz, 1f) || NearPath(px, pz, 2.4f) || InNoGrass(px, pz, 0f) || InFootprint(ctx, px, pz, -2f)) continue;
                if (hasCastle && Vector2.Distance(new Vector2(px, pz), castle2) < CastleR + 2f) continue;
                var g = ctx.Ground(px, pz); if (g > 110f) continue;
                if (Mathf.PerlinNoise(px * .03f + 3f, pz * .03f + 9f) < .4f) continue;
                var key = (Mathf.FloorToInt(px / 70f), Mathf.FloorToInt(pz / 70f));
                if (!meshes.TryGetValue(key, out var wm)) meshes[key] = wm = new WindMesh();
                var col = Color.Lerp(new Color(.18f, .26f, .16f), new Color(.36f, .36f, .22f), (float)rng.NextDouble());
                wm.Tuft(new Vector3(px, g - .04f, pz), .35f + (float)rng.NextDouble() * .45f, col, Color.Lerp(col, new Color(.9f, .6f, .3f), .35f), rng, 4, .06f);
            }
            foreach (var kv in meshes) kv.Value.Build("Mountain grass", ctx.Root, mat);
        }
    }
}
