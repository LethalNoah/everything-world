using System.Collections.Generic;
using UnityEngine;

namespace EverythingWorld.World
{
    /// The Djent Spires' biggest artists. Every artist stands on a hexagonal terrace of white stone edged in gold,
    /// covers on a slate board framed like a fretboard, a plaque and a crystal lantern; every song has its own light,
    /// and songs without a piece of their own get a glowing crystal shard (SongShards).
    /// - Plini: a crystal arch with a great gold sun rising behind it, rays fanning out ("Electric Sunrise"); a set of
    ///   crystal pan pipes ("Pan"); a tall figure of blue glass with wings ("Blue Angel").
    /// - Leprous: a hexagonal shaft stepping down into a glowing well ("Below"); a red crystal obelisk ("The Sky Is
    ///   Red": the whole sky turns red); a pair of white wings cast up on a rock in a dark pool ("Castaway Angels").
    /// - Polyphia: a beam of white light striking a great prism and fanning out into a spectrum ("Light").
    /// - Animals As Leaders: an eight-string guitar neck rising out of the rock like a monolith, gold frets, glowing
    ///   strings, eight tuning pegs ("Odessa").
    /// - Intervals: a molecule floating and turning over a gold dais ("D.O.S.E.").
    /// - Sleep Token: a ring of dark standing stones round a sunken circle with a glowing sigil and candles, a dark
    ///   place in the bright land ("The Summoning": the land dims and the sigil blazes).
    public partial class DjentKit
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
                Add("Leprous", "The Sky Is Red", "@the arrival and the citadel"); Add("Plini", "Electric Sunrise", "Plini");
                Add("Sleep Token", "The Summoning", "Sleep Token"); Add("Polyphia", "Light", "@the citadel"); Add("Animals As Leaders", "Odessa", "@the arrival and the citadel");
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
            if (stone == null) MakeMaterials();
            var root = new GameObject("Artist: " + a.name).transform; root.SetParent(ctx.Root, false);
            var front = facing.TryGetValue(a.name, out var f) ? f : new Vector3(center.x - a.x, 0, center.z - a.z).normalized;
            if (front.sqrMagnitude < .01f) front = Vector3.forward;
            var labels = new GameObject("Labels").transform; labels.SetParent(root, false);
            var area = root.gameObject.AddComponent<ArtistArea>();
            area.artistName = a.name; area.stationId = a.stationId; area.radius = a.r; area.glowColor = Ice;
            var z = new Plot { ctx = ctx, root = root, labels = labels, mk = new MeshKit(), solid = new MeshKit(), pictures = new MeshKit(), a = a, c = ctx.OnGround(a.x, a.z), front = front, side = Vector3.Cross(Vector3.up, front), area = area };
            var reach = Foot(a); var yaw = Quaternion.LookRotation(front).eulerAngles.y;
            // the terrace: a white hexagon edged in gold
            z.mk.Prism(stoneLight, z.c - Vector3.up * .3f, Vector3.up, reach * .62f, .38f, 6, 1f, yaw + 30f);
            {
                var rr = reach * .62f - .25f;
                for (var k = 0; k < 6; k++)
                {
                    float a0 = (k * 60f + yaw + 30f) * Mathf.Deg2Rad, a1 = ((k + 1) * 60f + yaw + 30f) * Mathf.Deg2Rad;
                    var p0 = z.c + new Vector3(Mathf.Sin(a0), 0, Mathf.Cos(a0)) * rr + Vector3.up * .1f; var p1 = z.c + new Vector3(Mathf.Sin(a1), 0, Mathf.Cos(a1)) * rr + Vector3.up * .1f;
                    z.mk.Box(HasStation(a) ? goldGlow : fretM, (p0 + p1) / 2f, new Vector3(.2f, .05f, Vector3.Distance(p0, p1)), Quaternion.LookRotation(p1 - p0));
                }
            }
            switch (a.name)
            {
                case "Plini": Plini(z); break;
                case "Leprous": Leprous(z); break;
                case "Polyphia": Polyphia(z); break;
                case "Animals As Leaders": Neck(z); break;
                case "Intervals": Molecule(z); break;
                case "Sleep Token": SleepToken(z); break;
                default: Small(z); break;
            }
            SongShards(z);
            Board(z, z.At(reach * .78f, -reach * .5f - 2.5f), front);
            ArtistBits.Plaque(z.mk, z.solid, labels, a, z.At(reach * .78f + 1.5f, reach * .5f + 2.5f), front, slate, stoneLight, Ink, 1.6f);
            Lantern(ctx, z.mk, z.At(reach * .78f, reach * .5f + 5f));
            if (z.area.glowLight == null) z.area.glowLight = ArtistBits.PointLight(root, z.c + Vector3.up * 6f + front * 4f, new Color(.85f, .93f, 1f), HasStation(a) ? 24f : 17f, 2f);
            area.glowRenderers = new Renderer[0];
            z.mk.Build("Area", root, false);
            z.solid.BuildCollider("Walls", root);
            z.pictures.Build("Covers", root, false);
            noGrass.Add(new Vector3(z.c.x, reach * .9f, z.c.z));
            if (HasStation(a) || reach >= 11f)
            {
                var mark = new GameObject("Shot: " + a.name + " up close").transform; mark.SetParent(ctx.Root, false);
                var cam = z.At(reach * .6f, reach * .35f) + Vector3.up * 1.7f; var look = z.At(-reach * .35f, 0) + Vector3.up * 3f - cam; mark.SetPositionAndRotation(cam, Quaternion.LookRotation(look.normalized));
            }
        }

        /// The covers on a slate board framed like a fretboard: gold frets up the sides, pearl edges.
        void Board(Plot z, Vector3 ground, Vector3 dir)
        {
            dir = Vector3.ProjectOnPlane(dir, Vector3.up).normalized; var rot = Quaternion.LookRotation(dir); var side = Vector3.Cross(Vector3.up, dir);
            var n = Mathf.Max(1, Mathf.Min(6, z.a.songList?.Length ?? 1)); const float size = .9f; var w = n * size * 1.25f + .6f;
            var c = ground + Vector3.up * 1.7f;
            z.mk.Box(slate, c - Vector3.up * .5f, new Vector3(w, size + 1.6f, .3f), rot); z.solid.Box(slate, c - Vector3.up * .5f, new Vector3(w, size + 1.6f, .4f), rot);
            foreach (var y in new[] { -1.3f, size / 2f + .32f }) z.mk.Box(pearlM, c + Vector3.up * y + dir * .16f, new Vector3(w, .05f, .02f), rot);
            foreach (var s in new[] { -1f, 1f }) for (var k = 0; k < 4; k++) z.mk.Box(fretM, c + side * s * (w / 2f - .12f) + dir * .16f - Vector3.up * (1.2f - k * .55f), new Vector3(.18f, .05f, .02f), rot);
            ArtistBits.CoverWall(z.pictures, z.labels, z.ctx, z.a, c + dir * .16f + Vector3.up * .08f, dir, n, size, n, true, Pearl);
        }

        /// A crystal shard for every song without a piece of its own, glowing while it plays.
        void SongShards(Plot z)
        {
            if (!SongIds.TryGetValue(z.a.name, out var list)) return;
            var up = Vector3.up; var k = 0; var seen = new HashSet<string>();
            Color[] cols = { Ice, Gold, Cyan };
            foreach (var (_, id) in list)
            {
                if (z.lit.Contains(id) || !seen.Add(id)) continue;
                var p = z.At(-Foot(z.a) * .25f - (k % 2) * 1.6f, Foot(z.a) * .45f + (k / 2) * 1.8f);
                var col = cols[(k + z.a.name.Length) % 3];
                z.mk.Prism(stoneLight, p - up * .2f, up, .4f, .7f, 6);
                Crystal(z.mk, SongGlow(id, col, 2f), p + up * .45f, up, .2f, 1.4f, 15f);
                halos.Add(p + up * 1.3f, 1f, col, .45f); ArtistBits.PostCollider(z.root, p, 1.5f, .42f);
                k++;
            }
        }

        // ---------------------------------------------------------------- Plini
        void Plini(Plot z)
        {
            var up = Vector3.up; var rot = z.Rot; var ctx = z.ctx;
            // "Electric Sunrise": a crystal arch, a great gold sun rising behind it, rays fanning out
            var arch = z.At(-8f, 0);
            foreach (var s in new[] { -1f, 1f }) { Crystal(z.mk, crystal, arch + z.side * s * 4.2f - up * .4f, (up - z.side * s * .08f).normalized, .55f, 7.6f); ArtistBits.PostCollider(z.root, arch + z.side * s * 4.2f, 6f, .6f); }
            for (var k = 0; k <= 12; k++) { var a = k * Mathf.PI / 12f; var p = arch + up * 6.6f + (z.side * Mathf.Cos(a) * 4.2f + up * Mathf.Sin(a) * 2.2f); z.mk.Box(crystal, p, new Vector3(.7f, .7f, .7f), rot * Quaternion.Euler(0, 0, k * 15f)); }
            var sunM = SongGlow(z, "Electric Sunrise", new Color(1f, .7f, .2f), 1.2f);
            var sun = arch - z.front * 3f + up * 5.5f;
            z.mk.Prism(sunM, sun - z.front * .2f, z.front, 4.2f, .4f, 32);
            for (var k = 0; k < 16; k++)
            {
                var a = k * Mathf.PI / 8f; var d = z.side * Mathf.Cos(a) + up * Mathf.Sin(a);
                if (Mathf.Sin(a) < -.35f) continue;
                z.mk.Box(sunM, sun + d * 6.6f - z.front * .1f, new Vector3(.25f, .25f, 3.6f), Quaternion.LookRotation(d, z.front));
            }
            z.mk.Prism(slate, sun - z.front * .5f - up * 5.6f, up, .45f, 5.3f, 6); z.solid.Prism(slate, sun - z.front * .5f - up * 5.6f, up, .45f, 5.3f, 6);
            halos.Add(sun + z.front * 1.5f, 7f, Gold, .5f);
            ArtistBits.PointLight(z.root, sun + z.front * 4f, Gold, 26f, 2.4f);
            life.pliniSun = sun;
            // "Pan": crystal pan pipes, eight tubes graduated in length, on a gold stand
            var pan = z.At(2f, -8.5f); var panRot = Quaternion.LookRotation(z.side);
            var panM = SongGlow(z, "Pan", Ice, 1.8f);
            z.mk.Box(goldM, pan + up * .9f, new Vector3(.3f, 1.8f, .3f), panRot);
            for (var k = 0; k < 8; k++) { var len = 2.6f - k * .24f; var p = pan + up * (1.9f + len / 2f) + z.front * ((k - 3.5f) * .42f); z.mk.Prism(panM, p - up * len / 2f, up, .17f, len, 8); }
            z.mk.Box(goldM, pan + up * 2.15f, new Vector3(.4f, .2f, 3.6f), panRot);
            z.solid.Box(goldM, pan + up * 1.5f, new Vector3(.6f, 3f, 3.6f), panRot);
            halos.Add(pan + up * 3f, 2.6f, Ice, .5f);
            // "Blue Angel": a tall figure of blue glass with great wings
            var ang = z.At(2f, 8.5f); var blue = SongGlow(z, "Blue Angel", new Color(.35f, .6f, 1f), 1.8f);
            z.mk.Prism(stoneLight, ang - up * .2f, up, 1.1f, .8f, 6);
            z.mk.Prism(blue, ang + up * .6f, up, .45f, 3.6f, 6, .55f); z.mk.Blob(blue, ang + up * 4.6f, Vector3.one * .36f, ctx.Rng, 0f);
            foreach (var s in new[] { -1f, 1f })
                for (var k = 0; k < 4; k++)
                {
                    var len = 3.4f - k * .5f; var wr = rot * Quaternion.Euler(-10f, 0, s * (20f + k * 16f));
                    z.mk.Box(blue, ang + up * 3.6f + z.side * s * .4f + wr * Vector3.right * s * len * .5f - z.front * .25f, new Vector3(len, .55f, .05f), wr);
                }
            ArtistBits.PostCollider(z.root, ang, 4f, 1f);
            halos.Add(ang + up * 3.5f, 3f, new Color(.4f, .65f, 1f), .5f);
        }

        // ---------------------------------------------------------------- Leprous
        void Leprous(Plot z)
        {
            var up = Vector3.up; var ctx = z.ctx;
            // "Below": a hexagonal shaft stepping down into a glowing well, a railing round its mouth
            var w = z.At(-6f, -4f); var below = SongGlow(z, "Below", Cyan, 2.2f);
            for (var k = 0; k < 5; k++) z.mk.Prism(k % 2 == 0 ? stone : stoneLight, w - up * (k * .7f + .5f), up, 4.4f - k * .7f, .5f, 6, 1f, 30f);
            z.mk.Prism(below, w - up * 3.9f, up, 1.2f, .1f, 6, 1f, 30f);
            halos.Add(w - up * 2.4f, 3f, Cyan, .6f); ArtistBits.PointLight(z.root, w - up * 2f, Cyan, 10f, 2.6f);
            for (var k = 0; k < 6; k++)
            {
                if (k == 0) continue;   // open toward the front
                float a0 = (k * 60f) * Mathf.Deg2Rad, a1 = ((k + 1) * 60f) * Mathf.Deg2Rad; var r = 4.7f;
                var fw = Quaternion.LookRotation(z.front);
                var p0 = w + fw * new Vector3(Mathf.Sin(a0), 0, Mathf.Cos(a0)) * r; var p1 = w + fw * new Vector3(Mathf.Sin(a1), 0, Mathf.Cos(a1)) * r;
                z.mk.Box(goldM, (p0 + p1) / 2f + up * 1f, new Vector3(.08f, .08f, Vector3.Distance(p0, p1)), Quaternion.LookRotation(p1 - p0));
                z.mk.Prism(goldM, p0 - up * .2f, up, .06f, 1.2f, 6);
                z.solid.Box(goldM, (p0 + p1) / 2f + up * .6f, new Vector3(.15f, 1.2f, Vector3.Distance(p0, p1)), Quaternion.LookRotation(p1 - p0));
            }
            // "The Sky Is Red": a red crystal obelisk, the tallest thing on the terrace
            var ob = z.At(-9f, 7f); var red = SongGlow(z, "The Sky Is Red", new Color(1f, .22f, .2f), 1.8f);
            z.mk.Prism(slate, ob - up * .3f, up, 1.4f, 1f, 6);
            Crystal(z.mk, red, ob + up * .6f, up, .8f, 10f, 20f);
            ArtistBits.PostCollider(z.root, ob, 6f, 1.3f); halos.Add(ob + up * 8f, 3f, new Color(1f, .3f, .25f), .5f);
            // "Castaway Angels": a dark pool with a rock in it, a pair of white wings cast up on the rock
            var pool = z.At(1f, 9f); var angels = SongGlow(z, "Castaway Angels", Pearl, 1.4f);
            z.mk.Prism(slate, pool - up * .25f, up, 3.4f, .35f, 6, 1f, 10f);
            z.mk.Prism(WorldMaterials.Solid(new Color(.1f, .14f, .22f), .95f), pool + up * .08f, up, 3f, .04f, 24);
            z.mk.Blob(stone, pool + up * .35f, new Vector3(1.2f, .6f, 1f), ctx.Rng, .3f);
            foreach (var s in new[] { -1f, 1f })
                for (var k = 0; k < 4; k++)
                {
                    var wr = Quaternion.LookRotation(z.front) * Quaternion.Euler(70f, s * (25f + k * 14f), 0);
                    z.mk.Box(angels, pool + up * .9f + z.side * s * (.4f + k * .35f), new Vector3(.4f, .04f, 1.8f - k * .3f), wr);
                }
            z.solid.Prism(slate, pool - up * .25f, up, 3.4f, .9f, 6);
            halos.Add(pool + up * 1f, 2.4f, Pearl, .4f);
            ArtistBits.PointLight(z.root, z.c + up * 7f, new Color(1f, .55f, .5f), 18f, 1.2f);
        }

        // ---------------------------------------------------------------- Polyphia
        /// "Light": a lamp on a pylon throws a white beam onto a great prism, and out of the far side the light fans
        /// into a spectrum laid across the terrace.
        void Polyphia(Plot z)
        {
            var up = Vector3.up; var ctx = z.ctx; var light = Song(z, "Light");
            var pr = z.At(-3f, 0); var prism = SongGlow(light, Pearl, 1.2f);
            z.mk.Prism(goldM, pr - up * .2f, up, 1.8f, .7f, 6);
            var prRot = Quaternion.LookRotation(z.front);
            // a triangular prism standing on end
            z.mk.Prism(prism, pr + up * .5f, up, 1.5f, 3.6f, 3, 1f, Quaternion.LookRotation(z.front).eulerAngles.y);
            z.solid.Prism(goldM, pr - up * .2f, up, 1.8f, 4.3f, 6);
            var lamp = z.At(-3f, -8.5f) + up * 2.5f;
            z.mk.Prism(slate, z.At(-3f, -8.5f) - up * .2f, up, .3f, 2.4f, 6); ArtistBits.PostCollider(z.root, z.At(-3f, -8.5f), 2.4f, .35f);
            z.mk.Box(goldM, lamp, new Vector3(.8f, .7f, .7f), Quaternion.LookRotation(z.side));
            var beam = SongGlow(light, Pearl, 2.4f);
            var b0 = lamp + z.side * .4f; var b1 = pr + up * 2.5f - z.side * .9f;
            z.mk.Box(beam, (b0 + b1) / 2f, new Vector3(.18f, .18f, Vector3.Distance(b0, b1)), Quaternion.LookRotation(b1 - b0));
            Color[] spec = { new Color(1f, .25f, .25f), new Color(1f, .6f, .2f), new Color(1f, .92f, .3f), new Color(.35f, 1f, .45f), new Color(.3f, .8f, 1f), new Color(.35f, .4f, 1f), new Color(.75f, .35f, 1f) };
            var o = pr + up * 2.5f + z.side * .9f;
            for (var k = 0; k < spec.Length; k++)
            {
                var d = Quaternion.AngleAxis(-10f + k * 4.5f, up) * Quaternion.AngleAxis(12f + k * 1.5f, z.front) * z.side;
                var end = o + d * 9f; var g = ctx.Ground(end.x, end.z); if (end.y < g + .3f) end.y = g + .3f;
                var m = SongGlow(light, spec[k], 2.2f);
                z.mk.Box(m, (o + end) / 2f, new Vector3(.28f, .06f, Vector3.Distance(o, end)), Quaternion.LookRotation(end - o));
                halos.Add(end + up * .4f, 1.4f, spec[k], .45f);
            }
            halos.Add(pr + up * 2.5f, 3.4f, Pearl, .55f);
            ArtistBits.PointLight(z.root, pr + up * 4f + z.front * 2f, Pearl, 16f, 2.2f);
            life.prismAt = pr + up * 2.5f;
        }

        // ---------------------------------------------------------------- Animals As Leaders
        /// "Odessa": an eight-string guitar neck rising out of the rock at a slant like a monolith: dark wood, gold
        /// frets narrowing toward the body, pearl inlays, eight glowing strings and a headstock with eight pegs.
        void Neck(Plot z)
        {
            var up = Vector3.up; var odessa = Song(z, "Odessa");
            var foot = z.At(-6f, 0); var dir = (up * .9f + z.front * .35f).normalized; const float L = 16f, W = 1.9f;
            var rot = Quaternion.LookRotation(dir, z.front); var face = rot * Vector3.up;   // the fretboard faces forward and up
            var wood = WorldMaterials.Solid(new Color(.18f, .12f, .1f), .4f);
            z.mk.Box(wood, foot + dir * (L / 2f - 1f) - face * .25f, new Vector3(W, .5f, L), rot);
            z.mk.Box(slate, foot + dir * (L / 2f - 1f) - face * .62f, new Vector3(W + .2f, .3f, L), rot);
            // frets: spaced like a real neck (closer together up toward the body, which is in the ground here)
            for (var k = 1; k <= 18; k++) { var t = L - 1f - (L - 2f) * (1f - Mathf.Pow(.5f, k / 12f)) * 1.6f; if (t < .5f) break; z.mk.Box(fretM, foot + dir * (t - 1f) + face * .02f, new Vector3(W, .06f, .08f), rot); if (k == 3 || k == 5 || k == 7 || k == 9 || k == 15 || k == 17) z.mk.Prism(pearlM, foot + dir * (t - 1.35f) + face * .01f, face, .14f, .03f, 10); if (k == 12) foreach (var s in new[] { -.45f, .45f }) z.mk.Prism(pearlM, foot + dir * (t - 1.35f) + face * .01f + z.side * s, face, .14f, .03f, 10); }
            var strings = SongGlow(odessa, Pearl, 2f);
            for (var k = 0; k < 8; k++) { var x = (k - 3.5f) * .21f; z.mk.Box(strings, foot + dir * (L / 2f - .5f) + face * .12f + z.side * x, new Vector3(.03f + k * .006f, .03f + k * .006f, L + 1f), rot); }
            // the headstock, angled back, eight pegs in a row
            var head = foot + dir * (L - .5f); var hd = (dir - face * .3f).normalized; var hrot = Quaternion.LookRotation(hd, z.front);
            z.mk.Box(wood, head + hd * 1.6f - face * .25f, new Vector3(W + .5f, .45f, 3.4f), hrot);
            for (var k = 0; k < 8; k++) { var s = k < 4 ? -1f : 1f; var p = head + hd * (.6f + (k % 4) * .75f) + z.side * s * (W / 2f + .35f); z.mk.Prism(metal, p, z.side * s, .12f, .5f, 8); z.mk.Prism(goldM, p + z.side * s * .5f, z.side * s, .25f, .1f, 10); }
            z.solid.Box(wood, foot + dir * (L / 2f - 1f) - face * .3f, new Vector3(W + .2f, 1f, L), rot);
            halos.Add(foot + dir * (L * .55f) + face * 1f, 4f, Pearl, .35f);
            ArtistBits.PointLight(z.root, foot + dir * 6f + face * 3f, new Color(.9f, .95f, 1f), 18f, 2f);
            life.neckAt = foot + dir * 8f;
        }

        // ---------------------------------------------------------------- Intervals
        /// "D.O.S.E.": a molecule floating and turning over a gold dais: a ring of six atoms with bonds between, side
        /// chains branching off, all glowing.
        void Molecule(Plot z)
        {
            var up = Vector3.up; var ctx = z.ctx; var id = Song(z, "D.O.S.E");
            var c = z.At(-4f, 0);
            z.mk.Prism(goldM, c - up * .2f, up, 2.4f, .6f, 6); z.solid.Prism(goldM, c - up * .2f, up, 2.4f, .6f, 6);
            z.mk.Prism(SongGlow(id, Gold, 1.6f), c + up * .42f, up, 1.8f, .04f, 24);
            var mk = new MeshKit(); var atomA = SongGlow(id, Ice, 2f); var atomB = SongGlow(id, Gold, 2f); var bond = SongGlow(id, Pearl, 1.2f);
            var ring = new Vector3[6];
            for (var k = 0; k < 6; k++) { var a = k * Mathf.PI / 3f; ring[k] = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0) * 1.6f; mk.Blob(k % 3 == 0 ? atomB : atomA, ring[k], Vector3.one * .38f, ctx.Rng, 0f); }
            void Bond(Vector3 a, Vector3 b) => mk.Box(bond, (a + b) / 2f, new Vector3(.1f, .1f, Vector3.Distance(a, b)), Quaternion.LookRotation(b - a));
            for (var k = 0; k < 6; k++) Bond(ring[k], ring[(k + 1) % 6]);
            foreach (var k in new[] { 0, 2, 4 }) { var o = ring[k] * 1.9f; mk.Blob(atomA, o, Vector3.one * .3f, ctx.Rng, 0f); Bond(ring[k], o); var o2 = o + new Vector3(0, 0, 1.1f); mk.Blob(atomB, o2, Vector3.one * .24f, ctx.Rng, 0f); Bond(o, o2); }
            var go = mk.BuildSingle("D.O.S.E. molecule", life.transform, false);
            if (go != null) { go.transform.position = c + up * 4.2f; life.molecule = go.transform; life.moleculeAt = c + up * 4.2f; life.moleculeSong = id; }
            halos.Add(c + up * 4.2f, 4f, Ice, .5f);
            ArtistBits.PointLight(z.root, c + up * 4f + z.front * 3f, new Color(.9f, .95f, 1f), 16f, 2f);
        }

        // ---------------------------------------------------------------- Sleep Token
        /// "The Summoning": a dark place in the bright land. A ring of eight black standing stones round a sunken
        /// circle, a sigil glowing in its floor (a star of lines in a circle), candles at each stone; when the song
        /// plays the land dims and the sigil blazes (DjentLife).
        void SleepToken(Plot z)
        {
            var up = Vector3.up; var ctx = z.ctx; var id = Song(z, "The Summoning");
            var c = z.At(-3f, 0); var black = WorldMaterials.Solid(new Color(.05f, .05f, .06f), .6f);
            z.mk.Prism(black, c - up * .3f, up, 7f, .45f, 32);
            var sigil = SongGlow(id, new Color(1f, .78f, .5f), 1.4f); life.sigil = sigil; life.sigilAt = c; c += up * .17f;
            for (var k = 0; k < 40; k++) { float a0 = k * Mathf.PI / 20f, a1 = (k + 1) * Mathf.PI / 20f; var p0 = c + new Vector3(Mathf.Cos(a0), 0, Mathf.Sin(a0)) * 5.2f + up * .0f; var p1 = c + new Vector3(Mathf.Cos(a1), 0, Mathf.Sin(a1)) * 5.2f; z.mk.Box(sigil, (p0 + p1) / 2f, new Vector3(.12f, .04f, Vector3.Distance(p0, p1) + .02f), Quaternion.LookRotation(p1 - p0)); }
            var pts = new Vector3[7]; for (var k = 0; k < 7; k++) { var a = k * Mathf.PI * 2f / 7f; pts[k] = c + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * 5.1f; }
            for (var k = 0; k < 7; k++) { var p0 = pts[k]; var p1 = pts[(k + 3) % 7]; z.mk.Box(sigil, (p0 + p1) / 2f + up * .01f, new Vector3(.1f, .04f, Vector3.Distance(p0, p1)), Quaternion.LookRotation(p1 - p0)); }
            for (var k = 0; k < 8; k++)
            {
                var a = k * Mathf.PI / 4f + Mathf.PI / 8f; var d = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)); var p = ctx.OnGround(c.x + d.x * 8.3f, c.z + d.z * 8.3f);
                if (Vector3.Dot(d, z.front) > .85f) continue;   // the way in
                var h = ctx.Range(3.6f, 5.2f); var rot = Quaternion.LookRotation(d) * Quaternion.Euler(ctx.Range(-4f, 4f), 0, ctx.Range(-4f, 4f));
                z.mk.Box(black, p + up * (h / 2f - .3f), new Vector3(1.3f, h, .7f), rot); z.solid.Box(black, p + up * (h / 2f - .3f), new Vector3(1.3f, h, .7f), rot);
                z.mk.Box(sigil, p + up * (h * .6f) - d * .36f, new Vector3(.08f, h * .35f, .02f), rot);
                var cd = p - d * 1.1f; z.mk.Prism(WorldMaterials.Solid(new Color(.92f, .88f, .8f)), cd, up, .1f, .4f, 8); z.mk.Blob(GlowM(new Color(1f, .75f, .4f), 2.6f), cd + up * .5f, new Vector3(.06f, .1f, .06f), ctx.Rng, 0f);
                halos.Add(cd + up * .55f, .7f, new Color(1f, .75f, .4f), .6f);
            }
            halos.Add(c + up * .5f, 5f, new Color(1f, .7f, .45f), .25f);
            ArtistBits.PointLight(z.root, c + up * 4f, new Color(1f, .7f, .45f), 14f, 1.6f);
        }
    }
}
