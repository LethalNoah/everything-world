using System.Collections.Generic;
using UnityEngine;
using static EverythingWorld.World.PlaceTools;

namespace EverythingWorld.World
{
    /// The Trance Steppe's artists. Each has a round stone floor with a gold ring, a tall slim stone bearing their name
    /// with a laser line down it, and one thing from their music facing the sunrise; covers on a pale stone board with a
    /// gold edge.
    /// - Above & Beyond: an arch holding a sun and a moon ("Sun & Moon"), a heart of light ("Thing Called Love") and a road
    ///   sign pointing away ("You Got To Go").
    /// - The rest: a sand tornado, a globe circled by hearts, a black obelisk banded with gold, a rain cloud over a lone
    ///   figure, a crumbling column, a ring of children holding hands, an aura of light, a split rock with light pouring
    ///   out, a little cooling tower, a golden blade.
    public partial class TranceKit
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
                Add("Darude", "Sandstorm", "Darude"); Add("Robert Miles", "Children", "Robert Miles"); Add("Above & Beyond", "Sun & Moon", "Above & Beyond"); Add("Zombie Nation", "Kernkraft", "Zombie Nation");
                Add("Myon", "Omen In The Rain", "Myon"); Add("Francesco Farfa", "Universal Love", "Francesco Farfa"); Add("Gesaffelstein", "Hate or Glory", "Gesaffelstein"); Add("Velvetine", "The Great Divide", "Velvetine");
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
            if (stage == null) MakeMaterials();
            var root = new GameObject("Artist: " + a.name).transform; root.SetParent(ctx.Root, false);
            var front = facing.TryGetValue(a.name, out var f) ? f : new Vector3(center.x - a.x, 0, center.z - a.z).normalized;
            if (front.sqrMagnitude < .01f) front = Vector3.forward;
            var labels = new GameObject("Labels").transform; labels.SetParent(root, false);
            var area = root.gameObject.AddComponent<ArtistArea>();
            area.artistName = a.name; area.stationId = a.stationId; area.radius = a.r; area.glowColor = Gold;
            var z = new Plot { ctx = ctx, root = root, labels = labels, mk = new MeshKit(), solid = new MeshKit(), pictures = new MeshKit(), a = a, c = ctx.OnGround(a.x, a.z), front = front, side = Vector3.Cross(Vector3.up, front), area = area };
            var reach = Foot(a); var up = Vector3.up;
            z.mk.Prism(stone, z.c - up * .3f, up, reach * .75f, .38f, 26); z.mk.Prism(GlowM(Gold, .9f), z.c + up * .09f, up, reach * .72f, .02f, 26); z.mk.Prism(stone, z.c + up * .1f, up, reach * .68f, .02f, 26);
            if (a.name == "Above & Beyond") Abgt(z); else Others(z, Stele(z));
            SongLamps(z);
            Board(z, z.At(reach * .6f, -reach * .45f - 1.5f), front);
            ArtistBits.Plaque(z.mk, z.solid, labels, a, z.At(reach * .6f + 1.5f, reach * .45f + 1.5f), front, iron, stone, Ink, 1.6f);
            Lamp(ctx, z.mk, z.At(reach * .5f, reach * .7f), false);
            if (z.area.glowLight == null) z.area.glowLight = ArtistBits.PointLight(root, z.c + up * 4f + front * 3f, Gold, HasStation(a) ? 22f : 13f, 1.2f);
            area.glowRenderers = new Renderer[0];
            z.mk.Build("Area", root, false);
            z.solid.BuildCollider("Walls", root);
            z.pictures.Build("Covers", root, false);
            noGrass.Add(new Vector3(z.c.x, reach * .85f, z.c.z));
            var mark = new GameObject("Shot: " + a.name + " up close").transform; mark.SetParent(ctx.Root, false);
            var cam = z.At(reach * .85f, reach * .3f) + up * 2f; var look = z.At(-reach * .3f, 0) + up * 1.8f - cam; mark.SetPositionAndRotation(cam, Quaternion.LookRotation(look.normalized));
        }

        /// The covers on a pale stone board with a gold edge.
        void Board(Plot z, Vector3 ground, Vector3 dir)
        {
            dir = Vector3.ProjectOnPlane(dir, Vector3.up).normalized; var rot = Quaternion.LookRotation(dir);
            var n = Mathf.Max(1, Mathf.Min(7, z.a.songList?.Length ?? 1)); const float size = .9f; var w = n * size * 1.25f + .6f;
            var c = ground + Vector3.up * 1.7f;
            z.mk.Box(lampM, c - Vector3.up * .5f - dir * .08f, new Vector3(w + .5f, size + 2f, .14f), rot); z.mk.Box(stone, c - Vector3.up * .5f - dir * .02f, new Vector3(w + .3f, size + 1.8f, .2f), rot); z.solid.Box(stone, c - Vector3.up * .5f, new Vector3(w + .5f, size + 2f, .3f), rot);
            z.mk.Box(Paint(new Color(.92f, .9f, .86f)), c - Vector3.up * .1f + dir * .09f, new Vector3(w, size + .9f, .04f), rot);
            ArtistBits.CoverWall(z.pictures, z.labels, z.ctx, z.a, c + dir * .12f + Vector3.up * .08f, dir, n, size, n, true, Ink);
        }

        /// A thin laser post for each song without its own piece.
        void SongLamps(Plot z)
        {
            if (!SongIds.TryGetValue(z.a.name, out var list)) return;
            var up = Vector3.up; var k = 0; var seen = new HashSet<string>();
            foreach (var (_, id) in list)
            {
                if (z.lit.Contains(id) || !seen.Add(id)) continue;
                var p = z.At(Foot(z.a) * .55f - (k % 2) * 1.3f, Foot(z.a) * .2f + (k / 2) * 1.3f);
                z.mk.Box(iron, p + up * .6f, new Vector3(.1f, 1.2f, .1f), z.Rot); z.mk.Box(SongGlow(id, Laser, 1.6f), p + up * 1.5f, new Vector3(.06f, .6f, .06f), z.Rot); ArtistBits.PostCollider(z.root, p, 1.8f, .1f);
                k++;
            }
        }

        /// A tall slim stone bearing their name with a laser line down it.
        PlaceFrame Stele(Plot z)
        {
            var up = Vector3.up; var f = new PlaceFrame(z.At(-Foot(z.a) * .5f, 0), z.front);
            z.mk.Box(stone, f.P(0, 1.6f, 0), new Vector3(1f, 3.2f, .4f), f.r); BoxCol(z.root, f.P(0, 1.6f, 0), new Vector3(1f, 3.2f, .4f), f.r); z.mk.Box(laserM, f.P(0, 1.6f, .21f), new Vector3(.04f, 3f, .02f), f.r); ArtistBits.Label(z.labels, z.a.name, f.P(0, 2.7f, .22f), f.Fwd, .11f, Ink, TextAnchor.MiddleCenter);
            return f;
        }

        // ---------------------------------------------------------------- Above & Beyond
        void Abgt(Plot z)
        {
            var up = Vector3.up; var rng = new System.Random(2000); var fr = z.front; var sd = z.side; var rot = z.Rot;
            var sm = Song(z, "Sun & Moon"); var love = Song(z, "Thing Called Love"); var go = Song(z, "You Got To Go"); life.sunMoonSong = sm;
            var ar = new PlaceFrame(z.At(-4f, 0), fr); for (var k = 0; k <= 16; k++) { var a = Mathf.PI * k / 16f; z.mk.Box(stone, ar.P(Mathf.Cos(a) * 3.4f, Mathf.Sin(a) * 4f, 0), new Vector3(.7f, .7f, .7f), ar.r * Quaternion.Euler(0, 0, a * Mathf.Rad2Deg + 90f)); } foreach (var e in new[] { -3.4f, 3.4f }) ArtistBits.PostCollider(z.root, ar.P(e, 0, 0), 1f, .5f);
            z.mk.Prism(SongGlow(sm, Gold, 1.6f), ar.P(-1.2f, 2.4f, .1f), ar.Fwd, .9f, .1f, 22); z.mk.Prism(GlowM(new Color(.8f, .86f, 1f), 1.4f), ar.P(1.2f, 2.4f, .1f), ar.Fwd, .8f, .1f, 22); z.mk.Prism(stone, ar.P(1.5f, 2.6f, .15f), ar.Fwd, .7f, .02f, 22); halos.Add(ar.P(0, 2.4f, 1f), 3f, Gold, .3f);
            var hm = new MeshKit(); var hmat = SongGlow(love, new Color(1f, .4f, .6f), 1.4f); hm.Blob(hmat, new Vector3(-.3f, .2f, 0), Vector3.one * .4f, rng, 0f); hm.Blob(hmat, new Vector3(.3f, .2f, 0), Vector3.one * .4f, rng, 0f); hm.Prism(hmat, new Vector3(0, .1f, 0), Vector3.down, .56f, .7f, 4, .02f, 45f); var hg = hm.BuildSingle("Heart of light", z.root, false); hg.transform.SetPositionAndRotation(z.At(2f, 5f) + up * 2f, rot); life.hearts.Add(hg.transform); z.mk.Prism(iron, z.At(2f, 5f), up, .04f, 1.4f, 5); ArtistBits.PostCollider(z.root, z.At(2f, 5f), 1.4f, .1f);
            var rs = z.At(2f, -5f); z.mk.Prism(iron, rs, up, .05f, 2.4f, 5); z.mk.Box(SongGlow(go, Gold, 1f), rs + up * 2.2f - sd * .5f, new Vector3(1.2f, .3f, .04f), Quaternion.LookRotation(fr)); z.mk.Tri(SongGlow(go, Gold, 1f), rs + up * 2.2f - sd * 1.3f, rs + up * 2.45f - sd * 1.05f, rs + up * 1.95f - sd * 1.05f, rs + fr); ArtistBits.PostCollider(z.root, rs, 2.4f, .1f);
            Folk(z.At(4f, 1f), -fr, Resident.Pose.Wave, new[] { "Sun and moon, side by side. That's the whole thing.", "Group therapy, every week, right here.", "When the sun comes up, everyone cries a bit. It's allowed." }, Local());
            z.area.glowLight = ArtistBits.PointLight(z.root, z.c + up * 4f, Gold, 22f, 1.4f);
        }

        // ---------------------------------------------------------------- everyone else
        void Others(Plot z, PlaceFrame f)
        {
            var up = Vector3.up; var rot = z.Rot; var rng = new System.Random(z.a.name.Length * 7 + z.a.name[0]); var c = z.At(1f, -1f) + up * .1f; var fr = z.front; var sd = z.side;
            void Post(Vector3 at, float h, float r = .04f) { z.mk.Prism(iron, at - up * .2f, up, r, h + .2f, 5); ArtistBits.PostCollider(z.root, at, h, r + .03f); }
            string Any() => SongIds.TryGetValue(z.a.name, out var l) && l.Length > 0 ? Song(z, l[0].title) : null;
            switch (z.a.name)
            {
                case "Darude": { var s = Any(); life.sandSong = s; var tm = new MeshKit(); for (var k = 0; k < 10; k++) tm.Prism(k == 0 ? SongGlow(s, new Color(1f, .8f, .5f), .8f) : Paint(new Color(.86f, .7f, .5f)), Vector3.up * k * .4f + new Vector3(Mathf.Sin(k * .7f), 0, Mathf.Cos(k * .9f)) * .1f * k, Vector3.up, .2f + k * .14f, .3f, 10, 1f, k * 20f); var tg = tm.BuildSingle("Sand tornado", z.root, false); tg.transform.position = c; life.twisters.Add(tg.transform); ArtistBits.PostCollider(z.root, c, 4f, .8f); break; }
                case "Francesco Farfa": { var s = Any(); Post(c, 1f, .06f); z.mk.Blob(Paint(new Color(.3f, .5f, .8f)), c + up * 1.8f, Vector3.one * .6f, rng, 0f); for (var k = 0; k < 6; k++) { var hp = c + up * 1.8f + Quaternion.Euler(0, k * 60f, 15f) * Vector3.forward * 1f; z.mk.Blob(k == 0 ? SongGlow(s, new Color(1f, .4f, .6f), 1.4f) : GlowM(new Color(1f, .4f, .6f), 1f), hp, Vector3.one * .12f, rng, 0f); } break; }
                case "Gesaffelstein": { var s = Any(); life.gloryHateSong = s; z.mk.Prism(dark, c, up, .7f, 5f, 4, .1f, 45f); for (var k = 0; k < 3; k++) z.mk.Prism(k == 1 ? SongGlow(s, Gold, 1f) : GlowM(Gold, .6f), c + up * (1.2f + k * 1.2f), up, .62f - k * .14f, .1f, 4, 1f, 45f); ArtistBits.PostCollider(z.root, c, 5f, .7f); break; }
                case "Myon": { var s = Any(); life.rainSong = s; z.mk.Box(stone, c + up * .9f, new Vector3(.4f, 1.8f, .3f), rot); z.mk.Blob(stone, c + up * 2f, Vector3.one * .18f, rng, 0f); BoxCol(z.root, c + up * 1f, new Vector3(.4f, 2f, .3f), rot); for (var k = 0; k < 4; k++) z.mk.Blob(SongGlow(k == 0 ? s : null, new Color(.6f, .64f, .74f), .5f), c + up * 3.6f + sd * (k - 1.5f) * .5f, new Vector3(.6f, .4f, .5f), rng, .2f); Post(c - fr * .8f, 3.4f, .03f); life.rainSpots.Add(c + up * 3.2f); break; }
                case "Primer": { var s = Any(); z.mk.Prism(stone, c, up, .5f, 2.4f, 10); for (var k = 0; k < 5; k++) z.mk.Blob(stone, c + Quaternion.Euler(0, k * 72f, 0) * fr * (1f + k * .2f) + up * .2f, new Vector3(.4f, .3f, .3f), rng, .3f); z.mk.Box(SongGlow(s, Gold, 1f), c + up * 2.44f, new Vector3(.8f, .1f, .8f), rot * Quaternion.Euler(8f, 0, 6f)); ArtistBits.PostCollider(z.root, c, 2.4f, .5f); break; }
                case "Robert Miles": { var s = Any(); life.childrenSong = s; for (var k = 0; k < 8; k++) { var cp = c + Quaternion.Euler(0, k * 45f, 0) * fr * 1.6f; var cf = (c - cp).normalized; z.mk.Box(stone, cp + up * .5f, new Vector3(.3f, 1f, .2f), Quaternion.LookRotation(cf)); z.mk.Blob(stone, cp + up * 1.15f, Vector3.one * .14f, rng, 0f); z.mk.Box(stone, cp + up * .85f + Vector3.Cross(up, cf) * .35f, new Vector3(.5f, .07f, .07f), Quaternion.LookRotation(Vector3.Cross(up, cf))); ArtistBits.PostCollider(z.root, cp, 1.2f, .2f); } z.mk.Prism(SongGlow(s, new Color(.8f, .86f, 1f), 1f), c + up * .05f, up, .8f, .02f, 16); break; }
                case "Soulero": { var s = Any(); var am = new MeshKit(); am.Blob(SongGlow(s, Violet, 1f), Vector3.zero, Vector3.one * .6f, rng, 0f); for (var k = 0; k < 3; k++) am.Prism(GlowM(Violet, .5f), Vector3.zero, Quaternion.Euler(k * 60f, 0, 0) * Vector3.up, 1f + k * .2f, .02f, 24); var ag = am.BuildSingle("Aura", z.root, false); ag.transform.position = c + up * 1.8f; life.spinners.Add((ag.transform, c + up * 1.8f)); Post(c, 1f, .05f); break; }
                case "Velvetine": { var s = Any(); foreach (var e in new[] { -1f, 1f }) { z.mk.Blob(stone, c + sd * e * .9f + up * 1f, new Vector3(.8f, 1.2f, 1f), rng, .2f); } z.mk.Box(SongGlow(s, White, 1.4f), c + up * 1.2f, new Vector3(.1f, 2f, .8f), rot); halos.Add(c + up * 1.2f + fr * .5f, 2f, White, .4f); BoxCol(z.root, c + up * 1f, new Vector3(2.6f, 2f, 1.2f), rot); break; }
                case "Zombie Nation": { var s = Any(); life.kernSong = s; z.mk.Prism(Paint(new Color(.7f, .7f, .7f)), c, up, 1.2f, 3f, 16, .7f); z.mk.Prism(Paint(new Color(.7f, .7f, .7f)), c + up * 3f, up, .84f, .8f, 16, 1.2f); z.mk.Prism(SongGlow(s, Laser, 1.4f), c + up * 3.8f, up, .9f, .02f, 16); ArtistBits.PostCollider(z.root, c, 3.8f, 1.2f); life.steam.Add(c + up * 3.9f); break; }
                case "zts": { var s = Any(); z.mk.Box(stone, c + up * .3f, new Vector3(.8f, .6f, .8f), rot); z.mk.Box(SongGlow(s, Gold, 1.4f), c + up * 1.6f, new Vector3(.12f, 2f, .4f), rot * Quaternion.Euler(0, 0, 8f)); BoxCol(z.root, c + up * 1f, new Vector3(.8f, 2f, .8f), rot); break; }
                default: { var s = Any(); Post(c, 1.4f); z.mk.Blob(SongGlow(s, Gold, 1.4f), c + up * 1.5f, Vector3.one * .16f, rng, 0f); break; }
            }
        }
    }
}
