using System.Collections.Generic;
using UnityEngine;

namespace EverythingWorld.World
{
    /// The Power Metal Peaks' artists. Each stands on a round courtyard of flagstones ringed in gold, covers on a
    /// shield-topped board, a plaque and a torch; songs without a piece of their own get a red paper lantern on a post
    /// (SongLanterns).
    /// - DragonForce: a fire arena. A ring of eight flame jets that burst with the music, a giant gold flying-V guitar
    ///   wreathed in flame ("Through the Fire and Flames": the dragon on the castle breathes fire and the sky burns),
    ///   and at the edge a ship's bow rising from an iceberg, a railing at the very tip ("My Heart Will Go On").
    /// - BRIDEAR: a mountain stage under a great gate of red lacquer hung with neon; a little stone labyrinth glowing
    ///   violet ("In The Labyrinth"), two giant dice ("G・A・M・E"), a floating crystal with beams to four pillars
    ///   ("NEXUS"), a heart lantern ("Reason For My Life").
    /// - HANABIE.: a fireworks rack (fireworks burst over it in their songs), a neon sign, a little office desk with
    ///   its lamp left on ("お先に失礼します。", leaving work first).
    /// - LADYBABY: a giant steamed bun on a plate ("Nippon Manju"), a taiko drum marked with a lightning bolt ("Haten
    ///   ni raimei": thunder rolls and lightning strikes over the sea).
    /// - Mary's Blood: a red sports car on a launch ramp ("Driver's High"), a signpost at a railing over a drop
    ///   ("World's End").
    /// - NEMOPHILA: a field of baby-blue flowers, and orange lantern-plant lanterns hanging over it ("鬼灯").
    /// - BABYMETAL: a fox shrine (a little gate, two fox statues with glowing eyes) and a giant heart-shaped box of
    ///   chocolates ("Gimme Chocolate!!").
    /// - Alestorm: by the cove, a pirate tavern with a giant tankard on its roof ("Drink"), a sombrero on a barrel
    ///   ("Mexico"), and a giant yellow rubber duck bobbing in the water.
    /// - Wind Rose: a dwarf mine in the hillside: timber frame, a rail cart heaped with glowing ore ("Mine Mine Mine!"),
    ///   pickaxes, ale barrels round a table ("Drunken Dwarves").
    /// - Sabaton: a parachute snagged on a tall pole over a stack of supply crates ("82nd All the Way").
    public partial class PowerKit
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
                Add("DragonForce", "Through the Fire", "@the dragon castle"); Add("DragonForce", "My Heart Will Go On", "DragonForce");
                Add("HANABIE.", "お先に", "HANABIE."); Add("LADYBABY", "Haten", "@the pirate cove"); Add("Alestorm", "Drink", "@the pirate cove");
                Add("BABYMETAL", "Gimme Chocolate", "BABYMETAL");
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
            var reach = Foot(a);
            z.mk.Prism(wallStone, z.c - Vector3.up * .3f, Vector3.up, reach * .62f, .38f, 20);
            { var rr = reach * .62f - .2f; for (var k = 0; k < 32; k++) { float a0 = k * Mathf.PI / 16f, a1 = (k + 1) * Mathf.PI / 16f; var p0 = z.c + new Vector3(Mathf.Cos(a0), 0, Mathf.Sin(a0)) * rr + Vector3.up * .1f; var p1 = z.c + new Vector3(Mathf.Cos(a1), 0, Mathf.Sin(a1)) * rr + Vector3.up * .1f; z.mk.Box(goldM, (p0 + p1) / 2f, new Vector3(.2f, .05f, Vector3.Distance(p0, p1) + .02f), Quaternion.LookRotation(p1 - p0)); } }
            switch (a.name)
            {
                case "DragonForce": DragonForce(z); break;
                case "BRIDEAR": Bridear(z); break;
                case "HANABIE.": Hanabie(z); break;
                case "LADYBABY": Ladybaby(z); break;
                case "Mary's Blood": MarysBlood(z); break;
                case "NEMOPHILA": Nemophila(z); break;
                case "BABYMETAL": Babymetal(z); break;
                case "Alestorm": Alestorm(z); break;
                case "Wind Rose": WindRose(z); break;
                case "Sabaton": Sabaton(z); break;
                default: Banner(ctx, z.mk, z.At(-3f, 0), front, bannerRed, 6f); break;
            }
            SongLanterns(z);
            if (a.name != "DragonForce" && a.name != "Alestorm")
                foreach (var e in new[] { -1f, 1f }) { var bz = z.At(-reach * .2f, e * reach * .75f); Brazier(ctx, z.mk, bz); ArtistBits.PointLight(root, bz + Vector3.up * 2.5f, Fire, 12f, 1.8f); }
            Board(z, z.At(reach * .78f, -reach * .5f - 2.5f), front);
            ArtistBits.Plaque(z.mk, z.solid, labels, a, z.At(reach * .78f + 1.5f, reach * .5f + 2.5f), front, woodDark, wood, Ink, 1.6f);
            Torch(ctx, z.mk, z.At(reach * .78f, reach * .5f + 5f));
            if (z.area.glowLight == null) z.area.glowLight = ArtistBits.PointLight(root, z.c + Vector3.up * 6f + front * 4f, new Color(1f, .62f, .32f), HasStation(a) ? 24f : 18f, 2.2f);
            area.glowRenderers = new Renderer[0];
            z.mk.Build("Area", root, false);
            z.solid.BuildCollider("Walls", root);
            z.pictures.Build("Covers", root, false);
            noGrass.Add(new Vector3(z.c.x, reach * .9f, z.c.z));
            if (HasStation(a) || reach >= 10f)
            {
                var mark = new GameObject("Shot: " + a.name + " up close").transform; mark.SetParent(ctx.Root, false);
                var cam = z.At(reach * .6f, reach * .35f) + Vector3.up * 1.7f; var look = z.At(-reach * .35f, 0) + Vector3.up * 2.5f - cam; mark.SetPositionAndRotation(cam, Quaternion.LookRotation(look.normalized));
            }
        }

        /// The covers on a dark wooden board under a little peaked roof, a painted shield on top.
        void Board(Plot z, Vector3 ground, Vector3 dir)
        {
            dir = Vector3.ProjectOnPlane(dir, Vector3.up).normalized; var rot = Quaternion.LookRotation(dir); var side = Vector3.Cross(Vector3.up, dir);
            var n = Mathf.Max(1, Mathf.Min(6, z.a.songList?.Length ?? 1)); const float size = .9f; var w = n * size * 1.25f + .6f;
            var c = ground + Vector3.up * 1.7f;
            z.mk.Box(woodDark, c - Vector3.up * .5f, new Vector3(w, size + 1.6f, .25f), rot); z.solid.Box(woodDark, c - Vector3.up * .5f, new Vector3(w, size + 1.6f, .4f), rot);
            foreach (var s in new[] { -1f, 1f }) { z.mk.Box(wood, c + side * s * (w / 2f) - Vector3.up * 1.1f, new Vector3(.25f, size + 2.6f, .3f), rot); }
            z.mk.Box(goldM, c + Vector3.up * (size / 2f + .45f) + dir * .14f, new Vector3(w, .1f, .04f), rot);
            var sh = c + Vector3.up * (size / 2f + 1f);
            z.mk.Box(bannerRed, sh, new Vector3(.9f, 1f, .1f), rot); z.mk.Prism(bannerRed, sh - Vector3.up * .5f, -Vector3.up, .45f, .45f, 4, .05f, 45f);
            z.mk.Box(goldM, sh + dir * .06f, new Vector3(.12f, .8f, .02f), rot); z.mk.Box(goldM, sh + dir * .06f + Vector3.up * .15f, new Vector3(.6f, .12f, .02f), rot);
            ArtistBits.CoverWall(z.pictures, z.labels, z.ctx, z.a, c + dir * .14f + Vector3.up * .08f, dir, n, size, n, true, Ink);
        }

        /// A red paper lantern on a post for every song without a piece of its own, glowing while it plays.
        void SongLanterns(Plot z)
        {
            if (!SongIds.TryGetValue(z.a.name, out var list)) return;
            var up = Vector3.up; var k = 0; var seen = new HashSet<string>();
            foreach (var (_, id) in list)
            {
                if (z.lit.Contains(id) || !seen.Add(id)) continue;
                var p = z.At(-Foot(z.a) * .25f - (k % 2) * 1.6f, Foot(z.a) * .45f + (k / 2) * 1.8f);
                z.mk.Prism(woodDark, p - up * .2f, up, .07f, 2.4f, 6); z.mk.Box(woodDark, p + up * 2.15f + z.front * .3f, new Vector3(.06f, .06f, .7f), z.Rot);
                var lp = p + up * 1.7f + z.front * .6f;
                z.mk.Blob(SongGlow(id, new Color(1f, .3f, .2f), 1.8f), lp, new Vector3(.32f, .42f, .32f), z.ctx.Rng, 0f);
                z.mk.Prism(rockDark, lp + up * .38f, up, .18f, .08f, 8); z.mk.Prism(rockDark, lp - up * .46f, up, .18f, .08f, 8);
                halos.Add(lp, 1.2f, new Color(1f, .35f, .25f), .55f); ArtistBits.PostCollider(z.root, p, 2.2f, .12f);
                k++;
            }
        }

        // ---------------------------------------------------------------- DragonForce
        void DragonForce(Plot z)
        {
            var up = Vector3.up; var ctx = z.ctx; var ttfaf = Song(z, "Through the Fire"); var mhwgo = Song(z, "My Heart Will Go On");
            var c = z.At(-2f, 0);
            // the ring of flame jets
            for (var k = 0; k < 8; k++)
            {
                var a = k * Mathf.PI / 4f + Mathf.PI / 8f; var p = ctx.OnGround(c.x + Mathf.Cos(a) * 7.5f, c.z + Mathf.Sin(a) * 7.5f);
                if (Vector3.Dot(new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)), z.front) > .9f) continue;
                z.mk.Prism(iron, p - up * .2f, up, .55f, 1.6f, 8, .7f); z.mk.Prism(rockDark, p + up * 1.4f, up, .42f, .2f, 8);
                z.mk.Blob(SongGlow(ttfaf, Fire, 2f), p + up * 1.65f, new Vector3(.3f, .2f, .3f), ctx.Rng, .2f);
                ArtistBits.PostCollider(z.root, p, 1.6f, .55f); life.jets.Add(p + up * 1.6f); halos.Add(p + up * 2f, 1.6f, Fire, .6f);
            }
            // the flying-V, gold, wreathed in flame
            var g = c - z.front * 2f; var vRot = z.Rot;
            var gold = SongGlow(ttfaf, Gold, 1.2f);
            foreach (var e in new[] { -1f, 1f }) z.mk.Box(gold, g + up * 2.6f + z.side * e * 1.3f, new Vector3(1.1f, 4.6f, .35f), vRot * Quaternion.Euler(0, 0, e * 28f));
            z.mk.Box(woodDark, g + up * 7.2f, new Vector3(.45f, 5.6f, .25f), vRot);
            z.mk.Box(gold, g + up * 10.4f, new Vector3(1f, 1.4f, .3f), vRot * Quaternion.Euler(0, 0, 15f));
            for (var k = 0; k < 6; k++) z.mk.Box(GlowM(new Color(.95f, .95f, 1f), 1.6f), g + up * 6f + z.side * ((k - 2.5f) * .07f) + z.front * .14f, new Vector3(.02f, 9f, .02f), vRot);
            z.mk.Box(rockDark, g + up * .35f, new Vector3(3f, .7f, 1.4f), vRot); z.solid.Box(rockDark, g + up * 2f, new Vector3(3.4f, 4f, 1.4f), vRot);
            life.flameAt.Add(g + up * .9f + z.side * 1.4f); life.flameAt.Add(g + up * .9f - z.side * 1.4f);
            halos.Add(g + up * 4f + z.front * 1f, 4f, Gold, .5f);
            ArtistBits.PointLight(z.root, g + up * 4f + z.front * 4f, Fire, 22f, 2.6f);
            // the ship's bow on an iceberg ("My Heart Will Go On")
            var ice = z.At(-1f, 11.5f); var iceM = SongGlow(mhwgo, new Color(.65f, .85f, 1f), .6f);
            z.mk.Blob(iceM, ice + up * 1.2f, new Vector3(3.2f, 2f, 2.6f), ctx.Rng, .35f); z.mk.Prism(iceM, ice + up * 2f, up, 1.2f, 3f, 5, .1f);
            var hull = WorldMaterials.Solid(new Color(.1f, .1f, .12f));
            var bowDir = (z.front * .8f + up * .35f).normalized;
            z.mk.Prism(hull, ice + up * 1.5f - z.front * 1.5f, bowDir, 1.6f, 6f, 3, .05f, Quaternion.LookRotation(z.front).eulerAngles.y + 180f);
            z.mk.Box(WorldMaterials.Solid(new Color(.6f, .1f, .1f)), ice + up * 1.6f - z.front * 1.2f, new Vector3(2.2f, .5f, 2.5f), Quaternion.LookRotation(bowDir));
            var tip = ice + up * 1.5f - z.front * 1.5f + bowDir * 5.4f;
            z.mk.Box(goldM, tip + up * .9f, new Vector3(.05f, .05f, 1.6f), Quaternion.LookRotation(z.side));
            BoxCol(z.root, ice + up * 1.4f, new Vector3(5f, 2.8f, 4.2f), z.Rot);
            halos.Add(ice + up * 2f, 3.4f, new Color(.6f, .85f, 1f), .4f);
            ArtistBits.PointLight(z.root, ice + up * 3f + z.front * 3f, new Color(.6f, .85f, 1f), 12f, 1.6f);
        }

        // ---------------------------------------------------------------- BRIDEAR
        void Bridear(Plot z)
        {
            var up = Vector3.up; var ctx = z.ctx; var rot = z.Rot;
            var lacquer = WorldMaterials.Solid(new Color(.75f, .12f, .1f));
            // the stage and the great gate over it
            var s = z.At(-9f, 0);
            z.mk.Box(rockDark, s + up * .6f, new Vector3(12f, 1.2f, 6f), rot); z.solid.Box(rockDark, s + up * .6f, new Vector3(12f, 1.2f, 6f), rot);
            z.mk.Box(neonPink, s + up * 1.22f + z.front * 3.02f, new Vector3(12f, .08f, .04f), rot);
            foreach (var e in new[] { -1f, 1f }) { z.mk.Prism(lacquer, s + z.side * e * 5.4f - up * .2f, up, .45f, 9.2f, 10); ArtistBits.PostCollider(z.root, s + z.side * e * 5.4f, 9f, .5f); }
            z.mk.Box(lacquer, s + up * 7.6f, new Vector3(12.4f, .6f, .8f), rot);
            z.mk.Box(rockDark, s + up * 8.6f, new Vector3(14f, .6f, 1f), rot * Quaternion.Euler(0, 0, 0));
            foreach (var e in new[] { -1f, 1f }) z.mk.Box(rockDark, s + up * 8.75f + z.side * e * 7.1f, new Vector3(1.6f, .4f, 1f), rot * Quaternion.Euler(0, 0, e * 14f));
            z.mk.Box(rockDark, s + up * 6.2f, new Vector3(1.8f, 2.2f, .3f), rot);
            ArtistBits.Label(z.labels, "BRIDEAR", s + up * 6.2f + z.front * .17f, z.front, .42f, Pink, TextAnchor.MiddleCenter);
            for (var k = 0; k < 9; k++) z.mk.Blob(k % 2 == 0 ? neonPink : neonCyan, s + up * 7.15f + z.side * ((k - 4f) * 1.3f) + z.front * .45f, Vector3.one * .14f, ctx.Rng, 0f);
            foreach (var e in new[] { -1f, 1f }) for (var j = 0; j < 2; j++) { var p = s - z.front * 2f + z.side * e * (2.6f + j * 1.6f) + up * (1.2f + .7f); z.mk.Box(rockDark, p, new Vector3(1.4f, 1.4f, .8f), rot); z.mk.Box(iron, p + z.front * .41f, new Vector3(1.2f, 1.2f, .02f), rot); }
            halos.Add(s + up * 7.2f + z.front * 1f, 7f, Pink, .3f);
            ArtistBits.PointLight(z.root, s + up * 5f + z.front * 4f, Pink, 22f, 2f);
            // "In The Labyrinth": a little stone maze, its walls topped with violet light
            var lab = z.At(1f, -10f); var violet = SongGlow(z, "In The Labyrinth", new Color(.7f, .4f, 1f), 1.6f);
            (float x, float y, float l, bool along)[] walls = { (-2.4f, 0, 5f, true), (2.4f, 0, 5f, true), (0, -2.4f, 3.4f, false), (-.8f, 2.4f, 3.2f, false), (-.8f, .4f, 2.6f, true), (.9f, -.7f, 2.6f, true), (0, 1.1f, 1.6f, false) };
            foreach (var (x, y, len, along) in walls)
            {
                var p = lab + z.side * x + z.front * y + up * .7f; var size = along ? new Vector3(.4f, 1.4f, len) : new Vector3(len, 1.4f, .4f);
                z.mk.Box(wallStone, p, size, rot); z.solid.Box(wallStone, p, size, rot); z.mk.Box(violet, p + up * .73f, new Vector3(size.x, .06f, size.z), rot);
            }
            halos.Add(lab + up * 1.5f, 3.4f, new Color(.7f, .4f, 1f), .35f);
            // "G・A・M・E": two giant dice
            var gm = SongGlow(z, "G・A・M・E", Ink, 1.4f);
            foreach (var (o, yaw, n) in new[] { (-1.2f, 20f, 5), (1.3f, -35f, 3) })
            {
                var d = z.At(-1f, 10f + o) + up * 1f; var dr = Quaternion.Euler(0, yaw, 0) * rot;
                z.mk.Box(WorldMaterials.Solid(new Color(.95f, .93f, .9f)), d, Vector3.one * 2f, dr); z.solid.Box(cloth, d, Vector3.one * 2f, dr);
                var top = d + up * 1.01f; var dx = dr * Vector3.right; var dz = dr * Vector3.forward;
                var pips = n == 5 ? new[] { (0f, 0f), (-.5f, -.5f), (.5f, .5f), (-.5f, .5f), (.5f, -.5f) } : new[] { (0f, 0f), (-.5f, -.5f), (.5f, .5f) };
                foreach (var (px, pz) in pips) z.mk.Prism(gm, top + dx * px + dz * pz, up, .18f, .02f, 10);
                foreach (var face in new[] { dz, -dx }) z.mk.Prism(gm, d + face * 1.01f, face, .18f, .02f, 10);
            }
            halos.Add(z.At(-1f, 10f) + up * 2f, 3f, Ink, .3f);
            // "NEXUS": a floating crystal with beams to four pillars
            var nx = z.At(-12f, 9f); var nexus = SongGlow(z, "NEXUS", NeonCyan, 2f);
            z.mk.Prism(nexus, nx + up * 4f, up, .7f, 1.4f, 4, .02f, 45f); z.mk.Prism(nexus, nx + up * 4f, -up, .7f, 1.2f, 4, .02f, 45f);
            for (var k = 0; k < 4; k++)
            {
                var a = k * Mathf.PI / 2f + Mathf.PI / 4f; var p = ctx.OnGround(nx.x + Mathf.Cos(a) * 3f, nx.z + Mathf.Sin(a) * 3f);
                z.mk.Prism(rockDark, p - up * .2f, up, .3f, 2.4f, 6); ArtistBits.PostCollider(z.root, p, 2.2f, .3f);
                var b0 = p + up * 2.3f; var b1 = nx + up * 4f; z.mk.Box(nexus, (b0 + b1) / 2f, new Vector3(.06f, .06f, Vector3.Distance(b0, b1)), Quaternion.LookRotation(b1 - b0));
            }
            halos.Add(nx + up * 4f, 2.6f, NeonCyan, .6f);
            // "Reason For My Life": a heart lantern on a pole
            var hp = z.At(3f, 10.5f); var heartM = SongGlow(z, "Reason For My Life", Pink, 2f);
            z.mk.Prism(iron, hp - up * .2f, up, .07f, 3.4f, 6); ArtistBits.PostCollider(z.root, hp, 3.2f, .12f);
            var hc = hp + up * 3.9f; foreach (var e in new[] { -1f, 1f }) z.mk.Blob(heartM, hc + z.side * e * .3f + up * .2f, Vector3.one * .4f, ctx.Rng, 0f); z.mk.Prism(heartM, hc + up * .1f, -up, .55f, .75f, 4, .05f, Quaternion.LookRotation(z.front).eulerAngles.y + 45f);
            halos.Add(hc, 1.8f, Pink, .6f);
        }

        // ---------------------------------------------------------------- the Japanese bands
        void Hanabie(Plot z)
        {
            var up = Vector3.up; var ctx = z.ctx; var rot = z.Rot;
            var rack = z.At(-3.5f, 0);
            z.mk.Box(wood, rack + up * .5f, new Vector3(3.6f, 1f, 1.6f), rot); z.solid.Box(wood, rack + up * .5f, new Vector3(3.6f, 1f, 1.6f), rot);
            for (var k = 0; k < 8; k++) { var p = rack + up * 1f + z.side * ((k % 4 - 1.5f) * .8f) + z.front * ((k / 4 - .5f) * .7f); z.mk.Prism(k % 3 == 0 ? neonPink : bannerRed, p, (up + z.side * ((k % 4 - 1.5f) * .06f)).normalized, .14f, 1.3f, 8); }
            life.fireworksAt.Add(rack + up * 2f); life.fireworksSongs.Add("HANABIE.");
            var sign = z.At(-4f, 4f) + up * 3.6f;
            z.mk.Prism(iron, sign - up * 3.8f, up, .08f, 3f, 6); ArtistBits.PostCollider(z.root, sign - up * 3.6f, 3f, .12f);
            z.mk.Box(rockDark, sign, new Vector3(3.4f, 1.2f, .15f), rot);
            foreach (var y in new[] { -.6f, .6f }) z.mk.Box(neonPink, sign + up * y + z.front * .09f, new Vector3(3.4f, .07f, .04f), rot);
            ArtistBits.Label(z.labels, "HANABIE.", sign + z.front * .1f, z.front, .5f, Pink, TextAnchor.MiddleCenter);
            halos.Add(sign + z.front * .6f, 2.4f, Pink, .5f);
            // leaving work first: a little office desk with its lamp left on, the chair pushed back
            var desk = z.At(-1f, -4.5f); var lamp = SongGlow(z, "お先に", new Color(1f, .9f, .6f), 2f);
            var grey = WorldMaterials.Solid(new Color(.55f, .56f, .58f));
            z.mk.Box(grey, desk + up * .75f, new Vector3(1.6f, .08f, .8f), rot); foreach (var e in new[] { -1f, 1f }) z.mk.Box(grey, desk + up * .37f + z.side * e * .75f, new Vector3(.06f, .75f, .75f), rot);
            z.mk.Box(rockDark, desk + up * 1.05f - z.front * .2f, new Vector3(.6f, .45f, .05f), rot); z.mk.Box(GlowM(new Color(.5f, .7f, 1f), 1.2f), desk + up * 1.05f - z.front * .17f, new Vector3(.52f, .38f, .02f), rot);
            z.mk.Prism(iron, desk + up * .8f + z.side * .6f, up, .03f, .45f, 6); z.mk.Prism(lamp, desk + up * 1.2f + z.side * .6f, up, .14f, .14f, 8, .5f);
            z.mk.Box(rockDark, desk + up * .45f + z.front * 1f + z.side * .3f, new Vector3(.5f, .08f, .5f), rot * Quaternion.Euler(0, 25f, 0));
            BoxCol(z.root, desk + up * .5f, new Vector3(1.6f, 1f, .8f), rot);
            halos.Add(desk + up * 1.3f + z.side * .6f, 1f, new Color(1f, .9f, .6f), .6f);
        }

        void Ladybaby(Plot z)
        {
            var up = Vector3.up; var ctx = z.ctx; var rot = z.Rot;
            // "Nippon Manju": a giant steamed bun on a plate
            var bun = z.At(-3.5f, -2.2f);
            z.mk.Prism(WorldMaterials.Solid(new Color(.92f, .92f, .95f)), bun + up * .3f, up, 2.4f, .2f, 20);
            z.mk.Blob(WorldMaterials.Solid(new Color(.98f, .94f, .86f)), bun + up * 1.2f, new Vector3(1.9f, 1.1f, 1.9f), ctx.Rng, .05f);
            z.mk.Prism(SongGlow(z, "Nippon Manju", Pink, 1.8f), bun + up * 2.25f, up, .3f, .05f, 12);
            BoxCol(z.root, bun + up * 1f, new Vector3(3.6f, 2f, 3.6f), rot); halos.Add(bun + up * 2.4f, 2.4f, Pink, .4f);
            // "Haten ni raimei": a taiko drum marked with a lightning bolt
            var drum = z.At(-3.5f, 3.2f); var drumRot = Quaternion.LookRotation(z.side);
            z.mk.Prism(WorldMaterials.Solid(new Color(.55f, .2f, .12f)), drum + up * 1.5f - z.side * .9f, z.side, 1.2f, 1.8f, 16, 1f);
            foreach (var e in new[] { -1f, 1f }) z.mk.Prism(WorldMaterials.Solid(new Color(.95f, .9f, .78f)), drum + up * 1.5f + z.side * e * .92f, z.side * e, 1.15f, .03f, 16);
            var bolt = SongGlow(z, "Haten", new Color(1f, .95f, .4f), 2.2f);
            var bf = drum + up * 1.5f + z.side * .96f;
            z.mk.Box(bolt, bf + up * .35f + z.front * .1f, new Vector3(.03f, .8f, .2f), drumRot * Quaternion.Euler(0, 0, 0) * Quaternion.Euler(25f, 0, 0));
            z.mk.Box(bolt, bf - up * .3f - z.front * .1f, new Vector3(.03f, .8f, .2f), drumRot * Quaternion.Euler(25f, 0, 0));
            z.mk.Box(woodDark, drum + up * .4f, new Vector3(1.6f, .8f, .6f), rot); BoxCol(z.root, drum + up * 1.3f, new Vector3(2.4f, 2.6f, 2.4f), rot);
            halos.Add(bf + z.side * .4f, 1.6f, new Color(1f, .95f, .4f), .5f);
            KawaiiSign(z.ctx, z.mk, z.At(-6f, 0));
        }

        void MarysBlood(Plot z)
        {
            var up = Vector3.up; var ctx = z.ctx; var rot = z.Rot;
            // "Driver's High": a red sports car on a launch ramp, speed lines behind it
            var ramp = z.At(-3f, -1f); var rr = Quaternion.LookRotation(z.side) * Quaternion.Euler(-14f, 0, 0);
            z.mk.Box(iron, ramp + up * .6f, new Vector3(3f, .2f, 6f), rr); z.solid.Box(iron, ramp + up * .6f, new Vector3(3f, .6f, 6f), rr);
            var car = ramp + up * 1.5f + z.side * .4f; var cr = rr; var red = WorldMaterials.Solid(new Color(.8f, .1f, .1f), .9f);
            z.mk.Box(red, car, new Vector3(1.9f, .6f, 4.2f), cr); z.mk.Box(red, car + cr * new Vector3(0, .5f, -.3f), new Vector3(1.6f, .45f, 1.8f), cr);
            z.mk.Box(rockDark, car + cr * new Vector3(0, .5f, -.3f), new Vector3(1.64f, .32f, 1.6f), cr);
            foreach (var e in new[] { -.6f, .6f }) z.mk.Box(SongGlow(z, "Driver", new Color(1f, .95f, .8f), 2.4f), car + cr * new Vector3(e, .05f, 2.11f), new Vector3(.35f, .15f, .03f), cr);
            for (var k = 0; k < 4; k++) z.mk.Box(neonCyan, car - z.side * (3.4f + k * .5f) + up * (-.2f + k * .25f), new Vector3(.05f, .05f, 2f + k * .5f), Quaternion.LookRotation(z.side));
            halos.Add(car + z.side * 2.6f, 2f, new Color(1f, .95f, .8f), .5f);
            // "World's End": a railing at the edge of the courtyard with a signpost pointing out over the drop
            var edge = z.At(-6f, 3f); var we = SongGlow(z, "World", Pink, 1.6f);
            for (var k = -2; k <= 2; k++) { var p = edge + z.side * k * 1.2f; z.mk.Prism(iron, p - up * .2f, up, .05f, 1.2f, 6); ArtistBits.PostCollider(z.root, p, 1f, .08f); }
            z.mk.Box(iron, edge + up * 1f, new Vector3(4.8f, .06f, .06f), rot);
            var post = edge + z.front * 1.4f; z.mk.Prism(woodDark, post - up * .2f, up, .08f, 3f, 6); ArtistBits.PostCollider(z.root, post, 2.8f, .1f);
            z.mk.Box(wood, post + up * 2.5f - z.front * .6f, new Vector3(.3f, .4f, 1.6f), rot); z.mk.Prism(wood, post + up * 2.5f - z.front * 1.4f, -z.front, .28f, .3f, 4, .05f, 45f);
            ArtistBits.Label(z.labels, "WORLD'S END", post + up * 2.5f - z.front * .6f + z.side * .16f, z.side, .14f, Pink, TextAnchor.MiddleCenter);
            z.mk.Box(we, post + up * 2.78f - z.front * .6f, new Vector3(.32f, .04f, 1.6f), rot);
            halos.Add(post + up * 2.6f, 1.4f, Pink, .45f);
        }

        void Nemophila(Plot z)
        {
            var up = Vector3.up; var ctx = z.ctx;
            var blue = WorldMaterials.Solid(new Color(.45f, .7f, 1f)); var blueGlow = GlowM(new Color(.5f, .75f, 1f), .9f);
            var f = z.At(-3f, 0);
            for (var k = 0; k < 140; k++)
            {
                var a = ctx.Range(0, Mathf.PI * 2f); var r = Mathf.Sqrt(ctx.Range(0f, 1f)) * 4.6f; var p = ctx.OnGround(f.x + Mathf.Cos(a) * r, f.z + Mathf.Sin(a) * r);
                z.mk.Prism(WorldMaterials.Solid(new Color(.2f, .35f, .2f)), p, up, .02f, .3f, 4);
                z.mk.Blob(k % 5 == 0 ? blueGlow : blue, p + up * .33f, new Vector3(.12f, .05f, .12f), ctx.Rng, 0f);
            }
            halos.Add(f + up * .5f, 4.6f, new Color(.5f, .75f, 1f), .2f);
            // "鬼灯": lantern-plant lanterns hanging from a line over the field
            var hz = SongGlow(z, "鬼灯", new Color(1f, .5f, .15f), 2f);
            var a0 = f + z.side * -5f; var a1 = f + z.side * 5f;
            foreach (var e in new[] { a0, a1 }) { z.mk.Prism(woodDark, e - up * .2f, up, .1f, 4f, 6); ArtistBits.PostCollider(z.root, e, 3.8f, .12f); }
            for (var k = 1; k < 10; k++)
            {
                var t = k / 10f; var p = Vector3.Lerp(a0, a1, t) + up * (3.8f - Mathf.Sin(t * Mathf.PI) * .7f);
                z.mk.Box(rockDark, p + up * .2f, new Vector3(.02f, .4f, .02f), Quaternion.identity);
                z.mk.Prism(hz, p - up * .05f, -up, .26f, .55f, 5, .15f);
                halos.Add(p - up * .3f, .8f, new Color(1f, .5f, .15f), .55f);
            }
            for (var k = 0; k < 20; k++) { var p0 = Vector3.Lerp(a0, a1, k / 20f) + up * (3.8f - Mathf.Sin(k / 20f * Mathf.PI) * .7f) + up * .4f; var p1 = Vector3.Lerp(a0, a1, (k + 1) / 20f) + up * (3.8f - Mathf.Sin((k + 1) / 20f * Mathf.PI) * .7f) + up * .4f; z.mk.Box(rockDark, (p0 + p1) / 2f, new Vector3(.02f, .02f, Vector3.Distance(p0, p1)), Quaternion.LookRotation(p1 - p0)); }
        }

        void Babymetal(Plot z)
        {
            var up = Vector3.up; var ctx = z.ctx; var rot = z.Rot;
            var red = WorldMaterials.Solid(new Color(.8f, .1f, .1f)); var white = WorldMaterials.Solid(new Color(.95f, .94f, .92f));
            // the fox shrine: a small red gate, two white foxes with glowing eyes either side of a little altar
            var sh = z.At(-4.5f, -2f);
            foreach (var e in new[] { -1f, 1f }) { z.mk.Prism(red, sh + z.side * e * 1.4f - up * .2f, up, .16f, 3.2f, 8); ArtistBits.PostCollider(z.root, sh + z.side * e * 1.4f, 3f, .18f); }
            z.mk.Box(red, sh + up * 2.8f, new Vector3(3.6f, .25f, .3f), rot); z.mk.Box(rockDark, sh + up * 3.2f, new Vector3(4.2f, .25f, .4f), rot);
            z.mk.Box(wood, sh - z.front * 1.2f + up * .5f, new Vector3(1.2f, 1f, .7f), rot); BoxCol(z.root, sh - z.front * 1.2f + up * .5f, new Vector3(1.2f, 1f, .7f), rot);
            var eyes = GlowM(new Color(1f, .3f, .2f), 2.4f);
            foreach (var e in new[] { -1f, 1f })
            {
                var fx = sh - z.front * .6f + z.side * e * 2.4f;
                z.mk.Box(rock, fx + up * .3f, new Vector3(.8f, .6f, .8f), rot);
                z.mk.Box(white, fx + up * 1f, new Vector3(.5f, .8f, .6f), rot); z.mk.Box(white, fx + up * 1.6f + z.front * .15f, new Vector3(.4f, .4f, .5f), rot);
                z.mk.Prism(white, fx + up * 1.6f + z.front * .4f, z.front, .14f, .35f, 4, .05f);
                foreach (var s in new[] { -1f, 1f }) { z.mk.Prism(white, fx + up * 1.8f + z.side * s * .14f, up, .08f, .3f, 4, .05f); z.mk.Box(eyes, fx + up * 1.68f + z.front * .41f + z.side * s * .1f, new Vector3(.06f, .04f, .02f), rot); }
                z.mk.Box(red, fx + up * 1.3f + z.front * .3f, new Vector3(.55f, .14f, .1f), rot);
                BoxCol(z.root, fx + up * .9f, new Vector3(.8f, 1.8f, .8f), rot);
                halos.Add(fx + up * 1.7f + z.front * .5f, .6f, new Color(1f, .3f, .2f), .6f);
            }
            // "Gimme Chocolate!!": a giant heart-shaped box of chocolates, lid tipped back
            var box = z.At(-1f, 4.5f); var pink = SongGlow(z, "Gimme Chocolate", Pink, 1.2f);
            var choc = WorldMaterials.Solid(new Color(.3f, .15f, .08f));
            var yaw = Quaternion.LookRotation(z.front).eulerAngles.y;
            foreach (var e in new[] { -1f, 1f }) z.mk.Prism(pink, box + z.side * e * .55f, up, 1.1f, .6f, 16);
            z.mk.Prism(pink, box - z.front * .6f, up, 1.5f, .6f, 4, 1f, yaw + 45f);
            for (var k = 0; k < 7; k++) z.mk.Blob(choc, box + up * .65f + z.side * ((k % 3 - 1f) * .7f) - z.front * ((k / 3) * .55f), new Vector3(.25f, .15f, .25f), ctx.Rng, 0f);
            z.mk.Box(pink, box + up * 1.6f + z.front * 1.3f, new Vector3(2.8f, 2.2f, .12f), rot * Quaternion.Euler(-20f, 0, 0));
            BoxCol(z.root, box + up * .4f, new Vector3(3.2f, .8f, 2.6f), rot);
            halos.Add(box + up * 1.2f, 2.4f, Pink, .45f);
        }

        // ---------------------------------------------------------------- the pirates, the dwarves, the paratroopers
        void Alestorm(Plot z)
        {
            var up = Vector3.up; var ctx = z.ctx; var rot = z.Rot;
            // the tavern: a timber-framed house, lit windows, a sign, a giant tankard on the roof ("Drink")
            var t = z.At(-5f, 0); const float W = 9f, D = 6f, H = 4.2f;
            z.mk.Box(cloth, t + up * H / 2f, new Vector3(W, H, D), rot); z.solid.Box(cloth, t + up * H / 2f, new Vector3(W, H, D), rot);
            foreach (var e in new[] { -1f, 0f, 1f }) z.mk.Box(woodDark, t + z.side * e * (W / 2f - .1f) + z.front * (D / 2f + .02f) + up * H / 2f, new Vector3(.3f, H, .06f), rot);
            z.mk.Box(woodDark, t + z.front * (D / 2f + .02f) + up * (H - .2f), new Vector3(W, .3f, .06f), rot);
            z.mk.GableRoof(woodDark, t + up * H, new Vector3(D + .8f, 0, W + .8f), 2.6f, Quaternion.LookRotation(z.side));
            var win = GlowM(new Color(1f, .72f, .4f), 2f);
            foreach (var e in new[] { -2.6f, 2.6f }) z.mk.Box(win, t + z.side * e + z.front * (D / 2f + .03f) + up * 2f, new Vector3(1.4f, 1f, .04f), rot);
            z.mk.Box(woodDark, t + z.front * (D / 2f + .03f) + up * 1.1f, new Vector3(1.2f, 2.2f, .05f), rot);
            var sign = t + z.front * (D / 2f + .8f) + up * 3.2f + z.side * 1.2f; z.mk.Box(iron, sign + up * .5f - z.front * .4f, new Vector3(.05f, .05f, .9f), rot); z.mk.Box(wood, sign, new Vector3(1.4f, .9f, .08f), rot);
            ArtistBits.Label(z.labels, "THE DRUNKEN DUCK", sign + z.front * .05f, z.front, .1f, Ink, TextAnchor.MiddleCenter);
            var mug = t + up * (H + 3.4f); var drink = SongGlow(z, "Drink", Gold, 1.4f);
            z.mk.Prism(wood, mug - up * .8f, up, 1.2f, 2.4f, 12); foreach (var y in new[] { -.5f, .9f }) z.mk.Prism(iron, mug + up * y, up, 1.24f, .14f, 12);
            z.mk.Blob(WorldMaterials.Solid(new Color(.98f, .96f, .9f)), mug + up * 1.7f, new Vector3(1.3f, .5f, 1.3f), ctx.Rng, .2f);
            z.mk.Prism(drink, mug + up * 1.56f, up, 1.1f, .02f, 12);
            z.mk.Box(wood, mug + z.side * 1.5f, new Vector3(.3f, 1.6f, .4f), rot);
            halos.Add(mug + up * 1f, 2.8f, Gold, .4f);
            ArtistBits.PointLight(z.root, t + z.front * (D / 2f + 3f) + up * 2.5f, new Color(1f, .7f, .4f), 16f, 2f);
            // "Mexico": a sombrero on a barrel by the door
            var bar = z.At(-1f, -4f); z.mk.Prism(wood, bar, up, .5f, 1.1f, 10); ArtistBits.PostCollider(z.root, bar, 1.1f, .5f);
            var mex = SongGlow(z, "Mexico", new Color(1f, .8f, .3f), 1.2f);
            z.mk.Prism(mex, bar + up * 1.12f, up, 1.1f, .05f, 16); z.mk.Prism(mex, bar + up * 1.15f, up, .4f, .5f, 12, .6f);
            // the giant rubber duck, bobbing in the cove
            if (hasCove)
            {
                var sea = new Vector3(seaDir2.x, 0, seaDir2.y); var dAt = new Vector3(cove2.x, ctx.Data.layout.waterLevel, cove2.y) + sea * 22f - Vector3.Cross(up, sea) * 10f;
                var dk = new MeshKit(); var yellow = WorldMaterials.Solid(new Color(1f, .85f, .1f)); var orange = WorldMaterials.Solid(new Color(1f, .5f, .1f));
                dk.Blob(yellow, up * 1.2f, new Vector3(3.2f, 2f, 4f), ctx.Rng, 0f); dk.Blob(yellow, up * 3.6f + Vector3.forward * 1.8f, new Vector3(1.8f, 1.8f, 1.8f), ctx.Rng, 0f);
                dk.Box(orange, up * 3.3f + Vector3.forward * 3.6f, new Vector3(1.2f, .35f, 1.2f), Quaternion.identity);
                foreach (var e in new[] { -.7f, .7f }) dk.Blob(WorldMaterials.Solid(new Color(.05f, .05f, .05f)), up * 4f + Vector3.forward * 3.2f + Vector3.right * e, Vector3.one * .2f, ctx.Rng, 0f);
                var go = dk.BuildSingle("The giant rubber duck", life.transform, false);
                if (go != null) { go.transform.SetPositionAndRotation(dAt, Quaternion.LookRotation(-sea)); life.duck = go.transform; life.duckAt = dAt; }
                halos.Add(dAt + up * 3f, 4f, new Color(1f, .85f, .3f), .25f);
            }
        }

        void WindRose(Plot z)
        {
            var up = Vector3.up; var ctx = z.ctx; var rot = z.Rot;
            // the mine mouth: a mound of rock with a timber-framed tunnel glowing inside
            var m = z.At(-6f, 0);
            z.mk.Blob(rock, m + up * 1.5f - z.front * 4.6f, new Vector3(6f, 4.2f, 4.5f), ctx.Rng, .25f); BoxCol(z.root, m + up * 2f - z.front * 4.8f, new Vector3(9f, 4f, 5f), rot);
            foreach (var e in new[] { -1f, 1f }) z.mk.Box(wood, m + z.side * e * 1.4f + up * 1.6f + z.front * .6f, new Vector3(.4f, 3.2f, .4f), rot);
            z.mk.Box(wood, m + up * 3.3f + z.front * .6f, new Vector3(3.6f, .45f, .5f), rot);
            z.mk.Box(rockDark, m + up * 1.5f + z.front * .3f, new Vector3(2.4f, 3f, .3f), rot);
            var ore = SongGlow(z, "Mine Mine Mine", new Color(1f, .75f, .25f), 2.2f);
            z.mk.Box(GlowM(new Color(1f, .6f, .25f), 1f), m + up * 1.3f + z.front * .46f, new Vector3(1.8f, 2.4f, .02f), rot);
            ArtistBits.Label(z.labels, "MINE MINE MINE", m + up * 3.3f + z.front * .87f, z.front, .2f, Ink, TextAnchor.MiddleCenter);
            // rails out of the mine and a cart heaped with glowing ore
            for (var k = 0; k < 8; k++) { var p = m + z.front * (1.2f + k * .9f); z.mk.Box(woodDark, ctx.OnGround(p.x, p.z) + up * .05f, new Vector3(1.6f, .1f, .25f), rot); }
            foreach (var e in new[] { -.55f, .55f }) z.mk.Box(iron, m + z.front * 4.4f + z.side * e + up * .14f, new Vector3(.08f, .08f, 7.4f), rot);
            var cart = m + z.front * 4.6f + up * .7f;
            z.mk.Box(iron, cart, new Vector3(1.5f, .9f, 1.9f), rot); foreach (var e in new[] { -1f, 1f }) foreach (var f in new[] { -.6f, .6f }) z.mk.Prism(rockDark, cart + z.side * e * .76f + z.front * f - up * .4f, z.side * e, .25f, .1f, 10);
            for (var k = 0; k < 7; k++) z.mk.Blob(ore, cart + up * .5f + z.side * ((k % 3 - 1) * .4f) + z.front * ((k / 3 - 1) * .5f), Vector3.one * .3f, ctx.Rng, .3f);
            BoxCol(z.root, cart, new Vector3(1.5f, 1.4f, 1.9f), rot); halos.Add(cart + up * .9f, 1.8f, new Color(1f, .75f, .25f), .6f);
            foreach (var e in new[] { -1f, 1f }) { var pk = m + z.front * 1.6f + z.side * e * 2.3f; z.mk.Box(woodDark, pk + up * .8f, new Vector3(.08f, 1.6f, .08f), rot * Quaternion.Euler(0, 0, e * 15f)); z.mk.Box(iron, pk + up * 1.55f + z.side * e * .2f, new Vector3(.9f, .12f, .12f), rot * Quaternion.Euler(0, 0, e * 15f + 10f)); }
            // "Drunken Dwarves": ale barrels round a table, tankards on it
            var tab = z.At(1.5f, -5f); var ale = SongGlow(z, "Drunken Dwarves", Gold, 1.6f);
            z.mk.Prism(wood, tab + up * .8f, up, 1.1f, .12f, 10); z.mk.Prism(woodDark, tab, up, .2f, .8f, 6); ArtistBits.PostCollider(z.root, tab, .9f, 1f);
            for (var k = 0; k < 3; k++) { var mp = tab + up * .92f + new Vector3(Mathf.Cos(k * 2.1f), 0, Mathf.Sin(k * 2.1f)) * .55f; z.mk.Prism(wood, mp, up, .12f, .3f, 8); z.mk.Prism(ale, mp + up * .28f, up, .1f, .02f, 8); }
            for (var k = 0; k < 4; k++) { var a = k * Mathf.PI / 2f + .4f; var b = ctx.OnGround(tab.x + Mathf.Cos(a) * 1.9f, tab.z + Mathf.Sin(a) * 1.9f); z.mk.Prism(wood, b, up, .4f, .8f, 10); z.mk.Prism(iron, b + up * .2f, up, .42f, .07f, 10); ArtistBits.PostCollider(z.root, b, .8f, .4f); }
            halos.Add(tab + up * 1.2f, 1.8f, Gold, .45f);
            ArtistBits.PointLight(z.root, m + up * 2.5f + z.front * 4f, new Color(1f, .7f, .35f), 16f, 2.2f);
        }

        void Sabaton(Plot z)
        {
            var up = Vector3.up; var ctx = z.ctx; var rot = z.Rot;
            var pole = z.At(-3f, 0); const float PH = 9f;
            z.mk.Prism(woodDark, pole - up * .3f, up, .2f, PH + .3f, 8); ArtistBits.PostCollider(z.root, pole, PH, .22f);
            var canopy = SongGlow(z, "82nd", new Color(.85f, .82f, .65f), .6f);
            var top = pole + up * (PH + .2f) + z.side * .8f;
            for (var k = 0; k < 10; k++) { var a = k * Mathf.PI / 5f; var d = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)); z.mk.Tri(canopy, top + up * 1.2f, top + d * 3.2f - up * (.4f + (k % 3) * .3f), top + new Vector3(Mathf.Cos(a + Mathf.PI / 5f), 0, Mathf.Sin(a + Mathf.PI / 5f)) * 3.2f - up * (.4f + ((k + 1) % 3) * .3f), top + up * 3f); z.mk.Tri(canopy, top + up * 1.2f, top + new Vector3(Mathf.Cos(a + Mathf.PI / 5f), 0, Mathf.Sin(a + Mathf.PI / 5f)) * 3.2f - up * (.4f + ((k + 1) % 3) * .3f), top + d * 3.2f - up * (.4f + (k % 3) * .3f), top - up * 3f); }
            for (var k = 0; k < 6; k++) { var a = k * Mathf.PI / 3f; var e = top + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * 3f - up * .6f; var b = pole + up * 4.5f + z.front * .3f; z.mk.Box(cloth, (e + b) / 2f, new Vector3(.02f, .02f, Vector3.Distance(e, b)), Quaternion.LookRotation(b - e)); }
            var olive = WorldMaterials.Solid(new Color(.3f, .33f, .2f));
            for (var k = 0; k < 5; k++) { var p = pole + z.side * ((k % 3 - 1) * 1.1f) + z.front * (1.4f + (k / 3) * 1.1f) + up * (.4f + (k / 3) * 0f); z.mk.Box(olive, ctx.OnGround(p.x, p.z) + up * .4f, new Vector3(1f, .8f, 1f), Quaternion.Euler(0, k * 17f, 0) * rot); BoxCol(z.root, ctx.OnGround(p.x, p.z) + up * .4f, new Vector3(1f, .8f, 1f), Quaternion.Euler(0, k * 17f, 0) * rot); }
            halos.Add(top, 3.2f, new Color(1f, .85f, .6f), .25f);
            Banner(ctx, z.mk, z.At(-3.5f, -4f), z.front, bannerRed, 5f);
        }
    }
}
