using System.Collections.Generic;
using UnityEngine;

namespace EverythingWorld.World
{
    /// The Fusion Isthmus' artists. Most have a doorstep of their own: a slice of brick house front with a lit window,
    /// a step to sit on, pots and a chair, and something from their song beside it; covers on a brick-framed board
    /// under a brass lamp, a plaque, a little brass lantern for every song without its own piece.
    /// - Tom Misch (the largest): a bedroom studio opened onto the street: a guitar to play, a violin, a desk with a
    ///   beat pad, a rug and an armchair; a small mirror ball over a YES in lights ("Disco Yes"); a bicycle with its
    ///   lamps on ("Nightrider": the town dims and the lamps glow); a fire escape up to a painted door ("Beautiful
    ///   Escape"); a wave of teal light ("Tidal Wave": the canal brightens and ripples); a brass 100 ("Last 100"); a
    ///   question mark of bulbs ("What Kinda Music").
    /// - Yussef Dayes: a drum kit on a round rug under a canopy of lights, to play; bird-of-paradise flowers and paper
    ///   birds ("Birds of Paradise"); a spiral of turquoise lights in the ground ("Turquoise Galaxy": the sky turns
    ///   turquoise); a rusted iron ring ("Rust"). Yussef Kamaal: an electric piano; a fan of taut glowing strings
    ///   ("Strings of Light": every string of bulbs in the town blazes); a door numbered 17 ("Joint 17").
    /// - Ezra Collective: a street party: bunting, three horns on stands, a ribbon arch ("Victory Dance": confetti);
    ///   a heart in lights ("Only Love"); a tidy chest of labelled drawers ("Well Organised").
    /// - The older fusion players, north of the canal: CASIOPEA a line of giant dominoes to topple ("Domino Line");
    ///   Pat Metheny Group a tree with glowing roots and a many-necked guitar ("The Roots of Coincidence"); Allan
    ///   Holdsworth a staircase of notes climbing fast, a little devil on a weathervane ("Devil Take the Hindmost");
    ///   Victor Wooten a bass and a jet's tail fin ("Dc10"); Medeski, Martin & Wood a house of bubbles ("Bubblehouse").
    /// - The rest: a confession booth, a dog by a barbecue, a lamplighter's ladder, a great moon, a sitar on a rug, a
    ///   comet on a pole, a box of lemons, a tiny island in a puddle, a bottle of medicine with a heart.
    public partial class FusionKit
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
                Add("Tom Misch", "Tidal Wave", "@the canal and the piano bridge"); Add("Tom Misch", "Nightrider", "Tom Misch"); Add("Yussef Dayes", "Turquoise Galaxy", "Yussef Dayes");
                Add("Ezra Collective", "Victory Dance", "Ezra Collective"); Add("Yussef Kamaal", "Strings of Light", "@along the canal");
                return list.ToArray();
            }
        }

        string Song(Plot z, string title)
        {
            if (!SongIds.TryGetValue(z.a.name, out var list)) return null;
            foreach (var (t, id) in list) if (t.StartsWith(title)) { z.lit.Add(id); return id; }
            return null;
        }

        Material SongGlow(string song, Color c, float s = 1.6f)
        {
            if (song == null) return GlowM(c, s);
            var m = new Material(GlowM(c, s)) { name = "Song glow" }; life.songGlows.Add((song, m, c * (s * .65f))); return m;
        }

        Material SongGlow(Plot z, string title, Color c, float s = 1.6f) => SongGlow(Song(z, title), c, s);

        public override void BuildArtist(LandContext ctx, IslandWorldData.Artist a)
        {
            if (brick == null) MakeMaterials();
            var root = new GameObject("Artist: " + a.name).transform; root.SetParent(ctx.Root, false);
            var front = facing.TryGetValue(a.name, out var f) ? f : new Vector3(center.x - a.x, 0, center.z - a.z).normalized;
            if (front.sqrMagnitude < .01f) front = Vector3.forward;
            var labels = new GameObject("Labels").transform; labels.SetParent(root, false);
            var area = root.gameObject.AddComponent<ArtistArea>();
            area.artistName = a.name; area.stationId = a.stationId; area.radius = a.r; area.glowColor = Brass;
            var z = new Plot { ctx = ctx, root = root, labels = labels, mk = new MeshKit(), solid = new MeshKit(), pictures = new MeshKit(), a = a, c = ctx.OnGround(a.x, a.z), front = front, side = Vector3.Cross(Vector3.up, front), area = area };
            var reach = Foot(a); var up = Vector3.up;
            z.mk.Prism(paving, z.c - up * .25f, up, reach * .62f, .33f, 18);
            switch (a.name)
            {
                case "Tom Misch": TomMisch(z); break;
                case "Yussef Dayes": YussefDayes(z); break;
                case "Yussef Kamaal": YussefKamaal(z); break;
                case "Ezra Collective": EzraCollective(z); break;
                case "CASIOPEA": case "Pat Metheny Group": case "Allan Holdsworth": case "Victor Wooten": case "Medeski, Martin & Wood": Elders(z); break;
                default: Doorstep(z); Others(z); break;
            }
            SongLamps(z);
            Board(z, z.At(reach * .6f, -reach * .45f - 1.5f), front);
            ArtistBits.Plaque(z.mk, z.solid, labels, a, z.At(reach * .6f + 1.5f, reach * .45f + 1.5f), front, brickDark, brassM, Cream, 1.6f);
            BrassLamp(ctx, z.mk, z.At(reach * .5f, reach * .62f), -z.side, true);
            if (z.area.glowLight == null) z.area.glowLight = ArtistBits.PointLight(root, z.c + up * 5f + front * 3f, Brass, HasStation(a) ? 26f : 18f, 2.4f);
            area.glowRenderers = new Renderer[0];
            z.mk.Build("Area", root, false);
            z.solid.BuildCollider("Walls", root);
            z.pictures.Build("Covers", root, false);
            noGrass.Add(new Vector3(z.c.x, reach * .9f, z.c.z));
            if (HasStation(a) || reach >= 10f)
            {
                var mark = new GameObject("Shot: " + a.name + " up close").transform; mark.SetParent(ctx.Root, false);
                var cam = z.At(reach * .85f, reach * .3f) + up * 2f; var look = z.At(-reach * .3f, 0) + up * 2.2f - cam; mark.SetPositionAndRotation(cam, Quaternion.LookRotation(look.normalized));
            }
        }

        /// The covers on a cream board in a brick frame, a small brass lamp over it.
        void Board(Plot z, Vector3 ground, Vector3 dir)
        {
            dir = Vector3.ProjectOnPlane(dir, Vector3.up).normalized; var rot = Quaternion.LookRotation(dir); var side = Vector3.Cross(Vector3.up, dir);
            var n = Mathf.Max(1, Mathf.Min(7, z.a.songList?.Length ?? 1)); const float size = .9f; var w = n * size * 1.25f + .6f;
            var c = ground + Vector3.up * 1.7f;
            z.mk.Box(brick, c - Vector3.up * .5f - dir * .06f, new Vector3(w + .5f, size + 2f, .2f), rot); z.solid.Box(brick, c - Vector3.up * .5f, new Vector3(w + .5f, size + 2f, .3f), rot); z.mk.Box(WorldMaterials.Solid(Cream), c - Vector3.up * .1f + dir * .05f, new Vector3(w, size + .9f, .04f), rot); z.mk.Box(stoneM, c + Vector3.up * (size / 2f + .56f) - dir * .06f, new Vector3(w + .7f, .14f, .3f), rot);
            z.mk.Prism(brassM, c + Vector3.up * (size / 2f + .6f) + dir * .05f, (dir + Vector3.up * .3f).normalized, .03f, .5f, 4); z.mk.Box(lampM, c + Vector3.up * (size / 2f + .72f) + dir * .5f, new Vector3(w * .5f, .08f, .12f), rot);
            ArtistBits.PointLight(z.root, c + dir * 1.6f + Vector3.up * .8f, new Color(1f, .9f, .75f), 5f, 1.4f);
            ArtistBits.CoverWall(z.pictures, z.labels, z.ctx, z.a, c + dir * .08f + Vector3.up * .08f, dir, n, size, n, true, Ink);
        }

        /// A little brass lantern on a post for each song without its own piece.
        void SongLamps(Plot z)
        {
            if (!SongIds.TryGetValue(z.a.name, out var list)) return;
            var up = Vector3.up; var k = 0; var seen = new HashSet<string>();
            foreach (var (_, id) in list)
            {
                if (z.lit.Contains(id) || !seen.Add(id)) continue;
                var p = z.At(Foot(z.a) * .55f - (k % 2) * 1.3f, Foot(z.a) * .2f + (k / 2) * 1.3f);
                z.mk.Prism(iron, p - up * .1f, up, .04f, 1.3f, 5); z.mk.Box(SongGlow(id, Brass, 2f), p + up * 1.42f, new Vector3(.2f, .28f, .2f), z.Rot); z.mk.Cone(brassM, p + up * 1.56f, .18f, .14f, 4, 45f); halos.Add(p + up * 1.42f, 1f, Brass, .5f); ArtistBits.PostCollider(z.root, p, 1.3f, .08f);
                k++;
            }
        }

        /// A slice of house front: brick, a door, a lit window with a flower box, a step to sit on, pots, a chair.
        void Doorstep(Plot z)
        {
            var up = Vector3.up; var g = z.At(-Foot(z.a) * .34f, 0); var rot = z.Rot; var hue = z.a.name.Length + z.a.name[0]; var paint = WorldMaterials.Solid(Paints[hue % Paints.Length]); var rng = new System.Random(hue);
            z.mk.Box(paint, g + up * 1.7f, new Vector3(5f, 3.6f, .4f), rot); z.solid.Box(paint, g + up * 1.7f, new Vector3(5f, 3.6f, .4f), rot); z.mk.Box(stoneM, g + up * 3.56f + z.front * .05f, new Vector3(5.2f, .2f, .6f), rot);
            z.mk.Box(WorldMaterials.Solid(Paints[(hue + 3) % Paints.Length] * .7f), g + up * 1.1f + z.front * .22f - z.side * 1.3f, new Vector3(1f, 2.2f, .06f), rot); z.mk.Box(windowM, g + up * 1.8f + z.front * .22f + z.side * 1f, new Vector3(1.5f, 1.4f, .06f), rot); z.mk.Box(white, g + up * 1.8f + z.front * .24f + z.side * 1f, new Vector3(.07f, 1.4f, .06f), rot); halos.Add(g + up * 1.8f + z.front * .9f + z.side * 1f, 1.6f, Brass, .35f);
            z.mk.Box(brickDark, g + up * .98f + z.front * .36f + z.side * 1f, new Vector3(1.6f, .22f, .3f), rot); for (var q = 0; q < 3; q++) z.mk.Blob(q == 1 ? leafM : WorldMaterials.Glow(Bloom[(hue + q) % Bloom.Length], .35f), g + up * 1.18f + z.front * .38f + z.side * (.55f + q * .45f), Vector3.one * .17f, rng, .3f);
            z.mk.Box(stoneM, g + up * .16f + z.front * .7f - z.side * 1.3f, new Vector3(1.6f, .32f, .9f), rot); z.solid.Box(stoneM, g + up * .16f + z.front * .7f - z.side * 1.3f, new Vector3(1.6f, .32f, .9f), rot); Interact.Seat(tlife.transform, g + up * .36f + z.front * .9f - z.side * 1.3f, z.front, "Sit on the step").seatHeight = .36f;
            Pot(z.mk, g + z.front * .7f - z.side * 2.3f, 1.2f, rng); Pot(z.mk, g + z.front * .6f + z.side * 2.2f, 1f, rng, false); Chair(z.mk, g + z.front * 1.3f + z.side * .9f + up * .08f, z.front, hue);
        }

        void Say1(Vector3 at, string text) => PlaceTools.Say(tlife.transform, at, text, 4.5f);

        // ---------------------------------------------------------------- Tom Misch
        void TomMisch(Plot z)
        {
            var up = Vector3.up; var rot = z.Rot; var c = z.At(-4f, 0); var rng = new System.Random(7);
            // a bedroom studio opened onto the street: a back wall with a window and shelves, a rug, a desk, an armchair
            z.mk.Box(WorldMaterials.Solid(Paints[4]), c + up * 1.8f - z.front * 1.6f, new Vector3(8f, 3.6f, .4f), rot); z.solid.Box(brick, c + up * 1.8f - z.front * 1.6f, new Vector3(8f, 3.6f, .4f), rot); z.mk.Box(woodM, c + up * .08f + z.front * .4f, new Vector3(8f, .16f, 4.4f), rot); z.solid.Box(woodM, c - up * .1f + z.front * .4f, new Vector3(8f, .5f, 4.4f), rot);
            z.mk.Box(WorldMaterials.Solid(new Color(.2f, .46f, .5f)), c + up * .17f + z.front * .6f, new Vector3(4.4f, .03f, 2.8f), rot); z.mk.Box(windowM, c + up * 2.2f - z.front * 1.38f + z.side * 2.4f, new Vector3(1.8f, 1.5f, .06f), rot);
            for (var s = 0; s < 2; s++) { z.mk.Box(woodM, c + up * (1.6f + s * .8f) - z.front * 1.26f - z.side * 2f, new Vector3(2.6f, .06f, .3f), rot); for (var k = 0; k < 8; k++) z.mk.Box(WorldMaterials.Solid(Paints[(k + s) % Paints.Length]), c + up * (1.8f + s * .8f) - z.front * 1.26f - z.side * (3.1f - k * .3f), new Vector3(.04f, .34f, .26f), rot); }
            var gt = c + up * .16f - z.side * .6f + z.front * .4f; var gm = new MeshKit(); gm.Prism(iron, Vector3.zero, Vector3.up, .02f, .35f, 5); gm.Blob(WorldMaterials.Solid(new Color(.9f, .86f, .76f), .6f), new Vector3(0, .7f, 0), new Vector3(.22f, .3f, .06f), rng, .05f); gm.Box(woodM, new Vector3(0, 1.42f, 0), new Vector3(.07f, .95f, .04f), Quaternion.identity); gm.Box(dark, new Vector3(0, 1.98f, 0), new Vector3(.1f, .2f, .04f), Quaternion.identity);
            var ggo = gm.BuildSingle("Tom's guitar", tlife.transform, false); ggo.transform.SetPositionAndRotation(gt, rot); string[] lick = { "A clean little lick with a slide at the end. It sounds like a Sunday.", "Major seven, minor nine, back again. The pots on the windowsill lean in.", "You loop four bars and play over them. Twenty minutes go by without telling you.", "One bent note that smiles on the way up." }; var ln = 0;
            Interact.Toy(ggo, Toy.Kind.Custom, "Play the guitar", new Vector3(.9f, 2.2f, .9f)).onUse = _ => { life.played = Time.time; Say1(gt + up * 2.8f, lick[ln++ % lick.Length]); };
            var vs = c + up * .16f + z.side * .8f - z.front * .6f; z.mk.Prism(iron, vs, up, .02f, .9f, 4); z.mk.Blob(woodM, vs + up * 1.1f, new Vector3(.12f, .2f, .05f), rng, .05f); z.mk.Box(dark, vs + up * 1.42f, new Vector3(.03f, .34f, .03f), rot);
            var ds = c + up * .16f + z.side * 2.6f - z.front * .5f; z.mk.Box(woodM, ds + up * .74f, new Vector3(1.6f, .06f, .7f), rot); foreach (var e in new[] { -.7f, .7f }) z.mk.Box(iron, ds + up * .37f + z.side * e, new Vector3(.04f, .74f, .6f), rot); z.mk.Box(dark, ds + up * .95f - z.front * .2f, new Vector3(.6f, .4f, .03f), rot * Quaternion.Euler(-12f, 0, 0)); z.mk.Box(GlowM(Teal, 1f), ds + up * .95f - z.front * .18f, new Vector3(.54f, .34f, .02f), rot * Quaternion.Euler(-12f, 0, 0)); z.mk.Box(dark, ds + up * .79f + z.front * .12f + z.side * .4f, new Vector3(.3f, .04f, .3f), rot); BoxCol(z.root, ds + up * .5f, new Vector3(1.6f, 1f, .7f), rot);
            var ac = c + up * .16f - z.side * 2.8f + z.front * .9f; var cloth = WorldMaterials.Solid(new Color(.72f, .46f, .3f)); z.mk.Box(cloth, ac + up * .28f, new Vector3(.9f, .5f, .9f), rot); z.mk.Box(cloth, ac + up * .75f - z.front * .36f, new Vector3(.9f, .8f, .22f), rot); foreach (var e in new[] { -.45f, .45f }) z.mk.Box(cloth, ac + up * .55f + z.side * e, new Vector3(.2f, .5f, .9f), rot); BoxCol(z.root, ac + up * .5f, new Vector3(1.1f, 1f, 1f), rot); Interact.Seat(tlife.transform, ac + up * .56f + z.front * .1f, z.front, "Sit in the armchair");
            Pot(z.mk, c + up * .16f - z.side * 3.6f - z.front * .9f, 1.6f, rng); Pot(z.mk, c + up * .16f + z.side * 3.7f + z.front * 1.6f, 1.3f, rng, false);
            z.area.glowLight = ArtistBits.PointLight(z.root, c + up * 3.4f + z.front * 1.4f, new Color(1f, .85f, .6f), 26f, 2.8f);
            // "Disco Yes": a small mirror ball over a YES in lights
            var dy = Song(z, "Disco Yes"); var yp = z.At(1f, 9f); foreach (var e in new[] { -1f, 1f }) { z.mk.Prism(iron, yp + z.side * e - up * .2f, up, .05f, 2.6f, 5); ArtistBits.PostCollider(z.root, yp + z.side * e, 2.4f, .07f); } z.mk.Box(dark, yp + up * 2f, new Vector3(2.2f, 1f, .12f), rot); ArtistBits.Label(z.labels, "YES", yp + up * 2f + z.front * .08f, z.front, .7f, new Color(1f, .55f, .8f), TextAnchor.MiddleCenter);
            var yg = SongGlow(dy, new Color(1f, .4f, .7f), 2f); z.mk.Box(yg, yp + up * 2.5f + z.front * .07f, new Vector3(2.1f, .06f, .04f), rot); z.mk.Box(yg, yp + up * 1.5f + z.front * .07f, new Vector3(2.1f, .06f, .04f), rot); z.mk.Prism(iron, yp + up * 2.6f, up, .012f, .5f, 3); z.mk.Blob(SongGlow(dy, new Color(.9f, .92f, 1f), 1.6f), yp + up * 3.3f, Vector3.one * .3f, rng, .14f); halos.Add(yp + up * 3.3f, 1.6f, new Color(1f, .7f, .9f), .45f);
            // "Nightrider": a bicycle with its lamps on, leaning on a lamp post
            var nr = Song(z, "Nightrider"); var bk = z.At(1f, -9f); z.mk.Prism(iron, bk - z.side * .9f - up * .2f, up, .07f, 3.4f, 6); z.mk.Box(SongGlow(nr, Brass, 2f), bk - z.side * .9f + up * 3.3f, new Vector3(.3f, .4f, .3f), rot); ArtistBits.PostCollider(z.root, bk - z.side * .9f, 3.2f, .1f);
            foreach (var e in new[] { -.52f, .52f }) { z.mk.Prism(dark, bk + up * .36f + z.side * e - z.front * .02f, z.front, .36f, .04f, 12); z.mk.Prism(WorldMaterials.Solid(Paints[2]), bk + up * .36f + z.side * e - z.front * .03f, z.front, .3f, .06f, 10); } z.mk.Box(WorldMaterials.Solid(Paints[2]), bk + up * .64f, new Vector3(1.04f, .05f, .05f), rot * Quaternion.Euler(0, 0, 12f)); z.mk.Box(dark, bk + up * .96f - z.side * .36f, new Vector3(.24f, .05f, .12f), rot);
            z.mk.Blob(SongGlow(nr, new Color(1f, .96f, .8f), 2.6f), bk + up * .9f + z.side * .62f, Vector3.one * .07f, rng, 0f); z.mk.Blob(SongGlow(nr, new Color(1f, .2f, .2f), 2.2f), bk + up * .7f - z.side * .6f, Vector3.one * .05f, rng, 0f); BoxCol(z.root, bk + up * .5f, new Vector3(1.8f, 1f, .3f), rot); halos.Add(bk + up * .9f + z.side * 1f, 1.2f, Brass, .5f); life.nightSong = nr;
            // "Beautiful Escape": a fire escape up to a painted door in the sky
            var be = Song(z, "Beautiful Escape"); var fe = z.At(-1.5f, -6.5f); z.mk.Box(brick, fe + up * 3f, new Vector3(3f, 6f, .4f), rot); z.solid.Box(brick, fe + up * 3f, new Vector3(3f, 6f, .4f), rot);
            for (var k = 0; k < 2; k++) { z.mk.Box(iron, fe + up * (1.9f + k * 2.2f) + z.front * .8f, new Vector3(2.6f, .06f, 1.2f), rot); for (var r = 0; r < 6; r++) z.mk.Box(iron, fe + up * (.5f + k * 2.2f + r * .26f) + z.front * (1.3f - r * .14f) + z.side * (k == 0 ? -1f : 1f) * .9f, new Vector3(.5f, .03f, .1f), rot); }
            z.mk.Box(SongGlow(be, new Color(.5f, .85f, 1f), 1.6f), fe + up * 5f + z.front * .22f, new Vector3(1f, 1.9f, .05f), rot); ArtistBits.Label(z.labels, "EXIT", fe + up * 6.2f + z.front * .22f, z.front, .2f, new Color(.6f, 1f, .7f), TextAnchor.MiddleCenter); halos.Add(fe + up * 5f + z.front * .8f, 1.8f, new Color(.5f, .85f, 1f), .45f);
            // "Tidal Wave": a wave of teal light
            var tw = Song(z, "Tidal Wave"); var wv = z.At(5.5f, 5.5f); var wg = SongGlow(tw, Teal, 1.6f); for (var k = 0; k < 16; k++) { var t = k / 15f; var h = .3f + Mathf.Pow(t, 1.6f) * 2.4f; var curl = t > .7f ? (t - .7f) / .3f : 0f; z.mk.Box(wg, wv + z.side * (-2f + t * 3.4f - curl * .9f) + up * (h / 2f + curl * .2f), new Vector3(.2f, h * (1f - curl * .5f), .5f), rot * Quaternion.Euler(0, 0, curl * 60f)); }
            z.mk.Box(stoneM, wv + up * .08f, new Vector3(4.6f, .16f, 1f), rot); BoxCol(z.root, wv + up * 1.2f, new Vector3(4.2f, 2.4f, .6f), rot); halos.Add(wv + up * 1.6f + z.front * .6f, 2.4f, Teal, .4f); life.tidalSong = tw;
            // "Last 100": a brass 100 on a milestone. "What Kinda Music": a question mark of bulbs
            var l1 = Song(z, "Last 100"); var ms = z.At(7.5f, -3.5f); z.mk.Box(stoneM, ms + up * .5f, new Vector3(.9f, 1f, .3f), rot); z.mk.Prism(stoneM, ms + up * 1f - z.front * .15f, z.front, .45f, .3f, 10); BoxCol(z.root, ms + up * .6f, new Vector3(.9f, 1.4f, .3f), rot); z.mk.Box(SongGlow(l1, Brass, 1.2f), ms + up * .12f + z.front * .16f, new Vector3(.8f, .06f, .02f), rot); ArtistBits.Label(z.labels, "100", ms + up * .7f + z.front * .16f, z.front, .34f, Brass, TextAnchor.MiddleCenter);
            var wk = Song(z, "What Kinda Music"); var qm = z.At(7.5f, 1.5f); var qg = SongGlow(wk, new Color(1f, .86f, .58f), 2f); z.mk.Prism(iron, qm - up * .2f, up, .04f, 1.2f, 5); for (var k = 0; k < 9; k++) { var a = (200f - k * 30f) * Mathf.Deg2Rad; z.mk.Blob(qg, qm + up * (2.9f + Mathf.Sin(a) * .5f) + z.side * Mathf.Cos(a) * .5f, Vector3.one * .08f, rng, 0f); } for (var k = 0; k < 3; k++) z.mk.Blob(qg, qm + up * (2.3f - k * .2f), Vector3.one * .08f, rng, 0f); z.mk.Blob(qg, qm + up * 1.4f, Vector3.one * .1f, rng, 0f); halos.Add(qm + up * 2.4f, 1.6f, Brass, .4f); ArtistBits.PostCollider(z.root, qm, 1.2f, .08f);
            Song(z, "Sensational");
        }

        // ---------------------------------------------------------------- Yussef Dayes
        void YussefDayes(Plot z)
        {
            var up = Vector3.up; var rot = z.Rot; var c = z.At(-2.5f, 0); var rng = new System.Random(11);
            // a drum kit on a round rug under a canopy of lights, to play
            z.mk.Prism(WorldMaterials.Solid(new Color(.5f, .16f, .2f)), c + up * .1f, up, 3.6f, .04f, 18); z.mk.Prism(WorldMaterials.Solid(new Color(.86f, .66f, .28f)), c + up * .11f, up, 2.6f, .04f, 18); z.mk.Prism(WorldMaterials.Solid(new Color(.2f, .46f, .5f)), c + up * .12f, up, 1.6f, .04f, 18);
            var km = new MeshKit(); var shell = WorldMaterials.Solid(new Color(.86f, .66f, .28f), .7f); km.Prism(shell, new Vector3(0, .32f, -.25f), Vector3.forward, .54f, .5f, 14); km.Prism(white, new Vector3(0, .32f, .25f), Vector3.forward, .5f, .02f, 14); foreach (var e in new[] { -1f, 1f }) { km.Prism(shell, new Vector3(e * .9f, .5f, -.3f), Vector3.up, .32f, .26f, 12); km.Prism(brassM, new Vector3(e * 1.25f, 1.25f, -.5f), Vector3.up, .38f, .02f, 14); km.Prism(iron, new Vector3(e * 1.25f, 0, -.5f), Vector3.up, .015f, 1.25f, 4); km.Prism(shell, new Vector3(e * .32f, .78f, -.1f), Vector3.up, .2f, .18f, 12); } km.Prism(brassM, new Vector3(0, 1.45f, -1f), Vector3.up, .44f, .02f, 14); km.Prism(iron, new Vector3(0, 0, -1f), Vector3.up, .015f, 1.45f, 4);
            var kgo = km.BuildSingle("Yussef's kit", tlife.transform, false); kgo.transform.SetPositionAndRotation(c + up * .14f, rot); BoxCol(z.root, c + up * .6f, new Vector3(2.8f, 1.2f, 1.6f), rot);
            var stool = c - z.front * 1.3f + up * .14f; z.mk.Prism(iron, stool, up, .04f, .5f, 5); z.mk.Prism(dark, stool + up * .5f, up, .2f, .08f, 10); Interact.Seat(tlife.transform, stool + up * .6f, z.front, "Sit at the kit").seatHeight = .6f;
            string[] groove = { "A broken beat that never falls over. You don't know how you're doing it. Don't stop.", "Ghost notes, rimshot, a splash that hangs in the air for a bar.", "You push the tempo and the lights overhead push with you.", "The hi-hat's breathing. You're just keeping it company." }; var gn = 0;
            Interact.Toy(kgo, Toy.Kind.Custom, "Play the drums", new Vector3(2.8f, 1.6f, 1.8f)).onUse = _ => { life.played = Time.time; Say1(c + up * 3f, groove[gn++ % groove.Length]); };
            var tops = new List<Vector3>(); for (var k = 0; k < 5; k++) { var d = Quaternion.Euler(0, k * 72f, 0) * z.front; var p = c + d * 4.4f; z.mk.Prism(iron, p - up * .2f, up, .05f, 3.8f, 5); ArtistBits.PostCollider(z.root, p, 3.6f, .07f); tops.Add(p + up * 3.6f); } for (var k = 0; k < 5; k++) { BulbString(z.mk, tops[k], tops[(k + 2) % 5], k); }
            z.area.glowLight = ArtistBits.PointLight(z.root, c + up * 3.4f, Brass, 24f, 2.8f);
            // "Birds of Paradise": the flowers, and paper birds on threads
            var bp = Song(z, "Birds of Paradise"); var fl = z.At(2f, 7f); var orange = SongGlow(bp, new Color(1f, .55f, .15f), 1.3f); var blue = SongGlow(bp, new Color(.35f, .5f, 1f), 1.3f);
            for (var k = 0; k < 4; k++) { var fp = fl + z.side * (-1.2f + k * .8f) + z.front * ((k % 2) * .5f); z.mk.Prism(leafDark, fp - up * .1f, (up + z.side * (k - 1.5f) * .08f).normalized, .04f, 1.5f + (k % 2) * .4f, 4); var top = fp + up * (1.4f + (k % 2) * .4f); for (var q = 0; q < 3; q++) z.mk.Box(orange, top + z.side * .16f + up * (q * .12f), new Vector3(.5f, .05f, .14f), rot * Quaternion.Euler(0, 0, 30f + q * 18f)); z.mk.Box(blue, top + z.side * .3f + up * .05f, new Vector3(.4f, .04f, .08f), rot * Quaternion.Euler(0, 0, 10f)); z.mk.Box(leafM, fp + up * .6f - z.side * .3f, new Vector3(.2f, 1.1f, .5f), rot * Quaternion.Euler(0, 0, 14f)); }
            for (var k = 0; k < 5; k++) { var b = fl + up * (2.6f + (k % 2) * .5f) + z.side * (-1.6f + k * .8f) + z.front * .6f; z.mk.Box(white, b, new Vector3(.34f, .02f, .12f), rot * Quaternion.Euler(0, k * 30f, 16f)); z.mk.Box(white, b + up * .04f, new Vector3(.34f, .02f, .12f), rot * Quaternion.Euler(0, k * 30f, -16f)); } halos.Add(fl + up * 1.6f, 2f, new Color(1f, .6f, .3f), .35f);
            // "Turquoise Galaxy": a spiral of turquoise lights set into the ground
            var tg = Song(z, "Turquoise Galaxy"); var gx = z.At(2f, -7f); var tq = SongGlow(tg, new Color(.3f, 1f, .9f), 1.8f); for (var k = 0; k < 44; k++) { var a = k * .42f; var r = .2f + k * .07f; z.mk.Blob(tq, gx + new Vector3(Mathf.Cos(a) * r, .12f, Mathf.Sin(a) * r), Vector3.one * (.05f + (k % 5 == 0 ? .05f : 0f)), rng, 0f); } z.mk.Prism(dark, gx + up * .06f, up, 3.4f, .04f, 18); halos.Add(gx + up * .4f, 3f, new Color(.3f, 1f, .9f), .35f); life.galaxySong = tg;
            // "Rust": a rusted iron ring
            var ru = Song(z, "Rust"); var rs = z.At(6f, 2.5f); var rust = WorldMaterials.Solid(new Color(.6f, .32f, .18f)); for (var k = 0; k < 16; k++) { var a = k * 22.5f * Mathf.Deg2Rad; z.mk.Box(rust, rs + up * (1.5f + Mathf.Sin(a) * 1.2f) + z.side * Mathf.Cos(a) * 1.2f, new Vector3(.5f, .22f, .22f), rot * Quaternion.Euler(0, 0, k * 22.5f + 90f)); } z.mk.Blob(SongGlow(ru, new Color(1f, .5f, .2f), 1.6f), rs + up * 1.5f, Vector3.one * .16f, rng, 0f); BoxCol(z.root, rs + up * 1.4f, new Vector3(2.8f, 2.8f, .3f), rot);
        }

        // ---------------------------------------------------------------- Yussef Kamaal
        void YussefKamaal(Plot z)
        {
            var up = Vector3.up; var rot = z.Rot; var c = z.At(-2f, 0); var rng = new System.Random(12);
            z.mk.Box(WorldMaterials.Solid(new Color(.6f, .14f, .16f)), c + up * .86f, new Vector3(1.5f, .2f, .56f), rot); z.mk.Box(white, c + up * .97f + z.front * .1f, new Vector3(1.4f, .03f, .24f), rot); z.mk.Box(dark, c + up * 1.02f - z.front * .14f, new Vector3(1.5f, .14f, .26f), rot); foreach (var e in new[] { -.65f, .65f }) z.mk.Prism(brassM, c + z.side * e, up, .03f, .8f, 5); BoxCol(z.root, c + up * .55f, new Vector3(1.5f, 1.1f, .6f), rot);
            var st = c + z.front * .9f; z.mk.Prism(iron, st, up, .04f, .46f, 5); z.mk.Prism(dark, st + up * .46f, up, .2f, .08f, 10); Interact.Seat(tlife.transform, st + up * .56f, -z.front, "Sit at the keys").seatHeight = .56f;
            // "Strings of Light": a fan of taut glowing strings from a post to the ground
            var sl = Song(z, "Strings of Light"); var sp = z.At(2f, 5.5f); z.mk.Prism(iron, sp - up * .2f, up, .07f, 4.2f, 6); ArtistBits.PostCollider(z.root, sp, 4f, .1f); for (var k = 0; k < 9; k++) { var end = sp + Quaternion.Euler(0, k * 22f - 88f, 0) * z.front * 3.4f; end.y = z.ctx.Ground(end.x, end.z) + .05f; var top = sp + up * 4f; z.mk.Prism(SongGlow(sl, Color.Lerp(Brass, Teal, k / 8f), 1.6f), top, (end - top).normalized, .02f, (end - top).magnitude, 4); } halos.Add(sp + up * 2f, 3f, Brass, .35f); life.stringsSong = sl;
            // "Joint 17": a door numbered 17 standing on its own
            var j = Song(z, "Joint 17"); var dp = z.At(2f, -5.5f); z.mk.Box(WorldMaterials.Solid(Paints[3]), dp + up * 1.1f, new Vector3(1.1f, 2.2f, .1f), rot); z.mk.Box(stoneM, dp + up * 1.1f - z.front * .02f, new Vector3(1.4f, 2.4f, .08f), rot); BoxCol(z.root, dp + up * 1.1f, new Vector3(1.4f, 2.4f, .14f), rot); z.mk.Blob(SongGlow(j, Brass, 1.6f), dp + up * 1.1f + z.side * .38f + z.front * .08f, Vector3.one * .05f, rng, 0f); ArtistBits.Label(z.labels, "17", dp + up * 1.7f + z.front * .07f, z.front, .3f, Brass, TextAnchor.MiddleCenter);
        }

        // ---------------------------------------------------------------- Ezra Collective
        void EzraCollective(Plot z)
        {
            var up = Vector3.up; var rot = z.Rot; var c = z.At(-2.5f, 0); var rng = new System.Random(13);
            // a street party: bunting between four poles, three horns on stands, a low stage
            z.mk.Box(woodM, c + up * .2f, new Vector3(5f, .4f, 3f), rot); z.solid.Box(woodM, c, new Vector3(5f, .8f, 3f), rot); z.mk.Box(woodM, c + up * .1f + z.front * 1.7f, new Vector3(1.6f, .2f, .4f), rot); z.solid.Box(woodM, c + up * .1f + z.front * 1.7f, new Vector3(1.6f, .2f, .4f), rot);
            var tops = new List<Vector3>(); foreach (var (x, zz) in new[] { (-1f, 1f), (1f, 1f), (1f, -1f), (-1f, -1f) }) { var p = c + z.side * x * 4.6f + z.front * zz * 3.6f; p.y = z.ctx.Ground(p.x, p.z); z.mk.Prism(iron, p - up * .2f, up, .05f, 3.8f, 5); ArtistBits.PostCollider(z.root, p, 3.6f, .07f); tops.Add(p + up * 3.6f); }
            for (var k = 0; k < 4; k++) { var a = tops[k]; var b = tops[(k + 1) % 4]; for (var s = 1; s < 10; s++) { var t = s / 10f; var p = Vector3.Lerp(a, b, t) - up * Mathf.Sin(t * Mathf.PI) * .5f; z.mk.Prism(WorldMaterials.Glow(Bloom[(s + k) % Bloom.Length], .4f), p, -up, .16f, .28f, 3); } }
            var vd = Song(z, "Victory Dance"); var brass2 = SongGlow(vd, Brass, 1.3f); for (var k = 0; k < 3; k++) { var h = c + up * .4f + z.side * (-1.4f + k * 1.4f) - z.front * .3f; z.mk.Prism(iron, h, up, .025f, 1.2f, 5); var hd = (z.front + up * .5f).normalized; z.mk.Prism(brassM, h + up * 1.1f - hd * .4f, hd, .04f, .6f + k * .15f, 6); z.mk.Cone(brass2, h + up * 1.1f + hd * (.5f + k * .15f), .2f + k * .04f, -.4f, 10); }
            z.area.glowLight = ArtistBits.PointLight(z.root, c + up * 3.4f + z.front * 1f, new Color(1f, .8f, .5f), 24f, 2.8f); life.victoryAt = c + up * 4f; life.victorySong = vd;
            // the ribbon arch out front
            var ar = z.At(3.4f, 0); var prev = ar - z.side * 2.4f; for (var s = 1; s <= 12; s++) { var t = s / 12f; var p = ar + z.side * Mathf.Lerp(-2.4f, 2.4f, t) + up * Mathf.Sin(t * Mathf.PI) * 3f; z.mk.Prism(SongGlow(vd, Bloom[s % Bloom.Length], .9f), prev, (p - prev).normalized, .1f, Vector3.Distance(prev, p) + .03f, 5); prev = p; } foreach (var e in new[] { -1f, 1f }) ArtistBits.PostCollider(z.root, ar + z.side * e * 2.4f, 1.4f, .14f);
            // "Only Love": a heart in lights. "Well Organised": a tidy chest of labelled drawers
            var ol = Song(z, "Only Love"); var hp = z.At(1f, 7.5f); var heart = SongGlow(ol, new Color(1f, .35f, .45f), 2f); z.mk.Prism(iron, hp - up * .2f, up, .05f, 1.5f, 5); for (var k = 0; k < 22; k++) { var t = k / 22f * Mathf.PI * 2f; var hx = 16f * Mathf.Pow(Mathf.Sin(t), 3f); var hy = 13f * Mathf.Cos(t) - 5f * Mathf.Cos(2f * t) - 2f * Mathf.Cos(3f * t) - Mathf.Cos(4f * t); z.mk.Blob(heart, hp + up * (2.4f + hy * .055f) + z.side * hx * .055f, Vector3.one * .08f, rng, 0f); } halos.Add(hp + up * 2.4f + z.front * .3f, 1.8f, new Color(1f, .45f, .5f), .5f); ArtistBits.PostCollider(z.root, hp, 1.5f, .08f);
            var wo = Song(z, "Well Organised"); var ch = z.At(1f, -7.5f); z.mk.Box(woodM, ch + up * .9f, new Vector3(1.6f, 1.8f, .6f), rot); BoxCol(z.root, ch + up * .9f, new Vector3(1.6f, 1.8f, .6f), rot); for (var r = 0; r < 4; r++) for (var q = 0; q < 3; q++) { z.mk.Box(WorldMaterials.Solid(Paints[(r + q) % Paints.Length]), ch + up * (.3f + r * .42f) + z.side * (-.5f + q * .5f) + z.front * .31f, new Vector3(.44f, .34f, .03f), rot); z.mk.Blob(SongGlow(wo, Brass, 1.2f), ch + up * (.3f + r * .42f) + z.side * (-.5f + q * .5f) + z.front * .34f, Vector3.one * .03f, rng, 0f); }
            Bench(z.mk, z.At(5.5f, 4f), -z.front);
        }

        // ---------------------------------------------------------------- the older fusion players
        void Elders(Plot z)
        {
            var up = Vector3.up; var rot = z.Rot; var c = z.At(-1.5f, 0); var rng = new System.Random(z.a.name.Length); string s;
            Bench(z.mk, z.At(3.6f, 3.4f), -z.front);
            switch (z.a.name)
            {
                case "CASIOPEA":
                    s = Song(z, "ドミノ・ライン"); if (s == null && SongIds.TryGetValue(z.a.name, out var cl) && cl.Length > 0) { s = cl[0].id; z.lit.Add(s); }
                    for (var k = 0; k < 9; k++) { var dm = new MeshKit(); dm.Box(white, new Vector3(0, .8f, 0), new Vector3(.8f, 1.6f, .2f), Quaternion.identity); dm.Box(dark, new Vector3(0, .8f, .101f), new Vector3(.7f, .03f, .01f), Quaternion.identity); var pips = SongGlow(s, k % 2 == 0 ? Teal : Brass, 1.6f); for (var q = 0; q < 1 + k % 5; q++) dm.Blob(pips, new Vector3(-.2f + (q % 3) * .2f, 1.2f + (q / 3) * .2f, .11f), new Vector3(.05f, .05f, .01f), rng, 0f); for (var q = 0; q < 1 + (k * 3) % 5; q++) dm.Blob(pips, new Vector3(-.2f + (q % 3) * .2f, .3f + (q / 3) * .2f, .11f), new Vector3(.05f, .05f, .01f), rng, 0f);
                        var dgo = dm.BuildSingle("Domino", tlife.transform, false); var dp = c + z.side * (-4f + k * 1f) + z.front * Mathf.Sin(k * .5f) * .8f; dp.y = z.ctx.Ground(dp.x, dp.z) + .1f; dgo.transform.SetPositionAndRotation(dp, Quaternion.LookRotation(z.side)); life.dominoes.Add(dgo.transform); life.dominoRot.Add(Quaternion.LookRotation(z.side)); }
                    var push = new GameObject("Domino line"); push.transform.SetParent(tlife.transform, false); push.transform.position = c - z.side * 4.6f + up * 1f; string[] fall = { "Click-click-click-click... all the way down the line, in perfect sixteenths.", "They go over like a keyboard run. Then, politely, they stand back up.", "Tak-tak-tak-tak. Somebody across the green applauds the timing." }; var fn = 0;
                    Interact.Toy(push, Toy.Kind.Custom, "Push the first domino", new Vector3(1.4f, 2f, 1.4f)).onUse = _ => { life.dominoAt = Time.time; Say1(c + up * 3f, fall[fn++ % fall.Length]); }; z.area.glowLight = ArtistBits.PointLight(z.root, c + up * 4f + z.front * 2f, Teal, 20f, 2.4f); break;
                case "Pat Metheny Group":
                    s = Song(z, "The Roots of Coincidence"); z.mk.Prism(WorldMaterials.Solid(new Color(.6f, .58f, .5f)), c - up * .3f, up, .34f, 3.6f, 7, .7f); ArtistBits.PostCollider(z.root, c, 3.2f, .36f); z.mk.Blob(leafM, c + up * 5f, new Vector3(2.8f, 2f, 2.8f), rng, .2f); z.mk.Blob(leafDark, c + up * 4.4f + z.side * 1.4f, new Vector3(1.8f, 1.4f, 1.8f), rng, .2f);
                    var root = SongGlow(s, new Color(1f, .7f, .3f), 1.8f); for (var k = 0; k < 9; k++) { var d = Quaternion.Euler(0, k * 40f, 0) * z.front; var prev = c + up * .1f; for (var q = 1; q <= 5; q++) { var p = c + d * q * .7f + Quaternion.Euler(0, 90f, 0) * d * Mathf.Sin(q * 1.3f + k) * .3f; p.y = z.ctx.Ground(p.x, p.z) + .1f; z.mk.Prism(root, prev, (p - prev).normalized, .05f - q * .007f, Vector3.Distance(prev, p), 4); prev = p; } } halos.Add(c + up * .4f, 3.4f, Brass, .35f);
                    var pg = z.At(2.6f, -3.4f); z.mk.Prism(iron, pg, up, .02f, .4f, 4); z.mk.Blob(woodM, pg + up * .8f, new Vector3(.4f, .4f, .07f), rng, .05f); for (var k = 0; k < 3; k++) z.mk.Box(woodM, pg + up * 1.5f + z.side * (-.24f + k * .24f), new Vector3(.06f, 1f, .04f), rot * Quaternion.Euler(0, 0, (k - 1) * -14f)); for (var k = 0; k < 10; k++) z.mk.Box(SongGlow(s, Cream, 1f), pg + up * .9f + z.side * (-.3f + k * .066f) + z.front * .05f, new Vector3(.006f, .8f, .006f), rot * Quaternion.Euler(0, 0, (k - 5) * 3f)); ArtistBits.PostCollider(z.root, pg, 2f, .3f); break;
                case "Allan Holdsworth":
                    s = Song(z, "Devil Take the Hindmost"); var note = SongGlow(s, Indigo, 1.8f); for (var k = 0; k < 12; k++) { var p = c + z.side * (-2.6f + k * .46f) + up * (.3f + k * .3f + (k % 3 == 2 ? .2f : 0f)); z.mk.Blob(note, p, new Vector3(.16f, .12f, .12f), rng, 0f); z.mk.Box(note, p + up * .32f + z.side * .13f, new Vector3(.03f, .6f, .03f), rot); if (k > 0) z.mk.Box(note, p + up * .6f - z.side * .1f, new Vector3(.5f, .06f, .03f), rot * Quaternion.Euler(0, 0, 32f)); } foreach (var e in new[] { -2.8f, 2.8f }) { z.mk.Prism(iron, c + z.side * e - up * .2f, up, .05f, e < 0 ? .7f : 4.4f, 5); ArtistBits.PostCollider(z.root, c + z.side * e, e < 0 ? .7f : 4f, .07f); }
                    halos.Add(c + up * 2.4f, 3f, Indigo, .35f); var wv = z.At(2.6f, -3.6f); z.mk.Prism(iron, wv - up * .2f, up, .04f, 2.6f, 5); z.mk.Box(brassM, wv + up * 2.5f, new Vector3(1f, .04f, .04f), rot); z.mk.Blob(WorldMaterials.Solid(new Color(.75f, .15f, .15f)), wv + up * 2.75f, new Vector3(.12f, .18f, .08f), rng, .05f); foreach (var e in new[] { -.06f, .06f }) z.mk.Cone(WorldMaterials.Solid(new Color(.75f, .15f, .15f)), wv + up * 2.9f + z.side * e, .03f, .1f, 4); z.mk.Prism(WorldMaterials.Solid(new Color(.75f, .15f, .15f)), wv + up * 2.66f - z.side * .1f, (-z.side + up * .5f).normalized, .015f, .3f, 3); ArtistBits.PostCollider(z.root, wv, 2.4f, .08f); break;
                case "Victor Wooten":
                    s = Song(z, "Dc10"); z.mk.Prism(iron, c, up, .02f, .35f, 5); z.mk.Blob(WorldMaterials.Solid(new Color(.75f, .5f, .2f), .7f), c + up * .75f, new Vector3(.26f, .34f, .07f), rng, .05f); z.mk.Box(woodM, c + up * 1.65f, new Vector3(.08f, 1.2f, .04f), rot); z.mk.Box(dark, c + up * 2.3f, new Vector3(.12f, .24f, .04f), rot); for (var k = 0; k < 4; k++) z.mk.Box(SongGlow(s, Brass, 1.6f), c + up * 1.45f + z.side * (-.03f + k * .02f) + z.front * .04f, new Vector3(.006f, 1.5f, .006f), rot); ArtistBits.PostCollider(z.root, c, 2.2f, .26f);
                    var fin = z.At(1.6f, -4f); var skin = WorldMaterials.Solid(new Color(.86f, .88f, .92f), .7f); z.mk.Box(skin, fin + up * .6f, new Vector3(4.2f, 1.2f, 1f), rot); z.mk.Box(skin, fin + up * 2.6f - z.side * .6f, new Vector3(1.8f, 3f, .22f), rot * Quaternion.Euler(0, 0, 16f)); z.mk.Prism(skin, fin + up * 2.1f - z.side * .5f - z.front * .1f, z.front, .55f, 1.6f, 10); z.mk.Box(SongGlow(s, new Color(.9f, .2f, .2f), 1.2f), fin + up * 3.5f - z.side * .85f + z.front * .12f, new Vector3(1.3f, .5f, .02f), rot * Quaternion.Euler(0, 0, 16f)); BoxCol(z.root, fin + up * 1.6f, new Vector3(4.2f, 3.2f, 1f), rot); ArtistBits.Label(z.labels, "DC-10", fin + up * .6f + z.front * .51f, z.front, .3f, Ink, TextAnchor.MiddleCenter); break;
                default:
                    s = Song(z, "Bubblehouse"); Color[] bub = { new Color(.6f, .9f, 1f), new Color(1f, .7f, .85f), new Color(.8f, 1f, .7f), new Color(1f, .9f, .6f) }; for (var k = 0; k < 26; k++) { var a = k * 2.4f; var h = (k % 6) * .5f; var r = 1.9f - h * .28f; var p = c + new Vector3(Mathf.Cos(a) * r, .5f + h, Mathf.Sin(a) * r); z.mk.Blob(SongGlow(s, bub[k % 4], .55f), p, Vector3.one * (.5f - h * .04f), rng, .04f); } z.mk.Blob(SongGlow(s, bub[0], .55f), c + up * 3.5f, Vector3.one * .5f, rng, .04f);
                    BoxCol(z.root, c + up * 1.6f, new Vector3(4f, 3.2f, .3f), rot); BoxCol(z.root, c + up * 1.6f, new Vector3(.3f, 3.2f, 4f), rot); halos.Add(c + up * 1.8f, 3.4f, new Color(.7f, .95f, 1f), .35f); z.area.glowLight = ArtistBits.PointLight(z.root, c + up * 4.4f + z.front * 2f, new Color(.8f, .95f, 1f), 20f, 2.4f); break;
            }
        }

        // ---------------------------------------------------------------- everyone else
        void Others(Plot z)
        {
            var up = Vector3.up; var rot = z.Rot; var p = z.At(2.8f, 2.8f); var rng = new System.Random(z.a.name.Length * 3); string s;
            switch (z.a.name)
            {
                case "BADBADNOTGOOD":
                    s = Song(z, "Confessions"); z.mk.Box(woodM, p + up * 1.2f, new Vector3(1.4f, 2.4f, 1.1f), rot); BoxCol(z.root, p + up * 1.2f, new Vector3(1.4f, 2.4f, 1.1f), rot); z.mk.Box(WorldMaterials.Solid(new Color(.5f, .14f, .2f)), p + up * 1.1f + z.front * .56f, new Vector3(.9f, 1.9f, .03f), rot); z.mk.Box(SongGlow(s, Brass, 1.4f), p + up * 2.2f + z.front * .57f, new Vector3(.5f, .2f, .02f), rot); z.mk.Prism(brassM, p + up * .9f + z.side * 1f, (up + z.side * .3f).normalized, .05f, .8f, 6, 2f); halos.Add(p + up * 2.2f + z.front * 1f, 1f, Brass, .4f); break;
                case "GoGo Penguin":
                    s = Song(z, "Garden Dog Barbecue"); z.mk.Prism(dark, p + up * .5f, up, .34f, .26f, 10, 1.2f); foreach (var k in new[] { 0f, 120f, 240f }) z.mk.Prism(iron, p, (Quaternion.Euler(0, k, 0) * z.front * .3f + up).normalized, .02f, .56f, 3); z.mk.Blob(SongGlow(s, new Color(1f, .5f, .2f), 1.8f), p + up * .72f, new Vector3(.26f, .05f, .26f), rng, 0f); halos.Add(p + up * .9f, 1.2f, new Color(1f, .55f, .25f), .5f); ArtistBits.PostCollider(z.root, p, .8f, .34f);
                    var dg = z.At(2.2f, 4.4f); var fur = WorldMaterials.Solid(new Color(.75f, .6f, .4f)); z.mk.Blob(fur, dg + up * .3f, new Vector3(.2f, .2f, .4f), rng, .04f); z.mk.Blob(fur, dg + up * .5f + z.front * .36f, Vector3.one * .15f, rng, 0f); foreach (var e in new[] { -.08f, .08f }) z.mk.Box(fur, dg + up * .62f + z.front * .34f + z.side * e, new Vector3(.06f, .14f, .04f), rot); z.mk.Prism(fur, dg + up * .36f - z.front * .36f, (up - z.front * .4f).normalized, .03f, .3f, 4); break;
                case "Krystyna Prońko":
                    s = SongIds.TryGetValue(z.a.name, out var kl) && kl.Length > 0 ? kl[0].id : null; if (s != null) z.lit.Add(s); z.mk.Box(stoneM, p + up * .4f, new Vector3(.8f, .8f, .8f), rot); BoxCol(z.root, p + up * .4f, new Vector3(.8f, .8f, .8f), rot); z.mk.Prism(WorldMaterials.Glow(new Color(.6f, .4f, .2f), .3f), p + up * .8f, up, .2f, .6f, 10); z.mk.Prism(white, p + up * 1.4f, up, .08f, .2f, 8); z.mk.Box(white, p + up * 1.1f + z.front * .2f, new Vector3(.26f, .3f, .02f), rot);
                    var hh = SongGlow(s, new Color(1f, .35f, .45f), 2f); for (var k = 0; k < 14; k++) { var t = k / 14f * Mathf.PI * 2f; var hx = 16f * Mathf.Pow(Mathf.Sin(t), 3f); var hy = 13f * Mathf.Cos(t) - 5f * Mathf.Cos(2f * t) - 2f * Mathf.Cos(3f * t) - Mathf.Cos(4f * t); z.mk.Blob(hh, p + up * (2.3f + hy * .03f) + z.side * hx * .03f, Vector3.one * .05f, rng, 0f); } halos.Add(p + up * 2.3f, 1.2f, new Color(1f, .45f, .5f), .45f); break;
                case "Latarnik":
                    s = Song(z, "Insia"); z.mk.Prism(iron, p - up * .2f, up, .07f, 3.8f, 6); z.mk.Box(SongGlow(s, Brass, 2.2f), p + up * 3.7f, new Vector3(.34f, .46f, .34f), rot); z.mk.Cone(brassM, p + up * 3.93f, .3f, .24f, 4, 45f); halos.Add(p + up * 3.7f, 2.2f, Brass, .6f); ArtistBits.PostCollider(z.root, p, 3.6f, .1f); foreach (var e in new[] { -.25f, .25f }) z.mk.Box(woodM, p + z.front * .7f + z.side * e + up * 1.5f, new Vector3(.05f, 3.2f, .05f), rot * Quaternion.Euler(-14f, 0, 0)); for (var k = 0; k < 7; k++) z.mk.Box(woodM, p + z.front * (1.06f - k * .1f) + up * (.3f + k * .42f), new Vector3(.5f, .04f, .05f), rot); break;
                case "Mildlife":
                    s = Song(z, "The Magnificent Moon"); z.mk.Prism(iron, p - up * .2f, up, .06f, 2.4f, 6); z.mk.Blob(SongGlow(s, new Color(1f, .96f, .82f), 1.5f), p + up * 3.6f, Vector3.one * 1.2f, rng, .04f); for (var k = 0; k < 5; k++) z.mk.Blob(WorldMaterials.Glow(new Color(.9f, .86f, .7f), .6f), p + up * 3.6f + Quaternion.Euler(k * 50f - 100f, k * 70f, 0) * Vector3.forward * 1.14f, Vector3.one * (.16f + (k % 2) * .1f), rng, 0f); halos.Add(p + up * 3.6f, 4f, new Color(1f, .95f, .8f), .5f); ArtistBits.PostCollider(z.root, p, 2.4f, .1f); break;
                case "Rishab Rikhiram Sharma":
                    s = Song(z, "Chanakya"); z.mk.Box(WorldMaterials.Solid(new Color(.6f, .16f, .2f)), p + up * .12f, new Vector3(2.4f, .04f, 1.6f), rot); z.mk.Box(WorldMaterials.Solid(new Color(.86f, .66f, .28f)), p + up * .15f, new Vector3(2f, .04f, 1.2f), rot); z.mk.Blob(woodM, p + up * .4f - z.side * .5f, new Vector3(.26f, .24f, .26f), rng, .04f); z.mk.Box(woodM, p + up * .62f + z.side * .2f, new Vector3(1.3f, .07f, .09f), rot * Quaternion.Euler(0, 0, 14f)); for (var k = 0; k < 5; k++) z.mk.Blob(brassM, p + up * (.62f + k * .05f) + z.side * (.1f + k * .16f) + z.front * .06f, Vector3.one * .03f, rng, 0f);
                    z.mk.Prism(brassM, p + z.side * .9f + z.front * .4f, up, .1f, .3f, 8); z.mk.Blob(SongGlow(s, new Color(1f, .7f, .3f), 2f), p + z.side * .9f + z.front * .4f + up * .42f, new Vector3(.05f, .1f, .05f), rng, 0f); halos.Add(p + z.side * .9f + up * .5f, 1f, Brass, .5f); Interact.Seat(tlife.transform, p + up * .16f + z.front * .5f - z.side * .2f, -z.front, "Sit on the rug").seatHeight = .16f; break;
                case "The Comet Is Coming":
                    s = Song(z, "Birth Of Creation"); z.mk.Prism(iron, p - up * .2f, (up + z.side * .25f).normalized, .05f, 3.6f, 5); var head = p + (up + z.side * .25f).normalized * 3.5f; var cm = SongGlow(s, new Color(.6f, .9f, 1f), 2.4f); z.mk.Blob(cm, head, Vector3.one * .4f, rng, .1f); for (var k = 1; k < 9; k++) z.mk.Blob(cm, head - (z.side + up * .4f).normalized * k * .4f, Vector3.one * (.3f - k * .03f), rng, .1f); halos.Add(head, 2.4f, new Color(.6f, .9f, 1f), .55f); ArtistBits.PostCollider(z.root, p, 3f, .1f); break;
                case "Venna":
                    s = Song(z, "Sicily"); z.mk.Box(woodM, p + up * .3f, new Vector3(1f, .6f, .7f), rot); BoxCol(z.root, p + up * .3f, new Vector3(1f, .6f, .7f), rot); var lemon = SongGlow(s, new Color(1f, .9f, .3f), 1.2f); for (var k = 0; k < 8; k++) z.mk.Blob(lemon, p + up * .66f + z.side * (-.34f + (k % 4) * .23f) + z.front * ((k / 4) * .26f - .13f), new Vector3(.1f, .08f, .13f), rng, .1f); halos.Add(p + up * .8f, 1f, new Color(1f, .9f, .4f), .4f); z.mk.Prism(iron, p + z.side * 1.1f, up, .02f, .9f, 4); z.mk.Prism(brassM, p + z.side * 1.1f + up * .9f, (up + z.front * .3f).normalized, .05f, .7f, 6, 2.2f); ArtistBits.Label(z.labels, "SICILIA", p + up * .3f + z.front * .36f, z.front, .16f, Ink, TextAnchor.MiddleCenter); break;
                case "Tara Clerkin Trio":
                    s = Song(z, "Slow Island"); z.mk.Prism(SongGlow(s, Teal, .7f), p + up * .1f, up, 2f, .03f, 16); z.mk.Blob(WorldMaterials.Solid(new Color(.8f, .74f, .56f)), p + up * .14f, new Vector3(.8f, .16f, .7f), rng, .1f); z.mk.Prism(leafDark, p + up * .2f + z.side * .2f, up, .03f, .9f, 4); for (var k = 0; k < 4; k++) z.mk.Box(leafM, p + up * 1.1f + z.side * .2f + Quaternion.Euler(0, k * 90f, 0) * z.front * .3f, new Vector3(.12f, .03f, .6f), Quaternion.Euler(-20f, k * 90f + rot.eulerAngles.y, 0)); halos.Add(p + up * .5f, 2f, Teal, .3f);
                    var sh = Song(z, "Silently"); var q2 = z.At(2.2f, -3f); z.mk.Prism(iron, q2 - up * .2f, up, .04f, 1.6f, 5); z.mk.Box(SongGlow(sh, Cream, 1.2f), q2 + up * 1.8f, new Vector3(.9f, .5f, .04f), rot); ArtistBits.Label(z.labels, "shh", q2 + up * 1.8f + z.front * .03f, z.front, .22f, Ink, TextAnchor.MiddleCenter); ArtistBits.PostCollider(z.root, q2, 1.6f, .08f); break;
                default:
                    z.mk.Prism(iron, p, up, .04f, 1.3f, 5); z.mk.Box(GlowM(Brass, 1.4f), p + up * 1.42f, new Vector3(.2f, .28f, .2f), rot); break;
            }
        }
    }
}
