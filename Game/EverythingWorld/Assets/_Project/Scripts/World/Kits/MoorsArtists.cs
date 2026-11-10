using System.Collections.Generic;
using UnityEngine;

namespace EverythingWorld.World
{
    /// The Alt Rock Moors' artists, each with covers on a slate board, a plaque and a street lamp:
    /// - Radiohead: the transmitter station (MoorsRadiohead). Gorillaz: a little floating island with a turning windmill.
    ///   Weezer: a fifties diner with a neon sign. The Cranberries: a ruined round tower with a dream cloud glowing over
    ///   it and cranberry bushes. Red Hot Chili Peppers: a swimming pool with a red eight-armed star over it. Hootie &
    ///   The Blowfish: a giant pufferfish and an owl on a post. Violet Grohl: a plastic-covered couch, a giant cake with
    ///   a beetle on it, and a fish-shaped apple.
    /// - Oasis: a giant champagne bottle and a supernova hanging over it. Muse: a red planet with rings over a lone
    ///   horse. Coldplay: yellow stars on poles and a cracked crown on a plinth. Pulp: a shopping trolley and a lit
    ///   disco floor. Train: Jupiter floating. Smash Mouth: a shooting star. Rage Against The Machine: a stack of
    ///   speakers with a megaphone. Blur: speakers and a trampoline. bôa: an elephant. Black Box Recorder: an orange
    ///   flight recorder. Dishwalla: a row of blue cars. Everyone else: a bus shelter, a phone box or a stone hut.
    public partial class MoorsKit
    {
        class Plot
        {
            public LandContext ctx; public Transform root, labels; public MeshKit mk, solid, pictures;
            public IslandWorldData.Artist a; public Vector3 c, front, side; public List<Renderer> glows; public ArtistArea area;
            public Vector3 At(float fw, float rt) { var p = c + front * fw + side * rt; return ctx.OnGround(p.x, p.z); }
            public Quaternion Rot => Quaternion.LookRotation(front);
        }

        public override void BuildArtist(LandContext ctx, IslandWorldData.Artist a)
        {
            if (stone == null) MakeMaterials();
            var root = new GameObject("Artist: " + a.name).transform; root.SetParent(ctx.Root, false);
            var front = facing.TryGetValue(a.name, out var f) ? f : new Vector3(center.x - a.x, 0, center.z - a.z).normalized;
            if (front.sqrMagnitude < .01f) front = Vector3.forward;
            var labels = new GameObject("Labels").transform; labels.SetParent(root, false);
            var area = root.gameObject.AddComponent<ArtistArea>();
            area.artistName = a.name; area.stationId = a.stationId; area.radius = a.r; area.glowColor = Lamp;
            var z = new Plot { ctx = ctx, root = root, labels = labels, mk = new MeshKit(), solid = new MeshKit(), pictures = new MeshKit(), a = a, c = ctx.OnGround(a.x, a.z), front = front, side = Vector3.Cross(Vector3.up, front), glows = new List<Renderer>(), area = area };
            switch (a.name)
            {
                case "Radiohead": RadioheadHall(z); break;
                case "Gorillaz": Windmill(z); break;
                case "Weezer": Diner(z); break;
                case "The Cranberries": RoundTower(z); break;
                case "Red Hot Chili Peppers": Pool(z); break;
                case "Hootie & The Blowfish": Blowfish(z); break;
                case "Violet Grohl": Couch(z); break;
                case "Oasis": Champagne(z); break;
                case "Muse": RedPlanet(z); break;
                case "Coldplay": YellowStars(z); break;
                case "Pulp": Disco(z); break;
                case "Train": Jupiter(z); break;
                case "Smash Mouth": ShootingStar(z); break;
                case "Rage Against The Machine": Megaphone(z); break;
                case "Blur": Trampoline(z); break;
                case "Black Box Recorder": BlackBox(z); break;
                case "Dishwalla": BlueCars(z); break;
                default: if (a.name.StartsWith("b") && a.name.EndsWith("a")) Elephant(z); else Small(z); break;
            }
            var reach = Foot(a);
            Board(z, z.At(reach * .8f, -reach * .5f - 2.5f), front);
            ArtistBits.Plaque(z.mk, z.solid, labels, a, z.At(reach * .8f + 1.5f, reach * .5f + 2.5f), front, slate, dark, Ink, 1.6f);
            StreetLamp(ctx, z.mk, z.At(reach * .8f, reach * .5f + 5f));
            if (z.area.glowLight == null) z.area.glowLight = ArtistBits.PointLight(root, z.c + Vector3.up * 5f + front * 5f, Lamp, HasStation(a) ? 18f : 12f, 1.3f);
            area.glowRenderers = z.glows.ToArray();
            z.mk.Build("Area", root, false);
            z.solid.BuildCollider("Walls", root);
            z.pictures.Build("Covers", root, false);
            noGrass.Add(new Vector3(z.c.x, reach * .9f, z.c.z));
        }

        void Board(Plot z, Vector3 ground, Vector3 dir)
        {
            dir = Vector3.ProjectOnPlane(dir, Vector3.up).normalized; var rot = Quaternion.LookRotation(dir); var side = Vector3.Cross(Vector3.up, dir);
            var n = Mathf.Max(1, Mathf.Min(5, z.a.songList?.Length ?? 1)); const float size = .9f; var w = n * size * 1.25f + .4f;
            foreach (var s in new[] { -1f, 1f }) { z.mk.Box(wallStone, ground + side * s * (w / 2f + .1f) + Vector3.up * 1.2f, new Vector3(.4f, 2.6f, .4f), rot); ArtistBits.PostCollider(z.root, ground + side * s * (w / 2f + .1f), 2.4f, .2f); }
            var c = ground + Vector3.up * 1.8f;
            z.mk.Box(slate, c - Vector3.up * .1f, new Vector3(w, size + .6f, .14f), rot); z.solid.Box(slate, c - Vector3.up * .1f, new Vector3(w, size + .6f, .24f), rot);
            z.mk.Box(stone, c + Vector3.up * (size / 2f + .3f), new Vector3(w + 1f, .2f, .5f), rot);
            ArtistBits.CoverWall(z.pictures, z.labels, z.ctx, z.a, c + dir * .075f + Vector3.up * .08f, dir, n, size, n, true, Ink);
        }

        // ---------------------------------------------------------------- the radio-station artists
        void Windmill(Plot z)
        {
            var up = Vector3.up; var isl = new MeshKit(); var grass = WorldMaterials.Solid(new Color(.4f, .55f, .3f)); var earth = WorldMaterials.Solid(new Color(.4f, .32f, .25f));
            isl.Prism(earth, Vector3.zero, Vector3.down, 5f, 5f, 9, .1f, 0f, false); isl.Prism(grass, Vector3.zero, Vector3.up, 5.1f, .4f, 9);
            isl.Prism(wood, up * .4f, up, 1.4f, 6f, 8, .6f); isl.Cone(slate, up * 6.4f, 1.2f, 1.5f, 8);
            isl.Box(GlowM(Lamp, 1.6f), up * 3f + Vector3.forward * 1.2f, new Vector3(.6f, .8f, .1f), Quaternion.identity);
            var t = isl.BuildSingle("Floating windmill island", life.transform, false).transform; life.island = t; life.islandBase = z.At(-6f, 0) + up * 9f; t.position = life.islandBase;
            var sails = new MeshKit(); for (var k = 0; k < 4; k++) sails.Box(WorldMaterials.Solid(new Color(.92f, .9f, .85f)), Quaternion.Euler(0, 0, k * 90f) * (Vector3.up * 2.4f), new Vector3(.9f, 4.4f, .08f), Quaternion.Euler(0, 0, k * 90f));
            var st = sails.BuildSingle("Windmill sails", t, false).transform; st.localPosition = up * 5.4f + Vector3.forward * 1.5f; life.sails = st;
            halos.Add(life.islandBase + up * 3f, 4f, Lamp, .2f);
            var shadow = z.At(-6f, 0); z.mk.Prism(WorldMaterials.Solid(new Color(.3f, .35f, .28f)), shadow + up * .02f, up, 4f, .02f, 12);
        }

        void Diner(Plot z)
        {
            var up = Vector3.up; var rot = z.Rot; var c = z.At(-5f, 0); var chrome = WorldMaterials.Solid(new Color(.85f, .86f, .88f), .95f); var teal = WorldMaterials.Solid(new Color(.35f, .75f, .72f), .6f);
            z.mk.Box(chrome, c + up * 1.8f, new Vector3(12f, 3.2f, 5f), rot); z.solid.Box(chrome, c + up * 1.8f, new Vector3(12f, 3.2f, 5f), rot);
            z.mk.Prism(chrome, c + up * 3.4f - z.side * 6f, z.side, 2.5f, 12f, 16); z.mk.Box(teal, c + up * 1f + z.front * 2.52f, new Vector3(12f, .6f, .05f), rot);
            for (var k = 0; k < 6; k++) z.mk.Box(GlowM(new Color(1f, .9f, .65f), 1.6f), c + up * 2.1f + z.front * 2.52f + z.side * ((k - 2.5f) * 1.8f), new Vector3(1.4f, 1f, .03f), rot);
            var sign = c + up * 6.2f; z.mk.Box(dark, sign, new Vector3(5f, 1.2f, .3f), rot); z.mk.Box(GlowM(new Color(1f, .3f, .5f), 2.6f), sign + z.front * .16f, new Vector3(4.6f, .8f, .02f), rot);
            ArtistBits.Label(z.labels, "DINER", sign + z.front * .18f, z.front, .35f, Color.white, TextAnchor.MiddleCenter);
            foreach (var s in new[] { -2f, 2f }) z.mk.Prism(chrome, c + up * 4.6f + z.side * s, up, .08f, 1f, 6);
            halos.Add(sign + z.front * .5f, 2.6f, new Color(1f, .3f, .5f), .55f);
            IndoorSpaces.Add(c + up * 1.8f, rot, new Vector3(12f, 3.2f, 5f));
        }

        void RoundTower(Plot z)
        {
            var up = Vector3.up; var c = z.At(-4f, 0);
            for (var k = 0; k < 5; k++) z.mk.Prism(stone, c + up * k * 2.6f, up, 2.4f - k * .08f, 2.6f, 14, (2.4f - (k + 1) * .08f) / (2.4f - k * .08f));
            z.mk.Box(dark, c + up * 3f + z.front * 2.3f, new Vector3(.8f, 1.6f, .1f), z.Rot);
            z.solid.Prism(stone, c, up, 2.4f, 12f, 12);
            var cloud = c + up * 17f; var dream = GlowM(new Color(.8f, .85f, 1f), 1.2f);
            for (var k = 0; k < 6; k++) z.mk.Blob(dream, cloud + new Vector3(Mathf.Cos(k) * 2f, Mathf.Sin(k * 2f) * .5f, Mathf.Sin(k) * 2f), new Vector3(1.8f, 1.1f, 1.6f), z.ctx.Rng, .2f);
            halos.Add(cloud, 5f, new Color(.8f, .85f, 1f), .4f);
            var berry = WorldMaterials.Solid(new Color(.75f, .1f, .2f)); var bush = WorldMaterials.Solid(new Color(.25f, .38f, .22f));
            for (var k = 0; k < 4; k++) { var bp = z.At(0f, -6f + k * 4f); soft.Blob(bush, bp + up * .5f, new Vector3(1f, .7f, 1f), z.ctx.Rng, .3f); for (var b = 0; b < 8; b++) z.mk.Blob(berry, bp + up * .5f + new Vector3(z.ctx.Range(-.8f, .8f), z.ctx.Range(.1f, .6f), z.ctx.Range(-.8f, .8f)), Vector3.one * .1f, z.ctx.Rng, 0f); }
        }

        void Pool(Plot z)
        {
            var up = Vector3.up; var c = z.At(-4f, 0); var tile = WorldMaterials.Solid(new Color(.9f, .92f, .92f));
            z.mk.Box(tile, c + up * .15f, new Vector3(12f, .3f, 8f), z.Rot);
            var water = new Material(WorldMaterials.Glow(new Color(.2f, .7f, .9f), .7f)) { name = "Pool water" }; z.mk.Box(water, c + up * .32f, new Vector3(10f, .04f, 6f), z.Rot);
            z.solid.Box(tile, c + up * .2f, new Vector3(12f, .4f, 8f), z.Rot);
            var ladder = c + z.side * 5f + z.front * 2f; foreach (var s in new[] { -.3f, .3f }) z.mk.Box(WorldMaterials.Solid(new Color(.85f, .85f, .88f), .9f), ladder + z.front * s + up * .8f, new Vector3(.06f, 1.2f, .06f), z.Rot);
            var star = c + up * 7f; var red = GlowM(new Color(1f, .15f, .12f), 2.2f);
            for (var k = 0; k < 8; k++) z.mk.Box(red, star + Quaternion.AngleAxis(k * 45f, z.front) * up * 1.4f, new Vector3(.5f, 2.8f, .2f), Quaternion.AngleAxis(k * 45f, z.front) * z.Rot);
            z.mk.Prism(steel, c + up * .3f - z.front * 4f, up, .15f, 6.4f, 6); halos.Add(star, 3.5f, new Color(1f, .15f, .12f), .45f);
            halos.Add(c + up * .8f, 5f, new Color(.2f, .7f, .9f), .25f);
        }

        void Blowfish(Plot z)
        {
            var up = Vector3.up; var c = z.At(-4f, -2f) + up * 3.2f; var skin = WorldMaterials.Solid(new Color(.9f, .78f, .45f)); var spine = WorldMaterials.Solid(new Color(.75f, .6f, .3f));
            z.mk.Blob(skin, c, Vector3.one * 2.4f, z.ctx.Rng, 0f);
            for (var k = 0; k < 40; k++) { var d = Quaternion.Euler(z.ctx.Range(0, 360f), z.ctx.Range(0, 360f), 0) * Vector3.forward; z.mk.Cone(spine, c + d * 2.2f, .15f, .6f, 4); }
            foreach (var s in new[] { -.8f, .8f }) { z.mk.Blob(WorldMaterials.Solid(Color.white), c + z.front * 2.1f + z.side * s + up * .5f, Vector3.one * .4f, z.ctx.Rng, 0f); z.mk.Blob(dark, c + z.front * 2.4f + z.side * s + up * .5f, Vector3.one * .2f, z.ctx.Rng, 0f); }
            z.mk.Prism(skin, c - z.front * 2.2f, -z.front, .6f, 1f, 6, 1.8f); z.mk.Prism(darkStone(), z.At(-4f, -2f) - up * .3f, up, 1.4f, 1.2f, 8);
            z.solid.Box(skin, c - up * 1.4f, new Vector3(4.6f, 6f, 4.6f), z.Rot);
            var post = z.At(-2f, 5f); z.mk.Prism(wood, post, up, .15f, 3f, 6); ArtistBits.PostCollider(z.root, post, 3f, .2f);
            var owl = post + up * 3.4f; var feather = WorldMaterials.Solid(new Color(.55f, .45f, .35f));
            z.mk.Blob(feather, owl, new Vector3(.4f, .5f, .38f), z.ctx.Rng, 0f); z.mk.Blob(feather, owl + up * .55f, Vector3.one * .32f, z.ctx.Rng, 0f);
            foreach (var s in new[] { -.13f, .13f }) { z.mk.Blob(GlowM(new Color(1f, .8f, .2f), 2.4f), owl + up * .6f + z.front * .28f + z.side * s, Vector3.one * .08f, z.ctx.Rng, 0f); z.mk.Cone(feather, owl + up * .82f + z.side * s * 1.4f, .07f, .2f, 4); }
        }

        Material darkStone() => WorldMaterials.Solid(new Color(.35f, .35f, .35f));

        void Couch(Plot z)
        {
            var up = Vector3.up; var rot = z.Rot; var c = z.At(-2f, -3f); var fabric = WorldMaterials.Solid(new Color(.75f, .55f, .45f)); var plastic = WorldMaterials.Solid(new Color(.85f, .9f, .95f), .95f);
            z.mk.Box(fabric, c + up * .45f, new Vector3(3f, .5f, 1.1f), rot); z.mk.Box(fabric, c + up * 1f - z.front * .45f, new Vector3(3f, .9f, .25f), rot);
            foreach (var s in new[] { -1.4f, 1.4f }) z.mk.Box(fabric, c + up * .75f + z.side * s, new Vector3(.3f, .6f, 1.1f), rot);
            z.mk.Box(plastic, c + up * .62f, new Vector3(3.15f, 1.3f, 1.25f), rot * Quaternion.Euler(0, 0, 1f));
            z.solid.Box(fabric, c + up * .6f, new Vector3(3.2f, 1.2f, 1.3f), rot);
            var cake = z.At(-4f, 4f); var sponge = WorldMaterials.Solid(new Color(.95f, .85f, .7f)); var icing = WorldMaterials.Solid(new Color(1f, .7f, .8f));
            for (var k = 0; k < 3; k++) { z.mk.Prism(sponge, cake + up * k * 1.1f, up, 2.4f - k * .6f, 1f, 18); z.mk.Prism(icing, cake + up * (k * 1.1f + 1f), up, 2.45f - k * .6f, .12f, 18); }
            var bug = cake + up * 3.4f; var shell = WorldMaterials.Solid(new Color(.15f, .35f, .2f), .8f); z.mk.Blob(shell, bug, new Vector3(.5f, .3f, .7f), z.ctx.Rng, 0f); for (var k = 0; k < 6; k++) z.mk.Box(dark, bug + z.side * ((k % 2) - .5f) * .8f + z.front * ((k / 2) - 1f) * .35f - up * .15f, new Vector3(.5f, .04f, .04f), Quaternion.LookRotation(z.front));
            z.solid.Prism(sponge, cake, up, 2.4f, 3.3f, 12);
            var ap = z.At(1f, 6f) + up * 1.4f; z.mk.Blob(WorldMaterials.Solid(new Color(.75f, .15f, .15f), .7f), ap, new Vector3(.9f, .85f, .9f), z.ctx.Rng, 0f); z.mk.Prism(WorldMaterials.Solid(new Color(.75f, .15f, .15f), .7f), ap - z.front * .9f, -z.front, .4f, .6f, 4, 1.6f); z.mk.Box(WorldMaterials.Solid(new Color(.35f, .5f, .2f)), ap + up * 1f, new Vector3(.08f, .4f, .08f), Quaternion.identity);
        }

        // ---------------------------------------------------------------- everyone else
        void Champagne(Plot z)
        {
            var up = Vector3.up; var c = z.At(-3f, 0); var glass = WorldMaterials.Solid(new Color(.15f, .35f, .18f), .9f); var foil = WorldMaterials.Solid(new Color(.9f, .75f, .3f), .9f);
            z.mk.Prism(glass, c, up, 1.2f, 4f, 14); z.mk.Prism(glass, c + up * 4f, up, 1.2f, 1.5f, 14, .3f); z.mk.Prism(foil, c + up * 5.5f, up, .4f, 1.4f, 10);
            z.mk.Box(WorldMaterials.Solid(new Color(.95f, .92f, .8f)), c + up * 2f + z.front * 1.18f, new Vector3(1.4f, 1.2f, .05f), z.Rot);
            z.solid.Prism(glass, c, up, 1.2f, 7f, 10);
            var nova = c + up * 16f; var m = new Material(WorldMaterials.Glow(new Color(1f, .9f, .6f), 2f)) { name = "Supernova" }; life.nova = m; life.novaAt = nova;
            z.mk.Blob(m, nova, Vector3.one * .8f, z.ctx.Rng, 0f); for (var k = 0; k < 10; k++) z.mk.Box(m, nova + Quaternion.Euler(k * 36f, k * 72f, 0) * Vector3.forward * 1.4f, new Vector3(.12f, .12f, 1.8f), Quaternion.Euler(k * 36f, k * 72f, 0));
            halos.Add(nova, 4f, new Color(1f, .9f, .6f), .6f);
        }

        void RedPlanet(Plot z)
        {
            var up = Vector3.up; var p = z.At(-5f, 0) + up * 11f; var red = WorldMaterials.Solid(new Color(.75f, .3f, .2f));
            z.mk.Blob(red, p, Vector3.one * 3f, z.ctx.Rng, .05f);
            for (var k = 0; k < 40; k++) { var a0 = k * Mathf.PI / 20f; var a1 = (k + 1) * Mathf.PI / 20f; var r0 = Quaternion.Euler(20f, 0, 15f) * new Vector3(Mathf.Cos(a0), 0, Mathf.Sin(a0)) * 5f; var r1 = Quaternion.Euler(20f, 0, 15f) * new Vector3(Mathf.Cos(a1), 0, Mathf.Sin(a1)) * 5f; z.mk.Box(GlowM(new Color(1f, .7f, .5f), .8f), p + (r0 + r1) / 2f, new Vector3(.8f, .05f, Vector3.Distance(r0, r1) + .02f), Quaternion.LookRotation(r1 - r0)); }
            var h = z.At(-1f, 3f); var coat = WorldMaterials.Solid(new Color(.2f, .18f, .16f));
            z.mk.Box(coat, h + up * 1.4f, new Vector3(.7f, .8f, 2f), Quaternion.LookRotation(z.side)); z.mk.Box(coat, h + up * 2.1f + z.side * 1.1f, new Vector3(.4f, 1f, .5f), Quaternion.LookRotation(z.side) * Quaternion.Euler(-30f, 0, 0));
            foreach (var s in new[] { -.25f, .25f }) foreach (var f in new[] { -.7f, .7f }) z.mk.Box(coat, h + up * .5f + z.front * s + z.side * f, new Vector3(.14f, 1f, .14f), Quaternion.identity);
            z.solid.Box(coat, h + up * 1.2f, new Vector3(.8f, 2.4f, 2.4f), Quaternion.LookRotation(z.side));
            halos.Add(p, 5f, new Color(1f, .5f, .3f), .25f);
        }

        void YellowStars(Plot z)
        {
            var up = Vector3.up; var yellow = GlowM(new Color(1f, .85f, .15f), 2.2f);
            for (var k = 0; k < 7; k++)
            {
                var p = z.At(-2f - (k % 3) * 2f, (k - 3f) * 1.8f); var h = 3f + (k % 3) * 1.2f; z.mk.Prism(steel, p, up, .05f, h, 6);
                var s = p + up * (h + .5f); for (var a = 0; a < 5; a++) z.mk.Box(yellow, s + Quaternion.AngleAxis(a * 72f, z.front) * up * .3f, new Vector3(.18f, .6f, .06f), Quaternion.AngleAxis(a * 72f, z.front) * z.Rot);
                halos.Add(s, 1.2f, new Color(1f, .85f, .15f), .7f);
            }
            var cp = z.At(-7f, 5f); z.mk.Prism(stone, cp, up, .9f, 1.2f, 10); var gold = WorldMaterials.Solid(new Color(.9f, .72f, .25f), .9f);
            z.mk.Prism(gold, cp + up * 1.2f, up, .7f, .5f, 10); for (var k = 0; k < 5; k++) z.mk.Cone(gold, cp + up * 1.7f + Quaternion.Euler(0, k * 72f, 0) * Vector3.forward * .55f, .14f, .4f, 4);
            z.mk.Box(dark, cp + up * 1.45f + z.front * .7f, new Vector3(.05f, .5f, .02f), z.Rot * Quaternion.Euler(0, 0, 20f)); ArtistBits.PostCollider(z.root, cp, 2f, .9f);
        }

        void Disco(Plot z)
        {
            var up = Vector3.up; var c = z.At(-2.5f, 0);
            Color[] tiles = { new Color(1f, .3f, .6f), new Color(.3f, .7f, 1f), new Color(1f, .85f, .3f), new Color(.5f, 1f, .5f) };
            for (var i = 0; i < 5; i++) for (var j = 0; j < 5; j++) z.mk.Box(GlowM(tiles[(i + j * 2) % 4], 1.2f), c + z.side * (i - 2f) * 1.1f + z.front * (j - 2f) * 1.1f + up * .08f, new Vector3(1.05f, .14f, 1.05f), z.Rot);
            var tr = z.At(1.5f, 5f); var wire = WorldMaterials.Solid(new Color(.8f, .8f, .82f), .9f);
            z.mk.Box(wire, tr + up * .7f, new Vector3(.8f, .7f, 1.2f), z.Rot); foreach (var s in new[] { -.35f, .35f }) foreach (var f in new[] { -.5f, .5f }) z.mk.Prism(dark, tr + z.side * s + z.front * f + up * .1f, z.side, .1f, .06f, 8);
            z.mk.Box(wire, tr + up * 1.2f - z.front * .7f, new Vector3(.8f, .06f, .3f), z.Rot);
            halos.Add(c + up * .5f, 3.5f, new Color(1f, .5f, .8f), .3f);
        }

        void Jupiter(Plot z)
        {
            var up = Vector3.up; var p = z.At(-4f, 0) + up * 9f;
            Color[] bands = { new Color(.85f, .7f, .5f), new Color(.75f, .55f, .4f), new Color(.92f, .85f, .72f), new Color(.7f, .45f, .3f), new Color(.88f, .78f, .6f) };
            for (var k = 0; k < 5; k++) { var y = (k - 2f) * .9f; var r = Mathf.Sqrt(Mathf.Max(.1f, 9f - y * y)); z.mk.Prism(WorldMaterials.Solid(bands[k]), p + up * (y - .45f), up, r, .9f, 18); }
            z.mk.Blob(WorldMaterials.Solid(new Color(.8f, .3f, .2f)), p + z.front * 2.7f - up * .9f, new Vector3(.6f, .3f, .3f), z.ctx.Rng, 0f);
            halos.Add(p, 5f, new Color(1f, .85f, .6f), .2f);
            z.mk.Prism(stone, z.At(-4f, 0) - up * .3f, up, 1f, .8f, 8);
        }

        void ShootingStar(Plot z)
        {
            var up = Vector3.up; var s = z.At(-3f, 0) + up * 5f; var star = GlowM(new Color(1f, .9f, .4f), 3f);
            for (var a = 0; a < 5; a++) z.mk.Box(star, s + Quaternion.AngleAxis(a * 72f, z.front) * up * .7f, new Vector3(.45f, 1.4f, .2f), Quaternion.AngleAxis(a * 72f, z.front) * z.Rot);
            for (var k = 0; k < 5; k++) z.mk.Box(GlowM(new Color(1f, .7f, .3f), 1.6f - k * .25f), s - z.side * (1.6f + k * 1.4f) + up * (-k * .4f), new Vector3(1.4f, .25f - k * .03f, .1f), Quaternion.LookRotation(z.front) * Quaternion.Euler(0, 0, -15f));
            z.mk.Prism(steel, z.At(-3f, 0), up, .1f, 4.4f, 6); ArtistBits.PostCollider(z.root, z.At(-3f, 0), 4.4f, .15f);
            halos.Add(s, 2.4f, new Color(1f, .9f, .4f), .6f);
        }

        void Megaphone(Plot z)
        {
            var up = Vector3.up; var c = z.At(-3f, 0); var rot = z.Rot;
            for (var k = 0; k < 6; k++) { var p = c + z.side * ((k % 3) - 1f) * 1.3f + up * ((k / 3) * 1.3f + .65f); z.mk.Box(dark, p, new Vector3(1.25f, 1.25f, 1f), rot); z.mk.Prism(WorldMaterials.Solid(new Color(.25f, .25f, .27f)), p + z.front * .51f, z.front, .45f, .02f, 14); }
            z.solid.Box(dark, c + up * 1.3f, new Vector3(4f, 2.6f, 1f), rot);
            z.mk.Prism(redPaint, c + up * 2.9f, z.front, .2f, 1.4f, 12, 3f);
            z.mk.Box(redPaint, c + up * 4.5f - z.front * .2f, new Vector3(3.6f, .9f, .1f), rot);
        }

        void Trampoline(Plot z)
        {
            var up = Vector3.up; var c = z.At(-3f, 0);
            z.mk.Prism(steel, c + up * .7f, up, 2.4f, .1f, 20); z.mk.Prism(dark, c + up * .75f, up, 2.2f, .03f, 20);
            for (var k = 0; k < 6; k++) { var a = k * 60f * Mathf.Deg2Rad; z.mk.Box(steel, c + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * 2.3f + up * .35f, new Vector3(.08f, .7f, .08f), Quaternion.identity); }
            foreach (var s in new[] { -1f, 1f }) { var sp = c + z.side * s * 4f; z.mk.Box(dark, sp + up * 1f, new Vector3(1.2f, 2f, 1f), z.Rot); z.mk.Prism(WorldMaterials.Solid(new Color(.3f, .3f, .3f)), sp + up * 1.2f + z.front * .51f, z.front, .4f, .02f, 14); z.solid.Box(dark, sp + up * 1f, new Vector3(1.2f, 2f, 1f), z.Rot); }
            z.solid.Prism(steel, c, up, 2.4f, .8f, 12);
        }

        void BlackBox(Plot z)
        {
            var up = Vector3.up; var c = z.At(-3f, 0); var orange = WorldMaterials.Solid(new Color(1f, .45f, .1f), .6f);
            z.mk.Box(orange, c + up * 1f, new Vector3(2.4f, 1.6f, 3f), z.Rot * Quaternion.Euler(0, 0, 4f)); z.mk.Prism(orange, c + up * 1f - z.side * 1.6f, z.side, .7f, .4f, 12);
            for (var k = 0; k < 3; k++) z.mk.Box(WorldMaterials.Solid(new Color(.9f, .9f, .9f)), c + up * (1.5f - k * .3f) + z.front * 1.51f, new Vector3(1.8f, .12f, .02f), z.Rot);
            z.solid.Box(orange, c + up * 1f, new Vector3(2.6f, 2f, 3.2f), z.Rot);
        }

        void BlueCars(Plot z)
        {
            var up = Vector3.up;
            for (var k = 0; k < 5; k++)
            {
                var p = z.At(-3f, (k - 2f) * 2.6f); var dir = z.front; var rot = Quaternion.LookRotation(dir); var body = WorldMaterials.Solid(new Color(.2f, .4f, .8f), .8f);
                z.mk.Box(body, p + up * .55f, new Vector3(1.5f, .5f, 3.6f), rot); z.mk.Box(body, p + up * 1f - dir * .2f, new Vector3(1.3f, .45f, 1.8f), rot); z.mk.Box(WorldMaterials.Solid(new Color(.2f, .25f, .3f), .9f), p + up * 1f - dir * .2f, new Vector3(1.32f, .32f, 1.7f), rot);
                foreach (var s in new[] { -.75f, .75f }) foreach (var f in new[] { -1.1f, 1.1f }) z.mk.Prism(dark, p + Vector3.Cross(up, dir) * s + dir * f + up * .3f, Vector3.Cross(up, dir) * Mathf.Sign(s), .3f, .2f, 10);
                z.solid.Box(body, p + up * .7f, new Vector3(1.6f, 1.4f, 3.8f), rot);
            }
        }

        void Elephant(Plot z)
        {
            var up = Vector3.up; var c = z.At(-3f, 0); var grey = WorldMaterials.Solid(new Color(.55f, .55f, .58f));
            z.mk.Blob(grey, c + up * 2.2f, new Vector3(1.6f, 1.3f, 2.2f), z.ctx.Rng, 0f); z.mk.Blob(grey, c + up * 2.8f + z.front * 2f, Vector3.one * 1f, z.ctx.Rng, 0f);
            z.mk.Prism(grey, c + up * 2.4f + z.front * 2.8f, (z.front * .3f - up).normalized, .3f, 1.8f, 8, .5f);
            foreach (var s in new[] { -1f, 1f }) { z.mk.Box(grey, c + up * 2.9f + z.front * 1.7f + z.side * s * .9f, new Vector3(.1f, 1.1f, .9f), Quaternion.LookRotation(z.front) * Quaternion.Euler(0, s * 25f, 0)); foreach (var f in new[] { -1f, 1f }) z.mk.Prism(grey, c + z.side * s * .8f + z.front * f * 1.2f, up, .35f, 1.6f, 8); }
            z.solid.Box(grey, c + up * 1.8f, new Vector3(3.4f, 3.6f, 5f), z.Rot);
        }

        void Small(Plot z)
        {
            var h = 0; foreach (var ch in z.a.name) h = (h * 31 + ch) & 0x7fffffff;
            var p = z.At(-2.5f, 0);
            switch (h % 3)
            {
                case 0: BusShelter(z.ctx, z.mk, z.solid, p, z.front); break;
                case 1: PhoneBox(z.ctx, z.mk, z.solid, p, z.front); break;
                default:
                    var up = Vector3.up; z.mk.Box(stone, p + up * 1.3f, new Vector3(4f, 2.6f, 3.4f), z.Rot); z.solid.Box(stone, p + up * 1.3f, new Vector3(4f, 2.6f, 3.4f), z.Rot);
                    z.mk.GableRoof(slate, p + up * 2.6f, new Vector3(4f, 0, 4.6f), 1.2f, Quaternion.LookRotation(z.side)); z.mk.Box(dark, p + up * .9f + z.front * 1.71f, new Vector3(.8f, 1.8f, .04f), z.Rot);
                    z.mk.Box(GlowM(Lamp, 1.4f), p + up * 1.5f + z.front * 1.71f + z.side * 1.2f, new Vector3(.6f, .6f, .04f), z.Rot);
                    break;
            }
        }
    }
}
