using System.Collections.Generic;
using UnityEngine;

namespace EverythingWorld.World
{
    /// The Caldera's biggest artists. Each artist has a clearing of black stone with its covers on an obsidian slab
    /// framed in neon, a plaque and a neon post; every song has its own light, and songs without a piece of their own get
    /// a glowing song stone (SongStones).
    /// - blessthefall: the largest area. Monumental gates of obsidian edged in neon, standing open on a path of light up
    ///   to a dais ("Meet Me At The Gates"); a stair going down into a glowing pit and another climbing back out to a
    ///   high platform ("To Hell And Back"); a black pool with a lure light bobbing over it ("Bottomfeeder"); a tall
    ///   banner on the highest rock ("Undefeated").
    /// - Killswitch Engage and Like Moths To Flames: halls, fortified stages walled with amp stacks between obsidian
    ///   bastions with neon crowns. Killswitch: a cracked monolith split by a magenta seam ("My Curse"), a stone heart
    ///   broken in two with light between ("The End of Heartache"), a ring of fire bowls ("This Fire"), a black pool with a
    ///   beam rising from it ("Holy Diver"), two stone arms reaching up ("The Arms of Sorrow"), a pillar of white light
    ///   ("This Is Absolution"). Like Moths To Flames: a great moth sculpture over a flame with moths circling it, a
    ///   crown on a plinth ("God Complex"), a glowing anatomical heart ("The Anatomy of Evil"), an eclipse disc with a
    ///   violet corona ("Spiritual Eclipse"), a horned figure ("Demon of My Own"), steps going down ("Habitual Decline"),
    ///   ground cracked open with light ("Killing What's Underneath").
    /// - Dayseeker: a headstone outlined in magenta neon ("Neon Grave"), an open coffin glowing within ("Crawl Back To My
    ///   Coffin"), an empty swing ("Without Me"), red roses glowing ("Bloodlust").
    /// - Alexisonfire: a wrecked car, its hazards blinking ("Accidents"), a giant light bulb on a pylon ("Happiness by the
    ///   Kilowatt"), a funeral wreath on a stand ("Hey, It's Your Funeral Mama"), a strip of pavement under a street lamp
    ///   ("Side Walk When She Walks").
    public partial class CalderaKit
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
                Add("Killswitch Engage", "My Curse", "@a neon volcano"); Add("blessthefall", "To Hell And Back", "@the pit arena");
                Add("blessthefall", "Meet Me At The Gates", "@blessthefall's gates"); Add("Like Moths To Flames", "Spiritual Eclipse", "@the arrival and the arena");
                Add("Turnstile", "BLACKOUT", "@the arrival and the arena"); Add("Turnstile", "BIRDS", "Turnstile"); Add("Holding Absence", "Afterlife", "Holding Absence");
                Add("Alexisonfire", "Happiness by the Kilowatt", "@the arrival and the arena");
                return list.ToArray();
            }
        }

        /// The id of one of this artist's songs (by the start of its title), marked as having its own light.
        string Song(Plot z, string title)
        {
            if (!SongIds.TryGetValue(z.a.name, out var list)) return null;
            foreach (var (t, id) in list) if (t.StartsWith(title)) { z.lit.Add(id); return id; }
            return null;
        }

        /// A glow that brightens while its song plays (a plain glow if the song isn't found).
        Material SongGlow(string song, Color c, float s = 1.6f)
        {
            if (song == null) return GlowM(c, s);
            var m = new Material(GlowM(c, s)) { name = "Song glow" }; life.songGlows.Add((song, m, c * (s * .65f))); return m;
        }

        Material SongGlow(Plot z, string title, Color c, float s = 1.6f) => SongGlow(Song(z, title), c, s);

        public override void BuildArtist(LandContext ctx, IslandWorldData.Artist a)
        {
            if (obsidian == null) MakeMaterials();
            var root = new GameObject("Artist: " + a.name).transform; root.SetParent(ctx.Root, false);
            var front = facing.TryGetValue(a.name, out var f) ? f : new Vector3(center.x - a.x, 0, center.z - a.z).normalized;
            if (front.sqrMagnitude < .01f) front = Vector3.forward;
            var labels = new GameObject("Labels").transform; labels.SetParent(root, false);
            var area = root.gameObject.AddComponent<ArtistArea>();
            area.artistName = a.name; area.stationId = a.stationId; area.radius = a.r; area.glowColor = Cyan;
            var z = new Plot { ctx = ctx, root = root, labels = labels, mk = new MeshKit(), solid = new MeshKit(), pictures = new MeshKit(), a = a, c = ctx.OnGround(a.x, a.z), front = front, side = Vector3.Cross(Vector3.up, front), area = area };
            z.mk.Prism(basalt, z.c - Vector3.up * .28f, Vector3.up, Foot(a) * .58f, .34f, 18);
            switch (a.name)
            {
                case "blessthefall": Blessthefall(z); break;
                case "Killswitch Engage": Hall(z, Magenta, "KILLSWITCH ENGAGE"); Killswitch(z); break;
                case "Like Moths To Flames": Hall(z, Cyan, "LIKE MOTHS TO FLAMES"); Moths(z); break;
                case "Dayseeker": Dayseeker(z); break;
                case "Alexisonfire": Alexisonfire(z); break;
                default: Others(z); break;
            }
            SongStones(z);
            var reach = Foot(a);
            Slab(z, z.At(reach * .8f, -reach * .5f - 2.5f), front);
            ArtistBits.Plaque(z.mk, z.solid, labels, a, z.At(reach * .8f + 1.5f, reach * .5f + 2.5f), front, obsidian, basalt, Ink, 1.6f);
            NeonPost(ctx, z.mk, z.At(reach * .8f, reach * .5f + 5f));
            if (z.area.glowLight == null) z.area.glowLight = ArtistBits.PointLight(root, z.c + Vector3.up * 5f + front * 5f, Cyan, HasStation(a) ? 20f : 13f, 1.3f);
            area.glowRenderers = new Renderer[0];
            z.mk.Build("Area", root, false);
            z.solid.BuildCollider("Walls", root);
            z.pictures.Build("Covers", root, false);
            noGrass.Add(new Vector3(z.c.x, reach * .9f, z.c.z));
            if (HasStation(a) || reach >= 11f)
            {
                var mark = new GameObject("Shot: " + a.name + " up close").transform; mark.SetParent(ctx.Root, false);
                var cam = z.At(reach * .55f, reach * .35f) + Vector3.up * 1.7f; var look = z.At(-reach * .35f, 0) + Vector3.up * 2.5f - cam; mark.SetPositionAndRotation(cam, Quaternion.LookRotation(look.normalized));
            }
        }

        /// The covers on an obsidian slab framed in neon.
        void Slab(Plot z, Vector3 ground, Vector3 dir)
        {
            dir = Vector3.ProjectOnPlane(dir, Vector3.up).normalized; var rot = Quaternion.LookRotation(dir); var side = Vector3.Cross(Vector3.up, dir);
            var n = Mathf.Max(1, Mathf.Min(6, z.a.songList?.Length ?? 1)); const float size = .9f; var w = n * size * 1.25f + .5f;
            var c = ground + Vector3.up * 1.7f;
            z.mk.Box(obsidian, c - Vector3.up * .5f, new Vector3(w, size + 1.6f, .3f), rot); z.solid.Box(obsidian, c - Vector3.up * .5f, new Vector3(w, size + 1.6f, .4f), rot);
            foreach (var y in new[] { -1.3f, size / 2f + .32f }) z.mk.Box(lavaCyan, c + Vector3.up * y + dir * .16f, new Vector3(w, .06f, .02f), rot);
            foreach (var s in new[] { -1f, 1f }) z.mk.Box(lavaCyan, c + side * s * (w / 2f) + dir * .16f - Vector3.up * .5f, new Vector3(.06f, size + 1.6f, .02f), rot);
            ArtistBits.CoverWall(z.pictures, z.labels, z.ctx, z.a, c + dir * .16f + Vector3.up * .08f, dir, n, size, n, true, Ink);
        }

        /// A glowing stone for each song that hasn't got a piece of its own: a short obsidian post with a neon shard.
        void SongStones(Plot z)
        {
            if (!SongIds.TryGetValue(z.a.name, out var list)) return;
            var up = Vector3.up; var k = 0; var seen = new HashSet<string>();
            Color[] cols = { Cyan, Violet, Magenta };
            foreach (var (_, id) in list)
            {
                if (z.lit.Contains(id) || !seen.Add(id)) continue;
                var p = z.At(-Foot(z.a) * .25f - (k % 2) * 1.6f, Foot(z.a) * .45f + (k / 2) * 1.8f);
                var col = cols[(k + z.a.name.Length) % 3];
                z.mk.Prism(obsidian, p - up * .2f, up, .35f, 1.4f, 5, .6f); z.mk.Prism(SongGlow(id, col, 2f), p + up * 1.15f, up, .14f, .9f, 5, .05f);
                halos.Add(p + up * 1.6f, 1f, col, .45f); ArtistBits.PostCollider(z.root, p, 1.3f, .38f);
                k++;
            }
        }

        // ---------------------------------------------------------------- halls
        /// A hall: a raised stage walled at the back with amp stacks, obsidian bastions either side with neon crowns, a
        /// drum riser, mics, a truss of lights, the name in neon above.
        void Hall(Plot z, Color accent, string name)
        {
            var up = Vector3.up; var rot = z.Rot; var ctx = z.ctx;
            var s = z.At(-10f, 0); const float W = 14f;
            z.mk.Box(basalt, s + up * .6f, new Vector3(W, 1.2f, 7f), rot); z.solid.Box(basalt, s + up * .6f, new Vector3(W, 1.2f, 7f), rot);
            z.mk.Box(GlowM(accent, 2f), s + up * 1.2f + z.front * 3.52f, new Vector3(W, .08f, .04f), rot);
            for (var i = 0; i < 9; i++) for (var j = 0; j < 4; j++)
            {
                var p = s + up * (1.2f + .75f + j * 1.45f) - z.front * 3f + z.side * ((i - 4f) * 1.5f);
                z.mk.Box(dark, p, new Vector3(1.45f, 1.4f, .9f), rot); z.mk.Box(grille, p + z.front * .46f, new Vector3(1.25f, 1.2f, .02f), rot);
                if ((i + j) % 3 == 0) z.mk.Blob(GlowM(accent, 3f), p + z.front * .47f + z.side * .5f + up * .55f, Vector3.one * .04f, ctx.Rng, 0f);
            }
            for (var j = 0; j <= 4; j++) z.mk.Box(GlowM(accent, 1.4f), s + up * (1.2f + .05f + j * 1.45f) - z.front * 2.53f, new Vector3(13.5f, .05f, .03f), rot);   // a neon line between each row of cabinets
            for (var i = 0; i <= 9; i++) z.mk.Box(GlowM(accent, 1.4f), s + up * 4.1f - z.front * 2.53f + z.side * ((i - 4.5f) * 1.5f), new Vector3(.04f, 5.8f, .03f), rot);
            z.solid.Box(dark, s + up * 4f - z.front * 3f, new Vector3(13.6f, 6f, 1f), rot);
            foreach (var e in new[] { -1f, 1f })
            {
                var b = s + z.side * e * (W / 2f + 1.6f); z.mk.Prism(obsidian, b - up * .3f, up, 2f, 9f, 6, .8f); z.solid.Prism(obsidian, b - up * .3f, up, 2f, 9f, 6);
                for (var k = 0; k < 6; k++) z.mk.Cone(obsidian, b + up * 8.6f + Quaternion.Euler(0, k * 60f, 0) * Vector3.forward * 1.3f, .35f, 1.2f, 4);
                z.mk.Prism(GlowM(accent, 2.6f), b + up * 8.65f, up, 1.62f, .12f, 6); halos.Add(b + up * 9f, 2.4f, accent, .55f);
            }
            z.mk.Box(dark, s + up * 1.6f - z.front * 1f, new Vector3(2.6f, .8f, 2f), rot);
            foreach (var x in new[] { -.7f, 0f, .7f }) z.mk.Prism(steel, s + up * 2f - z.front * 1f + z.side * x, up, .35f, .4f, 12);
            foreach (var x in new[] { -3f, 0f, 3f }) z.mk.Prism(steel, s + up * 1.2f + z.front * 2.2f + z.side * x, up, .03f, 1.6f, 6);
            var tr = s + up * 8f + z.front * 1.5f; z.mk.Box(steel, tr, new Vector3(W, .35f, .35f), rot);
            foreach (var e in new[] { -1f, 1f }) z.mk.Box(steel, s + z.side * e * (W / 2f - .3f) + z.front * 1.5f + up * 4.6f, new Vector3(.3f, 6.8f, .3f), rot);
            for (var k = 0; k < 7; k++) { var lp = tr + z.side * ((k - 3f) * 2f) - up * .4f; z.mk.Box(dark, lp, new Vector3(.35f, .45f, .35f), rot); var col = k % 2 == 0 ? accent : Ink; z.mk.Prism(GlowM(col, 3f), lp - up * .25f, (-up + z.front * .4f).normalized, .14f, .05f, 8); halos.Add(lp - up * .4f, 1.1f, col, .5f); }
            ArtistBits.Label(z.labels, name, s + up * 9.2f - z.front * 2.5f, z.front, .55f, accent, TextAnchor.MiddleCenter);
            ArtistBits.PointLight(z.root, s + up * 5f + z.front * 6f, accent, 18f, 2f);
            halos.Add(s + up * 4f + z.front * 1f, 6f, accent, .08f);
        }

        // ---------------------------------------------------------------- Killswitch Engage
        void Killswitch(Plot z)
        {
            var up = Vector3.up; var rot = z.Rot; var ctx = z.ctx;
            // My Curse: a cracked monolith split by a magenta seam
            var m = z.At(-1f, -13f); z.mk.Box(obsidian, m + up * 3.5f - z.side * .55f, new Vector3(1f, 7f, 1.6f), rot * Quaternion.Euler(0, 0, 3f)); z.mk.Box(obsidian, m + up * 3.2f + z.side * .55f, new Vector3(1f, 6.4f, 1.6f), rot * Quaternion.Euler(0, 0, -4f));
            z.mk.Box(SongGlow(z, "My Curse", Magenta, 3f), m + up * 3.3f, new Vector3(.12f, 6.6f, 1.2f), rot); halos.Add(m + up * 3.5f + z.front * 1f, 3f, Magenta, .5f); life.curse = m + up * 7f;
            z.solid.Box(obsidian, m + up * 3.5f, new Vector3(2.4f, 7f, 1.8f), rot);
            // The End of Heartache: a stone heart broken in two with light between
            var h = z.At(1f, 12f) + up * 2.2f; var stone = WorldMaterials.Solid(new Color(.25f, .23f, .26f));
            foreach (var s in new[] { -1f, 1f }) { z.mk.Blob(stone, h + z.side * s * .9f + up * .4f, new Vector3(.9f, .9f, .6f), ctx.Rng, .1f); z.mk.Cone(stone, h + z.side * s * .45f - up * .3f, .9f, -1.5f, 4, s * 20f); }
            z.mk.Box(SongGlow(z, "The End of Heartache", new Color(1f, .3f, .4f), 3f), h, new Vector3(.08f, 2.6f, .5f), rot * Quaternion.Euler(0, 0, 8f)); halos.Add(h + z.front * .6f, 2f, new Color(1f, .3f, .4f), .6f);
            z.mk.Box(basalt, z.At(1f, 12f) + up * .5f, new Vector3(2.6f, 1f, 1.4f), rot); z.solid.Box(basalt, z.At(1f, 12f) + up * 1.6f, new Vector3(2.8f, 3.2f, 1.6f), rot);
            // This Fire: a ring of fire bowls
            var fr = z.At(5f, -7f); var fire = SongGlow(z, "This Fire", new Color(1f, .3f, .7f), 3f);
            for (var k = 0; k < 5; k++) { var a = k * Mathf.PI * 2f / 5f; var p = ctx.OnGround(fr.x + Mathf.Cos(a) * 2.2f, fr.z + Mathf.Sin(a) * 2.2f); z.mk.Prism(dark, p, up, .12f, 1f, 6); z.mk.Prism(dark, p + up * 1f, up, .4f, .25f, 8, 1.3f); z.mk.Blob(fire, p + up * 1.32f, new Vector3(.32f, .25f, .32f), ctx.Rng, .3f); life.fires.Add(p + up * 1.4f); ArtistBits.PostCollider(z.root, p, 1.3f, .45f); }
            halos.Add(fr + up * 1.5f, 3f, Magenta, .4f);
            // Holy Diver: a black pool with a beam rising from it
            var pool = z.At(5f, 7f); z.mk.Prism(WorldMaterials.Solid(new Color(.02f, .03f, .06f), .98f), pool - up * .15f, up, 2f, .25f, 16); z.mk.Prism(obsidian, pool - up * .2f, up, 2.3f, .28f, 16);
            z.mk.Prism(SongGlow(z, "Holy Diver", new Color(.5f, .8f, 1f), 1.2f), pool + up * .1f, up, .2f, 6f, 10, .3f); halos.Add(pool + up * 2f, 2f, new Color(.5f, .8f, 1f), .4f);
            // The Arms of Sorrow: two stone arms reaching up out of the ground
            var ar = z.At(-4f, 13f); foreach (var s in new[] { -1f, 1f }) { var b = ar + z.side * s * .9f; z.mk.Prism(stone, b - up * .2f, (up + z.side * s * .25f).normalized, .35f, 3.2f, 6, .7f); var hand = b + (up + z.side * s * .25f).normalized * 3.2f; z.mk.Blob(stone, hand, new Vector3(.35f, .45f, .25f), ctx.Rng, .1f); for (var k = 0; k < 4; k++) z.mk.Box(stone, hand + up * .5f + z.side * ((k - 1.5f) * .12f), new Vector3(.08f, .45f, .08f), Quaternion.Euler(0, 0, (k - 1.5f) * -10f)); ArtistBits.PostCollider(z.root, b, 3f, .4f); }
            z.mk.Prism(SongGlow(z, "The Arms of Sorrow", Violet, 2f), ar + up * .04f, up, 1.6f, .04f, 14);
            // This Is Absolution: a pillar of white light
            var pl = z.At(-14f, 11f); z.mk.Prism(obsidian, pl - up * .2f, up, 1f, .6f, 8); z.mk.Prism(SongGlow(z, "This Is Absolution", Ink, 2f), pl + up * .4f, up, .12f, 7f, 8, .3f); halos.Add(pl + up * 5f, 2.4f, Ink, .4f); ArtistBits.PostCollider(z.root, pl, 3f, .5f);
        }

        // ---------------------------------------------------------------- Like Moths To Flames
        void Moths(Plot z)
        {
            var up = Vector3.up; var rot = z.Rot; var ctx = z.ctx;
            // a great moth sculpture over a flame, moths circling it
            var fl = z.At(0f, -12f); z.mk.Prism(obsidian, fl, up, 1.4f, 1.2f, 8, .7f); z.mk.Blob(GlowM(new Color(1f, .9f, .6f), 3.2f), fl + up * 1.7f, new Vector3(.5f, .8f, .5f), ctx.Rng, .2f);
            halos.Add(fl + up * 2f, 3f, new Color(1f, .9f, .6f), .8f); life.fires.Add(fl + up * 1.6f); life.mothFlame = fl + up * 2.2f; z.solid.Prism(obsidian, fl, up, 1.4f, 1.6f, 8);
            ArtistBits.PointLight(z.root, fl + up * 3f, new Color(1f, .9f, .65f), 12f, 1.8f);
            var moth = WorldMaterials.Solid(new Color(.55f, .5f, .45f)); var mb = fl + up * 6.5f - z.side * 1.5f;
            z.mk.Box(moth, mb, new Vector3(.5f, .5f, 2f), Quaternion.LookRotation(z.side)); foreach (var s in new[] { -1f, 1f }) { z.mk.Box(moth, mb + z.front * s * 1.6f + up * .2f, new Vector3(1.8f, .06f, 2.6f), Quaternion.LookRotation(z.side) * Quaternion.Euler(0, 0, s * 15f) * Quaternion.Euler(0, 90f, 0)); z.mk.Blob(GlowM(Cyan, 2f), mb + z.front * s * 1.8f + up * .3f, new Vector3(.3f, .05f, .3f), ctx.Rng, 0f); }
            z.mk.Box(dark, (mb + fl) / 2f - up * .6f, new Vector3(.08f, 4f, .08f), Quaternion.FromToRotation(Vector3.up, mb - fl));
            for (var k = 0; k < 6; k++) { var mk2 = new MeshKit(); mk2.Box(moth, Vector3.zero, new Vector3(.08f, .08f, .3f), Quaternion.identity); foreach (var s in new[] { -1f, 1f }) mk2.Box(moth, new Vector3(s * .2f, 0, 0), new Vector3(.35f, .02f, .3f), Quaternion.Euler(0, 0, s * 20f)); var go = mk2.BuildSingle("Moth", life.transform, false); if (go != null) life.moths.Add(go.transform); }
            // God Complex: a crown on a plinth
            var cr = z.At(4f, 9f); z.mk.Box(obsidian, cr + up * .7f, new Vector3(1.2f, 1.4f, 1.2f), rot); z.solid.Box(obsidian, cr + up * .7f, new Vector3(1.2f, 1.4f, 1.2f), rot);
            var gold = SongGlow(z, "God Complex", new Color(1f, .8f, .3f), 1.6f); z.mk.Prism(gold, cr + up * 1.4f, up, .45f, .3f, 12); for (var k = 0; k < 6; k++) z.mk.Cone(gold, cr + up * 1.7f + Quaternion.Euler(0, k * 60f, 0) * Vector3.forward * .4f, .1f, .35f, 4);
            halos.Add(cr + up * 1.7f, 1.2f, new Color(1f, .8f, .3f), .5f);
            // The Anatomy of Evil: a glowing anatomical heart on a stand
            var ah = z.At(-3f, 13f); z.mk.Prism(steel, ah, up, .06f, 2f, 6); var hm = SongGlow(z, "The Anatomy of Evil", new Color(1f, .15f, .35f), 2f);
            z.mk.Blob(hm, ah + up * 2.5f, new Vector3(.5f, .6f, .45f), ctx.Rng, .15f); foreach (var d in new[] { up + z.side * .4f, up - z.side * .2f, up + z.front * .3f }) z.mk.Prism(hm, ah + up * 2.9f, d.normalized, .1f, .6f, 6, .7f);
            halos.Add(ah + up * 2.5f, 1.5f, new Color(1f, .15f, .35f), .6f); ArtistBits.PostCollider(z.root, ah, 2f, .3f);
            // Spiritual Eclipse: a black disc with a violet corona, on a frame
            var ec = z.At(-14f, -11f); foreach (var s in new[] { -1f, 1f }) { z.mk.Box(dark, ec + z.side * s * 2.6f + up * 3f, new Vector3(.25f, 6f, .25f), rot); ArtistBits.PostCollider(z.root, ec + z.side * s * 2.6f, 6f, .2f); }
            var ecl = ec + up * 4f; z.mk.Prism(dark, ecl - z.front * .1f, z.front, 1.8f, .2f, 24); var corona = SongGlow(z, "Spiritual Eclipse", Violet, 2.4f);
            for (var k = 0; k < 24; k++) { float a0 = k * Mathf.PI / 12f, a1 = (k + 1) * Mathf.PI / 12f; var p0 = ecl + (z.side * Mathf.Cos(a0) + up * Mathf.Sin(a0)) * 2f; var p1 = ecl + (z.side * Mathf.Cos(a1) + up * Mathf.Sin(a1)) * 2f; z.mk.Box(corona, (p0 + p1) / 2f - z.front * .15f, new Vector3(.25f, .25f, Vector3.Distance(p0, p1)), Quaternion.LookRotation(p1 - p0, z.front)); }
            halos.Add(ecl, 3.2f, Violet, .45f);
            // Demon of My Own: a horned figure
            var dm = z.At(6f, -2f + 12f); var cloak = WorldMaterials.Solid(new Color(.08f, .06f, .1f));
            z.mk.Cone(cloak, dm, .8f, 2.4f, 8); z.mk.Blob(cloak, dm + up * 2.3f, new Vector3(.32f, .38f, .32f), ctx.Rng, 0f); foreach (var s in new[] { -1f, 1f }) z.mk.Prism(obsidian, dm + up * 2.5f + z.side * s * .2f, (up + z.side * s * .6f).normalized, .07f, .6f, 5, .1f);
            foreach (var s in new[] { -1f, 1f }) z.mk.Blob(SongGlow(z, "Demon of My Own", Magenta, 3f), dm + up * 2.35f + z.front * .28f + z.side * s * .1f, Vector3.one * .04f, ctx.Rng, 0f);
            ArtistBits.PostCollider(z.root, dm, 2.4f, .7f);
            // Habitual Decline: steps going down into the ground, a glow at the bottom
            var hd = z.At(-2f, -15f); for (var k = 0; k < 5; k++) z.mk.Box(basalt, hd + up * (.6f - k * .15f) - z.front * (k * .5f), new Vector3(1.6f, .14f, .5f), rot);
            z.mk.Box(SongGlow(z, "Habitual Decline", Cyan, 1.4f), hd - z.front * 2.6f + up * .02f, new Vector3(1.6f, .03f, .8f), rot);
            // Killing What's Underneath: the ground cracked open with light
            var kw = z.At(8f, 0f); var under = SongGlow(z, "Killing What", new Color(.4f, 1f, .9f), 1.6f);
            for (var k = 0; k < 7; k++) { var a = k * .9f; z.mk.Box(under, kw + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * ctx.Range(.5f, 1.6f) + up * .05f, new Vector3(.2f, .05f, ctx.Range(1f, 2.4f)), Quaternion.Euler(0, a * 57f, 0)); }
            halos.Add(kw + up * .5f, 2f, new Color(.4f, 1f, .9f), .4f);
        }

        // ---------------------------------------------------------------- blessthefall
        void Blessthefall(Plot z)
        {
            var up = Vector3.up; var rot = z.Rot; var ctx = z.ctx;
            // Meet Me At The Gates: monumental gates standing open on a path of light up to a dais
            var g = z.At(-6f, 0); var gate = SongGlow(z, "Meet Me At The Gates", Ink, 2.4f);
            foreach (var s in new[] { -1f, 1f })
            {
                var pil = g + z.side * s * 4.2f; z.mk.Box(obsidian, pil + up * 6.5f, new Vector3(1.8f, 13f, 1.8f), rot); z.solid.Box(obsidian, pil + up * 6.5f, new Vector3(1.8f, 13f, 1.8f), rot);
                z.mk.Cone(obsidian, pil + up * 13f, 1.4f, 3f, 4, 45f);
                foreach (var e in new[] { -1f, 1f }) z.mk.Box(gate, pil + z.side * e * .92f + z.front * .92f + up * 6.5f, new Vector3(.06f, 13f, .06f), rot);
                // a gate leaf swung open
                var hinge = pil - z.side * s * .9f; var lrot = rot * Quaternion.Euler(0, s * 70f, 0); var lc = hinge + lrot * new Vector3(-s * 1.7f, 4.5f, 0);
                var iron = WorldMaterials.Solid(new Color(.1f, .1f, .12f), .7f);
                for (var b = 0; b < 7; b++) z.mk.Box(iron, lc + lrot * new Vector3((b - 3f) * .5f, 0, 0), new Vector3(.1f, 8.5f, .1f), lrot);
                foreach (var y in new[] { -3.8f, 0f, 3.8f }) z.mk.Box(iron, lc + lrot * new Vector3(0, y, 0), new Vector3(3.4f, .14f, .12f), lrot);
                for (var b = 0; b < 7; b++) z.mk.Cone(gate, lc + lrot * new Vector3((b - 3f) * .5f, 4.25f, 0), .08f, .5f, 4);
                z.solid.Box(iron, lc, new Vector3(3.4f, 8.5f, .3f), lrot);
            }
            z.mk.Box(obsidian, g + up * 13.6f, new Vector3(10.2f, 1.4f, 1.8f), rot); z.mk.Box(gate, g + up * 12.85f + z.front * .92f, new Vector3(8.4f, .08f, .06f), rot);
            ArtistBits.Label(z.labels, "MEET ME AT THE GATES", g + up * 13.6f + z.front * .95f, z.front, .32f, Ink, TextAnchor.MiddleCenter);
            for (var k = 0; k < 9; k++) z.mk.Box(gate, g + z.front * (5f - k * 1.8f) + up * .05f, new Vector3(2.2f, .04f, 1.2f), rot);
            var dais = g - z.front * 11f; z.mk.Prism(obsidian, dais, up, 3f, 1.2f, 12); z.solid.Prism(obsidian, dais, up, 3f, 1.2f, 12); z.mk.Prism(gate, dais + up * 1.2f, up, 2.6f, .05f, 12);
            life.gates = dais + up * 1.5f; halos.Add(g + up * 6f, 7f, Ink, .2f); halos.Add(dais + up * 2f, 3.5f, Ink, .5f);
            ArtistBits.PointLight(z.root, g + up * 6f + z.front * 3f, new Color(.85f, .9f, 1f), 20f, 2.2f);
            // To Hell And Back: a stair down into a glowing pit, a stair up to a high platform
            var pit = z.At(2f, -14f); var hellM = SongGlow(z, "To Hell And Back", Magenta, 2f);
            z.mk.Prism(obsidian, pit - up * .2f, up, 3.4f, .5f, 16); z.mk.Prism(hellM, pit + up * .31f, up, 2.6f, .03f, 16); halos.Add(pit + up * 1f, 3.4f, Magenta, .6f);
            for (var k = 0; k < 6; k++) z.mk.Box(basalt, pit + z.front * (5f - k * .5f) + up * (.6f - k * .05f), new Vector3(1.6f, .14f, .5f), rot);
            var plat = z.At(2f, -22f); for (var k = 0; k < 12; k++) z.mk.Box(basalt, pit - z.side * 3.8f - z.side * (k * .6f) + up * (.25f + k * .45f), new Vector3(.6f, .5f + k * .9f, 1.6f), Quaternion.LookRotation(z.side));
            var top = pit - z.side * 11f + up * 5.6f; z.mk.Box(obsidian, top, new Vector3(4f, .6f, 4f), rot); z.mk.Box(SongGlow(z, "To Hell And Back", Cyan, 2f), top + up * .32f, new Vector3(3.6f, .04f, 3.6f), rot);
            foreach (var a in new[] { -1.8f, 1.8f }) foreach (var b in new[] { -1.8f, 1.8f }) { var leg = top + z.side * a + z.front * b; var gnd = new Vector3(leg.x, ctx.Ground(leg.x, leg.z), leg.z); z.mk.Box(obsidian, (leg + gnd) / 2f, new Vector3(.4f, leg.y - gnd.y, .4f), rot); ArtistBits.PostCollider(z.root, gnd, leg.y - gnd.y, .3f); }
            z.solid.Box(obsidian, top, new Vector3(4f, .6f, 4f), rot);
            for (var k = 0; k < 12; k++) z.solid.Box(basalt, pit - z.side * 3.8f - z.side * (k * .6f) + up * (.25f + k * .45f), new Vector3(.6f, .5f + k * .9f, 1.6f), Quaternion.LookRotation(z.side));
            // Bottomfeeder: a black pool with a lure light bobbing over it
            var bp = z.At(8f, 14f); z.mk.Prism(WorldMaterials.Solid(new Color(.02f, .03f, .06f), .98f), bp - up * .15f, up, 3f, .25f, 18); z.mk.Prism(obsidian, bp - up * .2f, up, 3.3f, .28f, 18);
            var lure = SongGlow(z, "Bottomfeeder", new Color(.5f, 1f, .8f), 3f); var lk = new MeshKit(); lk.Blob(lure, Vector3.zero, Vector3.one * .22f, ctx.Rng, 0f); lk.Box(dark, new Vector3(0, .6f, 0), new Vector3(.02f, 1.2f, .02f), Quaternion.identity);
            var lgo = lk.BuildSingle("Lure", life.transform, false); if (lgo != null) { life.lure = lgo.transform; life.lureAt = bp + up * 1.2f; }
            halos.Add(bp + up * 1.2f, 1.6f, new Color(.5f, 1f, .8f), .5f); z.solid.Prism(obsidian, bp, up, 3.2f, 1f, 12);
            // Undefeated: a tall banner on the highest rock
            var ub = z.At(-14f, 13f); z.mk.Blob(obsidian, ub + up * 1.5f, new Vector3(2.4f, 2f, 2.2f), ctx.Rng, .25f); z.solid.Box(obsidian, ub + up * 1.5f, new Vector3(4f, 3.4f, 3.6f), rot);
            var pole = ub + up * 3f; z.mk.Prism(steel, pole, up, .1f, 9f, 6);
            var ban = SongGlow(z, "Undefeated", Magenta, 1.3f); for (var k = 0; k < 7; k++) z.mk.Box(ban, pole + up * 8.2f + z.side * (.35f + k * .45f) - up * Mathf.Sin(k * .7f) * .15f, new Vector3(.46f, 1.6f, .04f), rot * Quaternion.Euler(0, Mathf.Sin(k * 1.2f) * 14f, 0));
            halos.Add(pole + up * 8f + z.side * 1.6f, 2.4f, Magenta, .35f);
            var mark = new GameObject("Shot: blessthefall's gates").transform; mark.SetParent(ctx.Root, false);
            var cam = z.At(10f, 3f) + up * 1.7f; var look = g + up * 6f - cam; mark.SetPositionAndRotation(cam, Quaternion.LookRotation(look.normalized));
        }

        // ---------------------------------------------------------------- Dayseeker and Alexisonfire
        void Dayseeker(Plot z)
        {
            var up = Vector3.up; var rot = z.Rot; var ctx = z.ctx; var stone = WorldMaterials.Solid(new Color(.22f, .22f, .25f));
            // Neon Grave: a headstone outlined in magenta neon over a glowing mound
            var gr = z.At(-6f, -3f); z.mk.Box(stone, gr + up * 1.3f, new Vector3(1.8f, 2.6f, .4f), rot); z.mk.Prism(stone, gr + up * 2.6f, z.front, .9f, .4f, 16);
            var neon = SongGlow(z, "Neon Grave", Magenta, 3f);
            for (var k = 0; k <= 16; k++) { var a0 = k * Mathf.PI / 16f; var a1 = (k + 1) * Mathf.PI / 16f; if (k == 16) break; var p0 = gr + up * 2.6f + (z.side * Mathf.Cos(a0) + up * Mathf.Sin(a0)) * 1f + z.front * .22f; var p1 = gr + up * 2.6f + (z.side * Mathf.Cos(a1) + up * Mathf.Sin(a1)) * 1f + z.front * .22f; z.mk.Box(neon, (p0 + p1) / 2f, new Vector3(.08f, .08f, Vector3.Distance(p0, p1)), Quaternion.LookRotation(p1 - p0, z.front)); }
            foreach (var s in new[] { -1f, 1f }) z.mk.Box(neon, gr + z.side * s * 1f + up * 1.3f + z.front * .22f, new Vector3(.08f, 2.6f, .08f), rot);
            z.mk.Blob(SongGlow(z, "Neon Grave", Violet, .9f), gr + z.front * 1.8f + up * .1f, new Vector3(.9f, .25f, 1.6f), ctx.Rng, .1f);
            halos.Add(gr + up * 2f + z.front * .8f, 3f, Magenta, .5f); z.solid.Box(stone, gr + up * 1.6f, new Vector3(2f, 3.2f, .6f), rot);
            // Crawl Back To My Coffin: an open coffin glowing within, its lid leaning beside it
            var cf = z.At(3f, -9f); var wood = WorldMaterials.Solid(new Color(.12f, .08f, .07f), .5f); var crot = Quaternion.LookRotation(z.side);
            z.mk.Box(wood, cf + up * .35f, new Vector3(.9f, .7f, 2.2f), crot); z.mk.Box(SongGlow(z, "Crawl Back", Cyan, 1.6f), cf + up * .71f, new Vector3(.7f, .02f, 2f), crot);
            z.mk.Box(wood, cf + z.front * 1f + up * .9f, new Vector3(.9f, .08f, 2.2f), crot * Quaternion.Euler(0, 0, 70f)); halos.Add(cf + up * 1f, 1.6f, Cyan, .5f); z.solid.Box(wood, cf + up * .6f, new Vector3(2f, 1.2f, 2.4f), crot);
            // Without Me: an empty swing under a crooked frame
            var sw = z.At(3f, 9f); foreach (var s in new[] { -1f, 1f }) { z.mk.Box(dark, sw + z.side * s * 1.2f + up * 1.5f, new Vector3(.12f, 3f, .12f), rot); ArtistBits.PostCollider(z.root, sw + z.side * s * 1.2f, 3f, .1f); }
            z.mk.Box(dark, sw + up * 3f, new Vector3(2.6f, .12f, .12f), rot); foreach (var s in new[] { -.3f, .3f }) z.mk.Box(dark, sw + z.side * s + up * 2f, new Vector3(.02f, 2f, .02f), rot);
            z.mk.Box(SongGlow(z, "Without Me", Violet, 2f), sw + up * .95f, new Vector3(.7f, .06f, .3f), rot); halos.Add(sw + up * 1f, 1f, Violet, .5f);
            // Bloodlust: a patch of glowing red roses
            var rs = z.At(-2f, 10f); var red = SongGlow(z, "Bloodlust", new Color(1f, .1f, .25f), 2f); var stem = WorldMaterials.Solid(new Color(.1f, .2f, .12f));
            for (var k = 0; k < 14; k++) { var p = rs + new Vector3(Mathf.Cos(k * 2.4f), 0, Mathf.Sin(k * 2.4f)) * (.4f + k * .1f); p.y = ctx.Ground(p.x, p.z); var hh = ctx.Range(.5f, .9f); z.mk.Prism(stem, p, up, .02f, hh, 4); z.mk.Blob(red, p + up * hh, Vector3.one * .09f, ctx.Rng, .2f); }
            halos.Add(rs + up * .8f, 2f, new Color(1f, .1f, .25f), .4f); ArtistBits.PostCollider(z.root, rs, 1f, 2.2f);
        }

        void Alexisonfire(Plot z)
        {
            var up = Vector3.up; var rot = z.Rot; var ctx = z.ctx;
            // Accidents: a wrecked car on its side, hazards blinking
            var car = z.At(-5f, -6f); var paint = WorldMaterials.Solid(new Color(.3f, .32f, .36f), .5f); var crot = Quaternion.LookRotation(z.side) * Quaternion.Euler(0, 0, 80f);
            z.mk.Box(paint, car + up * 1f, new Vector3(1.7f, .7f, 4f), crot); z.mk.Box(paint, car + up * 1f + crot * new Vector3(0, .55f, -.2f), new Vector3(1.5f, .55f, 2f), crot);
            var haz = SongGlow(z, "Accidents", new Color(1f, .6f, .1f), 3f); foreach (var e in new[] { -1f, 1f }) { z.mk.Box(haz, car + up * 1f + crot * new Vector3(e * .6f, .1f, 2.01f), new Vector3(.2f, .12f, .02f), crot); }
            halos.Add(car + up * 1.2f + z.side * 2.2f, 1.6f, new Color(1f, .6f, .1f), .5f); life.hazards.Add(car + up * 1.2f + z.side * 2.1f); z.solid.Box(paint, car + up * 1f, new Vector3(2.2f, 2f, 4.2f), Quaternion.LookRotation(z.side));
            // Happiness by the Kilowatt: a giant light bulb on a pylon
            var hb = z.At(-8f, 7f); for (var k = 0; k < 4; k++) { var a = k * 90f + 45f; var d = Quaternion.Euler(0, a, 0) * Vector3.forward; z.mk.Box(steel, hb + d * .6f + up * 4f, new Vector3(.15f, 8.2f, .15f), Quaternion.LookRotation(d) * Quaternion.Euler(-8f, 0, 0)); }
            for (var k = 1; k < 4; k++) z.mk.Box(steel, hb + up * (k * 2f), new Vector3(1.6f - k * .3f, .1f, .1f), rot);
            var bulb = SongGlow(z, "Happiness", new Color(1f, .9f, .5f), 2.4f); z.mk.Blob(bulb, hb + up * 9.6f, new Vector3(1.1f, 1.3f, 1.1f), ctx.Rng, 0f); z.mk.Prism(steel, hb + up * 7.8f, up, .5f, .7f, 10);
            halos.Add(hb + up * 9.6f, 3.6f, new Color(1f, .9f, .5f), .6f); ArtistBits.PostCollider(z.root, hb, 8f, .9f); life.bulb = hb + up * 9.6f;
            // Hey, It's Your Funeral Mama: a wreath on a stand
            var wr = z.At(4f, -9f); z.mk.Box(dark, wr + up * .8f, new Vector3(.08f, 1.6f, .08f), rot * Quaternion.Euler(-15f, 0, 0)); var fl = SongGlow(z, "Hey, It", new Color(1f, .95f, .85f), 1.4f);
            for (var k = 0; k < 18; k++) { var a = k * Mathf.PI / 9f; z.mk.Blob(k % 3 == 0 ? fl : WorldMaterials.Solid(new Color(.15f, .25f, .15f)), wr + up * 1.9f + (z.side * Mathf.Cos(a) + up * Mathf.Sin(a)) * .65f, Vector3.one * .16f, ctx.Rng, .2f); }
            ArtistBits.PostCollider(z.root, wr, 2.4f, .6f);
            // Side Walk When She Walks: a strip of pavement under a street lamp
            var sw = z.At(4f, 8f); z.mk.Box(WorldMaterials.Solid(new Color(.45f, .45f, .47f)), sw + up * .08f, new Vector3(1.8f, .16f, 6f), Quaternion.LookRotation(z.side));
            var lp = sw + z.front * 1.2f; z.mk.Prism(dark, lp, up, .08f, 4f, 6); z.mk.Box(dark, lp + up * 4f - z.front * .5f, new Vector3(.1f, .1f, 1f), rot);
            z.mk.Box(SongGlow(z, "Side Walk", new Color(1f, .85f, .55f), 2.6f), lp + up * 3.9f - z.front * 1f, new Vector3(.3f, .08f, .4f), rot); halos.Add(lp + up * 3.7f - z.front * 1f, 1.6f, new Color(1f, .85f, .55f), .7f); ArtistBits.PostCollider(z.root, lp, 4f, .1f);
        }
    }
}
