using System.Collections.Generic;
using UnityEngine;

namespace EverythingWorld.World
{
    /// Discotopia's artists. Each has a round white pad; most have a little light-up dance floor of their own under a
    /// small mirror ball on an arch, with something from their song beside it; covers on a white board framed in gold,
    /// a plaque, a little mirror ball on a stand for every song without its own piece.
    /// - ABBA (the largest): a star-shaped white stage with four microphones; a crown turning over a dance floor
    ///   ("Dancing Queen": the whole town's floor goes gold); a cannon and a white flag ("Waterloo": confetti); a pair
    ///   of giant dice ("Take A Chance On Me"); a winners' podium with one trophy under one cold light ("The Winner
    ///   Takes It All": everything else dims).
    /// - Earth, Wind & Fire: a gold pyramid with a glowing cap, a kalimba to play; a calendar page turned to the 21st
    ///   ("September": fireworks over the sea); a wall of light bars ("Let's Groove").
    /// - The Euro-disco corner out by the lighthouse: Boney M. an onion-domed kiosk and a pair of dancing boots
    ///   ("Rasputin": the domes turn and the light goes red and gold); Dschinghis Khan a fur hat, a balalaika and the
    ///   word in lights ("Moskau"); Sandy Marton a camel in sunglasses ("Camel by Camel"); International Music System a
    ///   desert road sign and a cactus ("Mojave"); Walter Murphy a bust in a white collar and a bottle ("A Fifth of
    ///   Beethoven").
    /// - The rest: CHIC a velvet rope and FREAK OUT in lights; Hot Chocolate a giant mug; Rufus a loving cup; The
    ///   Brothers Johnson a giant strawberry and a letter; Die Bokjollers a little paddle wheel.
    public partial class DiscoKit
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
                Add("ABBA", "Dancing Queen", "@the floor"); Add("ABBA", "Waterloo", "ABBA"); Add("ABBA", "The Winner Takes It All", "ABBA");
                Add("Earth, Wind & Fire", "September", "@the esplanade"); Add("Boney M.", "Rasputin", "Boney M.");
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
            if (stone == null) MakeMaterials();
            var root = new GameObject("Artist: " + a.name).transform; root.SetParent(ctx.Root, false);
            var front = facing.TryGetValue(a.name, out var f) ? f : new Vector3(center.x - a.x, 0, center.z - a.z).normalized;
            if (front.sqrMagnitude < .01f) front = Vector3.forward;
            var labels = new GameObject("Labels").transform; labels.SetParent(root, false);
            var area = root.gameObject.AddComponent<ArtistArea>();
            area.artistName = a.name; area.stationId = a.stationId; area.radius = a.r; area.glowColor = Gold;
            var z = new Plot { ctx = ctx, root = root, labels = labels, mk = new MeshKit(), solid = new MeshKit(), pictures = new MeshKit(), a = a, c = ctx.OnGround(a.x, a.z), front = front, side = Vector3.Cross(Vector3.up, front), area = area };
            var reach = Foot(a); var up = Vector3.up;
            z.mk.Prism(stone, z.c - up * .25f, up, reach * .62f, .33f, 20);
            for (var k = 0; k < 20; k++) { var d = Quaternion.Euler(0, k * 18f, 0) * front; z.mk.Blob(tileM[k % 4], z.c + d * (reach * .62f - .25f) + up * .1f, new Vector3(.13f, .05f, .13f), prng, 0f); }
            switch (a.name)
            {
                case "ABBA": Abba(z); break;
                case "Earth, Wind & Fire": EarthWindFire(z); break;
                default: LittleFloor(z); Others(z); break;
            }
            SongLamps(z);
            Board(z, z.At(reach * .6f, -reach * .45f - 1.5f), front);
            ArtistBits.Plaque(z.mk, z.solid, labels, a, z.At(reach * .6f + 1.5f, reach * .45f + 1.5f), front, navy, goldM, Pale, 1.6f);
            GlobeLamp(ctx, z.mk, z.At(reach * .5f, reach * .62f), true);
            if (z.area.glowLight == null) z.area.glowLight = ArtistBits.PointLight(root, z.c + up * 5f + front * 3f, new Color(1f, .95f, .85f), HasStation(a) ? 26f : 18f, 2.4f);
            area.glowRenderers = new Renderer[0];
            z.mk.Build("Area", root, false);
            z.solid.BuildCollider("Walls", root);
            z.pictures.Build("Covers", root, false);
            noGrass.Add(new Vector3(z.c.x, reach * .9f, z.c.z));
            if (HasStation(a) || reach >= 10f)
            {
                var mark = new GameObject("Shot: " + a.name + " up close").transform; mark.SetParent(ctx.Root, false);
                var cam = z.At(reach * .85f, reach * .3f) + up * 2f; var look = z.At(-reach * .3f, 0) + up * 2.4f - cam; mark.SetPositionAndRotation(cam, Quaternion.LookRotation(look.normalized));
            }
        }

        /// The covers on a white board in a gold frame, a row of lights along the top.
        void Board(Plot z, Vector3 ground, Vector3 dir)
        {
            dir = Vector3.ProjectOnPlane(dir, Vector3.up).normalized; var rot = Quaternion.LookRotation(dir); var side = Vector3.Cross(Vector3.up, dir);
            var n = Mathf.Max(1, Mathf.Min(7, z.a.songList?.Length ?? 1)); const float size = .9f; var w = n * size * 1.25f + .6f;
            var c = ground + Vector3.up * 1.7f;
            z.mk.Box(goldM, c - Vector3.up * .1f - dir * .02f, new Vector3(w + .2f, size + 1.1f, .08f), rot); z.mk.Box(marble, c - Vector3.up * .1f, new Vector3(w, size + .9f, .1f), rot); z.solid.Box(marble, c - Vector3.up * .5f, new Vector3(w, size + 1.6f, .3f), rot);
            foreach (var s in new[] { -1f, 1f }) z.mk.Prism(marble, c + side * s * (w / 2f + .2f) - Vector3.up * 1.9f, Vector3.up, .08f, size + 2.5f, 6);
            for (var k = 0; k < Mathf.RoundToInt(w / .45f); k++) z.mk.Blob(tileM[k % 4], c + Vector3.up * (size / 2f + .52f) + side * (-w / 2f + .25f + k * .45f) + dir * .06f, Vector3.one * .07f, prng, 0f);
            ArtistBits.PointLight(z.root, c + dir * 1.6f + Vector3.up * .8f, new Color(1f, .97f, .9f), 5f, 1.2f);
            ArtistBits.CoverWall(z.pictures, z.labels, z.ctx, z.a, c + dir * .06f + Vector3.up * .08f, dir, n, size, n, true, Ink);
        }

        /// A little mirror ball on a stand for each song without its own piece.
        void SongLamps(Plot z)
        {
            if (!SongIds.TryGetValue(z.a.name, out var list)) return;
            var up = Vector3.up; var k = 0; var seen = new HashSet<string>();
            foreach (var (_, id) in list)
            {
                if (z.lit.Contains(id) || !seen.Add(id)) continue;
                var p = z.At(Foot(z.a) * .55f - (k % 2) * 1.3f, Foot(z.a) * .2f + (k / 2) * 1.3f);
                z.mk.Prism(marble, p - up * .1f, up, .05f, 1.3f, 6); z.mk.Blob(SongGlow(id, Silver, 2f), p + up * 1.45f, Vector3.one * .24f, prng, .14f); halos.Add(p + up * 1.45f, 1.1f, Silver, .5f); ArtistBits.PostCollider(z.root, p, 1.3f, .1f);
                k++;
            }
        }

        /// A light-up floor of four tiles under a small mirror ball on a white arch, a bench beside it.
        void LittleFloor(Plot z)
        {
            var up = Vector3.up; var g = z.At(-Foot(z.a) * .3f, 0); var rot = z.Rot; var hue = z.a.name.Length;
            z.mk.Box(dark, g + up * .1f, new Vector3(4.3f, .1f, 4.3f), rot); for (var x = 0; x < 2; x++) for (var q = 0; q < 2; q++) z.mk.Box(tileM[(x + q * 2 + hue) % 4], g + z.side * (-1f + x * 2f) + z.front * (-1f + q * 2f) + up * .17f, new Vector3(1.9f, .04f, 1.9f), rot);
            var prev = g - z.side * 2.5f; for (var s = 1; s <= 10; s++) { var t = s / 10f; var p = g + z.side * Mathf.Lerp(-2.5f, 2.5f, t) + up * Mathf.Sin(t * Mathf.PI) * 3.6f; z.mk.Prism(marble, prev, (p - prev).normalized, .1f, Vector3.Distance(prev, p) + .03f, 5); prev = p; }
            foreach (var e in new[] { -1f, 1f }) ArtistBits.PostCollider(z.root, g + z.side * e * 2.5f, 1.6f, .14f);
            z.mk.Prism(chrome, g + up * 2.9f, up, .015f, .7f, 3); z.mk.Blob(lampM, g + up * 2.6f, Vector3.one * .4f, prng, .14f); halos.Add(g + up * 2.6f, 2f, Silver, .45f);
            var bn = z.At(-Foot(z.a) * .3f - 3.2f, 0); z.mk.Box(marble, bn + up * .45f, new Vector3(2.2f, .1f, .55f), rot); z.mk.Box(marble, bn + up * .8f - z.front * .25f, new Vector3(2.2f, .5f, .08f), rot); foreach (var e in new[] { -1f, 1f }) z.mk.Box(goldM, bn + up * .22f + z.side * e * .95f, new Vector3(.1f, .44f, .5f), rot);
            BoxCol(z.root, bn + up * .5f, new Vector3(2.2f, 1f, .6f), rot); Interact.Bench(tlife.transform, bn + up * .52f - z.side * .7f, bn + up * .52f + z.side * .7f, z.front);
        }

        void Say1(Vector3 at, string text) => PlaceTools.Say(tlife.transform, at, text, 4.5f);

        // ---------------------------------------------------------------- ABBA
        void Abba(Plot z)
        {
            var up = Vector3.up; var rot = z.Rot; var c = z.At(-3.5f, 0);
            // a white star-shaped stage with four microphones, lights under its edge
            z.mk.Prism(marble, c, up, 3.4f, .6f, 10); z.solid.Prism(marble, c - up * .3f, up, 4.6f, .9f, 10);
            for (var k = 0; k < 5; k++) { var d = Quaternion.Euler(0, k * 72f, 0) * z.front; z.mk.Prism(marble, c + d * 2.2f, d, 2f, 2.6f, 3, .05f, 0f); z.mk.Blob(tileM[k % 4], c + d * 4.7f + up * .32f, Vector3.one * .16f, prng, 0f); }
            z.mk.Prism(goldM, c + up * .6f, up, 2f, .03f, 5);
            for (var k = 0; k < 2; k++) { var st = c + z.front * (3.5f + (1 - k) * .4f) + up * ((k + .5f) * .3f); z.mk.Box(marble, st, new Vector3(2.2f, .3f, .4f), rot); z.solid.Box(marble, st - up * (k * .15f), new Vector3(2.2f, .3f * (k + 1), .4f), rot); }
            Color[] four = { Gold, Silver, Amber, Aqua }; for (var k = 0; k < 4; k++) { var mp = c + up * .6f + z.side * (-1.8f + k * 1.2f) + z.front * (k == 0 || k == 3 ? .4f : 1f); z.mk.Prism(chrome, mp, up, .02f, 1.5f, 5); z.mk.Prism(chrome, mp, up, .2f, .03f, 8); z.mk.Blob(GlowM(four[k], 1.6f), mp + up * 1.56f, new Vector3(.08f, .11f, .08f), prng, 0f); }
            z.area.glowLight = ArtistBits.PointLight(z.root, c + up * 4.5f + z.front * 2f, new Color(1f, .96f, .85f), 28f, 2.8f);
            // "Dancing Queen": a crown turning slowly over a round dance floor
            var dq = Song(z, "Dancing Queen"); var q = z.At(2.5f, 9f); z.mk.Prism(dark, q + up * .08f, up, 2.8f, .08f, 18); var qm = SongGlow(dq, Gold, 1.4f); z.mk.Prism(qm, q + up * .17f, up, 2.5f, .03f, 18); z.mk.Prism(dark, q + up * .175f, up, 1.7f, .03f, 16); z.mk.Prism(SongGlow(dq, Silver, 1.4f), q + up * .18f, up, 1.5f, .03f, 16);
            var cm = new MeshKit(); cm.Prism(goldM, Vector3.zero, Vector3.up, .5f, .22f, 12); for (var k = 0; k < 6; k++) { var d = Quaternion.Euler(0, k * 60f, 0) * Vector3.forward; cm.Cone(goldM, d * .46f + Vector3.up * .22f, .1f, .4f, 4); cm.Blob(SongGlow(dq, k % 2 == 0 ? Aqua : Amber, 2f), d * .46f + Vector3.up * .66f, Vector3.one * .06f, prng, 0f); }
            var cgo = cm.BuildSingle("The crown", tlife.transform, false); cgo.transform.position = q + up * 2.6f; life.crown = cgo.transform; halos.Add(q + up * 2.8f, 2.2f, Gold, .5f); life.queenSong = dq;
            ArtistBits.Label(z.labels, "17", q + up * .22f - z.front * 2.1f, up, .4f, Ink, TextAnchor.MiddleCenter);
            // "Waterloo": a white cannon and a white flag
            var wl = Song(z, "Waterloo"); var cn = z.At(2.5f, -9f); var aim = (z.side * -1f + up * .5f).normalized; foreach (var e in new[] { -.5f, .5f }) z.mk.Prism(marble, cn + up * .5f + z.front * e - z.front * Mathf.Sign(e) * .08f, z.front * Mathf.Sign(e), .5f, .16f, 12);
            z.mk.Prism(marble, cn + up * .7f - aim * .6f, aim, .28f, 2.2f, 10, .8f); z.mk.Prism(goldM, cn + up * .7f + aim * 1.5f, aim, .25f, .14f, 10); BoxCol(z.root, cn + up * .7f, new Vector3(1.4f, 1.4f, 2.4f), Quaternion.LookRotation(z.side)); life.cannonAt = cn + up * .7f + aim * 1.8f; life.cannonDir = aim; life.waterlooSong = wl;
            var fl = z.At(4.5f, -11f); z.mk.Prism(chrome, fl - up * .2f, up, .04f, 3.4f, 5); z.mk.Box(SongGlow(wl, Pale, 1f), fl + up * 2.8f + z.front * .6f, new Vector3(.04f, .8f, 1.2f), rot); ArtistBits.PostCollider(z.root, fl, 3f, .08f);
            var hat = z.At(.5f, -11f); z.mk.Prism(marble, hat - up * .1f, up, .05f, 1.5f, 5); z.mk.Box(navy, hat + up * 1.5f, new Vector3(.9f, .2f, .3f), rot); z.mk.Prism(navy, hat + up * 1.56f - z.front * .15f, z.front, .34f, .3f, 8); z.mk.Blob(goldM, hat + up * 1.72f + z.front * .16f, Vector3.one * .06f, prng, 0f); ArtistBits.PostCollider(z.root, hat, 1.5f, .1f);
            // "Take A Chance On Me": two giant dice
            var tc = Song(z, "Take A Chance On Me"); var di = z.At(7.5f, 5f); var pip = SongGlow(tc, Amber, 1.8f);
            for (var k = 0; k < 2; k++) { var dc = di + z.side * k * 1.5f + up * (.6f + k * .05f); var dr = rot * Quaternion.Euler(0, 20f + k * 35f, 0); z.mk.Box(marble, dc, Vector3.one * 1.2f, dr); BoxCol(z.root, dc, Vector3.one * 1.2f, dr); foreach (var (px, py) in k == 0 ? new[] { (-.3f, .3f), (.3f, -.3f), (0f, 0f) } : new[] { (-.3f, .3f), (.3f, .3f), (-.3f, -.3f), (.3f, -.3f), (0f, 0f) }) { z.mk.Blob(pip, dc + dr * new Vector3(px, py, .61f), new Vector3(.1f, .1f, .03f), prng, 0f); z.mk.Blob(pip, dc + dr * new Vector3(px, .61f, py), new Vector3(.1f, .03f, .1f), prng, 0f); } }
            // "The Winner Takes It All": a podium, one trophy, one cold light
            var wt = Song(z, "The Winner Takes It All"); var pd = z.At(7.5f, -4f); var cold = new Color(.75f, .88f, 1f);
            foreach (var (x, h) in new[] { (0f, 1.2f), (-1.3f, .8f), (1.3f, .5f) }) { z.mk.Box(marble, pd + z.side * x + up * (h / 2f), new Vector3(1.3f, h, 1.2f), rot); z.solid.Box(marble, pd + z.side * x + up * (h / 2f), new Vector3(1.3f, h, 1.2f), rot); }
            ArtistBits.Label(z.labels, "1", pd + up * .7f + z.front * .61f, z.front, .5f, Ink, TextAnchor.MiddleCenter); ArtistBits.Label(z.labels, "2", pd - z.side * 1.3f + up * .45f + z.front * .61f, z.front, .36f, Ink, TextAnchor.MiddleCenter); ArtistBits.Label(z.labels, "3", pd + z.side * 1.3f + up * .28f + z.front * .61f, z.front, .3f, Ink, TextAnchor.MiddleCenter);
            var tr = pd + up * 1.2f; z.mk.Prism(goldM, tr, up, .16f, .1f, 8); z.mk.Prism(goldM, tr + up * .1f, up, .04f, .24f, 6); z.mk.Prism(SongGlow(wt, Gold, 1.4f), tr + up * .34f, up, .1f, .3f, 10, 1.8f); foreach (var e in new[] { -1f, 1f }) z.mk.Prism(goldM, tr + up * .5f + z.side * e * .16f, (z.side * e + up * .4f).normalized, .02f, .18f, 4);
            life.winnerLight = ArtistBits.PointLight(z.root, tr + up * 4f + z.front * 1f, cold, 12f, .8f); life.winnerSong = wt; halos.Add(tr + up * .5f, 1.2f, Gold, .4f);
            Interact.Seat(tlife.transform, pd - z.side * 1.3f + up * .82f + z.front * .3f, z.front, "Sit on second place");
        }

        // ---------------------------------------------------------------- Earth, Wind & Fire
        void EarthWindFire(Plot z)
        {
            var up = Vector3.up; var rot = z.Rot; var c = z.At(-3f, 0);
            // a gold pyramid with a glowing cap
            var sep = Song(z, "September"); z.mk.Cone(goldM, c, 4.2f, 5f, 4, 45f + rot.eulerAngles.y); z.solid.Prism(goldM, c, up, 3f, 2.4f, 4, .5f, 45f + rot.eulerAngles.y); z.mk.Cone(SongGlow(sep, new Color(1f, .9f, .5f), 2.2f), c + up * 4.3f, .62f, .74f, 4, 45f + rot.eulerAngles.y); halos.Add(c + up * 4.8f, 3f, Gold, .6f);
            for (var k = 0; k < 4; k++) { var d = Quaternion.Euler(0, k * 90f, 0) * z.front; z.mk.Blob(tileM[k], c + d * 3.3f + up * .2f, Vector3.one * .16f, prng, 0f); }
            z.area.glowLight = ArtistBits.PointLight(z.root, c + up * 6f + z.front * 3f, new Color(1f, .9f, .6f), 26f, 2.6f);
            // a kalimba on a stand, to play
            var kp = z.At(2f, 0); z.mk.Prism(chrome, kp, up, .03f, .9f, 5); z.mk.Prism(chrome, kp, up, .22f, .03f, 8); var km = new MeshKit(); km.Box(WorldMaterials.Solid(new Color(.6f, .38f, .2f)), Vector3.zero, new Vector3(.34f, .06f, .44f), Quaternion.identity); for (var k = 0; k < 7; k++) km.Box(chrome, new Vector3(-.12f + k * .04f, .04f, .04f - Mathf.Abs(k - 3) * .025f), new Vector3(.02f, .01f, .26f - Mathf.Abs(k - 3) * .03f), Quaternion.identity);
            var kgo = km.BuildSingle("Kalimba", tlife.transform, false); kgo.transform.SetPositionAndRotation(kp + up * .95f, rot * Quaternion.Euler(-25f, 0, 0)); ArtistBits.PostCollider(z.root, kp, .9f, .14f);
            string[] plink = { "Plink, plink-a-plink. The pyramid's cap glows a little brighter with every note.", "You thumb out a little run. A horn section you can't see answers it.", "Ba-dee-ya. You don't know what it means either. It doesn't matter." }; var pn = 0;
            Interact.Toy(kgo, Toy.Kind.Custom, "Play the kalimba", new Vector3(.9f, .8f, .9f)).onUse = _ => { life.request = Time.time; Say1(kp + up * 2.2f, plink[pn++ % plink.Length]); };
            // "September": a calendar page turned to the 21st
            var cal = z.At(2f, 7f); foreach (var e in new[] { -.8f, .8f }) { z.mk.Prism(marble, cal + z.side * e - up * .2f, up, .05f, 2.2f, 5); ArtistBits.PostCollider(z.root, cal + z.side * e, 2f, .07f); }
            z.mk.Box(marble, cal + up * 2.3f, new Vector3(1.9f, 2f, .08f), rot); z.mk.Box(SongGlow(sep, new Color(1f, .45f, .25f), 1.6f), cal + up * 3.1f + z.front * .05f, new Vector3(1.9f, .44f, .04f), rot); ArtistBits.Label(z.labels, "SEPTEMBER", cal + up * 3.1f + z.front * .08f, z.front, .17f, Pale, TextAnchor.MiddleCenter); ArtistBits.Label(z.labels, "21", cal + up * 2.1f + z.front * .06f, z.front, .9f, Ink, TextAnchor.MiddleCenter); life.septemberSong = sep;
            // "Let's Groove": a wall of light bars
            var lg = Song(z, "Let's Groove"); var eq = z.At(2f, -7f); z.mk.Box(dark, eq + up * 1.5f - z.front * .1f, new Vector3(3.6f, 3f, .12f), rot); z.solid.Box(dark, eq + up * 1.5f - z.front * .1f, new Vector3(3.6f, 3f, .2f), rot); Color[] bars = { Aqua, Gold, Amber, new Color(1f, .4f, .6f), Aqua, Gold, Amber }; float[] hs = { 1.2f, 2f, 2.6f, 1.6f, 2.3f, 1.4f, .9f };
            for (var k = 0; k < 7; k++) z.mk.Box(SongGlow(lg, bars[k], 1.6f), eq + z.side * (-1.44f + k * .48f) + up * (.2f + hs[k] / 2f), new Vector3(.36f, hs[k], .06f), rot); halos.Add(eq + up * 1.5f + z.front * .6f, 2.4f, Aqua, .35f);
            var bn = z.At(5.5f, 3.4f); z.mk.Box(marble, bn + up * .45f, new Vector3(2.2f, .1f, .55f), rot); foreach (var e in new[] { -1f, 1f }) z.mk.Box(goldM, bn + up * .22f + z.side * e * .95f, new Vector3(.1f, .44f, .5f), rot); BoxCol(z.root, bn + up * .25f, new Vector3(2.2f, .5f, .55f), rot); Interact.Bench(tlife.transform, bn + up * .52f - z.side * .7f, bn + up * .52f + z.side * .7f, -z.front);
        }

        // ---------------------------------------------------------------- everyone else
        void Others(Plot z)
        {
            var up = Vector3.up; var rot = z.Rot; var p = z.At(2.8f, 2.6f); var q = z.At(2.8f, -2.6f); string s;
            switch (z.a.name)
            {
                case "Boney M.":
                    s = Song(z, "Rasputin"); z.mk.Prism(marble, p, up, 1.1f, 2.2f, 8); z.solid.Prism(marble, p, up, 1.1f, 2.2f, 8); z.mk.Box(dark, p + up * 1f + z.front * 1.02f, new Vector3(.7f, 1.6f, .06f), rot);
                    var dm = new MeshKit(); Color[] stripe = { new Color(1f, .3f, .3f), Gold, new Color(.3f, .7f, 1f), new Color(.4f, .9f, .5f) }; for (var k = 0; k < 8; k++) { var m = SongGlow(s, stripe[k % 4], .9f); var d = Quaternion.Euler(0, k * 45f, 0) * Vector3.forward; dm.Blob(m, d * .5f + Vector3.up * .7f, new Vector3(.36f, .8f, .36f), prng, .03f); } dm.Cone(goldM, new Vector3(0, 1.3f, 0), .3f, .8f, 8); dm.Blob(goldM, new Vector3(0, 2.2f, 0), Vector3.one * .1f, prng, 0f);
                    var dgo = dm.BuildSingle("Onion dome", tlife.transform, false); dgo.transform.position = p + up * 2.2f; life.dome = dgo.transform; life.rasputinSong = s; halos.Add(p + up * 3f, 2f, Gold, .4f);
                    foreach (var e in new[] { -.2f, .2f }) { z.mk.Box(dark, q + z.side * e + up * .3f, new Vector3(.2f, .6f, .22f), rot * Quaternion.Euler(0, e * 60f, e * -50f)); z.mk.Box(dark, q + z.side * e + up * .06f + z.front * .12f, new Vector3(.2f, .12f, .42f), rot * Quaternion.Euler(0, e * 60f, 0)); } ArtistBits.Label(z.labels, "hey! hey! hey!", q + up * 1.2f, z.front, .16f, Ink, TextAnchor.MiddleCenter); break;
                case "Dschinghis Khan":
                    s = Song(z, "Moskau"); var mg = SongGlow(s, new Color(1f, .35f, .25f), 2f); foreach (var e in new[] { -1.6f, 1.6f }) { z.mk.Prism(marble, p + z.side * e - up * .2f, up, .06f, 2.6f, 5); ArtistBits.PostCollider(z.root, p + z.side * e, 2.4f, .08f); } z.mk.Box(navy, p + up * 2.6f, new Vector3(3.6f, 1f, .12f), rot); z.mk.Box(mg, p + up * 3.06f + z.front * .07f, new Vector3(3.5f, .06f, .04f), rot); z.mk.Box(mg, p + up * 2.14f + z.front * .07f, new Vector3(3.5f, .06f, .04f), rot); ArtistBits.Label(z.labels, "MOSKAU", p + up * 2.6f + z.front * .08f, z.front, .44f, new Color(1f, .7f, .55f), TextAnchor.MiddleCenter);
                    z.mk.Prism(marble, q - up * .1f, up, .05f, 1.5f, 5); z.mk.Prism(WorldMaterials.Solid(new Color(.3f, .2f, .14f)), q + up * 1.45f, up, .3f, .3f, 10); z.mk.Blob(GlowM(new Color(1f, .3f, .2f), 1.2f), q + up * 1.6f + z.front * .3f, Vector3.one * .06f, prng, 0f); ArtistBits.PostCollider(z.root, q, 1.5f, .1f);
                    var bl = z.At(4.2f, -4.2f); z.mk.Prism(WorldMaterials.Solid(new Color(.75f, .5f, .25f)), bl + up * .55f - z.front * .04f, z.front, .34f, .08f, 3, 1f, 30f); z.mk.Box(WorldMaterials.Solid(new Color(.4f, .26f, .16f)), bl + up * 1.5f, new Vector3(.08f, 1.2f, .05f), rot); z.mk.Prism(chrome, bl, up, .02f, .3f, 4); break;
                case "Sandy Marton":
                    s = Song(z, "Camel By Camel"); var sand = WorldMaterials.Solid(new Color(.85f, .7f, .45f)); z.mk.Blob(sand, p + up * 1.5f, new Vector3(1.1f, .6f, .5f), prng, .06f); foreach (var e in new[] { -.4f, .4f }) z.mk.Blob(sand, p + up * 2.1f + z.side * e, new Vector3(.34f, .4f, .34f), prng, .06f); foreach (var (x, zz) in new[] { (-.8f, -.25f), (-.8f, .25f), (.8f, -.25f), (.8f, .25f) }) z.mk.Prism(sand, p + z.side * x + z.front * zz, up, .1f, 1.2f, 5);
                    z.mk.Prism(sand, p + up * 1.6f + z.side * 1f, (z.side * .4f + up).normalized, .16f, 1.1f, 6); z.mk.Blob(sand, p + up * 2.75f + z.side * 1.6f, new Vector3(.34f, .22f, .22f), prng, .05f); z.mk.Box(dark, p + up * 2.82f + z.side * 1.72f, new Vector3(.2f, .09f, .46f), rot); z.mk.Blob(SongGlow(s, Amber, 1.6f), p + up * 2.82f + z.side * 1.83f, new Vector3(.02f, .05f, .2f), prng, 0f); BoxCol(z.root, p + up * 1.2f, new Vector3(2.6f, 2.4f, 1f), rot); break;
                case "International Music System":
                    s = Song(z, "Mojave"); z.mk.Prism(chrome, p - up * .2f, up, .05f, 2.6f, 5); z.mk.Box(SongGlow(s, new Color(.3f, .8f, .5f), 1.2f), p + up * 2.3f, new Vector3(2.2f, .7f, .06f), rot); ArtistBits.Label(z.labels, "MOJAVE  →", p + up * 2.3f + z.front * .05f, z.front, .26f, Pale, TextAnchor.MiddleCenter); ArtistBits.PostCollider(z.root, p, 2.4f, .08f);
                    var cg = WorldMaterials.Glow(new Color(.4f, .8f, .55f), .3f); z.mk.Prism(cg, q - up * .1f, up, .3f, 2.4f, 8); foreach (var e in new[] { -1f, 1f }) { z.mk.Prism(cg, q + up * (1f + (e > 0 ? .4f : 0f)), z.side * e, .18f, .8f, 6); z.mk.Prism(cg, q + up * (1f + (e > 0 ? .4f : 0f)) + z.side * e * .7f, up, .18f, .8f, 6); } ArtistBits.PostCollider(z.root, q, 2.4f, .34f); break;
                case "Walter Murphy":
                    s = Song(z, "A Fifth Of Beethoven"); z.mk.Box(marble, p + up * .7f, new Vector3(.9f, 1.4f, .9f), rot); BoxCol(z.root, p + up * .7f, new Vector3(.9f, 1.4f, .9f), rot); z.mk.Blob(marble, p + up * 1.75f, new Vector3(.42f, .36f, .36f), prng, .04f); z.mk.Blob(marble, p + up * 2.3f, new Vector3(.3f, .34f, .3f), prng, .04f); z.mk.Blob(marble, p + up * 2.55f, new Vector3(.4f, .22f, .36f), prng, .22f);
                    foreach (var e in new[] { -1f, 1f }) z.mk.Box(SongGlow(s, Pale, 1.3f), p + up * 1.95f + z.side * e * .3f + z.front * .3f, new Vector3(.5f, .06f, .3f), rot * Quaternion.Euler(0, e * 30f, e * -30f)); ArtistBits.Label(z.labels, "5", p + up * .8f + z.front * .46f, z.front, .6f, Ink, TextAnchor.MiddleCenter);
                    z.mk.Prism(WorldMaterials.Glow(new Color(.5f, .8f, .5f), .4f), q, up, .16f, .7f, 8); z.mk.Prism(WorldMaterials.Glow(new Color(.5f, .8f, .5f), .4f), q + up * .7f, up, .06f, .3f, 6); z.mk.Box(marble, q + up * .35f + z.front * .16f, new Vector3(.2f, .26f, .02f), rot); ArtistBits.PostCollider(z.root, q, 1f, .18f); break;
                case "CHIC":
                    s = Song(z, "Le Freak"); var fg = SongGlow(s, new Color(1f, .4f, .7f), 2f); foreach (var e in new[] { -1.5f, 1.5f }) { z.mk.Prism(marble, p + z.side * e - up * .2f, up, .06f, 2.8f, 5); ArtistBits.PostCollider(z.root, p + z.side * e, 2.6f, .08f); } z.mk.Box(navy, p + up * 2.8f, new Vector3(3.4f, 1.1f, .12f), rot); z.mk.Box(fg, p + up * 3.3f + z.front * .07f, new Vector3(3.3f, .06f, .04f), rot); z.mk.Box(fg, p + up * 2.3f + z.front * .07f, new Vector3(3.3f, .06f, .04f), rot); ArtistBits.Label(z.labels, "FREAK OUT!", p + up * 2.8f + z.front * .08f, z.front, .4f, new Color(1f, .75f, .88f), TextAnchor.MiddleCenter);
                    foreach (var e in new[] { -1f, 1f }) { z.mk.Prism(goldM, q + z.side * e, up, .05f, 1f, 6); z.mk.Blob(goldM, q + z.side * e + up * 1.04f, Vector3.one * .08f, prng, 0f); ArtistBits.PostCollider(z.root, q + z.side * e, 1f, .08f); } for (var k = 0; k < 8; k++) { var t = k / 7f; z.mk.Blob(WorldMaterials.Solid(new Color(.6f, .1f, .2f)), q + z.side * Mathf.Lerp(-1f, 1f, t) + up * (.95f - Mathf.Sin(t * Mathf.PI) * .25f), Vector3.one * .06f, prng, 0f); } break;
                case "Hot Chocolate":
                    s = Song(z, "You Sexy Thing"); z.mk.Prism(marble, p, up, .8f, 1.3f, 14); z.solid.Prism(marble, p, up, .8f, 1.3f, 10); z.mk.Prism(WorldMaterials.Solid(new Color(.4f, .22f, .14f)), p + up * 1.26f, up, .72f, .06f, 14); for (var k = 0; k < 5; k++) { var a = (-70f + k * 35f) * Mathf.Deg2Rad; z.mk.Blob(marble, p + z.side * (.8f + Mathf.Cos(a) * .3f) + up * (.65f + Mathf.Sin(a) * .4f), Vector3.one * .1f, prng, 0f); }
                    var heart = SongGlow(s, new Color(1f, .35f, .5f), 1.8f); for (var k = 0; k < 16; k++) { var t = k / 16f * Mathf.PI * 2f; var hx = 16f * Mathf.Pow(Mathf.Sin(t), 3f); var hy = 13f * Mathf.Cos(t) - 5f * Mathf.Cos(2f * t) - 2f * Mathf.Cos(3f * t) - Mathf.Cos(4f * t); z.mk.Blob(heart, p + up * (2.5f + hy * .035f) + z.side * hx * .035f, Vector3.one * .06f, prng, 0f); } halos.Add(p + up * 2.5f, 1.4f, new Color(1f, .5f, .6f), .45f); ArtistBits.Label(z.labels, "I believe in miracles", p + up * 3.4f, z.front, .13f, Ink, TextAnchor.MiddleCenter); break;
                case "Rufus":
                    s = Song(z, "Ain't Nobody"); z.mk.Box(marble, p + up * .5f, new Vector3(.9f, 1f, .9f), rot); BoxCol(z.root, p + up * .5f, new Vector3(.9f, 1f, .9f), rot); var cup = SongGlow(s, Gold, 1.3f); z.mk.Prism(goldM, p + up * 1f, up, .2f, .1f, 8); z.mk.Prism(goldM, p + up * 1.1f, up, .05f, .3f, 6); z.mk.Prism(cup, p + up * 1.4f, up, .14f, .5f, 12, 2.2f); foreach (var e in new[] { -1f, 1f }) for (var k = 0; k < 5; k++) { var a = (-60f + k * 30f) * Mathf.Deg2Rad; z.mk.Blob(goldM, p + up * (1.65f + Mathf.Sin(a) * .2f) + z.side * e * (.32f + Mathf.Cos(a) * .14f), Vector3.one * .04f, prng, 0f); }
                    halos.Add(p + up * 1.7f, 1.3f, Gold, .5f); ArtistBits.Label(z.labels, "ain't nobody\nloves me better", p + up * .55f + z.front * .46f, z.front, .1f, Ink, TextAnchor.MiddleCenter); break;
                case "The Brothers Johnson":
                    s = Song(z, "Strawberry Letter 23"); var berry = SongGlow(s, new Color(1f, .22f, .3f), 1f); z.mk.Blob(berry, p + up * 1.1f, new Vector3(.9f, 1.1f, .9f), prng, .06f); for (var k = 0; k < 5; k++) z.mk.Box(WorldMaterials.Glow(new Color(.4f, .85f, .4f), .4f), p + up * 2.15f + Quaternion.Euler(0, k * 72f, 0) * z.front * .3f, new Vector3(.2f, .05f, .6f), Quaternion.Euler(-20f, k * 72f + rot.eulerAngles.y, 0)); for (var k = 0; k < 12; k++) { var d = Quaternion.Euler(k * 23f - 60f, k * 61f, 0) * Vector3.forward; z.mk.Blob(GlowM(Gold, .8f), p + up * 1.1f + Vector3.Scale(d, new Vector3(.88f, 1.05f, .88f)), Vector3.one * .04f, prng, 0f); }
                    BoxCol(z.root, p + up * 1.1f, new Vector3(1.6f, 2.2f, 1.6f), rot); halos.Add(p + up * 1.2f + z.front * .9f, 1.6f, new Color(1f, .35f, .4f), .4f); z.mk.Prism(marble, q - up * .1f, up, .04f, 1.2f, 5); z.mk.Box(marble, q + up * 1.4f, new Vector3(.9f, .6f, .04f), rot * Quaternion.Euler(0, 0, -8f)); ArtistBits.Label(z.labels, "23", q + up * 1.4f + z.front * .04f, z.front, .3f, new Color(.8f, .15f, .25f), TextAnchor.MiddleCenter); ArtistBits.PostCollider(z.root, q, 1.2f, .08f); break;
                case "Die Bokjollers":
                    s = Song(z, "CCR Medley"); var red = SongGlow(s, new Color(1f, .35f, .25f), .9f); var wm = new MeshKit(); wm.Prism(chrome, new Vector3(-.6f, 0, 0), Vector3.right, .08f, 1.2f, 6); for (var k = 0; k < 8; k++) { var qq = Quaternion.Euler(k * 45f, 0, 0); wm.Box(red, qq * new Vector3(0, .8f, 0), new Vector3(1f, .4f, .05f), qq); wm.Box(red, qq * new Vector3(0, .4f, 0), new Vector3(.06f, .8f, .06f), qq); }
                    var wgo = wm.BuildSingle("Little paddle wheel", tlife.transform, false); wgo.transform.SetPositionAndRotation(p + up * 1.3f, rot); foreach (var e in new[] { -.7f, .7f }) { z.mk.Prism(marble, p + z.side * e - up * .1f, up, .06f, 1.4f, 5); ArtistBits.PostCollider(z.root, p + z.side * e, 1.4f, .08f); } var t2 = Interact.Toy(wgo, Toy.Kind.Spin, "Spin the wheel", new Vector3(1.6f, 2.4f, 2.2f)); t2.axis = Vector3.right; t2.seconds = 6f; t2.amount = 1080f; ArtistBits.Label(z.labels, "rollin' on the river", p + up * 2.6f, z.front, .13f, Ink, TextAnchor.MiddleCenter); break;
                default:
                    z.mk.Blob(lampM, p + up * .6f, Vector3.one * .4f, prng, .14f); break;
            }
        }
    }
}
