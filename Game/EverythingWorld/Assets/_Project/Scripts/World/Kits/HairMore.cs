using System.Collections.Generic;
using UnityEngine;

namespace EverythingWorld.World
{
    /// The Strip's deep pass (part of HairKit): silly fun, turned up.
    /// - Every venue has its own style: its own wall colour, a facade pattern (red brick, chrome panels, racing
    ///   stripes, painted flames, leopard spots, mirror tiles, a starfield) and its own awning (leopard, zebra,
    ///   candy stripes, black), so no two look alike.
    /// - Guns N' Roses, the biggest venue: red brick climbing with glowing roses, a giant neon guitar on the front
    ///   that blazes for "Sweet Child O' Mine", a glowing white door standing open on the roof for "Knockin' On
    ///   Heaven's Door", the piano stage for "November Rain".
    /// - Song glows for the other big ones: Aerosmith's footprints walking in ("Walk This Way") and a dream cloud
    ///   ("Dream On"); Mötley Crüe's "GIRLS GIRLS GIRLS" neon; Bon Jovi, Twisted Sister, Van Halen, Warrant, Quiet Riot
    ///   and Slash glow for their songs.
    /// - The desert either side is filled: a guitar-shaped swimming pool glowing turquoise, a "NO VACANCY" motel sign,
    ///   neon-outlined cacti, plus more of the mesas, cans, billboards and chrome guitars.
    public partial class HairKit
    {
        public const string SweetChild = "1u1DBObkLFscNCpRpXbYb9", HeavensDoor = "4JiEyzf0Md7KEFFGWDDdCr", MyMichelle = "6zVhuqfLLKKL29blllsegO", Anastasia = "7JJ22vCP45tMhZnAH4Sp1e",
            DreamOn = "5MxNLUsfh7uzROypsoO5qe", WalkThisWay = "5SZ6zX4rOrEQferfFC2MfP", MissAThing = "0c1gHntWjKD7QShC8s99sq", GirlsGirls = "549Go7a66CX6k523uK9kUv",
            DrFeelgood = "2cc9k15AifO7RfxxuEExkS", WannaRock = "70Z9t1qhytWtG4cCmmi7mU", NotGonnaTakeIt = "1hlveB9M6ijHZRbzZ2teyh", FeelTheNoize = "3vV3cr2TpPqFk07zxYUbla",
            Panama = "05RgAMGypEvqhNs5hPCbMS", CherryPieSong = "7LygtNjQ65PSdzVjUnHXQb";

        Material SongGlow(string song, Color c, float s = 1.6f)
        {
            var m = new Material(GlowM(c, s)) { name = "Song glow" }; life.songGlows.Add((song, m, c * (s * .65f))); return m;
        }

        struct Style { public Color wall, trim; public string facade, awning; }
        static Style StyleOf(string name)
        {
            switch (name)
            {
                case "Guns N' Roses": return new Style { wall = new Color(.42f, .14f, .12f), trim = new Color(.1f, .08f, .08f), facade = "brick", awning = "black" };
                case "Aerosmith": return new Style { wall = new Color(.3f, .32f, .38f), trim = new Color(.85f, .86f, .9f), facade = "chrome", awning = "stripes" };
                case "Mötley Crüe": return new Style { wall = new Color(.1f, .09f, .11f), trim = new Color(.8f, .15f, .12f), facade = "flames", awning = "black" };
                case "Twisted Sister": return new Style { wall = new Color(.85f, .3f, .6f), trim = new Color(.95f, .85f, .4f), facade = "leopard", awning = "zebra" };
                case "Bon Jovi": return new Style { wall = new Color(.22f, .32f, .52f), trim = new Color(.9f, .9f, .92f), facade = "stars", awning = "stripes" };
                case "Europe": return new Style { wall = new Color(.12f, .12f, .25f), trim = new Color(.5f, .7f, 1f), facade = "stars", awning = "black" };
                case "Quiet Riot": return new Style { wall = new Color(.38f, .38f, .4f), trim = new Color(.75f, .75f, .78f), facade = "chrome", awning = "zebra" };
                case "Slash": return new Style { wall = new Color(.16f, .12f, .1f), trim = new Color(.7f, .5f, .25f), facade = "brick", awning = "leopard" };
                case "Van Halen": return new Style { wall = new Color(.85f, .15f, .12f), trim = new Color(.95f, .95f, .95f), facade = "racing", awning = "black" };
                case "Warrant": return new Style { wall = new Color(.95f, .9f, .85f), trim = new Color(.8f, .1f, .2f), facade = "mirror", awning = "stripes" };
                default: return new Style { wall = new Color(.38f, .16f, .48f), trim = new Color(.3f, .9f, 1f), facade = "mirror", awning = "leopard" };
            }
        }

        Material AwningM(Style st) => st.awning == "zebra" ? WorldMaterials.Solid(new Color(.95f, .95f, .93f)) : st.awning == "stripes" ? WorldMaterials.Solid(st.trim) : st.awning == "black" ? black : leopard;

        /// The facade pattern and the awning's own markings, on the front wall above the opening and on the sides.
        void Facade(Spot z, Vector3 b, Style st, Quaternion rot, float W, float H, float D, float lintel, Vector3 aw)
        {
            var up = Vector3.up; var f = z.front * (D / 2f + .03f); var trim = WorldMaterials.Solid(st.trim, .7f); var ctx = z.ctx;
            switch (st.facade)
            {
                case "brick":
                    for (var y = lintel + .4f; y < H; y += .5f) z.mk.Box(trim, b + f + up * y, new Vector3(W, .05f, .02f), rot);
                    foreach (var s in new[] { -1f, 1f }) for (var y = .5f; y < H; y += .5f) z.mk.Box(trim, b + z.side * s * (W / 2f + .01f) + up * y, new Vector3(.02f, .05f, D), rot);
                    break;
                case "chrome":
                    for (var x = -W / 2f + .6f; x < W / 2f; x += 1.2f) z.mk.Box(chrome, b + f + z.side * x + up * (lintel + (H - lintel) / 2f), new Vector3(.12f, H - lintel - .2f, .03f), rot);
                    break;
                case "flames":
                    var flameM = GlowM(new Color(1f, .35f, .1f), 1.2f);
                    foreach (var s in new[] { -1f, 1f }) for (var k = 0; k < 7; k++) { var p = b + z.side * s * (W / 2f + .03f) + z.front * (D / 2f - .8f - k * 1.4f); var hgt = 1.2f + (k % 3) * .8f; z.mk.Tri(flameM, p, p - z.front * 1.2f, p - z.front * .5f + up * hgt, p - z.side * s); }
                    break;
                case "leopard":
                    for (var k = 0; k < 40; k++) { var x = (k * 1.618f) % (W - 1f) - (W - 1f) / 2f; var y = lintel + .4f + (k * .73f) % (H - lintel - .6f); z.mk.Blob(black, b + f + z.side * x + up * y, new Vector3(.25f, .18f, .02f), ctx.Rng, .4f); }
                    break;
                case "stars":
                    for (var k = 0; k < 24; k++) { var x = (k * 2.17f) % (W - 1f) - (W - 1f) / 2f; var y = lintel + .4f + (k * 1.31f) % (H - lintel - .6f); z.mk.Blob(GlowM(new Color(1f, .95f, .8f), 2.6f), b + f + z.side * x + up * y, Vector3.one * .08f, ctx.Rng, 0f); }
                    break;
                case "racing":
                    var white = WorldMaterials.Solid(new Color(.95f, .95f, .95f)); var blk = black;
                    for (var k = 0; k < 6; k++) { var x = -W / 2f + 1f + k * (W - 2f) / 5f; z.mk.Box(k % 2 == 0 ? white : blk, b + f + z.side * x + up * (lintel + (H - lintel) / 2f), new Vector3(.5f, H - lintel + 1f, .03f), rot * Quaternion.Euler(0, 0, 30f)); }
                    break;
                default: // mirror tiles
                    for (var x = -W / 2f + .5f; x < W / 2f - .3f; x += .9f) for (var y = lintel + .5f; y < H - .2f; y += .9f) z.mk.Box(((int)(x * 3 + y * 7)) % 3 == 0 ? GlowM(st.trim, .6f) : chrome, b + f + z.side * x + up * y, new Vector3(.8f, .8f, .02f), rot);
                    break;
            }
            // the awning's markings
            var arot = rot * Quaternion.Euler(-14f, 0, 0); var aup = arot * Vector3.up;
            if (st.awning == "zebra") for (var k = 0; k < 10; k++) z.mk.Box(black, aw + aup * .06f + z.side * ((k - 4.5f) * (W - 3f) / 10f), new Vector3(.35f, .02f, 3.3f), arot * Quaternion.Euler(0, 20f, 0));
            else if (st.awning == "stripes") for (var k = 0; k < 8; k++) z.mk.Box(WorldMaterials.Solid(new Color(.97f, .96f, .93f)), aw + aup * .06f + z.side * ((k - 3.5f) * (W - 2.5f) / 8f), new Vector3((W - 2f) / 16f, .02f, 3.38f), arot);
        }

        void GunsMore(Spot z)
        {
            var up = Vector3.up; var ctx = z.ctx; var rot = Quaternion.LookRotation(z.front); var D = 11f; var b = z.c - z.front * 2f;
            // roses climbing the brick
            var rose = SongGlow(MyMichelle, new Color(1f, .15f, .3f), 1.8f); var vine = GlowM(new Color(.25f, .7f, .35f), .7f);
            foreach (var s in new[] { -1f, 1f })
            {
                var x = z.side * s * (z.W / 2f - .8f);
                for (var y = .5f; y < z.H - .5f; y += .6f) { var p = b + z.front * (D / 2f + .05f) + x + up * y + z.side * Mathf.Sin(y * 1.7f) * .4f; z.mk.Box(vine, p, new Vector3(.08f, .65f, .04f), rot * Quaternion.Euler(0, 0, Mathf.Sin(y) * 30f)); if (((int)(y * 10)) % 3 == 0) z.mk.Blob(rose, p + z.front * .08f, new Vector3(.2f, .2f, .1f), ctx.Rng, .1f); }
            }
            // a giant neon guitar across the front, between the marquee and the roof sign
            var gc = b + z.front * (D / 2f + .15f) + up * (z.H - 2.31f);  var neon = SongGlow(SweetChild, new Color(1f, .78f, .25f), 2f);   // clear of the brick courses behind it
            void Line(Vector3 a, Vector3 c) => z.mk.Box(neon, (a + c) / 2f, new Vector3(.14f, .14f, Vector3.Distance(a, c) + .05f), Quaternion.LookRotation(c - a, z.front));
            Vector3 G(float x, float y) => gc + z.side * x + up * y;
            var body = new List<Vector3>(); for (var k = 0; k <= 20; k++) { var a = k * Mathf.PI * 2f / 20f; var r = 1f + .25f * Mathf.Cos(2f * a); body.Add(G(-3f + Mathf.Cos(a) * 1.3f * r, Mathf.Sin(a) * .9f * r)); }
            for (var k = 0; k < 20; k++) Line(body[k], body[k + 1]);
            Line(G(-1.7f, .15f), G(3.4f, .15f)); Line(G(-1.7f, -.15f), G(3.4f, -.15f)); Line(G(3.4f, .3f), G(4.4f, .45f)); Line(G(3.4f, -.3f), G(4.4f, -.1f)); Line(G(4.4f, .45f), G(4.4f, -.1f));
            halos.Add(gc + z.front * 1f, 4f, new Color(1f, .78f, .25f), .35f);
            // Heaven's door on the roof: a white door frame with the door standing open and light pouring out
            var dp = b + up * (z.H + .2f) - z.front * 2f + z.side * (z.W / 2f - 3f); var white = WorldMaterials.Solid(new Color(.95f, .95f, .95f)); var light = SongGlow(HeavensDoor, new Color(1f, .97f, .85f), 2.4f);
            foreach (var s in new[] { -.75f, .75f }) z.mk.Box(white, dp + z.side * s + up * 1.4f, new Vector3(.18f, 2.8f, .25f), rot);
            z.mk.Box(white, dp + up * 2.85f, new Vector3(1.7f, .18f, .25f), rot); z.mk.Box(light, dp + up * 1.35f, new Vector3(1.3f, 2.6f, .04f), rot);
            var door = Quaternion.AngleAxis(-70f, up) * rot; z.mk.Box(white, dp - z.side * .65f + up * 1.35f + door * Vector3.right * .65f, new Vector3(1.3f, 2.6f, .08f), door);
            for (var k = 0; k < 4; k++) z.mk.Box(white, dp + z.front * (.6f + k * .4f) + up * (-.15f - k * .2f) + up * .107f, new Vector3(1.6f, .12f, .4f), rot);
            halos.Add(dp + up * 1.4f + z.front * .6f, 3f, new Color(1f, .97f, .85f), .45f);
            // Slash's song glows the stage's spotlight
            ArtistBits.PointLight(z.root, b + z.front * (D / 2f + 2f) + up * 4f, new Color(1f, .25f, .35f), 18f, 1.6f);
        }

        void SongBits(Spot z)
        {
            var up = Vector3.up; var ctx = z.ctx; var rot = Quaternion.LookRotation(z.front); var D = 11f; var b = z.c - z.front * 2f;
            var front = b + z.front * (D / 2f); var sign = b + up * (z.H + 1.1f) + z.front * (D / 2f - .3f);
            switch (z.a.name)
            {
                case "Aerosmith":
                {
                    var steps = SongGlow(WalkThisWay, new Color(.4f, .9f, 1f), 1.6f);
                    for (var k = 0; k < 10; k++) { var p = ctx.OnGround(front.x + z.front.x * (9f - k * 1.1f) + z.side.x * ((k % 2) * .5f - .25f), front.z + z.front.z * (9f - k * 1.1f) + z.side.z * ((k % 2) * .5f - .25f)); z.mk.Blob(steps, p + up * .06f, new Vector3(.14f, .02f, .3f), ctx.Rng, 0f); }
                    var cloud = SongGlow(DreamOn, new Color(.8f, .85f, 1f), 1.1f); var cc = b + up * (z.H + 6f) - z.front * 3f;
                    for (var k = 0; k < 5; k++) z.mk.Blob(cloud, cc + z.side * ((k - 2f) * 1.3f) + up * Mathf.Sin(k * 1.3f) * .5f, new Vector3(1.3f, .8f, .8f), ctx.Rng, .2f);
                    break;
                }
                case "Mötley Crüe":
                {
                    var g = SongGlow(GirlsGirls, new Color(1f, .3f, .7f), 2f); var p = front + up * 3f + z.side * (z.W / 2f - 2.5f) + z.front * .15f;
                    z.mk.Box(black, p, new Vector3(3.6f, 1.2f, .1f), rot); z.mk.Box(g, p + z.front * .06f + up * .5f, new Vector3(3.4f, .06f, .02f), rot); z.mk.Box(g, p + z.front * .06f - up * .5f, new Vector3(3.4f, .06f, .02f), rot);
                    ArtistBits.Label(z.labels, "GIRLS GIRLS GIRLS", p + z.front * .07f, z.front, .26f, new Color(1f, .45f, .8f), TextAnchor.MiddleCenter);
                    break;
                }
                case "Twisted Sister": z.mk.Box(SongGlow(NotGonnaTakeIt, new Color(1f, .3f, .7f), 2f), sign + z.front * .2f - up * 1.05f, new Vector3(z.W - 2.4f, .08f, .03f), rot); break;
                case "Quiet Riot": z.mk.Box(SongGlow(FeelTheNoize, new Color(1f, .8f, .3f), 2f), sign + z.front * .2f - up * 1.05f, new Vector3(z.W - 2.4f, .08f, .03f), rot); break;
                case "Van Halen": z.mk.Box(SongGlow(Panama, new Color(1f, .9f, .3f), 2f), sign + z.front * .2f - up * 1.05f, new Vector3(z.W - 2.4f, .08f, .03f), rot); break;
                case "Warrant": z.mk.Box(SongGlow(CherryPieSong, new Color(1f, .2f, .3f), 2f), sign + z.front * .2f - up * 1.05f, new Vector3(z.W - 2.4f, .08f, .03f), rot); break;
                case "Slash": z.mk.Box(SongGlow(Anastasia, new Color(1f, .6f, .2f), 2f), sign + z.front * .2f - up * 1.05f, new Vector3(z.W - 2.4f, .08f, .03f), rot); break;
                case "Bon Jovi": z.mk.Box(SongGlow(Prayer, new Color(.4f, .7f, 1f), 2f), sign + z.front * .2f - up * 1.05f, new Vector3(z.W - 2.4f, .08f, .03f), rot); break;
            }
        }

        // ---------------------------------------------------------------- the desert's extras
        void DesertMore(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            int pools = 0, motels = 0, neonCacti = 0;
            for (var tries = 0; tries < 500 && (pools < 2 || motels < 2 || neonCacti < 10); tries++)
            {
                var px = ctx.Range(ctx.MinX, ctx.MaxX); var pz = ctx.Range(ctx.MinZ, ctx.MaxZ);
                if (!ctx.IsLand(px, pz, 10f) || OnStrip(px, pz, 4f) || InNoGrass(px, pz, 8f) || !ctx.IsFree(px, pz, 10f)) continue;
                var p = ctx.OnGround(px, pz);
                if (pools < 2) { GuitarPool(ctx, mk, solid, p); pools++; noGrass.Add(new Vector3(px, 12f, pz)); }
                else if (motels < 2) { Motel(ctx, mk, solid, p); motels++; noGrass.Add(new Vector3(px, 6f, pz)); }
                else { NeonCactus(ctx, mk, p, Neon[neonCacti % Neon.Length]); neonCacti++; noGrass.Add(new Vector3(px, 3f, pz)); }
            }
            Debug.Log($"The Hair Metal Strip: {pools} guitar pools, {motels} motel signs, {neonCacti} neon cacti");
        }

        /// A swimming pool shaped like a guitar body, glowing turquoise, with a chrome rim and deck chairs.
        void GuitarPool(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 c)
        {
            var up = Vector3.up; var face = new Vector3(streetA.x - c.x, 0, streetA.z - c.z).normalized; var side = Vector3.Cross(up, face);
            var water = GlowM(new Color(.25f, .85f, 1f), .9f); const int N = 28; var pts = new List<Vector3>();
            for (var k = 0; k < N; k++) { var a = k * Mathf.PI * 2f / N; var r = 1f + .28f * Mathf.Cos(2f * a); pts.Add(c + side * Mathf.Cos(a) * 4.2f * r + face * Mathf.Sin(a) * 3f * r); }
            for (var k = 0; k < N; k++) { var a = pts[k]; var b = pts[(k + 1) % N]; mk.Tri(water, c + up * .08f, a + up * .08f, b + up * .08f, c - up); mk.Box(chrome, (a + b) / 2f + up * .14f, new Vector3(.25f, .14f, Vector3.Distance(a, b) + .1f), Quaternion.LookRotation(b - a)); }
            var neck = c + side * 4.6f; mk.Box(WorldMaterials.Solid(new Color(.35f, .22f, .15f)), neck + side * 2.6f + up * .1f, new Vector3(5.6f, .2f, 1f), Quaternion.LookRotation(face)); mk.Box(black, neck + side * 5.8f + up * .12f, new Vector3(1.4f, .24f, 1.6f), Quaternion.LookRotation(face));
            foreach (var s in new[] { -1f, 1f }) { var dc = c - face * 4.6f + side * s * 2f; mk.Box(WorldMaterials.Solid(s < 0 ? Pink : Gold), dc + up * .4f, new Vector3(.7f, .06f, 1.6f), Quaternion.LookRotation(face) * Quaternion.Euler(-15f, 0, 0)); solid.Box(chrome, dc + up * .3f, new Vector3(.8f, .6f, 1.7f), Quaternion.LookRotation(face)); }
            halos.Add(c + up * .5f, 5f, new Color(.25f, .85f, 1f), .3f);
            ArtistBits.PointLight(ctx.Root, c + up * 3f, new Color(.3f, .85f, 1f), 12f, 1.2f);
        }

        /// A tall motel sign on a pole: an arrow of chasing bulbs and "NO VACANCY" in neon.
        void Motel(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 g)
        {
            var up = Vector3.up; var face = new Vector3(streetA.x - g.x, 0, streetA.z - g.z).normalized; var rot = Quaternion.LookRotation(face); var side = Vector3.Cross(up, face);
            mk.Prism(chrome, g - up * .2f, up, .25f, 9f, 8); ArtistBits.PostCollider(ctx.Root, g, 9f, .3f);
            mk.Box(WorldMaterials.Solid(new Color(.15f, .5f, .55f)), g + up * 9.5f, new Vector3(4.6f, 2.2f, .3f), rot);
            ArtistBits.Label(ctx.Root, "MOTEL", g + up * 9.8f + face * .17f, face, .5f, new Color(1f, .9f, .7f), TextAnchor.MiddleCenter);
            mk.Box(GlowM(Pink, 2.2f), g + up * 8f + face * .1f, new Vector3(3.4f, .6f, .1f), rot);
            ArtistBits.Label(ctx.Root, "NO VACANCY", g + up * 8f + face * .17f, face, .26f, new Color(.2f, .02f, .1f), TextAnchor.MiddleCenter);
            for (var k = 0; k < 7; k++) mk.Blob(GlowM(Gold, 2.6f), g + up * (10.9f - k * .1f) + side * (-2f + k * .6f) + face * .2f, Vector3.one * .12f, ctx.Rng, 0f);
            mk.Tri(GlowM(Gold, 2f), g + up * 11.3f + side * 2.5f + face * .2f, g + up * 10.5f + side * 2.5f + face * .2f, g + up * 10.9f + side * 3.1f + face * .2f, g - face);
            halos.Add(g + up * 9f + face, 3.5f, Pink, .45f);
        }

        /// A cactus outlined in neon tubes.
        void NeonCactus(LandContext ctx, MeshKit mk, Vector3 g, Color c)
        {
            var up = Vector3.up; var m = GlowM(c, 2.2f); var face = new Vector3(streetA.x - g.x, 0, streetA.z - g.z).normalized; var side = Vector3.Cross(up, face); var h = ctx.Range(3f, 4.5f);
            void L(Vector3 a, Vector3 b) => mk.Box(m, (a + b) / 2f, new Vector3(.1f, .1f, Vector3.Distance(a, b) + .05f), Quaternion.LookRotation(b - a, face));
            foreach (var s in new[] { -.3f, .3f }) L(g + side * s, g + side * s + up * h);
            L(g + side * -.3f + up * h, g + side * .3f + up * h);
            L(g + side * .3f + up * h * .45f, g + side * 1.1f + up * h * .45f); L(g + side * 1.1f + up * h * .45f, g + side * 1.1f + up * h * .75f);
            L(g - side * .3f + up * h * .6f, g - side * 1f + up * h * .6f); L(g - side * 1f + up * h * .6f, g - side * 1f + up * h * .85f);
            mk.Box(black, g + up * .15f, new Vector3(1.2f, .3f, .6f), Quaternion.LookRotation(face));
            halos.Add(g + up * h * .5f, 2.2f, c, .35f);
        }
    }
}
