using System.Collections.Generic;
using UnityEngine;

namespace EverythingWorld.World
{
    /// The Driftwood Shore's beach, where the promenade from the heart comes out: a row of lit beach huts either
    /// side of it, and along the sand nearby the wreck, the bones of a whale, a jetty running straight out into open
    /// water with a beacon at its end, smouldering bonfires and tide pools, and everything the storm washed up.
    public partial class DriftwoodKit
    {
        void Coast(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var near = hasShore ? shore : center; var reach = hasShore ? 260f : 3000f;
            var used = new List<Vector3> { center };
            if (hasShore) { BeachHuts(ctx, mk, solid, shore, seaDir); used.Add(shore); }
            if (JettySpot(ctx, near, reach + 80f, used, out var jettyAt, out var jettySea)) Jetty(ctx, mk, solid, jettyAt, jettySea);
            if (CoastSpot(ctx, near, reach, used, 70f, out var wreckAt, out var wreckSea)) Wreck(ctx, mk, solid, wreckAt, wreckSea);
            if (CoastSpot(ctx, near, reach, used, 60f, out var whaleAt, out var whaleSea)) Whale(ctx, mk, whaleAt, whaleSea);
            for (var i = 0; i < 3; i++) if (CoastSpot(ctx, near, reach + 60f, used, 55f, out var fireAt, out var fireSea)) Bonfire(ctx, mk, solid, fireAt - fireSea * 8f, fireSea);
            for (var i = 0; i < 10; i++) if (CoastSpot(ctx, near, reach + 140f, used, 28f, out var poolAt, out var poolSea)) TidePool(ctx, mk, poolAt, poolSea);
        }

        /// A spot on this land within a few metres of open water, near 'near', far from the places already used.
        bool CoastSpot(LandContext ctx, Vector3 near, float reach, List<Vector3> used, float apart, out Vector3 at, out Vector3 sea)
        {
            at = default; sea = Vector3.forward;
            for (var tries = 0; tries < 700; tries++)
            {
                var a = ctx.Range(0, Mathf.PI * 2f); var r = reach * Mathf.Sqrt(ctx.Range(0f, 1f));
                float x = near.x + Mathf.Cos(a) * r, z = near.z + Mathf.Sin(a) * r;
                if (!ctx.IsLand(x, z, 0f) || InFootprint(ctx, x, z, 4f) || NearPath(x, z, 5f) || InNoGrass(x, z, 4f)) continue;
                var p = ctx.OnGround(x, z);
                var d = SeaFrom(ctx.Data, p, out var dist, 20f);
                if (dist > 14f) continue;
                var far = true;
                foreach (var u in used) if (Vector2.Distance(new Vector2(u.x, u.z), new Vector2(x, z)) < apart) { far = false; break; }
                if (!far) continue;
                at = p; sea = d; used.Add(p);
                return true;
            }
            return false;
        }

        // ---------------------------------------------------------------- the jetty and its beacon
        const float JettyL = 46f;

        /// Where a jetty can start on the beach and run straight out into open water: low sand, the waterline a
        /// few metres ahead, and nothing but sea for the whole length (and either side of it).
        bool JettySpot(LandContext ctx, Vector3 near, float reach, List<Vector3> used, out Vector3 start, out Vector3 sea)
        {
            start = default; sea = Vector3.forward;
            bool Water(Vector3 q) => ctx.Data.IslandAt(q.x, q.z) == null || ctx.Ground(q.x, q.z) < -.3f;
            for (var tries = 0; tries < 1200; tries++)
            {
                var a = ctx.Range(0, Mathf.PI * 2f); var r = reach * Mathf.Sqrt(ctx.Range(0f, 1f));
                float x = near.x + Mathf.Cos(a) * r, z = near.z + Mathf.Sin(a) * r;
                if (!ctx.IsLand(x, z, 0f) || ctx.Ground(x, z) > 4f || InFootprint(ctx, x, z, 4f) || NearPath(x, z, 5f) || InNoGrass(x, z, 4f)) continue;
                var p = ctx.OnGround(x, z);
                var d = SeaFrom(ctx.Data, p, out var dist, 20f);
                if (dist > 16f) continue;
                var far = true;
                foreach (var u in used) if (Vector2.Distance(new Vector2(u.x, u.z), new Vector2(x, z)) < 70f) { far = false; break; }
                if (!far) continue;
                // the waterline straight ahead
                var k = 0f;
                while (k < 24f && ctx.Ground(p.x + d.x * k, p.z + d.z * k) > .8f) k += 1f;
                if (k >= 24f) continue;
                var wl = p + d * k;
                var side = Vector3.Cross(Vector3.up, d);
                var open = true;
                for (var t = 6f; t <= JettyL + 14f && open; t += 3f)
                    foreach (var s in new[] { 0f, -7f, 7f })
                        if (!Water(wl + d * t + side * s)) { open = false; break; }
                if (!open) continue;
                start = ctx.OnGround(wl.x - d.x * 7f, wl.z - d.z * 7f); sea = d; used.Add(wl);
                return true;
            }
            return false;
        }

        void Jetty(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 at, Vector3 sea)
        {
            const float W = 3f, DeckY = 1.5f;
            var rot = Quaternion.LookRotation(sea);
            var side = Vector3.Cross(Vector3.up, sea);
            var deckEnd = Vector3.zero;
            for (var d = 0f; d < JettyL; d += 2f)
            {
                var p = at + sea * d;
                var g = ctx.Ground(p.x, p.z);
                p.y = Mathf.Max(DeckY, g + .35f);
                mk.Box(planks, p, new Vector3(W, .14f, 2.05f), rot);
                mk.Box(plankGap, p + Vector3.up * .072f + sea * .99f, new Vector3(W - .05f, .005f, .03f), rot);
                solid.Box(planks, p - Vector3.up * .1f, new Vector3(W, .34f, 2.05f), rot);
                if (Mathf.Repeat(d, 6f) < .1f)
                    foreach (var s in new[] { -1f, 1f })
                    {
                        var post = p + side * s * (W / 2f);
                        var bottom = Mathf.Min(ctx.Ground(post.x, post.z), -1.5f);
                        mk.Prism(driftDark, new Vector3(post.x, bottom, post.z), Vector3.up, .16f, p.y - bottom + .9f, 6);
                        var r0 = post + Vector3.up * .95f; var r1 = r0 + sea * 6f;
                        if (d + 6f < JettyL)
                        {
                            mk.Box(rope, (r0 + r1) / 2f, new Vector3(.05f, .05f, 6f), rot);
                            solid.Box(rope, (r0 + r1) / 2f - Vector3.up * .45f, new Vector3(.12f, 1.1f, 6f), rot);
                        }
                    }
                deckEnd = p;
            }
            // the beacon at the end: a banded tower with a lantern room and a beam that sweeps through the mist
            var end = deckEnd + sea * 2.5f; end.y = DeckY - .1f;
            mk.Prism(stone, new Vector3(end.x, -2f, end.z), Vector3.up, 2.1f, end.y + 2f, 10);
            var white = WorldMaterials.Solid(new Color(.88f, .87f, .84f)); var red = WorldMaterials.Solid(new Color(.62f, .24f, .22f));
            for (var b = 0; b < 4; b++) mk.Prism(b % 2 == 0 ? white : red, end + Vector3.up * b * 2.2f, Vector3.up, 1.6f - b * .15f, 2.2f, 12, .93f);
            solid.Prism(white, end, Vector3.up, 1.6f, 9f, 8, .7f);
            var room = end + Vector3.up * 8.8f;
            mk.Prism(iron, room, Vector3.up, 1.3f, .15f, 12);
            mk.Prism(WorldMaterials.Glow(new Color(1f, .9f, .7f), 3f), room + Vector3.up * .15f, Vector3.up, .9f, 1.4f, 10);
            mk.Cone(red, room + Vector3.up * 1.55f, 1.3f, 1.2f, 12);
            halos.Add(room + Vector3.up * .8f, 4f, new Color(1f, .9f, .7f), 1f);
            ArtistBits.PointLight(ctx.Root, room + Vector3.up * .8f, new Color(1f, .88f, .7f), 30f, 1.6f);
            life.beaconAt = room + Vector3.up * .85f;
            // a lantern where it leaves the beach
            var lamp = ArtistBits.LanternPost(mk, ctx.Root, ctx.OnGround(at.x + side.x * 2.2f, at.z + side.z * 2.2f), -side, driftDark, iron, Ember, 2.6f);
            halos.Add(lamp.transform.position, 1.4f, Ember, .9f);
            noGrass.Add(new Vector3(at.x, 6f, at.z));
            for (var d = 0f; d < 10f; d += 2f) Remember(at + sea * d);
            life.landmarks.Add(at);
        }

        // ---------------------------------------------------------------- the wreck
        void Wreck(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 at, Vector3 sea)
        {
            var along = Quaternion.Euler(0, ctx.Range(-30f, 30f), 0) * Vector3.Cross(Vector3.up, sea);
            var side = Vector3.Cross(Vector3.up, along);
            var hull = WorldMaterials.Solid(new Color(.38f, .33f, .3f));
            var sink = Vector3.up * -1.4f;
            const float L = 28f;
            mk.Box(hull, at + sink + Vector3.up * .3f, new Vector3(.6f, .6f, L), Quaternion.LookRotation(along));
            for (var i = 0; i < 11; i++)
            {
                var t = i / 10f - .5f;
                var width = Mathf.Lerp(4.2f, 1.2f, Mathf.Abs(t) * 2f);
                var c = at + along * t * L + sink;
                var broken = ctx.Chance(.35f);
                foreach (var s in new[] { -1f, 1f })
                {
                    if (broken && s > 0) continue;
                    var prev = c;
                    for (var k = 1; k <= 6; k++)
                    {
                        var a = k / 6f * Mathf.PI * .5f;
                        var p = c + side * s * Mathf.Sin(a) * width + Vector3.up * (1f - Mathf.Cos(a)) * 5.5f;
                        var d = p - prev;
                        mk.Box(hull, (prev + p) / 2f, new Vector3(.3f, .35f, d.magnitude + .1f), Quaternion.LookRotation(d, along));
                        RibCollider(ctx, (prev + p) / 2f, .35f);
                        prev = p;
                    }
                    var foot = c + side * s * width * .9f; foot.y = ctx.Ground(foot.x, foot.z);
                    ArtistBits.PostCollider(ctx.Root, foot, 4f, .45f);
                }
            }
            for (var i = 0; i < 6; i++)
            {
                var p = at + along * ctx.Range(-.4f, .4f) * L + side * ctx.Range(-2.5f, 2.5f) + sink + Vector3.up * ctx.Range(1.6f, 3.6f);
                var plen = ctx.Range(2f, 5f); var prot = Quaternion.LookRotation(along) * Quaternion.Euler(ctx.Range(-15f, 15f), 0, ctx.Range(-30f, 30f));
                mk.Box(hull, p, new Vector3(.12f, .4f, plen), prot);
                if (p.y - ctx.Ground(p.x, p.z) < 2.4f) BoxCol(ctx.Root, p, new Vector3(.2f, .45f, plen), prot);   // a loose plank at walking height
            }
            var mastFoot = at + along * 2f + sink;
            var mastDir = (Vector3.up + sea * .6f + along * .2f).normalized;
            mk.Prism(hull, mastFoot, mastDir, .35f, 13f, 8, .6f);
            var yard = mastFoot + mastDir * 10f;
            mk.Box(hull, yard, new Vector3(.2f, .2f, 6f), Quaternion.LookRotation(side));
            var sail = WorldMaterials.Solid(new Color(.78f, .7f, .68f));
            mk.Tri(sail, yard - side * 2.8f, yard + side * 1.2f, yard - side * 1.5f - mastDir * 5f + sea * 1.5f, yard + sea * 3f);
            mk.Tri(sail, yard - side * 2.8f, yard + side * 1.2f, yard - side * 1.5f - mastDir * 5f + sea * 1.5f, yard - sea * 3f);
            // a lantern someone hung on the broken mast
            var lantern = mastFoot + mastDir * 6f + side * .4f;
            mk.Box(WorldMaterials.Glow(Ember, 2.4f), lantern, new Vector3(.28f, .38f, .28f), Quaternion.identity);
            halos.Add(lantern, 1.6f, Ember, 1f);
            noGrass.Add(new Vector3(at.x, 18f, at.z));
            life.landmarks.Add(at);
        }

        // ---------------------------------------------------------------- the whale
        /// The bones of a whale on the sand: a long curve of vertebrae, ribs arching up out of the beach, the great
        /// jaw lying open and the skull at the end, all bleached bone-white.
        /// A post from the ground up to a piece of rib or bone, if that piece is at walking height.
        static void RibCollider(LandContext ctx, Vector3 p, float radius)
        {
            var g = ctx.Ground(p.x, p.z);
            if (p.y - g < .2f || p.y - g > 2.3f) return;
            ArtistBits.PostCollider(ctx.Root, new Vector3(p.x, g, p.z), p.y - g + .2f, radius);
        }

        void Whale(LandContext ctx, MeshKit mk, Vector3 at, Vector3 sea)
        {
            var along = Quaternion.Euler(0, ctx.Range(-25f, 25f), 0) * Vector3.Cross(Vector3.up, sea);
            var side = Vector3.Cross(Vector3.up, along);
            var boneMat = WorldMaterials.Solid(new Color(.9f, .88f, .82f));
            const float L = 18f;
            Vector3 Spine(float t) { var p = at + along * (t - .5f) * L + side * Mathf.Sin(t * 3f) * .8f; p.y = ctx.Ground(p.x, p.z) + .35f + Mathf.Sin(t * Mathf.PI) * .4f; return p; }
            for (var i = 0; i < 26; i++)
            {
                var t = i / 25f;
                mk.Blob(boneMat, Spine(t), new Vector3(.32f, .28f, .3f) * Mathf.Lerp(1f, .45f, t), ctx.Rng, .15f);
                if (i % 2 == 0) RibCollider(ctx, Spine(t), .4f);
            }
            for (var i = 0; i < 9; i++)
            {
                var t = .12f + i * .06f;
                var c = Spine(t); var h = Mathf.Lerp(3.4f, 2f, i / 8f); var w = Mathf.Lerp(2.6f, 1.6f, i / 8f);
                foreach (var s in new[] { -1f, 1f })
                {
                    if (i % 4 == 3 && s > 0) continue;                         // a few lost
                    var prev = c;
                    for (var k = 1; k <= 6; k++)
                    {
                        var a = k / 6f * Mathf.PI * .62f;
                        var p = c + side * s * Mathf.Sin(a) * w + Vector3.up * Mathf.Sin(a * .9f + .3f) * h - Vector3.up * (k > 4 ? (k - 4) * .6f : 0f);
                        var d = p - prev;
                        mk.Prism(boneMat, prev, d.normalized, .13f, d.magnitude + .05f, 6, .9f);
                        RibCollider(ctx, (prev + p) / 2f, .22f);
                        prev = p;
                    }
                    var foot = c + side * s * w * .9f; foot.y = ctx.Ground(foot.x, foot.z);
                    ArtistBits.PostCollider(ctx.Root, foot, 2.4f, .25f);
                }
            }
            // the skull and the open jaw
            var head = Spine(0f) - along * 2.2f;
            mk.Blob(boneMat, head + Vector3.up * .2f, new Vector3(1.1f, .55f, .9f), ctx.Rng, .1f);
            foreach (var s in new[] { -1f, 1f })
            {
                var j0 = head + side * s * .7f; var j1 = head - along * 4.8f + side * s * (1.4f + ctx.Range(0f, .4f)); j1.y = ctx.Ground(j1.x, j1.z) + .15f;
                var mid = (j0 + j1) / 2f + side * s * .5f + Vector3.up * .2f;
                mk.Prism(boneMat, j0, (mid - j0).normalized, .2f, Vector3.Distance(j0, mid), 6, .9f);
                mk.Prism(boneMat, mid, (j1 - mid).normalized, .18f, Vector3.Distance(mid, j1), 6, .6f);
            }
            ArtistBits.PostCollider(ctx.Root, head, 1.2f, 1f);
            noGrass.Add(new Vector3(at.x, 12f, at.z));
            life.landmarks.Add(at);
        }

        // ---------------------------------------------------------------- beach huts
        readonly List<Vector3> hutSpots = new List<Vector3>();   // so the lifeguard tower never stands into a hut

        void BeachHuts(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 at, Vector3 sea)
        {
            hutSpots.Clear();
            var along = Vector3.Cross(Vector3.up, sea);
            var start = at - sea * 10f;
            var built = 0;
            for (var i = 0; i < 8; i++)
            {
                var p = start + along * (i - 3.5f) * 4.6f;
                if (!ctx.IsLand(p.x, p.z, 1f) || InFootprint(ctx, p.x, p.z, 2f) || NearPath(p.x, p.z, 3.2f)) continue;
                p = ctx.OnGround(p.x, p.z);
                var rot = Quaternion.LookRotation(sea);
                var paint = WorldMaterials.Solid(HutPaint[i % HutPaint.Length]);
                const float w = 3f, d = 3f, h = 3.2f;
                var baseY = p.y + .4f;
                mk.Box(driftDark, new Vector3(p.x, p.y - .3f, p.z), new Vector3(w + .2f, 1.4f, d + .2f), rot);
                mk.Box(paint, new Vector3(p.x, baseY + h / 2f, p.z), new Vector3(w, h, d), rot);
                solid.Box(paint, new Vector3(p.x, baseY + h / 2f, p.z), new Vector3(w, h, d), rot);
                mk.GableRoof(WorldMaterials.Solid(new Color(.86f, .85f, .82f)), new Vector3(p.x, baseY + h, p.z), new Vector3(d + .4f, 0, w + .4f), 1.3f, Quaternion.LookRotation(along));
                var face = new Vector3(p.x, baseY, p.z) + sea * (d / 2f + .02f);
                mk.Box(WorldMaterials.Solid(Color.Lerp(HutPaint[(i + 2) % HutPaint.Length], Color.white, .3f)), face + Vector3.up * 1.1f, new Vector3(1.2f, 2.2f, .06f), rot);
                for (var k = 0; k < 6; k++) mk.Box(paint, face + along * ((k - 2.5f) * .45f) + Vector3.up * 2.6f + sea * .02f, new Vector3(.04f, .5f, .03f), rot);
                if (i % 2 == 1)
                {
                    var win = face + along * 1f + Vector3.up * 1.7f;
                    ArtistBits.GlassBox(ctx.Root, win, new Vector3(.6f, .6f, .05f), Ember, rot);
                    halos.Add(win + sea * .3f, 1.3f, Ember, .8f);
                }
                hutSpots.Add(p);
                built++;
            }
            if (built > 0) { noGrass.Add(new Vector3(start.x, 20f, start.z)); life.landmarks.Add(start); }
        }

        // ---------------------------------------------------------------- bonfires, tide pools, and what the storm left
        void Bonfire(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 at, Vector3 sea)
        {
            at = ctx.OnGround(at.x, at.z);
            for (var k = 0; k < 10; k++) { var a = k * Mathf.PI / 5f; mk.Blob(stone, at + new Vector3(Mathf.Cos(a), .1f, Mathf.Sin(a)) * 1f, new Vector3(.3f, .2f, .26f), ctx.Rng, .2f); }
            for (var k = 0; k < 5; k++) { var a = k * Mathf.PI * 2f / 5f; var d = new Vector3(Mathf.Cos(a), .5f, Mathf.Sin(a)).normalized; mk.Prism(driftDark, at + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * .7f, -d + Vector3.up * 1.2f, .1f, 1.2f, 5); }
            var embers = WorldMaterials.Glow(Ember, 2.6f);
            mk.Blob(embers, at + Vector3.up * .25f, new Vector3(.5f, .2f, .5f), ctx.Rng, .3f);
            for (var k = 0; k < 3; k++) mk.Box(WorldMaterials.Glow(new Color(1f, .75f, .35f), 3f), at + Vector3.up * (.5f + k * .12f) + new Vector3(ctx.Range(-.2f, .2f), 0, ctx.Range(-.2f, .2f)), new Vector3(.12f, .5f - k * .1f, .12f), Quaternion.Euler(0, k * 40f, 0));
            halos.Add(at + Vector3.up * .6f, 2.4f, Ember, 1f);
            var light = ArtistBits.PointLight(ctx.Root, at + Vector3.up * 1f, Ember, 12f, 2f);
            var flick = light.gameObject.AddComponent<FireFlicker>(); flick.baseIntensity = 2f;
            ArtistBits.PostCollider(ctx.Root, at, .8f, 1.2f);
            for (var k = 0; k < 3; k++)
            {
                var a = k * Mathf.PI * 2f / 3f + .4f; var d = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                var p = ctx.OnGround(at.x + d.x * 3.2f, at.z + d.z * 3.2f);
                var along = Vector3.Cross(Vector3.up, d);
                mk.Prism(drift, p + Vector3.up * .3f - along * 1f, along, .3f, 2f, 7);
                solid.Box(drift, p + Vector3.up * .3f, new Vector3(2f, .6f, .6f), Quaternion.LookRotation(along));
                if (k == 0) mk.Prism(WorldMaterials.Solid(new Color(.5f, .25f, .25f), .5f), p + Vector3.up * .6f + along * .6f, Vector3.up, .07f, .3f, 8);
                if (k == 1) mk.Box(WorldMaterials.Solid(new Color(.55f, .45f, .5f)), p + Vector3.up * .62f, new Vector3(1.2f, .04f, .5f), Quaternion.LookRotation(along) * Quaternion.Euler(0, 0, 3));
            }
            noGrass.Add(new Vector3(at.x, 5f, at.z));
            life.landmarks.Add(at);
        }

        void TidePool(LandContext ctx, MeshKit mk, Vector3 at, Vector3 sea)
        {
            var r = ctx.Range(1.6f, 3f);
            mk.Prism(water, at + Vector3.up * .03f, Vector3.up, r, .03f, 16);
            for (var k = 0; k < 9; k++)
            {
                var a = k * Mathf.PI * 2f / 9f + ctx.Range(-.2f, .2f);
                var rp = at + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * (r + .3f);
                mk.Blob(stone, ctx.OnGround(rp.x, rp.z), new Vector3(ctx.Range(.4f, .8f), ctx.Range(.25f, .55f), ctx.Range(.4f, .7f)), ctx.Rng, .25f);
            }
            var star = WorldMaterials.Solid(new Color(.9f, .45f, .25f));
            var sp = at + new Vector3(ctx.Range(-r, r) * .5f, .05f, ctx.Range(-r, r) * .5f);
            for (var k = 0; k < 5; k++) mk.Box(star, sp + Quaternion.Euler(0, k * 72f, 0) * Vector3.forward * .12f, new Vector3(.07f, .03f, .24f), Quaternion.Euler(0, k * 72f, 0));
            noGrass.Add(new Vector3(at.x, r + 1f, at.z));
        }

        /// Everything the storm washed up, thickest along the tide line: bleached logs and branches, seaweed, sea
        /// glass glinting, shells, buoys and a few lost crab pots. Sea glass sparkles (DriftwoodLife).
        void Washed(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var glassMats = new Material[SeaGlass.Length];
            for (var i = 0; i < SeaGlass.Length; i++) glassMats[i] = new Material(WorldMaterials.Glow(SeaGlass[i], 1.8f)) { name = "Sea glass" };
            life.seaGlass = glassMats;
            var shell = WorldMaterials.Solid(new Color(.92f, .88f, .82f));
            var buoy = WorldMaterials.Solid(new Color(.8f, .3f, .25f)); var buoyW = WorldMaterials.Solid(new Color(.9f, .9f, .88f));
            var pot = WorldMaterials.Solid(new Color(.3f, .3f, .32f));
            int logs = 0, glass = 0;
            for (var tries = 0; tries < 7000; tries++)
            {
                float x = ctx.Range(ctx.MinX, ctx.MaxX), z = ctx.Range(ctx.MinZ, ctx.MaxZ);
                if (!ctx.IsLand(x, z, 0f) || InFootprint(ctx, x, z, 1f) || NearPath(x, z, 2f) || InNoGrass(x, z, 1f)) continue;
                var p = ctx.OnGround(x, z);
                if (p.y > 5f && ctx.Chance(.85f)) continue;                 // mostly on the beaches
                var roll = ctx.Range(0f, 1f);
                if (roll < .14f && logs < 190)
                {
                    var dir = Quaternion.Euler(ctx.Range(-6f, 6f), ctx.Range(0, 360f), 0) * Vector3.forward;
                    var len = ctx.Range(1.5f, 7f); var r = ctx.Range(.12f, .45f);
                    // laid along the sand: both ends on the ground, so it doesn't float off a slope
                    var e0 = p - dir * len / 2f; var e1 = p + dir * len / 2f; e0.y = ctx.Ground(e0.x, e0.z); e1.y = ctx.Ground(e1.x, e1.z);
                    var ld = e1 - e0; var llen = ld.magnitude; ld /= llen;
                    mk.Prism(ctx.Chance(.6f) ? drift : driftDark, e0 + Vector3.up * r * .6f, ld, r, llen, 6, ctx.Range(.5f, .9f));
                    if (len > 3f && ctx.Chance(.5f))
                    {
                        var bs = Vector3.Lerp(e0, e1, .3f) + Vector3.up * r; var bd = (ld + Vector3.up * .6f + Vector3.Cross(Vector3.up, ld) * ctx.Range(-1f, 1f)).normalized; var bl = len * .3f;
                        mk.Prism(drift, bs, bd, r * .4f, bl, 5, .4f);
                        BoxCol(ctx.Root, bs + bd * bl / 2f, new Vector3(r * .7f, r * .7f, bl), Quaternion.LookRotation(bd));   // the branch sticking up
                    }
                    if (r > .25f) solid.Box(drift, (e0 + e1) / 2f + Vector3.up * r * .6f, new Vector3(r * 2f, r * 1.4f, llen), Quaternion.LookRotation(ld));
                    logs++;
                }
                else if (roll < .3f && p.y < 3f)
                    mk.Blob(seaweed, p + Vector3.up * .05f, new Vector3(ctx.Range(.4f, 1f), .08f, ctx.Range(.3f, .8f)), ctx.Rng, .4f);
                else if (roll < .55f && p.y < 3.5f && glass < 460)
                {
                    var k = ctx.Rng.Next(glassMats.Length);
                    mk.Box(glassMats[k], p + Vector3.up * .03f, new Vector3(.08f, .03f, .06f), Quaternion.Euler(0, ctx.Range(0, 360f), 0));
                    glass++;
                }
                else if (roll < .7f && p.y < 3.5f)
                    mk.Blob(shell, p + Vector3.up * .03f, new Vector3(.07f, .03f, .06f), ctx.Rng, .2f);
                else if (roll < .72f)
                {
                    mk.Blob(buoy, p + Vector3.up * .3f, Vector3.one * .3f, ctx.Rng, 0f);
                    solid.Box(buoy, p + Vector3.up * .3f, new Vector3(.62f, .6f, .62f), Quaternion.identity);
                    mk.Box(buoyW, p + Vector3.up * .3f, new Vector3(.62f, .1f, .62f), Quaternion.identity);
                    mk.Box(rope, p + Vector3.up * .05f + Vector3.forward * .7f, new Vector3(.03f, .03f, 1.4f), Quaternion.Euler(0, ctx.Range(0, 360f), 0));
                }
                else if (roll < .73f)
                {
                    var rot = Quaternion.Euler(0, ctx.Range(0, 360f), 0);
                    mk.Box(pot, p + Vector3.up * .3f, new Vector3(.9f, .6f, .6f), rot);
                    solid.Box(pot, p + Vector3.up * .3f, new Vector3(.9f, .6f, .6f), rot);
                }
            }
            Debug.Log($"The Driftwood Shore: {logs} pieces of driftwood, {glass} pieces of sea glass");
        }

        // ---------------------------------------------------------------- dune grass and heath
        /// Silvery marram grass on the dunes, bent hard by the wind; grey-green heath with pink sea thrift inland.
        void GroundCover(LandContext ctx)
        {
            var marramMat = WindMesh.Material(.6f, 1.4f, DuneSand);
            var heathMat = WindMesh.Material(.18f, .9f, Heath);
            var marram = new Dictionary<(int, int), WindMesh>(); var heath = new Dictionary<(int, int), WindMesh>();
            var rng = new System.Random(6262);
            const float step = 1.9f;
            for (var x = ctx.MinX; x < ctx.MaxX; x += step)
            for (var z = ctx.MinZ; z < ctx.MaxZ; z += step)
            {
                var px = x + ((float)rng.NextDouble() - .5f) * step; var pz = z + ((float)rng.NextDouble() - .5f) * step;
                if (!ctx.IsLand(px, pz, 1f) || InFootprint(ctx, px, pz, 0f, .5f) || NearPath(px, pz, 2.2f) || InNoGrass(px, pz, 0f)) continue;
                var g = ctx.Ground(px, pz);
                if (g < 2.2f) continue;                                         // the wet sand stays bare
                var n = Mathf.PerlinNoise(px * .02f + 6f, pz * .02f + 3f);
                var key = (Mathf.FloorToInt(px / 70f), Mathf.FloorToInt(pz / 70f));
                var at = new Vector3(px, g - .04f, pz);
                if (g < 7f)
                {
                    if (n < .35f) continue;
                    if (!marram.TryGetValue(key, out var wm)) marram[key] = wm = new WindMesh();
                    var col = Color.Lerp(new Color(.56f, .6f, .52f), new Color(.72f, .72f, .62f), (float)rng.NextDouble());
                    wm.Tuft(at, .7f + (float)rng.NextDouble() * .7f, col, Color.Lerp(col, Pearl, .4f), rng, 5, .05f);
                }
                else
                {
                    if (n < .3f) continue;
                    if (!heath.TryGetValue(key, out var wm)) heath[key] = wm = new WindMesh();
                    var col = Color.Lerp(new Color(.38f, .44f, .38f), new Color(.5f, .54f, .46f), (float)rng.NextDouble());
                    wm.Tuft(at, .25f + (float)rng.NextDouble() * .2f, col, Color.Lerp(col, Pearl, .2f), rng, 4, .08f);
                    if (rng.NextDouble() < .12) wm.Flower(at + new Vector3(.2f, 0, .15f), .25f, new Color(.9f, .55f, .68f), rng);   // sea thrift
                }
            }
            foreach (var kv in marram) kv.Value.Build("Marram grass", ctx.Root, marramMat);
            foreach (var kv in heath) kv.Value.Build("Heath", ctx.Root, heathMat);
        }
    }
}
