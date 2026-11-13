using System.Collections.Generic;
using UnityEngine;
using static EverythingWorld.World.PlaceTools;

namespace EverythingWorld.World
{
    /// The Heartbreak Boardwalk's artists. Most have a beach hut of their own: a faded hut with its window lit, a deck
    /// chair under a striped umbrella, and one thing from their song; covers on a cream card on a weathered board.
    /// - Lana Del Rey: a seaside bungalow with a porch swing, a white convertible ("West Coast"), a painter's easel
    ///   ("Norman fucking Rockwell"), a lemon tree ("Salvatore"), a white wedding arch ("Say Yes To Heaven"), a
    ///   butterfly as big as a door ("Happiness is a butterfly"), a black rose ("Black Beauty"), a ship's lantern on a
    ///   mast ("Mariners Apartment Complex"), a record player on a stoop ("Brooklyn Baby"), a cinnamon candle
    ///   ("Cinnamon Girl"), a lawn chair in the heat ("Summertime Sadness"), a vintage microphone ("Ultraviolence").
    /// - Olivia Rodrigo: a row of school lockers, a purple car with the door open ("good 4 u"), an ashtray smoking
    ///   ("cigarette smoke"), an upright piano ("All I Want"), a picnic with an apple pie ("all-american bitch").
    /// - Ashe: a wedding aisle with a veil on the ground ("Stop The Wedding!"), a lasso round a moon on a pole ("Lasso
    ///   The Moon"), a little wreath of flowers ("Funeral").
    /// - Billie Eilish: a pool of glowing sea-blue ("ocean eyes"), a parked car at night with a guitar on the roof
    ///   ("Happier Than Ever"), one dark spotlight ("No Time To Die").
    public partial class HeartKit
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
                Add("Lana Del Rey", "Summertime Sadness", "@the promenade"); Add("Lana Del Rey", "Ultraviolence", "@down the boardwalk"); Add("Billie Eilish", "ocean eyes", "@the pier at sunset");
                Add("Billie Eilish", "Happier Than Ever", "@the promenade"); Add("OneRepublic", "Counting Stars", "@the carousel"); Add("Lana Del Rey", "Happiness is a butterfly", "Lana Del Rey");
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
            if (boards == null) MakeMaterials();
            var root = new GameObject("Artist: " + a.name).transform; root.SetParent(ctx.Root, false);
            var front = facing.TryGetValue(a.name, out var f) ? f : new Vector3(center.x - a.x, 0, center.z - a.z).normalized;
            if (front.sqrMagnitude < .01f) front = Vector3.forward;
            var labels = new GameObject("Labels").transform; labels.SetParent(root, false);
            var area = root.gameObject.AddComponent<ArtistArea>();
            area.artistName = a.name; area.stationId = a.stationId; area.radius = a.r; area.glowColor = Rose;
            var z = new Plot { ctx = ctx, root = root, labels = labels, mk = new MeshKit(), solid = new MeshKit(), pictures = new MeshKit(), a = a, c = ctx.OnGround(a.x, a.z), front = front, side = Vector3.Cross(Vector3.up, front), area = area };
            var reach = Foot(a); var up = Vector3.up;
            z.mk.Prism(boards, z.c - up * .25f, up, reach * .66f, .33f, 24);
            switch (a.name)
            {
                case "Lana Del Rey": Lana(z); break;
                case "Olivia Rodrigo": Olivia(z); break;
                case "Ashe": Ashe(z); break;
                case "Billie Eilish": Billie(z); break;
                default: Others(z, Hut(z)); break;
            }
            SongLamps(z);
            Board(z, z.At(reach * .6f, -reach * .45f - 1.5f), front);
            ArtistBits.Plaque(z.mk, z.solid, labels, a, z.At(reach * .6f + 1.5f, reach * .45f + 1.5f), front, woodM, Paint(Cream), Ink, 1.6f);
            GlobeLamp(ctx, z.mk, z.At(reach * .5f, reach * .68f));
            if (z.area.glowLight == null) z.area.glowLight = ArtistBits.PointLight(root, z.c + up * 4f + front * 3f, new Color(1f, .82f, .7f), HasStation(a) ? 24f : 15f, 2f);
            area.glowRenderers = new Renderer[0];
            z.mk.Build("Area", root, false);
            z.solid.BuildCollider("Walls", root);
            z.pictures.Build("Covers", root, false);
            noGrass.Add(new Vector3(z.c.x, reach * .9f, z.c.z));
            var mark = new GameObject("Shot: " + a.name + " up close").transform; mark.SetParent(ctx.Root, false);
            var cam = z.At(reach * .85f, reach * .3f) + up * 2f; var look = z.At(-reach * .3f, 0) + up * 1.8f - cam; mark.SetPositionAndRotation(cam, Quaternion.LookRotation(look.normalized));
        }

        /// The covers on a cream card on a weathered board under a little striped roof.
        void Board(Plot z, Vector3 ground, Vector3 dir)
        {
            dir = Vector3.ProjectOnPlane(dir, Vector3.up).normalized; var rot = Quaternion.LookRotation(dir); var side = Vector3.Cross(Vector3.up, dir);
            var n = Mathf.Max(1, Mathf.Min(7, z.a.songList?.Length ?? 1)); const float size = .9f; var w = n * size * 1.25f + .6f;
            var c = ground + Vector3.up * 1.7f;
            z.mk.Box(woodM, c - Vector3.up * .5f - dir * .06f, new Vector3(w + .5f, size + 2f, .2f), rot); z.solid.Box(woodM, c - Vector3.up * .5f, new Vector3(w + .5f, size + 2f, .3f), rot); z.mk.Box(Paint(Cream), c - Vector3.up * .1f + dir * .07f, new Vector3(w, size + .9f, .04f), rot);
            var ns = Mathf.Max(2, Mathf.RoundToInt((w + 1f) / .5f)); for (var k = 0; k < ns; k++) z.mk.Box(k % 2 == 0 ? stripeA : stripeB, c + Vector3.up * (size / 2f + .7f) + side * (-(w + 1f) / 2f + (k + .5f) * (w + 1f) / ns) + dir * .3f, new Vector3((w + 1f) / ns, .06f, .9f), rot * Quaternion.Euler(14f, 0, 0));
            ArtistBits.CoverWall(z.pictures, z.labels, z.ctx, z.a, c + dir * .1f + Vector3.up * .08f, dir, n, size, n, true, Ink);
        }

        /// A little lantern on a post for each song without its own piece.
        void SongLamps(Plot z)
        {
            if (!SongIds.TryGetValue(z.a.name, out var list)) return;
            var up = Vector3.up; var k = 0; var seen = new HashSet<string>();
            foreach (var (_, id) in list)
            {
                if (z.lit.Contains(id) || !seen.Add(id)) continue;
                var p = z.At(Foot(z.a) * .55f - (k % 2) * 1.3f, Foot(z.a) * .2f + (k / 2) * 1.3f);
                z.mk.Prism(iron, p - up * .1f, up, .035f, 1.3f, 5); z.mk.Blob(SongGlow(id, Rose, 1.6f), p + up * 1.42f, Vector3.one * .14f, new System.Random(k), 0f); ArtistBits.PostCollider(z.root, p, 1.3f, .08f);
                k++;
            }
        }

        /// A beach hut of their own: a faded hut with its window lit, a deck chair under a striped umbrella.
        PlaceFrame Hut(Plot z)
        {
            var up = Vector3.up; var hue = z.a.name.Length + z.a.name[0]; var f = new PlaceFrame(z.At(-Foot(z.a) * .5f, 0), z.front); var wall = Paint(Faded[hue % Faded.Length]);
            z.mk.Box(wall, f.P(0, 1.4f, -.4f), new Vector3(3.2f, 2.8f, 2.4f), f.r); z.solid.Box(wall, f.P(0, 1.4f, -.4f), new Vector3(3.2f, 2.8f, 2.4f), f.r); z.mk.GableRoof(Paint(new Color(.5f, .44f, .5f)), f.P(0, 2.8f, -.4f), new Vector3(2.8f, 0, 3.6f), 1f, Quaternion.LookRotation(f.Right));
            z.mk.Box(woodM, f.P(-.7f, 1f, .81f), new Vector3(.8f, 2f, .04f), f.r); z.mk.Box(windowM, f.P(.8f, 1.5f, .81f), new Vector3(.9f, .7f, .04f), f.r); halos.Add(f.P(.8f, 1.5f, 1.3f), 1f, new Color(1f, .78f, .5f), .3f);
            var um = f.P(2.6f, 0, 1.4f); z.mk.Prism(white, um, up, .03f, 2.2f, 4); for (var k = 0; k < 8; k++) { var p0 = um + up * 2f + Quaternion.Euler(0, k * 45f, 0) * Vector3.forward * 1.2f; var p1 = um + up * 2f + Quaternion.Euler(0, k * 45f + 45f, 0) * Vector3.forward * 1.2f; z.mk.Tri(k % 2 == 0 ? stripeA : stripeB, um + up * 2.4f, p0, p1, um); } ArtistBits.PostCollider(z.root, um, 2f, .05f);
            var dc = um + f.Fwd * .4f - f.Right * .4f; z.mk.Box(stripeA, dc + up * .3f, new Vector3(.6f, .05f, 1.4f), f.r * Quaternion.Euler(-10f, 0, 0)); z.mk.Box(stripeA, dc + up * .66f - f.Fwd * .66f, new Vector3(.6f, .05f, .7f), f.r * Quaternion.Euler(-55f, 0, 0)); Interact.Seat(tlife.transform, dc + up * .4f, f.Fwd, "Lie back").seatHeight = .3f;
            return f;
        }

        // ---------------------------------------------------------------- Lana Del Rey
        void Lana(Plot z)
        {
            var up = Vector3.up; var rot = z.Rot; var rng = new System.Random(2014); var fr = z.front; var sd = z.side;
            var bb = Song(z, "Black Beauty"); var br = Song(z, "Brooklyn Baby"); var cg = Song(z, "Cinnamon Girl"); var hb = Song(z, "Happiness is a butterfly"); var ma = Song(z, "Mariners"); var nr = Song(z, "Norman"); var sa = Song(z, "Salvatore"); var sy = Song(z, "Say Yes"); var ss = Song(z, "Summertime Sadness"); var uv = Song(z, "Ultraviolence"); var wc = Song(z, "West Coast");
            life.sadSong = ss; life.violenceSong = uv; life.westSong = wc; life.butterflySong = hb;
            // the bungalow: white clapboard, a deep porch with a swing, a flag of bunting
            var f = new PlaceFrame(z.At(-6.5f, 0), fr); mk2(z, f);
            ArtistBits.Label(z.labels, "lana", f.P(0, 3.95f, 2.1f), fr, .3f, Neon, TextAnchor.MiddleCenter); z.area.glowLight = ArtistBits.PointLight(z.root, f.P(0, 3f, 4f), new Color(1f, .8f, .7f), 26f, 2.2f);
            // "Ultraviolence": a vintage microphone on the porch, violet light; "Brooklyn Baby": a record player on the step
            var mc = f.P(1.6f, .7f, 2.3f); z.mk.Prism(iron, mc, up, .02f, 1.4f, 4); z.mk.Prism(iron, mc, up, .16f, .02f, 8); z.mk.Box(SongGlow(uv, Violet, 1.6f), mc + up * 1.5f, new Vector3(.14f, .2f, .08f), rot); halos.Add(mc + up * 1.5f, 1f, Violet, .5f);
            var rp = f.P(-1.8f, .45f, 3.4f); z.mk.Box(woodM, rp, new Vector3(.6f, .16f, .5f), rot); z.mk.Prism(dark, rp + up * .09f, up, .22f, .01f, 14); z.mk.Prism(SongGlow(br, new Color(1f, .9f, .6f), 1.2f), rp + up * .1f, up, .07f, .01f, 10);
            // "West Coast": a white convertible
            var cv = z.At(4f, -9f) + up * .05f; var cr = rot * Quaternion.Euler(0, 70f, 0); z.mk.Box(white, cv + up * .55f, new Vector3(1.8f, .5f, 4.6f), cr); z.mk.Box(Paint(new Color(.7f, .2f, .26f)), cv + up * .74f - cr * Vector3.forward * .3f, new Vector3(1.6f, .1f, 2f), cr); z.mk.Box(Paint(new Color(.6f, .8f, .9f)), cv + up * 1f + cr * Vector3.forward * .7f, new Vector3(1.6f, .4f, .06f), cr * Quaternion.Euler(-20f, 0, 0)); foreach (var (x, zz) in new[] { (-.9f, 1.5f), (.9f, 1.5f), (.9f, -1.5f), (-.9f, -1.5f) }) z.mk.Prism(dark, cv + up * .3f + cr * new Vector3(x - Mathf.Sign(x) * .1f, 0, zz), cr * Vector3.right * Mathf.Sign(x), .3f, .2f, 12); foreach (var e in new[] { -.6f, .6f }) z.mk.Blob(SongGlow(wc, new Color(1f, .9f, .7f), 1.6f), cv + up * .6f + cr * new Vector3(e, 0, 2.31f), new Vector3(.14f, .1f, .03f), rng, 0f); BoxCol(z.root, cv + up * .6f, new Vector3(1.9f, 1.2f, 4.7f), cr); Interact.Seat(tlife.transform, cv + up * .8f - cr * Vector3.forward * .2f + cr * Vector3.right * .4f, cr * Vector3.forward, "Sit in the convertible").seatHeight = .5f;
            // "Summertime Sadness": a lawn chair in the heat, a fan, a glass
            var lc = z.At(6f, 6f) + up * .05f; z.mk.Box(stripeB, lc + up * .3f, new Vector3(.6f, .05f, 1.4f), rot * Quaternion.Euler(-10f, 30f, 0)); z.mk.Box(stripeB, lc + up * .66f - Quaternion.Euler(0, 30f, 0) * fr * .66f, new Vector3(.6f, .05f, .7f), rot * Quaternion.Euler(-55f, 30f, 0)); Interact.Seat(tlife.transform, lc + up * .4f, Quaternion.Euler(0, 30f, 0) * fr, "Lie back in the heat").seatHeight = .3f; z.mk.Prism(SongGlow(ss, new Color(1f, .7f, .3f), 1.2f), lc + sd * .7f, up, .05f, .18f, 6);
            // "Say Yes To Heaven": a white arch of flowers; "Happiness is a butterfly": a butterfly as big as a door
            var ar = z.At(9f, -1f); foreach (var e in new[] { -1f, 1f }) { z.mk.Prism(white, ar + sd * e * 1.2f - up * .2f, up, .07f, 2.4f, 5); ArtistBits.PostCollider(z.root, ar + sd * e * 1.2f, 2.2f, .08f); } for (var k = 0; k <= 12; k++) { var a = Mathf.PI * k / 12f; z.mk.Blob(k % 3 == 0 ? SongGlow(sy, White, 1.2f) : Paint(new Color(1f, .9f, .92f)), ar + up * (2.2f + Mathf.Sin(a) * 1f) + sd * Mathf.Cos(a) * 1.2f, Vector3.one * .16f, rng, .2f); }
            var bf = z.At(-1f, 11f) + up * 1.6f; var bm = SongGlow(hb, new Color(.7f, .85f, 1f), 1.2f); foreach (var e in new[] { -1f, 1f }) { z.mk.Tri(bm, bf, bf + sd * e * 1.3f + up * .9f, bf + sd * e * 1.1f - up * .1f, bf - fr * 3f); z.mk.Tri(bm, bf, bf + sd * e * 1.3f + up * .9f, bf + sd * e * 1.1f - up * .1f, bf + fr * 3f); z.mk.Tri(bm, bf, bf + sd * e * .9f - up * .9f, bf + sd * e * 1.1f - up * .1f, bf - fr * 3f); z.mk.Tri(bm, bf, bf + sd * e * .9f - up * .9f, bf + sd * e * 1.1f - up * .1f, bf + fr * 3f); } z.mk.Prism(dark, bf - up * .5f, up, .04f, 1f, 5); z.mk.Prism(iron, bf - up * 1.6f, up, .02f, 1.1f, 4); halos.Add(bf, 1.6f, new Color(.7f, .85f, 1f), .3f);
            // "Norman fucking Rockwell": a painter's easel; "Salvatore": a lemon tree in a pot; "Black Beauty": a black rose
            var es = z.At(-3f, -11f); foreach (var e in new[] { -.4f, .4f }) z.mk.Prism(woodM, es + sd * e, (up + sd * -e * .2f).normalized, .03f, 1.9f, 4); z.mk.Box(SongGlow(nr, new Color(.6f, .74f, .9f), .7f), es + up * 1.3f + fr * .06f, new Vector3(1f, .8f, .04f), rot * Quaternion.Euler(-10f, 0, 0)); ArtistBits.PostCollider(z.root, es, 1.6f, .3f);
            var lt = z.At(3f, 10f) + up * .05f; z.mk.Prism(Paint(new Color(.74f, .46f, .36f)), lt, up, .4f, .6f, 8, 1.2f); z.mk.Prism(trunkM, lt + up * .5f, up, .07f, 1.2f, 5); z.mk.Blob(leafM, lt + up * 2f, new Vector3(.9f, .7f, .9f), rng, .2f); for (var k = 0; k < 6; k++) z.mk.Blob(k == 0 ? SongGlow(sa, new Color(1f, .9f, .3f), 1.2f) : Paint(new Color(1f, .9f, .36f)), lt + up * (1.8f + (k % 3) * .2f) + Quaternion.Euler(0, k * 60f, 0) * Vector3.forward * .8f, new Vector3(.1f, .08f, .08f), rng, 0f); ArtistBits.PostCollider(z.root, lt, 1f, .45f);
            var rs = z.At(10f, 4f); z.mk.Prism(leafDark, rs, up, .02f, 1.1f, 4); z.mk.Blob(SongGlow(bb, new Color(.3f, .16f, .3f), .6f), rs + up * 1.16f, new Vector3(.16f, .12f, .16f), rng, .3f);
            // "Mariners Apartment Complex": a ship's lantern on a mast; "Cinnamon Girl": a candle on the porch rail
            var ms = z.At(-8f, 10f); z.mk.Prism(woodM, ms - up * .2f, up, .1f, 4.6f, 6); z.mk.Prism(woodM, ms + up * 3.6f, sd, .05f, 1.6f, 4); z.mk.Box(SongGlow(ma, new Color(1f, .8f, .4f), 1.8f), ms + up * 3.2f + sd * 1.4f, new Vector3(.24f, .34f, .24f), rot); halos.Add(ms + up * 3.2f + sd * 1.4f, 1.2f, new Color(1f, .8f, .4f), .5f); ArtistBits.PostCollider(z.root, ms, 4.4f, .12f);
            var cn = f.P(2.6f, 1.6f, 3.6f); z.mk.Prism(Paint(new Color(.66f, .34f, .2f)), cn, up, .06f, .14f, 8); z.mk.Blob(SongGlow(cg, new Color(1f, .6f, .3f), 2f), cn + up * .2f, new Vector3(.025f, .05f, .025f), rng, 0f);
            Folk(f.P(-1f, .7f, 2.6f), fr, Resident.Pose.Lean, new[] { "Come up on the porch, baby. The sun's doing the thing.", "The radio only plays slow songs after six. It's a rule. Nobody made it.", "Everyone's a little bit heartbroken here. It suits them." });
            Shot(z.ctx, "lana's bungalow", z.At(14f, 5f) + up * 2.2f, f.P(0, 2f, 0));
        }

        /// Lana's bungalow (split out so the method above stays readable).
        void mk2(Plot z, PlaceFrame f)
        {
            var up = Vector3.up; const float W = 9f, Dp = 6f, H = 3.6f;
            foreach (var (x, zz) in new[] { (-1f, -1f), (1f, -1f), (1f, 1f), (-1f, 1f) }) z.mk.Prism(woodM, f.P(x * (W / 2f - .3f), -.3f, zz * (Dp / 2f - .3f)), up, .14f, 1f, 5);
            z.mk.Box(white, f.P(0, .7f + H / 2f, 0), new Vector3(W, H, Dp), f.r); z.solid.Box(white, f.P(0, .1f + H / 2f, 0), new Vector3(W, H + 1.2f, Dp), f.r); for (var k = 0; k < 9; k++) z.mk.Box(Paint(new Color(.88f, .86f, .84f)), f.P(0, .9f + k * .38f, Dp / 2f + .01f), new Vector3(W, .04f, .02f), f.r);
            z.mk.GableRoof(Paint(new Color(.42f, .5f, .6f)), f.P(0, .7f + H, 0), new Vector3(Dp + 1f, 0, W + 1f), 1.8f, Quaternion.LookRotation(f.Right));
            z.mk.Box(boards, f.P(0, .7f, Dp / 2f + 1.4f), new Vector3(W, .12f, 2.8f), f.r); z.solid.Box(boards, f.P(0, .3f, Dp / 2f + 1.4f), new Vector3(W, .8f, 2.8f), f.r); for (var k = 0; k < 2; k++) z.mk.Box(boards, f.P(0, .2f + k * .26f, Dp / 2f + 3f + (1 - k) * .3f), new Vector3(2f, .1f, .34f), f.r);
            foreach (var e in new[] { -1f, 1f }) { z.mk.Box(white, f.P(e * (W / 2f - .2f), 2.4f, Dp / 2f + 2.7f), new Vector3(.12f, 3.4f, .12f), f.r); ArtistBits.PostCollider(z.root, f.P(e * (W / 2f - .2f), 0, Dp / 2f + 2.7f), 3.4f, .1f); }
            z.mk.Box(Paint(new Color(.42f, .5f, .6f)), f.P(0, 4.1f, Dp / 2f + 1.4f), new Vector3(W + .2f, .1f, 3f), f.r * Quaternion.Euler(8f, 0, 0));
            z.mk.Box(woodM, f.P(0, 1.75f, Dp / 2f + .02f), new Vector3(1f, 2f, .04f), f.r); foreach (var e in new[] { -2.6f, 2.6f }) { z.mk.Box(windowM, f.P(e, 2.2f, Dp / 2f + .02f), new Vector3(1.4f, 1.1f, .04f), f.r); halos.Add(f.P(e, 2.2f, Dp / 2f + .5f), 1.3f, new Color(1f, .78f, .5f), .3f); }
            var sw = f.P(-2.8f, .76f, Dp / 2f + 2f); z.mk.Box(woodM, sw + up * .5f, new Vector3(1.6f, .06f, .5f), f.r); z.mk.Box(woodM, sw + up * .8f - f.Fwd * .24f, new Vector3(1.6f, .5f, .05f), f.r); foreach (var e in new[] { -.75f, .75f }) z.mk.Prism(dark, sw + up * .5f + f.Right * e, up, .01f, 2.6f, 3); Interact.Seat(tlife.transform, sw + up * .55f, f.Fwd, "Sit on the porch swing").seatHeight = .5f;
            for (var k = 0; k < 9; k++) z.mk.Tri(k % 3 == 0 ? Paint(new Color(.7f, .2f, .26f)) : k % 3 == 1 ? Paint(Cream) : Paint(new Color(.3f, .36f, .6f)), f.P(-W / 2f + .5f + k, 3.9f, Dp / 2f + 2.75f), f.P(-W / 2f + 1.5f + k, 3.9f, Dp / 2f + 2.75f), f.P(-W / 2f + 1f + k, 3.5f, Dp / 2f + 2.75f), f.P(0, 3.7f, Dp / 2f + 5f));
            Mark(f.o, 7f);
        }

        // ---------------------------------------------------------------- Olivia Rodrigo
        void Olivia(Plot z)
        {
            var up = Vector3.up; var rot = z.Rot; var rng = new System.Random(2021); var fr = z.front; var sd = z.side; var cs = Song(z, "cigarette smoke"); var g4 = Song(z, "good 4 u"); var ai = Song(z, "All I Want"); var ab = Song(z, "all-american"); life.goodSong = g4;
            // a row of school lockers, purple, covered in stickers; a bench in front
            var lk = new PlaceFrame(z.At(-5f, 0), fr); for (var k = 0; k < 8; k++) { var lp = lk.P(-3.15f + k * .9f, 1.1f, 0); z.mk.Box(Paint(new Color(.5f, .34f, .7f)), lp, new Vector3(.86f, 2.2f, .6f), lk.r); z.mk.Box(dark, lp + fr * .31f + up * .7f, new Vector3(.5f, .04f, .01f), lk.r); z.mk.Box(dark, lp + fr * .31f + up * .6f, new Vector3(.5f, .04f, .01f), lk.r); if (k % 2 == 0) z.mk.Box(Paint(new[] { Rose, Mint, new Color(1f, .9f, .4f), Cream }[k / 2]), lp + fr * .31f - up * .2f + sd * .1f, new Vector3(.24f, .24f, .01f), lk.r * Quaternion.Euler(0, 0, k * 9f)); } z.solid.Box(woodM, lk.P(0, 1.1f, 0), new Vector3(7.2f, 2.2f, .6f), lk.r);
            ArtistBits.Label(z.labels, "SOUR", lk.P(0, 2.5f, .31f), fr, .3f, new Color(.7f, .5f, 1f), TextAnchor.MiddleCenter); Bench(z.root, z.mk, lk.P(0, 0, 1.4f), fr, 3f); z.area.glowLight = ArtistBits.PointLight(z.root, lk.P(0, 3f, 3f), new Color(.86f, .7f, 1f), 22f, 2.2f);
            // "good 4 u": a purple car with the door flung open
            var cv = z.At(4f, -7f) + up * .05f; var cr = rot * Quaternion.Euler(0, 60f, 0); var pm = SongGlow(g4, new Color(.6f, .3f, .9f), .6f); z.mk.Box(pm, cv + up * .6f, new Vector3(1.8f, .6f, 4.2f), cr); z.mk.Box(pm, cv + up * 1.1f - cr * Vector3.forward * .3f, new Vector3(1.6f, .5f, 2f), cr); z.mk.Box(Paint(new Color(.5f, .6f, .7f)), cv + up * 1.12f - cr * Vector3.forward * .3f, new Vector3(1.64f, .36f, 1.8f), cr); z.mk.Box(pm, cv + up * .8f + cr * new Vector3(-1.4f, 0, .3f), new Vector3(.06f, .8f, 1.1f), cr * Quaternion.Euler(0, -55f, 0)); foreach (var (x, zz) in new[] { (-.9f, 1.4f), (.9f, 1.4f), (.9f, -1.4f), (-.9f, -1.4f) }) z.mk.Prism(dark, cv + up * .3f + cr * new Vector3(x - Mathf.Sign(x) * .1f, 0, zz), cr * Vector3.right * Mathf.Sign(x), .3f, .2f, 12); BoxCol(z.root, cv + up * .7f, new Vector3(1.9f, 1.4f, 4.3f), cr);
            // "cigarette smoke": an ashtray on a stool, a thread of smoke; "All I Want": an upright piano; "all-american bitch": a picnic, an apple pie
            var at = z.At(6f, 2f) + up * .05f; z.mk.Prism(woodM, at + up * .5f, up, .2f, .04f, 8); z.mk.Prism(iron, at, up, .03f, .5f, 4); z.mk.Prism(Paint(new Color(.6f, .6f, .64f)), at + up * .54f, up, .1f, .04f, 8); z.mk.Prism(white, at + up * .58f, sd, .01f, .14f, 4); z.mk.Blob(SongGlow(cs, new Color(1f, .4f, .2f), 1.8f), at + up * .58f + sd * .14f, Vector3.one * .012f, rng, 0f); life.smokes.Add(at + up * .7f + sd * .14f); Interact.Seat(tlife.transform, at + up * .56f, -fr, "Sit on the stool").seatHeight = .5f;
            var pn = z.At(-1f, 8f) + up * .05f; z.mk.Box(Paint(new Color(.3f, .2f, .18f)), pn + up * .7f, new Vector3(1.6f, 1.4f, .6f), rot); z.mk.Box(white, pn + up * .76f + fr * .32f, new Vector3(1.5f, .06f, .2f), rot); z.mk.Box(SongGlow(ai, Cream, .5f), pn + up * 1.2f + fr * .32f, new Vector3(.5f, .4f, .02f), rot); BoxCol(z.root, pn + up * .7f, new Vector3(1.6f, 1.4f, .6f), rot);
            var pc = z.At(2f, 10f) + up * .05f; for (var x = 0; x < 4; x++) for (var y = 0; y < 4; y++) z.mk.Box((x + y) % 2 == 0 ? Paint(new Color(.8f, .2f, .26f)) : Paint(Cream), pc + up * .01f + sd * (-.6f + x * .4f) + fr * (-.6f + y * .4f), new Vector3(.4f, .01f, .4f), rot); z.mk.Prism(SongGlow(ab, new Color(.86f, .6f, .3f), .6f), pc + up * .02f, up, .24f, .08f, 10); z.mk.Prism(white, pc + up * .02f, up, .28f, .02f, 10);
            Shot(z.ctx, "the lockers", z.At(11f, 3f) + up * 2f, lk.P(0, 1.2f, 0));
        }

        // ---------------------------------------------------------------- Ashe
        void Ashe(Plot z)
        {
            var up = Vector3.up; var rot = z.Rot; var rng = new System.Random(2022); var fr = z.front; var sd = z.side; var sw = Song(z, "Stop The Wedding"); var lm = Song(z, "Lasso The Moon"); var fu = Song(z, "Funeral"); life.weddingSong = sw; life.moonSong = lm;
            // "Stop The Wedding!": an aisle of white chairs, an arch, a veil dropped on the boards
            var al = z.At(-2f, 0); for (var row = 0; row < 4; row++) foreach (var e in new[] { -1f, 1f }) for (var k = 0; k < 2; k++) Chair(z.mk, al + fr * (row * 1.2f - 1f) + sd * e * (1.1f + k * .6f), -fr);
            for (var k = 0; k < 9; k++) z.mk.Prism(Paint(Cream), al + fr * (-1.5f + k * .6f) + up * .01f, up, .2f, .01f, 6); var ar = al - fr * 2.6f; foreach (var e in new[] { -1f, 1f }) { z.mk.Prism(white, ar + sd * e * 1.2f - up * .2f, up, .07f, 2.6f, 5); ArtistBits.PostCollider(z.root, ar + sd * e * 1.2f, 2.4f, .08f); } for (var k = 0; k <= 12; k++) { var a = Mathf.PI * k / 12f; z.mk.Blob(Paint(k % 2 == 0 ? White : new Color(1f, .86f, .9f)), ar + up * (2.4f + Mathf.Sin(a)) + sd * Mathf.Cos(a) * 1.2f, Vector3.one * .16f, rng, .2f); }
            var vl = al + fr * 2f + sd * .3f + up * .02f; var vm = SongGlow(sw, White, .6f); z.mk.Tri(vm, vl, vl + fr * 1.2f + sd * .5f, vl + fr * 1f - sd * .6f, vl - up); z.mk.Tri(vm, vl, vl + fr * 1f - sd * .6f, vl + fr * .2f - sd * .5f, vl - up);
            z.area.glowLight = ArtistBits.PointLight(z.root, al + up * 4f + fr * 2f, new Color(1f, .88f, .86f), 22f, 2f);
            // "Lasso The Moon": a moon on a tall pole with a rope looped round it
            var mp = z.At(5f, 7f); z.mk.Prism(woodM, mp - up * .2f, up, .07f, 5f, 5); ArtistBits.PostCollider(z.root, mp, 4.8f, .09f); var mo = mp + up * 5.4f; z.mk.Prism(SongGlow(lm, new Color(.9f, .92f, 1f), 1.6f), mo - fr * .06f, fr, .7f, .12f, 18); z.mk.Prism(Paint(new Color(.24f, .26f, .4f)), mo - fr * .08f + sd * .36f + up * .16f, fr, .6f, .16f, 16); for (var k = 0; k < 16; k++) { var a0 = k * 22.5f * Mathf.Deg2Rad; var a1 = (k + 1) * 22.5f * Mathf.Deg2Rad; var p0 = mo + fr * .1f + sd * Mathf.Cos(a0) * .76f + up * Mathf.Sin(a0) * .76f; var p1 = mo + fr * .1f + sd * Mathf.Cos(a1) * .76f + up * Mathf.Sin(a1) * .76f; z.mk.Prism(Paint(new Color(.8f, .66f, .4f)), p0, (p1 - p0).normalized, .02f, Vector3.Distance(p0, p1), 3); } var rope = mo + fr * .1f - up * .76f; z.mk.Prism(Paint(new Color(.8f, .66f, .4f)), rope, (z.At(6f, 4f) - rope).normalized, .02f, Vector3.Distance(rope, z.At(6f, 4f)), 3); halos.Add(mo, 2f, new Color(.9f, .92f, 1f), .4f);
            // "Funeral": a small wreath of flowers on a stand, all very dramatic
            var wr = z.At(5f, -6f); z.mk.Prism(woodM, wr, (up + fr * .1f).normalized, .03f, 1.2f, 4); for (var k = 0; k < 14; k++) { var a = k * Mathf.PI * 2f / 14f; z.mk.Blob(k % 3 == 0 ? SongGlow(fu, new Color(1f, .9f, .94f), .6f) : Paint(k % 2 == 0 ? new Color(1f, .9f, .94f) : new Color(.86f, .4f, .5f)), wr + up * 1.3f + sd * Mathf.Cos(a) * .4f + up * Mathf.Sin(a) * .4f + fr * .05f, Vector3.one * .1f, rng, .2f); } ArtistBits.PostCollider(z.root, wr, 1.2f, .06f);
            Shot(z.ctx, "the wedding aisle", z.At(10f, 4f) + up * 2f, ar + up * 1.6f);
        }

        // ---------------------------------------------------------------- Billie Eilish
        void Billie(Plot z)
        {
            var up = Vector3.up; var rot = z.Rot; var rng = new System.Random(2019); var fr = z.front; var sd = z.side; var oe = Song(z, "ocean eyes"); var ht = Song(z, "Happier Than Ever"); var nt = Song(z, "No Time To Die"); life.oceanSong = oe; life.happierSong = ht; life.dieSong = nt;
            // "ocean eyes": a round pool of glowing sea-blue, edged in black stone
            var pl = z.At(-3f, 0) + up * .02f; z.mk.Prism(dark, pl, up, 3.4f, .3f, 22); z.mk.Prism(SongGlow(oe, new Color(.3f, .7f, .9f), 1f), pl + up * .3f, up, 3f, .02f, 22); BoxCol(z.root, pl + up * .15f, new Vector3(6.4f, .3f, 6.4f), rot); halos.Add(pl + up * .6f, 3.4f, new Color(.3f, .7f, .9f), .2f); for (var k = 0; k < 8; k++) z.mk.Blob(Paint(new Color(.56f, .9f, .4f)), pl + up * .32f + Quaternion.Euler(0, k * 45f, 0) * Vector3.forward * (1f + (k % 3) * .6f), new Vector3(.14f, .02f, .1f), rng, .2f);
            z.area.glowLight = ArtistBits.PointLight(z.root, pl + up * 3f, new Color(.4f, .8f, 1f), 18f, 2.2f);
            // "Happier Than Ever": a car parked at night, headlights on, a guitar on the roof
            var cv = z.At(4f, 7f) + up * .05f; var cr = rot * Quaternion.Euler(0, -50f, 0); var body = Paint(new Color(.16f, .18f, .2f)); z.mk.Box(body, cv + up * .6f, new Vector3(1.8f, .6f, 4.2f), cr); z.mk.Box(body, cv + up * 1.12f - cr * Vector3.forward * .3f, new Vector3(1.6f, .5f, 2f), cr); foreach (var (x, zz) in new[] { (-.9f, 1.4f), (.9f, 1.4f), (.9f, -1.4f), (-.9f, -1.4f) }) z.mk.Prism(dark, cv + up * .3f + cr * new Vector3(x - Mathf.Sign(x) * .1f, 0, zz), cr * Vector3.right * Mathf.Sign(x), .3f, .2f, 12); foreach (var e in new[] { -.6f, .6f }) z.mk.Blob(SongGlow(ht, new Color(1f, .96f, .8f), 1.8f), cv + up * .7f + cr * new Vector3(e, 0, 2.11f), new Vector3(.14f, .1f, .03f), rng, 0f); BoxCol(z.root, cv + up * .7f, new Vector3(1.9f, 1.4f, 4.3f), cr);
            var gt = cv + up * 1.4f - cr * Vector3.forward * .3f; z.mk.Blob(Paint(new Color(.7f, .44f, .24f)), gt, new Vector3(.36f, .08f, .5f), rng, 0f); z.mk.Box(woodM, gt + cr * Vector3.forward * .7f, new Vector3(.08f, .04f, .9f), cr);
            // "No Time To Die": one dark spotlight on an empty mark
            var sp = z.At(5f, -6f); z.mk.Prism(iron, sp - up * .2f, up, .05f, 4f, 5); z.mk.Prism(iron, sp + up * 3.8f, (z.At(3f, -6f) - sp).normalized + up * -.3f, .14f, .4f, 8, 1.4f); z.mk.Prism(SongGlow(nt, new Color(.9f, .86f, .7f), 1f), z.At(3f, -6f) + up * .02f, up, .8f, .01f, 16); ArtistBits.PostCollider(z.root, sp, 3.8f, .07f);
            Shot(z.ctx, "ocean eyes", z.At(9f, -4f) + up * 2f, pl + up * .4f);
        }

        // ---------------------------------------------------------------- everyone else: one thing from their song
        void Others(Plot z, PlaceFrame f)
        {
            var up = Vector3.up; var rot = z.Rot; var rng = new System.Random(z.a.name.Length * 7 + z.a.name[0]); var c = z.At(2f, 0) + up * .05f; var fr = z.front; var sd = z.side;
            void Post(Vector3 at, float h, float r = .04f) { z.mk.Prism(iron, at - up * .2f, up, r, h + .2f, 5); ArtistBits.PostCollider(z.root, at, h, r + .03f); }
            switch (z.a.name)
            {
                case "Avril Lavigne":
                {
                    var s = Song(z, "Sk8er Boi"); var rp = c - sd * .6f; z.mk.Tri(Paint(new Color(.5f, .5f, .54f)), rp, rp + sd * 2.4f, rp + sd * 2.4f + up * 1.2f, rp + fr); z.mk.Tri(Paint(new Color(.5f, .5f, .54f)), rp - fr * 1.6f, rp + sd * 2.4f - fr * 1.6f, rp + sd * 2.4f + up * 1.2f - fr * 1.6f, rp - fr * 3f); z.mk.Box(Paint(new Color(.56f, .56f, .6f)), rp + sd * 1.2f + up * .6f - fr * .8f, new Vector3(1.6f, .06f, 2.7f), Quaternion.LookRotation(fr) * Quaternion.Euler(0, 0, -26f)); BoxCol(z.root, rp + sd * 1.6f + up * .5f - fr * .8f, new Vector3(1.8f, 1f, 1.6f), Quaternion.LookRotation(sd));
                    var sb = new MeshKit(); sb.Box(SongGlow(s, new Color(1f, .3f, .4f), .6f), new Vector3(0, .1f, 0), new Vector3(.22f, .03f, .8f), Quaternion.identity); foreach (var e in new[] { -.3f, .3f }) foreach (var q in new[] { -.08f, .08f }) sb.Prism(dark, new Vector3(q, .05f, e), Vector3.right, .04f, .03f, 8); Thing(sb, "Skateboard", "a skateboard, stickers all over it", c + fr * 1.4f + up * .02f, rot, new Vector3(.25f, .15f, .8f), .5f, false); break;
                }
                case "Halsey":
                {
                    var s = Song(z, "Carry the Weight"); var sc = new MeshKit(); sc.Box(Paint(new Color(.5f, .34f, .3f)), new Vector3(0, .3f, 0), new Vector3(.7f, .55f, .26f), Quaternion.identity); sc.Box(SongGlow(s, Rose, .5f), new Vector3(0, .62f, 0), new Vector3(.24f, .06f, .06f), Quaternion.identity); Thing(sc, "Suitcase", "a suitcase, heavier than it looks", c, rot, new Vector3(.7f, .6f, .3f), 6f, false); break;
                }
                case "Harry Styles":
                {
                    var s = Song(z, "Sign of the Times"); Post(c, 3.6f, .06f); var ck = c + up * 3.2f + fr * .1f; z.mk.Prism(white, ck, fr, .5f, .06f, 16); z.mk.Box(dark, ck + fr * .07f + up * .18f, new Vector3(.03f, .36f, .01f), rot); z.mk.Box(SongGlow(s, Rose, 1f), ck + fr * .08f + sd * .14f + up * .1f, new Vector3(.03f, .3f, .01f), rot * Quaternion.Euler(0, 0, -50f)); halos.Add(ck + fr * .3f, 1f, Rose, .3f); break;
                }
                case "Holly Humberstone":
                {
                    var s = Song(z, "Beauty Pageant"); z.mk.Box(white, c + up * .3f, new Vector3(2.2f, .6f, 1.4f), rot); BoxCol(z.root, c + up * .3f, new Vector3(2.2f, .6f, 1.4f), rot); var cr = c + up * .7f; for (var k = 0; k < 7; k++) z.mk.Cone(SongGlow(k == 3 ? s : null, new Color(1f, .86f, .5f), 1f), cr + sd * (-.24f + k * .08f), .04f, .12f + (k == 3 ? .08f : 0f), 4); z.mk.Box(Paint(Rose), c + up * .62f + fr * .5f, new Vector3(.12f, .02f, .8f), rot * Quaternion.Euler(0, 40f, 0)); break;
                }
                case "Lola Young":
                {
                    var s = Song(z, "Messy"); for (var k = 0; k < 9; k++) z.mk.Box(Paint(Faded[k % Faded.Length]), c + Quaternion.Euler(0, k * 41f, 0) * Vector3.forward * (.2f + (k % 3) * .4f) + up * (.1f + (k % 4) * .12f), new Vector3(.6f, .1f, .4f), Quaternion.Euler(k * 7f, k * 53f, k * 11f)); z.mk.Blob(SongGlow(s, new Color(1f, .7f, .5f), .6f), c + up * .6f, new Vector3(.4f, .2f, .3f), rng, .3f); BoxCol(z.root, c + up * .3f, new Vector3(1.6f, .6f, 1.6f), rot); break;
                }
                case "Maroon 5":
                {
                    var s = Song(z, "Payphone"); z.mk.Box(Paint(new Color(.66f, .1f, .14f)), c + up * 1.2f, new Vector3(1f, 2.4f, 1f), rot); z.mk.Box(Paint(new Color(.66f, .1f, .14f)), c + up * 2.46f, new Vector3(1.1f, .14f, 1.1f), rot); for (var k = 0; k < 4; k++) { var r2 = rot * Quaternion.Euler(0, k * 90f, 0); z.mk.Box(k == 0 ? SongGlow(s, new Color(1f, .9f, .7f), .8f) : Paint(new Color(.4f, .5f, .56f)), c + up * 1.3f + r2 * Vector3.forward * .51f, new Vector3(.7f, 1.6f, .02f), r2); } BoxCol(z.root, c + up * 1.2f, new Vector3(1f, 2.4f, 1f), rot); break;
                }
                case "Meredith Brooks":
                {
                    var s = Song(z, "Bitch"); Post(c - sd * .9f, 1.8f, .03f); Post(c + sd * .9f, 1.8f, .03f); z.mk.Box(woodM, c + up * 1.7f, new Vector3(2f, .08f, .1f), rot); var hats = new[] { new Color(.2f, .2f, .22f), new Color(1f, .6f, .7f), new Color(.9f, .8f, .5f), new Color(.6f, .7f, .9f) }; for (var k = 0; k < 4; k++) { var hp = c + up * 1.5f + sd * (-.75f + k * .5f); z.mk.Prism(k == 0 ? SongGlow(s, hats[k], .4f) : Paint(hats[k]), hp, up, .2f, .03f, 10); z.mk.Prism(k == 0 ? SongGlow(s, hats[k], .4f) : Paint(hats[k]), hp, up, .11f, .12f, 10); } break;
                }
                case "OneRepublic":
                {
                    var s = Song(z, "Counting Stars"); life.starsSong = s; var ts = c; foreach (var k in new[] { 0, 120, 240 }) z.mk.Prism(iron, ts + up * 1.2f, (Quaternion.Euler(0, k, 0) * Vector3.forward * .4f - up).normalized, .02f, 1.3f, 4); z.mk.Prism(Paint(new Color(.8f, .8f, .84f)), ts + up * 1.2f, (fr + up * .8f).normalized, .1f, 1f, 8, 1.4f); ArtistBits.PostCollider(z.root, ts, 1.2f, .4f); for (var k = 0; k < 7; k++) z.mk.Blob(SongGlow(k == 0 ? s : null, new Color(1f, .95f, .7f), 1.4f), ts + up * (3f + (k % 3) * .5f) + fr * (1f + k * .3f) + sd * (k % 2 == 0 ? .6f : -.5f) * k * .2f, Vector3.one * .06f, rng, 0f); break;
                }
                case "Suki Waterhouse":
                {
                    var s = Song(z, "Good Looking"); var hm = new MeshKit(); hm.Prism(Paint(new Color(1f, .86f, .5f)), Vector3.zero, Vector3.up, .02f, .2f, 6); hm.Prism(Paint(new Color(1f, .86f, .5f)), new Vector3(0, .32f, 0), Vector3.forward, .14f, .02f, 14); hm.Prism(SongGlow(s, new Color(.8f, .9f, 1f), .7f), new Vector3(0, .32f, .021f), Vector3.forward, .12f, .005f, 14); Thing(hm, "Hand mirror", "a hand mirror. You look good. Of course you do", c + up * .7f, rot, new Vector3(.3f, .5f, .1f), .2f);
                    z.mk.Box(white, c + up * .35f, new Vector3(.8f, .7f, .5f), rot); BoxCol(z.root, c + up * .35f, new Vector3(.8f, .7f, .5f), rot); break;
                }
                case "Tal Bachman":
                {
                    var s = Song(z, "She's so High"); foreach (var e in new[] { -.3f, .3f }) z.mk.Prism(woodM, c + sd * e, (up + fr * -.08f).normalized, .04f, 6f, 4); for (var k = 0; k < 16; k++) z.mk.Box(woodM, c + up * (.4f + k * .36f) - fr * (.03f + k * .03f), new Vector3(.6f, .05f, .05f), rot); Star(z, c + up * 6.4f - fr * .5f, SongGlow(s, new Color(1f, .9f, .6f), 1.8f), .4f); ArtistBits.PostCollider(z.root, c, 3f, .34f); halos.Add(c + up * 6.4f - fr * .5f, 1.6f, new Color(1f, .9f, .6f), .4f); break;
                }
                default:
                {
                    if (SongIds.TryGetValue(z.a.name, out var list) && list.Length > 0) { var s = Song(z, list[0].title); Post(c, 1.4f); z.mk.Blob(SongGlow(s, Rose, 1.6f), c + up * 1.6f, Vector3.one * .2f, rng, 0f); }
                    break;
                }
            }
        }

        void Star(Plot z, Vector3 c, Material m, float r)
        {
            var face = z.front;
            for (var k = 0; k < 5; k++) { var a0 = Quaternion.AngleAxis(k * 72f, face) * Vector3.up; var a1 = Quaternion.AngleAxis(k * 72f + 36f, face) * Vector3.up; var a2 = Quaternion.AngleAxis(k * 72f - 36f, face) * Vector3.up; z.mk.Tri(m, c + a0 * r, c + a1 * r * .42f, c + a2 * r * .42f, c - face); z.mk.Tri(m, c + a0 * r, c + a1 * r * .42f, c + a2 * r * .42f, c + face); z.mk.Tri(m, c, c + a1 * r * .42f, c + a2 * r * .42f, c - face); z.mk.Tri(m, c, c + a1 * r * .42f, c + a2 * r * .42f, c + face); }
        }
    }
}
