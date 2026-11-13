using System.Collections.Generic;
using UnityEngine;
using static EverythingWorld.World.PlaceTools;

namespace EverythingWorld.World
{
    /// The Dubstep Shards' artists. Most have a floor of black stone ringed with small glowing shards, a slab of rock with
    /// their name cut into it and lit from inside, and one thing from their music; covers on a black slab with a crystal
    /// edge.
    /// - Virtual Riot: a garden of crystal flowers ("Stargarden"), a winged figure of crystal ("Neon Angel"), a moon ("Lunar"),
    ///   a great blade ("Edge"), a prism splitting light into colours ("CHROMA"), a black obelisk ("Blackout"), a portal ring
    ///   ("Different World") and a panel of scrolling light ("Rewrite").
    /// - Bassnectar: a tower of subwoofers ("Bass Head"), a melting clock ("Timestretch"), a radio mast ("Underground
    ///   Communication") and a wall of glowing graffiti ("Wildstyle Method").
    /// - The rest: a wrecking ball, a crystal castle, a crystal disco ball, glowing cracks, a giant teardrop, a crystal tree
    ///   with prayer flags, a bottle with a ship, a colour wheel and a scoreboard, a mint crystal, a mirrored double crystal,
    ///   a jagged green creature, a summit with a flag, a beam of light, a warped grid, arrows, an iridescent sphere.
    public partial class ShardKit
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
                Add("Skrillex", "Scary Monsters", "Skrillex"); Add("Bassnectar", "Bass Head", "Bassnectar"); Add("Virtual Riot", "CHROMA", "Virtual Riot"); Add("Virtual Riot", "Blackout", "Virtual Riot"); Add("Virtual Riot", "Lunar", "Virtual Riot");
                Add("NGHTMRE", "Teardrop", "NGHTMRE"); Add("Skybreak", "Into The Light", "Skybreak"); Add("Panda Eyes", "Colorblind", "Panda Eyes"); Add("Virtual Riot", "Stargarden", "Virtual Riot");
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
            area.artistName = a.name; area.stationId = a.stationId; area.radius = a.r; area.glowColor = Cyan;
            var z = new Plot { ctx = ctx, root = root, labels = labels, mk = new MeshKit(), solid = new MeshKit(), pictures = new MeshKit(), a = a, c = ctx.OnGround(a.x, a.z), front = front, side = Vector3.Cross(Vector3.up, front), area = area };
            var reach = Foot(a); var up = Vector3.up;
            z.mk.Prism(rockD, z.c - up * .3f, up, reach * .85f, .36f, 24); for (var k = 0; k < 14; k++) { var d = Quaternion.Euler(0, k * 360f / 14f, 0) * front; if (Vector3.Angle(d, front) < 30f) continue; Shard(root, z.mk, z.ctx.OnGround((z.c + d * reach * .8f).x, (z.c + d * reach * .8f).z), 1f + (k % 3) * .6f, .3f, d * .3f, k % 2 == 0 ? shardM : shardM2, z.solid); }
            switch (a.name)
            {
                case "Virtual Riot": Riot(z); break;
                case "Bassnectar": Nectar(z); break;
                default: Others(z, Slab(z)); break;
            }
            SongLamps(z);
            Board(z, z.At(reach * .6f, -reach * .45f - 1.5f), front);
            ArtistBits.Plaque(z.mk, z.solid, labels, a, z.At(reach * .6f + 1.5f, reach * .45f + 1.5f), front, iron, dark, Cyan, 1.6f);
            Lamp(ctx, z.mk, z.At(reach * .5f, reach * .7f), false);
            if (z.area.glowLight == null) z.area.glowLight = ArtistBits.PointLight(root, z.c + up * 4f + front * 3f, a.name.Length % 2 == 0 ? Cyan : Magenta, HasStation(a) ? 24f : 14f, 1.4f);
            area.glowRenderers = new Renderer[0];
            z.mk.Build("Area", root, false);
            z.solid.BuildCollider("Walls", root);
            z.pictures.Build("Covers", root, false);
            noGrass.Add(new Vector3(z.c.x, reach * .9f, z.c.z));
            var mark = new GameObject("Shot: " + a.name + " up close").transform; mark.SetParent(ctx.Root, false);
            var cam = z.At(reach * .85f, reach * .3f) + up * 2f; var look = z.At(-reach * .3f, 0) + up * 1.8f - cam; mark.SetPositionAndRotation(cam, Quaternion.LookRotation(look.normalized));
        }

        /// The covers on a black slab with a crystal edge.
        void Board(Plot z, Vector3 ground, Vector3 dir)
        {
            dir = Vector3.ProjectOnPlane(dir, Vector3.up).normalized; var rot = Quaternion.LookRotation(dir);
            var n = Mathf.Max(1, Mathf.Min(7, z.a.songList?.Length ?? 1)); const float size = .9f; var w = n * size * 1.25f + .6f;
            var c = ground + Vector3.up * 1.7f;
            z.mk.Box(shardM, c - Vector3.up * .5f - dir * .08f, new Vector3(w + .5f, size + 2f, .14f), rot); z.mk.Box(dark, c - Vector3.up * .5f - dir * .02f, new Vector3(w + .3f, size + 1.8f, .2f), rot); z.solid.Box(dark, c - Vector3.up * .5f, new Vector3(w + .5f, size + 2f, .3f), rot);
            z.mk.Box(Paint(new Color(.86f, .86f, .92f)), c - Vector3.up * .1f + dir * .09f, new Vector3(w, size + .9f, .04f), rot);
            ArtistBits.CoverWall(z.pictures, z.labels, z.ctx, z.a, c + dir * .12f + Vector3.up * .08f, dir, n, size, n, true, Ink);
        }

        /// A small glowing shard for each song without its own piece.
        void SongLamps(Plot z)
        {
            if (!SongIds.TryGetValue(z.a.name, out var list)) return;
            var up = Vector3.up; var k = 0; var seen = new HashSet<string>();
            foreach (var (_, id) in list)
            {
                if (z.lit.Contains(id) || !seen.Add(id)) continue;
                var p = z.At(Foot(z.a) * .5f - (k % 2) * 1.3f, Foot(z.a) * .2f + (k / 2) * 1.3f);
                z.mk.Prism(SongGlow(id, k % 2 == 0 ? Cyan : Magenta, 1.4f), p - up * .1f, up, .16f, 1.4f, 5, .05f); ArtistBits.PostCollider(z.root, p, 1.3f, .18f);
                k++;
            }
        }

        /// A slab of rock with their name cut into it and lit from inside.
        PlaceFrame Slab(Plot z)
        {
            var up = Vector3.up; var f = new PlaceFrame(z.At(-Foot(z.a) * .45f, 0), z.front);
            z.mk.Box(rock, f.P(0, 1.3f, 0), new Vector3(2.4f, 2.6f, .6f), f.r * Quaternion.Euler(0, 0, 4f)); BoxCol(z.root, f.P(0, 1.3f, 0), new Vector3(2.4f, 2.6f, .6f), f.r); z.mk.Box(shardM, f.P(0, 1.6f, .31f), new Vector3(2f, .5f, .02f), f.r); ArtistBits.Label(z.labels, z.a.name, f.P(0, 1.6f, .33f), f.Fwd, .17f, Ink, TextAnchor.MiddleCenter);
            return f;
        }

        // ---------------------------------------------------------------- Virtual Riot
        void Riot(Plot z)
        {
            var up = Vector3.up; var rng = new System.Random(2017); var fr = z.front; var sd = z.side; var rot = z.Rot;
            var star = Song(z, "Stargarden"); var angel = Song(z, "Neon Angel"); var lunar = Song(z, "Lunar"); var edge = Song(z, "Edge"); var chroma = Song(z, "CHROMA"); var black = Song(z, "Blackout"); var diff = Song(z, "Different World"); var rew = Song(z, "Rewrite");
            life.chromaSong = chroma; life.blackoutSong = black; life.lunarSong = lunar; life.starSong = star;
            // "Stargarden": crystal flowers
            for (var k = 0; k < 9; k++) { var fp = z.At(-4f + (k % 3) * 1.4f, -8f + (k / 3) * 1.4f); z.mk.Prism(Paint(new Color(.3f, .4f, .4f)), fp, up, .04f, .8f + (k % 2) * .4f, 4); for (var j = 0; j < 4; j++) z.mk.Prism(k == 4 ? SongGlow(star, Cyan, 1.4f) : (j % 2 == 0 ? shardM : shardM2), fp + up * (.8f + (k % 2) * .4f), (up + Quaternion.Euler(0, j * 90f + k * 20f, 0) * fr * .8f).normalized, .07f, .5f, 4, .05f); } ArtistBits.PostCollider(z.root, z.At(-2.6f, -6.6f), 1.3f, 2f);
            // "Neon Angel": a winged figure of crystal on a plinth
            var an = z.At(-7f, 0f); z.mk.Box(rock, an + up * .6f, new Vector3(1.4f, 1.2f, 1.4f), rot); BoxCol(z.root, an + up * 1.6f, new Vector3(1.4f, 3.2f, 1.4f), rot); z.mk.Prism(SongGlow(angel, Magenta, 1.4f), an + up * 1.2f, up, .3f, 1.6f, 5, .4f); z.mk.Prism(shardM2, an + up * 3f, up, .2f, .4f, 5); foreach (var e in new[] { -1f, 1f }) for (var k = 0; k < 4; k++) z.mk.Prism(shardM, an + up * 2.4f + sd * e * .2f, (sd * e + up * (.6f - k * .3f)).normalized, .08f, 1.6f - k * .2f, 4, .05f);
            // "Lunar": a moon; "Edge": a great blade; "CHROMA": a prism splitting light
            var mn = z.At(-Foot(z.a) * .9f, 5f) + up * 7f; z.mk.Prism(SongGlow(lunar, White, 1.2f), mn, fr, 2.4f, .2f, 26); z.mk.Prism(iron, mn - up * 7f - fr * .2f, up, .08f, 4.6f, 5); ArtistBits.PostCollider(z.root, mn - up * 7f, 4.6f, .2f);
            var bl = z.At(-2f, 7f); z.mk.Box(SongGlow(edge, White, 1f), bl + up * 2.4f, new Vector3(.08f, 4.8f, .8f), rot * Quaternion.Euler(0, 30f, 8f)); z.mk.Box(rock, bl + up * .3f, new Vector3(1.4f, .6f, 1.4f), rot); BoxCol(z.root, bl + up * 1.5f, new Vector3(1f, 3f, 1f), rot);
            var pr = z.At(3f, 4f); z.mk.Prism(SongGlow(chroma, White, .8f), pr, up, 1f, 2f, 3, 1f, 30f); z.mk.Box(GlowM(White, 1.6f), pr + up * 1f - sd * 3f, new Vector3(.06f, .06f, 4f), Quaternion.LookRotation(sd)); var cols = new[] { new Color(1f, .2f, .2f), new Color(1f, .6f, .2f), new Color(1f, 1f, .3f), new Color(.3f, 1f, .4f), new Color(.3f, .5f, 1f), new Color(.6f, .3f, 1f) }; for (var k = 0; k < 6; k++) { var dir = Quaternion.Euler(0, 20f + k * 6f, -4f + k * 1.6f) * sd; z.mk.Box(GlowM(cols[k], 1.6f), pr + up * 1f + dir * 2.4f, new Vector3(.05f, .05f, 4f), Quaternion.LookRotation(dir)); } ArtistBits.PostCollider(z.root, pr, 2f, 1f);
            // "Blackout": a black obelisk; "Different World": a portal ring; "Rewrite": a panel of scrolling light
            var ob = z.At(1f, -3f); z.mk.Prism(dark, ob, up, .9f, 5f, 4, .3f, 45f); z.mk.Box(SongGlow(black, Violet, 1f), ob + up * 2f + fr * .7f, new Vector3(.1f, 2f, .02f), rot); ArtistBits.PostCollider(z.root, ob, 5f, .9f);
            var pt = new PlaceFrame(z.At(5f, -6f), fr); for (var k = 0; k < 20; k++) { var a = k * Mathf.PI / 10f; z.mk.Box(k % 2 == 0 ? shardM : shardM2, pt.P(Mathf.Cos(a) * 1.6f, 1.9f + Mathf.Sin(a) * 1.6f, 0), new Vector3(.5f, .2f, .2f), pt.r * Quaternion.Euler(0, 0, a * Mathf.Rad2Deg + 90f)); } z.mk.Prism(SongGlow(diff, Violet, .8f), pt.P(0, 1.9f, 0), pt.Fwd, 1.4f, .02f, 24); foreach (var e in new[] { -1.6f, 1.6f }) ArtistBits.PostCollider(z.root, pt.P(e, 0, 0), 2f, .3f);
            var rp = new PlaceFrame(z.At(-5f, 10f), fr); z.mk.Box(dark, rp.P(0, 1.4f, 0), new Vector3(2f, 2.4f, .2f), rp.r); BoxCol(z.root, rp.P(0, 1.4f, 0), new Vector3(2f, 2.4f, .2f), rp.r); for (var k = 0; k < 8; k++) z.mk.Box(k == 0 ? SongGlow(rew, Lime, 1.4f) : GlowM(Lime, .9f), rp.P(-.6f + (k % 2) * .3f, .5f + k * .26f, .11f), new Vector3(.6f + (k * 13 % 5) * .14f, .06f, .01f), rp.r);
            Folk(z.At(7f, 2f), -fr, Resident.Pose.Stand, new[] { "Stand in the prism's light. You'll come out a different colour.", "When Blackout plays, the whole canyon goes dark. Then it doesn't.", "That ring goes to a different world. Probably. I haven't tried." }, Local());
            z.area.glowLight = ArtistBits.PointLight(z.root, z.c + up * 6f, Cyan, 28f, 1.8f);
            Shot(z.ctx, "virtual riot's crystal garden", z.At(Foot(z.a) * 1.1f, 4f) + up * 2.6f, z.c + up * 2f);
        }

        // ---------------------------------------------------------------- Bassnectar
        void Nectar(Plot z)
        {
            var up = Vector3.up; var rng = new System.Random(2007); var fr = z.front; var sd = z.side; var rot = z.Rot;
            var head = Song(z, "Bass Head"); var time = Song(z, "Timestretch"); var under = Song(z, "Underground"); var wild = Song(z, "Wildstyle"); life.bassSong = head;
            // "Bass Head": a tower of subwoofers
            var tw = z.At(-4f, 0f); for (var k = 0; k < 4; k++) { var sp = tw + up * (k * 1.6f + .8f); z.mk.Box(dark, sp, new Vector3(1.8f, 1.5f, 1.4f), rot * Quaternion.Euler(0, k * 8f, 0)); var wm = new MeshKit(); wm.Prism(k == 3 ? SongGlow(head, Magenta, 1.4f) : coneM, Vector3.zero, Vector3.forward, .6f, .1f, 18, .5f); var wg = wm.BuildSingle("Subwoofer", z.root, false); var wp = sp + (Quaternion.Euler(0, k * 8f, 0) * fr) * .71f; wg.transform.SetPositionAndRotation(wp, rot * Quaternion.Euler(0, k * 8f, 0)); life.woofers.Add((wg.transform, wp, k * .3f)); } BoxCol(z.root, tw + up * 3.2f, new Vector3(1.8f, 6.4f, 1.4f), rot);
            // "Timestretch": a melting clock draped over a rock
            var tc = z.At(3f, -5f); z.mk.Blob(rock, tc + up * .5f, new Vector3(1.2f, .8f, 1f), rng, .2f); for (var k = 0; k < 6; k++) z.mk.Box(k == 0 ? SongGlow(time, White, 1f) : white, tc + up * (1.2f - k * .2f) + fr * (.2f + k * .14f), new Vector3(1f - k * .06f, .06f, .3f), rot * Quaternion.Euler(k * 14f, 0, 0)); BoxCol(z.root, tc + up * .6f, new Vector3(2f, 1.2f, 1.6f), rot);
            // "Underground Communication": a radio mast with a blinking light
            var rm = z.At(-1f, -8f); for (var k = 0; k < 3; k++) { var leg = rm + Quaternion.Euler(0, k * 120f, 0) * fr * .8f; z.mk.Prism(iron, leg, (rm + up * 9f - leg).normalized, .05f, Vector3.Distance(rm + up * 9f, leg), 4); } z.mk.Blob(SongGlow(under, new Color(1f, .2f, .2f), 2f), rm + up * 9.2f, Vector3.one * .2f, rng, 0f); ArtistBits.PostCollider(z.root, rm, 3f, .9f);
            // "Wildstyle Method": a wall of glowing graffiti
            var gw = new PlaceFrame(z.At(-2f, 7f), fr); z.mk.Box(rock, gw.P(0, 1.4f, 0), new Vector3(5f, 2.8f, .4f), gw.r); BoxCol(z.root, gw.P(0, 1.4f, 0), new Vector3(5f, 2.8f, .4f), gw.r); var gc = new[] { Magenta, Cyan, Lime, Violet }; for (var k = 0; k < 12; k++) z.mk.Box(k == 0 ? SongGlow(wild, Lime, 1.4f) : GlowM(gc[k % 4], 1.1f), gw.P(-2f + (k * 7 % 12) * .36f, .6f + (k * 5 % 7) * .32f, .21f), new Vector3(.3f + (k % 3) * .2f, .14f + (k % 2) * .14f, .01f), gw.r * Quaternion.Euler(0, 0, (k * 37) % 60 - 30f));
            Folk(z.At(6f, 3f), -fr, Resident.Pose.Wave, new[] { "Bass head! Bass head!", "Stand in front of the tower. Feel your trousers flap.", "The clock melted the first night. Nobody fixed it." }, Local());
            z.area.glowLight = ArtistBits.PointLight(z.root, z.c + up * 5f, Magenta, 24f, 1.6f);
        }

        // ---------------------------------------------------------------- everyone else
        void Others(Plot z, PlaceFrame f)
        {
            var up = Vector3.up; var rot = z.Rot; var rng = new System.Random(z.a.name.Length * 7 + z.a.name[0]); var c = z.At(1f, -1.2f) + up * .06f; var fr = z.front; var sd = z.side;
            void Post(Vector3 at, float h, float r = .04f) { z.mk.Prism(iron, at - up * .2f, up, r, h + .2f, 5); ArtistBits.PostCollider(z.root, at, h, r + .03f); }
            string Any() => SongIds.TryGetValue(z.a.name, out var l) && l.Length > 0 ? Song(z, l[0].title) : null;
            switch (z.a.name)
            {
                case "Black Tiger Sex Machine": { var s = Any(); var fr2 = c - fr * .6f; foreach (var e in new[] { -1.2f, 1.2f }) Post(fr2 + sd * e, 3.4f, .08f); z.mk.Box(iron, fr2 + up * 3.4f, new Vector3(2.6f, .1f, .1f), Quaternion.LookRotation(sd)); z.mk.Prism(iron, fr2 + up * 3.4f, -up, .02f, 1.4f, 4); z.mk.Blob(SongGlow(s, Magenta, .8f), fr2 + up * 1.6f, Vector3.one * .5f, rng, 0f); break; }
                case "Careless Castle": { var s = Any(); for (var k = 0; k < 4; k++) { var tp = c + Quaternion.Euler(0, k * 90f + 45f, 0) * fr * .9f; z.mk.Prism(k == 0 ? SongGlow(s, Cyan, 1.2f) : shardM, tp, up, .3f, 1.4f + (k % 2) * .4f, 5); z.mk.Cone(shardM2, tp + up * (1.4f + (k % 2) * .4f), .34f, .5f, 5); } z.mk.Box(shardM, c + up * .5f, new Vector3(1.2f, 1f, 1.2f), rot); BoxCol(z.root, c + up * .8f, new Vector3(2f, 1.6f, 2f), rot); break; }
                case "Eccentric": { var s = Any(); Post(c, 2f, .03f); var db = new MeshKit(); db.Prism(SongGlow(s, White, 1.2f), Vector3.zero, Vector3.up, .4f, .5f, 6, .05f); db.Prism(SongGlow(s, White, 1.2f), Vector3.zero, Vector3.down, .4f, .5f, 6, .05f); var dg = db.BuildSingle("Disco crystal", z.root, false); dg.transform.position = c + up * 2.4f; life.spinners.Add((dg.transform, c + up * 2.4f)); break; }
                case "Freestylers": { var s = Any(); for (var k = 0; k < 8; k++) { var a = c + Quaternion.Euler(0, k * 45f, 0) * fr * .2f; var b = c + Quaternion.Euler(0, k * 45f + 10f, 0) * fr * (1.6f + (k % 3) * .4f); z.mk.Box(k == 0 ? SongGlow(s, Magenta, 1.6f) : shardM2, (a + b) / 2f + up * .03f, new Vector3(.1f, .02f, (b - a).magnitude), Quaternion.LookRotation(b - a)); } break; }
                case "NGHTMRE": { var s = Any(); life.tearSong = s; var td = c + up * 1.6f; z.mk.Blob(SongGlow(s, new Color(.3f, .6f, 1f), 1.2f), td, new Vector3(.6f, .7f, .6f), rng, 0f); z.mk.Cone(SongGlow(s, new Color(.3f, .6f, 1f), 1.2f), td + up * .4f, .5f, 1.1f, 10); z.mk.Prism(rock, c, up, .4f, .7f, 6); ArtistBits.PostCollider(z.root, c, 2.8f, .7f); break; }
                case "Of The Trees": { var dol = Song(z, "Dolori"); var horse = Song(z, "Windhorse"); var prev = c; for (var k = 0; k < 4; k++) { var dir = (up + sd * Mathf.Sin(k) * .3f).normalized; z.mk.Prism(shardM, prev, dir, .2f - k * .03f, 1f, 5); prev += dir; } for (var k = 0; k < 5; k++) z.mk.Prism(k == 0 ? SongGlow(dol, Cyan, 1.4f) : shardM, prev, (up + Quaternion.Euler(0, k * 72f, 0) * fr * .9f).normalized, .1f, 1.2f, 4, .05f); ArtistBits.PostCollider(z.root, c, 3f, .3f); var a0 = c + sd * 2f + up * .1f; Post(a0, 2.4f, .03f); var a1 = c - sd * 1.6f - fr * 1f; Post(a1, 2f, .03f); var cols = new[] { new Color(.3f, .5f, 1f), White, new Color(1f, .3f, .3f), new Color(.3f, 1f, .4f), new Color(1f, .9f, .3f) }; for (var k = 0; k < 8; k++) { var p = Vector3.Lerp(a0 + up * 2.4f, a1 + up * 2f, (k + .5f) / 8f) - up * Mathf.Sin((k + .5f) / 8f * Mathf.PI) * .4f; z.mk.Box(k == 0 ? SongGlow(horse, cols[0], 1f) : GlowM(cols[k % 5], .9f), p - up * .2f, new Vector3(.3f, .36f, .02f), Quaternion.LookRotation(fr)); } break; }
                case "Omnitica": { var s = Any(); var bt = c + up * .5f; z.mk.Prism(GlowM(new Color(.4f, .8f, .6f), .4f), bt - sd * .9f, sd, .45f, 1.4f, 10); z.mk.Prism(GlowM(new Color(.4f, .8f, .6f), .4f), bt + sd * .5f, sd, .2f, .6f, 8); z.mk.Prism(Paint(new Color(.5f, .36f, .26f)), bt + sd * 1.1f, sd, .16f, .14f, 8); z.mk.Box(SongGlow(s, Lime, 1.4f), bt - sd * .2f + up * .1f, new Vector3(.02f, .5f, .4f), Quaternion.LookRotation(fr)); BoxCol(z.root, bt, new Vector3(2.6f, 1f, 1f), Quaternion.LookRotation(sd)); break; }
                case "Panda Eyes": { var cb = Song(z, "Colorblind"); var hs = Song(z, "Highscore"); life.colorSong = cb; var cw = c - sd * .8f + up * 1.6f; for (var k = 0; k < 8; k++) z.mk.Tri(k == 0 ? SongGlow(cb, k % 2 == 0 ? new Color(.2f, .4f, 1f) : new Color(1f, .9f, .2f), 1.4f) : GlowM(k % 2 == 0 ? new Color(.2f, .4f, 1f) : new Color(1f, .9f, .2f), 1.2f), cw, cw + Quaternion.AngleAxis(k * 45f, fr) * up, cw + Quaternion.AngleAxis(k * 45f + 45f, fr) * up, cw - fr); Post(c - sd * .8f, .7f, .05f); var sb = c + sd * 1.4f; z.mk.Box(dark, sb + up * 1.2f, new Vector3(1.4f, .7f, .2f), rot); for (var k = 0; k < 5; k++) z.mk.Box(k == 0 ? SongGlow(hs, Lime, 1.4f) : GlowM(Lime, 1f), sb + up * 1.2f + fr * .11f + sd * (-.5f + k * .25f), new Vector3(.16f, .4f, .01f), rot); Post(sb, .9f, .05f); break; }
                case "Prismatic": { var s = Any(); Shard(z.root, z.mk, c, 3f, .7f, Vector3.zero, SongGlow(s, new Color(.4f, 1f, .7f), 1.4f), z.solid); Shard(z.root, z.mk, c + sd * .8f, 1.6f, .4f, sd * .4f, GlowM(new Color(.4f, 1f, .7f), 1f), z.solid); break; }
                case "Quartzone": { var s = Any(); var qc = c + up * 1.8f; z.mk.Prism(SongGlow(s, White, 1.2f), qc, up, .5f, 1.4f, 6, .05f); z.mk.Prism(SongGlow(s, White, 1.2f), qc, -up, .5f, 1.4f, 6, .05f); z.mk.Prism(rock, c, up, .3f, .4f, 6); ArtistBits.PostCollider(z.root, c, 3.2f, .5f); break; }
                case "Skrillex": { var sm = Song(z, "Scary Monsters"); var sum = Song(z, "Summit"); life.monsterSong = sm; var mc = c; z.mk.Blob(SongGlow(sm, Lime, 1f), mc + up * 1f, new Vector3(.8f, .9f, .6f), rng, .3f); for (var k = 0; k < 8; k++) z.mk.Cone(GlowM(Lime, 1.2f), mc + up * (1.2f + (k % 3) * .3f) + Quaternion.Euler(0, k * 45f, 0) * fr * .6f, .12f, .5f, 4); foreach (var e in new[] { -.25f, .25f }) z.mk.Blob(GlowM(Magenta, 2f), mc + up * 1.4f + fr * .55f + sd * e, Vector3.one * .1f, rng, 0f); ArtistBits.PostCollider(z.root, mc, 2f, .8f); var pk = c - fr * 1.6f + sd * 2f; z.mk.Prism(rock, pk, up, 1f, 2.4f, 5, .05f); z.mk.Prism(iron, pk + up * 2.3f, up, .02f, .8f, 4); z.mk.Tri(SongGlow(sum, Cyan, 1.4f), pk + up * 3.1f, pk + up * 2.8f, pk + up * 2.95f + sd * .5f, pk + fr); ArtistBits.PostCollider(z.root, pk, 2.4f, 1f); break; }
                case "Skybreak": { var s = Any(); life.lightSong = s; z.mk.Prism(SongGlow(s, White, .5f), c, up, .6f, 30f, 12, 1.6f, 0f, false); z.mk.Prism(rock, c - up * .1f, up, .8f, .2f, 12); break; }
                case "Subtronics": { var s = Any(); for (var x = -3; x <= 3; x++) for (var y = -3; y <= 3; y++) { var d = Mathf.Sqrt(x * x + y * y); var gp = c + sd * x * .5f + fr * y * .5f + up * (.1f + Mathf.Max(0f, 1.2f - d * .4f) * -.0f + Mathf.Exp(-d * .6f) * 1.2f); z.mk.Box(x == 0 && y == 0 ? SongGlow(s, Violet, 1.6f) : GlowM(Violet, .9f), gp, new Vector3(.06f, .06f, .06f), rot); } break; }
                case "Teminite": { var s = Any(); for (var k = 0; k < 3; k++) { var ap = c + fr * (k - 1) * .7f + up * 1.2f; z.mk.Tri(k == 1 ? SongGlow(s, Lime, 1.4f) : GlowM(Lime, 1.1f), ap + fr * .4f, ap - fr * .2f + sd * .5f, ap - fr * .2f - sd * .5f, ap - up); } Post(c, 1f, .04f); break; }
                case "ZEOL": { var s = Any(); var sp = new MeshKit(); sp.Blob(SongGlow(s, new Color(.7f, .9f, 1f), 1f), Vector3.zero, Vector3.one * .7f, rng, 0f); for (var k = 0; k < 6; k++) sp.Blob(GlowM(new[] { Cyan, Magenta, Lime, Violet }[k % 4], 1.2f), Quaternion.Euler(k * 60f, k * 40f, 0) * Vector3.forward * .7f, Vector3.one * .16f, rng, 0f); var sg = sp.BuildSingle("Iridescent sphere", z.root, false); sg.transform.position = c + up * 1.8f; life.spinners.Add((sg.transform, c + up * 1.8f)); z.mk.Prism(rock, c, up, .4f, .6f, 6); ArtistBits.PostCollider(z.root, c, 2.5f, .7f); break; }
                default: { var s = Any(); Shard(z.root, z.mk, c, 2.4f, .5f, Vector3.zero, SongGlow(s, Cyan, 1.4f), z.solid); break; }
            }
        }
    }
}
