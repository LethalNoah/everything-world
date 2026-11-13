using System.Collections.Generic;
using UnityEngine;
using static EverythingWorld.World.PlaceTools;

namespace EverythingWorld.World
{
    /// The Isle of Memes' artists. Most have a window of their own: a pastel window from an old operating
    /// system, standing in the grass with its title bar and three little buttons, their name across the top, a square of
    /// checkered marble in front, and one thing from their song; covers on a cream card on a glossy board.
    /// - Phineas and Ferb: a summer backyard. A big shady tree with a bench round it, half a rollercoaster loop going up
    ///   over the fence, and a piece for every song: a band stage with a spotlight ("Gitchee Gitchee Goo"), a squirrel
    ///   statue ("S.I.M.P."), a metronome ("Ain't Got Rhythm"), a pink spotlight and microphone ("I'm Lindana"), a heart
    ///   balloon ("You Snuck Your Way Right Into My Heart"), a glittering crown on a pedestal ("Disco Miniature Golfing
    ///   Queen"), a sun over a little globe ("Summer Belongs to You") and a big dial turned up a notch ("Kick It Up A Notch").
    /// - Stantough and Beedle the Bardcore: a bard's corner: a lute, a stool, a candle, a banner.
    public partial class MemeKit
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
                Add("PSY", "Gangnam", "@the homepage"); Add("Nathan Evans", "Wellerman", "@the dream mall"); Add("Burl Ives", "A Holly Jolly", "@the homepage");
                Add("Hampton The Hampster", "The HampsterDance", "@along the superhighway"); Add("Kahoot!", "Lobby Music", "@the dream mall"); Add("Phineas and Ferb", "Gitchee", "Phineas and Ferb");
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
            if (checkA == null) MakeMaterials();
            var root = new GameObject("Artist: " + a.name).transform; root.SetParent(ctx.Root, false);
            var front = facing.TryGetValue(a.name, out var f) ? f : new Vector3(center.x - a.x, 0, center.z - a.z).normalized;
            if (front.sqrMagnitude < .01f) front = Vector3.forward;
            var labels = new GameObject("Labels").transform; labels.SetParent(root, false);
            var area = root.gameObject.AddComponent<ArtistArea>();
            area.artistName = a.name; area.stationId = a.stationId; area.radius = a.r; area.glowColor = Pink;
            var z = new Plot { ctx = ctx, root = root, labels = labels, mk = new MeshKit(), solid = new MeshKit(), pictures = new MeshKit(), a = a, c = ctx.OnGround(a.x, a.z), front = front, side = Vector3.Cross(Vector3.up, front), area = area };
            var reach = Foot(a); var up = Vector3.up;
            z.mk.Prism(Paint(new Color(.8f, .76f, .86f)), z.c - up * .25f, up, reach * .66f, .33f, 24);
            switch (a.name)
            {
                case "Phineas and Ferb": Phineas(z); break;
                default: Others(z, Window(z)); break;
            }
            SongLamps(z);
            Board(z, z.At(reach * .6f, -reach * .45f - 1.5f), front);
            ArtistBits.Plaque(z.mk, z.solid, labels, a, z.At(reach * .6f + 1.5f, reach * .45f + 1.5f), front, chrome, Paint(Pink), Ink, 1.6f);
            GlobeLamp(ctx, z.mk, z.At(reach * .5f, reach * .68f));
            if (z.area.glowLight == null) z.area.glowLight = ArtistBits.PointLight(root, z.c + up * 4f + front * 3f, new Color(1f, .9f, 1f), HasStation(a) ? 24f : 14f, 1.2f);
            area.glowRenderers = new Renderer[0];
            z.mk.Build("Area", root, false);
            z.solid.BuildCollider("Walls", root);
            z.pictures.Build("Covers", root, false);
            noGrass.Add(new Vector3(z.c.x, reach * .9f, z.c.z));
            var mark = new GameObject("Shot: " + a.name + " up close").transform; mark.SetParent(ctx.Root, false);
            var cam = z.At(reach * .85f, reach * .3f) + up * 2f; var look = z.At(-reach * .3f, 0) + up * 1.8f - cam; mark.SetPositionAndRotation(cam, Quaternion.LookRotation(look.normalized));
        }

        /// The covers on a cream card on a glossy teal board.
        void Board(Plot z, Vector3 ground, Vector3 dir)
        {
            dir = Vector3.ProjectOnPlane(dir, Vector3.up).normalized; var rot = Quaternion.LookRotation(dir);
            var n = Mathf.Max(1, Mathf.Min(7, z.a.songList?.Length ?? 1)); const float size = .9f; var w = n * size * 1.25f + .6f;
            var c = ground + Vector3.up * 1.7f;
            z.mk.Box(Paint(Teal), c - Vector3.up * .5f - dir * .06f, new Vector3(w + .5f, size + 2f, .2f), rot); z.solid.Box(chrome, c - Vector3.up * .5f, new Vector3(w + .5f, size + 2f, .3f), rot); z.mk.Box(Paint(new Color(1f, .97f, .94f)), c - Vector3.up * .1f + dir * .07f, new Vector3(w, size + .9f, .04f), rot); z.mk.Box(white, c + Vector3.up * (size / 2f + .5f) + dir * .05f - Vector3.Cross(Vector3.up, dir) * (w / 2f - .4f), new Vector3(.5f, .14f, .02f), rot);
            ArtistBits.CoverWall(z.pictures, z.labels, z.ctx, z.a, c + dir * .1f + Vector3.up * .08f, dir, n, size, n, true, Ink);
        }

        /// A glossy little bubble on a post for each song without its own piece.
        void SongLamps(Plot z)
        {
            if (!SongIds.TryGetValue(z.a.name, out var list)) return;
            var up = Vector3.up; var k = 0; var seen = new HashSet<string>();
            foreach (var (_, id) in list)
            {
                if (z.lit.Contains(id) || !seen.Add(id)) continue;
                var p = z.At(Foot(z.a) * .55f - (k % 2) * 1.3f, Foot(z.a) * .2f + (k / 2) * 1.3f);
                z.mk.Prism(chrome, p - up * .1f, up, .035f, 1.3f, 5); z.mk.Blob(SongGlow(id, Aqua, 1.6f), p + up * 1.46f, Vector3.one * .18f, new System.Random(k), 0f); ArtistBits.PostCollider(z.root, p, 1.3f, .08f);
                k++;
            }
        }

        /// A window of their own: a pastel old-OS window standing in the grass, title bar and buttons, checkered marble in front.
        PlaceFrame Window(Plot z)
        {
            var up = Vector3.up; var hue = z.a.name.Length + z.a.name[0]; var f = new PlaceFrame(z.At(-Foot(z.a) * .45f, 0), z.front); var col = new[] { Pink, Teal, Lilac, Sunny, Aqua }[hue % 5];
            z.mk.Box(Paint(col), f.P(0, 2f, 0), new Vector3(5f, 3.4f, .2f), f.r); z.solid.Box(chrome, f.P(0, 2f, 0), new Vector3(5f, 3.4f, .2f), f.r); z.mk.Box(GlowM(new Color(.3f, .4f, .9f), .8f), f.P(0, 3.5f, .11f), new Vector3(4.9f, .36f, .02f), f.r); ArtistBits.Label(z.labels, z.a.name, f.P(-.8f, 3.5f, .13f), f.Fwd, .16f, Color.white, TextAnchor.MiddleCenter);
            for (var k = 0; k < 3; k++) z.mk.Box(k == 2 ? Paint(new Color(.9f, .3f, .3f)) : white, f.P(1.8f + k * .3f, 3.5f, .13f), new Vector3(.24f, .24f, .02f), f.r); z.mk.Box(GlowM(new Color(.5f, .76f, 1f), .7f), f.P(0, 2.2f, .11f), new Vector3(4.6f, 2f, .02f), f.r); z.mk.Box(GlowM(Bliss, .5f), f.P(0, .9f, .11f), new Vector3(4.6f, .8f, .02f), f.r); z.mk.Prism(GlowM(Bliss, .5f), f.P(-.6f, .9f, .12f), f.Fwd, 1.4f, .01f, 14); z.mk.Blob(white, f.P(1.2f, 2.6f, .13f), new Vector3(.5f, .2f, .02f), new System.Random(hue), 0f); for (var k = 0; k < 3; k++) z.mk.Box(Paint(new[] { Sunny, Pink, white.color }[k]), f.P(-1.9f, 2.7f - k * .6f, .13f), new Vector3(.34f, .3f, .02f), f.r);
            foreach (var e in new[] { -2.2f, 2.2f }) z.mk.Prism(chrome, f.P(e, -.2f, 0), up, .06f, .5f, 5);
            Checker(z.mk, f.P(0, -.25f, 2.6f) + up * .27f, f.r, 4, 1.2f, .02f);
            return f;
        }

        // ---------------------------------------------------------------- Phineas and Ferb: the summer backyard
        void Phineas(Plot z)
        {
            var up = Vector3.up; var rot = z.Rot; var rng = new System.Random(2007); var fr = z.front; var sd = z.side;
            var gg = Song(z, "Gitchee"); var sm = Song(z, "S.I.M.P"); var ar = Song(z, "Ain't Got Rhythm"); var li = Song(z, "I'm Lindana"); var sn = Song(z, "You Snuck"); var dq = Song(z, "Disco Miniature"); var sb = Song(z, "Summer Belongs"); var ku = Song(z, "Kick It Up"); life.gitcheeSong = gg;
            // a wooden fence round the back of the yard
            for (var k = 0; k < 18; k++) { var a = (k - 8.5f) * 9f * Mathf.Deg2Rad; var p = z.c - fr * Mathf.Cos(a) * 13f + sd * Mathf.Sin(a) * 13f; p = z.ctx.OnGround(p.x, p.z); z.mk.Box(Paint(new Color(.8f, .66f, .5f)), p + up * .9f, new Vector3(.24f, 1.8f, .06f), Quaternion.LookRotation((z.c - p).normalized)); } BoxCol(z.root, z.c - fr * 12.8f + up * .9f, new Vector3(24f, 1.8f, .3f), rot);
            // the big backyard tree with a bench all round it
            var tr = z.At(-6f, -5f); z.mk.Prism(Paint(new Color(.5f, .36f, .26f)), tr, up, .6f, 4f, 9, .7f); ArtistBits.PostCollider(z.root, tr, 3f, .7f); for (var k = 0; k < 6; k++) z.mk.Blob(leafM, tr + up * (5f + (k % 2) * 1f) + Quaternion.Euler(0, k * 60f, 0) * fr * (k == 0 ? 0f : 2.4f), new Vector3(2.6f, 1.8f, 2.6f), rng, .2f); z.mk.Prism(Paint(new Color(.66f, .5f, .36f)), tr + up * .4f, up, 1.5f, .1f, 14); for (var k = 0; k < 4; k++) { var d = Quaternion.Euler(0, k * 90f, 0) * fr; Interact.Seat(tlife.transform, tr + d * 1.3f + up * .5f, d, "Sit under the tree").seatHeight = .5f; }
            Folk(tr + fr * 1.3f + up * .48f, fr, Resident.Pose.Sit, new[] { "I know what we're gonna do today.", "There's a hundred and four days of summer vacation. We're using all of them.", "Mom's not going to believe this. She never sees it." }, null, null, .5f);
            // half a rollercoaster loop going up over the fence
            var lc = z.At(-9f, 4f); var lm = Paint(new Color(.9f, .3f, .3f)); for (var k = 0; k < 14; k++) { var a0 = k * 15f * Mathf.Deg2Rad; var a1 = (k + 1) * 15f * Mathf.Deg2Rad; foreach (var e in new[] { -.6f, .6f }) { var p0 = lc + sd * e - fr * Mathf.Sin(a0) * 5f + up * (5f - Mathf.Cos(a0) * 5f); var p1 = lc + sd * e - fr * Mathf.Sin(a1) * 5f + up * (5f - Mathf.Cos(a1) * 5f); z.mk.Prism(lm, p0, (p1 - p0).normalized, .08f, Vector3.Distance(p0, p1) + .02f, 5); } if (k % 2 == 0) { var mp = lc - fr * Mathf.Sin(a0) * 5f + up * (5f - Mathf.Cos(a0) * 5f); z.mk.Prism(Paint(new Color(.6f, .62f, .7f)), z.ctx.OnGround(mp.x, mp.z), up, .07f, mp.y - z.ctx.Ground(mp.x, mp.z), 4); } } ArtistBits.PostCollider(z.root, lc, 3f, .6f);
            // "Gitchee Gitchee Goo": a little band stage with a spotlight
            var bs = z.At(4f, -9f) + up * .05f; z.mk.Box(Paint(new Color(.5f, .36f, .26f)), bs + up * .3f, new Vector3(4f, .6f, 2.4f), rot); z.solid.Box(woodM(), bs + up * .2f, new Vector3(4f, .8f, 2.4f), rot); z.mk.Prism(chrome, bs + up * .6f - fr * .2f, up, .02f, 1.5f, 4); z.mk.Blob(white, bs + up * 2.16f - fr * .2f, new Vector3(.05f, .08f, .05f), rng, 0f); z.mk.Prism(iron, bs - fr * 1.6f, up, .05f, 3.8f, 5); z.mk.Prism(SongGlow(gg, Sunny, 1.4f), bs - fr * 1.4f + up * 3.6f, (fr - up * .5f).normalized, .2f, .3f, 8, 1.6f); ArtistBits.PostCollider(z.root, bs - fr * 1.6f, 3.6f, .07f);
            string[] gitchee = { "You grab the mic and sing a song with nonsense words in it. Everyone knows them anyway.", "Gitchee gitchee... you trail off. The whole backyard finishes it.", "The spotlight finds you. You were born for this. Briefly." }; var gn = 0;
            Spot("The band stage", bs + up * 1.4f, "Sing on the stage", new Vector3(3.6f, 1.4f, 2f)).onUse = _ => { life.played = Time.time; Say1(bs + up * 2.8f, gitchee[gn++ % gitchee.Length]); };
            // a squirrel statue; a metronome; a pink spotlight and mic
            var sq = z.At(7f, -3f) + up * .05f; z.mk.Box(marble, sq + up * .3f, new Vector3(.7f, .6f, .7f), rot); BoxCol(z.root, sq + up * .3f, new Vector3(.7f, .6f, .7f), rot); var sqm = SongGlow(sm, new Color(.7f, .5f, .36f), .5f); z.mk.Blob(sqm, sq + up * .9f, new Vector3(.2f, .28f, .2f), rng, 0f); z.mk.Blob(sqm, sq + up * 1.26f + fr * .06f, Vector3.one * .12f, rng, 0f); z.mk.Prism(sqm, sq + up * .7f - fr * .2f, (up - fr * .4f).normalized, .14f, .7f, 6, .5f);
            var mt = z.At(7f, 3f) + up * .05f; z.mk.Box(marble, mt + up * .3f, new Vector3(.7f, .6f, .7f), rot); BoxCol(z.root, mt + up * .3f, new Vector3(.7f, .6f, .7f), rot); z.mk.Prism(Paint(new Color(.5f, .36f, .26f)), mt + up * .6f, up, .3f, .8f, 4, .3f); z.mk.Prism(SongGlow(ar, Sunny, 1.2f), mt + up * .7f + fr * .2f, (up + sd * .3f).normalized, .015f, .7f, 3); life.metronome = mt + up * .7f + fr * .22f;
            var pl = z.At(3f, 9f) + up * .05f; z.mk.Prism(SongGlow(li, Pink, 1f), pl + up * .01f, up, 1f, .02f, 16); z.mk.Prism(chrome, pl, up, .02f, 1.5f, 4); z.mk.Blob(Paint(Pink), pl + up * 1.56f, new Vector3(.05f, .08f, .05f), rng, 0f);
            // a heart balloon; a glittering crown on a pedestal; a sun over a globe; a big dial
            var hb = z.At(-1f, 10f); z.mk.Prism(white, hb, up, .006f, 2.6f, 3); Heart(z.mk, SongGlow(sn, new Color(1f, .4f, .5f), 1f), hb + up * 3f, fr, .6f);
            var cr = z.At(-2f, -10f) + up * .05f; z.mk.Box(marble, cr + up * .5f, new Vector3(.6f, 1f, .6f), rot); BoxCol(z.root, cr + up * .5f, new Vector3(.6f, 1f, .6f), rot); var cm = SongGlow(dq, Sunny, 1.4f); z.mk.Prism(cm, cr + up * 1f, up, .24f, .14f, 10); for (var k = 0; k < 5; k++) z.mk.Cone(cm, cr + up * 1.14f + Quaternion.Euler(0, k * 72f, 0) * fr * .2f, .05f, .16f, 4); z.mk.Blob(Paint(new Color(.9f, .9f, 1f)), cr + up * 1.6f, Vector3.one * .14f, rng, .05f);
            var gl = z.At(1f, -12f) + up * .05f; z.mk.Prism(chrome, gl, up, .04f, 1f, 5); z.mk.Blob(aquaM, gl + up * 1.3f, Vector3.one * .3f, rng, 0f); z.mk.Blob(SongGlow(sb, new Color(1f, .8f, .3f), 1.6f), gl + up * 2.4f, Vector3.one * .3f, rng, 0f); ArtistBits.PostCollider(z.root, gl, 1f, .06f);
            var dl = z.At(-11f, -1f); z.mk.Prism(chrome, dl - up * .2f, up, .05f, 1.4f, 5); z.mk.Prism(Paint(new Color(.3f, .3f, .4f)), dl + up * 1.6f - fr * .05f, fr, .5f, .1f, 16); z.mk.Box(SongGlow(ku, new Color(1f, .4f, .2f), 1.2f), dl + up * 1.6f + fr * .06f + (Quaternion.AngleAxis(-60f, fr) * up) * .25f, new Vector3(.06f, .46f, .02f), Quaternion.AngleAxis(-60f, fr) * rot); for (var k = 0; k < 11; k++) z.mk.Box(white, dl + up * 1.6f + fr * .06f + Quaternion.AngleAxis(-120f + k * 24f, fr) * up * .42f, new Vector3(.03f, .08f, .01f), Quaternion.AngleAxis(-120f + k * 24f, fr) * rot); ArtistBits.PostCollider(z.root, dl, 1.4f, .07f);
            z.area.glowLight = ArtistBits.PointLight(z.root, z.c + up * 5f, new Color(1f, .94f, .8f), 26f, 1.4f);
            Shot(z.ctx, "the summer backyard", z.At(16f, 4f) + up * 2.4f, z.c + up * 2f - fr * 4f);
        }

        /// A flat heart shape facing 'face'.
        void Heart(MeshKit mk, Material m, Vector3 c, Vector3 face, float s)
        {
            face.y = 0; face.Normalize(); var side = Vector3.Cross(Vector3.up, face);
            Vector3 H(float t) => c + side * 16f * Mathf.Pow(Mathf.Sin(t), 3f) * s / 16f + Vector3.up * (13f * Mathf.Cos(t) - 5f * Mathf.Cos(2f * t) - 2f * Mathf.Cos(3f * t) - Mathf.Cos(4f * t)) * s / 16f;
            for (var k = 0; k < 16; k++) { var t0 = k * Mathf.PI * 2f / 16f; var t1 = (k + 1) * Mathf.PI * 2f / 16f; mk.Tri(m, c, H(t0), H(t1), c - face); mk.Tri(m, c, H(t0), H(t1), c + face); }
        }

        // ---------------------------------------------------------------- everyone else: one thing from their song
        void Others(Plot z, PlaceFrame f)
        {
            var up = Vector3.up; var rot = z.Rot; var rng = new System.Random(z.a.name.Length * 7 + z.a.name[0]); var c = z.At(1.8f, -1.6f) + up * .05f; var fr = z.front; var sd = z.side;
            void Post(Vector3 at, float h, float r = .04f) { z.mk.Prism(chrome, at - up * .2f, up, r, h + .2f, 5); ArtistBits.PostCollider(z.root, at, h, r + .03f); }
            void Plinth(Vector3 at, float w, float h) { z.mk.Box(marble, at + up * (h / 2f), new Vector3(w, h, w), rot); BoxCol(z.root, at + up * (h / 2f), new Vector3(w, h, w), rot); }
            string Any() => SongIds.TryGetValue(z.a.name, out var l) && l.Length > 0 ? Song(z, l[0].title) : null;
            switch (z.a.name)
            {
                case "Stantough": case "Beedle The Bardcore": { var s = Any(); var st = c; z.mk.Prism(Paint(new Color(.5f, .36f, .26f)), st + up * .45f, up, .24f, .06f, 10); for (var k = 0; k < 3; k++) z.mk.Prism(Paint(new Color(.5f, .36f, .26f)), st + Quaternion.Euler(0, k * 120f, 0) * fr * .18f, up, .025f, .45f, 4); Interact.Seat(tlife.transform, st + up * .52f, fr, "Sit on the bard's stool").seatHeight = .45f; z.mk.Blob(SongGlow(s, new Color(.8f, .56f, .3f), .6f), st + fr * .6f + up * .6f, new Vector3(.24f, .3f, .1f), rng, 0f); z.mk.Box(Paint(new Color(.5f, .36f, .26f)), st + fr * .6f + up * 1.1f, new Vector3(.05f, .6f, .04f), rot * Quaternion.Euler(0, 0, -20f)); Post(st + sd * 1.4f, 2.4f, .03f); z.mk.Box(Paint(new Color(.5f, .2f, .5f)), st + sd * 1.4f + up * 1.9f + fr * .02f, new Vector3(.8f, 1.1f, .02f), rot); z.mk.Tri(Paint(new Color(.5f, .2f, .5f)), st + sd * 1.0f + up * 1.35f, st + sd * 1.8f + up * 1.35f, st + sd * 1.4f + up * 1.0f, st + fr); z.mk.Prism(white, st - sd * .8f, up, .04f, .2f, 6); z.mk.Blob(GlowM(new Color(1f, .7f, .3f), 2f), st - sd * .8f + up * .26f, new Vector3(.02f, .05f, .02f), rng, 0f); string[] lute = { "You strum the lute. A modern song comes out in a medieval accent. Thou art lost in the music, verily.", "Hey nonny nonny. Somebody throws a coin.", "Your fingers know a chord they've never played. Ye olde muscle memory." }; var ln = 0; Spot("The lute", st + fr * .6f + up * .8f, "Strum the lute", new Vector3(.6f, .8f, .6f)).onUse = _ => { life.played = Time.time; Say1(st + up * 2.2f, lute[ln++ % lute.Length]); }; break; }
                case "The Little Singers": { var cj = Song(z, "Conjunction"); var jb = Song(z, "I'm Just a Bill"); Post(c - sd * .6f, 2.2f, .04f); z.mk.Box(SongGlow(cj, Sunny, .6f), c - sd * .6f + up * 2f, new Vector3(1.6f, .4f, .04f), rot); ArtistBits.Label(z.labels, "AND · BUT · OR", c - sd * .6f + up * 2f + fr * .03f, fr, .12f, Ink, TextAnchor.MiddleCenter); var sc = c + sd * 1.2f; Plinth(sc, .5f, .8f); z.mk.Prism(SongGlow(jb, new Color(1f, .96f, .86f), .6f), sc + up * .86f - sd * .3f, sd, .08f, .6f, 8); break; }
                case "Zac Efron": case "Troy": { var s = Any(); var bk = c; z.mk.Prism(chrome, bk, up, .06f, 3f, 5); ArtistBits.PostCollider(z.root, bk, 3f, .08f); z.mk.Box(white, bk + up * 3.3f + fr * .1f, new Vector3(1.2f, .8f, .05f), rot); for (var k = 0; k < 10; k++) { var a = Quaternion.Euler(0, k * 36f, 0); z.mk.Box(SongGlow(k == 0 ? s : null, new Color(1f, .5f, .2f), .7f), bk + up * 3f + fr * .5f + a * Vector3.forward * .22f, new Vector3(.1f, .02f, .02f), a); } var ball = new MeshKit(); ball.Blob(Paint(new Color(.9f, .45f, .15f)), new Vector3(0, .13f, 0), Vector3.one * .13f, rng, 0f); Thing(ball, "Basketball", "a basketball, Wildcats red", bk + fr * 1.4f + up * .02f, Quaternion.identity, new Vector3(.27f, .27f, .27f), .5f, false); break; }
                case "90s Classics": { var s = Song(z, "Everybody's Free"); life.sunSong = s; Plinth(c, .6f, .8f); z.mk.Prism(SongGlow(s, Sunny, .8f), c + up * .8f, up, .14f, .5f, 8); z.mk.Prism(white, c + up * 1.3f, up, .06f, .12f, 6); break; }
                case "Brigham Phillips": { var s = Song(z, "Ratlin"); z.mk.Prism(SongGlow(s, new Color(.4f, .5f, .3f), .3f), c + up * .01f, up, 1.4f, .03f, 14); z.mk.Prism(Paint(new Color(.4f, .3f, .24f)), c, up, .12f, 1.8f, 6, .6f); for (var k = 0; k < 3; k++) z.mk.Prism(Paint(new Color(.4f, .3f, .24f)), c + up * (1f + k * .3f), (Quaternion.Euler(0, k * 120f, 0) * fr + up).normalized, .05f, .7f, 4); z.mk.Blob(Paint(new Color(.3f, .3f, .3f)), c + up * 1.7f + fr * .3f, new Vector3(.2f, .14f, .14f), rng, 0f); ArtistBits.PostCollider(z.root, c, 1.8f, .14f); break; }
                case "Burl Ives": { var s = Song(z, "A Holly Jolly"); life.xmasSong = s; for (var k = 0; k < 3; k++) z.mk.Blob(white, c + up * (.4f + k * .7f - k * k * .05f), Vector3.one * (.5f - k * .12f), rng, 0f); z.mk.Cone(Paint(new Color(1f, .5f, .2f)), c + up * 1.8f + fr * .3f, .04f, .02f, 4); z.mk.Prism(SongGlow(s, new Color(.9f, .2f, .2f), .5f), c + up * 1.2f, up, .32f, .1f, 10); z.mk.Prism(dark, c + up * 2.2f, up, .22f, .3f, 8); ArtistBits.PostCollider(z.root, c, 2f, .5f); break; }
                case "CHAD": { var s = Any(); Post(c, 1.8f, .04f); z.mk.Prism(white, c + up * 2f - fr * .03f, fr, .5f, .06f, 16); z.mk.Prism(SongGlow(s, new Color(.9f, .2f, .2f), .9f), c + up * 2f, fr, .48f, .04f, 16); z.mk.Box(white, c + up * 2f + fr * .05f, new Vector3(.8f, .1f, .01f), rot * Quaternion.Euler(0, 0, 45f)); ArtistBits.Label(z.labels, "♪", c + up * 2f + fr * .06f, fr, .3f, Ink, TextAnchor.MiddleCenter); break; }
                case "CUNST": { var s = Any(); z.mk.Prism(Paint(Sunny), c, up, .5f, .1f, 10); z.mk.Prism(Paint(Sunny), c + up * 1.8f, up, .5f, .1f, 10); z.mk.Prism(glassM, c + up * .1f, up, .44f, .85f, 10, .1f); z.mk.Prism(glassM, c + up * 1.8f, -up, .44f, .85f, 10, .1f); z.mk.Cone(SongGlow(s, Lilac, .8f), c + up * .1f, .36f, .36f, 8); BoxCol(z.root, c + up * .9f, new Vector3(1f, 1.9f, 1f), rot); break; }
                case "Camilla and the Chickens": { var s = Any(); var cb = c + up * .4f; var cm = Paint(Color.white); z.mk.Blob(cm, cb, new Vector3(.3f, .3f, .4f), rng, 0f); z.mk.Blob(cm, cb + up * .36f + fr * .26f, Vector3.one * .16f, rng, 0f); z.mk.Box(SongGlow(s, new Color(.9f, .2f, .2f), .7f), cb + up * .56f + fr * .26f, new Vector3(.06f, .14f, .14f), rot); z.mk.Cone(Paint(Sunny), cb + up * .36f + fr * .44f, .05f, .02f, 4); foreach (var e in new[] { -.1f, .1f }) z.mk.Prism(Paint(Sunny), cb + sd * e, -up, .02f, .4f, 3); break; }
                case "DJ Chipman": { var s = Any(); Plinth(c, .7f, .7f); z.mk.Prism(SongGlow(s, new Color(.8f, .56f, .3f), .5f), c + up * .7f - sd * .14f, up, .14f, .3f, 10); z.mk.Prism(Paint(new Color(.9f, .3f, .3f)), c + up * 1f - sd * .14f, up, .15f, .04f, 10); var bn = new MeshKit(); for (var k = 0; k < 5; k++) bn.Blob(Paint(Sunny), new Vector3(0, .05f, -.2f + k * .1f) + Vector3.up * Mathf.Sin(k * .7f) * .04f, new Vector3(.06f, .05f, .07f), rng, 0f); Thing(bn, "Banana", "a banana. It's peanut butter jelly time", c + up * .72f + sd * .2f, rot, new Vector3(.14f, .12f, .5f), .2f, false); break; }
                case "Double Take": { var s = Any(); Post(c, 1.6f, .03f); var sm = SongGlow(s, new Color(1f, .7f, .2f), 1.4f); z.mk.Prism(sm, c + up * 2f - fr * .03f, fr, .5f, .06f, 16); for (var k = 0; k < 8; k++) { var a = Quaternion.AngleAxis(k * 45f, fr); z.mk.Box(sm, c + up * 2f + a * up * .8f, new Vector3(.08f, .3f, .04f), a * rot); } foreach (var e in new[] { -1f, 1f }) z.mk.Box(dark, c + up * 2.08f + sd * e * .18f + fr * .05f, new Vector3(.26f, .14f, .02f), rot); break; }
                case "Ed Helms": { var s = Any(); Plinth(c, .6f, .8f); var th = SongGlow(s, Pink, .7f); for (var k = 0; k < 4; k++) z.mk.Blob(th, c + up * (1f + k * .14f) + sd * Mathf.Sin(k) * .1f, new Vector3(.18f, .08f, .18f), rng, .3f); z.mk.Prism(th, c + up * 1.5f, (up + sd).normalized, .04f, .4f, 4); break; }
                case "Erika Henningsen": { var s = Any(); Plinth(c, .7f, .8f); z.mk.Box(SongGlow(s, Pink, .7f), c + up * .9f, new Vector3(.4f, .14f, .54f), rot * Quaternion.Euler(0, 20f, 0)); Heart(z.mk, Paint(new Color(.9f, .2f, .4f)), c + up * .98f, up, .12f); break; }
                case "First of October": { var s = Any(); Plinth(c, .7f, .8f); z.mk.Prism(white, c + up * .8f, up, .3f, .03f, 14); for (var k = 0; k < 5; k++) z.mk.Box(SongGlow(k == 0 ? s : null, new Color(1f, .86f, .5f), .4f), c + up * .86f + Quaternion.Euler(0, k * 72f, 0) * fr * .15f, new Vector3(.12f, .04f, .12f), rot * Quaternion.Euler(0, k * 30f, 0)); z.mk.Blob(Paint(new Color(.8f, .2f, .14f)), c + up * .9f, new Vector3(.14f, .03f, .14f), rng, .3f); break; }
                case "Hampton The Hampster": { var s = Song(z, "The HampsterDance"); life.hampSong = s; var wc = c + up * 1.1f; var wm = SongGlow(s, new Color(1f, .6f, .9f), .7f); for (var k = 0; k < 16; k++) { var a0 = k * 22.5f * Mathf.Deg2Rad; var a1 = (k + 1) * 22.5f * Mathf.Deg2Rad; foreach (var e in new[] { -.4f, .4f }) { var p0 = wc + sd * e + fr * Mathf.Cos(a0) * 1f + up * Mathf.Sin(a0) * 1f; var p1 = wc + sd * e + fr * Mathf.Cos(a1) * 1f + up * Mathf.Sin(a1) * 1f; z.mk.Prism(wm, p0, (p1 - p0).normalized, .04f, Vector3.Distance(p0, p1), 4); } if (k % 2 == 0) z.mk.Prism(chrome, wc - sd * .4f + fr * Mathf.Cos(a0) + up * Mathf.Sin(a0), sd, .02f, .8f, 3); } foreach (var e in new[] { -.6f, .6f }) z.mk.Prism(chrome, c + sd * e, (up + (e < 0 ? sd : -sd) * .1f).normalized, .04f, 1.1f, 4); z.mk.Blob(Paint(new Color(.9f, .7f, .5f)), wc - up * .8f, new Vector3(.22f, .18f, .3f), rng, 0f); BoxCol(z.root, wc, new Vector3(1f, 2.2f, 2.2f), rot); break; }
                case "Joe Siris": { var s = Any(); z.mk.Prism(SongGlow(s, new Color(.4f, .6f, .9f), .5f), c + up * .05f, up, .3f, .14f, 12, 1.2f); z.mk.Prism(Paint(new Color(.5f, .7f, 1f)), c + up * .16f, up, .26f, .02f, 12); break; }
                case "John Cena": { var s = Any(); Plinth(c, .9f, .7f); z.mk.Box(SongGlow(s, Sunny, 1f), c + up * .9f, new Vector3(.8f, .44f, .08f), rot); z.mk.Box(Paint(new Color(.2f, .2f, .22f)), c + up * .9f - fr * .02f, new Vector3(1.6f, .24f, .04f), rot); halos.Add(c + up * .9f, 1f, Sunny, .3f); break; }
                case "Kahoot!": { var s = Song(z, "Lobby Music"); life.kahootSong = s; var cols = new[] { new Color(.9f, .2f, .3f), new Color(.2f, .4f, .9f), new Color(1f, .7f, .1f), new Color(.2f, .7f, .3f) }; for (var k = 0; k < 4; k++) { var bp = c + sd * ((k % 2) * 1.1f - .55f) + up * (1.6f - (k / 2) * 1f); z.mk.Box(k == 0 ? SongGlow(s, cols[k], .8f) : Paint(cols[k]), bp, new Vector3(1f, .9f, .1f), rot); } foreach (var e in new[] { -1.2f, 1.2f }) Post(c + sd * e, 2.2f, .04f); break; }
                case "LeviathanJPTV": { var s = Any(); Plinth(c, .7f, .7f); z.mk.Prism(SongGlow(s, new Color(.5f, .3f, .9f), .9f), c + up * .7f, up, .24f, .5f, 10, .6f); z.mk.Prism(Paint(new Color(.3f, .2f, .6f)), c + up * 1.2f, up, .1f, .14f, 8); z.mk.Prism(Paint(new Color(.3f, .2f, .6f)), c + up * .96f + sd * .24f, sd, .04f, .16f, 4); break; }
                case "MicroDragon": { var s = Any(); Plinth(c, .8f, .8f); z.mk.Prism(glassM, c + up * .8f, up, .2f, .3f, 8, 1.4f); z.mk.Prism(glassM, c + up * 1.1f, up, .06f, .3f, 6); z.mk.Prism(SongGlow(s, new Color(.3f, 1f, .5f), 1.2f), c + up * .82f, up, .18f, .14f, 8, 1.3f); life.nyeSong = s; break; }
                case "Nathan Evans": { var s = Song(z, "Wellerman"); life.wellerSong = s; z.mk.Box(Paint(new Color(.5f, .36f, .26f)), c + up * .4f, new Vector3(.6f, .5f, 1.8f), rot * Quaternion.Euler(0, 30f, 0)); z.mk.Prism(Paint(new Color(.5f, .36f, .26f)), c + up * .6f, up, .04f, 1.6f, 4); z.mk.Tri(white, c + up * 2.2f, c + up * .9f, c + up * .9f + (rot * Quaternion.Euler(0, 30f, 0)) * Vector3.forward * .8f, c + sd); for (var k = 0; k < 3; k++) z.mk.Prism(k == 0 ? SongGlow(s, new Color(.7f, .5f, .3f), .5f) : woodM(), c + sd * 1.2f + fr * (k * .5f - .5f), up, .2f, .5f, 8); BoxCol(z.root, c + up * .4f, new Vector3(1f, .8f, 2f), rot); break; }
                case "PSY": { var s = Song(z, "Gangnam"); life.gangnamSong = s; Plinth(c, 1.2f, .3f); var hm = SongGlow(s, new Color(.6f, .5f, .4f), .4f); var hb = c + up * 1.3f; z.mk.Blob(hm, hb, new Vector3(.3f, .34f, .7f), rng, 0f); z.mk.Prism(hm, hb + fr * .5f, (fr + up).normalized, .14f, .6f, 6, .7f); z.mk.Box(hm, hb + fr * .9f + up * .5f, new Vector3(.16f, .2f, .4f), rot * Quaternion.Euler(30f, 0, 0)); foreach (var e in new[] { -1f, 1f }) foreach (var q in new[] { -.4f, .4f }) z.mk.Prism(hm, hb + fr * q + sd * e * .14f, -up, .05f, .7f, 4); z.mk.Box(Paint(new Color(.2f, .3f, .8f)), hb + up * .5f, new Vector3(.4f, .5f, .3f), rot); break; }
                case "Pink Guy": { var s = Any(); z.mk.Prism(SongGlow(s, Pink, .5f), c, up, .2f, 1.2f, 8); z.mk.Blob(SongGlow(s, Pink, .5f), c + up * 1.4f, Vector3.one * .22f, rng, 0f); ArtistBits.PostCollider(z.root, c, 1.6f, .24f); break; }
                case "Sharpay Evans": { var s = Song(z, "Fabulous"); life.fabSong = s; var lc = c; z.mk.Box(Paint(Pink), lc + up * .3f, new Vector3(.7f, .06f, 1.6f), rot * Quaternion.Euler(-10f, 30f, 0)); z.mk.Box(Paint(Pink), lc + up * .7f - Quaternion.Euler(0, 30f, 0) * fr * .7f, new Vector3(.7f, .06f, .7f), rot * Quaternion.Euler(-55f, 30f, 0)); Interact.Seat(tlife.transform, lc + up * .4f, Quaternion.Euler(0, 30f, 0) * fr, "Lounge fabulously").seatHeight = .3f; z.mk.Prism(SongGlow(s, new Color(.6f, .9f, 1f), .7f), lc + sd * 1.4f + up * .02f, up, .8f, .04f, 14); break; }
                case "Shred Meyer": { var s = Any(); Plinth(c, .6f, .8f); z.mk.Cone(SongGlow(s, Sunny, 1f), c + up * .8f, .12f, .4f, 8); for (var k = 0; k < 6; k++) z.mk.Box(Paint(new[] { Pink, Teal, Sunny }[k % 3]), c + up * (1.3f + k * .06f) + Quaternion.Euler(0, k * 60f, 0) * fr * .2f, new Vector3(.08f, .02f, .2f), Quaternion.Euler(k * 30f, k * 60f, 0)); break; }
                case "The Duck": { var s = Any(); var stall = c; z.mk.Box(Paint(new Color(1f, .9f, .4f)), stall + up * .5f, new Vector3(1.4f, 1f, .6f), rot); BoxCol(z.root, stall + up * .5f, new Vector3(1.4f, 1f, .6f), rot); ArtistBits.Label(z.labels, "LEMONADE", stall + up * .7f + fr * .31f, fr, .12f, Ink, TextAnchor.MiddleCenter); var dk = stall + fr * 1f; var dm = SongGlow(s, Color.white, .4f); z.mk.Blob(dm, dk + up * .2f, new Vector3(.18f, .16f, .26f), rng, 0f); z.mk.Blob(dm, dk + up * .44f - fr * .1f, Vector3.one * .1f, rng, 0f); z.mk.Cone(Paint(new Color(1f, .6f, .2f)), dk + up * .44f - fr * .22f, .04f, .02f, 4); string[] grapes = { "The duck waddles up. \"Got any grapes?\" No. He waddles away. Bum bum bum.", "The duck asks for grapes. Again. You still don't have any.", "\"Hey!\" says the duck. You know what's coming." }; var gn = 0; Spot("The duck", dk + up * .4f, "Talk to the duck", new Vector3(.6f, .6f, .6f)).onUse = _ => Say1(dk + up * 1.4f, grapes[gn++ % grapes.Length]); break; }
                case "The Gregory Brothers": { var s = Any(); z.mk.Box(Paint(new Color(.4f, .4f, .42f)), c + up * .9f, new Vector3(1.8f, 1.8f, 1.8f), rot); BoxCol(z.root, c + up * .9f, new Vector3(1.8f, 1.8f, 1.8f), rot); z.mk.Box(dark, c + up * .6f + fr * .91f, new Vector3(.8f, 1.2f, .02f), rot); z.mk.Prism(Paint(new Color(.5f, .36f, .26f)), c + fr * 1.2f + sd * .5f, (up + fr * .4f).normalized, .03f, .9f, 4); z.mk.Box(SongGlow(s, new Color(.6f, .9f, 1f), .7f), c + fr * 1.2f + sd * .5f + (up + fr * .4f).normalized * .9f, new Vector3(.7f, .1f, .08f), rot); break; }
                case "The Infield Boys": { var s = Any(); z.mk.Box(SongGlow(s, white.color, .4f), c + up * .03f, new Vector3(.5f, .04f, .5f), rot * Quaternion.Euler(0, 45f, 0)); for (var k = 0; k < 3; k++) z.mk.Box(white, c + up * .03f + Quaternion.Euler(0, k * 90f - 45f, 0) * fr * 4f, new Vector3(.4f, .06f, .4f), Quaternion.Euler(0, 45f, 0)); break; }
                case "The Irish Rovers": { var s = Song(z, "Drunken Sailor"); life.sailorSong = s; Post(c, 1.2f, .05f); var wc = c + up * 1.5f; z.mk.Prism(SongGlow(s, new Color(.6f, .44f, .3f), .4f), wc - fr * .06f, fr, .6f, .12f, 16); z.mk.Prism(Paint(new Color(.94f, .9f, .82f)), wc - fr * .07f, fr, .5f, .14f, 16); for (var k = 0; k < 8; k++) z.mk.Box(woodM(), wc + Quaternion.AngleAxis(k * 45f, fr) * up * .5f, new Vector3(.06f, 1.2f, .06f), Quaternion.AngleAxis(k * 45f, fr) * rot); break; }
                case "The Moonies": { var s = Any(); var sc = c; z.mk.Box(SongGlow(s, new Color(.4f, .9f, .3f), .5f), sc + up * .5f, new Vector3(.4f, .3f, 1.2f), rot); z.mk.Prism(chrome, sc + up * .5f + fr * .5f, up, .03f, .7f, 4); z.mk.Prism(chrome, sc + up * 1.2f + fr * .5f, sd, .02f, .6f, 4); z.mk.Prism(chrome, sc + up * 1.2f + fr * .5f - sd * .3f, sd, .02f, .6f, 4); foreach (var e in new[] { -.5f, .5f }) z.mk.Prism(dark, sc + up * .2f + fr * e - sd * .06f, sd, .2f, .12f, 10); BoxCol(z.root, sc + up * .5f, new Vector3(.6f, 1f, 1.4f), rot); Interact.Seat(tlife.transform, sc + up * .8f - fr * .2f, fr, "Sit on the scooter").seatHeight = .3f; break; }
                case "The Muppets Barbershop Quartet": { var s = Any(); for (var k = 0; k < 4; k++) { var hp = c + sd * (-1.2f + k * .8f); z.mk.Prism(Paint(new Color(.9f, .86f, .4f)), hp + up * 1.4f, up, .2f, .04f, 12); z.mk.Prism(Paint(new Color(.9f, .86f, .4f)), hp + up * 1.4f, up, .12f, .14f, 10); z.mk.Prism(k == 0 ? SongGlow(s, new Color(.9f, .2f, .2f), .5f) : Paint(new Color(.9f, .2f, .2f)), hp + up * 1.44f, up, .13f, .03f, 10); Post(hp, 1.3f, .03f); } break; }
                case "Vincent E. L.": { var s = Any(); var hy = c; z.mk.Prism(Paint(new Color(.9f, .2f, .2f)), hy, up, .13f, .56f, 8); z.mk.Blob(Paint(new Color(.9f, .2f, .2f)), hy + up * .6f, new Vector3(.14f, .1f, .14f), rng, 0f); z.mk.Prism(SongGlow(s, new Color(.6f, .9f, 1f), .8f), hy + up * .36f + sd * .14f, (sd + up * .6f).normalized, .05f, 1.6f, 5, 1.8f); ArtistBits.PostCollider(z.root, hy, .7f, .16f); break; }
                case "Wesley Willis": { var s = Any(); Plinth(c, .8f, .8f); var bb = c + up * .8f; z.mk.Prism(Paint(new Color(.86f, .6f, .3f)), bb, up, .3f, .1f, 12); z.mk.Prism(Paint(new Color(.5f, .3f, .2f)), bb + up * .1f, up, .32f, .1f, 12); z.mk.Prism(Paint(new Color(.4f, .8f, .3f)), bb + up * .2f, up, .33f, .02f, 12); z.mk.Blob(SongGlow(s, new Color(.9f, .66f, .34f), .4f), bb + up * .3f, new Vector3(.3f, .1f, .3f), rng, 0f); break; }
                case "Write Out Loud": { var s = Any(); z.mk.Box(Paint(Sunny), c + up * 1.2f, new Vector3(1f, 2f, .08f), rot); z.mk.Box(SongGlow(s, new Color(.8f, .9f, 1f), .6f), c + up * 1.2f + fr * .05f, new Vector3(.8f, 1.8f, .02f), rot); BoxCol(z.root, c + up * 1.2f, new Vector3(1f, 2.2f, .2f), rot); for (var k = 0; k < 5; k++) z.mk.Cone(Paint(Sunny), c + up * 2.24f + sd * (-.24f + k * .12f), .04f, .14f, 4); break; }
                case "yums": { var s = Any(); Post(c, 3f, .04f); z.mk.Box(SongGlow(s, new Color(.2f, .4f, .9f), .5f), c + up * 2.6f + sd * .7f, new Vector3(1.4f, .9f, .02f), rot); z.mk.Box(white, c + up * 2.6f + sd * .7f + fr * .02f, new Vector3(1.6f, .14f, .01f), rot * Quaternion.Euler(0, 0, 33f)); z.mk.Box(white, c + up * 2.6f + sd * .7f + fr * .02f, new Vector3(1.6f, .14f, .01f), rot * Quaternion.Euler(0, 0, -33f)); break; }
                case "Geek Music": { var s = Any(); var bf = new PlaceFrame(c - fr * .4f, fr); Post(bf.P(-1.8f, 0, 0), 2.6f, .05f); Post(bf.P(1.8f, 0, 0), 2.6f, .05f); for (var x = 0; x < 6; x++) for (var y = 0; y < 4; y++) { var tile = bf.P(-1.5f + x * .6f, 1.1f + y * .4f, .03f); z.mk.Box(x == 0 && y == 0 ? SongGlow(s, new Color(.3f, .4f, 1f), 1f) : GlowM(new Color(.2f, .3f, .9f), .7f), tile, new Vector3(.56f, .36f, .02f), bf.r); ArtistBits.Label(z.labels, "$" + ((4 - y) * 200), tile + bf.Fwd * .02f, bf.Fwd, .08f, new Color(1f, .82f, .3f), TextAnchor.MiddleCenter); } z.mk.Box(dark, bf.P(0, 1.8f, -.03f), new Vector3(3.8f, 1.8f, .04f), bf.r); BoxCol(z.root, bf.P(0, 1.6f, 0), new Vector3(3.8f, 2f, .2f), bf.r); break; }
                case "Mark Snow": { var s = Any(); var ufo = c + up * 4.4f; z.mk.Blob(Paint(new Color(.6f, .62f, .66f)), ufo, new Vector3(1.8f, .3f, 1.8f), rng, 0f); z.mk.Blob(GlowM(new Color(.5f, 1f, .6f), 1f), ufo + up * .3f, new Vector3(.7f, .4f, .7f), rng, 0f); for (var k = 0; k < 8; k++) z.mk.Blob(GlowM(new Color(.6f, 1f, .6f), 2f), ufo + Quaternion.Euler(0, k * 45f, 0) * Vector3.forward * 1.6f - up * .08f, Vector3.one * .1f, rng, 0f); z.mk.Prism(SongGlow(s, new Color(.5f, 1f, .6f), .3f), c + up * .05f, up, .8f, 4f, 12, .5f, 0f, false); Post(c - fr * 1.6f, 4.2f, .04f); break; }
                case "Mark Governor": { var s = Any(); Post(c, 5f, .1f); var hd = c + up * 5.2f; z.mk.Box(dark, hd, new Vector3(1.6f, 1f, .2f), rot); for (var x = 0; x < 3; x++) for (var y = 0; y < 2; y++) z.mk.Box(lampM, hd + fr * .11f + sd * (x - 1) * .5f + up * (y - .5f) * .44f, new Vector3(.4f, .36f, .02f), rot); foreach (var e in new[] { -1f, 1f }) z.mk.Prism(SongGlow(e > 0 ? s : null, e > 0 ? new Color(.3f, 1f, .4f) : new Color(1f, .3f, .8f), 2.2f), hd, (fr + sd * e * .5f + up * .4f).normalized, .03f, 12f, 4); break; }
                case "Captainsparklez": { var rv = Song(z, "Revenge"); var fk = Song(z, "Fallen Kingdom"); var bs = c - fr * .6f; for (var x = -1; x <= 1; x++) for (var y = 0; y < 2; y++) { if (x == 0 && y == 0) continue; z.mk.Box(Paint(new Color(.5f, .5f, .54f)), bs + sd * x + up * (y + .5f), Vector3.one, rot); } BoxCol(z.root, bs + up * 1f, new Vector3(3f, 2f, 1f), rot); z.mk.Box(SongGlow(fk, new Color(1f, .82f, .2f), 1.4f), bs + up * 2.2f, new Vector3(.5f, .3f, .5f), rot); var tn = c + sd * 2f + fr * .6f; z.mk.Box(Paint(new Color(.86f, .2f, .16f)), tn + up * .5f, Vector3.one, rot); z.mk.Box(white, tn + up * .5f + fr * .51f, new Vector3(1f, .3f, .02f), rot); z.mk.Box(SongGlow(rv, new Color(1f, .5f, .3f), 1.4f), tn + up * 1.15f, new Vector3(.06f, .3f, .06f), rot); BoxCol(z.root, tn + up * .5f, Vector3.one, rot); break; }
                case "Brad Knauber": { var s = Any(); var mn = c - fr * .8f; for (var x = -1; x <= 1; x++) for (var y = 0; y < 3; y++) { if (x == 0 && y < 2) continue; z.mk.Box(y == 2 ? Paint(new Color(.34f, .7f, .3f)) : Paint(new Color(.5f, .5f, .54f)), mn + sd * x + up * (y + .5f), Vector3.one, rot); } z.mk.Box(dark, mn + up * 1f, new Vector3(.98f, 2f, .98f), rot); BoxCol(z.root, mn + up * 1.5f, new Vector3(3f, 3f, 1f), rot); foreach (var e in new[] { -1f, 1f }) { var tp = mn + fr * .6f + sd * e * 1.1f + up * 1.6f; z.mk.Box(trunkM, tp, new Vector3(.12f, .5f, .12f), rot); z.mk.Box(e > 0 ? SongGlow(s, new Color(1f, .7f, .2f), 2f) : GlowM(new Color(1f, .7f, .2f), 2f), tp + up * .3f, Vector3.one * .14f, rot); } break; }
                default: { var s = Any(); Post(c, 1.4f); Bubble(z.mk, c + up * 1.8f, .3f); if (s != null) z.mk.Blob(SongGlow(s, Aqua, 1.4f), c + up * 1.8f, Vector3.one * .12f, rng, 0f); break; }
            }
        }
    }
}
