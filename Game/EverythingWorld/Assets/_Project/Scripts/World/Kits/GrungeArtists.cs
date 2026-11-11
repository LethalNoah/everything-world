using System.Collections.Generic;
using UnityEngine;

namespace EverythingWorld.World
{
    /// The Grunge Rainwood's artists, each a clearing off the logging roads with covers on a plank board under a little
    /// roof, a plaque and a road lamp. Every song has its own light (GrungeSongs).
    /// - The Smashing Pumpkins: an old glass conservatory glowing gold among the pines, a patch of glowing pumpkins, a
    ///   timber bell frame with three bells ("Disarm"), a tall pole crowned with a ring and a star ("Zero"), a stone
    ///   cherub on a boulder ("Cherub Rock"), and a crescent moon and stars hung on wires across the clearing ("Empires").
    /// - Soundgarden: a field of sunflowers bowed in the rain round a tall pole with a black sun and its burning rim
    ///   ("Black Hole Sun"), a frame hung with spoons that glint ("Spoonman"), a dead tree on a patch of cracked dry
    ///   ground with a lamp in its hand ("Burden In My Hand"), a rope swing ("Pretty Noose").
    /// - Audioslave: a stretch of empty highway running off into the trees under one tall lamp ("I Am the Highway"), a
    ///   little stone house in a ring of standing stones with one window lit ("Like a Stone"), a fire bowl
    ///   ("Doesn't Remind Me"). Temple Of The Dog: a small stone temple with a dog at its steps ("Say Hello 2 Heaven").
    /// - Stone Temple Pilots: an old car parked on a short piece of interstate with a road sign ("Interstate Love Song"),
    ///   an empty ring of lamps round one chair ("Big Empty"), a big red paper heart with a hole in it ("Trippin'...").
    /// - Live: a white farmhouse porch with a lightning rod on a tall mast ("Lightning Crashes"), one chair under one
    ///   lamp ("I Alone"), a porch swing ("All Over You").
    /// - Nirvana: a giant heart-shaped box with its lid ajar, glowing inside ("Heart-Shaped Box"), a rusted trailer
    ///   ("Lithium"), a low dark stage of candles and white lilies with a stool ("Where Did You Sleep Last Night").
    /// - Foo Fighters: a little silver saucer half sunk in the moss, rim lights glowing ("Everlong"), a flare
    ///   ("My Hero"), a mailbox with its flag up ("Sean").
    /// - Alice In Chains: a hut shaped like a cracked nutshell with a candle inside ("Nutshell"), a dead tree hung with
    ///   chains round a single lamp ("Would?"). Pearl Jam: a brook under a footbridge, the water glowing ("Even Flow"),
    ///   a slate on an easel with mountains drawn in chalk ("Jeremy").
    /// - The rest: Bush a machine head and a raked gravel square; Blind Melon a patch of sunlight and a bee; Dinosaur Jr.
    ///   a dinosaur skeleton; Melvins a bucket of glowing honey; Screaming Trees a tree with a face; Silverchair a silver
    ///   chair; Superheaven a swing set; Kurt Cobain a stool and a guitar under a bulb; Keo and Rehash a trailer each.
    public partial class GrungeKit
    {
        class Plot
        {
            public LandContext ctx; public Transform root, labels; public MeshKit mk, solid, pictures;
            public IslandWorldData.Artist a; public Vector3 c, front, side; public List<Renderer> glows; public ArtistArea area;
            public Vector3 At(float fw, float rt) { var p = c + front * fw + side * rt; return ctx.OnGround(p.x, p.z); }
            public Quaternion Rot => Quaternion.LookRotation(front);
        }

        public override void BuildArtist(LandContext ctx, IslandWorldData.Artist a)
        {
            if (mud == null) MakeMaterials();
            var root = new GameObject("Artist: " + a.name).transform; root.SetParent(ctx.Root, false);
            var front = facing.TryGetValue(a.name, out var f) ? f : new Vector3(center.x - a.x, 0, center.z - a.z).normalized;
            if (front.sqrMagnitude < .01f) front = Vector3.forward;
            var labels = new GameObject("Labels").transform; labels.SetParent(root, false);
            var area = root.gameObject.AddComponent<ArtistArea>();
            area.artistName = a.name; area.stationId = a.stationId; area.radius = a.r; area.glowColor = Lamp;
            var z = new Plot { ctx = ctx, root = root, labels = labels, mk = new MeshKit(), solid = new MeshKit(), pictures = new MeshKit(), a = a, c = ctx.OnGround(a.x, a.z), front = front, side = Vector3.Cross(Vector3.up, front), glows = new List<Renderer>(), area = area };
            // a clearing of trodden mud
            z.mk.Prism(mud, z.c - Vector3.up * .28f, Vector3.up, Foot(a) * .55f, .33f, 16);
            switch (a.name)
            {
                case "The Smashing Pumpkins": Pumpkins(z); break;
                case "Soundgarden": Soundgarden(z); break;
                case "Audioslave": Audioslave(z); break;
                case "Temple Of The Dog": Temple(z); break;
                case "Stone Temple Pilots": STP(z); break;
                case "Live": LiveFarm(z); break;
                case "Nirvana": Nirvana(z); break;
                case "Foo Fighters": Foo(z); break;
                case "Alice In Chains": Alice(z); break;
                case "Pearl Jam": PearlJam(z); break;
                case "Bush": Bush(z); break;
                case "Blind Melon": BlindMelon(z); break;
                case "Dinosaur Jr.": Dino(z); break;
                case "Melvins": Honey(z); break;
                case "Screaming Trees": FaceTree(z); break;
                case "Silverchair": SilverChair(z); break;
                case "Superheaven": Swings(z); break;
                case "Kurt Cobain": Stool(z); break;
                case "Keo": Trailer(ctx, z.mk, z.solid, z.At(-3f, 0), SongGlow(ThatsMe, Lamp, 2f), z.front); break;
                case "Rehash": Trailer(ctx, z.mk, z.solid, z.At(-3f, 0), SongGlow(BackToStrangers, new Color(.6f, .8f, 1f), 2f), z.front); break;
                default: Trailer(ctx, z.mk, z.solid, z.At(-3f, 0), null, z.front); break;
            }
            var reach = Foot(a);
            Board(z, z.At(reach * .8f, -reach * .5f - 2.5f), front);
            ArtistBits.Plaque(z.mk, z.solid, labels, a, z.At(reach * .8f + 1.5f, reach * .5f + 2.5f), front, plank, oldPlank, Ink, 1.6f);
            RoadLamp(ctx, z.mk, z.At(reach * .8f, reach * .5f + 5f), -z.side);
            if (z.area.glowLight == null) z.area.glowLight = ArtistBits.PointLight(root, z.c + Vector3.up * 5f + front * 5f, Lamp, HasStation(a) ? 20f : 13f, 1.3f);
            area.glowRenderers = z.glows.ToArray();
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

        /// The covers on a plank board under a little shingle roof.
        void Board(Plot z, Vector3 ground, Vector3 dir)
        {
            dir = Vector3.ProjectOnPlane(dir, Vector3.up).normalized; var rot = Quaternion.LookRotation(dir); var side = Vector3.Cross(Vector3.up, dir);
            var n = Mathf.Max(1, Mathf.Min(5, z.a.songList?.Length ?? 1)); const float size = .9f; var w = n * size * 1.25f + .4f;
            foreach (var s in new[] { -1f, 1f }) { z.mk.Prism(bark, ground + side * s * (w / 2f - .1f) - Vector3.up * .3f, Vector3.up, .13f, 3.3f, 6); ArtistBits.PostCollider(z.root, ground + side * s * (w / 2f - .1f), 2.8f, .14f); }
            var c = ground + Vector3.up * 1.8f;
            z.mk.Box(oldPlank, c - Vector3.up * .1f, new Vector3(w, size + .6f, .14f), rot); z.solid.Box(oldPlank, c - Vector3.up * .1f, new Vector3(w, size + .6f, .24f), rot);
            z.mk.GableRoof(moss, c + Vector3.up * (size / 2f + .3f), new Vector3(1f, 0, w + .5f), .35f, Quaternion.LookRotation(side));
            ArtistBits.CoverWall(z.pictures, z.labels, z.ctx, z.a, c + dir * .075f + Vector3.up * .08f, dir, n, size, n, true, Ink);
        }

        // ---------------------------------------------------------------- The Smashing Pumpkins
        void Pumpkins(Plot z)
        {
            var up = Vector3.up; var rot = z.Rot; var ctx = z.ctx;
            // the conservatory: an iron-framed glass house glowing gold, a pitched glass roof, a door
            var g = z.At(-11f, 0); const float W = 11f, D = 6f, H = 4.2f;
            Vector3 P(float x, float y, float d) => g + z.side * x + up * y + z.front * d;
            z.mk.Box(concrete, P(0, .25f, 0), new Vector3(W + .4f, .5f, D + .4f), rot);
            var glass = SongGlow(Mayonaise, new Color(1f, .78f, .35f), 1.2f);
            z.mk.Box(glass, P(0, .5f + H / 2f, 0), new Vector3(W - .1f, H, D - .1f), rot);
            var frame = WorldMaterials.Solid(new Color(.22f, .26f, .24f), .6f);
            for (var k = 0; k <= 8; k++) { var x = -W / 2f + k * W / 8f; foreach (var d in new[] { -D / 2f, D / 2f }) z.mk.Box(frame, P(x, .5f + H / 2f, d), new Vector3(.1f, H, .1f), rot); }
            for (var k = 0; k <= 4; k++) { var d = -D / 2f + k * D / 4f; foreach (var x in new[] { -W / 2f, W / 2f }) z.mk.Box(frame, P(x, .5f + H / 2f, d), new Vector3(.1f, H, .1f), rot); }
            foreach (var y in new[] { .55f, .5f + H * .5f, .5f + H }) { z.mk.Box(frame, P(0, y, D / 2f), new Vector3(W, .1f, .1f), rot); z.mk.Box(frame, P(0, y, -D / 2f), new Vector3(W, .1f, .1f), rot); }
            z.mk.GableRoof(glass, P(0, .5f + H, 0), new Vector3(D + .3f, 0, W + .3f), 2.2f, Quaternion.LookRotation(z.side));
            for (var k = 0; k <= 8; k++) { var x = -W / 2f + k * W / 8f; foreach (var s in new[] { -1f, 1f }) { var a = P(x, .5f + H, s * (D / 2f + .15f)); var b = P(x, .5f + H + 2.2f, 0); z.mk.Box(frame, (a + b) / 2f, new Vector3(.08f, .08f, Vector3.Distance(a, b)), Quaternion.LookRotation(b - a)); } }
            z.mk.Box(dark, P(0, 1.6f, D / 2f + .06f), new Vector3(1.4f, 2.2f, .05f), rot);
            z.solid.Box(glass, P(0, 2.5f, 0), new Vector3(W + .4f, 5f, D + .4f), rot);
            halos.Add(P(0, 2.5f, D / 2f + 1f), 5f, new Color(1f, .78f, .35f), .3f);
            ArtistBits.PointLight(z.root, P(0, 2.5f, D / 2f + 3f), new Color(1f, .78f, .4f), 14f, 1.4f);
            // the pumpkin patch out front of the glass house
            var orange = WorldMaterials.Solid(new Color(.85f, .42f, .1f)); var lit = SongGlow(Mayonaise, new Color(1f, .55f, .12f), 2.2f); var vine = WorldMaterials.Solid(new Color(.22f, .35f, .18f));
            for (var i = 0; i < 4; i++) for (var j = 0; j < 3; j++)
            {
                var p = z.At(-5.5f + j * 1.6f + ctx.Range(-.3f, .3f), -4.5f + i * 3f + ctx.Range(-.4f, .4f)); var s = ctx.Range(.45f, .8f);
                var glowing = (i + j) % 2 == 0;
                z.mk.Blob(glowing ? lit : orange, p + up * s * .55f, new Vector3(s, s * .7f, s), ctx.Rng, .12f); z.mk.Prism(vine, p + up * s * 1.1f, up, .06f, .25f, 5);
                if (glowing) halos.Add(p + up * s * .6f, s * 1.6f, new Color(1f, .55f, .12f), .45f);
            }
            for (var k = 0; k < 3; k++) z.mk.Box(vine, z.At(-4f, -4.5f + k * 4.5f) + up * .09f, new Vector3(.12f, .06f, 3.6f), rot);
            z.solid.Box(vine, z.At(-4f, 0) + up * .5f, new Vector3(11f, 1f, 5f), rot);
            // Disarm: a timber bell frame with three bells
            var bf = z.At(-3f, -12.5f); var bronze = WorldMaterials.Solid(new Color(.5f, .38f, .2f), .75f);
            foreach (var s in new[] { -1f, 1f }) { z.mk.Box(bark, bf + z.side * s * 2.4f + up * 2.5f, new Vector3(.35f, 5f, .35f), rot); ArtistBits.PostCollider(z.root, bf + z.side * s * 2.4f, 5f, .25f); }
            z.mk.Box(bark, bf + up * 5f, new Vector3(5.4f, .4f, .4f), Quaternion.LookRotation(z.side) * Quaternion.Euler(0, 90f, 0));
            z.mk.GableRoof(moss, bf + up * 5.25f, new Vector3(1.6f, 0, 6f), .7f, Quaternion.LookRotation(z.side));
            var bellGlow = SongGlow(Disarm, new Color(1f, .85f, .5f), 2.4f);
            for (var k = 0; k < 3; k++)
            {
                var bk = new MeshKit(); var s = 1f - k * .2f;
                bk.Prism(bronze, new Vector3(0, -1.5f * s, 0), Vector3.up, .7f * s, 1.3f * s, 12, .45f); bk.Blob(bronze, new Vector3(0, -.2f * s, 0), new Vector3(.32f, .2f, .32f) * s, ctx.Rng, 0f);
                bk.Prism(bellGlow, new Vector3(0, -1.55f * s, 0), Vector3.up, .66f * s, .05f, 12);
                bk.Blob(dark, new Vector3(0, -1.5f * s, 0), Vector3.one * .15f * s, ctx.Rng, 0f);
                var bell = bk.BuildSingle("Bell", life.transform, false).transform; bell.SetPositionAndRotation(bf + up * 4.8f + z.side * (k - 1f) * 1.5f, Quaternion.LookRotation(z.front)); life.bells.Add(bell);
            }
            halos.Add(bf + up * 3.5f, 2.6f, new Color(1f, .85f, .5f), .35f);
            // Zero: a tall pole with a glowing ring and a star at the top
            var zp = z.At(-14f, 9.5f); z.mk.Prism(dark, zp - up * .3f, up, .2f, 9.8f, 8); ArtistBits.PostCollider(z.root, zp, 9.5f, .2f);
            var zero = SongGlow(Zero, new Color(.85f, .9f, 1f), 2.4f);
            for (var k = 0; k < 24; k++) { var a0 = k * Mathf.PI / 12f; var a1 = (k + 1) * Mathf.PI / 12f; var p0 = zp + up * 9.5f + (z.side * Mathf.Cos(a0) * 1.1f + up * Mathf.Sin(a0) * 1.5f); var p1 = zp + up * 9.5f + (z.side * Mathf.Cos(a1) * 1.1f + up * Mathf.Sin(a1) * 1.5f); z.mk.Box(zero, (p0 + p1) / 2f, new Vector3(.16f, .16f, Vector3.Distance(p0, p1) + .02f), Quaternion.LookRotation(p1 - p0, z.front)); }
            Star(z.mk, zp + up * 11.6f, z.front, .7f, zero); halos.Add(zp + up * 10f, 3f, new Color(.85f, .9f, 1f), .45f);
            // Cherub Rock: a stone cherub on a mossy boulder
            var cr = z.At(-1f, 11f); var stone = WorldMaterials.Solid(new Color(.62f, .62f, .6f));
            z.mk.Blob(rock, cr + up * .8f, new Vector3(1.8f, 1.1f, 1.6f), ctx.Rng, .3f); z.mk.Blob(moss, cr + up * 1.6f, new Vector3(1.3f, .3f, 1.2f), ctx.Rng, .4f); z.solid.Box(rock, cr + up * .9f, new Vector3(3f, 1.8f, 2.8f), rot);
            var ch = cr + up * 1.8f; z.mk.Blob(stone, ch + up * .45f, new Vector3(.35f, .5f, .3f), ctx.Rng, .05f); z.mk.Blob(stone, ch + up * 1.1f, Vector3.one * .26f, ctx.Rng, 0f);
            foreach (var s in new[] { -1f, 1f }) z.mk.Box(SongGlow(CherubRock, new Color(1f, .95f, .85f), 1.8f), ch + up * .8f + z.side * s * .45f - z.front * .1f, new Vector3(.5f, .5f, .05f), rot * Quaternion.Euler(0, s * 30f, s * -25f));
            halos.Add(ch + up * .8f, 1.6f, new Color(1f, .95f, .85f), .45f);
            // Empires: a crescent moon and stars hung on wires across the clearing
            var wireA = z.At(-6f, -15f) + up * 7.5f; var wireB = z.At(-6f, 15f) + up * 7.5f;
            foreach (var e in new[] { wireA, wireB }) { var gnd = new Vector3(e.x, z.ctx.Ground(e.x, e.z), e.z); z.mk.Prism(bark, gnd - up * .3f, up, .2f, e.y - gnd.y + .5f, 6); ArtistBits.PostCollider(z.root, gnd, e.y - gnd.y, .2f); }
            z.mk.Box(dark, (wireA + wireB) / 2f - up * .9f, new Vector3(.03f, .03f, Vector3.Distance(wireA, wireB)), Quaternion.LookRotation(wireB - wireA));
            var empires = SongGlow(Empires, new Color(.8f, .85f, 1f), 2.6f);
            var mc = Vector3.Lerp(wireA, wireB, .5f) - up * 2.2f;
            for (var k = 0; k <= 12; k++) { var a = Mathf.Lerp(-1.2f, 1.2f, k / 12f); var r = 1.3f; var w = .35f * Mathf.Cos(a * 1.1f); z.mk.Blob(empires, mc + (up * Mathf.Sin(a) * r + z.side * (Mathf.Cos(a) * r - .5f)), new Vector3(w, w, .12f), ctx.Rng, 0f); }
            z.mk.Box(dark, mc + up * 1.3f, new Vector3(.02f, .9f, .02f), Quaternion.identity);
            for (var k = 0; k < 9; k++) { var t = (k + .5f) / 9f; if (Mathf.Abs(t - .5f) < .08f) continue; var sp = Vector3.Lerp(wireA, wireB, t) - up * (.9f + Mathf.Sin(t * Mathf.PI) * .5f + (k % 3) * .6f); z.mk.Box(dark, sp + up * ((k % 3) * .3f + .3f), new Vector3(.02f, (k % 3) * .6f + .5f, .02f), Quaternion.identity); Star(z.mk, sp, z.front, .3f, empires); }
            halos.Add(mc, 3f, new Color(.8f, .85f, 1f), .5f);
            noGrass.Add(new Vector3(g.x, 9f, g.z));
        }

        /// A flat five-pointed star facing 'face'.
        void Star(MeshKit mk, Vector3 c, Vector3 face, float r, Material m)
        {
            var up = Vector3.up; var side = Vector3.Cross(up, face).normalized;
            for (var k = 0; k < 5; k++)
            {
                float a0 = k * Mathf.PI * 2f / 5f + Mathf.PI / 2f, a1 = a0 + Mathf.PI / 5f, a2 = a0 - Mathf.PI / 5f;
                Vector3 V(float a, float rr) => c + (side * Mathf.Cos(a) + up * Mathf.Sin(a)) * rr;
                mk.Tri(m, V(a0, r), V(a1, r * .42f), V(a2, r * .42f), c - face);
                mk.Tri(m, V(a0, r), V(a2, r * .42f), V(a1, r * .42f), c + face);
                mk.Tri(m, c, V(a1, r * .42f), V(a2, r * .42f), c - face); mk.Tri(m, c, V(a2, r * .42f), V(a1, r * .42f), c + face);
            }
        }

        // ---------------------------------------------------------------- Soundgarden
        void Soundgarden(Plot z)
        {
            var up = Vector3.up; var rot = z.Rot; var ctx = z.ctx;
            // the black sun on a tall pole in a field of bowed sunflowers
            var sp = z.At(-9f, -2f); z.mk.Prism(dark, sp - up * .3f, up, .25f, 11.3f, 8); ArtistBits.PostCollider(z.root, sp, 11f, .25f);
            var sun = sp + up * 11.5f;
            z.mk.Prism(dark, sun - z.front * .2f, z.front, 2f, .4f, 24);
            var rim = SongGlow(BlackHoleSun, new Color(1f, .55f, .15f), 2.6f);
            for (var k = 0; k < 32; k++) { var a0 = k * Mathf.PI / 16f; var a1 = (k + 1) * Mathf.PI / 16f; var p0 = sun + (z.side * Mathf.Cos(a0) + up * Mathf.Sin(a0)) * 2.15f; var p1 = sun + (z.side * Mathf.Cos(a1) + up * Mathf.Sin(a1)) * 2.15f; z.mk.Box(rim, (p0 + p1) / 2f, new Vector3(.3f, .3f, Vector3.Distance(p0, p1) + .02f), Quaternion.LookRotation(p1 - p0, z.front)); }
            for (var k = 0; k < 16; k++) z.mk.Cone(rim, sun + Quaternion.AngleAxis(k * 22.5f, z.front) * up * 2.3f, .25f, .9f + (k % 2) * .6f, 4);
            halos.Add(sun + z.front * .6f, 4.5f, new Color(1f, .55f, .15f), .55f);
            ArtistBits.PointLight(z.root, sun + z.front * 4f, new Color(1f, .55f, .2f), 18f, 1.4f);
            var stalk = WorldMaterials.Solid(new Color(.25f, .38f, .2f)); var petal = WorldMaterials.Solid(new Color(.95f, .75f, .15f)); var seed = WorldMaterials.Solid(new Color(.2f, .12f, .06f));
            for (var i = 0; i < 26; i++)
            {
                var a = i * 2.399f; var r = 2.5f + Mathf.Sqrt(i) * 1.5f; var p = ctx.OnGround(sp.x + Mathf.Cos(a) * r, sp.z + Mathf.Sin(a) * r);
                if (Vector3.Dot(p - z.c, z.front) > 2f) continue;
                var h = ctx.Range(1.6f, 2.6f); var bow = (new Vector3(sp.x - p.x, 0, sp.z - p.z).normalized + Vector3.down * .4f).normalized;
                z.mk.Prism(stalk, p, up, .05f, h, 5); var head = p + up * h + bow * .15f;
                z.mk.Prism(petal, head, bow, .38f, .05f, 10); z.mk.Prism(seed, head + bow * .05f, bow, .22f, .06f, 8);
            }
            // Spoonman: a frame hung with spoons that glint
            var sf = z.At(-1f, -10f); foreach (var s in new[] { -1f, 1f }) { z.mk.Box(bark, sf + z.side * s * 1.6f + up * 1.5f, new Vector3(.2f, 3f, .2f), rot); ArtistBits.PostCollider(z.root, sf + z.side * s * 1.6f, 3f, .15f); }
            z.mk.Box(bark, sf + up * 3f, new Vector3(3.6f, .2f, .2f), rot);
            var spoon = SongGlow(Spoonman, new Color(.85f, .9f, 1f), 1.6f);
            for (var k = 0; k < 9; k++) { var x = (k - 4f) * .36f; var l = .5f + (k % 3) * .25f; var p = sf + z.side * x + up * (3f - l); z.mk.Box(dark, p + up * l / 2f, new Vector3(.01f, l, .01f), rot); z.mk.Box(spoon, p - up * .2f, new Vector3(.04f, .4f, .02f), rot); z.mk.Blob(spoon, p - up * .5f, new Vector3(.08f, .12f, .03f), ctx.Rng, 0f); }
            halos.Add(sf + up * 2.2f, 1.8f, new Color(.85f, .9f, 1f), .35f);
            // Burden In My Hand: a dead tree on a patch of cracked dry ground, holding up a lamp
            var bt = z.At(-2f, 10f); var dry = WorldMaterials.Solid(new Color(.55f, .45f, .32f));
            z.mk.Prism(dry, bt - up * .26f, up, 3.2f, .35f, 12);
            for (var k = 0; k < 9; k++) { var a = k * .7f; z.mk.Box(dark, bt + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * ctx.Range(.8f, 2.4f) + up * .1f, new Vector3(.05f, .02f, ctx.Range(.6f, 1.4f)), Quaternion.Euler(0, a * 57f, 0)); }
            var deadWood = WorldMaterials.Solid(new Color(.4f, .36f, .32f)); z.mk.Prism(deadWood, bt, up, .3f, 4f, 6, .6f); ArtistBits.PostCollider(z.root, bt, 4f, .3f);
            var arm = (z.side * -.5f + up + z.front * .3f).normalized; z.mk.Prism(deadWood, bt + up * 3.2f, arm, .14f, 2f, 5, .5f);
            foreach (var d in new[] { (z.side + up * .8f).normalized, (-z.front + up).normalized }) z.mk.Prism(deadWood, bt + up * 2.4f, d, .1f, 1.6f, 5, .4f);
            var lampAt = bt + up * 3.2f + arm * 2.1f; z.mk.Box(dark, lampAt + up * .35f, new Vector3(.02f, .5f, .02f), Quaternion.identity);
            z.mk.Box(SongGlow(BurdenInMyHand, Lamp, 2.6f), lampAt, new Vector3(.25f, .35f, .25f), rot); halos.Add(lampAt, 1.6f, Lamp, .7f);
            // Pretty Noose: a rope swing from a big pine bough
            var rs = z.At(3f, -14f); Pine(ctx, rs - z.side * 2f, 1f);
            var bough = rs + up * 6f; z.mk.Prism(bark, rs - z.side * 2f + up * 6f, z.side, .2f, 3f, 6);
            foreach (var s in new[] { -.3f, .3f }) z.mk.Box(WorldMaterials.Solid(new Color(.6f, .5f, .35f)), rs + z.front * s + up * 3.8f, new Vector3(.04f, 4.4f, .04f), Quaternion.identity);
            z.mk.Box(SongGlow(PrettyNoose, new Color(.95f, .3f, .25f), 1.8f), rs + up * 1.6f, new Vector3(.4f, .08f, .8f), rot); halos.Add(rs + up * 1.7f, 1.2f, new Color(.95f, .3f, .25f), .4f);
            noGrass.Add(new Vector3(sp.x, 10f, sp.z));
        }

        // ---------------------------------------------------------------- Audioslave and Temple of the Dog
        void Audioslave(Plot z)
        {
            var up = Vector3.up; var rot = z.Rot; var ctx = z.ctx;
            // I Am the Highway: a stretch of empty two-lane road running off into the trees, one tall lamp over it
            var asphalt = WorldMaterials.Solid(new Color(.12f, .13f, .14f), .7f); var yellow = SongGlow(Highway, new Color(1f, .8f, .2f), 1.4f);
            var road = new List<Vector3>(); for (var k = 0; k <= 16; k++) road.Add(z.At(-4f - k * 1.6f, -9f + Mathf.Sin(k * .25f) * 1.5f));
            Strip(z.mk, road, asphalt, 5f, .1f, ctx);
            for (var k = 0; k < 16; k += 2) { var a = road[k]; var b = road[k + 1]; z.mk.Box(yellow, (a + b) / 2f + up * .13f, new Vector3(.15f, .02f, Vector3.Distance(a, b) * .8f), Quaternion.LookRotation(b - a)); }
            foreach (var k in new[] { 0, 16 }) foreach (var s in new[] { -2.4f, 2.4f }) { var e = road[k] + z.side * s; z.mk.Box(WorldMaterials.Solid(new Color(.85f, .85f, .8f)), e + up * .13f, new Vector3(.12f, .02f, 1.4f), rot); }
            var hl = road[9] + z.side * 3.4f; hl.y = ctx.Ground(hl.x, hl.z); z.mk.Prism(metal, hl - up * .3f, up, .14f, 8.3f, 8); ArtistBits.PostCollider(z.root, hl, 8f, .15f);
            z.mk.Box(metal, hl + up * 7.9f - z.side * 1.2f, new Vector3(2.6f, .1f, .1f), rot);
            var hlLamp = hl + up * 7.75f - z.side * 2.4f; z.mk.Box(SongGlow(Highway, new Color(1f, .75f, .35f), 3f), hlLamp, new Vector3(.8f, .12f, .4f), rot); halos.Add(hlLamp - up * .3f, 2.6f, new Color(1f, .75f, .35f), .8f);
            ArtistBits.PointLight(z.root, hlLamp - up * 1f, new Color(1f, .75f, .35f), 14f, 1.6f);
            // Like a Stone: a ring of standing stones round a little stone house with one window lit
            var hc = z.At(-6f, 6f); var stone = WorldMaterials.Solid(new Color(.45f, .46f, .45f));
            z.mk.Box(stone, hc + up * 1.5f, new Vector3(3.6f, 3f, 3f), rot); z.solid.Box(stone, hc + up * 1.5f, new Vector3(3.6f, 3f, 3f), rot);
            z.mk.GableRoof(moss, hc + up * 3f, new Vector3(3.6f, 0, 4.2f), 1.4f, Quaternion.LookRotation(z.side));
            z.mk.Box(SongGlow(LikeAStone, Lamp, 2.4f), hc + up * 1.7f + z.front * 1.51f + z.side * .8f, new Vector3(.7f, .7f, .02f), rot); halos.Add(hc + up * 1.7f + z.front * 1.9f + z.side * .8f, 1.4f, Lamp, .7f);
            z.mk.Box(dark, hc + up * 1f + z.front * 1.51f - z.side * .7f, new Vector3(.8f, 1.8f, .02f), rot);
            z.mk.Prism(stone, hc + up * 3f + z.side * 1.2f, up, .3f, 1.4f, 6);
            for (var k = 0; k < 9; k++) { var a = k * Mathf.PI * 2f / 9f; var p = ctx.OnGround(hc.x + Mathf.Cos(a) * 5.6f, hc.z + Mathf.Sin(a) * 5.6f); if (Vector3.Dot(p - hc, z.front) > 4.8f) continue; var h = ctx.Range(1.8f, 2.8f); z.mk.Box(stone, p + up * h / 2f, new Vector3(.9f, h, .6f), Quaternion.LookRotation(p - hc) * Quaternion.Euler(0, 0, ctx.Range(-5f, 5f))); z.solid.Box(stone, p + up * h / 2f, new Vector3(.9f, h, .6f), Quaternion.LookRotation(p - hc)); }
            // Doesn't Remind Me: an iron fire bowl on a stone plinth
            var fb = z.At(1.5f, 1f); z.mk.Box(stone, fb + up * .4f, new Vector3(1.2f, .8f, 1.2f), rot); z.mk.Prism(dark, fb + up * .8f, up, .8f, .4f, 10, 1.3f);
            z.mk.Blob(SongGlow(DoesntRemind, new Color(1f, .5f, .15f), 3f), fb + up * 1.25f, new Vector3(.7f, .35f, .7f), ctx.Rng, .3f); halos.Add(fb + up * 1.6f, 1.8f, new Color(1f, .5f, .15f), .8f); life.fires.Add(fb + up * 1.4f);
            z.solid.Box(stone, fb + up * .6f, new Vector3(1.4f, 1.2f, 1.4f), rot);
        }

        void Temple(Plot z)
        {
            var up = Vector3.up; var rot = z.Rot; var ctx = z.ctx; var stone = WorldMaterials.Solid(new Color(.62f, .62f, .6f));
            var t = z.At(-4f, 0);
            for (var k = 0; k < 3; k++) z.mk.Box(stone, t + up * (k * .25f + .12f) + z.front * (1.6f - k * .35f), new Vector3(5f - k * .3f, .25f, 1f), rot);
            z.mk.Box(stone, t + up * .4f - z.front * .5f, new Vector3(5f, .8f, 3.6f), rot);
            foreach (var s in new[] { -1.8f, -.6f, .6f, 1.8f }) { z.mk.Prism(stone, t + z.side * s + z.front * .9f + up * .8f, up, .22f, 3f, 10); z.mk.Prism(stone, t + z.side * s - z.front * 1.8f + up * .8f, up, .22f, 3f, 10); }
            z.mk.Box(stone, t + up * 3.95f - z.front * .45f, new Vector3(4.8f, .35f, 3.4f), rot);
            z.mk.GableRoof(stone, t + up * 4.1f - z.front * .45f, new Vector3(3.6f, 0, 5f), 1f, Quaternion.LookRotation(z.side));
            z.solid.Box(stone, t + up * 2f - z.front * .5f, new Vector3(5f, 4f, 3.6f), rot);
            var heaven = SongGlow(SayHello2Heaven, new Color(.85f, .9f, 1f), 2.4f);
            z.mk.Box(heaven, t + up * 2.2f - z.front * 1.5f, new Vector3(3f, 2.4f, .05f), rot); halos.Add(t + up * 2.2f - z.front * .6f, 2.6f, new Color(.85f, .9f, 1f), .45f);
            life.heaven = t + up * 4.5f;
            for (var k = 0; k < 5; k++) z.mk.Blob(moss, t + up * (4.3f + (k % 2) * .3f) - z.front * .45f + z.side * (k - 2f) * 1f, new Vector3(.7f, .2f, .9f), ctx.Rng, .4f);
            foreach (var s in new[] { -1f, 1f }) { var lp = t + z.side * s * 3.2f + z.front * 1.8f; lp.y = ctx.Ground(lp.x, lp.z); z.mk.Box(stone, lp + up * .5f, new Vector3(.4f, 1f, .4f), rot); z.mk.Box(stone, lp + up * 1.1f, new Vector3(.6f, .2f, .6f), rot); z.mk.Blob(heaven, lp + up * 1.35f, new Vector3(.18f, .22f, .18f), ctx.Rng, 0f); halos.Add(lp + up * 1.4f, 1.2f, new Color(.85f, .9f, 1f), .5f); ArtistBits.PostCollider(z.root, lp, 1.2f, .35f); }
            // the dog at the steps
            var dg = z.At(-1.6f, 2.8f); z.mk.Box(stone, dg + up * .3f, new Vector3(.7f, .6f, 1f), rot);
            var dc = dg + up * .6f; z.mk.Blob(stone, dc + up * .35f, new Vector3(.32f, .45f, .5f), ctx.Rng, .05f); z.mk.Blob(stone, dc + up * .9f + z.front * .25f, new Vector3(.25f, .25f, .3f), ctx.Rng, .05f);
            z.mk.Box(stone, dc + up * .82f + z.front * .5f, new Vector3(.14f, .12f, .25f), rot); foreach (var s in new[] { -1f, 1f }) z.mk.Cone(stone, dc + up * 1.05f + z.side * s * .14f + z.front * .2f, .07f, .22f, 4);
            z.solid.Box(stone, dg + up * .7f, new Vector3(.8f, 1.4f, 1.1f), rot);
            ArtistBits.PointLight(z.root, t + up * 3f + z.front * 3f, new Color(.85f, .9f, 1f), 12f, 1.2f);
        }

        // ---------------------------------------------------------------- Stone Temple Pilots
        void STP(Plot z)
        {
            var up = Vector3.up; var rot = z.Rot; var ctx = z.ctx;
            // Interstate Love Song: an old sedan parked on a short piece of interstate with a road sign and a mile marker
            var asphalt = WorldMaterials.Solid(new Color(.15f, .15f, .16f), .7f);
            var road = new List<Vector3>(); for (var k = 0; k <= 10; k++) road.Add(z.At(-8f, -12f + k * 2f));
            Strip(z.mk, road, asphalt, 6f, .1f, ctx);
            var line = WorldMaterials.Solid(new Color(.9f, .9f, .85f)); for (var k = 0; k < 10; k += 2) z.mk.Box(line, (road[k] + road[k + 1]) / 2f + up * .13f, new Vector3(.15f, .02f, 1.4f), Quaternion.LookRotation(z.side));
            var car = z.At(-8f, -5f); var dir = z.side; var crot = Quaternion.LookRotation(dir); var paint = WorldMaterials.Solid(new Color(.3f, .4f, .55f), .5f);
            z.mk.Box(paint, car + up * .75f, new Vector3(2f, .7f, 5f), crot); z.mk.Box(paint, car + up * 1.4f - dir * .3f, new Vector3(1.8f, .65f, 2.6f), crot);
            z.mk.Box(WorldMaterials.Solid(new Color(.2f, .25f, .3f), .95f), car + up * 1.4f - dir * .3f, new Vector3(1.82f, .45f, 2.4f), crot);
            foreach (var s in new[] { -1f, 1f }) foreach (var w in new[] { -1.6f, 1.6f }) z.mk.Prism(dark, car + z.front * s * .95f + dir * w + up * .4f, z.front * s, .4f, .25f, 12);
            var headlight = SongGlow(InterstateLoveSong, new Color(1f, .95f, .75f), 3f); var tail = SongGlow(InterstateLoveSong, new Color(1f, .15f, .1f), 2.2f);
            foreach (var s in new[] { -.65f, .65f }) { z.mk.Prism(headlight, car + up * .85f + dir * 2.5f + z.front * s, dir, .16f, .05f, 10); z.mk.Box(tail, car + up * .9f - dir * 2.51f + z.front * s, new Vector3(.3f, .12f, .02f), crot); }
            halos.Add(car + up * .85f + dir * 3f, 1.8f, new Color(1f, .95f, .75f), .5f);
            z.solid.Box(paint, car + up * 1f, new Vector3(2.1f, 2f, 5.1f), crot);
            var sg = road[10] + z.front * 3.6f; sg.y = ctx.Ground(sg.x, sg.z); var green = WorldMaterials.Solid(new Color(.1f, .4f, .25f));
            foreach (var s in new[] { -1f, 1f }) { z.mk.Prism(metal, sg + z.side * s * 1.2f, up, .08f, 3.4f, 6); ArtistBits.PostCollider(z.root, sg + z.side * s * 1.2f, 3.4f, .1f); }
            z.mk.Box(green, sg + up * 3.2f, new Vector3(3.2f, 1.3f, .1f), Quaternion.LookRotation(z.side) * Quaternion.Euler(0, -90f, 0));
            ArtistBits.Label(z.labels, "LOVE  1 MI", sg + up * 3.2f + z.front * .07f, z.front, .26f, Color.white, TextAnchor.MiddleCenter);
            // Big Empty: a wide empty ring of lamps round one chair
            var be = z.At(-3f, 8f); var lampM = SongGlow(BigEmpty, new Color(.75f, .85f, 1f), 2.4f);
            for (var k = 0; k < 7; k++) { var a = k * Mathf.PI * 2f / 7f; var p = ctx.OnGround(be.x + Mathf.Cos(a) * 4.6f, be.z + Mathf.Sin(a) * 4.6f); z.mk.Prism(dark, p - up * .2f, up, .06f, 1.6f, 6); z.mk.Blob(lampM, p + up * 1.5f, Vector3.one * .14f, ctx.Rng, 0f); halos.Add(p + up * 1.5f, .9f, new Color(.75f, .85f, 1f), .5f); ArtistBits.PostCollider(z.root, p, 1.5f, .1f); }
            Chair(z, be, z.front);
            // Trippin' on a Hole in a Paper Heart: a big red paper heart with a hole in it, hung between two posts
            var ph = z.At(-12f, 7f); foreach (var s in new[] { -1f, 1f }) { z.mk.Prism(bark, ph + z.side * s * 2.4f - up * .3f, up, .15f, 5.3f, 6); ArtistBits.PostCollider(z.root, ph + z.side * s * 2.4f, 5f, .15f); }
            z.mk.Box(dark, ph + up * 4.9f, new Vector3(4.8f, .04f, .04f), Quaternion.LookRotation(z.side) * Quaternion.Euler(0, 90f, 0));
            var paper = SongGlow(PaperHeart, new Color(1f, .2f, .25f), 1.4f); var hc = ph + up * 3f;
            Heart(z.mk, hc, z.front, 1.7f, paper, .45f);
            halos.Add(hc, 2.4f, new Color(1f, .2f, .25f), .4f);
        }

        /// A flat heart facing 'face', with an optional hole in the middle (holeR, as a fraction of its size).
        void Heart(MeshKit mk, Vector3 c, Vector3 face, float s, Material m, float holeR, Vector3? right = null)
        {
            var side = (right ?? Vector3.Cross(Vector3.up, face)).normalized; var up = Vector3.Cross(face, side).normalized; var n = 40; var pts = new Vector3[n];
            for (var k = 0; k < n; k++) { var t = k * Mathf.PI * 2f / n; var x = 16f * Mathf.Pow(Mathf.Sin(t), 3); var y = 13f * Mathf.Cos(t) - 5f * Mathf.Cos(2 * t) - 2f * Mathf.Cos(3 * t) - Mathf.Cos(4 * t); pts[k] = c + (side * x + up * y) * (s / 16f); }
            for (var k = 0; k < n; k++)
            {
                var a = pts[k]; var b = pts[(k + 1) % n]; var ia = c + (a - c) * holeR; var ib = c + (b - c) * holeR;
                mk.Quad(m, a, b, ib, ia, c - face); mk.Quad(m, a, ia, ib, b, c + face);
            }
        }

        void Chair(Plot z, Vector3 at, Vector3 face)
        {
            var up = Vector3.up; var rot = Quaternion.LookRotation(face); var sd = Vector3.Cross(up, face);
            z.mk.Box(plank, at + up * .5f, new Vector3(.55f, .07f, .55f), rot); z.mk.Box(plank, at + up * .95f - face * .26f, new Vector3(.55f, .8f, .06f), rot);
            foreach (var a in new[] { -.24f, .24f }) foreach (var b in new[] { -.24f, .24f }) z.mk.Box(plank, at + up * .25f + sd * a + face * b, new Vector3(.05f, .5f, .05f), rot);
            ArtistBits.PostCollider(z.root, at, 1f, .3f);
        }

        // ---------------------------------------------------------------- Live
        void LiveFarm(Plot z)
        {
            var up = Vector3.up; var rot = z.Rot; var ctx = z.ctx; var white = WorldMaterials.Solid(new Color(.78f, .78f, .74f));
            // the farmhouse with its porch, a swing on the porch
            var h = z.At(-7f, -2f); const float W = 7f, D = 5f, H = 3.4f;
            z.mk.Box(white, h + up * H / 2f, new Vector3(W, H, D), rot); z.solid.Box(white, h + up * H / 2f, new Vector3(W, H, D), rot);
            z.mk.GableRoof(darkRock, h + up * H, new Vector3(D + .6f, 0, W + .6f), 2.2f, Quaternion.LookRotation(z.side));
            var porch = h + z.front * (D / 2f + 1.2f); z.mk.Box(plank, porch + up * .3f, new Vector3(W, .2f, 2.4f), rot); z.solid.Box(plank, porch + up * .2f, new Vector3(W, .4f, 2.4f), rot);
            z.mk.Box(darkRock, porch + up * (H - .2f), new Vector3(W + .2f, .12f, 2.6f), rot * Quaternion.Euler(-8f, 0, 0));
            foreach (var s in new[] { -3.3f, 3.3f }) { z.mk.Box(white, porch + z.side * s + z.front * 1.1f + up * H / 2f, new Vector3(.15f, H, .15f), rot); ArtistBits.PostCollider(z.root, porch + z.side * s + z.front * 1.1f, H, .1f); }
            z.mk.Box(dark, h + up * 1.1f + z.front * (D / 2f + .01f), new Vector3(.9f, 2f, .02f), rot);
            foreach (var s in new[] { -2.2f, 2.2f }) z.mk.Box(SongGlow(AllOverYou, Lamp, 1.8f), h + up * 1.8f + z.front * (D / 2f + .01f) + z.side * s, new Vector3(1f, .9f, .02f), rot);
            var sw = porch + z.side * -1.8f; foreach (var s in new[] { -.7f, .7f }) z.mk.Box(dark, sw + z.side * s + up * 2.3f, new Vector3(.02f, 1.6f, .02f), Quaternion.identity);
            z.mk.Box(plank, sw + up * 1.45f, new Vector3(1.6f, .1f, .5f), rot); z.mk.Box(plank, sw + up * 1.75f - z.front * .25f, new Vector3(1.6f, .5f, .06f), rot);
            halos.Add(h + up * 1.8f + z.front * (D / 2f + .6f), 3f, Lamp, .4f);
            // the lightning rod: a tall mast on the roof with a glass ball
            var mast = h + up * (H + 2.2f) + z.side * 2.4f; z.mk.Prism(metal, mast - up * 1f, up, .06f, 9f, 6); z.mk.Blob(SongGlow(LightningCrashes, new Color(.7f, .8f, 1f), 2.6f), mast + up * 5f, Vector3.one * .3f, ctx.Rng, 0f);
            z.mk.Cone(metal, mast + up * 8f, .1f, .8f, 6); halos.Add(mast + up * 5f, 1.6f, new Color(.7f, .8f, 1f), .5f);
            life.rod = mast + up * 8.8f;
            // I Alone: one chair under one lamp, out in the clearing
            var ia = z.At(1f, 8f); Chair(z, ia, -z.side);
            var lp = ia + z.side * .9f; z.mk.Prism(dark, lp - up * .2f, up, .05f, 2.8f, 6); z.mk.Box(dark, lp + up * 2.55f - z.side * .45f, new Vector3(.02f, .02f, .9f), Quaternion.LookRotation(z.side));
            z.mk.Cone(dark, lp + up * 2.4f - z.side * .9f, .28f, .25f, 10); z.mk.Blob(SongGlow(IAlone, Lamp, 2.6f), lp + up * 2.35f - z.side * .9f, Vector3.one * .1f, ctx.Rng, 0f);
            halos.Add(lp + up * 2.2f - z.side * .9f, 1.3f, Lamp, .6f); ArtistBits.PostCollider(z.root, lp, 2.6f, .08f);
        }

        // ---------------------------------------------------------------- Nirvana
        void Nirvana(Plot z)
        {
            var up = Vector3.up; var rot = z.Rot; var ctx = z.ctx;
            // the heart-shaped box on a plinth, its lid ajar, glowing inside
            var hb = z.At(-6f, -5f) + up * 1f; var red = WorldMaterials.Solid(new Color(.6f, .1f, .12f), .5f);
            z.mk.Box(darkRock, hb - up * .5f, new Vector3(3.6f, 1f, 2.4f), rot);
            var inner = SongGlow(HeartShapedBox, new Color(1f, .3f, .3f), 2.4f);
            Heart(z.mk, hb + up * 1.2f, up, 1.7f, inner, 0f, z.side);   // the floor glowing, a red heart wall round it
            for (var k = 0; k < 40; k++)
            {
                var t0 = k * Mathf.PI * 2f / 40f; var t1 = (k + 1) * Mathf.PI * 2f / 40f;
                Vector3 H(float t) { var x = 16f * Mathf.Pow(Mathf.Sin(t), 3); var y = 13f * Mathf.Cos(t) - 5f * Mathf.Cos(2 * t) - 2f * Mathf.Cos(3 * t) - Mathf.Cos(4 * t); return hb + up * 1.2f + (z.side * x - z.front * y) * (1.7f / 16f); }
                var a = H(t0); var b = H(t1); z.mk.Box(red, (a + b) / 2f + up * .3f, new Vector3(.12f, .6f, Vector3.Distance(a, b) + .03f), Quaternion.LookRotation(b - a));
            }
            // the lid, propped open
            var lidRot = Quaternion.AngleAxis(-35f, z.side);
            Heart(z.mk, hb + up * 2.5f - z.front * 1.4f, lidRot * up, 1.75f, red, 0f, z.side);
            halos.Add(hb + up * 2f, 2.6f, new Color(1f, .3f, .3f), .5f); z.solid.Box(darkRock, hb + up * .6f - z.front * .4f, new Vector3(4.4f, 2.8f, 4.6f), rot);
            ArtistBits.PointLight(z.root, hb + up * 3f + z.front * 2f, new Color(1f, .35f, .35f), 10f, 1.4f);
            // Lithium: a rusted trailer at the edge of the clearing
            Trailer(ctx, z.mk, z.solid, z.At(-6f, 8.5f), SongGlow(Lithium, new Color(.75f, .85f, 1f), 2f), z.front);
            // Where Did You Sleep Last Night: a low dark stage of candles and white lilies with a stool and a guitar
            var st = z.At(1.5f, -9f); var stage = WorldMaterials.Solid(new Color(.12f, .1f, .1f));
            z.mk.Box(stage, st + up * .2f, new Vector3(4.4f, .4f, 3.2f), rot); z.solid.Box(stage, st + up * .8f, new Vector3(4.4f, 1.6f, 3.2f), rot);
            var candle = SongGlow(WhereDidYouSleep, new Color(1f, .75f, .35f), 2.8f); var wax = WorldMaterials.Solid(new Color(.92f, .9f, .85f)); var lily = WorldMaterials.Solid(new Color(.95f, .95f, .92f)); var leaf = WorldMaterials.Solid(new Color(.2f, .35f, .2f));
            for (var k = 0; k < 14; k++) { var p = st + up * .4f + z.side * ((k % 7) - 3f) * .6f + z.front * (k < 7 ? -1.3f : 1.3f); var hh = .25f + (k % 3) * .2f; z.mk.Prism(wax, p, up, .07f, hh, 6); z.mk.Blob(candle, p + up * (hh + .06f), new Vector3(.04f, .07f, .04f), ctx.Rng, 0f); }
            for (var k = 0; k < 6; k++) { var p = st + up * .4f + z.side * (k < 3 ? -1.9f : 1.9f) + z.front * ((k % 3) - 1f) * .6f; z.mk.Prism(leaf, p, up, .04f, .9f, 5); for (var j = 0; j < 3; j++) z.mk.Cone(lily, p + up * (.9f + j * .12f) + Quaternion.Euler(0, j * 120f, 0) * Vector3.forward * .1f, .12f, .25f, 5); }
            Chair(z, st + up * .4f - z.front * .3f, z.front);
            var gtr = st + up * .9f + z.side * .6f; var wood = WorldMaterials.Solid(new Color(.55f, .35f, .18f));
            z.mk.Blob(wood, gtr, new Vector3(.32f, .4f, .1f), ctx.Rng, 0f); z.mk.Box(dark, gtr + up * .55f, new Vector3(.06f, .8f, .05f), rot * Quaternion.Euler(0, 0, -12f));
            halos.Add(st + up * 1f, 2.4f, new Color(1f, .75f, .35f), .5f);
        }

        // ---------------------------------------------------------------- Foo Fighters
        void Foo(Plot z)
        {
            var up = Vector3.up; var rot = z.Rot; var ctx = z.ctx;
            // Everlong: a little silver saucer half sunk and tipped in the moss, rim lights glowing
            var u = z.At(-6f, -2f) + up * .8f; var tilt = Quaternion.AngleAxis(14f, z.side) * Quaternion.AngleAxis(-8f, z.front);
            z.mk.Prism(metal, u - tilt * up * .4f, tilt * up, 3f, .6f, 20, .35f); z.mk.Prism(metal, u - tilt * up * .4f, -(tilt * up), 3f, .4f, 20, .5f);
            z.mk.Blob(WorldMaterials.Solid(new Color(.55f, .75f, .85f), .95f), u + tilt * up * .5f, new Vector3(1f, .55f, 1f), ctx.Rng, 0f);
            var rim = SongGlow(Everlong, new Color(.5f, 1f, .8f), 2.4f);
            for (var k = 0; k < 12; k++) { var a = k * Mathf.PI / 6f; z.mk.Blob(rim, u + tilt * (new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * 2.95f) - tilt * up * .2f, Vector3.one * .16f, ctx.Rng, 0f); }
            halos.Add(u, 3.6f, new Color(.5f, 1f, .8f), .35f); z.mk.Blob(moss, u - up * .6f, new Vector3(3.6f, .4f, 3.4f), ctx.Rng, .3f);
            z.solid.Box(metal, u, new Vector3(6.4f, 1.8f, 6.4f), tilt * rot); z.solid.Prism(moss, u - up * 1.2f, up, 3.9f, 1.4f, 12);
            ArtistBits.PointLight(z.root, u + up * 2f + z.front * 2f, new Color(.5f, 1f, .8f), 12f, 1.2f);
            // My Hero: a road flare burning red on a rock
            var fl = z.At(1f, -7f); z.mk.Blob(rock, fl + up * .3f, new Vector3(.8f, .5f, .7f), ctx.Rng, .3f); z.mk.Box(SongGlow(MyHero, new Color(1f, .15f, .1f), 3f), fl + up * .75f, new Vector3(.08f, .5f, .08f), rot * Quaternion.Euler(0, 0, 20f));
            halos.Add(fl + up * 1f, 1.8f, new Color(1f, .2f, .1f), .8f); life.flare = fl + up * 1f; ArtistBits.PostCollider(z.root, fl, .8f, .5f);
            // Sean: a mailbox on a post with its flag up
            var mb = z.At(1f, 7f); z.mk.Prism(bark, mb - up * .2f, up, .08f, 1.3f, 6); z.mk.Box(metal, mb + up * 1.25f, new Vector3(.4f, .4f, .7f), rot);
            z.mk.Box(SongGlow(SeanSong, new Color(1f, .3f, .2f), 2.2f), mb + up * 1.5f + z.side * .22f, new Vector3(.03f, .35f, .2f), rot); ArtistBits.PostCollider(z.root, mb, 1.4f, .25f);
        }

        // ---------------------------------------------------------------- Alice In Chains
        void Alice(Plot z)
        {
            var up = Vector3.up; var rot = z.Rot; var ctx = z.ctx;
            // Nutshell: a hut shaped like a cracked nutshell, a candle inside
            var n = z.At(-5f, -3f); var shell = WorldMaterials.Solid(new Color(.45f, .33f, .2f), .3f);
            z.mk.Blob(shell, n + up * 1.6f, new Vector3(2.4f, 1.9f, 2.2f), ctx.Rng, .06f);
            var inside = SongGlow(Nutshell, new Color(1f, .62f, .28f), 1.4f);
            var crack = new[] { new Vector2(0f, .1f), new Vector2(.25f, .7f), new Vector2(-.15f, 1.3f), new Vector2(.2f, 1.9f), new Vector2(-.1f, 2.5f), new Vector2(.15f, 3.1f) };
            for (var k = 0; k + 1 < crack.Length; k++)
            {
                var a = n + z.side * crack[k].x + up * crack[k].y + z.front * 2.15f; var b = n + z.side * crack[k + 1].x + up * crack[k + 1].y + z.front * 2.15f;
                var w = k < 2 ? .9f : .45f - k * .05f; z.mk.Box(inside, (a + b) / 2f, new Vector3(w, Vector3.Distance(a, b) + .08f, .3f), Quaternion.LookRotation(z.front, b - a));
            }
            for (var k = 0; k < 9; k++) { var a = k * Mathf.PI * 2f / 9f; z.mk.Box(shell, n + up * 1.6f + (z.side * Mathf.Cos(a) * 2.2f + z.front * Mathf.Sin(a) * 2f), new Vector3(.1f, .1f, 1.4f), Quaternion.LookRotation(-z.side * Mathf.Sin(a) + z.front * Mathf.Cos(a))); }
            ArtistBits.PointLight(z.root, n + up * 1.2f + z.front * 3.2f, new Color(1f, .62f, .28f), 9f, 1.4f);
            z.mk.Blob(SongGlow(Nutshell, new Color(1f, .7f, .35f), 3f), n + up * .9f + z.front * 1.9f, new Vector3(.08f, .14f, .08f), ctx.Rng, 0f); halos.Add(n + up * 1f + z.front * 2.3f, 1.4f, new Color(1f, .7f, .35f), .6f);
            z.solid.Box(shell, n + up * 1.6f, new Vector3(4.2f, 3.4f, 3.8f), rot);
            // Would?: a dead tree hung with chains round a single lamp
            var t = z.At(-3f, 5f); var deadWood = WorldMaterials.Solid(new Color(.3f, .28f, .27f)); var chainM = WorldMaterials.Solid(new Color(.45f, .45f, .47f), .8f);
            z.mk.Prism(deadWood, t, up, .4f, 6f, 6, .5f); ArtistBits.PostCollider(z.root, t, 6f, .4f);
            var lampAt = t + up * 3.2f + z.front * 1.6f;
            for (var k = 0; k < 4; k++)
            {
                var d = Quaternion.Euler(0, k * 90f + 30f, 0) * Vector3.forward; var tip = t + up * (4f + k * .4f) + d * 2.2f;
                z.mk.Prism(deadWood, t + up * (3.5f + k * .4f), (tip - (t + up * (3.5f + k * .4f))).normalized, .1f, 2.3f, 5, .4f);
                var links = 7; for (var j = 0; j < links; j++) z.mk.Box(chainM, tip - up * (j * .3f + .15f), new Vector3(j % 2 == 0 ? .1f : .03f, .28f, j % 2 == 0 ? .03f : .1f), Quaternion.identity);
            }
            z.mk.Box(dark, lampAt + up * .5f, new Vector3(.02f, .9f, .02f), Quaternion.identity); z.mk.Prism(deadWood, t + up * 4f, (lampAt + up * .95f - (t + up * 4f)).normalized, .08f, Vector3.Distance(lampAt + up * .95f, t + up * 4f), 5, .6f);
            z.mk.Box(SongGlow(Would, new Color(.65f, .8f, 1f), 2.6f), lampAt, new Vector3(.22f, .32f, .22f), rot); halos.Add(lampAt, 1.6f, new Color(.65f, .8f, 1f), .7f);
        }

        // ---------------------------------------------------------------- Pearl Jam
        void PearlJam(Plot z)
        {
            var up = Vector3.up; var rot = z.Rot; var ctx = z.ctx;
            // Even Flow: a little brook running across the clearing under a footbridge, its water glowing
            var flow = SongGlow(EvenFlow, new Color(.45f, .75f, .95f), 1.2f);
            var brook = new List<Vector3>(); for (var k = 0; k <= 12; k++) brook.Add(z.At(-6f + Mathf.Sin(k * .6f) * 1.2f, -9f + k * 1.5f) + up * .02f);
            Strip(z.mk, brook, flow, 1.4f, .09f, ctx);
            for (var k = 0; k < 12; k += 2) z.mk.Blob(k % 4 == 0 ? moss : rock, brook[k] + z.front * 1f + up * .15f, new Vector3(.4f, .25f, .4f), ctx.Rng, .3f);
            var br = brook[6]; var brot = Quaternion.LookRotation(z.front);
            for (var k = 0; k < 7; k++) z.mk.Box(oldPlank, br + up * .35f + z.front * (k - 3f) * .4f, new Vector3(1.4f, .1f, .36f), brot);
            foreach (var s in new[] { -.7f, .7f }) { z.mk.Box(bark, br + up * .9f + z.side * s, new Vector3(.08f, .08f, 2.8f), brot); foreach (var e in new[] { -1.3f, 1.3f }) z.mk.Box(bark, br + up * .6f + z.side * s + z.front * e, new Vector3(.08f, .7f, .08f), brot); }
            halos.Add(br + up * .3f, 2f, new Color(.45f, .75f, .95f), .35f); foreach (var s in new[] { -.72f, .72f }) z.solid.Box(bark, br + up * .7f + z.side * s, new Vector3(.2f, .9f, 2.8f), brot);
            // Jeremy: a slate on an easel, mountains drawn on it in chalk
            var ea = z.At(-2f, 6f); var erot = rot;
            foreach (var s in new[] { -.5f, .5f }) z.mk.Box(plank, ea + up * 1f + z.side * s, new Vector3(.08f, 2f, .08f), erot * Quaternion.Euler(-8f, 0, s * -10f));
            z.mk.Box(plank, ea + up * .9f - z.front * .5f, new Vector3(.08f, 1.9f, .08f), erot * Quaternion.Euler(20f, 0, 0));
            z.mk.Box(dark, ea + up * 1.4f + z.front * .05f, new Vector3(1.6f, 1.1f, .05f), erot * Quaternion.Euler(-8f, 0, 0));
            var chalk = SongGlow(Jeremy, new Color(.9f, .95f, 1f), 1.6f);
            Vector3 C(float x, float y) => ea + up * (1.4f + y) + z.side * x + z.front * (.09f - y * .14f);
            var peaks = new[] { new Vector2(-.7f, -.4f), new Vector2(-.35f, .15f), new Vector2(-.05f, -.15f), new Vector2(.3f, .35f), new Vector2(.7f, -.4f) };
            for (var k = 0; k + 1 < peaks.Length; k++) { var a = C(peaks[k].x, peaks[k].y); var b = C(peaks[k + 1].x, peaks[k + 1].y); z.mk.Box(chalk, (a + b) / 2f, new Vector3(.04f, .04f, Vector3.Distance(a, b)), Quaternion.LookRotation(b - a)); }
            z.mk.Blob(chalk, C(.55f, .35f), new Vector3(.08f, .08f, .02f), ctx.Rng, 0f);
            ArtistBits.PostCollider(z.root, ea, 2f, .5f); halos.Add(ea + up * 1.4f + z.front * .4f, 1.3f, new Color(.9f, .95f, 1f), .3f);
        }

        // ---------------------------------------------------------------- the rest
        void Bush(Plot z)
        {
            var up = Vector3.up; var rot = z.Rot; var ctx = z.ctx;
            var e = z.At(-3f, -2.5f); z.mk.Box(rust, e + up * .9f, new Vector3(1.8f, 1.4f, 2.6f), rot); z.mk.Box(metal, e + up * 1.9f, new Vector3(1.6f, .5f, 2.2f), rot);
            for (var k = 0; k < 4; k++) { z.mk.Prism(metal, e + up * 2.1f + z.front * (-.9f + k * .6f), up, .22f, .6f, 8); z.mk.Blob(SongGlow(Machinehead, new Color(1f, .4f, .2f), 2.2f), e + up * 2.75f + z.front * (-.9f + k * .6f), Vector3.one * .12f, ctx.Rng, 0f); }
            z.solid.Box(rust, e + up * 1.2f, new Vector3(2f, 2.4f, 2.8f), rot); halos.Add(e + up * 2.8f, 1.8f, new Color(1f, .4f, .2f), .4f);
            var zg = z.At(-2f, 3.5f); var gravel = WorldMaterials.Solid(new Color(.6f, .6f, .58f));
            z.mk.Box(oldPlank, zg + up * .05f, new Vector3(3.6f, .1f, 3.6f), rot); z.mk.Box(gravel, zg + up * .1f, new Vector3(3.4f, .04f, 3.4f), rot);
            for (var k = 0; k < 7; k++) z.mk.Box(WorldMaterials.Solid(new Color(.5f, .5f, .48f)), zg + up * .13f + z.front * ((k - 3f) * .45f), new Vector3(3.2f, .02f, .05f), Quaternion.LookRotation(z.side) * Quaternion.Euler(0, 90f, 0));
            z.mk.Blob(SongGlow(EverythingZen, new Color(.7f, .85f, 1f), 1f), zg + up * .45f, new Vector3(.6f, .4f, .5f), ctx.Rng, .2f); ArtistBits.PostCollider(z.root, zg, .8f, .5f);
        }

        void BlindMelon(Plot z)
        {
            var up = Vector3.up; var ctx = z.ctx; var c = z.At(-3f, 0);
            z.mk.Prism(SongGlow(NoRain, new Color(1f, .85f, .35f), .9f), c - up * .2f, up, 3f, .32f, 18); halos.Add(c + up * .5f, 3.4f, new Color(1f, .85f, .35f), .35f);
            var stalk = WorldMaterials.Solid(new Color(.25f, .4f, .2f)); var petal = WorldMaterials.Solid(new Color(1f, .8f, .15f));
            for (var k = 0; k < 5; k++) { var p = c + Quaternion.Euler(0, k * 72f, 0) * Vector3.forward * 1.3f; p.y = ctx.Ground(p.x, p.z); z.mk.Prism(stalk, p, up, .05f, 1.6f, 5); z.mk.Prism(petal, p + up * 1.6f, z.front, .32f, .05f, 10); z.mk.Prism(WorldMaterials.Solid(new Color(.25f, .15f, .05f)), p + up * 1.6f + z.front * .03f, z.front, .16f, .05f, 8); }
            var bee = c + up * 2.4f; var yellow = WorldMaterials.Solid(new Color(1f, .8f, .1f)); var black = WorldMaterials.Solid(new Color(.05f, .05f, .05f));
            for (var k = 0; k < 3; k++) z.mk.Blob(k % 2 == 0 ? yellow : black, bee + z.side * (k - 1f) * .22f, new Vector3(.17f, .2f, .2f), ctx.Rng, 0f);
            z.solid.Prism(stalk, c, up, 1.6f, 1.8f, 8);
            foreach (var s in new[] { -1f, 1f }) z.mk.Box(WorldMaterials.Solid(new Color(.85f, .9f, 1f), .9f), bee + up * .25f + z.front * s * .2f, new Vector3(.3f, .02f, .3f), Quaternion.Euler(0, 0, 0));
            life.sunPatch = c + up * 1f; noGrass.Add(new Vector3(c.x, 3f, c.z));
        }

        void Dino(Plot z)
        {
            var up = Vector3.up; var ctx = z.ctx; var bone = WorldMaterials.Solid(new Color(.85f, .82f, .74f)); var c = z.At(-3f, 0);
            var spine = new List<Vector3>(); for (var k = 0; k <= 14; k++) { var t = k / 14f; spine.Add(c + z.side * (t * 8f - 4f) + up * (1.6f + Mathf.Sin(t * Mathf.PI) * 1.2f - (t > .8f ? (t - .8f) * 3f : 0f))); }
            for (var k = 0; k + 1 < spine.Count; k++) z.mk.Box(bone, (spine[k] + spine[k + 1]) / 2f, new Vector3(.18f, .18f, Vector3.Distance(spine[k], spine[k + 1])), Quaternion.LookRotation(spine[k + 1] - spine[k]));
            for (var k = 4; k < 10; k++) foreach (var s in new[] { -1f, 1f }) z.mk.Box(bone, spine[k] + z.front * s * .45f - up * .5f, new Vector3(.08f, 1.1f, .08f), Quaternion.LookRotation(z.side) * Quaternion.Euler(0, 0, s * 25f));
            var skull = spine[0] - z.side * .5f; z.mk.Blob(bone, skull, new Vector3(.6f, .45f, .4f), ctx.Rng, .05f); z.mk.Blob(SongGlow(FeelThePain, new Color(1f, .4f, .3f), 3f), skull + z.front * .32f + up * .1f, Vector3.one * .08f, ctx.Rng, 0f);
            foreach (var k in new[] { 4, 10 }) foreach (var s in new[] { -1f, 1f }) { var f = spine[k] + z.front * s * .5f; var g = new Vector3(f.x, ctx.Ground(f.x, f.z), f.z); z.mk.Box(bone, (f + g) / 2f, new Vector3(.14f, f.y - g.y, .14f), Quaternion.identity); ArtistBits.PostCollider(z.root, g, f.y - g.y, .15f); }
            halos.Add(skull + z.front * .4f, 1f, new Color(1f, .4f, .3f), .5f);
        }

        void Honey(Plot z)
        {
            var up = Vector3.up; var ctx = z.ctx; var c = z.At(-3f, 0);
            z.mk.Prism(tin, c, up, .9f, 1.5f, 14, 1.2f); z.mk.Prism(dark, c + up * 1.5f, up, 1.06f, .05f, 14);
            var honey = SongGlow(HoneyBucket, new Color(1f, .65f, .1f), 2f); z.mk.Prism(honey, c + up * 1.46f, up, 1f, .05f, 14);
            for (var k = 0; k < 5; k++) { var a = k * 1.3f; var d = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)); z.mk.Box(honey, c + d * 1.05f + up * (1.2f - k * .1f), new Vector3(.12f, .5f + k * .1f, .05f), Quaternion.LookRotation(d)); }
            z.mk.Prism(honey, c + up * .06f + z.front * 1.2f, up, .6f, .04f, 10); halos.Add(c + up * 1.6f, 1.8f, new Color(1f, .65f, .1f), .5f);
            z.solid.Prism(tin, c, up, 1.1f, 1.6f, 10);
        }

        void FaceTree(Plot z)
        {
            var up = Vector3.up; var ctx = z.ctx; var c = z.At(-3f, 0);
            z.mk.Prism(bark, c - up * .3f, up, 1f, 8f, 8, .55f); z.mk.Cone(pineDark, c + up * 6f, 3f, 5f, 7); z.mk.Cone(pineDark, c + up * 8.5f, 2f, 3.5f, 7);
            var eyes = SongGlow(Asylum, new Color(.75f, .9f, 1f), 2.6f);
            foreach (var s in new[] { -1f, 1f }) { z.mk.Blob(dark, c + up * 3f + z.front * .8f + z.side * s * .35f, new Vector3(.22f, .28f, .1f), ctx.Rng, 0f); z.mk.Blob(eyes, c + up * 3f + z.front * .86f + z.side * s * .35f, Vector3.one * .08f, ctx.Rng, 0f); }
            z.mk.Blob(dark, c + up * 2f + z.front * .82f, new Vector3(.3f, .5f, .1f), ctx.Rng, 0f);
            halos.Add(c + up * 3f + z.front * 1.2f, 1.2f, new Color(.75f, .9f, 1f), .5f); ArtistBits.PostCollider(z.root, c, 6f, 1f);
        }

        void SilverChair(Plot z)
        {
            var up = Vector3.up; var c = z.At(-3f, 0); var silver = SongGlow(Freak, new Color(.85f, .88f, .95f), .9f); var rot = z.Rot; var sd = z.side;
            z.mk.Box(silver, c + up * 1.2f, new Vector3(1.8f, .25f, 1.8f), rot); z.mk.Box(silver, c + up * 2.6f - z.front * .8f, new Vector3(1.8f, 2.6f, .2f), rot);
            foreach (var a in new[] { -.8f, .8f }) foreach (var b in new[] { -.8f, .8f }) z.mk.Box(silver, c + up * .55f + sd * a + z.front * b, new Vector3(.18f, 1.1f, .18f), rot);
            halos.Add(c + up * 2f, 2.4f, new Color(.85f, .88f, .95f), .3f); z.solid.Box(silver, c + up * 1.4f, new Vector3(1.9f, 2.8f, 1.9f), rot);
        }

        void Swings(Plot z)
        {
            var up = Vector3.up; var c = z.At(-3f, 0); var rot = z.Rot; var frame = WorldMaterials.Solid(new Color(.55f, .25f, .2f), .4f);
            foreach (var s in new[] { -1.8f, 1.8f }) foreach (var f in new[] { -1f, 1f }) { z.mk.Box(frame, c + z.side * s + z.front * f * .5f + up * 1.4f, new Vector3(.1f, 2.9f, .1f), rot * Quaternion.Euler(f * 18f, 0, 0)); ArtistBits.PostCollider(z.root, c + z.side * s + z.front * f * .9f, 2.8f, .1f); }
            z.mk.Box(frame, c + up * 2.75f, new Vector3(3.8f, .1f, .1f), rot);
            foreach (var x in new[] { -.8f, .8f }) { foreach (var e in new[] { -.25f, .25f }) z.mk.Box(dark, c + z.side * (x + e) + up * 1.9f, new Vector3(.02f, 1.7f, .02f), rot); z.mk.Box(x < 0 ? SongGlow(YoungestDaughter, new Color(1f, .7f, .8f), 2f) : plank, c + z.side * x + up * 1f, new Vector3(.6f, .06f, .25f), rot); }
            halos.Add(c + z.side * -.8f + up * 1.1f, 1f, new Color(1f, .7f, .8f), .5f); z.solid.Box(plank, c + up * 1.4f, new Vector3(3.8f, 2.8f, .8f), rot);
        }

        void Stool(Plot z)
        {
            var up = Vector3.up; var ctx = z.ctx; var c = z.At(-3f, 0); var rot = z.Rot;
            z.mk.Prism(plank, c + up * .8f, up, .3f, .08f, 10); foreach (var k in new[] { 0f, 120f, 240f }) z.mk.Box(plank, c + up * .4f + Quaternion.Euler(0, k, 0) * Vector3.forward * .2f, new Vector3(.05f, .8f, .05f), Quaternion.Euler(0, k, 0) * Quaternion.Euler(10f, 0, 0));
            var g = c + up * 1.2f + z.side * .3f; var wood = WorldMaterials.Solid(new Color(.6f, .4f, .2f)); z.mk.Blob(wood, g, new Vector3(.35f, .45f, .12f), ctx.Rng, 0f); z.mk.Box(dark, g + up * .7f, new Vector3(.07f, 1f, .05f), rot * Quaternion.Euler(0, 0, -10f));
            z.mk.Box(dark, c + up * 3f, new Vector3(.02f, 1.2f, .02f), Quaternion.identity); z.mk.Blob(SongGlow(AndILoveHer, Lamp, 3f), c + up * 2.35f, Vector3.one * .12f, ctx.Rng, 0f);
            var post = c - z.front * 1.2f; z.mk.Prism(bark, post - up * .3f, up, .15f, 4f, 6); z.mk.Box(bark, post + up * 3.6f + z.front * .6f, new Vector3(.1f, .1f, 1.3f), rot); ArtistBits.PostCollider(z.root, post, 3.6f, .15f);
            halos.Add(c + up * 2.2f, 1.6f, Lamp, .7f); ArtistBits.PostCollider(z.root, c, 1f, .35f);
        }
    }
}
