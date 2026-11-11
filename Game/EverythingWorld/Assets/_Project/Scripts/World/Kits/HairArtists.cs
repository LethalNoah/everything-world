using System.Collections.Generic;
using UnityEngine;

namespace EverythingWorld.World
{
    /// The Strip's venues: each artist has a club front facing the boulevard, open across the front with a stage
    /// you can walk onto, a marquee of chasing bulbs, their name in neon, a leopard-print awning, and their own big
    /// sign on the roof:
    /// - Guns N' Roses: a giant neon rose, a grand piano on the stage under a lone lamp ("November Rain").
    /// - Aerosmith: neon wings, and a glowing asteroid hanging over the roof ("I Don't Want to Miss a Thing").
    /// - Mötley Crüe: a neon heart with a heartbeat line, a chopper on a launch ramp out front ("Kickstart My Heart").
    /// - Twisted Sister: a giant make-up mirror ringed with bulbs. Bon Jovi: a spinning neon star. Europe: a giant
    ///   red countdown display and a synth stack ("The Final Countdown"). Quiet Riot: a wall of speakers. Slash: a
    ///   giant top hat. Van Halen: a giant striped guitar. Warrant: a giant cherry pie. Others: a neon bolt.
    public partial class HairKit
    {
        class Spot
        {
            public LandContext ctx; public Transform root, labels; public MeshKit mk, solid, pictures;
            public IslandWorldData.Artist a; public Vector3 c, front, side, roof, stage; public List<Renderer> glows; public ArtistArea area; public float W, H;
        }

        public override void BuildArtist(LandContext ctx, IslandWorldData.Artist a)
        {
            if (asphalt == null) MakeMaterials();
            var root = new GameObject("Artist: " + a.name).transform; root.SetParent(ctx.Root, false);
            var front = hasStreet ? ArtistFront(ctx.Data, a) : new Vector3(center.x - a.x, 0, center.z - a.z).normalized;
            if (front.sqrMagnitude < .01f) front = Vector3.forward;
            var labels = new GameObject("Labels").transform; labels.SetParent(root, false);
            var area = root.gameObject.AddComponent<ArtistArea>();
            area.artistName = a.name; area.stationId = a.stationId; area.radius = a.r; area.glowColor = Pink;
            var big = HasStation(a);
            var z = new Spot { ctx = ctx, root = root, labels = labels, mk = new MeshKit(), solid = new MeshKit(), pictures = new MeshKit(), a = a, front = front, side = Vector3.Cross(Vector3.up, front), glows = new List<Renderer>(), area = area, W = a.name == "Guns N' Roses" ? 22f : big ? 18f : 13f, H = a.name == "Guns N' Roses" ? 13f : big ? 11f : 8f };
            var g = ctx.OnGround(a.x, a.z); z.c = new Vector3(a.x, g.y, a.z);
            Venue(z);
            switch (a.name)
            {
                case "Guns N' Roses": Rose(z); break;
                case "Aerosmith": Wings(z); break;
                case "Mötley Crüe": Heartbeat(z); break;
                case "Twisted Sister": Mirror(z); break;
                case "Bon Jovi": Star(z); break;
                case "Europe": Countdown(z); break;
                case "Quiet Riot": Speakers(z); break;
                case "Slash": TopHat(z); break;
                case "Van Halen": StripedGuitar(z); break;
                case "Warrant": CherryPie(z); break;
                default: Bolt(z); break;
            }
            if (a.name == "Guns N' Roses") GunsMore(z);
            SongBits(z);
            var mark = new GameObject("Shot: the " + a.name + " front").transform; mark.SetParent(ctx.Root, false);   // the venue seen from across the Strip
            var cam = z.c + front * (z.W * .5f + 16f) + z.side * 3f; cam = ctx.OnGround(cam.x, cam.z); var look = z.c + Vector3.up * (z.H * .55f) - cam - Vector3.up * 1.6f;
            mark.SetPositionAndRotation(cam, Quaternion.LookRotation(look.normalized));
            area.glowRenderers = z.glows.ToArray();
            z.mk.Build("Venue", root, false);
            z.solid.BuildCollider("Walls", root);
            z.pictures.Build("Covers", root, false);
            noGrass.Add(new Vector3(z.c.x, z.W * .7f, z.c.z));
        }

        /// The club front: walls on three sides and a roof, the front open over a low stage, a marquee of bulbs and
        /// the name in neon above, an awning, the covers on a board by the door, the plaque on the pavement.
        void Venue(Spot z)
        {
            var up = Vector3.up; var rot = Quaternion.LookRotation(z.front); var W = z.W; var H = z.H; const float D = 11f, t = .4f;
            var st = StyleOf(z.a.name); var paint = WorldMaterials.Solid(st.wall);
            var b = z.c - z.front * 2f;
            void Wall(Vector3 p, Vector3 s) { z.mk.Box(paint, p, s, rot); z.solid.Box(paint, p, s, rot); }
            Wall(b + up * (H / 2f - .5f) - z.front * (D / 2f - t / 2f), new Vector3(W, H + 1f, t));
            foreach (var s in new[] { -1f, 1f }) Wall(b + up * (H / 2f - .5f) + z.side * s * (W / 2f - t / 2f), new Vector3(t, H + 1f, D));
            z.mk.Box(paint, b + up * H, new Vector3(W, .4f, D), rot);
            var lintel = 5.2f;
            Wall(b + up * (lintel + (H - lintel) / 2f) + z.front * (D / 2f - t / 2f), new Vector3(W, H - lintel, t));
            // the stage at the back, the floor
            z.mk.Box(black, b + up * .02f, new Vector3(W - .8f, .04f, D - .8f), rot);
            z.stage = b - z.front * 2.2f + up * .9f;
            z.mk.Box(black, z.stage - up * .45f, new Vector3(W - 2f, .9f, 4.6f), rot); z.solid.Box(black, z.stage - up * .45f, new Vector3(W - 2f, .9f, 4.6f), rot);
            foreach (var s in new[] { -1f, 1f }) { var amp = z.stage + z.side * s * (W / 2f - 2.2f) + up * .9f - z.front * 1.2f; z.mk.Box(black, amp, new Vector3(1.6f, 1.8f, .9f), rot); z.mk.Box(WorldMaterials.Solid(new Color(.25f, .22f, .2f)), amp + z.front * .46f, new Vector3(1.4f, 1.5f, .02f), rot); }
            z.mk.Prism(chrome, z.stage + z.front * 1.2f, up, .03f, 1.6f, 6); z.mk.Blob(chrome, z.stage + z.front * 1.2f + up * 1.7f, new Vector3(.1f, .14f, .1f), z.ctx.Rng, 0f);
            var back = Neon[Mathf.Abs(z.a.name.GetHashCode()) % Neon.Length];
            z.mk.Box(GlowM(back, 1.1f), b - z.front * (D / 2f - t - .03f) + up * 3.2f, new Vector3(W - 2f, 4.4f, .04f), rot);          // a glowing backdrop behind the stage
            for (var k = 0; k < 5; k++) z.mk.Box(GlowM(Color.Lerp(back, Color.white, .5f), 2f), b - z.front * (D / 2f - t - .09f) + up * (1.6f + k * .8f), new Vector3(W - 2.2f, .06f, .02f), rot);
            z.mk.Box(WorldMaterials.Solid(new Color(.55f, .5f, .55f), .8f), b + up * .06f + z.front * 2f, new Vector3(W - 1f, .02f, 5f), rot);   // a shiny dance floor
            ArtistBits.PointLight(z.root, z.stage + up * 3.5f + z.front * 2f, back, 16f, 3f, true);
            IndoorSpaces.Add(b + up * H / 2f, rot, new Vector3(W, H, D));
            // the marquee: a box over the opening, chasing bulbs round it, the name across it
            var mq = b + z.front * (D / 2f + .6f) + up * (lintel + .8f);
            z.mk.Box(WorldMaterials.Solid(new Color(.95f, .92f, .85f)), mq, new Vector3(W - 1.5f, 1.4f, 1.2f), rot);
            var bulbs = new List<Material>();
            for (var k = 0; k < 16; k++)
            {
                var m = new Material(GlowM(new Color(1f, .9f, .6f), 2.2f)) { name = "Marquee bulb" }; bulbs.Add(m);
                foreach (var y in new[] { -.75f, .75f }) z.mk.Blob(m, mq + z.front * .62f + up * y + z.side * ((k - 7.5f) * (W - 2f) / 16f), Vector3.one * .1f, z.ctx.Rng, 0f);
            }
            life.marquees.Add(bulbs.ToArray());
            // the name is already in neon on the roof (and on the zone curtain), so the marquee just bills the night
            ArtistBits.Label(z.labels, "LIVE TONIGHT  ★  SOLD OUT", mq + z.front * .63f, z.front, .36f, new Color(.12f, .04f, .1f), TextAnchor.MiddleCenter);
            // the awning in leopard print over the pavement
            var aw = b + z.front * (D / 2f + 2.6f) + up * 4.4f;
            z.mk.Box(AwningM(st), aw, new Vector3(W - 2f, .1f, 3.4f), rot * Quaternion.Euler(-14f, 0, 0));
            Facade(z, b, st, rot, W, H, D, lintel, aw);
            if (st.awning == "leopard") for (var k = 0; k < 18; k++) z.mk.Blob(black, aw + z.side * ((k * 1.618f) % (W - 3f) - (W - 3f) / 2f) + z.front * (((k * .73f) % 3f) - 1.5f) + up * (.06f + (((k * .73f) % 3f) - 1.5f) * .24f), new Vector3(.22f, .03f, .16f), z.ctx.Rng, .4f);
            // the name again in big neon on the roof edge, and a glow round the door
            var sign = b + up * (H + 1.1f) + z.front * (D / 2f - .3f);
            z.mk.Box(black, sign, new Vector3(W - 2f, 1.8f, .3f), rot);
            var nc = Neon[Mathf.Abs(z.a.name.GetHashCode()) % Neon.Length];
            z.mk.Box(GlowM(nc, 2.4f), sign + z.front * .16f + up * .78f, new Vector3(W - 2.4f, .1f, .03f), rot);
            z.mk.Box(GlowM(nc, 2.4f), sign + z.front * .16f - up * .78f, new Vector3(W - 2.4f, .1f, .03f), rot);
            ArtistBits.Label(z.labels, z.a.name, sign + z.front * .17f, z.front, .55f, nc, TextAnchor.MiddleCenter);
            halos.Add(sign + z.front * 1.2f, W * .35f, nc, .45f);
            z.roof = b + up * (H + .2f) + z.front * (D / 2f - 2f);
            // covers on a board by the door, plaque on the pavement
            var n = Mathf.Max(1, Mathf.Min(5, z.a.songList?.Length ?? 1)); const float size = .9f; var bw = n * size * 1.25f + .4f;
            var board = b + z.front * (D / 2f + .3f) + z.side * (W / 2f - bw / 2f - .3f) + up * 2.3f;
            z.mk.Box(black, board, new Vector3(bw, size + .6f, .1f), rot);
            ArtistBits.CoverWall(z.pictures, z.labels, z.ctx, z.a, board + z.front * .06f + up * .08f, z.front, n, size, n, true, Ink);
            ArtistBits.Plaque(z.mk, z.solid, z.labels, z.a, z.ctx.OnGround(z.c.x + z.front.x * 9f - z.side.x * (W / 2f - 1f), z.c.z + z.front.z * 9f - z.side.z * (W / 2f - 1f)), z.front, chrome, black, Ink, 1.5f);
            var door = ArtistBits.GlassBox(z.root, b + z.front * (D / 2f + .1f) + up * (lintel + .05f), new Vector3(W - 1f, .12f, .12f), Pink, rot);
            z.glows.Add(door);
            z.area.glowLight = ArtistBits.PointLight(z.root, b + z.front * (D / 2f + 3f) + up * 3.5f, nc, 16f, 1.8f);
        }

        // ---------------------------------------------------------------- the big signs
        void Rose(Spot z)
        {
            var up = Vector3.up; var ctx = z.ctx; var r = z.roof + up * 6f;
            var petal = GlowM(new Color(1f, .15f, .3f), 2.4f); var leafG = GlowM(new Color(.3f, 1f, .45f), 1.8f);
            z.mk.Box(leafG, z.roof + up * 3f, new Vector3(.3f, 6f, .3f), Quaternion.LookRotation(z.front));
            foreach (var s in new[] { -1f, 1f }) z.mk.Box(leafG, z.roof + up * 2.4f + z.side * s * .7f, new Vector3(1.4f, .2f, .5f), Quaternion.LookRotation(z.front) * Quaternion.Euler(0, 0, s * 30f));
            for (var ring = 0; ring < 3; ring++)
                for (var k = 0; k < 6; k++)
                {
                    var a = k * 60f + ring * 30f; var d = Quaternion.AngleAxis(a, z.front) * up;
                    z.mk.Blob(petal, r + d * (.6f + ring * .55f) + z.front * (ring * -.2f), new Vector3(.9f - ring * .1f, .7f, .4f), ctx.Rng, .1f);
                }
            halos.Add(r + z.front * 1.5f, 4.5f, new Color(1f, .2f, .35f), .6f);
            // a grand piano on the stage under a lone streetlamp
            var p = z.stage + up * .05f + z.side * 2f;
            z.mk.Blob(black, p + up * .9f, new Vector3(1.1f, .25f, 1.6f), ctx.Rng, 0f);
            foreach (var s in new[] { -1f, 1f }) z.mk.Prism(black, p + z.side * s * .7f + z.front * .6f, up, .07f, .8f, 6);
            z.mk.Prism(black, p - z.front * 1f, up, .07f, .8f, 6);
            z.mk.Box(black, p + up * 1.5f + z.side * .3f - z.front * .2f, new Vector3(1.6f, .05f, 1.8f), Quaternion.LookRotation(z.front) * Quaternion.Euler(-35f, 0, 0));
            var lamp = p - z.side * 2.4f; z.mk.Prism(chrome, lamp, up, .07f, 4f, 6); z.mk.Box(GlowM(new Color(1f, .85f, .6f), 2.6f), lamp + up * 4f + z.side * .4f, new Vector3(.5f, .15f, .3f), Quaternion.LookRotation(z.front));
            life.roseStage = z.stage + z.front * 3f;
        }

        void Wings(Spot z)
        {
            var up = Vector3.up; var c = z.roof + up * 4f; var m = GlowM(Gold, 2.4f);
            foreach (var s in new[] { -1f, 1f })
                for (var k = 0; k < 6; k++)
                {
                    var w = c + z.side * s * (1.4f + k * .9f) + up * (k * .35f - .4f);
                    z.mk.Box(m, w, new Vector3(.18f, 2.6f - k * .3f, .15f), Quaternion.LookRotation(z.front) * Quaternion.Euler(0, 0, s * (-25f - k * 6f)));
                }
            z.mk.Prism(GlowM(Pink, 2.4f), c, z.front, 1.1f, .2f, 18);
            halos.Add(c + z.front, 5f, Gold, .5f);
            // a glowing asteroid hanging overhead
            var rock = WorldMaterials.Solid(new Color(.35f, .3f, .3f));
            var ast = z.roof + up * 16f - z.front * 3f;
            z.mk.Blob(rock, ast, new Vector3(3.6f, 3f, 3.2f), z.ctx.Rng, .5f);
            for (var k = 0; k < 7; k++) z.mk.Blob(GlowM(new Color(1f, .45f, .15f), 2.4f), ast + Random3(z.ctx) * 3.1f, Vector3.one * .45f, z.ctx.Rng, .3f);
            halos.Add(ast, 6f, new Color(1f, .5f, .2f), .5f);
            life.asteroid = ast;
        }

        static Vector3 Random3(LandContext ctx) => new Vector3(ctx.Range(-1f, 1f), ctx.Range(-1f, 1f), ctx.Range(-1f, 1f)).normalized;

        void Heartbeat(Spot z)
        {
            var up = Vector3.up; var c = z.roof + up * 4.5f; var red = GlowM(new Color(1f, .15f, .25f), 2.4f); var rot = Quaternion.LookRotation(z.front);
            for (var k = 0; k < 24; k++)
            {
                var a = k / 24f * Mathf.PI * 2f;
                var x = 16f * Mathf.Pow(Mathf.Sin(a), 3f); var y = 13f * Mathf.Cos(a) - 5f * Mathf.Cos(2f * a) - 2f * Mathf.Cos(3f * a) - Mathf.Cos(4f * a);
                var a1 = (k + 1) / 24f * Mathf.PI * 2f;
                var x1 = 16f * Mathf.Pow(Mathf.Sin(a1), 3f); var y1 = 13f * Mathf.Cos(a1) - 5f * Mathf.Cos(2f * a1) - 2f * Mathf.Cos(3f * a1) - Mathf.Cos(4f * a1);
                var p0 = c + z.side * x * .18f + up * y * .18f; var p1 = c + z.side * x1 * .18f + up * y1 * .18f;
                z.mk.Box(red, (p0 + p1) / 2f, new Vector3(.2f, .2f, Vector3.Distance(p0, p1) + .05f), Quaternion.LookRotation(p1 - p0, z.front));
            }
            Vector3[] ecg = { new Vector3(-4.5f, 0, 0), new Vector3(-1.5f, 0, 0), new Vector3(-.8f, 1.6f, 0), new Vector3(0f, -1.6f, 0), new Vector3(.8f, 2.2f, 0), new Vector3(1.5f, 0, 0), new Vector3(4.5f, 0, 0) };
            var white = GlowM(new Color(.9f, 1f, .95f), 2.6f);
            for (var k = 0; k + 1 < ecg.Length; k++) { var p0 = c + z.front * .3f + z.side * ecg[k].x + up * ecg[k].y; var p1 = c + z.front * .3f + z.side * ecg[k + 1].x + up * ecg[k + 1].y; z.mk.Box(white, (p0 + p1) / 2f, new Vector3(.14f, .14f, Vector3.Distance(p0, p1) + .05f), Quaternion.LookRotation(p1 - p0, z.front)); }
            halos.Add(c + z.front, 4.5f, new Color(1f, .2f, .3f), .55f);
            // a chopper on a launch ramp out on the pavement
            var ramp = z.c + z.front * 5f + z.side * (z.W / 2f - 3f);
            var rr = Quaternion.LookRotation(z.side);
            z.mk.Box(chrome, ramp + up * .9f, new Vector3(2.4f, .2f, 5f), rr * Quaternion.Euler(-20f, 0, 0)); z.solid.Box(chrome, ramp + up * .8f, new Vector3(2.4f, 1.6f, 5f), rr);
            Bike(z.mk, z.solid, ramp + up * 1.8f + z.side * 1.2f, z.side, new Color(.7f, .05f, .1f));
        }

        void Mirror(Spot z)
        {
            var up = Vector3.up; var c = z.roof + up * 4.5f; var rot = Quaternion.LookRotation(z.front);
            z.mk.Box(WorldMaterials.Solid(new Color(1f, .45f, .75f), .7f), c, new Vector3(7f, 6f, .4f), rot);
            z.mk.Box(WorldMaterials.Solid(new Color(.75f, .82f, .9f), .98f), c + z.front * .22f, new Vector3(6f, 5f, .02f), rot);
            for (var k = 0; k < 18; k++)
            {
                var t = k / 18f; Vector3 p;
                if (t < .25f) p = new Vector3(Mathf.Lerp(-3.2f, 3.2f, t / .25f), 2.7f, 0); else if (t < .5f) p = new Vector3(3.2f, Mathf.Lerp(2.7f, -2.7f, (t - .25f) / .25f), 0);
                else if (t < .75f) p = new Vector3(Mathf.Lerp(3.2f, -3.2f, (t - .5f) / .25f), -2.7f, 0); else p = new Vector3(-3.2f, Mathf.Lerp(-2.7f, 2.7f, (t - .75f) / .25f), 0);
                z.mk.Blob(GlowM(new Color(1f, .95f, .8f), 2.6f), c + z.front * .25f + z.side * p.x + up * p.y, Vector3.one * .22f, z.ctx.Rng, 0f);
            }
            halos.Add(c + z.front * 1.5f, 5f, new Color(1f, .9f, .8f), .45f);
        }

        void Star(Spot z)
        {
            var up = Vector3.up; var c = z.roof + up * 4.5f;
            var t = new GameObject("Spinning star").transform; t.SetParent(z.root, false);
            var sk = new MeshKit(); var m = GlowM(Gold, 2.6f);
            for (var k = 0; k < 5; k++)
            {
                var a0 = k * 72f * Mathf.Deg2Rad; var a1 = (k * 72f + 36f) * Mathf.Deg2Rad; var a2 = (k * 72f - 36f) * Mathf.Deg2Rad;
                Vector3 P(float a, float r) => new Vector3(Mathf.Sin(a) * r, Mathf.Cos(a) * r, 0);
                sk.Tri(m, P(a0, 3.2f), P(a1, 1.3f), P(a2, 1.3f), Vector3.forward); sk.Tri(m, P(a0, 3.2f), P(a2, 1.3f), P(a1, 1.3f), Vector3.back);
                sk.Tri(m, Vector3.zero, P(a1, 1.3f), P(a2, 1.3f), Vector3.forward); sk.Tri(m, Vector3.zero, P(a2, 1.3f), P(a1, 1.3f), Vector3.back);
            }
            sk.BuildSingle("Star", t, false);
            t.position = c; t.rotation = Quaternion.LookRotation(z.front);
            life.spinners.Add(t);
            halos.Add(c + z.front, 4f, Gold, .5f);
        }

        void Countdown(Spot z)
        {
            var up = Vector3.up; var c = z.roof + up * 3.4f; var rot = Quaternion.LookRotation(z.front);
            z.mk.Box(black, c, new Vector3(9f, 4.4f, .6f), rot);
            // seven-segment digits: built as separate segments so HairLife can show the count
            var segs = new List<Material>();
            Vector3[] segPos = { new Vector3(0, 1.2f, 0), new Vector3(.62f, .6f, 0), new Vector3(.62f, -.6f, 0), new Vector3(0, -1.2f, 0), new Vector3(-.62f, -.6f, 0), new Vector3(-.62f, .6f, 0), new Vector3(0, 0, 0) };
            for (var dgt = 0; dgt < 2; dgt++)
                for (var s = 0; s < 7; s++)
                {
                    var m = new Material(GlowM(new Color(1f, .1f, .08f), .3f)) { name = "Countdown segment" }; m.SetColor("_BaseColor", new Color(.12f, .03f, .03f)); segs.Add(m);
                    var horiz = s == 0 || s == 3 || s == 6;
                    z.mk.Box(m, c + z.front * .31f + z.side * ((dgt - .5f) * 2.4f + segPos[s].x) + up * segPos[s].y, horiz ? new Vector3(1.05f, .2f, .02f) : new Vector3(.2f, 1.05f, .02f), rot);
                }
            life.countdown = segs.ToArray();
            halos.Add(c + z.front * 1.2f, 4f, new Color(1f, .15f, .1f), .4f);
            // a synth stack on the stage
            for (var k = 0; k < 3; k++) { var kb = z.stage + z.side * 2f + up * (.9f + k * .5f) + z.front * .3f; z.mk.Box(black, kb, new Vector3(1.8f, .12f, .6f), rot * Quaternion.Euler(-15f, 0, 0)); z.mk.Box(WorldMaterials.Solid(new Color(.95f, .95f, .95f)), kb + up * .07f + z.front * .1f, new Vector3(1.6f, .02f, .2f), rot * Quaternion.Euler(-15f, 0, 0)); }
        }

        void Speakers(Spot z)
        {
            var up = Vector3.up; var rot = Quaternion.LookRotation(z.front); var grey = WorldMaterials.Solid(new Color(.3f, .3f, .32f));
            for (var x = 0; x < 4; x++) for (var y = 0; y < 3; y++)
            {
                var p = z.roof + up * (1f + y * 1.9f) + z.side * ((x - 1.5f) * 2f);
                z.mk.Box(black, p, new Vector3(1.9f, 1.8f, 1.2f), rot);
                var cone = new Material(GlowM(Cyan, .4f)) { name = "Speaker cone" };
                z.mk.Prism(grey, p + z.front * .6f, z.front, .7f, .05f, 16); z.mk.Prism(cone, p + z.front * .64f, z.front, .25f, .03f, 12);
                life.speakerCones.Add(cone);
            }
        }

        void TopHat(Spot z)
        {
            var up = Vector3.up; var c = z.roof;
            z.mk.Prism(black, c, up, 4.5f, .4f, 24);
            z.mk.Prism(black, c + up * .4f, up, 3f, 5f, 20, 1.05f);
            z.mk.Prism(WorldMaterials.Solid(new Color(.5f, .45f, .3f)), c + up * 1f, up, 3.05f, .8f, 20);
            for (var k = 0; k < 6; k++) z.mk.Blob(chrome, c + up * 1.4f + Quaternion.Euler(0, k * 60f, 0) * Vector3.forward * 3.06f, Vector3.one * .18f, z.ctx.Rng, 0f);
            halos.Add(c + up * 3f + z.front * 4f, 3f, Gold, .4f);
            ArtistBits.PointLight(z.root, c + up * 3f + z.front * 5f, new Color(1f, .85f, .6f), 14f, 1.6f);
        }

        void StripedGuitar(Spot z)
        {
            var up = Vector3.up; var c = z.roof + up * 1f; var rot = Quaternion.LookRotation(z.front) * Quaternion.Euler(0, 0, -12f);
            var red = WorldMaterials.Solid(new Color(.85f, .1f, .1f), .7f); var wh = WorldMaterials.Solid(new Color(.95f, .95f, .95f)); var bk = black;
            z.mk.Blob(red, c + up * 2.2f, new Vector3(2.2f, 2.4f, .5f), z.ctx.Rng, 0f);
            z.mk.Blob(red, c + up * 4.2f + z.side * .3f, new Vector3(1.6f, 1.4f, .48f), z.ctx.Rng, 0f);
            for (var k = 0; k < 7; k++) z.mk.Box(k % 2 == 0 ? wh : bk, c + up * (1.2f + k * .55f) + z.front * .5f + z.side * Mathf.Sin(k) * .4f, new Vector3(3.6f - Mathf.Abs(k - 3) * .4f, .12f, .03f), Quaternion.LookRotation(z.front) * Quaternion.Euler(0, 0, (k % 3 - 1) * 35f));
            z.mk.Box(WorldMaterials.Solid(new Color(.7f, .55f, .3f)), c + up * 7.5f, new Vector3(.4f, 5f, .25f), rot);
            z.mk.Box(bk, c + up * 10.3f, new Vector3(.6f, 1.2f, .25f), rot);
            halos.Add(c + up * 3f + z.front * 2f, 3.5f, new Color(1f, .3f, .3f), .4f);
        }

        void CherryPie(Spot z)
        {
            var up = Vector3.up; var c = z.roof + up * .3f;
            var crust = WorldMaterials.Solid(new Color(.85f, .62f, .32f)); var cherry = WorldMaterials.Solid(new Color(.8f, .05f, .12f), .8f);
            z.mk.Prism(crust, c, up, 4.6f, 1.6f, 24, 1.1f);
            z.mk.Prism(cherry, c + up * 1.6f, up, 4.4f, .2f, 24);
            for (var k = 0; k < 8; k++) { var a = k * 45f * Mathf.Deg2Rad; z.mk.Box(crust, c + up * 1.85f, new Vector3(.4f, .2f, 9f), Quaternion.Euler(0, k * 22.5f, 0)); }
            for (var k = 0; k < 10; k++) z.mk.Blob(cherry, c + up * 2.3f + Quaternion.Euler(0, k * 36f, 0) * Vector3.forward * 2.6f, Vector3.one * .5f, z.ctx.Rng, 0f);
            ArtistBits.PointLight(z.root, c + up * 4f + z.front * 4f, new Color(1f, .6f, .6f), 12f, 1.4f);
        }

        void Bolt(Spot z)
        {
            var up = Vector3.up; var c = z.roof + up * 1f; var m = GlowM(Cyan, 2.6f);
            Vector3[] b = { new Vector3(.8f, 7f, 0), new Vector3(-1.2f, 3.6f, 0), new Vector3(.4f, 3.8f, 0), new Vector3(-1f, 0f, 0) };
            for (var k = 0; k < 3; k++) { var p0 = c + z.side * b[k].x + up * b[k].y; var p1 = c + z.side * b[k + 1].x + up * b[k + 1].y; z.mk.Box(m, (p0 + p1) / 2f, new Vector3(.7f, .3f, Vector3.Distance(p0, p1) + .3f), Quaternion.LookRotation(p1 - p0, z.front)); }
            halos.Add(c + up * 3.5f + z.front, 3.5f, Cyan, .5f);
        }
    }
}
