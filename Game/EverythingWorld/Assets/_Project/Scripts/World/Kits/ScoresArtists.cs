using System.Collections.Generic;
using UnityEngine;
using static EverythingWorld.World.PlaceTools;

namespace EverythingWorld.World
{
    /// The Isle of Scores' artists. Most stand on a round stone dais with a music stand bearing their name and one thing
    /// from their film; covers on a royal-blue board with a gold frame.
    /// - John Williams: the starship corner at the end of the Star Road. A desert under two suns on tall poles, a starship
    ///   landed on its legs with its ramp down and blue engines, and a piece for every song: a slab of gold text tilting up
    ///   into the stars ("Star Wars"), a black stair edged in red with black banners ("The Imperial March"), a lava pool with
    ///   a red and a blue blade crossed over it ("Duel of the Fates", "Anakin vs. Obi-Wan"), a domed cantina with a little
    ///   band of stools and horns ("Cantina Band"), a gun turret ("Imperial Attack"), a podium of three medals ("Finale").
    /// - Danny Elfman: a Halloween town under a huge moon. A curly hill, crooked houses with orange windows, a gate, pumpkins
    ///   and gravestones and a bare tree; a little snowy pine with lights ("What's This?") and a bathtub on clawed legs
    ///   ("Kidnap the Sandy Claws"). The Citizens of Halloween have the pumpkin patch and a scarecrow next door.
    /// - The rest: a pavilion with a zither and a tree of white blossom; a circle of grave markers in the sand round a gold
    ///   glow; a stone temple door with a golden idol; a hang glider; a red bridge over a stream; a floodlight mast with
    ///   lasers; a flying saucer and its beam; a white mountain of spires; a tutu and a swan; winged helmets on spears; a
    ///   grand piano; striped tufty trees and a seed in a pot.
    public partial class ScoresKit
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
                Add("John Williams", "The Imperial March", "John Williams"); Add("John Williams", "Star Wars", "John Williams"); Add("John Williams", "Duel of the Fates", "John Williams");
                Add("The Citizens of Halloween", "This Is Halloween", "Danny Elfman"); Add("Danny Elfman", "What's This", "Danny Elfman"); Add("Richard Wagner", "Ride of the Valkyries", "Richard Wagner");
                Add("Pyotr Ilyich Tchaikovsky", "Tchaikovsky: Swan Lake", "@the lake of swans"); Add("Ennio Morricone", "L'Estasi", "Ennio Morricone"); Add("Mark Snow", "Materia", "Mark Snow"); Add("Jerry Goldsmith", "Soarin", "Jerry Goldsmith");
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
            if (stone == null) MakeMaterials();
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
                case "John Williams": Williams(z); break;
                case "Danny Elfman": Elfman(z); break;
                default: Others(z, Dais(z)); break;
            }
            SongLamps(z);
            Board(z, z.At(reach * .6f, -reach * .45f - 1.5f), front);
            ArtistBits.Plaque(z.mk, z.solid, labels, a, z.At(reach * .6f + 1.5f, reach * .45f + 1.5f), front, iron, navy, Gold, 1.6f);
            Lamp(ctx, z.mk, z.At(reach * .5f, reach * .7f), false);
            if (z.area.glowLight == null) z.area.glowLight = ArtistBits.PointLight(root, z.c + up * 4f + front * 3f, new Color(1f, .88f, .64f), HasStation(a) ? 24f : 14f, 1.3f);
            area.glowRenderers = new Renderer[0];
            z.mk.Build("Area", root, false);
            z.solid.BuildCollider("Walls", root);
            z.pictures.Build("Covers", root, false);
            noGrass.Add(new Vector3(z.c.x, reach * .9f, z.c.z));
            var mark = new GameObject("Shot: " + a.name + " up close").transform; mark.SetParent(ctx.Root, false);
            var cam = z.At(reach * .85f, reach * .3f) + up * 2f; var look = z.At(-reach * .3f, 0) + up * 1.8f - cam; mark.SetPositionAndRotation(cam, Quaternion.LookRotation(look.normalized));
        }

        /// The covers on a gold-framed royal-blue board, like a concert programme.
        void Board(Plot z, Vector3 ground, Vector3 dir)
        {
            dir = Vector3.ProjectOnPlane(dir, Vector3.up).normalized; var rot = Quaternion.LookRotation(dir);
            var n = Mathf.Max(1, Mathf.Min(7, z.a.songList?.Length ?? 1)); const float size = .9f; var w = n * size * 1.25f + .6f;
            var c = ground + Vector3.up * 1.7f;
            z.mk.Box(goldM, c - Vector3.up * .5f - dir * .08f, new Vector3(w + .6f, size + 2.1f, .16f), rot); z.mk.Box(navy, c - Vector3.up * .5f - dir * .02f, new Vector3(w + .4f, size + 1.9f, .2f), rot); z.solid.Box(iron, c - Vector3.up * .5f, new Vector3(w + .6f, size + 2.1f, .3f), rot);
            z.mk.Box(Paint(new Color(.9f, .88f, .82f)), c - Vector3.up * .1f + dir * .09f, new Vector3(w, size + .9f, .04f), rot);
            foreach (var e in new[] { -1f, 1f }) z.mk.Prism(iron, ground + Vector3.Cross(Vector3.up, dir) * e * (w / 2f), Vector3.up, .05f, 1.2f, 5);
            ArtistBits.CoverWall(z.pictures, z.labels, z.ctx, z.a, c + dir * .12f + Vector3.up * .08f, dir, n, size, n, true, Ink);
        }

        /// A little music stand with a gold light for each song without its own piece.
        void SongLamps(Plot z)
        {
            if (!SongIds.TryGetValue(z.a.name, out var list)) return;
            var up = Vector3.up; var k = 0; var seen = new HashSet<string>();
            foreach (var (_, id) in list)
            {
                if (z.lit.Contains(id) || !seen.Add(id)) continue;
                var p = z.At(Foot(z.a) * .55f - (k % 2) * 1.3f, Foot(z.a) * .2f + (k / 2) * 1.3f);
                z.mk.Prism(iron, p - up * .1f, up, .03f, 1.2f, 5); z.mk.Box(iron, p + up * 1.2f, new Vector3(.5f, .36f, .02f), z.Rot * Quaternion.Euler(-20f, 0, 0)); z.mk.Blob(SongGlow(id, Gold, 1.6f), p + up * 1.46f + z.front * .06f, Vector3.one * .1f, new System.Random(k), 0f); ArtistBits.PostCollider(z.root, p, 1.3f, .1f);
                k++;
            }
        }

        /// A round stone dais with a gold rim and a music stand bearing their name.
        PlaceFrame Dais(Plot z)
        {
            var up = Vector3.up; var f = new PlaceFrame(z.At(-Foot(z.a) * .45f, 0), z.front);
            z.mk.Prism(stone, z.c - up * .25f, up, Foot(z.a) * .7f, .4f, 24); z.mk.Prism(goldM, z.c + up * .15f, up, Foot(z.a) * .7f - .3f, .02f, 24); z.mk.Prism(stone, z.c + up * .16f, up, Foot(z.a) * .7f - .5f, .02f, 24);
            var ms = f.P(0, 0, 0) + up * .15f; z.mk.Prism(iron, ms, up, .04f, 1.4f, 5); z.mk.Box(navy, ms + up * 1.6f, new Vector3(1.8f, .8f, .06f), f.r * Quaternion.Euler(-15f, 0, 0)); z.mk.Box(goldM, ms + up * 1.98f + f.Fwd * .1f, new Vector3(1.8f, .06f, .06f), f.r); ArtistBits.Label(z.labels, z.a.name, ms + up * 1.6f + f.Fwd * .1f, f.Fwd, .16f, Gold, TextAnchor.MiddleCenter); BoxCol(z.root, ms + up * 1f, new Vector3(1.9f, 2f, .4f), f.r);
            return f;
        }

        // ---------------------------------------------------------------- John Williams: the starship corner
        void Williams(Plot z)
        {
            var up = Vector3.up; var rng = new System.Random(1977); var fr = z.front; var sd = z.side; var rot = z.Rot;
            z.mk.Prism(sand, z.c - up * .25f, up, Foot(z.a) * .92f, .32f, 30); for (var k = 0; k < 9; k++) z.mk.Blob(sand, z.At(-6f + (k % 3) * 5f, -12f + (k / 3) * 11f) - up * .2f, new Vector3(2.6f, .6f, 1.6f), rng, .2f);
            // two suns on tall poles behind
            foreach (var (fw, rt, h, r, col) in new[] { (-16f, -9f, 14f, 2.4f, Ember), (-16f, -3f, 12f, 1.7f, new Color(1f, .9f, .5f)) }) { var sp = z.At(fw, rt); z.mk.Prism(iron, sp, up, .08f, h - r, 5); z.mk.Prism(GlowM(col, 2f), sp + up * h - fr * .1f, fr, r, .2f, 24); halos.Add(sp + up * h + fr * .6f, r * 2.4f, col, .35f); ArtistBits.PostCollider(z.root, sp, h - r, .12f); }
            var mainT = Song(z, "Star Wars"); var march = Song(z, "The Imperial March"); var duel = Song(z, "Duel of the Fates"); var anakin = Song(z, "Anakin"); var cantina = Song(z, "Cantina"); var attack = Song(z, "Imperial Attack"); var finale = Song(z, "Finale");
            life.marchSong = march; life.starSong = mainT; life.duelSong = duel;
            // the starship: a long grey hull on legs, tapered nose, blue engines, ramp down
            var sh = z.At(-5f, -7f) + up * 2.2f; var sr = Quaternion.LookRotation(sd); var hull = Paint(new Color(.56f, .58f, .62f)); var hullD = Paint(new Color(.36f, .38f, .42f));
            z.mk.Box(hull, sh, new Vector3(4.4f, 1.6f, 9f), sr); z.mk.Prism(hull, sh + sd * 4.5f, sd, 1.6f, 4f, 4, .2f, 45f); z.mk.Box(hullD, sh + up * .9f - sd * 1f, new Vector3(3f, .4f, 5f), sr); z.mk.Blob(Paint(new Color(.3f, .4f, .6f)), sh + up * .9f + sd * 2.4f, new Vector3(.7f, .4f, .9f), rng, 0f);
            foreach (var e in new[] { -1.4f, 1.4f }) { z.mk.Prism(hullD, sh - sd * 4.5f + fr * e, -sd, .7f, 1f, 10); z.mk.Prism(GlowM(new Color(.4f, .7f, 1f), 2.2f), sh - sd * 5.5f + fr * e, -sd, .55f, .04f, 10); halos.Add(sh - sd * 6f + fr * e, 1.6f, new Color(.4f, .7f, 1f), .4f); }
            foreach (var e in new[] { -1.6f, 1.6f }) foreach (var q in new[] { -3f, 3f }) { var lg = sh - up * .8f + fr * e + sd * q; var ft = lg; ft.y = z.ctx.Ground(lg.x, lg.z); z.mk.Prism(iron, ft, up, .12f, lg.y - ft.y, 5); z.mk.Prism(iron, ft, up, .4f, .1f, 6); }
            BoxCol(z.root, sh - sd * 5f, new Vector3(4.4f, 1.6f, 1.6f), sr); var g0 = z.ctx.Ground(sh.x, sh.z); z.solid.Box(hull, new Vector3(sh.x, (g0 + sh.y + .8f) / 2f, sh.z), new Vector3(4.4f, sh.y + .8f - g0, 9f), sr);
            var rp0 = sh - up * .8f + fr * 2.2f; var rp1 = rp0 + fr * 3f; rp1.y = z.ctx.Ground(rp1.x, rp1.z) + .05f; z.mk.Box(hullD, (rp0 + rp1) / 2f, new Vector3(1.6f, .1f, Vector3.Distance(rp0, rp1)), Quaternion.LookRotation((rp1 - rp0).normalized)); z.mk.Box(lampM, rp0 + up * .02f, new Vector3(1.4f, .04f, .1f), rot);
            // "Star Wars": a slab of gold text tilting away up into the stars
            var cr = new PlaceFrame(z.At(-11f, 7f), fr); var tilt = Quaternion.Euler(-62f, 0, 0); var crawl = SongGlow(mainT, Gold, 1.2f); for (var k = 0; k < 9; k++) { var row = cr.o + up * .4f + cr.r * tilt * new Vector3(0, k * .9f, 0); var wid = 5.2f - k * .3f; for (var j = 0; j < 4; j++) z.mk.Box(crawl, row + cr.r * tilt * new Vector3(-wid / 2f + (j + .5f) * wid / 4f, 0, .02f), new Vector3(wid / 4f - .2f, .14f, .02f), cr.r * tilt); } z.mk.Box(dark, cr.o + up * .4f + cr.r * tilt * new Vector3(0, 3.6f, -.04f), new Vector3(5.8f, .06f, 8.4f), cr.r * tilt * Quaternion.Euler(90f, 0, 0)); ArtistBits.PostCollider(z.root, cr.o, 2f, 2.6f);
            // "The Imperial March": a black stair edged in red, black banners either side
            var im = new PlaceFrame(z.At(3f, 9f), -sd); for (var k = 0; k < 5; k++) { z.mk.Box(dark, im.P(0, .2f + k * .4f, -k * .7f), new Vector3(3f, .4f + k * .8f, .7f), im.r); z.solid.Box(dark, im.P(0, .2f + k * .4f, -k * .7f), new Vector3(3f, .4f + k * .8f, .7f), im.r); foreach (var e in new[] { -1.52f, 1.52f }) z.mk.Box(SongGlow(k == 4 ? march : null, new Color(1f, .1f, .1f), 1.4f), im.P(e, .4f + k * .8f, -k * .7f), new Vector3(.04f, .06f, .7f), im.r); }
            foreach (var e in new[] { -2.4f, 2.4f }) { var bp = im.P(e, 0, -2f); z.mk.Prism(iron, bp, up, .06f, 5f, 5); z.mk.Box(dark, bp + up * 3.4f + im.Fwd * .06f, new Vector3(1.2f, 3f, .04f), im.r); z.mk.Box(GlowM(new Color(1f, .1f, .1f), 1f), bp + up * 3.4f + im.Fwd * .09f, new Vector3(.14f, 3f, .02f), im.r); ArtistBits.PostCollider(z.root, bp, 5f, .1f); }
            // "Duel of the Fates": a lava pool with a red blade and a blue blade crossed over it
            var lv = z.At(5f, -4f); z.mk.Prism(stoneD, lv - up * .2f, up, 3f, .5f, 18); z.mk.Prism(SongGlow(duel, Ember, 1.6f), lv + up * .31f, up, 2.6f, .02f, 18); halos.Add(lv + up * .8f, 3f, Ember, .3f); BoxCol(z.root, lv, new Vector3(5.6f, .9f, 5.6f), rot);
            var bl = lv + up * 2.2f; z.mk.Prism(GlowM(new Color(1f, .15f, .15f), 2.4f), bl - (sd + up * .5f).normalized * .9f, (sd + up * .5f).normalized, .05f, 1.8f, 6); z.mk.Prism(SongGlow(anakin, new Color(.3f, .5f, 1f), 2.4f), bl - (-sd + up * .5f).normalized * .9f, (-sd + up * .5f).normalized, .05f, 1.8f, 6); ArtistBits.PointLight(z.root, bl, new Color(.8f, .4f, .8f), 8f, 1.2f);
            // "Cantina Band": a little dome with a lit door, three stools and horns
            var ct = z.At(-3f, 5f); z.mk.Blob(Paint(new Color(.74f, .64f, .5f)), ct + up * .6f, new Vector3(2.4f, 2f, 2.4f), rng, .05f); var cdir = (fr + sd).normalized; z.mk.Box(windowM, ct + cdir * 2.36f + up * 1f, new Vector3(1f, 1.8f, .04f), Quaternion.LookRotation(cdir)); BoxCol(z.root, ct + up * 1f, new Vector3(4f, 2.4f, 4f), Quaternion.LookRotation(cdir)); for (var k = 0; k < 3; k++) { var st = ct + cdir * 3.6f + Vector3.Cross(up, cdir) * (k - 1) * 1.1f; z.mk.Prism(iron, st, up, .04f, .6f, 4); z.mk.Prism(Paint(new Color(.5f, .3f, .2f)), st + up * .6f, up, .22f, .06f, 8); ArtistBits.PostCollider(z.root, st, .9f, .26f); z.mk.Prism(k == 1 ? SongGlow(cantina, Gold, 1f) : goldM, st + up * 1f, (cdir + up).normalized, .04f, .5f, 6, 3f); }
            // "Imperial Attack": a grey turret with two barrels
            var tu = z.At(8f, 3f); z.mk.Prism(hullD, tu, up, .9f, 1.2f, 10); z.mk.Blob(hull, tu + up * 1.3f, new Vector3(.9f, .6f, .9f), rng, 0f); foreach (var e in new[] { -.25f, .25f }) z.mk.Prism(dark, tu + up * 1.4f + sd * e, (fr + up * .3f).normalized, .07f, 2f, 6); z.mk.Blob(SongGlow(attack, new Color(.4f, 1f, .4f), 1.2f), tu + up * 1.4f + (fr + up * .3f).normalized * 2f, Vector3.one * .1f, rng, 0f); ArtistBits.PostCollider(z.root, tu, 1.8f, 1f);
            // "Finale": a podium of three medals
            var fp = z.At(7f, -9f); for (var k = 0; k < 3; k++) { var h = new[] { .6f, .9f, .4f }[k]; var pp = fp + sd * (k - 1) * 1.1f; z.mk.Box(stone, pp + up * h / 2f, new Vector3(1f, h, 1f), rot); z.mk.Prism(k == 1 ? SongGlow(finale, Gold, 1.4f) : goldM, pp + up * (h + .5f), fr, .22f, .04f, 14); z.mk.Prism(Paint(Velvet), pp + up * (h + .7f), up, .02f, .2f, 4); } BoxCol(z.root, fp + up * .45f, new Vector3(3.3f, .9f, 1f), rot);
            // a tall moisture tower and a little droid
            var mt = z.At(-9f, 12f); z.mk.Prism(hull, mt, up, .3f, 4f, 8); for (var k = 0; k < 3; k++) z.mk.Prism(hullD, mt + up * (1.2f + k * 1f), up, .55f, .2f, 8); z.mk.Blob(lampM, mt + up * 4.1f, Vector3.one * .14f, rng, 0f); ArtistBits.PostCollider(z.root, mt, 4f, .5f);
            var dr = new MeshKit(); dr.Prism(white, Vector3.zero, Vector3.up, .25f, .6f, 10); dr.Blob(Paint(new Color(.3f, .4f, .8f)), Vector3.up * .62f, new Vector3(.25f, .18f, .25f), rng, 0f); dr.Blob(GlowM(new Color(.4f, .6f, 1f), 1.2f), Vector3.up * .66f + Vector3.forward * .22f, Vector3.one * .05f, rng, 0f); Thing(dr, "Little droid", "a little droid (it beeps)", z.At(2f, 0f), rot, new Vector3(.5f, .8f, .5f), 1.2f, false);
            Folk(z.At(6f, 10f), -fr, Resident.Pose.Stand, new[] { "Two suns. You get used to it.", "The ship's not going anywhere. I asked.", "When the march plays, everything goes red. I don't like it. I love it." }, Local());
            z.area.glowLight = ArtistBits.PointLight(z.root, z.c + up * 8f, new Color(1f, .8f, .6f), 34f, 1.8f);
            Shot(z.ctx, "the starship under two suns", z.At(12f, 4f) + up * 2.4f, sh);
        }

        // ---------------------------------------------------------------- Danny Elfman: the Halloween town
        void Elfman(Plot z)
        {
            var up = Vector3.up; var rng = new System.Random(1993); var fr = z.front; var sd = z.side; var rot = z.Rot;
            z.mk.Prism(Paint(new Color(.2f, .18f, .24f)), z.c - up * .25f, up, Foot(z.a) * .92f, .32f, 28);
            var lament = Song(z, "Jack's Lament"); var whats = Song(z, "What's This"); var kidnap = Song(z, "Kidnap"); life.whatsSong = whats;
            // the moon, huge, behind the curly hill
            var mp = z.At(-Foot(z.a) * .95f, 0) + up * 10f; z.mk.Prism(SongGlow(lament, new Color(.96f, .94f, .8f), 1.6f), mp, fr, 4.6f, .2f, 32); halos.Add(mp + fr * 1f, 10f, new Color(.96f, .94f, .8f), .3f); z.mk.Prism(iron, z.At(-Foot(z.a) * .95f, 0) - fr * .3f, up, .1f, 10f, 5); ArtistBits.PostCollider(z.root, z.At(-Foot(z.a) * .95f, 0), 10f, .2f);
            // the curly hill: a ramp of dark stone that rises and curls over at the top
            var hs = z.At(-4f, -5f); var hillM = Paint(new Color(.22f, .2f, .26f));
            for (var k = 0; k < 20; k++) { var a = k / 19f; var ang = a * 1.4f * Mathf.PI; var r = 3.6f * (1f - a * .55f); var p = hs + sd * (a * 6f) + up * (a * 4.4f + Mathf.Sin(ang) * 1.6f * a) - fr * (Mathf.Cos(ang) * r - 3.6f) * .7f; z.mk.Box(hillM, p, new Vector3(1.2f, .3f, 1.4f), Quaternion.LookRotation(fr) * Quaternion.Euler(0, 0, -20f * a)); if (k < 12) z.solid.Box(hillM, p - up * .2f, new Vector3(1.2f, .5f, 1.4f), Quaternion.LookRotation(fr) * Quaternion.Euler(0, 0, -20f * a)); }
            for (var k = 0; k < 8; k++) { var p = hs + sd * (k * .8f); z.mk.Box(hillM, p + up * (k * .55f) / 2f, new Vector3(.9f, k * .55f + .1f, 1.4f), rot); z.solid.Box(hillM, p + up * (k * .55f) / 2f, new Vector3(.9f, k * .55f + .1f, 1.4f), rot); }
            // crooked houses with orange windows
            foreach (var (fw, rt, h, lean) in new[] { (-5f, 9f, 6f, 6f), (2f, 11f, 4.6f, -8f), (4f, -11f, 5.4f, 5f) })
            {
                var hp = z.At(fw, rt); var hf = FaceTo(hp, z.c); var hr = Quaternion.LookRotation(hf) * Quaternion.Euler(0, 0, lean); var body = Paint(new Color(.3f, .28f, .34f));
                z.mk.Box(body, hp + up * h / 2f, new Vector3(3f, h, 3f), hr); z.solid.Box(body, hp + up * h / 2f, new Vector3(3f, h, 3f), hr); z.mk.Prism(Paint(new Color(.16f, .14f, .18f)), hp + up * h, up, 2.3f, 2.4f, 4, .1f, 45f + lean); var hside = hr * Vector3.right;
                for (var k = 0; k < 3; k++) z.mk.Box(windowM, hp + up * (1.4f + k * 1.4f) + hf * 1.51f + hside * ((k % 2) * 1.2f - .6f) + hr * Vector3.up * 0f, new Vector3(.6f, .8f, .02f), hr); z.mk.Box(dark, hp + up * .9f + hf * 1.52f - hside * .7f, new Vector3(.8f, 1.6f, .02f), hr);
            }
            // a gate with a curl on top
            var gp = new PlaceFrame(z.At(Foot(z.a) * .72f, 0), fr); foreach (var e in new[] { -2.4f, 2.4f }) { z.mk.Prism(iron, gp.P(e, 0, 0), up, .12f, 3.4f, 6); ArtistBits.PostCollider(z.root, gp.P(e, 0, 0), 3.4f, .16f); z.mk.Blob(GlowM(Ember, 1.6f), gp.P(e, 3.6f, 0), new Vector3(.4f, .32f, .4f), rng, .1f); } for (var k = 0; k <= 12; k++) { var a = Mathf.PI * k / 12f; z.mk.Box(iron, gp.P(Mathf.Cos(a) * 2.4f, 3.4f + Mathf.Sin(a) * 1f, 0), new Vector3(.3f, .08f, .08f), gp.r * Quaternion.Euler(0, 0, a * Mathf.Rad2Deg + 90f)); } z.mk.Prism(iron, gp.P(0, 4.4f, 0), gp.Fwd, .5f, .06f, 12); z.mk.Prism(Paint(new Color(.2f, .18f, .24f)), gp.P(0, 4.4f, -.01f), gp.Fwd, .4f, .08f, 12);
            // pumpkins, gravestones, a bare twisted tree
            for (var k = 0; k < 14; k++) { var pp = z.At(-8f + (float)rng.NextDouble() * 16f, -12f + (float)rng.NextDouble() * 24f); if (Vector3.Distance(pp, hs) < 4f) continue; z.mk.Blob(k % 3 == 0 ? GlowM(Ember, 1.6f) : Paint(new Color(.9f, .45f, .1f)), pp + up * .25f, new Vector3(.34f, .26f, .34f), rng, .1f); z.mk.Prism(Paint(new Color(.2f, .4f, .2f)), pp + up * .5f, up, .04f, .14f, 4); }
            for (var k = 0; k < 6; k++) { var gs = z.At(-10f + k * 1.4f, 3f + (k % 2) * 1.4f); z.mk.Box(stoneD, gs + up * .5f, new Vector3(.7f, 1f, .2f), Quaternion.LookRotation(fr) * Quaternion.Euler(0, 0, (k % 3 - 1) * 8f)); z.mk.Prism(stoneD, gs + up * 1f, fr, .35f, .2f, 10); BoxCol(z.root, gs + up * .6f, new Vector3(.7f, 1.2f, .3f), rot); }
            var tr = z.At(-2f, -12f); var prev = tr; for (var k = 0; k < 6; k++) { var dir = (up + sd * Mathf.Sin(k * 1.3f) * .5f).normalized; z.mk.Prism(Paint(new Color(.14f, .12f, .14f)), prev, dir, .3f - k * .04f, 1f, 5); prev += dir; if (k > 2) for (var j = 0; j < 2; j++) { var bd = (Quaternion.Euler(0, k * 70f + j * 160f, 0) * fr + up * .4f).normalized; z.mk.Prism(Paint(new Color(.14f, .12f, .14f)), prev, bd, .06f, 1.6f, 4, .3f); } } ArtistBits.PostCollider(z.root, tr, 4f, .4f);
            // "What's This?": a little snowy pine wound with lights
            var pn = z.At(3f, 6f); z.mk.Prism(trunkM, pn, up, .14f, .6f, 5); for (var k = 0; k < 3; k++) { z.mk.Cone(Paint(new Color(.14f, .34f, .26f)), pn + up * (.5f + k * .9f), 1.3f - k * .35f, 1.4f, 8); z.mk.Cone(white, pn + up * (1.2f + k * .9f), .6f - k * .16f, .7f, 8); } for (var k = 0; k < 14; k++) { var a = k * 1.1f; var lh = .7f + k * .2f; z.mk.Blob(k % 3 == 0 ? SongGlow(k == 0 ? whats : null, new Color(1f, .3f, .3f), 1.6f) : GlowM(k % 3 == 1 ? new Color(1f, .9f, .4f) : new Color(.4f, .8f, 1f), 1.6f), pn + up * lh + (fr * Mathf.Cos(a) + sd * Mathf.Sin(a)) * (1.2f - k * .07f), Vector3.one * .08f, rng, 0f); } z.mk.Cone(GlowM(Gold, 1.6f), pn + up * 3.4f, .2f, .3f, 5); ArtistBits.PostCollider(z.root, pn, 3f, 1f);
            // "Kidnap the Sandy Claws": a bathtub on clawed legs
            var bt = z.At(3f, -5f); z.mk.Blob(white, bt + up * 1f, new Vector3(1.3f, .5f, .6f), rng, 0f); z.mk.Blob(dark, bt + up * 1.32f, new Vector3(1.1f, .2f, .45f), rng, 0f); foreach (var e in new[] { -.9f, .9f }) foreach (var q in new[] { -.35f, .35f }) { var lg = bt + sd * e + fr * q; z.mk.Prism(Paint(new Color(.7f, .6f, .3f)), lg, up, .06f, .7f, 5); for (var c = 0; c < 3; c++) z.mk.Box(Paint(new Color(.7f, .6f, .3f)), lg + Quaternion.Euler(0, c * 60f - 60f, 0) * (sd * Mathf.Sign(e)) * .14f, new Vector3(.04f, .04f, .26f), Quaternion.Euler(0, c * 60f - 60f, 0) * Quaternion.LookRotation(sd * Mathf.Sign(e))); } z.mk.Box(SongGlow(kidnap, new Color(.5f, 1f, .4f), 1f), bt + up * 1.4f, new Vector3(1.6f, .04f, .04f), Quaternion.LookRotation(sd)); BoxCol(z.root, bt + up * .8f, new Vector3(1.3f, 1.4f, 2.6f), Quaternion.LookRotation(sd));
            Folk(z.At(6f, 3f), -fr, Resident.Pose.Stand, new[] { "Everyone here's a little bit scary. In a nice way.", "Climb the curly hill. The moon's right there.", "When the snow comes, it means somebody found Christmas." }, Local());
            z.area.glowLight = ArtistBits.PointLight(z.root, z.c + up * 6f, new Color(1f, .6f, .3f), 28f, 1.6f);
            Shot(z.ctx, "the halloween town", z.At(Foot(z.a) * 1.1f, 4f) + up * 2.4f, mp - up * 5f);
        }

        // ---------------------------------------------------------------- everyone else
        void Others(Plot z, PlaceFrame f)
        {
            var up = Vector3.up; var rot = z.Rot; var rng = new System.Random(z.a.name.Length * 7 + z.a.name[0]); var c = z.At(1f, -1.2f) + up * .17f; var fr = z.front; var sd = z.side;
            void Post(Vector3 at, float h, float r = .04f) { z.mk.Prism(iron, at - up * .2f, up, r, h + .2f, 5); ArtistBits.PostCollider(z.root, at, h, r + .03f); }
            void Plinth(Vector3 at, float w, float h) { z.mk.Box(stone, at + up * (h / 2f), new Vector3(w, h, w), rot); BoxCol(z.root, at + up * (h / 2f), new Vector3(w, h, w), rot); }
            string Any() => SongIds.TryGetValue(z.a.name, out var l) && l.Length > 0 ? Song(z, l[0].title) : null;
            switch (z.a.name)
            {
                case "Ancient Chinese composer": { var tears = Song(z, "Imperial"); var snow = Song(z, "White Snow"); var pv = c; var red = Paint(new Color(.7f, .14f, .12f)); foreach (var (x, q) in new[] { (-1.4f, -1.4f), (1.4f, -1.4f), (1.4f, 1.4f), (-1.4f, 1.4f) }) { z.mk.Prism(red, pv + sd * x + fr * q, up, .12f, 2.6f, 8); ArtistBits.PostCollider(z.root, pv + sd * x + fr * q, 2.6f, .16f); } for (var k = 0; k < 2; k++) z.mk.Prism(Paint(new Color(.14f, .2f, .2f)), pv + up * (2.6f + k * .7f), up, 2.6f - k * .8f, .5f, 4, .5f, 45f); z.mk.Cone(goldM, pv + up * 3.8f, .1f, .5f, 6); z.mk.Box(woodM(), pv + up * .7f, new Vector3(1.6f, .1f, .5f), rot); z.mk.Box(Paint(new Color(.5f, .34f, .2f)), pv + up * .8f, new Vector3(1.4f, .1f, .36f), rot); for (var k = 0; k < 9; k++) z.mk.Box(GlowM(Pale, .6f), pv + up * .86f + sd * (-.6f + k * .15f), new Vector3(.01f, .01f, .34f), rot); BoxCol(z.root, pv + up * .4f, new Vector3(1.6f, .8f, .5f), rot); z.mk.Prism(SongGlow(tears, new Color(1f, .3f, .2f), 1.4f), pv + up * 2.2f, up, .22f, .4f, 8, .7f); var bt = c + sd * 3.2f; z.mk.Prism(Paint(new Color(.24f, .16f, .14f)), bt, (up + sd * .2f).normalized, .14f, 2.2f, 6); for (var k = 0; k < 7; k++) z.mk.Blob(k == 0 ? SongGlow(snow, new Color(1f, .94f, .96f), 1f) : Paint(new Color(1f, .9f, .94f)), bt + up * 2.2f + Quaternion.Euler(0, k * 51f, 0) * fr * .8f + up * (k % 2) * .4f, new Vector3(.5f, .36f, .5f), rng, .2f); ArtistBits.PostCollider(z.root, bt, 2.2f, .2f); break; }
                case "Ennio Morricone": { var s = Any(); z.mk.Prism(sand, z.c - up * .02f, up, 5.6f, .22f, 24); for (var k = 0; k < 16; k++) { var gp = c + Quaternion.Euler(0, k * 22.5f, 0) * fr * 4f; var gr = Quaternion.LookRotation(gp - c); z.mk.Box(woodM(), gp + up * .6f, new Vector3(.1f, 1.2f, .08f), gr * Quaternion.Euler(0, 0, (k % 3 - 1) * 6f)); z.mk.Box(woodM(), gp + up * .9f, new Vector3(.6f, .08f, .08f), gr); ArtistBits.PostCollider(z.root, gp, 1.2f, .12f); } z.mk.Prism(SongGlow(s, Gold, 1.8f), c + up * .1f, up, 1.2f, .04f, 18); halos.Add(c + up * .8f, 3f, Gold, .4f); life.goldSong = s; break; }
                case "Hollywood Studio Orchestra": { var s = Any(); var tf = new PlaceFrame(c - fr * .6f, fr); foreach (var e in new[] { -1.6f, 1.6f }) { z.mk.Box(stoneD, tf.P(e, 1.6f, 0), new Vector3(.8f, 3.2f, .8f), tf.r); z.solid.Box(stoneD, tf.P(e, 1.6f, 0), new Vector3(.8f, 3.2f, .8f), tf.r); } z.mk.Box(stoneD, tf.P(0, 3.5f, 0), new Vector3(4.2f, .6f, 1f), tf.r); z.mk.Box(dark, tf.P(0, 1.6f, -.3f), new Vector3(2.4f, 3.2f, .1f), tf.r); Plinth(c + fr * .8f, .6f, 1f); z.mk.Blob(SongGlow(s, Gold, 1.6f), c + fr * .8f + up * 1.3f, new Vector3(.18f, .3f, .18f), rng, 0f); z.mk.Blob(stone, c - fr * 1.6f + sd * 3f + up * 1.2f, new Vector3(1.3f, 1.3f, 1.3f), rng, .1f); ArtistBits.PostCollider(z.root, c - fr * 1.6f + sd * 3f, 2.4f, 1.3f); var hat = new MeshKit(); hat.Prism(Paint(new Color(.5f, .38f, .26f)), Vector3.zero, Vector3.up, .32f, .03f, 14); hat.Prism(Paint(new Color(.5f, .38f, .26f)), Vector3.up * .03f, Vector3.up, .17f, .14f, 12, .9f); hat.Prism(dark, Vector3.up * .04f, Vector3.up, .175f, .04f, 12); Thing(hat, "Adventurer's hat", "a battered brown fedora", c + sd * 1.4f + fr * 1.4f + up * .02f, rot, new Vector3(.6f, .2f, .6f), .3f, false); break; }
                case "Jerry Goldsmith": { var s = Any(); life.soarSong = s; foreach (var e in new[] { -2.4f, 2.4f }) { z.mk.Prism(iron, c + sd * e - fr * .8f, (up + fr * .2f).normalized, .06f, 4.4f, 5); z.mk.Prism(iron, c + sd * e + fr * .8f, (up - fr * .2f).normalized, .06f, 4.4f, 5); ArtistBits.PostCollider(z.root, c + sd * e, 4.2f, .2f); } z.mk.Prism(iron, c + up * 4.3f - sd * 2.4f, sd, .05f, 4.8f, 5); var gl = c + up * 3.2f; foreach (var e in new[] { -1f, 1f }) z.mk.Tri(SongGlow(s, new Color(.5f, .8f, 1f), 1f), gl + fr * 1.2f, gl - fr * .6f + sd * e * 2.4f, gl - fr * .4f, gl - up); foreach (var e in new[] { -1f, 1f }) z.mk.Tri(Paint(new Color(.5f, .8f, 1f)), gl + fr * 1.2f, gl - fr * .6f + sd * e * 2.4f, gl - fr * .4f, gl + up); z.mk.Prism(iron, gl, -up, .02f, 1f, 4); z.mk.Prism(iron, gl, up, .02f, 1.1f, 4); break; }
                case "Joe Hisaishi": { var s = Any(); z.mk.Box(waterM, c + up * .02f, new Vector3(1.8f, .02f, 6f), Quaternion.LookRotation(sd)); var red = Paint(new Color(.8f, .2f, .14f)); for (var k = 0; k <= 10; k++) { var a = k / 10f; var bp = c + sd * (a * 3.6f - 1.8f) + up * (Mathf.Sin(a * Mathf.PI) * .7f + .2f); z.mk.Box(woodM(), bp, new Vector3(.4f, .08f, 1.4f), Quaternion.LookRotation(fr) * Quaternion.Euler(0, 0, -Mathf.Cos(a * Mathf.PI) * 20f)); z.solid.Box(woodM(), bp - up * .3f, new Vector3(.42f, .68f, 1.4f), Quaternion.LookRotation(fr) * Quaternion.Euler(0, 0, -Mathf.Cos(a * Mathf.PI) * 20f)); foreach (var e in new[] { -.7f, .7f }) if (k % 2 == 0) z.mk.Prism(red, bp + fr * e, up, .04f, .6f, 5); } foreach (var e in new[] { -.7f, .7f }) z.mk.Prism(red, c + fr * e + up * .8f - sd * 1.8f, sd, .03f, 3.6f, 4); for (var k = 0; k < 2; k++) { var lt = c + sd * (k == 0 ? -2.6f : 2.6f) + fr * 1.4f; Post(lt, 1.6f); z.mk.Prism(SongGlow(k == 0 ? s : null, new Color(1f, .6f, .3f), 1.4f), lt + up * 1.6f, up, .2f, .4f, 8, .8f); } break; }
                case "Mark Governor": { var s = Any(); var mm = c; z.mk.Prism(iron, mm, up, .14f, 7f, 6, .6f); ArtistBits.PostCollider(z.root, mm, 7f, .2f); for (var x = 0; x < 3; x++) for (var y = 0; y < 2; y++) z.mk.Box(lampM, mm + up * (7.2f + y * .5f) + sd * (x - 1) * .5f + fr * .1f, new Vector3(.4f, .4f, .06f), rot); z.mk.Box(iron, mm + up * 7.45f, new Vector3(1.6f, 1.1f, .1f), rot); foreach (var e in new[] { -1f, 1f }) z.mk.Prism(SongGlow(e > 0 ? s : null, e > 0 ? new Color(.3f, 1f, .4f) : new Color(1f, .2f, .8f), 2.4f), mm + up * 6.6f, (fr + sd * e * .5f + up * .5f).normalized, .03f, 14f, 4); break; }
                case "Mark Snow": { var s = Any(); life.xfileSong = s; var sm = new MeshKit(); sm.Blob(Paint(new Color(.6f, .62f, .66f)), Vector3.zero, new Vector3(2.4f, .4f, 2.4f), rng, 0f); sm.Blob(GlowM(new Color(.5f, 1f, .6f), 1f), Vector3.up * .4f, new Vector3(.9f, .5f, .9f), rng, 0f); for (var k = 0; k < 8; k++) sm.Blob(GlowM(new Color(.6f, 1f, .6f), 2f), Quaternion.Euler(0, k * 45f, 0) * Vector3.forward * 2.2f - Vector3.up * .1f, Vector3.one * .12f, rng, 0f); var ufo = sm.BuildSingle("Flying saucer", z.root, false); ufo.transform.position = c + up * 6f; life.saucer = ufo.transform; life.saucerHome = c + up * 6f; z.mk.Cone(SongGlow(s, new Color(.5f, 1f, .6f), .5f), c + up * .05f, 1.6f, .02f, 16); z.mk.Prism(GlowM(new Color(.5f, 1f, .6f), .3f), c + up * .1f, up, .9f, 5.4f, 12, .5f, 0f, false); var tor = c + sd * 2.6f; z.mk.Box(iron, tor + up * .6f, new Vector3(.3f, 1.2f, .3f), rot); z.mk.Box(Paint(new Color(.3f, .4f, .5f)), tor + up * 1.2f, new Vector3(.5f, .3f, .7f), rot); BoxCol(z.root, tor + up * .7f, new Vector3(.5f, 1.4f, .7f), rot); break; }
                case "Michael Giacchino": { var s = Any(); var sm = c - fr * .6f; z.mk.Cone(white, sm, 2.6f, 4.8f, 12); for (var k = 0; k < 6; k++) z.mk.Cone(white, sm + Quaternion.Euler(0, k * 60f, 0) * fr * 2.2f, .3f, 2.4f + (k % 2) * 1f, 6); for (var k = 0; k < 12; k++) z.mk.Blob(k == 0 ? SongGlow(s, new Color(.5f, .6f, 1f), 1.6f) : starM, sm + Quaternion.Euler(0, k * 30f, 0) * fr * 2.2f + up * (.6f + (k % 3) * .7f) + Quaternion.Euler(0, k * 30f, 0) * fr * (-.6f - (k % 3) * .5f), Vector3.one * .07f, rng, 0f); ArtistBits.PostCollider(z.root, sm, 4f, 2.6f); var rk = c + sd * 2.6f + fr * 1.2f; z.mk.Prism(white, rk, up, .2f, 1.2f, 8); z.mk.Cone(Paint(new Color(.8f, .2f, .2f)), rk + up * 1.2f, .2f, .5f, 8); ArtistBits.PostCollider(z.root, rk, 1.6f, .3f); break; }
                case "Pyotr Ilyich Tchaikovsky": { var s = Any(); life.swanSong = s; var tt = c; Post(tt, 1.1f, .03f); z.mk.Blob(white, tt + up * 1.2f, new Vector3(.14f, .2f, .1f), rng, 0f); z.mk.Prism(SongGlow(s, new Color(.9f, .92f, 1f), 1f), tt + up * .95f, up, .6f, .06f, 18); ArtistBits.PostCollider(z.root, tt, 1.3f, .6f); var bar = c + sd * 2.6f; foreach (var e in new[] { -1f, 1f }) Post(bar + fr * e, 1f, .03f); z.mk.Prism(woodM(), bar - fr * 1f + up * 1f, fr, .03f, 2f, 5); var sw = c - fr * 1.6f - sd * 1.6f; z.mk.Blob(white, sw + up * .4f, new Vector3(.4f, .3f, .7f), rng, .05f); z.mk.Prism(white, sw + up * .5f + fr * .4f, (up + fr * .3f).normalized, .07f, .8f, 6); z.mk.Blob(white, sw + up * 1.3f + fr * .6f, new Vector3(.1f, .1f, .16f), rng, 0f); ArtistBits.PostCollider(z.root, sw, 1.2f, .6f); break; }
                case "Richard Wagner": { var s = Any(); life.valkSong = s; for (var k = 0; k < 3; k++) { var sp = c + sd * (k - 1) * 1.6f - fr * (k % 2) * .6f; var h = 3.4f + (k % 2) * .6f; z.mk.Prism(woodM(), sp, up, .04f, h, 5); ArtistBits.PostCollider(z.root, sp, h, .08f); z.mk.Cone(Paint(Pale), sp + up * h, .06f, .4f, 4); var hm = sp + up * (h - .5f); z.mk.Blob(k == 1 ? SongGlow(s, Pale, 1f) : Paint(new Color(.7f, .7f, .74f)), hm, new Vector3(.26f, .2f, .26f), rng, 0f); foreach (var e in new[] { -1f, 1f }) z.mk.Tri(white, hm + sd * e * .22f, hm + sd * e * .7f + up * .5f, hm + sd * e * .3f + up * .3f - fr * .2f, hm - fr); } break; }
                case "Sergei Rachmaninoff": { var s = Any(); Piano(z.ctx, z.mk, c, fr); z.mk.Blob(SongGlow(s, Gold, 1.6f), c + up * 1.8f - fr * .6f, Vector3.one * .1f, rng, 0f); Post(c + sd * 2.6f, 2.6f, .04f); z.mk.Blob(lampM, c + sd * 2.6f + up * 2.7f, Vector3.one * .2f, rng, 0f); break; }
                case "The Lorax Singers": { var s = Any(); var cols = new[] { new Color(1f, .5f, .7f), new Color(1f, .8f, .3f), new Color(1f, .6f, .3f), new Color(.7f, .5f, 1f) }; for (var k = 0; k < 4; k++) { var tp = c + Quaternion.Euler(0, k * 90f + 30f, 0) * fr * 2.4f; var h = 3f + (k % 2) * 1f; for (var j = 0; j < 6; j++) z.mk.Prism(j % 2 == 0 ? Paint(new Color(.9f, .86f, .5f)) : Paint(new Color(.2f, .16f, .14f)), tp + up * (j * h / 6f) + sd * Mathf.Sin(j * .6f) * .15f, up, .1f, h / 6f, 6); z.mk.Blob(GlowM(cols[k], .8f), tp + up * (h + .4f), new Vector3(.8f, .6f, .8f), rng, .3f); ArtistBits.PostCollider(z.root, tp, h, .2f); } z.mk.Prism(Paint(new Color(.6f, .36f, .26f)), c, up, .3f, .4f, 8, 1.2f); z.mk.Blob(SongGlow(s, new Color(.5f, 1f, .4f), 1.8f), c + up * .5f, Vector3.one * .12f, rng, 0f); ArtistBits.PostCollider(z.root, c, .5f, .4f); life.growSong = s; break; }
                case "The Citizens of Halloween": { var s = Any(); life.halloweenSong = s; z.mk.Prism(Paint(new Color(.24f, .18f, .14f)), z.c - up * .02f, up, 5.6f, .22f, 24); for (var k = 0; k < 18; k++) { var pp = c + Quaternion.Euler(0, k * 137f, 0) * fr * (1f + (k % 5) * .8f); z.mk.Blob(k % 4 == 0 ? SongGlow(k == 0 ? s : null, Ember, 1.6f) : Paint(new Color(.9f, .45f, .1f)), pp + up * .3f, new Vector3(.4f, .3f, .4f), rng, .1f); z.mk.Prism(Paint(new Color(.2f, .4f, .2f)), pp + up * .58f, up, .04f, .16f, 4); } var sc = c - fr * 2.4f; z.mk.Prism(woodM(), sc, up, .06f, 2.4f, 5); z.mk.Prism(woodM(), sc + up * 1.7f - sd * .9f, sd, .05f, 1.8f, 5); z.mk.Box(Paint(new Color(.4f, .3f, .2f)), sc + up * 1.5f, new Vector3(.6f, .8f, .3f), rot); z.mk.Blob(GlowM(Ember, 1.4f), sc + up * 2.2f, new Vector3(.3f, .26f, .3f), rng, .1f); z.mk.Cone(Paint(new Color(.2f, .16f, .14f)), sc + up * 2.4f, .4f, .6f, 6); ArtistBits.PostCollider(z.root, sc, 2.4f, .3f); ArtistBits.PostCollider(z.root, c, .8f, 4f); break; }
                case "Catherine O'Hara": { var s = Any(); var hl = c - fr * .4f; var hm = Paint(new Color(.22f, .2f, .26f)); for (var k = 0; k < 14; k++) { var a = k / 13f; z.mk.Box(hm, hl + up * (Mathf.Sin(a * Mathf.PI) * 2.2f + a * a * 1.4f) + sd * (a * 3f - 1.5f), new Vector3(.5f, .2f, .8f), Quaternion.LookRotation(fr) * Quaternion.Euler(0, 0, -Mathf.Cos(a * Mathf.PI) * 50f - a * 40f)); } BoxCol(z.root, hl + up * 1f, new Vector3(3f, 2f, .8f), rot); z.mk.Prism(SongGlow(s, new Color(.96f, .94f, .8f), 1.4f), hl - fr * 1f + up * 3.4f, fr, .9f, .1f, 22); Post(hl - fr * 1.1f, 2.6f, .04f); for (var k = 0; k < 5; k++) z.mk.Box(Paint(k % 2 == 0 ? new Color(.5f, .3f, .5f) : new Color(.3f, .4f, .6f)), c + sd * 2f + up * (.3f + k * .25f), new Vector3(.5f, .24f, .06f), rot * Quaternion.Euler(0, 0, k * 7f)); break; }
                default: { var s = Any(); Post(c, 1.4f); z.mk.Blob(SongGlow(s, Gold, 1.4f), c + up * 1.5f, Vector3.one * .2f, rng, 0f); break; }
            }
        }

        Material woodM() => Paint(new Color(.42f, .3f, .22f));
    }
}
