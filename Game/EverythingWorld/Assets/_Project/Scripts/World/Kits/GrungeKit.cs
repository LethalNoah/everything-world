using System.Collections.Generic;
using UnityEngine;

namespace EverythingWorld.World
{
    /// The Grunge Rainwood (WORLD_GUIDE.md): heavy and moody. A dripping pine forest on a high plateau under a low grey
    /// sky and constant drizzle. Behind where you arrive a mossy bluff rises, and a roaring waterfall pours off it into a
    /// dark pool; beside the pool stands the old sawmill, turned into a venue, its big door open on a lit stage, a
    /// rusted smokestack and a giant saw blade over the door, a water wheel turning in the race. The river runs from the
    /// pool through the forest down to the sea, crossed by plank bridges. Muddy logging roads with puddles, yellow road
    /// lamps, a coffee shack with one light on, a loaded logging truck, flannel shirts on clotheslines, rusted trailers,
    /// fallen mossy logs, stumps, fire lookout towers, phone booths. Cold blue-grey light, warm yellow lamps: it reads
    /// by brightness and blue against yellow.
    /// Files: GrungeKit (ground, bluff and falls, the mill, the river, roads, the forest), GrungeArtists, GrungeSongs,
    /// GrungeLife.
    public partial class GrungeKit : LandKit
    {
        public override string Land => "The Grunge Rainwood";
        public override Vector3 ArtistFront(IslandWorldData data, IslandWorldData.Artist a) => facing.TryGetValue(a.name, out var f) ? f : base.ArtistFront(data, a);

        public static readonly Color Lamp = new Color(1f, .82f, .42f), Ink = new Color(.95f, .93f, .86f), Falls = new Color(.75f, .9f, 1f), Flannel = new Color(.62f, .16f, .14f);
        const float RoadW = 3.2f, RiverW = 5.5f;

        Material mud, wetMud, moss, rock, darkRock, bark, pine, pineDark, plank, oldPlank, rust, dark, metal, water, foam, tin, concrete;
        GlowHalos halos; Vector3 center; GrungeLife life; MeshKit soft;
        readonly Dictionary<(int, int), List<Vector3>> pathCells = new Dictionary<(int, int), List<Vector3>>();
        readonly List<(Vector3 p, int w)> walkPoints = new List<(Vector3, int)>();
        readonly Dictionary<string, Vector3> facing = new Dictionary<string, Vector3>();
        readonly List<Vector3> noGrass = new List<Vector3>();
        Vector2 heart2, bluff2, lip2, pool2, mill2, toBluff2; float bluffR = 60f, poolY; bool hasBluff;
        readonly List<Vector2> river2 = new List<Vector2>(); readonly List<float> riverY = new List<float>();

        void MakeMaterials()
        {
            mud = WorldMaterials.Solid(new Color(.3f, .25f, .19f), .35f); wetMud = WorldMaterials.Solid(new Color(.2f, .17f, .14f), .75f);
            moss = WorldMaterials.Solid(new Color(.2f, .3f, .19f)); rock = WorldMaterials.Solid(new Color(.33f, .35f, .35f), .3f); darkRock = WorldMaterials.Solid(new Color(.19f, .21f, .21f), .35f);
            bark = WorldMaterials.Solid(new Color(.25f, .19f, .15f)); pine = WorldMaterials.Solid(new Color(.15f, .25f, .19f)); pineDark = WorldMaterials.Solid(new Color(.1f, .17f, .14f));
            plank = WorldMaterials.Solid(new Color(.42f, .33f, .25f)); oldPlank = WorldMaterials.Solid(new Color(.33f, .3f, .27f));
            rust = WorldMaterials.Solid(new Color(.47f, .25f, .15f), .25f); dark = WorldMaterials.Solid(new Color(.06f, .06f, .07f));
            metal = WorldMaterials.Solid(new Color(.55f, .57f, .6f), .75f); tin = WorldMaterials.Solid(new Color(.42f, .45f, .47f), .5f); concrete = WorldMaterials.Solid(new Color(.45f, .46f, .46f));
            water = new Material(WorldMaterials.Glow(new Color(.2f, .36f, .4f), .25f)) { name = "Rainwood water" };
            foam = new Material(WorldMaterials.Glow(Falls, 1f)) { name = "Falling water" };
        }

        Material GlowM(Color c, float s = 2f) => WorldMaterials.Glow(c, s * .65f);
        static bool HasStation(IslandWorldData.Artist a) => a.stationId != 0;
        static float Foot(IslandWorldData.Artist a)
        {
            switch (a.name)
            {
                case "The Smashing Pumpkins": return 19f;
                case "Soundgarden": return 17f;
                case "Audioslave": case "Stone Temple Pilots": return 15f;
                case "Live": case "Nirvana": case "Foo Fighters": return 13f;
                case "Alice In Chains": return 15f;
                case "Pearl Jam": case "Temple Of The Dog": return 11f;
                default: return 8f;
            }
        }

        public override bool Ground(float height, float slope, float patch, out Color color)
        {
            if (slope > 22f) color = Color.Lerp(new Color(.24f, .27f, .26f), new Color(.33f, .36f, .34f), patch);
            else if (slope > 12f) color = Color.Lerp(new Color(.19f, .26f, .19f), new Color(.28f, .31f, .27f), patch);
            else color = Color.Lerp(new Color(.17f, .24f, .17f), new Color(.29f, .25f, .19f), Mathf.SmoothStep(.35f, 1f, patch));
            color.a = 1f;
            return true;
        }

        static float SegDist(Vector2 p, Vector2 a, Vector2 b, out float t)
        {
            var ab = b - a; t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / Mathf.Max(ab.sqrMagnitude, .0001f));
            return Vector2.Distance(p, a + ab * t);
        }

        /// Distance to the river (and the river's water height there).
        float RiverDist(float x, float z, out float y)
        {
            y = 0f; var best = float.MaxValue; var p = new Vector2(x, z);
            for (var i = 0; i + 1 < river2.Count; i++)
            {
                if (Mathf.Abs(river2[i].x - x) > 40f && Mathf.Abs(river2[i + 1].x - x) > 40f) continue;
                var d = SegDist(p, river2[i], river2[i + 1], out var t);
                if (d < best) { best = d; y = Mathf.Lerp(riverY[i], riverY[i + 1], t); }
            }
            return best;
        }

        bool InRiver(float x, float z, float pad) => river2.Count > 1 && RiverDist(x, z, out _) < RiverW / 2f + pad || hasBluff && Vector2.Distance(new Vector2(x, z), pool2) < 13f + pad;
        bool OnBluff(float x, float z, float pad) => hasBluff && Vector2.Distance(new Vector2(x, z), bluff2) < bluffR + pad;

        // ---------------------------------------------------------------- shaping the ground
        public override void ShapeGround(IslandWorldData data, int index)
        {
            var isl = data.layout.islands[index]; heart2 = new Vector2(isl.labelX, isl.labelZ);
            river2.Clear(); riverY.Clear();
            bool Inside(Vector2 p, float m) { if (data.IslandAt(p.x, p.y) != isl) return false; for (var k = 0; k < 16; k++) { var a = k * Mathf.PI / 8f; if (data.IslandAt(p.x + Mathf.Cos(a) * m, p.y + Mathf.Sin(a) * m) != isl) return false; } return true; }
            bool Sea(float x, float z) => data.IslandAt(x, z) == null || data.HeightAt(x, z) < .2f;
            // which way is the sea: the bluff goes the other way
            var seaDir = Vector2.zero;
            for (var k = 0; k < 24; k++) { var a = k * Mathf.PI / 12f; var d = new Vector2(Mathf.Cos(a), Mathf.Sin(a)); for (var r = 40f; r < 600f; r += 12f) if (Sea(heart2.x + d.x * r, heart2.y + d.y * r)) { seaDir += d / r; break; } }
            seaDir = seaDir.sqrMagnitude > 1e-6f ? seaDir.normalized : Vector2.down;
            // the bluff: as big as fits, behind the arrival, away from the sea
            hasBluff = false; var bestScore = float.MinValue;
            for (var R = 75f; R >= 45f; R -= 5f)
                for (var k = 0; k < 24; k++)
                {
                    var a = k * Mathf.PI / 12f; var d = new Vector2(Mathf.Cos(a), Mathf.Sin(a)); var c = heart2 + d * (R + 75f);
                    if (!Inside(c, R + 18f)) continue;
                    var score = R * 2f - Vector2.Dot(d, seaDir) * 60f;
                    if (score > bestScore) { bestScore = score; hasBluff = true; bluff2 = c; bluffR = R; toBluff2 = d; }
                }
            if (hasBluff)
            {
                const float H = 24f;
                for (var r = 0; r < data.Nz; r++)
                {
                    var z = data.layout.depth - r * data.Step; if (Mathf.Abs(z - bluff2.y) > bluffR + 4f) continue;
                    for (var col = 0; col < data.Nx; col++)
                    {
                        var x = col * data.Step; if (Mathf.Abs(x - bluff2.x) > bluffR + 4f) continue;
                        var i = r * data.Nx + col; if (data.islandIds[i] != index) continue;
                        var v = new Vector2(x, z) - bluff2; var ang = Mathf.Atan2(v.y, v.x);
                        var edge = bluffR * (1f + .08f * Mathf.Sin(ang * 4f + 1.3f) + .05f * Mathf.Sin(ang * 9f));
                        var d = v.magnitude / edge; if (d >= 1f) continue;
                        data.heights[i] += H * (1f - Ease(.72f, 1f, d)) + 2f * Mathf.PerlinNoise(x * .05f, z * .05f) * (1f - d);
                    }
                }
                lip2 = bluff2 - toBluff2 * bluffR * .8f;
                pool2 = bluff2 - toBluff2 * (bluffR + 13f);
                var perp = new Vector2(-toBluff2.y, toBluff2.x);
                // the mill on whichever side of the pool has more room
                var m1 = pool2 + perp * 30f - toBluff2 * 4f; var m2 = pool2 - perp * 30f - toBluff2 * 4f;
                mill2 = Inside(m1, 26f) ? m1 : m2;
            }
            // the river: from the pool down to the sea, the shortest way that doesn't pass the arrival or another land
            if (hasBluff)
            {
                List<Vector2> best = null; var bestLen = float.MaxValue;
                // try every point of coast in reach as a goal; walk toward it, steering round the bluff, the arrival and the mill
                for (var k = 0; k < 72; k++)
                {
                    var a = k * Mathf.PI / 36f; var d = new Vector2(Mathf.Cos(a), Mathf.Sin(a)); Vector2? goal = null;
                    for (var r = 30f; r < 700f; r += 8f) { var g = heart2 + d * r; if (Sea(g.x, g.y)) { goal = g; break; } if (data.IslandAt(g.x, g.y) != isl) break; }
                    if (goal == null) continue;
                    var pts = new List<Vector2>(); var ok = false; var q = pool2; var heading = (goal.Value - pool2).normalized;
                    for (var step = 0; step < 160; step++)
                    {
                        var want = (goal.Value - q).normalized;
                        void Push(Vector2 c, float r) { var v = q - c; var dist = v.magnitude; if (dist < r) want += v.normalized * (r - dist) / r * 2.5f; }
                        Push(heart2, 60f); Push(mill2, 34f); Push(bluff2, bluffR + 22f);
                        heading = Vector2.Lerp(heading, want.normalized, .35f).normalized;
                        q += heading * 6f;
                        if (Sea(q.x, q.y)) { pts.Add(q); ok = true; break; }
                        if (data.IslandAt(q.x, q.y) != isl || Vector2.Distance(q, heart2) < 36f || Vector2.Distance(q, mill2) < 22f || Vector2.Distance(q, bluff2) < bluffR + 4f) break;
                        pts.Add(q);
                    }
                    if (ok && s_len(pts) < bestLen) { bestLen = s_len(pts); best = pts; }
                }
                if (best != null)
                {
                    poolY = data.HeightAt(pool2.x, pool2.y) - 1.4f;
                    river2.Add(pool2); riverY.Add(poolY); var y = poolY;
                    foreach (var q in best) { y = Mathf.Min(y, data.HeightAt(q.x, q.y) - 1.1f); river2.Add(q); riverY.Add(Mathf.Max(y, -.6f)); }
                    // smooth the drops a little so the water runs in rapids, not steps
                    for (var pass = 0; pass < 3; pass++) for (var i = 1; i + 1 < riverY.Count; i++) riverY[i] = Mathf.Min(riverY[i - 1], (riverY[i - 1] + riverY[i] * 2f + riverY[i + 1]) / 4f);
                }
            }
            var avoid = new List<Vector3>();
            if (hasBluff)
            {
                avoid.Add(new Vector3(bluff2.x, bluffR + 6f, bluff2.y)); avoid.Add(new Vector3(pool2.x, 20f, pool2.y)); avoid.Add(new Vector3(mill2.x, 24f, mill2.y));
                for (var i = 0; i < river2.Count; i += 3) avoid.Add(new Vector3(river2[i].x, 10f, river2[i].y));
            }
            ArtistPlacement.Spread(data, index, 54f, heart2, 46f, 40f, avoid, null, Land, 330f);
            foreach (var a in data.layout.artists)
            {
                if (a.sceneId != isl.sceneId) continue;
                a.r = Mathf.Max(a.r, Foot(a) + 6f);
                Level(data, index, new Vector2(a.x, a.z), Foot(a) + 1f, Foot(a) + 12f);
            }
            Level(data, index, heart2, 18f, 34f);
            if (hasBluff) { Level(data, index, mill2, 17f, 30f); Level(data, index, pool2, 12f, 20f, -1.6f); poolY = data.HeightAt(pool2.x, pool2.y) + .3f; if (riverY.Count > 0) riverY[0] = poolY; }
            if (river2.Count > 1) CarveRiver(data, index);
        }

        /// 0 below a, 1 above b, a smooth S between (Unity's SmoothStep interpolates between its first two arguments instead).
        static float Ease(float a, float b, float x) { var t = Mathf.Clamp01((x - a) / (b - a)); return t * t * (3f - 2f * t); }

        static float s_len(List<Vector2> p) { var l = 0f; for (var i = 1; i < p.Count; i++) l += Vector2.Distance(p[i - 1], p[i]); return l; }

        void CarveRiver(IslandWorldData data, int index)
        {
            float minX = float.MaxValue, maxX = float.MinValue, minZ = float.MaxValue, maxZ = float.MinValue;
            foreach (var p in river2) { minX = Mathf.Min(minX, p.x); maxX = Mathf.Max(maxX, p.x); minZ = Mathf.Min(minZ, p.y); maxZ = Mathf.Max(maxZ, p.y); }
            const float bank = 12f;
            for (var r = 0; r < data.Nz; r++)
            {
                var z = data.layout.depth - r * data.Step; if (z < minZ - bank || z > maxZ + bank) continue;
                for (var col = 0; col < data.Nx; col++)
                {
                    var x = col * data.Step; if (x < minX - bank || x > maxX + bank) continue;
                    var i = r * data.Nx + col; if (data.islandIds[i] != index) continue;
                    if (Vector2.Distance(new Vector2(x, z), pool2) < 14f) continue;
                    var d = RiverDist(x, z, out var y); if (d > bank) continue;
                    var bed = y - .35f;
                    if (d < RiverW / 2f + .5f) data.heights[i] = Mathf.Min(data.heights[i], bed);
                    else { var t = Mathf.SmoothStep(0, 1, (d - RiverW / 2f - .5f) / (bank - RiverW / 2f - .5f)); data.heights[i] = Mathf.Min(data.heights[i], Mathf.Lerp(y + .3f, data.heights[i], t)); }
                }
            }
        }

        void Level(IslandWorldData data, int index, Vector2 c, float flat, float blend, float drop = 0f)
        {
            var level = data.HeightAt(c.x, c.y) + drop;
            for (var r = 0; r < data.Nz; r++)
            {
                var z = data.layout.depth - r * data.Step; if (Mathf.Abs(z - c.y) > blend) continue;
                for (var col = 0; col < data.Nx; col++)
                {
                    var x = col * data.Step; if (Mathf.Abs(x - c.x) > blend) continue;
                    var i = r * data.Nx + col; if (data.islandIds[i] != index) continue;
                    var d = Vector2.Distance(new Vector2(x, z), c);
                    if (d < flat) data.heights[i] = level;
                    else if (d < blend) data.heights[i] = Mathf.Lerp(level, data.heights[i], Mathf.SmoothStep(0, 1, (d - flat) / (blend - flat)));
                }
            }
        }

        // ---------------------------------------------------------------- dressing
        public override void Dress(LandContext ctx)
        {
            MakeMaterials();
            halos = new GlowHalos(); soft = new MeshKit();
            pathCells.Clear(); walkPoints.Clear(); facing.Clear(); noGrass.Clear();
            center = ctx.Plaza;
            life = new GameObject("Rainwood life").AddComponent<GrungeLife>();
            life.transform.SetParent(ctx.Root, false);
            life.center = center; life.water = water; life.foam = foam;
            var mk = new MeshKit(); var solid = new MeshKit();
            Remember(ctx.OnGround(center.x, center.z), 0);
            if (hasBluff) { FallsAndPool(ctx, mk, solid); Mill(ctx, mk, solid); }
            if (river2.Count > 1) River(ctx, mk, solid);
            Arrival(ctx, mk, solid);
            Roads(ctx, mk, solid);
            Forest(ctx, mk, solid);
            mk.Build("Grunge Rainwood", ctx.Root, false);
            soft.Build("Rainwood pines", ctx.Root, false);
            solid.BuildCollider("Grunge Rainwood walls", ctx.Root);
        }

        public override void Finish(LandContext ctx) { GroundCover(ctx); halos.Build("Rainwood glow", ctx.Root); }

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
            ctx.IsLand(x, z, 4f) && !InFootprint(ctx, x, z, pad) && !NearPath(x, z, pad + 2f) && !InNoGrass(x, z, pad) && !InRiver(x, z, pad + 2f)
            && Vector2.Distance(new Vector2(x, z), new Vector2(center.x, center.z)) > 24f + pad;

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

        /// A road lamp: a creosoted wooden pole with a curved arm and a yellow lamp, the light hazy in the rain.
        void RoadLamp(LandContext ctx, MeshKit mk, Vector3 g, Vector3 arm)
        {
            var up = Vector3.up; arm = Vector3.ProjectOnPlane(arm, up).normalized;
            mk.Prism(bark, g - up * .3f, up, .16f, 6.3f, 6, .8f);
            mk.Box(dark, g + up * 5.9f + arm * .7f, new Vector3(.08f, .08f, 1.5f), Quaternion.LookRotation(arm));
            var bulb = g + up * 5.75f + arm * 1.4f;
            mk.Box(metal, bulb + up * .12f, new Vector3(.5f, .12f, .7f), Quaternion.LookRotation(arm));
            mk.Box(GlowM(Lamp, 3f), bulb, new Vector3(.36f, .06f, .5f), Quaternion.LookRotation(arm));
            halos.Add(bulb - up * .2f, 2.4f, Lamp, .7f); life.lamps.Add(bulb);
            ArtistBits.PostCollider(ctx.Root, g, 6f, .2f);
        }

        // ---------------------------------------------------------------- the bluff and the falls
        /// The waterfall: a stream across the mossy top of the bluff, pouring over the lip in a white glowing ribbon down
        /// the rock face into the dark pool, flanked by black rock and hanging moss; foam where it lands.
        void FallsAndPool(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var dir = new Vector3(-toBluff2.x, 0, -toBluff2.y); var side = Vector3.Cross(up, dir);
            // the falling water follows the face of the bluff from the lip to the pool
            var pts = new List<Vector3>(); var top = ctx.OnGround(bluff2.x - toBluff2.x * bluffR * .45f, bluff2.y - toBluff2.y * bluffR * .45f);
            for (var s = bluffR * .45f; s <= bluffR + 13f; s += 1.2f)
            {
                var q = bluff2 - toBluff2 * s; var y = ctx.Ground(q.x, q.y) + .35f;
                pts.Add(new Vector3(q.x, Mathf.Max(y, poolY + .1f), q.y));
            }
            Strip(mk, pts, foam, 6f, .05f);
            // a second, wider sheet just in front, so the water reads as a thick fall
            var sheet = new List<Vector3>(); foreach (var p in pts) if (p.y > poolY + 1f) sheet.Add(p + dir * .7f + up * .2f);
            if (sheet.Count > 1) Strip(mk, sheet, foam, 4.4f, 0f);
            // the cliff face either side: black rock buttresses and moss
            for (var k = 0; k < 9; k++)
            {
                var s = (k - 4f) * 4.2f; if (Mathf.Abs(s) < 4.4f) continue;
                var q = lip2 + new Vector2(side.x, side.z) * s - toBluff2 * 3f; var g = ctx.OnGround(q.x, q.y);
                var h = Mathf.Max(6f, ctx.Ground(lip2.x, lip2.y) - poolY + 2f);
                mk.Blob(k % 2 == 0 ? darkRock : rock, new Vector3(g.x, poolY + h * .45f, g.z), new Vector3(3.2f, h * .55f, 3f), ctx.Rng, .25f);
                mk.Blob(moss, new Vector3(g.x, poolY + h * .9f, g.z) + dir * .8f, new Vector3(2.2f, .9f, 1.6f), ctx.Rng, .4f);
                solid.Box(rock, new Vector3(g.x, poolY + h * .45f, g.z), new Vector3(5f, h * 1.1f, 5f), Quaternion.LookRotation(dir));
            }
            // the pool
            var pc = new Vector3(pool2.x, poolY, pool2.y);
            for (var k = 0; k < 28; k++) { float a0 = k * Mathf.PI * 2f / 28f, a1 = (k + 1) * Mathf.PI * 2f / 28f; mk.Tri(water, pc, pc + new Vector3(Mathf.Cos(a0), 0, Mathf.Sin(a0)) * 13f, pc + new Vector3(Mathf.Cos(a1), 0, Mathf.Sin(a1)) * 13f, pc - up); }
            for (var k = 0; k < 7; k++) mk.Blob(foam, pc + toV3(toBluff2) * (9f - k * .6f) + side * ctx.Range(-2.5f, 2.5f), new Vector3(1.6f, .12f, 1.2f), ctx.Rng, .3f);
            for (var k = 0; k < 24; k++)
            {
                var a = k * Mathf.PI * 2f / 24f; var off = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)); if (Vector3.Dot(off, toV3(toBluff2)) > .6f) continue;
                if (river2.Count > 1 && Vector2.Angle(new Vector2(off.x, off.z), river2[Mathf.Min(2, river2.Count - 1)] - pool2) < 20f) continue;
                var rp = ctx.OnGround(pool2.x + off.x * 13.6f, pool2.y + off.z * 13.6f); mk.Blob(k % 3 == 0 ? moss : rock, rp + up * .3f, new Vector3(1.4f, .8f, 1.2f), ctx.Rng, .35f);
            }
            halos.Add(pc + toV3(toBluff2) * 8f + up * 4f, 9f, Falls, .3f);
            ArtistBits.PointLight(ctx.Root, pc + up * 6f, Falls, 30f, 1.4f);
            life.fallsTop = top; life.fallsFoot = pc + toV3(toBluff2) * 9f; life.fallsDir = dir;
            noGrass.Add(new Vector3(pool2.x, 15f, pool2.y)); noGrass.Add(new Vector3(lip2.x, 10f, lip2.y));
            for (var s = .45f; s < .8f; s += .1f) noGrass.Add(new Vector3(bluff2.x - toBluff2.x * bluffR * s, 6f, bluff2.y - toBluff2.y * bluffR * s));
            var mark = new GameObject("Shot: the falls").transform; mark.SetParent(ctx.Root, false);
            var cam = ctx.OnGround(pool2.x - toBluff2.x * 34f + side.x * 12f, pool2.y - toBluff2.y * 34f + side.z * 12f) + up * 1.6f; var look = pc + toV3(toBluff2) * 10f + up * 9f - cam;
            mark.SetPositionAndRotation(cam, Quaternion.LookRotation(look.normalized));
        }

        static Vector3 toV3(Vector2 v) => new Vector3(v.x, 0, v.y);

        // ---------------------------------------------------------------- the mill
        /// The old sawmill, now a venue: a long timber hall with a tin roof, its big front door open on a lit stage with
        /// amps and string lights; a rusted smokestack; a giant saw blade over the door; a water wheel in the race on the
        /// pool side, turning; stacked logs and a log carriage by the wall.
        void Mill(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var c = ctx.OnGround(mill2.x, mill2.y);
            var front = new Vector3(center.x - c.x, 0, center.z - c.z).normalized; var rot = Quaternion.LookRotation(front); var side = Vector3.Cross(up, front);
            const float W = 22f, D = 14f, H = 8f, T = .4f, DoorW = 8f, DoorH = 6f;
            Vector3 P(float x, float y, float d) => c + side * x + up * y + front * d;
            mk.Box(oldPlank, P(0, .15f, 0), new Vector3(W, .3f, D), rot); solid.Box(oldPlank, P(0, .15f, 0), new Vector3(W, .3f, D), rot);
            // back, sides (fitted between front and back), front either side of the door, and the header
            void Wall(Vector3 at, Vector3 size) { mk.Box(plank, at, size, rot); solid.Box(plank, at, size, rot); }
            Wall(P(0, H / 2f, -D / 2f + T / 2f), new Vector3(W, H, T));
            foreach (var s in new[] { -1f, 1f }) Wall(P(s * (W / 2f - T / 2f), H / 2f, 0), new Vector3(T, H, D - 2f * T));
            var segW = (W - DoorW) / 2f;
            foreach (var s in new[] { -1f, 1f }) Wall(P(s * (DoorW / 2f + segW / 2f), H / 2f, D / 2f - T / 2f), new Vector3(segW, H, T));
            Wall(P(0, DoorH + (H - DoorH) / 2f, D / 2f - T / 2f), new Vector3(DoorW, H - DoorH, T));
            // vertical battens on the outside
            for (var k = 0; k < 23; k++) { var x = -W / 2f + .5f + k; if (Mathf.Abs(x) < DoorW / 2f + .2f) continue; mk.Box(oldPlank, P(x, H / 2f, D / 2f + .03f), new Vector3(.12f, H, .08f), rot); }
            mk.GableRoof(tin, P(0, H, 0), new Vector3(D + 1.2f, 0, W + 1.2f), 4f, Quaternion.LookRotation(side));
            // the big door slid open to one side on its rail
            mk.Box(rust, P(-DoorW / 2f - segW / 2f, DoorH / 2f, D / 2f + .25f), new Vector3(DoorW * .9f, DoorH, .15f), rot); solid.Box(rust, P(-DoorW / 2f - segW / 2f, DoorH / 2f, D / 2f + .25f), new Vector3(DoorW * .9f, DoorH, .3f), rot);
            mk.Box(dark, P(0, DoorH + .3f, D / 2f + .3f), new Vector3(W - 2f, .15f, .15f), rot);
            // the stage at the back: a platform, amps, a drum riser, a mic, string lights and a warm wash
            mk.Box(dark, P(0, .65f, -D / 2f + 2.6f), new Vector3(13f, 1f, 4.4f), rot); solid.Box(dark, P(0, .65f, -D / 2f + 2.6f), new Vector3(13f, 1f, 4.4f), rot);
            var grille = WorldMaterials.Solid(new Color(.18f, .17f, .16f));
            foreach (var s in new[] { -1f, 1f }) for (var k = 0; k < 2; k++) { var a = P(s * (4.6f + k * 1.4f), 1.15f + .8f, -D / 2f + 1.4f); mk.Box(dark, a, new Vector3(1.3f, 1.6f, .8f), rot); mk.Box(grille, a + front * .41f, new Vector3(1.1f, 1.3f, .02f), rot); }
            mk.Box(oldPlank, P(0, 1.45f, -D / 2f + 1.6f), new Vector3(3f, .6f, 2f), rot);
            foreach (var x in new[] { -.8f, 0f, .8f }) mk.Prism(metal, P(x, 1.75f, -D / 2f + 1.6f), up, .35f, .4f, 12);
            mk.Prism(metal, P(0, 1.15f, -D / 2f + 3.9f), up, .03f, 1.6f, 6); mk.Blob(dark, P(0, 2.8f, -D / 2f + 3.9f), Vector3.one * .1f, ctx.Rng, 0f);
            var stageLight = new Material(WorldMaterials.Glow(Lamp, 1.6f)) { name = "Mill stage" }; life.stage = stageLight;
            for (var k = 0; k <= 24; k++) { var t = k / 24f; var x = Mathf.Lerp(-W / 2f + 1f, W / 2f - 1f, t); var sag = Mathf.Abs(Mathf.Sin(t * Mathf.PI * 3f)) * .7f; mk.Blob(stageLight, P(x, H - 1.6f - sag, -D / 2f + 4.6f), Vector3.one * .12f, ctx.Rng, 0f); }
            // the backdrop: a big flannel-red cloth lit from below, the saw-blade ring painted on it in light
            var backdrop = new Material(WorldMaterials.Glow(new Color(.75f, .2f, .16f), .9f)) { name = "Mill backdrop" }; life.backdrop = backdrop;
            mk.Box(backdrop, P(0, 4f, -D / 2f + T + .05f), new Vector3(13f, 5f, .05f), rot);
            for (var k = 0; k < 6; k++) mk.Box(dark, P(-6f + k * 2.4f, 4f, -D / 2f + T + .09f), new Vector3(.12f, 5f, .02f), rot);
            for (var k = 0; k < 3; k++) mk.Box(dark, P(0, 2.2f + k * 1.6f, -D / 2f + T + .09f), new Vector3(13f, .1f, .02f), rot);
            for (var k = 0; k < 28; k++) { var a0 = k * Mathf.PI * 2f / 28f; var a1 = (k + 1) * Mathf.PI * 2f / 28f; var bc = P(0, 4.4f, -D / 2f + T + .12f); var p0 = bc + (side * Mathf.Cos(a0) + up * Mathf.Sin(a0)) * 1.6f; var p1 = bc + (side * Mathf.Cos(a1) + up * Mathf.Sin(a1)) * 1.6f; mk.Box(stageLight, (p0 + p1) / 2f, new Vector3(.12f, .12f, Vector3.Distance(p0, p1)), Quaternion.LookRotation(p1 - p0, front)); }
            // footlights along the front edge of the stage, and the amps' pilot lights
            for (var k = 0; k < 9; k++) { var fp = P(-5.6f + k * 1.4f, 1.2f, -D / 2f + 4.85f); mk.Box(dark, fp, new Vector3(.4f, .25f, .3f), rot * Quaternion.Euler(-30f, 0, 0)); mk.Box(stageLight, fp + front * .14f + up * .05f, new Vector3(.3f, .16f, .02f), rot * Quaternion.Euler(-30f, 0, 0)); }
            foreach (var s in new[] { -1f, 1f }) for (var k = 0; k < 2; k++) mk.Blob(GlowM(new Color(1f, .2f, .1f), 3f), P(s * (4.6f + k * 1.4f) + .45f, 2.6f, -D / 2f + 1.82f), Vector3.one * .05f, ctx.Rng, 0f);
            halos.Add(P(0, 4f, -D / 2f + 1.2f), 6f, new Color(1f, .45f, .3f), .25f);
            for (var k = 0; k <= 16; k++) { var t = k / 16f; mk.Blob(stageLight, P(Mathf.Lerp(-W / 2f + 1f, W / 2f - 1f, t), H - .5f - Mathf.Sin(t * Mathf.PI) * .7f, 1f), Vector3.one * .09f, ctx.Rng, 0f); }
            ArtistBits.PointLight(ctx.Root, P(0, 5f, -D / 2f + 5f), Lamp, 16f, 2.2f, true);
            ArtistBits.PointLight(ctx.Root, P(0, 4f, 1f), new Color(.9f, .35f, .3f), 12f, 1.2f, true);
            IndoorSpaces.Add(P(0, H / 2f, 0), rot, new Vector3(W - 2f * T, H, D - 2f * T));
            life.stageAt = P(0, 3f, -D / 2f + 3f);
            // a few old benches for the crowd
            for (var k = 0; k < 3; k++) foreach (var s in new[] { -1f, 1f }) { var b = P(s * 3.6f, .55f, 2.5f - k * 2.2f); mk.Box(plank, b, new Vector3(3.6f, .12f, .5f), rot); foreach (var e in new[] { -1.5f, 1.5f }) mk.Box(plank, b + side * e - up * .25f, new Vector3(.12f, .45f, .4f), rot); solid.Box(plank, b - up * .1f, new Vector3(3.6f, .6f, .5f), rot); }
            // the giant saw blade over the door
            var blade = P(0, H + 2f, D / 2f + .3f); mk.Prism(metal, blade, front, 2.4f, .12f, 28); mk.Prism(dark, blade + front * .13f, front, .5f, .04f, 12);
            for (var k = 0; k < 20; k++) { var a = k * 18f; var d = Quaternion.AngleAxis(a, front) * up; mk.Box(metal, blade + d * 2.5f + front * .06f, new Vector3(.3f, .3f, .1f), Quaternion.LookRotation(front, d) * Quaternion.Euler(0, 0, 45f)); }
            var bladeGlow = new Material(WorldMaterials.Glow(Lamp, 1.2f)) { name = "Saw blade ring" }; life.bladeRing = bladeGlow;
            for (var k = 0; k < 28; k++) { var a0 = k * Mathf.PI * 2f / 28f; var a1 = (k + 1) * Mathf.PI * 2f / 28f; var p0 = blade + front * .14f + (side * Mathf.Cos(a0) + up * Mathf.Sin(a0)) * 1.9f; var p1 = blade + front * .14f + (side * Mathf.Cos(a1) + up * Mathf.Sin(a1)) * 1.9f; mk.Box(bladeGlow, (p0 + p1) / 2f, new Vector3(.1f, .1f, Vector3.Distance(p0, p1)), Quaternion.LookRotation(p1 - p0, front)); }
            halos.Add(blade + front * .6f, 3.2f, Lamp, .45f);
            ArtistBits.Label(ctx.Root, "THE MILL", P(0, DoorH + .55f, D / 2f + .1f), front, .55f, Lamp, TextAnchor.MiddleCenter);
            // the smokestack
            var st = P(W / 2f - 2.5f, 0, -D / 2f - 1.6f); mk.Prism(rust, st, up, 1f, 17f, 10, .8f); mk.Prism(dark, st + up * 17f, up, .9f, .8f, 10, 1.15f); solid.Prism(rust, st, up, 1f, 6f, 10);
            for (var k = 1; k < 6; k++) mk.Prism(dark, st + up * (k * 3f), up, 1.02f - k * .035f, .2f, 10);
            life.chimney = st + up * 18f;
            // the water wheel in the race on the pool side
            var poolSide = Vector3.Dot(side, new Vector3(pool2.x - c.x, 0, pool2.y - c.z)) > 0 ? 1f : -1f;
            var wc = P(poolSide * (W / 2f + 1.6f), 3.4f, -1f);
            var wk = new MeshKit();
            for (var k = 0; k < 12; k++) { var r = Quaternion.AngleAxis(k * 30f, Vector3.right); wk.Box(oldPlank, r * new Vector3(0, 2.6f, 0), new Vector3(1.4f, .9f, .12f), r); wk.Box(plank, r * new Vector3(0, 1.3f, 0), new Vector3(.15f, 2.6f, .15f), r); }
            foreach (var x in new[] { -.7f, .7f }) wk.Prism(rust, new Vector3(x, 0, 0), Vector3.right * Mathf.Sign(x), 3f, .1f, 24);
            wk.Prism(dark, new Vector3(-1f, 0, 0), Vector3.right, .35f, 2f, 10);
            var wheel = wk.BuildSingle("Water wheel", life.transform, false).transform; wheel.SetPositionAndRotation(wc, Quaternion.LookRotation(side * poolSide) * Quaternion.Euler(0, -90f, 0)); life.wheel = wheel; life.wheelAxis = side;
            solid.Box(oldPlank, wc, new Vector3(1.6f, 6.4f, 6.4f), rot);
            // the race: a plank flume from the pool to the wheel
            var flumeA = new Vector3(pool2.x, poolY + 1.2f, pool2.y) + (wc - new Vector3(pool2.x, wc.y, pool2.y)).normalized * 11f; var flumeB = wc + up * 2.8f;
            flumeA.y = Mathf.Max(flumeA.y, flumeB.y + .4f);
            var fd = flumeB - flumeA; if (fd.magnitude > 2f)
            {
                var frot = Quaternion.LookRotation(fd); mk.Box(oldPlank, (flumeA + flumeB) / 2f, new Vector3(1.2f, .1f, fd.magnitude), frot); solid.Box(oldPlank, (flumeA + flumeB) / 2f + up * .2f, new Vector3(1.4f, .8f, fd.magnitude), frot);
                foreach (var s in new[] { -.6f, .6f }) mk.Box(oldPlank, (flumeA + flumeB) / 2f + frot * new Vector3(s, .3f, 0), new Vector3(.08f, .6f, fd.magnitude), frot);
                mk.Box(foam, (flumeA + flumeB) / 2f + up * .1f, new Vector3(1f, .05f, fd.magnitude), frot);
                for (var t = .2f; t < 1f; t += .3f) { var p = Vector3.Lerp(flumeA, flumeB, t); var g = ctx.Ground(p.x, p.z); mk.Box(oldPlank, new Vector3(p.x, (p.y + g) / 2f, p.z), new Vector3(.2f, p.y - g, .2f), Quaternion.identity); }
            }
            // logs stacked against the far wall
            var lp = P(-poolSide * (W / 2f + 2.5f), 0, 0);
            for (var row = 0; row < 3; row++) for (var k = 0; k < 4 - row; k++) mk.Prism(bark, lp + up * (.6f + row * 1.05f) + front * (-3f + k * 1.15f + row * .55f) - side * poolSide * 3.5f, side * poolSide, .55f, 7f, 8);
            solid.Box(bark, lp + up * 1.6f - front * 1f, new Vector3(5f, 3.2f, 7.2f), Quaternion.LookRotation(side));
            // lamps either side of the door
            foreach (var s in new[] { -1f, 1f }) { var lpos = P(s * (DoorW / 2f + .8f), DoorH - .4f, D / 2f + .35f); mk.Box(dark, lpos, new Vector3(.3f, .45f, .3f), rot); mk.Box(GlowM(Lamp, 3f), lpos - up * .05f + front * .05f, new Vector3(.22f, .3f, .22f), rot); halos.Add(lpos + front * .4f, 1.6f, Lamp, .8f); }
            noGrass.Add(new Vector3(c.x, 18f, c.z));
            millDoor = P(0, 0, D / 2f + 2f);
            var mark = new GameObject("Shot: the mill").transform; mark.SetParent(ctx.Root, false);
            var cam = P(-6f, 1.6f, D / 2f + 26f); cam.y = ctx.Ground(cam.x, cam.z) + 1.6f; var look = P(0, 5f, 0) - cam; mark.SetPositionAndRotation(cam, Quaternion.LookRotation(look.normalized));
            var mark2 = new GameObject("Shot: the stage in the mill").transform; mark2.SetParent(ctx.Root, false);
            var cam2 = P(2f, 1.7f, D / 2f - 1f); var look2 = P(0, 2.4f, -D / 2f + 2f) - cam2; mark2.SetPositionAndRotation(cam2, Quaternion.LookRotation(look2.normalized));
        }
        Vector3 millDoor;

        // ---------------------------------------------------------------- the river
        void River(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var pts = new List<Vector3>();
            for (var i = 1; i < river2.Count; i++) pts.Add(new Vector3(river2[i].x, riverY[i], river2[i].y));
            if (pts.Count < 2) return;
            Strip(mk, pts, water, RiverW, .02f);
            for (var i = 0; i + 1 < pts.Count; i++)
            {
                var a = pts[i]; var b = pts[i + 1]; var d = b - a; d.y = 0; var sd = Vector3.Cross(Vector3.up, d.normalized);
                if (a.y - b.y > 1.2f && i % 2 == 0) mk.Box(foam, (a + b) / 2f + Vector3.up * .06f, new Vector3(RiverW * .55f, .04f, d.magnitude * .5f), Quaternion.LookRotation(b - a));   // rapids
                if (i % 4 == 1) { var s = i % 8 == 1 ? 1f : -1f; var rp = a + sd * s * (RiverW / 2f + .6f); rp.y = ctx.Ground(rp.x, rp.z); mk.Blob(i % 3 == 0 ? moss : rock, rp + Vector3.up * .3f, new Vector3(1.1f, .6f, 1f), ctx.Rng, .2f); solid.Box(rock, rp + Vector3.up * .4f, new Vector3(2.6f, 1.4f, 2.4f), Quaternion.LookRotation(d)); }
            }
            life.river = pts.ToArray();
        }

        // ---------------------------------------------------------------- arrival
        /// Where you arrive: a muddy crossroads of logging roads with puddles, a carved wooden sign, the coffee shack with
        /// one window lit, a logging truck loaded with logs, flannel shirts on a clothesline, road lamps.
        void Arrival(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var c = ctx.OnGround(center.x, center.z);
            var toFalls = hasBluff ? new Vector3(pool2.x - c.x, 0, pool2.y - c.z).normalized : Vector3.forward; var sd = Vector3.Cross(up, toFalls);
            mk.Prism(mud, c - up * .25f, up, 11f, .33f, 18);
            for (var k = 0; k < 6; k++) { var a = k * 1.1f + .4f; var r = ctx.Range(3f, 8.5f); Puddle(mk, c + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * r + up * .16f, ctx.Range(.8f, 1.6f)); }
            // the sign, facing the arrival, on the side away from the falls
            var g = c - toFalls * 10f; g.y = ctx.Ground(g.x, g.z);
            foreach (var s in new[] { -1f, 1f }) { mk.Prism(bark, g + sd * s * 2.6f - up * .3f, up, .25f, 3.6f, 7); ArtistBits.PostCollider(ctx.Root, g + sd * s * 2.6f, 3.3f, .25f); }
            mk.Box(plank, g + up * 2.6f, new Vector3(5.8f, 1.1f, .2f), Quaternion.LookRotation(toFalls)); solid.Box(plank, g + up * 2.6f, new Vector3(5.8f, 1.1f, .3f), Quaternion.LookRotation(toFalls));
            mk.GableRoof(moss, g + up * 3.3f, new Vector3(1f, 0, 6.4f), .4f, Quaternion.LookRotation(sd));
            ArtistBits.Label(ctx.Root, "THE GRUNGE RAINWOOD", g + up * 2.6f + toFalls * .12f, toFalls, .3f, Lamp, TextAnchor.MiddleCenter);
            // the coffee shack
            var sh = c - toFalls * 6f + sd * 14f; sh.y = ctx.Ground(sh.x, sh.z); var srot = Quaternion.LookRotation(-sd);
            mk.Box(oldPlank, sh + up * 1.6f, new Vector3(4.4f, 3.2f, 3.4f), srot); solid.Box(oldPlank, sh + up * 1.6f, new Vector3(4.4f, 3.2f, 3.4f), srot);
            mk.GableRoof(tin, sh + up * 3.2f, new Vector3(4f, 0, 5f), 1f, Quaternion.LookRotation(sd));
            mk.Box(GlowM(Lamp, 2.6f), sh + up * 1.8f - sd * 1.71f + toFalls * .8f, new Vector3(1.4f, .9f, .02f), srot);
            mk.Box(dark, sh + up * 1.8f - sd * 1.71f - toFalls * .9f, new Vector3(1f, .8f, .02f), srot);   // the other window, dark
            mk.Box(plank, sh + up * 1.1f - sd * 2f + toFalls * .8f, new Vector3(1.8f, .1f, .5f), srot);    // the serving ledge
            mk.Box(WorldMaterials.Solid(Flannel), sh + up * 2.6f - sd * 2.1f, new Vector3(4.6f, .1f, 1f), srot * Quaternion.Euler(-20f, 0, 0));
            ArtistBits.Label(ctx.Root, "COFFEE", sh + up * 3.85f - sd * 1.2f, -sd, .32f, new Color(1f, .55f, .3f), TextAnchor.MiddleCenter);
            halos.Add(sh + up * 1.8f - sd * 2.2f + toFalls * .8f, 2f, Lamp, .7f); ArtistBits.PointLight(ctx.Root, sh + up * 2f - sd * 3f, Lamp, 9f, 1.6f);
            foreach (var k in new[] { -1.5f, 1.5f }) { var st = sh - sd * 3.2f + toFalls * k; st.y = ctx.Ground(st.x, st.z); mk.Prism(dark, st, up, .06f, .7f, 6); mk.Prism(plank, st + up * .7f, up, .3f, .08f, 10); ArtistBits.PostCollider(ctx.Root, st, .8f, .3f); }
            noGrass.Add(new Vector3(sh.x, 4.5f, sh.z));
            // the logging truck, loaded
            var tr = c + toFalls * 4f - sd * 15f; tr.y = ctx.Ground(tr.x, tr.z); LogTruck(ctx, mk, solid, tr, toFalls);
            // a clothesline of flannel shirts
            var cl = c - toFalls * 13f - sd * 9f; cl.y = ctx.Ground(cl.x, cl.z); Clothesline(ctx, mk, solid, cl, sd, 7f);
            // a fingerpost pointing to the biggest artists, Alice In Chains first
            Fingerpost(ctx, mk, c - toFalls * 6f - sd * 7f);
            // lamps round the crossroads
            for (var k = 0; k < 4; k++) { var a = k * Mathf.PI / 2f + Mathf.PI / 4f; var lp = ctx.OnGround(c.x + Mathf.Cos(a) * 12.5f, c.z + Mathf.Sin(a) * 12.5f); RoadLamp(ctx, mk, lp, new Vector3(c.x - lp.x, 0, c.z - lp.z)); }
            noGrass.Add(new Vector3(c.x, 12f, c.z));
            var mark = new GameObject("Shot: the crossroads").transform; mark.SetParent(ctx.Root, false);
            var cam = c - toFalls * 16f - sd * 4f + up * 1.7f; var look = c + toFalls * 20f + up * 4f - cam; mark.SetPositionAndRotation(cam, Quaternion.LookRotation(look.normalized));
        }

        /// A tall fingerpost with an arm pointing toward each of the land's best-known artists, lettered both sides and
        /// lit by a lamp on top.
        void Fingerpost(LandContext ctx, MeshKit mk, Vector3 g)
        {
            var up = Vector3.up; g.y = ctx.Ground(g.x, g.z);
            mk.Prism(bark, g - up * .3f, up, .14f, 5f, 7); ArtistBits.PostCollider(ctx.Root, g, 4.6f, .2f);
            var names = new[] { "Alice In Chains", "The Smashing Pumpkins", "Soundgarden", "Stone Temple Pilots", "Pearl Jam", "Audioslave", "Nirvana", "Live" };
            var k = 0;
            foreach (var name in names)
            {
                IslandWorldData.Artist a = null; foreach (var x in ctx.Artists) if (x.name == name) a = x;
                if (a == null) continue;
                var d = new Vector3(a.x - g.x, 0, a.z - g.z).normalized; var y = 4.3f - k * .42f; var arm = g + up * y + d * 1f; var rot = Quaternion.LookRotation(d);
                mk.Box(k == 0 ? WorldMaterials.Solid(Flannel) : plank, arm, new Vector3(.06f, .32f, 1.9f), rot); mk.Cone(k == 0 ? WorldMaterials.Solid(Flannel) : plank, arm + d * .95f, .16f, .35f, 3, 0f);
                var sd = Vector3.Cross(up, d);
                foreach (var e in new[] { -1f, 1f }) ArtistBits.Label(ctx.Root, name.ToUpperInvariant(), arm + sd * e * .04f, sd * e, .13f, Ink, TextAnchor.MiddleCenter);
                k++;
            }
            var lamp = g + up * 4.85f; mk.Box(dark, lamp + up * .2f, new Vector3(.4f, .08f, .4f), Quaternion.identity); mk.Box(GlowM(Lamp, 2.6f), lamp, new Vector3(.25f, .3f, .25f), Quaternion.identity);
            halos.Add(lamp, 2f, Lamp, .7f); ArtistBits.PointLight(ctx.Root, lamp + up * .3f, Lamp, 10f, 1.6f);
        }

        void Puddle(MeshKit mk, Vector3 at, float r)
        {
            var puddle = WorldMaterials.Solid(new Color(.16f, .19f, .21f), .97f);
            mk.Prism(puddle, at - Vector3.up * .06f, Vector3.up, r, .04f, 12);
        }

        /// A logging truck: a cab and a long trailer with stakes, loaded with big logs and chained down.
        void LogTruck(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 at, Vector3 dir)
        {
            var up = Vector3.up; var rot = Quaternion.LookRotation(dir); var sd = Vector3.Cross(up, dir);
            var cab = WorldMaterials.Solid(new Color(.55f, .14f, .12f), .5f);
            mk.Box(cab, at + dir * 5f + up * 1.9f, new Vector3(2.5f, 2.2f, 2.4f), rot); mk.Box(cab, at + dir * 6.9f + up * 1.4f, new Vector3(2.4f, 1.3f, 1.6f), rot);
            mk.Box(WorldMaterials.Solid(new Color(.15f, .2f, .25f), .95f), at + dir * 6.21f + up * 2.4f, new Vector3(2.2f, .8f, .02f), rot);
            mk.Box(metal, at + dir * 7.75f + up * 1.2f, new Vector3(2.3f, .9f, .1f), rot);
            foreach (var s in new[] { -.8f, .8f }) { mk.Prism(GlowM(Lamp, 2.6f), at + dir * 7.8f + up * 1.3f + sd * s, dir, .16f, .05f, 10); halos.Add(at + dir * 8.1f + up * 1.3f + sd * s, .9f, Lamp, .6f); }
            mk.Prism(metal, at + dir * 4.2f + sd * 1.3f + up * 1f, up, .12f, 2.8f, 6);
            mk.Box(dark, at + up * 1.1f - dir * 2f, new Vector3(2.4f, .3f, 10f), rot);
            for (var k = 0; k < 4; k++) foreach (var s in new[] { -1.15f, 1.15f }) mk.Box(dark, at + up * 2.3f - dir * (6f - k * 2.6f) + sd * s, new Vector3(.12f, 2.2f, .12f), rot);
            foreach (var w in new[] { 6.4f, 3.6f, -4f, -5.4f }) foreach (var s in new[] { -1.15f, 1.15f }) mk.Prism(dark, at + dir * w + sd * s * 1.05f + up * .55f, sd * Mathf.Sign(s), .55f, .35f, 12);
            for (var row = 0; row < 2; row++) for (var k = 0; k < 3 - row; k++) mk.Prism(bark, at + up * (1.8f + row * .85f) + sd * ((k - (2 - row) / 2f) * .9f) - dir * 7.4f, dir, .45f, 10.5f, 8);
            solid.Box(cab, at + dir * 1.2f + up * 1.6f, new Vector3(2.8f, 3.2f, 15f), rot);
        }

        /// Two posts and a sagging line of flannel shirts, red, blue and green.
        void Clothesline(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 a, Vector3 dir, float len)
        {
            var up = Vector3.up; dir = Vector3.ProjectOnPlane(dir, up).normalized; var b = a + dir * len; b.y = ctx.Ground(b.x, b.z);
            solid.Box(dark, (a + b) / 2f + up * 1.7f, new Vector3(.3f, 1.2f, len), Quaternion.LookRotation(dir));
            foreach (var p in new[] { a, b }) { mk.Prism(oldPlank, p - up * .3f, up, .1f, 2.6f, 6); mk.Box(oldPlank, p + up * 2.2f, new Vector3(.8f, .08f, .08f), Quaternion.LookRotation(Vector3.Cross(up, dir))); ArtistBits.PostCollider(ctx.Root, p, 2.3f, .12f); }
            Color[] cols = { Flannel, new Color(.18f, .26f, .45f), new Color(.2f, .36f, .22f), new Color(.6f, .45f, .2f), Flannel * .8f };
            var n = Mathf.FloorToInt(len / 1.1f) - 1; var rot = Quaternion.LookRotation(Vector3.Cross(up, dir));
            for (var k = 1; k <= n; k++)
            {
                var t = k / (float)(n + 1); var p = Vector3.Lerp(a, b, t) + up * (2.2f - Mathf.Sin(t * Mathf.PI) * .35f);
                var col = cols[(k + Mathf.RoundToInt(a.x)) % cols.Length]; var m = WorldMaterials.Solid(col);
                mk.Box(m, p - up * .4f, new Vector3(.75f, .8f, .06f), Quaternion.LookRotation(Vector3.Cross(up, dir)) * Quaternion.Euler(0, 90f, 0));
                mk.Box(WorldMaterials.Solid(col * .6f), p - up * .4f, new Vector3(.77f, .06f, .07f), Quaternion.LookRotation(Vector3.Cross(up, dir)) * Quaternion.Euler(0, 90f, 0));   // the check
                foreach (var s in new[] { -1f, 1f }) mk.Box(m, p - up * .3f + dir * s * .5f, new Vector3(.25f, .55f, .06f), Quaternion.LookRotation(Vector3.Cross(up, dir)) * Quaternion.Euler(0, 90f, s * 20f));
            }
            mk.Box(dark, (a + b) / 2f + up * 2.05f, new Vector3(.02f, .02f, len), Quaternion.LookRotation(dir));
        }

        // ---------------------------------------------------------------- roads
        void Roads(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var lamps = new List<Vector3>(); var walk = 1;
            void Lay(Vector3 join, Vector3 to, float stopShort, string artist)
            {
                var from = new Vector3(join.x, 0, join.z); to.y = 0;
                var dir = to - from; var total = dir.magnitude; if (total < 2f) return; dir /= total;
                if (artist != null) facing[artist] = -dir;
                var side = Vector3.Cross(Vector3.up, dir); var end = total - stopShort;
                var pts = new List<Vector3>(); var wet = new List<bool>();
                for (var d = walk == 1 ? 11f : RoadW / 2f + .1f; ; d += 1.5f)
                {
                    var dd = Mathf.Min(d, end); var q = from + dir * dd + side * Mathf.Sin(dd / 17f) * Mathf.Min(3.5f, end * .05f);
                    if (!ctx.IsLand(q.x, q.z, 0f)) break;
                    pts.Add(ctx.OnGround(q.x, q.z)); wet.Add(InRiver(q.x, q.z, 1.5f)); if (dd >= end) break;
                }
                if (pts.Count < 2) return;
                for (var i = 0; i < pts.Count; i++)
                {
                    if (!wet[i]) continue;
                    var j = i; while (j + 1 < pts.Count && wet[j + 1]) j++;
                    var i0 = Mathf.Max(0, i - 1); var i1 = Mathf.Min(pts.Count - 1, j + 1);
                    var deck = Mathf.Max(pts[i0].y, pts[i1].y) + .9f;
                    for (var k = i0; k <= i1; k++) { var p = pts[k]; p.y = deck; pts[k] = p; }
                    var a = pts[i0] - dir * 1.5f; var b = pts[i1] + dir * 1.5f; a.y = b.y = deck; PlankBridge(ctx, mk, solid, a, b);
                    i = j + 1;
                }
                Strip(mk, pts, mud, RoadW, .2f, ctx);
                // wheel ruts and puddles in the mud
                for (var i = 3; i < pts.Count; i += 9) if (!wet[i] && pts.Count > i) Puddle(mk, pts[i] + side * (i % 2 == 0 ? .7f : -.7f) + Vector3.up * .26f, .55f);
                foreach (var p in pts) Remember(p, walk);
                for (var i = 6; i < pts.Count; i += 12)
                {
                    var lp = pts[i] + side * ((i / 12) % 2 == 0 ? 1f : -1f) * (RoadW / 2f + .8f);
                    var near = false; foreach (var o in lamps) if ((o - lp).sqrMagnitude < 17f * 17f) near = true;
                    if (near || InRiver(lp.x, lp.z, 1f)) continue;
                    lamps.Add(lp); RoadLamp(ctx, mk, ctx.OnGround(lp.x, lp.z), pts[i] - lp);
                }
                walk++;
            }
            if (hasBluff) Lay(ctx.OnGround(center.x, center.z), millDoor, 0f, null);
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

        /// A plank footbridge with log rails.
        void PlankBridge(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 a, Vector3 b)
        {
            var up = Vector3.up; var dir = b - a; var len = dir.magnitude; dir /= len; var rot = Quaternion.LookRotation(dir); var side = Vector3.Cross(up, new Vector3(dir.x, 0, dir.z).normalized); var mid = (a + b) / 2f;
            for (var k = 0; k < Mathf.RoundToInt(len / .5f); k++) mk.Box(k % 3 == 0 ? oldPlank : plank, a + dir * (k * .5f + .25f), new Vector3(3.6f, .14f, .46f), rot);
            solid.Box(plank, mid - up * .05f, new Vector3(3.6f, .3f, len), rot);
            foreach (var s in new[] { -1f, 1f })
            {
                mk.Prism(bark, mid + side * s * 1.8f + up * 1f - dir * len / 2f, dir, .1f, len, 6);
                for (var t = 0f; t <= len; t += 2f) { var p = a + dir * t + side * s * 1.8f; mk.Prism(bark, p - up * 1.4f, up, .12f, 2.5f, 6); }
                solid.Box(bark, mid + side * s * 1.85f + up * .6f, new Vector3(.25f, 1.2f, len), rot);
            }
        }

        // ---------------------------------------------------------------- the forest
        /// Tall dark pines in thick stands with clearings between, fallen mossy logs, stumps, boulders, rusted trailers
        /// with clotheslines, log piles, old cars sinking into the moss, fire lookout towers, phone booths, benches under
        /// a lamp.
        void Forest(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            int pines = 0, logs = 0, stumps = 0, rocks = 0, trailers = 0, piles = 0, cars = 0, towers = 0, booths = 0, benches = 0;
            const float cell = 11f;
            for (var x = ctx.MinX; x < ctx.MaxX; x += cell)
            for (var z = ctx.MinZ; z < ctx.MaxZ; z += cell)
            {
                float px = x + ctx.Range(0f, cell), pz = z + ctx.Range(0f, cell);
                if (!Open(ctx, px, pz, 2.5f)) continue;
                var p = ctx.OnGround(px, pz); var roll = ctx.Range(0f, 1f);
                var stand = Mathf.PerlinNoise(px * .012f + 3f, pz * .012f + 9f);   // thick stands and clearings
                var top = OnBluff(px, pz, -8f);
                if (roll < .012f && towers < 3 && !top && Open(ctx, px, pz, 6f)) { Lookout(ctx, mk, solid, p); towers++; }
                else if (roll < .03f && trailers < 10 && !top && Open(ctx, px, pz, 6f)) { Trailer(ctx, mk, solid, p, null); trailers++; }
                else if (roll < .04f && booths < 4 && NearPath(px, pz, 8f) && !top) { PhoneBooth(ctx, mk, solid, p); booths++; }
                else if (roll < .07f && benches < 14 && NearPath(px, pz, 9f)) { Bench(ctx, mk, solid, p); benches++; }
                else if (roll < .08f && piles < 10 && !top) { LogPile(ctx, mk, solid, p); piles++; }
                else if (roll < .09f && cars < 7 && !top) { OldCar(ctx, mk, solid, p); cars++; }
                else if (roll < .09f + stand * .72f) { Pine(ctx, p, ctx.Range(.85f, 1.45f)); pines++; if (ctx.Chance(.3f)) { var q = ctx.OnGround(px + ctx.Range(-4.5f, 4.5f), pz + ctx.Range(-4.5f, 4.5f)); if (Open(ctx, q.x, q.z, 1.5f)) { Pine(ctx, q, ctx.Range(.6f, 1.1f)); pines++; } } }
                else
                {
                    var r2 = ctx.Range(0f, 1f);
                    if (r2 < .1f) { FallenLog(ctx, mk, solid, p); logs++; }
                    else if (r2 < .17f) { var sr = ctx.Range(.4f, .8f); mk.Prism(bark, p - Vector3.up * .3f, Vector3.up, sr, ctx.Range(.6f, 1.2f), 8); mk.Blob(moss, p + Vector3.up * .8f, new Vector3(sr * .8f, .15f, sr * .8f), ctx.Rng, .1f); ArtistBits.PostCollider(ctx.Root, p, 1.2f, sr * 1.15f); stumps++; }
                    else if (r2 < .23f) { var s = ctx.Range(1f, 2.6f); mk.Blob(rock, p + Vector3.up * s * .4f, new Vector3(s, s * .7f, s * .9f), ctx.Rng, .3f); mk.Blob(moss, p + Vector3.up * s * .95f, new Vector3(s * .8f, s * .2f, s * .7f), ctx.Rng, .4f); solid.Box(rock, p + Vector3.up * s * .45f, new Vector3(s * 2.7f, s * 1.7f, s * 2.6f), Quaternion.identity); rocks++; }
                }
            }
            Debug.Log($"The Grunge Rainwood: bluff {(hasBluff ? bluffR.ToString("0") + " m" : "none")}, river {river2.Count} points, {pines} pines, {logs} logs, {stumps} stumps, {rocks} boulders, {trailers} trailers, {piles} log piles, {cars} cars, {towers} lookouts, {booths} phone booths, {benches} benches");
        }

        /// A tall Pacific pine: a straight dark trunk, bare low down, with tiers of drooping dark-green boughs.
        void Pine(LandContext ctx, Vector3 at, float s)
        {
            var up = Vector3.up; var h = 17f * s;
            soft.Prism(bark, at - up * .4f, up, .4f * s, h * .75f, 7, .5f);
            var tiers = 6; var m = ctx.Chance(.5f) ? pine : pineDark;
            for (var k = 0; k < tiers; k++)
            {
                var t = k / (float)(tiers - 1); var y = h * (.32f + t * .58f) + ctx.Range(-.35f, .35f); var r = Mathf.Lerp(3.4f, .9f, t) * s;
                soft.Cone(m, at + up * y, r, h * .2f, 7, ctx.Range(0, 50f));
            }
            soft.Cone(m, at + up * h * .9f, .7f * s, h * .16f, 6);
            ArtistBits.PostCollider(ctx.Root, at, h * .6f, .4f * s);
        }

        void FallenLog(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 at)
        {
            var up = Vector3.up; var d = Quaternion.Euler(0, ctx.Range(0, 180f), 0) * Vector3.forward; var len = ctx.Range(5f, 9f); var r = ctx.Range(.35f, .6f);
            var a = at - d * len / 2f; a.y = ctx.Ground(a.x, a.z) + r * .7f; var b = at + d * len / 2f; b.y = ctx.Ground(b.x, b.z) + r * .7f;
            mk.Prism(bark, a, b - a, r, Vector3.Distance(a, b), 8);
            for (var k = 0; k < 3; k++) mk.Blob(moss, Vector3.Lerp(a, b, .2f + k * .3f) + up * r * .8f, new Vector3(r * 1.1f, r * .3f, r * 1.4f), ctx.Rng, .15f);
            mk.Prism(WorldMaterials.Solid(new Color(.45f, .36f, .26f)), b - (b - a).normalized * .01f, b - a, r * .9f, .02f, 8);
            solid.Box(bark, (a + b) / 2f, new Vector3(r * 3f, r * 2.6f, Vector3.Distance(a, b) + .3f), Quaternion.LookRotation(b - a));
        }

        void LogPile(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 at)
        {
            var up = Vector3.up; var d = Quaternion.Euler(0, ctx.Range(0, 180f), 0) * Vector3.forward; var sd = Vector3.Cross(up, d);
            for (var row = 0; row < 3; row++) for (var k = 0; k < 4 - row; k++) mk.Prism(bark, at + up * (.45f + row * .8f) + sd * ((k - (3 - row) / 2f) * .85f) - d * 3f, d, .42f, 6f, 8);
            for (var k = 0; k < 2; k++) mk.Blob(moss, at + up * 2.3f + d * (k - .5f) * 2f, new Vector3(.9f, .2f, 1f), ctx.Rng, .4f);
            solid.Box(bark, at + up * 1.2f, new Vector3(3.6f, 2.4f, 6f), Quaternion.LookRotation(d));
        }

        /// A rusted old car sinking into the moss, ferns growing through it.
        void OldCar(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 at)
        {
            var up = Vector3.up; var d = Quaternion.Euler(0, ctx.Range(0, 360f), 0) * Vector3.forward; var rot = Quaternion.LookRotation(d) * Quaternion.Euler(ctx.Range(-4f, 4f), 0, ctx.Range(-5f, 5f));
            Color[] paint = { new Color(.35f, .4f, .42f), new Color(.45f, .3f, .2f), new Color(.3f, .35f, .28f) };
            var body = WorldMaterials.Solid(paint[ctx.Rng.Next(paint.Length)], .3f);
            mk.Box(body, at + up * .55f, new Vector3(1.9f, .7f, 4.4f), rot); mk.Box(body, at + up * 1.15f - d * .3f, new Vector3(1.7f, .6f, 2.2f), rot);
            mk.Box(dark, at + up * 1.15f - d * .3f, new Vector3(1.72f, .4f, 2f), rot);
            mk.Blob(rust, at + up * .9f + d * 1.5f, new Vector3(.6f, .2f, .5f), ctx.Rng, .5f); mk.Blob(moss, at + up * 1.5f - d * .4f, new Vector3(.8f, .15f, 1f), ctx.Rng, .4f);
            solid.Box(body, at + up * .8f, new Vector3(2f, 1.6f, 4.5f), rot);
        }

        /// A rusted trailer on blocks: a rounded tin box with a lit window (or a dark one), a door step, an awning, a
        /// lawn chair and a clothesline. On its own in the woods, or as an artist's home.
        void Trailer(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 at, Material window, Vector3? faceDir = null)
        {
            var up = Vector3.up; var face = faceDir ?? new Vector3(center.x - at.x, 0, center.z - at.z).normalized; var rot = Quaternion.LookRotation(face); var sd = Vector3.Cross(up, face);
            Color[] paint = { new Color(.62f, .6f, .52f), new Color(.45f, .55f, .52f), new Color(.6f, .5f, .38f) };
            var shell = WorldMaterials.Solid(paint[ctx.Rng.Next(paint.Length)], .35f);
            var c = at + up * 1.7f;
            mk.Box(shell, c, new Vector3(7f, 2.4f, 2.6f), rot); foreach (var s in new[] { -1f, 1f }) mk.Prism(shell, c + sd * s * 3.5f - up * 1.2f, up, 1.3f, 2.4f, 10, 1f, 0f);
            mk.Box(rust, c - up * .85f, new Vector3(7.1f, .3f, 2.65f), rot);
            mk.Box(window ?? (ctx.Chance(.6f) ? GlowM(Lamp, 1.8f) : dark), c + up * .2f + face * 1.31f + sd * 1.4f, new Vector3(1.2f, .8f, .02f), rot);
            mk.Box(dark, c - up * .1f + face * 1.31f - sd * 1.1f, new Vector3(.8f, 1.9f, .02f), rot);
            mk.Box(concrete, at + up * .2f + face * 1.8f - sd * 1.1f, new Vector3(1f, .4f, .8f), rot);
            mk.Box(WorldMaterials.Solid(new Color(.3f, .45f, .4f)), c + up * 1.2f + face * 1.9f, new Vector3(4f, .06f, 1.6f), rot * Quaternion.Euler(-12f, 0, 0));
            foreach (var s in new[] { -1.8f, 1.8f }) mk.Prism(metal, at + face * 2.6f + sd * s, up, .04f, 2.8f, 6);
            foreach (var s in new[] { -2.5f, 0f, 2.5f }) mk.Box(concrete, at + up * .2f + sd * s, new Vector3(.5f, .5f, 1.8f), rot);
            solid.Box(shell, c - up * .4f, new Vector3(9.8f, 3.4f, 2.8f), rot); solid.Box(shell, at + up * .2f + face * 1.8f - sd * 1.1f, new Vector3(1f, .4f, .8f), rot);
            // a lawn chair
            var ch = at + face * 3.4f + sd * 2.4f; ch.y = ctx.Ground(ch.x, ch.z); var chair = WorldMaterials.Solid(new Color(.3f, .5f, .45f));
            mk.Box(chair, ch + up * .45f, new Vector3(.6f, .06f, .6f), rot); mk.Box(chair, ch + up * .8f - face * .3f, new Vector3(.6f, .7f, .06f), rot * Quaternion.Euler(-15f, 0, 0));
            foreach (var s in new[] { -.28f, .28f }) mk.Box(metal, ch + up * .22f + sd * s, new Vector3(.04f, .45f, .6f), rot);
            if (ctx.Chance(.5f)) { var cl = at + face * 4.5f - sd * 3.5f; cl.y = ctx.Ground(cl.x, cl.z); Clothesline(ctx, mk, solid, cl, sd, 5f); }
            halos.Add(c + up * .2f + face * 1.6f + sd * 1.4f, 1.3f, Lamp, .5f);
            noGrass.Add(new Vector3(at.x, 5f, at.z));
        }

        /// A fire lookout: a cabin with windows all round on a tall timber tower, its lamp lit.
        void Lookout(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 at)
        {
            var up = Vector3.up; const float H = 13f;
            foreach (var a in new[] { -1f, 1f }) foreach (var b in new[] { -1f, 1f }) { var p = at + new Vector3(a * 2f, 0, b * 2f); mk.Box(oldPlank, p + up * H / 2f + new Vector3(-a * .3f, 0, -b * .3f), new Vector3(.3f, H, .3f), Quaternion.Euler(b * 2.5f, 0, -a * 2.5f)); ArtistBits.PostCollider(ctx.Root, p, H, .25f); }
            for (var k = 1; k < 4; k++) { var y = k * H / 4f; foreach (var r in new[] { 0f, 90f }) mk.Box(oldPlank, at + up * y, new Vector3(4.2f - k * .3f, .15f, .15f), Quaternion.Euler(0, r, 0) * Quaternion.Euler(0, 0, 0)); foreach (var r in new[] { 0f, 90f }) mk.Box(oldPlank, at + up * (y - H / 8f), new Vector3(.12f, H / 3.6f, .12f), Quaternion.Euler(0, r, 45f)); }
            var cab = at + up * (H + 1.4f); solid.Box(oldPlank, at + up * 2f, new Vector3(4.6f, 4f, 4.6f), Quaternion.identity);
            mk.Box(plank, cab - up * 1.3f, new Vector3(4f, .25f, 4f), Quaternion.identity);
            mk.Box(plank, cab - up * .8f, new Vector3(3.2f, .9f, 3.2f), Quaternion.identity);
            mk.Box(GlowM(Lamp, 1.6f), cab + up * .3f, new Vector3(3.1f, 1.2f, 3.1f), Quaternion.identity);
            mk.Box(tin, cab + up * 1.05f, new Vector3(3.6f, .15f, 3.6f), Quaternion.identity); mk.Cone(tin, cab + up * 1.1f, 2.6f, 1.2f, 4, 45f);
            halos.Add(cab + up * .3f, 3.4f, Lamp, .45f); life.lamps.Add(cab + up * .3f);
            noGrass.Add(new Vector3(at.x, 4f, at.z));
        }

        void PhoneBooth(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 at)
        {
            var up = Vector3.up; var face = new Vector3(center.x - at.x, 0, center.z - at.z).normalized; var rot = Quaternion.LookRotation(face);
            mk.Box(metal, at + up * 1.2f, new Vector3(1.1f, 2.4f, 1.1f), rot); mk.Box(GlowM(new Color(.75f, .9f, 1f), 1.4f), at + up * 1.3f + face * .56f, new Vector3(.9f, 1.8f, .02f), rot);
            mk.Box(GlowM(Lamp, 2.4f), at + up * 2.3f, new Vector3(1f, .2f, 1f), rot); halos.Add(at + up * 2.4f, 1.6f, new Color(.8f, .9f, 1f), .5f);
            solid.Box(metal, at + up * 1.2f, new Vector3(1.1f, 2.4f, 1.1f), rot);
        }

        void Bench(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 at)
        {
            var up = Vector3.up; var d = Quaternion.Euler(0, ctx.Range(0, 360f), 0) * Vector3.forward; var rot = Quaternion.LookRotation(d); var sd = Vector3.Cross(up, d);
            mk.Box(oldPlank, at + up * .5f, new Vector3(1.8f, .1f, .5f), Quaternion.LookRotation(d)); mk.Box(oldPlank, at + up * .9f - d * .25f, new Vector3(1.8f, .4f, .08f), rot);
            foreach (var s in new[] { -.8f, .8f }) mk.Box(dark, at + up * .25f + sd * s, new Vector3(.1f, .5f, .45f), rot);
            solid.Box(oldPlank, at + up * .5f, new Vector3(1.9f, 1f, .6f), rot);
            RoadLamp(ctx, mk, at + sd * 1.6f - d * .4f, -sd);
        }

        void GroundCover(LandContext ctx)
        {
            var mat = WindMesh.Material(.25f, 1f, new Color(.2f, .32f, .2f));
            var meshes = new Dictionary<(int, int), WindMesh>(); var rng = new System.Random(1991);
            const float step = 2.6f;
            for (var x = ctx.MinX; x < ctx.MaxX; x += step)
            for (var z = ctx.MinZ; z < ctx.MaxZ; z += step)
            {
                var px = x + ((float)rng.NextDouble() - .5f) * step; var pz = z + ((float)rng.NextDouble() - .5f) * step;
                if (!ctx.IsLand(px, pz, 1f) || NearPath(px, pz, 2.8f) || InNoGrass(px, pz, 0f) || InFootprint(ctx, px, pz, -2f) || InRiver(px, pz, 1.5f)) continue;
                var n = Mathf.PerlinNoise(px * .04f + 5f, pz * .04f + 1f); if (n < .38f) continue;
                var key = (Mathf.FloorToInt(px / 70f), Mathf.FloorToInt(pz / 70f));
                if (!meshes.TryGetValue(key, out var wm)) meshes[key] = wm = new WindMesh();
                var at = new Vector3(px, ctx.Ground(px, pz) - .04f, pz);
                var col = Color.Lerp(new Color(.14f, .26f, .15f), new Color(.26f, .38f, .2f), (float)rng.NextDouble());
                if (n > .55f && rng.NextDouble() < .55) wm.Bracken(at, .7f + (float)rng.NextDouble() * .6f, col, Color.Lerp(col, new Color(.35f, .5f, .25f), .4f), rng, 6);
                else wm.Tuft(at, .3f + (float)rng.NextDouble() * .3f, col * .9f, col, rng, 3, .07f);
            }
            foreach (var kv in meshes) kv.Value.Build("Rainwood ferns", ctx.Root, mat);
        }
    }
}
