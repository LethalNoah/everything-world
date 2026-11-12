using System.Collections.Generic;
using UnityEngine;

namespace EverythingWorld.World
{
    /// The open downs between the artists and the landmarks (WORLD_GUIDE.md): field hedges with five-bar gates,
    /// a shepherd's hut with its lamp on, a half-remembered festival campsite with a fire going, big oaks with a
    /// picnic table and a lantern in the branches, beehives along a field edge, a scarecrow in the stubble with
    /// small square bales, and a kite lost in a lone tree. Scattered on a jittered grid over open ground, off the
    /// paths and clear of every area and landmark. Its own random numbers, so nothing else on the land changes.
    public partial class IndieDownsKit
    {
        void Fields(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var rng = new System.Random(5151);
            float R(float a, float b) => a + (float)rng.NextDouble() * (b - a);
            const float cell = 52f, jitter = 13f;
            int[] mix = { 0, 3, 5, 1, 0, 4, 6, 3, 2, 5 };
            var counts = new int[7];
            var k = 0;
            for (var x = ctx.MinX + cell / 2f; x < ctx.MaxX; x += cell)
            for (var z = ctx.MinZ + cell / 2f; z < ctx.MaxZ; z += cell)
            {
                var p = new Vector3(x + R(-jitter, jitter), 0, z + R(-jitter, jitter));
                var face = Quaternion.Euler(0, R(0, 360f), 0) * Vector3.forward;
                if (!Open(ctx, p.x, p.z, 9f) || !ctx.IsLand(p.x, p.z, 10f)) continue;
                p = ctx.OnGround(p.x, p.z);
                var steep = Mathf.Abs(ctx.Ground(p.x + 4f, p.z) - ctx.Ground(p.x - 4f, p.z)) + Mathf.Abs(ctx.Ground(p.x, p.z + 4f) - ctx.Ground(p.x, p.z - 4f));
                if (steep > 4f) continue;                                                   // too steep for anything
                var kind = mix[(k + rng.Next(2)) % mix.Length];
                k++;
                if ((kind == 1 || kind == 2) && steep > 1.1f) kind = 3;                     // huts and tents only on gentle ground; an oak instead
                counts[kind]++;
                switch (kind)
                {
                    case 0: FieldGate(ctx, mk, solid, p, face, rng); break;
                    case 1: ShepherdsHut(ctx, mk, solid, p, face, rng); break;
                    case 2: Campsite(ctx, mk, solid, p, face, rng); break;
                    case 3: OakAndPicnic(ctx, mk, solid, p, face, rng); break;
                    case 4: Beehives(ctx, mk, solid, p, face, rng); break;
                    case 5: Scarecrow(ctx, mk, solid, p, face, rng); break;
                    default: LostKite(ctx, mk, solid, p, face, rng); break;
                }
            }
            Debug.Log($"The Indie Downs: {counts[0]} field gates, {counts[1]} shepherd's huts, {counts[2]} campsites, {counts[3]} oaks with picnic tables, {counts[4]} beehive rows, {counts[5]} scarecrows, {counts[6]} lost kites");
        }

        /// A field hedge, gappy and uneven, with a five-bar gate hung between two posts.
        void FieldGate(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 p, Vector3 face, System.Random rng)
        {
            var along = Vector3.Cross(Vector3.up, face); var up = Vector3.up;
            var len = 16f + (float)rng.NextDouble() * 8f;
            var gate = ((float)rng.NextDouble() - .5f) * len * .4f;
            var shades = new[] { hedge, leafA, leafB };
            for (var d = -len / 2f; d < len / 2f; d += 1.6f)
            {
                if (Mathf.Abs(d - gate) < 2.2f) continue;
                var q = p + along * d + face * (Mathf.Sin(d * .15f) * .6f); q = ctx.OnGround(q.x, q.z);
                var w = .9f + (float)rng.NextDouble() * .6f; var hh = .9f + (float)rng.NextDouble() * .6f;
                mk.Blob(shades[rng.Next(shades.Length)], q + up * hh * .75f, new Vector3(w, hh, w), rng, .2f);
                solid.Blob(hedge, q + up * hh * .75f, new Vector3(w, hh, w) * 1.1f, ColRng, 0f);
                solid.Box(hedge, q + up * hh * .8f, new Vector3(w * 1.6f, hh * 1.6f, w * 1.6f), Quaternion.LookRotation(face));
            }
            // the posts and the gate, swung a little open
            var hinge = ctx.OnGround((p + along * (gate - 1.8f)).x, (p + along * (gate - 1.8f)).z);
            var latch = ctx.OnGround((p + along * (gate + 1.8f)).x, (p + along * (gate + 1.8f)).z);
            foreach (var q in new[] { hinge, latch }) { mk.Box(darkWood, q + up * .75f, new Vector3(.22f, 1.5f, .22f), Quaternion.LookRotation(face)); ArtistBits.PostCollider(ctx.Root, q, 1.5f, .18f); }
            var swing = Quaternion.LookRotation(Quaternion.Euler(0, -20f, 0) * along);
            var g0 = hinge + swing * new Vector3(0, 0, 1.7f);
            for (var i = 0; i < 5; i++) mk.Box(cream, g0 + up * (.3f + i * .22f), new Vector3(.06f, .08f, 3.3f), swing);
            mk.Box(cream, g0 + up * .75f, new Vector3(.07f, .08f, 3.5f), swing * Quaternion.Euler(-14f, 0, 0));      // the diagonal brace
            solid.Box(cream, g0 + up * .65f, new Vector3(.2f, 1.2f, 3.3f), swing);
        }

        /// A shepherd's hut on iron wheels, painted, with a curved tin roof, steps to the door and its lamp on.
        void ShepherdsHut(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 p, Vector3 face, System.Random rng)
        {
            var side = Vector3.Cross(Vector3.up, face); var up = Vector3.up;
            var rot = Quaternion.LookRotation(side);                 // the hut runs along 'side'; its door faces 'face'
            const float L = 5f, W = 2.4f, H = 2.1f, lift = .55f;
            var (lo, hi) = Footprint(ctx, p, rot, W + .4f, L);
            var b = new Vector3(p.x, hi + lift, p.z);                // the floor, level, just above the highest ground under it
            var paint = WorldMaterials.Solid(new[] { new Color(.36f, .48f, .42f), new Color(.38f, .45f, .58f), new Color(.6f, .36f, .32f) }[rng.Next(3)]);
            mk.Box(paint, b + up * H / 2f, new Vector3(W, H, L), rot);
            solid.Box(paint, new Vector3(p.x, (lo + b.y + H) / 2f, p.z), new Vector3(W + .2f, b.y + H - lo, L + .2f), rot);
            var tin = WorldMaterials.Solid(new Color(.55f, .56f, .58f), .4f);
            for (var i = 0; i < 7; i++)                               // the curved roof
            {
                var a = Mathf.Lerp(-70f, 70f, i / 6f) * Mathf.Deg2Rad;
                var at = b + up * (H - .2f) + face * Mathf.Sin(a) * 1.45f + up * Mathf.Cos(a) * .7f;
                var n = (face * Mathf.Sin(a) / 1.45f + up * Mathf.Cos(a) / .7f).normalized;
                mk.Box(tin, at, new Vector3(.62f, .06f, L + .4f), Quaternion.LookRotation(side, n));
            }
            foreach (var s in new[] { -1f, 1f })                     // the wheels
            foreach (var t in new[] { -1f, 1f })
            {
                var w = p + side * t * (L / 2f - .8f) + face * s * (W / 2f + .05f);
                w.y = ctx.Ground(w.x, w.z) + .43f;                    // each wheel stands on its own ground
                mk.Prism(iron, w, face * s, .45f, .08f, 12);
                mk.Prism(darkWood, w, face * s, .1f, .1f, 6);
            }
            var door = b + face * (W / 2f + .02f) - side * .9f;
            mk.Box(darkWood, door + up * .95f, new Vector3(.8f, 1.8f, .05f), Quaternion.LookRotation(face));
            var win = b + face * (W / 2f + .02f) + side * .9f + up * 1.25f;
            mk.Box(WorldMaterials.Glow(Lamp, .55f), win, new Vector3(.7f, .55f, .05f), Quaternion.LookRotation(face));
            var lampAt = door + up * 2f + side * .6f + face * .1f;
            mk.Box(WorldMaterials.Glow(Lamp, 1.8f), lampAt, new Vector3(.18f, .26f, .18f), Quaternion.identity);
            halos.Add(lampAt, 1.2f, Lamp, .8f);
            halos.Add(win + face * .2f, 1f, Lamp, .5f);
            ArtistBits.PointLight(ctx.Root, lampAt + face * .7f, Lamp, 11f, 1.2f);
            // wooden steps from the door down to the ground: solid blocks you can climb
            var foot = door + face * 1.4f; var drop = b.y - ctx.Ground(foot.x, foot.z);
            var steps = Mathf.Clamp(Mathf.CeilToInt(drop / .22f) - 1, 1, 5);
            for (var i = 0; i < steps; i++)
            {
                var st = door + face * (.3f + i * .32f);
                var top = b.y - (i + 1) * drop / (steps + 1); var g = ctx.Ground(st.x, st.z) - .1f;
                if (top <= g + .05f) break;
                var block = new Vector3(st.x, (top + g) / 2f, st.z);
                mk.Box(wood, block, new Vector3(.9f, top - g, .34f), Quaternion.LookRotation(face));
                solid.Box(wood, block, new Vector3(.9f, top - g, .34f), Quaternion.LookRotation(face));
            }
            noGrass.Add(new Vector3(p.x, 4f, p.z));
        }

        /// A festival campsite you half remember: ridge tents in faded colours round a small fire, log seats and
        /// a line of bunting between two poles.
        void Campsite(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 p, Vector3 face, System.Random rng)
        {
            var up = Vector3.up;
            var fire = p;
            var n = 3 + rng.Next(2);
            for (var i = 0; i < n; i++)
            {
                var a = i * Mathf.PI * 2f / n + (float)rng.NextDouble() * .4f;
                var dir = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                var c = fire + dir * 6.5f; c = ctx.OnGround(c.x, c.z);
                var rot = Quaternion.LookRotation(-dir);              // the door faces the fire
                const float L = 2.8f, W = 2.2f, H = 1.5f;
                var cloth = WorldMaterials.Solid(Color.Lerp(Pastels[rng.Next(Pastels.Length)], new[] { NoahBlue, SeanGreen, new Color(.8f, .4f, .3f), new Color(.85f, .7f, .3f) }[rng.Next(4)], .4f));
                Vector3 P(float x, float y, float z) { var q = c + rot * new Vector3(x, 0, z); return y < .5f ? ctx.OnGround(q.x, q.z, y) : q + up * y; }
                Vector3 rf = P(0, H, L / 2f), rb = P(0, H, -L / 2f);
                foreach (var s in new[] { -1f, 1f })
                    mk.Quad(cloth, P(s * W / 2f, .02f, L / 2f), rf, rb, P(s * W / 2f, .02f, -L / 2f), c + up * .3f);
                mk.Tri(cloth, P(-W / 2f, .02f, -L / 2f), P(W / 2f, .02f, -L / 2f), rb, c + up * .3f);
                mk.Tri(WorldMaterials.Solid(new Color(.2f, .18f, .17f)), P(-W / 2f + .1f, .03f, L / 2f), P(W / 2f - .1f, .03f, L / 2f), rf, c + up * .3f);   // the open door
                solid.Box(cloth, c + up * H / 2f, new Vector3(W, H, L), rot);
            }
            // the fire, its stones and the log seats
            for (var s = 0; s < 9; s++)
            {
                var a = s * Mathf.PI * 2f / 9f;
                mk.Blob(stone, fire + new Vector3(Mathf.Cos(a), .1f, Mathf.Sin(a)) * .9f, new Vector3(.25f, .18f, .25f), rng);
            }
            var flameA = WorldMaterials.Glow(new Color(1f, .55f, .2f), 3f);
            var flameB = WorldMaterials.Glow(new Color(1f, .8f, .35f), 3.5f);
            mk.Cone(flameA, fire + up * .15f, .4f, .9f, 5);
            mk.Cone(flameB, fire + up * .2f + new Vector3(.1f, 0, .08f), .22f, .65f, 5, 30f);
            halos.Add(fire + up * .7f, 2.6f, new Color(1f, .6f, .28f), .9f);
            var l = ArtistBits.PointLight(ctx.Root, fire + up * 1.2f, new Color(1f, .62f, .32f), 12f, 2f);
            l.gameObject.AddComponent<FireFlicker>();
            ArtistBits.PostCollider(ctx.Root, fire, 1f, 1.1f);
            for (var s = 0; s < 3; s++)
            {
                var a = (s + .5f) * Mathf.PI * 2f / 3f;
                var dir = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                var seat = fire + dir * 3f; seat.y = ctx.Ground(seat.x, seat.z) + .22f;
                var along = Vector3.Cross(Vector3.up, dir);
                mk.Prism(darkWood, seat - along * .9f, along, .25f, 1.8f, 7);
                solid.Box(darkWood, seat, new Vector3(1.8f, .5f, .5f), Quaternion.LookRotation(dir));
            }
            // bunting between two poles
            var a0 = fire + face * 9f + Vector3.Cross(Vector3.up, face) * 5f; a0 = ctx.OnGround(a0.x, a0.z);
            var a1 = fire + face * 9f - Vector3.Cross(Vector3.up, face) * 5f; a1 = ctx.OnGround(a1.x, a1.z);
            foreach (var q in new[] { a0, a1 }) { mk.Prism(darkWood, q, up, .06f, 3f, 5); ArtistBits.PostCollider(ctx.Root, q, 3f, .1f); }
            Bunting(mk, a0 + up * 2.9f, a1 + up * 2.9f, new[] { NoahBlue, Pastels[1], SeanGreen, Pastels[4], new Color(.85f, .3f, .3f) }, rng.Next(5), .7f);
            noGrass.Add(new Vector3(fire.x, 9.5f, fire.z));
        }

        /// A big oak with a picnic table under it and a lantern hung from a low branch.
        void OakAndPicnic(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 p, Vector3 face, System.Random rng)
        {
            var up = Vector3.up;
            var h = 4f + (float)rng.NextDouble() * 1.5f; var r = 4.5f + (float)rng.NextDouble() * 1.5f;
            mk.Prism(bark, p, up, .7f, h + 1.5f, 8, .6f);
            ArtistBits.PostCollider(ctx.Root, p, h, .8f);
            for (var b = 0; b < 3; b++)
            {
                var a = b * Mathf.PI * 2f / 3f + (float)rng.NextDouble();
                var dir = new Vector3(Mathf.Cos(a), .7f, Mathf.Sin(a)).normalized;
                mk.Prism(bark, p + up * (h * .8f), dir, .3f, 3f, 6, .5f);
            }
            soft.Blob(leafA, p + up * (h + r * .45f), new Vector3(r, r * .7f, r), rng, .14f);
            soft.Blob(leafB, p + up * (h + r * .35f) + new Vector3(r * .55f, 0, r * .25f), new Vector3(r * .7f, r * .55f, r * .7f), rng, .14f);
            soft.Blob(hedge, p + up * (h + r * .3f) - new Vector3(r * .4f, 0, r * .45f), new Vector3(r * .65f, r * .5f, r * .65f), rng, .14f);
            // the picnic table with its benches
            var t = p + face * 4.2f; t = ctx.OnGround(t.x, t.z);
            var rot = Quaternion.LookRotation(Vector3.Cross(Vector3.up, face));
            mk.Box(wood, t + up * .75f, new Vector3(.9f, .07f, 2f), rot);
            foreach (var s in new[] { -1f, 1f })
            {
                mk.Box(wood, t + rot * new Vector3(s * .75f, .45f, 0), new Vector3(.3f, .06f, 2f), rot);
                mk.Box(darkWood, t + rot * new Vector3(0, .37f, s * .8f), new Vector3(1.9f, .07f, .1f), rot * Quaternion.Euler(0, 0, 0));
                foreach (var e in new[] { -1f, 1f }) mk.Box(darkWood, t + rot * new Vector3(s * .3f, .37f, e * .8f), new Vector3(.08f, .74f, .08f), rot);
            }
            solid.Box(wood, t + up * .4f, new Vector3(2f, .8f, 2.1f), rot);
            // a lantern hung from a low branch, over the table
            var hang = p + face * 2.4f + up * 3.1f;
            mk.Box(darkWood, hang + up * .45f, new Vector3(.02f, .9f, .02f), Quaternion.identity);
            mk.Box(WorldMaterials.Glow(Lamp, 1.8f), hang, new Vector3(.22f, .3f, .22f), Quaternion.identity);
            mk.Box(iron, hang + up * .2f, new Vector3(.28f, .05f, .28f), Quaternion.identity);
            halos.Add(hang, 1.3f, Lamp, .8f);
            ArtistBits.PointLight(ctx.Root, hang - up * .3f, Lamp, 10f, 1.1f);
        }

        /// A row of white beehives on stands along a field edge, and a painted honey sign.
        void Beehives(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 p, Vector3 face, System.Random rng)
        {
            var along = Vector3.Cross(Vector3.up, face); var up = Vector3.up; var rot = Quaternion.LookRotation(face);
            var white = WorldMaterials.Solid(new Color(.93f, .92f, .88f));
            var n = 3 + rng.Next(3);
            for (var i = 0; i < n; i++)
            {
                var q = p + along * ((i - (n - 1) / 2f) * 2.2f); q = ctx.OnGround(q.x, q.z);
                mk.Box(darkWood, q + up * .2f, new Vector3(.7f, .4f, .7f), rot);
                var tiers = 2 + rng.Next(2);
                for (var t = 0; t < tiers; t++) mk.Box(t % 2 == 0 ? white : WorldMaterials.Solid(new Color(.9f, .86f, .72f)), q + up * (.55f + t * .32f), new Vector3(.62f, .3f, .62f), rot);
                mk.Box(slate, q + up * (.42f + tiers * .32f + .06f), new Vector3(.78f, .1f, .78f), rot * Quaternion.Euler(4f, 0, 0));
                mk.Box(board, q + up * .44f + face * .32f, new Vector3(.3f, .04f, .02f), rot);         // the entrance
                solid.Box(white, q + up * (.25f + tiers * .16f), new Vector3(.8f, .6f + tiers * .32f, .8f), rot);
            }
            var sign = p - face * 1.6f + along * ((n + 1) / 2f * 2.2f); sign = ctx.OnGround(sign.x, sign.z);
            mk.Box(darkWood, sign + up * .6f, new Vector3(.08f, 1.2f, .08f), rot);
            mk.Box(WorldMaterials.Solid(new Color(.9f, .72f, .22f)), sign + up * 1.15f, new Vector3(.8f, .4f, .05f), rot); BoxCol(ctx.Root, sign + up * 1.15f, new Vector3(.8f, .4f, .15f), rot);
            ArtistBits.PostCollider(ctx.Root, sign, 1.3f, .1f);
        }

        /// A scarecrow in the stubble with a few small square bales stacked by it.
        void Scarecrow(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 p, Vector3 face, System.Random rng)
        {
            var side = Vector3.Cross(Vector3.up, face); var up = Vector3.up; var rot = Quaternion.LookRotation(face);
            mk.Box(darkWood, p + up * 1.1f, new Vector3(.1f, 2.2f, .1f), rot);
            mk.Box(darkWood, p + up * 1.55f, new Vector3(1.8f, .08f, .08f), rot);
            var coat = WorldMaterials.Solid(new[] { new Color(.4f, .35f, .3f), new Color(.3f, .35f, .45f), new Color(.5f, .3f, .28f) }[rng.Next(3)]);
            mk.Box(coat, p + up * 1.3f, new Vector3(.6f, .75f, .3f), rot * Quaternion.Euler(0, 0, 3f));
            foreach (var s in new[] { -1f, 1f }) mk.Box(coat, p + up * 1.55f + side * s * .55f, new Vector3(.55f, .22f, .22f), rot);
            var straw = WorldMaterials.Solid(Straw);
            foreach (var s in new[] { -1f, 1f }) mk.Box(straw, p + up * 1.5f + side * s * .9f, new Vector3(.12f, .2f, .15f), rot * Quaternion.Euler(0, 0, s * 20f));
            mk.Blob(WorldMaterials.Solid(new Color(.86f, .78f, .6f)), p + up * 1.9f, new Vector3(.2f, .22f, .2f), rng, .05f);
            mk.Prism(board, p + up * 2.05f, up, .32f, .04f, 10);
            mk.Prism(board, p + up * 2.08f, up, .17f, .22f, 10, .9f);
            ArtistBits.PostCollider(ctx.Root, p, 2.1f, .35f);
            solid.Box(coat, p + up * 1.5f, new Vector3(2f, .4f, .4f), rot);
            // small square bales
            var hay = WorldMaterials.Solid(new Color(.86f, .74f, .42f));
            for (var i = 0; i < 4; i++)
            {
                var q = p + face * 3f + side * ((i % 3) - 1f) * 1.25f; q = ctx.OnGround(q.x, q.z);
                var y = i < 3 ? .3f : .9f;
                if (i == 3) q = ctx.OnGround((p + face * 3f + side * .6f).x, (p + face * 3f + side * .6f).z);
                mk.Box(hay, q + up * y, new Vector3(1.1f, .55f, .6f), rot * Quaternion.Euler(0, (float)rng.NextDouble() * 10f - 5f, 0));
            }
            solid.Box(hay, ctx.OnGround((p + face * 3f).x, (p + face * 3f).z) + up * .6f, new Vector3(3.8f, 1.2f, .8f), Quaternion.LookRotation(face));
        }

        /// A lone tree with somebody's kite caught in it, its tail hanging down.
        void LostKite(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 p, Vector3 face, System.Random rng)
        {
            var up = Vector3.up;
            var h = 3.5f + (float)rng.NextDouble(); var r = 3.2f + (float)rng.NextDouble() * .8f;
            mk.Prism(bark, p, up, .32f, h + 1f, 7, .7f);
            ArtistBits.PostCollider(ctx.Root, p, h, .38f);
            soft.Blob(leafB, p + up * (h + r * .55f), new Vector3(r, r * .85f, r), rng, .12f);
            soft.Blob(leafA, p + up * (h + r * .35f) + new Vector3(r * .5f, 0, r * .2f), new Vector3(r * .7f, r * .6f, r * .7f), rng, .12f);
            // the kite, a bright diamond stuck at the edge of the crown
            var kc = p + face * (r * .85f) + up * (h + r * .7f);
            var rot = Quaternion.LookRotation(face) * Quaternion.Euler(0, 0, 35f);
            var colA = new[] { new Color(.9f, .3f, .3f), NoahBlue, SeanGreen, new Color(.95f, .75f, .2f) }[rng.Next(4)];
            Vector3 K(float x, float y) => kc + rot * new Vector3(x, y, 0);
            mk.Tri(WorldMaterials.Solid(colA), K(0, .9f), K(.55f, 0), K(-.55f, 0), kc - face);
            mk.Tri(WorldMaterials.Solid(Cream), K(0, -1.1f), K(-.55f, 0), K(.55f, 0), kc - face);
            mk.Tri(WorldMaterials.Solid(colA), K(0, .9f), K(-.55f, 0), K(.55f, 0), kc + face);
            mk.Tri(WorldMaterials.Solid(Cream), K(0, -1.1f), K(.55f, 0), K(-.55f, 0), kc + face);
            var tail = K(0, -1.1f);
            for (var i = 0; i < 6; i++)
            {
                var a = tail - up * (i * .45f) + face * Mathf.Sin(i * 1.3f) * .15f; var b = tail - up * ((i + 1) * .45f) + face * Mathf.Sin((i + 1) * 1.3f) * .15f;
                mk.Box(darkWood, (a + b) / 2f, new Vector3(.01f, .46f, .01f), Quaternion.LookRotation(face, b - a));
                mk.Box(WorldMaterials.Solid(i % 2 == 0 ? colA : Cream), b, new Vector3(.18f, .08f, .02f), Quaternion.LookRotation(face) * Quaternion.Euler(0, 0, 45f));
            }
        }
    }
}
