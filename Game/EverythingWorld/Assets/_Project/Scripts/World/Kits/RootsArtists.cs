using System.Collections.Generic;
using UnityEngine;
using static EverythingWorld.World.PlaceTools;

namespace EverythingWorld.World
{
    /// The Roots Bay's artists. Most have a painted board house of their own with a verandah, a stool and a flag, and
    /// one thing from their song beside it; covers on a cream card on a red, gold and green board.
    /// - Bob Marley & The Wailers (the beach): the Sound System, speaker boxes stacked as tall as a house, hand
    ///   painted; a selector's table with a turntable to play; a yard fire with a pot on it and logs to sit on ("No
    ///   Woman, No Cry": the rainbow blazes); a line painted on a zinc fence ("Trenchtown Rock"); a sheriff's star
    ///   nailed to a post ("I Shot the Sheriff").
    /// - Toots & The Maytals: the dance hall. An open-sided hall under a zinc roof: a stage with an organ and drums
    ///   to play, a floor of people dancing, a cell door numbered 54-46 standing open, a rain barrel under a dripping
    ///   gutter ("Pressure Drop": the rain comes back).
    /// - The Specials: one grey street on a green island: three shut shops with TO LET in the windows, black and white
    ///   paving, a message chalked on a board for Rudy ("Ghost Town": the colour drains from everything).
    /// - Jimmy Cliff: a tall leaning tower of crates, a film projector and a record counter. Desmond Dekker: a
    ///   breakfast table, a torn shirt on a line, a spade. Peter Tosh: a herb garden behind a painted gate, a
    ///   unicycle. Burning Spear: a spear with a flame for a point, a black star, a ship in red, black and green.
    ///   Lee "Scratch" Perry: a studio shack covered in handprints, a mixing desk to dub on, an iron shirt chasing a
    ///   small red devil. Also Gregory Isaacs' night lamp and hammock, Damian Marley's road sign, Chronixx's sugar
    ///   cane, Koffee's toast, Shabba Ranks' gold chain, Shaggy's alibi, the Baha Men's dogs, Major Lazer's lights.
    public partial class RootsKit
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
                Add("Bob Marley & The Wailers", "No Woman, No Cry", "@the beach and the rainbow"); Add("The Specials", "Ghost Town", "The Specials"); Add("Toots & The Maytals", "Pressure Drop", "@the yard");
                Add("Gregory Isaacs", "Night Nurse", "@down bay road to the rainbow"); Add("Bob Marley & The Wailers", "Trenchtown Rock", "Bob Marley & The Wailers");
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
            if (earthM == null) MakeMaterials();
            var root = new GameObject("Artist: " + a.name).transform; root.SetParent(ctx.Root, false);
            var front = facing.TryGetValue(a.name, out var f) ? f : new Vector3(center.x - a.x, 0, center.z - a.z).normalized;
            if (front.sqrMagnitude < .01f) front = Vector3.forward;
            var labels = new GameObject("Labels").transform; labels.SetParent(root, false);
            var area = root.gameObject.AddComponent<ArtistArea>();
            area.artistName = a.name; area.stationId = a.stationId; area.radius = a.r; area.glowColor = Gold;
            var z = new Plot { ctx = ctx, root = root, labels = labels, mk = new MeshKit(), solid = new MeshKit(), pictures = new MeshKit(), a = a, c = ctx.OnGround(a.x, a.z), front = front, side = Vector3.Cross(Vector3.up, front), area = area };
            var reach = Foot(a); var up = Vector3.up;
            z.mk.Prism(a.name == "Bob Marley & The Wailers" ? sandM : a.name == "The Specials" ? Paint(new Color(.5f, .5f, .52f)) : earthM, z.c - up * .25f, up, reach * .66f, .33f, 22);
            switch (a.name)
            {
                case "Bob Marley & The Wailers": BobMarley(z); break;
                case "Toots & The Maytals": Toots(z); break;
                case "The Specials": Specials(z); break;
                default: House(z); Others(z); break;
            }
            SongLamps(z);
            Board(z, z.At(reach * .6f, -reach * .45f - 1.5f), front);
            ArtistBits.Plaque(z.mk, z.solid, labels, a, z.At(reach * .6f + 1.5f, reach * .45f + 1.5f), front, woodM, Paint(Gold), Ink, 1.6f);
            FlagPole(ctx, z.mk, z.At(reach * .5f, reach * .64f), z.side);
            if (z.area.glowLight == null) z.area.glowLight = ArtistBits.PointLight(root, z.c + up * 5f + front * 3f, new Color(1f, .92f, .7f), HasStation(a) ? 24f : 16f, 1.1f);
            area.glowRenderers = new Renderer[0];
            z.mk.Build("Area", root, false);
            z.solid.BuildCollider("Walls", root);
            z.pictures.Build("Covers", root, false);
            noGrass.Add(new Vector3(z.c.x, reach * .9f, z.c.z));
            if (HasStation(a) || reach >= 13f)
            {
                var mark = new GameObject("Shot: " + a.name + " up close").transform; mark.SetParent(ctx.Root, false);
                var cam = z.At(reach * .85f, reach * .3f) + up * 2f; var look = z.At(-reach * .3f, 0) + up * 2.4f - cam; mark.SetPositionAndRotation(cam, Quaternion.LookRotation(look.normalized));
            }
        }

        /// The covers on a cream card on a board painted in three bands: red, gold, green.
        void Board(Plot z, Vector3 ground, Vector3 dir)
        {
            dir = Vector3.ProjectOnPlane(dir, Vector3.up).normalized; var rot = Quaternion.LookRotation(dir);
            var n = Mathf.Max(1, Mathf.Min(7, z.a.songList?.Length ?? 1)); const float size = .9f; var w = n * size * 1.25f + .6f;
            var c = ground + Vector3.up * 1.7f; var grey = z.a.name == "The Specials";
            for (var k = 0; k < 3; k++) z.mk.Box(grey ? Paint(new Color(.3f + k * .12f, .3f + k * .12f, .32f + k * .12f)) : Paint(new[] { Green, Gold, Red }[k]), c + Vector3.up * (-.5f + (k - 1) * (size + 2f) / 3f) - dir * .06f, new Vector3(w + .5f, (size + 2f) / 3f, .2f), rot);
            z.solid.Box(woodM, c - Vector3.up * .5f, new Vector3(w + .5f, size + 2f, .3f), rot); z.mk.Box(Paint(Cream), c - Vector3.up * .1f + dir * .05f, new Vector3(w, size + .9f, .04f), rot);
            ArtistBits.CoverWall(z.pictures, z.labels, z.ctx, z.a, c + dir * .08f + Vector3.up * .08f, dir, n, size, n, true, Ink);
        }

        /// A painted calabash lamp on a post for each song without its own piece.
        void SongLamps(Plot z)
        {
            if (!SongIds.TryGetValue(z.a.name, out var list)) return;
            var up = Vector3.up; var k = 0; var seen = new HashSet<string>();
            foreach (var (_, id) in list)
            {
                if (z.lit.Contains(id) || !seen.Add(id)) continue;
                var p = z.At(Foot(z.a) * .55f - (k % 2) * 1.3f, Foot(z.a) * .2f + (k / 2) * 1.3f);
                z.mk.Prism(woodM, p - up * .1f, up, .04f, 1.3f, 5); z.mk.Blob(SongGlow(id, new[] { Red, Gold, Green }[k % 3], 1.6f), p + up * 1.42f, Vector3.one * .16f, new System.Random(k), .1f); ArtistBits.PostCollider(z.root, p, 1.3f, .08f);
                k++;
            }
        }

        /// A painted board house of their own: a verandah with a stool on it.
        PlaceFrame House(Plot z)
        {
            var hue = z.a.name.Length + z.a.name[0]; var f = new PlaceFrame(z.At(-Foot(z.a) * .46f, 0), z.front); var po = Shack(z.ctx, z.mk, z.solid, f, 5f, 3.4f, hue);
            Stool(z.mk, po.P(-1.5f, 0, .1f), (po.Fwd + po.Right * .2f).normalized, hue);
            return po;
        }

        // ---------------------------------------------------------------- Bob Marley & The Wailers: the Sound System
        void BobMarley(Plot z)
        {
            var up = Vector3.up; var rot = z.Rot; var rng = new System.Random(75); var nw = Song(z, "No Woman"); var tr = Song(z, "Trenchtown Rock"); var sh = Song(z, "I Shot The Sheriff"); foreach (var s in new[] { nw, tr, sh }) if (s != null) life.bob.Add(s); life.cryString = nw;
            // the Sound System: two wings of speaker boxes five high, a taller middle, a painted board across the top
            var c = z.At(-5f, 0); SpeakerStack(z.root, z.mk, z.solid, c, rot, 4, 6, 0); SpeakerStack(z.root, z.mk, z.solid, c - z.side * 5.4f + z.front * 1.2f, rot * Quaternion.Euler(0, -24f, 0), 3, 5, 1); SpeakerStack(z.root, z.mk, z.solid, c + z.side * 5.4f + z.front * 1.2f, rot * Quaternion.Euler(0, 24f, 0), 3, 5, 2);
            for (var k = 0; k < 3; k++) z.mk.Box(Paint(new[] { Red, Gold, Green }[k]), c + up * 9.1f + z.side * (-2f + k * 2f) + z.front * .2f, new Vector3(2f, 1.1f, .2f), rot); ArtistBits.Label(z.labels, "ONE LOVE SOUND", c + up * 9.1f + z.front * .32f, z.front, .5f, Ink, TextAnchor.MiddleCenter); foreach (var e in new[] { -2.8f, 2.8f }) z.mk.Prism(woodM, c + up * 8.4f + z.side * e, up, .06f, 1.2f, 4);
            z.area.glowLight = ArtistBits.PointLight(z.root, c + up * 4f + z.front * 4f, new Color(1f, .85f, .5f), 30f, 1.6f); halos.Add(c + up * 4f + z.front * 1.4f, 7f, Gold, .18f);
            // the selector's table: one turntable, a box of records; drop the needle
            var tb = c + z.front * 4.4f; z.mk.Box(Paint(Green), tb + up * .5f, new Vector3(2.4f, 1f, .8f), rot); z.mk.Box(Paint(Gold), tb + up * 1.02f, new Vector3(2.5f, .05f, .9f), rot); BoxCol(z.root, tb + up * .5f, new Vector3(2.4f, 1f, .8f), rot); z.mk.Box(zinc, tb + up * 1.08f - z.side * .5f, new Vector3(.6f, .08f, .5f), rot); z.mk.Prism(dark, tb + up * 1.13f - z.side * .5f, up, .22f, .012f, 16); z.mk.Prism(SongGlow(tr, Red, 1.2f), tb + up * 1.14f - z.side * .5f, up, .07f, .01f, 10); z.mk.Box(woodM, tb + up * 1.2f + z.side * .6f, new Vector3(.5f, .3f, .4f), rot); for (var k = 0; k < 6; k++) z.mk.Box(Paint(Paints[k % Paints.Length]), tb + up * 1.36f + z.side * (.42f + k * .07f), new Vector3(.02f, .3f, .34f), rot);
            string[] drop = { "The needle goes down. One beat of nothing. Then the bass arrives and the whole beach leans back.", "Rewind! Somebody shouts it, so you do: pull it back and drop it again from the top.", "One good thing about music: when it hits you, you feel no pain. It hits.", "The one-drop. The space where the first beat should be is the best part." }; var dn = 0;
            Spot("The selector's table", tb + up * 1.4f + z.front * .5f, "Drop the needle", new Vector3(2.4f, 1f, 1.2f)).onUse = _ => { life.played = Time.time; Say1(tb + up * 2.8f, drop[dn++ % drop.Length]); };
            Folk(tb - z.front * .9f + z.side * .6f, z.front, Resident.Pose.Work, new[] { "Selector's choice this morning. And the selector chooses: all of them.", "We built it one box at a time. Every box, somebody painted. That green one's my grandmother's.", "Stand there. No, there. That's where the bass meets itself." }).workRate = 1.2f;
            // "No Woman, No Cry": the yard fire: a pot on it, logs to sit on
            var fp = z.At(4f, 7.5f) + up * .06f; for (var k = 0; k < 8; k++) z.mk.Blob(Paint(new Color(.6f, .58f, .54f)), fp + Quaternion.Euler(0, k * 45f, 0) * Vector3.forward * .8f + up * .1f, Vector3.one * .18f, rng, .2f); for (var k = 0; k < 3; k++) { var d = Quaternion.Euler(0, k * 60f, 0) * Vector3.forward; z.mk.Prism(woodM, fp - d * .7f + up * .1f, (d + up * .2f).normalized, .08f, 1.4f, 5); } z.mk.Cone(fireM, fp + up * .14f, .34f, .9f, 6); z.mk.Cone(SongGlow(nw, new Color(1f, .6f, .2f), 1.6f), fp + up * .14f + z.side * .14f, .2f, .6f, 5, 30f);
            for (var k = 0; k < 3; k++) z.mk.Prism(iron, fp + Quaternion.Euler(0, k * 120f, 0) * Vector3.forward * .7f, (up * 1.4f - Quaternion.Euler(0, k * 120f, 0) * Vector3.forward * .7f).normalized, .02f, 1.56f, 3); z.mk.Prism(dark, fp + up * .86f, up, .26f, .3f, 10, 1.2f); z.mk.Prism(Paint(Cream), fp + up * 1.14f, up, .28f, .02f, 10); halos.Add(fp + up * .6f, 2f, new Color(1f, .6f, .25f), .4f); ArtistBits.PostCollider(z.root, fp, 1.4f, .7f); life.sparks.Add(fp + up * .9f);
            for (var k = 0; k < 3; k++) { var d = Quaternion.Euler(0, k * 100f - 60f, 0) * z.front; var lg = fp + d * 2.4f; var along = Vector3.Cross(up, d); z.mk.Prism(trunkM, lg + up * .2f - along * .8f, along, .2f, 1.6f, 7); BoxCol(z.root, lg + up * .2f, new Vector3(.4f, .4f, 1.6f), Quaternion.LookRotation(along)); Interact.Bench(tlife.transform, lg + up * .42f - along * .4f, lg + up * .42f + along * .4f, -d); }
            // "Trenchtown Rock": a line painted on a zinc fence. "I Shot the Sheriff": a star nailed to a post
            var zf = z.At(3f, -8f); var zr = rot * Quaternion.Euler(0, 30f, 0); for (var k = 0; k < 7; k++) z.mk.Box(k % 3 == 0 ? rustM : zinc, zf + zr * new Vector3(-2.1f + k * .7f, 1f + (k % 2) * .06f, 0), new Vector3(.7f, 2f, .05f), zr); BoxCol(z.root, zf + up * 1f, new Vector3(5f, 2f, .1f), zr); ArtistBits.Label(z.labels, "when it hits you\nfeel no pain", zf + up * 1.2f + zr * Vector3.forward * .05f, zr * Vector3.forward, .28f, Red, TextAnchor.MiddleCenter);
            var sp = z.At(8f, 1f); z.mk.Prism(woodM, sp - up * .2f, up, .07f, 2.4f, 5); ArtistBits.PostCollider(z.root, sp, 2.2f, .09f); var star = SongGlow(sh, Gold, 1.6f); for (var k = 0; k < 5; k++) z.mk.Box(star, sp + up * 1.9f + z.front * .08f + Quaternion.AngleAxis(k * 72f, z.front) * up * .13f, new Vector3(.08f, .26f, .02f), Quaternion.AngleAxis(k * 72f, z.front) * rot); z.mk.Prism(star, sp + up * 1.9f + z.front * .06f, z.front, .09f, .03f, 8); ArtistBits.Label(z.labels, "(but not the deputy)", sp + up * 1.5f + z.front * .09f, z.front, .09f, Ink, TextAnchor.MiddleCenter);
            Shot(z.ctx, "the sound system", z.At(13f, 5f) + up * 2.4f, c + up * 4.4f);
        }

        // ---------------------------------------------------------------- Toots & The Maytals: the dance hall
        void Toots(Plot z)
        {
            var up = Vector3.up; var rot = z.Rot; var rng = new System.Random(68); var ff = Song(z, "54-46"); var pd = Song(z, "Pressure Drop"); life.pressureSong = pd; var fo = new PlaceFrame(z.At(-3.5f, 0) + up * .12f, z.front); const float W = 12f, Dp = 9f, H = 4.2f;
            // an open-sided hall: a plank floor, posts, a zinc roof, a stage along the back
            z.mk.Box(plank, fo.P(0, -.02f, 0), new Vector3(W, .16f, Dp), fo.r); z.solid.Box(plank, fo.P(0, -.4f, 0), new Vector3(W, .9f, Dp), fo.r); for (var x = 0; x < 8; x++) for (var y = 0; y < 6; y++) if ((x + y) % 2 == 0) z.mk.Box(Paint(Cream), fo.P(-W / 2f + .75f + x * 1.5f, .065f, -Dp / 2f + .75f + y * 1.5f), new Vector3(1.5f, .01f, 1.5f), fo.r);
            foreach (var (x, zz) in new[] { (-1f, -1f), (1f, -1f), (1f, 1f), (-1f, 1f), (0f, -1f), (0f, 1f) }) { var p = fo.P(x * (W / 2f - .3f), 0, zz * (Dp / 2f - .3f)); z.mk.Prism(Paint(new[] { Red, Gold, Green }[Mathf.Abs((int)(x + zz * 2)) % 3]), p, up, .12f, H, 6); ArtistBits.PostCollider(z.root, p, H, .14f); }
            z.mk.GableRoof(zinc, fo.P(0, H, 0), new Vector3(Dp + 1.4f, 0, W + 1.4f), 1.6f, Quaternion.LookRotation(fo.Right)); ArtistBits.Label(z.labels, "DANCE HALL", fo.P(0, H + .6f, Dp / 2f + .72f), z.front, .5f, Ink, TextAnchor.MiddleCenter); z.mk.Box(Paint(Gold), fo.P(0, H + .6f, Dp / 2f + .68f), new Vector3(5f, .9f, .06f), fo.r);
            var sz = -Dp / 2f + 1.4f; z.mk.Box(Paint(Red), fo.P(0, .3f, sz), new Vector3(8f, .5f, 2.4f), fo.r); z.solid.Box(plank, fo.P(0, .2f, sz), new Vector3(8f, .7f, 2.4f), fo.r);
            var og = fo.P(-2.4f, .55f, sz); z.mk.Box(woodM, og + up * .5f, new Vector3(1.4f, 1f, .6f), fo.r); z.mk.Box(white, og + up * 1.02f + z.front * .2f, new Vector3(1.3f, .04f, .22f), fo.r); BoxCol(z.root, og + up * .5f, new Vector3(1.4f, 1f, .6f), fo.r);
            string[] ska = { "Chk-a, chk-a, chk-a. The off-beat. Your right hand is now in charge of everyone's knees.", "The organ bubbles. Somebody shouts \"give it to me one time!\" You give it to them one time.", "You hold the skank and the floor does the rest.", "Two times! Three times! Four times!" }; var sn = 0; Spot("The dance hall's organ", og + up * 1.3f + z.front * .5f, "Play the off-beat", new Vector3(1.4f, 1f, 1f)).onUse = _ => { life.played = Time.time; Say1(og + up * 2.8f, ska[sn++ % ska.Length]); };
            var dk = fo.P(1.8f, .55f, sz - .2f); var shell = Paint(Gold); z.mk.Prism(shell, dk + up * .3f - z.front * .2f, z.front, .42f, .4f, 12); z.mk.Prism(shell, dk + up * .5f + z.side * .7f, up, .26f, .2f, 10); z.mk.Prism(zinc, dk + up * 1.15f - z.side * .8f, up, .32f, .02f, 12); z.mk.Prism(iron, dk - z.side * .8f, up, .015f, 1.15f, 4); z.mk.Prism(iron, fo.P(-.2f, .55f, sz + .6f), up, .018f, 1.45f, 4); z.mk.Blob(zinc, fo.P(-.2f, 2.05f, sz + .6f), new Vector3(.06f, .09f, .06f), rng, 0f);
            Folk(fo.P(-1.6f, .06f, 1.4f), -z.front, Resident.Pose.Wave, new[] { "Knees up! Elbows out! That's it, that's the whole dance!", "They let him out and he wrote a song with his number in it. That's how you win." }); Folk(fo.P(2.2f, .06f, .6f), (-z.front - z.side * .4f).normalized, Resident.Pose.Wave, new[] { "It is you! Oh yeah!", "When the pressure drop, you gonna feel it. I feel it already." });
            z.area.glowLight = ArtistBits.PointLight(z.root, fo.P(0, 3f, 0), new Color(1f, .9f, .6f), 24f, 1.4f);
            // "54-46 Was My Number": a cell door standing open, the number on it
            var cd = z.At(5f, 8f); var cr = rot * Quaternion.Euler(0, -40f, 0); z.mk.Box(iron, cd + up * 1.2f, new Vector3(.12f, 2.4f, .12f), cr); z.mk.Box(iron, cd + up * 2.36f + cr * Vector3.right * .6f, new Vector3(1.3f, .1f, .1f), cr); z.mk.Box(iron, cd + up * .06f + cr * Vector3.right * .6f, new Vector3(1.3f, .1f, .1f), cr); for (var k = 0; k < 6; k++) z.mk.Prism(iron, cd + cr * new Vector3(.12f + k * .2f, .1f, 0), up, .025f, 2.2f, 4); BoxCol(z.root, cd + up * 1.2f + cr * Vector3.right * .6f, new Vector3(1.3f, 2.4f, .1f), cr);
            z.mk.Box(SongGlow(ff, Gold, .8f), cd + up * 1.5f + cr * new Vector3(.62f, 0, .06f), new Vector3(.7f, .3f, .02f), cr); ArtistBits.Label(z.labels, "54-46", cd + up * 1.5f + cr * new Vector3(.62f, 0, .08f), cr * Vector3.forward, .2f, Ink, TextAnchor.MiddleCenter);
            // "Pressure Drop": a rain barrel under a dripping gutter
            var rb = z.At(5f, -8f); z.mk.Prism(woodM, rb, up, .5f, 1f, 10); z.mk.Prism(iron, rb + up * .3f, up, .52f, .05f, 10); z.mk.Prism(iron, rb + up * .8f, up, .52f, .05f, 10); z.mk.Prism(SongGlow(pd, new Color(.5f, .8f, 1f), .8f), rb + up * .98f, up, .46f, .02f, 10); ArtistBits.PostCollider(z.root, rb, 1f, .52f); z.mk.Prism(zinc, rb + up * 2.6f - z.side * .8f, z.side, .06f, 1.2f, 5); z.mk.Prism(SongGlow(pd, new Color(.5f, .8f, 1f), 1.4f), rb + up * 2.5f + z.side * .3f, -up, .012f, 1.4f, 3); z.mk.Prism(woodM, rb - z.side * .8f - up * .2f, up, .05f, 2.9f, 4);
            Shot(z.ctx, "the dance hall", z.At(13f, -4f) + up * 2.2f, fo.P(0, 2.4f, 0));
        }

        // ---------------------------------------------------------------- The Specials: the grey street
        void Specials(Plot z)
        {
            var up = Vector3.up; var rot = z.Rot; var rng = new System.Random(81); var gt = Song(z, "Ghost Town"); var ru = Song(z, "A Message to You Rudy"); life.ghostSong = gt; var c = z.At(-4f, 0);
            // black and white paving, and three shops with their shutters down
            for (var x = 0; x < 10; x++) for (var y = 0; y < 5; y++) if ((x + y) % 2 == 0) z.mk.Box(dark, z.c + up * .09f + z.side * (-4.05f + x * .9f) + z.front * (-1.5f + y * .9f), new Vector3(.9f, .01f, .9f), rot); z.mk.Box(white, z.c + up * .085f + z.front * .3f, new Vector3(9f, .01f, 4.5f), rot);
            for (var k = 0; k < 3; k++)
            {
                var f = new PlaceFrame(c + z.side * (-4.4f + k * 4.4f), z.front); var g = .34f + k * .08f; var wall = Paint(new Color(g, g, g + .02f));
                z.mk.Box(wall, f.P(0, 2.4f, 0), new Vector3(4.3f, 4.8f, 3f), f.r); z.solid.Box(wall, f.P(0, 2.4f, 0), new Vector3(4.3f, 4.8f, 3f), f.r); z.mk.Box(dark, f.P(0, 4.9f, .1f), new Vector3(4.4f, .2f, 3.3f), f.r); z.mk.Box(zinc, f.P(.5f, 1.3f, 1.52f), new Vector3(2.6f, 2.2f, .06f), f.r); for (var q = 0; q < 9; q++) z.mk.Box(dark, f.P(.5f, .3f + q * .24f, 1.56f), new Vector3(2.6f, .03f, .02f), f.r); z.mk.Box(dark, f.P(-1.5f, 1.1f, 1.52f), new Vector3(.9f, 2.2f, .06f), f.r);
                z.mk.Box(k == 1 ? SongGlow(gt, new Color(.9f, .9f, .86f), .4f) : white, f.P(.5f, 3f, 1.52f), new Vector3(3.4f, .7f, .06f), f.r); ArtistBits.Label(z.labels, new[] { "TO LET", "CLOSED DOWN", "ALL MUST GO" }[k], f.P(.5f, 3f, 1.57f), z.front, .26f, Ink, TextAnchor.MiddleCenter); z.mk.Box(dark, f.P(0, 4f, 1.52f), new Vector3(1.2f, .9f, .06f), f.r);
            }
            z.area.glowLight = ArtistBits.PointLight(z.root, c + up * 3f + z.front * 4f, new Color(.8f, .82f, .88f), 22f, 1f);
            // a sheet of newspaper, a lamp post that doesn't work, a small grey car
            for (var k = 0; k < 4; k++) z.mk.Box(white, z.At(2f + k * .9f, -3f + k * 1.6f) + up * .11f, new Vector3(.4f, .01f, .56f), Quaternion.Euler(0, k * 50f, 0)); var lp = z.At(1f, 6.4f); z.mk.Prism(iron, lp - up * .2f, up, .07f, 4.6f, 6); z.mk.Prism(iron, lp + up * 4.3f, (z.front + up * .2f).normalized, .04f, 1f, 5); z.mk.Blob(dark, lp + up * 4.4f + z.front * .9f, new Vector3(.16f, .12f, .16f), rng, 0f); ArtistBits.PostCollider(z.root, lp, 4.4f, .1f);
            var car = z.At(3.6f, 2f) + up * .1f; var cr = rot * Quaternion.Euler(0, 80f, 0); var cf = cr * Vector3.forward; var cs = cr * Vector3.right; var grey = Paint(new Color(.5f, .52f, .56f)); z.mk.Box(grey, car + up * .6f, new Vector3(1.6f, .5f, 3.8f), cr); z.mk.Box(grey, car + up * 1.08f - cf * .1f, new Vector3(1.5f, .5f, 2f), cr); z.mk.Box(dark, car + up * 1.1f - cf * .1f, new Vector3(1.54f, .36f, 1.7f), cr); foreach (var e in new[] { -1f, 1f }) foreach (var q in new[] { -1.2f, 1.2f }) z.mk.Prism(dark, car + up * .32f + cs * e * .74f + cf * q, cs * e, .32f, .2f, 10); BoxCol(z.root, car + up * .7f, new Vector3(1.6f, 1.4f, 3.8f), cr);
            // "A Message to You Rudy": chalked on a board
            var mb = z.At(5f, -6f); foreach (var e in new[] { -.9f, .9f }) z.mk.Prism(woodM, mb + z.side * e - up * .2f, up, .05f, 2.2f, 4); z.mk.Box(dark, mb + up * 1.5f, new Vector3(2.2f, 1.3f, .06f), rot); BoxCol(z.root, mb + up * 1.2f, new Vector3(2.2f, 2f, .1f), rot); ArtistBits.Label(z.labels, "RUDY:\nstop your messing around\nbetter think of your future", mb + up * 1.5f + z.front * .04f, z.front, .12f, Cream, TextAnchor.MiddleCenter); z.mk.Box(SongGlow(ru, Cream, 1.2f), mb + up * .94f + z.front * .04f, new Vector3(1.8f, .03f, .01f), rot);
            var hat = z.At(6.4f, 4f); z.mk.Prism(iron, hat, up, .14f, .03f, 6); z.mk.Prism(iron, hat, up, .02f, 1.5f, 4); z.mk.Prism(dark, hat + up * 1.5f, up, .2f, .02f, 10); z.mk.Prism(dark, hat + up * 1.52f, up, .12f, .14f, 10); z.mk.Prism(white, hat + up * 1.54f, up, .125f, .04f, 10); ArtistBits.PostCollider(z.root, hat, 1.6f, .14f);
            Folk(z.At(1.4f, -5f), z.front, Resident.Pose.Lean, new[] { "This town is coming like a ghost town. All the clubs have been closed down.", "It rained here once and never quite stopped. Not the weather. The other thing.", "Do you remember the good old days? No? No. Me neither. But we danced." });
        }

        // ---------------------------------------------------------------- everyone else
        void Others(Plot z)
        {
            var up = Vector3.up; var rot = z.Rot; var p = z.At(2.8f, -3.2f); var rng = new System.Random(z.a.name.Length * 3); string s;
            switch (z.a.name)
            {
                case "Jimmy Cliff":
                    s = Song(z, "The Harder They Come"); for (var k = 0; k < 7; k++) { z.mk.Box(k == 6 ? SongGlow(s, Gold, .7f) : Paint(Paints[k % Paints.Length]), p + up * (.35f + k * .7f) + z.side * (k * k * .03f), new Vector3(.9f - k * .04f, .68f, .9f - k * .04f), rot * Quaternion.Euler(0, k * 7f, k * 1.6f)); } BoxCol(z.root, p + up * 2.4f, new Vector3(1f, 4.8f, 1f), rot); ArtistBits.Label(z.labels, "the harder they come\nthe harder they fall", p + up * 5.6f, z.front, .16f, Ink, TextAnchor.MiddleCenter);
                    var pj = z.At(4.4f, 1.4f); z.mk.Prism(iron, pj, up, .03f, 1.3f, 4); z.mk.Box(dark, pj + up * 1.4f, new Vector3(.3f, .3f, .5f), rot); foreach (var e in new[] { -.2f, .2f }) z.mk.Prism(dark, pj + up * 1.7f + z.front * e - z.side * .03f, z.side, .16f, .06f, 10); ArtistBits.PostCollider(z.root, pj, 1.4f, .1f); break;
                case "Desmond Dekker & The Aces":
                    s = Song(z, "Israelites"); life.israelitesSong = s; z.mk.Box(plank, p + up * .74f, new Vector3(1.3f, .06f, .8f), rot); foreach (var (x, zz) in new[] { (-.55f, -.3f), (.55f, -.3f), (.55f, .3f), (-.55f, .3f) }) z.mk.Prism(woodM, p + z.side * x + z.front * zz, up, .03f, .74f, 4); BoxCol(z.root, p + up * .4f, new Vector3(1.3f, .8f, .8f), rot); var bm = new MeshKit(); bm.Blob(Paint(new Color(.85f, .66f, .36f)), new Vector3(0, .06f, 0), new Vector3(.16f, .07f, .09f), rng, .1f); Thing(bm, "Bread", "a loaf of hard-dough bread", p + up * .78f, rot, new Vector3(.32f, .14f, .18f), .2f, false); z.mk.Prism(white, p + up * .78f + z.side * .4f, up, .05f, .09f, 8);
                    Stool(z.mk, p + z.front * .9f, -z.front, 1); var la = z.At(3f, 3f); var lb = z.At(5.6f, 4f); foreach (var g in new[] { la, lb }) z.mk.Prism(woodM, g - up * .2f, up, .04f, 2.2f, 4); z.mk.Prism(dark, la + up * 1.9f, (lb - la).normalized, .01f, (lb - la).magnitude, 3); var mid = (la + lb) / 2f + up * 1.5f; z.mk.Box(SongGlow(s, Cream, .4f), mid, new Vector3(.05f, .7f, .7f), Quaternion.LookRotation((lb - la).normalized)); z.mk.Box(dark, mid - up * .1f, new Vector3(.06f, .3f, .1f), Quaternion.LookRotation((lb - la).normalized) * Quaternion.Euler(20f, 0, 0));
                    z.mk.Prism(woodM, z.At(5.4f, 1f), (up + z.side * .2f).normalized, .03f, 1.4f, 4); z.mk.Box(zinc, z.At(5.4f, 1f) + up * .1f - z.side * .04f, new Vector3(.24f, .3f, .03f), rot); break;
                case "Peter Tosh":
                    s = Song(z, "Legalize It"); life.legalizeSong = s; var herb = SongGlow(s, new Color(.3f, .8f, .3f), .4f); for (var r = 0; r < 3; r++) for (var k = 0; k < 5; k++) { var hp = p + z.side * (-1.4f + k * .7f) - z.front * (r * .8f - .4f); hp.y = z.ctx.Ground(hp.x, hp.z); z.mk.Prism(leafDark, hp, up, .02f, 1.2f, 3); for (var q = 0; q < 5; q++) z.mk.Box(herb, hp + up * (.5f + q * .16f), new Vector3(.5f - q * .07f, .02f, .12f), Quaternion.Euler(0, q * 72f + k * 30f, 14f)); }
                    foreach (var e in new[] { -2f, 2f }) { z.mk.Prism(Paint(Red), p + z.side * e + z.front * 1.2f - up * .2f, up, .07f, 2.4f, 5); ArtistBits.PostCollider(z.root, p + z.side * e + z.front * 1.2f, 2.2f, .09f); } z.mk.Box(Paint(Gold), p + up * 2.2f + z.front * 1.2f, new Vector3(4.2f, .6f, .08f), rot); ArtistBits.Label(z.labels, "LEGALIZE IT", p + up * 2.2f + z.front * 1.25f, z.front, .34f, Green, TextAnchor.MiddleCenter);
                    var uc = z.At(4.6f, 2f); z.mk.Prism(dark, uc + up * .34f - z.side * .03f, z.side, .34f, .06f, 14); z.mk.Prism(zinc, uc + up * .34f, up, .02f, .8f, 4); z.mk.Box(dark, uc + up * 1.16f, new Vector3(.14f, .05f, .28f), rot); ArtistBits.PostCollider(z.root, uc, 1.2f, .2f); Guitar(z.mk, z.At(4.6f, .6f), rot, Paint(new Color(.2f, .2f, .22f)), rng); break;
                case "Burning Spear":
                    s = Song(z, "Marcus Garvey"); z.mk.Prism(Paint(new Color(.6f, .58f, .54f)), p, up, .7f, .8f, 8); z.mk.Prism(woodM, p + up * .8f, up, .05f, 3.2f, 5); z.mk.Cone(zinc, p + up * 4f, .12f, .5f, 4); z.mk.Cone(SongGlow(s, new Color(1f, .55f, .12f), 2f), p + up * 4.3f, .16f, .6f, 5); halos.Add(p + up * 4.5f, 1.6f, new Color(1f, .6f, .2f), .5f); ArtistBits.PostCollider(z.root, p, 2f, .7f);
                    var bs = z.At(4.6f, 1.6f); z.mk.Prism(woodM, bs - up * .2f, up, .05f, 2.6f, 4); for (var k = 0; k < 5; k++) z.mk.Box(dark, bs + up * 2.6f + z.front * .03f + Quaternion.AngleAxis(k * 72f, z.front) * up * .22f, new Vector3(.14f, .44f, .03f), Quaternion.AngleAxis(k * 72f, z.front) * rot); z.mk.Prism(dark, bs + up * 2.6f + z.front * .02f, z.front, .16f, .03f, 8); ArtistBits.PostCollider(z.root, bs, 2.4f, .07f);
                    var sh = z.At(1.4f, 4.6f) + up * .5f; z.mk.Box(woodM, sh - up * .3f, new Vector3(1.6f, .3f, .5f), rot); for (var k = 0; k < 3; k++) z.mk.Box(Paint(new[] { Red, Ink, Green }[k]), sh + up * (.0f + k * .12f), new Vector3(1.4f - k * .2f, .12f, .4f), rot); z.mk.Prism(woodM, sh + up * .3f, up, .02f, .8f, 4); z.mk.Tri(white, sh + up * 1.1f, sh + up * .4f, sh + up * .4f + z.side * .5f, sh - z.front); z.mk.Tri(white, sh + up * .4f, sh + up * 1.1f, sh + up * .4f + z.side * .5f, sh + z.front); break;
                case "Lee \"Scratch\" Perry":
                    s = Song(z, "Chase the Devil"); life.devilSong = s; for (var k = 0; k < 16; k++) { var hp = z.At(-Foot(z.a) * .46f, 0) + up * (1f + (k * 7 % 5) * .4f) + z.side * (-2.2f + (k * 5 % 9) * .55f) + z.front * 1.72f; z.mk.Blob(Paint(Paints[k % Paints.Length]), hp, new Vector3(.1f, .12f, .02f), rng, .2f); for (var q = 0; q < 4; q++) z.mk.Box(Paint(Paints[k % Paints.Length]), hp + up * .14f + z.side * (-.06f + q * .04f), new Vector3(.025f, .1f, .02f), rot); }
                    z.mk.Box(dark, p + up * .86f, new Vector3(1.4f, .14f, .7f), rot * Quaternion.Euler(-14f, 0, 0)); foreach (var e in new[] { -.6f, .6f }) z.mk.Prism(iron, p + z.side * e, up, .02f, .8f, 4); for (var k = 0; k < 16; k++) z.mk.Prism(k % 4 == 0 ? SongGlow(s, Red, 1.6f) : (Material)zinc, p + up * .96f + z.side * (-.56f + (k % 8) * .16f) + z.front * ((k / 8) * .24f - .1f), up, .03f, .05f, 6); BoxCol(z.root, p + up * .5f, new Vector3(1.4f, 1f, .7f), rot);
                    string[] dub = { "You pull the bass out. The room hangs in the air. You drop it back and it lands on everybody at once.", "Echo: ...evil ...evil ...evil. Out of earth ...earth ...earth.", "You turn one knob. The cymbal goes off into space and comes back wet.", "A cow moos in the mix. You didn't put it there. He did. Leave it." }; var dn = 0; Spot("The mixing desk", p + up * 1.3f, "Dub it", new Vector3(1.4f, .9f, 1f)).onUse = _ => { life.played = Time.time; Say1(p + up * 2.6f, dub[dn++ % dub.Length]); };
                    var isx = z.At(4.6f, 2.4f); z.mk.Prism(woodM, isx - up * .2f, up, .04f, 2.2f, 4); z.mk.Box(zinc, isx + up * 1.5f + z.front * .2f, new Vector3(.6f, .7f, .08f), rot); foreach (var e in new[] { -1f, 1f }) z.mk.Box(zinc, isx + up * 1.6f + z.front * .2f + z.side * e * .42f, new Vector3(.3f, .16f, .08f), rot * Quaternion.Euler(0, 0, e * 20f)); var dv = z.At(5.6f, -1f); z.mk.Blob(SongGlow(s, new Color(1f, .15f, .1f), 1.6f), dv + up * .3f, new Vector3(.1f, .16f, .08f), rng, 0f); foreach (var e in new[] { -.05f, .05f }) z.mk.Cone(SongGlow(s, new Color(1f, .15f, .1f), 1.6f), dv + up * .44f + z.side * e, .02f, .08f, 3); z.mk.Prism(Paint(Red), dv + up * .2f - z.front * .1f, (-z.front + up * .5f).normalized, .01f, .3f, 3); break;
                case "Gregory Isaacs":
                    s = Song(z, "Night Nurse"); life.nurseSong = s; z.mk.Prism(woodM, p - up * .2f, up, .05f, 2.4f, 4); z.mk.Blob(SongGlow(s, new Color(.5f, .6f, 1f), 1.8f), p + up * 2.5f, Vector3.one * .26f, rng, 0f); z.mk.Box(white, p + up * 2.5f + z.front * .26f, new Vector3(.2f, .06f, .01f), rot); z.mk.Box(white, p + up * 2.5f + z.front * .26f, new Vector3(.06f, .2f, .01f), rot); ArtistBits.PostCollider(z.root, p, 2.2f, .07f); Hammock(z.ctx, z.mk, z.At(4.6f, 1.6f), z.side, 2); ArtistBits.Label(z.labels, "only you alone\ncan quench this thirst", p + up * 1.4f, z.front, .1f, Ink, TextAnchor.MiddleCenter); break;
                case "Damian Marley":
                    s = Song(z, "Welcome To Jamrock"); foreach (var e in new[] { -1.8f, 1.8f }) { z.mk.Prism(zinc, p + z.side * e - up * .2f, up, .06f, 3.2f, 5); ArtistBits.PostCollider(z.root, p + z.side * e, 3f, .08f); } z.mk.Box(Paint(Green), p + up * 2.6f, new Vector3(4.2f, 1.1f, .08f), rot); z.mk.Box(SongGlow(s, Gold, .8f), p + up * 2.06f + z.front * .05f, new Vector3(4f, .05f, .02f), rot); ArtistBits.Label(z.labels, "WELCOME TO\nJAMROCK", p + up * 2.62f + z.front * .05f, z.front, .32f, Cream, TextAnchor.MiddleCenter);
                    SpeakerStack(z.root, z.mk, z.solid, z.At(4.6f, 2f), rot * Quaternion.Euler(0, 30f, 0), 1, 2, 3); break;
                case "Chronixx":
                    s = Song(z, "Skankin' Sweet"); for (var k = 0; k < 7; k++) z.mk.Prism(SongGlow(s, new Color(.6f, .8f, .3f), .3f), p + z.side * (-.6f + k * .2f) + z.front * ((k % 2) * .16f), (up + z.side * (k - 3) * .04f).normalized, .03f, 2.4f + (k % 3) * .3f, 5); ArtistBits.PostCollider(z.root, p, 2f, .7f); Hammock(z.ctx, z.mk, z.At(4.6f, 1.6f), z.side, 0);
                    var cn = new MeshKit(); cn.Prism(Paint(new Color(.6f, .8f, .3f)), Vector3.zero, Vector3.up, .03f, .6f, 5); Thing(cn, "Sugar cane", "a stick of sugar cane", p + z.front * 1f + up * .12f, rot * Quaternion.Euler(80f, 0, 0), new Vector3(.1f, .1f, .6f), .2f); break;
                case "Original Koffee":
                    s = Song(z, "Toast"); z.mk.Prism(woodM, p, up, .04f, .74f, 5); z.mk.Prism(Paint(Gold), p + up * .74f, up, .5f, .05f, 12); ArtistBits.PostCollider(z.root, p, .8f, .4f); for (var k = 0; k < 3; k++) { var gm = new MeshKit(); gm.Prism(SongGlow(s, new[] { Red, Gold, Green }[k], .8f), Vector3.zero, Vector3.up, .04f, .13f, 8, 1.2f); Thing(gm, "Cup", "a cup raised in thanks", p + up * .8f + Quaternion.Euler(0, k * 120f, 0) * Vector3.forward * .24f, Quaternion.identity, new Vector3(.1f, .16f, .1f), .15f, false); }
                    for (var k = 0; k < 3; k++) Stool(z.mk, p + Quaternion.Euler(0, k * 120f + 60f, 0) * z.front * 1f, -(Quaternion.Euler(0, k * 120f + 60f, 0) * z.front), k); ArtistBits.Label(z.labels, "blessings all pon mi life", p + up * 1.5f, z.front, .12f, Ink, TextAnchor.MiddleCenter); break;
                case "Shabba Ranks":
                    s = Song(z, "Mr. Loverman"); z.mk.Prism(woodM, p - up * .2f, up, .05f, 2.2f, 4); ArtistBits.PostCollider(z.root, p, 2f, .07f); var ch = SongGlow(s, Gold, 1.2f); for (var k = 0; k < 18; k++) { var a = k * 20f * Mathf.Deg2Rad; z.mk.Blob(ch, p + up * (1.5f + Mathf.Sin(a) * .5f) + z.side * Mathf.Cos(a) * .4f + z.front * .08f, Vector3.one * .05f, rng, 0f); } z.mk.Blob(ch, p + up * .9f + z.front * .08f, new Vector3(.12f, .12f, .03f), rng, 0f); z.mk.Prism(leafDark, p + z.side * .7f, up, .01f, .5f, 3); z.mk.Blob(Paint(Red), p + z.side * .7f + up * .54f, Vector3.one * .06f, rng, .2f); ArtistBits.Label(z.labels, "Shabba!", p + up * 2.4f, z.front, .2f, Red, TextAnchor.MiddleCenter); break;
                case "Shaggy":
                    s = Song(z, "It Wasn't Me"); foreach (var e in new[] { -1f, 1f }) z.mk.Prism(woodM, p + z.side * e - up * .2f, up, .05f, 2.4f, 4); z.mk.Box(Paint(Cream), p + up * 1.7f, new Vector3(2.4f, 1.1f, .06f), rot); BoxCol(z.root, p + up * 1.2f, new Vector3(2.4f, 2f, .1f), rot); ArtistBits.Label(z.labels, "IT WASN'T ME", p + up * 1.86f + z.front * .04f, z.front, .28f, Red, TextAnchor.MiddleCenter); ArtistBits.Label(z.labels, "(saw you on camera)  it wasn't me", p + up * 1.4f + z.front * .04f, z.front, .09f, Ink, TextAnchor.MiddleCenter); z.mk.Box(SongGlow(s, Gold, 1f), p + up * 1.2f + z.front * .04f, new Vector3(2f, .03f, .01f), rot);
                    string[] alibi = { "\"It wasn't me.\" Nobody asked. You said it anyway. Now they're asking.", "You practise your innocent face in a puddle. It needs work.", "Caught red-handed. Say it with confidence: wasn't me." }; var an = 0; Spot("The alibi board", p + up * 1.4f + z.front * .5f, "Deny everything", new Vector3(2.2f, 1.8f, 1f)).onUse = _ => Say1(p + up * 2.8f, alibi[an++ % alibi.Length]); break;
                case "Baha Men":
                    s = Song(z, "Who Let The Dogs Out"); foreach (var e in new[] { -1.2f, 1.2f }) z.mk.Prism(woodM, p + z.side * e - z.front * 1.4f - up * .2f, up, .05f, 1.6f, 4); z.mk.Box(woodM, p - z.front * 1.4f + up * 1.2f - z.side * 1.8f, new Vector3(1.2f, .06f, .06f), rot); z.mk.Box(plank, p - z.front * 1.1f + up * .7f + z.side * .6f, new Vector3(1.1f, 1f, .05f), rot * Quaternion.Euler(0, 70f, 0)); ArtistBits.Label(z.labels, "who? who? who?", p - z.front * 1.4f + up * 1.8f, z.front, .16f, Ink, TextAnchor.MiddleCenter);
                    for (var k = 0; k < 3; k++) { var dm = new MeshKit(); var fur = Paint(new[] { new Color(.6f, .42f, .26f), new Color(.2f, .18f, .18f), new Color(.9f, .86f, .78f) }[k]); dm.Blob(fur, new Vector3(0, .3f, 0), new Vector3(.14f, .14f, .3f), rng, .04f); dm.Blob(fur, new Vector3(0, .46f, .32f), new Vector3(.1f, .1f, .13f), rng, 0f); foreach (var e in new[] { -.08f, .08f }) { dm.Box(fur, new Vector3(e, .56f, .3f), new Vector3(.04f, .1f, .03f), Quaternion.identity); foreach (var q in new[] { -.2f, .2f }) dm.Box(fur, new Vector3(e, .1f, q), new Vector3(.04f, .2f, .04f), Quaternion.identity); } dm.Prism(SongGlow(s, Gold, .6f), new Vector3(0, .4f, -.28f), new Vector3(0, .8f, -.5f).normalized, .02f, .2f, 3);
                        var dgo = dm.BuildSingle("Dog", tlife.transform, false); var dp = z.At(3f + k * 1.2f, -2f + k * 1.6f) + up * .1f; dgo.transform.SetPositionAndRotation(dp, rot * Quaternion.Euler(0, 140f + k * 50f, 0)); var n = k; string[] woof = { "Woof. Woof, woof-woof. It's been asking the same question for twenty years.", "The tail goes round like a propeller. It did it. It let itself out.", "It rolls over in a puddle and is delighted about everything." }; Interact.Toy(dgo, Toy.Kind.Custom, "Pat the dog", new Vector3(.6f, .8f, .9f)).onUse = _ => Say1(dp + up * 1.4f, woof[n++ % woof.Length]); } break;
                case "Major Lazer":
                    s = Song(z, "Powerful"); SpeakerStack(z.root, z.mk, z.solid, p - z.front * .6f, rot, 2, 3, 2); var bolt = SongGlow(s, new Color(.4f, .9f, 1f), 2.2f); var b0 = p + up * 6.4f; foreach (var (dx, dy, len, ang) in new[] { (0f, 0f, 1.2f, 20f), (.2f, -1f, 1.2f, -30f), (0f, -2f, 1.2f, 20f) }) z.mk.Box(bolt, b0 + z.side * dx + up * dy + z.front * .2f, new Vector3(.2f, len, .06f), rot * Quaternion.Euler(0, 0, ang)); halos.Add(b0 - up * 1f + z.front * .5f, 2f, new Color(.4f, .9f, 1f), .4f); break;
                default:
                    z.mk.Prism(woodM, p, up, .04f, 1.3f, 5); z.mk.Blob(GlowM(Gold, 1.2f), p + up * 1.42f, Vector3.one * .16f, rng, .1f); break;
            }
        }
    }
}
