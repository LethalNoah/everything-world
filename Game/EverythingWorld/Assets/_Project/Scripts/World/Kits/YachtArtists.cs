using System.Collections.Generic;
using UnityEngine;

namespace EverythingWorld.World
{
    /// The Riviera's artists, each with a waterfront-classy set piece, a plaque, covers on a board and a lamp:
    /// - Elton John: a yellow brick road leading to a sequinned grand piano, a rocket on a pad ("Rocket Man"), a
    ///   tiny dancer turning on a music box, a crocodile in sunglasses.
    /// - Fleetwood Mac: an arch of two giant curved tusks ("Tusk"), a glowing crystal ball, a tumble of rocks.
    /// - Billy Joel: a little piano bar, its sign glowing, a piano and stools inside. Eagles: a mission-style hotel
    ///   with a bell tower and arches, its windows lit. Jimmy Buffett: a thatched tiki bar, a giant margarita, a
    ///   giant cheeseburger, a parrot on a perch. TOTO: a snow-capped mountain over a ring of drums. Kenny Loggins: a
    ///   jet on a pedestal. Phil Collins: a drum kit with a gong. Starship: a city skyline. Chicago: a clock at 3:35.
    ///   Steely Dan: a lantern pavilion. America: a convertible by a highway sign. Stevie Nicks: a white-winged dove.
    ///   Everyone else: a white grand piano on a teak deck under a parasol.
    public partial class YachtKit
    {
        class Plot
        {
            public LandContext ctx; public Transform root, labels; public MeshKit mk, solid, pictures;
            public IslandWorldData.Artist a; public Vector3 c, front, side; public List<Renderer> glows; public ArtistArea area;
            public Vector3 At(float fw, float rt) { var p = c + front * fw + side * rt; return ctx.OnGround(p.x, p.z); }
        }

        public override void BuildArtist(LandContext ctx, IslandWorldData.Artist a)
        {
            if (white == null) MakeMaterials();
            var root = new GameObject("Artist: " + a.name).transform; root.SetParent(ctx.Root, false);
            var front = facing.TryGetValue(a.name, out var f) ? f : new Vector3(center.x - a.x, 0, center.z - a.z).normalized;
            if (front.sqrMagnitude < .01f) front = Vector3.forward;
            var labels = new GameObject("Labels").transform; labels.SetParent(root, false);
            var area = root.gameObject.AddComponent<ArtistArea>();
            area.artistName = a.name; area.stationId = a.stationId; area.radius = a.r; area.glowColor = Sunset;
            var z = new Plot { ctx = ctx, root = root, labels = labels, mk = new MeshKit(), solid = new MeshKit(), pictures = new MeshKit(), a = a, c = ctx.OnGround(a.x, a.z), front = front, side = Vector3.Cross(Vector3.up, front), glows = new List<Renderer>(), area = area };
            z.mk.Prism(stone, z.c - Vector3.up * .3f, Vector3.up, Foot(a) * .7f, .4f, 20);
            switch (a.name)
            {
                case "Elton John": Elton(z); break;
                case "Fleetwood Mac": Tusk(z); break;
                case "Billy Joel": PianoBar(z); break;
                case "Eagles": Hotel(z); break;
                case "Jimmy Buffett": Tiki(z); break;
                case "TOTO": Kilimanjaro(z); break;
                case "Kenny Loggins": Jet(z); break;
                case "Phil Collins": Drums(z); break;
                case "Starship": Skyline(z); break;
                case "Chicago": Clock(z); break;
                case "Steely Dan": Pavilion(z); break;
                case "America": Convertible(z); break;
                case "Stevie Nicks": Dove(z); break;
                default: PianoDeck(z); break;
            }
            var reach = Foot(a);
            Board(z, z.At(reach * .85f, -reach * .5f - 2.5f), front);
            ArtistBits.Plaque(z.mk, z.solid, labels, a, z.At(reach * .85f + 1.5f, reach * .5f + 2.5f), front, teak, navy, Ink, 1.6f);
            Lamp(ctx, z.mk, z.At(reach * .85f, reach * .5f + 5f));
            if (z.area.glowLight == null) z.area.glowLight = ArtistBits.PointLight(root, z.c + Vector3.up * 5f + front * 4f, Sunset, HasStation(a) ? 18f : 12f, 1.4f);
            area.glowRenderers = z.glows.ToArray();
            z.mk.Build("Area", root, false);
            z.solid.BuildCollider("Walls", root);
            z.pictures.Build("Covers", root, false);
            noGrass.Add(new Vector3(z.c.x, reach * .9f, z.c.z));
        }

        void Board(Plot z, Vector3 ground, Vector3 dir)
        {
            dir = Vector3.ProjectOnPlane(dir, Vector3.up).normalized; var rot = Quaternion.LookRotation(dir); var side = Vector3.Cross(Vector3.up, dir);
            var n = Mathf.Max(1, Mathf.Min(6, z.a.songList?.Length ?? 1)); const float size = .9f; var w = n * size * 1.25f + .4f;
            foreach (var s in new[] { -1f, 1f }) z.mk.Prism(white, ground + side * s * (w / 2f - .1f) - dir * .05f, Vector3.up, .08f, 2.4f, 6);
            var c = ground + Vector3.up * 1.8f;
            z.mk.Box(navy, c - Vector3.up * .1f, new Vector3(w, size + .6f, .1f), rot); z.solid.Box(navy, c - Vector3.up * .1f, new Vector3(w, size + .6f, .2f), rot);
            z.mk.Box(white, c + Vector3.up * (size / 2f + .36f), new Vector3(w + .2f, .1f, .14f), rot);
            ArtistBits.CoverWall(z.pictures, z.labels, z.ctx, z.a, c + dir * .055f + Vector3.up * .08f, dir, n, size, n, true, Ink);
        }

        Transform Mover(Plot z, string name, MeshKit k, Vector3 at, Quaternion rot)
        {
            var t = new GameObject(name).transform; t.SetParent(z.root, false);
            k.BuildSingle(name, t, false); t.SetPositionAndRotation(at, rot); return t;
        }

        void Elton(Plot z)
        {
            var up = Vector3.up; var ctx = z.ctx; var gold = WorldMaterials.Solid(new Color(1f, .82f, .2f), .6f);
            for (var k = 0; k < 10; k++) for (var b = 0; b < 3; b++) z.mk.Box(gold, z.At(9f - k * 1.2f, (b - 1f) * 1f + ((k % 2) * .5f)) + up * .05f, new Vector3(.95f, .1f, 1.1f), Quaternion.LookRotation(z.front));
            var pp = z.At(-4f, 0) + up * .1f; var seq = WorldMaterials.Solid(new Color(.9f, .5f, .95f), .98f);
            Piano(ctx, z.mk, z.solid, pp, z.front);
            for (var k = 0; k < 24; k++) z.mk.Blob(GlowM(new[] { new Color(1f, .5f, .9f), new Color(.6f, .9f, 1f), new Color(1f, .9f, .4f) }[k % 3], 2.2f), pp + up * (.9f + (k % 4) * .1f) + z.side * ((k % 6) * .3f - .8f) + z.front * ((k / 6) * .4f - .6f), Vector3.one * .07f, ctx.Rng, 0f);
            // the rocket
            var pad = z.At(-6f, 9f); z.mk.Box(stone, pad + up * .2f, new Vector3(4f, .4f, 4f), Quaternion.identity); z.solid.Box(stone, pad + up * .2f, new Vector3(4f, .4f, 4f), Quaternion.identity);
            var rk = new MeshKit(); rk.Prism(white, Vector3.up * .5f, Vector3.up, .8f, 7f, 14); rk.Cone(WorldMaterials.Solid(new Color(.85f, .2f, .3f)), Vector3.up * 7.5f, .8f, 2.4f, 14);
            foreach (var d in new[] { Vector3.forward, Vector3.back, Vector3.left, Vector3.right }) { rk.Tri(WorldMaterials.Solid(new Color(.85f, .2f, .3f)), d * .8f + Vector3.up * .5f, d * .8f + Vector3.up * 3f, d * 2f, Vector3.Cross(d, Vector3.up) * 5f); rk.Tri(WorldMaterials.Solid(new Color(.85f, .2f, .3f)), d * .8f + Vector3.up * .5f, d * 2f, d * .8f + Vector3.up * 3f, -Vector3.Cross(d, Vector3.up) * 5f); }
            rk.Prism(WorldMaterials.Glow(new Color(.6f, .85f, 1f), 1.6f), Vector3.up * 5.5f + Vector3.forward * .78f, Vector3.forward, .28f, .05f, 12);
            life.rocket = Mover(z, "Rocket", rk, pad + up * .4f, Quaternion.identity); life.rocketPad = pad + up * .4f;
            // the tiny dancer on a music box
            var mb = z.At(3f, -8f);
            z.mk.Box(WorldMaterials.Solid(new Color(.95f, .7f, .8f)), mb + up * .6f, new Vector3(1.6f, 1.2f, 1.2f), Quaternion.LookRotation(z.front)); z.solid.Box(dark, mb + up * .6f, new Vector3(1.6f, 1.2f, 1.2f), Quaternion.LookRotation(z.front));
            var dk = new MeshKit(); var tutu = WorldMaterials.Solid(new Color(1f, .85f, .9f));
            dk.Prism(tutu, Vector3.up * .9f, Vector3.up, .6f, .12f, 14); dk.Box(tutu, Vector3.up * 1.3f, new Vector3(.18f, .7f, .14f), Quaternion.identity); dk.Blob(WorldMaterials.Solid(new Color(.95f, .8f, .7f)), Vector3.up * 1.8f, Vector3.one * .14f, new System.Random(1), 0f);
            dk.Box(WorldMaterials.Solid(new Color(.95f, .8f, .7f)), Vector3.up * .5f, new Vector3(.06f, .8f, .06f), Quaternion.identity); dk.Box(WorldMaterials.Solid(new Color(.95f, .8f, .7f)), Vector3.up * 1.7f + Vector3.right * .2f, new Vector3(.4f, .05f, .05f), Quaternion.Euler(0, 0, 40f));
            life.dancer = Mover(z, "Tiny dancer", dk, mb + up * 1.2f, Quaternion.identity);
            // a crocodile in sunglasses
            var cr = z.At(4f, 7f); var green = WorldMaterials.Solid(new Color(.3f, .55f, .3f), .5f);
            z.mk.Blob(green, cr + up * .5f, new Vector3(.7f, .45f, 2f), ctx.Rng, .1f); z.mk.Blob(green, cr + up * .55f + z.front * 2f, new Vector3(.4f, .3f, .9f), ctx.Rng, 0f);
            z.mk.Prism(green, cr + up * .4f - z.front * 2f, -z.front, .35f, 1.6f, 8, .2f);
            z.mk.Box(dark, cr + up * .85f + z.front * 1.7f, new Vector3(.8f, .15f, .1f), Quaternion.LookRotation(z.front));
            z.solid.Box(green, cr + up * .5f, new Vector3(1.4f, 1f, 5f), Quaternion.LookRotation(z.front));
        }

        void Tusk(Plot z)
        {
            var up = Vector3.up; var ivory = WorldMaterials.Solid(new Color(.95f, .92f, .82f), .6f); var c = z.At(-2f, 0);
            foreach (var s in new[] { -1f, 1f })
                for (var k = 0; k < 12; k++)
                {
                    float t0 = k / 12f, t1 = (k + 1) / 12f;
                    Vector3 P(float t) => c + z.side * s * (3.6f - t * 3.2f) + up * Mathf.Sin(t * Mathf.PI * .55f) * 9f;
                    z.mk.Prism(ivory, P(t0), (P(t1) - P(t0)).normalized, .7f * (1f - t0 * .8f), Vector3.Distance(P(t0), P(t1)) + .05f, 10);
                }
            foreach (var s in new[] { -1f, 1f }) ArtistBits.PostCollider(z.root, c + z.side * s * 3.6f, 3f, .8f);
            var ball = z.At(4f, 6f); z.mk.Prism(WorldMaterials.Solid(new Color(.4f, .25f, .45f)), ball, up, .4f, 1.2f, 10, .7f);
            z.mk.Blob(GlowM(new Color(.75f, .6f, 1f), 1.8f), ball + up * 1.6f, Vector3.one * .45f, z.ctx.Rng, 0f); halos.Add(ball + up * 1.6f, 1.6f, new Color(.75f, .6f, 1f), .7f);
            ArtistBits.PostCollider(z.root, ball, 1.6f, .4f);
            for (var k = 0; k < 6; k++) z.mk.Blob(stone, z.At(5f, -6f) + up * (.5f + (k / 3) * .8f) + z.side * ((k % 3) * 1.1f), new Vector3(.8f, .6f, .7f), z.ctx.Rng, .35f);
            z.solid.Box(stone, z.At(5f, -5f) + up * .8f, new Vector3(3.6f, 1.6f, 1.6f), Quaternion.LookRotation(z.front));
        }

        void PianoBar(Plot z)
        {
            var up = Vector3.up; var c = z.At(-3f, 0); var rot = Quaternion.LookRotation(z.front); var wall = WorldMaterials.Solid(new Color(.35f, .2f, .18f));
            const float W = 9f, D = 7f, H = 3.6f;
            void Wall(Vector3 p, Vector3 s) { z.mk.Box(wall, p, s, rot); z.solid.Box(wall, p, s, rot); }
            Wall(c + up * H / 2f - z.front * (D / 2f), new Vector3(W, H, .25f));
            foreach (var s in new[] { -1f, 1f }) Wall(c + up * H / 2f + z.side * s * (W / 2f), new Vector3(.25f, H, D));
            foreach (var s in new[] { -1f, 1f }) Wall(c + up * H / 2f + z.front * (D / 2f) + z.side * s * (W / 4f + .7f), new Vector3(W / 2f - 1.4f, H, .25f));
            z.mk.Box(wall, c + up * (H + .1f), new Vector3(W + .4f, .3f, D + .4f), rot);
            z.mk.Box(GlowM(new Color(1f, .45f, .5f), 2.2f), c + up * (H + .9f) + z.front * (D / 2f), new Vector3(5f, .9f, .1f), rot);
            ArtistBits.Label(z.labels, "PIANO BAR", c + up * (H + .9f) + z.front * (D / 2f + .06f), z.front, .4f, new Color(.2f, .05f, .08f), TextAnchor.MiddleCenter);
            Piano(z.ctx, z.mk, z.solid, c - z.front * 1.5f + up * .05f, z.side);
            z.mk.Box(WorldMaterials.Solid(new Color(.5f, .3f, .2f), .7f), c + up * .55f + z.side * 3f, new Vector3(1f, 1.1f, 4f), rot); z.solid.Box(dark, c + up * .55f + z.side * 3f, new Vector3(1f, 1.1f, 4f), rot);
            for (var k = 0; k < 3; k++) { var st = c + z.side * 2f + z.front * ((k - 1f) * 1.2f); z.mk.Prism(metal, st, up, .04f, .75f, 6); z.mk.Prism(WorldMaterials.Solid(new Color(.6f, .1f, .12f)), st + up * .75f, up, .25f, .1f, 10); }
            ArtistBits.PointLight(z.root, c + up * 3f, new Color(1f, .7f, .5f), 9f, 1.6f, true);
            IndoorSpaces.Add(c + up * H / 2f, rot, new Vector3(W, H, D));
        }

        void Hotel(Plot z)
        {
            var up = Vector3.up; var c = z.At(-4f, 0); var rot = Quaternion.LookRotation(z.front); var adobe = WorldMaterials.Solid(new Color(.95f, .88f, .75f)); var roof = WorldMaterials.Solid(new Color(.78f, .38f, .25f));
            z.mk.Box(adobe, c + up * 3f, new Vector3(14f, 6f, 6f), rot); z.solid.Box(adobe, c + up * 3f, new Vector3(14f, 6f, 6f), rot);
            z.mk.Box(roof, c + up * 6.2f, new Vector3(14.6f, .4f, 6.6f), rot);
            for (var k = 0; k < 5; k++) { var p = c + up * 1.4f + z.front * 3.4f + z.side * ((k - 2f) * 2.8f); z.mk.Box(adobe, p, new Vector3(.6f, 2.8f, .6f), rot); }
            z.mk.Box(adobe, c + up * 3f + z.front * 3.4f, new Vector3(14f, .5f, .8f), rot);
            var win = new Material(GlowM(new Color(1f, .7f, .4f), 1.4f)) { name = "Hotel windows" };
            for (var k = 0; k < 6; k++) z.mk.Box(win, c + up * 4.5f + z.front * 3.01f + z.side * ((k - 2.5f) * 2.2f), new Vector3(1f, 1.2f, .02f), rot);
            life.hotelWindows = win;
            var tw = c + z.side * 8.5f; z.mk.Box(adobe, tw + up * 5f, new Vector3(3f, 10f, 3f), rot); z.solid.Box(adobe, tw + up * 5f, new Vector3(3f, 10f, 3f), rot);
            z.mk.Box(dark, tw + up * 8.5f + z.front * 1.51f, new Vector3(1.2f, 1.6f, .02f), rot); z.mk.Prism(WorldMaterials.Solid(new Color(.85f, .7f, .35f), .9f), tw + up * 8f + z.front * 1.2f, up, .4f, .7f, 10, .5f);
            z.mk.Cone(roof, tw + up * 10f, 2.2f, 1.6f, 4, 45f);
            z.mk.Box(dark, c + up * 7.6f + z.front * 2.9f, new Vector3(8f, 1f, .2f), rot);
            ArtistBits.Label(z.labels, "HOTEL CALIFORNIA", c + up * 7.6f + z.front * 3.01f, z.front, .45f, new Color(1f, .45f, .35f), TextAnchor.MiddleCenter);
            halos.Add(c + up * 7.6f + z.front * 4f, 4f, new Color(1f, .45f, .35f), .4f);
            for (var k = 0; k < 3; k++) Palm(z.ctx, z.mk, z.At(4f, -8f + k * 8f), 8f);
        }

        void Tiki(Plot z)
        {
            var up = Vector3.up; var ctx = z.ctx; var c = z.At(-3f, 0); var rot = Quaternion.LookRotation(z.front); var thatch = WorldMaterials.Solid(new Color(.75f, .6f, .35f));
            foreach (var s in new[] { -1f, 1f }) foreach (var b in new[] { -1f, 1f }) { var p = c + z.side * s * 3f + z.front * b * 2f; z.mk.Prism(palmBark, p, up, .18f, 3.4f, 6); ArtistBits.PostCollider(z.root, p, 3.4f, .2f); }
            z.mk.Cone(thatch, c + up * 3.3f, 5f, 2.4f, 8);
            z.mk.Box(teak, c + up * .6f + z.front * 1.4f, new Vector3(5f, 1.2f, .9f), rot); z.solid.Box(teak, c + up * .6f + z.front * 1.4f, new Vector3(5f, 1.2f, .9f), rot);
            var tiki = new Material(GlowM(new Color(.3f, 1f, .8f), 1.4f)) { name = "Tiki lights" };
            for (var k = 0; k < 10; k++) z.mk.Blob(tiki, c + up * 3.2f + Quaternion.Euler(0, k * 36f, 0) * Vector3.forward * 4.4f, Vector3.one * .14f, ctx.Rng, 0f);
            life.tiki = tiki;
            // the giant margarita
            var mg = z.At(4f, -6f); var glass = WorldMaterials.Glow(new Color(.75f, 1f, .7f), .4f);
            z.mk.Prism(glass, mg, up, 1f, .2f, 14); z.mk.Prism(glass, mg + up * .2f, up, .15f, 2f, 8); z.mk.Prism(glass, mg + up * 2.2f, up, .2f, 1.6f, 16, 10f);
            z.mk.Prism(WorldMaterials.Solid(new Color(.95f, .95f, .95f)), mg + up * 3.75f, up, 2.05f, .1f, 16);
            z.mk.Prism(WorldMaterials.Solid(new Color(.4f, .8f, .3f)), mg + up * 3.6f + z.side * 1.8f, z.front, .5f, .1f, 12);
            ArtistBits.PostCollider(z.root, mg, 3.8f, 1f);
            // the giant cheeseburger
            var cb = z.At(4f, 6f); var bun = WorldMaterials.Solid(new Color(.85f, .6f, .3f));
            z.mk.Prism(bun, cb, up, 1.6f, .5f, 16); z.mk.Prism(WorldMaterials.Solid(new Color(.4f, .25f, .15f)), cb + up * .5f, up, 1.65f, .45f, 16);
            z.mk.Prism(WorldMaterials.Solid(new Color(1f, .8f, .2f)), cb + up * .95f, up, 1.75f, .08f, 4, 1f, 45f); z.mk.Prism(WorldMaterials.Solid(new Color(.4f, .75f, .3f)), cb + up * 1.03f, up, 1.75f, .08f, 14);
            z.mk.Blob(bun, cb + up * 1.5f, new Vector3(1.6f, .65f, 1.6f), ctx.Rng, 0f);
            z.solid.Prism(bun, cb, up, 1.6f, 2f, 10);
            // a parrot on a perch
            var pr = z.At(1f, -3.5f); z.mk.Prism(teak, pr, up, .06f, 2f, 6); z.mk.Box(teak, pr + up * 2f, new Vector3(.8f, .06f, .06f), rot);
            z.mk.Blob(WorldMaterials.Solid(new Color(.9f, .15f, .15f)), pr + up * 2.3f, new Vector3(.18f, .28f, .18f), ctx.Rng, 0f); z.mk.Blob(WorldMaterials.Solid(new Color(.2f, .5f, .95f)), pr + up * 2.2f - z.front * .1f, new Vector3(.12f, .3f, .1f), ctx.Rng, 0f);
            z.mk.Blob(WorldMaterials.Solid(new Color(1f, .85f, .2f)), pr + up * 2.55f + z.front * .05f, Vector3.one * .1f, ctx.Rng, 0f);
            ArtistBits.Label(z.labels, "MARGARITAVILLE", c + up * 2.4f + z.front * 1.9f, z.front, .3f, new Color(.2f, 1f, .8f), TextAnchor.MiddleCenter);
        }

        void Kilimanjaro(Plot z)
        {
            var up = Vector3.up; var c = z.At(-5f, 0);
            z.mk.Tri(WorldMaterials.Solid(new Color(.45f, .4f, .38f)), c - z.side * 7f, c + z.side * 7f, c + up * 7f, c - z.front);
            z.mk.Tri(white, c + up * 5f - z.side * 2f + z.front * .02f, c + up * 5f + z.side * 2f + z.front * .02f, c + up * 7f + z.front * .02f, c - z.front);
            z.solid.Box(dark, c + up * 2f, new Vector3(13f, 4f, .5f), Quaternion.LookRotation(z.front));
            var drum = new Material(GlowM(new Color(1f, .6f, .3f), .5f)) { name = "Drums" }; life.drums = drum;
            for (var k = 0; k < 5; k++) { var d = Quaternion.Euler(0, k * 72f, 0) * z.front; var p = z.At(2f, 0) + d * 2.4f; z.mk.Prism(WorldMaterials.Solid(new Color(.55f, .35f, .2f)), p, Vector3.up, .5f, 1f, 12, .8f); z.mk.Prism(drum, p + up * 1f, Vector3.up, .42f, .04f, 12); ArtistBits.PostCollider(z.root, p, 1f, .5f); }
        }

        void Jet(Plot z)
        {
            var up = Vector3.up; var c = z.At(-3f, 0); var grey = WorldMaterials.Solid(new Color(.55f, .58f, .62f), .7f);
            z.mk.Box(stone, c + up * 1.5f, new Vector3(1f, 3f, 1f), Quaternion.identity); z.solid.Box(stone, c + up * 1.5f, new Vector3(1f, 3f, 1f), Quaternion.identity);
            var j = c + up * 4f; var dir = (z.side + up * .3f).normalized;
            z.mk.Prism(grey, j - dir * 4f, dir, .7f, 7f, 10, .4f); z.mk.Tri(grey, j - dir * 1f + Vector3.Cross(up, dir) * 4f, j + dir * 1.5f, j - dir * 2.5f, j + up);
            z.mk.Tri(grey, j - dir * 1f - Vector3.Cross(up, dir) * 4f, j - dir * 2.5f, j + dir * 1.5f, j + up);
            z.mk.Tri(grey, j - dir * 3.5f, j - dir * 4f + up * 2f, j - dir * 2.5f, j + Vector3.Cross(up, dir));
        }

        void Drums(Plot z)
        {
            var up = Vector3.up; var c = z.At(-3f, 0);
            z.mk.Prism(WorldMaterials.Solid(new Color(.8f, .1f, .12f), .7f), c + up * 1.2f - z.front * .8f, z.front, 1.2f, 1.6f, 16);
            foreach (var s in new[] { -1f, 1f }) z.mk.Prism(WorldMaterials.Solid(new Color(.8f, .1f, .12f), .7f), c + up * 1.4f + z.side * s * 1.6f, up, .6f, .7f, 14);
            z.mk.Prism(WorldMaterials.Solid(new Color(.9f, .75f, .35f), .9f), c + up * 4f - z.front * 2f, z.front, 1.6f, .1f, 20); z.mk.Box(dark, c + up * 2f - z.front * 2f, new Vector3(3.6f, .2f, .2f), Quaternion.LookRotation(z.front));
            z.solid.Box(dark, c + up * 1.2f, new Vector3(4f, 2.4f, 3f), Quaternion.LookRotation(z.front));
        }

        void Skyline(Plot z)
        {
            var up = Vector3.up; var c = z.At(-4f, 0);
            for (var k = 0; k < 7; k++) { var h = 3f + (k * 37 % 7); var p = c + z.side * ((k - 3f) * 1.6f); z.mk.Box(WorldMaterials.Solid(new Color(.25f, .25f, .35f)), p + up * h / 2f, new Vector3(1.4f, h, 1.4f), Quaternion.LookRotation(z.front)); for (var w = 1; w < h; w++) z.mk.Box(GlowM(new Color(1f, .85f, .55f), 1.6f), p + up * w + z.front * .71f, new Vector3(.8f, .3f, .02f), Quaternion.LookRotation(z.front)); }
            z.solid.Box(dark, c + up * 4f, new Vector3(11f, 8f, 1.4f), Quaternion.LookRotation(z.front));
        }

        void Clock(Plot z)
        {
            var up = Vector3.up; var c = z.At(-3f, 0); var rot = Quaternion.LookRotation(z.front);
            z.mk.Prism(metal, c, up, .2f, 5f, 8); ArtistBits.PostCollider(z.root, c, 5f, .3f);
            z.mk.Prism(white, c + up * 6f - z.front * .1f, z.front, 1.4f, .2f, 20); z.mk.Prism(navy, c + up * 6f - z.front * .15f, z.front, 1.55f, .2f, 20);
            z.mk.Box(dark, c + up * 6f + z.front * .12f + Quaternion.AngleAxis(-105f, z.front) * up * .45f, new Vector3(.1f, .9f, .03f), Quaternion.LookRotation(z.front, Quaternion.AngleAxis(-105f, z.front) * up));
            z.mk.Box(dark, c + up * 6f + z.front * .12f + Quaternion.AngleAxis(-210f, z.front) * up * .6f, new Vector3(.07f, 1.2f, .03f), Quaternion.LookRotation(z.front, Quaternion.AngleAxis(-210f, z.front) * up));
        }

        void Pavilion(Plot z)
        {
            var up = Vector3.up; var c = z.At(-3f, 0); var red = WorldMaterials.Solid(new Color(.75f, .15f, .12f));
            foreach (var s in new[] { -1f, 1f }) foreach (var b in new[] { -1f, 1f }) { var p = c + z.side * s * 2.4f + z.front * b * 2.4f; z.mk.Prism(red, p, up, .15f, 3f, 6); ArtistBits.PostCollider(z.root, p, 3f, .2f); }
            z.mk.Cone(dark, c + up * 3f, 4.2f, 1.6f, 4, 45f);
            for (var k = 0; k < 4; k++) { var p = c + up * 2.5f + Quaternion.Euler(0, k * 90f + 45f, 0) * Vector3.forward * 2.8f; z.mk.Prism(GlowM(new Color(1f, .5f, .3f), 2f), p - up * .3f, up, .25f, .6f, 10, .8f); halos.Add(p, 1f, new Color(1f, .5f, .3f), .7f); }
        }

        void Convertible(Plot z)
        {
            var up = Vector3.up; var at = z.At(-1f, 0); var dir = z.side; var rot = Quaternion.LookRotation(dir); var side = Vector3.Cross(up, dir);
            var body = WorldMaterials.Solid(new Color(.3f, .7f, .8f), .8f);
            z.mk.Box(body, at + up * .7f, new Vector3(1.9f, .6f, 4.6f), rot); z.mk.Box(WorldMaterials.Solid(new Color(.95f, .9f, .8f)), at + up * 1f - dir * .3f, new Vector3(1.7f, .2f, 1.6f), rot);
            z.mk.Box(WorldMaterials.Solid(new Color(.2f, .25f, .3f), .9f), at + up * 1.25f + dir * .7f, new Vector3(1.7f, .45f, .05f), rot * Quaternion.Euler(-25f, 0, 0));
            foreach (var s in new[] { -1f, 1f }) foreach (var zz in new[] { -1.5f, 1.5f }) z.mk.Prism(dark, at + side * s * .9f + dir * zz + up * .38f, side * s, .38f, .25f, 12);
            z.solid.Box(body, at + up * .8f, new Vector3(2f, 1.6f, 4.8f), rot);
            var sp = z.At(4f, -5f); z.mk.Prism(metal, sp, up, .06f, 2.6f, 6); z.mk.Box(WorldMaterials.Solid(new Color(.15f, .45f, .25f)), sp + up * 2.4f, new Vector3(2.6f, .6f, .05f), Quaternion.LookRotation(z.front));
            ArtistBits.Label(z.labels, "VENTURA HIGHWAY", sp + up * 2.4f + z.front * .04f, z.front, .16f, Ink, TextAnchor.MiddleCenter);
        }

        void Dove(Plot z)
        {
            var up = Vector3.up; var c = z.At(-2f, 0); var w = WorldMaterials.Solid(new Color(.97f, .97f, .97f));
            z.mk.Prism(stone, c, up, .4f, 2f, 10); ArtistBits.PostCollider(z.root, c, 2f, .4f);
            var b = c + up * 3f; z.mk.Blob(w, b, new Vector3(.5f, .45f, .9f), z.ctx.Rng, 0f); z.mk.Blob(w, b + up * .4f + z.front * .6f, Vector3.one * .3f, z.ctx.Rng, 0f);
            foreach (var s in new[] { -1f, 1f }) { z.mk.Tri(w, b, b + z.side * s * 2.6f + up * 1.4f, b + z.side * s * 1.6f - z.front * .6f, b - up); z.mk.Tri(w, b, b + z.side * s * 1.6f - z.front * .6f, b + z.side * s * 2.6f + up * 1.4f, b + up); }
            halos.Add(b, 2.4f, Ink, .4f);
        }

        void PianoDeck(Plot z)
        {
            var up = Vector3.up; var c = z.At(-2f, 0); var rot = Quaternion.LookRotation(z.front);
            z.mk.Box(teak, c + up * .2f, new Vector3(6f, .4f, 5f), rot); z.solid.Box(teak, c + up * .2f, new Vector3(6f, .4f, 5f), rot);
            Piano(z.ctx, z.mk, z.solid, c + up * .4f, z.front);
            var col = new[] { new Color(1f, .45f, .45f), new Color(.3f, .55f, .9f), new Color(1f, .8f, .3f) }[Mathf.Abs(z.a.name.GetHashCode()) % 3];
            z.mk.Prism(white, c + z.side * 2.4f + up * .4f, up, .05f, 2.8f, 6); z.mk.Cone(WorldMaterials.Solid(col, .3f), c + z.side * 2.4f + up * 3f, 1.8f, .8f, 8);
        }
    }
}
