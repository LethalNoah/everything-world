using System.Collections.Generic;
using UnityEngine;
using static EverythingWorld.World.PlaceTools;

namespace EverythingWorld.World
{
    /// The Delta Crossroads' artists. Most have a porch of their own (a small plank shack with a tin roof, a rocking
    /// chair, a guitar leaning by the door) and one thing from their song beside it; covers on a cream card in a
    /// plank frame, a plaque, a bare bulb on a pole.
    /// - Robert Johnson (at the crossroads itself): two plank walls meeting in a corner, a chair facing into it, a
    ///   guitar; knee prints in the dust; a door standing on its own, to knock on. His songs bring midnight.
    /// - Muddy Waters: the juke joint. Tin roof, a blue neon guitar, one bare bulb over the door; inside, a little
    ///   stage with a guitar to play, a bar on barrels with jars to carry, crates to sit on, a shelf of mojo.
    /// - Howlin' Wolf: a steam locomotive standing on the line, its stack glowing; a wolf howling against a white
    ///   disc; a chair and a microphone to howl into. John Lee Hooker: a porch, a big amp, a board to stomp.
    /// - B.B. King: an empty stage, a black guitar alone on a stand under one bulb, empty seats, a sign whose first
    ///   half has gone out. Etta James: a rocking chair turned to the road, two figures walking away down it, a blue
    ///   ribbon tied round the porch post. Albert King: a bad sign, a ladder, a black cat, a cracked mirror, and a
    ///   small black cloud that rains on him alone.
    /// - Big Mama Thornton's hound dog; Fats Domino's blueberry hill; Gary Clark Jr.'s bright lights; Lead Belly's
    ///   cold stand of pines; Sister Rosetta Tharpe's railway platform, white guitar and umbrella; Stevie Ray
    ///   Vaughan's hat and guitar; R.L. Burnside's couch and jug; Junior Kimbrough's place and the city on a board.
    public partial class DeltaKit
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
                Add("Robert Johnson", "Cross Road Blues", "@the shadow on the west road"); Add("Howlin' Wolf", "Smokestack", "Howlin' Wolf"); Add("Sister Rosetta Tharpe", "Didn't It Rain", "@the crossroads");
                Add("B.B. King", "The Thrill Is Gone", "B.B. King"); Add("Muddy Waters", "Mannish Boy", "Muddy Waters");
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
            if (dustM == null) MakeMaterials();
            var root = new GameObject("Artist: " + a.name).transform; root.SetParent(ctx.Root, false);
            var front = facing.TryGetValue(a.name, out var f) ? f : new Vector3(center.x - a.x, 0, center.z - a.z).normalized;
            if (front.sqrMagnitude < .01f) front = Vector3.forward;
            var labels = new GameObject("Labels").transform; labels.SetParent(root, false);
            var area = root.gameObject.AddComponent<ArtistArea>();
            area.artistName = a.name; area.stationId = a.stationId; area.radius = a.r; area.glowColor = Bulb;
            var z = new Plot { ctx = ctx, root = root, labels = labels, mk = new MeshKit(), solid = new MeshKit(), pictures = new MeshKit(), a = a, c = ctx.OnGround(a.x, a.z), front = front, side = Vector3.Cross(Vector3.up, front), area = area };
            var reach = Foot(a); var up = Vector3.up;
            z.mk.Prism(dustM, z.c - up * .25f, up, reach * .66f, .33f, 20);
            switch (a.name)
            {
                case "Robert Johnson": RobertJohnson(z); break;
                case "Muddy Waters": MuddyWaters(z); break;
                case "Howlin' Wolf": HowlinWolf(z); break;
                case "John Lee Hooker": JohnLeeHooker(z); break;
                case "B.B. King": BBKing(z); break;
                case "Etta James": EttaJames(z); break;
                case "Albert King": AlbertKing(z); break;
                case "Lead Belly": LeadBelly(z); break;
                default: Porch(z); Others(z); break;
            }
            SongLamps(z);
            Board(z, z.At(reach * .6f, -reach * .45f - 1.5f), front);
            ArtistBits.Plaque(z.mk, z.solid, labels, a, z.At(reach * .6f + 1.5f, reach * .45f + 1.5f), front, plankDark, plank, Cream, 1.6f);
            BulbPole(ctx, z.mk, z.At(reach * .5f, reach * .64f), -z.side, true);
            if (z.area.glowLight == null) z.area.glowLight = ArtistBits.PointLight(root, z.c + up * 5f + front * 3f, Bulb, HasStation(a) ? 26f : 18f, 1.6f);
            area.glowRenderers = new Renderer[0];
            z.mk.Build("Area", root, false);
            z.solid.BuildCollider("Walls", root);
            z.pictures.Build("Covers", root, false);
            noGrass.Add(new Vector3(z.c.x, reach * .9f, z.c.z));
            if (HasStation(a) || reach >= 10f)
            {
                var mark = new GameObject("Shot: " + a.name + " up close").transform; mark.SetParent(ctx.Root, false);
                var cam = z.At(reach * .85f, reach * .3f) + up * 2f; var look = z.At(-reach * .3f, 0) + up * 2f - cam; mark.SetPositionAndRotation(cam, Quaternion.LookRotation(look.normalized));
            }
        }

        /// The covers on a cream card in a plank frame under a little tin roof.
        void Board(Plot z, Vector3 ground, Vector3 dir)
        {
            dir = Vector3.ProjectOnPlane(dir, Vector3.up).normalized; var rot = Quaternion.LookRotation(dir);
            var n = Mathf.Max(1, Mathf.Min(7, z.a.songList?.Length ?? 1)); const float size = .9f; var w = n * size * 1.25f + .6f;
            var c = ground + Vector3.up * 1.7f;
            z.mk.Box(plankDark, c - Vector3.up * .5f - dir * .06f, new Vector3(w + .5f, size + 2f, .2f), rot); z.solid.Box(plankDark, c - Vector3.up * .5f, new Vector3(w + .5f, size + 2f, .3f), rot); z.mk.Box(WorldMaterials.Solid(Cream), c - Vector3.up * .1f + dir * .05f, new Vector3(w, size + .9f, .04f), rot);
            z.mk.Box(tin, c + Vector3.up * (size / 2f + .62f) + dir * .2f, new Vector3(w + .9f, .05f, .9f), rot * Quaternion.Euler(14f, 0, 0));
            ArtistBits.CoverWall(z.pictures, z.labels, z.ctx, z.a, c + dir * .08f + Vector3.up * .08f, dir, n, size, n, true, Ink);
        }

        /// A jar with a light in it on a post, for each song without its own piece.
        void SongLamps(Plot z)
        {
            if (!SongIds.TryGetValue(z.a.name, out var list)) return;
            var up = Vector3.up; var k = 0; var seen = new HashSet<string>();
            foreach (var (_, id) in list)
            {
                if (z.lit.Contains(id) || !seen.Add(id)) continue;
                var p = z.At(Foot(z.a) * .55f - (k % 2) * 1.3f, Foot(z.a) * .2f + (k / 2) * 1.3f);
                z.mk.Prism(trunkM, p - up * .1f, up, .05f, 1.2f, 5); z.mk.Prism(SongGlow(id, Bulb, 2f), p + up * 1.1f, up, .09f, .22f, 6); z.mk.Prism(tin, p + up * 1.32f, up, .1f, .03f, 6); halos.Add(p + up * 1.2f, .8f, Bulb, .4f); ArtistBits.PostCollider(z.root, p, 1.2f, .08f);
                k++;
            }
        }

        /// A porch of their own: a small shack, a rocking chair, a guitar by the door.
        PlaceFrame Porch(Plot z, bool lit = false)
        {
            var hue = z.a.name.Length + z.a.name[0]; var f = new PlaceFrame(z.At(-Foot(z.a) * .42f, 0), z.front); var po = Shack(z.ctx, z.mk, z.solid, f, 4.6f, 3.2f, hue, hue % 3 == 0, lit);
            Rocker(z.mk, po.P(-1.3f, 0, .1f), (po.Fwd + po.Right * .2f).normalized); Guitar(z.mk, po.P(1.7f, 0, -.9f), po.r, WorldMaterials.Solid(new Color(.52f, .36f, .2f), .5f), new System.Random(hue));
            return po;
        }

        // ---------------------------------------------------------------- Robert Johnson: the corner
        void RobertJohnson(Plot z)
        {
            var up = Vector3.up; var rot = z.Rot; var rng = new System.Random(36); var cr = Song(z, "Cross Road Blues"); var md = Song(z, "Me and the Devil Blues"); foreach (var s in new[] { cr, md }) if (s != null) life.johnson.Add(s);
            // two plank walls meeting in a corner, a chair facing into it: the way he sat to play
            var k0 = z.At(-4.6f, -2.6f); var wallM = WorldMaterials.Solid(Planks[1]);
            z.mk.Box(wallM, k0 + up * 1.4f + z.side * 1.7f, new Vector3(3.4f, 2.8f, .16f), rot); z.solid.Box(wallM, k0 + up * 1.4f + z.side * 1.7f, new Vector3(3.4f, 2.8f, .16f), rot); z.mk.Box(wallM, k0 + up * 1.4f + z.front * 1.7f, new Vector3(.16f, 2.8f, 3.4f), rot); z.solid.Box(wallM, k0 + up * 1.4f + z.front * 1.7f, new Vector3(.16f, 2.8f, 3.4f), rot);
            var ch = k0 + z.side * 1.3f + z.front * 1.3f; var cf = (-z.side - z.front).normalized; var crot = Quaternion.LookRotation(cf);
            z.mk.Box(plankDark, ch + up * .45f, new Vector3(.46f, .05f, .46f), crot); z.mk.Box(plankDark, ch + up * .8f - cf * .21f, new Vector3(.46f, .7f, .05f), crot); foreach (var (x, zz) in new[] { (-.2f, -.2f), (.2f, -.2f), (.2f, .2f), (-.2f, .2f) }) z.mk.Box(plankDark, ch + up * .22f + crot * new Vector3(x, 0, zz), new Vector3(.04f, .44f, .04f), crot);
            Interact.Seat(tlife.transform, ch + up * .5f, cf, "Sit facing the corner"); Guitar(z.mk, k0 + z.side * .4f + z.front * .9f, Quaternion.LookRotation(z.side), WorldMaterials.Solid(new Color(.3f, .2f, .12f), .5f), rng);
            string[] corner = { "The corner throws the sound back at you, bigger. You'd swear there were two guitars.", "You sit with your back to the room. Somebody behind you is listening. There is nobody behind you.", "A slide, a moan, a bass line walking by itself underneath. Two hands shouldn't be able to do that." }; var cn = 0;
            Spot("The corner", ch + up * 1.3f, "Play into the corner", new Vector3(1f, 1f, 1f)).onUse = _ => { life.played = Time.time; Say1(ch + up * 2.6f, corner[cn++ % corner.Length]); };
            var bulb = SongGlow(cr, Bulb, 2.2f); z.mk.Prism(dark, k0 + up * 2.8f + z.side * 1f + z.front * 1f, -up, .008f, .6f, 3); z.mk.Blob(bulb, k0 + up * 2.14f + z.side * 1f + z.front * 1f, new Vector3(.07f, .1f, .07f), rng, 0f); halos.Add(k0 + up * 2.14f + z.side * 1f + z.front * 1f, .9f, Bulb, .5f);
            z.area.glowLight = ArtistBits.PointLight(z.root, k0 + up * 2f + z.side * 1.2f + z.front * 1.2f, Bulb, 16f, 1.8f);
            // "fell down on my knees": two knee prints in the dust, facing the crossroads
            var kn = z.At(4.5f, 0); var print = WorldMaterials.Solid(new Color(.6f, .48f, .32f)); foreach (var e in new[] { -.18f, .18f }) { z.mk.Blob(print, kn + z.side * e + up * .1f, new Vector3(.12f, .02f, .16f), rng, 0f); z.mk.Blob(print, kn + z.side * e - z.front * .5f + up * .1f, new Vector3(.08f, .02f, .12f), rng, 0f); }
            Interact.Seat(tlife.transform, kn + up * .12f - z.front * .25f, z.front, "Kneel at the crossroads").seatHeight = .12f;
            // "Me and the Devil": a door standing on its own. Knock.
            var dp = z.At(.5f, 4.6f); var dr = rot * Quaternion.Euler(0, -30f, 0); z.mk.Box(plankDark, dp + up * 1.15f, new Vector3(1.3f, 2.4f, .1f), dr); z.mk.Box(WorldMaterials.Solid(Planks[2]), dp + up * 1.1f + dr * Vector3.forward * .04f, new Vector3(.95f, 2.1f, .08f), dr); z.mk.Blob(SongGlow(md, new Color(1f, .3f, .15f), 1.4f), dp + up * 1.1f + dr * new Vector3(.34f, 0, .1f), Vector3.one * .04f, rng, 0f); BoxCol(z.root, dp + up * 1.15f, new Vector3(1.3f, 2.4f, .14f), dr);
            string[] knock = { "You knock. Nothing. Then, from the other side, where there is only field: a knock back.", "\"Hello, Satan,\" the song goes, \"I believe it's time to go.\" The door stays shut. This time.", "Knock, knock. The dust on the step shifts, as if somebody had moved their feet." }; var kk = 0;
            Spot("The door", dp + up * 1.2f + dr * Vector3.forward * .4f, "Knock", new Vector3(1.2f, 2f, .8f)).onUse = _ => Say1(dp + up * 2.9f, knock[kk++ % knock.Length]);
            BottleTree(z.ctx, z.mk, z.At(-1.5f, -6f), 1.1f, rng);
        }

        // ---------------------------------------------------------------- Muddy Waters: the juke joint
        void MuddyWaters(Plot z)
        {
            var up = Vector3.up; const float W = 11f, D = 8f, H = 3.2f, T = .25f, Door = 1.9f; var fo = new PlaceFrame(z.At(-4.5f, 0) + up * .16f, z.front); var rng = new System.Random(55);
            var mb = Song(z, "Mannish Boy"); var hc = Song(z, "I'm Your Hoochie Coochie Man"); foreach (var s in new[] { mb, hc }) if (s != null) life.muddy.Add(s);
            var wall = WorldMaterials.Glow(new Color(.5f, .4f, .3f), .16f); var floor = WorldMaterials.Glow(new Color(.42f, .34f, .26f), .12f);
            Room(z.mk, z.solid, fo, W, D, H, T, 0f, Door, wall, floor);
            z.mk.GableRoof(rustM, fo.P(0, H + .2f, 0), new Vector3(D + 1f, 0, W + 1f), 1.7f, Quaternion.LookRotation(fo.Right)); foreach (var e in new[] { -1f, 1f }) z.mk.Box(wall, fo.P(e * (W / 2f - .02f), H + .7f, 0), new Vector3(.04f, 1f, D * .55f), fo.r);
            // the porch: a lean-to on posts, one bare bulb over the door, the neon guitar up on the roof
            z.mk.Box(tin, fo.P(0, H - .2f, D / 2f + 1.3f), new Vector3(W + .6f, .06f, 2.8f), fo.r * Quaternion.Euler(9f, 0, 0)); z.mk.Box(plankDark, fo.P(0, -.1f, D / 2f + 1.2f), new Vector3(W, .12f, 2.4f), fo.r); foreach (var e in new[] { -1f, 0f, 1f }) { if (e == 0f) continue; z.mk.Prism(plankDark, fo.P(e * (W / 2f - .3f), -.16f, D / 2f + 2.3f), up, .08f, H - .3f, 4); ArtistBits.PostCollider(z.root, fo.P(e * (W / 2f - .3f), -.16f, D / 2f + 2.3f), H - .3f, .1f); }
            z.mk.Prism(dark, fo.P(0, H - .3f, D / 2f + .8f), -up, .008f, .5f, 3); z.mk.Blob(bulbM, fo.P(0, H - .88f, D / 2f + .8f), new Vector3(.08f, .11f, .08f), rng, 0f); halos.Add(fo.P(0, H - .88f, D / 2f + .8f), 1.2f, Bulb, .6f); ArtistBits.PointLight(z.root, fo.P(0, H - 1.2f, D / 2f + 1.2f), Bulb, 10f, 1.6f);
            var ng = fo.P(-1.6f, H + 2.6f, D / 2f - .6f); var nrot = fo.r * Quaternion.Euler(0, 0, 32f); z.mk.Prism(neonM, ng + nrot * new Vector3(0, -.5f, 0), z.front, .62f, .05f, 14); z.mk.Prism(dark, ng + nrot * new Vector3(0, -.5f, 0) + z.front * .03f, z.front, .5f, .05f, 14); z.mk.Prism(neonM, ng + nrot * new Vector3(0, .3f, 0), z.front, .44f, .05f, 14); z.mk.Prism(dark, ng + nrot * new Vector3(0, .3f, 0) + z.front * .03f, z.front, .33f, .05f, 14);
            z.mk.Box(neonM, ng + nrot * new Vector3(0, 1.5f, 0), new Vector3(.12f, 1.7f, .05f), nrot); z.mk.Box(neonM, ng + nrot * new Vector3(0, 2.5f, 0), new Vector3(.3f, .4f, .05f), nrot); foreach (var e in new[] { -1f, 1f }) z.mk.Prism(iron, ng + nrot * new Vector3(e * .5f, -.4f, -.2f) - up * 1.4f, up, .03f, 1.4f, 4);
            halos.Add(ng + up * .6f + z.front * .6f, 2.6f, Blues, .5f); ArtistBits.Label(z.labels, "JUKE", fo.P(2.2f, H + 1.5f, D / 2f - .3f), z.front, .7f, Blues, TextAnchor.MiddleCenter); foreach (var e in new[] { -1f, 1f }) z.mk.Prism(iron, fo.P(2.2f + e * 1.1f, H + .5f, D / 2f - .34f), up, .03f, 1.4f, 4); z.mk.Box(dark, fo.P(2.2f, H + 1.5f, D / 2f - .36f), new Vector3(2.6f, 1f, .05f), fo.r);
            Rocker(z.mk, fo.P(-4f, -.04f, D / 2f + 1.1f), z.front); Crate(z.mk, fo.P(4.2f, -.04f, D / 2f + 1.2f), z.front);
            Folk(fo.P(3.2f, -.04f, D / 2f + 1.4f), z.front, Resident.Pose.Lean, new[] { "Go on in. Band's just a fella and his foot, but it's enough.", "No fighting, no preaching, no requests. Well. One request.", "Storm comes, we all get in there and play till it's gone. Storm don't come, we do it anyway." });
            // inside: a low stage at the left end with a guitar to play, an amp, a microphone
            var sx = -W / 2f + T + 1.7f; z.mk.Box(plankDark, fo.P(sx, .12f, -.4f), new Vector3(3f, .24f, 4.6f), fo.r); z.solid.Box(plankDark, fo.P(sx, .02f, -.4f), new Vector3(3f, .44f, 4.6f), fo.r);
            var amp = fo.P(sx - .7f, .24f, -1.8f); z.mk.Box(dark, amp + up * .45f, new Vector3(.8f, .9f, .5f), fo.r); z.mk.Box(WorldMaterials.Solid(new Color(.6f, .52f, .36f)), amp + up * .4f + fo.Right * .0f + z.front * .26f, new Vector3(.66f, .6f, .02f), fo.r); BoxCol(z.root, amp + up * .45f, new Vector3(.8f, .9f, .5f), fo.r);
            var gt = fo.P(sx, .24f, -.4f); var gm = new MeshKit(); Guitar(gm, Vector3.zero, Quaternion.identity, WorldMaterials.Solid(new Color(.7f, .16f, .12f), .7f), rng, 1.05f); gm.Prism(iron, Vector3.zero, Vector3.up, .12f, .03f, 6);
            var ggo = gm.BuildSingle("The juke joint's guitar", tlife.transform, false); ggo.transform.SetPositionAndRotation(gt, Quaternion.LookRotation(fo.Right)); string[] riff = { "Da-DA-da-da-DUM. The whole room answers with its feet.", "One riff. Five notes. You could play it all night, and people have.", "You hit it and stop. In the gap somebody shouts \"YEAH\". You hit it again.", "The bulb swings. The floor bounces. The riff does not change. It doesn't need to." }; var rn = 0;
            Interact.Toy(ggo, Toy.Kind.Custom, "Play the riff", new Vector3(.9f, 1.6f, .9f)).onUse = _ => { life.played = Time.time; life.stomped = Time.time; Say1(gt + up * 2.4f, riff[rn++ % riff.Length]); };
            var mic = fo.P(sx + .8f, .24f, .6f); z.mk.Prism(iron, mic, up, .18f, .03f, 8); z.mk.Prism(iron, mic, up, .018f, 1.45f, 4); z.mk.Blob(steel, mic + up * 1.5f, new Vector3(.06f, .09f, .06f), rng, 0f);
            var hm = new MeshKit(); hm.Box(steel, new Vector3(0, .015f, 0), new Vector3(.12f, .03f, .035f), Quaternion.identity); z.mk.Prism(plankDark, fo.P(sx + .9f, .24f, -1.6f), up, .16f, .6f, 8); Thing(hm, "Harmonica", "a harmonica", fo.P(sx + .9f, .85f, -1.6f), fo.r, new Vector3(.12f, .04f, .04f), .1f);
            // "Mannish Boy" painted on the back wall; "Hoochie Coochie Man": a shelf of mojo
            ArtistBits.Label(z.labels, "M - A - N", fo.P(.6f, 2.2f, -D / 2f + T + .03f), z.front, .5f, Cream, TextAnchor.MiddleCenter); z.mk.Box(SongGlow(mb, Bulb, 1.4f), fo.P(.6f, 1.75f, -D / 2f + T + .03f), new Vector3(2.6f, .04f, .02f), fo.r);
            var shelf = fo.P(3.4f, 1.5f, -D / 2f + T + .14f); z.mk.Box(plankDark, shelf, new Vector3(1.6f, .05f, .24f), fo.r); var mojo = SongGlow(hc, new Color(1f, .25f, .15f), 1.6f); z.mk.Blob(mojo, shelf + up * .1f - fo.Right * .5f, new Vector3(.07f, .08f, .06f), rng, .2f); z.mk.Blob(WorldMaterials.Solid(Cream), shelf + up * .06f, new Vector3(.12f, .025f, .03f), rng, .1f); z.mk.Blob(WorldMaterials.Solid(new Color(.4f, .28f, .18f)), shelf + up * .08f + fo.Right * .5f, new Vector3(.08f, .06f, .05f), rng, .4f); ArtistBits.Label(z.labels, "mojo hand · black cat bone · john the conqueror", shelf - up * .14f + z.front * .13f, z.front, .055f, Cream, TextAnchor.MiddleCenter); halos.Add(shelf + up * .1f - fo.Right * .5f + z.front * .2f, .5f, new Color(1f, .3f, .2f), .5f);
            // the bar on barrels down the right end, jars to carry; crates to sit on round a cable drum
            var bx = W / 2f - T - 1.1f; foreach (var e in new[] { -1.2f, 1.2f }) { z.mk.Prism(plank, fo.P(bx, 0, e - .2f), up, .4f, .95f, 10); z.mk.Prism(iron, fo.P(bx, .3f, e - .2f), up, .42f, .05f, 10); } z.mk.Box(plankDark, fo.P(bx, 1f, -.2f), new Vector3(.8f, .08f, 4f), fo.r); BoxCol(z.root, fo.P(bx, .5f, -.2f), new Vector3(.9f, 1.1f, 4f), fo.r);
            for (var k = 0; k < 3; k++) { var jm = new MeshKit(); jm.Prism(WorldMaterials.Glow(new Color(.85f, .9f, .9f), .3f), Vector3.zero, Vector3.up, .05f, .15f, 8); jm.Prism(tin, new Vector3(0, .15f, 0), Vector3.up, .045f, .02f, 8); Thing(jm, "Jar", "a jar of something clear", fo.P(bx, 1.05f, -1.2f + k * 1f), Quaternion.identity, new Vector3(.11f, .18f, .11f), .2f, false); }
            Folk(fo.P(bx + .75f, 0, .4f), -fo.Right, Resident.Pose.Stand, new[] { "It's clear, it's cold, and it'll teach you things. One jar.", "That fella on the stage been playing since the cotton was planted.", "You want water, the river's that way." }, Local(Residents.Tool.None, true));
            var dr = fo.P(.8f, 0, 1f); z.mk.Prism(plank, dr, up, .7f, .7f, 12); z.mk.Prism(plankDark, dr + up * .7f, up, .9f, .06f, 12); ArtistBits.PostCollider(z.root, dr, .8f, .7f); z.mk.Prism(WorldMaterials.Glow(new Color(.85f, .9f, .9f), .3f), dr + up * .76f + fo.Right * .3f, up, .05f, .15f, 8);
            Crate(z.mk, dr + fo.Right * 1.3f, -fo.Right); Crate(z.mk, dr - fo.Right * 1.3f + z.front * .3f, fo.Right); Crate(z.mk, dr - z.front * 1.3f, -fo.Right);
            Folk(dr - z.front * 1.3f + up * .46f, -fo.Right, Resident.Pose.Sit, new[] { "Mm. Mm-hm. Play that again.", "I walked nine miles for this. I'd have walked ten.", "When he sings \"I'm a man\", you believe him. You believe everything." }, null, null, .46f);
            Folk(fo.P(-.9f, 0, -1.6f), -fo.Right, Resident.Pose.Wave, new[] { "Can't sit down! Not to this!", "Whoo! Hit it again!" });
            // one bare bulb indoors
            z.mk.Prism(dark, fo.P(0, H, 0), -up, .008f, .7f, 3); z.mk.Blob(bulbM, fo.P(0, H - .78f, 0), new Vector3(.08f, .11f, .08f), rng, 0f); halos.Add(fo.P(0, H - .78f, 0), 1f, Bulb, .5f); ArtistBits.PointLight(z.root, fo.P(0, H - 1f, 0), Bulb, 12f, 2.4f, true); ArtistBits.PointLight(z.root, fo.P(sx + .6f, 1.8f, -.4f), new Color(1f, .7f, .45f), 6f, 1.2f, true);
            z.area.glowLight = ArtistBits.PointLight(z.root, fo.P(0, H + 2f, D / 2f + 3f), Blues, 22f, 1.8f);
            Mark(fo.o, 8f);
            Shot(z.ctx, "the juke joint", fo.P(-6f, 2f, D / 2f + 12f), fo.P(0, 3.2f, D / 2f));
            Shot(z.ctx, "inside the juke joint", fo.P(3.4f, 1.7f, D / 2f - .9f), fo.P(sx, 1.1f, -.6f));
        }

        // ---------------------------------------------------------------- Howlin' Wolf: Smokestack Lightnin'
        void HowlinWolf(Plot z)
        {
            var up = Vector3.up; var rot = z.Rot; var rng = new System.Random(58); var s = Song(z, "Smokestack"); life.wolfSong = s;
            // the locomotive stands on the line just north of his place, facing the crossing
            var lc = ctx0.OnGround(z.c.x - 3f, center.z + RailOff) + up * .42f; var lr = Quaternion.LookRotation(R); var black = WorldMaterials.Solid(new Color(.08f, .08f, .09f), .6f);
            Vector3 L(float x, float y, float zz) => lc + lr * new Vector3(x, y, zz);
            z.mk.Prism(black, L(0, 2f, -1f), R, 1.1f, 6f, 12); z.mk.Box(black, L(0, .95f, 1.6f), new Vector3(2f, .7f, 7.6f), lr); z.mk.Box(black, L(0, 2.3f, -2.6f), new Vector3(2.5f, 2.6f, 2.6f), lr); z.mk.Box(rustM, L(0, 3.7f, -2.6f), new Vector3(2.8f, .14f, 3f), lr); foreach (var e in new[] { -1f, 1f }) z.mk.Box(WorldMaterials.Glow(new Color(1f, .6f, .25f), .5f), L(e * 1.26f, 2.7f, -2.6f), new Vector3(.03f, .8f, 1.2f), lr);
            z.mk.Prism(black, L(0, 3f, 3.8f), up, .34f, 1.5f, 10); z.mk.Prism(black, L(0, 4.5f, 3.8f), up, .34f, .6f, 10, 1.7f); z.mk.Prism(SongGlow(s, new Color(1f, .5f, .15f), 2.2f), L(0, 5.06f, 3.8f), up, .5f, .03f, 10); halos.Add(L(0, 5.3f, 3.8f), 1.6f, new Color(1f, .55f, .2f), .5f); life.smokeAt = L(0, 5.4f, 3.8f);
            z.mk.Prism(black, L(0, 3.1f, 1.6f), up, .3f, .5f, 8, .7f); z.mk.Prism(brassOf(), L(0, 3.1f, .2f), up, .16f, .5f, 8); z.mk.Blob(bulbM, L(0, 2.9f, 5.14f), Vector3.one * .2f, rng, 0f); z.mk.Box(black, L(0, 2.9f, 5f), new Vector3(.5f, .5f, .3f), lr); halos.Add(L(0, 2.9f, 5.6f), 1.6f, Bulb, .6f);
            for (var k = 0; k < 5; k++) z.mk.Box(rustM, L(0, .5f, 5.4f + k * .16f), new Vector3(2.1f - k * .36f, .12f + k * .12f, .14f), lr * Quaternion.Euler(-28f, 0, 0));
            foreach (var e in new[] { -1f, 1f }) { for (var k = 0; k < 3; k++) z.mk.Prism(rustM, L(e * .9f, .78f, -.4f + k * 1.7f), R * 0f + U * e, .78f, .14f, 12); z.mk.Prism(rustM, L(e * .9f, .46f, 4.4f), U * e, .46f, .14f, 10); z.mk.Box(steel, L(e * 1.04f, .78f, 1.3f), new Vector3(.06f, .12f, 3.8f), lr); }
            z.mk.Box(black, L(0, 1.7f, -6.4f), new Vector3(2.4f, 2.2f, 4.4f), lr); z.mk.Blob(dark, L(0, 2.9f, -6.4f), new Vector3(1f, .4f, 1.9f), rng, .3f); foreach (var e in new[] { -1f, 1f }) foreach (var q in new[] { -7.6f, -5.2f }) z.mk.Prism(rustM, L(e * .9f, .46f, q), U * e, .46f, .14f, 10);
            BoxCol(z.root, L(0, 2f, -1.4f), new Vector3(2.6f, 4f, 14.4f), lr); noGrass.Add(new Vector3(lc.x, 9f, lc.z));
            // a wolf, howling, cut out against a white disc on a pole
            var wp = z.At(1f, -5.5f); z.mk.Prism(trunkM, wp - up * .3f, up, .09f, 3.6f, 5); ArtistBits.PostCollider(z.root, wp, 3.3f, .1f); z.mk.Prism(SongGlow(s, new Color(.96f, .95f, .88f), .9f), wp + up * 4.1f - z.front * .04f, z.front, 1.1f, .04f, 18);
            var wf = wp + up * 3.75f + z.front * .03f; z.mk.Box(shadowM, wf, new Vector3(.9f, .36f, .04f), rot * Quaternion.Euler(0, 0, 14f)); z.mk.Box(shadowM, wf + z.side * .44f + up * .4f, new Vector3(.3f, .7f, .04f), rot * Quaternion.Euler(0, 0, -20f)); z.mk.Box(shadowM, wf + z.side * .62f + up * .86f, new Vector3(.14f, .44f, .04f), rot * Quaternion.Euler(0, 0, -34f)); z.mk.Box(shadowM, wf - z.side * .52f - up * .1f, new Vector3(.5f, .1f, .04f), rot * Quaternion.Euler(0, 0, 50f)); foreach (var e in new[] { -.3f, .26f }) z.mk.Box(shadowM, wf + z.side * e - up * .34f, new Vector3(.1f, .46f, .04f), rot);
            // his chair and a microphone: howl
            var cp = z.At(-1.5f, 1.5f); z.mk.Box(plankDark, cp + up * .5f, new Vector3(.9f, .1f, .8f), rot); z.mk.Box(plankDark, cp + up * 1f - z.front * .36f, new Vector3(.9f, 1f, .1f), rot); foreach (var e in new[] { -.4f, .4f }) z.mk.Box(plankDark, cp + up * .25f + z.side * e, new Vector3(.08f, .5f, .8f), rot); BoxCol(z.root, cp + up * .5f, new Vector3(.9f, 1f, .8f), rot); Interact.Seat(tlife.transform, cp + up * .58f + z.front * .05f, z.front, "Sit in the big chair").seatHeight = .55f;
            var mic = cp + z.front * 1.1f + z.side * .5f; z.mk.Prism(iron, mic, up, .18f, .03f, 8); z.mk.Prism(iron, mic, up, .018f, 1.4f, 4); z.mk.Blob(steel, mic + up * 1.46f, new Vector3(.06f, .09f, .06f), rng, 0f); ArtistBits.PostCollider(z.root, mic, 1.4f, .06f);
            string[] howl = { "\"Ah-OOOOOO...\" It goes out over the cotton and keeps going. The storm rumbles, impressed.", "\"Whoo-HOOO.\" Up on the line the engine lets off a little steam in sympathy.", "You howl. Somewhere out past the tracks, something howls the next line." }; var hn = 0;
            Spot("The Wolf's microphone", mic + up * 1.3f, "Howl", new Vector3(.9f, 1.8f, .9f)).onUse = _ => { life.played = Time.time; Say1(mic + up * 2.6f, howl[hn++ % howl.Length]); };
            z.area.glowLight = ArtistBits.PointLight(z.root, L(0, 3f, 7f), Bulb, 22f, 1.8f);
            Shot(z.ctx, "the engine on the line", L(-9f, 2.4f, 13f), L(0, 2.6f, 0));
        }

        Material brassOf() => WorldMaterials.Solid(new Color(.8f, .62f, .26f), .8f);

        // ---------------------------------------------------------------- John Lee Hooker: Boom Boom
        void JohnLeeHooker(Plot z)
        {
            var up = Vector3.up; var rot = z.Rot; var rng = new System.Random(62); var s = Song(z, "Boom Boom"); life.boomSong = s; var po = Porch(z, true);
            ArtistBits.Label(z.labels, "BOOM BOOM", po.P(0, 2.9f, .2f), z.front, .3f, Bulb, TextAnchor.MiddleCenter);
            // a big amp out in the yard and a board to stomp
            var amp = z.At(1f, 3.4f); z.mk.Box(dark, amp + up * .6f, new Vector3(1.1f, 1.2f, .55f), rot); z.mk.Box(WorldMaterials.Solid(new Color(.6f, .52f, .36f)), amp + up * .52f + z.front * .28f, new Vector3(.94f, .86f, .02f), rot); z.mk.Blob(SongGlow(s, new Color(1f, .3f, .15f), 2.4f), amp + up * 1.1f + z.front * .29f + z.side * .4f, Vector3.one * .035f, rng, 0f); BoxCol(z.root, amp + up * .6f, new Vector3(1.1f, 1.2f, .55f), rot);
            var bd = z.At(2.2f, 1.2f); z.mk.Box(plank, bd + up * .14f, new Vector3(1f, .06f, .7f), rot); z.mk.Box(SongGlow(s, Bulb, 1f), bd + up * .1f, new Vector3(1.1f, .02f, .8f), rot);
            string[] stomp = { "Boom. Boom. Boom. Boom. Every bulb in the Delta jumps.", "You stomp. The amp hums. A-haw-haw-haw-haw.", "One foot, four beats. That's the whole rhythm section and it's plenty.", "Boom boom boom boom. Somebody on a porch half a mile off stomps back." }; var sn = 0;
            Spot("The stomp board", bd + up * .6f, "Stomp your foot", new Vector3(1.1f, 1.2f, .9f)).onUse = _ => { life.stomped = Time.time; life.played = Time.time; Say1(bd + up * 2.2f, stomp[sn++ % stomp.Length]); };
            Crate(z.mk, bd - z.front * 1f, z.front); Guitar(z.mk, amp - z.side * .9f, rot, WorldMaterials.Solid(new Color(.75f, .5f, .2f), .7f), rng, 1.05f);
        }

        // ---------------------------------------------------------------- B.B. King: The Thrill Is Gone
        void BBKing(Plot z)
        {
            var up = Vector3.up; var rot = z.Rot; var rng = new System.Random(69); var s = Song(z, "The Thrill Is Gone"); life.thrillSong = s; var c = z.At(-3f, 0);
            // an empty stage, a faded curtain, one black guitar alone on a stand under one bulb
            z.mk.Box(plankDark, c + up * .2f, new Vector3(7f, .4f, 4f), rot); z.solid.Box(plankDark, c, new Vector3(7f, .8f, 4f), rot); z.mk.Box(plankDark, c + up * .1f + z.front * 2.3f, new Vector3(2f, .2f, .6f), rot); z.solid.Box(plankDark, c + up * .1f + z.front * 2.3f, new Vector3(2f, .2f, .6f), rot);
            var curtain = WorldMaterials.Solid(new Color(.34f, .12f, .14f)); z.mk.Box(curtain, c + up * 2.3f - z.front * 1.9f, new Vector3(7f, 3.8f, .14f), rot); z.solid.Box(curtain, c + up * 2.3f - z.front * 1.9f, new Vector3(7f, 3.8f, .14f), rot); for (var k = 0; k < 8; k++) z.mk.Box(WorldMaterials.Solid(new Color(.26f, .09f, .11f)), c + up * 2.3f - z.front * 1.82f + z.side * (-3.1f + k * .9f), new Vector3(.2f, 3.8f, .04f), rot); foreach (var e in new[] { -1f, 1f }) z.mk.Box(plankDark, c + up * 2.3f - z.front * 1.9f + z.side * e * 3.6f, new Vector3(.2f, 4.2f, .2f), rot);
            var gs = c + up * .4f; z.mk.Prism(iron, gs, up, .2f, .03f, 8); var gm = new MeshKit(); Guitar(gm, Vector3.zero, Quaternion.identity, WorldMaterials.Solid(new Color(.05f, .05f, .06f), .9f), rng, 1.1f); foreach (var e in new[] { -.1f, .1f }) gm.Blob(brassOf(), new Vector3(e, .2f, .07f), Vector3.one * .02f, rng, 0f);
            var ggo = gm.BuildSingle("The black guitar", tlife.transform, false); ggo.transform.SetPositionAndRotation(gs, rot); string[] bend = { "One note, bent up and held, shaking. It says more than most people's whole lives.", "You play three notes and leave the rest out. The empty seats lean in.", "A slow bend, a quick sting, a long quiet. That quiet is the song.", "She sings for you. That's the only way to put it. The guitar sings." }; var bn = 0;
            Interact.Toy(ggo, Toy.Kind.Custom, "Play one note", new Vector3(.9f, 1.7f, .9f)).onUse = _ => { life.played = Time.time; Say1(gs + up * 2.6f, bend[bn++ % bend.Length]); };
            var bulb = SongGlow(s, new Color(.75f, .85f, 1f), 2.4f); z.mk.Prism(dark, gs + up * 4.2f, -up, .008f, 1.4f, 3); z.mk.Blob(bulb, gs + up * 2.72f, new Vector3(.08f, .11f, .08f), rng, 0f); halos.Add(gs + up * 2.72f, 1.2f, new Color(.7f, .8f, 1f), .5f); z.mk.Box(plankDark, c + up * 4.5f - z.front * .9f, new Vector3(7.2f, .14f, 2.2f), rot); foreach (var e in new[] { -1f, 1f }) { z.mk.Prism(plankDark, c + z.side * e * 3.4f + z.front * .1f, up, .08f, 4.5f, 4); ArtistBits.PostCollider(z.root, c + z.side * e * 3.4f + z.front * .1f, 4.4f, .1f); }
            z.area.glowLight = ArtistBits.PointLight(z.root, gs + up * 2.4f + z.front * .6f, new Color(.75f, .85f, 1f), 18f, 2f);
            // a sign: the first half has gone out
            ArtistBits.Label(z.labels, "THE THRILL", c + up * 4.9f + z.front * .2f + z.side * 1.5f, z.front, .4f, new Color(.3f, .3f, .32f), TextAnchor.MiddleCenter); ArtistBits.Label(z.labels, "IS GONE", c + up * 4.9f + z.front * .2f - z.side * 1.9f, z.front, .4f, Blues, TextAnchor.MiddleCenter); z.mk.Box(dark, c + up * 4.9f + z.front * .14f, new Vector3(6.4f, .7f, .06f), rot); z.mk.Box(SongGlow(s, Blues, 1.8f), c + up * 4.62f + z.front * .18f - z.side * 1.9f, new Vector3(2.2f, .03f, .02f), rot); halos.Add(c + up * 4.9f + z.front * .7f - z.side * 1.9f, 1.6f, Blues, .4f);
            // a dropped bouquet, gone dry; a few empty seats
            var bq = c + up * .42f + z.front * 1.2f - z.side * 1.6f; for (var k = 0; k < 5; k++) { z.mk.Box(leafDry, bq + z.side * (k * .05f), new Vector3(.02f, .02f, .5f), rot * Quaternion.Euler(0, k * 9f - 18f, 0)); z.mk.Blob(WorldMaterials.Solid(new Color(.4f, .16f, .16f)), bq + rot * Quaternion.Euler(0, k * 9f - 18f, 0) * Vector3.forward * .26f + z.side * (k * .05f), Vector3.one * .05f, rng, .3f); }
            for (var k = 0; k < 5; k++) { var sp = z.At(4.2f + (k % 2) * 1.2f, -3f + k * 1.5f); Crate(z.mk, sp, -z.front); }
        }

        // ---------------------------------------------------------------- Etta James: I'd Rather Go Blind
        void EttaJames(Plot z)
        {
            var up = Vector3.up; var rot = z.Rot; var rng = new System.Random(68); var s = Song(z, "I'd Rather Go Blind"); var po = Porch(z);
            // a blue ribbon tied round the porch post, its ends lifting
            var rb = SongGlow(s, Blues, 1.6f); var post = po.P(2.1f, 1.5f, .9f); z.mk.Prism(rb, post - up * .06f, up, .1f, .12f, 6); z.mk.Box(rb, post + z.side * .3f - up * .2f, new Vector3(.6f, .1f, .02f), rot * Quaternion.Euler(0, 0, -30f)); z.mk.Box(rb, post + z.side * .26f - up * .34f, new Vector3(.5f, .09f, .02f), rot * Quaternion.Euler(0, 0, -52f)); halos.Add(post + z.front * .3f, .8f, Blues, .45f);
            // two figures walking away down the road, arm in arm, cut out of the light
            var away = z.At(7f, -2.5f); var ar = rot * Quaternion.Euler(0, 20f, 0); foreach (var e in new[] { -.3f, .3f }) { var p = away + ar * Vector3.right * e; z.mk.Box(shadowM, p + up * .95f, new Vector3(.42f, 1.1f, .05f), ar); z.mk.Blob(shadowM, p + up * 1.68f, new Vector3(.14f, .16f, .04f), rng, 0f); z.mk.Box(shadowM, p + up * .22f, new Vector3(.3f + (e > 0 ? .16f : 0f), .5f, .05f), ar); }
            z.mk.Prism(shadowM, away + ar * Vector3.right * .3f + up * 1.8f, up, .2f, .03f, 8); z.mk.Box(shadowM, away + up * 1.1f, new Vector3(.4f, .1f, .05f), ar); BoxCol(z.root, away + up * .9f, new Vector3(1.2f, 1.8f, .2f), ar);
            // a handkerchief on the porch rail, a single light in the window
            z.mk.Box(white, po.P(-2.1f, .95f, 1f), new Vector3(.3f, .3f, .02f), po.r * Quaternion.Euler(0, 0, 45f));
            string[] watch = { "You sit and watch them go. You could look away. You don't.", "The rocker creaks once and stops. Down the road the two shapes get smaller and do not turn round.", "Something told you it was over. Something deep down in your soul." }; var wn = 0;
            Spot("Etta's porch", po.P(-1.3f, 1.2f, .4f), "Watch the road", new Vector3(1f, 1.2f, 1f)).onUse = _ => Say1(po.P(-1.3f, 2.6f, .4f), watch[wn++ % watch.Length]);
        }

        // ---------------------------------------------------------------- Albert King: Born Under a Bad Sign
        void AlbertKing(Plot z)
        {
            var up = Vector3.up; var rot = z.Rot; var rng = new System.Random(67); var s = Song(z, "Born Under A Bad Sign"); var po = Porch(z); var p = z.At(2.6f, -2.8f);
            // the bad sign itself, crooked on its post, and a small black cloud over it that rains on nobody else
            z.mk.Prism(trunkM, p - up * .3f, (up + z.side * .1f).normalized, .08f, 3.3f, 5); ArtistBits.PostCollider(z.root, p, 3f, .1f); var sg = p + up * 2.7f + z.side * .3f; z.mk.Box(dark, sg, new Vector3(1.5f, 1f, .08f), rot * Quaternion.Euler(0, 0, -9f)); ArtistBits.Label(z.labels, "13", sg + z.front * .05f, z.front, .6f, Cream, TextAnchor.MiddleCenter); z.mk.Box(SongGlow(s, new Color(.6f, .7f, 1f), 1.4f), sg - up * .44f + z.front * .05f, new Vector3(1.2f, .03f, .02f), rot * Quaternion.Euler(0, 0, -9f));
            var cl = p + up * 5.2f + z.side * .3f; for (var k = 0; k < 4; k++) z.mk.Blob(WorldMaterials.Solid(new Color(.14f, .15f, .2f)), cl + z.side * (-1f + k * .7f) + up * ((k % 2) * .25f), new Vector3(.9f, .5f, .7f), rng, .3f); life.cloudAt = cl - up * .4f;
            // a ladder to walk under, a black cat, a cracked mirror, a guitar shaped like a V
            var ld = z.At(4.5f, 1.6f); foreach (var e in new[] { -.3f, .3f }) z.mk.Box(plank, ld + up * 1.5f + z.side * e, new Vector3(.06f, 3.2f, .06f), rot * Quaternion.Euler(-18f, 0, 0)); for (var k = 0; k < 7; k++) z.mk.Box(plank, ld + up * (.3f + k * .42f) + z.front * (.42f - k * .136f), new Vector3(.6f, .04f, .06f), rot); z.mk.Prism(trunkM, ld - z.front * .5f - up * .2f, up, .07f, 3.2f, 5); ArtistBits.PostCollider(z.root, ld - z.front * .5f, 3f, .09f);
            var cat = z.At(3.4f, 3.4f); z.mk.Blob(shadowM, cat + up * .2f, new Vector3(.14f, .2f, .2f), rng, 0f); z.mk.Blob(shadowM, cat + up * .46f + z.front * .08f, Vector3.one * .1f, rng, 0f); foreach (var e in new[] { -.05f, .05f }) { z.mk.Cone(shadowM, cat + up * .52f + z.front * .08f + z.side * e, .03f, .09f, 4); z.mk.Blob(SongGlow(s, new Color(.8f, 1f, .3f), 2f), cat + up * .48f + z.front * .17f + z.side * e * .8f, Vector3.one * .015f, rng, 0f); } z.mk.Prism(shadowM, cat + up * .1f - z.front * .16f, (up + z.side * .5f).normalized, .02f, .4f, 4);
            string[] catS = { "It crosses in front of you. Of course it does.", "The cat looks at you the way the weather looks at a picnic.", "It purrs. Somewhere, a string breaks." }; var cn = 0; Spot("The black cat", cat + up * .4f, "Let the cat cross your path", new Vector3(.8f, .8f, .8f)).onUse = _ => Say1(cat + up * 1.6f, catS[cn++ % catS.Length]);
            var mr = po.P(2.1f, 1.3f, 1.2f); z.mk.Box(plankDark, mr, new Vector3(.6f, .8f, .04f), po.r); z.mk.Box(steel, mr + po.Fwd * .025f, new Vector3(.5f, .7f, .02f), po.r); foreach (var a in new[] { 20f, -35f, 70f }) z.mk.Box(dark, mr + po.Fwd * .04f, new Vector3(.01f, .6f, .01f), po.r * Quaternion.Euler(0, 0, a));
            var gv = z.At(1f, 4.6f); z.mk.Prism(iron, gv, up, .14f, .03f, 6); var red = WorldMaterials.Solid(new Color(.6f, .1f, .1f), .7f); foreach (var e in new[] { -1f, 1f }) z.mk.Box(red, gv + up * .4f + z.side * e * .18f, new Vector3(.16f, .7f, .05f), rot * Quaternion.Euler(0, 0, e * 26f)); z.mk.Box(plankDark, gv + up * 1.05f, new Vector3(.05f, .8f, .03f), rot); z.mk.Box(red, gv + up * 1.5f, new Vector3(.1f, .16f, .03f), rot);
        }

        // ---------------------------------------------------------------- Lead Belly: in the pines
        void LeadBelly(Plot z)
        {
            var up = Vector3.up; var rot = z.Rot; var rng = new System.Random(59); var s = Song(z, "Where Did You Sleep Last Night"); var c = z.At(-1.5f, 0);
            // a stand of black pines close together, where the sun never shines; it is cold in there
            var pine = WorldMaterials.Solid(new Color(.1f, .16f, .14f)); var pine2 = WorldMaterials.Solid(new Color(.14f, .21f, .17f));
            for (var k = 0; k < 9; k++) { var a = k * 40f * Mathf.Deg2Rad; var d = k == 0 ? 0f : 3.2f + (k % 3) * .8f; var p = c + new Vector3(Mathf.Cos(a) * d, 0, Mathf.Sin(a) * d) - z.front * 1.4f; p.y = z.ctx.Ground(p.x, p.z); var h = 7f + (k % 4) * 1.4f; z.mk.Prism(trunkM, p - up * .3f, up, .2f, h * .5f, 6); ArtistBits.PostCollider(z.root, p, 3f, .22f); for (var q = 0; q < 4; q++) z.mk.Cone(q % 2 == 0 ? pine : pine2, p + up * (2.2f + q * h * .17f), 2f - q * .4f, h * .34f, 7, q * 25f); }
            var cold = SongGlow(s, new Color(.4f, .6f, 1f), 1.6f); z.mk.Prism(trunkM, c + z.front * 1.6f - z.side * 1.4f, up, .04f, 1.1f, 4); z.mk.Prism(cold, c + z.front * 1.6f - z.side * 1.4f + up * 1.1f, up, .1f, .24f, 6); halos.Add(c + z.front * 1.6f - z.side * 1.4f + up * 1.2f, 1.4f, Blues, .5f); z.area.glowLight = ArtistBits.PointLight(z.root, c + up * 2.4f + z.front * 1f, new Color(.45f, .6f, 1f), 16f, 2.2f);
            // a bedroll on the needles, a twelve-string against a trunk
            var br = c + z.front * 1.8f + z.side * .8f; z.mk.Box(WorldMaterials.Solid(new Color(.4f, .36f, .3f)), br + up * .14f, new Vector3(.8f, .1f, 1.9f), rot * Quaternion.Euler(0, 24f, 0)); z.mk.Prism(WorldMaterials.Solid(new Color(.5f, .2f, .16f)), br + up * .22f + rot * Quaternion.Euler(0, 24f, 0) * new Vector3(-.4f, 0, .9f), rot * Quaternion.Euler(0, 24f, 0) * Vector3.right, .14f, .8f, 8);
            Interact.Seat(tlife.transform, br + up * .2f, z.front, "Lie down in the pines").seatHeight = .2f; Guitar(z.mk, c + z.front * .9f - z.side * .4f, rot, WorldMaterials.Solid(new Color(.46f, .3f, .16f), .5f), rng, 1.15f);
            string[] pines = { "You shiver the whole night through. The wind in the needles asks the question again.", "It's ten degrees colder in here than out on the road. The sun doesn't come in.", "\"My girl, my girl, don't lie to me.\" The trees keep the answer to themselves." }; var pn = 0;
            Spot("In the pines", c + z.front * 1.4f + up * 1.3f, "Listen to the wind", new Vector3(1.4f, 1.4f, 1.4f)).onUse = _ => Say1(c + z.front * 1.4f + up * 2.8f, pines[pn++ % pines.Length]);
        }

        // ---------------------------------------------------------------- everyone else
        void Others(Plot z)
        {
            var up = Vector3.up; var rot = z.Rot; var p = z.At(2.6f, -2.8f); var rng = new System.Random(z.a.name.Length * 3); string s;
            switch (z.a.name)
            {
                case "Big Mama Thornton":
                    s = Song(z, "Hound Dog"); var fur = WorldMaterials.Solid(new Color(.56f, .4f, .26f)); var dgm = new MeshKit(); dgm.Blob(fur, new Vector3(0, .16f, 0), new Vector3(.22f, .16f, .46f), rng, .04f); dgm.Blob(fur, new Vector3(0, .14f, .52f), new Vector3(.15f, .12f, .2f), rng, 0f); foreach (var e in new[] { -.14f, .14f }) dgm.Box(WorldMaterials.Solid(new Color(.36f, .24f, .16f)), new Vector3(e, .1f, .5f), new Vector3(.06f, .02f, .26f), Quaternion.Euler(0, e * 60f, e * 300f)); dgm.Prism(fur, new Vector3(0, .12f, -.44f), new Vector3(.4f, .1f, -1f).normalized, .03f, .36f, 4);
                    var dgo = dgm.BuildSingle("The hound dog", tlife.transform, false); dgo.transform.SetPositionAndRotation(p + up * .1f, rot * Quaternion.Euler(0, 40f, 0)); string[] pat = { "One eye opens. The tail thumps twice. That's all you're getting.", "He ain't never caught a rabbit. He's thought about it, though.", "He sighs like a man twice his age and goes back to sleep." }; var pn = 0;
                    Interact.Toy(dgo, Toy.Kind.Custom, "Pat the hound dog", new Vector3(.8f, .6f, 1.2f)).onUse = _ => Say1(p + up * 1.6f, pat[pn++ % pat.Length]);
                    var dh = z.At(1f, -4.6f); z.mk.Box(plank, dh + up * .5f, new Vector3(1.1f, 1f, 1.3f), rot); z.mk.GableRoof(SongGlowTin(s), dh + up * 1f, new Vector3(1.3f, 0, 1.5f), .5f, rot); z.mk.Prism(dark, dh + up * .4f + z.front * .64f, z.front, .32f, .04f, 10); BoxCol(z.root, dh + up * .7f, new Vector3(1.1f, 1.4f, 1.3f), rot); z.mk.Prism(tin, p + z.side * .9f + up * .1f, up, .16f, .08f, 8, 1.3f); break;
                case "Fats Domino":
                    s = Song(z, "Blueberry Hill"); z.mk.Blob(WorldMaterials.Solid(new Color(.62f, .6f, .36f)), p - up * .3f, new Vector3(2.8f, 1.3f, 2.4f), rng, .12f); z.solid.Blob(dustM, p - up * .3f, new Vector3(2.6f, 1.2f, 2.2f), rng, .05f); var berry = SongGlow(s, new Color(.3f, .4f, 1f), 1.6f);
                    for (var k = 0; k < 8; k++) { var a = k * 45f * Mathf.Deg2Rad; var bp = p + new Vector3(Mathf.Cos(a) * 1.7f, .55f, Mathf.Sin(a) * 1.5f); z.mk.Blob(leafM, bp, new Vector3(.4f, .3f, .4f), rng, .3f); for (var q = 0; q < 4; q++) z.mk.Blob(berry, bp + Quaternion.Euler(q * 70f, q * 95f, 0) * Vector3.up * .34f, Vector3.one * .05f, rng, 0f); } halos.Add(p + up * 1.2f, 2.6f, Blues, .25f);
                    var bk = new MeshKit(); bk.Prism(plank, Vector3.zero, Vector3.up, .16f, .14f, 8, 1.2f); for (var q = 0; q < 6; q++) bk.Blob(berry, new Vector3(Mathf.Cos(q) * .08f, .16f, Mathf.Sin(q) * .08f), Vector3.one * .045f, rng, 0f); Thing(bk, "Blueberries", "a basket of blueberries", z.At(4.4f, -1f) + up * .1f, rot, new Vector3(.36f, .26f, .36f), .3f);
                    var pi = z.At(1.6f, 3.6f); z.mk.Box(plankDark, pi + up * .65f, new Vector3(1.4f, 1.3f, .55f), rot); z.mk.Box(white, pi + up * .78f + z.front * .32f, new Vector3(1.3f, .04f, .2f), rot); BoxCol(z.root, pi + up * .65f, new Vector3(1.4f, 1.3f, .6f), rot); Crate(z.mk, pi + z.front * .95f, -z.front); break;
                case "Gary Clark Jr.":
                    s = Song(z, "Bright Lights"); foreach (var e in new[] { -1.8f, 1.8f }) { z.mk.Prism(trunkM, p + z.side * e - up * .3f, up, .1f, 4.4f, 5); ArtistBits.PostCollider(z.root, p + z.side * e, 4f, .12f); } z.mk.Box(dark, p + up * 3.2f, new Vector3(4.4f, 1.8f, .12f), rot); ArtistBits.Label(z.labels, "BRIGHT LIGHTS", p + up * 3.45f + z.front * .08f, z.front, .44f, Bulb, TextAnchor.MiddleCenter); ArtistBits.Label(z.labels, "big city", p + up * 2.86f + z.front * .08f, z.front, .26f, Cream, TextAnchor.MiddleCenter);
                    var bl = SongGlow(s, new Color(1f, .9f, .6f), 2.4f); for (var k = 0; k < 15; k++) { z.mk.Blob(bl, p + up * 4.02f + z.side * (-2.1f + k * .3f) + z.front * .08f, Vector3.one * .07f, rng, 0f); z.mk.Blob(bl, p + up * 2.38f + z.side * (-2.1f + k * .3f) + z.front * .08f, Vector3.one * .07f, rng, 0f); } halos.Add(p + up * 3.2f + z.front * .8f, 3f, Bulb, .45f);
                    var am = z.At(4.2f, 1.6f); z.mk.Box(dark, am + up * .5f, new Vector3(.9f, 1f, .5f), rot); z.mk.Box(steel, am + up * .46f + z.front * .26f, new Vector3(.78f, .7f, .02f), rot); BoxCol(z.root, am + up * .5f, new Vector3(.9f, 1f, .5f), rot); Guitar(z.mk, am + z.side * .8f, rot, WorldMaterials.Solid(new Color(.6f, .12f, .1f), .8f), rng, 1.05f); break;
                case "Sister Rosetta Tharpe":
                    s = Song(z, "Didn't It Rain"); life.rainSong = s; z.mk.Box(plank, p + up * .25f, new Vector3(5f, .5f, 2.4f), rot); z.solid.Box(plank, p + up * .1f, new Vector3(5f, .8f, 2.4f), rot); z.mk.Box(plank, p + up * .12f + z.front * 1.5f, new Vector3(1.4f, .24f, .6f), rot); z.solid.Box(plank, p + up * .12f + z.front * 1.5f, new Vector3(1.4f, .24f, .6f), rot);
                    var wg = p + up * .5f - z.side * .6f; z.mk.Prism(iron, wg, up, .14f, .03f, 6); Guitar(z.mk, wg, rot, SongGlowM(s, new Color(.97f, .97f, .94f), .5f), rng, 1.1f); var um = p + up * .5f + z.side * .9f; z.mk.Prism(iron, um, up, .02f, 2.1f, 4); z.mk.Cone(dark, um + up * 1.8f, 1.1f, .4f, 8); z.mk.Box(white, um + up * 1.2f - z.side * .16f, new Vector3(.1f, 1.1f, .44f), rot * Quaternion.Euler(0, 0, -4f));
                    z.mk.Prism(WorldMaterials.Glow(new Color(.4f, .55f, .9f), .25f), z.At(4.8f, .6f) + up * .1f, up, 1.2f, .012f, 10); foreach (var e in new[] { -2.3f, 2.3f }) { z.mk.Prism(trunkM, p + up * .5f + z.side * e - z.front * 1f, up, .07f, 2.6f, 4); } z.mk.Box(tin, p + up * 3.1f - z.front * .4f, new Vector3(5.4f, .06f, 2f), rot * Quaternion.Euler(-8f, 0, 0)); ArtistBits.Label(z.labels, "PLATFORM 1", p + up * 2.8f + z.front * .5f, z.front, .16f, Ink, TextAnchor.MiddleCenter); z.mk.Box(white, p + up * 2.8f + z.front * .46f, new Vector3(1.8f, .34f, .04f), rot); break;
                case "Stevie Ray Vaughan":
                    s = Song(z, "Pride and Joy"); z.mk.Box(dark, p + up * .55f, new Vector3(1f, 1.1f, .5f), rot); z.mk.Box(WorldMaterials.Solid(new Color(.6f, .52f, .36f)), p + up * .5f + z.front * .26f, new Vector3(.86f, .76f, .02f), rot); BoxCol(z.root, p + up * .55f, new Vector3(1f, 1.1f, .5f), rot); z.mk.Prism(dark, p + up * 1.1f, up, .4f, .03f, 12); z.mk.Prism(dark, p + up * 1.12f, up, .2f, .16f, 10, .9f); z.mk.Prism(SongGlow(s, new Color(.8f, .85f, .9f), 1.2f), p + up * 1.14f, up, .21f, .04f, 10);
                    Guitar(z.mk, p + z.side * 1f + z.front * .2f, rot, WorldMaterials.Solid(new Color(.7f, .5f, .22f), .7f), rng, 1.08f); var fn = z.At(4.4f, 1.4f); z.mk.Box(plank, fn + up * .8f, new Vector3(1.6f, .9f, .06f), rot); foreach (var e in new[] { -.7f, .7f }) z.mk.Prism(trunkM, fn + z.side * e - up * .2f, up, .05f, 1.5f, 4); BoxCol(z.root, fn + up * .7f, new Vector3(1.6f, 1.3f, .1f), rot); ArtistBits.Label(z.labels, "PRIDE\n& JOY", fn + up * .8f + z.front * .04f, z.front, .2f, Rust, TextAnchor.MiddleCenter);
                    var ht = SongGlow(s, new Color(1f, .3f, .35f), 1.8f); for (var k = 0; k < 14; k++) { var t = k / 14f * Mathf.PI * 2f; var hx = 16f * Mathf.Pow(Mathf.Sin(t), 3f); var hy = 13f * Mathf.Cos(t) - 5f * Mathf.Cos(2f * t) - 2f * Mathf.Cos(3f * t) - Mathf.Cos(4f * t); z.mk.Blob(ht, fn + up * (1.7f + hy * .022f) + z.side * hx * .022f + z.front * .04f, Vector3.one * .03f, rng, 0f); } break;
                case "R.L. Burnside":
                    s = Song(z, "Goin' Down South"); var couch = WorldMaterials.Solid(new Color(.5f, .34f, .28f)); z.mk.Box(couch, p + up * .3f, new Vector3(1.9f, .44f, .8f), rot); z.mk.Box(couch, p + up * .7f - z.front * .32f, new Vector3(1.9f, .7f, .2f), rot); foreach (var e in new[] { -.9f, .9f }) z.mk.Box(couch, p + up * .5f + z.side * e, new Vector3(.2f, .5f, .8f), rot); BoxCol(z.root, p + up * .45f, new Vector3(1.9f, .9f, .8f), rot); Interact.Bench(tlife.transform, p + up * .56f - z.side * .5f + z.front * .1f, p + up * .56f + z.side * .5f + z.front * .1f, z.front);
                    var jg = new MeshKit(); jg.Prism(WorldMaterials.Solid(new Color(.7f, .62f, .46f)), Vector3.zero, Vector3.up, .13f, .26f, 8, .8f); jg.Prism(WorldMaterials.Solid(new Color(.4f, .28f, .18f)), new Vector3(0, .26f, 0), Vector3.up, .05f, .1f, 6); Thing(jg, "Jug", "a jug", p + z.side * 1.4f + up * .1f, rot, new Vector3(.28f, .38f, .28f), .5f);
                    var ar = z.At(4.6f, 1.4f); z.mk.Prism(trunkM, ar - up * .3f, up, .07f, 2.6f, 5); ArtistBits.PostCollider(z.root, ar, 2.3f, .09f); z.mk.Box(white, ar + up * 2f, new Vector3(1.6f, .4f, .05f), rot); ArtistBits.Label(z.labels, "SOUTH", ar + up * 2f + z.front * .04f, z.front, .22f, Ink, TextAnchor.MiddleCenter); z.mk.Box(SongGlow(s, Bulb, 1.4f), ar + up * 1.74f + z.front * .04f, new Vector3(1.4f, .03f, .02f), rot); z.mk.Cone(white, ar + up * 1.5f + z.front * .02f, .16f, -.3f, 3); break;
                case "Junior Kimbrough":
                    s = Song(z, "Meet Me in the City"); foreach (var e in new[] { -1.5f, 1.5f }) { z.mk.Prism(trunkM, p + z.side * e - up * .3f, up, .08f, 3.2f, 5); ArtistBits.PostCollider(z.root, p + z.side * e, 3f, .1f); } z.mk.Box(dark, p + up * 2.1f, new Vector3(3.6f, 1.9f, .08f), rot); var win = SongGlow(s, new Color(1f, .85f, .5f), 1.8f);
                    for (var k = 0; k < 7; k++) { var bh = .5f + ((k * 7) % 5) * .22f; var bx2 = -1.5f + k * .5f; z.mk.Box(WorldMaterials.Solid(new Color(.2f, .2f, .26f)), p + up * (1.25f + bh / 2f) + z.side * bx2 + z.front * .05f, new Vector3(.42f, bh, .03f), rot); for (var q = 0; q < Mathf.FloorToInt(bh / .22f); q++) if ((k + q) % 2 == 0) z.mk.Box(win, p + up * (1.36f + q * .22f) + z.side * (bx2 + ((q % 2) * .16f - .08f)) + z.front * .07f, new Vector3(.08f, .1f, .01f), rot); }
                    halos.Add(p + up * 2f + z.front * .6f, 2f, Bulb, .35f); ArtistBits.Label(z.labels, "JUNIOR'S PLACE", z.At(-Foot(z.a) * .42f, 0) + up * 3.45f + z.front * 3.1f, z.front, .26f, Cream, TextAnchor.MiddleCenter); break;
                default:
                    z.mk.Prism(trunkM, p, up, .05f, 1.2f, 5); z.mk.Prism(GlowM(Bulb, 1.4f), p + up * 1.1f, up, .09f, .22f, 6); break;
            }
        }

        Material SongGlowTin(string s) => SongGlow(s, new Color(.7f, .72f, .72f), .3f);
        Material SongGlowM(string s, Color c, float k) => SongGlow(s, c, k);
    }
}
