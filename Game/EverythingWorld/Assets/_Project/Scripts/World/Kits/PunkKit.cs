using System.Collections.Generic;
using UnityEngine;

namespace EverythingWorld.World
{
    /// The Punk Badlands (WORLD_GUIDE.md): teenage summer. Sun-bleached badlands at a harsh, bright noon: flat-topped
    /// mesas striped with rock bands, scrub, and the ruins of a suburb the skaters took over. You arrive in a cracked
    /// cul-de-sac (a basketball hoop, battered mailboxes, shopping carts, a town sign); beside it a giant drained
    /// swimming pool has become a skatepark, kids riding its walls; smaller skate bowls are carved into the dirt all
    /// round, with wooden ramps and kickers. Roofless house shells, graffiti walls, old couches, fences, swing sets,
    /// trampolines, an ice-cream truck, tumbleweeds. Checkerboard floors for the ska bands. Reads by a white-hot sky
    /// and pale ochre ground against bright graffiti (the J-Rock Isles next door is neon dusk, the Rainwood dark
    /// forest). Files: PunkKit (ground, arrival, the pool, bowls, ruins, paths, wilds), PunkArtists, PunkSongIds,
    /// PunkLife.
    public partial class PunkKit : LandKit
    {
        public override string Land => "The Punk Badlands";
        public override Vector3 ArtistFront(IslandWorldData data, IslandWorldData.Artist a) => facing.TryGetValue(a.name, out var f) ? f : base.ArtistFront(data, a);

        public static readonly Color Hot = new Color(1f, .3f, .55f), Lime = new Color(.55f, 1f, .3f), Sky = new Color(.3f, .75f, 1f), Sun = new Color(1f, .82f, .25f), Ink = new Color(.1f, .1f, .12f);
        static readonly Color[] Spray = { new Color(1f, .3f, .55f), new Color(.55f, 1f, .3f), new Color(.3f, .75f, 1f), new Color(1f, .82f, .25f), new Color(1f, .45f, .15f), new Color(.7f, .4f, 1f) };
        const float PathW = 3f, PoolR = 26f, PoolDepth = 4.2f;

        Material asphalt, concrete, concreteDark, coping, tile, wood, woodDark, metal, dark, white, rust, scrub;
        GlowHalos halos; Vector3 center; PunkLife life;
        readonly Dictionary<(int, int), List<Vector3>> pathCells = new Dictionary<(int, int), List<Vector3>>();
        readonly List<(Vector3 p, int w)> walkPoints = new List<(Vector3, int)>();
        readonly Dictionary<string, Vector3> facing = new Dictionary<string, Vector3>();
        readonly List<Vector3> noGrass = new List<Vector3>();
        readonly List<Vector3> bowls = new List<Vector3>();   // x, radius, z
        readonly List<Vector3> mesas = new List<Vector3>();   // x, radius, z
        Vector2 heart2, pool2; bool hasPool; float poolTop;

        void MakeMaterials()
        {
            asphalt = WorldMaterials.Solid(new Color(.32f, .31f, .3f)); concrete = WorldMaterials.Solid(new Color(.78f, .76f, .72f)); concreteDark = WorldMaterials.Solid(new Color(.55f, .53f, .5f));
            coping = WorldMaterials.Solid(new Color(.92f, .9f, .86f)); tile = WorldMaterials.Solid(new Color(.45f, .72f, .85f));
            wood = WorldMaterials.Solid(new Color(.72f, .56f, .38f)); woodDark = WorldMaterials.Solid(new Color(.38f, .28f, .2f));
            metal = WorldMaterials.Solid(new Color(.65f, .67f, .7f), .7f); dark = WorldMaterials.Solid(new Color(.08f, .08f, .09f)); white = WorldMaterials.Solid(new Color(.95f, .95f, .93f));
            rust = WorldMaterials.Solid(new Color(.55f, .3f, .2f)); scrub = WorldMaterials.Solid(new Color(.55f, .55f, .32f));
        }

        Material GlowM(Color c, float s = 2f) => WorldMaterials.Glow(c, s * .65f);
        Material Paint(int i) => WorldMaterials.Solid(Spray[((i % Spray.Length) + Spray.Length) % Spray.Length]);
        static bool HasStation(IslandWorldData.Artist a) => a.stationId != 0;
        static readonly HashSet<string> Ska = new HashSet<string> { "Streetlight Manifesto", "Reel Big Fish", "Less Than Jake", "Goldfinger", "The Interrupters" };
        static float Foot(IslandWorldData.Artist a)
        {
            switch (a.name)
            {
                case "Green Day": return 18f;
                case "Fall Out Boy": return 16f;
                case "Panic! At The Disco": return 15f;
                case "The Offspring": return 13f;
                default: return HasStation(a) ? 12f : 8f;
            }
        }

        public override bool Ground(float height, float slope, float patch, out Color color)
        {
            if (height < 2.6f) color = Color.Lerp(new Color(.82f, .74f, .58f), new Color(.88f, .8f, .64f), patch);
            else if (slope > 22f)
            {
                // the mesas' sides: bands of rock, red, cream and ochre
                var band = Mathf.FloorToInt(height / 3.2f) % 4;
                Color[] bands = { new Color(.75f, .45f, .32f), new Color(.88f, .78f, .6f), new Color(.8f, .58f, .36f), new Color(.68f, .38f, .3f) };
                color = Color.Lerp(bands[band], bands[band] * .9f, patch);
            }
            else color = Color.Lerp(new Color(.8f, .7f, .52f), new Color(.88f, .78f, .58f), Mathf.SmoothStep(0f, 1f, patch));   // sun-bleached dirt
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
            var isl = data.layout.islands[index]; heart2 = new Vector2(isl.labelX, isl.labelZ); bowls.Clear(); mesas.Clear();
            bool Inside(Vector2 p, float m) { if (data.IslandAt(p.x, p.y) != isl) return false; for (var k = 0; k < 16; k++) { var a = k * Mathf.PI / 8f; if (data.IslandAt(p.x + Mathf.Cos(a) * m, p.y + Mathf.Sin(a) * m) != isl || data.HeightAt(p.x + Mathf.Cos(a) * m, p.y + Mathf.Sin(a) * m) < 2.5f) return false; } return true; }
            // the pool: beside the arrival, on the flattest ground that fits
            hasPool = false; var best = float.MaxValue;
            for (var k = 0; k < 16; k++)
            {
                var a = k * Mathf.PI / 8f; var c = heart2 + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 62f; if (!Inside(c, PoolR + 14f)) continue;
                float lo = float.MaxValue, hi = float.MinValue; for (var j = 0; j < 12; j++) { var b = j * Mathf.PI / 6f; var h = data.HeightAt(c.x + Mathf.Cos(b) * PoolR, c.y + Mathf.Sin(b) * PoolR); lo = Mathf.Min(lo, h); hi = Mathf.Max(hi, h); }
                if (hi - lo < best) { best = hi - lo; pool2 = c; hasPool = true; }
            }
            // mesas: on the higher ground away from the heart, flat tops with steep banded sides
            var cands = new List<(Vector2 p, float h)>();
            for (var r = 0; r < data.Nz; r += 3)
            {
                var z = data.layout.depth - r * data.Step; if (Mathf.Abs(z - heart2.y) > 520f) continue;
                for (var col = 0; col < data.Nx; col += 3)
                {
                    var x = col * data.Step; if (Mathf.Abs(x - heart2.x) > 520f) continue;
                    var i = r * data.Nx + col; if (data.islandIds[i] != index) continue;
                    var d = Vector2.Distance(new Vector2(x, z), heart2); if (d < 170f || d > 500f) continue;
                    cands.Add((new Vector2(x, z), data.heights[i]));
                }
            }
            cands.Sort((p, q) => q.h.CompareTo(p.h));
            foreach (var (p, h) in cands)
            {
                if (mesas.Count >= 5) break;
                var far = true; foreach (var m in mesas) if (Vector2.Distance(p, new Vector2(m.x, m.z)) < 130f) far = false;
                if (!far) continue;
                for (var R = 55f; R >= 32f; R -= 8f) if (Inside(p, R + 16f)) { mesas.Add(new Vector3(p.x, R, p.y)); break; }
            }
            foreach (var m in mesas)
            {
                var R = m.y; var top = data.HeightAt(m.x, m.z) + 14f;
                for (var r = 0; r < data.Nz; r++)
                {
                    var z = data.layout.depth - r * data.Step; if (Mathf.Abs(z - m.z) > R + 16f) continue;
                    for (var col = 0; col < data.Nx; col++)
                    {
                        var x = col * data.Step; if (Mathf.Abs(x - m.x) > R + 16f) continue;
                        var i = r * data.Nx + col; if (data.islandIds[i] != index) continue;
                        var ang = Mathf.Atan2(z - m.z, x - m.x); var d = Vector2.Distance(new Vector2(x, z), new Vector2(m.x, m.z)) / (1f + .12f * Mathf.Sin(ang * 4f + m.x));
                        var w = 1f - Ease(R - 4f, R + 14f, d);
                        if (w > 0f) data.heights[i] = Mathf.Max(data.heights[i], Mathf.Lerp(data.heights[i], top, w));
                    }
                }
            }
            var avoid = new List<Vector3>();
            if (hasPool) avoid.Add(new Vector3(pool2.x, PoolR + 12f, pool2.y));
            foreach (var m in mesas) avoid.Add(new Vector3(m.x, m.y + 10f, m.z));
            ArtistPlacement.Spread(data, index, 54f, heart2, 46f, 40f, avoid, null, Land, 440f);
            foreach (var a in data.layout.artists)
            {
                if (a.sceneId != isl.sceneId) continue;
                a.r = Mathf.Max(a.r, Foot(a) + 6f);
                Level(data, index, new Vector2(a.x, a.z), Foot(a) + 1f, Foot(a) + 10f);
            }
            Level(data, index, heart2, 18f, 32f);
            // the pool itself: a level deck round it, then the basin dug out, shallow end to deep end, walls curving
            // down into a flat floor
            if (hasPool)
            {
                Level(data, index, pool2, PoolR + 6f, PoolR + 22f);
                poolTop = data.HeightAt(pool2.x, pool2.y);
                var axis = (pool2 - heart2).normalized;
                for (var r = 0; r < data.Nz; r++)
                {
                    var z = data.layout.depth - r * data.Step; if (Mathf.Abs(z - pool2.y) > PoolR + 2f) continue;
                    for (var col = 0; col < data.Nx; col++)
                    {
                        var x = col * data.Step; if (Mathf.Abs(x - pool2.x) > PoolR + 2f) continue;
                        var i = r * data.Nx + col; if (data.islandIds[i] != index) continue;
                        var rel = new Vector2(x, z) - pool2; var along = Vector2.Dot(rel, axis); var across = Vector2.Dot(rel, new Vector2(-axis.y, axis.x));
                        var d = Mathf.Sqrt(along * along / (PoolR * PoolR) + across * across / (PoolR * .62f * PoolR * .62f));
                        if (d >= 1f) continue;
                        var depth = Mathf.Lerp(PoolDepth * .6f, PoolDepth, (along / PoolR + 1f) * .5f);
                        data.heights[i] = poolTop - depth * Ease(1f, .55f, d);
                    }
                }
            }
        }

        void Level(IslandWorldData data, int index, Vector2 c, float flat, float blend)
        {
            var level = Mathf.Max(data.HeightAt(c.x, c.y), data.layout.waterLevel + 2.6f);
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
            life = new GameObject("Punk life").AddComponent<PunkLife>();
            life.transform.SetParent(ctx.Root, false);
            life.center = center;
            var mk = new MeshKit(); var solid = new MeshKit();
            Remember(ctx.OnGround(center.x, center.z), 0);
            Arrival(ctx, mk, solid);
            if (hasPool) Pool(ctx, mk, solid);
            Paths(ctx, mk, solid);
            Wilds(ctx, mk, solid);
            mk.Build("The Punk Badlands", ctx.Root, false);
            solid.BuildCollider("The Punk Badlands walls", ctx.Root);
        }

        public override void Finish(LandContext ctx) { Scrub(ctx); halos.Build("Badlands glow", ctx.Root); }

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

        bool OnMesa(float x, float z, float pad) { foreach (var m in mesas) if (Vector2.Distance(new Vector2(x, z), new Vector2(m.x, m.z)) < m.y + pad) return true; return false; }

        bool Open(LandContext ctx, float x, float z, float pad) =>
            ctx.IsLand(x, z, 5f) && !InFootprint(ctx, x, z, pad) && !NearPath(x, z, pad + 2f) && !InNoGrass(x, z, pad)
            && Vector2.Distance(new Vector2(x, z), new Vector2(center.x, center.z)) > 22f + pad
            && (!hasPool || Vector2.Distance(new Vector2(x, z), pool2) > PoolR + 8f + pad);

        void Strip(MeshKit mk, List<Vector3> pts, Material m, float w, float lift, LandContext ground)
        {
            var n = pts.Count; var L = new Vector3[n]; var R = new Vector3[n];
            Vector3 Flat(Vector3 v) { v.y = 0; return v.normalized; }
            for (var i = 0; i < n; i++)
            {
                var a = i > 0 ? Flat(pts[i] - pts[i - 1]) : Flat(pts[1] - pts[0]);
                var b = i < n - 1 ? Flat(pts[i + 1] - pts[i]) : Flat(pts[n - 1] - pts[n - 2]);
                var t = (a + b).normalized; var side = Vector3.Cross(Vector3.up, t);
                var miter = 1f / Mathf.Max(.6f, Vector3.Dot(side, Vector3.Cross(Vector3.up, b)));
                L[i] = pts[i] - side * (w / 2f * miter); R[i] = pts[i] + side * (w / 2f * miter);
                float gl = ground.Ground(L[i].x, L[i].z), gr = ground.Ground(R[i].x, R[i].z), bulge = Mathf.Clamp(pts[i].y - (gl + gr) / 2f, 0f, .3f);
                L[i].y = gl + bulge + lift; R[i].y = gr + bulge + lift;
            }
            for (var i = 0; i + 1 < n; i++) mk.Quad(m, L[i], R[i], R[i + 1], L[i + 1], (pts[i] + pts[i + 1]) / 2f - Vector3.up);
        }

        /// A graffiti wall: a slab of breeze blocks covered in colour, bubble letters and drips.
        void GraffitiWall(LandContext ctx, MeshKit mk, Vector3 g, Vector3 face, float w, int seed)
        {
            var up = Vector3.up; var rot = Quaternion.LookRotation(face); var sd = Vector3.Cross(up, face); const float H = 2.6f;
            mk.Box(concreteDark, g + up * (H / 2f - .2f), new Vector3(w, H + .4f, .4f), rot); BoxCol(ctx.Root, g + up * (H / 2f - .2f), new Vector3(w, H + .4f, .4f), rot);
            var rng = new System.Random(seed);
            for (var k = 0; k < Mathf.CeilToInt(w * 1.2f); k++)
            {
                var x = ((float)rng.NextDouble() - .5f) * (w - .6f); var y = .5f + (float)rng.NextDouble() * (H - .9f); var s = .4f + (float)rng.NextDouble() * .8f;
                foreach (var f in new[] { 1f, -1f }) mk.Box(Paint(seed + k), g + up * y + sd * x + face * f * (.205f + k * .013f), new Vector3(s * 1.4f, s, .004f), rot * Quaternion.Euler(0, 0, ((float)rng.NextDouble() - .5f) * 30f));
                if (k % 3 == 0) mk.Box(Paint(seed + k), g + up * (y - s * .7f) + sd * x + face * (.205f + k * .013f + .004f), new Vector3(.06f, s * .7f, .004f), rot);
            }
        }

        /// A shopping cart, tipped or upright.
        void Cart(LandContext ctx, MeshKit mk, Vector3 g, float yaw, bool tipped)
        {
            var rot = Quaternion.Euler(tipped ? 80f : 0f, yaw, 0); var up = rot * Vector3.up; var o = g + (tipped ? Vector3.up * .45f : Vector3.zero);
            for (var k = 0; k < 4; k++) mk.Box(metal, o + up * .7f + rot * new Vector3(k % 2 == 0 ? -.3f : .3f, 0, k < 2 ? -.45f : .45f), new Vector3(.03f, .5f, .03f), rot);
            foreach (var y in new[] { .5f, .95f }) { mk.Box(metal, o + up * y + rot * new Vector3(0, 0, -.45f), new Vector3(.62f, .03f, .03f), rot); mk.Box(metal, o + up * y + rot * new Vector3(0, 0, .45f), new Vector3(.62f, .03f, .03f), rot); foreach (var s in new[] { -.3f, .3f }) mk.Box(metal, o + up * y + rot * new Vector3(s, 0, 0), new Vector3(.03f, .03f, .92f), rot); }
            mk.Box(metal, o + up * 1.05f + rot * new Vector3(0, 0, -.55f), new Vector3(.6f, .05f, .05f), rot);
            BoxCol(ctx.Root, o + up * .6f, new Vector3(.7f, 1.1f, 1f), rot);
        }

        /// A battered mailbox on a post, leaning.
        void Mailbox(LandContext ctx, MeshKit mk, Vector3 g, int i)
        {
            var tilt = Quaternion.Euler(((i * 37) % 21) - 10f, i * 53f, ((i * 17) % 15) - 7f);
            mk.Box(woodDark, g + tilt * Vector3.up * .55f, new Vector3(.1f, 1.1f, .1f), tilt);
            mk.Box(Paint(i), g + tilt * Vector3.up * 1.15f, new Vector3(.3f, .32f, .55f), tilt);
            mk.Box(WorldMaterials.Solid(new Color(.85f, .15f, .15f)), g + tilt * (Vector3.up * 1.25f + Vector3.right * .17f), new Vector3(.03f, .3f, .06f), tilt);
            ArtistBits.PostCollider(ctx.Root, g, 1.2f, .2f);
        }

        // ---------------------------------------------------------------- arrival
        /// Where you arrive: a cracked cul-de-sac turning circle, a basketball hoop on a pole, a row of battered
        /// mailboxes, shopping carts, a town sign with the land's name, the drained pool just beyond.
        void Arrival(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var c = ctx.OnGround(center.x, center.z);
            var toPool = hasPool ? new Vector3(pool2.x - c.x, 0, pool2.y - c.z).normalized : Vector3.forward; var sd = Vector3.Cross(up, toPool);
            mk.Prism(asphalt, c - up * .25f, up, 14f, .33f, 24);
            for (var k = 0; k < 8; k++) { var a = k * .8f; var p = c + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * (4f + k); mk.Box(dark, p + up * .09f, new Vector3(.06f, .02f, 3f + k * .3f), Quaternion.Euler(0, a * 70f, 0)); }   // cracks
            mk.Prism(concreteDark, c - up * .2f, up, 14.4f, .3f, 24, 1f);
            var hoop = c - toPool * 12f + sd * 6f; hoop.y = ctx.Ground(hoop.x, hoop.z);
            mk.Prism(metal, hoop - up * .2f, up, .1f, 3.4f, 6); ArtistBits.PostCollider(ctx.Root, hoop, 3.2f, .14f);
            mk.Box(white, hoop + up * 3.4f + toPool * .3f, new Vector3(1.8f, 1.1f, .06f), Quaternion.LookRotation(toPool));
            for (var k = 0; k < 12; k++) { var a = k * Mathf.PI / 6f; mk.Box(WorldMaterials.Solid(new Color(1f, .4f, .1f)), hoop + up * 3.05f + toPool * .55f + (sd * Mathf.Cos(a) + toPool * Mathf.Sin(a)) * .23f, new Vector3(.12f, .03f, .03f), Quaternion.LookRotation(sd * -Mathf.Sin(a) + toPool * Mathf.Cos(a))); }
            for (var k = 0; k < 5; k++) Mailbox(ctx, mk, ctx.OnGround(c.x - toPool.x * 15f + sd.x * (k - 2f) * 3.4f, c.z - toPool.z * 15f + sd.z * (k - 2f) * 3.4f), k);
            Cart(ctx, mk, ctx.OnGround(c.x + sd.x * 10f, c.z + sd.z * 10f), 30f, false); Cart(ctx, mk, ctx.OnGround(c.x + sd.x * 11.5f + toPool.x * 2f, c.z + sd.z * 11.5f + toPool.z * 2f), 120f, true);
            // the town sign
            var s = c - sd * 15f; s.y = ctx.Ground(s.x, s.z); var rot = Quaternion.LookRotation(sd);
            foreach (var e in new[] { -2.4f, 2.4f }) { mk.Prism(metal, s + toPool * e - up * .2f, up, .08f, 3.6f, 6); ArtistBits.PostCollider(ctx.Root, s + toPool * e, 3.4f, .1f); }
            mk.Box(WorldMaterials.Solid(new Color(.2f, .45f, .3f)), s + up * 3f, new Vector3(5.8f, 1.6f, .1f), rot);
            mk.Box(white, s + up * 3f + sd * .06f, new Vector3(5.5f, 1.3f, .01f), rot); mk.Box(WorldMaterials.Solid(new Color(.2f, .45f, .3f)), s + up * 3f + sd * .07f, new Vector3(5.3f, 1.1f, .01f), rot);
            ArtistBits.Label(ctx.Root, "WELCOME TO THE PUNK BADLANDS", s + up * 3.2f + sd * .08f, sd, .26f, new Color(.95f, .95f, .93f), TextAnchor.MiddleCenter);
            ArtistBits.Label(ctx.Root, "POP. EVERYONE", s + up * 2.75f + sd * .08f, sd, .2f, new Color(.95f, .95f, .93f), TextAnchor.MiddleCenter);
            mk.Box(Paint(0), s + up * 2.9f + sd * .09f + toPool * 1.8f, new Vector3(1.2f, .5f, .01f), rot * Quaternion.Euler(0, 0, 18f));   // a tag across it
            noGrass.Add(new Vector3(c.x, 16f, c.z));
            var mark = new GameObject("Shot: the arrival and the pool").transform; mark.SetParent(ctx.Root, false);
            var cam = c - toPool * 9f + sd * 3f + up * 1.7f; mark.SetPositionAndRotation(cam, Quaternion.LookRotation((toPool * 60f - up * 6f).normalized));
        }

        // ---------------------------------------------------------------- the drained pool
        /// The giant drained swimming pool: a deck of pale concrete round it, white coping along the rim, the walls
        /// tiled sky blue to halfway, a ladder at the shallow end, the stump of a diving board at the deep end,
        /// graffiti all over the floor and walls, a few dead leaves; kids skating its walls (PunkLife).
        void Pool(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var c = new Vector3(pool2.x, poolTop, pool2.y); var axis = new Vector3(pool2.x - heart2.x, 0, pool2.y - heart2.y).normalized; var across = Vector3.Cross(up, axis);
            Vector3 Rim(float a, float scale) => c + (axis * Mathf.Cos(a) * PoolR + across * Mathf.Sin(a) * PoolR * .62f) * scale;
            for (var k = 0; k < 64; k++)
            {
                float a0 = k * Mathf.PI / 32f, a1 = (k + 1) * Mathf.PI / 32f;
                var p0 = Rim(a0, 1.02f); var p1 = Rim(a1, 1.02f); p0.y = p1.y = poolTop + .12f;
                mk.Box(coping, (p0 + p1) / 2f, new Vector3(.6f, .25f, Vector3.Distance(p0, p1) + .05f), Quaternion.LookRotation(p1 - p0));
                // the deck round it
                var d0 = Rim(a0, 1.25f); var d1 = Rim(a1, 1.25f); d0.y = d1.y = poolTop + .04f;
                mk.Box(concrete, (Rim(a0, 1.14f) + Rim(a1, 1.14f)) / 2f + up * (poolTop + .02f - c.y), new Vector3(PoolR * .25f * (1f + Mathf.Abs(Mathf.Cos(a0)) * .3f), .1f, Vector3.Distance(d0, d1) + .3f), Quaternion.LookRotation(d1 - d0));
                // blue tiles on the upper wall
                var w0 = Rim(a0, .96f); var w1 = Rim(a1, .96f); var g0 = ctx.Ground(w0.x, w0.z); var g1 = ctx.Ground(w1.x, w1.z);
                mk.Quad(tile, new Vector3(w0.x, poolTop - .05f, w0.z), new Vector3(w1.x, poolTop - .05f, w1.z), new Vector3(w1.x, Mathf.Max(g1, poolTop - 1.4f) + .05f, w1.z), new Vector3(w0.x, Mathf.Max(g0, poolTop - 1.4f) + .05f, w0.z), c - up * 3f);
            }
            // the floor and lower walls: pale concrete laid over the basin in small squares, each corner on the ground
            var floorM = WorldMaterials.Solid(new Color(.82f, .86f, .88f));
            for (var u = -PoolR; u < PoolR; u += 2f)
                for (var v = -PoolR * .62f; v < PoolR * .62f; v += 2f)
                {
                    Vector3 Q(float a, float b) { var q = c + axis * a + across * b; var e = (a * a) / (PoolR * PoolR) + (b * b) / (PoolR * .62f * PoolR * .62f); q.y = e < .92f ? ctx.Ground(q.x, q.z) + .14f : poolTop - .05f; return q; }
                    float e0 = (u + 1f) * (u + 1f) / (PoolR * PoolR) + (v + 1f) * (v + 1f) / (PoolR * .62f * PoolR * .62f); if (e0 > .95f) continue;
                    mk.Quad(floorM, Q(u, v), Q(u + 2f, v), Q(u + 2f, v + 2f), Q(u, v + 2f), c - up * 20f);
                }
            // graffiti on the floor and the walls
            for (var k = 0; k < 40; k++)
            {
                var a = ctx.Range(0, Mathf.PI * 2f); var r = Mathf.Sqrt(ctx.Range(0f, 1f)) * .8f; var p = c + (axis * Mathf.Cos(a) * PoolR + across * Mathf.Sin(a) * PoolR * .62f) * r; p = ctx.OnGround(p.x, p.z) + up * (.18f + k * .012f);
                mk.Blob(Paint(k), p, new Vector3(ctx.Range(.6f, 2f), .02f, ctx.Range(.4f, 1.4f)), ctx.Rng, .3f);
            }
            // the ladder at the shallow end, the diving board at the deep end
            var shallow = Rim(Mathf.PI, .98f); shallow.y = poolTop; var deep = Rim(0f, 1.15f); deep.y = poolTop;
            foreach (var e in new[] { -.35f, .35f }) mk.Box(metal, shallow + across * e - up * 1f + axis * .15f, new Vector3(.05f, 2.4f, .05f), Quaternion.LookRotation(axis) * Quaternion.Euler(-12f, 0, 0));
            for (var k = 0; k < 4; k++) mk.Box(metal, shallow - up * (k * .5f) + axis * (.25f + k * .1f), new Vector3(.7f, .04f, .04f), Quaternion.LookRotation(axis));
            mk.Box(concreteDark, deep + up * .4f, new Vector3(1f, .8f, 1.2f), Quaternion.LookRotation(axis)); solid.Box(concreteDark, deep + up * .4f, new Vector3(1f, .8f, 1.2f), Quaternion.LookRotation(axis));
            mk.Box(white, deep + up * .85f - axis * 1.6f, new Vector3(.6f, .1f, 2.4f), Quaternion.LookRotation(axis));
            // a fence round the deck, gaps where the paths come in; a lifeguard chair
            for (var k = 0; k < 40; k++)
            {
                if (k % 10 == 0 || k % 10 == 5) continue;
                float a0 = k * Mathf.PI / 20f, a1 = (k + 1) * Mathf.PI / 20f; var p0 = Rim(a0, 1.42f); var p1 = Rim(a1, 1.42f); p0 = ctx.OnGround(p0.x, p0.z); p1 = ctx.OnGround(p1.x, p1.z);
                mk.Prism(metal, p0 - up * .1f, up, .04f, 1.6f, 5);
                mk.Box(WorldMaterials.Solid(new Color(.6f, .62f, .65f), .3f), (p0 + p1) / 2f + up * .85f, new Vector3(.02f, 1.4f, Vector3.Distance(p0, p1)), Quaternion.LookRotation(p1 - p0));
                solid.Box(metal, (p0 + p1) / 2f + up * .8f, new Vector3(.1f, 1.6f, Vector3.Distance(p0, p1)), Quaternion.LookRotation(p1 - p0));
            }
            var chair = Rim(Mathf.PI * .5f, 1.2f); chair = ctx.OnGround(chair.x, chair.z);
            foreach (var e in new[] { -.5f, .5f }) foreach (var f in new[] { -.5f, .5f }) mk.Box(white, chair + up * 1.2f + axis * e + across * f, new Vector3(.08f, 2.4f, .08f), Quaternion.identity);
            mk.Box(WorldMaterials.Solid(new Color(.9f, .2f, .2f)), chair + up * 2.5f, new Vector3(1.1f, .1f, 1.1f), Quaternion.identity); mk.Box(WorldMaterials.Solid(new Color(.9f, .2f, .2f)), chair + up * 3f - across * .5f, new Vector3(1.1f, 1f, .08f), Quaternion.LookRotation(across));
            BoxCol(ctx.Root, chair + up * 1.3f, new Vector3(1.2f, 2.6f, 1.2f), Quaternion.identity);
            life.pool = c; life.poolAxis = axis; life.poolR = PoolR; life.poolDepth = PoolDepth;
            noGrass.Add(new Vector3(c.x, PoolR * 1.5f, c.z));
            ArtistBits.PointLight(ctx.Root, c + up * 6f, new Color(1f, .95f, .85f), 30f, .8f);
            var mark = new GameObject("Shot: the drained pool").transform; mark.SetParent(ctx.Root, false);
            var cam = Rim(Mathf.PI * .75f, 1.3f) + up * 3f; mark.SetPositionAndRotation(cam, Quaternion.LookRotation((c - up * 2f - cam).normalized));
        }

        // ---------------------------------------------------------------- paths
        /// Cracked suburban sidewalks: pale concrete slabs with dark joints, branching from the arrival to every artist;
        /// streetlights and the odd fire hydrant along them.
        void Paths(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var walk = 1; var lamps = new List<Vector3>(); var up = Vector3.up;
            void Lay(Vector3 join, Vector3 to, float stopShort, string artist)
            {
                var from = new Vector3(join.x, 0, join.z); to.y = 0;
                var dir = to - from; var total = dir.magnitude; if (total < 2f) return; dir /= total;
                if (artist != null) facing[artist] = -dir;
                var side = Vector3.Cross(Vector3.up, dir); var end = total - stopShort;
                var pts = new List<Vector3>();
                for (var d = walk == 1 ? 15f : PathW / 2f + .1f; ; d += 1.5f)
                {
                    var dd = Mathf.Min(d, end); var q = from + dir * dd + side * Mathf.Sin(dd / 14f) * Mathf.Min(2.5f, end * .04f);
                    if (!ctx.IsLand(q.x, q.z, 0f)) break;
                    pts.Add(ctx.OnGround(q.x, q.z)); if (dd >= end) break;
                }
                if (pts.Count < 2) return;
                Strip(mk, pts, concrete, PathW, .14f, ctx);
                for (var i = 2; i < pts.Count; i += 2) { var a = pts[i] - pts[i - 1]; a.y = 0; if (a.sqrMagnitude < .01f) continue; var sdd = Vector3.Cross(Vector3.up, a.normalized); float gl = ctx.Ground(pts[i].x - sdd.x * PathW / 2f, pts[i].z - sdd.z * PathW / 2f), gr = ctx.Ground(pts[i].x + sdd.x * PathW / 2f, pts[i].z + sdd.z * PathW / 2f); mk.Box(concreteDark, new Vector3(pts[i].x, (gl + gr) / 2f + Mathf.Clamp(pts[i].y - (gl + gr) / 2f, 0f, .3f) + .16f, pts[i].z), new Vector3(PathW, .02f, .06f), Quaternion.LookRotation(a)); }
                foreach (var p in pts) Remember(p, walk);
                for (var i = 9; i < pts.Count; i += 16)
                {
                    var lp = pts[i] + side * ((i / 16) % 2 == 0 ? 1f : -1f) * (PathW / 2f + .7f);
                    var near = false; foreach (var o in lamps) if ((o - lp).sqrMagnitude < 20f * 20f) near = true;
                    if (near || InFootprint(ctx, lp.x, lp.z, 1f)) continue;
                    lamps.Add(lp); var g = ctx.OnGround(lp.x, lp.z); var inward = -side * ((i / 16) % 2 == 0 ? 1f : -1f);
                    mk.Prism(metal, g - up * .2f, up, .09f, 5.6f, 6); mk.Box(metal, g + up * 5.4f + inward * .6f, new Vector3(.08f, .08f, 1.2f), Quaternion.LookRotation(inward));
                    mk.Box(GlowM(new Color(1f, .95f, .8f), 1.4f), g + up * 5.3f + inward * 1.1f, new Vector3(.4f, .1f, .3f), Quaternion.LookRotation(inward));
                    ArtistBits.PostCollider(ctx.Root, g, 5.4f, .12f); life.streetlights.Add(g + up * 5.3f + inward * 1.1f);
                    if (((i / 16) + walk) % 3 == 0) { var h = ctx.OnGround(lp.x + dir.x * 2f, lp.z + dir.z * 2f); mk.Prism(WorldMaterials.Solid(new Color(.9f, .2f, .15f)), h - up * .1f, up, .18f, .8f, 8); mk.Blob(WorldMaterials.Solid(new Color(.9f, .2f, .15f)), h + up * .75f, Vector3.one * .2f, ctx.Rng, 0f); ArtistBits.PostCollider(ctx.Root, h, .8f, .22f); }
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

        // ---------------------------------------------------------------- the wilds
        /// Skate bowls carved into the dirt, wooden ramps and kickers, roofless house shells, graffiti walls, couches
        /// left out, swing sets, trampolines, old cars on blocks, an ice-cream truck, tumbleweeds, scrub, rocks.
        void Wilds(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            int bowlN = 0, ramps = 0, shells = 0, walls = 0, couches = 0, swings = 0, trampolines = 0, cars = 0, carts = 0, rocks = 0; var truck = false;
            const float cell = 22f;
            for (var x = ctx.MinX; x < ctx.MaxX; x += cell)
            for (var z = ctx.MinZ; z < ctx.MaxZ; z += cell)
            {
                float px = x + ctx.Range(0f, cell), pz = z + ctx.Range(0f, cell);
                if (!Open(ctx, px, pz, 3f)) continue;
                var p = ctx.OnGround(px, pz); var roll = ctx.Range(0f, 1f); var face = Quaternion.Euler(0, ctx.Range(0, 360f), 0) * Vector3.forward;
                var mesa = OnMesa(px, pz, 0f);
                if (roll < .06f && !mesa && bowlN < 12) { if (Bowl(ctx, mk, solid, p)) bowlN++; }
                else if (roll < .13f && !mesa) { Ramp(ctx, mk, solid, p, face); ramps++; }
                else if (roll < .19f && !mesa) { if (HouseShell(ctx, mk, solid, p, face, shells)) shells++; }
                else if (roll < .25f) { GraffitiWall(ctx, mk, p, face, ctx.Range(4f, 8f), walls + 7); walls++; }
                else if (roll < .28f) { Couch(ctx, mk, p, face, couches); couches++; }
                else if (roll < .3f && !mesa) { SwingSet(ctx, mk, p, face); swings++; }
                else if (roll < .32f && !mesa) { Trampoline(ctx, mk, p); trampolines++; }
                else if (roll < .35f) { CarOnBlocks(ctx, mk, p, face, cars); cars++; }
                else if (roll < .37f && !truck && !mesa) { IceCreamTruck(ctx, mk, p, face); truck = true; }
                else if (roll < .41f) { Cart(ctx, mk, p, ctx.Range(0, 360f), ctx.Chance(.4f)); carts++; }
                else if (roll < .55f) { for (var k = 0; k < 3; k++) { var q = ctx.OnGround(px + ctx.Range(-3f, 3f), pz + ctx.Range(-3f, 3f)); mk.Blob(scrub, q + Vector3.up * .08f, new Vector3(ctx.Range(.5f, 1.1f), ctx.Range(.18f, .3f), ctx.Range(.5f, 1.1f)), ctx.Rng, .3f); } }
                else if (roll < .65f)
                {
                    var s = ctx.Range(1f, 2.4f); var lo = p.y; for (var k = 0; k < 6; k++) { var a = k * Mathf.PI / 3f; lo = Mathf.Min(lo, ctx.Ground(px + Mathf.Cos(a) * s * .9f, pz + Mathf.Sin(a) * s * .9f)); }
                    p.y = lo; mk.Blob(WorldMaterials.Solid(new Color(.72f, .48f, .34f)), p + Vector3.up * s * .35f, new Vector3(s, s * .7f, s * .9f), ctx.Rng, .3f); BoxCol(ctx.Root, p + Vector3.up * s * .35f, new Vector3(s * 1.8f, s * 1.3f, s * 1.6f), Quaternion.identity); rocks++;
                }
                else if (roll < .7f) life.tumbleweeds.Add(p);
            }
            Debug.Log($"The Punk Badlands: pool {(hasPool ? "yes" : "no")}, {mesas.Count} mesas, {bowlN} bowls, {ramps} ramps, {shells} house shells, {walls} graffiti walls, {couches} couches, {swings} swing sets, {trampolines} trampolines, {cars} cars, {carts} carts, {rocks} rocks, truck {(truck ? "yes" : "no")}");
        }

        /// A skate bowl: a round hollow in the dirt lined with concrete, white coping round the rim.
        bool Bowl(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 at)
        {
            const float R = 7f, Dp = 2.2f; var up = Vector3.up;
            float lo = float.MaxValue, hi = float.MinValue; for (var k = 0; k < 12; k++) { var a = k * Mathf.PI / 6f; var h = ctx.Ground(at.x + Mathf.Cos(a) * R, at.z + Mathf.Sin(a) * R); lo = Mathf.Min(lo, h); hi = Mathf.Max(hi, h); }
            if (hi - lo > 1.2f) return false;
            var rim = hi + .15f; var c = new Vector3(at.x, rim, at.z);
            // the bowl as rings of concrete going down and in, the floor flat
            const int rings = 6; Vector3 P(float a, float t) { var r = Mathf.Lerp(R, R * .35f, t); var y = rim - Dp * Mathf.Sin(t * Mathf.PI * .5f); return c + new Vector3(Mathf.Cos(a) * r, y - rim, Mathf.Sin(a) * r); }
            for (var k = 0; k < 24; k++)
            {
                float a0 = k * Mathf.PI / 12f, a1 = (k + 1) * Mathf.PI / 12f;
                for (var j = 0; j < rings; j++) { float t0 = j / (float)rings, t1 = (j + 1) / (float)rings; mk.Quad(concrete, P(a0, t0), P(a1, t0), P(a1, t1), P(a0, t1), c + up * 4f); }
                mk.Tri(concrete, P(a0, 1f), P(a1, 1f), c - up * Dp, c + up * 4f);
                var r0 = c + new Vector3(Mathf.Cos(a0), 0, Mathf.Sin(a0)) * (R + .2f); var r1 = c + new Vector3(Mathf.Cos(a1), 0, Mathf.Sin(a1)) * (R + .2f);
                mk.Box(coping, (r0 + r1) / 2f + up * .05f, new Vector3(.4f, .2f, Vector3.Distance(r0, r1) + .05f), Quaternion.LookRotation(r1 - r0));
                // a skirt of concrete down to the ground outside, so the rim never floats
                var g0 = ctx.Ground(r0.x, r0.z); var g1 = ctx.Ground(r1.x, r1.z);
                mk.Quad(concreteDark, r0, r1, new Vector3(r1.x, g1 - .3f, r1.z), new Vector3(r0.x, g0 - .3f, r0.z), c - up);
                if (rim - Mathf.Min(g0, g1) > .4f) { var lowG = Mathf.Min(g0, g1); var sm = (r0 + r1) / 2f; solid.Box(concreteDark, new Vector3(sm.x, (rim + lowG) / 2f, sm.z) + (sm - c).normalized * .2f, new Vector3(.5f, rim - lowG + .2f, Vector3.Distance(r0, r1) + .1f), Quaternion.LookRotation(r1 - r0)); }
                // the bowl's walls as colliders you can skate down
                for (var j = 0; j < rings; j++) { float t0 = j / (float)rings, t1 = (j + 1) / (float)rings; var m = (P(a0, t0) + P(a1, t1)) / 2f; var along = P(a1, t0) - P(a0, t0); var down = P(a0, t1) - P(a0, t0); solid.Box(concrete, m - Vector3.Cross(along, down).normalized * .15f, new Vector3(along.magnitude + .1f, .3f, down.magnitude + .1f), Quaternion.LookRotation(down, Vector3.Cross(down, along).normalized)); }
            }
            solid.Prism(concrete, c - up * (Dp + .3f), up, R * .38f, .3f, 12);
            for (var k = 0; k < 6; k++) mk.Blob(Paint(k + (int)at.x), c - up * (Dp - .02f) + new Vector3(ctx.Range(-1.8f, 1.8f), 0, ctx.Range(-1.8f, 1.8f)), new Vector3(ctx.Range(.4f, 1f), .02f, ctx.Range(.3f, .8f)), ctx.Rng, .3f);
            bowls.Add(new Vector3(c.x, R, c.z)); life.bowls.Add(new Vector4(c.x, c.y, c.z, R)); noGrass.Add(new Vector3(c.x, R + 2f, c.z)); ctx.AddKeepout(c.x, c.z, R + 2f);
            return true;
        }

        /// A wooden quarter-pipe or a kicker ramp, plywood on a frame.
        void Ramp(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 at, Vector3 face)
        {
            var up = Vector3.up; var rot = Quaternion.LookRotation(face); var sd = Vector3.Cross(up, face); var big = ctx.Chance(.4f);
            var H = big ? 2f : .8f; var L = big ? 3f : 2.2f; const float W = 3f;
            var lo = at.y; foreach (var e in new[] { -1f, 1f }) foreach (var f in new[] { -1f, 1f }) lo = Mathf.Min(lo, ctx.Ground(at.x + sd.x * e * W / 2f + face.x * f * L / 2f, at.z + sd.z * e * W / 2f + face.z * f * L / 2f));
            var b = new Vector3(at.x, lo, at.z);
            var ang = Mathf.Atan2(H, L) * Mathf.Rad2Deg;
            mk.Box(wood, b + up * H / 2f, new Vector3(W, .08f, Mathf.Sqrt(H * H + L * L)), rot * Quaternion.Euler(-ang, 0, 0));
            foreach (var e in new[] { -1f, 1f }) mk.Tri(woodDark, b + sd * e * W / 2f + face * L / 2f, b + sd * e * W / 2f - face * L / 2f, b + sd * e * W / 2f - face * L / 2f + up * H, b - sd * e * 3f);
            mk.Box(woodDark, b - face * L / 2f + up * H / 2f, new Vector3(W, H, .08f), rot);
            if (big) mk.Box(metal, b - face * L / 2f + up * H, new Vector3(W, .1f, .1f), rot);
            solid.Box(wood, b + up * H / 2f, new Vector3(W, .3f, Mathf.Sqrt(H * H + L * L)), rot * Quaternion.Euler(-ang, 0, 0));
            solid.Box(woodDark, b - face * (L / 2f - .1f) + up * H / 2f, new Vector3(W, H, .3f), rot);
            mk.Box(Paint((int)at.x), b + up * H * .55f + face * .1f, new Vector3(W * .5f, .02f, .8f), rot * Quaternion.Euler(-ang, 0, 0));
        }

        /// The shell of a suburban house: walls with window and door holes, no roof, a garage door hanging off.
        bool HouseShell(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 at, Vector3 face, int i)
        {
            var up = Vector3.up; var rot = Quaternion.LookRotation(face); var sd = Vector3.Cross(up, face); const float W = 9f, D = 7f, H = 3f;
            float lo = float.MaxValue, hi = float.MinValue; foreach (var e in new[] { -1f, 1f }) foreach (var f in new[] { -1f, 1f }) { var h = ctx.Ground(at.x + sd.x * e * W / 2f + face.x * f * D / 2f, at.z + sd.z * e * W / 2f + face.z * f * D / 2f); lo = Mathf.Min(lo, h); hi = Mathf.Max(hi, h); }
            if (hi - lo > 2.5f) return false;
            var b = new Vector3(at.x, hi, at.z); var wall = WorldMaterials.Solid(new[] { new Color(.9f, .86f, .78f), new Color(.75f, .82f, .85f), new Color(.88f, .8f, .65f) }[i % 3]);
            mk.Box(concreteDark, new Vector3(b.x, (lo + hi) / 2f - .1f, b.z), new Vector3(W + .2f, hi - lo + .4f, D + .2f), rot);
            void Wall(Vector3 c, Vector3 size, Quaternion r, float broken) { var s = size; s.y *= broken; mk.Box(wall, c + up * (s.y / 2f), s, r); solid.Box(wall, c + up * (s.y / 2f), s, r); }
            Wall(b - face * (D / 2f - .1f), new Vector3(W, H, .2f), rot, .9f);
            Wall(b + sd * (W / 2f - .1f), new Vector3(.2f, H, D - .4f), rot, .7f);
            Wall(b - sd * (W / 2f - .1f), new Vector3(.2f, H, D - .4f), rot, 1f);
            Wall(b + face * (D / 2f - .1f) - sd * 3.2f, new Vector3(2.6f, H, .2f), rot, 1f);
            Wall(b + face * (D / 2f - .1f) + sd * 2.6f, new Vector3(3.8f, H, .2f), rot, .55f);
            mk.Box(wall, b + face * (D / 2f - .1f) - sd * .7f + up * (H - .3f), new Vector3(2.4f, .6f, .2f), rot);
            mk.Box(WorldMaterials.Solid(new Color(.7f, .7f, .72f)), b + face * (D / 2f + .6f) + sd * 2.6f + up * .9f, new Vector3(3f, .08f, 2f), rot * Quaternion.Euler(50f, 0, 8f)); BoxCol(ctx.Root, b + face * (D / 2f + .6f) + sd * 2.6f + up * .9f, new Vector3(3f, .3f, 2f), rot * Quaternion.Euler(50f, 0, 8f));
            for (var k = 0; k < 5; k++) mk.Box(Paint(i + k), b + face * (D / 2f + .005f + k * .013f) - sd * 3.2f + up * (.6f + k * .4f) + sd * ((k % 2) * .4f - .2f), new Vector3(.9f, .35f, .004f), rot * Quaternion.Euler(0, 0, k * 13f));
            Mailbox(ctx, mk, ctx.OnGround(b.x + face.x * (D / 2f + 3f) + sd.x * 3.5f, b.z + face.z * (D / 2f + 3f) + sd.z * 3.5f), i);
            noGrass.Add(new Vector3(b.x, 6f, b.z));
            return true;
        }

        void Couch(LandContext ctx, MeshKit mk, Vector3 g, Vector3 face, int i)
        {
            var up = Vector3.up; var rot = Quaternion.LookRotation(face) * Quaternion.Euler(0, 0, ((i * 13) % 9) - 4f); var cloth = WorldMaterials.Solid(new[] { new Color(.55f, .45f, .3f), new Color(.4f, .5f, .35f), new Color(.6f, .3f, .3f) }[i % 3]);
            mk.Box(cloth, g + up * .3f, new Vector3(2f, .5f, .9f), rot); mk.Box(cloth, g + up * .75f - face * .35f, new Vector3(2f, .7f, .25f), rot);
            foreach (var e in new[] { -1f, 1f }) mk.Box(cloth, g + up * .55f + Vector3.Cross(up, face) * e * .9f, new Vector3(.25f, .55f, .9f), rot);
            BoxCol(ctx.Root, g + up * .55f, new Vector3(2.1f, 1.1f, 1f), rot);
        }

        void SwingSet(LandContext ctx, MeshKit mk, Vector3 g, Vector3 face)
        {
            var up = Vector3.up; var sd = Vector3.Cross(up, face); var frame = WorldMaterials.Solid(new Color(.3f, .55f, .8f));
            foreach (var e in new[] { -1.8f, 1.8f }) foreach (var f in new[] { -.9f, .9f }) mk.Box(frame, g + sd * e + face * f * .5f + up * 1.2f, new Vector3(.08f, 2.6f, .08f), Quaternion.LookRotation(face) * Quaternion.Euler(f * 20f, 0, 0));
            mk.Box(frame, g + up * 2.4f, new Vector3(3.8f, .08f, .08f), Quaternion.LookRotation(face));
            for (var k = 0; k < 2; k++) { var s = g + sd * (k - .5f) * 1.6f; foreach (var e in new[] { -.25f, .25f }) mk.Box(metal, s + sd * e + up * 1.55f, new Vector3(.02f, 1.7f, .02f), Quaternion.identity); mk.Box(dark, s + up * .65f, new Vector3(.6f, .05f, .25f), Quaternion.LookRotation(face)); }
            foreach (var e in new[] { -1.8f, 1.8f }) ArtistBits.PostCollider(ctx.Root, g + sd * e, 2.4f, .25f);
            BoxCol(ctx.Root, g + up * 1.4f, new Vector3(3.4f, 1.8f, .5f), Quaternion.LookRotation(face));   // the swings themselves
        }

        void Trampoline(LandContext ctx, MeshKit mk, Vector3 g)
        {
            var up = Vector3.up; mk.Prism(dark, g + up * .7f, up, 1.8f, .05f, 16); mk.Prism(WorldMaterials.Solid(new Color(.3f, .55f, .8f)), g + up * .72f, up, 2f, .06f, 16, 1.02f);
            for (var k = 0; k < 6; k++) { var a = k * Mathf.PI / 3f; mk.Box(metal, g + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * 1.9f + up * .35f, new Vector3(.05f, .7f, .05f), Quaternion.identity); }
            BoxCol(ctx.Root, g + up * .4f, new Vector3(3.8f, .8f, 3.8f), Quaternion.identity);
        }

        void CarOnBlocks(LandContext ctx, MeshKit mk, Vector3 g, Vector3 face, int i)
        {
            var up = Vector3.up; var rot = Quaternion.LookRotation(face); var paint = WorldMaterials.Solid(Color.Lerp(Spray[i % Spray.Length], new Color(.55f, .45f, .4f), .55f));
            foreach (var e in new[] { -1.4f, 1.4f }) mk.Box(concreteDark, g + face * e + up * .2f, new Vector3(1.6f, .4f, .5f), rot);
            mk.Box(paint, g + up * .8f, new Vector3(1.9f, .7f, 4.4f), rot); mk.Box(paint, g + up * 1.4f - face * .3f, new Vector3(1.7f, .55f, 2.2f), rot);
            mk.Box(WorldMaterials.Solid(new Color(.15f, .17f, .2f), .8f), g + up * 1.42f - face * .3f, new Vector3(1.74f, .4f, 2.1f), rot);
            mk.Box(rust, g + up * 1.16f + face * .8f, new Vector3(1.4f, .02f, 1.2f), rot);
            BoxCol(ctx.Root, g + up * .9f, new Vector3(1.9f, 1.6f, 4.4f), rot);
        }

        void IceCreamTruck(LandContext ctx, MeshKit mk, Vector3 g, Vector3 face)
        {
            var up = Vector3.up; var rot = Quaternion.LookRotation(face); var sd = Vector3.Cross(up, face);
            mk.Box(white, g + up * 1.5f, new Vector3(2.3f, 2.4f, 5.6f), rot); mk.Box(WorldMaterials.Solid(new Color(.95f, .5f, .7f)), g + up * .6f, new Vector3(2.32f, .4f, 5.62f), rot);
            mk.Box(GlowM(new Color(1f, .95f, .8f), 1.4f), g + up * 1.8f + sd * 1.16f, new Vector3(.03f, .9f, 2.4f), rot);
            var cone = g + up * 3.3f; mk.Prism(WorldMaterials.Solid(new Color(.9f, .7f, .4f)), cone + up * .9f, -up, .35f, .9f, 8, .1f); mk.Blob(WorldMaterials.Solid(new Color(1f, .7f, .8f)), cone + up * 1.1f, new Vector3(.45f, .35f, .45f), ctx.Rng, .1f);
            BoxCol(ctx.Root, g + up * 1.4f, new Vector3(2.3f, 2.8f, 5.6f), rot);
            halos.Add(g + up * 1.8f + sd * 1.8f, 1.6f, new Color(1f, .95f, .8f), .4f);
            life.truck = g + up * 2f;
        }

        void Scrub(LandContext ctx)
        {
            var mat = WindMesh.Material(.3f, 1f, new Color(.6f, .58f, .35f));
            var meshes = new Dictionary<(int, int), WindMesh>(); var rng = new System.Random(1994);
            const float st = 3f;
            for (var x = ctx.MinX; x < ctx.MaxX; x += st)
            for (var z = ctx.MinZ; z < ctx.MaxZ; z += st)
            {
                var px = x + ((float)rng.NextDouble() - .5f) * st; var pz = z + ((float)rng.NextDouble() - .5f) * st;
                if (!ctx.IsLand(px, pz, 1f) || NearPath(px, pz, 2.4f) || InNoGrass(px, pz, 0f) || InFootprint(ctx, px, pz, -2f)) continue;
                if (hasPool && Vector2.Distance(new Vector2(px, pz), pool2) < PoolR * 1.5f) continue;
                if (Mathf.PerlinNoise(px * .04f + 5f, pz * .04f + 3f) < .55f) continue;
                var key = (Mathf.FloorToInt(px / 70f), Mathf.FloorToInt(pz / 70f));
                if (!meshes.TryGetValue(key, out var wm)) meshes[key] = wm = new WindMesh();
                var col = Color.Lerp(new Color(.55f, .52f, .3f), new Color(.75f, .68f, .42f), (float)rng.NextDouble());
                wm.Tuft(new Vector3(px, ctx.Ground(px, pz) - .04f, pz), .25f + (float)rng.NextDouble() * .3f, col, Color.Lerp(col, new Color(.95f, .9f, .7f), .4f), rng, 3, .05f);
            }
            foreach (var kv in meshes) kv.Value.Build("Badlands scrub", ctx.Root, mat);
        }
    }
}
