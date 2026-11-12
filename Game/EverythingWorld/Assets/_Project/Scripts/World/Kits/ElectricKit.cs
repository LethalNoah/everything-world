using System.Collections.Generic;
using UnityEngine;

namespace EverythingWorld.World
{
    /// The Electric Fields (WORLD_GUIDE.md): a whimsical music festival on rolling hills at golden dusk, sliding
    /// into night. Uplifting, upbeat, a little weird. You arrive at the festival gate; flag-lined walkways under
    /// strings of lights lead to every stage and tent. MGMT's Big Top, a huge striped tent with a mirrored disco
    /// ball bursting through its roof, is the heart of it; Santigold has the big main stage; LCD Soundsystem's
    /// silver dome sits dim and waiting until one of their songs plays, then it erupts. Round them: a Ferris wheel
    /// turning on a hill, campsites, food trucks, hay bales with synths, giant inflatables, light towers, lasers
    /// over the crowd at night. "Kids" brings fireworks and confetti; "Electric Feel" sends electric blue along
    /// every string of lights. Files: ElectricKit (ground, layout, walkways, lights), ElectricArtists, ElectricWilds
    /// (the festival round them), ElectricLife (sky, lasers, fireworks, happenings).
    public partial class ElectricKit : LandKit
    {
        public override Vector3 ArtistFront(IslandWorldData data, IslandWorldData.Artist a) =>
            facing.TryGetValue(a.name, out var f) ? f : base.ArtistFront(data, a);
        public override string Land => "The Electric Fields";

        public const string Kids = "1jJci4qxiYcOHhQR247rEU", ElectricFeel = "3FtYbEfBqAlGO46NUDQSAt", AllMyFriends = "2Ud3deeqLAG988pfW0Kwcl",
                            DanceYrselfClean = "2cmRpmO04TLaKPzmAzySYZ", DisparateYouth = "0t2QiRkpag0fAgs9zuCPlH";
        public override (string song, string label, string artist)[] SpecialSongs => new[] { (Kids, "Kids", "MGMT"), (ElectricFeel, "Electric Feel", "MGMT"), (AllMyFriends, "All My Friends", "LCD Soundsystem"), (DisparateYouth, "Disparate Youth", "Santigold") };

        static readonly Color Grass = new Color(.42f, .58f, .3f), GrassB = new Color(.52f, .62f, .32f), Straw = new Color(.78f, .66f, .42f), Trodden = new Color(.68f, .6f, .44f);
        static readonly Color Sunset = new Color(1f, .62f, .32f), Hot = new Color(1f, .3f, .55f), Electric = new Color(.3f, .75f, 1f), Lime = new Color(.65f, 1f, .3f), Ink = new Color(.98f, .96f, .92f);
        static readonly Color[] Bunting = { new Color(.95f, .3f, .35f), new Color(1f, .78f, .25f), new Color(.3f, .7f, .95f), new Color(.55f, .85f, .4f), new Color(.8f, .45f, .9f), new Color(1f, .55f, .2f) };
        static readonly Color[] Neon = { new Color(1f, .3f, .6f), new Color(.3f, .8f, 1f), new Color(.7f, 1f, .3f), new Color(1f, .75f, .25f), new Color(.75f, .45f, 1f) };

        const float PathW = 3f;
        Material wood, metal, dark, canvas, straw, rope;
        GlowHalos halos;
        Vector3 center;
        readonly Dictionary<(int, int), List<Vector3>> pathCells = new Dictionary<(int, int), List<Vector3>>();
        readonly List<(Vector3 p, int w)> walkPoints = new List<(Vector3, int)>();
        readonly Dictionary<string, Vector3> facing = new Dictionary<string, Vector3>();
        readonly List<Vector3> noGrass = new List<Vector3>();
        ElectricLife life;
        MeshKit soft;

        void MakeMaterials()
        {
            wood = WorldMaterials.Solid(new Color(.5f, .38f, .28f));
            metal = WorldMaterials.Solid(new Color(.55f, .56f, .6f), .5f);
            dark = WorldMaterials.Solid(new Color(.1f, .1f, .12f));
            canvas = WorldMaterials.Solid(new Color(.94f, .92f, .86f));
            straw = WorldMaterials.Solid(Straw);
            rope = WorldMaterials.Solid(new Color(.3f, .3f, .32f));
        }

        Material GlowM(Color c, float s = 2f) => WorldMaterials.Glow(c, s * .65f);       // kept low so colours stay saturated under the bloom
        static bool HasStation(IslandWorldData.Artist a) => a.station || a.stationId != 0;

        /// How far an artist's own scene reaches (the big tent and the main stage are big).
        static float Foot(IslandWorldData.Artist a) => a.name == "MGMT" ? 26f : a.name == "Santigold" ? 20f : a.name == "LCD Soundsystem" ? 14f : 9f;

        public override bool Ground(float height, float slope, float patch, out Color color)
        {
            if (slope > 7f) color = Color.Lerp(new Color(.5f, .48f, .4f), new Color(.58f, .55f, .45f), patch);
            else if (height < 2.5f) color = Color.Lerp(new Color(.76f, .7f, .55f), new Color(.7f, .66f, .52f), patch);
            else color = Color.Lerp(Grass, GrassB, Mathf.SmoothStep(.1f, .9f, patch));
            color.a = 1f;
            return true;
        }

        /// The festival spreads round the gate where you arrive, stages about 95 m apart, the Big Top and the main
        /// stage nearest, a level pad under each of the big ones.
        public override void ShapeGround(IslandWorldData data, int index)
        {
            var isl = data.layout.islands[index];
            var heart = new Vector2(isl.labelX, isl.labelZ);
            ArtistPlacement.Spread(data, index, 95f, heart, 50f, 40f, null, null, Land);
            foreach (var a in data.layout.artists)
            {
                if (a.sceneId != isl.sceneId) continue;
                a.r = Mathf.Max(a.r, Foot(a) + 8f);                                          // the radio ring takes in the whole stage
                if (a.name == "MGMT" || a.name == "Santigold" || a.name == "LCD Soundsystem") Level(data, index, new Vector2(a.x, a.z), Foot(a) + 4f, Foot(a) + 22f);
            }
            Level(data, index, heart, 14f, 34f);
            foreach (var (at, r) in PlaceRings) Level(data, index, at, r, r + 14f);
        }

        static void Level(IslandWorldData data, int index, Vector2 c, float flat, float blend)
        {
            var level = data.HeightAt(c.x, c.y);
            for (var r = 0; r < data.Nz; r++)
            {
                var z = data.layout.depth - r * data.Step;
                if (Mathf.Abs(z - c.y) > blend) continue;
                for (var col = 0; col < data.Nx; col++)
                {
                    var x = col * data.Step;
                    if (Mathf.Abs(x - c.x) > blend) continue;
                    var i = r * data.Nx + col;
                    if (data.islandIds[i] != index) continue;
                    var d = Vector2.Distance(new Vector2(x, z), c);
                    if (d < flat) data.heights[i] = level;
                    else if (d < blend) data.heights[i] = Mathf.Lerp(level, data.heights[i], Mathf.SmoothStep(0, 1, (d - flat) / (blend - flat)));
                }
            }
        }

        public override void Dress(LandContext ctx)
        {
            MakeMaterials();
            halos = new GlowHalos();
            pathCells.Clear(); walkPoints.Clear(); facing.Clear(); noGrass.Clear();
            center = ctx.Plaza;
            life = new GameObject("Electric life").AddComponent<ElectricLife>();
            life.transform.SetParent(ctx.Root, false);
            life.center = center;
            var mk = new MeshKit(); var solid = new MeshKit(); soft = new MeshKit();
            var gateFront = Gate(ctx, mk, solid);
            Walkways(ctx, mk, solid, gateFront);
            SongPlaces(ctx, mk, solid);
            FieldPlaces(ctx, mk, solid);
            Festival(ctx, mk, solid);
            mk.Build("Festival", ctx.Root, false);
            soft.Build("Soft festival", ctx.Root, false);
            solid.BuildCollider("Festival walls", ctx.Root);
        }

        public override void Finish(LandContext ctx)
        {
            GroundCover(ctx);
            halos.Build("Festival glow", ctx.Root);
        }

        // ---------------------------------------------------------------- helpers
        void Remember(Vector3 p, int walk)
        {
            var key = (Mathf.FloorToInt(p.x / 8f), Mathf.FloorToInt(p.z / 8f));
            if (!pathCells.TryGetValue(key, out var l)) pathCells[key] = l = new List<Vector3>();
            l.Add(p); walkPoints.Add((p, walk));
        }

        bool NearPath(float x, float z, float within)
        {
            int cx = Mathf.FloorToInt(x / 8f), cz = Mathf.FloorToInt(z / 8f);
            for (var i = -1; i <= 1; i++)
            for (var j = -1; j <= 1; j++)
                if (pathCells.TryGetValue((cx + i, cz + j), out var l))
                    foreach (var p in l) { float dx = p.x - x, dz = p.z - z; if (dx * dx + dz * dz < within * within) return true; }
            return false;
        }

        bool InNoGrass(float x, float z, float pad)
        {
            foreach (var c in noGrass) { float dx = x - c.x, dz = z - c.z, r = c.y + pad; if (dx * dx + dz * dz < r * r) return true; }
            return false;
        }

        bool InFootprint(LandContext ctx, float x, float z, float pad, float scale = 1f)
        {
            foreach (var a in ctx.Artists) { float dx = x - a.x, dz = z - a.z, r = Foot(a) * scale + pad; if (dx * dx + dz * dz < r * r) return true; }
            return false;
        }

        bool Open(LandContext ctx, float x, float z, float pad) =>
            ctx.IsLand(x, z, 4f) && !InFootprint(ctx, x, z, pad) && !NearPath(x, z, pad + 2f) && !InNoGrass(x, z, pad)
            && Vector2.Distance(new Vector2(x, z), new Vector2(center.x, center.z)) > 24f + pad;

        /// A festival light: a pole with a cluster of coloured lamps (a light tower when tall).
        void LightPole(LandContext ctx, MeshKit mk, Vector3 g, Color col, float h = 4f)
        {
            mk.Prism(metal, g - Vector3.up * .3f, Vector3.up, .08f, h + .3f, 6);
            var top = g + Vector3.up * h;
            mk.Box(dark, top, new Vector3(.8f, .3f, .3f), Quaternion.Euler(0, ctx.Range(0, 180f), 0));
            mk.Blob(GlowM(col, 2.4f), top - Vector3.up * .25f, new Vector3(.25f, .18f, .25f), ctx.Rng, 0f);
            halos.Add(top - Vector3.up * .3f, 1.8f, col, .9f);
            ArtistBits.PostCollider(ctx.Root, g, h, .12f);
        }

        // ---------------------------------------------------------------- the gate where you arrive
        /// A festival gate of scaffolding wrapped in flags, with a light-up sign across the top and lanterns, facing
        /// the Big Top. Returns the spot just inside it, where the walkways start.
        Vector3 Gate(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            IslandWorldData.Artist big = null;
            foreach (var a in ctx.Artists) if (a.name == "MGMT") big = a;
            var face = big != null ? new Vector3(big.x - center.x, 0, big.z - center.z).normalized : Vector3.forward;
            var side = Vector3.Cross(Vector3.up, face); var up = Vector3.up;
            var c = ctx.OnGround(center.x, center.z);
            const float span = 12f, h = 7f;
            foreach (var s in new[] { -1f, 1f })
            {
                var foot = c + side * s * span / 2f;
                for (var k = 0; k < 4; k++)
                {
                    var p = foot + side * s * ((k % 2) * .9f) + face * ((k / 2) * .9f - .45f);
                    mk.Box(metal, p + up * h / 2f, new Vector3(.12f, h, .12f), Quaternion.identity);
                }
                for (var y = 1.2f; y < h; y += 1.6f) mk.Box(metal, foot + side * s * .45f + up * y, new Vector3(1f, .08f, 1f), Quaternion.LookRotation(face));
                solid.Box(metal, foot + side * s * .45f + up * h / 2f, new Vector3(1.1f, h, 1.1f), Quaternion.LookRotation(face));
                // flags wrapped round the towers
                for (var k = 0; k < 5; k++) soft.Box(WorldMaterials.Solid(Bunting[(k + (s > 0 ? 2 : 0)) % Bunting.Length]), foot + side * s * .45f + up * (1.6f + k * 1.1f) - face * .58f, new Vector3(1.1f, .9f, .02f), Quaternion.LookRotation(face));
            }
            // the sign across the top
            var signC = c + up * (h + .8f);
            mk.Box(dark, signC, new Vector3(span + 2f, 1.6f, .3f), Quaternion.LookRotation(face));
            for (var k = 0; k < 28; k++)
            {
                var col = Neon[k % Neon.Length];
                foreach (var y in new[] { -.7f, .7f }) mk.Blob(GlowM(col, 2.6f), signC + side * ((k - 13.5f) * .48f) + up * y - face * .17f, Vector3.one * .08f, ctx.Rng, 0f);
            }
            ArtistBits.Label(ctx.Root, "THE ELECTRIC FIELDS", signC - face * .17f + up * .25f, -face, .7f, new Color(1f, .9f, .6f), TextAnchor.MiddleCenter);
            ArtistBits.Label(ctx.Root, "THE ELECTRIC FIELDS", signC + face * .17f + up * .25f, face, .7f, new Color(1f, .9f, .6f), TextAnchor.MiddleCenter);
            halos.Add(signC - face * 2.5f, 4f, Sunset, .8f); halos.Add(signC + face * 2.5f, 4f, Sunset, .8f);
            ArtistBits.PointLight(ctx.Root, signC - face * 3f, new Color(1f, .75f, .5f), 18f, 1.6f);
            // the welcome area: a ring of tall flags, a striped merch tent and a map of the fields
            for (var k = 0; k < 12; k++)
            {
                var a = k * Mathf.PI * 2f / 12f; var d = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                if (Vector3.Dot(d, face) > .7f || Vector3.Dot(d, face) < -.85f) continue;          // keep the way in and the way on open
                var g = ctx.OnGround(c.x + d.x * 15f, c.z + d.z * 15f);
                mk.Prism(wood, g - up * .3f, up, .08f, 7.3f, 6);
                ArtistBits.PostCollider(ctx.Root, g, 7f, .1f);
                var fc = WorldMaterials.Solid(Bunting[k % Bunting.Length]); var along = Vector3.Cross(up, d);
                soft.Quad(fc, g + up * 7f, g + up * 7f + along * 1.6f, g + up * 4.6f + along * 1.6f, g + up * 4.6f, g + d);
                soft.Quad(fc, g + up * 7f, g + up * 7f + along * 1.6f, g + up * 4.6f + along * 1.6f, g + up * 4.6f, g - d);
            }
            var merch = ctx.OnGround(c.x + side.x * 9f + face.x * 3f, c.z + side.z * 9f + face.z * 3f);
            for (var k = 0; k < 4; k++)
            {
                var a0 = Quaternion.Euler(0, k * 90f + 45f, 0) * face * 3.2f; var a1 = Quaternion.Euler(0, k * 90f + 135f, 0) * face * 3.2f;
                var m = WorldMaterials.Solid(k % 2 == 0 ? new Color(.95f, .3f, .4f) : Ink);
                mk.Tri(m, merch + a0 + up * 2.2f, merch + a1 + up * 2.2f, merch + up * 4.6f, merch + up * 2f);
                mk.Quad(m, merch + a0, merch + a1, merch + a1 + up * 2.2f, merch + a0 + up * 2.2f, merch + up);
            }
            solid.Box(canvas, merch + up * 1.1f, new Vector3(4.4f, 2.2f, 4.4f), Quaternion.LookRotation(face));
            mk.Blob(GlowM(new Color(1f, .85f, .55f), 2.4f), merch + up * 4.7f, Vector3.one * .25f, ctx.Rng, 0f);
            halos.Add(merch + up * 4.8f, 1.6f, new Color(1f, .85f, .55f), .8f);
            var map = ctx.OnGround(c.x - side.x * 7f + face.x * 4f, c.z - side.z * 7f + face.z * 4f);
            foreach (var s in new[] { -1f, 1f }) mk.Prism(wood, map + side * s * 1.2f, up, .07f, 2.4f, 5);
            mk.Box(WorldMaterials.Solid(new Color(.95f, .9f, .75f)), map + up * 1.7f, new Vector3(2.6f, 1.4f, .08f), Quaternion.LookRotation(-face));
            for (var k = 0; k < 9; k++) mk.Blob(WorldMaterials.Solid(Bunting[k % Bunting.Length]), map + up * (1.3f + (k % 3) * .3f) + side * ((k / 3) * .7f - .7f) - face * .06f, new Vector3(.12f, .12f, .02f), ctx.Rng, 0f);
            solid.Box(wood, map + up * 1.2f, new Vector3(2.6f, 2.4f, .3f), Quaternion.LookRotation(face));
            noGrass.Add(new Vector3(c.x, 9f, c.z));
            life.gate = c;
            return c + face * 6f;
        }

        // ---------------------------------------------------------------- walkways
        /// Walkways of trodden straw from the gate to every stage and tent, branching where each joins the
        /// network (so they never cross), with flag bunting and strings of festoon lights on poles along them.
        void Walkways(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 start)
        {
            var path = WorldMaterials.Solid(Trodden);
            Remember(start, 0);
            var todo = new List<IslandWorldData.Artist>(ctx.Artists);
            var walk = 1; var poles = new List<Vector3>();
            var bulbMats = new List<Material>();
            for (var i = 0; i < 5; i++) bulbMats.Add(new Material(GlowM(new Color(1f, .82f, .55f), 2.2f)) { name = "Festoon" });
            life.festoon = bulbMats.ToArray();
            while (todo.Count > 0)
            {
                IslandWorldData.Artist next = null; Vector3 join = default; var best = float.MaxValue;
                foreach (var a in todo)
                    foreach (var (p, _) in walkPoints)
                    {
                        var d = Vector2.Distance(new Vector2(p.x, p.z), new Vector2(a.x, a.z)) - Foot(a);
                        if (d < best) { best = d; next = a; join = p; }
                    }
                if (next == null) break;
                todo.Remove(next);
                var from = new Vector3(join.x, 0, join.z); var to = new Vector3(next.x, 0, next.z);
                var dir = to - from; var total = dir.magnitude; if (total < 2f) continue;
                dir /= total;
                facing[next.name] = -dir;
                var side = Vector3.Cross(Vector3.up, dir);
                var end = total - Foot(next) * .8f;
                var pts = new List<Vector3>();
                for (var d = walk == 1 ? 0f : PathW / 2f + .1f; ; d += 3f)
                {
                    var dd = Mathf.Min(d, end);
                    var q = from + dir * dd + side * Mathf.Sin(dd / Mathf.Max(end, 1f) * Mathf.PI) * Mathf.Min(5f, end * .05f);
                    if (!ctx.IsLand(q.x, q.z, 0f)) break;
                    pts.Add(ctx.OnGround(q.x, q.z));
                    if (dd >= end) break;
                }
                if (pts.Count < 2) continue;
                Strip(mk, pts, path, PathW, .05f);
                foreach (var p in pts) Remember(p, walk);
                // poles every 12 m alternating sides, bunting and festoon lights swagged between them
                Vector3? prevPole = null; var k = 0;
                for (var i = 2; i < pts.Count; i += 4, k++)
                {
                    var s = k % 2 == 0 ? 1f : -1f;
                    var pole = pts[i] + side * s * (PathW / 2f + .8f);
                    var near = false; foreach (var o in poles) if ((o - pole).sqrMagnitude < 25f) near = true;
                    if (near) { prevPole = null; continue; }
                    poles.Add(pole);
                    var g = ctx.OnGround(pole.x, pole.z);
                    mk.Prism(wood, g - Vector3.up * .3f, Vector3.up, .07f, 4.3f, 6);
                    ArtistBits.PostCollider(ctx.Root, g, 4f, .1f);
                    var top = g + Vector3.up * 3.9f;
                    if (prevPole.HasValue)
                    {
                        var a = prevPole.Value; var b = top;
                        var n = Mathf.Max(4, Mathf.RoundToInt(Vector3.Distance(a, b) / .9f));
                        var prev = a;
                        for (var j = 1; j <= n; j++)
                        {
                            var t = j / (float)n; var q = Vector3.Lerp(a, b, t) - Vector3.up * Mathf.Sin(t * Mathf.PI) * .7f;
                            soft.Box(rope, (prev + q) / 2f, new Vector3(.02f, .02f, Vector3.Distance(prev, q) + .01f), Quaternion.LookRotation(q - prev));
                            // bunting triangles hanging under the line, a bulb at every other one
                            var flagC = WorldMaterials.Solid(Bunting[(j + i) % Bunting.Length]);
                            var across = (b - a).normalized * .22f;
                            foreach (var inside in new[] { q + Vector3.Cross(across, Vector3.up), q - Vector3.Cross(across, Vector3.up) })
                                soft.Tri(flagC, q - across, q + across, q - Vector3.up * .45f, inside);
                            if (j % 2 == 0) mk.Blob(bulbMats[(j / 2) % bulbMats.Count], q - Vector3.up * .08f + across * 2f, Vector3.one * .07f, ctx.Rng, 0f);
                            if (j % 6 == 0) halos.Add(q - Vector3.up * .1f, .8f, new Color(1f, .82f, .55f), .7f);
                            prev = q;
                        }
                    }
                    prevPole = top;
                    if (k % 4 == 1) ArtistBits.PointLight(ctx.Root, top - Vector3.up * .5f, new Color(1f, .8f, .55f), 10f, .9f);
                }
                walk++;
            }
        }

        /// A walkway laid on the ground as one seamless strip, mitred at the bends.
        void Strip(MeshKit mk, List<Vector3> pts, Material m, float w, float lift)
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
            }
            for (var i = 0; i + 1 < n; i++) mk.Quad(m, L[i], R[i], R[i + 1], L[i + 1], (pts[i] + pts[i + 1]) / 2f - Vector3.up);
        }

        // ---------------------------------------------------------------- ground cover
        /// Long festival grass with clover and wildflowers, trodden flat near the walkways.
        void GroundCover(LandContext ctx)
        {
            var grassMat = WindMesh.Material(.35f, 1.1f, Grass);
            var meshes = new Dictionary<(int, int), WindMesh>();
            var rng = new System.Random(4321);
            const float step = 1.8f;
            Color[] flowers = { new Color(1f, .9f, .3f), new Color(1f, 1f, 1f), new Color(.95f, .45f, .6f), new Color(.6f, .55f, 1f) };
            for (var x = ctx.MinX; x < ctx.MaxX; x += step)
            for (var z = ctx.MinZ; z < ctx.MaxZ; z += step)
            {
                var px = x + ((float)rng.NextDouble() - .5f) * step; var pz = z + ((float)rng.NextDouble() - .5f) * step;
                if (!ctx.IsLand(px, pz, 1f) || NearPath(px, pz, 2.4f) || InNoGrass(px, pz, 0f) || InFootprint(ctx, px, pz, 0f, .7f)) continue;
                var n = Mathf.PerlinNoise(px * .02f + 1f, pz * .02f + 5f);
                if (n < .3f) continue;
                var key = (Mathf.FloorToInt(px / 70f), Mathf.FloorToInt(pz / 70f));
                if (!meshes.TryGetValue(key, out var wm)) meshes[key] = wm = new WindMesh();
                var g = ctx.Ground(px, pz);
                var col = Color.Lerp(new Color(.36f, .55f, .26f), new Color(.6f, .66f, .32f), (float)rng.NextDouble());
                var at = new Vector3(px, g - .04f, pz);
                wm.Tuft(at, .35f + (float)rng.NextDouble() * .4f, col, Color.Lerp(col, new Color(1f, .85f, .5f), .35f), rng, 4, .07f);
                if (rng.NextDouble() < .1) wm.Flower(at + new Vector3(.2f, 0, .1f), .3f, flowers[rng.Next(flowers.Length)], rng);
            }
            foreach (var kv in meshes) kv.Value.Build("Festival grass", ctx.Root, grassMat);
        }
    }
}
