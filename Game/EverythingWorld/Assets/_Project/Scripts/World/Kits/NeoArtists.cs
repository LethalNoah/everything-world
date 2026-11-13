using System.Collections.Generic;
using UnityEngine;
using static EverythingWorld.World.PlaceTools;

namespace EverythingWorld.World
{
    /// The Neo Soul Quarter's artists. Most have a nook of their own: a piece of terracotta wall with a lit arch in
    /// it, a chair, a plant, candles, and one thing from their song; covers on a cream card on a clay board.
    /// - Maxwell (on the Courtyard): the Listening Room. A room to walk into: a wall of records, a turntable to play
    ///   ("Sumthin' Sumthin'"), two sofas, candles; crates on the step; a lamp left on by an empty chair ("This
    ///   Woman's Work"); a four-leaf clover in a pot ("Fortunate"); white wings on the wall ("Pretty Wings": feathers
    ///   fall); rungs of light climbing from the roof ("Ascension": lights rise from every street).
    /// - Ms. Lauryn Hill: an open-air schoolroom. A blackboard, desks to sit at, an apple; a street lamp with three
    ///   microphones under it ("Doo Wop"); a rope knotted between two posts ("Ex-Factor"); two chairs facing each
    ///   other over one lantern ("Nothing Even Matters").
    /// - Erykah Badu: a great wheel that never stops turning ("On & On": it turns faster); a signpost pointing every
    ///   way at once ("Didn't Cha Know"); tea and incense on a low table.
    /// - D'Angelo: a small round stage, one warm spotlight, an electric piano to play ("Untitled": the quarter goes
    ///   dark but for the spotlight).
    /// - Solange: three white cranes with paper cranes hanging from them ("Cranes in the Sky": paper cranes drift
    ///   up); a shelf of brown bottles ("Almeda"); bright cloth on a line ("Losing You"); round white steps to sit on.
    /// - Also Jill Scott's gold sun, Jorja Smith's blue lamp, Kali Uchis' tin-can telephone, Hiatus Kaiyote's red
    ///   room, SAULT's black cube in a ring of flame, Children of Zeus' workbench, KIRBY's web under a street lamp,
    ///   and Victoria Monét's mirror.
    public partial class NeoKit
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
                Add("Jorja Smith", "Blue Lights", "@down honey street"); Add("Jill Scott", "Golden", "@the courtyard"); Add("D'Angelo", "Untitled", "@one spotlight");
                Add("Maxwell", "Pretty Wings", "@the listening room"); Add("Hiatus Kaiyote", "Red Room", "Hiatus Kaiyote"); Add("SAULT", "Wildfires", "SAULT");
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
            if (tile == null) MakeMaterials();
            var root = new GameObject("Artist: " + a.name).transform; root.SetParent(ctx.Root, false);
            var front = facing.TryGetValue(a.name, out var f) ? f : new Vector3(center.x - a.x, 0, center.z - a.z).normalized;
            if (front.sqrMagnitude < .01f) front = Vector3.forward;
            var labels = new GameObject("Labels").transform; labels.SetParent(root, false);
            var area = root.gameObject.AddComponent<ArtistArea>();
            area.artistName = a.name; area.stationId = a.stationId; area.radius = a.r; area.glowColor = Honey;
            var z = new Plot { ctx = ctx, root = root, labels = labels, mk = new MeshKit(), solid = new MeshKit(), pictures = new MeshKit(), a = a, c = ctx.OnGround(a.x, a.z), front = front, side = Vector3.Cross(Vector3.up, front), area = area };
            var reach = Foot(a); var up = Vector3.up;
            z.mk.Prism(a.name == "D'Angelo" ? tileDark : tile, z.c - up * .25f, up, reach * .66f, .33f, 24);
            switch (a.name)
            {
                case "Maxwell": Maxwell(z); break;
                case "Ms. Lauryn Hill": LaurynHill(z); break;
                case "Erykah Badu": Badu(z); break;
                case "D'Angelo": DAngelo(z); break;
                case "Solange": Solange(z); break;
                default: Others(z, Nook(z)); break;
            }
            SongLamps(z);
            Board(z, z.At(reach * .6f, -reach * .45f - 1.5f), front);
            ArtistBits.Plaque(z.mk, z.solid, labels, a, z.At(reach * .6f + 1.5f, reach * .45f + 1.5f), front, woodM, Paint(Honey), Ink, 1.6f);
            Lantern(ctx, z.mk, z.At(reach * .5f, reach * .66f));
            if (z.area.glowLight == null) z.area.glowLight = ArtistBits.PointLight(root, z.c + up * 4f + front * 3f, new Color(1f, .8f, .52f), HasStation(a) ? 24f : 16f, 2.2f);
            area.glowRenderers = new Renderer[0];
            z.mk.Build("Area", root, false);
            z.solid.BuildCollider("Walls", root);
            z.pictures.Build("Covers", root, false);
            noGrass.Add(new Vector3(z.c.x, reach * .9f, z.c.z));
            var mark = new GameObject("Shot: " + a.name + " up close").transform; mark.SetParent(ctx.Root, false);
            var cam = z.At(reach * .85f, reach * .3f) + up * 2f; var look = z.At(-reach * .3f, 0) + up * 2.2f - cam; mark.SetPositionAndRotation(cam, Quaternion.LookRotation(look.normalized));
        }

        /// The covers on a cream card on a clay board with a honey-coloured rail and a row of candles at its foot.
        void Board(Plot z, Vector3 ground, Vector3 dir)
        {
            dir = Vector3.ProjectOnPlane(dir, Vector3.up).normalized; var rot = Quaternion.LookRotation(dir);
            var n = Mathf.Max(1, Mathf.Min(7, z.a.songList?.Length ?? 1)); const float size = .9f; var w = n * size * 1.25f + .6f;
            var c = ground + Vector3.up * 1.7f;
            z.mk.Box(Paint(Walls[2]), c - Vector3.up * .5f - dir * .06f, new Vector3(w + .5f, size + 2f, .2f), rot); z.mk.Box(Paint(Honey), c + Vector3.up * (size / 2f + .56f) - dir * .06f, new Vector3(w + .7f, .12f, .3f), rot);
            z.solid.Box(woodM, c - Vector3.up * .5f, new Vector3(w + .5f, size + 2f, .3f), rot); z.mk.Box(Paint(Cream), c - Vector3.up * .1f + dir * .07f, new Vector3(w, size + .9f, .04f), rot);
            ArtistBits.CoverWall(z.pictures, z.labels, z.ctx, z.a, c + dir * .1f + Vector3.up * .08f, dir, n, size, n, true, Ink);
            var rng = new System.Random(z.a.name.Length); Candles(z.mk, ground + dir * .6f + rot * Vector3.right * (w / 2f), 3, rng);
        }

        /// A candle in a glass jar on a post for each song without its own piece.
        void SongLamps(Plot z)
        {
            if (!SongIds.TryGetValue(z.a.name, out var list)) return;
            var up = Vector3.up; var k = 0; var seen = new HashSet<string>();
            foreach (var (_, id) in list)
            {
                if (z.lit.Contains(id) || !seen.Add(id)) continue;
                var p = z.At(Foot(z.a) * .55f - (k % 2) * 1.3f, Foot(z.a) * .2f + (k / 2) * 1.3f);
                z.mk.Prism(iron, p - up * .1f, up, .035f, 1.3f, 5); z.mk.Prism(SongGlow(id, Honey, 1.6f), p + up * 1.22f, up, .1f, .22f, 6); z.mk.Prism(iron, p + up * 1.44f, up, .11f, .03f, 6); ArtistBits.PostCollider(z.root, p, 1.3f, .08f);
                k++;
            }
        }

        /// A nook of their own: a piece of terracotta wall with a lit arch in it, vines over the top, a chair, a plant, candles.
        PlaceFrame Nook(Plot z)
        {
            var up = Vector3.up; var hue = z.a.name.Length + z.a.name[0]; var rng = new System.Random(hue); var f = new PlaceFrame(z.At(-Foot(z.a) * .48f, 0), z.front); var wall = Paint(Walls[hue % Walls.Length]);
            z.mk.Box(wall, f.P(0, 1.3f, 0), new Vector3(5.2f, 3.2f, .5f), f.r); z.solid.Box(wall, f.P(0, 1.3f, 0), new Vector3(5.2f, 3.2f, .5f), f.r); z.mk.Box(Paint(Walls[(hue + 3) % Walls.Length]), f.P(0, 2.98f, 0), new Vector3(5.5f, .2f, .7f), f.r);
            z.mk.Box(windowM, f.P(0, 1.2f, .27f), new Vector3(1.2f, 1.5f, .04f), f.r); z.mk.Prism(windowM, f.P(0, 1.95f, .25f), f.Fwd, .6f, .04f, 14); halos.Add(f.P(0, 1.5f, .8f), 1.6f, Honey, .32f);
            for (var k = 0; k < 5; k++) { var len = .5f + (float)rng.NextDouble() * 1.3f; z.mk.Box(k % 2 == 0 ? leafM : leafDark, f.P(-2.3f + k * 1.1f + (float)rng.NextDouble() * .3f, 2.9f - len / 2f, .3f + (k % 2) * .04f), new Vector3(.44f, len, .03f), f.r); }
            Chair(z.mk, f.P(-1.8f, .08f, 1.1f), (f.Fwd + f.Right * .3f).normalized);
            var pp = f.P(2.1f, .08f, .9f); z.mk.Prism(pot, pp, up, .26f, .44f, 8, 1.25f); for (var q = 0; q < 6; q++) z.mk.Prism(q % 2 == 0 ? leafM : leafPale, pp + up * .4f, (up + Quaternion.Euler(0, q * 60f, 0) * Vector3.forward * .3f).normalized, .07f, 1f + (q % 3) * .3f, 4, .2f); ArtistBits.PostCollider(z.root, pp, .5f, .3f);
            Candles(z.mk, f.P(-.9f, .08f, .7f), 4, rng);
            return f;
        }

        // ---------------------------------------------------------------- Maxwell: the Listening Room
        void Maxwell(Plot z)
        {
            var up = Vector3.up; var rng = new System.Random(96); var asc = Song(z, "Ascension"); var fo = Song(z, "Fortunate"); var pw = Song(z, "Pretty Wings"); var ss = Song(z, "Sumthin"); var tw = Song(z, "This Woman"); life.ascSong = asc; life.wingsSong = pw;
            var f = new PlaceFrame(z.At(-3f, 0) + up * .14f, z.front); const float W = 11f, Dp = 8f, H = 3.6f; var wall = Paint(Walls[2]); var inner = Paint(new Color(.56f, .3f, .3f));
            // the room: walls with a doorway, a plank floor, a flat roof with a honey-coloured edge
            void Wall(Vector3 c, Vector3 size) { z.mk.Box(wall, c, size, f.r); z.solid.Box(wall, c, size, f.r); }
            Wall(f.P(0, H / 2f, -Dp / 2f + .15f), new Vector3(W, H, .3f)); foreach (var e in new[] { -1f, 1f }) { Wall(f.P(e * (W / 2f - .15f), H / 2f, 0), new Vector3(.3f, H, Dp - .6f)); Wall(f.P(e * (W / 4f + .55f), H / 2f, Dp / 2f - .15f), new Vector3(W / 2f - 1.1f, H, .3f)); }
            Wall(f.P(0, H - .45f, Dp / 2f - .15f), new Vector3(2.2f, .9f, .3f)); z.mk.Box(woodM, f.P(0, .03f, 0), new Vector3(W - .6f, .06f, Dp - .6f), f.r); z.mk.Box(wall, f.P(0, H + .1f, 0), new Vector3(W + .4f, .2f, Dp + .4f), f.r); z.solid.Box(wall, f.P(0, H + .1f, 0), new Vector3(W + .4f, .2f, Dp + .4f), f.r); z.mk.Box(Paint(Honey), f.P(0, H + .26f, 0), new Vector3(W + .6f, .12f, Dp + .6f), f.r);
            IndoorSpaces.Add(f.P(0, H / 2f, 0), f.r, new Vector3(W, H, Dp));
            ArtistBits.Label(z.labels, "THE LISTENING ROOM", f.P(0, H - .4f, Dp / 2f + .03f), f.Fwd, .22f, Honey, TextAnchor.MiddleCenter);
            z.area.glowLight = ArtistBits.PointLight(z.root, f.P(0, 2.9f, .4f), new Color(1f, .76f, .48f), 13f, 2.6f, true); ArtistBits.PointLight(z.root, f.P(0, 2.6f, Dp / 2f + 2.4f), new Color(1f, .8f, .5f), 14f, 2.2f);
            // outside: a lit window on the left, the wings on the right, vines over the roof edge
            z.mk.Box(white, f.P(-3.3f, 1.8f, Dp / 2f + .02f), new Vector3(2.3f, 1.7f, .06f), f.r); z.mk.Box(windowM, f.P(-3.3f, 1.8f, Dp / 2f + .07f), new Vector3(2f, 1.4f, .06f), f.r); halos.Add(f.P(-3.3f, 1.8f, Dp / 2f + .6f), 1.8f, Honey, .32f);
            var wing = SongGlow(pw, new Color(1f, .97f, 1f), .45f); foreach (var e in new[] { -1f, 1f }) for (var k = 0; k < 6; k++) { var a = Quaternion.AngleAxis(e * (14f + k * 15f), f.Fwd); var len = 1.9f - k * .2f; z.mk.Box(wing, f.P(3.3f, 1.7f, Dp / 2f + .04f + k * .035f) + a * up * (len / 2f + .1f), new Vector3(.26f, len, .02f), a * f.r); }
            for (var k = 0; k < 7; k++) { var len = .5f + (float)rng.NextDouble() * 1.2f; z.mk.Box(k % 2 == 0 ? leafM : leafDark, f.P(-W / 2f + .8f + k * 1.6f, H - len / 2f, Dp / 2f + .06f + (k % 2) * .04f), new Vector3(.5f, len, .03f), f.r); }
            // "Ascension": rungs of light climbing from the roof
            var rung = SongGlow(asc, Honey, 1.8f); for (var k = 0; k < 9; k++) z.mk.Box(rung, f.P(0, H + 1.2f + k * 1.5f, -1.5f - k * .4f), new Vector3(1.8f - k * .14f, .09f, .09f), f.r); halos.Add(f.P(0, H + 13f, -5f), 3f, Honey, .3f);
            // the step: crates of records, a record to carry, a clover in a pot ("Fortunate"), a fig tree
            z.mk.Box(tileDark, f.P(0, -.08f, Dp / 2f + .9f), new Vector3(4.4f, .14f, 1.8f), f.r); Crate(z.mk, f.P(1.5f, 0, Dp / 2f + .6f), f.r * Quaternion.Euler(0, 12f, 0), rng); Crate(z.mk, f.P(2f, 0, Dp / 2f + 1.3f), f.r * Quaternion.Euler(0, -20f, 0), rng); Crate(z.mk, f.P(1.6f, .4f, Dp / 2f + .62f), f.r * Quaternion.Euler(0, 30f, 0), rng); BoxCol(z.root, f.P(1.7f, .4f, Dp / 2f + .9f), new Vector3(1.2f, .8f, 1.3f), f.r);
            var rec = new MeshKit(); rec.Box(Paint(new Color(.5f, .2f, .3f)), new Vector3(0, .012f, 0), new Vector3(.32f, .02f, .32f), Quaternion.identity); rec.Prism(dark, new Vector3(.06f, .024f, 0), Vector3.up, .14f, .006f, 14); Thing(rec, "Record", "a record, half out of its sleeve", f.P(-1.3f, .02f, Dp / 2f + 1.2f), f.r, new Vector3(.34f, .05f, .34f), .2f, false);
            var cl = f.P(-1.8f, 0, Dp / 2f + .5f); z.mk.Prism(pot, cl, up, .2f, .32f, 8, 1.25f); var clm = SongGlow(fo, new Color(.3f, .9f, .4f), 1.2f); z.mk.Prism(stemM, cl + up * .3f, up, .02f, .4f, 4); for (var k = 0; k < 4; k++) z.mk.Prism(clm, cl + up * .72f + Quaternion.Euler(0, k * 90f + 45f, 0) * Vector3.forward * .13f, up, .12f, .02f, 8); ArtistBits.PostCollider(z.root, cl, .4f, .24f);
            var fg = f.P(-5f, 0, Dp / 2f + 1.8f); Fig(z.ctx, z.mk, z.ctx.OnGround(fg.x, fg.z), .8f, rng); var sn = f.P(5.2f, 0, Dp / 2f + 1.6f); SnakePlant(z.ctx, z.mk, z.ctx.OnGround(sn.x, sn.z), .7f, rng);
            // inside: the wall of records, the console and its turntable ("Sumthin' Sumthin'"), speakers
            for (var row = 0; row < 4; row++) for (var k = 0; k < 13; k++) z.mk.Box(Paint(Color.HSVToRGB((float)rng.NextDouble(), .2f + (float)rng.NextDouble() * .4f, .5f + (float)rng.NextDouble() * .4f)), f.P(-3.6f + k * .6f, 1.3f + row * .56f, -Dp / 2f + .33f), new Vector3(.5f, .5f, .03f), f.r);
            var cn = f.P(0, .06f, -Dp / 2f + .8f); z.mk.Box(woodM, cn + up * .45f, new Vector3(3.2f, .9f, .6f), f.r); BoxCol(z.root, cn + up * .45f, new Vector3(3.2f, .9f, .6f), f.r); z.mk.Box(iron, cn + up * .94f - f.Right * .7f, new Vector3(.6f, .08f, .5f), f.r); z.mk.Prism(dark, cn + up * .98f - f.Right * .7f, up, .22f, .014f, 16); z.mk.Prism(SongGlow(ss, Rose, 1.4f), cn + up * .995f - f.Right * .7f, up, .07f, .012f, 10); z.mk.Box(iron, cn + up * 1f + f.Right * .4f, new Vector3(.8f, .2f, .4f), f.r); for (var k = 0; k < 3; k++) z.mk.Blob(lampM, cn + up * 1.02f + f.Right * (.16f + k * .24f) + f.Fwd * .21f, Vector3.one * .03f, rng, 0f);
            foreach (var e in new[] { -1f, 1f }) { var sp = cn + f.Right * e * 2.5f; z.mk.Box(woodM, sp + up * .7f, new Vector3(.8f, 1.4f, .6f), f.r); BoxCol(z.root, sp + up * .7f, new Vector3(.8f, 1.4f, .6f), f.r); z.mk.Prism(dark, sp + up * .5f + f.Fwd * .3f, f.Fwd, .28f, .03f, 14); z.mk.Prism(dark, sp + up * 1.1f + f.Fwd * .3f, f.Fwd, .13f, .03f, 12); }
            string[] drop = { "The needle finds the groove. A bass line walks in like it lives here, and the room exhales.", "You turn it up one notch. The candles lean toward the speakers.", "Side two. The falsetto goes up and takes the ceiling with it.", "Somebody on the sofa says \"mm\" and nothing else. That's the review." }; var dn = 0;
            Spot("The turntable", cn + up * 1.3f + f.Fwd * .5f, "Drop the needle", new Vector3(3.2f, 1f, 1.2f)).onUse = _ => { life.played = Time.time; Say1(cn + up * 2.4f + f.Fwd * .6f, drop[dn++ % drop.Length]); };
            // a rug, a low table of candles, two sofas facing each other
            z.mk.Prism(clothM, f.P(0, .07f, .7f), up, 2.3f, .012f, 20); z.mk.Prism(woodM, f.P(0, .09f, .7f), up, .6f, .3f, 10); BoxCol(z.root, f.P(0, .24f, .7f), new Vector3(1f, .36f, 1f), f.r); Candles(z.mk, f.P(0, .39f, .7f), 6, rng);
            Sofa(z.root, z.mk, f.P(-3.3f, .06f, .7f), f.Right, clothM, 2.6f); Sofa(z.root, z.mk, f.P(3.3f, .06f, .9f), -f.Right, Paint(new Color(.3f, .42f, .34f)), 2.6f);
            Folk(f.P(1.2f, .06f, -Dp / 2f + 1.7f), f.Fwd, Resident.Pose.Stand, new[] { "House rule: you can talk, but only between songs. And there's no between.", "Take a sofa. I've got a whole wall to get through and I'm on the second shelf.", "Low light, good speakers, somebody you like. That's the entire recipe." }, Local(Residents.Tool.Mug));
            // "This Woman's Work": a lamp left on beside an empty chair, a blanket folded over its arm
            var ch = f.P(4.3f, .06f, -2.2f); Chair(z.mk, ch, (f.Fwd - f.Right * .6f).normalized); z.mk.Box(Paint(Cream), ch + up * .52f + f.Right * .1f, new Vector3(.4f, .08f, .36f), f.r); var lp = f.P(4.7f, .06f, -3.1f); z.mk.Prism(iron, lp, up, .025f, 1.6f, 5); z.mk.Prism(iron, lp, up, .16f, .03f, 8); z.mk.Prism(SongGlow(tw, new Color(1f, .8f, .5f), 1.6f), lp + up * 1.5f, up, .2f, .3f, 8, .6f);
            Shot(z.ctx, "the listening room", z.At(11f, 5f) + up * 2.2f, f.P(0, 2.4f, 0));
            Shot(z.ctx, "inside the listening room", f.P(.6f, 1.7f, Dp / 2f - .9f), f.P(-.4f, 1.3f, -Dp / 2f));
        }

        // ---------------------------------------------------------------- Ms. Lauryn Hill: the schoolroom
        void LaurynHill(Plot z)
        {
            var up = Vector3.up; var rot = z.Rot; var rng = new System.Random(98); var dw = Song(z, "Doo Wop"); var ex = Song(z, "Ex-Factor"); var nm = Song(z, "Nothing Even Matters");
            // the blackboard, out in the open air
            var bb = z.At(-5.6f, 0) + up * .08f; foreach (var e in new[] { -2f, 2f }) { z.mk.Prism(woodM, bb + z.side * e, up, .07f, 2.9f, 5); ArtistBits.PostCollider(z.root, bb + z.side * e, 2.8f, .09f); }
            z.mk.Box(woodM, bb + up * 1.9f, new Vector3(4.2f, 2f, .08f), rot); z.mk.Box(Paint(new Color(.1f, .24f, .18f)), bb + up * 1.9f + z.front * .05f, new Vector3(3.9f, 1.7f, .04f), rot); z.mk.Box(woodM, bb + up * 1f + z.front * .12f, new Vector3(3.9f, .05f, .16f), rot); BoxCol(z.root, bb + up * 1.6f, new Vector3(4.2f, 2.6f, .2f), rot);
            ArtistBits.Label(z.labels, "L  O  V  E", bb + up * 2f + z.front * .08f, z.front, .5f, Cream, TextAnchor.MiddleCenter); ArtistBits.Label(z.labels, "today's lesson:", bb + up * 2.52f + z.front * .08f - z.side * 1f, z.front, .14f, Cream, TextAnchor.MiddleCenter); ArtistBits.Label(z.labels, "(discuss.)", bb + up * 1.36f + z.front * .08f + z.side * 1.2f, z.front, .12f, Cream, TextAnchor.MiddleCenter);
            for (var k = 0; k < 3; k++) z.mk.Box(white, bb + up * 1.04f + z.front * .13f + z.side * (-1f + k * .3f), new Vector3(.1f, .025f, .025f), rot * Quaternion.Euler(0, k * 25f, 0));
            z.area.glowLight = ArtistBits.PointLight(z.root, bb + up * 3.4f + z.front * 3f, new Color(1f, .8f, .52f), 22f, 2.4f);
            // desks to sit at, and an apple on the front one
            for (var row = 0; row < 2; row++) for (var col = 0; col < 3; col++)
            {
                var d = z.At(-2.6f + row * 2.3f, -2.3f + col * 2.3f) + up * .08f; z.mk.Box(woodM, d + up * .72f, new Vector3(.9f, .05f, .55f), rot * Quaternion.Euler(-6f, 0, 0)); foreach (var e in new[] { -.4f, .4f }) z.mk.Box(iron, d + up * .36f + z.side * e, new Vector3(.04f, .72f, .5f), rot); z.mk.Box(woodM, d + up * .5f, new Vector3(.8f, .04f, .4f), rot); BoxCol(z.root, d + up * .38f, new Vector3(.9f, .76f, .55f), rot);
                Chair(z.mk, d + z.front * .7f, -z.front);
                if (row == 1 && col == 0) Folk(d + z.front * .7f + up * .04f, -z.front, Resident.Pose.Sit, new[] { "I already know the answer. I'm waiting to see if anybody else does.", "She said how you gonna win when you ain't right within. I wrote it on my hand.", "This is the only class I never skipped." }, null, null, .5f);
            }
            var ad = z.At(-2.6f, 0) + up * .84f; var am = new MeshKit(); am.Blob(Paint(new Color(.8f, .16f, .14f)), new Vector3(0, .06f, 0), Vector3.one * .06f, rng, .08f); am.Prism(woodM, new Vector3(0, .1f, 0), Vector3.up, .008f, .05f, 3); Thing(am, "Apple", "an apple for the teacher", ad, Quaternion.identity, new Vector3(.14f, .14f, .14f), .15f, false);
            Folk(bb + z.front * 1.1f - z.side * 2.6f, (z.front + z.side * .3f).normalized, Resident.Pose.Stand, new[] { "Spell it. Now tell me what it means. No, not the dictionary. You.", "Everything is everything. That's not homework, it's just true.", "Sit anywhere. The ones at the back think I can't see them." });
            // "Doo Wop (That Thing)": a street-corner lamp with three microphones under it
            var sl = z.At(4.6f, -6.2f); z.mk.Prism(iron, sl - up * .2f, up, .08f, 4.2f, 6); z.mk.Prism(iron, sl + up * 3.9f, up, .2f, .08f, 6); z.mk.Blob(SongGlow(dw, Honey, 1.8f), sl + up * 4.2f, Vector3.one * .24f, rng, 0f); halos.Add(sl + up * 4.2f, 2f, Honey, .5f); ArtistBits.PostCollider(z.root, sl, 4f, .1f);
            for (var k = 0; k < 3; k++) { var mp = sl + Quaternion.Euler(0, k * 50f - 50f, 0) * z.front * 1.3f; z.mk.Prism(iron, mp, up, .016f, 1.5f, 4); z.mk.Prism(iron, mp, up, .14f, .02f, 8); z.mk.Blob(white, mp + up * 1.56f, new Vector3(.05f, .08f, .05f), rng, 0f); }
            // "Ex-Factor": a rope between two posts, pulled tight, a knot in the middle that won't come out
            var ea = z.At(5f, 4.4f); var eb = z.At(3.4f, 7.4f); foreach (var g in new[] { ea, eb }) { z.mk.Prism(woodM, g - up * .2f, up, .07f, 1.5f, 5); ArtistBits.PostCollider(z.root, g, 1.3f, .09f); } z.mk.Prism(Paint(new Color(.8f, .7f, .5f)), ea + up * 1.1f, (eb - ea).normalized, .025f, (eb - ea).magnitude, 5); z.mk.Blob(SongGlow(ex, Rose, 1.6f), (ea + eb) / 2f + up * 1.1f, Vector3.one * .12f, rng, .3f); halos.Add((ea + eb) / 2f + up * 1.1f, .9f, Rose, .4f);
            // "Nothing Even Matters": two chairs facing each other over one lantern, their backs to everything else
            var n0 = z.At(-1f, 7.4f) + up * .08f; Chair(z.mk, n0 - z.front * .7f, z.front); Chair(z.mk, n0 + z.front * .7f, -z.front); z.mk.Prism(iron, n0, up, .1f, .03f, 6); z.mk.Prism(SongGlow(nm, Honey, 1.8f), n0 + up * .03f, up, .08f, .2f, 6); z.mk.Cone(iron, n0 + up * .23f, .11f, .08f, 6); halos.Add(n0 + up * .14f, 1.1f, Honey, .5f);
            Shot(z.ctx, "the schoolroom", z.At(8f, 4f) + up * 2.2f, bb + up * 1.8f);
        }

        // ---------------------------------------------------------------- Erykah Badu: the wheel
        void Badu(Plot z)
        {
            var up = Vector3.up; var rot = z.Rot; var rng = new System.Random(97); var on = Song(z, "On & On"); var dc = Song(z, "Didn't Cha Know"); life.onSong = on;
            // the wheel: it keeps on turning
            var foot = z.At(-4.8f, 0); var hub = foot + up * 3.7f; const float R = 3f;
            foreach (var e in new[] { -1f, 1f }) foreach (var s in new[] { -1f, 1f }) { var b = foot + z.front * e * .7f + z.side * s * 2f; z.mk.Prism(woodM, b - up * .2f, (hub + z.front * e * .7f - b + up * .2f).normalized, .1f, (hub + z.front * e * .7f - b).magnitude + .2f, 5); ArtistBits.PostCollider(z.root, b, 1.4f, .14f); }
            z.mk.Prism(iron, hub - z.front * .8f, z.front, .1f, 1.6f, 6);
            var wk = new MeshKit(); var rim = SongGlow(on, Honey, 1.2f); var spoke = Paint(new Color(.5f, .3f, .22f));
            for (var k = 0; k < 16; k++) { var a = Quaternion.Euler(0, 0, k * 22.5f); wk.Box(Paint(Walls[k % Walls.Length]), a * Vector3.up * R, new Vector3(2f * R * Mathf.Tan(11.25f * Mathf.Deg2Rad) + .04f, .16f, .36f), a); if (k % 2 == 0) { wk.Prism(spoke, Vector3.zero, a * Vector3.up, .05f, R, 4); wk.Blob(rim, a * Vector3.up * (R + .22f), Vector3.one * .13f, rng, 0f); } }
            wk.Prism(spoke, new Vector3(0, 0, -.24f), Vector3.forward, .34f, .48f, 8);
            var go = wk.BuildSingle("The wheel", z.root, false); go.transform.SetPositionAndRotation(hub, rot); life.wheel = go.transform; BoxCol(z.root, foot + up * .5f, new Vector3(4.4f, 1f, 1.6f), rot); halos.Add(hub + z.front * .6f, 3.4f, Honey, .16f);
            z.area.glowLight = ArtistBits.PointLight(z.root, hub + z.front * 3f, new Color(1f, .8f, .52f), 22f, 2.4f);
            // "Didn't Cha Know": a signpost pointing every way at once
            var sp = z.At(4.2f, -6.4f); z.mk.Prism(woodM, sp - up * .2f, up, .08f, 3.6f, 5); ArtistBits.PostCollider(z.root, sp, 3.4f, .1f); z.mk.Blob(SongGlow(dc, Honey, 1.6f), sp + up * 3.52f, Vector3.one * .14f, rng, 0f);
            string[] ways = { "this way", "or this way", "not that way", "back again", "left, I think" };
            for (var k = 0; k < 5; k++) { var d = Quaternion.Euler(0, k * 77f + 20f, 0) * z.front; var ar = Quaternion.LookRotation(Vector3.Cross(d, up)); var at = sp + up * (3.1f - k * .42f) + d * .7f; z.mk.Box(Paint(Walls[(k + 1) % Walls.Length]), at, new Vector3(1.3f, .26f, .05f), ar); z.mk.Box(Paint(Walls[(k + 1) % Walls.Length]), at + d * .72f, new Vector3(.2f, .2f, .05f), ar * Quaternion.Euler(0, 0, 45f)); ArtistBits.Label(z.labels, ways[k], at + ar * Vector3.forward * .035f, ar * Vector3.forward, .11f, Cream, TextAnchor.MiddleCenter); ArtistBits.Label(z.labels, ways[k], at - ar * Vector3.forward * .035f, -(ar * Vector3.forward), .11f, Cream, TextAnchor.MiddleCenter); }
            // tea and incense on a low table, cushions round it
            var tt = z.At(3.4f, 5.6f) + up * .08f; z.mk.Prism(clothM, tt, up, 2.2f, .012f, 18); z.mk.Prism(woodM, tt + up * .02f, up, .6f, .3f, 10); BoxCol(z.root, tt + up * .18f, new Vector3(1f, .36f, 1f), rot);
            z.mk.Prism(pot, tt + up * .32f + z.side * .2f, up, .12f, .16f, 8, 1.2f); for (var k = 0; k < 2; k++) z.mk.Prism(white, tt + up * .32f - z.side * (.1f + k * .2f) + z.front * (k * .2f - .1f), up, .04f, .06f, 6); var inc = tt + up * .32f - z.front * .3f; z.mk.Prism(pot, inc, up, .06f, .03f, 6); z.mk.Prism(dark, inc, (up + z.side * .2f).normalized, .006f, .34f, 3); life.smokes.Add(inc + up * .4f);
            for (var k = 0; k < 4; k++) { var d = Quaternion.Euler(0, k * 90f + 30f, 0) * z.front; Cushion(z.mk, tt + d * 1.5f, -d, k + 1); }
            Folk(tt + Quaternion.Euler(0, 30f, 0) * z.front * 1.5f + up * .22f, -(Quaternion.Euler(0, 30f, 0) * z.front), Resident.Pose.Sit, new[] { "The wheel? Nobody pushes it. It was turning when we got here.", "I made a wrong turn once and ended up here. Best wrong turn I ever made.", "Tea's ready when the incense burns down. That's how we tell time." }, Local(Residents.Tool.Mug), null, .26f);
            var s1 = z.At(-1f, -6.6f); SnakePlant(z.ctx, z.mk, s1, .8f, rng); Candles(z.mk, z.At(-1.6f, 3.4f) + up * .08f, 6, rng);
            Shot(z.ctx, "the wheel", z.At(9f, 3f) + up * 2.2f, hub);
        }

        // ---------------------------------------------------------------- D'Angelo: one spotlight
        void DAngelo(Plot z)
        {
            var up = Vector3.up; var rot = z.Rot; var rng = new System.Random(2000); var un = Song(z, "Untitled"); life.untitledSong = un;
            var st = z.At(-1.6f, 0) + up * .08f; z.mk.Prism(dark, st, up, 3.2f, .3f, 24); z.solid.Prism(dark, st - up * .3f, up, 3.2f, .6f, 12); z.mk.Prism(SongGlow(un, new Color(1f, .7f, .36f), .5f), st + up * .3f, up, 1.7f, .012f, 24);
            // one lamp on a tall stand, leaning in over the stage
            var ls = st - z.front * 3.3f - z.side * 2.2f; var head = st + up * 5.2f - z.front * .6f; z.mk.Prism(iron, ls - up * .2f, up, .07f, 5.6f, 5); ArtistBits.PostCollider(z.root, ls, 5.2f, .1f); var arm = head - (ls + up * 5.4f); z.mk.Prism(iron, ls + up * 5.4f, arm.normalized, .05f, arm.magnitude, 4); z.mk.Prism(iron, head + up * .3f, -up, .2f, .5f, 8, 1.8f); z.mk.Prism(SongGlow(un, new Color(1f, .82f, .5f), 2.2f), head - up * .22f, -up, .3f, .03f, 10);
            var sp = ArtistBits.PointLight(z.root, st + up * 3.6f, new Color(1f, .78f, .46f), 11f, 2.8f); z.area.glowLight = sp; life.spot = sp; halos.Add(st + up * 1.6f, 2.6f, new Color(1f, .78f, .46f), .2f);
            // an electric piano to play, a stool, a microphone
            var ep = st + up * .3f - z.front * .5f; z.mk.Box(Paint(new Color(.2f, .14f, .14f)), ep + up * .8f, new Vector3(1.5f, .16f, .5f), rot); z.mk.Box(white, ep + up * .86f + z.front * .13f, new Vector3(1.4f, .05f, .2f), rot); z.mk.Box(Paint(new Color(.5f, .12f, .14f)), ep + up * .92f - z.front * .12f, new Vector3(1.5f, .08f, .24f), rot); foreach (var e in new[] { -.65f, .65f }) z.mk.Prism(iron, ep + z.side * e, up, .025f, .74f, 4); BoxCol(z.root, ep + up * .5f, new Vector3(1.5f, 1f, .5f), rot);
            var sg = ep + z.front * .8f; z.mk.Prism(Paint(new Color(.4f, .16f, .2f)), sg + up * .44f, up, .2f, .08f, 10); z.mk.Prism(iron, sg, up, .03f, .44f, 5); Interact.Seat(tlife.transform, sg + up * .54f, -z.front, "Sit at the keys").seatHeight = .5f;
            var mc = ep + z.side * 1.1f + z.front * .5f; z.mk.Prism(iron, mc, up, .018f, 1.5f, 4); z.mk.Prism(iron, mc, up, .16f, .02f, 8); z.mk.Blob(white, mc + up * 1.56f, new Vector3(.05f, .08f, .05f), rng, 0f);
            string[] chords = { "One chord, laid back so far behind the beat it's nearly the next beat. It's perfect there.", "The keys are warm and a little out of true. You play it slower. Slower than that.", "You hold the note. Nobody claps. Nobody breathes either.", "A minor ninth hangs under the lamp like smoke." }; var cn = 0;
            Spot("The electric piano", ep + up * 1.2f + z.front * .4f, "Play a slow chord", new Vector3(1.5f, .9f, 1f)).onUse = _ => { life.played = Time.time; Say1(ep + up * 2.4f, chords[cn++ % chords.Length]); };
            Candles(z.mk, st + z.front * 3.5f + z.side * 1.6f, 5, rng); Candles(z.mk, st + z.front * 3.3f - z.side * 2f, 4, rng);
            Shot(z.ctx, "one spotlight", z.At(8f, 3f) + up * 2f, st + up * 1.6f);
        }

        // ---------------------------------------------------------------- Solange: cranes in the sky
        void Solange(Plot z)
        {
            var up = Vector3.up; var rot = z.Rot; var rng = new System.Random(2016); var cr = Song(z, "Cranes"); var al = Song(z, "Almeda"); var ly = Song(z, "Losing You"); life.cranesSong = cr;
            var paper = SongGlow(cr, new Color(.9f, .95f, 1f), 1.2f); var steel = white;
            // three white cranes, a paper crane or three hanging from each
            for (var n = 0; n < 3; n++)
            {
                var b = z.At(-4.4f + n * .6f, -4.2f + n * 4f); var h = new[] { 7.5f, 10f, 6.2f }[n]; var jd = Quaternion.Euler(0, new[] { 40f, -20f, -70f }[n], 0) * z.front; var jl = 4.4f + n * .5f;
                foreach (var (x, zz) in new[] { (-.22f, -.22f), (.22f, -.22f), (.22f, .22f), (-.22f, .22f) }) z.mk.Prism(steel, b + new Vector3(x, -.2f, zz), up, .035f, h + .2f, 4); for (var y = .6f; y < h; y += .9f) { z.mk.Box(steel, b + up * y, new Vector3(.48f, .04f, .04f), Quaternion.Euler(0, 0, 35f)); z.mk.Box(steel, b + up * y, new Vector3(.04f, .04f, .48f), Quaternion.Euler(35f, 0, 0)); }
                ArtistBits.PostCollider(z.root, b, h, .34f); var top = b + up * h; z.mk.Prism(steel, top - jd * 1.4f, jd, .06f, jl + 1.4f, 4); z.mk.Prism(steel, top - jd * 1.4f + up * .5f, jd, .04f, jl + 1.4f, 3); z.mk.Prism(steel, top, up, .05f, 1.2f, 4); z.mk.Prism(steel, top + up * 1.2f, (jd * jl - up * .7f).normalized, .015f, new Vector3(jl, .7f, 0).magnitude, 3); z.mk.Box(Paint(new Color(.5f, .5f, .52f)), top - jd * 1.2f - up * .3f, new Vector3(.5f, .5f, .5f), Quaternion.LookRotation(jd));
                for (var k = 0; k < 3; k++) { var hp = top + jd * (1.6f + k * 1.3f); var drop = 1.2f + ((k + n) % 3) * .9f; z.mk.Prism(dark, hp, -up, .006f, drop, 3); PaperCrane(z.mk, paper, hp - up * (drop + .1f), Quaternion.Euler(0, k * 70f + n * 40f, 0) * z.side, .34f); }
            }
            z.area.glowLight = ArtistBits.PointLight(z.root, z.At(-3f, 0) + up * 5f, new Color(.9f, .9f, 1f), 22f, 2.4f); halos.Add(z.At(-3.8f, 0) + up * 7f, 4f, new Color(.8f, .9f, 1f), .14f);
            // round white steps to sit on
            var ps = z.At(.6f, .2f) + up * .08f; z.mk.Prism(white, ps, up, 1.9f, .4f, 22); z.mk.Prism(white, ps + up * .4f, up, 1.1f, .4f, 18); z.solid.Prism(white, ps - up * .3f, up, 1.9f, .7f, 12); z.solid.Prism(white, ps, up, 1.1f, .8f, 10); for (var k = 0; k < 3; k++) { var d = Quaternion.Euler(0, k * 70f - 70f, 0) * z.front; Interact.Seat(tlife.transform, ps + d * 1.5f + up * .5f, d, "Sit on the step").seatHeight = .42f; }
            // "Almeda": a shelf of brown bottles, brown jars, brown everything, glowing like honey
            var sh = z.At(3.6f, -6f) + up * .08f; var sr = rot * Quaternion.Euler(0, 30f, 0); var brown = SongGlow(al, new Color(.7f, .38f, .14f), .9f); foreach (var e in new[] { -1.1f, 1.1f }) z.mk.Box(woodM, sh + up * 1f + sr * Vector3.right * e, new Vector3(.06f, 2f, .34f), sr); for (var row = 0; row < 3; row++) { z.mk.Box(woodM, sh + up * (.5f + row * .7f), new Vector3(2.2f, .05f, .34f), sr); for (var k = 0; k < 7; k++) { var hh = .26f + ((k * 3 + row) % 4) * .07f; var bp = sh + up * (.53f + row * .7f) + sr * Vector3.right * (-.9f + k * .3f); z.mk.Prism(brown, bp, up, .07f + (k % 2) * .03f, hh, 7); z.mk.Prism(brown, bp + up * hh, up, .03f, .1f, 5); } } BoxCol(z.root, sh + up * 1f, new Vector3(2.3f, 2f, .4f), sr);
            // "Losing You": bright cloth on a line
            var la = z.At(4.6f, 4.4f); var lb = z.At(2.4f, 7.6f); foreach (var g in new[] { la, lb }) { z.mk.Prism(woodM, g - up * .2f, up, .05f, 2.5f, 4); ArtistBits.PostCollider(z.root, g, 2.3f, .07f); } var ld = (lb - la).normalized; z.mk.Prism(dark, la + up * 2.2f, ld, .01f, (lb - la).magnitude, 3);
            var bright = new[] { new Color(.95f, .5f, .2f), new Color(.2f, .6f, .8f), new Color(.95f, .8f, .2f), new Color(.85f, .25f, .45f), new Color(.3f, .7f, .45f) }; for (var k = 0; k < 5; k++) { var at = Vector3.Lerp(la, lb, .14f + k * .18f) + up * 1.6f; z.mk.Box(k == 2 ? SongGlow(ly, bright[k], .7f) : Paint(bright[k]), at, new Vector3(.04f, 1.2f, .62f), Quaternion.LookRotation(ld)); for (var q = 0; q < 3; q++) z.mk.Box(Paint(bright[(k + 2) % 5]), at + up * (-.4f + q * .4f), new Vector3(.06f, .1f, .62f), Quaternion.LookRotation(ld)); }
            Folk(ps + z.front * 2.6f - z.side * 1.6f, (z.front - z.side * .3f).normalized, Resident.Pose.Stand, new[] { "They're building something. Nobody's said what. I like it better not knowing.", "I tried to fold one. Mine looks like a boat. Hers fly.", "Sit on the white steps. It makes you feel like you're in something." });
            Shot(z.ctx, "the cranes", z.At(9f, 3.4f) + up * 2f, z.At(-3.8f, 0) + up * 5.6f);
        }

        /// A paper crane: a folded body, two wings, a neck and a tail.
        void PaperCrane(MeshKit mk, Material m, Vector3 c, Vector3 fwd, float s)
        {
            var up = Vector3.up; var rt = Vector3.Cross(up, fwd); Vector3 n = c + fwd * s, t = c - fwd * s;
            foreach (var e in new[] { -1f, 1f }) { var tip = c + rt * e * s * 1.1f + up * s * .6f; mk.Tri(m, n * .5f + c * .5f, t * .5f + c * .5f, tip, c - up); mk.Tri(m, n * .5f + c * .5f, t * .5f + c * .5f, tip, c + up * 3f); }
            mk.Prism(m, c - up * s * .1f, (fwd + up * .9f).normalized, s * .09f, s * 1.1f, 3, .3f); mk.Prism(m, c - up * s * .1f, (-fwd + up * .7f).normalized, s * .09f, s * .9f, 3, .2f); mk.Blob(m, c - up * s * .05f, new Vector3(s * .16f, s * .16f, s * .5f), new System.Random(3), 0f);
        }

        // ---------------------------------------------------------------- the others: one thing from their song
        void Others(Plot z, PlaceFrame f)
        {
            var up = Vector3.up; var rot = z.Rot; var rng = new System.Random(z.a.name.Length * 7);
            switch (z.a.name)
            {
                case "Jill Scott":
                {
                    // "Golden": a gold sun rising over the wall
                    var s = Song(z, "Golden"); life.goldenSong = s; var gold = SongGlow(s, new Color(1f, .78f, .2f), 1.8f); var sc = f.P(.6f, 4.3f, 0); z.mk.Prism(woodM, f.P(.6f, 2.9f, 0), up, .05f, 1.4f, 4); z.mk.Prism(gold, sc - f.Fwd * .06f, f.Fwd, .7f, .12f, 18);
                    for (var k = 0; k < 12; k++) { var a = Quaternion.AngleAxis(k * 30f, f.Fwd); z.mk.Box(gold, sc + a * up * (1f + (k % 2) * .12f), new Vector3(.1f, .4f + (k % 2) * .24f, .06f), a * f.r); } halos.Add(sc + f.Fwd * .5f, 2.6f, new Color(1f, .8f, .3f), .4f);
                    break;
                }
                case "Jorja Smith":
                {
                    // "Blue Lights": one blue lamp, turning the whole nook blue
                    var s = Song(z, "Blue Lights"); life.blueSong = s; var bl = new Color(.25f, .5f, 1f); var p = z.At(1.6f, 2.6f); z.mk.Prism(iron, p - up * .2f, up, .06f, 3.6f, 5); z.mk.Prism(iron, p + up * 3.4f, up, .16f, .06f, 6); z.mk.Blob(SongGlow(s, bl, 2.2f), p + up * 3.66f, Vector3.one * .22f, rng, 0f); z.mk.Prism(SongGlow(s, new Color(.1f, .25f, .8f), .3f), z.At(1.2f, 0) + up * .09f, up, 2.2f, .012f, 20);
                    halos.Add(p + up * 3.66f, 2.4f, bl, .6f); ArtistBits.PostCollider(z.root, p, 3.4f, .09f); z.area.glowLight = ArtistBits.PointLight(z.root, p + up * 3f + z.front * .5f, bl, 14f, 2.6f);
                    break;
                }
                case "Kali Uchis":
                {
                    // "telepatía": a tin-can telephone, the string glowing
                    var s = Song(z, "telepat"); var pink = SongGlow(s, new Color(1f, .5f, .7f), 1.6f); var a = z.At(1.4f, -2.8f) + up * 1.3f; var b = z.At(1.4f, 2.8f) + up * 1.3f; var d = (b - a).normalized;
                    foreach (var (g, e) in new[] { (a, 1f), (b, -1f) }) { z.mk.Prism(woodM, g - up * 1.5f, up, .05f, 1.4f, 4); ArtistBits.PostCollider(z.root, g - up * 1.3f, 1.3f, .08f); z.mk.Prism(Paint(new Color(.7f, .72f, .76f)), g - d * e * .14f, d * e, .09f, .24f, 8); }
                    var prev = a + d * .1f; for (var k = 1; k <= 10; k++) { var t = k / 10f; var q = Vector3.Lerp(a + d * .1f, b - d * .1f, t) - up * Mathf.Sin(t * Mathf.PI) * .3f; z.mk.Prism(pink, prev, (q - prev).normalized, .012f, Vector3.Distance(prev, q), 3); prev = q; } halos.Add((a + b) / 2f - up * .3f, 1.2f, new Color(1f, .5f, .7f), .3f);
                    break;
                }
                case "Hiatus Kaiyote":
                {
                    // "Red Room": the frame of a room, all red, a chair in it
                    var s = Song(z, "Red Room"); life.redSong = s; var redM = SongGlow(s, new Color(1f, .14f, .12f), 1.4f); var c = z.At(1.5f, 0) + up * .08f;
                    foreach (var (x, zz) in new[] { (-1.3f, -1.3f), (1.3f, -1.3f), (1.3f, 1.3f), (-1.3f, 1.3f) }) { var p = c + rot * new Vector3(x, 0, zz); z.mk.Box(redM, p + up * 1.25f, new Vector3(.08f, 2.5f, .08f), rot); ArtistBits.PostCollider(z.root, p, 2.5f, .08f); }
                    foreach (var e in new[] { -1.3f, 1.3f }) { z.mk.Box(redM, c + up * 2.5f + z.side * e, new Vector3(.08f, .08f, 2.68f), rot); z.mk.Box(redM, c + up * 2.5f + z.front * e, new Vector3(2.68f, .08f, .08f), rot); }
                    z.mk.Box(SongGlow(s, new Color(.8f, .1f, .1f), .5f), c + up * .012f, new Vector3(2.6f, .02f, 2.6f), rot); Chair(z.mk, c + up * .02f, z.front); halos.Add(c + up * 1.3f, 2.2f, new Color(1f, .15f, .12f), .26f); z.area.glowLight = ArtistBits.PointLight(z.root, c + up * 2.2f, new Color(1f, .3f, .25f), 12f, 2.4f);
                    break;
                }
                case "SAULT":
                {
                    // a black cube with nothing written on it but a question; "Wildfires": a ring of flame round it
                    var s = Song(z, "Wildfires"); var why = Song(z, "Why Why"); life.wildSong = s; var c = z.At(1.6f, 0) + up * .08f; life.wildAt = c; z.mk.Box(dark, c + up * .95f, new Vector3(1.9f, 1.9f, 1.9f), rot); z.solid.Box(dark, c + up * .95f, new Vector3(1.9f, 1.9f, 1.9f), rot);
                    z.mk.Box(SongGlow(why, Cream, .5f), c + up * .5f + z.front * .96f, new Vector3(1.5f, .22f, .02f), rot); ArtistBits.Label(z.labels, "why why why why why", c + up * .5f + z.front * .98f, z.front, .11f, Ink, TextAnchor.MiddleCenter);
                    var fl = SongGlow(s, new Color(1f, .36f, .06f), 1f); for (var k = 0; k < 10; k++) { var p = c + Quaternion.Euler(0, k * 36f, 0) * z.front * 2.2f; z.mk.Cone(fl, p, .16f + (k % 3) * .04f, .5f + (k % 3) * .22f, 5, k * 20f); } halos.Add(c + up * .4f, 3f, new Color(1f, .5f, .15f), .2f);
                    break;
                }
                case "Children of Zeus":
                {
                    // "Hard Work": a workbench, a half-made stool, tools laid out straight
                    var s = Song(z, "Hard Work"); var c = z.At(1.3f, .4f) + up * .08f; z.mk.Box(woodM, c + up * .86f, new Vector3(2.2f, .1f, .8f), rot); foreach (var (x, zz) in new[] { (-1f, -.3f), (1f, -.3f), (1f, .3f), (-1f, .3f) }) z.mk.Box(woodM, c + up * .42f + rot * new Vector3(x, 0, zz), new Vector3(.09f, .84f, .09f), rot); z.mk.Box(woodM, c + up * .2f, new Vector3(2f, .05f, .6f), rot); BoxCol(z.root, c + up * .46f, new Vector3(2.2f, .92f, .8f), rot);
                    z.mk.Prism(woodM, c + up * .93f - z.side * .7f, z.side, .02f, .34f, 5); z.mk.Box(iron, c + up * .94f - z.side * .34f, new Vector3(.08f, .07f, .2f), rot); z.mk.Box(iron, c + up * .92f + z.side * .2f, new Vector3(.5f, .012f, .12f), rot * Quaternion.Euler(0, 12f, 0)); z.mk.Box(woodM, c + up * .93f + z.side * .5f + z.front * .04f, new Vector3(.14f, .04f, .1f), rot * Quaternion.Euler(0, 12f, 0)); z.mk.Blob(SongGlow(s, Honey, 1.6f), c + up * 2.1f, Vector3.one * .12f, rng, 0f); z.mk.Prism(dark, c + up * 2.2f, up, .008f, 1f, 3); z.mk.Prism(iron, c - z.front * .5f + z.side * 1.2f, up, .03f, 3.2f, 4); z.mk.Prism(iron, c - z.front * .5f + z.side * 1.2f + up * 3.2f, (-z.side * 1.2f + z.front * .5f).normalized, .02f, 1.3f, 4); halos.Add(c + up * 2.1f, 1.2f, Honey, .5f);
                    z.mk.Prism(woodM, c + z.front * 1.3f - z.side * .8f + up * .4f, up, .2f, .05f, 8); for (var k = 0; k < 2; k++) z.mk.Prism(woodM, c + z.front * 1.3f - z.side * .8f + Quaternion.Euler(0, k * 120f, 0) * Vector3.forward * .13f, up, .025f, .4f, 4);
                    string[] work = { "You plane one curl off the leg. It's a good curl. There are four hundred more in there.", "Measure twice. Cut once. Sand until the record needs turning.", "Nothing here was made quickly, and you can tell, and that's the point." }; var wn = 0;
                    Spot("The workbench", c + up * 1.3f + z.front * .5f, "Put the work in", new Vector3(2.2f, .9f, 1.1f)).onUse = _ => { life.played = Time.time; Say1(c + up * 2.6f, work[wn++ % work.Length]); };
                    break;
                }
                case "KIRBY":
                {
                    // "Saving Grace": a street lamp in the dark and a web strung under it, every thread lit
                    var s = Song(z, "Saving Grace"); var silk = SongGlow(s, new Color(.9f, .95f, 1f), 1.3f); var a = z.At(1.4f, -1.9f); var b = z.At(1.4f, 1.9f); z.mk.Prism(iron, a - up * .2f, up, .07f, 4f, 6); z.mk.Prism(iron, a + up * 3.8f, z.side, .04f, .9f, 4); z.mk.Prism(lampM, a + up * 3.62f + z.side * .9f, up, .16f, .22f, 6, .6f); halos.Add(a + up * 3.6f + z.side * .9f, 1.8f, Honey, .5f); ArtistBits.PostCollider(z.root, a, 3.8f, .09f); z.mk.Prism(iron, b - up * .2f, up, .05f, 3.2f, 5); ArtistBits.PostCollider(z.root, b, 3f, .07f);
                    var wc = (a + b) / 2f + up * 2f; for (var k = 0; k < 8; k++) { var d = Quaternion.AngleAxis(k * 45f + 10f, z.front) * up; z.mk.Prism(silk, wc, d, .008f, 1.7f, 3); for (var q = 1; q <= 4; q++) { var p0 = wc + d * (q * .4f); var p1 = wc + Quaternion.AngleAxis(45f, z.front) * d * (q * .4f); z.mk.Prism(silk, p0, (p1 - p0).normalized, .006f, Vector3.Distance(p0, p1), 3); } }
                    break;
                }
                case "Victoria Monét":
                {
                    // "On My Mama": a tall mirror to stand in front of, and the family photographs along the wall
                    var s = Song(z, "On My Mama"); var c = z.At(.9f, -1.6f) + up * .08f; var mr = rot * Quaternion.Euler(0, 24f, 0); z.mk.Box(Paint(Honey), c + up * 1.2f, new Vector3(1.3f, 2.4f, .08f), mr); z.mk.Box(SongGlow(s, new Color(.7f, .9f, 1f), .7f), c + up * 1.2f + mr * Vector3.forward * .05f, new Vector3(1.1f, 2.2f, .03f), mr); z.mk.Box(woodM, c + up * .6f - mr * Vector3.forward * .4f, new Vector3(.06f, 1.3f, .06f), mr * Quaternion.Euler(-30f, 0, 0)); BoxCol(z.root, c + up * 1.2f, new Vector3(1.3f, 2.4f, .3f), mr);
                    for (var k = 0; k < 5; k++) { var at = f.P(1f + (k % 3) * .62f, 1.2f + (k / 3) * .7f + (k % 2) * .1f, .27f); z.mk.Box(Paint(Honey), at, new Vector3(.46f, .56f, .03f), f.r); z.mk.Box(Paint(new Color(.5f + k * .06f, .4f, .36f)), at + f.Fwd * .035f, new Vector3(.36f, .46f, .02f), f.r); }
                    z.mk.Prism(Paint(Rose), c + mr * Vector3.forward * 1.5f, up, .8f, .03f, 16);
                    break;
                }
            }
        }
    }
}
