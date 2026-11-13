using System.Collections.Generic;
using UnityEngine;
using static EverythingWorld.World.PlaceTools;

namespace EverythingWorld.World
{
    /// Studio Lot Island (WORLD_GUIDE.md): a film studio backlot at dusk. Sound stages like aircraft hangars with their
    /// numbers painted on and red ON AIR lights over the doors, sets standing side by side with their backs held up by
    /// scaffolding, trailers, lighting rigs, director's chairs, and searchlights sweeping the sky. You arrive at the Front
    /// Gate under the water tower. Backlot Avenue runs between the stages to the Tank, where a pirate ship floats on its
    /// own sea. Every set is a different film; walking it is like flicking through every movie you ever loved.
    /// Laid out on purpose (the build-land standard): StudioTown.cs has the places and the people.
    public partial class StudioKit : LandKit
    {
        public override string Land => "Studio Lot Island";
        public override Vector3 ArtistFront(IslandWorldData data, IslandWorldData.Artist a) => facing.TryGetValue(a.name, out var f) ? f : base.ArtistFront(data, a);

        public static readonly Color Pink = new Color(1f, .5f, .7f), Gold = new Color(1f, .8f, .3f), Red = new Color(.8f, .16f, .16f), Navy = new Color(.16f, .2f, .36f), Cream = new Color(.96f, .92f, .82f), Ink = new Color(.1f, .08f, .08f), Sky = new Color(.5f, .7f, .9f), Saffron = new Color(1f, .6f, .14f), Sea = new Color(.2f, .5f, .6f);
        static readonly Color[] Stages = { new Color(.84f, .82f, .76f), new Color(.7f, .72f, .74f), new Color(.86f, .78f, .64f), new Color(.6f, .64f, .7f) };
        const float PathW = 3f, StreetW = 9f;

        Material asphalt, concrete, scaffold, woodM, canvas, iron, dark, white, leafM, trunkM, windowM, lampM, onAirM, seaM, sand;
        GlowHalos halos; Vector3 center, D, S; StudioLife life;
        readonly Dictionary<(int, int), List<Vector3>> pathCells = new Dictionary<(int, int), List<Vector3>>();
        readonly List<(Vector3 p, int w)> walkPoints = new List<(Vector3, int)>();
        readonly Dictionary<string, Vector3> facing = new Dictionary<string, Vector3>();
        // x, radius, z
        readonly List<Vector3> noGrass = new List<Vector3>();
        Vector2 heart2, D2 = new Vector2(0, -1), S2 = new Vector2(1, 0); float plazaY, waterY, streetL;

        void MakeMaterials()
        {
            asphalt = WorldMaterials.Glow(new Color(.28f, .27f, .3f), .12f); concrete = WorldMaterials.Glow(new Color(.66f, .64f, .62f), .12f); scaffold = WorldMaterials.Solid(new Color(.5f, .52f, .56f), .6f); woodM = WorldMaterials.Glow(new Color(.56f, .42f, .3f), .1f); canvas = WorldMaterials.Glow(new Color(.86f, .82f, .72f), .14f); iron = WorldMaterials.Solid(new Color(.18f, .18f, .2f), .5f); dark = WorldMaterials.Solid(new Color(.08f, .07f, .08f)); white = WorldMaterials.Glow(new Color(.95f, .93f, .9f), .14f);
            leafM = WorldMaterials.Glow(new Color(.34f, .52f, .3f), .12f); trunkM = WorldMaterials.Glow(new Color(.4f, .3f, .24f), .1f); sand = WorldMaterials.Glow(new Color(.86f, .7f, .46f), .14f);
            windowM = new Material(WorldMaterials.Glow(new Color(1f, .8f, .5f), 1.1f)) { name = "Studio windows" }; lampM = new Material(WorldMaterials.Glow(new Color(1f, .92f, .76f), 1.6f)) { name = "Studio lamps" }; onAirM = new Material(WorldMaterials.Glow(new Color(1f, .14f, .1f), 2f)) { name = "Studio on air" }; seaM = new Material(WorldMaterials.Glow(Sea, .6f)) { name = "Studio tank water" };
        }

        Material GlowM(Color c, float s = 2f) => WorldMaterials.Glow(c, s * .65f);
        Material Paint(Color c) => WorldMaterials.Glow(c, .14f);
        static bool HasStation(IslandWorldData.Artist a) => a.stationId != 0;
        static float Foot(IslandWorldData.Artist a) => a.name == "The Lion King" ? 16f : a.name == "Daniele Zandara" ? 13f : a.name == "Phineas and Ferb" ? 15f : 9f;

        public override bool Ground(float height, float slope, float patch, out Color color)
        {
            // dusty studio ground: pale earth and patchy grass
            if (slope > 24f) color = Color.Lerp(new Color(.6f, .5f, .42f), new Color(.68f, .58f, .48f), patch);
            else color = patch < .45f ? Color.Lerp(new Color(.62f, .56f, .46f), new Color(.68f, .6f, .48f), patch / .45f) : Color.Lerp(new Color(.46f, .56f, .36f), new Color(.56f, .62f, .4f), (patch - .45f) / .55f);
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
            for (var k = 0; k < 8; k++) { var dir = new Vector2(Mathf.Sin(k * Mathf.PI / 4f), Mathf.Cos(k * Mathf.PI / 4f)); var len = 40f; while (len < 130f && Inside(heart2 + dir * (len + 10f), 50f)) len += 10f; if (len > streetL) { streetL = len; D2 = dir; } }
            S2 = new Vector2(-D2.y, D2.x); plazaY = Mathf.Max(data.HeightAt(heart2.x, heart2.y), waterY + 6f);
            var tank = P2(streetL + 22f, 0);
            var keep = new List<string>();
            void Put(string name, float d, float s, float room) { var p = P2(d, s); if (!Inside(p, room)) return; foreach (var a in data.layout.artists) if (a.sceneId == isl.sceneId && a.name == name) { a.x = p.x; a.z = p.y; keep.Add(name); } }
            Put("The Lion King", -6f, 56f, 32f); Put("Daniele Zandara", 0f, -54f, 28f);
            var avoid = new List<Vector3>(); for (var d = 0f; d <= streetL; d += 22f) avoid.Add(new Vector3(heart2.x + D2.x * d, 20f, heart2.y + D2.y * d)); avoid.Add(new Vector3(tank.x, 30f, tank.y));
            ArtistPlacement.Spread(data, index, 52f, heart2, 44f, 40f, avoid, keep, Land, 330f);
            Level(data, index, heart2, 22f, 40f, plazaY); Level(data, index, tank, 26f, 42f, Mathf.Max(data.HeightAt(tank.x, tank.y), waterY + 4f));
            for (var d = 26f; d <= streetL; d += 12f) foreach (var e in new[] { -1f, 1f }) { var p = P2(d, e * 18f); if (Inside(p, 14f)) Level(data, index, p, 10f, 18f, Mathf.Max(data.HeightAt(p.x, p.y), waterY + 3f)); }
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
            life = new GameObject("Studio life").AddComponent<StudioLife>();
            life.transform.SetParent(ctx.Root, false);
            life.center = center; life.lamps = lampM; life.onAir = onAirM; life.sea = seaM; life.windows = windowM;
            var mk = new MeshKit(); var solid = new MeshKit();
            Remember(ctx.OnGround(center.x, center.z), 0);
            TownStart(ctx);
            FrontGate(ctx, mk, solid);
            BacklotAvenue(ctx, mk, solid);
            TheTank(ctx, mk, solid);
            Paths(ctx, mk, solid);
            SoundStages(ctx, mk, solid);
            Wilds(ctx, mk, solid);
            mk.Build("Studio Lot Island", ctx.Root, false);
            solid.BuildCollider("Studio Lot Island walls", ctx.Root);
        }

        public override void Finish(LandContext ctx) { Verge(ctx); halos.Build("Studio glow", ctx.Root); }

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

        /// A film light on a stand: a black can on a tripod, its lens glowing.
        void FilmLight(LandContext ctx, MeshKit mk, Vector3 g, Vector3 aim, bool light = false)
        {
            var up = Vector3.up; aim.y = 0; aim.Normalize(); var head = g + up * 2.6f; for (var k = 0; k < 3; k++) { var leg = Quaternion.Euler(0, k * 120f, 0) * Vector3.forward * .6f; mk.Prism(iron, g + leg, (head - g - leg).normalized, .025f, (head - g - leg).magnitude, 3); }
            var dir = (aim - up * .3f).normalized; mk.Prism(dark, head - dir * .3f, dir, .26f, .6f, 8); mk.Prism(lampM, head + dir * .3f, dir, .24f, .02f, 8); halos.Add(head + dir * .5f, 1.4f, new Color(1f, .92f, .76f), .4f); ArtistBits.PostCollider(ctx.Root, g, 2.6f, .3f);
            if (light) { var go = new GameObject("Film light"); go.transform.SetParent(ctx.Root, false); go.transform.SetPositionAndRotation(head + dir * .4f, Quaternion.LookRotation(dir)); var l = go.AddComponent<Light>(); l.type = LightType.Spot; l.range = 18f; l.spotAngle = 50f; l.intensity = 3f; l.color = new Color(1f, .9f, .76f); }
        }

        /// A director's chair, canvas and wood, with a name on the back.
        void DirectorsChair(MeshKit mk, Vector3 g, Vector3 face, string name)
        {
            var up = Vector3.up; var rot = Quaternion.LookRotation(face); var side = Vector3.Cross(up, face); foreach (var e in new[] { -1f, 1f }) { mk.Box(woodM, g + up * .4f + side * e * .28f, new Vector3(.05f, .05f, .6f), rot * Quaternion.Euler(30f, 0, 0)); mk.Box(woodM, g + up * .4f + side * e * .28f, new Vector3(.05f, .05f, .6f), rot * Quaternion.Euler(-30f, 0, 0)); mk.Box(woodM, g + up * .9f + side * e * .28f - face * .24f, new Vector3(.05f, .7f, .05f), rot); }
            mk.Box(Paint(Navy), g + up * .66f, new Vector3(.56f, .04f, .44f), rot); mk.Box(Paint(Navy), g + up * 1.06f - face * .25f, new Vector3(.56f, .26f, .02f), rot); if (name != null) ArtistBits.Label(rootT, name, g + up * 1.06f - face * .26f, -face, .05f, Cream, TextAnchor.MiddleCenter);
            BoxCol(rootT, g + up * .5f, new Vector3(.66f, 1f, .56f), rot); Interact.Seat(tlife.transform, g + up * .7f, face, "Sit in the director's chair");
        }

        /// A painted set flat: a flat wall with scaffolding braced behind it.
        void Flat(LandContext ctx, MeshKit mk, MeshKit solid, PlaceFrame f, float w, float h, Material face)
        {
            var up = Vector3.up; mk.Box(face, f.P(0, h / 2f, 0), new Vector3(w, h, .2f), f.r); solid.Box(face, f.P(0, h / 2f, 0), new Vector3(w, h, .2f), f.r); mk.Box(canvas, f.P(0, h / 2f - .02f, -.12f), new Vector3(w - .04f, h - .08f, .02f), f.r);
            for (var x = -w / 2f + .5f; x <= w / 2f - .4f; x += 2.4f) { mk.Prism(scaffold, f.P(x, 0, -.2f), up, .05f, h, 4); mk.Prism(scaffold, f.P(x, 0, -2f), (f.P(x, h * .8f, -.2f) - f.P(x, 0, -2f)).normalized, .04f, (f.P(x, h * .8f, -.2f) - f.P(x, 0, -2f)).magnitude, 4); ArtistBits.PostCollider(ctx.Root, f.P(x, 0, -1.4f), 1.4f, .3f); }
        }

        /// A bench to sit on.
        void Bench(Transform parent, MeshKit mk, Vector3 g, Vector3 face, float len = 2f)
        {
            var up = Vector3.up; var rot = Quaternion.LookRotation(face); var side = Vector3.Cross(up, face); for (var k = 0; k < 3; k++) mk.Box(woodM, g + up * .46f + face * (-.16f + k * .16f), new Vector3(len, .05f, .13f), rot); for (var k = 0; k < 2; k++) mk.Box(woodM, g + up * (.7f + k * .2f) - face * .26f, new Vector3(len, .13f, .05f), rot); foreach (var e in new[] { -1f, 1f }) mk.Box(iron, g + up * .3f + side * e * (len / 2f - .15f), new Vector3(.06f, .6f, .5f), rot);
            BoxCol(parent, g + up * .45f, new Vector3(len, .9f, .5f), rot); Interact.Bench(tlife.transform, g + up * .52f - side * (len / 2f - .5f), g + up * .52f + side * (len / 2f - .5f), face);
        }

        /// A street lamp of the old studio kind.
        void Lamp(LandContext ctx, MeshKit mk, Vector3 g, bool light = false)
        {
            var up = Vector3.up; mk.Prism(iron, g - up * .2f, up, .07f, 4.2f, 6, .7f); mk.Prism(iron, g + up * 3.9f, up, .16f, .1f, 6); mk.Box(lampM, g + up * 4.2f, new Vector3(.3f, .5f, .3f), Quaternion.identity); mk.Cone(iron, g + up * 4.46f, .24f, .2f, 4, 45f); halos.Add(g + up * 4.2f, 1.6f, new Color(1f, .9f, .7f), .45f); ArtistBits.PostCollider(ctx.Root, g, 4f, .09f); if (light) ArtistBits.PointLight(ctx.Root, g + up * 3.6f, new Color(1f, .88f, .7f), 14f, 1.6f);
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
                Strip(mk, pts, asphalt, PathW, .13f, ctx);
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

        // ---------------------------------------------------------------- the rest of the lot
        /// Trailers, lighting rigs, stacked flats, prop piles (crates, a fake rock, a column), palm trees in pots, golf-cart-sized studio carts, benches.
        void Wilds(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            int trailers = 0, rigs = 0, flats = 0, props = 0, trees = 0, benches = 0; var rng = new System.Random(1939);
            const float cell = 22f; string[] names = { "STAR", "MAKEUP", "WARDROBE", "DIRECTOR", "STUNTS", "EXTRAS", "CATERING" };
            for (var x = ctx.MinX; x < ctx.MaxX; x += cell)
            for (var z = ctx.MinZ; z < ctx.MaxZ; z += cell)
            {
                float px = x + (float)rng.NextDouble() * cell, pz = z + (float)rng.NextDouble() * cell; var roll = (float)rng.NextDouble(); var s = (float)rng.NextDouble();
                if (!Open(ctx, px, pz, 4f)) continue;
                var p = ctx.OnGround(px, pz); var f = FaceTo(p, NearestWalk(p)); var rot = Quaternion.LookRotation(f); var side = Vector3.Cross(Vector3.up, f); var up = Vector3.up;
                if (roll < .14f) { if (!Open(ctx, px, pz, 6f)) continue; var tr = Quaternion.LookRotation(side); mk.Box(white, p + up * 1.5f, new Vector3(2.4f, 2.4f, 7f), tr); solid.Box(white, p + up * 1.5f, new Vector3(2.4f, 2.4f, 7f), tr); mk.Box(Paint(Red), p + up * .6f, new Vector3(2.42f, .2f, 7.02f), tr); for (var k = 0; k < 3; k++) mk.Box(windowM, p + up * 1.8f + side * (-2.2f + k * 2.2f) + f * 1.21f, new Vector3(.8f, .5f, .02f), rot); mk.Box(woodM, p + up * 1.4f + f * 1.21f + side * 3f, new Vector3(.7f, 1.8f, .02f), rot); mk.Box(Paint(Gold), p + up * 2.4f + f * 1.22f + side * 3f, new Vector3(.4f, .4f, .01f), rot); ArtistBits.Label(rootT, names[trailers % names.Length], p + up * 2.3f + f * 1.23f, f, .12f, Ink, TextAnchor.MiddleCenter); foreach (var e in new[] { -2.4f, 2.4f }) mk.Prism(dark, p + up * .4f + side * e - f * .9f - f * .2f, f, .4f, .2f, 10); noGrass.Add(new Vector3(p.x, 4f, p.z)); trailers++; }
                else if (roll < .26f) { FilmLight(ctx, mk, p, f, rigs % 3 == 0); rigs++; }
                else if (roll < .36f) { for (var k = 0; k < 3; k++) mk.Box(k % 2 == 0 ? canvas : Paint(Stages[k % 4]), p + up * 1.6f + side * (k * .3f) - f * (k * .2f), new Vector3(3f, 3.2f, .14f), rot * Quaternion.Euler(-8f, k * 4f, 0)); BoxCol(ctx.Root, p + up * 1.6f, new Vector3(3.4f, 3.2f, .8f), rot); flats++; }
                else if (roll < .48f) { for (var k = 0; k < 4; k++) mk.Box(woodM, p + up * (.35f + (k / 2) * .6f) + side * ((k % 2) * .8f), new Vector3(.7f, .6f, .6f), rot * Quaternion.Euler(0, k * 7f, 0)); if (s > .5f) mk.Blob(Paint(new Color(.6f, .56f, .5f)), p + f * 1.4f + up * .8f, new Vector3(1.4f, 1f, 1.2f), rng, .3f); else { mk.Prism(white, p + f * 1.4f, up, .3f, 2.6f, 10); mk.Box(white, p + f * 1.4f + up * 2.7f, new Vector3(.8f, .2f, .8f), rot); } BoxCol(ctx.Root, p + up * .8f + f * .9f, new Vector3(2.8f, 1.6f, 3f), rot); if (s <= .5f) ArtistBits.PostCollider(ctx.Root, p + f * 1.4f, 2.8f, .4f); props++; }
                else if (roll < .66f) { mk.Prism(Paint(new Color(.66f, .4f, .3f)), p, up, .5f, .7f, 8, 1.2f); var prev = p + up * .6f; for (var k = 0; k < 5; k++) { var dir = (up + new Vector3(.1f, 0, .06f) * k * .2f).normalized; mk.Prism(trunkM, prev, dir, .14f - k * .015f, 1f, 6); prev += dir; } for (var k = 0; k < 7; k++) { var fd = Quaternion.Euler(0, k * 51f, 0) * Vector3.forward; mk.Tri(leafM, prev, prev + fd * 2f - up * .8f + Vector3.Cross(up, fd) * .3f, prev + fd * 2f - up * .8f - Vector3.Cross(up, fd) * .3f, prev + up); mk.Tri(leafM, prev, prev + fd * 2f - up * .8f + Vector3.Cross(up, fd) * .3f, prev + fd * 2f - up * .8f - Vector3.Cross(up, fd) * .3f, prev - up * 3f); } ArtistBits.PostCollider(ctx.Root, p, 1f, .5f); ArtistBits.PostCollider(ctx.Root, p + new Vector3(.1f, 0, .06f), 3.4f, .3f); noGrass.Add(new Vector3(p.x, 1f, p.z)); trees++; }
                else if (roll < .72f) { Bench(ctx.Root, mk, p, f); Lamp(ctx, mk, p - f * 1f + side * 1.4f, benches % 2 == 0); DirectorsChair(mk, p + f * 1.6f + side * 1.6f, -f, null); noGrass.Add(new Vector3(p.x, 2.4f, p.z)); benches++; }
            }
            Debug.Log($"Studio Lot Island: Backlot Avenue {streetL:0} m, {trailers} trailers, {rigs} film lights, {flats} stacks of flats, {props} prop piles, {trees} potted palms, {benches} benches");
        }

        Vector3 NearestWalk(Vector3 p) { var best = center; var bd = float.MaxValue; foreach (var (q, _) in walkPoints) { var d = (q - p).sqrMagnitude; if (d < bd) { bd = d; best = q; } } return best; }

        /// Scrubby grass between the stages.
        void Verge(LandContext ctx)
        {
            var mat = WindMesh.Material(.3f, .8f, new Color(.52f, .58f, .38f));
            var meshes = new Dictionary<(int, int), WindMesh>(); var rng = new System.Random(1940);
            const float st = 2.8f;
            for (var x = ctx.MinX; x < ctx.MaxX; x += st)
            for (var z = ctx.MinZ; z < ctx.MaxZ; z += st)
            {
                var px = x + ((float)rng.NextDouble() - .5f) * st; var pz = z + ((float)rng.NextDouble() - .5f) * st;
                if (!ctx.IsLand(px, pz, 1f) || NearPath(px, pz, 3.6f) || InNoGrass(px, pz, 0f) || InFootprint(ctx, px, pz, -2f)) continue;
                if (Mathf.PerlinNoise(px * .04f + 8f, pz * .04f + 4f) < .5f) continue;
                var key = (Mathf.FloorToInt(px / 70f), Mathf.FloorToInt(pz / 70f));
                if (!meshes.TryGetValue(key, out var wm)) meshes[key] = wm = new WindMesh();
                var col = Color.Lerp(new Color(.46f, .54f, .34f), new Color(.62f, .6f, .4f), (float)rng.NextDouble());
                wm.Tuft(new Vector3(px, ctx.Ground(px, pz) - .04f, pz), .24f + (float)rng.NextDouble() * .3f, col, Color.Lerp(col, Cream, .4f), rng, 3, .08f);
            }
            foreach (var kv in meshes) kv.Value.Build("Studio grass", ctx.Root, mat);
        }
    }
}
