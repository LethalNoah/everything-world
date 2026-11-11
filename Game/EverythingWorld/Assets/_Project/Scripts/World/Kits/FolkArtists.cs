using System.Collections.Generic;
using UnityEngine;

namespace EverythingWorld.World
{
    /// The Folk Woods' artists, each a small story you walk into, made from their songs, with a plaque, covers on a
    /// board and a lantern. Their lights answer their songs (FolkLife.songGlows): the song's own scene brightens while
    /// it plays.
    /// - Fenne Lily: a little cottage you walk through room to room, each room lit its own colour, a cupboard ajar, a
    ///   cat asleep on the bed. Yusuf / Cat Stevens: a porch under a great oak with an old man's rocker and a boy's
    ///   chair side by side, a hand-painted globe, a "PEACE TRAIN" signal by the rails.
    /// - Gordon Lightfoot: a lighthouse on a rocky point with a turning beam, a ship's bell and the freighter on a
    ///   stand, the sky going sundown-gold. John Denver: a dirt country road between white fences up to a farmhouse
    ///   under snow-capped peaks, a mailbox, the COUNTRY ROADS sign. Simon & Garfunkel: a stone arch bridge over a
    ///   little stream, a bench for two old friends under a lamp. The Band: the big pink house with a porch, a creek
    ///   with a jetty. Kate Bush: a hill with a path running up it to a glowing door at the top. Warren Zevon: a
    ///   werewolf under a full moon outside a little restaurant with a paper-lantern doorway. Don McLean: a Chevy on a
    ///   dry levee above a cracked riverbed, a pie on a table. Harry Chapin: a cradle and a cat on a porch, a boy's
    ///   bat and ball, the man in the moon overhead. Carole King: a boulder split open, cracks glowing across the
    ///   ground. Martin Luther King, Jr.: steps and a lectern above a long reflecting pool.
    /// - The rest: Jeff Buckley a lit window and a bench for two; Elliott Smith a street corner sign for Condor Ave and
    ///   a parked car; Fiona Apple a running track; Josie Edwards a glowing flask on a lab table; Liana Flores silver
    ///   shoes on a stump under a rising moon; Lily Meola two swings side by side; Alice Phoebe Lou a busker's corner;
    ///   Anna Nalick a bedside clock glowing 2 AM; Jackson Browne an old car by an empty fuel pump; Neil Diamond a
    ///   bandstand strung with bulbs; Paul Simon a schoolyard; Randy Newman a toy box with a cowboy hat; Shel
    ///   Silverstein a treehouse; Van Morrison a picnic blanket with a transistor radio.
    public partial class FolkKit
    {
        class Plot
        {
            public LandContext ctx; public Transform root, labels; public MeshKit mk, solid, pictures;
            public IslandWorldData.Artist a; public Vector3 c, front, side; public List<Renderer> glows; public ArtistArea area;
            public Vector3 At(float fw, float rt) { var p = c + front * fw + side * rt; return ctx.OnGround(p.x, p.z); }
            public Quaternion Rot => Quaternion.LookRotation(front);
        }

        public const string CountryRoads = "1YYhDizHx7PnDhAhko6cDS", Werewolves = "2nmaEzFZrSm2aMLtfJDzyG", RunningUpThatHill = "1PtQJZVZIdWIYdARpZRDFO", EarthMove = "6whBAw3YnH4rN7ZOMRTzHJ",
            AmericanPie = "1fDsrQ23eTAVFElUMaf38X", CatsInTheCradle = "6hLqEJg2GnnDG4W0Y64hyD", Sundown = "0SjnBEHZVXgCKvOrpvzL2k", RoomToRoom = "6uiHZQZCNFWRBU9ALRGYKQ", SweetCaroline = "6l7tK5SsMlN8a9ccgeIkpS";

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
                case "Fenne Lily": Cottage(z); break;
                case "Yusuf / Cat Stevens": FatherAndSon(z); break;
                case "Gordon Lightfoot": Lighthouse(z); break;
                case "John Denver": CountryRoad(z); break;
                case "Simon & Garfunkel": StoneBridge(z); break;
                case "The Band": PinkHouse(z); break;
                case "Kate Bush": Hill(z); break;
                case "Warren Zevon": Werewolf(z); break;
                case "Don McLean": Levee(z); break;
                case "Harry Chapin": Cradle(z); break;
                case "Carole King": Boulder(z); break;
                case "Martin Luther King, Jr.": Lectern(z); break;
                default: Small(z); break;
            }
            SecondPass(z);   // a light for every song (FolkSecond)
            if (!HasStation(a)) StoryClearing(z);
            var reach = Foot(a);
            Board(z, z.At(reach * .85f, -reach * .5f - 2.5f), front);
            ArtistBits.Plaque(z.mk, z.solid, labels, a, z.At(reach * .85f + 1.5f, reach * .5f + 2.5f), front, wood, dark, Ink, 1.6f);
            Lantern(ctx, z.mk, z.At(reach * .85f, reach * .5f + 5f));
            if (z.area.glowLight == null) z.area.glowLight = ArtistBits.PointLight(root, z.c + Vector3.up * 4f + front * 4f, Warm, HasStation(a) ? 18f : 13f, 1.3f);
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

        /// A clearing round a smaller story: a ring of mossy stones and three lanterns hung on shepherd's hooks.
        void StoryClearing(Plot z)
        {
            var up = Vector3.up; var moss = S(.42f, .5f, .38f);
            for (var k = 0; k < 14; k++)
            {
                var a = k * Mathf.PI * 2f / 14f; if (Mathf.Abs(Mathf.DeltaAngle(a * Mathf.Rad2Deg, 0f)) < 30f) continue;   // open toward the path
                var p = z.c - z.front * 2.5f + (z.front * Mathf.Cos(a) + z.side * Mathf.Sin(a)) * 7.5f; p = z.ctx.OnGround(p.x, p.z);
                z.mk.Blob(stone, p + up * .2f, new Vector3(.45f, .3f, .4f), z.ctx.Rng, .3f); z.mk.Blob(moss, p + up * .42f, new Vector3(.35f, .08f, .3f), z.ctx.Rng, .2f);
            }
            foreach (var a in new[] { 100f, 180f, 260f })
            {
                var d = Quaternion.AngleAxis(a, Vector3.up) * z.front; var p = z.c - z.front * 2.5f + d * 6.6f; p = z.ctx.OnGround(p.x, p.z);
                z.mk.Prism(dark, p, up, .05f, 2.6f, 6); z.mk.Box(dark, p + up * 2.6f - d * .3f, new Vector3(.05f, .05f, .6f), Quaternion.LookRotation(-d));
                var lp = p + up * 2.3f - d * .55f; z.mk.Box(GlowM(Warm, 2.6f), lp, new Vector3(.24f, .32f, .24f), Quaternion.identity); halos.Add(lp, 1.4f, Warm, .8f);
                ArtistBits.PostCollider(z.root, p, 2.6f, .1f);
            }
        }

        /// A glowing material that brightens while the given song plays.
        Material SongGlow(string song, Color c, float s = 1.6f)
        {
            var m = new Material(GlowM(c, s)) { name = "Song glow" }; life.songGlows.Add((song, m, c * (s * .65f))); return m;
        }

        Material S(float r, float g, float b, float sm = .4f) => WorldMaterials.Solid(new Color(r, g, b), sm);

        void Chair(Plot z, Vector3 p, Quaternion r, float sc, Material m)
        {
            var up = Vector3.up; var f = r * Vector3.forward; var sd = r * Vector3.right;
            z.mk.Box(m, p + up * .5f * sc, new Vector3(.7f, .08f, .7f) * sc, r); z.mk.Box(m, p + up * 1f * sc - f * .32f * sc, new Vector3(.7f, .9f, .08f) * sc, r);
            foreach (var a in new[] { -1f, 1f }) foreach (var b in new[] { -1f, 1f }) z.mk.Box(m, p + up * .25f * sc + sd * a * .3f * sc + f * b * .3f * sc, new Vector3(.06f, .5f, .06f) * sc, r);
            z.solid.Box(m, p + up * .5f * sc, new Vector3(.8f, 1f, .8f) * sc, r);
        }

        // ---------------------------------------------------------------- the radio-station artists
        /// Fenne Lily: a little cottage of three rooms in a row, open at the front, walked through room to room.
        void Cottage(Plot z)
        {
            var up = Vector3.up; var rot = z.Rot; var c = z.At(-6f, 0); var plaster = S(.92f, .88f, .8f); var floor = S(.5f, .38f, .28f);
            const float W = 15f, D = 5f, H = 3f;
            Vector3 P(float x, float y, float d) => c + z.side * x + up * y + z.front * d;
            z.mk.Box(floor, P(0, .12f, 0), new Vector3(W - .05f, .2f, D - .05f), rot); z.solid.Box(floor, P(0, .1f, 0), new Vector3(W, .2f, D), rot);
            z.mk.Box(plaster, P(0, H / 2f, -D / 2f), new Vector3(W, H, .25f), rot); z.solid.Box(plaster, P(0, H / 2f, -D / 2f), new Vector3(W, H, .3f), rot);
            foreach (var s in new[] { -1f, 1f }) { z.mk.Box(plaster, P(s * W / 2f, H / 2f, 0), new Vector3(.25f, H, D), rot); z.solid.Box(plaster, P(s * W / 2f, H / 2f, 0), new Vector3(.3f, H, D), rot); }
            foreach (var s in new[] { -1f, 1f }) { var x = s * W / 6f; z.mk.Box(plaster, P(x, H / 2f, -D / 4f - .3f), new Vector3(.2f, H, D / 2f - .6f), rot); z.mk.Box(plaster, P(x, H - .35f, D / 4f), new Vector3(.2f, .7f, D / 2f), rot); z.solid.Box(plaster, P(x, H / 2f, -D / 4f - .3f), new Vector3(.25f, H, D / 2f - .6f), rot); }
            z.mk.GableRoof(S(.4f, .3f, .28f), P(0, H, 0), new Vector3(D + .8f, 0, W + .8f), 1.6f, Quaternion.LookRotation(z.side));
            Color[] tints = { new Color(1f, .78f, .5f), new Color(.7f, .82f, 1f), new Color(1f, .68f, .72f) };
            for (var k = 0; k < 3; k++)
            {
                var x = (k - 1f) * W / 3f; var lamp = SongGlow(new[] { Franz, ThankYouSorry, CupboardSong }[k], tints[k], 2f);   // each room its own song
                z.mk.Prism(dark, P(x, H - .6f, 0), up, .02f, .6f, 4); z.mk.Blob(lamp, P(x, H - .7f, 0), Vector3.one * .22f, z.ctx.Rng, 0f); halos.Add(P(x, H - .8f, 0), 1.6f, tints[k], .6f);
                ArtistBits.PointLight(z.root, P(x, 1.8f, .5f), tints[k], 6f, 1f, true);
            }
            // the left room: a bed with a cat asleep on it (Franz); the middle: a table and two chairs (Thank You Sorry); the right: the cupboard ajar
            var bed = P(-W / 3f, .45f, -1f); z.mk.Box(wood, bed, new Vector3(2.2f, .5f, 1.6f), Quaternion.LookRotation(z.side)); z.mk.Box(S(.85f, .85f, .95f), bed + up * .3f, new Vector3(2.1f, .2f, 1.5f), Quaternion.LookRotation(z.side)); z.solid.Box(wood, bed, new Vector3(2.2f, .7f, 1.6f), Quaternion.LookRotation(z.side));
            var cat = bed + up * .55f + z.side * .4f; var fur = S(.85f, .55f, .3f); z.mk.Blob(fur, cat, new Vector3(.3f, .18f, .2f), z.ctx.Rng, 0f); z.mk.Blob(fur, cat + z.side * .3f + up * .05f, Vector3.one * .13f, z.ctx.Rng, 0f);
            var tb = P(0, 0, -.8f); z.mk.Box(wood, tb + up * .75f, new Vector3(1.2f, .06f, .8f), rot); z.solid.Box(wood, tb + up * .4f, new Vector3(1.2f, .8f, .8f), rot);
            foreach (var s in new[] { -1f, 1f }) Chair(z, tb + z.side * s * 1f, Quaternion.LookRotation(-z.side * s), .9f, wood);
            var cb = P(W / 3f, 0, -1.8f); z.mk.Box(wood, cb + up * 1.2f, new Vector3(1.4f, 2.4f, .7f), rot); z.solid.Box(wood, cb + up * 1.2f, new Vector3(1.4f, 2.4f, .7f), rot);
            z.mk.Box(wood, cb + up * 1.2f + z.front * .6f + z.side * .5f, new Vector3(.06f, 2.2f, .7f), rot * Quaternion.Euler(0, 55f, 0)); z.mk.Box(GlowM(new Color(1f, .85f, .6f), 1.4f), cb + up * 1.2f + z.front * .36f, new Vector3(1.1f, 2f, .02f), rot);
            IndoorSpaces.Add(P(0, H / 2f, 0), rot, new Vector3(W - .4f, H, D - .4f));
        }

        /// Yusuf / Cat Stevens: an old man's rocker and a boy's chair on a porch under a great oak, a painted globe, a
        /// railway signal saying PEACE TRAIN.
        void FatherAndSon(Plot z)
        {
            var up = Vector3.up; var rot = z.Rot;
            Oak(z.ctx, z.mk, z.At(-8f, -4f), 1.5f);
            var deck = z.At(-2f, 0); z.mk.Box(wood, deck + up * .2f, new Vector3(7f, .4f, 4f), rot); z.solid.Box(wood, deck + up * .2f, new Vector3(7f, .4f, 4f), rot);
            foreach (var s in new[] { -3.3f, 3.3f }) z.mk.Prism(logM, deck + z.side * s + z.front * 1.8f, up, .12f, 3f, 6);
            z.mk.Box(wood, deck + up * 3.1f, new Vector3(7.4f, .15f, 4.4f), rot);
            Chair(z, deck + up * .4f - z.side * 1.2f, rot, 1.15f, S(.45f, .32f, .22f)); Chair(z, deck + up * .4f + z.side * 1.2f, rot, .75f, S(.6f, .45f, .3f));
            var lampM = SongGlow(FatherAndSonSong, Warm, 2.2f); z.mk.Box(lampM, deck + up * 2.6f, new Vector3(.3f, .4f, .3f), Quaternion.identity); halos.Add(deck + up * 2.6f, 1.6f, Warm, .7f);
            var g = z.At(2f, 6f); z.mk.Prism(wood, g, up, .1f, 1f, 6); var blue = S(.25f, .45f, .8f); var land = S(.35f, .65f, .3f);
            z.mk.Blob(blue, g + up * 1.7f, Vector3.one * .7f, z.ctx.Rng, 0f);
            for (var k = 0; k < 5; k++) z.mk.Blob(land, g + up * 1.7f + Quaternion.Euler(k * 70f, k * 50f, 0) * Vector3.forward * .55f, new Vector3(.3f, .25f, .2f), z.ctx.Rng, .3f);
            ArtistBits.PostCollider(z.root, g, 2.2f, .7f);
            var sg = z.At(2f, -7f); z.mk.Prism(dark, sg, up, .1f, 3.4f, 6); ArtistBits.PostCollider(z.root, sg, 3.4f, .15f);
            z.mk.Box(S(.9f, .9f, .88f), sg + up * 3.2f + z.front * .1f, new Vector3(1.6f, .4f, .06f), rot); ArtistBits.Label(z.labels, "PEACE TRAIN", sg + up * 3.2f + z.front * .14f, z.front, .14f, new Color(.1f, .1f, .1f), TextAnchor.MiddleCenter);
            z.mk.Blob(SongGlow(FolkKit.PeaceTrain, new Color(.3f, 1f, .4f), 2.4f), sg + up * 2.6f + z.front * .15f, Vector3.one * .15f, z.ctx.Rng, 0f);
        }

        // ---------------------------------------------------------------- the story scenes
        /// Gordon Lightfoot: a lighthouse on a rocky point with a turning beam, a ship's bell, the freighter on a stand.
        void Lighthouse(Plot z)
        {
            var up = Vector3.up; var c = z.At(-6f, -2f); var rock = S(.5f, .5f, .52f);
            z.mk.Blob(rock, c + up * .4f, new Vector3(3.6f, 1.2f, 3.2f), z.ctx.Rng, .3f); z.solid.Prism(rock, c - up * .2f, up, 3.2f, 1.4f, 10);
            for (var k = 0; k < 5; k++) z.mk.Prism(k % 2 == 0 ? S(.95f, .95f, .92f) : S(.75f, .15f, .12f), c + up * (1.2f + k * 1.4f), up, 1.2f - k * .1f, 1.4f, 14, (1.2f - (k + 1) * .1f) / (1.2f - k * .1f));
            z.solid.Prism(rock, c + up * 1.2f, up, 1.2f, 7f, 10);
            var lamp = c + up * 8.4f; z.mk.Prism(dark, lamp, up, .7f, 1f, 10); z.mk.Blob(SongGlow(FolkKit.EdmundFitzgerald, new Color(1f, .9f, .6f), 2.6f), lamp + up * .5f, Vector3.one * .5f, z.ctx.Rng, 0f); z.mk.Cone(S(.75f, .15f, .12f), lamp + up * 1f, .9f, .9f, 10);
            halos.Add(lamp + up * .5f, 3f, new Color(1f, .9f, .6f), .7f); life.lighthouse = lamp + up * .5f;
            var bell = z.At(1f, 4f); z.mk.Prism(wood, bell, up, .1f, 2f, 6); z.mk.Box(wood, bell + up * 2f + z.side * .4f, new Vector3(.08f, .08f, .9f), Quaternion.LookRotation(z.side)); z.mk.Prism(S(.85f, .7f, .3f, .9f), bell + up * 1.45f + z.side * .7f, up, .3f, .45f, 12, .6f); ArtistBits.PostCollider(z.root, bell, 2f, .15f);
            var fc = z.At(1f, -4.5f); var red = S(.6f, .15f, .12f); var fr = Quaternion.LookRotation(z.side); var wht = S(.92f, .92f, .9f);
            foreach (var s in new[] { -1.4f, 1.4f }) z.mk.Box(wood, fc + z.side * s + up * .4f, new Vector3(.8f, .8f, .3f), fr);
            z.mk.Box(red, fc + up * 1.1f, new Vector3(.8f, .6f, 4.4f), fr); z.mk.Box(dark, fc + up * 1.45f, new Vector3(.82f, .1f, 4.42f), fr); z.mk.Box(wht, fc + up * 1.75f + z.side * 1.8f, new Vector3(.7f, .5f, .7f), fr); z.mk.Box(wht, fc + up * 1.75f - z.side * 1.8f, new Vector3(.7f, .6f, .6f), fr);
            z.solid.Box(red, fc + up * 1f, new Vector3(1f, 2f, 4.6f), fr);
        }

        /// John Denver: a dirt road between white fences up to a farmhouse with its porch light on, snow-capped peaks
        /// behind, a mailbox and the COUNTRY ROADS sign.
        void CountryRoad(Plot z)
        {
            var up = Vector3.up; var rot = z.Rot; var rock = S(.45f, .45f, .5f); var snow = S(.97f, .97f, 1f); var fence = S(.95f, .95f, .93f);
            var mc = z.At(-10f, 0);
            for (var k = 0; k < 3; k++) { var p = mc + z.side * ((k - 1f) * 5f) - z.front * (k % 2) * 2f; var h = 7f + (k % 2) * 3f; z.mk.Cone(rock, p, 4f, h, 6); z.mk.Cone(snow, p + up * h * .68f, 4f * .32f + .1f, h * .32f + .05f, 6); }
            z.solid.Box(rock, mc + up * 2.5f, new Vector3(15f, 5f, 5f), rot);
            var road = S(.62f, .5f, .34f); z.mk.Box(road, z.At(-1f, 0) + up * .05f, new Vector3(2.4f, .06f, 10f), rot);
            foreach (var s in new[] { -1f, 1f }) for (var k = 0; k < 5; k++) { var fp = z.At(-5f + k * 2f, s * 2f); z.mk.Box(fence, fp + up * .5f, new Vector3(.1f, 1f, .1f), rot); z.mk.Box(fence, fp + up * .75f + z.front * 1f, new Vector3(.06f, .1f, 2f), rot); z.mk.Box(fence, fp + up * .4f + z.front * 1f, new Vector3(.06f, .1f, 2f), rot); ArtistBits.PostCollider(z.root, fp, 1f, .1f); }
            var fh = z.At(-6f, 5f); var red = S(.75f, .3f, .2f); z.mk.Box(red, fh + up * 1.6f, new Vector3(4f, 3.2f, 3.4f), rot); z.solid.Box(red, fh + up * 1.6f, new Vector3(4f, 3.2f, 3.4f), rot);
            z.mk.GableRoof(S(.3f, .3f, .32f), fh + up * 3.2f, new Vector3(4f, 0, 4.6f), 1.4f, Quaternion.LookRotation(z.side));
            var porch = SongGlow(CountryRoads, Warm, 2.4f); z.mk.Box(porch, fh + up * 2.2f + z.front * 1.72f, new Vector3(.3f, .35f, .1f), rot); halos.Add(fh + up * 2.2f + z.front * 2f, 1.6f, Warm, .7f);
            foreach (var s in new[] { -1f, 1f }) z.mk.Box(SongGlow(CountryRoads, Warm, 1.4f), fh + up * 1.7f + z.front * 1.71f + z.side * s * 1.2f, new Vector3(.7f, .7f, .02f), rot);
            var mb = z.At(2f, 2.8f); z.mk.Prism(wood, mb, up, .06f, 1.1f, 6); z.mk.Box(S(.3f, .45f, .65f), mb + up * 1.2f, new Vector3(.3f, .3f, .55f), rot); ArtistBits.PostCollider(z.root, mb, 1.2f, .2f);
            var sp = z.At(2f, -3.5f); z.mk.Prism(wood, sp, up, .08f, 2.4f, 6); z.mk.Box(S(.2f, .45f, .25f), sp + up * 2.2f, new Vector3(2.6f, .5f, .05f), rot); ArtistBits.PostCollider(z.root, sp, 2.4f, .15f);
            ArtistBits.Label(z.labels, "COUNTRY ROADS", sp + up * 2.2f + z.front * .04f, z.front, .16f, Ink, TextAnchor.MiddleCenter);
        }

        /// Simon & Garfunkel: a stone arch bridge over a little stream, a bench for two old friends under a lamp.
        void StoneBridge(Plot z)
        {
            var up = Vector3.up; var stoneM = S(.62f, .6f, .56f); var c = z.At(-3f, 0); var flow = Quaternion.LookRotation(z.side);
            z.mk.Box(SongGlow(FolkKit.BridgeOverTroubledWater, new Color(.3f, .55f, .8f), .5f), c + up * .03f, new Vector3(2.4f, .06f, 16f), flow);
            for (var k = 0; k < 10; k++) { var t = k / 9f; var a = Mathf.Lerp(-1f, 1f, t); var p = c + z.front * a * 3f + up * (1.2f - a * a * 1.1f); z.mk.Box(stoneM, p, new Vector3(2.6f, .3f, .7f), Quaternion.LookRotation(z.front) * Quaternion.Euler(-a * 30f, 0, 0)); }
            foreach (var s in new[] { -1f, 1f }) for (var k = 0; k < 6; k++) { var t = k / 5f; var a = Mathf.Lerp(-1f, 1f, t); z.mk.Box(stoneM, c + z.front * a * 3f + z.side * s * 1.25f + up * (1.6f - a * a * 1.1f), new Vector3(.2f, .5f, .7f), Quaternion.LookRotation(z.front)); }
            z.solid.Box(stoneM, c + up * .6f, new Vector3(2.6f, 1.2f, 6f), Quaternion.LookRotation(z.front));
            var b = z.At(-1f, 5f); z.mk.Box(wood, b + up * .45f, new Vector3(2f, .08f, .5f), Quaternion.LookRotation(-z.side)); z.mk.Box(wood, b + up * .85f + z.side * .25f, new Vector3(2f, .5f, .06f), Quaternion.LookRotation(-z.side)); z.solid.Box(wood, b + up * .5f, new Vector3(2.1f, 1f, .6f), Quaternion.LookRotation(-z.side));
            var lp = b + z.front * 1.6f; z.mk.Prism(dark, lp, up, .07f, 3.6f, 6); z.mk.Blob(GlowM(Warm, 2.4f), lp + up * 3.7f, Vector3.one * .25f, z.ctx.Rng, 0f); halos.Add(lp + up * 3.7f, 1.2f, Warm, .7f); ArtistBits.PostCollider(z.root, lp, 3.6f, .1f);
        }

        /// The Band: the big pink house with a porch, a creek with a little jetty and a rowboat (Up on Cripple Creek).
        void PinkHouse(Plot z)
        {
            var up = Vector3.up; var c = z.At(-5f, -2f); var rot = z.Rot; var pink = S(.95f, .6f, .65f);
            z.mk.Box(pink, c + up * 2f, new Vector3(8f, 4f, 6f), rot); z.solid.Box(pink, c + up * 2f, new Vector3(8f, 4f, 6f), rot);
            z.mk.GableRoof(S(.35f, .32f, .32f), c + up * 4f, new Vector3(6.6f, 0, 8.6f), 2f, Quaternion.LookRotation(z.side));
            z.mk.Box(wood, c + up * .25f + z.front * 3.9f, new Vector3(8f, .3f, 1.8f), rot); z.mk.Box(wood, c + up * 2.8f + z.front * 3.9f, new Vector3(8.2f, .12f, 2f), rot); foreach (var s in new[] { -3.8f, 0f, 3.8f }) z.mk.Prism(wood, c + z.front * 4.6f + z.side * s, up, .1f, 2.8f, 6);
            z.mk.Box(dark, c + up * 1f + z.front * 3.01f, new Vector3(1f, 2f, .02f), rot);
            foreach (var s in new[] { -2.4f, 2.4f }) z.mk.Box(SongGlow("0SnEAyKMyIjRA96e0JstYl", Warm, 1.4f), c + up * 2.2f + z.front * 3.01f + z.side * s, new Vector3(1.2f, 1f, .02f), rot);
            Chair(z, c + up * .4f + z.front * 3.9f + z.side * 2.4f, rot, 1f, wood);
            var creek = z.At(-2f, 8.5f); z.mk.Box(SongGlow("7aTCCNpHJD2Jgv0LLVJb8Y", new Color(.3f, .5f, .7f), .5f), creek + up * .02f, new Vector3(2.6f, .04f, 14f), rot);
            z.mk.Box(wood, creek + up * .3f - z.side * .6f, new Vector3(2f, .12f, 1.4f), Quaternion.LookRotation(z.side)); Canoe(z.mk, creek + up * .35f + z.front * 3f, z.front, new Color(.75f, .3f, .2f));
        }

        /// Kate Bush: a hill with a path of steps running up it to a glowing door at the top.
        void Hill(Plot z)
        {
            var up = Vector3.up; var c = z.At(-5f, 0);
            z.mk.Blob(S(.35f, .52f, .3f), c + up * .5f, new Vector3(6f, 4f, 5f), z.ctx.Rng, .1f);
            for (var k = 0; k < 10; k++) { var t = k / 10f; z.mk.Box(S(.6f, .5f, .38f), c + z.front * (4.5f - t * 4.5f) + up * (.4f + Mathf.Sin(t * Mathf.PI * .5f) * 4f), new Vector3(1f, .12f, .7f), z.Rot * Quaternion.Euler(-35f, 0, 0)); }
            var door = c + up * 4.4f; z.mk.Box(wood, door + up * 1.2f - z.side * .6f, new Vector3(.15f, 2.4f, .2f), z.Rot); z.mk.Box(wood, door + up * 1.2f + z.side * .6f, new Vector3(.15f, 2.4f, .2f), z.Rot); z.mk.Box(wood, door + up * 2.4f, new Vector3(1.4f, .15f, .2f), z.Rot);
            z.mk.Box(SongGlow(RunningUpThatHill, new Color(1f, .9f, .7f), 2f), door + up * 1.2f - z.front * .05f, new Vector3(1.05f, 2.25f, .02f), z.Rot); halos.Add(door + up * 1.2f + z.front * .5f, 2.4f, new Color(1f, .9f, .7f), .6f);
            z.solid.Box(dark, c + up * 2f, new Vector3(8f, 4f, 6f), z.Rot);
        }

        /// Warren Zevon: a werewolf under a full moon outside a little restaurant with a paper-lantern doorway.
        void Werewolf(Plot z)
        {
            var up = Vector3.up; var c = z.At(0f, -2f); var fur = S(.35f, .3f, .28f);
            z.mk.Box(fur, c + up * 1.4f, new Vector3(.6f, 1.6f, .5f), z.Rot); z.mk.Blob(fur, c + up * 2.5f, new Vector3(.35f, .38f, .4f), z.ctx.Rng, 0f);
            z.mk.Box(fur, c + up * 2.45f + z.front * .4f, new Vector3(.18f, .16f, .35f), z.Rot); foreach (var s in new[] { -.18f, .18f }) z.mk.Cone(fur, c + up * 2.75f + z.side * s, .1f, .35f, 4);
            foreach (var s in new[] { -.13f, .13f }) z.mk.Blob(SongGlow(Werewolves, new Color(1f, .8f, .2f), 2.4f), c + up * 2.55f + z.front * .33f + z.side * s, Vector3.one * .05f, z.ctx.Rng, 0f);
            foreach (var s in new[] { -.2f, .2f }) z.mk.Box(fur, c + up * .4f + z.side * s, new Vector3(.18f, .8f, .2f), z.Rot); z.solid.Box(fur, c + up * 1.4f, new Vector3(.8f, 2.8f, .8f), z.Rot);
            var r = z.At(-5f, 2f); var wallM = S(.75f, .2f, .15f); z.mk.Box(wallM, r + up * 1.8f, new Vector3(5f, 3.6f, 3f), z.Rot); z.solid.Box(wallM, r + up * 1.8f, new Vector3(5f, 3.6f, 3f), z.Rot);
            z.mk.Box(S(.15f, .1f, .08f), r + up * 3.8f, new Vector3(5.4f, .4f, 3.4f), z.Rot); z.mk.Box(dark, r + up * 1.1f + z.front * 1.51f, new Vector3(1.2f, 2.2f, .02f), z.Rot);
            z.mk.Box(SongGlow(Werewolves, new Color(1f, .7f, .35f), 1.4f), r + up * 1.7f + z.front * 1.51f + z.side * 1.6f, new Vector3(1.2f, 1f, .02f), z.Rot);
            for (var k = 0; k < 5; k++) { var lp = r + up * 3.3f + z.front * 1.7f + z.side * ((k - 2f) * .9f); z.mk.Blob(SongGlow(Werewolves, new Color(1f, .3f, .2f), 2f), lp, new Vector3(.22f, .28f, .22f), z.ctx.Rng, 0f); }
            halos.Add(r + up * 3.2f + z.front * 2f, 2.6f, new Color(1f, .4f, .25f), .5f);
            var moon = SongGlow(Werewolves, new Color(.95f, .95f, .85f), 1.4f); z.mk.Blob(moon, c + up * 14f - z.front * 4f, Vector3.one * 1.8f, z.ctx.Rng, 0f); halos.Add(c + up * 14f - z.front * 4f, 4f, new Color(.95f, .95f, .85f), .5f);
        }

        /// Don McLean: a Chevy parked on a dry levee above a cracked riverbed, a pie on a little table.
        void Levee(Plot z)
        {
            var up = Vector3.up; var bank = S(.55f, .45f, .32f); var bed = S(.7f, .6f, .45f);
            var lv = z.At(-4f, 0); z.mk.Box(bank, lv + up * .5f, new Vector3(14f, 1f, 3.4f), z.Rot); z.mk.Box(bank, lv + up * .3f - z.front * 2.2f, new Vector3(14f, .6f, 1.4f), z.Rot * Quaternion.Euler(20f, 0, 0)); z.solid.Box(bank, lv + up * .5f, new Vector3(14f, 1f, 3.4f), z.Rot);
            var rb = z.At(-7.5f, 0); z.mk.Box(bed, rb + up * .02f, new Vector3(14f, .04f, 3f), z.Rot); for (var k = 0; k < 12; k++) z.mk.Box(S(.4f, .32f, .22f), rb + up * .05f + z.side * ((k - 5.5f) * 1.1f) + z.front * Mathf.Sin(k * 2.1f) * .9f, new Vector3(.05f, .02f, 1.2f), z.Rot * Quaternion.Euler(0, k * 37f, 0));
            VintageCarFolk(z, lv + up * 1f, new Color(.2f, .35f, .7f));
            var p = z.At(1.5f, -3f); z.mk.Box(wood, p + up * .75f, new Vector3(1.4f, .08f, 1f), z.Rot); foreach (var a in new[] { -1f, 1f }) foreach (var b in new[] { -1f, 1f }) z.mk.Box(wood, p + up * .37f + z.side * a * .6f + z.front * b * .4f, new Vector3(.06f, .75f, .06f), z.Rot);
            z.mk.Prism(S(.85f, .62f, .32f), p + up * .79f, up, .4f, .12f, 14); z.mk.Prism(S(.75f, .15f, .2f), p + up * .9f, up, .34f, .02f, 14); z.solid.Box(wood, p + up * .4f, new Vector3(1.5f, .8f, 1.1f), z.Rot);
            var jb = z.At(1.5f, 3f); z.mk.Box(wood, jb + up * .8f, new Vector3(1f, 1.6f, .6f), z.Rot); z.mk.Prism(SongGlow(AmericanPie, new Color(1f, .5f, .3f), 1.8f), jb + up * 1.4f + z.front * .31f, z.front, .4f, .02f, 16); z.mk.Box(SongGlow(AmericanPie, new Color(.4f, .7f, 1f), 1.6f), jb + up * .7f + z.front * .31f, new Vector3(.8f, .5f, .02f), z.Rot); z.solid.Box(wood, jb + up * .8f, new Vector3(1f, 1.6f, .6f), z.Rot);
        }

        void VintageCarFolk(Plot z, Vector3 at, Color paint)
        {
            var up = Vector3.up; var dir = z.side; var rot = Quaternion.LookRotation(dir); var side = Vector3.Cross(up, dir); var body = WorldMaterials.Solid(paint, .8f);
            z.mk.Box(body, at + up * .7f, new Vector3(1.9f, .6f, 4.8f), rot); z.mk.Box(body, at + up * 1.25f - dir * .3f, new Vector3(1.7f, .55f, 2f), rot);
            z.mk.Box(WorldMaterials.Solid(new Color(.2f, .25f, .3f), .9f), at + up * 1.25f - dir * .3f, new Vector3(1.72f, .4f, 1.9f), rot);
            foreach (var s in new[] { -1f, 1f }) foreach (var zz in new[] { -1.5f, 1.5f }) z.mk.Prism(dark, at + side * s * .9f + dir * zz + up * .38f, side * s, .38f, .25f, 12);
            z.solid.Box(body, at + up * .8f, new Vector3(2f, 1.6f, 5f), rot);
        }

        /// Harry Chapin: a porch with a cradle and a sleeping cat, a boy's bat and ball, the man in the moon above.
        void Cradle(Plot z)
        {
            var up = Vector3.up; var deck = z.At(-2f, 0); z.mk.Box(wood, deck + up * .2f, new Vector3(6f, .4f, 4f), z.Rot); z.solid.Box(wood, deck + up * .2f, new Vector3(6f, .4f, 4f), z.Rot);
            var p = deck + up * .4f - z.side * 1.2f; var rot = Quaternion.LookRotation(z.side);
            z.mk.Box(wood, p + up * .7f, new Vector3(.8f, .4f, 1.4f), rot); foreach (var s in new[] { -.6f, .6f }) z.mk.Box(wood, p + up * .3f + z.side * s, new Vector3(.9f, .1f, .1f), z.Rot); z.solid.Box(wood, p + up * .5f, new Vector3(1f, 1f, 1.5f), rot);
            var cat = deck + up * .4f + z.side * 1f + z.front * 1f; var fur = S(.6f, .45f, .3f); z.mk.Blob(fur, cat + up * .3f, new Vector3(.25f, .22f, .4f), z.ctx.Rng, 0f); z.mk.Blob(fur, cat + up * .55f + z.front * .3f, Vector3.one * .16f, z.ctx.Rng, 0f);
            Chair(z, deck + up * .4f + z.side * 2f - z.front * .8f, z.Rot, 1.1f, S(.45f, .32f, .22f));
            var bat = z.At(2f, 2.5f); z.mk.Box(S(.75f, .6f, .4f), bat + up * .5f, new Vector3(.08f, 1f, .08f), z.Rot * Quaternion.Euler(0, 0, 20f)); z.mk.Blob(S(.95f, .95f, .92f), bat + up * .1f + z.side * .5f, Vector3.one * .1f, z.ctx.Rng, 0f);
            var moon = SongGlow(CatsInTheCradle, new Color(.95f, .92f, .8f), 1.4f); var mp = z.c + up * 12f - z.front * 6f; z.mk.Blob(moon, mp, Vector3.one * 1.4f, z.ctx.Rng, 0f); halos.Add(mp, 3f, new Color(.95f, .92f, .8f), .5f);
            foreach (var s in new[] { -.4f, .4f }) z.mk.Blob(dark, mp + z.front * 1.3f + z.side * s + up * .3f, Vector3.one * .12f, z.ctx.Rng, 0f);
        }

        /// Carole King: a boulder split open with light in the crack, cracks glowing across the ground round it.
        void Boulder(Plot z)
        {
            var up = Vector3.up; var c = z.At(-3f, 0); var glow = SongGlow(EarthMove, new Color(1f, .55f, .25f), 1.6f);
            foreach (var s in new[] { -1f, 1f }) z.mk.Blob(stone, c + up * 1.4f + z.side * s * 1.2f, new Vector3(1.5f, 1.7f, 1.7f), z.ctx.Rng, .25f);
            z.mk.Box(glow, c + up * 1.2f, new Vector3(.15f, 2.4f, 1.8f), z.Rot);
            for (var k = 0; k < 8; k++) { var d = Quaternion.Euler(0, k * 45f + 10f, 0) * Vector3.forward; var p = c; for (var j = 0; j < 3; j++) { var q = p + (d + Vector3.Cross(up, d) * Mathf.Sin(k + j * 2f) * .4f).normalized * 1.3f; q.y = z.ctx.Ground(q.x, q.z); z.mk.Box(glow, (p + q) / 2f + up * .04f, new Vector3(.08f, .04f, Vector3.Distance(p, q)), Quaternion.LookRotation(new Vector3(q.x - p.x, 0, q.z - p.z))); p = q; } }
            halos.Add(c + up * 1.2f + z.front * .5f, 2.6f, new Color(1f, .55f, .25f), .5f); z.solid.Box(stone, c + up * 1.4f, new Vector3(5.4f, 2.8f, 3.2f), z.Rot);
        }

        /// Martin Luther King, Jr.: wide steps and a lectern above a long reflecting pool, lamps either side.
        void Lectern(Plot z)
        {
            var up = Vector3.up; var c = z.At(-5f, 0); var rot = z.Rot; var pale = S(.85f, .83f, .78f);
            for (var k = 0; k < 4; k++) z.mk.Box(pale, c + up * (.15f + k * .3f) - z.front * k * .6f, new Vector3(7f - k, .3f, 4f - k * .6f), rot);
            z.solid.Box(pale, c + up * .6f - z.front * .6f, new Vector3(7f, 1.2f, 4f), rot);
            z.mk.Box(pale, c + up * 1.9f - z.front * 1.6f, new Vector3(.9f, 1.3f, .6f), rot); z.mk.Box(pale, c + up * 2.6f - z.front * 1.5f, new Vector3(1.1f, .1f, .8f), rot * Quaternion.Euler(-15f, 0, 0));
            var pool = z.At(2.5f, 0); z.mk.Box(pale, pool + up * .1f, new Vector3(3.6f, .2f, 7f), rot); z.mk.Box(WorldMaterials.Solid(new Color(.3f, .45f, .6f), .97f), pool + up * .22f, new Vector3(3f, .06f, 6.4f), rot); z.solid.Box(pale, pool + up * .15f, new Vector3(3.6f, .3f, 7f), rot);
            foreach (var s in new[] { -1f, 1f }) { var lp = c + z.side * s * 4f; z.mk.Prism(pale, lp, up, .25f, 3.4f, 10); z.mk.Blob(SongGlow("641U2WTuEdQ0XHSIWWHzIh", Warm, 2.2f), lp + up * 3.6f, Vector3.one * .3f, z.ctx.Rng, 0f); halos.Add(lp + up * 3.6f, 1.6f, Warm, .6f); ArtistBits.PostCollider(z.root, lp, 3.4f, .3f); }
            ArtistBits.Label(z.labels, "I HAVE A DREAM", c + up * .5f + z.front * 2.02f, z.front, .22f, new Color(.25f, .25f, .25f), TextAnchor.MiddleCenter);
        }

        /// Everyone else, a small story from their song.
        void Small(Plot z)
        {
            var up = Vector3.up; var c = z.At(-2.5f, 0); var rot = z.Rot; var n = z.a.name; var id = z.a.songList != null && z.a.songList.Length > 0 ? "" : "";
            switch (n)
            {
                case "Jeff Buckley": // Lover, You Should've Come Over: a lit window in a little wall, a bench for two, one empty
                    { var wl = S(.7f, .65f, .58f); z.mk.Box(wl, c + up * 1.5f - z.front * 1f, new Vector3(4f, 3f, .3f), rot); z.solid.Box(wl, c + up * 1.5f - z.front * 1f, new Vector3(4f, 3f, .4f), rot);
                      z.mk.Box(SongGlow("6Jv7kjGkhY2fT4yuBF3aTz", new Color(1f, .8f, .5f), 1.6f), c + up * 1.8f - z.front * .8f, new Vector3(1.2f, 1.2f, .02f), rot); halos.Add(c + up * 1.8f, 1.8f, Warm, .5f);
                      z.mk.Box(wood, c + up * .45f + z.front * 1.2f, new Vector3(2f, .08f, .5f), rot); z.mk.Box(wood, c + up * .85f + z.front * .95f, new Vector3(2f, .5f, .06f), rot); z.solid.Box(wood, c + up * .5f + z.front * 1.2f, new Vector3(2.1f, 1f, .6f), rot); break; }
                case "Elliott Smith": // Condor Ave: a street-corner sign, a streetlamp, a parked car with its lights on
                    { var sp = c + z.side * 2f; z.mk.Prism(S(.3f, .5f, .3f), sp, up, .06f, 2.8f, 6); z.mk.Box(S(.15f, .45f, .25f), sp + up * 2.7f, new Vector3(1.8f, .35f, .05f), rot); ArtistBits.Label(z.labels, "CONDOR AVE", sp + up * 2.7f + z.front * .04f, z.front, .13f, Color.white, TextAnchor.MiddleCenter); ArtistBits.PostCollider(z.root, sp, 2.8f, .1f);
                      VintageCarFolk(z, c - z.side * 1.5f - z.front * 1f, new Color(.45f, .5f, .45f)); foreach (var s in new[] { -.6f, .6f }) z.mk.Prism(SongGlow("2lUejG76ZEmgY7PvzwxxTj", new Color(1f, .95f, .8f), 2.4f), c - z.side * 4.1f - z.front * 1f + z.front * s + up * .75f, -z.side, .15f, .04f, 10); break; }
                case "Fiona Apple": // Fast As You Can: a stretch of running track with lanes and a start line
                    { var tr = S(.75f, .3f, .25f); z.mk.Box(tr, c + up * .03f, new Vector3(6f, .06f, 10f), rot); for (var k = 0; k <= 4; k++) z.mk.Box(S(.95f, .95f, .93f), c + up * .07f + z.side * ((k - 2f) * 1.4f), new Vector3(.08f, .02f, 10f), rot); z.mk.Box(S(.95f, .95f, .93f), c + up * .07f + z.front * 3f, new Vector3(6f, .02f, .15f), rot);
                      foreach (var s in new[] { -1f, 1f }) z.mk.Box(S(.3f, .3f, .32f), c + up * .2f + z.front * 3.4f + z.side * s * .7f, new Vector3(.4f, .2f, .4f), rot * Quaternion.Euler(-30f, 0, 0));
                      foreach (var s in new[] { -1f, 1f }) { var sl = c + z.front * 3f + z.side * s * 3.4f; z.mk.Prism(metal, sl, up, .06f, 1.6f, 6); z.mk.Blob(SongGlow("1UmmSKPdCtBuJsmbZG9G3u", new Color(.4f, 1f, .45f), 2.4f), sl + up * 1.7f, Vector3.one * .16f, z.ctx.Rng, 0f); halos.Add(sl + up * 1.7f, 1.2f, new Color(.4f, 1f, .45f), .5f); ArtistBits.PostCollider(z.root, sl, 1.6f, .1f); } break; }
                case "Josie Edwards": // Chemical Light: a glowing flask on a lab table
                    { z.mk.Box(dark, c + up * .9f, new Vector3(2f, .08f, 1f), rot); foreach (var a in new[] { -.9f, .9f }) foreach (var b in new[] { -.4f, .4f }) z.mk.Box(metal, c + up * .45f + z.side * a + z.front * b, new Vector3(.06f, .9f, .06f), rot); z.solid.Box(dark, c + up * .5f, new Vector3(2f, 1f, 1f), rot);
                      var fl = SongGlow("3PpTiirc8IzcjqEwDyaBZK", new Color(.4f, 1f, .6f), 2f); z.mk.Blob(fl, c + up * 1.3f, new Vector3(.35f, .35f, .35f), z.ctx.Rng, 0f); z.mk.Prism(fl, c + up * 1.55f, up, .1f, .4f, 8); halos.Add(c + up * 1.3f, 2f, new Color(.4f, 1f, .6f), .7f);
                      z.mk.Prism(S(.7f, .8f, .9f, .9f), c + up * .94f + z.side * .6f, up, .1f, .3f, 8); z.mk.Prism(S(.7f, .8f, .9f, .9f), c + up * .94f - z.side * .6f, up, .08f, .45f, 8); break; }
                case "Liana Flores": // Silvershoes, rises the moon: silver shoes on a stump under a big rising moon
                    { z.mk.Prism(bark, c - up * .2f, up, .6f, .8f, 10); z.solid.Prism(bark, c - up * .2f, up, .6f, .8f, 10); foreach (var s in new[] { -.15f, .15f }) z.mk.Box(SongGlow(Silvershoes, new Color(.8f, .88f, 1f), .9f), c + up * .7f + z.side * s, new Vector3(.12f, .12f, .3f), rot); halos.Add(c + up * .9f, 1f, new Color(.8f, .88f, 1f), .4f);
                      var moon = SongGlow("51Grh1RyUDcMBbpuyUIUHI", new Color(.95f, .95f, 1f), 1.6f); z.mk.Blob(moon, c + up * 5f - z.front * 3f, Vector3.one * 1.6f, z.ctx.Rng, 0f); halos.Add(c + up * 5f - z.front * 3f, 3.6f, new Color(.95f, .95f, 1f), .5f); break; }
                case "Lily Meola": // You and I: two swings side by side on one frame
                    { foreach (var s in new[] { -2f, 2f }) { z.mk.Box(wood, c + z.side * s + up * 1.6f, new Vector3(.15f, 3.4f, .15f), rot * Quaternion.Euler(0, 0, s * -3f)); ArtistBits.PostCollider(z.root, c + z.side * s, 3.2f, .15f); } z.mk.Box(wood, c + up * 3.3f, new Vector3(4.4f, .15f, .15f), rot);
                      foreach (var s in new[] { -.9f, .9f }) { foreach (var e in new[] { -.3f, .3f }) z.mk.Box(S(.8f, .75f, .6f), c + z.side * (s + e) + up * 2.1f, new Vector3(.03f, 2.4f, .03f), rot); z.mk.Box(wood, c + z.side * s + up * .9f, new Vector3(.8f, .06f, .3f), rot); }
                      foreach (var s in new[] { -1.7f, 1.7f }) { var lp = c + z.side * s + up * 2.9f; z.mk.Prism(dark, lp + up * .1f, up, .01f, .3f, 4); z.mk.Box(SongGlow("3veAtMxHWM7SbZOXfJ142t", new Color(1f, .7f, .55f), 2.2f), lp, new Vector3(.18f, .24f, .18f), rot); halos.Add(lp, 1.2f, new Color(1f, .7f, .55f), .6f); } break; }
                case "Alice Phoebe Lou": // Society: a busker's corner, an open guitar case, a mic stand, string lights
                    { z.mk.Box(dark, c + up * .1f + z.front * 1f, new Vector3(.4f, .15f, 1.1f), rot * Quaternion.Euler(0, 30f, 0)); for (var k = 0; k < 6; k++) z.mk.Prism(S(.9f, .75f, .3f, .9f), c + up * .19f + z.front * 1f + new Vector3(Mathf.Sin(k) * .12f, 0, Mathf.Cos(k * 2f) * .3f), up, .04f, .01f, 8);
                      z.mk.Prism(metal, c, up, .2f, .04f, 10); z.mk.Prism(metal, c, up, .025f, 1.5f, 6); z.mk.Blob(dark, c + up * 1.55f, new Vector3(.06f, .06f, .1f), z.ctx.Rng, 0f); ArtistBits.PostCollider(z.root, c, 1.5f, .2f);
                      foreach (var s in new[] { -1f, 1f }) { var pp = c + z.side * s * 2.5f - z.front * 1f; z.mk.Prism(wood, pp, up, .07f, 3f, 6); ArtistBits.PostCollider(z.root, pp, 3f, .1f); }
                      for (var k = 1; k < 10; k++) { var t = k / 10f; z.mk.Blob(SongGlow("4s8fwK25mQh35Z5gWby56t", Warm, 2.2f), c - z.front * 1f + z.side * Mathf.Lerp(-2.5f, 2.5f, t) + up * (2.9f - Mathf.Sin(t * Mathf.PI) * .6f), Vector3.one * .08f, z.ctx.Rng, 0f); } break; }
                case "Anna Nalick": // Breathe (2 AM): a bedside table, a lamp, a clock glowing 2:00
                    { z.mk.Box(wood, c + up * .35f, new Vector3(.8f, .7f, .6f), rot); z.solid.Box(wood, c + up * .35f, new Vector3(.8f, .7f, .6f), rot); z.mk.Box(dark, c + up * .85f + z.side * .15f, new Vector3(.35f, .25f, .2f), rot);
                      var digits = SongGlow("5auMzVHFr5Zfw6IbKarZ56", new Color(1f, .25f, .2f), 2f); z.mk.Box(digits, c + up * .85f + z.side * .15f + z.front * .11f, new Vector3(.3f, .14f, .01f), rot); ArtistBits.Label(z.labels, "2:00", c + up * .85f + z.side * .15f + z.front * .12f, z.front, .07f, new Color(1f, .3f, .2f), TextAnchor.MiddleCenter);
                      z.mk.Prism(metal, c + up * .7f - z.side * .2f, up, .03f, .4f, 6); z.mk.Prism(S(.95f, .9f, .8f), c + up * 1.05f - z.side * .2f, up, .2f, .25f, 10, .6f); z.mk.Blob(GlowM(Warm, 1.8f), c + up * 1.08f - z.side * .2f, Vector3.one * .1f, z.ctx.Rng, 0f); halos.Add(c + up * 1.1f, 1.2f, Warm, .5f);
                      z.mk.Box(S(.85f, .85f, .95f), c + up * .35f + z.side * 1.5f - z.front * .3f, new Vector3(1.6f, .5f, 2.2f), rot); z.solid.Box(wood, c + up * .35f + z.side * 1.5f - z.front * .3f, new Vector3(1.6f, .7f, 2.2f), rot); break; }
                case "Jackson Browne": // Running on Empty: an old car pulled up by an empty fuel pump
                    { VintageCarFolk(z, c + z.side * 1.5f, new Color(.7f, .55f, .25f)); var pump = c - z.side * 2f + z.front * .5f; z.mk.Box(S(.8f, .15f, .12f), pump + up * 1f, new Vector3(.7f, 2f, .5f), rot); z.mk.Box(SongGlow("6aqm56xP40foYBBtAWWrnY", new Color(1f, .9f, .6f), 1.6f), pump + up * 1.5f + z.front * .26f, new Vector3(.5f, .4f, .02f), rot); z.mk.Blob(S(.95f, .95f, .9f), pump + up * 2.2f, new Vector3(.3f, .3f, .15f), z.ctx.Rng, 0f); z.solid.Box(S(.8f, .15f, .12f), pump + up * 1f, new Vector3(.7f, 2f, .5f), rot); break; }
                case "Neil Diamond": // Sweet Caroline: a little bandstand strung with bulbs
                    { z.mk.Prism(S(.95f, .95f, .92f), c, up, 3f, .6f, 8); z.solid.Prism(wood, c, up, 3f, .6f, 8); for (var k = 0; k < 8; k++) { var a = k * 45f * Mathf.Deg2Rad; var pp = c + (z.side * Mathf.Cos(a) + z.front * Mathf.Sin(a)) * 2.7f; z.mk.Prism(S(.95f, .95f, .92f), pp + up * .6f, up, .1f, 2.6f, 6); ArtistBits.PostCollider(z.root, pp, 3.2f, .12f); }
                      z.mk.Cone(S(.75f, .2f, .2f), c + up * 3.2f, 3.3f, 1.6f, 8, 22.5f); var bulbs = SongGlow(SweetCaroline, Warm, 2.2f); for (var k = 0; k < 24; k++) { var a = k * 15f * Mathf.Deg2Rad; z.mk.Blob(bulbs, c + up * 3.15f + (z.side * Mathf.Cos(a) + z.front * Mathf.Sin(a)) * 3.2f, Vector3.one * .09f, z.ctx.Rng, 0f); } halos.Add(c + up * 3f, 3.6f, Warm, .4f); break; }
                case "Paul Simon": // Me and Julio Down by the Schoolyard: a schoolyard with a chain-link fence, a hoop and hopscotch
                    { for (var k = 0; k < 5; k++) { var fp = c - z.front * 2f + z.side * ((k - 2f) * 2f); z.mk.Prism(metal, fp, up, .05f, 2.2f, 6); ArtistBits.PostCollider(z.root, fp, 2.2f, .08f); } z.mk.Box(S(.6f, .62f, .65f, .7f), c - z.front * 2f + up * 1.1f, new Vector3(8f, 2f, .02f), rot);
                      var hp = c + z.side * 3f; z.mk.Prism(metal, hp, up, .08f, 3f, 6); z.mk.Box(S(.95f, .95f, .95f), hp + up * 3.1f - z.front * .3f, new Vector3(1.2f, .8f, .05f), rot); z.mk.Prism(SongGlow("4W9b49P72RO0l7FyrLenAm", new Color(1f, .5f, .15f), 1.6f), hp + up * 2.8f + z.front * .2f, up, .25f, .03f, 12); halos.Add(hp + up * 2.8f + z.front * .2f, 1f, new Color(1f, .5f, .15f), .5f); ArtistBits.PostCollider(z.root, hp, 3f, .12f);
                      for (var k = 0; k < 7; k++) z.mk.Box(S(.95f, .95f, .9f), c + up * .03f + z.front * (k * .6f) + z.side * ((k % 3 == 1) ? .35f : 0f), new Vector3(.5f, .02f, .5f), rot); break; }
                case "Randy Newman": // You've Got a Friend in Me: a wooden toy box with a cowboy hat on it and toy blocks round it
                    { z.mk.Box(S(.75f, .5f, .3f), c + up * .5f, new Vector3(1.6f, 1f, 1f), rot); z.solid.Box(S(.75f, .5f, .3f), c + up * .5f, new Vector3(1.6f, 1f, 1f), rot);
                      z.mk.Prism(S(.55f, .35f, .2f), c + up * 1f, up, .6f, .05f, 16); z.mk.Prism(S(.55f, .35f, .2f), c + up * 1.05f, up, .32f, .35f, 12, .85f);
                      z.mk.Prism(SongGlow("5aaUXcrsXI477I93yBE8lu", new Color(1f, .82f, .25f), 2f), c + up * .55f + z.front * .51f, z.front, .28f, .03f, 5); halos.Add(c + up * .55f + z.front * .7f, 1f, new Color(1f, .82f, .25f), .5f);
                      for (var k = 0; k < 5; k++) z.mk.Box(WorldMaterials.Solid(new[] { new Color(1f, .3f, .3f), new Color(.3f, .6f, 1f), new Color(1f, .85f, .2f) }[k % 3], .4f), c + z.front * 1.1f + z.side * ((k - 2f) * .5f) + up * .15f, Vector3.one * .3f, rot * Quaternion.Euler(0, k * 17f, 0)); break; }
                case "Shel Silverstein": // Crouchin' on the Outside: a treehouse in a big oak, a rope ladder down
                    { Oak(z.ctx, z.mk, c, 1.5f); var th = c + up * 4f + z.front * .4f; z.mk.Box(wood, th, new Vector3(2.6f, .15f, 2.4f), rot); z.mk.Box(wood, th + up * .9f - z.front * 1.1f, new Vector3(2.6f, 1.8f, .1f), rot); foreach (var s in new[] { -1.25f, 1.25f }) z.mk.Box(wood, th + up * .9f + z.side * s, new Vector3(.1f, 1.8f, 2.4f), rot);
                      z.mk.GableRoof(S(.55f, .3f, .2f), th + up * 1.8f, new Vector3(2.8f, 0, 2.8f), .9f, Quaternion.LookRotation(z.side)); z.mk.Box(SongGlow("1H7TYyq7yATzT22VSiAFy4", Warm, 1.6f), th + up * 1f - z.front * 1.04f, new Vector3(.6f, .5f, .02f), rot); z.mk.Box(SongGlow("1H7TYyq7yATzT22VSiAFy4", Warm, 2f), th + up * 1.5f + z.front * 1.1f, new Vector3(.2f, .25f, .2f), rot); halos.Add(th + up * 1.5f + z.front * 1.1f, 1.2f, Warm, .6f);
                      for (var k = 0; k < 7; k++) z.mk.Box(wood, th + z.front * 1.1f - up * (k * .55f + .3f), new Vector3(.6f, .05f, .05f), rot); foreach (var s in new[] { -.3f, .3f }) z.mk.Box(S(.8f, .75f, .6f), th + z.front * 1.1f + z.side * s - up * 2f, new Vector3(.03f, 4f, .03f), rot); break; }
                case "Van Morrison": // Brown Eyed Girl: a picnic blanket with a transistor radio and a basket
                    { z.mk.Box(S(.8f, .2f, .2f, .2f), c + up * .03f, new Vector3(3f, .04f, 2.4f), rot); for (var k = 0; k < 4; k++) z.mk.Box(S(.95f, .95f, .9f), c + up * .08f + z.side * ((k - 1.5f) * .7f), new Vector3(.15f, .01f, 2.4f), rot);
                      var radio = c + up * .25f + z.side * .6f; z.mk.Box(S(.2f, .45f, .5f), radio, new Vector3(.5f, .35f, .2f), rot); z.mk.Box(SongGlow("3yrSvpt2l1xhsV9Em88Pul", new Color(1f, .8f, .4f), 1.8f), radio + z.front * .11f, new Vector3(.3f, .1f, .01f), rot); z.mk.Box(metal, radio + up * .35f + z.side * .15f, new Vector3(.02f, .5f, .02f), rot * Quaternion.Euler(0, 0, -20f));
                      z.mk.Box(S(.7f, .55f, .3f), c + up * .2f - z.side * .7f, new Vector3(.6f, .4f, .4f), rot); break; }
                default: // a porch rocker on a little deck
                    { z.mk.Box(wood, c + up * .2f, new Vector3(3f, .4f, 2.4f), rot); z.solid.Box(wood, c + up * .2f, new Vector3(3f, .4f, 2.4f), rot); Chair(z, c + up * .4f, rot, 1f, wood); break; }
            }
        }
    }
}
