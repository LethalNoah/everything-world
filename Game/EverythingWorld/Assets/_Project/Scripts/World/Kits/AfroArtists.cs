using System.Collections.Generic;
using UnityEngine;
using static EverythingWorld.World.PlaceTools;

namespace EverythingWorld.World
{
    /// The Afrobeat Coast's artists. Most have a market stall of their own, bigger than the rest, with their name
    /// painted on the board over it, a bench, and one thing from their song beside it; covers on a cream card in a
    /// painted frame, a plaque, a coloured bulb on a pole.
    /// - Fela Kuti: the Shrine, the open-air club by the roundabout. A tin roof on tall poles over a painted stage:
    ///   three horns on stands, an organ to play, congas to play, a shekere to carry; a floor of people dancing;
    ///   a round fountain out front ("Water No Get Enemy": it leaps, and the evening turns sea-blue).
    /// - Burna Boy: a wall of painted speakers with YE in lights on top, a giant's footprints across the yard, a
    ///   heart broken in two over a plastic table with breakfast on it ("Last Last": the bonfires roar).
    /// - Wizkid: a rooftop lounge on a deck facing the sun: a sofa, a tall glowing bottle of ESSENCE, a star on a
    ///   pole ("Essence": the sky goes rose). William Onyeabor: a wall of synthesisers to play, a white hat, a
    ///   mirror that says FANTASTIC ("Fantastic Man": every bulb runs the rainbow).
    /// - CKay's string of hearts and little keyboard; Ayra Starr's star and motorbike ("Rush": the bus tears about);
    ///   Davido's banana plant ("Fall": bananas rain); Diamond Platnumz' arch of greetings and a diamond; I Hate My
    ///   Village's plank skate ramp; Letta Mbulu's radio under a baobab; Manu Dibango's great golden saxophone;
    ///   Nomo's stall, built upside down.
    public partial class AfroKit
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
                Add("Fela Kuti", "Water No Get Enemy", "Fela Kuti"); Add("Burna Boy", "Last Last", "@by the bonfires"); Add("Wizkid", "Essence", "@the beach at sunset");
                Add("William Onyeabor", "Fantastic Man", "@down market road to the sun"); Add("Davido", "Fall", "Davido");
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
            if (earth == null) MakeMaterials();
            var root = new GameObject("Artist: " + a.name).transform; root.SetParent(ctx.Root, false);
            var front = facing.TryGetValue(a.name, out var f) ? f : new Vector3(center.x - a.x, 0, center.z - a.z).normalized;
            if (front.sqrMagnitude < .01f) front = Vector3.forward;
            var labels = new GameObject("Labels").transform; labels.SetParent(root, false);
            var area = root.gameObject.AddComponent<ArtistArea>();
            area.artistName = a.name; area.stationId = a.stationId; area.radius = a.r; area.glowColor = Orange;
            var z = new Plot { ctx = ctx, root = root, labels = labels, mk = new MeshKit(), solid = new MeshKit(), pictures = new MeshKit(), a = a, c = ctx.OnGround(a.x, a.z), front = front, side = Vector3.Cross(Vector3.up, front), area = area };
            var reach = Foot(a); var up = Vector3.up;
            z.mk.Prism(earth, z.c - up * .25f, up, reach * .66f, .33f, 20);
            switch (a.name)
            {
                case "Fela Kuti": FelaKuti(z); break;
                case "Burna Boy": BurnaBoy(z); break;
                case "Wizkid": Wizkid(z); break;
                case "William Onyeabor": WilliamOnyeabor(z); break;
                case "Nomo": Nomo(z); break;
                default: OwnStall(z); Others(z); break;
            }
            SongLamps(z);
            Board(z, z.At(reach * .6f, -reach * .45f - 1.5f), front);
            ArtistBits.Plaque(z.mk, z.solid, labels, a, z.At(reach * .6f + 1.5f, reach * .45f + 1.5f), front, woodM, Paint(Paints[0]), Cream, 1.6f);
            BulbPole(ctx, z.mk, z.At(reach * .5f, reach * .64f), a.name.Length, true);
            if (z.area.glowLight == null) z.area.glowLight = ArtistBits.PointLight(root, z.c + up * 5f + front * 3f, new Color(1f, .7f, .4f), HasStation(a) ? 26f : 18f, 2.6f);
            area.glowRenderers = new Renderer[0];
            z.mk.Build("Area", root, false);
            z.solid.BuildCollider("Walls", root);
            z.pictures.Build("Covers", root, false);
            noGrass.Add(new Vector3(z.c.x, reach * .9f, z.c.z));
            if (HasStation(a) || reach >= 11f)
            {
                var mark = new GameObject("Shot: " + a.name + " up close").transform; mark.SetParent(ctx.Root, false);
                var cam = z.At(reach * .85f, reach * .3f) + up * 2f; var look = z.At(-reach * .3f, 0) + up * 2.2f - cam; mark.SetPositionAndRotation(cam, Quaternion.LookRotation(look.normalized));
            }
        }

        /// The covers on a cream card in a painted frame, a few bulbs along the top.
        void Board(Plot z, Vector3 ground, Vector3 dir)
        {
            dir = Vector3.ProjectOnPlane(dir, Vector3.up).normalized; var rot = Quaternion.LookRotation(dir); var hue = z.a.name.Length;
            var n = Mathf.Max(1, Mathf.Min(7, z.a.songList?.Length ?? 1)); const float size = .9f; var w = n * size * 1.25f + .6f;
            var c = ground + Vector3.up * 1.7f; var frame = Paint(Paints[hue % Paints.Length]);
            z.mk.Box(frame, c - Vector3.up * .5f - dir * .06f, new Vector3(w + .5f, size + 2f, .2f), rot); z.solid.Box(frame, c - Vector3.up * .5f, new Vector3(w + .5f, size + 2f, .3f), rot); z.mk.Box(Paint(Cream), c - Vector3.up * .1f + dir * .05f, new Vector3(w, size + .9f, .04f), rot);
            for (var k = 0; k < 4; k++) z.mk.Blob(bulbM[(hue + k) % 3], c + Vector3.up * (size / 2f + .62f) + dir * .1f + rot * Vector3.right * (-w / 2f + .2f + k * (w - .4f) / 3f), Vector3.one * .07f, new System.Random(k), 0f);
            ArtistBits.PointLight(z.root, c + dir * 1.6f + Vector3.up * .8f, new Color(1f, .9f, .75f), 5f, 1.4f);
            ArtistBits.CoverWall(z.pictures, z.labels, z.ctx, z.a, c + dir * .08f + Vector3.up * .08f, dir, n, size, n, true, Ink);
        }

        /// A paper lantern on a stick for each song without its own piece.
        void SongLamps(Plot z)
        {
            if (!SongIds.TryGetValue(z.a.name, out var list)) return;
            var up = Vector3.up; var k = 0; var seen = new HashSet<string>();
            foreach (var (_, id) in list)
            {
                if (z.lit.Contains(id) || !seen.Add(id)) continue;
                var p = z.At(Foot(z.a) * .55f - (k % 2) * 1.3f, Foot(z.a) * .2f + (k / 2) * 1.3f);
                z.mk.Prism(woodM, p - up * .1f, up, .04f, 1.3f, 5); z.mk.Blob(SongGlow(id, Gold, 2f), p + up * 1.44f, new Vector3(.16f, .2f, .16f), new System.Random(k), 0f); halos.Add(p + up * 1.44f, 1f, Gold, .5f); ArtistBits.PostCollider(z.root, p, 1.3f, .08f);
                k++;
            }
        }

        /// Their own stall: wider than the market's, a painted board with their name over it, a bench out front.
        PlaceFrame OwnStall(Plot z)
        {
            var up = Vector3.up; var hue = z.a.name.Length + z.a.name[0]; var f = new PlaceFrame(z.At(-Foot(z.a) * .4f, 0), z.front); var c1 = Paint(Cloth[hue % 6]); var c2 = Paint(Cloth[(hue + 2) % 6]); var rng = new System.Random(hue);
            foreach (var (x, zz) in new[] { (-2.2f, -1.1f), (2.2f, -1.1f), (2.2f, 1.2f), (-2.2f, 1.2f) }) { z.mk.Prism(woodM, f.P(x, -.3f, zz), up, .06f, zz > 0 ? 2.9f : 3.3f, 4); if (zz > 0) ArtistBits.PostCollider(z.root, f.P(x, 0, zz), 2.6f, .08f); }
            for (var s = 0; s < 8; s++) z.mk.Box(s % 2 == 0 ? c1 : c2, f.P(-2.1f + s * .6f, 2.74f + (s % 2) * .012f, .1f), new Vector3(.58f, .04f, 3f), f.r * Quaternion.Euler(9f, 0, 0));
            z.mk.Box(woodM, f.P(0, .8f, .1f), new Vector3(4f, .08f, 1.1f), f.r); z.mk.Box(c2, f.P(0, .45f, .64f), new Vector3(4f, .72f, .03f), f.r); BoxCol(z.root, f.P(0, .45f, .1f), new Vector3(4f, .9f, 1.1f), f.r); z.mk.Box(c1, f.P(0, 1.8f, -1.06f), new Vector3(4.2f, 2.6f, .04f), f.r);
            z.mk.Box(Paint(Cream), f.P(0, 3.1f, 1.3f), new Vector3(4.2f, .6f, .06f), f.r); ArtistBits.Label(z.labels, z.a.name.ToUpperInvariant(), f.P(0, 3.1f, 1.34f), z.front, z.a.name.Length > 14 ? .2f : .28f, Ink, TextAnchor.MiddleCenter);
            for (var k = 0; k < 6; k++) z.mk.Blob(bulbM[(hue + k) % 3], f.P(-2f + k * .8f, 2.76f, 1.42f), Vector3.one * .085f, rng, 0f); halos.Add(f.P(0, 2.6f, 1.6f), 2f, Gold, .35f);
            Bench(z.mk, f.P(-3.6f, 0, 1.6f), (z.front + z.side * .4f).normalized, hue);
            return new PlaceFrame(f.P(0, .84f, .1f), z.front);
        }

        /// A horn on a stand: a tube, a curve and a flared bell. 'o' is the foot.
        void Horn(MeshKit mk, Vector3 o, Quaternion rot, Material m, float s = 1f)
        {
            var up = Vector3.up; var f = rot * Vector3.forward; mk.Prism(iron, o, up, .14f * s, .03f, 6); mk.Prism(iron, o, up, .015f * s, .5f * s, 4);
            var lean = (up - f * .22f).normalized; mk.Prism(m, o + up * .3f * s, lean, .045f * s, .75f * s, 6, .6f); mk.Prism(m, o + up * .18f * s + f * .1f * s, (up * .5f - f).normalized, .05f * s, .22f * s, 6); mk.Prism(m, o + up * .22f * s + f * .2f * s, (up + f * .3f).normalized, .05f * s, .32f * s, 8, 2.4f);
        }

        // ---------------------------------------------------------------- Fela Kuti: the Shrine
        void FelaKuti(Plot z)
        {
            var up = Vector3.up; var rot = z.Rot; var rng = new System.Random(77); var s = Song(z, "Water No Get Enemy"); life.waterSong = s; var fo = new PlaceFrame(z.At(-4f, 0) + up * .14f, z.front);
            const float W = 16f, D = 12f, H = 5.4f;
            // a concrete floor, a stage along the back, a painted wall behind it, a tin roof on tall poles
            z.mk.Box(concrete, fo.P(0, -.05f, 0), new Vector3(W, .2f, D), fo.r); z.solid.Box(concrete, fo.P(0, -.4f, 0), new Vector3(W, .9f, D), fo.r);
            var sz = -D / 2f + 2f; z.mk.Box(Paint(Paints[5]), fo.P(0, .4f, sz), new Vector3(11f, .68f, 3.6f), fo.r); z.solid.Box(concrete, fo.P(0, .2f, sz), new Vector3(11f, 1f, 3.6f), fo.r); foreach (var e in new[] { -1f, 1f }) for (var k = 0; k < 2; k++) { z.mk.Box(concrete, fo.P(e * 4.4f, .12f + k * .23f, sz + 2.1f - k * .3f + .2f), new Vector3(1.6f, .23f, .3f), fo.r); z.solid.Box(concrete, fo.P(e * 4.4f, .12f + k * .23f, sz + 2.1f - k * .3f + .2f), new Vector3(1.6f, .23f, .3f), fo.r); }
            z.mk.Box(Paint(new Color(.12f, .1f, .14f)), fo.P(0, 2.6f, -D / 2f + .1f), new Vector3(11.4f, 4.6f, .2f), fo.r); z.solid.Box(concrete, fo.P(0, 2.6f, -D / 2f + .1f), new Vector3(11.4f, 4.6f, .2f), fo.r);
            for (var k = 0; k < 11; k++) { z.mk.Box(Paint(Cloth[k % 3 == 0 ? 2 : k % 3 == 1 ? 1 : 0]), fo.P(-5f + k * 1f, 1.3f + (k % 2) * .5f, -D / 2f + .22f), new Vector3(.8f, 1.6f + (k % 3) * .5f, .03f), fo.r * Quaternion.Euler(0, 0, (k % 2 == 0 ? 8f : -8f))); }
            ArtistBits.Label(z.labels, "THE SHRINE", fo.P(0, 4.2f, -D / 2f + .24f), z.front, .7f, Gold, TextAnchor.MiddleCenter); ArtistBits.Label(z.labels, "music is the weapon of the future", fo.P(0, 3.6f, -D / 2f + .24f), z.front, .18f, Cream, TextAnchor.MiddleCenter);
            var tops = new List<Vector3>(); foreach (var (x, zz) in new[] { (-1f, -1f), (1f, -1f), (1f, .2f), (-1f, .2f) }) { var p = fo.P(x * (W / 2f - .5f), 0, zz * (D / 2f - .5f)); z.mk.Prism(Paint(Paints[1]), p - up * .2f, up, .14f, H + .2f, 6); ArtistBits.PostCollider(z.root, p, H, .16f); tops.Add(p + up * H); }
            z.mk.Box(rustM, fo.P(0, H + .5f, -D * .2f), new Vector3(W + 1f, .1f, D * .7f), fo.r * Quaternion.Euler(6f, 0, 0)); z.mk.Box(tin, fo.P(0, H + .3f, -D * .2f + D * .36f), new Vector3(W + 1f, .1f, .5f), fo.r);
            // strings of bulbs from the roof out to poles at the front corners
            foreach (var e in new[] { -1f, 1f }) { var p = fo.P(e * (W / 2f - .5f), 0, D / 2f - .5f); z.mk.Prism(woodM, p - up * .2f, up, .08f, 4.4f, 5); ArtistBits.PostCollider(z.root, p, 4.2f, .1f); BulbString(z.mk, tops[e < 0 ? 3 : 2], p + up * 4.2f, e < 0 ? 0 : 1); BulbString(z.mk, p + up * 4.2f, fo.P(0, H, .2f * (D / 2f - .5f)), 2); }
            BulbString(z.mk, tops[3], tops[2], 1);
            // the horn section: three horns on stands, each with a microphone
            var brass = WorldMaterials.Glow(new Color(.95f, .72f, .25f), .35f); for (var k = 0; k < 3; k++) { var hp = fo.P(-3.4f + k * 1.5f, .7f, sz + .6f); Horn(z.mk, hp, fo.r, brass, 1.1f + k * .12f); z.mk.Prism(iron, hp + fo.Right * .5f, up, .015f, 1.4f, 4); z.mk.Blob(tin, hp + fo.Right * .5f + up * 1.44f, new Vector3(.05f, .08f, .05f), rng, 0f); }
            // the organ, to play; the congas, to play
            var og = fo.P(2.6f, .7f, sz + .2f); var orot = fo.r * Quaternion.Euler(0, -25f, 0); z.mk.Box(Paint(Cloth[0]), og + up * .5f, new Vector3(1.5f, 1f, .6f), orot); z.mk.Box(white, og + up * 1.02f + orot * Vector3.forward * .2f, new Vector3(1.4f, .04f, .22f), orot); z.mk.Box(white, og + up * 1.12f, new Vector3(1.4f, .04f, .22f), orot); BoxCol(z.root, og + up * .5f, new Vector3(1.5f, 1.1f, .6f), orot);
            string[] riff = { "Two chords, back and forth, and already it's been ten minutes.", "You hold the riff. The horns stab over it. Nobody is in any hurry at all.", "The organ says the same thing again, and it's truer this time.", "You play it for the hundredth time and the floor moves like it's the first." }; var rn = 0;
            Spot("The Shrine's organ", og + up * 1.3f + orot * Vector3.forward * .5f, "Play the organ riff", new Vector3(1.5f, 1.2f, 1f)).onUse = _ => { life.played = Time.time; Say1(og + up * 2.8f, riff[rn++ % riff.Length]); };
            var cg = fo.P(4.6f, .7f, sz + .8f); for (var k = 0; k < 3; k++) { var cp = cg + fo.Right * (-.5f + k * .5f) + z.front * ((k % 2) * .2f); z.mk.Prism(Paint(Cloth[(k + 4) % 6]), cp, up, .2f, .9f, 10, 1.15f); z.mk.Prism(white, cp + up * .9f, up, .235f, .03f, 10); }
            string[] conga = { "Tum, ta-tum-tum. The dancers find it with their shoulders first.", "Open tone, slap, open tone. A horn player looks round and grins.", "You play the same bar until it stops being a bar and becomes weather." }; var cn = 0;
            Spot("The congas", cg + up * 1.2f, "Play the congas", new Vector3(1.6f, 1.2f, 1f)).onUse = _ => { life.played = Time.time; Say1(cg + up * 2.6f, conga[cn++ % conga.Length]); };
            var sk = new MeshKit(); sk.Blob(WorldMaterials.Solid(new Color(.75f, .6f, .35f)), new Vector3(0, .14f, 0), new Vector3(.13f, .14f, .13f), rng, .05f); sk.Prism(WorldMaterials.Solid(new Color(.75f, .6f, .35f)), new Vector3(0, .26f, 0), Vector3.up, .04f, .14f, 6); for (var k = 0; k < 10; k++) sk.Blob(Paint(Cloth[k % 6]), new Vector3(0, .14f, 0) + Quaternion.Euler(k * 36f, k * 77f, 0) * Vector3.up * .135f, Vector3.one * .02f, rng, 0f);
            Thing(sk, "Shekere", "a shekere", fo.P(-5f, .72f, sz + 1.2f), Quaternion.identity, new Vector3(.28f, .42f, .28f), .3f);
            // the floor: people dancing, benches down the sides, a drinks hatch
            Folk(fo.P(-2.2f, .06f, 1.4f), -z.front, Resident.Pose.Wave, new[] { "It's been this song since before you were born! Come on!", "You don't dance to it. You get in it. Like water." });
            Folk(fo.P(1.6f, .06f, 2.6f), (-z.front + z.side * .4f).normalized, Resident.Pose.Wave, new[] { "Ah! You came! Everybody comes eventually.", "My feet know this one. I just follow them." });
            Folk(fo.P(.2f, .06f, -.2f), -z.front, Resident.Pose.Wave, new[] { "(Eyes closed. One hand up. Not available for conversation.)", "Shh. The horns are about to come back in. ...Now." });
            Bench(z.mk, fo.P(-W / 2f + 1.4f, .06f, 1.6f), fo.Right, 3, 3f); Bench(z.mk, fo.P(W / 2f - 1.4f, .06f, 1.6f), -fo.Right, 4, 3f);
            for (var k = 0; k < 3; k++) { var bt = new MeshKit(); bt.Prism(GlowM(new Color(.5f, .25f, .1f), .3f), Vector3.zero, Vector3.up, .04f, .18f, 6); bt.Prism(GlowM(new Color(.5f, .25f, .1f), .3f), new Vector3(0, .18f, 0), Vector3.up, .018f, .08f, 5); Thing(bt, "Malt", "a cold malt", fo.P(W / 2f - 1.4f + (k - 1) * .3f, .58f, 3.4f), Quaternion.identity, new Vector3(.1f, .28f, .1f), .2f, false); }
            z.mk.Box(woodM, fo.P(W / 2f - 1.4f, .3f, 3.4f), new Vector3(1.2f, .5f, .5f), fo.r); BoxCol(z.root, fo.P(W / 2f - 1.4f, .3f, 3.4f), new Vector3(1.2f, .5f, .5f), fo.r);
            ArtistBits.PointLight(z.root, fo.P(0, 3.8f, sz + 1.4f), new Color(1f, .7f, .35f), 20f, 3.2f); ArtistBits.PointLight(z.root, fo.P(0, 3f, 2.4f), new Color(1f, .5f, .6f), 16f, 2.2f); z.area.glowLight = ArtistBits.PointLight(z.root, fo.P(0, 4.6f, -1f), new Color(1f, .8f, .5f), 30f, 3f); halos.Add(fo.P(0, 4f, sz + 1f), 6f, Orange, .3f);
            // "Water No Get Enemy": the fountain out front
            var ft = z.At(8f, -8.5f); z.mk.Prism(concrete, ft - up * .1f, up, 2f, .7f, 18); z.solid.Prism(concrete, ft - up * .3f, up, 2f, .9f, 12); z.mk.Prism(SongGlow(s, new Color(.25f, .75f, 1f), 1.2f), ft + up * .56f, up, 1.75f, .03f, 18); z.mk.Prism(concrete, ft + up * .5f, up, .25f, .8f, 8); z.mk.Prism(Paint(Paints[1]), ft + up * 1.3f, up, .5f, .1f, 10, .6f);
            halos.Add(ft + up * 1f, 2.6f, Cyan, .4f); life.fountainAt = ft + up * 1.45f; ArtistBits.Label(z.labels, "water no get enemy", ft + up * .3f + z.front * 2.02f, z.front, .14f, Ink, TextAnchor.MiddleCenter);
            Mark(fo.o, 10f);
            Shot(z.ctx, "the shrine", fo.P(-7f, 2.2f, D / 2f + 12f), fo.P(0, 2.6f, -2f));
            Shot(z.ctx, "on the shrine's floor", fo.P(5f, 1.8f, 4.6f), fo.P(-1.5f, 1.6f, sz));
        }

        // ---------------------------------------------------------------- Burna Boy
        void BurnaBoy(Plot z)
        {
            var up = Vector3.up; var rot = z.Rot; var rng = new System.Random(91); var ye = Song(z, "Ye"); var ll = Song(z, "Last Last"); life.lastSong = ll; var c = z.At(-4f, 0);
            // a wall of painted speakers, cones glowing, YE in lights across the top
            var cone = SongGlow(ye, Orange, 1.5f); for (var r = 0; r < 3; r++) for (var k = 0; k < 5; k++) { var p = c + z.side * (-3.4f + k * 1.7f) + up * (.75f + r * 1.5f); z.mk.Box(Paint(r == 1 ? new Color(.14f, .12f, .14f) : Paints[(k + r) % Paints.Length] * .6f), p, new Vector3(1.6f, 1.4f, 1f), rot); z.mk.Prism(dark, p + z.front * .5f, z.front, .52f, .03f, 14); z.mk.Prism(cone, p + z.front * .52f, z.front, .2f, .03f, 12); }
            z.solid.Box(dark, c + up * 2.25f, new Vector3(8.5f, 4.5f, 1f), rot); ArtistBits.Label(z.labels, "YE", c + up * 5.4f + z.front * .3f, z.front, 1.4f, Gold, TextAnchor.MiddleCenter); foreach (var e in new[] { -1f, 1f }) z.mk.Prism(iron, c + up * 4.5f + z.side * e * 1.2f, up, .04f, 1.6f, 4); z.mk.Box(dark, c + up * 5.4f + z.front * .2f, new Vector3(3.2f, 1.7f, .1f), rot);
            var yb = SongGlow(ye, Gold, 2.2f); for (var k = 0; k < 9; k++) { z.mk.Blob(yb, c + up * 6.3f + z.side * (-1.5f + k * .375f) + z.front * .3f, Vector3.one * .08f, rng, 0f); z.mk.Blob(yb, c + up * 4.5f + z.side * (-1.5f + k * .375f) + z.front * .3f, Vector3.one * .08f, rng, 0f); } halos.Add(c + up * 5.4f + z.front * 1f, 3.4f, Gold, .45f);
            z.area.glowLight = ArtistBits.PointLight(z.root, c + up * 3f + z.front * 3.4f, new Color(1f, .65f, .3f), 26f, 3f);
            var mic = c + z.front * 2.4f; z.mk.Prism(iron, mic, up, .2f, .03f, 8); z.mk.Prism(iron, mic, up, .018f, 1.5f, 4); z.mk.Blob(tin, mic + up * 1.56f, new Vector3(.06f, .09f, .06f), rng, 0f); ArtistBits.PostCollider(z.root, mic, 1.5f, .06f);
            string[] sing = { "\"Ye, ye, ye, ye.\" The whole road sings the second ye.", "You sing it wrong. A hundred people sing it right over you, kindly.", "The speakers thump once, to show they could." }; var sn = 0;
            Spot("Burna's microphone", mic + up * 1.3f, "Sing the chorus", new Vector3(1f, 1.8f, 1f)).onUse = _ => { life.played = Time.time; Say1(mic + up * 2.8f, sing[sn++ % sing.Length]); };
            // a giant's footprints across the yard
            var print = WorldMaterials.Solid(new Color(.5f, .24f, .14f)); for (var k = 0; k < 3; k++) { var fp = z.At(3f + k * 2.6f, (k % 2 == 0 ? -1f : 1f) * 3.4f - 1.5f) + up * .12f; var fr = rot * Quaternion.Euler(0, (k % 2 == 0 ? -8f : 8f), 0); z.mk.Blob(print, fp, new Vector3(.75f, .02f, 1.2f), rng, .05f); z.mk.Blob(print, fp - fr * Vector3.forward * 1.3f, new Vector3(.55f, .02f, .5f), rng, .05f); for (var q = 0; q < 5; q++) z.mk.Blob(print, fp + fr * new Vector3(-.6f + q * .3f, 0, 1.4f - Mathf.Abs(q - 1.5f) * .12f), new Vector3(.14f - q * .012f, .02f, .16f), rng, 0f); }
            // "Last Last": a heart broken in two, and breakfast on a plastic table
            var hp = z.At(5f, 6.5f); z.mk.Prism(iron, hp - up * .2f, up, .05f, 1.6f, 5); ArtistBits.PostCollider(z.root, hp, 1.6f, .08f); var hm = SongGlow(ll, new Color(1f, .3f, .4f), 2.2f);
            for (var k = 0; k < 24; k++) { var t = k / 24f * Mathf.PI * 2f; var hx = 16f * Mathf.Pow(Mathf.Sin(t), 3f); var hy = 13f * Mathf.Cos(t) - 5f * Mathf.Cos(2f * t) - 2f * Mathf.Cos(3f * t) - Mathf.Cos(4f * t); z.mk.Blob(hm, hp + up * (2.6f + hy * .06f) + z.side * (hx * .06f + (hx > 0 ? .22f : -.22f)) + up * (hx > 0 ? .12f : -.1f), Vector3.one * .09f, rng, 0f); }
            halos.Add(hp + up * 2.6f + z.front * .3f, 2f, Pink, .5f);
            var tb = z.At(6.5f, 3.6f); z.mk.Box(Paint(Cream), tb + up * .72f, new Vector3(1f, .05f, 1f), rot); foreach (var (x, zz) in new[] { (-.42f, -.42f), (.42f, -.42f), (.42f, .42f), (-.42f, .42f) }) z.mk.Box(Paint(Cream), tb + up * .36f + rot * new Vector3(x, 0, zz), new Vector3(.05f, .72f, .05f), rot); BoxCol(z.root, tb + up * .4f, new Vector3(1f, .8f, 1f), rot); Chair(z.mk, tb + z.front * .95f, -z.front, 3);
            var bf = new MeshKit(); bf.Prism(white, Vector3.zero, Vector3.up, .16f, .02f, 12); bf.Box(WorldMaterials.Solid(new Color(.85f, .7f, .45f)), new Vector3(-.04f, .05f, 0), new Vector3(.14f, .06f, .12f), Quaternion.identity); bf.Blob(WorldMaterials.Solid(new Color(.98f, .96f, .9f)), new Vector3(.07f, .03f, .02f), new Vector3(.06f, .015f, .06f), rng, 0f); bf.Blob(GlowM(Gold, .5f), new Vector3(.07f, .045f, .02f), Vector3.one * .02f, rng, 0f);
            Thing(bf, "Breakfast", "breakfast (somebody had to eat it)", tb + up * .76f, rot, new Vector3(.34f, .1f, .34f), .2f, false);
        }

        // ---------------------------------------------------------------- Wizkid: Essence
        void Wizkid(Plot z)
        {
            var up = Vector3.up; var rot = z.Rot; var rng = new System.Random(92); var s = Song(z, "Essence"); life.essenceSong = s; var c = z.At(-3.5f, 0);
            // a rooftop lounge on a deck: a pergola strung with bulbs, a sofa, a low table
            z.mk.Box(woodM, c + up * .3f, new Vector3(8f, .6f, 5.4f), rot); z.solid.Box(woodM, c + up * .1f, new Vector3(8f, 1f, 5.4f), rot); for (var k = 0; k < 2; k++) { z.mk.Box(woodM, c + up * (.15f + k * .0f) + z.front * (3f + k * .0f) - up * .0f, new Vector3(2.4f, .3f, .7f), rot); } z.solid.Box(woodM, c + up * .15f + z.front * 3f, new Vector3(2.4f, .3f, .7f), rot);
            var d = c + up * .6f; var tops = new List<Vector3>(); foreach (var (x, zz) in new[] { (-1f, -1f), (1f, -1f), (1f, 1f), (-1f, 1f) }) { var p = d + z.side * x * 3.7f + z.front * zz * 2.4f; z.mk.Prism(white, p, up, .07f, 3f, 5); ArtistBits.PostCollider(z.root, p, 3f, .09f); tops.Add(p + up * 3f); }
            for (var k = 0; k < 4; k++) BulbString(z.mk, tops[k], tops[(k + 1) % 4], k); BulbString(z.mk, tops[0], tops[2], 1); for (var k = 0; k < 7; k++) z.mk.Box(white, d + up * 3f + z.side * (-3.6f + k * 1.2f), new Vector3(.08f, .08f, 5f), rot);
            var sofa = Paint(new Color(.94f, .9f, .82f)); var sp = d - z.front * 1.5f; z.mk.Box(sofa, sp + up * .3f, new Vector3(3.4f, .44f, .9f), rot); z.mk.Box(sofa, sp + up * .72f - z.front * .36f, new Vector3(3.4f, .7f, .2f), rot); foreach (var e in new[] { -1.6f, 1.6f }) z.mk.Box(sofa, sp + up * .5f + z.side * e, new Vector3(.2f, .5f, .9f), rot); for (var k = 0; k < 3; k++) z.mk.Blob(Paint(Cloth[(k * 2 + 1) % 6]), sp + up * .64f + z.side * (-1f + k * 1f) - z.front * .14f, new Vector3(.26f, .2f, .12f), rng, .1f);
            BoxCol(z.root, sp + up * .45f, new Vector3(3.4f, .9f, .9f), rot); Interact.Bench(tlife.transform, sp + up * .56f - z.side * 1f + z.front * .1f, sp + up * .56f + z.side * 1f + z.front * .1f, z.front);
            // the bottle of ESSENCE, tall and glowing, on a little table; a star on a pole
            var tb = d + z.front * .6f; z.mk.Prism(white, tb, up, .5f, .4f, 12); var rose = SongGlow(s, new Color(1f, .45f, .65f), 1.8f); z.mk.Box(rose, tb + up * .85f, new Vector3(.4f, .9f, .26f), rot); z.mk.Prism(SongGlow(s, Gold, 1.6f), tb + up * 1.3f, up, .08f, .16f, 8); z.mk.Blob(SongGlow(s, Gold, 1.6f), tb + up * 1.54f, Vector3.one * .1f, rng, 0f); ArtistBits.Label(z.labels, "ESSENCE", tb + up * .85f + z.front * .14f, z.front, .09f, Ink, TextAnchor.MiddleCenter); halos.Add(tb + up * 1f + z.front * .3f, 1.8f, Pink, .55f); ArtistBits.PostCollider(z.root, tb, 1.4f, .5f);
            z.area.glowLight = ArtistBits.PointLight(z.root, tb + up * 2f, new Color(1f, .6f, .7f), 22f, 2.8f);
            var st = z.At(4f, -7f); z.mk.Prism(iron, st - up * .2f, up, .06f, 4.6f, 5); ArtistBits.PostCollider(z.root, st, 4.4f, .08f); var star = SongGlow(s, Gold, 2.2f); for (var k = 0; k < 5; k++) z.mk.Box(star, st + up * 4.9f + Quaternion.AngleAxis(k * 72f, z.front) * up * .5f, new Vector3(.3f, 1f, .1f), Quaternion.AngleAxis(k * 72f, z.front) * rot); z.mk.Prism(star, st + up * 4.9f - z.front * .05f, z.front, .34f, .1f, 10); halos.Add(st + up * 4.9f + z.front * .3f, 2.4f, Gold, .5f);
            for (var k = 0; k < 2; k++) { var gm = new MeshKit(); gm.Prism(GlowM(new Color(1f, .6f, .5f), .6f), Vector3.zero, Vector3.up, .035f, .12f, 8, 1.4f); gm.Prism(tin, new Vector3(0, -.1f, 0), Vector3.up, .01f, .1f, 4); gm.Prism(tin, new Vector3(0, -.11f, 0), Vector3.up, .05f, .01f, 8); Thing(gm, "Glass", "something pink, with ice", tb + up * .52f + z.side * (k == 0 ? -.3f : .32f) + z.front * .26f, Quaternion.identity, new Vector3(.1f, .24f, .1f), .15f, false); }
            Folk(d + z.side * 2.6f + z.front * 1.2f, (z.front - z.side * .4f).normalized, Resident.Pose.Lean, new[] { "Shh. Just watch it go down.", "Only you fit hold my body. ...That's the song, not me. Although.", "Best seat on the coast. Don't tell the beach I said so." });
        }

        // ---------------------------------------------------------------- William Onyeabor: Fantastic Man
        void WilliamOnyeabor(Plot z)
        {
            var up = Vector3.up; var rot = z.Rot; var rng = new System.Random(93); var s = Song(z, "Fantastic Man"); life.fantasticSong = s; var c = z.At(-3f, 0);
            // a wall of machines: a tall cabinet of knobs and lights, keyboards on three sides
            z.mk.Box(dark, c + up * 1.5f - z.front * .6f, new Vector3(5f, 3f, .6f), rot); z.solid.Box(dark, c + up * 1.5f - z.front * .6f, new Vector3(5f, 3f, .6f), rot); var cols = new[] { Orange, Cyan, Pink, Gold, Green, Purple };
            for (var r = 0; r < 6; r++) for (var k = 0; k < 14; k++) { var p = c + up * (.5f + r * .44f) + z.side * (-2.2f + k * .34f) - z.front * .28f; if ((r + k) % 3 == 0) z.mk.Blob(SongGlow(s, cols[(r * 2 + k) % cols.Length], 1.8f), p, Vector3.one * .045f, rng, 0f); else z.mk.Prism(tin, p, z.front, .05f, .05f, 6); }
            for (var k = 0; k < 6; k++) { var a = c + up * (2.4f - (k % 3) * .5f) + z.side * (-2f + k * .5f) - z.front * .28f; var b = c + up * (1f + (k % 2) * .5f) + z.side * (1.8f - k * .6f) - z.front * .28f; var mid = (a + b) / 2f + z.front * .3f - up * .3f; z.mk.Prism(Paint(cols[k]), a, (mid - a).normalized, .015f, (mid - a).magnitude, 3); z.mk.Prism(Paint(cols[k]), mid, (b - mid).normalized, .015f, (b - mid).magnitude, 3); }
            halos.Add(c + up * 1.6f + z.front * .4f, 3.4f, Cyan, .35f); z.area.glowLight = ArtistBits.PointLight(z.root, c + up * 2.6f + z.front * 2f, new Color(.6f, .9f, 1f), 22f, 2.8f);
            foreach (var (x, yaw) in new[] { (0f, 0f), (-2.2f, 60f), (2.2f, -60f) }) { var kp = c + z.side * x + z.front * (x == 0f ? 1f : 1.6f); var kr = rot * Quaternion.Euler(0, yaw, 0); z.mk.Box(Paint(new Color(.8f, .2f, .2f)), kp + up * .9f, new Vector3(1.4f, .16f, .5f), kr); z.mk.Box(white, kp + up * .99f + kr * Vector3.forward * .1f, new Vector3(1.3f, .03f, .22f), kr); foreach (var e in new[] { -.6f, .6f }) z.mk.Box(iron, kp + up * .45f + kr * Vector3.right * e, new Vector3(.04f, .9f, .4f), kr); BoxCol(z.root, kp + up * .5f, new Vector3(1.4f, 1f, .5f), kr); }
            string[] synth = { "A fat, wobbling bass note. Then a tune so cheerful it's practically waving.", "Bleep, bloop, and a chord like sunrise over a factory. Fantastic.", "You turn one knob a little and the whole thing starts to laugh.", "It plays back what you played, but funkier. It always does that." }; var sn = 0;
            Spot("The synthesisers", c + z.front * 1.6f + up * 1.2f, "Play the synthesisers", new Vector3(2f, 1.4f, 1.6f)).onUse = _ => { life.played = Time.time; Say1(c + up * 3.6f, synth[sn++ % synth.Length]); };
            // a white hat on a stand, and a mirror that tells you what you want to hear
            var hs = z.At(2.5f, 6f); z.mk.Prism(iron, hs, up, .16f, .03f, 8); z.mk.Prism(iron, hs, up, .02f, 1.5f, 4); z.mk.Prism(white, hs + up * 1.5f, up, .42f, .03f, 12); z.mk.Prism(white, hs + up * 1.52f, up, .2f, .2f, 10, .85f); ArtistBits.PostCollider(z.root, hs, 1.6f, .1f);
            var mr = z.At(2.5f, -6f); z.mk.Box(Paint(Gold), mr + up * 1.2f, new Vector3(1.2f, 2.2f, .1f), rot); z.mk.Box(WorldMaterials.Solid(new Color(.6f, .7f, .78f), .95f), mr + up * 1.2f + z.front * .06f, new Vector3(1f, 2f, .02f), rot); BoxCol(z.root, mr + up * 1.2f, new Vector3(1.2f, 2.2f, .14f), rot); ArtistBits.Label(z.labels, "FANTASTIC", mr + up * 2.5f + z.front * .06f, z.front, .2f, Gold, TextAnchor.MiddleCenter);
            string[] look = { "\"You look so good,\" says the mirror. It's right, too.", "\"Fantastic,\" says the mirror, to you and to itself.", "The mirror wolf-whistles. A synth plays it back a third higher." }; var ln = 0;
            Spot("The mirror", mr + up * 1.3f + z.front * .6f, "Look in the mirror", new Vector3(1.2f, 2f, 1f)).onUse = _ => Say1(mr + up * 3f, look[ln++ % look.Length]);
        }

        // ---------------------------------------------------------------- Nomo: Upside Down
        void Nomo(Plot z)
        {
            var up = Vector3.up; var rot = z.Rot; var rng = new System.Random(94); var s = Song(z, "Upside Down"); var f = new PlaceFrame(z.At(-3.4f, 0), z.front); var c1 = Paint(Cloth[1]); var c2 = Paint(Cloth[3]);
            // a stall built the wrong way up: awning on the ground, table in the air, legs pointing at the sky
            for (var k = 0; k < 8; k++) z.mk.Box(k % 2 == 0 ? c1 : c2, f.P(-2.1f + k * .6f, .12f + (k % 2) * .012f, .1f), new Vector3(.58f, .04f, 3f), f.r * Quaternion.Euler(-6f, 0, 0)); foreach (var (x, zz) in new[] { (-2.2f, -1.1f), (2.2f, -1.1f), (2.2f, 1.2f), (-2.2f, 1.2f) }) { z.mk.Prism(woodM, f.P(x, .1f, zz), up, .06f, 3.2f, 4); ArtistBits.PostCollider(z.root, f.P(x, 0, zz), 3f, .08f); }
            z.mk.Box(woodM, f.P(0, 2.3f, .1f), new Vector3(4f, .08f, 1.1f), f.r); foreach (var x in new[] { -1.8f, 1.8f }) z.mk.Box(woodM, f.P(x, 2.7f, .1f), new Vector3(.08f, .8f, 1f), f.r); for (var k = 0; k < 4; k++) Drum(z.mk, f.P(-1.2f + k * .8f, 1.56f, .1f), 1.1f, SongGlow(s, Gold, .8f));
            var t = ArtistBits.Label(z.labels, "NOMO", f.P(0, 3.3f, 1.3f), z.front, .34f, Gold, TextAnchor.MiddleCenter); t.transform.Rotate(0, 0, 180f); z.mk.Box(Paint(Cream), f.P(0, 3.3f, 1.26f), new Vector3(2.6f, .7f, .05f), f.r);
            for (var k = 0; k < 6; k++) z.mk.Blob(bulbM[k % 3], f.P(-2f + k * .8f, .3f, 1.5f), Vector3.one * .085f, rng, 0f); halos.Add(f.P(0, .6f, 1.6f), 2f, Gold, .35f);
            // a palm standing on its head, and a chair on the "ceiling"
            var pp = z.At(2.5f, 5f); z.mk.Prism(palmTrunk, pp + up * .9f, up, .16f, 3.6f, 6, 1.2f); ArtistBits.PostCollider(z.root, pp, 4f, .2f); for (var k = 0; k < 8; k++) { var fd = Quaternion.Euler(0, k * 45f, 0) * Vector3.forward; z.mk.Box(k % 2 == 0 ? leafM : leafDark, pp + up * .6f + fd * 1f, new Vector3(.4f, .05f, 2.2f), Quaternion.LookRotation(fd) * Quaternion.Euler(-24f, 0, 0)); } for (var k = 0; k < 5; k++) z.mk.Prism(palmTrunk, pp + up * 4.4f, (Quaternion.Euler(0, k * 72f, 0) * Vector3.forward + up * .6f).normalized, .04f, .7f, 3, .3f);
            string[] flip = { "For a moment the sky is the floor and the sea is overhead, and it's fine, actually.", "The horns come in backwards and it sounds even better.", "Everything's the right way up. It's you that's upside down. Carry on." }; var fn = 0;
            Spot("The upside-down stall", f.P(0, 1.2f, 1.2f), "Stand on your head", new Vector3(3f, 2f, 1.4f)).onUse = _ => { life.played = Time.time; Say1(f.P(0, 3.8f, 1.2f), flip[fn++ % flip.Length]); };
            Bench(z.mk, f.P(-3.6f, 0, 1.8f), (z.front + z.side * .4f).normalized, 2);
        }

        // ---------------------------------------------------------------- everyone else
        void Others(Plot z)
        {
            var up = Vector3.up; var rot = z.Rot; var p = z.At(2.8f, -3.2f); var rng = new System.Random(z.a.name.Length * 3); string s;
            switch (z.a.name)
            {
                case "CKay":
                    s = Song(z, "love nwantiti"); life.heartsSong = s; life.heartsAt = p + up * 1.4f; var hm = SongGlow(s, new Color(1f, .35f, .6f), 2.2f); foreach (var e in new[] { -1.8f, 1.8f }) { z.mk.Prism(woodM, p + z.side * e - up * .2f, up, .05f, 3.4f, 5); ArtistBits.PostCollider(z.root, p + z.side * e, 3.2f, .07f); }
                    for (var h = 0; h < 3; h++) { var hc = p + up * (2.7f - Mathf.Abs(h - 1) * -.2f) + z.side * (-1.1f + h * 1.1f); for (var k = 0; k < 14; k++) { var t = k / 14f * Mathf.PI * 2f; var hx = 16f * Mathf.Pow(Mathf.Sin(t), 3f); var hy = 13f * Mathf.Cos(t) - 5f * Mathf.Cos(2f * t) - 2f * Mathf.Cos(3f * t) - Mathf.Cos(4f * t); z.mk.Blob(hm, hc + up * hy * .026f + z.side * hx * .026f, Vector3.one * .05f, rng, 0f); } }
                    halos.Add(p + up * 2.7f + z.front * .4f, 2.6f, Pink, .45f); ArtistBits.Label(z.labels, "ah ah ah", p + up * 1.9f, z.front, .24f, Pink, TextAnchor.MiddleCenter);
                    z.mk.Box(dark, p + up * .86f, new Vector3(1.1f, .12f, .36f), rot); z.mk.Box(white, p + up * .93f + z.front * .06f, new Vector3(1f, .02f, .16f), rot); foreach (var e in new[] { -.45f, .45f }) z.mk.Prism(iron, p + z.side * e, up, .02f, .82f, 4); BoxCol(z.root, p + up * .5f, new Vector3(1.1f, 1f, .4f), rot); break;
                case "Ayra Starr":
                    s = Song(z, "Rush"); life.rushSong = s; z.mk.Prism(iron, p - up * .2f, up, .06f, 3.6f, 5); ArtistBits.PostCollider(z.root, p, 3.4f, .08f); var star = SongGlow(s, Cyan, 2.2f); for (var k = 0; k < 5; k++) z.mk.Box(star, p + up * 3.9f + Quaternion.AngleAxis(k * 72f, z.front) * up * .46f, new Vector3(.28f, .9f, .1f), Quaternion.AngleAxis(k * 72f, z.front) * rot); z.mk.Prism(star, p + up * 3.9f - z.front * .05f, z.front, .32f, .1f, 10); halos.Add(p + up * 3.9f + z.front * .3f, 2.2f, Cyan, .5f);
                    Okada(z.ctx, z.mk, z.At(4.2f, 1.4f), rot * Quaternion.Euler(0, 90f, 0), 4); for (var k = 0; k < 5; k++) z.mk.Box(SongGlow(s, Cream, 1.2f), z.At(4.2f, 1.4f) + up * (.4f + k * .16f) + z.side * (-1.4f - k * .12f), new Vector3(.6f + k * .1f, .02f, .02f), rot);
                    ArtistBits.Label(z.labels, "RUSH", p + up * 2.4f + z.front * .08f, z.front, .36f, Cyan, TextAnchor.MiddleCenter); break;
                case "Davido":
                    s = Song(z, "Fall"); life.fallSong = s; life.bananasAt = p; Plantain(z.ctx, z.mk, p - z.front * .6f, 1.5f, rng); var yel = SongGlow(s, new Color(1f, .88f, .2f), 1.3f); z.mk.Box(woodM, p + z.front * .9f + up * .25f, new Vector3(1.4f, .5f, .9f), rot); BoxCol(z.root, p + z.front * .9f + up * .25f, new Vector3(1.4f, .5f, .9f), rot);
                    for (var k = 0; k < 12; k++) z.mk.Box(yel, p + z.front * .9f + up * (.56f + (k / 6) * .08f) + z.side * (-.5f + (k % 6) * .2f), new Vector3(.07f, .07f, .46f), rot * Quaternion.Euler(0, (k % 3) * 14f - 14f, 20f)); halos.Add(p + z.front * .9f + up * .8f, 1.4f, Gold, .45f);
                    for (var k = 0; k < 2; k++) { var bm = new MeshKit(); for (var q = 0; q < 4; q++) bm.Box(GlowM(new Color(1f, .88f, .2f), .5f), new Vector3(-.09f + q * .06f, .05f, 0), new Vector3(.05f, .05f, .36f), Quaternion.Euler(0, q * 6f - 9f, 18f)); Thing(bm, "Bananas", "a hand of bananas", z.At(4.4f, 1.2f + k * .7f) + up * .12f, rot, new Vector3(.3f, .14f, .4f), .3f); } break;
                case "Diamond Platnumz":
                    s = Song(z, "Komasava"); foreach (var e in new[] { -1.9f, 1.9f }) { z.mk.Prism(Paint(Paints[1]), p + z.side * e - up * .2f, up, .12f, 3.6f, 6); ArtistBits.PostCollider(z.root, p + z.side * e, 3.4f, .14f); } z.mk.Box(Paint(Paints[0]), p + up * 3.5f, new Vector3(4.4f, .5f, .2f), rot);
                    string[] hi = { "KOMASAVA?", "COMMENT ÇA VA?", "HABARI?", "HOW FAR?" }; for (var k = 0; k < 4; k++) { var bp = p + up * (2.9f - k * .56f) + z.side * (k % 2 == 0 ? -.5f : .5f); z.mk.Box(Paint(Cloth[(k * 2) % 6]), bp, new Vector3(2.6f, .46f, .06f), rot * Quaternion.Euler(0, 0, k % 2 == 0 ? 4f : -4f)); ArtistBits.Label(z.labels, hi[k], bp + z.front * .05f, z.front, .22f, Cream, TextAnchor.MiddleCenter); }
                    var dia = SongGlow(s, new Color(.6f, .95f, 1f), 2.2f); z.mk.Cone(dia, p + up * 4.3f, .4f, .34f, 6); z.mk.Cone(dia, p + up * 4.3f, .4f, -.6f, 6); halos.Add(p + up * 4.2f, 1.8f, Cyan, .55f); break;
                case "I Hate My Village":
                    s = Song(z, "Tony Hawk of Ghana"); var ramp = Paint(Paints[5]); for (var k = 0; k < 6; k++) { var a = k * 14f; var rp = p + z.side * (-1.8f + Mathf.Sin(a * Mathf.Deg2Rad) * 2.6f) + up * (.1f + (1f - Mathf.Cos(a * Mathf.Deg2Rad)) * 2.6f); z.mk.Box(k % 2 == 0 ? ramp : woodM, rp, new Vector3(.7f, .08f, 3f), rot * Quaternion.Euler(0, 0, a)); z.solid.Box(woodM, rp, new Vector3(.7f, .12f, 3f), rot * Quaternion.Euler(0, 0, a)); }
                    z.mk.Box(woodM, p + z.side * 1f + up * 1.6f, new Vector3(.9f, .08f, 3f), rot); z.solid.Box(woodM, p + z.side * 1.3f + up * .85f, new Vector3(.3f, 1.7f, 3f), rot); z.mk.Box(woodM, p + z.side * 1.3f + up * .85f, new Vector3(.2f, 1.6f, 3f), rot); z.mk.Prism(tin, p + z.side * .56f + up * 1.66f - z.front * 1.5f, z.front, .04f, 3f, 6);
                    var sb = new MeshKit(); sb.Box(SongGlow(s, Gold, .6f), new Vector3(0, .09f, 0), new Vector3(.2f, .02f, .78f), Quaternion.identity); foreach (var e in new[] { -.26f, .26f }) foreach (var q in new[] { -.08f, .08f }) sb.Prism(white, new Vector3(q - .015f * Mathf.Sign(q), .035f, e), Vector3.right * Mathf.Sign(q), .035f, .03f, 8); Thing(sb, "Skateboard", "a skateboard", z.At(4.6f, 0f) + up * .12f, rot, new Vector3(.24f, .12f, .8f), .6f);
                    ArtistBits.Label(z.labels, "TONY HAWK\nOF GHANA", p + z.side * 1.46f + up * 1f, z.side, .2f, Cream, TextAnchor.MiddleCenter); break;
                case "Letta Mbulu":
                    s = Song(z, "Nomalizo"); var bark = WorldMaterials.Solid(new Color(.5f, .42f, .36f)); z.mk.Prism(bark, p - up * .3f, up, .9f, 3.6f, 9, .7f); ArtistBits.PostCollider(z.root, p, 3.4f, .9f); for (var k = 0; k < 6; k++) { var bd = (Quaternion.Euler(0, k * 60f, 0) * Vector3.forward + up * .6f).normalized; z.mk.Prism(bark, p + up * 3.2f, bd, .16f, 1.8f, 5, .3f); z.mk.Blob(k % 2 == 0 ? leafM : leafDark, p + up * 3.2f + bd * 2f, new Vector3(.9f, .5f, .9f), rng, .3f); }
                    var st = p + z.front * 1.8f + z.side * .4f; z.mk.Prism(woodM, st, up, .22f, .5f, 8); var rm = new MeshKit(); rm.Box(Paint(Cloth[0]), new Vector3(0, .15f, 0), new Vector3(.5f, .3f, .18f), Quaternion.identity); rm.Prism(tin, new Vector3(-.12f, .15f, .09f), Vector3.forward, .09f, .02f, 10); rm.Blob(SongGlow(s, Gold, 2f), new Vector3(.16f, .2f, .1f), Vector3.one * .025f, rng, 0f); rm.Prism(tin, new Vector3(.2f, .3f, 0), new Vector3(.3f, 1f, 0).normalized, .008f, .5f, 3);
                    var rgo = rm.BuildSingle("The radio", tlife.transform, false); rgo.transform.SetPositionAndRotation(st + up * .5f, rot); string[] dial = { "Static, then a woman's voice, clear as water over stones.", "A choir comes in under her. The baobab seems to stand a little straighter.", "The song fades in from very far away, as if the radio had to go and fetch it." }; var dn = 0;
                    Interact.Toy(rgo, Toy.Kind.Custom, "Turn the dial", new Vector3(.7f, .6f, .5f)).onUse = _ => Say1(st + up * 1.8f, dial[dn++ % dial.Length]); halos.Add(st + up * .7f, .9f, Gold, .4f);
                    for (var k = 0; k < 3; k++) { z.mk.Prism(Paint(Cloth[(k * 2 + 1) % 6]), p - z.front * .2f + z.side * (1.4f + k * .5f) + up * .0f, up, .2f + k * .04f, .3f, 10, 1.4f); } break;
                case "Manu Dibango":
                    s = Song(z, "Mpuli Mwa Moni"); z.mk.Prism(concrete, p - up * .1f, up, 1.3f, .6f, 12); z.solid.Prism(concrete, p - up * .3f, up, 1.3f, .8f, 10); var gold = SongGlow(s, new Color(1f, .78f, .25f), 1.1f); Horn(z.mk, p + up * .5f, rot, gold, 4.2f); halos.Add(p + up * 2.6f, 3f, Gold, .4f); ArtistBits.PostCollider(z.root, p, 3.6f, .5f);
                    for (var k = 0; k < 5; k++) z.mk.Blob(SongGlow(s, Cream, 1.6f), p + up * (2.2f + k * .5f) + z.front * (1.2f + k * .3f) + z.side * Mathf.Sin(k * 1.3f) * .5f, Vector3.one * (.07f + k * .01f), rng, 0f); break;
                default:
                    z.mk.Prism(woodM, p, up, .04f, 1.3f, 5); z.mk.Blob(GlowM(Gold, 1.4f), p + up * 1.44f, new Vector3(.16f, .2f, .16f), rng, 0f); break;
            }
        }
    }
}
