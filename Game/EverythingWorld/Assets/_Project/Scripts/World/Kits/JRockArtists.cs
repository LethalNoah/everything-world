using System.Collections.Generic;
using UnityEngine;

namespace EverythingWorld.World
{
    /// The J-Rock Isles' artists. In the city each artist has a shopfront: a two-storey shop with its shutter rolled
    /// up, a warm room inside, an awning, a neon sign with the artist's name, a vending machine beside it; out on the
    /// hill, a small shrine court with a red gate and stone lanterns. Covers on a board, a plaque, a paper lantern;
    /// songs without a piece of their own get a red paper lantern that glows while they play (SongLanterns).
    /// - Hoshikuma Minami (the largest, in the city centre): a concert stage on the square, giant screens on the
    ///   building behind cycling colours, light trusses; a neon eye ("HOLLOW EYES"), a ring of rising bubbles
    ///   ("OXYGEN"), a halo ring of light ("NIRVANA"), a heart that pulses ("CRAVE").
    /// - Sithu Aye (by the school on the hill): a rooftop stage in the schoolyard, an amp and a guitar on a stand, a
    ///   banner announcing a new season and a cardboard cut-out of a new character ("It's the Second Season, So We Need
    ///   a New Character After All!"), a big glowing exclamation mark ("The Noticing!"), a sky-blue kite flying high
    ///   ("Skye": the sky clears to blue).
    /// - Others with a piece of a song: Ado a neon heart ("Aishite Aishite Aishite"); FLOW a rainbow neon arch
    ///   ("COLORS": the city's neon cycles through every colour); Roselia a phoenix of neon wings ("FIRE BIRD");
    ///   nomico a black-and-white screen with an apple ("Bad Apple!!": the whole land goes black and white);
    ///   Mai Yamane a little blues bar with a saxophone sign ("The Real Folk Blues"); Yorushika a bus stop with a
    ///   bench under a sun sign ("ただ君に晴れ"); ASIAN KUNG-FU GENERATION and Supercar a bicycle leaning on a sea
    ///   rail; King Gnu a red neon sign ("SPECIALZ"); the game composers (Mitsukiyo, technoplanet, Feryquitous) an
    ///   arcade cabinet in their shop window.
    public partial class JRockKit
    {
        class Plot
        {
            public LandContext ctx; public Transform root, labels; public MeshKit mk, solid, pictures;
            public IslandWorldData.Artist a; public Vector3 c, front, side; public ArtistArea area; public HashSet<string> lit = new HashSet<string>(); public bool city;
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
                Add("Hoshikuma Minami", "OXYGEN", "Hoshikuma Minami"); Add("Sithu Aye", "Skye", "@the school on the hill");
                Add("nomico", "Bad Apple", "@the arrival and the city"); Add("FLOW", "COLORS", "@the arrival and the city"); Add("Roselia", "FIRE BIRD", "Roselia");
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
            if (concrete == null) MakeMaterials();
            var root = new GameObject("Artist: " + a.name).transform; root.SetParent(ctx.Root, false);
            var front = facing.TryGetValue(a.name, out var f) ? f : new Vector3(center.x - a.x, 0, center.z - a.z).normalized;
            if (front.sqrMagnitude < .01f) front = Vector3.forward;
            var labels = new GameObject("Labels").transform; labels.SetParent(root, false);
            var area = root.gameObject.AddComponent<ArtistArea>();
            area.artistName = a.name; area.stationId = a.stationId; area.radius = a.r; area.glowColor = Pink;
            var z = new Plot { ctx = ctx, root = root, labels = labels, mk = new MeshKit(), solid = new MeshKit(), pictures = new MeshKit(), a = a, c = ctx.OnGround(a.x, a.z), front = front, side = Vector3.Cross(Vector3.up, front), area = area, city = InCity(a.x, a.z) };
            var reach = Foot(a); var up = Vector3.up;
            z.mk.Box(z.city ? concrete : WorldMaterials.Solid(new Color(.55f, .53f, .5f)), z.c - up * .15f, new Vector3(reach * 1.25f, .4f, reach * 1.25f), z.Rot);
            switch (a.name)
            {
                case "Hoshikuma Minami": Hoshikuma(z); break;
                case "Sithu Aye": SithuAye(z); break;
                default: if (z.city) Shopfront(z); else ShrineCourt(z); break;
            }
            SongLanterns(z);
            Board(z, z.At(reach * .55f, -reach * .45f - 1.5f), front);
            ArtistBits.Plaque(z.mk, z.solid, labels, a, z.At(reach * .55f + 1.5f, reach * .45f + 1.5f), front, dark, concreteDark, Ink, 1.6f);
            if (z.area.glowLight == null) z.area.glowLight = ArtistBits.PointLight(root, z.c + up * 5f + front * 4f, new Color(1f, .7f, .8f), HasStation(a) ? 22f : 15f, 2f);
            area.glowRenderers = new Renderer[0];
            z.mk.Build("Area", root, false);
            z.solid.BuildCollider("Walls", root);
            z.pictures.Build("Covers", root, false);
            noGrass.Add(new Vector3(z.c.x, reach * .9f, z.c.z));
            if (HasStation(a))
            {
                var mark = new GameObject("Shot: " + a.name + " up close").transform; mark.SetParent(ctx.Root, false);
                var cam = z.At(reach * .55f, reach * .3f) + up * 1.7f; var look = z.At(-reach * .4f, 0) + up * 4f - cam; mark.SetPositionAndRotation(cam, Quaternion.LookRotation(look.normalized));
            }
        }

        void Board(Plot z, Vector3 ground, Vector3 dir)
        {
            dir = Vector3.ProjectOnPlane(dir, Vector3.up).normalized; var rot = Quaternion.LookRotation(dir); var side = Vector3.Cross(Vector3.up, dir);
            var n = Mathf.Max(1, Mathf.Min(6, z.a.songList?.Length ?? 1)); const float size = .9f; var w = n * size * 1.25f + .6f;
            var c = ground + Vector3.up * 1.7f;
            z.mk.Box(dark, c - Vector3.up * .5f, new Vector3(w, size + 1.6f, .2f), rot); z.solid.Box(dark, c - Vector3.up * .5f, new Vector3(w, size + 1.6f, .4f), rot);
            foreach (var y in new[] { -1.3f, size / 2f + .32f }) z.mk.Box(GlowM(Pink, 2f), c + Vector3.up * y + dir * .11f, new Vector3(w, .06f, .02f), rot);
            ArtistBits.CoverWall(z.pictures, z.labels, z.ctx, z.a, c + dir * .11f + Vector3.up * .08f, dir, n, size, n, true, Ink);
        }

        void SongLanterns(Plot z)
        {
            if (!SongIds.TryGetValue(z.a.name, out var list)) return;
            var up = Vector3.up; var k = 0; var seen = new HashSet<string>();
            foreach (var (_, id) in list)
            {
                if (z.lit.Contains(id) || !seen.Add(id)) continue;
                var p = z.At(-Foot(z.a) * .1f - (k % 2) * 1.6f, Foot(z.a) * .5f + (k / 2) * 1.6f);
                z.mk.Box(woodDark, p + up * 1.4f, new Vector3(.12f, 2.8f, .12f), Quaternion.identity); ArtistBits.PostCollider(z.root, p, 2.7f, .12f);
                z.mk.Box(woodDark, p + up * 2.75f + z.front * .3f, new Vector3(.08f, .08f, .7f), z.Rot);
                var lp = p + up * 2.25f + z.front * .55f;
                z.mk.Blob(SongGlow(id, new Color(1f, .35f, .2f), 1.8f), lp, new Vector3(.26f, .34f, .26f), z.ctx.Rng, 0f);
                halos.Add(lp, 1f, new Color(1f, .4f, .25f), .5f);
                k++;
            }
        }

        // ---------------------------------------------------------------- shopfronts and shrine courts
        /// A two-storey shop facing the path, shutter rolled up, warm inside, an awning, the artist's name in neon, a
        /// vending machine beside it; the artist's piece in the window or out front.
        void Shopfront(Plot z)
        {
            var up = Vector3.up; var ctx = z.ctx; var rot = z.Rot; var sd = z.side;
            var s = z.At(-Foot(z.a) * .45f, 0); const float W = 7f, D = 5f, H = 6f;
            var col = NeonC[Mathf.Abs(z.a.name.GetHashCode()) % NeonC.Length];
            var wall = WorldMaterials.Solid(new[] { new Color(.75f, .7f, .65f), new Color(.6f, .65f, .7f), new Color(.8f, .76f, .66f) }[Mathf.Abs(z.a.name.GetHashCode() / 7) % 3]);
            var b = s - z.front * (D / 2f);
            z.mk.Box(wall, b + up * (H / 2f) - z.front * .1f, new Vector3(W, H, D - .2f), rot);
            foreach (var e in new[] { -1f, 1f }) z.solid.Box(wall, b + sd * e * (W / 2f - .2f) + up * H / 2f, new Vector3(.4f, H, D), rot);
            z.solid.Box(wall, b - z.front * (D / 2f - .2f) + up * H / 2f, new Vector3(W, H, .4f), rot); z.solid.Box(wall, b + up * (H - .8f), new Vector3(W, 1.6f, D), rot);
            z.mk.Box(GlowM(new Color(1f, .85f, .6f), 1f), b - z.front * (D / 2f - .45f) + up * 1.6f, new Vector3(W - 1f, 3f, .05f), rot);   // the lit back of the shop
            z.mk.Box(metal, s + up * 3.4f, new Vector3(W, .7f, .3f), rot);   // the rolled-up shutter
            z.mk.Box(WorldMaterials.Solid(col * .6f), s + up * 3f + z.front * .9f, new Vector3(W + .4f, .1f, 1.8f), rot * Quaternion.Euler(-14f, 0, 0));
            z.mk.Box(GlowM(new Color(1f, .85f, .55f), 1.2f), s + up * 4.9f + z.front * .03f, new Vector3(W * .7f, 1.1f, .04f), rot);   // the lit upstairs window
            var sign = s + up * 6.6f; z.mk.Box(dark, sign, new Vector3(W, 1.2f, .25f), rot);
            foreach (var y in new[] { -.6f, .6f }) z.mk.Box(GlowM(col, 2.4f), sign + up * y + z.front * .14f, new Vector3(W, .07f, .04f), rot);
            ArtistBits.Label(z.labels, z.a.name.ToUpperInvariant(), sign + z.front * .15f, z.front, .42f, col, TextAnchor.MiddleCenter);
            halos.Add(sign + z.front * 1f, 3.4f, col, .35f); halos.Add(s + up * 1.6f + z.front * 1.5f, 3f, new Color(1f, .85f, .6f), .3f);
            life.neon.Add(sign);
            Vending(ctx, z.mk, s + sd * (W / 2f + .9f) + z.front * .4f, z.front, z.a.name.Length);
            Piece(z, s + z.front * 1.6f);
        }

        /// A small shrine court out on the hill: a red gate, two stone lanterns, a little offering stand, cherry blossom.
        void ShrineCourt(Plot z)
        {
            var up = Vector3.up; var ctx = z.ctx;
            Torii(ctx, z.mk, z.At(Foot(z.a) * .2f, 0), z.front, .85f);
            foreach (var e in new[] { -1f, 1f }) StoneLantern(ctx, z.mk, z.At(-1f, e * 3.2f));
            var st = z.At(-4f, 0); z.mk.Box(wood, st + up * .5f, new Vector3(1.6f, 1f, .8f), z.Rot); BoxCol(z.root, st + up * .5f, new Vector3(1.6f, 1f, .8f), z.Rot);
            z.mk.GableRoof(tile, st + up * 1.6f, new Vector3(1.2f, 0, 2f), .5f, Quaternion.LookRotation(z.side));
            foreach (var e in new[] { -.6f, .6f }) z.mk.Box(woodDark, st + up * 1.3f + z.side * e, new Vector3(.08f, .6f, .08f), z.Rot);
            Cherry(ctx, z.mk, z.At(-5f, 4.5f), 6f);
            Piece(z, z.At(-1.5f, -3.5f));
        }

        /// The artist's own piece, where their song gives one.
        void Piece(Plot z, Vector3 at)
        {
            var up = Vector3.up; var ctx = z.ctx; var rot = z.Rot; var sd = z.side;
            switch (z.a.name)
            {
                case "Ado":
                {
                    var heart = SongGlow(z, "Aishite", Pink, 2.2f); var c = at + up * 2.4f;
                    foreach (var e in new[] { -1f, 1f }) z.mk.Box(heart, c + sd * e * .32f, new Vector3(.2f, 1.2f, .06f), rot * Quaternion.Euler(0, 0, e * 40f));
                    foreach (var e in new[] { -1f, 1f }) z.mk.Box(heart, c + sd * e * .4f + up * .55f, new Vector3(.55f, .2f, .06f), rot * Quaternion.Euler(0, 0, e * -20f));
                    z.mk.Prism(dark, at - up * .1f, up, .06f, 1.9f, 6); ArtistBits.PostCollider(z.root, at, 1.8f, .1f); halos.Add(c, 1.8f, Pink, .5f); break;
                }
                case "FLOW":
                {
                    Color[] rb = { new Color(1f, .3f, .3f), new Color(1f, .65f, .2f), new Color(1f, .95f, .3f), new Color(.35f, 1f, .45f), new Color(.3f, .7f, 1f), new Color(.65f, .4f, 1f) };
                    var colors = Song(z, "COLORS");
                    for (var k = 0; k < rb.Length; k++)
                        for (var j = 0; j < 16; j++)
                        {
                            float a0 = j * Mathf.PI / 16f, a1 = (j + 1) * Mathf.PI / 16f; var r = 2.6f - k * .2f;
                            var p0 = at + sd * Mathf.Cos(a0) * r + up * Mathf.Sin(a0) * r; var p1 = at + sd * Mathf.Cos(a1) * r + up * Mathf.Sin(a1) * r;
                            z.mk.Box(SongGlow(colors, rb[k], 1.8f), (p0 + p1) / 2f, new Vector3(.15f, .15f, Vector3.Distance(p0, p1) + .02f), Quaternion.LookRotation(p1 - p0));
                        }
                    foreach (var e in new[] { -1f, 1f }) ArtistBits.PostCollider(z.root, at + sd * e * 2.4f, 1.2f, .3f);
                    halos.Add(at + up * 2f, 3f, Color.white, .3f); break;
                }
                case "Roselia":
                {
                    var fire = SongGlow(z, "FIRE BIRD", new Color(1f, .45f, .15f), 2.2f); var c = at + up * 2.8f;
                    z.mk.Prism(dark, at - up * .1f, up, .06f, 2.2f, 6); ArtistBits.PostCollider(z.root, at, 2.1f, .1f);
                    z.mk.Blob(fire, c, new Vector3(.25f, .35f, .25f), ctx.Rng, 0f);
                    foreach (var e in new[] { -1f, 1f }) for (var k = 0; k < 4; k++) z.mk.Box(fire, c + sd * e * (.5f + k * .3f) + up * (.2f + k * .2f), new Vector3(.9f - k * .15f, .1f, .05f), rot * Quaternion.Euler(0, 0, e * (20f + k * 12f)));
                    for (var k = 0; k < 3; k++) z.mk.Box(fire, c - up * (.5f + k * .25f), new Vector3(.06f, .5f, .06f), rot * Quaternion.Euler(0, 0, (k - 1) * 18f));
                    halos.Add(c, 2.4f, new Color(1f, .5f, .2f), .6f); life.phoenix = c; break;
                }
                case "nomico":
                {
                    var screen = SongGlow(z, "Bad Apple", new Color(.95f, .95f, .95f), 1.4f);
                    z.mk.Box(dark, at + up * 1.6f, new Vector3(2.6f, 2f, .3f), rot); BoxCol(z.root, at + up * 1.2f, new Vector3(2.6f, 2.4f, .3f), rot);
                    z.mk.Box(screen, at + up * 1.6f + z.front * .16f, new Vector3(2.3f, 1.7f, .02f), rot);
                    z.mk.Blob(dark, at + up * 1.5f + z.front * .18f, new Vector3(.45f, .45f, .02f), ctx.Rng, 0f); z.mk.Box(dark, at + up * 2.05f + z.front * .18f, new Vector3(.06f, .25f, .02f), rot);
                    z.mk.Box(dark, at + up * 2.12f + z.front * .18f + sd * .15f, new Vector3(.25f, .1f, .02f), rot * Quaternion.Euler(0, 0, 25f));
                    halos.Add(at + up * 1.6f + z.front * .8f, 2f, Color.white, .4f); break;
                }
                case "Mai Yamane":
                {
                    var sax = SongGlow(z, "The Real Folk Blues", new Color(.4f, .6f, 1f), 2.2f); var c = at + up * 2.5f;
                    z.mk.Prism(dark, at - up * .1f, up, .06f, 2f, 6); ArtistBits.PostCollider(z.root, at, 1.9f, .1f);
                    z.mk.Box(sax, c, new Vector3(.12f, 1f, .05f), rot * Quaternion.Euler(0, 0, 15f)); z.mk.Box(sax, c - up * .55f + sd * .25f, new Vector3(.5f, .12f, .05f), rot * Quaternion.Euler(0, 0, -25f));
                    z.mk.Prism(sax, c - up * .4f + sd * .5f, (up + sd).normalized, .2f, .25f, 10, 1.6f);
                    ArtistBits.Label(z.labels, "BLUES", c + up * .9f + z.front * .05f, z.front, .3f, new Color(.5f, .7f, 1f), TextAnchor.MiddleCenter);
                    halos.Add(c, 1.8f, new Color(.4f, .6f, 1f), .5f); break;
                }
                case "Yorushika":
                {
                    var sun = SongGlow(z, "ただ君に晴れ", new Color(1f, .8f, .35f), 2f);
                    z.mk.Box(metal, at + up * 1.3f - z.front * .6f, new Vector3(2.6f, 2.6f, .08f), rot); z.mk.Box(metal, at + up * 2.65f, new Vector3(2.8f, .08f, 1.4f), rot);
                    z.mk.Box(wood, at + up * .45f - z.front * .2f, new Vector3(2f, .08f, .45f), rot); BoxCol(z.root, at + up * 1.3f - z.front * .4f, new Vector3(2.8f, 2.6f, .9f), rot);
                    var pole = at + sd * 1.8f; z.mk.Prism(metal, pole - up * .1f, up, .05f, 2.6f, 6); z.mk.Prism(sun, pole + up * 2.8f, z.front, .35f, .05f, 16); ArtistBits.PostCollider(z.root, pole, 2.5f, .08f);
                    halos.Add(pole + up * 2.8f, 1.4f, new Color(1f, .8f, .35f), .55f); break;
                }
                case "ASIAN KUNG-FU GENERATION": case "Supercar":
                {
                    var rail = at; for (var k = -2; k <= 2; k++) { var p = rail + sd * k * 1.1f; z.mk.Prism(metal, p - up * .1f, up, .04f, 1.1f, 6); }
                    z.mk.Box(metal, rail + up * 1f, new Vector3(4.6f, .06f, .06f), rot); BoxCol(z.root, rail + up * .5f, new Vector3(4.6f, 1f, .2f), rot);
                    var bike = rail - z.front * .5f; var frame = SongGlow(z, z.a.name == "Supercar" ? "YUMEGIWA" : "遥か", new Color(.4f, .8f, 1f), 1.4f);
                    foreach (var e in new[] { -.6f, .6f }) for (var k = 0; k < 10; k++) { var a = k * Mathf.PI / 5f; z.mk.Box(dark, bike + sd * e + up * .36f + (up * Mathf.Sin(a) + sd * Mathf.Cos(a)) * .34f, new Vector3(.12f, .04f, .04f), Quaternion.LookRotation(z.front, up * Mathf.Cos(a) - sd * Mathf.Sin(a))); }
                    z.mk.Box(frame, bike + up * .6f, new Vector3(1.1f, .05f, .05f), Quaternion.LookRotation(sd)); z.mk.Box(frame, bike + up * .75f - sd * .2f, new Vector3(.05f, .5f, .05f), rot);
                    halos.Add(bike + up * .6f, 1f, new Color(.4f, .8f, 1f), .4f); break;
                }
                case "King Gnu":
                {
                    var sign = SongGlow(z, "SPECIALZ", new Color(1f, .15f, .2f), 2.4f);
                    z.mk.Prism(dark, at - up * .1f, up, .07f, 2.4f, 6); ArtistBits.PostCollider(z.root, at, 2.3f, .1f);
                    z.mk.Box(dark, at + up * 2.8f, new Vector3(2.6f, .8f, .2f), rot);
                    foreach (var y in new[] { -.4f, .4f }) z.mk.Box(sign, at + up * (2.8f + y) + z.front * .11f, new Vector3(2.6f, .06f, .04f), rot);
                    ArtistBits.Label(z.labels, "SPECIALZ", at + up * 2.8f + z.front * .12f, z.front, .3f, new Color(1f, .3f, .3f), TextAnchor.MiddleCenter);
                    halos.Add(at + up * 2.8f + z.front * .6f, 1.8f, new Color(1f, .2f, .2f), .5f); break;
                }
                case "Mitsukiyo": case "technoplanet": case "Feryquitous":
                {
                    var screen = SongGlow(z, "", Cyan, 1.8f);
                    var body = WorldMaterials.Solid(new Color(.15f, .12f, .22f));
                    z.mk.Box(body, at + up * 1f, new Vector3(.9f, 1.9f, .8f), rot); BoxCol(z.root, at + up * 1f, new Vector3(.9f, 1.9f, .8f), rot);
                    z.mk.Box(screen, at + up * 1.4f + z.front * .38f, new Vector3(.65f, .55f, .04f), rot * Quaternion.Euler(-12f, 0, 0));
                    z.mk.Box(GlowM(Pink, 2.2f), at + up * 1.95f + z.front * .3f, new Vector3(.85f, .22f, .04f), rot);
                    halos.Add(at + up * 1.5f + z.front * .6f, 1.2f, Cyan, .5f); break;
                }
                default: break;
            }
        }

        // ---------------------------------------------------------------- Hoshikuma Minami
        void Hoshikuma(Plot z)
        {
            var up = Vector3.up; var ctx = z.ctx; var rot = z.Rot; var sd = z.side;
            var s = z.At(-7f, 0);
            z.mk.Box(dark, s + up * .7f, new Vector3(14f, 1.4f, 7f), rot); z.solid.Box(dark, s + up * .7f, new Vector3(14f, 1.4f, 7f), rot);
            z.mk.Box(GlowM(Pink, 2.2f), s + up * 1.42f + z.front * 3.52f, new Vector3(14f, .08f, .04f), rot);
            // the giant screens behind, a wall of them
            var wall = s - z.front * 4f;
            z.mk.Box(dark, wall + up * 6f, new Vector3(18f, 12f, 1f), rot); z.solid.Box(dark, wall + up * 6f, new Vector3(18f, 12f, 1f), rot);
            var screens = new Material[3]; for (var k = 0; k < 3; k++) screens[k] = new Material(GlowM(NeonC[k * 2], 1.6f)) { name = "Stage screen" };
            life.screens = screens;
            for (var k = 0; k < 3; k++) z.mk.Box(screens[k], wall + up * 7f + z.front * .52f + sd * ((k - 1f) * 5.8f), new Vector3(5.2f, 7f, .04f), rot);
            // a lighting truss over the stage on four legs
            foreach (var e in new[] { -1f, 1f }) foreach (var f in new[] { -1f, 1f }) { var leg = s + sd * e * 7f + z.front * f * 3.4f; z.mk.Box(metal, leg + up * 4.5f, new Vector3(.3f, 9f, .3f), rot); ArtistBits.PostCollider(z.root, leg, 9f, .25f); }
            foreach (var f in new[] { -1f, 1f }) z.mk.Box(metal, s + up * 9f + z.front * f * 3.4f, new Vector3(14.3f, .3f, .3f), rot);
            for (var k = 0; k < 8; k++) { var lp = s + up * 8.6f + z.front * 3.4f + sd * ((k - 3.5f) * 1.8f); z.mk.Box(dark, lp, new Vector3(.4f, .5f, .4f), rot); var col = NeonC[k % NeonC.Length]; z.mk.Prism(GlowM(col, 3f), lp - up * .3f, -up, .15f, .05f, 8); halos.Add(lp - up * .5f, 1f, col, .5f); }
            ArtistBits.PointLight(z.root, s + up * 6f + z.front * 6f, Pink, 26f, 2.4f);
            ArtistBits.PointLight(z.root, s + up * 4f + z.front * 3f, Cyan, 18f, 1.6f);
            // the songs: a neon eye, a halo, a heart, bubbles
            var eye = SongGlow(z, "HOLLOW EYES", Cyan, 2.4f); var ec = wall + up * 12.8f + z.front * .6f;
            for (var k = 0; k < 16; k++) { float a0 = k * Mathf.PI / 8f, a1 = (k + 1) * Mathf.PI / 8f; Vector3 E(float a) => ec + sd * Mathf.Cos(a) * 2f + up * Mathf.Sin(a) * .9f; z.mk.Box(eye, (E(a0) + E(a1)) / 2f, new Vector3(.15f, .15f, Vector3.Distance(E(a0), E(a1)) + .02f), Quaternion.LookRotation(E(a1) - E(a0), z.front)); }
            z.mk.Prism(eye, ec, z.front, .45f, .05f, 16);
            var halo = SongGlow(z, "NIRVANA", new Color(1f, .9f, .5f), 2.2f); var hc = s + up * 11f;
            for (var k = 0; k < 24; k++) { float a0 = k * Mathf.PI / 12f, a1 = (k + 1) * Mathf.PI / 12f; Vector3 H(float a) => hc + sd * Mathf.Cos(a) * 3f + z.front * Mathf.Sin(a) * 3f; z.mk.Box(halo, (H(a0) + H(a1)) / 2f, new Vector3(.18f, .18f, Vector3.Distance(H(a0), H(a1)) + .02f), Quaternion.LookRotation(H(a1) - H(a0))); }
            halos.Add(hc, 4f, new Color(1f, .9f, .5f), .35f);
            var crave = SongGlow(z, "CRAVE", Pink, 2.4f); var cc = wall + up * 2.8f + z.front * .6f - sd * 7.6f;
            foreach (var e in new[] { -1f, 1f }) z.mk.Box(crave, cc + sd * e * .3f, new Vector3(.2f, 1.1f, .06f), rot * Quaternion.Euler(0, 0, e * 40f));
            foreach (var e in new[] { -1f, 1f }) z.mk.Box(crave, cc + sd * e * .37f + up * .5f, new Vector3(.5f, .2f, .06f), rot * Quaternion.Euler(0, 0, e * -20f));
            Song(z, "OXYGEN"); life.oxygenAt = s + up * 1.6f; life.oxygenSong = Find("Hoshikuma Minami", "OXYGEN");
            halos.Add(wall + up * 7f + z.front * 3f, 9f, Pink, .2f);
        }

        // ---------------------------------------------------------------- Sithu Aye
        void SithuAye(Plot z)
        {
            var up = Vector3.up; var ctx = z.ctx; var rot = z.Rot; var sd = z.side;
            var s = z.At(-6f, 0);
            z.mk.Box(concrete, s + up * .5f, new Vector3(10f, 1f, 5f), rot); z.solid.Box(concrete, s + up * .5f, new Vector3(10f, 1f, 5f), rot);
            for (var k = 0; k <= 10; k++) z.mk.Box(metal, s + up * 1.6f + sd * (k - 5f) - z.front * 2.4f, new Vector3(.05f, 1.2f, .05f), rot);
            z.mk.Box(metal, s + up * 2.2f - z.front * 2.4f, new Vector3(10f, .06f, .06f), rot);
            // amp and guitar
            var amp = s + up * 1.5f - z.front * 1.4f - sd * 2.5f; z.mk.Box(dark, amp, new Vector3(1.4f, 1f, .6f), rot); z.mk.Box(GlowM(Amber, 2.2f), amp + up * .35f + z.front * .31f, new Vector3(.08f, .08f, .02f), rot);
            var gs = s + up * 1f + sd * 2.6f - z.front * .6f; var gtr = WorldMaterials.Solid(new Color(.2f, .55f, .85f), .8f);
            z.mk.Box(metal, gs + up * .3f, new Vector3(.5f, .6f, .4f), rot); z.mk.Blob(gtr, gs + up * .95f, new Vector3(.4f, .5f, .12f), ctx.Rng, 0f); z.mk.Box(woodDark, gs + up * 1.75f, new Vector3(.08f, 1.1f, .05f), rot);
            // the new season: a banner and a cut-out of the new character
            var newSeason = SongGlow(z, "It's the Second Season", Pink, 1.4f);
            foreach (var e in new[] { -1f, 1f }) { var p = s + sd * e * 4.8f - z.front * 2.4f; z.mk.Prism(metal, p, up, .06f, 4.6f, 6); ArtistBits.PostCollider(z.root, p, 4.4f, .1f); }
            z.mk.Box(cloth(), s + up * 4.4f - z.front * 2.45f, new Vector3(9.4f, 1.4f, .04f), rot);
            z.mk.Box(newSeason, s + up * 4.4f - z.front * 2.42f, new Vector3(9f, .12f, .02f), rot);
            ArtistBits.Label(z.labels, "SEASON 2", s + up * 4.45f - z.front * 2.4f, z.front, .55f, Pink, TextAnchor.MiddleCenter);
            var cut = s + up * 1f + sd * .6f - z.front * .8f; var skin = WorldMaterials.Solid(new Color(.98f, .86f, .78f)); var hair = WorldMaterials.Solid(new Color(.55f, .35f, .85f));
            z.mk.Box(WorldMaterials.Solid(new Color(.2f, .25f, .45f)), cut + up * .9f, new Vector3(.6f, 1f, .05f), rot); z.mk.Box(skin, cut + up * 1.65f, new Vector3(.42f, .5f, .05f), rot);
            z.mk.Box(hair, cut + up * 1.95f, new Vector3(.55f, .3f, .06f), rot); z.mk.Box(hair, cut + up * 1.4f - sd * .3f, new Vector3(.12f, .8f, .06f), rot);
            foreach (var e in new[] { -.1f, .1f }) z.mk.Box(GlowM(new Color(.4f, .8f, 1f), 1.4f), cut + up * 1.7f + sd * e + z.front * .03f, new Vector3(.08f, .1f, .01f), rot);
            z.mk.Box(woodDark, cut + up * .7f - z.front * .25f, new Vector3(.05f, 1.4f, .5f), rot * Quaternion.Euler(-20f, 0, 0));
            // "The Noticing!": a big glowing exclamation mark
            var ex = SongGlow(z, "The Noticing", Amber, 2.4f); var ep = s + up * 2.6f + sd * 4.4f;
            z.mk.Box(ex, ep + up * 1.4f, new Vector3(.45f, 1.9f, .3f), rot); z.mk.Box(ex, ep, new Vector3(.45f, .45f, .3f), rot);
            halos.Add(ep + up * .9f, 2f, Amber, .55f);
            // "Skye": a kite flying high (JRockLife)
            life.kiteAt = s + up * 26f - z.front * 12f; life.kiteSong = Find("Sithu Aye", "Skye"); Song(z, "Skye");
            var kk = new MeshKit(); var kiteM = new Material(GlowM(new Color(.45f, .8f, 1f), 1.4f)) { name = "Kite" };
            kk.Box(kiteM, Vector3.zero, new Vector3(1.6f, 2.2f, .04f), Quaternion.Euler(0, 0, 45f)); kk.Box(GlowM(Pink, 1.6f), -Vector3.up * 1.6f, new Vector3(.06f, 2f, .02f), Quaternion.identity);
            var kg = kk.BuildSingle("Skye kite", life.transform, false); if (kg != null) { kg.transform.position = life.kiteAt; life.kite = kg.transform; life.kiteString = s + up * 1.2f; }
            halos.Add(s + up * 3f, 5f, Pink, .2f);
            ArtistBits.PointLight(z.root, s + up * 4f + z.front * 4f, new Color(1f, .75f, .85f), 18f, 2f);
        }

        Material cloth() => WorldMaterials.Solid(new Color(.95f, .93f, .9f));
    }
}
