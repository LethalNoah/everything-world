using System.Collections.Generic;
using UnityEngine;

namespace EverythingWorld.World
{
    /// The Indie Downs (WORLD_GUIDE.md): a hazy late-summer afternoon you only half remember. Rolling chalk downs
    /// in faded colour, long grass that moves with the wind, hedgerows, a village green with bunting, and a giant
    /// guitar cut into a chalk hillside. Its weather swings with the music more than any other land (DownsLife).
    /// Two chalk paths cross it: one through the artists npgallagher added, one through Pyrosolstice's, each marked
    /// in that person's colour, meeting at the artists they both added. A quiet place stands apart
    /// (IndieDownsQuiet.cs); the artist areas are in IndieDownsArtists.cs.
    public partial class IndieDownsKit : LandKit
    {
        public override string Land => "The Indie Downs";

        static readonly Color Sage = new Color(.6f, .66f, .47f), Straw = new Color(.84f, .74f, .46f), ChalkColor = new Color(.92f, .91f, .86f);
        public static readonly Color NoahBlue = new Color(.05f, .45f, 1f), SeanGreen = new Color(.13f, .55f, .2f);
        /// Sean's lantern light: a deep green, so it still reads as green once it blooms.
        public static readonly Color SeanGlow = new Color(.02f, .38f, .08f);
        static readonly Color Ink = new Color(.16f, .14f, .12f), Lamp = new Color(1f, .8f, .55f), Cream = new Color(.95f, .92f, .84f);
        const string Noah = "npgallagher", Sean = "Pyrosolstice";
        static readonly Color[] Pastels =
        {
            new Color(.93f, .89f, .8f), new Color(.9f, .78f, .74f), new Color(.74f, .82f, .86f), new Color(.8f, .85f, .72f),
            new Color(.95f, .87f, .64f), new Color(.97f, .95f, .92f),
        };
        static readonly Color[] Doors = { new Color(.2f, .32f, .5f), new Color(.24f, .42f, .3f), new Color(.6f, .2f, .18f), new Color(.85f, .68f, .25f), new Color(.15f, .15f, .17f) };

        Material chalk, wood, darkWood, iron, board, cream, slate, terracotta, brick, glassDark, hedge, leafA, leafB, bark, stone, red;
        GlowHalos halos;
        WindMesh flags;                                                  // bunting (the long grass is built in Finish)
        readonly List<Vector3> noGrass = new List<Vector3>();           // x, radius, z
        readonly Dictionary<(int, int), List<Vector3>> pathCells = new Dictionary<(int, int), List<Vector3>>();
        readonly List<Vector3> noahPath = new List<Vector3>(), seanPath = new List<Vector3>();
        readonly Dictionary<string, Vector3> arrival = new Dictionary<string, Vector3>();   // artist -> the way their area faces
        readonly List<Vector3> kiteAnchors = new List<Vector3>();
        Vector3 hubGate;
        readonly List<(Vector3, Vector3)> innerSpurs = new List<(Vector3, Vector3)>();
        Material noahCap, seanCap;
        readonly List<Transform> sails = new List<Transform>();

        void MakeMaterials()
        {
            chalk = WorldMaterials.Solid(ChalkColor);
            wood = WorldMaterials.Solid(new Color(.62f, .5f, .36f));
            darkWood = WorldMaterials.Solid(new Color(.33f, .25f, .19f));
            iron = WorldMaterials.Solid(new Color(.2f, .21f, .22f));
            board = WorldMaterials.Solid(new Color(.18f, .16f, .15f));
            cream = WorldMaterials.Solid(Cream);
            slate = WorldMaterials.Solid(new Color(.38f, .4f, .44f));
            terracotta = WorldMaterials.Solid(new Color(.66f, .42f, .32f));
            brick = WorldMaterials.Solid(new Color(.64f, .44f, .37f));
            glassDark = WorldMaterials.Solid(new Color(.22f, .26f, .3f), .6f);
            hedge = WorldMaterials.Solid(new Color(.33f, .45f, .28f));
            leafA = WorldMaterials.Solid(new Color(.42f, .55f, .33f));
            leafB = WorldMaterials.Solid(new Color(.52f, .6f, .36f));
            bark = WorldMaterials.Solid(new Color(.38f, .32f, .27f));
            stone = WorldMaterials.Solid(new Color(.78f, .76f, .71f));
            red = WorldMaterials.Solid(new Color(.78f, .14f, .12f), .3f);
        }

        public override bool Ground(float height, float slope, float patch, out Color color)
        {
            // pale sage and straw, bleached a little paler up high; steep banks show the chalk underneath
            if (slope > 5f) color = Color.Lerp(new Color(.8f, .79f, .72f), ChalkColor, Mathf.InverseLerp(5f, 9f, slope));
            else color = Color.Lerp(Sage, Straw, Mathf.SmoothStep(0, 1, patch)) * (1f + Mathf.InverseLerp(5f, 45f, height) * .08f);
            color.a = 1f;
            return true;
        }

        // ---------------------------------------------------------------- dressing the land
        public override void Dress(LandContext ctx)
        {
            MakeMaterials();
            halos = new GlowHalos(); flags = new WindMesh();
            noGrass.Clear(); pathCells.Clear(); noahPath.Clear(); seanPath.Clear(); arrival.Clear(); kiteAnchors.Clear();
            var mk = new MeshKit(); var solid = new MeshKit(); var pictures = new MeshKit();
            var labels = new GameObject("Downs words").transform; labels.SetParent(ctx.Root, false);
            if (hasQuiet) { ctx.AddKeepout(quiet.x, quiet.z, QuietFlat + 4f); noGrass.Add(new Vector3(quiet.x, 16.5f, quiet.z)); }
            var otherGate = Gates(ctx);
            noahCap = new Material(WorldMaterials.Glow(NoahBlue, 2f)) { name = "npgallagher's path" };
            seanCap = new Material(WorldMaterials.Glow(SeanGlow, 1.5f)) { name = "Pyrosolstice's path" };
            TwoPaths(ctx, mk, otherGate);
            Landmarks(ctx, mk, solid);
            Green(ctx, mk, solid, pictures);
            ChalkGuitar(ctx, mk);
            Hedgerows(ctx, mk, solid);
            LoneTrees(ctx, mk);
            Copses(ctx, mk);
            Sheep(ctx, mk, solid);
            HayBales(ctx, mk, solid);
            if (hasQuiet) QuietPlace(ctx, mk, solid);
            KiteSpots(ctx);
            mk.Build("Downs scenery", ctx.Root, false);
            solid.BuildCollider("Downs walls", ctx.Root);
            pictures.Build("Downs posters", ctx.Root, false);
            var life = new GameObject("Downs life").AddComponent<DownsLife>();
            life.transform.SetParent(ctx.Root, false);
            life.center = ctx.Plaza; life.kiteAnchors = kiteAnchors.ToArray();
            life.noahMarker = noahCap; life.seanMarker = seanCap; life.sails = sails.ToArray();
            var adders = ctx.Data.layout.hubAdders ?? new string[0];
            foreach (var t in ctx.Data.layout.hubTimeline ?? new IslandWorldData.TimelineSong[0])
                if (t.a >= 0 && t.a < adders.Length) life.adderOf[t.id] = adders[t.a];
        }

        public override void Finish(LandContext ctx)
        {
            Grass(ctx);
            flags.Build("Bunting", ctx.Root, WindMaterial(.35f, 1.6f));
            halos.Build("Downs glow", ctx.Root);
        }

        /// The bridge ends on this land: returns the far one (the hub's is kept in hubGate).
        Vector3 Gates(LandContext ctx)
        {
            var sid = ctx.Island.sceneId;
            hubGate = ctx.Plaza; var other = ctx.Plaza; var gotOther = false;
            foreach (var b in ctx.Data.layout.bridges)
            {
                if (b.a != sid && b.b != sid) continue;
                var end = b.a == sid ? new Vector3(b.ax, 0, b.az) : new Vector3(b.bx, 0, b.bz);
                var to = b.a == sid ? b.b : b.a;
                if (to == 1000) hubGate = end;
                else if (!gotOther) { other = end; gotOther = true; }
            }
            return other;
        }

        // ---------------------------------------------------------------- the two paths
        static bool By(IslandWorldData.Artist a, string who) => !string.IsNullOrEmpty(a.addedBy) && a.addedBy.Contains(who);

        void TwoPaths(LandContext ctx, MeshKit mk, Vector3 otherGate)
        {
            var noah = new List<IslandWorldData.Artist>(); var sean = new List<IslandWorldData.Artist>(); var nobody = new List<IslandWorldData.Artist>();
            foreach (var a in ctx.Artists)
            {
                bool n = By(a, Noah), s = By(a, Sean);
                if (n) noah.Add(a);
                if (s) sean.Add(a);
                if (!n && !s) nobody.Add(a);
            }
            Walk(ctx, mk, hubGate, noah, NoahBlue, .06f, noahPath);
            Walk(ctx, mk, otherGate, sean, SeanGreen, .1f, seanPath);
            // the village green, and anyone on neither path, join the nearest path with a plain spur
            foreach (var (from, to) in innerSpurs) { var r = new List<Vector3> { from }; Leg(r, from, to); Lay(ctx, mk, r, .08f, null, null); }
            innerSpurs.Clear();
            Spur(ctx, mk, ctx.Plaza, 24f, null);
            foreach (var a in nobody) Spur(ctx, mk, ctx.OnGround(a.x, a.z), a.r * .8f, a.name);
        }

        /// A path from 'start' through every stop (nearest first), going round each area to the side it leaves by.
        void Walk(LandContext ctx, MeshKit mk, Vector3 start, List<IslandWorldData.Artist> stops, Color color, float lift, List<Vector3> keep)
        {
            if (stops.Count == 0) return;
            var route = new List<Vector3> { new Vector3(start.x, 0, start.z) };
            var at = route[0];
            var todo = new List<IslandWorldData.Artist>(stops);
            while (todo.Count > 0)
            {
                IslandWorldData.Artist a = null; var best = float.MaxValue;
                foreach (var s in todo)
                {
                    var d = Vector3.Distance(at, new Vector3(s.x, 0, s.z)) - s.r;
                    if (d < best) { best = d; a = s; }
                }
                todo.Remove(a);
                var c = new Vector3(a.x, 0, a.z); var R = a.r + 3f;
                var entryDir = at - c; entryDir.y = 0;
                entryDir = entryDir.sqrMagnitude > .01f ? entryDir.normalized : Vector3.forward;
                Leg(route, at, c + entryDir * R);
                innerSpurs.Add((c + entryDir * R, c + entryDir * a.r * .55f));
                if (!arrival.ContainsKey(a.name)) arrival[a.name] = entryDir;
                IslandWorldData.Artist next = null; best = float.MaxValue;
                foreach (var s in todo)
                {
                    var d = Vector3.Distance(c, new Vector3(s.x, 0, s.z));
                    if (d < best) { best = d; next = s; }
                }
                if (next == null) { at = c + entryDir * R; break; }
                var exitDir = new Vector3(next.x, 0, next.z) - c; exitDir.Normalize();
                Arc(route, c, R, entryDir, exitDir);
                at = c + exitDir * R;
            }
            Lay(ctx, mk, route, lift, color, keep);
        }

        static void Leg(List<Vector3> route, Vector3 a, Vector3 b)
        {
            var d = b - a; var len = d.magnitude;
            if (len < .5f) return;
            var side = Vector3.Cross(Vector3.up, d / len);
            var bow = Mathf.Min(6f, len * .08f) * (((int)(a.x + b.z) & 1) == 0 ? 1f : -1f);   // a gentle curve, not a ruler line
            for (var s = 3f; s < len; s += 3f) { var t = s / len; route.Add(a + d * t + side * Mathf.Sin(t * Mathf.PI) * bow); }
            route.Add(b);
        }

        static void Arc(List<Vector3> route, Vector3 c, float r, Vector3 from, Vector3 to)
        {
            var a0 = Mathf.Atan2(from.z, from.x); var a1 = Mathf.Atan2(to.z, to.x);
            var da = Mathf.DeltaAngle(a0 * Mathf.Rad2Deg, a1 * Mathf.Rad2Deg) * Mathf.Deg2Rad;
            var n = Mathf.Max(1, Mathf.CeilToInt(Mathf.Abs(da) * r / 3f));
            for (var i = 1; i <= n; i++) { var a = a0 + da * i / n; route.Add(c + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * r); }
        }

        /// Lays a route on the ground as chalk, skipping water, with waymarker posts in the path's colour.
        void Lay(LandContext ctx, MeshKit mk, List<Vector3> route, float lift, Color? color, List<Vector3> keep)
        {
            var run = new List<Vector3>();
            var cap = color.HasValue ? (color.Value == NoahBlue ? noahCap : seanCap) : null;
            var band = color.HasValue ? WorldMaterials.Solid(color.Value) : null;
            float walked = 0, nextMark = 14f; var markSide = 1f;
            void Flush() { if (run.Count > 1) Ribbon(ctx, mk, chalk, run, color.HasValue ? 2.2f : 1.8f, lift); run.Clear(); }
            for (var i = 0; i < route.Count; i++)
            {
                var p = route[i];
                if (hasQuiet)
                {
                    // paths bend round the quiet place, never through it
                    var away = new Vector3(p.x - quiet.x, 0, p.z - quiet.z);
                    if (away.magnitude < QuietFlat - 2f) { p = new Vector3(quiet.x, 0, quiet.z) + (away.sqrMagnitude > .01f ? away.normalized : Vector3.right) * (QuietFlat - 2f); route[i] = p; }
                }
                if (i > 0) walked += Vector3.Distance(route[i - 1], p);
                if (!ctx.IsLand(p.x, p.z, 0f)) { Flush(); continue; }
                var g = ctx.OnGround(p.x, p.z);
                run.Add(g); keep?.Add(g);
                var key = (Mathf.FloorToInt(p.x / 8f), Mathf.FloorToInt(p.z / 8f));
                if (!pathCells.TryGetValue(key, out var cell)) pathCells[key] = cell = new List<Vector3>();
                cell.Add(g);
                if (cap == null || walked < nextMark || i + 1 >= route.Count) continue;
                nextMark = walked + 34f;
                var along = route[i + 1] - p; along.y = 0;
                if (along.sqrMagnitude < .01f) continue;
                var post = p + Vector3.Cross(Vector3.up, along.normalized) * 1.8f * markSide;
                markSide = -markSide;
                if (!ctx.IsFree(post.x, post.z, 0f)) continue;        // not inside an artist's area
                post = ctx.OnGround(post.x, post.z);
                mk.Prism(darkWood, post, Vector3.up, .08f, 1.25f, 6);
                mk.Box(band, post + Vector3.up * .95f, new Vector3(.2f, .14f, .2f), Quaternion.identity);
                mk.Box(cap, post + Vector3.up * 1.42f, new Vector3(.3f, .34f, .3f), Quaternion.identity);
                mk.Box(darkWood, post + Vector3.up * 1.62f, new Vector3(.38f, .06f, .38f), Quaternion.identity);
                halos.Add(post + Vector3.up * 1.42f, .9f, color.Value == SeanGreen ? SeanGlow : color.Value, .9f);
                ArtistBits.PostCollider(ctx.Root, post, 1.25f, .12f);
            }
            Flush();
        }

        void Spur(LandContext ctx, MeshKit mk, Vector3 from, float radius, string artist)
        {
            var best = float.MaxValue; var join = Vector3.zero;
            foreach (var list in new[] { noahPath, seanPath })
                foreach (var p in list)
                {
                    var d = Vector2.Distance(new Vector2(p.x, p.z), new Vector2(from.x, from.z));
                    if (d < best) { best = d; join = p; }
                }
            if (best == float.MaxValue || best < radius) return;
            var dir = new Vector3(join.x - from.x, 0, join.z - from.z).normalized;
            if (artist != null && !arrival.ContainsKey(artist)) arrival[artist] = dir;
            var route = new List<Vector3> { new Vector3(join.x, 0, join.z) };
            Leg(route, route[0], new Vector3(from.x, 0, from.z) + dir * radius);
            Lay(ctx, mk, route, .08f, null, null);
        }

        bool NearPath(float x, float z, float within)
        {
            int cx = Mathf.FloorToInt(x / 8f), cz = Mathf.FloorToInt(z / 8f);
            var w2 = within * within;
            for (var dx = -1; dx <= 1; dx++)
            for (var dz = -1; dz <= 1; dz++)
            {
                if (!pathCells.TryGetValue((cx + dx, cz + dz), out var cell)) continue;
                foreach (var p in cell) { float ex = p.x - x, ez = p.z - z; if (ex * ex + ez * ez < w2) return true; }
            }
            return false;
        }

        bool InNoGrass(float x, float z, float pad)
        {
            foreach (var c in noGrass) { float dx = x - c.x, dz = z - c.z, r = c.y + pad; if (dx * dx + dz * dz < r * r) return true; }
            return false;
        }

        /// Open ground for scenery: on the land, outside every area, off the paths and away from the special places.
        bool Open(LandContext ctx, float x, float z, float pad) =>
            ctx.IsLand(x, z, 3f) && ctx.IsFree(x, z, pad) && !NearPath(x, z, pad + 1.5f) && !InNoGrass(x, z, pad);

        /// A flat strip laid along a path, just above the ground.
        static void Ribbon(LandContext ctx, MeshKit mk, Material m, List<Vector3> pts, float width, float lift)
        {
            for (var i = 0; i + 1 < pts.Count; i++)
            {
                var dir = pts[i + 1] - pts[i]; dir.y = 0;
                if (dir.sqrMagnitude < .01f) continue;
                var side = Vector3.Cross(Vector3.up, dir.normalized) * width / 2f;
                Vector3 E(Vector3 p, Vector3 s) { var q = p + s; q.y = Mathf.Max(ctx.Ground(q.x, q.z), ctx.Ground(p.x, p.z)) + lift; return q; }
                Vector3 a = E(pts[i], -side), b = E(pts[i], side), c = E(pts[i + 1], side), d = E(pts[i + 1], -side);
                mk.Quad(m, a, b, c, d, (a + c) / 2f - Vector3.up);
            }
        }

        /// The fingerpost stands where the two paths split apart: one arm along each, in each person's colour.
        void Fingerpost(LandContext ctx, MeshKit mk, Transform labels)
        {
            if (noahPath.Count < 10 || seanPath.Count < 10) return;
            float Nearest(List<Vector3> list, Vector3 p)
            {
                var best = float.MaxValue;
                foreach (var q in list) { var d = (new Vector2(q.x, q.z) - new Vector2(p.x, p.z)).sqrMagnitude; if (d < best) best = d; }
                return Mathf.Sqrt(best);
            }
            // a point on the blue path that still runs with the green one, just before the two part
            var forkIndex = -1; var bestScore = float.MaxValue; Vector3 noahDir = default;
            for (var i = 0; i < noahPath.Count; i++)
            {
                if (Nearest(seanPath, noahPath[i]) > 3.5f) continue;
                foreach (var step in new[] { 7, -7 })
                {
                    var j = i + step;
                    if (j < 0 || j >= noahPath.Count || Nearest(seanPath, noahPath[j]) < 10f) continue;
                    var score = Vector3.Distance(noahPath[i], ctx.Plaza) * .1f;      // any fork will do; prefer central ones
                    if (score < bestScore) { bestScore = score; forkIndex = i; noahDir = noahPath[j] - noahPath[i]; }
                }
            }
            if (forkIndex < 0) { Debug.Log("The Indie Downs: the paths never split, no fingerpost"); return; }
            var fork = noahPath[forkIndex];
            // the green path's way out of the same fork
            var g = 0; var gd = float.MaxValue;
            for (var i = 0; i < seanPath.Count; i++) { var d = Vector3.Distance(seanPath[i], fork); if (d < gd) { gd = d; g = i; } }
            Vector3 seanDir = default;
            foreach (var step in new[] { 7, -7 })
            {
                var j = g + step;
                if (j >= 0 && j < seanPath.Count && Nearest(noahPath, seanPath[j]) >= 10f) { seanDir = seanPath[j] - seanPath[g]; break; }
            }
            noahDir.y = 0; seanDir.y = 0;
            var both = noahDir.normalized + seanDir.normalized;
            var off = both.sqrMagnitude > .01f ? -both.normalized : Vector3.Cross(Vector3.up, noahDir.normalized);
            var post = ctx.OnGround(fork.x + off.x * 3.2f, fork.z + off.z * 3.2f);       // beside the fork, not in the way
            mk.Prism(darkWood, post, Vector3.up, .14f, 3.8f, 8);
            mk.Box(darkWood, post + Vector3.up * 3.85f, new Vector3(.36f, .1f, .36f), Quaternion.identity);
            ArtistBits.PostCollider(ctx.Root, post, 3.8f, .2f);
            var k = 0;
            foreach (var (dir0, color, who) in new[] { (noahDir, NoahBlue, Noah), (seanDir, SeanGreen, Sean) })
            {
                if (dir0.sqrMagnitude < .01f) continue;
                var dir = dir0.normalized;
                var y = 3.3f - k * .6f;
                var arm = post + Vector3.up * y + dir * 1.05f;
                var rot = Quaternion.LookRotation(dir);
                mk.Box(WorldMaterials.Solid(color), arm, new Vector3(.08f, .42f, 2f), rot);
                mk.Box(cream, arm + dir * 1.05f, new Vector3(.09f, .3f, .3f), rot * Quaternion.Euler(45, 0, 0));
                var s = Vector3.Cross(Vector3.up, dir);
                ArtistBits.Label(labels, who, arm + s * .05f + Vector3.up * .1f, s, .15f, Cream);
                ArtistBits.Label(labels, who, arm - s * .05f + Vector3.up * .1f, -s, .15f, Cream);
                k++;
            }
            Debug.Log($"The Indie Downs: fingerpost where the paths split at {post.x:0},{post.z:0}");
        }

        // ---------------------------------------------------------------- tall landmarks
        /// A few tall things to find your way by: a white windmill whose sails turn with the song's wind, a flint
        /// church with a spire, a brick folly tower on a hilltop, and rows of tall poplars along field edges.
        void Landmarks(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var taken = new List<Vector3>();
            bool Spot(float pad, out Vector3 best)
            {
                best = default; var bestH = float.MinValue;
                for (var i = 0; i < 80; i++)
                {
                    if (!ctx.TrySpot(out var p, 20f, pad, 1) || !Open(ctx, p.x, p.z, pad)) continue;
                    var far = true;
                    foreach (var t in taken) if (Vector3.Distance(t, p) < 220f) { far = false; break; }
                    if (!far) continue;
                    var flat = Mathf.Abs(ctx.Ground(p.x + 6f, p.z) - ctx.Ground(p.x - 6f, p.z)) + Mathf.Abs(ctx.Ground(p.x, p.z + 6f) - ctx.Ground(p.x, p.z - 6f));
                    var h = p.y - flat * 3f;
                    if (h > bestH) { bestH = h; best = p; }
                }
                if (bestH == float.MinValue) return false;
                taken.Add(best);
                noGrass.Add(new Vector3(best.x, pad, best.z));
                return true;
            }
            if (Spot(10f, out var mill)) Windmill(ctx, mk, solid, mill);
            if (Spot(16f, out var church)) Church(ctx, mk, solid, church);
            if (Spot(8f, out var folly)) Folly(ctx, mk, solid, folly);
            for (var r = 0; r < 2; r++) if (Spot(6f, out var row)) Poplars(ctx, mk, row);
        }

        void Windmill(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 at)
        {
            var face = new Vector3(ctx.Plaza.x - at.x, 0, ctx.Plaza.z - at.z).normalized;
            if (face.sqrMagnitude < .1f) face = Vector3.forward;
            at.y = Footprint(ctx, at, Quaternion.identity, 7f, 7f).lo;
            mk.Prism(cream, at - Vector3.up * .5f, Vector3.up, 3.4f, 13.5f, 12, .62f);
            solid.Prism(cream, at - Vector3.up * .5f, Vector3.up, 3.4f, 13.5f, 8, .62f);
            var top = at + Vector3.up * 13f;
            mk.Cone(slate, top, 2.6f, 3f, 12);
            mk.Box(darkWood, at + face * 3.3f + Vector3.up * 1.2f, new Vector3(1.4f, 2.4f, .2f), Quaternion.LookRotation(face));
            foreach (var y in new[] { 5f, 9f })
                mk.Box(glassDark, at + face * (3.4f - y * .1f) + Vector3.up * y, new Vector3(.8f, 1f, .15f), Quaternion.LookRotation(face));
            // the sails, on their own so DownsLife can turn them
            var hub = new GameObject("Windmill sails").transform;
            hub.SetParent(ctx.Root, false);
            hub.SetPositionAndRotation(top + face * 4.2f + Vector3.up * .8f, Quaternion.LookRotation(face));
            var sk = new MeshKit();
            sk.Prism(darkWood, Vector3.back * 2.2f, Vector3.forward, .35f, 2.7f, 8);
            for (var k = 0; k < 4; k++)
            {
                var r = Quaternion.Euler(0, 0, k * 90f);
                sk.Box(darkWood, r * new Vector3(0, 4.6f, 0), new Vector3(.22f, 9.2f, .18f), r);
                sk.Box(cream, r * new Vector3(.75f, 5.4f, .05f), new Vector3(1.3f, 7f, .05f), r);
                for (var b = 0; b < 6; b++) sk.Box(darkWood, r * new Vector3(.75f, 2.4f + b * 1.2f, .09f), new Vector3(1.4f, .06f, .05f), r);
            }
            var go = sk.Build("Sails", hub, false); go.isStatic = false;
            sails.Add(hub);
        }

        void Church(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 at)
        {
            var along = Quaternion.Euler(0, ctx.Range(0, 360f), 0) * Vector3.forward;
            var side = Vector3.Cross(Vector3.up, along);
            var rot = Quaternion.LookRotation(along);
            var (lo, hi) = Footprint(ctx, at - along * 2.5f, rot, 9f, 27f);
            var flint = WorldMaterials.Solid(new Color(.66f, .66f, .64f));
            var c = new Vector3(at.x, hi, at.z);
            var basePos = c - along * 2.5f;
            mk.Box(stone, new Vector3(basePos.x, (lo + hi) / 2f - .3f, basePos.z), new Vector3(9.4f, hi - lo + .8f, 27.4f), rot);
            mk.Box(flint, c + Vector3.up * 3.5f, new Vector3(8f, 7f, 16f), rot);
            solid.Box(flint, c + Vector3.up * 3.5f, new Vector3(8f, 7f, 16f), rot);
            mk.GableRoof(slate, c + Vector3.up * 7f, new Vector3(8.6f, 0, 16.6f), 4f, rot);
            foreach (var s in new[] { -1f, 1f })
            for (var i = 0; i < 4; i++)
                mk.Box(glassDark, c + side * s * 4.02f + along * (i * 3.6f - 5.4f) + Vector3.up * 3.6f, new Vector3(.1f, 2.6f, 1.1f), rot);
            var tower = c - along * 10f;
            var drop = hi - lo + .5f;
            mk.Box(flint, tower + Vector3.up * (8f - drop / 2f), new Vector3(5f, 16f + drop, 5f), rot);
            solid.Box(flint, tower + Vector3.up * 8f, new Vector3(5f, 16f, 5f), rot);
            mk.Box(stone, tower + Vector3.up * 16.2f, new Vector3(5.4f, .5f, 5.4f), rot);
            mk.Cone(slate, tower + Vector3.up * 16.4f, 3f, 13f, 8);
            mk.Box(iron, tower + Vector3.up * 30f, new Vector3(.08f, 1.4f, .08f), Quaternion.identity);
            mk.Box(WorldMaterials.Solid(new Color(.8f, .62f, .3f), .5f), tower + Vector3.up * 30.4f, new Vector3(.9f, .35f, .08f), rot);   // weathervane
            mk.Box(darkWood, tower - along * 2.53f + Vector3.up * 1.6f, new Vector3(1.6f, 3.2f, .1f), rot);
            mk.Prism(cream, tower - along * 2.55f + Vector3.up * 12f, -along, .9f, .06f, 16);   // the clock
            // a few old headstones
            for (var i = 0; i < 9; i++)
            {
                var p = c + side * ctx.Range(6f, 11f) * (i % 2 == 0 ? 1f : -1f) + along * ctx.Range(-8f, 8f);
                p.y = ctx.Ground(p.x, p.z);
                mk.Box(stone, p + Vector3.up * .45f, new Vector3(.6f, .9f, .14f), Quaternion.LookRotation(side) * Quaternion.Euler(ctx.Range(-6f, 6f), 0, ctx.Range(-5f, 5f)));
            }
        }

        void Folly(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 at)
        {
            at.y = Footprint(ctx, at, Quaternion.identity, 6f, 6f).lo;
            mk.Prism(brick, at - Vector3.up * .5f, Vector3.up, 2.8f, 17.5f, 12, .9f);
            solid.Prism(brick, at - Vector3.up * .5f, Vector3.up, 2.8f, 17.5f, 8, .9f);
            for (var k = 0; k < 12; k++)
            {
                if (k % 2 == 1) continue;
                var a = k * Mathf.PI * 2f / 12f;
                var dir = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                mk.Box(brick, at + dir * 2.35f + Vector3.up * 17.4f, new Vector3(1.1f, 1f, .5f), Quaternion.LookRotation(dir));
            }
            for (var k = 0; k < 4; k++)
            {
                var dir = Quaternion.Euler(0, k * 90f, 0) * Vector3.forward;
                mk.Box(glassDark, at + dir * (2.75f - (k + 1) * .07f) + Vector3.up * (4f + k * 3.2f), new Vector3(.6f, 1.2f, .12f), Quaternion.LookRotation(dir));
            }
            mk.Box(iron, at + Vector3.up * 19.5f, new Vector3(.06f, 3f, .06f), Quaternion.identity);
            flags.Flag(at + Vector3.up * 20.9f, at + Vector3.up * 20.2f, at + Vector3.up * 20.55f + Vector3.right * 1.2f, new Color(.85f, .25f, .22f));
        }

        void Poplars(LandContext ctx, MeshKit mk, Vector3 at)
        {
            var dir = Quaternion.Euler(0, ctx.Range(0, 180f), 0) * Vector3.forward;
            var n = 8 + ctx.Rng.Next(6);
            for (var i = 0; i < n; i++)
            {
                var p = at + dir * (i - n / 2f) * 5f;
                if (!Open(ctx, p.x, p.z, 2f)) continue;
                p = ctx.OnGround(p.x, p.z);
                var h = ctx.Range(11f, 15f);
                mk.Prism(bark, p, Vector3.up, .25f, 3f, 6, .8f);
                mk.Blob(i % 2 == 0 ? leafA : hedge, p + Vector3.up * (2.2f + h * .5f), new Vector3(1.4f, h * .5f, 1.4f), ctx.Rng, .1f);
                ArtistBits.PostCollider(ctx.Root, p, 3f, .3f);
            }
        }

        // ---------------------------------------------------------------- the village green
        /// The small market town at the land's heart: cottages in faded paint round a green, a maypole with
        /// bunting to every side, a red phone box, a postbox, benches, bicycles and lamp posts with gig posters.
        void Green(LandContext ctx, MeshKit mk, MeshKit solid, MeshKit pictures)
        {
            var c = ctx.Plaza;
            var lane = new List<Vector3>();
            for (var k = 0; k <= 48; k++) { var a = k * Mathf.PI * 2f / 48f; lane.Add(ctx.OnGround(c.x + Mathf.Cos(a) * 22f, c.z + Mathf.Sin(a) * 22f)); }
            Ribbon(ctx, mk, WorldMaterials.Solid(new Color(.8f, .78f, .72f)), lane, 3.4f, .07f);
            // the maypole and its bunting
            var pole = ctx.OnGround(c.x, c.z);
            mk.Prism(cream, pole, Vector3.up, .16f, 9f, 8, .7f);
            mk.Blob(WorldMaterials.Solid(new Color(.85f, .68f, .25f), .4f), pole + Vector3.up * 9.1f, Vector3.one * .3f, ctx.Rng, 0f);
            ArtistBits.PostCollider(ctx.Root, pole, 9f, .25f);
            var flagColors = new[] { NoahBlue, Pastels[1], SeanGreen, Pastels[4], new Color(.85f, .3f, .3f), Pastels[2] };
            for (var k = 0; k < 8; k++)
            {
                var a = (k + .5f) * Mathf.PI * 2f / 8f;
                var anchor = ctx.OnGround(c.x + Mathf.Cos(a) * 25.5f, c.z + Mathf.Sin(a) * 25.5f);
                if (NearPath(anchor.x, anchor.z, 2f)) continue;
                mk.Prism(darkWood, anchor, Vector3.up, .09f, 3.4f, 6);
                ArtistBits.PostCollider(ctx.Root, anchor, 3.4f, .14f);
                Bunting(mk, pole + Vector3.up * 8.6f, anchor + Vector3.up * 3.3f, flagColors, k, 1.4f);
            }
            // cottages round the green, facing it
            var houses = 0;
            for (var k = 0; k < 9; k++)
            {
                var a = k * Mathf.PI * 2f / 9f + .2f;
                var dir = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                var at = c + dir * 33f;
                if (!ctx.IsLand(at.x, at.z, 6f) || NearPath(at.x, at.z, 7f)) continue;
                var w = ctx.Range(7f, 9.5f); var d = ctx.Range(6f, 7.5f);
                House(ctx, mk, solid, ctx.OnGround(at.x, at.z), -dir, w, d, ctx.Range(4.4f, 5.6f), Pastels[(k * 5) % Pastels.Length], Doors[(k * 3) % Doors.Length], k % 3 == 0 ? terracotta : slate);
                houses++;
                if (k % 3 == 1) Bike(mk, ctx.OnGround(at.x - dir.x * (d / 2f + .7f), at.z - dir.z * (d / 2f + .7f)) + Vector3.Cross(Vector3.up, dir) * (w / 2f - 1f), Vector3.Cross(Vector3.up, dir), Pastels[k % Pastels.Length]);
            }
            // a red phone box, a postbox, benches and lamp posts with gig posters for the land's own artists
            var free = new List<Vector3>();
            for (var k = 0; k < 12; k++)
            {
                var a = k * Mathf.PI * 2f / 12f + .45f;
                var p = new Vector3(c.x + Mathf.Cos(a) * 26.5f, 0, c.z + Mathf.Sin(a) * 26.5f);
                if (ctx.IsLand(p.x, p.z, 2f) && !NearPath(p.x, p.z, 3f)) free.Add(ctx.OnGround(p.x, p.z));
            }
            for (var i = 0; i < free.Count; i++)
            {
                var p = free[i]; var toC = (c - p); toC.y = 0; toC.Normalize();
                if (i == 0) PhoneBox(mk, solid, p, toC);
                else if (i == 1) PostBox(mk, solid, p);
                else if (i % 3 == 0) LampPost(ctx, mk, pictures, p, toC, 2);
            }
            for (var k = 0; k < 4; k++)
            {
                var a = k * Mathf.PI / 2f + .78f;
                var p = ctx.OnGround(c.x + Mathf.Cos(a) * 11f, c.z + Mathf.Sin(a) * 11f);
                Bench(mk, solid, p, (c - p).normalized);
            }
            Debug.Log($"The Indie Downs: village green with {houses} cottages");
        }

        void House(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 at, Vector3 front, float w, float d, float wallH, Color wall, Color door, Material roof)
        {
            front = Vector3.ProjectOnPlane(front, Vector3.up).normalized;
            var rot = Quaternion.LookRotation(front);
            var side = Vector3.Cross(Vector3.up, front);
            var (lo, hi) = Footprint(ctx, at, rot, w, d);
            var baseY = hi;
            var paint = WorldMaterials.Solid(wall);
            var c = new Vector3(at.x, baseY, at.z);
            mk.Box(stone, new Vector3(c.x, (lo + baseY) / 2f - .2f, c.z), new Vector3(w + .3f, baseY - lo + .6f, d + .3f), rot);
            mk.Box(paint, c + Vector3.up * wallH / 2f, new Vector3(w, wallH, d), rot);
            solid.Box(paint, c + Vector3.up * wallH / 2f, new Vector3(w, wallH, d), rot);
            mk.GableRoof(roof, c + Vector3.up * wallH, new Vector3(d + .7f, 0, w + .7f), 2.6f, Quaternion.LookRotation(side));
            mk.Box(brick, c + Vector3.up * (wallH + 2.2f) + side * (w / 2f - 1.2f), new Vector3(.7f, 2f, .7f), rot);   // chimney
            var face = c + front * (d / 2f + .03f);
            mk.Box(WorldMaterials.Solid(door), face + Vector3.up * 1.15f, new Vector3(1.1f, 2.3f, .08f), rot);
            mk.Box(cream, face + Vector3.up * 2.4f, new Vector3(1.4f, .14f, .1f), rot);
            foreach (var s in new[] { -1f, 1f })
            foreach (var y in new[] { 1.5f, wallH - 1.2f })
            {
                var win = face + side * s * (w / 2f - 1.6f) + Vector3.up * y;
                mk.Box(cream, win, new Vector3(1.3f, 1.2f, .08f), rot);
                mk.Box(glassDark, win + front * .03f, new Vector3(1.05f, .95f, .06f), rot);
            }
            var upWin = face + Vector3.up * (wallH - 1.2f);
            mk.Box(cream, upWin, new Vector3(1.1f, 1.1f, .08f), rot);
            mk.Box(glassDark, upWin + front * .03f, new Vector3(.85f, .85f, .06f), rot);
        }

        /// The lowest and highest ground under a (rotated) rectangle.
        static (float lo, float hi) Footprint(LandContext ctx, Vector3 c, Quaternion rot, float w, float d)
        {
            float lo = float.MaxValue, hi = float.MinValue;
            for (var i = -2; i <= 2; i++)
            for (var j = -2; j <= 2; j++)
            {
                var p = c + rot * new Vector3(i * w / 4f, 0, j * d / 4f);
                var g = ctx.Ground(p.x, p.z);
                lo = Mathf.Min(lo, g); hi = Mathf.Max(hi, g);
            }
            return (lo, hi);
        }

        void PhoneBox(MeshKit mk, MeshKit solid, Vector3 at, Vector3 front)
        {
            var rot = Quaternion.LookRotation(front);
            mk.Box(red, at + Vector3.up * 1.3f, new Vector3(1.1f, 2.6f, 1.1f), rot);
            mk.Box(red, at + Vector3.up * 2.72f, new Vector3(1.2f, .25f, 1.2f), rot);
            mk.Box(red, at + Vector3.up * 2.9f, new Vector3(.8f, .15f, .8f), rot);
            foreach (var f in new[] { front, -front, rot * Vector3.right, rot * Vector3.left })
            {
                var r2 = Quaternion.LookRotation(f);
                mk.Box(glassDark, at + Vector3.up * 1.55f + f * .56f, new Vector3(.75f, 1.5f, .03f), r2);
                mk.Box(cream, at + Vector3.up * 2.45f + f * .57f, new Vector3(.8f, .16f, .03f), r2);
            }
            solid.Box(red, at + Vector3.up * 1.3f, new Vector3(1.15f, 2.6f, 1.15f), rot);
        }

        void PostBox(MeshKit mk, MeshKit solid, Vector3 at)
        {
            mk.Prism(red, at, Vector3.up, .3f, 1.2f, 10);
            mk.Blob(red, at + Vector3.up * 1.2f, new Vector3(.3f, .14f, .3f), new System.Random(3), 0f);
            mk.Box(board, at + Vector3.up * .95f + Vector3.forward * .29f, new Vector3(.3f, .05f, .04f), Quaternion.identity);
            solid.Prism(red, at, Vector3.up, .3f, 1.3f, 8);
        }

        void Bench(MeshKit mk, MeshKit solid, Vector3 at, Vector3 facing)
        {
            facing = Vector3.ProjectOnPlane(facing, Vector3.up).normalized;
            var rot = Quaternion.LookRotation(facing);
            mk.Box(wood, at + Vector3.up * HumanScale.SeatHeight, new Vector3(1.8f, .08f, HumanScale.SeatDepth), rot);
            mk.Box(wood, at + Vector3.up * .8f - facing * .22f, new Vector3(1.8f, .32f, .06f), rot);
            foreach (var s in new[] { -1f, 1f }) mk.Box(iron, at + rot * new Vector3(s * .75f, .22f, 0), new Vector3(.07f, .45f, .42f), rot);
            solid.Box(wood, at + Vector3.up * .45f, new Vector3(1.8f, .9f, .5f), rot);
        }

        /// A lamp post with gig posters (covers of this land's songs) wrapped round it.
        void LampPost(LandContext ctx, MeshKit mk, MeshKit pictures, Vector3 at, Vector3 facing, int posters)
        {
            mk.Prism(iron, at, Vector3.up, .1f, 4.2f, 8);
            ArtistBits.PostCollider(ctx.Root, at, 4.2f, .15f);
            mk.Box(iron, at + Vector3.up * 4.2f, new Vector3(.5f, .12f, .5f), Quaternion.identity);
            var glow = WorldMaterials.Glow(Lamp, 1.3f);
            mk.Box(glow, at + Vector3.up * 3.95f, new Vector3(.36f, .42f, .36f), Quaternion.identity);
            halos.Add(at + Vector3.up * 3.95f, 1.3f, Lamp, .7f);
            var pic = WorldMaterials.Picture(ctx.CoverAtlas);
            for (var i = 0; i < posters; i++)
            {
                var cover = RandomCover(ctx);
                if (cover < 0) break;
                var n = (i % 2 == 0 ? facing : -facing); n.y = 0; n.Normalize();
                var r = Vector3.Cross(n, Vector3.up);
                pictures.Picture(pic, at + Vector3.up * (1.45f + (i / 2) * .8f + (i % 2) * .4f) + n * .12f, r * .32f, Vector3.up * .32f, n, ctx.CoverRect(cover));
            }
        }

        int RandomCover(LandContext ctx)
        {
            for (var tries = 0; tries < 10; tries++)
            {
                var a = ctx.Artists[ctx.Rng.Next(ctx.Artists.Count)];
                if (a.songList == null || a.songList.Length == 0) continue;
                var s = a.songList[ctx.Rng.Next(a.songList.Length)];
                if (s.cover >= 0) return s.cover;
            }
            return -1;
        }

        void Bike(MeshKit mk, Vector3 at, Vector3 along, Color frame)
        {
            along = Vector3.ProjectOnPlane(along, Vector3.up).normalized;
            var side = Vector3.Cross(Vector3.up, along);
            var paint = WorldMaterials.Solid(frame, .3f);
            var lean = Quaternion.AngleAxis(12f, along);
            Vector3 P(float a, float u) => at + lean * (along * a + Vector3.up * u);
            foreach (var s in new[] { -.55f, .55f }) mk.Prism(iron, P(s, .36f) - side * .03f, lean * side, .35f, .06f, 12);
            void Bar(Vector3 a, Vector3 b, float t) { var d = b - a; mk.Box(paint, (a + b) / 2f, new Vector3(t, d.magnitude, t), Quaternion.LookRotation(lean * side, d.normalized)); }
            Bar(P(-.55f, .36f), P(-.05f, .4f), .05f);
            Bar(P(-.05f, .4f), P(.4f, .82f), .05f);
            Bar(P(-.05f, .4f), P(-.15f, .9f), .05f);
            Bar(P(-.15f, .88f), P(.4f, .82f), .05f);
            Bar(P(.55f, .36f), P(.42f, 1f), .05f);
            mk.Box(board, P(-.17f, .95f), new Vector3(.12f, .05f, .26f), Quaternion.LookRotation(along));
            mk.Box(iron, P(.42f, 1.02f), new Vector3(.5f, .04f, .04f), Quaternion.LookRotation(along));
        }

        /// Bunting along a sagging string from a to b. Flags flutter in the wind (WindGrass shader).
        void Bunting(MeshKit mk, Vector3 a, Vector3 b, Color[] colors, int offset, float sag)
        {
            var len = Vector3.Distance(a, b);
            var n = Mathf.Max(2, Mathf.FloorToInt(len / .6f));
            Vector3 At(float t) => Vector3.Lerp(a, b, t) - Vector3.up * sag * 4f * t * (1f - t);
            for (var i = 0; i < n; i++)
            {
                float t0 = (i + .1f) / n, t1 = (i + .7f) / n;
                Vector3 p0 = At(t0), p1 = At(t1), tip = At((t0 + t1) / 2f) - Vector3.up * .42f;
                flags.Flag(p0, p1, tip, colors[(i + offset) % colors.Length]);
                var s0 = At(i / (float)n); var s1 = At((i + 1f) / n); var d = s1 - s0;
                if (d.sqrMagnitude > 1e-4f) mk.Box(darkWood, (s0 + s1) / 2f, new Vector3(.025f, .025f, d.magnitude), Quaternion.LookRotation(d));
            }
        }

        static Material WindMaterial(float sway, float flutter) => WindMesh.Material(sway, flutter, Sage);

        // ---------------------------------------------------------------- the chalk guitar
        /// A giant guitar cut into the chalk of a hillside, on a slope that faces the hub's causeway.
        void ChalkGuitar(LandContext ctx, MeshKit mk)
        {
            var bestScore = float.MinValue; var at = Vector3.zero; var upHill = Vector3.forward;
            for (var x = ctx.MinX; x < ctx.MaxX; x += 10f)
            for (var z = ctx.MinZ; z < ctx.MaxZ; z += 10f)
            {
                if (!ctx.IsLand(x, z, 40f) || !ctx.IsFree(x, z, 40f) || NearPath(x, z, 30f) || InNoGrass(x, z, 50f)) continue;
                float gx = ctx.Ground(x + 15f, z) - ctx.Ground(x - 15f, z), gz = ctx.Ground(x, z + 15f) - ctx.Ground(x, z - 15f);
                var grad = new Vector3(gx, 0, gz) / 30f;                 // rise per metre, pointing uphill
                var steep = grad.magnitude;
                if (steep < .1f || steep > .5f) continue;
                var facing = -grad.normalized;                            // the way the slope faces
                var toGate = new Vector3(hubGate.x - x, 0, hubGate.z - z).normalized;
                var score = Vector3.Dot(facing, toGate) * 2f + Mathf.Min(steep, .3f) * 3f;
                if (score > bestScore) { bestScore = score; at = new Vector3(x, 0, z); upHill = grad.normalized; }
            }
            if (bestScore == float.MinValue) { Debug.Log("The Indie Downs: no hillside for the chalk guitar"); return; }
            const float S = 40f, cell = 1.2f;
            var across = Vector3.Cross(Vector3.up, upHill);
            bool Inside(float u, float v)
            {
                bool Circle(float cu, float cv, float r) => (u - cu) * (u - cu) + (v - cv) * (v - cv) < r * r;
                if (Circle(-.13f, 0, .075f)) return false;                       // sound hole
                if (u > -.53f && u < -.49f && Mathf.Abs(v) < .12f) return false; // bridge
                if (Circle(-.42f, 0, .3f) || Circle(-.02f, 0, .22f)) return true;  // lower and upper bouts
                if (u > -.42f && u < -.02f && Mathf.Abs(v) < .19f) return true;    // waist
                if (u >= .15f && u < .78f && Mathf.Abs(v) < .045f) return true;    // neck
                if (u >= .78f && u < .95f && Mathf.Abs(v) < .075f) return true;    // headstock
                return false;
            }
            var quads = 0;
            for (var u = -.75f * S; u < .97f * S; u += cell)
            for (var v = -.32f * S; v < .32f * S; v += cell)
            {
                if (!Inside((u + cell / 2f) / S, (v + cell / 2f) / S)) continue;
                Vector3 P(float du, float dv) { var p = at + upHill * (u + du) + across * (v + dv); return ctx.OnGround(p.x, p.z, .12f); }
                Vector3 a = P(0, 0), b = P(cell, 0), c = P(cell, cell), d = P(0, cell);
                mk.Quad(chalk, a, b, c, d, (a + c) / 2f - Vector3.up);
                quads++;
            }
            noGrass.Add(new Vector3(at.x + upHill.x * 4f, 40f, at.z + upHill.z * 4f));
            Debug.Log($"The Indie Downs: chalk guitar of {quads} pieces at {at.x:0},{at.z:0}");
        }

        // ---------------------------------------------------------------- hedgerows, trees, kites
        void Hedgerows(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            // old field hedges: uneven, gappy, mixed shades, the odd tree grown up out of them; never up steep banks
            var shades = new[] { hedge, leafA, leafB, WorldMaterials.Solid(new Color(.4f, .5f, .3f)) };
            for (var h = 0; h < 7; h++)
            {
                if (!ctx.TrySpot(out var start, 10f, 10f)) continue;
                var a = ctx.Range(0, Mathf.PI * 2f);
                var dir = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                var len = ctx.Range(30f, 70f);
                var wiggle = ctx.Range(2f, 5f); var phase = ctx.Range(0, 6f);
                var sinceTree = ctx.Range(6f, 16f);
                for (var d = 0f; d < len; d += ctx.Range(1.3f, 2.6f))
                {
                    var p = start + dir * d + Vector3.Cross(Vector3.up, dir) * (Mathf.Sin(d * .06f + phase) * wiggle + ctx.Range(-.5f, .5f));
                    if (!Open(ctx, p.x, p.z, 1.5f) || ctx.Chance(.12f)) continue;          // gaps
                    var g = ctx.OnGround(p.x, p.z);
                    if (Mathf.Abs(ctx.Ground(p.x + dir.x * 3f, p.z + dir.z * 3f) - g.y) > 1.6f) continue;   // too steep here
                    sinceTree -= 2f;
                    if (sinceTree <= 0f)
                    {
                        sinceTree = ctx.Range(14f, 30f);
                        var th = ctx.Range(3.5f, 5.5f); var cr = ctx.Range(2.6f, 3.8f);
                        mk.Prism(bark, g, Vector3.up, .28f, th + 1f, 7, .7f);
                        mk.Blob(shades[ctx.Rng.Next(3)], g + Vector3.up * (th + cr * .5f), new Vector3(cr, cr * .85f, cr), ctx.Rng, .14f);
                        ArtistBits.PostCollider(ctx.Root, g, th, .3f);
                    }
                    var w = ctx.Range(.9f, 1.6f); var hh = ctx.Range(.8f, 1.5f);
                    mk.Blob(shades[ctx.Rng.Next(shades.Length)], g + Vector3.up * hh * .75f, new Vector3(w, hh, w * ctx.Range(.85f, 1.15f)), ctx.Rng, .2f);
                    solid.Box(hedge, g + Vector3.up * hh * .8f, new Vector3(w * 1.6f, hh * 1.6f, w * 1.6f), Quaternion.LookRotation(dir));
                }
            }
        }

        void LoneTrees(LandContext ctx, MeshKit mk)
        {
            for (var i = 0; i < 26; i++)
            {
                if (!ctx.TrySpot(out var p, 8f, 6f) || !Open(ctx, p.x, p.z, 4f)) continue;
                var h = ctx.Range(3f, 4.5f);
                mk.Prism(bark, p, Vector3.up, .3f, h + 1f, 7, .7f);
                var leaf = i % 2 == 0 ? leafA : leafB;
                var r = ctx.Range(2.8f, 4f);
                mk.Blob(leaf, p + Vector3.up * (h + r * .6f), new Vector3(r, r * .85f, r), ctx.Rng, .12f);
                mk.Blob(leaf, p + Vector3.up * (h + r * .4f) + new Vector3(r * .5f, 0, r * .2f), new Vector3(r * .7f, r * .6f, r * .7f), ctx.Rng, .12f);
                ArtistBits.PostCollider(ctx.Root, p, h, .35f);
            }
        }

        /// Hilltops for the kites: the highest open spots, well apart.
        void KiteSpots(LandContext ctx)
        {
            var spots = new List<Vector3>();
            for (var i = 0; i < 400; i++)
                if (ctx.TrySpot(out var p, 15f, 10f, 1) && Open(ctx, p.x, p.z, 6f)) spots.Add(p);
            spots.Sort((a, b) => b.y.CompareTo(a.y));
            foreach (var s in spots)
            {
                var ok = true;
                foreach (var k in kiteAnchors) if (Vector3.Distance(k, s) < 140f) { ok = false; break; }
                if (ok) kiteAnchors.Add(s);
                if (kiteAnchors.Count >= 4) break;
            }
        }

        static readonly Color[] Flowers = { new Color(.86f, .16f, .12f), new Color(.86f, .16f, .12f), new Color(.86f, .16f, .12f), new Color(.35f, .5f, .9f), new Color(.97f, .96f, .9f) };

        /// Clumps of trees on the skyline, the way downland woods sit on the hilltops.
        void Copses(LandContext ctx, MeshKit mk)
        {
            for (var i = 0; i < 7; i++)
            {
                if (!ctx.TrySpot(out var c, 25f, 20f) || !Open(ctx, c.x, c.z, 12f)) continue;
                var n = 6 + ctx.Rng.Next(6);
                for (var k = 0; k < n; k++)
                {
                    var a = ctx.Range(0, Mathf.PI * 2f); var r = ctx.Range(0, 13f);
                    var p = ctx.OnGround(c.x + Mathf.Cos(a) * r, c.z + Mathf.Sin(a) * r);
                    if (!Open(ctx, p.x, p.z, 2f)) continue;
                    var h = ctx.Range(3.5f, 6f); var cr = ctx.Range(3f, 4.5f);
                    mk.Prism(bark, p, Vector3.up, .3f, h + 1f, 7, .7f);
                    mk.Blob(k % 3 == 0 ? hedge : (k % 2 == 0 ? leafA : leafB), p + Vector3.up * (h + cr * .5f), new Vector3(cr, cr * .9f, cr), ctx.Rng, .14f);
                    ArtistBits.PostCollider(ctx.Root, p, h, .35f);
                }
            }
        }

        /// Flocks of sheep grazing on the open downs.
        void Sheep(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var wool = WorldMaterials.Solid(new Color(.95f, .93f, .87f));
            var face = WorldMaterials.Solid(new Color(.14f, .13f, .13f));
            for (var f = 0; f < 7; f++)
            {
                if (!ctx.TrySpot(out var c, 15f, 12f) || !Open(ctx, c.x, c.z, 8f)) continue;
                var n = 5 + ctx.Rng.Next(6);
                for (var k = 0; k < n; k++)
                {
                    var a = ctx.Range(0, Mathf.PI * 2f); var r = ctx.Range(0, 11f);
                    var p = ctx.OnGround(c.x + Mathf.Cos(a) * r, c.z + Mathf.Sin(a) * r);
                    if (!Open(ctx, p.x, p.z, 1f)) continue;
                    var dir = Quaternion.Euler(0, ctx.Range(0, 360f), 0) * Vector3.forward;
                    var side = Vector3.Cross(Vector3.up, dir);
                    var grazing = ctx.Chance(.6f);
                    mk.Blob(wool, p + Vector3.up * .75f, new Vector3(.55f, .45f, .55f) + Vector3.Scale(new Vector3(.1f, 0, .1f), new Vector3(Mathf.Abs(dir.z), 0, Mathf.Abs(dir.x))), ctx.Rng, .12f);
                    mk.Blob(wool, p + Vector3.up * .78f + dir * .35f, new Vector3(.45f, .42f, .45f), ctx.Rng, .12f);
                    var head = p + dir * .85f + Vector3.up * (grazing ? .35f : .9f);
                    mk.Box(face, head, new Vector3(.24f, .26f, .38f), Quaternion.LookRotation(dir + Vector3.down * (grazing ? .8f : 0f)));
                    foreach (var (fx, fz) in new[] { (-1f, -1f), (1f, -1f), (-1f, 1f), (1f, 1f) })
                        mk.Box(face, p + side * fx * .2f + dir * fz * .3f + Vector3.up * .22f, new Vector3(.08f, .45f, .08f), Quaternion.LookRotation(dir));
                    solid.Box(wool, p + Vector3.up * .6f, new Vector3(.8f, 1.2f, 1.4f), Quaternion.LookRotation(dir));
                }
            }
        }

        /// Round bales left in a few mown fields.
        void HayBales(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var hay = WorldMaterials.Solid(new Color(.86f, .74f, .42f));
            var end = WorldMaterials.Solid(new Color(.78f, .64f, .34f));
            for (var f = 0; f < 4; f++)
            {
                if (!ctx.TrySpot(out var c, 20f, 15f) || !Open(ctx, c.x, c.z, 15f)) continue;
                var n = 5 + ctx.Rng.Next(6);
                var placed = new List<Vector3>();
                for (var tries = 0; tries < n * 6 && placed.Count < n; tries++)
                {
                    var a = ctx.Range(0, Mathf.PI * 2f); var r = Mathf.Sqrt(ctx.Range(0, 1f)) * 28f;
                    var p = c + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * r;
                    var tooClose = false;
                    foreach (var q in placed) if ((q - p).sqrMagnitude < 9f * 9f) { tooClose = true; break; }
                    if (tooClose || !Open(ctx, p.x, p.z, 2f)) continue;
                    placed.Add(p);
                    p = ctx.OnGround(p.x, p.z);
                    var axis = Quaternion.Euler(0, ctx.Range(0, 180f), 0) * Vector3.forward;
                    mk.Prism(hay, p + Vector3.up * .8f - axis * .65f, axis, .82f, 1.3f, 12);
                    mk.Prism(end, p + Vector3.up * .8f - axis * .66f, axis, .6f, 1.32f, 12);
                    solid.Box(hay, p + Vector3.up * .8f, new Vector3(1.6f, 1.6f, 1.3f), Quaternion.LookRotation(axis));
                }
            }
        }

        // ---------------------------------------------------------------- the long grass
        /// Meadows of long grass in drifts across the land, off the paths and out of the areas, in chunks so
        /// the camera only draws what's in view. It all moves with the wind.
        void Grass(LandContext ctx)
        {
            var mat = WindMaterial(.55f, 1f);
            var chunks = new Dictionary<(int, int), WindMesh>();
            const float spacing = 2.15f;
            var rng = new System.Random(4242);
            var tufts = 0;
            for (var x = ctx.MinX; x < ctx.MaxX; x += spacing)
            for (var z = ctx.MinZ; z < ctx.MaxZ; z += spacing)
            {
                var px = x + ((float)rng.NextDouble() - .5f) * spacing; var pz = z + ((float)rng.NextDouble() - .5f) * spacing;
                var n = Mathf.PerlinNoise(px * .012f + 31.7f, pz * .012f + 11.3f);
                if (n < .38f) continue;
                if (!ctx.IsLand(px, pz, 5f) || !ctx.IsFree(px, pz, 2f) || NearPath(px, pz, 2.2f) || InNoGrass(px, pz, 0f)) continue;
                var g = ctx.Ground(px, pz);
                if (g < 2.5f || Mathf.Abs(ctx.Ground(px + 1.5f, pz) - g) > 1f || Mathf.Abs(ctx.Ground(px, pz + 1.5f) - g) > 1f) continue;
                var key = (Mathf.FloorToInt(px / 80f), Mathf.FloorToInt(pz / 80f));
                if (!chunks.TryGetValue(key, out var wm)) chunks[key] = wm = new WindMesh();
                var tall = Mathf.Lerp(.7f, 1.45f, Mathf.InverseLerp(.38f, .8f, n)) * (.8f + (float)rng.NextDouble() * .4f);
                var tint = Color.Lerp(Sage * .95f, Straw, Mathf.Clamp01((float)rng.NextDouble() * .5f + .25f + (n - .38f) * .8f));
                wm.Tuft(new Vector3(px, g - .05f, pz), tall, tint, rng);
                var f = rng.NextDouble();
                if (f < .09) wm.Flower(new Vector3(px + .3f, g - .05f, pz + .2f), tall * .85f, Flowers[(int)(f * 1000) % Flowers.Length], rng);
                tufts++;
            }
            foreach (var kv in chunks) kv.Value.Build("Long grass", ctx.Root, mat);
            Debug.Log($"The Indie Downs: {tufts} tufts of long grass in {chunks.Count} patches");
        }
    }
}
