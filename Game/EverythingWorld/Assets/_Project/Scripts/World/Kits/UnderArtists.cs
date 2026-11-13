using System.Collections.Generic;
using UnityEngine;
using static EverythingWorld.World.PlaceTools;

namespace EverythingWorld.World
{
    /// The Trip-Hop Undercity's artists. Each has a square of white station tiles with a blue stripe round the edge, an old
    /// enamel station sign on a post with their name, and one thing from their music; covers in a poster frame on a tiled
    /// wall.
    /// - Portishead: a spy-film set: a microphone on a stand under a single spotlight ("Glory Box"), a wet road under a lone
    ///   street lamp ("Roads"), and a little bar table with a record player and two glasses ("Sour Times").
    /// - Massive Attack: a great glass teardrop hanging over a dark pool ("Teardrop") and a pair of wings of light ("Angel").
    /// - The rest: a slow pendulum, two glowing eyes in the dark, a sunset on an old television, a conductor's stand, a
    ///   little garden under glass and a tango spotlight, a porcelain vase, moths round a lamp, a piano with a lone chair in
    ///   the rain, a candle in a window, a crown.
    public partial class UnderKit
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
                Add("Portishead", "Glory Box", "Portishead"); Add("Portishead", "Roads", "Portishead"); Add("Massive Attack", "Teardrop", "Massive Attack"); Add("Massive Attack", "Angel", "Massive Attack");
                Add("Glass Animals", "Heat Waves", "Glass Animals"); Add("Moby", "Porcelain", "Moby"); Add("Rob Dougan", "Clubbed to Death", "Rob Dougan"); Add("Blue Foundation", "Eyes on Fire", "Blue Foundation");
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
            if (concrete == null) MakeMaterials();
            var root = new GameObject("Artist: " + a.name).transform; root.SetParent(ctx.Root, false);
            var front = facing.TryGetValue(a.name, out var f) ? f : new Vector3(center.x - a.x, 0, center.z - a.z).normalized;
            if (front.sqrMagnitude < .01f) front = Vector3.forward;
            var labels = new GameObject("Labels").transform; labels.SetParent(root, false);
            var area = root.gameObject.AddComponent<ArtistArea>();
            area.artistName = a.name; area.stationId = a.stationId; area.radius = a.r; area.glowColor = Cold;
            var z = new Plot { ctx = ctx, root = root, labels = labels, mk = new MeshKit(), solid = new MeshKit(), pictures = new MeshKit(), a = a, c = ctx.OnGround(a.x, a.z), front = front, side = Vector3.Cross(Vector3.up, front), area = area };
            var reach = Foot(a); var up = Vector3.up; var R = reach * 1.5f;
            z.mk.Box(concreteD, z.c - up * .2f, new Vector3(R + .4f, .5f, R + .4f), z.Rot); z.mk.Box(tileM, z.c + up * .06f, new Vector3(R, .02f, R), z.Rot); foreach (var e in new[] { -1f, 1f }) { z.mk.Box(stripeM, z.c + up * .07f + z.side * e * (R / 2f - .2f), new Vector3(.3f, .02f, R), z.Rot); z.mk.Box(stripeM, z.c + up * .07f + z.front * e * (R / 2f - .2f), new Vector3(R, .02f, .3f), z.Rot); }
            switch (a.name)
            {
                case "Portishead": Spy(z); break;
                case "Massive Attack": Tear(z); break;
                default: Others(z, SignPost(z)); break;
            }
            SongLamps(z);
            Board(z, z.At(reach * .6f, -reach * .45f - 1.5f), front);
            ArtistBits.Plaque(z.mk, z.solid, labels, a, z.At(reach * .6f + 1.5f, reach * .45f + 1.5f), front, iron, stripeM, Color.white, 1.6f);
            Lamp(ctx, z.mk, z.At(reach * .5f, reach * .7f), true);
            if (z.area.glowLight == null) z.area.glowLight = ArtistBits.PointLight(root, z.c + up * 4f + front * 3f, a.name.Length % 2 == 0 ? Cold : Red, HasStation(a) ? 22f : 14f, 1.6f);
            area.glowRenderers = new Renderer[0];
            z.mk.Build("Area", root, false);
            z.solid.BuildCollider("Walls", root);
            z.pictures.Build("Covers", root, false);
            noGrass.Add(new Vector3(z.c.x, reach * 1f, z.c.z));
            var mark = new GameObject("Shot: " + a.name + " up close").transform; mark.SetParent(ctx.Root, false);
            var cam = z.At(reach * .85f, reach * .3f) + up * 2f; var look = z.At(-reach * .3f, 0) + up * 1.8f - cam; mark.SetPositionAndRotation(cam, Quaternion.LookRotation(look.normalized));
        }

        /// The covers in a poster frame on a piece of tiled wall.
        void Board(Plot z, Vector3 ground, Vector3 dir)
        {
            dir = Vector3.ProjectOnPlane(dir, Vector3.up).normalized; var rot = Quaternion.LookRotation(dir);
            var n = Mathf.Max(1, Mathf.Min(7, z.a.songList?.Length ?? 1)); const float size = .9f; var w = n * size * 1.25f + .6f;
            var c = ground + Vector3.up * 1.7f;
            z.mk.Box(tileM, c - Vector3.up * .5f - dir * .1f, new Vector3(w + 1f, size + 2.6f, .3f), rot); z.solid.Box(tileM, c - Vector3.up * .5f, new Vector3(w + 1f, size + 2.6f, .4f), rot); z.mk.Box(stripeM, c - Vector3.up * 1.3f + dir * .06f, new Vector3(w + 1f, .2f, .02f), rot); z.mk.Box(dark, c - Vector3.up * .1f + dir * .06f, new Vector3(w + .2f, size + 1.1f, .04f), rot); z.mk.Box(Paint(new Color(.9f, .88f, .82f)), c - Vector3.up * .1f + dir * .09f, new Vector3(w, size + .9f, .02f), rot);
            ArtistBits.CoverWall(z.pictures, z.labels, z.ctx, z.a, c + dir * .11f + Vector3.up * .08f, dir, n, size, n, true, Ink);
        }

        /// A little caged lamp for each song without its own piece.
        void SongLamps(Plot z)
        {
            if (!SongIds.TryGetValue(z.a.name, out var list)) return;
            var up = Vector3.up; var k = 0; var seen = new HashSet<string>();
            foreach (var (_, id) in list)
            {
                if (z.lit.Contains(id) || !seen.Add(id)) continue;
                var p = z.At(Foot(z.a) * .55f - (k % 2) * 1.3f, Foot(z.a) * .2f + (k / 2) * 1.3f);
                z.mk.Prism(iron, p, up, .03f, 1.2f, 5); z.mk.Blob(SongGlow(id, k % 2 == 0 ? Amber : Cold, 1.6f), p + up * 1.3f, Vector3.one * .1f, new System.Random(k), 0f); ArtistBits.PostCollider(z.root, p, 1.3f, .1f);
                k++;
            }
        }

        /// An old enamel station sign on a post with their name.
        PlaceFrame SignPost(Plot z)
        {
            var up = Vector3.up; var f = new PlaceFrame(z.At(-Foot(z.a) * .5f, 0), z.front);
            foreach (var e in new[] { -1.1f, 1.1f }) { z.mk.Prism(iron, f.P(e, 0, 0), up, .05f, 2.6f, 5); ArtistBits.PostCollider(z.root, f.P(e, 0, 0), 2.6f, .08f); } z.mk.Box(Paint(new Color(.8f, .1f, .1f)), f.P(0, 2.2f, 0), new Vector3(2.4f, .6f, .06f), f.r); z.mk.Box(stripeM, f.P(0, 2.2f, .04f), new Vector3(2.6f, .24f, .02f), f.r); ArtistBits.Label(z.labels, z.a.name, f.P(0, 2.2f, .06f), f.Fwd, .13f, Color.white, TextAnchor.MiddleCenter);
            return f;
        }

        // ---------------------------------------------------------------- Portishead: the spy-film set
        void Spy(Plot z)
        {
            var up = Vector3.up; var rng = new System.Random(1994); var fr = z.front; var sd = z.side; var rot = z.Rot;
            var glory = Song(z, "Glory Box"); var roads = Song(z, "Roads"); var sour = Song(z, "Sour Times"); life.glorySong = glory; life.roadsSong = roads;
            // "Glory Box": a microphone on a stand under a single spotlight
            var mc = z.At(-3f, 0); z.mk.Prism(iron, mc, up, .02f, 1.6f, 4); z.mk.Blob(dark, mc + up * 1.7f, new Vector3(.07f, .12f, .07f), rng, 0f); z.mk.Prism(iron, mc, up, .3f, .04f, 10); ArtistBits.PostCollider(z.root, mc, 1.8f, .2f); z.mk.Prism(SongGlow(glory, Amber, 1f), mc + up * .03f, up, 1.4f, .01f, 20); var sp = mc - fr * 3f + up * 5f; z.mk.Prism(iron, mc - fr * 3f, up, .06f, 5f, 5); z.mk.Prism(dark, sp, (mc + up * 1.4f - sp).normalized, .3f, .6f, 10); ArtistBits.PostCollider(z.root, mc - fr * 3f, 5f, .1f); var sl = new GameObject("Spotlight"); sl.transform.SetParent(z.root, false); sl.transform.SetPositionAndRotation(sp, Quaternion.LookRotation((mc + up * 1f - sp).normalized)); var l = sl.AddComponent<Light>(); l.type = LightType.Spot; l.range = 14f; l.spotAngle = 30f; l.intensity = 4f; l.color = new Color(1f, .82f, .6f);
            // "Roads": a wet road under a lone street lamp
            var rd = z.At(3f, -6f); var rr = Quaternion.LookRotation(sd); z.mk.Box(Paint(new Color(.1f, .1f, .12f)), rd + up * .08f, new Vector3(3f, .02f, 9f), rr); for (var k = 0; k < 4; k++) z.mk.Box(white, rd + up * .1f + sd * (-3f + k * 2f), new Vector3(.12f, .01f, 1f), rr); z.mk.Prism(waterM, rd + up * .1f + sd * 1f, up, 1f, .01f, 12); var lp = rd - fr * 1.8f; z.mk.Prism(iron, lp, up, .06f, 4.2f, 6); z.mk.Box(SongGlow(roads, Amber, 1.6f), lp + up * 4.2f + fr * .4f, new Vector3(.4f, .2f, .8f), rot); halos.Add(lp + up * 4f + fr * .4f, 2f, Amber, .4f); ArtistBits.PostCollider(z.root, lp, 4.2f, .1f);
            // "Sour Times": a little bar table with a record player and two glasses
            var bt = z.At(2f, 5f); z.mk.Prism(iron, bt, up, .04f, .9f, 5); z.mk.Prism(Paint(new Color(.3f, .18f, .14f)), bt + up * .9f, up, .6f, .05f, 14); z.mk.Box(Paint(new Color(.4f, .3f, .24f)), bt + up * 1f, new Vector3(.5f, .12f, .4f), rot); z.mk.Prism(Paint(Ink), bt + up * 1.07f, up, .16f, .01f, 14); foreach (var e in new[] { -.3f, .3f }) z.mk.Prism(SongGlow(e < 0 ? sour : null, new Color(.9f, .6f, .3f), 1.2f), bt + up * .95f + sd * e + fr * .3f, up, .04f, .14f, 8); ArtistBits.PostCollider(z.root, bt, 1f, .6f); foreach (var e in new[] { -1f, 1f }) { var cp = bt + sd * e * 1.1f; z.mk.Box(Paint(new Color(.5f, .1f, .1f)), cp + up * .45f, new Vector3(.5f, .08f, .5f), Quaternion.LookRotation(-sd * e)); z.mk.Box(Paint(new Color(.5f, .1f, .1f)), cp + up * .8f + sd * e * .22f, new Vector3(.06f, .7f, .5f), Quaternion.LookRotation(-sd * e)); Interact.Seat(tlife.transform, cp + up * .5f, -sd * e, "Sit at the bar table").seatHeight = .48f; }
            Folk(z.At(5f, 2f), -fr, Resident.Pose.Stand, new[] { "Step up to the microphone. The light finds you.", "That road doesn't go anywhere. That's the point.", "Two glasses, one empty. Always." }, Local());
            z.area.glowLight = ArtistBits.PointLight(z.root, z.c + up * 4f, new Color(.6f, .7f, 1f), 22f, 1.6f);
            Shot(z.ctx, "the spy-film set", z.At(Foot(z.a) * 1.1f, 3f) + up * 2.2f, mc + up * 1.4f);
        }

        // ---------------------------------------------------------------- Massive Attack: the teardrop
        void Tear(Plot z)
        {
            var up = Vector3.up; var rng = new System.Random(1998); var fr = z.front; var sd = z.side; var rot = z.Rot;
            var tear = Song(z, "Teardrop"); var angel = Song(z, "Angel"); life.tearSong = tear; life.angelSong = angel;
            var pc = z.At(-2f, 0); z.mk.Prism(concreteD, pc - up * .1f, up, 2.6f, .4f, 20); z.mk.Prism(waterM, pc + up * .31f, up, 2.4f, .02f, 20); BoxCol(z.root, pc + up * .15f, new Vector3(5.2f, .5f, 5.2f), rot);
            var tm = new MeshKit(); var glass = SongGlow(tear, new Color(.6f, .8f, 1f), 1f); tm.Blob(glass, Vector3.zero, new Vector3(.9f, 1f, .9f), rng, 0f); tm.Cone(glass, Vector3.up * .5f, .8f, 1.6f, 14); tm.Prism(iron, Vector3.up * 2.1f, Vector3.up, .01f, 4f, 3); var tg = tm.BuildSingle("Teardrop", z.root, false); tg.transform.position = pc + up * 3f; life.swings.Add((tg.transform, pc + up * 3f)); halos.Add(pc + up * 3.2f, 3f, new Color(.6f, .8f, 1f), .3f);
            var an = z.At(2f, 5f); foreach (var e in new[] { -1f, 1f }) for (var k = 0; k < 5; k++) z.mk.Box(k == 0 && e > 0 ? SongGlow(angel, White(), 1.4f) : GlowM(new Color(.9f, .92f, 1f), 1f), an + up * (2.2f + k * .1f) + sd * e * (.4f + k * .36f), new Vector3(.3f, 1.4f - k * .2f, .04f), rot * Quaternion.Euler(0, 0, e * (20f + k * 12f))); z.mk.Prism(iron, an, up, .04f, 2f, 5); ArtistBits.PostCollider(z.root, an, 2.6f, .3f);
            Folk(z.At(5f, -3f), -fr, Resident.Pose.Stand, new[] { "It never quite falls.", "Stand under it when the song comes on. Just stand there." }, Local());
            z.area.glowLight = ArtistBits.PointLight(z.root, pc + up * 5f, new Color(.5f, .7f, 1f), 18f, 1.6f);
        }

        static Color White() => new Color(.94f, .96f, 1f);

        // ---------------------------------------------------------------- everyone else
        void Others(Plot z, PlaceFrame f)
        {
            var up = Vector3.up; var rot = z.Rot; var rng = new System.Random(z.a.name.Length * 7 + z.a.name[0]); var c = z.At(1f, -1f) + up * .07f; var fr = z.front; var sd = z.side;
            void Post(Vector3 at, float h, float r = .04f) { z.mk.Prism(iron, at - up * .2f, up, r, h + .2f, 5); ArtistBits.PostCollider(z.root, at, h, r + .03f); }
            string Any() => SongIds.TryGetValue(z.a.name, out var l) && l.Length > 0 ? Song(z, l[0].title) : null;
            switch (z.a.name)
            {
                case "Amon Tobin": { var s = Any(); foreach (var e in new[] { -1f, 1f }) Post(c + sd * e, 3f, .05f); z.mk.Box(iron, c + up * 3f, new Vector3(2f, .08f, .08f), Quaternion.LookRotation(sd)); var pm = new MeshKit(); pm.Prism(iron, Vector3.zero, Vector3.down, .01f, 2.2f, 3); pm.Blob(SongGlow(s, Cold, 1.4f), Vector3.down * 2.3f, Vector3.one * .2f, rng, 0f); var pg = pm.BuildSingle("Pendulum", z.root, false); pg.transform.position = c + up * 3f; life.pendulums.Add((pg.transform, Quaternion.LookRotation(fr))); break; }
                case "Blue Foundation": { var s = Any(); life.eyesSong = s; z.mk.Box(dark, c + up * 1.4f - fr * .3f, new Vector3(2f, 2.8f, .2f), rot); BoxCol(z.root, c + up * 1.4f - fr * .3f, new Vector3(2f, 2.8f, .3f), rot); foreach (var e in new[] { -.4f, .4f }) z.mk.Blob(SongGlow(e < 0 ? s : null, new Color(1f, .4f, .1f), 2f), c + up * 1.8f + sd * e - fr * .18f, new Vector3(.2f, .1f, .04f), rng, 0f); break; }
                case "Glass Animals": { var s = Any(); life.heatSong = s; z.mk.Box(Paint(new Color(.4f, .3f, .24f)), c + up * .5f, new Vector3(1.2f, 1f, .9f), rot); z.mk.Box(dark, c + up * 1.4f, new Vector3(1f, .8f, .7f), rot); z.mk.Box(SongGlow(s, new Color(1f, .5f, .3f), 1.2f), c + up * 1.4f + fr * .36f, new Vector3(.8f, .6f, .02f), rot); z.mk.Box(GlowM(new Color(1f, .8f, .3f), 1.4f), c + up * 1.3f + fr * .37f, new Vector3(.3f, .3f, .01f), rot); BoxCol(z.root, c + up * .9f, new Vector3(1.2f, 1.8f, .9f), rot); break; }
                case "Hidden Orchestra": { var s = Any(); Post(c, 1.2f, .03f); z.mk.Box(iron, c + up * 1.3f, new Vector3(.5f, .36f, .02f), rot * Quaternion.Euler(-20f, 0, 0)); z.mk.Box(SongGlow(s, Amber, 1f), c + up * 1.32f + fr * .02f, new Vector3(.4f, .26f, .01f), rot * Quaternion.Euler(-20f, 0, 0)); z.mk.Prism(Paint(new Color(.5f, .1f, .1f)), c + fr * 1f, up, .5f, .3f, 8); BoxCol(z.root, c + fr * 1f + up * .15f, new Vector3(1f, .3f, 1f), rot); break; }
                case "Hooverphonic": { var eden = Song(z, "Eden"); var mad = Song(z, "Mad About You"); var gc = c - sd * 1f; z.mk.Box(GlowM(new Color(.6f, .8f, .8f), .3f), gc + up * .8f, new Vector3(1.4f, 1.6f, 1.4f), rot); for (var k = 0; k < 5; k++) z.mk.Blob(k == 0 ? SongGlow(eden, new Color(.4f, 1f, .5f), 1f) : leafM, gc + up * (.4f + k * .2f) + Quaternion.Euler(0, k * 72f, 0) * fr * .3f, Vector3.one * .3f, rng, .3f); BoxCol(z.root, gc + up * .8f, new Vector3(1.4f, 1.6f, 1.4f), rot); var ts = c + sd * 1.8f; z.mk.Prism(SongGlow(mad, Red, 1f), ts + up * .02f, up, .9f, .01f, 18); break; }
                case "Moby": { var s = Any(); life.porcelainSong = s; Post(c, .8f, .2f); z.mk.Blob(SongGlow(s, new Color(.9f, .92f, 1f), .9f), c + up * 1.3f, new Vector3(.4f, .5f, .4f), rng, 0f); z.mk.Prism(white, c + up * 1.7f, up, .14f, .4f, 10, 1.4f); for (var k = 0; k < 8; k++) z.mk.Box(stripeM, c + up * 1.3f + Quaternion.Euler(0, k * 45f, 0) * fr * .38f, new Vector3(.06f, .3f, .01f), Quaternion.Euler(0, k * 45f, 0) * rot); break; }
                case "Moth Equals": { var s = Any(); Post(c, 2.4f, .04f); z.mk.Blob(SongGlow(s, Amber, 1.8f), c + up * 2.5f, Vector3.one * .16f, rng, 0f); for (var k = 0; k < 6; k++) { var mm = new MeshKit(); mm.Tri(Paint(new Color(.7f, .66f, .6f)), Vector3.zero, new Vector3(.12f, 0, .08f), new Vector3(.12f, 0, -.08f), Vector3.up); mm.Tri(Paint(new Color(.7f, .66f, .6f)), Vector3.zero, new Vector3(-.12f, 0, .08f), new Vector3(-.12f, 0, -.08f), Vector3.up); var mg = mm.BuildSingle("Moth", z.root, false); life.moths.Add((mg.transform, c + up * 2.5f, k * 1.05f)); } break; }
                case "Rob Dougan": { var s = Any(); life.clubSong = s; var pf = new PlaceFrame(c, fr); z.mk.Box(dark, pf.P(0, .8f, 0), new Vector3(1.5f, .3f, 1.8f), pf.r); foreach (var (x, q) in new[] { (-.6f, .8f), (.6f, .8f), (0f, -.8f) }) z.mk.Prism(dark, pf.P(x, 0, q), up, .06f, .66f, 6); z.mk.Box(SongGlow(s, new Color(.4f, 1f, .5f), 1f), pf.P(0, .96f, .92f), new Vector3(1.3f, .04f, .16f), pf.r); BoxCol(z.root, pf.P(0, .6f, 0), new Vector3(1.5f, 1.2f, 1.8f), pf.r); var ch = pf.P(1.8f, 0, 1f); z.mk.Box(woodM(), ch + up * .45f, new Vector3(.45f, .05f, .45f), pf.r); z.mk.Box(woodM(), ch + up * .8f + pf.Fwd * -.2f, new Vector3(.45f, .7f, .05f), pf.r); Interact.Seat(tlife.transform, ch + up * .5f, pf.Fwd, "Sit alone in the rain").seatHeight = .48f; life.rainSpots.Add(c + up * 4f); break; }
                case "Silent Poets": { var s = Any(); var wf = new PlaceFrame(c - fr * .4f, fr); z.mk.Box(concrete, wf.P(0, 1.4f, 0), new Vector3(2f, 2.8f, .3f), wf.r); BoxCol(z.root, wf.P(0, 1.4f, 0), new Vector3(2f, 2.8f, .3f), wf.r); z.mk.Box(dark, wf.P(0, 1.6f, .16f), new Vector3(.9f, 1f, .02f), wf.r); z.mk.Prism(white, wf.P(0, 1.2f, .2f), up, .05f, .2f, 6); z.mk.Blob(SongGlow(s, Amber, 2f), wf.P(0, 1.48f, .2f), new Vector3(.04f, .08f, .04f), rng, 0f); halos.Add(wf.P(0, 1.5f, .4f), 1f, Amber, .4f); break; }
                case "il:lo": { var s = Any(); Post(c, 1f, .2f); var cr = c + up * 1.1f; z.mk.Prism(SongGlow(s, new Color(1f, .8f, .3f), 1.2f), cr, up, .3f, .14f, 10); for (var k = 0; k < 5; k++) z.mk.Cone(GlowM(new Color(1f, .8f, .3f), 1f), cr + up * .14f + Quaternion.Euler(0, k * 72f, 0) * fr * .26f, .06f, .2f, 4); break; }
                default: { var s = Any(); Post(c, 1.4f); z.mk.Blob(SongGlow(s, Cold, 1.6f), c + up * 1.5f, Vector3.one * .14f, rng, 0f); break; }
            }
        }

        Material woodM() => Paint(new Color(.4f, .28f, .2f));
    }
}
