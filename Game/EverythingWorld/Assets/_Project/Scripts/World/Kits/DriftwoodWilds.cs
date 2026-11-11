using System.Collections.Generic;
using UnityEngine;

namespace EverythingWorld.World
{
    /// The Driftwood Shore beyond the heart: wild dune and heath with things to find all over it. Wind-bent trees
    /// leaning away from the sea, sand fences half buried on the dunes, cairns with a piece of sea glass glowing on
    /// top, lone benches with a lantern facing the view, upturned boats made into sheds with a lit window, a
    /// ruined cottage with a lamp still burning inside. Along the promenade: a signpost, benches, and a lifeguard
    /// tower where it reaches the beach.
    public partial class DriftwoodKit
    {
        void Wilds(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            Promenade(ctx, mk, solid);
            var leaf = WorldMaterials.Solid(new Color(.34f, .4f, .34f)); var leafB = WorldMaterials.Solid(new Color(.42f, .47f, .4f));
            var bark = WorldMaterials.Solid(new Color(.36f, .32f, .3f));
            int sheds = 0, ruins = 0, benches = 0, cairns = 0, trees = 0, fences = 0, nets = 0, pots = 0;
            const float cell = 46f;
            for (var x = ctx.MinX; x < ctx.MaxX; x += cell)
            for (var z = ctx.MinZ; z < ctx.MaxZ; z += cell)
            {
                float px = x + ctx.Range(0f, cell), pz = z + ctx.Range(0f, cell);
                if (!Open(ctx, px, pz, 6f)) continue;
                var near = Vector2.Distance(new Vector2(px, pz), new Vector2(center.x, center.z)) < RingR + 12f;
                if (near) continue;
                var p = ctx.OnGround(px, pz);
                var sea = SeaFrom(ctx.Data, p, out var dist, 160f);
                if (dist > 1000f) sea = seaDir;
                var roll = ctx.Range(0f, 1f);
                if (roll < .36f) { Trees(ctx, mk, solid, p, sea, bark, leaf, leafB); trees++; }
                else if (roll < .5f && p.y < 7f) { SandFence(ctx, mk, solid, p, sea); fences++; }
                else if (roll < .58f) { Cairn(ctx, mk, p); cairns++; }
                else if (roll < .66f) { LoneBench(ctx, mk, solid, p, sea); benches++; }
                else if (roll < .7f && sheds < 4 && Open(ctx, px, pz, 9f)) { BoatShed(ctx, mk, solid, p, sea); sheds++; }
                else if (roll < .72f && ruins < 2 && Open(ctx, px, pz, 11f)) { Ruin(ctx, mk, solid, p, sea); ruins++; }
                else if (roll < .78f) { Trees(ctx, mk, solid, p, sea, bark, leaf, leafB); trees++; }
                else if (roll < .84f) { NetPoles(ctx, mk, solid, p, sea); nets++; }
                else if (roll < .9f) { PotStack(ctx, mk, solid, p); pots++; }
            }
            Debug.Log($"The Driftwood Shore wilds: {trees} tree groups, {fences} sand fences, {cairns} cairns, {benches} benches, {sheds} boat sheds, {ruins} ruins, {nets} nets, {pots} pot stacks");
        }

        // ---------------------------------------------------------------- along the promenade
        void Promenade(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            if (!hasShore || walks.Count < 3) return;
            var path = walks[2];                                     // ring, footbridge, then the promenade
            var side = Vector3.Cross(Vector3.up, seaDir);
            // a signpost where it leaves the ring
            var sign = path[0] - side * 2.6f;
            if (!InFootprint(ctx, sign.x, sign.z, 0f))
            {
                sign = ctx.OnGround(sign.x, sign.z);
                mk.Prism(driftDark, sign, Vector3.up, .1f, 2.8f, 6);
                var boardAt = sign + Vector3.up * 2.2f + seaDir * .7f;
                mk.Box(drift, boardAt, new Vector3(.06f, .42f, 1.6f), Quaternion.LookRotation(seaDir));
                mk.Tri(drift, boardAt + seaDir * .8f + Vector3.up * .21f, boardAt + seaDir * .8f - Vector3.up * .21f, boardAt + seaDir * 1.1f, boardAt);
                ArtistBits.Label(ctx.Root, "the sea", boardAt - side * .04f + Vector3.up * .1f, -side, .22f, new Color(.2f, .18f, .16f));
                ArtistBits.Label(ctx.Root, "the sea", boardAt + side * .04f + Vector3.up * .1f, side, .22f, new Color(.2f, .18f, .16f));
                ArtistBits.PostCollider(ctx.Root, sign, 2.8f, .12f);
            }
            // benches facing out every 70 m or so, on alternate sides
            for (var i = 18; i < path.Count - 10; i += 24)
            {
                var s = (i / 24) % 2 == 0 ? 1f : -1f;
                var at = path[i] + side * s * 3.2f;
                if (!ctx.IsLand(at.x, at.z, 1f) || InFootprint(ctx, at.x, at.z, 1f) || NearOtherWalk(at, 2, 3f)) continue;
                LoneBench(ctx, mk, solid, ctx.OnGround(at.x, at.z), side * s);
            }
            // a lifeguard tower where it reaches the beach
            // (beside the path, clear of the beach huts: it used to stand into one)
            foreach (var off in new[] { 7f, -7f, 11f, -11f, 15f, -15f })
            {
                var end = path[path.Count - 1] + side * off;
                if (!ctx.IsLand(end.x, end.z, 0f) || NearPath(end.x, end.z, 3f) || hutSpots.Exists(h => new Vector2(h.x - end.x, h.z - end.z).sqrMagnitude < 5f * 5f)) continue;
                LifeguardTower(ctx, mk, solid, ctx.OnGround(end.x, end.z), seaDir); break;
            }
        }

        void LifeguardTower(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 at, Vector3 sea)
        {
            var rot = Quaternion.LookRotation(sea); var side = Vector3.Cross(Vector3.up, sea);
            const float H = 3.6f;
            var white = WorldMaterials.Solid(new Color(.88f, .87f, .84f)); var red = WorldMaterials.Solid(new Color(.75f, .3f, .26f));
            foreach (var (a, b) in new[] { (-1f, -1f), (1f, -1f), (-1f, 1f), (1f, 1f) })
            {
                var foot = at + side * a * 1.4f + sea * b * 1.4f; var top = at + side * a * 1.1f + sea * b * 1.1f + Vector3.up * H;
                foot.y = ctx.Ground(foot.x, foot.z) - .3f;
                mk.Prism(white, foot, (top - foot).normalized, .1f, Vector3.Distance(foot, top), 6);
            }
            mk.Box(planks, at + Vector3.up * (H + .006f), new Vector3(2.8f, .14f, 2.8f), rot);   // 6 mm up: its deck met a beach hut's roof plane
            solid.Box(planks, at + Vector3.up * (H - .1f), new Vector3(2.8f, .34f, 2.8f), rot);
            var hut = at + Vector3.up * (H + 1f) - sea * .3f;
            mk.Box(red, hut, new Vector3(2.2f, 1.9f, 1.8f), rot);
            mk.GableRoof(white, hut + Vector3.up * .95f, new Vector3(2.2f, 0, 2.6f), .6f, rot);
            mk.Box(WorldMaterials.Glow(Ember, 2.2f), hut + sea * .91f + Vector3.up * .2f, new Vector3(1.4f, .6f, .03f), rot);
            halos.Add(hut + sea * 1.2f, 1.8f, Ember, .9f);
            // the ladder, and a red and yellow flag snapping in the wind
            var l0 = at - sea * 2.6f; l0.y = ctx.Ground(l0.x, l0.z); var l1 = at - sea * 1.4f + Vector3.up * H;
            foreach (var s in new[] { -.35f, .35f }) mk.Box(white, (l0 + l1) / 2f + side * s, new Vector3(.06f, .06f, Vector3.Distance(l0, l1)), Quaternion.LookRotation(l1 - l0));
            for (var k = 1; k < 9; k++) mk.Box(white, Vector3.Lerp(l0, l1, k / 9f), new Vector3(.7f, .05f, .05f), rot);
            var pole = at + side * 1.2f + sea * 1.1f + Vector3.up * H;
            mk.Prism(white, pole, Vector3.up, .04f, 2.4f, 5);
            var lee = -sea;
            mk.Tri(red, pole + Vector3.up * 2.4f, pole + Vector3.up * 1.6f, pole + Vector3.up * 2f + lee * 1.1f, pole + side);
            mk.Tri(WorldMaterials.Solid(new Color(.95f, .8f, .3f)), pole + Vector3.up * 2.4f, pole + Vector3.up * 1.6f, pole + Vector3.up * 2f + lee * 1.1f, pole - side);
            ArtistBits.PostCollider(ctx.Root, at, H, 1.5f);
            noGrass.Add(new Vector3(at.x, 4f, at.z));
            life.landmarks.Add(at);
        }

        // ---------------------------------------------------------------- the wilds
        /// One to three small trees, crooked and bent hard away from the sea, their crowns combed flat.
        void Trees(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 at, Vector3 sea, Material bark, Material leaf, Material leafB)
        {
            var lee = -sea; var n = ctx.Rng.Next(1, 4);
            for (var t = 0; t < n; t++)
            {
                var p = t == 0 ? at : ctx.OnGround(at.x + ctx.Range(-5f, 5f), at.z + ctx.Range(-5f, 5f));
                if (t > 0 && !Open(ctx, p.x, p.z, 2f)) continue;
                var h = ctx.Range(3f, 6.5f); var prev = p - Vector3.up * .3f; var r = ctx.Range(.16f, .26f);
                for (var k = 0; k < 4; k++)
                {
                    var bend = k / 3f;
                    var d = (Vector3.up * (1f - bend * .55f) + lee * (.2f + bend * .9f) + new Vector3(ctx.Range(-.15f, .15f), 0, ctx.Range(-.15f, .15f))).normalized;
                    var len = h / 4f + .15f;
                    mk.Prism(bark, prev, d, r * (1f - k * .18f), len, 6, .85f);
                    solid.Box(bark, prev + d * len / 2f, new Vector3(r * 2f + .2f, len, r * 2f + .2f), Quaternion.FromToRotation(Vector3.up, d));   // the trunk, leaning
                    prev += d * len;
                }
                var crown = prev + lee * .6f;
                for (var k = 0; k < 4; k++)
                {
                    var c = crown + lee * (k * .7f - .5f) + new Vector3(ctx.Range(-.5f, .5f), ctx.Range(-.3f, .3f), ctx.Range(-.5f, .5f));
                    soft.Blob(k % 2 == 0 ? leaf : leafB, c, new Vector3(ctx.Range(1f, 1.6f), ctx.Range(.45f, .7f), ctx.Range(1f, 1.6f)), ctx.Rng, .25f);
                }
                ArtistBits.PostCollider(ctx.Root, p, h * .7f, r + .1f);
                noGrass.Add(new Vector3(p.x, .8f, p.z));
            }
        }

        /// A run of weathered slats along a dune, some leaning, wired together, half buried by drifted sand.
        void SandFence(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 at, Vector3 sea)
        {
            var along = Quaternion.Euler(0, ctx.Range(-25f, 25f), 0) * Vector3.Cross(Vector3.up, sea);
            var n = ctx.Rng.Next(16, 34); var bow = ctx.Range(-.04f, .04f);
            var start = at - along * n * .18f;
            Vector3 prev = default;
            for (var k = 0; k < n; k++)
            {
                if (ctx.Chance(.08f)) continue;                                      // a gap where one blew away
                var q = start + along * k * .36f + Vector3.Cross(Vector3.up, along) * (k - n / 2f) * (k - n / 2f) * bow * .1f;
                q = ctx.OnGround(q.x, q.z);
                var h = ctx.Range(.8f, 1.2f);
                mk.Box(k % 5 == 0 ? driftDark : drift, q + Vector3.up * (h / 2f - .15f), new Vector3(.09f, h, .03f), Quaternion.LookRotation(Vector3.Cross(Vector3.up, along)) * Quaternion.Euler(ctx.Range(-8f, 8f), 0, ctx.Range(-10f, 10f)));
                if (k > 0 && prev != default)
                    foreach (var y in new[] { .2f, .65f })
                        mk.Box(iron, (prev + q) / 2f + Vector3.up * y, new Vector3(.015f, .015f, Vector3.Distance(prev, q)), Quaternion.LookRotation(q - prev));
                prev = q;
            }
            var mid = ctx.OnGround(at.x, at.z);
            solid.Box(drift, mid + Vector3.up * .5f, new Vector3(.2f, 1f, n * .36f), Quaternion.LookRotation(along));
            mk.Blob(WorldMaterials.Solid(DuneSand), mid, new Vector3(n * .2f, .35f, 1f), ctx.Rng, .2f);
            noGrass.Add(new Vector3(at.x, n * .2f, at.z));
        }

        /// A fishing net hung to dry between three leaning poles, sagging, with cork floats along its top.
        void NetPoles(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 at, Vector3 sea)
        {
            var along = Quaternion.Euler(0, ctx.Range(-50f, 50f), 0) * Vector3.Cross(Vector3.up, sea);
            var net = WorldMaterials.Solid(new Color(.3f, .36f, .38f)); var cork = WorldMaterials.Solid(new Color(.75f, .55f, .35f));
            var tops = new Vector3[3];
            for (var k = 0; k < 3; k++)
            {
                var p = at + along * (k - 1) * 3.2f; p = ctx.OnGround(p.x, p.z);
                var h = ctx.Range(2.2f, 2.8f);
                var d = (Vector3.up + new Vector3(ctx.Range(-.08f, .08f), 0, ctx.Range(-.08f, .08f))).normalized;
                mk.Prism(driftDark, p - Vector3.up * .3f, d, .08f, h + .3f, 6);
                ArtistBits.PostCollider(ctx.Root, p, h, .1f);
                tops[k] = p - Vector3.up * .3f + d * (h + .2f);
            }
            for (var k = 0; k < 2; k++)
            {
                var a = tops[k]; var b = tops[k + 1];
                const int n = 10;
                for (var i = 0; i < n; i++)
                {
                    float f0 = i / (float)n, f1 = (i + 1) / (float)n;
                    Vector3 T(float f) => Vector3.Lerp(a, b, f) - Vector3.up * Mathf.Sin(f * Mathf.PI) * .5f;
                    var drop = 1.5f + Mathf.Sin(f0 * Mathf.PI) * .3f;
                    mk.Quad(net, T(f0), T(f1), T(f1) - Vector3.up * drop, T(f0) - Vector3.up * drop, T(f0) + sea);
                    mk.Quad(net, T(f0), T(f1), T(f1) - Vector3.up * drop, T(f0) - Vector3.up * drop, T(f0) - sea);
                    if (i % 2 == 0) mk.Blob(cork, T(f0), new Vector3(.1f, .07f, .07f), ctx.Rng, .1f);
                }
            }
            noGrass.Add(new Vector3(at.x, 3.5f, at.z));
        }

        /// Old crab pots stacked by a post, a coil of rope and a red buoy hanging off them.
        void PotStack(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 at)
        {
            var pot = WorldMaterials.Solid(new Color(.32f, .32f, .34f)); var mesh = WorldMaterials.Solid(new Color(.5f, .48f, .42f));
            var rot = Quaternion.Euler(0, ctx.Range(0, 360f), 0);
            var n = ctx.Rng.Next(3, 7);
            for (var k = 0; k < n; k++)
            {
                var layer = k < 3 ? 0 : k < 5 ? 1 : 2; var pos = k < 3 ? k - 1f : k < 5 ? k - 3.5f : 0f;
                var p = at + rot * new Vector3(pos * 1f, .3f + layer * .6f, 0) + new Vector3(ctx.Range(-.05f, .05f), 0, ctx.Range(-.05f, .05f));
                var r = rot * Quaternion.Euler(0, ctx.Range(-8f, 8f), 0);
                mk.Box(pot, p, new Vector3(.9f, .58f, .6f), r);
                mk.Box(mesh, p + Vector3.up * .3f, new Vector3(.8f, .02f, .5f), r);
            }
            solid.Box(pot, at + Vector3.up * .9f, new Vector3(3f, 1.8f, .7f), rot);
            mk.Prism(rope, at + rot * new Vector3(1.9f, 0, .3f), Vector3.up, .35f, .25f, 10);
            mk.Blob(WorldMaterials.Solid(new Color(.8f, .3f, .25f)), at + rot * new Vector3(1.6f, .9f, -.35f), Vector3.one * .25f, ctx.Rng, 0f);
            noGrass.Add(new Vector3(at.x, 2.2f, at.z));
        }

        /// Flat stones stacked by someone, with a piece of sea glass glowing on the top.
        void Cairn(LandContext ctx, MeshKit mk, Vector3 at)
        {
            var y = 0f; var n = ctx.Rng.Next(5, 9);
            for (var k = 0; k < n; k++)
            {
                var w = Mathf.Lerp(.6f, .22f, k / (float)n) + ctx.Range(-.05f, .05f); var h = ctx.Range(.12f, .2f);
                mk.Blob(stone, at + Vector3.up * (y + h / 2f) + new Vector3(ctx.Range(-.05f, .05f), 0, ctx.Range(-.05f, .05f)), new Vector3(w, h, w * ctx.Range(.75f, 1f)), ctx.Rng, .2f);
                y += h * 1.4f;
            }
            var col = SeaGlass[ctx.Rng.Next(SeaGlass.Length)];
            mk.Box(WorldMaterials.Glow(col, 2.2f), at + Vector3.up * (y + .05f), new Vector3(.1f, .06f, .08f), Quaternion.Euler(0, ctx.Range(0, 360f), 20f));
            halos.Add(at + Vector3.up * (y + .1f), .9f, col, .9f);
            ArtistBits.PostCollider(ctx.Root, at, y, .45f);
            noGrass.Add(new Vector3(at.x, 1f, at.z));
        }

        /// A driftwood bench facing the view, with a lantern on a post beside it.
        void LoneBench(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 at, Vector3 look)
        {
            look = Vector3.ProjectOnPlane(look, Vector3.up).normalized;
            var rot = Quaternion.LookRotation(look); var side = Vector3.Cross(Vector3.up, look);
            mk.Box(drift, at + Vector3.up * .46f, new Vector3(1.8f, .08f, .45f), rot);
            mk.Box(drift, at + Vector3.up * .8f - look * .22f, new Vector3(1.8f, .35f, .06f), rot * Quaternion.Euler(-10f, 0, 0));
            foreach (var s in new[] { -1f, 1f }) mk.Box(driftDark, at + side * s * .75f + Vector3.up * .23f, new Vector3(.1f, .46f, .42f), rot);
            solid.Box(drift, at + Vector3.up * .5f, new Vector3(1.8f, 1f, .5f), rot);
            var lamp = ArtistBits.LanternPost(mk, ctx.Root, ctx.OnGround(at.x + side.x * 1.6f, at.z + side.z * 1.6f), -side, driftDark, iron, Ember, 2.2f);
            halos.Add(lamp.transform.position, 1.3f, Ember, .9f);
            noGrass.Add(new Vector3(at.x, 1.4f, at.z));
        }

        /// An old boat turned upside down and made into a shed: tarred hull for a roof, a plank end wall with a
        /// door and a lit window, a stovepipe.
        void BoatShed(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 at, Vector3 sea)
        {
            // the hull, upside down: a smooth arch from the flat stern to the pointed bow, keel along the top
            var along = Quaternion.Euler(0, ctx.Range(-40f, 40f), 0) * Vector3.Cross(Vector3.up, sea);
            var side = Vector3.Cross(Vector3.up, along); var rot = Quaternion.LookRotation(along);
            var tar = WorldMaterials.Solid(new Color(.2f, .19f, .2f)); var band = WorldMaterials.Solid(HutPaint[ctx.Rng.Next(HutPaint.Length)]);
            const float L = 8.5f; const int stations = 10, arc = 8;
            var baseY = at.y - .15f;
            var stern = at - along * (L / 2f);
            Vector3 Q(int i, int k)
            {
                var t = i / (float)stations;                               // 0 at the stern, 1 at the bow
                var w = 1.75f * (1f - Mathf.Pow(t, 2.6f) * .94f);
                var hgt = 2.3f * (1f - Mathf.Pow(t, 3f) * .45f);
                var a = Mathf.PI * k / arc;
                var p = stern + along * (t * L) + side * Mathf.Cos(a) * w;
                p.y = baseY + Mathf.Sin(a) * hgt;
                return p;
            }
            var inside = at + Vector3.up * .5f;
            for (var i = 0; i < stations; i++)
                for (var k = 0; k < arc; k++)
                {
                    var m = k == 0 || k == arc - 1 ? band : tar;
                    mk.Quad(m, Q(i, k), Q(i + 1, k), Q(i + 1, k + 1), Q(i, k + 1), new Vector3(inside.x, baseY, inside.z) + along * ((i + .5f) / stations - .5f) * L * .8f);
                }
            for (var i = 0; i < stations; i++)
            {
                var a = Q(i, arc / 2); var b = Q(i + 1, arc / 2);
                mk.Box(WorldMaterials.Solid(new Color(.32f, .28f, .26f)), (a + b) / 2f + Vector3.up * .08f, new Vector3(.2f, .2f, Vector3.Distance(a, b) + .05f), Quaternion.LookRotation(b - a));
            }
            // the flat stern: a plank wall with a door and a lit window
            for (var k = 0; k < arc; k++) mk.Tri(planks, new Vector3(stern.x, baseY, stern.z), Q(0, k), Q(0, k + 1), stern + along);
            var door = stern - along * .03f + side * .45f; door.y = baseY;
            mk.Box(board, door + Vector3.up * .85f, new Vector3(.75f, 1.7f, .04f), rot);
            var win = stern - along * .04f - side * .65f + Vector3.up * (baseY - stern.y + 1.25f);
            mk.Box(WorldMaterials.Glow(Ember, 2.2f), win, new Vector3(.5f, .45f, .03f), rot);
            halos.Add(win - along * 1.2f, 1.4f, Ember, .9f);
            // a stovepipe through the hull near the stern
            var pipe = Q(2, arc / 2 - 1) + Vector3.up * .05f;
            mk.Prism(iron, pipe, Vector3.up, .09f, 1.1f, 6);
            mk.Box(iron, pipe + Vector3.up * 1.15f, new Vector3(.3f, .06f, .3f), Quaternion.identity);
            solid.Box(tar, at + Vector3.up * 1f, new Vector3(3.4f, 2.2f, L * .85f), rot);
            noGrass.Add(new Vector3(at.x, 5.5f, at.z));
            life.landmarks.Add(at);
        }

        /// A stone cottage long since roofless: broken walls, the chimney still standing, a doorway, and a lamp
        /// someone keeps lit on the hearth.
        void Ruin(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 at, Vector3 sea)
        {
            var rot = Quaternion.LookRotation(sea); var side = Vector3.Cross(Vector3.up, sea);
            var wallMat = WorldMaterials.Solid(new Color(.55f, .54f, .52f));
            const float W = 7f, D = 5f, T = .45f;
            var baseY = at.y - .3f;
            void Wall(Vector3 c, float len, Vector3 dir, float h0, float h1)
            {
                var n = Mathf.CeilToInt(len / 1f);
                for (var k = 0; k < n; k++)
                {
                    var t = (k + .5f) / n;
                    var h = Mathf.Lerp(h0, h1, t) * ctx.Range(.8f, 1f);
                    var p = c + dir * (t - .5f) * len; p.y = baseY + h / 2f;
                    mk.Box(wallMat, p, new Vector3(T, h, len / n + .02f), Quaternion.LookRotation(dir));
                    solid.Box(wallMat, p, new Vector3(T, h, len / n + .02f), Quaternion.LookRotation(dir));
                }
            }
            Wall(at - sea * (D / 2f), W, side, 3.4f, 1.2f);                                          // back
            Wall(at + side * (W / 2f), D - T, sea, 2.6f, 3f);                                        // gable ends
            Wall(at - side * (W / 2f), D - T, sea, .8f, 2.2f);
            Wall(at + sea * (D / 2f) + side * (W / 4f + .6f), W / 2f - 1.2f, side, 2.2f, 1.1f);     // front, with a doorway
            Wall(at + sea * (D / 2f) - side * (W / 4f + .6f), W / 2f - 1.2f, side, 1.4f, 2f);
            var chim = at + side * (W / 2f - .6f) - sea * .2f;
            mk.Box(wallMat, new Vector3(chim.x, baseY + 2.6f, chim.z), new Vector3(1f, 5.2f, 1.1f), rot);
            solid.Box(wallMat, new Vector3(chim.x, baseY + 2.6f, chim.z), new Vector3(1f, 5.2f, 1.1f), rot);
            var hearth = chim - side * .7f; hearth.y = ctx.Ground(hearth.x, hearth.z);
            mk.Box(WorldMaterials.Glow(Ember, 2.4f), hearth + Vector3.up * .3f, new Vector3(.25f, .35f, .25f), Quaternion.identity);
            halos.Add(hearth + Vector3.up * .35f, 1.6f, Ember, 1f);
            var light = ArtistBits.PointLight(ctx.Root, hearth + Vector3.up * .8f, Ember, 9f, 1.4f);
            light.gameObject.AddComponent<FireFlicker>().baseIntensity = 1.4f;
            for (var k = 0; k < 6; k++)
                mk.Blob(wallMat, ctx.OnGround(at.x + ctx.Range(-5f, 5f), at.z + ctx.Range(-5f, 5f)), new Vector3(ctx.Range(.25f, .5f), ctx.Range(.15f, .3f), ctx.Range(.25f, .4f)), ctx.Rng, .2f);
            noGrass.Add(new Vector3(at.x, 4.5f, at.z));
            life.landmarks.Add(at);
        }
    }
}
