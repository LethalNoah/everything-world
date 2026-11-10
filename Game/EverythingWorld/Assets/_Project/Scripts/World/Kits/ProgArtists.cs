using System.Collections.Generic;
using UnityEngine;

namespace EverythingWorld.World
{
    /// The Prog Marches' artists, each a strange monument with covers on a dark stone board, a plaque and an amber lamp:
    /// - Pink Floyd: an observatory with its dome open and a telescope pointed at the eclipse; a stretch of white brick
    ///   wall; a pink pig floating between two tall chimneys; two men in suits shaking hands, one of them on fire.
    /// - Frank Zappa: a mad garage with its door up (a drum kit and amps inside), a flying saucer hovering over a
    ///   flight of stone terraces, a heap of giant peaches, a giant spool of dental floss.
    /// - King Crimson: a giant crimson face with its mouth open in a scream, and a red meter with its needle in the red.
    /// - Rush: a red star in a circle on a pillar between temple columns, and a beacon tower flashing Y-Y-Z in Morse.
    /// - Electric Light Orchestra: a glowing flying saucer. The Alan Parsons Project: a golden eye on a pillar, a
    ///   bright star above. The Moody Blues: white satin curtains on frames. Manfred Mann's Earth Band: a turning
    ///   searchlight. Steven Wilson: an hourglass. After: a blindfolded stone head. Dear Sherlock: a tall birdcage with
    ///   a glowing bird. Geordie Greep: a giant gramophone horn. Kourosh Yaghmaei: a glowing ice flower.
    public partial class ProgKit
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
            area.artistName = a.name; area.stationId = a.stationId; area.radius = a.r; area.glowColor = Amber;
            var z = new Plot { ctx = ctx, root = root, labels = labels, mk = new MeshKit(), solid = new MeshKit(), pictures = new MeshKit(), a = a, c = ctx.OnGround(a.x, a.z), front = front, side = Vector3.Cross(Vector3.up, front), glows = new List<Renderer>(), area = area };
            switch (a.name)
            {
                case "Pink Floyd": Floyd(z); break;
                case "Frank Zappa": Zappa(z); ZappaMore(z); break;
                case "King Crimson": Crimson(z); CrimsonMore(z); break;
                case "Rush": Rush(z); RushMore(z); break;
                case "Electric Light Orchestra": Saucer(z); break;
                case "The Alan Parsons Project": Eye(z); ParsonsMore(z); break;
                case "The Moody Blues": Satin(z); break;
                case "Manfred Mann's Earth Band": Searchlight(z); break;
                case "Steven Wilson": Hourglass(z); break;
                case "After": Blindfold(z); break;
                case "Dear Sherlock": Birdcage(z); break;
                case "Geordie Greep": Gramophone(z); break;
                default: IceFlower(z); break;
            }
            var reach = Foot(a);
            Board(z, z.At(reach * .8f, -reach * .5f - 2.5f), front);
            ArtistBits.Plaque(z.mk, z.solid, labels, a, z.At(reach * .8f + 1.5f, reach * .5f + 2.5f), front, darkStone, dark, Ink, 1.6f);
            Lamp(ctx, z.mk, z.At(reach * .8f, reach * .5f + 5f));
            if (z.area.glowLight == null) z.area.glowLight = ArtistBits.PointLight(root, z.c + Vector3.up * 5f + front * 5f, Amber, HasStation(a) ? 20f : 13f, 1.4f);
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
            foreach (var s in new[] { -1f, 1f }) { z.mk.Box(darkStone, ground + side * s * (w / 2f - .1f) + Vector3.up * 1.2f, new Vector3(.25f, 2.6f, .25f), rot); ArtistBits.PostCollider(z.root, ground + side * s * (w / 2f - .1f), 2.4f, .15f); }
            var c = ground + Vector3.up * 1.8f;
            z.mk.Box(darkStone, c - Vector3.up * .1f, new Vector3(w, size + .6f, .14f), rot); z.solid.Box(darkStone, c - Vector3.up * .1f, new Vector3(w, size + .6f, .24f), rot);
            z.mk.Box(chrome, c + Vector3.up * (size / 2f + .22f), new Vector3(w + .1f, .06f, .2f), rot); z.mk.Box(GlowM(Violet, 2f), c - Vector3.up * (size / 2f + .42f), new Vector3(w, .04f, .16f), rot);
            ArtistBits.CoverWall(z.pictures, z.labels, z.ctx, z.a, c + dir * .075f + Vector3.up * .08f, dir, n, size, n, true, Ink);
        }

        // ---------------------------------------------------------------- Pink Floyd
        void Floyd(Plot z)
        {
            var up = Vector3.up;
            // the observatory: a drum with a dome, a slit open, a telescope pointed up at the eclipse
            var o = z.At(-8f, 0); var white = WorldMaterials.Solid(new Color(.88f, .88f, .9f)); var domeM = WorldMaterials.Solid(new Color(.72f, .74f, .78f), .8f);
            z.mk.Prism(white, o - up * .3f, up, 5f, 5.3f, 20); z.solid.Prism(white, o - up * .3f, up, 5f, 5.3f, 20);
            for (var k = 0; k < 5; k++) { var r0 = 5.2f * Mathf.Cos(k * Mathf.PI / 10f); var r1 = 5.2f * Mathf.Cos((k + 1) * Mathf.PI / 10f); z.mk.Prism(domeM, o + up * (5f + 5.2f * Mathf.Sin(k * Mathf.PI / 10f)), up, r0, 5.2f * (Mathf.Sin((k + 1) * Mathf.PI / 10f) - Mathf.Sin(k * Mathf.PI / 10f)), 20, r1 / r0); }
            z.mk.Box(dark, o + up * 8f + z.front * 2.3f, new Vector3(1.4f, 6f, 3f), z.Rot * Quaternion.Euler(-35f, 0, 0));
            z.mk.Prism(chrome, o + up * 7f, (z.front + up * 1.2f).normalized, .55f, 6f, 12); z.mk.Prism(SongGlow(AstronomyDomine, new Color(.8f, .85f, 1f), 2f), o + up * 7f + (z.front + up * 1.2f).normalized * 6f, (z.front + up * 1.2f).normalized, .5f, .05f, 12);
            z.mk.Box(dark, o + up * 1.2f + z.front * 5.01f, new Vector3(1.4f, 2.4f, .05f), z.Rot);
            foreach (var s in new[] { -2f, 2f }) z.mk.Box(GlowM(Amber, 1.8f), o + up * 3f + z.front * 4.95f + z.side * s, new Vector3(.8f, 1f, .05f), z.Rot);
            // the pig floating over a brick power station with four tall chimneys
            var ch = z.At(-6f, 13f); var brickM = WorldMaterials.Solid(new Color(.55f, .32f, .25f));
            z.mk.Box(brickM, ch + up * 3.2f, new Vector3(7f, 7f, 9f), Quaternion.LookRotation(z.side)); z.solid.Box(brickM, ch + up * 3.2f, new Vector3(7f, 7f, 9f), Quaternion.LookRotation(z.side));
            z.mk.Box(WorldMaterials.Solid(new Color(.4f, .22f, .18f)), ch + up * 6.8f, new Vector3(7.4f, .3f, 9.4f), Quaternion.LookRotation(z.side));
            for (var k = 0; k < 4; k++) z.mk.Box(GlowM(Amber, 1.2f), ch + z.front * 3.52f + z.side * (-3f + k * 2f) + up * 3.5f, new Vector3(1f, 2.6f, .03f), z.Rot);
            foreach (var s in new[] { -3.6f, 3.6f }) foreach (var fw in new[] { -2.6f, 2.6f }) { var cp = ch + z.side * s + z.front * fw; z.mk.Prism(WorldMaterials.Solid(new Color(.9f, .87f, .8f)), cp + up * 6.5f, up, .7f, 13f, 10, .8f); }
            var pig = new MeshKit(); var pink = WorldMaterials.Solid(new Color(1f, .62f, .72f), .5f);
            pig.Blob(pink, Vector3.zero, new Vector3(1.4f, 1.1f, 2.2f), z.ctx.Rng, 0f); pig.Blob(pink, new Vector3(0, .2f, 2f), new Vector3(.8f, .75f, .7f), z.ctx.Rng, 0f);
            pig.Prism(pink, new Vector3(0, .1f, 2.6f), Vector3.forward, .35f, .25f, 10);
            foreach (var s in new[] { -.4f, .4f }) { pig.Cone(pink, new Vector3(s, .85f, 2f), .22f, .4f, 4); pig.Prism(pink, new Vector3(s * 1.5f, -1f, s > 0 ? .9f : -.9f), Vector3.down, .2f, .5f, 6); pig.Prism(pink, new Vector3(-s * 1.5f, -1f, s > 0 ? .9f : -.9f), Vector3.down, .2f, .5f, 6); }
            var pt = pig.BuildSingle("Floating pig", life.transform, false).transform; life.pig = pt; life.pigBase = ch + up * 22f; pt.position = life.pigBase;
            // two men in suits shaking hands, one on fire
            var suit = WorldMaterials.Solid(new Color(.25f, .25f, .28f)); var skin = WorldMaterials.Solid(new Color(.85f, .7f, .58f));
            var hp = z.At(4f, -5f);
            foreach (var s in new[] { -1f, 1f })
            {
                var p = hp + z.side * s * .55f; var face = -z.side * s;
                z.mk.Box(suit, p + up * .5f, new Vector3(.35f, 1f, .3f), Quaternion.LookRotation(face)); z.mk.Box(suit, p + up * 1.35f, new Vector3(.55f, .8f, .3f), Quaternion.LookRotation(face));
                z.mk.Blob(skin, p + up * 1.95f, Vector3.one * .16f, z.ctx.Rng, 0f); z.mk.Box(suit, p + up * 1.45f + face * .3f, new Vector3(.12f, .12f, .45f), Quaternion.LookRotation(face));
            }
            z.solid.Box(suit, hp + up * 1f, new Vector3(1.6f, 2f, .6f), Quaternion.LookRotation(z.side));
            life.burning = hp + up * 1f + z.side * .55f;
            noGrass.Add(new Vector3(o.x, 7f, o.z));
        }

        // ---------------------------------------------------------------- Frank Zappa
        void Zappa(Plot z)
        {
            var up = Vector3.up;
            // the garage, its door rolled up, a drum kit and amps inside under a bare bulb
            var g = z.At(-8f, -5f); var rot = z.Rot; var tin = WorldMaterials.Solid(new Color(.55f, .6f, .58f), .6f);
            z.mk.Box(tin, g + up * 2f - z.front * 2.9f, new Vector3(7f, 4f, .2f), rot);
            foreach (var s in new[] { -1f, 1f }) z.mk.Box(tin, g + up * 2f + z.side * s * 3.4f, new Vector3(.2f, 4f, 6f), rot);
            z.mk.Box(tin, g + up * 3.6f + z.front * 3f, new Vector3(7f, .8f, .2f), rot); z.mk.Box(tin, g + up * 4.1f, new Vector3(7.4f, .2f, 6.4f), rot);
            z.mk.Box(WorldMaterials.Solid(new Color(.45f, .45f, .45f)), g + up * .05f, new Vector3(6.6f, .1f, 5.8f), rot);
            z.solid.Box(tin, g + up * 2f - z.front * 2.9f, new Vector3(7f, 4f, .4f), rot); foreach (var s in new[] { -1f, 1f }) z.solid.Box(tin, g + up * 2f + z.side * s * 3.4f, new Vector3(.4f, 4f, 6f), rot);
            IndoorSpaces.Add(g + up * 2f, rot, new Vector3(6.6f, 4f, 5.8f));
            var kit = g - z.front * 1.2f; var red = WorldMaterials.Solid(new Color(.75f, .15f, .15f), .7f);
            z.mk.Prism(red, kit + up * .5f - z.side * .05f, z.side, .55f, .5f, 14); foreach (var s in new[] { -.8f, .8f }) z.mk.Prism(red, kit + up * .7f + z.side * s + z.front * .5f, up, .3f, .35f, 12);
            z.mk.Prism(brass, kit + up * 1.3f + z.side * 1.2f, up, .45f, .03f, 14); z.mk.Prism(chrome, kit + z.side * 1.2f, up, .03f, 1.3f, 6);
            foreach (var s in new[] { -2.4f, 2.4f }) { z.mk.Box(dark, g + up * .8f + z.side * s - z.front * 1.8f, new Vector3(1.1f, 1.6f, .7f), rot); z.mk.Box(GlowM(Amber, 1.2f), g + up * 1.1f + z.side * s - z.front * 1.44f, new Vector3(.9f, .8f, .02f), rot); }
            var bulb = g + up * 3.5f; z.mk.Blob(GlowM(new Color(1f, .9f, .6f), 3f), bulb, Vector3.one * .15f, z.ctx.Rng, 0f); ArtistBits.PointLight(z.root, bulb - up * .5f, new Color(1f, .85f, .55f), 8f, 1.4f, true);
            // the Inca terraces and the saucer above them (the saucer is moved by ProgLife)
            var t0 = z.At(-6f, 8f);
            for (var k = 0; k < 4; k++) { z.mk.Box(stone, t0 + up * (k * .7f + .35f) - z.front * k * 1.2f, new Vector3(7f - k, .7f, 6f - k * 1.2f), rot); z.solid.Box(stone, t0 + up * (k * .7f + .35f) - z.front * k * 1.2f, new Vector3(7f - k, .7f, 6f - k * 1.2f), rot); }
            var ufo = new MeshKit(); ufo.Prism(chrome, Vector3.zero, Vector3.up, 3f, .4f, 24, .6f); ufo.Prism(chrome, Vector3.zero, Vector3.down, 3f, .3f, 24, .5f);
            ufo.Prism(GlowM(new Color(.5f, 1f, .6f), 2f), Vector3.up * .4f, Vector3.up, 1.2f, .8f, 16, .5f);
            for (var k = 0; k < 12; k++) { var a = k * 30f * Mathf.Deg2Rad; ufo.Blob(GlowM(Spectrum[k % 6], 3f), new Vector3(Mathf.Cos(a) * 2.8f, .1f, Mathf.Sin(a) * 2.8f), Vector3.one * .15f, z.ctx.Rng, 0f); }
            var ut = ufo.BuildSingle("Inca saucer", life.transform, false).transform; life.ufo = ut; life.ufoBase = t0 + up * 9f - z.front * 2f; ut.position = life.ufoBase;
            // giant peaches and a spool of floss
            var pe = z.At(2f, 7f); var peach = SongGlow(PeachesSong, new Color(1f, .55f, .3f), .45f); var leaf = WorldMaterials.Solid(new Color(.35f, .55f, .25f));
            Vector3[] heap = { new Vector3(0, 1f, 0), new Vector3(1.8f, .9f, .4f), new Vector3(.9f, .9f, -1.6f), new Vector3(.9f, 2.4f, -.4f) };
            foreach (var h in heap) { var p = pe + z.side * h.x + up * h.y + z.front * h.z; z.mk.Blob(peach, p, Vector3.one * 1f, z.ctx.Rng, .05f); z.mk.Box(leaf, p + up * .95f, new Vector3(.5f, .05f, .25f), Quaternion.Euler(0, 30f, 20f)); }
            z.solid.Box(peach, pe + up * 1.2f + z.side * .9f, new Vector3(4f, 2.6f, 3.2f), rot);
            var sp = z.At(3f, -7f); z.mk.Prism(WorldMaterials.Solid(new Color(.95f, .97f, 1f)), sp, up, 1.1f, 1.6f, 16); z.mk.Prism(WorldMaterials.Solid(new Color(.3f, .7f, .65f)), sp - up * .05f, up, 1.3f, .2f, 16); z.mk.Prism(WorldMaterials.Solid(new Color(.3f, .7f, .65f)), sp + up * 1.55f, up, 1.3f, .2f, 16);
            ArtistBits.PostCollider(z.root, sp, 1.8f, 1.3f);
            noGrass.Add(new Vector3(g.x, 6f, g.z));
        }

        // ---------------------------------------------------------------- King Crimson
        void Crimson(Plot z)
        {
            var up = Vector3.up; var c = z.At(-4f, 0) + up * 4.5f; var skin = WorldMaterials.Solid(new Color(.85f, .3f, .35f)); var deep = WorldMaterials.Solid(new Color(.45f, .05f, .12f));
            z.mk.Blob(skin, c, new Vector3(3.6f, 4.2f, 3f), z.ctx.Rng, .05f);
            foreach (var s in new[] { -1.3f, 1.3f }) { z.mk.Blob(WorldMaterials.Solid(Color.white), c + z.front * 2.5f + z.side * s + up * 1f, new Vector3(.7f, .5f, .3f), z.ctx.Rng, 0f); z.mk.Blob(dark, c + z.front * 2.75f + z.side * s + up * 1f, Vector3.one * .25f, z.ctx.Rng, 0f); z.mk.Box(deep, c + z.front * 2.6f + z.side * s + up * 1.8f, new Vector3(1.2f, .25f, .3f), z.Rot * Quaternion.Euler(0, 0, s * -20f)); }
            z.mk.Blob(deep, c + z.front * 2.4f - up * 1.4f, new Vector3(1.4f, 1.2f, .7f), z.ctx.Rng, 0f); z.mk.Blob(dark, c + z.front * 2.6f - up * 1.4f, new Vector3(1.1f, .9f, .5f), z.ctx.Rng, 0f);
            foreach (var s in new[] { -.5f, .5f }) z.mk.Blob(WorldMaterials.Solid(Color.white), c + z.front * 2.8f - up * .65f + z.side * s, new Vector3(.35f, .2f, .15f), z.ctx.Rng, 0f);
            z.mk.Blob(skin, c + z.front * 3f + up * .1f, new Vector3(.6f, 1f, .7f), z.ctx.Rng, 0f);
            z.mk.Prism(darkStone, z.At(-4f, 0) - up * .3f, up, 3.4f, 1f, 10);
            z.solid.Box(skin, c - up * 1f, new Vector3(7f, 9f, 6f), z.Rot);
            halos.Add(c + z.front * 3.5f - up * 1.4f, 3f, new Color(1f, .2f, .2f), .35f);
            ArtistBits.PointLight(z.root, c + z.front * 6f, new Color(1f, .35f, .35f), 16f, 1.6f);
            // the red meter
            var m = z.At(-7f, 7f) + up * 1.8f; z.mk.Box(darkStone, m - up * .9f, new Vector3(.4f, 1.8f, .4f), z.Rot);
            z.mk.Prism(dark, m, z.front, 1.3f, .2f, 20); for (var k = 0; k < 9; k++) { var a = (-60f + k * 15f); z.mk.Box(GlowM(k >= 6 ? new Color(1f, .1f, .1f) : new Color(.9f, .9f, .9f), 2f), m + z.front * .21f + Quaternion.AngleAxis(-a, z.front) * up * 1f, new Vector3(.06f, .25f, .02f), Quaternion.AngleAxis(-a, z.front) * z.Rot); }
            z.mk.Box(SongGlow(RedSong, new Color(1f, .2f, .1f), 2.6f), m + z.front * .23f + Quaternion.AngleAxis(-55f, z.front) * up * .5f, new Vector3(.06f, 1f, .02f), Quaternion.AngleAxis(-55f, z.front) * z.Rot);
            ArtistBits.PostCollider(z.root, z.At(-7f, 7f), 3f, 1.3f);
        }

        // ---------------------------------------------------------------- Rush
        void Rush(Plot z)
        {
            var up = Vector3.up; var c = z.At(-5f, 0);
            // the star in a circle on a pillar
            z.mk.Box(darkStone, c + up * 3f, new Vector3(1.2f, 6f, 1.2f), z.Rot); z.solid.Box(darkStone, c + up * 3f, new Vector3(1.2f, 6f, 1.2f), z.Rot);
            var sc = c + up * 9f + z.front * .7f; var redM = GlowM(new Color(1f, .15f, .1f), 1.6f);
            for (var k = 0; k < 40; k++) { var a0 = k * Mathf.PI / 20f; var a1 = (k + 1) * Mathf.PI / 20f; var p0 = sc + (z.side * Mathf.Cos(a0) + up * Mathf.Sin(a0)) * 3f; var p1 = sc + (z.side * Mathf.Cos(a1) + up * Mathf.Sin(a1)) * 3f; z.mk.Box(chrome, (p0 + p1) / 2f, new Vector3(.25f, .25f, Vector3.Distance(p0, p1) + .02f), Quaternion.LookRotation(p1 - p0, z.front)); }
            for (var k = 0; k < 5; k++)
            {
                var a = (90f + k * 72f) * Mathf.Deg2Rad; var b = (90f + k * 72f + 36f) * Mathf.Deg2Rad; var b2 = (90f + k * 72f - 36f) * Mathf.Deg2Rad;
                Vector3 S(float ang, float r) => sc + (z.side * Mathf.Cos(ang) + up * Mathf.Sin(ang)) * r;
                z.mk.Tri(redM, S(a, 2.8f), S(b, 1.1f), S(b2, 1.1f), sc - z.front);
                z.mk.Tri(redM, sc, S(b, 1.1f), S(b2, 1.1f), sc - z.front);
            }
            z.mk.Prism(redM, sc - z.front * .05f, z.front, 1.1f, .1f, 5);
            halos.Add(sc + z.front * .6f, 4f, new Color(1f, .2f, .15f), .35f);
            // the temple columns
            for (var k = 0; k < 6; k++) { var cp = z.At(-2f - (k / 3) * 6f, (k % 3 - 1) * 5.5f + (k % 3 == 1 ? 0 : 0)); if (k % 3 == 1) continue; z.mk.Prism(stone, cp - up * .2f, up, .5f, 7f, 10); z.mk.Box(stone, cp + up * 7f, new Vector3(1.4f, .4f, 1.4f), z.Rot); ArtistBits.PostCollider(z.root, cp, 7f, .5f); }
            // the YYZ beacon on a lattice mast
            var bt = z.At(1f, 7f); for (var k = 0; k < 3; k++) { var d = Quaternion.Euler(0, k * 120f, 0) * Vector3.forward; z.mk.Box(chrome, bt + d * .6f + up * 5f, new Vector3(.1f, 10f, .1f), Quaternion.LookRotation(d) * Quaternion.Euler(-3f, 0, 0)); }
            var beacon = new Material(WorldMaterials.Glow(new Color(1f, .15f, .1f), 1f)) { name = "YYZ beacon" }; z.mk.Blob(beacon, bt + up * 10.3f, Vector3.one * .45f, z.ctx.Rng, 0f);
            life.beacon = beacon; life.beaconAt = bt + up * 10.3f; ArtistBits.PostCollider(z.root, bt, 10f, .8f);
        }

        // ---------------------------------------------------------------- the others
        void Saucer(Plot z)
        {
            var up = Vector3.up; var s = new MeshKit(); var neon = new Material(WorldMaterials.Glow(new Color(.3f, .7f, 1f), 1.6f)) { name = "ELO neon" };
            s.Prism(chrome, Vector3.zero, Vector3.up, 4f, .6f, 28, .55f); s.Prism(chrome, Vector3.zero, Vector3.down, 4f, .4f, 28, .45f);
            s.Prism(neon, Vector3.up * .6f, Vector3.up, 1.8f, 1f, 20, .4f);
            for (var k = 0; k < 16; k++) { var a = k * 22.5f * Mathf.Deg2Rad; s.Box(GlowM(Spectrum[k % 6], 2.6f), new Vector3(Mathf.Cos(a) * 3.85f, .05f, Mathf.Sin(a) * 3.85f), new Vector3(.5f, .2f, .3f), Quaternion.Euler(0, -k * 22.5f, 0)); }
            var t = s.BuildSingle("ELO saucer", life.transform, false).transform; life.saucer = t; life.saucerBase = z.At(-4f, 0) + up * 6f; t.position = life.saucerBase; life.neon = neon;
            var pad = z.At(-4f, 0); z.mk.Prism(darkStone, pad - up * .3f, up, 3f, .6f, 16); z.mk.Prism(GlowM(new Color(.3f, .7f, 1f), 1.6f), pad + up * .31f, up, 2.6f, .02f, 16);
            halos.Add(life.saucerBase, 5f, new Color(.3f, .7f, 1f), .3f);
        }

        void Eye(Plot z)
        {
            var up = Vector3.up; var c = z.At(-3f, 0); var gold = GlowM(new Color(1f, .78f, .3f), 1.6f);
            z.mk.Box(darkStone, c + up * 2.5f, new Vector3(1.6f, 5f, 1f), z.Rot); z.solid.Box(darkStone, c + up * 2.5f, new Vector3(1.6f, 5f, 1f), z.Rot);
            var e = c + up * 7f + z.front * .6f;
            for (var k = 0; k < 24; k++)
            {
                float a0 = k * Mathf.PI / 12f, a1 = (k + 1) * Mathf.PI / 12f;
                Vector3 E(float a) => e + z.side * Mathf.Cos(a) * 3f + up * Mathf.Sin(a) * 1.3f * (Mathf.Sin(a) > 0 ? 1f : .8f);
                z.mk.Box(gold, (E(a0) + E(a1)) / 2f, new Vector3(.25f, .25f, Vector3.Distance(E(a0), E(a1)) + .02f), Quaternion.LookRotation(E(a1) - E(a0), z.front));
            }
            z.mk.Prism(gold, e - z.front * .1f, z.front, .9f, .2f, 16); z.mk.Prism(dark, e + z.front * .05f, z.front, .45f, .1f, 16);
            z.mk.Box(gold, e - up * 1.9f + z.side * .3f, new Vector3(.25f, 1.8f, .25f), z.Rot * Quaternion.Euler(0, 0, -10f)); z.mk.Box(gold, e - up * 1.4f - z.side * 1.6f, new Vector3(.25f, 1.6f, .25f), z.Rot * Quaternion.Euler(0, 0, 40f));
            z.mk.Box(gold, e + up * 2f, new Vector3(5.6f, .25f, .25f), z.Rot);
            halos.Add(e + z.front * .5f, 4f, new Color(1f, .78f, .3f), .35f);
            var star = c + up * 16f; z.mk.Blob(SongGlow(SiriusSong, new Color(.85f, .9f, 1f), 3f), star, Vector3.one * .5f, z.ctx.Rng, 0f); halos.Add(star, 3f, new Color(.85f, .9f, 1f), .7f);
        }

        void Satin(Plot z)
        {
            var up = Vector3.up; var satin = WorldMaterials.Solid(new Color(.96f, .96f, 1f), .9f);
            for (var k = 0; k < 4; k++)
            {
                var p = z.At(-2f - k * 1.6f, (k - 1.5f) * 3.2f); var rot = Quaternion.LookRotation(z.front) * Quaternion.Euler(0, (k - 1.5f) * 12f, 0);
                foreach (var s in new[] { -1.1f, 1.1f }) z.mk.Box(chrome, p + rot * new Vector3(s, 2.6f, 0), new Vector3(.1f, 5.2f, .1f), rot);
                z.mk.Box(chrome, p + rot * new Vector3(0, 5.2f, 0), new Vector3(2.4f, .1f, .1f), rot);
                for (var f = 0; f < 6; f++) z.mk.Box(satin, p + rot * new Vector3(-.9f + f * .36f, 2.7f, Mathf.Sin(f * 1.7f) * .08f), new Vector3(.38f, 4.8f, .03f), rot * Quaternion.Euler(0, (f % 2 == 0 ? 12f : -12f), 0));
                z.solid.Box(satin, p + up * 2.6f, new Vector3(2.4f, 5.2f, .3f), rot);
            }
            ArtistBits.PointLight(z.root, z.At(1f, 0) + up * 4f, new Color(.6f, .7f, 1f), 14f, 1.4f);
            halos.Add(z.At(-3f, 0) + up * 3f, 4f, new Color(.7f, .8f, 1f), .25f);
        }

        void Searchlight(Plot z)
        {
            var up = Vector3.up; var c = z.At(-2.5f, 0);
            z.mk.Prism(darkStone, c - up * .2f, up, 1.6f, 1f, 10); z.solid.Prism(darkStone, c - up * .2f, up, 1.6f, 2.4f, 10);
            var head = new MeshKit(); head.Prism(chrome, new Vector3(0, 0, -.8f), Vector3.forward, 1f, 1.6f, 16); head.Prism(GlowM(new Color(1f, .97f, .85f), 4f), new Vector3(0, 0, .81f), Vector3.forward, .9f, .02f, 16);
            var t = head.BuildSingle("Searchlight", life.transform, false).transform; t.position = c + up * 2.4f; life.searchlight = t;
            halos.Add(c + up * 2.4f, 2.6f, new Color(1f, .95f, .8f), .5f); ArtistBits.PointLight(z.root, c + up * 3f + z.front * 3f, new Color(1f, .95f, .8f), 14f, 1.6f);
        }

        void Hourglass(Plot z)
        {
            var up = Vector3.up; var c = z.At(-2.5f, 0); var sand = GlowM(new Color(1f, .8f, .45f), .9f);
            foreach (var y in new[] { 0f, 5f }) z.mk.Prism(wood, c + up * y, up, 1.8f, .35f, 12);
            foreach (var k in new[] { 0, 1, 2 }) { var a = k * 120f * Mathf.Deg2Rad; z.mk.Prism(wood, c + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * 1.5f, up, .1f, 5.2f, 6); }
            z.mk.Prism(glass, c + up * .35f, up, 1.3f, 2.2f, 14, .1f); z.mk.Prism(glass, c + up * 4.95f, Vector3.down, 1.3f, 2.2f, 14, .1f);
            z.mk.Prism(sand, c + up * .35f, up, 1.1f, .8f, 14, .5f); z.mk.Prism(sand, c + up * 3.6f, up, .5f, .5f, 10, .2f); z.mk.Box(sand, c + up * 1.9f, new Vector3(.05f, 1.5f, .05f), Quaternion.identity);
            z.solid.Prism(wood, c - up * .2f, up, 1.8f, 5.6f, 12);
            halos.Add(c + up * 1f, 2f, new Color(1f, .8f, .45f), .4f);
        }

        void Blindfold(Plot z)
        {
            var up = Vector3.up; var c = z.At(-3f, 0);
            z.mk.Box(darkStone, c + up * 1f, new Vector3(1.6f, 2f, 1.6f), z.Rot); z.mk.Blob(stone, c + up * 3.3f, new Vector3(1.1f, 1.4f, 1.1f), z.ctx.Rng, .05f);
            z.mk.Box(WorldMaterials.Solid(new Color(.15f, .12f, .25f)), c + up * 3.5f, new Vector3(2.3f, .4f, 2.3f), z.Rot);
            z.mk.Box(WorldMaterials.Solid(new Color(.15f, .12f, .25f)), c + up * 3.4f - z.front * 1.2f + z.side * .3f, new Vector3(.15f, .9f, .1f), z.Rot * Quaternion.Euler(0, 0, 15f));
            z.solid.Box(darkStone, c + up * 2.2f, new Vector3(2f, 4.4f, 2f), z.Rot);
        }

        void Birdcage(Plot z)
        {
            var up = Vector3.up; var c = z.At(-3f, 0);
            for (var k = 0; k < 16; k++) { var a = k * 22.5f * Mathf.Deg2Rad; z.mk.Box(brass, c + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * 1.5f + up * 2f, new Vector3(.06f, 4f, .06f), Quaternion.identity); }
            z.mk.Prism(brass, c, up, 1.6f, .2f, 16); z.mk.Prism(brass, c + up * 4f, up, 1.6f, 1.2f, 16, .05f); z.mk.Prism(brass, c + up * 5.2f, up, .1f, .8f, 6);
            var bird = GlowM(new Color(.6f, .9f, 1f), 3f); var bp = c + up * 2f; z.mk.Blob(bird, bp, new Vector3(.3f, .3f, .45f), z.ctx.Rng, 0f); z.mk.Box(bird, bp + up * .05f, new Vector3(1.2f, .04f, .3f), Quaternion.LookRotation(z.front));
            z.mk.Box(brass, bp - up * .35f, new Vector3(2.8f, .06f, .06f), Quaternion.LookRotation(z.front));
            halos.Add(bp, 1.6f, new Color(.6f, .9f, 1f), .6f);
            z.solid.Prism(brass, c - up * .2f, up, 1.6f, 5f, 12);
        }

        void Gramophone(Plot z)
        {
            var up = Vector3.up; var c = z.At(-3f, 0);
            z.mk.Box(wood, c + up * .6f, new Vector3(2.4f, 1.2f, 2.4f), z.Rot); z.mk.Prism(dark, c + up * 1.22f, up, 1f, .05f, 20);
            z.mk.Prism(brass, c + up * 1.3f + z.side * .8f, up, .1f, 1.6f, 6);
            var hornDir = (up * .6f + z.front * .8f).normalized; z.mk.Prism(brass, c + up * 2.8f + z.side * .8f, hornDir, .15f, 3.5f, 16, 14f);
            z.solid.Box(wood, c + up * 1.5f, new Vector3(2.6f, 3f, 2.6f), z.Rot);
            halos.Add(c + up * 2.8f + z.side * .8f + hornDir * 3.5f, 2.4f, Amber, .3f);
        }

        void IceFlower(Plot z)
        {
            var up = Vector3.up; var c = z.At(-3f, 0); var ice = GlowM(new Color(.6f, .85f, 1f), 1.4f);
            z.mk.Prism(darkStone, c - up * .2f, up, 1.4f, .6f, 8); z.mk.Prism(WorldMaterials.Solid(new Color(.5f, .7f, .8f), .9f), c + up * .4f, up, .12f, 3f, 6);
            for (var k = 0; k < 6; k++) { var d = Quaternion.Euler(0, k * 60f, 0) * z.front; z.mk.Cone(ice, c + up * 3.4f + d * .3f, .45f, 1.8f, 4, k * 60f); z.mk.Box(ice, c + up * 3.4f + d * .9f, new Vector3(.3f, .1f, 1.6f), Quaternion.LookRotation(d) * Quaternion.Euler(-30f, 0, 0)); }
            z.mk.Blob(GlowM(new Color(1f, 1f, 1f), 3f), c + up * 3.5f, Vector3.one * .35f, z.ctx.Rng, 0f);
            halos.Add(c + up * 3.5f, 2.4f, new Color(.6f, .85f, 1f), .5f);
            ArtistBits.PostCollider(z.root, c, 4f, 1.4f);
        }
    }
}
