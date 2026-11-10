using System.Collections.Generic;
using UnityEngine;

namespace EverythingWorld.World
{
    /// The Grateful Dead's world, walked like a story: a rainbow loop trail runs round the district past a place for
    /// many of their songs, each with a little sign, among the iconography (the bears, the terrapins, the skull and
    /// lightning bolt, skeletons and roses). Round the Wall of Sound (PsychArtists) the outer ring holds:
    /// - Ship of Fools: a ship run aground on the sand, fools' caps on its crew of skeletons, a lantern at the bow;
    /// - Brokedown Palace: a crumbling palace front, columns fallen, a river of blue glass running past it;
    /// - Dire Wolf: a great wolf howling on a rock under a big pale moon;
    /// - Ripple: a perfectly still round pool whose surface rings outward when the song plays;
    /// - Uncle Sam: a tall skeleton on stilts in a starred top hat;
    /// - Stella Blue: a blue star hanging over a lone bench;
    /// - Samson and Delilah: two temple pillars, one cracked and leaning;
    /// - Row Jimmy: a rowboat on the Ripple pool;
    /// - West L.A. Fadeaway: a roadside motel sign whose neon fades in and out;
    /// - Friend of the Devil: a little red devil on a fence post at a crossroads.
    /// Little dancing bears and terrapins also turn up along the paths all over the Wilds (PsychKit).
    public partial class PsychKit
    {
        public const string Ripple = "1OE5l6sedVcIFELMuxQOPI", ShipOfFools = "5ks4ht7EDua6UsaI4Dk7Lz", Brokedown = "362CS15hE1upuTKoWApzLn", DireWolf = "6rLfU9QCwLUMRW5gGG7BWA",
            StellaBlue = "3yXTSJKglvmJYPYaQeyTSm", Samson = "1r4TpmnDEOEW7HCcCVoErG", RowJimmy = "1hAATBnR2nC9sCQXOcjTKa", Fadeaway = "4Jv7xJqocJSTV1kLSALWH6", FriendOfTheDevil = "6GgTJJmtDddHoo8Bp6lVcf",
            Althea = "7M7AwtGvWdMYudqx5Iuh1m", CrazyFingers = "0vDuBSnjSXgWeGybod3NIe", Slipknot = "0fuUmeE8StvPkyeUxTKqcp", ThreesAndSevens = "63wsuMhok6GgcBRd2strGk", IfIHadATail = "06hBdrgjUendZyH9U1WV22";

        void DeadSign(Plot z, Vector3 at, string text)
        {
            var face = new Vector3(z.c.x - at.x, 0, z.c.z - at.z).normalized; if (face.sqrMagnitude < .1f) face = z.front;
            at = z.ctx.OnGround(at.x, at.z); var w = text.Length * .13f + .5f; var up = Vector3.up;
            z.mk.Prism(wood, at - up * .3f, up, .07f, 1.8f, 6); z.mk.Box(dark, at + up * 1.45f, new Vector3(w, .5f, .08f), Quaternion.LookRotation(face));
            for (var k = 0; k < 6; k++) z.mk.Box(WorldMaterials.Solid(Rainbow[k]), at + up * (1.75f) + Vector3.Cross(up, face) * ((k - 2.5f) * w / 6f) - face * .01f, new Vector3(w / 6f, .1f, .1f), Quaternion.LookRotation(face));
            ArtistBits.Label(z.labels, text, at + up * 1.45f + face * .05f, face, .15f, Ink, TextAnchor.MiddleCenter); ArtistBits.PostCollider(z.root, at, 1.8f, .1f);
        }

        void PlaceMark(Plot z, string name, Vector3 at, float dist = 12f)
        {
            var toC = new Vector3(z.c.x - at.x, 0, z.c.z - at.z).normalized; var cam = at + toC * dist; cam = z.ctx.OnGround(cam.x, cam.z);
            var m = new GameObject("Shot: " + name).transform; m.SetParent(z.ctx.Root, false); m.SetPositionAndRotation(cam, Quaternion.LookRotation(-toC));
        }

        void DeadWorld(Plot z)
        {
            var up = Vector3.up; var ctx = z.ctx;
            Vector3 P(float fw, float rt) => z.At(fw, rt);
            // the loop trail: a ring round the district, painted in rainbow bands
            var loop = new List<Vector3>();
            for (var k = 0; k <= 48; k++) { var a = k * Mathf.PI * 2f / 48f; loop.Add(P(Mathf.Cos(a) * 34f, Mathf.Sin(a) * 34f)); }
            Strip(z.mk, loop, PathW * .8f, .05f, 2);
            for (var k = 0; k < 48; k += 4) { var lp = loop[k] + (loop[k] - z.c).normalized * 2.4f; lp = ctx.OnGround(lp.x, lp.z); LavaLamp(ctx, z.mk, lp, .38f, k / 4 % Rainbow.Length); }
            // Ship of Fools, aground on the sand
            {
                var c = P(-40f, 8f); var hullM = WorldMaterials.Solid(new Color(.45f, .3f, .2f)); var rot = Quaternion.LookRotation(z.side) * Quaternion.Euler(0, 0, 8f);
                var sk = new MeshKit();
                sk.Box(hullM, up * 1.2f, new Vector3(3.2f, 1.6f, 9f), Quaternion.identity); sk.Prism(hullM, up * 1.2f + Vector3.forward * 4.5f, Vector3.forward, 1.6f, 2.4f, 4, .1f, 45f);
                sk.Box(wood, up * 2.05f, new Vector3(3f, .1f, 8.6f), Quaternion.identity); sk.Prism(wood, up * 2f, up, .15f, 8f, 6);
                sk.Box(WorldMaterials.Solid(new Color(.95f, .9f, .8f)), up * 6f + Vector3.forward * .3f, new Vector3(.05f, 3.6f, 4.2f), Quaternion.identity);
                var bonesM = WorldMaterials.Solid(new Color(.93f, .92f, .86f));
                for (var k = 0; k < 3; k++)
                {
                    var bp = up * 2.1f + Vector3.forward * (-2.5f + k * 2.2f) + Vector3.right * ((k % 2) - .5f);
                    sk.Box(bonesM, bp + up * .6f, new Vector3(.12f, 1.1f, .12f), Quaternion.identity); sk.Blob(bonesM, bp + up * 1.35f, new Vector3(.2f, .24f, .22f), ctx.Rng, 0f);
                    for (var j = 0; j < 3; j++) sk.Cone(WorldMaterials.Solid(Rainbow[(k * 2 + j) % Rainbow.Length], .4f), bp + up * 1.5f, .12f, .45f, 6, j * 120f);
                }
                var lantern = new Material(GlowM(new Color(1f, .75f, .35f), 2.4f)) { name = "Ship's lantern" }; sk.Box(lantern, up * 3f + Vector3.forward * 5.6f, new Vector3(.4f, .5f, .4f), Quaternion.identity); life.shipLantern = lantern;
                var t = Mover(z, "Ship of Fools", sk, c, rot); life.ship = t;
                z.solid.Box(hullM, c + up * 1.4f, new Vector3(3.4f, 2.8f, 11f), rot);
                DeadSign(z, c + z.front * 6f + z.side * -3f, "SHIP OF FOOLS"); PlaceMark(z, "ship of fools", c, 16f);
            }
            // Brokedown Palace, crumbling, a river of blue glass going by
            {
                var c = P(-30f, -36f); var rot = Quaternion.LookRotation(z.front); var pale = WorldMaterials.Solid(new Color(.88f, .82f, .75f));
                z.mk.Box(pale, c + up * .3f, new Vector3(12f, .6f, 4f), rot); z.solid.Box(pale, c + up * .3f, new Vector3(12f, .6f, 4f), rot);
                for (var k = 0; k < 5; k++)
                {
                    var cp = c + z.side * ((k - 2f) * 2.6f) + up * .6f; var h = k == 1 ? 2.4f : k == 3 ? 4.2f : 6f;
                    z.mk.Prism(pale, cp, up, .45f, h, 10); z.solid.Prism(pale, cp, up, .45f, h, 8);
                    if (k == 1) z.mk.Prism(pale, cp + z.front * 2.5f + z.side * .5f + up * .1f, (z.side + z.front * .3f).normalized, .45f, 3.4f, 10);
                }
                z.mk.Box(pale, c + up * 6.9f - z.side * 2.6f, new Vector3(7.5f, .8f, 1.6f), rot); z.mk.Box(pale, c + up * 7.5f - z.side * 2.6f, new Vector3(3f, .6f, 1.6f), rot * Quaternion.Euler(0, 0, 8f));
                var win = new Material(GlowM(new Color(1f, .8f, .5f), 1f)) { name = "Palace windows" }; life.palace = win;
                z.mk.Box(win, c + up * 3f - z.front * 1.3f, new Vector3(9f, 4f, .05f), rot); z.mk.Box(pale, c + up * 3f - z.front * 1.5f, new Vector3(11f, 5f, .3f), rot);
                var river = ArtistBits.GlassBox(z.root, c + z.front * 5f + up * .1f, new Vector3(18f, .08f, 3f), new Color(.3f, .55f, .95f), rot);
                DeadSign(z, c + z.front * 8f + z.side * 6f, "BROKEDOWN PALACE"); PlaceMark(z, "brokedown palace", c, 16f);
            }
            // Dire Wolf, howling on a rock under a big pale moon
            {
                var c = P(24f, -38f); var rock = WorldMaterials.Solid(new Color(.55f, .45f, .5f)); var fur = WorldMaterials.Solid(new Color(.3f, .28f, .32f));
                z.mk.Blob(rock, c + up * 1.2f, new Vector3(3.2f, 1.8f, 2.8f), ctx.Rng, .25f); z.solid.Box(rock, c + up * 1.2f, new Vector3(5.6f, 2.6f, 5f), Quaternion.identity);
                var w = c + up * 2.8f; var f = (z.front - z.side).normalized;
                z.mk.Blob(fur, w + up * .9f, new Vector3(.7f, .7f, 1.5f), ctx.Rng, 0f);
                z.mk.Prism(fur, w + up * 1.2f + f * 1.2f, (f + up * 1.4f).normalized, .45f, 1.8f, 8, .7f);
                z.mk.Blob(fur, w + up * 2.8f + f * 1.9f, new Vector3(.4f, .45f, .55f), ctx.Rng, 0f); z.mk.Prism(fur, w + up * 3.1f + f * 2.2f, (f + up * 1.2f).normalized, .18f, .7f, 6, .4f);
                foreach (var s in new[] { -.15f, .15f }) z.mk.Cone(fur, w + up * 3.15f + f * 1.8f + Vector3.Cross(up, f) * s, .1f, .35f, 4);
                var eyesM = new Material(GlowM(new Color(1f, .85f, .3f), 1f)) { name = "Wolf eyes" }; life.wolfEyes = eyesM;
                foreach (var s in new[] { -.13f, .13f }) z.mk.Blob(eyesM, w + up * 2.95f + f * 2.25f + Vector3.Cross(up, f) * s, Vector3.one * .06f, ctx.Rng, 0f);
                foreach (var a in new[] { -1f, 1f }) foreach (var b in new[] { -.6f, .6f }) z.mk.Box(fur, w + Vector3.Cross(up, f) * a * .35f + f * b + up * .3f, new Vector3(.18f, .9f, .18f), Quaternion.LookRotation(f));
                z.mk.Prism(fur, w + up * .9f - f * 1.4f, (-f + up * .3f).normalized, .2f, 1.4f, 6, .3f);
                var moon = new Material(GlowM(new Color(.95f, .95f, 1f), 1.6f)) { name = "Wolf moon" }; life.wolfMoon = moon;
                z.mk.Blob(moon, c + up * 16f + f * 6f, Vector3.one * 2.6f, ctx.Rng, 0f); halos.Add(c + up * 16f + f * 6f, 7f, new Color(.9f, .95f, 1f), .45f);
                DeadSign(z, c + z.front * 5f + z.side * 4f, "DIRE WOLF"); PlaceMark(z, "dire wolf", c, 16f);
            }
            // Ripple: a still round pool, a rowboat on it (Row Jimmy)
            {
                var c = P(32f, 30f); var rim = WorldMaterials.Solid(new Color(.85f, .78f, .65f));
                z.mk.Prism(rim, c - up * .2f, up, 6.4f, .45f, 32); z.solid.Prism(rim, c - up * .3f, up, 6.4f, .9f, 16);
                z.mk.Prism(WorldMaterials.Solid(new Color(.1f, .3f, .55f), .97f), c + up * .2f, up, 5.8f, .1f, 32);
                var ringM = new Material(GlowM(new Color(.7f, .9f, 1f), .6f)) { name = "Ripples" }; life.rippleM = ringM;
                for (var k = 0; k < 4; k++)
                {
                    var rk = new MeshKit(); for (var s = 0; s < 32; s++) { float a0 = s * Mathf.PI / 16f, a1 = (s + 1) * Mathf.PI / 16f; var p0 = new Vector3(Mathf.Cos(a0), 0, Mathf.Sin(a0)); var p1 = new Vector3(Mathf.Cos(a1), 0, Mathf.Sin(a1)); rk.Box(ringM, (p0 + p1) / 2f, new Vector3(.03f, .01f, Vector3.Distance(p0, p1) + .01f), Quaternion.LookRotation(p1 - p0)); }
                    var t = rk.BuildSingle("Ripple ring", life.transform, false).transform; t.position = c + up * .32f; t.localScale = Vector3.one * (1f + k * 1.3f); life.ripples.Add(t);
                }
                life.ripplePool = c + up * .32f;
                var boat = new MeshKit(); var paint = WorldMaterials.Solid(new Color(.85f, .3f, .2f), .5f);
                boat.Box(paint, up * .25f, new Vector3(1.1f, .4f, 2.8f), Quaternion.identity); boat.Box(wood, up * .45f, new Vector3(1f, .06f, .3f), Quaternion.identity);
                foreach (var s in new[] { -1f, 1f }) boat.Box(wood, up * .5f + Vector3.right * s * .9f, new Vector3(1.6f, .05f, .12f), Quaternion.Euler(0, 0, s * -15f));
                life.rowboat = Mover(z, "Row Jimmy's boat", boat, c + up * .25f + z.side * 2.4f, Quaternion.LookRotation(z.front));
                DeadSign(z, c + z.front * 7.5f - z.side * 3f, "RIPPLE"); PlaceMark(z, "ripple", c, 12f);
                DeadSign(z, c + z.front * 7.5f + z.side * 3f, "ROW JIMMY");
            }
            // Uncle Sam: a tall skeleton on stilts in a starred top hat
            {
                var c = P(36f, 12f); var bonesM = WorldMaterials.Solid(new Color(.93f, .92f, .86f)); var blue = WorldMaterials.Solid(new Color(.15f, .25f, .6f)); var red = WorldMaterials.Solid(new Color(.8f, .12f, .15f));
                foreach (var s in new[] { -.4f, .4f }) { z.mk.Box(red, c + z.side * s + up * 2.5f, new Vector3(.25f, 5f, .25f), Quaternion.identity); for (var k = 0; k < 5; k++) z.mk.Box(white, c + z.side * s + up * (.5f + k * 1f), new Vector3(.27f, .3f, .27f), Quaternion.identity); }
                z.mk.Box(blue, c + up * 6.2f, new Vector3(1.2f, 2f, .6f), Quaternion.LookRotation(z.front));
                for (var k = 0; k < 4; k++) z.mk.Box(bonesM, c + up * (5.6f + k * .35f) + z.front * .32f, new Vector3(.8f, .06f, .05f), Quaternion.LookRotation(z.front));
                foreach (var s in new[] { -1f, 1f }) z.mk.Box(bonesM, c + up * 6.6f + z.side * s * .9f, new Vector3(.1f, 1.6f, .1f), Quaternion.Euler(0, 0, s * 25f) * Quaternion.LookRotation(z.front));
                z.mk.Blob(bonesM, c + up * 7.7f, new Vector3(.38f, .45f, .4f), ctx.Rng, 0f); foreach (var s in new[] { -.13f, .13f }) z.mk.Blob(dark, c + up * 7.75f + z.front * .35f + z.side * s, Vector3.one * .09f, ctx.Rng, 0f);
                z.mk.Prism(blue, c + up * 8.05f, up, .7f, .08f, 16); z.mk.Prism(white, c + up * 8.1f, up, .38f, 1.2f, 16);
                for (var k = 0; k < 3; k++) z.mk.Prism(red, c + up * (8.3f + k * .35f), up, .385f, .12f, 16);
                for (var k = 0; k < 5; k++) z.mk.Blob(white, c + up * 8.25f + Quaternion.Euler(0, k * 72f, 0) * z.front * .66f, Vector3.one * .06f, ctx.Rng, 0f);
                ArtistBits.PostCollider(z.root, c, 6f, .7f);
                DeadSign(z, c + z.front * 3f + z.side * 3f, "UNCLE SAM");
            }
            // Stella Blue: a blue star over a lone bench
            {
                var c = P(-8f, 40f); var star = new Material(GlowM(new Color(.35f, .55f, 1f), 1.6f)) { name = "Stella Blue" }; life.stella = star; var sp = c + up * 6f;
                for (var a = 0; a < 5; a++) z.mk.Box(star, sp + Quaternion.AngleAxis(a * 72f, z.front) * up * .7f, new Vector3(.45f, 1.4f, .2f), Quaternion.AngleAxis(a * 72f, z.front) * Quaternion.LookRotation(z.front));
                halos.Add(sp, 4f, new Color(.35f, .55f, 1f), .7f); z.mk.Box(dark, sp + up * 3f, new Vector3(.02f, 6f, .02f), Quaternion.identity);
                z.mk.Box(wood, c + up * .45f, new Vector3(2.2f, .1f, .55f), Quaternion.LookRotation(z.front)); z.mk.Box(wood, c + up * .8f - z.front * .25f, new Vector3(2.2f, .6f, .08f), Quaternion.LookRotation(z.front)); z.solid.Box(wood, c + up * .5f, new Vector3(2.2f, 1f, .6f), Quaternion.LookRotation(z.front));
                DeadSign(z, c + z.front * 3f + z.side * 3f, "STELLA BLUE"); PlaceMark(z, "stella blue", c, 12f);
            }
            // Samson and Delilah: two temple pillars, one cracked and leaning
            {
                var c = P(-36f, 30f); var stoneM = WorldMaterials.Solid(new Color(.85f, .75f, .6f)); var glow = new Material(GlowM(new Color(1f, .7f, .3f), .4f)) { name = "Samson's cracks" }; life.samson = glow;
                z.mk.Prism(stoneM, c - z.side * 2.5f, up, .8f, 9f, 12); z.solid.Prism(stoneM, c - z.side * 2.5f, up, .8f, 9f, 8);
                var lean = (up + z.side * .25f).normalized; z.mk.Prism(stoneM, c + z.side * 2.5f, lean, .8f, 4f, 12); z.mk.Prism(stoneM, c + z.side * 2.5f + lean * 4.1f, (lean + z.side * .15f).normalized, .78f, 4.6f, 12);
                z.mk.Box(glow, c + z.side * 2.5f + lean * 4.05f, new Vector3(1.7f, .08f, 1.7f), Quaternion.LookRotation(z.front)); z.solid.Prism(stoneM, c + z.side * 2.5f, up, .8f, 8f, 8);
                z.mk.Box(stoneM, c + up * 9.3f - z.side * 1.2f, new Vector3(4.2f, .8f, 1.8f), Quaternion.LookRotation(z.front) * Quaternion.Euler(0, 0, -6f));
                DeadSign(z, c + z.front * 4f, "SAMSON AND DELILAH"); PlaceMark(z, "samson", c, 12f);
            }
            // West L.A. Fadeaway: a roadside motel sign whose neon fades in and out
            {
                var c = P(8f, -40f); var neon = new Material(GlowM(new Color(1f, .3f, .55f), 1.6f)) { name = "Fadeaway neon" }; life.fadeaway = neon;
                z.mk.Prism(metal, c, up, .18f, 7f, 8); ArtistBits.PostCollider(z.root, c, 7f, .3f);
                z.mk.Box(WorldMaterials.Solid(new Color(.15f, .35f, .3f)), c + up * 7.8f, new Vector3(5f, 2f, .3f), Quaternion.LookRotation(z.front));
                z.mk.Box(neon, c + up * 7.8f + z.front * .17f, new Vector3(4.6f, .1f, .02f), Quaternion.LookRotation(z.front)); z.mk.Box(neon, c + up * 8.6f + z.front * .17f, new Vector3(4.6f, .1f, .02f), Quaternion.LookRotation(z.front));
                ArtistBits.Label(z.labels, "MOTEL", c + up * 8.15f + z.front * .18f, z.front, .5f, new Color(1f, .5f, .7f), TextAnchor.MiddleCenter);
                DeadSign(z, c + z.front * 3f + z.side * 3f, "WEST L.A. FADEAWAY"); PlaceMark(z, "fadeaway", c, 12f);
            }
            // Friend of the Devil: a little red devil on a fence post at a crossroads
            {
                var c = P(38f, -14f); var red = WorldMaterials.Solid(new Color(.8f, .12f, .1f), .5f);
                z.mk.Box(wood, c + up * 1.1f, new Vector3(.3f, 2.2f, .3f), Quaternion.identity); ArtistBits.PostCollider(z.root, c, 2.2f, .25f);
                foreach (var d in new[] { z.side, -z.side }) z.mk.Box(wood, c + up * 1.4f + d * 2f, new Vector3(.12f, .12f, 4f), Quaternion.LookRotation(d));
                var dv = c + up * 2.3f; z.mk.Blob(red, dv + up * .3f, new Vector3(.3f, .35f, .25f), ctx.Rng, 0f); z.mk.Blob(red, dv + up * .85f, Vector3.one * .22f, ctx.Rng, 0f);
                foreach (var s in new[] { -.12f, .12f }) z.mk.Cone(red, dv + up * 1f + z.side * s, .06f, .25f, 4);
                z.mk.Prism(red, dv + up * .2f - z.front * .2f, (-z.front + up * -.2f).normalized, .03f, .9f, 4);
                DeadSign(z, c + z.front * 2.5f - z.side * 3f, "FRIEND OF THE DEVIL");
            }
            // signs for the places the district already had
            DeadSign(z, P(-14f, 9f), "THE WALL OF SOUND"); DeadSign(z, P(10f, -18f), "THE DANCING BEARS"); DeadSign(z, P(1f, -26f), "STEAL YOUR FACE");
            DeadSign(z, P(20f, 9f), "TERRAPINS"); DeadSign(z, P(1f, 18f), "THE ROSE GARDEN"); DeadSign(z, P(-15f, 27f), "FRANKLIN'S TOWER");
            DeadSign(z, P(10f, 28f), "BOX OF RAIN"); DeadSign(z, P(-19f, -18f), "EYES OF THE WORLD"); DeadSign(z, P(14f, -18f), "SHAKEDOWN STREET");
            var mark = new GameObject("Shot: the Dead's world").transform; mark.SetParent(ctx.Root, false); var cam = P(30f, -20f); var look = z.c - cam; look.y = 0; mark.SetPositionAndRotation(cam, Quaternion.LookRotation(look.normalized));
        }
    }
}
