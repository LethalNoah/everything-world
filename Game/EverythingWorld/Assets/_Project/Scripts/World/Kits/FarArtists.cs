using System.Collections.Generic;
using UnityEngine;
using static EverythingWorld.World.PlaceTools;

namespace EverythingWorld.World
{
    /// The Far Shores' artists. Most have a port of call on the Promenade: a little pavilion with a striped canvas roof
    /// and their name on a board, a bench, and one thing from their song beside it; covers on a cream card in a white
    /// frame, a plaque, a globe lamp.
    /// - Jorge Ben Jor (the beach end): a round thatched beach bar with stools, drinks to carry, and a corner of samba
    ///   drums to play ("Mas, Que Nada!": petals rain); a treasure chest of children's toys ("Meus Filhos, Meu
    ///   Tesouro"); a deck chair, a pile of books and a football ("O Filósofo").
    /// - Mulatu Astatke (the edge of the dunes): an eight-sided thatched pavilion with a vibraphone to play, congas,
    ///   a coffee ceremony (a cup to carry), a wise man's chair, and strings of old photographs ("Tezeta": the day
    ///   turns the colour of memory).
    /// - Tinariwen (out in the dunes): the camp. A great dark tent over rugs and cushions, a fire, three guitars and a
    ///   small amp to play, sweet tea to carry. Their songs raise the wind and the sand.
    /// - Elis Regina's stick, stone and little stream; Marcos Valle's mobile of stars; Rodrigo y Gabriela's two
    ///   guitars and a crocodile; Herb Alpert's flea circus; Santo & Johnny's steel guitar by a hammock; Tony
    ///   Bennett's piano and two microphones; Harry Belafonte's banana boat and tally board; Keith Mansfield's
    ///   open-air cinema; Dave Richmond's travel bureau; Esperanza Spalding's bass and thirty silver coins; Robert
    ///   Stillman's ring of stones; Bossario's record player by a deck chair.
    public partial class FarKit
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
                Add("Mulatu Astatke", "Yègellé Tezeta", "Mulatu Astatke"); Add("Santo & Johnny", "Sleep Walk", "@down the promenade"); Add("Tinariwen", "Cler Achel", "Tinariwen");
                Add("Elis Regina", "Águas", "@the compass"); Add("Jorge Ben Jor", "Mas, Que Nada", "Jorge Ben Jor");
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

        /// Every song of this artist, marked as having its piece.
        List<string> AllSongs(Plot z) { var l = new List<string>(); if (SongIds.TryGetValue(z.a.name, out var list)) foreach (var (_, id) in list) { z.lit.Add(id); l.Add(id); } return l; }

        Material SongGlow(string song, Color c, float s = 1.6f)
        {
            if (song == null) return GlowM(c, s);
            var m = new Material(GlowM(c, s)) { name = "Song glow" }; life.songGlows.Add((song, m, c * (s * .65f))); return m;
        }

        void Say1(Vector3 at, string text) => PlaceTools.Say(tlife.transform, at, text, 4.5f);

        public override void BuildArtist(LandContext ctx, IslandWorldData.Artist a)
        {
            if (tileW == null) MakeMaterials();
            var root = new GameObject("Artist: " + a.name).transform; root.SetParent(ctx.Root, false);
            var front = facing.TryGetValue(a.name, out var f) ? f : new Vector3(center.x - a.x, 0, center.z - a.z).normalized;
            if (front.sqrMagnitude < .01f) front = Vector3.forward;
            var labels = new GameObject("Labels").transform; labels.SetParent(root, false);
            var area = root.gameObject.AddComponent<ArtistArea>();
            area.artistName = a.name; area.stationId = a.stationId; area.radius = a.r; area.glowColor = Sun;
            var z = new Plot { ctx = ctx, root = root, labels = labels, mk = new MeshKit(), solid = new MeshKit(), pictures = new MeshKit(), a = a, c = ctx.OnGround(a.x, a.z), front = front, side = Vector3.Cross(Vector3.up, front), area = area };
            var reach = Foot(a); var up = Vector3.up;
            z.mk.Prism(a.name == "Tinariwen" ? duneM : stone, z.c - up * .25f, up, reach * .66f, .33f, 22);
            switch (a.name)
            {
                case "Jorge Ben Jor": JorgeBenJor(z); break;
                case "Mulatu Astatke": MulatuAstatke(z); break;
                case "Tinariwen": Tinariwen(z); break;
                default: Port(z); Others(z); break;
            }
            SongLamps(z);
            Board(z, z.At(reach * .6f, -reach * .45f - 1.5f), front);
            ArtistBits.Plaque(z.mk, z.solid, labels, a, z.At(reach * .6f + 1.5f, reach * .45f + 1.5f), front, bleached, white, Ink, 1.6f);
            GlobeLamp(ctx, z.mk, z.At(reach * .5f, reach * .64f));
            if (z.area.glowLight == null) z.area.glowLight = ArtistBits.PointLight(root, z.c + up * 5f + front * 3f, new Color(1f, .9f, .7f), HasStation(a) ? 24f : 16f, 1.2f);
            area.glowRenderers = new Renderer[0];
            z.mk.Build("Area", root, false);
            z.solid.BuildCollider("Walls", root);
            z.pictures.Build("Covers", root, false);
            noGrass.Add(new Vector3(z.c.x, reach * .9f, z.c.z));
            if (HasStation(a) || reach >= 12f)
            {
                var mark = new GameObject("Shot: " + a.name + " up close").transform; mark.SetParent(ctx.Root, false);
                var cam = z.At(reach * .85f, reach * .3f) + up * 2f; var look = z.At(-reach * .3f, 0) + up * 2f - cam; mark.SetPositionAndRotation(cam, Quaternion.LookRotation(look.normalized));
            }
        }

        /// The covers on a cream card in a white frame with a little striped canopy.
        void Board(Plot z, Vector3 ground, Vector3 dir)
        {
            dir = Vector3.ProjectOnPlane(dir, Vector3.up).normalized; var rot = Quaternion.LookRotation(dir); var hue = z.a.name.Length;
            var n = Mathf.Max(1, Mathf.Min(7, z.a.songList?.Length ?? 1)); const float size = .9f; var w = n * size * 1.25f + .6f;
            var c = ground + Vector3.up * 1.7f;
            z.mk.Box(white, c - Vector3.up * .5f - dir * .06f, new Vector3(w + .5f, size + 2f, .2f), rot); z.solid.Box(white, c - Vector3.up * .5f, new Vector3(w + .5f, size + 2f, .3f), rot); z.mk.Box(Paint(Cream), c - Vector3.up * .1f + dir * .05f, new Vector3(w, size + .9f, .04f), rot);
            var stripes = Mathf.Max(3, Mathf.RoundToInt((w + .9f) / .4f)); for (var k = 0; k < stripes; k++) z.mk.Box(k % 2 == 0 ? Paint(Paints[hue % Paints.Length]) : white, c + Vector3.up * (size / 2f + .64f + (k % 2) * .01f) + dir * .3f + rot * Vector3.right * (-(w + .9f) / 2f + (k + .5f) * (w + .9f) / stripes), new Vector3((w + .9f) / stripes - .02f, .04f, .9f), rot * Quaternion.Euler(14f, 0, 0));
            ArtistBits.CoverWall(z.pictures, z.labels, z.ctx, z.a, c + dir * .08f + Vector3.up * .08f, dir, n, size, n, true, Ink);
        }

        /// A glass float on a post for each song without its own piece.
        void SongLamps(Plot z)
        {
            if (!SongIds.TryGetValue(z.a.name, out var list)) return;
            var up = Vector3.up; var k = 0; var seen = new HashSet<string>();
            foreach (var (_, id) in list)
            {
                if (z.lit.Contains(id) || !seen.Add(id)) continue;
                var p = z.At(Foot(z.a) * .55f - (k % 2) * 1.3f, Foot(z.a) * .2f + (k / 2) * 1.3f);
                z.mk.Prism(bleached, p - up * .1f, up, .05f, 1.2f, 5); z.mk.Blob(SongGlow(id, Teal, 1.6f), p + up * 1.36f, Vector3.one * .18f, new System.Random(k), 0f); ArtistBits.PostCollider(z.root, p, 1.2f, .08f);
                k++;
            }
        }

        /// A port of call: a little pavilion with a striped canvas roof, their name on a board, a counter, a bench.
        PlaceFrame Port(Plot z)
        {
            var up = Vector3.up; var hue = z.a.name.Length + z.a.name[0]; var f = new PlaceFrame(z.At(-Foot(z.a) * .4f, 0), z.front); var c1 = Paint(Paints[hue % Paints.Length]);
            foreach (var (x, zz) in new[] { (-2f, -1.2f), (2f, -1.2f), (2f, 1.2f), (-2f, 1.2f) }) { z.mk.Prism(white, f.P(x, -.3f, zz), up, .07f, 3.1f, 6); ArtistBits.PostCollider(z.root, f.P(x, 0, zz), 2.8f, .09f); }
            for (var s = 0; s < 8; s++) foreach (var e in new[] { -1f, 1f }) z.mk.Box(s % 2 == 0 ? c1 : white, f.P(-1.93f + s * .55f, 3.1f + (s % 2) * .01f, e * .75f), new Vector3(.53f, .04f, 1.7f), f.r * Quaternion.Euler(e * 18f, 0, 0));
            z.mk.Box(white, f.P(0, .5f, -.9f), new Vector3(3.6f, 1f, .5f), f.r); z.mk.Box(c1, f.P(0, 1.02f, -.9f), new Vector3(3.8f, .06f, .7f), f.r); BoxCol(z.root, f.P(0, .5f, -.9f), new Vector3(3.6f, 1f, .5f), f.r);
            z.mk.Box(white, f.P(0, 2.5f, 1.3f), new Vector3(3.8f, .5f, .06f), f.r); ArtistBits.Label(z.labels, z.a.name.ToUpperInvariant(), f.P(0, 2.5f, 1.34f), z.front, z.a.name.Length > 18 ? .15f : z.a.name.Length > 13 ? .2f : .26f, Ink, TextAnchor.MiddleCenter);
            Bench(z.mk, f.P(-3.6f, 0, 1.6f), (z.front + z.side * .4f).normalized);
            return new PlaceFrame(f.P(0, 1.06f, -.9f), z.front);
        }

        // ---------------------------------------------------------------- Jorge Ben Jor: the beach bar
        void JorgeBenJor(Plot z)
        {
            var up = Vector3.up; var rot = z.Rot; var rng = new System.Random(63); var mq = Song(z, "Mas, Que Nada"); var mf = Song(z, "Meus Filhos"); var fi = Song(z, "O Filósofo"); life.sambaSong = mq; var c = z.At(-3f, 0);
            // a round bar under a thatched cone: a counter all the way round, stools, a barman in the middle
            z.mk.Prism(bleached, c, up, 2.5f, 1.05f, 16); z.mk.Prism(Paint(Teal), c + up * 1.05f, up, 2.75f, .07f, 16); z.mk.Prism(Paint(Coral), c + up * .5f, up, 2.53f, .08f, 16); z.solid.Prism(bleached, c, up, 2.6f, 1.1f, 12);
            z.mk.Prism(woodM, c, up, .16f, 4.2f, 6); for (var k = 0; k < 6; k++) { var d = Quaternion.Euler(0, k * 60f, 0) * z.front; z.mk.Prism(woodM, c + d * 2.3f + up * 1.1f, up, .06f, 1.9f, 4); } z.mk.Cone(thatch, c + up * 2.9f, 4.6f, 1.9f, 14); z.mk.Cone(Paint(new Color(.66f, .52f, .28f)), c + up * 3.3f, 3.2f, 1.6f, 14, 12f);
            for (var k = 0; k < 6; k++) { var d = Quaternion.Euler(0, k * 60f + 30f, 0) * z.front; var st = c + d * 3.5f; st.y = z.ctx.Ground(st.x, st.z) + .08f; z.mk.Prism(woodM, st, up, .05f, .72f, 5); z.mk.Prism(Paint(Paints[k % Paints.Length]), st + up * .72f, up, .22f, .08f, 10); ArtistBits.PostCollider(z.root, st, .7f, .1f); Interact.Seat(tlife.transform, st + up * .82f, -d, "Sit at the bar").seatHeight = .8f; }
            for (var k = 0; k < 4; k++) { var d = Quaternion.Euler(0, k * 50f - 75f, 0) * z.front; var gm = new MeshKit(); gm.Prism(GlowM(k % 2 == 0 ? new Color(.6f, .9f, .4f) : new Color(1f, .7f, .3f), .5f), Vector3.zero, Vector3.up, .045f, .14f, 8, 1.2f); gm.Blob(Paint(new Color(.4f, .7f, .2f)), new Vector3(.03f, .15f, 0), new Vector3(.03f, .01f, .03f), rng, 0f); Thing(gm, "Drink", k % 2 == 0 ? "a caipirinha, mostly lime" : "a passion-fruit batida", c + d * 2.5f + up * 1.1f, Quaternion.identity, new Vector3(.11f, .18f, .11f), .15f, false); }
            Folk(c + z.front * 1.4f + up * .02f, z.front, Resident.Pose.Stand, new[] { "What'll it be? It's all lime and sunshine, the only question is how much ice.", "O-ba, o-ba, o-ba! Sorry. It gets in your head. It's been in mine for sixty years.", "Sit. The sea's not going anywhere and neither is the song." }, Local(Residents.Tool.None, true));
            ArtistBits.Label(z.labels, "MAS QUE NADA", c + up * 2.5f + z.front * 3.9f, z.front, .34f, Coral, TextAnchor.MiddleCenter); z.mk.Box(white, c + up * 2.5f + z.front * 3.86f, new Vector3(3.6f, .6f, .05f), rot);
            z.area.glowLight = ArtistBits.PointLight(z.root, c + up * 2.4f + z.front * 3f, new Color(1f, .85f, .6f), 22f, 1.6f); life.sambaAt = c;
            // the samba corner: a big surdo, a tamborim, bells, a pandeiro. Play.
            var sc = z.At(3f, 7.5f); var skin = SongGlow(mq, Cream, .5f); z.mk.Prism(Paint(Paints[2]), sc, up, .42f, .8f, 12); z.mk.Prism(skin, sc + up * .8f, up, .44f, .03f, 12); z.mk.Prism(Paint(Teal), sc + up * .2f, up, .44f, .06f, 12); z.mk.Prism(Paint(Teal), sc + up * .6f, up, .44f, .06f, 12);
            z.mk.Prism(Paint(Coral), sc + z.side * .9f + up * .5f, up, .16f, .1f, 10); z.mk.Prism(skin, sc + z.side * .9f + up * .6f, up, .17f, .02f, 10); z.mk.Prism(iron, sc + z.side * .9f, up, .02f, .5f, 4); foreach (var e in new[] { 0f, .24f }) z.mk.Cone(Paint(new Color(.8f, .78f, .7f)), sc - z.side * (.8f + e) + up * (.7f + e), .09f + e * .1f, -.26f, 8); z.mk.Prism(iron, sc - z.side * .9f, up, .02f, .8f, 4);
            z.mk.Prism(Paint(Paints[5]), sc + z.front * .8f + up * .5f, (up + z.front * .4f).normalized, .2f, .05f, 12); z.mk.Prism(iron, sc + z.front * .8f, up, .02f, .5f, 4); ArtistBits.PostCollider(z.root, sc, .8f, .44f);
            string[] samba = { "Boom, ba-da boom, tak! The barman starts wiping the counter in time.", "O-ba, o-ba, o-ba. You didn't mean to sing. Everybody sings.", "The tamborim chatters, the surdo answers, and your feet make a decision without you.", "It swings like a hammock. That's the whole secret." }; var sn = 0;
            Spot("The samba drums", sc + up * 1.1f, "Play the samba", new Vector3(2.4f, 1.2f, 1.8f)).onUse = _ => { life.played = Time.time; Say1(sc + up * 2.6f, samba[sn++ % samba.Length]); };
            // "Meus Filhos, Meu Tesouro": a treasure chest, and what's in it is toys
            var ch = z.At(3.5f, -7f); z.mk.Box(woodM, ch + up * .3f, new Vector3(1.2f, .6f, .8f), rot); z.mk.Box(woodM, ch + up * .76f - z.front * .42f, new Vector3(1.2f, .06f, .8f), rot * Quaternion.Euler(-70f, 0, 0)); foreach (var e in new[] { -.5f, .5f }) z.mk.Box(Paint(Sun), ch + up * .3f + z.side * e, new Vector3(.06f, .62f, .82f), rot); BoxCol(z.root, ch + up * .3f, new Vector3(1.2f, .6f, .8f), rot);
            var toy = SongGlow(mf, Sun, 1.2f); z.mk.Blob(toy, ch + up * .68f - z.side * .3f, Vector3.one * .14f, rng, 0f); z.mk.Box(SongGlow(mf, Coral, 1.2f), ch + up * .66f + z.side * .1f, new Vector3(.3f, .14f, .16f), rot); z.mk.Cone(SongGlow(mf, Teal, 1.2f), ch + up * .6f + z.side * .4f, .1f, .26f, 4); halos.Add(ch + up * .8f, 1f, Sun, .3f);
            // "O Filósofo": a deck chair, a pile of books, a football: think
            var ph = z.At(7f, 2f); z.mk.Box(Paint(Paints[4]), ph + up * .4f, new Vector3(.6f, .05f, 1.1f), rot * Quaternion.Euler(-14f, 0, 0)); z.mk.Box(bleached, ph + up * .2f, new Vector3(.64f, .4f, .9f), rot); Interact.Seat(tlife.transform, ph + up * .5f, z.front, "Sit and think").seatHeight = .46f; for (var k = 0; k < 4; k++) z.mk.Box(SongGlow(fi, Paints[(k * 2) % Paints.Length], .4f), ph + z.side * .8f + up * (.06f + k * .1f), new Vector3(.3f, .09f, .4f), rot * Quaternion.Euler(0, k * 13f, 0));
            var fb = new MeshKit(); fb.Blob(white, new Vector3(0, .12f, 0), Vector3.one * .12f, rng, 0f); for (var k = 0; k < 5; k++) fb.Blob(dark, new Vector3(0, .12f, 0) + Quaternion.Euler(k * 72f, k * 50f, 0) * Vector3.up * .1f, Vector3.one * .04f, rng, 0f); Thing(fb, "Football", "a football", ph - z.side * .9f + up * .1f, Quaternion.identity, new Vector3(.25f, .25f, .25f), .4f, false);
            Shot(z.ctx, "the beach bar", z.At(11f, 5f) + up * 2.2f, c + up * 1.8f);
        }

        // ---------------------------------------------------------------- Mulatu Astatke: the pavilion
        void MulatuAstatke(Plot z)
        {
            var up = Vector3.up; var rot = z.Rot; var rng = new System.Random(69); var c = z.At(-3f, 0); var tz = Song(z, "Yègellé Tezeta"); var ys = Song(z, "Yèkèrmo Sèw"); foreach (var s in AllSongs(z)) if (s != ys) life.tezeta.Add(s);
            // an eight-sided pavilion: a stone floor, white posts, a tall thatched roof
            z.mk.Prism(stone, c - up * .1f, up, 6f, .4f, 8); z.solid.Prism(stone, c - up * .4f, up, 6f, .7f, 8); z.mk.Prism(Paint(Paints[6]), c + up * .305f, up, 3f, .01f, 8); z.mk.Prism(Paint(Sun), c + up * .31f, up, 2.6f, .01f, 8); z.mk.Prism(Paint(Coral), c + up * .315f, up, 2.2f, .01f, 8);
            var fl = c + up * .3f; var posts = new List<Vector3>(); for (var k = 0; k < 8; k++) { var d = Quaternion.Euler(0, k * 45f + 22.5f, 0) * z.front; var p = fl + d * 5.4f; z.mk.Prism(white, p, up, .12f, 3.4f, 6); ArtistBits.PostCollider(z.root, p, 3.4f, .14f); posts.Add(p + up * 3.2f); }
            z.mk.Cone(thatch, fl + up * 3.3f, 6.8f, 3.2f, 8, 22.5f); z.mk.Cone(Paint(new Color(.66f, .52f, .28f)), fl + up * 5f, 3.4f, 2.4f, 8, 22.5f); z.area.glowLight = ArtistBits.PointLight(z.root, fl + up * 2.6f, new Color(1f, .85f, .6f), 24f, 1.8f);
            // the vibraphone: play it
            var vb = fl - z.side * 1.6f - z.front * .4f; var vr = rot * Quaternion.Euler(0, 20f, 0); var bar = SongGlow(tz, new Color(1f, .86f, .5f), .9f); for (var k = 0; k < 13; k++) { z.mk.Box(k % 2 == 0 ? bar : WorldMaterials.Solid(new Color(.8f, .8f, .84f), .9f), vb + up * .9f + vr * new Vector3(-.78f + k * .13f, 0, 0), new Vector3(.11f, .02f, .6f - k * .02f), vr); z.mk.Prism(WorldMaterials.Solid(new Color(.75f, .75f, .8f), .9f), vb + up * .86f + vr * new Vector3(-.78f + k * .13f, 0, 0), -up, .035f, .5f - k * .02f, 6); }
            foreach (var e in new[] { -.86f, .86f }) z.mk.Box(iron, vb + up * .45f + vr * new Vector3(e, 0, 0), new Vector3(.04f, .9f, .5f), vr); BoxCol(z.root, vb + up * .5f, new Vector3(1.9f, 1f, .7f), vr);
            string[] vibes = { "Four notes, falling, in a scale that sounds like remembering something.", "The bars ring and hang in the heat. Somebody you used to know nearly walks in.", "A slow figure, round and round, a little sad and entirely content.", "You let one chord shimmer. The photographs on the strings turn in the breeze." }; var vn = 0;
            Spot("The vibraphone", vb + up * 1.2f + vr * Vector3.forward * .5f, "Play the vibraphone", new Vector3(1.9f, 1f, 1.2f)).onUse = _ => { life.played = Time.time; Say1(vb + up * 2.6f, vibes[vn++ % vibes.Length]); };
            for (var k = 0; k < 2; k++) { var cp = fl + z.side * (1.8f + k * .6f) - z.front * (1.4f - k * .2f); z.mk.Prism(Paint(Paints[k * 2 + 1]), cp, up, .22f, .95f, 10, 1.15f); z.mk.Prism(white, cp + up * .95f, up, .26f, .03f, 10); } ArtistBits.PostCollider(z.root, fl + z.side * 2.1f - z.front * 1.3f, 1f, .5f);
            // "Tezeta": strings of old photographs between the posts
            var sepia = SongGlow(tz, new Color(.86f, .7f, .46f), .6f); for (var k = 0; k < 8; k++) { if (k == 7 || k == 0) continue; var a = posts[k]; var b = posts[(k + 1) % 8]; z.mk.Prism(dark, a - up * .5f, (b - a).normalized, .008f, (b - a).magnitude, 3); for (var q = 1; q < 5; q++) { var p = Vector3.Lerp(a, b, q / 5f) - up * .78f; var pr = Quaternion.LookRotation(-(p - fl - up * 2.4f).normalized.WithY0()); z.mk.Box(sepia, p, new Vector3(.34f, .44f, .01f), pr * Quaternion.Euler(0, 0, (q * 37 % 9) - 4f)); z.mk.Box(WorldMaterials.Solid(new Color(.4f, .3f, .2f)), p + pr * Vector3.forward * .008f, new Vector3(.26f, .3f, .01f), pr * Quaternion.Euler(0, 0, (q * 37 % 9) - 4f)); } }
            // the coffee ceremony: a low table, the black pot, small cups; take one
            var cf = fl + z.front * 2.6f + z.side * 1.6f; z.mk.Box(woodM, cf + up * .2f, new Vector3(1f, .06f, .6f), rot); foreach (var e in new[] { -.4f, .4f }) z.mk.Box(woodM, cf + up * .1f + z.side * e, new Vector3(.06f, .2f, .5f), rot); z.mk.Blob(dark, cf + up * .36f - z.side * .28f, new Vector3(.12f, .13f, .12f), rng, 0f); z.mk.Prism(dark, cf + up * .44f - z.side * .28f, (up + z.side * .2f).normalized, .03f, .26f, 6); z.mk.Blob(leafM, cf + up * .01f + z.front * .5f, new Vector3(.6f, .01f, .4f), rng, .3f);
            for (var k = 0; k < 3; k++) { var cm = new MeshKit(); cm.Prism(white, Vector3.zero, Vector3.up, .035f, .05f, 8, 1.2f); cm.Prism(WorldMaterials.Solid(new Color(.2f, .12f, .08f)), new Vector3(0, .045f, 0), Vector3.up, .036f, .006f, 8); Thing(cm, "Coffee", "a small cup of strong coffee", cf + up * .24f + z.side * (0f + k * .16f) + z.front * (k % 2 * .1f - .05f), Quaternion.identity, new Vector3(.09f, .08f, .09f), .1f, false); }
            Folk(cf - z.front * .1f + z.side * .9f, -z.side, Resident.Pose.Sit, new[] { "Three cups. The first for pleasure, the second for thinking, the third for blessing. Sit.", "Tezeta. It means memory, and it means longing, and it's the name of the tune. All three, at once.", "He went away and learned everything and came home and it sounded like here. That's the trick." }, null, null, .2f);
            // "Yèkèrmo Sèw": a wise man's chair, his stick and his hat
            var wc = fl - z.front * 2.6f + z.side * .2f; z.mk.Box(woodM, wc + up * .46f, new Vector3(.6f, .06f, .56f), rot); z.mk.Box(woodM, wc + up * .95f - z.front * .26f, new Vector3(.6f, 1f, .06f), rot); foreach (var e in new[] { -.3f, .3f }) { z.mk.Box(woodM, wc + up * .23f + z.side * e, new Vector3(.06f, .46f, .56f), rot); z.mk.Box(woodM, wc + up * .7f + z.side * e, new Vector3(.06f, .06f, .56f), rot); } Interact.Seat(tlife.transform, wc + up * .5f, z.front, "Sit in the wise man's chair");
            z.mk.Prism(woodM, wc + z.side * .5f, (up - z.side * .1f).normalized, .02f, 1f, 4); z.mk.Blob(SongGlow(ys, Sun, 1.2f), wc + z.side * .4f + up * 1.02f, Vector3.one * .04f, rng, 0f); z.mk.Prism(white, wc + up * 1.48f - z.front * .26f, up, .22f, .02f, 10); z.mk.Prism(white, wc + up * 1.5f - z.front * .26f, up, .12f, .12f, 10, .9f);
            Shot(z.ctx, "the pavilion", z.At(12f, -6f) + up * 2.2f, fl + up * 1.8f);
        }

        // ---------------------------------------------------------------- Tinariwen: the camp
        void Tinariwen(Plot z)
        {
            var up = Vector3.up; var rot = z.Rot; var rng = new System.Random(79); var songs = AllSongs(z); foreach (var s in songs) life.desert.Add(s); var s0 = songs.Count > 0 ? songs[0] : null; var c = z.At(-5f, 0);
            // the great tent: dark cloth over a ridge, open at the front and sides, rugs and cushions under it
            var cloth = Paint(new Color(.16f, .16f, .3f)); z.mk.GableRoof(cloth, c + up * 1.5f, new Vector3(7f, 0, 10f), 2.2f, Quaternion.LookRotation(z.side)); z.mk.Box(cloth, c + up * .9f - z.front * 3.5f, new Vector3(10f, 1.8f, .06f), rot * Quaternion.Euler(-14f, 0, 0)); z.solid.Box(cloth, c + up * .9f - z.front * 3.4f, new Vector3(10f, 1.8f, .2f), rot);
            foreach (var e in new[] { -1f, 0f, 1f }) { z.mk.Prism(woodM, c + z.side * e * 4.8f, up, .07f, 3.7f, 5); ArtistBits.PostCollider(z.root, c + z.side * e * 4.8f, 3.6f, .09f); foreach (var q in new[] { -1f, 1f }) { if (q < 0) continue; z.mk.Prism(woodM, c + z.side * e * 4.8f + z.front * 3.4f, up, .05f, 1.5f, 4); ArtistBits.PostCollider(z.root, c + z.side * e * 4.8f + z.front * 3.4f, 1.5f, .07f); } }
            for (var k = 0; k < 3; k++) { z.mk.Box(Paint(new[] { new Color(.6f, .16f, .14f), new Color(.8f, .5f, .2f), new Color(.5f, .14f, .2f) }[k]), c + up * .06f + z.side * (-3.2f + k * 3.2f), new Vector3(3f, .03f, 5f), rot); z.mk.Box(Paint(Cream), c + up * .08f + z.side * (-3.2f + k * 3.2f), new Vector3(2.2f, .02f, 4.2f), rot); z.mk.Box(Paint(new[] { new Color(.6f, .16f, .14f), new Color(.8f, .5f, .2f), new Color(.5f, .14f, .2f) }[k]), c + up * .09f + z.side * (-3.2f + k * 3.2f), new Vector3(1.6f, .02f, 3.4f), rot); }
            for (var k = 0; k < 5; k++) { var cu = c + z.side * (-3.6f + k * 1.8f) - z.front * 2f + up * .1f; z.mk.Blob(Paint(Paints[(k * 2 + 1) % Paints.Length]), cu + up * .14f, new Vector3(.4f, .16f, .34f), rng, .1f); Interact.Seat(tlife.transform, cu + up * .22f, z.front, "Sit on the cushion").seatHeight = .2f; }
            Folk(c - z.side * 1.8f - z.front * 2f + up * .3f, z.front, Resident.Pose.Sit, new[] { "Sit. The tea is coming. The tea is always coming.", "We carried the guitars across the desert. The amplifier we carried twice.", "The song is about home. All of them are about home." }, Nomad(), null, .2f);
            Folk(c + z.side * 3.4f - z.front * 2f + up * .3f, (z.front - z.side * .3f).normalized, Resident.Pose.Sit, new[] { "(He nods in time, slowly. The riff comes round again like a horizon.)", "Assouf. It means the longing. Also it means: play it again." }, Nomad(), null, .2f);
            // the fire out front, a ring of stones
            var fp = z.At(3f, 0) + up * .06f; for (var k = 0; k < 8; k++) z.mk.Blob(stone, fp + Quaternion.Euler(0, k * 45f, 0) * Vector3.forward * .9f + up * .1f, Vector3.one * .2f, rng, .2f); for (var k = 0; k < 4; k++) { var d = Quaternion.Euler(0, k * 45f + 10f, 0) * Vector3.forward; z.mk.Prism(woodM, fp - d * .8f + up * .12f, (d + up * .25f).normalized, .09f, 1.6f, 5); }
            z.mk.Cone(fireM, fp + up * .14f, .44f, 1.4f, 6); z.mk.Cone(fireM, fp + up * .14f + z.side * .2f, .26f, .9f, 5, 30f); halos.Add(fp + up * .8f, 2.6f, new Color(1f, .6f, .2f), .5f); z.area.glowLight = ArtistBits.PointLight(z.root, fp + up * 1.4f, new Color(1f, .65f, .3f), 22f, 2.4f); ArtistBits.PostCollider(z.root, fp, 1f, .8f); life.sparks.Add(fp + up * 1.2f);
            // three guitars and a small amp: play the riff
            var gt = c + z.front * 1f; for (var k = 0; k < 3; k++) { var g = gt + z.side * (-1.2f + k * 1.2f); z.mk.Prism(iron, g, up, .12f, .03f, 6); Guitar(z.mk, g, rot, k == 1 ? SongGlow(s0, Sun, .5f) : Paint(k == 0 ? new Color(.5f, .14f, .14f) : Cream), rng, 1.05f); }
            var amp = gt + z.side * 2.6f; z.mk.Box(dark, amp + up * .4f, new Vector3(.8f, .8f, .45f), rot); z.mk.Box(Paint(new Color(.6f, .52f, .36f)), amp + up * .36f + z.front * .23f, new Vector3(.66f, .56f, .02f), rot); z.mk.Blob(SongGlow(s0, Coral, 2f), amp + up * .74f + z.front * .24f + z.side * .3f, Vector3.one * .03f, rng, 0f); BoxCol(z.root, amp + up * .4f, new Vector3(.8f, .8f, .45f), rot);
            string[] riff = { "One riff, loping, like a camel that knows the way. It doesn't resolve. It doesn't want to.", "The guitar says a thing. The other guitars say it back, a little differently. For as long as you like.", "You play it clean and dry, and the dunes seem to line up behind you.", "A hand-clap joins in from under the tent, exactly where the drum would be." }; var rn = 0;
            Spot("The camp's guitars", gt + up * 1.1f, "Play the desert riff", new Vector3(3.2f, 1.6f, 1.2f)).onUse = _ => { life.played = Time.time; Say1(gt + up * 2.8f, riff[rn++ % riff.Length]); };
            // tea: a tray, a pot, small glasses; take one
            var tr = z.At(1.5f, 3.4f) + up * .08f; z.mk.Prism(WorldMaterials.Solid(new Color(.8f, .66f, .3f), .8f), tr, up, .4f, .03f, 12); z.mk.Blob(WorldMaterials.Solid(new Color(.3f, .5f, .6f), .8f), tr + up * .16f, new Vector3(.12f, .13f, .12f), rng, 0f); z.mk.Prism(WorldMaterials.Solid(new Color(.3f, .5f, .6f), .8f), tr + up * .2f, (up + z.side).normalized, .02f, .2f, 4);
            for (var k = 0; k < 3; k++) { var tm = new MeshKit(); tm.Prism(GlowM(new Color(.9f, .6f, .2f), .5f), Vector3.zero, Vector3.up, .025f, .07f, 8, 1.2f); Thing(tm, "Tea", "a small glass of sweet tea", tr + up * .04f + Quaternion.Euler(0, k * 100f + 40f, 0) * Vector3.forward * .26f, Quaternion.identity, new Vector3(.07f, .09f, .07f), .1f, false); }
            // two smaller tents behind, water cans, a rolled rug
            SmallTent(z.ctx, z.mk, z.At(-10f, -8f), z.front, 0); SmallTent(z.ctx, z.mk, z.At(-9f, 9f), (z.front - z.side * .3f).normalized, 1); for (var k = 0; k < 3; k++) { var jc = z.At(-1f, -8f - k * .6f); z.mk.Box(Paint(k == 1 ? Paints[4] : new Color(.86f, .8f, .5f)), jc + up * .28f, new Vector3(.3f, .5f, .4f), rot * Quaternion.Euler(0, k * 20f, 0)); }
            Shot(z.ctx, "the camp in the dunes", z.At(13f, 7f) + up * 2.4f, c + up * 1.6f);
        }

        // ---------------------------------------------------------------- everyone else
        void Others(Plot z)
        {
            var up = Vector3.up; var rot = z.Rot; var p = z.At(2.8f, -3.2f); var rng = new System.Random(z.a.name.Length * 3); string s;
            switch (z.a.name)
            {
                case "Elis Regina":
                    s = Song(z, "Águas"); life.aguasSong = s; var water = SongGlow(s, new Color(.3f, .75f, .9f), .9f); for (var k = 0; k < 9; k++) z.mk.Box(water, p + z.side * (-2f + k * .5f) + z.front * Mathf.Sin(k * .8f) * .4f + up * .11f, new Vector3(.56f, .02f, .5f), rot * Quaternion.Euler(0, Mathf.Cos(k * .8f) * 30f, 0));
                    z.mk.Box(woodM, p - z.side * .9f + up * .2f + z.front * .7f, new Vector3(.9f, .05f, .05f), rot * Quaternion.Euler(0, 30f, 8f)); z.mk.Blob(stone, p + z.side * .2f + up * .2f + z.front * .8f, new Vector3(.2f, .14f, .18f), rng, .2f); z.mk.Blob(glassM, p + z.side * .9f + up * .14f + z.front * .7f, new Vector3(.08f, .02f, .12f), rng, .4f);
                    var boat = new MeshKit(); boat.Box(white, new Vector3(0, .04f, 0), new Vector3(.12f, .06f, .3f), Quaternion.identity); boat.Tri(white, new Vector3(0, .07f, -.06f), new Vector3(0, .07f, .08f), new Vector3(0, .26f, 0), new Vector3(1, .1f, 0)); boat.Tri(white, new Vector3(0, .07f, .08f), new Vector3(0, .07f, -.06f), new Vector3(0, .26f, 0), new Vector3(-1, .1f, 0)); Thing(boat, "Paper boat", "a paper boat", p + up * .13f, rot, new Vector3(.14f, .28f, .3f), .05f);
                    ArtistBits.Label(z.labels, "é pau, é pedra,\né o fim do caminho", p + up * 1.3f - z.front * .6f, z.front, .14f, Teal, TextAnchor.MiddleCenter); break;
                case "Marcos Valle":
                    s = Song(z, "Estrelar"); life.estrelarSong = s; life.starsAt = p; z.mk.Prism(white, p - up * .2f, up, .06f, 4f, 5); ArtistBits.PostCollider(z.root, p, 3.8f, .08f); var star = SongGlow(s, Sun, 2f); for (var a = 0; a < 3; a++) { z.mk.Box(white, p + up * (3.9f - a * .1f), new Vector3(2.6f - a * .6f, .03f, .03f), rot * Quaternion.Euler(0, a * 60f, 0)); foreach (var e in new[] { -1f, 1f }) { var sp = p + up * (3.9f - a * .1f) + rot * Quaternion.Euler(0, a * 60f, 0) * Vector3.right * e * (1.3f - a * .3f); z.mk.Prism(dark, sp, -up, .006f, .6f + a * .3f, 3); var sc = sp - up * (.8f + a * .3f); for (var k = 0; k < 5; k++) z.mk.Box(star, sc + Quaternion.AngleAxis(k * 72f, z.front) * up * .15f, new Vector3(.1f, .3f, .04f), Quaternion.AngleAxis(k * 72f, z.front) * rot); } }
                    halos.Add(p + up * 3f, 2.4f, Sun, .3f); foreach (var e in new[] { -.3f, .3f }) { z.mk.Prism(iron, p + z.front * 1.4f + z.side * e - z.side * .2f + up * .12f, z.side, .03f, .4f, 5); foreach (var q in new[] { 0f, .4f }) z.mk.Prism(Paint(Paints[5]), p + z.front * 1.4f + z.side * (e - .2f + q) + up * .12f - z.side * .04f, z.side, .1f, .08f, 8); } break;
                case "Rodrigo y Gabriela":
                    s = Song(z, "Tamac"); for (var k = 0; k < 2; k++) { var cp = p + z.side * (k == 0 ? -1f : 1f); var cf = k == 0 ? z.side : -z.side; var cr = Quaternion.LookRotation(cf); z.mk.Box(woodM, cp + up * .45f, new Vector3(.46f, .05f, .46f), cr); z.mk.Box(woodM, cp + up * .8f - cf * .21f, new Vector3(.46f, .7f, .05f), cr); foreach (var (x, zz) in new[] { (-.2f, -.2f), (.2f, -.2f), (.2f, .2f), (-.2f, .2f) }) z.mk.Box(woodM, cp + up * .22f + cr * new Vector3(x, 0, zz), new Vector3(.04f, .44f, .04f), cr); Interact.Seat(tlife.transform, cp + up * .5f, cf); Guitar(z.mk, cp + cf * .5f + z.front * .3f, cr, k == 0 ? SongGlow(s, new Color(.9f, .7f, .4f), .4f) : Paint(new Color(.6f, .36f, .2f)), rng); }
                    var croc = new MeshKit(); var green = Paint(new Color(.3f, .5f, .26f)); croc.Blob(green, new Vector3(0, .16f, 0), new Vector3(.26f, .14f, .8f), rng, .05f); croc.Box(green, new Vector3(0, .14f, 1f), new Vector3(.24f, .12f, .7f), Quaternion.identity); croc.Prism(green, new Vector3(0, .14f, -.7f), Vector3.back, .12f, 1.1f, 5, .1f); foreach (var e in new[] { -.1f, .1f }) croc.Blob(SongGlow(s, Sun, 1.6f), new Vector3(e, .26f, .66f), Vector3.one * .04f, rng, 0f); for (var k = 0; k < 6; k++) croc.Cone(white, new Vector3((k % 2 == 0 ? -.1f : .1f), .2f, .86f + (k / 2) * .16f), .02f, -.06f, 4);
                    var cgo = croc.BuildSingle("The crocodile", tlife.transform, false); cgo.transform.SetPositionAndRotation(z.At(4.6f, 1.6f) + up * .1f, rot * Quaternion.Euler(0, 110f, 0)); string[] tam = { "He doesn't move. They say he looked after the town's crocodiles. They say one of them is him.", "A slow blink. The two guitars speed up, as if he'd asked them to.", "He smiles. He has no choice about that, but you feel he means it." }; var tn = 0;
                    Interact.Toy(cgo, Toy.Kind.Custom, "Greet the crocodile", new Vector3(.8f, .6f, 2.6f)).onUse = _ => Say1(z.At(4.6f, 1.6f) + up * 1.6f, tam[tn++ % tam.Length]);
                    for (var k = 0; k < 9; k++) z.mk.Box(Paint(Paints[k % Paints.Length]), p + up * 2.6f + z.side * (-2f + k * .5f) - up * Mathf.Sin(k / 8f * Mathf.PI) * .3f, new Vector3(.4f, .5f, .01f), rot); foreach (var e in new[] { -2.2f, 2.2f }) { z.mk.Prism(white, p + z.side * e - up * .2f, up, .04f, 3f, 4); ArtistBits.PostCollider(z.root, p + z.side * e, 2.8f, .06f); } break;
                case "Herb Alpert & The Tijuana Brass":
                    s = Song(z, "Spanish Flea"); z.mk.Box(Paint(Coral), p + up * .5f, new Vector3(1.2f, 1f, .8f), rot); BoxCol(z.root, p + up * .5f, new Vector3(1.2f, 1f, .8f), rot); z.mk.Cone(Paint(Sun), p + up * 1.5f, .5f, .5f, 8); z.mk.Cone(Paint(Coral), p + up * 1.52f, .26f, .46f, 8, 22.5f); for (var k = 0; k < 4; k++) z.mk.Prism(white, p + up * 1f + Quaternion.Euler(0, k * 90f + 45f, 0) * Vector3.forward * .4f, up, .012f, .5f, 3); z.mk.Prism(dark, p + up * 1.34f - z.side * .3f, z.side, .004f, .6f, 3); z.mk.Blob(SongGlow(s, Sun, 2.4f), p + up * 1.3f, Vector3.one * .012f, rng, 0f);
                    ArtistBits.Label(z.labels, "FLEA CIRCUS", p + up * .6f + z.front * .41f, z.front, .14f, Cream, TextAnchor.MiddleCenter); string[] flea = { "A speck does a triple somersault off a matchstick. You applaud a speck.", "The flea rides a tiny bicycle across a hair. A tiny trumpet plays, you'd swear.", "You lose sight of him. Then your ankle itches, in three-four time." }; var fn = 0;
                    Spot("The flea circus", p + up * 1.3f, "Watch the flea circus", new Vector3(1.2f, 1f, .9f)).onUse = _ => Say1(p + up * 2.6f, flea[fn++ % flea.Length]);
                    var tp = z.At(4.4f, 1.4f); z.mk.Prism(iron, tp, up, .14f, .03f, 6); z.mk.Prism(iron, tp, up, .015f, 1f, 4); var bf = z.side; var br = WorldMaterials.Solid(new Color(.9f, .72f, .3f), .8f); z.mk.Prism(br, tp + up * 1.05f - bf * .3f, bf, .018f, .5f, 5); z.mk.Prism(br, tp + up * 1.05f + bf * .2f, bf, .025f, .18f, 8, 3.6f); z.mk.Box(br, tp + up * 1.09f - bf * .05f, new Vector3(.05f, .1f, .16f), Quaternion.LookRotation(bf)); break;
                case "Santo & Johnny":
                    s = Song(z, "Sleep Walk"); life.sleepSong = s; z.mk.Box(Paint(Cream), p + up * .8f, new Vector3(.9f, .08f, .3f), rot); for (var k = 0; k < 6; k++) z.mk.Box(SongGlow(s, new Color(.8f, .8f, 1f), .8f), p + up * .845f + z.front * (-.1f + k * .04f), new Vector3(.86f, .004f, .006f), rot); foreach (var (x, zz) in new[] { (-.4f, -.1f), (.4f, -.1f), (.4f, .1f), (-.4f, .1f) }) z.mk.Prism(iron, p + z.side * x + z.front * zz, up, .015f, .78f, 4); z.mk.Prism(WorldMaterials.Solid(new Color(.8f, .8f, .84f), .9f), p + up * .86f + z.side * .1f, z.side, .02f, .1f, 6); BoxCol(z.root, p + up * .45f, new Vector3(.9f, .9f, .3f), rot);
                    Hammock(z.ctx, z.mk, z.At(4.6f, 1.2f), z.side, 3); var moon = SongGlow(s, new Color(.9f, .9f, 1f), 1.2f); z.mk.Prism(white, p - z.side * 1.4f - up * .2f, up, .04f, 3.2f, 4); z.mk.Blob(moon, p - z.side * 1.4f + up * 3.3f, Vector3.one * .4f, rng, 0f); z.mk.Blob(Paint(Cream), z.At(4.6f, 1.2f) + up * .76f + z.side * 1.1f, new Vector3(.22f, .08f, .16f), rng, .1f); ArtistBits.PostCollider(z.root, p - z.side * 1.4f, 3f, .06f); break;
                case "Tony Bennett":
                    s = Song(z, "Body and Soul"); z.mk.Box(dark, p + up * .65f, new Vector3(1.4f, 1.3f, .55f), rot); z.mk.Box(white, p + up * .78f + z.front * .32f, new Vector3(1.3f, .04f, .2f), rot); BoxCol(z.root, p + up * .65f, new Vector3(1.4f, 1.3f, .6f), rot); var st = p + z.front * .95f; z.mk.Box(dark, st + up * .25f, new Vector3(.8f, .5f, .36f), rot); Interact.Seat(tlife.transform, st + up * .52f, -z.front, "Sit at the piano").seatHeight = .52f;
                    foreach (var e in new[] { -1f, 1f }) { var mic = p + z.side * e * 1.4f + z.front * .9f; z.mk.Prism(iron, mic, up, .16f, .03f, 8); z.mk.Prism(iron, mic, up, .016f, 1.45f, 4); z.mk.Blob(SongGlow(s, e < 0 ? Cream : Coral, 1.2f), mic + up * 1.5f, new Vector3(.06f, .09f, .06f), rng, 0f); ArtistBits.PostCollider(z.root, mic, 1.4f, .06f); }
                    z.mk.Prism(glassM, p + up * 1.32f + z.side * .4f, up, .04f, .18f, 6); z.mk.Prism(leafDark, p + up * 1.5f + z.side * .4f, up, .008f, .2f, 3); z.mk.Blob(Paint(new Color(.85f, .15f, .2f)), p + up * 1.72f + z.side * .4f, Vector3.one * .05f, rng, .2f); break;
                case "Harry Belafonte":
                    s = Song(z, "Banana Boat"); life.dayoSong = s; var hull = Paint(Paints[0]); var bo = z.At(1.6f, -3.6f); var brot = rot * Quaternion.Euler(0, 80f, 0); z.mk.Box(hull, bo + up * .5f, new Vector3(1.8f, .8f, 5f), brot); z.mk.Box(white, bo + up * .92f, new Vector3(1.9f, .08f, 5.1f), brot); z.mk.Box(woodM, bo + up * .86f, new Vector3(1.5f, .06f, 4.6f), brot); foreach (var e in new[] { -1f, 1f }) z.mk.Prism(hull, bo + up * .5f + brot * Vector3.forward * e * 2.5f, brot * (Vector3.forward * e + Vector3.up * .4f).normalized, .6f, 1.1f, 4, .2f); foreach (var e in new[] { -1f, 1f }) z.mk.Box(woodM, bo + up * .1f + brot * Vector3.forward * e * 1.4f, new Vector3(2.2f, .2f, .2f), brot); BoxCol(z.root, bo + up * .5f, new Vector3(1.9f, 1f, 5.4f), brot);
                    var ban = SongGlow(s, new Color(1f, .88f, .2f), .9f); for (var b = 0; b < 4; b++) for (var k = 0; k < 6; k++) z.mk.Box(ban, bo + up * (1f + (k / 3) * .1f) + brot * new Vector3(-.3f + (k % 3) * .3f, 0, -1.6f + b * 1.05f), new Vector3(.08f, .08f, .5f), brot * Quaternion.Euler(0, (k % 3) * 14f - 14f, 20f));
                    var tb = z.At(4.6f, 1.2f); foreach (var e in new[] { -.8f, .8f }) { z.mk.Prism(woodM, tb + z.side * e - up * .2f, up, .05f, 2.4f, 4); ArtistBits.PostCollider(z.root, tb + z.side * e, 2.2f, .07f); } z.mk.Box(dark, tb + up * 1.6f, new Vector3(1.8f, 1.2f, .06f), rot); ArtistBits.Label(z.labels, "TALLY\n||||  ||||  ||||\n6 foot · 7 foot · 8 foot\nBUNCH", tb + up * 1.6f + z.front * .04f, z.front, .13f, Cream, TextAnchor.MiddleCenter);
                    string[] tally = { "Six foot, seven foot, eight foot, BUNCH. You lose count and start again, happily.", "\"Day-O!\" you shout. From somewhere down the beach: \"Daaaay-O!\"", "Daylight come. It was already here, but it comes a bit more." }; var yn = 0; Spot("The tally board", tb + up * 1.4f + z.front * .5f, "Count the bananas", new Vector3(1.8f, 1.6f, 1f)).onUse = _ => { life.played = Time.time; Say1(tb + up * 2.9f, tally[yn++ % tally.Length]); };
                    var bm = new MeshKit(); for (var q = 0; q < 5; q++) bm.Box(GlowM(new Color(1f, .88f, .2f), .5f), new Vector3(-.12f + q * .06f, .05f, 0), new Vector3(.05f, .05f, .36f), Quaternion.Euler(0, q * 6f - 12f, 18f)); Thing(bm, "Bananas", "a hand of bananas", z.At(4.4f, -1f) + up * .12f, rot, new Vector3(.34f, .14f, .4f), .3f); break;
                case "Keith Mansfield":
                    s = Song(z, "Funky Fanfare"); foreach (var e in new[] { -2.2f, 2.2f }) { z.mk.Prism(white, p + z.side * e - up * .2f, up, .08f, 4.2f, 5); ArtistBits.PostCollider(z.root, p + z.side * e, 4f, .1f); } z.mk.Box(white, p + up * 2.8f, new Vector3(4.6f, 2.6f, .1f), rot); var cols = new[] { Paints[1], Paints[2], Paints[0], Paints[5], Paints[4] }; for (var k = 0; k < 5; k++) z.mk.Prism(SongGlow(s, cols[k], .7f), p + up * 2.8f + z.front * (.06f + k * .004f), z.front, 1.2f - k * .22f, .01f, 20);
                    ArtistBits.Label(z.labels, "OUR FEATURE\nPRESENTATION", p + up * 2.8f + z.front * .1f, z.front, .22f, Ink, TextAnchor.MiddleCenter); for (var r = 0; r < 2; r++) for (var k = 0; k < 3; k++) { var sp = z.At(5f + r * 1.2f, -4.6f + k * 1f) + up * .08f; z.mk.Box(Paint(Coral), sp + up * .42f, new Vector3(.6f, .1f, .5f), rot * Quaternion.Euler(0, 180f, 0)); z.mk.Box(Paint(Coral), sp + up * .8f + z.front * .24f, new Vector3(.6f, .7f, .08f), rot); Interact.Seat(tlife.transform, sp + up * .5f, -z.front); }
                    var pj = z.At(7.6f, -3.6f); z.mk.Prism(iron, pj, up, .03f, 1.3f, 4); z.mk.Box(dark, pj + up * 1.4f, new Vector3(.3f, .3f, .5f), rot); foreach (var e in new[] { -.2f, .2f }) z.mk.Prism(dark, pj + up * 1.7f + z.front * e - z.side * .03f, z.side, .16f, .06f, 10); z.mk.Blob(SongGlow(s, Cream, 2f), pj + up * 1.4f - z.front * .27f, Vector3.one * .05f, rng, 0f); ArtistBits.PostCollider(z.root, pj, 1.4f, .1f); break;
                case "Dave Richmond":
                    s = Song(z, "Visit to Motown"); z.mk.Box(white, p + up * 1.3f, new Vector3(3f, 2f, .08f), rot); foreach (var e in new[] { -1.4f, 1.4f }) { z.mk.Prism(white, p + z.side * e - up * .2f, up, .05f, 2.6f, 4); ArtistBits.PostCollider(z.root, p + z.side * e, 2.4f, .07f); } for (var k = 0; k < 3; k++) { z.mk.Box(Paint(Paints[(k * 2 + 1) % Paints.Length]), p + up * 1.4f + z.side * (-.95f + k * .95f) + z.front * .05f, new Vector3(.8f, 1.2f, .01f), rot); z.mk.Prism(Paint(Sun), p + up * 1.6f + z.side * (-.95f + k * .95f) + z.front * .06f, z.front, .2f, .005f, 12); }
                    ArtistBits.Label(z.labels, "TRAVEL BUREAU  ·  see the world by ear", p + up * 2.14f + z.front * .05f, z.front, .1f, Ink, TextAnchor.MiddleCenter); var car = new MeshKit(); car.Box(SongGlow(s, Coral, .5f), new Vector3(0, .06f, 0), new Vector3(.12f, .06f, .3f), Quaternion.identity); car.Box(glassM, new Vector3(0, .11f, -.02f), new Vector3(.11f, .05f, .14f), Quaternion.identity); foreach (var e in new[] { -.06f, .06f }) foreach (var q in new[] { -.1f, .1f }) car.Prism(dark, new Vector3(e, .03f, q), Vector3.right * Mathf.Sign(e), .03f, .02f, 6);
                    Thing(car, "Toy car", "a little toy car from Detroit", p + z.front * 1.2f + up * .12f, rot * Quaternion.Euler(0, 70f, 0), new Vector3(.14f, .14f, .3f), .1f); Guitar(z.mk, p + z.side * 1.9f + z.front * .6f, rot, Paint(new Color(.2f, .2f, .24f)), rng, 1.2f); break;
                case "Esperanza Spalding":
                    s = Song(z, "Judas"); var wood = WorldMaterials.Solid(new Color(.52f, .34f, .2f), .6f); z.mk.Prism(iron, p, up, .012f, .25f, 4); z.mk.Blob(wood, p + up * .78f, new Vector3(.34f, .58f, .15f), rng, .04f); z.mk.Box(dark, p + up * 1.62f, new Vector3(.07f, 1f, .05f), rot); z.mk.Box(wood, p + up * 2.16f, new Vector3(.1f, .16f, .08f), rot); ArtistBits.PostCollider(z.root, p, 2f, .3f);
                    var dish = z.At(4.4f, 1.4f); z.mk.Prism(stone, dish, up, .3f, .6f, 8); z.mk.Prism(dark, dish + up * .6f, up, .4f, .05f, 12, 1.2f); var silver = SongGlow(s, new Color(.85f, .9f, 1f), 1.2f); for (var k = 0; k < 30; k++) z.mk.Prism(silver, dish + up * (.66f + (k / 10) * .012f) + Quaternion.Euler(0, k * 37f, 0) * Vector3.forward * (.08f + (k % 5) * .06f), up, .035f, .008f, 8); ArtistBits.PostCollider(z.root, dish, .7f, .34f); break;
                case "Robert Stillman":
                    s = Song(z, "It's All Is"); for (var k = 0; k < 9; k++) { var d = Quaternion.Euler(0, k * 40f, 0) * z.front; var sp = p + d * 1.8f; sp.y = z.ctx.Ground(sp.x, sp.z); z.mk.Blob(stone, sp + up * .16f, new Vector3(.26f, .2f + (k % 3) * .06f, .22f), rng, .25f); } z.mk.Prism(iron, p, up, .14f, .03f, 6); z.mk.Prism(iron, p, up, .015f, .5f, 4); var brs = SongGlow(s, new Color(.95f, .76f, .3f), .5f); z.mk.Prism(brs, p + up * .3f, (up - z.front * .22f).normalized, .045f, .75f, 6, .6f); z.mk.Prism(brs, p + up * .22f + z.front * .2f, (up + z.front * .3f).normalized, .05f, .32f, 8, 2.4f); break;
                case "Bossario":
                    s = Song(z, "By the Sea"); z.mk.Box(Paint(Paints[4]), p + up * .4f, new Vector3(.6f, .05f, 1.1f), rot * Quaternion.Euler(-14f, 0, 0)); z.mk.Box(bleached, p + up * .2f, new Vector3(.64f, .4f, .9f), rot); Interact.Seat(tlife.transform, p + up * .5f, z.front, "Sit in the deck chair").seatHeight = .46f; var cr2 = p + z.side * 1f; z.mk.Box(bleached, cr2 + up * .22f, new Vector3(.6f, .44f, .6f), rot); z.mk.Box(Paint(Teal), cr2 + up * .5f, new Vector3(.5f, .12f, .44f), rot); z.mk.Prism(dark, cr2 + up * .57f, up, .17f, .01f, 14); z.mk.Prism(SongGlow(s, Coral, 1f), cr2 + up * .58f, up, .06f, .01f, 10); BoxCol(z.root, cr2 + up * .3f, new Vector3(.6f, .6f, .6f), rot); Guitar(z.mk, p - z.side * .9f, rot, Paint(new Color(.8f, .6f, .36f)), rng); break;
                default:
                    z.mk.Prism(bleached, p, up, .05f, 1.2f, 5); z.mk.Blob(GlowM(Teal, 1.2f), p + up * 1.36f, Vector3.one * .18f, rng, 0f); break;
            }
        }
    }

    static class FarVectorExtensions
    {
        /// The same direction with no up or down in it.
        public static Vector3 WithY0(this Vector3 v) { v.y = 0; return v.sqrMagnitude > .0001f ? v.normalized : Vector3.forward; }
    }
}
