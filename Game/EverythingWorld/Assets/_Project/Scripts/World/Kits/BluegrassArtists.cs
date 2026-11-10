using System.Collections.Generic;
using UnityEngine;

namespace EverythingWorld.World
{
    /// The Bluegrass Hollows' artists, each a homespun clearing with covers on a board (a fiddle carved on top), a
    /// plaque and a mason jar lamp:
    /// - Bill Monroe: a giant mandolin on a stone plinth. Bill Monroe & His Blue Grass Boys: a white Kentucky horse
    ///   fence under a big blue moon. Béla Fleck: a giant banjo. The Flecktones: an electric-blue banjo with amps.
    /// - Harry McClintock: the Big Rock Candy Mountain, striped pastel peaks with lollipop trees and a lemonade
    ///   spring. Flatt & Scruggs: an oil derrick and an old pickup piled high, a banjo on a chair. Old Crow
    ///   Medicine Show: a big wagon wheel against a fence lined with crows. Punch Brothers: a copper still and
    ///   jugs. Ricky Skaggs: a fiddle left on a rocking chair. Doc Watson: a black mountain and a guitar on a stool.
    /// - Molly Tuttle: a crooked tree. Nickel Creek: a little lighthouse on rocks. Sierra Hull: a black river pool
    ///   with stepping stones. The Chieftains: a dance floor by a stone cross. The Del McCoury Band: a 1952 black
    ///   motorcycle. The Infamous Stringdusters: a giant upright bass. The Stanley Brothers: the end of a lonely
    ///   railroad line, a bindle and a lantern. Tony Rice: a white chapel. Trampled by Turtles: giant turtles.
    ///   Watchhouse: a fire lookout tower glowing orange. Alison Krauss & Union Station: a porch swing.
    public partial class BluegrassKit
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
            if (wood == null) MakeMaterials();
            var root = new GameObject("Artist: " + a.name).transform; root.SetParent(ctx.Root, false);
            var front = facing.TryGetValue(a.name, out var f) ? f : new Vector3(center.x - a.x, 0, center.z - a.z).normalized;
            if (front.sqrMagnitude < .01f) front = Vector3.forward;
            var labels = new GameObject("Labels").transform; labels.SetParent(root, false);
            var area = root.gameObject.AddComponent<ArtistArea>();
            area.artistName = a.name; area.stationId = a.stationId; area.radius = a.r; area.glowColor = Warm;
            var z = new Plot { ctx = ctx, root = root, labels = labels, mk = new MeshKit(), solid = new MeshKit(), pictures = new MeshKit(), a = a, c = ctx.OnGround(a.x, a.z), front = front, side = Vector3.Cross(Vector3.up, front), glows = new List<Renderer>(), area = area };
            var n = a.name;
            if (n == "Billy Strings") Billy(z);
            else if (n == "Bill Monroe") { Statue(z, 4.2f, (mk, o, r, s) => Mandolin(mk, o, r, s)); MonroeMic(z); }
            else if (n == "Bill Monroe & His Blue Grass Boys") BlueMoonFence(z);
            else if (n.StartsWith("B") && n.Contains("Fleck and the Flecktones")) Flecktones(z);
            else if (n.StartsWith("B") && n.EndsWith("Fleck")) Statue(z, 3.6f, (mk, o, r, s) => Banjo(mk, o, r, s, false));
            else if (n == "Harry McClintock") CandyPeaks(z);
            else if (n == "Flatt & Scruggs") { Derrick(z); life.mists.Add(z.c); }
            else if (n == "Old Crow Medicine Show") WagonWheelFence(z);
            else if (n == "Punch Brothers") Still(z);
            else if (n == "Ricky Skaggs") { FiddleOnRocker(z); life.mists.Add(z.c); }
            else if (n == "Doc Watson") BlackMountain(z);
            else if (n == "Molly Tuttle") CrookedTree(z);
            else if (n == "Nickel Creek") Lighthouse(z);
            else if (n == "Sierra Hull") BlackRiver(z);
            else if (n == "The Chieftains") DanceFloor(z);
            else if (n == "The Del McCoury Band") { Motorcycle(z); life.mists.Add(z.c); }
            else if (n == "The Infamous Stringdusters") Statue(z, 2.6f, (mk, o, r, s) => UprightBass(mk, o, r, s));
            else if (n == "The Stanley Brothers") LonelyLine(z);
            else if (n == "Tony Rice") Chapel(z);
            else if (n == "Trampled by Turtles") Turtles(z);
            else if (n == "Watchhouse") Lookout(z);
            else PorchSwing(z);
            var reach = Foot(a);
            Board(z, z.At(reach * .8f, -reach * .5f - 2.5f), front);
            ArtistBits.Plaque(z.mk, z.solid, labels, a, z.At(reach * .8f + 1.5f, reach * .5f + 2.5f), front, wood, dark, Ink, 1.6f);
            JarLamp(ctx, z.mk, z.At(reach * .8f, reach * .5f + 5f));
            if (z.area.glowLight == null) z.area.glowLight = ArtistBits.PointLight(root, z.c + Vector3.up * 4f + front * 4f, Warm, 12f, 1.2f);
            area.glowRenderers = z.glows.ToArray();
            z.mk.Build("Area", root, false);
            z.solid.BuildCollider("Walls", root);
            z.pictures.Build("Covers", root, false);
            noGrass.Add(new Vector3(z.c.x, reach * .9f, z.c.z));
        }

        /// The covers board, two posts and a little shingle roof, with a fiddle carved on top.
        void Board(Plot z, Vector3 ground, Vector3 dir)
        {
            dir = Vector3.ProjectOnPlane(dir, Vector3.up).normalized; var rot = Quaternion.LookRotation(dir); var side = Vector3.Cross(Vector3.up, dir);
            var n = Mathf.Max(1, Mathf.Min(5, z.a.songList?.Length ?? 1)); const float size = .9f; var w = n * size * 1.25f + .4f;
            foreach (var s in new[] { -1f, 1f }) { z.mk.Prism(wood, ground + side * s * (w / 2f - .1f) - dir * .05f, Vector3.up, .1f, 2.5f, 6); ArtistBits.PostCollider(z.root, ground + side * s * (w / 2f - .1f), 2.4f, .12f); }
            var c = ground + Vector3.up * 1.8f;
            z.mk.Box(cream, c - Vector3.up * .1f, new Vector3(w, size + .6f, .1f), rot); z.solid.Box(cream, c - Vector3.up * .1f, new Vector3(w, size + .6f, .2f), rot);
            z.mk.Box(roofM, c + Vector3.up * (size / 2f + .4f), new Vector3(w + .4f, .08f, .5f), rot * Quaternion.Euler(-12f, 0, 0));
            Fiddle(z.mk, c + Vector3.up * (size / 2f + .85f), rot * Quaternion.Euler(0, 0, 90f), .9f);
            ArtistBits.CoverWall(z.pictures, z.labels, z.ctx, z.a, c + dir * .055f + Vector3.up * .08f, dir, n, size, n, true, new Color(.15f, .12f, .1f));
        }

        void Statue(Plot z, float scale, System.Action<MeshKit, Vector3, Quaternion, float> build)
        {
            var up = Vector3.up; var c = z.At(-3f, 0);
            z.mk.Prism(stone, c - up * .3f, up, 2.2f, 1.1f, 10); z.mk.Prism(stone, c + up * .8f, up, 1.6f, .3f, 10); z.solid.Prism(stone, c - up * .3f, up, 2.2f, 1.4f, 10);
            build(z.mk, c + up * (1.1f + scale * .55f), z.Rot * Quaternion.Euler(0, 0, 8f), scale);
            ArtistBits.PostCollider(z.root, c, scale * 2.2f, 1.4f);
            foreach (var s in new[] { -1f, 1f }) { var lp = z.At(-.6f, s * 2.6f); z.mk.Prism(GlowM(Warm, 2.6f), lp, Vector3.up, .15f, .3f, 8); halos.Add(lp + up * .2f, .8f, Warm, .8f); }
            ArtistBits.PointLight(z.root, c + up * 2f + z.front * 3f, Warm, 11f, 1.3f);
        }

        void BlueMoonFence(Plot z)
        {
            var up = Vector3.up; var white = trim;
            for (var k = 0; k < 6; k++)
            {
                var p = z.At(-3f, -7.5f + k * 3f); z.mk.Box(white, p + up * .7f, new Vector3(.18f, 1.4f, .18f), z.Rot);
                if (k < 5) foreach (var h in new[] { .55f, 1.15f }) z.mk.Box(white, z.At(-3f, -6f + k * 3f) + up * h, new Vector3(3f, .18f, .06f), Quaternion.LookRotation(z.side) * Quaternion.Euler(0, 90f, 0));
            }
            z.solid.Box(white, z.At(-3f, 0) + up * .7f, new Vector3(15f, 1.4f, .4f), Quaternion.LookRotation(z.front) * Quaternion.Euler(0, 0, 0) * Quaternion.identity);
            // the blue moon hanging low over the paddock, two horses in it
            var mc = z.At(-9f, 0) + up * 12f; var moon = new Material(WorldMaterials.Glow(new Color(.45f, .65f, 1f), .3f)) { name = "Blue moon" };
            z.mk.Prism(moon, mc - z.front * .1f, z.front, 3f, .2f, 32);
            halos.Add(mc, 7f, new Color(.45f, .65f, 1f), .35f);
            life.blueMoon = moon;
            Horse(z, z.At(-6.5f, -3.5f), Quaternion.Euler(0, 75f, 0) * z.front, new Color(.45f, .28f, .17f), true);
            Horse(z, z.At(-7.5f, 3.8f), Quaternion.Euler(0, -125f, 0) * z.front, new Color(.82f, .8f, .76f), false);
            ArtistBits.PointLight(z.root, z.At(-3f, 0) + up * 6f + z.front * 3f, new Color(.6f, .75f, 1f), 16f, 1.3f);
        }

        void Flecktones(Plot z)
        {
            var up = Vector3.up; var c = z.At(-2.5f, 0);
            Banjo(z.mk, c + up * 2.4f, z.Rot * Quaternion.Euler(0, 0, -10f), 2.6f, true);
            z.mk.Prism(metal, c - up * .2f, up, .9f, .25f, 12); z.mk.Prism(metal, c, up, .1f, 1.1f, 6);
            ArtistBits.PostCollider(z.root, c, 5f, 1f);
            foreach (var s in new[] { -1f, 1f }) { var ap = z.At(-1.5f, s * 3f); z.mk.Box(dark, ap + up * .6f, new Vector3(1f, 1.2f, .6f), z.Rot); z.mk.Box(GlowM(new Color(.4f, .7f, 1f), 1.8f), ap + up * .6f + z.front * .31f, new Vector3(.8f, .9f, .02f), z.Rot); z.solid.Box(dark, ap + up * .6f, new Vector3(1f, 1.2f, .6f), z.Rot); }
            halos.Add(c + up * 2.4f + z.front * .6f, 2.4f, new Color(.4f, .7f, 1f), .5f);
            ArtistBits.PointLight(z.root, c + up * 2.5f + z.front * 2.5f, new Color(.5f, .75f, 1f), 12f, 1.4f);
        }

        void CandyPeaks(Plot z)
        {
            var up = Vector3.up; var c = z.At(-5f, 0);
            Color[] candy = { new Color(1f, .62f, .8f), new Color(.6f, .95f, .82f), new Color(1f, .92f, .45f), new Color(.75f, .7f, 1f), new Color(1f, .98f, .95f) };
            float[] h = { 9f, 6.5f, 5f }; Vector3[] at = { Vector3.zero, z.side * 5f - z.front * 1f, -z.side * 4.6f - z.front * .5f };
            for (var p = 0; p < 3; p++)
            {
                var b = c + at[p]; var bands = 5;
                for (var k = 0; k < bands; k++) { var r0 = (3.6f - p * .6f) * (1f - k / (float)bands); z.mk.Prism(WorldMaterials.Solid(candy[(k + p) % candy.Length], .7f), b + up * (h[p] * k / bands), up, r0, h[p] / bands, 10, (bands - k - 1f) / (bands - k)); }
            }
            z.solid.Box(stone, c + up * 2.5f, new Vector3(12f, 5f, 6f), z.Rot);
            // lollipop trees, a lemonade spring, rock candy crystals
            for (var k = 0; k < 4; k++) { var lp = z.At(1.5f, -7f + k * 4.6f); z.mk.Prism(trim, lp, up, .07f, 2.4f, 6); z.mk.Prism(WorldMaterials.Solid(candy[k % 4], .8f), lp + up * 2.7f - z.front * .1f, z.front, .55f, .2f, 16); ArtistBits.PostCollider(z.root, lp, 2.4f, .1f); }
            var lem = z.At(2f, 4.5f); var lemonade = new Material(WorldMaterials.Glow(new Color(1f, .9f, .35f), .5f)) { name = "Lemonade spring" };
            z.mk.Prism(lemonade, lem + up * .02f, up, 1.8f, .05f, 18); for (var k = 0; k < 9; k++) { var a = k * 40f; z.mk.Blob(stone, lem + Quaternion.Euler(0, a, 0) * Vector3.forward * 1.9f + up * .2f, new Vector3(.4f, .3f, .35f), z.ctx.Rng, .3f); }
            halos.Add(lem + up * .4f, 2f, new Color(1f, .9f, .35f), .5f); life.lemonade = lem; life.candy = c + up * 4f;
            for (var k = 0; k < 6; k++) { var cp = z.At(-1.5f + z.ctx.Range(-1f, 1f), -6f + k * 2.4f); z.mk.Cone(GlowM(candy[k % 5], 1f), cp, .35f, z.ctx.Range(.8f, 1.6f), 5); }
            ArtistBits.PointLight(z.root, c + up * 6f + z.front * 6f, new Color(1f, .8f, .9f), 18f, 1.4f);
        }

        void Derrick(Plot z)
        {
            var up = Vector3.up; var c = z.At(-5f, -3f); var steel = WorldMaterials.Solid(new Color(.25f, .25f, .27f));
            for (var k = 0; k < 4; k++) { var d = Quaternion.Euler(0, 45f + k * 90f, 0) * Vector3.forward; z.mk.Box(steel, c + d * 1.1f + up * 4f, new Vector3(.18f, 8.2f, .18f), Quaternion.LookRotation(d) * Quaternion.Euler(-8f, 0, 0)); }
            for (var h = 1; h < 8; h += 2) z.mk.Box(steel, c + up * h, new Vector3(2.4f - h * .2f, .12f, 2.4f - h * .2f), Quaternion.identity);
            z.mk.Box(steel, c + up * 8.1f, new Vector3(.8f, .3f, .8f), Quaternion.identity);
            var oil = WorldMaterials.Solid(new Color(.05f, .05f, .06f), .95f); z.mk.Prism(oil, c + up * .03f, up, 2.2f, .04f, 14); life.gusher = c + up * .5f;
            ArtistBits.PostCollider(z.root, c, 8f, 1.6f);
            Pickup(z.ctx, z.mk, z.solid, z.At(-1f, 4.5f));
            var ch = z.At(1.5f, 1f); Rocker(z.mk, z.solid, z.ctx, ch, z.front); Banjo(z.mk, ch + up * .95f + z.front * .05f, z.Rot * Quaternion.Euler(0, 0, 25f), .9f, false);
        }

        void WagonWheelFence(Plot z)
        {
            var up = Vector3.up; Fence(z.ctx, z.mk, z.solid, z.At(-3f, -7f), z.At(-3f, 7f));
            var wc = z.At(-2.2f, -1f) + up * 1.6f; var rot = z.Rot * Quaternion.Euler(-12f, 0, 0);
            for (var k = 0; k < 12; k++) { var a = k * 30f * Mathf.Deg2Rad; z.mk.Box(wood, wc + rot * new Vector3(Mathf.Cos(a) * .8f, Mathf.Sin(a) * .8f, 0), new Vector3(.08f, 1.6f, .08f), rot * Quaternion.Euler(0, 0, k * 30f - 90f)); }
            for (var k = 0; k < 20; k++) { var a0 = k * 18f * Mathf.Deg2Rad; var a1 = (k + 1) * 18f * Mathf.Deg2Rad; var p0 = wc + rot * new Vector3(Mathf.Cos(a0), Mathf.Sin(a0), 0) * 1.6f; var p1 = wc + rot * new Vector3(Mathf.Cos(a1), Mathf.Sin(a1), 0) * 1.6f; z.mk.Box(dark, (p0 + p1) / 2f, new Vector3(.14f, .16f, Vector3.Distance(p0, p1) + .02f), Quaternion.LookRotation(p1 - p0, rot * Vector3.forward)); }
            z.mk.Prism(wood, wc - rot * Vector3.forward * .2f, rot * Vector3.forward, .25f, .4f, 8);
            // crows along the top rail
            var crow = WorldMaterials.Solid(new Color(.08f, .08f, .1f)); var beak = WorldMaterials.Solid(new Color(.3f, .3f, .3f));
            for (var k = 0; k < 5; k++)
            {
                var p = z.At(-3f, -6f + k * 2.9f + z.ctx.Range(-.4f, .4f)) + up * 1.5f; var face = Quaternion.Euler(0, z.ctx.Range(-60f, 60f), 0) * z.front;
                z.mk.Blob(crow, p + up * .15f, new Vector3(.14f, .16f, .14f), z.ctx.Rng, 0f); z.mk.Blob(crow, p + up * .35f + face * .08f, Vector3.one * .09f, z.ctx.Rng, 0f);
                z.mk.Cone(beak, p + up * .35f + face * .17f, .03f, .08f, 4); z.mk.Box(crow, p + up * .12f - face * .2f, new Vector3(.1f, .03f, .2f), Quaternion.LookRotation(face));
            }
        }

        void Still(Plot z)
        {
            var up = Vector3.up; var c = z.At(-2f, 0); var copper = WorldMaterials.Solid(new Color(.8f, .45f, .25f), .8f);
            z.mk.Prism(stone, c - up * .2f, up, 1f, .8f, 10); z.mk.Prism(copper, c + up * .6f, up, .85f, 1.2f, 14, .55f); z.mk.Prism(copper, c + up * 1.8f, up, .2f, .5f, 8);
            z.mk.Prism(copper, c + up * 2.25f, (z.side + up * .1f).normalized, .07f, 1.9f, 6);
            var keg = c + z.side * 2.2f; z.mk.Prism(wood, keg, up, .6f, 1.2f, 12); for (var k = 0; k < 4; k++) z.mk.Prism(copper, keg + up * (.2f + k * .25f), up, .62f, .06f, 12);
            for (var k = 0; k < 4; k++) { var j = z.At(.5f, -1.5f - k * .6f); z.mk.Prism(Tone(new Color(.85f, .82f, .72f)), j, up, .2f, .35f, 10, .5f); z.mk.Prism(Tone(new Color(.5f, .35f, .2f)), j + up * .35f, up, .05f, .12f, 6); }
            var fire = z.At(-2f, 0) + up * .1f; life.fires.Add(fire); var fl = ArtistBits.PointLight(z.root, fire + up * .5f, new Color(1f, .55f, .2f), 8f, 1.4f); fl.gameObject.AddComponent<FireFlicker>().baseIntensity = 1.4f;
            z.solid.Box(copper, c + up * 1f, new Vector3(5f, 2f, 2f), Quaternion.LookRotation(z.side));
        }

        void FiddleOnRocker(Plot z)
        {
            var up = Vector3.up; var deck = z.At(-2f, 0);
            z.mk.Box(plank, deck + up * .15f, new Vector3(4f, .3f, 3f), z.Rot); z.solid.Box(plank, deck + up * .15f, new Vector3(4f, .3f, 3f), z.Rot);
            Rocker(z.mk, z.solid, z.ctx, deck + up * .3f, z.front);
            Fiddle(z.mk, deck + up * 1f + z.front * .05f, z.Rot * Quaternion.Euler(-60f, 0, 0), 1f);
            var lp = deck + z.side * 1.4f + up * .3f; z.mk.Prism(SongGlow(UnclePenSkaggs, Warm, 2.6f), lp, up, .14f, .3f, 8); halos.Add(lp + up * .15f, .8f, Warm, .8f);
        }

        void BlackMountain(Plot z)
        {
            var up = Vector3.up; var c = z.At(-5f, 0); var rock = WorldMaterials.Solid(new Color(.16f, .16f, .2f));
            z.mk.Cone(rock, c, 4.2f, 8f, 7, 10f); z.mk.Cone(rock, c + z.side * 3.5f, 3f, 5f, 6, 30f); z.mk.Cone(rock, c - z.side * 3.2f, 2.6f, 4f, 6, 50f);
            z.solid.Box(rock, c + up * 2.5f, new Vector3(10f, 5f, 6f), z.Rot);
            DocPorch(z);
        }

        void CrookedTree(Plot z)
        {
            var up = Vector3.up; var c = z.At(-4f, 0); var p = c; var dir = up;
            for (var k = 0; k < 5; k++) { var nd = (up + z.side * Mathf.Sin(k * 1.7f) * .8f + z.front * Mathf.Cos(k * 1.3f) * .3f).normalized; z.mk.Prism(bark, p, nd, .5f - k * .06f, 2.2f, 7, .85f); p += nd * 2.1f; dir = nd; }
            for (var k = 0; k < 5; k++) crowns.Blob(leaves[k % 4], p + Quaternion.Euler(0, k * 72f, 0) * Vector3.forward * 2f + up * z.ctx.Range(-.5f, 1f), new Vector3(2.2f, 1.6f, 2.2f), z.ctx.Rng, .3f);
            ArtistBits.PostCollider(z.root, c, 5f, .6f);
            var g = z.At(0f, 2.5f); Guitar(z.mk, g + up * .85f, z.Rot * Quaternion.Euler(-15f, 0, 0), .9f);
        }

        void Lighthouse(Plot z)
        {
            var up = Vector3.up; var c = z.At(-4f, 0); var red = WorldMaterials.Solid(new Color(.75f, .15f, .12f));
            for (var k = 0; k < 7; k++) z.mk.Blob(stone, c + Quaternion.Euler(0, k * 51f, 0) * Vector3.forward * 2.2f + up * .3f, new Vector3(1f, .7f, .9f), z.ctx.Rng, .35f);
            for (var k = 0; k < 4; k++) z.mk.Prism(k % 2 == 0 ? trim : red, c + up * (k * 1.6f), up, 1.3f - k * .12f, 1.6f, 12, (1.3f - (k + 1) * .12f) / (1.3f - k * .12f));
            var lamp = c + up * 6.4f; var beam = GlowM(new Color(1f, .9f, .6f), 3f); z.mk.Prism(beam, lamp, up, .7f, 1f, 10); z.mk.Cone(red, lamp + up * 1f, .9f, 1f, 10);
            halos.Add(lamp + up * .5f, 2.2f, Warm, .9f); ArtistBits.PointLight(z.root, lamp + up * .5f, Warm, 16f, 1.6f);
            z.solid.Prism(stone, c - up * .2f, up, 2.2f, 7f, 10);
        }

        void BlackRiver(Plot z)
        {
            var up = Vector3.up; var c = z.At(-3f, 0); var black = new Material(WorldMaterials.Glow(new Color(.05f, .07f, .12f), .05f)) { name = "Black river" }; black.SetFloat("_Smoothness", .95f);
            for (var k = 0; k < 20; k++) { float a0 = k * Mathf.PI / 10f, a1 = (k + 1) * Mathf.PI / 10f; var w = c + up * .06f; z.mk.Tri(black, w, w + (z.side * Mathf.Cos(a0) * 5f + z.front * Mathf.Sin(a0) * 2.6f), w + (z.side * Mathf.Cos(a1) * 5f + z.front * Mathf.Sin(a1) * 2.6f), w - up); }
            for (var k = 0; k < 5; k++) { var sp = c + z.side * (-4f + k * 2f) + z.front * Mathf.Sin(k) * .6f; z.mk.Prism(stone, sp - up * .1f, up, .45f, .3f, 7); z.solid.Prism(stone, sp - up * .1f, up, .45f, .3f, 7); }
            for (var k = 0; k < 10; k++) { var a = k * 36f; var rp = c + Quaternion.Euler(0, a, 0) * (z.side * 5.3f); rp = z.ctx.OnGround(rp.x, rp.z); crowns.Box(WorldMaterials.Solid(new Color(.3f, .45f, .25f)), rp + up * .6f, new Vector3(.06f, 1.2f, .06f), Quaternion.Euler(z.ctx.Range(-10f, 10f), 0, z.ctx.Range(-10f, 10f))); }
        }

        void DanceFloor(Plot z)
        {
            var up = Vector3.up; var c = z.At(-2f, 0);
            z.mk.Box(plank, c + up * .1f, new Vector3(7f, .2f, 7f), z.Rot); z.solid.Box(plank, c + up * .05f, new Vector3(7f, .1f, 7f), z.Rot);
            var cross = z.At(-6.5f, 0); var grey = WorldMaterials.Solid(new Color(.6f, .6f, .58f));
            z.mk.Box(grey, cross + up * 2f, new Vector3(.6f, 4f, .4f), z.Rot); z.mk.Box(grey, cross + up * 3f, new Vector3(2.2f, .5f, .4f), z.Rot);
            z.mk.Prism(grey, cross + up * 3f - z.front * .2f, z.front, .8f, .4f, 16); z.mk.Prism(z.ctx.Chance(1f) ? leaves[0] : grey, cross + up * 3f - z.front * .25f, z.front, .55f, .5f, 16);
            ArtistBits.PostCollider(z.root, cross, 4f, .5f);
            foreach (var s in new[] { -1f, 1f }) { var pole = z.At(-5.5f, s * 3.5f); z.mk.Prism(wood, pole, up, .1f, 3.6f, 6); }
            for (var i = 0; i < 14; i++) { var t = i / 13f; var bp = z.At(-5.5f, Mathf.Lerp(-3.5f, 3.5f, t)) + up * (3.5f - Mathf.Sin(t * Mathf.PI) * .6f); z.mk.Blob(GlowM(i % 2 == 0 ? new Color(.4f, 1f, .5f) : Warm, 3f), bp, Vector3.one * .1f, z.ctx.Rng, 0f); }
            Fiddle(z.mk, c + up * .5f + z.side * 2.5f, z.Rot * Quaternion.Euler(-80f, 0, 30f), 1f);
        }

        void Motorcycle(Plot z)
        {
            var up = Vector3.up; var c = z.At(-2f, 0); var dir = z.side; var rot = Quaternion.LookRotation(dir); var black = WorldMaterials.Solid(new Color(.06f, .06f, .07f), .9f); var chrome = Tone(new Color(.8f, .8f, .82f), .95f);
            z.mk.Box(stone, c + up * .1f, new Vector3(1.6f, .2f, 3.4f), rot);
            foreach (var s in new[] { -1f, 1f }) { var wc = c + dir * s * 1f + up * .55f; z.mk.Prism(black, wc - z.front * .08f, z.front, .45f, .16f, 16); z.mk.Prism(chrome, wc - z.front * .09f, z.front, .18f, .18f, 10); }
            z.mk.Box(black, c + up * 1.05f + dir * .15f, new Vector3(.32f, .3f, .7f), rot); z.mk.Box(black, c + up * .95f - dir * .5f, new Vector3(.3f, .12f, .7f), rot);
            z.mk.Box(chrome, c + up * .7f, new Vector3(.36f, .4f, .5f), rot); z.mk.Box(chrome, c + up * .55f - dir * .5f, new Vector3(.08f, .08f, 1.2f), rot * Quaternion.Euler(-6f, 0, 0));
            z.mk.Box(chrome, c + up * .95f + dir * .85f, new Vector3(.06f, .9f, .06f), rot * Quaternion.Euler(-20f, 0, 0)); z.mk.Box(chrome, c + up * 1.4f + dir * .7f, new Vector3(.8f, .05f, .05f), rot);
            var head = c + up * 1.2f + dir * 1f; z.mk.Prism(SongGlow(Vincent, new Color(1f, .95f, .8f), 3f), head, dir, .12f, .08f, 10); halos.Add(head + dir * .2f, .8f, Warm, .8f);
            z.solid.Box(black, c + up * .7f, new Vector3(.8f, 1.4f, 2.6f), rot);
        }

        void LonelyLine(Plot z)
        {
            var up = Vector3.up; var dir = z.side; var rot = Quaternion.LookRotation(dir);
            for (var k = -6; k <= 6; k++) { var p = z.At(-3f, k * 1f); z.mk.Box(WorldMaterials.Solid(new Color(.38f, .3f, .24f)), p + up * .08f, new Vector3(.3f, .14f, 2.2f), rot * Quaternion.Euler(0, 90f, 0)); }
            foreach (var s in new[] { -.6f, .6f }) z.mk.Box(metal, z.At(-3f + s, 0) + up * .2f, new Vector3(.08f, .1f, 13f), rot);
            var stop = z.At(-3f, 6.8f); z.mk.Box(barnRed, stop + up * .5f, new Vector3(1.8f, 1f, .4f), rot); z.solid.Box(barnRed, stop + up * .5f, new Vector3(1.8f, 1f, .4f), rot);
            var bindle = z.At(-1.5f, 5f); z.mk.Box(wood, bindle + up * .5f, new Vector3(.05f, 1.6f, .05f), z.Rot * Quaternion.Euler(0, 0, 50f)); z.mk.Blob(WorldMaterials.Solid(new Color(.75f, .2f, .2f)), bindle + up * 1f + z.side * -.55f, Vector3.one * .25f, z.ctx.Rng, .2f);
            var lan = z.At(-1.2f, 4f); z.mk.Prism(GlowM(Warm, 2.6f), lan, up, .14f, .3f, 8); z.mk.Prism(metal, lan + up * .3f, up, .15f, .05f, 8); halos.Add(lan + up * .15f, .8f, Warm, .8f);
            var sign = z.At(-1f, -5f); z.mk.Prism(wood, sign, up, .07f, 2f, 6); z.mk.Box(trim, sign + up * 2f, new Vector3(1.6f, .3f, .05f), z.Rot * Quaternion.Euler(0, 0, 8f));
            ArtistBits.Label(z.labels, "CONSTANT SORROW", sign + up * 2f + z.front * .04f, z.front, .1f, new Color(.15f, .12f, .1f), TextAnchor.MiddleCenter);
        }

        void Chapel(Plot z)
        {
            var up = Vector3.up; var c = z.At(-4.5f, 0); var rot = z.Rot;
            z.mk.Box(trim, c + up * 1.8f, new Vector3(4f, 3.6f, 6f), rot); z.solid.Box(trim, c + up * 1.8f, new Vector3(4f, 3.6f, 6f), rot);
            z.mk.GableRoof(roofM, c + up * 3.63f, new Vector3(4.6f, 0, 6.6f), 1.8f, rot);
            var tw = c + z.front * 2.4f; z.mk.Box(trim, tw + up * 4.8f, new Vector3(1.4f, 2.4f, 1.4f), rot); z.mk.Cone(roofM, tw + up * 6f, 1f, 3f, 4, 45f);
            z.mk.Box(dark, c + up * 1.1f + z.front * 3.01f, new Vector3(1.1f, 2.2f, .04f), rot);
            z.mk.Prism(trim, c + up * 2.9f + z.front * 3f, z.front, .5f, .03f, 14); z.mk.Prism(SongGlow(ChurchStreet, new Color(1f, .85f, .5f), 2f), c + up * 2.9f + z.front * 3.02f, z.front, .4f, .03f, 14);
            foreach (var s in new[] { -1f, 1f }) for (var k = 0; k < 2; k++) z.mk.Box(GlowM(new Color(1f, .85f, .5f), 1.8f), c + up * 2f + z.side * s * 2.01f + z.front * (-1.2f + k * 2f), new Vector3(.04f, 1.4f, .7f), rot);
            var step = c + z.front * 3.6f; z.mk.Box(stone, step + up * .15f, new Vector3(2f, .3f, 1.2f), rot);
            Guitar(z.mk, step + up * 1f + z.side * .7f, rot * Quaternion.Euler(-12f, 0, -8f), .9f);
            ArtistBits.PointLight(z.root, c + up * 2f + z.front * 4.5f, Warm, 10f, 1f);
        }

        void Turtles(Plot z)
        {
            var up = Vector3.up; var shell = WorldMaterials.Solid(new Color(.3f, .42f, .22f)); var plate = WorldMaterials.Solid(new Color(.55f, .48f, .25f)); var skin = WorldMaterials.Solid(new Color(.45f, .55f, .35f));
            Vector3[] at = { z.At(-4f, -4f), z.At(-6f, 3.5f), z.At(-1.5f, 6f) }; float[] sc = { 2.4f, 1.8f, 1.2f };
            for (var k = 0; k < 3; k++)
            {
                var p = at[k]; var s = sc[k]; var face = Quaternion.Euler(0, -20f + k * 25f, 0) * z.front;
                z.mk.Cone(shell, p + up * .5f * s, 1.6f * s, 1.3f * s, 7); z.mk.Prism(plate, p + up * .35f * s, up, 1.62f * s, .18f * s, 7);
                for (var l = 0; l < 4; l++) { var d = Quaternion.Euler(0, 45f + l * 90f, 0) * face; z.mk.Prism(skin, p + d * 1.2f * s, up, .3f * s, .5f * s, 6); }
                z.mk.Blob(skin, p + face * 1.9f * s + up * .6f * s, new Vector3(.4f, .35f, .4f) * s, z.ctx.Rng, 0f);
                z.solid.Box(shell, p + up * .8f * s, new Vector3(3f, 1.6f, 3f) * s, Quaternion.LookRotation(face));
            }
        }

        void Lookout(Plot z)
        {
            var up = Vector3.up; var c = z.At(-4f, 0); var steel = WorldMaterials.Solid(new Color(.4f, .4f, .42f));
            for (var k = 0; k < 4; k++) { var d = Quaternion.Euler(0, 45f + k * 90f, 0) * Vector3.forward; z.mk.Box(steel, c + d * 1.4f + up * 4.5f, new Vector3(.18f, 9.2f, .18f), Quaternion.LookRotation(d) * Quaternion.Euler(-6f, 0, 0)); }
            for (var h = 2; h < 9; h += 3) z.mk.Box(steel, c + up * h, new Vector3(2.8f - h * .1f, .1f, 2.8f - h * .1f), Quaternion.identity);
            var cab = c + up * 9.6f; z.mk.Box(wood, cab, new Vector3(2.6f, 1.6f, 2.6f), Quaternion.identity);
            foreach (var d in new[] { Vector3.forward, Vector3.back, Vector3.left, Vector3.right }) z.mk.Box(GlowM(new Color(1f, .5f, .15f), 2.4f), cab + d * 1.31f + up * .2f, new Vector3(Mathf.Abs(d.x) > 0 ? .02f : 2f, .8f, Mathf.Abs(d.z) > 0 ? .02f : 2f), Quaternion.identity);
            z.mk.Cone(roofM, cab + up * .8f, 2f, 1.2f, 4, 45f);
            halos.Add(cab + up * .2f, 3f, new Color(1f, .5f, .15f), .6f); ArtistBits.PointLight(z.root, cab, new Color(1f, .55f, .2f), 18f, 1.6f);
            ArtistBits.PostCollider(z.root, c, 9f, 1.6f); life.embers = cab;
        }

        void PorchSwing(Plot z)
        {
            var up = Vector3.up; var c = z.At(-2.5f, 0); var rot = z.Rot;
            foreach (var s in new[] { -1f, 1f }) { z.mk.Box(wood, c + z.side * s * 1.6f + up * 1.4f, new Vector3(.15f, 2.9f, .15f), rot * Quaternion.Euler(0, 0, s * 10f)); ArtistBits.PostCollider(z.root, c + z.side * s * 1.6f, 2.8f, .15f); }
            z.mk.Box(wood, c + up * 2.8f, new Vector3(3.4f, .15f, .15f), rot);
            z.mk.Box(cream, c + up * .6f, new Vector3(1.8f, .08f, .6f), rot); z.mk.Box(cream, c + up * .95f - z.front * .28f, new Vector3(1.8f, .6f, .06f), rot);
            foreach (var s in new[] { -.85f, .85f }) z.mk.Box(dark, c + up * 1.7f + z.side * s, new Vector3(.02f, 2.2f, .02f), rot);
            z.solid.Box(cream, c + up * .6f, new Vector3(2f, 1.2f, .8f), rot);
            for (var i = 0; i < 10; i++) { var t = i / 9f; var bp = c + z.side * Mathf.Lerp(-1.6f, 1.6f, t) + up * (2.7f - Mathf.Sin(t * Mathf.PI) * .3f); z.mk.Blob(GlowM(Warm, 3f), bp, Vector3.one * .08f, z.ctx.Rng, 0f); }
            halos.Add(c + up * 2.5f, 1.6f, Warm, .6f);
        }
    }
}
