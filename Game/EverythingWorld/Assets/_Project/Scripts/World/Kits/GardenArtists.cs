using System.Collections.Generic;
using UnityEngine;
using static EverythingWorld.World.PlaceTools;

namespace EverythingWorld.World
{
    /// The Sample Gardens' artists. Each has a round bed edged in brick, a little painted wooden sign on a stake, and one
    /// thing from their music growing in it; covers pinned to a trellis board with roses climbing it.
    /// - Bibio: a garden shed with its door open and a reel-to-reel tape machine on a table outside, a cherry tree dropping
    ///   petals ("Petals"), an arch of curling vines ("Curls") and a bench with initials carved in it ("lovers' carvings").
    /// - Wintergatan: a marble machine, a great wooden wheel turning, a lift carrying marbles up and tracks running them down
    ///   through a little xylophone ("Sommarfågel").
    /// - The rest: an atom mobile and a dropped tray, dark stars hanging on threads, a blue scarf on a line, a silver disc in
    ///   a tree, a PA horn, a sun of stacked colours, a little shrine of crates and a lamp, a heap of records with a palm and
    ///   a suitcase, a tape recorder on a pile of books, a gramophone horn, a glass fountain, a birdhouse.
    public partial class GardenKit
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
                Add("The Avalanches", "Since I Left You", "The Avalanches"); Add("Bibio", "Petals", "Bibio"); Add("Caribou", "Silver", "Caribou"); Add("Wintergatan", "Sommarfågel", "Wintergatan");
                Add("The Books", "Tokyo", "The Books"); Add("BLARF", "Stars Without Light", "BLARF"); Add("Dolphin Machine", "Sunshine Gradient", "Dolphin Machine"); Add("Thom Yorke", "Hearing Damage", "Thom Yorke");
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
            if (brick == null) MakeMaterials();
            var root = new GameObject("Artist: " + a.name).transform; root.SetParent(ctx.Root, false);
            var front = facing.TryGetValue(a.name, out var f) ? f : new Vector3(center.x - a.x, 0, center.z - a.z).normalized;
            if (front.sqrMagnitude < .01f) front = Vector3.forward;
            var labels = new GameObject("Labels").transform; labels.SetParent(root, false);
            var area = root.gameObject.AddComponent<ArtistArea>();
            area.artistName = a.name; area.stationId = a.stationId; area.radius = a.r; area.glowColor = Gold;
            var z = new Plot { ctx = ctx, root = root, labels = labels, mk = new MeshKit(), solid = new MeshKit(), pictures = new MeshKit(), a = a, c = ctx.OnGround(a.x, a.z), front = front, side = Vector3.Cross(Vector3.up, front), area = area };
            var reach = Foot(a); var up = Vector3.up;
            z.mk.Prism(brickD, z.c - up * .3f, up, reach * .8f, .38f, 26); z.mk.Prism(Paint(new Color(.42f, .56f, .3f)), z.c + up * .085f, up, reach * .76f, .02f, 26);
            switch (a.name)
            {
                case "Bibio": Shed(z); break;
                case "Wintergatan": Marbles(z); break;
                default: Others(z, SignStake(z)); break;
            }
            SongLamps(z);
            Board(z, z.At(reach * .6f, -reach * .45f - 1.5f), front);
            ArtistBits.Plaque(z.mk, z.solid, labels, a, z.At(reach * .6f + 1.5f, reach * .45f + 1.5f), front, woodD, Paint(new Color(.3f, .4f, .3f)), Cream, 1.6f);
            Lamp(ctx, z.mk, z.At(reach * .5f, reach * .7f), false);
            if (z.area.glowLight == null) z.area.glowLight = ArtistBits.PointLight(root, z.c + up * 4f + front * 3f, Gold, HasStation(a) ? 22f : 13f, 1.2f);
            area.glowRenderers = new Renderer[0];
            z.mk.Build("Area", root, false);
            z.solid.BuildCollider("Walls", root);
            z.pictures.Build("Covers", root, false);
            noGrass.Add(new Vector3(z.c.x, reach * .85f, z.c.z));
            var mark = new GameObject("Shot: " + a.name + " up close").transform; mark.SetParent(ctx.Root, false);
            var cam = z.At(reach * .85f, reach * .3f) + up * 2f; var look = z.At(-reach * .3f, 0) + up * 1.8f - cam; mark.SetPositionAndRotation(cam, Quaternion.LookRotation(look.normalized));
        }

        /// The covers pinned to a trellis board with roses climbing it.
        void Board(Plot z, Vector3 ground, Vector3 dir)
        {
            dir = Vector3.ProjectOnPlane(dir, Vector3.up).normalized; var rot = Quaternion.LookRotation(dir); var rng = new System.Random(z.a.name.Length);
            var n = Mathf.Max(1, Mathf.Min(7, z.a.songList?.Length ?? 1)); const float size = .9f; var w = n * size * 1.25f + .6f;
            var c = ground + Vector3.up * 1.7f; var side = Vector3.Cross(Vector3.up, dir);
            z.mk.Box(woodD, c - Vector3.up * .5f - dir * .06f, new Vector3(w + .4f, size + 1.9f, .1f), rot); z.solid.Box(woodD, c - Vector3.up * .5f, new Vector3(w + .4f, size + 1.9f, .3f), rot); z.mk.Box(Paint(Cream), c - Vector3.up * .1f + dir * .02f, new Vector3(w, size + .9f, .04f), rot);
            for (var k = 0; k < 8; k++) { z.mk.Blob(leafD, c + side * (k % 2 == 0 ? -1f : 1f) * (w / 2f + .1f) + Vector3.up * (-1.3f + k * .36f) + dir * .06f, new Vector3(.24f, .2f, .14f), rng, .3f); if (k % 2 == 1) z.mk.Blob(Paint(Rose), c + side * (w / 2f + .14f) * (k % 4 == 1 ? 1f : -1f) + Vector3.up * (-1.2f + k * .36f) + dir * .1f, Vector3.one * .08f, rng, 0f); }
            ArtistBits.CoverWall(z.pictures, z.labels, z.ctx, z.a, c + dir * .06f + Vector3.up * .08f, dir, n, size, n, true, Ink);
        }

        /// A little jam jar of light on a stake for each song without its own piece.
        void SongLamps(Plot z)
        {
            if (!SongIds.TryGetValue(z.a.name, out var list)) return;
            var up = Vector3.up; var k = 0; var seen = new HashSet<string>();
            foreach (var (_, id) in list)
            {
                if (z.lit.Contains(id) || !seen.Add(id)) continue;
                var p = z.At(Foot(z.a) * .55f - (k % 2) * 1.3f, Foot(z.a) * .2f + (k / 2) * 1.3f);
                z.mk.Prism(woodD, p, up, .03f, 1f, 4); z.mk.Prism(SongGlow(id, Gold, 1.4f), p + up * 1f, up, .1f, .18f, 8); ArtistBits.PostCollider(z.root, p, 1.2f, .1f);
                k++;
            }
        }

        /// A little painted wooden sign on a stake.
        PlaceFrame SignStake(Plot z)
        {
            var up = Vector3.up; var f = new PlaceFrame(z.At(-Foot(z.a) * .5f, 0), z.front);
            z.mk.Box(woodD, f.P(0, .6f, 0), new Vector3(.08f, 1.2f, .08f), f.r); z.mk.Box(Paint(new Color(.84f, .78f, .62f)), f.P(0, 1.2f, .05f), new Vector3(1.4f, .4f, .04f), f.r * Quaternion.Euler(0, 0, 3f)); ArtistBits.Label(z.labels, z.a.name, f.P(0, 1.2f, .08f), f.Fwd, .1f, Ink, TextAnchor.MiddleCenter); ArtistBits.PostCollider(z.root, f.o, 1.4f, .1f);
            return f;
        }

        // ---------------------------------------------------------------- Bibio: the shed
        void Shed(Plot z)
        {
            var up = Vector3.up; var rng = new System.Random(2009); var fr = z.front; var sd = z.side; var rot = z.Rot;
            var curls = Song(z, "Curls"); var petals = Song(z, "Petals"); var carv = Song(z, "lovers"); life.petalSong = petals;
            // the shed
            var sh = new PlaceFrame(z.At(-6f, -2f), fr); var plank = Paint(new Color(.5f, .6f, .56f)); z.mk.Box(plank, sh.P(0, 1.2f, 0), new Vector3(3f, 2.4f, 2.4f), sh.r); z.solid.Box(plank, sh.P(0, 1.2f, 0), new Vector3(3f, 2.4f, 2.4f), sh.r); z.mk.Prism(woodD, sh.P(0, 2.4f, 0), up, 2.3f, 1f, 4, .05f, 45f); z.mk.Box(dark, sh.P(-.6f, 1f, 1.21f), new Vector3(.8f, 1.8f, .02f), sh.r); z.mk.Box(plank, sh.P(-1.3f, 1f, 1.6f), new Vector3(.06f, 1.8f, .8f), sh.r * Quaternion.Euler(0, 40f, 0)); z.mk.Box(GlowM(Gold, 1f), sh.P(.8f, 1.4f, 1.21f), new Vector3(.6f, .5f, .02f), sh.r); for (var k = 0; k < 8; k++) z.mk.Box(Paint(new Color(.42f, .5f, .48f)), sh.P(-1.5f + k * .43f, 1.2f, 1.21f), new Vector3(.02f, 2.4f, .01f), sh.r);
            // a reel-to-reel on a table
            var tb = sh.P(1.6f, 0, 2.6f); z.mk.Box(wood, tb + up * .75f, new Vector3(1.2f, .06f, .7f), rot); foreach (var e in new[] { -.5f, .5f }) foreach (var q in new[] { -.28f, .28f }) z.mk.Box(woodD, tb + up * .37f + sd * e + fr * q, new Vector3(.05f, .74f, .05f), rot); z.mk.Box(Paint(new Color(.5f, .48f, .44f)), tb + up * 1.1f, new Vector3(.9f, .6f, .2f), rot); foreach (var e in new[] { -.22f, .22f }) { var rg = new MeshKit(); rg.Prism(dark, Vector3.zero, Vector3.forward, .2f, .03f, 16); rg.Prism(Paint(new Color(.6f, .56f, .5f)), Vector3.forward * .031f, Vector3.forward, .06f, .01f, 8); var rgo = rg.BuildSingle("Tape reel", z.root, false); rgo.transform.SetPositionAndRotation(tb + up * 1.22f + sd * e + fr * .1f, rot); life.reels.Add(rgo.transform); } BoxCol(z.root, tb + up * .55f, new Vector3(1.2f, 1.1f, .7f), rot);
            // a cherry tree dropping petals
            var ct = z.At(3f, -6f); z.mk.Prism(trunkM, ct, (up + sd * .1f).normalized, .3f, 3f, 7, .6f); for (var k = 0; k < 7; k++) z.mk.Blob(k == 0 ? SongGlow(petals, new Color(1f, .78f, .86f), .8f) : Paint(new Color(1f, .78f, .86f)), ct + up * (3.4f + (k % 2) * .5f) + Quaternion.Euler(0, k * 51f, 0) * fr * (k == 0 ? 0f : 1.4f), new Vector3(1.4f, 1f, 1.4f), rng, .3f); ArtistBits.PostCollider(z.root, ct, 3f, .4f); life.petalTree = ct + up * 3.6f;
            // an arch of curling vines
            var ar = new PlaceFrame(z.At(2f, 5f), fr); for (var k = 0; k <= 14; k++) { var a = Mathf.PI * k / 14f; var p = ar.P(Mathf.Cos(a) * 1.3f, Mathf.Sin(a) * 2.6f, 0); z.mk.Box(leafD, p, new Vector3(.2f, .2f, .2f), ar.r); for (var j = 0; j < 3; j++) z.mk.Box(k % 4 == 0 && j == 0 ? SongGlow(curls, Leaf, 1.4f) : leafM, p + ar.r * (Quaternion.Euler(0, 0, j * 120f + k * 30f) * Vector3.right) * .3f, new Vector3(.14f, .04f, .08f), ar.r * Quaternion.Euler(0, 0, j * 120f + k * 30f)); } foreach (var e in new[] { -1.3f, 1.3f }) ArtistBits.PostCollider(z.root, ar.P(e, 0, 0), 1.6f, .16f);
            // a bench with initials carved in it
            var bn = z.At(-1f, 8f); Bench(z.root, z.mk, bn, -sd); z.mk.Box(SongGlow(carv, Gold, 1f), bn + up * .82f + sd * .27f, new Vector3(.5f, .08f, .01f), Quaternion.LookRotation(-sd)); ArtistBits.Label(z.labels, "A + B", bn + up * .82f + sd * .28f, -(-sd), .06f, Ink, TextAnchor.MiddleCenter);
            Folk(z.At(4f, 1f), -fr, Resident.Pose.Stand, new[] { "Every sound on that tape was recorded in this garden.", "Sit under the cherry tree. Count the petals. Lose count.", "Somebody carved their names in the bench. Every summer someone new." }, Local());
            z.area.glowLight = ArtistBits.PointLight(z.root, z.c + up * 4f, Gold, 22f, 1.4f);
        }

        // ---------------------------------------------------------------- Wintergatan: the marble machine
        void Marbles(Plot z)
        {
            var up = Vector3.up; var rng = new System.Random(2016); var fr = z.front; var sd = z.side; var rot = z.Rot;
            var s = SongIds.TryGetValue(z.a.name, out var l) && l.Length > 0 ? Song(z, l[0].title) : null; life.summerSong = s;
            var mf = new PlaceFrame(z.At(-3f, 0), fr); const float W = 5f, H = 5f;
            foreach (var x in new[] { -W / 2f, W / 2f }) foreach (var q in new[] { -.8f, .8f }) { z.mk.Box(wood, mf.P(x, H / 2f, q), new Vector3(.2f, H, .2f), mf.r); } foreach (var y in new[] { .3f, H }) foreach (var q in new[] { -.8f, .8f }) z.mk.Box(wood, mf.P(0, y, q), new Vector3(W + .2f, .2f, .2f), mf.r); z.solid.Box(wood, mf.P(0, H / 2f, 0), new Vector3(W + .4f, H, 1.8f), mf.r);
            // the big wheel
            var wm = new MeshKit(); for (var k = 0; k < 24; k++) { float a0 = k * Mathf.PI / 12f, a1 = (k + 1) * Mathf.PI / 12f; var p0 = new Vector3(Mathf.Cos(a0), Mathf.Sin(a0), 0) * 1.4f; var p1 = new Vector3(Mathf.Cos(a1), Mathf.Sin(a1), 0) * 1.4f; wm.Box(woodD, (p0 + p1) / 2f, new Vector3((p1 - p0).magnitude, .12f, .1f), Quaternion.FromToRotation(Vector3.right, p1 - p0)); if (k % 3 == 0) wm.Blob(GlowM(new Color(.8f, .9f, 1f), .9f), p0 * .9f, Vector3.one * .08f, rng, 0f); } for (var k = 0; k < 6; k++) wm.Box(wood, Vector3.zero, new Vector3(2.8f, .06f, .06f), Quaternion.Euler(0, 0, k * 30f)); wm.Prism(woodD, Vector3.back * .3f, Vector3.forward, .2f, .6f, 10);
            var wg = wm.BuildSingle("Marble wheel", z.root, false); wg.transform.SetPositionAndRotation(mf.P(-1f, 2f, .95f), mf.r); life.wheels.Add(wg.transform);
            // the lift, the tracks, the little xylophone
            z.mk.Box(woodD, mf.P(W / 2f - .5f, H / 2f + .2f, .9f), new Vector3(.3f, H - .4f, .1f), mf.r);
            var track = new[] { mf.P(W / 2f - .5f, H - .2f, .9f), mf.P(-W / 2f + .3f, H - .8f, .9f), mf.P(W / 2f - .9f, H - 1.6f, .9f), mf.P(-W / 2f + .3f, H - 2.4f, .9f), mf.P(.6f, 1f, .9f) };
            for (var k = 0; k + 1 < track.Length; k++) { var a = track[k]; var b = track[k + 1]; z.mk.Box(iron, (a + b) / 2f, new Vector3(.04f, .04f, (b - a).magnitude), Quaternion.LookRotation(b - a)); z.mk.Box(iron, (a + b) / 2f + mf.Fwd * .08f, new Vector3(.04f, .04f, (b - a).magnitude), Quaternion.LookRotation(b - a)); }
            life.marbleTrack.AddRange(track); life.marbleTrack.Add(mf.P(W / 2f - .5f, .5f, .9f));
            for (var k = 0; k < 8; k++) { var mm = new MeshKit(); mm.Blob(GlowM(new Color(.7f, .9f, 1f), 1.4f), Vector3.zero, Vector3.one * .07f, rng, 0f); var mg = mm.BuildSingle("Marble", z.root, false); life.marbles.Add((mg.transform, k / 8f)); }
            var xy = mf.P(.6f, .9f, .9f); var keys = new[] { new Color(1f, .3f, .3f), new Color(1f, .6f, .2f), new Color(1f, .9f, .3f), new Color(.4f, .9f, .4f), new Color(.3f, .6f, 1f), new Color(.6f, .4f, 1f) }; for (var k = 0; k < 6; k++) z.mk.Box(k == 0 ? SongGlow(s, keys[k], 1.4f) : GlowM(keys[k], 1f), xy + mf.Right * (k - 2.5f) * .2f, new Vector3(.16f, .04f, .5f - k * .04f), mf.r);
            var crank = mf.P(-W / 2f - .4f, 1.4f, 0); z.mk.Box(iron, crank, new Vector3(.4f, .06f, .06f), mf.r); z.mk.Box(woodD, crank + mf.Right * -.2f + mf.Fwd * .1f, new Vector3(.06f, .06f, .3f), mf.r);
            string[] turn = { "You turn the crank. The wheel rolls, the marbles climb, and the little xylophone plays a tune all by itself.", "Clack, clack, ding. The marbles race each other down.", "The whole machine wakes up and plays like it's been waiting for you." }; var tn = 0;
            Spot("The marble machine's crank", crank, "Turn the crank", new Vector3(1f, 1f, 1f)).onUse = _ => { life.cranked = Time.time; life.played = Time.time; Say(tlife.transform, crank + up * 1.4f, turn[tn++ % turn.Length], 5f); };
            halos.Add(mf.P(0, 2.5f, 1.4f), 4f, Gold, .2f);
            Folk(z.At(4f, 4f), -fr, Resident.Pose.Stand, new[] { "Two thousand marbles. I've counted. Twice.", "Turn the crank. It plays itself after that.", "It was built in a garden shed. Of course it was." }, Local());
            z.area.glowLight = ArtistBits.PointLight(z.root, mf.P(0, 3f, 3f), Gold, 18f, 1.4f);
            Shot(z.ctx, "the marble machine", z.At(Foot(z.a) * 1f, 3f) + up * 2f, mf.P(0, 2.4f, 0));
        }

        // ---------------------------------------------------------------- everyone else
        void Others(Plot z, PlaceFrame f)
        {
            var up = Vector3.up; var rot = z.Rot; var rng = new System.Random(z.a.name.Length * 7 + z.a.name[0]); var c = z.At(1f, -1f) + up * .1f; var fr = z.front; var sd = z.side;
            void Post(Vector3 at, float h, float r = .04f) { z.mk.Prism(woodD, at - up * .2f, up, r, h + .2f, 5); ArtistBits.PostCollider(z.root, at, h, r + .03f); }
            string Any() => SongIds.TryGetValue(z.a.name, out var l) && l.Length > 0 ? Song(z, l[0].title) : null;
            switch (z.a.name)
            {
                case "Atoms For Peace": { var dr = Song(z, "Dropped"); var ing = Song(z, "Ingenue"); Post(c, 2.6f, .04f); var am = c + up * 2.2f; z.mk.Blob(SongGlow(ing, Gold, 1.4f), am, Vector3.one * .14f, rng, 0f); for (var r = 0; r < 3; r++) for (var k = 0; k < 10; k++) z.mk.Box(Paint(new Color(.8f, .76f, .66f)), am + Quaternion.AngleAxis(k * 36f, Quaternion.Euler(r * 60f, r * 40f, 0) * up) * (Quaternion.Euler(r * 60f, r * 40f, 0) * Vector3.forward) * .6f, Vector3.one * .05f, Quaternion.identity); var tr = c + sd * 1.6f; z.mk.Box(Paint(new Color(.7f, .7f, .74f)), tr + up * .03f, new Vector3(.8f, .02f, .5f), rot * Quaternion.Euler(0, 20f, 4f)); z.mk.Box(SongGlow(dr, Rose, 1f), tr + up * .1f + fr * .5f, new Vector3(.2f, .1f, .14f), rot * Quaternion.Euler(30f, 50f, 0)); break; }
                case "BLARF": { var s = Any(); life.starSong = s; foreach (var e in new[] { -1.2f, 1.2f }) Post(c + sd * e, 2.6f, .04f); z.mk.Box(woodD, c + up * 2.6f, new Vector3(2.4f, .04f, .04f), Quaternion.LookRotation(sd)); for (var k = 0; k < 6; k++) { var sp = c + sd * (-1f + k * .4f) + up * (1.6f + (k % 3) * .3f); z.mk.Prism(Paint(Cream), sp, up, .005f, 2.6f - (1.6f + (k % 3) * .3f), 3); for (var j = 0; j < 5; j++) { var a = Quaternion.AngleAxis(j * 72f, fr) * up; z.mk.Box(k == 0 ? SongGlow(s, new Color(.3f, .3f, .5f), .8f) : Paint(new Color(.2f, .2f, .3f)), sp + a * .1f, new Vector3(.06f, .2f, .02f), Quaternion.LookRotation(fr, a)); } } break; }
                case "Burka Band": { var s = Any(); foreach (var e in new[] { -1.2f, 1.2f }) Post(c + sd * e, 2f, .04f); z.mk.Box(Paint(Cream), c + up * 2f, new Vector3(2.4f, .02f, .02f), Quaternion.LookRotation(sd)); z.mk.Box(SongGlow(s, new Color(.3f, .5f, 1f), .7f), c + up * 1.3f, new Vector3(1.2f, 1.4f, .02f), rot * Quaternion.Euler(0, 0, 4f)); break; }
                case "Caribou": { var s = Any(); life.silverSong = s; z.mk.Prism(trunkM, c, up, .2f, 2.4f, 6, .6f); for (var k = 0; k < 5; k++) z.mk.Blob(leafM, c + up * 2.8f + Quaternion.Euler(0, k * 72f, 0) * fr * 1f, new Vector3(1f, .8f, 1f), rng, .3f); ArtistBits.PostCollider(z.root, c, 2.4f, .3f); var dm = new MeshKit(); dm.Prism(SongGlow(s, new Color(.86f, .88f, .94f), 1.2f), Vector3.zero, Vector3.forward, .5f, .03f, 20); var dg = dm.BuildSingle("Silver disc", z.root, false); dg.transform.position = c + up * 2f + fr * 1.2f; life.spinners.Add((dg.transform, c + up * 2f + fr * 1.2f)); break; }
                case "DJ Plead": { var s = Any(); Post(c, 2.2f, .06f); z.mk.Prism(Paint(new Color(.6f, .6f, .56f)), c + up * 2.2f, fr, .1f, .8f, 10, 5f); z.mk.Prism(SongGlow(s, Gold, 1f), c + up * 2.2f + fr * .8f, fr, .5f, .01f, 14); break; }
                case "Dolphin Machine": { var s = Any(); life.gradSong = s; var sc = c + up * 1.8f; var cols = new[] { new Color(1f, .4f, .4f), new Color(1f, .6f, .3f), new Color(1f, .8f, .3f), new Color(1f, .9f, .6f) }; for (var k = 0; k < 4; k++) z.mk.Prism(k == 3 ? SongGlow(s, cols[k], 1.2f) : GlowM(cols[k], .9f), sc + fr * k * .02f, fr, 1f - k * .2f, .02f, 22); Post(c, 1.2f, .05f); break; }
                case "Four Tet": { var s = Any(); for (var k = 0; k < 6; k++) Crate(z.root, z.mk, c + sd * (k % 3 - 1) * .44f + up * (k / 3) * .42f, fr, rng); z.mk.Prism(woodD, c + up * .84f, up, .03f, .5f, 4); z.mk.Prism(SongGlow(s, Gold, 1.4f), c + up * 1.4f, up, .14f, .2f, 8, .6f); break; }
                case "The Avalanches": { var etoh = Song(z, "ETOH"); var since = Song(z, "Since I Left You"); life.sinceSong = since; for (var k = 0; k < 12; k++) z.mk.Prism(vinylM, c + up * (.02f + k * .03f) + sd * Mathf.Sin(k) * .1f, up, .3f, .02f, 16); z.mk.Prism(SongGlow(etoh, Rose, 1f), c + up * .4f, up, .1f, .01f, 10); var pm = c - fr * 1.2f + sd * 1.2f; z.mk.Prism(trunkM, pm, (up + sd * .2f).normalized, .12f, 3f, 6); for (var k = 0; k < 6; k++) { var fd = Quaternion.Euler(0, k * 60f, 0) * fr; z.mk.Tri(leafM, pm + up * 3f + sd * .6f, pm + up * 2.4f + sd * .6f + fd * 1.6f + Vector3.Cross(up, fd) * .3f, pm + up * 2.4f + sd * .6f + fd * 1.6f - Vector3.Cross(up, fd) * .3f, pm); } ArtistBits.PostCollider(z.root, pm, 3f, .2f); var sc = c + sd * 1.6f + fr * .6f; z.mk.Box(SongGlow(since, new Color(.4f, .8f, .9f), .6f), sc + up * .3f, new Vector3(.8f, .6f, .3f), rot * Quaternion.Euler(0, 20f, 0)); z.mk.Box(woodD, sc + up * .66f, new Vector3(.3f, .08f, .06f), rot * Quaternion.Euler(0, 20f, 0)); BoxCol(z.root, sc + up * .3f, new Vector3(.8f, .6f, .4f), rot); break; }
                case "The Books": { var s = Any(); life.tokyoSong = s; for (var k = 0; k < 6; k++) z.mk.Box(Paint(Color.HSVToRGB(k * .15f % 1f, .4f, .6f)), c + up * (.1f + k * .14f), new Vector3(.8f - k * .04f, .13f, .6f), rot * Quaternion.Euler(0, k * 9f, 0)); z.mk.Box(Paint(new Color(.4f, .4f, .4f)), c + up * 1.06f, new Vector3(.6f, .2f, .4f), rot); foreach (var e in new[] { -.15f, .15f }) z.mk.Prism(SongGlow(e < 0 ? s : null, new Color(1f, .4f, .7f), 1f), c + up * 1.17f + sd * e, up, .1f, .02f, 10); BoxCol(z.root, c + up * .6f, new Vector3(.9f, 1.2f, .7f), rot); break; }
                case "Thom Yorke": { var s = Any(); life.damageSong = s; z.mk.Box(woodD, c + up * .4f, new Vector3(.7f, .8f, .7f), rot); z.mk.Prism(Paint(new Color(.8f, .6f, .3f)), c + up * .9f, (up + fr).normalized, .05f, .8f, 8, 10f); z.mk.Prism(SongGlow(s, Gold, 1f), c + up * .9f + (up + fr).normalized * .8f, (up + fr).normalized, .5f, .01f, 14); BoxCol(z.root, c + up * .4f, new Vector3(.7f, .8f, .7f), rot); break; }
                case "alyzea": { var s = Any(); z.mk.Prism(brick, c, up, .9f, .5f, 14); z.mk.Prism(GlowM(new Color(.6f, .86f, .9f), .6f), c + up * .5f, up, .8f, .02f, 14); z.mk.Prism(glassM, c + up * .5f, up, .14f, 1.2f, 8); z.mk.Prism(SongGlow(s, new Color(.6f, .86f, 1f), 1.2f), c + up * 1.7f, up, .4f, .1f, 12); BoxCol(z.root, c + up * .3f, new Vector3(1.8f, .6f, 1.8f), rot); life.fountains.Add(c + up * 1.8f); break; }
                case "tictacto": { var s = Any(); Post(c, 1.6f, .05f); var bh = c + up * 1.9f; z.mk.Box(Paint(new Color(.6f, .7f, .8f)), bh, new Vector3(.4f, .4f, .4f), rot); z.mk.Prism(Paint(Terracotta), bh + up * .2f, up, .34f, .3f, 4, .05f, 45f); z.mk.Prism(dark, bh + fr * .21f, fr, .06f, .01f, 10); z.mk.Box(SongGlow(s, Gold, 1.2f), bh + fr * .21f - up * .12f, new Vector3(.1f, .06f, .02f), rot); break; }
                default: { var s = Any(); Post(c, 1.2f); z.mk.Prism(SongGlow(s, Gold, 1.4f), c + up * 1.2f, up, .12f, .2f, 8); break; }
            }
        }
    }
}
