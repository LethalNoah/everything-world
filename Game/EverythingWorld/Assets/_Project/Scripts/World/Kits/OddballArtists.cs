using System.Collections.Generic;
using UnityEngine;

namespace EverythingWorld.World
{
    /// The Oddball Backroads' artists: a roadside attraction each, on a gravel pull-off with a tall hand-painted sign
    /// ("SEE THE ..."), covers on a plywood board, a plaque, a string of bulbs; songs without a piece of their own get a
    /// little hand-painted arrow sign that lights up while they play (SongSigns).
    /// - Pixies: a giant head with its top open and a little cloud of thoughts floating out over it ("Where Is My
    ///   Mind?"); a fibreglass monkey on a pole with a halo ("Monkey Gone to Heaven"); a breaking wave of blue
    ///   fibreglass ("Wave of Mutilation"); a sign that just says HEY ("Hey"); a bench facing nothing ("All I Think
    ///   About Now").
    /// - Ween: a giant cheerful sailor wading in a pond ("Ocean Man": the sky turns ocean blue); a rose as tall as a
    ///   house ("Roses Are Free"); a giant baby bottle ("She's Your Baby").
    /// - Violent Femmes: a sun in sunglasses on a pole ("Blister In The Sun": the sun blazes); giant red lips ("Kiss
    ///   Off").
    /// - Beck: a giant hand making an L ("Loser"); R.E.M.: a small chapel leaning sideways ("Losing My Religion"), a
    ///   meteor smoking in a crater with THE END IS NEAR sign ("It's The End Of The World": meteors fall); The Flaming
    ///   Lips: a pink robot on the hill ("Yoshimi"); They Might Be Giants: a giant hot dog on a stand ("Hot Dog!");
    ///   CAKE: a race car on a plinth ("The Distance"); Eels: a giant tooth ("Novocaine For The Soul"); Butthole Surfers:
    ///   a giant pepper shaker ("Pepper"); Bloodhound Gang: a giant fire hydrant with flames ("Fire Water Burn");
    ///   Wilco: two halves of a bridge that don't meet ("Too Far Apart"); MJ Lenderman: a giant wristwatch
    ///   ("Wristwatch"); Silver Jews: giant dice and a ruler ("Random Rules"); Dexter and The Moonrocks: a parking lot
    ///   sign saying LOT ("It's A Lot"); That Handsome Devil: a little red devil statue ("Charlie's Inferno"); Victor
    ///   Jones: a moon raining ("Moon Bright Rain").
    public partial class OddballKit
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
                Add("Ween", "Ocean Man", "Ween"); Add("Pixies", "Where Is My Mind", "Pixies"); Add("R.E.M.", "It's The End", "@the arrival and the head");
                Add("Violent Femmes", "Blister In The Sun", "@the arrival and the head"); Add("The Flaming Lips", "Yoshimi", "The Flaming Lips");
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
            if (asphalt == null) MakeMaterials();
            var root = new GameObject("Artist: " + a.name).transform; root.SetParent(ctx.Root, false);
            var front = facing.TryGetValue(a.name, out var f) ? f : new Vector3(center.x - a.x, 0, center.z - a.z).normalized;
            if (front.sqrMagnitude < .01f) front = Vector3.forward;
            var labels = new GameObject("Labels").transform; labels.SetParent(root, false);
            var area = root.gameObject.AddComponent<ArtistArea>();
            area.artistName = a.name; area.stationId = a.stationId; area.radius = a.r; area.glowColor = Sun;
            var z = new Plot { ctx = ctx, root = root, labels = labels, mk = new MeshKit(), solid = new MeshKit(), pictures = new MeshKit(), a = a, c = ctx.OnGround(a.x, a.z), front = front, side = Vector3.Cross(Vector3.up, front), area = area };
            var reach = Foot(a); var up = Vector3.up;
            z.mk.Prism(WorldMaterials.Solid(new Color(.62f, .58f, .5f)), z.c - up * .25f, up, reach * .62f, .32f, 16);
            AttractionSign(z);
            Attraction(z);
            SongSigns(z);
            Board(z, z.At(reach * .6f, -reach * .45f - 1.5f), front);
            ArtistBits.Plaque(z.mk, z.solid, labels, a, z.At(reach * .6f + 1.5f, reach * .45f + 1.5f), front, woodDark, wood, Ink, 1.6f);
            if (z.area.glowLight == null) z.area.glowLight = ArtistBits.PointLight(root, z.c + up * 6f + front * 4f, new Color(1f, .9f, .7f), HasStation(a) ? 22f : 16f, 1.4f);
            area.glowRenderers = new Renderer[0];
            z.mk.Build("Area", root, false);
            z.solid.BuildCollider("Walls", root);
            z.pictures.Build("Covers", root, false);
            noGrass.Add(new Vector3(z.c.x, reach * .9f, z.c.z));
            if (HasStation(a) || reach >= 10f)
            {
                var mark = new GameObject("Shot: " + a.name + " up close").transform; mark.SetParent(ctx.Root, false);
                var cam = z.At(reach * .6f, reach * .3f) + up * 1.7f; var look = z.At(-reach * .4f, 0) + up * 3f - cam; mark.SetPositionAndRotation(cam, Quaternion.LookRotation(look.normalized));
            }
        }

        /// A tall hand-painted roadside sign on two poles, "SEE THE ...", bulbs round its edge.
        void AttractionSign(Plot z)
        {
            var up = Vector3.up; var p = z.At(Foot(z.a) * .4f, Foot(z.a) * .55f); var rot = z.Rot;
            foreach (var e in new[] { -1.6f, 1.6f }) { z.mk.Prism(woodDark, p + z.side * e - up * .2f, up, .12f, 5f, 6); ArtistBits.PostCollider(z.root, p + z.side * e, 4.8f, .14f); }
            z.mk.Box(WorldMaterials.Solid(new Color(.95f, .9f, .75f)), p + up * 4f, new Vector3(4.2f, 1.6f, .1f), rot);
            var bulbs = GlowM(Sun, 2.2f);
            for (var k = 0; k < 14; k++) { var x = (k % 7 - 3f) * .6f; z.mk.Blob(bulbs, p + up * (k < 7 ? 4.85f : 3.15f) + z.side * x + z.front * .07f, Vector3.one * .06f, z.ctx.Rng, 0f); }
            halos.Add(p + up * 4f + z.front * 1f, 2.6f, Sun, .3f);
            ArtistBits.Label(z.labels, "SEE THE", p + up * 4.45f + z.front * .07f, z.front, .2f, new Color(.7f, .2f, .3f), TextAnchor.MiddleCenter);
            ArtistBits.Label(z.labels, z.a.name.ToUpperInvariant(), p + up * 3.9f + z.front * .07f, z.front, .3f, Ink, TextAnchor.MiddleCenter);
        }

        void Board(Plot z, Vector3 ground, Vector3 dir)
        {
            dir = Vector3.ProjectOnPlane(dir, Vector3.up).normalized; var rot = Quaternion.LookRotation(dir); var side = Vector3.Cross(Vector3.up, dir);
            var n = Mathf.Max(1, Mathf.Min(6, z.a.songList?.Length ?? 1)); const float size = .9f; var w = n * size * 1.25f + .6f;
            var c = ground + Vector3.up * 1.7f;
            z.mk.Box(wood, c - Vector3.up * .3f, new Vector3(w, size + 1.2f, .1f), rot); z.solid.Box(wood, c - Vector3.up * .5f, new Vector3(w, size + 1.6f, .3f), rot);
            foreach (var s in new[] { -1f, 1f }) z.mk.Box(woodDark, c + side * s * (w / 2f + .05f) - Vector3.up * 1f, new Vector3(.15f, size + 2.4f, .15f), rot);
            ArtistBits.CoverWall(z.pictures, z.labels, z.ctx, z.a, c + dir * .06f + Vector3.up * .08f, dir, n, size, n, true, Ink);
        }

        /// A little hand-painted arrow sign for each song without its own piece, lighting up while it plays.
        void SongSigns(Plot z)
        {
            if (!SongIds.TryGetValue(z.a.name, out var list)) return;
            var up = Vector3.up; var k = 0; var seen = new HashSet<string>();
            foreach (var (_, id) in list)
            {
                if (z.lit.Contains(id) || !seen.Add(id)) continue;
                var p = z.At(-Foot(z.a) * .1f - (k % 2) * 1.6f, -Foot(z.a) * .5f - (k / 2) * 1.6f);
                z.mk.Prism(woodDark, p - up * .2f, up, .05f, 1.8f, 6); ArtistBits.PostCollider(z.root, p, 1.6f, .08f);
                var m = SongGlow(id, new[] { Flamingo, Sky, Sun }[(k + z.a.name.Length) % 3], 1.4f);
                z.mk.Box(m, p + up * 1.6f + z.side * .3f, new Vector3(.8f, .3f, .04f), z.Rot); z.mk.Prism(m, p + up * 1.6f + z.side * .7f, z.side, .2f, .25f, 4, .05f, 45f);
                halos.Add(p + up * 1.6f + z.front * .4f, .9f, Sun, .4f);
                k++;
            }
        }

        // ---------------------------------------------------------------- the attractions
        void Attraction(Plot z)
        {
            var up = Vector3.up; var ctx = z.ctx; var rot = z.Rot; var sd = z.side; var c = z.At(-Foot(z.a) * .35f, 0);
            string first = null; if (SongIds.TryGetValue(z.a.name, out var l) && l.Length > 0) first = l[0].id;
            Material G(Color col, float s = 1.4f) { if (first != null) z.lit.Add(first); return SongGlow(first, col, s); }
            switch (z.a.name)
            {
                case "Pixies":
                {
                    // the open head, thoughts floating out
                    var h = z.At(-6f, 0); var mind = SongGlow(z, "Where Is My Mind", new Color(.75f, .85f, 1f), 1.2f);
                    z.mk.Box(white, h + up * 1f, new Vector3(4.4f, 2f, 4.4f), rot); z.solid.Box(white, h + up * 1f, new Vector3(4.4f, 2f, 4.4f), rot);
                    z.mk.Blob(fibre, h + up * 4.6f, new Vector3(2.8f, 2.6f, 2.6f), ctx.Rng, .05f); BoxCol(z.root, h + up * 4.4f, new Vector3(4.8f, 4.4f, 4.6f), rot);
                    z.mk.Prism(fibre, h + up * 6.8f, up, 2f, .3f, 14, 1f); z.mk.Box(fibre, h + up * 7.6f - z.front * 1.6f, new Vector3(3.6f, .3f, 2.4f), rot * Quaternion.Euler(-60f, 0, 0));   // the top swung open like a lid
                    foreach (var e in new[] { -1f, 1f }) { z.mk.Blob(white, h + up * 5.2f + z.front * 2.3f + sd * e * .9f, Vector3.one * .45f, ctx.Rng, 0f); z.mk.Blob(dark, h + up * 5.2f + z.front * 2.6f + sd * e * .9f, Vector3.one * .2f, ctx.Rng, 0f); }
                    for (var k = 0; k < 5; k++) z.mk.Blob(mind, h + up * (9f + k * .9f) + new Vector3(Mathf.Sin(k * 1.7f), 0, Mathf.Cos(k * 1.7f)) * (1f + k * .3f), Vector3.one * (.7f - k * .08f), ctx.Rng, .2f);
                    halos.Add(h + up * 10.5f, 3f, new Color(.75f, .85f, 1f), .4f); life.mindAt = h + up * 9f;
                    // the monkey with a halo
                    var m = z.At(3f, -9f); var halo = SongGlow(z, "Monkey Gone to Heaven", Sun, 2.2f); var brown = WorldMaterials.Solid(new Color(.5f, .33f, .2f));
                    z.mk.Prism(metal, m - up * .2f, up, .1f, 4f, 6); ArtistBits.PostCollider(z.root, m, 4f, .14f);
                    z.mk.Blob(brown, m + up * 4.6f, new Vector3(.7f, .9f, .6f), ctx.Rng, 0f); z.mk.Blob(brown, m + up * 5.8f, Vector3.one * .5f, ctx.Rng, 0f); z.mk.Blob(fibre, m + up * 5.7f + z.front * .35f, new Vector3(.32f, .25f, .15f), ctx.Rng, 0f);
                    foreach (var e in new[] { -1f, 1f }) { z.mk.Box(brown, m + up * 5f + sd * e * .7f, new Vector3(.9f, .2f, .2f), rot * Quaternion.Euler(0, 0, e * 30f)); z.mk.Blob(brown, m + up * 5.8f + sd * e * .55f, Vector3.one * .18f, ctx.Rng, 0f); }
                    for (var k = 0; k < 16; k++) { float a0 = k * Mathf.PI / 8f, a1 = (k + 1) * Mathf.PI / 8f; var p0 = m + up * 6.6f + new Vector3(Mathf.Cos(a0), 0, Mathf.Sin(a0)) * .55f; var p1 = m + up * 6.6f + new Vector3(Mathf.Cos(a1), 0, Mathf.Sin(a1)) * .55f; z.mk.Box(halo, (p0 + p1) / 2f, new Vector3(.08f, .08f, Vector3.Distance(p0, p1)), Quaternion.LookRotation(p1 - p0)); }
                    halos.Add(m + up * 6.6f, 1.4f, Sun, .6f);
                    // the wave
                    var w = z.At(3f, 9f); var wave = SongGlow(z, "Wave of Mutilation", Sky, .8f);
                    for (var k = 0; k < 10; k++) { var t = k / 10f; var p = w + sd * (t - .5f) * 6f + up * (Mathf.Sin(t * Mathf.PI) * 3f); z.mk.Box(wave, p + up * .5f, new Vector3(.7f, 1.4f + Mathf.Sin(t * Mathf.PI) * 2f, 2f), rot * Quaternion.Euler(-25f + t * 40f, 0, 0)); }
                    z.mk.Blob(white, w + up * 3.8f + z.front * .6f, new Vector3(1.4f, .5f, .8f), ctx.Rng, .3f); BoxCol(z.root, w + up * 1.6f, new Vector3(6.4f, 3.2f, 2.4f), rot);
                    // HEY; the bench facing nothing
                    var hey = SongGlow(z, "Hey", Flamingo, 1.8f); var hs = z.At(-1f, 12f);
                    z.mk.Prism(woodDark, hs - up * .2f, up, .08f, 2.6f, 6); ArtistBits.PostCollider(z.root, hs, 2.5f, .1f); z.mk.Box(hey, hs + up * 2.8f, new Vector3(1.8f, .8f, .08f), rot);
                    ArtistBits.Label(z.labels, "HEY", hs + up * 2.8f + z.front * .05f, z.front, .45f, Cream, TextAnchor.MiddleCenter);
                    var bench = z.At(-2f, -12f); var bm = SongGlow(z, "All I Think About Now", new Color(.95f, .9f, .7f), .6f);
                    z.mk.Box(bm, bench + up * .45f, new Vector3(1.8f, .1f, .45f), Quaternion.LookRotation(-z.front)); z.mk.Box(bm, bench + up * .8f + z.front * .22f, new Vector3(1.8f, .45f, .06f), Quaternion.LookRotation(-z.front)); BoxCol(z.root, bench + up * .45f, new Vector3(1.8f, .9f, .5f), rot);
                    break;
                }
                case "Ween":
                {
                    // the ocean man: a cheerful giant sailor wading in a pond
                    var pond = z.At(-5f, -1f); var ocean = SongGlow(z, "Ocean Man", Sky, .5f);
                    z.mk.Prism(WorldMaterials.Solid(new Color(.6f, .55f, .45f)), pond - up * .3f, up, 5.4f, .35f, 20); z.mk.Prism(ocean, pond + up * .1f, up, 5f, .02f, 24);
                    var man = pond + up * .1f; var navy = WorldMaterials.Solid(new Color(.2f, .25f, .5f));
                    z.mk.Box(navy, man + up * 2.5f, new Vector3(2f, 3f, 1.4f), rot); z.mk.Blob(fibre, man + up * 5f, new Vector3(1.1f, 1.2f, 1.1f), ctx.Rng, 0f);
                    z.mk.Box(white, man + up * 6.2f, new Vector3(1.6f, .4f, 1.6f), rot); z.mk.Box(WorldMaterials.Solid(new Color(.85f, .2f, .2f)), man + up * 4.65f + z.front * 1.02f, new Vector3(.9f, .15f, .1f), rot);
                    foreach (var e in new[] { -1f, 1f }) { z.mk.Box(navy, man + up * 3.6f + sd * e * 1.5f, new Vector3(1.6f, .45f, .45f), rot * Quaternion.Euler(0, 0, e * -35f)); z.mk.Blob(white, man + up * 5.2f + z.front * .95f + sd * e * .4f, Vector3.one * .2f, ctx.Rng, 0f); }
                    BoxCol(z.root, man + up * 2.8f, new Vector3(2.2f, 5.6f, 1.6f), rot); halos.Add(pond + up * .6f, 5f, Sky, .25f); life.oceanAt = pond;
                    var rose = z.At(3f, 9f); var red = SongGlow(z, "Roses Are Free", new Color(.95f, .2f, .3f), .9f);
                    z.mk.Prism(WorldMaterials.Solid(new Color(.25f, .45f, .2f)), rose - up * .2f, up, .25f, 6f, 6); ArtistBits.PostCollider(z.root, rose, 6f, .3f);
                    for (var k = 0; k < 7; k++) z.mk.Blob(red, rose + up * (6.6f + (k % 2) * .3f) + new Vector3(Mathf.Cos(k * .9f), 0, Mathf.Sin(k * .9f)) * (k < 4 ? .5f : .9f), new Vector3(.8f, .7f, .8f), ctx.Rng, .2f);
                    foreach (var e in new[] { -1f, 1f }) z.mk.Box(WorldMaterials.Solid(new Color(.3f, .55f, .25f)), rose + up * 3f + sd * e * .7f, new Vector3(1.2f, .1f, .6f), rot * Quaternion.Euler(0, 0, e * 25f));
                    var bottle = z.At(3f, -9f); var milk = SongGlow(z, "She's Your Baby", new Color(.95f, .95f, .9f), .7f);
                    z.mk.Prism(milk, bottle, up, 1f, 3.4f, 14); z.mk.Prism(WorldMaterials.Solid(new Color(.55f, .75f, .95f)), bottle + up * 3.4f, up, 1.05f, .5f, 14); z.mk.Prism(WorldMaterials.Solid(new Color(.95f, .75f, .55f)), bottle + up * 3.9f, up, .5f, 1f, 12, .3f);
                    ArtistBits.PostCollider(z.root, bottle, 3.6f, 1.05f);
                    break;
                }
                case "Violent Femmes":
                {
                    var sun = z.At(-5f, -3f); var blister = SongGlow(z, "Blister In The Sun", Sun, 2f);
                    z.mk.Prism(metal, sun - up * .2f, up, .12f, 5f, 6); ArtistBits.PostCollider(z.root, sun, 5f, .15f);
                    z.mk.Prism(blister, sun + up * 7f - z.front * .2f, z.front, 2f, .4f, 20);
                    for (var k = 0; k < 12; k++) { var a = k * Mathf.PI / 6f; var d = sd * Mathf.Cos(a) + up * Mathf.Sin(a); z.mk.Prism(blister, sun + up * 7f + d * 2.2f, d, .3f, 1.2f, 4, .05f); }
                    foreach (var e in new[] { -1f, 1f }) z.mk.Box(dark, sun + up * 7.4f + z.front * .22f + sd * e * .7f, new Vector3(1f, .55f, .05f), rot); z.mk.Box(dark, sun + up * 7.5f + z.front * .22f, new Vector3(.4f, .08f, .05f), rot);
                    z.mk.Box(dark, sun + up * 6.4f + z.front * .22f, new Vector3(1.2f, .12f, .05f), rot * Quaternion.Euler(0, 0, 8f));
                    halos.Add(sun + up * 7f, 4f, Sun, .5f); life.sunAt = sun + up * 7f;
                    var lips = z.At(2f, 7f); var kiss = SongGlow(z, "Kiss Off", new Color(.9f, .1f, .25f), 1.2f);
                    z.mk.Blob(kiss, lips + up * 2.2f + sd * -.6f, new Vector3(1.2f, .7f, .6f), ctx.Rng, .05f); z.mk.Blob(kiss, lips + up * 2.2f + sd * .6f, new Vector3(1.2f, .7f, .6f), ctx.Rng, .05f); z.mk.Blob(kiss, lips + up * 1.5f, new Vector3(1.6f, .7f, .6f), ctx.Rng, .05f);
                    z.mk.Box(woodDark, lips + up * .4f, new Vector3(1f, .8f, 1f), rot); BoxCol(z.root, lips + up * 1.4f, new Vector3(3.6f, 2.8f, 1.4f), rot);
                    break;
                }
                case "Beck":
                {
                    var hand = G(new Color(.95f, .78f, .62f), .5f);
                    z.mk.Box(woodDark, c + up * .5f, new Vector3(1.6f, 1f, 1.6f), rot);
                    z.mk.Box(hand, c + up * 2f, new Vector3(1.4f, 2f, .8f), rot); z.mk.Box(hand, c + up * 4f + sd * -.45f, new Vector3(.4f, 2.2f, .4f), rot); z.mk.Box(hand, c + up * 2.6f + sd * 1.3f, new Vector3(1.6f, .4f, .4f), rot);
                    BoxCol(z.root, c + up * 2.4f, new Vector3(3f, 4.8f, 1.6f), rot); break;
                }
                case "R.E.M.":
                {
                    var ch = c - z.front * 1f; var lean = rot * Quaternion.Euler(0, 0, 10f); var church = SongGlow(z, "Losing My Religion", new Color(1f, .85f, .55f), 1.4f);
                    z.mk.Box(white, ch + up * 2f + sd * .3f, new Vector3(4f, 4f, 6f), lean); z.mk.GableRoof(WorldMaterials.Solid(new Color(.3f, .3f, .35f)), ch + up * 4f + sd * .7f, new Vector3(4.6f, 0, 6.6f), 2.4f, lean);
                    z.mk.Box(church, ch + up * 2f + sd * .3f + z.front * 3.02f, new Vector3(1.2f, 2f, .05f), lean);
                    z.mk.Box(white, ch + up * 7f + sd * 1.2f + z.front * 2f, new Vector3(1f, 2.4f, 1f), lean); BoxCol(z.root, ch + up * 2f, new Vector3(4.6f, 4f, 6f), rot);
                    var met = z.At(3f, 9f); var end = SongGlow(z, "It's The End", new Color(1f, .45f, .15f), 2f);
                    z.mk.Prism(WorldMaterials.Solid(new Color(.35f, .3f, .26f)), met - up * .1f, up, 3.4f, .4f, 14, 1.2f); z.mk.Blob(dark, met + up * .9f, new Vector3(1.6f, 1.2f, 1.4f), ctx.Rng, .3f); z.mk.Blob(end, met + up * .5f, new Vector3(1.8f, .4f, 1.6f), ctx.Rng, .3f);
                    BoxCol(z.root, met + up * .9f, new Vector3(3f, 2f, 2.8f), rot); life.meteorAt = met + up * 1.5f;
                    var es = met + z.front * 4f + sd * 2f; z.mk.Prism(woodDark, ctx.OnGround(es.x, es.z) - up * .2f, up, .07f, 2.2f, 6); z.mk.Box(white, es + up * 2f, new Vector3(1.8f, .8f, .06f), rot); ArtistBits.PostCollider(z.root, ctx.OnGround(es.x, es.z), 2f, .1f);
                    ArtistBits.Label(z.labels, "THE END IS NEAR", es + up * 2f + z.front * .04f, z.front, .16f, Ink, TextAnchor.MiddleCenter);
                    break;
                }
                case "The Flaming Lips":
                {
                    var pink = G(new Color(1f, .45f, .75f), .8f); var r = c; var body = WorldMaterials.Solid(new Color(.95f, .5f, .75f));
                    foreach (var e in new[] { -1f, 1f }) { z.mk.Box(body, r + up * 1.6f + sd * e * .8f, new Vector3(.7f, 3.2f, .8f), rot); }
                    z.mk.Box(body, r + up * 4.6f, new Vector3(3f, 2.8f, 1.8f), rot); z.mk.Box(body, r + up * 6.8f, new Vector3(1.8f, 1.6f, 1.6f), rot);
                    foreach (var e in new[] { -1f, 1f }) { z.mk.Box(body, r + up * 4.4f + sd * e * 2f, new Vector3(.6f, 2.8f, .6f), rot * Quaternion.Euler(0, 0, e * 20f)); z.mk.Box(pink, r + up * 7f + z.front * .82f + sd * e * .45f, new Vector3(.4f, .3f, .04f), rot); }
                    z.mk.Prism(metal, r + up * 7.6f, up, .05f, 1f, 6); z.mk.Blob(pink, r + up * 8.7f, Vector3.one * .2f, ctx.Rng, 0f);
                    BoxCol(z.root, r + up * 3.8f, new Vector3(3.6f, 7.6f, 2f), rot); halos.Add(r + up * 7f + z.front * 1f, 2f, new Color(1f, .5f, .8f), .5f); life.robotEyes = r + up * 7f; break;
                }
                case "They Might Be Giants (For Kids)":
                {
                    var dog = c + up * 2.2f; var bun = WorldMaterials.Solid(new Color(.85f, .62f, .35f)); var sausage = G(new Color(.8f, .3f, .2f), .5f);
                    z.mk.Box(woodDark, c + up * .8f, new Vector3(1f, 1.6f, 1f), rot);
                    foreach (var e in new[] { -.45f, .45f }) z.mk.Prism(bun, dog + z.front * e - sd * 2.6f, sd, .7f, 5.2f, 12); z.mk.Prism(sausage, dog + up * .35f - sd * 3f, sd, .5f, 6f, 12);
                    for (var k = 0; k < 8; k++) z.mk.Box(WorldMaterials.Solid(Sun), dog + up * .86f + sd * ((k - 3.5f) * .6f), new Vector3(.3f, .06f, .12f), rot * Quaternion.Euler(0, 0, k % 2 == 0 ? 20f : -20f));
                    BoxCol(z.root, c + up * 1.6f, new Vector3(6f, 3.2f, 1.6f), rot); break;
                }
                case "CAKE":
                {
                    var car = c + up * 1.4f; var red = G(new Color(.85f, .15f, .15f), .5f);
                    z.mk.Box(white, c + up * .6f, new Vector3(3f, 1.2f, 5f), rot); z.mk.Box(red, car + up * .4f, new Vector3(1.8f, .5f, 4.4f), rot); z.mk.Box(red, car + up * .8f - z.front * .4f, new Vector3(.9f, .4f, 1.2f), rot);
                    z.mk.Box(red, car + up * .9f - z.front * 2f, new Vector3(2f, .1f, .6f), rot);
                    foreach (var e in new[] { -1f, 1f }) foreach (var f in new[] { -1.5f, 1.5f }) z.mk.Prism(dark, car + sd * e * 1f + z.front * f + up * .3f, sd * e, .38f, .35f, 12);
                    BoxCol(z.root, c + up * 1.2f, new Vector3(3f, 2.4f, 5f), rot); break;
                }
                case "Eels":
                {
                    var tooth = G(new Color(.98f, .97f, .92f), .6f);
                    z.mk.Box(tooth, c + up * 3.4f, new Vector3(2.4f, 2.6f, 2f), rot);
                    foreach (var e in new[] { -1f, 1f }) z.mk.Prism(tooth, c + up * 2.2f + sd * e * .6f, -up, .5f, 1.8f, 8, .3f);
                    z.mk.Prism(metal, c - up * .2f, up, .1f, 2f, 6); BoxCol(z.root, c + up * 2.6f, new Vector3(2.6f, 5.2f, 2.2f), rot); break;
                }
                case "Butthole Surfers":
                {
                    var shaker = G(new Color(.92f, .92f, .95f), .6f);
                    z.mk.Prism(shaker, c, up, 1.1f, 3.2f, 12, .8f); z.mk.Prism(metal, c + up * 3.2f, up, .9f, .9f, 12, .4f);
                    for (var k = 0; k < 5; k++) z.mk.Blob(dark, c + up * 4.1f + new Vector3(Mathf.Cos(k * 1.25f), 0, Mathf.Sin(k * 1.25f)) * .25f, Vector3.one * .06f, ctx.Rng, 0f);
                    ArtistBits.PostCollider(z.root, c, 4f, 1.1f); break;
                }
                case "Bloodhound Gang":
                {
                    var red = WorldMaterials.Solid(new Color(.85f, .15f, .12f)); var fire = G(new Color(1f, .5f, .15f), 2.2f);
                    z.mk.Prism(red, c, up, .9f, 2.6f, 12); z.mk.Blob(red, c + up * 2.7f, Vector3.one * .9f, ctx.Rng, 0f); foreach (var e in new[] { -1f, 1f }) z.mk.Prism(red, c + up * 1.6f + sd * e * .9f, sd * e, .35f, .5f, 8);
                    for (var k = 0; k < 5; k++) z.mk.Prism(fire, c + up * 3.4f + new Vector3(Mathf.Cos(k * 1.25f), 0, Mathf.Sin(k * 1.25f)) * .35f, up, .25f, 1.2f + (k % 2) * .6f, 5, .05f);
                    ArtistBits.PostCollider(z.root, c, 3.4f, 1f); halos.Add(c + up * 4f, 2f, new Color(1f, .5f, .15f), .6f); break;
                }
                case "Wilco":
                {
                    var apart = G(Sky, .6f);
                    foreach (var e in new[] { -1f, 1f })
                    {
                        var base_ = c + sd * e * 5f; z.mk.Box(wood, base_ + up * 1.4f, new Vector3(1f, 2.8f, 2.6f), rot); BoxCol(z.root, base_ + up * 1.4f, new Vector3(1f, 2.8f, 2.6f), rot);
                        z.mk.Box(apart, base_ + up * 2.9f - sd * e * 2f, new Vector3(4f, .2f, 2.4f), rot * Quaternion.Euler(0, 0, e * 6f)); z.solid.Box(wood, base_ + up * 2.9f - sd * e * 2f, new Vector3(4f, .3f, 2.4f), rot * Quaternion.Euler(0, 0, e * 6f));
                        foreach (var f in new[] { -1.15f, 1.15f }) z.mk.Box(woodDark, base_ + up * 3.5f - sd * e * 2f + z.front * f, new Vector3(4f, .08f, .08f), rot * Quaternion.Euler(0, 0, e * 6f));
                    }
                    break;
                }
                case "MJ Lenderman":
                {
                    var face = G(new Color(.98f, .95f, .85f), .8f); var gold = WorldMaterials.Solid(new Color(.85f, .7f, .3f), .9f);
                    z.mk.Box(woodDark, c + up * 1.2f, new Vector3(1.6f, 2.4f, .6f), rot);
                    z.mk.Prism(gold, c + up * 3.6f - z.front * .2f, z.front, 1.6f, .5f, 20); z.mk.Prism(face, c + up * 3.6f + z.front * .3f, z.front, 1.4f, .04f, 20);
                    z.mk.Box(dark, c + up * 4f + z.front * .36f, new Vector3(.1f, .9f, .02f), rot); z.mk.Box(dark, c + up * 3.6f + z.front * .36f + sd * .3f, new Vector3(.6f, .08f, .02f), rot * Quaternion.Euler(0, 0, 30f));
                    foreach (var e in new[] { -1f, 1f }) z.mk.Box(WorldMaterials.Solid(new Color(.35f, .2f, .12f)), c + up * (3.6f + e * 2.2f), new Vector3(1.4f, 1.6f, .4f), rot);
                    BoxCol(z.root, c + up * 3f, new Vector3(3.4f, 6f, 1.2f), rot); break;
                }
                case "Silver Jews":
                {
                    var dice = G(new Color(.95f, .95f, .92f), .5f);
                    foreach (var (o, yaw) in new[] { (-1.3f, 15f), (1.2f, -30f) }) { var d = c + sd * o + up * 1f; var dr = Quaternion.Euler(0, yaw, 0) * rot; z.mk.Box(dice, d, Vector3.one * 2f, dr); BoxCol(z.root, d, Vector3.one * 2f, dr); for (var k = 0; k < 3; k++) z.mk.Prism(dark, d + up * 1.01f + dr * new Vector3((k - 1) * .5f, 0, (k - 1) * .5f), up, .18f, .02f, 10); }
                    var ruler = c + z.front * 3f; z.mk.Box(WorldMaterials.Solid(Sun), ruler + up * .15f, new Vector3(6f, .3f, .8f), rot);
                    for (var k = 0; k < 12; k++) z.mk.Box(dark, ruler + up * .31f + sd * ((k - 5.5f) * .5f) + z.front * .25f, new Vector3(.04f, .02f, k % 2 == 0 ? .3f : .15f), rot);
                    break;
                }
                case "Dexter and The Moonrocks":
                {
                    var sign = G(new Color(.3f, .6f, 1f), 1.4f); z.mk.Prism(metal, c - up * .2f, up, .08f, 3.4f, 6); ArtistBits.PostCollider(z.root, c, 3.2f, .1f);
                    z.mk.Box(sign, c + up * 3.6f, new Vector3(1.6f, 1.6f, .1f), rot); ArtistBits.Label(z.labels, "LOT", c + up * 3.6f + z.front * .06f, z.front, .5f, Cream, TextAnchor.MiddleCenter);
                    for (var k = 0; k < 4; k++) z.mk.Box(white, c + z.front * 3f + sd * ((k - 1.5f) * 2.6f) + up * .03f, new Vector3(.15f, .02f, 4f), rot);
                    break;
                }
                case "That Handsome Devil":
                {
                    var red = G(new Color(.85f, .15f, .12f), .7f);
                    z.mk.Box(woodDark, c + up * .5f, new Vector3(1.2f, 1f, 1.2f), rot); z.mk.Box(red, c + up * 1.6f, new Vector3(.7f, 1.2f, .5f), rot); z.mk.Blob(red, c + up * 2.5f, Vector3.one * .35f, ctx.Rng, 0f);
                    foreach (var e in new[] { -1f, 1f }) z.mk.Prism(red, c + up * 2.7f + sd * e * .2f, (up + sd * e * .4f).normalized, .1f, .4f, 5, .05f);
                    z.mk.Prism(red, c + up * 1.2f - z.front * .3f, (-z.front - up * .3f).normalized, .05f, .9f, 5, .3f);
                    BoxCol(z.root, c + up * 1.4f, new Vector3(1.2f, 2.8f, 1.2f), rot); break;
                }
                case "Victor Jones":
                {
                    var moon = G(new Color(.95f, .95f, .85f), 1.4f); z.mk.Prism(metal, c - up * .2f, up, .06f, 3f, 6); ArtistBits.PostCollider(z.root, c, 3f, .1f);
                    z.mk.Blob(moon, c + up * 4f, Vector3.one * .8f, ctx.Rng, 0f);
                    for (var k = 0; k < 10; k++) z.mk.Box(WorldMaterials.Solid(Sky), c + up * (2.6f + (k % 4) * .3f) + sd * ((k - 4.5f) * .2f), new Vector3(.03f, .3f, .03f), rot);
                    halos.Add(c + up * 4f, 1.6f, new Color(.95f, .95f, .85f), .5f); break;
                }
                default: JunkSculpture(ctx, z.mk, c); break;
            }
        }
    }
}
