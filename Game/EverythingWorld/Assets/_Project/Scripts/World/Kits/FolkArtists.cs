using System.Collections.Generic;
using UnityEngine;

namespace EverythingWorld.World
{
    /// The Folk Woods' artists, each a quiet clearing with a plaque, covers on a board and a lantern:
    /// - Fenne Lily: three freestanding doorframes in a row, each with a lit room glimpsed through it ("Room to
    ///   Room"), a cupboard with its door ajar. Yusuf / Cat Stevens: two chairs under a tree, an old man's and a
    ///   boy's ("Father and Son"), a hand-painted globe ("Wild World").
    /// - Gordon Lightfoot: a model lake freighter on a stand. John Denver: a little range of snow-capped mountains
    ///   and a "COUNTRY ROADS" sign. Simon & Garfunkel: a park bench under a lamp. The Band: a big pink house.
    ///   Kate Bush: a hill with a path running up it. Warren Zevon: a werewolf. Don McLean: a pie on a table and a
    ///   Chevy. Harry Chapin: a cradle and a cat. Carole King: a split boulder. Martin Luther King, Jr.: a quiet
    ///   stone lectern on steps. Everyone else: a porch rocker, a stump with a harmonica, a canoe rack or a swing.
    public partial class FolkKit
    {
        class Plot
        {
            public LandContext ctx; public Transform root, labels; public MeshKit mk, solid, pictures;
            public IslandWorldData.Artist a; public Vector3 c, front, side; public List<Renderer> glows; public ArtistArea area;
            public Vector3 At(float fw, float rt) { var p = c + front * fw + side * rt; return ctx.OnGround(p.x, p.z); }
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
            switch (a.name)
            {
                case "Fenne Lily": Rooms(z); break;
                case "Yusuf / Cat Stevens": FatherAndSon(z); break;
                case "Gordon Lightfoot": Freighter(z); break;
                case "John Denver": Mountains(z); break;
                case "Simon & Garfunkel": Bench(z); break;
                case "The Band": PinkHouse(z); break;
                case "Kate Bush": Hill(z); break;
                case "Warren Zevon": Werewolf(z); break;
                case "Don McLean": Pie(z); break;
                case "Harry Chapin": Cradle(z); break;
                case "Carole King": Boulder(z); break;
                case "Martin Luther King, Jr.": Lectern(z); break;
                default: Small(z); break;
            }
            var reach = Foot(a);
            Board(z, z.At(reach * .85f, -reach * .5f - 2.5f), front);
            ArtistBits.Plaque(z.mk, z.solid, labels, a, z.At(reach * .85f + 1.5f, reach * .5f + 2.5f), front, wood, dark, Ink, 1.6f);
            Lantern(ctx, z.mk, z.At(reach * .85f, reach * .5f + 5f));
            if (z.area.glowLight == null) z.area.glowLight = ArtistBits.PointLight(root, z.c + Vector3.up * 4f + front * 4f, Warm, HasStation(a) ? 16f : 11f, 1.2f);
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
            foreach (var s in new[] { -1f, 1f }) z.mk.Prism(logM, ground + side * s * (w / 2f - .1f) - dir * .05f, Vector3.up, .1f, 2.4f, 6);
            var c = ground + Vector3.up * 1.8f;
            z.mk.Box(wood, c - Vector3.up * .1f, new Vector3(w, size + .6f, .1f), rot); z.solid.Box(wood, c - Vector3.up * .1f, new Vector3(w, size + .6f, .2f), rot);
            z.mk.Box(WorldMaterials.Solid(new Color(.35f, .32f, .3f)), c + Vector3.up * (size / 2f + .4f), new Vector3(w + .4f, .08f, .5f), rot * Quaternion.Euler(-12f, 0, 0));
            ArtistBits.CoverWall(z.pictures, z.labels, z.ctx, z.a, c + dir * .055f + Vector3.up * .08f, dir, n, size, n, true, Ink);
        }

        void Rooms(Plot z)
        {
            var up = Vector3.up; var rot = Quaternion.LookRotation(z.front);
            Color[] rooms = { new Color(1f, .8f, .55f), new Color(.75f, .85f, 1f), new Color(1f, .7f, .75f) };
            for (var k = 0; k < 3; k++)
            {
                var p = z.At(-2f - k * 3.5f, (k - 1f) * .8f);
                foreach (var s in new[] { -1f, 1f }) { z.mk.Box(wood, p + up * 1.3f + z.side * s * .65f, new Vector3(.15f, 2.6f, .2f), rot); ArtistBits.PostCollider(z.root, p + z.side * s * .65f, 2.6f, .1f); }
                z.mk.Box(wood, p + up * 2.65f, new Vector3(1.45f, .15f, .2f), rot);
                z.mk.Box(GlowM(rooms[k], .9f), p + up * 1.3f - z.front * .12f, new Vector3(1.15f, 2.4f, .02f), rot);
                halos.Add(p + up * 1.3f + z.front * .6f, 1.4f, rooms[k], .5f);
            }
            var cb = z.At(3f, 5f); z.mk.Box(wood, cb + up * 1.2f, new Vector3(1.4f, 2.4f, .7f), rot); z.solid.Box(wood, cb + up * 1.2f, new Vector3(1.4f, 2.4f, .7f), rot);
            z.mk.Box(wood, cb + up * 1.2f + z.front * .6f + z.side * .5f, new Vector3(.06f, 2.2f, .7f), rot * Quaternion.Euler(0, 55f, 0));
            z.mk.Box(GlowM(new Color(1f, .85f, .6f), 1.4f), cb + up * 1.2f + z.front * .36f, new Vector3(1.1f, 2f, .02f), rot);
        }

        void FatherAndSon(Plot z)
        {
            var up = Vector3.up; var rot = Quaternion.LookRotation(z.front);
            Oak(z.ctx, z.mk, z.At(-5f, 0), 1.3f);
            foreach (var (s, sc) in new[] { (-1.4f, 1f), (1.4f, .7f) })
            {
                var p = z.At(-1f, s); var r = Quaternion.LookRotation(z.front);
                z.mk.Box(wood, p + up * .5f * sc, new Vector3(.7f, .08f, .7f) * sc, r); z.mk.Box(wood, p + up * 1f * sc - z.front * .32f * sc, new Vector3(.7f, .9f, .08f) * sc, r);
                foreach (var a in new[] { -1f, 1f }) foreach (var b in new[] { -1f, 1f }) z.mk.Box(wood, p + up * .25f * sc + z.side * a * .3f * sc + z.front * b * .3f * sc, new Vector3(.06f, .5f, .06f) * sc, r);
                z.solid.Box(wood, p + up * .5f * sc, new Vector3(.8f, 1f, .8f) * sc, r);
            }
            var g = z.At(4f, 6f); z.mk.Prism(wood, g, up, .1f, 1f, 6); var blue = WorldMaterials.Solid(new Color(.25f, .45f, .8f)); var land = WorldMaterials.Solid(new Color(.35f, .65f, .3f));
            z.mk.Blob(blue, g + up * 1.7f, Vector3.one * .7f, z.ctx.Rng, 0f);
            for (var k = 0; k < 5; k++) z.mk.Blob(land, g + up * 1.7f + Quaternion.Euler(k * 70f, k * 50f, 0) * Vector3.forward * .55f, new Vector3(.3f, .25f, .2f), z.ctx.Rng, .3f);
            ArtistBits.PostCollider(z.root, g, 2.2f, .7f);
        }

        void Freighter(Plot z)
        {
            var up = Vector3.up; var rot = Quaternion.LookRotation(z.side); var c = z.At(-2f, 0); var red = WorldMaterials.Solid(new Color(.6f, .15f, .12f)); var whiteM = WorldMaterials.Solid(new Color(.92f, .92f, .9f));
            foreach (var s in new[] { -3f, 3f }) z.mk.Box(wood, c + z.side * s + up * .5f, new Vector3(1.6f, 1f, .4f), rot);
            z.mk.Box(red, c + up * 1.6f, new Vector3(1.6f, 1.2f, 9f), rot); z.mk.Box(dark, c + up * 2.3f, new Vector3(1.62f, .2f, 9.02f), rot);
            z.mk.Box(whiteM, c + up * 2.8f + z.side * 3.6f, new Vector3(1.4f, 1f, 1.4f), rot); z.mk.Box(whiteM, c + up * 2.8f - z.side * 3.6f, new Vector3(1.4f, 1.2f, 1.2f), rot);
            z.mk.Prism(dark, c + up * 3.2f - z.side * 3.8f, up, .2f, .8f, 8);
            z.solid.Box(red, c + up * 1.6f, new Vector3(1.8f, 3.2f, 9.4f), rot);
        }

        void Mountains(Plot z)
        {
            var up = Vector3.up; var c = z.At(-4f, 0); var rock = WorldMaterials.Solid(new Color(.45f, .45f, .5f)); var snow = WorldMaterials.Solid(new Color(.97f, .97f, 1f));
            for (var k = 0; k < 3; k++) { var p = c + z.side * ((k - 1f) * 4f); var h = 5f + (k % 2) * 2f; z.mk.Cone(rock, p, 3f, h, 6); z.mk.Cone(snow, p + up * h * .7f, 3f * .3f + .1f, h * .3f + .05f, 6); }
            z.solid.Box(rock, c + up * 2f, new Vector3(12f, 4f, 4f), Quaternion.LookRotation(z.front));
            var sp = z.At(4f, 6f); z.mk.Prism(wood, sp, up, .08f, 2.4f, 6); z.mk.Box(WorldMaterials.Solid(new Color(.2f, .45f, .25f)), sp + up * 2.2f, new Vector3(2.6f, .5f, .05f), Quaternion.LookRotation(z.front));
            ArtistBits.Label(z.labels, "COUNTRY ROADS", sp + up * 2.2f + z.front * .04f, z.front, .16f, Ink, TextAnchor.MiddleCenter);
        }

        void Bench(Plot z)
        {
            var up = Vector3.up; var rot = Quaternion.LookRotation(z.front); var p = z.At(-2f, 0);
            z.mk.Box(wood, p + up * .45f, new Vector3(2f, .08f, .5f), rot); z.mk.Box(wood, p + up * .85f - z.front * .25f, new Vector3(2f, .5f, .06f), rot);
            foreach (var s in new[] { -.9f, .9f }) z.mk.Box(dark, p + up * .22f + z.side * s, new Vector3(.08f, .45f, .5f), rot);
            z.solid.Box(wood, p + up * .5f, new Vector3(2.1f, 1f, .6f), rot);
            var lp = p + z.side * 1.6f; z.mk.Prism(dark, lp, up, .07f, 3.6f, 6); z.mk.Blob(GlowM(Warm, 2.4f), lp + up * 3.7f, Vector3.one * .25f, z.ctx.Rng, 0f); halos.Add(lp + up * 3.7f, 1.2f, Warm, .7f); ArtistBits.PostCollider(z.root, lp, 3.6f, .1f);
        }

        void PinkHouse(Plot z)
        {
            var up = Vector3.up; var c = z.At(-4f, 0); var rot = Quaternion.LookRotation(z.front); var pink = WorldMaterials.Solid(new Color(.95f, .6f, .65f));
            z.mk.Box(pink, c + up * 2f, new Vector3(8f, 4f, 6f), rot); z.solid.Box(pink, c + up * 2f, new Vector3(8f, 4f, 6f), rot);
            z.mk.GableRoof(WorldMaterials.Solid(new Color(.35f, .32f, .32f)), c + up * 4f, new Vector3(6.6f, 0, 8.6f), 2f, Quaternion.LookRotation(z.side));
            z.mk.Box(dark, c + up * 1f + z.front * 3.01f, new Vector3(1f, 2f, .02f), rot);
            foreach (var s in new[] { -2.4f, 2.4f }) z.mk.Box(GlowM(Warm, 1.4f), c + up * 2.2f + z.front * 3.01f + z.side * s, new Vector3(1.2f, 1f, .02f), rot);
        }

        void Hill(Plot z)
        {
            var up = Vector3.up; var c = z.At(-4f, 0);
            z.mk.Blob(WorldMaterials.Solid(new Color(.35f, .52f, .3f)), c + up * .5f, new Vector3(5f, 3f, 4f), z.ctx.Rng, .1f);
            for (var k = 0; k < 8; k++) { var t = k / 8f; z.mk.Box(WorldMaterials.Solid(new Color(.6f, .5f, .38f)), c + z.front * (3.5f - t * 3.5f) + up * (.4f + Mathf.Sin(t * Mathf.PI * .5f) * 3f), new Vector3(1f, .1f, .8f), Quaternion.LookRotation(z.front) * Quaternion.Euler(-35f, 0, 0)); }
            z.solid.Box(dark, c + up * 1.5f, new Vector3(8f, 3f, 6f), Quaternion.LookRotation(z.front));
        }

        void Werewolf(Plot z)
        {
            var up = Vector3.up; var c = z.At(-2f, 0); var fur = WorldMaterials.Solid(new Color(.35f, .3f, .28f));
            z.mk.Prism(stone, c, up, 1f, .6f, 8); z.solid.Prism(stone, c, up, 1f, 3.4f, 8);
            z.mk.Box(fur, c + up * 1.4f, new Vector3(.6f, 1.6f, .5f), Quaternion.LookRotation(z.front)); z.mk.Blob(fur, c + up * 2.5f, new Vector3(.35f, .38f, .4f), z.ctx.Rng, 0f);
            z.mk.Box(fur, c + up * 2.45f + z.front * .4f, new Vector3(.18f, .16f, .35f), Quaternion.LookRotation(z.front));
            foreach (var s in new[] { -.18f, .18f }) z.mk.Cone(fur, c + up * 2.75f + z.side * s, .1f, .35f, 4);
            foreach (var s in new[] { -.13f, .13f }) z.mk.Blob(GlowM(new Color(1f, .8f, .2f), 2.4f), c + up * 2.55f + z.front * .33f + z.side * s, Vector3.one * .05f, z.ctx.Rng, 0f);
            z.mk.Box(WorldMaterials.Solid(new Color(.85f, .2f, .2f)), c + up * 2f + z.front * .26f, new Vector3(.5f, .2f, .05f), Quaternion.LookRotation(z.front));
        }

        void Pie(Plot z)
        {
            var up = Vector3.up; var p = z.At(-1f, -2f); var rot = Quaternion.LookRotation(z.front);
            z.mk.Box(wood, p + up * .75f, new Vector3(1.4f, .08f, 1f), rot); foreach (var a in new[] { -1f, 1f }) foreach (var b in new[] { -1f, 1f }) z.mk.Box(wood, p + up * .37f + z.side * a * .6f + z.front * b * .4f, new Vector3(.06f, .75f, .06f), rot);
            z.mk.Prism(WorldMaterials.Solid(new Color(.85f, .62f, .32f)), p + up * .79f, up, .4f, .12f, 14); z.mk.Prism(WorldMaterials.Solid(new Color(.75f, .15f, .2f)), p + up * .9f, up, .34f, .02f, 14);
            z.solid.Box(wood, p + up * .4f, new Vector3(1.5f, .8f, 1.1f), rot);
            VintageCarFolk(z, z.At(2f, 4f), new Color(.2f, .35f, .7f));
        }

        void VintageCarFolk(Plot z, Vector3 at, Color paint)
        {
            var up = Vector3.up; var dir = z.side; var rot = Quaternion.LookRotation(dir); var side = Vector3.Cross(up, dir); var body = WorldMaterials.Solid(paint, .8f);
            z.mk.Box(body, at + up * .7f, new Vector3(1.9f, .6f, 4.8f), rot); z.mk.Box(body, at + up * 1.25f - dir * .3f, new Vector3(1.7f, .55f, 2f), rot);
            z.mk.Box(WorldMaterials.Solid(new Color(.2f, .25f, .3f), .9f), at + up * 1.25f - dir * .3f, new Vector3(1.72f, .4f, 1.9f), rot);
            foreach (var s in new[] { -1f, 1f }) foreach (var zz in new[] { -1.5f, 1.5f }) z.mk.Prism(dark, at + side * s * .9f + dir * zz + up * .38f, side * s, .38f, .25f, 12);
            z.solid.Box(body, at + up * .8f, new Vector3(2f, 1.6f, 5f), rot);
        }

        void Cradle(Plot z)
        {
            var up = Vector3.up; var p = z.At(-1f, 0); var rot = Quaternion.LookRotation(z.side);
            z.mk.Box(wood, p + up * .7f, new Vector3(.8f, .4f, 1.4f), rot); foreach (var s in new[] { -.6f, .6f }) z.mk.Box(wood, p + up * .3f + z.side * s, new Vector3(.9f, .1f, .1f), Quaternion.LookRotation(z.front));
            z.solid.Box(wood, p + up * .5f, new Vector3(1f, 1f, 1.5f), rot);
            var cat = z.At(1.5f, 2f); var fur = WorldMaterials.Solid(new Color(.6f, .45f, .3f));
            z.mk.Blob(fur, cat + up * .3f, new Vector3(.25f, .22f, .4f), z.ctx.Rng, 0f); z.mk.Blob(fur, cat + up * .55f + z.front * .3f, Vector3.one * .16f, z.ctx.Rng, 0f);
            foreach (var s in new[] { -.08f, .08f }) z.mk.Cone(fur, cat + up * .68f + z.front * .3f + z.side * s, .05f, .12f, 4);
        }

        void Boulder(Plot z)
        {
            var up = Vector3.up; var c = z.At(-2f, 0);
            foreach (var s in new[] { -1f, 1f }) z.mk.Blob(stone, c + up * 1.4f + z.side * s * 1.1f, new Vector3(1.4f, 1.6f, 1.6f), z.ctx.Rng, .25f);
            z.mk.Box(GlowM(new Color(1f, .6f, .3f), 1.6f), c + up * 1.2f, new Vector3(.15f, 2.4f, 1.8f), Quaternion.LookRotation(z.front));
            z.solid.Box(stone, c + up * 1.4f, new Vector3(5f, 2.8f, 3f), Quaternion.LookRotation(z.front));
        }

        void Lectern(Plot z)
        {
            var up = Vector3.up; var c = z.At(-2f, 0); var rot = Quaternion.LookRotation(z.front);
            for (var k = 0; k < 3; k++) { z.mk.Box(stone, c + up * (.15f + k * .3f) - z.front * k * .5f, new Vector3(5f - k, .3f, 3f - k * .5f), rot); }
            z.solid.Box(stone, c + up * .45f - z.front * .5f, new Vector3(5f, .9f, 3f), rot);
            z.mk.Box(stone, c + up * 1.5f - z.front * 1.1f, new Vector3(.9f, 1.3f, .6f), rot); z.mk.Box(stone, c + up * 2.2f - z.front * 1f, new Vector3(1.1f, .1f, .8f), rot * Quaternion.Euler(-15f, 0, 0));
            ArtistBits.Label(z.labels, "I HAVE A DREAM", c + up * .5f + z.front * 1.52f, z.front, .2f, new Color(.25f, .25f, .25f), TextAnchor.MiddleCenter);
        }

        void Small(Plot z)
        {
            var up = Vector3.up; var c = z.At(-2f, 0); var rot = Quaternion.LookRotation(z.front);
            var h = 0; foreach (var ch in z.a.name) h = (h * 31 + ch) & 0x7fffffff;
            switch (h % 6)
            {
                case 0: // a porch rocker on a little deck
                    z.mk.Box(wood, c + up * .2f, new Vector3(3f, .4f, 2.4f), rot); z.solid.Box(wood, c + up * .2f, new Vector3(3f, .4f, 2.4f), rot);
                    z.mk.Box(wood, c + up * .7f, new Vector3(.7f, .06f, .6f), rot); z.mk.Box(wood, c + up * 1.15f - z.front * .3f, new Vector3(.7f, .9f, .06f), rot * Quaternion.Euler(-10f, 0, 0));
                    foreach (var s in new[] { -.3f, .3f }) z.mk.Box(wood, c + up * .45f + z.side * s, new Vector3(.06f, .08f, .9f), rot);
                    break;
                case 1: // a stump with a harmonica and a guitar leaning
                    z.mk.Prism(bark, c - up * .2f, up, .7f, 1f, 10); z.solid.Prism(bark, c - up * .2f, up, .7f, 1f, 10);
                    z.mk.Box(metal, c + up * .85f, new Vector3(.5f, .08f, .14f), rot);
                    z.mk.Blob(WorldMaterials.Solid(new Color(.75f, .55f, .3f), .7f), c + up * .7f + z.side * 1f, new Vector3(.35f, .45f, .12f), z.ctx.Rng, 0f); z.mk.Box(dark, c + up * 1.5f + z.side * 1f, new Vector3(.07f, 1f, .05f), rot * Quaternion.Euler(0, 0, 10f));
                    break;
                case 2: // a canoe on a rack
                    foreach (var s in new[] { -1.2f, 1.2f }) { z.mk.Box(wood, c + z.side * s + up * .5f, new Vector3(.15f, 1f, 1f), rot); }
                    Canoe(z.mk, c + up * 1.4f, z.side, new Color(.75f, .25f, .15f)); z.solid.Box(wood, c + up * .9f, new Vector3(4.6f, 1.8f, 1f), rot);
                    foreach (var s in new[] { -.5f, .5f }) { var pp = c + z.front * .7f + z.side * s; z.mk.Box(wood, pp + up * .8f, new Vector3(.06f, 1.7f, .04f), rot * Quaternion.Euler(-12f, 0, s * 10f)); z.mk.Box(wood, pp + up * .2f + z.front * .07f, new Vector3(.2f, .5f, .03f), rot * Quaternion.Euler(-12f, 0, 0)); }
                    break;
                case 3: // an upright piano under the trees with a candle on it
                    z.mk.Box(dark, c + up * .8f, new Vector3(1.6f, 1.6f, .6f), rot); z.mk.Box(WorldMaterials.Solid(new Color(.95f, .93f, .88f)), c + up * .78f + z.front * .38f, new Vector3(1.4f, .05f, .22f), rot);
                    z.mk.Box(dark, c + up * .4f + z.front * .3f, new Vector3(1.6f, .8f, .1f), rot); z.solid.Box(dark, c + up * .8f, new Vector3(1.7f, 1.6f, .9f), rot);
                    z.mk.Box(WorldMaterials.Solid(new Color(.95f, .93f, .85f)), c + up * 1.75f + z.side * .5f, new Vector3(.08f, .2f, .08f), rot); z.mk.Blob(GlowM(new Color(1f, .7f, .3f), 3f), c + up * 1.9f + z.side * .5f, Vector3.one * .05f, z.ctx.Rng, 0f);
                    halos.Add(c + up * 1.95f + z.side * .5f + z.front * .3f, .8f, Warm, .7f);
                    z.mk.Box(wood, c + up * .45f + z.front * 1f, new Vector3(1f, .08f, .4f), rot);
                    break;
                case 4: // a hammock strung between two trees
                    foreach (var s in new[] { -2.6f, 2.6f }) Oak(z.ctx, z.mk, c + z.side * s - z.front, .8f);
                    for (var k = 0; k < 8; k++) { var t0 = k / 8f; var t1 = (k + 1) / 8f; var a0 = c - z.front + z.side * Mathf.Lerp(-2.2f, 2.2f, t0) + up * (1.5f - Mathf.Sin(t0 * Mathf.PI) * .7f); var a1 = c - z.front + z.side * Mathf.Lerp(-2.2f, 2.2f, t1) + up * (1.5f - Mathf.Sin(t1 * Mathf.PI) * .7f); z.mk.Box(WorldMaterials.Solid(new Color(.85f, .55f, .3f)), (a0 + a1) / 2f, new Vector3(.75f, .05f, Vector3.Distance(a0, a1) + .02f), Quaternion.LookRotation(a1 - a0)); }
                    break;
                default: // a rope swing from a tree (case 5)
                    Oak(z.ctx, z.mk, c - z.front * 2f, 1f);
                    var hang = c - z.front * 2f + up * 4.6f + z.side * 2f; var seat = hang - up * 3.6f;
                    z.mk.Box(WorldMaterials.Solid(new Color(.75f, .65f, .45f)), (hang + seat) / 2f, new Vector3(.05f, 3.6f, .05f), rot); z.mk.Box(wood, seat, new Vector3(.6f, .06f, .25f), rot);
                    break;
            }
        }
    }
}
