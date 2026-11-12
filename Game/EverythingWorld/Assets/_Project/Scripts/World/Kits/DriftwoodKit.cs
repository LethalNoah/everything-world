using System.Collections.Generic;
using UnityEngine;

namespace EverythingWorld.World
{
    /// The Driftwood Shore (WORLD_GUIDE.md): bittersweet. The morning after a storm on a cold shore: silver sea and
    /// sky, pearl mist, a thin line of rose and gold breaking on the horizon, wind, waves, and everything the storm
    /// left behind: bleached driftwood, sea glass, a wreck half buried in the sand. The warmth is in small things:
    /// a bonfire still smouldering, lit beach huts, lanterns on the boardwalk.
    ///
    /// At the heart, where you arrive, a still lagoon with a curved driftwood arch rising out of it and a little
    /// footbridge through the arch; a boardwalk rings the lagoon and every artist sits close round it. From there a
    /// long promenade runs out to the beach (the huts, the wreck, the whale bones, the jetty and its beacon). The
    /// rest of the land is wild dune and heath with wind-bent trees, sand fences, cairns, boat sheds and a ruined
    /// cottage. When "Somewhere Over the Rainbow" plays, a rainbow rises out over the sea, seen through the arch.
    /// Artist areas: DriftwoodArtists.cs. The coast: DriftwoodCoast.cs. The wilds: DriftwoodWilds.cs.
    /// Weather and music: DriftwoodLife.
    public partial class DriftwoodKit : LandKit
    {
        public override Vector3 ArtistFront(IslandWorldData data, IslandWorldData.Artist a) =>
            facing.TryGetValue(a.name, out var f) ? f : base.ArtistFront(data, a);
        public override string Land => "The Driftwood Shore";
        public override (string song, string label, string artist)[] SpecialSongs => new[] { (RainbowSong, "Over the Rainbow", (string)null) };

        // the palette, from the covers: Jack Johnson's silver-grey and rose, Agnes Obel's ember, pencil-grey
        static readonly Color WetSand = new Color(.42f, .4f, .38f), DuneSand = new Color(.7f, .66f, .58f), Heath = new Color(.47f, .51f, .46f), HeathB = new Color(.54f, .55f, .5f);
        static readonly Color Bone = new Color(.84f, .8f, .72f), Ember = new Color(1f, .6f, .28f), Rose = new Color(.82f, .6f, .6f), Pearl = new Color(.86f, .86f, .83f);
        static readonly Color Ink = new Color(.94f, .92f, .88f), Bulb = new Color(1f, .78f, .5f);
        static readonly Color[] HutPaint = { new Color(.78f, .6f, .6f), new Color(.62f, .7f, .64f), new Color(.6f, .68f, .76f), new Color(.86f, .82f, .72f), new Color(.7f, .64f, .72f) };
        static readonly Color[] SeaGlass = { new Color(.4f, .9f, .8f), new Color(.45f, .85f, .5f), new Color(1f, .7f, .3f), new Color(.85f, .92f, 1f), new Color(.4f, .6f, 1f) };

        const float RingR = 22f, LagoonR = 14f, ArchR = 11f, WalkW = 2.4f, WalkLift = .28f;

        Material drift, driftDark, planks, plankGap, rope, iron, board, stone, water, seaweed, cloth, bulb;
        MeshKit soft;                   // seaweed and low leaves you walk through (the land audit skips "Soft")
        GlowHalos halos;
        Vector3 center, seaDir = Vector3.forward, shore;
        bool hasShore;
        readonly Dictionary<(int, int), List<Vector3>> pathCells = new Dictionary<(int, int), List<Vector3>>();
        readonly Dictionary<string, Vector3> facing = new Dictionary<string, Vector3>();
        readonly List<Vector3> noGrass = new List<Vector3>();           // x, radius, z
        readonly List<List<Vector3>> walks = new List<List<Vector3>>();
        readonly List<int> railKind = new List<int>();                  // 0 none, 1 right side, 2 both sides, 3 the side toward the heart
        readonly List<(Vector3 p, int w)> walkPoints = new List<(Vector3, int)>();
        DriftwoodLife life;
        public const string RainbowSong = "25U7raB3ZSszayTYClh4hF";     // Somewhere Over the Rainbow / What a Wonderful World

        void MakeMaterials()
        {
            drift = WorldMaterials.Solid(Bone);
            driftDark = WorldMaterials.Solid(new Color(.6f, .56f, .5f));
            planks = WorldMaterials.Solid(new Color(.62f, .56f, .5f));
            plankGap = WorldMaterials.Solid(new Color(.32f, .29f, .27f));
            rope = WorldMaterials.Solid(new Color(.72f, .66f, .54f));
            iron = WorldMaterials.Solid(new Color(.2f, .2f, .22f));
            board = WorldMaterials.Solid(new Color(.16f, .15f, .15f));
            stone = WorldMaterials.Solid(new Color(.5f, .5f, .52f));
            water = WorldMaterials.Solid(new Color(.34f, .42f, .46f), .95f);
            seaweed = WorldMaterials.Solid(new Color(.18f, .24f, .16f), .6f);
            cloth = WorldMaterials.Solid(new Color(.8f, .74f, .7f));
            bulb = WorldMaterials.Glow(Bulb, 2.6f);
        }

        public override bool Ground(float height, float slope, float patch, out Color color)
        {
            if (slope > 5f) color = Color.Lerp(new Color(.46f, .46f, .48f), new Color(.56f, .56f, .57f), patch);
            else if (height < 1.9f) color = WetSand;
            else if (height < 6f) color = Color.Lerp(DuneSand, new Color(.66f, .63f, .56f), patch);
            else color = Color.Lerp(Heath, HeathB, Mathf.SmoothStep(0, 1, patch));
            color.a = 1f;
            return true;
        }

        static bool HasStation(IslandWorldData.Artist a) => a.station || a.stationId != 0;

        /// A plain box collider on its own object (a branch, a loose plank: shapes the merged walls don't cover).
        static void BoxCol(Transform parent, Vector3 center, Vector3 size, Quaternion rot)
        {
            var go = new GameObject("Box collider"); go.transform.SetParent(parent, false); go.transform.SetPositionAndRotation(center, rot);
            go.AddComponent<BoxCollider>().size = size; go.isStatic = true;
        }

        /// How far an artist's own scene reaches from its centre.
        static float Foot(IslandWorldData.Artist a) => a.name == "Jack Johnson" ? 22f : HasStation(a) ? 13f : 8f;

        /// The way to the open sea from here, and how far: the shortest of 16 directions (or MaxValue).
        static Vector3 SeaFrom(IslandWorldData data, Vector3 p, out float dist, float max = 720f)
        {
            dist = float.MaxValue; var best = Vector3.forward;
            var step = max < 60f ? 2f : 6f;
            for (var k = 0; k < 16; k++)
            {
                var a = k * Mathf.PI / 8f; var d = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                for (var s = step; s < max && s < dist; s += step)
                {
                    var q = p + d * s;
                    if (data.IslandAt(q.x, q.z) == null || data.HeightAt(q.x, q.z) < .2f) { dist = s; best = d; break; }
                }
            }
            return best;
        }

        /// Level the heart for the lagoon, and spread the artists round it with room to wander between them,
        /// leaving the way to the sea open and keeping well away from the borders with the next lands.
        public override void ShapeGround(IslandWorldData data, int index)
        {
            var isl = data.layout.islands[index]; var sid = isl.sceneId;
            var heart = new Vector2(isl.labelX, isl.labelZ);

            // a level floor for the lagoon and the ring of boardwalk round it
            var level = data.HeightAt(heart.x, heart.y);
            const float FlatR = RingR + 6f, BlendR = RingR + 22f;
            for (var r = 0; r < data.Nz; r++)
            {
                var z = data.layout.depth - r * data.Step;
                if (Mathf.Abs(z - heart.y) > BlendR) continue;
                for (var col = 0; col < data.Nx; col++)
                {
                    var x = col * data.Step;
                    if (Mathf.Abs(x - heart.x) > BlendR) continue;
                    var i = r * data.Nx + col;
                    if (data.islandIds[i] != index) continue;
                    var d = Vector2.Distance(new Vector2(x, z), heart);
                    if (d < FlatR) data.heights[i] = level;
                    else if (d < BlendR) data.heights[i] = Mathf.Lerp(level, data.heights[i], Mathf.SmoothStep(0, 1, (d - FlatR) / (BlendR - FlatR)));
                }
            }

            // spread the artists round the lagoon (not crammed in, not out at the borders), keeping the lagoon
            // and the promenade to the sea clear
            var sea3 = SeaFrom(data, new Vector3(heart.x, 0, heart.y), out var seaDist);
            var avoid = new List<Vector3> { new Vector3(heart.x, RingR + 8f, heart.y) };
            for (var d = RingR; d < Mathf.Min(seaDist, 700f); d += 15f) avoid.Add(new Vector3(heart.x + sea3.x * d, 14f, heart.y + sea3.z * d));
            ArtistPlacement.Spread(data, index, 110f, heart, 48f, RingR + 10f, avoid, null, Land);
        }

        public override void Dress(LandContext ctx)
        {
            MakeMaterials();
            halos = new GlowHalos();
            pathCells.Clear(); facing.Clear(); noGrass.Clear(); walks.Clear(); railKind.Clear(); walkPoints.Clear();
            center = ctx.Plaza;
            life = new GameObject("Driftwood life").AddComponent<DriftwoodLife>();
            life.transform.SetParent(ctx.Root, false);
            life.center = center;
            seaDir = SeaFrom(ctx.Data, center, out _);
            hasShore = Shoreline(ctx, out shore);
            SeaLane(ctx);
            var mk = new MeshKit(); var solid = new MeshKit();
            soft = new MeshKit();
            ArchAndLagoon(ctx, mk, solid);
            Boardwalk(ctx);
            foreach (var w in walks) Walk(mk, solid, w);
            Rails(mk, solid);
            Lamps(ctx, mk);
            SongPlaces(ctx, mk, solid);
            Coast(ctx, mk, solid);
            Wilds(ctx, mk, solid);
            Washed(ctx, mk, solid);
            mk.Build("Driftwood scenery", ctx.Root, false);
            soft.Build("Soft seaweed and leaves", ctx.Root, false);
            solid.BuildCollider("Driftwood walls", ctx.Root);
        }

        public override void Finish(LandContext ctx)
        {
            GroundCover(ctx);
            halos.Build("Driftwood glow", ctx.Root);
        }

        // ---------------------------------------------------------------- places and spacing
        Vector3 SeaDir(LandContext ctx, Vector3 p, out float dist) => SeaFrom(ctx.Data, p, out dist);

        /// Where the way from the heart toward the sea reaches the beach.
        bool Shoreline(LandContext ctx, out Vector3 at)
        {
            at = center;
            for (var s = RingR; s < 720f; s += 3f)
            {
                var q = center + seaDir * s;
                if (!ctx.IsLand(q.x, q.z, 0f)) return s > RingR + 20f;
                at = ctx.OnGround(q.x, q.z);
            }
            return false;
        }

        /// A line of open sea off the beach, a kilometre long, for ships, whales and storms (DriftwoodLife).
        void SeaLane(LandContext ctx)
        {
            if (!hasShore) return;
            var across = Vector3.Cross(Vector3.up, seaDir);
            bool Water(Vector3 q) => ctx.Data.IslandAt(q.x, q.z) == null || ctx.Ground(q.x, q.z) < -1f;
            for (var s = 140f; s <= 420f; s += 20f)
            {
                var mid = shore + seaDir * s; mid.y = 0f;
                var open = true;
                for (var k = -500f; k <= 500f && open; k += 25f) open = Water(mid + across * k);
                if (!open) continue;
                life.laneFrom = mid - across * 500f; life.laneTo = mid + across * 500f; life.hasLane = true;
                return;
            }
        }

        void Remember(Vector3 p)
        {
            var key = (Mathf.FloorToInt(p.x / 8f), Mathf.FloorToInt(p.z / 8f));
            if (!pathCells.TryGetValue(key, out var l)) pathCells[key] = l = new List<Vector3>();
            l.Add(p);
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

        bool NearOtherWalk(Vector3 q, int walk, float within)
        {
            foreach (var (p, w) in walkPoints)
            {
                if (w == walk) continue;
                float dx = p.x - q.x, dz = p.z - q.z;
                if (dx * dx + dz * dz < within * within) return true;
            }
            return false;
        }

        bool InNoGrass(float x, float z, float pad)
        {
            foreach (var c in noGrass) { float dx = x - c.x, dz = z - c.z, r = c.y + pad; if (dx * dx + dz * dz < r * r) return true; }
            return false;
        }

        /// Inside an artist's own scene (its footprint, scaled, plus pad).
        bool InFootprint(LandContext ctx, float x, float z, float pad, float scale = 1f)
        {
            foreach (var a in ctx.Artists) { float dx = x - a.x, dz = z - a.z, r = Foot(a) * scale + pad; if (dx * dx + dz * dz < r * r) return true; }
            return false;
        }

        bool Open(LandContext ctx, float x, float z, float pad) =>
            ctx.IsLand(x, z, 2f) && !InFootprint(ctx, x, z, pad) && !NearPath(x, z, pad + 1.5f) && !InNoGrass(x, z, pad);

        // ---------------------------------------------------------------- the boardwalk
        void AddWalk(List<Vector3> pts, int rails)
        {
            var id = walks.Count;
            walks.Add(pts); railKind.Add(rails);
            foreach (var p in pts) { Remember(p); walkPoints.Add((p, id)); }
        }

        /// From the ring round the lagoon, a straight boardwalk to every artist, each joining the network where
        /// it's closest (so they branch like a tree and never cross), and the long promenade out to the beach.
        void Boardwalk(LandContext ctx)
        {
            if (hasShore)
            {
                var pts = new List<Vector3>();
                var len = Vector3.Distance(new Vector3(center.x, 0, center.z), new Vector3(shore.x, 0, shore.z)) - 6f;
                for (var d = RingR + WalkW / 2f + .1f; d <= len; d += 3f)
                {
                    var q = center + seaDir * d;
                    if (!ctx.IsLand(q.x, q.z, 0f)) break;
                    pts.Add(ctx.OnGround(q.x, q.z));
                }
                if (pts.Count > 2) AddWalk(pts, 1);
            }
            var todo = new List<IslandWorldData.Artist>(ctx.Artists);
            while (todo.Count > 0)
            {
                IslandWorldData.Artist next = null; Vector3 join = default; var best = float.MaxValue;
                foreach (var a in todo)
                {
                    var c = new Vector3(a.x, 0, a.z);
                    foreach (var (p, _) in walkPoints)
                    {
                        var d = Vector2.Distance(new Vector2(p.x, p.z), new Vector2(c.x, c.z)) - Foot(a);
                        if (d < best) { best = d; next = a; join = p; }
                    }
                }
                if (next == null) break;
                todo.Remove(next);
                var to = new Vector3(next.x, 0, next.z); var from = new Vector3(join.x, 0, join.z);
                var dir = to - from; var total = dir.magnitude;
                if (total < 1f) continue;
                dir /= total;
                facing[next.name] = -dir;
                float start = WalkW / 2f + .1f, end = total - Mathf.Max(6f, Foot(next) * .55f);
                if (end < start + 2f) continue;
                var pts = new List<Vector3>();
                for (var d = start; ; d += 3f)
                {
                    var dd = Mathf.Min(d, end);
                    var q = from + dir * dd;
                    if (!ctx.IsLand(q.x, q.z, 0f)) break;
                    if (dd > start + 4f && NearOtherWalk(q, -1, WalkW + .6f)) break;       // ran into another walk: stop short
                    pts.Add(ctx.OnGround(q.x, q.z));
                    if (dd >= end) break;
                }
                if (pts.Count >= 2) AddWalk(pts, 1);
            }
        }

        /// One stretch of boardwalk, laid as a single seamless deck (mitred at the bends so nothing overlaps),
        /// with plank lines, edge boards, posts underneath and a collider.
        void Walk(MeshKit mk, MeshKit solid, List<Vector3> pts)
        {
            const float w = WalkW, lift = WalkLift, thick = .16f;
            var n = pts.Count; if (n < 2) return;
            var closed = (pts[0] - pts[n - 1]).sqrMagnitude < .01f;
            var L = new Vector3[n]; var R = new Vector3[n];
            Vector3 Flat(Vector3 v) { v.y = 0; return v.normalized; }
            for (var i = 0; i < n; i++)
            {
                var a = i > 0 ? Flat(pts[i] - pts[i - 1]) : closed ? Flat(pts[n - 1] - pts[n - 2]) : Flat(pts[1] - pts[0]);
                var b = i < n - 1 ? Flat(pts[i + 1] - pts[i]) : closed ? Flat(pts[1] - pts[0]) : Flat(pts[n - 1] - pts[n - 2]);
                var t = a + b; t = t.sqrMagnitude < .01f ? b : t.normalized;
                var side = Vector3.Cross(Vector3.up, t);
                var miter = 1f / Mathf.Max(.6f, Vector3.Dot(side, Vector3.Cross(Vector3.up, b)));
                var top = pts[i] + Vector3.up * lift;
                L[i] = top - side * (w / 2f * miter); R[i] = top + side * (w / 2f * miter);
            }
            var dn = Vector3.up * thick;
            for (var i = 0; i + 1 < n; i++)
            {
                var mid = (pts[i] + pts[i + 1]) / 2f + Vector3.up * lift;
                mk.Quad(planks, L[i], R[i], R[i + 1], L[i + 1], mid - Vector3.up);
                mk.Quad(driftDark, L[i], L[i + 1], L[i + 1] - dn, L[i] - dn, mid - dn * .5f + (R[i] - L[i]) * .5f);
                mk.Quad(driftDark, R[i], R[i + 1], R[i + 1] - dn, R[i] - dn, mid - dn * .5f - (R[i] - L[i]) * .5f);
                var d = pts[i + 1] - pts[i]; var len = d.magnitude;
                if (len < .1f) continue;
                var rot = Quaternion.LookRotation(d);
                solid.Box(planks, mid - Vector3.up * .12f, new Vector3(w, .3f, len + .05f), rot);
                var gaps = Mathf.FloorToInt(len / .5f);
                for (var k = 1; k < gaps; k++)
                {
                    var f = k / (float)gaps;
                    var a = Vector3.Lerp(L[i], L[i + 1], f); var b = Vector3.Lerp(R[i], R[i + 1], f);
                    mk.Box(plankGap, (a + b) / 2f + Vector3.up * .004f, new Vector3(.03f, .006f, Vector3.Distance(a, b) - .06f), Quaternion.LookRotation(b - a));
                }
                if (i % 2 == 0)
                    foreach (var s in new[] { L[i], R[i] })
                    {
                        var g = pts[i].y;
                        mk.Box(driftDark, new Vector3(s.x, (g - .4f + s.y) / 2f, s.z), new Vector3(.16f, s.y - g + .4f, .16f), rot);
                    }
            }
            if (!closed)
                foreach (var (i, j) in new[] { (0, 1), (n - 1, n - 2) })
                    mk.Quad(driftDark, L[i], R[i], R[i] - dn, L[i] - dn, (pts[j] + pts[i]) / 2f + Vector3.up * (lift - thick / 2f) + (pts[j] - pts[i]).normalized * .5f);
        }

        /// Rope rails on posts, solid to walk into, left open wherever another walk joins.
        void Rails(MeshKit mk, MeshKit solid)
        {
            for (var wi = 0; wi < walks.Count; wi++)
            {
                var kind = railKind[wi]; if (kind == 0) continue;
                var pts = walks[wi];
                for (var i = 0; i + 1 < pts.Count; i++)
                {
                    var flat = pts[i + 1] - pts[i]; flat.y = 0;
                    if (flat.sqrMagnitude < .01f) continue;
                    var right = Vector3.Cross(Vector3.up, flat.normalized);
                    var sides = kind == 2 ? new[] { 1f, -1f } : kind == 3 ? new[] { Vector3.Dot(right, center - pts[i]) > 0 ? 1f : -1f } : new[] { 1f };
                    foreach (var s in sides)
                    {
                        var off = right * s * (WalkW / 2f - .08f);
                        var p0 = pts[i] + off; var p1 = pts[i + 1] + off;
                        if (NearOtherWalk(p0, wi, WalkW / 2f + 1.6f) || NearOtherWalk(p1, wi, WalkW / 2f + 1.6f)) continue;
                        var rot = Quaternion.LookRotation(flat);
                        foreach (var p in i + 2 == pts.Count ? new[] { p0, p1 } : new[] { p0 })
                            mk.Box(driftDark, p + Vector3.up * (WalkLift + .5f), new Vector3(.12f, 1f, .12f), rot);
                        foreach (var h in new[] { .95f, .55f })
                        {
                            var r0 = p0 + Vector3.up * (WalkLift + h); var r1 = p1 + Vector3.up * (WalkLift + h);
                            var mid = (r0 + r1) / 2f - Vector3.up * .06f;
                            mk.Box(rope, (r0 + mid) / 2f, new Vector3(.05f, .05f, Vector3.Distance(r0, mid) + .03f), Quaternion.LookRotation(mid - r0));
                            mk.Box(rope, (mid + r1) / 2f, new Vector3(.05f, .05f, Vector3.Distance(mid, r1) + .03f), Quaternion.LookRotation(r1 - mid));
                        }
                        solid.Box(rope, (p0 + p1) / 2f + Vector3.up * (WalkLift + .65f), new Vector3(.14f, 1.3f, Vector3.Distance(p0, p1) + .1f), Quaternion.LookRotation(p1 - p0));
                    }
                }
            }
        }

        /// Lanterns on posts along the walks, every 20-odd metres, on alternate sides.
        void Lamps(LandContext ctx, MeshKit mk)
        {
            var lamps = new List<Vector3>();
            for (var wi = 0; wi < walks.Count; wi++)
            {
                var pts = walks[wi];
                for (var i = 2; i < pts.Count; i += 4)
                {
                    var flat = pts[Mathf.Min(i + 1, pts.Count - 1)] - pts[i - 1]; flat.y = 0; flat.Normalize();
                    var side = Vector3.Cross(Vector3.up, flat);
                    var post = pts[i] + side * ((i / 4) % 2 == 0 ? 1.9f : -1.9f);
                    var near = false;
                    foreach (var o in lamps) if ((o - post).sqrMagnitude < 20f * 20f) { near = true; break; }
                    if (near || NearOtherWalk(post, wi, WalkW / 2f + .5f) || InFootprint(ctx, post.x, post.z, 0f, .6f)) continue;
                    lamps.Add(post);
                    var g = ctx.OnGround(post.x, post.z);
                    var lamp = ArtistBits.LanternPost(mk, ctx.Root, g, (pts[i] - post).normalized, driftDark, iron, Ember, 2.6f);
                    halos.Add(lamp.transform.position, 1.4f, Ember, .9f);
                    if (lamps.Count % 3 == 0) ArtistBits.PointLight(ctx.Root, lamp.transform.position - Vector3.up * .3f, Ember, 10f, 1.2f);
                }
            }
        }

        // ---------------------------------------------------------------- the arch and the lagoon
        /// At the heart: a still lagoon ringed by boardwalk, with a footbridge across it toward the sea and a
        /// curved arch of bleached driftwood standing over the bridge, its feet in the water on piles of stones.
        /// Sea-glass floats hang from it and a ship's bell at the top; lanterns burn at its feet.
        void ArchAndLagoon(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var c = ctx.OnGround(center.x, center.z);
            var sea = seaDir; var across = Vector3.Cross(Vector3.up, sea); var up = Vector3.up;
            mk.Prism(water, c + up * .05f, up, LagoonR, .04f, 40);
            mk.Prism(WorldMaterials.Solid(WetSand), c + up * .03f, up, LagoonR + 3f, .03f, 40);
            for (var k = 0; k < 46; k++)
            {
                var a = ctx.Range(0, Mathf.PI * 2f); var r = ctx.Range(LagoonR + .5f, LagoonR + 3.2f);
                var dirk = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                if (Mathf.Abs(Vector3.Dot(dirk, across)) < .2f) continue;            // clear where the bridge lands
                var p = ctx.OnGround(c.x + dirk.x * r, c.z + dirk.z * r);
                for (var j = 0; j < 4; j++) soft.Box(seaweed, p + new Vector3(ctx.Range(-.3f, .3f), .7f, ctx.Range(-.3f, .3f)), new Vector3(.04f, 1.4f, .04f), Quaternion.Euler(ctx.Range(-12, 12), 0, ctx.Range(-12, 12)));
            }
            noGrass.Add(new Vector3(c.x, RingR + WalkW, c.z));

            // the ring of boardwalk, railed on the lagoon side
            var ring = new List<Vector3>();
            const int ringN = 48;
            for (var k = 0; k <= ringN; k++)
            {
                var a = k * Mathf.PI * 2f / ringN;
                var q = c + (sea * Mathf.Cos(a) + across * Mathf.Sin(a)) * RingR;
                ring.Add(new Vector3(q.x, c.y, q.z));
            }
            ring[ringN] = ring[0];
            AddWalk(ring, 3);
            // the footbridge across, humped a little, railed both sides
            var bridge = new List<Vector3>();
            var half = RingR - WalkW / 2f - .1f;
            for (var d = -half; d <= half + .01f; d += 2f)
            {
                var t = (d + half) / (2f * half);
                bridge.Add(c + sea * d + up * Mathf.Sin(t * Mathf.PI) * .7f);
            }
            AddWalk(bridge, 2);

            // the arch: a half circle of overlapping driftwood logs in two layers, lashed with rope
            Vector3 P(float a, float r) => c + across * Mathf.Cos(a) * r + up * Mathf.Sin(a) * r;
            const int segs = 11;
            for (var layer = 0; layer < 2; layer++)
            {
                var off = sea * (layer == 0 ? -.45f : .45f);
                var r = ArchR + (layer == 0 ? 0f : -.35f);
                for (var i = 0; i < segs; i++)
                {
                    float a0 = Mathf.PI * (i - .15f) / segs, a1 = Mathf.PI * (i + 1.15f) / segs;
                    if (layer == 1) { a0 += Mathf.PI * .5f / segs; a1 += Mathf.PI * .5f / segs; }
                    a0 = Mathf.Max(a0, -.05f); a1 = Mathf.Min(a1, Mathf.PI + .05f);
                    var p0 = P(a0, r) + off + up * ctx.Range(-.1f, .1f); var p1 = P(a1, r) + off;
                    var d = p1 - p0;
                    mk.Prism(layer == 0 ? drift : driftDark, p0, d.normalized, ctx.Range(.42f, .58f) * (layer == 0 ? 1f : .8f), d.magnitude, 7, ctx.Range(.7f, .95f));
                }
            }
            for (var i = 1; i < segs; i += 2)
            {
                var a = Mathf.PI * i / segs;
                var tangent = (-across * Mathf.Sin(a) + up * Mathf.Cos(a)).normalized;
                mk.Prism(rope, P(a, ArchR - .15f) - tangent * .25f, tangent, .62f, .5f, 10);
            }
            // a ship's bell at the top and sea-glass floats hanging along the curve
            var brass = WorldMaterials.Solid(new Color(.78f, .6f, .3f), .8f);
            var apex = P(Mathf.PI / 2f, ArchR - .6f);
            mk.Box(rope, apex - up * .4f, new Vector3(.03f, .8f, .03f), Quaternion.identity);
            mk.Prism(brass, apex - up * 1.4f, up, .42f, .6f, 12, .55f);
            mk.Blob(brass, apex - up * 1.5f, Vector3.one * .08f, ctx.Rng, 0f);
            var glassCol = new Color(.5f, .9f, .85f);
            var glassMat = WorldMaterials.Glow(glassCol, 1.8f);
            for (var k = 0; k < 8; k++)
            {
                var a = Mathf.Lerp(.35f, Mathf.PI - .35f, (k + .5f) / 8f);
                if (Mathf.Abs(a - Mathf.PI / 2f) < .2f) continue;
                var top = P(a, ArchR - .55f); var len = ctx.Range(1f, 2.6f);
                mk.Box(rope, top - up * len / 2f, new Vector3(.02f, len, .02f), Quaternion.identity);
                mk.Blob(k % 3 == 0 ? WorldMaterials.Glow(SeaGlass[k % SeaGlass.Length], 1.8f) : glassMat, top - up * (len + .25f), Vector3.one * .26f, ctx.Rng, .05f);
                halos.Add(top - up * (len + .25f), 1.3f, glassCol, .8f);
            }
            // stone piles at the feet, in the water, with a lantern on each
            foreach (var s in new[] { -1f, 1f })
            {
                var foot = c + across * s * ArchR;
                for (var k = 0; k < 9; k++)
                    mk.Blob(stone, foot + new Vector3(ctx.Range(-1.3f, 1.3f), ctx.Range(0f, .5f), ctx.Range(-1.3f, 1.3f)), new Vector3(ctx.Range(.5f, .9f), ctx.Range(.35f, .6f), ctx.Range(.5f, .9f)), ctx.Rng, .25f);
                for (var r = 0; r < 5; r++)
                {
                    var rd = (across * s * ctx.Range(.3f, 1f) + new Vector3(ctx.Range(-.8f, .8f), ctx.Range(-.2f, .3f), ctx.Range(-.8f, .8f))).normalized;
                    mk.Prism(drift, foot + up * .5f, rd, .16f, ctx.Range(1.4f, 2.4f), 5, .3f);
                }
                var lantern = foot + up * 1.3f - across * s * 1.3f;
                mk.Box(iron, lantern - up * .45f, new Vector3(.06f, .9f, .06f), Quaternion.identity);
                mk.Box(WorldMaterials.Glow(Ember, 2.4f), lantern + up * .15f, new Vector3(.3f, .4f, .3f), Quaternion.identity);
                mk.Cone(iron, lantern + up * .36f, .26f, .25f, 4, 45f);
                halos.Add(lantern + up * .15f, 1.8f, Ember, 1f);
                ArtistBits.PostCollider(ctx.Root, foot, 3f, 2.1f);   // wide enough for the stone pile and the sticks
            }
            ArtistBits.PointLight(ctx.Root, c + up * 4f, Ember, 22f, 1.3f);
            life.archAt = c + up * 15f; life.seaDir = sea;
        }
    }
}
