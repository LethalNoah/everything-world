using System.Collections.Generic;
using UnityEngine;
using static EverythingWorld.World.PlaceTools;

namespace EverythingWorld.World
{
    /// The Ambient Mists' artists. Each has a round of mown grass in the long meadow, a wooden post with a faded enamel
    /// name plate, and one thing left out in the field from their music; covers on an old cork board on legs.
    /// - Boards of Canada: a radio telescope dish turned to the sky ("Prophecy At 1420 MHz"), an old camper van with its
    ///   windows lit ("Dayvan Cowboy"), a faded rainbow ("Roygbiv"), a weather balloon floating on a line ("Hydrogen Helium
    ///   Lithium Leviathan"), a little satellite dish ("Satellite Anthem Icarus"), a gate standing alone in the grass
    ///   ("Into The Magic Land") and a dark still pond ("Naraka").
    /// - Aphex Twin: an upright piano in the grass ("Avril 14th"), three standing stones ("Ageispolis"), a crystal ("Xtal"),
    ///   an oscilloscope on a crate ("Pulsewidth"), a ring of green chalk ("Green Calx") and a box of blinking lights
    ///   ("Omgyjya-Switch7").
    /// - The rest: a sphere of wire, a mirror on a stand, keys on a line, a bathtub in the grass, balloons, a little house
    ///   with a lit window, stairs up into nothing, a small stone arch, a maple turning red, a giant glowing mushroom, a
    ///   sun on a post and a paper lantern, a silver figure and two chairs, a beam of light from the sky, a jumble of shapes.
    public partial class MistKit
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
                Add("Marconi Union", "Weightless", "Marconi Union"); Add("Aphex Twin", "Avril 14th", "Aphex Twin"); Add("Boards of Canada", "Roygbiv", "Boards of Canada"); Add("Aphex Twin", "Xtal", "Aphex Twin");
                Add("Shpongle", "Divine Moments", "Shpongle"); Add("Tycho", "Japan", "Tycho"); Add("Tycho", "Into The Woods", "Tycho"); Add("ridgeclub", "I'm God", "ridgeclub");
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
            if (wood == null) MakeMaterials();
            var root = new GameObject("Artist: " + a.name).transform; root.SetParent(ctx.Root, false);
            var front = facing.TryGetValue(a.name, out var f) ? f : new Vector3(center.x - a.x, 0, center.z - a.z).normalized;
            if (front.sqrMagnitude < .01f) front = Vector3.forward;
            var labels = new GameObject("Labels").transform; labels.SetParent(root, false);
            var area = root.gameObject.AddComponent<ArtistArea>();
            area.artistName = a.name; area.stationId = a.stationId; area.radius = a.r; area.glowColor = Orange;
            var z = new Plot { ctx = ctx, root = root, labels = labels, mk = new MeshKit(), solid = new MeshKit(), pictures = new MeshKit(), a = a, c = ctx.OnGround(a.x, a.z), front = front, side = Vector3.Cross(Vector3.up, front), area = area };
            var reach = Foot(a); var up = Vector3.up;
            z.mk.Prism(Paint(new Color(.56f, .62f, .46f)), z.c - up * .3f, up, reach * .8f, .34f, 26);
            switch (a.name)
            {
                case "Boards of Canada": Boards(z); break;
                case "Aphex Twin": Aphex(z); break;
                default: Others(z, NamePost(z)); break;
            }
            SongLamps(z);
            Board(z, z.At(reach * .6f, -reach * .45f - 1.5f), front);
            ArtistBits.Plaque(z.mk, z.solid, labels, a, z.At(reach * .6f + 1.5f, reach * .45f + 1.5f), front, wood, Paint(new Color(.3f, .4f, .42f)), Haze, 1.6f);
            Lamp(ctx, z.mk, z.At(reach * .5f, reach * .7f), false);
            if (z.area.glowLight == null) z.area.glowLight = ArtistBits.PointLight(root, z.c + up * 4f + front * 3f, Bulb, HasStation(a) ? 22f : 13f, 1.2f);
            area.glowRenderers = new Renderer[0];
            z.mk.Build("Area", root, false);
            z.solid.BuildCollider("Walls", root);
            z.pictures.Build("Covers", root, false);
            noGrass.Add(new Vector3(z.c.x, reach * .85f, z.c.z));
            var mark = new GameObject("Shot: " + a.name + " up close").transform; mark.SetParent(ctx.Root, false);
            var cam = z.At(reach * .85f, reach * .3f) + up * 2f; var look = z.At(-reach * .3f, 0) + up * 1.8f - cam; mark.SetPositionAndRotation(cam, Quaternion.LookRotation(look.normalized));
        }

        /// The covers pinned to an old cork board on wooden legs.
        void Board(Plot z, Vector3 ground, Vector3 dir)
        {
            dir = Vector3.ProjectOnPlane(dir, Vector3.up).normalized; var rot = Quaternion.LookRotation(dir);
            var n = Mathf.Max(1, Mathf.Min(7, z.a.songList?.Length ?? 1)); const float size = .9f; var w = n * size * 1.25f + .6f;
            var c = ground + Vector3.up * 1.7f;
            z.mk.Box(wood, c - Vector3.up * .5f - dir * .06f, new Vector3(w + .4f, size + 1.9f, .14f), rot); z.mk.Box(Paint(new Color(.66f, .54f, .4f)), c - Vector3.up * .1f + dir * .02f, new Vector3(w, size + .9f, .04f), rot); z.solid.Box(wood, c - Vector3.up * .5f, new Vector3(w + .4f, size + 1.9f, .3f), rot);
            foreach (var e in new[] { -1f, 1f }) z.mk.Box(wood, ground + Vector3.up * .6f + Vector3.Cross(Vector3.up, dir) * e * (w / 2f), new Vector3(.12f, 1.2f, .12f), rot);
            ArtistBits.CoverWall(z.pictures, z.labels, z.ctx, z.a, c + dir * .06f + Vector3.up * .08f, dir, n, size, n, true, Ink);
        }

        /// A little bulb on a stick for each song without its own piece.
        void SongLamps(Plot z)
        {
            if (!SongIds.TryGetValue(z.a.name, out var list)) return;
            var up = Vector3.up; var k = 0; var seen = new HashSet<string>();
            foreach (var (_, id) in list)
            {
                if (z.lit.Contains(id) || !seen.Add(id)) continue;
                var p = z.At(Foot(z.a) * .55f - (k % 2) * 1.3f, Foot(z.a) * .2f + (k / 2) * 1.3f);
                z.mk.Prism(wood, p, up, .03f, 1.2f, 5); z.mk.Blob(SongGlow(id, Bulb, 1.4f), p + up * 1.3f, new Vector3(.1f, .14f, .1f), new System.Random(k), 0f); ArtistBits.PostCollider(z.root, p, 1.3f, .08f);
                k++;
            }
        }

        /// A wooden post with a faded enamel name plate.
        PlaceFrame NamePost(Plot z)
        {
            var up = Vector3.up; var f = new PlaceFrame(z.At(-Foot(z.a) * .5f, 0), z.front);
            z.mk.Box(wood, f.P(0, .9f, 0), new Vector3(.16f, 1.8f, .16f), f.r); z.mk.Box(Paint(new Color(.3f, .44f, .46f)), f.P(0, 1.6f, .1f), new Vector3(1.6f, .4f, .04f), f.r); z.mk.Box(Paint(Haze), f.P(0, 1.6f, .125f), new Vector3(1.5f, .32f, .01f), f.r); ArtistBits.Label(z.labels, z.a.name, f.P(0, 1.6f, .14f), f.Fwd, .12f, new Color(.2f, .3f, .32f), TextAnchor.MiddleCenter); ArtistBits.PostCollider(z.root, f.o, 1.8f, .14f);
            return f;
        }

        // ---------------------------------------------------------------- Boards of Canada
        void Boards(Plot z)
        {
            var up = Vector3.up; var rng = new System.Random(1998); var fr = z.front; var sd = z.side; var rot = z.Rot;
            var proph = Song(z, "Prophecy"); var dayvan = Song(z, "Dayvan"); var roy = Song(z, "Roygbiv"); var hyd = Song(z, "Hydrogen"); var sat = Song(z, "Satellite"); var magic = Song(z, "Into The Magic"); var nar = Song(z, "Naraka"); life.roySong = roy;
            // the radio telescope dish
            var ds = z.At(-8f, -2f); foreach (var e in new[] { -1f, 1f }) { z.mk.Box(white, ds + up * 2.4f + sd * e * 1.4f, new Vector3(.3f, 4.8f, .6f), rot); } z.mk.Box(white, ds + up * .4f, new Vector3(3.6f, .8f, 2f), rot); z.solid.Box(white, ds + up * 2f, new Vector3(3.6f, 4f, 2f), rot);
            var dc = ds + up * 5f; var aim = (fr * .5f + up).normalized; z.mk.Prism(white, dc, aim, 4.4f, 1.4f, 24, .2f); z.mk.Prism(Paint(new Color(.6f, .62f, .6f)), dc + aim * .02f, aim, 4.2f, .02f, 24); for (var k = 0; k < 3; k++) { var q = Quaternion.AngleAxis(k * 120f, aim) * Vector3.Cross(aim, sd).normalized; z.mk.Prism(iron, dc + q * 3.8f, (dc + aim * 3.4f - (dc + q * 3.8f)).normalized, .04f, Vector3.Distance(dc + aim * 3.4f, dc + q * 3.8f), 4); } z.mk.Blob(SongGlow(proph, new Color(1f, .4f, .3f), 2f), dc + aim * 3.5f, Vector3.one * .2f, rng, 0f);
            // the camper van
            var cv = z.At(1f, -9f); var cr = Quaternion.LookRotation(sd); var body = Paint(new Color(.8f, .6f, .4f)); z.mk.Box(body, cv + up * 1.2f, new Vector3(2.2f, 1.6f, 5f), cr); z.mk.Box(white, cv + up * 2.1f, new Vector3(2.2f, .3f, 5f), cr); z.solid.Box(body, cv + up * 1.2f, new Vector3(2.2f, 2.2f, 5f), cr); for (var k = 0; k < 3; k++) z.mk.Box(k == 1 ? SongGlow(dayvan, Bulb, 1.2f) : windowM(), cv + up * 1.5f + sd * (-1.4f + k * 1.2f) + fr * 1.11f, new Vector3(.8f, .5f, .02f), cr); foreach (var e in new[] { -1.6f, 1.6f }) foreach (var q in new[] { -1.12f, 1.12f }) z.mk.Prism(dark, cv + up * .4f + sd * e + fr * q, fr * Mathf.Sign(q), .4f, .2f, 10);
            // a faded rainbow
            var rb = z.At(-Foot(z.a) * .8f, 6f); var cols = new[] { new Color(.86f, .5f, .5f), new Color(.86f, .7f, .5f), new Color(.86f, .84f, .56f), new Color(.56f, .78f, .56f), new Color(.5f, .66f, .84f), new Color(.66f, .56f, .84f) }; for (var b = 0; b < 6; b++) for (var k = 0; k <= 14; k++) { var a = Mathf.PI * k / 14f; var r = 4f - b * .3f; z.mk.Box(b == 0 && k == 7 ? SongGlow(roy, cols[b], .8f) : GlowM(cols[b], .45f), rb + sd * Mathf.Cos(a) * r + up * Mathf.Sin(a) * r, new Vector3(.94f, .28f, .04f), rot * Quaternion.Euler(0, 0, a * Mathf.Rad2Deg + 90f)); }
            // a weather balloon floating on a line
            var bl = z.At(4f, 6f); var bm = new MeshKit(); bm.Blob(SongGlow(hyd, new Color(.96f, .94f, .9f), .9f), Vector3.zero, new Vector3(1.2f, 1.4f, 1.2f), rng, 0f); bm.Prism(white, -Vector3.up * 1.4f, -Vector3.up, .01f, 7f, 3); var bg = bm.BuildSingle("Weather balloon", z.root, false); bg.transform.position = bl + up * 9f; life.balloons.Add((bg.transform, bl + up * 9f)); z.mk.Box(wood, bl + up * .3f, new Vector3(.4f, .6f, .4f), rot); BoxCol(z.root, bl + up * .3f, new Vector3(.4f, .6f, .4f), rot);
            // a little satellite dish, a gate standing alone, a dark pond
            var sd2 = z.At(6f, -3f); z.mk.Prism(iron, sd2, up, .05f, 1.4f, 5); z.mk.Prism(white, sd2 + up * 1.4f, (fr + up).normalized, .7f, .3f, 14, .2f); z.mk.Blob(SongGlow(sat, new Color(.6f, .8f, 1f), 1.4f), sd2 + up * 1.4f + (fr + up).normalized * .6f, Vector3.one * .07f, rng, 0f); ArtistBits.PostCollider(z.root, sd2, 1.6f, .4f);
            var gt = new PlaceFrame(z.At(-3f, 9f), fr); foreach (var e in new[] { -1f, 1f }) { z.mk.Box(wood, gt.P(e, .7f, 0), new Vector3(.16f, 1.4f, .16f), gt.r); ArtistBits.PostCollider(z.root, gt.P(e, 0, 0), 1.4f, .12f); } for (var k = 0; k < 4; k++) z.mk.Box(wood, gt.P(0, .3f + k * .3f, 0), new Vector3(1.8f, .1f, .05f), gt.r * Quaternion.Euler(0, 0, k == 2 ? 30f : 0f)); z.mk.Box(SongGlow(magic, new Color(1f, .86f, .6f), .6f), gt.P(0, .8f, -.3f), new Vector3(1.8f, 1.4f, .02f), gt.r);
            var pd = z.At(-2f, -5f); z.mk.Prism(Paint(new Color(.2f, .24f, .26f)), pd - up * .2f, up, 2f, .26f, 16); z.mk.Prism(SongGlow(nar, new Color(.2f, .3f, .34f), .4f), pd + up * .07f, up, 1.8f, .02f, 16); BoxCol(z.root, pd, new Vector3(3.6f, .3f, 3.6f), rot);
            Folk(z.At(4f, 9f), -fr, Resident.Pose.Stand, new[] { "The dish is listening for something. It's been listening for years.", "That van hasn't moved since a summer I can't quite remember.", "Somewhere there's a tape of this exact afternoon." }, Local());
            z.area.glowLight = ArtistBits.PointLight(z.root, z.c + up * 6f, Bulb, 28f, 1.4f);
            Shot(z.ctx, "the radio telescope", z.At(Foot(z.a) * 1.1f, -4f) + up * 2.4f, dc);
        }

        Material windowM() => GlowM(Bulb, 1f);

        // ---------------------------------------------------------------- Aphex Twin
        void Aphex(Plot z)
        {
            var up = Vector3.up; var rng = new System.Random(1992); var fr = z.front; var sd = z.side; var rot = z.Rot;
            var avril = Song(z, "Avril"); var agei = Song(z, "Ageispolis"); var xtal = Song(z, "Xtal"); var pulse = Song(z, "Pulsewidth"); var calx = Song(z, "Green Calx"); var omg = Song(z, "Omgyjya"); life.avrilSong = avril; life.xtalSong = xtal;
            // an upright piano out in the grass, a stool before it
            var pf = new PlaceFrame(z.At(-3f, -2f), fr); var pw = Paint(new Color(.3f, .2f, .16f)); z.mk.Box(pw, pf.P(0, .7f, 0), new Vector3(1.6f, 1.4f, .6f), pf.r); z.mk.Box(pw, pf.P(0, .75f, .4f), new Vector3(1.6f, .1f, .3f), pf.r); z.mk.Box(white, pf.P(0, .82f, .45f), new Vector3(1.4f, .03f, .18f), pf.r); for (var k = 0; k < 10; k++) z.mk.Box(dark, pf.P(-.6f + k * .14f, .85f, .5f), new Vector3(.05f, .02f, .1f), pf.r); z.mk.Box(SongGlow(avril, Bulb, 1f), pf.P(0, 1.1f, .31f), new Vector3(.6f, .3f, .02f), pf.r); BoxCol(z.root, pf.P(0, .7f, .1f), new Vector3(1.6f, 1.4f, .9f), pf.r); var st = pf.P(0, 0, 1.1f); z.mk.Prism(wood, st, up, .22f, .5f, 10); Interact.Seat(tlife.transform, st + up * .52f, -pf.Fwd, "Sit at the piano").seatHeight = .5f;
            // three standing stones; a crystal
            for (var k = 0; k < 3; k++) { var sp = z.At(-6f + k * 1.6f, 6f + (k % 2) * .6f); var h = 2f + k * .5f; z.mk.Box(Paint(new Color(.5f, .5f, .46f)), sp + up * (h / 2f - .2f), new Vector3(.8f, h, .5f), rot * Quaternion.Euler(0, k * 20f, (k - 1) * 4f)); BoxCol(z.root, sp + up * h / 2f, new Vector3(.8f, h, .5f), rot); if (k == 1) z.mk.Box(SongGlow(agei, new Color(.6f, .8f, .8f), 1f), sp + up * h * .6f + fr * .26f, new Vector3(.3f, .3f, .02f), rot); }
            var cx = z.At(3f, 5f); z.mk.Prism(Paint(new Color(.5f, .5f, .46f)), cx, up, .5f, .5f, 8); z.mk.Prism(SongGlow(xtal, new Color(.7f, .9f, 1f), 1.4f), cx + up * .5f, up, .3f, 1.4f, 6, .05f); ArtistBits.PostCollider(z.root, cx, 1.9f, .5f);
            // an oscilloscope on a crate; a ring of green chalk; a box of blinking lights
            var os = z.At(4f, -4f); z.mk.Box(wood, os + up * .4f, new Vector3(.8f, .8f, .8f), rot); z.mk.Box(Paint(new Color(.6f, .6f, .56f)), os + up * 1.1f, new Vector3(.6f, .5f, .5f), rot); z.mk.Box(SongGlow(pulse, new Color(.4f, 1f, .5f), 1.2f), os + up * 1.12f + fr * .26f, new Vector3(.36f, .28f, .01f), rot); BoxCol(z.root, os + up * .7f, new Vector3(.8f, 1.4f, .8f), rot);
            z.mk.Prism(SongGlow(calx, new Color(.5f, .9f, .4f), .8f), z.At(0f, 1f) + up * .02f, up, 2.4f, .02f, 28); z.mk.Prism(Paint(new Color(.56f, .62f, .46f)), z.At(0f, 1f) + up * .025f, up, 2.2f, .02f, 28);
            var bx = z.At(-1f, -7f); z.mk.Box(dark, bx + up * .5f, new Vector3(1f, 1f, .6f), rot); for (var k = 0; k < 12; k++) z.mk.Box(k == 0 ? SongGlow(omg, new Color(1f, .3f, .2f), 1.6f) : GlowM(k % 3 == 0 ? new Color(1f, .3f, .2f) : new Color(.4f, 1f, .5f), 1.2f), bx + up * (.3f + (k / 4) * .2f) + sd * (-.3f + (k % 4) * .2f) + fr * .31f, new Vector3(.06f, .06f, .02f), rot); BoxCol(z.root, bx + up * .5f, new Vector3(1f, 1f, .6f), rot);
            Folk(z.At(6f, 2f), -fr, Resident.Pose.Stand, new[] { "Somebody plays that piano at dawn. I've never seen who.", "The stones hum if you put your ear to them. Try it.", "The little box blinks in a pattern. I think it's counting something." }, Local());
            z.area.glowLight = ArtistBits.PointLight(z.root, z.c + up * 5f, Bulb, 24f, 1.4f);
        }

        // ---------------------------------------------------------------- everyone else
        void Others(Plot z, PlaceFrame f)
        {
            var up = Vector3.up; var rot = z.Rot; var rng = new System.Random(z.a.name.Length * 7 + z.a.name[0]); var c = z.At(1f, -1f) + up * .05f; var fr = z.front; var sd = z.side;
            void Post(Vector3 at, float h, float r = .04f) { z.mk.Prism(wood, at - up * .2f, up, r, h + .2f, 5); ArtistBits.PostCollider(z.root, at, h, r + .03f); }
            string Any() => SongIds.TryGetValue(z.a.name, out var l) && l.Length > 0 ? Song(z, l[0].title) : null;
            switch (z.a.name)
            {
                case "Douglas Holmquist": { var s = Any(); var sc = c + up * 1.4f; for (var r = 0; r < 4; r++) { var axis = Quaternion.Euler(r * 45f, r * 30f, 0) * up; for (var k = 0; k < 16; k++) { var a0 = Quaternion.AngleAxis(k * 22.5f, axis) * Vector3.Cross(axis, Vector3.forward).normalized * .9f; var a1 = Quaternion.AngleAxis((k + 1) * 22.5f, axis) * Vector3.Cross(axis, Vector3.forward).normalized * .9f; z.mk.Box(r == 0 && k == 0 ? SongGlow(s, Bulb, 1.4f) : iron, sc + (a0 + a1) / 2f, new Vector3(.03f, .03f, (a1 - a0).magnitude), Quaternion.LookRotation(a1 - a0)); } } Post(c, .5f, .05f); ArtistBits.PostCollider(z.root, c, 2.3f, .9f); break; }
                case "Elori Saxl": { var s = Any(); var mf = new PlaceFrame(c, fr); foreach (var e in new[] { -.5f, .5f }) z.mk.Box(wood, mf.P(e, .9f, 0), new Vector3(.08f, 1.8f, .08f), mf.r); z.mk.Box(wood, mf.P(0, 1.1f, 0), new Vector3(1f, 1.4f, .06f), mf.r); z.mk.Box(SongGlow(s, new Color(.8f, .86f, .9f), .9f), mf.P(0, 1.1f, .04f), new Vector3(.86f, 1.26f, .01f), mf.r); BoxCol(z.root, mf.P(0, .9f, 0), new Vector3(1.1f, 1.8f, .3f), mf.r); break; }
                case "Express Rising": { var s = Any(); foreach (var e in new[] { -1.4f, 1.4f }) Post(c + sd * e, 1.6f); z.mk.Box(Paint(Haze), c + up * 1.55f, new Vector3(.02f, .02f, 2.8f), Quaternion.LookRotation(sd)); for (var k = 0; k < 7; k++) { var kp = c + sd * (-1f + k * .33f) + up * 1.4f; z.mk.Box(k == 3 ? SongGlow(s, Bulb, 1.2f) : Paint(new Color(.7f, .6f, .36f)), kp, new Vector3(.04f, .2f, .02f), rot); z.mk.Prism(Paint(new Color(.7f, .6f, .36f)), kp + up * .12f - fr * .02f, fr, .04f, .02f, 8); } break; }
                case "KatzPascale": { var sink = Song(z, "SINK"); var spill = Song(z, "SPILL"); z.mk.Blob(white, c + up * .5f, new Vector3(1f, .4f, .5f), rng, 0f); z.mk.Blob(Paint(new Color(.3f, .4f, .42f)), c + up * .74f, new Vector3(.86f, .14f, .38f), rng, 0f); z.mk.Box(SongGlow(sink, new Color(.5f, .7f, .8f), .6f), c + up * .8f, new Vector3(1.4f, .02f, .6f), Quaternion.LookRotation(sd)); BoxCol(z.root, c + up * .4f, new Vector3(.9f, .8f, 2f), Quaternion.LookRotation(sd)); z.mk.Prism(SongGlow(spill, new Color(.5f, .7f, .8f), .5f), c + fr * 1.6f + sd * .6f + up * .02f, up, 1.2f, .02f, 14); break; }
                case "Marconi Union": { var s = Any(); life.weightSong = s; for (var k = 0; k < 5; k++) { var bm = new MeshKit(); bm.Blob(k == 0 ? SongGlow(s, new Color(.9f, .86f, .8f), .9f) : GlowM(new[] { Orange, Teal, Haze, new Color(.8f, .7f, .8f) }[k % 4], .6f), Vector3.zero, new Vector3(.4f, .5f, .4f), rng, 0f); bm.Prism(white, -Vector3.up * .5f, -Vector3.up, .006f, 2.6f, 3); var g = bm.BuildSingle("Balloon", z.root, false); var home = c + up * (3f + k * .3f) + Quaternion.Euler(0, k * 72f, 0) * fr * .4f; g.transform.position = home; life.balloons.Add((g.transform, home)); } z.mk.Box(wood, c + up * .2f, new Vector3(.3f, .4f, .3f), rot); BoxCol(z.root, c + up * .2f, new Vector3(.3f, .4f, .3f), rot); break; }
                case "Mark Barrott": { var s = Any(); var hp = c - fr * .4f; z.mk.Box(white, hp + up * .9f, new Vector3(2f, 1.8f, 1.6f), rot); z.solid.Box(white, hp + up * .9f, new Vector3(2f, 1.8f, 1.6f), rot); z.mk.Prism(Paint(new Color(.7f, .4f, .34f)), hp + up * 1.8f, up, 1.6f, 1f, 4, .05f, 45f); z.mk.Box(SongGlow(s, Bulb, 1.2f), hp + up * 1f + fr * .81f, new Vector3(.5f, .5f, .02f), rot); z.mk.Box(wood, hp + up * .6f + fr * .81f + sd * .6f, new Vector3(.4f, 1.2f, .02f), rot); break; }
                case "Pipetka": { var s = Any(); for (var k = 0; k < 9; k++) { var sp = c - fr * (k * .5f - 1f) + up * (k * .3f + .15f); z.mk.Box(wood, sp, new Vector3(1f, .1f, .5f), rot); z.mk.Box(wood, sp - up * (k * .3f + .15f) / 2f, new Vector3(.08f, k * .3f + .15f, .08f), rot); z.solid.Box(wood, sp - up * .05f, new Vector3(1f, .2f, .5f), rot); } z.mk.Box(SongGlow(s, Bulb, 1.4f), c - fr * 3.6f + up * 2.9f, new Vector3(1f, .04f, .5f), rot); halos.Add(c - fr * 3.6f + up * 3.2f, 1.6f, Bulb, .3f); break; }
                case "Prosecutor Archangels": { var s = Any(); var ar = new PlaceFrame(c, fr); var stone = Paint(new Color(.6f, .58f, .52f)); foreach (var e in new[] { -1f, 1f }) { z.mk.Box(stone, ar.P(e, 1f, 0), new Vector3(.5f, 2f, .5f), ar.r); BoxCol(z.root, ar.P(e, 1f, 0), new Vector3(.5f, 2f, .5f), ar.r); } for (var k = 0; k <= 8; k++) { var a = Mathf.PI * k / 8f; z.mk.Box(k == 4 ? SongGlow(s, Bulb, 1.2f) : stone, ar.P(Mathf.Cos(a) * 1f, 2f + Mathf.Sin(a) * .7f, 0), new Vector3(.4f, .3f, .5f), ar.r * Quaternion.Euler(0, 0, a * Mathf.Rad2Deg + 90f)); } break; }
                case "Rei Harakami": { var s = Any(); life.seasonSong = s; z.mk.Prism(trunkM, c, (up + sd * .1f).normalized, .2f, 2.6f, 6, .6f); for (var k = 0; k < 6; k++) z.mk.Blob(k == 0 ? SongGlow(s, new Color(.9f, .4f, .3f), .7f) : Paint(k % 2 == 0 ? new Color(.86f, .4f, .3f) : new Color(.86f, .6f, .36f)), c + up * (2.8f + (k % 2) * .4f) + Quaternion.Euler(0, k * 60f, 0) * fr * (k == 0 ? 0f : 1f), new Vector3(1f, .7f, 1f), rng, .3f); ArtistBits.PostCollider(z.root, c, 2.6f, .3f); for (var k = 0; k < 9; k++) z.mk.Box(Paint(new Color(.86f, .4f, .3f)), c + Quaternion.Euler(0, k * 40f, 0) * fr * (1f + k % 3 * .4f) + up * .03f, new Vector3(.14f, .01f, .1f), Quaternion.Euler(0, k * 30f, 0)); break; }
                case "Shpongle": { var s = Any(); life.shpongleSong = s; z.mk.Prism(white, c, up, .3f, 2f, 10, .8f); z.mk.Blob(SongGlow(s, new Color(.8f, .4f, 1f), 1f), c + up * 2.2f, new Vector3(1.4f, .6f, 1.4f), rng, .05f); for (var k = 0; k < 7; k++) z.mk.Blob(GlowM(new Color(.5f, 1f, .7f), 1.2f), c + up * 2.5f + Quaternion.Euler(0, k * 51f, 0) * fr * .8f, Vector3.one * .1f, rng, 0f); ArtistBits.PostCollider(z.root, c, 2.6f, 1.2f); break; }
                case "Tycho": { var woods = Song(z, "Into The Woods"); var japan = Song(z, "Japan"); life.woodsSong = woods; life.japanSong = japan; Post(c - sd * .8f, 2.4f, .06f); z.mk.Prism(SongGlow(woods, Orange, 1.4f), c - sd * .8f + up * 2.8f - fr * .05f, fr, .7f, .06f, 22); for (var k = 0; k < 4; k++) z.mk.Box(dark, c - sd * .8f + up * (2.5f + k * .16f) + fr * .02f, new Vector3(1.4f, .04f, .02f), rot); var lt = c + sd * 1.4f; Post(lt, 1.8f); z.mk.Prism(SongGlow(japan, new Color(1f, .7f, .7f), 1.2f), lt + up * 1.7f, up, .26f, .5f, 10, .8f); break; }
                case "Vegyn": { var argent = Song(z, "La femme"); var both = Song(z, "There"); var fig = c - sd * 1f; var silver = Paint(new Color(.7f, .72f, .76f)); z.mk.Box(silver, fig + up * .5f, new Vector3(.5f, 1f, .5f), rot); z.mk.Prism(SongGlow(argent, new Color(.86f, .88f, .9f), .9f), fig + up * 1f, up, .2f, 1f, 8, .7f); z.mk.Blob(silver, fig + up * 2.2f, new Vector3(.16f, .2f, .16f), rng, 0f); BoxCol(z.root, fig + up * 1.1f, new Vector3(.5f, 2.3f, .5f), rot); foreach (var e in new[] { -1f, 1f }) { var ch = c + sd * 1.4f + fr * e * .6f; var cf = -fr * e; z.mk.Box(wood, ch + up * .45f, new Vector3(.45f, .06f, .45f), Quaternion.LookRotation(cf)); z.mk.Box(wood, ch + up * .75f - cf * .2f, new Vector3(.45f, .6f, .05f), Quaternion.LookRotation(cf)); Interact.Seat(tlife.transform, ch + up * .5f, cf, "Sit and talk").seatHeight = .48f; } z.mk.Blob(SongGlow(both, Bulb, 1.2f), c + sd * 1.4f + up * 1.2f, Vector3.one * .08f, rng, 0f); BoxCol(z.root, c + sd * 1.4f + up * .4f, new Vector3(.6f, .8f, 1.8f), rot); break; }
                case "ridgeclub": { var s = Any(); life.godSong = s; z.mk.Prism(SongGlow(s, new Color(1f, .96f, .86f), .5f), c, up, 1f, 30f, 14, 1.6f, 0f, false); z.mk.Prism(Paint(new Color(.66f, .64f, .56f)), c - up * .1f, up, 1.1f, .2f, 14); halos.Add(c + up * 1f, 2.4f, new Color(1f, .96f, .86f), .3f); break; }
                default: { var s = Any(); var jc = c + up * 1.6f; for (var k = 0; k < 9; k++) z.mk.Box(k == 0 ? SongGlow(s, new Color(.8f, .5f, 1f), 1.4f) : GlowM(new[] { Orange, Teal, new Color(.8f, .5f, 1f) }[k % 3], .8f), jc + new Vector3(Mathf.Sin(k * 2.1f), Mathf.Cos(k * 1.7f) * .6f, Mathf.Sin(k * 1.3f)) * .7f, Vector3.one * (.16f + (k % 3) * .08f), Quaternion.Euler(k * 30f, k * 50f, k * 20f)); Post(c, .9f, .05f); ArtistBits.PostCollider(z.root, c, 2.4f, .8f); break; }
            }
        }
    }
}
