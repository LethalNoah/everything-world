using System.Collections.Generic;
using UnityEngine;
using static EverythingWorld.World.PlaceTools;

namespace EverythingWorld.World
{
    /// The Melodic Bass Highlands' artists. Most stand on a round dais of turf ringed with a rune band, a tall rune stone
    /// with their name on it, a little floating island drifting over them, and one thing from their music; covers on a
    /// violet board edged in gold.
    /// - Seven Lions: seven stone lions in a ring round a pool, a great crystal floating over it ("Creation"), a glowing
    ///   flower ("Nepenthe"), a little floating rock pouring a waterfall into the pool ("The Fall"), a rising sun ("Days to
    ///   Come") and a floating crystal for every other song.
    /// - Mr FijiWiji: a bed on a rock under the stars ("Insomnia"), a crystal ball ("The Mentalist"), paper lanterns rising
    ///   from a stand ("Time to Say Goodbye"), a mask ("Cynical") and a bunch of flowers ("Gina").
    /// - The rest: a tiny neon skyline, a giant glowing flower, a crystal cluster, a tree of gold leaves, a fox, dancing
    ///   crabs, a dark cube of pixels, a misty pool, a crescent moon, an orb in a bowl, a potion bottle, a clock face with a
    ///   coiled snake, a sculpture of rings.
    public partial class HighKit
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
                Add("Noisestorm", "Crab Rave", "Noisestorm"); Add("Mr FijiWiji", "Time to Say Goodbye", "Mr FijiWiji"); Add("Mr FijiWiji", "Insomnia", "Mr FijiWiji"); Add("Dabin", "Bloom", "Dabin");
                Add("Seven Lions", "The Fall", "Seven Lions"); Add("Seven Lions", "Days to Come", "Seven Lions"); Add("Adventure Club", "Retro City", "Adventure Club"); Add("Rogue", "Nightfall", "Rogue");
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
            if (rock == null) MakeMaterials();
            var root = new GameObject("Artist: " + a.name).transform; root.SetParent(ctx.Root, false);
            var front = facing.TryGetValue(a.name, out var f) ? f : new Vector3(center.x - a.x, 0, center.z - a.z).normalized;
            if (front.sqrMagnitude < .01f) front = Vector3.forward;
            var labels = new GameObject("Labels").transform; labels.SetParent(root, false);
            var area = root.gameObject.AddComponent<ArtistArea>();
            area.artistName = a.name; area.stationId = a.stationId; area.radius = a.r; area.glowColor = Pink;
            var z = new Plot { ctx = ctx, root = root, labels = labels, mk = new MeshKit(), solid = new MeshKit(), pictures = new MeshKit(), a = a, c = ctx.OnGround(a.x, a.z), front = front, side = Vector3.Cross(Vector3.up, front), area = area };
            var reach = Foot(a); var up = Vector3.up;
            switch (a.name)
            {
                case "Seven Lions": Lions(z); break;
                case "Mr FijiWiji": Fiji(z); break;
                default: Others(z, Dais(z)); break;
            }
            SongLamps(z);
            Board(z, z.At(reach * .6f, -reach * .45f - 1.5f), front);
            ArtistBits.Plaque(z.mk, z.solid, labels, a, z.At(reach * .6f + 1.5f, reach * .45f + 1.5f), front, iron, Paint(Indigo), Gold, 1.6f);
            Lamp(ctx, z.mk, z.At(reach * .5f, reach * .7f), false);
            if (z.area.glowLight == null) z.area.glowLight = ArtistBits.PointLight(root, z.c + up * 4f + front * 3f, new Color(1f, .7f, .8f), HasStation(a) ? 24f : 14f, 1.3f);
            area.glowRenderers = new Renderer[0];
            z.mk.Build("Area", root, false);
            z.solid.BuildCollider("Walls", root);
            z.pictures.Build("Covers", root, false);
            noGrass.Add(new Vector3(z.c.x, reach * .9f, z.c.z));
            var mark = new GameObject("Shot: " + a.name + " up close").transform; mark.SetParent(ctx.Root, false);
            var cam = z.At(reach * .85f, reach * .3f) + up * 2f; var look = z.At(-reach * .3f, 0) + up * 1.8f - cam; mark.SetPositionAndRotation(cam, Quaternion.LookRotation(look.normalized));
        }

        /// The covers on a violet board edged in gold.
        void Board(Plot z, Vector3 ground, Vector3 dir)
        {
            dir = Vector3.ProjectOnPlane(dir, Vector3.up).normalized; var rot = Quaternion.LookRotation(dir);
            var n = Mathf.Max(1, Mathf.Min(7, z.a.songList?.Length ?? 1)); const float size = .9f; var w = n * size * 1.25f + .6f;
            var c = ground + Vector3.up * 1.7f;
            z.mk.Box(lampM, c - Vector3.up * .5f - dir * .08f, new Vector3(w + .5f, size + 2f, .14f), rot); z.mk.Box(Paint(Violet), c - Vector3.up * .5f - dir * .02f, new Vector3(w + .3f, size + 1.8f, .2f), rot); z.solid.Box(iron, c - Vector3.up * .5f, new Vector3(w + .5f, size + 2f, .3f), rot);
            z.mk.Box(Paint(new Color(.92f, .88f, .94f)), c - Vector3.up * .1f + dir * .09f, new Vector3(w, size + .9f, .04f), rot);
            ArtistBits.CoverWall(z.pictures, z.labels, z.ctx, z.a, c + dir * .12f + Vector3.up * .08f, dir, n, size, n, true, Ink);
        }

        /// A small floating crystal over a rune post for each song without its own piece.
        void SongLamps(Plot z)
        {
            if (!SongIds.TryGetValue(z.a.name, out var list)) return;
            var up = Vector3.up; var k = 0; var seen = new HashSet<string>();
            foreach (var (_, id) in list)
            {
                if (z.lit.Contains(id) || !seen.Add(id)) continue;
                var p = z.At(Foot(z.a) * .55f - (k % 3) * 1.3f, Foot(z.a) * .2f + (k / 3) * 1.3f);
                z.mk.Box(rock, p + up * .5f, new Vector3(.3f, 1f, .3f), z.Rot); z.mk.Prism(SongGlow(id, k % 2 == 0 ? Pink : Rune, 1.6f), p + up * 1.3f, up, .14f, .5f, 5, .05f); z.mk.Prism(SongGlow(id, k % 2 == 0 ? Pink : Rune, 1.6f), p + up * 1.3f, -up, .14f, .25f, 5, .05f); ArtistBits.PostCollider(z.root, p, 1.4f, .2f);
                k++;
            }
        }

        /// The dais: turf ringed with a rune band, a tall rune stone with their name, a little island floating over it.
        PlaceFrame Dais(Plot z)
        {
            var up = Vector3.up; var f = new PlaceFrame(z.At(-Foot(z.a) * .45f, 0), z.front); var R = Foot(z.a) * .7f;
            z.mk.Prism(rockD, z.c - up * .3f, up, R, .44f, 24); z.mk.Prism(runeM, z.c + up * .15f, up, R - .2f, .02f, 24); z.mk.Prism(grassTop, z.c + up * .16f, up, R - .5f, .02f, 24);
            var sg = f.o; z.mk.Box(rock, sg + up * 1.4f, new Vector3(1.4f, 2.8f, .5f), f.r); BoxCol(z.root, sg + up * 1.4f, new Vector3(1.4f, 2.8f, .5f), f.r); ArtistBits.Label(z.labels, z.a.name, sg + up * 2.3f + f.Fwd * .26f, f.Fwd, .17f, Rune, TextAnchor.MiddleCenter); for (var k = 0; k < 3; k++) z.mk.Box(runeM, sg + up * (.6f + k * .4f) + f.Fwd * .26f, new Vector3(.8f - k * .2f, .05f, .02f), f.r);
            Floater(z.ctx, z.c + up * (13f + z.a.name.Length % 5), 2.2f, new System.Random(z.a.name.Length), false, 0f);
            return f;
        }

        /// A stone lion on a plinth, looking out.
        void Lion(Plot z, Vector3 g, Vector3 face)
        {
            var up = Vector3.up; var rot = Quaternion.LookRotation(face); var stone = Paint(new Color(.6f, .56f, .62f)); var rng = new System.Random((int)(g.x * 10f));
            z.mk.Box(rock, g + up * .5f, new Vector3(1.2f, 1f, 2f), rot); BoxCol(z.root, g + up * 1.2f, new Vector3(1.2f, 2.4f, 2f), rot);
            z.mk.Box(stone, g + up * 1.4f - face * .2f, new Vector3(.8f, .7f, 1.5f), rot); foreach (var e in new[] { -.28f, .28f }) foreach (var q in new[] { -.7f, .5f }) z.mk.Box(stone, g + up * 1.12f + Vector3.Cross(up, face) * e + face * q, new Vector3(.2f, .3f, .24f), rot);
            z.mk.Blob(stone, g + up * 2f + face * .6f, new Vector3(.5f, .5f, .4f), rng, .2f); z.mk.Box(stone, g + up * 1.9f + face * .95f, new Vector3(.34f, .3f, .3f), rot); z.mk.Box(stone, g + up * 1.5f - face * 1f, new Vector3(.08f, .5f, .08f), rot * Quaternion.Euler(-40f, 0, 0));
            foreach (var e in new[] { -.1f, .1f }) z.mk.Box(runeM, g + up * 2.04f + face * 1.1f + Vector3.Cross(up, face) * e, new Vector3(.05f, .04f, .02f), rot);
        }

        // ---------------------------------------------------------------- Seven Lions
        void Lions(Plot z)
        {
            var up = Vector3.up; var rng = new System.Random(77); var fr = z.front; var sd = z.side; var rot = z.Rot;
            z.mk.Prism(rockD, z.c - up * .3f, up, Foot(z.a) * .9f, .44f, 30); z.mk.Prism(grassTop, z.c + up * .14f, up, Foot(z.a) * .86f, .02f, 30);
            var creation = Song(z, "Creation"); var nep = Song(z, "Nepenthe"); var fall = Song(z, "The Fall"); var days = Song(z, "Days to Come"); life.fallSong = fall; life.daysSong = days;
            var pc = z.At(-2f, 0); z.mk.Prism(rock, pc - up * .2f, up, 4.4f, .7f, 20); z.mk.Prism(waterM, pc + up * .52f, up, 4f, .02f, 20); BoxCol(z.root, pc + up * .2f, new Vector3(8.6f, .8f, 8.6f), rot);
            for (var k = 0; k < 7; k++) { var a = -90f + k * 30f; var d = Quaternion.Euler(0, a + 180f, 0) * fr; Lion(z, pc + d * 7.6f, -d); }
            // "Creation": a great crystal floating over the pool
            var cr = new MeshKit(); cr.Prism(SongGlow(creation, Pink, 1.4f), Vector3.zero, up, 1f, 2.4f, 6, .05f); cr.Prism(SongGlow(creation, Pink, 1.4f), Vector3.zero, -up, 1f, 1.6f, 6, .05f); var cg = cr.BuildSingle("Floating crystal", z.root, false); cg.transform.position = pc + up * 5f; life.spinners.Add((cg.transform, pc + up * 5f)); halos.Add(pc + up * 5.6f, 4f, Pink, .3f);
            // "The Fall": a little floating rock pouring a waterfall into the pool
            var fa = pc + sd * 2.6f + up * 11f; var fm = new MeshKit(); fm.Prism(rock, Vector3.zero, -up, 1.6f, 2.4f, 7, .05f); fm.Prism(grassTop, Vector3.zero, up, 1.62f, .2f, 7); fm.Box(SongGlow(fall, Water, 1f), new Vector3(0, -5.5f, 0), new Vector3(.8f, 10.5f, .12f), Quaternion.identity); var fg = fm.BuildSingle("The Fall", z.root, false); fg.transform.position = fa;
            // "Nepenthe": a glowing flower; "Days to Come": a rising sun
            var nf = z.At(4f, -8f); z.mk.Prism(Paint(new Color(.3f, .5f, .3f)), nf, up, .06f, 1.6f, 5); for (var k = 0; k < 6; k++) { var pd = Quaternion.Euler(0, k * 60f, 0) * fr; z.mk.Tri(SongGlow(nep, new Color(.8f, .4f, 1f), 1.2f), nf + up * 1.6f, nf + up * 2f + pd * .8f + Vector3.Cross(up, pd) * .3f, nf + up * 2f + pd * .8f - Vector3.Cross(up, pd) * .3f, nf); } ArtistBits.PostCollider(z.root, nf, 1.8f, .4f);
            var ds = z.At(-Foot(z.a) * .85f, 0); z.mk.Prism(SongGlow(days, Gold, 1.4f), ds + up * 3f, fr, 2.4f, .1f, 26); z.mk.Box(rock, ds + up * 1f, new Vector3(6f, 2f, .6f), rot); BoxCol(z.root, ds + up * 1f, new Vector3(6f, 2f, .6f), rot); halos.Add(ds + up * 3f + fr * .5f, 5f, Gold, .25f);
            Folk(z.At(6f, 6f), -fr, Resident.Pose.Stand, new[] { "Seven lions. I count them every night. Still seven.", "When the drop comes the crystal spins faster. You'll see.", "The waterfall comes from that little island up there. It never runs dry." }, Local());
            z.area.glowLight = ArtistBits.PointLight(z.root, pc + up * 6f, Pink, 28f, 1.8f);
            Shot(z.ctx, "the seven lions", z.At(Foot(z.a) * 1.1f, 5f) + up * 2.6f, pc + up * 3f);
        }

        // ---------------------------------------------------------------- Mr FijiWiji
        void Fiji(Plot z)
        {
            var up = Vector3.up; var rng = new System.Random(88); var fr = z.front; var sd = z.side; var rot = z.Rot;
            z.mk.Prism(rockD, z.c - up * .3f, up, Foot(z.a) * .85f, .44f, 26); z.mk.Prism(Paint(new Color(.2f, .2f, .36f)), z.c + up * .14f, up, Foot(z.a) * .8f, .02f, 26);
            var ins = Song(z, "Insomnia"); var ment = Song(z, "The Mentalist"); var bye = Song(z, "Time to Say Goodbye"); var cyn = Song(z, "Cynical"); var gina = Song(z, "Gina"); life.insomniaSong = ins; life.byeSong = bye;
            // "Insomnia": a bed on a rock under the stars
            var bd = z.At(-3f, -2f); z.mk.Blob(rock, bd + up * .4f, new Vector3(2f, .8f, 3f), rng, .1f); z.mk.Box(Paint(new Color(.3f, .3f, .56f)), bd + up * 1.1f, new Vector3(1.6f, .3f, 2.4f), rot); z.mk.Box(white, bd + up * 1.32f - fr * .9f, new Vector3(1.2f, .16f, .5f), rot); z.mk.Box(SongGlow(ins, new Color(.5f, .6f, 1f), .8f), bd + up * 1.28f + fr * .3f, new Vector3(1.62f, .1f, 1.6f), rot); BoxCol(z.root, bd + up * .6f, new Vector3(2f, 1.2f, 3f), rot); Interact.Seat(tlife.transform, bd + up * 1.3f, fr, "Lie down and look at the stars").seatHeight = 1.2f;
            for (var k = 0; k < 9; k++) z.mk.Blob(GlowM(new Color(.9f, .9f, 1f), 1.6f), bd + up * (5f + (k % 3) * 1.4f) + Quaternion.Euler(0, k * 40f, 0) * fr * (1f + k * .3f), Vector3.one * .1f, rng, 0f);
            // "The Mentalist": a crystal ball on a pedestal
            var cb = z.At(2f, -5f); z.mk.Prism(rock, cb, up, .3f, 1f, 8, .7f); z.mk.Blob(SongGlow(ment, new Color(.6f, .9f, 1f), 1.2f), cb + up * 1.3f, Vector3.one * .34f, rng, 0f); ArtistBits.PostCollider(z.root, cb, 1.6f, .4f);
            // "Time to Say Goodbye": paper lanterns rising from a stand
            var ls = z.At(2f, 4f); z.mk.Box(trunkM, ls + up * .4f, new Vector3(1.4f, .8f, .8f), rot); BoxCol(z.root, ls + up * .4f, new Vector3(1.4f, .8f, .8f), rot); for (var k = 0; k < 5; k++) { var lm = new MeshKit(); lm.Prism(k == 0 ? SongGlow(bye, new Color(1f, .7f, .4f), 1.6f) : lampM, Vector3.zero, up, .22f, .4f, 6, .8f); var lg = lm.BuildSingle("Paper lantern", z.root, false); var home = ls + up * 1f + sd * (k - 2) * .3f; lg.transform.position = home; life.lanterns.Add((lg.transform, home, k * 1.3f)); }
            // "Cynical": a mask on a post; "Gina": a bunch of flowers
            var mp = z.At(-2f, 5f); z.mk.Prism(iron, mp, up, .04f, 1.6f, 5); z.mk.Blob(SongGlow(cyn, new Color(.9f, .9f, 1f), .8f), mp + up * 1.8f + fr * .05f, new Vector3(.3f, .36f, .08f), rng, 0f); foreach (var e in new[] { -.1f, .1f }) z.mk.Box(dark, mp + up * 1.86f + fr * .12f + sd * e, new Vector3(.08f, .04f, .02f), rot); ArtistBits.PostCollider(z.root, mp, 1.9f, .2f);
            var gf = z.At(5f, 0f); for (var k = 0; k < 7; k++) { var d = Quaternion.Euler(0, k * 51f, 0) * fr * .3f; z.mk.Prism(Paint(new Color(.3f, .5f, .3f)), gf + d, (up + d).normalized, .02f, 1f, 4); z.mk.Blob(k == 0 ? SongGlow(gina, Pink, 1.2f) : GlowM(k % 2 == 0 ? Pink : Gold, .7f), gf + d * 1.6f + up * 1f, Vector3.one * .14f, rng, 0f); } ArtistBits.PostCollider(z.root, gf, 1.1f, .4f);
            Folk(z.At(5f, 5f), -fr, Resident.Pose.Stand, new[] { "Can't sleep? Nobody here can. It's too beautiful.", "Light a lantern and let it go. They come back. Eventually.", "The crystal ball says you'll be here a while." }, Local());
            z.area.glowLight = ArtistBits.PointLight(z.root, z.c + up * 5f, new Color(.7f, .7f, 1f), 24f, 1.6f);
        }

        // ---------------------------------------------------------------- everyone else
        void Others(Plot z, PlaceFrame f)
        {
            var up = Vector3.up; var rot = z.Rot; var rng = new System.Random(z.a.name.Length * 7 + z.a.name[0]); var c = z.At(1f, -1.2f) + up * .16f; var fr = z.front; var sd = z.side;
            void Post(Vector3 at, float h, float r = .04f) { z.mk.Prism(iron, at - up * .2f, up, r, h + .2f, 5); ArtistBits.PostCollider(z.root, at, h, r + .03f); }
            void Plinth(Vector3 at, float w, float h) { z.mk.Box(rock, at + up * (h / 2f), new Vector3(w, h, w), rot); BoxCol(z.root, at + up * (h / 2f), new Vector3(w, h, w), rot); }
            string Any() => SongIds.TryGetValue(z.a.name, out var l) && l.Length > 0 ? Song(z, l[0].title) : null;
            switch (z.a.name)
            {
                case "Adventure Club": { var s = Any(); life.retroSong = s; for (var k = 0; k < 7; k++) { var h = .8f + (k * 37 % 5) * .4f; var bp = c + sd * (k - 3) * .5f - fr * (k % 2) * .4f; z.mk.Box(Paint(new Color(.16f, .14f, .26f)), bp + up * h / 2f, new Vector3(.4f, h, .4f), rot); for (var j = 0; j < (int)(h / .3f); j++) z.mk.Box(k == 3 && j == 0 ? SongGlow(s, new Color(1f, .3f, .8f), 1.4f) : GlowM(j % 2 == 0 ? new Color(1f, .3f, .8f) : new Color(.3f, .9f, 1f), 1.2f), bp + up * (.2f + j * .3f) + fr * .21f, new Vector3(.2f, .08f, .02f), rot); } BoxCol(z.root, c + up * 1f, new Vector3(3.6f, 2f, 1f), rot); break; }
                case "Dabin": { var s = Any(); life.bloomSong = s; z.mk.Prism(Paint(new Color(.3f, .5f, .3f)), c, up, .12f, 2.4f, 6); for (var k = 0; k < 8; k++) { var pd = Quaternion.Euler(0, k * 45f, 0) * fr; z.mk.Tri(k == 0 ? SongGlow(s, Pink, 1.4f) : GlowM(Pink, .8f), c + up * 2.4f, c + up * 2.9f + pd * 1.4f + Vector3.Cross(up, pd) * .5f, c + up * 2.9f + pd * 1.4f - Vector3.Cross(up, pd) * .5f, c); } z.mk.Blob(lampM, c + up * 2.5f, Vector3.one * .3f, rng, 0f); ArtistBits.PostCollider(z.root, c, 2.4f, .4f); break; }
                case "Direct": { var s = Any(); Crystals(z.root, z.mk, c, 1.3f, crystalM, rng); z.mk.Prism(SongGlow(s, Rune, 1.8f), c + up * .2f, up, .3f, 2.6f, 6, .05f); break; }
                case "Diveo": { var s = Any(); z.mk.Prism(trunkM, c, up, .3f, 2.6f, 7, .7f); for (var k = 0; k < 6; k++) z.mk.Blob(k == 0 ? SongGlow(s, Gold, 1f) : GlowM(Gold, .6f), c + up * (3f + (k % 2) * .6f) + Quaternion.Euler(0, k * 60f, 0) * fr * (k == 0 ? 0f : 1.3f), new Vector3(1.2f, .8f, 1.2f), rng, .25f); ArtistBits.PostCollider(z.root, c, 2.6f, .4f); break; }
                case "Foxes": { var s = Any(); Plinth(c, 1.2f, .6f); var fx = Paint(new Color(.9f, .45f, .2f)); var fb = c + up * .6f; z.mk.Box(fx, fb + up * .4f, new Vector3(.4f, .4f, .9f), rot); z.mk.Box(fx, fb + up * .8f + fr * .45f, new Vector3(.36f, .34f, .36f), rot); foreach (var e in new[] { -.12f, .12f }) z.mk.Cone(fx, fb + up * .97f + fr * .45f + sd * e, .08f, .2f, 4); z.mk.Box(white, fb + up * .74f + fr * .66f, new Vector3(.14f, .14f, .1f), rot); z.mk.Box(SongGlow(s, new Color(1f, .6f, .3f), 1f), fb + up * .5f - fr * .6f, new Vector3(.18f, .18f, .6f), rot * Quaternion.Euler(30f, 0, 0)); break; }
                case "Noisestorm": { var s = Any(); life.crabSong = s; for (var k = 0; k < 6; k++) { var cm = new MeshKit(); var shell = k == 0 ? SongGlow(s, new Color(1f, .4f, .2f), 1.2f) : Paint(new Color(.95f, .4f, .2f)); cm.Blob(shell, new Vector3(0, .2f, 0), new Vector3(.3f, .14f, .24f), rng, 0f); foreach (var e in new[] { -1f, 1f }) { cm.Box(shell, new Vector3(e * .36f, .3f, .14f), new Vector3(.14f, .1f, .16f), Quaternion.identity); for (var j = 0; j < 3; j++) cm.Box(shell, new Vector3(e * .3f, .1f, -.1f + j * .1f), new Vector3(.2f, .03f, .03f), Quaternion.Euler(0, 0, e * 30f)); } cm.Box(dark, new Vector3(-.06f, .36f, .2f), Vector3.one * .04f, Quaternion.identity); cm.Box(dark, new Vector3(.06f, .36f, .2f), Vector3.one * .04f, Quaternion.identity); var cg = cm.BuildSingle("Crab", z.root, false); var home = c + Quaternion.Euler(0, k * 60f, 0) * fr * 1.4f; cg.transform.SetPositionAndRotation(home, Quaternion.LookRotation(home - c)); life.crabs.Add((cg.transform, home)); } break; }
                case "Pixel Terror": { var s = Any(); var cb = c + up * 1.4f; z.mk.Box(dark, cb, Vector3.one * 1.6f, rot * Quaternion.Euler(0, 45f, 0)); for (var k = 0; k < 14; k++) { var face = Quaternion.Euler(0, 45f + (k % 4) * 90f, 0) * fr; z.mk.Box(k == 0 ? SongGlow(s, new Color(1f, .2f, .3f), 1.6f) : GlowM(k % 3 == 0 ? Pink : Rune, 1.2f), cb + face * .81f + up * ((k * 7 % 5) * .3f - .6f) + Vector3.Cross(up, face) * ((k * 3 % 5) * .3f - .6f), new Vector3(.2f, .2f, .02f), Quaternion.LookRotation(face)); } BoxCol(z.root, cb, Vector3.one * 1.6f, rot * Quaternion.Euler(0, 45f, 0)); break; }
                case "Pure 100%": { var s = Any(); z.mk.Prism(rock, c - up * .1f, up, 1.6f, .4f, 16); z.mk.Prism(SongGlow(s, Water, 1f), c + up * .31f, up, 1.4f, .02f, 16); BoxCol(z.root, c + up * .15f, new Vector3(3.2f, .4f, 3.2f), rot); life.mists.Add(c + up * .4f); break; }
                case "Rogue": { var dr = Song(z, "Dreams"); var nf = Song(z, "Nightfall"); life.nightSong = nf; var mc = c + up * 2.2f; for (var k = 0; k <= 12; k++) { var a = Mathf.PI * (k / 12f) * 1.4f - .2f; z.mk.Box(k == 6 ? SongGlow(nf, new Color(.8f, .86f, 1f), 1.4f) : GlowM(new Color(.8f, .86f, 1f), .9f), mc + sd * Mathf.Cos(a) * 1.4f + up * Mathf.Sin(a) * 1.4f, new Vector3(.3f, .3f + Mathf.Sin(k / 12f * Mathf.PI) * .2f, .2f), rot * Quaternion.Euler(0, 0, a * Mathf.Rad2Deg)); } z.mk.Blob(SongGlow(dr, Gold, 1.6f), mc + sd * .4f + up * .2f, Vector3.one * .14f, rng, 0f); Post(c - sd * 1.4f, 2f, .05f); break; }
                case "Skrux": { var s = Any(); z.mk.Prism(rock, c, up, .3f, .8f, 8, 2.4f); BoxCol(z.root, c + up * .4f, new Vector3(1.2f, .8f, 1.2f), rot); z.mk.Blob(SongGlow(s, Rune, 1.8f), c + up * 1.1f, Vector3.one * .3f, rng, 0f); break; }
                case "Sound Remedy": { var s = Any(); Plinth(c, .8f, .7f); var bt = c + up * .7f; z.mk.Prism(GlowM(new Color(.6f, .9f, 1f), .3f), bt, up, .34f, .6f, 10, .6f); z.mk.Prism(SongGlow(s, new Color(.4f, 1f, .6f), 1.4f), bt + up * .02f, up, .3f, .36f, 10, .8f); z.mk.Prism(GlowM(new Color(.6f, .9f, 1f), .3f), bt + up * .6f, up, .1f, .4f, 8); z.mk.Prism(trunkM, bt + up * 1f, up, .12f, .14f, 8); break; }
                case "Vexare": { var rs = Song(z, "Rattlesnakes"); var cm = Song(z, "The Clockmaker"); var cf = new PlaceFrame(c - fr * .4f, fr); z.mk.Prism(rock, cf.P(0, 2f, 0), fr, 1.4f, .3f, 24); z.mk.Prism(SongGlow(cm, Gold, 1f), cf.P(0, 2f, .31f), fr, 1.2f, .02f, 24); for (var k = 0; k < 12; k++) z.mk.Box(dark, cf.P(0, 2f, .34f) + Quaternion.AngleAxis(k * 30f, fr) * up * 1f, new Vector3(.06f, .2f, .02f), Quaternion.LookRotation(fr, Quaternion.AngleAxis(k * 30f, fr) * up)); z.mk.Box(dark, cf.P(.2f, 2.2f, .35f), new Vector3(.06f, .6f, .02f), cf.r * Quaternion.Euler(0, 0, -40f)); z.mk.Box(dark, cf.P(-.15f, 2.4f, .35f), new Vector3(.05f, .9f, .02f), cf.r * Quaternion.Euler(0, 0, 20f)); z.mk.Box(rock, cf.P(0, .4f, 0), new Vector3(.6f, .8f, .6f), cf.r); BoxCol(z.root, cf.P(0, 1.7f, 0), new Vector3(2.8f, 3.4f, .4f), cf.r); var sn = c + sd * 2f; for (var k = 0; k < 14; k++) { var a = k * .6f; z.mk.Blob(k == 13 ? SongGlow(rs, new Color(.6f, 1f, .4f), 1.4f) : Paint(new Color(.5f, .6f, .3f)), sn + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * (.6f - k * .03f) + up * (.12f + k * .05f), Vector3.one * .13f, rng, 0f); } ArtistBits.PostCollider(z.root, sn, .8f, .6f); break; }
                case "Xilent": { var s = Any(); var rc = c + up * 1.8f; for (var r = 0; r < 3; r++) { var axis = Quaternion.Euler(r * 50f, r * 60f, 0) * up; for (var k = 0; k < 18; k++) z.mk.Box(r == 0 && k == 0 ? SongGlow(s, Rune, 1.6f) : GlowM(r == 1 ? Pink : Rune, .9f), rc + Quaternion.AngleAxis(k * 20f, axis) * Vector3.Cross(axis, Vector3.forward).normalized * (1.2f - r * .2f), Vector3.one * .12f, Quaternion.identity); } Post(c, 1f, .06f); ArtistBits.PostCollider(z.root, c, 3f, 1.2f); break; }
                default: { var s = Any(); Post(c, 1.4f); z.mk.Blob(SongGlow(s, Pink, 1.4f), c + up * 1.5f, Vector3.one * .16f, rng, 0f); break; }
            }
        }
    }
}
