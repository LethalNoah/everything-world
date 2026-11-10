using System.Collections.Generic;
using UnityEngine;

namespace EverythingWorld.World
{
    /// The Bayou's artists, each on a porch of their own (the big ones on stilts over their own fishing hole),
    /// with things from their songs round it, a plaque by the road, covers on a board and a lantern:
    /// - The Marshall Tucker Band: a rainbow over the fishing hole ("Searchin' for a Rainbow"), an eagle on a
    ///   post ("Fly Eagle Fly"), a boxcar on a stretch of track ("Can't You See"), a gold-panning sluice.
    /// - The Allman Brothers Band: an old flatbed truck heaped with giant peaches, peach trees, two drum kits and
    ///   two guitars on the porch, a sunburst sign ("Soulshine"), an old headstone with candles under an oak.
    /// - Creedence Clearwater Revival: a paddle steamer at a landing on the river ("Proud Mary"), a porch with
    ///   a washboard and a bayou lantern; the moon rises huge in "Bad Moon Rising".
    /// - Little Feat: a cantina porch under a glowing crescent moon ("Spanish Moon"), an upright piano, a clock.
    /// - Lynyrd Skynyrd: a big porch with a rocking chair ("Simple Man"), a road sign home, a birdcage standing open
    ///   with birds on the roof ("Free Bird").
    /// - The Outlaws: a hitching rail with saddles and a longhorn skull. 38 Special: a rope swing from an oak
    ///   ("Hold On Loosely"). George Thorogood: a chopper and a giant bone. James Gang: a pair of boots left
    ///   by a road sign and a suitcase ("Walk Away"). The Red Clay Strays: a porch swing and a fiddle on red clay.
    public partial class BayouKit
    {
        class Spot
        {
            public LandContext ctx; public Transform root, labels; public MeshKit mk, solid, pictures;
            public IslandWorldData.Artist a; public Vector3 c, front, side; public List<Renderer> glows; public ArtistArea area;
            public Vector3 At(float fw, float rt) { var p = c + front * fw + side * rt; return ctx.OnGround(p.x, p.z); }
        }

        public override void BuildArtist(LandContext ctx, IslandWorldData.Artist a)
        {
            if (plank == null) MakeMaterials();
            var root = new GameObject("Artist: " + a.name).transform; root.SetParent(ctx.Root, false);
            var front = facing.TryGetValue(a.name, out var f) ? f : new Vector3(center.x - a.x, 0, center.z - a.z).normalized;
            if (front.sqrMagnitude < .01f) front = Vector3.forward;
            var labels = new GameObject("Labels").transform; labels.SetParent(root, false);
            var area = root.gameObject.AddComponent<ArtistArea>();
            area.artistName = a.name; area.stationId = a.stationId; area.radius = a.r; area.glowColor = new Color(1f, .75f, .4f);
            var z = new Spot { ctx = ctx, root = root, labels = labels, mk = new MeshKit(), solid = new MeshKit(), pictures = new MeshKit(), a = a, c = ctx.OnGround(a.x, a.z), front = front, side = Vector3.Cross(Vector3.up, front), glows = new List<Renderer>(), area = area };
            switch (a.name)
            {
                case "The Marshall Tucker Band": Porch(z, 9f, 7f); MarshallTucker(z); break;
                case "Allman Brothers Band": Porch(z, 10f, 7f); Allmans(z); break;
                case "Creedence Clearwater Revival": Porch(z, 9f, 7f); Creedence(z); break;
                case "Little Feat": Porch(z, 8f, 6.5f); LittleFeat(z); break;
                case "Lynyrd Skynyrd": Porch(z, 10f, 7f); Skynyrd(z); break;
                case "The Outlaws": SmallPorch(z); Outlaws(z); break;
                case "38 Special": SmallPorch(z); RopeSwing(z); break;
                case "George Thorogood & The Destroyers": SmallPorch(z); BadToTheBone(z); break;
                case "James Gang": SmallPorch(z); WalkAway(z); break;
                case "The Red Clay Strays": SmallPorch(z); RedClay(z); break;
                default: SmallPorch(z); break;
            }
            var reach = Foot(a);
            Board(z, z.At(reach * .8f, -reach * .5f - 2.5f), front);
            ArtistBits.Plaque(z.mk, z.solid, labels, a, z.At(reach * .8f + 1.5f, reach * .5f + 2.5f), front, plank, dark, Ink, 1.6f);
            z.glows.Add(Lamp(ctx, z.mk, root, z.At(reach * .8f, reach * .5f + 5f)));
            if (z.area.glowLight == null) z.area.glowLight = ArtistBits.PointLight(root, z.c + Vector3.up * 4f + front * 4f, new Color(1f, .75f, .45f), HasStation(a) ? 18f : 12f, 1.2f);
            area.glowRenderers = z.glows.ToArray();
            z.mk.Build("Area", root, false);
            z.solid.BuildCollider("Walls", root);
            z.pictures.Build("Covers", root, false);
            noGrass.Add(new Vector3(z.c.x, reach * .8f, z.c.z));
        }

        void Board(Spot z, Vector3 ground, Vector3 dir)
        {
            dir = Vector3.ProjectOnPlane(dir, Vector3.up).normalized;
            var rot = Quaternion.LookRotation(dir); var side = Vector3.Cross(Vector3.up, dir);
            var n = Mathf.Max(1, Mathf.Min(5, z.a.songList?.Length ?? 1)); const float size = .9f;
            var w = n * size * 1.25f + .4f;
            foreach (var s in new[] { -1f, 1f }) z.mk.Prism(greyWood, ground + side * s * (w / 2f - .1f) - dir * .05f, Vector3.up, .08f, 2.4f, 6);
            var c = ground + Vector3.up * 1.8f;
            z.mk.Box(plank, c - Vector3.up * .1f, new Vector3(w, size + .6f, .1f), rot);
            z.mk.Box(tin, c + Vector3.up * (size / 2f + .35f), new Vector3(w + .3f, .08f, .5f), rot * Quaternion.Euler(-12f, 0, 0));
            z.solid.Box(plank, c - Vector3.up * .1f, new Vector3(w, size + .6f, .2f), rot);
            ArtistBits.CoverWall(z.pictures, z.labels, z.ctx, z.a, c + dir * .055f + Vector3.up * .08f, dir, n, size, n, true, Ink);
        }

        /// A porch on stilts: a tin-roofed shack with a deep front porch facing the road, its back over the
        /// fishing hole, steps up, a lit doorway and windows, a lantern on the porch.
        Vector3 Porch(Spot z, float W, float D)
        {
            var ctx = z.ctx; var up = Vector3.up; var rot = Quaternion.LookRotation(z.front); const float F = 1.4f, H = 3.4f;
            var at = z.At(-2f, 0); var floor = at + up * F;
            for (var i = -1; i <= 1; i++) for (var j = -1; j <= 2; j++)
            {
                var s = at + z.side * i * (W / 2f - .3f) + z.front * (j * (D / 2f) - D / 4f);
                var g = ctx.Ground(s.x, s.z); z.mk.Prism(greyWood, new Vector3(s.x, g - .6f, s.z), up, .16f, floor.y - g + .6f, 6);
            }
            z.mk.Box(plank, floor - up * .1f + z.front * 1.2f, new Vector3(W, .2f, D + 3.4f), rot);
            z.solid.Box(plank, floor - up * .6f + z.front * 1.2f, new Vector3(W, 1f, D + 3.4f), rot);
            for (var k = 0; k < 4; k++) { var sp = at + z.front * (D / 2f + 3.2f + (3 - k) * .45f) + up * (k * .35f + .17f); z.mk.Box(plank, sp, new Vector3(2.4f, .34f, .5f), rot); z.solid.Box(plank, sp, new Vector3(2.4f, .34f, .5f), rot); }
            var wallM = WorldMaterials.Solid(new Color(.52f, .46f, .37f));
            z.mk.Box(wallM, floor + up * H / 2f - z.front * (D / 2f - .1f), new Vector3(W, H, .2f), rot);
            foreach (var s in new[] { -1f, 1f }) z.mk.Box(wallM, floor + up * H / 2f + z.side * s * (W / 2f - .1f), new Vector3(.2f, H, D - .4f), rot);
            foreach (var s in new[] { -1f, 1f }) z.mk.Box(wallM, floor + up * H / 2f + z.front * (D / 2f - .1f) + z.side * s * (W / 4f + .65f), new Vector3(W / 2f - 1.3f, H, .2f), rot);
            z.mk.Box(wallM, floor + up * (H - .45f) + z.front * (D / 2f - .1f), new Vector3(2.6f, .9f, .2f), rot);
            z.solid.Box(wallM, floor + up * H / 2f - z.front * (D / 2f - .1f), new Vector3(W, H, .2f), rot);
            foreach (var s in new[] { -1f, 1f }) z.solid.Box(wallM, floor + up * H / 2f + z.side * s * (W / 2f - .1f), new Vector3(.2f, H, D), rot);
            foreach (var s in new[] { -1f, 1f }) z.solid.Box(wallM, floor + up * H / 2f + z.front * (D / 2f - .1f) + z.side * s * (W / 4f + .65f), new Vector3(W / 2f - 1.3f, H, .2f), rot);
            foreach (var s in new[] { -1f, 1f }) z.mk.Box(GlowM(new Color(1f, .72f, .38f), 1.8f), floor + up * 1.8f + z.front * (D / 2f + .01f) + z.side * s * (W / 4f + .65f), new Vector3(1.1f, .9f, .04f), rot);
            z.mk.GableRoof(tin, floor + up * H + z.front * 1.2f, new Vector3(D + 4.2f, 0, W + 1f), 1.8f, Quaternion.LookRotation(z.side));
            foreach (var s in new[] { -1f, 1f }) { var post = floor + z.front * (D / 2f + 2.9f) + z.side * s * (W / 2f - .3f); z.mk.Prism(greyWood, post, up, .12f, H, 6); ArtistBits.PostCollider(z.root, post, H, .15f); }
            foreach (var s in new[] { -1f, 1f }) { var rp = floor + z.front * (D / 2f + 3f) + z.side * s * (W / 4f + .8f) + up * .9f; z.mk.Box(greyWood, rp, new Vector3(W / 2f - 1.6f, .1f, .1f), rot); z.solid.Box(greyWood, rp - up * .4f, new Vector3(W / 2f - 1.6f, 1f, .2f), rot); }
            IndoorSpaces.Add(floor + up * H / 2f, rot, new Vector3(W, H, D));
            ArtistBits.PointLight(z.root, floor + up * 2.6f, new Color(1f, .7f, .4f), 9f, 1.4f, true);
            var lantern = floor + z.front * (D / 2f + 2.7f) + up * (H - .5f);
            var go = new GameObject("Porch lantern"); go.transform.SetParent(z.root, false);
            var lk = new MeshKit(); lk.Box(GlowM(new Color(1f, .78f, .4f), 2.6f), Vector3.zero, new Vector3(.3f, .4f, .3f), Quaternion.identity);
            z.glows.Add(lk.BuildSingle("Glass", go.transform, false).GetComponent<Renderer>()); go.transform.position = lantern;
            halos.Add(lantern, 1.5f, new Color(1f, .75f, .35f), .8f);
            z.area.glowLight = ArtistBits.PointLight(z.root, lantern + z.front * 1.5f, new Color(1f, .72f, .4f), 16f, 1.5f);
            RockingChair(z.mk, z.solid, floor + z.front * (D / 2f + 1.5f) - z.side * (W / 2f - 1.2f), z.front);
            return floor;
        }

        /// A smaller porch on the ground for the one- and two-song artists: a deck, a lean-to roof, a lantern.
        void SmallPorch(Spot z)
        {
            var up = Vector3.up; var rot = Quaternion.LookRotation(z.front); var at = z.At(-3f, 0);
            z.mk.Box(plank, at + up * .25f, new Vector3(6f, .5f, 4.5f), rot); z.solid.Box(plank, at + up * .25f, new Vector3(6f, .5f, 4.5f), rot);
            z.mk.Box(WorldMaterials.Solid(new Color(.52f, .46f, .37f)), at + up * 1.9f - z.front * 2.1f, new Vector3(6f, 3.3f, .2f), rot);
            z.solid.Box(plank, at + up * 1.9f - z.front * 2.1f, new Vector3(6f, 3.3f, .2f), rot);
            foreach (var s in new[] { -1f, 1f }) { var post = at + z.front * 2f + z.side * s * 2.8f; z.mk.Prism(greyWood, post, up, .1f, 3f, 6); ArtistBits.PostCollider(z.root, post, 3f, .12f); }
            z.mk.Box(tin, at + up * 3.4f, new Vector3(6.4f, .08f, 5f), rot * Quaternion.Euler(-10f, 0, 0));
            var lantern = at + z.front * 2f + up * 2.7f;
            var go = new GameObject("Porch lantern"); go.transform.SetParent(z.root, false);
            var lk = new MeshKit(); lk.Box(GlowM(new Color(1f, .78f, .4f), 2.6f), Vector3.zero, new Vector3(.25f, .35f, .25f), Quaternion.identity);
            z.glows.Add(lk.BuildSingle("Glass", go.transform, false).GetComponent<Renderer>()); go.transform.position = lantern;
            halos.Add(lantern + z.front * .3f, 1.2f, new Color(1f, .75f, .35f), .8f);
        }

        // ---------------------------------------------------------------- The Marshall Tucker Band
        void MarshallTucker(Spot z)
        {
            var ctx = z.ctx; var up = Vector3.up;
            // a rainbow arching over the fishing hole behind
            var hole = z.c - z.front * 10f; hole.y = ctx.Ground(hole.x, hole.z);
            Color[] bands = { new Color(1f, .25f, .2f), new Color(1f, .6f, .15f), new Color(1f, .9f, .2f), new Color(.3f, .85f, .35f), new Color(.25f, .55f, 1f), new Color(.55f, .3f, .9f) };
            for (var b = 0; b < bands.Length; b++)
            {
                var R = 13f - b * .6f; var m = GlowM(bands[b], 1.1f);
                for (var k = 0; k < 20; k++)
                {
                    float a0 = k * Mathf.PI / 20f, a1 = (k + 1) * Mathf.PI / 20f;
                    var p0 = hole + z.side * Mathf.Cos(a0) * R + up * Mathf.Sin(a0) * R; var p1 = hole + z.side * Mathf.Cos(a1) * R + up * Mathf.Sin(a1) * R;
                    z.mk.Box(m, (p0 + p1) / 2f, new Vector3(.62f, .5f, Vector3.Distance(p0, p1) + .05f), Quaternion.LookRotation(p1 - p0, -z.front));
                }
            }
            life.rainbow = hole + up * 10f;
            // an eagle on a tall post
            var ep = z.At(3f, -10f); z.mk.Prism(greyWood, ep, up, .15f, 5f, 6); ArtistBits.PostCollider(z.root, ep, 5f, .2f);
            var brown = WorldMaterials.Solid(new Color(.35f, .25f, .15f)); var head = WorldMaterials.Solid(new Color(.95f, .95f, .92f));
            z.mk.Blob(brown, ep + up * 5.4f, new Vector3(.35f, .45f, .3f), ctx.Rng, 0f); z.mk.Blob(head, ep + up * 6f + z.front * .1f, Vector3.one * .18f, ctx.Rng, 0f);
            z.mk.Box(WorldMaterials.Solid(new Color(1f, .8f, .2f)), ep + up * 6f + z.front * .3f, new Vector3(.06f, .06f, .14f), Quaternion.LookRotation(z.front));
            foreach (var s in new[] { -1f, 1f }) z.mk.Tri(brown, ep + up * 5.6f, ep + up * 6.3f + z.side * s * 1.4f - z.front * .2f, ep + up * 5.3f + z.side * s * .9f - z.front * .3f, z.front);
            foreach (var s in new[] { -1f, 1f }) z.mk.Tri(brown, ep + up * 5.6f, ep + up * 5.3f + z.side * s * .9f - z.front * .3f, ep + up * 6.3f + z.side * s * 1.4f - z.front * .2f, -z.front);
            // a boxcar on a stretch of track
            var tc = z.At(6f, 12f); var trot = Quaternion.LookRotation(z.front);
            for (var k = -5; k <= 5; k++) z.mk.Box(bark, tc + z.front * k * 1.1f + up * .08f, new Vector3(2.6f, .16f, .3f), trot);
            foreach (var s in new[] { -.75f, .75f }) z.mk.Box(metal, tc + z.side * s + up * .22f, new Vector3(.1f, .12f, 12f), trot);
            var car = WorldMaterials.Solid(Rust);
            z.mk.Box(car, tc + up * 2.2f, new Vector3(2.8f, 2.8f, 8f), trot); z.solid.Box(car, tc + up * 1.8f, new Vector3(2.8f, 3.6f, 8f), trot);
            z.mk.Box(dark, tc + up * 2f + z.side * 1.41f, new Vector3(.02f, 2.2f, 2.2f), trot);
            foreach (var s in new[] { -1f, 1f }) foreach (var k in new[] { -2.8f, 2.8f }) z.mk.Prism(dark, tc + z.side * s * .8f + z.front * k + up * .5f, z.side * s, .4f, .15f, 10);
        }

        // ---------------------------------------------------------------- The Allman Brothers Band
        void Allmans(Spot z)
        {
            var ctx = z.ctx; var up = Vector3.up;
            // the peach truck
            var tp = z.At(4f, 9f); var dir = Quaternion.Euler(0, 20f, 0) * z.side; var rot = Quaternion.LookRotation(dir); var sd = Vector3.Cross(up, dir);
            var body = WorldMaterials.Solid(new Color(.3f, .45f, .35f), .5f);
            z.mk.Box(body, tp + up * 1.6f + dir * 2.6f, new Vector3(2.2f, 1.6f, 2f), rot);
            z.mk.Box(WorldMaterials.Solid(new Color(.15f, .2f, .22f), .9f), tp + up * 1.9f + dir * 2.6f, new Vector3(2.22f, .6f, 1.6f), rot);
            z.mk.Box(body, tp + up * 1.2f + dir * 3.9f, new Vector3(1.8f, .9f, 1.2f), rot);
            z.mk.Box(plank, tp + up * 1.1f - dir * .9f, new Vector3(2.4f, .2f, 5f), rot);
            foreach (var s in new[] { -1f, 1f }) z.mk.Box(plank, tp + up * 1.5f - dir * .9f + sd * s * 1.15f, new Vector3(.1f, .7f, 5f), rot);
            foreach (var s in new[] { -1f, 1f }) foreach (var k in new[] { -2.4f, 3f }) z.mk.Prism(dark, tp + sd * s * 1.1f + dir * k + up * .55f, sd * s, .55f, .3f, 12);
            var peach = WorldMaterials.Solid(new Color(1f, .6f, .35f), .3f); var blush = WorldMaterials.Solid(new Color(.95f, .35f, .25f), .3f); var leafG = WorldMaterials.Solid(new Color(.3f, .5f, .22f));
            for (var k = 0; k < 9; k++)
            {
                var p = tp + up * (1.9f + (k / 3) * .55f) - dir * (2.6f - (k % 3) * 1.6f) + sd * ctx.Range(-.5f, .5f);
                z.mk.Blob(k % 2 == 0 ? peach : blush, p, Vector3.one * ctx.Range(.55f, .7f), ctx.Rng, .05f);
                z.mk.Box(leafG, p + up * .6f, new Vector3(.4f, .05f, .2f), Quaternion.Euler(0, k * 40f, 20f));
            }
            z.solid.Box(body, tp + up * 1.5f + dir * .6f, new Vector3(2.4f, 3f, 7.6f), rot);
            ArtistBits.Label(z.labels, "PEACHES", tp + up * 2.3f + dir * 2.6f + sd * 1.13f, sd, .3f, new Color(.95f, .9f, .75f), TextAnchor.MiddleCenter);
            // peach trees
            for (var k = 0; k < 3; k++)
            {
                var p = z.At(-6f + k * 3f, -13f + (k % 2) * 2f); z.mk.Prism(bark, p - up * .2f, up, .18f, 2f, 6, .7f);
                soft.Blob(leafG, p + up * 2.6f, new Vector3(1.8f, 1.3f, 1.8f), ctx.Rng, .3f);
                for (var m = 0; m < 5; m++) z.mk.Blob(peach, p + up * 2.3f + new Vector3(ctx.Range(-1.4f, 1.4f), ctx.Range(-.5f, .5f), ctx.Range(-1.4f, 1.4f)), Vector3.one * .14f, ctx.Rng, 0f);
                ArtistBits.PostCollider(z.root, p, 2f, .25f);
            }
            // two drum kits and two guitars on the porch: two drummers, twin leads
            var floor = z.At(-2f, 0) + up * 1.4f + z.front * 4.4f;
            foreach (var s in new[] { -1f, 1f })
            {
                var dk = floor + z.side * s * 2.6f;
                z.mk.Prism(white, dk + up * .2f, z.front, .32f, .45f, 14);
                z.mk.Prism(WorldMaterials.Solid(new Color(.7f, .2f, .15f)), dk + up * .55f + z.side * .5f, up, .22f, .25f, 12);
                z.mk.Prism(WorldMaterials.Solid(new Color(.85f, .7f, .3f), .9f), dk + up * 1f - z.side * .5f, up, .3f, .02f, 14);
                z.mk.Box(metal, dk + up * .5f - z.side * .5f, new Vector3(.03f, 1f, .03f), Quaternion.identity);
                var g = floor + z.side * s * .8f - z.front * 1f;
                z.mk.Blob(WorldMaterials.Solid(s < 0 ? new Color(.7f, .45f, .2f) : new Color(.15f, .12f, .1f), .7f), g + up * .5f, new Vector3(.32f, .42f, .1f), ctx.Rng, 0f);
                z.mk.Box(dark, g + up * 1.2f, new Vector3(.06f, .9f, .05f), Quaternion.LookRotation(z.front));
            }
            // a sunburst sign over the steps
            var pole = z.At(-2f, 0) - z.side * 7.4f + z.front * 4.5f; z.mk.Prism(greyWood, pole - up * .3f, up, .16f, 8.9f, 6); ArtistBits.PostCollider(z.root, pole, 8.6f, .2f);
            var sun = pole + up * 8.8f + z.front * .25f;
            z.mk.Prism(GlowM(new Color(1f, .75f, .25f), 1.8f), sun, z.front, 1f, .1f, 16);
            for (var k = 0; k < 12; k++) { var d = Quaternion.AngleAxis(k * 30f, z.front) * up; z.mk.Box(GlowM(new Color(1f, .6f, .2f), 1.6f), sun + d * 1.6f + z.front * .05f, new Vector3(.18f, .7f, .05f), Quaternion.LookRotation(z.front, d)); }
            halos.Add(sun + z.front * .6f, 2.2f, new Color(1f, .7f, .25f), .7f);
            // an old headstone with candles under a small oak
            var hs = z.At(2f, -14f);
            z.mk.Box(WorldMaterials.Solid(new Color(.75f, .73f, .7f)), hs + up * .55f, new Vector3(.8f, 1.1f, .18f), Quaternion.LookRotation(z.front));
            z.mk.Prism(WorldMaterials.Solid(new Color(.75f, .73f, .7f)), hs + up * 1.1f - z.front * .09f, z.front, .4f, .18f, 12);
            for (var k = 0; k < 4; k++) { var cp = hs + z.front * .6f + z.side * ((k - 1.5f) * .3f); z.mk.Prism(white, cp, up, .04f, .2f, 6); z.mk.Blob(GlowM(new Color(1f, .7f, .35f), 2.8f), cp + up * .26f, new Vector3(.025f, .05f, .025f), ctx.Rng, 0f); }
            halos.Add(hs + z.front * .7f + up * .4f, .8f, new Color(1f, .7f, .35f), .9f);
            z.solid.Box(dark, hs + up * .6f, new Vector3(.9f, 1.2f, .3f), Quaternion.LookRotation(z.front));
            LiveOak(ctx, z.mk, z.At(4f, -18f), .9f);
        }

        // ---------------------------------------------------------------- Creedence Clearwater Revival
        void Creedence(Spot z)
        {
            var ctx = z.ctx; var up = Vector3.up;
            // the paddle steamer, moored at the nearest bend of the river with a landing stage from the bank
            var main = channels.Count > 0 ? channels[0] : null;
            if (main != null)
            {
                var bi = 0; var bd = float.MaxValue;
                for (var i = 3; i < main.pts.Count - 3; i++) { var p = main.pts[i]; if (!ctx.IsLand(p.x, p.z, 0f)) continue; var d = (p.x - z.c.x) * (p.x - z.c.x) + (p.z - z.c.z) * (p.z - z.c.z); if (d < bd) { bd = d; bi = i; } }
                var at = main.pts[bi]; var along = main.pts[bi + 2] - main.pts[bi - 2]; along.y = 0; along.Normalize();
                life.steamerIndex = bi;
                life.steamer = Steamer(z.root, at, along);
            }
            life.moonDir = -z.front;
            // a washboard and a jug on the porch, a lantern hung on a pole by the water
            var floor = z.At(-2f, 0) + up * 1.4f + z.front * 4.6f;
            z.mk.Box(metal, floor + up * .5f + z.side * 2f, new Vector3(.5f, .9f, .05f), Quaternion.LookRotation(z.front) * Quaternion.Euler(-15f, 0, 0));
            for (var k = 0; k < 6; k++) z.mk.Box(WorldMaterials.Solid(new Color(.55f, .55f, .55f), .9f), floor + up * (.3f + k * .12f) + z.side * 2f + z.front * .03f, new Vector3(.45f, .03f, .03f), Quaternion.LookRotation(z.front));
            z.mk.Blob(WorldMaterials.Solid(new Color(.7f, .55f, .35f), .5f), floor + up * .3f + z.side * 2.6f, new Vector3(.25f, .3f, .25f), ctx.Rng, 0f);
            // the swamp moon on a tall pole over the porch: a pale disc that glows
            var mp = z.At(-4f, -8f); z.mk.Prism(greyWood, mp, up, .14f, 7f, 6); ArtistBits.PostCollider(z.root, mp, 7f, .2f);
            z.mk.Prism(GlowM(new Color(1f, .92f, .7f), 1.6f), mp + up * 7.6f, z.front, 1f, .08f, 18);
            halos.Add(mp + up * 7.6f + z.front * .5f, 2f, new Color(1f, .9f, .7f), .6f);
        }

        /// A white paddle steamer: two decks with rails, twin tall black stacks, a big red stern wheel.
        Transform Steamer(Transform parent, Vector3 at, Vector3 along)
        {
            var go = new GameObject("Paddle steamer"); go.transform.SetParent(parent, false);
            var sk = new MeshKit(); var up = Vector3.up; var red = WorldMaterials.Solid(new Color(.75f, .18f, .15f));
            sk.Box(white, up * .5f, new Vector3(6f, 1.4f, 20f), Quaternion.identity);
            sk.Prism(white, up * -.2f + Vector3.forward * 10f, Vector3.forward, 3f, 2f, 3, .1f);
            sk.Box(red, up * 1.25f, new Vector3(6.05f, .15f, 20.05f), Quaternion.identity);
            sk.Box(white, up * 2.6f, new Vector3(5f, 2.6f, 14f), Quaternion.identity);
            sk.Box(white, up * 4.1f, new Vector3(6.4f, .2f, 16f), Quaternion.identity);
            sk.Box(white, up * 5.1f + Vector3.forward * 2f, new Vector3(4f, 1.8f, 7f), Quaternion.identity);
            sk.Box(white, up * 6.1f + Vector3.forward * 2f, new Vector3(4.6f, .2f, 8f), Quaternion.identity);
            sk.Box(white, up * 6.8f + Vector3.forward * 4f, new Vector3(2.4f, 1.3f, 2.4f), Quaternion.identity);
            for (var k = 0; k < 7; k++) foreach (var s in new[] { -1f, 1f }) sk.Box(WorldMaterials.Glow(new Color(1f, .8f, .45f), 1.3f), up * 2.7f + Vector3.right * s * 2.51f + Vector3.forward * (-6f + k * 2f), new Vector3(.02f, .9f, .9f), Quaternion.identity);
            foreach (var s in new[] { -1f, 1f }) { sk.Prism(WorldMaterials.Solid(new Color(.08f, .08f, .08f)), up * 6f + Vector3.right * s * 1.2f + Vector3.forward * 6.5f, up, .35f, 6f, 10); sk.Box(WorldMaterials.Solid(new Color(.85f, .7f, .3f)), up * 12.1f + Vector3.right * s * 1.2f + Vector3.forward * 6.5f, new Vector3(1f, .3f, 1f), Quaternion.identity); }
            for (var y = 0; y < 2; y++) foreach (var s in new[] { -1f, 1f }) sk.Box(white, up * (4.8f + y * 2f) + Vector3.right * s * (3.1f - y * .8f), new Vector3(.06f, .06f, 15f - y * 7f), Quaternion.identity);
            sk.BuildSingle("Hull", go.transform, false);
            var wheel = new GameObject("Stern wheel").transform; wheel.SetParent(go.transform, false); wheel.localPosition = up * 1.8f - Vector3.forward * 11.5f;
            var wk = new MeshKit();
            for (var k = 0; k < 10; k++) { var r = Quaternion.Euler(k * 36f, 0, 0); wk.Box(red, r * (Vector3.up * 1.9f), new Vector3(5f, .9f, .12f), r); }
            foreach (var s in new[] { -2.4f, 2.4f }) wk.Prism(red, Vector3.right * s, Vector3.right, 2.4f, .12f, 16);
            wk.BuildSingle("Paddles", wheel, false);
            go.transform.SetPositionAndRotation(at + up * -.4f, Quaternion.LookRotation(along));
            life.steamerWheel = wheel;
            var smoke = new GameObject("Stack light").AddComponent<Light>(); smoke.transform.SetParent(go.transform, false); smoke.transform.localPosition = up * 3f;
            smoke.type = LightType.Point; smoke.range = 16f; smoke.intensity = 1.2f; smoke.color = new Color(1f, .8f, .5f);
            return go.transform;
        }

        // ---------------------------------------------------------------- Little Feat
        void LittleFeat(Spot z)
        {
            var ctx = z.ctx; var up = Vector3.up;
            // a glowing crescent moon over the porch roof
            var mpole = z.At(-2f, 0) - z.side * 6.6f + z.front * 4f; z.mk.Prism(greyWood, mpole - up * .3f, up, .16f, 6.4f, 6); ArtistBits.PostCollider(z.root, mpole, 6f, .2f);
            var mc = mpole + up * 8.1f + z.front * .3f;
            var moon = GlowM(new Color(1f, .85f, .5f), 2f);
            for (var k = 0; k < 12; k++)
            {
                float a0 = Mathf.Lerp(50f, 310f, k / 12f) * Mathf.Deg2Rad, a1 = Mathf.Lerp(50f, 310f, (k + 1) / 12f) * Mathf.Deg2Rad;
                var w = .9f * Mathf.Sin(Mathf.Lerp(.08f, .92f, (k + .5f) / 12f) * Mathf.PI);
                var p0 = mc + z.side * Mathf.Cos(a0) * 2f + up * Mathf.Sin(a0) * 2f; var p1 = mc + z.side * Mathf.Cos(a1) * 2f + up * Mathf.Sin(a1) * 2f;
                z.mk.Box(moon, (p0 + p1) / 2f + (mc - (p0 + p1) / 2f).normalized * -.1f, new Vector3(w, .25f, Vector3.Distance(p0, p1) + .05f), Quaternion.LookRotation(p1 - p0, z.front));
            }
            halos.Add(mc + z.front * .8f, 2.8f, new Color(1f, .8f, .45f), .6f);
            ArtistBits.Label(z.labels, "CANTINA", z.At(-2f, 0) + up * 5.6f + z.front * 5.8f, z.front, .3f, new Color(1f, .8f, .5f), TextAnchor.MiddleCenter);
            // an upright piano on the porch, a big clock on the wall
            var floor = z.At(-2f, 0) + up * 1.4f + z.front * 4f;
            z.mk.Box(dark, floor + up * .7f + z.side * 2f, new Vector3(1.6f, 1.4f, .6f), Quaternion.LookRotation(z.front));
            z.mk.Box(white, floor + up * .75f + z.side * 2f + z.front * .35f, new Vector3(1.5f, .05f, .2f), Quaternion.LookRotation(z.front));
            z.solid.Box(dark, floor + up * .7f + z.side * 2f, new Vector3(1.6f, 1.4f, .7f), Quaternion.LookRotation(z.front));
            var clock = z.At(-2f, 0) + up * 4.25f + z.front * 3.27f - z.side * 2.65f;      // on the front wall, over the window
            z.mk.Prism(white, clock, z.front, .38f, .06f, 16); z.mk.Prism(plank, clock - z.front * .02f, z.front, .46f, .05f, 16);
            z.mk.Box(dark, clock + z.front * .07f + up * .13f, new Vector3(.04f, .26f, .02f), Quaternion.LookRotation(z.front));
            z.mk.Box(dark, clock + z.front * .07f + z.side * .1f, new Vector3(.2f, .04f, .02f), Quaternion.LookRotation(z.front));
        }

        // ---------------------------------------------------------------- Lynyrd Skynyrd
        void Skynyrd(Spot z)
        {
            var ctx = z.ctx; var up = Vector3.up;
            // a road sign home
            var sp = z.At(8f, -8f); z.mk.Prism(greyWood, sp, up, .1f, 2.8f, 6); ArtistBits.PostCollider(z.root, sp, 2.8f, .15f);
            z.mk.Box(WorldMaterials.Solid(new Color(.2f, .4f, .3f)), sp + up * 2.5f, new Vector3(2.8f, .6f, .06f), Quaternion.LookRotation(z.front));
            ArtistBits.Label(z.labels, "SWEET HOME", sp + up * 2.5f + z.front * .04f, z.front, .22f, new Color(.95f, .95f, .9f), TextAnchor.MiddleCenter);
            // a tall birdcage standing open, birds on the porch roof
            var cg = z.At(6f, 8f);
            var brass = WorldMaterials.Solid(new Color(.85f, .7f, .35f), .8f);
            for (var k = 0; k < 12; k++) { var d = Quaternion.Euler(0, k * 30f, 0) * Vector3.forward; if (k == 0) continue; z.mk.Box(brass, cg + d * 1.1f + up * 1.5f, new Vector3(.04f, 3f, .04f), Quaternion.identity); }
            z.mk.Prism(brass, cg, up, 1.15f, .15f, 12); z.mk.Cone(brass, cg + up * 3f, 1.15f, 1.2f, 12);
            z.mk.Box(brass, cg + Quaternion.Euler(0, 0, 0) * Vector3.forward * 1.5f + up * 1.5f + z.side * .4f, new Vector3(.04f, 1.6f, .9f), Quaternion.Euler(0, 50f, 0));
            z.solid.Prism(brass, cg, up, 1.15f, 4f, 8);
            var bird = WorldMaterials.Solid(new Color(.2f, .2f, .22f));
            var roof = z.At(-2f, 0) + up * (1.4f + 3.4f + 1.8f);
            for (var k = 0; k < 7; k++) { var p = roof + z.side * ((k - 3f) * 1.1f) + up * .1f; z.mk.Blob(bird, p, new Vector3(.12f, .14f, .22f), ctx.Rng, 0f); z.mk.Blob(bird, p + up * .15f + z.front * .15f, Vector3.one * .08f, ctx.Rng, 0f); }
            life.freeBirds = roof;
            // a wall of guitars on the porch
            var floor = z.At(-2f, 0) + up * 1.4f + z.front * 3.65f;
            for (var k = 0; k < 3; k++) { var g = floor + z.side * (1.2f + k * .7f) + up * 1.3f; z.mk.Blob(WorldMaterials.Solid(new[] { new Color(.75f, .2f, .15f), new Color(.85f, .75f, .5f), new Color(.1f, .1f, .1f) }[k], .7f), g, new Vector3(.3f, .38f, .08f), ctx.Rng, 0f); z.mk.Box(dark, g + up * .7f, new Vector3(.05f, .8f, .04f), Quaternion.LookRotation(z.front)); }
        }

        // ---------------------------------------------------------------- the smaller porches
        void Outlaws(Spot z)
        {
            var ctx = z.ctx; var up = Vector3.up; var rot = Quaternion.LookRotation(z.side);
            var hr = z.At(3f, 4f);
            foreach (var s in new[] { -1.8f, 1.8f }) { z.mk.Prism(greyWood, hr + z.side * s, up, .1f, 1.2f, 6); ArtistBits.PostCollider(z.root, hr + z.side * s, 1.2f, .12f); }
            z.mk.Box(greyWood, hr + up * 1.15f, new Vector3(.12f, .12f, 3.8f), rot);
            z.solid.Box(greyWood, hr + up * .6f, new Vector3(.2f, 1.2f, 3.8f), rot);
            var leather = WorldMaterials.Solid(new Color(.45f, .28f, .15f), .5f);
            foreach (var s in new[] { -.8f, .8f }) { z.mk.Blob(leather, hr + up * 1.3f + z.side * s, new Vector3(.35f, .25f, .45f), ctx.Rng, 0f); z.mk.Box(leather, hr + up * .9f + z.side * s, new Vector3(.6f, .6f, .03f), Quaternion.LookRotation(z.front)); }
            var sk = z.At(-3f, 0) + up * 3f - z.front * 2f; var bone = WorldMaterials.Solid(new Color(.92f, .9f, .82f));
            z.mk.Blob(bone, sk, new Vector3(.3f, .35f, .25f), ctx.Rng, 0f);
            foreach (var s in new[] { -1f, 1f }) z.mk.Prism(bone, sk + z.side * s * .2f + up * .1f, (z.side * s + up * .3f).normalized, .08f, 1.3f, 6, .2f);
            // a cowboy hat on the post
            var hat = WorldMaterials.Solid(new Color(.3f, .22f, .15f));
            z.mk.Prism(hat, hr + z.side * 1.8f + up * 1.25f, up, .38f, .04f, 12); z.mk.Prism(hat, hr + z.side * 1.8f + up * 1.29f, up, .2f, .2f, 10);
        }

        void RopeSwing(Spot z)
        {
            var ctx = z.ctx; var up = Vector3.up;
            var tree = z.At(4f, -7f); LiveOak(ctx, z.mk, tree, 1.1f);
            var hang = tree + up * 5.6f + z.side * 3.6f; var seat = hang - up * 3.6f + z.front * 1.2f;
            z.mk.Box(rope, (hang + seat) / 2f, new Vector3(.05f, .05f, Vector3.Distance(hang, seat)), Quaternion.LookRotation(seat - hang));
            z.mk.Box(plank, seat, new Vector3(.6f, .06f, .25f), Quaternion.LookRotation(z.front));
            life.ropeSwing = (hang, seat);
            ArtistBits.Label(z.labels, "HOLD ON LOOSELY", z.At(5f, 2f) + up * 1.4f, z.front, .2f, Ink, TextAnchor.MiddleCenter);
            z.mk.Box(plank, z.At(5f, 2f) + up * 1.4f - z.front * .03f, new Vector3(2.4f, .45f, .04f), Quaternion.LookRotation(z.front));
            z.mk.Prism(greyWood, z.At(5f, 2f), up, .07f, 1.2f, 6);
        }

        void BadToTheBone(Spot z)
        {
            var ctx = z.ctx; var up = Vector3.up;
            Motorcycle(ctx, z.mk, z.solid, z.At(3f, 3f), Quaternion.Euler(0, 30f, 0) * z.side, new Color(.1f, .1f, .1f));
            var bp = z.At(1f, -4f); var bone = WorldMaterials.Solid(new Color(.95f, .92f, .82f)); var rot = Quaternion.LookRotation(z.side);
            z.mk.Box(bone, bp + up * .5f, new Vector3(.6f, .6f, 3f), rot);
            foreach (var s in new[] { -1f, 1f }) foreach (var t in new[] { -.4f, .4f }) z.mk.Blob(bone, bp + up * (.5f + t) + z.side * s * 1.6f, Vector3.one * .5f, ctx.Rng, 0f);
            z.solid.Box(bone, bp + up * .5f, new Vector3(1.2f, 1.2f, 4.2f), rot);
            var stool = z.At(-1f, 3f); z.mk.Prism(metal, stool, Vector3.up, .05f, .8f, 6); z.mk.Prism(WorldMaterials.Solid(new Color(.6f, .1f, .1f)), stool + up * .8f, up, .25f, .1f, 12);
        }

        void WalkAway(Spot z)
        {
            var ctx = z.ctx; var up = Vector3.up;
            var sp = z.At(4f, -4f); z.mk.Prism(greyWood, sp, up, .08f, 2.4f, 6); ArtistBits.PostCollider(z.root, sp, 2.4f, .12f);
            var arrow = Quaternion.LookRotation(-z.side);
            z.mk.Box(white, sp + up * 2.1f - z.side * .6f, new Vector3(.05f, .4f, 1.6f), arrow);
            z.mk.Cone(white, sp + up * 2.1f - z.side * 1.5f, .3f, .35f, 4, 45f);
            ArtistBits.Label(z.labels, "WALK AWAY", sp + up * 2.1f - z.side * .6f + z.front * .04f, z.front, .16f, new Color(.15f, .15f, .15f), TextAnchor.MiddleCenter);
            var boots = WorldMaterials.Solid(new Color(.35f, .22f, .12f), .5f);
            foreach (var s in new[] { -.15f, .15f }) { var b = z.At(3f, -2.4f) + z.side * s; z.mk.Box(boots, b + up * .25f, new Vector3(.12f, .5f, .18f), Quaternion.LookRotation(z.front)); z.mk.Box(boots, b + up * .06f + z.front * .1f, new Vector3(.12f, .12f, .32f), Quaternion.LookRotation(z.front)); }
            var case_ = z.At(1.5f, 3f); z.mk.Box(WorldMaterials.Solid(new Color(.55f, .35f, .2f)), case_ + up * .3f, new Vector3(.8f, .6f, .25f), Quaternion.LookRotation(z.front)); z.mk.Box(dark, case_ + up * .66f, new Vector3(.25f, .1f, .05f), Quaternion.LookRotation(z.front));
            z.solid.Box(dark, case_ + up * .3f, new Vector3(.9f, .6f, .35f), Quaternion.LookRotation(z.front));
        }

        void RedClay(Spot z)
        {
            var ctx = z.ctx; var up = Vector3.up; var rot = Quaternion.LookRotation(z.front);
            var clay = WorldMaterials.Solid(new Color(.65f, .3f, .2f));
            z.mk.Prism(clay, z.At(3f, 0) - up * .15f, up, 3.5f, .2f, 14);
            var at = z.At(-3f, 0) + up * 3.3f + z.front * 1f;
            var swing = at - up * 2.2f;
            foreach (var s in new[] { -.8f, .8f }) z.mk.Box(rope, at + z.side * s - up * 1.1f, new Vector3(.03f, 2.2f, .03f), rot);
            z.mk.Box(plank, swing + up * .02f, new Vector3(1.8f, .08f, .5f), rot); z.mk.Box(plank, swing + up * .35f - z.front * .25f, new Vector3(1.8f, .5f, .05f), rot);
            z.solid.Box(plank, swing + up * .2f, new Vector3(1.9f, .6f, .6f), rot);
            var f = swing + up * .12f + z.side * .3f;
            z.mk.Blob(WorldMaterials.Solid(new Color(.55f, .3f, .15f), .8f), f, new Vector3(.18f, .05f, .3f), ctx.Rng, 0f);
            z.mk.Box(dark, f + z.front * .35f, new Vector3(.04f, .03f, .3f), rot);
        }
    }
}
