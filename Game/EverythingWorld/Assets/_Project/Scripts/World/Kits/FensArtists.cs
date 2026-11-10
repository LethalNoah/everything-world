using System.Collections.Generic;
using UnityEngine;

namespace EverythingWorld.World
{
    /// The Shoegaze Fens' artist islands. Each island has a glowing standing stone, the artist's covers on a
    /// dark board, a plaque by where the path arrives, a lamp, and a scene built from their songs:
    /// - Deafheaven: "Dream House", a black timber frame of a house filled solid with glowing pink-gold light; a
    ///   spiral stair climbing into the air to nowhere ("Vertigo"); a great cloud of coloured lights hanging
    ///   over it ("Great Mass of Color"); black spires round the island. Heavy and ethereal.
    /// - Mazzy Star: a little stone bridge over a pool ("Look On Down From The Bridge") overgrown with glowing
    ///   red roses ("Roseblood"), candles on it, a swing under a willow, and a tiny frosted jetty with a rowing
    ///   boat and a pale lamp ("Quiet, The Winter Harbor"). Dreamy and bittersweet.
    /// - Slowdive: a golden sun on a pole in a ring of standing mirrors; when "When the Sun Hits" plays it
    ///   blazes and the whole fen turns gold (FensLife).
    /// - my bloody valentine: a giant pink guitar leaning out of the moss, blurred, its strings glowing.
    /// - Cocteau Twins, M83, Alcest, Cigarettes After Sex, Fine, Knifeplay, Alvvays, Ethel Cain, ivri, Grimes,
    ///   Slow Pulp, Hue Hinton: their own small scenes from a song title; everyone else one of a set of
    ///   dreamy, noisy things (a stack of pedals, an amp with a lamp on it, a glowing microphone, a giant
    ///   cassette, a dream catcher, a lava lamp, a television of static, a record on a stand).
    public partial class FensKit
    {
        class Isle
        {
            public LandContext ctx; public Transform root, labels; public MeshKit mk, solid, pictures;
            public IslandWorldData.Artist a; public Vector3 c, front, side; public float R; public List<Renderer> glows; public ArtistArea area;
            public Vector3 At(float fw, float rt) { var p = c + front * fw + side * rt; return ctx.OnGround(p.x, p.z); }
        }

        static Material Ghost(Color c)
        {
            var m = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit")) { name = "Ghost" };
            m.SetColor("_BaseColor", c);
            m.SetFloat("_Surface", 1f); m.SetFloat("_Blend", 2f);
            m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha); m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
            m.SetFloat("_ZWrite", 0f); m.SetFloat("_Cull", 0f);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            return m;
        }

        public override void BuildArtist(LandContext ctx, IslandWorldData.Artist a)
        {
            if (stoneM == null) MakeMaterials();
            var root = new GameObject("Artist: " + a.name).transform; root.SetParent(ctx.Root, false);
            var front = facing.TryGetValue(a.name, out var f) ? f : new Vector3(heart.x - a.x, 0, heart.y - a.z).normalized;
            if (front.sqrMagnitude < .01f) front = Vector3.forward;
            var labels = new GameObject("Labels").transform; labels.SetParent(root, false);
            var area = root.gameObject.AddComponent<ArtistArea>();
            area.artistName = a.name; area.stationId = a.stationId; area.radius = a.r; area.glowColor = HotPink;
            var z = new Isle { ctx = ctx, root = root, labels = labels, mk = new MeshKit(), solid = new MeshKit(), pictures = new MeshKit(), a = a, c = ctx.OnGround(a.x, a.z), front = front, side = Vector3.Cross(Vector3.up, front), R = IslandR(a), glows = new List<Renderer>(), area = area };
            switch (a.name)
            {
                case "Deafheaven": DreamHouse(z); break;
                case "Mazzy Star": Bridge(z); break;
                case "Slowdive": SunMirrors(z); break;
                case "my bloody valentine": PinkGuitar(z); break;
                case "Cocteau Twins": NeonHeart(z); break;
                case "M83": Synth(z); break;
                case "Alcest": SpiritTree(z); break;
                case "Cigarettes After Sex": Monochrome(z); break;
                case "Fine": Ladder(z); break;
                case "Knifeplay": Tears(z); break;
                case "Alvvays": FlowerArch(z); break;
                case "Ethel Cain": PorchSwing(z); break;
                case "ivri": AmpStack(z); break;
                case "Grimes": Arrows(z); break;
                case "Slow Pulp": Balloons(z); break;
                case "Hue Hinton": Crash(z); break;
                default: Generic(z); break;
            }
            // the island's own things: glowing stone, covers, plaque, lamp
            GlowStone(z, z.At(-z.R * .55f, -z.R * .5f));
            Board(z, z.At(z.R - 6.5f, -4.2f), front);
            ArtistBits.Plaque(z.mk, z.solid, labels, a, z.At(z.R - 4f, 3.2f), front, wood, black, Ink, 1.5f);
            z.glows.Add(Lamp(ctx, z.mk, root, z.At(z.R - 2.5f, -2.4f), Glows[Mathf.Abs(a.name.GetHashCode()) % Glows.Length]));
            if (z.area.glowLight == null) z.area.glowLight = ArtistBits.PointLight(root, z.c + Vector3.up * 3f, HotPink, HasStation(a) ? 18f : 11f, HasStation(a) ? 1.4f : 1f);
            area.glowRenderers = z.glows.ToArray();
            z.mk.Build("Area", root, false);
            z.solid.BuildCollider("Walls", root);
            z.pictures.Build("Covers", root, false);
        }

        /// The standing stone every island has, a slit of light carved down its face.
        void GlowStone(Isle z, Vector3 g)
        {
            var face = (z.c - g); face.y = 0; face = face.sqrMagnitude > .01f ? face.normalized : z.front;
            var rot = Quaternion.LookRotation(face) * Quaternion.Euler(0, 0, z.ctx.Range(-4f, 4f));
            var h = z.ctx.Range(2.2f, 3f);
            z.mk.Box(stoneDark, g + Vector3.up * (h / 2f - .2f), new Vector3(1f, h, .5f), rot);
            var col = Glows[Mathf.Abs(z.a.name.GetHashCode() / 7) % Glows.Length];
            z.mk.Box(GlowM(col, 3f), g + Vector3.up * (h / 2f) + face * .26f, new Vector3(.08f, h * .7f, .02f), rot);
            halos.Add(g + Vector3.up * (h / 2f) + face * 1.2f, 1.4f, col, .9f);
            z.solid.Box(stoneDark, g + Vector3.up * (h / 2f), new Vector3(1f, h, .5f), rot);
        }

        void Board(Isle z, Vector3 ground, Vector3 dir)
        {
            dir = Vector3.ProjectOnPlane(dir, Vector3.up).normalized;
            var rot = Quaternion.LookRotation(dir); var side = Vector3.Cross(Vector3.up, dir);
            var n = Mathf.Max(1, Mathf.Min(3, z.a.songList?.Length ?? 1)); const float size = .8f;
            var w = n * size * 1.25f + .3f;
            foreach (var s in new[] { -1f, 1f }) z.mk.Prism(iron, ground + side * s * (w / 2f - .1f) - dir * .05f, Vector3.up, .05f, 2.1f, 6);
            var c = ground + Vector3.up * 1.6f;
            z.mk.Box(black, c - Vector3.up * .1f, new Vector3(w, size + .55f, .08f), rot);
            z.solid.Box(black, c - Vector3.up * .1f, new Vector3(w, size + .55f, .16f), rot);
            ArtistBits.CoverWall(z.pictures, z.labels, z.ctx, z.a, c + dir * .045f + Vector3.up * .08f, dir, n, size, n, true, Ink);
        }

        // ---------------------------------------------------------------- Deafheaven
        void DreamHouse(Isle z)
        {
            var ctx = z.ctx; var up = Vector3.up; var rot = Quaternion.LookRotation(z.front);
            var hc = z.At(-3f, 0); const float W = 7f, D = 6f, H = 4f, Roof = 2.6f;
            var light = new Color(1f, .58f, .48f);
            var fill = GlowM(light, 2.2f);
            // the house of light
            z.mk.Box(fill, hc + up * (H / 2f), new Vector3(W - .3f, H - .1f, D - .3f), rot);
            foreach (var s in new[] { -1f, 1f })
            {
                var eaveL = hc + up * H + z.side * s * (W / 2f - .15f);
                var ridge = hc + up * (H + Roof);
                foreach (var e in new[] { -1f, 1f })
                    z.mk.Quad(fill, eaveL + z.front * e * (D / 2f - .15f), ridge + z.front * e * (D / 2f - .15f), ridge - z.front * e * (D / 2f - .15f), eaveL - z.front * e * (D / 2f - .15f), hc + up * H);
            }
            foreach (var e in new[] { -1f, 1f })
                z.mk.Tri(fill, hc + up * H + z.side * (W / 2f - .15f) + z.front * e * (D / 2f - .15f), hc + up * H - z.side * (W / 2f - .15f) + z.front * e * (D / 2f - .15f), hc + up * (H + Roof) + z.front * e * (D / 2f - .15f), hc + up * H);
            // the black frame round it
            void Beam(Vector3 a, Vector3 b) { var d = b - a; z.mk.Box(black, (a + b) / 2f, new Vector3(.22f, .22f, d.magnitude + .22f), Quaternion.LookRotation(d)); }
            Vector3 Corner(float sx, float sz, float y) => hc + z.side * sx * (W / 2f) + z.front * sz * (D / 2f) + up * y;
            foreach (var (sx, sz) in new[] { (-1f, -1f), (1f, -1f), (1f, 1f), (-1f, 1f) }) Beam(Corner(sx, sz, 0), Corner(sx, sz, H));
            foreach (var y in new[] { 0f, H })
            {
                Beam(Corner(-1, -1, y), Corner(1, -1, y)); Beam(Corner(1, -1, y), Corner(1, 1, y));
                Beam(Corner(1, 1, y), Corner(-1, 1, y)); Beam(Corner(-1, 1, y), Corner(-1, -1, y));
            }
            foreach (var sz in new[] { -1f, 1f })
            {
                var ridge = hc + up * (H + Roof) + z.front * sz * (D / 2f);
                Beam(Corner(-1, sz, H), ridge); Beam(Corner(1, sz, H), ridge);
            }
            Beam(hc + up * (H + Roof) - z.front * (D / 2f), hc + up * (H + Roof) + z.front * (D / 2f));
            z.solid.Box(black, hc + up * (H / 2f), new Vector3(W, H, D), rot);
            foreach (var k in new[] { -1f, 1f }) halos.Add(hc + up * 2.5f + z.front * k * (D / 2f + 2.4f), 3.4f, light, .9f);
            halos.Add(hc + up * (H + Roof + 2.5f), 3.5f, light, .8f);
            z.area.glowLight = ArtistBits.PointLight(z.root, hc + up * 2f + z.front * (D / 2f + 2f), light, 20f, 2f);

            // Vertigo: a spiral stair climbing to a little lit platform, nine metres up
            var foot = z.At(3.5f, 7.5f);
            const int steps = 27; const float rise = .32f, rad = 1.7f;
            z.mk.Prism(black, foot, up, .18f, steps * rise + 1.4f, 8);
            ArtistBits.PostCollider(z.root, foot, steps * rise, .2f);
            for (var k = 0; k < steps; k++)
            {
                var a = k * 20f; var d = Quaternion.Euler(0, a, 0) * z.front;
                var p = foot + d * rad * .55f + up * (k * rise + rise);
                var r = Quaternion.LookRotation(Vector3.Cross(up, d));
                z.mk.Box(black, p, new Vector3(1.9f, .12f, .7f), Quaternion.LookRotation(Vector3.Cross(up, d)) * Quaternion.Euler(0, 90f, 0));
                z.solid.Box(black, p - up * .1f, new Vector3(1.9f, .3f, .7f), Quaternion.LookRotation(Vector3.Cross(up, d)) * Quaternion.Euler(0, 90f, 0));
                var rail = foot + d * (rad + .05f) + up * (k * rise + rise + .95f);
                z.mk.Box(iron, rail - up * .45f, new Vector3(.04f, .9f, .04f), Quaternion.identity);
                z.solid.Box(iron, rail - up * .3f, new Vector3(.1f, 1.2f, .6f), r);
            }
            var platY = steps * rise + rise;
            z.mk.Prism(black, foot + up * platY, up, rad + .4f, .14f, 16);
            z.mk.Prism(black, foot + up * (platY + .14f), -up, rad + .4f, .14f, 16);          // so it has an underside
            z.solid.Prism(black, foot + up * (platY - .1f), up, rad + .4f, .3f, 12);
            for (var k = 0; k < 24; k++)
            {
                var a0 = Quaternion.Euler(0, k * 15f, 0) * z.front * (rad + .35f); var a1 = Quaternion.Euler(0, (k + 1) * 15f, 0) * z.front * (rad + .35f);
                var p0 = foot + up * (platY + .16f) + a0; var p1 = foot + up * (platY + .16f) + a1;
                z.mk.Box(GlowM(HotPink, 3f), (p0 + p1) / 2f, new Vector3(.12f, .04f, Vector3.Distance(p0, p1) + .02f), Quaternion.LookRotation(p1 - p0));
            }
            halos.Add(foot + up * (platY + 1.2f), 2.4f, HotPink, .9f);
            for (var k = 0; k < 16; k++)
            {
                if (k == 13 || k == 14) continue;     // where the stair arrives
                var a = k * 22.5f; var d = Quaternion.Euler(0, a, 0) * z.front; var d2 = Quaternion.Euler(0, a + 22.5f, 0) * z.front;
                var p0 = foot + d * (rad + .35f) + up * (platY + 1f); var p1 = foot + d2 * (rad + .35f) + up * (platY + 1f);
                z.mk.Box(iron, (p0 + p1) / 2f, new Vector3(.05f, .05f, Vector3.Distance(p0, p1)), Quaternion.LookRotation(p1 - p0));
                z.mk.Box(iron, p0 - up * .5f, new Vector3(.04f, 1f, .04f), Quaternion.identity);
                z.solid.Box(iron, (p0 + p1) / 2f - up * .4f, new Vector3(.1f, 1.2f, Vector3.Distance(p0, p1)), Quaternion.LookRotation(p1 - p0));
            }

            // Great Mass of Color: a cloud of coloured lights hanging over the house (it drifts, FensLife)
            var cloud = new GameObject("Great Mass of Color").transform; cloud.SetParent(z.root, false);
            cloud.position = hc + up * 13f;
            var cm = new MeshKit();
            Color[] mass = { HotPink, Violet, new Color(1f, .6f, .3f), new Color(.4f, .8f, 1f), new Color(1f, .85f, .4f), new Color(.6f, 1f, .7f) };
            for (var k = 0; k < 46; k++)
            {
                var p = new Vector3(ctx.Range(-7f, 7f), ctx.Range(-2.5f, 2.5f), ctx.Range(-6f, 6f));
                if (p.x * p.x / 49f + p.y * p.y / 6.25f + p.z * p.z / 36f > 1f) continue;
                var col = mass[k % mass.Length];
                cm.Blob(GlowM(col, 3f), p, Vector3.one * ctx.Range(.25f, .6f), ctx.Rng, .1f);
                if (k % 3 == 0) halos.Add(cloud.position + p, 1.6f, col, .7f);
            }
            cm.Build("Lights", cloud, false);
            life.colorMass = cloud;

            // black spires round the island's edge: the heavy side
            for (var k = 0; k < 7; k++)
            {
                var a = (k / 7f) * Mathf.PI * 2f + .3f; var d = Quaternion.Euler(0, a * Mathf.Rad2Deg, 0) * z.front;
                if (Vector3.Dot(d, z.front) > .6f) continue;
                var p = z.ctx.OnGround(z.c.x + d.x * (z.R - 2f), z.c.z + d.z * (z.R - 2f));
                var h = ctx.Range(6f, 11f);
                z.mk.Prism(black, p - up * .3f, (up + d * .08f).normalized, ctx.Range(.5f, .8f), h, 5, .05f);
                ArtistBits.PostCollider(z.root, p, h * .5f, .6f);
            }
        }

        // ---------------------------------------------------------------- Mazzy Star
        void Bridge(Isle z)
        {
            var ctx = z.ctx; var up = Vector3.up;
            var pc = z.At(-3f, 0);
            z.mk.Prism(water, pc + up * .04f, up, 5.5f, .03f, 28);
            noGrass.Add(new Vector3(pc.x, 6.5f, pc.z));
            // the little stone bridge across it
            const int segs = 10; const float span = 12f, rise = 1.5f;
            var across = z.side;
            var rose = GlowM(new Color(1f, .2f, .35f), 2.6f); var leaves = WorldMaterials.Solid(new Color(.25f, .38f, .28f));
            Vector3 P(float t) => pc + across * (t - .5f) * span + up * (Mathf.Sin(t * Mathf.PI) * rise + .2f);
            for (var k = 0; k < segs; k++)
            {
                var a = P(k / (float)segs); var b = P((k + 1) / (float)segs); var d = b - a;
                var r = Quaternion.LookRotation(d);
                z.mk.Box(stoneM, (a + b) / 2f, new Vector3(2.2f, .3f, d.magnitude + .05f), r);
                z.solid.Box(stoneM, (a + b) / 2f - up * .1f, new Vector3(2.2f, .4f, d.magnitude + .05f), r);
                foreach (var s in new[] { -1f, 1f })
                {
                    var off = z.front * s * 1.05f;
                    z.mk.Box(stoneDark, (a + b) / 2f + off + up * .45f, new Vector3(.22f, .65f, d.magnitude + .05f), r);
                    z.solid.Box(stoneDark, (a + b) / 2f + off + up * .5f, new Vector3(.22f, 1f, d.magnitude + .05f), r);
                    // roses climbing over the parapet
                    for (var j = 0; j < 3; j++)
                    {
                        var rp = Vector3.Lerp(a, b, ctx.Range(0f, 1f)) + off + up * ctx.Range(.4f, .9f) + z.front * s * ctx.Range(.05f, .2f);
                        z.mk.Blob(leaves, rp, new Vector3(.22f, .15f, .22f), ctx.Rng, .3f);
                        if (j < 2) z.mk.Blob(rose, rp + up * .1f + z.front * s * .1f, Vector3.one * .08f, ctx.Rng, .2f);
                    }
                }
            }
            for (var k = 0; k < 6; k++) halos.Add(P((k + .5f) / 6f) + up * 1.5f + z.front * ((k % 2) * 2f - 1f) * 1.6f, 1.2f, new Color(1f, .3f, .45f), .8f);
            // candles along the top of the bridge, and a pale lamp hanging at the crown
            var flame = GlowM(new Color(1f, .7f, .45f), 3f); var wax = WorldMaterials.Solid(new Color(.95f, .92f, .88f));
            for (var k = 2; k < segs - 1; k += 2)
            {
                var p = P(k / (float)segs) + z.front * .9f + up * .78f;
                z.mk.Prism(wax, p, up, .05f, .25f, 6);
                z.mk.Blob(flame, p + up * .32f, new Vector3(.03f, .06f, .03f), ctx.Rng, 0f);
            }
            var crown = P(.5f);
            z.area.glowLight = ArtistBits.PointLight(z.root, crown + up * 2.5f, new Color(1f, .78f, .85f), 18f, 1.6f);
            // a willow with a swing
            var wt = z.At(-z.R + 5f, z.R * .5f);
            var willowLeaves = new MeshKit();
            Willow(ctx, z.mk, willowLeaves, wt, 1.1f);
            willowLeaves.Build("Soft foliage", z.root, false);
            var bough = wt + up * 4.6f + z.front * 1.8f;
            foreach (var s in new[] { -.4f, .4f }) z.mk.Box(rope0, bough + z.side * s - up * 1.6f, new Vector3(.03f, 3.2f, .03f), Quaternion.identity);
            z.mk.Box(wood, bough - up * 3.2f, new Vector3(1f, .06f, .35f), Quaternion.LookRotation(z.front));
            // Quiet, the Winter Harbor: a small frosted jetty out into the fen, a rowing boat, a pale lamp
            var back = -z.front;
            var j0 = z.c + back * (z.R - 2f); j0.y = waterY + .45f;
            var frost = WorldMaterials.Solid(new Color(.86f, .9f, .96f));
            for (var d = 0f; d < 9f; d += 1.5f)
            {
                var p = j0 + back * d;
                z.mk.Box(frost, p, new Vector3(1.8f, .1f, 1.45f), Quaternion.LookRotation(back));
                z.solid.Box(frost, p - up * .15f, new Vector3(1.8f, .4f, 1.5f), Quaternion.LookRotation(back));
                if (Mathf.Repeat(d, 3f) < .1f) foreach (var s in new[] { -1f, 1f }) z.mk.Prism(wood, p + z.side * s * .9f - up * 1.2f, up, .08f, 1.6f, 6);
            }
            var boat = j0 + back * 5f + z.side * 1.9f; boat.y = waterY - .05f;
            var hull = WorldMaterials.Solid(new Color(.6f, .7f, .8f));
            for (var i = 0; i < 5; i++) { var t = i / 4f - .5f; var w = Mathf.Lerp(.7f, .2f, Mathf.Abs(t) * 2f); z.mk.Box(hull, boat + back * t * 3f + up * .15f, new Vector3(w * 2f, .4f, .62f), Quaternion.LookRotation(back)); }
            var lampAt = j0 + back * 8.5f - z.side * .8f;
            var pale = new Color(.75f, .85f, 1f);
            z.mk.Prism(iron, lampAt, up, .05f, 2.2f, 6);
            z.mk.Box(GlowM(pale, 3f), lampAt + up * 2.3f, new Vector3(.3f, .4f, .3f), Quaternion.identity);
            halos.Add(lampAt + up * 2.3f, 2f, pale, 1f);
            ArtistBits.PointLight(z.root, lampAt + up * 2.2f, pale, 12f, 1.2f);
            for (var k = 0; k < 30; k++) z.mk.Box(GlowM(new Color(.9f, .95f, 1f), 1.6f), j0 + back * ctx.Range(0f, 9f) + z.side * ctx.Range(-.8f, .8f) + up * .06f, Vector3.one * .03f, Quaternion.identity);
        }

        Material rope0 => WorldMaterials.Solid(new Color(.7f, .62f, .55f));

        // ---------------------------------------------------------------- Slowdive
        void SunMirrors(Isle z)
        {
            var ctx = z.ctx; var up = Vector3.up;
            var p = z.At(-1f, 0);
            z.mk.Prism(iron, p, up, .1f, 5.2f, 8);
            var sunMat = new Material(GlowM(Gold, 2f)) { name = "Slowdive sun" };
            var sun = p + up * 5.4f;
            z.mk.Blob(sunMat, sun, Vector3.one * .9f, ctx.Rng, 0f);
            for (var k = 0; k < 12; k++)
            {
                var a = k * 30f; var d = Quaternion.AngleAxis(a, z.front) * z.side;
                z.mk.Box(sunMat, sun + d * 1.5f, new Vector3(.08f, .08f, .9f), Quaternion.LookRotation(d, z.front));
            }
            halos.Add(sun, 3.5f, Gold, 1f);
            ArtistBits.PostCollider(z.root, p, 5f, .15f);
            var mirror = new Material(GlowM(new Color(.85f, .82f, .95f), .35f)) { name = "Slowdive mirrors" };
            mirror.SetFloat("_Smoothness", 1f);
            for (var k = 0; k < 8; k++)
            {
                var a = k * 45f + 22.5f; var d = Quaternion.Euler(0, a, 0) * z.front;
                if (Vector3.Dot(d, z.front) > .8f) continue;
                var m = ctx.OnGround(p.x + d.x * 5.5f, p.z + d.z * 5.5f);
                var r = Quaternion.LookRotation(-d) * Quaternion.Euler(-12f, 0, 0);
                z.mk.Box(black, m + up * 1.3f, new Vector3(1.3f, 2.4f, .12f), r);
                z.mk.Box(mirror, m + up * 1.3f + (-d) * .07f, new Vector3(1.1f, 2.2f, .02f), r);
                z.solid.Box(black, m + up * 1.3f, new Vector3(1.3f, 2.6f, .3f), r);
            }
            life.sunDisc = sunMat; life.mirrors = mirror;
            z.area.glowLight = ArtistBits.PointLight(z.root, sun, Gold, 16f, 1.6f);
            life.slowdiveSun = sun;
        }

        // ---------------------------------------------------------------- my bloody valentine
        void PinkGuitar(Isle z)
        {
            var ctx = z.ctx; var up = Vector3.up;
            var b = z.At(-1.5f, 0) + up * .4f;
            var lean = (up + z.side * .45f - z.front * .15f).normalized;
            var face = Vector3.Cross(lean, z.side).normalized; if (Vector3.Dot(face, z.front) < 0) face = -face;
            var pink = WorldMaterials.Solid(new Color(.95f, .45f, .65f), .7f);
            void Guitar(Material m, Vector3 offset)
            {
                var c0 = b + offset + lean * 1.2f;
                z.mk.Blob(m, c0, new Vector3(1.3f, 1.2f, .3f), ctx.Rng, 0f);
                z.mk.Blob(m, c0 + lean * 1.3f + z.side * .2f, new Vector3(1f, .9f, .28f), ctx.Rng, 0f);
                z.mk.Box(m, c0 + lean * 4.3f, new Vector3(.35f, 4.4f, .2f), Quaternion.LookRotation(face, lean));
                z.mk.Box(m, c0 + lean * 6.8f, new Vector3(.55f, 1f, .2f), Quaternion.LookRotation(face, lean) * Quaternion.Euler(0, 0, 8f));
            }
            Guitar(pink, Vector3.zero);
            Guitar(Ghost(new Color(1f, .4f, .7f, .25f)), z.side * .45f + up * .2f);          // the blur
            Guitar(Ghost(new Color(.7f, .4f, 1f, .2f)), -z.side * .4f - up * .15f);
            var str = GlowM(new Color(1f, .8f, .95f), 3f);
            for (var k = 0; k < 6; k++)
            {
                var off = Vector3.Cross(lean, face).normalized * ((k - 2.5f) * .05f) + face * .32f;
                var s0 = b + lean * .6f + off; var s1 = b + lean * 7f + off;
                z.mk.Box(str, (s0 + s1) / 2f, new Vector3(.015f, .015f, Vector3.Distance(s0, s1)), Quaternion.LookRotation(s1 - s0));
            }
            halos.Add(b + lean * 3f + face * 1.6f, 3f, new Color(1f, .45f, .7f), .9f);
            z.solid.Box(pink, b + lean * 1.8f, new Vector3(2.6f, 3.5f, .7f), Quaternion.LookRotation(face, lean));
        }

        // ---------------------------------------------------------------- the smaller scenes
        void NeonHeart(Isle z)
        {
            var p = z.At(-1f, 0); var up = Vector3.up; var neon = GlowM(HotPink, 3.4f);
            foreach (var s in new[] { -1f, 1f }) { z.mk.Prism(iron, p + z.side * s * 1.4f, up, .05f, 3.6f, 6); ArtistBits.PostCollider(z.root, p + z.side * s * 1.4f, 3.4f, .08f); }
            var hc = p + up * 2.6f;
            Vector3 H(float t) { var a = t * Mathf.PI * 2f; var x = 16f * Mathf.Pow(Mathf.Sin(a), 3f); var y = 13f * Mathf.Cos(a) - 5f * Mathf.Cos(2f * a) - 2f * Mathf.Cos(3f * a) - Mathf.Cos(4f * a); return hc + z.side * x * .06f + up * y * .06f; }
            for (var k = 0; k < 40; k++) { var a = H(k / 40f); var b = H((k + 1) / 40f); z.mk.Box(neon, (a + b) / 2f, new Vector3(.07f, .07f, Vector3.Distance(a, b) + .02f), Quaternion.LookRotation(b - a)); }
            halos.Add(hc + z.front * 1.4f, 2.2f, HotPink, 1f);
            ArtistBits.Label(z.labels, "heaven or las vegas", hc - up * 1.4f + z.front * .05f, z.front, .18f, new Color(1f, .8f, .5f));
            for (var k = 0; k < 14; k++) z.mk.Box(GlowM(Gold, 3f), hc + new Vector3(z.ctx.Range(-2f, 2f), z.ctx.Range(-1f, 2f), z.ctx.Range(-.3f, .3f)), Vector3.one * .06f, Quaternion.Euler(45, 45, 0));
        }

        void Synth(Isle z)
        {
            var p = z.At(-1f, 0); var up = Vector3.up; var rot = Quaternion.LookRotation(z.front);
            foreach (var s in new[] { -1f, 1f }) z.mk.Box(iron, p + z.side * s * .7f + up * .45f, new Vector3(.06f, .9f, .6f), rot * Quaternion.Euler(0, 0, s * 15f));
            z.mk.Box(black, p + up * .95f, new Vector3(1.9f, .12f, .6f), rot);
            for (var k = 0; k < 15; k++) z.mk.Box(GlowM(k % 5 == 0 ? HotPink : new Color(.95f, .95f, 1f), 2.4f), p + up * 1.02f + z.side * ((k - 7) * .11f) + z.front * .12f, new Vector3(.09f, .02f, .25f), rot);
            z.solid.Box(black, p + up * .5f, new Vector3(1.9f, 1f, .6f), rot);
            // Blister Sunrise: a half sun of glowing bands rising behind it
            var sc = p - z.front * 2.5f;
            Color[] bands = { new Color(1f, .3f, .4f), new Color(1f, .5f, .35f), new Color(1f, .75f, .4f) };
            for (var b = 0; b < 3; b++)
                for (var k = 0; k < 12; k++)
                {
                    float a0 = Mathf.PI * k / 12f, a1 = Mathf.PI * (k + 1) / 12f, r0 = 2.6f - b * .8f, r1 = r0 - .7f;
                    Vector3 Q(float a, float r) => sc + z.side * Mathf.Cos(a) * r + up * Mathf.Sin(a) * r;
                    z.mk.Quad(GlowM(bands[b], 2.6f), Q(a0, r0), Q(a1, r0), Q(a1, r1), Q(a0, r1), sc - z.front);
                    z.mk.Quad(GlowM(bands[b], 2.6f), Q(a0, r0), Q(a1, r0), Q(a1, r1), Q(a0, r1), sc + z.front);
                }
            halos.Add(sc + up * 1.4f + z.front * 1.5f, 2.6f, new Color(1f, .5f, .4f), .9f);
        }

        void SpiritTree(Isle z)
        {
            var ctx = z.ctx; var p = z.At(-1.5f, 0); var up = Vector3.up; var bark = WorldMaterials.Solid(new Color(.3f, .28f, .3f));
            z.mk.Prism(bark, p - up * .3f, up, .55f, 3.4f, 8, .7f);
            ArtistBits.PostCollider(z.root, p, 3f, .6f);
            var white = GlowM(new Color(.95f, .97f, 1f), 2.6f);
            for (var k = 0; k < 7; k++)
            {
                var d = (Quaternion.Euler(0, k * 51f, 0) * z.front + up * ctx.Range(.4f, 1.1f)).normalized;
                var from = p + up * ctx.Range(2.4f, 3.1f); var len = ctx.Range(1.8f, 3f);
                z.mk.Prism(bark, from, d, .16f, len, 6, .3f);
                z.mk.Blob(WorldMaterials.Solid(new Color(.4f, .5f, .45f)), from + d * len, new Vector3(1f, .6f, 1f), ctx.Rng, .3f);
                var spirit = from + d * (len * .6f) - up * .35f;
                z.mk.Blob(white, spirit, new Vector3(.13f, .17f, .13f), ctx.Rng, 0f);
                halos.Add(spirit, .9f, new Color(.9f, .95f, 1f), .9f);
            }
        }

        void Monochrome(Isle z)
        {
            var p = z.At(-1f, 0); var up = Vector3.up; var rot = Quaternion.LookRotation(z.front);
            var grey = WorldMaterials.Solid(new Color(.3f, .3f, .3f)); var white = GlowM(new Color(.92f, .92f, .92f), 2.2f);
            z.mk.Box(grey, p + up * .35f, new Vector3(.7f, .7f, .7f), rot);
            z.mk.Box(black, p + up * 1.1f, new Vector3(1f, .8f, .8f), rot);
            z.mk.Box(white, p + up * 1.1f + z.front * .41f, new Vector3(.75f, .58f, .02f), rot);
            halos.Add(p + up * 1.1f + z.front * 1.3f, 1.4f, Color.white, .8f);
            z.solid.Box(grey, p + up * .75f, new Vector3(1f, 1.5f, .8f), rot);
            var chair = p + z.front * 2.2f + z.side * .9f;
            z.mk.Box(black, chair + up * .45f, new Vector3(.55f, .06f, .55f), rot); z.mk.Box(black, chair + up * .8f + z.front * .25f, new Vector3(.55f, .7f, .06f), rot);
            foreach (var (x, y) in new[] { (-.23f, -.23f), (.23f, -.23f), (-.23f, .23f), (.23f, .23f) }) z.mk.Box(black, chair + rot * new Vector3(x, .22f, y), new Vector3(.04f, .45f, .04f), rot);
            z.solid.Box(black, chair + up * .5f, new Vector3(.6f, 1f, .6f), rot);
        }

        void Ladder(Isle z)
        {
            var p = z.At(-1f, 0); var up = Vector3.up; var lean = (up - z.front * .18f).normalized;
            foreach (var s in new[] { -.35f, .35f }) z.mk.Box(wood, p + z.side * s + lean * 3f, new Vector3(.08f, 6f, .08f), Quaternion.LookRotation(z.front, lean));
            for (var k = 1; k < 18; k++) z.mk.Box(wood, p + lean * (k * .34f), new Vector3(.7f, .05f, .05f), Quaternion.LookRotation(z.front, lean));
            var top = p + lean * 6.2f;
            z.mk.Blob(GlowM(Lilac, 3f), top, Vector3.one * .22f, z.ctx.Rng, 0f);
            halos.Add(top, 1.8f, Lilac, 1f);
            ArtistBits.PostCollider(z.root, p, 2f, .5f);
        }

        void Tears(Isle z)
        {
            var p = z.At(-1f, 0); var up = Vector3.up;
            z.mk.Prism(stoneM, p, up, 1.1f, .6f, 12); z.mk.Prism(water, p + up * .6f, up, .95f, .02f, 12);
            z.mk.Prism(stoneM, p + up * .6f, up, .2f, 1.4f, 8);
            z.mk.Blob(stoneM, p + up * 2.2f, new Vector3(.35f, .45f, .35f), z.ctx.Rng, .1f);
            var tear = GlowM(new Color(.5f, .75f, 1f), 3f);
            for (var k = 0; k < 9; k++)
            {
                var t = p + up * (2.1f - k * .17f) + z.front * (.36f + k * .02f) + z.side * ((k % 2) * .16f - .08f);
                z.mk.Blob(tear, t, new Vector3(.04f, .06f, .04f), z.ctx.Rng, 0f);
            }
            halos.Add(p + up * 1.4f + z.front * 1f, 1.4f, new Color(.5f, .75f, 1f), .9f);
            z.solid.Prism(stoneM, p, up, 1.1f, 2.6f, 8);
        }

        void FlowerArch(Isle z)
        {
            var ctx = z.ctx; var p = z.At(-.5f, 0); var up = Vector3.up;
            Vector3 A(float t) => p + z.side * Mathf.Cos(t * Mathf.PI) * 1.5f + up * Mathf.Sin(t * Mathf.PI) * 2.8f;
            foreach (var s in new[] { -1f, 1f }) ArtistBits.PostCollider(z.root, p + z.side * s * 1.5f, 2.4f, .12f);
            for (var k = 0; k < 14; k++)
            {
                var a = A(k / 14f); var b = A((k + 1) / 14f);
                z.mk.Box(wood, (a + b) / 2f, new Vector3(.1f, .1f, Vector3.Distance(a, b) + .05f), Quaternion.LookRotation(b - a));
                var col = k % 3 == 0 ? new Color(1f, .97f, .95f) : k % 3 == 1 ? DustyPink : new Color(1f, .6f, .75f);
                z.mk.Blob(k % 2 == 0 ? GlowM(col, 1.4f) : WorldMaterials.Solid(col), a + z.front * ctx.Range(-.1f, .1f), Vector3.one * .18f, ctx.Rng, .3f);
                z.mk.Blob(leafM, a + z.front * .15f, new Vector3(.2f, .14f, .2f), ctx.Rng, .3f);
            }
            for (var s = -1; s <= 1; s += 2) z.mk.Box(wood, p + z.side * s * 1.5f + up * .1f, new Vector3(.12f, .2f, .12f), Quaternion.identity);
            halos.Add(p + up * 2.6f + z.front * 1.2f, 1.6f, DustyPink, .8f);
        }

        void PorchSwing(Isle z)
        {
            var p = z.At(-1f, 0); var up = Vector3.up; var rot = Quaternion.LookRotation(z.front);
            var white = WorldMaterials.Solid(new Color(.92f, .9f, .86f));
            foreach (var s in new[] { -1.3f, 1.3f }) { z.mk.Box(white, p + z.side * s + up * 1.4f, new Vector3(.14f, 2.8f, .14f), rot); ArtistBits.PostCollider(z.root, p + z.side * s, 2.8f, .1f); }
            z.mk.Box(white, p + up * 2.8f, new Vector3(2.9f, .14f, .14f), rot);
            foreach (var s in new[] { -.75f, .75f }) z.mk.Box(rope0, p + z.side * s + up * 2.05f, new Vector3(.03f, 1.5f, .03f), Quaternion.identity);
            z.mk.Box(white, p + up * 1.25f, new Vector3(1.7f, .08f, .5f), rot); z.mk.Box(white, p + up * 1.55f - z.front * .24f, new Vector3(1.7f, .55f, .06f), rot);
            // a screen door standing alone, light behind it
            var door = p - z.front * 2.2f + z.side * 2.2f;
            z.mk.Box(white, door + up * 1.05f, new Vector3(1f, 2.1f, .08f), rot);
            z.mk.Box(GlowM(new Color(1f, .82f, .55f), 2f), door + up * 1.1f - z.front * .05f, new Vector3(.8f, 1.8f, .02f), rot);
            halos.Add(door + up * 1.1f - z.front * 1.2f, 1.6f, new Color(1f, .82f, .55f), .9f);
            z.solid.Box(white, door + up * 1.05f, new Vector3(1f, 2.1f, .2f), rot);
        }

        void AmpStack(Isle z)
        {
            var p = z.At(-1f, 0); var up = Vector3.up; var rot = Quaternion.LookRotation(z.front);
            var grille = WorldMaterials.Solid(new Color(.22f, .22f, .24f));
            for (var k = 0; k < 2; k++)
            {
                var c = p + up * (.75f + k * 1.5f);
                z.mk.Box(black, c, new Vector3(1.6f, 1.5f, .9f), rot);
                z.mk.Box(grille, c + z.front * .46f, new Vector3(1.4f, 1.3f, .02f), rot);
            }
            var head = p + up * 3.3f;
            z.mk.Box(black, head, new Vector3(1.6f, .6f, .7f), rot);
            for (var k = 0; k < 6; k++) z.mk.Prism(WorldMaterials.Solid(new Color(.8f, .8f, .82f)), head + z.front * .36f + z.side * ((k - 2.5f) * .2f), z.front, .05f, .05f, 8);
            z.mk.Box(GlowM(new Color(1f, .15f, .15f), 3.4f), head + z.front * .36f + z.side * .68f + up * .15f, new Vector3(.06f, .06f, .02f), rot);
            halos.Add(head + z.front * 1f, 1.2f, new Color(1f, .2f, .2f), .9f);
            z.solid.Box(black, p + up * 1.8f, new Vector3(1.6f, 3.6f, .9f), rot);
        }

        void Arrows(Isle z)
        {
            var p = z.At(-1f, 0); var up = Vector3.up; var neon = GlowM(new Color(.4f, 1f, .9f), 3f);
            for (var k = 0; k < 5; k++)
            {
                var c = p + z.front * (k * .9f - 1.8f) + up * (1f + k * .25f);
                foreach (var s in new[] { -1f, 1f })
                {
                    var a = c + z.side * s * .7f - z.front * .4f; var b = c + z.front * .3f;
                    z.mk.Box(neon, (a + b) / 2f, new Vector3(.09f, .09f, Vector3.Distance(a, b)), Quaternion.LookRotation(b - a));
                }
                halos.Add(c + up * .8f, 1.1f, new Color(.4f, 1f, .9f), .8f);
            }
        }

        void Balloons(Isle z)
        {
            var ctx = z.ctx; var p = z.At(-1f, 0); var up = Vector3.up;
            z.mk.Prism(wood, p, up, .08f, 1.2f, 6); ArtistBits.PostCollider(z.root, p, 1.2f, .1f);
            for (var k = 0; k < 7; k++)
            {
                var b = p + up * ctx.Range(4f, 6.5f) + new Vector3(ctx.Range(-1.2f, 1.2f), 0, ctx.Range(-1.2f, 1.2f));
                var col = Glows[k % Glows.Length];
                z.mk.Blob(GlowM(col, 1.3f), b, new Vector3(.4f, .5f, .4f), ctx.Rng, 0f);
                var s0 = p + up * 1.2f; var d = b - s0;
                z.mk.Box(WorldMaterials.Solid(PaleFog), (s0 + b) / 2f, new Vector3(.015f, .015f, d.magnitude), Quaternion.LookRotation(d));
                if (k % 2 == 0) halos.Add(b, 1.1f, col, .6f);
            }
        }

        void Crash(Isle z)
        {
            var p = z.At(-1f, .5f); var up = Vector3.up; var rot = Quaternion.LookRotation(z.side) * Quaternion.Euler(0, 0, 4f);
            var paint = WorldMaterials.Solid(new Color(.55f, .62f, .78f), .4f);
            z.mk.Box(paint, p + up * .6f, new Vector3(1.7f, .55f, 3.6f), rot);
            z.mk.Box(paint, p + up * 1.05f - z.side * .3f, new Vector3(1.5f, .5f, 1.8f), rot * Quaternion.Euler(6f, 0, 0));
            var post = p + z.side * 2.3f;
            z.mk.Prism(iron, post, (up + z.side * .25f).normalized, .07f, 3.2f, 6);
            z.mk.Box(GlowM(new Color(1f, .85f, .55f), 2.6f), post + (up + z.side * .25f).normalized * 3.2f, new Vector3(.35f, .2f, .35f), Quaternion.identity);
            halos.Add(post + (up + z.side * .25f).normalized * 3.2f, 1.4f, new Color(1f, .85f, .55f), .9f);
            z.solid.Box(paint, p + up * .8f, new Vector3(1.8f, 1.6f, 3.6f), rot);
            ArtistBits.PostCollider(z.root, post, 3f, .1f);
        }

        /// Everyone else: one of a set of dreamy, noisy things, chosen by name, at double size (giant, so it reads
        /// from across the water, and fits the dream).
        void Generic(Isle z)
        {
            var ctx = z.ctx; var at = z.At(-1f, 0); var p = Vector3.zero; var up = Vector3.up;
            var rot = Quaternion.LookRotation(z.front);
            var big = new MeshKit(); var bigSolid = new MeshKit();            // built small at the origin, then shown at double size
            var col = Glows[Mathf.Abs(z.a.name.GetHashCode()) % Glows.Length];
            switch (Mathf.Abs(z.a.name.GetHashCode() / 3) % 8)
            {
                case 0: // a stack of pedals with a cable snaking off
                    for (var k = 0; k < 4; k++) big.Box(WorldMaterials.Solid(Glows[(k + 1) % Glows.Length] * .8f), p + up * (.25f + k * .45f), new Vector3(.9f - k * .1f, .42f, .7f - k * .05f), rot * Quaternion.Euler(0, k * 12f, 0));
                    big.Prism(GlowM(col, 3f), p + up * 1.9f, up, .06f, .05f, 6);
                    bigSolid.Box(black, p + up * .9f, new Vector3(.9f, 1.8f, .7f), rot);
                    break;
                case 1: // an amp with a lamp on it
                    big.Box(black, p + up * .6f, new Vector3(1.2f, 1.2f, .6f), rot);
                    big.Box(WorldMaterials.Solid(new Color(.75f, .7f, .6f)), p + up * .6f + z.front * .31f, new Vector3(1f, .8f, .02f), rot);
                    big.Prism(WorldMaterials.Solid(new Color(.6f, .5f, .4f)), p + up * 1.2f, up, .08f, .5f, 6);
                    big.Prism(GlowM(col, 2.4f), p + up * 1.65f, up, .3f, .35f, 10, .6f);
                    bigSolid.Box(black, p + up * .6f, new Vector3(1.2f, 1.2f, .6f), rot);
                    break;
                case 2: // a microphone stand, the mic glowing
                    big.Prism(iron, p, up, .04f, 1.6f, 6);
                    big.Blob(GlowM(col, 3f), p + up * 1.7f + z.front * .1f, new Vector3(.1f, .14f, .1f), ctx.Rng, 0f);
                    bigSolid.Box(black, p + up * (1.6f / 2f), new Vector3(.1f * 2f, 1.6f, .1f * 2f), Quaternion.identity);
                    break;
                case 3: // a giant cassette leaning in the moss
                    big.Box(black, p + up * .9f, new Vector3(2.4f, 1.6f, .25f), rot * Quaternion.Euler(-10f, 0, 0));
                    big.Box(WorldMaterials.Solid(DustyPink), p + up * 1.05f + z.front * .14f, new Vector3(2f, .7f, .02f), rot * Quaternion.Euler(-10f, 0, 0));
                    foreach (var s in new[] { -.55f, .55f }) big.Prism(GlowM(col, 2f), p + up * .75f + z.side * s + z.front * .12f, z.front, .2f, .03f, 10);
                    bigSolid.Box(black, p + up * .9f, new Vector3(2.4f, 1.8f, .5f), rot);
                    break;
                case 4: // a dream catcher hanging from a crooked post
                    big.Prism(wood, p, (up - z.front * .1f).normalized, .07f, 2.8f, 6);
                    var hoop = p + up * 2f + z.front * .35f;
                    for (var k = 0; k < 16; k++) { var a = k * Mathf.PI / 8f; var b = (k + 1) * Mathf.PI / 8f; Vector3 Q(float t) => hoop + (z.side * Mathf.Cos(t) + up * Mathf.Sin(t)) * .5f; big.Box(wood, (Q(a) + Q(b)) / 2f, new Vector3(.03f, .03f, Vector3.Distance(Q(a), Q(b)) + .01f), Quaternion.LookRotation(Q(b) - Q(a))); }
                    big.Blob(GlowM(col, 3f), hoop, Vector3.one * .08f, ctx.Rng, 0f);
                    for (var k = 0; k < 3; k++) big.Box(WorldMaterials.Solid(PaleFog), hoop - up * (.8f + k * .05f) + z.side * (k - 1) * .25f, new Vector3(.08f, .35f, .02f), rot);
                    bigSolid.Box(black, p + up * (2.6f / 2f), new Vector3(.1f * 2f, 2.6f, .1f * 2f), Quaternion.identity);
                    break;
                case 5: // a lava lamp as tall as you
                    big.Prism(iron, p, up, .4f, .5f, 10, .6f);
                    big.Prism(GlowM(col, 1.8f), p + up * .5f, up, .3f, 1.3f, 10, .7f);
                    for (var k = 0; k < 3; k++) big.Blob(GlowM(Color.Lerp(col, Color.white, .4f), 3f), p + up * (.8f + k * .35f), Vector3.one * .12f, ctx.Rng, .3f);
                    big.Prism(iron, p + up * 1.8f, up, .21f, .3f, 10, .5f);
                    bigSolid.Box(black, p + up * (2f / 2f), new Vector3(.4f * 2f, 2f, .4f * 2f), Quaternion.identity);
                    break;
                case 6: // a television of static on a crate
                    big.Box(wood, p + up * .3f, new Vector3(.8f, .6f, .6f), rot);
                    big.Box(black, p + up * .95f, new Vector3(.9f, .7f, .7f), rot);
                    big.Box(GlowM(new Color(.85f, .85f, .95f), 2f), p + up * .95f + z.front * .36f, new Vector3(.66f, .5f, .02f), rot);
                    bigSolid.Box(black, p + up * .65f, new Vector3(.9f, 1.3f, .7f), rot);
                    break;
                default: // a record on a stand
                    big.Box(wood, p + up * .3f, new Vector3(.3f, .6f, .3f), rot);
                    big.Prism(black, p + up * 1.25f - z.front * .02f, z.front, .7f, .03f, 24);
                    big.Prism(GlowM(col, 2.4f), p + up * 1.25f + z.front * .015f, z.front, .2f, .01f, 12);
                    bigSolid.Box(black, p + up * (1.9f / 2f), new Vector3(.4f * 2f, 1.9f, .4f * 2f), Quaternion.identity);
                    break;
            }
            var go = big.Build("Giant thing", z.root, false);
            go.transform.SetPositionAndRotation(at, Quaternion.identity); go.transform.localScale = Vector3.one * 2f;
            bigSolid.BuildCollider("Giant thing walls", go.transform);
            halos.Add(at + (up * 1.4f + z.front * 1.4f) * 2f, 2.4f, col, .7f);
        }
    }
}
