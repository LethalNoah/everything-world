using System.Collections.Generic;
using UnityEngine;

namespace EverythingWorld.World
{
    /// The Math Rock Escarpment's artists. Above the escarpment each artist has a little wooden terrace with a bench
    /// and a wind chime; below it, a clearing of mown grass ringed with low stones. Covers on a wooden board under a
    /// small roof, a plaque, a lantern; songs without their own piece get a wind chime whose tubes glow while the song
    /// plays (SongChimes).
    /// - MONO (below, by the big fall): a long viewing deck at the edge of the clearing with a bench facing the sea; a
    ///   pale bare tree with ash drifting down round it ("Ashes in the Snow"); a patch of gerberas ("Gerbera"); low
    ///   white-flowered shrubs ("Winter Daphne"); a small boat on a still pool ("Dream Odyssey"); a tall slender lamp
    ///   that sends a beam into the sky in its song ("Everlasting Light"). While MONO plays, the quiet side swells with
    ///   the song: its lights and mist rise slowly toward the end, the way the songs build (MathLife).
    /// - tricot: a little wooden house front with paper screens lit warm and a row of glass wind bells; two bottles with
    ///   a hazard sign between them ("混ぜるな危険"); a pot of soup steaming on a stove ("potage"); an easel with a
    ///   painting ("artsick"); a pair of numbered stones ("18,19").
    /// - toe: a small wooden stage under a big autumn tree hung with string lights; two moons on poles ("two moons":
    ///   two moons rise in the sky); a sunflower ("サニーボーイ・ラプソディ"); a ring of stones round a strange glyph
    ///   ("エソテリック").
    /// Then the others (MathMore).
    public partial class MathKit
    {
        class Plot
        {
            public LandContext ctx; public Transform root, labels; public MeshKit mk, solid, pictures;
            public IslandWorldData.Artist a; public Vector3 c, front, side; public ArtistArea area; public HashSet<string> lit = new HashSet<string>(); public bool below;
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
                Add("MONO", "Everlasting Light", "MONO"); Add("MONO", "Ashes in the Snow", "@the big fall and the house");
                Add("toe", "two moons", "@the arrival and the escarpment"); Add("Elephant Gym", "春雨", "Elephant Gym");
                Add("65daysofstatic", "Supermoon", "@the arrival and the escarpment"); Add("Chon", "Bubble Dream", "Chon");
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
            if (wood == null) MakeMaterials();
            var root = new GameObject("Artist: " + a.name).transform; root.SetParent(ctx.Root, false);
            var front = facing.TryGetValue(a.name, out var f) ? f : new Vector3(center.x - a.x, 0, center.z - a.z).normalized;
            if (front.sqrMagnitude < .01f) front = Vector3.forward;
            var labels = new GameObject("Labels").transform; labels.SetParent(root, false);
            var area = root.gameObject.AddComponent<ArtistArea>();
            area.artistName = a.name; area.stationId = a.stationId; area.radius = a.r; area.glowColor = Warm;
            var z = new Plot { ctx = ctx, root = root, labels = labels, mk = new MeshKit(), solid = new MeshKit(), pictures = new MeshKit(), a = a, c = ctx.OnGround(a.x, a.z), front = front, side = Vector3.Cross(Vector3.up, front), area = area, below = Below(a.x, a.z) };
            var reach = Foot(a); var up = Vector3.up;
            if (z.below)
            {
                // a clearing of mown grass ringed with low stones
                z.mk.Prism(WorldMaterials.Solid(new Color(.48f, .52f, .3f)), z.c - up * .3f, up, reach * .62f, .36f, 20);
                for (var k = 0; k < 14; k++) { var an = k * Mathf.PI / 7f; var p = ctx.OnGround(z.c.x + Mathf.Cos(an) * reach * .66f, z.c.z + Mathf.Sin(an) * reach * .66f); if (Vector3.Dot((p - z.c).normalized, front) > .85f) continue; z.mk.Blob(stone, p + up * .15f, new Vector3(.45f, .3f, .4f), ctx.Rng, .3f); }
            }
            else
            {
                // a wooden terrace with a bench and a wind chime
                z.mk.Prism(wood, z.c - up * .3f, up, reach * .62f, .42f, 8, 1f, 22.5f); z.solid.Prism(wood, z.c - up * .3f, up, reach * .62f, .42f, 8, 1f, 22.5f);
                for (var k = -3; k <= 3; k++) z.mk.Box(woodDark, z.c + z.side * k * reach * .17f + up * .13f, new Vector3(.05f, .02f, reach * 1.1f), z.Rot);
            }
            switch (a.name)
            {
                case "MONO": Mono(z); break;
                case "tricot": Tricot(z); break;
                case "toe": Toe(z); break;
                default: Others(z); break;
            }
            SongChimes(z);
            Board(z, z.At(reach * .78f, -reach * .5f - 2.5f), front);
            ArtistBits.Plaque(z.mk, z.solid, labels, a, z.At(reach * .78f + 1.5f, reach * .5f + 2.5f), front, woodDark, wood, Ink, 1.6f);
            Lantern(ctx, z.mk, z.At(reach * .78f, reach * .5f + 5f));
            if (!z.below && a.name != "tricot" && a.name != "toe") { Bench(ctx, z.mk, z.At(reach * .35f, -reach * .4f), -z.side * .3f + z.front, false); WindChime(ctx, z.mk, z.At(-reach * .45f, reach * .45f)); }
            if (z.area.glowLight == null) z.area.glowLight = ArtistBits.PointLight(root, z.c + up * 6f + front * 4f, Warm, HasStation(a) ? 24f : 17f, 1.8f);
            area.glowRenderers = new Renderer[0];
            z.mk.Build("Area", root, false);
            z.solid.BuildCollider("Walls", root);
            z.pictures.Build("Covers", root, false);
            noGrass.Add(new Vector3(z.c.x, reach * .9f, z.c.z));
            if (HasStation(a) || reach >= 11f || a.name == "Explosions In The Sky" || a.name == "American Football")
            {
                var mark = new GameObject("Shot: " + a.name + " up close").transform; mark.SetParent(ctx.Root, false);
                var cam = z.At(reach * .6f, reach * .35f) + up * 1.7f; var look = z.At(-reach * .35f, 0) + up * 2.2f - cam; mark.SetPositionAndRotation(cam, Quaternion.LookRotation(look.normalized));
            }
        }

        /// The covers on a wooden board under a little shingle roof.
        void Board(Plot z, Vector3 ground, Vector3 dir)
        {
            dir = Vector3.ProjectOnPlane(dir, Vector3.up).normalized; var rot = Quaternion.LookRotation(dir); var side = Vector3.Cross(Vector3.up, dir);
            var n = Mathf.Max(1, Mathf.Min(6, z.a.songList?.Length ?? 1)); const float size = .9f; var w = n * size * 1.25f + .6f;
            var c = ground + Vector3.up * 1.7f;
            z.mk.Box(wood, c - Vector3.up * .5f, new Vector3(w, size + 1.6f, .2f), rot); z.solid.Box(wood, c - Vector3.up * .5f, new Vector3(w, size + 1.6f, .4f), rot);
            foreach (var s in new[] { -1f, 1f }) z.mk.Box(woodDark, c + side * s * (w / 2f) - Vector3.up * 1.1f, new Vector3(.2f, size + 2.6f, .25f), rot);
            z.mk.GableRoof(woodDark, c + Vector3.up * (size / 2f + .4f), new Vector3(.9f, 0, w + .5f), .5f, Quaternion.LookRotation(side));
            ArtistBits.CoverWall(z.pictures, z.labels, z.ctx, z.a, c + dir * .11f + Vector3.up * .08f, dir, n, size, n, true, Ink);
        }

        /// A wind chime for each song without a piece of its own, its tubes glowing while the song plays.
        void SongChimes(Plot z)
        {
            if (!SongIds.TryGetValue(z.a.name, out var list)) return;
            var k = 0; var seen = new HashSet<string>();
            foreach (var (_, id) in list)
            {
                if (z.lit.Contains(id) || !seen.Add(id)) continue;
                var p = z.At(-Foot(z.a) * .2f - (k % 2) * 1.8f, Foot(z.a) * .5f + (k / 2) * 2f);
                WindChime(z.ctx, z.mk, p, SongGlow(id, Warm, 1.8f)); halos.Add(p + Vector3.up * 2.1f, 1f, Warm, .45f);
                k++;
            }
        }

        // ---------------------------------------------------------------- MONO
        void Mono(Plot z)
        {
            var up = Vector3.up; var ctx = z.ctx; var rot = z.Rot; var toSea = new Vector3(sea2.x, 0, sea2.y);
            // the viewing deck at the seaward edge of the clearing, a bench on it facing the sea
            var deck = z.c + toSea * 9f; deck = ctx.OnGround(deck.x, deck.z); var dRot = Quaternion.LookRotation(toSea); var dsd = Vector3.Cross(up, toSea);
            z.mk.Box(wood, deck + up * .35f, new Vector3(9f, .2f, 3.4f), dRot); z.solid.Box(wood, deck + up * .2f, new Vector3(9f, .5f, 3.4f), dRot);
            for (var k = -4; k <= 4; k++) { var p = deck + toSea * 1.6f + dsd * k; z.mk.Box(woodDark, p + up * .9f, new Vector3(.1f, 1.1f, .1f), dRot); }
            z.mk.Box(woodDark, deck + toSea * 1.6f + up * 1.45f, new Vector3(9f, .08f, .12f), dRot); z.solid.Box(woodDark, deck + toSea * 1.6f + up * .9f, new Vector3(9f, 1.2f, .2f), dRot);
            Bench(ctx, z.mk, deck + up * .45f, toSea, false);
            // "Ashes in the Snow": a pale bare tree, ash drifting down round it
            var tree = z.At(-6f, -7f); var pale = WorldMaterials.Solid(new Color(.88f, .87f, .85f));
            z.mk.Prism(pale, tree - up * .3f, up, .35f, 5f, 6, .6f);
            for (var k = 0; k < 7; k++) { var d = (Quaternion.Euler(0, k * 51f, 0) * Quaternion.Euler(40f + (k % 3) * 8f, 0, 0) * up).normalized; z.mk.Prism(pale, tree + up * (3f + k * .3f), d, .12f, 3.4f - k * .2f, 5, .2f); }
            z.mk.Blob(SongGlow(z, "Ashes in the Snow", Mist, 1.2f), tree + up * 6.8f, Vector3.one * .3f, ctx.Rng, 0f);
            ArtistBits.PostCollider(z.root, tree, 4f, .4f); life.ashTree = tree + up * 6f;
            // "Gerbera": a patch of bright daisies
            var gb = z.At(3f, -9f); var petals = SongGlow(z, "Gerbera", new Color(1f, .45f, .35f), .8f);
            for (var k = 0; k < 26; k++) { var a = ctx.Range(0, Mathf.PI * 2f); var r = Mathf.Sqrt(ctx.Range(0f, 1f)) * 2.2f; var p = ctx.OnGround(gb.x + Mathf.Cos(a) * r, gb.z + Mathf.Sin(a) * r); z.mk.Prism(WorldMaterials.Solid(new Color(.3f, .45f, .2f)), p, up, .025f, .5f, 4); z.mk.Prism(k % 4 == 0 ? WorldMaterials.Solid(new Color(1f, .85f, .3f)) : petals, p + up * .5f, up, .16f, .03f, 10); }
            halos.Add(gb + up * .6f, 2.4f, new Color(1f, .5f, .4f), .3f);
            // "Winter Daphne": low shrubs with small white flowers
            var wd = SongGlow(z, "Winter Daphne", new Color(1f, .92f, .95f), 1.2f);
            for (var k = 0; k < 4; k++) { var p = z.At(-2f - k * 1.6f, 8f + (k % 2) * 1.2f); z.mk.Blob(WorldMaterials.Solid(new Color(.2f, .32f, .2f)), p + up * .45f, new Vector3(.8f, .55f, .8f), ctx.Rng, .3f); BoxCol(z.root, p + up * .45f, new Vector3(1.5f, 1f, 1.5f), Quaternion.identity); for (var j = 0; j < 6; j++) z.mk.Blob(wd, p + up * .8f + new Vector3(ctx.Range(-.6f, .6f), ctx.Range(-.1f, .2f), ctx.Range(-.6f, .6f)), Vector3.one * .1f, ctx.Rng, 0f); }
            halos.Add(z.At(-4f, 8.5f) + up * .8f, 3f, Mist, .3f);
            // "Dream Odyssey": a small boat on a still pool
            var pool = z.At(-9f, 3f); var dream = SongGlow(z, "Dream Odyssey", new Color(.55f, .75f, 1f), .5f);
            z.mk.Prism(stone, pool - up * .25f, up, 3.3f, .33f, 16); z.mk.Prism(dream, pool + up * .09f, up, 3f, .02f, 20);
            z.mk.Box(wood, pool + up * .3f, new Vector3(.9f, .3f, 2.4f), rot * Quaternion.Euler(0, 35f, 0)); z.mk.Blob(lanternM, pool + up * .7f + z.front * .3f, Vector3.one * .12f, ctx.Rng, 0f);
            BoxCol(z.root, pool, new Vector3(6f, .7f, 6f), rot);
            halos.Add(pool + up * .4f, 3f, new Color(.55f, .75f, 1f), .35f);
            // "Everlasting Light": a tall slender lamp; in its song a beam rises into the sky
            var lamp = z.At(-3f, 3.5f); var ev = SongGlow(z, "Everlasting Light", new Color(1f, .95f, .85f), 2f);
            z.mk.Prism(metal, lamp - up * .2f, up, .1f, 7.2f, 8); ArtistBits.PostCollider(z.root, lamp, 7f, .14f);
            z.mk.Prism(ev, lamp + up * 7f, up, .3f, .6f, 8, .7f); z.mk.Prism(woodDark, lamp + up * 7.6f, up, .4f, .1f, 8);
            halos.Add(lamp + up * 7.2f, 2.6f, new Color(1f, .95f, .85f), .6f);
            life.monoLamp = lamp + up * 7.6f; life.monoAt = z.c;
            ArtistBits.PointLight(z.root, lamp + up * 6f, new Color(1f, .93f, .8f), 22f, 1.6f);
        }

        // ---------------------------------------------------------------- tricot
        void Tricot(Plot z)
        {
            var up = Vector3.up; var ctx = z.ctx; var rot = z.Rot;
            // a little house front: an open deck, paper screens lit warm, a row of glass wind bells under the eave
            var h = z.At(-8f, 0); const float W = 9f;
            z.mk.Box(wood, h + up * .5f, new Vector3(W, .3f, 3f), rot); z.solid.Box(wood, h + up * .3f, new Vector3(W, .6f, 3f), rot);
            z.mk.Box(woodDark, h + up * 2.3f - z.front * 1.5f, new Vector3(W, 3.6f, .2f), rot); z.solid.Box(woodDark, h + up * 2.3f - z.front * 1.5f, new Vector3(W, 3.6f, .3f), rot);
            var screen = GlowM(new Color(1f, .88f, .65f), 1.3f);
            for (var k = 0; k < 4; k++) { var p = h + up * 2.1f - z.front * 1.38f + z.side * ((k - 1.5f) * 2.1f); z.mk.Box(screen, p, new Vector3(1.9f, 2.8f, .03f), rot); for (var j = 1; j < 4; j++) z.mk.Box(woodDark, p + up * (j * .7f - 1.4f) + z.front * .02f, new Vector3(1.9f, .04f, .02f), rot); z.mk.Box(woodDark, p + z.front * .02f, new Vector3(.04f, 2.8f, .02f), rot); }
            foreach (var e in new[] { -1f, 1f }) z.mk.Box(woodDark, h + z.side * e * (W / 2f - .15f) + z.front * 1.3f + up * 2f, new Vector3(.2f, 3.4f, .2f), rot);
            z.mk.Box(woodDark, h + up * 4.3f, new Vector3(W + 1.4f, .25f, 4.2f), rot * Quaternion.Euler(-10f, 0, 0));
            var bell = GlowM(new Color(.6f, .85f, 1f), 1.4f);
            for (var k = 0; k < 6; k++) { var p = h + up * 3.6f + z.front * 1.6f + z.side * ((k - 2.5f) * 1.3f); z.mk.Box(cloth, p + up * .25f, new Vector3(.01f, .5f, .01f), rot); z.mk.Blob(bell, p - up * .05f, new Vector3(.14f, .16f, .14f), ctx.Rng, 0f); z.mk.Box(cloth, p - up * .45f, new Vector3(.1f, .35f, .01f), rot); }
            halos.Add(h + up * 2f + z.front * .4f, 5f, Warm, .35f);
            ArtistBits.PointLight(z.root, h + up * 2.5f + z.front * 2.5f, Warm, 14f, 1.8f);
            // "混ぜるな危険" (don't mix, dangerous): two bottles with a hazard sign between them
            var bt = z.At(1f, -9f); var hz = SongGlow(z, "混ぜるな", new Color(1f, .85f, .2f), 1.8f);
            z.mk.Box(wood, bt + up * .45f, new Vector3(2.2f, .9f, .8f), rot); BoxCol(z.root, bt + up * .45f, new Vector3(2.2f, .9f, .8f), rot);
            foreach (var (e, col) in new[] { (-.7f, new Color(.4f, .7f, 1f)), (.7f, new Color(1f, .45f, .6f)) }) { z.mk.Prism(GlowM(col, .9f), bt + up * .9f + z.side * e, up, .2f, .7f, 10); z.mk.Prism(WorldMaterials.Solid(new Color(.95f, .95f, .95f)), bt + up * 1.6f + z.side * e, up, .1f, .2f, 8); }
            z.mk.Prism(hz, bt + up * 1.3f, z.front, .4f, .03f, 3, 1f, 0f);
            halos.Add(bt + up * 1.3f + z.front * .3f, 1.2f, new Color(1f, .85f, .2f), .5f);
            // "potage": a soup pot steaming on a little stove
            var st = z.At(1f, 9f); var pot = SongGlow(z, "potage", new Color(1f, .6f, .25f), 1.6f);
            z.mk.Box(metal, st + up * .45f, new Vector3(.9f, .9f, .7f), rot); BoxCol(z.root, st + up * .45f, new Vector3(.9f, .9f, .7f), rot);
            z.mk.Prism(pot, st + up * .9f, up, .05f, .02f, 10);
            z.mk.Prism(WorldMaterials.Solid(new Color(.75f, .25f, .2f)), st + up * .92f, up, .34f, .45f, 12); z.mk.Prism(WorldMaterials.Solid(new Color(.9f, .75f, .4f)), st + up * 1.36f, up, .3f, .02f, 12);
            life.steamAt.Add(st + up * 1.4f); halos.Add(st + up * .9f, 1f, new Color(1f, .6f, .25f), .5f);
            // "artsick": an easel with a painting
            var ea = z.At(4.5f, -7f); var paint = SongGlow(z, "artsick", new Color(1f, .5f, .7f), 1f);
            foreach (var e in new[] { -.4f, .4f }) z.mk.Box(woodDark, ea + up * .9f + z.side * e, new Vector3(.06f, 1.9f, .06f), rot * Quaternion.Euler(-8f, 0, 0));
            z.mk.Box(woodDark, ea + up * .9f - z.front * .5f, new Vector3(.06f, 1.9f, .06f), rot * Quaternion.Euler(20f, 0, 0));
            z.mk.Box(cloth, ea + up * 1.4f + z.front * .1f, new Vector3(1.2f, .9f, .04f), rot * Quaternion.Euler(-8f, 0, 0));
            z.mk.Box(paint, ea + up * 1.4f + z.front * .13f, new Vector3(.9f, .6f, .02f), rot * Quaternion.Euler(-8f, 0, 0));
            ArtistBits.PostCollider(z.root, ea, 1.8f, .5f);
            // "18,19": a pair of numbered stones
            var ns = Song(z, "18,19");
            for (var k = 0; k < 2; k++) { var p = z.At(5f, 4f + k * 1.6f); z.mk.Box(stone, p + up * .4f, new Vector3(1f, .8f, .5f), rot); BoxCol(z.root, p + up * .4f, new Vector3(1f, .8f, .5f), rot); ArtistBits.Label(z.labels, (18 + k).ToString(), p + up * .45f + z.front * .26f, z.front, .32f, Ink, TextAnchor.MiddleCenter); z.mk.Box(SongGlow(ns, Warm, 1.6f), p + up * .83f, new Vector3(.8f, .04f, .3f), rot); }
        }

        // ---------------------------------------------------------------- toe
        void Toe(Plot z)
        {
            var up = Vector3.up; var ctx = z.ctx; var rot = z.Rot;
            // a small wooden stage under a big autumn tree hung with string lights
            var s = z.At(-6f, 0);
            z.mk.Box(wood, s + up * .4f, new Vector3(7f, .8f, 4.4f), rot); z.solid.Box(wood, s + up * .4f, new Vector3(7f, .8f, 4.4f), rot);
            foreach (var e in new[] { -1.8f, 1.8f }) { z.mk.Box(woodDark, s + up * 1.25f + z.side * e - z.front * 1.2f, new Vector3(.8f, .9f, .5f), rot); z.mk.Box(WorldMaterials.Solid(new Color(.2f, .2f, .2f)), s + up * 1.25f + z.side * e - z.front * .94f, new Vector3(.7f, .7f, .02f), rot); }
            z.mk.Prism(metal, s + up * .8f + z.front * 1f, up, .03f, 1.4f, 6);
            var tree = z.At(-9.5f, 4f); AutumnTree(ctx, z.mk, tree, 11f);
            var branchA = tree + up * 6.5f; StringLights(z.mk, branchA, s + up * 4f - z.side * 3.6f + z.front * 2f, 1.2f); StringLights(z.mk, branchA, s + up * 4f + z.side * 3.4f + z.front * 2f, 1.4f);
            foreach (var e in new[] { -3.6f, 3.4f }) { var pp = s + z.side * e + z.front * 2f; z.mk.Box(woodDark, ctx.OnGround(pp.x, pp.z) + up * 2f, new Vector3(.15f, 4f, .15f), Quaternion.identity); ArtistBits.PostCollider(z.root, ctx.OnGround(pp.x, pp.z), 3.8f, .12f); }
            ArtistBits.PointLight(z.root, s + up * 3f + z.front * 3f, Warm, 14f, 1.8f);
            // "two moons": two moons on tall poles
            var tm = SongGlow(z, "two moons", new Color(.95f, .95f, .85f), 1.6f);
            foreach (var (e, h) in new[] { (-1f, 6.4f), (1f, 5f) }) { var p = z.At(2f, e * 7f); z.mk.Prism(woodDark, p - up * .2f, up, .07f, h, 6); z.mk.Blob(tm, p + up * (h + .5f), Vector3.one * .55f, ctx.Rng, 0f); halos.Add(p + up * (h + .5f), 1.8f, new Color(.95f, .95f, .85f), .5f); ArtistBits.PostCollider(z.root, p, h, .1f); }
            // "サニーボーイ・ラプソディ" (Sunny Boy Rhapsody): a tall sunflower
            var sf = z.At(4f, -3f); var sun = SongGlow(z, "サニーボーイ", new Color(1f, .8f, .15f), 1.4f);
            z.mk.Prism(WorldMaterials.Solid(new Color(.3f, .45f, .15f)), sf - up * .1f, up, .06f, 2.8f, 6);
            z.mk.Prism(WorldMaterials.Solid(new Color(.3f, .18f, .1f)), sf + up * 2.85f - z.front * .05f, z.front, .3f, .1f, 12);
            for (var k = 0; k < 12; k++) { var a = k * 30f; z.mk.Box(sun, sf + up * 2.85f + Quaternion.AngleAxis(a, z.front) * up * .45f, new Vector3(.14f, .35f, .03f), Quaternion.LookRotation(z.front, Quaternion.AngleAxis(a, z.front) * up)); }
            ArtistBits.PostCollider(z.root, sf, 2.6f, .12f);
            // "エソテリック" (esoteric): a ring of stones round a strange glyph
            var gl = z.At(4f, 4f); var glyph = SongGlow(z, "エソテリック", new Color(.6f, .8f, 1f), 1.6f);
            for (var k = 0; k < 7; k++) { var a = k * Mathf.PI * 2f / 7f; var p = ctx.OnGround(gl.x + Mathf.Cos(a) * 1.8f, gl.z + Mathf.Sin(a) * 1.8f); z.mk.Box(stone, p + up * .35f, new Vector3(.35f, .7f, .3f), Quaternion.Euler(0, -a * Mathf.Rad2Deg, 0)); ArtistBits.PostCollider(z.root, p, .7f, .2f); }
            for (var k = 0; k < 3; k++) z.mk.Box(glyph, gl + up * .06f, new Vector3(.08f, .02f, 2.4f - k * .5f), Quaternion.Euler(0, k * 60f + 15f, 0));
            z.mk.Prism(glyph, gl + up * .05f, up, .9f, .02f, 3, 1f, 30f);
            halos.Add(gl + up * .5f, 2f, new Color(.6f, .8f, 1f), .4f);
        }
    }
}
