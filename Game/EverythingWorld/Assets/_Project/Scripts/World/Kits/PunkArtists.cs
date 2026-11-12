using System.Collections.Generic;
using UnityEngine;

namespace EverythingWorld.World
{
    /// The Punk Badlands' artists. Each stands on a slab of cracked driveway with a graffiti wall behind (checkerboard
    /// floors for the ska bands), covers on a plywood board, a plaque, a streetlight; songs without a piece of their own
    /// get a tag sprayed on a breeze-block post that glows while they play (SongTags).
    /// - Green Day (the largest): a suburban house whose garage has become a venue, door rolled up on a stage with amps
    ///   and a drum kit; a giant clock ("Good Riddance (Time of Your Life)"); a big old television with a red heart on
    ///   its screen ("American Idiot"); a calendar on a post turned to September, leaves under it ("Wake Me up When
    ///   September Ends": autumn leaves fall and the sky turns gold); a short boulevard of streetlights, half of them
    ///   broken ("Boulevard of Broken Dreams": the land dims and the lamps flicker); a padded box ("Basket Case"); a flag
    ///   on a pole ("Holiday").
    /// - Fall Out Boy: a half-pipe arena, a big wooden half-pipe with bleachers either side; a wall of polaroids ("Thnks
    ///   fr th Mmrs"); a slide ("Sugar, We're Goin Down"); a little dance floor under a mirror ball ("Dance, Dance"); a
    ///   patch of corn with a light shining down on it ("Heaven, Iowa"); a doorway full of light ("Love From The Other
    ///   Side").
    /// - Panic! At The Disco: a striped cabaret tent with a marquee of bulbs; a ladder up into the sky with balloons
    ///   tied to it ("High Hopes": balloons rise); a wedding arch ("I Write Sins Not Tragedies"); a lounge piano under a
    ///   spotlight ("Death of a Bachelor"); a clock stopped at nine ("Nine in the Afternoon").
    /// - The Offspring: a battered house front ("The Kids Aren't Alright"), a road sign pointing far away ("You're
    ///   Gonna Go Far, Kid"), a lowrider ("Pretty Fly"), a HELP WANTED sign ("Why Don't You Get A Job").
    /// - Others: My Chemical Romance a marching drum and black banners ("Welcome to the Black Parade": the sky darkens);
    ///   blink-182 a birthday cake ("What's My Age Again?"); Goldfinger red balloons ("99 Red Balloons": red balloons
    ///   float up over the land); Bowling For Soup a stack of old TVs ("1985"); Jimmy Eat World a mic stand alone in a
    ///   circle ("The Middle"); Plain White T's an acoustic guitar on a stoop ("Hey There Delilah"); The Clash a
    ///   boombox ("Rock the Casbah"); the ska bands checkerboard floors and horns.
    public partial class PunkKit
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
                Add("Green Day", "Wake Me up When September Ends", "Green Day"); Add("Green Day", "Holiday / Boulevard", "Green Day");
                Add("Goldfinger", "99 Red Balloons", "@the arrival and the pool"); Add("My Chemical Romance", "Welcome to the Black Parade", "My Chemical Romance");
                Add("Panic! At The Disco", "High Hopes", "Panic! At The Disco");
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
            if (concrete == null) MakeMaterials();
            var root = new GameObject("Artist: " + a.name).transform; root.SetParent(ctx.Root, false);
            var front = facing.TryGetValue(a.name, out var f) ? f : new Vector3(center.x - a.x, 0, center.z - a.z).normalized;
            if (front.sqrMagnitude < .01f) front = Vector3.forward;
            var labels = new GameObject("Labels").transform; labels.SetParent(root, false);
            var area = root.gameObject.AddComponent<ArtistArea>();
            area.artistName = a.name; area.stationId = a.stationId; area.radius = a.r; area.glowColor = Hot;
            var z = new Plot { ctx = ctx, root = root, labels = labels, mk = new MeshKit(), solid = new MeshKit(), pictures = new MeshKit(), a = a, c = ctx.OnGround(a.x, a.z), front = front, side = Vector3.Cross(Vector3.up, front), area = area };
            var reach = Foot(a); var up = Vector3.up;
            if (Ska.Contains(a.name))
            {
                // a checkerboard floor
                const int n = 8; var cell = reach * 1.2f / n;
                for (var i = 0; i < n; i++) for (var j = 0; j < n; j++) z.mk.Box((i + j) % 2 == 0 ? white : dark, z.c + z.side * ((i - n / 2f + .5f) * cell) + z.front * ((j - n / 2f + .5f) * cell) - up * .1f, new Vector3(cell, .3f, cell), z.Rot);
            }
            else z.mk.Box(concrete, z.c - up * .15f, new Vector3(reach * 1.2f, .4f, reach * 1.2f), z.Rot);
            if (reach < 10f) GraffitiWall(ctx, z.mk, z.At(-reach * .62f, 0), front, reach * 1.1f, a.name.Length * 13);
            switch (a.name)
            {
                case "Green Day": GreenDay(z); break;
                case "Fall Out Boy": FallOutBoy(z); break;
                case "Panic! At The Disco": Panic(z); break;
                case "The Offspring": Offspring(z); break;
                default: Others(z); break;
            }
            SongTags(z);
            Board(z, z.At(reach * .55f, -reach * .45f - 1.5f), front);
            ArtistBits.Plaque(z.mk, z.solid, labels, a, z.At(reach * .55f + 1.5f, reach * .45f + 1.5f), front, woodDark, wood, Ink, 1.6f);
            if (z.area.glowLight == null) z.area.glowLight = ArtistBits.PointLight(root, z.c + up * 5f + front * 4f, new Color(1f, .9f, .75f), HasStation(a) ? 22f : 15f, 1.4f);
            area.glowRenderers = new Renderer[0];
            z.mk.Build("Area", root, false);
            z.solid.BuildCollider("Walls", root);
            z.pictures.Build("Covers", root, false);
            noGrass.Add(new Vector3(z.c.x, reach * .9f, z.c.z));
            if (HasStation(a) || reach >= 11f)
            {
                var mark = new GameObject("Shot: " + a.name + " up close").transform; mark.SetParent(ctx.Root, false);
                var cam = z.At(reach * .55f, reach * .3f) + up * 1.7f; var look = z.At(-reach * .4f, 0) + up * 2.5f - cam; mark.SetPositionAndRotation(cam, Quaternion.LookRotation(look.normalized));
            }
        }

        /// The covers on a plywood board nailed between two posts, a sticker or two.
        void Board(Plot z, Vector3 ground, Vector3 dir)
        {
            dir = Vector3.ProjectOnPlane(dir, Vector3.up).normalized; var rot = Quaternion.LookRotation(dir); var side = Vector3.Cross(Vector3.up, dir);
            var n = Mathf.Max(1, Mathf.Min(7, z.a.songList?.Length ?? 1)); const float size = .9f; var w = n * size * 1.25f + .6f;
            var c = ground + Vector3.up * 1.7f;
            z.mk.Box(wood, c - Vector3.up * .3f, new Vector3(w, size + 1.2f, .1f), rot); z.solid.Box(wood, c - Vector3.up * .5f, new Vector3(w, size + 1.6f, .3f), rot);
            foreach (var s in new[] { -1f, 1f }) z.mk.Box(woodDark, c + side * s * (w / 2f + .05f) - Vector3.up * 1f, new Vector3(.15f, size + 2.4f, .15f), rot);
            z.mk.Box(Paint(z.a.name.Length), c + side * (w / 2f - .4f) + Vector3.up * (size / 2f + .35f) + dir * .06f, new Vector3(.5f, .3f, .01f), rot * Quaternion.Euler(0, 0, 12f));
            ArtistBits.CoverWall(z.pictures, z.labels, z.ctx, z.a, c + dir * .06f + Vector3.up * .08f, dir, n, size, n, true, Ink);
        }

        /// A tag sprayed on a breeze-block post for each song without its own piece, glowing while it plays.
        void SongTags(Plot z)
        {
            if (!SongIds.TryGetValue(z.a.name, out var list)) return;
            var up = Vector3.up; var k = 0; var seen = new HashSet<string>();
            foreach (var (_, id) in list)
            {
                if (z.lit.Contains(id) || !seen.Add(id)) continue;
                var p = z.At(-Foot(z.a) * .1f - (k % 2) * 1.6f, Foot(z.a) * .5f + (k / 2) * 1.6f);
                z.mk.Box(concreteDark, p + up * .8f, new Vector3(.6f, 1.6f, .6f), z.Rot); BoxCol(z.root, p + up * .8f, new Vector3(.6f, 1.6f, .6f), z.Rot);
                var col = Spray[(k + z.a.name.Length) % Spray.Length];
                z.mk.Box(SongGlow(id, col, 1.6f), p + up * 1.1f + z.front * .31f, new Vector3(.5f, .5f, .02f), z.Rot * Quaternion.Euler(0, 0, 15f));
                halos.Add(p + up * 1.1f + z.front * .6f, .8f, col, .45f);
                k++;
            }
        }

        // ---------------------------------------------------------------- Green Day
        void GreenDay(Plot z)
        {
            var up = Vector3.up; var ctx = z.ctx; var rot = z.Rot; var sd = z.side;
            // the house, its garage a venue: door rolled up, a stage inside with amps and drums, string lights
            var h = z.At(-9f, -2f); const float W = 14f, D = 9f, H = 4.2f;
            var wall = WorldMaterials.Solid(new Color(.86f, .82f, .72f));
            z.mk.Box(wall, h + up * H / 2f - sd * 3.5f, new Vector3(7f, H, D), rot); z.solid.Box(wall, h + up * H / 2f - sd * 3.5f, new Vector3(7f, H, D), rot);
            z.mk.GableRoof(woodDark, h + up * H - sd * 3.5f, new Vector3(D + .8f, 0, 7.8f), 2f, Quaternion.LookRotation(sd));
            z.mk.Box(GlowM(new Color(1f, .85f, .55f), 1.4f), h + up * 2f - sd * 3.5f + z.front * (D / 2f + .02f), new Vector3(1.6f, 1.2f, .04f), rot);
            var g = h + sd * 3.5f;
            foreach (var e in new[] { -1f, 1f }) { z.mk.Box(wall, g + sd * e * 3.4f + up * H / 2f, new Vector3(.3f, H, D), rot); z.solid.Box(wall, g + sd * e * 3.4f + up * H / 2f, new Vector3(.3f, H, D), rot); }
            z.mk.Box(wall, g - z.front * (D / 2f - .15f) + up * H / 2f, new Vector3(7f, H, .3f), rot); z.solid.Box(wall, g - z.front * (D / 2f - .15f) + up * H / 2f, new Vector3(7f, H, .3f), rot);
            z.mk.Box(wall, g + up * (H + .15f), new Vector3(7.1f, .3f, D), rot);
            z.mk.Box(metal, g + z.front * (D / 2f - .2f) + up * (H - .4f), new Vector3(6.6f, .6f, .4f), rot);   // the rolled-up door
            z.mk.Box(woodDark, g + up * .3f - z.front * 1f, new Vector3(6.4f, .6f, 6f), rot); z.solid.Box(woodDark, g + up * .3f - z.front * 1f, new Vector3(6.4f, .6f, 6f), rot);
            foreach (var e in new[] { -2.2f, 2.2f }) { z.mk.Box(dark, g + up * 1.4f + sd * e - z.front * 3f, new Vector3(1.2f, 1.6f, .7f), rot); z.mk.Box(WorldMaterials.Solid(new Color(.2f, .2f, .2f)), g + up * 1.4f + sd * e - z.front * 2.64f, new Vector3(1f, 1.3f, .02f), rot); }
            z.mk.Prism(WorldMaterials.Solid(new Color(.85f, .15f, .15f)), g + up * .9f - z.front * 2.6f - up * .0f, z.front, .45f, .5f, 14);
            for (var k = 0; k < 14; k++) z.mk.Blob(GlowM(Sun, 2.4f), g + up * (H - .4f) + sd * ((k - 6.5f) * .48f) - z.front * (1f + Mathf.Sin(k) * .4f), Vector3.one * .07f, ctx.Rng, 0f);
            halos.Add(g + up * 2f + z.front * 3f, 5f, Sun, .25f);
            ArtistBits.PointLight(z.root, g + up * 3f + z.front * 2f, new Color(1f, .8f, .5f), 14f, 2f);
            // "Good Riddance (Time of Your Life)": a giant clock
            var clock = z.At(6f, -11f); var face = SongGlow(z, "Good Riddance", new Color(1f, .97f, .9f), .9f);
            z.mk.Box(woodDark, clock + up * 1.6f, new Vector3(.3f, 3.2f, .3f), rot); ArtistBits.PostCollider(z.root, clock, 3f, .2f);
            z.mk.Prism(dark, clock + up * 4.2f - z.front * .1f, z.front, 1.6f, .2f, 24); z.mk.Prism(face, clock + up * 4.2f + z.front * .1f, z.front, 1.4f, .03f, 24);
            z.mk.Box(dark, clock + up * 4.6f + z.front * .15f, new Vector3(.08f, .8f, .02f), rot); z.mk.Box(dark, clock + up * 4.2f + z.front * .15f + sd * .3f, new Vector3(.6f, .08f, .02f), rot * Quaternion.Euler(0, 0, 25f));
            // "American Idiot": a big old television, a red heart on its screen
            var tv = z.At(-2f, 11f); var heart = SongGlow(z, "American Idiot", new Color(1f, .15f, .15f), 2f);
            z.mk.Box(woodDark, tv + up * 1.2f, new Vector3(2.6f, 2.2f, 1.6f), rot); BoxCol(z.root, tv + up * 1.2f, new Vector3(2.6f, 2.2f, 1.6f), rot);
            z.mk.Box(WorldMaterials.Solid(new Color(.2f, .22f, .25f)), tv + up * 1.3f + z.front * .81f, new Vector3(1.9f, 1.5f, .02f), rot);
            foreach (var e in new[] { -1f, 1f }) z.mk.Box(heart, tv + up * 1.3f + z.front * .83f + sd * e * .18f, new Vector3(.14f, .7f, .02f), rot * Quaternion.Euler(0, 0, e * 40f));
            foreach (var e in new[] { -1f, 1f }) z.mk.Box(heart, tv + up * 1.6f + z.front * .83f + sd * e * .22f, new Vector3(.32f, .14f, .02f), rot * Quaternion.Euler(0, 0, e * -20f));
            halos.Add(tv + up * 1.3f + z.front * 1.4f, 1.6f, new Color(1f, .2f, .2f), .5f);
            // "Wake Me up When September Ends": a calendar on a post turned to September
            var cal = z.At(7f, 7f); var sept = SongGlow(z, "Wake Me up When September Ends", new Color(1f, .6f, .2f), 1.4f);
            z.mk.Prism(woodDark, cal - up * .1f, up, .08f, 2f, 6); ArtistBits.PostCollider(z.root, cal, 2f, .1f);
            z.mk.Box(white, cal + up * 2.4f, new Vector3(1.2f, 1.4f, .04f), rot); z.mk.Box(sept, cal + up * 2.9f + z.front * .03f, new Vector3(1.2f, .35f, .02f), rot);
            ArtistBits.Label(z.labels, "SEPTEMBER", cal + up * 2.9f + z.front * .04f, z.front, .14f, Ink, TextAnchor.MiddleCenter);
            for (var k = 0; k < 8; k++) z.mk.Blob(WorldMaterials.Solid(new Color(.9f, .45f + k * .03f, .15f)), ctx.OnGround(cal.x + ctx.Range(-1.5f, 1.5f), cal.z + ctx.Range(-1.5f, 1.5f)) + up * .02f, new Vector3(.3f, .02f, .2f), ctx.Rng, .3f);
            life.september = cal + up * 3f;
            // "Holiday / Boulevard of Broken Dreams": a short boulevard of streetlights, half of them broken
            var bl = Song(z, "Holiday / Boulevard"); var lamp = SongGlow(bl, new Color(1f, .9f, .7f), 1.6f); life.boulevard = lamp;
            for (var k = 0; k < 4; k++) foreach (var e in new[] { -1f, 1f })
            {
                var lp = z.At(-2f + k * 3.2f, -14f + e * 2f); z.mk.Prism(metal, lp - up * .2f, up, .08f, 4.2f, 6); ArtistBits.PostCollider(z.root, lp, 4f, .1f);
                z.mk.Box(metal, lp + up * 4f - sd * e * .4f, new Vector3(.06f, .06f, .9f), Quaternion.LookRotation(sd));
                var broken = (k + (e > 0 ? 1 : 0)) % 2 == 0;
                z.mk.Box(broken ? dark : lamp, lp + up * 3.9f - sd * e * .8f, new Vector3(.4f, .1f, .3f), rot);
                if (!broken) halos.Add(lp + up * 3.6f - sd * e * .8f, 1.6f, new Color(1f, .9f, .7f), .5f);
            }
            // "Basket Case": a padded box; "Holiday": a flag on a pole
            var pad = z.At(-1f, -6.5f); var padM = SongGlow(z, "Basket Case", new Color(.95f, .93f, .88f), .5f);
            z.mk.Box(padM, pad + up * .9f, new Vector3(1.8f, 1.8f, 1.8f), rot); BoxCol(z.root, pad + up * .9f, new Vector3(1.8f, 1.8f, 1.8f), rot);
            for (var k = 0; k < 9; k++) z.mk.Box(WorldMaterials.Solid(new Color(.8f, .78f, .72f)), pad + up * (.4f + (k / 3) * .5f) + z.front * .91f + sd * ((k % 3 - 1) * .5f), new Vector3(.12f, .12f, .02f), rot);
            var flag = z.At(4f, 11f); var hol = SongGlow(z, "Holiday", new Color(1f, .25f, .25f), .9f);
            z.mk.Prism(metal, flag - up * .2f, up, .07f, 6.4f, 6); ArtistBits.PostCollider(z.root, flag, 6f, .1f);
            z.mk.Box(hol, flag + up * 5.6f + sd * .9f, new Vector3(1.8f, 1.1f, .03f), rot);
        }

        // ---------------------------------------------------------------- Fall Out Boy
        void FallOutBoy(Plot z)
        {
            var up = Vector3.up; var ctx = z.ctx; var rot = z.Rot; var sd = z.side;
            // the half-pipe: two curved walls facing each other across a flat, plywood on frames, with bleachers
            var hp = z.At(-6f, 0); const float W = 8f, R = 3.2f, Flat = 4f;
            for (var s = -1; s <= 1; s += 2)
            {
                // the curved wall as smooth strips, curving up from the flat to the deck
                Vector3 P(float a) => hp + sd * s * (Flat / 2f + Mathf.Sin(a) * R) + up * (R - Mathf.Cos(a) * R);
                for (var k = 0; k < 10; k++)
                {
                    float a0 = k * Mathf.PI / 20f, a1 = (k + 1) * Mathf.PI / 20f;
                    var p0 = P(a0); var p1 = P(a1); var fw = z.front * (W / 2f);
                    var inside = hp + up * R * 1.5f;
                    z.mk.Quad(wood, p0 - fw, p0 + fw, p1 + fw, p1 - fw, (p0 + p1) / 2f - (inside - (p0 + p1) / 2f));
                    z.mk.Quad(woodDark, p0 - fw, p1 - fw, p1 + fw, p0 + fw, inside);
                    z.solid.Quad(wood, p0 - fw, p0 + fw, p1 + fw, p1 - fw, (p0 + p1) / 2f - (inside - (p0 + p1) / 2f));
                    if (k % 3 == 0) foreach (var e in new[] { -1f, 1f }) z.mk.Box(woodDark, new Vector3(p0.x, (p0.y + hp.y) / 2f, p0.z) + z.front * e * (W / 2f - .1f), new Vector3(.12f, p0.y - hp.y + .1f, .12f), rot);
                }
                var deck = hp + sd * s * (Flat / 2f + R + .8f) + up * R;
                z.mk.Box(woodDark, deck, new Vector3(1.8f, .15f, W), rot); z.solid.Box(woodDark, deck - up * R / 2f, new Vector3(1.8f, R, W), rot);
                z.mk.Box(metal, hp + sd * s * (Flat / 2f + R) + up * R, new Vector3(.12f, .12f, W), rot);
                z.mk.Box(woodDark, deck - up * R / 2f + sd * s * .9f, new Vector3(.1f, R, W), rot);
                // bleachers beyond the deck
                for (var r = 0; r < 3; r++) { var b = deck + sd * s * (2f + r * 1f) - up * (R - .4f - r * .5f); z.mk.Box(metal, b, new Vector3(1f, .1f, W), rot); z.solid.Box(metal, b - up * .2f, new Vector3(1f, .5f + r * .5f, W), rot); }
            }
            z.mk.Box(wood, hp + up * .03f, new Vector3(Flat + .1f, .06f, W), rot);
            for (var k = 0; k < 4; k++) z.mk.Box(Paint(k + 2), hp + sd * ((k - 1.5f) * 1.2f) + up * .07f, new Vector3(.8f, .02f, 1.4f), rot * Quaternion.Euler(0, k * 30f, 0));
            ArtistBits.PointLight(z.root, hp + up * 6f, new Color(1f, .95f, .85f), 18f, 1.4f);
            // "Thnks fr th Mmrs": a wall of polaroids
            var wall = z.At(-12f, 0) ; var mem = SongGlow(z, "Thnks fr th Mmrs", new Color(1f, .9f, .75f), .7f);
            z.mk.Box(woodDark, wall + up * 1.6f, new Vector3(6f, 3.2f, .2f), rot); BoxCol(z.root, wall + up * 1.6f, new Vector3(6f, 3.2f, .3f), rot);
            for (var k = 0; k < 18; k++) { var p = wall + up * (.6f + (k / 6) * .9f) + sd * ((k % 6 - 2.5f) * .9f) + z.front * .11f; z.mk.Box(white, p, new Vector3(.62f, .72f, .01f), rot * Quaternion.Euler(0, 0, ((k * 37) % 17) - 8f)); z.mk.Box(k % 4 == 0 ? mem : Paint(k), p + up * .05f + z.front * .01f, new Vector3(.5f, .48f, .01f), rot * Quaternion.Euler(0, 0, ((k * 37) % 17) - 8f)); }
            // "Sugar, We're Goin Down": a playground slide
            var sl = z.At(2f, 10f); var slide = SongGlow(z, "Sugar", new Color(1f, .6f, .85f), .9f);
            foreach (var e in new[] { -.4f, .4f }) z.mk.Box(metal, sl + up * 1.2f + sd * e - z.front * 1.2f, new Vector3(.06f, 2.4f, .06f), rot);
            z.mk.Box(slide, sl + up * 1.2f + z.front * .5f, new Vector3(.8f, .06f, 3.6f), rot * Quaternion.Euler(35f, 0, 0)); BoxCol(z.root, sl + up * 1.1f, new Vector3(1f, 2.4f, 3.4f), rot);
            // "Dance, Dance": a dance floor under a mirror ball
            var df = z.At(4f, -10f); var dance = SongGlow(z, "Dance, Dance", Sky, 1.2f);
            for (var i = 0; i < 4; i++) for (var j = 0; j < 4; j++) z.mk.Box((i + j) % 2 == 0 ? dance : GlowM(Hot, 1f), df + sd * (i - 1.5f) + z.front * (j - 1.5f) + up * .06f, new Vector3(.98f, .1f, .98f), rot);
            z.mk.Prism(metal, df - up * .1f + sd * 2.3f, up, .05f, 3.4f, 6); z.mk.Box(metal, df + up * 3.3f + sd * 1.2f, new Vector3(2.4f, .05f, .05f), Quaternion.LookRotation(sd)); ArtistBits.PostCollider(z.root, df + sd * 2.3f, 3.2f, .08f);
            z.mk.Blob(WorldMaterials.Solid(new Color(.85f, .88f, .95f), .95f), df + up * 2.8f, Vector3.one * .35f, ctx.Rng, 0f); halos.Add(df + up * 2.8f, 1.4f, Color.white, .6f);
            // "Heaven, Iowa": a patch of corn with light coming down on it
            var corn = z.At(-2f, 12f); var heaven = SongGlow(z, "Heaven, Iowa", new Color(1f, .95f, .7f), .8f);
            for (var k = 0; k < 20; k++) { var p = ctx.OnGround(corn.x + ctx.Range(-1.6f, 1.6f), corn.z + ctx.Range(-1.6f, 1.6f)); z.mk.Prism(WorldMaterials.Solid(new Color(.55f, .6f, .25f)), p, up, .04f, ctx.Range(1.6f, 2.2f), 4); z.mk.Box(WorldMaterials.Solid(new Color(.95f, .8f, .3f)), p + up * 1.4f, new Vector3(.08f, .3f, .08f), Quaternion.identity); }
            z.mk.Prism(heaven, corn + up * 2.4f, up, 1.8f, 8f, 10, .6f, 0f, false); BoxCol(z.root, corn + up * 1f, new Vector3(3.2f, 2f, 3.2f), Quaternion.identity);
            // "Love From The Other Side": a doorway full of light
            var dw = z.At(-1f, -12f); var other = SongGlow(z, "Love From The Other Side", new Color(.9f, .95f, 1f), 1.6f);
            foreach (var e in new[] { -1f, 1f }) { z.mk.Box(white, dw + sd * e * .7f + up * 1.2f, new Vector3(.2f, 2.4f, .3f), rot); BoxCol(z.root, dw + sd * e * .7f + up * 1.2f, new Vector3(.2f, 2.4f, .3f), rot); }
            z.mk.Box(white, dw + up * 2.5f, new Vector3(1.6f, .2f, .3f), rot); z.mk.Box(other, dw + up * 1.2f, new Vector3(1.2f, 2.3f, .04f), rot);
            halos.Add(dw + up * 1.2f + z.front * .8f, 1.8f, new Color(.9f, .95f, 1f), .55f);
        }

        // ---------------------------------------------------------------- Panic! At The Disco
        void Panic(Plot z)
        {
            var up = Vector3.up; var ctx = z.ctx; var rot = z.Rot; var sd = z.side;
            // the cabaret tent: striped walls, a peaked top, a stage at the back, a marquee of bulbs over the door
            var t = z.At(-7f, 0); const float R = 6f;
            for (var k = 0; k < 16; k++)
            {
                float a0 = k * Mathf.PI / 8f, a1 = (k + 1) * Mathf.PI / 8f; var am = (a0 + a1) / 2f; var d = new Vector3(Mathf.Cos(am), 0, Mathf.Sin(am));
                if (Vector3.Dot(d, z.front) > .8f) continue;   // the door
                var p0 = t + new Vector3(Mathf.Cos(a0), 0, Mathf.Sin(a0)) * R; var p1 = t + new Vector3(Mathf.Cos(a1), 0, Mathf.Sin(a1)) * R;
                var m = k % 2 == 0 ? WorldMaterials.Solid(new Color(.75f, .1f, .15f)) : white;
                z.mk.Box(m, (p0 + p1) / 2f + up * 1.6f, new Vector3(.1f, 3.2f, Vector3.Distance(p0, p1) + .05f), Quaternion.LookRotation(p1 - p0)); z.solid.Box(m, (p0 + p1) / 2f + up * 1.6f, new Vector3(.2f, 3.2f, Vector3.Distance(p0, p1) + .05f), Quaternion.LookRotation(p1 - p0));
                z.mk.Tri(m, p0 + up * 3.2f, p1 + up * 3.2f, t + up * 6.2f, t);
            }
            z.mk.Prism(Paint(3), t + up * 6.2f, up, .05f, 1.2f, 4); z.mk.Box(Paint(0), t + up * 7f + sd * .4f, new Vector3(.8f, .4f, .02f), rot);
            var door = t + z.front * R; var bulbs = new Material(GlowM(Sun, 2.4f)) { name = "Marquee bulbs" }; life.marquee = bulbs;
            z.mk.Box(dark, door + up * 3.6f, new Vector3(5f, 1f, .3f), rot);
            for (var k = 0; k < 16; k++) { var x = (k - 7.5f) * .3f; z.mk.Blob(bulbs, door + up * 4.15f + sd * x + z.front * .16f, Vector3.one * .07f, ctx.Rng, 0f); z.mk.Blob(bulbs, door + up * 3.05f + sd * x + z.front * .16f, Vector3.one * .07f, ctx.Rng, 0f); }
            ArtistBits.Label(z.labels, "TONIGHT", door + up * 3.6f + z.front * .17f, z.front, .32f, Sun, TextAnchor.MiddleCenter);
            z.mk.Box(woodDark, t - z.front * 3.5f + up * .4f, new Vector3(5f, .8f, 2.4f), rot); z.solid.Box(woodDark, t - z.front * 3.5f + up * .4f, new Vector3(5f, .8f, 2.4f), rot);
            halos.Add(door + up * 3.6f + z.front * 1f, 3f, Sun, .4f); ArtistBits.PointLight(z.root, t + up * 4f, new Color(1f, .7f, .5f), 12f, 2f);
            // "High Hopes": a ladder up into the sky, balloons tied to the top
            var lad = z.At(4f, 10f); var hopes = SongGlow(z, "High Hopes", Sky, 1.2f);
            foreach (var e in new[] { -.3f, .3f }) z.mk.Box(woodDark, lad + up * 4f + sd * e, new Vector3(.08f, 8f, .08f), rot * Quaternion.Euler(-8f, 0, 0));
            for (var k = 0; k < 14; k++) z.mk.Box(woodDark, lad + up * (.4f + k * .55f) - z.front * (k * .075f), new Vector3(.6f, .06f, .06f), rot);
            ArtistBits.PostCollider(z.root, lad, 4f, .4f);
            for (var k = 0; k < 6; k++) { var b = lad + up * (8.6f + (k % 3) * .6f) - z.front * 1.1f + sd * ((k - 2.5f) * .45f); z.mk.Blob(k % 2 == 0 ? hopes : GlowM(Hot, 1.2f), b, new Vector3(.3f, .38f, .3f), ctx.Rng, 0f); }
            life.highHopes = lad + up * 8f;
            // "I Write Sins Not Tragedies": a wedding arch with flowers
            var arch = z.At(4f, -10f); var sins = SongGlow(z, "I Write Sins", new Color(1f, .85f, .9f), 1f);
            for (var k = 0; k <= 12; k++) { var a = k * Mathf.PI / 12f; z.mk.Box(white, arch + sd * Mathf.Cos(a) * 1.5f + up * (Mathf.Sin(a) * 2.6f + .1f), new Vector3(.18f, .4f, .18f), Quaternion.LookRotation(z.front, sd * -Mathf.Sin(a) + up * Mathf.Cos(a))); if (k % 2 == 0) z.mk.Blob(sins, arch + sd * Mathf.Cos(a) * 1.5f + up * (Mathf.Sin(a) * 2.6f + .1f) + z.front * .12f, Vector3.one * .18f, ctx.Rng, .2f); }
            foreach (var e in new[] { -1.5f, 1.5f }) ArtistBits.PostCollider(z.root, arch + sd * e, 1.6f, .15f);
            // "Death of a Bachelor": a lounge piano under a spotlight; "Nine in the Afternoon": a clock stopped at nine
            var pn = z.At(1f, -5f); z.mk.Box(dark, pn + up * .8f, new Vector3(1.6f, .3f, 1.1f), rot); z.mk.Box(dark, pn + up * 1.05f - z.front * .3f, new Vector3(1.6f, .3f, .4f), rot); z.mk.Box(white, pn + up * .97f + z.front * .3f, new Vector3(1.4f, .04f, .3f), rot);
            foreach (var e in new[] { -.7f, .7f }) z.mk.Box(dark, pn + up * .35f + sd * e, new Vector3(.08f, .7f, .08f), rot); BoxCol(z.root, pn + up * .6f, new Vector3(1.6f, 1.2f, 1.1f), rot);
            var spot = SongGlow(z, "Death of a Bachelor", new Color(1f, .95f, .8f), 1.2f); z.mk.Prism(spot, pn + up * 6f, -up, .3f, .2f, 10, 1.4f); halos.Add(pn + up * 1.5f, 1.8f, new Color(1f, .95f, .8f), .4f);
            var nine = SongGlow(z, "Nine in the Afternoon", new Color(1f, .9f, .6f), 1.4f); var nc = door + up * 4.9f;
            z.mk.Prism(nine, nc - z.front * .05f, z.front, .45f, .04f, 16); z.mk.Box(dark, nc + up * .17f + z.front * .01f, new Vector3(.04f, .34f, .01f), rot); z.mk.Box(dark, nc - sd * .13f + z.front * .01f, new Vector3(.26f, .04f, .01f), rot);
        }

        // ---------------------------------------------------------------- The Offspring
        void Offspring(Plot z)
        {
            var up = Vector3.up; var ctx = z.ctx; var rot = z.Rot; var sd = z.side;
            var hf = z.At(-6f, 0); var kids = SongGlow(z, "The Kids Aren't Alright", new Color(1f, .85f, .55f), 1f);
            var wall = WorldMaterials.Solid(new Color(.78f, .78f, .74f));
            z.mk.Box(wall, hf + up * 2f, new Vector3(9f, 4f, .3f), rot); z.solid.Box(wall, hf + up * 2f, new Vector3(9f, 4f, .3f), rot);
            z.mk.GableRoof(woodDark, hf + up * 4f - z.front * .5f, new Vector3(1.6f, 0, 9.4f), 1.4f, Quaternion.LookRotation(sd));
            z.mk.Box(kids, hf + up * 1.9f + z.front * .16f + sd * 2.2f, new Vector3(1.6f, 1.1f, .02f), rot); z.mk.Box(dark, hf + up * 1.9f + z.front * .16f - sd * 2.2f, new Vector3(1.6f, 1.1f, .02f), rot);
            z.mk.Box(woodDark, hf + up * 1.1f + z.front * .16f, new Vector3(1.1f, 2.2f, .02f), rot);
            for (var k = 0; k < 5; k++) z.mk.Box(Paint(k + 4), hf + up * (.6f + k * .35f) + z.front * .17f + sd * (k * .5f - 3.6f), new Vector3(1f, .3f, .01f), rot * Quaternion.Euler(0, 0, k * 11f));
            var sign = z.At(3f, 9f); var far = SongGlow(z, "You're Gonna Go Far", new Color(.3f, .8f, .4f), 1.2f);
            z.mk.Prism(metal, sign - up * .2f, up, .07f, 3.2f, 6); ArtistBits.PostCollider(z.root, sign, 3f, .1f);
            z.mk.Box(far, sign + up * 2.8f + sd * .7f, new Vector3(1.6f, .5f, .04f), rot); z.mk.Prism(far, sign + up * 2.8f + sd * 1.5f, sd, .25f, .4f, 4, .05f, 45f);
            ArtistBits.Label(z.labels, "FAR  1,000 MI", sign + up * 2.8f + sd * .7f + z.front * .03f, z.front, .14f, Ink, TextAnchor.MiddleCenter);
            var car = z.At(3f, -9f); var fly = SongGlow(z, "Pretty Fly", new Color(1f, .8f, .2f), 1.4f); var crot = Quaternion.LookRotation(sd);
            z.mk.Box(WorldMaterials.Solid(new Color(.35f, .15f, .5f), .9f), car + up * .5f, new Vector3(2f, .5f, 5f), crot); z.mk.Box(WorldMaterials.Solid(new Color(.35f, .15f, .5f), .9f), car + up * .95f - sd * .3f, new Vector3(1.8f, .45f, 2.2f), crot);
            foreach (var e in new[] { -1f, 1f }) foreach (var f in new[] { -1.6f, 1.6f }) z.mk.Prism(fly, car + z.front * e * 1.02f + sd * f + up * .3f, z.front * e, .32f, .1f, 12);
            BoxCol(z.root, car + up * .6f, new Vector3(2f, 1.2f, 5f), crot);
            var job = z.At(-1f, 11f); var help = SongGlow(z, "Why Don't You Get A Job", new Color(1f, .95f, .8f), 1.4f);
            z.mk.Box(dark, job + up * 2f, new Vector3(2.2f, .8f, .1f), rot); z.mk.Box(help, job + up * 2f + z.front * .06f, new Vector3(2f, .6f, .02f), rot);
            ArtistBits.Label(z.labels, "HELP WANTED", job + up * 2f + z.front * .08f, z.front, .2f, new Color(.8f, .1f, .1f), TextAnchor.MiddleCenter);
            foreach (var e in new[] { -1f, 1f }) { z.mk.Prism(metal, job + sd * e * 1f - up * .2f, up, .05f, 2.4f, 6); ArtistBits.PostCollider(z.root, job + sd * e, 2.2f, .08f); }
        }

        // ---------------------------------------------------------------- the others
        void Others(Plot z)
        {
            var up = Vector3.up; var ctx = z.ctx; var rot = z.Rot; var sd = z.side; var c = z.At(-2f, 0);
            string first = null; if (SongIds.TryGetValue(z.a.name, out var l) && l.Length > 0) first = l[0].id;
            Material G(Color col, float s = 1.6f) { if (first != null) z.lit.Add(first); return SongGlow(first, col, s); }
            switch (z.a.name)
            {
                case "My Chemical Romance":
                {
                    var parade = SongGlow(z, "Welcome to the Black Parade", new Color(.95f, .95f, .95f), 1f);
                    z.mk.Prism(dark, c + up * .9f - sd * .5f, sd, .5f, 1f, 16); z.mk.Prism(parade, c + up * .9f - sd * .52f, sd, .45f, .02f, 16); BoxCol(z.root, c + up * .9f, new Vector3(1.1f, 1.1f, 1.1f), rot);
                    foreach (var e in new[] { -2.2f, 2.2f }) { var p = c + sd * e - z.front * .6f; z.mk.Prism(metal, p - up * .2f, up, .05f, 4f, 6); z.mk.Box(dark, p + up * 3f + z.front * .05f, new Vector3(1f, 1.8f, .03f), rot); ArtistBits.PostCollider(z.root, p, 3.8f, .08f); }
                    life.blackParade = c; break;
                }
                case "blink-182":
                {
                    z.mk.Prism(WorldMaterials.Solid(new Color(.95f, .8f, .9f)), c, up, 1f, .8f, 16); z.mk.Prism(white, c + up * .8f, up, 1.02f, .1f, 16); BoxCol(z.root, c + up * .45f, new Vector3(2f, .9f, 2f), Quaternion.identity);
                    var flame = G(Sun, 2.4f);
                    for (var k = 0; k < 7; k++) { var a = k * Mathf.PI * 2f / 7f; var p = c + up * .9f + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * .6f; z.mk.Prism(Paint(k), p, up, .04f, .4f, 6); z.mk.Blob(flame, p + up * .47f, new Vector3(.05f, .09f, .05f), ctx.Rng, 0f); }
                    halos.Add(c + up * 1.4f, 1.6f, Sun, .5f); break;
                }
                case "Goldfinger":
                {
                    for (var k = 0; k < 9; k++) { var b = c + up * (2.6f + (k % 3) * .55f) + sd * ((k % 3 - 1) * .6f) + z.front * ((k / 3 - 1) * .5f); z.mk.Blob(G(new Color(1f, .15f, .15f), 1f), b, new Vector3(.32f, .4f, .32f), ctx.Rng, 0f); z.mk.Box(white, (b + c + up * .6f) / 2f, new Vector3(.01f, Vector3.Distance(b, c + up * .6f), .01f), Quaternion.LookRotation(z.front, (b - c - up * .6f).normalized)); }
                    z.mk.Box(concreteDark, c + up * .3f, new Vector3(.6f, .6f, .6f), rot); BoxCol(z.root, c + up * .3f, new Vector3(.6f, .6f, .6f), rot); life.balloonsAt = c + up * 3f; break;
                }
                case "Bowling For Soup":
                {
                    for (var k = 0; k < 5; k++) { var p = c + up * (.45f + (k / 3) * .85f) + sd * ((k % 3 - 1f) * .95f + (k / 3) * .45f); z.mk.Box(woodDark, p, new Vector3(.9f, .8f, .7f), rot); z.mk.Box(G(k % 2 == 0 ? Sky : Hot, 1.2f), p + z.front * .36f, new Vector3(.68f, .52f, .02f), rot); }
                    BoxCol(z.root, c + up * .85f, new Vector3(3f, 1.7f, .8f), rot); break;
                }
                case "Jimmy Eat World":
                {
                    var mid = G(new Color(1f, .95f, .85f), 1.2f);
                    for (var k = 0; k < 32; k++) { float a0 = k * Mathf.PI / 16f, a1 = (k + 1) * Mathf.PI / 16f; var p0 = c + new Vector3(Mathf.Cos(a0), 0, Mathf.Sin(a0)) * 2.6f + up * .05f; var p1 = c + new Vector3(Mathf.Cos(a1), 0, Mathf.Sin(a1)) * 2.6f + up * .05f; z.mk.Box(mid, (p0 + p1) / 2f, new Vector3(.15f, .03f, Vector3.Distance(p0, p1) + .02f), Quaternion.LookRotation(p1 - p0)); }
                    z.mk.Prism(metal, c, up, .03f, 1.6f, 6); z.mk.Box(dark, c + up * 1.65f, new Vector3(.08f, .18f, .08f), rot); ArtistBits.PostCollider(z.root, c, 1.6f, .15f); break;
                }
                case "Plain White T's":
                {
                    for (var k = 0; k < 3; k++) z.mk.Box(concrete, c + up * (.15f + k * .3f) - z.front * (k * .4f), new Vector3(2.4f, .3f, .4f), rot);
                    BoxCol(z.root, c + up * .45f - z.front * .4f, new Vector3(2.4f, .9f, 1.2f), rot);
                    var gtr = G(new Color(.95f, .75f, .45f), .6f); z.mk.Blob(gtr, c + up * .9f - z.front * .5f + sd * .5f, new Vector3(.35f, .45f, .12f), ctx.Rng, 0f); z.mk.Box(woodDark, c + up * 1.6f - z.front * .5f + sd * .5f, new Vector3(.07f, 1f, .04f), rot);
                    break;
                }
                case "The Clash":
                {
                    z.mk.Box(dark, c + up * .5f, new Vector3(1.6f, .8f, .5f), rot); BoxCol(z.root, c + up * .5f, new Vector3(1.6f, .8f, .5f), rot);
                    foreach (var e in new[] { -.5f, .5f }) z.mk.Prism(G(Lime, 1.4f), c + up * .5f + sd * e + z.front * .26f, z.front, .28f, .02f, 14);
                    z.mk.Box(metal, c + up * 1.05f, new Vector3(1.4f, .06f, .06f), rot); break;
                }
                default:
                    if (Ska.Contains(z.a.name))
                    {
                        // a horn on a stand, catching the sun
                        var horn = G(Sun, 1f); z.mk.Prism(metal, c, up, .04f, 1.2f, 6); z.mk.Prism(horn, c + up * 1.3f, (up + z.front).normalized, .06f, 1f, 10, 5f); ArtistBits.PostCollider(z.root, c, 1.2f, .15f);
                        halos.Add(c + up * 1.8f, 1.2f, Sun, .4f);
                    }
                    else { Ramp(ctx, z.mk, z.solid, c, z.front); }
                    break;
            }
        }
    }
}
