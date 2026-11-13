using System.Collections.Generic;
using UnityEngine;
using static EverythingWorld.World.PlaceTools;

namespace EverythingWorld.World
{
    /// The Pop Republic's artists. Most have a pop-up stage of their own: a small round stage under an arch of
    /// bulbs, a microphone, a backdrop in a candy colour, and one thing from their song; covers on a cream card on
    /// a white board with a row of bulbs on top. The K-pop groups have booths round K-Pop Square.
    /// - Taylor Swift: the Eras Hall. A long stage with a runway out into the crowd, a sunset arch at the end of it
    ///   ("Cruel Summer"), and behind it a staircase of eras, a dress on every step. Round it: a red scarf on a hook
    ///   and a red maple ("All Too Well", "Red": red leaves fall), a birthday table ("22"), a tiny town you're too big
    ///   for ("Anti-Hero"), a long white wall to write a name on ("Blank Space"), a lilac cat ("Karma"), a bed of
    ///   lavender ("Lavender Haze"), a banjo on a chair ("Mean"), a paper ring on a cushion ("Paper Rings"), an old
    ///   film screen ("Wildest Dreams"), a shovel in fresh earth ("no body, no crime"), a friendship-bracelet stall.
    /// - Sabrina Carpenter: a pink heart-shaped stage edged with bulbs; a playpen full of blocks ("Manchild"); giant
    ///   letters in a heap ("Nonsense"); a pink telephone on a pedestal ("Juno": everything goes pink).
    /// - Katy Perry: firework tubes ("Firework": fireworks over the land), a gold tiger ("Roar"), a black horse on a
    ///   gold plinth ("Dark Horse"), a party's aftermath with a disco ball ("Last Friday Night"), two giant cherries.
    /// - Michael Jackson: a floor of tiles that light up ("Billie Jean"), a graveyard gate ("Thriller": a green night
    ///   falls), a red jacket on a stand ("Beat It"), one sparkling glove.
    public partial class PopKit
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
                Add("Katy Perry", "Firework", "@the plaza"); Add("Taylor Swift", "Cruel Summer", "@the eras hall"); Add("Taylor Swift", "Lavender Haze", "@down the red carpet");
                Add("Taylor Swift", "All Too Well", "@the eras hall"); Add("Michael Jackson", "Thriller", "@the palace"); Add("BTS", "Dynamite", "@k-pop square"); Add("Adele", "Skyfall", "@the plaza");
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
            if (paving == null) MakeMaterials();
            var root = new GameObject("Artist: " + a.name).transform; root.SetParent(ctx.Root, false);
            var front = facing.TryGetValue(a.name, out var f) ? f : new Vector3(center.x - a.x, 0, center.z - a.z).normalized;
            if (front.sqrMagnitude < .01f) front = Vector3.forward;
            var labels = new GameObject("Labels").transform; labels.SetParent(root, false);
            var area = root.gameObject.AddComponent<ArtistArea>();
            area.artistName = a.name; area.stationId = a.stationId; area.radius = a.r; area.glowColor = Pink;
            var z = new Plot { ctx = ctx, root = root, labels = labels, mk = new MeshKit(), solid = new MeshKit(), pictures = new MeshKit(), a = a, c = ctx.OnGround(a.x, a.z), front = front, side = Vector3.Cross(Vector3.up, front), area = area };
            var reach = Foot(a); var up = Vector3.up; var isK = System.Array.IndexOf(KPop, a.name) >= 0;
            z.mk.Prism(isK ? pavingPink : paving, z.c - up * .25f, up, reach * .66f, .33f, 24);
            if (isK && SongIds.TryGetValue(a.name, out var ks)) foreach (var (_, sid) in ks) life.kpop.Add(sid);
            switch (a.name)
            {
                case "Taylor Swift": Taylor(z); break;
                case "Sabrina Carpenter": Sabrina(z); break;
                case "Katy Perry": Katy(z); break;
                case "Michael Jackson": Michael(z); break;
                default: Others(z, isK ? Booth(z) : PopUp(z)); break;
            }
            SongLamps(z);
            Board(z, z.At(reach * .6f, -reach * .45f - 1.5f), front);
            ArtistBits.Plaque(z.mk, z.solid, labels, a, z.At(reach * .6f + 1.5f, reach * .45f + 1.5f), front, white, goldM, Ink, 1.6f);
            StarLamp(ctx, z.mk, z.At(reach * .5f, reach * .68f), a.name.Length);
            if (z.area.glowLight == null) z.area.glowLight = ArtistBits.PointLight(root, z.c + up * 4f + front * 3f, new Color(1f, .9f, .92f), HasStation(a) ? 24f : 15f, .9f);
            area.glowRenderers = new Renderer[0];
            z.mk.Build("Area", root, false);
            z.solid.BuildCollider("Walls", root);
            z.pictures.Build("Covers", root, false);
            noGrass.Add(new Vector3(z.c.x, reach * .9f, z.c.z));
            var mark = new GameObject("Shot: " + a.name + " up close").transform; mark.SetParent(ctx.Root, false);
            var cam = z.At(reach * .85f, reach * .3f) + up * 2f; var look = z.At(-reach * .3f, 0) + up * 2.2f - cam; mark.SetPositionAndRotation(cam, Quaternion.LookRotation(look.normalized));
        }

        /// The covers on a cream card on a white board with a row of bulbs along the top.
        void Board(Plot z, Vector3 ground, Vector3 dir)
        {
            dir = Vector3.ProjectOnPlane(dir, Vector3.up).normalized; var rot = Quaternion.LookRotation(dir); var side = Vector3.Cross(Vector3.up, dir);
            var n = Mathf.Max(1, Mathf.Min(7, z.a.songList?.Length ?? 1)); const float size = .9f; var w = n * size * 1.25f + .6f;
            var c = ground + Vector3.up * 1.7f;
            z.mk.Box(white, c - Vector3.up * .5f - dir * .06f, new Vector3(w + .5f, size + 2f, .2f), rot); z.solid.Box(white, c - Vector3.up * .5f, new Vector3(w + .5f, size + 2f, .3f), rot); z.mk.Box(Paint(new Color(1f, .97f, .9f)), c - Vector3.up * .1f + dir * .07f, new Vector3(w, size + .9f, .04f), rot);
            var nb = Mathf.Max(3, Mathf.RoundToInt(w / .5f)); for (var k = 0; k < nb; k++) z.mk.Blob(bulbM[k % 3], c + Vector3.up * (size / 2f + .58f) + side * (-w / 2f + k * w / (nb - 1)) + dir * .06f, Vector3.one * .07f, new System.Random(k), 0f);
            ArtistBits.CoverWall(z.pictures, z.labels, z.ctx, z.a, c + dir * .1f + Vector3.up * .08f, dir, n, size, n, true, Ink);
        }

        /// A little glowing star on a post for each song without its own piece.
        void SongLamps(Plot z)
        {
            if (!SongIds.TryGetValue(z.a.name, out var list)) return;
            var up = Vector3.up; var k = 0; var seen = new HashSet<string>();
            foreach (var (_, id) in list)
            {
                if (z.lit.Contains(id) || !seen.Add(id)) continue;
                var p = z.At(Foot(z.a) * .55f - (k % 2) * 1.3f, Foot(z.a) * .2f + (k / 2) * 1.3f);
                z.mk.Prism(white, p - up * .1f, up, .035f, 1.3f, 5); Star(z.mk, SongGlow(id, Lemon, 1.6f), p + up * 1.5f, z.front, .22f); ArtistBits.PostCollider(z.root, p, 1.3f, .08f);
                k++;
            }
        }

        /// A pop-up stage of their own: a small round stage under an arch of bulbs, a backdrop, a microphone.
        PlaceFrame PopUp(Plot z)
        {
            var up = Vector3.up; var hue = z.a.name.Length + z.a.name[0]; var f = new PlaceFrame(z.At(-Foot(z.a) * .48f, 0), z.front); var col = Candy[hue % Candy.Length];
            z.mk.Box(Paint(col), f.P(0, 1.8f, -.2f), new Vector3(5f, 3.6f, .3f), f.r); z.solid.Box(white, f.P(0, 1.8f, -.2f), new Vector3(5f, 3.6f, .3f), f.r); z.mk.Box(white, f.P(0, 3.66f, -.2f), new Vector3(5.2f, .16f, .4f), f.r);
            z.mk.Prism(white, f.P(0, .02f, 1.1f), up, 2f, .36f, 18); z.solid.Prism(white, f.P(0, -.2f, 1.1f), up, 2f, .6f, 12); z.mk.Prism(goldM, f.P(0, .38f, 1.1f), up, 2f, .02f, 18);
            for (var k = 0; k <= 12; k++) { var a = Mathf.PI * k / 12f; z.mk.Blob(bulbM[(k + hue) % 3], f.P(Mathf.Cos(a) * 2.3f, .4f + Mathf.Sin(a) * 3.2f, .1f), Vector3.one * .1f, new System.Random(k), 0f); }
            Mic(z.mk, f.P(0, .38f, 1.6f), goldM);
            return f;
        }

        /// An idol booth on K-Pop Square: a lilac backdrop, a little stage, a stand of photocards.
        PlaceFrame Booth(Plot z)
        {
            var up = Vector3.up; var f = new PlaceFrame(z.At(-Foot(z.a) * .48f, 0), z.front);
            z.mk.Box(white, f.P(0, 1.8f, -.2f), new Vector3(5f, 3.6f, .3f), f.r); z.solid.Box(white, f.P(0, 1.8f, -.2f), new Vector3(5f, 3.6f, .3f), f.r); z.mk.Box(GlowM(Lilac, .9f), f.P(0, 2f, -.03f), new Vector3(4.4f, 2.6f, .03f), f.r); ArtistBits.Label(z.labels, z.a.name, f.P(0, 3.2f, .01f), f.Fwd, .3f, White, TextAnchor.MiddleCenter);
            z.mk.Box(dark, f.P(0, .2f, 1f), new Vector3(4.6f, .4f, 2f), f.r); z.solid.Box(dark, f.P(0, 0f, 1f), new Vector3(4.6f, .6f, 2f), f.r); z.mk.Box(GlowM(Lilac, 1.2f), f.P(0, .41f, 1f), new Vector3(4.4f, .02f, 1.8f), f.r);
            var ps = f.P(2.8f, 0, 1.6f); z.mk.Prism(white, ps, up, .04f, 1.3f, 5); for (var k = 0; k < 6; k++) z.mk.Box(Paint(Candy[k]), ps + up * (1f + (k / 3) * .3f) + f.Right * (-.2f + (k % 3) * .2f) + f.Fwd * .05f, new Vector3(.16f, .24f, .01f), f.r); ArtistBits.PostCollider(z.root, ps, 1.3f, .06f);
            return f;
        }

        // ---------------------------------------------------------------- Taylor Swift: the Eras Hall
        void Taylor(Plot z)
        {
            var up = Vector3.up; var rot = z.Rot; var rng = new System.Random(1989); var fr = z.front; var sd = z.side;
            var cs = Song(z, "Cruel Summer"); var atw = Song(z, "All Too Well"); var rd = Song(z, "Red"); var tt = Song(z, "22"); var ah = Song(z, "Anti-Hero"); var bs = Song(z, "Blank Space"); var ka = Song(z, "Karma"); var lh = Song(z, "Lavender Haze"); var mn = Song(z, "Mean"); var pr = Song(z, "Paper Rings"); var wd = Song(z, "Wildest Dreams"); var nb = Song(z, "no body");
            life.cruelSong = cs; life.hazeSong = lh; life.blankSong = bs; life.red.Add(atw); life.red.Add(rd);
            // the main stage, the runway out into the crowd, a round platform at its end
            var st = z.At(-7f, 0) + up * .1f; z.mk.Box(white, st + up * .6f, new Vector3(14f, 1.2f, 5f), rot); z.solid.Box(white, st + up * .5f, new Vector3(14f, 1.4f, 5f), rot); z.mk.Box(GlowM(Lilac, .9f), st + up * 1.21f, new Vector3(13.4f, .02f, 4.4f), rot);
            var rw = st + fr * 7f; z.mk.Box(white, rw + up * .6f, new Vector3(2.6f, 1.2f, 9f), rot); z.solid.Box(white, rw + up * .5f, new Vector3(2.6f, 1.4f, 9f), rot); for (var k = 0; k < 9; k++) z.mk.Box(bulbM[k % 3], rw + up * 1.21f + fr * (-4f + k), new Vector3(2.4f, .02f, .5f), rot);
            var tip = st + fr * 12.4f; z.mk.Prism(white, tip, up, 2.4f, 1.2f, 20); z.solid.Prism(white, tip - up * .2f, up, 2.4f, 1.4f, 12); z.mk.Prism(SongGlow(cs, new Color(1f, .5f, .5f), 1.2f), tip + up * 1.2f, up, 2.2f, .02f, 20);
            for (var k = 0; k < 3; k++) { var sp = st + fr * 2.8f + sd * 4.6f + up * (.2f + k * .36f) - fr * (k * .4f); z.mk.Box(white, sp, new Vector3(1.6f, .4f, .5f), rot); z.solid.Box(white, sp - up * .2f, new Vector3(1.6f, .6f, .5f), rot); }
            // "Cruel Summer": an arch of sunset over the runway's end
            for (var k = 0; k <= 16; k++) { var a = Mathf.PI * k / 16f; var col = Color.Lerp(new Color(1f, .5f, .3f), new Color(1f, .4f, .7f), k / 16f); z.mk.Blob(SongGlow(cs, col, 1.6f), tip + up * (1.2f + Mathf.Sin(a) * 4.4f) + sd * Mathf.Cos(a) * 3.4f, Vector3.one * .26f, rng, 0f); }
            // the staircase of eras behind the stage: a dress on every step
            var eras = new[] { ("debut", new Color(.4f, .8f, .78f)), ("Fearless", new Color(1f, .82f, .36f)), ("Speak Now", new Color(.6f, .36f, .8f)), ("Red", new Color(.86f, .14f, .2f)), ("1989", new Color(.56f, .78f, 1f)), ("reputation", new Color(.12f, .12f, .14f)), ("Lover", new Color(1f, .66f, .84f)), ("folklore", new Color(.72f, .72f, .7f)), ("evermore", new Color(.72f, .46f, .26f)), ("Midnights", new Color(.2f, .24f, .5f)), ("TTPD", new Color(.95f, .94f, .9f)), ("Showgirl", new Color(1f, .56f, .2f)) };
            var stair = st - fr * 3.2f; for (var k = 0; k < eras.Length; k++)
            {
                var sx = (k - 5.5f) * 1.1f; var sy = 1.2f + k * .5f; var sp = stair + sd * sx * 0f - fr * (k * .6f) + up * sy; var (name, col) = eras[k];
                z.mk.Box(white, sp - up * .2f, new Vector3(13f, .4f, .6f), rot); z.solid.Box(white, sp - up * .3f, new Vector3(13f, .6f, .6f), rot);
                var dp = sp + sd * (k % 2 == 0 ? -1f : 1f) * (1.4f + (k % 4) * 1.2f); var dm = k == 3 ? SongGlow(rd, col, .8f) : Paint(col); z.mk.Prism(dm, dp, up, .5f, 1.2f, 10, .3f); z.mk.Prism(dm, dp + up * 1.2f, up, .16f, .5f, 8, 1.3f); z.mk.Prism(white, dp + up * 1.7f, up, .03f, .2f, 4); ArtistBits.Label(z.labels, name, sp + fr * .32f, fr, .12f, Ink, TextAnchor.MiddleCenter);
            }
            z.mk.Box(white, stair + up * (1.2f + 12 * .5f + 1.4f) - fr * 7.8f, new Vector3(14f, 3f, .4f), rot); ArtistBits.Label(z.labels, "THE ERAS HALL", stair + up * (1.2f + 12 * .5f + 1.4f) - fr * 7.56f, fr, .7f, Ink, TextAnchor.MiddleCenter);
            Mic(z.mk, st + up * 1.2f + fr * 1.4f, goldM);
            string[] sing = { "You step out onto the runway and fifty thousand people start the bridge before you do.", "Every era at once. You change costume at the top of the stairs and nobody sees how.", "Somebody in the front row is crying. So is somebody in the back row. So are you, a bit.", "You hold out the mic. They know every word of the ten-minute version. All ten minutes." }; var sn = 0;
            Spot("The Eras stage", st + up * 2.4f + fr * 1.4f, "Take the stage", new Vector3(4f, 1.6f, 2f)).onUse = _ => { life.played = Time.time; Say1(st + up * 3.6f + fr * 1.4f, sing[sn++ % sing.Length]); };
            z.area.glowLight = ArtistBits.PointLight(z.root, st + up * 6f + fr * 6f, new Color(1f, .92f, .96f), 30f, 1.4f); halos.Add(tip + up * 3.4f, 4f, new Color(1f, .5f, .5f), .12f);
            // the crowd round the runway
            for (var k = 0; k < 8; k++) { var e = k % 2 == 0 ? 1f : -1f; Folk(rw + sd * e * (2.6f + (k / 4) * 1.1f) + fr * (-3f + (k / 2 % 2) * 3.5f) - up * .1f, -sd * e, k % 3 == 0 ? Resident.Pose.Stand : Resident.Pose.Wave, new[] { "I traded bracelets with forty people today. FORTY.", "Which era are you? Don't answer, I can tell. It's the one you're wearing.", "Shh, it's the surprise song. It's always the surprise song." }); }
            // "All Too Well" and "Red": a red scarf on a hook; a red maple dropping leaves
            var hk = z.At(4f, -12f); z.mk.Prism(white, hk - up * .2f, up, .05f, 2.2f, 5); z.mk.Prism(white, hk + up * 1.9f, fr, .03f, .3f, 4); var scarf = SongGlow(atw, RedC, .9f); z.mk.Box(scarf, hk + up * 1.4f + fr * .3f, new Vector3(.2f, 1f, .04f), rot); z.mk.Box(scarf, hk + up * 1.3f + fr * .34f + sd * .14f, new Vector3(.2f, 1.2f, .04f), rot * Quaternion.Euler(0, 0, 8f)); ArtistBits.PostCollider(z.root, hk, 2f, .07f);
            var mp = z.At(6.6f, -14.4f); z.mk.Prism(Paint(new Color(.4f, .28f, .22f)), mp - up * .2f, up, .2f, 3f, 6, .6f); ArtistBits.PostCollider(z.root, mp, 2.6f, .26f); for (var k = 0; k < 5; k++) z.mk.Blob(k % 2 == 0 ? SongGlow(rd, new Color(.9f, .18f, .14f), .5f) : Paint(new Color(.95f, .4f, .2f)), mp + up * (3.4f + (k % 2) * .6f) + Quaternion.Euler(0, k * 72f, 0) * Vector3.forward * (k == 0 ? 0f : 1.3f), new Vector3(1.6f, 1.2f, 1.6f), rng, .2f); for (var k = 0; k < 10; k++) z.mk.Box(Paint(new Color(.9f, .3f, .16f)), mp + up * .03f + Quaternion.Euler(0, k * 37f, 0) * Vector3.forward * (1f + (k % 3) * .7f), new Vector3(.2f, .01f, .16f), Quaternion.Euler(0, k * 51f, 0));
            // "22": a birthday table, a cake, party hats
            var bt = z.At(9f, -7f) + up * .08f; z.mk.Prism(white, bt + up * .72f, up, .7f, .05f, 14); z.mk.Prism(white, bt, up, .05f, .72f, 5); BoxCol(z.root, bt + up * .38f, new Vector3(1.4f, .76f, 1.4f), rot); z.mk.Prism(Paint(new Color(1f, .8f, .88f)), bt + up * .77f, up, .3f, .24f, 12); for (var k = 0; k < 2; k++) { z.mk.Prism(white, bt + up * 1.01f + sd * (k * .14f - .07f), up, .015f, .12f, 4); z.mk.Blob(SongGlow(tt, Lemon, 1.6f), bt + up * 1.15f + sd * (k * .14f - .07f), new Vector3(.02f, .04f, .02f), rng, 0f); } for (var k = 0; k < 3; k++) z.mk.Cone(Paint(Candy[k]), bt + up * .77f + Quaternion.Euler(0, k * 120f, 0) * Vector3.forward * .5f, .08f, .22f, 6);
            // "Anti-Hero": a tiny town you're far too big for
            var tw = z.At(10f, 4f) + up * .08f; for (var k = 0; k < 7; k++) { var hp = tw + Quaternion.Euler(0, k * 51f, 0) * Vector3.forward * (.6f + (k % 3) * .5f); var hc = Paint(Candy[k % Candy.Length]); z.mk.Box(hc, hp + up * .18f, new Vector3(.3f, .36f, .3f), Quaternion.Euler(0, k * 20f, 0)); z.mk.GableRoof(k == 3 ? SongGlow(ah, new Color(.9f, .3f, .3f), .7f) : Paint(new Color(.6f, .3f, .3f)), hp + up * .36f, new Vector3(.36f, 0, .36f), .16f, Quaternion.Euler(0, k * 20f, 0)); } z.mk.Prism(leafM, tw + up * .01f, up, 1.8f, .02f, 14); BoxCol(z.root, tw + up * .2f, new Vector3(2.4f, .4f, 2.4f), rot);
            // "Blank Space": a long white wall and a gold pen; write a name
            var bw = new PlaceFrame(z.At(5f, 11.6f), (-fr - sd * .3f).normalized); z.mk.Box(white, bw.P(0, 1.3f, 0), new Vector3(6f, 2.4f, .3f), bw.r); z.solid.Box(white, bw.P(0, 1.3f, 0), new Vector3(6f, 2.4f, .3f), bw.r); z.mk.Box(SongGlow(bs, White, .5f), bw.P(0, 1.3f, .16f), new Vector3(5.6f, 2f, .02f), bw.r); z.mk.Box(goldM, bw.P(0, .7f, .3f), new Vector3(1.2f, .05f, .3f), bw.r); z.mk.Prism(goldM, bw.P(.2f, .74f, .3f), bw.Right, .02f, .3f, 6);
            string[] names = { "You write a name. It looks wonderful. You add a little heart over the i, though there's no i in it.", "You write a name, cross it out, write it again bigger.", "The space is blank again the moment you look away. Got a long list, after all." }; var nn = 0;
            Spot("The blank wall", bw.P(0, 1.2f, .7f), "Write a name", new Vector3(3f, 1.4f, .8f)).onUse = _ => Say1(bw.P(0, 2.9f, .4f), names[nn++ % names.Length]);
            // "Karma": a lilac cat, purring
            var kc = z.At(10.4f, 10f) + up * .08f; Plinth(z, kc, .9f, .5f); var cat = SongGlow(ka, Lilac, 1.1f); z.mk.Blob(cat, kc + up * .74f, new Vector3(.3f, .38f, .32f), rng, 0f); z.mk.Blob(cat, kc + up * 1.2f + fr * .06f, Vector3.one * .2f, rng, 0f); foreach (var e in new[] { -1f, 1f }) z.mk.Cone(cat, kc + up * 1.34f + sd * e * .1f + fr * .06f, .06f, .14f, 4); z.mk.Prism(cat, kc + up * .55f - fr * .24f, (sd + up * .6f).normalized, .05f, .6f, 5);
            string[] purr = { "Purrrr. Karma's a cat, and this one likes you.", "The cat looks at you like it knows everything you've ever done. It's fine. It purrs anyway.", "A slow blink. You're on the good list." }; var pn = 0; Spot("Karma the cat", kc + up * 1f, "Stroke the cat", new Vector3(.8f, .8f, .8f)).onUse = _ => Say1(kc + up * 2f, purr[pn++ % purr.Length]);
            // "Lavender Haze": a bed of lavender
            var lv = z.At(-2f, -14f) + up * .08f; z.mk.Prism(Paint(new Color(.5f, .4f, .34f)), lv, up, 2.2f, .14f, 14); for (var k = 0; k < 30; k++) { var lp = lv + Quaternion.Euler(0, k * 47f, 0) * Vector3.forward * (.3f + (k % 6) * .32f); z.mk.Prism(leafMint, lp + up * .14f, up, .015f, .4f, 3); z.mk.Prism(k % 4 == 0 ? SongGlow(lh, Lilac, 1f) : Paint(Lilac), lp + up * .5f, up, .045f, .22f, 4, .4f); }
            // "Mean": a banjo on a chair
            var bj = z.At(-4f, 13f) + up * .08f; Chair(z.mk, bj, (fr + sd * .2f).normalized); z.mk.Prism(Paint(new Color(.96f, .94f, .88f)), bj + up * .62f + fr * .1f, (fr + up * 1.3f).normalized, .2f, .06f, 14); z.mk.Prism(SongGlow(mn, Gold, .6f), bj + up * .62f + fr * .1f, (up + fr * .3f).normalized, .025f, .9f, 4);
            // "Paper Rings": a paper ring on a cushion
            var rg = z.At(1.6f, 15f) + up * .08f; Plinth(z, rg, .6f, 1f); z.mk.Prism(Paint(Pink), rg + up * 1f, up, .24f, .08f, 10); var ring = SongGlow(pr, White, 1.2f); for (var k = 0; k < 12; k++) { var a = Quaternion.AngleAxis(k * 30f, sd); z.mk.Box(ring, rg + up * 1.24f + a * up * .12f, new Vector3(.03f, .065f, .03f), a * rot); }
            // "Wildest Dreams": an old film screen with sepia light
            var fs = new PlaceFrame(z.At(-10f, 12f), (fr - sd * .4f).normalized); foreach (var e in new[] { -1.8f, 1.8f }) { z.mk.Prism(Paint(new Color(.4f, .3f, .22f)), fs.P(e, -.2f, 0), up, .07f, 3.4f, 5); ArtistBits.PostCollider(z.root, fs.P(e, 0, 0), 3f, .09f); } z.mk.Box(SongGlow(wd, new Color(.9f, .74f, .5f), .9f), fs.P(0, 2f, 0), new Vector3(3.4f, 2f, .05f), fs.r); z.mk.Box(dark, fs.P(0, 3.06f, 0), new Vector3(3.6f, .12f, .1f), fs.r);
            // "no body, no crime": a shovel left in fresh earth
            var sh = z.At(-9f, -12f) + up * .02f; z.mk.Blob(Paint(new Color(.4f, .28f, .2f)), sh + up * .1f, new Vector3(1f, .26f, .7f), rng, .1f); z.mk.Prism(Paint(new Color(.5f, .36f, .24f)), sh + up * .2f, (up + sd * .2f).normalized, .03f, 1.1f, 4); z.mk.Box(SongGlow(nb, new Color(.7f, .72f, .76f), .4f), sh + up * .24f, new Vector3(.26f, .3f, .03f), rot);
            // the friendship-bracelet stall
            var bk = new PlaceFrame(z.At(9f, 0f), -sd); z.mk.Box(Paint(Pink), bk.P(0, .5f, 0), new Vector3(2.4f, 1f, .9f), bk.r); z.mk.Box(white, bk.P(0, 1.02f, 0), new Vector3(2.5f, .05f, 1f), bk.r); Col(bk.P(0, .5f, 0), new Vector3(2.4f, 1f, .9f), bk.r); foreach (var e in new[] { -1.1f, 1.1f }) z.mk.Prism(white, bk.P(e, 1f, -.4f), up, .03f, 1.6f, 4); z.mk.Box(Paint(Lemon), bk.P(0, 2.6f, -.4f), new Vector3(2.5f, .4f, .05f), bk.r); Sign(bk.P(0, 2.6f, -.37f), bk.Fwd, "BRACELETS · TRADE ONE", .14f, Ink);
            for (var k = 0; k < 4; k++) { var bm = new MeshKit(); for (var q = 0; q < 10; q++) { var a = q * 36f * Mathf.Deg2Rad; bm.Blob(Paint(Candy[(q + k) % Candy.Length]), new Vector3(Mathf.Cos(a) * .05f, .02f, Mathf.Sin(a) * .05f), Vector3.one * .018f, rng, 0f); } Thing(bm, "Bracelet", "a friendship bracelet. Somebody spelled your name wrong, lovingly", bk.P(-.8f + k * .5f, 1.06f, .1f), Quaternion.identity, new Vector3(.14f, .05f, .14f), .05f, false); }
            Folk(bk.P(.3f, 0, -.8f), bk.Fwd, Resident.Pose.Stand, new[] { "Trade you one! Any one! They all mean something!", "I've made two thousand of these. My fingers are beads now.", "Take two. Give one away. That's the whole rule." }, Local(Residents.Tool.None, true));
            Shot(z.ctx, "the eras hall", z.At(16f, 6f) + up * 2.6f, st + up * 4f - fr * 2f);
        }

        void Plinth(Plot z, Vector3 at, float w, float h) { z.mk.Box(white, at + Vector3.up * (h / 2f), new Vector3(w, h, w), z.Rot); BoxCol(z.root, at + Vector3.up * (h / 2f), new Vector3(w, h, w), z.Rot); }

        /// A chair to sit on.
        void Chair(MeshKit mk, Vector3 g, Vector3 face)
        {
            var up = Vector3.up; var rot = Quaternion.LookRotation(face); mk.Box(white, g + up * .45f, new Vector3(.46f, .05f, .46f), rot); mk.Box(white, g + up * .8f - face * .21f, new Vector3(.46f, .7f, .05f), rot); foreach (var (x, zz) in new[] { (-.2f, -.2f), (.2f, -.2f), (.2f, .2f), (-.2f, .2f) }) mk.Box(white, g + up * .22f + rot * new Vector3(x, 0, zz), new Vector3(.04f, .44f, .04f), rot);
            Interact.Seat(tlife.transform, g + up * .5f, face);
        }

        // ---------------------------------------------------------------- Sabrina Carpenter
        void Sabrina(Plot z)
        {
            var up = Vector3.up; var rot = z.Rot; var rng = new System.Random(2024); var fr = z.front; var sd = z.side; var ju = Song(z, "Juno"); var mc = Song(z, "Manchild"); var ns = Song(z, "Nonsense"); life.junoSong = ju;
            // the heart-shaped stage, edged with bulbs, a pink velvet backdrop
            var st = z.At(-4f, 0) + up * .1f; Vector3 H(float t, float s) { var x = 16f * Mathf.Pow(Mathf.Sin(t), 3f); var y = 13f * Mathf.Cos(t) - 5f * Mathf.Cos(2f * t) - 2f * Mathf.Cos(3f * t) - Mathf.Cos(4f * t); return st + sd * x * s - fr * y * s; }
            const float sc = .3f; for (var k = 0; k < 40; k++) { var t0 = k * Mathf.PI * 2f / 40f; var t1 = (k + 1) * Mathf.PI * 2f / 40f; z.mk.Tri(Paint(Pink), st + up * .9f, H(t0, sc) + up * .9f, H(t1, sc) + up * .9f, st - up * 4f); z.mk.Box(white, (H(t0, sc) + H(t1, sc)) / 2f + up * .45f, new Vector3(.12f, .9f, Vector3.Distance(H(t0, sc), H(t1, sc)) + .05f), Quaternion.LookRotation((H(t1, sc) - H(t0, sc)).normalized)); if (k % 2 == 0) z.mk.Blob(bulbM[k % 3], H(t0, sc) + up * .96f, Vector3.one * .1f, rng, 0f); }
            z.solid.Box(white, st + up * .4f, new Vector3(8f, .8f, 7f), rot); var back = st - fr * 4.4f; z.mk.Box(Paint(new Color(.9f, .4f, .6f)), back + up * 3f, new Vector3(9f, 6f, .3f), rot); z.solid.Box(white, back + up * 3f, new Vector3(9f, 6f, .3f), rot); for (var k = 0; k < 9; k++) z.mk.Box(Paint(new Color(.84f, .34f, .54f)), back + up * 3f + fr * .17f + sd * (-4f + k), new Vector3(.4f, 6f, .03f), rot);
            for (var k = 0; k < 40; k++) { var t = k * Mathf.PI * 2f / 40f; z.mk.Blob(bulbM[k % 3], back + up * 3.2f + fr * .2f + sd * 16f * Mathf.Pow(Mathf.Sin(t), 3f) * .16f + up * (13f * Mathf.Cos(t) - 5f * Mathf.Cos(2f * t) - 2f * Mathf.Cos(3f * t) - Mathf.Cos(4f * t)) * .16f, Vector3.one * .09f, rng, 0f); }
            Mic(z.mk, st + up * .9f + fr * .6f, GlowM(Pink, 1.4f)); ArtistBits.Label(z.labels, "sabrina", back + up * 5.6f + fr * .2f, fr, .36f, White, TextAnchor.MiddleCenter);
            string[] sing = { "You do the little shoulder shimmy. The whole crowd does it back.", "A wink to the front row. Somebody there will talk about it for a year.", "You make up a new last line on the spot. It rhymes. It's filthy. Everyone screams.", "Bulbs flash. A heart of light behind you. You're adorable and you know it." }; var sn = 0;
            Spot("The heart stage", st + up * 1.8f + fr * .6f, "Take the stage", new Vector3(4f, 1.6f, 3f)).onUse = _ => { life.played = Time.time; Say1(st + up * 3.4f, sing[sn++ % sing.Length]); };
            z.area.glowLight = ArtistBits.PointLight(z.root, st + up * 5f + fr * 4f, new Color(1f, .82f, .9f), 26f, 1.4f); halos.Add(back + up * 3.2f + fr * .6f, 4.4f, Pink, .14f);
            // "Juno": a pink telephone on a pedestal
            var ph = z.At(6f, -8f) + up * .08f; Plinth(z, ph, .7f, 1f); var pm = SongGlow(ju, Pink, 1f); z.mk.Box(pm, ph + up * 1.12f, new Vector3(.4f, .2f, .3f), rot); z.mk.Box(pm, ph + up * 1.3f, new Vector3(.5f, .08f, .14f), rot); foreach (var e in new[] { -1f, 1f }) z.mk.Box(pm, ph + up * 1.26f + sd * e * .22f, new Vector3(.12f, .08f, .16f), rot); z.mk.Prism(white, ph + up * 1.22f + fr * .151f, fr, .1f, .01f, 12);
            string[] ring = { "Ring ring. A voice says something sweet and slightly unhinged, then hangs up.", "You pick it up. It's for you. It's always for you.", "Hello? Just pink static, and a giggle." }; var rn = 0; Spot("The pink telephone", ph + up * 1.3f, "Pick up the phone", new Vector3(.8f, .6f, .8f)).onUse = _ => Say1(ph + up * 2.2f, ring[rn++ % ring.Length]);
            // "Manchild": a playpen full of blocks and a rattle
            var pp = z.At(6f, 7.4f) + up * .08f; for (var k = 0; k < 4; k++) { var d = Quaternion.Euler(0, k * 90f, 0) * fr; var ed = Vector3.Cross(up, d); z.mk.Box(white, pp + d * 1.2f + up * .7f, new Vector3(.06f, .06f, 2.4f), Quaternion.LookRotation(ed)); for (var q = 0; q < 8; q++) z.mk.Prism(white, pp + d * 1.2f + ed * (-1.05f + q * .3f), up, .02f, .7f, 4); } BoxCol(z.root, pp + up * .4f, new Vector3(2.5f, .8f, 2.5f), rot);
            var blocks = new[] { "M", "A", "N" }; for (var k = 0; k < 3; k++) { var bp = pp + sd * (-.5f + k * .5f) + up * .2f; z.mk.Box(k == 1 ? SongGlow(mc, Candy[k], .6f) : Paint(Candy[k]), bp, Vector3.one * .4f, Quaternion.Euler(0, k * 13f, 0)); ArtistBits.Label(z.labels, blocks[k], bp + Quaternion.Euler(0, k * 13f, 0) * Vector3.forward * .21f, Quaternion.Euler(0, k * 13f, 0) * Vector3.forward, .2f, Ink, TextAnchor.MiddleCenter); } z.mk.Prism(Paint(Lemon), pp + fr * .6f + up * .1f, sd, .04f, .5f, 5); z.mk.Blob(Paint(Pink), pp + fr * .6f + sd * .6f + up * .1f, Vector3.one * .14f, rng, 0f);
            // "Nonsense": a heap of giant letters
            var hp = z.At(-1f, 10.6f) + up * .08f; var lets = "NONSENSE"; for (var k = 0; k < lets.Length; k++) { var lp = hp + Quaternion.Euler(0, k * 45f, 0) * Vector3.forward * (.4f + (k % 3) * .5f) + up * (.4f + (k % 2) * .5f); var lr = Quaternion.Euler(k * 23f, k * 61f, k * 17f); z.mk.Box(k == 0 ? SongGlow(ns, Candy[k % Candy.Length], .5f) : Paint(Candy[k % Candy.Length]), lp, new Vector3(.8f, .8f, .2f), lr); ArtistBits.Label(z.labels, lets[k].ToString(), lp + lr * Vector3.forward * .11f, lr * Vector3.forward, .5f, Ink, TextAnchor.MiddleCenter); } BoxCol(z.root, hp + up * .6f, new Vector3(2.6f, 1.2f, 2.6f), rot);
            for (var k = 0; k < 5; k++) Folk(st + fr * (5.6f + (k % 2) * 1.2f) + sd * (-3f + k * 1.5f) - up * .1f, -fr, k % 2 == 0 ? Resident.Pose.Wave : Resident.Pose.Stand, new[] { "She's SO funny. And she can actually sing. It's unfair.", "I've been doing the shoulder thing all day. I can't stop.", "Front row! Front row! I camped. Worth it." });
            Shot(z.ctx, "the heart stage", z.At(13f, 4f) + up * 2.4f, st + up * 2f - fr * 2f);
        }

        // ---------------------------------------------------------------- Katy Perry
        void Katy(Plot z)
        {
            var up = Vector3.up; var rot = z.Rot; var rng = new System.Random(2010); var fr = z.front; var sd = z.side; var fw = Song(z, "Firework"); var ro = Song(z, "Roar"); var dh = Song(z, "Dark Horse"); var lf = Song(z, "Last Friday"); var ik = Song(z, "I Kissed"); life.fireworkSong = fw; life.roarSong = ro;
            var f = PopUp(z);
            // "Firework": three launch tubes in a rack
            var ft = z.At(-1f, -7f) + up * .08f; z.mk.Box(Paint(new Color(.3f, .3f, .4f)), ft + up * .3f, new Vector3(1.8f, .6f, .8f), rot); BoxCol(z.root, ft + up * .5f, new Vector3(1.8f, 1f, .8f), rot); for (var k = 0; k < 3; k++) { var tp = ft + sd * (-.6f + k * .6f) + up * .6f; z.mk.Prism(Paint(Candy[k]), tp, (up + fr * .1f).normalized, .18f, 1.2f, 10); z.mk.Prism(SongGlow(fw, Candy[k], 1.6f), tp + up * 1.2f, up, .14f, .03f, 10); }
            // "Roar": a gold tiger
            var tg = z.At(4f, 7f) + up * .08f; Plinth(z, tg, 2.4f, .6f); var gm = SongGlow(ro, Gold, .7f); var tb = tg + up * 1.3f; z.mk.Blob(gm, tb, new Vector3(.5f, .5f, 1.1f), rng, 0f); z.mk.Blob(gm, tb + fr * 1f + up * .3f, new Vector3(.4f, .4f, .4f), rng, 0f); foreach (var e in new[] { -1f, 1f }) { z.mk.Cone(gm, tb + fr * 1f + up * .66f + sd * e * .22f, .1f, .16f, 4); foreach (var q in new[] { -.6f, .6f }) z.mk.Prism(gm, tb + fr * q + sd * e * .3f, -up, .12f, .7f, 5); } z.mk.Prism(gm, tb - fr * 1f, (-fr + up * .8f).normalized, .07f, 1f, 5); for (var k = 0; k < 5; k++) z.mk.Box(Paint(Ink), tb + up * .45f + fr * (-.6f + k * .3f), new Vector3(.9f, .06f, .1f), rot);
            // "Dark Horse": a black horse on a gold plinth
            var hs = z.At(6f, -9f) + up * .08f; z.mk.Box(goldM, hs + up * .4f, new Vector3(1.4f, .8f, 2.4f), rot); BoxCol(z.root, hs + up * 1.2f, new Vector3(1.4f, 2.4f, 2.4f), rot); var hm = SongGlow(dh, new Color(.24f, .1f, .36f), .5f); var hb = hs + up * 1.6f; z.mk.Blob(hm, hb, new Vector3(.4f, .45f, .9f), rng, 0f); z.mk.Prism(hm, hb + fr * .7f, (fr + up * 1.2f).normalized, .2f, .9f, 6, .7f); z.mk.Box(hm, hb + fr * 1.1f + up * .8f, new Vector3(.24f, .26f, .6f), rot * Quaternion.Euler(30f, 0, 0)); foreach (var e in new[] { -1f, 1f }) foreach (var q in new[] { -.6f, .6f }) z.mk.Prism(hm, hb + fr * q + sd * e * .2f, -up, .07f, .8f, 4); z.mk.Prism(hm, hb - fr * .9f, (-fr - up * .6f).normalized, .06f, .6f, 4);
            // "Last Friday Night": the aftermath; a disco ball, cups everywhere, a pink flamingo
            var pa = z.At(-3f, 9f) + up * .08f; z.mk.Prism(white, pa - up * .2f, up, .04f, 3f, 4); z.mk.Blob(SongGlow(lf, new Color(.85f, .9f, 1f), 1.4f), pa + up * 2.9f, Vector3.one * .4f, rng, .05f); ArtistBits.PostCollider(z.root, pa, 2.8f, .06f); for (var k = 0; k < 10; k++) z.mk.Prism(RedC.r > 0 ? Paint(RedC) : white, pa + Quaternion.Euler(0, k * 37f, 0) * Vector3.forward * (.6f + (k % 4) * .4f) + up * .01f, k % 3 == 0 ? (Quaternion.Euler(0, k * 30f, 0) * Vector3.forward) : up, .07f, .14f, 6, 1.3f);
            var fl = pa + sd * 1.6f; z.mk.Blob(Paint(Pink), fl + up * .7f, new Vector3(.2f, .2f, .32f), rng, 0f); z.mk.Prism(Paint(Pink), fl + up * .78f + fr * .2f, (up + fr * .2f).normalized, .04f, .5f, 4); z.mk.Blob(Paint(Pink), fl + up * 1.3f + fr * .28f, Vector3.one * .08f, rng, 0f); z.mk.Prism(Paint(new Color(.2f, .2f, .2f)), fl, up, .015f, .6f, 3);
            // "I Kissed A Girl": two giant cherries
            var ch = z.At(1f, -13f) + up * .08f; var cm = SongGlow(ik, RedC, .6f); foreach (var e in new[] { -1f, 1f }) { z.mk.Blob(cm, ch + sd * e * .5f + up * .5f, Vector3.one * .5f, rng, 0f); z.mk.Prism(leafM, ch + sd * e * .5f + up * .96f, (up * 1.6f - sd * e * .5f).normalized, .03f, 1.7f, 4); } BoxCol(z.root, ch + up * .5f, new Vector3(2f, 1f, 1f), rot);
            Shot(z.ctx, "katy perry's garden", z.At(11f, 3f) + up * 2.2f, z.At(-1f, 0) + up * 1.6f);
        }

        // ---------------------------------------------------------------- Michael Jackson
        void Michael(Plot z)
        {
            var up = Vector3.up; var rot = z.Rot; var rng = new System.Random(1982); var fr = z.front; var sd = z.side; var bj = Song(z, "Billie Jean"); var th = Song(z, "Thriller"); var bi = Song(z, "Beat It"); life.thrillerSong = th;
            // "Billie Jean": a floor of tiles that light up
            var fl = z.At(-2f, 0) + up * .1f; z.mk.Box(dark, fl + up * .06f, new Vector3(7.4f, .12f, 5.8f), rot); var tc = new[] { Pink, Lemon, Cyan, White, Lilac };
            for (var x = 0; x < 6; x++) for (var y = 0; y < 5; y++) { var m = (x + y) % 3 == 0 ? SongGlow(bj, tc[(x * 2 + y) % 5], 1.2f) : GlowM(tc[(x * 2 + y) % 5], .5f); z.mk.Box(m, fl + up * .13f + sd * (-3f + x * 1.2f) + fr * (-2.4f + y * 1.2f), new Vector3(1.1f, .02f, 1.1f), rot); }
            string[] moon = { "You moonwalk across the tiles. Each one lights up behind you. Somebody gasps.", "Spin, toes, hat. You don't have a hat. You do it anyway.", "Hee-hee! It just comes out." }; var mn = 0; Spot("The light-up floor", fl + up * 1f, "Moonwalk", new Vector3(7f, 1f, 5.6f)).onUse = _ => { life.played = Time.time; Say1(fl + up * 2.6f, moon[mn++ % moon.Length]); };
            z.area.glowLight = ArtistBits.PointLight(z.root, fl + up * 4f, new Color(1f, .96f, .96f), 18f, 1.2f);
            // "Thriller": a graveyard gate, three headstones, a green lamp
            var gy = z.At(-8f, -6f); var gr = rot * Quaternion.Euler(0, 30f, 0); foreach (var e in new[] { -1.2f, 1.2f }) { z.mk.Box(Paint(new Color(.5f, .5f, .52f)), gy + gr * Vector3.right * e + up * 1.2f, new Vector3(.4f, 2.4f, .4f), gr); BoxCol(z.root, gy + gr * Vector3.right * e + up * 1.2f, new Vector3(.4f, 2.4f, .4f), gr); } for (var k = 0; k < 9; k++) z.mk.Prism(dark, gy + gr * Vector3.right * (-.9f + k * .22f), up, .02f, 2f + Mathf.Sin(k * .7f) * .3f, 4); z.mk.Box(dark, gy + up * 1.9f, new Vector3(2.4f, .06f, .06f), gr);
            for (var k = 0; k < 3; k++) { var hp = gy - gr * Vector3.forward * (1.6f + k * .4f) + gr * Vector3.right * (-1.4f + k * 1.4f); z.mk.Box(Paint(new Color(.6f, .6f, .62f)), hp + up * .45f, new Vector3(.6f, .9f, .15f), gr * Quaternion.Euler(0, 0, (k - 1) * 6f)); BoxCol(z.root, hp + up * .45f, new Vector3(.6f, .9f, .2f), gr); }
            var gl = gy + gr * Vector3.right * 1.9f; z.mk.Prism(dark, gl - up * .2f, up, .04f, 2.6f, 4); z.mk.Blob(SongGlow(th, new Color(.4f, 1f, .4f), 1.4f), gl + up * 2.5f, Vector3.one * .16f, rng, 0f); ArtistBits.PostCollider(z.root, gl, 2.4f, .06f); halos.Add(gl + up * 2.5f, 1.4f, new Color(.4f, 1f, .4f), .4f);
            // "Beat It": a red jacket on a stand
            var jk = z.At(5f, 7f) + up * .08f; z.mk.Prism(white, jk, up, .04f, 1.4f, 4); z.mk.Prism(white, jk, up, .3f, .04f, 8); var jm = SongGlow(bi, RedC, .5f); z.mk.Box(jm, jk + up * 1.4f, new Vector3(.6f, .7f, .3f), rot); foreach (var e in new[] { -1f, 1f }) z.mk.Box(jm, jk + up * 1.42f + sd * e * .4f, new Vector3(.18f, .64f, .2f), rot * Quaternion.Euler(0, 0, e * -8f)); for (var k = 0; k < 4; k++) z.mk.Box(goldM, jk + up * (1.2f + k * .14f) + fr * .16f + sd * .1f, new Vector3(.02f, .02f, .02f), rot); ArtistBits.PostCollider(z.root, jk, 1.8f, .3f);
            // one sparkling glove on a plinth
            var gv = z.At(6f, -6f) + up * .08f; Plinth(z, gv, .6f, 1f); z.mk.Box(Paint(White), gv + up * 1.2f, new Vector3(.2f, .34f, .07f), rot); for (var k = 0; k < 4; k++) z.mk.Box(Paint(White), gv + up * (1.42f + (k % 2) * .03f) + sd * (-.075f + k * .05f), new Vector3(.04f, .16f, .05f), rot); z.mk.Box(Paint(White), gv + up * 1.26f + sd * .12f, new Vector3(.06f, .14f, .05f), rot * Quaternion.Euler(0, 0, -30f)); halos.Add(gv + up * 1.3f, .8f, White, .5f);
            Shot(z.ctx, "the light-up floor", z.At(10f, 3f) + up * 2.2f, fl + up * .6f);
        }

        // ---------------------------------------------------------------- everyone else: one thing from their song
        void Others(Plot z, PlaceFrame f)
        {
            var up = Vector3.up; var rot = z.Rot; var rng = new System.Random(z.a.name.Length * 7 + z.a.name[0]); var c = z.At(2.2f, 0) + up * .08f; var fr = z.front; var sd = z.side;
            void Post(Vector3 at, float h, float r = .04f) { z.mk.Prism(white, at - up * .2f, up, r, h + .2f, 5); ArtistBits.PostCollider(z.root, at, h, r + .03f); }
            switch (z.a.name)
            {
                case "BTS":
                {
                    var dy = Song(z, "Dynamite"); var sp = Song(z, "Spring Day"); life.dynamiteSong = dy; Post(c - sd * 1f, 2.6f); z.mk.Blob(SongGlow(dy, new Color(1f, .7f, .3f), 1.2f), c - sd * 1f + up * 2.9f, Vector3.one * .36f, rng, .05f); halos.Add(c - sd * 1f + up * 2.9f, 1.6f, new Color(1f, .7f, .3f), .3f);
                    var tr = c + sd * 1.6f; z.mk.Prism(Paint(new Color(.4f, .3f, .3f)), tr - up * .2f, up, .14f, 2.4f, 6, .6f); ArtistBits.PostCollider(z.root, tr, 2.2f, .18f); for (var k = 0; k < 5; k++) z.mk.Blob(k == 0 ? SongGlow(sp, new Color(1f, .78f, .86f), .7f) : Paint(new Color(1f, .82f, .9f)), tr + up * (2.8f + (k % 2) * .4f) + Quaternion.Euler(0, k * 72f, 0) * Vector3.forward * (k == 0 ? 0f : .9f), new Vector3(1.1f, .8f, 1.1f), rng, .15f); break;
                }
                case "TWICE":
                {
                    var fs = Song(z, "Feel Special"); var wl = Song(z, "What is Love"); var hb = c + up * 3.2f; Vector3 HP(float t) { return hb + sd * 16f * Mathf.Pow(Mathf.Sin(t), 3f) * .05f + up * (13f * Mathf.Cos(t) - 5f * Mathf.Cos(2f * t) - 2f * Mathf.Cos(3f * t) - Mathf.Cos(4f * t)) * .05f; } var hm = SongGlow(fs, Pink, 1.2f); for (var k = 0; k < 24; k++) { var t0 = k * Mathf.PI * 2f / 24f; var t1 = (k + 1) * Mathf.PI * 2f / 24f; z.mk.Tri(hm, hb, HP(t0), HP(t1), hb - fr); z.mk.Tri(hm, hb, HP(t0), HP(t1), hb + fr); } z.mk.Prism(white, c, up, .006f, 2.5f, 3);
                    var lt = c + sd * 1.6f + fr * .4f; Post(lt, .9f, .03f); z.mk.Box(SongGlow(wl, White, .7f), lt + up * 1f, new Vector3(.5f, .34f, .03f), rot); z.mk.Tri(Paint(Pink), lt + up * 1.17f + fr * .02f + sd * -.25f, lt + up * 1.17f + fr * .02f + sd * .25f, lt + up * 1f + fr * .02f, lt + fr); break;
                }
                case "BLACKPINK":
                {
                    var s = Song(z, "DDU-DU"); Post(c, .4f, .3f); var hm = SongGlow(s, Pink, 1f); z.mk.Box(dark, c + up * 1.3f, new Vector3(1.2f, .7f, .7f), rot); z.mk.Box(hm, c + up * 1.3f + sd * .62f, new Vector3(.04f, .6f, .6f), rot); z.mk.Box(hm, c + up * 1.3f - sd * .62f, new Vector3(.04f, .6f, .6f), rot); z.mk.Prism(dark, c + up * .4f, up, .07f, .6f, 6); halos.Add(c + up * 1.3f, 1.2f, Pink, .3f); break;
                }
                case "IVE":
                {
                    var s = Song(z, "LOVE DIVE"); var pl = c + fr * .4f; z.mk.Prism(white, pl, up, 1.4f, .3f, 16); z.mk.Prism(SongGlow(s, new Color(1f, .5f, .7f), .8f), pl + up * .28f, up, 1.25f, .02f, 16); BoxCol(z.root, pl + up * .15f, new Vector3(2.8f, .3f, 2.8f), rot);
                    var db = pl - fr * 1.8f; Post(db - sd * .4f, 1.4f, .05f); Post(db + sd * .4f, 1.4f, .05f); z.mk.Box(white, db + up * 1.5f + fr * .7f, new Vector3(.7f, .06f, 2f), rot); break;
                }
                case "NewJeans":
                {
                    var s = Song(z, "Super Shy"); var bm = SongGlow(s, new Color(.6f, .8f, 1f), .8f); var b = c + up * .3f; Post(c, .1f, .4f); z.mk.Blob(bm, b + up * .5f, new Vector3(.5f, .5f, .45f), rng, 0f); z.mk.Blob(bm, b + up * 1.2f, Vector3.one * .38f, rng, 0f); foreach (var e in new[] { -1f, 1f }) { z.mk.Prism(bm, b + up * 1.45f + sd * e * .15f, (up + sd * e * .1f).normalized, .08f, .7f, 6, .6f); z.mk.Blob(dark, b + up * 1.24f + fr * .34f + sd * e * .12f, Vector3.one * .04f, rng, 0f); z.mk.Blob(Paint(Pink), b + up * 1.12f + fr * .32f + sd * e * .2f, new Vector3(.06f, .03f, .02f), rng, 0f); } break;
                }
                case "aespa":
                {
                    var s = Song(z, "Supernova"); life.novaSong = s; Post(c, 2.4f, .04f); var nm = SongGlow(s, new Color(.8f, .9f, 1f), 1.8f); z.mk.Blob(nm, c + up * 2.8f, Vector3.one * .3f, rng, 0f); for (var k = 0; k < 12; k++) { var d = Quaternion.AngleAxis(k * 30f, fr) * up; z.mk.Prism(nm, c + up * 2.8f, d, .03f, k % 2 == 0 ? 1.2f : .7f, 3, .1f); } halos.Add(c + up * 2.8f, 2.4f, new Color(.8f, .9f, 1f), .4f); break;
                }
                case "Adele":
                {
                    var s = Song(z, "Skyfall"); life.skyfallSong = s; var sm = SongGlow(s, new Color(.5f, .7f, 1f), .5f); for (var k = 0; k < 5; k++) { var p = c + Quaternion.Euler(0, k * 72f, 0) * Vector3.forward * (.4f + (k % 3) * .5f) + up * .2f; var r = Quaternion.Euler(k * 30f, k * 50f, k * 20f); z.mk.Box(sm, p, new Vector3(.9f - k * .1f, .2f, .7f), r); z.mk.Box(Paint(White), p + r * Vector3.up * .11f, new Vector3(.3f, .02f, .2f), r); } BoxCol(z.root, c + up * .3f, new Vector3(2.4f, .6f, 2.4f), rot); break;
                }
                case "Alex Warren":
                {
                    var s = Song(z, "FEVER DREAM"); for (var k = 0; k < 3; k++) { var p = c + sd * (-1f + k) + fr * ((k % 2) * .6f); Post(p, 1.6f + k * .5f, .03f); z.mk.Blob(k == 1 ? SongGlow(s, new Color(1f, .7f, .86f), .9f) : Paint(Candy[k + 1]), p + up * (2f + k * .5f), new Vector3(.7f, .4f, .5f), rng, .3f); } break;
                }
                case "B.J. Thomas":
                {
                    var s = Song(z, "Most Of All"); Bench(z.root, z.mk, c, fr, 1.6f); var hp = c - fr * .4f + up * 1.6f; Post(c - fr * .4f, 1.3f, .03f); for (var k = 0; k < 2; k++) z.mk.Blob(SongGlow(s, Pink, 1f), hp + sd * (k * .2f - .1f), Vector3.one * .16f, rng, 0f); z.mk.Cone(SongGlow(s, Pink, 1f), hp - up * .02f, .25f, -.3f, 6); break;
                }
                case "Bebe Rexha":
                {
                    var s = Song(z, "One Day"); Post(c - sd * .7f, 1.4f, .04f); Post(c + sd * .7f, 1.4f, .04f); z.mk.Box(white, c + up * 1.6f, new Vector3(1.8f, 1.2f, .05f), rot); for (var k = 0; k < 28; k++) z.mk.Box(k == 17 ? SongGlow(s, Pink, 1.4f) : Paint(new Color(.9f, .9f, .92f)), c + up * (2f - (k / 7) * .26f) + sd * (-.72f + (k % 7) * .24f) + fr * .03f, new Vector3(.2f, .2f, .01f), rot); break;
                }
                case "Christina Perri":
                {
                    var s = Song(z, "a thousand years"); var hg = c + up * .1f; z.mk.Prism(goldM, hg, up, .6f, .1f, 10); z.mk.Prism(goldM, hg + up * 2f, up, .6f, .1f, 10); var gl = SongGlow(s, new Color(.9f, .95f, 1f), .7f); z.mk.Prism(gl, hg + up * .1f, up, .55f, .95f, 10, .1f); z.mk.Prism(gl, hg + up * 2f, -up, .55f, .95f, 10, .1f); z.mk.Cone(Paint(Lemon), hg + up * .1f, .4f, .4f, 10); for (var k = 0; k < 3; k++) z.mk.Prism(goldM, hg + Quaternion.Euler(0, k * 120f, 0) * Vector3.forward * .55f, up, .03f, 2f, 4); BoxCol(z.root, hg + up * 1f, new Vector3(1.2f, 2.1f, 1.2f), rot); break;
                }
                case "Dido":
                {
                    var s = Song(z, "White Flag"); Post(c, 5f, .05f); z.mk.Box(SongGlow(s, White, .6f), c + up * 4.4f + sd * .9f, new Vector3(1.8f, 1.1f, .02f), rot); break;
                }
                case "Ellie Goulding":
                {
                    var s = Song(z, "Lights"); for (var k = 0; k < 7; k++) { var p = c + Quaternion.Euler(0, k * 51f, 0) * Vector3.forward * (.6f + (k % 3) * .5f); var h = 1f + (k % 4) * .5f; Post(p, h, .02f); z.mk.Blob(k == 0 ? SongGlow(s, Cyan, 1.6f) : bulbM[k % 3], p + up * (h + .1f), Vector3.one * .16f, rng, 0f); } halos.Add(c + up * 1.6f, 2f, Cyan, .2f); break;
                }
                case "GAYLE":
                {
                    var s = Song(z, "abcdefu"); var abc = "abcdef"; for (var k = 0; k < 6; k++) { var bp = c + sd * (-1.25f + k * .5f) + up * (.25f + (k == 5 ? .5f : 0f)) + (k == 5 ? -sd * 1.5f : Vector3.zero); z.mk.Box(k == 5 ? SongGlow(s, RedC, .5f) : Paint(Candy[k]), bp, Vector3.one * .48f, rot * Quaternion.Euler(0, k * 7f, 0)); ArtistBits.Label(z.labels, abc[k].ToString(), bp + fr * .25f, fr, .26f, Ink, TextAnchor.MiddleCenter); } BoxCol(z.root, c + up * .4f, new Vector3(3f, .8f, .5f), rot); break;
                }
                case "Imagine Dragons":
                {
                    var ra = Song(z, "Radioactive"); var de = Song(z, "Demons"); var bp = c - sd * 1f; z.mk.Prism(Paint(new Color(.9f, .8f, .2f)), bp, up, .4f, 1f, 12); z.mk.Prism(SongGlow(ra, new Color(.5f, 1f, .3f), 1.4f), bp + up * 1f, up, .36f, .04f, 12); for (var k = 0; k < 3; k++) z.mk.Tri(dark, bp + up * .5f + Quaternion.Euler(0, k * 120f, 0) * Vector3.forward * .41f, bp + up * .7f + Quaternion.Euler(0, k * 120f + 20f, 0) * Vector3.forward * .41f, bp + up * .7f + Quaternion.Euler(0, k * 120f - 20f, 0) * Vector3.forward * .41f, bp); ArtistBits.PostCollider(z.root, bp, 1f, .42f); halos.Add(bp + up * 1.2f, 1f, new Color(.5f, 1f, .3f), .4f);
                    var dk = c + sd * 1.2f; Post(dk, 1.6f, .03f); var dm = SongGlow(de, new Color(.5f, .3f, .9f), 1f); z.mk.Tri(dm, dk + up * 2f, dk + up * 1.6f + sd * .9f, dk + up * 1.3f, dk - fr); z.mk.Tri(dm, dk + up * 2f, dk + up * 1.6f - sd * .9f, dk + up * 1.3f, dk - fr); z.mk.Prism(dm, dk + up * 1.4f, -up, .02f, 1f, 3); break;
                }
                case "Isabel LaRosa":
                {
                    var s = Song(z, "Hallucination"); for (var k = 0; k < 3; k++) { var mp = c + Quaternion.Euler(0, k * 120f, 0) * Vector3.forward * 1f; var mr = Quaternion.LookRotation(-(Quaternion.Euler(0, k * 120f, 0) * Vector3.forward)); z.mk.Box(white, mp + up * 1.1f, new Vector3(1f, 2.1f, .08f), mr); z.mk.Box(k == 0 ? SongGlow(s, new Color(.7f, .6f, 1f), .7f) : glassM, mp + up * 1.1f + mr * Vector3.forward * .05f, new Vector3(.86f, 1.96f, .02f), mr); BoxCol(z.root, mp + up * 1.1f, new Vector3(1f, 2.1f, .1f), mr); } break;
                }
                case "Kristine Blond":
                {
                    var s = Song(z, "Love Shy"); Post(c - sd * 1f, 2f, .03f); Post(c + sd * 1f, 2f, .03f); z.mk.Prism(white, c - sd * 1f + up * 2f, sd, .02f, 2f, 4); for (var k = 0; k < 6; k++) z.mk.Box(Paint(new Color(.9f, .5f, .6f)), c + up * 1.1f + sd * (-.85f + k * .34f) + fr * ((k % 2) * .05f), new Vector3(.34f, 1.8f, .03f), rot); z.mk.Blob(SongGlow(s, Pink, 1.2f), c + up * 1.1f - fr * .4f + sd * .3f, Vector3.one * .26f, rng, 0f); break;
                }
                case "Lady Gaga":
                {
                    var s = Song(z, "Just Dance"); var fl = c + fr * .3f; z.mk.Box(dark, fl + up * .06f, new Vector3(2.6f, .12f, 2.6f), rot); for (var x = 0; x < 3; x++) for (var y = 0; y < 3; y++) z.mk.Box((x + y) % 2 == 0 ? SongGlow(s, Candy[(x + y * 3) % Candy.Length], 1.1f) : GlowM(Lilac, .4f), fl + up * .13f + sd * (-.85f + x * .85f) + fr * (-.85f + y * .85f), new Vector3(.8f, .02f, .8f), rot); Post(fl - fr * 1.4f, 2.6f, .03f); z.mk.Blob(Paint(new Color(.85f, .9f, 1f)), fl - fr * 1.4f + up * 2.9f, Vector3.one * .3f, rng, .05f); break;
                }
                case "Miley Cyrus":
                {
                    var s = Song(z, "7 Things"); Post(c - sd * .7f, 1.4f, .04f); Post(c + sd * .7f, 1.4f, .04f); z.mk.Box(Paint(new Color(1f, .97f, .9f)), c + up * 1.7f, new Vector3(1.6f, 1.6f, .05f), rot); for (var k = 0; k < 7; k++) { z.mk.Box(k == 6 ? SongGlow(s, Pink, 1f) : Paint(new Color(.4f, .5f, .9f)), c + up * (2.3f - k * .2f) + sd * .1f + fr * .03f, new Vector3(1.1f, .04f, .01f), rot); ArtistBits.Label(z.labels, (k + 1) + ".", c + up * (2.3f - k * .2f) - sd * .6f + fr * .04f, fr, .08f, Ink, TextAnchor.MiddleCenter); } break;
                }
                case "Pentatonix":
                {
                    var s = Song(z, "Hallelujah"); for (var k = 0; k < 5; k++) Mic(z.mk, c + sd * (-1.6f + k * .8f) + fr * (Mathf.Abs(k - 2) * -.25f), k == 2 ? SongGlow(s, White, 1.6f) : goldM); halos.Add(c + up * 1.6f, 2f, White, .2f); break;
                }
                case "Petite Meller":
                {
                    var s = Song(z, "In Love With Myself"); var mp = c; z.mk.Box(white, mp + up * .5f, new Vector3(1.4f, 1f, .6f), rot); BoxCol(z.root, mp + up * .5f, new Vector3(1.4f, 1f, .6f), rot); z.mk.Box(Paint(Pink), mp + up * 1.8f - fr * .2f, new Vector3(1.4f, 1.6f, .1f), rot); z.mk.Box(SongGlow(s, new Color(1f, .9f, .94f), .6f), mp + up * 1.8f - fr * .14f, new Vector3(1.1f, 1.3f, .02f), rot); for (var k = 0; k < 10; k++) { var a = k / 10f * Mathf.PI * 2f; z.mk.Blob(lampM, mp + up * (1.8f + Mathf.Sin(a) * .74f) + sd * Mathf.Cos(a) * .62f - fr * .1f, Vector3.one * .06f, rng, 0f); } break;
                }
                case "Reneé Rapp":
                {
                    var sa = Song(z, "Snow Angel"); var ib = Song(z, "I Hate Boston"); life.snowSong = sa; var sn = c - sd * .8f; z.mk.Prism(Paint(White), sn + up * .01f, up, 1.2f, .08f, 14); var am = SongGlow(sa, new Color(.8f, .9f, 1f), .7f); z.mk.Blob(am, sn + up * .1f - fr * .5f, new Vector3(.2f, .02f, .2f), rng, 0f); z.mk.Box(am, sn + up * .1f, new Vector3(.3f, .02f, .6f), rot); foreach (var e in new[] { -1f, 1f }) { z.mk.Box(am, sn + up * .1f - fr * .2f + sd * e * .4f, new Vector3(.5f, .02f, .14f), rot * Quaternion.Euler(0, e * 30f, 0)); z.mk.Tri(am, sn + up * .1f + fr * .2f, sn + up * .1f + fr * .7f + sd * e * .4f, sn + up * .1f + fr * .7f, sn - up); }
                    var sc = c + sd * 1.2f; z.mk.Box(Paint(new Color(.6f, .3f, .3f)), sc + up * .35f, new Vector3(.8f, .6f, .3f), rot); z.mk.Box(SongGlow(ib, Lemon, .6f), sc + up * .5f + fr * .16f, new Vector3(.5f, .14f, .01f), rot); ArtistBits.Label(z.labels, "BOS → anywhere", sc + up * .5f + fr * .17f, fr, .04f, Ink, TextAnchor.MiddleCenter); BoxCol(z.root, sc + up * .35f, new Vector3(.8f, .7f, .3f), rot); break;
                }
                case "Rick Astley":
                {
                    var s = Song(z, "Never Gonna"); var tv = c; z.mk.Box(Paint(new Color(.4f, .36f, .34f)), tv + up * .9f, new Vector3(1.4f, 1.2f, 1.1f), rot); z.mk.Box(SongGlow(s, new Color(.7f, .9f, 1f), 1.2f), tv + up * .94f + fr * .56f, new Vector3(1f, .8f, .02f), rot); z.mk.Box(white, tv + up * .15f, new Vector3(1.2f, .3f, 1f), rot); BoxCol(z.root, tv + up * .7f, new Vector3(1.4f, 1.6f, 1.1f), rot); var bt = tv + fr * 1.2f + sd * 1f; Post(bt, .8f, .05f); z.mk.Prism(redM(), bt + up * .86f, up, .14f, .1f, 10); ArtistBits.Label(z.labels, "FREE PRIZE", bt + up * 1.15f, fr, .08f, Ink, TextAnchor.MiddleCenter);
                    string[] rr = { "You press the button. The television flickers. A man in a trench coat starts dancing. You've been had.", "You press it again. Same man. Same dance. You knew. You pressed it anyway.", "The prize is the song. It was always the song." }; var rn = 0; Spot("The prize button", bt + up * 1f, "Press for a prize", new Vector3(.5f, .5f, .5f)).onUse = _ => { life.played = Time.time; Say1(tv + up * 2.2f, rr[rn++ % rr.Length]); }; break;
                }
                case "Savage Garden":
                {
                    var s = Song(z, "I Want You"); var bt = c; z.mk.Prism(SongGlow(s, new Color(.5f, .14f, .1f), .5f), bt, up, .4f, 1.4f, 12, .9f); z.mk.Prism(Paint(new Color(.5f, .14f, .1f)), bt + up * 1.4f, up, .36f, .5f, 12, .4f); z.mk.Prism(Paint(RedC), bt + up * 1.9f, up, .16f, .12f, 10); z.mk.Box(white, bt + up * .8f + fr * .41f, new Vector3(.6f, .3f, .01f), rot); ArtistBits.Label(z.labels, "cherry cola", bt + up * .8f + fr * .42f, fr, .07f, RedC, TextAnchor.MiddleCenter); ArtistBits.PostCollider(z.root, bt, 2f, .42f); break;
                }
                case "The Proclaimers":
                {
                    var s = Song(z, "I'm Gonna Be"); z.mk.Box(Paint(new Color(.8f, .8f, .78f)), c + up * .6f, new Vector3(.8f, 1.2f, .4f), rot); z.mk.Prism(Paint(new Color(.8f, .8f, .78f)), c + up * 1.2f, fr, .4f, .2f, 10); BoxCol(z.root, c + up * .6f, new Vector3(.8f, 1.4f, .4f), rot); z.mk.Box(SongGlow(s, Lemon, .5f), c + up * .9f + fr * .21f, new Vector3(.6f, .3f, .01f), rot); ArtistBits.Label(z.labels, "500\nMILES", c + up * .9f + fr * .22f, fr, .08f, Ink, TextAnchor.MiddleCenter); foreach (var e in new[] { -1f, 1f }) { z.mk.Box(Paint(new Color(.4f, .28f, .2f)), c + fr * .8f + sd * e * .14f + up * .1f, new Vector3(.14f, .2f, .3f), rot); z.mk.Box(Paint(new Color(.4f, .28f, .2f)), c + fr * .75f + sd * e * .14f + up * .26f, new Vector3(.12f, .2f, .14f), rot); } break;
                }
                case "The Wanted":
                {
                    var s = Song(z, "Glad You Came"); var ch = c; z.mk.Box(Paint(Cyan), ch + up * .3f, new Vector3(.7f, .06f, 1.6f), rot * Quaternion.Euler(-10f, 0, 0)); z.mk.Box(Paint(Cyan), ch + up * .7f - fr * .7f, new Vector3(.7f, .06f, .8f), rot * Quaternion.Euler(-60f, 0, 0)); BoxCol(z.root, ch + up * .3f, new Vector3(.7f, .6f, 1.6f), rot); Interact.Seat(tlife.transform, ch + up * .44f, fr, "Lie back").seatHeight = .3f; z.mk.Prism(SongGlow(s, new Color(1f, .6f, .3f), .9f), ch + sd * .7f + up * .02f, up, .04f, .2f, 6); Post(ch + sd * .7f, .01f, .02f); break;
                }
                default:
                {
                    if (SongIds.TryGetValue(z.a.name, out var list) && list.Length > 0) { var s = Song(z, list[0].title); Post(c, 1.6f); Star(z.mk, SongGlow(s, Lemon, 1.6f), c + up * 2f, fr, .5f); }
                    break;
                }
            }
        }

        Material redM() => Paint(RedC);
    }
}
