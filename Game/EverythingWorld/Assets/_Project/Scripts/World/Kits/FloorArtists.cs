using System.Collections.Generic;
using UnityEngine;
using static EverythingWorld.World.PlaceTools;

namespace EverythingWorld.World
{
    /// The Four-on-the-Floor Steppe's artists. Most have a little festival stage of their own: a low black deck under a
    /// canopy on truss poles in their colour, a DJ table, a light on a stand, their name in lights along the canopy, and
    /// one thing from their music on the deck; covers on a black board with a neon edge.
    /// - deadmau5: a great glowing cube on a deck, its faces lit in blocks ("Jupiter" a planet floating over it, "Snowcone"
    ///   a giant snow cone, "Fabienk" a pair of glowing headphones).
    /// - Astronaut: a rocket on its launch gantry, a glowing Earth ("Earthsphere"), a trophy ("Champions"), a rain cloud
    ///   ("Rain") and a war drum ("War").
    /// - Zero Eden: a pool of digital waves ("Ocean Simulator"), a sign of ones and zeros ("Between Binaries") and a door of
    ///   light ("Anything Is Possible").
    /// - The rest: a neon graveyard, an equalizer and a sunrise, a pool float, a clock, a mirror ball, a pyramid of lights,
    ///   lanterns, a power-up star, a paper plane, drums, a wall of amps, antlers, a cube of facets, a disco car, bouncing
    ///   balls, a speaker tower, a hill with one star, a cosmic orb, a speedometer, a beating heart and a ring of fire.
    public partial class FloorKit
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
                Add("Avicii", "Levels", "Avicii"); Add("Avicii", "Wake Me Up", "Avicii"); Add("Swedish House Mafia", "Don't You Worry", "@the main stage"); Add("Daft Punk", "Contact", "Daft Punk");
                Add("deadmau5", "Snowcone", "deadmau5"); Add("Wuki", "Ring of Fire", "Wuki"); Add("Vicetone", "Heartbeat", "Vicetone"); Add("Astronaut", "Rain", "Astronaut"); Add("Zero Eden", "Ocean Simulator", "Zero Eden");
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
            if (truss == null) MakeMaterials();
            var root = new GameObject("Artist: " + a.name).transform; root.SetParent(ctx.Root, false);
            var front = facing.TryGetValue(a.name, out var f) ? f : new Vector3(center.x - a.x, 0, center.z - a.z).normalized;
            if (front.sqrMagnitude < .01f) front = Vector3.forward;
            var labels = new GameObject("Labels").transform; labels.SetParent(root, false);
            var area = root.gameObject.AddComponent<ArtistArea>();
            area.artistName = a.name; area.stationId = a.stationId; area.radius = a.r; area.glowColor = Laser;
            var z = new Plot { ctx = ctx, root = root, labels = labels, mk = new MeshKit(), solid = new MeshKit(), pictures = new MeshKit(), a = a, c = ctx.OnGround(a.x, a.z), front = front, side = Vector3.Cross(Vector3.up, front), area = area };
            var reach = Foot(a); var up = Vector3.up;
            z.mk.Prism(ground, z.c - up * .3f, up, reach * .85f, .34f, 26);
            switch (a.name)
            {
                case "deadmau5": Cube(z); break;
                case "Astronaut": Rocket(z); break;
                case "Zero Eden": Eden(z); break;
                default: Others(z, Deck(z)); break;
            }
            SongLamps(z);
            Board(z, z.At(reach * .6f, -reach * .45f - 1.5f), front);
            ArtistBits.Plaque(z.mk, z.solid, labels, a, z.At(reach * .6f + 1.5f, reach * .45f + 1.5f), front, truss, dark, Laser, 1.6f);
            Lamp(ctx, z.mk, z.At(reach * .5f, reach * .7f), false);
            if (z.area.glowLight == null) z.area.glowLight = ArtistBits.PointLight(root, z.c + up * 4f + front * 3f, Neon[a.name.Length % Neon.Length], HasStation(a) ? 24f : 14f, 1.4f);
            area.glowRenderers = new Renderer[0];
            z.mk.Build("Area", root, false);
            z.solid.BuildCollider("Walls", root);
            z.pictures.Build("Covers", root, false);
            noGrass.Add(new Vector3(z.c.x, reach * .9f, z.c.z));
            var mark = new GameObject("Shot: " + a.name + " up close").transform; mark.SetParent(ctx.Root, false);
            var cam = z.At(reach * .85f, reach * .3f) + up * 2f; var look = z.At(-reach * .3f, 0) + up * 1.8f - cam; mark.SetPositionAndRotation(cam, Quaternion.LookRotation(look.normalized));
        }

        /// The covers on a black board with a neon edge.
        void Board(Plot z, Vector3 ground, Vector3 dir)
        {
            dir = Vector3.ProjectOnPlane(dir, Vector3.up).normalized; var rot = Quaternion.LookRotation(dir);
            var n = Mathf.Max(1, Mathf.Min(7, z.a.songList?.Length ?? 1)); const float size = .9f; var w = n * size * 1.25f + .6f;
            var c = ground + Vector3.up * 1.7f;
            z.mk.Box(GlowM(Neon[z.a.name.Length % Neon.Length], 1.2f), c - Vector3.up * .5f - dir * .08f, new Vector3(w + .5f, size + 2f, .14f), rot); z.mk.Box(dark, c - Vector3.up * .5f - dir * .02f, new Vector3(w + .3f, size + 1.8f, .2f), rot); z.solid.Box(dark, c - Vector3.up * .5f, new Vector3(w + .5f, size + 2f, .3f), rot);
            z.mk.Box(Paint(new Color(.9f, .9f, .92f)), c - Vector3.up * .1f + dir * .09f, new Vector3(w, size + .9f, .04f), rot);
            ArtistBits.CoverWall(z.pictures, z.labels, z.ctx, z.a, c + dir * .12f + Vector3.up * .08f, dir, n, size, n, true, Ink);
        }

        /// A glow stick standing in the grass for each song without its own piece.
        void SongLamps(Plot z)
        {
            if (!SongIds.TryGetValue(z.a.name, out var list)) return;
            var up = Vector3.up; var k = 0; var seen = new HashSet<string>();
            foreach (var (_, id) in list)
            {
                if (z.lit.Contains(id) || !seen.Add(id)) continue;
                var p = z.At(Foot(z.a) * .55f - (k % 2) * 1.3f, Foot(z.a) * .2f + (k / 2) * 1.3f);
                z.mk.Prism(SongGlow(id, Neon[k % Neon.Length], 2f), p, up, .06f, 1.2f, 6); ArtistBits.PostCollider(z.root, p, 1.2f, .1f);
                k++;
            }
        }

        /// A little festival stage: a low black deck under a canopy on truss poles, a DJ table, their name in lights.
        PlaceFrame Deck(Plot z)
        {
            var up = Vector3.up; var f = new PlaceFrame(z.At(-Foot(z.a) * .25f, 0), z.front); var col = Neon[(z.a.name.Length + z.a.name[0]) % Neon.Length];
            z.mk.Box(dark, f.P(0, .25f, 0), new Vector3(6f, .5f, 4f), f.r); z.solid.Box(dark, f.P(0, .25f, 0), new Vector3(6f, .5f, 4f), f.r); z.mk.Box(GlowM(col, 1.2f), f.P(0, .5f, 2f), new Vector3(6f, .04f, .04f), f.r);
            foreach (var (x, q) in new[] { (-2.8f, -1.8f), (2.8f, -1.8f), (2.8f, 1.8f), (-2.8f, 1.8f) }) { z.mk.Prism(truss, f.P(x, .5f, q), up, .06f, 3.4f, 4); ArtistBits.PostCollider(z.root, f.P(x, 0, q), 3.9f, .1f); }
            z.mk.Box(Paint(col * .6f), f.P(0, 4f, 0), new Vector3(6.4f, .12f, 4.4f), f.r); z.mk.Box(GlowM(col, 1.4f), f.P(0, 3.8f, 2.22f), new Vector3(6.4f, .5f, .04f), f.r); ArtistBits.Label(z.labels, z.a.name, f.P(0, 3.8f, 2.26f), f.Fwd, .22f, Ink, TextAnchor.MiddleCenter);
            z.mk.Box(dark, f.P(0, 1f, -1f), new Vector3(2f, 1f, .8f), f.r); z.mk.Box(GlowM(col, 1.4f), f.P(0, 1.02f, -.59f), new Vector3(1.8f, .08f, .02f), f.r);
            return f;
        }

        // ---------------------------------------------------------------- deadmau5: the cube
        void Cube(Plot z)
        {
            var up = Vector3.up; var rng = new System.Random(2006); var fr = z.front; var sd = z.side; var rot = z.Rot;
            var jup = Song(z, "Jupiter"); var snow = Song(z, "Snowcone"); var fab = Song(z, "Fabienk"); life.snowSong = snow;
            var cb = z.At(-3f, 0); z.mk.Box(dark, cb + up * .3f, new Vector3(8f, .6f, 8f), rot); z.solid.Box(dark, cb + up * .3f, new Vector3(8f, .6f, 8f), rot);
            var cube = new Material(WorldMaterials.Glow(Blue, 1.2f)) { name = "Floor cube" }; life.cube = cube; var cc = cb + up * 3.4f; z.mk.Box(dark, cc, Vector3.one * 5f, rot); z.solid.Box(dark, cc, Vector3.one * 5f, rot);
            for (var k = 0; k < 4; k++) { var face = Quaternion.Euler(0, k * 90f, 0) * fr; var fs = Vector3.Cross(up, face); for (var x = 0; x < 5; x++) for (var y = 0; y < 5; y++) z.mk.Box((x + y + k) % 3 == 0 ? cube : GlowM(Neon[(x * 3 + y + k) % Neon.Length], .7f), cc + face * 2.51f + fs * (x - 2f) * .96f + up * (y - 2f) * .96f, new Vector3(.9f, .9f, .02f), Quaternion.LookRotation(face)); }
            z.mk.Box(cube, cc + up * 2.51f, new Vector3(4.8f, .02f, 4.8f), rot); halos.Add(cc + fr * 3.4f, 5f, Blue, .25f);
            // "Jupiter": a banded planet floating over the cube
            var jp = new MeshKit(); jp.Blob(SongGlow(jup, new Color(.9f, .66f, .44f), .9f), Vector3.zero, Vector3.one * 1.4f, rng, 0f); for (var k = 0; k < 3; k++) jp.Prism(Paint(new Color(.7f, .5f, .36f)), Vector3.up * (-.6f + k * .5f), Vector3.up, 1.38f - Mathf.Abs(k - 1) * .2f, .12f, 18); var jg = jp.BuildSingle("Jupiter", z.root, false); jg.transform.position = cc + up * 6f; life.spinners.Add((jg.transform, cc + up * 6f));
            // "Snowcone": a giant snow cone; "Fabienk": glowing headphones on a stand
            var sc = z.At(4f, -6f); z.mk.Prism(Paint(new Color(.9f, .86f, .7f)), sc + up * 1.6f, -up, .7f, 1.6f, 10, .05f); z.mk.Blob(SongGlow(snow, new Color(.8f, .9f, 1f), 1.2f), sc + up * 1.9f, new Vector3(.8f, .6f, .8f), rng, .1f); ArtistBits.PostCollider(z.root, sc, 2.4f, .6f);
            var hp = z.At(4f, 6f); z.mk.Prism(truss, hp, up, .05f, 1.6f, 4); for (var k = 0; k <= 8; k++) { var a = Mathf.PI * k / 8f; z.mk.Box(dark, hp + up * (1.9f + Mathf.Sin(a) * .4f) + sd * Mathf.Cos(a) * .4f, new Vector3(.12f, .08f, .1f), rot * Quaternion.Euler(0, 0, a * Mathf.Rad2Deg + 90f)); } foreach (var e in new[] { -.42f, .42f }) z.mk.Prism(SongGlow(fab, Magenta, 1.2f), hp + up * 1.8f + sd * e - sd * Mathf.Sign(e) * .08f, sd * Mathf.Sign(e), .2f, .16f, 12); ArtistBits.PostCollider(z.root, hp, 2.3f, .5f);
            Folk(z.At(6f, 2f), -fr, Resident.Pose.Wave, new[] { "The cube! The cube! It's watching you back.", "Wait for the build. Wait for it.", "Jupiter's up there. Of course it is." }, Local());
            z.area.glowLight = ArtistBits.PointLight(z.root, cc + fr * 5f, Blue, 28f, 2f);
            Shot(z.ctx, "the cube", z.At(Foot(z.a) * 1.1f, 4f) + up * 2.6f, cc);
        }

        // ---------------------------------------------------------------- Astronaut: the launch pad
        void Rocket(Plot z)
        {
            var up = Vector3.up; var rng = new System.Random(2009); var fr = z.front; var sd = z.side; var rot = z.Rot;
            var earth = Song(z, "Earthsphere"); var champ = Song(z, "Champions"); var rain = Song(z, "Rain"); var war = Song(z, "War"); life.rainSong = rain;
            var lp = z.At(-3f, -2f); z.mk.Prism(Paint(new Color(.4f, .4f, .42f)), lp, up, 3f, .6f, 16); z.solid.Prism(truss, lp, up, 3f, .6f, 10);
            var rk = lp + up * .6f; var rw = white; z.mk.Prism(rw, rk + up * 1f, up, .9f, 8f, 14); z.mk.Cone(Paint(new Color(.9f, .3f, .2f)), rk + up * 9f, .9f, 2.4f, 14); for (var k = 0; k < 4; k++) { var d = Quaternion.Euler(0, k * 90f + 45f, 0) * fr; z.mk.Tri(Paint(new Color(.9f, .3f, .2f)), rk + up * 1f + d * .9f, rk + up * 3.4f + d * .9f, rk + d * 2f, rk - d); z.mk.Tri(Paint(new Color(.9f, .3f, .2f)), rk + up * 1f + d * .9f, rk + up * 3.4f + d * .9f, rk + d * 2f, rk + d * 4f); } z.mk.Prism(GlowM(new Color(.5f, .8f, 1f), 1.4f), rk + up * 6f + fr * .89f, fr, .3f, .04f, 12); z.mk.Prism(fireM, rk + up * 1f, -up, .7f, 1f, 12, .3f); z.solid.Prism(rw, rk, up, 1f, 10f, 10);
            var gt = lp + sd * 2.8f; z.mk.Prism(truss, gt, up, .5f, 11f, 4); for (var k = 1; k < 11; k++) z.mk.Box(truss, gt + up * k, new Vector3(1.1f, .05f, 1.1f), rot); z.mk.Box(truss, gt + up * 8f - sd * 1.2f, new Vector3(.1f, .1f, 2f), Quaternion.LookRotation(sd)); ArtistBits.PostCollider(z.root, gt, 11f, .6f); for (var k = 0; k < 4; k++) z.mk.Blob(GlowM(new Color(1f, .2f, .2f), 2f), gt + up * (2f + k * 3f) + fr * .6f, Vector3.one * .08f, rng, 0f);
            // "Earthsphere": a glowing Earth; "Champions": a trophy; "Rain": a rain cloud; "War": a war drum
            var ea = z.At(4f, -5f); z.mk.Prism(truss, ea, up, .05f, 1.4f, 4); z.mk.Blob(SongGlow(earth, new Color(.3f, .6f, 1f), 1f), ea + up * 2.2f, Vector3.one * .8f, rng, 0f); for (var k = 0; k < 4; k++) z.mk.Blob(Paint(new Color(.3f, .6f, .3f)), ea + up * 2.2f + Quaternion.Euler(k * 50f, k * 90f, 0) * fr * .76f, new Vector3(.3f, .2f, .1f), rng, .2f); ArtistBits.PostCollider(z.root, ea, 3f, .8f);
            var tr = z.At(5f, 0f); z.mk.Box(dark, tr + up * .5f, new Vector3(.8f, 1f, .8f), rot); BoxCol(z.root, tr + up * .5f, new Vector3(.8f, 1f, .8f), rot); z.mk.Prism(SongGlow(champ, Amber, 1.4f), tr + up * 1f, up, .1f, .5f, 8, 3f); z.mk.Prism(SongGlow(champ, Amber, 1.4f), tr + up * 1.5f, up, .3f, .4f, 10, .6f);
            var rc = z.At(4f, 5f); z.mk.Prism(truss, rc, up, .04f, 3f, 4); for (var k = 0; k < 4; k++) z.mk.Blob(SongGlow(k == 0 ? rain : null, new Color(.7f, .76f, .86f), .7f), rc + up * 3.4f + sd * (k - 1.5f) * .5f, new Vector3(.6f, .4f, .5f), rng, .2f); for (var k = 0; k < 10; k++) z.mk.Box(GlowM(new Color(.5f, .7f, 1f), 1.2f), rc + up * (1.6f + (k % 4) * .4f) + sd * ((k % 5) - 2f) * .3f, new Vector3(.02f, .3f, .02f), rot); ArtistBits.PostCollider(z.root, rc, 3f, .1f);
            var dr = z.At(1f, 7f); z.mk.Prism(Paint(new Color(.5f, .2f, .16f)), dr, up, .7f, 1f, 14); z.mk.Prism(SongGlow(war, new Color(.9f, .86f, .8f), .8f), dr + up * 1f, up, .7f, .02f, 14); BoxCol(z.root, dr + up * .5f, new Vector3(1.4f, 1f, 1.4f), rot);
            Folk(z.At(6f, -2f), -fr, Resident.Pose.Stand, new[] { "Countdown's been at ten for six hours. Very dramatic.", "When it rains on the pad, it's the good kind of rain.", "We're all astronauts tonight." }, Local());
            z.area.glowLight = ArtistBits.PointLight(z.root, rk + up * 5f + fr * 3f, new Color(.6f, .8f, 1f), 26f, 1.8f);
        }

        // ---------------------------------------------------------------- Zero Eden: the ocean simulator
        void Eden(Plot z)
        {
            var up = Vector3.up; var rng = new System.Random(2020); var fr = z.front; var sd = z.side; var rot = z.Rot;
            var ocean = Song(z, "Ocean Simulator"); var bin = Song(z, "Between Binaries"); var any = Song(z, "Anything Is Possible"); life.oceanSong = ocean;
            var pc = z.At(-2f, 0); z.mk.Box(dark, pc + up * .2f, new Vector3(8f, .4f, 6f), rot); BoxCol(z.root, pc + up * .2f, new Vector3(8f, .4f, 6f), rot);
            var wave = new Material(WorldMaterials.Glow(new Color(.2f, .6f, 1f), 1.2f)) { name = "Floor waves" }; life.waves = wave; for (var x = 0; x < 14; x++) for (var y = 0; y < 10; y++) { var tile = new MeshKit(); tile.Box((x + y) % 4 == 0 ? SongGlow(ocean, new Color(.2f, .6f, 1f), 1.2f) : wave, Vector3.zero, new Vector3(.5f, .06f, .5f), Quaternion.identity); var tg = tile.BuildSingle("Wave tile", z.root, false); var home = pc + up * .45f + sd * (x - 6.5f) * .54f + fr * (y - 4.5f) * .54f; tg.transform.SetPositionAndRotation(home, rot); life.tiles.Add((tg.transform, home, x * .5f + y * .3f)); }
            var bs = new PlaceFrame(z.At(-Foot(z.a) * .7f, 0), fr); z.mk.Box(dark, bs.P(0, 2f, 0), new Vector3(6f, 2.4f, .2f), bs.r); BoxCol(z.root, bs.P(0, 2f, 0), new Vector3(6f, 2.4f, .2f), bs.r); var bits = "0110100101101001"; for (var k = 0; k < bits.Length; k++) z.mk.Box(k == 0 ? SongGlow(bin, Laser, 1.4f) : GlowM(Laser, bits[k] == '1' ? 1.4f : .3f), bs.P(-2.6f + (k % 8) * .74f, 1.6f + (k / 8) * .8f, .11f), new Vector3(bits[k] == '1' ? .14f : .5f, .5f, .02f), bs.r); foreach (var e in new[] { -2.8f, 2.8f }) z.mk.Prism(truss, bs.P(e, 0, 0), up, .05f, 3.2f, 4);
            var dg = new PlaceFrame(z.At(3f, 6f), fr); foreach (var e in new[] { -.7f, .7f }) z.mk.Box(dark, dg.P(e, 1.3f, 0), new Vector3(.2f, 2.6f, .2f), dg.r); z.mk.Box(dark, dg.P(0, 2.6f, 0), new Vector3(1.6f, .2f, .2f), dg.r); z.mk.Box(SongGlow(any, White, 1.2f), dg.P(0, 1.25f, 0), new Vector3(1.2f, 2.4f, .02f), dg.r); BoxCol(z.root, dg.P(0, 1.3f, 0), new Vector3(1.6f, 2.6f, .3f), dg.r); halos.Add(dg.P(0, 1.3f, .6f), 2f, White, .3f);
            Folk(z.At(5f, -3f), -fr, Resident.Pose.Stand, new[] { "It's an ocean, but it's made of light. Don't dive in.", "Walk through the door. Anything is possible. Probably." }, Local());
            z.area.glowLight = ArtistBits.PointLight(z.root, pc + up * 4f, new Color(.3f, .6f, 1f), 22f, 1.6f);
        }

        // ---------------------------------------------------------------- everyone else, on their decks
        void Others(Plot z, PlaceFrame f)
        {
            var up = Vector3.up; var rot = z.Rot; var rng = new System.Random(z.a.name.Length * 7 + z.a.name[0]); var c = f.P(0, .5f, .4f); var fr = z.front; var sd = z.side;
            void Post(Vector3 at, float h, float r = .04f) { z.mk.Prism(truss, at - up * .2f, up, r, h + .2f, 5); }
            string Any() => SongIds.TryGetValue(z.a.name, out var l) && l.Length > 0 ? Song(z, l[0].title) : null;
            switch (z.a.name)
            {
                case "+TEK": { var s = Any(); for (var k = 0; k < 4; k++) { var gp = c + sd * (k - 1.5f) * 1.2f + fr * (k % 2) * .4f; z.mk.Box(Paint(new Color(.3f, .3f, .34f)), gp + up * .4f, new Vector3(.6f, .8f, .16f), rot); z.mk.Prism(Paint(new Color(.3f, .3f, .34f)), gp + up * .8f, fr, .3f, .16f, 10); z.mk.Box(k == 0 ? SongGlow(s, Laser, 1.6f) : GlowM(Laser, 1f), gp + up * .5f + fr * .09f, new Vector3(.3f, .04f, .01f), rot); } break; }
                case "Avicii": { var lv = Song(z, "Levels"); var wake = Song(z, "Wake Me Up"); life.levelsSong = lv; life.wakeSong = wake; for (var k = 0; k < 9; k++) { var h = .3f + Mathf.Abs(Mathf.Sin(k * 1.3f)) * 1.6f; z.mk.Box(k == 4 ? SongGlow(lv, Neon[k % Neon.Length], 1.4f) : GlowM(Neon[k % Neon.Length], 1.2f), c + sd * (k - 4f) * .32f + up * h / 2f, new Vector3(.26f, h, .26f), rot); } z.mk.Prism(SongGlow(wake, Amber, 1.4f), c - fr * 1.4f + up * 2.6f, fr, .9f, .06f, 20); break; }
                case "Barry Can't Swim": { var s = Any(); z.mk.Prism(SongGlow(s, new Color(1f, .4f, .4f), .9f), c + up * .2f, up, .8f, .3f, 16); z.mk.Prism(dark, c + up * .15f, up, .4f, .4f, 16); for (var k = 0; k < 4; k++) z.mk.Box(white, c + up * .36f + Quaternion.Euler(0, k * 90f, 0) * fr * .6f, new Vector3(.3f, .02f, .2f), Quaternion.Euler(0, k * 90f, 0) * rot); break; }
                case "Cash Cash": { var s = Any(); var cf = c - fr * .4f + up * 1.6f; z.mk.Prism(dark, cf, fr, 1.1f, .2f, 20); z.mk.Prism(SongGlow(s, Laser, 1f), cf + fr * .21f, fr, 1f, .02f, 20); z.mk.Box(dark, cf + fr * .24f + up * .3f, new Vector3(.06f, .6f, .02f), rot); z.mk.Box(dark, cf + fr * .24f + sd * .2f, new Vector3(.4f, .06f, .02f), rot); break; }
                case "Chromeo": { var s = Any(); z.mk.Prism(truss, c, up, .03f, 2.2f, 4); var mb = new MeshKit(); mb.Blob(SongGlow(s, White, 1.2f), Vector3.zero, Vector3.one * .5f, rng, 0f); for (var k = 0; k < 20; k++) mb.Box(GlowM(Neon[k % Neon.Length], 1.2f), Quaternion.Euler(k * 37f, k * 71f, 0) * Vector3.forward * .5f, Vector3.one * .1f, Quaternion.Euler(k * 37f, k * 71f, 0)); var mg = mb.BuildSingle("Mirror ball", z.root, false); mg.transform.position = c + up * 2.6f; life.spinners.Add((mg.transform, c + up * 2.6f)); break; }
                case "Daft Punk": { var contact = Song(z, "Contact"); var eol = Song(z, "End of Line"); life.contactSong = contact; var py = c - fr * .2f; for (var k = 0; k < 5; k++) { var w = 2.4f - k * .48f; z.mk.Box(dark, py + up * (k * .4f + .2f), new Vector3(w, .4f, w), rot); foreach (var e in new[] { -1f, 1f }) z.mk.Box(k % 2 == 0 ? SongGlow(contact, White, 1.6f) : GlowM(Blue, 1.4f), py + up * (k * .4f + .2f) + fr * e * (w / 2f + .01f), new Vector3(w, .06f, .02f), rot); } z.mk.Blob(SongGlow(eol, new Color(1f, .3f, .2f), 1.6f), py + up * 2.2f, Vector3.one * .14f, rng, 0f); BoxCol(z.root, py + up * 1f, new Vector3(2.4f, 2f, 2.4f), rot); break; }
                case "Duke": { var s = Any(); for (var k = 0; k < 5; k++) { var lp = c + sd * (k - 2f) * .8f + up * (2.4f + (k % 2) * .3f); z.mk.Box(truss, lp + up * .6f, new Vector3(.02f, .9f, .02f), rot); z.mk.Prism(k == 2 ? SongGlow(s, Amber, 1.6f) : lampM, lp - up * .2f, up, .18f, .4f, 8, .5f); } break; }
                case "Electric Joy Ride": { var s = Any(); var sp = c + up * 1.4f; for (var k = 0; k < 5; k++) { var a = Quaternion.AngleAxis(k * 72f, fr) * up; z.mk.Box(SongGlow(k == 0 ? s : null, Amber, 1.6f), sp + a * .4f, new Vector3(.3f, .8f, .1f), Quaternion.LookRotation(fr, a)); } Post(c, 1f, .04f); break; }
                case "Flight Facilities": { var s = Any(); var pp = c + up * 1.6f; z.mk.Tri(SongGlow(s, White, 1f), pp + fr * .8f, pp - fr * .6f + sd * .6f, pp - fr * .5f, pp - up); z.mk.Tri(SongGlow(s, White, 1f), pp + fr * .8f, pp - fr * .6f - sd * .6f, pp - fr * .5f, pp - up); Post(c, 1.5f, .02f); break; }
                case "Francis Mercier": { var s = Any(); for (var k = 0; k < 3; k++) { var dp = c + sd * (k - 1) * .8f; z.mk.Prism(Paint(new Color(.6f, .36f, .2f)), dp, up, .3f, .9f - k * .1f, 12, .8f); z.mk.Prism(k == 1 ? SongGlow(s, Amber, 1f) : Paint(new Color(.9f, .86f, .76f)), dp + up * (.9f - k * .1f), up, .26f, .02f, 12); } break; }
                case "Justice": { var fl = Song(z, "Flower"); var gen = Song(z, "Genesis"); for (var x = 0; x < 6; x++) for (var y = 0; y < 3; y++) { var ap = c - fr * .6f + sd * (x - 2.5f) * .8f + up * (y * .7f + .35f); z.mk.Box(dark, ap, new Vector3(.76f, .66f, .5f), rot); z.mk.Box(Paint(new Color(.3f, .26f, .2f)), ap + fr * .26f, new Vector3(.6f, .5f, .02f), rot); z.mk.Box(x == 2 && y == 2 ? SongGlow(gen, White, 1.6f) : x == 3 && y == 2 ? SongGlow(fl, new Color(1f, .4f, .6f), 1.6f) : GlowM(White, .6f), ap + fr * .27f + up * .26f, new Vector3(.5f, .04f, .01f), rot); } BoxCol(z.root, c - fr * .6f + up * 1.05f, new Vector3(4.8f, 2.1f, .5f), rot); break; }
                case "Loukeman": { var s = Any(); var hd = c + up * 1.4f; z.mk.Box(Paint(new Color(.5f, .4f, .3f)), hd, new Vector3(.4f, .5f, .6f), rot); foreach (var e in new[] { -1f, 1f }) { var a = hd + up * .2f + sd * e * .2f; for (var k = 0; k < 4; k++) { var dir = (up + sd * e * .6f + fr * (k - 1.5f) * .3f).normalized; z.mk.Prism(SongGlow(k == 0 && e > 0 ? s : null, new Color(.9f, .86f, .7f), .7f), a, dir, .04f, .6f + k * .1f, 4, .4f); } } Post(c, 1.2f, .05f); break; }
                case "Madeon": { var s = Any(); var mc = c + up * 1.6f; for (var k = 0; k < 6; k++) z.mk.Tri(k == 0 ? SongGlow(s, Neon[k % Neon.Length], 1.4f) : GlowM(Neon[k % Neon.Length], 1f), mc, mc + Quaternion.AngleAxis(k * 60f, fr) * up * .9f, mc + Quaternion.AngleAxis(k * 60f + 60f, fr) * up * .9f, mc - fr); Post(c, 1f, .04f); break; }
                case "Metro Area": { var s = Any(); var car = c; z.mk.Box(Paint(new Color(.7f, .2f, .5f)), car + up * .4f, new Vector3(1.4f, .4f, 2.6f), rot); z.mk.Box(GlowM(new Color(.4f, .8f, 1f), .5f), car + up * .8f - fr * .2f, new Vector3(1.2f, .4f, 1.2f), rot); z.mk.Box(SongGlow(s, Amber, 1.6f), car + up * .4f + fr * 1.31f, new Vector3(1.2f, .14f, .02f), rot); foreach (var e in new[] { -.7f, .7f }) foreach (var q in new[] { -.9f, .9f }) z.mk.Prism(dark, car + up * .2f + sd * e + fr * q, sd * Mathf.Sign(e), .22f, .1f, 10); BoxCol(z.root, car + up * .5f, new Vector3(1.4f, 1f, 2.6f), rot); break; }
                case "Rob Gasser": { var s = Any(); for (var k = 0; k < 4; k++) { var bm = new MeshKit(); bm.Blob(k == 0 ? SongGlow(s, Neon[k], 1.6f) : GlowM(Neon[k], 1.4f), Vector3.zero, Vector3.one * .2f, rng, 0f); var bg = bm.BuildSingle("Bouncing ball", z.root, false); var home = c + sd * (k - 1.5f) * .7f; bg.transform.position = home; life.bouncers.Add((bg.transform, home, k * .7f)); } break; }
                case "Sonic Future": { var s = Any(); for (var k = 0; k < 3; k++) { var sp = c + up * (k * .9f + .45f); z.mk.Box(dark, sp, new Vector3(1f, .86f, .8f), rot); z.mk.Prism(Paint(new Color(.2f, .2f, .22f)), sp + fr * .41f, fr, .32f, .02f, 14); z.mk.Prism(k == 1 ? SongGlow(s, Laser, 1.4f) : GlowM(Laser, .9f), sp + fr * .42f, fr, .1f, .02f, 10); } BoxCol(z.root, c + up * 1.35f, new Vector3(1f, 2.7f, .8f), rot); break; }
                case "Swedish House Mafia": { var s = Any(); life.worrySong = s; var hl = c - fr * .4f; z.mk.Blob(Paint(new Color(.3f, .34f, .26f)), hl + up * .3f, new Vector3(1.4f, .6f, 1f), rng, .1f); z.mk.Prism(truss, hl + up * .8f, up, .02f, 2f, 4); for (var k = 0; k < 5; k++) { var a = Quaternion.AngleAxis(k * 72f, fr) * up; z.mk.Box(SongGlow(k == 0 ? s : null, Amber, 1.8f), hl + up * 3f + a * .26f, new Vector3(.12f, .5f, .04f), Quaternion.LookRotation(fr, a)); } halos.Add(hl + up * 3f, 1.8f, Amber, .4f); break; }
                case "TAKIO": { var s = Any(); var oc = c + up * 1.6f; z.mk.Blob(SongGlow(s, new Color(.6f, .3f, 1f), 1.2f), oc, Vector3.one * .5f, rng, .1f); for (var k = 0; k < 14; k++) z.mk.Blob(GlowM(k % 2 == 0 ? Magenta : Blue, 1.2f), oc + Quaternion.Euler(k * 50f, k * 80f, 0) * Vector3.forward * (.8f + (k % 3) * .2f), Vector3.one * .06f, rng, 0f); Post(c, 1f, .04f); break; }
                case "Throttle": { var s = Any(); var sf = c - fr * .2f + up * 1.4f; z.mk.Prism(dark, sf, fr, 1f, .2f, 20); for (var k = 0; k < 9; k++) { var a = Quaternion.AngleAxis(-120f + k * 30f, fr) * up; z.mk.Box(GlowM(k > 6 ? new Color(1f, .3f, .2f) : Laser, 1.2f), sf + fr * .21f + a * .8f, new Vector3(.06f, .2f, .01f), Quaternion.LookRotation(fr, a)); } z.mk.Box(SongGlow(s, new Color(1f, .3f, .2f), 1.6f), sf + fr * .22f + (Quaternion.AngleAxis(70f, fr) * up) * .35f, new Vector3(.06f, .7f, .01f), Quaternion.LookRotation(fr, Quaternion.AngleAxis(70f, fr) * up)); break; }
                case "Vicetone": { var s = Any(); life.heartSong = s; var hm = new MeshKit(); var m = SongGlow(s, new Color(1f, .2f, .4f), 1.6f); hm.Blob(m, new Vector3(-.3f, .2f, 0), Vector3.one * .4f, rng, 0f); hm.Blob(m, new Vector3(.3f, .2f, 0), Vector3.one * .4f, rng, 0f); hm.Prism(m, new Vector3(0, .1f, 0), Vector3.down, .56f, .7f, 4, .02f, 45f); var hg = hm.BuildSingle("Heart", z.root, false); hg.transform.SetPositionAndRotation(c + up * 1.6f, rot); life.hearts.Add(hg.transform); Post(c, 1f, .04f); break; }
                case "Wuki": { var s = Any(); life.fireSong = s; for (var k = 0; k < 12; k++) { var fp = c + Quaternion.Euler(0, k * 30f, 0) * fr * 1.2f; z.mk.Cone(k == 0 ? SongGlow(s, new Color(1f, .5f, .14f), 2f) : fireM, fp, .2f, .6f + (k % 3) * .2f, 5); } life.fireSpots.Add(c + up * .4f); break; }
                default: { var s = Any(); Post(c, 1.4f); z.mk.Blob(SongGlow(s, Laser, 1.6f), c + up * 1.5f, Vector3.one * .2f, rng, 0f); break; }
            }
        }
    }
}
