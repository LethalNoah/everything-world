using System.Collections.Generic;
using UnityEngine;
using static EverythingWorld.World.PlaceTools;

namespace EverythingWorld.World
{
    /// Boom Bap Harbour's artists. Most have a stoop of their own (a slice of brownstone front with steps to sit on)
    /// and one thing from their song beside it; covers pasted on a brick wall like posters, a plaque, a street light.
    /// - Wu-Tang Clan (the largest, at the harbour): the orange ferry at its slip, and the Chamber, a hall made of
    ///   shipping containers: a boxing ring with a chessboard for a floor ("Da Mystery of Chessboxin'"), wooden
    ///   training dummies, a door numbered 7 ("7th Chamber"), a cash register that rains ("C.R.E.A.M.": bills fall on
    ///   the Block), a scarf on a hook ("Protect Ya Neck"), a hive ("Method Man"). Their songs bring out the bees.
    /// - The Notorious B.I.G.: a brownstone stoop, a gold crown on a cushion, a console and a TV to play, a magazine
    ///   rack, IT WAS ALL A DREAM in bulbs ("Juicy": gold rises). Outkast: a small stage with a long green car parked
    ///   on it, an instant camera on a tripod to shake pictures out of ("Hey Ya!"), a porch swing under a little rain
    ///   cloud ("Ms. Jackson": a summer shower), a drum machine going very fast ("B.O.B.").
    /// - JAŸ-Z: a model skyline with a yellow cab ("Empire State of Mind": evening comes and every window lights) and a
    ///   wall of 99 tally marks. A Tribe Called Quest: a can to kick. Fugees: a guitar and a letter. Grandmaster
    ///   Flash: two turntables and a wall that says DON'T PUSH ME. Kurtis Blow: a basketball court, balls to throw.
    ///   Beastie Boys: a patrol car with its lights on. Also AZ's chess table, Cannibal Ox's iron pigeons, Diddy's
    ///   candles, RZA's projector, Shabazz Palaces' black arch, T La Rock's gift, Ceren Yaldız' payphone.
    public partial class BoomKit
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
                Add("Wu-Tang Clan", "C.R.E.A.M.", "@down the block"); Add("JAŸ-Z", "Empire State", "@down the block"); Add("The Notorious B.I.G.", "Juicy", "The Notorious B.I.G.");
                Add("Outkast", "Ms. Jackson", "Outkast"); Add("Beastie Boys", "Sabotage", "Beastie Boys");
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

        /// Every song of this artist whose title starts with 'title', marked.
        List<string> Songs(Plot z, string title) { var l = new List<string>(); if (SongIds.TryGetValue(z.a.name, out var list)) foreach (var (t, id) in list) if (title == null || t.StartsWith(title)) { z.lit.Add(id); l.Add(id); } return l; }

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
            z.mk.Prism(a.name == "Kurtis Blow" ? asphalt : walk, z.c - up * .25f, up, reach * .66f, .33f, 22);
            switch (a.name)
            {
                case "Wu-Tang Clan": WuTang(z); break;
                case "The Notorious B.I.G.": Biggie(z); break;
                case "Outkast": Outkast(z); break;
                case "JAŸ-Z": JayZ(z); break;
                case "Kurtis Blow": KurtisBlow(z); break;
                default: Stoop(z); Others(z); break;
            }
            SongLamps(z);
            Board(z, z.At(reach * .6f, -reach * .45f - 1.5f), front);
            ArtistBits.Plaque(z.mk, z.solid, labels, a, z.At(reach * .6f + 1.5f, reach * .45f + 1.5f), front, iron, chain, Cream, 1.6f);
            StreetLight(ctx, z.mk, z.At(reach * .5f, reach * .64f), -z.side);
            if (z.area.glowLight == null) z.area.glowLight = ArtistBits.PointLight(root, z.c + up * 5f + front * 3f, new Color(1f, .9f, .7f), HasStation(a) ? 24f : 16f, 1.2f);
            area.glowRenderers = new Renderer[0];
            z.mk.Build("Area", root, false);
            z.solid.BuildCollider("Walls", root);
            z.pictures.Build("Covers", root, false);
            noGrass.Add(new Vector3(z.c.x, reach * .9f, z.c.z));
            if (HasStation(a) || reach >= 11f)
            {
                var mark = new GameObject("Shot: " + a.name + " up close").transform; mark.SetParent(ctx.Root, false);
                var cam = z.At(reach * .85f, reach * .3f) + up * 2f; var look = z.At(-reach * .3f, 0) + up * 2.2f - cam; mark.SetPositionAndRotation(cam, Quaternion.LookRotation(look.normalized));
            }
        }

        /// The covers pasted on a piece of brick wall, like posters, a tag sprayed beside them.
        void Board(Plot z, Vector3 ground, Vector3 dir)
        {
            dir = Vector3.ProjectOnPlane(dir, Vector3.up).normalized; var rot = Quaternion.LookRotation(dir); var hue = z.a.name.Length;
            var n = Mathf.Max(1, Mathf.Min(7, z.a.songList?.Length ?? 1)); const float size = .9f; var w = n * size * 1.25f + .6f;
            var c = ground + Vector3.up * 1.7f; var brick = Paint(Browns[hue % Browns.Length]);
            z.mk.Box(brick, c - Vector3.up * .5f - dir * .06f, new Vector3(w + .9f, size + 2.2f, .3f), rot); z.solid.Box(brick, c - Vector3.up * .5f, new Vector3(w + .9f, size + 2.2f, .36f), rot); z.mk.Box(Paint(Cream), c - Vector3.up * .1f + dir * .1f, new Vector3(w, size + .9f, .03f), rot); z.mk.Box(concrete, c + Vector3.up * (size / 2f + .66f) - dir * .06f, new Vector3(w + 1.1f, .14f, .44f), rot);
            ArtistBits.CoverWall(z.pictures, z.labels, z.ctx, z.a, c + dir * .13f + Vector3.up * .08f, dir, n, size, n, true, Ink);
        }

        /// A spray can on the kerb, lit, for each song without its own piece.
        void SongLamps(Plot z)
        {
            if (!SongIds.TryGetValue(z.a.name, out var list)) return;
            var up = Vector3.up; var k = 0; var seen = new HashSet<string>();
            foreach (var (_, id) in list)
            {
                if (z.lit.Contains(id) || !seen.Add(id)) continue;
                var p = z.At(Foot(z.a) * .55f - (k % 2) * 1.3f, Foot(z.a) * .2f + (k / 2) * 1.3f);
                z.mk.Prism(concrete, p, up, .2f, .5f, 8); z.mk.Prism(SongGlow(id, Tags[k % Tags.Length], 1.4f), p + up * .5f, up, .07f, .22f, 8); z.mk.Prism(chain, p + up * .72f, up, .03f, .04f, 6); ArtistBits.PostCollider(z.root, p, .7f, .2f);
                k++;
            }
        }

        /// A stoop of their own: a slice of brownstone front, a door, a window, six steps to sit on.
        PlaceFrame Stoop(Plot z)
        {
            var up = Vector3.up; var hue = z.a.name.Length + z.a.name[0]; var f = new PlaceFrame(z.At(-Foot(z.a) * .44f, 0), z.front); var wall = Paint(Browns[hue % Browns.Length]); var trim = Paint(Browns[(hue + 2) % Browns.Length] * 1.15f);
            z.mk.Box(wall, f.P(0, 2.6f, 0), new Vector3(5.4f, 5.2f, .6f), f.r); z.solid.Box(wall, f.P(0, 2.6f, 0), new Vector3(5.4f, 5.2f, .6f), f.r); z.mk.Box(trim, f.P(0, 5.3f, .1f), new Vector3(5.6f, .4f, .8f), f.r);
            for (var k = 0; k < 5; k++) { var sy = (k + 1) * .26f; var sz = .3f + 2f - k * .42f; z.mk.Box(trim, f.P(-1.2f, sy - .13f, sz), new Vector3(1.8f, .26f, .44f), f.r); z.solid.Box(trim, f.P(-1.2f, sy / 2f, sz), new Vector3(1.8f, sy, .44f), f.r); }
            z.mk.Box(dark, f.P(-1.2f, 2.4f, .32f), new Vector3(1.1f, 2.2f, .06f), f.r); z.mk.Box(trim, f.P(-1.2f, 3.6f, .34f), new Vector3(1.5f, .24f, .14f), f.r); z.mk.Box(glassM, f.P(1.3f, 2.9f, .32f), new Vector3(1.2f, 1.9f, .06f), f.r); z.mk.Box(trim, f.P(1.3f, 3.96f, .34f), new Vector3(1.5f, .2f, .14f), f.r); z.mk.Box(trim, f.P(1.3f, 1.9f, .36f), new Vector3(1.4f, .12f, .2f), f.r);
            Interact.Bench(tlife.transform, f.P(-1.7f, .84f, 1.5f), f.P(-.7f, .84f, 1.5f), z.front);
            return f;
        }

        // ---------------------------------------------------------------- Wu-Tang Clan: the ferry and the Chamber
        void WuTang(Plot z)
        {
            var up = Vector3.up; var rot = z.Rot; var rng = new System.Random(36); foreach (var s in Songs(z, null)) life.wu.Add(s);
            var cream = Song(z, "C.R.E.A.M."); var chess = Song(z, "Da Mystery"); var meth = Song(z, "Method Man"); var neck = Song(z, "Protect Ya Neck"); var seven = Song(z, "Wu-Tang: 7th"); life.creamSong = cream;
            // the Chamber: an open-fronted hall made of shipping containers, two high, a third across the top
            var c = z.At(-5f, 0); var yel = Paint(new Color(.9f, .72f, .1f)); var blk = Paint(new Color(.12f, .12f, .13f));
            foreach (var e in new[] { -1f, 1f }) for (var k = 0; k < 2; k++) { var cp = c + z.side * e * 5.6f + up * (1.3f + k * 2.62f); z.mk.Box(k == 0 ? blk : yel, cp, new Vector3(2.5f, 2.6f, 9f), rot); for (var q = 0; q < 12; q++) z.mk.Box(dark, cp + z.front * (-4f + q * .72f) - z.side * e * 1.26f, new Vector3(.03f, 2.4f, .1f), rot); } z.solid.Box(blk, c - z.side * 5.6f + up * 2.6f, new Vector3(2.5f, 5.2f, 9f), rot); z.solid.Box(blk, c + z.side * 5.6f + up * 2.6f, new Vector3(2.5f, 5.2f, 9f), rot);
            z.mk.Box(blk, c + up * 6.56f - z.front * 2f, new Vector3(13.7f, 2.6f, 2.5f), rot); z.mk.Box(yel, c + up * 2.6f - z.front * 4.3f, new Vector3(8.7f, 5.2f, .3f), rot); z.solid.Box(yel, c + up * 2.6f - z.front * 4.3f, new Vector3(8.7f, 5.2f, .3f), rot); z.mk.Box(concrete, c + up * .06f, new Vector3(8.7f, .12f, 9f), rot);
            ArtistBits.Label(z.labels, "36 CHAMBERS", c + up * 6.56f - z.front * .74f, z.front, .9f, Yellow, TextAnchor.MiddleCenter); ArtistBits.Label(z.labels, "enter", c + up * 5.4f - z.front * .74f, z.front, .24f, Cream, TextAnchor.MiddleCenter);
            z.area.glowLight = ArtistBits.PointLight(z.root, c + up * 4f, new Color(1f, .85f, .4f), 28f, 2.6f); ArtistBits.PointLight(z.root, c + up * 3f + z.front * 5f, new Color(1f, .9f, .6f), 16f, 1.4f);
            // "Da Mystery of Chessboxin'": a boxing ring whose floor is a chessboard
            var rg = c - z.front * .6f + up * .12f; z.mk.Box(blk, rg + up * .2f, new Vector3(5.2f, .4f, 5.2f), rot); z.solid.Box(blk, rg + up * .1f, new Vector3(5.2f, .6f, 5.2f), rot); var lit = SongGlow(chess, new Color(1f, .95f, .8f), .4f);
            for (var x = 0; x < 8; x++) for (var y = 0; y < 8; y++) if ((x + y) % 2 == 0) z.mk.Box(lit, rg + up * .405f + z.side * (-2.1f + x * .6f) + z.front * (-2.1f + y * .6f), new Vector3(.6f, .01f, .6f), rot);
            foreach (var (x, y) in new[] { (-1f, -1f), (1f, -1f), (1f, 1f), (-1f, 1f) }) { var p = rg + z.side * x * 2.5f + z.front * y * 2.5f; z.mk.Prism(Paint(x * y > 0 ? Red : Sky), p + up * .4f, up, .08f, 1.5f, 6); ArtistBits.PostCollider(z.root, p, 1.9f, .1f); }
            for (var k = 0; k < 3; k++) foreach (var (a, b) in new[] { ((-1f, -1f), (1f, -1f)), ((1f, -1f), (1f, 1f)), ((1f, 1f), (-1f, 1f)), ((-1f, 1f), (-1f, -1f)) }) { if (a.Item2 > 0 && b.Item2 > 0) continue; var p0 = rg + z.side * a.Item1 * 2.5f + z.front * a.Item2 * 2.5f + up * (.9f + k * .4f); var p1 = rg + z.side * b.Item1 * 2.5f + z.front * b.Item2 * 2.5f + up * (.9f + k * .4f); z.mk.Prism(white, p0, (p1 - p0).normalized, .025f, (p1 - p0).magnitude, 4); }
            for (var k = 0; k < 6; k++) { var pp = rg + up * .41f + z.side * (-2.1f + k * .84f) + z.front * (k % 2 == 0 ? -1.5f : 1.5f); z.mk.Prism(k % 2 == 0 ? white : dark, pp, up, .14f, .1f, 8); z.mk.Prism(k % 2 == 0 ? white : dark, pp + up * .1f, up, .08f, .34f + (k % 3) * .14f, 6, .6f); z.mk.Blob(k % 2 == 0 ? white : dark, pp + up * (.5f + (k % 3) * .14f), Vector3.one * .08f, rng, 0f); }
            var gl = new MeshKit(); gl.Blob(Paint(Red), new Vector3(0, .12f, 0), new Vector3(.13f, .12f, .16f), rng, .05f); gl.Prism(white, new Vector3(0, .1f, -.16f), Vector3.back, .08f, .08f, 8); Thing(gl, "Boxing glove", "a boxing glove", rg + up * .42f + z.front * 2f + z.side * 1.6f, rot, new Vector3(.26f, .24f, .34f), .3f); Thing(gl, "Boxing glove", "the other boxing glove", rg + up * .42f + z.front * 2f + z.side * 1.2f, rot * Quaternion.Euler(0, 30f, 0), new Vector3(.26f, .24f, .34f), .3f);
            string[] box = { "Knight to f3. Then a jab. Then a rook lift. Nobody said the swords were only for one game.", "You make a move and throw a hook in the same breath. The pieces flinch.", "Check. Bell. Round two.", "A chess clock dings. Somewhere a sword comes out of a scabbard, purely for emphasis." }; var bn = 0;
            Spot("The chessboxing ring", rg + up * 1.4f + z.front * 2.6f, "Make your move", new Vector3(3f, 2f, 1.2f)).onUse = _ => { life.played = Time.time; Say1(rg + up * 3.2f, box[bn++ % box.Length]); };
            // wooden training dummies, a door numbered 7, a scarf on a hook
            for (var k = 0; k < 2; k++) { var dm = c + z.side * (k == 0 ? -3.4f : 3.4f) + z.front * 3f + up * .12f; z.mk.Prism(woodM, dm, up, .16f, 1.8f, 8); for (var q = 0; q < 3; q++) z.mk.Prism(woodM, dm + up * (1.5f - q * .4f), Quaternion.Euler(0, q * 120f + k * 40f, 0) * z.front, .04f, .6f, 5); z.mk.Prism(woodM, dm + up * .5f, (z.front + up * -.4f).normalized, .045f, .7f, 5); ArtistBits.PostCollider(z.root, dm, 1.8f, .2f); }
            var d7 = c - z.front * 4.1f + z.side * 2.6f; z.mk.Box(blk, d7 + up * 1.2f, new Vector3(1.2f, 2.3f, .1f), rot); z.mk.Blob(SongGlow(seven, Yellow, 1.8f), d7 + up * 1.1f + z.side * .4f + z.front * .08f, Vector3.one * .05f, rng, 0f); ArtistBits.Label(z.labels, "7", d7 + up * 1.8f + z.front * .07f, z.front, .5f, Yellow, TextAnchor.MiddleCenter);
            var hk = c - z.front * 4.1f - z.side * 2.6f + up * 1.9f; z.mk.Prism(chain, hk, z.front, .02f, .2f, 4); var scarf = SongGlow(neck, new Color(1f, .8f, .1f), .7f); z.mk.Box(scarf, hk + z.front * .2f - up * .5f, new Vector3(.2f, 1f, .04f), rot); z.mk.Box(scarf, hk + z.front * .22f - up * .4f + z.side * .14f, new Vector3(.2f, .8f, .04f), rot * Quaternion.Euler(0, 0, 8f)); for (var k = 0; k < 3; k++) z.mk.Box(dark, hk + z.front * .23f - up * (.2f + k * .3f), new Vector3(.21f, .06f, .05f), rot);
            // "Method Man": a hive hanging from the top container, humming
            var hv = c + up * 4.9f + z.front * 3.4f + z.side * 4f; var honey = SongGlow(meth, new Color(1f, .75f, .15f), 1.2f); for (var k = 0; k < 5; k++) z.mk.Prism(honey, hv - up * k * .26f, -up, .34f - Mathf.Abs(k - 1.5f) * .08f, .26f, 10); z.mk.Prism(dark, hv, up, .02f, .5f, 3); halos.Add(hv - up * .6f, 1.4f, Gold, .4f);
            // "C.R.E.A.M.": a cash register on a crate out front, its drawer open
            var cr = z.At(7f, 5f); z.mk.Box(woodM, cr + up * .4f, new Vector3(.8f, .8f, .8f), rot); z.mk.Box(chain, cr + up * 1f, new Vector3(.6f, .4f, .5f), rot); z.mk.Box(chain, cr + up * 1.3f - z.front * .1f, new Vector3(.5f, .26f, .3f), rot * Quaternion.Euler(-20f, 0, 0)); z.mk.Box(dark, cr + up * .86f + z.front * .4f, new Vector3(.56f, .08f, .4f), rot); var cash = SongGlow(cream, new Color(.4f, .9f, .45f), 1.3f); for (var k = 0; k < 4; k++) z.mk.Box(cash, cr + up * .91f + z.front * .4f + z.side * (-.2f + k * .13f), new Vector3(.1f, .03f, .3f), rot); BoxCol(z.root, cr + up * .6f, new Vector3(.8f, 1.3f, .8f), rot); halos.Add(cr + up * 1f + z.front * .5f, .9f, new Color(.4f, .9f, .5f), .3f);
            string[] ring = { "Ka-ching. A dollar bill floats up out of the drawer and goes off over the Block.", "Cash rules everything around you. It says so. It's the register talking.", "Dolla dolla bill, y'all. The drawer shuts itself." }; var rn = 0; Spot("The cash register", cr + up * 1.3f, "Ring it up", new Vector3(.9f, 1.2f, .9f)).onUse = _ => Say1(cr + up * 2.4f, ring[rn++ % ring.Length]);
            // the orange ferry at its slip, across the yard
            var fy = z.At(2f, -11f) + up * .3f; var fr = rot * Quaternion.Euler(0, 76f, 0); var orange = Paint(new Color(.95f, .5f, .12f)); z.mk.Box(Paint(new Color(.14f, .16f, .2f)), fy + up * .5f, new Vector3(3.6f, 1.4f, 11f), fr); z.mk.Box(orange, fy + up * 2f, new Vector3(3.4f, 1.6f, 9.6f), fr); z.mk.Box(orange, fy + up * 3.5f, new Vector3(2.8f, 1.4f, 7f), fr); z.mk.Box(orange, fy + up * 4.7f, new Vector3(1.8f, 1f, 2.4f), fr); z.mk.Prism(dark, fy + up * 4.2f - fr * Vector3.forward * 1.6f, up, .34f, 1.8f, 8);
            foreach (var e in new[] { -1f, 1f }) for (var k = 0; k < 6; k++) { z.mk.Box(glassM, fy + up * 2.2f + fr * new Vector3(e * 1.71f, 0, -3.6f + k * 1.44f), new Vector3(.03f, .7f, .9f), fr); if (k < 4) z.mk.Box(glassM, fy + up * 3.6f + fr * new Vector3(e * 1.41f, 0, -2.4f + k * 1.6f), new Vector3(.03f, .6f, .9f), fr); } BoxCol(z.root, fy + up * 2.2f, new Vector3(3.6f, 4.6f, 11f), fr); ArtistBits.Label(z.labels, "SHAOLIN FERRY", fy + up * 1.5f + fr * Vector3.right * 1.72f, fr * Vector3.right, .3f, Ink, TextAnchor.MiddleCenter);
            Folk(z.At(6f, -5f), z.front, Resident.Pose.Stand, new[] { "Next boat to Shaolin's whenever. You don't catch it. It catches you.", "Thirty-six chambers in there. I've seen four. The fourth one was just a guy with a chessboard, and he beat me.", "Protect ya neck. I mean it, there's a draught off the water." });
            Shot(z.ctx, "the chamber", z.At(15f, 3f) + up * 2.4f, c + up * 3f);
        }

        // ---------------------------------------------------------------- The Notorious B.I.G.
        void Biggie(Plot z)
        {
            var up = Vector3.up; var rot = z.Rot; var rng = new System.Random(97); foreach (var s in Songs(z, "Juicy")) life.juicy.Add(s); var ju = Song(z, "Juicy"); var bp = Song(z, "Big Poppa"); var f = Stoop(z); life.biggieAt = z.c;
            // IT WAS ALL A DREAM, in bulbs, over the door
            z.mk.Box(dark, f.P(0, 6.2f, .2f), new Vector3(5.6f, 1.2f, .12f), f.r); ArtistBits.Label(z.labels, "IT WAS ALL A DREAM", f.P(0, 6.2f, .28f), z.front, .34f, Gold, TextAnchor.MiddleCenter); var bl = SongGlow(ju, new Color(1f, .9f, .6f), 2.2f); for (var k = 0; k < 15; k++) { z.mk.Blob(bl, f.P(-2.66f + k * .38f, 6.74f, .28f), Vector3.one * .07f, rng, 0f); z.mk.Blob(bl, f.P(-2.66f + k * .38f, 5.66f, .28f), Vector3.one * .07f, rng, 0f); } halos.Add(f.P(0, 6.2f, .9f), 3f, Gold, .35f);
            // a gold crown on a velvet cushion on a pedestal
            var cp = z.At(2f, 4.6f); z.mk.Prism(concrete, cp, up, .4f, 1f, 8); z.mk.Blob(Paint(new Color(.5f, .1f, .14f)), cp + up * 1.1f, new Vector3(.36f, .1f, .36f), rng, .05f); z.mk.Prism(goldM, cp + up * 1.2f, up, .22f, .16f, 10); for (var k = 0; k < 5; k++) z.mk.Cone(goldM, cp + up * 1.36f + Quaternion.Euler(0, k * 72f, 0) * Vector3.forward * .19f, .05f, .16f, 4); ArtistBits.PostCollider(z.root, cp, 1.3f, .4f); halos.Add(cp + up * 1.4f, 1.2f, Gold, .5f);
            z.area.glowLight = ArtistBits.PointLight(z.root, z.c + up * 3.6f + z.front * 2f, new Color(1f, .85f, .5f), 24f, 1.6f);
            // "Juicy": a TV on a milk crate with a games console; a rack of magazines; a red and black jacket with a hat to match
            var tv = z.At(1f, -4.6f); Crate(z.mk, tv, z.front, 2, false); z.mk.Box(dark, tv + up * .7f, new Vector3(.8f, .6f, .6f), rot); var scr = SongGlow(ju, new Color(.4f, .7f, 1f), 1.2f); z.mk.Box(scr, tv + up * .72f + z.front * .31f, new Vector3(.6f, .44f, .02f), rot); z.mk.Box(chain, tv + z.front * .9f + up * .06f, new Vector3(.4f, .08f, .3f), rot); z.mk.Box(dark, tv + z.front * 1.3f + z.side * .2f + up * .04f, new Vector3(.16f, .04f, .08f), rot); Crate(z.mk, tv + z.front * 1.9f, -z.front, 5);
            string[] game = { "Bleep. You get to the second world. In 1994 this was the height of luxury.", "Two controllers, one works. You get the one that works. That's hospitality.", "You pause it. Outside, somebody's sipping something fancy when they used to be thirsty." }; var gn = 0; Spot("The console", tv + z.front * .9f + up * .5f, "Play the console", new Vector3(.9f, .8f, .9f)).onUse = _ => Say1(tv + up * 2f, game[gn++ % game.Length]);
            var mg = z.At(5.4f, -2f); z.mk.Box(chain, mg + up * .7f, new Vector3(1.2f, 1.4f, .1f), rot); for (var k = 0; k < 6; k++) z.mk.Box(Paint(Tags[k % Tags.Length]), mg + up * (.4f + (k / 3) * .5f) + z.side * (-.36f + (k % 3) * .36f) + z.front * .08f, new Vector3(.3f, .4f, .02f), rot * Quaternion.Euler(-8f, 0, 0)); BoxCol(z.root, mg + up * .7f, new Vector3(1.2f, 1.4f, .2f), rot); var mz = new MeshKit(); mz.Box(Paint(Tags[0]), new Vector3(0, .01f, 0), new Vector3(.24f, .02f, .32f), Quaternion.identity); mz.Box(white, new Vector3(0, .022f, .08f), new Vector3(.2f, .004f, .06f), Quaternion.identity); Thing(mz, "Magazine", "a rap magazine, well read", mg + z.front * .7f + up * .12f, rot, new Vector3(.24f, .05f, .32f), .1f);
            var jk = z.At(-1f, 5.6f); z.mk.Prism(iron, jk, up, .16f, .03f, 8); z.mk.Prism(iron, jk, up, .02f, 1.6f, 4); for (var r = 0; r < 4; r++) for (var q = 0; q < 3; q++) z.mk.Box(Paint((r + q) % 2 == 0 ? Red : Ink), jk + up * (.8f + r * .2f) + z.side * (-.2f + q * .2f), new Vector3(.2f, .2f, .16f), rot); z.mk.Prism(Paint(Red), jk + up * 1.66f, up, .2f, .08f, 10); z.mk.Box(Paint(Ink), jk + up * 1.66f + z.front * .2f, new Vector3(.3f, .02f, .2f), rot); ArtistBits.PostCollider(z.root, jk, 1.7f, .3f);
            // "Big Poppa": a table for two with champagne
            var tb = z.At(5.4f, 3.2f); z.mk.Prism(iron, tb, up, .04f, .72f, 5); z.mk.Prism(white, tb + up * .72f, up, .46f, .04f, 12); ArtistBits.PostCollider(z.root, tb, .8f, .36f); z.mk.Prism(Paint(new Color(.1f, .3f, .16f)), tb + up * .76f, up, .05f, .2f, 8); z.mk.Prism(SongGlow(bp, Gold, 1.4f), tb + up * .96f, up, .02f, .12f, 6); Crate(z.mk, tb + z.side * .9f, -z.side, 0); Crate(z.mk, tb - z.side * .9f, z.side, 3);
            Folk(f.P(1.6f, 0, 1.2f), z.front, Resident.Pose.Lean, new[] { "He used to rhyme right there on that corner. You could hear it three blocks off. You could hear it thinking.", "Biggest voice this block ever had. Still got it. Listen.", "Spread love. That's the Brooklyn way. He said it. We kept it." });
        }

        // ---------------------------------------------------------------- Outkast
        void Outkast(Plot z)
        {
            var up = Vector3.up; var rot = z.Rot; var rng = new System.Random(2000); var hy = Song(z, "Hey Ya!"); var mj = Song(z, "Ms. Jackson"); var bob = Song(z, "B.O.B."); life.heyYaSong = hy; life.jacksonSong = mj; life.bobSong = bob; var c = z.At(-4f, 0);
            // a small stage in green and purple with a long low car parked on it
            var grn = Paint(new Color(.2f, .6f, .3f)); var pur = Paint(new Color(.5f, .3f, .7f)); z.mk.Box(pur, c + up * .25f, new Vector3(8f, .5f, 4.4f), rot); z.solid.Box(pur, c + up * .05f, new Vector3(8f, .9f, 4.4f), rot); z.mk.Box(grn, c + up * 2.6f - z.front * 2.1f, new Vector3(8f, 4.4f, .2f), rot); z.solid.Box(grn, c + up * 2.6f - z.front * 2.1f, new Vector3(8f, 4.4f, .2f), rot); for (var k = 0; k < 7; k++) z.mk.Box(k % 2 == 0 ? pur : Paint(Yellow), c + up * 2.6f - z.front * 1.98f + z.side * (-3.3f + k * 1.1f), new Vector3(.3f, 4.2f, .04f), rot);
            for (var k = 0; k < 2; k++) { z.mk.Box(pur, c + up * (.12f) + z.front * (2.4f + k * .0f) - up * .0f, new Vector3(2f, .25f, .5f), rot); } z.solid.Box(pur, c + up * .12f + z.front * 2.4f, new Vector3(2f, .25f, .5f), rot);
            var car = c + up * .5f - z.front * .4f; var cr = rot * Quaternion.Euler(0, 90f, 0); var cf = cr * Vector3.forward; var cs = cr * Vector3.right; var body = Paint(new Color(.3f, .7f, .4f)); z.mk.Box(body, car + up * .6f, new Vector3(1.9f, .56f, 5.6f), cr); z.mk.Box(Paint(Cream), car + up * 1.1f - cf * .2f, new Vector3(1.7f, .5f, 2.2f), cr); z.mk.Box(glassM, car + up * 1.12f - cf * .2f, new Vector3(1.74f, .36f, 1.9f), cr); foreach (var e in new[] { -1f, 1f }) { z.mk.Box(body, car + up * .9f - cf * 2.5f + cs * e * .8f, new Vector3(.12f, .5f, 1f), cr * Quaternion.Euler(-16f, 0, 0)); foreach (var q in new[] { -1.8f, 1.8f }) { z.mk.Prism(dark, car + up * .36f + cs * e * .86f + cf * q, cs * e, .38f, .2f, 12); z.mk.Prism(white, car + up * .36f + cs * e * .97f + cf * q, cs * e, .26f, .01f, 12); } z.mk.Blob(bulbM, car + up * .6f + cs * e * .7f + cf * 2.82f, Vector3.one * .1f, rng, 0f); } z.mk.Box(chain, car + up * .4f + cf * 2.82f, new Vector3(1.9f, .14f, .08f), cr); BoxCol(z.root, car + up * .8f, new Vector3(1.9f, 1.5f, 5.6f), cr);
            z.area.glowLight = ArtistBits.PointLight(z.root, c + up * 4f + z.front * 2f, new Color(.8f, 1f, .7f), 24f, 1.6f);
            // "Hey Ya!": an instant camera on a tripod: shake it
            var cm = z.At(4f, -5f); for (var k = 0; k < 3; k++) z.mk.Prism(iron, cm + Quaternion.Euler(0, k * 120f, 0) * Vector3.forward * .4f, (up * 1.2f - Quaternion.Euler(0, k * 120f, 0) * Vector3.forward * .4f).normalized, .02f, 1.27f, 4); z.mk.Box(white, cm + up * 1.36f, new Vector3(.36f, .28f, .34f), rot); z.mk.Prism(dark, cm + up * 1.38f + z.front * .17f, z.front, .09f, .06f, 10); var rb = SongGlow(hy, new Color(1f, .4f, .5f), 1.4f); z.mk.Box(rb, cm + up * 1.26f + z.front * .171f, new Vector3(.34f, .03f, .01f), rot); ArtistBits.PostCollider(z.root, cm, 1.5f, .3f);
            for (var k = 0; k < 5; k++) { z.mk.Box(white, cm + z.front * (.8f + k * .3f) + z.side * Mathf.Sin(k * 2f) * .4f + up * .11f, new Vector3(.2f, .01f, .24f), rot * Quaternion.Euler(0, k * 40f, 0)); z.mk.Box(SongGlow(hy, Tags[k % Tags.Length], .5f), cm + z.front * (.8f + k * .3f) + z.side * Mathf.Sin(k * 2f) * .4f + up * .116f + rot * Quaternion.Euler(0, k * 40f, 0) * Vector3.forward * .02f, new Vector3(.16f, .01f, .16f), rot * Quaternion.Euler(0, k * 40f, 0)); }
            string[] shake = { "Click, whirr. You shake it. Shake it. Sh-shake it. The picture is of you, shaking a picture.", "A square photo slides out. Everybody in it is mid-clap. One, two, three, uh!", "You shake it and the whole stage counts you in.", "The photo develops into the exact colour of this afternoon." }; var sn = 0; Spot("The camera", cm + up * 1.4f, "Shake it", new Vector3(.9f, 1.4f, .9f)).onUse = _ => { life.played = Time.time; Say1(cm + up * 2.6f, shake[sn++ % shake.Length]); };
            // "Ms. Jackson": a porch swing under its own small rain cloud, an umbrella, roses that mean sorry
            var sw = z.At(4.5f, 5f); foreach (var e in new[] { -1f, 1f }) { z.mk.Prism(white, sw + z.side * e * 1.2f - up * .2f, up, .07f, 2.7f, 5); ArtistBits.PostCollider(z.root, sw + z.side * e * 1.2f, 2.5f, .09f); foreach (var q in new[] { -.6f, .6f }) if (e < 0) z.mk.Prism(chain, sw + up * 2.4f + z.side * q, -up, .012f, 1.8f, 3); } z.mk.Box(white, sw + up * 2.5f, new Vector3(2.6f, .1f, .1f), rot); z.mk.Box(white, sw + up * .5f, new Vector3(1.5f, .06f, .5f), rot); z.mk.Box(white, sw + up * .9f - z.front * .24f, new Vector3(1.5f, .6f, .05f), rot); Interact.Bench(tlife.transform, sw + up * .56f - z.side * .4f, sw + up * .56f + z.side * .4f, z.front);
            for (var k = 0; k < 3; k++) z.mk.Blob(SongGlow(mj, new Color(.6f, .66f, .8f), .4f), sw + up * 4f + z.side * (-.7f + k * .7f) + up * ((k % 2) * .2f), new Vector3(.8f, .44f, .6f), rng, .3f); for (var k = 0; k < 7; k++) z.mk.Box(SongGlow(mj, Sky, 1.2f), sw + up * (3.4f - (k % 3) * .3f) + z.side * (-.9f + k * .3f) + z.front * ((k % 2) * .3f - .15f), new Vector3(.02f, .2f, .02f), rot);
            var rs = sw + z.side * 2f; z.mk.Prism(Paint(Sky * .8f), rs, up, .14f, .34f, 8, 1.2f); for (var k = 0; k < 5; k++) { z.mk.Prism(leafM, rs + up * .3f, (up + Quaternion.Euler(0, k * 72f, 0) * Vector3.forward * .3f).normalized, .01f, .44f, 3); z.mk.Blob(Paint(Red), rs + up * .3f + (up + Quaternion.Euler(0, k * 72f, 0) * Vector3.forward * .3f).normalized * .46f, Vector3.one * .06f, rng, .2f); } ArtistBits.Label(z.labels, "sorry\n(for real)", rs + up * .9f + z.front * .1f, z.front, .1f, Ink, TextAnchor.MiddleCenter);
            // "B.O.B.": a drum machine going very fast
            var dmm = z.At(7.5f, 0f); z.mk.Box(dark, dmm + up * .8f, new Vector3(.9f, .14f, .6f), rot * Quaternion.Euler(-14f, 0, 0)); foreach (var e in new[] { -.4f, .4f }) z.mk.Prism(iron, dmm + z.side * e, up, .02f, .76f, 4); var pads = SongGlow(bob, new Color(1f, .5f, .15f), 2f); for (var k = 0; k < 8; k++) z.mk.Box(k % 3 == 0 ? pads : chain, dmm + up * .89f + z.side * (-.34f + (k % 4) * .22f) + z.front * ((k / 4) * .2f - .1f), new Vector3(.16f, .03f, .14f), rot * Quaternion.Euler(-14f, 0, 0)); BoxCol(z.root, dmm + up * .5f, new Vector3(.9f, 1f, .6f), rot);
            string[] fast = { "Tk-tk-tk-tk-tk. A hundred and fifty-five beats a minute. Your hands give up. Your feet don't.", "Power, music, electric revival. The pads light up faster than you can read them.", "You hit one pad and get a whole gospel choir at double speed." }; var fn = 0; Spot("The drum machine", dmm + up * 1.1f, "Hit the pads", new Vector3(.9f, .8f, .8f)).onUse = _ => { life.played = Time.time; Say1(dmm + up * 2.2f, fast[fn++ % fast.Length]); };
        }

        // ---------------------------------------------------------------- JAŸ-Z
        void JayZ(Plot z)
        {
            var up = Vector3.up; var rot = z.Rot; var rng = new System.Random(9); var es = Song(z, "Empire State"); var np = Song(z, "99 Problems"); life.empireSong = es; var c = z.At(-3f, 0);
            // a model skyline on a long table of concrete: towers with every window waiting to light, one tall one with a spire
            z.mk.Box(concrete, c + up * .3f, new Vector3(8f, .6f, 3f), rot); z.solid.Box(concrete, c + up * .3f, new Vector3(8f, .6f, 3f), rot); var win = SongGlow(es, new Color(1f, .85f, .5f), .5f);
            for (var k = 0; k < 13; k++) { var h = 1.2f + (k * 37 % 11) * .34f + (k == 6 ? 3.2f : 0f); var w = k == 6 ? .9f : .5f + (k % 3) * .14f; var p = c + up * .6f + z.side * (-3.6f + k * .6f) + z.front * ((k % 3) * .7f - .7f); z.mk.Box(Paint(new Color(.3f, .32f, .38f) * (.8f + (k % 4) * .1f)), p + up * h / 2f, new Vector3(w, h, w), rot); for (var r = 0; r < Mathf.FloorToInt(h / .3f); r++) z.mk.Box(win, p + up * (.2f + r * .3f) + z.front * (w / 2f + .01f), new Vector3(w * .7f, .12f, .01f), rot); if (k == 6) { z.mk.Box(Paint(new Color(.4f, .42f, .48f)), p + up * (h + .4f), new Vector3(.5f, .8f, .5f), rot); z.mk.Prism(chain, p + up * (h + .8f), up, .04f, 1.4f, 5, .2f); z.mk.Blob(SongGlow(es, Red, 2f), p + up * (h + 2.2f), Vector3.one * .06f, rng, 0f); } }
            z.area.glowLight = ArtistBits.PointLight(z.root, c + up * 3f + z.front * 3f, new Color(1f, .9f, .7f), 22f, 1.4f);
            var cab = new MeshKit(); cab.Box(Paint(Yellow), new Vector3(0, .1f, 0), new Vector3(.2f, .1f, .46f), Quaternion.identity); cab.Box(Paint(Yellow), new Vector3(0, .19f, -.02f), new Vector3(.18f, .08f, .22f), Quaternion.identity); cab.Box(dark, new Vector3(0, .19f, -.02f), new Vector3(.19f, .05f, .18f), Quaternion.identity); cab.Box(white, new Vector3(0, .25f, -.02f), new Vector3(.08f, .03f, .05f), Quaternion.identity); foreach (var e in new[] { -.1f, .1f }) foreach (var q in new[] { -.15f, .15f }) cab.Prism(dark, new Vector3(e, .05f, q), Vector3.right * Mathf.Sign(e), .05f, .03f, 8);
            Thing(cab, "Toy cab", "a little yellow cab", c + up * .62f + z.front * 1.2f + z.side * 1f, rot * Quaternion.Euler(0, 70f, 0), new Vector3(.22f, .28f, .48f), .2f);
            ArtistBits.Label(z.labels, "CONCRETE JUNGLE\nwhere dreams are made of", c + up * .34f + z.front * 1.52f, z.front, .14f, Ink, TextAnchor.MiddleCenter);
            // "99 Problems": a wall of tally marks, ninety-nine of them
            var tw = z.At(3f, 6f); var tr = rot * Quaternion.Euler(0, -30f, 0); var tf = tr * Vector3.forward; z.mk.Box(Paint(Browns[3]), tw + up * 1.5f, new Vector3(4.6f, 3f, .3f), tr); z.solid.Box(concrete, tw + up * 1.5f, new Vector3(4.6f, 3f, .3f), tr); var chalk = SongGlow(np, Cream, .5f);
            for (var k = 0; k < 99; k++) { var g = k / 5; var j = k % 5; var gx = -1.9f + (g % 7) * .62f; var gy = 2.6f - (g / 7) * .9f; if (j < 4) z.mk.Box(chalk, tw + up * gy + tr * new Vector3(gx + j * .09f, 0, .16f), new Vector3(.025f, .5f, .01f), tr); else z.mk.Box(chalk, tw + up * gy + tr * new Vector3(gx + .13f, 0, .165f), new Vector3(.5f, .025f, .01f), tr * Quaternion.Euler(0, 0, 50f)); }
            string[] count = { "Ninety-seven, ninety-eight, ninety-nine. You count again. Still ninety-nine. There is no hundredth mark.", "Somebody's chalked a small arrow under the last one. It doesn't point at anything.", "You could add one. It doesn't feel right. Ninety-nine's the number." }; var cn = 0; Spot("The tally wall", tw + up * 1.5f + tf * .7f, "Count the marks", new Vector3(3f, 2.4f, 1f)).onUse = _ => Say1(tw + up * 3.6f, count[cn++ % count.Length]);
        }

        // ---------------------------------------------------------------- Kurtis Blow: Basketball
        void KurtisBlow(Plot z)
        {
            var up = Vector3.up; var rot = z.Rot; var rng = new System.Random(84); var s = Song(z, "Basketball"); var c = z.c + up * .1f;
            // a half court: painted lines, a hoop at each end, a chain-link fence along the back, balls to throw
            var line = SongGlow(s, new Color(1f, 1f, .95f), .3f); foreach (var e in new[] { -1f, 1f }) { z.mk.Box(line, c + z.side * e * 6.4f, new Vector3(.1f, .01f, 10f), rot); z.mk.Box(line, c + z.front * e * 5f, new Vector3(12.9f, .01f, .1f), rot); for (var k = 0; k < 14; k++) { var a = (k / 13f * 180f - 90f) * Mathf.Deg2Rad; z.mk.Box(line, c + z.side * e * (6.4f - Mathf.Cos(a) * 2.6f) + z.front * Mathf.Sin(a) * 2.6f, new Vector3(.1f, .01f, .66f), rot * Quaternion.Euler(0, -a * Mathf.Rad2Deg * e, 0)); } Hoop(z.ctx, z.mk, z.At(0, e * 6.9f), -z.side * e); }
            z.mk.Box(line, c, new Vector3(.1f, .01f, 10f), rot); for (var k = 0; k < 16; k++) { var a = k * 22.5f * Mathf.Deg2Rad; z.mk.Box(line, c + z.side * Mathf.Cos(a) * 1.4f + z.front * Mathf.Sin(a) * 1.4f, new Vector3(.1f, .01f, .58f), rot * Quaternion.Euler(0, -k * 22.5f, 0)); }
            Fence(z.ctx, z.mk, z.At(-5.6f, -6.6f), z.At(-5.6f, 6.6f));
            for (var k = 0; k < 3; k++) { var bm = new MeshKit(); bm.Blob(Paint(new Color(.9f, .45f, .15f)), new Vector3(0, .13f, 0), Vector3.one * .13f, rng, 0f); bm.Prism(dark, new Vector3(0, .13f, -.131f), Vector3.forward, .005f, .262f, 3); bm.Prism(dark, new Vector3(-.131f, .13f, 0), Vector3.right, .005f, .262f, 3); Thing(bm, "Basketball", "a basketball", c + z.side * (-1f + k * 1f) + z.front * (3f - k * .4f), Quaternion.identity, new Vector3(.27f, .27f, .27f), .5f, false); }
            ArtistBits.Label(z.labels, "THEY'RE PLAYING\nBASKETBALL", z.At(-5.4f, 0) + up * 3.2f, z.front, .34f, Gold, TextAnchor.MiddleCenter); z.mk.Box(dark, z.At(-5.5f, 0) + up * 3.2f, new Vector3(4.8f, 1.2f, .06f), rot);
            Crate(z.mk, z.At(4.6f, -4f), -z.front, 1); Crate(z.mk, z.At(4.6f, -2.8f), -z.front, 4); Folk(z.At(2f, 2f), (-z.front - z.side).normalized, Resident.Pose.Wave, new[] { "Next! Who's got next? You got next.", "We love that basketball. He said it first. We just play it.", "First to eleven, win by two, call your own fouls, argue about all of them." });
            z.area.glowLight = ArtistBits.PointLight(z.root, c + up * 5f, new Color(1f, .95f, .85f), 22f, 1.2f);
        }

        // ---------------------------------------------------------------- everyone else
        void Others(Plot z)
        {
            var up = Vector3.up; var rot = z.Rot; var p = z.At(2.8f, 3.4f); var rng = new System.Random(z.a.name.Length * 3); string s;
            switch (z.a.name)
            {
                case "A Tribe Called Quest":
                    s = Song(z, "Can I Kick It?"); var cn = new MeshKit(); cn.Prism(SongGlow(s, Red, .5f), Vector3.zero, Vector3.up, .06f, .16f, 8); cn.Prism(Paint(Green), new Vector3(0, .05f, 0), Vector3.up, .062f, .05f, 8); cn.Prism(chain, new Vector3(0, .16f, 0), Vector3.up, .055f, .01f, 8); Thing(cn, "Can", "a can (you can kick it)", p + up * .12f, Quaternion.identity, new Vector3(.14f, .18f, .14f), .1f, false);
                    foreach (var e in new[] { -1.2f, 1.2f }) { z.mk.Prism(chain, p - z.front * 3f + z.side * e - up * .2f, up, .05f, 3f, 5); ArtistBits.PostCollider(z.root, p - z.front * 3f + z.side * e, 2.8f, .07f); } z.mk.Box(dark, p - z.front * 3f + up * 2.3f, new Vector3(2.8f, 1.2f, .08f), rot); for (var k = 0; k < 3; k++) z.mk.Box(Paint(new[] { Red, Ink, Green }[k]), p - z.front * 2.95f + up * (2.66f - k * .36f), new Vector3(2.6f, .3f, .02f), rot); ArtistBits.Label(z.labels, "CAN I KICK IT?", p - z.front * 2.9f + up * 2.3f, z.front, .26f, Cream, TextAnchor.MiddleCenter);
                    string[] yes = { "\"Yes you can!\" says the whole block, as one.", "\"Well, I'm gone!\" says somebody, and stays exactly where they are.", "Yes you can. You always could." }; var yn = 0; Spot("Can I kick it?", p + up * .9f, "Ask: can I kick it?", new Vector3(1.4f, 1.6f, 1.4f)).onUse = _ => { life.played = Time.time; Say1(p + up * 2.2f, yes[yn++ % yes.Length]); }; break;
                case "Fugees":
                    s = Song(z, "Killing Me Softly"); z.mk.Prism(iron, p, up, .14f, .03f, 6); var gb = Paint(new Color(.6f, .42f, .24f)); z.mk.Blob(gb, p + up * .3f, new Vector3(.2f, .26f, .06f), rng, .04f); z.mk.Box(woodM, p + up * .88f - z.front * .1f, new Vector3(.05f, .72f, .03f), rot * Quaternion.Euler(-9f, 0, 0)); for (var k = 0; k < 6; k++) z.mk.Box(SongGlow(s, new Color(1f, .9f, .7f), .9f), p + up * .7f + z.side * (-.04f + k * .016f) + z.front * .04f, new Vector3(.004f, .9f, .004f), rot * Quaternion.Euler(-9f, 0, 0));
                    var mic = p + z.side * 1.2f; z.mk.Prism(iron, mic, up, .16f, .03f, 8); z.mk.Prism(iron, mic, up, .016f, 1.5f, 4); z.mk.Blob(chain, mic + up * 1.56f, new Vector3(.06f, .09f, .06f), rng, 0f); ArtistBits.PostCollider(z.root, mic, 1.5f, .06f); var lt = new MeshKit(); lt.Box(white, new Vector3(0, .008f, 0), new Vector3(.22f, .016f, .15f), Quaternion.identity); lt.Tri(Paint(Cream), new Vector3(-.11f, .018f, .075f), new Vector3(.11f, .018f, .075f), new Vector3(0, .018f, -.01f), Vector3.down); Thing(lt, "Letter", "a letter, read out loud to everyone", p - z.side * 1f + up * .12f, rot * Quaternion.Euler(0, 20f, 0), new Vector3(.22f, .05f, .15f), .05f);
                    Crate(z.mk, p + z.front * 1.4f, -z.front, 2); Crate(z.mk, p + z.front * 1.5f + z.side * 1f, -z.front, 5); ArtistBits.Label(z.labels, "one time · two times", p + up * 2f, z.front, .14f, Gold, TextAnchor.MiddleCenter); break;
                case "Grandmaster Flash":
                    s = Song(z, "The Message"); life.messageSong = s; var dk = Decks(z.root, z.mk, p, rot, SongGlow(s, Gold, 1.6f)); string[] flash = { "You cut the break back in on the one. That's the whole invention, right there, under your fingers.", "Clock theory: you mark the record with a crayon and it tells you the time.", "Quick mix. The beat never drops. It's like a jungle sometimes." }; var fn = 0; Spot("Flash's decks", dk + up * .2f, "Cut the break", new Vector3(2.2f, .8f, 1f)).onUse = _ => { life.played = Time.time; Say1(dk + up * 1.6f, flash[fn++ % flash.Length]); };
                    var wl = z.At(1f, -4.4f); var wr = rot * Quaternion.Euler(0, 24f, 0); z.mk.Box(Paint(Browns[1]), wl + up * 1.6f, new Vector3(5f, 3.2f, .3f), wr); z.solid.Box(concrete, wl + up * 1.6f, new Vector3(5f, 3.2f, .3f), wr); ArtistBits.Label(z.labels, "DON'T\nPUSH ME", wl + up * 1.9f + wr * Vector3.forward * .17f, wr * Vector3.forward, .5f, Cream, TextAnchor.MiddleCenter); ArtistBits.Label(z.labels, "'cause I'm close to the edge", wl + up * .8f + wr * Vector3.forward * .17f, wr * Vector3.forward, .13f, Gold, TextAnchor.MiddleCenter);
                    for (var k = 0; k < 14; k++) z.mk.Blob(glassM, z.At(4.6f, -1f + (k % 5) * .5f) + up * .11f + z.front * ((k * 7 % 5) * .2f), new Vector3(.06f + (k % 3) * .03f, .01f, .05f), rng, .5f); break;
                case "Beastie Boys":
                    s = Song(z, "Sabotage"); life.sabotageSong = s; var car = z.At(2.6f, -1f) + up * .1f; var cr = rot * Quaternion.Euler(0, 60f, 0); var cf = cr * Vector3.forward; var cs = cr * Vector3.right; var tan = Paint(new Color(.66f, .56f, .4f)); z.mk.Box(tan, car + up * .7f, new Vector3(1.9f, .6f, 5f), cr); z.mk.Box(tan, car + up * 1.24f - cf * .2f, new Vector3(1.7f, .56f, 2.4f), cr); z.mk.Box(glassM, car + up * 1.26f - cf * .2f, new Vector3(1.74f, .4f, 2.1f), cr); z.mk.Box(dark, car + up * .7f, new Vector3(1.92f, .12f, 5.02f), cr);
                    foreach (var e in new[] { -1f, 1f }) foreach (var q in new[] { -1.6f, 1.6f }) z.mk.Prism(dark, car + up * .36f + cs * e * .86f + cf * q, cs * e, .36f, .2f, 10); z.mk.Box(chain, car + up * 1.58f - cf * .2f, new Vector3(1f, .08f, .2f), cr); z.mk.Blob(redM, car + up * 1.7f - cf * .2f - cs * .3f, Vector3.one * .13f, rng, 0f); z.mk.Blob(blueM, car + up * 1.7f - cf * .2f + cs * .3f, Vector3.one * .13f, rng, 0f); halos.Add(car + up * 1.8f, 1.2f, Red, .3f); BoxCol(z.root, car + up * .8f, new Vector3(1.9f, 1.6f, 5f), cr); Interact.Seat(tlife.transform, car + up * 1f + cf * 2.2f, cf, "Sit on the hood").seatHeight = .9f;
                    var st = z.At(5f, 3.6f); z.mk.Prism(iron, st, up, .16f, .03f, 8); z.mk.Prism(iron, st, up, .02f, 1.5f, 4); z.mk.Blob(Paint(new Color(.85f, .7f, .56f)), st + up * 1.6f, Vector3.one * .14f, rng, 0f); z.mk.Box(dark, st + up * 1.64f + z.front * .13f, new Vector3(.26f, .08f, .02f), rot); z.mk.Box(Paint(new Color(.3f, .2f, .12f)), st + up * 1.54f + z.front * .13f, new Vector3(.18f, .04f, .03f), rot); z.mk.Blob(Paint(new Color(.3f, .2f, .12f)), st + up * 1.76f, new Vector3(.17f, .08f, .17f), rng, .1f); ArtistBits.PostCollider(z.root, st, 1.7f, .16f); ArtistBits.Label(z.labels, "I can't stand it", st + up * 2.1f, z.front, .12f, Cream, TextAnchor.MiddleCenter); break;
                case "AZ":
                    s = Song(z, "Mo Money"); z.mk.Box(concrete, p + up * .4f, new Vector3(1f, .8f, 1f), rot); BoxCol(z.root, p + up * .4f, new Vector3(1f, .8f, 1f), rot); for (var x = 0; x < 8; x++) for (var y = 0; y < 8; y++) if ((x + y) % 2 == 0) z.mk.Box(dark, p + up * .805f + z.side * (-.35f + x * .1f) + z.front * (-.35f + y * .1f), new Vector3(.1f, .01f, .1f), rot); for (var k = 0; k < 8; k++) z.mk.Prism(k % 2 == 0 ? white : SongGlow(s, Gold, .8f), p + up * .81f + z.side * (-.35f + (k * 3 % 8) * .1f) + z.front * (-.35f + (k * 5 % 8) * .1f), up, .03f, .1f, 6, .5f);
                    Crate(z.mk, p + z.front * 1f, -z.front, 0); Crate(z.mk, p - z.front * 1f, z.front, 3); var bc = z.At(5f, 1f); z.mk.Box(dark, bc + up * .2f, new Vector3(.7f, .16f, .46f), rot); z.mk.Box(dark, bc + up * .5f - z.front * .22f, new Vector3(.7f, .46f, .06f), rot * Quaternion.Euler(-16f, 0, 0)); for (var k = 0; k < 6; k++) z.mk.Box(SongGlow(s, new Color(.4f, .9f, .45f), 1f), bc + up * .3f + z.side * (-.24f + (k % 3) * .24f) + z.front * ((k / 3) * .18f - .08f), new Vector3(.2f, .04f, .12f), rot); break;
                case "Cannibal Ox":
                    s = Song(z, "Iron Galaxy"); var rust = Paint(new Color(.42f, .3f, .24f)); z.mk.Prism(iron, p - up * .2f, up, .08f, 3.4f, 6); ArtistBits.PostCollider(z.root, p, 3.2f, .1f); for (var k = 0; k < 9; k++) { var a = k * 40f; var r = .8f + (k % 3) * .6f; var d = Quaternion.Euler((k % 3) * 22f - 22f, a, 0) * Vector3.forward; z.mk.Prism(iron, p + up * 3f, d, .015f, r, 3); z.mk.Blob(k % 4 == 0 ? SongGlow(s, new Color(.8f, .9f, 1f), 1.6f) : rust, p + up * 3f + d * r, Vector3.one * (.1f + (k % 3) * .06f), rng, .2f); } halos.Add(p + up * 3f, 2f, Sky, .25f);
                    for (var k = 0; k < 4; k++) { var pg = z.At(4.4f + (k % 2) * .5f, -1f + k * .6f) + up * .1f; z.mk.Blob(rust, pg + up * .1f, new Vector3(.09f, .08f, .15f), rng, .1f); z.mk.Blob(rust, pg + up * .2f + z.front * .1f, Vector3.one * .05f, rng, 0f); z.mk.Cone(rust, pg + up * .2f + z.front * .16f, .015f, .04f, 3); } break;
                case "Diddy":
                    s = Song(z, "I'll Be Missing You"); var cg = SongGlow(s, new Color(1f, .8f, .45f), 2f); for (var k = 0; k < 9; k++) { var cp = p + z.side * (-1f + (k % 5) * .5f) + z.front * ((k / 5) * .5f - .2f) + up * .1f; var h = .14f + (k * 7 % 4) * .06f; z.mk.Prism(white, cp, up, .04f, h, 6); z.mk.Blob(cg, cp + up * (h + .05f), new Vector3(.025f, .05f, .025f), rng, 0f); } halos.Add(p + up * .4f, 1.6f, Gold, .4f);
                    for (var k = 0; k < 5; k++) { z.mk.Prism(leafM, p - z.front * .5f + z.side * (-.8f + k * .4f) + up * .1f, (up + z.front * .2f).normalized, .01f, .4f, 3); z.mk.Blob(white, p - z.front * .42f + z.side * (-.8f + k * .4f) + up * .5f, Vector3.one * .06f, rng, .2f); } ArtistBits.Label(z.labels, "every step I take", p + up * 1.2f - z.front * .6f, z.front, .14f, Cream, TextAnchor.MiddleCenter); break;
                case "RZA":
                    s = Song(z, "Quentin Tarantino"); var pj = p; z.mk.Prism(iron, pj, up, .03f, 1.3f, 4); for (var k = 0; k < 3; k++) z.mk.Prism(iron, pj, (Quaternion.Euler(0, k * 120f, 0) * Vector3.forward * .4f + up * .2f).normalized, .02f, .5f, 3); z.mk.Box(dark, pj + up * 1.4f, new Vector3(.3f, .3f, .5f), rot * Quaternion.Euler(0, 180f, 0)); foreach (var e in new[] { -.2f, .2f }) z.mk.Prism(dark, pj + up * 1.7f + z.front * e - z.side * .03f, z.side, .16f, .06f, 10); z.mk.Blob(SongGlow(s, Yellow, 2f), pj + up * 1.4f - z.front * .27f, Vector3.one * .05f, rng, 0f); ArtistBits.PostCollider(z.root, pj, 1.4f, .2f);
                    var sc = p - z.front * 3.4f; foreach (var e in new[] { -1.3f, 1.3f }) z.mk.Prism(chain, sc + z.side * e - up * .2f, up, .04f, 3f, 4); z.mk.Box(SongGlow(s, new Color(1f, .9f, .3f), .5f), sc + up * 2f, new Vector3(2.6f, 1.6f, .04f), rot); for (var k = 0; k < 2; k++) z.mk.Box(dark, sc + up * 2f + z.front * .03f + z.side * (k == 0 ? -1.1f : 1.1f), new Vector3(.14f, 1.6f, .01f), rot); ArtistBits.Label(z.labels, "A FILM IN\nTHIRTY-SIX CHAMBERS", sc + up * 2f + z.front * .04f, z.front, .16f, Ink, TextAnchor.MiddleCenter);
                    var cb = new MeshKit(); cb.Box(dark, new Vector3(0, .1f, 0), new Vector3(.34f, .2f, .02f), Quaternion.identity); cb.Box(white, new Vector3(0, .22f, 0), new Vector3(.34f, .04f, .02f), Quaternion.Euler(0, 0, 12f)); for (var k = 0; k < 4; k++) cb.Box(dark, new Vector3(-.12f + k * .08f, .22f, .011f), new Vector3(.03f, .04f, .001f), Quaternion.Euler(0, 0, 40f)); Thing(cb, "Clapperboard", "a clapperboard", z.At(4.8f, 1.6f) + up * .12f, rot, new Vector3(.34f, .26f, .1f), .2f); break;
                case "Shabazz Palaces":
                    s = Song(z, "Swerve"); var blk = Paint(new Color(.06f, .06f, .08f)); foreach (var e in new[] { -1f, 1f }) { z.mk.Box(blk, p + z.side * e * 1.2f + up * 1.6f, new Vector3(.5f, 3.2f, .5f), rot); BoxCol(z.root, p + z.side * e * 1.2f + up * 1.6f, new Vector3(.5f, 3.2f, .5f), rot); } for (var k = 0; k < 9; k++) { var a = k / 8f * Mathf.PI; z.mk.Box(blk, p + up * (3.2f + Mathf.Sin(a) * 1.2f) - z.side * Mathf.Cos(a) * 1.2f, new Vector3(.6f, .5f, .5f), rot * Quaternion.Euler(0, 0, -a * Mathf.Rad2Deg + 90f)); }
                    var sg = SongGlow(s, Gold, 2f); for (var k = 0; k < 12; k++) z.mk.Blob(sg, p + up * (1f + (k * 5 % 7) * .5f) + z.side * (-.8f + (k * 3 % 5) * .4f) + z.front * .1f, Vector3.one * (.03f + (k % 3) * .015f), rng, 0f); halos.Add(p + up * 2.4f, 2f, Gold, .3f); break;
                case "T La Rock":
                    s = Song(z, "It's Yours"); z.mk.Box(SongGlow(s, Red, .4f), p + up * .45f, new Vector3(.9f, .7f, .9f), rot); z.mk.Box(Paint(Yellow), p + up * .45f, new Vector3(.92f, .72f, .16f), rot); z.mk.Box(Paint(Yellow), p + up * .45f, new Vector3(.16f, .72f, .92f), rot); foreach (var e in new[] { -1f, 1f }) z.mk.Blob(Paint(Yellow), p + up * .9f + z.side * e * .16f, new Vector3(.16f, .1f, .08f), rng, .1f); BoxCol(z.root, p + up * .45f, new Vector3(.9f, .8f, .9f), rot); ArtistBits.Label(z.labels, "it's yours", p + up * 1.4f, z.front, .2f, Gold, TextAnchor.MiddleCenter);
                    string[] yours = { "You lift the lid. Inside: a drum machine and a microphone. It's yours.", "The tag says: to whoever's reading this. That's you.", "There's nothing in it but a beat. That's plenty." }; var on = 0; Spot("The gift", p + up * .9f, "Open it", new Vector3(1f, 1.2f, 1f)).onUse = _ => Say1(p + up * 2.2f, yours[on++ % yours.Length]); break;
                case "Ceren Yaldız":
                    s = Song(z, "Ara Ara"); z.mk.Prism(chain, p - up * .2f, up, .06f, 2.2f, 6); z.mk.Box(Paint(Sky * .8f), p + up * 1.7f, new Vector3(.6f, 1.1f, .5f), rot); z.mk.Box(chain, p + up * 1.6f + z.front * .26f, new Vector3(.36f, .5f, .04f), rot); z.mk.Box(dark, p + up * 1.86f + z.front * .3f - z.side * .2f, new Vector3(.08f, .3f, .08f), rot); z.mk.Blob(SongGlow(s, Gold, 1.8f), p + up * 2.1f + z.front * .26f, Vector3.one * .03f, rng, 0f); ArtistBits.PostCollider(z.root, p, 2.2f, .3f); ArtistBits.Label(z.labels, "ara ara", p + up * 2.4f, z.front, .16f, Gold, TextAnchor.MiddleCenter);
                    string[] call = { "It rings and rings. \"Ara, ara,\" the song says: call, call. Nobody picks up. You call again.", "A voice answers in a language you half know and hangs up laughing.", "The line clicks. Somebody on the other end is playing the same song." }; var an = 0; Spot("The payphone", p + up * 1.6f + z.front * .4f, "Make a call", new Vector3(.8f, 1.4f, .8f)).onUse = _ => Say1(p + up * 2.9f, call[an++ % call.Length]); break;
                default:
                    z.mk.Prism(concrete, p, up, .2f, .5f, 8); z.mk.Prism(GlowM(Gold, 1.2f), p + up * .5f, up, .07f, .22f, 8); break;
            }
        }
    }
}
