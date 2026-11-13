using System.Collections.Generic;
using UnityEngine;
using static EverythingWorld.World.PlaceTools;

namespace EverythingWorld.World
{
    /// The Concert Isles' composers. Each has a garden room walled in clipped hedge, open toward their lane: a marble bust
    /// on a plinth at the back with their name cut into it, a candle lantern, and one thing from their music; covers on a
    /// dark board with a gold frame.
    /// - Vivaldi: a tree in blossom. Dvořák: a great globe. Debussy: a round pool holding the moon. Grieg: a sunrise and a
    ///   flute. Satie: a lone chair under a lamp. Liszt: a bell on a frame. Schubert: a stand of candles. Chopin: a piano
    ///   and a moon lantern. Handel: a choir of little stone singers. Holst: a red planet. Stravinsky: standing stones round
    ///   a fire. Pachelbel: three rings of stone turning through each other. Bach: a cello on its stand and a row of organ
    ///   pipes. The William Tell Overture: an apple on a post with an arrow through it. Beethoven: a piano under a moon and
    ///   four great drums. Max Mathews: a bicycle built for two and an old computer of blinking lights. Max Richter: a
    ///   window frame full of daylight. Penguin Cafe Orchestra: a harmonium and a penguin. Philip Glass: a spiral of glass
    ///   panes. Mozart: a violin on a stand and a tear of light.
    public partial class ConcertKit
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
                Add("Ludwig van Beethoven", "Piano Sonata No. 14", "@the moon terrace"); Add("Claude Debussy", "Clair de Lune", "Claude Debussy"); Add("George Frideric Handel", "Hallelujah", "George Frideric Handel");
                Add("Gustav Holst", "The Planets", "Gustav Holst"); Add("Johann Sebastian Bach", "Toccata", "@the concert hall"); Add("Edvard Grieg", "Morning Mood", "Edvard Grieg"); Add("Antonio Vivaldi", "The Four Seasons", "Antonio Vivaldi");
                Add("London Symphony Orchestra", "William Tell", "London Symphony Orchestra"); Add("Igor Stravinsky", "The Rite", "Igor Stravinsky"); Add("Wolfgang Amadeus Mozart", "Mozart: Requiem", "Wolfgang Amadeus Mozart"); Add("Ludwig van Beethoven", "Symphony No. 5", "Ludwig van Beethoven");
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
            if (marble == null) MakeMaterials();
            var root = new GameObject("Artist: " + a.name).transform; root.SetParent(ctx.Root, false);
            var front = facing.TryGetValue(a.name, out var f) ? f : new Vector3(center.x - a.x, 0, center.z - a.z).normalized;
            if (front.sqrMagnitude < .01f) front = Vector3.forward;
            var labels = new GameObject("Labels").transform; labels.SetParent(root, false);
            var area = root.gameObject.AddComponent<ArtistArea>();
            area.artistName = a.name; area.stationId = a.stationId; area.radius = a.r; area.glowColor = Candle;
            var z = new Plot { ctx = ctx, root = root, labels = labels, mk = new MeshKit(), solid = new MeshKit(), pictures = new MeshKit(), a = a, c = ctx.OnGround(a.x, a.z), front = front, side = Vector3.Cross(Vector3.up, front), area = area };
            var reach = Foot(a); var up = Vector3.up;
            Piece(z, Room(z));
            SongLamps(z);
            Board(z, z.At(reach * .6f, -reach * .45f - 1.5f), front);
            ArtistBits.Plaque(z.mk, z.solid, labels, a, z.At(reach * .6f + 1.5f, reach * .45f + 1.5f), front, iron, Paint(new Color(.1f, .12f, .14f)), Gold, 1.6f);
            Lamp(ctx, z.mk, z.At(reach * .5f, reach * .7f), false);
            if (z.area.glowLight == null) z.area.glowLight = ArtistBits.PointLight(root, z.c + up * 4f + front * 3f, Candle, 14f, 1.3f);
            area.glowRenderers = new Renderer[0];
            z.mk.Build("Area", root, false);
            z.solid.BuildCollider("Walls", root);
            z.pictures.Build("Covers", root, false);
            noGrass.Add(new Vector3(z.c.x, reach * 1f, z.c.z));
            var mark = new GameObject("Shot: " + a.name + " up close").transform; mark.SetParent(ctx.Root, false);
            var cam = z.At(reach * .85f, reach * .3f) + up * 2f; var look = z.At(-reach * .3f, 0) + up * 1.8f - cam; mark.SetPositionAndRotation(cam, Quaternion.LookRotation(look.normalized));
        }

        /// The covers on a dark board with a gold frame.
        void Board(Plot z, Vector3 ground, Vector3 dir)
        {
            dir = Vector3.ProjectOnPlane(dir, Vector3.up).normalized; var rot = Quaternion.LookRotation(dir);
            var n = Mathf.Max(1, Mathf.Min(7, z.a.songList?.Length ?? 1)); const float size = .9f; var w = n * size * 1.25f + .6f;
            var c = ground + Vector3.up * 1.7f;
            z.mk.Box(goldM, c - Vector3.up * .5f - dir * .08f, new Vector3(w + .5f, size + 2f, .16f), rot); z.mk.Box(Paint(new Color(.1f, .14f, .14f)), c - Vector3.up * .5f - dir * .02f, new Vector3(w + .3f, size + 1.8f, .2f), rot); z.solid.Box(iron, c - Vector3.up * .5f, new Vector3(w + .5f, size + 2f, .3f), rot);
            z.mk.Box(Paint(new Color(.9f, .88f, .82f)), c - Vector3.up * .1f + dir * .09f, new Vector3(w, size + .9f, .04f), rot);
            ArtistBits.CoverWall(z.pictures, z.labels, z.ctx, z.a, c + dir * .12f + Vector3.up * .08f, dir, n, size, n, true, Ink);
        }

        /// A candle on a little marble stand for each song without its own piece.
        void SongLamps(Plot z)
        {
            if (!SongIds.TryGetValue(z.a.name, out var list)) return;
            var up = Vector3.up; var k = 0; var seen = new HashSet<string>();
            foreach (var (_, id) in list)
            {
                if (z.lit.Contains(id) || !seen.Add(id)) continue;
                var p = z.At(Foot(z.a) * .55f - (k % 2) * 1.3f, Foot(z.a) * .2f + (k / 2) * 1.3f);
                z.mk.Prism(marble, p, up, .14f, 1.1f, 8); z.mk.Prism(white, p + up * 1.1f, up, .05f, .2f, 6); z.mk.Blob(SongGlow(id, Candle, 1.8f), p + up * 1.4f, new Vector3(.05f, .09f, .05f), new System.Random(k), 0f); ArtistBits.PostCollider(z.root, p, 1.2f, .16f);
                k++;
            }
        }

        /// The garden room: a square of clipped hedge open at the front, a marble floor, a bust on a plinth at the back.
        PlaceFrame Room(Plot z)
        {
            var up = Vector3.up; var R = Foot(z.a) * .85f; var rot = z.Rot; var fr = z.front; var sd = z.side;
            z.mk.Box(gravel, z.c - up * .2f, new Vector3(R * 2f, .44f, R * 2f), rot); z.mk.Box(marble, z.c + up * .03f, new Vector3(R * 1.1f, .02f, R * 1.1f), rot);
            void Wall(Vector3 a, Vector3 b) { var mid = (a + b) / 2f; var len = (b - a).magnitude; var wr = Quaternion.LookRotation((b - a).normalized); z.mk.Box(hedgeM, mid + up * .9f, new Vector3(.9f, 1.9f, len), wr); z.solid.Box(hedgeM, mid + up * .9f, new Vector3(.9f, 1.9f, len), wr); }
            var c = z.c; Vector3 Cn(float f, float s) => c + fr * f * R + sd * s * R;
            Wall(Cn(-1, -1), Cn(-1, 1)); Wall(Cn(-1, -1), Cn(1, -1)); Wall(Cn(-1, 1), Cn(1, 1)); Wall(Cn(1, -1), Cn(1, -.3f)); Wall(Cn(1, .3f), Cn(1, 1));
            foreach (var s in new[] { -1f, 1f }) Topiary(z.ctx, z.mk, Cn(1, s * .3f) + fr * .9f + sd * s * .2f, .7f);
            var f = new PlaceFrame(z.At(-R * .7f, 0), fr); z.mk.Box(marble, f.P(0, .7f, 0), new Vector3(1f, 1.4f, 1f), f.r); z.solid.Box(marble, f.P(0, .7f, 0), new Vector3(1f, 1.4f, 1f), f.r); z.mk.Box(marbleD, f.P(0, 1.42f, 0), new Vector3(1.1f, .06f, 1.1f), f.r);
            var rng = new System.Random(z.a.name.Length); z.mk.Box(white, f.P(0, 1.7f, 0), new Vector3(.7f, .5f, .4f), f.r); z.mk.Prism(white, f.P(0, 1.95f, 0), up, .12f, .2f, 8); z.mk.Blob(white, f.P(0, 2.35f, 0), new Vector3(.22f, .28f, .24f), rng, .05f);
            ArtistBits.Label(z.labels, z.a.name, f.P(0, .9f, .52f), f.Fwd, .1f, Ink, TextAnchor.MiddleCenter);
            return f;
        }

        // ---------------------------------------------------------------- their pieces
        void Piece(Plot z, PlaceFrame f)
        {
            var up = Vector3.up; var rot = z.Rot; var rng = new System.Random(z.a.name.Length * 7 + z.a.name[0]); var c = z.At(1f, -1.3f) + up * .05f; var fr = z.front; var sd = z.side;
            void Post(Vector3 at, float h, float r = .04f) { z.mk.Prism(iron, at - up * .2f, up, r, h + .2f, 5); ArtistBits.PostCollider(z.root, at, h, r + .03f); }
            void Plinth(Vector3 at, float w, float h) { z.mk.Box(marble, at + up * (h / 2f), new Vector3(w, h, w), rot); BoxCol(z.root, at + up * (h / 2f), new Vector3(w, h, w), rot); }
            string Any() => SongIds.TryGetValue(z.a.name, out var l) && l.Length > 0 ? Song(z, l[0].title) : null;
            switch (z.a.name)
            {
                case "Antonio Vivaldi": { var s = Any(); life.springSong = s; z.mk.Prism(trunkM, c, (up + sd * .1f).normalized, .2f, 2.4f, 6, .7f); ArtistBits.PostCollider(z.root, c, 2.4f, .3f); for (var k = 0; k < 7; k++) z.mk.Blob(k == 0 ? SongGlow(s, new Color(1f, .8f, .9f), .9f) : Paint(new Color(1f, .8f, .9f)), c + up * 2.6f + Quaternion.Euler(0, k * 51f, 0) * fr * 1f + up * (k % 2) * .5f, new Vector3(.8f, .6f, .8f), rng, .2f); break; }
                case "Antonín Dvořák": { var s = Any(); Plinth(c, .8f, .9f); z.mk.Blob(SongGlow(s, new Color(.4f, .7f, 1f), .9f), c + up * 1.7f, Vector3.one * .7f, rng, 0f); z.mk.Prism(goldM, c + up * 1.7f - sd * .8f, sd, .03f, 1.6f, 5); for (var k = 0; k < 5; k++) z.mk.Blob(Paint(new Color(.3f, .6f, .3f)), c + up * 1.7f + Quaternion.Euler(k * 40f, k * 70f, 0) * fr * .66f, new Vector3(.24f, .2f, .1f), rng, .2f); break; }
                case "Claude Debussy": { var s = Any(); life.clairSong = s; z.mk.Prism(marble, c - up * .1f, up, 1.8f, .5f, 20); z.mk.Prism(waterM, c + up * .38f, up, 1.6f, .02f, 20); z.mk.Prism(SongGlow(s, Silver, 1.6f), c + up * .4f, up, .6f, .02f, 20); BoxCol(z.root, c + up * .2f, new Vector3(3.6f, .6f, 3.6f), rot); break; }
                case "Edvard Grieg": { var s = Any(); life.morningSong = s; Post(c - fr * .4f, 2f, .05f); z.mk.Prism(SongGlow(s, new Color(1f, .7f, .4f), 1.4f), c - fr * .4f + up * 2.6f, fr, .7f, .06f, 20); for (var k = 0; k < 8; k++) z.mk.Box(GlowM(new Color(1f, .8f, .5f), 1f), c - fr * .38f + up * 2.6f + Quaternion.AngleAxis(k * 22.5f - 78.75f, fr) * up * 1f, new Vector3(.06f, .4f, .02f), Quaternion.LookRotation(fr, Quaternion.AngleAxis(k * 22.5f - 78.75f, fr) * up)); Plinth(c + sd * 1.4f, .5f, .9f); z.mk.Prism(Paint(new Color(.8f, .8f, .84f)), c + sd * 1.4f + up * .95f - fr * .3f, fr, .02f, .6f, 6); break; }
                case "Erik Satie": { var s = Any(); var ch = c; z.mk.Box(woodM(), ch + up * .45f, new Vector3(.5f, .06f, .5f), rot); z.mk.Box(woodM(), ch + up * .8f - fr * .24f, new Vector3(.5f, .7f, .06f), rot); foreach (var e in new[] { -.2f, .2f }) foreach (var q in new[] { -.2f, .2f }) z.mk.Prism(woodM(), ch + sd * e + fr * q, up, .02f, .45f, 4); Interact.Seat(tlife.transform, ch + up * .5f, fr, "Sit very still").seatHeight = .48f; BoxCol(z.root, ch + up * .3f, new Vector3(.5f, .6f, .5f), rot); var lp = ch + sd * 1.2f; Post(lp, 2.4f); z.mk.Blob(SongGlow(s, Candle, 1.4f), lp + up * 2.5f, Vector3.one * .16f, rng, 0f); break; }
                case "Franz Liszt": { var s = Any(); foreach (var e in new[] { -1f, 1f }) Post(c + sd * e, 2.6f, .06f); z.mk.Prism(iron, c - sd * 1f + up * 2.6f, sd, .05f, 2f, 5); z.mk.Prism(SongGlow(s, Gold, 1f), c + up * 1.6f, up, .4f, .8f, 12, .5f); z.mk.Prism(goldM, c + up * 2.4f, up, .08f, .2f, 6); z.mk.Blob(goldM, c + up * 1.5f, Vector3.one * .08f, rng, 0f); break; }
                case "Franz Schubert": { var s = Any(); life.aveSong = s; z.mk.Box(iron, c + up * .5f, new Vector3(1.6f, 1f, .6f), rot); BoxCol(z.root, c + up * .5f, new Vector3(1.6f, 1f, .6f), rot); for (var k = 0; k < 15; k++) { var cp = c + up * (1.02f + (k / 5) * .12f) + sd * (-.6f + (k % 5) * .3f) - fr * ((k / 5) * .15f - .15f); z.mk.Prism(white, cp, up, .03f, .14f + (k % 3) * .04f, 5); z.mk.Blob(k == 7 ? SongGlow(s, Candle, 2f) : candleM, cp + up * (.22f + (k % 3) * .04f), new Vector3(.03f, .05f, .03f), rng, 0f); } halos.Add(c + up * 1.4f, 1.8f, Candle, .4f); break; }
                case "Frédéric Chopin": { var bal = Song(z, "Ballade"); var noc = Song(z, "Nocturne"); Piano(z.root, z.mk, c - fr * .4f, fr, "Play a nocturne"); var ml = c + sd * 2.4f; Post(ml, 2.6f); z.mk.Prism(SongGlow(noc, Silver, 1.4f), ml + up * 2.8f, fr, .4f, .06f, 16); z.mk.Blob(SongGlow(bal, Candle, 1.4f), c - fr * .4f + up * 1.2f - fr * .4f + sd * .5f, Vector3.one * .06f, rng, 0f); break; }
                case "George Frideric Handel": { var s = Any(); life.hallSong = s; for (var r = 0; r < 3; r++) { z.mk.Box(marbleD, c + up * (r * .3f + .15f) - fr * r * .6f, new Vector3(3.4f, .3f + r * .6f, .6f), rot); BoxCol(z.root, c + up * (r * .3f + .15f) - fr * r * .6f, new Vector3(3.4f, .3f + r * .6f, .6f), rot); for (var k = 0; k < 5; k++) { var sp = c + up * (r * .6f + .3f) - fr * r * .6f + sd * (k - 2) * .6f; z.mk.Prism(white, sp, up, .12f, .5f, 6, .7f); z.mk.Blob(r == 1 && k == 2 ? SongGlow(s, Gold, 1f) : white, sp + up * .62f, Vector3.one * .1f, rng, 0f); } } break; }
                case "Gustav Holst": { var s = Any(); life.marsSong = s; Plinth(c, .7f, 1f); z.mk.Blob(SongGlow(s, new Color(1f, .3f, .14f), 1.1f), c + up * 1.8f, Vector3.one * .7f, rng, .05f); z.mk.Prism(Paint(new Color(.6f, .5f, .4f)), c + up * 1.8f, (up + fr * .3f).normalized, 1.1f, .02f, 24); break; }
                case "Igor Stravinsky": { var s = Any(); life.riteSong = s; for (var k = 0; k < 7; k++) { var sp = c + Quaternion.Euler(0, k * 51.4f, 0) * fr * 2f; z.mk.Box(marbleD, sp + up * .9f, new Vector3(.5f, 1.8f + (k % 3) * .4f, .4f), Quaternion.LookRotation(sp - c)); ArtistBits.PostCollider(z.root, sp, 1.8f, .35f); } z.mk.Cone(SongGlow(s, new Color(1f, .5f, .14f), 2f), c, .4f, 1f, 6); life.fires.Add(c + up * .8f); ArtistBits.PointLight(z.root, c + up * 1.2f, new Color(1f, .6f, .3f), 8f, 1.4f); ArtistBits.PostCollider(z.root, c, .8f, .5f); break; }
                case "Johann Pachelbel": { var s = Any(); Plinth(c, .7f, .7f); for (var k = 0; k < 3; k++) { var axis = Quaternion.Euler(k * 60f, k * 30f, 0) * fr; for (var j = 0; j < 16; j++) { var p = c + up * 1.8f + Quaternion.AngleAxis(j * 22.5f, axis) * Vector3.Cross(axis, up).normalized * .7f; z.mk.Box(k == 0 && j == 0 ? SongGlow(s, Gold, 1.4f) : marble, p, Vector3.one * .12f, Quaternion.identity); } } break; }
                case "Johann Sebastian Bach": { var cello = Song(z, "Cello"); var toc = Song(z, "Toccata"); life.toccataSong = toc; var ce = c + sd * 1f; z.mk.Blob(Paint(new Color(.5f, .26f, .12f)), ce + up * .6f, new Vector3(.36f, .5f, .18f), rng, 0f); z.mk.Blob(Paint(new Color(.5f, .26f, .12f)), ce + up * 1.1f, new Vector3(.28f, .3f, .16f), rng, 0f); z.mk.Box(dark, ce + up * 1.7f, new Vector3(.06f, .9f, .04f), rot); z.mk.Box(SongGlow(cello, Candle, 1.2f), ce + up * .9f + fr * .17f, new Vector3(.12f, .6f, .02f), rot); ArtistBits.PostCollider(z.root, ce, 2f, .4f); for (var k = 0; k < 7; k++) { var pp = c - sd * (1f + k * .3f) - fr * .5f; z.mk.Prism(k == 3 ? SongGlow(toc, Gold, 1f) : goldM, pp, up, .1f, 1f + Mathf.Abs(k - 3) * -.15f + 1.4f, 8); } BoxCol(z.root, c - sd * 1.9f - fr * .5f + up * 1.2f, new Vector3(2.2f, 2.4f, .3f), rot); break; }
                case "London Symphony Orchestra": { var s = Any(); life.tellSong = s; Post(c, 1.6f, .06f); z.mk.Blob(Paint(new Color(.8f, .14f, .14f)), c + up * 1.8f, Vector3.one * .16f, rng, 0f); z.mk.Prism(Paint(new Color(.3f, .5f, .2f)), c + up * 1.94f, up, .01f, .08f, 4); z.mk.Prism(woodM(), c + up * 1.8f - sd * .6f, sd, .015f, 1.2f, 4); z.mk.Tri(SongGlow(s, new Color(1f, .9f, .6f), 1.4f), c + up * 1.8f - sd * .6f, c + up * 1.9f - sd * .85f, c + up * 1.7f - sd * .85f, c + fr); z.mk.Tri(SongGlow(s, new Color(1f, .9f, .6f), 1.4f), c + up * 1.8f - sd * .6f, c + up * 1.9f - sd * .85f, c + up * 1.7f - sd * .85f, c - fr); break; }
                case "Ludwig van Beethoven": { var moon = Song(z, "Piano Sonata"); var fifth = Song(z, "Symphony No. 5"); life.moonSong = moon; life.fifthSong = fifth; Piano(z.root, z.mk, c - fr * .6f + sd * .8f, fr, "Play the moonlight"); var ml = c + sd * 3f - fr * 1.6f; Post(ml, 3.2f); z.mk.Prism(SongGlow(moon, Silver, 1.6f), ml + up * 3.4f, fr, .6f, .06f, 20); for (var k = 0; k < 4; k++) { var dp = c - sd * (1.6f + (k % 2) * 1.1f) + fr * ((k / 2) * 1.2f - .2f); z.mk.Prism(Paint(new Color(.5f, .14f, .14f)), dp, up, .5f, .8f, 14); z.mk.Prism(k == 3 ? SongGlow(fifth, new Color(1f, .9f, .8f), 1f) : white, dp + up * .8f, up, .5f, .02f, 14); ArtistBits.PostCollider(z.root, dp, .8f, .5f); } break; }
                case "Max Mathews": { var s = Any(); life.daisySong = s; var bk = c + sd * .6f; foreach (var q in new[] { -1.1f, 1.1f }) { z.mk.Prism(dark, bk + up * .4f + fr * q - sd * .02f, sd, .4f, .04f, 14); } z.mk.Box(iron, bk + up * .75f, new Vector3(.05f, .05f, 2.2f), rot); foreach (var q in new[] { -.6f, .4f }) { z.mk.Box(iron, bk + up * .9f + fr * q, new Vector3(.05f, .4f, .05f), rot); z.mk.Box(dark, bk + up * 1.12f + fr * q, new Vector3(.2f, .05f, .3f), rot); } z.mk.Box(iron, bk + up * 1.1f + fr * 1.1f, new Vector3(.5f, .04f, .04f), rot); BoxCol(z.root, bk + up * .5f, new Vector3(.4f, 1f, 2.6f), rot); var cm = c - sd * 1.6f - fr * .6f; z.mk.Box(Paint(new Color(.6f, .6f, .56f)), cm + up * 1f, new Vector3(1.2f, 2f, .7f), rot); BoxCol(z.root, cm + up * 1f, new Vector3(1.2f, 2f, .7f), rot); for (var k = 0; k < 12; k++) z.mk.Box(k == 0 ? SongGlow(s, new Color(.4f, 1f, .5f), 1.6f) : GlowM(k % 2 == 0 ? new Color(1f, .3f, .2f) : new Color(.4f, 1f, .5f), 1.2f), cm + up * (1.3f + (k / 4) * .2f) + sd * (-.3f + (k % 4) * .2f) + fr * .36f, new Vector3(.06f, .06f, .02f), rot); for (var e = 0; e < 2; e++) z.mk.Prism(dark, cm + up * .7f + sd * (e * .5f - .25f) + fr * .36f, fr, .16f, .02f, 12); break; }
                case "Max Richter": { var s = Any(); var wf = new PlaceFrame(c - fr * .3f, fr); foreach (var e in new[] { -.9f, .9f }) { z.mk.Box(white, wf.P(e, 1.3f, 0), new Vector3(.14f, 2.6f, .14f), wf.r); ArtistBits.PostCollider(z.root, wf.P(e, 0, 0), 2.6f, .1f); } foreach (var h in new[] { .1f, 2.5f }) z.mk.Box(white, wf.P(0, h, 0), new Vector3(1.9f, .14f, .14f), wf.r); z.mk.Box(white, wf.P(0, 1.3f, 0), new Vector3(.06f, 2.4f, .06f), wf.r); z.mk.Box(white, wf.P(0, 1.3f, 0), new Vector3(1.7f, .06f, .06f), wf.r); z.mk.Box(SongGlow(s, new Color(1f, .96f, .86f), 1.4f), wf.P(0, 1.3f, -.04f), new Vector3(1.7f, 2.3f, .02f), wf.r); halos.Add(wf.P(0, 1.3f, .6f), 2.2f, new Color(1f, .96f, .86f), .3f); break; }
                case "Penguin Cafe Orchestra": { var s = Any(); var hm = c - fr * .2f; z.mk.Box(woodM(), hm + up * .6f, new Vector3(1.2f, 1.2f, .6f), rot); z.mk.Box(white, hm + up * 1.22f + fr * .15f, new Vector3(1f, .04f, .2f), rot); z.mk.Box(SongGlow(s, Candle, 1f), hm + up * 1f + fr * .31f, new Vector3(.8f, .3f, .02f), rot); BoxCol(z.root, hm + up * .6f, new Vector3(1.2f, 1.2f, .6f), rot); var pg = c + sd * 1.6f + fr * .6f; z.mk.Blob(dark, pg + up * .4f, new Vector3(.24f, .4f, .2f), rng, 0f); z.mk.Blob(white, pg + up * .38f + fr * .1f, new Vector3(.18f, .32f, .12f), rng, 0f); z.mk.Blob(dark, pg + up * .86f, Vector3.one * .14f, rng, 0f); z.mk.Cone(Paint(Gold), pg + up * .86f + fr * .12f, .04f, .1f, 4); ArtistBits.PostCollider(z.root, pg, .9f, .26f); break; }
                case "Philip Glass": { var s = Any(); var glass = GlowM(new Color(.7f, .9f, 1f), .5f); for (var k = 0; k < 10; k++) { var a = k * .6f; var gp = c + (fr * Mathf.Cos(a) + sd * Mathf.Sin(a)) * (.4f + k * .14f); z.mk.Box(k == 9 ? SongGlow(s, new Color(.7f, .9f, 1f), 1.2f) : glass, gp + up * (.6f + k * .12f), new Vector3(.04f, 1.2f + k * .1f, .8f), Quaternion.LookRotation(gp - c)); } ArtistBits.PostCollider(z.root, c, 2f, 1.8f); break; }
                case "Wolfgang Amadeus Mozart": { var nacht = Song(z, "Eine kleine"); var lac = Song(z, "Mozart: Requiem"); life.lacrimosaSong = lac; Post(c, 1.1f, .03f); z.mk.Blob(Paint(new Color(.6f, .3f, .14f)), c + up * 1.3f, new Vector3(.12f, .2f, .05f), rng, 0f); z.mk.Box(dark, c + up * 1.6f, new Vector3(.03f, .4f, .03f), rot); z.mk.Box(SongGlow(nacht, Candle, 1.2f), c + up * 1.3f + sd * .2f, new Vector3(.02f, .02f, .5f), rot * Quaternion.Euler(30f, 0, 0)); var tr = c + sd * 1.8f; Plinth(tr, .5f, 1f); z.mk.Blob(SongGlow(lac, new Color(.6f, .8f, 1f), 1.6f), tr + up * 1.3f, new Vector3(.14f, .2f, .14f), rng, 0f); z.mk.Cone(SongGlow(lac, new Color(.6f, .8f, 1f), 1.6f), tr + up * 1.4f, .13f, .3f, 8); break; }
                case "Ancient Chinese composer": { var tears = Song(z, "Imperial"); var snow = Song(z, "White Snow"); var pv = c - fr * .4f; var red = Paint(new Color(.7f, .14f, .12f)); foreach (var (x, q) in new[] { (-1.2f, -1.2f), (1.2f, -1.2f), (1.2f, 1.2f), (-1.2f, 1.2f) }) { z.mk.Prism(red, pv + sd * x + fr * q, up, .1f, 2.4f, 8); ArtistBits.PostCollider(z.root, pv + sd * x + fr * q, 2.4f, .14f); } for (var k = 0; k < 2; k++) z.mk.Prism(Paint(new Color(.14f, .2f, .2f)), pv + up * (2.4f + k * .6f), up, 2.2f - k * .7f, .45f, 4, .5f, 45f); z.mk.Box(woodM(), pv + up * .7f, new Vector3(1.4f, .1f, .4f), rot); for (var k = 0; k < 9; k++) z.mk.Box(GlowM(Silver, .6f), pv + up * .76f + sd * (-.56f + k * .14f), new Vector3(.01f, .01f, .34f), rot); BoxCol(z.root, pv + up * .4f, new Vector3(1.4f, .8f, .4f), rot); z.mk.Prism(SongGlow(tears, new Color(1f, .3f, .2f), 1.4f), pv + up * 2f, up, .2f, .36f, 8, .7f); var bt = c + sd * 2.8f; z.mk.Prism(trunkM, bt, (up + sd * .2f).normalized, .12f, 2f, 6); for (var k = 0; k < 6; k++) z.mk.Blob(k == 0 ? SongGlow(snow, new Color(1f, .94f, .96f), 1f) : Paint(new Color(1f, .9f, .94f)), bt + up * 2f + Quaternion.Euler(0, k * 60f, 0) * fr * .7f + up * (k % 2) * .3f, new Vector3(.45f, .32f, .45f), rng, .2f); ArtistBits.PostCollider(z.root, bt, 2f, .2f); break; }
                case "Ennio Morricone": { var s = Any(); for (var k = 0; k < 12; k++) { var gp = c + Quaternion.Euler(0, k * 30f, 0) * fr * 2.4f; var gr = Quaternion.LookRotation(gp - c); z.mk.Box(woodM(), gp + up * .6f, new Vector3(.1f, 1.2f, .08f), gr * Quaternion.Euler(0, 0, (k % 3 - 1) * 6f)); z.mk.Box(woodM(), gp + up * .9f, new Vector3(.5f, .08f, .08f), gr); ArtistBits.PostCollider(z.root, gp, 1.2f, .1f); } z.mk.Prism(SongGlow(s, Gold, 1.8f), c + up * .05f, up, .9f, .04f, 18); halos.Add(c + up * .6f, 2.4f, Gold, .4f); break; }
                case "Pyotr Ilyich Tchaikovsky": { var s = Any(); Post(c, 1.1f, .03f); z.mk.Blob(white, c + up * 1.2f, new Vector3(.14f, .2f, .1f), rng, 0f); z.mk.Prism(SongGlow(s, Silver, 1f), c + up * .95f, up, .6f, .06f, 18); ArtistBits.PostCollider(z.root, c, 1.3f, .6f); var sw = c + sd * 2f - fr * .6f; z.mk.Prism(marble, sw - up * .1f, up, 1.2f, .3f, 16); z.mk.Prism(waterM, sw + up * .21f, up, 1.05f, .02f, 16); z.mk.Blob(white, sw + up * .4f, new Vector3(.3f, .22f, .5f), rng, .05f); z.mk.Prism(white, sw + up * .5f + fr * .3f, (up + fr * .3f).normalized, .05f, .6f, 6); z.mk.Blob(white, sw + up * 1.1f + fr * .44f, new Vector3(.08f, .08f, .12f), rng, 0f); BoxCol(z.root, sw + up * .2f, new Vector3(2.4f, .5f, 2.4f), rot); break; }
                case "Sergei Rachmaninoff": { var s = Any(); Piano(z.root, z.mk, c - fr * .4f, fr, "Play the second concerto"); z.mk.Blob(SongGlow(s, Candle, 1.6f), c - fr * 1.2f + up * 1.3f + sd * .5f, Vector3.one * .07f, rng, 0f); var ml = c + sd * 2.6f; Post(ml, 2.6f); z.mk.Blob(candleM, ml + up * 2.7f, Vector3.one * .12f, rng, 0f); break; }
                default: { var s = Any(); Post(c, 1.4f); z.mk.Blob(SongGlow(s, Candle, 1.4f), c + up * 1.5f, Vector3.one * .14f, rng, 0f); break; }
            }
        }

        Material woodM() => Paint(new Color(.4f, .26f, .16f));
    }
}
