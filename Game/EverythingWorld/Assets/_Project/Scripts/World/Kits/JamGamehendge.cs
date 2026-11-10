using System.Collections.Generic;
using UnityEngine;

namespace EverythingWorld.World
{
    /// Gamehendge, out in the woods, and the album trail.
    /// - Gamehendge: you step through a glowing Rhombus into a clearing. Icculus's mountain rises as a tall rock
    ///   spire, a long ladder climbing it to a little lit hut at the top; the Helping Friendly Book lies open on a
    ///   pedestal, its pages glowing; Wilson's dark tower stands across the clearing with one lit window and a
    ///   mockingbird on top; the Lizards sit round a fire. "The Divided Sky" wakes it all up.
    /// - The album trail: along the trails, things from the albums, each with a little sign: a horse hoisted in a
    ///   sling (Hoist), a bed floating in the air (Rift), a red farmhouse with a cow (Farmhouse), a big boat (Big
    ///   Boat), a glowing orb over a pool (Sigma Oasis), a giant halved nectarine (A Picture of Nectar), a deck
    ///   chair and a mower on a square of lawn (Lawn Boy), a floating ghost (The Story of the Ghost).
    public partial class JamKit
    {
        void Gamehendge(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            if (!FindSpot(ctx, 36f, 140f, 320f, out var c)) return;
            var up = Vector3.up; var toHeart = new Vector3(center.x - c.x, 0, center.z - c.z).normalized; var side = Vector3.Cross(up, toHeart);
            var stone = WorldMaterials.Solid(new Color(.5f, .48f, .5f)); var darkStone = WorldMaterials.Solid(new Color(.18f, .17f, .2f));
            // the Rhombus, at the edge of the clearing nearest the heart, the trail running through it
            var rh = c + toHeart * 30f; rh.y = ctx.Ground(rh.x, rh.z);
            var rhM = new Material(GlowM(new Color(.55f, .85f, 1f), 1.4f)) { name = "Rhombus" };
            Vector3[] corners = { up * 9f, side * 4f + up * 4.5f, Vector3.zero + up * .2f, -side * 4f + up * 4.5f };
            for (var k = 0; k < 4; k++) { var a = rh + corners[k]; var b = rh + corners[(k + 1) % 4]; mk.Box(rhM, (a + b) / 2f, new Vector3(.45f, .45f, Vector3.Distance(a, b) + .3f), Quaternion.LookRotation(b - a, toHeart)); }
            halos.Add(rh + up * 4.5f, 5f, new Color(.55f, .85f, 1f), .45f);
            foreach (var s in new[] { -1f, 1f }) ArtistBits.PostCollider(ctx.Root, rh + side * s * 4f, 5f, .3f);
            life.rhombus = rhM;
            Footpath(ctx, mk, rh + toHeart * 2f);
            var inPts = new List<Vector3>(); for (var d = -2f; d < 26f; d += 2.5f) { var q = rh - toHeart * d; inPts.Add(ctx.OnGround(q.x, q.z)); }
            Strip(mk, inPts, dirt, TrailW * .8f, .04f); foreach (var p in inPts) Remember(p, 901);
            // Icculus's mountain: a rock spire, a long ladder up it, a little hut with a lantern at the top
            var mt = c - toHeart * 12f + side * 10f; mt.y = ctx.Ground(mt.x, mt.z);
            for (var k = 0; k < 7; k++) mk.Blob(stone, mt + up * (k * 3.4f + 1.5f) + new Vector3(Mathf.Sin(k) * .8f, 0, Mathf.Cos(k * 1.3f) * .8f), new Vector3(5.5f - k * .6f, 2.4f, 5.2f - k * .55f), ctx.Rng, .25f);
            solid.Prism(stone, mt - up * .5f, up, 4.5f, 24f, 8, .4f);
            var lad = mt + toHeart * 4.6f; var ladTop = mt + toHeart * 1.6f + up * 24f;
            foreach (var s in new[] { -.5f, .5f }) { var a = lad + side * s; var b = ladTop + side * s; mk.Box(wood, (a + b) / 2f, new Vector3(.12f, .12f, Vector3.Distance(a, b)), Quaternion.LookRotation(b - a)); }
            for (var k = 1; k < 30; k++) { var p = Vector3.Lerp(lad, ladTop, k / 30f); mk.Box(wood, p, new Vector3(1.1f, .08f, .08f), Quaternion.LookRotation(toHeart)); }
            var hut = mt + up * 24.5f;
            mk.Box(wood, hut + up * 1f, new Vector3(2.4f, 2f, 2.4f), Quaternion.LookRotation(toHeart)); mk.Cone(WorldMaterials.Solid(new Color(.45f, .3f, .2f)), hut + up * 2f, 2f, 1.6f, 4, 45f);
            mk.Box(GlowM(Warm, 2.4f), hut + up * 1.1f + toHeart * 1.21f, new Vector3(.7f, .8f, .02f), Quaternion.LookRotation(toHeart));
            halos.Add(hut + up * 1.2f + toHeart * 1.6f, 2f, Warm, .8f); life.mountainTop = hut + up * 3f;
            Sign(ctx, mk, lad + toHeart * 2f + side * 2f, toHeart, "ICCULUS'S MOUNTAIN");
            ArtistBits.PointLight(ctx.Root, hut + up * 1.5f + toHeart * 2f, Warm, 12f, 1.6f);
            // the Helping Friendly Book, open on a pedestal
            var bk = c + toHeart * 6f; bk.y = ctx.Ground(bk.x, bk.z); var brot = Quaternion.LookRotation(toHeart);
            mk.Prism(stone, bk, up, .6f, 1.2f, 8, .7f); solid.Prism(stone, bk, up, .6f, 1.2f, 8);
            var book = new Material(GlowM(new Color(1f, .92f, .7f), 1.1f)) { name = "Helping Friendly Book" };
            foreach (var s in new[] { -1f, 1f }) { mk.Box(WorldMaterials.Solid(new Color(.45f, .15f, .12f)), bk + up * 1.25f + side * s * .55f, new Vector3(1.1f, .08f, 1.4f), brot * Quaternion.Euler(0, 0, s * 12f)); mk.Box(book, bk + up * 1.31f + side * s * .52f, new Vector3(1f, .04f, 1.3f), brot * Quaternion.Euler(0, 0, s * 12f)); }
            halos.Add(bk + up * 1.6f, 2.2f, new Color(1f, .9f, .6f), .6f);
            life.book = book; life.bookAt = bk + up * 1.4f;
            Sign(ctx, mk, bk + toHeart * 2f + side * 2.2f, toHeart, "THE HELPING FRIENDLY BOOK");
            // Wilson's tower, dark, one window lit, a mockingbird on top
            var wt = c - toHeart * 14f - side * 12f; wt.y = ctx.Ground(wt.x, wt.z);
            mk.Prism(darkStone, wt - up * .5f, up, 3f, 19f, 8, .85f); solid.Prism(darkStone, wt - up * .5f, up, 3f, 19f, 8);
            for (var k = 0; k < 8; k++) mk.Box(darkStone, wt + up * 18.9f + Quaternion.Euler(0, k * 45f, 0) * Vector3.forward * 2.4f, new Vector3(.8f, 1.2f, .5f), Quaternion.Euler(0, k * 45f, 0));
            var win = new Material(GlowM(new Color(1f, .4f, .2f), 1.6f)) { name = "Wilson's window" };
            mk.Box(win, wt + up * 14f + toHeart * 2.6f, new Vector3(.7f, 1.4f, .05f), Quaternion.LookRotation(toHeart));
            life.wilsonWindow = win;
            // his castle wall round the tower: a ring of dark crenellated wall with a gate facing the clearing and torches
            for (var k = 0; k < 20; k++)
            {
                var a0 = k * 18f * Mathf.Deg2Rad; var a1 = (k + 1) * 18f * Mathf.Deg2Rad; var gateAng = Mathf.Atan2(Vector3.Dot(toHeart, Vector3.forward), Vector3.Dot(toHeart, Vector3.right));
                var mid = (a0 + a1) / 2f; if (Mathf.Abs(Mathf.DeltaAngle(mid * Mathf.Rad2Deg, gateAng * Mathf.Rad2Deg)) < 12f) continue;
                var p0 = wt + new Vector3(Mathf.Cos(a0), 0, Mathf.Sin(a0)) * 8f; var p1 = wt + new Vector3(Mathf.Cos(a1), 0, Mathf.Sin(a1)) * 8f; p0.y = ctx.Ground(p0.x, p0.z); p1.y = ctx.Ground(p1.x, p1.z);
                var wm = (p0 + p1) / 2f; var wr = Quaternion.LookRotation(new Vector3(p1.x - p0.x, 0, p1.z - p0.z)); var wl = Vector3.Distance(new Vector3(p0.x, 0, p0.z), new Vector3(p1.x, 0, p1.z));
                mk.Box(darkStone, wm + up * 2f, new Vector3(1f, 5f, wl + .1f), wr); mk.Box(darkStone, wm + up * 4.9f, new Vector3(1f, .9f, wl * .45f), wr); solid.Box(darkStone, wm + up * 2f, new Vector3(1f, 5f, wl), wr);
            }
            var gate = wt + toHeart * 8f; gate.y = ctx.Ground(gate.x, gate.z);
            foreach (var s in new[] { -1f, 1f }) { var gp = gate + side * s * 2.4f; mk.Prism(darkStone, gp - up * .3f, up, 1f, 7f, 8); solid.Prism(darkStone, gp - up * .3f, up, 1f, 7f, 8); mk.Box(GlowM(new Color(1f, .5f, .15f), 2.6f), gp + up * 5f + toHeart * 1f, new Vector3(.3f, .5f, .3f), Quaternion.identity); halos.Add(gp + up * 5f + toHeart * 1.1f, 1.6f, new Color(1f, .5f, .15f), .8f); }
            Sign(ctx, mk, gate + toHeart * 3f + side * 4f, toHeart, "WILSON'S CASTLE");
            // the Lizards' village: round huts with pointed thatch, doorways lit, round a little square
            var vil = c + side * 17f + toHeart * 4f; vil.y = ctx.Ground(vil.x, vil.z); var thatch = WorldMaterials.Solid(new Color(.6f, .5f, .3f)); var mud = WorldMaterials.Solid(new Color(.55f, .45f, .35f));
            for (var k = 0; k < 5; k++)
            {
                var a = k * 72f * Mathf.Deg2Rad + .4f; var hp = vil + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * 6f; hp.y = ctx.Ground(hp.x, hp.z); var hf = (vil - hp); hf.y = 0; hf.Normalize();
                mk.Prism(mud, hp - up * .2f, up, 1.7f, 2.2f, 10); mk.Cone(thatch, hp + up * 2f, 2.2f, 2.4f, 10); solid.Prism(mud, hp - up * .2f, up, 1.7f, 2.4f, 8);
                mk.Box(GlowM(Warm, 1.8f), hp + up * .9f + hf * 1.66f, new Vector3(.8f, 1.4f, .04f), Quaternion.LookRotation(hf)); halos.Add(hp + up * 1f + hf * 2f, 1.4f, Warm, .6f);
            }
            ArtistBits.PointLight(ctx.Root, vil + up * 3f, Warm, 14f, 1.4f);
            Sign(ctx, mk, vil + toHeart * 8.5f, toHeart, "THE LIZARDS' VILLAGE");
            // the Famous Mockingbird, flying between the mountain and the Book (JamLife)
            var bird = WorldMaterials.Solid(new Color(.6f, .6f, .66f)); var bk2 = new MeshKit();
            bk2.Blob(bird, Vector3.zero, new Vector3(.3f, .3f, .55f), ctx.Rng, 0f); bk2.Blob(bird, new Vector3(0, .3f, .4f), Vector3.one * .18f, ctx.Rng, 0f);
            foreach (var s in new[] { -1f, 1f }) { bk2.Tri(bird, Vector3.zero, new Vector3(s * 1.4f, .6f, 0), new Vector3(s * .9f, 0, -.4f), Vector3.down); bk2.Tri(bird, Vector3.zero, new Vector3(s * .9f, 0, -.4f), new Vector3(s * 1.4f, .6f, 0), Vector3.up); }
            life.mockingbird = bk2.BuildSingle("The Famous Mockingbird", life.transform, false).transform;
            // the Lizards round a fire
            var fire = c - toHeart * 2f + side * -2f; fire.y = ctx.Ground(fire.x, fire.z);
            for (var k = 0; k < 6; k++) mk.Blob(stone, fire + Quaternion.Euler(0, k * 60f, 0) * Vector3.forward * .9f + up * .15f, new Vector3(.25f, .18f, .22f), ctx.Rng, .3f);
            var lizard = WorldMaterials.Solid(new Color(.35f, .6f, .3f), .5f);
            for (var k = 0; k < 5; k++)
            {
                var d = Quaternion.Euler(0, k * 72f + 15f, 0) * Vector3.forward; var lp = fire + d * 2.6f; lp.y = ctx.Ground(lp.x, lp.z);
                mk.Blob(lizard, lp + up * .45f, new Vector3(.35f, .3f, .7f), ctx.Rng, 0f);
                mk.Blob(lizard, lp + up * .9f - d * .3f, new Vector3(.25f, .35f, .25f), ctx.Rng, 0f);
                mk.Blob(lizard, lp + up * 1.25f - d * .45f, new Vector3(.2f, .18f, .28f), ctx.Rng, 0f);
                mk.Prism(lizard, lp + up * .35f + d * .6f, (d + up * .1f).normalized, .14f, 1.2f, 6, .2f);
            }
            var fl = ArtistBits.PointLight(ctx.Root, fire + up * 1f, new Color(1f, .55f, .2f), 12f, 1.8f); fl.gameObject.AddComponent<FireFlicker>().baseIntensity = 1.8f;
            ArtistBits.PostCollider(ctx.Root, fire, .5f, .9f); life.fires.Add(fire + up * .25f);
            ArtistBits.Label(ctx.Root, "GAMEHENDGE", rh + up * 10f, toHeart, .4f, new Color(.7f, .9f, 1f), TextAnchor.MiddleCenter);
            Sign(ctx, mk, rh + toHeart * 1.5f + side * 5.5f, toHeart, "THE RHOMBUS");
            Sign(ctx, mk, fire + toHeart * 3.6f + side * 1.5f, toHeart, "THE LIZARDS");
            // lanterns on posts round the clearing
            for (var k = 0; k < 10; k++) { var a = k * 36f * Mathf.Deg2Rad; var lp = c + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * 27f; if (!ctx.IsLand(lp.x, lp.z, 2f)) continue; lp.y = ctx.Ground(lp.x, lp.z); mk.Prism(wood, lp - up * .3f, up, .08f, 2.8f, 6); mk.Box(GlowM(new Color(.6f, .85f, 1f), 2.4f), lp + up * 2.7f, new Vector3(.3f, .4f, .3f), Quaternion.identity); halos.Add(lp + up * 2.7f, 1.4f, new Color(.6f, .85f, 1f), .7f); ArtistBits.PostCollider(ctx.Root, lp, 2.8f, .12f); }
            noGrass.Add(new Vector3(c.x, 34f, c.z));
            var mark = new GameObject("Shot: Gamehendge").transform; mark.SetParent(ctx.Root, false);
            var feet = rh + toHeart * 14f; feet.y = ctx.Ground(feet.x, feet.z); mark.SetPositionAndRotation(feet, Quaternion.LookRotation(-toHeart));
            life.gamehendge = c;
        }

        /// A little wooden sign naming a place in Gamehendge (a longer story text can go on it later).
        void Sign(LandContext ctx, MeshKit mk, Vector3 at, Vector3 face, string text)
        {
            at.y = ctx.Ground(at.x, at.z); var w = text.Length * .13f + .5f;
            mk.Prism(wood, at - Vector3.up * .3f, Vector3.up, .07f, 1.7f, 6); mk.Box(wood, at + Vector3.up * 1.4f, new Vector3(w, .5f, .08f), Quaternion.LookRotation(face));
            ArtistBits.Label(ctx.Root, text, at + Vector3.up * 1.4f + face * .05f, face, .15f, Ink, TextAnchor.MiddleCenter); ArtistBits.PostCollider(ctx.Root, at, 1.7f, .1f);
        }

        // ---------------------------------------------------------------- the album trail
        void AlbumTrail(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var pieces = new System.Action<Vector3, Vector3>[]
            {
                (at, f) => Hoist(ctx, mk, solid, at, f), (at, f) => Rift(ctx, mk, solid, at, f), (at, f) => Farmhouse(ctx, mk, solid, at, f), (at, f) => BigBoat(ctx, mk, solid, at, f),
                (at, f) => Oasis(ctx, mk, solid, at, f), (at, f) => Nectar(ctx, mk, solid, at, f), (at, f) => LawnBoy(ctx, mk, solid, at, f), (at, f) => Ghost(ctx, mk, solid, at, f),
            };
            string[] names = { "HOIST", "RIFT", "FARMHOUSE", "BIG BOAT", "SIGMA OASIS", "A PICTURE OF NECTAR", "LAWN BOY", "THE STORY OF THE GHOST" };
            var placed = new List<Vector3>();
            for (var i = 0; i < pieces.Length; i++)
            {
                Vector3 best = default; var found = false;
                for (var t = 0; t < 400 && !found; t++)
                {
                    float x = ctx.Range(ctx.MinX, ctx.MaxX), z = ctx.Range(ctx.MinZ, ctx.MaxZ);
                    if (!Open(ctx, x, z, 9f) || !NearPath(x, z, 18f)) continue;
                    var far = true; foreach (var p in placed) if (Vector2.Distance(new Vector2(x, z), new Vector2(p.x, p.z)) < 45f) far = false;
                    if (!far) continue;
                    best = ctx.OnGround(x, z); found = true;
                }
                if (!found) continue;
                placed.Add(best);
                // face the nearest trail point
                Vector3 near = best; var bd = float.MaxValue; foreach (var (p, _) in walkPoints) { var d = (p - best).sqrMagnitude; if (d < bd) { bd = d; near = p; } }
                var f = new Vector3(near.x - best.x, 0, near.z - best.z); f = f.sqrMagnitude > .1f ? f.normalized : Vector3.forward;
                pieces[i](best, f);
                var sign = best + f * 6f + Vector3.Cross(Vector3.up, f) * 3f; sign.y = ctx.Ground(sign.x, sign.z);
                mk.Prism(wood, sign - Vector3.up * .3f, Vector3.up, .07f, 1.6f, 6);
                mk.Box(wood, sign + Vector3.up * 1.3f, new Vector3(2.6f, .5f, .08f), Quaternion.LookRotation(f));
                ArtistBits.Label(ctx.Root, names[i], sign + Vector3.up * 1.3f + f * .05f, f, .14f, Ink, TextAnchor.MiddleCenter);
                ArtistBits.PointLight(ctx.Root, best + Vector3.up * 5f + f * 4f, Warm, 12f, 1.2f);
                noGrass.Add(new Vector3(best.x, 8f, best.z));
            }
        }

        void Hoist(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 at, Vector3 f)
        {
            var up = Vector3.up; var side = Vector3.Cross(up, f); var rot = Quaternion.LookRotation(f);
            foreach (var s in new[] { -1f, 1f }) { mk.Box(wood, at + side * s * 3f + up * 4f, new Vector3(.4f, 8f, .4f), rot); ArtistBits.PostCollider(ctx.Root, at + side * s * 3f, 8f, .3f); }
            mk.Box(wood, at + up * 8f, new Vector3(6.6f, .4f, .4f), rot);
            var horse = WorldMaterials.Solid(new Color(.55f, .35f, .2f)); var h = at + up * 3.6f;
            mk.Box(WorldMaterials.Solid(new Color(.75f, .65f, .45f)), (at + up * 8f + h + up * .9f) / 2f, new Vector3(.08f, 3.3f, .08f), rot);
            mk.Box(WorldMaterials.Solid(new Color(.8f, .75f, .6f)), h + up * .3f, new Vector3(1.4f, .3f, 2.6f), Quaternion.LookRotation(side));
            mk.Blob(horse, h, new Vector3(.75f, .7f, 1.4f), ctx.Rng, 0f);
            mk.Blob(horse, h + side * 1.6f + up * .5f, new Vector3(.35f, .45f, .7f), ctx.Rng, 0f);
            foreach (var a in new[] { -1f, 1f }) foreach (var b in new[] { -1f, 1f }) mk.Box(horse, h + side * a * .9f + f * b * .4f - up * 1f, new Vector3(.18f, 1.3f, .18f), Quaternion.LookRotation(f) * Quaternion.Euler(a * 15f, 0, 0));
        }

        void Rift(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 at, Vector3 f)
        {
            var up = Vector3.up; var rot = Quaternion.LookRotation(f) * Quaternion.Euler(0, 0, 8f); var b = at + up * 3.4f;
            mk.Box(wood, b, new Vector3(2.2f, .4f, 3f), rot);
            mk.Box(WorldMaterials.Solid(new Color(.35f, .45f, .75f)), b + up * .3f + f * .2f, new Vector3(2.15f, .25f, 2.4f), rot);
            mk.Box(white, b + up * .35f - f * 1.1f, new Vector3(1.4f, .25f, .6f), rot);
            mk.Box(wood, b + up * .6f - f * 1.45f, new Vector3(2.2f, 1.2f, .12f), rot);
            mk.Prism(GlowM(new Color(.6f, .7f, 1f), 1.6f), at + up * .05f, up, 2.2f, .05f, 24);
            halos.Add(b, 3f, new Color(.6f, .7f, 1f), .4f);
            life.floaters.Add(b);
        }

        void Farmhouse(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 at, Vector3 f)
        {
            var up = Vector3.up; var rot = Quaternion.LookRotation(f); var side = Vector3.Cross(up, f); var red = WorldMaterials.Solid(new Color(.7f, .15f, .12f));
            mk.Box(red, at + up * 2f, new Vector3(6f, 4f, 5f), rot); solid.Box(red, at + up * 2f, new Vector3(6f, 4f, 5f), rot);
            mk.GableRoof(WorldMaterials.Solid(new Color(.3f, .3f, .32f)), at + up * 4f, new Vector3(5.6f, 0, 6.6f), 2.2f, Quaternion.LookRotation(side));
            var fw = new Material(GlowM(Warm, 1.4f)) { name = "Farmhouse windows" }; life.farmWindows = fw;
            foreach (var s in new[] { -1.6f, 1.6f }) { mk.Box(fw, at + up * 2.4f + f * 2.51f + side * s, new Vector3(1f, 1.1f, .02f), rot); mk.Box(white, at + up * 2.4f + f * 2.52f + side * s, new Vector3(1.2f, .1f, .03f), rot); }
            var cow = at + f * 5f + side * 3f; var hide = white; var spot = dark;
            mk.Blob(hide, cow + up * 1.1f, new Vector3(.6f, .55f, 1.1f), ctx.Rng, 0f); mk.Blob(hide, cow + up * 1.3f + side * 1.1f, new Vector3(.3f, .32f, .4f), ctx.Rng, 0f);
            for (var k = 0; k < 4; k++) mk.Blob(spot, cow + up * (1.2f + Mathf.Sin(k) * .3f) + side * Mathf.Cos(k * 2f) * .5f + f * .55f, new Vector3(.2f, .18f, .05f), ctx.Rng, .2f);
            foreach (var a in new[] { -1f, 1f }) foreach (var b in new[] { -1f, 1f }) mk.Box(hide, cow + side * a * .6f + f * b * .3f + up * .35f, new Vector3(.14f, .7f, .14f), Quaternion.LookRotation(side));
        }

        void BigBoat(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 at, Vector3 f)
        {
            var up = Vector3.up; var side = Vector3.Cross(up, f); var rot = Quaternion.LookRotation(side); var hull = WorldMaterials.Solid(new Color(.85f, .82f, .75f)); var stripe = WorldMaterials.Solid(new Color(.75f, .15f, .15f));
            foreach (var s in new[] { -4f, 0f, 4f }) mk.Box(wood, at + side * s + up * .5f, new Vector3(2.8f, 1f, .4f), rot);
            mk.Box(hull, at + up * 2f, new Vector3(3.6f, 2f, 12f), rot); mk.Prism(hull, at + up * 2f + side * 6f, side, 1.8f, 2.4f, 4, .1f, 45f);
            mk.Box(stripe, at + up * 2.4f, new Vector3(3.62f, .3f, 12.02f), rot);
            mk.Box(white, at + up * 3.8f - side * 1.5f, new Vector3(2.6f, 1.8f, 4f), rot);
            for (var k = 0; k < 4; k++) mk.Box(GlowM(Warm, 1.4f), at + up * 3.9f - side * (3f - k * 1f) + f * 1.31f, new Vector3(.6f, .5f, .02f), rot);
            solid.Box(hull, at + up * 2f, new Vector3(3.6f, 4f, 13f), rot);
        }

        void Oasis(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 at, Vector3 f)
        {
            var up = Vector3.up;
            mk.Prism(WorldMaterials.Solid(new Color(.55f, .52f, .48f)), at - up * .1f, up, 4.2f, .4f, 20);
            mk.Prism(WorldMaterials.Glow(new Color(.2f, .55f, .7f), .3f), at + up * .28f, up, 3.7f, .04f, 20);
            var orb = at + up * 4f;
            mk.Blob(GlowM(new Color(.6f, .95f, 1f), 2.2f), orb, Vector3.one * 1.2f, ctx.Rng, 0f);
            for (var k = 0; k < 3; k++) mk.Prism(GlowM(new Color(.5f, .8f, 1f), 1.4f), orb - up * .05f, up, 1.8f + k * .7f, .06f, 24);
            halos.Add(orb, 4f, new Color(.5f, .9f, 1f), .5f);
            solid.Prism(dark, at, up, 4.2f, .6f, 12);
            life.floaters.Add(orb);
        }

        void Nectar(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 at, Vector3 f)
        {
            var up = Vector3.up; var rot = Quaternion.LookRotation(f); var c = at + up * 2.4f;
            mk.Blob(WorldMaterials.Solid(new Color(.95f, .45f, .2f), .5f), c - f * .6f, new Vector3(2.5f, 2.4f, 1.3f), ctx.Rng, 0f);
            mk.Prism(WorldMaterials.Solid(new Color(1f, .7f, .25f), .4f), c + f * .1f, f, 2.4f, .1f, 24);
            mk.Blob(WorldMaterials.Solid(new Color(.55f, .25f, .15f)), c + f * .25f, new Vector3(.7f, .9f, .3f), ctx.Rng, .2f);
            mk.Box(WorldMaterials.Solid(new Color(.3f, .55f, .25f)), c + up * 2.6f - f * .4f, new Vector3(.8f, .1f, .4f), rot * Quaternion.Euler(0, 0, 25f));
            solid.Box(dark, c, new Vector3(4.8f, 4.8f, 2f), rot);
        }

        void LawnBoy(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 at, Vector3 f)
        {
            var up = Vector3.up; var rot = Quaternion.LookRotation(f); var side = Vector3.Cross(up, f);
            mk.Box(WorldMaterials.Solid(new Color(.3f, .7f, .3f)), at + up * .05f, new Vector3(7f, .1f, 7f), rot);
            var ch = at + side * -1.2f; var stripe = new[] { new Color(.9f, .3f, .3f), new Color(.95f, .95f, .9f) };
            for (var k = 0; k < 5; k++) mk.Box(WorldMaterials.Solid(stripe[k % 2]), ch + up * .5f + f * ((k - 2f) * .14f), new Vector3(.7f, .04f, .14f), rot);
            for (var k = 0; k < 5; k++) mk.Box(WorldMaterials.Solid(stripe[k % 2]), ch + up * (.6f + k * .14f) - f * .4f, new Vector3(.7f, .14f, .04f), rot * Quaternion.Euler(-20f, 0, 0));
            mk.Box(metal, ch + up * .25f, new Vector3(.75f, .5f, .8f), rot);
            var mw = at + side * 1.6f; mk.Box(WorldMaterials.Solid(new Color(.75f, .2f, .15f)), mw + up * .35f, new Vector3(.9f, .4f, 1.1f), rot); mk.Box(metal, mw + up * .9f - f * .9f, new Vector3(.06f, 1f, .06f), rot * Quaternion.Euler(40f, 0, 0));
            foreach (var a in new[] { -.45f, .45f }) foreach (var b in new[] { -.45f, .45f }) mk.Prism(dark, mw + side * a + f * b + up * .15f, side * Mathf.Sign(a), .15f, .08f, 10);
            solid.Box(dark, at + up * .5f, new Vector3(4f, 1f, 1.4f), rot);
        }

        void Ghost(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 at, Vector3 f)
        {
            var up = Vector3.up; var g = at + up * 3f; var sheet = GlowM(new Color(.9f, .95f, 1f), .7f);
            mk.Blob(sheet, g + up * 1.4f, new Vector3(1f, 1.1f, .9f), ctx.Rng, 0f);
            mk.Prism(sheet, g - up * 1.2f, up, 1.2f, 2.6f, 10, .75f);
            for (var k = 0; k < 6; k++) mk.Cone(sheet, g - up * 1.2f + Quaternion.Euler(0, k * 60f, 0) * Vector3.forward * 1f, .3f, -.5f, 5);
            foreach (var s in new[] { -.3f, .3f }) mk.Blob(dark, g + up * 1.5f + f * .85f + Vector3.Cross(up, f) * s, new Vector3(.15f, .22f, .05f), ctx.Rng, 0f);
            halos.Add(g, 3f, new Color(.85f, .9f, 1f), .5f);
            life.floaters.Add(g);
        }
    }
}
