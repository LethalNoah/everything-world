using System.Collections.Generic;
using UnityEngine;

namespace EverythingWorld.World
{
    /// Lanternwood (WORLD_GUIDE.md): birch woods dusted with snow, small clearings and a frozen creek.
    /// A small abandoned chapel full of candles. Hanging lanterns, log cabins, canoes, stacked firewood,
    /// a lute resting on a stump. Blue-hour dusk with warm windows. Hushed.
    /// Artist areas: halls are log cabins with lit windows; smaller artists get lantern posts and
    /// benches in the clearings (monuments get a little lean-to shelter).
    public class LanternwoodKit : LandKit
    {
        public override string Land => "Lanternwood";

        static readonly Color Snow = new Color(.86f, .89f, .94f), SnowShade = new Color(.78f, .82f, .9f);
        static readonly Color Moss = new Color(.42f, .48f, .38f), FrostRock = new Color(.6f, .62f, .67f);
        static readonly Color LanternGlass = new Color(1f, .66f, .3f);
        static readonly Color Ink = new Color(.93f, .88f, .76f);

        Material bark, barkMark, leafGold, leafPale, snowTop, logs, darkWood, planks, stone, ice, board, iron, candle, canoeRed, canoeGreen;

        void MakeMaterials()
        {
            bark = WorldMaterials.Solid(new Color(.9f, .89f, .86f));
            barkMark = WorldMaterials.Solid(new Color(.16f, .16f, .17f));
            leafGold = WorldMaterials.Solid(new Color(.84f, .72f, .4f));
            leafPale = WorldMaterials.Solid(new Color(.64f, .68f, .47f));
            snowTop = WorldMaterials.Solid(new Color(.94f, .95f, .98f));
            logs = WorldMaterials.Solid(new Color(.6f, .42f, .28f));
            darkWood = WorldMaterials.Solid(new Color(.36f, .27f, .21f));
            planks = WorldMaterials.Solid(new Color(.46f, .4f, .36f));
            stone = WorldMaterials.Solid(new Color(.52f, .53f, .57f));
            ice = WorldMaterials.Solid(new Color(.7f, .83f, .92f), .85f);
            board = WorldMaterials.Solid(new Color(.22f, .17f, .14f));
            iron = WorldMaterials.Solid(new Color(.12f, .12f, .13f));
            candle = WorldMaterials.Solid(new Color(.95f, .92f, .82f));
            canoeRed = WorldMaterials.Solid(new Color(.62f, .2f, .16f));
            canoeGreen = WorldMaterials.Solid(new Color(.24f, .4f, .3f));
        }

        public override bool Ground(float height, float slope, float patch, out Color color)
        {
            if (slope > 7f || height > 26f) color = FrostRock;
            else if (patch > .7f) color = Moss;
            else if (patch < .32f) color = SnowShade;
            else color = Snow;
            return true;
        }

        // ---------------------------------------------------------------- scenery
        readonly List<Vector3> clear = new List<Vector3>(); // x, z = point, y = radius: no trees here (creek, paths)

        bool NearClear(float x, float z)
        {
            foreach (var c in clear)
            {
                float dx = x - c.x, dz = z - c.z;
                if (dx * dx + dz * dz < c.y * c.y) return true;
            }
            return false;
        }

        public override void Dress(LandContext ctx)
        {
            MakeMaterials();
            clear.Clear();
            var mk = new MeshKit(); var solid = new MeshKit();
            var chapelFront = Chapel(ctx, mk, solid);
            var creek = Creek(ctx, mk);
            Paths(ctx, mk, chapelFront);
            Props(ctx, mk, solid, chapelFront, creek);
            mk.Build("Lanternwood scenery", ctx.Root, false);
            solid.BuildCollider("Lanternwood walls", ctx.Root);
            Forest(ctx);
        }

        /// The chapel at the plaza. Returns the spot just outside its door.
        Vector3 Chapel(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var root = new GameObject("The chapel").transform;
            root.SetParent(ctx.Root, false);
            // face the door toward the nearest cabin, so the lantern path leads somewhere
            var face = Vector3.forward;
            var best = float.MaxValue;
            foreach (var a in ctx.Artists)
            {
                var d = new Vector3(a.x - ctx.Plaza.x, 0, a.z - ctx.Plaza.z);
                if (a.tier == "hall" && d.magnitude < best) { best = d.magnitude; face = d.normalized; }
            }
            var rot = Quaternion.LookRotation(face);
            var p = ctx.Plaza;
            const float w = 7f, len = 12f, wallH = 4.6f, t = .35f;
            var floorY = p.y + .4f;
            Vector3 L(float x, float y, float z) => new Vector3(p.x, floorY, p.z) + rot * new Vector3(x, y, z);

            mk.Box(stone, L(0, -.35f, 0), new Vector3(w + .8f, 1.1f, len + .8f), rot);              // foundation
            mk.Box(darkWood, L(0, .02f, 0), new Vector3(w, .1f, len), rot);                          // floor
            solid.Box(planks, L(0, wallH / 2, -len / 2), new Vector3(w, wallH, t), rot); mk.Box(planks, L(0, wallH / 2, -len / 2), new Vector3(w, wallH, t), rot);             // back wall
            foreach (var s in new[] { -1f, 1f })
            {
                solid.Box(planks, L(s * w / 2, wallH / 2, 0), new Vector3(t, wallH, len), rot); mk.Box(planks, L(s * w / 2, wallH / 2, 0), new Vector3(t, wallH, len), rot);     // side walls
                solid.Box(planks, L(s * (w / 2 - 1.35f), wallH / 2, len / 2), new Vector3(2.7f, wallH, t), rot); mk.Box(planks, L(s * (w / 2 - 1.35f), wallH / 2, len / 2), new Vector3(2.7f, wallH, t), rot); // front, either side of the door
                for (var k = 0; k < 3; k++)                                                          // warm windows
                    mk.Box(WorldMaterials.Glow(LanternGlass, 1.3f), L(s * (w / 2 + .19f), 2.6f, -3.5f + k * 3.5f), new Vector3(.05f, 1.5f, .8f), rot);
            }
            solid.Box(planks, L(0, wallH - .6f, len / 2), new Vector3(1.6f, 1.2f, t), rot); mk.Box(planks, L(0, wallH - .6f, len / 2), new Vector3(1.6f, 1.2f, t), rot);          // above the door
            mk.GableRoof(darkWood, L(0, wallH, 0), new Vector3(w + 1.4f, 0, len + 1.2f), 3.8f, rot, snowTop);
            // gable fronts above the walls
            foreach (var z in new[] { -len / 2, len / 2 })
            {
                var a = L(-w / 2, wallH, z); var b = L(w / 2, wallH, z); var c = L(0, wallH + 3.8f, z);
                mk.Tri(planks, a, b, c, L(0, wallH, 0));
            }
            // bell cupola on the ridge, at the front
            mk.Box(planks, L(0, wallH + 4.2f, len / 2 - 1.4f), new Vector3(1.4f, 1.6f, 1.4f), rot);
            mk.Cone(darkWood, L(0, wallH + 5f, len / 2 - 1.4f), 1.15f, 1.6f, 4, 45f);
            mk.Blob(iron, L(0, wallH + 4.1f, len / 2 - 1.4f), new Vector3(.35f, .4f, .35f), ctx.Rng, .05f);
            // inside: pews, an altar, and candles everywhere
            for (var r = 0; r < 4; r++)
                foreach (var s in new[] { -1f, 1f })
                {
                    mk.Box(darkWood, L(s * 1.6f, .45f, 2.2f - r * 1.8f), new Vector3(2f, .12f, .5f), rot);
                    solid.Box(darkWood, L(s * 1.6f, .55f, 2.1f - r * 1.8f), new Vector3(2f, 1.1f, .7f), rot);
                    mk.Box(darkWood, L(s * 1.6f, .8f, 2.0f - r * 1.8f), new Vector3(2f, .7f, .08f), rot);
                }
            mk.Box(stone, L(0, .55f, -len / 2 + 1.4f), new Vector3(2.4f, 1.1f, 1f), rot);
            solid.Box(stone, L(0, .55f, -len / 2 + 1.4f), new Vector3(2.4f, 1.1f, 1f), rot);
            var flame = WorldMaterials.Glow(new Color(1f, .72f, .35f), 6f);
            for (var i = 0; i < 46; i++)
            {
                Vector3 c;
                if (i < 14) c = L(ctx.Range(-1.1f, 1.1f), 1.1f, -len / 2 + ctx.Range(1.05f, 1.75f));    // on the altar
                else if (i < 30) c = L(ctx.Range(-w / 2 + .4f, w / 2 - .4f), .07f, ctx.Range(-len / 2 + .5f, -len / 2 + 2.6f)); // on the floor round the altar
                else c = L((ctx.Chance(.5f) ? -1 : 1) * (w / 2 - .45f), .07f, ctx.Range(-len / 2 + .6f, len / 2 - .8f)); // along the walls
                var hgt = ctx.Range(.12f, .38f);
                mk.Prism(candle, c, Vector3.up, ctx.Range(.03f, .06f), hgt, 5);
                mk.Cone(flame, c + Vector3.up * (hgt + .01f), .025f, .09f, 4);
            }
            ArtistBits.PointLight(root, L(0, 1.8f, -len / 2 + 1.6f), new Color(1f, .68f, .38f), 9f, 2.2f);
            ArtistBits.PointLight(root, L(0, 1.4f, 0), new Color(1f, .66f, .36f), 8f, 1.3f);
            ArtistBits.PointLight(root, L(0, 2.2f, len / 2 + 1.5f), new Color(1f, .68f, .4f), 10f, 1.4f); // spilling out of the door
            return L(0, 0, len / 2 + 3.5f);
        }

        /// A frozen creek winding downhill from high ground to the shore. Returns its course.
        List<Vector3> Creek(LandContext ctx, MeshKit mk)
        {
            // start on the highest of a handful of spots well away from the chapel
            Vector3 start = default; var top = float.MinValue;
            for (var i = 0; i < 300; i++)
                if (ctx.TrySpot(out var s, 40f, 10f, 5) && Vector2.Distance(new Vector2(s.x, s.z), new Vector2(ctx.Plaza.x, ctx.Plaza.z)) > 120f && s.y > top)
                { top = s.y; start = s; }
            var path = new List<Vector3>();
            if (top == float.MinValue) return path;
            var p = start; var heading = Vector3.zero;
            for (var step = 0; step < 260; step++)
            {
                path.Add(p);
                if (ctx.Ground(p.x, p.z) < .7f) break;
                Vector3 next = p; var bestScore = float.MaxValue;
                for (var k = 0; k < 16; k++)
                {
                    var a = k * Mathf.PI / 8f;
                    var dir = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                    var q = p + dir * 5f; q.y = ctx.Ground(q.x, q.z);
                    var score = q.y - p.y - Vector3.Dot(dir, heading) * .35f;     // downhill, and keep going the same way
                    if (!ctx.IsFree(q.x, q.z, 2f)) score += 3f;                   // steer round artist plots
                    if (score < bestScore) { bestScore = score; next = q; }
                }
                heading = (next - p).normalized;
                p = next;
            }
            Ribbon(ctx, mk, ice, path, 3.2f, .07f);
            foreach (var q in path) clear.Add(new Vector3(q.x, 5f, q.z));
            // stones along the banks
            for (var i = 2; i < path.Count; i += 3)
            {
                var side = Vector3.Cross(Vector3.up, (path[i] - path[i - 1]).normalized) * (ctx.Chance(.5f) ? 2.4f : -2.4f);
                var c = path[i] + side;
                mk.Blob(stone, ctx.OnGround(c.x, c.z, .1f), new Vector3(ctx.Range(.3f, .7f), ctx.Range(.2f, .4f), ctx.Range(.3f, .7f)), ctx.Rng);
            }
            return path;
        }

        /// A flat strip laid along a path, just above the ground (ice, trodden snow).
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

        /// One network of trodden-snow paths from the chapel door that reaches every artist area: each area
        /// joins the network at its closest point (like branches off a trunk), so paths never run side by
        /// side. Lantern posts stand along them, never two close together.
        void Paths(LandContext ctx, MeshKit mk, Vector3 from)
        {
            var trodden = WorldMaterials.Solid(new Color(.72f, .74f, .8f));
            var glow = WorldMaterials.Glow(LanternGlass, 1.4f);
            var posts = new List<Vector3>();
            var network = new List<Vector3> { from };                 // points already on a path
            var todo = new List<IslandWorldData.Artist>(ctx.Artists);
            while (todo.Count > 0)
            {
                // the unconnected area closest to the network so far
                IslandWorldData.Artist next = null; Vector3 join = default; var best = float.MaxValue;
                foreach (var a in todo)
                {
                    var c = new Vector3(a.x, 0, a.z);
                    foreach (var p in network)
                    {
                        var d = Vector3.Distance(new Vector3(p.x, 0, p.z), c) - a.r;
                        if (d < best) { best = d; next = a; join = p; }
                    }
                }
                todo.Remove(next);
                var to = new Vector3(next.x, 0, next.z);
                var dir = to - new Vector3(join.x, 0, join.z);
                var len = dir.magnitude - next.r * .65f;               // stop inside the area's clearing
                dir.Normalize();
                var side = Vector3.Cross(Vector3.up, dir);
                var pts = new List<Vector3>();
                for (var d = 0f; d <= Mathf.Max(0f, len); d += 4f)
                {
                    var t = len > 0 ? d / len : 0f;
                    var q = new Vector3(join.x, 0, join.z) + dir * d + side * Mathf.Sin(t * Mathf.PI) * Mathf.Min(4f, len * .06f); // a gentle bow
                    if (!ctx.IsLand(q.x, q.z, 0f)) break;
                    pts.Add(ctx.OnGround(q.x, q.z));
                }
                if (pts.Count < 2) continue;
                Ribbon(ctx, mk, trodden, pts, 1.7f, .04f);
                network.AddRange(pts);
                for (var i = 0; i < pts.Count; i++)
                {
                    clear.Add(new Vector3(pts[i].x, 3.2f, pts[i].z));
                    if (i % 3 != 1) continue;
                    var post = pts[i] + side * ((i / 3) % 2 == 0 ? 1.5f : -1.5f);
                    var tooClose = false;
                    foreach (var o in posts) if ((o - post).sqrMagnitude < 9f * 9f) { tooClose = true; break; }
                    if (tooClose || !ctx.IsFree(post.x, post.z, 1f) && Vector3.Distance(post, to) > next.r) continue;
                    posts.Add(post);
                    post.y = ctx.Ground(post.x, post.z);
                    var arm = (pts[i] - post); arm.y = 0; arm.Normalize();      // the lantern hangs over the path
                    mk.Prism(darkWood, post, Vector3.up, .07f, 2.3f, 5);
                    mk.Box(darkWood, post + Vector3.up * 2.22f + arm * .3f, new Vector3(.06f, .06f, .6f), Quaternion.LookRotation(arm));
                    mk.Box(glow, post + Vector3.up * 1.82f + arm * .52f, new Vector3(.18f, .26f, .18f), Quaternion.identity);
                    mk.Box(iron, post + Vector3.up * 1.98f + arm * .52f, new Vector3(.24f, .05f, .24f), Quaternion.identity);
                }
            }
        }

        /// A disc of packed snow, the clearing an artist area stands in.
        static void Clearing(LandContext ctx, MeshKit mk, Material m, Vector3 center, float radius)
        {
            const int rings = 4, spokes = 24;
            Vector3 P(int ring, int spoke)
            {
                var a = spoke * Mathf.PI * 2f / spokes; var rr = radius * ring / rings;
                var x = center.x + Mathf.Cos(a) * rr; var z = center.z + Mathf.Sin(a) * rr;
                return new Vector3(x, ctx.Ground(x, z) + .05f, z);
            }
            for (var ring = 0; ring < rings; ring++)
            for (var s = 0; s < spokes; s++)
            {
                var below = new Vector3(center.x, ctx.Ground(center.x, center.z) - 5f, center.z);
                if (ring == 0) mk.Tri(m, P(0, 0), P(1, s), P(1, s + 1), below);
                else mk.Quad(m, P(ring, s), P(ring, s + 1), P(ring + 1, s + 1), P(ring + 1, s), below);
            }
        }

        void Props(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 chapelFront, List<Vector3> creek)
        {
            // the lute resting on a stump, beside the chapel door
            var side = Vector3.Cross(Vector3.up, (chapelFront - ctx.Plaza).normalized);
            var stump = chapelFront + side * 5f; stump.y = ctx.Ground(stump.x, stump.z);
            mk.Prism(logs, stump, Vector3.up, .42f, .55f, 7);
            mk.Blob(leafGold, stump + Vector3.up * .66f, new Vector3(.28f, .1f, .36f), ctx.Rng, .02f);           // lute body
            var neck = Quaternion.LookRotation(side) * Quaternion.Euler(-12f, 0, 0);
            mk.Box(darkWood, stump + Vector3.up * .7f + neck * Vector3.forward * .45f, new Vector3(.06f, .04f, .6f), neck);
            // firewood stacked against the chapel
            Firewood(mk, solid, chapelFront - side * 4.5f, side);
            // two canoes pulled up where the creek meets the shore
            if (creek.Count > 3)
            {
                var end = creek[creek.Count - 1]; var dir = (creek[creek.Count - 1] - creek[creek.Count - 3]).normalized;
                var across = Vector3.Cross(Vector3.up, dir);
                for (var k = 0; k < 2; k++)
                {
                    var c = end - dir * 8f + across * (k == 0 ? 4f : -4.5f);
                    c.y = Mathf.Max(ctx.Ground(c.x, c.z), .2f) + .3f;
                    mk.Blob(k == 0 ? canoeRed : canoeGreen, c, new Vector3(.55f, .32f, 2.6f), ctx.Rng, .03f);
                }
            }
        }

        void Firewood(MeshKit mk, MeshKit solid, Vector3 at, Vector3 along)
        {
            along = Vector3.ProjectOnPlane(along, Vector3.up).normalized;
            var front = Vector3.Cross(along, Vector3.up);
            for (var row = 0; row < 4; row++)
            for (var i = 0; i < 6 - row; i++)
            {
                var p = at + along * ((i - (5 - row) / 2f) * .32f) + Vector3.up * (.17f + row * .28f) - front * .5f;
                mk.Prism(logs, p, front, .15f, 1f, 6);
            }
            solid.Box(logs, at + Vector3.up * .6f, new Vector3(2f, 1.2f, 1f), Quaternion.LookRotation(front));
        }

        /// Thousands of birches, in clumps with clearings between, a few with lanterns hanging from them.
        void Forest(LandContext ctx)
        {
            var visuals = new Dictionary<(int, int), MeshKit>(); var trunks = new Dictionary<(int, int), MeshKit>();
            var root = new GameObject("Birch woods").transform;
            root.SetParent(ctx.Root, false);
            var glow = WorldMaterials.Glow(LanternGlass, 1.6f);
            const float step = 8.5f;
            var seed = ctx.Island.sceneId * 13.7f;
            var count = 0;
            for (var x = ctx.MinX; x < ctx.MaxX; x += step)
            for (var z = ctx.MinZ; z < ctx.MaxZ; z += step)
            {
                float tx = x + ctx.Range(-3.5f, 3.5f), tz = z + ctx.Range(-3.5f, 3.5f);
                var thick = Mathf.PerlinNoise(tx * .012f + seed, tz * .012f);
                if (thick < .38f) continue;                                         // a clearing
                if (thick < .5f && ctx.Chance(.6f)) continue;                       // thinning toward its edge
                if (!ctx.IsLand(tx, tz, 6f) || !ctx.IsFree(tx, tz, 3f) || NearClear(tx, tz)) continue;
                var g = ctx.Ground(tx, tz);
                var key = (Mathf.FloorToInt(tx / 128f), Mathf.FloorToInt(tz / 128f));
                if (!visuals.TryGetValue(key, out var vis)) { visuals[key] = vis = new MeshKit(); trunks[key] = new MeshKit(); }
                Birch(ctx, vis, trunks[key], new Vector3(tx, g - .2f, tz), glow);
                count++;
            }
            foreach (var kv in visuals)
            {
                kv.Value.Build($"Birches {kv.Key.Item1},{kv.Key.Item2}", root, false);
                trunks[kv.Key].Build($"Birch trunks {kv.Key.Item1},{kv.Key.Item2}", root, true);
            }
            Debug.Log($"Lanternwood: {count} birches");
        }

        void Birch(LandContext ctx, MeshKit vis, MeshKit trunk, Vector3 at, Material lanternGlow)
        {
            var h = ctx.Range(8f, 14f); var r = ctx.Range(.16f, .26f);
            var lean = new Vector3(ctx.Range(-.06f, .06f), 1f, ctx.Range(-.06f, .06f)).normalized;
            vis.Prism(bark, at, lean, r, h, 5, .55f, ctx.Range(0, 70));
            trunk.Prism(bark, at, Vector3.up, r, 3f, 5);
            for (var i = 0; i < 3; i++)                                             // the dark marks on birch bark
            {
                var y = ctx.Range(1f, h * .7f);
                var spin = Quaternion.Euler(0, ctx.Range(0, 360), 0);
                vis.Box(barkMark, at + lean * y + spin * Vector3.forward * r * .85f, new Vector3(r * 1.1f, .07f, .06f), spin);
            }
            var top = at + lean * h;
            if (ctx.Chance(.3f))
            {
                // bare, with a few upswept branches and snow caught on top
                for (var b = 0; b < 4; b++)
                {
                    var a = ctx.Range(0, Mathf.PI * 2f);
                    var dir = new Vector3(Mathf.Cos(a), ctx.Range(.9f, 1.6f), Mathf.Sin(a));
                    vis.Prism(bark, at + lean * h * ctx.Range(.55f, .85f), dir, r * .4f, ctx.Range(1.6f, 2.8f), 4, .3f);
                }
                vis.Blob(snowTop, top, new Vector3(.35f, .18f, .35f), ctx.Rng);
            }
            else
            {
                var leaves = ctx.Chance(.55f) ? leafGold : leafPale;
                var n = ctx.Chance(.5f) ? 2 : 3;
                for (var k = 0; k < n; k++)
                {
                    var c = top - lean * ctx.Range(1.2f, 3.6f) + new Vector3(ctx.Range(-1f, 1f), 0, ctx.Range(-1f, 1f));
                    vis.Blob(leaves, c, new Vector3(ctx.Range(1.3f, 2.1f), ctx.Range(1.5f, 2.4f), ctx.Range(1.3f, 2.1f)), ctx.Rng);
                }
                vis.Blob(snowTop, top - lean * .6f, new Vector3(ctx.Range(.9f, 1.3f), .45f, ctx.Range(.9f, 1.3f)), ctx.Rng);
                if (ctx.Chance(.06f))                                               // a lantern hanging from a low branch
                {
                    var side = new Vector3(ctx.Range(-1f, 1f), 0, ctx.Range(-1f, 1f)).normalized;
                    var hang = at + Vector3.up * ctx.Range(2.6f, 3.4f) + side * .9f;
                    vis.Prism(darkWood, at + Vector3.up * (hang.y - at.y + .45f), side + Vector3.up * .15f, .04f, 1f, 4);
                    vis.Box(lanternGlow, hang, new Vector3(.18f, .26f, .18f), Quaternion.identity);
                    vis.Box(iron, hang + Vector3.up * .17f, new Vector3(.24f, .05f, .24f), Quaternion.identity);
                }
            }
        }

        // ---------------------------------------------------------------- artist areas
        public override void BuildArtist(LandContext ctx, IslandWorldData.Artist a)
        {
            if (bark == null) MakeMaterials();
            var root = new GameObject("Artist: " + a.name).transform;
            root.SetParent(ctx.Root, false);
            var center = ctx.OnGround(a.x, a.z); // everything below is in world coordinates; root stays at the origin
            var toPlaza = new Vector3(ctx.Plaza.x - a.x, 0, ctx.Plaza.z - a.z);
            var front = toPlaza.sqrMagnitude > 1f ? toPlaza.normalized : Vector3.forward;
            var right = Vector3.Cross(Vector3.up, front);
            var mk = new MeshKit(); var solid = new MeshKit(); var pictures = new MeshKit();
            var labels = new GameObject("Labels").transform; labels.SetParent(root, false);
            var area = root.gameObject.AddComponent<ArtistArea>();
            area.artistName = a.name; area.stationId = a.stationId; area.radius = a.r; area.glowColor = LanternGlass;

            var glows = new List<Renderer>();
            Clearing(ctx, mk, WorldMaterials.Solid(new Color(.78f, .8f, .86f)), center, a.tier == "shrine" ? 7f : a.tier == "monument" ? 9f : 15f);
            if (a.tier == "shrine")
            {
                ArtistBits.Plaque(mk, solid, labels, a, center, front, darkWood, board, Ink);
                var coverAt = center + Vector3.up * 2.25f - front * .02f;
                mk.Box(board, coverAt - front * .05f, new Vector3(.86f, .86f, .06f), Quaternion.LookRotation(front));
                ArtistBits.CoverWall(pictures, labels, ctx, a, coverAt - front * .01f, front, 1, .74f, 1, false, Ink);
                glows.Add(ArtistBits.LanternPost(mk, root, Ground(ctx, center + right * 1.5f), -right, darkWood, iron, LanternGlass));
                Bench(ctx, mk, solid, center + front * 3.2f, -front);
            }
            else if (a.tier == "monument")
            {
                LeanTo(ctx, mk, solid, pictures, labels, a, center, front);
                ArtistBits.Plaque(mk, solid, labels, a, Ground(ctx, center + front * 3.6f + right * 2.6f), front, darkWood, board, Ink, 1.3f);
                glows.Add(ArtistBits.LanternPost(mk, root, Ground(ctx, center + front * 3.4f - right * 2.4f), right, darkWood, iron, LanternGlass));
                area.glowLight = ArtistBits.PointLight(root, center + front * 2f + Vector3.up * 2.4f, new Color(1f, .68f, .38f), 9f, 1.1f);
            }
            else
            {
                Cabin(ctx, mk, solid, pictures, labels, a, center, front, glows);
                ArtistBits.Plaque(mk, solid, labels, a, Ground(ctx, center + front * 7.8f + right * 3.2f), front, darkWood, board, Ink, 1.7f);
                foreach (var s in new[] { -1f, 1f })
                    glows.Add(ArtistBits.LanternPost(mk, root, Ground(ctx, center + front * 10f + right * 2.2f * s), -right * s, darkWood, iron, LanternGlass));
                Firewood(mk, solid, Ground(ctx, center + right * 5.6f - front * 1f), front);
                area.glowLight = ArtistBits.PointLight(root, center + front * 9f + Vector3.up * 2.6f, new Color(1f, .68f, .38f), 12f, 1.2f);
            }
            area.glowRenderers = glows.ToArray();
            mk.Build("Area", root, false);
            solid.BuildCollider("Walls", root);
            pictures.Build("Covers", root, false);
        }

        static Vector3 Ground(LandContext ctx, Vector3 p) => ctx.OnGround(p.x, p.z);

        void Bench(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 at, Vector3 facing)
        {
            at = Ground(ctx, at);
            var rot = Quaternion.LookRotation(facing);
            mk.Box(darkWood, at + Vector3.up * .45f, new Vector3(1.7f, .08f, .45f), rot);
            mk.Box(snowTop, at + Vector3.up * .5f, new Vector3(1.6f, .03f, .38f), rot);
            mk.Box(darkWood, at + Vector3.up * .78f - facing * .2f, new Vector3(1.7f, .32f, .06f), rot);
            foreach (var s in new[] { -1f, 1f })
                mk.Box(darkWood, at + rot * new Vector3(s * .7f, .22f, 0), new Vector3(.08f, .45f, .4f), rot);
            solid.Box(darkWood, at + Vector3.up * .45f, new Vector3(1.7f, .9f, .5f), rot);
        }

        /// A small open shelter: back wall with the covers, a snowy roof, a bench inside.
        void LeanTo(LandContext ctx, MeshKit mk, MeshKit solid, MeshKit pictures, Transform labels, IslandWorldData.Artist a, Vector3 c, Vector3 front)
        {
            var rot = Quaternion.LookRotation(front);
            const float w = 3.6f, d = 2.2f;
            var floor = c.y + .1f;
            Vector3 P(float x, float y, float z) => new Vector3(c.x, floor, c.z) + rot * new Vector3(x, y, z);
            mk.Box(darkWood, P(0, -.05f, 0), new Vector3(w + .4f, .2f, d + .4f), rot);
            solid.Box(logs, P(0, 1.2f, -d / 2), new Vector3(w, 2.4f, .16f), rot); mk.Box(logs, P(0, 1.2f, -d / 2), new Vector3(w, 2.4f, .16f), rot);
            foreach (var s in new[] { -1f, 1f })
            {
                mk.Prism(darkWood, P(s * w / 2, 0, d / 2), Vector3.up, .1f, 2.2f, 5);
                mk.Box(logs, P(s * w / 2, 1.2f, 0), new Vector3(.14f, 2.4f, d), rot);
            }
            var roofRot = rot * Quaternion.Euler(-14f, 0, 0);
            mk.Box(darkWood, P(0, 2.55f, 0), new Vector3(w + .6f, .14f, d + .9f), roofRot);
            mk.Box(snowTop, P(0, 2.66f, 0), new Vector3(w + .5f, .1f, d + .8f), roofRot);
            ArtistBits.CoverWall(pictures, labels, ctx, a, P(0, 1.55f, -d / 2 + .09f), front, 5, .56f, 5, true, Ink);
            Bench(ctx, mk, solid, P(0, 0, .2f) - Vector3.up * .1f, front);
        }

        /// A log cabin with lit windows, its covers on the back wall inside.
        void Cabin(LandContext ctx, MeshKit mk, MeshKit solid, MeshKit pictures, Transform labels, IslandWorldData.Artist a, Vector3 c, Vector3 front,
                   List<Renderer> glows)
        {
            var rot = Quaternion.LookRotation(front);
            const float w = 8f, d = 9f, logR = .2f;
            const int courses = 9;
            var floor = c.y + .35f;
            Vector3 P(float x, float y, float z) => new Vector3(c.x, floor, c.z) + rot * new Vector3(x, y, z);
            var right = rot * Vector3.right; var fwd = rot * Vector3.forward;
            mk.Box(stone, P(0, -.3f, 0), new Vector3(w + .6f, .9f, d + .6f), rot);
            mk.Box(darkWood, P(0, .05f, 0), new Vector3(w, .1f, d), rot);
            for (var k = 0; k < courses; k++)
            {
                var y = .25f + k * logR * 1.9f;
                // side walls run front to back, back wall left to right; logs overhang at the corners
                foreach (var s in new[] { -1f, 1f })
                    mk.Prism(logs, P(s * w / 2, y, -d / 2 - .35f), fwd, logR, d + .7f, 6);
                mk.Prism(logs, P(-w / 2 - .35f, y + logR * .95f, -d / 2), right, logR, w + .7f, 6);
                // front wall, with a door gap in the lower courses
                if (k < 6)
                {
                    mk.Prism(logs, P(-w / 2 - .35f, y + logR * .95f, d / 2), right, logR, w / 2 + .35f - .8f, 6);
                    mk.Prism(logs, P(.8f, y + logR * .95f, d / 2), right, logR, w / 2 + .35f - .8f, 6);
                }
                else mk.Prism(logs, P(-w / 2 - .35f, y + logR * .95f, d / 2), right, logR, w + .7f, 6);
            }
            var wallH = .25f + courses * logR * 1.9f;
            solid.Box(logs, P(0, wallH / 2, -d / 2), new Vector3(w, wallH, .4f), rot);
            foreach (var s in new[] { -1f, 1f })
            {
                solid.Box(logs, P(s * w / 2, wallH / 2, 0), new Vector3(.4f, wallH, d), rot);
                solid.Box(logs, P(s * (w / 4 + .4f), wallH / 2, d / 2), new Vector3(w / 2 - .8f, wallH, .4f), rot);
                // two lit windows on each side
                foreach (var z in new[] { -2f, 2f })
                {
                    var win = ArtistBits.GlassBox(labels.parent, P(s * (w / 2 + .18f), 1.7f, z), new Vector3(.06f, 1f, 1.1f), new Color(1f, .7f, .38f));
                    win.transform.rotation = rot;
                    glows.Add(win);
                }
            }
            var roofBase = P(0, wallH, 0);
            mk.GableRoof(darkWood, roofBase, new Vector3(w + 1.6f, 0, d + 1.4f), 3.2f, rot, snowTop);
            foreach (var z in new[] { -d / 2, d / 2 })
                mk.Tri(logs, P(-w / 2, wallH, z), P(w / 2, wallH, z), P(0, wallH + 3.2f, z), P(0, wallH, 0));
            mk.Box(stone, P(w / 2 - 1f, wallH + 1.8f, -d / 2 + 1.5f), new Vector3(.9f, 2.8f, .9f), rot);   // chimney
            // inside: the covers on the back wall, a rug, a warm light
            ArtistBits.CoverWall(pictures, labels, ctx, a, P(0, 1.75f, -d / 2 + .22f), front, 5, .62f, 15, true, Ink);
            mk.Box(WorldMaterials.Solid(new Color(.55f, .2f, .18f)), P(0, .12f, .5f), new Vector3(3.2f, .02f, 2.2f), rot);
            ArtistBits.PointLight(labels.parent, P(0, 2.4f, -1f), new Color(1f, .7f, .42f), 9f, 1.6f);
        }
    }
}
