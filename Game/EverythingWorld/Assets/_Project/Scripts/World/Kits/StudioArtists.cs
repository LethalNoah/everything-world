using System.Collections.Generic;
using UnityEngine;
using static EverythingWorld.World.PlaceTools;

namespace EverythingWorld.World
{
    /// Studio Lot Island's artists. Every artist is a film set: a wooden stage floor, a painted flat braced with
    /// scaffolding behind it, a film light and a director's chair, and the piece of their film in the middle.
    /// - The Lion King: the savannah set. Sand, a rocky promontory to climb with the sun painted behind it, flat-topped
    ///   acacias, and a piece for every song: a green-lit gorge ("Be Prepared"), a starry stone ("Can You Feel the Love
    ///   Tonight"), a log full of glowing grubs ("Hakuna Matata"), a tower of animals ("I Just Can't Wait to Be King") and a
    ///   moon over the jungle ("The Lion Sleeps Tonight").
    /// - Daniele Zandara: the scoring stage: a big screen on scaffolding, rows of seats, a projector, and four film reels
    ///   lit in each song's colour.
    /// - The rest: the palace of the Bollywood sets with marigold garlands (Kishore Kumar, Arijit Singh, Pritam, A.R. Rahman),
    ///   coral and a giant clam, an ice stair, a hammock in the jungle, a swirl of coloured leaves, the small-world boat, the
    ///   golden sun, a green vision, a curly hill under the moon, a red biplane, a scythe, a red jumpsuit, the blue game
    ///   board, a dojo mat with its trophy, the skeleton fondue, the fuzzy singers, a pea pod, a campfire, a treasure chest,
    ///   a log in the swamp under a rainbow, and a living-room couch.
    public partial class StudioKit
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
                Add("The Lion King", "Can You Feel", "The Lion King"); Add("The Lion King", "Be Prepared", "The Lion King"); Add("Idina Menzel", "Let It Go", "Idina Menzel");
                Add("Alan Menken", "Under the Sea", "Alan Menken"); Add("Carmen Twillie", "Circle of Life", "Carmen Twillie"); Add("The Muppets", "Rainbow Connection", "The Muppets");
                Add("The Intermezzo Orchestra", "Pirates", "@the tank"); Add("The Disneyland Children's Chorus", "It's a Small World", "The Disneyland Children's Chorus"); Add("A.R. Rahman", "Jai Ho", "A.R. Rahman");
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
            switch (a.name)
            {
                case "The Lion King": LionKing(z); break;
                case "Daniele Zandara": Scoring(z); break;
                case "Phineas and Ferb": Phineas(z); break;
                default: Others(z, SetFloor(z)); break;
            }
            SongLamps(z);
            Board(z, z.At(reach * .6f, -reach * .45f - 1.5f), front);
            ArtistBits.Plaque(z.mk, z.solid, labels, a, z.At(reach * .6f + 1.5f, reach * .45f + 1.5f), front, woodM, Paint(Navy), Cream, 1.6f);
            Lamp(ctx, z.mk, z.At(reach * .5f, reach * .7f), false);
            if (z.area.glowLight == null) z.area.glowLight = ArtistBits.PointLight(root, z.c + up * 4f + front * 3f, new Color(1f, .9f, .74f), HasStation(a) ? 24f : 14f, 1.3f);
            area.glowRenderers = new Renderer[0];
            z.mk.Build("Area", root, false);
            z.solid.BuildCollider("Walls", root);
            z.pictures.Build("Covers", root, false);
            noGrass.Add(new Vector3(z.c.x, reach * .9f, z.c.z));
            var mark = new GameObject("Shot: " + a.name + " up close").transform; mark.SetParent(ctx.Root, false);
            var cam = z.At(reach * .85f, reach * .3f) + up * 2f; var look = z.At(-reach * .3f, 0) + up * 1.8f - cam; mark.SetPositionAndRotation(cam, Quaternion.LookRotation(look.normalized));
        }

        /// The covers on a cream card on a navy board with a gold rim, like a premiere poster.
        void Board(Plot z, Vector3 ground, Vector3 dir)
        {
            dir = Vector3.ProjectOnPlane(dir, Vector3.up).normalized; var rot = Quaternion.LookRotation(dir);
            var n = Mathf.Max(1, Mathf.Min(7, z.a.songList?.Length ?? 1)); const float size = .9f; var w = n * size * 1.25f + .6f;
            var c = ground + Vector3.up * 1.7f;
            z.mk.Box(Paint(Gold), c - Vector3.up * .5f - dir * .08f, new Vector3(w + .7f, size + 2.2f, .16f), rot); z.mk.Box(Paint(Navy), c - Vector3.up * .5f - dir * .02f, new Vector3(w + .5f, size + 2f, .2f), rot); z.solid.Box(woodM, c - Vector3.up * .5f, new Vector3(w + .7f, size + 2.2f, .3f), rot);
            z.mk.Box(Paint(Cream), c - Vector3.up * .1f + dir * .09f, new Vector3(w, size + .9f, .04f), rot);
            for (var k = 0; k < 6; k++) z.mk.Blob(lampM, c + Vector3.up * (size / 2f + .9f) + dir * .1f + Vector3.Cross(Vector3.up, dir) * (-w / 2f + k * w / 5f), Vector3.one * .06f, new System.Random(k), 0f);
            ArtistBits.CoverWall(z.pictures, z.labels, z.ctx, z.a, c + dir * .12f + Vector3.up * .08f, dir, n, size, n, true, Ink);
        }

        /// A small gold film light on a post for each song without its own piece.
        void SongLamps(Plot z)
        {
            if (!SongIds.TryGetValue(z.a.name, out var list)) return;
            var up = Vector3.up; var k = 0; var seen = new HashSet<string>();
            foreach (var (_, id) in list)
            {
                if (z.lit.Contains(id) || !seen.Add(id)) continue;
                var p = z.At(Foot(z.a) * .55f - (k % 2) * 1.3f, Foot(z.a) * .2f + (k / 2) * 1.3f);
                z.mk.Prism(iron, p - up * .1f, up, .035f, 1.3f, 5); z.mk.Prism(dark, p + up * 1.3f, z.front, .14f, .24f, 8); z.mk.Prism(SongGlow(id, Gold, 1.6f), p + up * 1.3f + z.front * .24f, z.front, .12f, .02f, 8); ArtistBits.PostCollider(z.root, p, 1.3f, .08f);
                k++;
            }
        }

        /// The set's floor and its painted flat: a wooden stage, a flat braced behind, a film light and a director's chair.
        PlaceFrame SetFloor(Plot z)
        {
            var up = Vector3.up; var hue = z.a.name.Length + z.a.name[0]; var f = new PlaceFrame(z.At(-Foot(z.a) * .4f, 0), z.front);
            var col = BackdropOf(z.a.name, hue);
            z.mk.Box(woodM, z.c + up * .05f, new Vector3(Foot(z.a) * 1.3f, .3f, Foot(z.a) * 1.1f), z.Rot); for (var k = -3; k <= 3; k++) z.mk.Box(Paint(new Color(.44f, .32f, .22f)), z.c + up * .21f + z.side * k * Foot(z.a) * .18f, new Vector3(.04f, .02f, Foot(z.a) * 1.1f), z.Rot);
            Flat(z.ctx, z.mk, z.solid, f, 7f, 4.4f, Paint(col));
            ArtistBits.Label(z.labels, z.a.name, f.P(0, 3.9f, .12f), f.Fwd, .3f, Cream, TextAnchor.MiddleCenter);
            FilmLight(z.ctx, z.mk, z.At(Foot(z.a) * .3f, -Foot(z.a) * .55f), z.c - z.At(Foot(z.a) * .3f, -Foot(z.a) * .55f), false);
            DirectorsChair(z.mk, z.At(Foot(z.a) * .45f, Foot(z.a) * .1f) + up * .2f, -z.front, null);
            return f;
        }

        static Color BackdropOf(string name, int hue)
        {
            switch (name)
            {
                case "Kishore Kumar": case "Arijit Singh": case "Pritam": case "A.R. Rahman": return new Color(.96f, .86f, .7f);
                case "Alan Menken": return new Color(.2f, .5f, .7f);
                case "Idina Menzel": return new Color(.5f, .7f, .9f);
                case "Phil Harris": case "Tori Kelly": return new Color(.36f, .54f, .3f);
                case "Catherine O'Hara": return new Color(.2f, .2f, .3f);
                case "Geek Music": return new Color(.1f, .14f, .5f);
                case "The Simpsons": return new Color(.9f, .7f, .8f);
                case "The Muppets": return new Color(.3f, .4f, .3f);
            }
            return new[] { new Color(.86f, .7f, .5f), new Color(.5f, .6f, .8f), new Color(.8f, .5f, .4f), new Color(.6f, .7f, .5f) }[hue % 4];
        }

        // ---------------------------------------------------------------- The Lion King: the savannah set
        void LionKing(Plot z)
        {
            var up = Vector3.up; var rng = new System.Random(1994); var fr = z.front; var sd = z.side; var rot = z.Rot;
            z.mk.Prism(sand, z.c - up * .25f, up, Foot(z.a) * .9f, .32f, 28);
            // the painted sunset behind: a great flat with an orange sky and a huge sun
            var bf = new PlaceFrame(z.At(-Foot(z.a) * .85f, 0), fr); Flat(z.ctx, z.mk, z.solid, bf, 22f, 9f, Paint(new Color(1f, .6f, .3f)));
            z.mk.Box(Paint(new Color(1f, .8f, .4f)), bf.P(0, 2f, .12f), new Vector3(22f, 4f, .02f), bf.r); z.mk.Prism(GlowM(new Color(1f, .76f, .3f), 1.2f), bf.P(-3f, 4.4f, .13f), fr, 3.2f, .02f, 28);
            for (var k = 0; k < 5; k++) z.mk.Box(Paint(new Color(.3f, .2f, .16f)), bf.P(4f + k * 1.4f, .6f + (k % 2) * .3f, .14f), new Vector3(.9f, .5f, .02f), bf.r);
            // the rocky promontory: steps of rock you can climb, its point jutting out over the sand
            var pr = new PlaceFrame(z.At(-Foot(z.a) * .4f, Foot(z.a) * .35f), (fr - sd * .6f).normalized); var rock = Paint(new Color(.56f, .48f, .42f)); var rockD = Paint(new Color(.44f, .38f, .34f));
            for (var k = 0; k < 6; k++) { var h = .5f + k * .6f; var p = pr.P(0, h / 2f, -6f + k * 1.6f); z.mk.Box(k % 2 == 0 ? rock : rockD, p, new Vector3(3.6f - k * .2f, h, 2f), pr.r * Quaternion.Euler(0, k * 4f, 0)); z.solid.Box(rock, p, new Vector3(3.6f - k * .2f, h, 2f), pr.r * Quaternion.Euler(0, k * 4f, 0)); }
            var tip = pr.P(0, 3.5f, 4.6f); z.mk.Box(rock, tip - up * .3f, new Vector3(2.2f, .6f, 4f), pr.r * Quaternion.Euler(-6f, 0, 0)); z.solid.Box(rock, tip - up * .3f, new Vector3(2.2f, .6f, 4f), pr.r * Quaternion.Euler(-6f, 0, 0)); z.mk.Box(rockD, pr.P(0, 1.5f, 4.6f), new Vector3(1.6f, 3f, 2.6f), pr.r); z.solid.Box(rockD, pr.P(0, 1.5f, 4.6f), new Vector3(1.6f, 3f, 2.6f), pr.r);
            for (var k = 0; k < 6; k++) { var rb = pr.P((k % 2 == 0 ? -1f : 1f) * 2f, .6f, -5f + k * 1.8f); z.mk.Blob(k % 2 == 0 ? rock : rockD, rb, new Vector3(1f, .9f, 1f), rng, .3f); ArtistBits.PostCollider(z.root, rb - up * .6f, 1.5f, .9f); }
            var love = Song(z, "Can You Feel"); var prep = Song(z, "Be Prepared"); var hak = Song(z, "Hakuna"); var king = Song(z, "I Just Can't"); var sleeps = Song(z, "The Lion Sleeps"); life.loveSong = love; life.prepSong = prep;
            z.mk.Prism(SongGlow(love, new Color(1f, .9f, .6f), 1f), tip + up * .02f + pr.Fwd * 1.4f, up, .5f, .04f, 16); ArtistBits.Label(z.labels, "★", tip + up * .2f + pr.Fwd * 1.4f, up, .5f, Gold, TextAnchor.MiddleCenter);
            // acacias, flat-topped
            void Acacia(Vector3 g, float s) { var trunk = g; var top = g + up * 4f * s + sd * .6f; z.mk.Prism(trunkM, trunk, (top - trunk).normalized, .2f * s, (top - trunk).magnitude, 6); z.mk.Prism(trunkM, top - up * .6f, (sd * -1f + up).normalized, .1f * s, 1.6f * s, 5); for (var k = 0; k < 3; k++) z.mk.Blob(leafM, top + up * .2f + Quaternion.Euler(0, k * 120f, 0) * fr * 1.2f * s, new Vector3(2.2f * s, .4f, 1.6f * s), rng, .2f); ArtistBits.PostCollider(z.root, g, 4f * s, .3f * s); }
            Acacia(z.At(-Foot(z.a) * .2f, -Foot(z.a) * .6f), 1.2f); Acacia(z.At(Foot(z.a) * .2f, Foot(z.a) * .7f), .9f); Acacia(z.At(-Foot(z.a) * .65f, -Foot(z.a) * .2f), 1f);
            // "Be Prepared": a gorge of dark rocks with green light rising out of it
            var bp = z.At(-Foot(z.a) * .1f, -Foot(z.a) * .3f); for (var k = 0; k < 7; k++) { var d = Quaternion.Euler(0, k * 51f, 0) * fr; z.mk.Cone(Paint(new Color(.26f, .24f, .24f)), bp + d * 1.6f, .5f, 1.4f + (k % 3) * .6f, 5); } z.mk.Prism(SongGlow(prep, new Color(.5f, 1f, .3f), 1.4f), bp - up * .1f, up, 1.1f, .06f, 12); halos.Add(bp + up * 1f, 2.4f, new Color(.5f, 1f, .3f), .3f); ArtistBits.PostCollider(z.root, bp, 1.6f, 1.8f);
            // "Hakuna Matata": a hollow log with glowing grubs
            var lg = z.At(Foot(z.a) * .2f, -Foot(z.a) * .2f) + up * .5f; z.mk.Prism(trunkM, lg - sd * 1.5f, sd, .5f, 3f, 10); z.mk.Prism(dark, lg - sd * 1.52f, sd, .38f, .02f, 10); for (var k = 0; k < 5; k++) z.mk.Blob(SongGlow(k == 0 ? hak : null, new Color(1f, .7f, .3f), 1f), lg + up * .5f + sd * (-1.2f + k * .6f), new Vector3(.12f, .06f, .06f), rng, 0f); BoxCol(z.root, lg, new Vector3(1f, 1f, 3f), Quaternion.LookRotation(sd)); Interact.Seat(tlife.transform, lg + up * .5f + sd * .6f, fr, "Sit on the log");
            // "I Just Can't Wait to Be King": a tower of painted animal blocks
            var tw = z.At(Foot(z.a) * .05f, Foot(z.a) * .05f); var tc = new[] { new Color(.6f, .6f, .62f), new Color(.9f, .7f, .3f), new Color(.86f, .5f, .6f), new Color(.4f, .7f, .4f), new Color(.9f, .3f, .2f) }; for (var k = 0; k < 5; k++) z.mk.Box(k == 4 ? SongGlow(king, tc[k], .8f) : Paint(tc[k]), tw + up * (.5f + k * .9f), new Vector3(1.6f - k * .2f, .8f, 1.4f - k * .2f), rot * Quaternion.Euler(0, k * 12f, 0)); BoxCol(z.root, tw + up * 2.2f, new Vector3(1.6f, 4.5f, 1.4f), rot);
            // "The Lion Sleeps Tonight": a moon on a pole above a little jungle
            var mn = z.At(-Foot(z.a) * .5f, -Foot(z.a) * .6f); z.mk.Prism(iron, mn, up, .05f, 5f, 5); z.mk.Prism(SongGlow(sleeps, new Color(.9f, .92f, 1f), 1.2f), mn + up * 5.2f - fr * .1f, fr, .8f, .1f, 18); for (var k = 0; k < 4; k++) { var jb = mn + Quaternion.Euler(0, k * 90f, 0) * fr * 1f; z.mk.Blob(leafM, jb + up * .6f, new Vector3(.8f, .7f, .8f), rng, .3f); ArtistBits.PostCollider(z.root, jb, 1.3f, .7f); } ArtistBits.PostCollider(z.root, mn, 5f, .1f);
            // animal cut-outs on the sand: a giraffe and an elephant, painted flat
            var gf = new PlaceFrame(z.At(-Foot(z.a) * .6f, Foot(z.a) * .05f), fr); var yel = Paint(new Color(.9f, .7f, .3f)); z.mk.Box(yel, gf.P(0, 1.4f, 0), new Vector3(1.6f, .8f, .1f), gf.r); z.mk.Box(yel, gf.P(.7f, 2.8f, 0), new Vector3(.3f, 2.2f, .1f), gf.r * Quaternion.Euler(0, 0, -10f)); z.mk.Box(yel, gf.P(1f, 3.9f, 0), new Vector3(.6f, .3f, .1f), gf.r); foreach (var e in new[] { -.6f, .6f }) z.mk.Box(yel, gf.P(e, .5f, 0), new Vector3(.14f, 1f, .1f), gf.r); z.mk.Box(scaffold, gf.P(0, 1f, -.4f), new Vector3(.06f, 2f, .06f), gf.r * Quaternion.Euler(30f, 0, 0)); BoxCol(z.root, gf.P(0, 1.4f, 0), new Vector3(2f, 2.8f, .4f), gf.r);
            Folk(z.At(Foot(z.a) * .3f, Foot(z.a) * .2f), -fr, Resident.Pose.Stand, new[] { "Climb the rock. Everybody does the pose. Everybody.", "The sun's painted. Took three weeks. Worth it.", "When the love song plays, they turn the lights down and the stars come out." }, Local());
            z.area.glowLight = ArtistBits.PointLight(z.root, tip + up * 3f, new Color(1f, .8f, .5f), 30f, 1.8f);
        }

        // ---------------------------------------------------------------- Daniele Zandara: the scoring stage
        void Scoring(Plot z)
        {
            var up = Vector3.up; var fr = z.front; var sd = z.side; var rot = z.Rot; var rng = new System.Random(7);
            z.mk.Prism(Paint(new Color(.3f, .16f, .18f)), z.c - up * .25f, up, Foot(z.a) * .85f, .33f, 24);
            // the screen
            var sf = new PlaceFrame(z.At(-Foot(z.a) * .6f, 0), fr); Flat(z.ctx, z.mk, z.solid, sf, 12f, 7f, dark); z.mk.Box(GlowM(new Color(.7f, .8f, 1f), .9f), sf.P(0, 3.8f, .12f), new Vector3(10.6f, 5.4f, .02f), sf.r); halos.Add(sf.P(0, 3.8f, 2f), 6f, new Color(.7f, .8f, 1f), .2f);
            ArtistBits.Label(z.labels, "DANIELE ZANDARA", sf.P(0, 5.6f, .14f), sf.Fwd, .4f, Ink, TextAnchor.MiddleCenter); for (var k = 0; k < 2; k++) z.mk.Box(Paint(new Color(.5f, .1f, .14f)), sf.P(k == 0 ? -5.9f : 5.9f, 3.6f, .2f), new Vector3(1.2f, 7.2f, .2f), sf.r);
            // rows of red seats facing it
            for (var r = 0; r < 3; r++) for (var k = -3; k <= 3; k++) { var sp = z.At(-Foot(z.a) * .05f + r * 1.6f, k * 1f); var st = Quaternion.LookRotation(-fr); z.mk.Box(Paint(new Color(.6f, .12f, .14f)), sp + up * .45f, new Vector3(.8f, .1f, .7f), st); z.mk.Box(Paint(new Color(.6f, .12f, .14f)), sp + up * .85f + fr * .32f, new Vector3(.8f, .8f, .1f), st); if (k % 2 == 0 && r == 1) Interact.Seat(tlife.transform, sp + up * .5f, -fr, "Sit and watch"); }
            BoxCol(z.root, z.At(-Foot(z.a) * .05f + 1.6f, 0) + up * .5f, new Vector3(7f, 1f, 4.4f), rot);
            // the projector on a stand behind, its beam glowing
            var pj = z.At(Foot(z.a) * .4f, 0); z.mk.Box(iron, pj + up * .7f, new Vector3(.6f, 1.4f, .6f), rot); z.mk.Box(dark, pj + up * 1.6f, new Vector3(.6f, .5f, 1f), rot); foreach (var e in new[] { -.3f, .3f }) z.mk.Prism(dark, pj + up * 2.1f + fr * e, sd, .3f, .06f, 14); z.mk.Prism(lampM, pj + up * 1.6f - fr * .52f, -fr, .14f, .02f, 10); BoxCol(z.root, pj + up * 1f, new Vector3(.8f, 2f, 1.2f), rot);
            // four film reels on stands, one per song
            var titles = new[] { ("Copper Green", new Color(.5f, .9f, .6f)), ("Moms are Tough", new Color(1f, .7f, .5f)), ("Red & Blue", new Color(.9f, .3f, .5f)), ("Reincarnated", new Color(.5f, .6f, 1f)) };
            for (var k = 0; k < 4; k++) { var (t, col) = titles[k]; var s = Song(z, t); var rp = z.At(-Foot(z.a) * .2f + k * 1.6f, (k % 2 == 0 ? -1f : 1f) * Foot(z.a) * .62f); var face = (sd * (k % 2 == 0 ? 1f : -1f)); z.mk.Prism(iron, rp, up, .05f, 1.6f, 5); z.mk.Prism(SongGlow(s, col, 1.2f), rp + up * 2.3f - face * .05f, face, .7f, .1f, 18); z.mk.Prism(dark, rp + up * 2.3f - face * .06f, face, .2f, .12f, 10); for (var j = 0; j < 5; j++) z.mk.Box(dark, rp + up * 2.3f + face * .06f + Quaternion.AngleAxis(j * 72f, face) * up * .42f, new Vector3(.16f, .16f, .02f), Quaternion.LookRotation(face)); ArtistBits.PostCollider(z.root, rp, 1.6f, .1f); }
            Folk(z.At(Foot(z.a) * .3f, Foot(z.a) * .3f), -fr, Resident.Pose.Stand, new[] { "Sit anywhere. The score's the star here.", "Every reel's a different film. Same composer.", "Lights down. Here it comes." }, Local());
            z.area.glowLight = ArtistBits.PointLight(z.root, sf.P(0, 4f, 4f), new Color(.7f, .8f, 1f), 22f, 1.4f);
        }

        // ---------------------------------------------------------------- Phineas and Ferb: the summer backyard (moved here from the Isle of Memes)
        void Phineas(Plot z)
        {
            var up = Vector3.up; var rot = z.Rot; var rng = new System.Random(2007); var fr = z.front; var sd = z.side;
            var gg = Song(z, "Gitchee"); var sm = Song(z, "S.I.M.P"); var ar = Song(z, "Ain't Got Rhythm"); var li = Song(z, "I'm Lindana"); var sn = Song(z, "You Snuck"); var dq = Song(z, "Disco Miniature"); var sb = Song(z, "Summer Belongs"); var ku = Song(z, "Kick It Up"); 
            // a wooden fence round the back of the yard
            for (var k = 0; k < 18; k++) { var a = (k - 8.5f) * 9f * Mathf.Deg2Rad; var p = z.c - fr * Mathf.Cos(a) * 13f + sd * Mathf.Sin(a) * 13f; p = z.ctx.OnGround(p.x, p.z); z.mk.Box(Paint(new Color(.8f, .66f, .5f)), p + up * .9f, new Vector3(.24f, 1.8f, .06f), Quaternion.LookRotation((z.c - p).normalized)); } BoxCol(z.root, z.c - fr * 12.8f + up * .9f, new Vector3(24f, 1.8f, .3f), rot);
            // the big backyard tree with a bench all round it
            var tr = z.At(-6f, -5f); z.mk.Prism(Paint(new Color(.5f, .36f, .26f)), tr, up, .6f, 4f, 9, .7f); ArtistBits.PostCollider(z.root, tr, 3f, .7f); for (var k = 0; k < 6; k++) z.mk.Blob(leafM, tr + up * (5f + (k % 2) * 1f) + Quaternion.Euler(0, k * 60f, 0) * fr * (k == 0 ? 0f : 2.4f), new Vector3(2.6f, 1.8f, 2.6f), rng, .2f); z.mk.Prism(Paint(new Color(.66f, .5f, .36f)), tr + up * .4f, up, 1.5f, .1f, 14); for (var k = 0; k < 4; k++) { var d = Quaternion.Euler(0, k * 90f, 0) * fr; Interact.Seat(tlife.transform, tr + d * 1.3f + up * .5f, d, "Sit under the tree").seatHeight = .5f; }
            Folk(tr + fr * 1.3f + up * .48f, fr, Resident.Pose.Sit, new[] { "I know what we're gonna do today.", "There's a hundred and four days of summer vacation. We're using all of them.", "Mom's not going to believe this. She never sees it." }, null, null, .5f);
            // half a rollercoaster loop going up over the fence
            var lc = z.At(-9f, 4f); var lm = Paint(new Color(.9f, .3f, .3f)); for (var k = 0; k < 14; k++) { var a0 = k * 15f * Mathf.Deg2Rad; var a1 = (k + 1) * 15f * Mathf.Deg2Rad; foreach (var e in new[] { -.6f, .6f }) { var p0 = lc + sd * e - fr * Mathf.Sin(a0) * 5f + up * (5f - Mathf.Cos(a0) * 5f); var p1 = lc + sd * e - fr * Mathf.Sin(a1) * 5f + up * (5f - Mathf.Cos(a1) * 5f); z.mk.Prism(lm, p0, (p1 - p0).normalized, .08f, Vector3.Distance(p0, p1) + .02f, 5); } if (k % 2 == 0) { var mp = lc - fr * Mathf.Sin(a0) * 5f + up * (5f - Mathf.Cos(a0) * 5f); z.mk.Prism(Paint(new Color(.6f, .62f, .7f)), z.ctx.OnGround(mp.x, mp.z), up, .07f, mp.y - z.ctx.Ground(mp.x, mp.z), 4); } } ArtistBits.PostCollider(z.root, lc, 3f, .6f);
            // "Gitchee Gitchee Goo": a little band stage with a spotlight
            var bs = z.At(4f, -9f) + up * .05f; z.mk.Box(Paint(new Color(.5f, .36f, .26f)), bs + up * .3f, new Vector3(4f, .6f, 2.4f), rot); z.solid.Box(woodM, bs + up * .2f, new Vector3(4f, .8f, 2.4f), rot); z.mk.Prism(iron, bs + up * .6f - fr * .2f, up, .02f, 1.5f, 4); z.mk.Blob(white, bs + up * 2.16f - fr * .2f, new Vector3(.05f, .08f, .05f), rng, 0f); z.mk.Prism(iron, bs - fr * 1.6f, up, .05f, 3.8f, 5); z.mk.Prism(SongGlow(gg, Gold, 1.4f), bs - fr * 1.4f + up * 3.6f, (fr - up * .5f).normalized, .2f, .3f, 8, 1.6f); ArtistBits.PostCollider(z.root, bs - fr * 1.6f, 3.6f, .07f);
            string[] gitchee = { "You grab the mic and sing a song with nonsense words in it. Everyone knows them anyway.", "Gitchee gitchee... you trail off. The whole backyard finishes it.", "The spotlight finds you. You were born for this. Briefly." }; var gn = 0;
            Spot("The band stage", bs + up * 1.4f, "Sing on the stage", new Vector3(3.6f, 1.4f, 2f)).onUse = _ => { life.played = Time.time; Say1(bs + up * 2.8f, gitchee[gn++ % gitchee.Length]); };
            // a squirrel statue; a metronome; a pink spotlight and mic
            var sq = z.At(7f, -3f) + up * .05f; z.mk.Box(Paint(Cream), sq + up * .3f, new Vector3(.7f, .6f, .7f), rot); BoxCol(z.root, sq + up * .3f, new Vector3(.7f, .6f, .7f), rot); var sqm = SongGlow(sm, new Color(.7f, .5f, .36f), .5f); z.mk.Blob(sqm, sq + up * .9f, new Vector3(.2f, .28f, .2f), rng, 0f); z.mk.Blob(sqm, sq + up * 1.26f + fr * .06f, Vector3.one * .12f, rng, 0f); z.mk.Prism(sqm, sq + up * .7f - fr * .2f, (up - fr * .4f).normalized, .14f, .7f, 6, .5f);
            var mt = z.At(7f, 3f) + up * .05f; z.mk.Box(Paint(Cream), mt + up * .3f, new Vector3(.7f, .6f, .7f), rot); BoxCol(z.root, mt + up * .3f, new Vector3(.7f, .6f, .7f), rot); z.mk.Prism(Paint(new Color(.5f, .36f, .26f)), mt + up * .6f, up, .3f, .8f, 4, .3f); z.mk.Prism(SongGlow(ar, Gold, 1.2f), mt + up * .7f + fr * .2f, (up + sd * .3f).normalized, .015f, .7f, 3); 
            var pl = z.At(3f, 9f) + up * .05f; z.mk.Prism(SongGlow(li, Pink, 1f), pl + up * .01f, up, 1f, .02f, 16); z.mk.Prism(iron, pl, up, .02f, 1.5f, 4); z.mk.Blob(Paint(Pink), pl + up * 1.56f, new Vector3(.05f, .08f, .05f), rng, 0f);
            // a heart balloon; a glittering crown on a pedestal; a sun over a globe; a big dial
            var hb = z.At(-1f, 10f); z.mk.Prism(white, hb, up, .006f, 2.6f, 3); Heart(z.mk, SongGlow(sn, new Color(1f, .4f, .5f), 1f), hb + up * 3f, fr, .6f);
            var cr = z.At(-2f, -10f) + up * .05f; z.mk.Box(Paint(Cream), cr + up * .5f, new Vector3(.6f, 1f, .6f), rot); BoxCol(z.root, cr + up * .5f, new Vector3(.6f, 1f, .6f), rot); var cm = SongGlow(dq, Gold, 1.4f); z.mk.Prism(cm, cr + up * 1f, up, .24f, .14f, 10); for (var k = 0; k < 5; k++) z.mk.Cone(cm, cr + up * 1.14f + Quaternion.Euler(0, k * 72f, 0) * fr * .2f, .05f, .16f, 4); z.mk.Blob(Paint(new Color(.9f, .9f, 1f)), cr + up * 1.6f, Vector3.one * .14f, rng, .05f);
            var gl = z.At(1f, -12f) + up * .05f; z.mk.Prism(iron, gl, up, .04f, 1f, 5); z.mk.Blob(GlowM(Sky, 1.2f), gl + up * 1.3f, Vector3.one * .3f, rng, 0f); z.mk.Blob(SongGlow(sb, new Color(1f, .8f, .3f), 1.6f), gl + up * 2.4f, Vector3.one * .3f, rng, 0f); ArtistBits.PostCollider(z.root, gl, 1f, .06f);
            var dl = z.At(-11f, -1f); z.mk.Prism(iron, dl - up * .2f, up, .05f, 1.4f, 5); z.mk.Prism(Paint(new Color(.3f, .3f, .4f)), dl + up * 1.6f - fr * .05f, fr, .5f, .1f, 16); z.mk.Box(SongGlow(ku, new Color(1f, .4f, .2f), 1.2f), dl + up * 1.6f + fr * .06f + (Quaternion.AngleAxis(-60f, fr) * up) * .25f, new Vector3(.06f, .46f, .02f), Quaternion.AngleAxis(-60f, fr) * rot); for (var k = 0; k < 11; k++) z.mk.Box(white, dl + up * 1.6f + fr * .06f + Quaternion.AngleAxis(-120f + k * 24f, fr) * up * .42f, new Vector3(.03f, .08f, .01f), Quaternion.AngleAxis(-120f + k * 24f, fr) * rot); ArtistBits.PostCollider(z.root, dl, 1.4f, .07f);
            z.area.glowLight = ArtistBits.PointLight(z.root, z.c + up * 5f, new Color(1f, .94f, .8f), 26f, 1.4f);
            Shot(z.ctx, "the summer backyard", z.At(16f, 4f) + up * 2.4f, z.c + up * 2f - fr * 4f);
        }
        void Heart(MeshKit mk, Material m, Vector3 c, Vector3 face, float s)
        {
            face.y = 0; face.Normalize(); var side = Vector3.Cross(Vector3.up, face);
            Vector3 H(float t) => c + side * 16f * Mathf.Pow(Mathf.Sin(t), 3f) * s / 16f + Vector3.up * (13f * Mathf.Cos(t) - 5f * Mathf.Cos(2f * t) - 2f * Mathf.Cos(3f * t) - Mathf.Cos(4f * t)) * s / 16f;
            for (var k = 0; k < 16; k++) { var t0 = k * Mathf.PI * 2f / 16f; var t1 = (k + 1) * Mathf.PI * 2f / 16f; mk.Tri(m, c, H(t0), H(t1), c - face); mk.Tri(m, c, H(t0), H(t1), c + face); }
        }

        // ---------------------------------------------------------------- everyone else: their film's piece
        void Others(Plot z, PlaceFrame f)
        {
            var up = Vector3.up; var rot = z.Rot; var rng = new System.Random(z.a.name.Length * 7 + z.a.name[0]); var c = z.At(1.2f, -1.2f) + up * .2f; var fr = z.front; var sd = z.side;
            void Post(Vector3 at, float h, float r = .04f) { z.mk.Prism(iron, at - up * .2f, up, r, h + .2f, 5); ArtistBits.PostCollider(z.root, at, h, r + .03f); }
            void Plinth(Vector3 at, float w, float h) { z.mk.Box(Paint(Cream), at + up * (h / 2f), new Vector3(w, h, w), rot); BoxCol(z.root, at + up * (h / 2f), new Vector3(w, h, w), rot); }
            string Any() => SongIds.TryGetValue(z.a.name, out var l) && l.Length > 0 ? Song(z, l[0].title) : null;
            void Garlands() { for (var g = 0; g < 2; g++) for (var k = 0; k <= 10; k++) { var x = -3f + k * .6f; var y = 3.4f - g * .7f - Mathf.Sin(k / 10f * Mathf.PI) * .5f; z.mk.Blob(Paint(k % 2 == 0 ? new Color(1f, .6f, .1f) : new Color(1f, .8f, .2f)), f.P(x, y, .3f), Vector3.one * .12f, rng, .2f); } }
            void Palace() { var ivory = Paint(new Color(.98f, .92f, .8f)); for (var k = -1; k <= 1; k++) { z.mk.Box(dark, f.P(k * 2.2f, 1.3f, .13f), new Vector3(1.2f, 2.2f, .02f), f.r); z.mk.Prism(dark, f.P(k * 2.2f, 2.4f, .13f), f.Fwd, .6f, .02f, 12); } z.mk.Prism(ivory, f.P(0, 4.4f, -.4f), up, 1.2f, .4f, 14); z.mk.Blob(ivory, f.P(0, 5.1f, -.4f), new Vector3(1.2f, 1.1f, 1.2f), rng, 0f); z.mk.Cone(Paint(Gold), f.P(0, 6.1f, -.4f), .14f, .6f, 6); foreach (var e in new[] { -3.2f, 3.2f }) { z.mk.Prism(ivory, f.P(e, 0, -.2f), up, .4f, 5f, 8); z.mk.Blob(ivory, f.P(e, 5.3f, -.2f), new Vector3(.5f, .5f, .5f), rng, 0f); } Garlands(); }
            switch (z.a.name)
            {
                case "Kishore Kumar": { Palace(); var s1 = Song(z, "Mere Sapnon"); var s2 = Song(z, "Wada Karo"); var jp = c; z.mk.Box(Paint(new Color(.3f, .5f, .3f)), jp + up * .6f, new Vector3(1.4f, .7f, 2.6f), rot); z.mk.Box(SongGlow(s1, new Color(.6f, .9f, 1f), .6f), jp + up * 1.1f + fr * .6f, new Vector3(1.2f, .4f, .04f), rot * Quaternion.Euler(-30f, 0, 0)); foreach (var e in new[] { -.6f, .6f }) foreach (var q in new[] { -.9f, .9f }) z.mk.Prism(dark, jp + up * .3f + sd * e + fr * q, sd * Mathf.Sign(e), .3f, .14f, 10); BoxCol(z.root, jp + up * .7f, new Vector3(1.6f, 1.4f, 2.8f), rot); var mp = c + sd * 2.4f; Post(mp, 1.6f); z.mk.Prism(SongGlow(s2, Gold, 1f), mp + up * 1.8f, up, .12f, .3f, 8); break; }
                case "Arijit Singh": { Palace(); var s = Any(); var mic = c; Post(mic, 1.5f, .02f); z.mk.Blob(SongGlow(s, new Color(1f, .5f, .6f), 1f), mic + up * 1.6f, new Vector3(.08f, .12f, .08f), rng, 0f); for (var k = 0; k < 8; k++) z.mk.Blob(GlowM(new Color(1f, .3f, .4f), .8f), mic + up * (2f + (k % 3) * .4f) + Quaternion.Euler(0, k * 45f, 0) * fr * 1.2f, new Vector3(.16f, .14f, .06f), rng, 0f); break; }
                case "Pritam": { Palace(); var s = Any(); for (var k = 0; k < 7; k++) { var dp = c + sd * (-1.8f + k * .6f); z.mk.Prism(Paint(new Color(.7f, .4f, .2f)), dp, up, .12f, .1f, 8, 1.4f); z.mk.Blob(SongGlow(k == 3 ? s : null, Saffron, 1.6f), dp + up * .2f, new Vector3(.05f, .1f, .05f), rng, 0f); } halos.Add(c + up * .4f, 2f, Saffron, .3f); break; }
                case "A.R. Rahman": { Palace(); var s = Song(z, "Jai Ho"); life.jaiSong = s; var dr = c; z.mk.Prism(Paint(new Color(.7f, .2f, .2f)), dr, up, .6f, .8f, 14); z.mk.Prism(SongGlow(s, Saffron, 1f), dr + up * .8f, up, .6f, .02f, 14); BoxCol(z.root, dr + up * .4f, new Vector3(1.2f, .8f, 1.2f), rot); for (var k = 0; k < 3; k++) z.mk.Box(Paint(new Color(1f, .5f, .1f)), dr + up * (2.4f + k * .5f) + sd * (k - 1) * .8f, new Vector3(.9f, .3f, .02f), rot * Quaternion.Euler(0, 0, k * 20f - 20f)); break; }
                case "Alan Menken": { var s = Song(z, "Under the Sea"); life.seaSong = s; for (var k = 0; k < 6; k++) { var cp = c + Quaternion.Euler(0, k * 60f, 0) * fr * 1.8f; var cc = new[] { new Color(1f, .4f, .5f), new Color(1f, .6f, .3f), new Color(.7f, .4f, 1f) }[k % 3]; for (var j = 0; j < 3; j++) z.mk.Prism(Paint(cc), cp, (up + Quaternion.Euler(0, j * 120f, 0) * fr * .5f).normalized, .1f, 1.2f + j * .3f, 5, .5f); ArtistBits.PostCollider(z.root, cp, 1.6f, .6f); } var cl = c; z.mk.Blob(Paint(new Color(.8f, .6f, .9f)), cl + up * .3f, new Vector3(1f, .3f, .8f), rng, 0f); z.mk.Blob(Paint(new Color(.8f, .6f, .9f)), cl + up * .9f - fr * .4f, new Vector3(1f, .3f, .8f), rng, 0f); z.mk.Blob(SongGlow(s, new Color(.9f, .95f, 1f), 1.4f), cl + up * .6f, Vector3.one * .2f, rng, 0f); BoxCol(z.root, cl + up * .5f, new Vector3(2f, 1f, 1.6f), rot); break; }
                case "Idina Menzel": { var s = Song(z, "Let It Go"); life.letItSong = s; var ice = SongGlow(s, new Color(.6f, .86f, 1f), .9f); for (var k = 0; k < 8; k++) { var st = c - fr * (k * .6f) + sd * Mathf.Sin(k * .5f) * 1.2f + up * (k * .35f); z.mk.Box(ice, st, new Vector3(1.4f, .14f, .7f), rot * Quaternion.Euler(0, k * 8f, 0)); z.solid.Box(ice, st - up * .1f, new Vector3(1.4f, .3f, .7f), rot * Quaternion.Euler(0, k * 8f, 0)); } for (var k = 0; k < 6; k++) z.mk.Cone(GlowM(new Color(.7f, .9f, 1f), .6f), c + Quaternion.Euler(0, k * 60f, 0) * fr * 2.6f, .3f, 1.4f + (k % 3) * .6f, 6); halos.Add(c + up * 2f - fr * 2f, 3f, new Color(.6f, .86f, 1f), .3f); break; }
                case "Phil Harris": { var s = Any(); var t1 = c - sd * 1.8f; var t2 = c + sd * 1.8f; foreach (var t in new[] { t1, t2 }) { z.mk.Prism(trunkM, t, up, .22f, 3.4f, 7); z.mk.Blob(leafM, t + up * 3.6f, new Vector3(1.6f, 1.2f, 1.6f), rng, .3f); ArtistBits.PostCollider(z.root, t, 3.4f, .3f); } for (var k = 0; k <= 8; k++) { var p = Vector3.Lerp(t1, t2, k / 8f) + up * (1.6f - Mathf.Sin(k / 8f * Mathf.PI) * .6f); z.mk.Box(Paint(new Color(.8f, .7f, .4f)), p, new Vector3(.45f, .04f, .9f), Quaternion.LookRotation(fr)); } z.mk.Blob(SongGlow(s, new Color(1f, .8f, .4f), .8f), c + up * 1.2f + fr * .6f, new Vector3(.2f, .2f, .2f), rng, 0f); Interact.Seat(tlife.transform, c + up * 1.1f, fr, "Lie in the hammock"); break; }
                case "Tori Kelly": { var s = Any(); for (var k = 0; k < 18; k++) { var a = k * .5f; var lp = c + up * (.6f + k * .2f) + (fr * Mathf.Cos(a) + sd * Mathf.Sin(a)) * (.8f + k * .05f); var lc = new[] { new Color(.9f, .4f, .2f), new Color(1f, .8f, .2f), new Color(.6f, .2f, .5f), new Color(.3f, .7f, .4f) }[k % 4]; z.mk.Box(k == 17 ? SongGlow(s, lc, 1f) : GlowM(lc, .5f), lp, new Vector3(.3f, .02f, .16f), Quaternion.Euler(0, k * 40f, 30f)); } ArtistBits.PostCollider(z.root, c, 4.4f, 1.6f); break; }
                case "The Disneyland Children's Chorus": { var s = Song(z, "It's a Small World"); life.worldSong = s; var ch = c; z.mk.Prism(seaM, ch, up, 1.8f, .04f, 18); z.mk.Prism(Paint(Cream), ch - up * .2f, up, 2f, .2f, 18); var bt = ch + up * .3f; z.mk.Box(Paint(new Color(.6f, .8f, 1f)), bt, new Vector3(1.2f, .4f, 2.4f), rot); for (var k = 0; k < 3; k++) z.mk.Box(Paint(new[] { new Color(1f, .7f, .8f), new Color(.7f, 1f, .8f), new Color(1f, .9f, .6f) }[k]), bt + up * .3f + fr * (-.7f + k * .7f), new Vector3(1f, .1f, .2f), rot); BoxCol(z.root, ch + up * .4f, new Vector3(4f, .8f, 4f), rot); var clk = c + sd * 3f; z.mk.Box(Paint(new Color(1f, .9f, .9f)), clk + up * 1.6f, new Vector3(1f, 3.2f, 1f), rot); z.mk.Cone(Paint(new Color(.8f, .7f, 1f)), clk + up * 3.2f, .8f, 1f, 4, 45f); z.mk.Prism(SongGlow(s, new Color(1f, .9f, .5f), 1f), clk + up * 2.4f + fr * .51f, fr, .34f, .02f, 14); BoxCol(z.root, clk + up * 1.6f, new Vector3(1f, 3.2f, 1f), rot); break; }
                case "Carmen Twillie": { var s = Song(z, "Circle of Life"); life.circleSong = s; z.mk.Prism(SongGlow(s, new Color(1f, .7f, .2f), 1.2f), f.P(0, 2.6f, .14f), f.Fwd, 2f, .02f, 28); for (var k = 0; k < 12; k++) z.mk.Box(GlowM(new Color(1f, .8f, .4f), .8f), f.P(0, 2.6f, .15f) + Quaternion.AngleAxis(k * 30f, f.Fwd) * up * 2.6f, new Vector3(.12f, .8f, .02f), Quaternion.AngleAxis(k * 30f, f.Fwd) * f.r); var rk = c; z.mk.Blob(Paint(new Color(.56f, .48f, .42f)), rk + up * .5f, new Vector3(1.2f, .8f, 1f), rng, .3f); BoxCol(z.root, rk + up * .5f, new Vector3(2f, 1f, 1.8f), rot); break; }
                case "Carolina Gaitán - La Gaita": { var s = Any(); var vc = c; Plinth(vc, .6f, .9f); z.mk.Box(SongGlow(s, new Color(.3f, 1f, .5f), 1.2f), vc + up * 1.5f, new Vector3(1f, 1.2f, .08f), rot * Quaternion.Euler(0, 0, 8f)); halos.Add(vc + up * 1.5f + fr * .5f, 1.6f, new Color(.3f, 1f, .5f), .35f); for (var k = 0; k < 6; k++) z.mk.Blob(Paint(new Color(.9f, .5f, .2f)), vc + sd * (1f + k * .2f) + up * .1f, new Vector3(.2f, .06f, .2f), rng, .3f); break; }
                case "Catherine O'Hara": { var s = Any(); var hl = c - fr * .6f; for (var k = 0; k < 14; k++) { var a = k / 13f; z.mk.Box(Paint(new Color(.2f, .22f, .26f)), hl + up * (Mathf.Sin(a * Mathf.PI) * 2.4f + a * a * 1.6f) + sd * (a * 3f - 1.5f), new Vector3(.5f, .2f, .8f), Quaternion.LookRotation(fr) * Quaternion.Euler(0, 0, -Mathf.Cos(a * Mathf.PI) * 50f - a * 40f)); } z.mk.Prism(SongGlow(s, new Color(.95f, .95f, .8f), 1.3f), f.P(1.6f, 3.2f, .14f), f.Fwd, 1f, .02f, 22); BoxCol(z.root, hl + up * 1f, new Vector3(3f, 2f, .8f), rot); break; }
                case "Alberto Pomeranz": { var s = Any(); var pl = c + up * 1.4f; Post(c, 1.2f, .06f); var red = Paint(new Color(.8f, .1f, .1f)); z.mk.Box(red, pl, new Vector3(.5f, .5f, 2.4f), rot); foreach (var e in new[] { 0f, .7f }) z.mk.Box(red, pl + up * e + fr * .4f, new Vector3(3.4f, .06f, .6f), rot); foreach (var e in new[] { -1f, 1f }) z.mk.Prism(iron, pl + fr * .4f + sd * e * 1.2f, up, .02f, .7f, 4); z.mk.Box(SongGlow(s, new Color(1f, .9f, .6f), .8f), pl + fr * 1.24f, new Vector3(1f, .1f, .04f), rot); z.mk.Box(red, pl - fr * 1.1f + up * .3f, new Vector3(.06f, .6f, .4f), rot); BoxCol(z.root, pl + up * .3f, new Vector3(3.4f, 1.2f, 2.6f), rot); break; }
                case "Filippo Vicarelli": { var s = Any(); var sc = c; z.mk.Prism(woodM, sc, (up + sd * .1f).normalized, .05f, 2.6f, 6); ArtistBits.PostCollider(z.root, sc, 2.6f, .1f); var top = sc + (up + sd * .1f).normalized * 2.6f; z.mk.Tri(SongGlow(s, new Color(.8f, .86f, .9f), .9f), top, top - sd * 1.6f - up * .5f, top - sd * .6f - up * .2f, top - fr); z.mk.Tri(SongGlow(s, new Color(.8f, .86f, .9f), .9f), top, top - sd * 1.6f - up * .5f, top - sd * .6f - up * .2f, top + fr); break; }
                case "Fonola Band": { var s = Any(); var mq = c; z.mk.Box(Paint(new Color(.8f, .1f, .1f)), mq + up * 1.1f, new Vector3(.5f, .9f, .3f), rot); foreach (var e in new[] { -.13f, .13f }) z.mk.Box(Paint(new Color(.8f, .1f, .1f)), mq + up * .35f + sd * e, new Vector3(.2f, .7f, .2f), rot); z.mk.Blob(Paint(Cream), mq + up * 1.8f, new Vector3(.2f, .24f, .2f), rng, 0f); z.mk.Box(SongGlow(s, new Color(1f, .3f, .3f), .7f), mq + up * 1.86f + fr * .2f, new Vector3(.24f, .06f, .02f), rot); BoxCol(z.root, mq + up * 1f, new Vector3(.6f, 2f, .4f), rot); for (var k = 0; k < 6; k++) z.mk.Box(Paint(new Color(.5f, .7f, .5f)), c + sd * 1.4f + up * (.1f + (k / 3) * .2f) + fr * ((k % 3) * .3f), new Vector3(.6f, .18f, .28f), rot); break; }
                case "Geek Music": { var s = Any(); for (var x = 0; x < 6; x++) for (var y = 0; y < 5; y++) { var tile = f.P(-2.75f + x * 1.1f, .8f + y * .62f, .14f); z.mk.Box(y == 4 ? Paint(new Color(.1f, .14f, .5f)) : SongGlow(x == 0 && y == 0 ? s : null, new Color(.2f, .3f, .9f), .7f), tile, new Vector3(1f, .54f, .02f), f.r); if (y < 4) ArtistBits.Label(z.labels, "$" + ((4 - y) * 200), tile + f.Fwd * .02f, f.Fwd, .16f, Gold, TextAnchor.MiddleCenter); } for (var k = 0; k < 3; k++) { var pd = c + sd * (-1.4f + k * 1.4f); z.mk.Box(Paint(new Color(.3f, .3f, .5f)), pd + up * .5f, new Vector3(.9f, 1f, .5f), rot); BoxCol(z.root, pd + up * .5f, new Vector3(.9f, 1f, .5f), rot); z.mk.Box(lampM, pd + up * 1.01f, new Vector3(.2f, .02f, .2f), rot); } break; }
                case "Joe \"Bean\" Esposito": { var s = Any(); z.mk.Box(Paint(new Color(.9f, .86f, .7f)), c + up * .03f, new Vector3(3f, .06f, 3f), rot); z.mk.Box(Paint(new Color(.8f, .2f, .2f)), c + up * .07f, new Vector3(2.2f, .02f, 2.2f), rot); var tp = c + sd * 1.9f; Plinth(tp, .5f, .9f); z.mk.Prism(SongGlow(s, Gold, 1f), tp + up * .9f, up, .1f, .7f, 8, 1.8f); z.mk.Blob(Paint(Gold), tp + up * 1.7f, new Vector3(.12f, .14f, .12f), rng, 0f); break; }
                case "Patryk Scelina": { var s = Any(); Plinth(c, .9f, .7f); z.mk.Prism(Paint(new Color(.3f, .3f, .34f)), c + up * .7f, up, .4f, .4f, 12, 1.2f); z.mk.Prism(SongGlow(s, new Color(1f, .8f, .3f), 1f), c + up * 1.08f, up, .44f, .02f, 12); for (var k = 0; k < 3; k++) { var sk = c + up * 1.3f + Quaternion.Euler(0, k * 120f, 0) * fr * .5f; z.mk.Blob(Paint(Cream), sk + up * .2f, new Vector3(.1f, .12f, .1f), rng, 0f); z.mk.Prism(iron, sk - up * .2f, (sk - (c + up * 1f)).normalized, .01f, .4f, 3); } for (var k = 0; k < 10; k++) z.mk.Blob(Paint(new Color(1f, .55f, .1f)), c + Quaternion.Euler(0, k * 36f, 0) * fr * .9f + up * .05f, new Vector3(.12f, .08f, .12f), rng, .3f); break; }
                case "Piero Umiliani": { var s = Any(); for (var k = 0; k < 3; k++) { var mp = c + sd * (-1.2f + k * 1.2f); var col = k == 1 ? new Color(.9f, .5f, .2f) : new Color(1f, .5f, .7f); z.mk.Blob(Paint(col), mp + up * (k == 1 ? .9f : .6f), new Vector3(.4f, k == 1 ? .9f : .6f, .4f), rng, .3f); z.mk.Prism(k == 0 ? SongGlow(s, new Color(1f, .3f, .3f), .8f) : Paint(new Color(.8f, .2f, .3f)), mp + up * (k == 1 ? 1.3f : .9f) + fr * .3f, fr, .14f, .3f, 10, 1.4f); ArtistBits.PostCollider(z.root, mp, 1.2f, .4f); } break; }
                case "Shady Cicada": { var s = Any(); var pod = c; z.mk.Blob(Paint(new Color(.4f, .8f, .3f)), pod + up * .5f, new Vector3(1.6f, .4f, .5f), rng, .1f); for (var k = 0; k < 4; k++) z.mk.Blob(k == 0 ? SongGlow(s, new Color(.6f, 1f, .4f), .8f) : Paint(new Color(.5f, .9f, .3f)), pod + up * .8f + sd * (-.9f + k * .6f), Vector3.one * .26f, rng, 0f); BoxCol(z.root, pod + up * .4f, new Vector3(1f, .8f, 3.2f), Quaternion.LookRotation(sd)); Interact.Seat(tlife.transform, pod + up * .55f + fr * .2f, fr, "Sit on the pea pod"); break; }
                case "SpongeBob SquarePants": { var s = Any(); var fp = c; for (var k = 0; k < 6; k++) z.mk.Prism(trunkM, fp + Quaternion.Euler(0, k * 60f, 0) * fr * .5f, (up + Quaternion.Euler(0, k * 60f, 0) * -fr).normalized, .08f, .8f, 5); z.mk.Cone(SongGlow(s, new Color(1f, .6f, .2f), 1.8f), fp + up * .1f, .4f, 1f, 6); life.fires.Add(fp + up * .6f); ArtistBits.PointLight(z.root, fp + up * 1f, new Color(1f, .6f, .3f), 8f, 1.5f); ArtistBits.PostCollider(z.root, fp, .8f, .6f); foreach (var e in new[] { -1.6f, 1.6f }) { var lg = fp + sd * e + up * .25f; z.mk.Prism(trunkM, lg - fr * .9f, fr, .25f, 1.8f, 8); BoxCol(z.root, lg, new Vector3(.5f, .5f, 1.8f), rot); Interact.Seat(tlife.transform, lg + up * .25f, -sd * Mathf.Sign(e), "Sit by the campfire"); } var uk = new MeshKit(); uk.Blob(Paint(new Color(.8f, .5f, .3f)), new Vector3(0, .1f, 0), new Vector3(.18f, .1f, .26f), rng, 0f); uk.Box(Paint(new Color(.5f, .3f, .2f)), new Vector3(0, .12f, .5f), new Vector3(.06f, .03f, .5f), Quaternion.identity); Thing(uk, "Ukulele", "a little ukulele", fp + sd * 1.6f + fr * .6f + up * .5f, rot, new Vector3(.4f, .2f, 1f), .4f, false); break; }
                case "The Intermezzo Orchestra": { var s = Song(z, "Pirates"); life.pirateSong = s; var ch = c; z.mk.Box(woodM, ch + up * .3f, new Vector3(1.2f, .6f, .8f), rot); z.mk.Prism(woodM, ch + up * .6f - sd * .6f, sd, .4f, 1.2f, 10); z.mk.Box(SongGlow(s, Gold, 1.4f), ch + up * .7f + fr * .1f, new Vector3(1f, .2f, .5f), rot * Quaternion.Euler(-30f, 0, 0)); halos.Add(ch + up * 1.2f, 1.6f, Gold, .3f); BoxCol(z.root, ch + up * .4f, new Vector3(1.2f, .8f, .8f), rot); for (var k = 0; k < 6; k++) z.mk.Prism(Paint(Gold), ch + fr * .8f + Quaternion.Euler(0, k * 50f, 0) * sd * .4f, up, .1f, .02f, 8); break; }
                case "The Muppets": { var s = Song(z, "Rainbow Connection"); life.rainbowSong = s; var lg = c + up * .3f; z.mk.Prism(trunkM, lg - sd * 1.4f, sd, .35f, 2.8f, 8); BoxCol(z.root, lg, new Vector3(.7f, .7f, 2.8f), Quaternion.LookRotation(sd)); Interact.Seat(tlife.transform, lg + up * .35f, fr, "Sit on the log in the swamp"); z.mk.Prism(seaM, c - fr * 1.2f - up * .15f, up, 1.6f, .02f, 14); var rb = new[] { new Color(1f, .3f, .3f), new Color(1f, .6f, .2f), new Color(1f, .9f, .3f), new Color(.4f, .9f, .4f), new Color(.3f, .6f, 1f), new Color(.6f, .4f, 1f) }; for (var b = 0; b < 6; b++) for (var k = 0; k <= 16; k++) { var a = Mathf.PI * k / 16f; var r = 3f - b * .22f; z.mk.Box(b == 0 && k == 8 ? SongGlow(s, rb[b], 1f) : GlowM(rb[b], .7f), f.P(Mathf.Cos(a) * r, .6f + Mathf.Sin(a) * r, .2f), new Vector3(.62f, .22f, .02f), f.r * Quaternion.Euler(0, 0, a * Mathf.Rad2Deg + 90f)); } var bj = new MeshKit(); bj.Prism(Paint(new Color(.9f, .86f, .7f)), Vector3.zero, Vector3.up, .2f, .06f, 14); bj.Box(Paint(new Color(.5f, .3f, .2f)), new Vector3(0, .03f, .6f), new Vector3(.06f, .03f, .9f), Quaternion.identity); Thing(bj, "Banjo", "a banjo, a bit swampy", lg + up * .4f + sd * 1f, rot, new Vector3(.4f, .1f, 1.2f), .6f, false); break; }
                case "The Simpsons": { var s = Any(); var cu = c; z.mk.Box(Paint(new Color(.5f, .6f, .3f)), cu + up * .35f, new Vector3(2.4f, .5f, .9f), rot); z.mk.Box(Paint(new Color(.5f, .6f, .3f)), cu + up * .8f - fr * .4f, new Vector3(2.4f, .7f, .2f), rot); foreach (var e in new[] { -1.2f, 1.2f }) z.mk.Box(Paint(new Color(.5f, .6f, .3f)), cu + up * .55f + sd * e, new Vector3(.2f, .5f, .9f), rot); BoxCol(z.root, cu + up * .5f, new Vector3(2.6f, 1f, 1f), rot); Interact.Bench(tlife.transform, cu + up * .55f - sd * .7f, cu + up * .55f + sd * .7f, fr); var tv = cu + fr * 2.4f; z.mk.Box(woodM, tv + up * .5f, new Vector3(1f, 1f, .8f), rot); z.mk.Box(SongGlow(s, new Color(.6f, .8f, 1f), 1f), tv + up * .6f - fr * .41f, new Vector3(.7f, .5f, .02f), rot); BoxCol(z.root, tv + up * .5f, new Vector3(1f, 1f, .8f), rot); var sb = new MeshKit(); sb.Box(Paint(new Color(.9f, .3f, .2f)), new Vector3(0, .1f, 0), new Vector3(.24f, .03f, .8f), Quaternion.identity); foreach (var q in new[] { -.3f, .3f }) sb.Box(dark, new Vector3(0, .04f, q), new Vector3(.22f, .06f, .06f), Quaternion.identity); Thing(sb, "Skateboard", "a red skateboard", cu + sd * 2f + up * .02f, rot, new Vector3(.3f, .14f, .8f), .8f, false); break; }
                case "Zac Efron": case "Troy": { var s = Any(); var bk = c; z.mk.Prism(iron, bk, up, .06f, 3f, 5); ArtistBits.PostCollider(z.root, bk, 3f, .08f); z.mk.Box(white, bk + up * 3.3f + fr * .1f, new Vector3(1.2f, .8f, .05f), rot); for (var k = 0; k < 10; k++) { var a = Quaternion.Euler(0, k * 36f, 0); z.mk.Box(SongGlow(k == 0 ? s : null, new Color(1f, .5f, .2f), .7f), bk + up * 3f + fr * .5f + a * Vector3.forward * .22f, new Vector3(.1f, .02f, .02f), a); } var ball = new MeshKit(); ball.Blob(Paint(new Color(.9f, .45f, .15f)), new Vector3(0, .13f, 0), Vector3.one * .13f, rng, 0f); Thing(ball, "Basketball", "a basketball, Wildcats red", bk + fr * 1.4f + up * .02f, Quaternion.identity, new Vector3(.27f, .27f, .27f), .5f, false); break; }
                case "Erika Henningsen": { var s = Any(); Plinth(c, .7f, .8f); z.mk.Box(SongGlow(s, Pink, .7f), c + up * .9f, new Vector3(.4f, .14f, .54f), rot * Quaternion.Euler(0, 20f, 0)); Heart(z.mk, Paint(new Color(.9f, .2f, .4f)), c + up * .98f, up, .12f); break; }
                case "Sharpay Evans": { var s = Song(z, "Fabulous"); var lc = c; z.mk.Box(Paint(Pink), lc + up * .3f, new Vector3(.7f, .06f, 1.6f), rot * Quaternion.Euler(-10f, 30f, 0)); z.mk.Box(Paint(Pink), lc + up * .7f - Quaternion.Euler(0, 30f, 0) * fr * .7f, new Vector3(.7f, .06f, .7f), rot * Quaternion.Euler(-55f, 30f, 0)); Interact.Seat(tlife.transform, lc + up * .4f, Quaternion.Euler(0, 30f, 0) * fr, "Lounge fabulously").seatHeight = .3f; z.mk.Prism(SongGlow(s, new Color(.6f, .9f, 1f), .7f), lc + sd * 1.4f + up * .02f, up, .8f, .04f, 14); break; }
                case "Write Out Loud": { var s = Any(); z.mk.Box(Paint(Gold), c + up * 1.2f, new Vector3(1f, 2f, .08f), rot); z.mk.Box(SongGlow(s, new Color(.8f, .9f, 1f), .6f), c + up * 1.2f + fr * .05f, new Vector3(.8f, 1.8f, .02f), rot); BoxCol(z.root, c + up * 1.2f, new Vector3(1f, 2.2f, .2f), rot); for (var k = 0; k < 5; k++) z.mk.Cone(Paint(Gold), c + up * 2.24f + sd * (-.24f + k * .12f), .04f, .14f, 4); break; }
                default: { var s = Any(); Post(c, 1.4f); z.mk.Prism(SongGlow(s, Gold, 1.2f), c + up * 1.5f, up, .2f, .2f, 10); break; }
            }
            Folk(z.At(Foot(z.a) * .25f, Foot(z.a) * .45f), -fr, Resident.Pose.Stand, new[] { "Quiet on set. Well, quiet-ish.", "Every set's a different film. This one's my favourite. They all are.", "Sit in the chair. Yell \"cut\". Nobody will stop." }, Local());
        }
    }
}
