using System.Collections.Generic;
using UnityEngine;
using static EverythingWorld.World.PlaceTools;

namespace EverythingWorld.World
{
    /// The Pop Republic (WORLD_GUIDE.md): a pop capital on a bright day. A deep blue sky, white paving, candy-coloured
    /// houses on the hills, lollipop trees, billboards, fountains, confetti always somewhere in the air. You arrive on
    /// the Plaza, with its great fountain. The Red Carpet runs from it, between velvet ropes and stars in the pavement,
    /// to the Palace: a glittering stage under an arch of lights, with a ferris wheel turning behind it. Off to one
    /// side, K-Pop Square and its idol stage. Joyful, everybody singing along, a big show.
    /// Laid out on purpose (the build-land standard): PopTown.cs has the places and the people.
    public partial class PopKit : LandKit
    {
        public override string Land => "The Pop Republic";
        public override Vector3 ArtistFront(IslandWorldData data, IslandWorldData.Artist a) => facing.TryGetValue(a.name, out var f) ? f : base.ArtistFront(data, a);

        public static readonly Color Pink = new Color(1f, .5f, .74f), Cyan = new Color(.3f, .82f, .92f), Lemon = new Color(1f, .9f, .36f), White = new Color(.98f, .97f, .96f), Ink = new Color(.12f, .1f, .2f), Gold = new Color(1f, .8f, .3f), Lilac = new Color(.74f, .62f, 1f), RedC = new Color(.86f, .12f, .2f);
        static readonly Color[] Candy = { new Color(1f, .7f, .82f), new Color(.66f, .9f, .96f), new Color(1f, .92f, .56f), new Color(.8f, .74f, 1f), new Color(.7f, .94f, .76f), new Color(1f, .8f, .64f) };
        static readonly string[] KPop = { "BTS", "TWICE", "BLACKPINK", "IVE", "NewJeans", "aespa" };
        const float PathW = 3f, CarpetW = 6f;

        Material paving, pavingPink, carpet, goldM, glassM, white, chrome, leafM, leafPink, leafMint, trunkM, windowM, lampM, waterM, dark, ropeM;
        readonly Material[] bulbM = new Material[3];
        GlowHalos halos; Vector3 center, D, S; PopLife life; Vector3 kCenter; bool hasSquare;
        readonly Dictionary<(int, int), List<Vector3>> pathCells = new Dictionary<(int, int), List<Vector3>>();
        readonly List<(Vector3 p, int w)> walkPoints = new List<(Vector3, int)>();
        readonly Dictionary<string, Vector3> facing = new Dictionary<string, Vector3>();
        // x, radius, z
        readonly List<Vector3> noGrass = new List<Vector3>();
        Vector2 heart2, D2 = new Vector2(0, -1), S2 = new Vector2(1, 0), k2; float plazaY, waterY, streetL, palaceY;

        void MakeMaterials()
        {
            paving = WorldMaterials.Solid(new Color(.93f, .92f, .9f)); pavingPink = WorldMaterials.Solid(new Color(1f, .82f, .88f)); carpet = WorldMaterials.Glow(RedC, .2f); goldM = WorldMaterials.Glow(Gold, .45f); glassM = WorldMaterials.Solid(new Color(.6f, .86f, .96f), .9f);
            white = WorldMaterials.Solid(White); chrome = WorldMaterials.Solid(new Color(.8f, .82f, .88f), .9f); dark = WorldMaterials.Solid(new Color(.1f, .08f, .14f)); ropeM = WorldMaterials.Solid(new Color(.6f, .1f, .2f));
            leafM = WorldMaterials.Solid(new Color(.4f, .78f, .46f)); leafPink = WorldMaterials.Solid(new Color(1f, .62f, .78f)); leafMint = WorldMaterials.Solid(new Color(.6f, .92f, .78f)); trunkM = WorldMaterials.Solid(new Color(.95f, .9f, .86f));
            windowM = new Material(WorldMaterials.Glow(new Color(.7f, .9f, 1f), .5f)) { name = "Pop windows" }; lampM = new Material(WorldMaterials.Glow(new Color(1f, .95f, .8f), 1.2f)) { name = "Pop lamps" };
            waterM = new Material(WorldMaterials.Glow(Cyan, .9f)) { name = "Pop water" };
            for (var i = 0; i < 3; i++) bulbM[i] = new Material(WorldMaterials.Glow(new[] { Pink, Lemon, Cyan }[i], 1.6f)) { name = "Pop bulbs " + i };
        }

        Material GlowM(Color c, float s = 2f) => WorldMaterials.Glow(c, s * .65f);
        Material Paint(Color c) => WorldMaterials.Solid(c);
        static bool HasStation(IslandWorldData.Artist a) => a.stationId != 0;
        static float Foot(IslandWorldData.Artist a)
        {
            switch (a.name)
            {
                case "Taylor Swift": return 18f;
                case "Sabrina Carpenter": return 14f;
                case "Katy Perry": case "Michael Jackson": return 12f;
                default: return 8.5f;
            }
        }

        public override bool Ground(float height, float slope, float patch, out Color color)
        {
            // candy hills: bright fresh green, mint and a little pink where flowers are thick
            if (slope > 26f) color = Color.Lerp(new Color(.86f, .76f, .72f), new Color(.92f, .84f, .78f), patch);
            else color = patch > .78f ? Color.Lerp(new Color(.98f, .7f, .82f), new Color(1f, .78f, .86f), (patch - .78f) / .22f) : Color.Lerp(new Color(.44f, .78f, .46f), new Color(.6f, .88f, .64f), Mathf.SmoothStep(0f, 1f, patch / .78f));
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
            // the Red Carpet runs whichever way the land runs longest, and ends at the Palace
            streetL = 0f;
            for (var k = 0; k < 8; k++) { var dir = new Vector2(Mathf.Sin(k * Mathf.PI / 4f), Mathf.Cos(k * Mathf.PI / 4f)); var len = 40f; while (len < 140f && Inside(heart2 + dir * (len + 10f), 70f)) len += 10f; if (len > streetL) { streetL = len; D2 = dir; } }
            S2 = new Vector2(-D2.y, D2.x); plazaY = Mathf.Max(data.HeightAt(heart2.x, heart2.y), waterY + 6f);
            var pal = P2(streetL + 22f, 0); palaceY = Mathf.Max(data.HeightAt(pal.x, pal.y), waterY + 4f);

            var keep = new List<string>();
            void Put(string name, Vector2 p, float room) { if (!Inside(p, room)) return; foreach (var a in data.layout.artists) if (a.sceneId == isl.sceneId && a.name == name) { a.x = p.x; a.z = p.y; keep.Add(name); } }
            Put("Taylor Swift", P2(-6f, 58f), 34f); Put("Sabrina Carpenter", P2(4f, -56f), 30f); Put("Michael Jackson", P2(-62f, -10f), 26f);
            // K-Pop Square: the idol groups round one square, on whichever side has room
            hasSquare = false;
            foreach (var side in new[] { -1f, 1f })
            {
                var c = P2(70f, side * -96f); if (!Inside(c, 70f)) continue;
                k2 = c; hasSquare = true; Put("Katy Perry", P2(76f, side * 44f), 26f);   // on the other side from the square
                for (var k = 0; k < KPop.Length; k++) { var a = (k * 60f + 30f) * Mathf.Deg2Rad; Put(KPop[k], c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 40f, 16f); }
                break;
            }
            if (!hasSquare) Put("Katy Perry", P2(76f, 44f), 26f);
            var avoid = new List<Vector3>(); for (var d = 0f; d <= streetL; d += 22f) avoid.Add(new Vector3(heart2.x + D2.x * d, 14f, heart2.y + D2.y * d)); avoid.Add(new Vector3(pal.x, 40f, pal.y)); if (hasSquare) avoid.Add(new Vector3(k2.x, 22f, k2.y));
            ArtistPlacement.Spread(data, index, 54f, heart2, 44f, 40f, avoid, keep, Land, 330f);
            Level(data, index, heart2, 22f, 40f, plazaY); Level(data, index, pal, 34f, 52f, palaceY); if (hasSquare) Level(data, index, k2, 22f, 36f, Mathf.Max(data.HeightAt(k2.x, k2.y), waterY + 4f));
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
            center = ctx.Plaza; D = new Vector3(D2.x, 0, D2.y); S = new Vector3(S2.x, 0, S2.y);
            life = new GameObject("Pop life").AddComponent<PopLife>();
            life.transform.SetParent(ctx.Root, false);
            life.center = center; life.water = waterM; life.lamps = lampM; life.bulbs = bulbM;
            var mk = new MeshKit(); var solid = new MeshKit();
            Remember(ctx.OnGround(center.x, center.z), 0);
            TownStart(ctx);
            ThePlaza(ctx, mk, solid);
            RedCarpet(ctx, mk, solid);
            ThePalace(ctx, mk, solid);
            if (hasSquare) KPopSquare(ctx, mk, solid);
            Paths(ctx, mk, solid);
            Houses(ctx, mk, solid);
            Wilds(ctx, mk, solid);
            mk.Build("The Pop Republic", ctx.Root, false);
            solid.BuildCollider("The Pop Republic walls", ctx.Root);
        }

        public override void Finish(LandContext ctx) { Verge(ctx); halos.Build("Pop glow", ctx.Root); }

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
            // laid in narrow lanes across its width, every vertex on the ground, so on side slopes it tilts with the hill
            var n = pts.Count; var lanes = Mathf.Max(1, Mathf.CeilToInt(w / 1.5f)); var V = new Vector3[n, lanes + 1];
            Vector3 Flat(Vector3 v) { v.y = 0; return v.normalized; }
            for (var i = 0; i < n; i++)
            {
                var a = i > 0 ? Flat(pts[i] - pts[i - 1]) : Flat(pts[1] - pts[0]);
                var b = i < n - 1 ? Flat(pts[i + 1] - pts[i]) : Flat(pts[n - 1] - pts[n - 2]);
                var t = (a + b).normalized; var side = Vector3.Cross(Vector3.up, t);
                var miter = 1f / Mathf.Max(.6f, Vector3.Dot(side, Vector3.Cross(Vector3.up, b)));
                for (var k = 0; k <= lanes; k++) { var q = pts[i] + side * ((k / (float)lanes - .5f) * w * miter); q.y = ground.Ground(q.x, q.z) + lift; V[i, k] = q; }
            }
            for (var i = 0; i + 1 < n; i++) for (var k = 0; k < lanes; k++) mk.Quad(m, V[i, k], V[i, k + 1], V[i + 1, k + 1], V[i + 1, k], (V[i, k] + V[i + 1, k + 1]) / 2f - Vector3.up);
        }

        /// True when the ground within r of (x, z) rises or falls no more than 'rise'.
        bool Gentle(LandContext ctx, float x, float z, float r, float rise)
        {
            float lo = ctx.Ground(x, z), hi = lo;
            for (var k = 0; k < 8; k++) { var a = k * Mathf.PI / 4f; var h = ctx.Ground(x + Mathf.Cos(a) * r, z + Mathf.Sin(a) * r); lo = Mathf.Min(lo, h); hi = Mathf.Max(hi, h); }
            return hi - lo <= rise;
        }

        /// A street lamp with a star on top.
        void StarLamp(LandContext ctx, MeshKit mk, Vector3 g, int i, bool light = false)
        {
            var up = Vector3.up; mk.Prism(white, g - up * .2f, up, .07f, 4.4f, 6, .7f); mk.Blob(lampM, g + up * 4.3f, Vector3.one * .26f, new System.Random(i), 0f); Star(mk, bulbM[i % 3], g + up * 4.9f, Vector3.forward, .3f); halos.Add(g + up * 4.3f, 1.4f, new Color(1f, .95f, .8f), .3f);
            ArtistBits.PostCollider(ctx.Root, g, 4.2f, .1f); if (light) ArtistBits.PointLight(ctx.Root, g + up * 3.8f, new Color(1f, .9f, .8f), 12f, .9f);
        }

        /// A flat five-pointed star facing 'face'.
        void Star(MeshKit mk, Material m, Vector3 c, Vector3 face, float r)
        {
            face.Normalize(); var up = Mathf.Abs(Vector3.Dot(face, Vector3.up)) > .9f ? Vector3.forward : Vector3.up;
            for (var k = 0; k < 5; k++) { var a0 = Quaternion.AngleAxis(k * 72f, face) * up; var a1 = Quaternion.AngleAxis(k * 72f + 36f, face) * up; var a2 = Quaternion.AngleAxis(k * 72f - 36f, face) * up; mk.Tri(m, c + a0 * r, c + a1 * r * .42f, c + a2 * r * .42f, c - face); mk.Tri(m, c + a0 * r, c + a1 * r * .42f, c + a2 * r * .42f, c + face); mk.Tri(m, c, c + a1 * r * .42f, c + a2 * r * .42f, c - face); mk.Tri(m, c, c + a1 * r * .42f, c + a2 * r * .42f, c + face); }
        }

        /// A lollipop tree: a white trunk and a round candy crown.
        void Lollipop(LandContext ctx, MeshKit mk, Vector3 g, float s, int i, System.Random rng)
        {
            var up = Vector3.up; mk.Prism(trunkM, g - up * .2f, up, .12f * s, 2.8f * s, 6); ArtistBits.PostCollider(ctx.Root, g, 2.6f * s, .16f * s); var m = i % 3 == 0 ? leafPink : i % 3 == 1 ? leafMint : leafM;
            mk.Blob(m, g + up * 3.6f * s, Vector3.one * 1.3f * s, rng, .06f); noGrass.Add(new Vector3(g.x, .6f * s, g.z));
        }

        /// A billboard on two legs with a colour and a word.
        void Billboard(LandContext ctx, MeshKit mk, Vector3 g, Vector3 face, int i)
        {
            var up = Vector3.up; var rot = Quaternion.LookRotation(face); var side = Vector3.Cross(up, face); string[] words = { "SING IT!", "ENCORE", "#1", "ON TOUR", "SOLD OUT", "LOUDER!", "ALL TOGETHER", "ONE MORE TIME", "STAN", "LIVE TONIGHT" };
            foreach (var e in new[] { -2f, 2f }) { var leg = g + side * e; var drop = Mathf.Max(0f, leg.y - ctx.Ground(leg.x, leg.z)); mk.Prism(white, leg - up * (.3f + drop), up, .12f, 5.2f + drop, 5); ArtistBits.PostCollider(ctx.Root, leg - up * drop, 5f + drop, .14f); }
            mk.Box(white, g + up * 6.2f, new Vector3(6.6f, 3.2f, .16f), rot); mk.Box(Paint(Candy[i % Candy.Length]), g + up * 6.2f + face * .1f, new Vector3(6.2f, 2.8f, .04f), rot); for (var k = 0; k < 8; k++) mk.Blob(bulbM[k % 3], g + up * 7.86f + side * (-3f + k * .86f) + face * .1f, Vector3.one * .1f, new System.Random(k), 0f);
            Sign(g + up * 6.2f + face * .14f, face, words[i % words.Length], .7f, Ink);
        }

        /// A fountain: a round basin, a tiered middle, glowing jets.
        void Fountain(LandContext ctx, MeshKit mk, Vector3 g, float s)
        {
            var up = Vector3.up; mk.Prism(white, g, up, 3f * s, .6f, 20); mk.Prism(waterM, g + up * .5f, up, 2.8f * s, .02f, 20); mk.Prism(white, g, up, .5f * s, 1.6f * s, 10); mk.Prism(white, g + up * 1.6f * s, up, 1.2f * s, .2f, 14); mk.Prism(waterM, g + up * 1.78f * s, up, 1.1f * s, .02f, 14);
            for (var k = 0; k < 6; k++) { var d = Quaternion.Euler(0, k * 60f, 0) * Vector3.forward; mk.Prism(waterM, g + up * .5f + d * 2f * s, (up + d * .3f).normalized, .05f * s, 1.4f * s, 5, .4f); }
            mk.Prism(waterM, g + up * 1.8f * s, up, .1f * s, 2.4f * s, 6, .3f); BoxCol(ctx.Root, g + up * .4f, new Vector3(6f * s, .8f, 6f * s), Quaternion.identity); halos.Add(g + up * 2.4f * s, 2.4f * s, Cyan, .2f); life.sprays.Add(g + up * (1.8f + 2.4f) * s);
            noGrass.Add(new Vector3(g.x, 3.4f * s, g.z));
        }

        /// A confetti cannon on a little cart. It fires when somebody plays.
        void Cannon(LandContext ctx, MeshKit mk, Vector3 g, Vector3 aim, int i)
        {
            var up = Vector3.up; aim.y = 0; aim.Normalize(); var d = (aim + up * 1.4f).normalized; mk.Box(Paint(Candy[i % Candy.Length]), g + up * .3f, new Vector3(.8f, .5f, 1f), Quaternion.LookRotation(aim)); mk.Prism(chrome, g + up * .5f, d, .24f, 1.3f, 10, 1.2f); mk.Prism(bulbM[i % 3], g + up * .5f + d * 1.3f, d, .3f, .05f, 10);
            BoxCol(ctx.Root, g + up * .5f, new Vector3(.9f, 1f, 1.1f), Quaternion.LookRotation(aim)); life.cannons.Add(g + up * .5f + d * 1.4f);
        }

        /// A bench to sit on.
        void Bench(Transform parent, MeshKit mk, Vector3 g, Vector3 face, float len = 2f)
        {
            var up = Vector3.up; var rot = Quaternion.LookRotation(face); var side = Vector3.Cross(up, face); var m = Paint(Candy[(int)(Mathf.Abs(g.x * 7f + g.z)) % Candy.Length]); for (var k = 0; k < 3; k++) mk.Box(m, g + up * .46f + face * (-.16f + k * .16f), new Vector3(len, .05f, .13f), rot); for (var k = 0; k < 2; k++) mk.Box(m, g + up * (.7f + k * .2f) - face * .26f, new Vector3(len, .13f, .05f), rot); foreach (var e in new[] { -1f, 1f }) mk.Box(white, g + up * .3f + side * e * (len / 2f - .15f), new Vector3(.06f, .6f, .5f), rot);
            BoxCol(parent, g + up * .45f, new Vector3(len, .9f, .5f), rot); Interact.Bench(tlife.transform, g + up * .52f - side * (len / 2f - .5f), g + up * .52f + side * (len / 2f - .5f), face);
        }

        /// A string of coloured bulbs from a to b, sagging.
        void Bulbs(MeshKit mk, Vector3 a, Vector3 b, int phase)
        {
            var len = Vector3.Distance(a, b); var n = Mathf.Max(3, Mathf.RoundToInt(len / 1.1f)); var sag = Mathf.Min(1f, len * .06f); var prev = a;
            for (var i = 1; i <= n; i++) { var t = i / (float)n; var p = Vector3.Lerp(a, b, t) - Vector3.up * Mathf.Sin(t * Mathf.PI) * sag; mk.Prism(dark, prev, (p - prev).normalized, .012f, Vector3.Distance(prev, p), 3); prev = p; if (i < n) mk.Blob(bulbM[(i + phase) % 3], p - Vector3.up * .08f, Vector3.one * .08f, new System.Random(i), 0f); }
        }

        // ---------------------------------------------------------------- the lanes to the artists
        void Paths(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var walk = 10; var n = 0; var rng = new System.Random(30);
            void Lay(Vector3 join, Vector3 to, float stopShort, string artist)
            {
                var from = new Vector3(join.x, 0, join.z); to.y = 0;
                var dir = to - from; var total = dir.magnitude; if (total < 2f) return; dir /= total;
                if (artist != null) facing[artist] = -dir;
                var side = Vector3.Cross(Vector3.up, dir); var end = total - stopShort; if (end < PathW) return;
                var pts = new List<Vector3>();
                for (var d = PathW / 2f + .1f; ; d += 1.5f)
                {
                    var dd = Mathf.Min(d, end); var q = from + dir * dd + side * Mathf.Sin(dd / 16f) * Mathf.Min(1.6f, end * .03f);
                    if (!ctx.IsLand(q.x, q.z, 0f)) break;
                    pts.Add(ctx.OnGround(q.x, q.z)); if (dd >= end) break;
                }
                if (pts.Count < 2) return;
                Strip(mk, pts, n % 2 == 0 ? paving : pavingPink, PathW, .13f, ctx);
                foreach (var p in pts) Remember(p, walk);
                for (var i = 6; i < pts.Count; i += 9, n++)
                {
                    var s = n % 2 == 0 ? 1f : -1f; var lp = pts[i] + side * s * (PathW / 2f + .7f);
                    if (InFootprint(ctx, lp.x, lp.z, 1f) || InNoGrass(lp.x, lp.z, 1f)) continue;
                    StarLamp(ctx, mk, ctx.OnGround(lp.x, lp.z), n, n % 3 == 0);
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

        // ---------------------------------------------------------------- the candy hills
        /// Candy houses, lollipop trees, billboards, fountains, benches under a star lamp, confetti cannons, ice-cream carts.
        void Wilds(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            int houses = 0, trees = 0, boards = 0, fountains = 0, benches = 0, cannons = 0; var rng = new System.Random(2008);
            const float cell = 21f;
            for (var x = ctx.MinX; x < ctx.MaxX; x += cell)
            for (var z = ctx.MinZ; z < ctx.MaxZ; z += cell)
            {
                float px = x + (float)rng.NextDouble() * cell, pz = z + (float)rng.NextDouble() * cell; var roll = (float)rng.NextDouble(); var s = (float)rng.NextDouble();
                if (!Open(ctx, px, pz, 4f)) continue;
                var p = ctx.OnGround(px, pz); var f = FaceTo(p, NearestWalk(p));
                if (roll < .16f) { if (!Open(ctx, px, pz, 7f) || !ctx.IsLand(px, pz, 8f) || Mathf.Abs(ctx.Ground(px + 4f, pz) - ctx.Ground(px - 4f, pz)) > 2f || Mathf.Abs(ctx.Ground(px, pz + 4f) - ctx.Ground(px, pz - 4f)) > 2f) continue; House(ctx, mk, solid, new PlaceFrame(p, f), houses + 30, rng); houses++; }
                else if (roll < .5f) { Lollipop(ctx, mk, p, .8f + s * .7f, trees, rng); trees++; }
                else if (roll >= .68f) continue;
                else if (!Gentle(ctx, px, pz, roll < .55f ? 3.4f : roll < .58f ? 2.4f : 1.6f, roll < .55f ? 1.6f : .6f)) continue;   // benches, fountains, carts and billboards only on gentle ground
                else if (roll < .55f) { Billboard(ctx, mk, p, f, boards + 3); noGrass.Add(new Vector3(p.x, 3f, p.z)); boards++; }
                else if (roll < .58f) { Fountain(ctx, mk, p, .7f); fountains++; }
                else if (roll < .65f) { Bench(ctx.Root, mk, p, f); StarLamp(ctx, mk, p - f * 1f + Vector3.Cross(Vector3.up, f) * 1.4f, benches, benches % 2 == 0); noGrass.Add(new Vector3(p.x, 2f, p.z)); benches++; }
                else if (roll < .68f) { Cannon(ctx, mk, p, f, cannons); cannons++; }
            }
            Debug.Log($"The Pop Republic: Red Carpet {streetL:0} m, {houses} candy houses, {trees} lollipop trees, {boards} billboards, {fountains} fountains, {benches} benches, {cannons} confetti cannons");
        }

        Vector3 NearestWalk(Vector3 p) { var best = center; var bd = float.MaxValue; foreach (var (q, _) in walkPoints) { var d = (q - p).sqrMagnitude; if (d < bd) { bd = d; best = q; } } return best; }

        /// Bright grass and flowers.
        void Verge(LandContext ctx)
        {
            var mat = WindMesh.Material(.3f, .8f, new Color(.5f, .84f, .52f));
            var meshes = new Dictionary<(int, int), WindMesh>(); var rng = new System.Random(2009);
            var flowers = new[] { new Color(1f, .5f, .7f), new Color(1f, .9f, .4f), new Color(.5f, .8f, 1f), new Color(1f, 1f, 1f), new Color(.8f, .6f, 1f) };
            const float st = 2.6f;
            for (var x = ctx.MinX; x < ctx.MaxX; x += st)
            for (var z = ctx.MinZ; z < ctx.MaxZ; z += st)
            {
                var px = x + ((float)rng.NextDouble() - .5f) * st; var pz = z + ((float)rng.NextDouble() - .5f) * st;
                if (!ctx.IsLand(px, pz, 1f) || NearPath(px, pz, 3.6f) || InNoGrass(px, pz, 0f) || InFootprint(ctx, px, pz, -2f)) continue;
                if (Mathf.PerlinNoise(px * .04f + 8f, pz * .04f + 4f) < .42f) continue;
                var key = (Mathf.FloorToInt(px / 70f), Mathf.FloorToInt(pz / 70f));
                if (!meshes.TryGetValue(key, out var wm)) meshes[key] = wm = new WindMesh();
                var col = Color.Lerp(new Color(.42f, .76f, .44f), new Color(.6f, .88f, .6f), (float)rng.NextDouble());
                wm.Tuft(new Vector3(px, ctx.Ground(px, pz) - .04f, pz), .24f + (float)rng.NextDouble() * .3f, col, rng.NextDouble() < .3 ? flowers[rng.Next(flowers.Length)] : Color.Lerp(col, new Color(.9f, 1f, .7f), .3f), rng, 3, .08f);
            }
            foreach (var kv in meshes) kv.Value.Build("Pop grass", ctx.Root, mat);
        }
    }
}
