using System.Collections.Generic;
using UnityEngine;

namespace EverythingWorld.World
{
    /// The Post-Punk Docklands (WORLD_GUIDE.md): night-time melancholy. A grey industrial harbour at night under
    /// sodium-orange street light, black water and wet concrete, no rain falling, only its reflections. Up on the
    /// plateau where you arrive stand brutalist tower blocks, all dark but for a single lit window high in the tallest;
    /// the ground falls away west to the quays at sea level: a concrete quay wall with bollards, three container cranes
    /// with red lights, a cargo ship moored and lit, stacks of containers, long warehouses with roller doors and flyers,
    /// and a breakwater running out to a lighthouse whose beam sweeps the harbour. Streets of asphalt and puddles under
    /// tall sodium lamps join everything. Reads by darkness against orange, with black and cold blue in between.
    /// Files: DocksKit (ground, the quay, the harbour, the blocks, streets, the city), DocksArtists, DocksSongs, DocksLife.
    public partial class DocksKit : LandKit
    {
        public override string Land => "The Post-Punk Docklands";
        public override Vector3 ArtistFront(IslandWorldData data, IslandWorldData.Artist a) => facing.TryGetValue(a.name, out var f) ? f : base.ArtistFront(data, a);

        public static readonly Color Sodium = new Color(1f, .58f, .18f), Window = new Color(1f, .82f, .5f), Ink = new Color(.95f, .92f, .85f), Cold = new Color(.55f, .7f, 1f);
        const float RoadW = 4f, QuayY = 2.4f;

        Material concrete, darkConcrete, asphalt, brick, darkBrick, steel, rust, dark, black, yellow, glassDark, sodium, windowsOff;
        GlowHalos halos, lampHalos; Vector3 center; DocksLife life; Material blinkM;
        readonly Dictionary<(int, int), List<Vector3>> pathCells = new Dictionary<(int, int), List<Vector3>>();
        readonly List<(Vector3 p, int w)> walkPoints = new List<(Vector3, int)>();
        readonly Dictionary<string, Vector3> facing = new Dictionary<string, Vector3>();
        readonly List<Vector3> noGrass = new List<Vector3>();
        readonly List<Vector2> quayEdge = new List<Vector2>();     // land cells on the harbour coast
        readonly List<Vector2> quayNormal = new List<Vector2>();   // toward the sea
        Vector2 heart2, harbour2, seaDir2; bool hasHarbour;
        // the city centre: flat ground round the arrival laid out on a street grid square to the harbour
        const float CityR = 125f, GridStep = 48f, GridRoadW = 8f;
        Vector3 GU => hasHarbour ? new Vector3(seaDir2.x, 0, seaDir2.y) : Vector3.right;
        Vector3 GV => Vector3.Cross(Vector3.up, GU);
        bool InCity(float x, float z, float pad = 0f) => Vector2.Distance(new Vector2(x, z), heart2) < CityR + pad;
        float step = 8f;

        void MakeMaterials()
        {
            concrete = WorldMaterials.Solid(new Color(.42f, .41f, .4f), .35f); darkConcrete = WorldMaterials.Solid(new Color(.26f, .26f, .26f), .4f);
            asphalt = WorldMaterials.Solid(new Color(.1f, .1f, .11f), .75f); brick = WorldMaterials.Solid(new Color(.38f, .22f, .17f), .25f); darkBrick = WorldMaterials.Solid(new Color(.24f, .16f, .14f), .25f);
            steel = WorldMaterials.Solid(new Color(.4f, .42f, .45f), .7f); rust = WorldMaterials.Solid(new Color(.45f, .24f, .14f), .3f);
            dark = WorldMaterials.Solid(new Color(.06f, .06f, .07f)); black = WorldMaterials.Solid(new Color(.03f, .03f, .035f), .6f);
            yellow = WorldMaterials.Solid(new Color(.85f, .65f, .12f), .4f); glassDark = WorldMaterials.Solid(new Color(.08f, .1f, .13f), .95f);
            sodium = new Material(WorldMaterials.Glow(Sodium, 2.2f)) { name = "Sodium lamps" };
            windowsOff = new Material(WorldMaterials.Glow(new Color(.06f, .07f, .09f), 0f)) { name = "Tower windows" };
        }

        Material GlowM(Color c, float s = 2f) => WorldMaterials.Glow(c, s * .65f);
        static bool HasStation(IslandWorldData.Artist a) => a.stationId != 0;
        static float Foot(IslandWorldData.Artist a)
        {
            switch (a.name)
            {
                case "The Smiths": return 19f;
                case "The Cure": return 16f;
                case "Sonic Youth": case "Maruja": return 15f;
                case "Joy Division": case "Black Country, New Road": return 11f;
                case "Madness": case "Melt-Banana": return 9f;
                default: return 8f;
            }
        }

        public override bool Ground(float height, float slope, float patch, out Color color)
        {
            if (height < QuayY + .6f) color = Color.Lerp(new Color(.33f, .33f, .34f), new Color(.4f, .39f, .38f), patch);   // the quay's concrete
            else if (slope > 20f) color = Color.Lerp(new Color(.27f, .26f, .25f), new Color(.36f, .34f, .32f), patch);
            else color = Color.Lerp(new Color(.25f, .27f, .24f), new Color(.37f, .36f, .34f), Mathf.SmoothStep(0f, 1f, patch));
            color.a = 1f;
            return true;
        }

        static float Ease(float a, float b, float x) { var t = Mathf.Clamp01((x - a) / (b - a)); return t * t * (3f - 2f * t); }

        float HarbourDist(float x, float z)
        {
            var best = float.MaxValue;
            foreach (var q in quayEdge) { float dx = q.x - x, dz = q.y - z, d = dx * dx + dz * dz; if (d < best) best = d; }
            return Mathf.Sqrt(best);
        }

        bool OnQuay(float x, float z, float within = 70f) => hasHarbour && HarbourDist(x, z) < within;

        // ---------------------------------------------------------------- shaping the ground
        void ShapeDocks(IslandWorldData data, int index)
        {
            var isl = data.layout.islands[index]; heart2 = new Vector2(isl.labelX, isl.labelZ); step = data.Step;
            quayEdge.Clear(); quayNormal.Clear();
            bool Sea(float x, float z) => data.IslandAt(x, z) == null || data.HeightAt(x, z) < .2f;
            // the harbour coast: land cells with the sea beside them, within reach of the arrival
            for (var r = 1; r < data.Nz - 1; r++)
            {
                var z = data.layout.depth - r * data.Step; if (Mathf.Abs(z - heart2.y) > 430f) continue;
                for (var col = 1; col < data.Nx - 1; col++)
                {
                    var x = col * data.Step; if (Mathf.Abs(x - heart2.x) > 430f) continue;
                    var i = r * data.Nx + col; if (data.islandIds[i] != index) continue;
                    if (Vector2.Distance(new Vector2(x, z), heart2) > 420f) continue;
                    var n = Vector2.zero;
                    for (var a = -1; a <= 1; a++) for (var b = -1; b <= 1; b++) if (Sea(x + a * data.Step, z + b * data.Step)) n += new Vector2(a, b);
                    if (n.sqrMagnitude < .5f) continue;
                    quayEdge.Add(new Vector2(x, z)); quayNormal.Add(n.normalized);
                }
            }
            hasHarbour = quayEdge.Count > 12;
            if (hasHarbour)
            {
                // the harbour's middle: the coast point nearest the arrival
                var best = float.MaxValue; var bi = 0;
                for (var k = 0; k < quayEdge.Count; k++) { var d = Vector2.Distance(quayEdge[k], heart2); if (d < best) { best = d; bi = k; } }
                var sum = Vector2.zero; foreach (var (q, k) in Indexed(quayEdge)) if (Vector2.Distance(q, quayEdge[bi]) < 50f) sum += quayNormal[k];
                harbour2 = quayEdge[bi]; seaDir2 = sum.normalized;
                Level(data, index, heart2, CityR, CityR + 70f);   // the flat city centre, sloping away to the docks and the edges
                // bring the ground down to the quay along the harbour coast, rising back to the plateau inland
                for (var r = 0; r < data.Nz; r++)
                {
                    var z = data.layout.depth - r * data.Step; if (Mathf.Abs(z - heart2.y) > 560f) continue;
                    for (var col = 0; col < data.Nx; col++)
                    {
                        var x = col * data.Step; if (Mathf.Abs(x - heart2.x) > 560f) continue;
                        var i = r * data.Nx + col; if (data.islandIds[i] != index) continue;
                        var d = HarbourDist(x, z); if (d > 130f) continue;
                        var w = 1f - Ease(60f, 130f, d);
                        data.heights[i] = Mathf.Lerp(data.heights[i], QuayY, w);
                    }
                }
            }
            var avoid = new List<Vector3>();
            if (hasHarbour) { foreach (var (q, k) in Indexed(quayEdge)) if (k % 2 == 0) avoid.Add(new Vector3(q.x, 16f, q.y)); }
            ArtistPlacement.Spread(data, index, 52f, heart2, 44f, CityR - 22f, avoid, null, Land, 360f);   // round the gridded centre, not on it
            foreach (var a in data.layout.artists)
            {
                if (a.sceneId != isl.sceneId) continue;
                a.r = Mathf.Max(a.r, Foot(a) + 6f);
                Level(data, index, new Vector2(a.x, a.z), Foot(a) + 1f, Foot(a) + 12f);
            }
            Level(data, index, heart2, 22f, 40f);
        }

        static IEnumerable<(Vector2, int)> Indexed(List<Vector2> l) { for (var k = 0; k < l.Count; k++) yield return (l[k], k); }

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
                    else if (d < blend) data.heights[i] = Mathf.Lerp(level, data.heights[i], Ease(0f, 1f, (d - flat) / (blend - flat)));
                }
            }
        }

        // ---------------------------------------------------------------- dressing
        public override void Dress(LandContext ctx)
        {
            MakeMaterials();
            halos = new GlowHalos(); lampHalos = new GlowHalos();
            pathCells.Clear(); walkPoints.Clear(); facing.Clear(); noGrass.Clear();
            center = ctx.Plaza;
            life = new GameObject("Docklands life").AddComponent<DocksLife>();
            life.transform.SetParent(ctx.Root, false);
            blinkM = new Material(WorldMaterials.Glow(new Color(1f, .1f, .08f), 2f)) { name = "Red warning lights" };
            life.center = center; life.sodium = sodium; life.windows = windowsOff; life.blink = blinkM;
            var mk = new MeshKit(); var solid = new MeshKit();
            Remember(ctx.OnGround(center.x, center.z), 0);
            Arrival(ctx, mk, solid);
            Towers(ctx, mk, solid);
            if (hasHarbour) Harbour(ctx, mk, solid);
            if (hasHarbour) Grid(ctx, mk, solid);
            Streets(ctx, mk, solid);
            DockPlaces(ctx, mk, solid);
            City(ctx, mk, solid);
            mk.Build("Post-Punk Docklands", ctx.Root, false);
            solid.BuildCollider("Post-Punk Docklands walls", ctx.Root);
        }

        public override void Finish(LandContext ctx) { Weeds(ctx); halos.Build("Docklands glow", ctx.Root); life.lampHalos = lampHalos.Build("Docklands lamp glow", ctx.Root); }

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
            ctx.IsLand(x, z, 6f) && !InFootprint(ctx, x, z, pad) && !NearPath(x, z, pad + 2.5f) && !InNoGrass(x, z, pad)
            && Vector2.Distance(new Vector2(x, z), new Vector2(center.x, center.z)) > 30f + pad;

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

        /// A plain box collider on its own object (for big simple shapes like container stacks).
        static void BoxCol(Transform parent, Vector3 center, Vector3 size, Quaternion rot)
        {
            var go = new GameObject("Box collider"); go.transform.SetParent(parent, false); go.transform.SetPositionAndRotation(center, rot);
            go.AddComponent<BoxCollider>().size = size; go.isStatic = true;
        }

        void Puddle(MeshKit mk, Vector3 at, float r)
        {
            mk.Prism(WorldMaterials.Solid(new Color(.16f, .19f, .26f), .98f), at - Vector3.up * .05f, Vector3.up, r, .04f, 12);
        }

        /// A tall concrete street lamp with a sodium head, its orange pool of light on the wet ground.
        void SodiumLamp(LandContext ctx, MeshKit mk, Vector3 g, Vector3 arm, bool light = false)
        {
            var up = Vector3.up; arm = Vector3.ProjectOnPlane(arm, up).normalized;
            mk.Prism(darkConcrete, g - up * .3f, up, .17f, 7.6f, 8, .7f);
            mk.Box(darkConcrete, g + up * 7.2f + arm * .7f, new Vector3(.12f, .12f, 1.5f), Quaternion.LookRotation(arm));
            var head = g + up * 7.05f + arm * 1.45f;
            mk.Box(dark, head + up * .12f, new Vector3(.45f, .18f, .9f), Quaternion.LookRotation(arm));
            mk.Box(sodium, head, new Vector3(.34f, .06f, .7f), Quaternion.LookRotation(arm));
            lampHalos.Add(head - up * .25f, 3f, Sodium, .75f);
            // the pool of light: a halo centred a metre under the ground, so only a low dome of it shows round the lamp's foot
            // and your eyes are never inside it (standing by a lamp, a halo at ground level filled the whole view with orange)
            var pool = new Vector3(head.x, ctx.Ground(head.x, head.z) + .03f, head.z);
            lampHalos.Add(pool - up * 1f, 2.6f, Sodium, .45f);
            life.lamps.Add(head);
            if (light) ArtistBits.PointLight(ctx.Root, head - up * 1f, Sodium, 14f, 1.5f);
            ArtistBits.PostCollider(ctx.Root, g, 7f, .22f);
        }

        /// A wall of torn flyers: overlapping rectangles in faded colours on a concrete or brick face.
        void Flyers(MeshKit mk, Vector3 c, Vector3 face, float w, float h, System.Random rng)
        {
            var side = Vector3.Cross(Vector3.up, face).normalized;
            Color[] cols = { new Color(.85f, .82f, .75f), new Color(.75f, .25f, .2f), new Color(.2f, .2f, .22f), new Color(.85f, .7f, .2f), new Color(.35f, .5f, .7f), new Color(.9f, .5f, .6f) };
            var n = Mathf.RoundToInt(w * h * 1.6f);
            for (var k = 0; k < n; k++)
            {
                float x = ((float)rng.NextDouble() - .5f) * (w - .5f), y = ((float)rng.NextDouble() - .5f) * (h - .6f);
                var m = WorldMaterials.Solid(cols[rng.Next(cols.Length)]);
                mk.Box(m, c + side * x + Vector3.up * y + face * (.012f + k * .007f), new Vector3(.42f, .6f, .005f), Quaternion.LookRotation(face) * Quaternion.Euler(0, 0, ((float)rng.NextDouble() - .5f) * 12f));
            }
        }

        // ---------------------------------------------------------------- arrival
        /// Where you arrive: a wet concrete square between the blocks, puddles, sodium lamps, a concrete bus shelter
        /// pasted with flyers, a phone box of steel and glass, a cast name sign on a concrete wall, and the view west
        /// down to the cranes.
        void Arrival(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var c = ctx.OnGround(center.x, center.z);
            var toSea = hasHarbour ? new Vector3(harbour2.x - c.x, 0, harbour2.y - c.z).normalized : Vector3.left; var sd = Vector3.Cross(up, toSea);
            mk.Prism(concrete, c - up * .25f, up, 14f, .33f, 24);
            for (var k = 0; k < 12; k++) for (var j = 0; j < 4; j++) { var a = k * Mathf.PI / 6f; var r = 3.5f + j * 3.2f; mk.Box(darkConcrete, c + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * r + up * .09f, new Vector3(.06f, .02f, 3f), Quaternion.Euler(0, -a * Mathf.Rad2Deg, 0)); }
            for (var k = 0; k < 7; k++) { var a = k * .9f + .3f; var r = ctx.Range(3f, 11f); Puddle(mk, c + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * r + up * .15f, ctx.Range(.7f, 1.8f)); }
            // the name, cast on a low concrete wall facing the square, on the side away from the sea
            var w = c - toSea * 12f; w.y = ctx.Ground(w.x, w.z);
            mk.Box(concrete, w + up * 1.1f, new Vector3(9f, 2.2f, .6f), Quaternion.LookRotation(toSea)); solid.Box(concrete, w + up * 1.1f, new Vector3(9f, 2.2f, .6f), Quaternion.LookRotation(toSea));
            ArtistBits.Label(ctx.Root, "THE POST-PUNK DOCKLANDS", w + up * 1.4f + toSea * .32f, toSea, .32f, Sodium, TextAnchor.MiddleCenter);
            Flyers(mk, w + up * 1f - toSea * .31f, -toSea, 8f, 1.6f, ctx.Rng);
            // the bus shelter
            var bs = c + sd * 12f - toSea * 3f; bs.y = ctx.Ground(bs.x, bs.z); var brot = Quaternion.LookRotation(-sd);
            mk.Box(concrete, bs + up * 1.3f + sd * .8f, new Vector3(4f, 2.6f, .25f), brot); solid.Box(concrete, bs + up * 1.3f + sd * .8f, new Vector3(4f, 2.6f, .25f), brot);
            mk.Box(concrete, bs + up * 2.7f, new Vector3(4.4f, .2f, 2f), brot);
            foreach (var s in new[] { -1.9f, 1.9f }) { mk.Box(concrete, bs + up * 1.3f + toSea * s, new Vector3(.25f, 2.6f, 1.7f), brot); solid.Box(concrete, bs + up * 1.3f + toSea * s, new Vector3(.25f, 2.6f, 1.7f), brot); }
            mk.Box(darkConcrete, bs + up * .5f + sd * .5f, new Vector3(3.4f, .1f, .45f), brot);
            Flyers(mk, bs + up * 1.4f + sd * .66f, -sd, 3.4f, 1.6f, ctx.Rng);
            mk.Box(GlowM(new Color(.85f, .9f, 1f), 1.4f), bs + up * 2.58f, new Vector3(3.6f, .04f, .2f), brot); halos.Add(bs + up * 2.3f, 2.2f, new Color(.85f, .9f, 1f), .3f);
            // the phone box: steel and glass, its light on
            var pb = c - sd * 12f - toSea * 2f; pb.y = ctx.Ground(pb.x, pb.z); var prot = Quaternion.LookRotation(sd);
            mk.Box(steel, pb + up * 1.25f, new Vector3(1.1f, 2.5f, 1.1f), prot); mk.Box(GlowM(new Color(.8f, .9f, 1f), 1.5f), pb + up * 1.35f + sd * .56f, new Vector3(.85f, 1.8f, .02f), prot);
            mk.Box(dark, pb + up * 2.4f + sd * .57f, new Vector3(.9f, .2f, .02f), prot); ArtistBits.Label(ctx.Root, "TELEPHONE", pb + up * 2.4f + sd * .59f, sd, .09f, Ink, TextAnchor.MiddleCenter);
            solid.Box(steel, pb + up * 1.25f, new Vector3(1.1f, 2.5f, 1.1f), prot); halos.Add(pb + up * 1.4f + sd * .8f, 1.4f, new Color(.8f, .9f, 1f), .5f);
            // sodium lamps round the square
            for (var k = 0; k < 5; k++) { var a = k * Mathf.PI * 2f / 5f + .2f; var lp = ctx.OnGround(c.x + Mathf.Cos(a) * 15.5f, c.z + Mathf.Sin(a) * 15.5f); SodiumLamp(ctx, mk, lp, new Vector3(c.x - lp.x, 0, c.z - lp.z), k % 2 == 0); }
            noGrass.Add(new Vector3(c.x, 16f, c.z));
            var mark = new GameObject("Shot: the square").transform; mark.SetParent(ctx.Root, false);
            var cam = c - toSea * 9f + sd * 5f + up * 1.7f; var look = c + toSea * 40f + up * 3f - cam; mark.SetPositionAndRotation(cam, Quaternion.LookRotation(look.normalized));
        }

        // ---------------------------------------------------------------- the tower blocks
        /// Three brutalist blocks round the square: the tallest (twelve floors of ribbed concrete, balconies, a lift
        /// tower) dark but for one window lit high up; two lower slabs with a few lit windows.
        void Towers(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var c = ctx.OnGround(center.x, center.z);
            var toSea = hasHarbour ? new Vector3(harbour2.x - c.x, 0, harbour2.y - c.z).normalized : Vector3.left; var sd = Vector3.Cross(up, toSea);
            if (hasHarbour) { toSea = GU; sd = GV; }
            var spots = new[] { (c - toSea * GridStep + sd * GridStep, 12, 16f, 14f, true), (c - toSea * GridStep - sd * GridStep, 7, 28f, 11f, false), (c + sd * GridStep, 6, 26f, 11f, false) };
            foreach (var (p0, floors, W, D, tall) in spots)
            {
                var p = p0; p.y = ctx.Ground(p.x, p.z) - .5f; if (!ctx.IsLand(p.x, p.z, 10f) || InFootprint(ctx, p.x, p.z, W * .7f)) continue;
                var face = new Vector3(c.x - p.x, 0, c.z - p.z).normalized; if (hasHarbour) face = Mathf.Abs(Vector3.Dot(face, GU)) > Mathf.Abs(Vector3.Dot(face, GV)) ? GU * Mathf.Sign(Vector3.Dot(face, GU)) : GV * Mathf.Sign(Vector3.Dot(face, GV));
                var rot = Quaternion.LookRotation(face); var s2 = Vector3.Cross(up, face);
                const float FH = 3f; var H = floors * FH + 1f;
                mk.Box(concrete, p + up * H / 2f, new Vector3(W, H, D), rot); solid.Box(concrete, p + up * H / 2f, new Vector3(W, H, D), rot);
                mk.Box(darkConcrete, p + up * (H + .4f), new Vector3(W + .4f, .8f, D + .4f), rot);
                // windows on the front and the back, a balcony slab under each floor, ribs between bays
                var bays = Mathf.FloorToInt(W / 2.4f); var lit = new System.Random(floors * 31 + bays);
                for (var f = 0; f < floors; f++)
                {
                    var y = 1.6f + f * FH;
                    foreach (var dirS in new[] { 1f, -1f })
                    {
                        var fc = p + face * dirS * (D / 2f + .02f) + up * y; var frot = Quaternion.LookRotation(face * dirS);
                        mk.Box(darkConcrete, fc + face * dirS * .35f - up * 1.05f, new Vector3(W, .18f, .7f), frot);
                        for (var b = 0; b < bays; b++)
                        {
                            var x = (b - (bays - 1) / 2f) * (W / bays);
                            Material m = windowsOff;
                            if (tall && dirS > 0 && f == floors - 3 && b == bays / 2 + 1) { m = new Material(WorldMaterials.Glow(Window, 2.4f)) { name = "The one lit window" }; life.oneWindow = m; life.oneWindowAt = fc + s2 * x + face * .5f; halos.Add(fc + s2 * x + face * .8f, 2.6f, Window, .8f); }
                            else if (!tall && lit.NextDouble() < .12) m = GlowM(Window, 1.2f);
                            mk.Box(m, fc + s2 * x, new Vector3(W / bays - .6f, 1.4f, .03f), frot);
                        }
                    }
                }
                for (var b = 0; b <= bays; b++) { var x = (b - bays / 2f) * (W / bays); mk.Box(darkConcrete, p + s2 * x + face * (D / 2f + .25f) + up * H / 2f, new Vector3(.3f, H, .5f), rot); }
                if (tall)
                {
                    mk.Box(concrete, p + s2 * (W / 2f + 1.5f) + up * (H + 3f) / 2f, new Vector3(3f, H + 3f, 4f), rot); solid.Box(concrete, p + s2 * (W / 2f + 1.5f) + up * (H + 3f) / 2f, new Vector3(3f, H + 3f, 4f), rot);
                    var red = blinkM; mk.Blob(red, p + s2 * (W / 2f + 1.5f) + up * (H + 3.3f), Vector3.one * .2f, ctx.Rng, 0f); life.blinkers.Add(p + s2 * (W / 2f + 1.5f) + up * (H + 3.3f));
                    life.towerTop = p + up * (H + 1f);
                    var mark = new GameObject("Shot: the tower with one lit window").transform; mark.SetParent(ctx.Root, false);
                    var cam = p + face * 75f - s2 * 8f; cam.y = ctx.Ground(cam.x, cam.z) + 1.7f; var look = life.oneWindowAt - cam; mark.SetPositionAndRotation(cam, Quaternion.LookRotation(look.normalized));
                }
                // the entrance: a dark doorway with a strip light over it
                var door = p + face * (D / 2f + .05f) + up * 1.2f; mk.Box(dark, door, new Vector3(2f, 2.4f, .05f), rot); mk.Box(GlowM(new Color(.8f, .9f, 1f), 2f), door + up * 1.4f + face * .1f, new Vector3(1.6f, .08f, .1f), rot);
                halos.Add(door + up * 1.2f + face * .4f, 1.6f, new Color(.8f, .9f, 1f), .4f);
                noGrass.Add(new Vector3(p.x, Mathf.Max(W, D) * .75f, p.z));
            }
        }

        // ---------------------------------------------------------------- the harbour
        void Harbour(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up;
            // the quay wall: concrete blocks along the whole harbour coast, a yellow edge line, bollards
            var bollard = WorldMaterials.Solid(new Color(.15f, .15f, .16f), .6f);
            for (var k = 0; k < quayEdge.Count; k++)
            {
                var q = quayEdge[k]; var g = new Vector3(q.x, 0, q.y); var top = QuayY + .1f;
                var n = new Vector3(quayNormal[k].x, 0, quayNormal[k].y);
                mk.Box(darkConcrete, new Vector3(q.x, (top - 4f) / 2f, q.y), new Vector3(step, top + 4f, step), Quaternion.identity);
                solid.Box(darkConcrete, new Vector3(q.x, (top - 4f) / 2f, q.y), new Vector3(step, top + 4f, step), Quaternion.identity);
                if (k % 3 == 0) { var b = new Vector3(q.x, top, q.y) + n * (step * .35f); mk.Prism(bollard, b, up, .25f, .6f, 10); mk.Prism(bollard, b + up * .6f, up, .33f, .12f, 10); ArtistBits.PostCollider(ctx.Root, b, .72f, .3f); }
                noGrass.Add(new Vector3(q.x, 7f, q.y));
            }
            var hc = ctx.OnGround(harbour2.x, harbour2.y); var n0 = new Vector3(seaDir2.x, 0, seaDir2.y); var t0 = Vector3.Cross(up, n0);
            // cranes along the quay
            for (var k = -1; k <= 1; k++)
            {
                var at = QuayPoint(harbour2 + new Vector2(t0.x, t0.z) * k * 38f, out var nn);
                if (at == null) continue;
                Crane(ctx, mk, solid, at.Value, nn, k);
            }
            // the ship, moored off the quay
            Ship(ctx, mk, solid, hc + n0 * 22f, t0);
            // the breakwater and the lighthouse, out into the sea past the cranes
            var bwStart = QuayPoint(harbour2 + new Vector2(t0.x, t0.z) * 80f, out var bwn) ?? QuayPoint(harbour2 - new Vector2(t0.x, t0.z) * 80f, out bwn);
            if (bwStart != null) Breakwater(ctx, mk, solid, bwStart.Value, bwn);
            // container stacks and warehouses on the quay behind the cranes
            var rng = ctx.Rng; int stacks = 0, sheds = 0;
            for (var k = 0; k < quayEdge.Count; k += 2)
            {
                var q = quayEdge[k]; var n = new Vector3(quayNormal[k].x, 0, quayNormal[k].y); var inland = new Vector3(q.x, 0, q.y) - n * ctx.Range(22f, 46f);
                var p = ctx.OnGround(inland.x, inland.z); if (p.y > QuayY + 1.5f || !Open(ctx, p.x, p.z, 7f)) continue;
                if (sheds < 4 && rng.NextDouble() < .25) { Warehouse(ctx, mk, solid, p, n); sheds++; }
                else if (stacks < 14 && rng.NextDouble() < .5) { ContainerStack(ctx, mk, solid, p, Vector3.Cross(up, n)); stacks++; }
            }
        }

        /// The quay point nearest a spot, with its seaward direction.
        Vector3? QuayPoint(Vector2 near, out Vector3 normal)
        {
            normal = Vector3.forward; var best = float.MaxValue; var bi = -1;
            for (var k = 0; k < quayEdge.Count; k++) { var d = Vector2.Distance(quayEdge[k], near); if (d < best) { best = d; bi = k; } }
            if (bi < 0 || best > 30f) return null;
            var sum = Vector2.zero; for (var k = 0; k < quayEdge.Count; k++) if (Vector2.Distance(quayEdge[k], quayEdge[bi]) < 24f) sum += quayNormal[k];
            normal = new Vector3(sum.x, 0, sum.y).normalized;
            return new Vector3(quayEdge[bi].x, QuayY + .1f, quayEdge[bi].y);
        }

        /// A container crane: two pairs of legs straddling the quay, a boom reaching out over the water, a cab, red
        /// lights at the top.
        void Crane(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 edge, Vector3 n, int k)
        {
            var up = Vector3.up; var t = Vector3.Cross(up, n); var rot = Quaternion.LookRotation(n);
            var paint = WorldMaterials.Solid(k == 0 ? new Color(.62f, .5f, .2f) : new Color(.35f, .4f, .45f), .4f);
            var b = edge - n * 10f; b.y = QuayY + .1f;
            foreach (var s in new[] { -1f, 1f }) foreach (var f in new[] { -7f, 7f })
            {
                var leg = b + t * s * 5f + n * f; leg.y = f > 0 ? QuayY + .1f : ctx.Ground(leg.x, leg.z);
                mk.Box(paint, leg + up * 13f, new Vector3(.9f, 26f, .9f), rot); ArtistBits.PostCollider(ctx.Root, leg, 26f, .6f);
            }
            foreach (var s in new[] { -1f, 1f }) { mk.Box(paint, b + t * s * 5f + up * 26f, new Vector3(1f, 1.2f, 15f), rot); mk.Box(paint, b + t * s * 5f + up * 13f, new Vector3(.4f, .4f, 14f), rot); }
            foreach (var f in new[] { -7f, 7f }) mk.Box(paint, b + n * f + up * 26f, new Vector3(11f, 1f, 1f), rot);
            mk.Box(paint, b + up * 27.5f + n * 12f, new Vector3(2.4f, 1.6f, 46f), rot);   // the boom, out over the water and back
            for (var j = 0; j < 8; j++) mk.Box(paint, b + up * (29f + (j % 2) * 1.6f) + n * (-8f + j * 5.5f), new Vector3(.25f, 3.2f, .25f), rot * Quaternion.Euler(j % 2 == 0 ? 35f : -35f, 0, 0));
            mk.Box(paint, b + up * 31f - n * 6f, new Vector3(3f, 6f, 3f), rot);
            var cab = b + up * 25.5f + n * 9f; mk.Box(dark, cab, new Vector3(2.6f, 2.4f, 2.6f), rot); mk.Box(GlowM(Window, 1.6f), cab + n * 1.31f, new Vector3(2.2f, 1.4f, .02f), rot); halos.Add(cab + n * 1.8f, 1.8f, Window, .4f);
            var hook = b + up * 27f + n * 26f; mk.Box(dark, hook - up * 6f, new Vector3(.05f, 12f, .05f), Quaternion.identity); mk.Box(yellow, hook - up * 12.3f, new Vector3(1.6f, .5f, 1f), rot);
            foreach (var p in new[] { b + up * 34.2f - n * 6f, b + up * 28.6f + n * 34f }) { mk.Blob(blinkM, p, Vector3.one * .25f, ctx.Rng, 0f); life.blinkers.Add(p); }
            ArtistBits.PointLight(ctx.Root, b + up * 22f + n * 4f, Sodium, 30f, 1.4f);
            mk.Box(sodium, b + up * 25.3f, new Vector3(.6f, .1f, .6f), rot); halos.Add(b + up * 25f, 3f, Sodium, .5f);
            life.craneTops.Add(b + up * 34f);
            if (k == 0)
            {
                var mark = new GameObject("Shot: the cranes and the ship").transform; mark.SetParent(ctx.Root, false);
                var cam = edge - n * 40f + t * 26f; cam.y = ctx.Ground(cam.x, cam.z) + 1.7f; var look = edge + n * 10f + up * 12f - cam; mark.SetPositionAndRotation(cam, Quaternion.LookRotation(look.normalized));
            }
        }

        /// A cargo ship moored along the quay: a black-and-red hull, containers on deck, a white bridge at the stern with
        /// lit windows, a funnel, deck lights, a mooring line to the bollards.
        void Ship(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 mid, Vector3 along)
        {
            var up = Vector3.up; along = Vector3.ProjectOnPlane(along, up).normalized; var rot = Quaternion.LookRotation(along); var sd = Vector3.Cross(up, along);
            if (ctx.IsLand(mid.x, mid.z, 0f)) return;
            mid.y = 0f; const float L = 64f, B = 11f;
            var hull = WorldMaterials.Solid(new Color(.1f, .1f, .11f), .5f); var red = WorldMaterials.Solid(new Color(.45f, .1f, .08f), .4f); var white = WorldMaterials.Solid(new Color(.78f, .78f, .76f), .3f);
            mk.Box(hull, mid + up * 2.5f, new Vector3(B, 5f, L), rot); mk.Box(red, mid - up * .6f, new Vector3(B - .2f, 1.4f, L - .4f), rot);
            mk.Cone(hull, mid + along * (L / 2f) + up * 2.5f, B / 2f, 8f, 4, 45f);   // a blunt bow
            mk.Box(hull, mid + along * (L / 2f + 3f) + up * 3.5f, new Vector3(B * .7f, 3f, 6f), rot);
            solid.Box(hull, mid + up * 3f, new Vector3(B, 8f, L + 10f), rot);
            Color[] cc = { new Color(.55f, .15f, .12f), new Color(.15f, .3f, .45f), new Color(.6f, .45f, .15f), new Color(.25f, .4f, .3f), new Color(.45f, .45f, .47f) };
            var rng = ctx.Rng;
            for (var i = 0; i < 7; i++) for (var j = 0; j < 3; j++) for (var h = 0; h < 1 + (i + j) % 3; h++)
                mk.Box(WorldMaterials.Solid(cc[rng.Next(cc.Length)], .3f), mid + along * (-18f + i * 6.4f) + sd * (j - 1f) * 3.1f + up * (6.3f + h * 2.6f), new Vector3(2.9f, 2.5f, 6.1f), rot);
            var br = mid - along * 26f; mk.Box(white, br + up * 9f, new Vector3(B - 1f, 8f, 7f), rot); mk.Box(white, br + up * 13.6f, new Vector3(B + 2f, 1.2f, 4f), rot);
            var win = GlowM(Window, 1.8f); for (var f = 0; f < 3; f++) mk.Box(win, br + up * (6.5f + f * 2.4f) + along * 3.52f, new Vector3(B - 2f, .9f, .02f), rot);
            mk.Box(GlowM(new Color(.8f, .9f, 1f), 1.8f), br + up * 13.6f + along * 2.02f, new Vector3(B, .6f, .02f), rot);
            mk.Prism(red, br + up * 13f - along * 2.5f, up, 1.2f, 4f, 10); mk.Prism(dark, br + up * 17f - along * 2.5f, up, 1.25f, .6f, 10);
            mk.Box(dark, br + up * 18f, new Vector3(.15f, 5f, .15f), rot); mk.Blob(GlowM(Color.white, 3f), br + up * 20.6f, Vector3.one * .2f, ctx.Rng, 0f); halos.Add(br + up * 20.6f, 1.6f, Color.white, .5f);
            halos.Add(br + up * 9f + along * 4f, 6f, Window, .3f);
            foreach (var e in new[] { -1f, 1f }) { var dl = mid + along * e * 20f + up * 5.5f; mk.Blob(sodium, dl, Vector3.one * .2f, ctx.Rng, 0f); halos.Add(dl, 2f, Sodium, .5f); }
            ArtistBits.PointLight(ctx.Root, mid + up * 12f, Sodium, 30f, 1.2f);
            // lit portholes along the hull
            for (var k = 0; k < 14; k++) foreach (var s in new[] { -1f, 1f }) mk.Prism(GlowM(Window, 1.4f), mid + along * (-26f + k * 4f) + sd * s * (B / 2f + .01f) + up * 3.6f, sd * s, .22f, .03f, 8);
            life.ship = mid;
        }

        /// A breakwater of concrete blocks running out to sea, ending in a red-and-white lighthouse whose beam turns.
        void Breakwater(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 start, Vector3 n)
        {
            var up = Vector3.up; var rot = Quaternion.LookRotation(n); var t = Vector3.Cross(up, n);
            const float Len = 70f;
            for (var d = 4f; d < Len; d += 4f)
            {
                var p = start + n * d; if (ctx.IsLand(p.x, p.z, 0f)) continue;
                mk.Box(darkConcrete, new Vector3(p.x, (QuayY - 4f) / 2f, p.z), new Vector3(6f, QuayY + 4f, 4f), rot); solid.Box(darkConcrete, new Vector3(p.x, (QuayY - 4f) / 2f, p.z), new Vector3(6f, QuayY + 4f, 4f), rot);
                foreach (var s in new[] { -1f, 1f }) { mk.Box(concrete, new Vector3(p.x, QuayY + .5f, p.z) + t * s * 2.8f, new Vector3(.4f, 1f, 4f), rot); }
                foreach (var s in new[] { -1f, 1f }) solid.Box(concrete, new Vector3(p.x, QuayY + .6f, p.z) + t * s * 2.8f, new Vector3(.4f, 1.2f, 4f), rot);
                if (((int)d) % 16 == 0) { var l = new Vector3(p.x, QuayY + 1f, p.z) + t * 2.8f; mk.Blob(sodium, l + up * .2f, Vector3.one * .15f, ctx.Rng, 0f); halos.Add(l + up * .3f, 1.2f, Sodium, .5f); }
                Remember(new Vector3(p.x, QuayY + .1f, p.z), 500);
            }
            var lh = start + n * (Len + 3f); lh.y = QuayY;
            mk.Prism(darkConcrete, new Vector3(lh.x, -4f, lh.z), up, 5f, QuayY + 4.1f, 16); solid.Prism(darkConcrete, new Vector3(lh.x, -4f, lh.z), up, 5f, QuayY + 4.1f, 16);
            var white = WorldMaterials.Solid(new Color(.85f, .85f, .82f)); var red = WorldMaterials.Solid(new Color(.65f, .12f, .1f));
            for (var k = 0; k < 6; k++) mk.Prism(k % 2 == 0 ? white : red, lh + up * (.1f + k * 3f), up, 2.2f - k * .15f, 3f, 14, (2.2f - (k + 1) * .15f) / (2.2f - k * .15f));
            solid.Prism(white, lh, up, 2.2f, 18f, 12);
            var lamp = lh + up * 19.2f; mk.Prism(dark, lamp - up * 1.1f, up, 1.8f, .3f, 12); mk.Prism(GlowM(new Color(1f, .95f, .8f), 3f), lamp - up * .8f, up, 1.1f, 1.6f, 10); mk.Cone(red, lamp + up * .8f, 1.5f, 1.4f, 12);
            halos.Add(lamp, 5f, new Color(1f, .95f, .8f), .7f); ArtistBits.PointLight(ctx.Root, lamp, new Color(1f, .95f, .8f), 30f, 2f);
            life.lighthouse = lamp;
            var mark = new GameObject("Shot: the lighthouse").transform; mark.SetParent(ctx.Root, false);
            var cam = start - n * 12f + t * 14f; cam.y = ctx.Ground(cam.x, cam.z) + 1.7f; var look = lamp - up * 6f - cam; mark.SetPositionAndRotation(cam, Quaternion.LookRotation(look.normalized));
        }

        void ContainerStack(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 at, Vector3 dir)
        {
            var up = Vector3.up; var rot = Quaternion.LookRotation(dir); var sd = Vector3.Cross(up, dir);
            Color[] cc = { new Color(.5f, .14f, .1f), new Color(.14f, .28f, .42f), new Color(.55f, .42f, .14f), new Color(.22f, .36f, .26f), new Color(.4f, .4f, .42f), new Color(.6f, .3f, .1f) };
            var rows = ctx.Rng.Next(1, 4); var high = ctx.Rng.Next(1, 4);
            for (var r = 0; r < rows; r++) for (var h = 0; h < high - (r % 2); h++)
            {
                var c = at + sd * (r - (rows - 1) / 2f) * 2.6f + up * (1.3f + h * 2.6f); var m = WorldMaterials.Solid(cc[ctx.Rng.Next(cc.Length)], .3f);
                mk.Box(m, c, new Vector3(2.45f, 2.55f, 12f), rot);
                for (var k = 0; k < 10; k++) mk.Box(m, c + sd * 1.24f + dir * (-5.4f + k * 1.2f), new Vector3(.04f, 2.3f, .3f), rot);
                mk.Box(dark, c + dir * 6.01f, new Vector3(2.3f, 2.4f, .02f), rot);
            }
            BoxCol(ctx.Root, at + up * 1.3f * Mathf.Max(1, high), new Vector3(rows * 2.6f, 2.6f * Mathf.Max(1, high), 12.1f), rot);
            noGrass.Add(new Vector3(at.x, 7f, at.z));
        }

        /// A long corrugated warehouse with a roller door half up, a light over it, flyers on the wall.
        void Warehouse(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 at, Vector3 seaN)
        {
            var up = Vector3.up; var face = seaN; var rot = Quaternion.LookRotation(face); var sd = Vector3.Cross(up, face);
            const float W = 24f, D = 12f, H = 7f; var tin = WorldMaterials.Solid(new Color(.3f, .32f, .34f), .5f);
            mk.Box(tin, at + up * H / 2f, new Vector3(W, H, D), rot); solid.Box(tin, at + up * H / 2f, new Vector3(W, H, D), rot);
            mk.GableRoof(darkConcrete, at + up * H, new Vector3(D + .6f, 0, W + .6f), 2f, Quaternion.LookRotation(sd));
            for (var k = 0; k < 24; k++) mk.Box(darkConcrete, at + sd * (-W / 2f + .5f + k) + up * H / 2f + face * (D / 2f + .02f), new Vector3(.08f, H, .05f), rot);
            foreach (var s in new[] { -6f, 6f })
            {
                var dc = at + sd * s + face * (D / 2f + .05f); mk.Box(dark, dc + up * 1.4f, new Vector3(4f, 2.8f, .04f), rot); mk.Box(steel, dc + up * 3.7f, new Vector3(4.2f, 1.8f, .1f), rot);
                for (var k = 0; k < 6; k++) mk.Box(darkConcrete, dc + up * (2.9f + k * .3f) + face * .06f, new Vector3(4.2f, .04f, .02f), rot);
                mk.Box(sodium, dc + up * 5f + face * .3f, new Vector3(.6f, .1f, .3f), rot); lampHalos.Add(dc + up * 4.7f + face * .5f, 2f, Sodium, .6f); life.lamps.Add(dc + up * 5f + face * .3f);
                halos.Add(dc + up * .5f + face * .8f, 2f, Window, .25f); mk.Box(GlowM(Window, .9f), dc + up * .4f + face * .01f, new Vector3(3.9f, .8f, .02f), rot);
            }
            Flyers(mk, at + up * 2f + face * (D / 2f + .06f), face, 3.6f, 2f, ctx.Rng);
            noGrass.Add(new Vector3(at.x, 14f, at.z));
        }

        // ---------------------------------------------------------------- streets
        void Streets(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var lamps = new List<Vector3>(); var walk = 1;
            void Lay(Vector3 join, Vector3 to, float stopShort, string artist)
            {
                var from = new Vector3(join.x, 0, join.z); to.y = 0;
                var full = to - from; if (full.magnitude < 2f) return;
                // inside the city: along the grid, then across it (an L), so streets never cut through the blocks
                var legs = new List<Vector3> { from };
                if (hasHarbour && InCity(from.x, from.z, 20f))
                {
                    var du = Vector3.Dot(full, GU); var dv = Vector3.Dot(full, GV);
                    var corner = Mathf.Abs(du) > Mathf.Abs(dv) ? from + GV * dv : from + GU * du;
                    if ((corner - from).magnitude > 4f && (to - corner).magnitude > 4f) legs.Add(corner);
                }
                legs.Add(to);
                var lastDir = (to - legs[legs.Count - 2]).normalized;
                if (artist != null) facing[artist] = -lastDir;
                var side = Vector3.Cross(Vector3.up, lastDir);
                var pts = new List<Vector3>(); var first = true;
                for (var li = 0; li + 1 < legs.Count; li++)
                {
                    var a = legs[li]; var b = legs[li + 1]; var seg = b - a; var len = seg.magnitude; var dir = seg / len;
                    var end = li + 2 == legs.Count ? len - stopShort : len;
                    var stop = false;
                    for (var d = first ? (walk == 1 ? 15f : RoadW / 2f + .1f) : 1.5f; ; d += 1.5f)
                    {
                        var dd = Mathf.Min(d, end); var q = a + dir * dd;
                        if (!ctx.IsLand(q.x, q.z, 0f)) { stop = true; break; }
                        pts.Add(ctx.OnGround(q.x, q.z)); if (dd >= end) break;
                    }
                    first = false; if (stop) break;
                }
                if (pts.Count < 2) return;
                Strip(mk, pts, asphalt, RoadW, .2f, ctx);
                for (var i = 4; i < pts.Count; i += 7) Puddle(mk, pts[i] + Vector3.Cross(Vector3.up, (pts[Mathf.Min(i + 1, pts.Count - 1)] - pts[i - 1]).normalized) * (i % 2 == 0 ? .9f : -1.1f) + Vector3.up * .27f, ctx.Range(.4f, .9f));
                foreach (var p in pts) Remember(p, walk);
                for (var i = 8; i < pts.Count; i += 14)
                {
                    var sdi = Vector3.Cross(Vector3.up, (pts[Mathf.Min(i + 1, pts.Count - 1)] - pts[i - 1]).normalized);
                    var lp = pts[i] + sdi * ((i / 14) % 2 == 0 ? 1f : -1f) * (RoadW / 2f + .8f);
                    var near = false; foreach (var o in lamps) if ((o - lp).sqrMagnitude < 19f * 19f) near = true;
                    if (near) continue;
                    lamps.Add(lp); SodiumLamp(ctx, mk, ctx.OnGround(lp.x, lp.z), pts[i] - lp, lamps.Count % 2 == 0);
                }
                walk++;
            }
            if (hasHarbour) Lay(ctx.OnGround(center.x, center.z), new Vector3(harbour2.x, 0, harbour2.y) - new Vector3(seaDir2.x, 0, seaDir2.y) * 14f, 0f, null);
            var todo = new List<IslandWorldData.Artist>(ctx.Artists);
            while (todo.Count > 0)
            {
                IslandWorldData.Artist next = null; Vector3 join = default; var best = float.MaxValue;
                foreach (var a in todo)
                    foreach (var (p, w) in walkPoints) { if (w >= 500) continue; var d = Vector2.Distance(new Vector2(p.x, p.z), new Vector2(a.x, a.z)) - Foot(a); if (d < best) { best = d; next = a; join = p; } }
                if (next == null) break;
                todo.Remove(next);
                Lay(join, new Vector3(next.x, 0, next.z), Foot(next) * .8f, next.name);
            }
        }

        // ---------------------------------------------------------------- the gridded centre
        /// The city centre: wide asphalt streets on a square grid, running down to the harbour and across it, kerbs and
        /// sodium lamps at the corners; on each block a tall tower, a pair of slabs or a brick terrace, square to the
        /// streets. Blocks holding the arrival or an artist stay open as squares.
        void Grid(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var c = new Vector3(heart2.x, 0, heart2.y); var n = Mathf.CeilToInt(CityR / GridStep);
            var kerb = WorldMaterials.Solid(new Color(.5f, .5f, .5f), .3f);
            // streets: lines halfway between block centres, the cross streets broken at the junctions so nothing overlaps
            for (var axis = 0; axis < 2; axis++)
            {
                var along = axis == 0 ? GV : GU; var across = axis == 0 ? GU : GV;
                for (var k = -n; k < n; k++)
                {
                    var off = (k + .5f) * GridStep; if (Mathf.Abs(off) > CityR - 10f) continue;
                    var half = Mathf.Sqrt(Mathf.Max(0f, CityR * CityR - off * off));
                    var pts = new List<Vector3>();
                    void Flush() { if (pts.Count > 1) { Strip(mk, pts, asphalt, GridRoadW, axis == 0 ? .21f : .22f, ctx); foreach (var p in pts) Remember(p, 400 + k); } pts.Clear(); }
                    for (var t = -half; t <= half; t += 2f)
                    {
                        var q = c + across * off + along * t;
                        if (!ctx.IsLand(q.x, q.z, 2f) || InFootprint(ctx, q.x, q.z, 2f)) { Flush(); continue; }
                        var nearest = Mathf.Repeat(t - GridStep / 2f, GridStep); var junction = axis == 1 && Mathf.Min(nearest, GridStep - nearest) < GridRoadW / 2f + .5f;
                        if (junction) { Flush(); continue; }
                        pts.Add(ctx.OnGround(q.x, q.z));
                    }
                    Flush();
                }
            }
            // blocks
            int towers = 0, slabs = 0, terraces = 0, squares = 0;
            for (var i = -n; i <= n; i++)
            for (var j = -n; j <= n; j++)
            {
                var bc = c + GU * (i * GridStep) + GV * (j * GridStep);
                if (Vector2.Distance(new Vector2(bc.x, bc.z), heart2) > CityR - 18f || !ctx.IsLand(bc.x, bc.z, 20f)) continue;
                if (i == 0 && j == 0) continue;   // the arrival square
                var taken = false; foreach (var a in ctx.Artists) if (Vector2.Distance(new Vector2(a.x, a.z), new Vector2(bc.x, bc.z)) < Foot(a) + 11f) taken = true;
                if (taken || InNoGrass(bc.x, bc.z, 4f)) { squares++; continue; }
                var g = ctx.OnGround(bc.x, bc.z); var face = (i + j) % 2 == 0 ? GU : -GV; var roll = ctx.Range(0f, 1f);
                // kerbed pavement round the block
                mk.Box(kerb, g + up * .12f, new Vector3(GridStep - GridRoadW - .2f, .24f, GridStep - GridRoadW - .2f), Quaternion.LookRotation(GU));
                if (roll < .4f) { GridBuilding(ctx, mk, solid, g, face, 18f, 16f, ctx.Rng.Next(9, 15), concrete); towers++; }
                else if (roll < .75f) { foreach (var s in new[] { -1f, 1f }) GridBuilding(ctx, mk, solid, g + face * s * 9.5f, face * s, 32f, 9f, ctx.Rng.Next(4, 8), s < 0 ? concrete : darkConcrete); slabs++; }
                else { foreach (var s in new[] { -1f, 1f }) GridTerrace(ctx, mk, solid, g + face * s * 10f, face * s); terraces++; }
                noGrass.Add(new Vector3(bc.x, (GridStep - GridRoadW) * .7f, bc.z));
                // lamps at the block's corners
                foreach (var a in new[] { -1f, 1f }) foreach (var b in new[] { -1f, 1f })
                    if ((i + j + (a > 0 ? 1 : 0)) % 2 == 0) { var lp = bc + GU * a * (GridStep / 2f - GridRoadW / 2f - .8f) + GV * b * (GridStep / 2f - GridRoadW / 2f - .8f); SodiumLamp(ctx, mk, ctx.OnGround(lp.x, lp.z), -(GU * a + GV * b), (i + j) % 2 == 0); }
            }
            Debug.Log($"The Post-Punk Docklands: grid of {n * 2} streets each way, {towers} towers, {slabs} slab pairs, {terraces} terrace pairs, {squares} open squares");
        }

        /// A block of flats square to the grid: ribbed concrete, rows of windows (a few lit, the rest dark glass that
        /// The Sprawl lights), balconies, a roof edge.
        void GridBuilding(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 at, Vector3 face, float W, float D, int floors, Material m)
        {
            var up = Vector3.up; var rot = Quaternion.LookRotation(face); var sd = Vector3.Cross(up, face); const float FH = 3f; var H = floors * FH; at.y -= .4f;
            mk.Box(m, at + up * H / 2f, new Vector3(W, H, D), rot); solid.Box(m, at + up * H / 2f, new Vector3(W, H, D), rot);
            mk.Box(darkConcrete, at + up * (H + .35f), new Vector3(W + .4f, .7f, D + .4f), rot);
            var bays = Mathf.FloorToInt(W / 2.6f);
            for (var f = 0; f < floors; f++) foreach (var s in new[] { 1f, -1f })
            {
                var y = 1.7f + f * FH;
                mk.Box(darkConcrete, at + up * (y - 1.05f) + face * s * (D / 2f + .3f), new Vector3(W, .16f, .6f), rot);
                for (var b = 0; b < bays; b++)
                {
                    var x = (b - (bays - 1) / 2f) * (W / bays);
                    mk.Box(ctx.Chance(.08f) ? GlowM(Window, 1.3f) : windowsOff, at + up * y + face * s * (D / 2f + .02f) + sd * x, new Vector3(W / bays - .7f, 1.4f, .03f), Quaternion.LookRotation(face * s));
                }
            }
            for (var b = 0; b <= bays; b++) { var x = (b - bays / 2f) * (W / bays); mk.Box(darkConcrete, at + sd * x + face * (D / 2f + .2f) + up * H / 2f, new Vector3(.3f, H, .4f), rot); }
            var door = at + face * (D / 2f + .05f) + up * 1.2f; mk.Box(dark, door, new Vector3(2f, 2.4f, .05f), rot); mk.Box(GlowM(new Color(.8f, .9f, 1f), 2f), door + up * 1.4f + face * .1f, new Vector3(1.6f, .08f, .1f), rot);
            halos.Add(door + up * 1.2f + face * .4f, 1.4f, new Color(.8f, .9f, 1f), .35f);
        }

        /// A brick terrace row along one side of a block, square to the grid, a lit window or two.
        void GridTerrace(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 at, Vector3 face)
        {
            var up = Vector3.up; var rot = Quaternion.LookRotation(face); var sd = Vector3.Cross(up, face); const float w = 5f, D = 8f, H = 6f; const int n = 6; var W = n * w; at.y -= .4f;
            mk.Box(brick, at + up * H / 2f, new Vector3(W, H, D), rot); solid.Box(brick, at + up * H / 2f, new Vector3(W, H, D), rot);
            mk.GableRoof(darkConcrete, at + up * H, new Vector3(D + .5f, 0, W + .3f), 2.4f, Quaternion.LookRotation(sd));
            for (var k = 0; k < n; k++)
            {
                var x = (k - (n - 1) / 2f) * w; var f = at + face * (D / 2f + .02f) + sd * x;
                mk.Box(dark, f + up * 1.1f - sd * 1.4f, new Vector3(1f, 2.1f, .03f), rot);
                foreach (var y in new[] { 1.5f, 4.3f }) mk.Box(ctx.Chance(.2f) ? GlowM(Window, 1.3f) : windowsOff, f + up * y + sd * .9f, new Vector3(1.6f, 1.2f, .03f), rot);
                mk.Box(darkBrick, at + sd * x + up * (H + 2.6f), new Vector3(.8f, 1.6f, .8f), rot);
            }
        }

        // ---------------------------------------------------------------- the city
        /// Fills the land: low concrete and brick blocks with a few lit windows, terraced brick rows, chain-link fences,
        /// skips, oil drums burning, parked cars, steam from grates, walls of flyers, concrete benches under a lamp, and on
        /// the quay more containers and pallets.
        void City(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            int blocks = 0, rows = 0, fences = 0, skips = 0, drums = 0, cars = 0, grates = 0, walls = 0, benches = 0, quay = 0;
            const float cell = 17f;
            for (var x = ctx.MinX; x < ctx.MaxX; x += cell)
            for (var z = ctx.MinZ; z < ctx.MaxZ; z += cell)
            {
                float px = x + ctx.Range(0f, cell), pz = z + ctx.Range(0f, cell);
                if (!Open(ctx, px, pz, 4f)) continue;
                var p = ctx.OnGround(px, pz); var low = p.y < QuayY + 1.2f; var roll = ctx.Range(0f, 1f);
                if (low)
                {
                    if (roll < .35f && Open(ctx, px, pz, 8f)) { ContainerStack(ctx, mk, solid, p, Quaternion.Euler(0, ctx.Range(0, 180f), 0) * Vector3.forward); quay++; }
                    else if (roll < .39f) { Drum(ctx, mk, p, true); drums++; }
                    else if (roll < .55f) { Pallets(ctx, mk, solid, p); quay++; }
                    continue;
                }
                var city = InCity(px, pz, 10f);
                if (city && roll < .27f) continue;
                if (roll < .12f && Open(ctx, px, pz, 12f)) { Block(ctx, mk, solid, p); blocks++; }
                else if (roll < .19f && Open(ctx, px, pz, 10f)) { Terrace(ctx, mk, solid, p); rows++; }
                else if (roll < .23f) { Fence(ctx, mk, solid, p); fences++; }
                else if (roll < .27f) { Skip(ctx, mk, solid, p); skips++; }
                else if (roll < .31f) { Drum(ctx, mk, p, true); drums++; }
                else if (roll < .36f) { Car(ctx, mk, solid, p); cars++; }
                else if (roll < .40f) { Grate(ctx, mk, p); grates++; }
                else if (roll < .46f && Open(ctx, px, pz, 5f) && NearPath(px, pz, 14f)) { FlyerWall(ctx, mk, solid, p); walls++; }
                else if (roll < .58f && benches < 16 && NearPath(px, pz, 12f)) { Bench(ctx, mk, solid, p); benches++; }
            }
            Debug.Log($"The Post-Punk Docklands: harbour {(hasHarbour ? quayEdge.Count + " quay cells" : "none")}, {blocks} blocks, {rows} terraces, {fences} fences, {skips} skips, {drums} drums, {cars} cars, {grates} grates, {walls} flyer walls, {benches} benches, {quay} on the quay");
        }

        void Block(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 at)
        {
            var up = Vector3.up; var face = Quaternion.Euler(0, ctx.Range(0, 4) * 90f + ctx.Range(-10f, 10f), 0) * Vector3.forward; var rot = Quaternion.LookRotation(face); var sd = Vector3.Cross(up, face);
            var floors = ctx.Rng.Next(3, 7); var W = ctx.Range(12f, 18f); const float D = 9f, FH = 3f; var H = floors * FH; at.y -= .6f;
            var m = ctx.Chance(.6f) ? concrete : darkConcrete;
            mk.Box(m, at + up * H / 2f, new Vector3(W, H, D), rot); solid.Box(m, at + up * H / 2f, new Vector3(W, H, D), rot);
            mk.Box(darkConcrete, at + up * (H + .3f), new Vector3(W + .3f, .6f, D + .3f), rot);
            var bays = Mathf.FloorToInt(W / 2.6f);
            for (var f = 0; f < floors; f++) foreach (var s in new[] { 1f, -1f }) for (var b = 0; b < bays; b++)
            {
                var x = (b - (bays - 1) / 2f) * (W / bays); var lit = ctx.Chance(.1f);
                mk.Box(lit ? GlowM(Window, 1.2f) : windowsOff, at + up * (1.7f + f * FH) + face * s * (D / 2f + .02f) + sd * x, new Vector3(W / bays - .7f, 1.3f, .03f), Quaternion.LookRotation(face * s));
            }
            noGrass.Add(new Vector3(at.x, Mathf.Max(W, D) * .7f, at.z));
        }

        /// A short row of brick terraced houses: a chimney each, a lit window or two, a front door.
        void Terrace(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 at)
        {
            var up = Vector3.up; var face = new Vector3(center.x - at.x, 0, center.z - at.z).normalized; var rot = Quaternion.LookRotation(face); var sd = Vector3.Cross(up, face);
            var n = ctx.Rng.Next(3, 6); const float w = 5f, D = 8f, H = 6f; var W = n * w; at.y -= .4f;
            mk.Box(brick, at + up * H / 2f, new Vector3(W, H, D), rot); solid.Box(brick, at + up * H / 2f, new Vector3(W, H, D), rot);
            mk.GableRoof(darkConcrete, at + up * H, new Vector3(D + .5f, 0, W + .3f), 2.4f, Quaternion.LookRotation(sd));
            for (var k = 0; k < n; k++)
            {
                var x = (k - (n - 1) / 2f) * w; var f = at + face * (D / 2f + .02f) + sd * x;
                mk.Box(dark, f + up * 1.1f - sd * 1.4f, new Vector3(1f, 2.1f, .03f), rot);
                foreach (var y in new[] { 1.5f, 4.3f }) mk.Box(ctx.Chance(.2f) ? GlowM(Window, 1.3f) : windowsOff, f + up * y + sd * .9f, new Vector3(1.6f, 1.2f, .03f), rot);
                mk.Box(darkBrick, at + sd * x + up * (H + 2.6f), new Vector3(.8f, 1.6f, .8f), rot);
            }
            noGrass.Add(new Vector3(at.x, W * .6f, at.z));
        }

        void Fence(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 at)
        {
            var up = Vector3.up; var d = Quaternion.Euler(0, ctx.Range(0, 180f), 0) * Vector3.forward; var len = ctx.Range(8f, 16f); var rot = Quaternion.LookRotation(d);
            var mesh = WorldMaterials.Solid(new Color(.45f, .47f, .5f), .6f);
            for (var t = 0f; t <= len; t += 2.5f) { var p = at + d * (t - len / 2f); p.y = ctx.Ground(p.x, p.z); mk.Prism(steel, p - up * .2f, up, .05f, 2.6f, 6); }
            for (var k = 0; k < 4; k++) mk.Box(mesh, at + up * (.3f + k * .6f), new Vector3(.03f, .03f, len), rot);
            for (var k = 0; k < 3; k++) mk.Box(steel, at + up * (2.45f + k * .12f) + Vector3.Cross(up, d) * (k * .08f), new Vector3(.02f, .02f, len), rot);   // barbed wire along the top
            solid.Box(mesh, at + up * 1.2f, new Vector3(.2f, 2.4f, len), rot);
        }

        void Skip(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 at)
        {
            var up = Vector3.up; var rot = Quaternion.Euler(0, ctx.Range(0, 180f), 0); var m = WorldMaterials.Solid(ctx.Chance(.5f) ? new Color(.65f, .5f, .1f) : new Color(.2f, .32f, .25f), .3f);
            mk.Box(m, at + up * .7f, new Vector3(2f, 1.4f, 3.6f), rot); mk.Blob(dark, at + up * 1.4f, new Vector3(.9f, .3f, 1.6f), ctx.Rng, .5f);
            solid.Box(m, at + up * .75f, new Vector3(2.1f, 1.6f, 3.7f), rot);
        }

        /// An oil drum, burning (its fire flickers in DocksLife).
        void Drum(LandContext ctx, MeshKit mk, Vector3 at, bool fire)
        {
            var up = Vector3.up; mk.Prism(rust, at, up, .45f, 1.1f, 12); mk.Prism(dark, at + up * 1.08f, up, .42f, .04f, 12);
            if (fire) { mk.Blob(GlowM(new Color(1f, .5f, .15f), 3f), at + up * 1.15f, new Vector3(.3f, .15f, .3f), ctx.Rng, .3f); halos.Add(at + up * 1.5f, 1.6f, new Color(1f, .5f, .15f), .7f); life.fires.Add(at + up * 1.25f); }
            ArtistBits.PostCollider(ctx.Root, at, 1.1f, .48f);
        }

        void Pallets(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 at)
        {
            var up = Vector3.up; var rot = Quaternion.Euler(0, ctx.Range(0, 90f), 0); var wood = WorldMaterials.Solid(new Color(.45f, .36f, .25f));
            var h = ctx.Rng.Next(2, 7); for (var k = 0; k < h; k++) mk.Box(wood, at + up * (.08f + k * .16f), new Vector3(1.2f, .14f, 1f), rot * Quaternion.Euler(0, k * 4f, 0));
            solid.Box(wood, at + up * h * .08f, new Vector3(1.3f, h * .16f, 1.1f), rot);
        }

        void Car(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 at)
        {
            var up = Vector3.up; var d = Quaternion.Euler(0, ctx.Range(0, 360f), 0) * Vector3.forward; var rot = Quaternion.LookRotation(d);
            Color[] paint = { new Color(.3f, .32f, .35f), new Color(.4f, .15f, .12f), new Color(.15f, .2f, .3f), new Color(.6f, .58f, .5f) };
            var body = WorldMaterials.Solid(paint[ctx.Rng.Next(paint.Length)], .6f);
            mk.Box(body, at + up * .6f, new Vector3(1.7f, .6f, 4f), rot); mk.Box(body, at + up * 1.1f - d * .2f, new Vector3(1.5f, .5f, 2f), rot);
            mk.Box(glassDark, at + up * 1.1f - d * .2f, new Vector3(1.52f, .38f, 1.9f), rot);
            foreach (var s in new[] { -1f, 1f }) foreach (var w in new[] { -1.3f, 1.3f }) mk.Prism(dark, at + Vector3.Cross(up, d) * s * .8f + d * w + up * .33f, Vector3.Cross(up, d) * s, .33f, .18f, 10);
            solid.Box(body, at + up * .75f, new Vector3(1.8f, 1.5f, 4.1f), rot);
        }

        /// A street grate breathing steam (DocksLife), lit orange from below.
        void Grate(LandContext ctx, MeshKit mk, Vector3 at)
        {
            var rot = Quaternion.Euler(0, ctx.Range(0, 90f), 0);
            mk.Box(GlowM(Sodium, .8f), at + Vector3.up * .04f, new Vector3(1.2f, .04f, .8f), rot);
            for (var k = 0; k < 5; k++) mk.Box(steel, at + Vector3.up * .1f + rot * new Vector3((k - 2f) * .22f, 0, 0), new Vector3(.06f, .04f, .78f), rot);
            life.grates.Add(at + Vector3.up * .2f);
        }

        void FlyerWall(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 at)
        {
            var up = Vector3.up; var face = new Vector3(center.x - at.x, 0, center.z - at.z).normalized; var rot = Quaternion.LookRotation(face);
            var m = ctx.Chance(.5f) ? brick : concrete; var W = ctx.Range(5f, 9f);
            mk.Box(m, at + up * 1.4f, new Vector3(W, 2.8f, .5f), rot); solid.Box(m, at + up * 1.4f, new Vector3(W, 2.8f, .5f), rot);
            Flyers(mk, at + up * 1.4f + face * .26f, face, W - .4f, 2.2f, ctx.Rng);
            if (ctx.Chance(.5f)) { var l = at + up * 3f + face * .5f; mk.Box(dark, l, new Vector3(.4f, .2f, .5f), rot); mk.Box(sodium, l - up * .1f, new Vector3(.3f, .04f, .4f), rot); lampHalos.Add(l - up * .3f, 1.6f, Sodium, .6f); life.lamps.Add(l); }
        }

        void Bench(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 at)
        {
            var up = Vector3.up; var d = Quaternion.Euler(0, ctx.Range(0, 360f), 0) * Vector3.forward; var rot = Quaternion.LookRotation(d); var sd = Vector3.Cross(up, d);
            mk.Box(concrete, at + up * .25f, new Vector3(2.2f, .5f, .6f), rot); solid.Box(concrete, at + up * .25f, new Vector3(2.2f, .5f, .6f), rot); if (plife != null) Interact.Bench(plife.transform, at + up * .56f - sd * .6f, at + up * .56f + sd * .6f, d);
            SodiumLamp(ctx, mk, at + sd * 1.8f - d * .5f, -sd);
        }

        void Weeds(LandContext ctx)
        {
            var mat = WindMesh.Material(.2f, 1f, new Color(.2f, .26f, .18f));
            var meshes = new Dictionary<(int, int), WindMesh>(); var rng = new System.Random(1979);
            const float st = 3f;
            for (var x = ctx.MinX; x < ctx.MaxX; x += st)
            for (var z = ctx.MinZ; z < ctx.MaxZ; z += st)
            {
                var px = x + ((float)rng.NextDouble() - .5f) * st; var pz = z + ((float)rng.NextDouble() - .5f) * st;
                if (!ctx.IsLand(px, pz, 1f) || NearPath(px, pz, 3f) || InNoGrass(px, pz, 0f) || InFootprint(ctx, px, pz, -2f) || OnQuay(px, pz, 40f)) continue;
                if (Mathf.PerlinNoise(px * .05f + 3f, pz * .05f + 8f) < .45f) continue;
                var key = (Mathf.FloorToInt(px / 70f), Mathf.FloorToInt(pz / 70f));
                if (!meshes.TryGetValue(key, out var wm)) meshes[key] = wm = new WindMesh();
                var col = Color.Lerp(new Color(.16f, .22f, .14f), new Color(.3f, .32f, .2f), (float)rng.NextDouble());
                wm.Tuft(new Vector3(px, ctx.Ground(px, pz) - .04f, pz), .25f + (float)rng.NextDouble() * .35f, col, Color.Lerp(col, new Color(.45f, .4f, .25f), .4f), rng, 3, .06f);
            }
            foreach (var kv in meshes) kv.Value.Build("Docklands weeds", ctx.Root, mat);
        }
    }
}
