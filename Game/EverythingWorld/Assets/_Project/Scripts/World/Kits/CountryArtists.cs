using System.Collections.Generic;
using UnityEngine;
using static EverythingWorld.World.PlaceTools;

namespace EverythingWorld.World
{
    /// The Country Plains' artists. Most have a front porch of their own: the front of a little clapboard house with
    /// its window lit, a porch with a rocking chair, a mailbox with their name, and one thing from their song; covers
    /// on a cream card on a barn-board sign.
    /// - Zac Brown Band: a backyard cookout. A fryer and a picnic table ("Chicken Fried"), a patch of sand with a beach
    ///   chair ("Toes"), a kiddie pool with a lawn chair in it ("Knee Deep"), a snowed-in pickup ("Colder Weather").
    /// - Tyler Childers: a hillside cabin porch: a fiddle on a chair, a quilt over the rail ("All Your'n"), a rocking
    ///   chair and a guitar ("Lady May"), a feather in the doorframe ("Feathered Indians"), a road sign ("Follow You To
    ///   Virgie").
    /// - Jason Aldean: a big green tractor to climb on ("Big Green Tractor"), a party round a tailgate ("My Kinda Party").
    /// - Lady A: a little bar with a bartender ("Bartender"), beehives and honey jars ("American Honey"), a clock at a
    ///   quarter after one ("Need You Now").
    public partial class CountryKit
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
                Add("Zach Bryan", "Something in the Orange", "@the square"); Add("Zac Brown Band", "Colder Weather", "@down main street"); Add("Luke Bryan", "Rain Is A Good Thing", "@the square");
                Add("The Charlie Daniels Band", "The Devil Went Down", "@the bonfire"); Add("Reba McEntire", "The Night The Lights", "@down main street"); Add("Lady A", "Need You Now", "@the bonfire");
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
            if (dirt == null) MakeMaterials();
            var root = new GameObject("Artist: " + a.name).transform; root.SetParent(ctx.Root, false);
            var front = facing.TryGetValue(a.name, out var f) ? f : new Vector3(center.x - a.x, 0, center.z - a.z).normalized;
            if (front.sqrMagnitude < .01f) front = Vector3.forward;
            var labels = new GameObject("Labels").transform; labels.SetParent(root, false);
            var area = root.gameObject.AddComponent<ArtistArea>();
            area.artistName = a.name; area.stationId = a.stationId; area.radius = a.r; area.glowColor = Wheat;
            var z = new Plot { ctx = ctx, root = root, labels = labels, mk = new MeshKit(), solid = new MeshKit(), pictures = new MeshKit(), a = a, c = ctx.OnGround(a.x, a.z), front = front, side = Vector3.Cross(Vector3.up, front), area = area };
            var reach = Foot(a); var up = Vector3.up;
            z.mk.Prism(dirt, z.c - up * .25f, up, reach * .66f, .33f, 24);
            switch (a.name)
            {
                case "Zac Brown Band": ZacBrown(z); break;
                case "Tyler Childers": Tyler(z); break;
                case "Jason Aldean": Aldean(z); break;
                case "Lady A": LadyA(z); break;
                default: Others(z, Porch(z)); break;
            }
            SongLamps(z);
            Board(z, z.At(reach * .6f, -reach * .45f - 1.5f), front);
            ArtistBits.Plaque(z.mk, z.solid, labels, a, z.At(reach * .6f + 1.5f, reach * .45f + 1.5f), front, woodM, Paint(Cream), Ink, 1.6f);
            PostLamp(ctx, z.mk, z.At(reach * .5f, reach * .68f), -z.side);
            if (z.area.glowLight == null) z.area.glowLight = ArtistBits.PointLight(root, z.c + up * 4f + front * 3f, new Color(1f, .86f, .64f), HasStation(a) ? 24f : 15f, 1.3f);
            area.glowRenderers = new Renderer[0];
            z.mk.Build("Area", root, false);
            z.solid.BuildCollider("Walls", root);
            z.pictures.Build("Covers", root, false);
            noGrass.Add(new Vector3(z.c.x, reach * .9f, z.c.z));
            var mark = new GameObject("Shot: " + a.name + " up close").transform; mark.SetParent(ctx.Root, false);
            var cam = z.At(reach * .85f, reach * .3f) + up * 2f; var look = z.At(-reach * .3f, 0) + up * 1.8f - cam; mark.SetPositionAndRotation(cam, Quaternion.LookRotation(look.normalized));
        }

        /// The covers on a cream card on a sign of weathered barn boards.
        void Board(Plot z, Vector3 ground, Vector3 dir)
        {
            dir = Vector3.ProjectOnPlane(dir, Vector3.up).normalized; var rot = Quaternion.LookRotation(dir);
            var n = Mathf.Max(1, Mathf.Min(7, z.a.songList?.Length ?? 1)); const float size = .9f; var w = n * size * 1.25f + .6f;
            var c = ground + Vector3.up * 1.7f;
            for (var k = 0; k < 5; k++) z.mk.Box(k % 2 == 0 ? Paint(BarnRed) : Paint(new Color(.56f, .2f, .16f)), c - Vector3.up * (1.1f - k * (size + 2f) / 5f) - dir * .06f, new Vector3(w + .5f, (size + 2f) / 5f - .02f, .2f), rot);
            z.solid.Box(woodM, c - Vector3.up * .5f, new Vector3(w + .5f, size + 2f, .3f), rot); z.mk.Box(Paint(Cream), c - Vector3.up * .1f + dir * .07f, new Vector3(w, size + .9f, .04f), rot);
            ArtistBits.CoverWall(z.pictures, z.labels, z.ctx, z.a, c + dir * .1f + Vector3.up * .08f, dir, n, size, n, true, Ink);
        }

        /// A mason-jar lantern on a fence post for each song without its own piece.
        void SongLamps(Plot z)
        {
            if (!SongIds.TryGetValue(z.a.name, out var list)) return;
            var up = Vector3.up; var k = 0; var seen = new HashSet<string>();
            foreach (var (_, id) in list)
            {
                if (z.lit.Contains(id) || !seen.Add(id)) continue;
                var p = z.At(Foot(z.a) * .55f - (k % 2) * 1.3f, Foot(z.a) * .2f + (k / 2) * 1.3f);
                z.mk.Prism(woodM, p - up * .1f, up, .06f, 1.2f, 4); z.mk.Prism(SongGlow(id, Wheat, 1.6f), p + up * 1.1f, up, .09f, .2f, 8); z.mk.Prism(tin, p + up * 1.3f, up, .095f, .03f, 8); ArtistBits.PostCollider(z.root, p, 1.3f, .08f);
                k++;
            }
        }

        /// A front porch of their own: a little clapboard house front, a porch, a rocking chair, a mailbox with their name.
        PlaceFrame Porch(Plot z)
        {
            var up = Vector3.up; var hue = z.a.name.Length + z.a.name[0]; var f = new PlaceFrame(z.At(-Foot(z.a) * .5f, 0), z.front); var wall = Paint(Paints[hue % Paints.Length]);
            z.mk.Box(wall, f.P(0, 1.7f, -.6f), new Vector3(5.6f, 3.8f, 1.4f), f.r); z.solid.Box(wall, f.P(0, 1.7f, -.6f), new Vector3(5.6f, 3.8f, 1.4f), f.r); for (var k = 0; k < 9; k++) z.mk.Box(Paint(Paints[hue % Paints.Length] * .9f), f.P(0, .1f + k * .4f, .11f), new Vector3(5.6f, .04f, .02f), f.r); z.mk.GableRoof(tin, f.P(0, 3.6f, -.6f), new Vector3(5.9f, 0, 1.7f), 1.2f, f.r);
            z.mk.Box(plank, f.P(0, .15f, .9f), new Vector3(5.6f, .2f, 1.8f), f.r); z.solid.Box(plank, f.P(0, -.05f, .9f), new Vector3(5.6f, .5f, 1.8f), f.r); z.mk.Box(tin, f.P(0, 2.7f, .9f), new Vector3(5.8f, .05f, 1.9f), f.r * Quaternion.Euler(10f, 0, 0)); foreach (var e in new[] { -2.7f, 2.7f }) { z.mk.Prism(woodM, f.P(e, .2f, 1.7f), up, .06f, 2.4f, 4); ArtistBits.PostCollider(z.root, f.P(e, 0, 1.7f), 2.4f, .08f); }
            z.mk.Box(woodM, f.P(-1.2f, 1.25f, .13f), new Vector3(.9f, 2f, .04f), f.r); z.mk.Box(windowM, f.P(1.1f, 1.5f, .13f), new Vector3(1.2f, .9f, .04f), f.r); halos.Add(f.P(1.1f, 1.5f, .6f), 1.2f, new Color(1f, .78f, .46f), .3f);
            Rocker(z.mk, f.P(.2f, .25f, 1f), f.Fwd);
            var mb = f.P(3.6f, 0, 2.4f); z.mk.Prism(woodM, mb - up * .2f, up, .05f, 1.2f, 4); z.mk.Box(Paint(new Color(.6f, .62f, .64f)), mb + up * 1.1f, new Vector3(.24f, .26f, .5f), f.r); z.mk.Box(Paint(BarnRed), mb + up * 1.2f + f.Right * .13f + f.Fwd * .1f, new Vector3(.02f, .2f, .04f), f.r); ArtistBits.PostCollider(z.root, mb, 1.2f, .07f);
            return f;
        }

        // ---------------------------------------------------------------- Zac Brown Band: the backyard cookout
        void ZacBrown(Plot z)
        {
            var up = Vector3.up; var rot = z.Rot; var rng = new System.Random(2008); var fr = z.front; var sd = z.side; var cf = Song(z, "Chicken Fried"); var to = Song(z, "Toes"); var kd = Song(z, "Knee Deep"); var cw = Song(z, "Colder Weather"); var km = Song(z, "Keep Me in Mind"); life.colderSong = cw;
            var f = Porch(z);
            // "Chicken Fried": a fryer on a stand, a picnic table with a checked cloth, a plate to carry
            var pt = z.At(0f, -6f) + up * .05f; z.mk.Box(woodM, pt + up * .74f, new Vector3(1.2f, .06f, 2.4f), rot * Quaternion.Euler(0, 90f, 0)); for (var x = 0; x < 3; x++) for (var y = 0; y < 6; y++) if ((x + y) % 2 == 0) z.mk.Box(Paint(BarnRed), pt + up * .78f + fr * (-.4f + x * .4f) + sd * (-1f + y * .4f), new Vector3(.4f, .01f, .4f), rot); foreach (var e in new[] { -1f, 1f }) { z.mk.Box(woodM, pt + up * .44f + fr * e * .9f, new Vector3(2.4f, .06f, .3f), rot * Quaternion.Euler(0, 0, 0)); Interact.Bench(tlife.transform, pt + up * .5f + fr * e * .9f - sd * .6f, pt + up * .5f + fr * e * .9f + sd * .6f, -fr * e); } BoxCol(z.root, pt + up * .4f, new Vector3(2.4f, .8f, 2.6f), rot);
            var fy = pt + sd * 2.4f; z.mk.Prism(iron, fy, up, .3f, .9f, 10); z.mk.Prism(Paint(new Color(.86f, .6f, .26f)), fy + up * .9f, up, .26f, .02f, 10); z.mk.Blob(SongGlow(cf, new Color(1f, .6f, .2f), 1.6f), fy + up * .2f, Vector3.one * .1f, rng, 0f); ArtistBits.PostCollider(z.root, fy, .9f, .32f);
            var pl = new MeshKit(); pl.Prism(white, Vector3.zero, Vector3.up, .16f, .02f, 12); for (var k = 0; k < 3; k++) pl.Blob(Paint(new Color(.8f, .56f, .26f)), new Vector3((k - 1) * .06f, .04f, 0), new Vector3(.05f, .03f, .07f), rng, .2f); Thing(pl, "Plate", "a plate of fried chicken", pt + up * .8f, rot, new Vector3(.34f, .1f, .34f), .3f, false);
            // "Toes": a patch of sand, a beach chair, an umbrella; "Knee Deep": a kiddie pool with a lawn chair in it
            var sa = z.At(6f, -2f); z.mk.Prism(Paint(new Color(.9f, .8f, .6f)), sa + up * .01f, up, 1.8f, .04f, 12); var bc = sa + up * .05f; z.mk.Box(Paint(new Color(.3f, .6f, .8f)), bc + up * .3f, new Vector3(.6f, .05f, 1.2f), rot * Quaternion.Euler(-10f, 40f, 0)); z.mk.Box(Paint(new Color(.3f, .6f, .8f)), bc + up * .62f - Quaternion.Euler(0, 40f, 0) * fr * .6f, new Vector3(.6f, .05f, .6f), rot * Quaternion.Euler(-55f, 40f, 0)); Interact.Seat(tlife.transform, bc + up * .4f, Quaternion.Euler(0, 40f, 0) * fr, "Toes in the sand").seatHeight = .3f; z.mk.Prism(white, sa + sd * 1.2f, up, .03f, 2f, 4); z.mk.Cone(SongGlow(to, new Color(1f, .8f, .3f), .7f), sa + sd * 1.2f + up * 1.9f, 1.1f, .4f, 8);
            var kp = z.At(6f, 5f); z.mk.Prism(Paint(new Color(.3f, .6f, .9f)), kp, up, 1.3f, .4f, 16); z.mk.Prism(SongGlow(kd, new Color(.4f, .8f, 1f), .7f), kp + up * .36f, up, 1.2f, .02f, 16); BoxCol(z.root, kp + up * .2f, new Vector3(2.6f, .4f, 2.6f), rot); z.mk.Box(stripe(), kp + up * .5f, new Vector3(.6f, .05f, 1.2f), rot * Quaternion.Euler(-10f, -30f, 0)); Interact.Seat(tlife.transform, kp + up * .6f, Quaternion.Euler(0, -30f, 0) * fr, "Sit knee deep").seatHeight = .4f;
            // "Colder Weather": a pickup in a drift of snow, its own little cold patch
            var cwp = z.At(-2f, 9f); z.mk.Prism(Paint(new Color(.94f, .96f, 1f)), cwp + up * .01f, up, 3.4f, .1f, 14); Pickup(z.root, z.mk, cwp, rot * Quaternion.Euler(0, 70f, 0), new Color(.5f, .3f, .24f), false); z.mk.Box(SongGlow(cw, new Color(.86f, .92f, 1f), .7f), cwp + up * 1.84f + Quaternion.Euler(0, 70f, 0) * fr * .9f, new Vector3(1.8f, .1f, 1.4f), rot * Quaternion.Euler(0, 70f, 0)); for (var k = 0; k < 6; k++) z.mk.Blob(Paint(Color.white), cwp + Quaternion.Euler(0, k * 60f, 0) * fr * 2.6f + up * .2f, new Vector3(.6f, .3f, .5f), rng, .2f);
            // "Keep Me in Mind": a little picture frame on the porch rail
            z.mk.Box(woodM, f.P(2.2f, 1.1f, 1.7f), new Vector3(.4f, .5f, .04f), f.r); z.mk.Box(SongGlow(km, new Color(1f, .9f, .7f), .6f), f.P(2.2f, 1.1f, 1.73f), new Vector3(.3f, .4f, .01f), f.r);
            Folk(fy - fr * .8f, fr, Resident.Pose.Work, new[] { "Chicken's almost done. Sweet tea's in the cooler. Sit down.", "Little things. Cold beer, warm night, people you love. That's it. That's the list.", "Kick your boots off. Nobody here's going anywhere." }, Local(Residents.Tool.Spatula, true)).workRate = 1f;
            z.area.glowLight = ArtistBits.PointLight(z.root, pt + up * 3f, new Color(1f, .82f, .6f), 22f, 1.6f);
            Shot(z.ctx, "the backyard cookout", z.At(12f, 3f) + up * 2.2f, pt + up * .8f);
        }

        Material stripe() => Paint(new Color(.86f, .3f, .26f));

        // ---------------------------------------------------------------- Tyler Childers: the hillside porch
        void Tyler(Plot z)
        {
            var up = Vector3.up; var rot = z.Rot; var rng = new System.Random(2017); var fr = z.front; var sd = z.side; var ay = Song(z, "All Your'n"); var fi = Song(z, "Feathered Indians"); var vi = Song(z, "Follow You To Virgie"); var lm = Song(z, "Lady May");
            // the cabin: dark logs, a deep porch, a stone chimney
            var f = new PlaceFrame(z.At(-5f, 0), fr); const float W = 7f, Dp = 5f, H = 3f; for (var k = 0; k < 8; k++) z.mk.Prism(Paint(new Color(.42f, .3f, .22f)), f.P(-W / 2f - .2f, .2f + k * .38f, Dp / 2f - .2f), f.Right, .2f, W + .4f, 6); z.solid.Box(woodM, f.P(0, H / 2f, 0), new Vector3(W, H, Dp), f.r); z.mk.Box(Paint(new Color(.4f, .3f, .22f)), f.P(0, H / 2f, 0), new Vector3(W - .2f, H, Dp - .2f), f.r); z.mk.GableRoof(tin, f.P(0, H + .1f, 0), new Vector3(W + .8f, 0, Dp + 3.4f), 1.6f, f.r);
            var ch = f.P(W / 2f + .3f, 0, -1f); z.mk.Box(Paint(new Color(.56f, .54f, .5f)), ch + up * 2.6f, new Vector3(1f, 5.2f, 1f), f.r); z.solid.Box(woodM, ch + up * 2.6f, new Vector3(1f, 5.2f, 1f), f.r); life.smokes.Add(ch + up * 5.4f);
            z.mk.Box(plank, f.P(0, .2f, Dp / 2f + 1.3f), new Vector3(W, .2f, 2.6f), f.r); z.solid.Box(plank, f.P(0, 0, Dp / 2f + 1.3f), new Vector3(W, .5f, 2.6f), f.r); foreach (var e in new[] { -3.3f, 3.3f }) { z.mk.Prism(woodM, f.P(e, .2f, Dp / 2f + 2.5f), up, .08f, 2.6f, 5); ArtistBits.PostCollider(z.root, f.P(e, 0, Dp / 2f + 2.5f), 2.6f, .1f); } z.mk.Box(woodM, f.P(0, 1f, Dp / 2f + 2.5f), new Vector3(W, .06f, .06f), f.r);
            z.mk.Box(woodM, f.P(-1f, 1.2f, Dp / 2f + .02f), new Vector3(.9f, 2f, .04f), f.r); z.mk.Box(windowM, f.P(1.6f, 1.5f, Dp / 2f + .02f), new Vector3(1f, .8f, .04f), f.r); halos.Add(f.P(1.6f, 1.5f, Dp / 2f + .5f), 1.2f, new Color(1f, .78f, .46f), .3f);
            // "Lady May": a rocking chair and a guitar leaned against it; "All Your'n": a quilt over the porch rail
            Rocker(z.mk, f.P(1.4f, .3f, Dp / 2f + 1.2f), f.Fwd); var gt = f.P(2.1f, .3f, Dp / 2f + 1.4f); z.mk.Blob(SongGlow(lm, new Color(.86f, .6f, .36f), .6f), gt + up * .3f, new Vector3(.26f, .32f, .08f), rng, 0f); z.mk.Box(woodM, gt + up * .9f, new Vector3(.05f, .8f, .04f), f.r * Quaternion.Euler(0, 0, -10f));
            var qc = new[] { BarnRed, Denim, Wheat, new Color(.5f, .6f, .42f), Cream }; for (var x = 0; x < 6; x++) for (var y = 0; y < 3; y++) z.mk.Box(x + y == 3 ? SongGlow(ay, qc[(x + y) % 5], .6f) : Paint(qc[(x * 2 + y) % 5]), f.P(-2.8f + x * .36f, .86f - y * .34f, Dp / 2f + 2.55f), new Vector3(.34f, .32f, .02f), f.r);
            // "Feathered Indians": a single feather hung in the door frame; a fiddle on a chair
            z.mk.Prism(dark, f.P(-1f, 2.28f, Dp / 2f + .06f), -up, .004f, .3f, 3); z.mk.Box(SongGlow(fi, Cream, .8f), f.P(-1f, 1.84f, Dp / 2f + .06f), new Vector3(.06f, .3f, .01f), f.r * Quaternion.Euler(0, 0, 8f));
            var fc = f.P(-2.6f, .3f, Dp / 2f + 1.3f); z.mk.Box(woodM, fc + up * .45f, new Vector3(.46f, .05f, .46f), f.r); z.mk.Box(woodM, fc + up * .8f - f.Fwd * .2f, new Vector3(.46f, .7f, .05f), f.r); foreach (var (x, zz) in new[] { (-.2f, -.2f), (.2f, -.2f), (.2f, .2f), (-.2f, .2f) }) z.mk.Box(woodM, fc + up * .22f + f.r * new Vector3(x, 0, zz), new Vector3(.04f, .44f, .04f), f.r); z.mk.Blob(Paint(new Color(.56f, .3f, .16f)), fc + up * .54f, new Vector3(.12f, .04f, .26f), rng, 0f); z.mk.Box(dark, fc + up * .56f + f.Fwd * .34f, new Vector3(.03f, .02f, .3f), f.r);
            string[] fid = { "You lift the fiddle and saw out something old. It sounds like the hills have known it longer than you.", "A slow waltz. The rocking chair rocks in time on its own.", "You play it badly and it's still beautiful. That's mountain music." }; var fn = 0;
            Spot("The fiddle", fc + up * .9f, "Play the fiddle", new Vector3(.6f, .8f, .6f)).onUse = _ => { life.played = Time.time; Say1(fc + up * 2f, fid[fn++ % fid.Length]); };
            // "Follow You To Virgie": a little green road sign
            var rs = z.At(5f, -6f); z.mk.Prism(tin, rs - up * .2f, up, .04f, 2.4f, 4); ArtistBits.PostCollider(z.root, rs, 2.2f, .06f); z.mk.Box(SongGlow(vi, new Color(.2f, .5f, .3f), .5f), rs + up * 2f, new Vector3(1.4f, .5f, .04f), rot); ArtistBits.Label(z.labels, "VIRGIE  12", rs + up * 2f + fr * .03f, fr, .16f, Cream, TextAnchor.MiddleCenter);
            Folk(f.P(-.2f, .3f, Dp / 2f + 2f), f.Fwd, Resident.Pose.Lean, new[] { "Come on up on the porch. Mind the third step, it's soft.", "Cabin's been in the family four generations. The fiddle's older.", "The hills here hold a song like a bowl holds water." });
            z.area.glowLight = ArtistBits.PointLight(z.root, f.P(0, 2.6f, Dp / 2f + 2f), new Color(1f, .8f, .56f), 18f, 1.8f);
            Shot(z.ctx, "the cabin porch", z.At(11f, 4f) + up * 2f, f.P(0, 1.4f, Dp / 2f));
        }

        // ---------------------------------------------------------------- Jason Aldean
        void Aldean(Plot z)
        {
            var up = Vector3.up; var rot = z.Rot; var rng = new System.Random(2009); var fr = z.front; var sd = z.side; var bt = Song(z, "Big Green Tractor"); var kp = Song(z, "My Kinda Party"); life.tractorSong = bt;
            // the big green tractor, gleaming
            var tr = z.At(-3f, -1f); var tm = SongGlow(bt, new Color(.24f, .56f, .26f), .4f); var tf = Quaternion.LookRotation(sd); var tfw = sd; mk3(z, tr, tf, tm); z.area.glowLight = ArtistBits.PointLight(z.root, tr + up * 4f + fr * 3f, new Color(1f, .86f, .6f), 22f, 1.6f);
            // "My Kinda Party": a pickup with its tailgate down, a cooler, string lights, a few folk
            var pp = z.At(4f, 6f); Pickup(z.root, z.mk, pp, rot * Quaternion.Euler(0, 160f, 0), new Color(.2f, .2f, .22f)); var cl = pp + fr * 2.4f + sd * 1f; z.mk.Box(Paint(new Color(.9f, .3f, .2f)), cl + up * .3f, new Vector3(.8f, .5f, .5f), rot); z.mk.Box(white, cl + up * .58f, new Vector3(.84f, .08f, .54f), rot); BoxCol(z.root, cl + up * .3f, new Vector3(.8f, .6f, .5f), rot);
            var a = pp + sd * 3f + up * .2f; var b = pp - sd * 3f; foreach (var g in new[] { a, b }) { z.mk.Prism(woodM, g - up * .2f, up, .05f, 3.2f, 4); ArtistBits.PostCollider(z.root, g, 3f, .07f); } for (var k = 1; k < 10; k++) { var t = k / 10f; z.mk.Blob(k % 3 == 0 ? SongGlow(kp, Wheat, 1.6f) : lampM, Vector3.Lerp(a, b, t) + up * (2.9f - Mathf.Sin(t * Mathf.PI) * .4f), Vector3.one * .07f, rng, 0f); }
            for (var k = 0; k < 3; k++) Folk(pp + fr * (3.4f + k * .6f) + sd * (-1.4f + k * 1.4f), -fr, k == 1 ? Resident.Pose.Wave : Resident.Pose.Stand, new[] { "Party's wherever the tailgate's down.", "Somebody turn it up! No, up! There it is!", "Climb up on the tractor! He lets everybody!" });
            Shot(z.ctx, "the big green tractor", z.At(10f, -4f) + up * 2f, tr + up * 1.6f);
        }

        /// The big green tractor (for Jason Aldean): bigger and shinier than the old ones in the fields, with a seat to climb into.
        void mk3(Plot z, Vector3 g, Quaternion rot, Material m)
        {
            var up = Vector3.up; var f = rot * Vector3.forward; var r = rot * Vector3.right; z.mk.Box(m, g + up * 1.4f + f * .9f, new Vector3(1.2f, 1.1f, 2.6f), rot); z.mk.Box(m, g + up * 2.3f - f * .6f, new Vector3(1.6f, 1.6f, 1.4f), rot); z.mk.Box(Paint(new Color(.6f, .7f, .76f)), g + up * 2.4f - f * .6f, new Vector3(1.64f, 1.2f, 1.2f), rot); z.mk.Box(m, g + up * 3.16f - f * .6f, new Vector3(1.8f, .1f, 1.6f), rot); z.mk.Prism(dark, g + up * 1.9f + f * 1.6f, up, .07f, 1.4f, 5); z.mk.Box(Paint(new Color(1f, .86f, .3f)), g + up * .9f + f * 2.21f, new Vector3(1.2f, .3f, .02f), rot);
            foreach (var e in new[] { -1f, 1f }) { z.mk.Prism(dark, g + up * 1.1f - f * .6f + r * e * .8f - r * e * .24f, r * e, 1.1f, .48f, 16); z.mk.Prism(Paint(new Color(1f, .86f, .3f)), g + up * 1.1f - f * .6f + r * e * 1.05f, r * e, .5f, .02f, 12); z.mk.Prism(dark, g + up * .55f + f * 1.8f + r * e * .6f - r * e * .14f, r * e, .55f, .28f, 12); }
            z.solid.Box(woodM, g + up * 1.4f, new Vector3(2.4f, 2.8f, 4f), rot); Interact.Seat(tlife.transform, g + up * 2.3f - f * .7f, f, "Climb up on the tractor").seatHeight = .3f; Mark(g, 3f);
        }

        // ---------------------------------------------------------------- Lady A
        void LadyA(Plot z)
        {
            var up = Vector3.up; var rot = z.Rot; var rng = new System.Random(2010); var fr = z.front; var sd = z.side; var ah = Song(z, "American Honey"); var bt = Song(z, "Bartender"); var nn = Song(z, "Need You Now"); life.honeySong = ah; life.needSong = nn;
            // "Bartender": a little roadhouse bar, a counter, stools, a bartender
            var f = new PlaceFrame(z.At(-5f, 0), fr); z.mk.Box(Paint(new Color(.36f, .28f, .24f)), f.P(0, 1.8f, -1f), new Vector3(7f, 3.6f, 1f), f.r); z.solid.Box(woodM, f.P(0, 1.8f, -1f), new Vector3(7f, 3.6f, 1f), f.r); z.mk.Box(tin, f.P(0, 3.4f, .4f), new Vector3(7.4f, .06f, 3f), f.r * Quaternion.Euler(8f, 0, 0)); foreach (var e in new[] { -3.5f, 3.5f }) { z.mk.Prism(woodM, f.P(e, 0, 1.8f), up, .07f, 3.2f, 4); ArtistBits.PostCollider(z.root, f.P(e, 0, 1.8f), 3f, .09f); }
            for (var row = 0; row < 3; row++) for (var k = 0; k < 10; k++) z.mk.Prism(Paint(new Color(.5f + (k % 3) * .1f, .3f, .16f)), f.P(-2.7f + k * .6f, 1.4f + row * .5f, -.45f), up, .06f, .3f, 6);
            z.mk.Box(woodM, f.P(0, .55f, .5f), new Vector3(5f, 1.1f, .6f), f.r); z.mk.Box(SongGlow(bt, new Color(1f, .7f, .4f), .5f), f.P(0, 1.12f, .5f), new Vector3(5.1f, .05f, .7f), f.r); BoxCol(z.root, f.P(0, .55f, .5f), new Vector3(5f, 1.1f, .6f), f.r);
            for (var k = 0; k < 4; k++) { var sp = f.P(-1.8f + k * 1.2f, 0, 1.3f); z.mk.Prism(Paint(BarnRed), sp + up * .74f, up, .2f, .06f, 10); z.mk.Prism(iron, sp, up, .03f, .74f, 4); Interact.Seat(tlife.transform, sp + up * .82f, -f.Fwd, "Sit at the bar").seatHeight = .76f; }
            Folk(f.P(.4f, 0, -.2f), f.Fwd, Resident.Pose.Stand, new[] { "What'll it be? Don't answer. I know. You want the slow one.", "Last call's whenever the fire goes out. The fire never goes out.", "Pour you something sweet. Tonight seems like that kind of night." }, Local(Residents.Tool.Mug, true));
            // "American Honey": beehives on the grass and a shelf of honey jars glowing
            for (var k = 0; k < 3; k++) { var hp = z.At(4f, -7f + k * 1.6f); z.mk.Box(white, hp + up * .4f, new Vector3(.7f, .8f, .6f), rot); z.mk.Box(white, hp + up * .86f, new Vector3(.8f, .1f, .7f), rot); BoxCol(z.root, hp + up * .45f, new Vector3(.7f, .9f, .6f), rot); } var sh = z.At(6f, -5f); z.mk.Box(woodM, sh + up * .9f, new Vector3(1.4f, .05f, .4f), rot); foreach (var e in new[] { -.6f, .6f }) z.mk.Box(woodM, sh + up * .45f + sd * e, new Vector3(.05f, .9f, .4f), rot); for (var k = 0; k < 5; k++) z.mk.Prism(k == 2 ? SongGlow(ah, new Color(1f, .7f, .2f), 1.3f) : GlowM(new Color(1f, .7f, .2f), .7f), sh + up * .93f + sd * (-.5f + k * .25f), up, .08f, .2f, 8); BoxCol(z.root, sh + up * .45f, new Vector3(1.4f, .9f, .4f), rot);
            // "Need You Now": a clock on a post at a quarter after one
            var ck = z.At(5f, 7f); z.mk.Prism(iron, ck - up * .2f, up, .05f, 3f, 5); ArtistBits.PostCollider(z.root, ck, 2.8f, .07f); z.mk.Prism(white, ck + up * 3.2f - fr * .05f, fr, .5f, .1f, 16); z.mk.Box(dark, ck + up * 3.2f + fr * .06f + sd * .12f, new Vector3(.03f, .3f, .01f), rot * Quaternion.Euler(0, 0, -90f)); z.mk.Box(dark, ck + up * 3.2f + fr * .07f + up * .18f, new Vector3(.03f, .38f, .01f), rot * Quaternion.Euler(0, 0, -30f)); z.mk.Blob(SongGlow(nn, new Color(.6f, .7f, 1f), 1f), ck + up * 3.2f + fr * .08f, Vector3.one * .04f, rng, 0f);
            z.area.glowLight = ArtistBits.PointLight(z.root, f.P(0, 2.6f, 2f), new Color(1f, .78f, .5f), 18f, 1.8f);
            Shot(z.ctx, "the roadhouse bar", z.At(11f, 3f) + up * 2f, f.P(0, 1.4f, 0));
        }

        // ---------------------------------------------------------------- everyone else: one thing from their song
        void Others(Plot z, PlaceFrame f)
        {
            var up = Vector3.up; var rot = z.Rot; var rng = new System.Random(z.a.name.Length * 7 + z.a.name[0]); var c = z.At(2.4f, 0) + up * .02f; var fr = z.front; var sd = z.side;
            void Post(Vector3 at, float h, float r = .05f) { z.mk.Prism(woodM, at - up * .2f, up, r, h + .2f, 5); ArtistBits.PostCollider(z.root, at, h, r + .03f); }
            void Stump(Vector3 at, float h = .6f) { z.mk.Prism(trunkM, at, up, .4f, h, 9); z.mk.Prism(Paint(new Color(.8f, .66f, .46f)), at + up * h, up, .38f, .01f, 9); BoxCol(z.root, at + up * h / 2f, new Vector3(.8f, h, .8f), rot); }
            void SignPost(Vector3 at, string text, Material m, Color ink) { Post(at, 2.2f, .04f); z.mk.Box(m, at + up * 2f, new Vector3(1.4f, .5f, .04f), rot); ArtistBits.Label(z.labels, text, at + up * 2f + fr * .03f, fr, .14f, ink, TextAnchor.MiddleCenter); }
            switch (z.a.name)
            {
                case "Alabama": { var mm = Song(z, "Mountain Music"); var ss = Song(z, "Song of the South"); Stump(c - sd * .8f); z.mk.Prism(Paint(new Color(.9f, .86f, .76f)), c - sd * .8f + up * .66f, fr, .2f, .04f, 14); z.mk.Box(SongGlow(mm, new Color(.86f, .66f, .4f), .5f), c - sd * .8f + up * .66f + fr * .5f, new Vector3(.05f, .03f, .7f), rot); SignPost(c + sd * 1f, "MOBILE  280", SongGlow(ss, new Color(.2f, .5f, .3f), .5f), Cream); break; }
                case "Alan Jackson": { var ch = Song(z, "Chattahoochee"); var ww = Song(z, "Where Were You"); z.mk.Prism(SongGlow(ch, new Color(.3f, .5f, .6f), .5f), c + up * .01f, up, 1.6f, .03f, 14); z.mk.Prism(dark, c + up * .1f, up, .5f, .14f, 12); z.mk.Prism(Paint(new Color(.3f, .5f, .6f)), c + up * .1f, up, .25f, .16f, 10); Post(c + sd * 1.8f, .8f, .03f); z.mk.Prism(white, c + sd * 1.8f + up * .8f, up, .05f, .16f, 6); z.mk.Blob(SongGlow(ww, new Color(1f, .8f, .4f), 1.6f), c + sd * 1.8f + up * 1f, new Vector3(.02f, .05f, .02f), rng, 0f); break; }
                case "Darius Rucker": { var wg = Song(z, "Wagon Wheel"); var al = Song(z, "Alright"); var wc = c + up * 1f; z.mk.Prism(SongGlow(wg, new Color(.66f, .5f, .32f), .4f), wc - fr * .06f, fr, 1f, .12f, 16); z.mk.Prism(Paint(Cream), wc - fr * .07f, fr, .86f, .14f, 16); z.mk.Prism(woodM, wc - fr * .1f, fr, .2f, .2f, 10); for (var k = 0; k < 8; k++) z.mk.Box(woodM, wc + Quaternion.AngleAxis(k * 22.5f, fr) * up * .44f, new Vector3(.06f, .8f, .06f), Quaternion.AngleAxis(k * 22.5f, fr) * rot); BoxCol(z.root, wc, new Vector3(2f, 2f, .3f), rot); Post(c + sd * 1.6f, 1.4f, .03f); z.mk.Blob(SongGlow(al, Wheat, 1.2f), c + sd * 1.6f + up * 1.6f, Vector3.one * .14f, rng, 0f); break; }
                case "Dolly Parton": { var nf = Song(z, "9 to 5"); var jo = Song(z, "Jolene"); var tc = c - sd * .8f; z.mk.Box(Paint(new Color(.6f, .6f, .62f)), tc + up * 1.2f, new Vector3(.6f, .8f, .3f), rot); z.mk.Prism(SongGlow(nf, Cream, .7f), tc + up * 1.3f + fr * .16f, fr, .2f, .02f, 14); Post(tc - fr * .2f, .8f, .05f); z.mk.Box(dark, tc + up * 1.3f + fr * .18f, new Vector3(.02f, .14f, .01f), rot); z.mk.Box(dark, tc + up * 1.36f + fr * .18f + sd * .04f, new Vector3(.02f, .1f, .01f), rot * Quaternion.Euler(0, 0, -60f)); var rp = c + sd * 1.2f; z.mk.Prism(Paint(new Color(.7f, .46f, .36f)), rp, up, .22f, .36f, 8, 1.2f); z.mk.Prism(leafM, rp + up * .3f, up, .015f, .6f, 3); z.mk.Blob(SongGlow(jo, new Color(.9f, .3f, .3f), .8f), rp + up * .94f, Vector3.one * .1f, rng, .3f); ArtistBits.PostCollider(z.root, rp, .5f, .25f); break; }
                case "Hank Williams, Jr.": { var wm = Song(z, "Weatherman"); var ih = Song(z, "If Heaven"); Post(c, 3f, .04f); for (var k = 0; k < 4; k++) z.mk.Box(tin, c + up * 2.6f, new Vector3(.03f, .03f, .8f), Quaternion.Euler(0, k * 45f, 0)); z.mk.Tri(SongGlow(wm, Wheat, .7f), c + up * 3.1f + fr * .5f, c + up * 3.1f - fr * .4f + up * .25f, c + up * 3.1f - fr * .4f - up * .1f, c + up * 3.1f + sd); z.mk.Tri(SongGlow(wm, Wheat, .7f), c + up * 3.1f + fr * .5f, c + up * 3.1f - fr * .4f + up * .25f, c + up * 3.1f - fr * .4f - up * .1f, c + up * 3.1f - sd); Post(c + sd * 1.8f, 1.6f, .03f); z.mk.Blob(SongGlow(ih, new Color(.9f, .94f, 1f), 1f), c + sd * 1.8f + up * 2f, new Vector3(.4f, .2f, .3f), rng, .2f); break; }
                case "Johnny Cash": { var hu = Song(z, "Hurt"); var gg = Song(z, "God's Gonna"); life.hurtSong = hu; Stump(c - sd * .8f, .5f); z.mk.Box(SongGlow(gg, new Color(.6f, .6f, .62f), .5f), c - sd * .8f + up * .72f, new Vector3(.04f, .26f, .4f), rot * Quaternion.Euler(20f, 0, 0)); z.mk.Prism(woodM, c - sd * .8f + up * .6f, (up + fr * .4f).normalized, .03f, .9f, 4); var gs = c + sd * 1f; Post(gs - fr * .2f, .8f, .03f); z.mk.Blob(Paint(new Color(.08f, .08f, .08f)), gs + up * .7f, new Vector3(.26f, .34f, .08f), rng, 0f); z.mk.Box(dark, gs + up * 1.3f, new Vector3(.06f, .8f, .04f), rot); z.mk.Prism(white, gs + fr * .5f, up, .05f, .16f, 6); z.mk.Blob(SongGlow(hu, new Color(1f, .8f, .5f), 1.4f), gs + fr * .5f + up * .2f, new Vector3(.02f, .05f, .02f), rng, 0f); break; }
                case "Sierra Ferrell": { var pd = Song(z, "Pair of Dice"); var tl = Song(z, "Trying to Love You"); for (var k = 0; k < 2; k++) { var dp = c + sd * (-.6f + k * 1.2f) + up * .36f; var dr = rot * Quaternion.Euler(0, k * 30f, 0); z.mk.Box(k == 0 ? SongGlow(pd, Cream, .4f) : white, dp, Vector3.one * .7f, dr); for (var q = 0; q < 3 + k * 2; q++) z.mk.Blob(dark, dp + dr * new Vector3(-.2f + q * .1f, .36f, (q % 2) * .2f - .1f), Vector3.one * .05f, rng, 0f); } BoxCol(z.root, c + up * .36f, new Vector3(2f, .7f, .8f), rot); Post(c + fr * 1.4f, 1f, .03f); z.mk.Blob(SongGlow(tl, new Color(1f, .6f, .7f), 1f), c + fr * 1.4f + up * 1.2f, Vector3.one * .12f, rng, 0f); break; }
                case "Toby Keith": { var cr = Song(z, "Courtesy"); var sc = Song(z, "Should've Been A Cowboy"); Post(c - sd * 1f, 5f, .05f); for (var k = 0; k < 5; k++) z.mk.Box(Paint(k % 2 == 0 ? BarnRed : Cream), c - sd * 1f + up * (4.6f - k * .18f) + sd * .7f, new Vector3(.02f, .18f, 1.4f), Quaternion.LookRotation(sd)); z.mk.Box(SongGlow(cr, Denim, .5f), c - sd * 1f + up * 4.42f + sd * .35f, new Vector3(.03f, .54f, .7f), Quaternion.LookRotation(sd)); var rl = c + sd * 1.2f; foreach (var e in new[] { -.6f, .6f }) Post(rl + sd * e, .9f, .04f); z.mk.Box(woodM, rl + up * 1f, new Vector3(1.4f, .08f, .08f), Quaternion.LookRotation(fr)); z.mk.Blob(SongGlow(sc, new Color(.56f, .34f, .2f), .5f), rl + up * 1.16f, new Vector3(.3f, .14f, .4f), rng, 0f); break; }
                case "Zach Bryan": { var so = Song(z, "Something in the Orange"); var da = Song(z, "Dawns"); life.orangeSong = so; Post(c, 2.4f, .04f); z.mk.Blob(SongGlow(so, new Color(1f, .5f, .16f), 1.8f), c + up * 2.7f, Vector3.one * .4f, rng, 0f); halos.Add(c + up * 2.7f, 2f, new Color(1f, .5f, .16f), .5f); Rocker(z.mk, c + fr * 1.2f - sd * .8f, fr); Post(c + sd * 1.2f, 1f, .03f); z.mk.Prism(SongGlow(da, new Color(1f, .8f, .6f), .8f), c + sd * 1.2f + up * 1.1f, fr, .2f, .04f, 12); break; }
                case "Blake Shelton": { var s = Song(z, "God's Country"); SignPost(c, "GOD'S COUNTRY", SongGlow(s, new Color(.4f, .5f, .3f), .4f), Cream); break; }
                case "Brad Paisley": { var s = Song(z, "Remind Me"); Post(c - sd * .5f, 1.2f, .03f); Post(c + sd * .5f, 1.2f, .03f); z.mk.Box(woodM, c + up * 1.5f, new Vector3(1.2f, .9f, .06f), rot); z.mk.Box(SongGlow(s, new Color(1f, .86f, .7f), .6f), c + up * 1.5f + fr * .04f, new Vector3(1f, .7f, .02f), rot); break; }
                case "Carrie Underwood": { var s = Song(z, "Before He Cheats"); var pr = rot * Quaternion.Euler(0, 70f, 0); Pickup(z.root, z.mk, c, pr, new Color(.3f, .4f, .26f), false); z.mk.Blob(SongGlow(s, new Color(1f, .9f, .6f), 1.4f), c + up * .9f + pr * Vector3.forward * 2.22f + pr * Vector3.right * -.7f, new Vector3(.14f, .1f, .03f), rng, 0f); z.mk.Prism(Paint(new Color(.7f, .5f, .3f)), c + pr * Vector3.forward * 2.8f + sd * .4f, (up + fr * .3f).normalized, .04f, .9f, 6, 1.4f); for (var k = 0; k < 4; k++) z.mk.Box(Paint(new Color(.8f, .9f, .94f)), c + pr * Vector3.forward * 2.6f + Quaternion.Euler(0, k * 80f, 0) * fr * .3f + up * .01f, new Vector3(.1f, .01f, .07f), Quaternion.Euler(0, k * 50f, 0)); break; }
                case "Dasha": { var s = Song(z, "Memo"); Post(c - sd * .5f, 1.4f, .03f); Post(c + sd * .5f, 1.4f, .03f); z.mk.Box(Paint(new Color(.66f, .5f, .36f)), c + up * 1.6f, new Vector3(1.2f, .8f, .05f), rot); for (var k = 0; k < 4; k++) z.mk.Box(k == 1 ? SongGlow(s, new Color(1f, .9f, .5f), .6f) : Paint(new Color(1f, .96f, .7f)), c + up * (1.4f + (k / 2) * .3f) + sd * (-.3f + (k % 2) * .5f) + fr * .04f, new Vector3(.3f, .24f, .01f), rot * Quaternion.Euler(0, 0, k * 6f - 6f)); break; }
                case "Dylan Gossett": { var s = Song(z, "Coal"); z.mk.Box(Paint(new Color(.3f, .26f, .24f)), c + up * .5f, new Vector3(1f, .6f, 1.6f), rot); foreach (var e in new[] { -1f, 1f }) foreach (var q in new[] { -.5f, .5f }) z.mk.Prism(dark, c + up * .2f + sd * e * .5f + fr * q, sd * e, .2f, .06f, 10); for (var k = 0; k < 8; k++) z.mk.Blob(k == 0 ? SongGlow(s, new Color(1f, .4f, .2f), 1f) : dark, c + up * .86f + new Vector3((k % 3 - 1) * .25f, 0, (k / 3 - 1) * .3f), Vector3.one * .16f, rng, .3f); BoxCol(z.root, c + up * .5f, new Vector3(1f, 1f, 1.6f), rot); break; }
                case "Eric Church": { var s = Song(z, "Springsteen"); Post(c, 1f, .04f); z.mk.Box(dark, c + up * 1.12f, new Vector3(.6f, .3f, .2f), rot); z.mk.Box(SongGlow(s, new Color(1f, .7f, .4f), .8f), c + up * 1.14f + fr * .11f, new Vector3(.4f, .14f, .01f), rot); foreach (var e in new[] { -.18f, .18f }) z.mk.Prism(Paint(new Color(.8f, .8f, .8f)), c + up * 1.14f + fr * .11f + sd * e, fr, .05f, .01f, 8); break; }
                case "Florida Georgia Line": { var s = Song(z, "Cruise"); Pickup(z.root, z.mk, c, rot * Quaternion.Euler(0, 90f, 0), new Color(.9f, .9f, .9f)); z.mk.Box(SongGlow(s, new Color(.3f, .6f, .9f), .5f), c + up * .9f - Quaternion.Euler(0, 90f, 0) * fr * 1.3f, new Vector3(.6f, .4f, .4f), rot); break; }
                case "Gabby Barrett": { var s = Song(z, "I Hope"); z.mk.Prism(Paint(new Color(.56f, .54f, .5f)), c, up, .8f, .8f, 12); z.mk.Prism(SongGlow(s, new Color(.4f, .7f, 1f), .8f), c + up * .7f, up, .66f, .02f, 12); foreach (var e in new[] { -.7f, .7f }) z.mk.Prism(woodM, c + sd * e, up, .05f, 1.9f, 4); z.mk.GableRoof(woodM, c + up * 1.9f, new Vector3(1.8f, 0, 1.2f), .5f, Quaternion.LookRotation(fr)); BoxCol(z.root, c + up * .4f, new Vector3(1.6f, .8f, 1.6f), rot); break; }
                case "George Strait": { var s = Song(z, "Amarillo"); SignPost(c, "AMARILLO  112", SongGlow(s, new Color(.2f, .5f, .3f), .5f), Cream); break; }
                case "Hank Williams": { var s = Song(z, "Your Cheatin"); Post(c, 1.6f, .03f); var hm = SongGlow(s, new Color(.9f, .2f, .26f), 1f); var hc = c + up * 2.1f; foreach (var e in new[] { -1f, 1f }) { z.mk.Blob(hm, hc + sd * e * .2f + up * .1f, Vector3.one * .22f, rng, 0f); z.mk.Tri(hm, hc + sd * e * .4f, hc + sd * e * .02f + up * .1f, hc - up * .4f + sd * e * .04f, hc + fr); z.mk.Tri(hm, hc + sd * e * .4f, hc + sd * e * .02f + up * .1f, hc - up * .4f + sd * e * .04f, hc - fr); } break; }
                case "Jake Owen": { var s = Song(z, "Barefoot"); foreach (var e in new[] { -.15f, .15f }) { var bp = c + sd * e; z.mk.Box(Paint(new Color(.5f, .34f, .22f)), bp + up * .2f, new Vector3(.12f, .4f, .14f), rot * Quaternion.Euler(0, 0, e * 40f)); z.mk.Box(Paint(new Color(.5f, .34f, .22f)), bp + up * .05f + fr * .1f, new Vector3(.12f, .1f, .3f), rot); } z.mk.Prism(SongGlow(s, new Color(.4f, .5f, .8f), .5f), c + sd * 1f, up, .4f, .06f, 10); break; }
                case "John Anderson": { var s = Song(z, "Seminole Wind"); Post(c, 3.2f, .04f); z.mk.Prism(SongGlow(s, new Color(1f, .5f, .2f), .7f), c + up * 3f, (sd + up * -.1f).normalized, .2f, 1.2f, 8, .3f); break; }
                case "Kacey Musgraves": { var s = Song(z, "Abilene"); Post(c, 1f, .03f); z.mk.Box(SongGlow(s, new Color(1f, .9f, .7f), .6f), c + up * 1.1f, new Vector3(.5f, .26f, .02f), rot * Quaternion.Euler(0, 0, 8f)); ArtistBits.Label(z.labels, "ABILENE", c + up * 1.1f + fr * .02f, fr, .06f, Ink, TextAnchor.MiddleCenter); break; }
                case "Kenny Rogers": { var s = Song(z, "The Gambler"); z.mk.Prism(Paint(new Color(.2f, .4f, .26f)), c + up * .74f, up, .7f, .05f, 12); z.mk.Prism(woodM, c, up, .06f, .74f, 5); BoxCol(z.root, c + up * .38f, new Vector3(1.4f, .76f, 1.4f), rot); for (var k = 0; k < 5; k++) z.mk.Box(k == 0 ? SongGlow(s, Cream, .6f) : white, c + up * .8f + Quaternion.Euler(0, k * 30f - 60f, 0) * fr * .3f, new Vector3(.14f, .01f, .2f), Quaternion.Euler(0, k * 30f, 0)); for (var k = 0; k < 4; k++) z.mk.Prism(Paint(k % 2 == 0 ? BarnRed : Cream), c + up * (.8f + k * .02f) - fr * .3f + sd * .2f, up, .05f, .02f, 8); Chair(z, c - fr * 1f, fr); break; }
                case "LeAnn Rimes": { var s = Song(z, "Can't Fight The Moonlight"); life.moonSong = s; Post(c, 3f, .04f); z.mk.Prism(SongGlow(s, new Color(.9f, .92f, 1f), 1.6f), c + up * 3.4f - fr * .06f, fr, .5f, .1f, 16); halos.Add(c + up * 3.4f, 1.6f, new Color(.9f, .92f, 1f), .4f); break; }
                case "Luke Bryan": { var s = Song(z, "Rain Is A Good Thing"); life.rainSong = s; z.mk.Prism(woodM, c, up, .5f, 1f, 12); z.mk.Prism(SongGlow(s, new Color(.5f, .7f, .9f), .6f), c + up * .96f, up, .46f, .02f, 12); for (var k = 0; k < 2; k++) z.mk.Prism(iron, c + up * (.3f + k * .5f), up, .52f, .05f, 12); ArtistBits.PostCollider(z.root, c, 1f, .52f); break; }
                case "Luke Combs": { var s = Song(z, "Where the Wild"); var tp = c; for (var k = 0; k < 4; k++) z.mk.Tri(SongGlow(k == 0 ? s : null, new Color(.4f, .5f, .3f), .3f), tp + up * 1.4f, tp + Quaternion.Euler(0, k * 90f + 45f, 0) * fr * 1.2f, tp + Quaternion.Euler(0, k * 90f + 135f, 0) * fr * 1.2f, tp - up); BoxCol(z.root, tp + up * .6f, new Vector3(1.6f, 1.2f, 1.6f), rot); break; }
                case "Maren Morris": { var s = Song(z, "The Bones"); var hp = c; foreach (var (x, zz) in new[] { (-1f, -1f), (1f, -1f), (1f, 1f), (-1f, 1f) }) { z.mk.Prism(Paint(new Color(.86f, .74f, .56f)), hp + rot * new Vector3(x, 0, zz) * .8f, up, .05f, 1.8f, 4); ArtistBits.PostCollider(z.root, hp + rot * new Vector3(x, 0, zz) * .8f, 1.8f, .06f); } foreach (var e in new[] { -.8f, .8f }) z.mk.Prism(SongGlow(e < 0 ? s : null, new Color(.86f, .74f, .56f), .5f), hp + up * 1.8f + fr * e - sd * .8f, sd, .04f, 1.6f, 4); z.mk.Prism(Paint(new Color(.86f, .74f, .56f)), hp + up * 1.8f - fr * .8f + sd * .8f, (up * .6f + fr * .8f).normalized, .04f, 1.2f, 4); z.mk.Prism(Paint(new Color(.86f, .74f, .56f)), hp + up * 1.8f + fr * .8f + sd * .8f, (up * .6f - fr * .8f).normalized, .04f, 1.2f, 4); break; }
                case "Marty Robbins": { var s = Song(z, "Big Iron"); Post(c, 1.6f, .04f); z.mk.Prism(SongGlow(s, new Color(.4f, .3f, .2f), .4f), c + up * 1.7f, up, .4f, .04f, 12); z.mk.Prism(Paint(new Color(.4f, .3f, .2f)), c + up * 1.72f, up, .18f, .2f, 10, .8f); z.mk.Box(Paint(new Color(.8f, .7f, .3f)), c + up * 1.2f + fr * .06f, new Vector3(.16f, .16f, .02f), rot * Quaternion.Euler(0, 0, 45f)); break; }
                case "Merle Haggard": { var s = Song(z, "Mama Tried"); Post(c, 1f, .04f); z.mk.Box(Paint(new Color(.6f, .62f, .64f)), c + up * 1.12f, new Vector3(.26f, .28f, .5f), rot); for (var k = 0; k < 3; k++) z.mk.Box(k == 0 ? SongGlow(s, Cream, .5f) : white, c + up * 1.3f + fr * (.26f + k * .02f) + up * k * .02f, new Vector3(.2f, .01f, .14f), rot * Quaternion.Euler(0, k * 15f, 0)); break; }
                case "Patsy Cline": { var s = Song(z, "Walkin' After Midnight"); life.midnightSong = s; Post(c, 3.2f, .05f); z.mk.Box(SongGlow(s, new Color(.7f, .8f, 1f), 1.2f), c + up * 3.3f, new Vector3(.3f, .4f, .3f), rot); halos.Add(c + up * 3.3f, 1.4f, new Color(.7f, .8f, 1f), .4f); for (var k = 0; k < 6; k++) z.mk.Box(dark, c + fr * (.6f + k * .4f) + sd * ((k % 2) * .2f - .1f) + up * .01f, new Vector3(.08f, .01f, .2f), rot); break; }
                case "Rascal Flatts": { var s = Song(z, "Life is a Highway"); Post(c, 2.4f, .04f); z.mk.Box(SongGlow(s, new Color(.2f, .36f, .7f), .5f), c + up * 2.2f, new Vector3(.8f, .9f, .04f), rot); ArtistBits.Label(z.labels, "HWY\n∞", c + up * 2.2f + fr * .03f, fr, .16f, Cream, TextAnchor.MiddleCenter); break; }
                case "Reba McEntire": { var s = Song(z, "The Night The Lights"); life.lightsSong = s; Post(c, 3.2f, .05f); z.mk.Box(SongGlow(s, new Color(.3f, .3f, .34f), .4f), c + up * 3.3f, new Vector3(.3f, .4f, .3f), rot); SignPost(c + sd * 1.6f, "GEORGIA", Paint(new Color(.2f, .5f, .3f)), Cream); break; }
                case "Sturgill Simpson": { var s = Song(z, "Call to Arms"); Post(c, 1.2f, .04f); z.mk.Prism(SongGlow(s, new Color(.9f, .76f, .36f), .9f), c + up * 1.3f, sd, .04f, .6f, 6); z.mk.Prism(SongGlow(s, new Color(.9f, .76f, .36f), .9f), c + up * 1.3f + sd * .6f, sd, .14f, .2f, 8, 1.6f); break; }
                case "Sugarland": { var s = Song(z, "Stuck Like Glue"); Post(c, .6f, .2f); z.mk.Prism(white, c + up * .7f, up, .14f, .4f, 8); z.mk.Cone(SongGlow(s, new Color(1f, .6f, .3f), .8f), c + up * 1.1f, .1f, .2f, 6); break; }
                case "Tanya Tucker": { var s = Song(z, "Delta Dawn"); z.mk.Box(Paint(new Color(.5f, .34f, .26f)), c + up * .3f, new Vector3(.7f, .5f, .26f), rot); z.mk.Prism(leafM, c + sd * .7f, up, .015f, .5f, 3); z.mk.Blob(SongGlow(s, new Color(1f, .7f, .8f), .8f), c + sd * .7f + up * .54f, Vector3.one * .1f, rng, .3f); BoxCol(z.root, c + up * .3f, new Vector3(.7f, .6f, .3f), rot); break; }
                case "Tebey": { var s = Song(z, "Blinding Lights"); Pickup(z.root, z.mk, c, rot * Quaternion.Euler(0, 180f, 0), new Color(.16f, .16f, .2f), false); var hf = rot * Quaternion.Euler(0, 180f, 0) * Vector3.forward; foreach (var e in new[] { -.7f, .7f }) z.mk.Blob(SongGlow(s, new Color(1f, .96f, .86f), 2f), c + up * .9f + hf * 2.23f + Quaternion.Euler(0, 180f, 0) * sd * e, new Vector3(.16f, .12f, .03f), rng, 0f); halos.Add(c + up * .9f + hf * 2.6f, 1.6f, Color.white, .4f); break; }
                case "The Band Perry": { var s = Song(z, "If I Die Young"); z.mk.Prism(SongGlow(s, new Color(.6f, .7f, .9f), .3f), c + up * .01f, up, 1.2f, .02f, 14); z.mk.Box(white, c + up * .1f, new Vector3(.5f, .14f, 1.2f), rot); for (var k = 0; k < 3; k++) z.mk.Blob(white, c + up * .3f + fr * (-.3f + k * .3f), Vector3.one * .1f, rng, .3f); break; }
                case "The Charlie Daniels Band": { var s = Song(z, "The Devil Went Down"); life.devilSong = s; Stump(c, .7f); var gf = SongGlow(s, new Color(1f, .8f, .3f), 1.2f); z.mk.Blob(gf, c + up * .84f, new Vector3(.12f, .04f, .3f), rng, 0f); z.mk.Box(gf, c + up * .86f + fr * .4f, new Vector3(.03f, .02f, .36f), rot); halos.Add(c + up * 1f, 1.2f, new Color(1f, .8f, .3f), .5f); string[] duel = { "You pick up the golden fiddle. Somewhere a red glow gets nervous.", "Fire on the mountain! Run, boys, run! You play it faster than you knew you could.", "You bet your soul against nobody. You win anyway." }; var dn = 0; Spot("The golden fiddle", c + up * 1.2f, "Play the golden fiddle", new Vector3(.8f, .6f, .8f)).onUse = _ => { life.played = Time.time; Say1(c + up * 2.4f, duel[dn++ % duel.Length]); }; break; }
                case "Tim McGraw": { var s = Song(z, "Live Like You Were Dying"); var bb = c + up * .9f; foreach (var e in new[] { -1f, 1f }) Post(c + sd * e * .8f, .8f, .04f); z.mk.Prism(SongGlow(s, new Color(.5f, .3f, .2f), .4f), bb - sd * .6f, sd, .4f, 1.2f, 10); z.mk.Blob(Paint(new Color(.5f, .4f, .3f)), bb + up * .4f, new Vector3(.4f, .1f, .3f), rng, 0f); Interact.Seat(tlife.transform, bb + up * .5f, fr, "Climb on the barrel").seatHeight = .1f; BoxCol(z.root, bb, new Vector3(1.6f, 1f, .8f), rot); break; }
                case "Waylon Jennings": { var s = Song(z, "Mammas Don't"); Post(c, 1.4f, .04f); z.mk.Prism(SongGlow(s, new Color(.4f, .3f, .2f), .4f), c + up * 1.5f, up, .4f, .04f, 12); z.mk.Prism(Paint(new Color(.4f, .3f, .2f)), c + up * 1.52f, up, .18f, .2f, 10, .8f); for (var k = 0; k < 8; k++) { var a0 = k * 45f * Mathf.Deg2Rad; var a1 = (k + 1) * 45f * Mathf.Deg2Rad; var p0 = c + up * .9f + sd * Mathf.Cos(a0) * .3f + fr * (.1f + Mathf.Sin(a0) * .3f); var p1 = c + up * .9f + sd * Mathf.Cos(a1) * .3f + fr * (.1f + Mathf.Sin(a1) * .3f); z.mk.Prism(Paint(new Color(.8f, .7f, .5f)), p0, (p1 - p0).normalized, .02f, Vector3.Distance(p0, p1), 3); } break; }
                default: { if (SongIds.TryGetValue(z.a.name, out var list) && list.Length > 0) { var s = Song(z, list[0].title); Post(c, 1.4f); z.mk.Blob(SongGlow(s, Wheat, 1.6f), c + up * 1.6f, Vector3.one * .2f, rng, 0f); } break; }
            }
        }

        void Chair(Plot z, Vector3 g, Vector3 face)
        {
            var up = Vector3.up; var rot = Quaternion.LookRotation(face); z.mk.Box(woodM, g + up * .45f, new Vector3(.46f, .05f, .46f), rot); z.mk.Box(woodM, g + up * .8f - face * .21f, new Vector3(.46f, .7f, .05f), rot); foreach (var (x, zz) in new[] { (-.2f, -.2f), (.2f, -.2f), (.2f, .2f), (-.2f, .2f) }) z.mk.Box(woodM, g + up * .22f + rot * new Vector3(x, 0, zz), new Vector3(.04f, .44f, .04f), rot);
            Interact.Seat(tlife.transform, g + up * .5f, face);
        }
    }
}
