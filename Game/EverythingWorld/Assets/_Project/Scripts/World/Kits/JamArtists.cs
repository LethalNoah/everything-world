using System.Collections.Generic;
using UnityEngine;

namespace EverythingWorld.World
{
    /// The Commonwealth's artists.
    /// - Phish: the forest amphitheatre. A big curved shell over the stage, two trampolines on it, a keyboard rig,
    ///   a drum riser; overhead three trusses of moving lights that swing and change colour with the music
    ///   (JamLife); a sloping lawn in front for the crowd; a hot-air balloon tethered high above; beside it a
    ///   donut garden (giant glazed donuts with sprinkles), a giant hot dog, and a giant vacuum on a plinth next to
    ///   a tent printed with red donuts on blue.
    /// - Trey Anastasio: a giant hollow-body guitar and a brass section on stands. Widespread Panic: a great oak
    ///   hung with lights over a porch stage. Umphrey's McGee: a giant metronome. Billy Strings: a lantern-lit
    ///   bluegrass stage with a giant banjo. Goose: a giant goose. Blues Traveler: a giant harmonica. Oysterhead: a
    ///   giant oyster with a glowing pearl. The String Cheese Incident: giant string cheese. The Disco Biscuits: a
    ///   mirror-ball biscuit. Jerry Garcia Band: a guitar under a rose arch. Rusted Root: a tree of rusted roots.
    ///   Yonder Mountain String Band: a mountain cutout stage. Everyone else: a tapestry stage.
    public partial class JamKit
    {
        class Plot
        {
            public LandContext ctx; public Transform root, labels; public MeshKit mk, solid, pictures;
            public IslandWorldData.Artist a; public Vector3 c, front, side; public List<Renderer> glows; public ArtistArea area;
            public Vector3 At(float fw, float rt) { var p = c + front * fw + side * rt; return ctx.OnGround(p.x, p.z); }
        }

        public override void BuildArtist(LandContext ctx, IslandWorldData.Artist a)
        {
            if (dark == null) MakeMaterials();
            var root = new GameObject("Artist: " + a.name).transform; root.SetParent(ctx.Root, false);
            var front = facing.TryGetValue(a.name, out var f) ? f : new Vector3(center.x - a.x, 0, center.z - a.z).normalized;
            if (front.sqrMagnitude < .01f) front = Vector3.forward;
            var labels = new GameObject("Labels").transform; labels.SetParent(root, false);
            var area = root.gameObject.AddComponent<ArtistArea>();
            area.artistName = a.name; area.stationId = a.stationId; area.radius = a.r; area.glowColor = Warm;
            var z = new Plot { ctx = ctx, root = root, labels = labels, mk = new MeshKit(), solid = new MeshKit(), pictures = new MeshKit(), a = a, c = ctx.OnGround(a.x, a.z), front = front, side = Vector3.Cross(Vector3.up, front), glows = new List<Renderer>(), area = area };
            switch (a.name)
            {
                case "Phish": Amphitheatre(z); break;
                case "Trey Anastasio": Trey(z); break;
                case "Widespread Panic": Oak(z); break;
                case "Umphrey's McGee": Metronome(z); break;
                case "Billy Strings": Banjo(z); break;
                case "Goose": GooseStatue(z); break;
                case "Blues Traveler": Harmonica(z); break;
                case "Oysterhead": Oyster(z); break;
                case "The String Cheese Incident": StringCheese(z); break;
                case "The Disco Biscuits": Biscuit(z); break;
                case "Jerry Garcia Band": RoseArch(z); break;
                case "Rusted Root": Roots(z); break;
                case "Yonder Mountain String Band": Mountain(z); break;
                default: Tapestry(z); break;
            }
            var reach = Foot(a);
            Board(z, z.At(reach * .85f, -reach * .5f - 2.5f), front);
            ArtistBits.Plaque(z.mk, z.solid, labels, a, z.At(reach * .85f + 1.5f, reach * .5f + 2.5f), front, wood, dark, Ink, 1.6f);
            var lp = z.At(reach * .85f, reach * .5f + 5f); z.mk.Prism(wood, lp - Vector3.up * .3f, Vector3.up, .1f, 3.4f, 6);
            var go = new GameObject("Lantern"); go.transform.SetParent(root, false);
            var lk = new MeshKit(); lk.Box(GlowM(Warm, 2.6f), Vector3.zero, new Vector3(.35f, .45f, .35f), Quaternion.identity);
            z.glows.Add(lk.BuildSingle("Glass", go.transform, false).GetComponent<Renderer>()); go.transform.position = lp + Vector3.up * 3.3f;
            halos.Add(lp + Vector3.up * 3.3f, 1.4f, Warm, .8f); ArtistBits.PostCollider(root, lp, 3.4f, .12f);
            if (z.area.glowLight == null) z.area.glowLight = ArtistBits.PointLight(root, z.c + Vector3.up * 5f + front * 4f, Warm, HasStation(a) ? 18f : 12f, 1.4f);
            area.glowRenderers = z.glows.ToArray();
            z.mk.Build("Area", root, false);
            z.solid.BuildCollider("Walls", root);
            z.pictures.Build("Covers", root, false);
            noGrass.Add(new Vector3(z.c.x, reach * .9f, z.c.z));
        }

        void Board(Plot z, Vector3 ground, Vector3 dir)
        {
            dir = Vector3.ProjectOnPlane(dir, Vector3.up).normalized; var rot = Quaternion.LookRotation(dir); var side = Vector3.Cross(Vector3.up, dir);
            var n = Mathf.Max(1, Mathf.Min(6, z.a.songList?.Length ?? 1)); const float size = .9f; var w = n * size * 1.25f + .4f;
            foreach (var s in new[] { -1f, 1f }) z.mk.Prism(wood, ground + side * s * (w / 2f - .1f) - dir * .05f, Vector3.up, .08f, 2.4f, 6);
            var c = ground + Vector3.up * 1.8f;
            z.mk.Box(wood, c - Vector3.up * .1f, new Vector3(w, size + .6f, .1f), rot); z.solid.Box(wood, c - Vector3.up * .1f, new Vector3(w, size + .6f, .2f), rot);
            ArtistBits.CoverWall(z.pictures, z.labels, z.ctx, z.a, c + dir * .055f + Vector3.up * .08f, dir, n, size, n, true, Ink);
        }

        // ---------------------------------------------------------------- Phish
        void Amphitheatre(Plot z)
        {
            var up = Vector3.up; var ctx = z.ctx; var rot = Quaternion.LookRotation(z.front);
            var st = z.At(-16f, 0); var shellM = WorldMaterials.Solid(new Color(.85f, .82f, .76f)); var deck = WorldMaterials.Solid(new Color(.18f, .16f, .15f));
            z.mk.Box(deck, st + up * .9f, new Vector3(26f, 1.8f, 12f), rot); z.solid.Box(deck, st + up * .9f, new Vector3(26f, 1.8f, 12f), rot);
            // the curved shell: ribs arching over the back of the stage
            for (var k = 0; k <= 12; k++)
            {
                var a = Mathf.Lerp(-80f, 80f, k / 12f) * Mathf.Deg2Rad; var a1 = Mathf.Lerp(-80f, 80f, (k + 1) / 12f) * Mathf.Deg2Rad;
                for (var r = 0; r < 4; r++)
                {
                    var rr = 14f - r * .2f; float e0 = r * 22f * Mathf.Deg2Rad, e1 = (r + 1) * 22f * Mathf.Deg2Rad;
                    Vector3 P(float az, float el) => st - z.front * 2f + up * 1.8f + (z.side * Mathf.Sin(az) - z.front * Mathf.Cos(az) * .55f) * rr * Mathf.Cos(el) + up * rr * Mathf.Sin(el) * .9f;
                    if (k < 12) { z.mk.Quad(shellM, P(a, e0), P(a1, e0), P(a1, e1), P(a, e1), st - z.front * 30f + up * 4f); z.mk.Quad(shellM, P(a, e0), P(a1, e0), P(a1, e1), P(a, e1), st + z.front * 30f + up * 4f); }
                }
            }
            z.solid.Box(shellM, st - z.front * 8f + up * 6f, new Vector3(26f, 12f, 1f), rot);
            // trampolines, keys, drums
            foreach (var s in new[] { -1f, 1f }) { var tp = st + up * 1.8f + z.side * s * 4f + z.front * 2f; z.mk.Prism(WorldMaterials.Solid(new Color(.1f, .1f, .12f)), tp + up * .5f, up, 1.2f, .06f, 16); z.mk.Prism(metal, tp, up, 1.3f, .55f, 16, 1f, 0f, false); }
            for (var k = 0; k < 3; k++) { var kb = st + up * (2.6f + k * .4f) - z.side * 8f + z.front * 1f; z.mk.Box(dark, kb, new Vector3(2f, .15f, .7f), rot * Quaternion.Euler(-12f, 0, 0)); z.mk.Box(white, kb + up * .09f + z.front * .15f, new Vector3(1.8f, .02f, .25f), rot * Quaternion.Euler(-12f, 0, 0)); }
            z.mk.Box(deck, st + up * 2.4f - z.front * 3f + z.side * 7f, new Vector3(5f, 1.2f, 4f), rot);
            z.mk.Prism(WorldMaterials.Solid(new Color(.75f, .15f, .2f)), st + up * 3f - z.front * 3f + z.side * 7f, z.front, .7f, .6f, 14);
            // the light rig: three trusses of moving lights
            var pods = new List<Transform>(); var lens = new List<Material>();
            for (var row = 0; row < 3; row++)
            {
                var ty = 11f + row * .5f; var tz = (row - 1) * 3.5f;
                z.mk.Box(metal, st + up * ty + z.front * tz, new Vector3(24f, .4f, .4f), rot);
                for (var k = 0; k < 9; k++)
                {
                    var pk = new MeshKit(); pk.Box(dark, Vector3.zero, new Vector3(.5f, .6f, .5f), Quaternion.identity);
                    var m = new Material(GlowM(Dye[(k + row) % Dye.Length], 2.2f)) { name = "Rig lens" }; lens.Add(m);
                    pk.Prism(m, Vector3.down * .3f, Vector3.down, .2f, .05f, 10);
                    var t = new GameObject("Moving light").transform; t.SetParent(z.root, false); pk.BuildSingle("Head", t, false);
                    t.position = st + up * (ty - .5f) + z.front * tz + z.side * ((k - 4f) * 2.7f); pods.Add(t);
                }
            }
            foreach (var s in new[] { -1f, 1f }) foreach (var b in new[] { -1f, 1f }) { var p = st + z.side * s * 12.5f + z.front * b * 4.5f; z.mk.Box(metal, p + up * 6f, new Vector3(.4f, 12f, .4f), rot); ArtistBits.PostCollider(z.root, p, 12f, .3f); }
            life.pods = pods.ToArray(); life.podLens = lens.ToArray(); life.stage = st + up * 2f;
            // beams from the rig, drawn by JamLife; a wash light over the stage
            var wash = ArtistBits.PointLight(z.root, st + up * 8f + z.front * 6f, new Color(.8f, .6f, 1f), 30f, 2.4f); life.wash = wash;
            // the lawn sloping down to the stage, a mixing tent in the middle of it
            var mix = z.At(10f, 9f);
            foreach (var a in new[] { -1f, 1f }) foreach (var b in new[] { -1f, 1f }) { z.mk.Prism(metal, mix + z.side * a * 2.2f + z.front * b * 1.7f, up, .06f, 2.9f, 6); ArtistBits.PostCollider(z.root, mix + z.side * a * 2.2f + z.front * b * 1.7f, 2.9f, .1f); }
            z.mk.Box(canvas, mix + up * 2.95f, new Vector3(4.8f, .12f, 3.8f), rot); z.mk.Cone(canvas, mix + up * 3f, 3.1f, .7f, 4, 45f);
            z.mk.Box(dark, mix + up * .5f, new Vector3(3.6f, 1f, 1f), rot); z.solid.Box(dark, mix + up * .5f, new Vector3(3.6f, 1f, 1f), rot);
            z.mk.Box(dark, mix + up * 1.05f - z.front * .05f, new Vector3(3.4f, .12f, .9f), rot * Quaternion.Euler(-12f, 0, 0));
            for (var k = 0; k < 24; k++) z.mk.Box(GlowM(Dye[k % Dye.Length], 2.2f), mix + up * 1.13f + z.side * ((k % 12 - 5.5f) * .26f) + z.front * ((k / 12) * .3f - .15f), new Vector3(.08f, .03f, .08f), rot);
            foreach (var s2 in new[] { -1f, 1f }) { z.mk.Box(dark, mix + up * 1.6f + z.side * s2 * 1f - z.front * .3f, new Vector3(.9f, .6f, .1f), rot); z.mk.Box(GlowM(new Color(.4f, .7f, 1f), 1.6f), mix + up * 1.6f + z.side * s2 * 1f - z.front * .24f, new Vector3(.8f, .5f, .02f), rot); }
            ArtistBits.PointLight(z.root, mix + up * 2.4f, new Color(.6f, .7f, 1f), 7f, .8f);
            // the crowd on the lawn, swaying (JamLife)
            var shirts = new[] { new Color(1f, .3f, .35f), new Color(1f, .62f, .2f), new Color(1f, .9f, .25f), new Color(.3f, .85f, .45f), new Color(.25f, .6f, 1f), new Color(.7f, .4f, 1f), new Color(.9f, .9f, .85f), new Color(.2f, .2f, .25f) };
            var skin = new[] { new Color(.95f, .78f, .62f), new Color(.75f, .55f, .4f), new Color(.5f, .35f, .25f) }; var jeans = WorldMaterials.Solid(new Color(.2f, .25f, .4f));
            for (var k = 0; k < 70; k++)
            {
                var fw = z.ctx.Range(-6f, 15f); var rt = z.ctx.Range(-11f, 11f);
                if (Vector2.Distance(new Vector2(fw, rt), new Vector2(10f, 9f)) < 4f || Mathf.Abs(rt) < 1.6f) continue;
                var feet = z.At(fw, rt); var pk = new MeshKit(); var shirt = WorldMaterials.Solid(shirts[z.ctx.Rng.Next(shirts.Length)], .3f); var sk = WorldMaterials.Solid(skin[z.ctx.Rng.Next(skin.Length)]); var h = z.ctx.Range(.9f, 1.08f);
                foreach (var s2 in new[] { -.11f, .11f }) pk.Box(jeans, new Vector3(s2, .42f * h, 0), new Vector3(.14f, .84f * h, .16f), Quaternion.identity);
                pk.Box(shirt, new Vector3(0, 1.15f * h, 0), new Vector3(.44f, .62f * h, .24f), Quaternion.identity); pk.Blob(sk, new Vector3(0, 1.62f * h, 0), new Vector3(.13f, .15f, .14f), z.ctx.Rng, 0f);
                var raised = z.ctx.Chance(.35f);
                foreach (var s2 in new[] { -1f, 1f }) pk.Box(shirt, new Vector3(s2 * .28f, (raised ? 1.65f : 1.05f) * h, 0), new Vector3(.1f, .6f * h, .1f), Quaternion.Euler(0, 0, raised ? s2 * -15f : s2 * 8f));
                var pt = pk.BuildSingle("Soft crowd", life.transform, false).transform; pt.SetPositionAndRotation(feet, Quaternion.LookRotation(-z.front) * Quaternion.Euler(0, z.ctx.Range(-25f, 25f), 0)); life.crowd.Add(pt);
            }
            var lm = new GameObject("Shot: the lawn").transform; lm.SetParent(z.ctx.Root, false); var lf = z.At(17f, -6f); var ll = st + up * 4f - lf; ll.y = 0; lm.SetPositionAndRotation(lf, Quaternion.LookRotation(ll.normalized));
            life.lawn = z.At(5f, 0); life.stageFront = st + up * 1.9f + z.front * 5.6f; life.stageSide = z.side;
            // a grand piano under the stage that rises into one white spot for The Squirming Coil
            var pkp = new MeshKit(); var lacquer = WorldMaterials.Solid(new Color(.04f, .04f, .05f), .95f);
            pkp.Box(lacquer, new Vector3(0, 1f, 0), new Vector3(1.5f, .35f, 2.2f), Quaternion.identity); pkp.Blob(lacquer, new Vector3(.2f, 1f, -1f), new Vector3(.6f, .17f, .5f), z.ctx.Rng, 0f);
            pkp.Box(lacquer, new Vector3(-.2f, 1.6f, -.1f), new Vector3(1.4f, .04f, 2f), Quaternion.Euler(0, 0, 25f)); pkp.Box(white, new Vector3(0, 1.12f, 1.12f), new Vector3(1.4f, .05f, .25f), Quaternion.identity);
            foreach (var p in new[] { new Vector3(-.6f, .4f, .9f), new Vector3(.6f, .4f, .9f), new Vector3(0, .4f, -.9f) }) pkp.Box(lacquer, p, new Vector3(.12f, .8f, .12f), Quaternion.identity);
            var piano = pkp.BuildSingle("Grand piano", life.transform, false).transform; life.pianoHome = st + up * 1.8f + z.front * 1f; piano.SetPositionAndRotation(life.pianoHome - up * 2.4f, Quaternion.LookRotation(z.side)); piano.gameObject.SetActive(false); life.piano = piano;
            var sp = new GameObject("Piano spot"); sp.transform.SetParent(z.root, false); sp.transform.SetPositionAndRotation(life.pianoHome + up * 10f + z.front * 3f, Quaternion.LookRotation((life.pianoHome - (life.pianoHome + up * 10f + z.front * 3f)).normalized));
            var spl = sp.AddComponent<Light>(); spl.type = LightType.Spot; spl.spotAngle = 22f; spl.range = 16f; spl.intensity = 0f; spl.color = Color.white; spl.shadows = LightShadows.None; life.pianoSpot = spl;
            // the balloon tethered high above
            var bk = new MeshKit();
            for (var r = 0; r < 8; r++)
            {
                float a0 = r / 8f * Mathf.PI, a1 = (r + 1) / 8f * Mathf.PI;
                for (var s = 0; s < 12; s++)
                {
                    float b0 = s * Mathf.PI / 6f, b1 = (s + 1) * Mathf.PI / 6f;
                    Vector3 P(float a, float b) { var rad = Mathf.Sin(a) * 8f * (a > Mathf.PI * .6f ? Mathf.Lerp(1f, .35f, (a - Mathf.PI * .6f) / (Mathf.PI * .4f)) : 1f); return new Vector3(Mathf.Cos(b) * rad, Mathf.Cos(a) * 10f, Mathf.Sin(b) * rad); }
                    bk.Quad(WorldMaterials.Solid(Dye[s % Dye.Length], .4f), P(a0, b0), P(a0, b1), P(a1, b1), P(a1, b0), Vector3.zero);
                }
            }
            bk.Box(wood, Vector3.down * 13.5f, new Vector3(2f, 1.4f, 2f), Quaternion.identity);
            bk.Box(GlowM(new Color(1f, .6f, .2f), 2.4f), Vector3.down * 12f, new Vector3(.5f, .5f, .5f), Quaternion.identity);
            var balloon = new GameObject("Tethered balloon").transform; balloon.SetParent(z.root, false); bk.BuildSingle("Balloon", balloon, false);
            balloon.position = z.c + up * 48f; life.balloon = balloon;
            for (var k = 0; k < 20; k++) { var p = Vector3.Lerp(z.At(4f, 14f) + up * .5f, z.c + up * 34f, k / 20f); z.mk.Box(dark, p, new Vector3(.05f, .05f, 1.6f), Quaternion.LookRotation(z.c + up * 34f - (z.At(4f, 14f) + up * .5f))); }
            // the donut garden: giant glazed donuts with sprinkles on the lawn's edge
            for (var k = 0; k < 4; k++)
            {
                var dp = z.At(2f + k * 4f, -24f + (k % 2) * 3f) + up * 1.6f; var dr = Quaternion.LookRotation(z.side) * Quaternion.Euler(0, k * 20f, 0);
                for (var s = 0; s < 14; s++)
                {
                    var a = s * Mathf.PI * 2f / 14f; var p = dp + dr * (new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0) * 1.1f);
                    z.mk.Blob(WorldMaterials.Solid(new Color(.85f, .6f, .35f)), p, Vector3.one * .6f, ctx.Rng, 0f);
                    z.mk.Blob(WorldMaterials.Solid(new[] { new Color(1f, .5f, .7f), new Color(.45f, .25f, .15f), new Color(.95f, .95f, .9f), new Color(.5f, .7f, 1f) }[k], .6f), p + dr * Vector3.forward * .2f, new Vector3(.55f, .55f, .45f), ctx.Rng, 0f);
                    if (s % 2 == 0) z.mk.Box(WorldMaterials.Solid(Dye[(s + k) % Dye.Length]), p + dr * Vector3.forward * .55f, new Vector3(.25f, .06f, .06f), dr * Quaternion.Euler(0, 0, s * 40f));
                }
                z.solid.Box(dark, dp, new Vector3(1f, 3.4f, 3.4f), dr);
            }
            // a giant hot dog on a cart
            var hd = z.At(14f, 22f); var hr = Quaternion.LookRotation(z.side);
            z.mk.Prism(WorldMaterials.Solid(new Color(.9f, .7f, .4f)), hd + up * 1.6f - z.side * 3.2f, z.side, 1.2f, 6.4f, 12);
            z.mk.Prism(WorldMaterials.Solid(new Color(.75f, .25f, .18f)), hd + up * 2.4f - z.side * 3.9f, z.side, .8f, 7.8f, 12);
            for (var k = 0; k < 8; k++) z.mk.Box(WorldMaterials.Solid(new Color(1f, .85f, .1f)), hd + up * 3.25f + z.side * ((k - 3.5f) * .9f) + z.front * Mathf.Sin(k) * .2f, new Vector3(.12f, .08f, .6f), hr * Quaternion.Euler(0, 30f * (k % 2 == 0 ? 1 : -1), 0));
            foreach (var s in new[] { -2.5f, 2.5f }) z.mk.Prism(dark, hd + z.side * s + up * .5f - z.front * 1.2f, z.front, .5f, 2.4f, 12);
            z.solid.Box(dark, hd + up * 1.8f, new Vector3(2.8f, 3.4f, 8f), hr);
            // a giant vacuum on a plinth by a tent printed with red donuts on blue
            var vc = z.At(16f, -10f);
            z.mk.Box(WorldMaterials.Solid(new Color(.6f, .6f, .62f)), vc + up * .4f, new Vector3(2.4f, .8f, 2.4f), rot); z.solid.Box(dark, vc + up * 2f, new Vector3(2.4f, 4f, 2.4f), rot);
            z.mk.Blob(WorldMaterials.Solid(new Color(.85f, .2f, .25f), .7f), vc + up * 1.6f, new Vector3(.8f, .8f, 1.2f), ctx.Rng, 0f);
            z.mk.Box(metal, vc + up * 3f + z.side * .4f, new Vector3(.12f, 2.6f, .12f), rot * Quaternion.Euler(0, 0, -20f));
            z.mk.Box(dark, vc + up * 4.3f + z.side * .9f, new Vector3(.8f, .15f, .3f), rot);
            var tent = z.At(18f, -16f); var blue = WorldMaterials.Solid(new Color(.2f, .35f, .8f), .3f); var dot = WorldMaterials.Solid(new Color(.85f, .15f, .15f));
            z.mk.Cone(blue, tent, 3f, 4f, 8);
            for (var k = 0; k < 14; k++) { var a = k * 50f * Mathf.Deg2Rad; var h = .6f + (k % 4) * .7f; var r = 3f * (1f - h / 4f) + .05f; z.mk.Blob(dot, tent + up * h + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * r, new Vector3(.3f, .3f, .3f), ctx.Rng, 0f); }
            z.solid.Prism(blue, tent, up, 2.6f, 3f, 8);
            life.phish = z.c;
        }

        // ---------------------------------------------------------------- the others
        void Trey(Plot z)
        {
            var up = Vector3.up; var c = z.At(-3f, 0); var rot = Quaternion.LookRotation(z.front); var body = WorldMaterials.Solid(new Color(.6f, .35f, .18f), .8f);
            z.mk.Blob(body, c + up * 3f, new Vector3(2.4f, 2.6f, .6f), z.ctx.Rng, 0f); z.mk.Blob(body, c + up * 5.6f, new Vector3(1.8f, 1.6f, .55f), z.ctx.Rng, 0f);
            foreach (var s in new[] { -1f, 1f }) z.mk.Box(dark, c + up * 4f + z.side * s * 1f + z.front * .58f, new Vector3(.25f, 1.2f, .05f), rot * Quaternion.Euler(0, 0, s * 12f));
            z.mk.Box(WorldMaterials.Solid(new Color(.3f, .2f, .12f)), c + up * 9.5f, new Vector3(.5f, 5f, .3f), rot); z.mk.Box(dark, c + up * 12.5f, new Vector3(.8f, 1.4f, .3f), rot);
            z.solid.Box(body, c + up * 4f, new Vector3(4.4f, 8f, 1.2f), rot);
            var brass = WorldMaterials.Solid(new Color(.9f, .75f, .35f), .9f);
            for (var k = 0; k < 4; k++) { var p = z.At(3f, -5f + k * 3.3f); z.mk.Prism(dark, p, up, .04f, 1.3f, 6); z.mk.Prism(brass, p + up * 1.4f, (z.front + up * .3f).normalized, .08f, 1.2f, 8, 3f); }
        }

        void Oak(Plot z)
        {
            var up = Vector3.up; var c = z.At(-4f, 0);
            z.mk.Prism(bark, c - up * .3f, up, 1.2f, 5f, 9, .7f);
            for (var k = 0; k < 7; k++) { var d = Quaternion.Euler(0, k * 51f, 0) * Vector3.forward; z.mk.Prism(bark, c + up * 4.5f, (d + up * .3f).normalized, .4f, 7f, 6, .4f); soft.Blob(leaf, c + up * 7f + d * 6.5f, new Vector3(3.6f, 2.2f, 3.6f), z.ctx.Rng, .3f); StringLights(z.mk, c + up * 6f + d * 2f, c + up * 5f + d * 7f, Warm, 6); }
            soft.Blob(leaf, c + up * 10f, new Vector3(6f, 3f, 6f), z.ctx.Rng, .3f);
            ArtistBits.PostCollider(z.root, c, 5f, 1.2f);
            var porch = z.At(3f, 0); var rot = Quaternion.LookRotation(z.front);
            z.mk.Box(wood, porch + up * .4f, new Vector3(8f, .8f, 4f), rot); z.solid.Box(wood, porch + up * .4f, new Vector3(8f, .8f, 4f), rot);
            for (var k = 0; k < 2; k++) z.mk.Box(dark, porch + up * 1.4f + z.side * (k == 0 ? -2.5f : 2.5f) - z.front * 1f, new Vector3(1.3f, 1.3f, .7f), rot);
            var fl = ArtistBits.PointLight(z.root, c + up * 6f, Warm, 16f, 1.8f); z.area.glowLight = fl;
        }

        void Metronome(Plot z)
        {
            var up = Vector3.up; var c = z.At(-3f, 0); var rot = Quaternion.LookRotation(z.front); var wd = WorldMaterials.Solid(new Color(.35f, .2f, .12f), .6f);
            z.mk.Prism(wd, c, up, 3f, 8f, 4, .25f, 45f); z.solid.Prism(wd, c, up, 2.6f, 8f, 4, .3f, 45f);
            var arm = new MeshKit(); arm.Box(metal, Vector3.up * 3.5f, new Vector3(.2f, 7f, .1f), Quaternion.identity); arm.Box(WorldMaterials.Solid(new Color(.9f, .75f, .35f), .9f), Vector3.up * 5f, new Vector3(.8f, .6f, .3f), Quaternion.identity);
            var t = new GameObject("Metronome arm").transform; t.SetParent(z.root, false); arm.BuildSingle("Arm", t, false); t.SetPositionAndRotation(c + up * 1.5f + z.front * 2.3f, rot); life.metronome = t;
        }

        void Banjo(Plot z)
        {
            var up = Vector3.up; var c = z.At(-3f, 0); var rot = Quaternion.LookRotation(z.front);
            z.mk.Box(wood, c + up * .5f, new Vector3(8f, 1f, 5f), rot); z.solid.Box(wood, c + up * .5f, new Vector3(8f, 1f, 5f), rot);
            var b = c + up * 5f - z.front * 2f;
            z.mk.Prism(white, b, z.front, 2.2f, .4f, 20); z.mk.Prism(metal, b - z.front * .05f, z.front, 2.4f, .3f, 20);
            z.mk.Box(wood, b + up * 4f + z.front * .2f, new Vector3(.4f, 6f, .2f), rot); ArtistBits.PostCollider(z.root, b - up * 5f, 5f, .4f);
            for (var k = 0; k < 5; k++) z.mk.Box(GlowM(new Color(.7f, .9f, 1f), 1.6f), b + up * 3.5f + z.front * .35f + z.side * ((k - 2f) * .08f), new Vector3(.02f, 7f, .02f), rot);
            foreach (var s in new[] { -1f, 1f }) { var lp = c + z.side * s * 4.5f + up * 1f; z.mk.Prism(wood, lp, up, .08f, 2.5f, 6); z.mk.Box(GlowM(Warm, 2.4f), lp + up * 2.6f, new Vector3(.3f, .4f, .3f), Quaternion.identity); halos.Add(lp + up * 2.6f, 1.2f, Warm, .8f); }
        }

        void GooseStatue(Plot z)
        {
            var up = Vector3.up; var c = z.At(-3f, 0); var w = WorldMaterials.Solid(new Color(.95f, .95f, .92f)); var orange = WorldMaterials.Solid(new Color(1f, .55f, .15f));
            z.mk.Blob(w, c + up * 2.4f, new Vector3(2f, 1.6f, 3f), z.ctx.Rng, 0f);
            z.mk.Prism(w, c + up * 3f + z.front * 1.8f, (z.front * .3f + up).normalized, .5f, 3f, 10, .7f);
            z.mk.Blob(w, c + up * 6f + z.front * 2.6f, new Vector3(.7f, .6f, .9f), z.ctx.Rng, 0f);
            z.mk.Prism(orange, c + up * 5.9f + z.front * 3.3f, z.front, .25f, .8f, 8, .3f);
            foreach (var s in new[] { -.2f, .2f }) z.mk.Blob(dark, c + up * 6.2f + z.front * 3f + z.side * s * 2f, Vector3.one * .09f, z.ctx.Rng, 0f);
            foreach (var s in new[] { -.6f, .6f }) z.mk.Box(orange, c + up * .5f + z.side * s, new Vector3(.2f, 1.2f, .2f), Quaternion.identity);
            z.solid.Box(w, c + up * 2.4f, new Vector3(3f, 4.8f, 4.5f), Quaternion.LookRotation(z.front));
        }

        void Harmonica(Plot z)
        {
            var up = Vector3.up; var c = z.At(-3f, 0); var rot = Quaternion.LookRotation(z.front) * Quaternion.Euler(-18f, 0, 0); var hc = c + up * 2.4f;
            var chrome = WorldMaterials.Solid(new Color(.85f, .87f, .9f), .95f); var comb = WorldMaterials.Solid(new Color(.15f, .3f, .7f), .5f);
            z.mk.Box(chrome, hc + rot * Vector3.up * .55f, new Vector3(7f, .35f, 1.6f), rot); z.mk.Box(chrome, hc - rot * Vector3.up * .55f, new Vector3(7f, .35f, 1.6f), rot); z.mk.Box(comb, hc, new Vector3(6.8f, .8f, 1.5f), rot);
            for (var k = 0; k < 10; k++) { var hp = hc + rot * new Vector3((k - 4.5f) * .62f, 0, .76f); z.mk.Box(dark, hp, new Vector3(.36f, .42f, .04f), rot); ArtistBits.Label(z.labels, (k + 1).ToString(), hc + rot * new Vector3((k - 4.5f) * .62f, .55f, .82f), z.front, .12f, new Color(.1f, .1f, .1f), TextAnchor.MiddleCenter); }
            foreach (var s in new[] { -2.6f, 2.6f }) { z.mk.Prism(metal, c + z.side * s, up, .1f, 2.1f, 8); ArtistBits.PostCollider(z.root, c + z.side * s, 2.1f, .15f); }
            halos.Add(hc + z.front * 1.2f, 3f, new Color(.6f, .75f, 1f), .3f); ArtistBits.PointLight(z.root, hc + z.front * 3f + up * 2f, new Color(.8f, .85f, 1f), 9f, 1.2f);
            z.solid.Box(chrome, hc, new Vector3(7f, 1.6f, 1.8f), rot);
        }

        void Oyster(Plot z)
        {
            var up = Vector3.up; var c = z.At(-3f, 0); var shell = WorldMaterials.Solid(new Color(.65f, .62f, .58f), .7f);
            z.mk.Blob(shell, c + up * .8f, new Vector3(3f, .8f, 2.6f), z.ctx.Rng, .2f);
            z.mk.Blob(shell, c + up * 2.6f - z.front * 1.2f, new Vector3(3f, .6f, 2.4f), z.ctx.Rng, .2f);
            z.mk.Blob(GlowM(new Color(1f, .97f, .92f), 1.8f), c + up * 1.6f + z.front * .3f, Vector3.one * .7f, z.ctx.Rng, 0f);
            halos.Add(c + up * 1.6f + z.front * .8f, 2f, Ink, .6f); z.solid.Box(shell, c + up * 1.2f, new Vector3(5f, 2.4f, 4f), Quaternion.LookRotation(z.front));
        }

        void StringCheese(Plot z)
        {
            var up = Vector3.up; var c = z.At(-3f, 0); var ch = WorldMaterials.Solid(new Color(1f, .97f, .86f)); var strand = GlowM(new Color(1f, .95f, .75f), 1.2f);
            z.mk.Prism(WorldMaterials.Solid(new Color(.2f, .45f, .85f), .6f), c, up, 1f, 2.2f, 16); z.mk.Prism(ch, c + up * 2.2f, up, .9f, 3.2f, 16);
            for (var k = 0; k < 10; k++)
            {
                var d = Quaternion.Euler(0, k * 36f, 0) * Vector3.forward; var p = c + up * 5.4f + d * .5f;
                for (var s = 0; s < 6; s++) { var q = p + d * .5f + up * (.35f - s * .12f); z.mk.Box(strand, (p + q) / 2f, new Vector3(.12f, .12f, Vector3.Distance(p, q) + .05f), Quaternion.LookRotation(q - p)); p = q; d = (d + Vector3.down * .25f).normalized; }
            }
            halos.Add(c + up * 5.6f, 3f, new Color(1f, .95f, .75f), .45f); ArtistBits.PostCollider(z.root, c, 5.4f, 1.05f);
        }

        void Biscuit(Plot z)
        {
            var up = Vector3.up; var c = z.At(-3f, 0);
            z.mk.Prism(metal, c, up, .12f, 4.5f, 8); ArtistBits.PostCollider(z.root, c, 4.5f, .2f);
            z.mk.Prism(WorldMaterials.Solid(new Color(.88f, .88f, .92f), .98f), c + up * 5f - z.front * .4f, z.front, 1.6f, .8f, 16);
            for (var k = 0; k < 9; k++) z.mk.Box(WorldMaterials.Solid(new Color(.95f, .95f, 1f), .99f), c + up * 5f + z.front * .41f + Quaternion.AngleAxis(k * 40f, z.front) * Vector3.up * (k % 2 == 0 ? .6f : 1.1f), new Vector3(.25f, .25f, .02f), Quaternion.LookRotation(z.front));
            halos.Add(c + up * 5f, 2.4f, new Color(.8f, .85f, 1f), .5f);
        }

        void RoseArch(Plot z)
        {
            var up = Vector3.up; var c = z.At(-3f, 0); var rose = WorldMaterials.Solid(new Color(.8f, .1f, .2f), .6f);
            for (var k = 0; k <= 10; k++) { var a = k / 10f * Mathf.PI; var p = c + z.side * Mathf.Cos(a) * 2.6f + up * Mathf.Sin(a) * 3.6f; soft.Blob(leaf, p, Vector3.one * .5f, z.ctx.Rng, .3f); soft.Blob(rose, p + z.front * .3f, Vector3.one * .25f, z.ctx.Rng, .2f); }
            foreach (var s in new[] { -1f, 1f }) ArtistBits.PostCollider(z.root, c + z.side * s * 2.6f, 2f, .3f);
            var g = c + up * .1f; z.mk.Blob(WorldMaterials.Solid(new Color(.75f, .55f, .3f), .7f), g + up * .6f, new Vector3(.45f, .55f, .15f), z.ctx.Rng, 0f); z.mk.Box(dark, g + up * 1.6f, new Vector3(.07f, 1.2f, .05f), Quaternion.LookRotation(z.front));
        }

        void Roots(Plot z)
        {
            var up = Vector3.up; var c = z.At(-3f, 0); var rust = WorldMaterials.Solid(new Color(.6f, .3f, .15f), .5f);
            z.mk.Prism(rust, c, up, .8f, 6f, 8, .6f);
            for (var k = 0; k < 8; k++) { var d = Quaternion.Euler(0, k * 45f, 0) * Vector3.forward; z.mk.Prism(rust, c + up * .8f, (d - up * .4f).normalized, .3f, 4f, 6, .3f); z.mk.Prism(rust, c + up * 5.5f, (d + up * .7f).normalized, .25f, 3f, 6, .3f); }
            ArtistBits.PostCollider(z.root, c, 6f, .8f);
        }

        void Mountain(Plot z)
        {
            var up = Vector3.up; var c = z.At(-4f, 0); var rot = Quaternion.LookRotation(z.front);
            z.mk.Tri(WorldMaterials.Solid(new Color(.35f, .4f, .5f)), c - z.side * 6f, c + z.side * 6f, c + up * 8f + z.side * 1f, c - z.front);
            z.mk.Tri(white, c + up * 5.6f - z.side * 1f + z.front * .02f, c + up * 5.6f + z.side * 2.4f + z.front * .02f, c + up * 8f + z.side * 1f + z.front * .02f, c - z.front);
            z.mk.Box(wood, c + up * .4f + z.front * 2f, new Vector3(9f, .8f, 4f), rot); z.solid.Box(wood, c + up * .4f + z.front * 2f, new Vector3(9f, .8f, 4f), rot);
            z.solid.Box(dark, c + up * 3f, new Vector3(12f, 6f, .5f), rot);
        }

        // ---------------------------------------------------------------- the rest, each from their song
        void Tapestry(Plot z)
        {
            var up = Vector3.up; var c = z.At(-2.5f, 0); var rot = Quaternion.LookRotation(z.front); var n = z.a.name;
            if (n == "Cbdb")
            {   // Stacy's on the Lake: a little dock on a pond with a rowboat and a lantern
                var water = WorldMaterials.Solid(new Color(.15f, .3f, .45f), .95f); z.mk.Prism(water, c - up * .1f, up, 4.2f, .22f, 24); z.mk.Prism(WorldMaterials.Solid(new Color(.5f, .45f, .35f)), c - up * .15f, up, 4.5f, .16f, 24);
                z.mk.Box(wood, c + z.front * 2.6f + up * .3f, new Vector3(1.4f, .15f, 4f), rot); foreach (var s in new[] { -.6f, .6f }) foreach (var f in new[] { 1f, 3.6f }) z.mk.Prism(wood, c + z.front * f + z.side * s - up * .3f, up, .08f, .8f, 6);
                var boat = c + z.side * 1.6f + up * .08f; var hull = WorldMaterials.Solid(new Color(.75f, .2f, .15f), .5f);
                z.mk.Box(hull, boat + up * .2f, new Vector3(1f, .35f, 2.6f), rot * Quaternion.Euler(0, 20f, 0)); z.mk.Box(wood, boat + up * .35f, new Vector3(.9f, .05f, .3f), rot * Quaternion.Euler(0, 20f, 0));
                var lp = c + z.front * 4.4f + z.side * .7f; z.mk.Prism(wood, lp, up, .06f, 2f, 6); z.mk.Box(GlowM(Warm, 2.6f), lp + up * 2.1f, new Vector3(.28f, .38f, .28f), Quaternion.identity); halos.Add(lp + up * 2.1f, 1.4f, Warm, .8f);
                z.solid.Prism(water, c - up * .2f, up, 4.4f, .6f, 12);
            }
            else if (n == "Enya Lisa")
            {   // Only Time: a tall hourglass of glowing sand on a plinth
                var glass = WorldMaterials.Solid(new Color(.75f, .85f, .95f), .95f); var sand = GlowM(new Color(1f, .8f, .4f), 1.4f); var frame = wood;
                z.mk.Prism(frame, c, up, 1.6f, .4f, 8); z.mk.Prism(frame, c + up * 5.2f, up, 1.6f, .4f, 8);
                foreach (var a in new[] { 0f, 120f, 240f }) z.mk.Prism(frame, c + Quaternion.Euler(0, a, 0) * Vector3.forward * 1.4f, up, .08f, 5.4f, 6);
                z.mk.Prism(glass, c + up * .4f, up, 1.2f, 2.4f, 16, .1f); z.mk.Prism(glass, c + up * 2.8f, up, .12f, 2.4f, 16, 10f);
                z.mk.Prism(sand, c + up * .4f, up, 1.1f, .9f, 16, .55f); z.mk.Prism(sand, c + up * 3.9f, up, .6f, 1.1f, 16, 1.6f); z.mk.Prism(sand, c + up * 1.3f, up, .04f, 1.5f, 6);
                halos.Add(c + up * 2.8f, 2.4f, new Color(1f, .8f, .4f), .5f); ArtistBits.PostCollider(z.root, c, 5.6f, 1.6f);
            }
            else if (n == "TAUK")
            {   // Mindshift: a big head in profile, cut from dark steel, with glowing gears turning slowly inside it
                var steelD = WorldMaterials.Solid(new Color(.2f, .22f, .25f), .6f); var hc = c + up * 3.2f;
                z.mk.Blob(steelD, hc, new Vector3(.4f, 2.4f, 2.1f), z.ctx.Rng, 0f); z.mk.Blob(steelD, hc - up * 1.8f + z.side * .1f, new Vector3(.4f, 1f, 1.1f), z.ctx.Rng, 0f); z.mk.Box(steelD, hc + up * .2f - z.side * 0f + z.front * 0f + Vector3.Cross(up, z.side) * 2.1f, new Vector3(.4f, .8f, .5f), Quaternion.LookRotation(z.side));
                for (var k = 0; k < 3; k++) { var gc = hc + up * (.6f - k * .7f) + Vector3.Cross(up, z.side) * (k - 1f) * .7f; var gm = GlowM(new[] { new Color(1f, .6f, .2f), new Color(.3f, .8f, 1f), new Color(.8f, .4f, 1f) }[k], 2.2f); z.mk.Prism(gm, gc - z.side * .25f, z.side, .55f - k * .1f, .5f, 10); for (var t = 0; t < 8; t++) z.mk.Box(gm, gc + Quaternion.AngleAxis(t * 45f, z.side) * up * (.65f - k * .1f), new Vector3(.5f, .2f, .2f), Quaternion.LookRotation(z.side) * Quaternion.Euler(0, 0, t * 45f)); }
                halos.Add(hc, 2.8f, new Color(.9f, .6f, 1f), .4f); z.mk.Prism(stoneM, c, up, 1.2f, 1f, 8); z.solid.Box(steelD, hc - up * 1f, new Vector3(1f, 6f, 4.4f), Quaternion.LookRotation(z.side));
            }
            else if (n == "The Jauntee")
            {   // Valley Girl: a giant pink roller skate with glowing wheels
                var pink = WorldMaterials.Solid(new Color(1f, .45f, .7f), .6f); var sc = c + up * .6f; var r2 = Quaternion.LookRotation(z.side);
                z.mk.Box(pink, sc + up * .6f, new Vector3(1.6f, .6f, 3.6f), r2); z.mk.Box(pink, sc + up * 2f - z.side * 1f, new Vector3(1.6f, 2.4f, 1.6f), r2); z.mk.Box(white, sc + up * 3.25f - z.side * 1f, new Vector3(1.7f, .25f, 1.7f), r2);
                for (var k = 0; k < 6; k++) z.mk.Box(white, sc + up * (1.6f + k * .3f) - z.side * .2f, new Vector3(1.2f, .05f, .05f), r2);
                foreach (var s in new[] { -1.1f, 1.1f }) foreach (var f in new[] { -.6f, .6f }) z.mk.Prism(GlowM(new Color(.5f, 1f, .9f), 2.4f), sc + z.side * s + z.front * f - up * .1f, z.front * Mathf.Sign(f), .45f, .3f, 14);
                halos.Add(sc, 2.6f, new Color(.5f, 1f, .9f), .45f); z.solid.Box(pink, sc + up * 1.4f, new Vector3(1.8f, 3f, 3.8f), r2);
            }
            else if (n == "The Werks")
            {   // Into the Moss: a garden of mossy boulders with glowing moss and little mushrooms
                var moss = WorldMaterials.Solid(new Color(.25f, .45f, .2f)); var glowMoss = GlowM(new Color(.5f, 1f, .4f), 1.2f);
                for (var k = 0; k < 6; k++) { var bp = c + Quaternion.Euler(0, k * 60f + 15f, 0) * Vector3.forward * (k == 0 ? 0f : 2.6f); bp.y = z.ctx.Ground(bp.x, bp.z); var s = k == 0 ? 1.8f : z.ctx.Range(.8f, 1.3f); z.mk.Blob(stoneM, bp + up * .5f * s, new Vector3(1.2f, .9f, 1.1f) * s, z.ctx.Rng, .3f); soft.Blob(moss, bp + up * 1.2f * s, new Vector3(1.1f, .4f, 1f) * s, z.ctx.Rng, .3f); for (var g = 0; g < 4; g++) z.mk.Blob(glowMoss, bp + up * 1.3f * s + new Vector3(z.ctx.Range(-.6f, .6f), 0, z.ctx.Range(-.6f, .6f)) * s, Vector3.one * .12f, z.ctx.Rng, 0f); z.solid.Box(stoneM, bp + up * .6f * s, new Vector3(2f, 1.6f, 2f) * s, Quaternion.identity); }
                halos.Add(c + up * 1.6f, 4f, new Color(.5f, 1f, .4f), .3f);
            }
            else if (n == "Voodoo Visionary")
            {   // Cold Shallow Moon: a pale moon hanging low over a shallow pool that holds its reflection
                var moonM = GlowM(new Color(.85f, .92f, 1f), 1.8f); var mp = c + up * 5f - z.front * 1f;
                z.mk.Blob(moonM, mp, Vector3.one * 1.6f, z.ctx.Rng, 0f); halos.Add(mp, 5f, new Color(.8f, .9f, 1f), .5f);
                z.mk.Prism(WorldMaterials.Solid(new Color(.1f, .15f, .25f), .97f), c + z.front * 1.5f - up * .1f, up, 3f, .22f, 24); z.mk.Prism(stoneM, c + z.front * 1.5f - up * .15f, up, 3.3f, .14f, 24);
                z.mk.Prism(GlowM(new Color(.8f, .9f, 1f), 1f), c + z.front * 1.5f + up * .12f, up, 1f, .02f, 20);
                foreach (var s in new[] { -1f, 1f }) { z.mk.Prism(wood, mp + z.side * s * .3f, up, .02f, 6f, 4); }
                z.solid.Prism(stoneM, c + z.front * 1.5f - up * .2f, up, 3.3f, .6f, 12);
            }
            else if (n == "velvet stripe")
            {   // Haunt You Here: a lone doorway frame standing in the woods, warm light inside, a faint figure in it
                var frameM = WorldMaterials.Solid(new Color(.3f, .22f, .18f)); var dp = c;
                foreach (var s in new[] { -1f, 1f }) { z.mk.Box(frameM, dp + z.side * s * .75f + up * 1.3f, new Vector3(.2f, 2.6f, .25f), rot); ArtistBits.PostCollider(z.root, dp + z.side * s * .75f, 2.6f, .15f); }
                z.mk.Box(frameM, dp + up * 2.7f, new Vector3(1.7f, .2f, .25f), rot); z.mk.Box(GlowM(new Color(1f, .7f, .45f), 1.2f), dp + up * 1.25f - z.front * .1f, new Vector3(1.3f, 2.5f, .02f), rot);
                var ghost = WorldMaterials.Glow(new Color(.85f, .9f, 1f), .5f); z.mk.Box(ghost, dp + up * 1f - z.front * .25f, new Vector3(.4f, 1f, .2f), rot); z.mk.Blob(ghost, dp + up * 1.7f - z.front * .25f, Vector3.one * .15f, z.ctx.Rng, 0f);
                halos.Add(dp + up * 1.3f + z.front * .4f, 2f, new Color(1f, .7f, .45f), .5f);
            }
            else
            {   // a tapestry stage
                var h = Mathf.Abs(n.GetHashCode());
                z.mk.Box(wood, c + up * .3f, new Vector3(6f, .6f, 4f), rot); z.solid.Box(wood, c + up * .3f, new Vector3(6f, .6f, 4f), rot);
                foreach (var s in new[] { -1f, 1f }) { z.mk.Prism(wood, c + z.side * s * 2.8f - z.front * 1.8f, up, .1f, 4.2f, 6); ArtistBits.PostCollider(z.root, c + z.side * s * 2.8f - z.front * 1.8f, 4.2f, .12f); }
                for (var b = 0; b < 8; b++) z.mk.Box(WorldMaterials.Solid(Dye[(b + h) % Dye.Length], .3f), c - z.front * 1.8f + up * (.8f + b * .4f), new Vector3(5.5f, .4f, .04f), rot);
                StringLights(z.mk, c + z.side * -2.8f + up * 4f - z.front * 1.8f, c + z.side * 2.8f + up * 4f - z.front * 1.8f, Warm, 8);
            }
        }
        Material stoneM => WorldMaterials.Solid(new Color(.5f, .48f, .45f));
    }
}
