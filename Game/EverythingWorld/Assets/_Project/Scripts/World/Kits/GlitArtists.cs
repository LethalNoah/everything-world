using System.Collections.Generic;
using UnityEngine;
using static EverythingWorld.World.PlaceTools;

namespace EverythingWorld.World
{
    /// The Glitterlands' artists. Most have a little cloud stage of their own: a round floor of light on a bank of
    /// cloud, a mirror ball overhead, a curtain of tinsel behind, and one thing from their song; covers on a cream card
    /// on a silver board.
    /// - Carly Rae Jepsen: a pastel stage with a gold saxophone on a stand ("Run Away With Me"), one glowing tile and
    ///   a dropped shoe ("Don't Leave Me on the Dance Floor"), a tightrope with a balancing figure ("On Wires").
    /// - Charli xcx: a party for one, a cake and balloons and a single chair ("party 4 u"); a lime-green box with a
    ///   question mark ("Guess").
    /// - Magdalena Bay: a giant old television floating on cloud ("Image"); a sphere of liquid silver ("Mercurial World").
    public partial class GlitKit
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
                Add("Carly Rae Jepsen", "Run Away With Me", "@under the mirrorball"); Add("O-Zone", "Dragostea", "@the sky club"); Add("Haddaway", "What Is Love", "@up the glitter mile");
                Add("Chappell Roan", "Red Wine Supernova", "@the sky club"); Add("Magdalena Bay", "Mercurial World", "Magdalena Bay");
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
            if (floorM == null) MakeMaterials();
            var root = new GameObject("Artist: " + a.name).transform; root.SetParent(ctx.Root, false);
            var front = facing.TryGetValue(a.name, out var f) ? f : new Vector3(center.x - a.x, 0, center.z - a.z).normalized;
            if (front.sqrMagnitude < .01f) front = Vector3.forward;
            var labels = new GameObject("Labels").transform; labels.SetParent(root, false);
            var area = root.gameObject.AddComponent<ArtistArea>();
            area.artistName = a.name; area.stationId = a.stationId; area.radius = a.r; area.glowColor = Pink;
            var z = new Plot { ctx = ctx, root = root, labels = labels, mk = new MeshKit(), solid = new MeshKit(), pictures = new MeshKit(), a = a, c = ctx.OnGround(a.x, a.z), front = front, side = Vector3.Cross(Vector3.up, front), area = area };
            var reach = Foot(a); var up = Vector3.up;
            z.mk.Prism(floorM, z.c - up * .25f, up, reach * .66f, .33f, 24);
            switch (a.name)
            {
                case "Carly Rae Jepsen": Carly(z); break;
                case "Charli xcx": Charli(z); break;
                case "Magdalena Bay": MagBay(z); break;
                default: Others(z, CloudStage(z)); break;
            }
            SongLamps(z);
            Board(z, z.At(reach * .6f, -reach * .45f - 1.5f), front);
            ArtistBits.Plaque(z.mk, z.solid, labels, a, z.At(reach * .6f + 1.5f, reach * .45f + 1.5f), front, chrome, Paint(Pink), Ink, 1.6f);
            BallPost(ctx, z.mk, z.At(reach * .5f, reach * .68f), -z.side);
            if (z.area.glowLight == null) z.area.glowLight = ArtistBits.PointLight(root, z.c + up * 4f + front * 3f, new Color(1f, .86f, 1f), HasStation(a) ? 24f : 15f, 1.8f);
            area.glowRenderers = new Renderer[0];
            z.mk.Build("Area", root, false);
            z.solid.BuildCollider("Walls", root);
            z.pictures.Build("Covers", root, false);
            noGrass.Add(new Vector3(z.c.x, reach * .9f, z.c.z));
            var mark = new GameObject("Shot: " + a.name + " up close").transform; mark.SetParent(ctx.Root, false);
            var cam = z.At(reach * .85f, reach * .3f) + up * 2f; var look = z.At(-reach * .3f, 0) + up * 1.8f - cam; mark.SetPositionAndRotation(cam, Quaternion.LookRotation(look.normalized));
        }

        /// The covers on a cream card on a silver board edged in tile lights.
        void Board(Plot z, Vector3 ground, Vector3 dir)
        {
            dir = Vector3.ProjectOnPlane(dir, Vector3.up).normalized; var rot = Quaternion.LookRotation(dir); var side = Vector3.Cross(Vector3.up, dir);
            var n = Mathf.Max(1, Mathf.Min(7, z.a.songList?.Length ?? 1)); const float size = .9f; var w = n * size * 1.25f + .6f;
            var c = ground + Vector3.up * 1.7f;
            z.mk.Box(chrome, c - Vector3.up * .5f - dir * .06f, new Vector3(w + .5f, size + 2f, .2f), rot); z.solid.Box(chrome, c - Vector3.up * .5f, new Vector3(w + .5f, size + 2f, .3f), rot); z.mk.Box(Paint(Cream), c - Vector3.up * .1f + dir * .07f, new Vector3(w, size + .9f, .04f), rot);
            var nb = Mathf.Max(3, Mathf.RoundToInt(w / .5f)); for (var k = 0; k < nb; k++) z.mk.Box(tileM[k % 4], c + Vector3.up * (size / 2f + .52f) + side * (-w / 2f + k * w / (nb - 1)) + dir * .06f, new Vector3(.3f, .14f, .03f), rot);
            ArtistBits.CoverWall(z.pictures, z.labels, z.ctx, z.a, c + dir * .1f + Vector3.up * .08f, dir, n, size, n, true, Ink);
        }

        /// A little mirror ball on a post for each song without its own piece.
        void SongLamps(Plot z)
        {
            if (!SongIds.TryGetValue(z.a.name, out var list)) return;
            var up = Vector3.up; var k = 0; var seen = new HashSet<string>();
            foreach (var (_, id) in list)
            {
                if (z.lit.Contains(id) || !seen.Add(id)) continue;
                var p = z.At(Foot(z.a) * .55f - (k % 2) * 1.3f, Foot(z.a) * .2f + (k / 2) * 1.3f);
                z.mk.Prism(chrome, p - up * .1f, up, .035f, 1.3f, 5); z.mk.Blob(SongGlow(id, Pink, 1.6f), p + up * 1.44f, Vector3.one * .16f, new System.Random(k), 0f); ArtistBits.PostCollider(z.root, p, 1.3f, .08f);
                k++;
            }
        }

        /// A little cloud stage of their own: a round floor of light on cloud, a mirror ball over it, tinsel behind.
        PlaceFrame CloudStage(Plot z)
        {
            var up = Vector3.up; var hue = z.a.name.Length + z.a.name[0]; var rng = new System.Random(hue); var f = new PlaceFrame(z.At(-Foot(z.a) * .45f, 0), z.front);
            for (var k = 0; k < 6; k++) CloudBank(z.mk, f.P(-2.6f + k * 1.04f, -.2f, -.6f + (k % 2) * .3f), .7f, rng);
            z.mk.Prism(floorM, f.P(0, 0, .6f), up, 1.8f, .4f, 18); z.solid.Prism(floorM, f.P(0, -.2f, .6f), up, 1.8f, .6f, 12); z.mk.Prism(tileM[hue % 4], f.P(0, .41f, .6f), up, 1.7f, .01f, 18);
            for (var k = 0; k < 18; k++) z.mk.Box(k % 3 == 0 ? mirror : Paint(Disco[(hue + k) % Disco.Length]), f.P(-2f + k * .235f, 1.8f, -.9f), new Vector3(.08f, 3.2f, .02f), f.r);
            z.mk.Box(chrome, f.P(0, 3.45f, -.9f), new Vector3(4.6f, .1f, .1f), f.r); foreach (var e in new[] { -2.3f, 2.3f }) { z.mk.Prism(chrome, f.P(e, 0, -.9f), up, .05f, 3.4f, 5); ArtistBits.PostCollider(z.root, f.P(e, 0, -.9f), 3.4f, .07f); }
            Ball(z.mk, f.P(0, 4.2f, .4f), .35f); z.mk.Prism(iron, f.P(0, 4.55f, .4f), up, .01f, .5f, 3);
            return f;
        }

        // ---------------------------------------------------------------- Carly Rae Jepsen
        void Carly(Plot z)
        {
            var up = Vector3.up; var rot = z.Rot; var rng = new System.Random(2015); var fr = z.front; var sd = z.side; var ra = Song(z, "Run Away"); var dl = Song(z, "Don"); var ow = Song(z, "On Wires"); life.runSong = ra;
            var st = z.At(-4f, 0) + up * .1f; DanceFloor(z.mk, z.solid, st, rot, 7, 5, 1.2f, .6f); for (var k = 0; k < 8; k++) CloudBank(z.mk, st - fr * 3.8f + sd * (-4.4f + k * 1.26f), .9f, rng);
            var bk = st - fr * 3.6f; z.mk.Box(Paint(new Color(.98f, .8f, .9f)), bk + up * 3f, new Vector3(9f, 5.4f, .3f), rot); z.solid.Box(chrome, bk + up * 3f, new Vector3(9f, 5.4f, .3f), rot); Heart(z.mk, GlowM(Pink, 1.2f), bk + up * 3.4f + fr * .2f, fr, 1.4f); ArtistBits.Label(z.labels, "E•MO•TION", bk + up * 5.2f + fr * .2f, fr, .4f, Ink, TextAnchor.MiddleCenter);
            Ball(z.mk, st + up * 6f, .8f); z.mk.Prism(iron, st + up * 6.8f, up, .02f, 1f, 3); halos.Add(st + up * 6f, 2.4f, Silver, .3f); z.area.glowLight = ArtistBits.PointLight(z.root, st + up * 4f + fr * 2f, new Color(1f, .86f, .96f), 24f, 2.2f);
            // "Run Away With Me": a gold saxophone on a stand, centre stage
            var sx = st + up * .62f + fr * 1f; z.mk.Prism(chrome, sx, up, .02f, .5f, 4); var gm = SongGlow(ra, Gold, 1.2f); z.mk.Prism(gm, sx + up * .5f, up, .07f, .9f, 8, .7f); z.mk.Prism(gm, sx + up * .5f, (fr + up * .3f).normalized, .1f, .4f, 8, 1.8f); z.mk.Prism(gm, sx + up * 1.4f, (-fr + up * .4f).normalized, .03f, .3f, 5);
            string[] sax = { "You lift the sax and play the opening. Everybody on the cloud turns round at once.", "Bwaaa-ba-ba-baaa! You don't know how to play the saxophone. Doesn't matter.", "That riff. The whole Glitterlands starts running, in the best way." }; var sn = 0;
            Spot("The gold saxophone", sx + up * 1f, "Play the riff", new Vector3(.8f, 1.2f, .8f)).onUse = _ => { life.played = Time.time; Say1(sx + up * 2.4f, sax[sn++ % sax.Length]); };
            // "Don't Leave Me on the Dance Floor": one glowing tile out on its own, a shoe left on it
            var lt = z.At(5f, -7f); z.mk.Box(floorM, lt + up * .1f, new Vector3(1.4f, .2f, 1.4f), rot); z.mk.Box(SongGlow(dl, Pink, 1.6f), lt + up * .21f, new Vector3(1.2f, .02f, 1.2f), rot); z.mk.Box(Paint(new Color(.9f, .2f, .5f)), lt + up * .3f + sd * .2f, new Vector3(.12f, .1f, .3f), rot * Quaternion.Euler(0, 30f, 0)); z.mk.Prism(Paint(new Color(.9f, .2f, .5f)), lt + up * .22f + sd * .2f - fr * .1f, up, .015f, .1f, 4);
            // "On Wires": a tightrope between two posts and a figure balancing with a pole
            var wa = z.At(4f, 6f); var wb = z.At(4f, 12f); foreach (var g in new[] { wa, wb }) { z.mk.Prism(chrome, g - up * .2f, up, .07f, 4f, 6); ArtistBits.PostCollider(z.root, g, 3.8f, .09f); } z.mk.Prism(SongGlow(ow, Silver, 1.4f), wa + up * 3.8f, (wb - wa).normalized, .015f, Vector3.Distance(wa, wb), 3); var fg = (wa + wb) / 2f + up * 3.8f; var fm = Paint(new Color(.95f, .6f, .8f)); z.mk.Prism(fm, fg, up, .08f, .7f, 5); z.mk.Blob(fm, fg + up * .84f, Vector3.one * .1f, rng, 0f); z.mk.Prism(Paint(Gold), fg + up * .5f - fr * 1f, fr, .015f, 2f, 3);
            Shot(z.ctx, "carly's stage", z.At(13f, 4f) + up * 2.2f, st + up * 2.4f);
        }

        // ---------------------------------------------------------------- Charli xcx
        void Charli(Plot z)
        {
            var up = Vector3.up; var rot = z.Rot; var rng = new System.Random(2024); var fr = z.front; var sd = z.side; var p4 = Song(z, "party 4 u"); var gu = Song(z, "Guess");
            // "party 4 u": a party for one: a table, a cake, balloons, one chair, the lights on
            var pt = z.At(-3f, 0) + up * .05f; DanceFloor(z.mk, z.solid, pt, rot, 5, 5, 1.2f, .3f); var tb = pt + up * .3f; z.mk.Prism(white, tb + up * .72f, up, .6f, .05f, 14); z.mk.Prism(chrome, tb, up, .05f, .72f, 5); BoxCol(z.root, tb + up * .38f, new Vector3(1.2f, .76f, 1.2f), rot); z.mk.Prism(Paint(Pink), tb + up * .77f, up, .3f, .24f, 12); z.mk.Prism(white, tb + up * 1.01f, up, .31f, .03f, 12); z.mk.Blob(SongGlow(p4, Gold, 1.8f), tb + up * 1.16f, new Vector3(.02f, .05f, .02f), rng, 0f); Chair(z.mk, tb - fr * .9f, fr, "Sit at your party");
            for (var k = 0; k < 5; k++) { var bp = tb + Quaternion.Euler(0, k * 72f, 0) * fr * 1.6f; z.mk.Prism(white, bp, up, .005f, 2.4f + k * .2f, 3); z.mk.Blob(Paint(Disco[k]), bp + up * (2.7f + k * .2f), new Vector3(.3f, .38f, .3f), rng, 0f); }
            z.area.glowLight = ArtistBits.PointLight(z.root, pt + up * 4f, new Color(1f, .9f, 1f), 20f, 2.2f);
            // "Guess": a lime-green box with a question mark on every side
            var gb = z.At(5f, 6f) + up * .05f; var gm = SongGlow(gu, Lime, .9f); z.mk.Box(gm, gb + up * .9f, new Vector3(1.8f, 1.8f, 1.8f), rot); BoxCol(z.root, gb + up * .9f, new Vector3(1.8f, 1.8f, 1.8f), rot); for (var k = 0; k < 4; k++) { var d = Quaternion.Euler(0, k * 90f, 0) * fr; ArtistBits.Label(z.labels, "?", gb + up * .9f + d * .91f, d, .9f, Ink, TextAnchor.MiddleCenter); } halos.Add(gb + up * .9f, 2f, Lime, .3f);
            Shot(z.ctx, "a party for one", z.At(9f, -4f) + up * 2f, pt + up * 1f);
        }

        // ---------------------------------------------------------------- Magdalena Bay
        void MagBay(Plot z)
        {
            var up = Vector3.up; var rot = z.Rot; var rng = new System.Random(2021); var fr = z.front; var sd = z.side; var im = Song(z, "Image"); var mw = Song(z, "Mercurial World"); life.imageSong = im; life.mercSong = mw;
            // "Image": a giant old television floating over a cloud bank
            var tv = z.At(-3f, -2f) + up * 3.2f; CloudBank(z.mk, tv - up * 3.2f, 1.6f, rng); z.mk.Box(Paint(new Color(.86f, .84f, .8f)), tv, new Vector3(3.2f, 2.6f, 2.4f), rot); z.mk.Box(SongGlow(im, new Color(.4f, .7f, 1f), 1.2f), tv + fr * 1.21f, new Vector3(2.4f, 1.9f, .02f), rot); for (var k = 0; k < 8; k++) z.mk.Box(Paint(new Color(.3f, .5f, .9f)), tv + fr * 1.22f + up * (-.8f + k * .23f), new Vector3(2.4f, .03f, .01f), rot); foreach (var e in new[] { -1f, 1f }) z.mk.Prism(chrome, tv + up * 1.3f, (up + sd * e * .5f).normalized, .02f, 1.4f, 3); halos.Add(tv + fr * 1.6f, 2.4f, new Color(.4f, .7f, 1f), .2f);
            // "Mercurial World": a sphere of liquid silver on a cloud, and smaller drops round it
            var mp = z.At(4f, 5f) + up * 1.6f; CloudBank(z.mk, mp - up * 1.6f, 1.2f, rng); var mm = SongGlow(mw, Silver, 1f); z.mk.Blob(mm, mp, Vector3.one * 1.2f, rng, 0f); for (var k = 0; k < 6; k++) z.mk.Blob(mm, mp + Quaternion.Euler(k * 50f, k * 70f, 0) * Vector3.forward * (1.8f + (k % 3) * .4f), Vector3.one * (.15f + (k % 3) * .08f), rng, 0f); ArtistBits.PostCollider(z.root, mp - up * 1.6f, 2.6f, 1.2f); halos.Add(mp, 2.6f, Silver, .3f);
            z.area.glowLight = ArtistBits.PointLight(z.root, z.c + up * 4f, new Color(.7f, .8f, 1f), 20f, 2f);
            Shot(z.ctx, "magdalena bay's television", z.At(9f, 3f) + up * 2.4f, tv);
        }

        // ---------------------------------------------------------------- everyone else: one thing from their song
        void Others(Plot z, PlaceFrame f)
        {
            var up = Vector3.up; var rot = z.Rot; var rng = new System.Random(z.a.name.Length * 7 + z.a.name[0]); var c = z.At(2.2f, 0) + up * .05f; var fr = z.front; var sd = z.side;
            void Post(Vector3 at, float h, float r = .04f) { z.mk.Prism(chrome, at - up * .2f, up, r, h + .2f, 5); ArtistBits.PostCollider(z.root, at, h, r + .03f); }
            void Plinth(Vector3 at, float w, float h) { z.mk.Box(chrome, at + up * (h / 2f), new Vector3(w, h, w), rot); BoxCol(z.root, at + up * (h / 2f), new Vector3(w, h, w), rot); }
            switch (z.a.name)
            {
                case "Audrey Hobert": { var su = Song(z, "Sue me"); var ph = Song(z, "Phoebe"); Plinth(c - sd * .8f, .6f, .9f); z.mk.Box(SongGlow(su, Gold, .7f), c - sd * .8f + up * 1.05f, new Vector3(.12f, .12f, .4f), rot); z.mk.Prism(Paint(new Color(.5f, .34f, .26f)), c - sd * .8f + up * 1.05f, up, .02f, .3f, 4); Post(c + sd * .9f, 2.2f, .006f); z.mk.Blob(SongGlow(ph, Pink, 1f), c + sd * .9f + up * 2.5f, new Vector3(.36f, .42f, .36f), rng, 0f); ArtistBits.Label(z.labels, "PHOEBE", c + sd * .9f + up * 2.5f + fr * .37f, fr, .08f, Ink, TextAnchor.MiddleCenter); break; }
                case "Cryalot": { var lb = Song(z, "Labyrinth"); var sy = Song(z, "See You Again"); for (var k = 0; k < 5; k++) { var a = k * 72f; for (var q = 0; q < 2; q++) z.mk.Box(k == 0 && q == 0 ? SongGlow(lb, Lilac, .8f) : crystal, c + Quaternion.Euler(0, a + q * 30f, 0) * Vector3.forward * (.8f + q * .7f) + up * .4f, new Vector3(1f, .8f, .2f), Quaternion.Euler(0, a + q * 30f + 90f, 0)); } BoxCol(z.root, c + up * .4f, new Vector3(3f, .8f, 3f), rot); Heart(z.mk, SongGlow(sy, Silver, 1f), c + up * 1.8f, fr, .5f); break; }
                case "ADÉLA": { var s = Song(z, "Nicole Kidman"); z.mk.Prism(Paint(new Color(.95f, .6f, .74f)), c + up * .01f, up, .8f, .02f, 10); Star(z.mk, SongGlow(s, Gold, 1.4f), c + up * .04f, up, .6f); Post(c + fr * 1.2f - sd * .8f, 1.2f, .03f); z.mk.Box(dark, c + fr * 1.2f - sd * .8f + up * 1.3f, new Vector3(.3f, .24f, .2f), rot); break; }
                case "Ace of Base": { var s = Song(z, "Happy Nation"); life.happySong = s; for (var k = 0; k < 3; k++) { var fp = c + sd * (-1f + k); Post(fp, 2.6f, .03f); z.mk.Box(Paint(new[] { new Color(.3f, .5f, .9f), new Color(1f, .86f, .3f), Cream }[k]), fp + up * 2.3f + sd * .4f, new Vector3(.8f, .5f, .02f), rot); } z.mk.Blob(SongGlow(s, Gold, 1.4f), c + up * 3.4f - fr * .6f, Vector3.one * .4f, rng, 0f); break; }
                case "Ava Max": { var s = Song(z, "Sweet but Psycho"); var ca = c + up * .6f; Plinth(c, .7f, .3f); z.mk.Blob(SongGlow(s, new Color(.9f, .14f, .2f), .8f), ca + up * .2f, Vector3.one * .3f, rng, 0f); z.mk.Prism(Paint(new Color(.7f, .5f, .3f)), ca + up * .44f, up, .02f, .5f, 4); break; }
                case "Caramella Girls": { var s = Song(z, "Caramelldansen"); life.caraSong = s; DanceFloor(z.mk, z.solid, c, rot, 2, 2, 1f, .2f); foreach (var e in new[] { -1f, 1f }) { z.mk.Prism(SongGlow(s, Pink, 1f), c + up * .2f + sd * e * .3f, (up + sd * e * .25f).normalized, .14f, 1.6f, 6, .4f); } break; }
                case "Chappell Roan": { var s = Song(z, "Red Wine Supernova"); life.wineSong = s; Plinth(c, .7f, .8f); var g = c + up * .8f; z.mk.Prism(glassM, g, up, .12f, .04f, 10); z.mk.Prism(glassM, g, up, .02f, .4f, 5); z.mk.Prism(glassM, g + up * .4f, up, .2f, .44f, 10, 1.2f); z.mk.Prism(Paint(new Color(.6f, .1f, .2f)), g + up * .44f, up, .19f, .2f, 10, 1.1f); Star(z.mk, SongGlow(s, new Color(1f, .3f, .5f), 1.8f), g + up * 1.4f, fr, .3f); halos.Add(g + up * 1.4f, 1.2f, new Color(1f, .3f, .5f), .4f); break; }
                case "Haddaway": { var s = Song(z, "What Is Love"); life.loveSong = s; Post(c, 1.6f, .03f); Heart(z.mk, SongGlow(s, new Color(.9f, .2f, .3f), 1.2f), c + up * 2.4f, fr, .7f); ArtistBits.Label(z.labels, "?", c + up * 2.4f + fr * .02f, fr, .4f, Cream, TextAnchor.MiddleCenter); break; }
                case "Kero Kero Bonito": { var s = Song(z, "The Princess and the Clock"); Post(c, 1.6f, .05f); z.mk.Prism(white, c + up * 2f - fr * .05f, fr, .5f, .1f, 16); z.mk.Box(dark, c + up * 2.16f + fr * .06f, new Vector3(.03f, .3f, .01f), rot); z.mk.Box(dark, c + up * 2.06f + fr * .06f + sd * .08f, new Vector3(.03f, .2f, .01f), rot * Quaternion.Euler(0, 0, -60f)); for (var k = 0; k < 5; k++) z.mk.Cone(SongGlow(k == 2 ? s : null, Gold, 1f), c + up * 2.5f + sd * (-.2f + k * .1f), .04f, .14f, 4); break; }
                case "Kim Petras": { var s = Song(z, "Coconuts"); Plinth(c, 1f, .8f); for (var k = 0; k < 3; k++) z.mk.Blob(k == 0 ? SongGlow(s, new Color(.5f, .34f, .24f), .4f) : Paint(new Color(.44f, .3f, .2f)), c + up * (1f + (k == 2 ? .3f : 0f)) + sd * (k == 1 ? .3f : -.15f), Vector3.one * .22f, rng, .1f); break; }
                case "Lorde": { var s = Song(z, "Supercut"); life.cutSong = s; for (var k = 0; k <= 10; k++) { var a = Mathf.PI * k / 10f; z.mk.Box(k % 2 == 0 ? SongGlow(s, new Color(1f, .9f, .7f), .7f) : dark, c + up * Mathf.Sin(a) * 2.6f + sd * Mathf.Cos(a) * 1.8f, new Vector3(.6f, .5f, .06f), rot * Quaternion.Euler(0, 0, -Mathf.Cos(a) * 60f + 0f)); } foreach (var e in new[] { -1.8f, 1.8f }) ArtistBits.PostCollider(z.root, c + sd * e, 1f, .3f); break; }
                case "O-Zone": { var s = Song(z, "Dragostea din tei"); life.dragSong = s; z.mk.Prism(Paint(new Color(.4f, .3f, .26f)), c - up * .2f, up, .16f, 2.4f, 6); ArtistBits.PostCollider(z.root, c, 2.2f, .2f); for (var k = 0; k < 4; k++) z.mk.Blob(Paint(new Color(.5f, .8f, .5f)), c + up * (2.8f + (k % 2) * .4f) + Quaternion.Euler(0, k * 90f, 0) * Vector3.forward * (k == 0 ? 0f : .7f), new Vector3(.9f, .7f, .9f), rng, .2f); var hp = c + fr * 1.2f; Post(hp, 1f, .03f); foreach (var e in new[] { -1f, 1f }) z.mk.Prism(SongGlow(s, new Color(.4f, .7f, 1f), 1.2f), hp + up * 1.2f + sd * e * .14f, sd * e, .08f, .05f, 8); break; }
                case "Owl City": { var s = Song(z, "Good Time"); life.goodSong = s; Post(c, 2.2f, .04f); var ob = c + up * 2.5f; var om = SongGlow(s, new Color(.7f, .6f, .9f), .6f); z.mk.Blob(om, ob, new Vector3(.3f, .4f, .26f), rng, 0f); foreach (var e in new[] { -1f, 1f }) { z.mk.Blob(Paint(Cream), ob + up * .14f + sd * e * .12f + fr * .2f, Vector3.one * .1f, rng, 0f); z.mk.Blob(dark, ob + up * .14f + sd * e * .12f + fr * .28f, Vector3.one * .04f, rng, 0f); z.mk.Cone(om, ob + up * .34f + sd * e * .14f, .05f, .14f, 4); } break; }
                case "Ruelle": { var s = Song(z, "Slow Motion"); Post(c - sd * .6f, 1f, .03f); Post(c + sd * .6f, 1f, .03f); z.mk.Box(chrome, c + up * 1.6f, new Vector3(1.4f, 1.1f, .06f), rot); z.mk.Box(SongGlow(s, new Color(.8f, .76f, .9f), .5f), c + up * 1.6f + fr * .04f, new Vector3(1.2f, .9f, .02f), rot); break; }
                case "Stepford Wives": { var s = Song(z, "After Red 40"); Plinth(c, .8f, .8f); z.mk.Prism(glassM, c + up * .8f, up, .3f, .2f, 10, 1.3f); for (var k = 0; k < 9; k++) z.mk.Blob(k == 0 ? SongGlow(s, new Color(1f, .1f, .2f), 1.2f) : Paint(new Color(.9f, .1f, .2f)), c + up * (1f + (k % 3) * .06f) + Quaternion.Euler(0, k * 40f, 0) * Vector3.forward * .18f, Vector3.one * .06f, rng, 0f); break; }
                case "Tove Lo": { var s = Song(z, "des fleurs"); Plinth(c, .6f, .8f); z.mk.Prism(glassM, c + up * .8f, up, .12f, .4f, 8); for (var k = 0; k < 9; k++) { var d = (up + Quaternion.Euler(0, k * 40f, 0) * Vector3.forward * .3f).normalized; z.mk.Prism(Paint(new Color(.3f, .6f, .3f)), c + up * 1.1f, d, .01f, .6f, 3); z.mk.Blob(k == 0 ? SongGlow(s, Pink, 1f) : Paint(Disco[k % Disco.Length]), c + up * 1.1f + d * .64f, Vector3.one * .08f, rng, .2f); } break; }
                case "Vylet Pony": { var s = Song(z, "Creature City"); Plinth(c, 1.2f, .5f); var hb = c + up * 1.1f; var hm = SongGlow(s, new Color(.7f, .6f, 1f), .7f); z.mk.Blob(hm, hb, new Vector3(.3f, .3f, .6f), rng, 0f); z.mk.Blob(hm, hb + fr * .5f + up * .3f, Vector3.one * .22f, rng, 0f); foreach (var e in new[] { -1f, 1f }) foreach (var q in new[] { -.3f, .3f }) z.mk.Prism(hm, hb + fr * q + sd * e * .14f, -up, .04f, .5f, 4); Star(z.mk, GlowM(Gold, 1.2f), hb + up * .5f - fr * .1f, sd, .12f); break; }
                default: { if (SongIds.TryGetValue(z.a.name, out var list) && list.Length > 0) { var s = Song(z, list[0].title); Post(c, 1.4f); z.mk.Blob(SongGlow(s, Pink, 1.6f), c + up * 1.6f, Vector3.one * .2f, rng, 0f); } break; }
            }
        }

        void Star(MeshKit mk, Material m, Vector3 c, Vector3 face, float r)
        {
            face.Normalize(); var up = Mathf.Abs(Vector3.Dot(face, Vector3.up)) > .9f ? Vector3.forward : Vector3.up;
            for (var k = 0; k < 5; k++) { var a0 = Quaternion.AngleAxis(k * 72f, face) * up; var a1 = Quaternion.AngleAxis(k * 72f + 36f, face) * up; var a2 = Quaternion.AngleAxis(k * 72f - 36f, face) * up; mk.Tri(m, c + a0 * r, c + a1 * r * .42f, c + a2 * r * .42f, c - face); mk.Tri(m, c + a0 * r, c + a1 * r * .42f, c + a2 * r * .42f, c + face); mk.Tri(m, c, c + a1 * r * .42f, c + a2 * r * .42f, c - face); mk.Tri(m, c, c + a1 * r * .42f, c + a2 * r * .42f, c + face); }
        }
    }
}
