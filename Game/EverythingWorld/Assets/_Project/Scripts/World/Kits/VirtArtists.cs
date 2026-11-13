using System.Collections.Generic;
using UnityEngine;
using static EverythingWorld.World.PlaceTools;

namespace EverythingWorld.World
{
    /// Mount Virtuoso's artists. In the calm foothills: a round of mossy flat stones with a carved standing stone bearing
    /// their name. On the mountain: the guitar heroes, each on a ledge of the climb with an amp stack, a giant guitar and
    /// one thing from their song. Covers on a slab of slate set in a wooden frame.
    /// - Ólafur Arnalds: an upright piano under a little wooden shelter by the water, a window with a lamp in it ("Near
    ///   Light"), two chairs side by side ("saman") and a curl of stones like a breaking wave ("Öldurót").
    /// - Enya: a ring of standing stones lit sea-blue ("Caribbean Blue") and an hourglass ("Only Time"). Vangelis: a
    ///   running track's white lanes and starting blocks. Pat's Soundhouse: a giant egg in a nest.
    /// - The guitar heroes: white cliffs and birds (Eric Johnson), two streaks of light chasing each other (Nili Brosh), a
    ///   chrome guitar and a rocket trail (Joe Satriani), a dragon carved in the rock (Richie Kotzen), and at the top, a
    ///   white bucket on a pole and a ghost of light (Buckethead).
    public partial class VirtKit
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
                Add("Enya", "Caribbean Blue", "Enya"); Add("Enya", "Only Time", "Enya"); Add("Vangelis", "Titles", "Vangelis"); Add("Ólafur Arnalds", "Near Light", "Ólafur Arnalds");
                Add("Eric Johnson", "Cliffs Of Dover", "Eric Johnson"); Add("Buckethead", "Ghost", "Buckethead"); Add("Richie Kotzen", "Chase the Dragon", "Richie Kotzen"); Add("Joe Satriani", "Raspberry", "Joe Satriani");
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
            if (rock == null) MakeMaterials();
            var root = new GameObject("Artist: " + a.name).transform; root.SetParent(ctx.Root, false);
            var front = facing.TryGetValue(a.name, out var f) ? f : new Vector3(center.x - a.x, 0, center.z - a.z).normalized;
            if (front.sqrMagnitude < .01f) front = Vector3.forward;
            var labels = new GameObject("Labels").transform; labels.SetParent(root, false);
            var area = root.gameObject.AddComponent<ArtistArea>();
            area.artistName = a.name; area.stationId = a.stationId; area.radius = a.r; area.glowColor = IsShredder(a.name) ? Ember : Glow;
            var z = new Plot { ctx = ctx, root = root, labels = labels, mk = new MeshKit(), solid = new MeshKit(), pictures = new MeshKit(), a = a, c = ctx.OnGround(a.x, a.z), front = front, side = Vector3.Cross(Vector3.up, front), area = area };
            var reach = Foot(a); var up = Vector3.up;
            if (IsShredder(a.name)) Ledge(z); else if (a.name == "Ólafur Arnalds") Olafur(z); else Others(z, Stones(z));
            SongLamps(z);
            Board(z, z.At(reach * .6f, -reach * .45f - 1.5f), front);
            ArtistBits.Plaque(z.mk, z.solid, labels, a, z.At(reach * .6f + 1.5f, reach * .45f + 1.5f), front, wood, Paint(Slate), Snow, 1.6f);
            if (!IsShredder(a.name)) Lamp(ctx, z.mk, z.At(reach * .5f, reach * .7f), false);
            if (z.area.glowLight == null) z.area.glowLight = ArtistBits.PointLight(root, z.c + up * 4f + front * 3f, IsShredder(a.name) ? Ember : Glow, HasStation(a) ? 22f : 14f, 1.4f);
            area.glowRenderers = new Renderer[0];
            z.mk.Build("Area", root, false);
            z.solid.BuildCollider("Walls", root);
            z.pictures.Build("Covers", root, false);
            noGrass.Add(new Vector3(z.c.x, reach * .9f, z.c.z));
            var mark = new GameObject("Shot: " + a.name + " up close").transform; mark.SetParent(ctx.Root, false);
            var cam = z.At(reach * .85f, reach * .3f) + up * 2f; var look = z.At(-reach * .3f, 0) + up * 1.8f - cam; mark.SetPositionAndRotation(cam, Quaternion.LookRotation(look.normalized));
        }

        /// The covers on a slab of slate in a wooden frame.
        void Board(Plot z, Vector3 ground, Vector3 dir)
        {
            dir = Vector3.ProjectOnPlane(dir, Vector3.up).normalized; var rot = Quaternion.LookRotation(dir);
            var n = Mathf.Max(1, Mathf.Min(7, z.a.songList?.Length ?? 1)); const float size = .9f; var w = n * size * 1.25f + .6f;
            var c = ground + Vector3.up * 1.7f;
            z.mk.Box(wood, c - Vector3.up * .5f - dir * .08f, new Vector3(w + .5f, size + 2f, .16f), rot); z.mk.Box(rockD, c - Vector3.up * .5f - dir * .02f, new Vector3(w + .3f, size + 1.8f, .2f), rot); z.solid.Box(wood, c - Vector3.up * .5f, new Vector3(w + .5f, size + 2f, .3f), rot);
            z.mk.Box(Paint(new Color(.86f, .86f, .84f)), c - Vector3.up * .1f + dir * .09f, new Vector3(w, size + .9f, .04f), rot);
            ArtistBits.CoverWall(z.pictures, z.labels, z.ctx, z.a, c + dir * .12f + Vector3.up * .08f, dir, n, size, n, true, Ink);
        }

        /// A little lantern on a stone for each song without its own piece.
        void SongLamps(Plot z)
        {
            if (!SongIds.TryGetValue(z.a.name, out var list)) return;
            var up = Vector3.up; var k = 0; var seen = new HashSet<string>();
            foreach (var (_, id) in list)
            {
                if (z.lit.Contains(id) || !seen.Add(id)) continue;
                var p = z.At(Foot(z.a) * .55f - (k % 2) * 1.3f, Foot(z.a) * .2f + (k / 2) * 1.3f);
                z.mk.Blob(rockD, p + up * .3f, new Vector3(.4f, .4f, .4f), new System.Random(k), .2f); z.mk.Prism(SongGlow(id, IsShredder(z.a.name) ? Ember : Glow, 1.6f), p + up * .6f, up, .1f, .24f, 6); ArtistBits.PostCollider(z.root, p, .9f, .3f);
                k++;
            }
        }

        /// A round of mossy flat stones with a carved standing stone bearing their name.
        PlaceFrame Stones(Plot z)
        {
            var up = Vector3.up; var f = new PlaceFrame(z.At(-Foot(z.a) * .5f, 0), z.front); var rng = new System.Random(z.a.name.Length);
            for (var k = 0; k < 10; k++) z.mk.Prism(k % 2 == 0 ? rock : rockD, z.c + Quaternion.Euler(0, k * 36f, 0) * z.front * (1.5f + (k % 3) * 1.6f) - up * .25f, up, .9f + (k % 2) * .4f, .32f, 7, 1f, k * 20f);
            z.mk.Box(rock, f.P(0, 1.2f, 0), new Vector3(1.2f, 2.4f, .5f), f.r * Quaternion.Euler(0, 0, 3f)); BoxCol(z.root, f.P(0, 1.2f, 0), new Vector3(1.2f, 2.4f, .5f), f.r); z.mk.Blob(GlowM(Moss, .3f), f.P(0, 2.4f, 0), new Vector3(.7f, .2f, .3f), rng, .2f); ArtistBits.Label(z.labels, z.a.name, f.P(0, 1.6f, .26f), f.Fwd, .13f, Ink, TextAnchor.MiddleCenter);
            return f;
        }

        // ---------------------------------------------------------------- the guitar heroes' ledges
        void Ledge(Plot z)
        {
            var up = Vector3.up; var rng = new System.Random(z.a.name.Length * 11); var fr = z.front; var sd = z.side; var rot = z.Rot;
            z.mk.Prism(rockD, z.c - up * .3f, up, 8f, .36f, 16); for (var k = 0; k < 6; k++) z.mk.Blob(rock, z.c - fr * 6.6f + sd * (k - 2.5f) * 2.2f + up * .6f, new Vector3(1.2f, 1f, 1f), rng, .3f);
            // the amp stack, its lights glowing
            var ap = z.At(-4f, 3f); var amp = new PlaceFrame(ap, fr); for (var k = 0; k < 3; k++) { var cp = amp.P(0, .55f + k * 1.05f, 0); z.mk.Box(dark, cp, new Vector3(1.6f, 1f, .8f), amp.r); z.mk.Box(Paint(new Color(.26f, .24f, .22f)), cp + amp.Fwd * .41f, new Vector3(1.4f, .84f, .02f), amp.r); } z.mk.Box(ampM, amp.P(0, 3.2f, .41f), new Vector3(1f, .08f, .02f), amp.r); BoxCol(z.root, amp.P(0, 1.6f, 0), new Vector3(1.6f, 3.2f, .8f), amp.r);
            var col = z.a.name == "Joe Satriani" ? new Color(.8f, .82f, .86f) : z.a.name == "Buckethead" ? new Color(.9f, .9f, .9f) : z.a.name == "Eric Johnson" ? new Color(.86f, .6f, .3f) : z.a.name == "Nili Brosh" ? new Color(.2f, .3f, .7f) : new Color(.7f, .1f, .1f);
            var s = SongIds.TryGetValue(z.a.name, out var l) && l.Length > 0 ? Song(z, l[0].title) : null;
            Guitar(z.mk, z.root, z.At(-3f, -3f), fr, col, SongGlow(s, Lightning, 1.6f), 1.2f);
            switch (z.a.name)
            {
                case "Eric Johnson": { life.cliffsSong = s; var cl = z.At(1f, -5f); for (var k = 0; k < 3; k++) z.mk.Box(white, cl + up * (1f + k * .4f) + sd * k * .8f, new Vector3(1.2f, 2f + k * .8f, 1f), rot * Quaternion.Euler(0, k * 12f, 0)); BoxCol(z.root, cl + up * 1.4f + sd * .8f, new Vector3(3f, 2.8f, 1.4f), rot); for (var k = 0; k < 5; k++) { var b = new MeshKit(); b.Tri(white, Vector3.zero, new Vector3(-.3f, .08f, -.05f), new Vector3(-.1f, 0, -.1f), Vector3.down); b.Tri(white, Vector3.zero, new Vector3(.3f, .08f, -.05f), new Vector3(.1f, 0, -.1f), Vector3.down); var bg = b.BuildSingle("Bird", z.root, false); life.birds.Add((bg.transform, cl + up * 5f, k * 1.2f)); } break; }
                case "Nili Brosh": { for (var k = 0; k < 2; k++) { var sm = new MeshKit(); sm.Prism(GlowM(k == 0 ? new Color(.3f, .6f, 1f) : Ember, 1.8f), Vector3.zero, Vector3.back, .08f, 1.6f, 6, .1f); var sg = sm.BuildSingle("Streak", z.root, false); life.chasers.Add((sg.transform, z.c + up * 2.2f, k * Mathf.PI)); } break; }
                case "Joe Satriani": { var rk = z.At(2f, -4f); z.mk.Prism(white, rk + up * .4f, up, .4f, 2f, 10); z.mk.Cone(Paint(new Color(.8f, .2f, .2f)), rk + up * 2.4f, .4f, .8f, 10); for (var k = 0; k < 6; k++) z.mk.Blob(GlowM(Ember, 1.4f - k * .2f), rk + up * (.2f - k * .1f) - fr * (k * .3f), Vector3.one * (.3f - k * .03f), rng, .2f); ArtistBits.PostCollider(z.root, rk, 3f, .5f); break; }
                case "Richie Kotzen": { var dg = z.At(1f, -5f) ; z.mk.Blob(rock, dg + up * 1.2f, new Vector3(1.4f, 1.4f, 1.2f), rng, .2f); z.mk.Box(rock, dg + up * 2f + fr * .8f, new Vector3(.7f, .6f, 1.4f), rot); foreach (var e in new[] { -.25f, .25f }) { z.mk.Blob(GlowM(Ember, 2f), dg + up * 2.2f + fr * 1.4f + sd * e, Vector3.one * .1f, rng, 0f); z.mk.Cone(rock, dg + up * 2.4f + fr * .4f + sd * e, .14f, .6f, 4); } z.mk.Blob(GlowM(Ember, 1.6f), dg + up * 1.9f + fr * 1.7f, new Vector3(.3f, .14f, .2f), rng, .2f); ArtistBits.PostCollider(z.root, dg, 2.6f, 1.4f); life.fires.Add(dg + up * 2f + fr * 1.8f); break; }
                case "Buckethead": { life.ghostSong = s; var bp = z.At(1f, -5f); z.mk.Prism(iron, bp, up, .05f, 2.6f, 5); z.mk.Prism(white, bp + up * 2.6f, up, .34f, .5f, 14, 1.2f); z.mk.Box(Paint(new Color(.8f, .2f, .2f)), bp + up * 2.9f, new Vector3(.4f, .1f, .02f), rot); ArtistBits.PostCollider(z.root, bp, 2.6f, .2f); var gm = new MeshKit(); gm.Blob(GlowM(new Color(.86f, .9f, 1f), 1f), Vector3.zero, new Vector3(.5f, .8f, .4f), rng, .2f); gm.Prism(GlowM(new Color(.86f, .9f, 1f), .6f), Vector3.down * .6f, Vector3.down, .5f, .8f, 8, .05f); var gg = gm.BuildSingle("Ghost", z.root, false); life.ghosts.Add((gg.transform, bp + up * 3.2f + fr * 1.6f)); break; }
            }
            var spot = new GameObject("Ledge spotlight"); spot.transform.SetParent(z.root, false); spot.transform.SetPositionAndRotation(z.c + fr * 5f + up * 6f, Quaternion.LookRotation((z.c - (z.c + fr * 5f + up * 6f)).normalized)); var lt = spot.AddComponent<Light>(); lt.type = LightType.Spot; lt.range = 16f; lt.spotAngle = 50f; lt.intensity = 3f; lt.color = new Color(1f, .8f, .6f);
            Folk(z.At(0, 2f), fr, Resident.Pose.Wave, new[] { "Faster! Faster than that!", "Each ledge up, someone plays a little quicker.", "Don't look down. Look at the fretboard." }, Local());
            z.area.glowLight = ArtistBits.PointLight(z.root, z.c + up * 3f + fr * 2f, Ember, 16f, 1.6f);
        }

        // ---------------------------------------------------------------- Ólafur Arnalds
        void Olafur(Plot z)
        {
            var up = Vector3.up; var rng = new System.Random(2013); var fr = z.front; var sd = z.side; var rot = z.Rot;
            var near = Song(z, "Near Light"); var saman = Song(z, "saman"); var old = Song(z, "Öldurót"); life.nearSong = near;
            z.mk.Prism(rockD, z.c - up * .3f, up, Foot(z.a) * .85f, .34f, 22); z.mk.Prism(GlowM(Moss, .2f), z.c + up * .05f, up, Foot(z.a) * .8f, .02f, 22);
            // an upright piano under a little wooden shelter
            var sh = new PlaceFrame(z.At(-5f, 0), fr); foreach (var (x, q) in new[] { (-1.6f, -1f), (1.6f, -1f), (1.6f, 1f), (-1.6f, 1f) }) { z.mk.Box(wood, sh.P(x, 1.3f, q), new Vector3(.14f, 2.6f, .14f), sh.r); ArtistBits.PostCollider(z.root, sh.P(x, 0, q), 2.6f, .1f); } z.mk.Prism(Paint(new Color(.3f, .26f, .22f)), sh.P(0, 2.6f, 0), up, 2.4f, .8f, 4, .05f, 45f);
            var pw = Paint(new Color(.24f, .16f, .12f)); z.mk.Box(pw, sh.P(0, .7f, -.6f), new Vector3(1.6f, 1.4f, .6f), sh.r); z.mk.Box(white, sh.P(0, .82f, -.2f), new Vector3(1.4f, .03f, .18f), sh.r); BoxCol(z.root, sh.P(0, .7f, -.5f), new Vector3(1.6f, 1.4f, .8f), sh.r); z.mk.Prism(wood, sh.P(0, 0, .6f), up, .22f, .5f, 10); Interact.Seat(tlife.transform, sh.P(0, .52f, .6f), -sh.Fwd, "Play quietly").seatHeight = .5f; z.mk.Box(GlowM(new Color(.3f, .26f, .2f), .2f), sh.P(0, 1.05f, -.29f), new Vector3(1.4f, .5f, .02f), sh.r);
            // "Near Light": a window frame with a lamp in it; "saman": two chairs side by side; "Öldurót": stones curled like a wave
            var wf = new PlaceFrame(z.At(1f, -6f), fr); z.mk.Box(wood, wf.P(0, 1.4f, 0), new Vector3(1.6f, 2f, .14f), wf.r); z.mk.Box(SongGlow(near, Glow, 1.2f), wf.P(0, 1.4f, .02f), new Vector3(1.3f, 1.7f, .02f), wf.r); z.mk.Box(wood, wf.P(0, 1.4f, .04f), new Vector3(.06f, 1.7f, .02f), wf.r); z.mk.Box(wood, wf.P(0, 1.4f, .04f), new Vector3(1.3f, .06f, .02f), wf.r); BoxCol(z.root, wf.P(0, 1.2f, 0), new Vector3(1.6f, 2.4f, .3f), wf.r); halos.Add(wf.P(0, 1.4f, .6f), 1.6f, Glow, .35f);
            foreach (var e in new[] { -.4f, .4f }) { var cp = z.At(2f, 5f) + sd * e; z.mk.Box(wood, cp + up * .45f, new Vector3(.45f, .05f, .45f), rot); z.mk.Box(wood, cp + up * .8f + fr * .2f, new Vector3(.45f, .7f, .05f), rot); Interact.Seat(tlife.transform, cp + up * .5f, -fr, "Sit together").seatHeight = .48f; } BoxCol(z.root, z.At(2f, 5f) + up * .4f, new Vector3(1.2f, .8f, .6f), rot); z.mk.Blob(SongGlow(saman, Glow, 1.2f), z.At(2f, 5f) + up * 1.4f, Vector3.one * .08f, rng, 0f);
            for (var k = 0; k < 14; k++) { var a = k / 13f; var wp = z.At(5f - a * 3f, -2f + a * 4f) + up * (Mathf.Sin(a * Mathf.PI) * 1.4f + .2f) + fr * (Mathf.Sin(a * Mathf.PI * 1.5f) * .6f); z.mk.Blob(k == 7 ? SongGlow(old, new Color(.5f, .7f, .8f), .8f) : (k % 2 == 0 ? rock : rockD), wp, new Vector3(.5f, .4f, .4f), rng, .2f); } ArtistBits.PostCollider(z.root, z.At(3.5f, 0f), 1.6f, 1.6f);
            Folk(z.At(4f, 3f), -fr, Resident.Pose.Stand, new[] { "Play quietly. The mountain is listening.", "Two chairs. Always two.", "The stones roll like the sea when the song comes on." }, Local());
            z.area.glowLight = ArtistBits.PointLight(z.root, z.c + up * 4f, Glow, 22f, 1.4f);
        }

        // ---------------------------------------------------------------- everyone else in the foothills
        void Others(Plot z, PlaceFrame f)
        {
            var up = Vector3.up; var rot = z.Rot; var rng = new System.Random(z.a.name.Length * 7 + z.a.name[0]); var c = z.At(1f, -1f) + up * .05f; var fr = z.front; var sd = z.side;
            string Any() => SongIds.TryGetValue(z.a.name, out var l) && l.Length > 0 ? Song(z, l[0].title) : null;
            switch (z.a.name)
            {
                case "Enya": { var blue = Song(z, "Caribbean Blue"); var only = Song(z, "Only Time"); life.blueSong = blue; for (var k = 0; k < 7; k++) { var sp = c + Quaternion.Euler(0, k * 51.4f, 0) * fr * 2.2f; z.mk.Box(rock, sp + up * .9f, new Vector3(.5f, 1.8f + (k % 3) * .3f, .4f), Quaternion.LookRotation(sp - c)); z.mk.Box(SongGlow(k == 0 ? blue : null, new Color(.3f, .6f, 1f), .9f), sp + up * 1.2f - (sp - c).normalized * .21f, new Vector3(.2f, .5f, .02f), Quaternion.LookRotation(sp - c)); ArtistBits.PostCollider(z.root, sp, 1.8f, .35f); } var hg = c + sd * 3.6f; z.mk.Box(wood, hg + up * .05f, new Vector3(.8f, .1f, .8f), rot); z.mk.Box(wood, hg + up * 1.45f, new Vector3(.8f, .1f, .8f), rot); z.mk.Prism(GlowM(new Color(.8f, .9f, 1f), .4f), hg + up * .1f, up, .34f, .65f, 10, .1f); z.mk.Prism(GlowM(new Color(.8f, .9f, 1f), .4f), hg + up * 1.4f, -up, .34f, .65f, 10, .1f); z.mk.Prism(SongGlow(only, Glow, 1.2f), hg + up * .12f, up, .22f, .12f, 10, .3f); BoxCol(z.root, hg + up * .75f, new Vector3(.8f, 1.5f, .8f), rot); break; }
                case "Vangelis": { var s = Any(); life.titlesSong = s; for (var k = 0; k < 5; k++) z.mk.Box(white, c + sd * (k - 2f) * 1.2f + fr * 2f + up * .03f, new Vector3(.08f, .02f, 7f), rot); for (var k = 0; k < 4; k++) { var bp = c + sd * (k - 1.5f) * 1.2f - fr * 1.2f; z.mk.Box(k == 1 ? SongGlow(s, Glow, 1.2f) : Paint(new Color(.8f, .2f, .2f)), bp + up * .15f, new Vector3(.5f, .1f, .3f), rot * Quaternion.Euler(-40f, 0, 0)); } break; }
                case "Pat's Soundhouse": { var s = Any(); var ns = c; for (var k = 0; k < 14; k++) z.mk.Box(wood, ns + up * .3f + Quaternion.Euler(0, k * 26f, 0) * fr * .8f, new Vector3(.06f, .06f, 1f), Quaternion.Euler(0, k * 26f + 80f, k * 10f)); z.mk.Blob(SongGlow(s, new Color(.96f, .92f, .82f), .8f), ns + up * 1f, new Vector3(.6f, .8f, .6f), rng, 0f); ArtistBits.PostCollider(z.root, ns, 1.8f, .9f); break; }
                default: { var s = Any(); z.mk.Blob(rockD, c + up * .3f, Vector3.one * .4f, rng, .2f); z.mk.Prism(SongGlow(s, Glow, 1.6f), c + up * .6f, up, .12f, .3f, 6); break; }
            }
        }
    }
}
