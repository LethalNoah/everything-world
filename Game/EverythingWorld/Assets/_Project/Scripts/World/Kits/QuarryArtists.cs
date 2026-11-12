using System.Collections.Generic;
using UnityEngine;

namespace EverythingWorld.World
{
    /// The Nu Metal Quarry's artists. Round the rim each band plays off a shipping container with one side swung open as
    /// a stage, on a pad of gravel, amps inside, a lamp over it; covers on a sheet of steel, a plaque; songs without a
    /// piece of their own get a work lamp on a post that glows while they play (SongLamps).
    /// - Nickelback (on the quarry floor): a stage built on a mining rig, a steel deck under a tall drill tower hung with
    ///   lights and topped with a gold star ("Rockstar"), amps and a drum riser; a huge picture frame standing on the
    ///   floor ("Photograph": camera flashes go off round the pit); a wire strung between poles where crows sit ("Bones
    ///   For The Crows": crows circle over the rig).
    /// - Creed: a tall conveyor tower with a light at the very top ("Higher": a beam goes up), two crane jibs swung wide
    ///   open ("With Arms Wide Open"), a chain-link cage ("My Own Prison").
    /// - 3 Doors Down: a mine cart heaped with glowing green crystal ("Kryptonite": the light turns green), an empty chair
    ///   on a flatbed under a lamp ("Here Without You").
    /// - Breaking Benjamin: a cracked heart of scrap ("Dear Agony"), a flickering neon sign ("Give Me A Sign"), wings
    ///   welded from scrap metal ("Anthem Of The Angels").
    /// - Disturbed: a ring of candles round an old speaker cabinet, silent ("The Sound of Silence": the fires and lamps go
    ///   down and the sky darkens), a lightning rod ("Stricken").
    /// - Others: Linkin Park a road barrier with END on it ("In the End"); Seether a broken mirror ("Broken"); Hoobastank a
    ///   lone streetlight and a bench ("The Reason"); Puddle Of Mudd a frosted glass pane ("Blurry"); NOTHING MORE scrap
    ///   shields on a wall ("VALHALLA").
    public partial class QuarryKit
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
                Add("3 Doors Down", "Kryptonite", "@down into the quarry"); Add("Disturbed", "The Sound of Silence", "@the arrival and the quarry");
                Add("Nickelback", "Photograph", "@down into the quarry"); Add("Creed", "Higher", "Creed");
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
            if (rock == null) MakeMaterials();
            var root = new GameObject("Artist: " + a.name).transform; root.SetParent(ctx.Root, false);
            var front = facing.TryGetValue(a.name, out var f) ? f : new Vector3(center.x - a.x, 0, center.z - a.z).normalized;
            if (front.sqrMagnitude < .01f) front = Vector3.forward;
            var labels = new GameObject("Labels").transform; labels.SetParent(root, false);
            var area = root.gameObject.AddComponent<ArtistArea>();
            area.artistName = a.name; area.stationId = a.stationId; area.radius = a.r; area.glowColor = Fire;
            var z = new Plot { ctx = ctx, root = root, labels = labels, mk = new MeshKit(), solid = new MeshKit(), pictures = new MeshKit(), a = a, c = ctx.OnGround(a.x, a.z), front = front, side = Vector3.Cross(Vector3.up, front), area = area };
            var reach = Foot(a); var up = Vector3.up;
            z.mk.Prism(gravel, z.c - up * .25f, up, reach * .62f, .33f, 16);
            if (a.name == "Nickelback") Nickelback(z);
            else { Container(z); switch (a.name) { case "Creed": Creed(z); break; case "3 Doors Down": ThreeDoorsDown(z); break; case "Breaking Benjamin": BreakingBenjamin(z); break; case "Disturbed": Disturbed(z); break; default: Others(z); break; } }
            SongLamps(z);
            Board(z, z.At(reach * .6f, -reach * .45f - 1.5f), front);
            ArtistBits.Plaque(z.mk, z.solid, labels, a, z.At(reach * .6f + 1.5f, reach * .45f + 1.5f), front, rustDark, steel, Ink, 1.6f);
            Drum(ctx, z.mk, z.At(reach * .35f, reach * .62f));
            if (z.area.glowLight == null) z.area.glowLight = ArtistBits.PointLight(root, z.c + up * 6f + front * 4f, Sodium, HasStation(a) ? 24f : 16f, 2f);
            area.glowRenderers = new Renderer[0];
            z.mk.Build("Area", root, false);
            z.solid.BuildCollider("Walls", root);
            z.pictures.Build("Covers", root, false);
            noGrass.Add(new Vector3(z.c.x, reach * .9f, z.c.z));
            if (HasStation(a) || reach >= 11f)
            {
                var mark = new GameObject("Shot: " + a.name + " up close").transform; mark.SetParent(ctx.Root, false);
                var cam = z.At(reach * .6f, reach * .3f) + up * 1.7f; var look = z.At(-reach * .4f, 0) + up * 3f - cam; mark.SetPositionAndRotation(cam, Quaternion.LookRotation(look.normalized));
            }
        }

        /// The covers on a sheet of steel bolted to two I-beams.
        void Board(Plot z, Vector3 ground, Vector3 dir)
        {
            dir = Vector3.ProjectOnPlane(dir, Vector3.up).normalized; var rot = Quaternion.LookRotation(dir); var side = Vector3.Cross(Vector3.up, dir);
            var n = Mathf.Max(1, Mathf.Min(7, z.a.songList?.Length ?? 1)); const float size = .9f; var w = n * size * 1.25f + .6f;
            var c = ground + Vector3.up * 1.7f;
            z.mk.Box(steel, c - Vector3.up * .3f, new Vector3(w, size + 1.2f, .08f), rot); z.solid.Box(steel, c - Vector3.up * .5f, new Vector3(w, size + 1.6f, .3f), rot);
            foreach (var s in new[] { -1f, 1f }) z.mk.Box(rustDark, c + side * s * (w / 2f + .1f) - Vector3.up * 1f, new Vector3(.2f, size + 2.4f, .3f), rot);
            z.mk.Box(yellow, c + Vector3.up * (size / 2f + .35f) + dir * .05f, new Vector3(w, .12f, .02f), rot);
            ArtistBits.CoverWall(z.pictures, z.labels, z.ctx, z.a, c + dir * .05f + Vector3.up * .08f, dir, n, size, n, true, Ink);
        }

        /// A work lamp on a post for each song without its own piece, glowing while it plays.
        void SongLamps(Plot z)
        {
            if (!SongIds.TryGetValue(z.a.name, out var list)) return;
            var up = Vector3.up; var k = 0; var seen = new HashSet<string>();
            foreach (var (_, id) in list)
            {
                if (z.lit.Contains(id) || !seen.Add(id)) continue;
                var p = z.At(-Foot(z.a) * .1f - (k % 2) * 1.6f, Foot(z.a) * .5f + (k / 2) * 1.6f);
                z.mk.Prism(steel, p - up * .2f, up, .06f, 2.4f, 6); ArtistBits.PostCollider(z.root, p, 2.2f, .1f);
                z.mk.Box(dark, p + up * 2.35f + z.front * .15f, new Vector3(.35f, .3f, .3f), z.Rot);
                z.mk.Box(SongGlow(id, Sodium, 2f), p + up * 2.35f + z.front * .31f, new Vector3(.28f, .22f, .02f), z.Rot);
                halos.Add(p + up * 2.35f + z.front * .6f, 1.2f, Sodium, .5f);
                k++;
            }
        }

        /// A shipping container with one long side swung open as a stage: amps and a mic inside, a lamp over it.
        void Container(Plot z)
        {
            var up = Vector3.up; var rot = z.Rot; var sd = z.side; var c = z.At(-Foot(z.a) * .55f, 0);
            var col = WorldMaterials.Solid(new[] { new Color(.6f, .2f, .15f), new Color(.2f, .35f, .5f), new Color(.25f, .45f, .3f), new Color(.75f, .55f, .2f) }[Mathf.Abs(z.a.name.GetHashCode()) % 4]);
            const float L = 6f, D = 2.4f, H = 2.6f;
            z.mk.Box(col, c - z.front * (D / 2f - .05f) + up * H / 2f, new Vector3(L, H, .1f), rot);
            foreach (var e in new[] { -1f, 1f }) z.mk.Box(col, c + sd * e * (L / 2f - .05f) + up * H / 2f, new Vector3(.1f, H, D), rot);
            z.mk.Box(col, c + up * (H - .05f), new Vector3(L, .1f, D), rot); z.mk.Box(rustDark, c + up * .15f, new Vector3(L, .3f, D), rot);
            z.mk.Box(col, c + z.front * (D / 2f + 1.2f) - sd * (L / 2f) + up * H / 2f, new Vector3(.1f, H, 2.4f), rot * Quaternion.Euler(0, 40f, 0));   // the door swung open
            z.solid.Box(col, c - z.front * (D / 2f - .05f) + up * H / 2f, new Vector3(L, H, .2f), rot); foreach (var e in new[] { -1f, 1f }) z.solid.Box(col, c + sd * e * (L / 2f - .05f) + up * H / 2f, new Vector3(.2f, H, D), rot);
            z.solid.Box(rustDark, c + up * .15f, new Vector3(L, .3f, D), rot);
            foreach (var e in new[] { -1.8f, 1.8f }) z.mk.Box(dark, c + sd * e + up * .8f - z.front * .5f, new Vector3(1f, 1f, .6f), rot);
            z.mk.Prism(steel, c + up * .3f + z.front * .5f, up, .03f, 1.4f, 6);
            z.mk.Box(lampM, c + up * (H - .2f) + z.front * .3f, new Vector3(L * .6f, .08f, .2f), rot);
            halos.Add(c + up * 1.6f + z.front * 1.4f, 3f, Sodium, .35f);
            ArtistBits.PointLight(z.root, c + up * 2f + z.front * 2f, Sodium, 10f, 1.6f);
        }

        // ---------------------------------------------------------------- Nickelback
        void Nickelback(Plot z)
        {
            var up = Vector3.up; var ctx = z.ctx; var rot = z.Rot; var sd = z.side;
            // the rig: a steel deck on legs, a drill tower behind hung with lights, a gold star on top
            var s = z.At(-7f, 0);
            z.mk.Box(steel, s + up * 1.2f, new Vector3(14f, .4f, 8f), rot); z.solid.Box(steel, s + up * .7f, new Vector3(14f, 1.4f, 8f), rot);
            foreach (var e in new[] { -1f, 1f }) foreach (var f in new[] { -1f, 1f }) z.mk.Box(rustDark, s + sd * e * 6.5f + z.front * f * 3.5f + up * .5f, new Vector3(.5f, 1f, .5f), rot);
            z.mk.Box(yellow, s + up * 1.42f + z.front * 4f, new Vector3(14f, .06f, .1f), rot);
            var tower = s - z.front * 3f; const float TH = 22f;
            foreach (var e in new[] { -1f, 1f }) foreach (var f in new[] { -1f, 1f }) { var leg = tower + sd * e * (2f - .0f) + z.front * f * 1.2f; z.mk.Box(rust, leg + up * TH / 2f, new Vector3(.35f, TH, .35f), Quaternion.Euler(f * 3f, 0, -e * 3f) * rot); ArtistBits.PostCollider(z.root, leg, TH, .3f); }
            for (var y = 3f; y < TH; y += 3f) { z.mk.Box(rust, tower + up * y, new Vector3(4f, .2f, .2f), rot); z.mk.Box(rust, tower + up * y, new Vector3(.2f, .2f, 2.4f), rot); }
            for (var k = 0; k < 10; k++) { var lp = tower + up * (6f + k * 1.5f) + z.front * 1.4f + sd * ((k % 2) * 2f - 1f) * 1.8f; z.mk.Box(dark, lp, new Vector3(.4f, .4f, .4f), rot); var col = k % 3 == 0 ? Fire : Sodium; z.mk.Prism(GlowM(col, 2.6f), lp + z.front * .2f, z.front, .15f, .05f, 8); halos.Add(lp + z.front * .6f, 1.2f, col, .5f); }
            var star = SongGlow(z, "Rockstar", new Color(1f, .82f, .3f), 2.4f); var sc = tower + up * (TH + 1.8f);
            for (var k = 0; k < 5; k++) { var d = Quaternion.AngleAxis(k * 72f, z.front) * up; z.mk.Prism(star, sc, d, .5f, 1.8f, 5, .05f); }
            halos.Add(sc, 4f, new Color(1f, .82f, .3f), .7f);
            // amps, drums, mics on the deck
            for (var i = 0; i < 6; i++) { var p = s + up * (1.4f + .7f + (i / 3) * 1.4f) - z.front * 2.6f + sd * ((i % 3 - 1f) * 1.5f + (i % 3 == 1 ? 0 : 0)); z.mk.Box(dark, p + sd * 3.5f * (i < 3 ? 1f : 1f), new Vector3(1.4f, 1.4f, .8f), rot); }
            z.mk.Box(dark, s + up * 1.8f - z.front * 1f, new Vector3(2.6f, .8f, 2f), rot); foreach (var x in new[] { -.7f, 0f, .7f }) z.mk.Prism(steel, s + up * 2.2f - z.front * 1f + sd * x, up, .35f, .4f, 12);
            foreach (var x in new[] { -3f, 0f, 3f }) z.mk.Prism(steel, s + up * 1.4f + z.front * 2.5f + sd * x, up, .03f, 1.6f, 6);
            ArtistBits.PointLight(z.root, s + up * 8f + z.front * 8f, Sodium, 30f, 2.4f);
            ArtistBits.PointLight(z.root, s + up * 4f + z.front * 3f, Fire, 18f, 1.6f);
            // "Photograph": a huge picture frame standing on the quarry floor
            var ph = z.At(4f, -12f); var photo = SongGlow(z, "Photograph", new Color(1f, .92f, .75f), .8f);
            z.mk.Box(wood, ph + up * 3f, new Vector3(4.6f, 3.6f, .3f), Quaternion.LookRotation(z.front + sd * .6f));
            z.mk.Box(photo, ph + up * 3f + (z.front + sd * .6f).normalized * .16f, new Vector3(3.8f, 2.8f, .02f), Quaternion.LookRotation(z.front + sd * .6f));
            foreach (var e in new[] { -1.6f, 1.6f }) z.mk.Box(wood, ph + Vector3.Cross(up, (z.front + sd * .6f).normalized) * e + up * .6f, new Vector3(.2f, 1.2f, .2f), Quaternion.identity);
            BoxCol(z.root, ph + up * 2.4f, new Vector3(4.6f, 4.8f, .5f), Quaternion.LookRotation(z.front + sd * .6f));
            life.flashAt = ph + up * 3f;
            // "Bones For The Crows": a wire between two poles where crows sit
            var bones = Song(z, "Bones For The Crows");
            var w0 = z.At(5f, 10f); var w1 = z.At(-3f, 13f); foreach (var w in new[] { w0, w1 }) { z.mk.Prism(wood, w - up * .2f, up, .12f, 6.2f, 6); ArtistBits.PostCollider(z.root, w, 6f, .14f); }
            var wa = w0 + up * 6f; var wb = w1 + up * 6f; z.mk.Box(dark, (wa + wb) / 2f, new Vector3(.03f, .03f, Vector3.Distance(wa, wb)), Quaternion.LookRotation(wb - wa));
            var crowM = WorldMaterials.Solid(new Color(.06f, .06f, .07f));
            for (var k = 1; k < 6; k++) { var p = Vector3.Lerp(wa, wb, k / 6f) + up * .2f; z.mk.Box(crowM, p, new Vector3(.18f, .22f, .35f), Quaternion.LookRotation(sd)); z.mk.Blob(crowM, p + up * .17f + sd * .12f, Vector3.one * .09f, ctx.Rng, 0f); }
            life.crowsAt = s + up * 18f; life.crowSong = bones;
        }

        // ---------------------------------------------------------------- Creed, 3 Doors Down, Breaking Benjamin, Disturbed
        void Creed(Plot z)
        {
            var up = Vector3.up; var ctx = z.ctx; var rot = z.Rot; var sd = z.side;
            var t = z.At(-2f, -9f); var higher = SongGlow(z, "Higher", new Color(1f, .95f, .8f), 2.4f); const float H = 16f;
            foreach (var e in new[] { -1f, 1f }) foreach (var f in new[] { -1f, 1f }) z.mk.Box(steel, t + sd * e * .8f + z.front * f * .8f + up * H / 2f, new Vector3(.18f, H, .18f), rot);
            for (var y = 1.5f; y < H; y += 1.5f) z.mk.Box(steel, t + up * y, new Vector3(1.8f, .08f, 1.8f), rot);
            z.mk.Box(dark, t + up * (H + .4f), new Vector3(1.4f, .8f, 1.4f), rot); z.mk.Prism(higher, t + up * (H + .8f), up, .5f, .4f, 10);
            halos.Add(t + up * (H + 1f), 3f, new Color(1f, .95f, .8f), .7f); ArtistBits.PostCollider(z.root, t, H, 1.2f); life.higherAt = t + up * (H + 1.2f); life.higherSong = Song(z, "Higher");
            var cr = z.At(4f, 9f); var arms = SongGlow(z, "With Arms Wide Open", Hazard, 1.2f);
            z.mk.Box(yellow, cr + up * 3f, new Vector3(.8f, 6f, .8f), rot); ArtistBits.PostCollider(z.root, cr, 6f, .6f);
            foreach (var e in new[] { -1f, 1f }) { var d = (sd * e * .9f + up * .45f).normalized; z.mk.Box(arms, cr + up * 5.6f + d * 3f, new Vector3(.4f, .4f, 6f), Quaternion.LookRotation(d)); }
            var cage = z.At(6f, -2f); var prison = SongGlow(z, "My Own Prison", new Color(.8f, .85f, .9f), .7f);
            for (var k = 0; k < 4; k++) { var a = k * 90f; var d = Quaternion.Euler(0, a, 0) * z.front; var p = cage + d * 1.2f; z.mk.Box(chain, p + up * 1.2f, new Vector3(2.4f, 2.4f, .03f), Quaternion.LookRotation(d)); foreach (var e in new[] { -1.2f, 1.2f }) z.mk.Box(steel, p + Vector3.Cross(up, d) * e + up * 1.2f, new Vector3(.08f, 2.4f, .08f), Quaternion.identity); }
            z.mk.Box(prison, cage + up * 2.45f, new Vector3(2.4f, .05f, 2.4f), rot); BoxCol(z.root, cage + up * 1.2f, new Vector3(2.5f, 2.4f, 2.5f), rot);
        }

        void ThreeDoorsDown(Plot z)
        {
            var up = Vector3.up; var ctx = z.ctx; var rot = z.Rot; var sd = z.side;
            var cart = z.At(-1f, -8f); var kr = SongGlow(z, "Kryptonite", Green, 2.4f);
            z.mk.Box(rust, cart + up * .8f, new Vector3(1.6f, 1f, 2.2f), rot); foreach (var e in new[] { -1f, 1f }) foreach (var f in new[] { -.7f, .7f }) z.mk.Prism(dark, cart + sd * e * .82f + z.front * f + up * .3f, sd * e, .28f, .1f, 10);
            for (var k = 0; k < 6; k++) z.mk.Prism(kr, cart + up * 1.1f + new Vector3(ctx.Range(-.5f, .5f), 0, ctx.Range(-.7f, .7f)), (up + new Vector3(ctx.Range(-.4f, .4f), 0, ctx.Range(-.4f, .4f))).normalized, .18f, ctx.Range(.6f, 1.2f), 6, .05f);
            BoxCol(z.root, cart + up * .8f, new Vector3(1.6f, 1.4f, 2.2f), rot); halos.Add(cart + up * 1.6f, 2.4f, Green, .7f);
            ArtistBits.PointLight(z.root, cart + up * 2f, Green, 10f, 2.2f); life.kryptonite = cart + up * 1.5f;
            var bed = z.At(3f, 8f); var lamp = SongGlow(z, "Here Without You", Sodium, 2f);
            z.mk.Box(rustDark, bed + up * .9f, new Vector3(2.4f, .2f, 4.4f), rot); foreach (var e in new[] { -1f, 1f }) foreach (var f in new[] { -1.5f, 1.5f }) z.mk.Prism(dark, bed + sd * e * 1.2f + z.front * f + up * .45f, sd * e, .45f, .2f, 10);
            BoxCol(z.root, bed + up * .6f, new Vector3(2.4f, 1.2f, 4.4f), rot);
            var chairM = WorldMaterials.Solid(new Color(.55f, .4f, .3f)); var ch = bed + up * 1f;
            z.mk.Box(chairM, ch + up * .45f, new Vector3(.5f, .06f, .5f), rot); z.mk.Box(chairM, ch + up * .8f - z.front * .24f, new Vector3(.5f, .6f, .05f), rot);
            foreach (var e in new[] { -.2f, .2f }) foreach (var f in new[] { -.2f, .2f }) z.mk.Box(chairM, ch + up * .22f + sd * e + z.front * f, new Vector3(.04f, .45f, .04f), rot);
            var pole = bed + sd * 1.6f; z.mk.Prism(steel, ctx.OnGround(pole.x, pole.z) - up * .2f, up, .07f, 4.2f, 6); z.mk.Box(lamp, pole + up * 3.9f - sd * .6f, new Vector3(.4f, .12f, .3f), rot); ArtistBits.PostCollider(z.root, ctx.OnGround(pole.x, pole.z), 4f, .1f);
            halos.Add(pole + up * 3.6f - sd * .6f, 1.6f, Sodium, .6f);
        }

        void BreakingBenjamin(Plot z)
        {
            var up = Vector3.up; var ctx = z.ctx; var rot = z.Rot; var sd = z.side;
            var hp = z.At(-1f, -8f); var agony = SongGlow(z, "Dear Agony", new Color(1f, .2f, .2f), 1.6f);
            z.mk.Prism(rustDark, hp - up * .1f, up, .1f, 1.6f, 6); ArtistBits.PostCollider(z.root, hp, 1.6f, .15f);
            var hc = hp + up * 2.6f;
            foreach (var e in new[] { -1f, 1f }) { z.mk.Box(agony, hc + sd * e * .55f + up * .1f, new Vector3(.3f, 1.6f, .15f), rot * Quaternion.Euler(0, 0, e * 40f)); z.mk.Box(agony, hc + sd * e * .65f + up * .8f, new Vector3(.8f, .3f, .15f), rot * Quaternion.Euler(0, 0, e * -20f)); }
            z.mk.Box(dark, hc + up * .2f, new Vector3(.06f, 1.9f, .2f), rot * Quaternion.Euler(0, 0, 8f));   // the crack down it
            halos.Add(hc, 1.8f, new Color(1f, .25f, .2f), .5f);
            var sg = z.At(4f, 8f); var sign = SongGlow(z, "Give Me A Sign", new Color(.4f, .8f, 1f), 2.2f); life.flickerSign = sign;
            z.mk.Box(dark, sg + up * 3f, new Vector3(3f, 1f, .2f), rot); foreach (var e in new[] { -1.2f, 1.2f }) { z.mk.Prism(steel, sg + sd * e - up * .1f, up, .06f, 2.6f, 6); ArtistBits.PostCollider(z.root, sg + sd * e, 2.6f, .1f); }
            foreach (var y in new[] { -.5f, .5f }) z.mk.Box(sign, sg + up * (3f + y) + z.front * .11f, new Vector3(3f, .06f, .04f), rot);
            ArtistBits.Label(z.labels, "GIVE ME A SIGN", sg + up * 3f + z.front * .12f, z.front, .26f, new Color(.5f, .85f, 1f), TextAnchor.MiddleCenter);
            var wg = z.At(5f, -6f); var angels = SongGlow(z, "Anthem Of The Angels", new Color(.9f, .93f, 1f), .6f);
            z.mk.Prism(rustDark, wg - up * .1f, up, .1f, 2f, 6); ArtistBits.PostCollider(z.root, wg, 2f, .15f);
            foreach (var e in new[] { -1f, 1f }) for (var k = 0; k < 5; k++) { var wr = rot * Quaternion.Euler(0, 0, e * (15f + k * 14f)); z.mk.Box(k % 2 == 0 ? angels : steel, wg + up * 2.4f + sd * e * (.4f + k * .35f) + up * k * .2f, new Vector3(1.8f - k * .2f, .25f, .05f), wr); }
        }

        void Disturbed(Plot z)
        {
            var up = Vector3.up; var ctx = z.ctx; var rot = z.Rot; var sd = z.side;
            var sp = z.At(-1f, -8f); var silence = SongGlow(z, "The Sound of Silence", new Color(1f, .8f, .5f), 2.4f);
            z.mk.Box(dark, sp + up * 1.1f, new Vector3(1.6f, 2.2f, 1f), rot); BoxCol(z.root, sp + up * 1.1f, new Vector3(1.6f, 2.2f, 1f), rot);
            foreach (var y in new[] { .7f, 1.6f }) z.mk.Prism(WorldMaterials.Solid(new Color(.2f, .2f, .2f)), sp + up * y + z.front * .51f, z.front, .5f, .02f, 16);
            for (var k = 0; k < 10; k++) { var a = k * Mathf.PI / 5f; var cp = ctx.OnGround(sp.x + Mathf.Cos(a) * 2f, sp.z + Mathf.Sin(a) * 2f); z.mk.Prism(WorldMaterials.Solid(new Color(.92f, .9f, .82f)), cp, up, .07f, .35f, 8); z.mk.Blob(silence, cp + up * .43f, new Vector3(.04f, .08f, .04f), ctx.Rng, 0f); halos.Add(cp + up * .45f, .5f, new Color(1f, .8f, .5f), .6f); }
            var rod = z.At(4f, 8f); var stricken = SongGlow(z, "Stricken", new Color(.7f, .8f, 1f), 2.4f);
            z.mk.Prism(steel, rod - up * .2f, up, .12f, 9f, 6, .3f); z.mk.Blob(stricken, rod + up * 9f, Vector3.one * .25f, ctx.Rng, 0f); ArtistBits.PostCollider(z.root, rod, 8f, .15f);
            halos.Add(rod + up * 9f, 2f, new Color(.7f, .8f, 1f), .6f); life.rodAt = rod + up * 9f;
        }

        void Others(Plot z)
        {
            var up = Vector3.up; var ctx = z.ctx; var rot = z.Rot; var sd = z.side; var c = z.At(2f, -6f);
            string first = null; if (SongIds.TryGetValue(z.a.name, out var l) && l.Length > 0) first = l[0].id;
            Material G(Color col, float s = 1.6f) { if (first != null) z.lit.Add(first); return SongGlow(first, col, s); }
            switch (z.a.name)
            {
                case "Linkin Park":
                {
                    var bar = G(new Color(1f, .3f, .2f), 1.4f);
                    foreach (var e in new[] { -1.2f, 1.2f }) { z.mk.Box(white(), c + sd * e + up * .5f, new Vector3(.15f, 1f, .15f), rot); }
                    for (var k = 0; k < 5; k++) z.mk.Box(k % 2 == 0 ? bar : white(), c + up * 1f + sd * ((k - 2f) * .5f), new Vector3(.5f, .3f, .1f), rot);
                    ArtistBits.Label(z.labels, "END", c + up * 1.5f + z.front * .07f, z.front, .3f, new Color(1f, .3f, .2f), TextAnchor.MiddleCenter);
                    BoxCol(z.root, c + up * .6f, new Vector3(2.6f, 1.2f, .3f), rot); break;
                }
                case "Seether":
                {
                    z.mk.Box(rustDark, c + up * 1.2f, new Vector3(1.4f, 2.2f, .1f), rot); BoxCol(z.root, c + up * 1.2f, new Vector3(1.4f, 2.4f, .3f), rot);
                    var mirror = G(new Color(.85f, .9f, 1f), .8f);
                    for (var k = 0; k < 6; k++) z.mk.Tri(mirror, c + up * 1.2f + z.front * .06f, c + up * (1.2f + Mathf.Sin(k * 1.05f) * .9f) + sd * Mathf.Cos(k * 1.05f) * .55f + z.front * .06f, c + up * (1.2f + Mathf.Sin(k * 1.05f + .9f) * .9f) + sd * Mathf.Cos(k * 1.05f + .9f) * .55f + z.front * .07f, c - z.front);
                    break;
                }
                case "Hoobastank":
                {
                    var lamp = G(Sodium, 2.2f); z.mk.Prism(steel, c - up * .2f, up, .08f, 4.4f, 6); z.mk.Box(lamp, c + up * 4.2f + z.front * .5f, new Vector3(.4f, .12f, .3f), rot); ArtistBits.PostCollider(z.root, c, 4.2f, .1f);
                    var b = c + z.front * 1.4f + sd * .8f; z.mk.Box(wood, b + up * .45f, new Vector3(1.8f, .08f, .45f), rot); BoxCol(z.root, b + up * .4f, new Vector3(1.8f, .8f, .5f), rot);
                    halos.Add(c + up * 4f + z.front * .5f, 1.6f, Sodium, .6f); break;
                }
                case "Puddle Of Mudd":
                {
                    z.mk.Box(steel, c + up * 1.3f, new Vector3(2f, 2.4f, .1f), rot); z.mk.Box(G(new Color(.85f, .9f, .95f), .6f), c + up * 1.3f + z.front * .06f, new Vector3(1.8f, 2.2f, .02f), rot); BoxCol(z.root, c + up * 1.3f, new Vector3(2f, 2.6f, .3f), rot); break;
                }
                case "NOTHING MORE":
                {
                    z.mk.Box(rustDark, c + up * 1.4f, new Vector3(4f, 2.8f, .3f), rot); BoxCol(z.root, c + up * 1.4f, new Vector3(4f, 2.8f, .4f), rot);
                    for (var k = 0; k < 3; k++) { var sh = c + up * 1.6f + sd * ((k - 1f) * 1.3f) + z.front * .2f; z.mk.Prism(k == 1 ? G(Hazard, 1.2f) : steel, sh, z.front, .55f, .1f, 12); z.mk.Blob(dark, sh + z.front * .12f, Vector3.one * .12f, ctx.Rng, 0f); }
                    break;
                }
                default: Drum(ctx, z.mk, c); break;
            }
        }

        Material white() => WorldMaterials.Solid(new Color(.92f, .92f, .9f));
    }
}
