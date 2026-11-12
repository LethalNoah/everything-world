using System.Collections.Generic;
using UnityEngine;

namespace EverythingWorld.World
{
    /// The Wisp Swamp, the Bayou's supernatural corner: out along the long northwest arm of the land the water
    /// spreads into a still, flooded cypress backwater. A plank boardwalk winds through it on posts. Blue-green
    /// will-o'-the-wisps drift over the water, and a few float ahead of you along the boardwalk as if leading you
    /// (BayouLife). Half sunk beside the boardwalk lies the wreck of an old riverboat, moss hanging off it. At the
    /// far end, on its own little island, stands the oldest thing in the Bayou: a giant cypress whose hollow glows.
    /// "In Memory of Elizabeth Reed" gathers every wisp into a swirl round the old tree.
    public partial class BayouKit
    {
        // where the swamp is (ShapeGround)
        readonly List<Vector3> swampSpine = new List<Vector3>();      // (x, basin radius, z) from the entry to the far end
        readonly List<(Vector3 c, float r)> swampPools = new List<(Vector3, float)>();
        float swampY; bool hasSwamp; Vector2 oldTree;

        bool InlandAt(IslandWorldData data, int index, Vector2 p, float m)
        {
            if (data.IslandAt(p.x, p.y) != data.layout.islands[index]) return false;
            for (var k = 0; k < 12; k++) { var a = k * Mathf.PI / 6f; if (data.IslandAt(p.x + Mathf.Cos(a) * m, p.y + Mathf.Sin(a) * m) != data.layout.islands[index]) return false; }
            return true;
        }

        /// Find the long arm of land running away from the heart (the northwest arm, next to the Jam Band
        /// Commonwealth) and trace its middle: along it, the point farthest from any edge every 25 m.
        void FindSwamp(IslandWorldData data, int index, Vector2 heart)
        {
            swampSpine.Clear(); swampPools.Clear(); hasSwamp = false;
            var u = new Vector2(-320f, 380f).normalized;                 // the arm's direction from where you arrive
            var best = new Dictionary<int, (Vector2 p, float inland)>();
            for (var r = 0; r < data.Nz; r += 2)
            {
                var z = data.layout.depth - r * data.Step; if (z < heart.y - 100f || z > heart.y + 800f) continue;
                for (var col = 0; col < data.Nx; col += 2)
                {
                    var x = col * data.Step; if (x < heart.x - 700f || x > heart.x + 200f) continue;
                    if (data.islandIds[r * data.Nx + col] != index) continue;
                    var p = new Vector2(x, z); var proj = Vector2.Dot(p - heart, u);
                    if (proj < 400f || proj > 760f) continue;
                    var bin = Mathf.FloorToInt(proj / 25f);
                    var inland = 0f; for (var m = 20f; m <= 90f; m += 5f) { if (InlandAt(data, index, p, m)) inland = m; else break; }
                    if (!best.TryGetValue(bin, out var b) || inland > b.inland) best[bin] = (p, inland);
                }
            }
            var bins = new List<int>(best.Keys); bins.Sort();
            foreach (var b in bins)
            {
                var (p, inland) = best[b];
                if (inland < 38f) { if (swampSpine.Count > 3) break; continue; }
                swampSpine.Add(new Vector3(p.x, Mathf.Min(inland - 18f, 30f), p.y));
            }
            if (swampSpine.Count < 4) return;
            // the water sits a little under the lowest ground along the arm
            swampY = float.MaxValue;
            foreach (var s in swampSpine) swampY = Mathf.Min(swampY, data.HeightAt(s.x, s.z));
            swampY -= .6f;
            // start the swamp past any high ground at the near end, so it doesn't cut a deep valley
            while (swampSpine.Count > 4 && data.HeightAt(swampSpine[0].x, swampSpine[0].z) > swampY + 7f) swampSpine.RemoveAt(0);
            if (swampSpine.Count < 4 || swampY < seaLevel + 1f) return;
            hasSwamp = true;
            var last = swampSpine[swampSpine.Count - 1]; oldTree = new Vector2(last.x, last.z);
            foreach (var s in swampSpine) { var pc = new Vector3(s.x, swampY, s.z); ponds.Add((pc, s.y)); swampPools.Add((pc, s.y)); }
            // the gaps between pools along the arm, so the water is one long backwater
            for (var i = 0; i + 1 < swampSpine.Count; i++)
            {
                var a = swampSpine[i]; var b = swampSpine[i + 1]; var m = (a + b) / 2f; var rr = Mathf.Min(a.y, b.y) * .85f;
                var pc = new Vector3(m.x, swampY, m.z); ponds.Add((pc, rr)); swampPools.Add((pc, rr));
            }
        }

        /// After digging: the old tree's little island rises out of the water.
        void RaiseTreeIsland(IslandWorldData data, int index)
        {
            if (!hasSwamp) return;
            for (var r = 0; r < data.Nz; r++)
            {
                var z = data.layout.depth - r * data.Step; if (Mathf.Abs(z - oldTree.y) > 16f) continue;
                for (var col = 0; col < data.Nx; col++)
                {
                    var x = col * data.Step; if (Mathf.Abs(x - oldTree.x) > 16f) continue;
                    var i = r * data.Nx + col; if (data.islandIds[i] != index) continue;
                    var d = Vector2.Distance(new Vector2(x, z), oldTree);
                    if (d < 8f) data.heights[i] = swampY + 1.1f - d * .03f;
                    else if (d < 14f) data.heights[i] = Mathf.Max(data.heights[i], Mathf.Lerp(swampY + .9f, swampY - .9f, (d - 8f) / 6f));
                }
            }
        }

        void Swamp(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            if (!hasSwamp) return;
            var up = Vector3.up;
            var first = swampSpine[0]; var dirIn = new Vector3(swampSpine[1].x - first.x, 0, swampSpine[1].z - first.z).normalized;
            var tree = new Vector3(oldTree.x, swampY + 1.1f, oldTree.y);

            // the boardwalk: from the bank before the first pool, weaving from side to side to the old tree's island
            var route = new List<Vector3>();
            var entry = new Vector3(first.x, 0, first.z) - dirIn * (first.y + 14f);
            route.Add(entry);
            for (var i = 0; i < swampSpine.Count - 1; i++)
            {
                var s = swampSpine[i]; var nx = swampSpine[i + 1];
                var dir = new Vector3(nx.x - s.x, 0, nx.z - s.z).normalized; var side = Vector3.Cross(up, dir);
                route.Add(new Vector3(s.x, 0, s.z) + side * s.y * .35f * (i % 2 == 0 ? 1f : -1f));
            }
            var toTree = (tree - route[route.Count - 1]); toTree.y = 0; var stop = tree - toTree.normalized * 8.5f;
            route.Add(new Vector3(stop.x, 0, stop.z));
            var walk = new List<Vector3>();
            for (var i = 0; i + 1 < route.Count; i++)
            {
                var a = route[i]; var b = route[i + 1]; var n = Mathf.Max(1, Mathf.CeilToInt(Vector3.Distance(a, b) / 2.5f));
                for (var k = 0; k < n; k++)
                {
                    var q = Vector3.Lerp(a, b, k / (float)n);
                    var g = ctx.Ground(q.x, q.z);
                    walk.Add(new Vector3(q.x, Mathf.Max(g + .12f, swampY + .55f), q.z));
                }
            }
            walk.Add(new Vector3(stop.x, Mathf.Max(ctx.Ground(stop.x, stop.z) + .12f, swampY + .55f), stop.z));
            Strip(mk, walk, greyWood, 2.6f, 0f);
            for (var i = 0; i + 1 < walk.Count; i++)
            {
                var a = walk[i]; var b = walk[i + 1]; var d = b - a; var flat = new Vector3(d.x, 0, d.z).normalized; var side = Vector3.Cross(up, flat);
                solid.Box(plank, (a + b) / 2f - up * .25f, new Vector3(2.6f, .5f, d.magnitude + .1f), Quaternion.LookRotation(d));
                // planks across, posts down into the water, a low rope rail on the outside of each bend
                mk.Box(plank, (a + b) / 2f + up * .02f, new Vector3(2.7f, .05f, .3f), Quaternion.LookRotation(flat));
                if (i % 2 == 0) foreach (var sd in new[] { -1.25f, 1.25f }) { var post = a + side * sd; var g = ctx.Ground(post.x, post.z); if (post.y - g > .4f) { mk.Prism(greyWood, new Vector3(post.x, g - .4f, post.z), up, .1f, post.y - g + 1.3f, 6); ArtistBits.PostCollider(ctx.Root, new Vector3(post.x, g, post.z), post.y - g + .9f, .12f); } }
                if (i % 2 == 0 && i > 2)
                {
                    var rs = (i / 6) % 2 == 0 ? 1.25f : -1.25f;
                    var ra = a + side * rs + up * .9f; var rb = b + side * rs + up * .9f;
                    mk.Box(rope, (ra + rb) / 2f, new Vector3(.05f, .05f, Vector3.Distance(ra, rb)), Quaternion.LookRotation(rb - ra));
                    solid.Box(rope, (ra + rb) / 2f - up * .4f, new Vector3(.12f, 1f, Vector3.Distance(ra, rb)), Quaternion.LookRotation(rb - ra));
                }
            }
            foreach (var p in walk) Remember(p, 996);
            Footpath(ctx, mk, solid, entry, 0f);
            life.swampPath = walk.ToArray();
            life.swampY = swampY; life.oldTree = tree;
            foreach (var (c, r) in swampPools) life.swampPools.Add(new Vector3(c.x, r, c.z));
            // lanterns gone cold: rusted hooks on tall posts along the way, each with a faint blue-green glow
            for (var i = 6; i < walk.Count - 4; i += 12)
            {
                var a = walk[i]; var b = walk[i + 1]; var side = Vector3.Cross(up, new Vector3(b.x - a.x, 0, b.z - a.z).normalized);
                var lp = a + side * 1.5f * ((i / 12) % 2 == 0 ? 1f : -1f); var g = ctx.Ground(lp.x, lp.z);
                mk.Prism(greyWood, new Vector3(lp.x, g - .4f, lp.z), up, .1f, a.y - g + 3f, 6); ArtistBits.PostCollider(ctx.Root, new Vector3(lp.x, g, lp.z), a.y - g + 2.6f, .12f);
                var glass = new Vector3(lp.x, a.y + 2.3f, lp.z);
                mk.Box(GlowM(new Color(.4f, 1f, .85f), 2.2f), glass, new Vector3(.25f, .35f, .25f), Quaternion.identity);
                halos.Add(glass, 1.3f, new Color(.4f, 1f, .85f), .9f);
            }

            // cypress standing out in the water, thick with moss, so the backwater feels enclosed
            var placed = 0;
            for (var tries = 0; tries < 400 && placed < 34; tries++)
            {
                var (c, r) = swampPools[ctx.Rng.Next(swampPools.Count)];
                var a = ctx.Range(0, Mathf.PI * 2f); var rr = ctx.Range(.2f, 1.05f) * r;
                var q = new Vector3(c.x + Mathf.Cos(a) * rr, 0, c.z + Mathf.Sin(a) * rr);
                if (NearPath(q.x, q.z, 4.5f) || Vector2.Distance(new Vector2(q.x, q.z), oldTree) < 16f) continue;
                q.y = Mathf.Min(ctx.Ground(q.x, q.z), swampY);
                CypressTree(ctx, mk, q, ctx.Range(1f, 1.5f), swampY);
                placed++;
            }
            // lily pads and pale flowers on the still water
            var pad = WorldMaterials.Solid(new Color(.3f, .45f, .24f)); var flower = WorldMaterials.Solid(new Color(.85f, .95f, 1f));
            for (var k = 0; k < 160; k++)
            {
                var (c, r) = swampPools[ctx.Rng.Next(swampPools.Count)];
                var a = ctx.Range(0, Mathf.PI * 2f); var rr = ctx.Range(0f, .95f) * r;
                var q = new Vector3(c.x + Mathf.Cos(a) * rr, swampY - .07f, c.z + Mathf.Sin(a) * rr);
                if (NearPath(q.x, q.z, 1.8f) || ctx.Ground(q.x, q.z) > swampY - .1f) continue;
                soft.Prism(pad, q, up, ctx.Range(.35f, .7f), .02f, 8);
                if (ctx.Chance(.2f)) soft.Blob(flower, q + up * .1f, new Vector3(.16f, .1f, .16f), ctx.Rng, .3f);
            }

            Wreck(ctx, mk, solid, walk);
            OldCypress(ctx, mk, solid, tree, walk[walk.Count - 1]);

            // where the photo tour looks from: on the boardwalk, toward the old tree
            var mid = walk[walk.Count * 2 / 3]; var look = tree - mid; look.y = 0;
            var mark = new GameObject("Shot: the Wisp Swamp").transform; mark.SetParent(ctx.Root, false);
            mark.SetPositionAndRotation(mid + up * .1f, Quaternion.LookRotation(look));
            var near = walk[Mathf.Max(0, walk.Count - 7)];
            var mark2 = new GameObject("Shot: the old cypress").transform; mark2.SetParent(ctx.Root, false);
            mark2.SetPositionAndRotation(near + up * .1f, Quaternion.LookRotation(look));
            noGrass.Add(new Vector3(tree.x, 9f, tree.z));
        }

        /// The wreck of an old riverboat, half sunk and listing in the backwater: a broken hull, the remains of a
        /// cabin, one stack fallen across it, the paddle wheel's ribs, moss hanging everywhere.
        void Wreck(LandContext ctx, MeshKit mk, MeshKit solid, List<Vector3> walk)
        {
            var up = Vector3.up; var i = walk.Count / 2; var a = walk[i]; var b = walk[Mathf.Min(i + 1, walk.Count - 1)];
            var along = new Vector3(b.x - a.x, 0, b.z - a.z).normalized; var side = Vector3.Cross(up, along);
            // beside the boardwalk, in the deepest open water nearby
            var at = new Vector3(a.x, swampY, a.z) + side * 11f;
            if (ctx.Ground(at.x, at.z) > swampY - .3f) at = new Vector3(a.x, swampY, a.z) - side * 11f;
            var rot = Quaternion.LookRotation(Quaternion.Euler(0, 25f, 0) * along) * Quaternion.Euler(4f, 0, 14f);
            var hull = WorldMaterials.Solid(new Color(.55f, .52f, .46f)); var rotWood = WorldMaterials.Solid(new Color(.35f, .3f, .25f));
            Vector3 P(float x, float y, float z) => at + rot * new Vector3(x, y, z);
            mk.Box(hull, P(0, -.2f, 0), new Vector3(6f, 1.8f, 18f), rot);
            mk.Box(rotWood, P(0, .72f, 0), new Vector3(6.2f, .15f, 18.2f), rot);
            mk.Box(hull, P(0, 1.9f, -1f), new Vector3(4.6f, 2.2f, 9f), rot);
            mk.Box(rotWood, P(0, 3.05f, -1f), new Vector3(5.6f, .15f, 10f), rot * Quaternion.Euler(0, 0, -6f));
            for (var k = 0; k < 4; k++) mk.Box(WorldMaterials.Solid(new Color(.08f, .1f, .1f)), P(2.31f, 2f, -4f + k * 2.2f), new Vector3(.02f, .9f, .9f), rot);   // empty windows
            mk.Prism(WorldMaterials.Solid(new Color(.12f, .1f, .09f)), P(-1f, 3.1f, 3f), (rot * new Vector3(-1f, .25f, .6f)).normalized, .35f, 7f, 10);              // the fallen stack
            for (var k = 0; k < 8; k++) { var rr = Quaternion.AngleAxis(k * 45f, rot * Vector3.right); mk.Box(rotWood, P(0, .6f, -10.2f) + rr * (rot * Vector3.up) * 1.6f, new Vector3(4.6f, .25f, .12f), rr * rot); }
            for (var k = 0; k < 24; k++) { var p = P(ctx.Range(-2.8f, 2.8f), ctx.Range(1f, 3f), ctx.Range(-8f, 8f)); HangMoss(ctx, p, ctx.Range(.8f, 2.2f)); }
            solid.Box(hull, P(0, .8f, 0), new Vector3(6f, 4f, 18f), rot);
            var glow = ArtistBits.PointLight(ctx.Root, P(0, 2f, -1f), new Color(.4f, 1f, .85f), 10f, 1.2f);
            life.wreck = P(0, 2f, -1f);
        }

        /// The oldest thing in the Bayou: a giant cypress on its own island, its buttressed foot as wide as a
        /// room, a hollow in it facing the boardwalk that glows blue-green from inside, moss hanging in curtains.
        void OldCypress(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 at, Vector3 from)
        {
            var up = Vector3.up; var face = new Vector3(from.x - at.x, 0, from.z - at.z).normalized; var rot = Quaternion.LookRotation(face);
            var oldBark = WorldMaterials.Solid(new Color(.36f, .31f, .27f));
            // the foot: buttresses flaring out all round, leaving the hollow open at the front
            for (var k = 0; k < 9; k++)
            {
                var d = Quaternion.Euler(0, k * 40f + 20f, 0) * face;
                if (Vector3.Dot(d, face) > .85f) continue;
                mk.Prism(oldBark, at + d * 3.2f - up * .5f, (-d * .55f + up).normalized, 1.3f, 6f, 6, .35f);
            }
            mk.Prism(oldBark, at + up * 3f - face * .6f, up, 2.6f, 4f, 10, .75f);
            mk.Prism(oldBark, at + up * 6.8f - face * .6f, up, 2f, 22f, 10, .5f);
            // the hollow: a tall dark opening with the glow inside
            mk.Box(WorldMaterials.Solid(new Color(.05f, .06f, .06f)), at + up * 1.8f + face * 1.2f, new Vector3(1.8f, 3.4f, .1f), rot);
            mk.Box(GlowM(new Color(.4f, 1f, .85f), 2.2f), at + up * 1.6f + face * 1.1f, new Vector3(1.2f, 2.6f, .05f), rot);
            halos.Add(at + up * 1.8f + face * 2.2f, 2.6f, new Color(.4f, 1f, .85f), .9f);
            ArtistBits.PointLight(ctx.Root, at + up * 2f + face * 2f, new Color(.45f, 1f, .85f), 14f, 1.8f);
            // great limbs and flat crowns high up, moss in curtains
            var crownLeaf = WorldMaterials.Solid(new Color(.33f, .44f, .24f));
            for (var k = 0; k < 7; k++)
            {
                var d = Quaternion.Euler(0, k * 51f, 0) * Vector3.forward; var y = 16f + (k % 3) * 4f;
                var limbDir = (d + up * .35f).normalized;
                mk.Prism(oldBark, at + up * y - face * .6f, limbDir, .55f, 9f, 6, .4f);
                var tip = at + up * y - face * .6f + limbDir * 9f;
                soft.Blob(crownLeaf, tip + up * 1f, new Vector3(6f, 1.6f, 6f), ctx.Rng, .35f);
                for (var m = 0; m < 9; m++) HangMoss(ctx, Vector3.Lerp(at + up * y, tip, ctx.Range(.3f, 1f)) - up * .3f, ctx.Range(2.5f, 5.5f));
            }
            soft.Blob(crownLeaf, at + up * 30f, new Vector3(8f, 2.2f, 8f), ctx.Rng, .35f);
            // the hollow is walkable: colliders round the foot but not across the opening
            for (var k = 0; k < 9; k++)
            {
                var d = Quaternion.Euler(0, k * 40f + 20f, 0) * face; if (Vector3.Dot(d, face) > .6f) continue;
                solid.Box(oldBark, at + d * 2.6f + up * 2f, new Vector3(2.4f, 5f, 1.2f), Quaternion.LookRotation(d));
            }
            solid.Prism(oldBark, at + up * 4f - face * 1.2f, up, 1.8f, 24f, 8);
            ArtistBits.Label(ctx.Root, "THE OLD ONE", ctx.OnGround(from.x, from.z) + up * 1.5f + Quaternion.Euler(0, 90f, 0) * face * 2f, face, .18f, new Color(.7f, 1f, .9f), TextAnchor.MiddleCenter);
        }
    }
}
