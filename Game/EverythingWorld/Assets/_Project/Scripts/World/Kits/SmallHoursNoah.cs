using System.Collections.Generic;
using UnityEngine;

namespace EverythingWorld.World
{
    /// Noah Gallagher's place in the Small Hours, built with him. The cream bungalow with the screened porch where
    /// the first album was made, now his studio (his koa Languedoc on a stand, both album covers on the wall), in a
    /// yard of dry winter grass under huge bare trees, with a swing set, a birdbath and white goose ornaments.
    /// Behind it, the night forest from the cover of Akashic Sanctuary ("Doctrine Factorum"): dark twisted trunks,
    /// teal light falling between them, purple and green orbs in the branches, a huge yellow moon with a magenta
    /// halo. A path winds through it past a light for each of that album's eight songs to a clearing with a still
    /// pool and the Ferris wheel from The Twelfth House: twelve gondolas, one per sign and per song, round a
    /// yin-yang hub, under a band of aurora. Whichever of his songs is playing, its light or gondola glows.
    /// His artist station covers the whole place and plays his whole discography (build/station_extra.json).
    public partial class SmallHoursKit
    {
        const string NoahName = "Noah Gallagher";
        float noahR = 74f;                 // how far his place reaches: set by where it finds room (ShapeGround)
        Vector3 noah; bool hasNoah;

        static readonly string[] Signs = { "Aries", "Taurus", "Gemini", "Cancer", "Leo", "Virgo", "Libra", "Scorpio", "Sagittarius", "Capricorn", "Aquarius", "Pisces" };
        static readonly (string id, string title)[] TwelfthHouse =
        {
            ("0CSTVF9JRuEeT0hwHsDRNx", "The Ordinary World"), ("14iXrL4jwEXfBIR8EIKZIM", "Something More"), ("4LkHchfKRBuW55IQzTbV8j", "Breaking Through"),
            ("5Msl12Sdj5NT8LypjFGn12", "A Moment's Notice"), ("7fszFjzdGzdk1IPNhvLVWO", "Ley Lines"), ("5bw73xSn4iNlqpSM6WP7pd", "The Gauntlet"),
            ("4mLqyjfkoyOthYp1bqeynr", "Silicon Sanctum"), ("5H9b2nTNE7gkqvtVcvt06y", "The Scarab Song"), ("77vazqn7VFtUgFCONyI7IU", "End Of K'atun"),
            ("6R8nrIvhYbsXocAo0kZqch", "Whiteout"), ("3u4wx9RKR34yIhHfCSskKO", "The Synchronous World"), ("6IylgJDbSuyorKKx3G67V6", "Water"),
        };
        static readonly (string id, string title)[] Akashic =
        {
            ("02CgTke4nnUacp8WN7NusB", "Desolate Zenith"), ("44bDZRYh3MZVK3jZojqSQC", "Periapsis"), ("2nT2BdTJS6G3IWed8KI9cd", "Doctrine Factorum"),
            ("2QeK4n0f2xQLLmtRZierWM", "Bohr's Reverie"), ("3XCkTxCVPiEj9qsMDh4nSk", "Ashes of Corinth"), ("2g5TUSiI5YPWvBL5upUWM3", "Formula 0"),
            ("3SuTKgORWHIIMh616JowUY", "Prometheus Resolute"), ("3BRXvXtk509gQn1psZLv3b", "Blind Retrospect"),
        };
        static readonly Color Teal = new Color(.2f, .9f, .78f), Orchid = new Color(.72f, .32f, 1f), Lime = new Color(.45f, 1f, .55f), Moon = new Color(1f, .76f, .08f), Magenta = new Color(1f, .25f, .75f);

        bool InNoah(Vector3 p, float pad) => hasNoah && (new Vector2(p.x, p.z) - new Vector2(noah.x, noah.z)).magnitude < noahR + pad;

        /// His area covers the whole forest (so his station plays throughout), and the ground there is evened out
        /// and dipped for the pool.
        public override void ShapeGround(IslandWorldData data, int index)
        {
            hasNoah = false;
            var isl = data.layout.islands[index]; var sid = isl.sceneId;
            IslandWorldData.Artist mine = null;
            foreach (var a in data.layout.artists) if (a.sceneId == sid && a.name == NoahName) mine = a;
            if (mine == null) return;
            // his place needs room for the house, the forest and the wheel: find the widest open part of the
            // Small Hours (well inside its own land, clear of the hamlet and the way in) and move his area there
            bool Here(float x, float z)
            {
                var cc = Mathf.RoundToInt(x / data.Step); var rr = Mathf.RoundToInt((data.layout.depth - z) / data.Step);
                return cc >= 0 && rr >= 0 && cc < data.Nx && rr < data.Nz && data.islandIds[rr * data.Nx + cc] == index && data.heights[rr * data.Nx + cc] > 1.5f;
            }
            var bestR = 0f; var best = new Vector3(mine.x, 0, mine.z);
            for (var r = 0; r < data.Nz; r += 4)
            for (var c = 0; c < data.Nx; c += 4)
            {
                if (data.islandIds[r * data.Nx + c] != index) continue;
                float x = c * data.Step, z = data.layout.depth - r * data.Step;
                var room = 130f;
                foreach (var a in data.layout.artists)
                    if (a.sceneId == sid && a != mine) room = Mathf.Min(room, Vector2.Distance(new Vector2(x, z), new Vector2(a.x, a.z)) - a.r - 12f);
                room = Mathf.Min(room, Vector2.Distance(new Vector2(x, z), new Vector2(isl.labelX, isl.labelZ)) - 30f);
                if (room <= bestR) continue;
                // how far the land reaches round this point
                var edge = 0f;
                for (var d = 20f; d <= room + 8f; d += 8f)
                {
                    var all = true;
                    for (var k = 0; k < 16 && all; k++) { var ang = k * Mathf.PI / 8f; all = Here(x + Mathf.Cos(ang) * d, z + Mathf.Sin(ang) * d); }
                    if (!all) break;
                    edge = d;
                }
                var reach = Mathf.Min(room, edge - 8f);
                if (reach > bestR) { bestR = reach; best = new Vector3(x, 0, z); }
            }
            if (bestR > 45f) { mine.x = best.x; mine.z = best.z; noahR = Mathf.Min(bestR, 115f); }
            else noahR = 60f;
            mine.r = noahR;
            noah = new Vector3(mine.x, 0, mine.z); hasNoah = true;
            Debug.Log($"The Small Hours: Noah Gallagher's place at {mine.x:0},{mine.z:0}, reaching {noahR:0} m");

            // spread the other homes out over the rest of the hollow: each in turn goes to the open spot farthest from
            // every home already placed, your place and the way in, staying well inside the land and 48 m or more
            // from any other land (so no home sits on the border, hearing the next land's music)
            bool FarFromOtherLands(float x, float z)
            {
                for (var d = 5f; d <= 48f; d += 4f)
                    for (var k = 0; k < 32; k++)
                    {
                        var ang = k * Mathf.PI / 16f;
                        var cc = Mathf.RoundToInt((x + Mathf.Cos(ang) * d) / data.Step); var rr = Mathf.RoundToInt((data.layout.depth - z - Mathf.Sin(ang) * d) / data.Step);
                        if (cc < 0 || rr < 0 || cc >= data.Nx || rr >= data.Nz) continue;
                        var j = rr * data.Nx + cc;
                        if (data.islandIds[j] != index && data.islandIds[j] != 255 && data.heights[j] > 1.5f) return false;
                    }
                return true;
            }
            var cands = new List<Vector3>();
            for (var r = 0; r < data.Nz; r += 3)
            for (var c = 0; c < data.Nx; c += 3)
            {
                if (data.islandIds[r * data.Nx + c] != index) continue;
                float x = c * data.Step, z = data.layout.depth - r * data.Step;
                var inland = true;
                for (var k = 0; k < 12 && inland; k++) { var ang = k * Mathf.PI / 6f; inland = Here(x + Mathf.Cos(ang) * 26f, z + Mathf.Sin(ang) * 26f); }
                if (inland && FarFromOtherLands(x, z)) cands.Add(new Vector3(x, 0, z));
            }
            var taken = new List<Vector3> { new Vector3(noah.x, noahR + 18f, noah.z), new Vector3(isl.labelX, 45f, isl.labelZ) };   // x, radius, z
            var homes = new List<IslandWorldData.Artist>();
            foreach (var a in data.layout.artists) if (a.sceneId == sid && a != mine) homes.Add(a);
            homes.Sort((p, q) => string.CompareOrdinal(p.name, q.name));
            foreach (var a in homes)
            {
                var bestD = float.MinValue; var at = new Vector3(a.x, 0, a.z);
                foreach (var cand in cands)
                {
                    var d = float.MaxValue;
                    foreach (var t in taken) d = Mathf.Min(d, Vector2.Distance(new Vector2(cand.x, cand.z), new Vector2(t.x, t.z)) - t.y);
                    if (d > bestD) { bestD = d; at = cand; }
                }
                if (bestD < a.r + 6f) continue;            // nowhere roomier than where it already is
                a.x = at.x; a.z = at.z;
                taken.Add(new Vector3(at.x, a.r + 6f, at.z));
            }
            // soften the ground under the whole place to its average, easing back out
            float sum = 0; var n = 0;
            for (var k = 0; k < 24; k++) { var ang = k * Mathf.PI / 12f; sum += data.HeightAt(noah.x + Mathf.Cos(ang) * 30f, noah.z + Mathf.Sin(ang) * 30f); n++; }
            var level = Mathf.Max(3f, sum / n);
            noah.y = level;
            var step = data.Step;
            for (var r = 0; r < data.Nz; r++)
            {
                var z = data.layout.depth - r * step;
                if (Mathf.Abs(z - noah.z) > noahR + 10f) continue;
                for (var c = 0; c < data.Nx; c++)
                {
                    var x = c * step;
                    if (Mathf.Abs(x - noah.x) > noahR + 10f) continue;
                    var i = r * data.Nx + c;
                    if (data.islandIds[i] != index) continue;
                    var d = Vector2.Distance(new Vector2(x, z), new Vector2(noah.x, noah.z));
                    var keep = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(noahR - 25f, noahR + 10f, d));
                    data.heights[i] = Mathf.Lerp(Mathf.Lerp(level, data.heights[i], .25f), data.heights[i], keep);
                }
            }
        }

        // ---------------------------------------------------------------- the whole place
        void NoahPlace(LandContext ctx, IslandWorldData.Artist a)
        {
            var root = new GameObject("Artist: " + a.name).transform; root.SetParent(ctx.Root, false);
            var F = facing.TryGetValue(a.name, out var f) ? f : new Vector3(center.x - a.x, 0, center.z - a.z).normalized;
            F.y = 0; F.Normalize();
            var away = -F; var side = Vector3.Cross(Vector3.up, F);
            var N = new Vector3(a.x, 0, a.z);
            var mk = new MeshKit(); var solid = new MeshKit(); var pictures = new MeshKit(); var forest = new MeshKit();
            var labels = new GameObject("Labels").transform; labels.SetParent(root, false);
            var area = root.gameObject.AddComponent<ArtistArea>();
            area.artistName = a.name; area.stationId = a.stationId; area.radius = a.r; area.glowColor = WindowGlow;
            var glows = new List<Renderer>();
            var houseAt = N + F * 4f;
            House(ctx, root, mk, solid, pictures, labels, a, houseAt, F, glows);
            Yard(ctx, root, mk, solid, N, F, side);
            var clearing = N + away * (noahR * .55f);
            var path = SongPath(ctx, N, away, side, clearing);
            Forest(ctx, root, forest, N, F, clearing, path);
            SongLights(ctx, root, mk, labels, path);
            AuroraRibbons(ctx, root, N, clearing);
            Clearing(ctx, root, mk, solid, labels, clearing, F, side);
            BuildMoon(ctx, root, mk, N + away * (noahR * .3f));
            ArtistBits.Plaque(mk, solid, labels, a, ctx.OnGround((houseAt + F * 10f + side * 4.5f).x, (houseAt + F * 10f + side * 4.5f).z), F, darkWood, board, Ink, 1.5f);
            foreach (var g in glows) if (g != null) halos.Add(g.transform.position, 1.2f, WindowGlow, .8f);
            area.glowRenderers = glows.ToArray();
            mk.Build("Home", root, false);
            forest.Build("Forest", root, false);
            solid.BuildCollider("Walls", root);
            pictures.Build("Covers", root, false);
        }

        // ---------------------------------------------------------------- the house
        void House(LandContext ctx, Transform root, MeshKit mk, MeshKit solid, MeshKit pictures, Transform labels, IslandWorldData.Artist a, Vector3 at, Vector3 front, List<Renderer> glows)
        {
            const float W = 11f, D = 9f, H = 3.6f, t = .3f, porchD = 3.2f;
            var rot = Quaternion.LookRotation(front);
            var side = Vector3.Cross(Vector3.up, front);
            var (lo, hi) = Footprint(ctx, at + front * porchD / 2f, rot, W, D + porchD);
            var fy = hi + .6f;
            var flat = new Vector3(at.x, 0, at.z);
            var up = Vector3.up;
            var siding = WorldMaterials.Solid(new Color(.86f, .83f, .7f));
            var trim = WorldMaterials.Solid(new Color(.95f, .94f, .9f));
            var shutter = WorldMaterials.Solid(new Color(.16f, .26f, .2f));
            var roof = WorldMaterials.Solid(new Color(.26f, .23f, .24f));
            var brick = WorldMaterials.Solid(new Color(.55f, .25f, .2f));
            var floorMat = WorldMaterials.Solid(new Color(.42f, .32f, .26f));
            mk.Box(WorldMaterials.Solid(new Color(.4f, .38f, .36f)), new Vector3(at.x, (lo + fy) / 2f - .2f, at.z) + front * porchD / 2f, new Vector3(W + .3f, fy - lo + .5f, D + porchD + .3f), rot);
            mk.Box(floorMat, flat + up * (fy - .05f), new Vector3(W, .1f, D), rot);
            solid.Box(floorMat, flat + up * (fy - .25f), new Vector3(W, .5f, D + porchD * 2f), rot);
            void Wall(Vector3 p, Vector3 s) { mk.Box(siding, p, s, rot); solid.Box(siding, p, s, rot); }
            Wall(flat - front * (D / 2f - t / 2f) + up * (fy + H / 2f), new Vector3(W, H, t));
            foreach (var s in new[] { -1f, 1f })
            {
                Wall(flat + side * s * (W / 2f - t / 2f) + up * (fy + H / 2f), new Vector3(t, H, D - 2f * t));
                // a window with dark green shutters on each side wall, lit
                var win = flat + side * s * (W / 2f + .02f) + up * (fy + 1.8f) - front * 1.5f;
                glows.Add(ArtistBits.GlassBox(root, win, new Vector3(.06f, 1.3f, 1f), WindowGlow, rot));
                foreach (var k in new[] { -1f, 1f }) mk.Box(shutter, win + front * k * .8f + side * s * .03f, new Vector3(.06f, 1.4f, .5f), rot);
            }
            // siding lines
            for (var y = .3f; y < H; y += .3f)
                foreach (var s in new[] { -1f, 1f }) mk.Box(trim, flat + side * s * (W / 2f + .005f) + up * (fy + y), new Vector3(.02f, .02f, D), rot);
            // the front wall with a door into the studio, and the screened porch across the front
            var fz = flat + front * (D / 2f - t / 2f);
            var door = HumanScale.DoorWidth; var pier = (W - door) / 2f;
            foreach (var s in new[] { -1f, 1f }) Wall(fz + side * s * (door / 2f + pier / 2f) + up * (fy + H / 2f), new Vector3(pier, H, t));
            Wall(fz + up * (fy + HumanScale.DoorHeight + (H - HumanScale.DoorHeight) / 2f), new Vector3(door, Mathf.Max(.2f, H - HumanScale.DoorHeight), t));
            var porch = flat + front * (D / 2f + porchD / 2f);
            mk.Box(floorMat, porch + up * (fy - .05f), new Vector3(W, .1f, porchD), rot);
            var pf = flat + front * (D / 2f + porchD);
            // the porch front: a knee wall and screens either side of an open doorway, all solid but the way in
            var screen = WorldMaterials.Solid(new Color(.15f, .17f, .2f), .2f);
            var gap = door + .2f;
            foreach (var s in new[] { -1f, 1f })
            {
                var span = (W - gap) / 2f;
                var mid = pf + side * s * (gap / 2f + span / 2f);
                mk.Box(siding, mid + up * (fy + .45f), new Vector3(span, .9f, .2f), rot);              // the knee wall
                solid.Box(siding, mid + up * (fy + H / 2f), new Vector3(span, H, .25f), rot);          // screens are solid
                var panels = 3;
                for (var k = 0; k < panels; k++)
                {
                    var x = gap / 2f + (k + .5f) * span / panels;
                    mk.Box(screen, pf + side * s * x + up * (fy + .9f + (H - .9f) / 2f), new Vector3(span / panels - .12f, H - 1f, .03f), rot);
                }
                for (var k = 0; k <= panels; k++)
                    mk.Box(trim, pf + side * s * (gap / 2f + k * span / panels) + up * (fy + H / 2f), new Vector3(.12f, H, .14f), rot);
                // the side walls of the porch
                Wall(pf - front * porchD / 2f + side * s * (W / 2f - .1f) + up * (fy + H / 2f), new Vector3(.2f, H, porchD));
            }
            mk.Box(trim, pf + up * (fy + H - .1f), new Vector3(W, .2f, .14f), rot);                    // the header over the screens
            // the screen door, swung open on its hinge so the way in is plain
            var hinge = pf + side * (gap / 2f - .05f) + up * fy;
            var swung = Quaternion.LookRotation(front) * Quaternion.Euler(0, 105f, 0);
            var leaf = hinge + swung * new Vector3(-.5f, 0, 0);
            mk.Box(trim, leaf + up * 1.1f, new Vector3(1f, 2.2f, .06f), swung);
            mk.Box(screen, leaf + up * 1.1f + swung * new Vector3(0, 0, .01f), new Vector3(.8f, 1.9f, .02f), swung);
            var roofEave = flat + front * porchD / 2f + up * (fy + H);
            mk.GableRoof(roof, roofEave, new Vector3(D + porchD + 1f, 0, W + 1f), 3.4f, Quaternion.LookRotation(side));
            // the front gable with a small dormer window, and the red brick chimney
            var gable = pf + front * .5f + up * (fy + H);
            mk.Tri(siding, gable - side * (W / 2f + .3f), gable + side * (W / 2f + .3f), gable + up * 3.3f, gable - front);
            glows.Add(ArtistBits.GlassBox(root, gable + up * 1.3f + front * .05f, new Vector3(.8f, .9f, .05f), WindowGlow, rot));
            mk.Box(trim, gable + up * 1.3f + front * .04f, new Vector3(1f, 1.1f, .03f), rot);
            mk.Box(brick, flat - front * 1.5f + side * 2.2f + up * (fy + H + 2.8f), new Vector3(.9f, 2.6f, .9f), rot);
            var steps = pf + front * .8f; steps.y = (fy + ctx.Ground(steps.x, steps.z)) / 2f;
            mk.Box(WorldMaterials.Solid(new Color(.5f, .48f, .45f)), steps, new Vector3(door + .4f, fy - ctx.Ground(steps.x, steps.z) + .2f, 1.4f), rot);
            solid.Box(floorMat, steps, new Vector3(door + .4f, fy - ctx.Ground(steps.x, steps.z) + .2f, 1.4f), rot);
            IndoorSpaces.Add(flat + front * porchD / 2f + up * (fy + H / 2f), rot, new Vector3(W, H, D + porchD));

            // inside, the studio: desk, monitors, laptop, interface, microphone, amp, the guitar, both covers
            var desk = flat - front * (D / 2f - 1f) + side * 2f + up * fy;
            mk.Box(wood, desk + up * .76f, new Vector3(3.4f, .08f, 1.1f), rot);
            solid.Box(wood, desk + up * .4f, new Vector3(3.4f, .8f, 1.1f), rot);
            foreach (var s in new[] { -1f, 1f })
            {
                mk.Box(board, desk + side * s * 1.25f + up * 1.05f, new Vector3(.32f, .5f, .3f), rot);   // monitor speakers
                mk.Prism(WorldMaterials.Solid(new Color(.3f, .3f, .32f)), desk + side * s * 1.25f + up * 1.1f + front * .155f, front, .09f, .01f, 10);
            }
            mk.Box(WorldMaterials.Glow(new Color(.55f, .7f, 1f), 1.4f), desk + up * 1.25f - front * .2f, new Vector3(1.3f, .7f, .04f), rot);   // the screen
            mk.Box(WorldMaterials.Solid(new Color(.75f, .18f, .16f)), desk + up * .85f + front * .25f + side * .7f, new Vector3(.32f, .08f, .2f), rot);   // the interface
            mk.Box(board, desk + up * .82f + front * .25f - side * .2f, new Vector3(.9f, .03f, .3f), rot);   // keyboard
            var mic = flat - front * 1.6f + side * .3f + up * fy;
            mk.Prism(iron, mic, up, .03f, 1.6f, 6); mk.Box(iron, mic + up * .02f, new Vector3(.5f, .04f, .5f), rot);
            ArtistBits.PostCollider(root, mic, 1.7f, .2f);
            mk.Box(WorldMaterials.Solid(new Color(.62f, .62f, .64f), .6f), mic + up * 1.7f, new Vector3(.12f, .25f, .12f), rot);
            var amp = flat - front * (D / 2f - .8f) - side * 1.5f + up * fy;
            mk.Box(board, amp + up * .4f, new Vector3(.8f, .8f, .45f), rot);
            mk.Box(WorldMaterials.Solid(new Color(.75f, .7f, .6f)), amp + up * .5f + front * .23f, new Vector3(.66f, .5f, .02f), rot);
            solid.Box(board, amp + up * .4f, new Vector3(.8f, .8f, .45f), rot);
            mk.Box(WorldMaterials.Solid(new Color(.3f, .24f, .34f)), flat + up * (fy + .02f) - front * .5f, new Vector3(4.6f, .02f, 3.2f), rot);
            Languedoc(mk, flat - front * (D / 2f - 1.1f) - side * 3.6f + up * fy, front);
            ArtistBits.PostCollider(root, flat - front * (D / 2f - 1.1f) - side * 3.6f + up * fy, 1.3f, .35f);
            // both album covers, large, on the side walls, lit
            var covers = new List<int>();
            foreach (var s in a.songList ?? new IslandWorldData.Song[0]) if (s.cover >= 0 && !covers.Contains(s.cover)) covers.Add(s.cover);
            var pic = WorldMaterials.Picture(ctx.CoverAtlas);
            for (var i = 0; i < covers.Count && i < 2; i++)
            {
                var sgn = i == 0 ? -1f : 1f;
                var n = -side * sgn; var r2 = Vector3.Cross(n, Vector3.up);
                var c = flat + side * sgn * (W / 2f - t - .05f) + up * (fy + 2f) + front * 1f;
                mk.Box(board, c - n * .02f, new Vector3(.06f, 1.9f, 1.9f), rot);
                pictures.Picture(pic, c + n * .02f, r2 * .85f, up * .85f, n, ctx.CoverRect(covers[i]));
                ArtistBits.PointLight(root, c + n * 1.5f + up * .8f, WindowGlow, 4f, .9f, true);
            }
            ArtistBits.CoverWall(pictures, labels, ctx, a, flat - front * (D / 2f - t - .03f) - side * 1.2f + up * (fy + 2.5f), front, 3, .6f, 3, true, Ink);
            ArtistBits.PointLight(root, flat + up * (fy + H - .5f), WindowGlow, 11f, 1.4f, true);
            var porchLight = pf + front * .12f + up * (fy + 2.9f) + side * 1f;
            glows.Add(ArtistBits.GlassBox(root, porchLight, new Vector3(.25f, .3f, .2f), WindowGlow, rot));
        }

        /// His koa Languedoc semi-hollow on a floor stand: pointed horns, two long f-holes, two humbuckers in cream
        /// rings, an ebony bridge and hourglass tailpiece, an ebony fretboard with bar inlays, a scalloped headstock.
        void Languedoc(MeshKit mk, Vector3 ground, Vector3 front)
        {
            var side = Vector3.Cross(Vector3.up, front);
            // the guitar leans back on the stand; local x = across, y = up the neck, z = out of the top
            var lean = Quaternion.LookRotation(front) * Quaternion.Euler(-12f, 0, 0);
            var o = ground + Vector3.up * .32f;
            Vector3 P(float x, float y, float z) => o + lean * new Vector3(-x, y, z) * 1.15f;
            var koa = WorldMaterials.Solid(new Color(.78f, .45f, .18f), .75f);
            var ebony = WorldMaterials.Solid(new Color(.07f, .06f, .06f), .5f);
            var cream = WorldMaterials.Solid(new Color(.92f, .88f, .76f));
            var chrome = WorldMaterials.Solid(new Color(.8f, .8f, .82f), .85f);
            Vector2[] outline =
            {
                new Vector2(0, 0), new Vector2(.1f, .01f), new Vector2(.16f, .05f), new Vector2(.185f, .12f), new Vector2(.175f, .2f), new Vector2(.145f, .27f),
                new Vector2(.135f, .31f), new Vector2(.15f, .36f), new Vector2(.155f, .41f), new Vector2(.14f, .445f), new Vector2(.1f, .405f), new Vector2(.06f, .375f),
                new Vector2(.03f, .38f), new Vector2(-.03f, .38f), new Vector2(-.065f, .4f), new Vector2(-.11f, .45f), new Vector2(-.15f, .5f), new Vector2(-.165f, .445f),
                new Vector2(-.15f, .38f), new Vector2(-.135f, .31f), new Vector2(-.145f, .27f), new Vector2(-.175f, .2f), new Vector2(-.185f, .12f), new Vector2(-.16f, .05f), new Vector2(-.1f, .01f),
            };
            const float th = .045f;
            var mid = new Vector2(0, .24f);
            for (var i = 0; i < outline.Length; i++)
            {
                var a = outline[i]; var b = outline[(i + 1) % outline.Length];
                mk.Tri(koa, P(mid.x, mid.y, th), P(a.x, a.y, th), P(b.x, b.y, th), P(mid.x, mid.y, 0));
                mk.Tri(koa, P(mid.x, mid.y, -th), P(a.x, a.y, -th), P(b.x, b.y, -th), P(mid.x, mid.y, 0));
                mk.Quad(koa, P(a.x, a.y, th), P(b.x, b.y, th), P(b.x, b.y, -th), P(a.x, a.y, -th), P(mid.x, mid.y, 0));
                // cream binding round the top edge
                var e0 = P(a.x, a.y, th + .002f); var e1 = P(b.x, b.y, th + .002f); var d = e1 - e0;
                if (d.sqrMagnitude > 1e-6f) mk.Box(cream, (e0 + e1) / 2f, new Vector3(.006f, .006f, d.magnitude), Quaternion.LookRotation(d, lean * Vector3.forward));
            }
            void Bar(Material m, float x0, float y0, float x1, float y1, float w, float z)
            {
                var p0 = P(x0, y0, z); var p1 = P(x1, y1, z); var d = p1 - p0;
                if (d.sqrMagnitude < 1e-8f) return;
                mk.Box(m, (p0 + p1) / 2f, new Vector3(w, w * .5f, d.magnitude + w * .5f), Quaternion.LookRotation(d, lean * Vector3.forward));
            }
            // f-holes: long S curves on both sides
            foreach (var s in new[] { -1f, 1f })
            {
                Vector2[] f = { new Vector2(.09f, .08f), new Vector2(.1f, .1f), new Vector2(.105f, .16f), new Vector2(.11f, .24f), new Vector2(.12f, .29f), new Vector2(.13f, .3f) };
                for (var k = 0; k + 1 < f.Length; k++) Bar(ebony, s * f[k].x, f[k].y, s * f[k + 1].x, f[k + 1].y, .012f, th + .001f);
            }
            // pickups in cream rings, the bridge, the hourglass tailpiece, knobs
            foreach (var y in new[] { .33f, .2f })
            {
                mk.Box(cream, P(0, y, th + .003f), new Vector3(.1f, .058f, .006f) * 1.15f, lean);
                mk.Box(chrome, P(0, y, th + .008f), new Vector3(.08f, .04f, .01f) * 1.15f, lean);
            }
            mk.Box(ebony, P(0, .13f, th + .008f), new Vector3(.11f, .018f, .016f) * 1.15f, lean);
            mk.Box(ebony, P(0, .065f, th + .006f), new Vector3(.05f, .07f, .012f) * 1.15f, lean);
            mk.Box(ebony, P(0, .02f, th + .006f), new Vector3(.085f, .04f, .012f) * 1.15f, lean);
            foreach (var (x, y) in new[] { (.1f, .17f), (.11f, .085f) }) mk.Prism(ebony, P(-x, y, th), lean * Vector3.forward, .012f, .02f, 10);
            // the neck: ebony fretboard with frets and bar inlays, then the scalloped black headstock with tuners
            Bar(koa, 0, .37f, 0, .84f, .05f, th - .02f);
            Bar(ebony, 0, .37f, 0, .82f, .048f, th + .005f);
            for (var k = 1; k <= 20; k++)
            {
                var y = .82f - .45f * (1f - Mathf.Pow(2f, -k / 12f)) / (1f - Mathf.Pow(2f, -20f / 12f));
                Bar(chrome, -.024f, y, .024f, y, .003f, th + .012f);
                if (k == 3 || k == 5 || k == 7 || k == 9 || k == 12 || k == 15 || k == 17) Bar(cream, -.012f, y + .008f, .012f, y + .008f, .005f, th + .013f);
            }
            for (var k = 0; k < 6; k++) Bar(chrome, (k - 2.5f) * .007f, .03f, (k - 2.5f) * .0075f, .83f, .0015f, th + .016f);
            Vector2[] head = { new Vector2(-.028f, .82f), new Vector2(.028f, .82f), new Vector2(.045f, .87f), new Vector2(.035f, .9f), new Vector2(.05f, .93f), new Vector2(.04f, .97f), new Vector2(-.01f, .975f), new Vector2(-.045f, .95f), new Vector2(-.035f, .91f), new Vector2(-.05f, .88f) };
            var hm = new Vector2(0, .9f);
            for (var i = 0; i < head.Length; i++)
            {
                var a = head[i]; var b = head[(i + 1) % head.Length];
                mk.Tri(ebony, P(hm.x, hm.y, .01f), P(a.x, a.y, .01f), P(b.x, b.y, .01f), P(hm.x, hm.y, -.02f));
                mk.Tri(ebony, P(hm.x, hm.y, -.01f), P(a.x, a.y, -.01f), P(b.x, b.y, -.01f), P(hm.x, hm.y, .02f));
            }
            for (var k = 0; k < 3; k++)
                foreach (var s in new[] { -1f, 1f })
                    mk.Box(ebony, P(s * .06f, .86f + k * .035f, -.005f), new Vector3(.03f, .02f, .02f) * 1.15f, lean);
            // the stand
            var iron2 = WorldMaterials.Solid(new Color(.12f, .12f, .14f));
            foreach (var s in new[] { -1f, 1f }) mk.Box(iron2, ground + side * s * .14f + Vector3.up * .15f + front * .08f, new Vector3(.03f, .3f, .3f), Quaternion.LookRotation(front));
            mk.Box(iron2, P(0, .7f, -.08f), new Vector3(.03f, .03f, .25f), lean);
            mk.Box(iron2, ground - front * .15f + Vector3.up * .45f, new Vector3(.03f, .9f, .03f), Quaternion.LookRotation(front) * Quaternion.Euler(-12f, 0, 0));
        }

        // ---------------------------------------------------------------- the yard
        void Yard(LandContext ctx, Transform root, MeshKit mk, MeshKit solid, Vector3 N, Vector3 F, Vector3 side)
        {
            Vector3 G(Vector3 p) => ctx.OnGround(p.x, p.z);
            var dirt = WorldMaterials.Solid(new Color(.42f, .35f, .28f));
            // a worn dirt drive curving up to the porch from the lane
            var drive = new List<Vector3>();
            for (var t = 0f; t <= 1f; t += .05f) { var p = N + F * Mathf.Lerp(noahR * .36f, 13f, t) + side * Mathf.Sin(t * Mathf.PI) * 4f; drive.Add(G(p)); }
            Ribbon(ctx, mk, dirt, drive, 3f, .06f);
            foreach (var p in drive) Remember(p);
            // the swing set
            var swing = N + F * 13f + side * 8f;
            var white = WorldMaterials.Solid(new Color(.8f, .8f, .8f), .4f);
            foreach (var s in new[] { -1f, 1f })
                foreach (var k in new[] { -1f, 1f })
                {
                    var foot = G(swing + side * s * 2.2f + F * k * 1f); var top = G(swing + side * s * 2.2f) + Vector3.up * 2.6f; var d = top - foot;
                    mk.Box(white, (foot + top) / 2f, new Vector3(.07f, d.magnitude, .07f), Quaternion.LookRotation(side, d.normalized));
                }
            foreach (var s in new[] { -1f, 1f }) foreach (var k in new[] { -1f, 1f }) ArtistBits.PostCollider(root, G(swing + side * s * 2.2f + F * k * .8f), 2.6f, .15f);
            var bar = G(swing) + Vector3.up * 2.6f;
            mk.Box(white, bar, new Vector3(4.6f, .08f, .08f), Quaternion.LookRotation(F));
            foreach (var s in new[] { -.8f, .8f })
            {
                foreach (var k in new[] { -.25f, .25f }) mk.Box(iron, bar + side * (s + k) - Vector3.up * .95f, new Vector3(.02f, 1.9f, .02f), Quaternion.identity);
                solid.Box(iron, bar + side * s - Vector3.up * 1.3f, new Vector3(.7f, 1.3f, .4f), Quaternion.LookRotation(F));
                mk.Box(WorldMaterials.Solid(new Color(.3f, .32f, .38f)), bar + side * s - Vector3.up * 1.9f, new Vector3(.6f, .05f, .3f), Quaternion.LookRotation(F));
            }
            // the birdbath and the white goose ornaments
            var bath = G(N + F * 16f - side * 6f);
            mk.Prism(WorldMaterials.Solid(new Color(.75f, .74f, .7f)), bath, Vector3.up, .12f, .8f, 8);
            mk.Prism(WorldMaterials.Solid(new Color(.75f, .74f, .7f)), bath + Vector3.up * .8f, Vector3.up, .45f, .12f, 12, 1.1f);
            ArtistBits.PostCollider(root, bath, 1f, .5f);
            var goose = WorldMaterials.Solid(new Color(.95f, .95f, .93f));
            for (var i = 0; i < 4; i++)
            {
                var g = G(N + F * (12.5f + (i % 2) * .8f) + side * (-3f + i * 1.8f));
                var look = Quaternion.Euler(0, i * 50f, 0) * F;
                mk.Blob(goose, g + Vector3.up * .2f, new Vector3(.18f, .16f, .26f), ctx.Rng, .05f);
                ArtistBits.PostCollider(root, g, .65f, .25f);
                mk.Box(goose, g + Vector3.up * .42f + look * .18f, new Vector3(.05f, .3f, .05f), Quaternion.LookRotation(look));
                mk.Blob(goose, g + Vector3.up * .58f + look * .22f, new Vector3(.06f, .06f, .08f), ctx.Rng, .05f);
                mk.Box(WorldMaterials.Solid(new Color(.95f, .6f, .2f)), g + Vector3.up * .58f + look * .31f, new Vector3(.03f, .03f, .07f), Quaternion.LookRotation(look));
            }
            // huge bare winter trees arching over the house
            foreach (var (fw, sd) in new[] { (8f, -12f), (2f, 13f), (18f, 15f), (-6f, -16f) })
            {
                var p = G(N + F * fw + side * sd);
                BareTree(ctx, mk, root, p, ctx.Range(1f, 1.3f));
            }
            // dry winter grass round the house
            var grass = new WindMesh(); var rng = new System.Random(77);
            for (var i = 0; i < 1400; i++)
            {
                var a = (float)rng.NextDouble() * Mathf.PI * 2f; var r = Mathf.Sqrt((float)rng.NextDouble()) * 24f;
                var p = N + F * 10f + new Vector3(Mathf.Cos(a) * r, 0, Mathf.Sin(a) * r);
                if (NearPath(p.x, p.z, 1.8f)) continue;
                if ((new Vector2(p.x, p.z) - new Vector2(N.x + F.x * 4f, N.z + F.z * 4f)).magnitude < 9.5f) continue;
                var col = Color.Lerp(new Color(.48f, .4f, .3f), new Color(.62f, .52f, .36f), (float)rng.NextDouble());
                grass.Tuft(G(p) - Vector3.up * .03f, .2f + (float)rng.NextDouble() * .25f, col, Color.Lerp(col, new Color(.8f, .72f, .55f), .4f), rng, 3, .07f);
            }
            grass.Build("Winter grass", root, WindMesh.Material(.08f, .6f, Moss));
        }

        void BareTree(LandContext ctx, MeshKit mk, Transform root, Vector3 at, float scale)
        {
            var barkGrey = WorldMaterials.Solid(new Color(.3f, .28f, .27f));
            var h = 9f * scale;
            mk.Prism(barkGrey, at, Vector3.up, .7f * scale, h, 8, .6f);
            ArtistBits.PostCollider(root, at, h, .75f * scale);
            void Branch(Vector3 from, Vector3 dir, float len, float r, int depth)
            {
                mk.Prism(barkGrey, from, dir, r, len, 6, .65f);
                if (depth == 0) return;
                var end = from + dir * len;
                for (var k = 0; k < 3; k++)
                {
                    var nd = (Quaternion.AngleAxis(ctx.Range(-50f, 50f), Vector3.up) * Quaternion.AngleAxis(ctx.Range(15f, 40f), Vector3.Cross(dir, Vector3.up).normalized) * dir + Vector3.up * .25f).normalized;
                    Branch(end, nd, len * ctx.Range(.55f, .75f), r * .6f, depth - 1);
                }
            }
            for (var k = 0; k < 5; k++)
            {
                var a = k * Mathf.PI * 2f / 5f + ctx.Range(0, .5f);
                var dir = new Vector3(Mathf.Cos(a), ctx.Range(.5f, 1.1f), Mathf.Sin(a)).normalized;
                Branch(at + Vector3.up * h * ctx.Range(.55f, .85f), dir, 6f * scale, .32f * scale, 2);
            }
        }

        // ---------------------------------------------------------------- the forest and the song lights
        List<Vector3> SongPath(LandContext ctx, Vector3 N, Vector3 away, Vector3 side, Vector3 clearing)
        {
            var pts = new List<Vector3>();
            for (var t = 0f; t <= 1.0001f; t += .02f)
            {
                var p = Vector3.Lerp(N + away * 8f, clearing - away * 14f, t) + side * Mathf.Sin(t * Mathf.PI * 1.5f) * 13f;
                pts.Add(ctx.OnGround(p.x, p.z));
            }
            return pts;
        }

        void Forest(LandContext ctx, Transform root, MeshKit mk, Vector3 N, Vector3 F, Vector3 clearing, List<Vector3> path)
        {
            var trunk = WorldMaterials.Solid(new Color(.09f, .07f, .11f));
            var canopy = WorldMaterials.Solid(new Color(.06f, .1f, .12f));
            var shaft = WorldMaterials.Glow(Teal, 1.6f);
            var shaftB = WorldMaterials.Glow(new Color(.3f, 1f, .6f), 1.3f);
            var moss = WorldMaterials.Solid(new Color(.1f, .24f, .22f));
            var trodden = WorldMaterials.Solid(new Color(.16f, .22f, .24f));
            Ribbon(ctx, mk, trodden, path, 1.8f, .06f);
            foreach (var p in path) Remember(p);
            bool Clear(Vector3 p, float pad)
            {
                if ((new Vector2(p.x, p.z) - new Vector2(N.x + F.x * 10f, N.z + F.z * 10f)).magnitude < 24f + pad) return false;   // the house and yard
                if ((new Vector2(p.x, p.z) - new Vector2(clearing.x, clearing.z)).magnitude < 22f + pad) return false;
                if (NearPath(p.x, p.z, 2.5f + pad)) return false;
                return ctx.IsLand(p.x, p.z, 2f);
            }
            var shafts = new MeshKit();         // light falling between the trunks: walked through (the land audit skips "Soft")
            var trees = 0;
            var many = Mathf.RoundToInt(260f * (noahR / 74f) * (noahR / 74f));
            for (var i = 0; i < many; i++)
            {
                var a = ctx.Range(0, Mathf.PI * 2f); var r = Mathf.Sqrt(ctx.Range(0f, 1f)) * (noahR - 4f);
                var p = N + new Vector3(Mathf.Cos(a) * r, 0, Mathf.Sin(a) * r);
                if (!Clear(p, 1.5f)) continue;
                p = ctx.OnGround(p.x, p.z);
                trees++;
                // a dark twisted trunk: a few leaning segments, then bare reaching branches and a little dark canopy
                var h = ctx.Range(9f, 16f);
                var at = p; var dir = Vector3.up; var rad = ctx.Range(.35f, .6f);
                for (var s = 0; s < 4; s++)
                {
                    var len = h / 4f;
                    dir = (dir + new Vector3(ctx.Range(-.25f, .25f), 0, ctx.Range(-.25f, .25f))).normalized;
                    mk.Prism(trunk, at, dir, rad, len + .1f, 6, .85f);
                    at += dir * len; rad *= .85f;
                }
                for (var b = 0; b < 4; b++)
                {
                    var bd = (dir + new Vector3(ctx.Range(-1f, 1f), ctx.Range(-.1f, .5f), ctx.Range(-1f, 1f))).normalized;
                    var from = at - dir * ctx.Range(0f, h * .3f);
                    var bl = ctx.Range(2.5f, 5f);
                    mk.Prism(trunk, from, bd, rad * .6f, bl, 5, .4f);
                    if (ctx.Chance(.4f)) mk.Blob(canopy, from + bd * bl, new Vector3(ctx.Range(1.2f, 2.4f), ctx.Range(.8f, 1.4f), ctx.Range(1.2f, 2.4f)), ctx.Rng, .2f);
                }
                ArtistBits.PostCollider(root, p, h * .5f, rad + .25f);
                mk.Blob(moss, p + Vector3.up * .1f, new Vector3(1.2f, .25f, 1.2f), ctx.Rng, .3f);
                // teal light falling down between the trunks
                if (ctx.Chance(.55f))
                {
                    var sp = p + new Vector3(ctx.Range(-2.5f, 2.5f), 0, ctx.Range(-2.5f, 2.5f));
                    sp.y = ctx.Ground(sp.x, sp.z);
                    var sh = ctx.Range(5f, 13f);
                    shafts.Box(ctx.Chance(.6f) ? shaft : shaftB, sp + Vector3.up * sh / 2f, new Vector3(ctx.Range(.08f, .2f), sh, ctx.Range(.08f, .2f)), Quaternion.Euler(0, ctx.Range(0, 90f), 0));
                    halos.Add(sp + Vector3.up * sh * .3f, 1.4f, Teal, .4f);
                }
                // glowing orbs in the branches
                if (ctx.Chance(.35f))
                {
                    var col = ctx.Chance(.5f) ? Orchid : Lime;
                    var op = at + new Vector3(ctx.Range(-2f, 2f), ctx.Range(-2f, 1f), ctx.Range(-2f, 2f));
                    mk.Blob(WorldMaterials.Glow(col, 2.6f), op, Vector3.one * ctx.Range(.18f, .32f), ctx.Rng, .05f);
                    halos.Add(op, 1.6f, col, .9f);
                }
            }
            // teal-tipped ferns over the forest floor
            var ferns = new WindMesh(); var rng = new System.Random(919);
            for (var i = 0; i < many * 10; i++)
            {
                var a = (float)rng.NextDouble() * Mathf.PI * 2f; var r = Mathf.Sqrt((float)rng.NextDouble()) * (noahR - 2f);
                var p = N + new Vector3(Mathf.Cos(a) * r, 0, Mathf.Sin(a) * r);
                if (!Clear(p, -1.5f)) continue;
                var g = ctx.OnGround(p.x, p.z) - Vector3.up * .03f;
                if (rng.NextDouble() < .3) ferns.Bracken(g, .5f + (float)rng.NextDouble() * .4f, new Color(.08f, .18f, .2f), new Color(.2f, .7f, .62f), rng);
                else ferns.Tuft(g, .3f + (float)rng.NextDouble() * .3f, new Color(.08f, .16f, .18f), new Color(.16f, .5f, .46f), rng, 3, .07f);
            }
            ferns.Build("Forest floor", root, WindMesh.Material(.1f, .5f, Moss));
            shafts.Build("Soft light shafts", root, false);
            ArtistBits.PointLight(root, N + Vector3.up * (noah.y + 6f) - F * 30f, Teal, 45f, 1.1f);
            Debug.Log($"The Small Hours: Noah Gallagher's forest of {trees} trees");
        }

        /// The eight songs of Akashic Sanctuary along the path: a stone with the song's name and a light above it.
        void SongLights(LandContext ctx, Transform root, MeshKit mk, Transform labels, List<Vector3> path)
        {
            var lights = new Material[Akashic.Length];
            var stoneMat = WorldMaterials.Solid(new Color(.2f, .22f, .26f));
            Color[] tones = { Teal, Orchid, Moon, Lime, Teal, Orchid, Moon, Lime };
            for (var i = 0; i < Akashic.Length; i++)
            {
                var k = Mathf.Clamp(Mathf.RoundToInt((i + .6f) / (Akashic.Length + .4f) * (path.Count - 1)), 1, path.Count - 2);
                var along = (path[k + 1] - path[k - 1]); along.y = 0; along.Normalize();
                var side = Vector3.Cross(Vector3.up, along) * (i % 2 == 0 ? 1f : -1f);
                var at = path[k] + side * 2.6f; at = ctx.OnGround(at.x, at.z);
                var col = Akashic[i].title == "Periapsis" ? IndieDownsKit.SeanGreen * 1.6f : tones[i];
                mk.Prism(stoneMat, at, Vector3.up, .35f, 1.1f, 7, .8f);
                lights[i] = new Material(WorldMaterials.Glow(col, 1f)) { name = Akashic[i].title };
                mk.Blob(lights[i], at + Vector3.up * 1.7f, Vector3.one * .28f, ctx.Rng, .05f);
                halos.Add(at + Vector3.up * 1.7f, 1.4f, col, .6f);
                ArtistBits.Label(labels, Akashic[i].title, at + Vector3.up * 1.15f - side * .36f, -side, .16f, Color.Lerp(col, Color.white, .5f));
                ArtistBits.PostCollider(root, at, 1.1f, .4f);
            }
            life.songLights = lights;
            life.songLightColors = new Color[Akashic.Length];
            for (var i = 0; i < Akashic.Length; i++) life.songLightColors[i] = Akashic[i].title == "Periapsis" ? IndieDownsKit.SeanGreen * 1.6f : tones[i];
            life.songLightIds = new string[Akashic.Length];
            for (var i = 0; i < Akashic.Length; i++) life.songLightIds[i] = Akashic[i].id;
        }

        void BuildMoon(LandContext ctx, Transform root, MeshKit mk, Vector3 above)
        {
            var at = ctx.OnGround(above.x, above.z) + Vector3.up * 46f;
            var mat = new Material(WorldMaterials.Glow(Moon, 1.8f)) { name = "The moon" };
            mk.Blob(mat, at, Vector3.one * 7f, ctx.Rng, .03f);
            halos.Add(at, 26f, Magenta, 1f);
            halos.Add(at, 16f, new Color(1f, .7f, .05f), 1f);
            ArtistBits.PointLight(root, at - Vector3.up * 8f, new Color(1f, .75f, .55f), 80f, 1.6f);
            life.moon = mat;
        }

        // ---------------------------------------------------------------- the clearing: pool, wheel, aurora
        void Clearing(LandContext ctx, Transform root, MeshKit mk, MeshKit solid, Transform labels, Vector3 c, Vector3 F, Vector3 side)
        {
            var away = -F;
            var g = ctx.OnGround(c.x, c.z);
            // the still pool, ringed with stones
            var pool = WorldMaterials.Solid(new Color(.06f, .1f, .16f), .95f);
            mk.Prism(pool, g + Vector3.up * .04f, Vector3.up, 10f, .03f, 32);
            for (var k = 0; k < 28; k++)
            {
                var a = k * Mathf.PI * 2f / 28f;
                var p = g + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * 10.4f;
                p.y = Mathf.Max(p.y, ctx.Ground(p.x, p.z) - .1f);                                     // never sunk into a rise
                mk.Blob(stone, p, new Vector3(.6f, .35f, .5f), ctx.Rng, .2f);
                ArtistBits.PostCollider(root, new Vector3(p.x, Mathf.Min(p.y, ctx.Ground(p.x, p.z)), p.z), Mathf.Abs(p.y - ctx.Ground(p.x, p.z)) + .5f, .55f);
            }
            // the platform over the far side of the pool, and the wheel standing on it
            var deck = g + away * 7f;
            var planks = WorldMaterials.Solid(new Color(.36f, .28f, .24f));
            mk.Box(planks, deck + Vector3.up * .6f, new Vector3(16f, .3f, 6f), Quaternion.LookRotation(F));
            solid.Box(planks, deck + Vector3.up * .5f, new Vector3(16f, .5f, 6f), Quaternion.LookRotation(F));
            foreach (var s in new[] { -1f, 1f }) foreach (var k in new[] { -1f, 1f }) mk.Prism(darkWood, deck + side * s * 7.5f + F * k * 2.6f - Vector3.up * 1f, Vector3.up, .15f, 1.7f, 6);
            const float R = 13f;
            var hub = deck + Vector3.up * (R + 3.5f);
            var frame = WorldMaterials.Solid(new Color(.75f, .72f, .78f), .5f);
            foreach (var s in new[] { -1f, 1f })
                foreach (var k in new[] { -1f, 1f })
                {
                    var foot = deck + side * k * 6.5f + away * s * 2.6f + Vector3.up * .75f; var top = hub + away * s * 2.6f; var d = top - foot;
                    mk.Box(frame, (foot + top) / 2f, new Vector3(.35f, d.magnitude, .35f), Quaternion.LookRotation(F, d.normalized));
                }
            mk.Box(frame, hub, new Vector3(.4f, .4f, 5.6f), Quaternion.LookRotation(away));
            // the turning part: rim, spokes, lights and the yin-yang hub, built round its own centre
            var wheel = new GameObject("The Twelfth House wheel").transform; wheel.SetParent(root, false);
            wheel.SetPositionAndRotation(hub, Quaternion.LookRotation(F));
            var wk = new MeshKit();
            var rimLights = WorldMaterials.Glow(new Color(1f, .82f, .55f), 2.4f);
            for (var ring = 0; ring < 2; ring++)
            {
                var rr = ring == 0 ? R : R * .62f;
                for (var k = 0; k < 48; k++)
                {
                    float a0 = k * Mathf.PI * 2f / 48f, a1 = (k + 1) * Mathf.PI * 2f / 48f;
                    foreach (var zz in new[] { -.6f, .6f })
                    {
                        var p0 = new Vector3(Mathf.Cos(a0) * rr, Mathf.Sin(a0) * rr, zz); var p1 = new Vector3(Mathf.Cos(a1) * rr, Mathf.Sin(a1) * rr, zz);
                        wk.Box(frame, (p0 + p1) / 2f, new Vector3(.18f, .18f, Vector3.Distance(p0, p1) + .05f), Quaternion.LookRotation(p1 - p0, Vector3.forward));
                    }
                    if (ring == 0 && k % 2 == 0) wk.Blob(rimLights, new Vector3(Mathf.Cos(a0) * (rr + .2f), Mathf.Sin(a0) * (rr + .2f), .7f), Vector3.one * .14f, ctx.Rng, 0f);
                }
            }
            for (var k = 0; k < 12; k++)
            {
                var a = k * Mathf.PI * 2f / 12f;
                foreach (var zz in new[] { -.6f, .6f })
                {
                    var p1 = new Vector3(Mathf.Cos(a) * R, Mathf.Sin(a) * R, zz);
                    wk.Box(frame, p1 / 2f, new Vector3(.14f, .14f, R), Quaternion.LookRotation(p1, Vector3.forward));
                }
            }
            // the yin-yang hub on both faces: a blue disc, a green half, the two small circles and their dots
            var blue = WorldMaterials.Solid(new Color(.15f, .35f, .75f)); var green = WorldMaterials.Solid(new Color(.2f, .6f, .35f));
            foreach (var zz in new[] { -.75f, .75f })
            {
                var facingOut = new Vector3(0, 0, Mathf.Sign(zz));
                Disc(wk, blue, new Vector3(0, 0, zz), 3.2f, facingOut, 0f, Mathf.PI * 2f);
                Disc(wk, green, new Vector3(0, 0, zz + .02f * Mathf.Sign(zz)), 3.2f, facingOut, Mathf.PI * .5f, Mathf.PI * 1.5f);
                Disc(wk, green, new Vector3(0, 1.6f, zz + .04f * Mathf.Sign(zz)), 1.6f, facingOut, 0f, Mathf.PI * 2f);
                Disc(wk, blue, new Vector3(0, -1.6f, zz + .04f * Mathf.Sign(zz)), 1.6f, facingOut, 0f, Mathf.PI * 2f);
                Disc(wk, blue, new Vector3(0, 1.6f, zz + .06f * Mathf.Sign(zz)), .45f, facingOut, 0f, Mathf.PI * 2f);
                Disc(wk, green, new Vector3(0, -1.6f, zz + .06f * Mathf.Sign(zz)), .45f, facingOut, 0f, Mathf.PI * 2f);
            }
            wk.Build("Wheel", wheel, false).isStatic = false;
            // the twelve gondolas, one per sign and song, kept upright as the wheel turns
            var gondolas = new Transform[12]; var lamps = new Material[12];
            Color Element(int i) => (i % 4) switch { 0 => new Color(1f, .4f, .2f), 1 => new Color(.4f, .85f, .35f), 2 => new Color(1f, .85f, .3f), _ => new Color(.3f, .6f, 1f) };
            for (var i = 0; i < 12; i++)
            {
                var t = new GameObject("Gondola: " + Signs[i]).transform; t.SetParent(root, false);
                var gk = new MeshKit();
                var col = Element(i);
                gk.Box(WorldMaterials.Solid(Color.Lerp(col, new Color(.9f, .88f, .85f), .5f)), Vector3.down * 1.5f, new Vector3(2f, 1.6f, 1.6f), Quaternion.identity);
                gk.Box(WorldMaterials.Solid(new Color(.25f, .22f, .28f)), Vector3.down * .55f, new Vector3(2.3f, .2f, 1.9f), Quaternion.identity);
                gk.Box(frame, Vector3.down * .25f, new Vector3(.1f, .5f, .1f), Quaternion.identity);
                lamps[i] = new Material(WorldMaterials.Glow(col, .9f)) { name = Signs[i] };
                foreach (var zz in new[] { -.82f, .82f }) gk.Box(lamps[i], new Vector3(0, -1.45f, zz), new Vector3(1.5f, .9f, .02f), Quaternion.identity);
                gk.Build("Cabin", t, false).isStatic = false;
                foreach (var zz in new[] { -1f, 1f })
                {
                    var n = new Vector3(0, 0, zz);
                    var lab = ArtistBits.Label(t, Signs[i].ToUpperInvariant(), Vector3.zero, n, .22f, Color.white);
                    lab.transform.localPosition = new Vector3(0, -1.15f, zz * .84f); lab.transform.localRotation = Quaternion.LookRotation(-n);
                    var song = ArtistBits.Label(t, TwelfthHouse[i].title, Vector3.zero, n, .13f, Color.white);
                    song.transform.localPosition = new Vector3(0, -1.5f, zz * .84f); song.transform.localRotation = Quaternion.LookRotation(-n);
                }
                gondolas[i] = t;
            }
            life.wheel = wheel; life.gondolas = gondolas; life.gondolaLamps = lamps; life.wheelRadius = R;
            life.gondolaColors = new Color[12]; life.gondolaIds = new string[12];
            for (var i = 0; i < 12; i++) { life.gondolaColors[i] = Element(i); life.gondolaIds[i] = TwelfthHouse[i].id; }
            ArtistBits.PointLight(root, hub + F * 6f, new Color(1f, .82f, .6f), 40f, 1.4f);
            ArtistBits.Label(labels, "THE TWELFTH HOUSE", ctx.OnGround((g + F * 11f).x, (g + F * 11f).z) + Vector3.up * .6f, F, .3f, Ink);

        }

        static void Disc(MeshKit mk, Material m, Vector3 c, float r, Vector3 facingOut, float a0, float a1)
        {
            const int n = 28;
            for (var k = 0; k < n; k++)
            {
                float t0 = Mathf.Lerp(a0, a1, k / (float)n), t1 = Mathf.Lerp(a0, a1, (k + 1) / (float)n);
                mk.Tri(m, c, c + new Vector3(Mathf.Cos(t0) * r, Mathf.Sin(t0) * r, 0), c + new Vector3(Mathf.Cos(t1) * r, Mathf.Sin(t1) * r, 0), c - facingOut);
            }
        }

        /// Ribbons of aurora snaking through the forest between the trees, a little over head height: bright at their
        /// lower edge and fading upward, teal into green into violet. Bright stretches travel along each ribbon's path
        /// (Aurora Ribbon shader), so only part of it is lit at a time; it all brightens with the music (SmallHoursLife).
        void AuroraRibbons(LandContext ctx, Transform root, Vector3 N, Vector3 clearing)
        {
            var v = new List<Vector3>(); var col = new List<Color>(); var tri = new List<int>(); var uv = new List<Vector2>();
            for (var ribbon = 0; ribbon < 3; ribbon++)
            {
                const int n = 160;
                var start = ribbon * 2.1f; var span = Mathf.PI * 1.35f;
                var baseR = Mathf.Lerp(30f, noahR - 14f, (ribbon + .5f) / 3f);
                var first = true;
                for (var k = 0; k <= n; k++)
                {
                    var t = k / (float)n;
                    var a = start + t * span;
                    var r = baseR + Mathf.Sin(t * Mathf.PI * 5f + ribbon) * 9f;
                    var p = N + new Vector3(Mathf.Cos(a) * r, 0, Mathf.Sin(a) * r);
                    if (!ctx.IsLand(p.x, p.z, 0f)) { first = true; continue; }
                    var g = ctx.Ground(p.x, p.z);
                    var low = g + 3.2f + Mathf.Sin(t * Mathf.PI * 7f + ribbon * 2f) * 1.2f;
                    var high = low + 6f + Mathf.Sin(t * Mathf.PI * 3f) * 1.5f;
                    var hue = Mathf.Repeat(t * 2f + ribbon * .33f, 1f);
                    var c0 = hue < .5f ? Color.Lerp(new Color(.15f, 1f, .75f), new Color(.35f, 1f, .45f), hue * 2f) : Color.Lerp(new Color(.35f, 1f, .45f), new Color(.6f, .4f, 1f), (hue - .5f) * 2f);
                    var fade = Mathf.Clamp01(Mathf.Min(t, 1f - t) * 8f);
                    var along = t + ribbon * .37f;                    // where this point sits along its ribbon (for the travelling light)
                    v.Add(new Vector3(p.x, low, p.z)); col.Add(new Color(c0.r, c0.g, c0.b, .9f * fade)); uv.Add(new Vector2(along, 0f));
                    v.Add(new Vector3(p.x, high, p.z)); col.Add(new Color(c0.r * .6f, c0.g * .6f, c0.b, 0f)); uv.Add(new Vector2(along, 1f));
                    if (!first) { var b = v.Count - 4; tri.AddRange(new[] { b, b + 1, b + 3, b, b + 3, b + 2 }); }
                    first = false;
                }
            }
            var mesh = new Mesh { name = "Aurora ribbons", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            mesh.SetVertices(v); mesh.SetColors(col); mesh.SetUVs(0, uv); mesh.SetTriangles(tri, 0); mesh.RecalculateBounds();
            var bb = mesh.bounds; bb.Expand(6f); mesh.bounds = bb;
            var go = new GameObject("Aurora ribbons"); go.transform.SetParent(root, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var rend = go.AddComponent<MeshRenderer>();
            var travelling = Shader.Find("Everything World/Aurora Ribbon");
            var mat = new Material(travelling != null ? travelling : Shader.Find("Universal Render Pipeline/Particles/Unlit")) { name = "Aurora ribbons" };
            mat.SetColor("_BaseColor", Color.white);
            mat.SetFloat("_Surface", 1f); mat.SetFloat("_Blend", 2f);
            mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha); mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
            mat.SetFloat("_ZWrite", 0f); mat.SetFloat("_Cull", 0f);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            rend.sharedMaterial = mat;
            rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            life.auroraMaterial = mat;
        }

        /// A band of aurora hanging over the clearing, like the one above the wheel on the album cover.
        void Aurora(LandContext ctx, Transform root, Vector3 at, Vector3 along)
        {
            var v = new List<Vector3>(); var col = new List<Color>(); var tri = new List<int>();
            const int n = 40; const float width = 110f, height = 26f;
            for (var k = 0; k <= n; k++)
            {
                var t = k / (float)n;
                var x = (t - .5f) * width;
                var bend = Mathf.Sin(t * Mathf.PI * 2.3f) * 12f;
                var c0 = Color.Lerp(new Color(.2f, 1f, .6f), new Color(.3f, .7f, 1f), Mathf.Sin(t * 7f) * .5f + .5f);
                v.Add(new Vector3(x, 0, bend)); col.Add(new Color(c0.r, c0.g, c0.b, .55f * Mathf.Sin(t * Mathf.PI)));
                v.Add(new Vector3(x, height, bend + 4f)); col.Add(new Color(c0.r * .6f, c0.g * .5f, c0.b, 0f));
                if (k == 0) continue;
                var b = v.Count - 4;
                tri.AddRange(new[] { b, b + 1, b + 3, b, b + 3, b + 2 });
            }
            var mesh = new Mesh { name = "Aurora band" };
            mesh.SetVertices(v); mesh.SetColors(col); mesh.SetTriangles(tri, 0); mesh.RecalculateBounds();
            var go = new GameObject("Aurora band"); go.transform.SetParent(root, false);
            go.transform.SetPositionAndRotation(at, Quaternion.LookRotation(Vector3.Cross(along, Vector3.up)));
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>();
            var mat = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit")) { name = "Aurora band" };
            mat.SetColor("_BaseColor", new Color(1f, 1f, 1f, 1f));
            mat.SetFloat("_Surface", 1f); mat.SetFloat("_Blend", 2f);
            mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha); mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
            mat.SetFloat("_ZWrite", 0f); mat.SetFloat("_Cull", 0f);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            r.sharedMaterial = mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            life.aurora = go.transform;
        }
    }
}
