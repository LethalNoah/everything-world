using System.Collections.Generic;
using UnityEngine;

namespace EverythingWorld.World
{
    /// The rest of the Small Hours at 3 am, between the artists' homes (WORLD_GUIDE.md): the neighbours' cottages
    /// asleep, dark but for the odd landing light; garden sheds and washing left out on the line in the drizzle;
    /// wet dry-stone walls with gates, and dark garden trees; a lone street lamp over a bench and a puddle; a
    /// drinks machine humming blue at a corner; telegraph poles with their wires. Scattered on a jittered grid over
    /// the open hollow, off the lanes and clear of every home and of Noah Gallagher's place. Its own random numbers,
    /// so nothing else in the land changes.
    public partial class SmallHoursKit
    {
        void Night(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var rng = new System.Random(3003);
            float R(float a, float b) => a + (float)rng.NextDouble() * (b - a);
            var soft = new MeshKit();                       // washing on the line and low leaves: brushed through (the land audit skips "Soft")
            var counts = new int[7];
            const float cell = 46f, jitter = 11f;
            int[] mix = { 2, 0, 1, 6, 3, 2, 6 };            // walls and garden trees most, then cottages, sheds and lamps
            var k = 0;
            for (var x = ctx.MinX + cell / 2f; x < ctx.MaxX; x += cell)
            for (var z = ctx.MinZ + cell / 2f; z < ctx.MaxZ; z += cell)
            {
                var p = new Vector3(x + R(-jitter, jitter), 0, z + R(-jitter, jitter));
                var face = Quaternion.Euler(0, R(0, 360f), 0) * Vector3.forward;
                if (!ctx.IsLand(p.x, p.z, 8f) || !ctx.IsFree(p.x, p.z, 9f) || NearPath(p.x, p.z, 6f) || InNoah(p, 14f)) continue;
                p = ctx.OnGround(p.x, p.z);
                var kind = k % 11 == 5 ? 5 : mix[(k + rng.Next(2)) % mix.Length];
                k++;
                counts[kind]++;
                switch (kind)
                {
                    case 0: SleepingCottage(ctx, mk, solid, p, face, rng); break;
                    case 1: ShedAndWashing(ctx, mk, solid, soft, p, face, rng); break;
                    case 2: StoneWall(ctx, mk, solid, p, face, rng); break;
                    case 3: LampBench(ctx, mk, solid, p, face); break;
                    case 5: DrinksMachineAndPoles(ctx, mk, solid, p, face, rng); break;
                    default: GardenTrees(ctx, mk, solid, soft, p, rng); break;
                }
            }
            soft.Build("Soft washing and leaves", ctx.Root, false);
            Debug.Log($"The Small Hours: {counts[0]} sleeping cottages, {counts[1]} sheds, {counts[2]} stone walls, {counts[6]} garden copses, {counts[3]} lamps and benches, {counts[5]} drinks machines");
        }

        /// A neighbour's cottage, asleep: dark windows, the door shut, a hedge in front; now and then a landing
        /// light left on upstairs.
        void SleepingCottage(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 p, Vector3 face, System.Random rng)
        {
            var rot = Quaternion.LookRotation(face); var side = Vector3.Cross(Vector3.up, face); var up = Vector3.up;
            float W = 5.5f + (float)rng.NextDouble() * 1.5f, D = 5f, H = 3.4f + (float)rng.NextDouble() * .8f;
            var (lo, hi) = Footprint(ctx, p, rot, W, D);
            var b = new Vector3(p.x, lo - .3f, p.z); var top = hi + H;
            var paint = WorldMaterials.Solid(Color.Lerp(Paint[rng.Next(Paint.Length)], new Color(.2f, .2f, .26f), .45f));
            mk.Box(paint, new Vector3(p.x, (b.y + top) / 2f, p.z), new Vector3(W, top - b.y, D), rot);
            solid.Box(paint, new Vector3(p.x, (b.y + top) / 2f, p.z), new Vector3(W, top - b.y, D), rot);
            mk.GableRoof(slate, new Vector3(p.x, top, p.z), new Vector3(D + .7f, 0, W + .7f), 2.2f, Quaternion.LookRotation(side));
            mk.Box(stone, new Vector3(p.x, top + 1.6f, p.z) + side * (W / 2f - 1f), new Vector3(.6f, 1.6f, .6f), rot);
            var front = new Vector3(p.x, hi, p.z) + face * (D / 2f + .02f);
            mk.Box(darkWood, front + up * 1.05f, new Vector3(1f, 2.1f, .06f), rot);                                  // the door, shut
            var lit = rng.NextDouble() < .35;
            foreach (var s in new[] { -1f, 1f })
            {
                mk.Box(glassDark, front + side * s * (W / 4f + .4f) + up * 1.5f, new Vector3(1f, 1f, .05f), rot);
                var upstairs = front + side * s * (W / 4f + .4f) + up * (H - .55f);
                var on = lit && s > 0;
                mk.Box(on ? WorldMaterials.Glow(WindowGlow, 1.1f) : glassDark, upstairs, new Vector3(.8f, .6f, .05f), rot);
                if (on)
                {
                    halos.Add(upstairs + face * .3f, 1f, WindowGlow, .5f);
                    ArtistBits.PointLight(ctx.Root, upstairs + face * 1.2f, WindowGlow, 7f, .6f);
                }
            }
            // a low hedge along the front garden, with a gap at the path
            foreach (var s in new[] { -1f, 1f })
            {
                var h = new Vector3(p.x, 0, p.z) + face * (D / 2f + 3.2f) + side * s * (W / 4f + .9f); h = ctx.OnGround(h.x, h.z);
                mk.Blob(leaf, h + up * .55f, new Vector3(W / 4f + .3f, .7f, .6f), rng, .2f);
                solid.Blob(leaf, h + up * .55f, new Vector3(W / 4f + .3f, .7f, .6f) * 1.1f, Smooth, 0f);
                solid.Box(leaf, h + up * .55f, new Vector3(W / 2f + .6f, 1.3f, 1.2f), rot);
            }
        }

        /// A garden shed with a water butt, and washing left out on the line.
        void ShedAndWashing(LandContext ctx, MeshKit mk, MeshKit solid, MeshKit soft, Vector3 p, Vector3 face, System.Random rng)
        {
            var rot = Quaternion.LookRotation(face); var side = Vector3.Cross(Vector3.up, face); var up = Vector3.up;
            var (lo, hi) = Footprint(ctx, p, rot, 2.6f, 2.2f);
            var c = new Vector3(p.x, (lo + hi) / 2f, p.z);
            mk.Box(wood, c + up * 1.1f, new Vector3(2.6f, 2.2f + (hi - lo), 2.2f), rot);
            solid.Box(wood, c + up * 1.1f, new Vector3(2.6f, 2.2f + (hi - lo), 2.2f), rot);
            mk.Box(slate, c + up * (2.35f + (hi - lo) / 2f), new Vector3(3f, .12f, 2.6f), rot * Quaternion.Euler(8f, 0, 0));
            mk.Box(darkWood, c + face * 1.12f + up * .95f, new Vector3(.8f, 1.8f, .04f), rot);
            var butt = ctx.OnGround((c + side * 1.8f).x, (c + side * 1.8f).z);
            mk.Prism(iron, butt, up, .45f, 1.1f, 10);
            ArtistBits.PostCollider(ctx.Root, butt, 1.1f, .5f);
            // the washing line: two posts, a sagging line, things pegged out and left in the rain
            var a = ctx.OnGround((c + face * 4f - side * 3.5f).x, (c + face * 4f - side * 3.5f).z);
            var b = ctx.OnGround((c + face * 4f + side * 3.5f).x, (c + face * 4f + side * 3.5f).z);
            foreach (var q in new[] { a, b }) { mk.Prism(darkWood, q, up, .06f, 2.2f, 5); ArtistBits.PostCollider(ctx.Root, q, 2.2f, .12f); }
            Color[] cloth = { new Color(.55f, .58f, .68f), new Color(.68f, .5f, .5f), new Color(.82f, .8f, .76f), new Color(.45f, .52f, .48f) };
            var n = 3 + rng.Next(3);
            for (var i = 0; i < 6; i++)
            {
                float t0 = i / 6f, t1 = (i + 1) / 6f;
                Vector3 L(float t) => Vector3.Lerp(a, b, t) + up * (2.1f - .35f * 4f * t * (1f - t));
                mk.Box(iron, (L(t0) + L(t1)) / 2f, new Vector3(.015f, .015f, Vector3.Distance(L(t0), L(t1))), Quaternion.LookRotation(L(t1) - L(t0)));
            }
            for (var i = 0; i < n; i++)
            {
                var t = (i + 1f) / (n + 1f);
                var at = Vector3.Lerp(a, b, t) + up * (2.1f - .35f * 4f * t * (1f - t));
                var h = .6f + (float)rng.NextDouble() * .5f;
                soft.Box(WorldMaterials.Solid(cloth[rng.Next(cloth.Length)]), at - up * h / 2f, new Vector3(.03f, h, .7f + (float)rng.NextDouble() * .4f), Quaternion.LookRotation(side));
            }
        }

        /// A length of wet dry-stone wall with a wooden gate in it, and a dark garden tree behind.
        void StoneWall(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 p, Vector3 face, System.Random rng)
        {
            var along = Vector3.Cross(Vector3.up, face); var up = Vector3.up;
            var len = 12f + (float)rng.NextDouble() * 6f;
            var gate = (float)rng.NextDouble() * len * .5f - len * .25f;
            var wet = WorldMaterials.Solid(WetStone, .55f);
            for (var d = -len / 2f; d < len / 2f; d += 1.1f)
            {
                if (Mathf.Abs(d - gate) < 1.1f) continue;
                var q = p + along * d; q = ctx.OnGround(q.x, q.z);
                var h = 1.05f + (float)rng.NextDouble() * .2f;
                mk.Box(wet, q + up * (h / 2f - .1f), new Vector3(1.15f, h, .7f), Quaternion.LookRotation(face) * Quaternion.Euler(0, (float)rng.NextDouble() * 6f - 3f, 0));
                mk.Box(stone, q + up * (h - .05f), new Vector3(1.1f, .14f, .8f), Quaternion.LookRotation(face));       // the coping stones
                solid.Box(wet, q + up * (h / 2f), new Vector3(1.2f, h + .2f, .8f), Quaternion.LookRotation(face));
            }
            // the gate, a little open
            var hinge = ctx.OnGround((p + along * (gate - 1f)).x, (p + along * (gate - 1f)).z);
            var swing = Quaternion.LookRotation(Quaternion.Euler(0, 25f, 0) * along);
            for (var i = 0; i < 4; i++) mk.Box(darkWood, hinge + swing * new Vector3(0, .25f + i * .25f, 1f), new Vector3(.06f, .08f, 2f), swing);
            solid.Box(darkWood, hinge + swing * new Vector3(0, .6f, 1f), new Vector3(.15f, 1.2f, 2f), swing);
            // a dark tree in the garden behind
            var tree = p - face * 4f + along * ((float)rng.NextDouble() * 6f - 3f); tree = ctx.OnGround(tree.x, tree.z);
            var th = 4f + (float)rng.NextDouble() * 2f;
            mk.Prism(bark, tree, up, .25f, th, 6, .7f);
            mk.Blob(leaf, tree + up * (th + 1.4f), new Vector3(2.8f, 2.4f, 2.8f), rng, .15f);
            ArtistBits.PostCollider(ctx.Root, tree, th, .3f);
        }

        /// A few dark garden trees round a hedge, dripping.
        void GardenTrees(LandContext ctx, MeshKit mk, MeshKit solid, MeshKit soft, Vector3 p, System.Random rng)
        {
            var up = Vector3.up;
            var n = 2 + rng.Next(3);
            for (var i = 0; i < n; i++)
            {
                var a = (float)rng.NextDouble() * Mathf.PI * 2f; var r = 2f + (float)rng.NextDouble() * 5f;
                var t = p + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * r; t = ctx.OnGround(t.x, t.z);
                var h = 3.5f + (float)rng.NextDouble() * 3f; var cr = 2f + (float)rng.NextDouble() * 1.2f;
                mk.Prism(bark, t, up, .22f, h + .5f, 6, .7f);
                soft.Blob(i % 2 == 0 ? leaf : WorldMaterials.Solid(new Color(.12f, .19f, .2f)), t + up * (h + cr * .6f), new Vector3(cr, cr * .85f, cr), rng, .16f);
                ArtistBits.PostCollider(ctx.Root, t, h, .28f);
            }
            var hr = 1.2f + (float)rng.NextDouble();
            mk.Blob(leaf, p + up * hr * .6f, new Vector3(hr * 1.6f, hr * .8f, hr), rng, .2f);
            solid.Blob(leaf, p + up * hr * .6f, new Vector3(hr * 1.6f, hr * .8f, hr) * 1.1f, Smooth, 0f);
            solid.Box(leaf, p + up * hr * .6f, new Vector3(hr * 3f, hr * 1.6f, hr * 1.9f), Quaternion.identity);
        }

        /// A lone sodium lamp over a bench and a puddle: somewhere to sit at 3 am.
        void LampBench(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 p, Vector3 face)
        {
            var rot = Quaternion.LookRotation(face); var side = Vector3.Cross(Vector3.up, face); var up = Vector3.up;
            mk.Box(wood, p + up * .45f, new Vector3(1.8f, .08f, .5f), rot);
            mk.Box(wood, p + up * .8f - face * .22f, new Vector3(1.8f, .35f, .06f), rot);
            foreach (var s in new[] { -1f, 1f }) mk.Box(iron, p + side * s * .75f + up * .22f, new Vector3(.06f, .45f, .45f), rot);
            solid.Box(wood, p + up * .5f, new Vector3(1.8f, 1f, .6f), rot); if (plife != null) Interact.Bench(plife.transform, p + up * .5f - side * .5f, p + up * .5f + side * .5f, face);
            var post = p + side * 1.6f - face * .2f; post = ctx.OnGround(post.x, post.z);
            StreetLamp(ctx, mk, post, face - side * .3f);
            // a puddle catching the lamp
            var puddle = new Material(WorldMaterials.Solid(new Color(.1f, .11f, .16f), .97f)) { name = "Puddle" };
            var pc = p + face * 2.2f; pc = ctx.OnGround(pc.x, pc.z);
            mk.Prism(puddle, pc + up * .03f, up, 1.4f, .02f, 14);
        }

        /// A drinks machine humming blue at a corner, and a pair of telegraph poles with their wires.
        void DrinksMachineAndPoles(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 p, Vector3 face, System.Random rng)
        {
            var rot = Quaternion.LookRotation(face); var side = Vector3.Cross(Vector3.up, face); var up = Vector3.up;
            var blue = new Color(.55f, .78f, 1f);
            mk.Box(WorldMaterials.Solid(new Color(.75f, .76f, .8f)), p + up * 1f, new Vector3(1.1f, 2f, .8f), rot);
            solid.Box(iron, p + up * 1f, new Vector3(1.1f, 2f, .8f), rot);
            mk.Box(WorldMaterials.Glow(blue, 1.4f), p + face * .41f + up * 1.2f + side * .12f, new Vector3(.7f, 1.2f, .02f), rot);
            for (var i = 0; i < 4; i++) mk.Box(WorldMaterials.Glow(new Color(1f, .45f, .4f), 1.6f), p + face * .41f + up * (.9f + i * .25f) - side * .4f, new Vector3(.08f, .08f, .03f), rot);
            halos.Add(p + face * .8f + up * 1.2f, 2.2f, blue, .6f);
            ArtistBits.PointLight(ctx.Root, p + face * 1.2f + up * 1.6f, blue, 8f, .9f);
            // telegraph poles running past, wires sagging between them and on out of sight
            var dir = Quaternion.Euler(0, (float)rng.NextDouble() * 60f - 30f, 0) * side;
            var poles = new List<Vector3>();
            foreach (var d in new[] { -14f, 14f })
            {
                var q = p - face * 3f + dir * d; q = ctx.OnGround(q.x, q.z);
                if (!ctx.IsLand(q.x, q.z, 2f) || NearPath(q.x, q.z, 2f)) continue;
                mk.Prism(darkWood, q, up, .14f, 8f, 6, .8f);
                mk.Box(darkWood, q + up * 7.4f, new Vector3(1.6f, .1f, .1f), Quaternion.LookRotation(Vector3.Cross(Vector3.up, dir)));
                ArtistBits.PostCollider(ctx.Root, q, 8f, .2f);
                poles.Add(q);
            }
            if (poles.Count < 2) return;
            var across = Vector3.Cross(Vector3.up, dir).normalized;
            foreach (var s in new[] { -.6f, .6f })
            {
                Vector3 W(float t) => Vector3.Lerp(poles[0], poles[1], t) + across * s + up * (7.45f - 1f * 4f * t * (1f - t));
                for (var i = 0; i < 10; i++)
                    mk.Box(iron, (W(i / 10f) + W((i + 1) / 10f)) / 2f, new Vector3(.02f, .02f, Vector3.Distance(W(i / 10f), W((i + 1) / 10f))), Quaternion.LookRotation(W((i + 1) / 10f) - W(i / 10f)));
            }
        }
    }
}
