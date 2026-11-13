using System.Collections.Generic;
using UnityEngine;
using static EverythingWorld.World.PlaceTools;

namespace EverythingWorld.World
{
    /// The Velvet Rooftops' artists. Each has a roof terrace: a deck with a low parapet round three sides, a velvet
    /// sofa, their name in neon, and one thing from their song; covers in a lit glass case, a plaque, a gold lamp.
    /// - Rihanna: a great black umbrella with rain falling all round its edge and none under it ("Umbrella": it rains
    ///   on the whole city); a diamond as tall as you turning the light ("Diamonds": diamonds fall from the sky); a
    ///   signal lamp on a mast ("SOS": every red light in the city blinks it).
    /// - Frank Ocean: a white sports car parked on the roof with its lamps on ("White Ferrari"); a cabana in pink and
    ///   white stripes with clouds painted inside ("Pink + White": the sky turns pink); a room with no walls, a bed,
    ///   a ceiling fan and a small whirlwind of paper ("Thinkin Bout You").
    /// - The Weeknd: a wall of white lights too bright to look at and a red jacket on a stand ("Blinding Lights"); one
    ///   star on a very tall mast ("Lonely Star"). SZA: a garden swing among flowers under a small sun lamp ("Good
    ///   Days"); a sword on a stand and a yellow stripe ("Kill Bill": the city goes red).
    /// - The Punjabi terrace (Diljit Dosanjh, Karan Aujla, Prabh Singh): marigold garlands, a golden goat, dhol
    ///   drums to play, a tumbi on silk cushions, a clock stopped at 9:45.
    /// - And: TLC's waterfall off a tank, Ginuwine's carousel pony, GIVĒON's calendar and a cake with one candle,
    ///   Jay Sean's lift going down, Steve Lacy's guitar and striped amp, Daniel Caesar's two chairs, H.E.R.'s dark
    ///   glasses and guitar, Brent Faiyaz' chalk footsteps, 070 Shake's driving seat, Jason Derulo's speech bubble,
    ///   Sean Kingston's mirror, Mike Posner's boarding pass, blackbear's flickering sign, J Rice's safe, Danny
    ///   Madden's stack of books, and a pair of white wings on the edge of a roof ("I Believe I Can Fly").
    public partial class VelvetKit
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
                Add("Rihanna", "Umbrella", "Rihanna"); Add("Rihanna", "SOS", "@the drowned city"); Add("Frank Ocean", "Pink + White", "@the pool");
                Add("The Weeknd", "Blinding Lights", "The Weeknd"); Add("SZA", "Kill Bill", "@the bar and the sign");
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
            if (roofM == null) MakeMaterials();
            var root = new GameObject("Artist: " + a.name).transform; root.SetParent(ctx.Root, false);
            var front = facing.TryGetValue(a.name, out var f) ? f : new Vector3(center.x - a.x, 0, center.z - a.z).normalized;
            if (front.sqrMagnitude < .01f) front = Vector3.forward;
            var labels = new GameObject("Labels").transform; labels.SetParent(root, false);
            var area = root.gameObject.AddComponent<ArtistArea>();
            area.artistName = a.name; area.stationId = a.stationId; area.radius = a.r; area.glowColor = Rose;
            var z = new Plot { ctx = ctx, root = root, labels = labels, mk = new MeshKit(), solid = new MeshKit(), pictures = new MeshKit(), a = a, c = ctx.OnGround(a.x, a.z), front = front, side = Vector3.Cross(Vector3.up, front), area = area };
            var reach = Foot(a); var up = Vector3.up; var hue = a.name.Length + a.name[0];
            // the deck, a line of light round its edge
            z.mk.Prism(neonM[hue % 5], z.c - up * .33f, up, reach * .66f + .2f, .33f, 24); z.mk.Prism(deckM, z.c - up * .25f, up, reach * .66f, .33f, 24);
            switch (a.name)
            {
                case "Rihanna": Rihanna(z); break;
                case "Frank Ocean": FrankOcean(z); break;
                case "The Weeknd": TheWeeknd(z); break;
                case "SZA": Sza(z); break;
                default: Terrace(z); Others(z); break;
            }
            SongLamps(z);
            Board(z, z.At(reach * .6f, -reach * .45f - 1.5f), front);
            ArtistBits.Plaque(z.mk, z.solid, labels, a, z.At(reach * .6f + 1.5f, reach * .45f + 1.5f), front, dark, steel, Cream, 1.6f);
            Lamp(ctx, z.mk, z.At(reach * .5f, reach * .64f), true);
            if (z.area.glowLight == null) z.area.glowLight = ArtistBits.PointLight(root, z.c + up * 4.4f + front * 2f, Color.Lerp(Neons[hue % 5], Color.white, .5f), HasStation(a) ? 26f : 18f, 2.8f);
            area.glowRenderers = new Renderer[0];
            z.mk.Build("Area", root, false);
            z.solid.BuildCollider("Walls", root);
            z.pictures.Build("Covers", root, false);
            noGrass.Add(new Vector3(z.c.x, reach * .9f, z.c.z));
            if (HasStation(a) || reach >= 12f)
            {
                var mark = new GameObject("Shot: " + a.name + " up close").transform; mark.SetParent(ctx.Root, false);
                var cam = z.At(reach * .85f, reach * .3f) + up * 2f; var look = z.At(-reach * .3f, 0) + up * 2.2f - cam; mark.SetPositionAndRotation(cam, Quaternion.LookRotation(look.normalized));
            }
        }

        /// The covers in a lit glass case on a dark plinth.
        void Board(Plot z, Vector3 ground, Vector3 dir)
        {
            dir = Vector3.ProjectOnPlane(dir, Vector3.up).normalized; var rot = Quaternion.LookRotation(dir); var hue = z.a.name.Length;
            var n = Mathf.Max(1, Mathf.Min(7, z.a.songList?.Length ?? 1)); const float size = .9f; var w = n * size * 1.25f + .6f;
            var c = ground + Vector3.up * 1.7f;
            z.mk.Box(dark, c - Vector3.up * .5f - dir * .06f, new Vector3(w + .5f, size + 2f, .2f), rot); z.solid.Box(dark, c - Vector3.up * .5f, new Vector3(w + .5f, size + 2f, .3f), rot); z.mk.Box(white, c - Vector3.up * .1f + dir * .05f, new Vector3(w, size + .9f, .04f), rot);
            foreach (var e in new[] { -1f, 1f }) z.mk.Box(neonM[hue % 5], c + Vector3.up * (-.1f + e * (size / 2f + .5f)) + dir * .06f, new Vector3(w + .1f, .05f, .04f), rot);
            ArtistBits.PointLight(z.root, c + dir * 1.6f + Vector3.up * .8f, new Color(1f, .93f, .85f), 5f, 1.6f);
            ArtistBits.CoverWall(z.pictures, z.labels, z.ctx, z.a, c + dir * .08f + Vector3.up * .08f, dir, n, size, n, true, Ink);
        }

        /// A candle in a glass on the deck for each song without its own piece.
        void SongLamps(Plot z)
        {
            if (!SongIds.TryGetValue(z.a.name, out var list)) return;
            var up = Vector3.up; var k = 0; var seen = new HashSet<string>();
            foreach (var (_, id) in list)
            {
                if (z.lit.Contains(id) || !seen.Add(id)) continue;
                var p = z.At(Foot(z.a) * .55f - (k % 2) * 1.3f, Foot(z.a) * .2f + (k / 2) * 1.3f);
                z.mk.Prism(steel, p, up, .04f, .9f, 5); z.mk.Prism(SongGlow(id, Gold, 2f), p + up * .9f, up, .1f, .2f, 8); halos.Add(p + up * 1f, .9f, Gold, .5f); ArtistBits.PostCollider(z.root, p, 1f, .08f);
                k++;
            }
        }

        /// A roof terrace: a low parapet round the back and sides, a velvet sofa, their name in neon.
        PlaceFrame Terrace(Plot z)
        {
            var up = Vector3.up; var hue = z.a.name.Length + z.a.name[0]; var f = new PlaceFrame(z.At(-Foot(z.a) * .44f, 0), z.front); const float w = 8f;
            z.mk.Box(concrete, f.P(0, .45f, 0), new Vector3(w, .9f, .4f), f.r); z.solid.Box(concrete, f.P(0, .45f, 0), new Vector3(w, .9f, .4f), f.r); z.mk.Box(neonM[hue % 5], f.P(0, .92f, 0), new Vector3(w, .04f, .44f), f.r);
            foreach (var e in new[] { -1f, 1f }) { z.mk.Box(concrete, f.P(e * (w / 2f - .2f), .45f, 1.6f), new Vector3(.4f, .9f, 2.8f), f.r); z.solid.Box(concrete, f.P(e * (w / 2f - .2f), .45f, 1.6f), new Vector3(.4f, .9f, 2.8f), f.r); }
            Sofa(z.root, z.mk, f.P(-1.6f, 0, .9f), z.front, hue % 2 == 0 ? velvet : white, 2.2f);
            NeonSign(z.labels, z.mk, f.P(0, 3.3f, -.1f), z.front, z.a.name.ToUpperInvariant(), z.a.name.Length > 12 ? .34f : .46f, hue, Mathf.Max(3.4f, z.a.name.Length * .36f + 1f), 1f);
            return f;
        }

        // ---------------------------------------------------------------- Rihanna
        void Rihanna(Plot z)
        {
            var up = Vector3.up; var rot = z.Rot; var rng = new System.Random(88); var um = Song(z, "Umbrella"); var di = Song(z, "Diamonds"); var so = Song(z, "SOS"); life.umbrellaSong = um; life.diamondsSong = di; life.sosSong = so;
            // the umbrella: a great black canopy on a curved handle; rain falls all round its edge, and none under it
            var c = z.At(-3f, 0); z.mk.Prism(steel, c - up * .2f, up, .1f, 5.2f, 6); for (var k = 0; k < 10; k++) { var d = Quaternion.Euler(0, k * 36f, 0) * Vector3.forward; z.mk.Box(k % 2 == 0 ? dark : WorldMaterials.Solid(new Color(.12f, .1f, .16f)), c + up * 4.6f + d * 1.4f, new Vector3(1.8f, .06f, 2.9f), Quaternion.LookRotation(d) * Quaternion.Euler(20f, 0, 0)); z.mk.Prism(steel, c + up * 5f, (d - up * .36f).normalized, .02f, 3f, 3); z.mk.Blob(SongGlow(um, new Color(.6f, .8f, 1f), 1.8f), c + up * 4.02f + d * 2.76f, Vector3.one * .05f, rng, 0f); }
            z.mk.Prism(steel, c + up * 5f, up, .04f, .5f, 4); ArtistBits.PostCollider(z.root, c, 5f, .12f); life.umbrellaAt = c + up * 3.9f; z.mk.Prism(SongGlow(um, new Color(.3f, .5f, .9f), .6f), c + up * .095f, up, 2.9f, .012f, 24); z.mk.Prism(deckM, c + up * .1f, up, 2.3f, .012f, 24);
            Sofa(z.root, z.mk, c + z.front * 1f + z.side * 1.2f, (z.front - z.side * .3f).normalized, velvet, 2f); z.area.glowLight = ArtistBits.PointLight(z.root, c + up * 3f + z.front * 2f, new Color(.7f, .8f, 1f), 26f, 3f);
            string[] under = { "You stand under it. The rain comes down all round you in a ring and not one drop gets in.", "There's room for two under here. There was always room for two.", "Ella, ella. The rain on the canopy keeps the beat." }; var un = 0;
            Spot("Under the umbrella", c + up * 1.4f + z.front * .9f, "Stand under the umbrella", new Vector3(2.2f, 2f, 2.2f)).onUse = _ => Say1(c + up * 3.2f, under[un++ % under.Length]);
            // "Diamonds": a diamond as tall as you on a black plinth
            var dp = z.At(3f, 7.4f); z.mk.Prism(dark, dp, up, .7f, .6f, 8); var dm = SongGlow(di, new Color(.75f, .95f, 1f), 1.6f); z.mk.Cone(dm, dp + up * 1.7f, .8f, .7f, 8); z.mk.Cone(dm, dp + up * 1.7f, .8f, -1.1f, 8); z.mk.Cone(SongGlow(di, Color.white, 2.2f), dp + up * 1.72f, .5f, .44f, 8, 22.5f); halos.Add(dp + up * 1.6f, 2.6f, new Color(.7f, .95f, 1f), .6f); ArtistBits.PostCollider(z.root, dp, 2.4f, .7f); ArtistBits.PointLight(z.root, dp + up * 2.6f, new Color(.75f, .95f, 1f), 12f, 2.4f);
            ArtistBits.Label(z.labels, "shine bright", dp + up * .3f + z.front * .72f, z.front, .12f, Cream, TextAnchor.MiddleCenter);
            // "SOS": a signal lamp on a mast, blinking it out to the drowned towers
            var sp = z.At(3f, -7.4f); z.mk.Prism(steel, sp - up * .2f, up, .1f, 6.4f, 5, .5f); z.mk.Box(dark, sp + up * 6.2f, new Vector3(.6f, .6f, .6f), rot); z.mk.Blob(beaconM, sp + up * 6.2f + z.front * .3f, Vector3.one * .22f, rng, 0f); halos.Add(sp + up * 6.2f + z.front * .5f, 2f, new Color(1f, .15f, .15f), .6f); ArtistBits.PostCollider(z.root, sp, 6f, .12f); z.mk.Box(dark, sp + up * 1.1f + z.front * .2f, new Vector3(.3f, .2f, .2f), rot); z.mk.Box(SongGlow(so, Rose, 2f), sp + up * 1.16f + z.front * .31f, new Vector3(.1f, .04f, .02f), rot);
            string[] key = { "Dit-dit-dit. Dah, dah, dah. Dit-dit-dit. Far out on the water a red light answers.", "You tap it out. Every mast on the island picks it up and passes it on.", "S, O, S. Please, someone, help me. A window lights, forty floors under the sea." }; var kn = 0;
            Spot("The signal key", sp + up * 1.2f + z.front * .5f, "Tap out a signal", new Vector3(.9f, 1.4f, .9f)).onUse = _ => { life.played = Time.time; Say1(sp + up * 2.6f, key[kn++ % key.Length]); };
        }

        // ---------------------------------------------------------------- Frank Ocean
        void FrankOcean(Plot z)
        {
            var up = Vector3.up; var rot = z.Rot; var rng = new System.Random(2012); var pw = Song(z, "Pink + White"); var tb = Song(z, "Thinkin Bout You"); var wf = Song(z, "White Ferrari"); life.pinkSong = pw;
            // "White Ferrari": a low white sports car parked on the roof, lamps on, the passenger door open
            var car = z.At(-3f, -3f) + up * .12f; var cr = rot * Quaternion.Euler(0, 70f, 0); var cf = cr * Vector3.forward; var cs = cr * Vector3.right; var wh = SongGlow(wf, new Color(.97f, .97f, .95f), .35f);
            z.mk.Box(wh, car + up * .5f, new Vector3(1.9f, .44f, 4.4f), cr); z.mk.Box(wh, car + up * .86f - cf * .3f, new Vector3(1.6f, .4f, 1.8f), cr); z.mk.Box(dark, car + up * .88f - cf * .3f, new Vector3(1.64f, .3f, 1.5f), cr); z.mk.Box(wh, car + up * .62f + cf * 1.5f, new Vector3(1.8f, .2f, 1.4f), cr * Quaternion.Euler(6f, 0, 0));
            foreach (var e in new[] { -1f, 1f }) { foreach (var q in new[] { -1.4f, 1.4f }) { z.mk.Prism(dark, car + up * .34f + cs * e * .86f + cf * q, cs * e, .34f, .22f, 12); z.mk.Prism(steel, car + up * .34f + cs * e * .98f + cf * q, cs * e, .2f, .01f, 10); } z.mk.Blob(SongGlow(wf, new Color(1f, .97f, .85f), 2.4f), car + up * .52f + cs * e * .66f + cf * 2.2f, new Vector3(.14f, .06f, .05f), rng, 0f); z.mk.Blob(GlowM(new Color(1f, .15f, .1f), 1.4f), car + up * .56f + cs * e * .7f - cf * 2.2f, new Vector3(.16f, .05f, .04f), rng, 0f); }
            z.mk.Box(wh, car + up * .7f - cf * .3f + cs * 1.5f, new Vector3(.08f, .5f, 1.1f), cr * Quaternion.Euler(0, 50f, 0)); BoxCol(z.root, car + up * .6f, new Vector3(1.9f, 1.2f, 4.4f), cr); halos.Add(car + up * .5f + cf * 2.8f, 1.6f, Cream, .5f); Interact.Seat(tlife.transform, car + up * .7f - cf * .3f + cs * .46f, cf, "Sit in the passenger seat").seatHeight = .5f;
            z.area.glowLight = ArtistBits.PointLight(z.root, car + up * 3f, new Color(1f, .95f, .9f), 26f, 2.8f);
            // "Pink + White": a cabana in pink and white stripes, clouds painted inside, a daybed
            var cb = z.At(-2f, 6.6f); var pinkM = SongGlow(pw, new Color(1f, .55f, .72f), .6f); foreach (var (x, zz) in new[] { (-1.8f, -1.4f), (1.8f, -1.4f), (1.8f, 1.4f), (-1.8f, 1.4f) }) { z.mk.Prism(white, cb + z.side * x + z.front * zz - up * .2f, up, .06f, 3f, 5); ArtistBits.PostCollider(z.root, cb + z.side * x + z.front * zz, 2.8f, .08f); }
            for (var k = 0; k < 8; k++) foreach (var e in new[] { -1f, 1f }) z.mk.Box(k % 2 == 0 ? pinkM : white, cb + up * (2.9f + (k % 2) * .01f) + z.side * (-1.72f + k * .49f) + z.front * e * .8f, new Vector3(.47f, .04f, 1.8f), rot * Quaternion.Euler(e * 16f, 0, 0)); z.mk.Box(SongGlow(pw, new Color(.55f, .75f, 1f), .5f), cb + up * 1.5f - z.front * 1.44f, new Vector3(3.6f, 2.6f, .05f), rot); for (var k = 0; k < 4; k++) z.mk.Blob(white, cb + up * (1.2f + (k % 2) * .9f) - z.front * 1.36f + z.side * (-1.2f + k * .8f), new Vector3(.5f, .26f, .06f), rng, .2f);
            z.mk.Box(white, cb + up * .3f, new Vector3(1.6f, .4f, 2.2f), rot); z.mk.Box(pinkM, cb + up * .54f, new Vector3(1.5f, .1f, 2.1f), rot); BoxCol(z.root, cb + up * .3f, new Vector3(1.6f, .6f, 2.2f), rot); Interact.Seat(tlife.transform, cb + up * .62f, z.front, "Lie on the daybed").seatHeight = .6f; halos.Add(cb + up * 1.6f + z.front * 1f, 2.6f, Rose, .3f);
            // "Thinkin Bout You": a room with no walls: a bed, a ceiling fan on a pole, a small whirlwind of paper
            var rm = z.At(4f, 6.5f); z.mk.Box(white, rm + up * .3f, new Vector3(1.5f, .4f, 2f), rot * Quaternion.Euler(0, 30f, 0)); z.mk.Box(SongGlow(tb, new Color(.5f, .85f, .55f), .5f), rm + up * .54f, new Vector3(1.4f, .1f, 1.5f), rot * Quaternion.Euler(0, 30f, 0)); BoxCol(z.root, rm + up * .3f, new Vector3(1.5f, .6f, 2f), rot * Quaternion.Euler(0, 30f, 0));
            var tw = z.At(6.5f, 3f); for (var k = 0; k < 16; k++) { var a = k * .9f; var r = .2f + k * .05f; z.mk.Box(white, tw + up * (.3f + k * .16f) + new Vector3(Mathf.Cos(a) * r, 0, Mathf.Sin(a) * r), new Vector3(.2f, .01f, .26f), Quaternion.Euler(k * 23f, k * 51f, k * 17f)); } z.mk.Prism(SongGlow(tb, Cream, .5f), tw + up * .1f, up, .5f, .01f, 12);
            string[] think = { "A tornado flew around this room before you came. It left the papers going round.", "You've been thinking about it. Forever. Do you not think so far ahead?", "The fan turns. The sheets are still warm. Nobody's here." }; var tn = 0; Spot("The whirlwind", tw + up * 1.4f, "Think about it", new Vector3(1.4f, 2.4f, 1.4f)).onUse = _ => Say1(tw + up * 3.4f, think[tn++ % think.Length]);
        }

        // ---------------------------------------------------------------- The Weeknd
        void TheWeeknd(Plot z)
        {
            var up = Vector3.up; var rot = z.Rot; var rng = new System.Random(2020); var bl = Song(z, "Blinding Lights"); var ls = Song(z, "Lonely Star"); life.blindingSong = bl;
            // a wall of white lights too bright to look at, a red jacket on a stand in front of it
            var c = z.At(-4f, 0); var blM = new Material(GlowM(new Color(1f, .97f, .9f), 2.2f)) { name = "Blinding lights" }; life.blinding = blM; z.lit.Add(bl);
            z.mk.Box(dark, c + up * 2.4f, new Vector3(9f, 4.8f, .4f), rot); z.solid.Box(dark, c + up * 2.4f, new Vector3(9f, 4.8f, .4f), rot); for (var r = 0; r < 6; r++) for (var k = 0; k < 12; k++) z.mk.Blob(blM, c + up * (.5f + r * .76f) + z.side * (-4.1f + k * .745f) + z.front * .24f, Vector3.one * .16f, rng, 0f);
            halos.Add(c + up * 2.4f + z.front * 1f, 6f, Cream, .5f); z.area.glowLight = ArtistBits.PointLight(z.root, c + up * 2.6f + z.front * 3f, new Color(1f, .95f, .9f), 28f, 3.6f);
            var jk = c + z.front * 3f; z.mk.Prism(steel, jk, up, .18f, .03f, 8); z.mk.Prism(steel, jk, up, .02f, 1.6f, 4); var red = GlowM(new Color(.85f, .08f, .1f), .5f); z.mk.Box(red, jk + up * 1.2f, new Vector3(.56f, .8f, .26f), rot); foreach (var e in new[] { -1f, 1f }) z.mk.Box(red, jk + up * 1.16f + z.side * e * .38f, new Vector3(.16f, .84f, .2f), rot * Quaternion.Euler(0, 0, e * 8f)); z.mk.Box(dark, jk + up * 1.2f + z.front * .135f, new Vector3(.12f, .8f, .01f), rot); ArtistBits.PostCollider(z.root, jk, 1.7f, .3f);
            ArtistBits.Label(z.labels, "I can't sleep until I feel your touch", jk + up * 2.1f, z.front, .14f, Cream, TextAnchor.MiddleCenter);
            // "Lonely Star": one star on a very tall mast, a bench under it
            var st = z.At(3.4f, 6.4f); z.mk.Prism(steel, st - up * .2f, up, .1f, 12f, 5, .3f); ArtistBits.PostCollider(z.root, st, 6f, .12f); var sm = SongGlow(ls, new Color(.9f, .92f, 1f), 2.4f); for (var k = 0; k < 5; k++) z.mk.Box(sm, st + up * 12.2f + Quaternion.AngleAxis(k * 72f, z.front) * up * .5f, new Vector3(.3f, 1f, .1f), Quaternion.AngleAxis(k * 72f, z.front) * rot); z.mk.Prism(sm, st + up * 12.2f - z.front * .05f, z.front, .34f, .1f, 10); halos.Add(st + up * 12.2f, 3f, Cream, .6f);
            Sofa(z.root, z.mk, st + z.front * 1.6f, z.front, velvet, 2f);
        }

        // ---------------------------------------------------------------- SZA
        void Sza(Plot z)
        {
            var up = Vector3.up; var rot = z.Rot; var rng = new System.Random(2022); var gd = Song(z, "Good Days"); var kb = Song(z, "Kill Bill"); life.goodDaysSong = gd; life.killBillSong = kb;
            // "Good Days": a garden swing among flowers, a small sun on a pole
            var c = z.At(-3f, 2f); foreach (var e in new[] { -1f, 1f }) { z.mk.Prism(white, c + z.side * e * 1.3f - up * .2f, up, .07f, 2.9f, 5); ArtistBits.PostCollider(z.root, c + z.side * e * 1.3f, 2.7f, .09f); if (e < 0) foreach (var q in new[] { -.7f, .7f }) z.mk.Prism(steel, c + up * 2.6f + z.side * q, -up, .012f, 2f, 3); } z.mk.Box(white, c + up * 2.7f, new Vector3(2.8f, .1f, .1f), rot); z.mk.Box(white, c + up * .5f, new Vector3(1.7f, .06f, .5f), rot); z.mk.Box(white, c + up * .9f - z.front * .24f, new Vector3(1.7f, .6f, .05f), rot); Interact.Bench(tlife.transform, c + up * .56f - z.side * .5f, c + up * .56f + z.side * .5f, z.front);
            for (var k = 0; k < 14; k++) { var a = k * 26f * Mathf.Deg2Rad; var p = c + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * (2.4f + (k % 3) * .6f); p.y = z.ctx.Ground(p.x, p.z) + .1f; if (Vector3.Dot(p - c, z.front) > 1.4f) continue; z.mk.Blob(leafM, p + up * .26f, new Vector3(.4f, .3f, .4f), rng, .3f); z.mk.Blob(SongGlow(gd, Neons[k % 5], .9f), p + up * .6f, Vector3.one * .12f, rng, .2f); }
            var sun = c - z.front * 2.4f - z.side * 2.4f; z.mk.Prism(steel, sun - up * .2f, up, .06f, 4.2f, 5); z.mk.Blob(SongGlow(gd, new Color(1f, .8f, .4f), 2f), sun + up * 4.4f, Vector3.one * .6f, rng, 0f); halos.Add(sun + up * 4.4f, 3.4f, Gold, .6f); ArtistBits.PostCollider(z.root, sun, 4f, .08f); z.area.glowLight = ArtistBits.PointLight(z.root, sun + up * 3.6f + z.front * 2f, new Color(1f, .85f, .6f), 24f, 3f);
            ArtistBits.Label(z.labels, "good day in my mind", c + up * 3.1f, z.front, .16f, Gold, TextAnchor.MiddleCenter);
            // "Kill Bill": a sword on a stand, a yellow stripe down a black board
            var kp = z.At(2f, -6.4f); z.mk.Box(dark, kp + up * 1.5f, new Vector3(2.6f, 3f, .2f), rot); z.solid.Box(dark, kp + up * 1.5f, new Vector3(2.6f, 3f, .2f), rot); z.mk.Box(SongGlow(kb, new Color(1f, .85f, .1f), 1.2f), kp + up * 1.5f + z.front * .11f, new Vector3(.5f, 3f, .02f), rot); z.mk.Box(dark, kp + up * 1.5f + z.front * .12f + z.side * .32f, new Vector3(.06f, 3f, .02f), rot); z.mk.Box(dark, kp + up * 1.5f + z.front * .12f - z.side * .32f, new Vector3(.06f, 3f, .02f), rot);
            var sw = kp + z.front * 1.2f; z.mk.Box(woodM, sw + up * .5f, new Vector3(1.6f, .08f, .3f), rot); foreach (var e in new[] { -.6f, .6f }) z.mk.Box(woodM, sw + up * .25f + z.side * e, new Vector3(.08f, .5f, .3f), rot); z.mk.Box(steel, sw + up * .62f + z.side * .2f, new Vector3(1.1f, .03f, .05f), rot * Quaternion.Euler(0, 0, 3f)); z.mk.Box(dark, sw + up * .6f - z.side * .52f, new Vector3(.36f, .05f, .06f), rot * Quaternion.Euler(0, 0, 3f)); z.mk.Prism(Paint(Gold), sw + up * .6f - z.side * .34f - z.front * .0f, z.side, .06f, .02f, 8); BoxCol(z.root, sw + up * .35f, new Vector3(1.6f, .7f, .3f), rot);
            string[] sword = { "You don't touch it. You think about touching it. You're mature like that.", "I might. No. I won't. ...I might.", "The blade catches the red off the masts. Rather be in hell than alone." }; var sn = 0; Spot("The sword", sw + up * 1f, "Think about it", new Vector3(1.6f, 1.2f, .9f)).onUse = _ => Say1(sw + up * 2.4f, sword[sn++ % sword.Length]);
        }

        // ---------------------------------------------------------------- everyone else
        void Others(Plot z)
        {
            var up = Vector3.up; var rot = z.Rot; var p = z.At(2.6f, 3.2f); var rng = new System.Random(z.a.name.Length * 3); string s;
            switch (z.a.name)
            {
                case "Diljit Dosanjh":
                    s = Song(z, "G.O.A.T."); z.mk.Prism(dark, p, up, .6f, .7f, 8); var gold = SongGlow(s, Gold, 1.1f); z.mk.Blob(gold, p + up * 1.2f, new Vector3(.22f, .22f, .4f), rng, .04f); z.mk.Blob(gold, p + up * 1.5f + z.front * .42f, new Vector3(.12f, .14f, .18f), rng, 0f); foreach (var e in new[] { -.08f, .08f }) { z.mk.Cone(gold, p + up * 1.62f + z.front * .38f + z.side * e, .03f, .2f, 4); foreach (var q in new[] { -.26f, .26f }) z.mk.Box(gold, p + up * .9f + z.side * e * 2f + z.front * q, new Vector3(.06f, .4f, .06f), rot); } z.mk.Blob(gold, p + up * 1.4f + z.front * .56f, new Vector3(.03f, .1f, .03f), rng, 0f); halos.Add(p + up * 1.3f, 1.6f, Gold, .5f); ArtistBits.PostCollider(z.root, p, 1.8f, .6f);
                    Marigolds(z, 0); Dhol(z, z.At(4.6f, 0f), s); break;
                case "Karan Aujla":
                    s = Song(z, "Softly"); for (var k = 0; k < 4; k++) z.mk.Blob(Paint(new[] { Wine, new Color(.85f, .55f, .15f), new Color(.2f, .4f, .5f), Cream }[k]), p + z.side * (-.8f + k * .55f) + z.front * ((k % 2) * .4f) + up * .22f, new Vector3(.4f, .16f, .36f), rng, .1f); Interact.Seat(tlife.transform, p + up * .3f, z.front, "Sit on the cushions").seatHeight = .26f;
                    var tu = p + z.side * 1.5f; z.mk.Prism(steel, tu, up, .12f, .03f, 6); z.mk.Blob(SongGlow(s, new Color(.9f, .6f, .25f), .6f), tu + up * .3f, Vector3.one * .14f, rng, .05f); z.mk.Box(woodM, tu + up * .9f, new Vector3(.04f, 1f, .04f), rot); z.mk.Box(woodM, tu + up * 1.36f, new Vector3(.2f, .04f, .04f), rot); Marigolds(z, 2); break;
                case "Prabh Singh":
                    s = Song(z, "9:45"); z.mk.Prism(steel, p - up * .2f, up, .08f, 3.2f, 6); z.mk.Prism(dark, p + up * 3.3f - z.front * .1f, z.front, .7f, .2f, 16); z.mk.Prism(SongGlow(s, Cream, 1f), p + up * 3.3f + z.front * .1f, z.front, .62f, .02f, 16); z.mk.Box(dark, p + up * 3.3f + z.front * .13f + (Quaternion.AngleAxis(-285f, z.front) * up) * .18f, new Vector3(.05f, .38f, .02f), Quaternion.AngleAxis(-285f, z.front) * rot); z.mk.Box(dark, p + up * 3.3f + z.front * .13f + (Quaternion.AngleAxis(-270f, z.front) * up) * .26f, new Vector3(.04f, .54f, .02f), Quaternion.AngleAxis(-270f, z.front) * rot); halos.Add(p + up * 3.3f + z.front * .4f, 1.4f, Cream, .4f); ArtistBits.PostCollider(z.root, p, 3f, .1f);
                    ArtistBits.Label(z.labels, "9:45", p + up * 2.2f + z.front * .1f, z.front, .3f, Gold, TextAnchor.MiddleCenter); Marigolds(z, 1); break;
                case "TLC":
                    s = Song(z, "Waterfalls"); WaterTower(z.ctx, z.mk, p - z.front * 1f, 1f, 1); var wt = SongGlow(s, new Color(.4f, .9f, 1f), 1.2f); z.mk.Box(wt, p - z.front * 1f + up * 1.6f + z.front * 1.7f, new Vector3(1.3f, 2.8f, .06f), rot); z.mk.Prism(concrete, p + z.front * 1f, up, 1.4f, .3f, 14); z.mk.Prism(wt, p + z.front * 1f + up * .3f, up, 1.2f, .02f, 14); life.waterfallAt = p - z.front * 1f + up * 2.9f + z.front * 1.76f; halos.Add(p + z.front * 1f + up * .8f, 2f, Aqua, .4f);
                    ArtistBits.Label(z.labels, "don't go chasing", p + z.front * 2.44f + up * .16f, z.front, .12f, Cream, TextAnchor.MiddleCenter); break;
                case "Ginuwine":
                    s = Song(z, "Pony"); z.mk.Prism(dark, p, up, 1.2f, .2f, 14); z.mk.Prism(Paint(Gold), p + up * .2f, up, .05f, 2.6f, 6, 1f, 3f); var pony = SongGlow(s, new Color(.95f, .93f, .9f), .5f); z.mk.Blob(pony, p + up * 1.3f, new Vector3(.24f, .26f, .5f), rng, .04f); z.mk.Prism(pony, p + up * 1.4f + z.front * .4f, (up + z.front * .5f).normalized, .12f, .5f, 6, .8f); z.mk.Blob(pony, p + up * 1.86f + z.front * .7f, new Vector3(.1f, .12f, .2f), rng, 0f); foreach (var e in new[] { -.12f, .12f }) foreach (var q in new[] { -.3f, .3f }) z.mk.Box(pony, p + up * .86f + z.side * e + z.front * q, new Vector3(.06f, .5f, .06f), rot * Quaternion.Euler(q * 40f, 0, 0)); z.mk.Box(Paint(Wine), p + up * 1.56f, new Vector3(.3f, .06f, .4f), rot); ArtistBits.PostCollider(z.root, p, 2.4f, .5f);
                    Interact.Seat(tlife.transform, p + up * 1.62f, z.front, "Ride the carousel pony").seatHeight = .9f; for (var k = 0; k < 8; k++) z.mk.Blob(neonM[k % 5], p + up * 2.8f + Quaternion.Euler(0, k * 45f, 0) * Vector3.forward * .6f, Vector3.one * .07f, rng, 0f); break;
                case "GIVĒON":
                    s = Song(z, "Heartbreak Anniversary"); z.mk.Prism(steel, p, up, .04f, .72f, 5); z.mk.Prism(white, p + up * .72f, up, .46f, .04f, 12); ArtistBits.PostCollider(z.root, p, .8f, .36f); z.mk.Prism(white, p + up * .76f, up, .2f, .14f, 12); z.mk.Prism(Paint(Wine), p + up * .9f, up, .2f, .02f, 12); z.mk.Prism(white, p + up * .92f, up, .012f, .12f, 4); z.mk.Blob(SongGlow(s, new Color(1f, .8f, .4f), 2.2f), p + up * 1.08f, new Vector3(.025f, .05f, .025f), rng, 0f); halos.Add(p + up * 1.1f, .7f, Gold, .5f);
                    var cl = z.At(1f, 5.4f); z.mk.Prism(steel, cl - up * .2f, up, .04f, 2f, 4); z.mk.Box(white, cl + up * 1.7f, new Vector3(1.2f, 1.2f, .04f), rot); z.mk.Box(Paint(Wine), cl + up * 2.2f + z.front * .03f, new Vector3(1.2f, .24f, .02f), rot); for (var k = 0; k < 20; k++) z.mk.Box(k == 13 ? SongGlow(s, Rose, 2f) : (Material)dark, cl + up * (1.9f - (k / 5) * .22f) + z.side * (-.4f + (k % 5) * .2f) + z.front * .03f, new Vector3(.1f, .1f, .01f), rot); ArtistBits.PostCollider(z.root, cl, 2f, .06f); break;
                case "Jay Sean":
                    s = Song(z, "Down"); z.mk.Box(concrete, p + up * 1.4f, new Vector3(2.4f, 2.8f, 1.6f), rot); z.solid.Box(concrete, p + up * 1.4f, new Vector3(2.4f, 2.8f, 1.6f), rot); foreach (var e in new[] { -.45f, .45f }) z.mk.Box(steel, p + up * 1.1f + z.front * .81f + z.side * e, new Vector3(.86f, 2.1f, .03f), rot); var arrow = SongGlow(s, Aqua, 2.2f); z.mk.Cone(arrow, p + up * 2.62f + z.front * .82f, .16f, -.24f, 3, 0f); z.mk.Box(dark, p + up * 2.5f + z.front * .81f, new Vector3(.6f, .4f, .02f), rot); halos.Add(p + up * 2.5f + z.front * 1.1f, .9f, Aqua, .5f);
                    string[] lift = { "Ding. The doors open on a floor that is under water. A fish goes past. The doors close.", "Going down. The lights count backwards: 40, 39, 38, sea level, 36...", "Baby, are you down, down, down? The lift says yes." }; var dn = 0; Spot("The lift", p + up * 1.2f + z.front * 1.3f, "Call the lift", new Vector3(2f, 2.2f, 1f)).onUse = _ => Say1(p + up * 3.2f, lift[dn++ % lift.Length]); break;
                case "Steve Lacy":
                    s = Song(z, "Bad Habit"); z.mk.Box(dark, p + up * .5f, new Vector3(.9f, 1f, .5f), rot); for (var k = 0; k < 6; k++) z.mk.Box(k % 2 == 0 ? SongGlow(s, Rose, .7f) : white, p + up * .5f + z.side * (-.375f + k * .15f) + z.front * .26f, new Vector3(.15f, .8f, .02f), rot); BoxCol(z.root, p + up * .5f, new Vector3(.9f, 1f, .5f), rot);
                    var g1 = p + z.side * 1.2f; z.mk.Prism(steel, g1, up, .12f, .03f, 6); z.mk.Blob(Paint(new Color(.2f, .5f, .5f)), g1 + up * .3f, new Vector3(.2f, .26f, .06f), rng, .04f); z.mk.Box(woodM, g1 + up * .88f - z.front * .1f, new Vector3(.05f, .72f, .03f), rot * Quaternion.Euler(-9f, 0, 0)); ArtistBits.Label(z.labels, "I wish I knew\nyou wanted me", p + up * 1.8f, z.front, .13f, Rose, TextAnchor.MiddleCenter); break;
                case "Daniel Caesar":
                    s = Song(z, "Get You"); foreach (var e in new[] { -1f, 1f }) { var cp = p + z.side * e * .6f; var cf = (-z.side * e + z.front * .4f).normalized; var cr = Quaternion.LookRotation(cf); z.mk.Box(white, cp + up * .45f, new Vector3(.46f, .05f, .46f), cr); z.mk.Box(white, cp + up * .8f - cf * .21f, new Vector3(.46f, .7f, .05f), cr); foreach (var (x, zz) in new[] { (-.2f, -.2f), (.2f, -.2f), (.2f, .2f), (-.2f, .2f) }) z.mk.Box(white, cp + up * .22f + cr * new Vector3(x, 0, zz), new Vector3(.04f, .44f, .04f), cr); Interact.Seat(tlife.transform, cp + up * .5f, cf); }
                    var arc = SongGlow(s, Gold, 1.8f); for (var k = 0; k < 13; k++) { var a = k / 12f * Mathf.PI; z.mk.Blob(arc, p + up * (1.2f + Mathf.Sin(a) * 1.6f) - z.side * Mathf.Cos(a) * 1.4f - z.front * .4f, Vector3.one * .06f, rng, 0f); } halos.Add(p + up * 2.4f, 1.8f, Gold, .35f); ArtistBits.Label(z.labels, "who would've thought", p + up * 3f - z.front * .4f, z.front, .13f, Cream, TextAnchor.MiddleCenter); break;
                case "H.E.R.":
                    s = Song(z, "Focus"); z.mk.Prism(steel, p, up, .16f, .03f, 8); z.mk.Prism(steel, p, up, .02f, 1.5f, 4); foreach (var e in new[] { -.14f, .14f }) { z.mk.Prism(dark, p + up * 1.56f + z.side * e - z.front * .02f, z.front, .11f, .04f, 12); z.mk.Prism(SongGlow(s, Violet, 1.4f), p + up * 1.56f + z.side * e + z.front * .02f, z.front, .085f, .01f, 12); } z.mk.Box(dark, p + up * 1.56f, new Vector3(.1f, .03f, .03f), rot); ArtistBits.PostCollider(z.root, p, 1.6f, .14f);
                    var g2 = p + z.side * 1.2f; z.mk.Prism(steel, g2, up, .12f, .03f, 6); z.mk.Blob(Paint(Wine), g2 + up * .3f, new Vector3(.2f, .26f, .06f), rng, .04f); z.mk.Box(woodM, g2 + up * .88f - z.front * .1f, new Vector3(.05f, .72f, .03f), rot * Quaternion.Euler(-9f, 0, 0)); break;
                case "Brent Faiyaz":
                    s = Song(z, "DEAD MAN WALKING"); var chalk = SongGlow(s, Cream, .7f); for (var k = 0; k < 9; k++) { var fp = z.At(1f + k * .7f, 1.4f + (k % 2 == 0 ? -.2f : .2f) + k * .3f) + up * .11f; z.mk.Blob(chalk, fp, new Vector3(.09f, .01f, .16f), rng, 0f); z.mk.Blob(chalk, fp - z.front * .2f, new Vector3(.07f, .01f, .07f), rng, 0f); }
                    var bt = z.At(5.4f, 5.2f); z.mk.Prism(steel, bt, up, .04f, .72f, 5); z.mk.Prism(dark, bt + up * .72f, up, .4f, .04f, 12); z.mk.Prism(Paint(new Color(.1f, .3f, .16f)), bt + up * .76f, up, .05f, .22f, 8); z.mk.Prism(SongGlow(s, Gold, 1.4f), bt + up * .98f, up, .02f, .1f, 6); ArtistBits.PostCollider(z.root, bt, .8f, .36f); break;
                case "070 Shake":
                    s = Song(z, "Baby Driver"); z.mk.Box(velvet, p + up * .34f, new Vector3(.6f, .16f, .6f), rot); z.mk.Box(velvet, p + up * .8f - z.front * .26f, new Vector3(.6f, .9f, .14f), rot * Quaternion.Euler(-10f, 0, 0)); z.mk.Box(dark, p + up * .14f, new Vector3(.66f, .28f, .66f), rot); BoxCol(z.root, p + up * .3f, new Vector3(.66f, .6f, .66f), rot); Interact.Seat(tlife.transform, p + up * .46f, z.front, "Sit in the driving seat").seatHeight = .44f;
                    var whl = p + z.front * .7f + up * .9f; z.mk.Prism(dark, whl - z.front * .02f, (z.front + up * .5f).normalized, .22f, .04f, 14); z.mk.Prism(SongGlow(s, Violet, 1.6f), whl, (z.front + up * .5f).normalized, .06f, .05f, 8); z.mk.Prism(steel, p + z.front * .9f, (up * .9f - z.front * .2f).normalized, .03f, .9f, 4); foreach (var e in new[] { -1f, 1f }) z.mk.Blob(GlowM(Cream, 1.6f), p + z.front * 1.6f + z.side * e * .5f + up * .4f, Vector3.one * .08f, rng, 0f); break;
                case "Jason Derulo":
                    s = Song(z, "Whatcha Say"); z.mk.Prism(steel, p - up * .2f, up, .05f, 2.4f, 5); ArtistBits.PostCollider(z.root, p, 2.2f, .07f); z.mk.Blob(SongGlow(s, Cream, .8f), p + up * 2.9f, new Vector3(1.2f, .6f, .08f), rng, .05f); z.mk.Tri(white, p + up * 2.4f - z.side * .3f, p + up * 2.4f + z.side * .1f, p + up * 2f - z.side * .5f, p - z.front); ArtistBits.Label(z.labels, "mmm, whatcha say", p + up * 2.9f + z.front * .1f, z.front, .18f, Ink, TextAnchor.MiddleCenter); break;
                case "Sean Kingston":
                    s = Song(z, "Beautiful Girls"); z.mk.Box(Paint(Gold), p + up * 1.2f, new Vector3(1.2f, 2.2f, .1f), rot); z.mk.Box(WorldMaterials.Solid(new Color(.55f, .6f, .75f), .95f), p + up * 1.2f + z.front * .06f, new Vector3(1f, 2f, .02f), rot); BoxCol(z.root, p + up * 1.2f, new Vector3(1.2f, 2.2f, .14f), rot); var ht = SongGlow(s, Rose, 2f); for (var h = 0; h < 3; h++) for (var k = 0; k < 10; k++) { var t = k / 10f * Mathf.PI * 2f; var hx = 16f * Mathf.Pow(Mathf.Sin(t), 3f); var hy = 13f * Mathf.Cos(t) - 5f * Mathf.Cos(2f * t) - 2f * Mathf.Cos(3f * t) - Mathf.Cos(4f * t); z.mk.Blob(ht, p + up * (2.7f + h * .0f + hy * .012f) + z.side * (-.5f + h * .5f + hx * .012f) + z.front * .08f, Vector3.one * .025f, rng, 0f); } break;
                case "Mike Posner":
                    s = Song(z, "I Took A Pill"); z.mk.Box(white, p + up * .36f, new Vector3(.6f, .05f, 1.1f), rot * Quaternion.Euler(-14f, 0, 0)); z.mk.Box(woodM, p + up * .18f, new Vector3(.64f, .36f, .9f), rot); Interact.Seat(tlife.transform, p + up * .46f, z.front, "Sit in the deck chair").seatHeight = .44f; var bp = new MeshKit(); bp.Box(white, new Vector3(0, .006f, 0), new Vector3(.3f, .012f, .13f), Quaternion.identity); bp.Box(SongGlow(s, Aqua, 1f), new Vector3(-.08f, .014f, 0), new Vector3(.1f, .004f, .1f), Quaternion.identity); Thing(bp, "Boarding pass", "a boarding pass: IBZ, one way", p + z.side * .9f + up * .12f, rot, new Vector3(.3f, .04f, .13f), .05f);
                    z.mk.Box(dark, p - z.side * 1.2f + up * .5f, new Vector3(1f, 1f, .5f), rot); foreach (var e in new[] { -.24f, .24f }) z.mk.Prism(steel, p - z.side * (1.2f + e) + up * 1.02f, up, .18f, .02f, 12); BoxCol(z.root, p - z.side * 1.2f + up * .5f, new Vector3(1f, 1f, .5f), rot); break;
                case "blackbear":
                    s = Song(z, "idfc"); NeonSign(z.labels, z.mk, p + up * 2.6f, z.front, "idfc", .7f, 1, 2.6f, 1.2f); z.mk.Box(SongGlow(s, Violet, 2f), p + up * 1.94f + z.front * .02f, new Vector3(2f, .04f, .02f), rot); foreach (var e in new[] { -1.1f, 1.1f }) ArtistBits.PostCollider(z.root, p + z.side * e, 2f, .07f); break;
                case "J Rice":
                    s = Song(z, "Where My Diamonds Hide"); z.mk.Box(dark, p + up * .5f, new Vector3(1f, 1f, .9f), rot); z.mk.Box(steel, p + up * .5f + z.front * .46f + z.side * .5f, new Vector3(.9f, .9f, .06f), rot * Quaternion.Euler(0, -60f, 0)); z.mk.Prism(steel, p + up * .5f + z.front * .46f, z.front, .14f, .04f, 10); BoxCol(z.root, p + up * .5f, new Vector3(1f, 1f, .9f), rot); var dmm = SongGlow(s, new Color(.75f, .95f, 1f), 2f); for (var k = 0; k < 5; k++) { z.mk.Cone(dmm, p + up * .3f + z.side * (-.3f + k * .15f) + z.front * .2f, .05f, .05f, 6); z.mk.Cone(dmm, p + up * .3f + z.side * (-.3f + k * .15f) + z.front * .2f, .05f, -.08f, 6); } halos.Add(p + up * .5f + z.front * .6f, .9f, Aqua, .5f); break;
                case "Danny Madden":
                    s = Song(z, "Facts of Life"); for (var k = 0; k < 7; k++) z.mk.Box(k == 3 ? SongGlow(s, Gold, .8f) : Paint(Neons[k % 5] * .6f), p + up * (.07f + k * .13f), new Vector3(.5f, .12f, .36f), rot * Quaternion.Euler(0, k * 11f - 30f, 0)); BoxCol(z.root, p + up * .5f, new Vector3(.6f, 1f, .5f), rot); z.mk.Prism(steel, p + z.side * .9f, up, .03f, 1.2f, 4); z.mk.Cone(lampM, p + z.side * .9f + up * 1.2f, .2f, .26f, 8); halos.Add(p + z.side * .9f + up * 1.2f, .9f, Gold, .5f); break;
                case "R. Kelly":
                    s = Song(z, "I Believe I Can Fly"); life.flySong = s; life.flyAt = p; var feather = SongGlow(s, new Color(1f, 1f, .96f), .6f); z.mk.Prism(steel, p - up * .2f, up, .05f, 1.6f, 5); foreach (var e in new[] { -1f, 1f }) for (var k = 0; k < 7; k++) z.mk.Box(feather, p + up * (1.9f + k * .12f) + z.side * e * (.2f + k * .24f), new Vector3(.24f, .9f - k * .08f, .04f), rot * Quaternion.Euler(0, 0, -e * (20f + k * 9f))); halos.Add(p + up * 2.2f, 2f, Cream, .3f); ArtistBits.PostCollider(z.root, p, 1.6f, .07f); break;
                default:
                    z.mk.Prism(steel, p, up, .04f, .9f, 5); z.mk.Prism(GlowM(Gold, 1.4f), p + up * .9f, up, .1f, .2f, 8); break;
            }
        }

        /// Garlands of marigolds strung over a Punjabi terrace.
        void Marigolds(Plot z, int phase)
        {
            var up = Vector3.up; var rng = new System.Random(phase + 5); var a = z.At(-3f, -4f) + up * 3f; var b = z.At(-3f, 4f) + up * 3f; var c = z.At(4f, 4.4f) + up * 2.6f; foreach (var g in new[] { z.At(-3f, -4f), z.At(-3f, 4f), z.At(4f, 4.4f) }) { z.mk.Prism(steel, g - up * .2f, up, .04f, 3.2f, 4); ArtistBits.PostCollider(z.root, g, 3f, .06f); }
            foreach (var (p0, p1) in new[] { (a, b), (b, c) }) for (var k = 0; k <= 18; k++) { var t = k / 18f; var p = Vector3.Lerp(p0, p1, t) - up * Mathf.Sin(t * Mathf.PI) * .6f; z.mk.Blob(GlowM(k % 2 == 0 ? new Color(1f, .6f, .1f) : new Color(1f, .82f, .15f), .7f), p, Vector3.one * .09f, rng, .2f); }
        }

        /// Two dhol drums on stands: play.
        void Dhol(Plot z, Vector3 g, string song)
        {
            var up = Vector3.up; var skin = SongGlow(song, Cream, .5f); for (var k = 0; k < 2; k++) { var p = g + z.side * (k * 1.1f - .55f); z.mk.Prism(steel, p, up, .03f, .6f, 4); z.mk.Prism(Paint(k == 0 ? Wine : new Color(.85f, .55f, .15f)), p + up * .9f - z.side * .34f, z.side, .3f, .68f, 12); z.mk.Prism(skin, p + up * .9f - z.side * .36f, z.side, .31f, .02f, 12); z.mk.Prism(skin, p + up * .9f + z.side * .34f, z.side, .31f, .02f, 12); } ArtistBits.PostCollider(z.root, g, 1.2f, .7f);
            string[] beat = { "Dha-ge-na, dha! The marigolds shake on their strings.", "The left hand rolls, the right hand cracks, and three roofs away somebody starts a bhangra shoulder.", "You hit the big end. The whole drowned city gets the beat a moment later, across the water." }; var bn = 0; Spot("The dhol", g + up * 1.2f, "Play the dhol", new Vector3(2f, 1.2f, 1f)).onUse = _ => { life.played = Time.time; Say1(g + up * 2.6f, beat[bn++ % beat.Length]); };
        }
    }
}
