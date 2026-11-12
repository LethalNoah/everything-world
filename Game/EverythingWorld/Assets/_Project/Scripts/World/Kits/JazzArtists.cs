using System.Collections.Generic;
using UnityEngine;
using static EverythingWorld.World.PlaceTools;

namespace EverythingWorld.World
{
    /// The Old Jazz Quarter's artists. Most have a club doorway of their own (a slice of dark wall, a door under a
    /// small awning with a lamp, a cocktail table with a candle and two chairs) and one thing from their song beside
    /// it, in brass or in light; covers on a white card in a black frame, a plaque, a street lamp.
    /// - Miles Davis: the club itself, The Blue Mode. A red neon marquee with chasing bulbs; inside, blue light and
    ///   smoke, a small stage (a trumpet on a stand in a blue spot, a piano to play, a bass, drums), tables, a bar.
    ///   "So What": the town turns blue.
    /// - Alice Coltrane (the hilltop, above the rain): a round open temple with a gold dome, a harp to play, an organ,
    ///   lotus lamps round the steps, a piano with two candles. Her songs stop the rain and bring out the stars.
    /// - Duke Ellington: a subway car at a platform with a blue A on it, a station entrance with globe lamps, a white
    ///   piano, a jeep with blue lamps. Billie Holiday: one microphone in one light, a gardenia on a stool, empty
    ///   chairs; when her song plays the town's lights go down. Chet Baker: a bench under the one tree with amber
    ///   leaves, a trumpet left on it. Herbie Hancock: a pond with an island and a piano on it, cantaloupes.
    /// - The swing era: Glenn Miller's bandstand, Cab Calloway's call and response, Ella Fitzgerald's little yellow
    ///   basket, Frank Sinatra's lamp post, Nat King Cole's piano and photograph. Soul jazz: Cannonball Adderley's
    ///   three MERCYs, Jimmy Smith's organ with its roots down, Les McCann's scales, Donald Byrd's dominoes.
    ///   The travellers: Django Reinhardt's caravan and a small swing, Dave Brubeck's nine stones, Stan Getz's wave
    ///   pavement and beach umbrella, Moondog on his corner, Sonny Rollins by the bridge, Pharoah Sanders' orbiting
    ///   lights, Joe Henderson's globe, Zoh Amba's clock without hands.
    public partial class JazzKit
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
                Add("Alice Coltrane", "Journey In Satchidananda", "Alice Coltrane"); Add("Billie Holiday", "Strange Fruit", "Billie Holiday"); Add("Miles Davis", "So What", "@the square");
                Add("Duke Ellington", "Take the", "Duke Ellington"); Add("Chet Baker", "Tis Autumn", "Chet Baker");
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
            if (cobble == null) MakeMaterials();
            var root = new GameObject("Artist: " + a.name).transform; root.SetParent(ctx.Root, false);
            var front = facing.TryGetValue(a.name, out var f) ? f : new Vector3(center.x - a.x, 0, center.z - a.z).normalized;
            if (front.sqrMagnitude < .01f) front = Vector3.forward;
            var labels = new GameObject("Labels").transform; labels.SetParent(root, false);
            var area = root.gameObject.AddComponent<ArtistArea>();
            area.artistName = a.name; area.stationId = a.stationId; area.radius = a.r; area.glowColor = Brass;
            var z = new Plot { ctx = ctx, root = root, labels = labels, mk = new MeshKit(), solid = new MeshKit(), pictures = new MeshKit(), a = a, c = ctx.OnGround(a.x, a.z), front = front, side = Vector3.Cross(Vector3.up, front), area = area };
            var reach = Foot(a); var up = Vector3.up;
            z.mk.Prism(cobble, z.c - up * .25f, up, reach * .66f, .33f, 20);
            switch (a.name)
            {
                case "Miles Davis": MilesDavis(z); break;
                case "Alice Coltrane": AliceColtrane(z); break;
                case "Duke Ellington": DukeEllington(z); break;
                case "Billie Holiday": BillieHoliday(z); break;
                case "Chet Baker": ChetBaker(z); break;
                case "Herbie Hancock": HerbieHancock(z); break;
                case "Django Reinhardt": Django(z); break;
                default: Doorway(z); Others(z); break;
            }
            SongLamps(z);
            Board(z, z.At(reach * .6f, -reach * .45f - 1.5f), front);
            ArtistBits.Plaque(z.mk, z.solid, labels, a, z.At(reach * .6f + 1.5f, reach * .45f + 1.5f), front, dark, brassM, Paper, 1.6f);
            StreetLamp(ctx, z.mk, z.At(reach * .5f, reach * .64f), -z.side, true);
            if (z.area.glowLight == null) z.area.glowLight = ArtistBits.PointLight(root, z.c + up * 5f + front * 3f, Brass, HasStation(a) ? 26f : 18f, 2.6f);
            area.glowRenderers = new Renderer[0];
            z.mk.Build("Area", root, false);
            z.solid.BuildCollider("Walls", root);
            z.pictures.Build("Covers", root, false);
            noGrass.Add(new Vector3(z.c.x, reach * .9f, z.c.z));
            if (HasStation(a) || reach >= 11f)
            {
                var mark = new GameObject("Shot: " + a.name + " up close").transform; mark.SetParent(ctx.Root, false);
                var cam = z.At(reach * .85f, reach * .3f) + up * 2f; var look = z.At(-reach * .3f, 0) + up * 2.4f - cam; mark.SetPositionAndRotation(cam, Quaternion.LookRotation(look.normalized));
            }
        }

        /// The covers on a white card in a black frame, a small lamp over it.
        void Board(Plot z, Vector3 ground, Vector3 dir)
        {
            dir = Vector3.ProjectOnPlane(dir, Vector3.up).normalized; var rot = Quaternion.LookRotation(dir);
            var n = Mathf.Max(1, Mathf.Min(7, z.a.songList?.Length ?? 1)); const float size = .9f; var w = n * size * 1.25f + .6f;
            var c = ground + Vector3.up * 1.7f;
            z.mk.Box(dark, c - Vector3.up * .5f - dir * .06f, new Vector3(w + .5f, size + 2f, .2f), rot); z.solid.Box(dark, c - Vector3.up * .5f, new Vector3(w + .5f, size + 2f, .3f), rot); z.mk.Box(white, c - Vector3.up * .1f + dir * .05f, new Vector3(w, size + .9f, .04f), rot);
            z.mk.Prism(iron, c + Vector3.up * (size / 2f + .6f) + dir * .05f, (dir + Vector3.up * .3f).normalized, .03f, .5f, 4); z.mk.Box(lampM, c + Vector3.up * (size / 2f + .72f) + dir * .5f, new Vector3(w * .5f, .08f, .12f), rot);
            ArtistBits.PointLight(z.root, c + dir * 1.6f + Vector3.up * .8f, new Color(1f, .93f, .8f), 5f, 1.5f);
            ArtistBits.CoverWall(z.pictures, z.labels, z.ctx, z.a, c + dir * .08f + Vector3.up * .08f, dir, n, size, n, true, Ink);
        }

        /// A little lamp on a post for each song without its own piece.
        void SongLamps(Plot z)
        {
            if (!SongIds.TryGetValue(z.a.name, out var list)) return;
            var up = Vector3.up; var k = 0; var seen = new HashSet<string>();
            foreach (var (_, id) in list)
            {
                if (z.lit.Contains(id) || !seen.Add(id)) continue;
                var p = z.At(Foot(z.a) * .55f - (k % 2) * 1.3f, Foot(z.a) * .2f + (k / 2) * 1.3f);
                z.mk.Prism(iron, p - up * .1f, up, .04f, 1.3f, 5); z.mk.Box(SongGlow(id, Brass, 2f), p + up * 1.42f, new Vector3(.2f, .28f, .2f), z.Rot); z.mk.Cone(iron, p + up * 1.56f, .18f, .14f, 4, 45f); halos.Add(p + up * 1.42f, 1f, Brass, .5f); ArtistBits.PostCollider(z.root, p, 1.3f, .08f);
                k++;
            }
        }

        /// A club doorway: a slice of dark wall, a door under a small awning with a lamp, a table and two chairs.
        void Doorway(Plot z)
        {
            var up = Vector3.up; var g = z.At(-Foot(z.a) * .36f, 0); var rot = z.Rot; var hue = z.a.name.Length + z.a.name[0]; var wall = WorldMaterials.Solid(Walls[hue % Walls.Length]);
            z.mk.Box(wall, g + up * 1.8f, new Vector3(5f, 3.8f, .4f), rot); z.solid.Box(wall, g + up * 1.8f, new Vector3(5f, 3.8f, .4f), rot); z.mk.Box(grey, g + up * 3.76f + z.front * .05f, new Vector3(5.2f, .2f, .6f), rot);
            z.mk.Box(dark, g + up * 1.1f + z.front * .22f - z.side * 1.2f, new Vector3(1.05f, 2.2f, .06f), rot); z.mk.Box(dark, g + up * 2.5f + z.front * .7f - z.side * 1.2f, new Vector3(1.7f, .08f, 1.1f), rot * Quaternion.Euler(12f, 0, 0)); z.mk.Blob(lampM, g + up * 2.32f + z.front * .5f - z.side * 1.2f, new Vector3(.09f, .1f, .09f), new System.Random(hue), 0f); halos.Add(g + up * 2.2f + z.front * .7f - z.side * 1.2f, 1.2f, Brass, .5f);
            Blind(z.mk, new PlaceFrame(g, z.front), 1.1f, 1.9f, .2f, 1.6f, 1.4f, true, dark);
            var t = g + z.front * 1.5f + z.side * 1.3f; Table(z.ctx, z.mk, t); Chair(z.mk, t + z.side * .85f, -z.side); Chair(z.mk, t - z.side * .85f + z.front * .2f, z.side);
        }

        // ---------------------------------------------------------------- Miles Davis: the club
        void MilesDavis(Plot z)
        {
            var up = Vector3.up; const float W = 14f, D = 11f, H = 4.2f, T = .3f, Door = 2.2f; var fo = new PlaceFrame(z.At(-4f, 0) + up * .16f, z.front); var rng = new System.Random(59);
            var wall = WorldMaterials.Glow(new Color(.15f, .19f, .34f), .3f); var floor = WorldMaterials.Glow(new Color(.16f, .17f, .22f), .2f); var so = Song(z, "So What"); life.soWhatSong = so;
            Room(z.mk, z.solid, fo, W, D, H, T, 0f, Door, wall, floor);
            // outside, the club is as dark as the rest of the street
            var skin = WorldMaterials.Solid(new Color(.1f, .1f, .12f)); var seg = W / 2f - Door / 2f;
            foreach (var e in new[] { -1f, 1f }) { z.mk.Box(skin, fo.P(e * (Door / 2f + seg / 2f), H / 2f, D / 2f + .02f), new Vector3(seg, H, .04f), fo.r); z.mk.Box(skin, fo.P(e * (W / 2f + .02f), H / 2f, 0), new Vector3(.04f, H, D), fo.r); }
            z.mk.Box(skin, fo.P(0, (H + HumanScale.DoorHeight + .2f) / 2f, D / 2f + .02f), new Vector3(Door, H - HumanScale.DoorHeight - .2f, .04f), fo.r); z.mk.Box(skin, fo.P(0, H / 2f, -D / 2f - .02f), new Vector3(W, H, .04f), fo.r);
            // the storey above, dark windows, a water tank
            z.mk.Box(WorldMaterials.Solid(Walls[1]), fo.P(0, H + 2f, 0), new Vector3(W, 3.6f, D), fo.r); z.solid.Box(wall, fo.P(0, H + 2f, 0), new Vector3(W, 3.6f, D), fo.r); z.mk.Box(grey, fo.P(0, H + 3.9f, 0), new Vector3(W + .3f, .24f, D + .3f), fo.r);
            var glass = WorldMaterials.Solid(new Color(.09f, .1f, .13f), .9f); var ff = new PlaceFrame(fo.P(0, 0, 0), z.front); for (var k = 0; k < 4; k++) Blind(z.mk, ff, -4.8f + k * 3.2f, H + 2.1f, D / 2f, 1.4f, 1.8f, k == 2, glass);
            // the marquee: a canopy over the door, the name in red neon, bulbs chasing along its edge, a blade sign above
            z.mk.Box(dark, fo.P(0, 3.3f, D / 2f + 1.3f), new Vector3(7f, .6f, 2.6f), fo.r); foreach (var e in new[] { -1f, 1f }) { z.mk.Prism(iron, fo.P(e * 3.2f, -.16f, D / 2f + 2.3f), up, .06f, 3.2f, 6); ArtistBits.PostCollider(z.root, fo.P(e * 3.2f, -.16f, D / 2f + 2.3f), 3.2f, .08f); }
            z.mk.Box(neonM, fo.P(0, 3.62f, D / 2f + 2.62f), new Vector3(7f, .04f, .04f), fo.r); z.mk.Box(neonM, fo.P(0, 2.98f, D / 2f + 2.62f), new Vector3(7f, .04f, .04f), fo.r); ArtistBits.Label(z.labels, "THE BLUE MODE", fo.P(0, 3.3f, D / 2f + 2.64f), z.front, .4f, Red, TextAnchor.MiddleCenter);
            for (var k = 0; k < 13; k++) { z.mk.Blob(bulbM[k % 3], fo.P(-3.3f + k * .55f, 2.96f, D / 2f + 2.4f), Vector3.one * .07f, rng, 0f); if (k % 3 == 0) halos.Add(fo.P(-3.3f + k * .55f, 2.9f, D / 2f + 2.4f), .6f, Brass, .3f); }
            for (var k = 0; k < 5; k++) foreach (var e in new[] { -1f, 1f }) z.mk.Blob(bulbM[(k + 1) % 3], fo.P(e * 3.46f, 2.96f, D / 2f + .3f + k * .5f), Vector3.one * .07f, rng, 0f);
            var bl = fo.P(-W / 2f + 1f, H + 2.2f, D / 2f + .7f); z.mk.Box(dark, bl, new Vector3(.9f, 3.6f, .2f), fo.r); z.mk.Box(iron, bl - z.front * .4f, new Vector3(.08f, .08f, .7f), fo.r); foreach (var e in new[] { -1f, 1f }) z.mk.Box(neonM, bl + z.side * e * .4f + z.front * .11f, new Vector3(.04f, 3.4f, .02f), fo.r);
            ArtistBits.Label(z.labels, "J\nA\nZ\nZ", bl + z.front * .12f, z.front, .6f, Red, TextAnchor.MiddleCenter); halos.Add(bl + z.front * .7f, 2.4f, Red, .5f); halos.Add(fo.P(0, 3.3f, D / 2f + 3.3f), 3.4f, Red, .45f);
            ArtistBits.PointLight(z.root, fo.P(0, 2.4f, D / 2f + 3.4f), new Color(1f, .35f, .3f), 14f, 2.6f);
            foreach (var e in new[] { -1f, 1f }) { z.mk.Box(dark, fo.P(e * 2.3f, 1.7f, D / 2f + .04f), new Vector3(1.1f, 1.6f, .06f), fo.r); z.mk.Box(white, fo.P(e * 2.3f, 1.7f, D / 2f + .08f), new Vector3(.9f, 1.4f, .02f), fo.r); ArtistBits.Label(z.labels, e < 0 ? "TONIGHT\n\nQUINTET\n\ntwo sets" : "NO COVER\n\nNO TALKING\nduring the\nbass solo", fo.P(e * 2.3f, 1.7f, D / 2f + .1f), z.front, .1f, Ink, TextAnchor.MiddleCenter); }
            Folk(fo.P(4.4f, -.16f, D / 2f + 1.2f), z.front, Resident.Pose.Stand, new[] { "Go on in. Sit anywhere. Keep it down during the ballads.", "They've been playing the same tune for forty minutes. It hasn't repeated once.", "No list. No rope. If you can hear it from the square, you're invited." });
            // inside: blue light and smoke; the stage along the back wall
            var sz = -D / 2f + T + 1.7f; z.mk.Box(WorldMaterials.Solid(new Color(.2f, .2f, .23f)), fo.P(0, .15f, sz), new Vector3(9f, .3f, 3.2f), fo.r); z.solid.Box(floor, fo.P(0, .05f, sz), new Vector3(9f, .5f, 3.2f), fo.r); z.mk.Box(WorldMaterials.Solid(new Color(.1f, .1f, .16f)), fo.P(0, H / 2f, -D / 2f + T + .04f), new Vector3(9.4f, H - .3f, .06f), fo.r);
            z.mk.Prism(blueM, fo.P(0, .31f, sz + .3f), up, 1.1f, .01f, 16); var ts = fo.P(0, .3f, sz + .3f); z.mk.Prism(iron, ts, up, .16f, .03f, 8); z.mk.Prism(iron, ts, up, .015f, .9f, 4); Trumpet(z.mk, ts + up * .95f, Quaternion.LookRotation((z.side + up * .5f).normalized), brassM); halos.Add(ts + up * 1.1f, 1.4f, Blue, .5f);
            z.mk.Cone(iron, fo.P(0, H - .5f, sz + .3f), .3f, .4f, 8); z.mk.Blob(blueM, fo.P(0, H - .56f, sz + .3f), new Vector3(.18f, .08f, .18f), rng, 0f);
            // the piano, to play: two chords
            var pp = fo.P(-3f, .3f, sz - .2f); var pr = fo.r * Quaternion.Euler(0, 90f, 0); z.mk.Box(dark, pp + up * .65f, new Vector3(1.5f, 1.3f, .6f), pr); z.mk.Box(white, pp + up * .78f + pr * Vector3.forward * .36f, new Vector3(1.4f, .04f, .2f), pr); BoxCol(z.root, pp + up * .65f, new Vector3(1.5f, 1.3f, .7f), pr);
            var stool = pp + pr * Vector3.forward * 1f; z.mk.Prism(iron, stool, up, .04f, .46f, 5); z.mk.Prism(dark, stool + up * .46f, up, .2f, .07f, 10); Interact.Seat(tlife.transform, stool + up * .55f, pr * Vector3.back, "Sit at the piano").seatHeight = .55f;
            string[] chords = { "Two chords. The bass answers: so... what.", "You lay out for eight bars. That's the best thing you've played all night.", "One voicing, all fourths. The room goes a shade bluer.", "You play less. Then less than that. Somebody at the bar says \"yeah\"." }; var cn = 0;
            Spot("The club's piano", pp + up * 1f + pr * Vector3.forward * .4f, "Play two chords", new Vector3(1.6f, 1.2f, 1f)).onUse = _ => { life.played = Time.time; Say1(pp + up * 2.6f, chords[cn++ % chords.Length]); };
            Bass(z.mk, fo.P(2.4f, .3f, sz - .3f), fo.r, rng); Folk(fo.P(3.1f, .3f, sz - .2f), z.front, Resident.Pose.Stand, new[] { "(He plays the question. Eight notes, walking up.)", "\"The answer's two chords,\" he says. \"It's always two chords.\"" });
            var dk = fo.P(.3f, .3f, sz - .9f); var shell = WorldMaterials.Solid(new Color(.75f, .76f, .8f), .8f); z.mk.Prism(shell, dk + up * .3f - z.front * .2f, z.front, .42f, .4f, 12); z.mk.Prism(shell, dk + up * .5f + z.side * .7f, up, .26f, .2f, 10); z.mk.Prism(brassM, dk + up * 1.15f - z.side * .8f, up, .32f, .02f, 12); z.mk.Prism(iron, dk - z.side * .8f, up, .015f, 1.15f, 4);
            // tables with candles, chairs to sit on, people listening
            var tn = 0; foreach (var (x, zz) in new[] { (-3.6f, 1.4f), (-.6f, 2.2f), (2.2f, 1f), (-2.2f, 3.9f) })
            {
                var tp = fo.P(x, 0, zz); Table(z.ctx, z.mk, tp); Chair(z.mk, tp + z.side * .85f, (-z.front - z.side * .3f).normalized); Chair(z.mk, tp - z.side * .85f, (-z.front + z.side * .3f).normalized);
                if (tn == 0) Folk(tp + z.side * .85f + up * .5f, -z.front, Resident.Pose.Sit, new[] { "Shh.", "He turned his back on us for the whole solo. Best thing I ever saw.", "Listen to what he's not playing." }, null, null, .5f);
                if (tn == 2) Folk(tp - z.side * .85f + up * .5f, -z.front, Resident.Pose.Sit, new[] { "I came in out of the rain in about nineteen fifty-nine. Haven't found a reason to leave.", "Kind of slow. Kind of quiet. Kind of perfect." }, Local(Residents.Tool.Mug), null, .5f);
                tn++;
            }
            // the bar down the right wall
            var bx = W / 2f - T - .7f; z.mk.Box(WorldMaterials.Solid(new Color(.24f, .24f, .27f)), fo.P(bx - .5f, .55f, 1.6f), new Vector3(.7f, 1.1f, 5f), fo.r); z.mk.Box(brassM, fo.P(bx - .5f, 1.12f, 1.6f), new Vector3(.9f, .05f, 5.2f), fo.r); BoxCol(z.root, fo.P(bx - .5f, .55f, 1.6f), new Vector3(.9f, 1.1f, 5.2f), fo.r);
            for (var k = 0; k < 9; k++) z.mk.Prism(WorldMaterials.Glow(k % 3 == 0 ? Brass : Silver, .3f), fo.P(bx + .5f, 1.5f + (k % 2) * .5f, -.4f + k * .5f), up, .05f, .3f, 6); z.mk.Box(dark, fo.P(bx + .5f, 1.45f, 1.6f), new Vector3(.2f, .05f, 4.6f), fo.r); z.mk.Box(dark, fo.P(bx + .5f, 1.95f, 1.6f), new Vector3(.2f, .05f, 4.6f), fo.r);
            Folk(fo.P(bx + .1f, 0, 1.2f), -z.side, Resident.Pose.Stand, new[] { "What are you having? Take it to a table. Mind the cymbal.", "House rule: you can clap, but you have to mean it.", "The ice melts slower in here. It's the tempo." }, Local(Residents.Tool.None, true));
            for (var k = 0; k < 3; k++) { var gm = new MeshKit(); gm.Prism(WorldMaterials.Glow(k == 1 ? new Color(.6f, .75f, 1f) : Brass, .6f), Vector3.zero, Vector3.up, .04f, .14f, 8, 1.2f); Thing(gm, "Drink", k == 1 ? "something blue, with ice" : "a short amber drink", fo.P(bx - .5f, 1.16f, .2f + k * 1.1f), Quaternion.identity, new Vector3(.1f, .16f, .1f), .15f, false); }
            ArtistBits.PointLight(z.root, fo.P(0, H - .7f, sz + 1f), Blue, 13f, 2.6f, true); ArtistBits.PointLight(z.root, fo.P(1f, 2.2f, 2.4f), new Color(.8f, .85f, 1f), 9f, 1.1f, true);
            z.area.glowLight = ArtistBits.PointLight(z.root, fo.P(0, 5f, D / 2f + 5f), new Color(1f, .5f, .45f), 24f, 2.4f); life.smoke.Add(fo.P(0, 2.6f, .5f));
            Mark(fo.o, 9f);
            Shot(z.ctx, "the blue mode", fo.P(-7f, 2f, D / 2f + 13f), fo.P(0, 3.4f, D / 2f));
            Shot(z.ctx, "inside the blue mode", fo.P(3.6f, 1.7f, D / 2f - 1f), fo.P(-1f, 1.2f, sz));
        }

        // ---------------------------------------------------------------- Alice Coltrane: the temple above the rain
        void AliceColtrane(Plot z)
        {
            var up = Vector3.up; var rot = z.Rot; var c = z.At(-3f, 0); var rng = new System.Random(71); var saffron = WorldMaterials.Glow(new Color(1f, .55f, .12f), .45f);
            var j1 = Song(z, "Journey In"); var j2 = Song(z, "Journey to"); var tu = Song(z, "Turiya"); foreach (var s in new[] { j1, j2, tu }) if (s != null) life.aliceSongs.Add(s); life.aliceAt = c;
            // three round steps, eight white columns, a ring, a gold dome
            for (var k = 0; k < 3; k++) { z.mk.Prism(k == 2 ? white : stoneM, c + up * (k * .2f - .1f), up, 9f - k * .7f, .3f, 28); z.solid.Prism(stoneM, c + up * (k * .2f - .4f), up, 9f - k * .7f, .6f, 16); }
            var fl = c + up * .5f; z.mk.Prism(goldM, fl + up * .005f, up, 2.4f, .01f, 24); z.mk.Prism(white, fl + up * .01f, up, 2.1f, .01f, 24);
            for (var k = 0; k < 8; k++) { var d = Quaternion.Euler(0, k * 45f + 22.5f, 0) * z.front; var p = fl + d * 6.6f; z.mk.Prism(white, p, up, .32f, 5f, 10, .85f); z.mk.Prism(goldM, p + up * 4.9f, up, .4f, .16f, 10); z.mk.Prism(stoneM, p, up, .44f, .2f, 10); ArtistBits.PostCollider(z.root, p, 5f, .34f); }
            z.mk.Prism(white, fl + up * 5.06f, up, 7.3f, .4f, 28); z.mk.Cone(goldM, fl + up * 5.46f, 7f, 3.4f, 20); z.mk.Prism(goldM, fl + up * 8.7f, up, .1f, 1.2f, 6); z.mk.Blob(goldM, fl + up * 10f, Vector3.one * .3f, rng, 0f);
            halos.Add(fl + up * 8f, 9f, Gold, .4f); halos.Add(fl + up * 38f, 34f, Gold, .16f);
            z.area.glowLight = ArtistBits.PointLight(z.root, fl + up * 3.6f, new Color(1f, .8f, .5f), 34f, 3.4f); ArtistBits.PointLight(z.root, fl + up * 2f + z.front * 9f, Gold, 16f, 1.8f);
            // saffron cloth between the back columns
            for (var k = 0; k < 3; k++) { var a = Quaternion.Euler(0, 135f + k * 45f + 22.5f, 0) * z.front; var b = Quaternion.Euler(0, 180f + k * 45f + 22.5f, 0) * z.front; var m = fl + (a + b) * 3.3f + up * 3.2f; z.mk.Box(saffron, m, new Vector3(Vector3.Distance(a, b) * 6.6f - .8f, 3f, .04f), Quaternion.LookRotation(-(a + b).normalized)); }
            // the harp, to play
            var hp = fl - z.side * 2.2f - z.front * .6f; var hm = new MeshKit(); hm.Prism(goldM, Vector3.zero, new Vector3(0, 1f, .12f).normalized, .06f, 1.9f, 6); hm.Prism(goldM, new Vector3(0, .12f, 0), new Vector3(0, .5f, 1f).normalized, .07f, 1.3f, 6); hm.Prism(goldM, new Vector3(0, 1.86f, .22f), new Vector3(0, -.46f, 1f).normalized, .05f, 1.1f, 6);
            var strings = SongGlow(j1, new Color(1f, .9f, .6f), 1.6f); for (var k = 0; k < 11; k++) { var t = (k + 1) / 12f; var a = new Vector3(0, 1.82f - t * .5f, .26f + t * 1f); var b = new Vector3(0, .14f + t * .56f, .1f + t * 1.05f); hm.Prism(strings, b, (a - b).normalized, .008f, (a - b).magnitude, 3); }
            var hgo = hm.BuildSingle("The harp", tlife.transform, false); hgo.transform.SetPositionAndRotation(hp, rot * Quaternion.Euler(0, 60f, 0)); BoxCol(z.root, hp + up * .9f, new Vector3(.5f, 1.8f, 1.4f), rot * Quaternion.Euler(0, 60f, 0));
            string[] gliss = { "One long glissando, bottom to top. The rain forgets itself.", "The strings ring on after you stop. They seem to be listening to something further off.", "A slow wave of notes. For a moment the hill feels very high and very still.", "You brush the strings and the gold in the dome brightens, like breath on an ember." }; var gn = 0;
            Interact.Toy(hgo, Toy.Kind.Custom, "Play the harp", new Vector3(1f, 2f, 1.6f)).onUse = _ => { life.played = Time.time; Say1(hp + up * 2.8f, gliss[gn++ % gliss.Length]); };
            var cu = hp + z.front * 1.2f + z.side * .5f; z.mk.Prism(saffron, cu, up, .5f, .18f, 12); Interact.Seat(tlife.transform, cu + up * .2f, (hp - cu).normalized, "Sit on the cushion").seatHeight = .2f;
            // the organ, and a rug
            var og = fl + z.side * 2.4f - z.front * .8f; var orot = rot * Quaternion.Euler(0, -30f, 0); z.mk.Box(WorldMaterials.Solid(new Color(.3f, .22f, .16f)), og + up * .55f, new Vector3(1.4f, 1.1f, .7f), orot); z.mk.Box(white, og + up * .98f + orot * Vector3.forward * .3f, new Vector3(1.2f, .04f, .2f), orot); z.mk.Box(white, og + up * 1.1f + orot * Vector3.forward * .14f, new Vector3(1.2f, .04f, .2f), orot); BoxCol(z.root, og + up * .6f, new Vector3(1.4f, 1.2f, .8f), orot);
            var os = og + orot * Vector3.forward * 1f; z.mk.Box(WorldMaterials.Solid(new Color(.3f, .22f, .16f)), os + up * .25f, new Vector3(1f, .5f, .4f), orot); Interact.Seat(tlife.transform, os + up * .52f, orot * Vector3.back, "Sit at the organ").seatHeight = .52f;
            z.mk.Box(WorldMaterials.Glow(new Color(.6f, .14f, .16f), .25f), fl + up * .02f + z.front * 2.6f, new Vector3(4.4f, .03f, 2.4f), rot); z.mk.Box(saffron, fl + up * .03f + z.front * 2.6f, new Vector3(3.8f, .03f, 1.8f), rot);
            // lotus lamps round the steps: the journey
            for (var k = 0; k < 12; k++) { var d = Quaternion.Euler(0, k * 30f, 0) * z.front; var p = c + d * 9.8f; p.y = z.ctx.Ground(p.x, p.z); var lg = SongGlow(k % 2 == 0 ? j1 : j2, Gold, 1.8f); for (var q = 0; q < 6; q++) z.mk.Cone(lg, p + up * .12f + Quaternion.Euler(0, q * 60f, 0) * Vector3.forward * .16f, .12f, .3f, 4, q * 60f); z.mk.Prism(stoneM, p - up * .1f, up, .34f, .22f, 8); if (k % 2 == 0) halos.Add(p + up * .3f, 1.1f, Gold, .5f); }
            // "Turiya & Ramakrishna": a piano out front with two candles
            var pg = z.At(6f, -8f); Grand(z.root, z.mk, pg, rot * Quaternion.Euler(0, 140f, 0), dark, white); var cg = SongGlow(tu, new Color(1f, .8f, .45f), 2.2f); foreach (var e in new[] { -.4f, .4f }) { z.mk.Prism(white, pg + up * 1.02f + z.side * e, up, .03f, .22f, 6); z.mk.Blob(cg, pg + up * 1.3f + z.side * e, new Vector3(.035f, .06f, .035f), rng, 0f); halos.Add(pg + up * 1.3f + z.side * e, .7f, Gold, .5f); }
            Bench(z.mk, z.At(7.5f, 7f), (-z.front - z.side * .5f).normalized);
            Folk(z.At(8.5f, 2.5f), -z.front, Resident.Pose.Stand, new[] { "It doesn't rain up here. Look down: you can see it falling on everybody else.", "I walked up for the view and stayed for the quiet.", "When she plays, the clouds open. I've stopped being surprised. I haven't stopped coming." });
            Shot(z.ctx, "the temple on the hill", z.At(15f, 6f) + up * 2.4f, fl + up * 4f);
        }

        // ---------------------------------------------------------------- Duke Ellington: the A train
        void DukeEllington(Plot z)
        {
            var up = Vector3.up; var rot = z.Rot; var rng = new System.Random(41); var at = Song(z, "Take the"); var jb = Song(z, "Jeep's Blues"); life.trainSong = at;
            // the subway car at its platform: lit windows, doors, a blue A
            var car = z.At(-6.5f, 0) + up * .5f; var steel = WorldMaterials.Solid(new Color(.46f, .47f, .5f), .7f); var trainM = new Material(WorldMaterials.Glow(new Color(1f, .95f, .8f), 1.1f)) { name = "A train windows" }; life.train = trainM;
            z.mk.Box(steel, car + up * 1.5f, new Vector3(13f, 2.6f, 2.7f), rot); z.solid.Box(steel, car + up * 1.2f, new Vector3(13f, 3.4f, 2.7f), rot); z.mk.Box(dark, car + up * 2.9f, new Vector3(12.6f, .24f, 2.3f), rot); z.mk.Box(dark, car - up * .1f, new Vector3(12f, .6f, 2.2f), rot);
            for (var k = 0; k < 9; k++) { if (k == 2 || k == 6) { z.mk.Box(WorldMaterials.Solid(new Color(.3f, .31f, .34f), .7f), car + up * 1.3f + z.side * (-5.2f + k * 1.3f) + z.front * 1.36f, new Vector3(1.1f, 2.1f, .04f), rot); continue; } z.mk.Box(trainM, car + up * 1.8f + z.side * (-5.2f + k * 1.3f) + z.front * 1.36f, new Vector3(.9f, .8f, .04f), rot); }
            z.mk.Prism(blueM, car + up * 1.8f + z.side * 6.1f + z.front * 1.37f, z.front, .36f, .03f, 16); ArtistBits.Label(z.labels, "A", car + up * 1.8f + z.side * 6.1f + z.front * 1.41f, z.front, .5f, Paper, TextAnchor.MiddleCenter); halos.Add(car + up * 1.8f + z.side * 6.1f + z.front * 1.8f, 1.2f, Blue, .5f);
            foreach (var e in new[] { -1f, 1f }) { z.mk.Blob(lampM, car + up * .9f + z.side * e * 6.52f + z.front * .7f, Vector3.one * .12f, rng, 0f); z.mk.Blob(lampM, car + up * .9f + z.side * e * 6.52f - z.front * .7f, Vector3.one * .12f, rng, 0f); }
            for (var k = 0; k < 9; k++) z.mk.Box(dark, car - up * .46f + z.side * (-6.4f + k * 1.6f), new Vector3(.3f, .12f, 3.2f), rot); foreach (var e in new[] { -.75f, .75f }) z.mk.Box(steel, car - up * .38f + z.front * e, new Vector3(14f, .1f, .08f), rot);
            z.mk.Box(stoneM, car - up * .3f + z.front * 2.5f, new Vector3(13f, .4f, 2f), rot); z.solid.Box(stoneM, car - up * .5f + z.front * 2.5f, new Vector3(13f, .8f, 2f), rot); z.mk.Box(WorldMaterials.Glow(new Color(.95f, .9f, .6f), .3f), car - up * .09f + z.front * 1.65f, new Vector3(13f, .02f, .2f), rot);
            var sgn = car + up * 2.6f + z.front * 3.3f - z.side * 3f; foreach (var e in new[] { -1.2f, 1.2f }) { z.mk.Prism(iron, sgn - up * 2.7f + z.side * e, up, .05f, 3f, 5); ArtistBits.PostCollider(z.root, sgn - up * 2.7f + z.side * e, 2.8f, .07f); } z.mk.Box(dark, sgn, new Vector3(2.8f, .6f, .08f), rot); ArtistBits.Label(z.labels, "UPTOWN  ·  SUGAR HILL", sgn + z.front * .05f, z.front, .16f, Paper, TextAnchor.MiddleCenter);
            Bench(z.mk, car - up * .1f + z.front * 2.9f + z.side * 3f, z.front);
            ArtistBits.PointLight(z.root, car + up * 2.4f + z.front * 3f, new Color(1f, .95f, .8f), 16f, 2.2f); halos.Add(car + up * 1.8f + z.front * 2f, 4f, Paper, .2f);
            // the station entrance: railings round a stair down, two globe lamps
            var en = z.At(5f, -8.5f); z.mk.Box(dark, en + up * .05f, new Vector3(2.2f, .1f, 4f), rot); for (var k = 0; k < 5; k++) z.mk.Box(stoneM, en + up * (.02f - k * .0f) - z.front * (1.4f - k * .7f) + up * (.1f - k * .02f), new Vector3(1.9f, .06f, .5f), rot);
            foreach (var e in new[] { -1f, 1f }) { z.mk.Box(iron, en + up * 1f + z.side * e * 1.1f, new Vector3(.06f, .06f, 4f), rot); for (var k = 0; k < 6; k++) z.mk.Prism(iron, en + z.side * e * 1.1f + z.front * (-2f + k * .8f), up, .025f, 1f, 4); BoxCol(z.root, en + up * .55f + z.side * e * 1.1f, new Vector3(.1f, 1.1f, 4f), rot); var gp = en + z.side * e * 1.1f + z.front * 2f; z.mk.Prism(iron, gp, up, .05f, 2.6f, 6); z.mk.Blob(SongGlow(at, new Color(.8f, .95f, .85f), 2f), gp + up * 2.8f, Vector3.one * .24f, rng, 0f); halos.Add(gp + up * 2.8f, 1.4f, Paper, .5f); ArtistBits.PostCollider(z.root, gp, 2.6f, .07f); }
            BoxCol(z.root, en + up * .55f - z.front * 2f, new Vector3(2.2f, 1.1f, .1f), rot); z.mk.Box(iron, en + up * 1f - z.front * 2f, new Vector3(2.2f, .06f, .06f), rot); ArtistBits.Label(z.labels, "SUBWAY", en + up * 2.2f + z.front * 2f, z.front, .22f, Paper, TextAnchor.MiddleCenter);
            // the Duke's white piano, to sit at
            var pg = z.At(2.5f, 1f); Grand(z.root, z.mk, pg, rot * Quaternion.Euler(0, 200f, 0), white, white); var st = pg + rot * Quaternion.Euler(0, 200f, 0) * Vector3.forward * 1.5f; z.mk.Box(dark, st + up * .25f, new Vector3(.9f, .5f, .4f), rot * Quaternion.Euler(0, 200f, 0)); Interact.Seat(tlife.transform, st + up * .52f, (pg - st).normalized, "Sit at the piano").seatHeight = .52f;
            string[] duke = { "A big easy chord with a wink in it. Somewhere, a whole band comes in behind you.", "You play the train: chug, chug, and a whistle up top.", "Elegant. That's the word. Even the wrong notes arrive in a good suit." }; var dn = 0;
            Spot("The Duke's piano", pg + up * 1.1f, "Play something elegant", new Vector3(1.8f, 1.2f, 2.2f)).onUse = _ => { life.played = Time.time; Say1(pg + up * 2.8f, duke[dn++ % duke.Length]); };
            z.area.glowLight = ArtistBits.PointLight(z.root, pg + up * 4f, new Color(1f, .93f, .8f), 24f, 2.6f);
            // "Jeep's Blues": a jeep with blue lamps
            var jp = z.At(5f, 9f); var jr = rot * Quaternion.Euler(0, -35f, 0); var jf = jr * Vector3.forward; var js = jr * Vector3.right; var paint = WorldMaterials.Solid(new Color(.36f, .37f, .38f));
            z.mk.Box(paint, jp + up * .75f, new Vector3(1.5f, .5f, 3.2f), jr); z.mk.Box(paint, jp + up * 1.05f + jf * 1f, new Vector3(1.4f, .3f, 1.1f), jr); z.mk.Box(WorldMaterials.Solid(new Color(.2f, .22f, .26f), .9f), jp + up * 1.45f + jf * .4f, new Vector3(1.4f, .6f, .05f), jr * Quaternion.Euler(-12f, 0, 0)); z.mk.Box(dark, jp + up * 1.05f - jf * .5f, new Vector3(1.3f, .3f, .5f), jr);
            foreach (var e in new[] { -1f, 1f }) { foreach (var s in new[] { -1f, 1.1f }) z.mk.Prism(dark, jp + up * .4f + js * e * .7f + jf * s, js * e, .4f, .24f, 10); z.mk.Blob(SongGlow(jb, Blue, 2.4f), jp + up * .95f + js * e * .5f + jf * 1.62f, Vector3.one * .1f, rng, 0f); halos.Add(jp + up * .95f + js * e * .5f + jf * 1.9f, 1f, Blue, .6f); }
            BoxCol(z.root, jp + up * .8f, new Vector3(1.6f, 1.5f, 3.3f), jr); Interact.Seat(tlife.transform, jp + up * 1.05f - jf * .3f + js * .35f, jf, "Sit in the jeep").seatHeight = .5f;
        }

        // ---------------------------------------------------------------- Billie Holiday: one light
        void BillieHoliday(Plot z)
        {
            var up = Vector3.up; var rot = z.Rot; var c = z.At(-2f, 0); var rng = new System.Random(39); var s = Song(z, "Strange Fruit"); life.billieSong = s;
            // a low round stage, a microphone, a single light from above; nothing else is lit
            z.mk.Prism(dark, c - up * .1f, up, 3.2f, .4f, 24); z.solid.Prism(dark, c - up * .4f, up, 3.2f, .7f, 14); var light = SongGlow(s, new Color(1f, .97f, .9f), 1.5f); z.mk.Prism(light, c + up * .305f, up, 1.5f, .01f, 20);
            Mic(z.root, z.mk, c + up * .3f); var post = c - z.front * 3.6f; z.mk.Prism(iron, post - up * .2f, up, .08f, 6.4f, 6); z.mk.Prism(iron, post + up * 6.1f, (z.front + up * .1f).normalized, .05f, 3.4f, 5); z.mk.Cone(iron, c + up * 6.2f, .4f, .5f, 8); z.mk.Blob(light, c + up * 6.16f, new Vector3(.2f, .1f, .2f), rng, 0f); ArtistBits.PostCollider(z.root, post, 6f, .1f);
            halos.Add(c + up * 6f, 1.6f, Paper, .6f); halos.Add(c + up * 1.4f, 2.2f, Paper, .22f); z.area.glowLight = ArtistBits.PointLight(z.root, c + up * 4.6f, new Color(1f, .97f, .92f), 12f, 3.2f);
            // a white gardenia on a stool
            var stl = c + up * .3f + z.side * 1.1f - z.front * .2f; z.mk.Prism(iron, stl, up, .03f, .7f, 4); z.mk.Prism(dark, stl + up * .7f, up, .18f, .05f, 10); var petal = SongGlow(s, new Color(1f, 1f, .96f), 1.2f); for (var k = 0; k < 7; k++) z.mk.Blob(petal, stl + up * .8f + Quaternion.Euler(0, k * 51f, 0) * Vector3.forward * (k == 0 ? 0f : .06f), new Vector3(.06f, .03f, .06f), rng, .2f); z.mk.Blob(leafDark, stl + up * .77f + z.side * .1f, new Vector3(.07f, .015f, .04f), rng, 0f);
            // chairs set out in the dark, to sit and listen
            for (var k = 0; k < 6; k++) { var d = Quaternion.Euler(0, -50f + k * 20f, 0) * z.front; var p = c + d * (6.2f + (k % 2) * 1.2f); p.y = z.ctx.Ground(p.x, p.z) + .08f; Chair(z.mk, p, -d); }
            Spot("Billie Holiday's light", c + up * 1.4f, "Stand a moment", new Vector3(2.4f, 2.4f, 2.4f)).onUse = _ => Say1(c + up * 3f, "Nobody speaks here. The rain sounds further away.");
            Shot(z.ctx, "one light", z.At(8.5f, -3f) + up * 1.8f, c + up * 2.2f);
        }

        // ---------------------------------------------------------------- Chet Baker: 'Tis Autumn
        void ChetBaker(Plot z)
        {
            var up = Vector3.up; var rot = z.Rot; var c = z.At(-2.5f, 0); var rng = new System.Random(54); var s = Song(z, "Tis Autumn"); life.autumnSong = s; life.leavesAt = c;
            // the one tree in town with leaves on it, and they are amber
            z.mk.Prism(trunkM, c - up * .3f, up, .4f, 4.2f, 8, .6f); ArtistBits.PostCollider(z.root, c, 4f, .42f); var amber = SongGlow(s, new Color(1f, .62f, .18f), .9f); var amber2 = SongGlow(s, new Color(1f, .78f, .3f), .8f);
            for (var k = 0; k < 6; k++) { var d = (Quaternion.Euler(0, k * 60f + 10f, 0) * Vector3.forward + up * .8f).normalized; z.mk.Prism(trunkM, c + up * 3.2f, d, .12f, 2.8f, 5, .3f); z.mk.Blob(k % 2 == 0 ? amber : amber2, c + up * 3.2f + d * 3f, new Vector3(1.7f, 1.2f, 1.7f), rng, .3f); }
            z.mk.Blob(amber2, c + up * 6.4f, new Vector3(2f, 1.3f, 2f), rng, .3f); halos.Add(c + up * 5.4f, 5f, new Color(1f, .68f, .25f), .3f);
            for (var k = 0; k < 40; k++) { var a = (float)rng.NextDouble() * Mathf.PI * 2f; var r = 1f + (float)rng.NextDouble() * 5.4f; var p = c + new Vector3(Mathf.Cos(a) * r, 0, Mathf.Sin(a) * r); p.y = z.ctx.Ground(p.x, p.z) + .1f; z.mk.Box(k % 3 == 0 ? amber2 : amber, p, new Vector3(.16f, .01f, .1f), Quaternion.Euler(0, k * 37f, 0)); }
            z.area.glowLight = ArtistBits.PointLight(z.root, c + up * 3f + z.front * 2.4f, new Color(1f, .75f, .4f), 22f, 2.6f);
            // a bench under it, a trumpet left on the seat
            var b = c + z.front * 2.2f + up * .08f; Bench(z.mk, b, z.front, 2.4f); Trumpet(z.mk, b + up * .56f - z.side * .8f, Quaternion.LookRotation(z.side), brassM);
            var cm = new MeshKit(); cm.Box(dark, new Vector3(0, .06f, 0), new Vector3(.3f, .12f, .7f), Quaternion.identity); cm.Box(brassM, new Vector3(0, .125f, 0), new Vector3(.06f, .01f, .08f), Quaternion.identity); Thing(cm, "Trumpet case", "an empty trumpet case", b + z.front * .9f + z.side * .9f, rot * Quaternion.Euler(0, 20f, 0), new Vector3(.3f, .14f, .7f), .6f);
            Spot("Chet's bench", b + up * 1.2f - z.side * .8f, "Listen for the trumpet", new Vector3(1f, 1f, 1f)).onUse = _ => Say1(b + up * 2.4f, "Nobody's playing it. You can hear it anyway: soft, close, a little behind the beat.");
            Table(z.ctx, z.mk, z.At(3.5f, -5f)); Chair(z.mk, z.At(3.5f, -5f) + z.side * .85f, -z.side);
        }

        // ---------------------------------------------------------------- Herbie Hancock: Cantaloupe Island
        void HerbieHancock(Plot z)
        {
            var up = Vector3.up; var rot = z.Rot; var c = z.At(-2f, 0); var rng = new System.Random(64); var ci = Song(z, "Cantaloupe Island"); var ss = Song(z, "Sansho Shima");
            // a pond, ankle deep, an island in it, a piano on the island
            z.mk.Prism(new Material(waterM) { name = "Pond" }, c + up * .09f, up, 6.2f, .02f, 26); z.mk.Prism(stoneM, c + up * .08f, up, 6.5f, .01f, 26);
            z.mk.Prism(grey, c - up * .1f, up, 2.7f, .36f, 16); z.solid.Prism(grey, c - up * .3f, up, 2.7f, .56f, 12); var isl = c + up * .26f;
            Grand(z.root, z.mk, isl, rot * Quaternion.Euler(0, 180f, 0), dark, white); var st = isl + z.front * 1.6f; z.mk.Prism(iron, st, up, .04f, .46f, 5); z.mk.Prism(dark, st + up * .46f, up, .2f, .07f, 10); Interact.Seat(tlife.transform, st + up * .55f, -z.front, "Sit at the piano").seatHeight = .55f;
            string[] vamp = { "The vamp. You know the one. Your left hand could do it asleep.", "Four chords round and round, and every time they come up fresh.", "You comp. The pond ripples on two and four.", "A blue note, bent. A melon rolls a quarter turn, approving." }; var vn = 0;
            Spot("Herbie's piano", isl + up * 1.1f, "Play the vamp", new Vector3(1.8f, 1.2f, 2.2f)).onUse = _ => { life.played = Time.time; Say1(isl + up * 2.8f, vamp[vn++ % vamp.Length]); };
            // a little palm of light on the island, cantaloupes floating round it (two to carry, one cut open)
            var palm = isl - z.side * 1.8f - z.front * .6f; z.mk.Prism(trunkM, palm, (up + z.side * -.12f).normalized, .08f, 2.6f, 5, .7f); var frond = SongGlow(ci, new Color(1f, .78f, .4f), 1.1f); for (var k = 0; k < 7; k++) z.mk.Box(frond, palm + up * 2.6f - z.side * .3f + Quaternion.Euler(0, k * 51f, 0) * Vector3.forward * .6f, new Vector3(.14f, .03f, 1.3f), Quaternion.Euler(-24f, k * 51f, 0)); halos.Add(palm + up * 2.6f, 1.8f, Gold, .4f);
            var rind = WorldMaterials.Solid(new Color(.6f, .6f, .56f)); var flesh = SongGlow(ci, new Color(1f, .6f, .25f), 1.6f);
            for (var k = 0; k < 6; k++) { var p = c + Quaternion.Euler(0, k * 60f + 20f, 0) * z.front * (3.8f + (k % 2) * 1f) + up * .22f; if (k < 2) { var mm = new MeshKit(); mm.Blob(rind, new Vector3(0, .17f, 0), Vector3.one * .17f, rng, .04f); Thing(mm, "Cantaloupe", "a cantaloupe", p - up * .1f, Quaternion.identity, new Vector3(.34f, .34f, .34f), .5f); } else if (k == 3) { z.mk.Blob(rind, p, new Vector3(.2f, .12f, .2f), rng, .04f); z.mk.Prism(flesh, p + up * .1f, up, .17f, .02f, 10); halos.Add(p + up * .2f, .8f, new Color(1f, .6f, .3f), .5f); } else z.mk.Blob(rind, p, Vector3.one * (.17f + (k % 3) * .02f), rng, .04f); }
            z.area.glowLight = ArtistBits.PointLight(z.root, isl + up * 3.6f, new Color(.85f, .9f, 1f), 22f, 2.6f);
            // "Sansho Shima": three standing stones, four small lights among them
            var sg = z.At(4.5f, 7.5f); var lg = SongGlow(ss, new Color(.7f, .85f, 1f), 2f); for (var k = 0; k < 3; k++) { var p = sg + z.side * (-1.2f + k * 1.2f) + z.front * ((k % 2) * .5f); z.mk.Box(dark, p + up * (.9f + k * .25f), new Vector3(.6f, 1.8f + k * .5f, .4f), rot * Quaternion.Euler(0, k * 20f - 20f, k * 3f - 3f)); BoxCol(z.root, p + up * .9f, new Vector3(.6f, 1.8f, .4f), rot); }
            for (var k = 0; k < 4; k++) { var p = sg + z.side * (-1.8f + k * 1.2f) + z.front * 1f; z.mk.Blob(lg, p + up * .2f, Vector3.one * .09f, rng, 0f); halos.Add(p + up * .2f, .7f, Blue, .5f); }
            // a synthesiser on a stand, for the later records
            var sy = z.At(4.5f, -7f); z.mk.Box(dark, sy + up * .9f, new Vector3(1.2f, .14f, .4f), rot); z.mk.Box(white, sy + up * .98f + z.front * .08f, new Vector3(1.1f, .02f, .16f), rot); foreach (var e in new[] { -.5f, .5f }) z.mk.Box(iron, sy + up * .45f + z.side * e, new Vector3(.04f, .9f, .3f), rot); for (var k = 0; k < 6; k++) z.mk.Blob(GlowM(k % 2 == 0 ? Blue : Brass, 1.2f), sy + up * 1f - z.front * .12f + z.side * (-.4f + k * .16f), Vector3.one * .025f, rng, 0f); BoxCol(z.root, sy + up * .5f, new Vector3(1.2f, 1f, .4f), rot);
        }

        // ---------------------------------------------------------------- Django Reinhardt: Minor Swing
        void Django(Plot z)
        {
            var up = Vector3.up; var rot = z.Rot; var rng = new System.Random(37); var s = Song(z, "Minor Swing");
            // a caravan with a lit window, a fire, a guitar leaning on the step
            var cv = z.At(-3f, -1f) + up * .7f; var wood = WorldMaterials.Solid(new Color(.3f, .3f, .33f)); z.mk.Box(wood, cv + up * .9f, new Vector3(3.4f, 1.8f, 2f), rot); z.solid.Box(wood, cv + up * .6f, new Vector3(3.4f, 2.6f, 2f), rot); z.mk.Prism(dark, cv + up * 1.8f - z.side * 1.8f, z.side, 1.02f, 3.6f, 12);
            foreach (var e in new[] { -1.1f, 1.1f }) foreach (var q in new[] { -1f, 1f }) z.mk.Prism(dark, cv - up * .2f + z.side * e + z.front * q * 1.06f, z.front * q, .5f, .1f, 12);
            z.mk.Box(windowM, cv + up * 1.1f + z.front * 1.01f + z.side * .8f, new Vector3(.8f, .7f, .04f), rot); z.mk.Box(dark, cv + up * .8f + z.front * 1.01f - z.side * .7f, new Vector3(.8f, 1.6f, .04f), rot); for (var k = 0; k < 3; k++) z.mk.Box(wood, cv - up * (.1f + k * .22f) + z.front * (1.2f + k * .26f) - z.side * .7f, new Vector3(.9f, .06f, .26f), rot); halos.Add(cv + up * 1.1f + z.front * 1.6f + z.side * .8f, 1.3f, Brass, .4f);
            var gt = cv - up * .66f + z.front * 1.6f + z.side * .4f; z.mk.Blob(WorldMaterials.Solid(new Color(.5f, .38f, .22f), .6f), gt + up * .34f, new Vector3(.2f, .26f, .07f), rng, .04f); z.mk.Box(dark, gt + up * .95f - z.front * .12f, new Vector3(.06f, .8f, .04f), rot * Quaternion.Euler(-10f, 0, 0));
            var fr = z.At(2.2f, -2.6f); for (var k = 0; k < 6; k++) z.mk.Blob(grey, fr + Quaternion.Euler(0, k * 60f, 0) * Vector3.forward * .5f + up * .08f, Vector3.one * .14f, rng, .2f); var flame = SongGlow(s, new Color(1f, .6f, .2f), 2.2f); z.mk.Cone(flame, fr + up * .05f, .26f, .7f, 5); z.mk.Cone(flame, fr + up * .05f + z.side * .12f, .16f, .45f, 4, 30f); halos.Add(fr + up * .5f, 2f, new Color(1f, .6f, .25f), .6f); z.area.glowLight = ArtistBits.PointLight(z.root, fr + up * 1.2f, new Color(1f, .65f, .3f), 20f, 2.8f);
            foreach (var d in new[] { -1f, 1f }) { var lg = fr + z.side * d * 1.5f + z.front * .3f; z.mk.Prism(trunkM, lg + up * .2f - z.front * .6f, z.front, .2f, 1.2f, 7); BoxCol(z.root, lg + up * .25f, new Vector3(.4f, .5f, 1.2f), rot); Interact.Seat(tlife.transform, lg + up * .42f, -z.side * d, "Sit by the fire").seatHeight = .42f; }
            // a minor swing: a small one, to sit on
            var sw = z.At(2.5f, 4f); foreach (var e in new[] { -1f, 1f }) { z.mk.Prism(iron, sw + z.side * e * 1f - z.front * .5f - up * .1f, (up + z.front * .2f).normalized, .05f, 2.5f, 5); z.mk.Prism(iron, sw + z.side * e * 1f + z.front * .5f - up * .1f, (up - z.front * .2f).normalized, .05f, 2.5f, 5); ArtistBits.PostCollider(z.root, sw + z.side * e * 1f, 2.2f, .1f); foreach (var q in new[] { -.25f, .25f }) if (e < 0) z.mk.Prism(iron, sw + up * 2.3f + z.side * q, -up, .012f, 1.8f, 3); }
            z.mk.Prism(iron, sw + up * 2.34f - z.side * 1f, z.side, .05f, 2f, 5); z.mk.Box(SongGlow(s, Brass, .8f), sw + up * .5f, new Vector3(.7f, .05f, .26f), rot); Interact.Seat(tlife.transform, sw + up * .56f, z.front, "Sit on the small swing").seatHeight = .5f;
        }

        // ---------------------------------------------------------------- everyone else
        void Others(Plot z)
        {
            var up = Vector3.up; var rot = z.Rot; var p = z.At(2.4f, -2.8f); var rng = new System.Random(z.a.name.Length * 3); string s; var reachP = Foot(z.a);
            switch (z.a.name)
            {
                case "Cab Calloway":
                    s = Song(z, "Minnie the Moocher"); foreach (var e in new[] { -1.3f, 1.3f }) { z.mk.Prism(iron, p + z.side * e - up * .2f, up, .05f, 3f, 5); ArtistBits.PostCollider(z.root, p + z.side * e, 2.8f, .07f); } z.mk.Box(dark, p + up * 2.3f, new Vector3(3f, 1f, .12f), rot); ArtistBits.Label(z.labels, "HI-DE-HO", p + up * 2.3f + z.front * .08f, z.front, .44f, Brass, TextAnchor.MiddleCenter);
                    var cb = SongGlow(s, new Color(1f, .9f, .62f), 2f); for (var k = 0; k < 12; k++) { z.mk.Blob(cb, p + up * 2.86f + z.side * (-1.4f + k * .255f) + z.front * .08f, Vector3.one * .06f, rng, 0f); z.mk.Blob(cb, p + up * 1.74f + z.side * (-1.4f + k * .255f) + z.front * .08f, Vector3.one * .06f, rng, 0f); } halos.Add(p + up * 2.3f + z.front * .6f, 2f, Brass, .4f);
                    Mic(z.root, z.mk, p + z.front * 1.2f); string[] call = { "\"HI-DE-HI-DE-HI-DE-HI!\" And the whole street answers: \"HI-DE-HI-DE-HI-DE-HI!\"", "\"HO-DE-HO-DE-HO-DE-HO!\" A window opens three floors up to sing it back.", "You try a long one, with a wobble on the end. They get every note of it. They always do." }; var cn = 0;
                    Spot("Cab's microphone", p + z.front * 1.2f + up * 1.3f, "Call out", new Vector3(1f, 1.8f, 1f)).onUse = _ => { life.played = Time.time; Say1(p + up * 3.6f, call[cn++ % call.Length]); }; break;
                case "Cannonball Adderley":
                    s = Song(z, "Mercy"); z.mk.Prism(iron, p - up * .2f, up, .06f, 3.8f, 6); ArtistBits.PostCollider(z.root, p, 3.6f, .08f); for (var k = 0; k < 3; k++) { z.mk.Box(dark, p + up * (3.3f - k * .7f) + z.side * .8f, new Vector3(1.5f, .56f, .1f), rot); ArtistBits.Label(z.labels, "MERCY", p + up * (3.3f - k * .7f) + z.side * .8f + z.front * .07f, z.front, .3f, Brass, TextAnchor.MiddleCenter); z.mk.Box(SongGlow(s, Brass, 1.6f), p + up * (3.04f - k * .7f) + z.side * .8f + z.front * .07f, new Vector3(1.3f, .03f, .02f), rot); }
                    halos.Add(p + up * 2.6f + z.side * .8f + z.front * .5f, 1.8f, Brass, .4f); Sax(z.mk, p + z.front * 1.3f - z.side * .3f, rot, brassM, .85f); z.mk.Prism(iron, p + z.front * 1.3f - z.side * .3f, up, .12f, .03f, 6); break;
                case "Donald Byrd":
                    s = Song(z, "Dominoes"); z.mk.Box(grey, p + up * .74f, new Vector3(1.5f, .06f, .9f), rot); foreach (var (x, zz) in new[] { (-.65f, -.35f), (.65f, -.35f), (.65f, .35f), (-.65f, .35f) }) z.mk.Prism(iron, p + z.side * x + z.front * zz, up, .03f, .74f, 4); BoxCol(z.root, p + up * .4f, new Vector3(1.5f, .8f, .9f), rot);
                    var pip = SongGlow(s, Brass, 1.8f); for (var k = 0; k < 9; k++) { var dp = p + up * .79f + z.side * (-.56f + k * .14f) + z.front * (Mathf.Sin(k * .9f) * .16f); z.mk.Box(white, dp, new Vector3(.1f, .03f, .2f), rot * Quaternion.Euler(0, k % 3 == 0 ? 90f : 0f, 0)); z.mk.Blob(pip, dp + up * .02f, new Vector3(.02f, .006f, .02f), rng, 0f); }
                    Trumpet(z.mk, p + up * .84f + z.side * .5f - z.front * .28f, Quaternion.LookRotation(z.side), brassM); Chair(z.mk, p + z.front * .9f, -z.front); Chair(z.mk, p - z.front * .9f, z.front);
                    string[] dom = { "Click. Double six. Somebody behind you whistles, low.", "You lay one down that fits both ends. The trumpet on the table hums.", "Click, click. The line turns a corner and keeps going, like a good bass part." }; var dn2 = 0;
                    Spot("Dominoes", p + up * 1f, "Play a domino", new Vector3(1.5f, .6f, .9f)).onUse = _ => { life.played = Time.time; Say1(p + up * 2.2f, dom[dn2++ % dom.Length]); }; break;
                case "Ella Fitzgerald":
                    s = Song(z, "A-Tisket"); var bm = new MeshKit(); var yellow = SongGlow(s, new Color(1f, .86f, .2f), 1.3f); bm.Prism(yellow, Vector3.zero, Vector3.up, .16f, .16f, 10, 1.25f); for (var k = 0; k < 7; k++) { var a = k / 6f * Mathf.PI; bm.Blob(yellow, new Vector3(Mathf.Cos(a) * .19f, .16f + Mathf.Sin(a) * .22f, 0), Vector3.one * .025f, rng, 0f); }
                    Thing(bm, "Basket", "a little yellow basket", p + up * .12f, rot, new Vector3(.4f, .4f, .4f), .3f); halos.Add(p + up * .3f, 1f, new Color(1f, .86f, .3f), .5f);
                    var lt = z.At(4.6f, 1.5f); var lm = new MeshKit(); lm.Box(white, new Vector3(0, .01f, 0), new Vector3(.24f, .02f, .16f), Quaternion.identity); lm.Box(SongGlow(s, Red, .8f), new Vector3(0, .022f, 0), new Vector3(.04f, .004f, .04f), Quaternion.identity); Thing(lm, "Letter", "a letter somebody dropped", lt + up * .12f, rot * Quaternion.Euler(0, 30f, 0), new Vector3(.24f, .05f, .16f), .1f);
                    Mic(z.root, z.mk, p - z.side * 1.2f + z.front * .4f); break;
                case "Frank Sinatra":
                    s = Song(z, "I've Got You Under My Skin"); z.mk.Prism(iron, p - up * .2f, up, .08f, 4.2f, 6); z.mk.Prism(iron, p, up, .2f, .4f, 8, .5f); z.mk.Blob(lampM, p + up * 4.1f, new Vector3(.2f, .26f, .2f), rng, 0f); halos.Add(p + up * 4.1f, 2f, Brass, .6f); ArtistBits.PostCollider(z.root, p, 4f, .1f); z.mk.Prism(poolM, p + up * .1f, up, 1.6f, .012f, 14);
                    z.mk.Prism(grey, p + up * 2.2f + z.front * .12f, up, .22f, .03f, 12); z.mk.Prism(grey, p + up * 2.22f + z.front * .12f, up, .13f, .14f, 10, .9f); z.mk.Box(WorldMaterials.Solid(Coats[0]), p + up * 1.5f + z.side * .14f, new Vector3(.12f, 1.3f, .4f), rot * Quaternion.Euler(0, 0, 4f));
                    var hb = z.At(4.2f, -.6f); z.mk.Box(dark, hb + up * 1.1f, new Vector3(.9f, 2.2f, .06f), rot); z.mk.Blob(dark, hb + up * 2.3f, new Vector3(.2f, .24f, .05f), rng, 0f); BoxCol(z.root, hb + up * 1.1f, new Vector3(.9f, 2.2f, .1f), rot); var heart = SongGlow(s, new Color(1f, .25f, .3f), 2.2f); z.mk.Blob(heart, hb + up * 1.6f + z.side * .1f + z.front * .04f, new Vector3(.09f, .09f, .02f), rng, 0f); halos.Add(hb + up * 1.6f + z.side * .1f + z.front * .3f, .7f, Red, .6f);
                    Mic(z.root, z.mk, p + z.front * 1.4f + z.side * .8f); break;
                case "Glenn Miller":
                    s = Song(z, "In the Mood"); life.moodSong = s; z.mk.Box(grey, p + up * .15f - z.front * .4f, new Vector3(4.4f, .3f, 2f), rot); z.solid.Box(grey, p + up * .05f - z.front * .4f, new Vector3(4.4f, .5f, 2f), rot);
                    for (var k = 0; k < 3; k++) { var sp2 = p + up * .3f + z.side * (-1.4f + k * 1.4f); z.mk.Box(white, sp2 + up * .5f + z.front * .3f, new Vector3(.9f, 1f, .06f), rot); z.mk.Box(SongGlow(s, Brass, 1.4f), sp2 + up * .7f + z.front * .34f, new Vector3(.5f, .06f, .02f), rot * Quaternion.Euler(0, 0, 20f)); z.mk.Box(SongGlow(s, Brass, 1.4f), sp2 + up * .5f + z.front * .34f, new Vector3(.5f, .06f, .02f), rot * Quaternion.Euler(0, 0, -20f)); var tb = sp2 + up * 1.3f - z.front * .5f; var td = (z.front + up * .25f).normalized; z.mk.Prism(brassM, tb, td, .02f, 1.3f, 5); z.mk.Prism(brassM, tb + up * .08f, td, .02f, .9f, 5); z.mk.Prism(brassM, tb + td * 1.3f, td, .03f, .22f, 8, 4f); z.mk.Prism(iron, sp2 - z.front * .5f, up, .02f, 1.3f, 4); }
                    for (var k = 0; k < 9; k++) z.mk.Blob(bulbM[k % 3], p + up * .34f + z.side * (-2.1f + k * .52f) + z.front * .62f, Vector3.one * .07f, rng, 0f); halos.Add(p + up * 1f + z.front * .8f, 2.4f, Brass, .35f); BoxCol(z.root, p + up * .9f + z.front * .3f, new Vector3(3.8f, 1.2f, .1f), rot); break;
                case "Jimmy Smith":
                    s = Song(z, "Root Down"); var wood = WorldMaterials.Solid(new Color(.34f, .25f, .18f)); z.mk.Box(wood, p + up * .6f, new Vector3(1.5f, 1f, .8f), rot); z.mk.Box(white, p + up * 1.02f + z.front * .34f, new Vector3(1.3f, .04f, .2f), rot); z.mk.Box(white, p + up * 1.14f + z.front * .16f, new Vector3(1.3f, .04f, .2f), rot); z.mk.Box(wood, p + up * 1.3f - z.front * .2f, new Vector3(1.5f, .3f, .3f), rot); BoxCol(z.root, p + up * .7f, new Vector3(1.5f, 1.4f, .8f), rot);
                    var root = SongGlow(s, Brass, 1.8f); for (var k = 0; k < 8; k++) { var d = Quaternion.Euler(0, k * 45f + 10f, 0) * z.front; var prev = p + d * .5f + up * .14f; for (var q = 1; q <= 4; q++) { var np = p + d * (.5f + q * .55f) + Quaternion.Euler(0, 90f, 0) * d * Mathf.Sin(q * 1.4f + k) * .22f; np.y = z.ctx.Ground(np.x, np.z) + .1f; z.mk.Prism(root, prev, (np - prev).normalized, .045f - q * .008f, Vector3.Distance(prev, np), 4); prev = np; } } halos.Add(p + up * .3f, 2.6f, Brass, .35f);
                    var jst = p + z.front * 1.1f; z.mk.Box(wood, jst + up * .25f, new Vector3(1f, .5f, .4f), rot); Interact.Seat(tlife.transform, jst + up * .52f, -z.front, "Sit at the organ").seatHeight = .52f; var ls = p + z.side * 1.6f - z.front * .2f; z.mk.Box(wood, ls + up * .6f, new Vector3(.8f, 1.2f, .6f), rot); for (var k = 0; k < 4; k++) z.mk.Box(dark, ls + up * (.9f + k * .07f) + z.front * .31f, new Vector3(.6f, .03f, .02f), rot); BoxCol(z.root, ls + up * .6f, new Vector3(.8f, 1.2f, .6f), rot);
                    string[] org = { "A fat chord, drawbars all the way out. It goes down through the floor and takes root.", "Your left hand walks. Your foot walks. The speaker spins up and the whole lane grins.", "You lean on one note until it growls." }; var on = 0;
                    Spot("The organ", p + up * 1.2f + z.front * .4f, "Play the organ", new Vector3(1.5f, 1f, 1f)).onUse = _ => { life.played = Time.time; Say1(p + up * 2.8f, org[on++ % org.Length]); }; break;
                case "Joe Henderson":
                    s = Song(z, "Earth"); z.mk.Prism(iron, p, up, .3f, .1f, 10); z.mk.Prism(iron, p, up, .04f, 1f, 5); z.mk.Blob(WorldMaterials.Solid(new Color(.24f, .26f, .3f), .7f), p + up * 1.7f, Vector3.one * .75f, rng, 0f); var landG = SongGlow(s, new Color(.75f, .85f, .7f), .9f); for (var k = 0; k < 7; k++) z.mk.Blob(landG, p + up * 1.7f + Quaternion.Euler(k * 47f - 80f, k * 97f, 0) * Vector3.forward * .68f, new Vector3(.3f, .2f, .14f) * (1f + (k % 3) * .2f), rng, .3f);
                    for (var k = 0; k < 20; k++) { var a = k * 18f * Mathf.Deg2Rad; z.mk.Blob(brassM, p + up * 1.7f + (z.side * Mathf.Cos(a) + (z.front + up * .4f).normalized * Mathf.Sin(a)) * 1.05f, Vector3.one * .03f, rng, 0f); } halos.Add(p + up * 1.7f, 1.8f, new Color(.7f, .85f, 1f), .35f); ArtistBits.PostCollider(z.root, p, 2.4f, .5f);
                    Sax(z.mk, p + z.front * 1.4f + z.side * .9f, rot, brassM); break;
                case "Les McCann":
                    s = Song(z, "Compared to What"); z.mk.Prism(iron, p, up, .3f, .1f, 10); z.mk.Prism(iron, p, up, .05f, 2.4f, 6); z.mk.Box(brassM, p + up * 2.36f, new Vector3(2.6f, .06f, .06f), rot * Quaternion.Euler(0, 0, 9f)); ArtistBits.PostCollider(z.root, p, 2.4f, .1f);
                    foreach (var e in new[] { -1f, 1f }) { var end = p + up * (2.36f + e * .2f) + z.side * e * 1.28f; var pan = end - up * (1.1f); z.mk.Prism(iron, pan + up * .02f, up, .012f, 1.08f, 3); z.mk.Prism(brassM, pan - up * .02f, up, .4f, .04f, 12, 1.2f); if (e < 0) z.mk.Blob(WorldMaterials.Solid(Silver, .8f), pan + up * .12f, Vector3.one * .1f, rng, .1f); else { ArtistBits.Label(z.labels, "?", pan + up * .34f, z.front, .5f, Brass, TextAnchor.MiddleCenter); z.mk.Blob(SongGlow(s, Brass, 2f), pan + up * .06f, new Vector3(.2f, .02f, .2f), rng, 0f); halos.Add(pan + up * .3f, .9f, Brass, .5f); } }
                    var ep = z.At(4.4f, 1.6f); z.mk.Box(dark, ep + up * .85f, new Vector3(1.3f, .16f, .5f), rot); z.mk.Box(white, ep + up * .94f + z.front * .1f, new Vector3(1.2f, .02f, .2f), rot); foreach (var e in new[] { -.55f, .55f }) z.mk.Prism(iron, ep + z.side * e, up, .025f, .8f, 4); BoxCol(z.root, ep + up * .5f, new Vector3(1.3f, 1f, .5f), rot); break;
                case "Moondog":
                    s = Song(z, "Bird's Lament"); var cloak = WorldMaterials.Solid(new Color(.3f, .3f, .32f)); z.mk.Cone(cloak, p, .55f, 1.75f, 9); z.mk.Blob(WorldMaterials.Solid(new Color(.62f, .62f, .63f)), p + up * 1.84f, Vector3.one * .17f, rng, 0f); z.mk.Cone(grey, p + up * 1.9f, .2f, .22f, 8); foreach (var e in new[] { -1f, 1f }) z.mk.Cone(white, p + up * 2f + z.side * e * .16f, .05f, .34f, 5, 0f); z.mk.Blob(cloak, p + up * 1.56f + z.front * .1f, new Vector3(.12f, .3f, .08f), rng, .2f);
                    z.mk.Prism(trunkM, p + z.side * .55f, up, .025f, 2.5f, 4); z.mk.Cone(grey, p + z.side * .55f + up * 2.5f, .06f, .3f, 4); ArtistBits.PostCollider(z.root, p, 2f, .5f); z.mk.Prism(poolM, p + up * .06f, up, 1.8f, .012f, 12);
                    var wa = p - z.side * 3.2f + up * 3.4f; var wb = p + z.side * 3.2f + up * 3.4f; foreach (var e in new[] { -3.2f, 3.2f }) { z.mk.Prism(iron, p + z.side * e - up * .2f, up, .06f, 3.7f, 5); ArtistBits.PostCollider(z.root, p + z.side * e, 3.4f, .08f); } z.mk.Prism(dark, wa, (wb - wa).normalized, .012f, (wb - wa).magnitude, 3);
                    var bird = SongGlow(s, new Color(1f, 1f, .95f), 1.2f); for (var k = 0; k < 7; k++) { var bp = Vector3.Lerp(wa, wb, .1f + k * .13f) + up * .09f; z.mk.Blob(bird, bp, new Vector3(.07f, .08f, .13f), rng, 0f); z.mk.Blob(bird, bp + up * .1f + z.front * .08f, Vector3.one * .045f, rng, 0f); } var gb = p + z.front * 1.5f - z.side * .5f; z.mk.Blob(bird, gb + up * .09f, new Vector3(.07f, .08f, .13f), rng, 0f); z.mk.Blob(bird, gb + up * .19f + z.front * .08f, Vector3.one * .045f, rng, 0f);
                    Spot("Moondog's corner", p + up * 1.2f + z.front * .6f, "Stand with him", new Vector3(1.4f, 2f, 1.4f)).onUse = _ => Say1(p + up * 3f, "He says nothing. He stands at his corner with his spear while the town goes round him, and the birds stay."); break;
                case "Nat King Cole":
                    s = Song(z, "Unforgettable"); Grand(z.root, z.mk, p, rot * Quaternion.Euler(0, 180f, 0), dark, white); var fr2 = p + up * 1.03f - z.side * .3f - z.front * .2f; z.mk.Box(WorldMaterials.Solid(Silver, .8f), fr2 + up * .18f, new Vector3(.3f, .36f, .03f), rot * Quaternion.Euler(-10f, 0, 0)); z.mk.Box(SongGlow(s, new Color(1f, .96f, .86f), 1f), fr2 + up * .18f + z.front * .02f, new Vector3(.24f, .3f, .02f), rot * Quaternion.Euler(-10f, 0, 0)); halos.Add(fr2 + up * .2f + z.front * .3f, .7f, Paper, .5f);
                    var vs = p + up * 1.03f + z.side * .35f - z.front * .1f; z.mk.Prism(WorldMaterials.Solid(Silver, .9f), vs, up, .04f, .2f, 6); z.mk.Prism(leafDark, vs + up * .2f, up, .008f, .22f, 3); z.mk.Blob(SongGlow(s, new Color(1f, .2f, .25f), 1.6f), vs + up * .44f, Vector3.one * .05f, rng, .2f);
                    var nst = p + z.front * 1.6f; z.mk.Box(dark, nst + up * .25f, new Vector3(.9f, .5f, .4f), rot); Interact.Seat(tlife.transform, nst + up * .52f, -z.front, "Sit at the piano").seatHeight = .52f; break;
                case "Pharoah Sanders":
                    s = Song(z, "Astral Traveling"); life.astralSong = s; Sax(z.mk, p, rot, brassM, 1.1f); z.mk.Prism(iron, p, up, .14f, .03f, 6); var sm = SongGlow(s, new Color(1f, .9f, .6f), 2.2f); var sm2 = SongGlow(s, new Color(.7f, .75f, 1f), 2f);
                    for (var r = 0; r < 3; r++) { var tilt = Quaternion.AngleAxis(20f + r * 28f, Quaternion.Euler(0, r * 60f, 0) * Vector3.forward); for (var k = 0; k < 10 + r * 3; k++) { var a = k * Mathf.PI * 2f / (10 + r * 3); z.mk.Blob((k + r) % 3 == 0 ? sm2 : sm, p + up * 2.6f + tilt * new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * (1.3f + r * .7f), Vector3.one * (k % 4 == 0 ? .09f : .045f), rng, 0f); } }
                    z.mk.Blob(sm, p + up * 2.6f, Vector3.one * .16f, rng, 0f); halos.Add(p + up * 2.6f, 3.2f, new Color(1f, .9f, .7f), .4f); break;
                case "Sonny Rollins":
                    s = Song(z, "St. Thomas"); Sax(z.mk, p, rot, brassM, 1.1f); z.mk.Prism(iron, p, up, .14f, .03f, 6); var ms = p + z.side * .9f; z.mk.Prism(iron, ms, up, .02f, 1.2f, 4); z.mk.Box(dark, ms + up * 1.3f, new Vector3(.5f, .36f, .03f), rot * Quaternion.Euler(-14f, 0, 0)); z.mk.Box(white, ms + up * 1.3f + z.front * .02f, new Vector3(.4f, .28f, .02f), rot * Quaternion.Euler(-14f, 0, 0));
                    var pl = z.At(1f, 4.2f); z.mk.Prism(grey, pl - up * .1f, up, 1.6f, .24f, 12); z.mk.Prism(trunkM, pl, (up + z.side * .14f).normalized, .1f, 3.4f, 6, .6f); var fd = SongGlow(s, new Color(1f, .8f, .4f), 1.1f); for (var k = 0; k < 8; k++) z.mk.Box(fd, pl + up * 3.4f + z.side * .5f + Quaternion.Euler(0, k * 45f, 0) * Vector3.forward * .8f, new Vector3(.16f, .03f, 1.7f), Quaternion.Euler(-26f, k * 45f, 0)); halos.Add(pl + up * 3.4f, 2.2f, Gold, .4f); ArtistBits.PostCollider(z.root, pl, 3.2f, .12f); break;
                case "Sonny Rollins Quartet":
                    s = Song(z, "St. Thomas"); Sax(z.mk, p - z.side * .9f, rot, brassM, 1f); Bass(z.mk, p + z.side * .3f - z.front * .5f, rot, rng); var sn = p + z.side * 1.3f + z.front * .3f; z.mk.Prism(iron, sn, up, .02f, .6f, 4); z.mk.Prism(WorldMaterials.Solid(Silver, .8f), sn + up * .6f, up, .22f, .14f, 12); z.mk.Prism(brassM, sn + up * 1.1f + z.side * .5f, up, .26f, .02f, 12); z.mk.Prism(iron, sn + z.side * .5f, up, .012f, 1.1f, 3);
                    var up2 = p - z.side * 2f - z.front * .6f; z.mk.Box(dark, up2 + up * .65f, new Vector3(1.3f, 1.3f, .5f), rot); z.mk.Box(white, up2 + up * .78f + z.front * .3f, new Vector3(1.2f, .04f, .18f), rot); BoxCol(z.root, up2 + up * .65f, new Vector3(1.3f, 1.3f, .6f), rot);
                    var sun = SongGlow(s, new Color(1f, .78f, .35f), 1.8f); z.mk.Prism(iron, p - z.front * 1.4f - up * .2f, up, .05f, 3.4f, 5); z.mk.Blob(sun, p - z.front * 1.4f + up * 3.5f, Vector3.one * .4f, rng, 0f); halos.Add(p - z.front * 1.4f + up * 3.5f, 2.4f, Gold, .5f); ArtistBits.PostCollider(z.root, p - z.front * 1.4f, 3.2f, .07f); break;
                case "Stan Getz":
                    s = Song(z, "The Girl From Ipanema"); var wv = SongGlow(s, new Color(.95f, .95f, .92f), .5f); for (var r = 0; r < 6; r++) for (var k = 0; k < 14; k++) { var wp = p + z.side * (-1.9f + k * .3f) + z.front * (-1.2f + r * .5f + Mathf.Sin(k * .9f) * .14f) + up * .1f; z.mk.Box(r % 2 == 0 ? wv : dark, wp, new Vector3(.3f, .02f, .26f), rot * Quaternion.Euler(0, Mathf.Cos(k * .9f) * 22f, 0)); }
                    var um = p + z.side * 1.2f + z.front * .2f; z.mk.Prism(iron, um, up, .03f, 2.2f, 5); z.mk.Cone(white, um + up * 1.9f, 1.3f, .5f, 8); z.mk.Cone(dark, um + up * 2.12f, .76f, .3f, 8); z.mk.Prism(dark, um + up * 1.86f, up, 1.32f, .04f, 8); ArtistBits.PostCollider(z.root, um, 2f, .06f);
                    var dc = p - z.side * .3f + z.front * .1f; z.mk.Box(white, dc + up * .36f, new Vector3(.6f, .05f, 1.1f), rot * Quaternion.Euler(-12f, 0, 0)); z.mk.Box(grey, dc + up * .18f, new Vector3(.64f, .36f, .9f), rot); Interact.Seat(tlife.transform, dc + up * .46f, z.front, "Sit in the deck chair").seatHeight = .46f; Sax(z.mk, p - z.side * 1.4f + z.front * .6f, rot, brassM); halos.Add(p + up * .4f, 2.4f, Paper, .2f); break;
                case "The Dave Brubeck Quartet":
                    s = Song(z, "Blue Rondo"); life.rondoSong = s; var stones = new Material[9]; for (var k = 0; k < 9; k++) { stones[k] = new Material(GlowM(Blue, .4f)) { name = "Rondo stone " + k }; var a = k * 40f; var sp3 = p + Quaternion.Euler(0, a, 0) * z.front * 2f + up * .1f; z.mk.Prism(k == 0 || k == 2 || k == 4 || k == 6 ? stoneM : grey, sp3 - up * .04f, up, .46f, .06f, 9); z.mk.Prism(stones[k], sp3 + up * .025f, up, k == 0 || k == 2 || k == 4 || k == 6 ? .34f : .22f, .012f, 9); }
                    life.rondo = stones; z.lit.Add(s); halos.Add(p + up * .4f, 2.8f, Blue, .3f); z.mk.Prism(iron, p, up, .03f, .9f, 4); z.mk.Box(dark, p + up * 1.05f, new Vector3(.16f, .3f, .12f), rot); z.mk.Box(brassM, p + up * 1.1f + z.front * .07f, new Vector3(.015f, .24f, .01f), rot * Quaternion.Euler(0, 0, 16f)); ArtistBits.PostCollider(z.root, p, 1f, .08f);
                    ArtistBits.Label(z.labels, "2 · 2 · 2 · 3", p + up * 1.5f, z.front, .16f, Blue, TextAnchor.MiddleCenter); break;
                case "Zoh Amba":
                    s = Song(z, "Another Time"); z.mk.Prism(white, p + up * .5f - z.front * .3f, (z.front + up * .9f).normalized, 1.1f, .1f, 20); z.mk.Prism(iron, p + up * .46f - z.front * .34f, (z.front + up * .9f).normalized, 1.2f, .08f, 20); var tick = SongGlow(s, Brass, 1.8f); var cn2 = p + up * .5f - z.front * .3f + (z.front + up * .9f).normalized * .11f; var cu2 = Vector3.Cross(z.side, (z.front + up * .9f).normalized).normalized;
                    for (var k = 0; k < 12; k++) { var a = k * 30f * Mathf.Deg2Rad; z.mk.Blob(tick, cn2 + (z.side * Mathf.Cos(a) + cu2 * Mathf.Sin(a)) * .9f, Vector3.one * (k % 3 == 0 ? .06f : .035f), rng, 0f); } z.mk.Blob(tick, cn2, Vector3.one * .05f, rng, 0f); halos.Add(cn2 + z.front * .3f, 1.6f, Brass, .4f); BoxCol(z.root, p + up * .5f, new Vector3(2.2f, 1.2f, 1.2f), rot);
                    Sax(z.mk, p + z.side * 1.7f + z.front * .6f, rot, brassM, 1.05f); for (var k = 0; k < 9; k++) { var fp = p + Quaternion.Euler(0, k * 40f, 0) * z.front * (1.5f + (k % 3) * .3f); fp.y = z.ctx.Ground(fp.x, fp.z) + .1f; z.mk.Prism(leafDark, fp, up, .01f, .26f, 3); z.mk.Blob(white, fp + up * .28f, Vector3.one * .05f, rng, .2f); } break;
                default:
                    z.mk.Prism(iron, p, up, .04f, 1.3f, 5); z.mk.Box(GlowM(Brass, 1.4f), p + up * 1.42f, new Vector3(.2f, .28f, .2f), rot); break;
            }
        }
    }
}
