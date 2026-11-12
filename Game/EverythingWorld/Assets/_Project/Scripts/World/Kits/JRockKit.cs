using System.Collections.Generic;
using UnityEngine;

namespace EverythingWorld.World
{
    /// The J-Rock Isles (WORLD_GUIDE.md): anime energy, like an opening sequence. Below, round where you arrive, a dense
    /// neon city on a grid of streets: tall buildings with lit windows and vertical neon signs, a red torii on the
    /// tallest rooftop, vending machines glowing on every corner, power lines tangled overhead, red lanterns strung
    /// across the streets, cherry trees in blossom. Above, on the high ground, an anime hilltop town: a school with a
    /// clock and a rooftop fence, little tiled houses, and at the top a shrine reached by a path lined with red torii.
    /// A train runs along the coast, its crossing bells ringing. A dramatic dusk, big clouds lit orange and pink.
    /// Reads by warm orange dusk and pink blossom against cyan neon (the Caldera next door is violet night, the
    /// Rainwood dark green). Files: JRockKit (ground, city, hill, train, paths, wilds), JRockArtists, JRockSongIds,
    /// JRockLife.
    public partial class JRockKit : LandKit
    {
        public override string Land => "The J-Rock Isles";
        public override Vector3 ArtistFront(IslandWorldData data, IslandWorldData.Artist a) => facing.TryGetValue(a.name, out var f) ? f : base.ArtistFront(data, a);

        public static readonly Color Pink = new Color(1f, .35f, .7f), Cyan = new Color(.25f, .9f, 1f), Amber = new Color(1f, .7f, .3f), Lime = new Color(.6f, 1f, .35f), Red = new Color(.85f, .12f, .1f), Ink = new Color(.95f, .95f, 1f), Blossom = new Color(1f, .72f, .84f);
        static readonly Color[] NeonC = { Pink, Cyan, Amber, Lime, new Color(1f, .9f, .3f), new Color(.6f, .5f, 1f) };
        const float CityR = 110f, Block = 34f, StreetW = 7f, PathW = 3f;

        Material asphalt, concrete, concreteDark, tile, wood, woodDark, red, metal, dark, glassLit, glassCool, lanternM, blossomM, bark, vendM;
        GlowHalos halos; Vector3 center; JRockLife life;
        readonly Dictionary<(int, int), List<Vector3>> pathCells = new Dictionary<(int, int), List<Vector3>>();
        readonly List<(Vector3 p, int w)> walkPoints = new List<(Vector3, int)>();
        readonly Dictionary<string, Vector3> facing = new Dictionary<string, Vector3>();
        readonly List<Vector3> noGrass = new List<Vector3>();
        readonly List<Vector3> buildingsAt = new List<Vector3>();   // x, half-size, z
        Vector2 heart2, hill2, school2; bool hasHill, hasSchool; Vector3 GU = Vector3.forward, GV = Vector3.right;

        void MakeMaterials()
        {
            asphalt = WorldMaterials.Solid(new Color(.2f, .2f, .23f), .7f); concrete = WorldMaterials.Solid(new Color(.62f, .6f, .6f)); concreteDark = WorldMaterials.Solid(new Color(.34f, .34f, .37f));
            tile = WorldMaterials.Solid(new Color(.25f, .28f, .36f)); wood = WorldMaterials.Solid(new Color(.55f, .38f, .24f)); woodDark = WorldMaterials.Solid(new Color(.25f, .17f, .12f));
            red = WorldMaterials.Solid(Red); metal = WorldMaterials.Solid(new Color(.55f, .57f, .6f), .7f); dark = WorldMaterials.Solid(new Color(.07f, .07f, .09f));
            glassLit = new Material(WorldMaterials.Glow(new Color(1f, .82f, .55f), 1.2f)) { name = "City windows" };
            glassCool = new Material(WorldMaterials.Glow(new Color(.6f, .85f, 1f), 1f)) { name = "City windows cool" };
            lanternM = new Material(WorldMaterials.Glow(new Color(1f, .35f, .2f), 1.8f)) { name = "Paper lanterns" };
            blossomM = WorldMaterials.Solid(Blossom); bark = WorldMaterials.Solid(new Color(.3f, .2f, .18f));
            vendM = new Material(WorldMaterials.Glow(new Color(.9f, .95f, 1f), 1.4f)) { name = "Vending machines" };
        }

        Material GlowM(Color c, float s = 2f) => WorldMaterials.Glow(c, s * .65f);
        static bool HasStation(IslandWorldData.Artist a) => a.stationId != 0;
        static float Foot(IslandWorldData.Artist a)
        {
            switch (a.name)
            {
                case "Hoshikuma Minami": return 16f;
                case "Sithu Aye": return 15f;
                default: return HasStation(a) ? 12f : 8f;
            }
        }

        public override bool Ground(float height, float slope, float patch, out Color color)
        {
            if (height < 2.6f) color = Color.Lerp(new Color(.6f, .55f, .48f), new Color(.68f, .62f, .52f), patch);
            else if (slope > 26f) color = Color.Lerp(new Color(.36f, .34f, .33f), new Color(.45f, .42f, .4f), patch);
            else color = Color.Lerp(new Color(.32f, .45f, .3f), new Color(.45f, .52f, .34f), Mathf.SmoothStep(0f, 1f, patch));
            color.a = 1f;
            return true;
        }

        static void BoxCol(Transform parent, Vector3 center, Vector3 size, Quaternion rot)
        {
            var go = new GameObject("Box collider"); go.transform.SetParent(parent, false); go.transform.SetPositionAndRotation(center, rot);
            go.AddComponent<BoxCollider>().size = size; go.isStatic = true;
        }

        static float Ease(float a, float b, float x) { var t = Mathf.Clamp01((x - a) / (b - a)); return t * t * (3f - 2f * t); }
        bool InCity(float x, float z) => Vector2.Distance(new Vector2(x, z), heart2) < CityR;

        // ---------------------------------------------------------------- shaping the ground
        void ShapeIsles(IslandWorldData data, int index)
        {
            var isl = data.layout.islands[index]; heart2 = new Vector2(isl.labelX, isl.labelZ);
            bool Inside(Vector2 p, float m) { if (data.IslandAt(p.x, p.y) != isl) return false; for (var k = 0; k < 16; k++) { var a = k * Mathf.PI / 8f; if (data.IslandAt(p.x + Mathf.Cos(a) * m, p.y + Mathf.Sin(a) * m) != isl || data.HeightAt(p.x + Mathf.Cos(a) * m, p.y + Mathf.Sin(a) * m) < 2f) return false; } return true; }
            // the hill: the highest ground well inside the land, out of the city
            hasHill = false; var bestH = float.MinValue;
            for (var r = 0; r < data.Nz; r += 2)
            {
                var z = data.layout.depth - r * data.Step; if (Mathf.Abs(z - heart2.y) > 380f) continue;
                for (var col = 0; col < data.Nx; col += 2)
                {
                    var x = col * data.Step; if (Mathf.Abs(x - heart2.x) > 380f) continue;
                    var i = r * data.Nx + col; if (data.islandIds[i] != index) continue;
                    var d = Vector2.Distance(new Vector2(x, z), heart2); if (d < CityR + 90f || d > 360f) continue;
                    if (data.heights[i] <= bestH || !Inside(new Vector2(x, z), 45f)) continue;
                    bestH = data.heights[i]; hill2 = new Vector2(x, z); hasHill = true;
                }
            }
            var toHill = hasHill ? (hill2 - heart2).normalized : Vector2.up;
            GV = new Vector3(toHill.x, 0, toHill.y); GU = Vector3.Cross(Vector3.up, GV);
            // the school: two thirds of the way up toward the shrine, on a level pad
            hasSchool = false;
            if (hasHill)
                foreach (var f in new[] { .66f, .58f, .74f, .5f })
                    foreach (var off in new[] { 0f, 30f, -30f, 50f, -50f })
                    {
                        var p = Vector2.Lerp(heart2, hill2, f) + new Vector2(-toHill.y, toHill.x) * off;
                        if (Inside(p, 34f) && Vector2.Distance(p, hill2) > 60f) { school2 = p; hasSchool = true; goto found; }
                    }
            found:
            // Hoshikuma Minami in the city centre, Sithu Aye at the school
            var keep = new List<string>();
            foreach (var a in data.layout.artists)
            {
                if (a.sceneId != isl.sceneId) continue;
                if (a.name == "Hoshikuma Minami") { var p = heart2 - toHill * 40f + new Vector2(-toHill.y, toHill.x) * 12f; a.x = p.x; a.z = p.y; keep.Add(a.name); }
                if (a.name == "Sithu Aye" && hasSchool) { var p = school2 + new Vector2(-toHill.y, toHill.x) * 34f; if (!Inside(p, 22f)) p = school2 - new Vector2(-toHill.y, toHill.x) * 34f; a.x = p.x; a.z = p.y; keep.Add(a.name); }
            }
            var avoid = new List<Vector3>();
            if (hasHill) avoid.Add(new Vector3(hill2.x, 34f, hill2.y));
            if (hasSchool) avoid.Add(new Vector3(school2.x, 28f, school2.y));
            ArtistPlacement.Spread(data, index, 44f, heart2, 46f, 30f, avoid, keep, Land, 380f);
            // the city: one level of ground, easing into the hills round it
            Level(data, index, heart2, CityR, CityR + 70f);
            foreach (var a in data.layout.artists)
            {
                if (a.sceneId != isl.sceneId) continue;
                a.r = Mathf.Max(a.r, Foot(a) + 6f);
                if (!InCity(a.x, a.z)) Level(data, index, new Vector2(a.x, a.z), Foot(a) + 1f, Foot(a) + 10f);
            }
            if (hasSchool) Level(data, index, school2, 26f, 44f);
            if (hasHill) Level(data, index, hill2, 18f, 34f);
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
            pathCells.Clear(); walkPoints.Clear(); facing.Clear(); noGrass.Clear(); buildingsAt.Clear();
            center = ctx.Plaza;
            life = new GameObject("J-Rock life").AddComponent<JRockLife>();
            life.transform.SetParent(ctx.Root, false);
            life.center = center; life.lanterns = lanternM; life.windows = glassLit; life.windowsCool = glassCool; life.vending = vendM;
            var mk = new MeshKit(); var solid = new MeshKit();
            Remember(ctx.OnGround(center.x, center.z), 0);
            Arrival(ctx, mk, solid);
            Paths(ctx, mk, solid);
            City(ctx, mk, solid);
            if (hasHill) Shrine(ctx, mk, solid);
            if (hasSchool) School(ctx, mk, solid);
            Train(ctx, mk, solid);
            IslesPlaces(ctx, mk, solid);
            Wilds(ctx, mk, solid);
            mk.Build("The J-Rock Isles", ctx.Root, false);
            solid.BuildCollider("The J-Rock Isles walls", ctx.Root);
        }

        public override void Finish(LandContext ctx) { Grass(ctx); halos.Build("Isles glow", ctx.Root); }

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

        bool NearBuilding(float x, float z, float pad) { foreach (var b in buildingsAt) if (Mathf.Abs(x - b.x) < b.y + pad && Mathf.Abs(z - b.z) < b.y + pad) return true; return false; }

        bool Open(LandContext ctx, float x, float z, float pad) =>
            ctx.IsLand(x, z, 4f) && !InFootprint(ctx, x, z, pad) && !NearPath(x, z, pad + 2f) && !InNoGrass(x, z, pad) && !NearBuilding(x, z, pad)
            && Vector2.Distance(new Vector2(x, z), new Vector2(center.x, center.z)) > 18f + pad;

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
                // each edge follows its own ground, lifted a little only where the middle bulges
                float gl = ground.Ground(L[i].x, L[i].z), gr = ground.Ground(R[i].x, R[i].z), bulge = Mathf.Clamp(pts[i].y - (gl + gr) / 2f, 0f, .3f);
                L[i].y = gl + bulge + lift; R[i].y = gr + bulge + lift;
            }
            for (var i = 0; i + 1 < n; i++) mk.Quad(m, L[i], R[i], R[i + 1], L[i + 1], (pts[i] + pts[i + 1]) / 2f - Vector3.up);
        }

        /// A red paper lantern hanging from a short bracket.
        void PaperLantern(MeshKit mk, Vector3 at, System.Random rng)
        {
            mk.Blob(lanternM, at, new Vector3(.24f, .32f, .24f), rng, 0f);
            mk.Prism(dark, at + Vector3.up * .3f, Vector3.up, .14f, .06f, 8); mk.Prism(dark, at - Vector3.up * .36f, Vector3.up, .14f, .06f, 8);
            halos.Add(at, 1.1f, new Color(1f, .4f, .25f), .5f);
        }

        /// A string of red paper lanterns sagging between two points.
        void LanternString(MeshKit mk, Vector3 a, Vector3 b, System.Random rng)
        {
            var n = Mathf.Max(3, Mathf.RoundToInt(Vector3.Distance(a, b) / 2.2f));
            for (var j = 0; j <= n * 2; j++) { float t0 = j / (n * 2f), t1 = (j + 1) / (n * 2f); var p0 = Vector3.Lerp(a, b, t0) - Vector3.up * Mathf.Sin(t0 * Mathf.PI) * .9f; var p1 = Vector3.Lerp(a, b, Mathf.Min(1f, t1)) - Vector3.up * Mathf.Sin(Mathf.Min(1f, t1) * Mathf.PI) * .9f; if (j < n * 2) mk.Box(dark, (p0 + p1) / 2f, new Vector3(.02f, .02f, Vector3.Distance(p0, p1)), Quaternion.LookRotation(p1 - p0)); }
            for (var j = 1; j < n; j++) { var t = j / (float)n; PaperLantern(mk, Vector3.Lerp(a, b, t) - Vector3.up * (Mathf.Sin(t * Mathf.PI) * .9f + .45f), rng); }
        }

        /// A vending machine: a glowing front, a dark body, a strip of coloured buttons.
        void Vending(LandContext ctx, MeshKit mk, Vector3 g, Vector3 face, int i)
        {
            var up = Vector3.up; var rot = Quaternion.LookRotation(face); i = Mathf.Abs(i); var body = WorldMaterials.Solid(new[] { new Color(.85f, .2f, .2f), new Color(.2f, .4f, .8f), new Color(.92f, .92f, .92f) }[i % 3]);
            mk.Box(body, g + up * .95f, new Vector3(1f, 1.9f, .75f), rot);
            mk.Box(vendM, g + up * 1.25f + face * .38f, new Vector3(.8f, .9f, .02f), rot);
            for (var k = 0; k < 6; k++) mk.Box(GlowM(NeonC[(i + k) % NeonC.Length], 1.6f), g + up * (1.25f + ((k / 3) - .5f) * .4f) + face * .4f + Vector3.Cross(up, face) * ((k % 3 - 1) * .24f), new Vector3(.14f, .3f, .02f), rot);
            halos.Add(g + up * 1.2f + face * .8f, 1.4f, new Color(.9f, .95f, 1f), .5f);
            BoxCol(ctx.Root, g + up * .95f, new Vector3(1f, 1.9f, .75f), rot);
        }

        /// A cherry tree in full blossom, petals on the ground under it in small patches.
        void Cherry(LandContext ctx, MeshKit mk, Vector3 at, float h)
        {
            var up = Vector3.up;
            mk.Prism(bark, at - up * .3f, (up + new Vector3(ctx.Range(-.15f, .15f), 0, ctx.Range(-.15f, .15f))).normalized, h * .05f + .12f, h * .62f, 6, .7f);
            for (var k = 0; k < 5; k++) mk.Blob(blossomM, at + up * h * (.72f + (k % 2) * .08f) + new Vector3(ctx.Range(-1f, 1f), 0, ctx.Range(-1f, 1f)) * h * .16f, Vector3.one * h * ctx.Range(.15f, .2f), ctx.Rng, .3f);
            for (var k = 0; k < 4; k++) { var a = k * Mathf.PI / 2f + ctx.Range(0f, 1f); var r = ctx.Range(.6f, h * .3f); var q = ctx.OnGround(at.x + Mathf.Cos(a) * r, at.z + Mathf.Sin(a) * r); mk.Blob(blossomM, q + up * .01f, new Vector3(ctx.Range(.4f, .7f), .02f, ctx.Range(.4f, .7f)), ctx.Rng, .3f); }
            halos.Add(at + up * h * .75f, h * .3f, Blossom, .14f);
            ArtistBits.PostCollider(ctx.Root, at, h * .55f, h * .05f + .16f);
            life.blossomAt.Add(at + up * h * .7f);
        }

        /// A torii: two red pillars, a black top beam curving up at the ends, a red tie beam.
        void Torii(LandContext ctx, MeshKit mk, Vector3 g, Vector3 face, float s = 1f)
        {
            var up = Vector3.up; var sd = Vector3.Cross(up, face); var rot = Quaternion.LookRotation(face);
            foreach (var e in new[] { -1f, 1f }) { mk.Prism(red, g + sd * e * 1.6f * s - up * .2f, up, .2f * s, 4.4f * s, 10); mk.Prism(dark, g + sd * e * 1.6f * s - up * .2f, up, .26f * s, .5f * s, 10); ArtistBits.PostCollider(ctx.Root, g + sd * e * 1.6f * s, 4f * s, .22f * s); }
            mk.Box(red, g + up * 3.4f * s, new Vector3(3.9f * s, .25f * s, .26f * s), Quaternion.LookRotation(face));
            mk.Box(dark, g + up * 4.25f * s, new Vector3(4.8f * s, .28f * s, .4f * s), rot);
            foreach (var e in new[] { -1f, 1f }) mk.Box(dark, g + up * 4.35f * s + sd * e * 2.55f * s, new Vector3(.7f * s, .22f * s, .4f * s), rot * Quaternion.Euler(0, 0, e * -12f));
            mk.Box(red, g + up * 3.9f * s, new Vector3(.3f * s, .55f * s, .2f * s), rot);
        }

        // ---------------------------------------------------------------- arrival
        /// Where you arrive: a square in the middle of the city, a crossing painted across it, a big neon screen on the
        /// building opposite, vending machines and a bench, a cherry tree, red lanterns strung overhead.
        void Arrival(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var c = ctx.OnGround(center.x, center.z);
            mk.Box(concrete, c - up * .15f, new Vector3(26f, .4f, 26f), Quaternion.LookRotation(GV));
            for (var k = -5; k <= 5; k++) { mk.Box(WorldMaterials.Solid(new Color(.92f, .92f, .9f)), c + up * .06f + GU * k * 1.6f, new Vector3(.8f, .02f, 5f), Quaternion.LookRotation(GV)); mk.Box(WorldMaterials.Solid(new Color(.92f, .92f, .9f)), c + up * .06f + GV * k * 1.6f, new Vector3(.8f, .02f, 5f), Quaternion.LookRotation(GU)); }
            foreach (var e in new[] { -1f, 1f }) Vending(ctx, mk, ctx.OnGround(c.x + GU.x * e * 11f + GV.x * 9f, c.z + GU.z * e * 11f + GV.z * 9f), -GV, e > 0 ? 0 : 1);
            Cherry(ctx, mk, ctx.OnGround(c.x - GU.x * 10f - GV.x * 10f, c.z - GU.z * 10f - GV.z * 10f), 7f);
            var p0 = c + GU * 13f - GV * 13f + up * 6f; var p1 = c - GU * 13f + GV * 13f + up * 6f;
            foreach (var p in new[] { p0, p1 }) { var g = ctx.OnGround(p.x, p.z); mk.Prism(concreteDark, g - up * .2f, up, .16f, 6.4f, 8); ArtistBits.PostCollider(ctx.Root, g, 6f, .18f); }
            LanternString(mk, p0, p1, ctx.Rng);
            var sign = c - GV * 13f + up * 3.4f;
            mk.Box(dark, sign, new Vector3(9f, 2.2f, .3f), Quaternion.LookRotation(GV)); solid.Box(dark, sign - up * 1.4f, new Vector3(9f, 5f, .4f), Quaternion.LookRotation(GV));
            foreach (var y in new[] { -1.05f, 1.05f }) mk.Box(GlowM(Pink, 2.4f), sign + up * y + GV * .17f, new Vector3(9f, .08f, .04f), Quaternion.LookRotation(GV));
            ArtistBits.Label(ctx.Root, "THE J-ROCK ISLES", sign + GV * .18f, GV, .55f, Cyan, TextAnchor.MiddleCenter);
            halos.Add(sign + GV * 1.6f, 5f, Pink, .3f);
            ArtistBits.PointLight(ctx.Root, c + up * 6f, Amber, 26f, 1.6f);
            noGrass.Add(new Vector3(c.x, 16f, c.z)); buildingsAt.Add(new Vector3(c.x, 14f, c.z));
            var mark = new GameObject("Shot: the arrival and the city").transform; mark.SetParent(ctx.Root, false);
            var cam = c - GV * 10f + GU * 4f + up * 1.7f; mark.SetPositionAndRotation(cam, Quaternion.LookRotation((GV * 80f + up * 14f).normalized));
        }

        // ---------------------------------------------------------------- paths
        void Paths(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var path = WorldMaterials.Solid(new Color(.5f, .48f, .46f));
            var walk = 1;
            void Lay(Vector3 join, Vector3 to, float stopShort, string artist, bool torii)
            {
                var from = new Vector3(join.x, 0, join.z); to.y = 0;
                var dir = to - from; var total = dir.magnitude; if (total < 2f) return; dir /= total;
                if (artist != null) facing[artist] = -dir;
                var side = Vector3.Cross(Vector3.up, dir); var end = total - stopShort;
                var pts = new List<Vector3>();
                for (var d = walk == 1 ? 14f : PathW / 2f + .1f; ; d += 1.5f)
                {
                    var dd = Mathf.Min(d, end); var q = from + dir * dd + side * Mathf.Sin(dd / 12f) * Mathf.Min(2.5f, end * .04f);
                    if (!ctx.IsLand(q.x, q.z, 0f)) break;
                    pts.Add(ctx.OnGround(q.x, q.z)); if (dd >= end) break;
                }
                if (pts.Count < 2) return;
                Strip(mk, pts, path, PathW, .16f, ctx);
                foreach (var p in pts) Remember(p, walk);
                // out of the city, lanterns on posts; on the way up to the shrine, a row of red torii
                for (var i = 6; i < pts.Count; i += torii ? 4 : 10)
                {
                    if (InCity(pts[i].x, pts[i].z)) continue;
                    var a = pts[Mathf.Min(i + 1, pts.Count - 1)] - pts[i - 1]; a.y = 0; a.Normalize();
                    if (torii && i > pts.Count - 46) { Torii(ctx, mk, pts[i], a, .9f); continue; }
                    if (torii) continue;
                    var s = (i / 10) % 2 == 0 ? 1f : -1f; var lp = ctx.OnGround(pts[i].x + side.x * s * (PathW / 2f + .9f), pts[i].z + side.z * s * (PathW / 2f + .9f));
                    if (InFootprint(ctx, lp.x, lp.z, 1f)) continue;
                    mk.Box(woodDark, lp + Vector3.up * 1.4f, new Vector3(.14f, 2.8f, .14f), Quaternion.identity); mk.Box(woodDark, lp + Vector3.up * 2.75f + side * s * -.3f, new Vector3(.08f, .08f, .7f), Quaternion.LookRotation(side));
                    PaperLantern(mk, lp + Vector3.up * 2.3f + side * s * -.55f, ctx.Rng); ArtistBits.PostCollider(ctx.Root, lp, 2.7f, .12f);
                }
                walk++;
            }
            if (hasHill) Lay(ctx.OnGround(center.x, center.z), new Vector3(hill2.x, 0, hill2.y), 12f, null, true);
            if (hasSchool) Lay(ctx.OnGround(center.x, center.z), new Vector3(school2.x, 0, school2.y), 22f, null, false);
            var todo = new List<IslandWorldData.Artist>(ctx.Artists);
            while (todo.Count > 0)
            {
                IslandWorldData.Artist next = null; Vector3 join = default; var best = float.MaxValue;
                foreach (var a in todo)
                    foreach (var (p, w) in walkPoints) { var d = Vector2.Distance(new Vector2(p.x, p.z), new Vector2(a.x, a.z)) - Foot(a); if (d < best) { best = d; next = a; join = p; } }
                if (next == null) break;
                todo.Remove(next);
                Lay(join, new Vector3(next.x, 0, next.z), Foot(next) * .8f, next.name, false);
            }
        }

        // ---------------------------------------------------------------- the city
        /// Streets on a grid round the arrival, buildings filling the blocks: tall concrete blocks with rows of lit
        /// windows, vertical neon signs on their corners, water tanks and billboards on the roofs, a red torii on the
        /// tallest; vending machines, power poles with tangled wires, lanterns strung across the streets.
        void City(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var c0 = ctx.OnGround(center.x, center.z); var y0 = c0.y;
            Vector3 P(float u, float v) => new Vector3(center.x, y0, center.z) + GU * u + GV * v;
            var n = Mathf.CeilToInt(CityR / Block);
            var white = WorldMaterials.Solid(new Color(.9f, .9f, .88f));
            // the streets
            for (var k = -n; k <= n; k++)
            {
                var off = (k + .5f) * Block;
                foreach (var along in new[] { true, false })
                    for (var t = -CityR; t < CityR; t += 4f)
                    {
                        var a = along ? P(off, t) : P(t, off); var b = along ? P(off, t + 4f) : P(t + 4f, off); var m = (a + b) / 2f;
                        if (!InCity(m.x, m.z) || !ctx.IsLand(m.x, m.z, 2f)) continue;
                        if (Vector2.Distance(new Vector2(m.x, m.z), new Vector2(center.x, center.z)) < 14f) continue;
                        var dir = b - a; var sd = Vector3.Cross(up, dir.normalized);
                        mk.Box(asphalt, new Vector3(m.x, ctx.Ground(m.x, m.z) + .05f, m.z), new Vector3(StreetW, .1f, 4.05f), Quaternion.LookRotation(dir));
                        if (Mathf.Repeat(t, 8f) < .1f) mk.Box(white, new Vector3(m.x, ctx.Ground(m.x, m.z) + .11f, m.z), new Vector3(.15f, .02f, 2f), Quaternion.LookRotation(dir));
                    }
            }
            // the buildings, four to a block
            var made = 0; var tallest = 0f; Vector3 tallestTop = default; Vector3 tallestFace = GV;
            for (var i = -n; i < n; i++)
            for (var j = -n; j < n; j++)
                for (var q = 0; q < 4; q++)
                {
                    var u = (i + 1f) * Block + ((q % 2) - .5f) * Block * .5f - Block * .5f; var v = (j + 1f) * Block + ((q / 2) - .5f) * Block * .5f - Block * .5f;
                    var c = P(u, v); var half = Block * .25f - 1.2f;
                    if (!InCity(c.x, c.z) || !ctx.IsLand(c.x, c.z, 8f)) continue;
                    var near = false;
                    foreach (var e in new[] { (-1f, -1f), (1f, -1f), (1f, 1f), (-1f, 1f), (0f, 0f) }) { var pp = c + GU * e.Item1 * half + GV * e.Item2 * half; if (NearPath(pp.x, pp.z, 3f) || InFootprint(ctx, pp.x, pp.z, 3f) || NearBuilding(pp.x, pp.z, 1f)) near = true; }
                    if (near) continue;
                    var g = float.MaxValue; foreach (var e in new[] { (-1f, -1f), (1f, -1f), (1f, 1f), (-1f, 1f) }) { var pp = c + GU * e.Item1 * half + GV * e.Item2 * half; g = Mathf.Min(g, ctx.Ground(pp.x, pp.z)); }
                    var d = Vector2.Distance(new Vector2(c.x, c.z), new Vector2(center.x, center.z)) / CityR;
                    var h = Mathf.Lerp(26f, 8f, d) * ctx.Range(.6f, 1.3f);
                    var face = (q % 2 == 0 ? -GU : GU);
                    Building(ctx, mk, solid, new Vector3(c.x, g, c.z), half, h, face, made);
                    buildingsAt.Add(new Vector3(c.x, half, c.z)); noGrass.Add(new Vector3(c.x, half * 1.5f, c.z));
                    if (h > tallest) { tallest = h; tallestTop = new Vector3(c.x, g + h, c.z); tallestFace = face; }
                    made++;
                }
            // a torii on the tallest roof, high over the city
            if (tallest > 0f) { Torii(ctx, mk, tallestTop + up * .3f, -tallestFace, 1.3f); halos.Add(tallestTop + up * 5f, 4f, new Color(1f, .4f, .3f), .3f); ArtistBits.PointLight(ctx.Root, tallestTop + up * 4f - tallestFace * 3f, new Color(1f, .5f, .35f), 16f, 1.6f); }
            // vending machines, power poles and lanterns along the streets
            var poles = new List<Vector3>();
            for (var i = -n; i <= n; i++)
            for (var j = -n; j <= n; j++)
            {
                var corner = P((i + .5f) * Block + StreetW * .5f + .6f, (j + .5f) * Block + StreetW * .5f + .6f);
                if (!InCity(corner.x, corner.z) || !ctx.IsLand(corner.x, corner.z, 3f) || InFootprint(ctx, corner.x, corner.z, 2f) || NearPath(corner.x, corner.z, 2f)) continue;
                var g = ctx.OnGround(corner.x, corner.z);
                mk.Prism(concreteDark, g - up * .2f, up, .16f, 8.2f, 8); mk.Box(concreteDark, g + up * 7.6f, new Vector3(1.6f, .12f, .12f), Quaternion.LookRotation(GV)); ArtistBits.PostCollider(ctx.Root, g, 8f, .18f);
                mk.Prism(concreteDark, g + up * 6.8f + GU * .3f, up, .25f, .7f, 8);
                poles.Add(g + up * 7.6f);
                if (ctx.Chance(.55f)) { var vp = g + GU * 1.6f + GV * 1.2f; if (!NearPath(vp.x, vp.z, 1.5f)) Vending(ctx, mk, ctx.OnGround(vp.x, vp.z), -GU, i + j); }
                ArtistBits.PointLight(ctx.Root, g + up * 6.5f, Amber, 16f, 1.4f);
                halos.Add(g + up * 7f, 1.6f, Amber, .4f);
            }
            // tangled wires between neighbouring poles, sagging, two or three to a span
            foreach (var a in poles)
                foreach (var b in poles)
                {
                    var d = Vector3.Distance(a, b); if (d < 1f || d > Block * 1.1f || a.x > b.x) continue;
                    for (var w = 0; w < 3; w++)
                    {
                        var oa = a + up * (w * .25f - .25f) + GU * (w - 1) * .4f; var ob = b + up * (w * .2f - .2f) + GV * (w - 1) * .4f; var last = oa;
                        for (var k = 1; k <= 8; k++) { var t = k / 8f; var p = Vector3.Lerp(oa, ob, t) - up * Mathf.Sin(t * Mathf.PI) * (.6f + w * .25f); mk.Box(dark, (last + p) / 2f, new Vector3(.03f, .03f, Vector3.Distance(last, p)), Quaternion.LookRotation(p - last)); last = p; }
                    }
                    if (ctx.Chance(.3f)) LanternString(mk, a - up * 1.2f, b - up * 1.2f, ctx.Rng);
                }
            Debug.Log($"The J-Rock Isles: {made} city buildings, {poles.Count} power poles, tallest {tallest:F0} m");
        }

        /// A city building: concrete block, rows of windows (most lit warm, some cool), a vertical neon sign on a corner,
        /// a water tank or billboard on the roof, a lit shopfront at street level.
        void Building(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 g, float half, float h, Vector3 face, int i)
        {
            var up = Vector3.up; var rot = Quaternion.LookRotation(face); var sd = Vector3.Cross(up, face);
            var w = half * 2f; var wallM = i % 3 == 0 ? concreteDark : (i % 3 == 1 ? concrete : WorldMaterials.Solid(new[] { new Color(.72f, .62f, .58f), new Color(.55f, .62f, .68f), new Color(.75f, .72f, .62f) }[i % 3]));
            mk.Box(wallM, g + up * (h / 2f - .5f), new Vector3(w, h + 1f, w), rot); solid.Box(wallM, g + up * (h / 2f - .5f), new Vector3(w, h + 1f, w), rot);
            var front = g + face * (half + .02f);
            for (var f = 1; f * 3.2f + 2f < h; f++)
                for (var k = 0; k < 3; k++)
                {
                    var lit = (i * 7 + f * 3 + k) % 5;
                    if (lit == 4) continue;
                    mk.Box(lit < 2 ? glassLit : (lit == 2 ? glassCool : WorldMaterials.Solid(new Color(.15f, .16f, .2f))), front + up * (f * 3.2f + .6f) + sd * ((k - 1f) * w * .3f), new Vector3(w * .22f, 1.4f, .04f), rot);
                }
            // the shopfront at street level
            mk.Box(GlowM(NeonC[i % NeonC.Length] * .6f + Color.white * .4f, 1.2f), front + up * 1.3f, new Vector3(w * .7f, 2.2f, .04f), rot);
            mk.Box(WorldMaterials.Solid(NeonC[(i + 2) % NeonC.Length] * .55f), front + up * 2.7f + face * .6f, new Vector3(w * .75f, .1f, 1.2f), rot * Quaternion.Euler(-12f, 0, 0));
            halos.Add(front + up * 1.4f + face * 1.4f, w * .35f, NeonC[i % NeonC.Length], .35f);
            // a vertical neon sign on the corner
            if (h > 10f)
            {
                var corner = g + face * (half + .4f) + sd * (half - .3f); var col = NeonC[(i + 1) % NeonC.Length]; var sh = Mathf.Min(h - 4f, 9f);
                mk.Box(dark, corner + up * (3.5f + sh / 2f), new Vector3(.2f, sh, 1.1f), rot);
                for (var k = 0; k < Mathf.FloorToInt(sh / 1.1f); k++) mk.Box(GlowM(col, 2.4f), corner + up * (4f + k * 1.1f) + face * .11f, new Vector3(.04f, .7f, .7f), rot * Quaternion.Euler(0, 0, (k % 2) * 90f));
                halos.Add(corner + up * (3.5f + sh / 2f) + face * .6f, sh * .45f, col, .4f);
                life.neon.Add(corner + up * (3.5f + sh / 2f));
            }
            // on the roof: a water tank on legs, or a lit billboard
            var roof = g + up * h;
            if (i % 2 == 0) { var t = roof + sd * half * .4f - face * half * .3f; foreach (var e in new[] { (-1f, -1f), (1f, -1f), (1f, 1f), (-1f, 1f) }) mk.Box(metal, t + new Vector3(e.Item1, 0, e.Item2) * .7f + up * .8f, new Vector3(.12f, 1.6f, .12f), Quaternion.identity); mk.Prism(metal, t + up * 1.6f, up, 1.2f, 2f, 10); mk.Cone(metal, t + up * 3.6f, 1.25f, .6f, 10); }
            else { var bb = roof + up * 2.4f; mk.Box(dark, bb - up * 1.2f, new Vector3(.2f, 2.4f, .2f), rot); var col = NeonC[(i + 3) % NeonC.Length]; mk.Box(GlowM(Color.Lerp(col, Color.white, .3f), 1.6f), bb + up * 1.2f, new Vector3(w * .8f, 2.6f, .2f), rot); halos.Add(bb + up * 1.2f + face, w * .5f, col, .3f); }
        }

        // ---------------------------------------------------------------- the shrine on the hill
        /// At the top of the hill: a stone platform, a small shrine hall with a deep sweeping roof, a red torii before it,
        /// stone lanterns, a rope and bell, an offering box, and cherry trees round it; the view down over the city.
        void Shrine(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var c = ctx.OnGround(hill2.x, hill2.y); var toCity = new Vector3(center.x - c.x, 0, center.z - c.z).normalized; var rot = Quaternion.LookRotation(toCity); var sd = Vector3.Cross(up, toCity);
            mk.Box(WorldMaterials.Solid(new Color(.55f, .53f, .5f)), c - up * .2f, new Vector3(22f, .6f, 22f), rot); solid.Box(concrete, c - up * .2f, new Vector3(22f, .6f, 22f), rot);
            var hall = c - toCity * 5f;
            mk.Box(wood, hall + up * 2.2f, new Vector3(8f, 4f, 6f), rot); solid.Box(wood, hall + up * 2.2f, new Vector3(8f, 4f, 6f), rot);
            mk.Box(woodDark, hall + up * .3f + toCity * 3.2f, new Vector3(8.4f, .6f, 1.2f), rot);
            mk.GableRoof(tile, hall + up * 4.2f, new Vector3(9.6f, 0, 8.6f), 3.2f, Quaternion.LookRotation(sd));
            mk.Box(GlowM(Amber, 1.4f), hall + up * 1.8f + toCity * 3.02f, new Vector3(3f, 2.4f, .04f), rot);
            mk.Box(woodDark, hall + up * .6f + toCity * 4.2f, new Vector3(2f, 1f, .9f), rot); BoxCol(ctx.Root, hall + up * .6f + toCity * 4.2f, new Vector3(2f, 1f, .9f), rot);
            mk.Box(WorldMaterials.Solid(new Color(.9f, .85f, .7f)), hall + up * 2.9f + toCity * 4.2f, new Vector3(.12f, 2.2f, .12f), rot);
            mk.Blob(WorldMaterials.Solid(new Color(.85f, .7f, .3f), .9f), hall + up * 4.05f + toCity * 4.2f, Vector3.one * .3f, ctx.Rng, 0f);
            Torii(ctx, mk, ctx.OnGround(c.x + toCity.x * 8f, c.z + toCity.z * 8f) + up * .1f, toCity, 1.2f);
            foreach (var e in new[] { -1f, 1f }) { var lp = c + toCity * 3f + sd * e * 4f; StoneLantern(ctx, mk, lp); }
            foreach (var e in new[] { -1f, 1f }) Cherry(ctx, mk, ctx.OnGround(c.x + sd.x * e * 9f - toCity.x * 6f, c.z + sd.z * e * 9f - toCity.z * 6f), 7f);
            halos.Add(hall + up * 2f + toCity * 4f, 4f, Amber, .4f);
            ArtistBits.PointLight(ctx.Root, hall + up * 3f + toCity * 6f, Amber, 18f, 1.6f);
            noGrass.Add(new Vector3(c.x, 14f, c.z)); buildingsAt.Add(new Vector3(c.x, 12f, c.z)); ctx.AddKeepout(c.x, c.z, 14f);
            life.shrineAt = hall + up * 4f;
            var mark = new GameObject("Shot: the shrine on the hill").transform; mark.SetParent(ctx.Root, false);
            var cam = c + toCity * 18f + sd * 6f; cam = new Vector3(cam.x, Mathf.Max(ctx.Ground(cam.x, cam.z), c.y) + 2.2f, cam.z); mark.SetPositionAndRotation(cam, Quaternion.LookRotation((hall + up * 3f - cam).normalized));
        }

        void StoneLantern(LandContext ctx, MeshKit mk, Vector3 at)
        {
            var up = Vector3.up; at = ctx.OnGround(at.x, at.z); var g = WorldMaterials.Solid(new Color(.55f, .53f, .5f));
            mk.Prism(g, at - up * .1f, up, .45f, .3f, 6); mk.Prism(g, at + up * .2f, up, .16f, .9f, 6);
            mk.Box(g, at + up * 1.25f, new Vector3(.75f, .5f, .75f), Quaternion.identity); mk.Box(GlowM(Amber, 1.8f), at + up * 1.25f, new Vector3(.4f, .3f, .78f), Quaternion.identity);
            mk.Prism(g, at + up * 1.5f, up, .65f, .35f, 6, .2f);
            halos.Add(at + up * 1.25f, 1.1f, Amber, .6f); ArtistBits.PostCollider(ctx.Root, at, 1.6f, .45f);
        }

        // ---------------------------------------------------------------- the school
        /// Partway up the hill: a long three-storey school with rows of windows (a few classrooms still lit), a clock on
        /// the front, a fence round the roof, a gate and a dirt schoolyard with a running track marked on it.
        void School(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var c = ctx.OnGround(school2.x, school2.y); var toCity = new Vector3(center.x - c.x, 0, center.z - c.z).normalized; var rot = Quaternion.LookRotation(toCity); var sd = Vector3.Cross(up, toCity);
            const float W = 36f, D = 10f, H = 11f;
            var b = c - toCity * 9f;
            var cream = WorldMaterials.Solid(new Color(.9f, .88f, .8f));
            mk.Box(cream, b + up * (H / 2f - .5f), new Vector3(W, H + 1f, D), rot); solid.Box(cream, b + up * (H / 2f - .5f), new Vector3(W, H + 1f, D), rot);
            var front = b + toCity * (D / 2f + .02f);
            for (var f = 0; f < 3; f++)
                for (var k = 0; k < 9; k++)
                    mk.Box((f * 9 + k) % 7 == 0 ? glassLit : WorldMaterials.Solid(new Color(.3f, .4f, .5f), .8f), front + up * (1.6f + f * 3.4f) + sd * ((k - 4f) * 3.8f), new Vector3(2.6f, 1.6f, .04f), rot);
            mk.Prism(cream, front + up * (H + .2f), toCity, 1.4f, .3f, 20); mk.Prism(WorldMaterials.Solid(new Color(.95f, .95f, .95f)), front + up * (H + .2f) + toCity * .31f, toCity, 1.2f, .02f, 20);
            mk.Box(dark, front + up * (H + .35f) + toCity * .34f, new Vector3(.08f, .9f, .02f), rot); mk.Box(dark, front + up * (H + .2f) + toCity * .34f + sd * .3f, new Vector3(.6f, .08f, .02f), rot);
            for (var k = 0; k <= 18; k++) { var p = b + up * H + sd * ((k - 9f) * 2f) + toCity * (D / 2f - .2f); mk.Box(metal, p + up * .7f, new Vector3(.05f, 1.4f, .05f), rot); }
            mk.Box(metal, b + up * (H + 1.4f) + toCity * (D / 2f - .2f), new Vector3(W, .06f, .06f), rot);
            // the yard: packed dirt with a running track painted on
            var yard = c + toCity * 6f; var dirt = WorldMaterials.Solid(new Color(.62f, .52f, .4f));
            mk.Box(dirt, yard + up * .02f, new Vector3(W, .06f, 18f), rot);
            for (var k = 0; k < 48; k++) { float a0 = k * Mathf.PI / 24f, a1 = (k + 1) * Mathf.PI / 24f; Vector3 O(float a) => yard + sd * Mathf.Cos(a) * 12f + toCity * Mathf.Sin(a) * 6f + up * .07f; mk.Box(WorldMaterials.Solid(new Color(.95f, .95f, .92f)), (O(a0) + O(a1)) / 2f, new Vector3(.15f, .02f, Vector3.Distance(O(a0), O(a1))), Quaternion.LookRotation(O(a1) - O(a0))); }
            // the gate
            var gate = yard + toCity * 9.5f; foreach (var e in new[] { -1f, 1f }) { var gp = gate + sd * e * 2.6f; mk.Box(concrete, gp + up * 1.2f, new Vector3(.6f, 2.4f, .6f), rot); BoxCol(ctx.Root, gp + up * 1.2f, new Vector3(.6f, 2.4f, .6f), rot); }
            foreach (var e in new[] { -1f, 1f }) Cherry(ctx, mk, ctx.OnGround(gate.x + sd.x * e * 6f, gate.z + sd.z * e * 6f), 6f);
            halos.Add(front + up * 6f + toCity * 3f, 9f, new Color(1f, .85f, .6f), .15f);
            ArtistBits.PointLight(ctx.Root, front + up * 4f + toCity * 6f, new Color(1f, .85f, .65f), 22f, 1.4f);
            noGrass.Add(new Vector3(c.x, 22f, c.z)); buildingsAt.Add(new Vector3(c.x, 19f, c.z)); ctx.AddKeepout(c.x, c.z, 22f);
            life.schoolRoof = b + up * (H + .2f);
            var mark = new GameObject("Shot: the school on the hill").transform; mark.SetParent(ctx.Root, false);
            var cam = c + toCity * 26f + sd * 10f; cam = ctx.OnGround(cam.x, cam.z) + up * 1.7f; mark.SetPositionAndRotation(cam, Quaternion.LookRotation((b + up * 5f - cam).normalized));
        }

        // ---------------------------------------------------------------- the train along the coast
        /// A track on low concrete piers following the coast, catenary poles over it; a short commuter train runs along
        /// it and back (JRockLife), its windows lit. Where it passes nearest the city, a level crossing with blinking
        /// lights and a crossing sign.
        void Train(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up;
            // the coast: walk round the city at the distance where the land meets the sea, keeping on land
            var pts = new List<Vector3>();
            for (var k = 0; k < 120; k++)
            {
                var a = k * Mathf.PI * 2f / 120f; var d = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)); float edge = 0f;
                for (var s = 30f; s < 420f; s += 6f) { var q = center + d * s; if (!ctx.IsLand(q.x, q.z, 0f)) break; edge = s; }
                var r = edge - 14f; if (r < CityR + 10f) { pts.Add(Vector3.positiveInfinity); continue; }
                var p = center + d * r; var g = ctx.Ground(p.x, p.z);
                if (g > 12f || InFootprint(ctx, p.x, p.z, 4f)) { pts.Add(Vector3.positiveInfinity); continue; }
                pts.Add(new Vector3(p.x, Mathf.Max(g, ctx.Data.layout.waterLevel + 1f) + 1.4f, p.z));
            }
            // the longest unbroken run
            List<Vector3> best = new List<Vector3>(), run = new List<Vector3>();
            for (var k = 0; k < pts.Count * 2; k++) { var p = pts[k % pts.Count]; if (float.IsInfinity(p.x)) { if (run.Count > best.Count) best = run; run = new List<Vector3>(); } else run.Add(p); if (run.Count >= pts.Count) break; }
            if (run.Count > best.Count) best = run;
            if (best.Count < 8) { Debug.Log("The J-Rock Isles: no coast for the train"); return; }
            for (var pass = 0; pass < 3; pass++) for (var k = 1; k < best.Count - 1; k++) { var p = best[k]; p.y = (best[k - 1].y + p.y * 2f + best[k + 1].y) / 4f; best[k] = p; }
            for (var k = 0; k + 1 < best.Count; k++)
            {
                var a = best[k]; var b = best[k + 1]; var d = b - a; var rot = Quaternion.LookRotation(d); var len = d.magnitude;
                mk.Box(concrete, (a + b) / 2f, new Vector3(3.6f, .5f, len + .1f), rot); solid.Box(concrete, (a + b) / 2f, new Vector3(3.6f, .5f, len + .1f), rot);
                var sd = Vector3.Cross(up, new Vector3(d.x, 0, d.z).normalized);
                foreach (var e in new[] { -.7f, .7f }) mk.Box(metal, (a + b) / 2f + sd * e + up * .32f, new Vector3(.1f, .12f, len + .1f), rot);
                var g = ctx.Ground(a.x, a.z); if (a.y - g > .5f) { mk.Box(concreteDark, new Vector3(a.x, (a.y + g) / 2f - .2f, a.z), new Vector3(2.4f, a.y - g, 1.2f), rot); BoxCol(ctx.Root, new Vector3(a.x, (a.y + g) / 2f - .2f, a.z), new Vector3(2.4f, a.y - g, 1.2f), rot); }
                if (k % 3 == 0) { var pole = a + sd * 2.2f; mk.Box(metal, pole + up * 2.8f, new Vector3(.15f, 5.6f, .15f), Quaternion.identity); mk.Box(metal, pole + up * 5.4f - sd * 1.4f, new Vector3(.08f, .08f, 3f), Quaternion.LookRotation(sd)); ArtistBits.PostCollider(ctx.Root, pole, 5.4f, .12f); }
            }
            life.track = best.ToArray();
            // the crossing nearest the city
            var nearest = best[0]; foreach (var p in best) if (Vector3.Distance(p, center) < Vector3.Distance(nearest, center)) nearest = p;
            var toC = new Vector3(center.x - nearest.x, 0, center.z - nearest.z).normalized; var cp = ctx.OnGround(nearest.x + toC.x * 4f, nearest.z + toC.z * 4f);
            mk.Box(dark, cp + up * 1.6f, new Vector3(.14f, 3.2f, .14f), Quaternion.identity); ArtistBits.PostCollider(ctx.Root, cp, 3f, .12f);
            var crossRot = Quaternion.LookRotation(toC);
            foreach (var e in new[] { 45f, -45f }) mk.Box(WorldMaterials.Solid(new Color(.95f, .85f, .2f)), cp + up * 3.2f, new Vector3(1.4f, .22f, .05f), crossRot * Quaternion.Euler(0, 0, e));
            var blink = new Material[2]; for (var k = 0; k < 2; k++) blink[k] = new Material(GlowM(new Color(1f, .15f, .1f), 2.6f)) { name = "Crossing light" };
            foreach (var e in new[] { -1f, 1f }) mk.Prism(blink[e > 0 ? 1 : 0], cp + up * 2.5f + Vector3.Cross(up, toC) * e * .32f + toC * .1f, toC, .16f, .06f, 12);
            life.crossing = blink; life.crossingAt = cp + up * 2.5f;
            halos.Add(cp + up * 2.5f, 1.2f, new Color(1f, .2f, .15f), .5f);
            var mark = new GameObject("Shot: the coast train").transform; mark.SetParent(ctx.Root, false);
            var cam = cp + toC * 12f + up * 1.7f; mark.SetPositionAndRotation(cam, Quaternion.LookRotation((nearest + up * 2f - cam).normalized));
        }

        // ---------------------------------------------------------------- the wilds
        /// Beyond the city: cherry trees, little houses with tiled roofs, vending machines by the lanes, stone lanterns,
        /// bamboo, bicycles, the odd shrine gate on its own.
        void Wilds(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            int cherries = 0, houses = 0, vend = 0, toro = 0, bamboo = 0, gates = 0;
            const float cell = 19f;
            for (var x = ctx.MinX; x < ctx.MaxX; x += cell)
            for (var z = ctx.MinZ; z < ctx.MaxZ; z += cell)
            {
                float px = x + ctx.Range(0f, cell), pz = z + ctx.Range(0f, cell);
                if (InCity(px, pz) || !Open(ctx, px, pz, 3f)) continue;
                var p = ctx.OnGround(px, pz); var roll = ctx.Range(0f, 1f);
                if (roll < .2f) { Cherry(ctx, mk, p, ctx.Range(5f, 8f)); cherries++; }
                else if (roll < .3f) { if (House(ctx, mk, solid, p, houses)) houses++; }
                else if (roll < .34f) { Vending(ctx, mk, p, new Vector3(center.x - px, 0, center.z - pz).normalized, vend); vend++; }
                else if (roll < .38f) { StoneLantern(ctx, mk, p); toro++; }
                else if (roll < .46f) { for (var k = 0; k < 9; k++) { var q = ctx.OnGround(px + ctx.Range(-2f, 2f), pz + ctx.Range(-2f, 2f)); var h = ctx.Range(4f, 8f); mk.Prism(WorldMaterials.Solid(new Color(.45f, .6f, .3f)), q - Vector3.up * .2f, Vector3.up, .07f, h, 6); mk.Blob(WorldMaterials.Solid(new Color(.35f, .55f, .28f)), q + Vector3.up * h, new Vector3(.6f, .4f, .6f), ctx.Rng, .3f); } BoxCol(ctx.Root, p + Vector3.up * 2f, new Vector3(4f, 4f, 4f), Quaternion.identity); bamboo++; }
                else if (roll < .48f) { Torii(ctx, mk, p, Quaternion.Euler(0, ctx.Range(0, 360f), 0) * Vector3.forward, .8f); gates++; }
            }
            Debug.Log($"The J-Rock Isles: {cherries} cherry trees, {houses} houses, {vend} vending machines, {toro} stone lanterns, {bamboo} bamboo stands, {gates} lone gates");
        }

        bool House(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 g, int i)
        {
            var up = Vector3.up; var face = new Vector3(center.x - g.x, 0, center.z - g.z).normalized; var rot = Quaternion.LookRotation(face); var sd = Vector3.Cross(up, face);
            float lo = g.y, hi = g.y; foreach (var (x, z) in new[] { (-1f, -1f), (1f, -1f), (1f, 1f), (-1f, 1f) }) { var q = g + sd * x * 3.6f + face * z * 3f; var h = ctx.Ground(q.x, q.z); lo = Mathf.Min(lo, h); hi = Mathf.Max(hi, h); }
            if (hi - lo > 3f) return false;
            var b = new Vector3(g.x, hi + .2f, g.z);
            mk.Box(concreteDark, new Vector3(b.x, (lo + b.y) / 2f, b.z), new Vector3(7.4f, b.y - lo + .4f, 6.2f), rot);
            var wall = WorldMaterials.Solid(new[] { new Color(.88f, .85f, .78f), new Color(.75f, .7f, .62f), new Color(.82f, .8f, .74f) }[i % 3]);
            mk.Box(wall, b + up * 1.6f, new Vector3(7f, 3.2f, 5.8f), rot); solid.Box(wall, b + up * 1.4f, new Vector3(7f, 3.6f, 5.8f), rot);
            mk.GableRoof(tile, b + up * 3.2f, new Vector3(7f, 0, 8.4f), 1.8f, Quaternion.LookRotation(sd));
            mk.Box(i % 3 == 0 ? glassLit : WorldMaterials.Solid(new Color(.2f, .22f, .26f)), b + up * 1.7f + face * 2.92f + sd * 1.5f, new Vector3(1.6f, 1f, .04f), rot);
            mk.Box(woodDark, b + up * 1f + face * 2.92f - sd * 1.6f, new Vector3(1f, 2f, .04f), rot);
            PaperLantern(mk, b + up * 2.4f + face * 3.2f - sd * .8f, ctx.Rng);
            noGrass.Add(new Vector3(b.x, 5f, b.z)); buildingsAt.Add(new Vector3(b.x, 4f, b.z));
            return true;
        }

        void Grass(LandContext ctx)
        {
            var mat = WindMesh.Material(.3f, 1f, new Color(.35f, .48f, .3f));
            var meshes = new Dictionary<(int, int), WindMesh>(); var rng = new System.Random(1997);
            const float st = 2.4f;
            for (var x = ctx.MinX; x < ctx.MaxX; x += st)
            for (var z = ctx.MinZ; z < ctx.MaxZ; z += st)
            {
                var px = x + ((float)rng.NextDouble() - .5f) * st; var pz = z + ((float)rng.NextDouble() - .5f) * st;
                if (InCity(px, pz) || !ctx.IsLand(px, pz, 1f) || NearPath(px, pz, 2.4f) || InNoGrass(px, pz, 0f) || InFootprint(ctx, px, pz, -2f)) continue;
                if (Mathf.PerlinNoise(px * .03f + 2f, pz * .03f + 7f) < .4f) continue;
                var key = (Mathf.FloorToInt(px / 70f), Mathf.FloorToInt(pz / 70f));
                if (!meshes.TryGetValue(key, out var wm)) meshes[key] = wm = new WindMesh();
                var col = Color.Lerp(new Color(.3f, .45f, .28f), new Color(.5f, .58f, .34f), (float)rng.NextDouble());
                wm.Tuft(new Vector3(px, ctx.Ground(px, pz) - .04f, pz), .3f + (float)rng.NextDouble() * .35f, col, Color.Lerp(col, Blossom, .2f), rng, 4, .05f);
            }
            foreach (var kv in meshes) kv.Value.Build("Isles grass", ctx.Root, mat);
        }
    }
}
