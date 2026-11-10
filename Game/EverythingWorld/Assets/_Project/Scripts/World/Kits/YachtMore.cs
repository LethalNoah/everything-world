using UnityEngine;

namespace EverythingWorld.World
{
    /// The Riviera's deep pass (part of YachtKit): every artist's songs as things that glow up while they play.
    /// - Elton John: star-shaped sunglasses as a gateway over the yellow brick road (which glows gold for "Goodbye
    ///   Yellow Brick Road"), a pair of giant glittering platform boots ("Bennie And The Jets"), a checkered dance
    ///   floor under the crocodile ("Crocodile Rock"), the music box lights up for "Tiny Dancer", and a little plane
    ///   circles high overhead with its tail light blinking ("Daniel").
    /// - Jimmy Buffett: his own sandy beach: palms with a striped hammock, tiki torches, a giant flip-flop, a giant
    ///   salt shaker, a neon cheeseburger sign ("Cheeseburger In Paradise").
    /// - Steely Dan: a koi pond with an arched red bridge and paper lanterns ("Aja"), a brass saxophone.
    /// - Phil Collins: a drum riser under a rising moon, the kick drum and the moon glowing ("In the Air Tonight").
    /// - Everyone else a design from their song: Badfinger a big baby-blue heart; Bob Seger a bench under a crescent
    ///   moon; Gary Wright a giant dreamcatcher; Huey Lewis a glowing square frame; John Mellencamp a little ice
    ///   cream stand; King Harvest a dance floor under a big moon; Paul McCartney a searchlight on a wall; Redbone a
    ///   turning sunburst; Rick Springfield a jukebox; Tom Petty a guitar on a stool in a spotlight.
    public partial class YachtKit
    {
        public const string YellowBrickRoad = "4IRHwIZHzlHT1FQpRa5RdE", BennieJets = "5Wj1rJnCLpMHdLaxsFtJLs", CrocodileRock = "0Zqm1DYMWDQYfahtnC0Xvn",
            TinyDancer = "5fP7YHDNkIwh3F7zPPgEdB", DanielSong = "0SGm63cGOdpnrpDmLb2Fxv", Cheeseburger = "6VeZ970uI0Yi6sjBgyFBrp", Aja = "0w0Zs8pu4wx9iErRMEG96L",
            InTheAirTonight = "18AXbzPzBS8Y3AkgSxzJPb", BabyBlue = "6S3JlDAGk3uu3NtZbPnuhS", NightMoves = "6UBjSnyP1O5W5ndJoO9vUk", DreamWeaver = "74DrA5fFoGSy4xgkZarZtP",
            HipToBeSquare = "648BMGrt98kUbLo24A4vgj", JackDiane = "43btz2xjMKpcmjkuRsvxyg", Moonlight = "0q21FNwES2bbtcduB6kjEU", BandOnTheRun = "1H4idkmruFoJBg1DvUv2tY",
            ComeAndGetYourLove = "7GVUmCP00eSsqc4tzj1sDD", JessiesGirl = "1sgfjgLRCqSfndpofU8T2C", GentlyWeeps = "7HNFphOjDJpUGiseV5YvPZ";

        Material SongGlow(string song, Color c, float s = 1.6f)
        {
            var m = new Material(GlowM(c, s)) { name = "Song glow" }; life.songGlows.Add((song, m, c * (s * .65f))); return m;
        }

        static Quaternion Rot(Plot z) => Quaternion.LookRotation(z.front);

        void EltonMore(Plot z)
        {
            var up = Vector3.up; var ctx = z.ctx; var rot = Rot(z);
            // star-shaped sunglasses as a gateway over the road
            var g = z.At(10.5f, 0) + up * 4.6f; var frame = WorldMaterials.Solid(new Color(1f, .35f, .7f), .8f); var lens = GlowM(new Color(1f, .55f, .85f), .9f);
            foreach (var s in new[] { -1f, 1f })
            {
                var c = g + z.side * s * 1.9f;
                Vector3 P(int k) { var a = (90f + k * 36f) * Mathf.Deg2Rad; var r = k % 2 == 0 ? 1.6f : .72f; return c + (z.side * Mathf.Cos(a) + up * Mathf.Sin(a)) * r; }
                for (var k = 0; k < 10; k++) { var p0 = P(k); var p1 = P(k + 1); z.mk.Box(frame, (p0 + p1) / 2f, new Vector3(.16f, .16f, Vector3.Distance(p0, p1) + .1f), Quaternion.LookRotation(p1 - p0, z.front)); z.mk.Tri(lens, c, p0, p1, c - z.front); z.mk.Tri(lens, c, p1, p0, c + z.front); }
                var post = z.At(10.5f, s * 3.6f); z.mk.Prism(frame, post, up, .12f, 4.7f, 8); ArtistBits.PostCollider(z.root, post, 4.7f, .15f);
                z.mk.Box(frame, g + z.side * s * 3.55f, new Vector3(.4f, .16f, .16f), rot);
            }
            z.mk.Box(frame, g + up * .3f, new Vector3(.9f, .16f, .16f), rot * Quaternion.Euler(0, 0, 0));
            halos.Add(g, 4f, new Color(1f, .5f, .8f), .25f);
            // platform boots for Bennie
            var bt = z.At(-8f, -9f); var glitter = SongGlow(BennieJets, new Color(.55f, .75f, 1f), .7f);
            foreach (var s in new[] { -.7f, .7f })
            {
                var b = bt + z.side * s;
                z.mk.Box(glitter, b + up * .45f + z.front * .3f, new Vector3(.8f, .9f, 1.8f), rot); z.mk.Box(glitter, b + up * .7f - z.front * .55f, new Vector3(.8f, 1.4f, .5f), rot);
                z.mk.Box(glitter, b + up * 1.9f - z.front * .25f, new Vector3(.75f, 1.6f, .9f), rot);
            }
            z.solid.Box(dark, bt + up * 1.3f, new Vector3(2.4f, 2.6f, 2f), rot);
            halos.Add(bt + up * 1.5f, 2.6f, new Color(.55f, .75f, 1f), .3f);
            // a checkered dance floor under the crocodile
            var df = z.At(4f, 7f); var tileA = SongGlow(CrocodileRock, new Color(1f, .4f, .75f), .9f); var tileB = SongGlow(CrocodileRock, new Color(.3f, .9f, .85f), .9f);
            for (var i = 0; i < 5; i++) for (var j = 0; j < 7; j++) z.mk.Box((i + j) % 2 == 0 ? tileA : tileB, df + z.side * ((i - 2f) * 1f) + z.front * ((j - 3f) * 1f) + up * .15f, new Vector3(.96f, .06f, .96f), rot);
            // the music box lights for Tiny Dancer
            var mb = z.At(3f, -8f); z.mk.Box(SongGlow(TinyDancer, new Color(1f, .75f, .85f), 1.4f), mb + up * .6f + z.front * .61f, new Vector3(1.3f, .9f, .02f), rot);
            // Daniel's plane circles high overhead
            var pk = new MeshKit(); var silver = WorldMaterials.Solid(new Color(.85f, .86f, .9f), .8f);
            pk.Prism(silver, new Vector3(0, 0, -2.2f), Vector3.forward, .35f, 4.4f, 10, .5f);
            pk.Tri(silver, new Vector3(-3f, 0, 0), new Vector3(3f, 0, 0), new Vector3(0, 0, 1f), Vector3.up); pk.Tri(silver, new Vector3(-3f, 0, 0), new Vector3(0, 0, 1f), new Vector3(3f, 0, 0), Vector3.down);
            pk.Tri(silver, new Vector3(0, 0, -1.8f), new Vector3(0, 1.2f, -2.3f), new Vector3(0, 0, -1.2f), Vector3.right); pk.Tri(silver, new Vector3(0, 0, -1.8f), new Vector3(0, 0, -1.2f), new Vector3(0, 1.2f, -2.3f), Vector3.left);
            pk.Blob(SongGlow(DanielSong, new Color(1f, .2f, .15f), 3f), new Vector3(0, 1.2f, -2.3f), Vector3.one * .18f, ctx.Rng, 0f);
            foreach (var s in new[] { -1f, 1f }) pk.Blob(GlowM(s < 0 ? new Color(1f, .2f, .15f) : new Color(.2f, 1f, .4f), 3f), new Vector3(s * 3f, 0, 0), Vector3.one * .15f, ctx.Rng, 0f);
            var plane = new GameObject("Daniel's plane").transform; plane.SetParent(life.transform, false); pk.BuildSingle("Plane", plane, false);
            life.plane = plane; life.planeCenter = z.c + up * 45f;
            ArtistBits.PointLight(z.root, z.At(9f, 0) + up * 5f, new Color(1f, .6f, .85f), 14f, 1.2f);
        }

        void BuffettMore(Plot z)
        {
            var up = Vector3.up; var ctx = z.ctx; var rot = Rot(z);
            // palms and a striped hammock between two of them
            var h0 = z.At(-10f, -9f); var h1 = z.At(-10f, -2.5f); Palm(ctx, z.mk, h0, 7.5f); Palm(ctx, z.mk, h1, 8f); Palm(ctx, z.mk, z.At(-6f, 11f), 9f); Palm(ctx, z.mk, z.At(6f, 12f), 7f);
            Color[] stripe = { new Color(1f, .45f, .4f), new Color(.98f, .92f, .8f), new Color(.3f, .7f, .85f) };
            for (var k = 0; k < 12; k++)
            {
                var t0 = k / 12f; var t1 = (k + 1) / 12f; var sag = 1f;
                Vector3 H(float t) => Vector3.Lerp(h0, h1, t) + up * (1.9f - Mathf.Sin(t * Mathf.PI) * sag);
                z.mk.Box(WorldMaterials.Solid(stripe[k % 3]), (H(t0) + H(t1)) / 2f, new Vector3(1.1f, .05f, Vector3.Distance(H(t0), H(t1)) + .02f), Quaternion.LookRotation(H(t1) - H(t0)));
            }
            // tiki torches
            foreach (var p in new[] { z.At(6f, -11f), z.At(9f, -4f), z.At(9f, 4f), z.At(-2f, -12f) })
            {
                z.mk.Prism(palmBark, p, up, .08f, 2.2f, 6); z.mk.Prism(WorldMaterials.Solid(new Color(.3f, .22f, .15f)), p + up * 2.1f, up, .16f, .35f, 8, 1.2f);
                z.mk.Blob(GlowM(new Color(1f, .55f, .15f), 3.2f), p + up * 2.65f, new Vector3(.14f, .3f, .14f), ctx.Rng, 0f); halos.Add(p + up * 2.7f, 1f, new Color(1f, .55f, .2f), .8f);
                ArtistBits.PostCollider(z.root, p, 2.2f, .12f);
            }
            var fl = ArtistBits.PointLight(z.root, z.At(9f, 0) + up * 2.5f, new Color(1f, .6f, .3f), 12f, 1.2f); fl.gameObject.AddComponent<FireFlicker>().baseIntensity = 1.2f;
            // a giant flip-flop, a giant salt shaker
            var ff = z.At(-2f, 11f); var sole = WorldMaterials.Solid(new Color(.25f, .65f, .8f)); var fr = Quaternion.LookRotation(Quaternion.Euler(0, 30f, 0) * z.front);
            z.mk.Blob(sole, ff + up * .15f, new Vector3(1.1f, .15f, 2.4f), ctx.Rng, 0f);
            foreach (var s in new[] { -1f, 1f }) z.mk.Box(WorldMaterials.Solid(new Color(1f, .85f, .3f)), ff + up * .45f + fr * new Vector3(s * .5f, 0, .3f), new Vector3(.12f, .12f, 1.4f), fr * Quaternion.Euler(0, -s * 35f, 0));
            z.solid.Box(sole, ff + up * .3f, new Vector3(2f, .6f, 4.4f), fr);
            var ss = z.At(-6f, -12f); z.mk.Prism(WorldMaterials.Glow(new Color(.85f, .95f, 1f), .2f), ss, up, .7f, 2f, 12); z.mk.Prism(metal, ss + up * 2f, up, .72f, .6f, 12, .7f);
            for (var k = 0; k < 5; k++) z.mk.Blob(dark, ss + up * 2.62f + new Vector3(Mathf.Cos(k * 1.3f), 0, Mathf.Sin(k * 1.3f)) * .25f, Vector3.one * .05f, ctx.Rng, 0f);
            ArtistBits.PostCollider(z.root, ss, 2.6f, .75f);
            // the neon cheeseburger sign
            var sg = z.At(7f, 8.5f); z.mk.Prism(metal, sg, up, .08f, 3.2f, 6); ArtistBits.PostCollider(z.root, sg, 3.2f, .12f);
            var neon = SongGlow(Cheeseburger, new Color(1f, .75f, .25f), 2f); var bunN = SongGlow(Cheeseburger, new Color(1f, .45f, .2f), 2f);
            var sc = sg + up * 4f; z.mk.Box(navy, sc - z.front * .1f, new Vector3(3.4f, 1.8f, .1f), rot);
            z.mk.Box(bunN, sc + up * .35f, new Vector3(1.6f, .35f, .04f), rot); z.mk.Box(neon, sc, new Vector3(1.8f, .12f, .04f), rot); z.mk.Box(bunN, sc - up * .3f, new Vector3(1.6f, .25f, .04f), rot);
            ArtistBits.Label(z.labels, "CHEESEBURGER IN PARADISE", sc - up * .7f + z.front * .03f, z.front, .16f, new Color(1f, .9f, .6f), TextAnchor.MiddleCenter);
            halos.Add(sc + z.front * .4f, 2.4f, new Color(1f, .6f, .25f), .35f);
        }

        void SteelyMore(Plot z)
        {
            var up = Vector3.up; var ctx = z.ctx; var rot = Rot(z);
            var pc = z.At(4f, 5.5f); var pond = new Material(WorldMaterials.Glow(new Color(.08f, .25f, .3f), .15f)) { name = "Koi pond" }; pond.SetFloat("_Smoothness", .8f);
            for (var k = 0; k < 24; k++) { float a0 = k * Mathf.PI / 12f, a1 = (k + 1) * Mathf.PI / 12f; var w = pc + up * .06f; Vector3 E(float a) => z.side * Mathf.Cos(a) * 3.6f + z.front * Mathf.Sin(a) * 2.4f; z.mk.Tri(pond, w, w + E(a0), w + E(a1), w - up); }
            for (var k = 0; k < 12; k++) { var a = k * 30f * Mathf.Deg2Rad; z.mk.Blob(stone, pc + z.side * Mathf.Cos(a) * 3.9f + z.front * Mathf.Sin(a) * 2.7f + up * .15f, new Vector3(.45f, .25f, .4f), ctx.Rng, .3f); }
            for (var k = 0; k < 5; k++)
            {
                var kk = new MeshKit(); var col = k % 2 == 0 ? new Color(1f, .45f, .15f) : new Color(1f, .95f, .9f);
                kk.Blob(GlowM(col, .7f), Vector3.zero, new Vector3(.12f, .08f, .32f), ctx.Rng, 0f); kk.Tri(GlowM(col, .7f), new Vector3(0, 0, -.25f), new Vector3(.14f, 0, -.45f), new Vector3(-.14f, 0, -.45f), Vector3.up);
                var tr = kk.BuildSingle("Koi", life.transform, false).transform; life.koi.Add((tr, pc + up * .02f, 1.2f + k * .35f, k * 1.3f, z.side, z.front));
            }
            // an arched red bridge across the pond
            var red = WorldMaterials.Solid(new Color(.8f, .18f, .12f));
            for (var k = 0; k < 10; k++)
            {
                var t0 = k / 10f; var t1 = (k + 1) / 10f; Vector3 B(float t) => pc + z.front * Mathf.Lerp(-3.6f, 3.6f, t) + up * (.25f + Mathf.Sin(t * Mathf.PI) * .9f);
                z.mk.Box(teak, (B(t0) + B(t1)) / 2f, new Vector3(1.4f, .1f, Vector3.Distance(B(t0), B(t1)) + .02f), Quaternion.LookRotation(B(t1) - B(t0)));
                foreach (var s in new[] { -.7f, .7f }) z.mk.Box(red, (B(t0) + B(t1)) / 2f + z.side * s + up * .5f, new Vector3(.08f, .08f, Vector3.Distance(B(t0), B(t1)) + .02f), Quaternion.LookRotation(B(t1) - B(t0)));
                if (k % 3 == 0) foreach (var s in new[] { -.7f, .7f }) z.mk.Box(red, B(t0) + z.side * s + up * .25f, new Vector3(.1f, .55f, .1f), rot);
            }
            // paper lanterns on a line, a brass sax on a stand
            var lan = SongGlow(Aja, new Color(1f, .55f, .3f), 1.8f);
            foreach (var s in new[] { -1f, 1f }) { var p = z.At(4f, 5.5f + s * 5f); z.mk.Prism(red, p, up, .08f, 3f, 6); ArtistBits.PostCollider(z.root, p, 3f, .12f); }
            for (var k = 0; k < 7; k++) { var p = z.At(4f, .5f + k * 10f / 6f) + up * (2.8f - Mathf.Sin(k / 6f * Mathf.PI) * .4f); z.mk.Prism(lan, p - up * .25f, up, .2f, .45f, 10, .8f); halos.Add(p, .8f, new Color(1f, .55f, .3f), .6f); }
            var sx = z.At(-3f, 0); var brassM = WorldMaterials.Solid(new Color(.9f, .7f, .3f), .9f);
            z.mk.Prism(dark, sx, up, .03f, .6f, 6); z.mk.Prism(brassM, sx + up * .6f, (up + z.side * .15f).normalized, .1f, 1f, 10, 1.4f); z.mk.Prism(brassM, sx + up * .6f, (z.front - up * .2f).normalized, .14f, .35f, 10, 1.8f);
        }

        void PhilCollins(Plot z)
        {
            var up = Vector3.up; var ctx = z.ctx; var rot = Rot(z); var c = z.At(-2f, 0);
            z.mk.Box(dark, c + up * .3f, new Vector3(5f, .6f, 4f), rot); z.solid.Box(dark, c + up * .3f, new Vector3(5f, .6f, 4f), rot);
            var shell = WorldMaterials.Solid(new Color(.15f, .15f, .2f), .8f); var head = WorldMaterials.Solid(new Color(.95f, .95f, .92f)); var cym = WorldMaterials.Solid(new Color(.9f, .75f, .35f), .9f);
            var kick = c + up * 1.2f + z.front * .3f; z.mk.Prism(shell, kick - z.front * .4f, z.front, .6f, .8f, 16); z.mk.Prism(SongGlow(InTheAirTonight, new Color(.6f, .7f, 1f), 1.4f), kick + z.front * .41f, z.front, .55f, .02f, 16);
            foreach (var s in new[] { -.5f, .5f }) { z.mk.Prism(shell, c + up * 1.8f + z.side * s + z.front * .2f, up, .3f, .3f, 12); z.mk.Prism(head, c + up * 2.1f + z.side * s + z.front * .2f, up, .3f, .02f, 12); }
            z.mk.Prism(shell, c + up * 1.3f + z.side * 1.3f - z.front * .3f, up, .45f, .5f, 12); z.mk.Prism(head, c + up * 1.8f + z.side * 1.3f - z.front * .3f, up, .45f, .02f, 12);
            z.mk.Prism(shell, c + up * 1.3f - z.side * 1f + z.front * .6f, up, .33f, .2f, 12);
            foreach (var p in new[] { c - z.side * 1.8f + z.front * .3f, c + z.side * 1.9f + z.front * .6f, c - z.side * 1.5f - z.front * .8f })
            { z.mk.Prism(metal, p + up * .6f, up, .03f, 1.6f, 6); z.mk.Prism(cym, p + up * 2.2f, up, .5f, .02f, 16, .2f); }
            z.mk.Box(dark, c + up * .9f - z.front * 1.2f, new Vector3(.5f, .5f, .5f), rot);
            var gong = c - z.front * 2.6f + up * 3.2f; z.mk.Prism(cym, gong - z.front * .05f, z.front, 1.4f, .08f, 24);
            foreach (var s in new[] { -1f, 1f }) z.mk.Box(dark, gong + z.side * s * 1.7f - up * .5f, new Vector3(.12f, 4.6f, .12f), rot); z.mk.Box(dark, gong + up * 1.7f, new Vector3(3.6f, .12f, .12f), rot);
            var moon = c - z.front * 9f + up * 10f; z.mk.Prism(SongGlow(InTheAirTonight, new Color(.75f, .8f, 1f), 1.2f), moon, z.front, 2.4f, .1f, 28); halos.Add(moon, 5f, new Color(.7f, .8f, 1f), .25f);
            ArtistBits.PointLight(z.root, c + up * 4f + z.front * 3f, new Color(.6f, .7f, 1f), 12f, 1.4f);
        }

        /// The other artists, each a small design from their song (with its own song glow).
        bool SmallDesign(Plot z)
        {
            var up = Vector3.up; var ctx = z.ctx; var rot = Rot(z); var c = z.At(-2f, 0);
            switch (z.a.name)
            {
                case "Badfinger":
                {
                    var m = SongGlow(BabyBlue, new Color(.45f, .7f, 1f), 1.8f); var hc = c + up * 3.4f;
                    for (var k = 0; k < 40; k++) { float t0 = k / 40f * Mathf.PI * 2f, t1 = (k + 1) / 40f * Mathf.PI * 2f; Vector3 H(float t) => hc + (z.side * 16f * Mathf.Pow(Mathf.Sin(t), 3) + up * (13f * Mathf.Cos(t) - 5f * Mathf.Cos(2 * t) - 2f * Mathf.Cos(3 * t) - Mathf.Cos(4 * t))) * .1f; z.mk.Box(m, (H(t0) + H(t1)) / 2f, new Vector3(.18f, .18f, Vector3.Distance(H(t0), H(t1)) + .03f), Quaternion.LookRotation(H(t1) - H(t0), z.front)); }
                    z.mk.Prism(metal, c, up, .08f, 2f, 6); ArtistBits.PostCollider(z.root, c, 2f, .15f); halos.Add(hc, 2.6f, new Color(.45f, .7f, 1f), .35f); return true;
                }
                case "Bob Seger":
                {
                    z.mk.Box(teak, c + up * .45f, new Vector3(2f, .08f, .5f), rot); z.mk.Box(teak, c + up * .8f - z.front * .25f, new Vector3(2f, .5f, .06f), rot); foreach (var s in new[] { -.9f, .9f }) z.mk.Box(metal, c + z.side * s + up * .22f, new Vector3(.08f, .45f, .45f), rot); z.solid.Box(teak, c + up * .5f, new Vector3(2.1f, 1f, .6f), rot);
                    var mc = c - z.front * 3f + up * 6f; var m = SongGlow(NightMoves, new Color(1f, .92f, .7f), 1.6f);
                    for (var k = 0; k < 14; k++) { var a0 = (40f + k * 20f) * Mathf.Deg2Rad; var a1 = (40f + (k + 1) * 20f) * Mathf.Deg2Rad; Vector3 O(float a, float r, float off) => mc + (z.side * Mathf.Cos(a) * r + up * Mathf.Sin(a) * r) + z.side * off; z.mk.Tri(m, O(a0, 1.6f, 0), O(a1, 1.6f, 0), O(a1, 1.2f, .5f), mc - z.front); z.mk.Tri(m, O(a0, 1.6f, 0), O(a1, 1.2f, .5f), O(a0, 1.2f, .5f), mc - z.front); }
                    halos.Add(mc, 2.6f, new Color(1f, .9f, .7f), .3f); return true;
                }
                case "Gary Wright":
                {
                    var hc = c + up * 3.6f; var ring = WorldMaterials.Solid(new Color(.6f, .45f, .3f)); var m = SongGlow(DreamWeaver, new Color(.7f, .55f, 1f), 1.6f);
                    for (var k = 0; k < 24; k++) { var a0 = k * Mathf.PI / 12f; var a1 = (k + 1) * Mathf.PI / 12f; var p0 = hc + (z.side * Mathf.Cos(a0) + up * Mathf.Sin(a0)) * 1.8f; var p1 = hc + (z.side * Mathf.Cos(a1) + up * Mathf.Sin(a1)) * 1.8f; z.mk.Box(ring, (p0 + p1) / 2f, new Vector3(.14f, .14f, Vector3.Distance(p0, p1) + .02f), Quaternion.LookRotation(p1 - p0, z.front)); }
                    for (var k = 0; k < 8; k++) { var a = k * Mathf.PI / 4f; var p0 = hc + (z.side * Mathf.Cos(a) + up * Mathf.Sin(a)) * 1.75f; var p1 = hc + (z.side * Mathf.Cos(a + 2.4f) + up * Mathf.Sin(a + 2.4f)) * .6f; z.mk.Box(m, (p0 + p1) / 2f, new Vector3(.04f, .04f, Vector3.Distance(p0, p1)), Quaternion.LookRotation(p1 - p0, z.front)); }
                    for (var k = 0; k < 3; k++) { var p = hc + z.side * (k - 1f) * 1f - up * (2.2f + k % 2 * .4f); z.mk.Box(ring, p + up * .5f, new Vector3(.03f, 1f, .03f), rot); z.mk.Blob(WorldMaterials.Solid(new Color(.95f, .9f, .85f)), p, new Vector3(.12f, .3f, .05f), ctx.Rng, 0f); }
                    foreach (var s in new[] { -2.2f, 2.2f }) { z.mk.Prism(ring, c + z.side * s, up, .08f, 5.6f, 6); ArtistBits.PostCollider(z.root, c + z.side * s, 5.6f, .12f); } z.mk.Box(ring, hc + up * 2f, new Vector3(4.4f, .1f, .1f), rot);
                    halos.Add(hc, 2.6f, new Color(.7f, .55f, 1f), .3f); return true;
                }
                case "Huey Lewis & The News":
                {
                    var m = SongGlow(HipToBeSquare, new Color(1f, .8f, .3f), 1.8f); var cc = c + up * 2.5f; var rq = rot * Quaternion.Euler(20f, 30f, 0);
                    for (var e = 0; e < 12; e++) { var axis = e / 4; var i = e % 4; var a = (i & 1) == 0 ? -1f : 1f; var b = (i & 2) == 0 ? -1f : 1f; var off = axis == 0 ? new Vector3(0, a, b) : axis == 1 ? new Vector3(a, 0, b) : new Vector3(a, b, 0); var size = axis == 0 ? new Vector3(2.2f, .14f, .14f) : axis == 1 ? new Vector3(.14f, 2.2f, .14f) : new Vector3(.14f, .14f, 2.2f); z.mk.Box(m, cc + rq * off * 1.1f, size, rq); }
                    z.mk.Prism(stone, c - up * .2f, up, 1f, .8f, 8); ArtistBits.PostCollider(z.root, c, 4f, 1.2f); halos.Add(cc, 2.6f, new Color(1f, .8f, .3f), .3f); return true;
                }
                case "John Mellencamp":
                {
                    var stand = WorldMaterials.Solid(new Color(.95f, .95f, .92f)); var red = WorldMaterials.Solid(new Color(.85f, .2f, .2f));
                    z.mk.Box(stand, c + up * 1.4f, new Vector3(3.4f, 2.8f, 2.4f), rot); z.solid.Box(stand, c + up * 1.4f, new Vector3(3.4f, 2.8f, 2.4f), rot);
                    for (var k = 0; k < 6; k++) z.mk.Box(k % 2 == 0 ? red : stand, c + up * 3f + z.front * 1.5f + z.side * ((k - 2.5f) * .6f), new Vector3(.6f, .1f, 1f), rot * Quaternion.Euler(-15f, 0, 0));
                    z.mk.Box(SongGlow(JackDiane, new Color(1f, .85f, .55f), 1.6f), c + up * 1.6f + z.front * 1.21f, new Vector3(2f, .9f, .02f), rot);
                    var cone = c + up * 3.6f; z.mk.Cone(WorldMaterials.Solid(new Color(.85f, .65f, .35f)), cone + up * 1.2f, .5f, -1.2f, 8); z.mk.Blob(WorldMaterials.Solid(new Color(1f, .85f, .9f)), cone + up * 1.45f, Vector3.one * .55f, ctx.Rng, .1f);
                    return true;
                }
                case "King Harvest":
                {
                    var m = SongGlow(Moonlight, new Color(.9f, .92f, 1f), 1.2f);
                    for (var i = 0; i < 4; i++) for (var j = 0; j < 4; j++) z.mk.Box((i + j) % 2 == 0 ? m : teak, c + z.side * ((i - 1.5f) * 1.1f) + z.front * ((j - 1.5f) * 1.1f) + up * .16f, new Vector3(1.06f, .08f, 1.06f), rot);
                    var moon = c - z.front * 4f + up * 7f; z.mk.Prism(m, moon, z.front, 2.2f, .1f, 28); halos.Add(moon, 4f, new Color(.85f, .9f, 1f), .3f); return true;
                }
                case "Paul McCartney":
                {
                    var brick = WorldMaterials.Solid(new Color(.6f, .35f, .28f)); z.mk.Box(brick, c + up * 1.6f, new Vector3(6f, 3.2f, .6f), rot); z.solid.Box(brick, c + up * 1.6f, new Vector3(6f, 3.2f, .6f), rot);
                    var tw = c + z.side * 3.4f; z.mk.Box(brick, tw + up * 2.5f, new Vector3(1.4f, 5f, 1.4f), rot); z.solid.Box(brick, tw + up * 2.5f, new Vector3(1.4f, 5f, 1.4f), rot);
                    var head = new MeshKit(); head.Prism(metal, new Vector3(0, 0, -.5f), Vector3.forward, .5f, 1f, 14); head.Prism(SongGlow(BandOnTheRun, new Color(1f, .95f, .8f), 3f), new Vector3(0, 0, .51f), Vector3.forward, .45f, .02f, 14);
                    var t = head.BuildSingle("Searchlight", life.transform, false).transform; t.position = tw + up * 5.5f; life.searchlights.Add(t);
                    var sil = WorldMaterials.Solid(new Color(.1f, .1f, .12f)); for (var k = 0; k < 4; k++) { var p = c + z.front * .32f + z.side * ((k - 1.5f) * 1.1f); z.mk.Box(sil, p + up * 1f, new Vector3(.4f, 1.6f, .02f), rot); z.mk.Blob(sil, p + up * 2f, new Vector3(.18f, .2f, .02f), ctx.Rng, 0f); }
                    return true;
                }
                case "Redbone":
                {
                    var sk = new MeshKit(); Color[] rays = { new Color(1f, .5f, .2f), new Color(.3f, .8f, .85f), new Color(1f, .85f, .3f) };
                    for (var k = 0; k < 12; k++) { var a0 = k * 30f * Mathf.Deg2Rad; var a1 = (k + .5f) * 30f * Mathf.Deg2Rad; sk.Tri(GlowM(rays[k % 3], 1.2f), Vector3.zero, new Vector3(Mathf.Cos(a0), Mathf.Sin(a0), 0) * 2.4f, new Vector3(Mathf.Cos(a1), Mathf.Sin(a1), 0) * 2.4f, -Vector3.forward); sk.Tri(GlowM(rays[k % 3], 1.2f), Vector3.zero, new Vector3(Mathf.Cos(a1), Mathf.Sin(a1), 0) * 2.4f, new Vector3(Mathf.Cos(a0), Mathf.Sin(a0), 0) * 2.4f, Vector3.forward); }
                    sk.Prism(SongGlow(ComeAndGetYourLove, new Color(1f, .7f, .3f), 2f), new Vector3(0, 0, -.05f), Vector3.forward, .8f, .1f, 20);
                    var t = sk.BuildSingle("Sunburst", life.transform, false).transform; t.SetPositionAndRotation(c + up * 3.6f, rot); life.spinners.Add(t);
                    z.mk.Prism(metal, c, up, .1f, 3.6f, 6); ArtistBits.PostCollider(z.root, c, 3.4f, .15f); return true;
                }
                case "Rick Springfield":
                {
                    var box = WorldMaterials.Solid(new Color(.55f, .25f, .15f), .7f); var jb = c;
                    z.mk.Box(box, jb + up * .9f, new Vector3(1.6f, 1.8f, .9f), rot); z.mk.Prism(box, jb + up * 1.8f, z.front, .8f, .9f, 14); z.solid.Box(box, jb + up * 1.2f, new Vector3(1.7f, 2.6f, 1f), rot);
                    var m = SongGlow(JessiesGirl, new Color(1f, .4f, .7f), 1.8f); var m2 = SongGlow(JessiesGirl, new Color(.4f, .85f, 1f), 1.8f);
                    for (var k = 0; k < 9; k++) { var a = (k / 8f) * Mathf.PI; z.mk.Box(k % 2 == 0 ? m : m2, jb + up * 1.8f + z.front * .46f + (z.side * Mathf.Cos(a) + up * Mathf.Sin(a)) * .7f, new Vector3(.12f, .12f, .02f), rot); }
                    z.mk.Box(WorldMaterials.Solid(new Color(.95f, .9f, .7f)), jb + up * 1.1f + z.front * .46f, new Vector3(1f, .5f, .02f), rot); halos.Add(jb + up * 1.8f + z.front * .6f, 1.8f, new Color(1f, .5f, .8f), .3f); return true;
                }
                case "Tom Petty":
                {
                    z.mk.Prism(teak, c, up, .3f, .7f, 10); var gm = WorldMaterials.Solid(new Color(.7f, .45f, .25f), .7f);
                    z.mk.Blob(gm, c + up * 1.2f + z.front * .3f, new Vector3(.45f, .55f, .12f), ctx.Rng, 0f); z.mk.Box(dark, c + up * 1.9f + z.front * .25f, new Vector3(.1f, 1.1f, .06f), rot * Quaternion.Euler(-10f, 0, 0));
                    var tear = SongGlow(GentlyWeeps, new Color(.5f, .75f, 1f), 1.8f); for (var k = 0; k < 4; k++) z.mk.Blob(tear, c + up * (1.6f - k * .35f) + z.front * .5f + z.side * .2f, new Vector3(.06f, .1f, .06f), ctx.Rng, 0f);
                    var spot = c + up * 6f + z.front * 1f; z.mk.Prism(dark, spot, -up, .25f, .5f, 10); halos.Add(c + up * 1.3f, 2.2f, new Color(1f, .95f, .85f), .35f);
                    ArtistBits.PointLight(z.root, spot - up, new Color(1f, .95f, .85f), 8f, 2f); ArtistBits.PostCollider(z.root, c, 1.6f, .5f); return true;
                }
            }
            return false;
        }

        /// A sailboat's sails: a mainsail behind the mast and a jib in front.
        static void Sails(MeshKit mk)
        {
            var sail = WorldMaterials.Solid(new Color(.98f, .96f, .92f), .2f);
            foreach (var s in new[] { 1f, -1f }) { mk.Tri(sail, new Vector3(0, 2.5f, .7f), new Vector3(0, 12.5f, .75f), new Vector3(0, 2.5f, -3.4f), new Vector3(-s, 5f, -.5f)); mk.Tri(sail, new Vector3(0, 2f, 1f), new Vector3(0, 11.5f, .9f), new Vector3(0, 1.8f, 4.6f), new Vector3(-s, 5f, 2f)); }
        }
    }
}
