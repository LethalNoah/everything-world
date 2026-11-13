using System.Collections.Generic;
using UnityEngine;
using static EverythingWorld.World.PlaceTools;

namespace EverythingWorld.World
{
    /// The IDM Glitchlands' artists. Each has a square pad of dark tiles with a glowing grid, their name floating over it
    /// in terminal green inside a wireframe frame, and one thing from their music; covers on a black panel with a green
    /// edge.
    /// - Mr. Bill: a sprawling workstation of monitors on a long desk with a chair to sit in, a giant coffee mug
    ///   ("Caffeine"), a planet ("Corot-7b"), a ladder climbing up into nothing ("Ladder Anxiety"), a scatter of debris
    ///   ("Ejecta"), a glowing brain ("Cryptomnesia"), a flag ("IDM Anthem") and a floating block for every other song.
    /// - The rest: a block of lava capped with snow, a wireframe bicycle, a circuit board slab, a glowing banana and
    ///   arrows, a pixel desert with a cactus, a return key, a green cloud raining, a speech bubble, a red car and a bass
    ///   guitar, a broken stair, a black cat in a grey cloud, a cube of water, a pulsing blob, and glowing roots.
    public partial class GlitchKit
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
                Add("Lorn", "Acid Rain", "Lorn"); Add("Ametsub", "Snowy Lava", "Ametsub"); Add("Woulg", "The Water", "Woulg"); Add("Venetian Snares", "Öngyilkos", "Venetian Snares"); Add("Squarepusher", "My Red Hot Car", "Squarepusher");
                Add("Mr. Bill", "IDM Anthem", "Mr. Bill"); Add("Mr. Bill", "Corot-7b", "Mr. Bill"); Add("Mr. Bill", "Caffeine", "Mr. Bill");
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
            area.artistName = a.name; area.stationId = a.stationId; area.radius = a.r; area.glowColor = Term;
            var z = new Plot { ctx = ctx, root = root, labels = labels, mk = new MeshKit(), solid = new MeshKit(), pictures = new MeshKit(), a = a, c = ctx.OnGround(a.x, a.z), front = front, side = Vector3.Cross(Vector3.up, front), area = area };
            var reach = Foot(a); var up = Vector3.up; var R = reach * 1.5f;
            z.mk.Box(tile, z.c - up * .2f, new Vector3(R, .5f, R), z.Rot); for (var i = -3; i <= 3; i++) { z.mk.Box(gridM, z.c + up * .06f + z.side * i * R / 7f, new Vector3(.04f, .02f, R), z.Rot); z.mk.Box(gridM, z.c + up * .06f + z.front * i * R / 7f, new Vector3(R, .02f, .04f), z.Rot); }
            switch (a.name)
            {
                case "Mr. Bill": Bill(z); break;
                default: Others(z, Frame(z)); break;
            }
            SongLamps(z);
            Board(z, z.At(reach * .6f, -reach * .45f - 1.5f), front);
            ArtistBits.Plaque(z.mk, z.solid, labels, a, z.At(reach * .6f + 1.5f, reach * .45f + 1.5f), front, iron, dark, Term, 1.6f);
            Lamp(ctx, z.mk, z.At(reach * .5f, reach * .7f), false);
            if (z.area.glowLight == null) z.area.glowLight = ArtistBits.PointLight(root, z.c + up * 4f + front * 3f, new Color(.6f, 1f, .8f), HasStation(a) ? 24f : 14f, 1.3f);
            area.glowRenderers = new Renderer[0];
            z.mk.Build("Area", root, false);
            z.solid.BuildCollider("Walls", root);
            z.pictures.Build("Covers", root, false);
            noGrass.Add(new Vector3(z.c.x, reach * 1f, z.c.z));
            var mark = new GameObject("Shot: " + a.name + " up close").transform; mark.SetParent(ctx.Root, false);
            var cam = z.At(reach * .85f, reach * .3f) + up * 2f; var look = z.At(-reach * .3f, 0) + up * 1.8f - cam; mark.SetPositionAndRotation(cam, Quaternion.LookRotation(look.normalized));
        }

        /// The covers on a black panel with a green edge.
        void Board(Plot z, Vector3 ground, Vector3 dir)
        {
            dir = Vector3.ProjectOnPlane(dir, Vector3.up).normalized; var rot = Quaternion.LookRotation(dir);
            var n = Mathf.Max(1, Mathf.Min(7, z.a.songList?.Length ?? 1)); const float size = .9f; var w = n * size * 1.25f + .6f;
            var c = ground + Vector3.up * 1.7f;
            z.mk.Box(gridM, c - Vector3.up * .5f - dir * .08f, new Vector3(w + .4f, size + 1.9f, .14f), rot); z.mk.Box(dark, c - Vector3.up * .5f - dir * .02f, new Vector3(w + .3f, size + 1.8f, .2f), rot); z.solid.Box(dark, c - Vector3.up * .5f, new Vector3(w + .4f, size + 1.9f, .3f), rot);
            z.mk.Box(Paint(new Color(.86f, .9f, .9f)), c - Vector3.up * .1f + dir * .09f, new Vector3(w, size + .9f, .04f), rot);
            ArtistBits.CoverWall(z.pictures, z.labels, z.ctx, z.a, c + dir * .12f + Vector3.up * .08f, dir, n, size, n, true, Ink);
        }

        /// A small floating block for each song without its own piece.
        void SongLamps(Plot z)
        {
            if (!SongIds.TryGetValue(z.a.name, out var list)) return;
            var up = Vector3.up; var k = 0; var seen = new HashSet<string>();
            foreach (var (_, id) in list)
            {
                if (z.lit.Contains(id) || !seen.Add(id)) continue;
                var p = z.At(Foot(z.a) * .55f - (k % 3) * 1.2f, Foot(z.a) * .15f + (k / 3) * 1.2f);
                z.mk.Box(dark, p + up * .5f, new Vector3(.14f, 1f, .14f), z.Rot); z.mk.Box(SongGlow(id, Sort[k % Sort.Length], 1.4f), p + up * 1.4f, Vector3.one * .36f, z.Rot * Quaternion.Euler(0, 45f, 0)); ArtistBits.PostCollider(z.root, p, 1.2f, .12f);
                k++;
            }
        }

        /// Their name floating in terminal green inside a wireframe frame.
        PlaceFrame Frame(Plot z)
        {
            var up = Vector3.up; var f = new PlaceFrame(z.At(-Foot(z.a) * .5f, 0), z.front);
            foreach (var e in new[] { -1.3f, 1.3f }) { z.mk.Box(gridM, f.P(e, 1.5f, 0), new Vector3(.06f, 3f, .06f), f.r); ArtistBits.PostCollider(z.root, f.P(e, 0, 0), 3f, .1f); } foreach (var h in new[] { 1.3f, 3f }) z.mk.Box(gridM, f.P(0, h, 0), new Vector3(2.6f, .06f, .06f), f.r); z.mk.Box(dark, f.P(0, 2.15f, -.02f), new Vector3(2.5f, 1.6f, .02f), f.r);
            ArtistBits.Label(z.labels, z.a.name, f.P(0, 2.4f, .02f), f.Fwd, .2f, Term, TextAnchor.MiddleCenter); ArtistBits.Label(z.labels, ">_", f.P(-.9f, 1.7f, .02f), f.Fwd, .16f, Term, TextAnchor.MiddleCenter);
            return f;
        }

        // ---------------------------------------------------------------- Mr. Bill: the workstation
        void Bill(Plot z)
        {
            var up = Vector3.up; var rng = new System.Random(2016); var fr = z.front; var sd = z.side; var rot = z.Rot;
            var caf = Song(z, "Caffeine"); var corot = Song(z, "Corot-7b"); var ladder = Song(z, "Ladder Anxiety"); var ejecta = Song(z, "Ejecta"); var crypt = Song(z, "Cryptomnesia"); var anthem = Song(z, "IDM Anthem"); life.cafSong = caf; life.corotSong = corot; life.anthemSong = anthem;
            // the workstation: a long desk of monitors with a chair
            var dk = new PlaceFrame(z.At(-5f, 0), fr); z.mk.Box(dark, dk.P(0, .8f, 0), new Vector3(8f, .1f, 1.6f), dk.r); foreach (var e in new[] { -3.8f, 3.8f }) z.mk.Box(dark, dk.P(e, .4f, 0), new Vector3(.1f, .8f, 1.4f), dk.r); BoxCol(z.root, dk.P(0, .45f, 0), new Vector3(8f, .9f, 1.6f), dk.r);
            var scr = new Material(WorldMaterials.Glow(Term, 1.2f)) { name = "Glitch monitors" }; life.monitors = scr; for (var k = 0; k < 7; k++) { var mp = dk.P(-3f + k * 1f, 1.5f + (k % 2) * .9f, -.4f); z.mk.Box(dark, mp, new Vector3(.94f, .7f, .08f), dk.r * Quaternion.Euler(0, (k - 3) * -6f, 0)); z.mk.Box(k == 3 ? SongGlow(crypt, Term, 1.2f) : scr, mp + dk.Fwd * .05f, new Vector3(.84f, .6f, .02f), dk.r * Quaternion.Euler(0, (k - 3) * -6f, 0)); }
            for (var k = 0; k < 3; k++) z.mk.Box(GlowM(Sort[k], .9f), dk.P(-1f + k * 1f, .86f, .4f), new Vector3(.8f, .04f, .3f), dk.r);
            var ch = dk.P(0, 0, 1.6f); z.mk.Prism(dark, ch, up, .04f, .45f, 5); z.mk.Box(dark, ch + up * .5f, new Vector3(.6f, .1f, .6f), dk.r); z.mk.Box(dark, ch + up * .95f + dk.Fwd * .3f, new Vector3(.6f, .8f, .1f), dk.r); Interact.Seat(tlife.transform, ch + up * .55f, -dk.Fwd, "Sit at the workstation").seatHeight = .55f;
            // "Caffeine": a giant coffee mug; "Corot-7b": a planet
            var mg = z.At(3f, -7f); z.mk.Prism(white, mg, up, 1f, 1.8f, 16); z.mk.Prism(Paint(new Color(.24f, .14f, .1f)), mg + up * 1.78f, up, .9f, .02f, 16); for (var k = 0; k <= 8; k++) { var a = Mathf.PI * k / 8f; z.mk.Box(white, mg + sd * 1f + sd * Mathf.Sin(a) * .5f + up * (.9f + Mathf.Cos(a) * .5f), new Vector3(.16f, .2f, .2f), rot); } z.mk.Box(SongGlow(caf, Amber, 1.4f), mg + up * 2.2f, new Vector3(.1f, .6f, .1f), rot * Quaternion.Euler(0, 0, 15f)); ArtistBits.PostCollider(z.root, mg, 1.8f, 1.1f);
            var pl = new MeshKit(); pl.Blob(SongGlow(corot, new Color(1f, .5f, .2f), 1f), Vector3.zero, Vector3.one * 1.2f, rng, 0f); pl.Prism(GlowM(new Color(1f, .7f, .4f), .6f), Vector3.zero, (Vector3.up + Vector3.right * .3f).normalized, 2f, .04f, 28); var pg = pl.BuildSingle("Corot-7b", z.root, false); pg.transform.position = z.At(-2f, 8f) + up * 6f; life.spinners.Add((pg.transform, z.At(-2f, 8f) + up * 6f));
            // "Ladder Anxiety": a ladder into nothing; "Ejecta": debris; "IDM Anthem": a flag
            var ld = z.At(4f, 6f); foreach (var e in new[] { -.35f, .35f }) z.mk.Box(iron, ld + up * 4f + sd * e, new Vector3(.06f, 8f, .06f), rot); for (var k = 0; k < 16; k++) z.mk.Box(k == 15 ? SongGlow(ladder, Red, 1.4f) : iron, ld + up * (.4f + k * .5f), new Vector3(.7f, .05f, .05f), rot); ArtistBits.PostCollider(z.root, ld, 8f, .4f);
            for (var k = 0; k < 12; k++) { var dp = z.At(6f + (k % 4) * .8f, -2f + (k / 4) * 1.2f); z.mk.Box(k == 0 ? SongGlow(ejecta, Red, 1.2f) : GlowM(Sort[k % Sort.Length], .7f), dp + up * (.15f + (k % 3) * .1f), Vector3.one * (.2f + (k % 3) * .15f), Quaternion.Euler(k * 30f, k * 50f, k * 20f)); }
            var fp = z.At(-8f, -8f); z.mk.Prism(iron, fp, up, .05f, 6f, 5); z.mk.Box(SongGlow(anthem, White, 1f), fp + up * 5.3f + sd * .9f, new Vector3(1.8f, 1.2f, .03f), rot); ArtistBits.PostCollider(z.root, fp, 6f, .1f);
            Folk(z.At(2f, 3f), -fr, Resident.Pose.Stand, new[] { "Thirteen tabs open. All of them errors. All of them music.", "Don't touch the ladder. Nobody knows where it goes.", "Have a coffee. The mug's three metres tall. You'll need a straw." }, Local());
            z.area.glowLight = ArtistBits.PointLight(z.root, z.c + up * 5f, new Color(.6f, 1f, .8f), 28f, 1.6f);
            Shot(z.ctx, "mr. bill's workstation", z.At(Foot(z.a) * 1.1f, 4f) + up * 2.4f, dk.P(0, 1.6f, 0));
        }

        // ---------------------------------------------------------------- everyone else
        void Others(Plot z, PlaceFrame f)
        {
            var up = Vector3.up; var rot = z.Rot; var rng = new System.Random(z.a.name.Length * 7 + z.a.name[0]); var c = z.At(1f, -1f) + up * .06f; var fr = z.front; var sd = z.side;
            void Post(Vector3 at, float h, float r = .04f) { z.mk.Prism(iron, at - up * .2f, up, r, h + .2f, 5); ArtistBits.PostCollider(z.root, at, h, r + .03f); }
            string Any() => SongIds.TryGetValue(z.a.name, out var l) && l.Length > 0 ? Song(z, l[0].title) : null;
            switch (z.a.name)
            {
                case "Ametsub": { var s = Any(); life.lavaSong = s; z.mk.Box(SongGlow(s, new Color(1f, .4f, .1f), 1.2f), c + up * .7f, new Vector3(1.4f, 1.4f, 1.4f), rot); z.mk.Box(white, c + up * 1.5f, new Vector3(1.5f, .2f, 1.5f), rot); BoxCol(z.root, c + up * .8f, new Vector3(1.5f, 1.6f, 1.5f), rot); break; }
                case "Autechre": { var bike = Song(z, "Bike"); var eu = Song(z, "Eutow"); var bm = GlowM(Term, 1.2f); foreach (var q in new[] { -.6f, .6f }) for (var k = 0; k < 12; k++) { var a0 = k * Mathf.PI / 6f; var a1 = (k + 1) * Mathf.PI / 6f; var p0 = c + up * .5f + fr * q + (fr * Mathf.Cos(a0) + up * Mathf.Sin(a0)) * .45f; var p1 = c + up * .5f + fr * q + (fr * Mathf.Cos(a1) + up * Mathf.Sin(a1)) * .45f; z.mk.Box(k == 0 && q > 0 ? SongGlow(bike, Term, 1.4f) : bm, (p0 + p1) / 2f, new Vector3(.04f, .04f, (p1 - p0).magnitude), Quaternion.LookRotation(p1 - p0)); } z.mk.Box(bm, c + up * .8f, new Vector3(.04f, .04f, 1.2f), rot); z.mk.Box(bm, c + up * 1f + fr * .5f, new Vector3(.4f, .04f, .04f), rot); ArtistBits.PostCollider(z.root, c, 1f, .7f); var sh = c + sd * 1.8f + up * 1.2f; for (var k = 0; k < 5; k++) z.mk.Box(k == 0 ? SongGlow(eu, Cyan, 1.2f) : GlowM(Cyan, .8f), sh + Quaternion.Euler(k * 40f, k * 70f, 0) * Vector3.forward * .4f, new Vector3(.6f, .06f, .2f), Quaternion.Euler(k * 40f, k * 70f, k * 10f)); break; }
                case "Circuit Bent": { var s = Any(); var cb = new PlaceFrame(c, fr); z.mk.Box(Paint(new Color(.1f, .4f, .2f)), cb.P(0, 1.2f, 0), new Vector3(2f, 2.4f, .1f), cb.r * Quaternion.Euler(-10f, 0, 0)); BoxCol(z.root, cb.P(0, 1.2f, 0), new Vector3(2f, 2.4f, .3f), cb.r); for (var k = 0; k < 10; k++) z.mk.Box(k == 0 ? SongGlow(s, Amber, 1.4f) : GlowM(Amber, .9f), cb.P(-.8f + (k % 5) * .4f, .5f + (k / 5) * 1.1f + (k % 2) * .3f, .07f), new Vector3(.04f, .5f, .01f), cb.r * Quaternion.Euler(-10f, 0, (k % 3) * 90f)); for (var k = 0; k < 4; k++) z.mk.Box(dark, cb.P(-.6f + k * .4f, 1.3f, .1f), new Vector3(.24f, .14f, .08f), cb.r * Quaternion.Euler(-10f, 0, 0)); break; }
                case "Iglooghost": { var nv = Song(z, "New Vectors"); var xj = Song(z, "Xiangjiao"); var bn = c + up * 1.2f; for (var k = 0; k < 8; k++) { var a = (k - 3.5f) * .18f; z.mk.Box(k == 4 ? SongGlow(xj, new Color(1f, .9f, .3f), 1.2f) : GlowM(new Color(1f, .86f, .2f), .9f), bn + sd * Mathf.Sin(a) * 1.2f + up * (Mathf.Cos(a) * 1.2f - 1.2f), new Vector3(.36f, .34f, .3f), rot * Quaternion.Euler(0, 0, -a * Mathf.Rad2Deg)); } Post(c, .6f, .05f); for (var k = 0; k < 4; k++) { var ap = c + sd * 2f + up * (.6f + k * .5f); z.mk.Tri(k == 0 ? SongGlow(nv, Cyan, 1.4f) : GlowM(Cyan, 1f), ap + fr * .4f, ap - fr * .2f + sd * .3f, ap - fr * .2f - sd * .3f, ap - up); } break; }
                case "Igorrr": { var s = Any(); for (var k = 0; k < 20; k++) z.mk.Box(Paint(new Color(.86f, .7f, .4f)), c + sd * ((k % 5) - 2f) * .4f + fr * ((k / 5) - 1.5f) * .4f + up * .1f * ((k * 7) % 3), new Vector3(.4f, .2f + ((k * 7) % 3) * .2f, .4f), rot); var cc = c + up * .4f; z.mk.Box(SongGlow(s, new Color(.4f, .8f, .3f), .9f), cc + up * .8f, new Vector3(.3f, 1.6f, .3f), rot); z.mk.Box(GlowM(new Color(.4f, .8f, .3f), .7f), cc + up * 1f + sd * .3f, new Vector3(.3f, .2f, .2f), rot); z.mk.Box(GlowM(new Color(.4f, .8f, .3f), .7f), cc + up * 1.2f + sd * .45f, new Vector3(.2f, .5f, .2f), rot); BoxCol(z.root, c + up * .8f, new Vector3(2f, 1.6f, 1.6f), rot); break; }
                case "Kyegi": { var s = Any(); var kp = c + up * .4f; z.mk.Box(white, kp, new Vector3(1.6f, .5f, 1.2f), rot); z.mk.Box(white, kp + fr * -.4f + sd * .3f + up * .0f, new Vector3(1f, .5f, 1.2f), rot); z.mk.Box(SongGlow(s, Term, 1.4f), kp + up * .26f, new Vector3(.8f, .02f, .1f), rot); z.mk.Box(SongGlow(s, Term, 1.4f), kp + up * .26f + sd * .4f + fr * .2f, new Vector3(.1f, .02f, .5f), rot); BoxCol(z.root, kp, new Vector3(1.6f, .5f, 1.6f), rot); break; }
                case "Lorn": { var s = Any(); life.acidSong = s; Post(c, 3f, .05f); for (var k = 0; k < 5; k++) z.mk.Blob(SongGlow(k == 0 ? s : null, new Color(.4f, 1f, .3f), .9f), c + up * 3.2f + sd * (k - 2f) * .5f, new Vector3(.6f, .4f, .5f), rng, .2f); for (var k = 0; k < 12; k++) z.mk.Box(GlowM(new Color(.4f, 1f, .3f), 1.4f), c + up * (1.2f + (k % 4) * .5f) + sd * ((k % 5) - 2f) * .35f + fr * ((k % 3) - 1f) * .2f, new Vector3(.02f, .3f, .02f), rot); break; }
                case "Memo Boy": { var s = Any(); var bb = new PlaceFrame(c + up * 1.8f, fr); z.mk.Box(SongGlow(s, new Color(.3f, .5f, 1f), .9f), bb.o, new Vector3(1.8f, 1f, .2f), bb.r); z.mk.Tri(GlowM(new Color(.3f, .5f, 1f), .9f), bb.P(-.4f, -.5f, 0), bb.P(-.1f, -.5f, 0), bb.P(-.6f, -.9f, 0), bb.P(0, 0, -1f)); for (var k = 0; k < 3; k++) z.mk.Box(white, bb.P(-.5f + k * .5f, 0, .11f), new Vector3(.2f, .2f, .02f), bb.r); Post(c, 1.2f, .05f); break; }
                case "Squarepusher": { var car = Song(z, "My Red Hot Car"); var pap = Song(z, "Papalon"); life.carSong = car; var cp = c; z.mk.Box(Paint(new Color(.86f, .14f, .14f)), cp + up * .45f, new Vector3(1.6f, .5f, 3f), rot); z.mk.Box(SongGlow(car, Red, .8f), cp + up * .9f - fr * .3f, new Vector3(1.4f, .4f, 1.4f), rot); foreach (var e in new[] { -.8f, .8f }) foreach (var q in new[] { -1f, 1f }) z.mk.Prism(dark, cp + up * .3f + sd * e + fr * q, sd * Mathf.Sign(e), .3f, .14f, 10); BoxCol(z.root, cp + up * .6f, new Vector3(1.6f, 1.2f, 3f), rot); var bs = c + sd * 2.2f; z.mk.Box(Paint(new Color(.2f, .2f, .24f)), bs + up * .8f, new Vector3(.4f, .6f, .1f), rot); z.mk.Box(SongGlow(pap, Cyan, 1f), bs + up * 1.6f, new Vector3(.08f, 1.4f, .06f), rot); ArtistBits.PostCollider(z.root, bs, 2.4f, .3f); break; }
                case "Tim Koch": { var s = Any(); for (var k = 0; k < 7; k++) { var sp = c - fr * (k * .5f - 1.2f) + up * (k * .3f + .15f) + sd * ((k * 37 % 3) - 1f) * .3f; z.mk.Box(k == 6 ? SongGlow(s, Red, 1.2f) : tile, sp, new Vector3(1f, .12f, .5f), rot * Quaternion.Euler((k % 2) * 8f, 0, (k % 3) * 6f)); } BoxCol(z.root, c + up * .9f, new Vector3(1.4f, 1.8f, 3.4f), rot); break; }
                case "Venetian Snares": { var s = Any(); life.gloomSong = s; var ct = c; z.mk.Box(Paint(new Color(.06f, .06f, .08f)), ct + up * .3f, new Vector3(.36f, .5f, .7f), rot); z.mk.Box(Paint(new Color(.06f, .06f, .08f)), ct + up * .7f + fr * .3f, new Vector3(.32f, .3f, .3f), rot); foreach (var e in new[] { -.1f, .1f }) { z.mk.Cone(Paint(new Color(.06f, .06f, .08f)), ct + up * .85f + fr * .3f + sd * e, .06f, .16f, 4); z.mk.Box(GlowM(Amber, 1.6f), ct + up * .74f + fr * .46f + sd * e, new Vector3(.05f, .04f, .02f), rot); } z.mk.Box(Paint(new Color(.06f, .06f, .08f)), ct + up * .5f - fr * .5f, new Vector3(.06f, .6f, .06f), rot * Quaternion.Euler(-30f, 0, 0)); BoxCol(z.root, ct + up * .4f, new Vector3(.5f, .8f, 1f), rot); for (var k = 0; k < 4; k++) z.mk.Blob(SongGlow(k == 0 ? s : null, new Color(.5f, .52f, .56f), .5f), ct + up * 2.6f + sd * (k - 1.5f) * .5f, new Vector3(.6f, .36f, .5f), rng, .2f); Post(ct - fr * 1f, 2.4f, .03f); break; }
                case "Woulg": { var s = Any(); life.waterSong = s; z.mk.Box(SongGlow(s, new Color(.3f, .6f, 1f), .8f), c + up * .8f, Vector3.one * 1.4f, rot); WireCube(z.mk, gridM, c + up * .8f, 1.46f, rot); BoxCol(z.root, c + up * .8f, Vector3.one * 1.4f, rot); break; }
                case "Zebbler Encanti Experience": { var s = Any(); var bm = new MeshKit(); bm.Blob(SongGlow(s, new Color(1f, .4f, .6f), 1f), Vector3.zero, new Vector3(.8f, .6f, .7f), rng, .4f); for (var k = 0; k < 6; k++) bm.Blob(GlowM(new Color(1f, .5f, .7f), .8f), Quaternion.Euler(k * 50f, k * 60f, 0) * Vector3.forward * .6f, Vector3.one * .26f, rng, .3f); var bg = bm.BuildSingle("Blob", z.root, false); bg.transform.position = c + up * 1f; life.blobs.Add(bg.transform); ArtistBits.PostCollider(z.root, c, 1.6f, .8f); break; }
                case "diaphane": { var s = Any(); for (var k = 0; k < 9; k++) { var a = c + Quaternion.Euler(0, k * 40f, 0) * fr * .3f; var b = c + Quaternion.Euler(0, k * 40f + 20f, 0) * fr * (1.4f + (k % 3) * .5f); z.mk.Box(k == 0 ? SongGlow(s, Term, 1.4f) : gridM, (a + b) / 2f + up * .05f, new Vector3(.06f, .06f, (b - a).magnitude), Quaternion.LookRotation(b - a)); for (var j = 0; j < 2; j++) { var bb = b + Quaternion.Euler(0, j * 50f - 25f, 0) * (b - a).normalized * .6f; z.mk.Box(gridM, (b + bb) / 2f + up * .05f, new Vector3(.04f, .04f, .6f), Quaternion.LookRotation(bb - b)); } } z.mk.Prism(Paint(new Color(.3f, .24f, .2f)), c, up, .2f, 1.2f, 6, .6f); ArtistBits.PostCollider(z.root, c, 1.2f, .3f); break; }
                default: { var s = Any(); z.mk.Box(dark, c + up * .5f, new Vector3(.2f, 1f, .2f), rot); z.mk.Box(SongGlow(s, Term, 1.4f), c + up * 1.4f, Vector3.one * .5f, rot * Quaternion.Euler(0, 45f, 0)); ArtistBits.PostCollider(z.root, c, 1.2f, .2f); break; }
            }
        }
    }
}
