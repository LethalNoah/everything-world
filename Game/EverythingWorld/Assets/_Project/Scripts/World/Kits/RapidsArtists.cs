using System.Collections.Generic;
using UnityEngine;
using static EverythingWorld.World.PlaceTools;

namespace EverythingWorld.World
{
    /// The Liquid Rapids' artists. Each has a floor of flat river stones round a little stone basin of glowing water, a
    /// wooden signpost with their name, and one thing from their music by the water; covers on a wooden board under a
    /// lantern.
    /// - Netsky: a sun over a green sugarloaf hill ("Everybody Loves The Sunshine", "Rio").
    /// - High Contrast: a black-and-white disc and a loop of light ("If We Ever", "Return Of Forever").
    /// - The rest: a mirror pool, a pair of wings, a stag, a big volume knob, a little gold skyline, two hands almost
    ///   touching, a panda, an open book, a beach hut, a bunch of balloons, a fire bowl, a ball of energy, a swinging
    ///   pendulum, a brown paper bag, a glowing rose, a stack of speakers, a disc of afterglow, a rainbow prism tree.
    public partial class RapidsKit
    {
        class Plot
        {
            public LandContext ctx; public Transform root, labels; public MeshKit mk, solid, pictures;
            public IslandWorldData.Artist a; public Vector3 c, front, side; public ArtistArea area; public HashSet<string> lit = new HashSet<string>();
            public Vector3 At(float fw, float rt) { var p = c + front * fw + side * rt; return ctx.OnGround(p.x, p.z); }
            public Quaternion Rot => Quaternion.LookRotation(front);
        }

        internal static Dictionary<string, (string title, string id)[]> SongTable => SongIds;
        static string Find(string artist, string title) { if (SongIds.TryGetValue(artist, out var l)) foreach (var (t, id) in l) if (t.StartsWith(title)) return id; return null; }

        public override (string song, string label, string artist)[] SpecialSongs
        {
            get
            {
                var list = new List<(string, string, string)>();
                void Add(string artist, string title, string where) { var id = Find(artist, title); if (id != null) list.Add((id, title, where)); }
                Add("Netsky", "Everybody Loves The Sunshine", "Netsky"); Add("Pendulum", "Witchcraft", "Pendulum"); Add("Wilkinson", "Afterglow", "Wilkinson"); Add("Goldie", "Inner City Life", "Goldie");
                Add("London Elektricity", "Life Is Beautiful", "London Elektricity"); Add("Melano", "On Fire", "Melano"); Add("jungle wizard", "Peace Within Colors", "jungle wizard"); Add("Sub Focus", "Desire", "Sub Focus");
                return list.ToArray();
            }
        }

        /// The song's id (and it is marked as having its own piece). If the title isn't found, the artist's first song.
        string Song(Plot z, string title)
        {
            if (!SongIds.TryGetValue(z.a.name, out var list) || list.Length == 0) return null;
            foreach (var (t, id) in list) if (t.StartsWith(title)) { z.lit.Add(id); return id; }
            z.lit.Add(list[0].id); return list[0].id;
        }

        Material SongGlow(string song, Color c, float s = 1.6f)
        {
            if (song == null) return GlowM(c, s);
            var m = new Material(GlowM(c, s)) { name = "Song glow" }; life.songGlows.Add((song, m, c * (s * .65f))); return m;
        }

        void Say1(Vector3 at, string text) => PlaceTools.Say(tlife.transform, at, text, 4.5f);

        public override void BuildArtist(LandContext ctx, IslandWorldData.Artist a)
        {
            if (stone == null) MakeMaterials();
            var root = new GameObject("Artist: " + a.name).transform; root.SetParent(ctx.Root, false);
            var front = facing.TryGetValue(a.name, out var f) ? f : new Vector3(center.x - a.x, 0, center.z - a.z).normalized;
            if (front.sqrMagnitude < .01f) front = Vector3.forward;
            var labels = new GameObject("Labels").transform; labels.SetParent(root, false);
            var area = root.gameObject.AddComponent<ArtistArea>();
            area.artistName = a.name; area.stationId = a.stationId; area.radius = a.r; area.glowColor = Gold;
            var z = new Plot { ctx = ctx, root = root, labels = labels, mk = new MeshKit(), solid = new MeshKit(), pictures = new MeshKit(), a = a, c = ctx.OnGround(a.x, a.z), front = front, side = Vector3.Cross(Vector3.up, front), area = area };
            var reach = Foot(a); var up = Vector3.up; var rng = new System.Random(a.name.Length * 13 + a.name[0]);
            z.mk.Prism(stoneD, z.c - up * .3f, up, reach * .75f, .36f, 24);
            for (var k = 0; k < 18; k++) { var d = Quaternion.Euler(0, k * 20f + (float)rng.NextDouble() * 8f, 0) * front; z.mk.Prism(k % 2 == 0 ? stone : path, z.c + d * reach * (.3f + (k % 3) * .14f) + up * .06f, up, .5f + (float)rng.NextDouble() * .3f, .02f, 7); }
            // the little basin of glowing water, off to one side
            var bs = z.At(reach * .1f, reach * .42f); z.mk.Prism(stoneD, bs, up, 1f, .5f, 14); z.mk.Prism(waterM, bs + up * .53f, up, .82f, .02f, 14); ArtistBits.PostCollider(root, bs, .5f, 1f); halos.Add(bs + up * .9f, 1.4f, Water, .3f);
            if (a.name == "Netsky") Netsky(z); else if (a.name == "High Contrast") Contrast(z); else Others(z);
            Signpost(z);
            SongLamps(z);
            Board(z, z.At(reach * .6f, -reach * .45f - 1.5f), front);
            ArtistBits.Plaque(z.mk, z.solid, labels, a, z.At(reach * .6f + 1.5f, reach * .45f + 1.5f), front, iron, wood, Ink, 1.6f);
            Lamp(ctx, z.mk, z.At(reach * .5f, reach * .7f), false);
            if (z.area.glowLight == null) z.area.glowLight = ArtistBits.PointLight(root, z.c + up * 4f + front * 3f, Gold, 13f, 1.2f);
            area.glowRenderers = new Renderer[0];
            z.mk.Build("Area", root, false);
            z.solid.BuildCollider("Walls", root);
            z.pictures.Build("Covers", root, false);
            noGrass.Add(new Vector3(z.c.x, reach * .85f, z.c.z));
            var mark = new GameObject("Shot: " + a.name + " up close").transform; mark.SetParent(ctx.Root, false);
            var cam = z.At(reach * .85f, reach * .3f) + up * 2f; var look = z.At(-reach * .3f, 0) + up * 1.8f - cam; mark.SetPositionAndRotation(cam, Quaternion.LookRotation(look.normalized));
        }

        /// The covers on a wooden board with a little lantern on top.
        void Board(Plot z, Vector3 ground, Vector3 dir)
        {
            dir = Vector3.ProjectOnPlane(dir, Vector3.up).normalized; var rot = Quaternion.LookRotation(dir); var up = Vector3.up; var sd = Vector3.Cross(up, dir);
            var n = Mathf.Max(1, Mathf.Min(7, z.a.songList?.Length ?? 1)); const float size = .9f; var w = n * size * 1.25f + .6f;
            var c = ground + up * 1.7f;
            z.mk.Box(wood, c - up * .1f - dir * .04f, new Vector3(w + .3f, size + 1.1f, .16f), rot); z.solid.Box(wood, c - up * .1f, new Vector3(w + .3f, size + 1.1f, .3f), rot);
            foreach (var e in new[] { -1f, 1f }) { z.mk.Prism(woodD, ground + sd * e * (w / 2f), up, .07f, 2.4f, 6); ArtistBits.PostCollider(z.root, ground + sd * e * (w / 2f), 2.4f, .1f); }
            z.mk.Prism(lampM, c + up * .55f + dir * .2f, up, .1f, .2f, 6); halos.Add(c + up * .65f + dir * .4f, .8f, Gold, .4f);
            ArtistBits.CoverWall(z.pictures, z.labels, z.ctx, z.a, c + dir * .1f, dir, n, size, n, true, Ink);
        }

        /// A wooden signpost with their name.
        void Signpost(Plot z)
        {
            var up = Vector3.up; var g = z.At(Foot(z.a) * .2f, -Foot(z.a) * .55f); z.mk.Prism(woodD, g - up * .2f, up, .08f, 2.4f, 6); ArtistBits.PostCollider(z.root, g, 2.4f, .1f);
            var sr = Quaternion.LookRotation(z.front) * Quaternion.Euler(0, 0, 4f); z.mk.Box(wood, g + up * 1.9f + z.front * .1f, new Vector3(Mathf.Max(1.4f, z.a.name.Length * .13f), .4f, .06f), sr);
            ArtistBits.Label(z.labels, z.a.name, g + up * 1.9f + z.front * .14f, z.front, .1f, Ink, TextAnchor.MiddleCenter);
        }

        /// A little glowing float on a reed for each song without its own piece.
        void SongLamps(Plot z)
        {
            if (!SongIds.TryGetValue(z.a.name, out var list)) return;
            var up = Vector3.up; var k = 0; var seen = new HashSet<string>();
            foreach (var (_, id) in list)
            {
                if (z.lit.Contains(id) || !seen.Add(id)) continue;
                var p = z.At(Foot(z.a) * .55f - (k % 2) * 1.3f, Foot(z.a) * .2f + (k / 2) * 1.3f);
                z.mk.Prism(woodD, p, up, .04f, 1.2f, 5); z.mk.Blob(SongGlow(id, Gold, 1.6f), p + up * 1.3f, Vector3.one * .14f, new System.Random(k), 0f); ArtistBits.PostCollider(z.root, p, 1.3f, .08f);
                k++;
            }
        }

        // ---------------------------------------------------------------- Netsky
        void Netsky(Plot z)
        {
            var up = Vector3.up; var rng = new System.Random(2010); var fr = z.front; var sd = z.side;
            var sun = Song(z, "Everybody Loves The Sunshine"); var rio = Song(z, "Rio"); life.sunshineSong = sun;
            var hill = z.At(-3.4f, -1.4f); z.mk.Prism(leafM, hill, up, 1.6f, 4.4f, 10, .25f); z.mk.Prism(Paint(new Color(.5f, .46f, .4f)), hill, up, 1.65f, 1.4f, 10, .8f); ArtistBits.PostCollider(z.root, hill, 4.4f, 1.6f);
            z.mk.Box(SongGlow(rio, new Color(.9f, .94f, 1f), 1f), hill + up * 4.6f, new Vector3(.1f, .7f, .1f), z.Rot); z.mk.Box(SongGlow(rio, new Color(.9f, .94f, 1f), 1f), hill + up * 4.8f, new Vector3(.6f, .08f, .08f), z.Rot);
            var sm = new MeshKit(); sm.Prism(SongGlow(sun, Gold, 1.8f), Vector3.zero, Vector3.back, 1.1f, .2f, 24); for (var k = 0; k < 12; k++) { var d = Quaternion.Euler(0, 0, k * 30f) * Vector3.up; sm.Box(GlowM(Gold, 1.2f), d * 1.6f, new Vector3(.12f, .6f, .06f), Quaternion.LookRotation(Vector3.forward, d)); }
            var sg = sm.BuildSingle("Netsky sun", z.root, false); var sp = z.At(-2.6f, 2f) + up * 5f; sg.transform.SetPositionAndRotation(sp, Quaternion.LookRotation(fr)); life.spinners.Add(sg.transform); halos.Add(sp + fr * .6f, 3f, Gold, .4f);
            z.mk.Prism(iron, z.At(-2.6f, 2f), up, .06f, 3.8f, 5); ArtistBits.PostCollider(z.root, z.At(-2.6f, 2f), 3.8f, .1f);
            for (var k = 0; k < 3; k++) Bench(z.root, z.mk, z.At(1.6f, -3f + k * 3f), fr, 1.6f);
            Folk(z.At(2f, 3.6f), -fr, Resident.Pose.Wave, new[] { "Everybody loves the sunshine. Even at sunset.", "Sit, face the sun, let it go down slow.", "That hill's from somewhere far away. Somewhere with a beach." }, Local());
            z.area.glowLight = ArtistBits.PointLight(z.root, z.c + up * 4f, Gold, 18f, 1.4f);
        }

        // ---------------------------------------------------------------- High Contrast
        void Contrast(Plot z)
        {
            var up = Vector3.up; var fr = z.front; var sd = z.side;
            var ever = Song(z, "If We Ever"); var fore = Song(z, "Return Of Forever");
            var dc = new PlaceFrame(z.At(-3f, -1.6f), fr); z.mk.Prism(dark, dc.P(0, 2.4f, 0), -dc.Fwd, 2f, .2f, 32); z.mk.Prism(white, dc.P(0, 2.4f, .02f), dc.Fwd, 2f, .02f, 32, 1f, 0f, false); z.mk.Box(SongGlow(ever, White, 1.4f), dc.P(0, 2.4f, .13f), new Vector3(4f, .14f, .02f), dc.r); z.mk.Box(dark, dc.P(0, 2.4f, .14f), new Vector3(.14f, 4f, .02f), dc.r); z.mk.Prism(iron, dc.P(0, 0, -.2f), up, .1f, .5f, 6); BoxCol(z.root, dc.P(0, 2.4f, 0), new Vector3(4f, 4f, .3f), dc.r);
            var lm = new MeshKit(); var lmat = SongGlow(fore, new Color(.6f, .8f, 1f), 1.4f); for (var k = 0; k < 40; k++) { var t = k / 40f * Mathf.PI * 2f; var t2 = (k + 1) / 40f * Mathf.PI * 2f; Vector3 P(float a) => new Vector3(Mathf.Sin(a) * 1.4f, Mathf.Sin(a * 2f) * .6f, 0); var a0 = P(t); var a1 = P(t2); lm.Box(lmat, (a0 + a1) / 2f, new Vector3(.08f, .08f, (a1 - a0).magnitude + .02f), Quaternion.LookRotation(a1 - a0)); }
            var lg = lm.BuildSingle("Loop of forever", z.root, false); var lp = z.At(-2.4f, 2.6f) + up * 2.4f; lg.transform.SetPositionAndRotation(lp, Quaternion.LookRotation(fr)); life.spinners.Add(lg.transform); z.mk.Prism(iron, z.At(-2.4f, 2.6f), up, .05f, 1.6f, 5); ArtistBits.PostCollider(z.root, z.At(-2.4f, 2.6f), 1.6f, .1f); halos.Add(lp + fr * .5f, 2f, new Color(.6f, .8f, 1f), .3f);
            Folk(z.At(2f, -3f), -fr, Resident.Pose.Stand, new[] { "Black and white, and somehow it's the most colourful thing here.", "If we ever... you know. Come back here." }, Local());
            z.area.glowLight = ArtistBits.PointLight(z.root, z.c + up * 4f, new Color(.8f, .9f, 1f), 16f, 1.3f);
        }

        // ---------------------------------------------------------------- everyone else
        void Others(Plot z)
        {
            var up = Vector3.up; var rot = z.Rot; var rng = new System.Random(z.a.name.Length * 7 + z.a.name[0]); var c = z.At(-1f, -1f) + up * .1f; var fr = z.front; var sd = z.side;
            void Post(Vector3 at, float h, float r = .04f) { z.mk.Prism(iron, at - up * .2f, up, r, h + .2f, 5); ArtistBits.PostCollider(z.root, at, h, r + .03f); }
            string Any() => SongIds.TryGetValue(z.a.name, out var l) && l.Length > 0 ? Song(z, l[0].title) : null;
            switch (z.a.name)
            {
                case "B-complex": { var s = Any(); z.mk.Prism(stoneD, c, up, 1.6f, .3f, 18); z.mk.Prism(SongGlow(s, new Color(.8f, .9f, 1f), 1.2f), c + up * .3f, up, 1.4f, .02f, 18); for (var k = 0; k < 2; k++) z.mk.Box(white, c + up * 1.2f - fr * .9f + sd * (k - .5f) * 1.2f, new Vector3(.9f, 1.8f, .06f), rot * Quaternion.Euler(0, (k - .5f) * 30f, 0)); BoxCol(z.root, c + up * .2f, new Vector3(3.2f, .4f, 3.2f), rot); BoxCol(z.root, c + up * 1f - fr * .9f, new Vector3(2.4f, 2f, .2f), rot); break; }
                case "Birdy": { var s = Any(); Post(c, 1.6f, .05f); foreach (var e in new[] { -1f, 1f }) for (var k = 0; k < 5; k++) z.mk.Box(k == 0 ? SongGlow(s, White, 1.2f) : white, c + up * (2f + k * .1f) + sd * e * (.4f + k * .32f), new Vector3(.5f, .14f, .7f - k * .1f), rot * Quaternion.Euler(0, 0, e * (20f + k * 6f))); break; }
                case "Calibre": { var s = Any(); var bm = Paint(new Color(.5f, .38f, .28f)); z.mk.Box(bm, c + up * 1.1f, new Vector3(.5f, .6f, 1.4f), rot); foreach (var a in new[] { -1f, 1f }) foreach (var b in new[] { -.5f, .5f }) z.mk.Prism(bm, c + sd * a * .18f + fr * b, up, .07f, .9f, 5); z.mk.Box(bm, c + up * 1.6f + fr * .8f, new Vector3(.2f, .6f, .2f), rot * Quaternion.Euler(-30f, 0, 0)); z.mk.Box(bm, c + up * 1.9f + fr * 1f, new Vector3(.24f, .26f, .4f), rot); foreach (var e in new[] { -1f, 1f }) for (var k = 0; k < 3; k++) z.mk.Box(SongGlow(k == 0 ? s : null, Gold, 1f), c + up * (2.2f + k * .2f) + fr * .9f + sd * e * (.15f + k * .15f), new Vector3(.04f, .3f, .04f), rot * Quaternion.Euler(0, 0, e * -30f)); BoxCol(z.root, c + up * 1f, new Vector3(.6f, 2f, 1.8f), rot); break; }
                case "Camo & Krooked": { var s = Any(); z.mk.Prism(dark, c, up, .9f, 1.2f, 20); z.mk.Prism(iron, c + up * 1.2f, up, .7f, .3f, 20); z.mk.Box(SongGlow(s, Gold, 1.6f), c + up * 1.5f + fr * .4f, new Vector3(.08f, .04f, .5f), rot); for (var k = 0; k < 9; k++) { var d = Quaternion.Euler(0, -120f + k * 30f, 0) * fr; z.mk.Box(GlowM(k < 7 ? Gold : Ember, .8f), c + up * .62f + d * 1.0f, new Vector3(.1f, .08f, .04f), Quaternion.LookRotation(d)); } ArtistBits.PostCollider(z.root, c, 1.5f, .9f); break; }
                case "Goldie": { var s = Any(); life.cityLifeSong = s; z.mk.Box(stoneD, c + up * .2f, new Vector3(3f, .4f, 1f), rot); for (var k = 0; k < 7; k++) { var h = 1f + (k * 37 % 5) * .4f; var bp = c + sd * (k - 3f) * .4f + up * (.4f + h / 2f); z.mk.Box(Paint(new Color(.3f, .26f, .2f)), bp, new Vector3(.36f, h, .5f), rot); for (var w = 0; w < (int)(h * 2f); w++) z.mk.Box(SongGlow(k == 3 && w == 0 ? s : null, Gold, 1.4f), bp + fr * .26f + up * (-h / 2f + .3f + w * .45f), new Vector3(.12f, .14f, .02f), rot); } BoxCol(z.root, c + up * 1.2f, new Vector3(3f, 2.4f, 1f), rot); break; }
                case "Hybrid Minds": { var s = Any(); foreach (var e in new[] { -1f, 1f }) { var hp = c + sd * e * .7f + up * 1.8f; z.mk.Box(white, hp, new Vector3(.6f, .2f, .3f), rot * Quaternion.Euler(0, 0, e * 10f)); for (var k = 0; k < 4; k++) z.mk.Box(white, hp - sd * e * (.4f + (k == 0 ? .1f : 0f)) + up * (k - 1.5f) * .1f, new Vector3(.4f, .06f, .08f), rot); Post(c + sd * e * 1f, 1.6f, .04f); } z.mk.Blob(SongGlow(s, Gold, 1.8f), c + up * 1.8f, Vector3.one * .08f, rng, 0f); halos.Add(c + up * 1.8f + fr * .3f, .8f, Gold, .5f); break; }
                case "LAOS": { var s = Any(); var w = white; var b = dark; z.mk.Blob(w, c + up * .7f, new Vector3(.8f, .7f, .7f), rng, 0f); z.mk.Blob(w, c + up * 1.6f, new Vector3(.55f, .5f, .5f), rng, 0f); foreach (var e in new[] { -1f, 1f }) { z.mk.Blob(b, c + up * 2f + sd * e * .4f, Vector3.one * .16f, rng, 0f); z.mk.Blob(b, c + up * 1.7f + fr * .42f + sd * e * .18f, new Vector3(.12f, .14f, .04f), rng, 0f); z.mk.Blob(b, c + up * .9f + sd * e * .75f + fr * .2f, new Vector3(.2f, .4f, .2f), rng, 0f); z.mk.Blob(b, c + up * .2f + sd * e * .4f + fr * .4f, new Vector3(.24f, .2f, .3f), rng, 0f); } z.mk.Prism(SongGlow(s, Leaf, 1.2f), c + up * 1.3f + fr * .5f + sd * .6f, (up + fr).normalized, .03f, 1.2f, 5); ArtistBits.PostCollider(z.root, c, 2f, .8f); break; }
                case "Lenzman": { var s = Any(); z.mk.Box(woodD, c + up * .5f, new Vector3(1.4f, 1f, .8f), rot); foreach (var e in new[] { -1f, 1f }) z.mk.Box(white, c + up * 1.06f + sd * e * .5f, new Vector3(.9f, .06f, .7f), rot * Quaternion.Euler(0, 0, e * -8f)); z.mk.Box(SongGlow(s, Gold, 1.2f), c + up * 1.12f, new Vector3(.04f, .02f, .6f), rot); halos.Add(c + up * 1.4f, 1f, Gold, .4f); BoxCol(z.root, c + up * .55f, new Vector3(1.6f, 1.1f, .9f), rot); break; }
                case "Logistics": { var s = Any(); var hut = new PlaceFrame(c - fr * .4f, fr); var sc = new[] { new Color(.3f, .6f, .9f), White }; for (var k = 0; k < 6; k++) z.mk.Box(Paint(sc[k % 2]), hut.P(-.9f + k * .36f, 1.1f, 0), new Vector3(.36f, 2.2f, 1.6f), hut.r); z.mk.Prism(Paint(new Color(.8f, .3f, .24f)), hut.P(0, 2.2f, 0), up, 1.5f, .7f, 4, .05f, 45f); z.mk.Box(SongGlow(s, Gold, 1.2f), hut.P(0, .9f, .81f), new Vector3(.7f, 1.6f, .02f), hut.r); BoxCol(z.root, hut.P(0, 1.1f, 0), new Vector3(2.2f, 2.2f, 1.6f), hut.r); z.mk.Box(Paint(new Color(.9f, .8f, .55f)), c + fr * 1.2f + up * .02f, new Vector3(2.6f, .02f, 1.2f), rot); break; }
                case "London Elektricity": { var s = Any(); life.beautifulSong = s; var cols = new[] { new Color(1f, .5f, .6f), new Color(1f, .85f, .4f), new Color(.5f, .8f, 1f), new Color(.7f, 1f, .6f), new Color(.86f, .6f, 1f) }; var bal = new MeshKit(); for (var k = 0; k < 7; k++) { var bp = new Vector3(Mathf.Sin(k * 1.7f) * .7f, 2.2f + (k % 3) * .4f, Mathf.Cos(k * 1.3f) * .5f); bal.Blob(k == 0 ? SongGlow(s, cols[0], 1.2f) : GlowM(cols[k % 5], .9f), bp, new Vector3(.32f, .4f, .32f), rng, 0f); bal.Box(Paint(White), bp * .5f, new Vector3(.01f, bp.magnitude, .01f), Quaternion.FromToRotation(Vector3.up, bp)); } var bg = bal.BuildSingle("Balloons", z.root, false); bg.transform.position = c; life.bobbers.Add((bg.transform, c)); z.mk.Box(woodD, c + up * .2f, new Vector3(.4f, .4f, .4f), rot); ArtistBits.PostCollider(z.root, c, .4f, .3f); break; }
                case "Melano": { var s = Any(); life.fireSong = s; z.mk.Prism(iron, c, up, .3f, .7f, 10, 3f); z.mk.Prism(iron, c + up * .7f, up, 1f, .2f, 16); for (var k = 0; k < 7; k++) z.mk.Prism(k == 0 ? SongGlow(s, Ember, 2f) : GlowM(k % 2 == 0 ? Ember : Gold, 1.6f), c + up * .9f + Quaternion.Euler(0, k * 51f, 0) * fr * (k == 0 ? 0f : .4f), up, .2f, .5f + (k % 3) * .3f, 5, .05f); life.fires.Add(c + up * 1.3f); ArtistBits.PointLight(z.root, c + up * 1.6f, Ember, 8f, 1.4f); ArtistBits.PostCollider(z.root, c, 1f, 1f); break; }
                case "Metrik": { var s = Any(); var em = new MeshKit(); em.Blob(SongGlow(s, new Color(.4f, .8f, 1f), 1.8f), Vector3.zero, Vector3.one * .5f, rng, 0f); for (var k = 0; k < 3; k++) em.Prism(GlowM(new Color(.4f, .8f, 1f), .7f), Vector3.zero, Quaternion.Euler(k * 60f, k * 40f, 0) * Vector3.up, .9f, .03f, 28); var eg = em.BuildSingle("Energy", z.root, false); eg.transform.position = c + up * 2f; life.spinners.Add(eg.transform); Post(c, 1.2f, .06f); halos.Add(c + up * 2f, 2f, new Color(.4f, .8f, 1f), .4f); break; }
                case "Pendulum": { var s = Any(); life.witchSong = s; foreach (var e in new[] { -1f, 1f }) { z.mk.Prism(dark, c + sd * e * 1.4f, up, .1f, 4.4f, 6); ArtistBits.PostCollider(z.root, c + sd * e * 1.4f, 4.4f, .14f); } z.mk.Box(dark, c + up * 4.4f, new Vector3(3f, .16f, .16f), rot); var pm = new MeshKit(); pm.Box(iron, Vector3.down * 1.6f, new Vector3(.04f, 3.2f, .04f), Quaternion.identity); pm.Prism(SongGlow(s, new Color(.7f, .4f, 1f), 1.6f), Vector3.down * 3.2f, Vector3.forward, .4f, .12f, 18); pm.Prism(iron, Vector3.down * 3.2f + Vector3.back * .01f, Vector3.back, .44f, .04f, 18); var pg = pm.BuildSingle("Pendulum", z.root, false); pg.transform.SetPositionAndRotation(c + up * 4.4f, rot); life.pendulums.Add((pg.transform, rot)); break; }
                case "Roni Size": { var s = Any(); var bm = Paint(new Color(.66f, .5f, .32f)); z.mk.Box(bm, c + up * .9f, new Vector3(1.2f, 1.8f, .7f), rot); z.mk.Box(Paint(new Color(.56f, .42f, .26f)), c + up * 1.85f, new Vector3(1.2f, .1f, .74f), rot * Quaternion.Euler(6f, 0, 0)); z.mk.Box(SongGlow(s, Gold, 1f), c + up * 1f + fr * .36f, new Vector3(.6f, .5f, .02f), rot); BoxCol(z.root, c + up * .9f, new Vector3(1.2f, 1.8f, .7f), rot); break; }
                case "Sub Focus": { var s = Any(); life.desireSong = s; z.mk.Prism(Paint(new Color(.3f, .5f, .3f)), c, up, .04f, 1.6f, 5); foreach (var e in new[] { -1f, 1f }) z.mk.Box(leafM, c + up * .7f + sd * e * .18f, new Vector3(.3f, .04f, .14f), rot * Quaternion.Euler(0, 0, e * 30f)); var rose = SongGlow(s, new Color(1f, .2f, .3f), 1.8f); for (var k = 0; k < 7; k++) z.mk.Blob(rose, c + up * (1.7f + (k / 3) * .1f) + Quaternion.Euler(0, k * 51f, 0) * fr * (k == 0 ? 0f : .14f), new Vector3(.14f, .18f, .1f), rng, 0f); halos.Add(c + up * 1.8f, 1.2f, new Color(1f, .2f, .3f), .4f); ArtistBits.PostCollider(z.root, c, 1.8f, .2f); break; }
                case "Uk Apache": { var s = Any(); for (var k = 0; k < 6; k++) { var sp = c + sd * ((k % 3) - 1f) * 1f + up * (.5f + (k / 3) * 1f); z.mk.Box(dark, sp, new Vector3(.95f, .95f, .8f), rot); z.mk.Prism(iron, sp + fr * .41f, fr, .34f, .02f, 16); z.mk.Prism(k == 1 ? SongGlow(s, Gold, 1.4f) : GlowM(Gold, .6f), sp + fr * .43f, fr, .1f, .02f, 10); life.speakers.Add(sp + fr * .5f); } BoxCol(z.root, c + up * 1f, new Vector3(3f, 2f, .8f), rot); break; }
                case "Wilkinson": { var s = Any(); life.afterglowSong = s; var gm = new MeshKit(); gm.Prism(SongGlow(s, new Color(1f, .5f, .4f), 1.6f), Vector3.zero, Vector3.back, 1.4f, .1f, 32); for (var k = 1; k <= 3; k++) gm.Prism(GlowM(Color.Lerp(new Color(1f, .5f, .4f), new Color(.9f, .4f, .8f), k / 3f), .6f - k * .12f), Vector3.forward * k * .04f, Vector3.back, 1.4f + k * .4f, .02f, 32); var gg = gm.BuildSingle("Afterglow", z.root, false); gg.transform.SetPositionAndRotation(c + up * 2.6f - fr * .4f, rot); Post(c - fr * .5f, 1.2f, .06f); halos.Add(c + up * 2.6f, 3f, new Color(1f, .5f, .4f), .3f); break; }
                case "jungle wizard": { var s = Any(); life.colorsSong = s; z.mk.Prism(trunkM, c, up, .2f, 1.8f, 7, .6f); var cols = new[] { new Color(1f, .3f, .3f), new Color(1f, .6f, .2f), new Color(1f, .9f, .3f), new Color(.4f, .9f, .4f), new Color(.3f, .6f, 1f), new Color(.6f, .4f, 1f) }; for (var k = 0; k < 12; k++) { var d = Quaternion.Euler(0, k * 30f, 0) * fr; z.mk.Prism(k == 0 ? SongGlow(s, cols[0], 1.6f) : GlowM(cols[k % 6], 1.2f), c + up * (1.8f + (k % 3) * .3f) + d * (.5f + (k % 2) * .4f), (up + d * .6f).normalized, .16f, .5f, 3, .1f); } halos.Add(c + up * 2.2f, 2f, new Color(.8f, .7f, 1f), .3f); ArtistBits.PostCollider(z.root, c, 1.8f, .3f); break; }
                default: { var s = Any(); Post(c, 1.4f); z.mk.Blob(SongGlow(s, Gold, 1.4f), c + up * 1.5f, Vector3.one * .16f, rng, 0f); break; }
            }
        }

        static readonly Color White = new Color(.94f, .94f, .92f);
    }
}
