using System.Collections.Generic;
using UnityEngine;
using static EverythingWorld.World.PlaceTools;

namespace EverythingWorld.World
{
    /// The Lo-Fi Hills' artists. Most have a porch of their own: a wall with one big lit window, a little roof over
    /// a wooden deck, a hanging lamp, a chair, a plant, usually a cat; and one thing from their song. Covers on a
    /// cream card on a wooden board under its own small roof.
    /// - Idealism: a covered wooden deck in the rain: a low table with a blanket over it and cushions round it; a
    ///   rain chain running down into a bowl, and chimes ("waon to ame": the rain comes harder); a small glowing
    ///   machine with a lever and a little monster sitting on it ("monster gambling in tokyo"); a reading corner
    ///   with a sign that says nothing at all ("dont say a word").
    /// - digitalluc: a small car parked in the rain with its headlights on and the door open, to sit in ("Driver");
    ///   a fish tank as tall as you, glowing ("Underwater": everything goes teal and bubbles rise); a heap of
    ///   pillows under a dim red lamp ("Soft Hell").
    /// - Michael Lementi and Felipe Jara: the arcade corner. Two cabinets under an awning, one deep-sea green
    ///   ("Aquatic Ambience"), one racing blue ("Big Blue"); and an old television on a crate with a console, a
    ///   castle on the screen, a beanbag in front of it ("Inside the Castle Walls").
    /// - And fourteen more, one thing each: see Others.
    public partial class LofiKit
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
                Add("digitalluc", "Underwater", "@the corner"); Add("TOKiMONSTA", "Darkest", "@up lamp lane"); Add("quickly, quickly", "Getsomerest", "@the study");
                Add("Stan Forebee", "Awakening", "@up lamp lane"); Add("Idealism", "waon to ame", "@the deck in the rain");
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
            if (road == null) MakeMaterials();
            var root = new GameObject("Artist: " + a.name).transform; root.SetParent(ctx.Root, false);
            var front = facing.TryGetValue(a.name, out var f) ? f : new Vector3(center.x - a.x, 0, center.z - a.z).normalized;
            if (front.sqrMagnitude < .01f) front = Vector3.forward;
            var labels = new GameObject("Labels").transform; labels.SetParent(root, false);
            var area = root.gameObject.AddComponent<ArtistArea>();
            area.artistName = a.name; area.stationId = a.stationId; area.radius = a.r; area.glowColor = Lamp;
            var z = new Plot { ctx = ctx, root = root, labels = labels, mk = new MeshKit(), solid = new MeshKit(), pictures = new MeshKit(), a = a, c = ctx.OnGround(a.x, a.z), front = front, side = Vector3.Cross(Vector3.up, front), area = area };
            var reach = Foot(a); var up = Vector3.up;
            z.mk.Prism(paving, z.c - up * .25f, up, reach * .66f, .33f, 24);
            switch (a.name)
            {
                case "Idealism": Idealism(z); break;
                case "digitalluc": Digitalluc(z); break;
                case "Michael Lementi": Lementi(z); break;
                default: Others(z, Porch(z)); break;
            }
            SongLamps(z);
            Board(z, z.At(reach * .6f, -reach * .45f - 1.5f), front);
            ArtistBits.Plaque(z.mk, z.solid, labels, a, z.At(reach * .6f + 1.5f, reach * .45f + 1.5f), front, woodM, Paint(Cream), Ink, 1.6f);
            StreetLamp(ctx, z.mk, z.At(reach * .5f, reach * .68f), -z.side);
            Puddle(z.mk, z.At(reach * .34f, -reach * .2f) + up * .09f, .7f);
            if (z.area.glowLight == null) z.area.glowLight = ArtistBits.PointLight(root, z.c + up * 3.6f + front * 3f, new Color(1f, .8f, .54f), HasStation(a) ? 24f : 15f, 2.3f);
            area.glowRenderers = new Renderer[0];
            z.mk.Build("Area", root, false);
            z.solid.BuildCollider("Walls", root);
            z.pictures.Build("Covers", root, false);
            noGrass.Add(new Vector3(z.c.x, reach * .9f, z.c.z));
            var mark = new GameObject("Shot: " + a.name + " up close").transform; mark.SetParent(ctx.Root, false);
            var cam = z.At(reach * .85f, reach * .3f) + up * 2f; var look = z.At(-reach * .3f, 0) + up * 1.8f - cam; mark.SetPositionAndRotation(cam, Quaternion.LookRotation(look.normalized));
        }

        /// The covers on a cream card on a wooden board under its own small roof.
        void Board(Plot z, Vector3 ground, Vector3 dir)
        {
            dir = Vector3.ProjectOnPlane(dir, Vector3.up).normalized; var rot = Quaternion.LookRotation(dir);
            var n = Mathf.Max(1, Mathf.Min(7, z.a.songList?.Length ?? 1)); const float size = .9f; var w = n * size * 1.25f + .6f;
            var c = ground + Vector3.up * 1.7f;
            z.mk.Box(woodM, c - Vector3.up * .5f - dir * .06f, new Vector3(w + .5f, size + 2f, .2f), rot); z.mk.Box(Paint(Roofs[0]), c + Vector3.up * (size / 2f + .7f) + dir * .2f, new Vector3(w + 1f, .08f, 1f), rot * Quaternion.Euler(14f, 0, 0));
            z.solid.Box(woodM, c - Vector3.up * .5f, new Vector3(w + .5f, size + 2f, .3f), rot); z.mk.Box(Paint(Cream), c - Vector3.up * .1f + dir * .07f, new Vector3(w, size + .9f, .04f), rot);
            ArtistBits.CoverWall(z.pictures, z.labels, z.ctx, z.a, c + dir * .1f + Vector3.up * .08f, dir, n, size, n, true, Ink);
        }

        /// A paper lantern on a post for each song without its own piece.
        void SongLamps(Plot z)
        {
            if (!SongIds.TryGetValue(z.a.name, out var list)) return;
            var up = Vector3.up; var k = 0; var seen = new HashSet<string>();
            foreach (var (_, id) in list)
            {
                if (z.lit.Contains(id) || !seen.Add(id)) continue;
                var p = z.At(Foot(z.a) * .55f - (k % 2) * 1.3f, Foot(z.a) * .2f + (k / 2) * 1.3f);
                z.mk.Prism(iron, p - up * .1f, up, .03f, 1.3f, 5); z.mk.Blob(SongGlow(id, Lamp, 1.6f), p + up * 1.4f, new Vector3(.14f, .18f, .14f), new System.Random(k), 0f); ArtistBits.PostCollider(z.root, p, 1.3f, .08f);
                k++;
            }
        }

        /// A porch of their own: a wall with one big lit window, a little roof over a wooden deck, a hanging lamp, a chair, a plant, usually a cat.
        PlaceFrame Porch(Plot z)
        {
            var up = Vector3.up; var hue = z.a.name.Length + z.a.name[0]; var rng = new System.Random(hue); var f = new PlaceFrame(z.At(-Foot(z.a) * .5f, 0), z.front); var wall = Paint(Walls[hue % Walls.Length]);
            z.mk.Box(wall, f.P(0, 1.4f, 0), new Vector3(5.6f, 3.6f, .4f), f.r); z.solid.Box(wall, f.P(0, 1.4f, 0), new Vector3(5.6f, 3.6f, .4f), f.r); z.mk.Box(Paint(Roofs[hue % Roofs.Length]), f.P(0, 3.1f, .8f), new Vector3(6.2f, .1f, 2.4f), f.r * Quaternion.Euler(12f, 0, 0));
            z.mk.Box(white, f.P(.6f, 1.7f, .21f), new Vector3(2.7f, 1.8f, .04f), f.r); z.mk.Box(windowM, f.P(.6f, 1.7f, .25f), new Vector3(2.4f, 1.5f, .04f), f.r); z.mk.Box(curtain, f.P(-.36f, 1.7f, .28f), new Vector3(.5f, 1.5f, .02f), f.r); z.mk.Box(white, f.P(.6f, 1.7f, .29f), new Vector3(.05f, 1.5f, .03f), f.r); halos.Add(f.P(.6f, 1.7f, .8f), 2f, Lamp, .3f);
            z.mk.Box(dark, f.P(1.2f, 1.14f, .29f), new Vector3(.3f, .36f, .02f), f.r); z.mk.Box(dark, f.P(1.2f, 1.4f, .29f), new Vector3(.5f, .2f, .02f), f.r);
            z.mk.Box(woodM, f.P(0, .1f, 1f), new Vector3(5.6f, .14f, 1.8f), f.r); z.solid.Box(woodM, f.P(0, -.1f, 1f), new Vector3(5.6f, .5f, 1.8f), f.r); foreach (var e in new[] { -2.7f, 2.7f }) { z.mk.Box(woodM, f.P(e, 1.5f, 1.8f), new Vector3(.1f, 2.9f, .1f), f.r); ArtistBits.PostCollider(z.root, f.P(e, 0, 1.8f), 2.8f, .08f); }
            z.mk.Prism(dark, f.P(-1.6f, 2.5f, 1.2f), up, .006f, .5f, 3); z.mk.Blob(lampM, f.P(-1.6f, 2.4f, 1.2f), new Vector3(.12f, .16f, .12f), rng, 0f); halos.Add(f.P(-1.6f, 2.4f, 1.2f), 1.1f, Lamp, .5f);
            Chair(z.mk, f.P(-1.9f, .17f, .9f), (f.Fwd + f.Right * .3f).normalized); PotPlant(z.root, z.mk, f.P(2.2f, .17f, .7f), 1f, hue);
            if (hue % 3 != 0) Cat(z.mk, hue % 2 == 0 ? f.P(1.3f, .17f, 1.3f) : f.P(-1f, .17f, 1.4f), f.Fwd, hue % 2 == 0, true);
            return f;
        }

        // ---------------------------------------------------------------- Idealism: a deck in the rain
        void Idealism(Plot z)
        {
            var up = Vector3.up; var rot = z.Rot; var rng = new System.Random(2016); var ds = Song(z, "dont say a word"); var mg = Song(z, "monster gambling"); var wa = Song(z, "waon to ame"); life.ameSong = wa;
            // the deck: boards on short posts, four corner posts, a wide low roof; open on every side
            var f = new PlaceFrame(z.At(-3.4f, 0) + up * .1f, z.front); const float W = 9f, Dp = 7f; z.mk.Box(woodM, f.P(0, .26f, 0), new Vector3(W, .16f, Dp), f.r); z.solid.Box(woodM, f.P(0, .04f, 0), new Vector3(W, .6f, Dp), f.r); z.mk.Box(woodM, f.P(0, .1f, Dp / 2f + .3f), new Vector3(2.4f, .16f, .6f), f.r); z.solid.Box(woodM, f.P(0, -.1f, Dp / 2f + .3f), new Vector3(2.4f, .4f, .6f), f.r);
            foreach (var (x, zz) in new[] { (-1f, -1f), (1f, -1f), (1f, 1f), (-1f, 1f) }) { var p = f.P(x * (W / 2f - .2f), .3f, zz * (Dp / 2f - .2f)); z.mk.Box(woodM, p + up * 1.5f, new Vector3(.16f, 3f, .16f), f.r); ArtistBits.PostCollider(z.root, p, 3f, .12f); }
            z.mk.GableRoof(Paint(Roofs[0]), f.P(0, 3.3f, 0), new Vector3(Dp + 1.6f, 0, W + 1.6f), 1.3f, Quaternion.LookRotation(f.Right)); IndoorSpaces.Add(f.P(0, 1.8f, 0), f.r, new Vector3(W, 3f, Dp));
            z.area.glowLight = ArtistBits.PointLight(z.root, f.P(0, 2.6f, 0), new Color(1f, .8f, .54f), 16f, 2.6f, true); for (var k = 0; k < 3; k++) { var lp = f.P(-3f + k * 3f, 3f, Dp / 2f - .3f); z.mk.Prism(dark, lp, -up, .006f, .4f, 3); z.mk.Blob(lampM, lp - up * .56f, new Vector3(.16f, .2f, .16f), rng, 0f); halos.Add(lp - up * .56f, 1.2f, Lamp, .5f); }
            // the low table with a blanket over it, cushions round it, tea
            var tb = f.P(-1.6f, .34f, -.4f); z.mk.Box(quilt, tb + up * .2f, new Vector3(1.9f, .36f, 1.9f), f.r); z.mk.Box(woodM, tb + up * .42f, new Vector3(1.3f, .06f, 1.3f), f.r); BoxCol(z.root, tb + up * .22f, new Vector3(1.3f, .44f, 1.3f), f.r); z.mk.Prism(Paint(Sage), tb + up * .45f + f.Right * .2f, up, .09f, .12f, 8, 1.2f); z.mk.Prism(white, tb + up * .45f - f.Right * .2f + f.Fwd * .2f, up, .04f, .06f, 6); life.smokes.Add(tb + up * .64f + f.Right * .2f);
            for (var k = 0; k < 4; k++) { var d = f.r * Quaternion.Euler(0, k * 90f, 0) * Vector3.forward; var cp = tb + d * 1.3f; z.mk.Prism(Paint(new[] { Mauve, Sage, RainBlue, new Color(.86f, .6f, .5f) }[k]), cp, up, .36f, .14f, 10, .86f); Interact.Seat(tlife.transform, cp + up * .2f, -d, "Sit at the table").seatHeight = .2f; } Cat(z.mk, tb + up * .46f - f.Right * .3f - f.Fwd * .3f, f.Fwd, true, true);
            Folk(tb + f.r * Quaternion.Euler(0, 270f, 0) * Vector3.forward * 1.3f + up * .14f, f.Right, Resident.Pose.Sit, new[] { "Shh. Not because you're loud. It's just nice.", "Feet under the blanket. That's the whole invention.", "I came to wait out the rain. That was a while ago now." }, Local(Residents.Tool.Mug), null, .2f);
            // "waon to ame": a rain chain into a bowl; chimes
            var rc = f.P(W / 2f + .5f, 0, Dp / 2f + .2f); var cm = SongGlow(wa, RainBlue, 1.4f); for (var k = 0; k < 9; k++) z.mk.Prism(k % 2 == 0 ? cm : iron, rc + up * (3.2f - k * .32f), -up, .05f, .22f, 6, .5f); z.mk.Prism(Paint(new Color(.4f, .44f, .5f)), rc + up * .14f, up, .34f, .2f, 10, 1.3f); z.mk.Prism(puddleM, rc + up * .32f, up, .4f, .012f, 10); ArtistBits.PostCollider(z.root, rc, .5f, .4f); halos.Add(rc + up * 1.6f, 1.2f, RainBlue, .3f);
            for (var k = 0; k < 5; k++) { var hp = f.P(W / 2f - .4f - k * .22f, 3.1f, -Dp / 2f + .5f); var len = .5f + (k % 3) * .16f; z.mk.Prism(dark, hp, -up, .004f, .3f, 3); z.mk.Prism(SongGlow(wa, new Color(.8f, .9f, 1f), .9f), hp - up * .3f, -up, .02f, len, 5); }
            // "monster gambling in tokyo": a small glowing machine with a lever, and a little monster sitting on it
            var gm = f.P(2.6f, .34f, -2.2f); var gl = SongGlow(mg, Pink, 1.4f); z.mk.Box(Paint(new Color(.8f, .3f, .4f)), gm + up * .8f, new Vector3(.8f, 1.6f, .6f), f.r); z.mk.Box(gl, gm + up * 1.1f + f.Fwd * .31f, new Vector3(.6f, .5f, .02f), f.r); for (var k = 0; k < 3; k++) z.mk.Box(white, gm + up * 1.1f + f.Fwd * .33f + f.Right * (-.19f + k * .19f), new Vector3(.15f, .3f, .01f), f.r); z.mk.Prism(iron, gm + up * .9f + f.Right * .42f, (up + f.Right * .3f).normalized, .02f, .5f, 4); z.mk.Blob(GlowM(new Color(1f, .3f, .3f), 1f), gm + up * .9f + f.Right * .42f + (up + f.Right * .3f).normalized * .5f, Vector3.one * .06f, rng, 0f); BoxCol(z.root, gm + up * .8f, new Vector3(.8f, 1.6f, .6f), f.r);
            var mo = gm + up * 1.6f; var mm = Paint(new Color(.5f, .8f, .6f)); z.mk.Blob(mm, mo + up * .16f, new Vector3(.18f, .16f, .16f), rng, 0f); foreach (var e in new[] { -1f, 1f }) { z.mk.Cone(mm, mo + up * .28f + f.Right * e * .1f, .04f, .1f, 4); z.mk.Blob(white, mo + up * .2f + f.Fwd * .14f + f.Right * e * .06f, Vector3.one * .035f, rng, 0f); z.mk.Blob(dark, mo + up * .2f + f.Fwd * .17f + f.Right * e * .06f, Vector3.one * .016f, rng, 0f); }
            string[] pull = { "Clunk. Whirr. Three little shapes, none matching. The monster looks pleased.", "You win one token. The machine takes it back with a polite noise.", "Cherry, cherry, monster. The monster shrugs.", "Jackpot: a small tune and no prize at all. It's enough." }; var pn = 0; Spot("The little machine", gm + up * 1.2f + f.Fwd * .5f, "Pull the lever", new Vector3(.9f, 1f, .8f)).onUse = _ => { life.played = Time.time; Say1(gm + up * 2.6f, pull[pn++ % pull.Length]); };
            // "dont say a word": a reading corner, a sign that says nothing at all
            var rd = f.P(-3.6f, .34f, 2.2f); Chair(z.mk, rd, (f.Right + f.Fwd * .3f).normalized, "Sit and read"); for (var k = 0; k < 6; k++) z.mk.Box(Paint(Color.HSVToRGB(k / 7f, .4f, .7f)), rd + f.Fwd * .7f + up * (.03f + k * .05f), new Vector3(.3f, .05f, .22f), f.r * Quaternion.Euler(0, k * 13f, 0)); var sg = f.P(-3.9f, 2.3f, 3.2f); z.mk.Prism(dark, sg + up * .5f, -up, .005f, .5f, 3); z.mk.Box(SongGlow(ds, Cream, .5f), sg - up * .2f, new Vector3(.9f, .4f, .04f), f.r); ArtistBits.Label(z.labels, ". . .", sg - up * .16f + f.Fwd * .03f, f.Fwd, .2f, Ink, TextAnchor.MiddleCenter);
            PotPlant(z.root, z.mk, f.P(4f, .34f, 3f), 1.3f, 3); PotPlant(z.root, z.mk, z.At(2.6f, 5.6f), 1.1f, 4);
            Shot(z.ctx, "the deck in the rain", z.At(10f, 4.4f) + up * 2f, f.P(0, 1.6f, 0));
        }

        // ---------------------------------------------------------------- digitalluc
        void Digitalluc(Plot z)
        {
            var up = Vector3.up; var rot = z.Rot; var rng = new System.Random(2019); var un = Song(z, "Underwater"); var dr = Song(z, "Driver"); var sh = Song(z, "Soft Hell"); life.water.Add(un);
            // "Driver": a small car in the rain, headlights on, the door open
            var cp = z.At(-1f, -3.6f) + up * .1f; var cr = rot * Quaternion.Euler(0, 150f, 0); var body = Paint(new Color(.5f, .56f, .7f)); z.mk.Box(body, cp + up * .6f, new Vector3(1.7f, .6f, 3.8f), cr); z.mk.Box(body, cp + up * 1.16f - cr * Vector3.forward * .2f, new Vector3(1.5f, .56f, 1.9f), cr); z.mk.Box(windowM, cp + up * 1.16f - cr * Vector3.forward * .2f, new Vector3(1.54f, .4f, 1.6f), cr);
            foreach (var (x, zz) in new[] { (-.86f, 1.2f), (.86f, 1.2f), (.86f, -1.2f), (-.86f, -1.2f) }) z.mk.Prism(dark, cp + up * .3f + cr * new Vector3(x - Mathf.Sign(x) * .1f, 0, zz), cr * Vector3.right * Mathf.Sign(x), .3f, .2f, 12);
            var hl = SongGlow(dr, new Color(1f, .9f, .6f), 2f); foreach (var e in new[] { -1f, 1f }) { var hp = cp + up * .64f + cr * new Vector3(e * .56f, 0, 1.91f); z.mk.Blob(hl, hp, new Vector3(.14f, .1f, .03f), rng, 0f); halos.Add(hp + cr * Vector3.forward * .3f, 1.3f, new Color(1f, .9f, .6f), .5f); z.mk.Box(GlowM(new Color(1f, .2f, .2f), .9f), cp + up * .64f + cr * new Vector3(e * .56f, 0, -1.91f), new Vector3(.26f, .1f, .02f), cr); }
            z.mk.Box(body, cp + up * .8f + cr * new Vector3(-1.3f, 0, .5f), new Vector3(.06f, .9f, 1f), cr * Quaternion.Euler(0, -60f, 0)); BoxCol(z.root, cp + up * .7f, new Vector3(1.7f, 1.4f, 3.8f), cr); Interact.Seat(tlife.transform, cp + up * .8f + cr * new Vector3(-1f, 0, .1f), cr * Vector3.left, "Sit in the driver's seat").seatHeight = .5f; ArtistBits.PointLight(z.root, cp + up * 1f + cr * Vector3.forward * 3.4f, new Color(1f, .9f, .6f), 9f, 2.4f); Puddle(z.mk, cp + cr * Vector3.forward * 3.2f, 1.2f);
            // "Underwater": a fish tank as tall as you are, glowing
            var tk = z.At(-4.4f, 3.4f) + up * .1f; var tr = rot * Quaternion.Euler(0, -24f, 0); var teal = SongGlow(un, new Color(.2f, .8f, .8f), 1.1f); z.mk.Box(woodM, tk + up * .4f, new Vector3(3.4f, .8f, 1.2f), tr); z.mk.Box(teal, tk + up * 1.8f, new Vector3(3.2f, 2f, 1f), tr); z.mk.Box(iron, tk + up * 2.86f, new Vector3(3.4f, .12f, 1.2f), tr); foreach (var e in new[] { -1.65f, 1.65f }) z.mk.Box(iron, tk + up * 1.8f + tr * Vector3.right * e, new Vector3(.08f, 2f, 1.08f), tr); BoxCol(z.root, tk + up * 1.5f, new Vector3(3.4f, 3f, 1.2f), tr);
            for (var k = 0; k < 7; k++) { var fp = tk + up * (1.1f + (k * 37 % 10) * .14f) + tr * new Vector3(-1.2f + k * .4f, 0, .52f); var fc = GlowM(new[] { new Color(1f, .6f, .2f), new Color(1f, .9f, .4f), new Color(1f, .5f, .6f) }[k % 3], 1.2f); z.mk.Blob(fc, fp, new Vector3(.12f, .07f, .02f), rng, 0f); z.mk.Tri(fc, fp - tr * Vector3.right * .1f, fp - tr * Vector3.right * .22f + up * .07f, fp - tr * Vector3.right * .22f - up * .07f, fp - tr * Vector3.forward); }
            for (var k = 0; k < 5; k++) z.mk.Prism(leafM, tk + up * .82f + tr * new Vector3(-1.3f + k * .65f, 0, .53f), (up + tr * Vector3.right * (k % 2 - .5f) * .3f).normalized, .03f, .5f + (k % 3) * .3f, 3, .2f); halos.Add(tk + up * 1.8f + tr * Vector3.forward * 1f, 3f, new Color(.2f, .8f, .8f), .24f); z.area.glowLight = ArtistBits.PointLight(z.root, tk + up * 1.8f + tr * Vector3.forward * 2.4f, new Color(.4f, .9f, .9f), 16f, 2.4f);
            Bench(z.root, z.mk, tk + tr * Vector3.forward * 2.6f, -(tr * Vector3.forward), 1.8f);
            // "Soft Hell": a heap of pillows under a dim red lamp
            var ph = z.At(3.4f, 5.4f) + up * .1f; var cols = new[] { new Color(.7f, .3f, .36f), new Color(.86f, .6f, .6f), Mauve, new Color(.56f, .2f, .3f) }; for (var k = 0; k < 9; k++) z.mk.Blob(Paint(cols[k % 4]), ph + Quaternion.Euler(0, k * 67f, 0) * Vector3.forward * (.3f + (k % 3) * .4f) + up * (.16f + (2 - k % 3) * .2f), new Vector3(.5f, .2f, .4f), rng, .1f); BoxCol(z.root, ph + up * .3f, new Vector3(2f, .6f, 2f), rot); Interact.Seat(tlife.transform, ph + up * .66f, z.front, "Sink into the pillows").seatHeight = .3f;
            var rl = ph - z.front * 1.4f; z.mk.Prism(iron, rl, up, .03f, 1.9f, 5); z.mk.Prism(SongGlow(sh, new Color(1f, .25f, .25f), 1.3f), rl + up * 1.8f, up, .22f, .3f, 8, .6f); halos.Add(rl + up * 1.9f, 1.6f, new Color(1f, .3f, .3f), .4f); ArtistBits.PostCollider(z.root, rl, 1.8f, .06f); Cat(z.mk, ph + up * .6f + z.side * .6f, z.front, true, true);
            Shot(z.ctx, "the fish tank and the car", z.At(10f, 2f) + up * 2f, z.At(-2.6f, 0) + up * 1.4f);
        }

        // ---------------------------------------------------------------- Michael Lementi: the arcade corner
        void Lementi(Plot z)
        {
            var up = Vector3.up; var rot = z.Rot; var rng = new System.Random(1994); var aq = Song(z, "Aquatic Ambience"); var bb = Song(z, "Big Blue"); life.water.Add(aq); life.blueSong = bb;
            var f = new PlaceFrame(z.At(-3.6f, 0) + up * .1f, z.front); z.mk.Box(Paint(Walls[3]), f.P(0, 1.6f, 0), new Vector3(6f, 3.6f, .4f), f.r); z.solid.Box(woodM, f.P(0, 1.6f, 0), new Vector3(6f, 3.6f, .4f), f.r); z.mk.Box(Paint(Mauve), f.P(0, 3.1f, 1.1f), new Vector3(6.6f, .1f, 2.8f), f.r * Quaternion.Euler(10f, 0, 0)); foreach (var e in new[] { -3.1f, 3.1f }) { z.mk.Box(iron, f.P(e, 1.4f, 2.3f), new Vector3(.08f, 2.9f, .08f), f.r); ArtistBits.PostCollider(z.root, f.P(e, 0, 2.3f), 2.8f, .07f); }
            z.mk.Box(GlowM(Pink, 1.2f), f.P(0, 3.5f, .3f), new Vector3(2.6f, .5f, .06f), f.r); ArtistBits.Label(z.labels, "ARCADE", f.P(0, 3.5f, .35f), f.Fwd, .36f, Ink, TextAnchor.MiddleCenter); IndoorSpaces.Add(f.P(0, 1.5f, 1.2f), f.r, new Vector3(6f, 3f, 2.4f));
            void Cabinet(Vector3 g, Material screen, Color body, string title)
            {
                var b = Paint(body); z.mk.Box(b, g + up * .9f, new Vector3(.8f, 1.8f, .8f), f.r); z.mk.Box(b, g + up * 1.96f - f.Fwd * .1f, new Vector3(.8f, .34f, .6f), f.r); z.mk.Box(screen, g + up * 1.36f + f.Fwd * .41f, new Vector3(.62f, .5f, .02f), f.r * Quaternion.Euler(-8f, 0, 0)); z.mk.Box(dark, g + up * .98f + f.Fwd * .5f, new Vector3(.76f, .06f, .3f), f.r * Quaternion.Euler(12f, 0, 0)); z.mk.Prism(iron, g + up * 1.02f + f.Fwd * .5f - f.Right * .16f, up, .012f, .1f, 4); z.mk.Blob(GlowM(new Color(1f, .3f, .3f), 1f), g + up * 1.14f + f.Fwd * .5f - f.Right * .16f, Vector3.one * .03f, rng, 0f); foreach (var q in new[] { .06f, .18f }) z.mk.Prism(GlowM(new Color(1f, .9f, .4f), .9f), g + up * 1.02f + f.Fwd * .5f + f.Right * q, up, .025f, .016f, 6);
                z.mk.Box(screen, g + up * 1.96f + f.Fwd * .21f, new Vector3(.7f, .22f, .02f), f.r); ArtistBits.Label(z.labels, title, g + up * 1.96f + f.Fwd * .23f, f.Fwd, .07f, Ink, TextAnchor.MiddleCenter); BoxCol(z.root, g + up * 1f, new Vector3(.8f, 2f, .9f), f.r);
                var st = g + f.Fwd * 1.1f; z.mk.Prism(Paint(Mauve), st + up * .5f, up, .18f, .06f, 8); z.mk.Prism(iron, st, up, .03f, .5f, 4); Interact.Seat(tlife.transform, st + up * .58f, -f.Fwd, "Sit at the cabinet").seatHeight = .54f;
            }
            Cabinet(f.P(-1.5f, 0, .7f), SongGlow(aq, new Color(.2f, .8f, .7f), 1.4f), new Color(.2f, .36f, .4f), "DEEP WATER"); Cabinet(f.P(0f, 0, .7f), SongGlow(bb, new Color(.25f, .5f, 1f), 1.6f), new Color(.2f, .26f, .5f), "BLUE CIRCUIT"); Vending(z.ctx, z.mk, f.P(2f, 0, .7f), f.Fwd, 4, false);
            z.area.glowLight = ArtistBits.PointLight(z.root, f.P(0, 2.4f, 1.4f), new Color(.6f, .8f, 1f), 14f, 2.6f, true); halos.Add(f.P(-.7f, 1.4f, 1.4f), 2.2f, new Color(.3f, .7f, 1f), .2f);
            string[] play = { "The water level. You forget the timer exists. Everybody does.", "A coin, a countdown, a blue car going far too fast round a track in the sky.", "GAME OVER. The music's so nice you put another coin in just to hear it start." }; var pn = 0; Spot("The cabinets", f.P(-.7f, 1.3f, 1.3f), "Play a game", new Vector3(2.4f, 1f, .8f)).onUse = _ => { life.played = Time.time; Say1(f.P(-.7f, 2.9f, 1.2f), play[pn++ % play.Length]); };
            Cat(z.mk, f.P(-1.5f, 2.13f, .6f), f.Fwd, true, false);
            Shot(z.ctx, "the arcade corner", z.At(8f, 2.6f) + up * 1.9f, f.P(0, 1.6f, 0));
        }

        // ---------------------------------------------------------------- everyone else: one thing from their song
        void Others(Plot z, PlaceFrame f)
        {
            var up = Vector3.up; var rot = z.Rot; var rng = new System.Random(z.a.name.Length * 7 + z.a.name[0]); var c = z.At(1.6f, 0) + up * .08f; var fr = z.front; var sd = z.side;
            void Table(Vector3 at, float w = .9f) { z.mk.Prism(woodM, at + up * .68f, up, w / 2f, .05f, 10); z.mk.Prism(iron, at, up, .04f, .68f, 5); BoxCol(z.root, at + up * .36f, new Vector3(w, .74f, w), rot); }
            void Post(Vector3 at, float h, float r = .04f) { z.mk.Prism(iron, at - up * .2f, up, r, h + .2f, 5); ArtistBits.PostCollider(z.root, at, h, r + .03f); }
            switch (z.a.name)
            {
                case "Felipe Jara":
                {
                    // "Inside the Castle Walls": an old television on a crate, a console, a castle on the screen, a beanbag
                    var s = Song(z, "Inside the Castle"); var tv = c - fr * .6f; z.mk.Box(woodM, tv + up * .3f, new Vector3(1f, .6f, .7f), rot); z.mk.Box(Paint(new Color(.36f, .34f, .36f)), tv + up * 1f, new Vector3(1f, .8f, .8f), rot); var scr = SongGlow(s, new Color(.5f, .8f, 1f), 1.5f); z.mk.Box(scr, tv + up * 1.02f + fr * .41f, new Vector3(.76f, .58f, .02f), rot); foreach (var e in new[] { -.2f, 0f, .2f }) z.mk.Box(Paint(Cream), tv + up * (.92f + (e == 0f ? .1f : 0f)) + fr * .43f + sd * e, new Vector3(.14f, .3f + (e == 0f ? .2f : 0f), .01f), rot); z.mk.Box(Paint(new Color(.4f, .7f, .4f)), tv + up * .78f + fr * .43f, new Vector3(.76f, .08f, .01f), rot); z.mk.Cone(GlowM(new Color(1f, .3f, .3f), 1f), tv + up * 1.22f + fr * .43f, .05f, .08f, 4);
                    z.mk.Prism(iron, tv + up * 1.4f, (up + sd * .4f).normalized, .008f, .5f, 3); z.mk.Prism(iron, tv + up * 1.4f, (up - sd * .4f).normalized, .008f, .5f, 3); BoxCol(z.root, tv + up * .7f, new Vector3(1f, 1.4f, .8f), rot); z.mk.Box(Paint(new Color(.3f, .3f, .34f)), tv + fr * .8f + up * .05f, new Vector3(.4f, .08f, .3f), rot); z.mk.Prism(dark, tv + fr * .9f + up * .06f, (fr + sd * .3f).normalized, .008f, .8f, 3);
                    var bg = tv + fr * 2.1f; z.mk.Blob(Paint(new Color(.7f, .3f, .36f)), bg + up * .26f, new Vector3(.6f, .3f, .6f), rng, .1f); BoxCol(z.root, bg + up * .2f, new Vector3(1f, .4f, 1f), rot); Interact.Seat(tlife.transform, bg + up * .5f, -fr, "Sit on the beanbag").seatHeight = .3f; halos.Add(tv + up * 1f + fr * .8f, 1.6f, new Color(.5f, .8f, 1f), .3f);
                    break;
                }
                case "Alecs DeLarge":
                {
                    var s = Song(z, "North Street"); Post(c, 3f, .05f); z.mk.Box(SongGlow(s, new Color(.3f, .6f, .4f), .8f), c + up * 2.8f + sd * .7f, new Vector3(1.6f, .36f, .05f), rot); ArtistBits.Label(z.labels, "NORTH ST", c + up * 2.8f + sd * .7f + fr * .04f, fr, .2f, Cream, TextAnchor.MiddleCenter); z.mk.Box(Paint(new Color(.3f, .6f, .4f)), c + up * 2.4f - fr * .5f, new Vector3(.05f, .3f, 1.2f), rot); z.mk.Tri(Paint(Cream), c + up * 3.3f, c + up * 3.1f - sd * .16f, c + up * 3.1f + sd * .16f, c - fr * 3f); ArtistBits.Label(z.labels, "N", c + up * 3.5f, fr, .16f, Cream, TextAnchor.MiddleCenter);
                    break;
                }
                case "Bonus Points":
                {
                    var s = Song(z, "Cup of Joe"); life.joeSong = s; z.mk.Prism(white, c, up, .5f, 1f, 14, 1.3f); z.mk.Prism(SongGlow(s, new Color(.6f, .36f, .2f), .8f), c + up * .96f, up, .58f, .03f, 14); for (var k = 0; k < 5; k++) { var a = (-60f + k * 30f) * Mathf.Deg2Rad; z.mk.Blob(white, c + up * (.5f + Mathf.Sin(a) * .3f) + sd * (.66f + Mathf.Cos(a) * .24f), Vector3.one * .07f, rng, 0f); } z.mk.Prism(white, c - up * .02f, up, .8f, .06f, 14); ArtistBits.PostCollider(z.root, c, 1f, .64f); life.smokes.Add(c + up * 1.3f); halos.Add(c + up * 1.1f, 1.2f, Lamp, .3f);
                    break;
                }
                case "Doctor Do":
                {
                    var s = Song(z, "the world is small"); Table(c, .7f); z.mk.Prism(woodM, c + up * .73f, up, .1f, .04f, 8); z.mk.Prism(iron, c + up * .77f, up, .012f, .12f, 4); var gl = SongGlow(s, new Color(.4f, .7f, 1f), 1.2f); z.mk.Blob(gl, c + up * 1.06f, Vector3.one * .18f, rng, 0f); for (var k = 0; k < 6; k++) z.mk.Blob(Paint(new Color(.4f, .7f, .4f)), c + up * 1.06f + Quaternion.Euler(k * 50f, k * 77f, 0) * Vector3.forward * .15f, new Vector3(.07f, .05f, .07f), rng, .3f); halos.Add(c + up * 1.06f, .9f, new Color(.4f, .7f, 1f), .4f);
                    break;
                }
                case "Jinsang":
                {
                    var s = Song(z, "Genesis"); life.genesisSong = s; z.mk.Prism(Paint(new Color(.4f, .32f, .28f)), c, up, .7f, .2f, 12); var gl = SongGlow(s, new Color(.6f, 1f, .6f), 1.5f); z.mk.Prism(gl, c + up * .2f, (up + sd * .1f).normalized, .02f, .4f, 4); foreach (var e in new[] { -1f, 1f }) z.mk.Blob(gl, c + up * .6f + sd * (.04f + e * .09f), new Vector3(.09f, .03f, .05f), rng, 0f); for (var k = 0; k < 8; k++) z.mk.Prism(iron, c + Quaternion.Euler(0, k * 45f, 0) * Vector3.forward * .6f + up * .2f, (up * .8f - Quaternion.Euler(0, k * 45f, 0) * Vector3.forward * .6f).normalized, .008f, 1f, 3); halos.Add(c + up * .6f, 1f, new Color(.6f, 1f, .6f), .5f); BoxCol(z.root, c + up * .5f, new Vector3(1.3f, 1f, 1.3f), rot);
                    break;
                }
                case "Jonny Abbey":
                {
                    var s = Song(z, "The Superb Lyrebird"); Post(c, 1.3f, .04f); z.mk.Prism(woodM, c + up * 1.3f - sd * .4f, sd, .03f, .8f, 5); var b = c + up * 1.5f; var br = Paint(new Color(.5f, .4f, .34f)); z.mk.Blob(br, b, new Vector3(.12f, .13f, .2f), rng, 0f); z.mk.Blob(br, b + up * .18f + fr * .16f, Vector3.one * .07f, rng, 0f); z.mk.Cone(Paint(Lamp), b + up * .16f + fr * .22f, .02f, .01f, 4); z.mk.Prism(Paint(Lamp), b + up * .18f + fr * .22f, fr, .012f, .07f, 3);
                    var tl = SongGlow(s, new Color(.9f, .8f, 1f), 1f); for (var k = 0; k < 9; k++) { var a = Quaternion.AngleAxis(-60f + k * 15f, fr); z.mk.Prism(k == 0 || k == 8 ? Paint(new Color(.4f, .3f, .3f)) : tl, b - fr * .16f, (a * up + -fr * .3f).normalized, .012f, .7f + (k == 0 || k == 8 ? .1f : 0f), 3); } halos.Add(b + up * .5f - fr * .2f, 1f, new Color(.9f, .8f, 1f), .3f);
                    break;
                }
                case "Sleepdealer":
                {
                    var s = Song(z, "Weed"); z.mk.Prism(Paint(new Color(.7f, .46f, .36f)), c, up, .3f, .44f, 8, 1.25f); for (var k = 0; k < 7; k++) { var d = (up + Quaternion.Euler(0, k * 51f, 0) * Vector3.forward * .5f).normalized; z.mk.Prism(leafM, c + up * .4f, d, .02f, .8f, 3); for (var q = 0; q < 5; q++) z.mk.Prism(leafM, c + up * .4f + d * .8f, (Quaternion.AngleAxis(-50f + q * 25f, Vector3.Cross(d, up)) * d).normalized, .03f, .3f, 3, .1f); } Post(c - fr * .7f, 2f); z.mk.Prism(iron, c - fr * .7f + up * 2f, fr, .02f, .7f, 4); z.mk.Prism(SongGlow(s, new Color(.8f, .4f, 1f), 1.4f), c + up * 1.96f, -up, .2f, .06f, 8); halos.Add(c + up * 1.6f, 1.2f, new Color(.8f, .4f, 1f), .4f); ArtistBits.PostCollider(z.root, c, .5f, .34f);
                    break;
                }
                case "Stan Forebee":
                {
                    var s = Song(z, "Awakening of the Flowers"); life.flowers.Add(s); z.mk.Box(woodM, c + up * .16f, new Vector3(3.4f, .32f, 1.4f), rot); z.mk.Box(Paint(new Color(.3f, .26f, .26f)), c + up * .33f, new Vector3(3.2f, .02f, 1.2f), rot); BoxCol(z.root, c + up * .2f, new Vector3(3.4f, .4f, 1.4f), rot); var cols = new[] { Pink, new Color(1f, .9f, .5f), new Color(.8f, .7f, 1f), new Color(1f, .7f, .5f) };
                    for (var k = 0; k < 12; k++) { var fp = c + sd * (-1.4f + (k % 6) * .56f) + fr * ((k / 6) * .5f - .25f) + up * .34f; var h = .2f + (k * 7 % 5) * .1f; var open = (k * 7 % 5) / 4f; z.mk.Prism(leafM, fp, up, .015f, h, 3); var pm = SongGlow(s, cols[k % 4], .9f); for (var q = 0; q < 5; q++) { var pd = Quaternion.Euler(0, q * 72f, 0) * Vector3.forward; z.mk.Blob(pm, fp + up * (h + .03f) + pd * (.03f + open * .06f), new Vector3(.05f, .04f, .05f), rng, 0f); } z.mk.Blob(Paint(Lamp), fp + up * (h + .04f), Vector3.one * .025f, rng, 0f); } halos.Add(c + up * .7f, 1.8f, Pink, .2f);
                    break;
                }
                case "TOKiMONSTA":
                {
                    var s = Song(z, "Darkest"); life.dimSong = s; Post(c, 1.8f, .03f); z.mk.Prism(iron, c, up, .2f, .03f, 10); z.mk.Prism(SongGlow(s, new Color(.6f, .4f, .9f), .6f), c + up * 1.7f, up, .3f, .4f, 10, .6f); halos.Add(c + up * 1.8f, 1.2f, new Color(.6f, .4f, .9f), .3f); var dm = c + sd * .9f; Post(dm, 1f, .03f); z.mk.Box(white, dm + up * 1.1f, new Vector3(.2f, .3f, .06f), rot); z.mk.Prism(dark, dm + up * 1.1f + fr * .03f, fr, .06f, .03f, 10); z.mk.Box(white, dm + up * 1.14f + fr * .065f - sd * .02f, new Vector3(.015f, .05f, .01f), rot * Quaternion.Euler(0, 0, 40f)); ArtistBits.Label(z.labels, "dim", dm + up * 1.3f + fr * .04f, fr, .05f, Ink, TextAnchor.MiddleCenter);
                    break;
                }
                case "The Deli":
                {
                    var s = Song(z, "Flowers"); life.flowers.Add(s); z.mk.Box(woodM, c + up * .4f, new Vector3(2.6f, .8f, .8f), rot); BoxCol(z.root, c + up * .4f, new Vector3(2.6f, .8f, .8f), rot); foreach (var e in new[] { -1.3f, 1.3f }) z.mk.Box(woodM, c + sd * e - fr * .3f + up * 1.3f, new Vector3(.06f, 2.6f, .06f), rot); for (var k = 0; k < 5; k++) z.mk.Box(k % 2 == 0 ? Paint(Cream) : Paint(Sage), c + up * 2.5f + sd * (-1.04f + k * .52f) + fr * .2f, new Vector3(.52f, .05f, 1.4f), rot * Quaternion.Euler(12f, 0, 0)); var cols = new[] { Pink, new Color(1f, .9f, .5f), new Color(.8f, .7f, 1f), new Color(1f, .6f, .4f) };
                    for (var k = 0; k < 4; k++) { var bp = c + up * .8f + sd * (-.9f + k * .6f); z.mk.Prism(Paint(new Color(.5f, .54f, .6f)), bp, up, .14f, .3f, 8, 1.2f); var pm = SongGlow(s, cols[k], .8f); for (var q = 0; q < 6; q++) { var d = (up + Quaternion.Euler(0, q * 60f, 0) * Vector3.forward * .3f).normalized; z.mk.Prism(leafM, bp + up * .26f, d, .01f, .4f, 3); z.mk.Blob(pm, bp + up * .26f + d * .42f, Vector3.one * .06f, rng, .2f); } }
                    var bq = new MeshKit(); bq.Prism(Paint(Cream), Vector3.zero, Vector3.up, .03f, .26f, 6, 2.4f); for (var q = 0; q < 5; q++) bq.Blob(Paint(cols[q % 4]), new Vector3(Mathf.Cos(q * 1.26f) * .05f, .3f, Mathf.Sin(q * 1.26f) * .05f), Vector3.one * .045f, rng, .2f); Thing(bq, "Flowers", "a bunch of flowers, wrapped in paper", c + up * .82f + sd * 1.1f + fr * .1f, Quaternion.identity, new Vector3(.2f, .4f, .2f), .2f, false);
                    break;
                }
                case "letherette":
                {
                    var s = Song(z, "Rich & Dan"); Table(c, 1f); foreach (var e in new[] { -1f, 1f }) { Chair(z.mk, c + sd * e * .95f, -sd * e); var mp = c + up * .73f + sd * e * .22f; z.mk.Prism(SongGlow(s, e < 0 ? new Color(1f, .7f, .4f) : new Color(.5f, .7f, 1f), .7f), mp, up, .06f, .11f, 8); ArtistBits.Label(z.labels, e < 0 ? "RICH" : "DAN", mp + up * .06f + fr * .065f, fr, .035f, Ink, TextAnchor.MiddleCenter); life.smokes.Add(mp + up * .2f); }
                    break;
                }
                case "o k h o":
                {
                    var s = Song(z, "41058"); Post(c, 1.1f, .04f); z.mk.Box(Paint(new Color(.7f, .3f, .3f)), c + up * 1.3f, new Vector3(.4f, .4f, .6f), rot); z.mk.Box(dark, c + up * 1.36f + fr * .31f, new Vector3(.28f, .04f, .01f), rot); z.mk.Box(SongGlow(s, Cream, .6f), c + up * 1.22f + fr * .31f, new Vector3(.3f, .12f, .01f), rot); ArtistBits.Label(z.labels, "41058", c + up * 1.22f + fr * .32f, fr, .06f, Ink, TextAnchor.MiddleCenter); z.mk.Box(redFlag(), c + up * 1.5f + sd * .22f, new Vector3(.02f, .2f, .12f), rot);
                    var lt = new MeshKit(); lt.Box(Paint(Cream), new Vector3(0, .01f, 0), new Vector3(.22f, .015f, .14f), Quaternion.identity); lt.Box(Paint(new Color(.7f, .3f, .3f)), new Vector3(.07f, .02f, .03f), new Vector3(.04f, .005f, .05f), Quaternion.identity); Thing(lt, "Letter", "a letter with no return address", c + fr * .7f + up * .02f, rot * Quaternion.Euler(0, 20f, 0), new Vector3(.24f, .05f, .16f), .1f, false);
                    break;
                }
                case "quickly, quickly":
                {
                    var s = Song(z, "Getsomerest"); life.restSong = s; var br = rot * Quaternion.Euler(0, 90f, 0); z.mk.Box(woodM, c + up * .22f, new Vector3(1.3f, .44f, 2.4f), br); z.mk.Box(white, c + up * .5f, new Vector3(1.2f, .14f, 2.3f), br); z.mk.Box(quilt, c + up * .56f + br * Vector3.forward * .3f, new Vector3(1.26f, .14f, 1.6f), br); z.mk.Box(white, c + up * .64f - br * Vector3.forward * .85f, new Vector3(.8f, .14f, .4f), br); z.mk.Box(woodM, c + up * .6f - br * Vector3.forward * 1.22f, new Vector3(1.3f, 1.2f, .08f), br); BoxCol(z.root, c + up * .36f, new Vector3(1.3f, .72f, 2.4f), br); Interact.Seat(tlife.transform, c + up * .7f + fr * .4f, fr, "Get some rest").seatHeight = .64f;
                    var ms = SongGlow(s, new Color(.8f, .86f, 1f), 1.3f); Post(c - fr * 1.2f - sd * 1.6f, 2.6f, .03f); var mp = c - fr * 1.2f - sd * 1.6f + up * 3f; z.mk.Prism(ms, mp - fr * .04f, fr, .34f, .08f, 14); z.mk.Prism(Paint(new Color(.2f, .22f, .34f)), mp - fr * .05f + sd * .16f + up * .06f, fr, .28f, .1f, 12); for (var k = 0; k < 3; k++) ArtistBits.Label(z.labels, "z", c + up * (1.2f + k * .36f) - sd * (.8f - k * .2f), fr, .12f + k * .05f, Cream, TextAnchor.MiddleCenter); Cat(z.mk, c + up * .64f + br * Vector3.forward * .6f, fr, true, true); halos.Add(mp, 1.2f, new Color(.8f, .86f, 1f), .4f);
                    break;
                }
                case "tides.":
                {
                    var s = Song(z, "tuscano"); for (var k = 0; k < 3; k++) { var p = c + sd * (-1.6f + k * 1.6f) - fr * (k == 1 ? .5f : 0f); z.mk.Prism(Paint(new Color(.74f, .46f, .34f)), p, up, .3f, .4f, 8, 1.2f); z.mk.Prism(trunkM, p + up * .4f, up, .05f, .4f, 4); z.mk.Cone(leafDark, p + up * .7f, .32f, 2.2f + (k == 1 ? .7f : 0f), 7); ArtistBits.PostCollider(z.root, p, 2f, .34f); } var pc = c + fr * 1f; Post(pc, .9f, .02f); z.mk.Box(SongGlow(s, new Color(1f, .8f, .5f), .8f), pc + up * 1.1f, new Vector3(.5f, .34f, .02f), rot); z.mk.Box(Paint(new Color(.5f, .7f, .9f)), pc + up * 1.16f + fr * .012f, new Vector3(.44f, .16f, .01f), rot); z.mk.Prism(Paint(new Color(1f, .7f, .3f)), pc + up * 1.14f + fr * .02f + sd * .1f, fr, .05f, .01f, 10);
                    break;
                }
                case "wuudlum":
                {
                    // "Häwelmann": a little bed on wheels with a nightshirt for a sail, and the moon to follow
                    var s = Song(z, "H"); var br = rot * Quaternion.Euler(0, 70f, 0); var al = br * Vector3.forward; z.mk.Box(woodM, c + up * .5f, new Vector3(.8f, .3f, 1.5f), br); z.mk.Box(white, c + up * .68f, new Vector3(.7f, .1f, 1.4f), br); z.mk.Box(quilt, c + up * .72f - al * .2f, new Vector3(.74f, .1f, .9f), br); foreach (var e in new[] { -1f, 1f }) { z.mk.Box(woodM, c + up * .8f + al * e * .75f, new Vector3(.8f, .7f, .06f), br); foreach (var q in new[] { -1f, 1f }) z.mk.Prism(dark, c + up * .2f + al * e * .55f + br * Vector3.right * q * .4f, br * Vector3.right * q, .2f, .06f, 10); } BoxCol(z.root, c + up * .6f, new Vector3(.9f, 1.2f, 1.6f), br);
                    z.mk.Prism(woodM, c + up * .7f, up, .025f, 1.9f, 4); z.mk.Tri(white, c + up * 2.5f, c + up * 1.1f, c + up * 1.2f + al * .9f, c - br * Vector3.right * 3f); z.mk.Tri(white, c + up * 2.5f, c + up * 1.1f, c + up * 1.2f + al * .9f, c + br * Vector3.right * 3f);
                    var mp = c + up * 3.6f + al * 2.2f; var ms = SongGlow(s, new Color(1f, .95f, .7f), 1.5f); Post(c + al * 2.2f, 3.1f, .03f); z.mk.Prism(ms, mp - fr * .05f, fr, .5f, .1f, 16); foreach (var e in new[] { -1f, 1f }) z.mk.Blob(dark, mp + fr * .06f + sd * e * .16f + up * .1f, Vector3.one * .04f, rng, 0f); z.mk.Box(dark, mp + fr * .06f - up * .14f, new Vector3(.26f, .03f, .01f), rot); halos.Add(mp + fr * .2f, 1.8f, new Color(1f, .95f, .7f), .4f);
                    break;
                }
            }
        }

        Material redFlag() => Paint(new Color(.9f, .3f, .3f));
    }
}
