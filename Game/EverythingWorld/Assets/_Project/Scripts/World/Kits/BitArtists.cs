using System.Collections.Generic;
using UnityEngine;
using static EverythingWorld.World.PlaceTools;

namespace EverythingWorld.World
{
    /// The Game Isles's artists. Every game is its own little world on a raised square of blocks, with the artist's name in
    /// gold on a block sign and a gold block for every song; covers on a board framed like a game cartridge.
    /// - ConcernedApe: a farm in all four seasons at once behind a wooden fence: a farmhouse with a red roof, a chicken coop,
    ///   a scarecrow and a shipping bin, and a quarter for each season: blossom and green shoots ("Spring (It's a Big World
    ///   Outside)"), sunflowers and melons ("Summer (The Sun Can Bend an Orange Sky)"), orange trees and pumpkins ("Fall
    ///   (Ghost Synth)"), and snow with a snowman ("Winter (Nocturne of Ice)").
    /// - meganeko: a neon stage of pixel speakers and a wall of blinking pixels, a giant soda can ("Astro Soda"), a blocky
    ///   shark ("Sharkatzor") and a film camera on a crane ("Lights Camera Action").
    /// - The blocky hillside (C418, Lena Raine, Captainsparklez, Brad Knauber): block trees and a block house, steps up into
    ///   the sunrise, a ruined block castle with a crown, and a mine with torches.
    /// - The Ring's corner (Martin O'Donnell, Halo, Jack Wall): an angular silver monolith with blue light, a purple alien
    ///   spire, and a pair of snakeskin boots on a crate.
    /// - The rest: a cave of crystals with a barrel, a sword in a stone, the calm white shop counter, paper flowers, a black
    ///   single wing and a long sword, a gothic lamp among white flowers, a grassy hill with a bomb and a star, a hall of
    ///   golden pillars with a little red heart, and a pixel atom.
    public partial class BitKit
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
                Add("Toby Fox", "MEGALOVANIA", "Toby Fox"); Add("Nobuo Uematsu", "Advent", "Nobuo Uematsu"); Add("Blue Brew Music", "Wii Shop", "Blue Brew Music");
                Add("ConcernedApe", "Winter", "ConcernedApe"); Add("ConcernedApe", "Spring", "ConcernedApe"); Add("ConcernedApe", "Summer", "ConcernedApe"); Add("ConcernedApe", "Fall", "ConcernedApe");
                Add("Martin O'Donnell", "Halo Theme", "@the ring"); Add("SIE Sound Team", "Hunter's Dream", "SIE Sound Team"); Add("Super Mario 64", "Bob-Omb", "Super Mario 64"); Add("C418", "Living Mice", "C418");
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
            switch (a.name)
            {
                case "ConcernedApe": Farm(z); break;
                case "meganeko": Neon(z); break;
                default: Others(z, Base(z)); break;
            }
            SongLamps(z);
            Board(z, z.At(reach * .6f, -reach * .45f - 1.5f), front);
            ArtistBits.Plaque(z.mk, z.solid, labels, a, z.At(reach * .6f + 1.5f, reach * .45f + 1.5f), front, iron, Paint(new Color(.2f, .24f, .5f)), Gold, 1.6f);
            Lamp(ctx, z.mk, z.At(reach * .5f, reach * .7f), false);
            if (z.area.glowLight == null) z.area.glowLight = ArtistBits.PointLight(root, z.c + up * 4f + front * 3f, new Color(1f, .9f, .7f), HasStation(a) ? 24f : 14f, 1.3f);
            area.glowRenderers = new Renderer[0];
            z.mk.Build("Area", root, false);
            z.solid.BuildCollider("Walls", root);
            z.pictures.Build("Covers", root, false);
            noGrass.Add(new Vector3(z.c.x, reach * .9f, z.c.z));
            var mark = new GameObject("Shot: " + a.name + " up close").transform; mark.SetParent(ctx.Root, false);
            var cam = z.At(reach * .85f, reach * .3f) + up * 2f; var look = z.At(-reach * .3f, 0) + up * 1.8f - cam; mark.SetPositionAndRotation(cam, Quaternion.LookRotation(look.normalized));
        }

        /// The covers on a board shaped like a grey game cartridge, a coloured label in the middle.
        void Board(Plot z, Vector3 ground, Vector3 dir)
        {
            dir = Vector3.ProjectOnPlane(dir, Vector3.up).normalized; var rot = Quaternion.LookRotation(dir);
            var n = Mathf.Max(1, Mathf.Min(7, z.a.songList?.Length ?? 1)); const float size = .9f; var w = n * size * 1.25f + .6f;
            var c = ground + Vector3.up * 1.7f;
            z.mk.Box(Paint(new Color(.56f, .56f, .6f)), c - Vector3.up * .3f - dir * .04f, new Vector3(w + .8f, size + 2.6f, .24f), rot); z.mk.Box(Paint(new Color(.46f, .46f, .5f)), c + Vector3.up * (size / 2f + 1.1f), new Vector3(w + .3f, .5f, .3f), rot); z.solid.Box(iron, c - Vector3.up * .3f, new Vector3(w + .8f, size + 2.6f, .3f), rot);
            for (var k = 0; k < 6; k++) z.mk.Box(Paint(new Color(.4f, .4f, .44f)), c + Vector3.up * (-size / 2f - 1.1f) + Vector3.Cross(Vector3.up, dir) * (-w / 2f + .4f + k * (w - .8f) / 5f) + dir * .13f, new Vector3(.1f, .4f, .02f), rot);
            z.mk.Box(Paint(new Color(.95f, .92f, .82f)), c - Vector3.up * .1f + dir * .13f, new Vector3(w, size + .9f, .04f), rot);
            ArtistBits.CoverWall(z.pictures, z.labels, z.ctx, z.a, c + dir * .16f + Vector3.up * .08f, dir, n, size, n, true, Ink);
        }

        /// A floating gold block for each song without its own piece.
        void SongLamps(Plot z)
        {
            if (!SongIds.TryGetValue(z.a.name, out var list)) return;
            var up = Vector3.up; var k = 0; var seen = new HashSet<string>();
            foreach (var (_, id) in list)
            {
                if (z.lit.Contains(id) || !seen.Add(id)) continue;
                var p = z.At(Foot(z.a) * .55f - (k % 2) * 1.3f, Foot(z.a) * .2f + (k / 2) * 1.3f);
                z.mk.Box(iron, p + up * .7f, new Vector3(.1f, 1.4f, .1f), z.Rot); z.mk.Box(SongGlow(id, Gold, 1.4f), p + up * 1.8f, new Vector3(.6f, .6f, .6f), z.Rot); ArtistBits.PostCollider(z.root, p, 1.4f, .1f);
                k++;
            }
        }

        /// A raised square of blocks, grass on top, dirt down the sides, the artist's name on a block sign.
        PlaceFrame Base(Plot z)
        {
            var up = Vector3.up; var f = new PlaceFrame(z.At(-Foot(z.a) * .45f, 0), z.front); var w = Foot(z.a) * 1.3f;
            z.mk.Box(dirt, z.c - up * .3f, new Vector3(w, .8f, w), z.Rot); z.mk.Box(Paint(new Color(.34f, .7f, .3f)), z.c + up * .1f, new Vector3(w + .04f, .02f, w + .04f), z.Rot); for (var k = 0; k < 8; k++) z.mk.Box(Paint(new Color(.3f, .62f, .26f)), z.c + up * .02f + z.Rot * new Vector3(w / 2f + .01f, 0, -w / 2f + (k + .5f) * w / 8f), new Vector3(.02f, .24f, .5f), z.Rot);
            var sg = f.o + up * .1f; z.mk.Box(trunkM, sg + up * .7f, new Vector3(.2f, 1.4f, .2f), f.r); z.mk.Box(Paint(new Color(.6f, .42f, .24f)), sg + up * 1.7f, new Vector3(2.6f, .9f, .2f), f.r); ArtistBits.Label(z.labels, z.a.name, sg + up * 1.7f + f.Fwd * .11f, f.Fwd, .2f, Gold, TextAnchor.MiddleCenter); BoxCol(z.root, sg + up * 1f, new Vector3(2.6f, 2f, .3f), f.r);
            return f;
        }

        // ---------------------------------------------------------------- ConcernedApe: the farm in four seasons
        void Farm(Plot z)
        {
            var up = Vector3.up; var rng = new System.Random(2016); var fr = z.front; var sd = z.side; var rot = z.Rot; var R = Foot(z.a) * .85f;
            var winter = Song(z, "Winter"); var spring = Song(z, "Spring"); var summer = Song(z, "Summer"); var fall = Song(z, "Fall"); life.winterSong = winter; life.springSong = spring; life.summerSong = summer; life.fallSong = fall;
            // four quarters of ground: spring green, summer gold-green, fall orange-brown, winter white-blue
            var q = new[] { (fw: 1f, rt: -1f, col: new Color(.36f, .7f, .32f)), (1f, 1f, new Color(.5f, .7f, .26f)), (-1f, 1f, new Color(.62f, .42f, .24f)), (-1f, -1f, new Color(.72f, .78f, .86f)) };
            foreach (var (fw, rt, col) in q) { var qc = z.At(fw * R / 2f, rt * R / 2f); qc.y = z.c.y; z.mk.Box(Paint(col), qc - up * .2f, new Vector3(R - .1f, .44f, R - .1f), rot); }
            z.mk.Box(dirt, z.c - up * .2f, new Vector3(R * 2f + .4f, .4f, .8f), rot); z.mk.Box(dirt, z.c - up * .2f, new Vector3(.8f, .4f, R * 2f + .4f), rot);
            // a wooden fence round it, open at the front
            for (var k = 0; k < 4; k++) { var dir = Quaternion.AngleAxis(k * 90f, up) * fr; var sideDir = Vector3.Cross(up, dir); for (var j = -8; j <= 8; j++) { if (k == 0 && Mathf.Abs(j) <= 1) continue; var pp = z.c + dir * R + sideDir * (j * R / 8f); z.mk.Box(trunkM, pp + up * .6f, new Vector3(.14f, 1.2f, .14f), rot); if (j < 8 && !(k == 0 && j == -2)) { z.mk.Box(Paint(new Color(.66f, .5f, .32f)), pp + sideDir * R / 16f + up * .8f, new Vector3(.08f, .12f, R / 8f), Quaternion.LookRotation(sideDir)); z.mk.Box(Paint(new Color(.66f, .5f, .32f)), pp + sideDir * R / 16f + up * .45f, new Vector3(.08f, .12f, R / 8f), Quaternion.LookRotation(sideDir)); } } var fc = z.c + dir * R; if (k == 0) { foreach (var e in new[] { -1f, 1f }) BoxCol(z.root, fc + up * .6f + sideDir * e * (R / 2f + R / 16f), new Vector3(R - R / 8f, 1.2f, .2f), Quaternion.LookRotation(sideDir)); } else BoxCol(z.root, fc + up * .6f, new Vector3(.2f, 1.2f, R * 2f), Quaternion.LookRotation(dir)); }
            // spring: a blossom tree and rows of green shoots
            var sp = z.At(R * .55f, -R * .55f); BlockTree(z.ctx, z.mk, sp, 1f, Paint(new Color(1f, .7f, .84f))); z.mk.Box(SongGlow(spring, new Color(1f, .7f, .86f), 1f), sp + up * 4.62f, new Vector3(.6f, .6f, .6f), rot); for (var r = 0; r < 3; r++) for (var c = 0; c < 4; c++) z.mk.Box(Paint(new Color(.4f, .86f, .3f)), z.At(R * (.15f + r * .14f), -R * (.1f + c * .1f)) + up * .2f, new Vector3(.16f, .4f, .16f), rot);
            // summer: sunflowers and melons
            for (var k = 0; k < 6; k++) { var sf = z.At(R * (.2f + (k % 3) * .25f), R * (.25f + (k / 3) * .4f)); z.mk.Box(Paint(new Color(.3f, .6f, .2f)), sf + up * .9f, new Vector3(.1f, 1.8f, .1f), rot); z.mk.Box(k == 0 ? SongGlow(summer, Gold, 1.2f) : Paint(Gold), sf + up * 1.9f + fr * .05f, new Vector3(.6f, .6f, .08f), rot); z.mk.Box(Paint(new Color(.4f, .24f, .1f)), sf + up * 1.9f + fr * .1f, new Vector3(.24f, .24f, .02f), rot); } for (var k = 0; k < 4; k++) z.mk.Box(Paint(new Color(.2f, .6f, .3f)), z.At(R * .75f, R * (.2f + k * .18f)) + up * .2f, new Vector3(.5f, .4f, .5f), rot);
            // fall: orange trees and pumpkins
            var ft = z.At(-R * .5f, R * .5f); BlockTree(z.ctx, z.mk, ft, 1f, Paint(new Color(.96f, .5f, .14f))); z.mk.Box(SongGlow(fall, new Color(1f, .5f, .14f), 1f), ft + up * 4.62f, new Vector3(.6f, .6f, .6f), rot); for (var k = 0; k < 6; k++) z.mk.Box(Paint(new Color(.95f, .5f, .1f)), z.At(-R * (.2f + (k % 3) * .2f), R * (.15f + (k / 3) * .2f)) + up * .24f, new Vector3(.5f, .4f, .5f), Quaternion.Euler(0, k * 20f, 0));
            // winter: snow, a snowman, a frozen pond
            var sm = z.At(-R * .5f, -R * .5f); for (var k = 0; k < 3; k++) z.mk.Box(white, sm + up * (.4f + k * .7f), Vector3.one * (.9f - k * .2f), rot); z.mk.Box(Paint(new Color(1f, .5f, .1f)), sm + up * 1.82f + fr * .3f, new Vector3(.1f, .1f, .4f), rot); z.mk.Box(SongGlow(winter, new Color(.6f, .8f, 1f), 1f), z.At(-R * .3f, -R * .75f) + up * .03f, new Vector3(2.4f, .04f, 1.6f), rot); ArtistBits.PostCollider(z.root, sm, 2f, .45f); BlockTree(z.ctx, z.mk, z.At(-R * .8f, -R * .2f), .8f, white);
            // the farmhouse at the back, a coop, a scarecrow, a shipping bin
            var fh = new PlaceFrame(z.At(-R * .15f, 0) - fr * 2f, fr); var wall = Paint(new Color(.7f, .5f, .3f)); z.mk.Box(wall, fh.P(0, 1.6f, 0), new Vector3(4f, 3.2f, 3f), fh.r); z.solid.Box(wall, fh.P(0, 1.6f, 0), new Vector3(4f, 3.2f, 3f), fh.r); z.mk.Prism(Paint(new Color(.8f, .2f, .16f)), fh.P(0, 3.2f, 0), up, 3f, 1.6f, 4, .05f, 45f); z.mk.Box(trunkM, fh.P(0, 1f, 1.51f), new Vector3(.9f, 2f, .02f), fh.r); foreach (var e in new[] { -1.3f, 1.3f }) z.mk.Box(windowM(), fh.P(e, 2f, 1.51f), new Vector3(.8f, .8f, .02f), fh.r);
            var co = z.At(-R * .1f, R * .75f); z.mk.Box(Paint(new Color(.8f, .3f, .2f)), co + up * .8f, new Vector3(2f, 1.6f, 1.6f), rot); BoxCol(z.root, co + up * .8f, new Vector3(2f, 1.6f, 1.6f), rot); z.mk.Box(dark, co + up * .5f + fr * .81f, new Vector3(.5f, .6f, .02f), rot); for (var k = 0; k < 3; k++) { var ch = new MeshKit(); ch.Box(white, new Vector3(0, .2f, 0), new Vector3(.3f, .3f, .4f), Quaternion.identity); ch.Box(white, new Vector3(0, .42f, .16f), new Vector3(.18f, .2f, .18f), Quaternion.identity); ch.Box(Paint(new Color(.9f, .2f, .2f)), new Vector3(0, .56f, .16f), new Vector3(.06f, .08f, .12f), Quaternion.identity); ch.Box(Paint(Gold), new Vector3(0, .42f, .28f), new Vector3(.06f, .04f, .08f), Quaternion.identity); var cg = ch.BuildSingle("Chicken", z.root, false); var home = co + fr * (2.2f + k * .8f) + sd * (k - 1) * 1.2f; cg.transform.position = home; life.chickens.Add((cg.transform, home)); }
            var scc = z.At(R * .45f, R * .05f); z.mk.Box(trunkM, scc + up * 1f, new Vector3(.1f, 2f, .1f), rot); z.mk.Box(trunkM, scc + up * 1.5f, new Vector3(1.4f, .1f, .1f), rot); z.mk.Box(Paint(new Color(.4f, .5f, .7f)), scc + up * 1.3f, new Vector3(.6f, .7f, .3f), rot); z.mk.Box(Paint(new Color(.9f, .8f, .5f)), scc + up * 1.9f, new Vector3(.4f, .4f, .4f), rot); z.mk.Box(Paint(new Color(.5f, .3f, .2f)), scc + up * 2.16f, new Vector3(.8f, .06f, .8f), rot); ArtistBits.PostCollider(z.root, scc, 2f, .3f);
            var sb = z.At(R * .1f, -R * .2f); z.mk.Box(Paint(new Color(.5f, .34f, .2f)), sb + up * .5f, new Vector3(1.4f, 1f, 1f), rot); BoxCol(z.root, sb + up * .5f, new Vector3(1.4f, 1f, 1f), rot);
            Folk(z.At(R * .3f, -R * .3f), -fr, Resident.Pose.Stand, new[] { "Four seasons, one farm. The crops are confused but happy.", "Ship anything in the bin. Nobody collects it. That's farming.", "When winter plays, it snows. Only on my side." }, Local(Residents.Tool.None, true));
            z.area.glowLight = ArtistBits.PointLight(z.root, z.c + up * 7f, new Color(1f, .9f, .7f), 30f, 1.6f);
        }

        Material windowM() => GlowM(new Color(1f, .8f, .4f), 1.2f);

        // ---------------------------------------------------------------- meganeko: the neon stage
        void Neon(Plot z)
        {
            var up = Vector3.up; var rng = new System.Random(2017); var fr = z.front; var sd = z.side; var rot = z.Rot;
            var lights = Song(z, "Lights Camera"); var soda = Song(z, "Astro Soda"); var shark = Song(z, "Sharkatzor"); life.sharkSong = shark; life.sodaSong = soda;
            var st = new PlaceFrame(z.At(-Foot(z.a) * .4f, 0), fr); z.mk.Box(Paint(new Color(.14f, .12f, .24f)), z.c - up * .2f, new Vector3(Foot(z.a) * 1.7f, .5f, Foot(z.a) * 1.6f), rot); z.mk.Box(dark, st.P(0, .5f, -1f), new Vector3(12f, 1f, 6f), st.r); z.solid.Box(dark, st.P(0, .5f, -1f), new Vector3(12f, 1f, 6f), st.r);
            // the wall of pixels
            var px = new Material(WorldMaterials.Glow(Pink, 1.2f)) { name = "Bit pixel wall" }; life.pixelWall = px; var px2 = new Material(WorldMaterials.Glow(Cyan, 1.2f)) { name = "Bit pixel wall 2" }; life.pixelWall2 = px2;
            z.mk.Box(dark, st.P(0, 4.6f, -3.9f), new Vector3(12f, 7.4f, .4f), st.r); z.solid.Box(dark, st.P(0, 4.6f, -3.9f), new Vector3(12f, 7.4f, .4f), st.r); for (var x = 0; x < 20; x++) for (var y = 0; y < 12; y++) { var on = Mathf.PerlinNoise(x * .3f, y * .3f) > .45f; if (!on) continue; z.mk.Box((x + y) % 3 == 0 ? px2 : px, st.P(-5.7f + x * .6f, 1.3f + y * .6f, -3.68f), new Vector3(.5f, .5f, .02f), st.r); }
            // pixel speaker stacks
            foreach (var e in new[] { -1f, 1f }) for (var k = 0; k < 3; k++) { var sp = st.P(e * 5.2f, 1.6f + k * 1.2f, -.6f); z.mk.Box(Paint(new Color(.2f, .2f, .26f)), sp, new Vector3(1.4f, 1.1f, 1f), st.r); z.mk.Box(dark, sp + st.Fwd * .51f, new Vector3(.8f, .8f, .02f), st.r); z.mk.Box(GlowM(Cyan, 1.2f), sp + st.Fwd * .52f, new Vector3(.3f, .3f, .02f), st.r); }
            // "Astro Soda": a giant soda can
            var can = z.At(2f, -Foot(z.a) * .55f); z.mk.Prism(Paint(new Color(.9f, .2f, .3f)), can, up, 1f, 3.2f, 14); z.mk.Prism(Paint(new Color(.8f, .8f, .84f)), can + up * 3.2f, up, .9f, .2f, 14); z.mk.Prism(SongGlow(soda, Cyan, 1.2f), can + up * 1.2f, up, 1.02f, .8f, 14); z.mk.Box(Paint(new Color(.6f, .6f, .64f)), can + up * 3.42f + fr * .3f, new Vector3(.3f, .04f, .5f), rot); ArtistBits.PostCollider(z.root, can, 3.4f, 1f);
            // "Sharkatzor": a blocky shark leaping
            var sh = z.At(2f, Foot(z.a) * .55f) + up * 1.6f; var shr = Quaternion.LookRotation(-sd) * Quaternion.Euler(-20f, 0, 0); var grey = Paint(new Color(.5f, .6f, .74f)); z.mk.Box(grey, sh, new Vector3(1.2f, 1.2f, 3f), shr); z.mk.Box(white, sh + shr * new Vector3(0, -.5f, .2f), new Vector3(1.1f, .3f, 2.4f), shr); z.mk.Box(grey, sh + shr * new Vector3(0, .9f, 0), new Vector3(.2f, .9f, .8f), shr); z.mk.Box(grey, sh + shr * new Vector3(0, .3f, -1.9f), new Vector3(.2f, 1.2f, .8f), shr); for (var k = 0; k < 4; k++) z.mk.Box(white, sh + shr * new Vector3(-.3f + k * .2f, -.3f, 1.51f), new Vector3(.1f, .14f, .02f), shr); z.mk.Box(SongGlow(shark, Pink, 1.6f), sh + shr * new Vector3(.61f, .2f, 1f), new Vector3(.02f, .2f, .2f), shr); z.mk.Box(iron, sh - up * .9f, new Vector3(.2f, 1.6f, .2f), rot); BoxCol(z.root, sh - up * .4f, new Vector3(1.4f, 2.6f, 3f), shr);
            // "Lights Camera Action": a film camera on a crane arm
            var cr = z.At(-1f, Foot(z.a) * .3f); z.mk.Box(iron, cr + up * .6f, new Vector3(.8f, 1.2f, .8f), rot); var arm = cr + up * 1.2f; var tip = arm + (fr + up * .8f).normalized * 4f; z.mk.Prism(iron, arm, (tip - arm).normalized, .1f, 4f, 6); z.mk.Box(dark, tip + up * .2f, new Vector3(.5f, .5f, .9f), rot); foreach (var e in new[] { -.2f, .2f }) z.mk.Prism(dark, tip + up * .6f + fr * e, sd, .24f, .1f, 10); z.mk.Prism(SongGlow(lights, new Color(1f, .9f, .6f), 1.6f), tip + up * .2f + fr * .46f, fr, .16f, .02f, 10); BoxCol(z.root, cr + up * .6f, new Vector3(.8f, 1.2f, .8f), rot);
            Folk(z.At(Foot(z.a) * .2f, -Foot(z.a) * .2f), -fr, Resident.Pose.Stand, new[] { "Turn it up! The pixels dance with the bass.", "The shark is fine. The shark is part of the show.", "Grab a soda. It's three metres tall. Good luck." }, Local());
            z.area.glowLight = ArtistBits.PointLight(z.root, st.P(0, 4f, 3f), new Color(1f, .5f, .8f), 26f, 1.8f);
        }

        // ---------------------------------------------------------------- everyone else
        void Others(Plot z, PlaceFrame f)
        {
            var up = Vector3.up; var rot = z.Rot; var rng = new System.Random(z.a.name.Length * 7 + z.a.name[0]); var c = z.At(1f, -1.2f) + up * .12f; var fr = z.front; var sd = z.side;
            void Block(Material m, Vector3 at, float s = 1f, bool col = true) { z.mk.Box(m, at + up * s / 2f, Vector3.one * s, rot); if (col) BoxCol(z.root, at + up * s / 2f, Vector3.one * s, rot); }
            string Any() => SongIds.TryGetValue(z.a.name, out var l) && l.Length > 0 ? Song(z, l[0].title) : null;
            switch (z.a.name)
            {
                case "Arcade Player": { var s = Any(); for (var k = 0; k <= 8; k++) { var a = Mathf.PI * k / 8f; Block(stoneM, c - fr * 1.2f + sd * Mathf.Cos(a) * 2.4f + up * Mathf.Sin(a) * 2.4f, 1f, Mathf.Sin(a) < .5f); } for (var k = 0; k < 5; k++) z.mk.Prism(GlowM(k % 2 == 0 ? Cyan : Pink, 1.4f), c - fr * 1.6f + sd * (k - 2) * .8f, (up + sd * (k - 2) * .1f).normalized, .16f, .8f + (k % 3) * .4f, 5, .1f); z.mk.Prism(trunkM, c + fr * 1.2f + sd * 1.4f, up, .4f, .9f, 10); z.mk.Prism(SongGlow(s, Gold, .8f), c + fr * 1.2f + sd * 1.4f + up * .45f, up, .42f, .1f, 10); ArtistBits.PostCollider(z.root, c + fr * 1.2f + sd * 1.4f, .9f, .45f); break; }
                case "Big Giant Circles": { var s = Any(); z.mk.Blob(stoneM, c + up * .3f, new Vector3(1f, .6f, .8f), rng, .1f); z.mk.Box(SongGlow(s, Cyan, 1.4f), c + up * 1.4f, new Vector3(.1f, 1.8f, .04f), rot); z.mk.Box(Paint(Gold), c + up * 2.1f, new Vector3(.6f, .1f, .1f), rot); z.mk.Box(trunkM, c + up * 2.4f, new Vector3(.08f, .5f, .08f), rot); ArtistBits.PostCollider(z.root, c, 1f, .8f); BlockTree(z.ctx, z.mk, c - fr * 2f + sd * 2.4f, 1.3f, Paint(new Color(.3f, .8f, .5f))); break; }
                case "Blue Brew Music": { var s = Any(); life.shopSong = s; z.mk.Box(white, c + up * .55f, new Vector3(3f, 1.1f, .8f), rot); BoxCol(z.root, c + up * .55f, new Vector3(3f, 1.1f, .8f), rot); z.mk.Box(SongGlow(s, new Color(.4f, .8f, 1f), 1f), c + up * 1.12f, new Vector3(3f, .04f, .82f), rot); z.mk.Box(white, c - fr * 1.6f + up * 1.6f, new Vector3(3.6f, 3.2f, .2f), rot); z.solid.Box(white, c - fr * 1.6f + up * 1.6f, new Vector3(3.6f, 3.2f, .2f), rot); for (var k = 0; k < 4; k++) z.mk.Box(GlowM(new Color(.5f, .8f, 1f), .6f), c - fr * 1.48f + up * (1.6f + (k / 2) * .9f) + sd * ((k % 2) * 1.6f - .8f), new Vector3(1.2f, .7f, .02f), rot); var bag = new MeshKit(); bag.Box(white, new Vector3(0, .25f, 0), new Vector3(.4f, .5f, .2f), Quaternion.identity); bag.Box(Paint(new Color(.4f, .7f, 1f)), new Vector3(0, .25f, .11f), new Vector3(.2f, .2f, .02f), Quaternion.identity); Thing(bag, "Shopping bag", "a little white shopping bag", c + up * 1.14f + sd * .8f, rot, new Vector3(.4f, .5f, .3f), .3f, false); break; }
                case "Brad Knauber": { var s = Any(); for (var x = -2; x <= 2; x++) for (var y = 0; y < 4; y++) { if (Mathf.Abs(x) < 1 && y < 2) continue; Block(y == 3 ? Paint(new Color(.34f, .7f, .3f)) : stoneM, c - fr * 1.5f + sd * x + up * y); } z.mk.Box(dark, c - fr * 1.5f + up * 1f, new Vector3(.98f, 2f, .98f), rot); foreach (var e in new[] { -1f, 1f }) { var tp = c - fr * .9f + sd * e * 1.1f + up * 1.6f; z.mk.Box(trunkM, tp, new Vector3(.12f, .5f, .12f), rot); z.mk.Box(e > 0 ? SongGlow(s, new Color(1f, .7f, .2f), 2f) : GlowM(new Color(1f, .7f, .2f), 2f), tp + up * .3f, new Vector3(.14f, .14f, .14f), rot); } ArtistBits.PointLight(z.root, c - fr * .4f + up * 1.8f, new Color(1f, .7f, .3f), 6f, 1.2f); break; }
                case "C418": { var s = Song(z, "Living Mice"); life.miceSong = s; var hs = c - fr * 1f; for (var x = -1; x <= 1; x++) for (var y = 0; y < 2; y++) for (var q = -1; q <= 1; q++) { if (Mathf.Abs(x) < 1 && q == 1 && y == 0) continue; if (x == 0 && q == 0) continue; Block(Paint(new Color(.66f, .5f, .3f)), hs + sd * x + fr * q + up * y); } for (var x = -1; x <= 1; x++) for (var q = -1; q <= 1; q++) Block(trunkM, hs + sd * x + fr * q + up * 2f); z.mk.Box(SongGlow(s, new Color(1f, .8f, .5f), 1f), hs + up * .5f + fr * 1.51f + sd * .9f, new Vector3(.4f, .4f, .02f), rot); BlockTree(z.ctx, z.mk, c + sd * 3f, 1f, leafM); break; }
                case "Captainsparklez": { var rv = Song(z, "Revenge"); var fk = Song(z, "Fallen Kingdom"); var cs = c - fr * 1f; for (var x = -2; x <= 2; x++) for (var y = 0; y < 3; y++) { if (Mathf.Abs(x) < 2 && y < 2 && x != 0) continue; if ((x + y) % 4 == 3) continue; Block(stoneM, cs + sd * x + up * y); } for (var x = -2; x <= 2; x += 2) Block(stoneM, cs + sd * x + up * 3f); Block(Paint(Gold), cs + up * 1.2f, .3f, false); z.mk.Box(SongGlow(fk, Gold, 1.6f), cs + up * 1.6f, new Vector3(.5f, .2f, .5f), rot); for (var k = 0; k < 4; k++) z.mk.Box(Paint(Gold), cs + up * 1.8f + Quaternion.Euler(0, k * 90f, 0) * fr * .2f, new Vector3(.1f, .2f, .1f), rot); var tn = c + sd * 2.6f + fr * 1f; Block(Paint(new Color(.86f, .2f, .16f)), tn); z.mk.Box(white, tn + up * .5f + fr * .51f, new Vector3(1f, .3f, .02f), rot); z.mk.Box(SongGlow(rv, new Color(1f, .5f, .3f), 1.4f), tn + up * 1.1f, new Vector3(.06f, .3f, .06f), rot); break; }
                case "Halo": { var s = Any(); var purple = Paint(new Color(.42f, .2f, .6f)); for (var k = 0; k < 4; k++) z.mk.Prism(purple, c - fr * .6f + up * (k * 1.2f), (up + fr * (k % 2 == 0 ? .2f : -.2f)).normalized, 1.2f - k * .25f, 1.3f, 5, .7f, k * 20f); z.mk.Blob(SongGlow(s, new Color(.8f, .4f, 1f), 1.6f), c - fr * .6f + up * 5.2f, Vector3.one * .3f, rng, 0f); ArtistBits.PostCollider(z.root, c - fr * .6f, 4.8f, 1.2f); break; }
                case "Jack Wall": { var s = Any(); Block(trunkM, c, 1f); foreach (var e in new[] { -.22f, .22f }) { var bt = c + up * 1f + sd * e; var snake = Paint(new Color(.7f, .6f, .36f)); z.mk.Box(snake, bt + up * .36f, new Vector3(.2f, .7f, .26f), rot); z.mk.Box(snake, bt + up * .08f + fr * .12f, new Vector3(.2f, .16f, .5f), rot); z.mk.Box(dark, bt + up * .02f + fr * .12f, new Vector3(.22f, .04f, .52f), rot); } z.mk.Box(SongGlow(s, Gold, 1.2f), c + up * 1.02f, new Vector3(.9f, .02f, .9f), rot); break; }
                case "Kenneth C M Young": { var s = Any(); var cols = new[] { new Color(1f, .5f, .6f), new Color(.6f, .8f, 1f), new Color(1f, .86f, .4f), new Color(.7f, 1f, .6f) }; for (var k = 0; k < 9; k++) { var fp = c + Quaternion.Euler(0, k * 40f, 0) * fr * (1f + (k % 3) * .7f); var h = .6f + (k % 3) * .4f; z.mk.Box(Paint(new Color(.4f, .7f, .4f)), fp + up * h / 2f, new Vector3(.06f, h, .06f), rot); for (var j = 0; j < 4; j++) z.mk.Tri(k == 0 && j == 0 ? SongGlow(s, cols[k % 4], 1.2f) : GlowM(cols[k % 4], .6f), fp + up * h, fp + up * h + Quaternion.Euler(0, j * 90f + k * 13f, 0) * (fr * .4f + up * .2f), fp + up * h + Quaternion.Euler(0, j * 90f + 45f + k * 13f, 0) * fr * .25f, fp + up * (h - 1f)); } ArtistBits.PostCollider(z.root, c, 1f, 2f); break; }
                case "Lena Raine": { var s = Any(); for (var k = 0; k < 6; k++) Block(k == 5 ? Paint(new Color(.34f, .7f, .3f)) : stoneM, c - fr * (k * .9f - 1f) + up * (k * .5f - .5f), 1f); z.mk.Prism(SongGlow(s, new Color(1f, .7f, .4f), 1.6f), c - fr * 6f + up * 5f, fr, 1.6f, .1f, 4, 1f, 45f); halos.Add(c - fr * 5.6f + up * 5f, 4f, new Color(1f, .7f, .4f), .3f); z.mk.Box(iron, c - fr * 6.1f + up * 2f, new Vector3(.12f, 4f, .12f), rot); break; }
                case "Martin O'Donnell": { var reach = Song(z, "Ghosts of Reach"); var theme = Song(z, "Halo Theme"); life.haloSong = theme; var mn = c - fr * .8f; var silver = Paint(new Color(.62f, .64f, .7f)); z.mk.Box(silver, mn + up * 2.4f, new Vector3(1.4f, 4.8f, .9f), rot * Quaternion.Euler(0, 0, 6f)); z.mk.Box(silver, mn + up * 4.9f + sd * .3f, new Vector3(2f, .4f, 1f), rot * Quaternion.Euler(0, 0, -14f)); z.solid.Box(silver, mn + up * 2.4f, new Vector3(1.4f, 4.8f, .9f), rot); for (var k = 0; k < 5; k++) z.mk.Box(k == 2 ? SongGlow(theme, new Color(.3f, .7f, 1f), 1.8f) : GlowM(new Color(.3f, .7f, 1f), 1.4f), mn + up * (.8f + k * .9f) + fr * .46f, new Vector3(.9f - (k % 2) * .4f, .06f, .02f), rot); z.mk.Prism(SongGlow(reach, new Color(.5f, .9f, 1f), .8f), mn + fr * 2f, up, .9f, .04f, 6); z.mk.Prism(GlowM(new Color(.5f, .9f, 1f), .3f), mn + fr * 2f + up * .05f, up, .6f, 2f, 6, .2f, 0f, false); break; }
                case "Nobuo Uematsu": { var s = Any(); life.angelSong = s; Block(stoneM, c, 1.2f); var wb = c + up * 1.2f; for (var k = 0; k < 6; k++) { var a = k / 5f; z.mk.Tri(k == 0 ? SongGlow(s, new Color(.3f, .2f, .4f), .6f) : Paint(new Color(.06f, .06f, .1f)), wb + up * .2f, wb + up * (1f + a * 2.6f) + sd * (1.6f - a * .4f) - fr * a * .4f, wb + up * (.8f + a * 2.2f) + sd * (2.4f - a * 1f) - fr * a * .2f, wb - fr * 2f); } z.mk.Box(Paint(new Color(.8f, .82f, .86f)), c + up * 2.8f - sd * .7f + fr * .4f, new Vector3(.06f, 3.6f, .14f), rot * Quaternion.Euler(0, 0, -10f)); z.mk.Box(dark, c + up * 1f - sd * .4f + fr * .4f, new Vector3(.3f, .1f, .1f), rot); break; }
                case "SIE Sound Team": { var s = Any(); life.dreamSong = s; var lp = c; z.mk.Box(iron, lp + up * 1.4f, new Vector3(.12f, 2.8f, .12f), rot); z.mk.Box(iron, lp + up * 2.9f, new Vector3(.5f, .1f, .5f), rot); z.mk.Box(SongGlow(s, new Color(1f, .8f, .5f), 1.6f), lp + up * 3.2f, new Vector3(.36f, .5f, .36f), rot); z.mk.Cone(iron, lp + up * 3.46f, .32f, .4f, 4, 45f); ArtistBits.PostCollider(z.root, lp, 2.8f, .14f); for (var k = 0; k < 14; k++) { var fp = c + Quaternion.Euler(0, k * 77f, 0) * fr * (.8f + (k % 4) * .5f); z.mk.Box(Paint(new Color(.3f, .5f, .3f)), fp + up * .2f, new Vector3(.04f, .4f, .04f), rot); z.mk.Box(white, fp + up * .44f, new Vector3(.18f, .08f, .18f), Quaternion.Euler(0, k * 30f, 0)); } Block(stoneM, c - fr * 1.6f + sd * 1.4f, .8f); break; }
                case "Super Mario 64": { var s = Any(); life.bombSong = s; for (var k = 0; k < 3; k++) { var w = 3.6f - k * 1.1f; z.mk.Box(Paint(new Color(.34f, .7f, .3f)), c - fr * 1f + up * (k * .6f + .3f), new Vector3(w, .6f, w), rot); z.solid.Box(dirt, c - fr * 1f + up * (k * .6f + .3f), new Vector3(w, .6f, w), rot); } var bb = c + sd * 2.4f + fr * .6f; z.mk.Blob(Paint(new Color(.1f, .1f, .12f)), bb + up * .5f, Vector3.one * .5f, rng, 0f); z.mk.Box(Paint(new Color(.8f, .7f, .3f)), bb + up * 1.06f, new Vector3(.14f, .2f, .14f), rot); z.mk.Box(SongGlow(s, new Color(1f, .5f, .2f), 2f), bb + up * 1.22f, new Vector3(.06f, .12f, .06f), rot); foreach (var e in new[] { -1f, 1f }) z.mk.Box(white, bb + up * .6f + fr * .46f + sd * e * .16f, new Vector3(.08f, .2f, .02f), rot); ArtistBits.PostCollider(z.root, bb, 1f, .5f); var stp = c - fr * 1f + up * 2f; for (var k = 0; k < 5; k++) { var a0 = Quaternion.AngleAxis(k * 72f, fr) * up; z.mk.Box(GlowM(Gold, 1.6f), stp + up * 1.2f + a0 * .3f, new Vector3(.2f, .6f, .1f), Quaternion.LookRotation(fr, a0)); } break; }
                case "Toby Fox": { var s = Any(); life.megaSong = s; for (var k = 0; k < 6; k++) { var pp = c - fr * (k / 2) * 1.8f + sd * ((k % 2) * 4f - 2f); z.mk.Box(Paint(new Color(.9f, .76f, .3f)), pp + up * 2.4f, new Vector3(.7f, 4.8f, .7f), rot); BoxCol(z.root, pp + up * 2.4f, new Vector3(.7f, 4.8f, .7f), rot); z.mk.Box(GlowM(new Color(1f, .86f, .4f), 1.4f), pp + up * 3.4f - sd * Mathf.Sign((k % 2) - .5f) * .36f, new Vector3(.02f, 2f, .5f), rot); } var ht = c - fr * 1.8f + up * 1.6f; string[] h = { "XX.XX", "XXXXX", ".XXX.", "..X.." }; for (var y = 0; y < 4; y++) for (var x = 0; x < 5; x++) if (h[y][x] == 'X') z.mk.Box(SongGlow(s, new Color(1f, .1f, .14f), 1.6f), ht + sd * ((x - 2f) * .14f) + up * ((2f - y) * .14f), new Vector3(.13f, .13f, .13f), rot); break; }
                case "she": { var s = Any(); var at = c + up * 2f; Block(SongGlow(s, Pink, 1.2f), at - up * .3f, .6f, false); for (var r = 0; r < 3; r++) { var axis = Quaternion.Euler(r * 60f, r * 40f, 0) * Vector3.up; for (var k = 0; k < 12; k++) { var p = at + Quaternion.AngleAxis(k * 30f, axis) * Vector3.Cross(axis, Vector3.forward).normalized * 1.3f; z.mk.Box(GlowM(r == 0 ? Cyan : r == 1 ? Gold : Pink, 1f), p, Vector3.one * .14f, Quaternion.identity); } } z.mk.Box(iron, c + up * .8f, new Vector3(.1f, 1.6f, .1f), rot); ArtistBits.PostCollider(z.root, c, 3f, 1.2f); break; }
                case "Daniele Zandara": { var titles = new[] { ("Copper Green", new Color(.4f, 1f, .5f)), ("Moms are Tough", new Color(1f, .6f, .4f)), ("Red & Blue", new Color(.9f, .3f, .5f)), ("Reincarnated", new Color(.5f, .6f, 1f)) }; var cs = c - fr * 1f; for (var x = -2; x <= 2; x++) for (var y = 0; y < 3; y++) { if (Mathf.Abs(x) < 1 && y < 2) continue; if (y == 2 && x % 2 != 0) continue; Block(Paint(new Color(.3f, .26f, .34f)), cs + sd * x + up * y); } z.mk.Box(dark, cs + up * 1f, new Vector3(.98f, 2f, .98f), rot); for (var k = 0; k < 4; k++) { var (t, col) = titles[k]; var sg = Song(z, t); var tp = c + fr * .8f + sd * (-1.8f + k * 1.2f); z.mk.Box(iron, tp + up * .6f, new Vector3(.1f, 1.2f, .1f), rot); z.mk.Box(SongGlow(sg, col, 1.8f), tp + up * 1.3f, Vector3.one * .22f, rot); ArtistBits.PostCollider(z.root, tp, 1.4f, .12f); } for (var k = 0; k < 9; k++) z.mk.Box(coinM, c + sd * 2.6f + Quaternion.Euler(0, k * 40f, 0) * fr * .3f + up * (.1f + (k % 3) * .1f), new Vector3(.24f, .06f, .24f), rot); break; }
                case "Filippo Vicarelli": { var s = Any(); var sc = c; z.mk.Box(trunkM, sc + up * 1.3f, new Vector3(.12f, 2.6f, .12f), rot * Quaternion.Euler(0, 0, 8f)); ArtistBits.PostCollider(z.root, sc, 2.6f, .12f); for (var k = 0; k < 6; k++) z.mk.Box(SongGlow(k == 0 ? s : null, new Color(.8f, .86f, .9f), 1f), sc + up * (2.5f - k * .1f) - sd * (.2f + k * .26f), new Vector3(.28f, .12f, .06f), rot); break; }
                case "Patryk Scelina": { var s = Any(); Block(Paint(new Color(.4f, .4f, .44f)), c, .8f); z.mk.Box(Paint(new Color(.3f, .3f, .34f)), c + up * 1.05f, new Vector3(.8f, .5f, .8f), rot); z.mk.Box(SongGlow(s, new Color(1f, .8f, .3f), 1.2f), c + up * 1.31f, new Vector3(.7f, .02f, .7f), rot); for (var k = 0; k < 3; k++) { var sk = c + up * 1.6f + Quaternion.Euler(0, k * 120f, 0) * fr * .5f; z.mk.Box(white, sk, Vector3.one * .2f, rot); } for (var k = 0; k < 8; k++) z.mk.Box(Paint(new Color(1f, .55f, .1f)), c + Quaternion.Euler(0, k * 45f, 0) * fr * .9f + up * .06f, Vector3.one * .14f, rot); break; }
                default: { var s = Any(); z.mk.Box(iron, c + up * .7f, new Vector3(.1f, 1.4f, .1f), rot); z.mk.Box(SongGlow(s, Gold, 1.4f), c + up * 1.8f, Vector3.one * .6f, rot); ArtistBits.PostCollider(z.root, c, 1.4f, .1f); break; }
            }
        }
    }
}
