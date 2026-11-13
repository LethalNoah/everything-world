using System.Collections.Generic;
using UnityEngine;
using static EverythingWorld.World.PlaceTools;

namespace EverythingWorld.World
{
    /// The Latin Coast's artists. Most have a little painted courtyard of their own: a bright wall with an arched
    /// doorway, bougainvillea over the top, a tiled bench, papel picado, and one thing from their song; covers on a
    /// cream card on a board painted with a border of tiles.
    /// - Shakira: an open-air stage on the sand with a golden sun behind it; a belt of gold coins on a stand and a floor
    ///   that glows ("Hips Don't Lie"); a little range of mountains with a compass ("Whenever, Wherever"); a motorbike
    ///   ("Can't Remember to Forget You").
    /// - Bad Bunny: a pink wall of hearts and names ("Tití Me Preguntó"), a giant pair of pink sunglasses and a mirror
    ///   ("Me Porto Bonito").
    public partial class LatinKit
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
                Add("Shakira", "Hips Don't Lie", "@la plaza"); Add("Don Omar", "Danza Kuduro", "@la plaza"); Add("Los Del Rio", "Macarena", "@la plaza");
                Add("Los Bukis", "Mar De Soledad", "@the harbour"); Add("Camila Cabello", "Havana", "@down calle mayor");
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
            area.artistName = a.name; area.stationId = a.stationId; area.radius = a.r; area.glowColor = Marigold;
            var z = new Plot { ctx = ctx, root = root, labels = labels, mk = new MeshKit(), solid = new MeshKit(), pictures = new MeshKit(), a = a, c = ctx.OnGround(a.x, a.z), front = front, side = Vector3.Cross(Vector3.up, front), area = area };
            var reach = Foot(a); var up = Vector3.up;
            z.mk.Prism(cobble, z.c - up * .25f, up, reach * .66f, .33f, 24);
            switch (a.name)
            {
                case "Shakira": Shakira(z); break;
                case "Bad Bunny": BadBunny(z); break;
                default: Others(z, Courtyard(z)); break;
            }
            SongLamps(z);
            Board(z, z.At(reach * .6f, -reach * .45f - 1.5f), front);
            ArtistBits.Plaque(z.mk, z.solid, labels, a, z.At(reach * .6f + 1.5f, reach * .45f + 1.5f), front, woodM, Paint(Cream), Ink, 1.6f);
            Lantern(ctx, z.mk, z.At(reach * .5f, reach * .68f), -z.side);
            if (z.area.glowLight == null) z.area.glowLight = ArtistBits.PointLight(root, z.c + up * 4f + front * 3f, new Color(1f, .8f, .6f), HasStation(a) ? 24f : 15f, 1.4f);
            area.glowRenderers = new Renderer[0];
            z.mk.Build("Area", root, false);
            z.solid.BuildCollider("Walls", root);
            z.pictures.Build("Covers", root, false);
            noGrass.Add(new Vector3(z.c.x, reach * .9f, z.c.z));
            var mark = new GameObject("Shot: " + a.name + " up close").transform; mark.SetParent(ctx.Root, false);
            var cam = z.At(reach * .85f, reach * .3f) + up * 2f; var look = z.At(-reach * .3f, 0) + up * 1.8f - cam; mark.SetPositionAndRotation(cam, Quaternion.LookRotation(look.normalized));
        }

        /// The covers on a cream card on a board edged with painted tiles.
        void Board(Plot z, Vector3 ground, Vector3 dir)
        {
            dir = Vector3.ProjectOnPlane(dir, Vector3.up).normalized; var rot = Quaternion.LookRotation(dir); var side = Vector3.Cross(Vector3.up, dir);
            var n = Mathf.Max(1, Mathf.Min(7, z.a.songList?.Length ?? 1)); const float size = .9f; var w = n * size * 1.25f + .6f;
            var c = ground + Vector3.up * 1.7f;
            z.mk.Box(white, c - Vector3.up * .5f - dir * .06f, new Vector3(w + .5f, size + 2f, .2f), rot); z.solid.Box(white, c - Vector3.up * .5f, new Vector3(w + .5f, size + 2f, .3f), rot); z.mk.Box(Paint(Cream), c - Vector3.up * .1f + dir * .07f, new Vector3(w, size + .9f, .04f), rot);
            var nt = Mathf.Max(3, Mathf.RoundToInt((w + .5f) / .25f)); for (var k = 0; k < nt; k++) z.mk.Box(Paint(k % 2 == 0 ? Cobalt : Marigold), c + Vector3.up * (size / 2f + .44f) + side * (-(w + .5f) / 2f + (k + .5f) * (w + .5f) / nt) + dir * .05f, new Vector3((w + .5f) / nt - .02f, .22f, .02f), rot);
            ArtistBits.CoverWall(z.pictures, z.labels, z.ctx, z.a, c + dir * .1f + Vector3.up * .08f, dir, n, size, n, true, Ink);
        }

        /// A tin star lantern on a post for each song without its own piece.
        void SongLamps(Plot z)
        {
            if (!SongIds.TryGetValue(z.a.name, out var list)) return;
            var up = Vector3.up; var k = 0; var seen = new HashSet<string>();
            foreach (var (_, id) in list)
            {
                if (z.lit.Contains(id) || !seen.Add(id)) continue;
                var p = z.At(Foot(z.a) * .55f - (k % 2) * 1.3f, Foot(z.a) * .2f + (k / 2) * 1.3f);
                z.mk.Prism(iron, p - up * .1f, up, .035f, 1.3f, 5); var m = SongGlow(id, Marigold, 1.6f); for (var q = 0; q < 6; q++) z.mk.Cone(m, p + up * 1.5f, .08f, .22f, 4, q * 30f); z.mk.Blob(m, p + up * 1.5f, Vector3.one * .12f, new System.Random(k), 0f); ArtistBits.PostCollider(z.root, p, 1.3f, .08f);
                k++;
            }
        }

        /// A little painted courtyard of their own: a bright wall with an arched doorway, bougainvillea, a tiled bench.
        PlaceFrame Courtyard(Plot z)
        {
            var up = Vector3.up; var hue = z.a.name.Length + z.a.name[0]; var rng = new System.Random(hue); var f = new PlaceFrame(z.At(-Foot(z.a) * .5f, 0), z.front); var wall = Paint(Walls[hue % Walls.Length]);
            foreach (var e in new[] { -1f, 1f }) { z.mk.Box(wall, f.P(e * 1.9f, 1.6f, 0), new Vector3(1.8f, 3.2f, .4f), f.r); z.solid.Box(wall, f.P(e * 1.9f, 1.6f, 0), new Vector3(1.8f, 3.2f, .4f), f.r); }
            z.mk.Box(wall, f.P(0, 2.8f, 0), new Vector3(2f, .8f, .4f), f.r); z.solid.Box(wall, f.P(0, 2.8f, 0), new Vector3(2f, .8f, .4f), f.r); for (var k = 0; k <= 8; k++) { var a = Mathf.PI * k / 8f; z.mk.Box(wall, f.P(Mathf.Cos(a) * .9f, 2.2f + Mathf.Sin(a) * .5f, 0), new Vector3(.3f, .3f, .4f), f.r); }
            z.mk.Box(Paint(Walls[(hue + 3) % Walls.Length]), f.P(0, 1.2f, -.1f), new Vector3(1.6f, 2.4f, .1f), f.r); z.mk.Box(white, f.P(0, 3.26f, 0), new Vector3(5.8f, .14f, .5f), f.r);
            for (var k = 0; k < 6; k++) z.mk.Blob(k % 2 == 0 ? Paint(Fuchsia) : leafM, f.P(-2.4f + k * .96f, 3.4f + (k % 2) * .2f, .1f), new Vector3(.6f, .4f, .4f), rng, .3f); for (var k = 0; k < 4; k++) z.mk.Box(leafM, f.P(-2.6f + k * 1.6f, 2.6f, .22f), new Vector3(.3f, 1.2f, .03f), f.r);
            Bench(z.root, z.mk, f.P(-1.9f, 0, .9f), f.Fwd, 1.6f); Picado(z.mk, f.P(-2.8f, 3.3f, .2f), f.P(2f, 3.6f, 2.6f), hue);
            return f;
        }

        // ---------------------------------------------------------------- Shakira: the stage on the sand
        void Shakira(Plot z)
        {
            var up = Vector3.up; var rot = z.Rot; var rng = new System.Random(2006); var fr = z.front; var sd = z.side; var hd = Song(z, "Hips Don't Lie"); var ww = Song(z, "Whenever"); var cr = Song(z, "Can't Remember"); life.hipsSong = hd; life.wheneverSong = ww;
            var st = z.At(-5f, 0) + up * .05f; z.mk.Prism(Paint(new Color(.94f, .84f, .64f)), st + up * .01f, up, 7f, .06f, 22);
            z.mk.Box(woodM, st + up * .4f, new Vector3(9f, .8f, 5f), rot); z.solid.Box(woodM, st + up * .3f, new Vector3(9f, 1f, 5f), rot); z.mk.Box(SongGlow(hd, Marigold, .7f), st + up * .81f, new Vector3(8.6f, .02f, 4.6f), rot);
            // the golden sun behind
            var sun = st - fr * 2.8f + up * 4f; foreach (var e in new[] { -2f, 2f }) { z.mk.Prism(woodM, st - fr * 2.8f + sd * e, up, .1f, 4f, 5); ArtistBits.PostCollider(z.root, st - fr * 2.8f + sd * e, 4f, .12f); } var sm = GlowM(new Color(1f, .74f, .26f), 1.4f); z.mk.Prism(sm, sun - fr * .06f, fr, 1.8f, .12f, 22); for (var k = 0; k < 16; k++) { var a = Quaternion.AngleAxis(k * 22.5f, fr); z.mk.Box(sm, sun + a * up * (2.4f + (k % 2) * .4f), new Vector3(.2f, .9f + (k % 2) * .5f, .06f), a * rot); } halos.Add(sun + fr * .6f, 4f, new Color(1f, .74f, .26f), .2f); ArtistBits.Label(z.labels, "SHAKIRA", sun + fr * .1f, fr, .5f, Ink, TextAnchor.MiddleCenter);
            // "Hips Don't Lie": a belt of gold coins on a stand at the front of the stage
            var bs = st + up * .8f + fr * 1.6f + sd * 2.8f; z.mk.Prism(iron, bs, up, .03f, 1.1f, 4); for (var k = 0; k < 18; k++) { var a = k * 20f * Mathf.Deg2Rad; z.mk.Prism(SongGlow(hd, new Color(1f, .82f, .3f), 1.2f), bs + up * 1.1f + sd * Mathf.Cos(a) * .3f + fr * Mathf.Sin(a) * .2f - up * (k % 2) * .06f, up, .05f, .01f, 8); }
            string[] dance = { "You try the hip move. Your hips, unlike hers, absolutely lie. The crowd cheers anyway.", "Coins jingle. Somebody shouts \"¡Eso!\". You feel like a star for four seconds.", "The floor glows gold under your feet. Barranquilla is a feeling." }; var dn = 0;
            Spot("The stage", st + up * 1.6f, "Dance", new Vector3(8f, 1.4f, 4f)).onUse = _ => { life.played = Time.time; Say1(st + up * 3.2f, dance[dn++ % dance.Length]); };
            z.area.glowLight = ArtistBits.PointLight(z.root, st + up * 5f + fr * 3f, new Color(1f, .82f, .5f), 26f, 1.8f);
            // "Whenever, Wherever": a little range of mountains and a compass on a stand
            var mt = z.At(4f, -8f); for (var k = 0; k < 4; k++) z.mk.Cone(k == 1 ? SongGlow(ww, new Color(.5f, .6f, .8f), .5f) : Paint(new Color(.5f, .58f, .7f)), mt + sd * (k * .8f - 1.2f), .7f + (k % 2) * .3f, 1f + (k % 3) * .5f, 5); BoxCol(z.root, mt + up * .6f, new Vector3(3.6f, 1.2f, 1.4f), rot); var cp = mt + fr * 1.8f; z.mk.Prism(iron, cp, up, .03f, .9f, 4); z.mk.Prism(white, cp + up * .9f, up, .24f, .04f, 14); z.mk.Box(Paint(new Color(.9f, .2f, .2f)), cp + up * .95f + fr * .08f, new Vector3(.03f, .01f, .16f), rot);
            // "Can't Remember to Forget You": a motorbike
            var mb = z.At(5f, 7f) + up * .05f; var mr = rot * Quaternion.Euler(0, 70f, 0); var mf = mr * Vector3.forward; var mm = SongGlow(cr, new Color(.14f, .14f, .16f), .3f); foreach (var e in new[] { -.8f, .8f }) z.mk.Prism(dark, mb + up * .4f + mf * e - (mr * Vector3.right) * .06f, mr * Vector3.right, .4f, .12f, 14); z.mk.Box(mm, mb + up * .8f, new Vector3(.4f, .4f, 1.2f), mr); z.mk.Box(Paint(new Color(.8f, .2f, .2f)), mb + up * 1f + mf * .3f, new Vector3(.36f, .2f, .5f), mr); z.mk.Prism(Paint(Chrome()), mb + up * 1.1f + mf * .7f, (mr * Vector3.right), .02f, .6f, 4); z.mk.Prism(Paint(Chrome()), mb + up * 1.1f + mf * .7f - mr * Vector3.right * .3f, mr * Vector3.right, .02f, .6f, 4); BoxCol(z.root, mb + up * .6f, new Vector3(.6f, 1.2f, 2f), mr); Interact.Seat(tlife.transform, mb + up * 1.1f - mf * .2f, mf, "Sit on the motorbike").seatHeight = .3f;
            for (var k = 0; k < 5; k++) Folk(st + fr * (4.4f + (k % 2) * 1f) + sd * (-3f + k * 1.5f), -fr, Resident.Pose.Wave, new[] { "¡Shakira, Shakira!", "My hips are trying. My hips are doing their best.", "She's from the coast, like us. You can tell from the way she moves." });
            Shot(z.ctx, "shakira's stage", z.At(13f, 4f) + up * 2.2f, st + up * 2.6f);
        }

        Color Chrome() => new Color(.8f, .82f, .86f);

        // ---------------------------------------------------------------- Bad Bunny
        void BadBunny(Plot z)
        {
            var up = Vector3.up; var rot = z.Rot; var rng = new System.Random(2022); var fr = z.front; var sd = z.side; var tt = Song(z, "Tití"); var mp = Song(z, "Me Porto Bonito"); life.titiSong = tt;
            // "Tití Me Preguntó": a pink wall covered in hearts with names scrawled on them
            var w = new PlaceFrame(z.At(-4f, 0), fr); z.mk.Box(Paint(new Color(1f, .62f, .78f)), w.P(0, 1.6f, 0), new Vector3(6f, 3.2f, .4f), w.r); z.solid.Box(woodM, w.P(0, 1.6f, 0), new Vector3(6f, 3.2f, .4f), w.r);
            string[] names = { "♥", "♥", "♥", "♥", "♥", "♥", "♥", "♥", "♥" }; for (var k = 0; k < 9; k++) { var hp = w.P(-2.2f + (k % 3) * 2.2f, .8f + (k / 3) * .8f, .21f); var hm = k == 4 ? SongGlow(tt, Fuchsia, 1.2f) : Paint(Walls[k % Walls.Length]); z.mk.Box(hm, hp, new Vector3(.5f, .5f, .02f), w.r * Quaternion.Euler(0, 0, 45f)); ArtistBits.Label(z.labels, names[k], hp + fr * .02f, fr, .2f, Cream, TextAnchor.MiddleCenter); }
            ArtistBits.Label(z.labels, "¿y las novias?", w.P(0, 3f, .22f), fr, .2f, Ink, TextAnchor.MiddleCenter);
            // "Me Porto Bonito": giant pink sunglasses on a stand, and a mirror
            var sg = z.At(4f, -6f); z.mk.Prism(iron, sg - up * .2f, up, .05f, 2.2f, 5); ArtistBits.PostCollider(z.root, sg, 2f, .07f); foreach (var e in new[] { -1f, 1f }) { z.mk.Prism(SongGlow(mp, Fuchsia, 1f), sg + up * 2.4f + sd * e * .55f - fr * .05f, fr, .45f, .1f, 16); z.mk.Prism(Paint(new Color(.2f, .2f, .3f)), sg + up * 2.4f + sd * e * .55f + fr * .01f, fr, .38f, .02f, 16); } z.mk.Box(Paint(Fuchsia), sg + up * 2.5f, new Vector3(.3f, .08f, .08f), rot);
            var mi = z.At(4f, 6f); z.mk.Box(Paint(Marigold), mi + up * 1.2f, new Vector3(1.1f, 2.2f, .08f), rot * Quaternion.Euler(0, -20f, 0)); z.mk.Box(WorldMaterials.Solid(new Color(.8f, .9f, 1f), .95f), mi + up * 1.2f + (rot * Quaternion.Euler(0, -20f, 0)) * Vector3.forward * .05f, new Vector3(.9f, 2f, .02f), rot * Quaternion.Euler(0, -20f, 0)); BoxCol(z.root, mi + up * 1.2f, new Vector3(1.1f, 2.2f, .2f), rot);
            z.area.glowLight = ArtistBits.PointLight(z.root, w.P(0, 3f, 3f), new Color(1f, .6f, .8f), 20f, 1.8f);
            Shot(z.ctx, "the wall of hearts", z.At(10f, 3f) + up * 2f, w.P(0, 1.6f, 0));
        }

        // ---------------------------------------------------------------- everyone else: one thing from their song
        void Others(Plot z, PlaceFrame f)
        {
            var up = Vector3.up; var rot = z.Rot; var rng = new System.Random(z.a.name.Length * 7 + z.a.name[0]); var c = z.At(2.4f, .6f) + up * .02f; var fr = z.front; var sd = z.side;
            void Post(Vector3 at, float h, float r = .04f) { z.mk.Prism(iron, at - up * .2f, up, r, h + .2f, 5); ArtistBits.PostCollider(z.root, at, h, r + .03f); }
            void Plinth(Vector3 at, float w, float h) { z.mk.Box(white, at + up * (h / 2f), new Vector3(w, h, w), rot); for (var k = 0; k < 4; k++) z.mk.Box(Paint(k % 2 == 0 ? Cobalt : Marigold), at + up * (h * .5f) + Quaternion.Euler(0, k * 90f, 0) * fr * (w / 2f + .01f), new Vector3(w * .6f, h * .4f, .01f), rot * Quaternion.Euler(0, k * 90f, 0)); BoxCol(z.root, at + up * (h / 2f), new Vector3(w, h, w), rot); }
            switch (z.a.name)
            {
                case "Los Lobos": { var lb = Song(z, "La Bamba"); var wc = Song(z, "When the Circus"); life.bambaSong = lb; Plinth(c - sd * .8f, .6f, .6f); z.mk.Blob(SongGlow(lb, new Color(.86f, .6f, .36f), .5f), c - sd * .8f + up * .9f, new Vector3(.26f, .32f, .1f), rng, 0f); z.mk.Box(woodM, c - sd * .8f + up * 1.5f, new Vector3(.06f, .8f, .04f), rot); var tc = c + sd * 1.2f; for (var k = 0; k < 8; k++) z.mk.Tri(k % 2 == 0 ? Paint(new Color(.9f, .2f, .2f)) : white, tc + up * 2.2f, tc + Quaternion.Euler(0, k * 45f, 0) * fr * 1.1f + up * .8f, tc + Quaternion.Euler(0, (k + 1) * 45f, 0) * fr * 1.1f + up * .8f, tc - up); for (var k = 0; k < 8; k++) z.mk.Box(k % 2 == 0 ? Paint(new Color(.9f, .2f, .2f)) : white, tc + up * .4f + Quaternion.Euler(0, k * 45f + 22.5f, 0) * fr * 1.02f, new Vector3(.86f, .8f, .02f), rot * Quaternion.Euler(0, k * 45f + 22.5f, 0)); z.mk.Blob(SongGlow(wc, Marigold, 1.4f), tc + up * 2.3f, Vector3.one * .1f, rng, 0f); BoxCol(z.root, tc + up * .5f, new Vector3(2f, 1f, 2f), rot); break; }
                case "Natalia Lafourcade": { var hr = Song(z, "Hasta la Ra"); var eb = Song(z, "Ella es Bonita"); var tr = c - sd * .6f; for (var k = 0; k < 6; k++) z.mk.Prism(SongGlow(k == 0 ? hr : null, new Color(.8f, .6f, .4f), .5f), tr, (Quaternion.Euler(0, k * 60f, 0) * fr * 1f - up * .2f).normalized, .08f, 1.2f, 4, .2f); z.mk.Prism(trunkM, tr, up, .18f, 2.4f, 6, .6f); z.mk.Blob(leafM, tr + up * 3f, new Vector3(1.4f, 1f, 1.4f), rng, .2f); ArtistBits.PostCollider(z.root, tr, 2.4f, .22f); var fcp = c + sd * 1.2f; Plinth(fcp, .5f, .8f); for (var k = 0; k < 10; k++) { var a = k * 36f * Mathf.Deg2Rad; z.mk.Blob(k % 3 == 0 ? SongGlow(eb, Fuchsia, 1f) : Paint(Walls[k % Walls.Length]), fcp + up * .88f + sd * Mathf.Cos(a) * .18f + fr * Mathf.Sin(a) * .18f, Vector3.one * .06f, rng, .3f); } break; }
                case "Camila Cabello": { var s = Song(z, "Havana"); life.havanaSong = s; var cr = rot * Quaternion.Euler(0, 75f, 0); var cf = cr * Vector3.forward; var cm = SongGlow(s, new Color(.6f, .9f, .86f), .3f); z.mk.Box(cm, c + up * .55f, new Vector3(1.9f, .5f, 4.8f), cr); z.mk.Box(cm, c + up * 1f - cf * .3f, new Vector3(1.7f, .5f, 1.9f), cr); z.mk.Box(white, c + up * .32f, new Vector3(1.94f, .1f, 4.84f), cr); z.mk.Box(Paint(new Color(.6f, .76f, .84f)), c + up * 1.02f - cf * .3f, new Vector3(1.72f, .36f, 1.7f), cr); foreach (var (x, zz) in new[] { (-.9f, 1.5f), (.9f, 1.5f), (.9f, -1.5f), (-.9f, -1.5f) }) z.mk.Prism(dark, c + up * .32f + cr * new Vector3(x - Mathf.Sign(x) * .1f, 0, zz), cr * Vector3.right * Mathf.Sign(x), .32f, .2f, 12); foreach (var e in new[] { -.7f, .7f }) z.mk.Blob(lampM, c + up * .62f + cf * 2.41f + cr * Vector3.right * e, new Vector3(.14f, .14f, .03f), rng, 0f); BoxCol(z.root, c + up * .7f, new Vector3(1.9f, 1.4f, 4.8f), cr); Interact.Seat(tlife.transform, c + up * .8f - cf * .3f + cr * Vector3.right * .4f, cf, "Sit in the car").seatHeight = .4f; break; }
                case "Don Omar": { var s = Song(z, "Danza Kuduro"); life.kuduroSong = s; for (var x = 0; x < 3; x++) for (var y = 0; y < 3; y++) z.mk.Box((x + y) % 2 == 0 ? SongGlow(s, Confetti(x + y), 1f) : Paint(Cream), c + up * .03f + sd * (x - 1) * 1f + fr * (y - 1) * 1f, new Vector3(.96f, .04f, .96f), rot); break; }
                case "Edgardo Nuñez": { var s = Song(z, "Renacer"); life.renacerSong = s; Post(c, 1.4f, .03f); var sm = SongGlow(s, new Color(1f, .7f, .4f), 1.6f); z.mk.Prism(sm, c + up * 1.6f - fr * .03f, fr, .4f, .06f, 16); for (var k = 0; k < 7; k++) { var a = Quaternion.AngleAxis(-90f + k * 30f, fr); z.mk.Box(sm, c + up * 1.6f + a * up * .7f, new Vector3(.06f, .4f, .04f), a * rot); } break; }
                case "Eslabon Armado": { var s = Song(z, "Ella Baila Sola"); life.solaSong = s; z.mk.Prism(SongGlow(s, new Color(1f, .9f, .7f), .8f), c + up * .02f, up, 1f, .02f, 16); var fm = Paint(new Color(.9f, .3f, .4f)); z.mk.Cone(fm, c + up * .1f, .4f, .9f, 10); z.mk.Prism(fm, c + up * 1f, up, .1f, .4f, 6); z.mk.Blob(Paint(new Color(.6f, .44f, .34f)), c + up * 1.56f, Vector3.one * .12f, rng, 0f); z.mk.Prism(Paint(new Color(.6f, .44f, .34f)), c + up * 1.3f, (up + sd).normalized, .03f, .5f, 4); Post(c - fr * 1.2f, 3.4f, .03f); z.mk.Prism(iron, c - fr * 1.2f + up * 3.4f, (fr + -up * .8f).normalized, .14f, .3f, 8, 1.4f); break; }
                case "Feid": { var s = Song(z, "Normal"); Post(c, 1.2f, .03f); var gm = SongGlow(s, new Color(.4f, 1f, .3f), 1.4f); foreach (var e in new[] { -1f, 1f }) z.mk.Prism(gm, c + up * 1.5f + sd * e * .3f - fr * .03f, fr, .24f, .05f, 12); z.mk.Box(gm, c + up * 1.55f, new Vector3(.2f, .05f, .05f), rot); halos.Add(c + up * 1.5f, 1f, new Color(.4f, 1f, .3f), .4f); break; }
                case "Los Bukis": { var s = Song(z, "Mar De Soledad"); life.soledadSong = s; z.mk.Prism(SongGlow(s, new Color(.2f, .4f, .8f), .5f), c + up * .01f, up, 1.6f, .03f, 16); z.mk.Box(Paint(Walls[3]), c + up * .2f, new Vector3(.5f, .2f, 1.4f), rot * Quaternion.Euler(0, 30f, 0)); z.mk.Prism(woodM, c + up * .2f, up, .02f, .9f, 3); z.mk.Tri(white, c + up * 1.1f, c + up * .3f, c + up * .3f + (rot * Quaternion.Euler(0, 30f, 0)) * Vector3.forward * .6f, c + sd); break; }
                case "Los Del Rio": { var s = Song(z, "Macarena"); life.macarenaSong = s; Post(c - sd * .6f, 1.2f, .03f); Post(c + sd * .6f, 1.2f, .03f); z.mk.Box(white, c + up * 1.6f, new Vector3(1.6f, 1.2f, .05f), rot); string[] steps = { "1 arm", "2 arm", "3 palm", "4 palm", "5 shoulder", "6 shoulder", "7 head", "8 hips · ¡hey!" }; for (var k = 0; k < 8; k++) ArtistBits.Label(z.labels, steps[k], c + up * (2.06f - (k % 4) * .26f) + sd * (k < 4 ? -.4f : .4f) + fr * .03f, fr, .055f, Ink, TextAnchor.MiddleCenter); z.mk.Prism(SongGlow(s, Fuchsia, 1f), c + up * .02f + fr * 1f, up, .8f, .02f, 14); break; }
                case "Miami Sound Machine": { var s = Song(z, "Words Get In the Way"); Plinth(c, .6f, .8f); z.mk.Box(Paint(new Color(1f, .5f, .6f)), c + up * .9f, new Vector3(.4f, .14f, .3f), rot); z.mk.Box(Paint(new Color(1f, .5f, .6f)), c + up * 1.05f, new Vector3(.5f, .08f, .14f), rot); for (var k = 0; k < 6; k++) z.mk.Prism(SongGlow(k == 0 ? s : null, new Color(1f, .5f, .6f), .6f), c + up * (.9f - k * .1f) + sd * Mathf.Sin(k) * .2f + fr * .2f, (sd * Mathf.Cos(k) - up).normalized, .01f, .14f, 3); break; }
                case "Nico Play": { var s = Song(z, "La Ciruela"); z.mk.Prism(trunkM, c - up * .2f, up, .14f, 2f, 6, .6f); ArtistBits.PostCollider(z.root, c, 2f, .18f); z.mk.Blob(leafM, c + up * 2.6f, new Vector3(1.3f, 1f, 1.3f), rng, .2f); for (var k = 0; k < 8; k++) z.mk.Blob(k == 0 ? SongGlow(s, new Color(.5f, .14f, .4f), .6f) : Paint(new Color(.5f, .16f, .4f)), c + up * (2.1f + (k % 3) * .3f) + Quaternion.Euler(0, k * 45f, 0) * Vector3.forward * 1f, Vector3.one * .1f, rng, 0f); break; }
                case "Rafa": { var s = Song(z, "Ideally Attached"); foreach (var e in new[] { -1.4f, 1.4f }) Post(c + sd * e, 1.6f, .05f); for (var k = 0; k <= 8; k++) { var t = k / 8f; z.mk.Box(SongGlow(k == 4 ? s : null, Walls[k % Walls.Length], .3f), c + sd * Mathf.Lerp(-1.3f, 1.3f, t) + up * (1.3f - Mathf.Sin(t * Mathf.PI) * .5f), new Vector3(.3f, .03f, .7f), rot); } Interact.Seat(tlife.transform, c + up * .8f, fr, "Lie in the hammock").seatHeight = .1f; break; }
                case "Shawn Mendes": { var s = Song(z, "Señorita"); z.mk.Prism(white, c + up * .72f, up, .4f, .04f, 12); z.mk.Prism(iron, c, up, .04f, .72f, 4); BoxCol(z.root, c + up * .38f, new Vector3(.8f, .76f, .8f), rot); z.mk.Prism(SongGlow(s, new Color(1f, .9f, .7f), 1f), c + up * .76f, up, .05f, .08f, 8); foreach (var e in new[] { -1f, 1f }) { var cp = c + sd * e * .7f; z.mk.Box(iron, cp + up * .45f, new Vector3(.4f, .04f, .4f), Quaternion.LookRotation(-sd * e)); z.mk.Box(iron, cp + up * .8f + sd * e * .18f, new Vector3(.4f, .6f, .03f), Quaternion.LookRotation(-sd * e)); Interact.Seat(tlife.transform, cp + up * .5f, -sd * e, "Sit at the café table"); } break; }
                case "The Mariachis": { var s = Song(z, "La Cucaracha"); life.cucaSong = s; Plinth(c, .7f, .8f); z.mk.Prism(SongGlow(s, Marigold, .5f), c + up * .82f, up, .7f, .05f, 16); z.mk.Cone(Paint(Marigold), c + up * .86f, .26f, .4f, 12); for (var k = 0; k < 16; k++) z.mk.Box(Paint(new Color(.6f, .1f, .14f)), c + up * .88f + Quaternion.Euler(0, k * 22.5f, 0) * fr * .6f, new Vector3(.12f, .02f, .06f), Quaternion.Euler(0, k * 22.5f, 0)); break; }
                default: { if (SongIds.TryGetValue(z.a.name, out var list) && list.Length > 0) { var s = Song(z, list[0].title); Post(c, 1.4f); z.mk.Blob(SongGlow(s, Marigold, 1.6f), c + up * 1.6f, Vector3.one * .2f, rng, 0f); } break; }
            }
        }

        Color Confetti(int k) => new[] { Fuchsia, Marigold, Turquoise, Lime, Cobalt }[k % 5];
    }
}
