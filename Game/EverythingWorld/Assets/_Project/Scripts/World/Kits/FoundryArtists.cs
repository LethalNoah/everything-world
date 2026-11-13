using System.Collections.Generic;
using UnityEngine;
using static EverythingWorld.World.PlaceTools;

namespace EverythingWorld.World
{
    /// The Electro Foundry's artists. Each has a square steel plate edged in hazard stripes, a neon sign on a steel post
    /// with their name, and one thing from their music, built in the works; covers on a steel board with a neon edge.
    /// - Farleon: a giant robot being assembled in a gantry ("Apollo"), an arcade cabinet ("Super Game") and a signpost
    ///   pointing a very long way ("Far From Home").
    /// - Insan3lik3: a wall of modular synth racks patched with glowing cables ("You Make Me Go"), a giant pitch wheel ("Bad
    ///   Pitched") and a tear-off calendar ("The Other Day").
    /// - meganeko: a neon stage with a wall of pixels, a giant soda can ("Astro Soda"), a blocky shark leaping ("Sharkatzor")
    ///   and a film camera on a crane ("Lights Camera Action").
    /// - The rest: a pixel atom, a globe on a heater, a ribbon that loops for ever, a neon halo, a giant neon 1 and 2, a tap
    ///   dripping light, a rabbit in neon, a neon heart.
    public partial class FoundryKit
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
                Add("she", "Atomic", "she"); Add("meganeko", "Sharkatzor", "meganeko"); Add("meganeko", "Astro Soda", "meganeko"); Add("Farleon", "Apollo", "Farleon"); Add("Farleon", "Super Game", "Farleon");
                Add("Bioblitz", "Globe Warmer", "Bioblitz"); Add("James Egbert", "Halo", "James Egbert"); Add("Rabbit Killer", "Imaginationland", "Rabbit Killer"); Add("Ryeland", "Luvu", "Ryeland");
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
            if (steel == null) MakeMaterials();
            var root = new GameObject("Artist: " + a.name).transform; root.SetParent(ctx.Root, false);
            var front = facing.TryGetValue(a.name, out var f) ? f : new Vector3(center.x - a.x, 0, center.z - a.z).normalized;
            if (front.sqrMagnitude < .01f) front = Vector3.forward;
            var labels = new GameObject("Labels").transform; labels.SetParent(root, false);
            var area = root.gameObject.AddComponent<ArtistArea>();
            area.artistName = a.name; area.stationId = a.stationId; area.radius = a.r; area.glowColor = Green;
            var z = new Plot { ctx = ctx, root = root, labels = labels, mk = new MeshKit(), solid = new MeshKit(), pictures = new MeshKit(), a = a, c = ctx.OnGround(a.x, a.z), front = front, side = Vector3.Cross(Vector3.up, front), area = area };
            var reach = Foot(a); var up = Vector3.up; var R = reach * 1.5f;
            z.mk.Box(plate, z.c - up * .2f, new Vector3(R, .5f, R), z.Rot); for (var k = 0; k < (int)(R / 1f); k++) foreach (var e in new[] { -1f, 1f }) { z.mk.Box(k % 2 == 0 ? hazard : dark, z.c + up * .06f + z.side * e * (R / 2f - .2f) + z.front * (-R / 2f + .5f + k), new Vector3(.3f, .02f, 1f), z.Rot); z.mk.Box(k % 2 == 0 ? hazard : dark, z.c + up * .06f + z.front * e * (R / 2f - .2f) + z.side * (-R / 2f + .5f + k), new Vector3(1f, .02f, .3f), z.Rot); }
            switch (a.name)
            {
                case "Farleon": Robot(z); break;
                case "Insan3lik3": Synths(z); break;
                case "meganeko": Neon(z); break;
                default: Others(z, NeonSign(z)); break;
            }
            SongLamps(z);
            Board(z, z.At(reach * .6f, -reach * .45f - 1.5f), front);
            ArtistBits.Plaque(z.mk, z.solid, labels, a, z.At(reach * .6f + 1.5f, reach * .45f + 1.5f), front, steelD, dark, Green, 1.6f);
            Lamp(ctx, z.mk, z.At(reach * .5f, reach * .7f), false);
            if (z.area.glowLight == null) z.area.glowLight = ArtistBits.PointLight(root, z.c + up * 4f + front * 3f, a.name.Length % 2 == 0 ? Green : Blue, HasStation(a) ? 24f : 14f, 1.5f);
            area.glowRenderers = new Renderer[0];
            z.mk.Build("Area", root, false);
            z.solid.BuildCollider("Walls", root);
            z.pictures.Build("Covers", root, false);
            noGrass.Add(new Vector3(z.c.x, reach * 1f, z.c.z));
            var mark = new GameObject("Shot: " + a.name + " up close").transform; mark.SetParent(ctx.Root, false);
            var cam = z.At(reach * .85f, reach * .3f) + up * 2f; var look = z.At(-reach * .3f, 0) + up * 1.8f - cam; mark.SetPositionAndRotation(cam, Quaternion.LookRotation(look.normalized));
        }

        /// The covers on a steel board with a neon edge.
        void Board(Plot z, Vector3 ground, Vector3 dir)
        {
            dir = Vector3.ProjectOnPlane(dir, Vector3.up).normalized; var rot = Quaternion.LookRotation(dir);
            var n = Mathf.Max(1, Mathf.Min(7, z.a.songList?.Length ?? 1)); const float size = .9f; var w = n * size * 1.25f + .6f;
            var c = ground + Vector3.up * 1.7f;
            z.mk.Box(z.a.name.Length % 2 == 0 ? greenM : blueM, c - Vector3.up * .5f - dir * .08f, new Vector3(w + .5f, size + 2f, .14f), rot); z.mk.Box(steelD, c - Vector3.up * .5f - dir * .02f, new Vector3(w + .3f, size + 1.8f, .2f), rot); z.solid.Box(steelD, c - Vector3.up * .5f, new Vector3(w + .5f, size + 2f, .3f), rot);
            z.mk.Box(Paint(new Color(.88f, .9f, .9f)), c - Vector3.up * .1f + dir * .09f, new Vector3(w, size + .9f, .04f), rot);
            ArtistBits.CoverWall(z.pictures, z.labels, z.ctx, z.a, c + dir * .12f + Vector3.up * .08f, dir, n, size, n, true, Ink);
        }

        /// A neon tube on a steel post for each song without its own piece.
        void SongLamps(Plot z)
        {
            if (!SongIds.TryGetValue(z.a.name, out var list)) return;
            var up = Vector3.up; var k = 0; var seen = new HashSet<string>();
            foreach (var (_, id) in list)
            {
                if (z.lit.Contains(id) || !seen.Add(id)) continue;
                var p = z.At(Foot(z.a) * .55f - (k % 2) * 1.3f, Foot(z.a) * .2f + (k / 2) * 1.3f);
                z.mk.Box(steelD, p + up * .5f, new Vector3(.12f, 1f, .12f), z.Rot); z.mk.Box(SongGlow(id, k % 2 == 0 ? Green : Blue, 1.6f), p + up * 1.4f, new Vector3(.1f, .8f, .1f), z.Rot); ArtistBits.PostCollider(z.root, p, 1.8f, .12f);
                k++;
            }
        }

        /// A neon sign on a steel post with their name.
        PlaceFrame NeonSign(Plot z)
        {
            var up = Vector3.up; var f = new PlaceFrame(z.At(-Foot(z.a) * .5f, 0), z.front); var col = z.a.name.Length % 2 == 0 ? greenM : blueM;
            z.mk.Box(steelD, f.P(0, 1.4f, 0), new Vector3(.2f, 2.8f, .2f), f.r); ArtistBits.PostCollider(z.root, f.o, 2.8f, .14f); z.mk.Box(dark, f.P(0, 2.6f, .1f), new Vector3(2.6f, .6f, .1f), f.r); z.mk.Box(col, f.P(0, 2.6f, .16f), new Vector3(2.6f, .06f, .02f), f.r); z.mk.Box(col, f.P(0, 2.32f, .16f), new Vector3(2.6f, .04f, .02f), f.r); ArtistBits.Label(z.labels, z.a.name, f.P(0, 2.6f, .17f), f.Fwd, .17f, z.a.name.Length % 2 == 0 ? Green : Blue, TextAnchor.MiddleCenter);
            return f;
        }

        // ---------------------------------------------------------------- Farleon: the robot
        void Robot(Plot z)
        {
            var up = Vector3.up; var rng = new System.Random(2014); var fr = z.front; var sd = z.side; var rot = z.Rot;
            var apollo = Song(z, "Apollo"); var game = Song(z, "Super Game"); var far = Song(z, "Far From Home"); life.apolloSong = apollo; life.gameSong = game;
            // a giant robot in an assembly gantry
            var rb = z.At(-3f, 0); var body = Paint(new Color(.7f, .72f, .76f)); foreach (var e in new[] { -.8f, .8f }) { z.mk.Box(body, rb + up * 1.4f + sd * e, new Vector3(.8f, 2.8f, .9f), rot); z.mk.Box(steelD, rb + up * .2f + sd * e + fr * .2f, new Vector3(1f, .4f, 1.4f), rot); } z.mk.Box(body, rb + up * 4f, new Vector3(2.8f, 2.6f, 1.6f), rot); z.mk.Box(SongGlow(apollo, new Color(1f, .6f, .2f), 1.4f), rb + up * 4.2f + fr * .81f, new Vector3(1f, 1f, .02f), rot); foreach (var e in new[] { -1.8f, 1.8f }) z.mk.Box(body, rb + up * 3.6f + sd * e, new Vector3(.7f, 2.4f, .7f), rot * Quaternion.Euler(0, 0, -Mathf.Sign(e) * 10f)); z.mk.Box(body, rb + up * 6f, new Vector3(1.4f, 1.2f, 1.2f), rot); foreach (var e in new[] { -.3f, .3f }) z.mk.Box(greenM, rb + up * 6.1f + fr * .61f + sd * e, new Vector3(.3f, .14f, .02f), rot); z.mk.Prism(steelD, rb + up * 6.6f, up, .04f, .8f, 4); z.mk.Blob(greenM, rb + up * 7.4f, Vector3.one * .12f, rng, 0f);
            z.solid.Box(body, rb + up * 3f, new Vector3(3f, 6f, 1.8f), rot); foreach (var e in new[] { -2.8f, 2.8f }) { z.mk.Box(hazard, rb + up * 4f + sd * e - fr * 1f, new Vector3(.3f, 8f, .3f), rot); ArtistBits.PostCollider(z.root, rb + sd * e - fr * 1f, 8f, .25f); } z.mk.Box(hazard, rb + up * 8f - fr * 1f, new Vector3(5.9f, .3f, .3f), rot); life.welds.Add(rb + up * 4f + sd * 1.4f + fr * .9f);
            // "Super Game": an arcade cabinet; "Far From Home": a signpost pointing a very long way
            var ac = z.At(3f, -6f); var af = -sd; var ar = Quaternion.LookRotation(af); z.mk.Box(Paint(new Color(.2f, .2f, .5f)), ac + up * .9f, new Vector3(.9f, 1.8f, .8f), ar); z.mk.Box(SongGlow(game, new Color(.4f, .9f, 1f), 1.2f), ac + up * 1.5f + af * .41f, new Vector3(.7f, .55f, .02f), ar); z.mk.Box(GlowM(Pink, 1.4f), ac + up * 1.95f + af * .41f, new Vector3(.8f, .2f, .02f), ar); BoxCol(z.root, ac + up * .9f, new Vector3(.9f, 1.8f, .8f), ar);
            var sp = z.At(4f, 6f); z.mk.Prism(steelD, sp, up, .06f, 2.6f, 5); for (var k = 0; k < 3; k++) { var dr = Quaternion.Euler(0, k * 50f - 50f, 0) * sd; z.mk.Box(k == 1 ? SongGlow(far, Hazard, 1f) : Paint(Hazard), sp + up * (1.6f + k * .4f) + dr * .5f, new Vector3(1.2f, .26f, .04f), Quaternion.LookRotation(Vector3.Cross(up, dr))); } ArtistBits.PostCollider(z.root, sp, 2.6f, .1f);
            Folk(z.At(5f, 0), -fr, Resident.Pose.Stand, new[] { "We're nearly done with him. We've been nearly done for years.", "When Apollo plays, his chest lights up. He likes it.", "There's a high score on that cabinet nobody can beat." }, Local());
            z.area.glowLight = ArtistBits.PointLight(z.root, rb + up * 5f + fr * 4f, new Color(1f, .7f, .4f), 24f, 1.8f);
            Shot(z.ctx, "farleon's robot", z.At(Foot(z.a) * 1.1f, 3f) + up * 2.4f, rb + up * 4f);
        }

        // ---------------------------------------------------------------- Insan3lik3: the synth wall
        void Synths(Plot z)
        {
            var up = Vector3.up; var rng = new System.Random(2013); var fr = z.front; var sd = z.side; var rot = z.Rot;
            var bad = Song(z, "Bad Pitched"); var other = Song(z, "The Other Day"); var go = Song(z, "You Make Me Go"); life.goSong = go;
            var wf = new PlaceFrame(z.At(-5f, 0), fr); z.mk.Box(dark, wf.P(0, 2.4f, 0), new Vector3(7f, 4.8f, .8f), wf.r); z.solid.Box(dark, wf.P(0, 2.4f, 0), new Vector3(7f, 4.8f, .8f), wf.r);
            var pat = new Material(WorldMaterials.Glow(Green, 1.2f)) { name = "Foundry patch lights" }; life.patch = pat; for (var x = 0; x < 8; x++) for (var y = 0; y < 5; y++) { var mp = wf.P(-3.1f + x * .88f, .8f + y * .9f, .41f); z.mk.Box(steelD, mp, new Vector3(.8f, .8f, .02f), wf.r); for (var k = 0; k < 3; k++) z.mk.Prism(Paint(new Color(.8f, .8f, .82f)), mp + wf.Fwd * .02f + wf.Right * (-.24f + k * .24f) + up * .2f, wf.Fwd, .07f, .04f, 8); z.mk.Blob((x + y) % 3 == 0 ? pat : GlowM((x + y) % 2 == 0 ? Blue : Pink, 1.2f), mp + wf.Fwd * .03f - up * .2f + wf.Right * ((x % 3) * .2f - .2f), Vector3.one * .04f, rng, 0f); }
            var cols = new[] { Green, Blue, Pink, Hazard }; for (var k = 0; k < 9; k++) { var a = wf.P(-3f + (k * 37 % 8) * .88f, .9f + (k * 13 % 5) * .9f, .45f); var b = wf.P(-3f + (k * 23 % 8) * .88f, .9f + (k * 7 % 5) * .9f, .45f); var mid = (a + b) / 2f + wf.Fwd * .4f - up * .5f; foreach (var (p0, p1) in new[] { (a, mid), (mid, b) }) z.mk.Box(k == 0 ? SongGlow(go, cols[k % 4], 1.4f) : GlowM(cols[k % 4], 1.2f), (p0 + p1) / 2f, new Vector3(.04f, .04f, (p1 - p0).magnitude), Quaternion.LookRotation(p1 - p0)); }
            var pw = z.At(3f, -5f); z.mk.Box(steelD, pw + up * .6f, new Vector3(.8f, 1.2f, .8f), rot); var wm = new MeshKit(); wm.Prism(SongGlow(bad, Pink, 1.2f), Vector3.left * .2f, Vector3.right, .6f, .4f, 20); wm.Box(steelD, Vector3.zero, new Vector3(.42f, .14f, .14f), Quaternion.identity); var wg = wm.BuildSingle("Pitch wheel", z.root, false); wg.transform.SetPositionAndRotation(pw + up * 1.8f, rot); life.pitch = wg.transform; BoxCol(z.root, pw + up * .9f, new Vector3(.8f, 1.8f, .8f), rot);
            var cl = new PlaceFrame(z.At(3f, 5f), fr); z.mk.Box(steelD, cl.P(0, .9f, 0), new Vector3(.1f, 1.8f, .1f), cl.r); z.mk.Box(white, cl.P(0, 2f, .06f), new Vector3(.9f, 1f, .04f), cl.r); z.mk.Box(SongGlow(other, new Color(1f, .3f, .3f), 1f), cl.P(0, 2.35f, .09f), new Vector3(.9f, .26f, .02f), cl.r); ArtistBits.Label(z.labels, "13", cl.P(0, 1.9f, .09f), cl.Fwd, .3f, Ink, TextAnchor.MiddleCenter); ArtistBits.PostCollider(z.root, cl.o, 2.4f, .4f);
            Folk(z.At(5f, 1f), -fr, Resident.Pose.Stand, new[] { "Every cable goes somewhere. Not always somewhere useful.", "Bend the pitch wheel. Everything sounds slightly wrong. Perfect.", "What day is it? The calendar's stuck on the other day." }, Local());
            z.area.glowLight = ArtistBits.PointLight(z.root, wf.P(0, 2.4f, 4f), Green, 22f, 1.8f);
        }

        // ---------------------------------------------------------------- meganeko: the neon stage
        void Neon(Plot z)
        {
            var up = Vector3.up; var rng = new System.Random(2017); var fr = z.front; var sd = z.side; var rot = z.Rot;
            var lights = Song(z, "Lights Camera"); var soda = Song(z, "Astro Soda"); var shark = Song(z, "Sharkatzor"); life.sharkSong = shark; life.sodaSong = soda; life.sodaAt = z.At(2f, -6f) + up * 3.4f;
            var st = new PlaceFrame(z.At(-5f, 0), fr); z.mk.Box(dark, st.P(0, .4f, -.4f), new Vector3(10f, .8f, 5f), st.r); z.solid.Box(dark, st.P(0, .4f, -.4f), new Vector3(10f, .8f, 5f), st.r);
            var px = new Material(WorldMaterials.Glow(Pink, 1.2f)) { name = "Foundry pixel wall" }; var px2 = new Material(WorldMaterials.Glow(Blue, 1.2f)) { name = "Foundry pixel wall 2" }; life.pixelWall = px; life.pixelWall2 = px2;
            z.mk.Box(dark, st.P(0, 4.2f, -2.9f), new Vector3(10f, 6.6f, .4f), st.r); z.solid.Box(dark, st.P(0, 4.2f, -2.9f), new Vector3(10f, 6.6f, .4f), st.r); for (var x = 0; x < 16; x++) for (var y = 0; y < 10; y++) { if (Mathf.PerlinNoise(x * .3f, y * .3f) < .42f) continue; z.mk.Box((x + y) % 3 == 0 ? px2 : px, st.P(-4.5f + x * .6f, 1.4f + y * .6f, -2.68f), new Vector3(.5f, .5f, .02f), st.r); }
            foreach (var e in new[] { -1f, 1f }) for (var k = 0; k < 3; k++) { var sp = st.P(e * 4.3f, 1.4f + k * 1.1f, -.8f); z.mk.Box(steelD, sp, new Vector3(1.2f, 1f, .9f), st.r); z.mk.Box(blueM, sp + st.Fwd * .46f, new Vector3(.3f, .3f, .02f), st.r); }
            // "Astro Soda": a giant soda can
            var can = z.At(2f, -6f); z.mk.Prism(Paint(new Color(.9f, .2f, .3f)), can, up, 1f, 3.2f, 14); z.mk.Prism(Paint(new Color(.8f, .8f, .84f)), can + up * 3.2f, up, .9f, .2f, 14); z.mk.Prism(SongGlow(soda, new Color(.3f, .9f, 1f), 1.2f), can + up * 1.2f, up, 1.02f, .8f, 14); z.mk.Box(Paint(new Color(.6f, .6f, .64f)), can + up * 3.42f + fr * .3f, new Vector3(.3f, .04f, .5f), rot); ArtistBits.PostCollider(z.root, can, 3.4f, 1f);
            // "Sharkatzor": a blocky shark leaping
            var sh = z.At(2f, 6f) + up * 1.6f; var shr = Quaternion.LookRotation(-sd) * Quaternion.Euler(-20f, 0, 0); var grey = Paint(new Color(.5f, .6f, .74f)); z.mk.Box(grey, sh, new Vector3(1.2f, 1.2f, 3f), shr); z.mk.Box(white, sh + shr * new Vector3(0, -.5f, .2f), new Vector3(1.1f, .3f, 2.4f), shr); z.mk.Box(grey, sh + shr * new Vector3(0, .9f, 0), new Vector3(.2f, .9f, .8f), shr); z.mk.Box(grey, sh + shr * new Vector3(0, .3f, -1.9f), new Vector3(.2f, 1.2f, .8f), shr); for (var k = 0; k < 4; k++) z.mk.Box(white, sh + shr * new Vector3(-.3f + k * .2f, -.3f, 1.51f), new Vector3(.1f, .14f, .02f), shr); z.mk.Box(SongGlow(shark, Pink, 1.6f), sh + shr * new Vector3(.61f, .2f, 1f), new Vector3(.02f, .2f, .2f), shr); z.mk.Box(steelD, sh - up * .9f, new Vector3(.2f, 1.6f, .2f), rot); BoxCol(z.root, sh - up * .4f, new Vector3(1.4f, 2.6f, 3f), shr);
            // "Lights Camera Action": a film camera on a crane arm
            var cr = z.At(4f, 0); z.mk.Box(steelD, cr + up * .6f, new Vector3(.8f, 1.2f, .8f), rot); var arm = cr + up * 1.2f; var tip = arm + (-fr + up * .8f).normalized * 4f; z.mk.Prism(steel, arm, (tip - arm).normalized, .1f, 4f, 6); z.mk.Box(dark, tip + up * .2f, new Vector3(.5f, .5f, .9f), rot); foreach (var e in new[] { -.2f, .2f }) z.mk.Prism(dark, tip + up * .6f + fr * e, sd, .24f, .1f, 10); z.mk.Prism(SongGlow(lights, new Color(1f, .9f, .6f), 1.6f), tip + up * .2f - fr * .46f, -fr, .16f, .02f, 10); BoxCol(z.root, cr + up * .6f, new Vector3(.8f, 1.2f, .8f), rot);
            Folk(z.At(5f, -3f), -fr, Resident.Pose.Wave, new[] { "Turn it up! The pixels dance with the bass.", "The shark is fine. The shark is part of the show.", "Grab a soda. It's three metres tall. Good luck." }, Local());
            z.area.glowLight = ArtistBits.PointLight(z.root, st.P(0, 4f, 3f), Pink, 26f, 1.8f);
            Shot(z.ctx, "meganeko's neon stage", z.At(Foot(z.a) * 1.1f, 3f) + up * 2.4f, st.P(0, 3f, -2f));
        }

        // ---------------------------------------------------------------- everyone else
        void Others(Plot z, PlaceFrame f)
        {
            var up = Vector3.up; var rot = z.Rot; var rng = new System.Random(z.a.name.Length * 7 + z.a.name[0]); var c = z.At(1f, -1f) + up * .06f; var fr = z.front; var sd = z.side;
            void Post(Vector3 at, float h, float r = .04f) { z.mk.Prism(steelD, at - up * .2f, up, r, h + .2f, 5); ArtistBits.PostCollider(z.root, at, h, r + .03f); }
            string Any() => SongIds.TryGetValue(z.a.name, out var l) && l.Length > 0 ? Song(z, l[0].title) : null;
            switch (z.a.name)
            {
                case "she": { var s = Any(); life.atomSong = s; var at = c + up * 2f; z.mk.Box(SongGlow(s, Pink, 1.4f), at, Vector3.one * .6f, rot); for (var r = 0; r < 3; r++) { var axis = Quaternion.Euler(r * 60f, r * 40f, 0) * Vector3.up; for (var k = 0; k < 12; k++) { var p = at + Quaternion.AngleAxis(k * 30f, axis) * Vector3.Cross(axis, Vector3.forward).normalized * 1.3f; z.mk.Box(r == 0 ? blueM : r == 1 ? GlowM(Hazard, 1.4f) : GlowM(Pink, 1.4f), p, Vector3.one * .14f, Quaternion.identity); } } z.mk.Box(steelD, c + up * .8f, new Vector3(.12f, 1.6f, .12f), rot); ArtistBits.PostCollider(z.root, c, 3f, 1.2f); break; }
                case "Bioblitz": { var s = Any(); life.warmSong = s; z.mk.Box(steelD, c + up * .5f, new Vector3(1.2f, 1f, .8f), rot); for (var k = 0; k < 5; k++) z.mk.Box(furnaceM, c + up * (.3f + k * .14f) + fr * .41f, new Vector3(1f, .04f, .02f), rot); z.mk.Blob(SongGlow(s, new Color(.3f, .6f, 1f), 1f), c + up * 1.6f, Vector3.one * .5f, rng, 0f); for (var k = 0; k < 4; k++) z.mk.Blob(Paint(new Color(.3f, .6f, .3f)), c + up * 1.6f + Quaternion.Euler(k * 50f, k * 90f, 0) * fr * .48f, new Vector3(.2f, .14f, .06f), rng, .2f); BoxCol(z.root, c + up * .8f, new Vector3(1.2f, 1.6f, .8f), rot); break; }
                case "Dave Era": { var s = Any(); var lc = c + up * 1.8f; for (var k = 0; k < 32; k++) { var a = k * Mathf.PI * 2f / 32f; var p = lc + sd * Mathf.Sin(a) * 1.4f + fr * Mathf.Sin(a * 2f) * .5f + up * Mathf.Cos(a) * .7f; z.mk.Box(k == 0 ? SongGlow(s, Green, 1.6f) : (k % 2 == 0 ? greenM : blueM), p, Vector3.one * .12f, Quaternion.identity); } Post(c, 1f, .06f); break; }
                case "James Egbert": { var s = Any(); life.haloSong = s; z.mk.Box(steelD, c + up * 1f, new Vector3(.6f, 2f, .6f), rot); BoxCol(z.root, c + up * 1f, new Vector3(.6f, 2f, .6f), rot); for (var k = 0; k < 20; k++) { var a = k * Mathf.PI / 10f; z.mk.Box(k == 0 ? SongGlow(s, White, 1.8f) : lampM, c + up * 2.6f + sd * Mathf.Cos(a) * .8f + fr * Mathf.Sin(a) * .8f, new Vector3(.28f, .06f, .06f), Quaternion.LookRotation(sd * -Mathf.Sin(a) + fr * Mathf.Cos(a))); } halos.Add(c + up * 2.6f, 1.6f, White, .4f); break; }
                case "Manteeny": { var s = Any(); var nf = new PlaceFrame(c, fr); z.mk.Box(SongGlow(s, Green, 1.4f), nf.P(-.8f, 1.4f, 0), new Vector3(.2f, 2f, .1f), nf.r); z.mk.Box(greenM, nf.P(-1f, 2.3f, 0), new Vector3(.4f, .14f, .1f), nf.r * Quaternion.Euler(0, 0, -40f)); var two = new[] { (new Vector3(.8f, 2.3f, 0), new Vector3(.8f, .14f, .1f), 0f), (new Vector3(1.15f, 1.95f, 0), new Vector3(.14f, .7f, .1f), 0f), (new Vector3(.8f, 1.4f, 0), new Vector3(.9f, .14f, .1f), -35f), (new Vector3(.8f, .5f, 0), new Vector3(.8f, .14f, .1f), 0f) }; foreach (var (p, sz, a) in two) z.mk.Box(blueM, nf.P(p.x, p.y, p.z), sz, nf.r * Quaternion.Euler(0, 0, a)); foreach (var e in new[] { -.8f, .8f }) Post(nf.P(e, 0, -.1f), .5f, .05f); BoxCol(z.root, nf.P(0, 1.3f, 0), new Vector3(2.4f, 2.6f, .3f), nf.r); break; }
                case "Mord Fustang": { var s = Any(); Post(c, 2f, .08f); z.mk.Box(steel, c + up * 2f + fr * .3f, new Vector3(.2f, .2f, .7f), rot); z.mk.Box(steel, c + up * 2.1f, new Vector3(.4f, .1f, .1f), rot); var tap = c + up * 1.9f + fr * .6f; z.mk.Prism(steel, tap, -up, .08f, .2f, 8); for (var k = 0; k < 5; k++) z.mk.Blob(SongGlow(k == 0 ? s : null, Green, 1.6f), tap - up * (.4f + k * .32f), new Vector3(.06f, .1f, .06f), rng, 0f); z.mk.Prism(SongGlow(s, Green, 1f), c + fr * .6f + up * .02f, up, .5f, .02f, 12); life.dripAt = tap; break; }
                case "Rabbit Killer": { var s = Any(); life.imagSong = s; var rb = c + up * .1f; var bm = SongGlow(s, Pink, 1f); z.mk.Blob(bm, rb + up * .7f, new Vector3(.5f, .6f, .6f), rng, 0f); z.mk.Blob(bm, rb + up * 1.4f + fr * .2f, new Vector3(.32f, .32f, .32f), rng, 0f); foreach (var e in new[] { -.12f, .12f }) z.mk.Box(bm, rb + up * 2f + fr * .2f + sd * e, new Vector3(.1f, .7f, .06f), rot * Quaternion.Euler(0, 0, -e * 60f)); z.mk.Blob(white, rb + up * .6f - fr * .55f, Vector3.one * .14f, rng, 0f); ArtistBits.PostCollider(z.root, c, 2.2f, .6f); break; }
                case "Ryeland": { var s = Any(); life.luvuSong = s; var hm = new MeshKit(); var m = SongGlow(s, new Color(1f, .2f, .4f), 1.6f); for (var k = 0; k < 24; k++) { var a = k * Mathf.PI * 2f / 24f; var x = 16f * Mathf.Pow(Mathf.Sin(a), 3f); var y = 13f * Mathf.Cos(a) - 5f * Mathf.Cos(2f * a) - 2f * Mathf.Cos(3f * a) - Mathf.Cos(4f * a); hm.Box(m, new Vector3(x, y, 0) * .06f, Vector3.one * .12f, Quaternion.identity); } var hg = hm.BuildSingle("Neon heart", z.root, false); hg.transform.SetPositionAndRotation(c + up * 2f, rot); life.hearts.Add(hg.transform); Post(c, 1.2f, .05f); break; }
                default: { var s = Any(); Post(c, 1.4f); z.mk.Box(SongGlow(s, Green, 1.6f), c + up * 1.8f, new Vector3(.1f, .8f, .1f), rot); break; }
            }
        }
    }
}
