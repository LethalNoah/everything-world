using System.Collections.Generic;
using UnityEngine;

namespace EverythingWorld.World
{
    /// The rest of the Shoegaze Fens. In the water: lily pads (some with glowing flowers), broken pillars from
    /// the cathedral standing in the shallows with candles on them, amps and pedals half sunk with their lights
    /// still on, weeping willows along the banks. On the hills round the fen: willows, pink flowering shrubs,
    /// pairs of standing stones, benches with a lamp facing the water, a fallen speaker overgrown with flowers,
    /// and at the end of each trail something to find: a ruined bell tower, a stone circle, a moon gate, a great
    /// willow hung with lanterns.
    public partial class FensKit
    {
        void Wilds(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var maxR = 0f; foreach (var e in edge) maxR = Mathf.Max(maxR, e);
            var pad = WorldMaterials.Solid(new Color(.35f, .5f, .38f)); var padB = WorldMaterials.Solid(new Color(.42f, .55f, .4f));
            int lilies = 0, pillars = 0, sunk = 0, willows = 0;

            // ---------------------------------------------------------------- in the water
            for (var tries = 0; tries < 9000; tries++)
            {
                var a = ctx.Range(0, Mathf.PI * 2f); var r = Mathf.Sqrt(ctx.Range(0f, 1f)) * maxR;
                float x = heart.x + Mathf.Cos(a) * r, z = heart.y + Mathf.Sin(a) * r;
                if (!InMarsh(x, z, 1f) || OnIsland(x, z, -2f) || NearPath(x, z, 2.2f) || !Wet(ctx, x, z)) continue;
                var roll = ctx.Range(0f, 1f);
                var w = new Vector3(x, waterY, z);
                if (roll < .8f && lilies < 700)
                {
                    // a cluster of lily pads
                    var n = ctx.Rng.Next(2, 6);
                    for (var k = 0; k < n; k++)
                    {
                        var p = w + new Vector3(ctx.Range(-1.6f, 1.6f), 0, ctx.Range(-1.6f, 1.6f));
                        if (OnIsland(p.x, p.z, -1f)) continue;
                        var rr = ctx.Range(.35f, .8f);
                        mk.Prism(k % 2 == 0 ? pad : padB, p - Vector3.up * (WaterDrop - .005f), Vector3.up, rr, .02f, 9, 1f, ctx.Range(0, 40f));
                        lilies++;
                        if (ctx.Chance(.22f))
                        {
                            var col = ctx.Chance(.5f) ? HotPink : ctx.Chance(.5f) ? new Color(1f, .95f, .95f) : Lilac;
                            var fp = p + Vector3.up * .05f;
                            for (var j = 0; j < 5; j++) { var d = Quaternion.Euler(0, j * 72f, 0) * Vector3.forward; mk.Box(GlowM(col, 2f), fp + d * .1f + Vector3.up * .05f, new Vector3(.08f, .04f, .18f), Quaternion.LookRotation(d) * Quaternion.Euler(-30f, 0, 0)); }
                            if (ctx.Chance(.4f)) halos.Add(fp + Vector3.up * .4f, .8f, col, .7f);
                        }
                    }
                }
                else if (roll < .88f && pillars < 18 && r > CathR + 10f && !NearPath(x, z, 6f))
                {
                    // a broken pillar standing in the shallows, a candle burning on some
                    var h = ctx.Range(2f, 6f); var floor = waterY - FloorDrop;
                    var g = new Vector3(x, floor, z);
                    mk.Prism(stoneM, g, (Vector3.up + new Vector3(ctx.Range(-.08f, .08f), 0, ctx.Range(-.08f, .08f))).normalized, .55f, h + FloorDrop, 9, .95f);
                    mk.Box(stoneM, g + Vector3.up * (h + FloorDrop), new Vector3(1.3f, .3f, 1.3f), Quaternion.Euler(ctx.Range(-6f, 6f), ctx.Range(0, 90f), ctx.Range(-6f, 6f)));
                    ArtistBits.PostCollider(ctx.Root, g, h + FloorDrop, .6f);
                    if (ctx.Chance(.6f))
                    {
                        var top = g + Vector3.up * (h + FloorDrop + .15f);
                        mk.Prism(WorldMaterials.Solid(new Color(.95f, .92f, .88f)), top, Vector3.up, .06f, .3f, 6);
                        mk.Blob(GlowM(new Color(1f, .7f, .45f), 3f), top + Vector3.up * .38f, new Vector3(.04f, .08f, .04f), ctx.Rng, 0f);
                        halos.Add(top + Vector3.up * .4f, .9f, new Color(1f, .7f, .45f), .9f);
                    }
                    pillars++;
                }
                else if (roll < .93f && sunk < 10 && !NearPath(x, z, 5f))
                {
                    // an amp or a pedal half sunk, its light still on
                    var rot = Quaternion.Euler(ctx.Range(-25f, 25f), ctx.Range(0, 360f), ctx.Range(-20f, 20f));
                    var big = ctx.Chance(.5f);
                    var size = big ? new Vector3(1.6f, 1.3f, .8f) : new Vector3(1.4f, .5f, 1.1f);
                    var c = w + Vector3.up * (big ? .1f : -.1f);
                    mk.Box(big ? black : WorldMaterials.Solid(Glows[sunk % Glows.Length] * .7f), c, size, rot);
                    var col = Glows[(sunk + 2) % Glows.Length];
                    var led = c + rot * new Vector3(size.x * .35f, size.y * .5f + .01f, size.z * .3f);
                    mk.Box(GlowM(col, 3.4f), led, Vector3.one * .08f, rot);
                    halos.Add(led + Vector3.up * .3f, .9f, col, .9f);
                    solid.Box(black, c, size, rot);
                    sunk++;
                }
            }
            // weeping willows round the banks, standing in the shallows
            for (var k = 0; k < 64; k += 3)
            {
                var a = k * Mathf.PI * 2f / 64f + ctx.Range(-.05f, .05f);
                var r = edge[k] - ctx.Range(2f, 6f);
                float x = heart.x + Mathf.Cos(a) * r, z = heart.y + Mathf.Sin(a) * r;
                if (OnIsland(x, z, -4f) || NearPath(x, z, 5f) || InNoGrass(x, z, 2f)) continue;
                Willow(ctx, mk, soft, new Vector3(x, ctx.Ground(x, z), z), ctx.Range(.9f, 1.3f));
                willows++;
            }

            // ---------------------------------------------------------------- the hills
            var shrub = WorldMaterials.Solid(new Color(.4f, .5f, .42f)); var blossom = WorldMaterials.Solid(DustyPink); var blossomB = WorldMaterials.Solid(new Color(.95f, .78f, .85f));
            int things = 0;
            const float cell = 34f;
            for (var x = ctx.MinX; x < ctx.MaxX; x += cell)
            for (var z = ctx.MinZ; z < ctx.MaxZ; z += cell)
            {
                float px = x + ctx.Range(0f, cell), pz = z + ctx.Range(0f, cell);
                if (InMarsh(px, pz, -6f) || !Open(ctx, px, pz, 5f)) continue;
                var p = ctx.OnGround(px, pz);
                var toFen = new Vector3(heart.x - px, 0, heart.y - pz).normalized;
                var roll = ctx.Range(0f, 1f);
                if (roll < .3f) { Willow(ctx, mk, soft, p, ctx.Range(.8f, 1.2f)); willows++; }
                else if (roll < .52f)
                {
                    // flowering shrubs
                    var n = ctx.Rng.Next(2, 5);
                    for (var k = 0; k < n; k++)
                    {
                        var q = ctx.OnGround(px + ctx.Range(-3f, 3f), pz + ctx.Range(-3f, 3f));
                        var s = ctx.Range(.6f, 1.2f);
                        soft.Blob(shrub, q + Vector3.up * s * .5f, new Vector3(s, s * .7f, s), ctx.Rng, .3f);
                        for (var j = 0; j < 6; j++) soft.Blob(j % 2 == 0 ? blossom : blossomB, q + Vector3.up * s * .5f + Random3(ctx) * s * .9f, Vector3.one * .14f, ctx.Rng, .2f);
                    }
                }
                else if (roll < .6f)
                {
                    // two standing stones leaning together
                    foreach (var sd in new[] { -1f, 1f })
                    {
                        var q = p + Vector3.Cross(Vector3.up, toFen) * sd * 1.2f;
                        q = ctx.OnGround(q.x, q.z);
                        var h = ctx.Range(2.4f, 3.6f);
                        mk.Box(stoneDark, q + Vector3.up * (h / 2f - .3f), new Vector3(.9f, h, .5f), Quaternion.LookRotation(toFen) * Quaternion.Euler(0, 0, -sd * 8f));
                        solid.Box(stoneDark, q + Vector3.up * (h / 2f - .3f), new Vector3(.9f, h, .5f), Quaternion.LookRotation(toFen));
                    }
                    if (ctx.Chance(.5f)) { var col = Glows[things % Glows.Length]; mk.Blob(GlowM(col, 3f), p + Vector3.up * .4f, Vector3.one * .15f, ctx.Rng, 0f); halos.Add(p + Vector3.up * .5f, 1f, col, .9f); }
                }
                else if (roll < .68f)
                {
                    // a bench with a lamp, facing the fen
                    var rot = Quaternion.LookRotation(toFen); var sd = Vector3.Cross(Vector3.up, toFen);
                    mk.Box(wood, p + Vector3.up * .46f, new Vector3(1.8f, .08f, .45f), rot);
                    mk.Box(wood, p + Vector3.up * .8f - toFen * .22f, new Vector3(1.8f, .35f, .06f), rot * Quaternion.Euler(-10f, 0, 0));
                    foreach (var s in new[] { -1f, 1f }) mk.Box(iron, p + sd * s * .8f + Vector3.up * .23f, new Vector3(.08f, .46f, .42f), rot);
                    solid.Box(wood, p + Vector3.up * .5f, new Vector3(1.8f, 1f, .5f), rot);
                    Lamp(ctx, mk, ctx.Root, ctx.OnGround(p.x + sd.x * 1.6f, p.z + sd.z * 1.6f), Glows[things % Glows.Length], 2.5f);
                }
                else if (roll < .72f)
                {
                    // a fallen speaker cabinet, overgrown with flowers
                    var rot = Quaternion.Euler(ctx.Range(-10f, 10f), ctx.Range(0, 360f), 80f + ctx.Range(-8f, 8f));
                    mk.Box(black, p + Vector3.up * .5f, new Vector3(1.4f, 1.9f, 1f), rot);
                    mk.Prism(WorldMaterials.Solid(new Color(.2f, .2f, .22f)), p + Vector3.up * 1.01f, Vector3.up, .5f, .02f, 14);
                    for (var j = 0; j < 9; j++) mk.Blob(j % 3 == 0 ? GlowM(DustyPink, 1.2f) : blossom, p + Vector3.up * 1.05f + new Vector3(ctx.Range(-.7f, .7f), ctx.Range(0, .2f), ctx.Range(-.6f, .6f)), Vector3.one * .12f, ctx.Rng, .2f);
                    solid.Box(black, p + Vector3.up * .5f, new Vector3(1.9f, 1f, 1.4f), Quaternion.Euler(0, rot.eulerAngles.y, 0));
                }
                else if (roll < .78f) Lamp(ctx, mk, ctx.Root, p, Glows[things % Glows.Length], ctx.Range(2.4f, 3.2f));
                else continue;
                noGrass.Add(new Vector3(px, 2f, pz));
                things++;
            }

            // ---------------------------------------------------------------- at the end of each trail
            for (var i = 0; i < life.hillSpots.Count; i++)
            {
                var s = life.hillSpots[i];
                var toFen = new Vector3(heart.x - s.x, 0, heart.y - s.z).normalized;
                switch (i % 4)
                {
                    case 0: BellTower(ctx, mk, solid, s, toFen); break;
                    case 1: StoneCircle(ctx, mk, solid, s); break;
                    case 2: MoonGate(ctx, mk, solid, s, toFen); break;
                    default: LanternWillow(ctx, mk, s); break;
                }
                noGrass.Add(new Vector3(s.x, 8f, s.z));
            }
            Debug.Log($"The Shoegaze Fens: {lilies} lily pads, {pillars} pillars, {sunk} sunken amps, {willows} willows, {things} things on the hills");
        }

        static Vector3 Random3(LandContext ctx) => new Vector3(ctx.Range(-1f, 1f), ctx.Range(-.3f, 1f), ctx.Range(-1f, 1f)).normalized;

        /// A weeping willow: a dark leaning trunk splitting into limbs that arch up and out and droop at their ends,
        /// long tapering curtains of leaves hanging from all along them (pale green, some tinged pink, a few
        /// with blossom at the tips), and a few soft clumps of leaves over the crown. Leaves go in 'leaves' (you
        /// walk through them); the trunk has a collider.
        void Willow(LandContext ctx, MeshKit mk, MeshKit leaves, Vector3 at, float scale)
        {
            var bark = WorldMaterials.Solid(new Color(.3f, .26f, .28f));
            var greens = new[] { WorldMaterials.Solid(new Color(.55f, .66f, .5f)), WorldMaterials.Solid(new Color(.64f, .72f, .56f)), WorldMaterials.Solid(new Color(.72f, .64f, .68f)), WorldMaterials.Solid(new Color(.48f, .6f, .48f)) };
            var lean = (Vector3.up + new Vector3(ctx.Range(-.15f, .15f), 0, ctx.Range(-.15f, .15f))).normalized;
            var h = 4f * scale;
            mk.Prism(bark, at - Vector3.up * .5f, lean, .42f * scale, h + .5f, 8, .55f);
            var crown = at + lean * h;
            var limbs = ctx.Rng.Next(6, 9);
            for (var k = 0; k < limbs; k++)
            {
                var yaw = k * 360f / limbs + ctx.Range(-18f, 18f);
                var outDir = Quaternion.Euler(0, yaw, 0) * Vector3.forward;
                var reach = ctx.Range(2.6f, 3.8f) * scale; var lift = ctx.Range(1.2f, 2.2f) * scale;
                // the limb: up and out, then drooping (a short curve of pieces)
                Vector3 L(float t) => crown + outDir * reach * t + Vector3.up * (lift * Mathf.Sin(t * Mathf.PI * .8f) - t * t * .6f * scale);
                for (var i = 0; i < 4; i++)
                {
                    var a = L(i / 4f); var b = L((i + 1) / 4f);
                    mk.Prism(bark, a, (b - a).normalized, .13f * scale * (1f - i * .2f), Vector3.Distance(a, b) + .05f, 5, .8f);
                }
                // curtains hanging from along the limb
                for (var j = 0; j < 9; j++)
                {
                    var t = ctx.Range(.25f, 1f);
                    var top = L(t) + new Vector3(ctx.Range(-.3f, .3f), 0, ctx.Range(-.3f, .3f));
                    var ground = Mathf.Max(ctx.Ground(top.x, top.z), waterY);
                    var len = Mathf.Min(top.y - ground - ctx.Range(.2f, 1.2f), ctx.Range(2.2f, 4.8f) * scale);
                    if (len < .6f) continue;
                    var w = ctx.Range(.12f, .2f) * scale;
                    var across = Vector3.Cross(Vector3.up, outDir) * w;
                    var tip = top - Vector3.up * len + outDir * ctx.Range(-.1f, .25f);
                    var m = greens[(k + j) % greens.Length];
                    foreach (var inside in new[] { top + outDir, top - outDir })
                    {
                        leaves.Tri(m, top - across, top + across, tip, inside);
                        leaves.Tri(m, top, tip + across * .3f, tip - across * .3f, inside);
                    }
                    if (j % 4 == 0) leaves.Blob(WorldMaterials.Solid(new Color(.95f, .76f, .84f)), tip, Vector3.one * .07f, ctx.Rng, .2f);
                }
            }
            for (var k = 0; k < 4; k++)
                leaves.Blob(greens[k % greens.Length], crown + Vector3.up * ctx.Range(.8f, 1.6f) * scale + new Vector3(ctx.Range(-1.2f, 1.2f), 0, ctx.Range(-1.2f, 1.2f)) * scale, new Vector3(1.6f, .7f, 1.6f) * scale, ctx.Rng, .3f);
            ArtistBits.PostCollider(ctx.Root, at, h, .45f * scale);
            noGrass.Add(new Vector3(at.x, 1.2f, at.z));
        }

        void BellTower(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 at, Vector3 face)
        {
            var rot = Quaternion.LookRotation(face); var up = Vector3.up; const float H = 14f;
            foreach (var (sx, sz) in new[] { (-1f, -1f), (1f, -1f), (1f, 1f), (-1f, 1f) })
            {
                var p = at + rot * new Vector3(sx * 1.6f, 0, sz * 1.6f);
                var h = H - (sx > 0 && sz > 0 ? 3.5f : ctx.Range(0f, 1f));
                mk.Box(stoneM, p + up * (h / 2f - .5f), new Vector3(1f, h, 1f), rot);
                solid.Box(stoneM, p + up * (h / 2f - .5f), new Vector3(1f, h, 1f), rot);
            }
            mk.Box(stoneM, at + up * 6f, new Vector3(4.2f, .5f, 4.2f), rot);
            mk.Box(stoneM, at + up * (H - 3f), new Vector3(4.2f, .4f, 4.2f), rot);
            var bell = at + up * (H - 5f);
            var brass = WorldMaterials.Solid(new Color(.8f, .62f, .32f), .8f);
            mk.Prism(brass, bell - up * .8f, up, .9f, 1.3f, 14, .5f);
            mk.Box(iron, bell + up * .7f, new Vector3(.1f, .5f, .1f), Quaternion.identity);
            mk.Blob(GlowM(Lilac, 3f), bell - up * 1.1f, Vector3.one * .25f, ctx.Rng, 0f);
            halos.Add(bell - up * .5f, 2.4f, Lilac, 1f);
            ArtistBits.PointLight(ctx.Root, bell - up * 1.5f, Lilac, 16f, 1.6f);
            life.hillBell = bell;
        }

        void StoneCircle(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 at)
        {
            var up = Vector3.up;
            for (var k = 0; k < 9; k++)
            {
                var a = k * Mathf.PI * 2f / 9f; var d = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                var p = ctx.OnGround(at.x + d.x * 7f, at.z + d.z * 7f);
                var h = ctx.Range(2.5f, 4f);
                var rot = Quaternion.LookRotation(d) * Quaternion.Euler(ctx.Range(-5f, 5f), 0, ctx.Range(-5f, 5f));
                mk.Box(stoneDark, p + up * (h / 2f - .4f), new Vector3(1.2f, h, .7f), rot);
                solid.Box(stoneDark, p + up * (h / 2f - .4f), new Vector3(1.2f, h, .7f), rot);
                var col = Glows[k % Glows.Length];
                mk.Box(GlowM(col, 3f), p + up * (h * .55f) - d * .36f, new Vector3(.12f, h * .5f, .02f), rot);
                halos.Add(p + up * (h * .55f) - d * 1.5f, 1.2f, col, .8f);
            }
            mk.Box(stoneM, at + up * .4f, new Vector3(2.4f, .8f, 1.4f), Quaternion.identity);
            solid.Box(stoneM, at + up * .4f, new Vector3(2.4f, .8f, 1.4f), Quaternion.identity);
            mk.Blob(GlowM(HotPink, 3.4f), at + up * 1.3f, Vector3.one * .4f, ctx.Rng, .05f);
            halos.Add(at + up * 1.3f, 2.4f, HotPink, 1f);
            ArtistBits.PointLight(ctx.Root, at + up * 2f, HotPink, 16f, 1.6f);
        }

        void MoonGate(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 at, Vector3 face)
        {
            var up = Vector3.up; var side = Vector3.Cross(up, face); var c = at + up * 2.6f;
            for (var k = 0; k < 24; k++)
            {
                float a0 = k * Mathf.PI / 12f, a1 = (k + 1) * Mathf.PI / 12f;
                Vector3 P(float a, float r) => c + (side * Mathf.Cos(a) + up * Mathf.Sin(a)) * r;
                var p0 = P(a0, 2.9f); var p1 = P(a1, 2.9f);
                mk.Box(stoneM, (p0 + p1) / 2f, new Vector3(.7f, .7f, Vector3.Distance(p0, p1) + .1f), Quaternion.LookRotation(p1 - p0, face));
                var g0 = P(a0, 2.45f); var g1 = P(a1, 2.45f);
                mk.Box(GlowM(Lilac, 2.6f), (g0 + g1) / 2f, new Vector3(.12f, .12f, Vector3.Distance(g0, g1) + .02f), Quaternion.LookRotation(g1 - g0, face));
            }
            foreach (var s in new[] { -1f, 1f }) ArtistBits.PostCollider(ctx.Root, at + side * s * 2.6f, 4f, .5f);
            solid.Box(stoneM, c + up * 2.7f, new Vector3(5f, .8f, .8f), Quaternion.LookRotation(face));
            halos.Add(c + face * 2f, 2.6f, Lilac, .9f); halos.Add(c - face * 2f, 2.6f, Lilac, .9f);
            ArtistBits.PointLight(ctx.Root, c, Lilac, 14f, 1.4f);
        }

        void LanternWillow(LandContext ctx, MeshKit mk, Vector3 at)
        {
            Willow(ctx, mk, soft, at, 1.8f);
            for (var k = 0; k < 14; k++)
            {
                var a = ctx.Range(0, Mathf.PI * 2f); var r = ctx.Range(2f, 5f);
                var p = at + new Vector3(Mathf.Cos(a) * r, ctx.Range(3.5f, 6.5f), Mathf.Sin(a) * r);
                var col = Glows[k % Glows.Length];
                mk.Box(WorldMaterials.Solid(new Color(.6f, .55f, .5f)), p + Vector3.up * .5f, new Vector3(.02f, 1f, .02f), Quaternion.identity);
                mk.Box(GlowM(col, 2.8f), p, new Vector3(.25f, .35f, .25f), Quaternion.Euler(0, a * Mathf.Rad2Deg, 0));
                if (k % 2 == 0) halos.Add(p, 1.2f, col, .9f);
            }
            ArtistBits.PointLight(ctx.Root, at + Vector3.up * 4f, HotPink, 18f, 1.5f);
        }
    }
}
