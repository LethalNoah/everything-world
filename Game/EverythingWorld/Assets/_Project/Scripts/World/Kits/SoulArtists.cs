using System.Collections.Generic;
using UnityEngine;

namespace EverythingWorld.World
{
    /// The Soul Lowlands' artists. Most have a porch of their own (a painted front, a lit window, a rocking chair to sit
    /// in) with something from their song out front; covers on a painted board under a row of bulbs, a plaque; a
    /// glowing 45 on a stand for every song without a piece of its own (SongLamps).
    /// - Stevie Wonder (south of the square, the largest): a round bandstand with a clavinet to play; brass horns on
    ///   stands out front ("Sir Duke": the town's bulbs chase and notes rise); a ladder against a wall, a cracked
    ///   mirror and a black cat ("Superstition": the lamps flicker and the light turns strange); a spiral of steps up
    ///   to a beacon ("Higher Ground"); a giant sealed letter ("Signed, Sealed, Delivered").
    /// - Otis Redding (on the shore): a dock out over the water with a chair at the end to sit and watch from
    ///   ("(Sittin' On) the Dock of the Bay": the light softens and gulls come); a table for two with a rose ("Try a
    ///   Little Tenderness").
    /// - Ray Charles: a grand piano to sit at; a peach tree and a road sign ("Georgia on My Mind": blossom drifts); an
    ///   electric piano between two horns, call and response ("What'd I Say").
    /// - Marvin Gaye: a little mountain to climb with a flag on top, a river with a plank across ("Ain't No Mountain
    ///   High Enough"). Sam Cooke: an outdoor schoolroom, a globe, books to carry ("Wonderful World"). Ben E. King: a
    ///   moon on a tall post and two pairs of footprints ("Stand By Me": night falls and the moon is the only light).
    /// - The gospel singers round the chapel, and the rest: a pool to wade in, a little ark, a sunburst, five rising
    ///   steps, giant buttercups, an organ and a crate of green onions, and so on (Others).
    public partial class SoulKit
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
                Add("Stevie Wonder", "Superstition", "Stevie Wonder"); Add("Stevie Wonder", "Sir Duke", "@main street"); Add("Otis Redding", "(Sittin' On) the Dock", "Otis Redding");
                Add("Ben E. King", "Stand By Me", "Ben E. King"); Add("The Edwin Hawkins Singers", "Oh Happy Day", "@the chapel"); Add("Ray Charles", "Georgia", "Ray Charles");
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
            if (brick == null) MakeMaterials();
            var root = new GameObject("Artist: " + a.name).transform; root.SetParent(ctx.Root, false);
            var front = facing.TryGetValue(a.name, out var f) ? f : new Vector3(center.x - a.x, 0, center.z - a.z).normalized;
            if (front.sqrMagnitude < .01f) front = Vector3.forward;
            if (a.name == "Otis Redding" && hasRiver) front = -E;   // his back to the river: the dock runs out behind him
            var labels = new GameObject("Labels").transform; labels.SetParent(root, false);
            var area = root.gameObject.AddComponent<ArtistArea>();
            area.artistName = a.name; area.stationId = a.stationId; area.radius = a.r; area.glowColor = Gold;
            var z = new Plot { ctx = ctx, root = root, labels = labels, mk = new MeshKit(), solid = new MeshKit(), pictures = new MeshKit(), a = a, c = ctx.OnGround(a.x, a.z), front = front, side = Vector3.Cross(Vector3.up, front), area = area };
            var reach = Foot(a); var up = Vector3.up;
            z.mk.Prism(brick, z.c - up * .25f, up, reach * .62f, .33f, 18);
            switch (a.name)
            {
                case "Stevie Wonder": Stevie(z); break;
                case "Otis Redding": Otis(z); break;
                case "Ray Charles": RayCharles(z); break;
                case "Marvin Gaye": Marvin(z); break;
                case "Sam Cooke": SamCooke(z); break;
                case "Ben E. King": BenEKing(z); break;
                default: Porch(z); Others(z); break;
            }
            SongLamps(z);
            Board(z, z.At(reach * .6f, -reach * .45f - 1.5f), front);
            ArtistBits.Plaque(z.mk, z.solid, labels, a, z.At(reach * .6f + 1.5f, reach * .45f + 1.5f), front, woodDark, gold, Cream, 1.6f);
            GlobeLamp(ctx, z.mk, z.At(reach * .5f, reach * .62f));
            if (z.area.glowLight == null) z.area.glowLight = ArtistBits.PointLight(root, z.c + up * 5f + front * 3f, Gold, HasStation(a) ? 24f : 16f, 2f);
            area.glowRenderers = new Renderer[0];
            z.mk.Build("Area", root, false);
            z.solid.BuildCollider("Walls", root);
            z.pictures.Build("Covers", root, false);
            noGrass.Add(new Vector3(z.c.x, reach * .9f, z.c.z));
            if (HasStation(a) || reach >= 10f)
            {
                var mark = new GameObject("Shot: " + a.name + " up close").transform; mark.SetParent(ctx.Root, false);
                var cam = z.At(reach * .75f, reach * .3f) + up * 1.9f; var look = z.At(-reach * .3f, 0) + up * 2.2f - cam; mark.SetPositionAndRotation(cam, Quaternion.LookRotation(look.normalized));
            }
        }

        /// The covers on a cream-painted board between two posts, a row of bulbs along the top.
        void Board(Plot z, Vector3 ground, Vector3 dir)
        {
            dir = Vector3.ProjectOnPlane(dir, Vector3.up).normalized; var rot = Quaternion.LookRotation(dir); var side = Vector3.Cross(Vector3.up, dir);
            var n = Mathf.Max(1, Mathf.Min(7, z.a.songList?.Length ?? 1)); const float size = .9f; var w = n * size * 1.25f + .6f;
            var c = ground + Vector3.up * 1.7f;
            z.mk.Box(cream, c - Vector3.up * .1f, new Vector3(w, size + .9f, .08f), rot); z.solid.Box(cream, c - Vector3.up * .5f, new Vector3(w, size + 1.6f, .3f), rot);
            foreach (var s in new[] { -1f, 1f }) z.mk.Box(woodDark, c + side * s * (w / 2f + .08f) - Vector3.up * .7f, new Vector3(.16f, size + 2.4f, .2f), rot);
            z.mk.Box(velvet, c + Vector3.up * (size / 2f + .42f), new Vector3(w + .3f, .14f, .14f), rot);
            for (var k = 0; k < Mathf.RoundToInt(w / .45f); k++) z.mk.Blob(bulbM[k % 3], c + Vector3.up * (size / 2f + .26f) + side * (-w / 2f + .25f + k * .45f) + dir * .1f, Vector3.one * .07f, prng, 0f);
            ArtistBits.CoverWall(z.pictures, z.labels, z.ctx, z.a, c + dir * .05f + Vector3.up * .08f, dir, n, size, n, true, Ink);
        }

        /// A glowing 45 on a little stand for each song without its own piece.
        void SongLamps(Plot z)
        {
            if (!SongIds.TryGetValue(z.a.name, out var list)) return;
            var up = Vector3.up; var k = 0; var seen = new HashSet<string>();
            foreach (var (_, id) in list)
            {
                if (z.lit.Contains(id) || !seen.Add(id)) continue;
                var p = z.At(Foot(z.a) * .55f - (k % 2) * 1.4f, Foot(z.a) * .2f + (k / 2) * 1.4f);
                z.mk.Prism(iron, p - up * .2f, up, .04f, 1.5f, 6); ArtistBits.PostCollider(z.root, p, 1.3f, .08f);
                z.mk.Prism(dark, p + up * 1.55f - z.front * .02f, z.front, .3f, .03f, 16); z.mk.Prism(SongGlow(id, Gold, 2f), p + up * 1.55f + z.front * .015f, z.front, .11f, .02f, 12);
                halos.Add(p + up * 1.55f + z.front * .3f, .9f, Gold, .5f);
                k++;
            }
        }

        /// A porch front: a deck, a painted wall with a door and a lit window, a tin roof on two posts, a rocking chair.
        void Porch(Plot z)
        {
            var up = Vector3.up; var reach = Foot(z.a); var g = z.At(-reach * .32f, 0); var rot = z.Rot; var paint = Paint(z.a.name.Length + z.a.name[0]);
            z.mk.Box(boardM, g + up * .18f, new Vector3(5f, .36f, 2.8f), rot); z.solid.Box(boardM, g - up * .1f, new Vector3(5f, .9f, 2.8f), rot); z.mk.Box(wood, g + up * .1f + z.front * 1.7f, new Vector3(1.6f, .2f, .5f), rot);
            z.mk.Box(paint, g + up * 1.9f - z.front * 1.25f, new Vector3(5f, 3.2f, .3f), rot); z.solid.Box(paint, g + up * 1.9f - z.front * 1.25f, new Vector3(5f, 3.2f, .3f), rot);
            z.mk.Box(white, g + up * 1.4f - z.front * 1.08f - z.side * 1.2f, new Vector3(.95f, 2.1f, .06f), rot); z.mk.Box(windowM, g + up * 1.85f - z.front * 1.08f + z.side * 1.1f, new Vector3(1.3f, 1.2f, .06f), rot); z.mk.Box(white, g + up * 1.85f - z.front * 1.06f + z.side * 1.1f, new Vector3(.06f, 1.2f, .06f), rot);
            foreach (var e in new[] { -1f, 1f }) { z.mk.Box(white, g + up * 1.7f + z.front * 1.25f + z.side * e * 2.35f, new Vector3(.14f, 2.7f, .14f), rot); ArtistBits.PostCollider(z.root, g + z.front * 1.25f + z.side * e * 2.35f, 3f, .1f); }
            z.mk.Box(WorldMaterials.Solid(new Color(.5f, .52f, .55f), .5f), g + up * 3.3f + z.front * .1f, new Vector3(5.6f, .1f, 3.4f), rot * Quaternion.Euler(8f, 0, 0));
            halos.Add(g + up * 1.85f - z.front * .5f + z.side * 1.1f, 1.5f, Gold, .4f);
            var rc = g + up * .36f + z.side * 1.3f - z.front * .2f; z.mk.Box(wood, rc + up * .42f, new Vector3(.6f, .06f, .55f), rot); z.mk.Box(wood, rc + up * .85f - z.front * .26f, new Vector3(.6f, .8f, .06f), rot * Quaternion.Euler(-8f, 0, 0)); foreach (var e in new[] { -.3f, .3f }) z.mk.Box(woodDark, rc + up * .06f + z.side * e, new Vector3(.05f, .08f, .9f), rot);
            Interact.Seat(tlife.transform, rc + up * .5f, z.front, "Sit in the rocking chair");
        }

        void Say1(Vector3 at, string text) => PlaceTools.Say(tlife.transform, at, text, 4.5f);

        // ---------------------------------------------------------------- Stevie Wonder
        void Stevie(Plot z)
        {
            var up = Vector3.up; var rot = z.Rot; var c = z.At(-3f, 0);
            // the bandstand: a round deck, eight posts, a pointed roof with a gold ball, bulbs round the eave
            z.mk.Prism(boardM, c, up, 5.2f, .7f, 16); z.solid.Prism(boardM, c - up * .3f, up, 5.2f, 1f, 16); z.mk.Prism(velvet, c + up * .3f, up, 5.3f, .12f, 16);
            for (var k = 0; k < 3; k++) { var st = c + z.front * (5.3f + (2 - k) * .4f) + up * ((k + .5f) * .233f); z.mk.Box(boardM, st, new Vector3(2.4f, .233f, .4f), rot); z.solid.Box(boardM, st - up * (k * .117f), new Vector3(2.4f, .233f * (k + 1), .4f), rot); }
            for (var k = 0; k < 8; k++) { var d = Quaternion.Euler(0, k * 45f + 22.5f, 0) * z.front; z.mk.Prism(white, c + d * 4.8f + up * .7f, up, .1f, 3.3f, 6); ArtistBits.PostCollider(z.root, c + d * 4.8f, 4f, .12f); z.mk.Blob(bulbM[k % 3], c + d * 5.1f + up * 3.9f, Vector3.one * .1f, prng, 0f); var d2 = Quaternion.Euler(0, k * 45f, 0) * z.front; z.mk.Blob(bulbM[(k + 1) % 3], c + d2 * 5.3f + up * 3.9f, Vector3.one * .1f, prng, 0f); }
            z.mk.Prism(velvet, c + up * 4f, up, 5.6f, .16f, 16); z.mk.Cone(WorldMaterials.Solid(new Color(.7f, .16f, .24f)), c + up * 4.16f, 5.5f, 2.6f, 16); z.mk.Blob(gold, c + up * 6.9f, Vector3.one * .3f, prng, 0f);
            z.area.glowLight = ArtistBits.PointLight(z.root, c + up * 3.4f, Gold, 26f, 2.4f); halos.Add(c + up * 3f + z.front * 5f, 4f, Gold, .3f);
            // on it: the clavinet to play, a harmonica on a stand, a drum kit, a bench
            var kb = c + up * .7f + z.front * 1.4f; var km = new MeshKit(); km.Box(WorldMaterials.Solid(new Color(.5f, .25f, .18f)), new Vector3(0, .82f, 0), new Vector3(1.5f, .16f, .5f), Quaternion.identity); km.Box(white, new Vector3(0, .91f, .1f), new Vector3(1.36f, .03f, .24f), Quaternion.identity); for (var k = 0; k < 10; k++) km.Box(dark, new Vector3(-.6f + k * .133f, .93f, .04f), new Vector3(.05f, .03f, .13f), Quaternion.identity); foreach (var e in new[] { -.65f, .65f }) km.Box(iron, new Vector3(e, .4f, 0), new Vector3(.05f, .8f, .4f), Quaternion.identity);
            var kgo = km.BuildSingle("Clavinet", tlife.transform, false); kgo.transform.SetPositionAndRotation(kb, Quaternion.LookRotation(-z.front)); BoxCol(z.root, kb + up * .5f, new Vector3(1.5f, 1f, .5f), rot);
            string[] riff = { "That riff. You know the one. Your shoulders start before your hands do.", "Dn-dn, dn-dn-dn, dn... the whole bandstand leans into it.", "The horns answer from out front. Nobody's playing them. Don't worry about it.", "You hit a wrong note with total confidence and it turns out to be the right one." }; var rn = 0;
            Interact.Toy(kgo, Toy.Kind.Custom, "Play the clavinet", new Vector3(1.7f, 1.4f, 1f)).onUse = _ => { life.riff = Time.time; Say1(kb + up * 2.4f, riff[rn++ % riff.Length]); };
            var hs = c + up * .7f - z.side * 2.2f + z.front * 1.8f; z.mk.Prism(iron, hs, up, .02f, 1.3f, 5); z.mk.Box(WorldMaterials.Solid(new Color(.75f, .76f, .8f), .8f), hs + up * 1.33f, new Vector3(.22f, .05f, .07f), rot);
            var dk = c + up * .7f - z.front * 2.2f; z.mk.Prism(velvet, dk + up * .3f, z.front, .5f, .5f, 14); z.mk.Prism(cream, dk + up * .3f + z.front * .5f, z.front, .47f, .02f, 14); foreach (var e in new[] { -1f, 1f }) { z.mk.Prism(velvet, dk + z.side * e * .9f + up * .55f, up, .3f, .25f, 12); z.mk.Prism(gold, dk + z.side * e * 1.2f + up * 1.25f - z.front * .3f, up, .34f, .02f, 14); z.mk.Prism(iron, dk + z.side * e * 1.2f - z.front * .3f, up, .015f, 1.25f, 4); }
            BoxCol(z.root, dk + up * .6f, new Vector3(2.8f, 1.2f, 1.4f), rot);
            var bn = c + up * .7f + z.side * 2.6f - z.front * .6f; z.mk.Box(wood, bn + up * .45f, new Vector3(.5f, .08f, 2f), rot); foreach (var e in new[] { -.8f, .8f }) z.mk.Box(iron, bn + up * .22f + z.front * e, new Vector3(.4f, .44f, .06f), rot); Interact.Bench(tlife.transform, bn + up * .5f - z.front * .6f, bn + up * .5f + z.front * .6f, -z.side);
            // "Sir Duke": brass on stands out front
            var duke = Song(z, "Sir Duke"); var brass = SongGlow(duke, new Color(1f, .78f, .3f), 1.5f);
            for (var k = 0; k < 4; k++) { var h = z.At(5.6f, -3.3f + k * 2.2f); z.mk.Prism(iron, h - up * .2f, up, .03f, 1.4f, 5); z.mk.Prism(iron, h, up, .2f, .04f, 8); var hd = (z.front + up * .5f).normalized; z.mk.Prism(gold, h + up * 1.2f - hd * .4f, hd, .04f, .6f, 6); z.mk.Cone(brass, h + up * 1.2f + hd * .55f, .22f, -.4f, 10); halos.Add(h + up * 1.4f + z.front * .5f, .9f, Gold, .4f); ArtistBits.PostCollider(z.root, h, 1.3f, .1f); }
            life.dukeAt = z.At(6f, 0) + up * 1.6f; life.dukeSong = duke;
            // "Superstition": a ladder against a wall with the writing on it, a cracked mirror, a black cat
            var sup = Song(z, "Superstition"); var sp = z.At(2f, 11f); var srot = Quaternion.LookRotation(-z.side); var eerie = new Color(.7f, .45f, 1f);
            z.mk.Box(brickDark, sp + up * 1.6f, new Vector3(4.4f, 3.2f, .4f), srot); z.solid.Box(brickDark, sp + up * 1.4f, new Vector3(4.4f, 3.6f, .4f), srot); ArtistBits.Label(z.labels, "13", sp + up * 2.2f - z.side * .22f + z.front * 1.1f, -z.side, .8f, new Color(.9f, .85f, .75f), TextAnchor.MiddleCenter); ArtistBits.Label(z.labels, "very superstitious", sp + up * 1.2f - z.side * .22f + z.front * .6f, -z.side, .16f, new Color(.9f, .85f, .75f), TextAnchor.MiddleCenter);
            var lad = sp - z.side * 1.2f - z.front * 1.2f; var lrot = srot * Quaternion.Euler(-18f, 0, 0); foreach (var e in new[] { -.3f, .3f }) z.mk.Box(wood, lad + up * 1.5f + z.front * e, new Vector3(.06f, 3.2f, .06f), lrot); for (var k = 0; k < 7; k++) z.mk.Box(wood, lad + up * (.35f + k * .42f) + z.side * (.42f - k * .135f), new Vector3(.6f, .05f, .05f), srot);
            z.mk.Box(gold, sp + up * 1.5f - z.side * .23f - z.front * 1.5f, new Vector3(1f, 1.5f, .06f), srot); var shard = SongGlow(sup, eerie, 1.4f); for (var k = 0; k < 5; k++) z.mk.Box(shard, sp + up * (1f + k * .22f) - z.side * .27f - z.front * (1.5f + ((k * 37) % 5 - 2) * .14f), new Vector3(.34f - (k % 2) * .1f, .2f, .02f), srot * Quaternion.Euler(0, 0, k * 31f));
            var cat = sp - z.side * 2.4f + z.front * .6f; z.mk.Blob(dark, cat + up * .26f, new Vector3(.2f, .22f, .34f), prng, .05f); z.mk.Blob(dark, cat + up * .52f - z.side * .1f + z.front * .18f, Vector3.one * .13f, prng, 0f); foreach (var e in new[] { -.06f, .06f }) { z.mk.Cone(dark, cat + up * .62f - z.side * .1f + z.front * (.18f + e), .04f, .1f, 4); z.mk.Blob(SongGlow(sup, new Color(.6f, 1f, .4f), 2.2f), cat + up * .54f - z.side * .22f + z.front * (.18f + e), Vector3.one * .025f, prng, 0f); }
            z.mk.Prism(dark, cat + up * .12f - z.front * .3f, (up + z.side * .4f).normalized, .03f, .5f, 4); life.superAt = sp + up * 2f; life.superSong = sup;
            // "Higher Ground": steps winding up to a beacon
            var hg = z.At(2f, -11.5f); var hi = Song(z, "Higher Ground"); const int steps = 11;
            for (var k = 0; k < steps; k++) { var a = k * 36f; var d = Quaternion.Euler(0, a, 0) * z.front; var p = hg + d * 2.2f; var h = (k + 1) * .3f; var rr = Quaternion.LookRotation(d); z.mk.Box(k % 2 == 0 ? cream : (Material)boardM, p + up * (h - .06f), new Vector3(1.9f, .12f, 1.7f), rr); z.solid.Box(cream, p + up * (h / 2f), new Vector3(1.9f, h, 1.7f), rr); }
            z.mk.Prism(white, hg, up, .7f, steps * .3f + .2f, 10); z.solid.Prism(white, hg, up, .7f, steps * .3f + .2f, 10); z.mk.Prism(iron, hg + up * (steps * .3f + .2f), up, .05f, 1.6f, 6); z.mk.Blob(SongGlow(hi, new Color(1f, .9f, .6f), 2.4f), hg + up * (steps * .3f + 2f), Vector3.one * .34f, prng, 0f); halos.Add(hg + up * (steps * .3f + 2f), 2.4f, Gold, .6f);
            ArtistBits.Label(z.labels, "HIGHER GROUND", hg + z.front * 3.4f + up * .5f, z.front, .2f, Ink, TextAnchor.MiddleCenter);
            // "Signed, Sealed, Delivered": a giant letter with a red seal, propped on an easel
            var sl = z.At(8.5f, 6.5f); var ss = Song(z, "Signed, Sealed, Delivered"); var lr = rot * Quaternion.Euler(-12f, 20f, 0); var lf = lr * Vector3.forward;
            foreach (var e in new[] { -1f, 1f }) z.mk.Box(wood, sl + up * 1.1f + (lr * Vector3.right) * e * 1f - lf * .3f, new Vector3(.08f, 2.4f, .08f), lr); z.mk.Box(cream, sl + up * 1.6f, new Vector3(2.8f, 1.8f, .08f), lr); z.mk.Prism(cream, sl + up * 2.5f + lf * .05f, lf, 1.4f, .02f, 3, 1f, 180f); z.mk.Prism(SongGlow(ss, new Color(1f, .25f, .2f), 1.8f), sl + up * 1.75f + lf * .06f, lf, .3f, .05f, 12);
            z.mk.Box(Paint(2), sl + up * 2.15f + (lr * Vector3.right) * 1f + lf * .05f, new Vector3(.45f, .55f, .02f), lr); ArtistBits.Label(z.labels, "I'm yours", sl + up * 1.15f + lf * .06f, lf, .22f, Ink, TextAnchor.MiddleCenter); BoxCol(z.root, sl + up * 1.3f, new Vector3(2.8f, 2.6f, .6f), lr);
        }

        // ---------------------------------------------------------------- Otis Redding
        void Otis(Plot z)
        {
            var up = Vector3.up; var dock = Song(z, "(Sittin' On) the Dock"); var outD = hasRiver ? E : -z.front; var sideD = Vector3.Cross(up, outD); var rot = Quaternion.LookRotation(outD);
            // walk out from the shore: the deck starts where he stands and runs out over the water
            var start = z.c + outD * 3f; var deckY = Mathf.Max(z.c.y, waterY + 1.5f) + .1f; const float Len = 30f, Wd = 3.2f; var o = new Vector3(start.x, deckY, start.z);
            z.mk.Box(boardM, o + outD * (Len / 2f) - up * .1f, new Vector3(Wd, .2f, Len), rot); z.solid.Box(boardM, o + outD * (Len / 2f) - up * .4f, new Vector3(Wd, .8f, Len), rot);
            for (var k = 0; k < 30; k++) z.mk.Box(woodDark, o + outD * (k + .5f) + up * .005f, new Vector3(Wd, .01f, .04f), rot);
            for (var k = 0; k <= 6; k++) foreach (var e in new[] { -1f, 1f }) { var p = o + outD * (k * Len / 6f) + sideD * e * (Wd / 2f - .15f); z.mk.Prism(woodDark, p - up * 4.5f, up, .18f, 5.3f, 7); }
            z.mk.Box(boardM, start - outD * 1.5f + up * (deckY - start.y) * .5f, new Vector3(Wd, .2f, 3.4f), rot * Quaternion.Euler(Mathf.Atan2(deckY - z.c.y, 3f) * -Mathf.Rad2Deg, 0, 0));
            foreach (var e in new[] { -1f, 1f }) BoxCol(z.root, o + outD * (Len / 2f) + sideD * e * (Wd / 2f) + up * .4f, new Vector3(.1f, .8f, Len), rot);
            // the end of the dock: a chair to sit in, a lantern, a tackle box; nothing to do but watch
            var end = o + outD * (Len - 1.2f); z.mk.Box(wood, end + up * .45f - sideD * .6f, new Vector3(.55f, .06f, .5f), rot); z.mk.Box(wood, end + up * .85f - sideD * .6f - outD * .24f, new Vector3(.55f, .75f, .06f), rot); foreach (var (x, zz) in new[] { (-.24f, -.2f), (.24f, -.2f), (.24f, .2f), (-.24f, .2f) }) z.mk.Box(woodDark, end + up * .22f - sideD * (.6f + x) + outD * zz, new Vector3(.05f, .44f, .05f), rot);
            Interact.Seat(tlife.transform, end + up * .52f - sideD * .6f, outD, "Sit on the dock of the bay"); Interact.Seat(tlife.transform, end + outD * 1.1f + sideD * .7f + up * .02f, outD, "Sit on the end of the dock");
            var lp = end + sideD * 1.3f + outD * .8f; z.mk.Prism(woodDark, lp - up * 3f, up, .14f, 5.2f, 6); z.mk.Box(iron, lp + up * 2.1f - sideD * .3f, new Vector3(.6f, .05f, .05f), rot); z.mk.Blob(SongGlow(dock, new Color(1f, .8f, .5f), 2.2f), lp + up * 1.85f - sideD * .55f, new Vector3(.16f, .2f, .16f), prng, 0f); halos.Add(lp + up * 1.85f - sideD * .55f, 2f, Gold, .6f);
            z.area.glowLight = ArtistBits.PointLight(z.root, lp + up * 1.8f - sideD * .6f, Gold, 20f, 2f);
            var wm = new GameObject("Dock whistle"); wm.transform.SetParent(tlife.transform, false); wm.transform.position = end + up * 1f + sideD * .5f;
            string[] wh = { "You whistle the last verse. It goes out over the water and doesn't come back, and that's all right.", "The tide rolls away. You watch it. That's the whole job out here.", "Two thousand miles from somewhere. The lantern doesn't mind." }; var wn = 0;
            Interact.Toy(wm, Toy.Kind.Custom, "Whistle", new Vector3(1f, 1.2f, 1f)).onUse = _ => Say1(end + up * 2.4f, wh[wn++ % wh.Length]);
            life.dockAt = end + up * 3f; life.dockSong = dock;
            // "Try a Little Tenderness": a table for two on the bank, one rose
            var tn = Song(z, "Try a Little Tenderness"); var tb = z.At(3.5f, -5.5f); z.mk.Prism(white, tb, up, .08f, .74f, 6); z.mk.Prism(white, tb + up * .74f, up, .55f, .04f, 14); z.mk.Prism(Paint(4), tb + up * .78f, up, .5f, .01f, 14); BoxCol(z.root, tb + up * .4f, new Vector3(1f, .8f, 1f), z.Rot);
            z.mk.Prism(WorldMaterials.Glow(new Color(.6f, .9f, 1f), .3f), tb + up * .79f, up, .05f, .16f, 6); z.mk.Prism(WorldMaterials.Solid(new Color(.3f, .5f, .25f)), tb + up * .95f, up, .012f, .22f, 4); z.mk.Blob(SongGlow(tn, new Color(1f, .25f, .35f), 2f), tb + up * 1.2f, Vector3.one * .07f, prng, .1f); halos.Add(tb + up * 1.2f, .8f, new Color(1f, .4f, .45f), .5f);
            foreach (var e in new[] { -1f, 1f }) { var ch = tb + z.side * e * .95f; z.mk.Box(white, ch + up * .45f, new Vector3(.45f, .05f, .45f), z.Rot); z.mk.Box(white, ch + up * .8f + z.side * e * .2f, new Vector3(.05f, .7f, .45f), z.Rot); z.mk.Prism(white, ch, up, .04f, .45f, 5); Interact.Seat(tlife.transform, ch + up * .5f, -z.side * e); }
        }

        // ---------------------------------------------------------------- Ray Charles
        void RayCharles(Plot z)
        {
            var up = Vector3.up; var rot = z.Rot; var c = z.At(-2.5f, 0);
            z.mk.Prism(boardM, c, up, 4.2f, .5f, 14); z.solid.Prism(boardM, c - up * .3f, up, 4.2f, .8f, 14); z.mk.Prism(velvet, c + up * .2f, up, 4.3f, .1f, 14);
            for (var k = 0; k < 2; k++) { var st = c + z.front * (4.3f + (1 - k) * .4f) + up * ((k + .5f) * .25f); z.mk.Box(boardM, st, new Vector3(2f, .25f, .4f), rot); z.solid.Box(boardM, st - up * (k * .125f), new Vector3(2f, .25f * (k + 1), .4f), rot); }
            // the grand piano, lid up, a stool to sit at, dark glasses left on the lid
            var p = c + up * .5f - z.side * .3f; z.mk.Box(dark, p + up * .82f, new Vector3(1.5f, .28f, 2.1f), rot); z.mk.Prism(dark, p + up * .68f - z.front * 1.05f + z.side * .1f, up, .78f, .28f, 10); z.mk.Box(dark, p + up * 1.36f + z.side * .5f - z.front * .1f, new Vector3(.05f, 1.2f, 2f), rot * Quaternion.Euler(0, 0, -38f));
            z.mk.Box(white, p + up * .86f + z.front * 1.1f, new Vector3(1.36f, .04f, .2f), rot); foreach (var (x, zz) in new[] { (-.6f, .8f), (.6f, .8f), (0f, -1.4f) }) z.mk.Prism(dark, p + z.side * x + z.front * zz, up, .05f, .7f, 6);
            BoxCol(z.root, p + up * .6f - z.front * .2f, new Vector3(1.6f, 1.2f, 2.8f), rot); z.mk.Box(dark, p + up * .98f + z.side * .2f + z.front * .3f, new Vector3(.3f, .03f, .1f), rot);
            var st2 = p + z.front * 1.75f; z.mk.Prism(dark, st2, up, .05f, .45f, 6); z.mk.Prism(velvet, st2 + up * .45f, up, .22f, .08f, 10); Interact.Seat(tlife.transform, st2 + up * .56f, -z.front, "Sit at the piano");
            z.area.glowLight = ArtistBits.PointLight(z.root, c + up * 4f + z.front * 2f, Gold, 22f, 2.2f);
            // "Georgia on My Mind": a peach tree and a road sign
            var geo = Song(z, "Georgia"); var gt = z.At(1f, 8f); z.mk.Prism(WorldMaterials.Solid(new Color(.4f, .3f, .22f)), gt - up * .3f, up, .2f, 2.6f, 6, .7f); ArtistBits.PostCollider(z.root, gt, 2.4f, .22f); z.mk.Blob(WorldMaterials.Solid(new Color(.4f, .56f, .28f)), gt + up * 3.6f, new Vector3(2f, 1.6f, 2f), prng, .2f);
            var peach = SongGlow(geo, new Color(1f, .62f, .4f), 1.3f); for (var k = 0; k < 10; k++) { var d = Quaternion.Euler(k * 23f - 100f, k * 71f, 0) * Vector3.forward; z.mk.Blob(peach, gt + up * 3.6f + Vector3.Scale(d, new Vector3(1.9f, 1.4f, 1.9f)), Vector3.one * .16f, prng, .1f); }
            var gs = z.At(3.4f, 9.5f); z.mk.Prism(iron, gs - up * .2f, up, .05f, 2.6f, 6); z.mk.Box(WorldMaterials.Solid(new Color(.2f, .45f, .3f)), gs + up * 2.2f, new Vector3(1.9f, .5f, .05f), rot); ArtistBits.Label(z.labels, "GEORGIA", gs + up * 2.2f + z.front * .04f, z.front, .22f, Cream, TextAnchor.MiddleCenter); ArtistBits.PostCollider(z.root, gs, 2.4f, .08f);
            life.georgiaAt = gt + up * 3.6f; life.georgiaSong = geo;
            // "What'd I Say": an electric piano between two horns, the call and the response
            var wd = Song(z, "What'd I Say"); var ep = z.At(1.5f, -8f); z.mk.Box(WorldMaterials.Solid(new Color(.75f, .2f, .2f)), ep + up * .8f, new Vector3(1.3f, .16f, .45f), rot); z.mk.Box(white, ep + up * .89f + z.front * .08f, new Vector3(1.2f, .03f, .2f), rot); foreach (var e in new[] { -.55f, .55f }) z.mk.Box(iron, ep + up * .4f + z.side * e, new Vector3(.04f, .8f, .35f), rot); BoxCol(z.root, ep + up * .5f, new Vector3(1.3f, 1f, .5f), rot);
            foreach (var e in new[] { -1f, 1f }) { var h = ep + z.side * e * 2.4f; z.mk.Prism(iron, h - up * .2f, up, .04f, 1.9f, 5); var hd = (-z.side * e + up * .15f).normalized; z.mk.Cone(SongGlow(wd, e > 0 ? new Color(1f, .8f, .3f) : new Color(.4f, .8f, 1f), 1.6f), h + up * 1.7f + hd * .6f, .34f, -.6f, 10); ArtistBits.Label(z.labels, e > 0 ? "hey!" : "(hey!)", h + up * 2.3f, z.front, .2f, Ink, TextAnchor.MiddleCenter); ArtistBits.PostCollider(z.root, h, 1.8f, .08f); }
        }

        // ---------------------------------------------------------------- Marvin Gaye
        void Marvin(Plot z)
        {
            var up = Vector3.up; var rot = z.Rot; var song = Song(z, "Ain't No Mountain"); var c = z.At(-3f, 0); var stone = WorldMaterials.Solid(new Color(.6f, .52f, .46f)); var stone2 = WorldMaterials.Solid(new Color(.7f, .62f, .52f));
            // the mountain: stone tiers you can climb, a flag on the top
            const int tiers = 9; for (var k = 0; k < tiers; k++) { var r = 4.6f - k * .46f; var h = (k + 1) * .34f; z.mk.Prism(k % 2 == 0 ? stone : stone2, c + up * (h - .34f), up, r, .34f, 9, 1f, k * 11f); z.solid.Prism(stone, c, up, r, h, 9, 1f, k * 11f); }
            var top = c + up * (tiers * .34f); z.mk.Prism(iron, top, up, .04f, 2.4f, 5); z.mk.Box(SongGlow(song, new Color(1f, .8f, .3f), 1.8f), top + up * 2f + z.side * .5f, new Vector3(1f, .6f, .04f), rot); halos.Add(top + up * 2f, 1.8f, Gold, .5f);
            ArtistBits.Label(z.labels, "(not high enough)", c + z.front * 4.7f + up * .5f, z.front, .16f, Ink, TextAnchor.MiddleCenter); Interact.Seat(tlife.transform, top + up * .04f + z.front * .3f, z.front, "Sit on the mountain top");
            z.area.glowLight = ArtistBits.PointLight(z.root, top + up * 2.6f, Gold, 22f, 2f);
            // the river, with a plank across; the valley is wherever you're standing
            var rv = z.At(4.6f, 0); z.mk.Box(SongGlow(song, new Color(.3f, .65f, 1f), .9f), rv + up * .1f, new Vector3(13f, .04f, 1.6f), rot); z.mk.Box(boardM, rv + up * .22f, new Vector3(1.1f, .1f, 2.4f), rot); z.solid.Box(boardM, rv + up * .1f, new Vector3(1.1f, .3f, 2.4f), rot);
            ArtistBits.Label(z.labels, "(not wide enough)", rv + z.side * 3.4f + up * .5f + z.front * .9f, z.front, .16f, Ink, TextAnchor.MiddleCenter);
        }

        // ---------------------------------------------------------------- Sam Cooke
        void SamCooke(Plot z)
        {
            var up = Vector3.up; var rot = z.Rot; var song = Song(z, "(What A) Wonderful World"); var c = z.At(-3.5f, 0);
            // a blackboard, two desks to sit at, a globe that lights, books to carry
            foreach (var e in new[] { -1.6f, 1.6f }) { z.mk.Box(wood, c + z.side * e + up * 1.3f, new Vector3(.1f, 2.6f, .1f), rot); ArtistBits.PostCollider(z.root, c + z.side * e, 2.6f, .08f); }
            z.mk.Box(WorldMaterials.Solid(new Color(.16f, .26f, .2f)), c + up * 1.7f, new Vector3(3.2f, 1.6f, .08f), rot); z.mk.Box(wood, c + up * .86f + z.front * .08f, new Vector3(3.2f, .06f, .16f), rot); BoxCol(z.root, c + up * 1.3f, new Vector3(3.2f, 2.6f, .2f), rot);
            ArtistBits.Label(z.labels, "1 + 1 = 2", c + up * 2f + z.front * .06f, z.front, .34f, Cream, TextAnchor.MiddleCenter); ArtistBits.Label(z.labels, "what a wonderful world this would be", c + up * 1.4f + z.front * .06f, z.front, .13f, Cream, TextAnchor.MiddleCenter);
            for (var k = 0; k < 2; k++) { var d = z.At(.5f, -1.5f + k * 3f); z.mk.Box(wood, d + up * .72f, new Vector3(1.1f, .06f, .6f), rot); foreach (var e in new[] { -.5f, .5f }) z.mk.Box(iron, d + up * .36f + z.side * e, new Vector3(.04f, .72f, .5f), rot); z.mk.Box(wood, d + up * .44f + z.front * .7f, new Vector3(1f, .06f, .35f), rot); foreach (var e in new[] { -.45f, .45f }) z.mk.Box(iron, d + up * .22f + z.front * .7f + z.side * e, new Vector3(.04f, .44f, .3f), rot); BoxCol(z.root, d + up * .4f, new Vector3(1.1f, .8f, .6f), rot); Interact.Seat(tlife.transform, d + up * .5f + z.front * .7f, -z.front, "Sit at the desk"); }
            var gl = z.At(-1.5f, 3.4f); z.mk.Prism(wood, gl, up, .3f, .1f, 10); z.mk.Prism(gold, gl, up, .03f, 1f, 6); z.mk.Blob(SongGlow(song, new Color(.35f, .7f, 1f), 1.6f), gl + up * 1.4f, Vector3.one * .42f, prng, .04f); z.mk.Blob(WorldMaterials.Solid(new Color(.4f, .65f, .35f)), gl + up * 1.48f + z.front * .28f, new Vector3(.2f, .24f, .16f), prng, .2f); halos.Add(gl + up * 1.4f, 1.6f, new Color(.5f, .8f, 1f), .5f); ArtistBits.PostCollider(z.root, gl, 1.8f, .3f);
            z.area.glowLight = ArtistBits.PointLight(z.root, gl + up * 2.4f, new Color(.7f, .85f, 1f), 18f, 1.8f);
            string[] books = { "a history book (don't know much about it)", "a biology book", "a science book", "the French I took" };
            for (var k = 0; k < 4; k++) { var bm = new MeshKit(); bm.Box(Paint(k + 1), new Vector3(0, .03f, 0), new Vector3(.26f, .06f, .34f), Quaternion.identity); bm.Box(cream, new Vector3(.01f, .03f, 0), new Vector3(.25f, .045f, .32f), Quaternion.identity); Thing(bm, "Book", books[k], z.At(.5f, -1.5f + (k / 2) * 3f) + up * (.76f + (k % 2) * .065f), rot * Quaternion.Euler(0, k * 17f, 0), new Vector3(.27f, .07f, .35f), .3f); }
        }

        // ---------------------------------------------------------------- Ben E. King
        void BenEKing(Plot z)
        {
            var up = Vector3.up; var rot = z.Rot; var song = Song(z, "Stand By Me"); var c = z.At(-2.5f, 0);
            // the moon on a tall curved post, a round stone with two pairs of footprints side by side
            z.mk.Prism(iron, c - z.side * 2.4f - up * .2f, up, .1f, 5f, 6); z.mk.Prism(iron, c - z.side * 2.4f + up * 4.8f, (z.side + up * .5f).normalized, .08f, 2.6f, 6); ArtistBits.PostCollider(z.root, c - z.side * 2.4f, 5f, .12f);
            var moonAt = c - z.side * .1f + up * 6.1f; var moon = SongGlow(song, new Color(1f, .96f, .8f), 1.6f); z.mk.Blob(moon, moonAt, Vector3.one * .95f, prng, .03f); halos.Add(moonAt, 4f, new Color(1f, .95f, .8f), .5f); z.area.glowLight = ArtistBits.PointLight(z.root, moonAt - up * 1.2f, new Color(1f, .95f, .82f), 24f, 2f);
            z.mk.Prism(cream, c, up, 2.2f, .16f, 16); z.solid.Prism(cream, c - up * .3f, up, 2.2f, .46f, 16);
            var step = SongGlow(song, new Color(1f, .85f, .5f), 1.5f); foreach (var s in new[] { -.55f, .55f }) foreach (var e in new[] { -.13f, .13f }) { z.mk.Blob(step, c + z.side * (s + e) + up * .17f, new Vector3(.08f, .012f, .15f), prng, 0f); z.mk.Blob(step, c + z.side * (s + e) + up * .17f - z.front * .2f, new Vector3(.06f, .012f, .06f), prng, 0f); }
            ArtistBits.Label(z.labels, "stand here", c + z.front * 1.5f + up * .2f, up, .16f, Ink, TextAnchor.MiddleCenter);
            var bn = z.At(1.5f, 4.5f); TownBenchAt(z, bn, -z.side); life.moonAt = moonAt; life.moonSong = song;
        }

        void TownBenchAt(Plot z, Vector3 g, Vector3 face)
        {
            var up = Vector3.up; var rot = Quaternion.LookRotation(face); var side = Vector3.Cross(up, face); const float len = 2f;
            z.mk.Box(wood, g + up * .45f, new Vector3(len, .07f, .5f), rot); z.mk.Box(wood, g + up * .82f - face * .24f, new Vector3(len, .4f, .06f), rot); foreach (var e in new[] { -1f, 1f }) z.mk.Box(iron, g + up * .32f + side * e * (len / 2f - .12f) - face * .08f, new Vector3(.06f, .95f, .5f), rot);
            BoxCol(z.root, g + up * .5f, new Vector3(len, 1f, .6f), rot); Interact.Bench(tlife.transform, g + up * .52f - side * (len / 2f - .4f), g + up * .52f + side * (len / 2f - .4f), face);
        }

        // ---------------------------------------------------------------- everyone else
        void Others(Plot z)
        {
            var up = Vector3.up; var rot = z.Rot; var p = z.At(2.6f, -2.6f); var q = z.At(2.8f, 2.4f); string s;
            switch (z.a.name)
            {
                case "Kirk Franklin":
                    s = Song(z, "Stomp"); var stomp = SongGlow(s, new Color(1f, .8f, .3f), 1.6f); for (var k = 0; k < 8; k++) { var fp = z.At(1.4f + k * .5f, -3.4f + (k % 2) * .5f + k * .25f); z.mk.Blob(stomp, fp + up * .1f, new Vector3(.09f, .012f, .17f), prng, 0f); }
                    s = Song(z, "I Smile"); var smile = SongGlow(s, new Color(1f, .9f, .4f), 1.8f); for (var k = 0; k < 9; k++) { var a = (200f + k * 17.5f) * Mathf.Deg2Rad; z.mk.Blob(smile, q + up * (2.3f + Mathf.Sin(a) * 1f) + z.side * Mathf.Cos(a) * 1.3f, Vector3.one * .1f, prng, 0f); }
                    foreach (var e in new[] { -.5f, .5f }) z.mk.Blob(smile, q + up * 2.6f + z.side * e, Vector3.one * .12f, prng, 0f); z.mk.Prism(iron, q - up * .2f, up, .04f, 1.4f, 5); halos.Add(q + up * 2.1f, 1.8f, Gold, .4f); ArtistBits.PostCollider(z.root, q, 1.4f, .08f); break;
                case "The Staple Singers":
                    s = Song(z, "Wade In The Water"); z.mk.Prism(cream, p - up * .1f, up, 2.9f, .22f, 16); z.mk.Prism(SongGlow(s, new Color(.3f, .7f, 1f), 1f), p + up * .13f, up, 2.6f, .02f, 16); halos.Add(p + up * .5f, 2.4f, new Color(.4f, .75f, 1f), .3f);
                    s = Song(z, "I'll Take You There"); z.mk.Prism(wood, q - up * .2f, up, .06f, 2.6f, 6); z.mk.Box(SongGlow(s, new Color(1f, .8f, .35f), 1.4f), q + up * 2.2f + z.side * .5f, new Vector3(1.5f, .4f, .05f), rot); z.mk.Prism(SongGlow(s, new Color(1f, .8f, .35f), 1.4f), q + up * 2.2f + z.side * 1.25f - z.front * .025f, z.front, .34f, .05f, 3, 1f, 30f); ArtistBits.Label(z.labels, "THERE", q + up * 2.2f + z.side * .5f + z.front * .04f, z.front, .2f, Ink, TextAnchor.MiddleCenter); ArtistBits.PostCollider(z.root, q, 2.4f, .08f); break;
                case "Syl Johnson":
                    s = Song(z, "Different Strokes"); for (var k = 0; k < 3; k++) { z.mk.Box(wood, p + up * (.2f + k * .4f) + z.side * (k % 2) * .15f, new Vector3(.8f, .38f, .6f), rot * Quaternion.Euler(0, k * 12f, 0)); for (var r = 0; r < 5; r++) z.mk.Box(Paint(k + r), p + up * (.42f + k * .4f) + z.side * (-.28f + r * .14f + (k % 2) * .15f), new Vector3(.02f, .08f, .5f), rot); }
                    BoxCol(z.root, p + up * .6f, new Vector3(.9f, 1.2f, .7f), rot); z.mk.Blob(SongGlow(s, new Color(1f, .5f, .3f), 1.6f), p + up * 1.5f, Vector3.one * .12f, prng, 0f);
                    s = Song(z, "Is It Because"); z.mk.Prism(iron, q - up * .2f, up, .03f, 1.6f, 5); z.mk.Blob(WorldMaterials.Solid(new Color(.75f, .76f, .8f), .8f), q + up * 1.5f, new Vector3(.07f, .1f, .07f), prng, 0f); z.mk.Blob(SongGlow(s, new Color(.7f, .8f, 1f), 1.6f), q + up * 2.6f, Vector3.one * .14f, prng, 0f); halos.Add(q + up * 2.6f, 1.2f, new Color(.7f, .8f, 1f), .5f); ArtistBits.PostCollider(z.root, q, 1.6f, .08f); break;
                case "Mahalia Jackson":
                    s = Song(z, "How I Got Over"); for (var k = 0; k < 7; k++) { var t = (k - 3) / 3f; var bp = p + z.side * (k - 3) * .7f + up * (.9f - t * t * .7f); z.mk.Box(boardM, bp, new Vector3(.74f, .12f, 1.5f), rot * Quaternion.Euler(0, 0, t * 26f)); z.solid.Box(boardM, bp - up * .3f, new Vector3(.74f, .7f, 1.5f), rot * Quaternion.Euler(0, 0, t * 26f)); }
                    foreach (var e in new[] { -1f, 1f }) { z.mk.Prism(white, p + z.side * e * 2.3f - up * .1f, up, .06f, 1.9f, 6); z.mk.Blob(SongGlow(s, new Color(1f, .85f, .5f), 2f), p + z.side * e * 2.3f + up * 1.9f, Vector3.one * .16f, prng, 0f); halos.Add(p + z.side * e * 2.3f + up * 1.9f, 1.2f, Gold, .5f); } break;
                case "The Edwin Hawkins Singers":
                    s = Song(z, "Oh Happy Day"); var sun = SongGlow(s, new Color(1f, .82f, .3f), 2f); z.mk.Prism(white, p - up * .2f, up, .07f, 3f, 6); z.mk.Prism(sun, p + up * 3.6f - z.front * .04f, z.front, .7f, .08f, 16); for (var k = 0; k < 12; k++) { var a = k * 30f; var d = Quaternion.AngleAxis(a, z.front) * up; z.mk.Box(sun, p + up * 3.6f + d * 1.15f, new Vector3(.08f, .5f, .05f), Quaternion.LookRotation(z.front, d)); }
                    halos.Add(p + up * 3.6f + z.front * .4f, 3f, Gold, .6f); ArtistBits.PostCollider(z.root, p, 3f, .1f); life.happyAt = p + up * 3.6f; life.happySong = s; break;
                case "Jubalaires":
                    s = Song(z, "Noah"); z.mk.Box(wood, p + up * .5f, new Vector3(1.4f, .9f, 3.4f), rot); z.mk.Prism(wood, p + up * .05f + z.front * 1.7f, up, .7f, .9f, 3, 1f, rot.eulerAngles.y + 30f); z.mk.Box(Paint(1), p + up * 1.4f - z.front * .2f, new Vector3(1.1f, .9f, 1.8f), rot); z.mk.Box(woodDark, p + up * 1.95f - z.front * .2f, new Vector3(1.4f, .12f, 2.1f), rot); z.solid.Box(wood, p + up * .7f, new Vector3(1.4f, 1.6f, 3.6f), rot);
                    Color[] beasts = { new Color(.9f, .8f, .5f), new Color(.6f, .6f, .62f), new Color(.95f, .95f, .9f) }; for (var k = 0; k < 3; k++) foreach (var e in new[] { -.25f, .25f }) { var ap = z.At(4f, -3.6f + k * .9f) + z.front * e; z.mk.Blob(WorldMaterials.Solid(beasts[k]), ap + up * (.2f + k * .06f), new Vector3(.14f, .14f + k * .05f, .22f), prng, .05f); z.mk.Blob(WorldMaterials.Solid(beasts[k]), ap + up * (.36f + k * .12f) + z.side * .18f, Vector3.one * .08f, prng, 0f); }
                    Color[] bow = { new Color(1f, .3f, .3f), new Color(1f, .7f, .3f), new Color(1f, .95f, .4f), new Color(.4f, .9f, .5f), new Color(.4f, .6f, 1f) }; for (var r = 0; r < 5; r++) { var rm = SongGlow(s, bow[r], 1.2f); for (var k = 0; k <= 24; k++) { var a = k / 24f * Mathf.PI; z.mk.Blob(rm, p + up * (1.2f + Mathf.Sin(a) * (3f - r * .17f)) + z.front * Mathf.Cos(a) * (3f - r * .17f) - z.side * 1f, Vector3.one * .1f, prng, 0f); } } break;
                case "Booker T. & the M.G.'s":
                    s = Song(z, "Green Onions"); z.mk.Box(woodDark, p + up * .55f, new Vector3(1.4f, 1.1f, .7f), rot); z.mk.Box(white, p + up * 1f + z.front * .3f, new Vector3(1.2f, .04f, .2f), rot); z.mk.Box(white, p + up * .86f + z.front * .42f, new Vector3(1.2f, .04f, .2f), rot); BoxCol(z.root, p + up * .55f, new Vector3(1.4f, 1.1f, .8f), rot);
                    z.mk.Box(wood, q + up * .25f, new Vector3(.8f, .5f, .6f), rot); var onion = SongGlow(s, new Color(.5f, 1f, .4f), 1.4f); for (var k = 0; k < 7; k++) { var op = q + up * .52f + z.side * (-.3f + k * .1f) + z.front * ((k % 2) * .16f - .08f); z.mk.Blob(white, op, Vector3.one * .06f, prng, 0f); z.mk.Prism(onion, op, (up + z.side * ((k % 3) - 1) * .2f).normalized, .018f, .4f, 4); }
                    halos.Add(q + up * .8f, 1f, new Color(.5f, 1f, .4f), .4f); BoxCol(z.root, q + up * .25f, new Vector3(.8f, .5f, .6f), rot); break;
                case "The Five Stairsteps":
                    s = Song(z, "O-o-h Child"); for (var k = 0; k < 5; k++) { var sp = p + z.side * k * .9f + up * ((k + 1) * .15f); z.mk.Box(SongGlow(s, Color.Lerp(new Color(.5f, .4f, .3f), new Color(1f, .92f, .6f), k / 4f), .5f + k * .45f), sp + up * ((k + 1) * .15f - .04f), new Vector3(.9f, .08f, 1.4f), rot); z.solid.Box(cream, sp, new Vector3(.9f, (k + 1) * .3f, 1.4f), rot); z.mk.Box(cream, sp - up * .05f, new Vector3(.88f, (k + 1) * .3f - .1f, 1.38f), rot); }
                    ArtistBits.Label(z.labels, "things are gonna get brighter", p + z.side * 1.8f + z.front * .75f + up * .12f, z.front, .12f, Ink, TextAnchor.MiddleCenter); break;
                case "Life of the Party":
                    s = Song(z, "Ooh Child"); z.mk.Box(white, p + up * .76f, new Vector3(2f, .06f, .9f), rot); z.mk.Box(Paint(1), p + up * .4f + z.front * .46f, new Vector3(2f, .7f, .02f), rot); BoxCol(z.root, p + up * .4f, new Vector3(2f, .8f, .9f), rot); for (var k = 0; k < 5; k++) { var bp = p + z.side * (-.8f + k * .4f) + up * (2f + (k % 2) * .4f); z.mk.Blob(SongGlow(s, Pastel[k], 1f), bp, new Vector3(.2f, .25f, .2f), prng, .05f); z.mk.Prism(dark, p + z.side * (-.8f + k * .4f) + up * .8f, up, .006f, 1.1f + (k % 2) * .4f, 3); }
                    for (var k = 0; k < 9; k++) z.mk.Prism(Paint(k), q + z.side * (-2f + k * .5f) + up * (2.5f - Mathf.Sin(k / 8f * Mathf.PI) * .4f), -up, .16f, .26f, 3); foreach (var e in new[] { -2.1f, 2.1f }) { z.mk.Prism(white, q + z.side * e - up * .2f, up, .05f, 2.8f, 5); ArtistBits.PostCollider(z.root, q + z.side * e, 2.6f, .07f); } break;
                case "The Foundations":
                    s = Song(z, "Build Me up Buttercup"); var bc = SongGlow(s, new Color(1f, .85f, .2f), 1.5f); for (var k = 0; k < 3; k++) { var fp = z.At(2.4f + k * .4f, -3.4f + k * 1.4f); var h = 2.2f + k * .5f; z.mk.Prism(WorldMaterials.Solid(new Color(.35f, .6f, .25f)), fp - up * .2f, up, .06f, h + .2f, 5); for (var pk = 0; pk < 6; pk++) { var d = Quaternion.Euler(0, pk * 60f, 0) * z.front; z.mk.Blob(bc, fp + up * h + d * .34f, new Vector3(.22f, .1f, .22f), prng, .05f); } z.mk.Blob(WorldMaterials.Solid(new Color(.8f, .55f, .1f)), fp + up * (h + .05f), Vector3.one * .16f, prng, 0f); halos.Add(fp + up * h, 1.1f, Gold, .4f); ArtistBits.PostCollider(z.root, fp, h, .1f); }
                    z.mk.Prism(wood, q, up, .3f, .7f, 8); z.mk.Box(dark, q + up * .76f, new Vector3(.26f, .1f, .2f), rot); z.mk.Box(dark, q + up * .86f, new Vector3(.34f, .06f, .08f), rot); ArtistBits.PostCollider(z.root, q, .8f, .3f); ArtistBits.Label(z.labels, "(I'll be over at ten)", q + up * 1.3f, z.front, .11f, Ink, TextAnchor.MiddleCenter); break;
                case "Wendy Rene":
                    s = Song(z, "After Laughter"); foreach (var e in new[] { -1f, 1f }) { var mp = p + z.side * e * .9f; z.mk.Prism(iron, mp - up * .2f, up, .03f, 1.9f, 5); z.mk.Blob(e < 0 ? gold : cream, mp + up * 2f, new Vector3(.34f, .42f, .1f), prng, 0f); for (var k = 0; k < 7; k++) { var a = (e < 0 ? 200f + k * 23f : 20f + k * 23f) * Mathf.Deg2Rad; z.mk.Blob(dark, mp + up * (e < 0 ? 2f : 1.72f) + up * Mathf.Sin(a) * .16f + z.side * Mathf.Cos(a) * .18f + z.front * .1f, Vector3.one * .025f, prng, 0f); } ArtistBits.PostCollider(z.root, mp, 1.8f, .07f); }
                    var tear = SongGlow(s, new Color(.4f, .7f, 1f), 1.8f); for (var k = 0; k < 3; k++) z.mk.Blob(tear, p + z.side * .9f + up * (1.5f - k * .4f) + z.front * .12f + z.side * .14f, new Vector3(.05f, .09f, .05f), prng, 0f); break;
                case "Alessia Cara":
                    s = Song(z, "Here"); z.mk.Box(Paint(5), p + up * 1.2f - z.front * .9f, new Vector3(2.6f, 2.4f, .1f), rot); z.mk.Box(Paint(5), p + up * 1.2f - z.side * 1.3f, new Vector3(.1f, 2.4f, 1.8f), rot); z.solid.Box(cream, p + up * 1.2f - z.front * .9f, new Vector3(2.6f, 2.4f, .14f), rot); z.solid.Box(cream, p + up * 1.2f - z.side * 1.3f, new Vector3(.14f, 2.4f, 1.8f), rot);
                    var ch = p - z.side * .7f - z.front * .3f; z.mk.Box(wood, ch + up * .45f, new Vector3(.5f, .06f, .5f), rot); z.mk.Box(wood, ch + up * .8f - z.front * .22f, new Vector3(.5f, .7f, .05f), rot); z.mk.Prism(woodDark, ch, up, .04f, .45f, 5); Interact.Seat(tlife.transform, ch + up * .5f, z.front, "Sit in the corner");
                    z.mk.Box(dark, p + z.side * .7f + up * .5f - z.front * .5f, new Vector3(.6f, 1f, .5f), rot); z.mk.Prism(SongGlow(s, new Color(.7f, .5f, 1f), 1.6f), p + z.side * .7f + up * .6f - z.front * .24f, z.front, .2f, .02f, 12); for (var k = 0; k < 4; k++) z.mk.Prism(WorldMaterials.Solid(new Color(.85f, .2f, .2f)), p + z.side * (-.2f + k * .22f) + z.front * (.5f + (k % 2) * .2f), up, .04f, .11f, 6, 1.3f); break;
                case "Teddy Swims":
                    s = Song(z, "Lose Control"); var heart = SongGlow(s, new Color(1f, .3f, .45f), 2f); z.mk.Prism(iron, p - up * .2f, up, .05f, 1.6f, 5); for (var k = 0; k < 20; k++) { var t = k / 20f * Mathf.PI * 2f; var hx = 16f * Mathf.Pow(Mathf.Sin(t), 3f); var hy = 13f * Mathf.Cos(t) - 5f * Mathf.Cos(2f * t) - 2f * Mathf.Cos(3f * t) - Mathf.Cos(4f * t); z.mk.Blob(heart, p + up * (2.3f + hy * .05f) + z.side * hx * .05f, Vector3.one * .08f, prng, 0f); }
                    halos.Add(p + up * 2.3f + z.front * .3f, 1.8f, new Color(1f, .4f, .5f), .5f); ArtistBits.PostCollider(z.root, p, 1.6f, .08f); life.flicker = heart; break;
                case "Nathaniel Rateliff & The Night Sweats":
                    s = Song(z, "S.O.B."); z.mk.Box(woodDark, p + up * .55f, new Vector3(2.4f, 1.1f, .6f), rot); z.mk.Box(wood, p + up * 1.12f, new Vector3(2.6f, .06f, .8f), rot); BoxCol(z.root, p + up * .55f, new Vector3(2.6f, 1.1f, .8f), rot); for (var k = 0; k < 5; k++) z.mk.Prism(k == 2 ? SongGlow(s, new Color(1f, .7f, .3f), 1.6f) : WorldMaterials.Glow(new Color(.5f, .8f, .5f), .3f), p + up * 1.15f + z.side * (-.9f + k * .45f), up, .045f, .3f, 6, .5f);
                    var bs = p + z.front * .9f; z.mk.Prism(iron, bs, up, .04f, .7f, 5); z.mk.Prism(velvet, bs + up * .7f, up, .2f, .08f, 10); Interact.Seat(tlife.transform, bs + up * .8f, -z.front, "Sit at the bar").seatHeight = .8f; ArtistBits.Label(z.labels, "CLAP  ·  STOMP  ·  HOLLER", p + up * 1.9f, z.front, .14f, Ink, TextAnchor.MiddleCenter); break;
                default:
                    z.mk.Prism(wood, p, up, .34f, .8f, 8); z.mk.Blob(GlowM(Gold, 1.4f), p + up * 1f, Vector3.one * .16f, prng, 0f); ArtistBits.PostCollider(z.root, p, .8f, .34f); break;
            }
        }
    }
}
