using System.Collections.Generic;
using UnityEngine;
using static EverythingWorld.World.PlaceTools;

namespace EverythingWorld.World
{
    /// The Synthwave Coast's artists. Most have a roadside neon of their own: a dark panel outlined in neon with their
    /// name, a square of glowing grid in front, a palm, and one thing from their song; covers on a cream card on a
    /// chrome board with a neon strip.
    /// - The Midnight: a retro garage on the coast, its door up, a chrome car inside with racing stripes ("Days of
    ///   Thunder"), a saxophone on a stand under a pink spotlight, a moon with a bite out of it ("Vampires"), and a neon
    ///   ring of a spaceship hanging over the forecourt ("Neon Odyssey").
    public partial class SynthKit
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
                Add("The Midnight", "Days of Thunder", "@sunset point"); Add("The Midnight", "Vampires", "@the neon diner"); Add("FM-84", "Arcade Summer", "@down the coast road");
                Add("ODESZA", "A Moment Apart", "@sunset point"); Add("Gost", "Genesee", "@the neon diner");
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
            if (asphalt == null) MakeMaterials();
            var root = new GameObject("Artist: " + a.name).transform; root.SetParent(ctx.Root, false);
            var front = facing.TryGetValue(a.name, out var f) ? f : new Vector3(center.x - a.x, 0, center.z - a.z).normalized;
            if (front.sqrMagnitude < .01f) front = Vector3.forward;
            var labels = new GameObject("Labels").transform; labels.SetParent(root, false);
            var area = root.gameObject.AddComponent<ArtistArea>();
            area.artistName = a.name; area.stationId = a.stationId; area.radius = a.r; area.glowColor = Magenta;
            var z = new Plot { ctx = ctx, root = root, labels = labels, mk = new MeshKit(), solid = new MeshKit(), pictures = new MeshKit(), a = a, c = ctx.OnGround(a.x, a.z), front = front, side = Vector3.Cross(Vector3.up, front), area = area };
            var reach = Foot(a); var up = Vector3.up;
            z.mk.Prism(asphalt, z.c - up * .25f, up, reach * .66f, .33f, 24);
            switch (a.name)
            {
                case "The Midnight": Midnight(z); break;
                default: Others(z, Roadside(z)); break;
            }
            SongLamps(z);
            Board(z, z.At(reach * .6f, -reach * .45f - 1.5f), front);
            ArtistBits.Plaque(z.mk, z.solid, labels, a, z.At(reach * .6f + 1.5f, reach * .45f + 1.5f), front, chrome, pinkN, Ink, 1.6f);
            TubeLamp(ctx, z.mk, z.At(reach * .5f, reach * .68f), -z.side);
            if (z.area.glowLight == null) z.area.glowLight = ArtistBits.PointLight(root, z.c + up * 4f + front * 3f, new Color(1f, .5f, .9f), HasStation(a) ? 24f : 15f, 2.2f);
            area.glowRenderers = new Renderer[0];
            z.mk.Build("Area", root, false);
            z.solid.BuildCollider("Walls", root);
            z.pictures.Build("Covers", root, false);
            noGrass.Add(new Vector3(z.c.x, reach * .9f, z.c.z));
            var mark = new GameObject("Shot: " + a.name + " up close").transform; mark.SetParent(ctx.Root, false);
            var cam = z.At(reach * .85f, reach * .3f) + up * 2f; var look = z.At(-reach * .3f, 0) + up * 1.8f - cam; mark.SetPositionAndRotation(cam, Quaternion.LookRotation(look.normalized));
        }

        /// The covers on a cream card on a chrome board with a neon strip along the top.
        void Board(Plot z, Vector3 ground, Vector3 dir)
        {
            dir = Vector3.ProjectOnPlane(dir, Vector3.up).normalized; var rot = Quaternion.LookRotation(dir);
            var n = Mathf.Max(1, Mathf.Min(7, z.a.songList?.Length ?? 1)); const float size = .9f; var w = n * size * 1.25f + .6f;
            var c = ground + Vector3.up * 1.7f;
            z.mk.Box(dark, c - Vector3.up * .5f - dir * .06f, new Vector3(w + .5f, size + 2f, .2f), rot); z.solid.Box(dark, c - Vector3.up * .5f, new Vector3(w + .5f, size + 2f, .3f), rot); z.mk.Box(Paint(Cream), c - Vector3.up * .1f + dir * .07f, new Vector3(w, size + .9f, .04f), rot); z.mk.Box(pinkN, c + Vector3.up * (size / 2f + .52f) + dir * .06f, new Vector3(w + .4f, .06f, .03f), rot);
            ArtistBits.CoverWall(z.pictures, z.labels, z.ctx, z.a, c + dir * .1f + Vector3.up * .08f, dir, n, size, n, true, Ink);
        }

        /// A neon tube on a post for each song without its own piece.
        void SongLamps(Plot z)
        {
            if (!SongIds.TryGetValue(z.a.name, out var list)) return;
            var up = Vector3.up; var k = 0; var seen = new HashSet<string>();
            foreach (var (_, id) in list)
            {
                if (z.lit.Contains(id) || !seen.Add(id)) continue;
                var p = z.At(Foot(z.a) * .55f - (k % 2) * 1.3f, Foot(z.a) * .2f + (k / 2) * 1.3f);
                z.mk.Prism(chrome, p - up * .1f, up, .035f, 1.2f, 5); z.mk.Prism(SongGlow(id, k % 2 == 0 ? Cyan : Magenta, 1.8f), p + up * 1.1f, up, .05f, .5f, 6); ArtistBits.PostCollider(z.root, p, 1.3f, .08f);
                k++;
            }
        }

        /// A roadside neon of their own: a dark panel outlined in neon with their name, a square of grid in front, a palm.
        PlaceFrame Roadside(Plot z)
        {
            var up = Vector3.up; var hue = z.a.name.Length + z.a.name[0]; var rng = new System.Random(hue); var f = new PlaceFrame(z.At(-Foot(z.a) * .5f, 0), z.front); var n = hue % 2 == 0 ? pinkN : cyanN;
            z.mk.Box(dark, f.P(0, 2f, 0), new Vector3(5f, 3.2f, .3f), f.r); z.solid.Box(dark, f.P(0, 2f, 0), new Vector3(5f, 3.2f, .3f), f.r); foreach (var e in new[] { -1f, 1f }) { z.mk.Prism(chrome, f.P(e * 2.3f, -.2f, 0), up, .07f, .6f, 5); z.mk.Box(n, f.P(e * 2.42f, 2f, .16f), new Vector3(.06f, 3.1f, .02f), f.r); z.mk.Box(n, f.P(0, 2f + e * 1.52f, .16f), new Vector3(4.9f, .06f, .02f), f.r); }
            ArtistBits.Label(z.labels, z.a.name, f.P(0, 3f, .17f), f.Fwd, .3f, hue % 2 == 0 ? Magenta : Cyan, TextAnchor.MiddleCenter); halos.Add(f.P(0, 2f, .8f), 2.4f, hue % 2 == 0 ? Magenta : Cyan, .18f);
            GridPad(z.ctx, z.mk, f.P(0, 0, 2.6f), f.r, 5f, 1.25f); noGrass.RemoveAt(noGrass.Count - 1); NeonPalm(z.ctx, z.mk, f.P(3.6f, 0, .4f), 1f, rng, hue);
            return f;
        }

        // ---------------------------------------------------------------- The Midnight: the garage on the coast
        void Midnight(Plot z)
        {
            var up = Vector3.up; var rot = z.Rot; var rng = new System.Random(2016); var fr = z.front; var sd = z.side; var dt = Song(z, "Days of Thunder"); var vp = Song(z, "Vampires"); var no = Song(z, "Neon Odyssey"); life.thunderSong = dt; life.vampSong = vp; life.odysseySong = no;
            // the garage: an open bay, the door rolled up, a chrome car inside, the name in pink script across the top
            var f = new PlaceFrame(z.At(-6f, 0), fr); const float W = 10f, Dp = 7f, H = 4f;
            void Wall(Vector3 at, Vector3 size) { z.mk.Box(dark, at, size, f.r); z.solid.Box(dark, at, size, f.r); }
            Wall(f.P(0, H / 2f, -Dp / 2f + .15f), new Vector3(W, H, .3f)); foreach (var e in new[] { -1f, 1f }) Wall(f.P(e * (W / 2f - .15f), H / 2f, 0), new Vector3(.3f, H, Dp - .6f)); Wall(f.P(0, H - .4f, Dp / 2f - .15f), new Vector3(W, .8f, .3f)); z.mk.Box(dark, f.P(0, H + .1f, 0), new Vector3(W + .4f, .2f, Dp + .4f), f.r); z.solid.Box(dark, f.P(0, H + .1f, 0), new Vector3(W + .4f, .2f, Dp + .4f), f.r);
            for (var k = 0; k < 5; k++) z.mk.Box(chrome, f.P(0, H - .66f + k * .12f, Dp / 2f - .3f), new Vector3(W - .6f, .1f, .08f), f.r); z.mk.Box(pinkN, f.P(0, H + .5f, Dp / 2f), new Vector3(W, .06f, .04f), f.r); ArtistBits.Label(z.labels, "the Midnight", f.P(0, H + 1.1f, Dp / 2f + .05f), fr, .6f, Magenta, TextAnchor.MiddleCenter); z.mk.Box(dark, f.P(0, H + 1.1f, Dp / 2f), new Vector3(6f, 1f, .1f), f.r);
            GridPad(z.ctx, z.mk, f.P(0, .02f, 0), f.r, 9f, 1f); noGrass.RemoveAt(noGrass.Count - 1); IndoorSpaces.Add(f.P(0, H / 2f, 0), f.r, new Vector3(W, H, Dp));
            SportsCar(z.root, z.mk, f.P(-1.6f, .02f, 0), f.r * Quaternion.Euler(0, 180f, 0), new Color(.9f, .9f, .94f)); for (var k = 0; k < 2; k++) z.mk.Box(SongGlow(dt, new Color(1f, .3f, .3f), 1f), f.P(-1.6f + (k - .5f) * .3f, .67f, 0), new Vector3(.12f, .02f, 4.2f), f.r);
            // a saxophone on a stand under a pink spotlight
            var sx = f.P(2.6f, .02f, .6f); z.mk.Prism(chrome, sx, up, .02f, .5f, 4); var gm = GlowM(SunGold, 1.2f); z.mk.Prism(gm, sx + up * .5f, up, .07f, .9f, 8, .7f); z.mk.Prism(gm, sx + up * .5f, (-fr + up * .3f).normalized, .1f, .4f, 8, 1.8f); z.mk.Prism(gm, sx + up * 1.4f, (fr + up * .4f).normalized, .03f, .3f, 5); z.mk.Prism(Paint(Magenta), sx + up * 3.8f, -up, .25f, .3f, 8, 1.6f);
            string[] sax = { "You lift the sax and the solo pours out over the coast like it's been waiting all night.", "Long notes. The neon leans in to listen.", "You play the hook. Somewhere a car on the Coast Road slows down to hear it." }; var sn = 0;
            Spot("The saxophone", sx + up * 1f, "Play the sax solo", new Vector3(.8f, 1.2f, .8f)).onUse = _ => { life.played = Time.time; Say1(sx + up * 2.4f, sax[sn++ % sax.Length]); };
            z.area.glowLight = ArtistBits.PointLight(z.root, f.P(0, 3f, 1f), new Color(1f, .5f, .9f), 14f, 2.4f, true);
            // "Vampires": a moon with a bite taken out of it; "Neon Odyssey": a neon ring of a ship hung over the forecourt
            var mp = z.At(4f, 7f); z.mk.Prism(chrome, mp - up * .2f, up, .05f, 3.6f, 5); ArtistBits.PostCollider(z.root, mp, 3.4f, .07f); z.mk.Prism(SongGlow(vp, new Color(1f, .9f, .9f), 1.4f), mp + up * 4.2f - fr * .06f, fr, .6f, .1f, 16); z.mk.Prism(dark, mp + up * 4.5f - fr * .08f + sd * .45f, fr, .3f, .14f, 10); for (var k = 0; k < 2; k++) z.mk.Cone(Paint(new Color(.8f, .1f, .2f)), mp + up * (3.66f) + sd * (k * .2f - .1f) + fr * .02f, .04f, -.18f, 4);
            var ring = z.At(4f, -5f) + up * 5f; var om = SongGlow(no, Cyan, 1.6f); for (var k = 0; k < 24; k++) { var a0 = k * 15f * Mathf.Deg2Rad; var a1 = (k + 1) * 15f * Mathf.Deg2Rad; var p0 = ring + new Vector3(Mathf.Cos(a0), 0, Mathf.Sin(a0)) * 2.4f; var p1 = ring + new Vector3(Mathf.Cos(a1), 0, Mathf.Sin(a1)) * 2.4f; z.mk.Prism(om, p0, (p1 - p0).normalized, .07f, Vector3.Distance(p0, p1), 5); } z.mk.Blob(om, ring + up * .4f, new Vector3(.8f, .4f, .8f), rng, 0f); foreach (var e in new[] { -1f, 1f }) z.mk.Prism(chrome, ring + sd * e * 2.4f, up, .02f, 3f, 3); halos.Add(ring, 3f, Cyan, .3f);
            Folk(f.P(1f, .02f, 2.4f), fr, Resident.Pose.Lean, new[] { "Every song's about a night like this one. That's not an accident.", "Take her out on the Coast Road. Keys are in it. They're always in it.", "The sax player comes by sometimes. Leaves the sax. Never says why." });
            Shot(z.ctx, "the midnight's garage", z.At(13f, 4f) + up * 2.2f, f.P(0, 2f, 0));
        }

        // ---------------------------------------------------------------- everyone else: one thing from their song
        void Others(Plot z, PlaceFrame f)
        {
            var up = Vector3.up; var rot = z.Rot; var rng = new System.Random(z.a.name.Length * 7 + z.a.name[0]); var c = z.At(2.6f, 1f) + up * .05f; var fr = z.front; var sd = z.side;
            void Post(Vector3 at, float h, float r = .04f) { z.mk.Prism(chrome, at - up * .2f, up, r, h + .2f, 5); ArtistBits.PostCollider(z.root, at, h, r + .03f); }
            void Plinth(Vector3 at, float w, float h) { z.mk.Box(dark, at + up * (h / 2f), new Vector3(w, h, w), rot); z.mk.Box(cyanN, at + up * h, new Vector3(w + .02f, .03f, w + .02f), rot); BoxCol(z.root, at + up * (h / 2f), new Vector3(w, h, w), rot); }
            switch (z.a.name)
            {
                case "Blood Orange": { var s = Song(z, "Best to You"); Plinth(c, .8f, .8f); z.mk.Blob(SongGlow(s, new Color(1f, .3f, .2f), 1f), c + up * 1.2f, Vector3.one * .38f, rng, 0f); z.mk.Prism(Paint(new Color(.6f, .1f, .14f)), c + up * 1.2f + fr * .36f, fr, .3f, .03f, 12); break; }
                case "Cannons": { var s = Song(z, "Light as a Feather"); Post(c, 1.4f, .006f); z.mk.Box(SongGlow(s, new Color(.9f, .8f, 1f), 1.4f), c + up * 2.2f, new Vector3(.14f, 1f, .02f), rot * Quaternion.Euler(0, 0, 20f)); halos.Add(c + up * 2.2f, 1.2f, new Color(.9f, .8f, 1f), .4f); break; }
                case "FM-84": { var s = Song(z, "Arcade Summer"); life.arcadeSong = s; z.mk.Box(dark, c + up * .9f, new Vector3(.8f, 1.8f, .8f), rot); z.mk.Box(SongGlow(s, new Color(1f, .5f, .2f), 1.3f), c + up * 1.3f + fr * .41f, new Vector3(.6f, .5f, .02f), rot); z.mk.Box(pinkN, c + up * 1.8f + fr * .41f, new Vector3(.7f, .16f, .02f), rot); BoxCol(z.root, c + up * .9f, new Vector3(.8f, 1.8f, .8f), rot); break; }
                case "Futurecop!": { var s = Song(z, "We Belong"); foreach (var e in new[] { -1f, 1f }) { var fp = c + sd * e * .4f; var m = SongGlow(e < 0 ? s : null, e < 0 ? Cyan : Magenta, 1.2f); z.mk.Prism(m, fp, up, .1f, 1f, 6); z.mk.Blob(m, fp + up * 1.2f, Vector3.one * .16f, rng, 0f); } z.mk.Prism(SongGlow(s, Cream, 1f), c + sd * -.3f + up * .8f, sd, .03f, .6f, 4); break; }
                case "Gost": { var s = Song(z, "Genesee"); life.geneseeSong = s; Post(c, 2.4f, .04f); z.mk.Box(SongGlow(s, new Color(.8f, .1f, .1f), .6f), c + up * 2.2f + sd * .6f, new Vector3(1.6f, .4f, .04f), rot); ArtistBits.Label(z.labels, "GENESEE AVE", c + up * 2.2f + sd * .6f + fr * .03f, fr, .14f, Cream, TextAnchor.MiddleCenter); break; }
                case "Haute & Freddy": { var s = Song(z, "Femme Hysteria"); Plinth(c, .7f, .8f); z.mk.Prism(chrome, c + up * .8f, up, .12f, .4f, 8); z.mk.Prism(SongGlow(s, Magenta, 1.4f), c + up * 1.2f, up, .1f, .4f, 8, .5f); break; }
                case "Home": { var s = Song(z, "Nosebleed"); Plinth(c, 1f, .8f); z.mk.Box(dark, c + up * .95f, new Vector3(.9f, .3f, .5f), rot); z.mk.Box(SongGlow(s, new Color(1f, .6f, .3f), .8f), c + up * .98f + fr * .26f, new Vector3(.5f, .14f, .01f), rot); foreach (var e in new[] { -.15f, .15f }) z.mk.Prism(chrome, c + up * .98f + fr * .26f + sd * e, fr, .05f, .01f, 8); break; }
                case "Love Spells": { var s = Song(z, "Keep It To Yourself"); Plinth(c, .7f, .8f); var lp = c + up * 1.1f; z.mk.Box(SongGlow(s, new Color(1f, .7f, .9f), 1f), lp, new Vector3(.4f, .34f, .2f), rot); for (var k = 0; k < 8; k++) { var a = k * Mathf.PI / 7f; z.mk.Box(chrome, lp + up * (.2f + Mathf.Sin(a) * .2f) + sd * Mathf.Cos(a) * .14f, new Vector3(.05f, .05f, .05f), rot); } break; }
                case "NINA": { var s = Song(z, "Beyond Memory"); Plinth(c, .8f, .8f); z.mk.Box(dark, c + up * 1f, new Vector3(.7f, .1f, .4f), rot * Quaternion.Euler(0, 20f, 0)); z.mk.Box(SongGlow(s, Cyan, .7f), c + up * 1.06f, new Vector3(.4f, .01f, .2f), rot * Quaternion.Euler(0, 20f, 0)); break; }
                case "ODESZA": { var s = Song(z, "A Moment Apart"); life.momentSong = s; var rc = c + up * 1.6f; var m = SongGlow(s, new Color(1f, .8f, .5f), 1.4f); foreach (var e in new[] { -1f, 1f }) for (var k = 0; k < 10; k++) { var a0 = (90f + e * (15f + k * 15f)) * Mathf.Deg2Rad; var a1 = (90f + e * (15f + (k + 1) * 15f)) * Mathf.Deg2Rad; var p0 = rc + sd * Mathf.Cos(a0) * 1f + up * Mathf.Sin(a0) * 1f + sd * e * .2f; var p1 = rc + sd * Mathf.Cos(a1) * 1f + up * Mathf.Sin(a1) * 1f + sd * e * .2f; z.mk.Prism(m, p0, (p1 - p0).normalized, .05f, Vector3.Distance(p0, p1), 4); } Post(c, .5f, .03f); halos.Add(rc, 1.6f, new Color(1f, .8f, .5f), .3f); break; }
                case "Panama": { var s = Song(z, "Always"); life.alwaysSong = s; NeonPalm(z.ctx, z.mk, c, .8f, rng, 1); z.mk.Prism(SongGlow(s, new Color(1f, .5f, .7f), 1.2f), c + up * .02f, up, 1f, .02f, 14); break; }
                case "Parallels": { var s = Song(z, "Dry Blood"); Post(c, 1.4f, .03f); var hc = c + up * 2f; var hm = SongGlow(s, new Color(1f, .1f, .2f), 1.4f); foreach (var e in new[] { -1f, 1f }) { z.mk.Blob(hm, hc + sd * e * .2f + up * .1f, Vector3.one * .22f, rng, 0f); z.mk.Tri(hm, hc + sd * e * .4f, hc + sd * e * .02f + up * .1f, hc - up * .4f + sd * e * .04f, hc + fr); z.mk.Tri(hm, hc + sd * e * .4f, hc + sd * e * .02f + up * .1f, hc - up * .4f + sd * e * .04f, hc - fr); } z.mk.Prism(hm, hc - up * .4f, -up, .03f, .4f, 4, .2f); break; }
                case "Radio Wolf": { var s = Song(z, "Journeys End"); Post(c, 2.2f, .04f); z.mk.Box(SongGlow(s, SunGold, .6f), c + up * 2f, new Vector3(1.4f, .5f, .04f), rot); ArtistBits.Label(z.labels, "ROAD ENDS", c + up * 2f + fr * .03f, fr, .14f, Ink, TextAnchor.MiddleCenter); break; }
                case "SG Lewis": { var s = Song(z, "Feelings Gone"); Post(c, 2.6f, .03f); z.mk.Blob(SongGlow(s, new Color(.7f, .7f, .9f), .6f), c + up * 2.9f, Vector3.one * .3f, rng, .05f); break; }
                case "Washed Out": { var s = Song(z, "Feel It All Around"); life.feelSong = s; var hm = c + up * .8f; foreach (var e in new[] { -1.2f, 1.2f }) Post(c + sd * e, 1.4f, .05f); for (var k = 0; k <= 8; k++) { var t = k / 8f; z.mk.Box(SongGlow(k == 4 ? s : null, new Color(1f, .7f, .5f), .6f), c + sd * Mathf.Lerp(-1.1f, 1.1f, t) + up * (1.2f - Mathf.Sin(t * Mathf.PI) * .5f), new Vector3(.28f, .03f, .7f), rot); } Interact.Seat(tlife.transform, c + up * .8f, fr, "Lie in the hammock").seatHeight = .1f; break; }
                case "YELLOW MAGIC ORCHESTRA": { var s = Song(z, "RYDEEN"); life.rydeenSong = s; z.mk.Box(dark, c + up * .8f, new Vector3(2f, .2f, .7f), rot); foreach (var e in new[] { -.9f, .9f }) z.mk.Box(chrome, c + up * .4f + sd * e, new Vector3(.05f, .8f, .6f), rot); for (var k = 0; k < 14; k++) z.mk.Box(k % 2 == 0 ? white : SongGlow(k == 5 ? s : null, Cyan, .7f), c + up * .92f + sd * (-.85f + k * .13f) + fr * .1f, new Vector3(.1f, .03f, .36f), rot); BoxCol(z.root, c + up * .5f, new Vector3(2f, 1f, .7f), rot); break; }
                default: { if (SongIds.TryGetValue(z.a.name, out var list) && list.Length > 0) { var s = Song(z, list[0].title); Post(c, 1.4f); z.mk.Blob(SongGlow(s, Magenta, 1.6f), c + up * 1.6f, Vector3.one * .2f, rng, 0f); } break; }
            }
        }
    }
}
