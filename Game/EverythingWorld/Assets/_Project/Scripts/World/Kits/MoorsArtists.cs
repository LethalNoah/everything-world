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
                case "Radiohead": if (hasDistrict) District(z); else Small(z); break;
                case "Gorillaz": Windmill(z); break;
                case "Weezer": Diner(z); break;
                case "The Cranberries": RoundTower(z); break;
                case "Red Hot Chili Peppers": California(z); break;
                case "Hootie & The Blowfish": Blowfish(z); break;
                case "Violet Grohl": Couch(z); break;
                case "Oasis": Champagne(z); break;
                case "Muse": RedPlanet(z); break;
                case "Coldplay": YellowStars(z); break;
                case "Pulp": Disco(z); break;
                case "Train": Jupiter(z); break;
                case "Smash Mouth": ShootingStar(z); break;
                case "Rage Against The Machine": AmpWall(z); break;
                case "Blur": Trampoline(z); break;
                case "Black Box Recorder": BlackBox(z); break;
                case "Dishwalla": BlueCars(z); break;
                default: if (a.name.StartsWith("b") && a.name.EndsWith("a")) Elephant(z); else Small(z); break;
            }
            SongPieces(z);   // a light for every song (MoorsSongs)
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
            isl.Box(SongGlow(ClintEastwood, Lamp, 1.6f), up * 3f + Vector3.forward * 1.2f, new Vector3(.6f, .8f, .1f), Quaternion.identity);
            var t = isl.BuildSingle("Floating windmill island", life.transform, false).transform; life.island = t; life.islandBase = z.At(-6f, 0) + up * 9f; t.position = life.islandBase;
            var sails = new MeshKit(); for (var k = 0; k < 4; k++) sails.Box(WorldMaterials.Solid(new Color(.92f, .9f, .85f)), Quaternion.Euler(0, 0, k * 90f) * (Vector3.up * 2.4f), new Vector3(.9f, 4.4f, .08f), Quaternion.Euler(0, 0, k * 90f));
            var st = sails.BuildSingle("Windmill sails", t, false).transform; st.localPosition = up * 5.4f + Vector3.forward * 1.5f; life.sails = st;
            halos.Add(life.islandBase + up * 3f, 4f, Lamp, .2f);
            var shadow = z.At(-6f, 0); z.mk.Prism(WorldMaterials.Solid(new Color(.3f, .35f, .28f)), shadow + up * .02f, up, 4f, .02f, 12);
            // On Melancholy Hill: a small round green hill with a lone bench and a lamp on top
            var hill = z.At(-1f, 7.5f); z.mk.Blob(grass, hill, new Vector3(3.6f, 1.6f, 3.6f), z.ctx.Rng, .05f); z.solid.Prism(grass, hill - up * .2f, up, 3f, 1.4f, 10);
            var top = hill + up * 1.5f; z.mk.Box(wood, top + up * .45f, new Vector3(1.8f, .08f, .5f), z.Rot); z.mk.Box(wood, top + up * .75f - z.front * .22f, new Vector3(1.8f, .5f, .06f), z.Rot);
            foreach (var s in new[] { -.8f, .8f }) z.mk.Box(steel, top + up * .22f + z.side * s, new Vector3(.06f, .45f, .45f), z.Rot);
            StreetLamp(z.ctx, z.mk, top + z.side * 1.6f);
        }

        void Diner(Plot z)
        {
            var up = Vector3.up; var rot = z.Rot; var c = z.At(-5f, 0); var chrome = WorldMaterials.Solid(new Color(.85f, .86f, .88f), .95f); var teal = WorldMaterials.Solid(new Color(.35f, .75f, .72f), .6f);
            z.mk.Box(chrome, c + up * 1.8f, new Vector3(12f, 3.2f, 5f), rot); z.solid.Box(chrome, c + up * 1.8f, new Vector3(12f, 3.2f, 5f), rot);
            z.mk.Prism(chrome, c + up * 3.4f - z.side * 6f, z.side, 2.5f, 12f, 16); z.mk.Box(teal, c + up * 1f + z.front * 2.52f, new Vector3(12f, .6f, .05f), rot);
            for (var k = 0; k < 6; k++) z.mk.Box(SongGlow(SayItAintSo, new Color(1f, .9f, .65f), 1.6f), c + up * 2.1f + z.front * 2.52f + z.side * ((k - 2.5f) * 1.8f), new Vector3(1.4f, 1f, .03f), rot);
            var sign = c + up * 6.2f; z.mk.Box(dark, sign, new Vector3(5f, 1.2f, .3f), rot); z.mk.Box(SongGlow(BuddyHolly, new Color(1f, .3f, .5f), 2.6f), sign + z.front * .16f, new Vector3(4.6f, .8f, .02f), rot);
            ArtistBits.Label(z.labels, "DINER", sign + z.front * .18f, z.front, .35f, Color.white, TextAnchor.MiddleCenter);
            foreach (var s in new[] { -2f, 2f }) z.mk.Prism(chrome, c + up * 4.6f + z.side * s, up, .08f, 1f, 6);
            halos.Add(sign + z.front * .5f, 2.6f, new Color(1f, .3f, .5f), .55f);
            // Island In The Sun: a round pond with a tiny sandy island and one palm
            var pond = z.At(2.5f, 8f); z.mk.Prism(WorldMaterials.Solid(new Color(.25f, .5f, .6f), .9f), pond - up * .1f, up, 3.4f, .18f, 24); z.mk.Prism(stone, pond - up * .15f, up, 3.7f, .18f, 24);
            var sand = WorldMaterials.Solid(new Color(.9f, .82f, .6f)); z.mk.Prism(sand, pond, up, 1.2f, .3f, 14, .8f);
            var tp = pond + up * .3f; for (var k = 0; k < 4; k++) { var d = (up + z.side * .15f * k).normalized; z.mk.Prism(wood, tp, d, .14f, .9f, 6, .9f); tp += d * .9f; }
            for (var k = 0; k < 6; k++) { var a = Quaternion.AngleAxis(k * 60f, up) * z.front; soft.Box(WorldMaterials.Solid(new Color(.25f, .5f, .22f)), tp + a * .9f - up * .25f, new Vector3(.3f, .04f, 1.9f), Quaternion.LookRotation(a - up * .35f)); }
            z.solid.Prism(stone, pond - up * .2f, up, 3.7f, .6f, 12);
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
            foreach (var s in new[] { -.13f, .13f }) { z.mk.Blob(SongGlow(OnlyWannaBe, new Color(1f, .8f, .2f), 2.4f), owl + up * .6f + z.front * .28f + z.side * s, Vector3.one * .08f, z.ctx.Rng, 0f); z.mk.Cone(feather, owl + up * .82f + z.side * s * 1.4f, .07f, .2f, 4); }
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
            var ap = z.At(1f, 6f) + up * 1.4f; z.mk.Blob(SongGlow(Applefish, new Color(.95f, .2f, .15f), .7f), ap, new Vector3(.9f, .85f, .9f), z.ctx.Rng, 0f); z.mk.Prism(WorldMaterials.Solid(new Color(.75f, .15f, .15f), .7f), ap - z.front * .9f, -z.front, .4f, .6f, 4, 1.6f); z.mk.Box(WorldMaterials.Solid(new Color(.35f, .5f, .2f)), ap + up * 1f, new Vector3(.08f, .4f, .08f), Quaternion.identity); ArtistBits.PostCollider(z.root, z.At(1f, 6f), 2.4f, 1f);
        }

        // ---------------------------------------------------------------- everyone else
        void Champagne(Plot z)
        {
            var up = Vector3.up; var c = z.At(-3f, 0); var glass = WorldMaterials.Solid(new Color(.15f, .35f, .18f), .9f); var foil = WorldMaterials.Solid(new Color(.9f, .75f, .3f), .9f);
            z.mk.Prism(glass, c, up, 1.2f, 4f, 14); z.mk.Prism(glass, c + up * 4f, up, 1.2f, 1.5f, 14, .3f); z.mk.Prism(foil, c + up * 5.5f, up, .4f, 1.4f, 10);
            z.mk.Box(SongGlow(DontLookBack, new Color(1f, .9f, .65f), .9f), c + up * 2f + z.front * 1.18f, new Vector3(1.4f, 1.2f, .05f), z.Rot); halos.Add(c + up * 2f + z.front * 1.6f, 1.6f, new Color(1f, .9f, .65f), .4f);
            z.solid.Prism(glass, c, up, 1.2f, 7f, 10);
            var nova = c + up * 16f; var m = new Material(WorldMaterials.Glow(new Color(1f, .9f, .6f), 2f)) { name = "Supernova" }; life.nova = m; life.novaAt = nova;
            z.mk.Blob(m, nova, Vector3.one * .8f, z.ctx.Rng, 0f); for (var k = 0; k < 10; k++) z.mk.Box(m, nova + Quaternion.Euler(k * 36f, k * 72f, 0) * Vector3.forward * 1.4f, new Vector3(.12f, .12f, 1.8f), Quaternion.Euler(k * 36f, k * 72f, 0));
            halos.Add(nova, 4f, new Color(1f, .9f, .6f), .6f);
        }

        void RedPlanet(Plot z)
        {
            var up = Vector3.up; var p = z.At(-5f, 0) + up * 11f; var red = WorldMaterials.Solid(new Color(.75f, .3f, .2f)); var ring = SongGlow(Cydonia, new Color(1f, .7f, .5f), .8f);
            z.mk.Blob(red, p, Vector3.one * 3f, z.ctx.Rng, .05f);
            for (var k = 0; k < 40; k++) { var a0 = k * Mathf.PI / 20f; var a1 = (k + 1) * Mathf.PI / 20f; var r0 = Quaternion.Euler(20f, 0, 15f) * new Vector3(Mathf.Cos(a0), 0, Mathf.Sin(a0)) * 5f; var r1 = Quaternion.Euler(20f, 0, 15f) * new Vector3(Mathf.Cos(a1), 0, Mathf.Sin(a1)) * 5f; z.mk.Box(ring, p + (r0 + r1) / 2f, new Vector3(.8f, .05f, Vector3.Distance(r0, r1) + .02f), Quaternion.LookRotation(r1 - r0)); }
            var h = z.At(-1f, 3f); var coat = WorldMaterials.Solid(new Color(.2f, .18f, .16f));
            z.mk.Box(coat, h + up * 1.4f, new Vector3(.7f, .8f, 2f), Quaternion.LookRotation(z.side)); z.mk.Box(coat, h + up * 2.1f + z.side * 1.1f, new Vector3(.4f, 1f, .5f), Quaternion.LookRotation(z.side) * Quaternion.Euler(-30f, 0, 0));
            foreach (var s in new[] { -.25f, .25f }) foreach (var f in new[] { -.7f, .7f }) z.mk.Box(coat, h + up * .5f + z.front * s + z.side * f, new Vector3(.14f, 1f, .14f), Quaternion.identity);
            z.solid.Box(coat, h + up * 1.2f, new Vector3(.8f, 2.4f, 2.4f), Quaternion.LookRotation(z.side));
            halos.Add(p, 5f, new Color(1f, .5f, .3f), .25f);
        }

        void YellowStars(Plot z)
        {
            var up = Vector3.up; var yellow = SongGlow(Yellow, new Color(1f, .85f, .15f), 2.2f);
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
            for (var i = 0; i < 5; i++) for (var j = 0; j < 5; j++) z.mk.Box(SongGlow(Disco2000, tiles[(i + j * 2) % 4], 1.2f), c + z.side * (i - 2f) * 1.1f + z.front * (j - 2f) * 1.1f + up * .08f, new Vector3(1.05f, .14f, 1.05f), z.Rot);
            var tr = z.At(1.5f, 5f); var wire = WorldMaterials.Solid(new Color(.8f, .8f, .82f), .9f);
            z.mk.Box(wire, tr + up * .7f, new Vector3(.8f, .7f, 1.2f), z.Rot); foreach (var s in new[] { -.35f, .35f }) foreach (var f in new[] { -.5f, .5f }) z.mk.Prism(dark, tr + z.side * s + z.front * f + up * .1f, z.side, .1f, .06f, 8);
            z.mk.Box(wire, tr + up * 1.2f - z.front * .7f, new Vector3(.8f, .06f, .3f), z.Rot);
            halos.Add(c + up * .5f, 3.5f, new Color(1f, .5f, .8f), .3f);
        }

        void ShootingStar(Plot z)
        {
            var up = Vector3.up; var s = z.At(-3f, 0) + up * 5f; var star = SongGlow(AllStar, new Color(1f, .9f, .4f), 3f);
            for (var a = 0; a < 5; a++) z.mk.Box(star, s + Quaternion.AngleAxis(a * 72f, z.front) * up * .7f, new Vector3(.45f, 1.4f, .2f), Quaternion.AngleAxis(a * 72f, z.front) * z.Rot);
            for (var k = 0; k < 5; k++) z.mk.Box(SongGlow(Believer, new Color(1f, .7f, .3f), 1.6f - k * .25f), s - z.side * (1.6f + k * 1.4f) + up * (-k * .4f), new Vector3(1.4f, .25f - k * .03f, .1f), Quaternion.LookRotation(z.front) * Quaternion.Euler(0, 0, -15f));
            z.mk.Prism(steel, z.At(-3f, 0), up, .1f, 4.4f, 6); ArtistBits.PostCollider(z.root, z.At(-3f, 0), 4.4f, .15f);
            halos.Add(s, 2.4f, new Color(1f, .9f, .4f), .6f);
        }

        void Trampoline(Plot z)
        {
            var up = Vector3.up; var c = z.At(-3f, 0);
            z.mk.Prism(steel, c + up * .7f, up, 2.4f, .1f, 20); z.mk.Prism(dark, c + up * .75f, up, 2.2f, .03f, 20);
            for (var k = 0; k < 6; k++) { var a = k * 60f * Mathf.Deg2Rad; z.mk.Box(steel, c + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * 2.3f + up * .35f, new Vector3(.08f, .7f, .08f), Quaternion.identity); }
            foreach (var s in new[] { -1f, 1f }) { var sp = c + z.side * s * 4f; z.mk.Box(dark, sp + up * 1f, new Vector3(1.2f, 2f, 1f), z.Rot); z.mk.Prism(SongGlow(Song2, new Color(.35f, .6f, 1f), 1.4f), sp + up * 1.2f + z.front * .51f, z.front, .4f, .02f, 14); halos.Add(sp + up * 1.2f + z.front * .8f, 1.2f, new Color(.35f, .6f, 1f), .5f); z.solid.Box(dark, sp + up * 1f, new Vector3(1.2f, 2f, 1f), z.Rot); }
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
                z.mk.Box(body, p + up * .55f, new Vector3(1.5f, .5f, 3.6f), rot); z.mk.Box(body, p + up * 1f - dir * .2f, new Vector3(1.3f, .45f, 1.8f), rot); z.mk.Box(WorldMaterials.Solid(new Color(.2f, .25f, .3f), .9f), p + up * 1f - dir * .2f, new Vector3(1.34f, .32f, 1.7f), rot);
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

        // ---------------------------------------------------------------- the Red Hot Chili Peppers' corner
        /// A California corner on the moor: a glowing turquoise pool on a tiled deck under tall palms strung with lights,
        /// sun loungers, a red eight-armed neon star turning on a tall pole, a little carousel turning (Soul to Squeeze),
        /// and a giant red chili pepper on a plinth. "Californication" warms the sky to sunset and the pool glows; "Can't
        /// Stop" spins the star hard; "Soul to Squeeze" sets the carousel and its lights going.
        void California(Plot z)
        {
            var up = Vector3.up; var rot = z.Rot; var c = z.At(-5f, 0); var tile = WorldMaterials.Solid(new Color(.93f, .9f, .84f)); var rimM = WorldMaterials.Solid(new Color(.75f, .85f, .88f), .6f);
            z.mk.Box(tile, c + up * .1f, new Vector3(17f, .4f, 11f), rot); z.solid.Box(tile, c + up * .1f, new Vector3(17f, .4f, 11f), rot);
            foreach (var s in new[] { -1f, 1f }) { z.mk.Box(rimM, c + z.front * s * 3.2f + up * .5f, new Vector3(10.8f, .4f, .4f), rot); z.mk.Box(rimM, c + z.side * s * 5.2f + up * .5f, new Vector3(.4f, .4f, 6f), rot); }
            var water = new Material(WorldMaterials.Glow(new Color(.2f, .72f, .9f), .8f)) { name = "California pool" }; life.rhcpPool = water;
            z.mk.Box(water, c + up * .55f, new Vector3(10f, .1f, 6f), rot); z.solid.Box(rimM, c + up * .5f, new Vector3(10.8f, .5f, 6.4f), rot);
            halos.Add(c + up * 1f, 6f, new Color(.2f, .72f, .9f), .25f);
            var ladder = c + z.side * 4.6f + z.front * 3.3f; foreach (var s in new[] { -.3f, .3f }) { z.mk.Box(steel, ladder + z.side * s + up * 1f, new Vector3(.06f, .9f, .06f), rot); z.mk.Box(steel, ladder + z.side * s + up * 1.45f - z.front * .2f, new Vector3(.06f, .06f, .45f), rot); }
            // palms at the deck corners, strung with warm lights
            var lights = new Material(WorldMaterials.Glow(new Color(1f, .78f, .45f), 1.2f)) { name = "California lights" }; life.rhcpLights = lights;
            var bark = WorldMaterials.Solid(new Color(.45f, .36f, .26f)); var frond = WorldMaterials.Solid(new Color(.22f, .45f, .2f));
            var tops = new List<Vector3>();
            foreach (var sx in new[] { -1f, 1f }) foreach (var sf in new[] { -1f, 1f })
            {
                var foot = c + z.side * sx * 7.6f + z.front * sf * 4.6f + up * .3f; var lean = (up * 4f + z.side * sx * .6f + z.front * sf * .3f).normalized; var p = foot;
                for (var k = 0; k < 5; k++) { var d = (lean + z.side * sx * k * .04f).normalized; z.mk.Prism(bark, p, d, .26f - k * .02f, 1.9f, 7, .92f); p += d * 1.9f; }
                for (var k = 0; k < 8; k++) { var a = Quaternion.AngleAxis(k * 45f, up) * z.front; soft.Box(frond, p + a * 1.5f - up * .35f, new Vector3(.5f, .06f, 3.2f), Quaternion.LookRotation(a - up * .35f)); }
                ArtistBits.PostCollider(z.root, foot, 4f, .3f); tops.Add(p - up * .6f);
            }
            int[] ring = { 0, 1, 3, 2 };
            for (var i = 0; i < 4; i++)
            {
                var a = tops[ring[i]]; var b = tops[ring[(i + 1) % 4]];
                for (var k = 1; k < 14; k++) { var tt = k / 14f; var p = Vector3.Lerp(a, b, tt) - up * Mathf.Sin(tt * Mathf.PI) * 1.2f; z.mk.Blob(lights, p, Vector3.one * .12f, z.ctx.Rng, 0f); }
            }
            // sun loungers
            for (var k = 0; k < 3; k++)
            {
                var lp = c - z.front * 4.4f + z.side * ((k - 1f) * 2.6f) + up * .3f; var lr = Quaternion.LookRotation(z.front);
                z.mk.Box(WorldMaterials.Solid(new Color(.95f, .95f, .95f)), lp + up * .35f, new Vector3(.75f, .1f, 1.5f), lr); z.mk.Box(WorldMaterials.Solid(new Color(.95f, .95f, .95f)), lp + up * .6f - z.front * .95f, new Vector3(.75f, .1f, .8f), lr * Quaternion.Euler(-40f, 0, 0));
                z.mk.Box(WorldMaterials.Solid(new Color(.9f, .3f, .25f)), lp + up * .42f, new Vector3(.7f, .04f, 1.4f), lr); z.solid.Box(tile, lp + up * .45f, new Vector3(.8f, .9f, 2.2f), lr);
            }
            // the red star turning on its pole
            var pole = z.At(-12.5f, 0); z.mk.Prism(steel, pole, up, .14f, 9f, 8); ArtistBits.PostCollider(z.root, pole, 9f, .2f);
            var red = GlowM(new Color(1f, .12f, .1f), 2.6f); var sk = new MeshKit();
            for (var k = 0; k < 8; k++) { var d = Quaternion.AngleAxis(k * 45f, Vector3.forward) * Vector3.up; sk.Box(red, d * 1.2f, new Vector3(.45f, 2.4f, .2f), Quaternion.AngleAxis(k * 45f, Vector3.forward)); }
            var star = sk.BuildSingle("California star", life.transform, false).transform; var starAt = pole + up * 10.4f; life.rhcpStarRot = Quaternion.LookRotation(z.front); star.SetPositionAndRotation(starAt, life.rhcpStarRot); life.rhcpStar = star;
            halos.Add(starAt, 4.5f, new Color(1f, .12f, .1f), .5f);
            // a little carousel (MoorsLife turns it)
            var cAt = z.At(-4f, 12f); var cm = new MeshKit(); var stripeA = WorldMaterials.Solid(new Color(.9f, .2f, .2f)); var stripeB = WorldMaterials.Solid(new Color(.96f, .94f, .88f)); var gold = WorldMaterials.Solid(new Color(.9f, .72f, .3f), .8f);
            cm.Prism(stripeB, Vector3.zero, up, 2.6f, .35f, 16); cm.Prism(gold, Vector3.zero, up, .15f, 3.4f, 8);
            for (var k = 0; k < 12; k++) { float a0 = k * 30f * Mathf.Deg2Rad, a1 = (k + 1) * 30f * Mathf.Deg2Rad; Vector3 b0 = new Vector3(Mathf.Cos(a0), 0, Mathf.Sin(a0)) * 2.9f + up * 3.2f, b1 = new Vector3(Mathf.Cos(a1), 0, Mathf.Sin(a1)) * 2.9f + up * 3.2f; cm.Tri(k % 2 == 0 ? stripeA : stripeB, up * 4.6f, b1, b0, up * 3.6f); cm.Tri(k % 2 == 0 ? stripeA : stripeB, up * 4.6f, b0, b1, up * 5.2f); cm.Box(lights, (b0 + b1) / 2f - up * .05f, new Vector3(.12f, .12f, .12f), Quaternion.identity); }
            for (var k = 0; k < 6; k++)
            {
                var a = k * 60f * Mathf.Deg2Rad; var hp = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * 1.9f; var hd = new Vector3(-Mathf.Sin(a), 0, Mathf.Cos(a));
                cm.Prism(gold, hp, up, .04f, 3.2f, 6); var horse = k % 2 == 0 ? stripeB : WorldMaterials.Solid(new Color(.55f, .35f, .25f));
                cm.Box(horse, hp + up * 1.3f, new Vector3(.3f, .45f, 1f), Quaternion.LookRotation(hd)); cm.Box(horse, hp + up * 1.65f + hd * .5f, new Vector3(.22f, .45f, .25f), Quaternion.LookRotation(hd + up));
            }
            var ct = cm.BuildSingle("Carousel", life.transform, false).transform; ct.position = cAt; life.carousel.Add(ct); life.carouselAt = cAt; ArtistBits.PostCollider(z.root, cAt, 4f, 2.8f);
            halos.Add(cAt + up * 3.2f, 4f, new Color(1f, .78f, .45f), .3f);
            // a giant red chili pepper on a plinth
            var pp = z.At(-3f, -12f); z.mk.Box(concrete, pp + up * .5f, new Vector3(3.4f, 1f, 1.8f), Quaternion.LookRotation(z.side)); z.solid.Box(concrete, pp + up * 1.2f, new Vector3(3.4f, 2.4f, 1.8f), Quaternion.LookRotation(z.side));
            var chili = WorldMaterials.Solid(new Color(.85f, .06f, .05f), .85f); var stemM = WorldMaterials.Solid(new Color(.2f, .5f, .15f));
            var prev = pp + up * 1.6f - z.side * 1.4f; var r = .55f;
            for (var k = 1; k <= 7; k++) { var next = pp + up * (1.6f + Mathf.Sin(k * .45f) * .5f) - z.side * 1.4f + z.side * k * .45f - up * k * k * .015f; var d = next - prev; z.mk.Prism(chili, prev, d.normalized, r, d.magnitude + .05f, 10, (r - .06f) / r); prev = next; r -= .06f; }
            z.mk.Cone(chili, prev, r + .02f, .4f, 8);
            var stemAt = pp + up * 1.6f - z.side * 1.4f; z.mk.Prism(stemM, stemAt, -z.side, .5f, .15f, 10); z.mk.Prism(stemM, stemAt - z.side * .15f, (-z.side + up * .5f).normalized, .08f, .6f, 6);
            ArtistBits.PointLight(z.root, c + up * 5f, new Color(1f, .7f, .5f), 22f, 1.4f);
        }

        // ---------------------------------------------------------------- everyone else, each their own
        /// Rage Against The Machine: a wall of amp cabinets with red-lit heads, a microphone out front, burning oil drums
        /// and a red board that says WAKE UP.
        void AmpWall(Plot z)
        {
            var up = Vector3.up; var c = z.At(-3f, 0); var rot = z.Rot; var cab = WorldMaterials.Solid(new Color(.1f, .1f, .11f)); var grille = WorldMaterials.Solid(new Color(.22f, .22f, .23f)); var trim = WorldMaterials.Solid(new Color(.75f, .75f, .72f), .7f);
            for (var k = 0; k < 6; k++)
            {
                var p = c + z.side * ((k % 3) - 1f) * 1.5f + up * ((k / 3) * 1.15f + .58f);
                z.mk.Box(cab, p, new Vector3(1.45f, 1.1f, .9f), rot); z.mk.Box(grille, p + z.front * .46f, new Vector3(1.25f, .9f, .02f), rot); z.mk.Box(trim, p + z.front * .47f + up * .38f + z.side * .45f, new Vector3(.3f, .08f, .02f), rot);
            }
            foreach (var s in new[] { -.75f, .75f }) { var h = c + z.side * s + up * 2.6f; z.mk.Box(cab, h, new Vector3(1.4f, .5f, .7f), rot); for (var k = 0; k < 4; k++) z.mk.Blob(SongGlow(WakeUp, new Color(1f, .1f, .08f), 2.6f), h + z.front * .36f + z.side * ((k - 1.5f) * .25f), Vector3.one * .05f, z.ctx.Rng, 0f); }
            z.solid.Box(cab, c + up * 1.4f, new Vector3(4.6f, 2.9f, 1f), rot);
            var board = c - z.front * .6f + up * 4.2f; z.mk.Box(redPaint, board, new Vector3(4.4f, 1.1f, .12f), rot); foreach (var s in new[] { -2f, 2f }) z.mk.Box(steel, board + z.side * s - up * 1.6f, new Vector3(.1f, 2.2f, .1f), rot);
            ArtistBits.Label(z.labels, "WAKE UP", board + z.front * .07f, z.front, .55f, Color.white, TextAnchor.MiddleCenter);
            var mic = z.At(0f, 0); z.mk.Prism(steel, mic, up, .25f, .04f, 10); z.mk.Prism(steel, mic, up, .03f, 1.5f, 6); z.mk.Blob(dark, mic + up * 1.55f, new Vector3(.07f, .07f, .1f), z.ctx.Rng, 0f); ArtistBits.PostCollider(z.root, mic, 1.6f, .2f);
            foreach (var s in new[] { -3.4f, 3.4f })
            {
                var drum = z.At(-1f, s); var rust = WorldMaterials.Solid(new Color(.35f, .2f, .12f)); z.mk.Prism(rust, drum, up, .45f, 1f, 12); ArtistBits.PostCollider(z.root, drum, 1f, .5f);
                var flame = GlowM(new Color(1f, .5f, .1f), 3f); for (var k = 0; k < 4; k++) z.mk.Cone(flame, drum + up * 1f + new Vector3(z.ctx.Range(-.2f, .2f), 0, z.ctx.Range(-.2f, .2f)), .2f, z.ctx.Range(.5f, .9f), 5);
                halos.Add(drum + up * 1.4f, 2.4f, new Color(1f, .5f, .1f), .7f);
            }
        }

        /// Train: Jupiter hanging over the moor, banded and smooth, with glowing drops falling from it (MoorsLife).
        void Jupiter(Plot z)
        {
            var up = Vector3.up; var p = z.At(0f, 0) + up * 13f;
            Color[] bands = { new Color(.86f, .74f, .56f), new Color(.78f, .6f, .44f), new Color(.93f, .87f, .75f), new Color(.74f, .52f, .36f), new Color(.9f, .8f, .64f), new Color(.82f, .66f, .5f), new Color(.95f, .9f, .8f) };
            const int n = 14; const float R = 3.2f;
            for (var k = 0; k < n; k++)
            {
                float y0 = -R + k * 2f * R / n, y1 = y0 + 2f * R / n; float r0 = Mathf.Sqrt(Mathf.Max(.02f, R * R - y0 * y0)), r1 = Mathf.Sqrt(Mathf.Max(.02f, R * R - y1 * y1));
                z.mk.Prism(WorldMaterials.Solid(bands[k % bands.Length], .3f), p + up * y0, up, r0, y1 - y0, 28, r1 / r0);
            }
            z.mk.Blob(WorldMaterials.Solid(new Color(.8f, .32f, .2f)), p + z.front * (R * .93f) - up * .8f, new Vector3(.7f, .35f, .2f), z.ctx.Rng, 0f);
            halos.Add(p, 6f, new Color(1f, .85f, .6f), .2f);
            life.jupiterAt = p;
        }

        /// The Cranberries: a tall stone round tower with a pointed cap and one lit window high up, candles in an alcove at
        /// its foot, a dream cloud glowing over it, cranberry bushes round it.
        void RoundTower(Plot z)
        {
            var up = Vector3.up; var c = z.At(-4f, 0);
            for (var k = 0; k < 5; k++) z.mk.Prism(stone, c + up * k * 2.6f, up, 2.4f - k * .08f, 2.6f, 16, (2.4f - (k + 1) * .08f) / (2.4f - k * .08f));
            z.mk.Cone(slate, c + up * 13f, 2.05f, 3.6f, 16);
            z.mk.Box(SongGlow(Linger, Lamp, 1.8f), c + up * 10.4f + z.front * 2.04f, new Vector3(.5f, .9f, .1f), z.Rot);
            z.mk.Box(dark, c + up * 1.1f + z.front * 2.32f, new Vector3(1.4f, 2f, .1f), z.Rot); z.mk.Box(SongGlow(Zombie, new Color(1f, .2f, .1f), 1.4f), c + up * 1.1f + z.front * 2.38f, new Vector3(.08f, 1.8f, .02f), z.Rot);   // a red line of light at the door's edge
            for (var k = 0; k < 6; k++) { var cp = c + z.front * 2.45f + z.side * ((k - 2.5f) * .2f) + up * (.3f + (k % 2) * .25f); z.mk.Prism(white, cp, up, .05f, .3f, 6); z.mk.Blob(SongGlow(AveMaria, new Color(1f, .75f, .35f), 3f), cp + up * .36f, Vector3.one * .04f, z.ctx.Rng, 0f); }
            halos.Add(c + z.front * 2.6f + up * .8f, 1.6f, new Color(1f, .75f, .35f), .6f);
            z.solid.Prism(stone, c, up, 2.4f, 13f, 12);
            var cloud = c + up * 21f; var dream = SongGlow(Dreams, new Color(.82f, .86f, 1f), 1.3f);
            for (var k = 0; k < 8; k++) z.mk.Blob(dream, cloud + new Vector3(Mathf.Cos(k) * 2.6f, Mathf.Sin(k * 2f) * .6f, Mathf.Sin(k) * 2.6f), new Vector3(2.2f, 1.3f, 1.9f), z.ctx.Rng, .2f);
            halos.Add(cloud, 6f, new Color(.82f, .86f, 1f), .45f);
            var berry = WorldMaterials.Solid(new Color(.75f, .1f, .2f)); var bush = WorldMaterials.Solid(new Color(.25f, .38f, .22f));
            for (var k = 0; k < 6; k++) { var bp = z.At(-1f + (k % 2) * 2f, -7f + k * 2.8f); soft.Blob(bush, bp + up * .5f, new Vector3(1f, .7f, 1f), z.ctx.Rng, .3f); for (var b = 0; b < 8; b++) z.mk.Blob(berry, bp + up * .5f + new Vector3(z.ctx.Range(-.8f, .8f), z.ctx.Range(.1f, .6f), z.ctx.Range(-.8f, .8f)), Vector3.one * .1f, z.ctx.Rng, 0f); }
        }

        /// The rest, each a thing from their song.
        void Small(Plot z)
        {
            var up = Vector3.up; var p = z.At(-2.5f, 0); var rot = z.Rot; var n = z.a.name;
            if (n == "311")
            {   // Sunset in July: a huge half sun going down behind a porch swing
                var sun = SongGlow(SunsetInJuly, new Color(1f, .5f, .15f), 2.2f); z.mk.Prism(sun, p - z.front * 3f - z.front * .15f, z.front, 3.4f, .3f, 32); halos.Add(p - z.front * 3f + up * 1.5f, 4.5f, new Color(1f, .5f, .15f), .4f);
                foreach (var s in new[] { -1.4f, 1.4f }) { z.mk.Box(wood, p + z.side * s + up * 1.2f - z.front * .4f, new Vector3(.12f, 2.6f, .12f), rot * Quaternion.Euler(12f, 0, 0)); z.mk.Box(wood, p + z.side * s + up * 1.2f + z.front * .4f, new Vector3(.12f, 2.6f, .12f), rot * Quaternion.Euler(-12f, 0, 0)); }
                z.mk.Box(wood, p + up * 2.45f, new Vector3(3f, .12f, .12f), rot); z.mk.Box(wood, p + up * .7f, new Vector3(2f, .1f, .6f), rot); z.mk.Box(wood, p + up * 1.05f - z.front * .28f, new Vector3(2f, .6f, .08f), rot);
                foreach (var s in new[] { -.9f, .9f }) z.mk.Box(steel, p + z.side * s + up * 1.6f, new Vector3(.03f, 1.7f, .03f), rot);
                z.solid.Box(wood, p + up * 1.2f, new Vector3(3f, 2.4f, 1.2f), rot);
            }
            else if (n == "Black Box Recorder")
            {   // a flight recorder and a little school desk and chair (Child Psychology)
                BlackBox(z); var d = z.At(-1f, 4f); var desk = WorldMaterials.Solid(new Color(.55f, .42f, .3f));
                z.mk.Box(desk, d + up * .75f, new Vector3(1.1f, .06f, .7f), rot); foreach (var s in new[] { -.5f, .5f }) foreach (var f in new[] { -.3f, .3f }) z.mk.Box(steel, d + up * .37f + z.side * s + z.front * f, new Vector3(.04f, .74f, .04f), rot);
                var ch = d + z.front * .7f; z.mk.Box(desk, ch + up * .45f, new Vector3(.45f, .05f, .45f), rot); z.mk.Box(desk, ch + up * .75f + z.front * .22f, new Vector3(.45f, .5f, .04f), rot); z.solid.Box(desk, d + up * .5f, new Vector3(1.2f, 1f, 1.6f), rot);
            }
            else if (n == "Everclear")
            {   // So Much for the Afterglow: arches of neon, each fainter than the last, over a bench
                for (var k = 0; k < 3; k++)
                {
                    var m = SongGlow(Afterglow, new Color(1f, .62f, .25f), 2.4f - k * .8f); var ac = p - z.front * k * 1.6f; var R = 3.4f;
                    for (var s = 0; s < 18; s++) { float a0 = s * Mathf.PI / 18f, a1 = (s + 1) * Mathf.PI / 18f; var q0 = ac + z.side * Mathf.Cos(a0) * R + up * Mathf.Sin(a0) * R; var q1 = ac + z.side * Mathf.Cos(a1) * R + up * Mathf.Sin(a1) * R; z.mk.Box(m, (q0 + q1) / 2f, new Vector3(.18f, .18f, Vector3.Distance(q0, q1) + .02f), Quaternion.LookRotation(q1 - q0)); }
                    foreach (var s in new[] { -1f, 1f }) ArtistBits.PostCollider(z.root, ac + z.side * s * R, 1f, .15f);
                }
                halos.Add(p + up * 3f, 4f, new Color(1f, .62f, .25f), .35f);
                z.mk.Box(wood, p - z.front * 1.6f + up * .45f, new Vector3(2.2f, .08f, .5f), rot); z.solid.Box(wood, p - z.front * 1.6f + up * .3f, new Vector3(2.2f, .6f, .5f), rot);
            }
            else if (n == "For Opal")
            {   // Fatal Attraction: a giant horseshoe magnet with bits of iron flying to it
                var red = WorldMaterials.Solid(new Color(.85f, .1f, .1f), .6f); var tip = SongGlow(FatalAttraction, new Color(.75f, .85f, 1f), 1.4f); var m = p + up * 1.2f;
                z.mk.Box(concrete, p + up * .4f, new Vector3(3.6f, .8f, 1.4f), rot);
                for (var s = 0; s < 12; s++) { float a0 = Mathf.PI + s * Mathf.PI / 12f, a1 = Mathf.PI + (s + 1) * Mathf.PI / 12f; var q0 = m + up * .8f + z.side * Mathf.Cos(a0) * 1.2f - up * Mathf.Sin(a0) * 1.2f; var q1 = m + up * .8f + z.side * Mathf.Cos(a1) * 1.2f - up * Mathf.Sin(a1) * 1.2f; z.mk.Box(red, (q0 + q1) / 2f, new Vector3(.5f, .5f, Vector3.Distance(q0, q1) + .05f), Quaternion.LookRotation(q1 - q0, z.front)); }
                foreach (var s in new[] { -1.2f, 1.2f }) { z.mk.Box(red, m + up * 1.4f + z.side * s, new Vector3(.5f, 1.3f, .5f), rot); z.mk.Box(tip, m + up * 2.35f + z.side * s, new Vector3(.52f, .6f, .52f), rot); }
                for (var k = 0; k < 9; k++) z.mk.Box(steel, m + up * (2.8f + z.ctx.Range(0f, 1.6f)) + z.side * z.ctx.Range(-1.6f, 1.6f) + z.front * z.ctx.Range(-.6f, .6f), new Vector3(.08f, .08f, .4f), Quaternion.Euler(z.ctx.Range(0, 360f), z.ctx.Range(0, 360f), 0));
                z.solid.Box(red, p + up * 1.8f, new Vector3(3.6f, 3.6f, 1.4f), rot);
            }
            else if (n == "Incubus")
            {   // Drive: an old open-topped car on a strip of road, headlights on
                var road = WorldMaterials.Solid(new Color(.25f, .25f, .27f)); z.mk.Box(road, p + up * .04f, new Vector3(3.6f, .08f, 9f), rot);
                var body = WorldMaterials.Solid(new Color(.15f, .35f, .45f), .8f); var cp = p + z.front * .5f;
                z.mk.Box(body, cp + up * .7f, new Vector3(1.9f, .6f, 4.4f), rot); z.mk.Box(body, cp + up * 1.05f - z.front * 1.2f, new Vector3(1.9f, .2f, 1.6f), rot); z.mk.Box(WorldMaterials.Solid(new Color(.6f, .7f, .75f), .9f), cp + up * 1.3f + z.front * .3f, new Vector3(1.7f, .45f, .06f), rot * Quaternion.Euler(-20f, 0, 0));
                z.mk.Box(WorldMaterials.Solid(new Color(.5f, .3f, .2f)), cp + up * .95f - z.front * .5f, new Vector3(1.6f, .3f, 1.2f), rot);
                foreach (var s in new[] { -.95f, .95f }) foreach (var f in new[] { -1.4f, 1.4f }) z.mk.Prism(dark, cp + z.side * s + z.front * f + up * .38f, z.side * Mathf.Sign(s), .38f, .22f, 12);
                foreach (var s in new[] { -.6f, .6f }) z.mk.Prism(SongGlow(DriveSong, new Color(1f, .95f, .8f), 3f), cp + z.side * s + z.front * 2.2f + up * .8f, z.front, .16f, .05f, 10);
                halos.Add(cp + z.front * 2.8f + up * .8f, 2f, new Color(1f, .95f, .8f), .6f);
                z.solid.Box(body, cp + up * .8f, new Vector3(2f, 1.4f, 4.6f), rot);
            }
            else if (n == "Lenny Kravitz")
            {   // Fly Away: a glowing paper plane taking off from a tilted pole, birds on a wire
                var pole = p; z.mk.Box(steel, pole + up * 2.4f, new Vector3(.12f, 4.8f, .12f), rot * Quaternion.Euler(-15f, 0, 0)); ArtistBits.PostCollider(z.root, pole, 4.6f, .2f);
                var pl = pole + up * 5f + z.front * 1.2f; var paper = SongGlow(FlyAway, new Color(.95f, .97f, 1f), .9f); var fwd = (z.front + up * .35f).normalized; var sd = z.side;
                z.mk.Tri(paper, pl + fwd * 1.6f, pl - fwd * 1f + sd * 1.1f, pl - fwd * .7f, pl + up); z.mk.Tri(paper, pl + fwd * 1.6f, pl - fwd * .7f, pl - fwd * 1f - sd * 1.1f, pl + up);
                z.mk.Tri(paper, pl + fwd * 1.6f, pl - fwd * 1f + sd * 1.1f, pl - fwd * .7f, pl - up); z.mk.Tri(paper, pl + fwd * 1.6f, pl - fwd * .7f, pl - fwd * 1f - sd * 1.1f, pl - up);
                halos.Add(pl, 2f, new Color(.95f, .97f, 1f), .4f);
                var w0 = z.At(-4f, -4f) + up * 3.2f; var w1 = z.At(-4f, 4f) + up * 3.2f; foreach (var w in new[] { w0, w1 }) { z.mk.Prism(wood, w - up * 3.4f, up, .1f, 3.5f, 6); ArtistBits.PostCollider(z.root, w - up * 3.2f, 3.4f, .15f); }
                z.mk.Box(dark, (w0 + w1) / 2f, new Vector3(.03f, .03f, 8f), Quaternion.LookRotation(w1 - w0));
                for (var k = 0; k < 5; k++) { var b = Vector3.Lerp(w0, w1, .15f + k * .17f) + up * .15f; z.mk.Blob(WorldMaterials.Solid(new Color(.95f, .95f, .93f)), b, new Vector3(.12f, .12f, .18f), z.ctx.Rng, 0f); }
            }
            else if (n == "New Radicals")
            {   // You Get What You Give: a giant bucket hat on a plinth
                z.mk.Prism(concrete, p, up, 1.4f, 1f, 16); var hat = WorldMaterials.Solid(new Color(.55f, .62f, .7f), .2f);
                z.mk.Prism(hat, p + up * 1f, up, 1.9f, .14f, 24, .75f); z.mk.Prism(hat, p + up * 1.1f, up, 1.25f, 1.1f, 24, .8f); z.mk.Prism(hat, p + up * 2.2f, up, 1f, .08f, 24, .5f);
                z.mk.Prism(SongGlow(YouGetWhatYouGive, new Color(1f, .55f, .2f), 1.2f), p + up * 1.15f, up, 1.27f, .2f, 24);
                z.solid.Prism(concrete, p, up, 2f, 2.4f, 12);
            }
            else if (n == "No Doubt")
            {   // Just a Girl: a dressing-room mirror ringed with bulbs, and a pink stool
                var frame = WorldMaterials.Solid(new Color(.95f, .5f, .7f), .5f); var mc = p + up * 1.9f;
                z.mk.Box(frame, mc, new Vector3(2.6f, 2.2f, .2f), rot); z.mk.Box(WorldMaterials.Solid(new Color(.75f, .82f, .88f), .95f), mc + z.front * .11f, new Vector3(2.1f, 1.7f, .02f), rot);
                for (var k = 0; k < 16; k++) { var a = k / 16f; Vector3 q = a < .25f ? new Vector3(-1.2f + a * 4f * 2.4f, 1f, 0) : a < .5f ? new Vector3(1.2f, 1f - (a - .25f) * 4f * 2f, 0) : a < .75f ? new Vector3(1.2f - (a - .5f) * 4f * 2.4f, -1f, 0) : new Vector3(-1.2f, -1f + (a - .75f) * 4f * 2f, 0); z.mk.Blob(SongGlow(JustAGirl, new Color(1f, .92f, .7f), 2.6f), mc + z.side * q.x + up * q.y + z.front * .14f, Vector3.one * .09f, z.ctx.Rng, 0f); }
                halos.Add(mc + z.front * .5f, 2.4f, new Color(1f, .9f, .7f), .4f);
                foreach (var s in new[] { -1f, 1f }) z.mk.Box(frame, p + z.side * s * 1f + up * .4f, new Vector3(.1f, .8f, .5f), rot);
                var st = p + z.front * 1.4f; z.mk.Prism(frame, st, up, .3f, .6f, 12); z.solid.Box(frame, p + up * 1.5f, new Vector3(2.6f, 3f, .6f), rot);
            }
            else if (n == "OUT IN FRONT")
            {   // Medicine: a giant two-tone capsule lying on a plinth
                z.mk.Box(concrete, p + up * .4f, new Vector3(3.8f, .8f, 1.6f), rot);
                var a = WorldMaterials.Solid(new Color(.9f, .2f, .3f), .7f); var b = WorldMaterials.Solid(new Color(.95f, .95f, .93f), .7f); var cp = p + up * 1.4f;
                z.mk.Prism(a, cp, -z.side, .6f, 1.2f, 18); z.mk.Prism(b, cp, z.side, .6f, 1.2f, 18);
                z.mk.Blob(a, cp - z.side * 1.2f, Vector3.one * .6f, z.ctx.Rng, 0f); z.mk.Blob(b, cp + z.side * 1.2f, Vector3.one * .6f, z.ctx.Rng, 0f);
                z.mk.Prism(SongGlow(Medicine, new Color(.5f, 1f, .8f), 2f), cp - z.side * .06f, z.side, .63f, .12f, 18); halos.Add(cp, 1.6f, new Color(.5f, 1f, .8f), .4f);
                z.solid.Box(concrete, p + up * 1f, new Vector3(3.8f, 2f, 1.6f), rot);
            }
            else if (n == "Tonic")
            {   // If You Could Only See: a giant pair of spectacles on a stand
                var rim = WorldMaterials.Solid(new Color(.1f, .1f, .1f), .6f); var gc = p + up * 2.6f;
                foreach (var s in new[] { -1f, 1f }) for (var k = 0; k < 20; k++) { float a0 = k * Mathf.PI / 10f, a1 = (k + 1) * Mathf.PI / 10f; var cc = gc + z.side * s * 1.1f; var q0 = cc + z.side * Mathf.Cos(a0) * .85f + up * Mathf.Sin(a0) * .7f; var q1 = cc + z.side * Mathf.Cos(a1) * .85f + up * Mathf.Sin(a1) * .7f; z.mk.Box(rim, (q0 + q1) / 2f, new Vector3(.12f, .12f, Vector3.Distance(q0, q1) + .03f), Quaternion.LookRotation(q1 - q0)); }
                foreach (var s in new[] { -1f, 1f }) Glass(z.root, gc + z.side * s * 1.1f, new Vector3(1.5f, 1.2f, .04f), new Color(.7f, .85f, 1f, .2f), rot);
                z.mk.Box(rim, gc + up * .25f, new Vector3(.5f, .1f, .1f), rot); z.mk.Blob(SongGlow(IfYouCouldOnlySee, new Color(.6f, .85f, 1f), 2.4f), gc + up * .25f + z.front * .1f, Vector3.one * .1f, z.ctx.Rng, 0f); halos.Add(gc, 2.4f, new Color(.6f, .85f, 1f), .35f); z.mk.Prism(steel, p, up, .1f, 1.9f, 6); z.mk.Prism(concrete, p, up, .6f, .3f, 12); ArtistBits.PostCollider(z.root, p, 2f, .5f);
            }
            else
            {   // a little stone hut with a lit window
                z.mk.Box(stone, p + up * 1.3f, new Vector3(4f, 2.6f, 3.4f), rot); z.solid.Box(stone, p + up * 1.3f, new Vector3(4f, 2.6f, 3.4f), rot);
                z.mk.GableRoof(slate, p + up * 2.6f, new Vector3(4f, 0, 4.6f), 1.2f, Quaternion.LookRotation(z.side)); z.mk.Box(dark, p + up * .9f + z.front * 1.71f, new Vector3(.8f, 1.8f, .04f), rot);
                z.mk.Box(GlowM(Lamp, 1.4f), p + up * 1.5f + z.front * 1.71f + z.side * 1.2f, new Vector3(.6f, .6f, .04f), rot);
            }
        }
    }
}
