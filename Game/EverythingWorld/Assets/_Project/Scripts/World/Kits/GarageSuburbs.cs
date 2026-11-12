using System.Collections.Generic;
using UnityEngine;

namespace EverythingWorld.World
{
    /// Garage Rock Row's suburbs: the strip stays the loud heart, and round it, behind the power lines, a quiet grid
    /// of residential streets runs parallel to the Row, crossed every block, lined with small nondescript houses
    /// (gable roofs, a porch light, a mailbox, a driveway, now and then a car or a tree). Late at night: most windows
    /// dark, some lit warm, a few flickering blue with a TV, and here and there a garage with its door up and a
    /// band's gear inside, the glow spilling onto the driveway (where garage rock starts). The song places sit on
    /// lots in these streets, each under its tall neon sign.
    public partial class GarageKit
    {
        // the grid, in the Row's own frame: t along the Row from where you arrive, u across it
        const float SubFirst = 64f, SubStep = 48f, CrossStep = 52f, SubRoadHalf = 3.5f, SubWalk = 1.4f;
        const int SubRows = 3;
        float SubLastU => SubFirst + (SubRows - 1) * SubStep;
        float CrossT(int k) => k * CrossStep - 26f;
        int CrossCount => Mathf.FloorToInt((streetLen + 66f) / CrossStep) + 1;

        Vector3 SubSide => Vector3.Cross(Vector3.up, streetDir);
        Vector3 SubXZ(float t, float u) => streetA + streetDir * t + SubSide * u;
        (float t, float u) SubFrame(float x, float z) { var rel = new Vector3(x - streetA.x, 0, z - streetA.z); return (Vector3.Dot(rel, streetDir), Vector3.Dot(rel, SubSide)); }

        /// On a suburb street (or its pavement), with some margin.
        bool OnSubRoad(float x, float z, float pad)
        {
            if (!hasStreet) return false;
            var (t, u) = SubFrame(x, z); var au = Mathf.Abs(u); var half = SubRoadHalf + SubWalk + pad;
            if (t < -34f - pad || t > streetLen + 44f + pad) return false;
            for (var k = 0; k < SubRows; k++) if (Mathf.Abs(au - (SubFirst + k * SubStep)) < half) return true;
            if (au > 44f - pad && au < SubLastU + half)
                for (var k = 0; k < CrossCount; k++) if (Mathf.Abs(t - CrossT(k)) < half) return true;
            return false;
        }

        /// Inside the suburbs' rectangle (streets and blocks).
        bool InSuburbs(float x, float z, float pad)
        {
            if (!hasStreet) return false;
            var (t, u) = SubFrame(x, z); var au = Mathf.Abs(u);
            return t > -40f - pad && t < streetLen + 50f + pad && au > 40f - pad && au < SubLastU + 22f + pad;
        }

        /// Smooth the ground under the suburbs so streets and houses sit on gentle slopes (the coast is left alone).
        void SmoothSuburbs(IslandWorldData data, int index, Vector2 heart, Vector2 dir)
        {
            var across = new Vector2(-dir.y, dir.x);
            var src = (float[])data.heights.Clone();
            const int R = 3;
            for (var r = 0; r < data.Nz; r++)
            {
                var z = data.layout.depth - r * data.Step;
                for (var c = 0; c < data.Nx; c++)
                {
                    var i = r * data.Nx + c;
                    if (data.islandIds[i] != index) continue;
                    var rel = new Vector2(c * data.Step, z) - heart;
                    var t = Vector2.Dot(rel, dir); var au = Mathf.Abs(Vector2.Dot(rel, across));
                    var w = Mathf.Clamp01((au - 38f) / 18f) * Mathf.Clamp01((SubLastU + 40f - au) / 20f)
                          * Mathf.Clamp01((t + 70f) / 25f) * Mathf.Clamp01((streetLen + 85f - t) / 25f);
                    if (w <= 0f) continue;
                    float sum = 0f; var n = 0; var coast = false;
                    for (var dr = -R; dr <= R && !coast; dr++)
                        for (var dc = -R; dc <= R; dc++)
                        {
                            int rr = r + dr, cc = c + dc;
                            if (rr < 0 || cc < 0 || rr >= data.Nz || cc >= data.Nx) { coast = true; break; }
                            var j = rr * data.Nx + cc;
                            if (src[j] < 1.6f) { coast = true; break; }
                            sum += src[j]; n++;
                        }
                    if (coast || n == 0) continue;
                    data.heights[i] = Mathf.Lerp(src[i], Mathf.Max(sum / n, 2.2f), .85f * w);
                }
            }
        }

        // ---------------------------------------------------------------- the song places' lots
        /// A lot on the outer side of a suburb street, facing it (toward the Row), clear of everything.
        Vector3 SuburbLot(LandContext ctx, float clear)
        {
            var cands = new List<Vector3>();
            for (var k = 0; k < SubRows; k++)
                for (var j = 0; j + 1 < CrossCount; j++)
                    foreach (var s in new[] { -1f, 1f })
                        foreach (var f in new[] { .3f, .5f, .7f })
                        {
                            var t = Mathf.Lerp(CrossT(j), CrossT(j + 1), f);
                            cands.Add(SubXZ(t, s * (SubFirst + k * SubStep + SubRoadHalf + SubWalk + clear + 1.5f)));
                        }
            for (var i = cands.Count - 1; i > 0; i--) { var j = ctx.Rng.Next(i + 1); (cands[i], cands[j]) = (cands[j], cands[i]); }
            foreach (var q in cands)
            {
                float px = q.x, pz = q.z;
                if (!ctx.IsLand(px, pz, clear + 8f) || !ctx.IsFree(px, pz, clear) || OnTown(px, pz, clear) || InNoGrass(px, pz, clear) || InRing(ctx, q, clear + 2f)) continue;
                if (OnSubRoad(px, pz, clear - SubRoadHalf - SubWalk + .5f) && !OnlyFrontRoad(px, pz, clear)) continue;
                if (!Gentle(ctx, px, pz, clear + 2f, clear * .4f)) continue;
                var ok = true; foreach (var s in placeSpots) if (Vector2.Distance(new Vector2(s.x, s.z), new Vector2(px, pz)) < clear + 26f) { ok = false; break; }
                if (!ok) continue;
                var p = ctx.OnGround(px, pz); placeSpots.Add(p); ctx.AddKeepout(p.x, p.z, clear + 3f); noGrass.Add(new Vector3(p.x, clear + 6f, p.z)); return p;
            }
            return Vector3.positiveInfinity;
        }

        /// The lot touches no street except the one in front of it (cross streets would run through it).
        bool OnlyFrontRoad(float x, float z, float clear)
        {
            var (t, _) = SubFrame(x, z);
            for (var k = 0; k < CrossCount; k++) if (Mathf.Abs(t - CrossT(k)) < clear + SubRoadHalf + SubWalk + 1f) return false;
            return true;
        }

        // ---------------------------------------------------------------- the streets and houses
        bool NearPlace(Vector3 q, float r) { foreach (var p in placeSpots) if (new Vector2(q.x - p.x, q.z - p.z).magnitude < r) return true; return false; }

        static readonly Color[] HouseC = { new Color(.82f, .78f, .68f), new Color(.66f, .72f, .76f), new Color(.72f, .78f, .7f), new Color(.86f, .84f, .8f), new Color(.7f, .64f, .56f), new Color(.6f, .62f, .66f), new Color(.8f, .74f, .6f) };
        static readonly Color[] RoofC = { new Color(.22f, .22f, .25f), new Color(.32f, .24f, .2f), new Color(.26f, .3f, .32f), new Color(.38f, .34f, .3f) };
        static readonly Color Porch = new Color(1f, .8f, .45f), TvBlue = new Color(.45f, .65f, 1f);

        void Suburbs(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            if (!hasStreet) return;
            var up = Vector3.up;
            var road = WorldMaterials.Solid(new Color(.19f, .2f, .22f), .8f);
            var pave = WorldMaterials.Solid(new Color(.42f, .42f, .42f));
            var lampM = GlowM(Sodium, 2.4f);
            const float seg = 4f;
            bool SegOk(Vector3 a, Vector3 b) => ctx.IsLand(a.x, a.z, 3f) && ctx.IsLand(b.x, b.z, 3f) && Mathf.Abs(ctx.Ground(a.x, a.z) - ctx.Ground(b.x, b.z)) < 2f && !InRing(ctx, a, 1f) && !InRing(ctx, b, 1f);
            // one strip of road: asphalt with a pale pavement both sides, each corner on the ground
            void Strip(Vector3 a, Vector3 b)
            {
                var d = b - a; d.y = 0; var sd = Vector3.Cross(up, d.normalized);
                Vector3 G(Vector3 p, float o) { var q = p + sd * o; return ctx.OnGround(q.x, q.z); }
                var lift = up * .09f;
                mk.Quad(road, G(a, -SubRoadHalf) + lift, G(a, SubRoadHalf) + lift, G(b, SubRoadHalf) + lift, G(b, -SubRoadHalf) + lift, (a + b) / 2f - up * 2f);
                foreach (var s in new[] { -1f, 1f })
                {
                    float o0 = s * SubRoadHalf, o1 = s * (SubRoadHalf + SubWalk); var pl = up * .13f;
                    mk.Quad(pave, G(a, o0) + pl, G(a, o1) + pl, G(b, o1) + pl, G(b, o0) + pl, (a + b) / 2f - up * 2f);
                }
            }
            var roadCount = 0;
            // the long streets, parallel to the Row
            for (var k = 0; k < SubRows; k++)
                foreach (var s in new[] { -1f, 1f })
                    for (var t = -30f; t < streetLen + 40f; t += seg)
                    {
                        var a = SubXZ(t, s * (SubFirst + k * SubStep)); var b = SubXZ(t + seg, s * (SubFirst + k * SubStep));
                        if (!SegOk(a, b)) continue;
                        Strip(a, b); roadCount++;
                    }
            // the cross streets, from behind the town out through every row (breaking where they meet a long street)
            for (var j = 0; j < CrossCount; j++)
                foreach (var s in new[] { -1f, 1f })
                    for (var u = 44f; u < SubLastU; u += seg)
                    {
                        var u2 = Mathf.Min(u + seg, SubLastU);
                        var blocked = false;
                        for (var k = 0; k < SubRows; k++) { var pu = SubFirst + k * SubStep; if (u2 > pu - SubRoadHalf - SubWalk && u < pu + SubRoadHalf + SubWalk) blocked = true; }
                        if (blocked) continue;
                        var a = SubXZ(CrossT(j), s * u); var b = SubXZ(CrossT(j), s * u2);
                        if (!SegOk(a, b)) continue;
                        Strip(a, b); roadCount++;
                    }
            // the houses, both sides of every long street
            var tv = new Material(GlowM(TvBlue, 2f)) { name = "TV glow" }; life.tv = tv;
            var houses = 0; var garages = 0; var markDone = false;
            for (var k = 0; k < SubRows; k++)
                foreach (var s in new[] { -1f, 1f })
                    foreach (var toward in new[] { -1f, 1f })   // -1: the side nearer the Row
                        for (var j = 0; j + 1 < CrossCount; j++)
                        {
                            float t0 = CrossT(j) + SubRoadHalf + SubWalk + 5f, t1 = CrossT(j + 1) - SubRoadHalf - SubWalk - 5f;
                            var n = Mathf.Max(1, Mathf.FloorToInt((t1 - t0) / 13.5f) + 1); var gap = n > 1 ? (t1 - t0) / (n - 1) : 0f;
                            for (var h = 0; h < n; h++)
                            {
                                var t = n > 1 ? t0 + h * gap : (t0 + t1) / 2f;
                                var pu = SubFirst + k * SubStep; var u = s * (pu + toward * (SubRoadHalf + SubWalk + 4.5f + 4f));
                                var c = SubXZ(t, u);
                                var face = (SubXZ(t, s * pu) - c).normalized;
                                if (House(ctx, mk, solid, c, face, houses, tv, ref garages)) houses++;
                                if (!markDone && houses > 6 && k == 0)
                                {
                                    var cam = SubXZ(t - 20f, s * pu) ; cam = ctx.OnGround(cam.x, cam.z) + up * 1.6f;
                                    var m = new GameObject("Shot: the suburbs").transform; m.SetParent(ctx.Root, false);
                                    m.SetPositionAndRotation(cam, Quaternion.LookRotation(streetDir * 1f + face * -.25f));
                                    markDone = true;
                                }
                            }
                        }
            // low sodium lamps along the long streets, every other one casting light
            var lampN = 0;
            for (var k = 0; k < SubRows; k++)
                foreach (var s in new[] { -1f, 1f })
                    for (var t = -20f; t < streetLen + 36f; t += 34f)
                    {
                        var side = (lampN % 2 == 0) ? 1f : -1f; var u = s * (SubFirst + k * SubStep) + side * (SubRoadHalf + SubWalk - .3f);
                        var q = SubXZ(t, u); if (!ctx.IsLand(q.x, q.z, 4f) || InRing(ctx, q, 1f) || InNoGrass(q.x, q.z, .5f)) continue;
                        var p = ctx.OnGround(q.x, q.z); var inward = (SubXZ(t, s * (SubFirst + k * SubStep)) - q).normalized;
                        mk.Prism(metal, p - up * .3f, up, .08f, 5.8f, 6);
                        var head = p + up * 5.5f + inward * 1f;
                        mk.Box(metal, p + up * 5.55f + inward * .5f, new Vector3(.08f, .08f, 1.1f), Quaternion.LookRotation(inward));
                        mk.Box(lampM, head - up * .08f, new Vector3(.45f, .1f, .3f), Quaternion.LookRotation(inward));
                        halos.Add(head - up * .4f, 1.6f, Sodium, .7f);
                        if (lampN % 2 == 0) ArtistBits.PointLight(ctx.Root, head - up * .5f, Sodium, 18f, 1.8f);
                        ArtistBits.PostCollider(ctx.Root, p, 5.5f, .12f); noGrass.Add(new Vector3(p.x, .8f, p.z));
                        lampN++;
                    }
            Debug.Log($"Garage Rock Row: suburbs with {roadCount} road pieces, {houses} houses ({garages} band garages)");
        }

        /// One small house facing the street: a foundation down to the ground, walls, a gable roof, a door with a
        /// porch light, two front windows (dark, lit, or a TV's blue), a mailbox and a driveway; sometimes an
        /// attached garage with its door up and a band's gear inside, sometimes a car or a tree.
        bool House(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 c, Vector3 face, int i, Material tv, ref int garages)
        {
            var up = Vector3.up; var side = Vector3.Cross(up, face); var rot = Quaternion.LookRotation(face);
            var W = ctx.Range(7f, 9.5f); const float D = 7.5f; const float Hh = 3f;
            var hasGarage = ctx.Chance(.14f);
            var span = W / 2f + (hasGarage ? 5.5f : 1f);
            // footprint must be clear: land, off the roads, out of artist rings and song places
            float lo = float.MaxValue, hi = float.MinValue;
            foreach (var (x, z) in new[] { (-1f, -1f), (1f, -1f), (1f, 1f), (-1f, 1f), (0f, 0f) })
            {
                var q = c + side * x * span + face * z * (D / 2f + 1f);
                if (!ctx.IsLand(q.x, q.z, 3f) || OnSubRoad(q.x, q.z, .3f) || InRing(ctx, q, 2f) || NearPlace(q, 14f) || OnTown(q.x, q.z, 2f)) return false;
                var g = ctx.Ground(q.x, q.z); lo = Mathf.Min(lo, g); hi = Mathf.Max(hi, g);
            }
            if (hi - lo > 3f || !ctx.IsFree(c.x, c.z, 4f)) return false;
            var floor = hi + .3f; var b = new Vector3(c.x, floor, c.z);
            var wallM = WorldMaterials.Solid(HouseC[i % HouseC.Length]);
            var roofM = WorldMaterials.Solid(RoofC[(i / 3) % RoofC.Length]);
            var foundM = WorldMaterials.Solid(new Color(.36f, .35f, .34f));
            var fh = floor - lo + .4f;
            mk.Box(foundM, b - up * (fh / 2f), new Vector3(W + .2f, fh, D + .2f), rot);
            mk.Box(wallM, b + up * Hh / 2f, new Vector3(W, Hh, D), rot);
            solid.Box(wallM, b + up * (Hh / 2f - fh / 2f), new Vector3(W, Hh + fh, D), rot);
            mk.GableRoof(roofM, b + up * Hh, new Vector3(D + .9f, 0, W + .9f), ctx.Range(1.4f, 2.2f), Quaternion.LookRotation(side));
            // the front: door, porch light, steps, two windows
            var front = b + face * (D / 2f + .02f);
            var doorX = ctx.Chance(.5f) ? -W * .22f : W * .22f;
            mk.Box(WorldMaterials.Solid(Color.Lerp(HouseC[(i + 3) % HouseC.Length], new Color(.2f, .2f, .22f), .6f)), front + side * doorX + up * 1.05f, new Vector3(1f, 2.1f, .06f), rot);
            mk.Box(foundM, front + side * doorX + face * .5f + up * -.1f, new Vector3(1.6f, .4f, 1f), rot);
            solid.Box(foundM, front + side * doorX + face * .5f - up * .2f, new Vector3(1.6f, .6f, 1f), rot);
            var lit = ctx.Range(0f, 1f);
            var porchOn = lit < .7f;
            if (porchOn)
            {
                var pl = front + side * (doorX + .75f) + up * 2.2f + face * .1f;
                mk.Box(GlowM(Porch, 2.4f), pl, new Vector3(.18f, .26f, .14f), rot);
                halos.Add(pl + face * .5f, 1.3f, Porch, .6f);
            }
            var winX = -doorX * 1.1f; var winY = up * 1.6f;
            Material win;
            if (lit < .3f) win = GlowM(new Color(1f, .82f, .5f), 1.8f);
            else if (lit < .42f) win = tv;
            else win = WorldMaterials.Solid(new Color(.12f, .14f, .18f), .9f);
            mk.Box(win, front + side * winX + winY, new Vector3(1.8f, 1.1f, .05f), rot);
            if (lit < .42f) halos.Add(front + side * winX + winY + face * .6f, 1.2f, lit < .3f ? new Color(1f, .82f, .5f) : TvBlue, .35f);
            // the driveway and the mailbox at the pavement
            var drive = side * (W / 2f + (hasGarage ? 2.7f : 1.6f));
            var dStart = c + drive + face * (D / 2f - 1f); var dEnd = c + drive + face * (D / 2f + 4.6f);
            var dMid = (dStart + dEnd) / 2f; var dLen = Vector3.Distance(dStart, dEnd);
            var dy = Mathf.Max(ctx.Ground(dStart.x, dStart.z), ctx.Ground(dEnd.x, dEnd.z));
            mk.Box(WorldMaterials.Solid(new Color(.38f, .38f, .37f)), new Vector3(dMid.x, dy + .02f, dMid.z), new Vector3(hasGarage ? 4.4f : 2.8f, .14f, dLen), rot);
            noGrass.Add(new Vector3(dMid.x, 2.6f, dMid.z));
            var mb = c - drive * .2f + face * (D / 2f + 4.2f) + side * (doorX > 0 ? -1f : 1f) * 1.2f; mb = ctx.OnGround(mb.x, mb.z);
            mk.Prism(wood, mb - up * .2f, up, .05f, 1.25f, 6);
            mk.Box(WorldMaterials.Solid(i % 4 == 0 ? new Color(.6f, .15f, .12f) : new Color(.25f, .27f, .3f)), mb + up * 1.15f, new Vector3(.25f, .25f, .45f), rot);
            ArtistBits.PostCollider(ctx.Root, mb, 1.2f, .12f);
            if (hasGarage)
            {
                // the band's garage: door rolled up, a bulb, an amp, a drum kit, a rug, the glow spilling out
                garages++;
                var g = b + side * (W / 2f + 2.7f) + face * .5f; var gw = 5f; var gd = 6f; var gh = 2.8f; var gb = g - face * .5f;
                var gfh = floor - lo + .4f;
                mk.Box(foundM, gb - up * (gfh / 2f), new Vector3(gw + .2f, gfh, gd + .2f), rot);
                solid.Box(foundM, gb - up * (gfh / 2f), new Vector3(gw + .2f, gfh, gd + .2f), rot);   // the garage floor stands on its slab, not on the slope below
                mk.Box(wallM, gb - face * (gd / 2f - .1f) + up * gh / 2f, new Vector3(gw, gh, .2f), rot);
                foreach (var sx in new[] { -1f, 1f }) mk.Box(wallM, gb + side * sx * (gw / 2f - .1f) + up * gh / 2f, new Vector3(.2f, gh, gd - .4f), rot);
                mk.Box(wallM, gb + up * (gh + .1f), new Vector3(gw, .2f, gd), rot);
                mk.Box(WorldMaterials.Solid(new Color(.85f, .84f, .8f)), gb + face * (gd / 2f - .2f) + up * (gh - .25f), new Vector3(gw - .4f, .5f, .3f), rot);   // the rolled-up door
                foreach (var sx in new[] { -1f, 1f }) solid.Box(wallM, gb + side * sx * (gw / 2f - .1f) + up * gh / 2f, new Vector3(.2f, gh, gd), rot);
                solid.Box(wallM, gb - face * (gd / 2f - .1f) + up * gh / 2f, new Vector3(gw, gh, .2f), rot);
                var warm = new Color(1f, .78f, .45f);
                mk.Box(WorldMaterials.Solid(new Color(.5f, .18f, .16f)), gb + up * .02f, new Vector3(3f, .03f, 2.4f), rot);
                mk.Box(dark, gb - face * 2.1f + side * 1.4f + up * .45f, new Vector3(.8f, .9f, .45f), rot);
                mk.Box(GlowM(new Color(1f, .3f, .2f), 2f), gb - face * 1.86f + side * 1.4f + up * .75f, new Vector3(.08f, .08f, .02f), rot);
                var kit = gb - face * 1.6f - side * .9f;
                solid.Box(dark, gb - face * 2.1f + side * 1.4f + up * .45f, new Vector3(.8f, .9f, .45f), rot); solid.Box(dark, kit + up * .5f, new Vector3(1.6f, 1f, 1.2f), rot);
                mk.Prism(WorldMaterials.Solid(new Color(.85f, .85f, .88f)), kit + up * .05f - face * .25f, face, .38f, .5f, 12);
                mk.Prism(metal, kit + side * .6f, up, .02f, 1.1f, 5); mk.Prism(WorldMaterials.Solid(new Color(.8f, .7f, .3f), .9f), kit + side * .6f + up * 1.1f, up, .28f, .02f, 12);
                var bulb = gb + up * (gh - .3f);
                mk.Blob(GlowM(warm, 2.6f), bulb, Vector3.one * .12f, ctx.Rng, 0f);
                halos.Add(bulb + face * 1f, 2.2f, warm, .6f);
                ArtistBits.PointLight(ctx.Root, gb + up * 2f + face * 3f, warm, 9f, 1.8f);
                noGrass.Add(new Vector3(gb.x, 4f, gb.z));
            }
            else if (ctx.Chance(.35f))
                Car(ctx, mk, solid, ctx.OnGround(dMid.x, dMid.z) + up * .1f, face * (ctx.Chance(.5f) ? 1f : -1f), Color.Lerp(Paint[ctx.Rng.Next(Paint.Length)], new Color(.4f, .4f, .42f), .4f), false);
            // a tree in some back yards
            if (ctx.Chance(.45f))
            {
                var q = c - face * (D / 2f + ctx.Range(3f, 6f)) + side * ctx.Range(-W / 2f, W / 2f);
                if (ctx.IsLand(q.x, q.z, 3f) && !OnSubRoad(q.x, q.z, 1f) && !InRing(ctx, q, 2f))
                {
                    q = ctx.OnGround(q.x, q.z); var h = ctx.Range(5f, 8f);
                    mk.Prism(WorldMaterials.Solid(new Color(.22f, .18f, .16f)), q - up * .3f, up, .22f, h * .5f, 6);
                    mk.Blob(WorldMaterials.Solid(new Color(.18f, .25f, .18f)), q + up * h * .68f, new Vector3(2.2f, h * .42f, 2.2f), ctx.Rng, .25f);
                    ArtistBits.PostCollider(ctx.Root, q, h * .5f, .28f);
                }
            }
            noGrass.Add(new Vector3(c.x, Mathf.Max(W, D) * .6f + .5f, c.z));
            return true;
        }
    }

    public partial class GarageLife
    {
        public Material tv;

        /// The TVs in the suburbs' windows flicker, all a little differently in brightness and colour over time.
        void SuburbsUpdate(float t)
        {
            if (tv == null) return;
            var f = .7f + .25f * Mathf.PerlinNoise(t * 2.3f, 0f) + .15f * Mathf.Sin(t * 13f) * Mathf.PerlinNoise(t * .7f, 5f);
            var col = Color.Lerp(new Color(.45f, .65f, 1f), new Color(.8f, .9f, 1f), Mathf.PerlinNoise(t * .9f, 9f));
            tv.SetColor("_EmissionColor", col * f * 1.3f);
        }
    }
}
