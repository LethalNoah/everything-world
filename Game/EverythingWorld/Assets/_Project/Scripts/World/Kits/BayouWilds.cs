using System.Collections.Generic;
using UnityEngine;

namespace EverythingWorld.World
{
    /// The swamp between the porches: bald cypress standing in the shallows with their knees, live oaks dripping
    /// Spanish moss, palmetto clumps, tin shacks on stilts, fishing docks with jon boats, a rusted truck in the
    /// weeds, campfires ringed with lawn chairs, hammocks, herons, a bonfire site on the highest hill (lit high
    /// for "Fire on the Mountain"), and tall grass and reeds along the water.
    public partial class BayouKit
    {
        void Wilds(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            int cypress = 0, oaks = 0, palms = 0, shacks = 0, docks = 0, trucks = 0, camps = 0, benches = 0, herons = 0, hammocks = 0;
            // cypress along every bank, some standing out in the water
            foreach (var c in channels)
                for (var i = 2; i < c.pts.Count; i += 5)
                {
                    if (!ctx.Chance(c.main ? .45f : .6f)) continue;
                    var p = c.pts[i]; var dir = (c.pts[Mathf.Min(i + 1, c.pts.Count - 1)] - c.pts[i - 1]); dir.y = 0; if (dir.sqrMagnitude < .01f) continue;
                    var side = Vector3.Cross(Vector3.up, dir.normalized) * (ctx.Chance(.5f) ? 1f : -1f);
                    var off = c.w / 2f + ctx.Range(-2.5f, 3f);
                    var q = p + side * off;
                    if (!ctx.IsLand(q.x, q.z, 1f) || NearPath(q.x, q.z, 4f) || InFootprint(ctx, q.x, q.z, 2f)) continue;
                    var g = ctx.Ground(q.x, q.z);
                    CypressTree(ctx, mk, new Vector3(q.x, Mathf.Min(g, p.y) , q.z), ctx.Range(.8f, 1.3f), p.y);
                    cypress++;
                }
            foreach (var (pc, pr) in ponds)
                for (var k = 0; k < (pr > 10f ? 5 : 2); k++)
                {
                    var a = ctx.Range(0, Mathf.PI * 2f); var q = pc + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * (pr + ctx.Range(-1f, 2f));
                    if (InFootprint(ctx, q.x, q.z, 1f) || NearPath(q.x, q.z, 4f)) continue;
                    CypressTree(ctx, mk, new Vector3(q.x, Mathf.Min(ctx.Ground(q.x, q.z), pc.y), q.z), ctx.Range(.8f, 1.2f), pc.y); cypress++;
                }

            MountainFire(ctx, mk, solid);

            const float cell = 34f;
            for (var x = ctx.MinX; x < ctx.MaxX; x += cell)
            for (var z = ctx.MinZ; z < ctx.MaxZ; z += cell)
            {
                float px = x + ctx.Range(0f, cell), pz = z + ctx.Range(0f, cell);
                if (!Open(ctx, px, pz, 5f)) continue;
                var p = ctx.OnGround(px, pz);
                var nearWater = WaterAt(px, pz, 14f, out var surface);
                var nearRoad = NearPath(px, pz, 12f);
                var roll = ctx.Range(0f, 1f);
                if (nearWater && docks < 6 && roll < .22f) { if (Dock(ctx, mk, solid, p)) docks++; else continue; }
                else if (nearWater && shacks < 5 && roll < .36f) { StiltShack(ctx, mk, solid, p); shacks++; }
                else if (nearWater && roll < .5f) { Heron(ctx, mk, p); herons++; }
                else if (nearRoad && trucks < 3 && roll < .12f) { Pickup(ctx, mk, solid, p, Quaternion.Euler(0, ctx.Range(0, 360f), 0) * Vector3.forward, new Color(.55f, .3f, .2f)); trucks++; }
                else if (nearRoad && benches < 7 && roll < .24f) { Bench(ctx, mk, solid, p); benches++; }
                else if (roll < .42f) { for (var k = 0; k < ctx.Rng.Next(1, 3); k++) { var q = ctx.OnGround(px + ctx.Range(-7f, 7f), pz + ctx.Range(-7f, 7f)); if (WaterAt(q.x, q.z, 3f, out _)) continue; LiveOak(ctx, mk, q, ctx.Range(1.1f, 1.6f)); } oaks++; }
                else if (roll < .58f) { Palmettos(ctx, p); palms++; }
                else if (roll < .64f && camps < 4) { Camp(ctx, mk, solid, p); camps++; }
                else if (roll < .7f && hammocks < 3) { Hammock(ctx, mk, solid, p); hammocks++; }
                else if (roll < .82f) { CypressTree(ctx, mk, p, ctx.Range(.9f, 1.3f), -999f); cypress++; }
                else continue;
                noGrass.Add(new Vector3(px, 4f, pz));
            }
            Debug.Log($"Southern Rock Bayou: {channels.Count} channels, {ponds.Count} ponds, {cypress} cypress, {oaks} oak groups, {palms} palmetto clumps, {shacks} stilt shacks, {docks} docks, {trucks} trucks, {camps} camps, {benches} benches, {herons} herons, {hammocks} hammocks");
        }

        /// A bald cypress: a flared, buttressed foot, a tall straight trunk, flat feathery crowns, knees poking up
        /// round it, Spanish moss hanging.
        void CypressTree(LandContext ctx, MeshKit mk, Vector3 at, float s, float waterY)
        {
            var up = Vector3.up; var h = ctx.Range(11f, 16f) * s;
            mk.Prism(bark, at - up * 1f, up, 1.1f * s, 3f, 8, .45f);
            mk.Prism(bark, at + up * 1.8f, up, .5f * s, h - 1.8f, 7, .55f);
            var cypressLeaf = WorldMaterials.Solid(new Color(.38f, .5f, .26f));
            for (var k = 0; k < 4; k++)
            {
                var y = h * (.62f + k * .12f); var d = Quaternion.Euler(0, ctx.Range(0, 360f), 0) * Vector3.forward;
                var c = at + up * y + d * ctx.Range(.5f, 2f) * s;
                soft.Blob(cypressLeaf, c, new Vector3(ctx.Range(2.4f, 3.6f), .9f, ctx.Range(2.4f, 3.6f)) * s * (1f - k * .12f), ctx.Rng, .35f);
                mk.Prism(bark, at + up * (y - 1f), (d + up * .6f).normalized, .12f * s, 2.2f * s, 5, .5f);
                for (var m = 0; m < 3; m++) HangMoss(ctx, c + new Vector3(ctx.Range(-1.6f, 1.6f), -.5f, ctx.Range(-1.6f, 1.6f)) * s, ctx.Range(1.2f, 3f));
            }
            for (var k = 0; k < ctx.Rng.Next(3, 7); k++)
            {
                var a = ctx.Range(0, Mathf.PI * 2f); var r = ctx.Range(1.6f, 3.4f) * s;
                var kp = at + new Vector3(Mathf.Cos(a) * r, 0, Mathf.Sin(a) * r); kp.y = Mathf.Max(ctx.Ground(kp.x, kp.z), waterY - .4f) - .2f;
                mk.Cone(bark, kp, ctx.Range(.15f, .25f), ctx.Range(.4f, .9f), 5);
            }
            ArtistBits.PostCollider(ctx.Root, at, h * .7f, .6f * s);
            noGrass.Add(new Vector3(at.x, 1.5f * s, at.z));
        }

        /// Spanish moss: a few grey-green strands hanging from a branch.
        void HangMoss(LandContext ctx, Vector3 from, float len)
        {
            for (var k = 0; k < 3; k++)
            {
                var p = from + new Vector3(ctx.Range(-.3f, .3f), 0, ctx.Range(-.3f, .3f)); var l = len * ctx.Range(.6f, 1f);
                soft.Box(mossM, p - Vector3.up * l / 2f, new Vector3(.12f, l, .04f), Quaternion.Euler(0, ctx.Range(0, 180f), 0));
            }
        }

        /// A live oak: a short massive trunk, long low limbs spreading wide, dark leaves, moss hanging everywhere.
        void LiveOak(LandContext ctx, MeshKit mk, Vector3 at, float s)
        {
            var up = Vector3.up; var h = 3.4f * s;
            mk.Prism(bark, at - up * .4f, up, .7f * s, h + .4f, 8, .75f);
            var crown = at + up * h;
            var oakLeaf = WorldMaterials.Solid(new Color(.27f, .38f, .2f));
            for (var k = 0; k < 6; k++)
            {
                var d = Quaternion.Euler(0, k * 60f + ctx.Range(-20f, 20f), 0) * Vector3.forward; var reach = ctx.Range(4f, 6.5f) * s;
                var dirL = (d + up * ctx.Range(.1f, .35f)).normalized;
                mk.Prism(bark, crown - up * .3f, dirL, .3f * s, reach, 6, .45f);
                var tip = crown + dirL * reach;
                soft.Blob(oakLeaf, tip + up * .8f * s, new Vector3(2.6f, 1.4f, 2.6f) * s, ctx.Rng, .3f);
                soft.Blob(oakLeaf, crown + dirL * reach * .55f + up * 1.6f * s, new Vector3(2.4f, 1.5f, 2.4f) * s, ctx.Rng, .3f);
                for (var m = 0; m < 4; m++) HangMoss(ctx, Vector3.Lerp(crown, tip, ctx.Range(.4f, 1f)) - up * .2f, ctx.Range(1.5f, 3.2f) * Mathf.Min(1.3f, s));
            }
            soft.Blob(oakLeaf, crown + up * 2.6f * s, new Vector3(4f, 2f, 4f) * s, ctx.Rng, .3f);
            ArtistBits.PostCollider(ctx.Root, at, h, .8f * s);
            noGrass.Add(new Vector3(at.x, 1.5f * s, at.z));
        }

        void Palmettos(LandContext ctx, Vector3 at)
        {
            var frond = WorldMaterials.Solid(new Color(.36f, .48f, .26f));
            for (var k = 0; k < ctx.Rng.Next(3, 7); k++)
            {
                var p = ctx.OnGround(at.x + ctx.Range(-4f, 4f), at.z + ctx.Range(-4f, 4f));
                if (WaterAt(p.x, p.z, 1f, out _)) continue;
                for (var f = 0; f < 7; f++)
                {
                    var d = (Quaternion.Euler(0, f * 51f + ctx.Range(-10f, 10f), 0) * Vector3.forward + Vector3.up * ctx.Range(.5f, 1.4f)).normalized;
                    var tip = p + d * ctx.Range(1.1f, 1.6f);
                    var w = Vector3.Cross(d, Vector3.up).normalized * .35f;
                    soft.Tri(frond, p + Vector3.up * .1f, tip + w, tip - w, Vector3.up);
                    soft.Tri(frond, p + Vector3.up * .1f, tip - w, tip + w, Vector3.down);
                }
            }
        }

        /// A tin-roofed fishing shack on stilts at the water's edge, a lit window, a canoe pulled up.
        void StiltShack(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 at)
        {
            var up = Vector3.up; WaterAt(at.x, at.z, 20f, out var s);
            var toWater = Vector3.zero; for (var k = 0; k < 16; k++) { var d = Quaternion.Euler(0, k * 22.5f, 0) * Vector3.forward; var q = at + d * 10f; if (WaterAt(q.x, q.z, 0f, out _)) toWater += d; }
            var face = toWater.sqrMagnitude > .01f ? toWater.normalized : Vector3.forward; var rot = Quaternion.LookRotation(face); var side = Vector3.Cross(up, face);
            var floor = new Vector3(at.x, Mathf.Max(at.y, s) + 1.6f, at.z);
            foreach (var i in new[] { -1f, 1f }) foreach (var j in new[] { -1f, 1f }) { var p = at + side * i * 2.2f + face * j * 1.8f; var g = ctx.Ground(p.x, p.z); mk.Prism(greyWood, new Vector3(p.x, g - .5f, p.z), up, .14f, floor.y - g + .5f, 6); }
            mk.Box(plank, floor, new Vector3(5f, .2f, 4.4f), rot); solid.Box(plank, floor - up * .4f, new Vector3(5f, 1f, 4.4f), rot);
            var w = WorldMaterials.Solid(new Color(.48f, .44f, .38f));
            mk.Box(w, floor + up * 1.3f - face * .4f, new Vector3(4.4f, 2.6f, 3.2f), rot); solid.Box(w, floor + up * 1.3f - face * .4f, new Vector3(4.4f, 2.6f, 3.2f), rot);
            mk.GableRoof(tin, floor + up * 2.6f - face * .2f, new Vector3(4.4f, 0, 5f), 1.2f, Quaternion.LookRotation(side));
            mk.Box(GlowM(new Color(1f, .72f, .38f), 1.8f), floor + up * 1.5f + face * 1.21f + side * .9f, new Vector3(.8f, .7f, .04f), rot);
            mk.Box(dark, floor + up * 1f + face * 1.21f - side * .8f, new Vector3(.9f, 2f, .04f), rot);
            halos.Add(floor + up * 1.5f + face * 2f + side * .9f, 1.2f, new Color(1f, .72f, .38f), .6f);
            // a canoe pulled up beside it
            var cn = at + side * 3.8f; var cg = ctx.Ground(cn.x, cn.z);
            mk.Blob(WorldMaterials.Solid(new Color(.25f, .45f, .4f), .5f), new Vector3(cn.x, Mathf.Max(cg, s) + .2f, cn.z), new Vector3(.45f, .25f, 2.2f), ctx.Rng, 0f);
            noGrass.Add(new Vector3(at.x, 4f, at.z));
        }

        /// A plank fishing dock out over the water, a jon boat tied up, a bucket and a lantern at the end.
        bool Dock(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 at)
        {
            var up = Vector3.up; Vector3 dir = Vector3.zero; float best = float.MaxValue, s = 0f;
            for (var k = 0; k < 24; k++)
            {
                var d = Quaternion.Euler(0, k * 15f, 0) * Vector3.forward;
                for (var t = 2f; t < 16f; t += 1f) { var q = at + d * t; if (WaterAt(q.x, q.z, -2f, out var sy)) { if (t < best) { best = t; dir = d; s = sy; } break; } }
            }
            if (best == float.MaxValue) return false;
            var rot = Quaternion.LookRotation(dir); var side = Vector3.Cross(up, dir);
            var len = best + 5f; var y = Mathf.Max(at.y, s + .6f);
            var mid = new Vector3(at.x, y, at.z) + dir * len / 2f;
            mk.Box(plank, mid, new Vector3(1.8f, .15f, len), rot); solid.Box(plank, mid - up * .3f, new Vector3(1.8f, .7f, len), rot);
            for (var t = 1f; t <= len; t += 2.5f) foreach (var sd in new[] { -.85f, .85f }) { var p = new Vector3(at.x, 0, at.z) + dir * t + side * sd; var g = ctx.Ground(p.x, p.z); mk.Prism(greyWood, new Vector3(p.x, g - .4f, p.z), up, .1f, y - g + .9f, 6); }
            var end = new Vector3(at.x, y, at.z) + dir * len;
            var boat = end + side * 2f - dir * 1.5f; boat.y = s + .05f;
            mk.Box(WorldMaterials.Solid(new Color(.55f, .58f, .5f), .6f), boat + up * .25f, new Vector3(1.4f, .5f, 3.6f), rot);
            mk.Box(dark, boat + up * .35f, new Vector3(1.2f, .35f, 3.3f), rot);
            mk.Box(metal, boat + up * .6f - dir * 1.7f, new Vector3(.3f, .7f, .3f), rot);
            mk.Prism(metal, end + up * .08f - dir * .5f + side * .4f, up, .2f, .4f, 10);
            Lamp(ctx, mk, ctx.Root, end - dir * .3f - side * .6f, 2f);
            Remember(end, 998);
            return true;
        }

        void Heron(LandContext ctx, MeshKit mk, Vector3 at)
        {
            var up = Vector3.up; var d = Quaternion.Euler(0, ctx.Range(0, 360f), 0) * Vector3.forward;
            var grey = WorldMaterials.Solid(new Color(.72f, .74f, .78f)); var leg = WorldMaterials.Solid(new Color(.75f, .65f, .35f));
            foreach (var s in new[] { -.08f, .08f }) mk.Box(leg, at + up * .45f + Vector3.Cross(up, d) * s, new Vector3(.03f, .9f, .03f), Quaternion.LookRotation(d));
            mk.Blob(grey, at + up * 1.05f, new Vector3(.22f, .25f, .45f), ctx.Rng, 0f);
            mk.Box(grey, at + up * 1.45f + d * .25f, new Vector3(.06f, .55f, .06f), Quaternion.LookRotation(d) * Quaternion.Euler(20f, 0, 0));
            mk.Blob(grey, at + up * 1.75f + d * .32f, Vector3.one * .08f, ctx.Rng, 0f);
            mk.Box(leg, at + up * 1.75f + d * .52f, new Vector3(.03f, .03f, .3f), Quaternion.LookRotation(d));
            life.herons.Add(at);
        }

        void Bench(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 at)
        {
            var face = new Vector3(center.x - at.x, 0, center.z - at.z).normalized; var rot = Quaternion.LookRotation(face); var up = Vector3.up;
            mk.Box(plank, at + up * .45f, new Vector3(1.8f, .08f, .5f), rot);
            mk.Box(plank, at + up * .8f - face * .25f, new Vector3(1.8f, .5f, .06f), rot);
            foreach (var s in new[] { -.8f, .8f }) mk.Box(greyWood, at + up * .22f + Vector3.Cross(up, face) * s, new Vector3(.08f, .45f, .45f), rot);
            solid.Box(plank, at + up * .5f, new Vector3(1.9f, 1f, .6f), rot);
            Lamp(ctx, mk, ctx.Root, at + Vector3.Cross(up, face) * 1.4f);
        }

        /// A campfire ringed with stones, lawn chairs and a cooler round it.
        void Camp(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 at)
        {
            var up = Vector3.up; var stone = WorldMaterials.Solid(new Color(.5f, .48f, .44f));
            for (var k = 0; k < 9; k++) { var d = Quaternion.Euler(0, k * 40f, 0) * Vector3.forward; mk.Blob(stone, at + d * .9f + up * .12f, new Vector3(.25f, .18f, .22f), ctx.Rng, .3f); }
            for (var k = 0; k < 3; k++) { var d = Quaternion.Euler(0, k * 60f, 0) * Vector3.forward; mk.Box(bark, at + up * .2f, new Vector3(.18f, .18f, 1.2f), Quaternion.LookRotation(d)); }
            Color[] chair = { new Color(.2f, .5f, .7f), new Color(.85f, .55f, .2f), new Color(.3f, .6f, .35f) };
            for (var k = 0; k < 3; k++)
            {
                var d = Quaternion.Euler(0, k * 120f + 30f, 0) * Vector3.forward; var p = ctx.OnGround(at.x + d.x * 2.6f, at.z + d.z * 2.6f); var r = Quaternion.LookRotation(-d);
                mk.Box(WorldMaterials.Solid(chair[k]), p + up * .35f, new Vector3(.6f, .06f, .5f), r);
                mk.Box(WorldMaterials.Solid(chair[k]), p + up * .7f + d * .25f, new Vector3(.6f, .6f, .05f), r * Quaternion.Euler(-15f, 0, 0));
                solid.Box(dark, p + up * .5f, new Vector3(.7f, 1f, .7f), r);
            }
            var cooler = ctx.OnGround(at.x + 2.2f, at.z - 1.4f); mk.Box(WorldMaterials.Solid(new Color(.85f, .25f, .2f)), cooler + up * .25f, new Vector3(.7f, .5f, .45f), Quaternion.identity); mk.Box(white, cooler + up * .53f, new Vector3(.72f, .08f, .47f), Quaternion.identity);
            var fl = ArtistBits.PointLight(ctx.Root, at + up * 1f, new Color(1f, .55f, .2f), 9f, 1.5f); fl.gameObject.AddComponent<FireFlicker>().baseIntensity = 1.5f;
            ArtistBits.PostCollider(ctx.Root, at, .5f, .9f);
            life.fires.Add(at + up * .25f);
        }

        void Hammock(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 at)
        {
            var d = Quaternion.Euler(0, ctx.Range(0, 180f), 0) * Vector3.forward; var up = Vector3.up;
            var a = ctx.OnGround(at.x - d.x * 3.5f, at.z - d.z * 3.5f); var b = ctx.OnGround(at.x + d.x * 3.5f, at.z + d.z * 3.5f);
            LiveOak(ctx, mk, a, .9f); LiveOak(ctx, mk, b, .85f);
            var cloth = WorldMaterials.Solid(new Color(.85f, .75f, .5f));
            for (var k = 0; k < 6; k++)
            {
                float t0 = .12f + k * .127f, t1 = t0 + .127f;
                var p0 = Vector3.Lerp(a, b, t0) + up * (1.6f - Mathf.Sin(t0 * Mathf.PI) * .8f); var p1 = Vector3.Lerp(a, b, t1) + up * (1.6f - Mathf.Sin(t1 * Mathf.PI) * .8f);
                mk.Box(cloth, (p0 + p1) / 2f, new Vector3(1f, .05f, Vector3.Distance(p0, p1) + .02f), Quaternion.LookRotation(p1 - p0));
            }
            foreach (var (e, t) in new[] { (a, .12f), (b, .88f) }) { var p = Vector3.Lerp(a, b, t) + up * 1.6f; mk.Box(rope, (e + up * 1.7f + p) / 2f, new Vector3(.04f, .04f, Vector3.Distance(e + up * 1.7f, p)), Quaternion.LookRotation(p - e - up * 1.7f)); }
        }

        /// "Fire on the Mountain": a bonfire site on the highest open hill: a great stacked pyre in a ring of stones,
        /// split-log benches, a footpath up to it. It always smoulders; the song sets it roaring.
        void MountainFire(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            Vector3 best = default; var bestH = float.MinValue;
            IslandWorldData.Artist mtb = null; foreach (var a in ctx.Artists) if (a.name == "The Marshall Tucker Band") mtb = a;
            for (var t = 0; t < 600; t++)
            {
                float x = ctx.Range(ctx.MinX, ctx.MaxX), z = ctx.Range(ctx.MinZ, ctx.MaxZ);
                if (!ctx.IsLand(x, z, 30f) || !Open(ctx, x, z, 14f)) continue;
                if (mtb != null && Vector2.Distance(new Vector2(x, z), new Vector2(mtb.x, mtb.z)) > 140f) continue;
                var h = ctx.Ground(x, z); if (h > bestH) { bestH = h; best = ctx.OnGround(x, z); }
            }
            if (bestH == float.MinValue) return;
            var up = Vector3.up; var stone = WorldMaterials.Solid(new Color(.55f, .52f, .47f));
            for (var k = 0; k < 14; k++) { var d = Quaternion.Euler(0, k * 360f / 14f, 0) * Vector3.forward; mk.Blob(stone, ctx.OnGround(best.x + d.x * 3.2f, best.z + d.z * 3.2f) + up * .2f, new Vector3(.45f, .35f, .4f), ctx.Rng, .3f); }
            for (var k = 0; k < 10; k++)
            {
                var a = k * 36f; var d = Quaternion.Euler(0, a, 0) * Vector3.forward;
                mk.Prism(bark, best + d * 1.6f, (-d + up * 1.8f).normalized, .16f, 3.8f, 6);
            }
            for (var k = 0; k < 5; k++) { var d = Quaternion.Euler(0, k * 72f + 20f, 0) * Vector3.forward; var p = ctx.OnGround(best.x + d.x * 7f, best.z + d.z * 7f) + up * .3f; var sd = Vector3.Cross(up, d); mk.Prism(bark, p - sd * 1.3f, sd, .3f, 2.6f, 8); solid.Box(bark, p, new Vector3(.6f, .6f, 2.6f), Quaternion.LookRotation(sd)); }
            solid.Box(bark, best + up * 1.5f, new Vector3(3.6f, 3f, 3.6f), Quaternion.identity);
            var fl = ArtistBits.PointLight(ctx.Root, best + up * 3f, new Color(1f, .5f, .2f), 20f, 1.4f); fl.gameObject.AddComponent<FireFlicker>().baseIntensity = 1.4f;
            life.mountainFire = best; life.mountainLight = fl;
            ArtistBits.Label(ctx.Root, "FIRE ON THE MOUNTAIN", ctx.OnGround(best.x + 5f, best.z + 5f) + up * 1.6f, (new Vector3(center.x - best.x, 0, center.z - best.z)).normalized, .22f, Ink, TextAnchor.MiddleCenter);
            noGrass.Add(new Vector3(best.x, 9f, best.z));
            Footpath(ctx, mk, solid, best, 6f);
        }

        /// A footpath from the nearest road to a place, bridged over any water on the way.
        void Footpath(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 to, float stopShort)
        {
            Vector3 from = center; var bd = float.MaxValue;
            foreach (var (p, w) in walkPoints) { if (w >= 998) continue; var d = (p.x - to.x) * (p.x - to.x) + (p.z - to.z) * (p.z - to.z); if (d < bd) { bd = d; from = p; } }
            var dir = new Vector3(to.x - from.x, 0, to.z - from.z); var total = dir.magnitude - stopShort; if (total < 4f) return; dir.Normalize();
            var side = Vector3.Cross(Vector3.up, dir);
            var pts = new List<Vector3>(); var wet = new List<bool>();
            for (var d = RoadW / 2f; ; d += 3f)
            {
                var dd = Mathf.Min(d, total);
                var q = from + dir * dd + side * Mathf.Sin(dd / total * Mathf.PI * 2f) * 4f;
                var g = ctx.Ground(q.x, q.z); var over = WaterAt(q.x, q.z, 2f, out var s);
                pts.Add(new Vector3(q.x, over ? Mathf.Max(g, s + .7f) : g, q.z)); wet.Add(over);
                if (dd >= total) break;
            }
            var dirt = WorldMaterials.Solid(new Color(.55f, .48f, .36f));
            var run = new List<Vector3>(); var runWet = wet[0];
            for (var i = 0; i <= pts.Count; i++)
            {
                if (i < pts.Count && (run.Count == 0 || wet[i] == runWet)) { run.Add(pts[i]); runWet = wet[i]; continue; }
                if (run.Count > 1) { if (runWet) Bridge(ctx, mk, solid, run); else Strip(mk, run, dirt, 2.4f, .05f); }
                if (i < pts.Count) { var last = run[run.Count - 1]; run = new List<Vector3> { last, pts[i] }; runWet = wet[i]; }
            }
            foreach (var p in pts) Remember(p, 997);
        }

        /// Tall swamp grass and ferns over the land, reeds and cattails crowding the water's edge.
        void GroundCover(LandContext ctx)
        {
            var mat = WindMesh.Material(.35f, 1f, Cypress);
            var meshes = new Dictionary<(int, int), WindMesh>();
            var rng = new System.Random(1971);
            const float step = 2f;
            for (var x = ctx.MinX; x < ctx.MaxX; x += step)
            for (var z = ctx.MinZ; z < ctx.MaxZ; z += step)
            {
                var px = x + ((float)rng.NextDouble() - .5f) * step; var pz = z + ((float)rng.NextDouble() - .5f) * step;
                if (!ctx.IsLand(px, pz, 1f) || NearPath(px, pz, 3.2f) || InNoGrass(px, pz, 0f) || InFootprint(ctx, px, pz, 0f, .7f)) continue;
                var wetEdge = WaterAt(px, pz, 3f, out var s);
                var g = ctx.Ground(px, pz);
                if (wetEdge && g < s - .5f) continue;                                     // open water
                var n = Mathf.PerlinNoise(px * .025f + 5f, pz * .025f + 1f);
                if (!wetEdge && n < .35f) continue;
                var key = (Mathf.FloorToInt(px / 70f), Mathf.FloorToInt(pz / 70f));
                if (!meshes.TryGetValue(key, out var wm)) meshes[key] = wm = new WindMesh();
                var at = new Vector3(px, Mathf.Max(g, wetEdge ? s - .2f : g) - .04f, pz);
                if (wetEdge)
                {
                    var col = Color.Lerp(new Color(.4f, .5f, .25f), new Color(.6f, .6f, .32f), (float)rng.NextDouble());
                    wm.Tuft(at, 1.2f + (float)rng.NextDouble() * .9f, col, Color.Lerp(col, new Color(.85f, .8f, .5f), .4f), rng, 4, .05f);
                    if (rng.NextDouble() < .15) wm.Flower(at + new Vector3(.1f, 0, .1f), 1.6f, new Color(.45f, .3f, .18f), rng);   // cattail
                }
                else
                {
                    var col = Color.Lerp(new Color(.32f, .45f, .22f), new Color(.55f, .56f, .3f), (float)rng.NextDouble());
                    wm.Tuft(at, .5f + (float)rng.NextDouble() * .6f, col, Color.Lerp(col, new Color(.85f, .82f, .5f), .3f), rng, 4, .08f);
                    if (rng.NextDouble() < .04) wm.Flower(at + new Vector3(.2f, 0, .1f), .45f, new Color(.95f, .85f, .3f), rng);
                    else if (rng.NextDouble() < .03) wm.Flower(at + new Vector3(-.15f, 0, .2f), .5f, new Color(.95f, .55f, .2f), rng);
                }
            }
            foreach (var kv in meshes) kv.Value.Build("Bayou grass", ctx.Root, mat);
        }
    }
}
