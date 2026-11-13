using System.Collections.Generic;
using UnityEngine;
using static EverythingWorld.World.PlaceTools;

namespace EverythingWorld.World
{
    /// The Hip-Hop Boroughs' artists. Most have a corner of their own: a piece of wall with a mural panel, a door
    /// and a stoop to sit on, a hydrant (the British ones get a red phone box), and one thing from their song;
    /// covers on a cream card on a concrete board with a gold rail.
    /// - Kendrick Lamar: a round stage ringed with light bars under a banner ("Not Like Us": the cones pound and
    ///   confetti falls); a long white table with one gold seat in the middle ("HUMBLE.": the colour drains); a very
    ///   tall street lamp with a gold figure standing on top of it ("Alright": the light turns gold).
    /// - Logic: a wall with forty-four gold tally bars; a studio desk with a puzzle cube to carry; one enormous
    ///   cigarette smoking in a dish ("Nikki"); a chain between two posts, broken in the middle ("Slave II").
    /// - Eminem: a battle stage under one white light, a mic to grab, a clock stopped at one shot, a hoodie on a
    ///   hook, a plate of spaghetti to carry ("Lose Yourself": the light drops and pulses like a heartbeat).
    /// - Kanye West: a white piano with one key lit and a line of dancers in black ("Runaway"); a heap of gold with
    ///   a spade in it ("Gold Digger").
    /// - Kid Cudi: the sun and the moon on either end of one beam; a bench facing out over the edge; a sign pointing
    ///   to happiness (his songs bring the night down).
    /// - And thirty-eight more, one thing each: see Others.
    public partial class HipKit
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
                Add("Kendrick Lamar", "Not Like Us", "@the stage and the tower"); Add("Kendrick Lamar", "HUMBLE", "@the block"); Add("Eminem", "Lose Yourself", "@the battle stage");
                Add("Kid Cudi", "Day 'N' Nite", "@up victory avenue"); Add("Public Enemy", "Fight The Power", "@the block"); Add("Travis Scott", "SICKO MODE", "@up victory avenue");
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
            if (asphalt == null) MakeMaterials();
            var root = new GameObject("Artist: " + a.name).transform; root.SetParent(ctx.Root, false);
            var front = facing.TryGetValue(a.name, out var f) ? f : new Vector3(center.x - a.x, 0, center.z - a.z).normalized;
            if (front.sqrMagnitude < .01f) front = Vector3.forward;
            var labels = new GameObject("Labels").transform; labels.SetParent(root, false);
            var area = root.gameObject.AddComponent<ArtistArea>();
            area.artistName = a.name; area.stationId = a.stationId; area.radius = a.r; area.glowColor = Gold;
            var z = new Plot { ctx = ctx, root = root, labels = labels, mk = new MeshKit(), solid = new MeshKit(), pictures = new MeshKit(), a = a, c = ctx.OnGround(a.x, a.z), front = front, side = Vector3.Cross(Vector3.up, front), area = area };
            var reach = Foot(a); var up = Vector3.up;
            z.mk.Prism(concrete, z.c - up * .25f, up, reach * .66f, .33f, 24);
            switch (a.name)
            {
                case "Kendrick Lamar": Kendrick(z); break;
                case "Logic": Logic(z); break;
                case "Eminem": Eminem(z); break;
                case "Kanye West": Kanye(z); break;
                case "Kid Cudi": Cudi(z); break;
                default: Others(z, Stoop(z)); break;
            }
            SongLamps(z);
            Board(z, z.At(reach * .6f, -reach * .45f - 1.5f), front);
            ArtistBits.Plaque(z.mk, z.solid, labels, a, z.At(reach * .6f + 1.5f, reach * .45f + 1.5f), front, concreteDark, goldM, Ink, 1.6f);
            StreetLamp(ctx, z.mk, z.At(reach * .5f, reach * .68f), -z.side);
            if (z.area.glowLight == null) z.area.glowLight = ArtistBits.PointLight(root, z.c + up * 4f + front * 3f, new Color(1f, .84f, .56f), HasStation(a) ? 24f : 15f, 1.1f);
            area.glowRenderers = new Renderer[0];
            z.mk.Build("Area", root, false);
            z.solid.BuildCollider("Walls", root);
            z.pictures.Build("Covers", root, false);
            noGrass.Add(new Vector3(z.c.x, reach * .9f, z.c.z));
            var mark = new GameObject("Shot: " + a.name + " up close").transform; mark.SetParent(ctx.Root, false);
            var cam = z.At(reach * .85f, reach * .3f) + up * 2f; var look = z.At(-reach * .3f, 0) + up * 2.2f - cam; mark.SetPositionAndRotation(cam, Quaternion.LookRotation(look.normalized));
        }

        /// The covers on a cream card on a concrete board with a gold rail.
        void Board(Plot z, Vector3 ground, Vector3 dir)
        {
            dir = Vector3.ProjectOnPlane(dir, Vector3.up).normalized; var rot = Quaternion.LookRotation(dir);
            var n = Mathf.Max(1, Mathf.Min(7, z.a.songList?.Length ?? 1)); const float size = .9f; var w = n * size * 1.25f + .6f;
            var c = ground + Vector3.up * 1.7f;
            z.mk.Box(concreteDark, c - Vector3.up * .5f - dir * .06f, new Vector3(w + .5f, size + 2f, .2f), rot); z.mk.Box(goldM, c + Vector3.up * (size / 2f + .56f) - dir * .06f, new Vector3(w + .7f, .12f, .3f), rot);
            z.solid.Box(concreteDark, c - Vector3.up * .5f, new Vector3(w + .5f, size + 2f, .3f), rot); z.mk.Box(Paint(Cream), c - Vector3.up * .1f + dir * .07f, new Vector3(w, size + .9f, .04f), rot);
            ArtistBits.CoverWall(z.pictures, z.labels, z.ctx, z.a, c + dir * .1f + Vector3.up * .08f, dir, n, size, n, true, Ink);
        }

        /// A little gold speaker on a post for each song without its own piece.
        void SongLamps(Plot z)
        {
            if (!SongIds.TryGetValue(z.a.name, out var list)) return;
            var up = Vector3.up; var k = 0; var seen = new HashSet<string>();
            foreach (var (_, id) in list)
            {
                if (z.lit.Contains(id) || !seen.Add(id)) continue;
                var p = z.At(Foot(z.a) * .55f - (k % 2) * 1.3f, Foot(z.a) * .2f + (k / 2) * 1.3f);
                z.mk.Prism(iron, p - up * .1f, up, .035f, 1.2f, 5); z.mk.Box(dark, p + up * 1.26f, new Vector3(.24f, .3f, .2f), z.Rot); z.mk.Prism(SongGlow(id, Gold, 1.6f), p + up * 1.26f + z.front * .1f, z.front, .08f, .02f, 10); ArtistBits.PostCollider(z.root, p, 1.3f, .08f);
                k++;
            }
        }

        /// A corner of their own: a wall with a mural panel, a door and a stoop to sit on; a hydrant, or a red phone box.
        PlaceFrame Stoop(Plot z)
        {
            var up = Vector3.up; var hue = z.a.name.Length + z.a.name[0]; var uk = Uk(z.a.name); var f = new PlaceFrame(z.At(-Foot(z.a) * .5f, 0), z.front); var wall = uk ? Paint(new Color(.74f, .64f, .42f)) : Paint(Walls[hue % Walls.Length]);
            z.mk.Box(wall, f.P(0, 1.6f, 0), new Vector3(5.8f, 4f, .5f), f.r); z.solid.Box(wall, f.P(0, 1.6f, 0), new Vector3(5.8f, 4f, .5f), f.r); z.mk.Box(concreteDark, f.P(0, 3.7f, 0), new Vector3(6.1f, .2f, .7f), f.r);
            var tag = Tags[hue % Tags.Length]; z.mk.Box(Paint(tag), f.P(1.1f, 1.9f, .27f), new Vector3(3f, 2.4f, .03f), f.r); z.mk.Box(goldM, f.P(1.1f, .8f, .3f), new Vector3(3f, .16f, .02f), f.r); z.mk.Box(Paint(Tags[(hue + 3) % Tags.Length]), f.P(1.1f, 2.5f, .3f), new Vector3(3f, .5f, .02f), f.r * Quaternion.Euler(0, 0, 0));
            z.mk.Box(woodM, f.P(-1.8f, 1.5f, .27f), new Vector3(1.1f, 2.2f, .04f), f.r); z.mk.Box(windowM, f.P(-1.8f, 2.84f, .27f), new Vector3(1.1f, .36f, .04f), f.r);
            for (var k = 0; k < 2; k++) { var sp = f.P(-1.8f, .1f + k * .2f, .5f + (1 - k) * .4f + .2f); z.mk.Box(concrete, sp, new Vector3(1.7f, .2f + k * .4f - k * .2f, .4f), f.r); z.solid.Box(concrete, sp - up * .2f, new Vector3(1.7f, .6f, .4f), f.r); }
            Interact.Seat(tlife.transform, f.P(-1.8f, .46f, .7f), f.Fwd, "Sit on the stoop").seatHeight = .36f;
            if (uk) { var pb = f.P(2.3f, 0, 1.2f); z.mk.Box(redM, pb + up * 1.2f, new Vector3(.9f, 2.4f, .9f), f.r); z.mk.Box(redM, pb + up * 2.46f, new Vector3(1f, .14f, 1f), f.r); for (var s = 0; s < 4; s++) { var r = f.r * Quaternion.Euler(0, s * 90f, 0); z.mk.Box(darkGlass, pb + up * 1.3f + r * Vector3.forward * .46f, new Vector3(.62f, 1.6f, .02f), r); z.mk.Box(Paint(Cream), pb + up * 2.26f + r * Vector3.forward * .46f, new Vector3(.6f, .14f, .02f), r); } BoxCol(z.root, pb + up * 1.2f, new Vector3(.9f, 2.4f, .9f), f.r); }
            else Hydrant(z.ctx, z.mk, f.P(2.4f, 0, 1.2f), hue % 5 == 0);
            return f;
        }

        // ---------------------------------------------------------------- Kendrick Lamar
        void Kendrick(Plot z)
        {
            var up = Vector3.up; var rot = z.Rot; var rng = new System.Random(2017); var al = Song(z, "Alright"); var hu = Song(z, "HUMBLE"); var nl = Song(z, "Not Like Us"); life.alrightSong = al; life.humbleSong = hu; life.notLikeUsSong = nl;
            // the round stage, ringed with light bars, under the banner
            var st = z.At(-4.4f, 0) + up * .08f; z.mk.Prism(dark, st, up, 3.6f, .5f, 26); z.solid.Prism(dark, st - up * .3f, up, 3.6f, .8f, 14); z.mk.Prism(goldM, st + up * .5f, up, 3.6f, .012f, 26); z.mk.Prism(dark, st + up * .53f, up, 3.3f, .012f, 26);
            var bar = SongGlow(nl, new Color(1f, .9f, .7f), 1.6f); for (var k = 0; k < 11; k++) { var d = Quaternion.Euler(0, 180f - 75f + k * 15f, 0) * z.front; var bp = st + d * 4.4f; z.mk.Box(iron, bp + up * 1.9f, new Vector3(.14f, 3.8f, .14f), Quaternion.LookRotation(-d)); z.mk.Box(bar, bp + up * 2f - d * .09f, new Vector3(.1f, 3.2f, .04f), Quaternion.LookRotation(-d)); ArtistBits.PostCollider(z.root, bp, 3.6f, .1f); }
            var bn = st - z.front * 5f; foreach (var e in new[] { -4f, 4f }) { z.mk.Prism(iron, bn + z.side * e - up * .2f, up, .1f, 7.6f, 5); ArtistBits.PostCollider(z.root, bn + z.side * e, 7f, .12f); } z.mk.Box(Paint(Purple), bn + up * 6f, new Vector3(8f, 2.4f, .1f), rot); z.mk.Box(goldM, bn + up * 4.86f + z.front * .02f, new Vector3(8f, .14f, .12f), rot); ArtistBits.Label(z.labels, "THEY NOT LIKE US", bn + up * 6f + z.front * .07f, z.front, .62f, Gold, TextAnchor.MiddleCenter);
            var mc = st + up * .54f + z.front * 1f; z.mk.Prism(iron, mc, up, .02f, 1.5f, 4); z.mk.Prism(iron, mc, up, .16f, .02f, 8); z.mk.Blob(goldM, mc + up * 1.56f, new Vector3(.05f, .08f, .05f), rng, 0f);
            z.area.glowLight = ArtistBits.PointLight(z.root, st + up * 4f + z.front * 2f, new Color(1f, .86f, .6f), 26f, 1.6f); halos.Add(st + up * 2.4f, 4f, Gold, .12f);
            Folk(st + up * .54f - z.side * 1.6f, z.front, Resident.Pose.Wave, new[] { "Pop out and show 'em! That's the whole plan!", "Everybody on this mesa knows every word. Even the hydrants.", "He said we gon' be alright. Look around. He was right." });
            // "HUMBLE.": a long white table, one gold seat in the middle
            var tb = new PlaceFrame(z.At(4f, -6.4f) + up * .08f, (z.front + z.side * .5f).normalized); z.mk.Box(white, tb.P(0, .76f, 0), new Vector3(5.6f, .08f, 1f), tb.r); z.mk.Box(white, tb.P(0, .4f, .48f), new Vector3(5.6f, .72f, .03f), tb.r); foreach (var e in new[] { -2.6f, 2.6f }) z.mk.Box(white, tb.P(e, .38f, 0), new Vector3(.1f, .76f, .9f), tb.r); BoxCol(z.root, tb.P(0, .4f, 0), new Vector3(5.6f, .84f, 1f), tb.r);
            var seat = SongGlow(hu, Gold, 1.2f); for (var k = 0; k < 7; k++) { var sp = tb.P(-2.25f + k * .75f, 0, -.8f); z.mk.Prism(k == 3 ? seat : woodM, sp + up * .42f, up, .2f, .06f, 8); z.mk.Prism(iron, sp, up, .03f, .42f, 4); Interact.Seat(tlife.transform, sp + up * .5f, tb.Fwd, k == 3 ? "Sit down" : "Sit").seatHeight = .46f; }
            for (var k = 0; k < 16; k++) { var a = Quaternion.Euler(0, k * 22.5f, 0); z.mk.Box(seat, tb.P(0, 2.1f, -.8f) + a * Vector3.forward * .34f, new Vector3(.14f, .03f, .03f), a); } ArtistBits.Label(z.labels, "sit down.", tb.P(0, .42f, .5f), tb.Fwd, .16f, Ink, TextAnchor.MiddleCenter);
            var lf = new MeshKit(); lf.Blob(Paint(new Color(.8f, .58f, .3f)), new Vector3(0, .07f, 0), new Vector3(.18f, .07f, .09f), rng, .1f); Thing(lf, "Loaf", "a loaf of bread", tb.P(-1.2f, .8f, .1f), tb.r, new Vector3(.36f, .14f, .2f), .3f, false);
            // "Alright": a street lamp twice the height of the others, and somebody standing on top of it
            var lp = z.At(4.4f, 6.6f); z.mk.Prism(iron, lp - up * .2f, up, .11f, 9.2f, 6, .7f); z.mk.Prism(iron, lp + up * 9f, z.front, .06f, 1.4f, 4); z.mk.Box(lampM, lp + up * 8.9f + z.front * 1.4f, new Vector3(.3f, .08f, .6f), rot); ArtistBits.PostCollider(z.root, lp, 9f, .13f);
            var fig = SongGlow(al, Gold, 1.6f); var ft = lp + up * 9.1f; foreach (var e in new[] { -1f, 1f }) { z.mk.Box(fig, ft + up * .4f + z.side * e * .1f, new Vector3(.13f, .8f, .13f), rot); z.mk.Box(fig, ft + up * 1.32f + z.side * e * .46f, new Vector3(.62f, .11f, .11f), rot * Quaternion.Euler(0, 0, e * 24f)); } z.mk.Box(fig, ft + up * 1.14f, new Vector3(.36f, .7f, .2f), rot); z.mk.Blob(fig, ft + up * 1.66f, Vector3.one * .15f, rng, 0f); halos.Add(ft + up * 1.2f, 2.6f, Gold, .4f);
            Shot(z.ctx, "the pop out", z.At(12f, 4f) + up * 2.4f, st + up * 3.4f);
        }

        // ---------------------------------------------------------------- Logic
        void Logic(Plot z)
        {
            var up = Vector3.up; var rot = z.Rot; var rng = new System.Random(44); var bars = Song(z, "44 Bars"); var nk = Song(z, "Nikki"); var sl = Song(z, "Slave II");
            // forty-four bars, tallied in gold on a concrete wall
            var w = new PlaceFrame(z.At(-5.2f, 0), z.front); z.mk.Box(concrete, w.P(0, 1.8f, 0), new Vector3(7.6f, 4.2f, .5f), w.r); z.solid.Box(concrete, w.P(0, 1.8f, 0), new Vector3(7.6f, 4.2f, .5f), w.r); z.mk.Box(concreteDark, w.P(0, 4f, 0), new Vector3(7.9f, .2f, .7f), w.r);
            var tally = SongGlow(bars, Gold, 1.3f); for (var row = 0; row < 4; row++) for (var k = 0; k < 11; k++) { var five = k % 5 == 4; var at = w.P(-3f + k * .6f - (five ? 1.2f : 0f), 3.2f - row * .8f, .27f + (five ? .035f : 0f)); z.mk.Box(tally, at, new Vector3(five ? 2.4f : .09f, five ? .08f : .56f, .02f), w.r * Quaternion.Euler(0, 0, five ? 16f : (k % 2) * 4f - 2f)); }
            ArtistBits.Label(z.labels, "44", w.P(3f, .7f, .27f), w.Fwd, .5f, Ink, TextAnchor.MiddleCenter); z.area.glowLight = ArtistBits.PointLight(z.root, w.P(0, 3f, 3f), new Color(1f, .86f, .6f), 22f, 1.4f);
            // the desk: a notebook, a microphone, a puzzle cube to carry
            var dk = new PlaceFrame(z.At(-1f, -3.4f) + up * .08f, (z.front + z.side * .4f).normalized); z.mk.Box(woodM, dk.P(0, .74f, 0), new Vector3(1.8f, .06f, .8f), dk.r); foreach (var (x, zz) in new[] { (-.8f, -.3f), (.8f, -.3f), (.8f, .3f), (-.8f, .3f) }) z.mk.Box(iron, dk.P(x, .36f, zz), new Vector3(.05f, .72f, .05f), dk.r); BoxCol(z.root, dk.P(0, .4f, 0), new Vector3(1.8f, .8f, .8f), dk.r);
            z.mk.Box(white, dk.P(-.4f, .78f, .05f), new Vector3(.4f, .02f, .3f), dk.r * Quaternion.Euler(0, 12f, 0)); z.mk.Prism(iron, dk.P(.6f, .77f, -.1f), up, .015f, .5f, 4); z.mk.Blob(goldM, dk.P(.6f, 1.3f, -.06f), new Vector3(.05f, .08f, .05f), rng, 0f);
            var cube = new MeshKit(); var cc = new[] { Red, Gold, Sky, new Color(.3f, .8f, .4f), Cream, new Color(.95f, .5f, .15f) }; for (var x = 0; x < 3; x++) for (var y = 0; y < 3; y++) for (var q = 0; q < 3; q++) cube.Box(Paint(cc[(x * 2 + y + q * 3) % 6]), new Vector3((x - 1) * .06f, .03f + y * .06f, (q - 1) * .06f), Vector3.one * .055f, Quaternion.identity); Thing(cube, "Puzzle cube", "a puzzle cube, nearly solved", dk.P(.1f, .78f, .1f), dk.r * Quaternion.Euler(0, 30f, 0), new Vector3(.2f, .2f, .2f), .2f, false);
            Chair0(z.mk, dk.P(0, 0, .8f), -dk.Fwd);
            // "Nikki": one enormous cigarette smoking in a dish
            var ng = z.At(4.2f, -5.4f) + up * .08f; z.mk.Prism(concreteDark, ng, up, 1f, .16f, 14); var cd = (z.side + z.front * .4f + up * .16f).normalized; z.mk.Prism(white, ng + up * .3f - cd * .9f, cd, .12f, 1.5f, 10); z.mk.Prism(Paint(new Color(.8f, .56f, .3f)), ng + up * .3f - cd * 1.5f, cd, .122f, .6f, 10); z.mk.Prism(SongGlow(nk, new Color(1f, .4f, .1f), 1.8f), ng + up * .3f + cd * .6f, cd, .11f, .1f, 10); life.smokes.Add(ng + up * .5f + cd * .7f); BoxCol(z.root, ng + up * .3f, new Vector3(2f, .6f, 2f), rot);
            // "Slave II": a chain between two posts, broken in the middle
            var ca = z.At(4.8f, 3.6f); var cb = z.At(2.8f, 7f); foreach (var g in new[] { ca, cb }) { z.mk.Prism(iron, g - up * .2f, up, .09f, 2.2f, 6); ArtistBits.PostCollider(z.root, g, 2f, .11f); } var brk = SongGlow(sl, Gold, 1.8f);
            for (var k = 0; k < 12; k++) { if (k == 5 || k == 6) continue; var t = (k + .5f) / 12f; var sag = Mathf.Sin(t * Mathf.PI) * (k < 5 ? 1.5f * t * 2f : 1.5f * (1f - t) * 2f); var p = Vector3.Lerp(ca, cb, t) + up * (1.9f - sag); z.mk.Prism(k == 4 || k == 7 ? brk : iron, p - z.front * .05f, z.front, .11f, .06f, 8); z.mk.Prism(Paint(Concrete), p - z.front * .06f, z.front, .06f, .08f, 6); } halos.Add((ca + cb) / 2f + up * .7f, 1.2f, Gold, .3f);
            Shot(z.ctx, "forty-four bars", z.At(10f, 3f) + up * 2.2f, w.P(0, 2f, 0));
        }

        /// A plain chair to sit on.
        void Chair0(MeshKit mk, Vector3 g, Vector3 face)
        {
            var up = Vector3.up; var rot = Quaternion.LookRotation(face); mk.Box(woodM, g + up * .45f, new Vector3(.46f, .05f, .46f), rot); mk.Box(woodM, g + up * .8f - face * .21f, new Vector3(.46f, .7f, .05f), rot); foreach (var (x, zz) in new[] { (-.2f, -.2f), (.2f, -.2f), (.2f, .2f), (-.2f, .2f) }) mk.Box(woodM, g + up * .22f + rot * new Vector3(x, 0, zz), new Vector3(.04f, .44f, .04f), rot);
            Interact.Seat(tlife.transform, g + up * .5f, face);
        }

        // ---------------------------------------------------------------- Eminem
        void Eminem(Plot z)
        {
            var up = Vector3.up; var rot = z.Rot; var rng = new System.Random(8); var ls = Song(z, "Lose Yourself"); if (SongIds.TryGetValue(z.a.name, out var all)) foreach (var (_, id) in all) { life.lose.Add(id); z.lit.Add(id); }
            // the battle stage: a brick wall, a low deck, one white light, one microphone
            var w = new PlaceFrame(z.At(-4.6f, 0), z.front); z.mk.Box(brickM, w.P(0, 1.8f, 0), new Vector3(6.4f, 4.2f, .5f), w.r); z.solid.Box(brickM, w.P(0, 1.8f, 0), new Vector3(6.4f, 4.2f, .5f), w.r); for (var k = 0; k < 4; k++) z.mk.Box(Paint(Tags[(k * 2 + 1) % Tags.Length]), w.P(-2f + k * 1.3f, 1.3f + (k % 2) * .5f, .27f + k * .035f), new Vector3(1.6f, 1.1f, .02f), w.r * Quaternion.Euler(0, 0, -14f + k * 9f));
            z.mk.Box(dark, w.P(0, .25f, 1.9f), new Vector3(4.6f, .34f, 3f), w.r); z.solid.Box(dark, w.P(0, .1f, 1.9f), new Vector3(4.6f, .64f, 3f), w.r);
            var white1 = SongGlow(ls, new Color(1f, 1f, .95f), 2f); z.mk.Prism(iron, w.P(-2.6f, 0, .5f), up, .06f, 5f, 5); z.mk.Prism(iron, w.P(-2.6f, 4.9f, .5f), w.Right, .04f, 2.6f, 4); z.mk.Prism(iron, w.P(0, 4.9f, .5f), (-up + w.Fwd * .4f).normalized, .2f, .5f, 8, 1.6f); z.mk.Prism(white1, w.P(0, 4.42f, .7f), (-up + w.Fwd * .4f).normalized, .3f, .03f, 10); z.mk.Prism(SongGlow(ls, new Color(1f, 1f, .9f), .5f), w.P(0, .43f, 2.2f), up, 1f, .012f, 20);
            var sp = ArtistBits.PointLight(z.root, w.P(0, 3.4f, 2f), new Color(1f, .98f, .9f), 12f, 2f); z.area.glowLight = sp; halos.Add(w.P(0, 2f, 2.2f), 2f, Color.white, .14f);
            var mc = w.P(0, .42f, 2.2f); z.mk.Prism(iron, mc, up, .02f, 1.5f, 4); z.mk.Prism(iron, mc, up, .16f, .02f, 8); z.mk.Blob(white, mc + up * 1.56f, new Vector3(.05f, .08f, .05f), rng, 0f);
            string[] shot = { "Palms: sweaty. Knees: weak. You open your mouth anyway, and this time the words come.", "One shot. One opportunity. You take it. The wall behind you shakes.", "The crowd that isn't there goes quiet. Then it doesn't.", "You look down at the mic, then up. Go." }; var sn = 0;
            Spot("The battle mic", mc + up * 1.4f, "Take your one shot", new Vector3(1f, 1f, 1f)).onUse = _ => { life.played = Time.time; Say1(mc + up * 2.6f, shot[sn++ % shot.Length]); };
            // a clock stopped at the moment; a hoodie on a hook; a plate of spaghetti to carry
            var ck = w.P(2.2f, 3.1f, .27f); z.mk.Prism(white, ck, w.Fwd, .5f, .04f, 16); z.mk.Box(dark, ck + w.Fwd * .05f + up * .18f, new Vector3(.04f, .36f, .01f), w.r); z.mk.Box(redM, ck + w.Fwd * .06f + w.Right * .1f + up * .1f, new Vector3(.03f, .3f, .01f), w.r * Quaternion.Euler(0, 0, -44f)); ArtistBits.Label(z.labels, "ONE SHOT", ck - up * .76f + w.Fwd * .02f, w.Fwd, .16f, Cream, TextAnchor.MiddleCenter);
            var hk = z.At(3f, -5.6f); z.mk.Prism(iron, hk - up * .2f, up, .05f, 2.2f, 5); z.mk.Prism(iron, hk + up * 1.9f, z.front, .03f, .3f, 4); z.mk.Box(Paint(new Color(.4f, .4f, .44f)), hk + up * 1.4f + z.front * .3f, new Vector3(.6f, .8f, .16f), rot); z.mk.Blob(Paint(new Color(.36f, .36f, .4f)), hk + up * 1.9f + z.front * .3f, new Vector3(.2f, .2f, .16f), rng, 0f); ArtistBits.PostCollider(z.root, hk, 2f, .08f);
            var tp = z.At(3.4f, 5.4f) + up * .08f; z.mk.Prism(woodM, tp + up * .7f, up, .5f, .05f, 10); z.mk.Prism(iron, tp, up, .05f, .7f, 5); BoxCol(z.root, tp + up * .4f, new Vector3(.9f, .8f, .9f), rot);
            var sg = new MeshKit(); sg.Prism(white, Vector3.zero, Vector3.up, .16f, .02f, 12); sg.Blob(Paint(new Color(.95f, .82f, .5f)), new Vector3(0, .05f, 0), new Vector3(.11f, .04f, .11f), rng, .3f); sg.Blob(redM, new Vector3(0, .08f, 0), new Vector3(.06f, .02f, .06f), rng, .3f); Thing(sg, "Spaghetti", "a plate of spaghetti. Somebody's mom's", tp + up * .76f, Quaternion.identity, new Vector3(.34f, .12f, .34f), .3f, false);
            Shot(z.ctx, "the battle stage", z.At(9f, 3f) + up * 2.2f, w.P(0, 2.2f, 1f));
        }

        // ---------------------------------------------------------------- Kanye West
        void Kanye(Plot z)
        {
            var up = Vector3.up; var rot = z.Rot; var rng = new System.Random(2010); var ra = Song(z, "Runaway"); var gd = Song(z, "Gold Digger"); life.runawaySong = ra;
            // "Runaway": a white floor, a white piano, one key lit, a line of dancers in black
            var fl = new PlaceFrame(z.At(-2.6f, 0) + up * .09f, z.front); z.mk.Box(white, fl.P(0, 0, 0), new Vector3(9f, .02f, 6f), fl.r);
            var pn = fl.P(-2.6f, 0, .6f); var pr = fl.r * Quaternion.Euler(0, 40f, 0); z.mk.Box(white, pn + up * .86f, new Vector3(1.6f, .3f, 2.2f), pr); foreach (var (x, zz) in new[] { (-.7f, .9f), (.7f, .9f), (0f, -.9f) }) z.mk.Box(white, pn + up * .36f + pr * new Vector3(x, 0, zz), new Vector3(.1f, .72f, .1f), pr); z.mk.Box(white, pn + up * 1.5f + pr * new Vector3(-.5f, 0, -.1f), new Vector3(.06f, 1f, 1.9f), pr * Quaternion.Euler(0, 0, -26f)); z.mk.Box(Paint(Cream), pn + up * .8f + pr * new Vector3(0, 0, 1.2f), new Vector3(1.5f, .06f, .26f), pr); z.mk.Box(dark, pn + up * .84f + pr * new Vector3(0, 0, 1.16f), new Vector3(1.4f, .02f, .12f), pr);
            z.mk.Box(SongGlow(ra, new Color(1f, 1f, 1f), 2f), pn + up * .85f + pr * new Vector3(.3f, 0, 1.24f), new Vector3(.06f, .04f, .2f), pr); BoxCol(z.root, pn + up * .6f, new Vector3(1.6f, 1.2f, 2.5f), pr); halos.Add(pn + up * 1f + pr * new Vector3(.3f, 0, 1.3f), .7f, Color.white, .5f);
            string[] note = { "One note. Then the same note. Then the same note again. It's enough. It's devastating.", "Plink. Plink. Plink. Fifteen times, and the dancers rise onto their toes.", "You hold the last one. A toast, then. To all of us." }; var nn = 0;
            Spot("The white piano", pn + up * 1.3f + pr * Vector3.forward * 1.5f, "Play the one note", new Vector3(1.4f, 1f, 1f)).onUse = _ => { life.played = Time.time; Say1(pn + up * 2.6f, note[nn++ % note.Length]); };
            for (var k = 0; k < 6; k++) { var bp = fl.P(-.6f + k * .9f, .02f, -1.6f + (k % 2) * .5f); z.mk.Prism(Paint(new Color(.9f, .8f, .72f)), bp, up, .05f, .8f, 5); z.mk.Prism(dark, bp + up * .76f, up, .34f, .04f, 12); z.mk.Prism(dark, bp + up * .8f, up, .1f, .5f, 6, .8f); z.mk.Blob(Paint(new Color(.5f, .34f, .26f)), bp + up * 1.46f, Vector3.one * .1f, rng, 0f); foreach (var e in new[] { -1f, 1f }) z.mk.Box(Paint(new Color(.5f, .34f, .26f)), bp + up * 1.5f + fl.Right * e * .2f, new Vector3(.05f, .5f, .05f), fl.r * Quaternion.Euler(0, 0, e * -22f)); ArtistBits.PostCollider(z.root, bp, 1.5f, .16f); }
            z.area.glowLight = ArtistBits.PointLight(z.root, fl.P(0, 4f, 1f), new Color(1f, .96f, .9f), 22f, 1.5f);
            // "Gold Digger": a heap of gold with a spade stuck in it
            var gp = z.At(3.6f, 5.6f) + up * .08f; var gm = SongGlow(gd, Gold, 1.1f); for (var k = 0; k < 16; k++) z.mk.Blob(gm, gp + Quaternion.Euler(0, k * 67f, 0) * Vector3.forward * (.2f + (k % 4) * .24f) + up * (.1f + (3 - k % 4) * .16f), Vector3.one * (.2f + (k % 3) * .06f), rng, .3f); z.mk.Prism(woodM, gp + up * .5f, (up + z.side * .3f).normalized, .03f, 1.3f, 5); z.mk.Box(iron, gp + up * .44f - z.side * .02f, new Vector3(.26f, .36f, .03f), rot * Quaternion.Euler(0, 0, -16f)); BoxCol(z.root, gp + up * .4f, new Vector3(1.8f, .8f, 1.8f), rot); halos.Add(gp + up * .6f, 1.6f, Gold, .3f);
            Shot(z.ctx, "the white piano", z.At(9f, 3f) + up * 2.2f, fl.P(0, 1.2f, 0));
        }

        // ---------------------------------------------------------------- Kid Cudi
        void Cudi(Plot z)
        {
            var up = Vector3.up; var rot = z.Rot; var rng = new System.Random(2009); var dn = Song(z, "Day 'N' Nite"); var ph = Song(z, "Pursuit"); if (SongIds.TryGetValue(z.a.name, out var all)) foreach (var (_, id) in all) life.cudi.Add(id);
            // "Day 'N' Nite": the sun and the moon on either end of one beam; the moon is up
            var pv = z.At(-3.6f, 0); z.mk.Prism(iron, pv - up * .2f, up, .14f, 4.6f, 6, .7f); ArtistBits.PostCollider(z.root, pv, 4.2f, .16f); var top = pv + up * 4.4f; var bd = (z.side + up * .42f).normalized; z.mk.Prism(iron, top - bd * 4f, bd, .07f, 8f, 5); z.mk.Blob(iron, top, Vector3.one * .2f, rng, 0f);
            var moon = SongGlow(dn, new Color(.8f, .88f, 1f), 1.6f); var mp = top + bd * 4f; z.mk.Prism(moon, mp - z.front * .08f, z.front, 1.2f, .16f, 20); z.mk.Prism(Paint(new Color(.2f, .22f, .34f)), mp - z.front * .1f + z.side * .5f + up * .2f, z.front, 1f, .2f, 18); halos.Add(mp + z.front * .3f, 3f, new Color(.7f, .8f, 1f), .3f);
            var sun = SongGlow(ph, Gold, 1.3f); var sp = top - bd * 4f; z.mk.Prism(sun, sp - z.front * .08f, z.front, .8f, .16f, 16); for (var k = 0; k < 10; k++) { var a = Quaternion.AngleAxis(k * 36f, z.front); z.mk.Box(sun, sp + a * up * 1.14f, new Vector3(.1f, .36f, .06f), a * rot); }
            z.area.glowLight = ArtistBits.PointLight(z.root, pv + up * 3f + z.front * 3f, new Color(.8f, .86f, 1f), 22f, 1.4f);
            // a bench facing out over the edge, alone
            var bn = z.At(-6.4f, 3f) + up * .08f; Bench(z.root, z.mk, bn, -z.front, 1.8f); z.mk.Prism(iron, bn - z.front * 1f + z.side * 1.4f, up, .04f, 1.1f, 5); z.mk.Blob(moon, bn - z.front * 1f + z.side * 1.4f + up * 1.2f, Vector3.one * .12f, rng, 0f);
            // "Pursuit of Happiness": a sign pointing to it. It's always a bit further on
            var sg = z.At(3.4f, -5.6f); var sr = rot * Quaternion.Euler(0, -30f, 0); z.mk.Prism(iron, sg - up * .2f, up, .06f, 3f, 5); ArtistBits.PostCollider(z.root, sg, 2.8f, .08f); z.mk.Box(SongGlow(ph, Gold, .9f), sg + up * 2.5f + sr * Vector3.right * .7f, new Vector3(2f, .5f, .06f), sr); z.mk.Box(goldM, sg + up * 2.5f + sr * Vector3.right * 1.76f, new Vector3(.36f, .36f, .06f), sr * Quaternion.Euler(0, 0, 45f)); ArtistBits.Label(z.labels, "HAPPINESS", sg + up * 2.56f + sr * new Vector3(.7f, 0, .04f), sr * Vector3.forward, .2f, Ink, TextAnchor.MiddleCenter); ArtistBits.Label(z.labels, "(a bit further)", sg + up * 2.36f + sr * new Vector3(.7f, 0, .04f), sr * Vector3.forward, .09f, Ink, TextAnchor.MiddleCenter);
            Folk(z.At(2.6f, 4.6f) + up * .08f, (z.front - z.side * .3f).normalized, Resident.Pose.Stand, new[] { "When his songs come on, the sun goes down. Only time it does. We look forward to it.", "The lonely stoner seems to free his mind at night. I just come for the stars.", "That bench? Best seat on the mesa. One at a time." });
            Shot(z.ctx, "day and night", z.At(10f, 3f) + up * 2.2f, top);
        }

        // ---------------------------------------------------------------- everyone else: one thing from their song
        void Others(Plot z, PlaceFrame f)
        {
            var up = Vector3.up; var rot = z.Rot; var rng = new System.Random(z.a.name.Length * 7 + z.a.name[0]); var c = z.At(1.4f, 0) + up * .08f; var fr = z.front; var sd = z.side;
            void Plinth(Vector3 at, float w, float h) { z.mk.Box(concrete, at + up * (h / 2f), new Vector3(w, h, w), rot); BoxCol(z.root, at + up * (h / 2f), new Vector3(w, h, w), rot); }
            void Post(Vector3 at, float h, float r = .05f) { z.mk.Prism(iron, at - up * .2f, up, r, h + .2f, 5); ArtistBits.PostCollider(z.root, at, h, r + .03f); }
            switch (z.a.name)
            {
                case "Black Eyed Peas":
                {
                    var s = Song(z, "I Gotta Feeling"); var st = Song(z, "Let's Get It Started"); life.feelingSong = s; Post(c, 3.4f); var mb = SongGlow(s, new Color(1f, 1f, 1f), 1.2f); z.mk.Blob(mb, c + up * 3.8f, Vector3.one * .5f, rng, 0f); for (var k = 0; k < 12; k++) z.mk.Blob(Paint(Tags[k % Tags.Length]), c + up * 3.8f + Quaternion.Euler(k * 30f, k * 67f, 0) * Vector3.forward * .5f, Vector3.one * .08f, rng, 0f); halos.Add(c + up * 3.8f, 2f, Color.white, .3f);
                    z.mk.Box(SongGlow(st, new Color(.3f, .9f, .4f), 1f), c + fr * 2f + up * .012f, new Vector3(3.6f, .02f, .3f), rot); ArtistBits.Label(z.labels, "START", c + fr * 2.5f + up * .03f, up, .3f, Ink, TextAnchor.MiddleCenter);
                    break;
                }
                case "Little Simz":
                {
                    var v = Song(z, "Venom"); var it = Song(z, "Introvert"); var th = c - sd * 1.2f; z.mk.Box(Paint(Purple), th + up * .3f, new Vector3(.9f, .6f, .8f), rot); z.mk.Box(Paint(Purple), th + up * 1.3f - fr * .34f, new Vector3(.9f, 2f, .14f), rot); z.mk.Box(goldM, th + up * 2.36f - fr * .34f, new Vector3(1f, .12f, .18f), rot); foreach (var e in new[] { -1f, 1f }) z.mk.Box(goldM, th + up * .7f + sd * e * .44f, new Vector3(.1f, .3f, .8f), rot); BoxCol(z.root, th + up * .7f, new Vector3(.9f, 1.4f, .8f), rot); Interact.Seat(tlife.transform, th + up * .64f + fr * .1f, fr, "Take the throne").seatHeight = .6f;
                    var sn = SongGlow(v, new Color(.3f, .9f, .3f), 1.2f); for (var k = 0; k < 9; k++) { var a = k * 50f; z.mk.Blob(sn, th + up * (.9f + k * .16f) + sd * .5f + Quaternion.Euler(0, a, 0) * Vector3.forward * .16f, new Vector3(.09f, .07f, .09f), rng, 0f); }
                    var bx = c + sd * 1.7f; z.mk.Box(concreteDark, bx + up * .8f, new Vector3(1.5f, 1.6f, 1.5f), rot); BoxCol(z.root, bx + up * .8f, new Vector3(1.5f, 1.6f, 1.5f), rot); z.mk.Box(SongGlow(it, new Color(1f, .8f, .4f), 1.4f), bx + up * 1f + fr * .76f, new Vector3(.4f, .4f, .02f), rot);
                    break;
                }
                case "Nicki Minaj":
                {
                    var s = Song(z, "Starships"); var an = Song(z, "Anaconda"); life.starSong = s; var pink = Paint(new Color(1f, .45f, .7f)); z.mk.Prism(pink, c + up * .7f, up, .5f, 2.6f, 10); z.mk.Cone(Paint(Cream), c + up * 3.3f, .5f, 1.1f, 10); for (var k = 0; k < 3; k++) { var d = Quaternion.Euler(0, k * 120f, 0) * fr; z.mk.Tri(Paint(Purple), c + d * .5f + up * 1.6f, c + d * 1.2f + up * .1f, c + d * .5f + up * .6f, c + Vector3.Cross(up, d) * 5f); z.mk.Tri(Paint(Purple), c + d * .5f + up * 1.6f, c + d * 1.2f + up * .1f, c + d * .5f + up * .6f, c - Vector3.Cross(up, d) * 5f); }
                    z.mk.Cone(SongGlow(s, new Color(1f, .5f, .8f), 1.8f), c + up * .7f, .4f, -.01f, 8); z.mk.Prism(SongGlow(s, new Color(1f, .5f, .8f), 1.8f), c + up * .1f, up, .2f, .6f, 8, 2f); z.mk.Prism(darkGlass, c + up * 2.5f + fr * .46f, fr, .2f, .06f, 10); ArtistBits.PostCollider(z.root, c, 3.6f, .6f); halos.Add(c + up * .4f, 1.6f, new Color(1f, .5f, .8f), .4f);
                    var sn = SongGlow(an, new Color(.3f, .8f, .3f), .9f); for (var k = 0; k < 14; k++) z.mk.Blob(sn, c + Quaternion.Euler(0, k * 40f, 0) * Vector3.forward * (1.5f - k * .02f) + up * (.12f + (k > 11 ? (k - 11) * .2f : 0f)), new Vector3(.16f, .12f, .16f), rng, 0f);
                    break;
                }
                case "Stormzy":
                {
                    var vb = Song(z, "Vossi Bop"); var su = Song(z, "Shut Up"); Bench(z.root, z.mk, c - sd * 1.4f, fr, 1.8f); var cp = c + sd * 1.5f; Post(cp, 2.4f); var cr = SongGlow(vb, Gold, 1.3f); z.mk.Prism(dark, cp + up * 2.4f, up, .36f, .2f, 10); for (var k = 0; k < 5; k++) z.mk.Cone(cr, cp + up * 2.6f + Quaternion.Euler(0, k * 72f, 0) * Vector3.forward * .3f, .1f, .34f, 4); halos.Add(cp + up * 2.7f, 1.2f, Gold, .4f);
                    var sb = c + fr * 2.4f + sd * .2f; Post(sb, 1.4f, .04f); z.mk.Box(SongGlow(su, Cream, .5f), sb + up * 1.5f, new Vector3(1.3f, .44f, .05f), rot); ArtistBits.Label(z.labels, "SHUT UP", sb + up * 1.5f + fr * .04f, fr, .22f, Ink, TextAnchor.MiddleCenter);
                    break;
                }
                case "cupcakKe":
                {
                    var a = Song(z, "Deepthroat"); var cpr = Song(z, "Cpr"); z.mk.Prism(Paint(new Color(.9f, .4f, .6f)), c, up, .7f, 1f, 12, 1.4f); for (var k = 0; k < 4; k++) z.mk.Blob(SongGlow(k == 0 ? a : null, new Color(1f, .8f, .9f), .5f), c + up * (1.2f + k * .36f), new Vector3(1.1f - k * .24f, .3f, 1.1f - k * .24f), rng, .1f); z.mk.Blob(redM, c + up * 2.6f, Vector3.one * .16f, rng, 0f); ArtistBits.PostCollider(z.root, c, 2f, .9f);
                    var fa = c + sd * 2f + fr * .6f; Plinth(fa, .6f, .7f); z.mk.Box(white, fa + up * .9f, new Vector3(.5f, .36f, .24f), rot); var cross = SongGlow(cpr, Red, 1.4f); z.mk.Box(cross, fa + up * .9f + fr * .125f, new Vector3(.26f, .08f, .01f), rot); z.mk.Box(cross, fa + up * .9f + fr * .13f, new Vector3(.08f, .26f, .01f), rot);
                    break;
                }
                case "Ab-Soul":
                {
                    var s = Song(z, "Terrorist"); Post(c, 2f); var e3 = c + up * 3f; z.mk.Tri(goldM, e3 + up * 1f, e3 - sd * 1.1f - up * .8f, e3 + sd * 1.1f - up * .8f, e3 - fr * 4f); z.mk.Tri(goldM, e3 + up * 1f, e3 - sd * 1.1f - up * .8f, e3 + sd * 1.1f - up * .8f, e3 + fr * 4f); z.mk.Blob(white, e3 - up * .1f + fr * .08f, new Vector3(.44f, .22f, .04f), rng, 0f); z.mk.Blob(SongGlow(s, Sky, 1.6f), e3 - up * .1f + fr * .12f, new Vector3(.16f, .16f, .03f), rng, 0f); halos.Add(e3 + fr * .4f, 1.4f, Sky, .3f);
                    break;
                }
                case "Afroman":
                {
                    var s = Song(z, "Crazy Rap"); var so = c - sd * .4f; var cloth = Paint(new Color(.5f, .6f, .3f)); z.mk.Box(cloth, so + up * .3f, new Vector3(2.2f, .44f, .9f), rot); z.mk.Box(cloth, so + up * .7f - fr * .36f, new Vector3(2.2f, .7f, .2f), rot); foreach (var e in new[] { -1f, 1f }) z.mk.Box(cloth, so + up * .5f + sd * e * 1f, new Vector3(.2f, .5f, .9f), rot); BoxCol(z.root, so + up * .45f, new Vector3(2.2f, .9f, .9f), rot); Interact.Bench(tlife.transform, so + up * .54f - sd * .5f + fr * .1f, so + up * .54f + sd * .5f + fr * .1f, fr);
                    var bt = new MeshKit(); bt.Prism(Paint(new Color(.66f, .5f, .3f)), Vector3.zero, Vector3.up, .06f, .2f, 6); bt.Prism(SongGlow(s, new Color(.3f, .6f, .3f), .8f), new Vector3(0, .2f, 0), Vector3.up, .025f, .1f, 6); Thing(bt, "Bottle", "a bottle in a paper bag", so + sd * 1.6f + up * .02f, Quaternion.identity, new Vector3(.14f, .32f, .14f), .3f, false);
                    break;
                }
                case "Awkwafina":
                {
                    var s = Song(z, "The Scuttlebutt"); z.mk.Box(white, c + up * .55f, new Vector3(.44f, 1.1f, .44f), rot); z.mk.Prism(SongGlow(s, new Color(.4f, .7f, 1f), .9f), c + up * 1.1f, up, .17f, .5f, 10); z.mk.Box(redM, c + up * .8f + fr * .23f - sd * .08f, new Vector3(.05f, .05f, .03f), rot); z.mk.Box(Paint(Sky), c + up * .8f + fr * .23f + sd * .08f, new Vector3(.05f, .05f, .03f), rot); BoxCol(z.root, c + up * .8f, new Vector3(.44f, 1.6f, .44f), rot);
                    string[] g = { "Glug. You heard it here first: the hydrant on the corner is seeing the lamp post.", "Glug glug. Apparently the Tower's boombox takes D batteries. Four thousand of them.", "The scuttlebutt is: there is no scuttlebutt today. Which is itself suspicious." }; var gn = 0; Spot("The water cooler", c + up * 1.4f + fr * .4f, "Hear the scuttlebutt", new Vector3(.6f, 1f, .8f)).onUse = _ => Say1(c + up * 2.4f, g[gn++ % g.Length]);
                    break;
                }
                case "Bakar":
                {
                    var s = Song(z, "Alive"); var m = SongGlow(s, new Color(1f, .25f, .3f), 1.6f); Post(c - sd * 2f, 2f); Post(c + sd * 2f, 2f); var ys = new[] { 0f, 0f, .2f, -.3f, 1.1f, -.9f, .3f, 0f, 0f, .16f, 0f }; var prev = c - sd * 2f + up * 1.5f; for (var k = 1; k < ys.Length; k++) { var p = c + sd * (-2f + k * .4f) + up * (1.5f + ys[k]); z.mk.Prism(m, prev, (p - prev).normalized, .03f, Vector3.Distance(prev, p), 4); prev = p; } halos.Add(c + up * 1.6f, 1.4f, new Color(1f, .3f, .3f), .26f);
                    break;
                }
                case "Benny The Butcher":
                {
                    var s = Song(z, "Johnny P"); Car(z.root, z.mk, c + up * .02f, rot * Quaternion.Euler(0, 70f, 0), new Color(.1f, .1f, .12f), false); z.mk.Blob(SongGlow(s, Gold, 1.4f), c + up * 1.5f, new Vector3(.1f, .06f, .1f), rng, 0f);
                    break;
                }
                case "Cardi B":
                {
                    var s = Song(z, "WAP"); z.mk.Prism(Paint(new Color(.95f, .8f, .2f)), c, up, .24f, .4f, 10, 1.2f); z.mk.Prism(woodM, c + sd * .1f + up * .1f, (up + sd * .26f).normalized, .025f, 1.7f, 5); z.mk.Blob(Paint(Cream), c + sd * .1f + up * .16f, new Vector3(.2f, .12f, .2f), rng, .3f); z.mk.Prism(SongGlow(s, new Color(.4f, .7f, 1f), .8f), c + fr * 1.1f + up * .01f, up, 1f, .012f, 14); ArtistBits.PostCollider(z.root, c, .5f, .3f); var ws = c + fr * 1.1f + sd * 1.4f; Post(ws, .9f, .03f); z.mk.Tri(Paint(new Color(.95f, .8f, .2f)), ws + up * 1.5f, ws + up * .9f - sd * .3f, ws + up * .9f + sd * .3f, ws - fr * 3f); ArtistBits.Label(z.labels, "WET\nFLOOR", ws + up * 1.1f + fr * .02f, fr, .08f, Ink, TextAnchor.MiddleCenter);
                    break;
                }
                case "Central Cee":
                {
                    var s = Song(z, "Doja"); var p = c + sd * .4f; Post(p - sd * .8f, 2.2f, .04f); Post(p + sd * .8f, 2.2f, .04f); z.mk.Box(white, p + up * 2f, new Vector3(2.2f, .6f, .05f), rot); z.mk.Box(SongGlow(s, Red, .9f), p + up * 1.76f + fr * .03f, new Vector3(2.2f, .1f, .02f), rot); ArtistBits.Label(z.labels, "DOJA ROAD  W12", p + up * 2.06f + fr * .04f, fr, .18f, Ink, TextAnchor.MiddleCenter);
                    break;
                }
                case "Childish Gambino":
                {
                    var s = Song(z, "Redbone"); var m = SongGlow(s, new Color(1f, .35f, .15f), 1.6f); Post(c, 2.2f); var e0 = c + up * 2.9f; for (var k = 0; k < 16; k++) { var a = k * 22.5f * Mathf.Deg2Rad; z.mk.Blob(m, e0 + sd * Mathf.Cos(a) * 1.1f + up * Mathf.Sin(a) * .5f, Vector3.one * .07f, rng, 0f); } z.mk.Prism(m, e0 - fr * .04f, fr, .34f, .06f, 14); z.mk.Prism(dark, e0 - fr * .02f, fr, .15f, .08f, 10); ArtistBits.Label(z.labels, "STAY WOKE", c + up * 1.9f + fr * .08f, fr, .18f, Cream, TextAnchor.MiddleCenter); z.mk.Box(dark, c + up * 1.9f, new Vector3(1.5f, .34f, .08f), rot); halos.Add(e0 + fr * .3f, 1.6f, new Color(1f, .4f, .2f), .3f);
                    break;
                }
                case "Dave":
                {
                    var s = Song(z, "Sprinter"); var vr = rot * Quaternion.Euler(0, 70f, 0); z.mk.Box(white, c + up * 1.4f, new Vector3(2f, 2f, 5.2f), vr); z.mk.Box(white, c + up * .9f + vr * Vector3.forward * 3f, new Vector3(2f, 1f, .9f), vr); z.mk.Box(darkGlass, c + up * 1.8f + vr * Vector3.forward * 2.62f, new Vector3(1.8f, .8f, .04f), vr); foreach (var (x, zz) in new[] { (-1f, 1.9f), (1f, 1.9f), (1f, -1.7f), (-1f, -1.7f) }) z.mk.Prism(dark, c + up * .36f + vr * new Vector3(x - Mathf.Sign(x) * .1f, 0, zz), vr * Vector3.right * Mathf.Sign(x), .36f, .2f, 12); z.mk.Box(SongGlow(s, Gold, .8f), c + up * 1.4f + vr * Vector3.right * 1.01f, new Vector3(.02f, .3f, 3f), vr); BoxCol(z.root, c + up * 1.2f, new Vector3(2f, 2.4f, 6f), vr);
                    break;
                }
                case "Denzel Curry":
                {
                    var s = Song(z, "Bulls on Parade"); var m = SongGlow(s, Red, 1.3f); Post(c, 1.5f, .03f); z.mk.Blob(dark, c + up * 1.6f, new Vector3(.3f, .24f, .2f), rng, 0f); foreach (var e in new[] { -1f, 1f }) for (var k = 0; k < 5; k++) { var a = (20f + k * 22f) * Mathf.Deg2Rad; z.mk.Blob(k == 4 ? m : Paint(Cream), c + up * (1.6f + Mathf.Sin(a) * .7f) + sd * e * (.3f + (1f - Mathf.Cos(a)) * .7f + .2f * k * .2f), Vector3.one * (.13f - k * .02f), rng, 0f); } z.mk.Prism(m, c + up * 1.44f + fr * .2f, fr, .08f, .03f, 8);
                    break;
                }
                case "Dizzee Rascal":
                {
                    var s = Song(z, "Fix Up"); z.mk.Prism(redM, c + up * .9f - fr * .3f, fr, .9f, .6f, 16); z.mk.Prism(SongGlow(s, Cream, .5f), c + up * .9f + fr * .3f, fr, .82f, .02f, 16); foreach (var e in new[] { -1f, 1f }) z.mk.Box(iron, c + up * .2f + sd * e * .5f, new Vector3(.06f, .4f, .7f), rot); BoxCol(z.root, c + up * .9f, new Vector3(1.8f, 1.8f, .7f), rot);
                    string[] hit = { "BOOM. One drum, hit hard, with nothing round it. The whole tune, basically.", "BOOM-boom. Somebody on the next corner straightens their collar without knowing why.", "You hit it again. Fix up. Look sharp." }; var hn = 0; Spot("The big drum", c + up * 1f + fr * .8f, "Hit the drum", new Vector3(1.6f, 1.6f, 1f)).onUse = _ => { life.played = Time.time; Say1(c + up * 2.6f, hit[hn++ % hit.Length]); };
                    break;
                }
                case "Doechii":
                {
                    var s = Song(z, "DENIAL"); z.mk.Box(SongGlow(s, new Color(.2f, .5f, .9f), .7f), c + up * .012f, new Vector3(1.6f, .02f, 6f), rot * Quaternion.Euler(0, 78f, 0)); var bt = c + sd * .6f + up * .04f; z.mk.Box(woodM, bt + up * .14f, new Vector3(.6f, .24f, 1.4f), rot * Quaternion.Euler(0, 78f, 0)); z.mk.Prism(woodM, bt + up * .2f, up, .02f, 1f, 4); z.mk.Tri(white, bt + up * 1.2f, bt + up * .4f, bt + up * .4f + sd * .6f, bt - fr * 3f); z.mk.Tri(white, bt + up * 1.2f, bt + up * .4f, bt + up * .4f + sd * .6f, bt + fr * 3f);
                    var sg = c + fr * 1.6f - sd * 1.6f; Post(sg, 1.1f, .03f); z.mk.Box(white, sg + up * 1.2f, new Vector3(1.3f, .4f, .04f), rot); ArtistBits.Label(z.labels, "this is not a river", sg + up * 1.2f + fr * .03f, fr, .1f, Ink, TextAnchor.MiddleCenter);
                    break;
                }
                case "Fort Minor":
                {
                    var s = Song(z, "Remember the Name"); var hs = new[] { .4f, .8f, .6f, .2f, 2f, 4f }; var lb = new[] { "10%\nluck", "20%\nskill", "15%\nwill", "5%\npleasure", "50%\npain", "100%\nreason" }; var m = SongGlow(s, Gold, .9f);
                    for (var k = 0; k < 6; k++) { var p = c + sd * (-2f + k * .8f); z.mk.Box(k == 5 ? m : Paint(Tags[(k + 1) % 5]), p + up * (hs[k] / 2f), new Vector3(.6f, hs[k], .6f), rot); ArtistBits.Label(z.labels, lb[k], p + up * (hs[k] + .28f), fr, .09f, Cream, TextAnchor.MiddleCenter); } BoxCol(z.root, c + up * 1f + sd * 1.6f, new Vector3(1.4f, 2f, .6f), rot);
                    break;
                }
                case "J. Cole":
                {
                    var s = Song(z, "No Role Modelz"); Plinth(c, 1.1f, 1.5f); z.mk.Box(goldM, c + up * .9f + fr * .56f, new Vector3(.7f, .26f, .02f), rot); ArtistBits.Label(z.labels, "(vacant)", c + up * .9f + fr * .58f, fr, .1f, Ink, TextAnchor.MiddleCenter); z.mk.Prism(SongGlow(s, Gold, 1f), c + up * 1.5f, up, .4f, .012f, 14); foreach (var e in new[] { -1f, 1f }) z.mk.Box(dark, c + up * 1.53f + sd * e * .14f, new Vector3(.12f, .01f, .3f), rot);
                    break;
                }
                case "JID":
                {
                    var s = Song(z, "Community"); Post(c - sd * 1.1f, 2.2f, .05f); Post(c + sd * 1.1f, 2.2f, .05f); z.mk.Box(woodM, c + up * 1.5f, new Vector3(2.4f, 1.4f, .08f), rot); z.mk.Box(SongGlow(s, Gold, .7f), c + up * 2.3f, new Vector3(2.5f, .2f, .12f), rot); ArtistBits.Label(z.labels, "COMMUNITY BOARD", c + up * 2.3f + fr * .07f, fr, .12f, Ink, TextAnchor.MiddleCenter); string[] notes = { "cypher\nthurs", "lost:\none glove", "free\ncouch", "block\nparty", "tutor\nwanted", "found:\none glove" };
                    for (var k = 0; k < 6; k++) { var at = c + up * (1.75f - (k / 3) * .56f) + sd * (-.76f + (k % 3) * .76f) + fr * .05f; z.mk.Box(Paint(Tags[(k * 2) % Tags.Length]), at, new Vector3(.56f, .44f, .01f), rot * Quaternion.Euler(0, 0, (k % 3 - 1) * 5f)); ArtistBits.Label(z.labels, notes[k], at + fr * .012f, fr, .06f, Ink, TextAnchor.MiddleCenter); }
                    break;
                }
                case "Jamws":
                {
                    var s = Song(z, "Toronto"); z.mk.Prism(concrete, c, up, .34f, 5f, 8, .4f); z.mk.Prism(concreteDark, c + up * 3.6f, up, .6f, .4f, 12); z.mk.Prism(darkGlass, c + up * 3.72f, up, .64f, .14f, 12); z.mk.Prism(concrete, c + up * 5f, up, .06f, 1.8f, 5, .3f); z.mk.Blob(SongGlow(s, Red, 1.8f), c + up * 6.8f, Vector3.one * .1f, rng, 0f); halos.Add(c + up * 6.8f, 1f, Red, .5f); ArtistBits.PostCollider(z.root, c, 5f, .4f);
                    break;
                }
                case "K.Flay":
                {
                    var s = Song(z, "High Enough"); foreach (var e in new[] { -1f, 1f }) z.mk.Prism(woodM, c + sd * e * .3f - up * .1f, (up + fr * -.12f).normalized, .04f, 5.4f, 4); for (var k = 0; k < 14; k++) z.mk.Box(woodM, c + up * (.4f + k * .36f) - fr * (.05f + k * .043f), new Vector3(.6f, .05f, .05f), rot); z.mk.Blob(SongGlow(s, new Color(.6f, .8f, 1f), 1.6f), c + up * 5.7f - fr * .66f, Vector3.one * .3f, rng, .2f); halos.Add(c + up * 5.7f - fr * .66f, 1.8f, new Color(.6f, .8f, 1f), .4f); ArtistBits.PostCollider(z.root, c, 3f, .34f);
                    break;
                }
                case "KAYTRANADA":
                {
                    var s = Song(z, "BUS RIDE"); foreach (var e in new[] { -1.3f, 1.3f }) Post(c + sd * e - fr * .5f, 2.4f, .05f); z.mk.Box(darkGlass, c + up * 1.3f - fr * .5f, new Vector3(2.6f, 2f, .04f), rot); z.mk.Box(concreteDark, c + up * 2.5f - fr * .1f, new Vector3(3f, .08f, 1.3f), rot); Bench(z.root, z.mk, c - fr * .1f, fr, 1.8f); var bs = c + sd * 2f + fr * .5f; Post(bs, 2.6f, .04f); z.mk.Prism(SongGlow(s, Gold, 1.2f), bs + up * 2.7f - fr * .03f, fr, .3f, .06f, 14); ArtistBits.Label(z.labels, "BUS", bs + up * 2.7f + fr * .04f, fr, .16f, Ink, TextAnchor.MiddleCenter);
                    break;
                }
                case "Lil Wayne":
                {
                    var s = Song(z, "Don't Cry"); Plinth(c, .9f, .8f); var m = SongGlow(s, new Color(.4f, .7f, 1f), 1.4f); z.mk.Blob(m, c + up * 1.5f, Vector3.one * .46f, rng, 0f); z.mk.Cone(m, c + up * 1.72f, .36f, .9f, 10); halos.Add(c + up * 1.6f, 1.4f, new Color(.4f, .7f, 1f), .4f);
                    break;
                }
                case "Loyle Carner":
                {
                    var s = Song(z, "Ottolenghi"); var seat = Paint(new Color(.2f, .3f, .5f)); foreach (var e in new[] { -1f, 1f }) { var sp = c + fr * e * .9f; var face = -fr * e; var r = Quaternion.LookRotation(face); z.mk.Box(seat, sp + up * .4f, new Vector3(1.5f, .2f, .5f), r); z.mk.Box(seat, sp + up * .9f - face * .26f, new Vector3(1.5f, 1.2f, .14f), r); BoxCol(z.root, sp + up * .7f, new Vector3(1.5f, 1.4f, .6f), r); Interact.Bench(tlife.transform, sp + up * .56f - sd * .4f, sp + up * .56f + sd * .4f, face); }
                    z.mk.Box(concreteDark, c + up * .72f, new Vector3(.9f, .05f, .6f), rot); z.mk.Prism(iron, c, up, .04f, .72f, 5); z.mk.Box(SongGlow(s, new Color(1f, .8f, .5f), .7f), c + up * 1.6f - sd * .8f, new Vector3(.04f, 1f, 1.6f), rot);
                    var bk = new MeshKit(); bk.Box(Paint(new Color(.9f, .5f, .2f)), new Vector3(0, .03f, 0), new Vector3(.24f, .05f, .3f), Quaternion.identity); bk.Box(white, new Vector3(.005f, .03f, 0), new Vector3(.23f, .04f, .28f), Quaternion.identity); Thing(bk, "Cookbook", "a cookbook, read on the train", c + up * .76f, rot * Quaternion.Euler(0, 20f, 0), new Vector3(.26f, .08f, .32f), .3f, false);
                    break;
                }
                case "Lupe Fiasco":
                {
                    var s = Song(z, "Daydreamin"); var eye = SongGlow(s, Gold, 1.8f); foreach (var e in new[] { -1f, 1f }) { z.mk.Box(concrete, c + sd * e * .5f + up * .8f, new Vector3(.6f, 1.6f, .6f), rot); z.mk.Box(concreteDark, c + sd * e * 1.2f + up * 2.5f, new Vector3(.44f, 1.6f, .44f), rot); for (var k = 0; k < 3; k++) z.mk.Box(windowM, c + sd * e * .5f + up * (.4f + k * .44f) + fr * .31f, new Vector3(.3f, .24f, .02f), rot); z.mk.Blob(eye, c + sd * e * .22f + up * 3.96f + fr * .42f, Vector3.one * .09f, rng, 0f); }
                    z.mk.Box(brickM, c + up * 2.5f, new Vector3(1.8f, 1.8f, 1f), rot); for (var k = 0; k < 6; k++) z.mk.Box(windowM, c + up * (2f + (k / 3) * .7f) + sd * (-.5f + (k % 3) * .5f) + fr * .51f, new Vector3(.3f, .4f, .02f), rot); z.mk.Box(concrete, c + up * 3.9f, new Vector3(.9f, .8f, .8f), rot); z.mk.Prism(iron, c + up * 4.3f, up, .02f, .6f, 4); BoxCol(z.root, c + up * 2f, new Vector3(2.8f, 4f, 1f), rot);
                    break;
                }
                case "M.I.A.":
                {
                    var s = Song(z, "Paper Planes"); life.planesSong = s; var m = SongGlow(s, new Color(1f, 1f, 1f), 1f); Post(c, 3.6f); for (var k = 0; k < 6; k++) { var d = Quaternion.Euler(0, k * 60f, 0) * fr; var hp = c + up * 3.6f + d * (.8f + (k % 2) * .5f); z.mk.Prism(iron, c + up * 3.6f, d, .012f, .8f + (k % 2) * .5f, 3); var drop = .5f + (k % 3) * .5f; z.mk.Prism(dark, hp, -up, .005f, drop, 3); var pc = hp - up * (drop + .06f); var fw = Quaternion.Euler(0, k * 77f, 0) * Vector3.forward; var rt = Vector3.Cross(up, fw); foreach (var e in new[] { -1f, 1f }) { z.mk.Tri(m, pc + fw * .34f, pc - fw * .26f + rt * e * .24f + up * .06f, pc - fw * .26f, pc - up); z.mk.Tri(m, pc + fw * .34f, pc - fw * .26f + rt * e * .24f + up * .06f, pc - fw * .26f, pc + up); } }
                    var cr = c + sd * 1.8f + fr * .4f; Plinth(cr, .6f, .8f); z.mk.Box(dark, cr + up * .96f, new Vector3(.44f, .3f, .4f), rot); z.mk.Box(Paint(new Color(.3f, .7f, .4f)), cr + up * 1.18f - fr * .1f, new Vector3(.36f, .16f, .04f), rot); z.mk.Box(goldM, cr + up * .84f + fr * .3f, new Vector3(.4f, .06f, .2f), rot);
                    break;
                }
                case "Madvillain":
                {
                    var s = Song(z, "Accordion"); var mp = c - sd * 1.2f; Post(mp, 1.5f, .04f); var mm = Paint(new Color(.6f, .62f, .66f)); z.mk.Blob(mm, mp + up * 1.8f, new Vector3(.26f, .32f, .22f), rng, 0f); z.mk.Box(dark, mp + up * 1.86f + fr * .2f, new Vector3(.36f, .08f, .06f), rot); z.mk.Box(mm, mp + up * 1.86f + fr * .22f, new Vector3(.05f, .12f, .06f), rot); z.mk.Box(mm, mp + up * 1.62f + fr * .2f, new Vector3(.3f, .2f, .06f), rot); for (var k = 0; k < 3; k++) z.mk.Box(dark, mp + up * 1.62f + fr * .235f + sd * (-.08f + k * .08f), new Vector3(.02f, .14f, .01f), rot);
                    var ac = c + sd * 1.2f; Plinth(ac, .7f, .7f); var glow = SongGlow(s, Red, .8f); foreach (var e in new[] { -1f, 1f }) z.mk.Box(glow, ac + up * .96f + sd * e * .34f, new Vector3(.14f, .5f, .3f), rot); for (var k = 0; k < 7; k++) z.mk.Box(k % 2 == 0 ? Paint(Cream) : dark, ac + up * .96f + sd * (-.24f + k * .08f), new Vector3(.07f, .44f - (k % 2) * .06f, .26f), rot);
                    string[] sq = { "Wheeze. A loop of accordion, slightly broken, and suddenly everything rhymes.", "You squeeze it. Living off borrowed time, the clock ticks faster.", "It sounds like a villain's theme played at a kitchen table. Perfect." }; var qn = 0; Spot("The accordion", ac + up * 1.4f + fr * .4f, "Squeeze the accordion", new Vector3(.9f, .8f, .8f)).onUse = _ => { life.played = Time.time; Say1(ac + up * 2.4f, sq[qn++ % sq.Length]); };
                    break;
                }
                case "Megan Thee Stallion":
                {
                    var s = Song(z, "Thot Shit"); Plinth(c, 1.6f, .7f); var g = SongGlow(s, Gold, .9f); var b = c + up * 2.2f; var lean = Quaternion.AngleAxis(-34f, sd); z.mk.Box(g, b, new Vector3(.6f, .7f, 1.7f), rot * Quaternion.Euler(-34f, 0, 0)); foreach (var e in new[] { -1f, 1f }) { z.mk.Prism(g, c + up * .7f - fr * .5f + sd * e * .2f, (up * 1.1f - fr * .1f).normalized, .09f, 1.2f, 5); z.mk.Prism(g, b + lean * fr * .7f + sd * e * .2f, (lean * (fr + up * -.2f)).normalized, .07f, .9f, 5); }
                    z.mk.Prism(g, b + lean * fr * .7f + up * .2f, (lean * (fr * .5f + up)).normalized, .2f, 1f, 6, .7f); z.mk.Box(g, b + lean * fr * .7f + lean * (fr * .5f + up).normalized * 1.1f + up * .16f, new Vector3(.26f, .3f, .7f), rot * Quaternion.Euler(10f, 0, 0)); z.mk.Prism(g, b - lean * fr * .8f, (-fr + up * -.6f).normalized, .08f, .9f, 4, .3f); halos.Add(b + up * .4f, 2.2f, Gold, .2f);
                    break;
                }
                case "Missy Elliott":
                {
                    var s = Song(z, "Get Ur Freak On"); var m = SongGlow(s, new Color(.7f, .35f, 1f), 1.3f); foreach (var e in new[] { -1f, 1f }) { Post(c + sd * e * 1.7f, 2.2f, .06f); z.mk.Prism(dark, c + sd * e * 1.7f + up * 2.3f - sd * e * .2f, sd * e, .56f, .4f, 14); z.mk.Prism(m, c + sd * e * 1.5f + up * 2.3f - sd * e * .03f, -sd * e, .4f, .03f, 12); } for (var k = 0; k <= 10; k++) { var a = k * 18f * Mathf.Deg2Rad; var a2 = (k + 1) * 18f * Mathf.Deg2Rad; if (k == 10) break; var p0 = c + up * (2.7f + Mathf.Sin(a) * 1.5f) - sd * Mathf.Cos(a) * 1.8f; var p1 = c + up * (2.7f + Mathf.Sin(a2) * 1.5f) - sd * Mathf.Cos(a2) * 1.8f; z.mk.Prism(dark, p0, (p1 - p0).normalized, .09f, Vector3.Distance(p0, p1) + .04f, 5); } halos.Add(c + up * 2.4f, 2.2f, new Color(.7f, .35f, 1f), .2f);
                    break;
                }
                case "Nas":
                {
                    var s = Song(z, "N.Y. State of Mind"); var m = SongGlow(s, Gold, 1.4f); var br = rot * Quaternion.Euler(0, 78f, 0); var al = br * Vector3.forward; z.mk.Box(concreteDark, c + up * 1.1f, new Vector3(.9f, .16f, 6.4f), br); foreach (var e in new[] { -1.6f, 1.6f }) { foreach (var q in new[] { -.4f, .4f }) z.mk.Box(iron, c + al * e + br * Vector3.right * q + up * 1.6f, new Vector3(.12f, 3.2f, .12f), br); z.mk.Box(iron, c + al * e + up * 3.1f, new Vector3(.94f, .12f, .12f), br); z.mk.Blob(m, c + al * e + up * 3.3f, Vector3.one * .08f, rng, 0f); }
                    for (var k = 0; k < 9; k++) { var t = -3.2f + k * .8f; var top = 3.1f - Mathf.Abs(Mathf.Abs(t) - 1.6f) * 1.1f; if (top > 1.3f) z.mk.Prism(iron, c + al * t + up * 1.18f, up, .012f, top - 1.18f, 3); } BoxCol(z.root, c + up * 1f, new Vector3(.9f, 2f, 6.4f), br); for (var k = 0; k < 5; k++) z.mk.Box(k % 2 == 0 ? concrete : brickM, c - fr * 1.4f + sd * (-2f + k * 1f) + up * (.9f + (k * 37 % 5) * .24f), new Vector3(.8f, 1.8f + (k * 37 % 5) * .48f, .5f), rot);
                    break;
                }
                case "Public Enemy":
                {
                    var s = Song(z, "Fight The Power"); life.fightSong = s; Plinth(c, 1.3f, .7f); z.mk.Prism(SongGlow(s, Red, 1.3f), c + up * .7f, up, .76f, .03f, 16); z.mk.Prism(dark, c + up * .7f, up, .3f, 2f, 8); z.mk.Box(dark, c + up * 3f, new Vector3(.76f, .7f, .7f), rot); for (var k = 0; k < 4; k++) z.mk.Box(Paint(new Color(.14f, .13f, .15f)), c + up * 3.1f + sd * (-.28f + k * .19f) + fr * .34f, new Vector3(.17f, .5f, .14f), rot); z.mk.Box(dark, c + up * 2.84f + fr * .3f - sd * .4f, new Vector3(.2f, .2f, .5f), rot * Quaternion.Euler(0, 30f, 0)); halos.Add(c + up * 1f, 1.8f, Red, .26f);
                    break;
                }
                case "Sheck Wes":
                {
                    var s = Song(z, "Mo Bamba"); Hoop(z.ctx, z.mk, c - fr * 1f, fr, 6.4f); z.mk.Prism(SongGlow(s, Gold, 1.3f), c + up * 6.4f + fr * .08f, up, .26f, .03f, 12); Ball(c + fr * 1.6f + sd * .6f + up * .02f); ArtistBits.Label(z.labels, "7 ft is the low rim", c - fr * .9f + up * 1.6f, fr, .1f, Cream, TextAnchor.MiddleCenter);
                    break;
                }
                case "The Streets":
                {
                    var s = Song(z, "Fit but You Know It"); z.mk.Box(white, c + up * .5f, new Vector3(2.4f, 1f, .7f), rot); z.mk.Box(Paint(Sky), c + up * 1.02f, new Vector3(2.5f, .05f, .8f), rot); BoxCol(z.root, c + up * .5f, new Vector3(2.4f, 1f, .7f), rot); foreach (var e in new[] { -1.2f, 1.2f }) z.mk.Prism(iron, c + sd * e + up * 1f, up, .03f, 1.4f, 4); z.mk.Box(SongGlow(s, new Color(1f, .9f, .4f), 1.1f), c + up * 2.6f, new Vector3(2.6f, .5f, .1f), rot); ArtistBits.Label(z.labels, "CHIPS  ·  OPEN LATE", c + up * 2.6f + fr * .06f, fr, .18f, Ink, TextAnchor.MiddleCenter);
                    var ch = new MeshKit(); ch.Prism(white, Vector3.zero, Vector3.up, .04f, .16f, 6, 1.8f); for (var k = 0; k < 5; k++) ch.Box(Paint(new Color(.95f, .8f, .36f)), new Vector3((k - 2) * .02f, .2f, (k % 2) * .02f), new Vector3(.02f, .14f, .02f), Quaternion.Euler(0, 0, (k - 2) * 8f)); Thing(ch, "Chips", "a cone of chips, salt and vinegar", c + up * 1.06f + sd * .5f, Quaternion.identity, new Vector3(.16f, .3f, .16f), .2f, false);
                    Folk(c - fr * .8f - sd * .3f, fr, Resident.Pose.Stand, new[] { "Salt and vinegar? 'Course salt and vinegar.", "Geezer in the queue's been telling the same story for two verses.", "Open late. It's always late up here; the sun's been setting for years." }, Local(Residents.Tool.Spatula, true));
                    break;
                }
                case "Travis Scott":
                {
                    var s = Song(z, "SICKO MODE"); life.sickoSong = s; var m = SongGlow(s, new Color(1f, .5f, .2f), 1.3f); const float R = 2.4f; var hub = c + up * (R + .4f); for (var k = 0; k < 24; k++) { var a0 = k * 15f * Mathf.Deg2Rad; var a1 = (k + 1) * 15f * Mathf.Deg2Rad; foreach (var e in new[] { -.25f, .25f }) { var p0 = hub + fr * e + sd * Mathf.Cos(a0) * R + up * Mathf.Sin(a0) * R; var p1 = hub + fr * e + sd * Mathf.Cos(a1) * R + up * Mathf.Sin(a1) * R; z.mk.Prism(m, p0, (p1 - p0).normalized, .04f, Vector3.Distance(p0, p1) + .02f, 4); } z.mk.Box(iron, hub + sd * Mathf.Cos(a0) * R + up * Mathf.Sin(a0) * R, new Vector3(.05f, .05f, .6f), rot); }
                    foreach (var e in new[] { -1f, 1f }) { z.mk.Prism(iron, c + sd * e * 1.6f - up * .2f, (hub + sd * e * .9f - c - sd * e * 1.6f).normalized, .06f, 3.2f, 4); ArtistBits.PostCollider(z.root, c + sd * e * 1.6f, 1.6f, .1f); } z.mk.Box(Paint(Purple), hub + up * R + up * .2f, new Vector3(.5f, .3f, .9f), rot * Quaternion.Euler(0, 90f, 0)); halos.Add(hub, 3f, new Color(1f, .5f, .2f), .14f);
                    break;
                }
                case "Tyler, The Creator":
                {
                    var s = Song(z, "EARFQUAKE"); life.earSong = s; var m = SongGlow(s, new Color(1f, .5f, .7f), 1.5f); var prev = c - sd * 2.6f + up * .02f; for (var k = 1; k <= 8; k++) { var p = c + sd * (-2.6f + k * .65f) + fr * ((k % 2) * .7f - .35f) + up * .02f; var mid = (prev + p) / 2f; var dir = (p - prev).normalized; z.mk.Box(dark, mid, new Vector3(.4f, .02f, Vector3.Distance(prev, p) + .2f), Quaternion.LookRotation(dir)); z.mk.Box(m, mid + up * .03f, new Vector3(.12f, .02f, Vector3.Distance(prev, p) + .1f), Quaternion.LookRotation(dir)); prev = p; }
                    var pastel = new[] { new Color(1f, .7f, .8f), new Color(.7f, .9f, 1f), new Color(1f, .95f, .6f), new Color(.75f, 1f, .8f) }; for (var k = 0; k < 9; k++) { var fp = c + sd * (-2.3f + k * .58f) + fr * ((k % 3) * .5f - .5f); var h = .4f + (k % 4) * .16f; z.mk.Prism(leafM, fp, up, .02f, h, 4); for (var q = 0; q < 5; q++) z.mk.Blob(Paint(pastel[k % 4]), fp + up * h + Quaternion.Euler(0, q * 72f, 0) * Vector3.forward * .09f, new Vector3(.07f, .03f, .07f), rng, 0f); z.mk.Blob(goldM, fp + up * (h + .02f), Vector3.one * .04f, rng, 0f); }
                    break;
                }
                case "Will Smith":
                {
                    var s = Song(z, "Arabian Nights"); z.mk.Box(Paint(Purple), c + up * .02f, new Vector3(2.6f, .03f, 1.7f), rot); z.mk.Box(goldM, c + up * .045f, new Vector3(2.2f, .02f, 1.3f), rot); z.mk.Box(Paint(new Color(.7f, .2f, .3f)), c + up * .07f, new Vector3(1.9f, .02f, 1f), rot); var lp = c + up * .1f; z.mk.Prism(goldM, lp, up, .14f, .06f, 8); z.mk.Blob(goldM, lp + up * .22f, new Vector3(.3f, .16f, .2f), rng, 0f); z.mk.Prism(goldM, lp + up * .22f + sd * .2f, (sd + up * .5f).normalized, .05f, .4f, 5, .4f); z.mk.Prism(goldM, lp + up * .36f, up, .06f, .12f, 6, .4f);
                    var wisp = SongGlow(s, new Color(.3f, .6f, 1f), 1.5f); for (var k = 0; k < 6; k++) z.mk.Blob(wisp, lp + sd * (.56f + Mathf.Sin(k * 1.1f) * .14f) + up * (.5f + k * .3f), Vector3.one * (.06f + k * .035f), rng, .2f); life.smokes.Add(lp + sd * .56f + up * 2.4f); halos.Add(lp + up * 1.4f + sd * .5f, 1.4f, new Color(.3f, .6f, 1f), .3f);
                    break;
                }
                case "mgk":
                {
                    var s = Song(z, "JJK"); var m = SongGlow(s, new Color(1f, .4f, .7f), 1.3f); Plinth(c, .9f, .3f); var g = c + up * .3f; z.mk.Blob(m, g + up * .5f, new Vector3(.36f, .44f, .07f), rng, 0f); z.mk.Blob(m, g + up * .9f, new Vector3(.26f, .3f, .07f), rng, 0f); z.mk.Box(woodM, g + up * 1.7f, new Vector3(.1f, 1.3f, .05f), rot); z.mk.Box(m, g + up * 2.44f, new Vector3(.16f, .3f, .05f), rot); z.mk.Box(white, g + up * .6f + fr * .08f, new Vector3(.14f, .05f, .02f), rot); halos.Add(g + up * .8f + fr * .3f, 1.2f, new Color(1f, .4f, .7f), .3f);
                    break;
                }
            }
        }
    }
}
