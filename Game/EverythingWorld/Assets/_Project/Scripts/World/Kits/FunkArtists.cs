using System.Collections.Generic;
using UnityEngine;

namespace EverythingWorld.World
{
    /// The Funk Tropics' artists, each in a jungle clearing: a dark pad ringed with lights, an overgrown speaker totem,
    /// covers on a neon-edged board, a plaque, a glowing mushroom for every song without a piece of its own.
    /// - Parliament (beside the Mothership): a giant flashlight propped up at the sky ("Flash Light": beams sweep from
    ///   the ship); a little club whose roof floats above its walls ("Give Up The Funk": the ship lifts off her legs);
    ///   giant star glasses.
    /// - Funkadelic (the other side of the ship): a lone guitar on a mound under one spotlight ("Maggot Brain": the
    ///   whole jungle goes dark but for it); a red-hot neon flame ("Red Hot Mama").
    /// - Prince: a round purple stage, a purple motorcycle; an umbrella ("Purple Rain": it rains purple); a giant
    ///   beret on a hat stand ("Raspberry Beret"); a clock stuck at a minute to midnight ("1999").
    /// - The Meters: parade umbrellas, a bass drum and a snare; a pond with fire on it ("Fire on the Bayou"); a
    ///   feathered drum ("Hey Pocky A-Way"); neon lips ("Just Kissed My Baby").
    /// - James Brown: a stage, a leaning mic stand, the cape on its stand to take ("The Payback": the light goes red);
    ///   a steering wheel on a post ("People Get Up And Drive Your Funky Soul").
    /// - Thundercat: a giant six-string bass with cat ears; a heart-shaped hole of light ("Them Changes"); a durag and
    ///   seven gold stars ("Dragonball Durag"). Commodores: a house of mighty bricks ("Brick House"), a hammock ("Easy").
    /// - The rest: bongos to play, a roulette wheel to spin, an old television, eyes in a bush, and so on (Others).
    public partial class FunkKit
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
                Add("Parliament", "Flash Light", "@the mothership"); Add("Parliament", "Give Up The Funk", "@the mothership"); Add("Prince", "Purple Rain", "Prince");
                Add("Funkadelic", "Maggot Brain", "Funkadelic"); Add("The Meters", "Fire on the Bayou", "The Meters"); Add("James Brown", "The Payback", "@funk street");
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
            if (trunk == null) MakeMaterials();
            var root = new GameObject("Artist: " + a.name).transform; root.SetParent(ctx.Root, false);
            var front = facing.TryGetValue(a.name, out var f) ? f : new Vector3(center.x - a.x, 0, center.z - a.z).normalized;
            if (front.sqrMagnitude < .01f) front = Vector3.forward;
            var labels = new GameObject("Labels").transform; labels.SetParent(root, false);
            var area = root.gameObject.AddComponent<ArtistArea>();
            area.artistName = a.name; area.stationId = a.stationId; area.radius = a.r; area.glowColor = Purple;
            var z = new Plot { ctx = ctx, root = root, labels = labels, mk = new MeshKit(), solid = new MeshKit(), pictures = new MeshKit(), a = a, c = ctx.OnGround(a.x, a.z), front = front, side = Vector3.Cross(Vector3.up, front), area = area };
            var reach = Foot(a); var up = Vector3.up; var hue = (a.name.Length * 3 + a.name[0]) % Neon.Length;
            z.mk.Prism(paveDark, z.c - up * .25f, up, reach * .62f, .33f, 18);
            for (var k = 0; k < 18; k++) { var d = Quaternion.Euler(0, k * 20f, 0) * front; z.mk.Blob(tileM[k % 4], z.c + d * (reach * .62f - .25f) + up * .1f, new Vector3(.14f, .05f, .14f), prng, 0f); }
            switch (a.name)
            {
                case "Parliament": Parliament(z); break;
                case "Funkadelic": Funkadelic(z); break;
                case "Prince": Prince(z); break;
                case "The Meters": Meters(z); break;
                case "James Brown": JamesBrown(z); break;
                case "Thundercat": Thundercat(z); break;
                case "Commodores": Commodores(z); break;
                default: Totem(z, hue); Others(z, hue); break;
            }
            SongLamps(z);
            Board(z, z.At(reach * .6f, -reach * .45f - 1.5f), front, Neon[hue]);
            ArtistBits.Plaque(z.mk, z.solid, labels, a, z.At(reach * .6f + 1.5f, reach * .45f + 1.5f), front, trunk, chrome, Ink, 1.6f);
            FruitLamp(ctx, z.mk, z.At(reach * .5f, reach * .62f), -z.side, false, hue);
            if (z.area.glowLight == null) z.area.glowLight = ArtistBits.PointLight(root, z.c + up * 5f + front * 3f, Color.Lerp(Neon[hue], Color.white, .35f), HasStation(a) ? 24f : 16f, 2f);
            area.glowRenderers = new Renderer[0];
            z.mk.Build("Area", root, false);
            z.solid.BuildCollider("Walls", root);
            z.pictures.Build("Covers", root, false);
            noGrass.Add(new Vector3(z.c.x, reach * .9f, z.c.z));
            if (HasStation(a) || reach >= 10f)
            {
                var mark = new GameObject("Shot: " + a.name + " up close").transform; mark.SetParent(ctx.Root, false);
                var cam = z.At(reach * .8f, reach * .3f) + up * 1.9f; var look = z.At(-reach * .3f, 0) + up * 2.2f - cam; mark.SetPositionAndRotation(cam, Quaternion.LookRotation(look.normalized));
            }
        }

        /// The covers on a dark board edged in neon, lit from above.
        void Board(Plot z, Vector3 ground, Vector3 dir, Color col)
        {
            dir = Vector3.ProjectOnPlane(dir, Vector3.up).normalized; var rot = Quaternion.LookRotation(dir); var side = Vector3.Cross(Vector3.up, dir);
            var n = Mathf.Max(1, Mathf.Min(7, z.a.songList?.Length ?? 1)); const float size = .9f; var w = n * size * 1.25f + .6f;
            var c = ground + Vector3.up * 1.7f; var g = GlowM(col, 2f);
            z.mk.Box(dark, c - Vector3.up * .1f, new Vector3(w, size + .9f, .08f), rot); z.solid.Box(dark, c - Vector3.up * .5f, new Vector3(w, size + 1.6f, .3f), rot);
            foreach (var s in new[] { -1f, 1f }) { z.mk.Prism(trunk, c + side * s * (w / 2f + .1f) - Vector3.up * 1.9f, Vector3.up, .09f, size + 2.4f, 6); z.mk.Box(g, c + side * s * (w / 2f - .04f) - Vector3.up * .1f + dir * .05f, new Vector3(.06f, size + .9f, .04f), rot); }
            z.mk.Box(g, c + Vector3.up * (size / 2f + .32f) + dir * .05f, new Vector3(w, .06f, .04f), rot); z.mk.Box(g, c - Vector3.up * (size / 2f + .52f) + dir * .05f, new Vector3(w, .06f, .04f), rot);
            ArtistBits.PointLight(z.root, c + dir * 1.6f + Vector3.up * .8f, new Color(1f, .95f, .9f), 5f, 1.2f);
            ArtistBits.CoverWall(z.pictures, z.labels, z.ctx, z.a, c + dir * .05f + Vector3.up * .08f, dir, n, size, n, true, Ink);
        }

        /// A glowing mushroom for each song without its own piece.
        void SongLamps(Plot z)
        {
            if (!SongIds.TryGetValue(z.a.name, out var list)) return;
            var up = Vector3.up; var k = 0; var seen = new HashSet<string>();
            foreach (var (_, id) in list)
            {
                if (z.lit.Contains(id) || !seen.Add(id)) continue;
                var p = z.At(Foot(z.a) * .55f - (k % 2) * 1.3f, Foot(z.a) * .2f + (k / 2) * 1.3f); var col = Neon[(k * 2 + z.a.name.Length) % Neon.Length];
                z.mk.Prism(white, p - up * .1f, up, .08f, .9f, 6); z.mk.Prism(SongGlow(id, col, 1.8f), p + up * .8f, up, .42f, .28f, 10, .25f); halos.Add(p + up * .9f, 1.1f, col, .5f); ArtistBits.PostCollider(z.root, p, .9f, .2f);
                k++;
            }
        }

        /// An overgrown speaker totem at the back of the clearing: three cabinets, their cones glowing, leaves on top, a log to sit on.
        void Totem(Plot z, int hue)
        {
            var up = Vector3.up; var g = z.At(-Foot(z.a) * .36f, 0); var rot = z.Rot;
            for (var k = 0; k < 3; k++) { z.mk.Box(dark, g + up * (.7f + k * 1.35f), new Vector3(1.5f - k * .15f, 1.3f, .9f), rot); z.mk.Prism(GlowM(Neon[(hue + k) % Neon.Length], 1.3f), g + up * (.7f + k * 1.35f) + z.front * .46f, z.front, .48f - k * .05f, .03f, 14); z.mk.Prism(dark, g + up * (.7f + k * 1.35f) + z.front * .48f, z.front, .18f, .03f, 10); }
            z.solid.Box(dark, g + up * 2f, new Vector3(1.5f, 4f, .9f), rot); z.mk.Blob(leaf, g + up * 4.2f, new Vector3(1.2f, .4f, .9f), prng, .35f); z.mk.Box(floraM[hue % 3], g + up * 3f + z.side * .8f + z.front * .3f, new Vector3(.05f, 2.2f, .03f), rot); halos.Add(g + up * 2f + z.front * 1f, 2.2f, Neon[hue], .35f);
            var lg = z.At(-Foot(z.a) * .1f, -2.6f); z.mk.Prism(trunk, lg + up * .28f - z.front * 1f, z.front, .28f, 2f, 8); BoxCol(z.root, lg + up * .28f, new Vector3(.56f, .56f, 2f), rot); Interact.Bench(plife.transform, lg + up * .58f - z.front * .6f, lg + up * .58f + z.front * .6f, z.side);
        }

        void Say1(Vector3 at, string text) => PlaceTools.Say(plife.transform, at, text, 4.5f);

        // ---------------------------------------------------------------- Parliament
        void Parliament(Plot z)
        {
            var up = Vector3.up; var rot = z.Rot;
            // "Flash Light": a flashlight the size of a tree trunk, propped up and pointed at the sky
            var fl = Song(z, "Flash Light"); var b = z.At(-2f, -4f); var aim = (up * 1f + z.front * .35f + z.side * .2f).normalized; var body = WorldMaterials.Solid(new Color(.75f, .15f, .2f), .6f);
            z.mk.Prism(body, b + up * .6f, aim, .55f, 3.4f, 12); z.mk.Prism(chrome, b + up * .6f + aim * 3.4f, aim, .56f, .5f, 12, 1.6f); z.mk.Prism(SongGlow(fl, new Color(1f, .95f, .7f), 2.6f), b + up * .6f + aim * 3.9f, aim, .82f, .06f, 14); z.mk.Box(dark, b + up * .6f + aim * 1.6f + z.front * .5f, new Vector3(.3f, .16f, .6f), Quaternion.LookRotation(aim));
            foreach (var e in new[] { -1f, 1f }) z.mk.Prism(trunk, b + z.side * e * .8f - up * .2f, (up + aim * .3f - z.side * e * .25f).normalized, .1f, 2.6f, 5); BoxCol(z.root, b + up * 1.8f + aim * 1.2f, new Vector3(1.6f, 3.6f, 1.6f), rot); halos.Add(b + up * .6f + aim * 4.4f, 3f, Gold, .6f);
            z.area.glowLight = ArtistBits.PointLight(z.root, b + up * .6f + aim * 5f, new Color(1f, .95f, .75f), 24f, 2.4f); life.flashAt = b + up * .6f + aim * 4f; life.flashDir = aim; life.flashSong = fl;
            // "Give Up The Funk": a little club with its roof torn off and floating
            var gu = Song(z, "Give Up The Funk"); var h = z.At(-2f, 5f); var wall = WorldMaterials.Solid(new Color(.3f, .14f, .42f)); var neon = SongGlow(gu, Pink, 1.8f);
            foreach (var (x, zz, sx, szz) in new[] { (0f, -2f, 5f, .25f), (-2.5f, 0f, .25f, 4f), (2.5f, 0f, .25f, 4f) }) { z.mk.Box(wall, h + z.side * x + z.front * zz + up * 1.3f, new Vector3(sx, 2.6f, szz), rot); z.solid.Box(wall, h + z.side * x + z.front * zz + up * 1.3f, new Vector3(sx, 2.6f, szz), rot); }
            z.mk.Box(neon, h + up * 2.62f - z.front * 2f, new Vector3(5f, .06f, .3f), rot); foreach (var e in new[] { -2.5f, 2.5f }) z.mk.Box(neon, h + up * 2.62f + z.side * e, new Vector3(.3f, .06f, 4f), rot);
            var roof = new MeshKit(); roof.Box(wall, Vector3.zero, new Vector3(5.6f, .2f, 4.6f), Quaternion.identity); roof.Box(neon, new Vector3(0, -.12f, 0), new Vector3(5.2f, .04f, 4.2f), Quaternion.identity); var rgo = roof.BuildSingle("The roof", plife.transform, false); rgo.transform.SetPositionAndRotation(h + up * 4.6f, rot * Quaternion.Euler(8f, 12f, -6f)); life.roof = rgo.transform; life.roofHome = h + up * 4.6f; life.roofSong = gu;
            z.mk.Box(tileM[1], h + up * .1f, new Vector3(4.4f, .04f, 3.4f), rot); Interact.Bench(plife.transform, h - z.front * 1.5f - z.side * 1.5f + up * .5f, h - z.front * 1.5f + z.side * 1.5f + up * .5f, z.front); z.mk.Box(dark, h - z.front * 1.5f + up * .25f, new Vector3(4f, .5f, .5f), rot);
            ArtistBits.Label(z.labels, "(we want the funk)", h + up * 1.6f - z.front * 1.86f, z.front, .2f, Ink, TextAnchor.MiddleCenter);
            // star glasses on a stand
            var sg = z.At(4.5f, 0); z.mk.Prism(chrome, sg - up * .2f, up, .04f, 1.9f, 5); foreach (var e in new[] { -1f, 1f }) { for (var k = 0; k < 5; k++) { var d = Quaternion.AngleAxis(k * 72f, z.front) * up; z.mk.Prism(GlowM(Gold, 1.8f), sg + up * 2f + z.side * e * .5f, d, .16f, .46f, 3, .1f); } z.mk.Prism(dark, sg + up * 2f + z.side * e * .5f + z.front * .03f, z.front, .2f, .04f, 10); } z.mk.Box(chrome, sg + up * 2f, new Vector3(.4f, .05f, .05f), rot); ArtistBits.PostCollider(z.root, sg, 1.9f, .08f);
        }

        // ---------------------------------------------------------------- Funkadelic
        void Funkadelic(Plot z)
        {
            var up = Vector3.up; var rot = z.Rot; var mb = Song(z, "Maggot Brain"); var c = z.At(-2.5f, 0);
            // a low mound of earth, one guitar standing on it, one spotlight
            z.mk.Blob(earth, c + up * .2f, new Vector3(3.6f, .7f, 3.6f), prng, .12f); z.solid.Prism(earth, c - up * .2f, up, 2.8f, .7f, 10);
            var gt = c + up * .75f; var gm = new MeshKit(); gm.Prism(chrome, Vector3.zero, Vector3.up, .02f, .35f, 5); gm.Blob(WorldMaterials.Solid(new Color(.92f, .9f, .85f), .6f), new Vector3(0, .7f, 0), new Vector3(.24f, .32f, .06f), prng, .05f); gm.Box(WorldMaterials.Solid(new Color(.5f, .34f, .2f)), new Vector3(0, 1.45f, 0), new Vector3(.07f, 1f, .04f), Quaternion.identity); gm.Box(dark, new Vector3(0, 2.02f, 0), new Vector3(.1f, .2f, .04f), Quaternion.identity);
            var strings = SongGlow(mb, new Color(.9f, .95f, 1f), 1.6f); for (var k = 0; k < 3; k++) gm.Box(strings, new Vector3(-.02f + k * .02f, 1.3f, .03f), new Vector3(.006f, 1.3f, .006f), Quaternion.identity);
            var ggo = gm.BuildSingle("The guitar", plife.transform, false); ggo.transform.SetPositionAndRotation(gt, rot); string[] solo = { "One note, bent until it cries. You don't play the next one for a long time.", "You play like you just heard the worst news there is. The jungle goes quiet to listen.", "Ten minutes later you're still on the same solo and nobody has moved." }; var sn = 0;
            Interact.Toy(ggo, Toy.Kind.Custom, "Play the guitar", new Vector3(.9f, 2.4f, .9f)).onUse = _ => { life.plucked = Time.time; Say1(gt + up * 2.8f, solo[sn++ % solo.Length]); };
            life.maggotLight = ArtistBits.PointLight(z.root, gt + up * 5f + z.front * 1.5f, new Color(.9f, .95f, 1f), 16f, 1.4f); z.area.glowLight = life.maggotLight; life.maggotSong = mb; halos.Add(gt + up * 1.3f, 1.6f, Ink, .4f);
            for (var k = 0; k < 14; k++) { var d = Quaternion.Euler(0, k * 26f, 0) * z.front; var fp = c + d * (3.2f + (k % 3) * .7f); fp.y = z.ctx.Ground(fp.x, fp.z); z.mk.Prism(leaf, fp, up, .015f, .34f, 3); z.mk.Blob(floraM[0], fp + up * .36f, Vector3.one * .06f, prng, 0f); }
            // "Red Hot Mama": a flame of red neon
            var rh = Song(z, "Red Hot Mama"); var fp2 = z.At(3f, 6.5f); var red = SongGlow(rh, new Color(1f, .22f, .15f), 2f); z.mk.Prism(chrome, fp2 - up * .2f, up, .05f, 1.4f, 5); for (var k = 0; k < 9; k++) { var t = k / 8f; var w = Mathf.Sin(t * Mathf.PI) * .7f + .08f; z.mk.Box(red, fp2 + up * (1.4f + t * 2.2f) + z.side * Mathf.Sin(t * 5f) * .18f, new Vector3(w, .2f, .06f), rot); }
            halos.Add(fp2 + up * 2.4f + z.front * .3f, 2.2f, new Color(1f, .3f, .2f), .55f); ArtistBits.PostCollider(z.root, fp2, 1.4f, .08f);
            var lg = z.At(4f, -4f); z.mk.Prism(trunk, lg + up * .28f - z.side * 1.1f, z.side, .28f, 2.2f, 8); BoxCol(z.root, lg + up * .28f, new Vector3(2.2f, .56f, .56f), rot); Interact.Bench(plife.transform, lg + up * .58f - z.side * .7f, lg + up * .58f + z.side * .7f, -z.front);
        }

        // ---------------------------------------------------------------- Prince
        void Prince(Plot z)
        {
            var up = Vector3.up; var rot = z.Rot; var c = z.At(-3f, 0); var purple = WorldMaterials.Solid(new Color(.4f, .14f, .6f), .7f); var pr = Song(z, "Purple Rain");
            // a round purple stage with a mic stand wound in flowers and an umbrella; a purple motorcycle beside it
            z.mk.Prism(purple, c, up, 4.4f, .6f, 16); z.solid.Prism(purple, c - up * .3f, up, 4.4f, .9f, 16); z.mk.Prism(SongGlow(pr, Purple, 1.6f), c + up * .58f, up, 4.46f, .05f, 16);
            for (var k = 0; k < 2; k++) { var st = c + z.front * (4.5f + (1 - k) * .4f) + up * ((k + .5f) * .3f); z.mk.Box(purple, st, new Vector3(2f, .3f, .4f), rot); z.solid.Box(purple, st - up * (k * .15f), new Vector3(2f, .3f * (k + 1), .4f), rot); }
            var mic = c + up * .6f + z.front * 1.6f; z.mk.Prism(white, mic, up, .02f, 1.5f, 5); z.mk.Blob(mirror, mic + up * 1.55f, new Vector3(.07f, .1f, .07f), prng, 0f); for (var k = 0; k < 7; k++) z.mk.Blob(floraM[0], mic + up * (.3f + k * .17f) + Quaternion.Euler(0, k * 80f, 0) * z.front * .07f, Vector3.one * .05f, prng, 0f);
            var um = c + up * .6f - z.side * 1.8f; z.mk.Prism(dark, um, up, .02f, 2.1f, 5); z.mk.Prism(SongGlow(pr, new Color(.6f, .3f, 1f), 1.2f), um + up * 1.9f, up, 1.1f, .42f, 10, .08f); halos.Add(um + up * 2f, 1.8f, Purple, .45f);
            z.area.glowLight = ArtistBits.PointLight(z.root, c + up * 4.4f + z.front * 2f, Purple, 26f, 2.4f); life.princeAt = c + up * 6f; life.rainSong = pr;
            var mo = z.At(1f, -7f); var mrot = Quaternion.LookRotation(z.side); foreach (var e in new[] { -.9f, .9f }) { z.mk.Prism(dark, mo + up * .42f + z.side * e - z.front * .08f, z.front, .42f, .16f, 14); z.mk.Prism(chrome, mo + up * .42f + z.side * e - z.front * .09f, z.front, .2f, .18f, 10); }
            z.mk.Box(purple, mo + up * .75f, new Vector3(.4f, .4f, 1.5f), mrot); z.mk.Box(purple, mo + up * 1f + z.side * .5f, new Vector3(.5f, .3f, .7f), mrot * Quaternion.Euler(-20f, 0, 0)); z.mk.Box(dark, mo + up * .98f - z.side * .3f, new Vector3(.36f, .1f, .7f), mrot); z.mk.Prism(chrome, mo + up * 1.2f + z.side * .75f - z.front * .35f, z.front, .02f, .7f, 5); BoxCol(z.root, mo + up * .7f, new Vector3(.6f, 1.4f, 2.4f), mrot);
            Interact.Seat(plife.transform, mo + up * 1.05f - z.side * .3f, z.side, "Sit on the purple motorcycle").seatHeight = 1.05f;
            // "Raspberry Beret": a giant beret on a hat stand
            var rb = Song(z, "Raspberry Beret"); var hs = z.At(2.5f, 7f); z.mk.Prism(trunk, hs - up * .2f, up, .06f, 2.4f, 6); z.mk.Prism(trunk, hs, up, .3f, .06f, 8); var beret = SongGlow(rb, new Color(.85f, .15f, .4f), 1.1f); z.mk.Blob(beret, hs + up * 2.45f + z.side * .15f, new Vector3(.75f, .22f, .75f), prng, .08f); z.mk.Prism(beret, hs + up * 2.6f + z.side * .15f, up, .03f, .14f, 4); halos.Add(hs + up * 2.5f, 1.4f, Pink, .45f); ArtistBits.PostCollider(z.root, hs, 2.2f, .1f);
            ArtistBits.Label(z.labels, "second hand", hs + up * 1.3f + z.front * .1f, z.front, .14f, Ink, TextAnchor.MiddleCenter);
            // "1999": a clock stuck at a minute to midnight
            var nn = Song(z, "1999"); var ck = z.At(6.5f, 2.5f); var num = SongGlow(nn, new Color(1f, .4f, .9f), 2f); foreach (var e in new[] { -1.1f, 1.1f }) { z.mk.Prism(chrome, ck + z.side * e - up * .2f, up, .05f, 2.2f, 5); ArtistBits.PostCollider(z.root, ck + z.side * e, 2f, .07f); }
            z.mk.Box(dark, ck + up * 2.4f, new Vector3(2.6f, 1.3f, .14f), rot); z.mk.Box(num, ck + up * 3.02f + z.front * .08f, new Vector3(2.5f, .06f, .04f), rot); z.mk.Box(num, ck + up * 1.78f + z.front * .08f, new Vector3(2.5f, .06f, .04f), rot);
            ArtistBits.Label(z.labels, "23:59", ck + up * 2.6f + z.front * .09f, z.front, .5f, new Color(1f, .6f, .95f), TextAnchor.MiddleCenter); ArtistBits.Label(z.labels, "31 DEC 1999", ck + up * 2.05f + z.front * .09f, z.front, .16f, Ink, TextAnchor.MiddleCenter); halos.Add(ck + up * 2.4f + z.front * .5f, 1.8f, Pink, .4f);
        }

        // ---------------------------------------------------------------- The Meters
        void Meters(Plot z)
        {
            var up = Vector3.up; var rot = z.Rot; var c = z.At(-3f, 0);
            // a second line stopped mid-street: umbrellas on poles, a bass drum, a snare, a tambourine
            Color[] par = { Purple, Gold, Lime, Orange, Pink }; var pk = Song(z, "Hey Pocky A-Way");
            for (var k = 0; k < 5; k++) { var p = c + z.side * (-4f + k * 2f) - z.front * (k % 2) * 1.2f; p.y = z.ctx.Ground(p.x, p.z); z.mk.Prism(trunk, p - up * .2f, up, .03f, 2.5f + (k % 2) * .4f, 5); var top = p + up * (2.3f + (k % 2) * .4f); z.mk.Prism(WorldMaterials.Glow(par[k], .5f), top, up, .8f, .3f, 8, .06f); for (var q = 0; q < 8; q++) z.mk.Blob(tileM[q % 4], top + Quaternion.Euler(0, q * 45f, 0) * z.front * .8f - up * .08f, Vector3.one * .05f, prng, 0f); ArtistBits.PostCollider(z.root, p, 2.2f, .06f); }
            var bd = c + z.front * 2f - z.side * 1.5f; z.mk.Prism(WorldMaterials.Solid(new Color(.6f, .15f, .2f)), bd + up * .6f - z.side * .25f, z.side, .6f, .5f, 16); z.mk.Prism(white, bd + up * .6f + z.side * .25f, z.side, .57f, .02f, 16); BoxCol(z.root, bd + up * .6f, new Vector3(.6f, 1.2f, 1.2f), rot);
            for (var q = 0; q < 9; q++) { var a = (q - 4) * 14f; var d = Quaternion.AngleAxis(a, z.side) * up; z.mk.Box(SongGlow(pk, par[q % 5], 1.1f), bd + up * .6f + d * 1.1f, new Vector3(.06f, .9f, .22f), Quaternion.LookRotation(z.side, d)); }
            var sn = c + z.front * 2.2f + z.side * 1.4f; z.mk.Prism(chrome, sn, up, .02f, .8f, 5); z.mk.Prism(white, sn + up * .8f, up, .3f, .16f, 12); ArtistBits.PostCollider(z.root, sn, .9f, .3f);
            var sm = new GameObject("Second line snare"); sm.transform.SetParent(plife.transform, false); sm.transform.position = sn + up * .9f; string[] roll = { "Rat-a-tat, a-rat-tat. The umbrellas bob. You've started a parade of one.", "You find the street beat: lazy on purpose, landing just behind where you expect it.", "Somebody answers on a cowbell from way back in the trees." }; var rn = 0;
            Interact.Toy(sm, Toy.Kind.Custom, "Play the snare", new Vector3(.9f, .8f, .9f)).onUse = _ => { life.plucked = Time.time; Say1(sn + up * 2.2f, roll[rn++ % roll.Length]); };
            z.area.glowLight = ArtistBits.PointLight(z.root, c + up * 4.5f + z.front * 2f, Gold, 24f, 2.2f);
            // "Fire on the Bayou": a black pond with fire on it
            var fb = Song(z, "Fire on the Bayou"); var po = z.At(3f, 8f); z.mk.Prism(stone, po - up * .1f, up, 3.6f, .24f, 14); z.mk.Prism(WorldMaterials.Solid(new Color(.05f, .09f, .14f), .9f), po + up * .15f, up, 3.2f, .02f, 14); var flame = SongGlow(fb, Orange, 1.8f); for (var k = 0; k < 7; k++) { var d = Quaternion.Euler(0, k * 51f, 0) * z.front * (.4f + (k % 3) * .8f); z.mk.Cone(flame, po + d + up * .17f, .2f + (k % 2) * .1f, .6f + (k % 3) * .25f, 5); } halos.Add(po + up * .6f, 3f, Orange, .45f); life.bayouAt = po + up * .3f; life.bayouSong = fb;
            // "Just Kissed My Baby": lips of neon
            var jk = Song(z, "Just Kissed My Baby"); var lp = z.At(3f, -7.5f); var lips = SongGlow(jk, new Color(1f, .25f, .45f), 2f); z.mk.Prism(chrome, lp - up * .2f, up, .04f, 1.7f, 5); for (var k = 0; k <= 10; k++) { var t = k / 10f; var x = -1f + t * 2f; var yU = Mathf.Sin(t * Mathf.PI) * .32f + (Mathf.Abs(x) < .3f ? -.1f * (1f - Mathf.Abs(x) / .3f) : 0f); var yL = -Mathf.Sin(t * Mathf.PI) * .42f; z.mk.Blob(lips, lp + up * (2f + yU) + z.side * x, Vector3.one * .08f, prng, 0f); z.mk.Blob(lips, lp + up * (2f + yL) + z.side * x, Vector3.one * .08f, prng, 0f); }
            halos.Add(lp + up * 2f + z.front * .3f, 1.6f, Pink, .5f); ArtistBits.PostCollider(z.root, lp, 1.7f, .08f);
            var lg = z.At(5.5f, 3f); z.mk.Prism(trunk, lg + up * .28f - z.side * 1.1f, z.side, .28f, 2.2f, 8); BoxCol(z.root, lg + up * .28f, new Vector3(2.2f, .56f, .56f), rot); Interact.Bench(plife.transform, lg + up * .58f - z.side * .7f, lg + up * .58f + z.side * .7f, -z.front);
        }

        // ---------------------------------------------------------------- James Brown
        void JamesBrown(Plot z)
        {
            var up = Vector3.up; var rot = z.Rot; var c = z.At(-2.5f, 0); var pb = Song(z, "The Payback");
            z.mk.Prism(paveDark, c, up, 3.8f, .5f, 14); z.solid.Prism(paveDark, c - up * .3f, up, 3.8f, .8f, 14); z.mk.Prism(SongGlow(pb, new Color(1f, .2f, .15f), 1.4f), c + up * .48f, up, 3.86f, .05f, 14);
            for (var k = 0; k < 2; k++) { var st = c + z.front * (3.9f + (1 - k) * .4f) + up * ((k + .5f) * .25f); z.mk.Box(paveDark, st, new Vector3(2f, .25f, .4f), rot); z.solid.Box(paveDark, st - up * (k * .125f), new Vector3(2f, .25f * (k + 1), .4f), rot); }
            // the mic stand caught mid-fall, the footwork lit on the floor, the cape on its stand
            var mic = c + up * .5f + z.front * 1.4f; z.mk.Prism(chrome, mic, (up + z.side * .7f).normalized, .02f, 1.6f, 5); z.mk.Prism(chrome, mic, up, .22f, .04f, 10); z.mk.Blob(mirror, mic + (up + z.side * .7f).normalized * 1.65f, new Vector3(.07f, .1f, .07f), prng, 0f);
            for (var k = 0; k < 8; k++) z.mk.Blob(tileM[k % 4], c + up * .52f + z.front * (-.2f + (k % 2) * .5f) + z.side * (-1.6f + k * .42f), new Vector3(.08f, .012f, .16f), prng, 0f);
            var cs = c + up * .5f - z.side * 2.2f - z.front * .6f; z.mk.Prism(chrome, cs, up, .03f, 1.8f, 5); z.mk.Box(chrome, cs + up * 1.75f, new Vector3(.9f, .04f, .04f), rot);
            var cm = new MeshKit(); var cape = WorldMaterials.Solid(new Color(.5f, .1f, .55f), .5f); cm.Box(cape, new Vector3(0, -.75f, 0), new Vector3(1.1f, 1.5f, .08f), Quaternion.identity); cm.Box(WorldMaterials.Solid(new Color(.9f, .72f, .25f), .8f), new Vector3(0, -1.5f, .01f), new Vector3(1.12f, .1f, .09f), Quaternion.identity); cm.Box(WorldMaterials.Solid(new Color(.9f, .72f, .25f), .8f), new Vector3(0, -.05f, .01f), new Vector3(1.12f, .1f, .09f), Quaternion.identity);
            var cgo = cm.BuildSingle("The cape", plife.transform, false); cgo.transform.SetPositionAndRotation(cs + up * 1.72f + z.front * .06f, rot); string[] capeL = { "You put on the cape. You stagger off. You throw it down and come back for one more. The crowd you can't see goes wild.", "They drape the cape over you. You're done. You are not done.", "Third time back to the mic. Somebody shouts \"Good God!\" and means it." }; var cn = 0;
            Interact.Toy(cgo, Toy.Kind.Custom, "Take the cape", new Vector3(1.3f, 2f, .8f)).onUse = _ => Say1(cs + up * 2.8f, capeL[cn++ % capeL.Length]);
            z.area.glowLight = ArtistBits.PointLight(z.root, c + up * 4.2f + z.front * 2f, new Color(1f, .5f, .4f), 22f, 2.2f); life.paybackSong = pb;
            var ps = z.At(3f, 6.5f); foreach (var e in new[] { -1.3f, 1.3f }) { z.mk.Prism(chrome, ps + z.side * e - up * .2f, up, .05f, 2.6f, 5); ArtistBits.PostCollider(z.root, ps + z.side * e, 2.4f, .07f); }
            z.mk.Box(dark, ps + up * 2.8f, new Vector3(3.2f, 1f, .14f), rot); var pg = SongGlow(pb, new Color(1f, .2f, .15f), 2.2f); z.mk.Box(pg, ps + up * 3.26f + z.front * .08f, new Vector3(3.1f, .06f, .04f), rot); z.mk.Box(pg, ps + up * 2.34f + z.front * .08f, new Vector3(3.1f, .06f, .04f), rot); ArtistBits.Label(z.labels, "THE PAYBACK", ps + up * 2.8f + z.front * .09f, z.front, .36f, new Color(1f, .55f, .45f), TextAnchor.MiddleCenter); halos.Add(ps + up * 2.8f + z.front * .5f, 2f, new Color(1f, .3f, .2f), .45f);
            // "People Get Up And Drive Your Funky Soul": a steering wheel on a post, a road sign
            var dr = Song(z, "People Get Up"); var wp = z.At(3f, -6.5f); z.mk.Prism(chrome, wp - up * .2f, (up + z.front * .25f).normalized, .05f, 1.4f, 5); var wc = wp + (up + z.front * .25f).normalized * 1.25f; var wn = (z.front + up * .5f).normalized; var wheel = SongGlow(dr, Gold, 1.6f); for (var k = 0; k < 14; k++) { var d = Quaternion.AngleAxis(k * 360f / 14f, wn) * z.side; z.mk.Blob(wheel, wc + d * .34f, Vector3.one * .06f, prng, 0f); } for (var k = 0; k < 3; k++) { var d = Quaternion.AngleAxis(k * 120f + 90f, wn) * z.side; z.mk.Prism(chrome, wc, d, .02f, .32f, 4); }
            ArtistBits.PostCollider(z.root, wp, 1.4f, .1f); ArtistBits.Label(z.labels, "DRIVE", wp + up * 2.1f, z.front, .28f, Gold, TextAnchor.MiddleCenter); Interact.Seat(plife.transform, wp - z.front * .6f + up * .5f, z.front, "Sit behind the wheel"); z.mk.Box(dark, wp - z.front * .6f + up * .22f, new Vector3(.6f, .44f, .6f), rot);
        }

        // ---------------------------------------------------------------- Thundercat
        void Thundercat(Plot z)
        {
            var up = Vector3.up; var rot = z.Rot; var c = z.At(-2.5f, 0);
            // a giant six-string bass standing in the clearing, cat ears on the headstock
            var body = WorldMaterials.Solid(new Color(.9f, .5f, .15f), .7f); z.mk.Blob(body, c + up * 1.5f, new Vector3(1.3f, 1.6f, .3f), prng, .05f); z.mk.Box(WorldMaterials.Solid(new Color(.45f, .3f, .18f)), c + up * 5f, new Vector3(.5f, 4.4f, .2f), rot); z.mk.Box(body, c + up * 7.5f, new Vector3(.7f, 1f, .22f), rot); foreach (var e in new[] { -1f, 1f }) z.mk.Cone(body, c + up * 7.95f + z.side * e * .28f, .2f, .46f, 4);
            var tc = Song(z, "Them Changes"); var strings = SongGlow(tc, Cyan, 1.8f); for (var k = 0; k < 6; k++) z.mk.Box(strings, c + up * 4.3f + z.side * (-.2f + k * .08f) + z.front * .13f, new Vector3(.02f, 6.4f, .02f), rot); BoxCol(z.root, c + up * 4f, new Vector3(2.4f, 8f, .7f), rot); halos.Add(c + up * 4f + z.front * .8f, 3.4f, Cyan, .35f);
            z.area.glowLight = ArtistBits.PointLight(z.root, c + up * 4.5f + z.front * 3f, Cyan, 22f, 2.2f);
            // "Them Changes": a heart-shaped hole of light in the floor
            var hh = z.At(3.4f, 0); var heart = SongGlow(tc, new Color(1f, .25f, .45f), 1.6f); for (var k = 0; k < 26; k++) { var t = k / 26f * Mathf.PI * 2f; var hx = 16f * Mathf.Pow(Mathf.Sin(t), 3f); var hy = 13f * Mathf.Cos(t) - 5f * Mathf.Cos(2f * t) - 2f * Mathf.Cos(3f * t) - Mathf.Cos(4f * t); z.mk.Blob(heart, hh + up * .12f + z.side * hx * .07f + z.front * hy * .07f, new Vector3(.12f, .04f, .12f), prng, 0f); } z.mk.Prism(dark, hh + up * .08f + z.front * .15f, up, .9f, .02f, 12); ArtistBits.Label(z.labels, "nobody move", hh + up * .16f - z.front * 1.6f, up, .13f, Ink, TextAnchor.MiddleCenter);
            // "Dragonball Durag": an orange durag on a stand, seven gold stars round it
            var dd = Song(z, "Dragonball Durag"); var ds = z.At(3f, 6f); var dur = SongGlow(dd, new Color(1f, .55f, .1f), 1.2f); z.mk.Prism(chrome, ds - up * .2f, up, .04f, 1.6f, 5); z.mk.Blob(white, ds + up * 1.6f, Vector3.one * .2f, prng, 0f); z.mk.Blob(dur, ds + up * 1.68f - z.front * .02f, new Vector3(.23f, .2f, .24f), prng, .03f); z.mk.Box(dur, ds + up * 1.2f - z.front * .3f, new Vector3(.12f, .8f, .03f), rot * Quaternion.Euler(-14f, 0, 6f));
            for (var k = 0; k < 7; k++) { var d = Quaternion.Euler(0, k * 51.4f, 0) * z.front; z.mk.Blob(SongGlow(dd, Gold, 1.6f), ds + d * .9f + up * (.9f + Mathf.Sin(k * 2.1f) * .5f), Vector3.one * .1f, prng, 0f); } halos.Add(ds + up * 1.5f, 1.6f, Orange, .45f); ArtistBits.PostCollider(z.root, ds, 1.6f, .1f);
            var lg = z.At(4f, -5f); z.mk.Prism(trunk, lg + up * .28f - z.side * 1.1f, z.side, .28f, 2.2f, 8); BoxCol(z.root, lg + up * .28f, new Vector3(2.2f, .56f, .56f), rot); Interact.Bench(plife.transform, lg + up * .58f - z.side * .7f, lg + up * .58f + z.side * .7f, -z.front);
        }

        // ---------------------------------------------------------------- Commodores
        void Commodores(Plot z)
        {
            var up = Vector3.up; var rot = z.Rot; var bh = Song(z, "Brick House"); var c = z.At(-2.5f, -1.5f); var bk = WorldMaterials.Solid(new Color(.6f, .26f, .2f)); var bk2 = WorldMaterials.Solid(new Color(.72f, .34f, .24f));
            // a house of big bricks, mighty mighty: solid all the way through, one window of light
            for (var y = 0; y < 5; y++) for (var x = 0; x < 4; x++) { var off = (y % 2) * .55f; z.mk.Box((x + y) % 2 == 0 ? bk : bk2, c + z.side * (-1.65f + x * 1.1f + off - .27f) + up * (.3f + y * .6f), new Vector3(1.06f, .56f, 3f), rot); }
            z.solid.Box(bk, c + up * 1.5f, new Vector3(4.6f, 3f, 3f), rot); z.mk.Box(SongGlow(bh, new Color(1f, .7f, .35f), 1.6f), c + up * 1.7f + z.front * 1.52f, new Vector3(1f, 1f, .04f), rot); foreach (var e in new[] { -1f, 1f }) z.mk.Box(WorldMaterials.Solid(new Color(.4f, .2f, .16f)), c + up * 3.5f + z.side * e * 1.2f, new Vector3(2.9f, .16f, 3.4f), rot * Quaternion.Euler(0, 0, -e * 28f));
            ArtistBits.Label(z.labels, "MIGHTY MIGHTY", c + up * 2.7f + z.front * 1.52f, z.front, .22f, Ink, TextAnchor.MiddleCenter); halos.Add(c + up * 1.7f + z.front * 2f, 1.6f, Gold, .4f); z.area.glowLight = ArtistBits.PointLight(z.root, c + up * 4.5f + z.front * 3f, Gold, 18f, 2f);
            // "Easy": a hammock between two palms
            var ez = Song(z, "Easy"); var a = z.At(3f, 3f); var b = z.At(3f, 6.6f); foreach (var e in new[] { a, b }) { z.mk.Prism(trunk, e - up * .3f, up, .2f, 4.6f, 7, .7f); ArtistBits.PostCollider(z.root, e, 4f, .22f); for (var k = 0; k < 6; k++) { var d = Quaternion.Euler(0, k * 60f, 0) * z.front; z.mk.Box(k % 2 == 0 ? leaf : leafDark, e + up * 4.4f + (d + up * .2f).normalized * 1.1f, new Vector3(.6f, .05f, 2.2f), Quaternion.LookRotation((d + up * .1f).normalized, up)); } }
            var ham = SongGlow(ez, new Color(.95f, .85f, .6f), .7f); for (var k = 0; k < 9; k++) { var t = k / 8f; var p = Vector3.Lerp(a, b, t) + up * (1.5f - Mathf.Sin(t * Mathf.PI) * .8f); z.mk.Box(ham, p, new Vector3(1f, .04f, .5f), Quaternion.LookRotation(z.side)); }
            Interact.Seat(plife.transform, (a + b) / 2f + up * .74f, -z.front, "Lie in the hammock").seatHeight = .5f; ArtistBits.Label(z.labels, "easy like Sunday morning", (a + b) / 2f + up * 2.4f, z.front, .13f, Ink, TextAnchor.MiddleCenter);
        }

        // ---------------------------------------------------------------- everyone else
        void Others(Plot z, int hue)
        {
            var up = Vector3.up; var rot = z.Rot; var p = z.At(2.6f, 2.6f); var col = Neon[hue]; string s;
            switch (z.a.name)
            {
                case "Average White Band":
                    s = Song(z, "Pick Up the Pieces"); for (var k = 0; k < 7; k++) { var q = z.At(1.6f + (k % 3) * 1f, 1.4f + k * .5f); z.mk.Box(SongGlow(s, Neon[k % Neon.Length], 1.1f), q + up * .12f, new Vector3(.5f, .06f, .5f), rot * Quaternion.Euler(0, k * 37f, 0)); z.mk.Blob(SongGlow(s, Neon[k % Neon.Length], 1.1f), q + up * .12f + (rot * Quaternion.Euler(0, k * 37f, 0) * Vector3.right) * .3f, new Vector3(.12f, .03f, .12f), prng, 0f); }
                    z.mk.Prism(chrome, p - up * .1f, up, .03f, 1f, 5); z.mk.Prism(GlowM(Gold, 1f), p + up * .9f, (up + z.side * .3f).normalized, .05f, .7f, 6, 2.2f); break;
                case "Curtis Mayfield":
                    s = Song(z, "Pusherman"); foreach (var e in new[] { -1.2f, 1.2f }) { z.mk.Prism(chrome, p + z.side * e - up * .2f, up, .05f, 2.4f, 5); ArtistBits.PostCollider(z.root, p + z.side * e, 2.2f, .07f); } z.mk.Box(dark, p + up * 2.6f, new Vector3(3f, 1f, .14f), rot); var mq = SongGlow(s, Gold, 2f); for (var k = 0; k < 10; k++) { z.mk.Blob(mq, p + up * 3.05f + z.side * (-1.35f + k * .3f) + z.front * .09f, Vector3.one * .06f, prng, 0f); z.mk.Blob(mq, p + up * 2.15f + z.side * (-1.35f + k * .3f) + z.front * .09f, Vector3.one * .06f, prng, 0f); }
                    ArtistBits.Label(z.labels, "SUPER FLY", p + up * 2.6f + z.front * .09f, z.front, .36f, Gold, TextAnchor.MiddleCenter); z.mk.Box(chrome, p + up * .1f + z.front * 1.2f, new Vector3(.3f, .12f, .5f), rot * Quaternion.Euler(-12f, 0, 0)); break;
                case "Flea":
                    s = Song(z, "Maggot Brain"); z.mk.Prism(chrome, p, up, .02f, .35f, 5); z.mk.Blob(SongGlow(s, new Color(1f, .25f, .2f), 1.2f), p + up * .7f, new Vector3(.22f, .3f, .06f), prng, .05f); z.mk.Box(WorldMaterials.Solid(new Color(.5f, .34f, .2f)), p + up * 1.5f, new Vector3(.07f, 1.1f, .04f), rot); z.mk.Box(dark, p + up * 2.1f, new Vector3(.1f, .2f, .04f), rot); ArtistBits.PostCollider(z.root, p, 2f, .2f); halos.Add(p + up * .8f, 1f, new Color(1f, .3f, .25f), .4f); break;
                case "Funk Factory - Michael Urbaniak":
                    s = Song(z, "Rien Ne Va Plus"); z.mk.Prism(trunk, p, up, .5f, .8f, 10); var wm = new MeshKit(); wm.Prism(dark, Vector3.zero, Vector3.up, .9f, .08f, 18); for (var k = 0; k < 18; k++) wm.Box(k == 0 ? GlowM(Lime, 1.4f) : k % 2 == 0 ? SongGlow(s, new Color(1f, .2f, .2f), 1.1f) : (Material)paveDark, Quaternion.Euler(0, k * 20f, 0) * new Vector3(0, .09f, .68f), new Vector3(.2f, .02f, .3f), Quaternion.Euler(0, k * 20f, 0)); wm.Blob(white, new Vector3(0, .13f, .5f), Vector3.one * .05f, prng, 0f); wm.Prism(chrome, new Vector3(0, .08f, 0), Vector3.up, .12f, .14f, 8);
                    var wgo = wm.BuildSingle("Roulette wheel", plife.transform, false); wgo.transform.position = p + up * .8f; ArtistBits.PostCollider(z.root, p, .9f, .9f); var t = Interact.Toy(wgo, Toy.Kind.Spin, "Spin the wheel", new Vector3(2f, .8f, 2f)); t.seconds = 5f; t.amount = 1440f; break;
                case "Ginger Root":
                    s = Song(z, "Loretta"); z.mk.Box(WorldMaterials.Solid(new Color(.5f, .36f, .24f)), p + up * .35f, new Vector3(.9f, .7f, .6f), rot); z.mk.Box(WorldMaterials.Solid(new Color(.3f, .24f, .2f)), p + up * 1.05f, new Vector3(.9f, .7f, .7f), rot); z.mk.Box(SongGlow(s, new Color(.6f, .9f, 1f), 1.6f), p + up * 1.07f + z.front * .36f, new Vector3(.62f, .48f, .02f), rot); foreach (var e in new[] { -1f, 1f }) z.mk.Prism(chrome, p + up * 1.4f, (up + z.side * e * .6f).normalized, .01f, .6f, 3); BoxCol(z.root, p + up * .7f, new Vector3(.9f, 1.4f, .7f), rot); halos.Add(p + up * 1.07f + z.front * .7f, 1.1f, Cyan, .45f); break;
                case "Incredible Bongo Band":
                    s = Song(z, "Apache"); var bm = new MeshKit(); foreach (var e in new[] { -.24f, .26f }) { bm.Prism(WorldMaterials.Solid(new Color(.6f, .3f, .15f)), new Vector3(e, 0, 0), Vector3.up, e < 0 ? .2f : .25f, .5f, 10, .85f); bm.Prism(SongGlow(s, new Color(1f, .9f, .7f), 1f), new Vector3(e, .5f, 0), Vector3.up, e < 0 ? .19f : .24f, .02f, 10); } var bgo = bm.BuildSingle("Bongos", plife.transform, false); bgo.transform.position = p + up * .5f; z.mk.Prism(chrome, p, up, .03f, .5f, 5); ArtistBits.PostCollider(z.root, p, 1f, .4f);
                    string[] bo = { "Bip-bop, bippy-bip-bop. You've played the most sampled eight bars there are.", "The break. Somewhere a DJ who isn't born yet says thank you.", "Both hands, eyes shut. The jungle keeps time for you." }; var bn = 0; Interact.Toy(bgo, Toy.Kind.Custom, "Play the bongos", new Vector3(1f, 1f, .8f)).onUse = _ => { life.plucked = Time.time; Say1(p + up * 2f, bo[bn++ % bo.Length]); }; break;
                case "Kaelin Ellis":
                    s = Song(z, "Move"); for (var k = 0; k < 5; k++) { var q = z.At(1.4f + k * .9f, 1.6f + k * .5f); z.mk.Prism(SongGlow(s, Neon[(hue + k) % Neon.Length], 1.4f), q + up * .12f, up, .36f, .03f, 3, 1f, rot.eulerAngles.y + 30f); z.mk.Box(SongGlow(s, Neon[(hue + k) % Neon.Length], 1.4f), q + up * .12f - z.front * .4f, new Vector3(.2f, .03f, .5f), rot); } break;
                case "Lafayette Afro Rock Band":
                    s = Song(z, "Malik"); for (var k = 0; k < 3; k++) { var h = p + z.side * (-1f + k * 1f); z.mk.Prism(chrome, h - up * .2f, up, .03f, 1.4f, 5); var hd = (z.front + up * .5f).normalized; z.mk.Prism(GlowM(Gold, .8f), h + up * 1.2f - hd * .4f, hd, .04f, .6f, 6); z.mk.Cone(SongGlow(s, Gold, 1.5f), h + up * 1.2f + hd * .55f, .22f, -.4f, 10); ArtistBits.PostCollider(z.root, h, 1.3f, .1f); } break;
                case "Lettuce":
                    s = Song(z, "Phyllis"); z.mk.Box(trunk, p + up * .25f, new Vector3(1.4f, .5f, .8f), rot); BoxCol(z.root, p + up * .25f, new Vector3(1.4f, .5f, .8f), rot); for (var k = 0; k < 4; k++) { var lp = p + up * .62f + z.side * (-.45f + k * .3f) + z.front * ((k % 2) * .2f - .1f); z.mk.Blob(SongGlow(s, new Color(.55f, 1f, .45f), 1.1f), lp, new Vector3(.18f, .15f, .18f), prng, .25f); } halos.Add(p + up * .8f, 1.1f, Lime, .4f); break;
                case "Shuggie Otis":
                    s = Song(z, "Inspiration Information"); z.mk.Box(WorldMaterials.Solid(new Color(.2f, .3f, .45f)), p + up * 1.1f, new Vector3(1.4f, 2.2f, 1.2f), rot); BoxCol(z.root, p + up * 1.1f, new Vector3(1.4f, 2.2f, 1.2f), rot); z.mk.Box(dark, p + up * 1.3f + z.front * .61f, new Vector3(1f, .8f, .02f), rot); ArtistBits.Label(z.labels, "i", p + up * 2.6f + z.front * .1f, z.front, .5f, Ink, TextAnchor.MiddleCenter); z.mk.Blob(SongGlow(s, new Color(1f, .9f, .5f), 2f), p + up * 3.2f, new Vector3(.22f, .28f, .22f), prng, 0f); z.mk.Prism(chrome, p + up * 2.2f, up, .05f, .8f, 5); halos.Add(p + up * 3.2f, 1.6f, Gold, .55f); break;
                case "The Cactus Channel":
                    s = Song(z, "Stay a While"); var cg = SongGlow(s, new Color(.4f, .9f, .5f), .8f); z.mk.Prism(cg, p - up * .1f, up, .3f, 2.6f, 8); foreach (var e in new[] { -1f, 1f }) { z.mk.Prism(cg, p + up * (1f + (e > 0 ? .4f : 0f)), z.side * e, .18f, .8f, 6); z.mk.Prism(cg, p + up * (1f + (e > 0 ? .4f : 0f)) + z.side * e * .7f, up, .18f, .9f, 6); } z.mk.Blob(floraM[2], p + up * 2.6f, Vector3.one * .12f, prng, 0f); ArtistBits.PostCollider(z.root, p, 2.6f, .34f); ArtistBits.Label(z.labels, "stay a while", p + up * 3.1f, z.front, .14f, Ink, TextAnchor.MiddleCenter); break;
                case "Voilaaa":
                    s = Song(z, "Spies Are Watching Me"); z.mk.Blob(leafDark, p + up * .9f, new Vector3(1.6f, 1.1f, 1.2f), prng, .3f); z.mk.Blob(leaf, p + up * 1.2f + z.side * .9f, new Vector3(1f, .8f, .9f), prng, .3f); BoxCol(z.root, p + up * .8f, new Vector3(2.4f, 1.6f, 1.4f), rot); var eye = SongGlow(s, new Color(1f, 1f, .7f), 2f); foreach (var (x, y) in new[] { (-.5f, 1.1f), (.6f, .8f), (.2f, 1.5f) }) foreach (var e in new[] { -.09f, .09f }) { z.mk.Blob(eye, p + up * y + z.side * (x + e) + z.front * .95f, new Vector3(.06f, .04f, .02f), prng, 0f); z.mk.Blob(dark, p + up * y + z.side * (x + e) + z.front * .97f, new Vector3(.02f, .03f, .01f), prng, 0f); } break;
                default:
                    z.mk.Blob(GlowM(col, 1.4f), p + up * .5f, Vector3.one * .3f, prng, .1f); break;
            }
        }
    }
}
