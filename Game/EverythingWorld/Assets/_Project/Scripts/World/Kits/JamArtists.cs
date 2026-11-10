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
            z.mk.Box(dark, mix + up * 1f, new Vector3(4f, 2f, 3f), rot); z.solid.Box(dark, mix + up * 1f, new Vector3(4f, 2f, 3f), rot);
            z.mk.Box(canvas, mix + up * 2.9f, new Vector3(4.6f, .1f, 3.6f), rot);
            for (var k = 0; k < 6; k++) z.mk.Box(GlowM(Dye[k], 1.6f), mix + up * 1.6f - z.front * 1.51f + z.side * ((k - 2.5f) * .5f), new Vector3(.3f, .2f, .02f), rot);
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
            var up = Vector3.up; var c = z.At(-3f, 0); var rot = Quaternion.LookRotation(z.front) * Quaternion.Euler(0, 0, 10f);
            z.mk.Box(metal, c + up * 2f, new Vector3(6f, 1.4f, 1.2f), rot); z.mk.Box(dark, c + up * 2f, new Vector3(5.6f, .8f, 1.25f), rot);
            for (var k = 0; k < 10; k++) z.mk.Box(dark, c + up * 2f + z.front * .63f + z.side * ((k - 4.5f) * .5f), new Vector3(.3f, .3f, .02f), rot);
            z.solid.Box(metal, c + up * 2f, new Vector3(6f, 1.6f, 1.4f), rot);
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
            var up = Vector3.up; var c = z.At(-3f, 0); var ch = WorldMaterials.Solid(new Color(.98f, .95f, .82f));
            z.mk.Prism(ch, c, up, .9f, 5f, 12); for (var k = 0; k < 8; k++) z.mk.Box(ch, c + up * (5.2f + k * .1f) + Quaternion.Euler(0, k * 45f, 0) * Vector3.forward * .7f, new Vector3(.18f, 1.4f, .18f), Quaternion.Euler(0, k * 45f, 0) * Quaternion.Euler(30f, 0, 0));
            z.mk.Box(WorldMaterials.Solid(new Color(.2f, .45f, .8f)), c + up * 1f, new Vector3(2f, 1.8f, .02f), Quaternion.LookRotation(z.front)); ArtistBits.PostCollider(z.root, c, 5f, .9f);
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

        void Tapestry(Plot z)
        {
            var up = Vector3.up; var c = z.At(-2f, 0); var rot = Quaternion.LookRotation(z.front); var h = Mathf.Abs(z.a.name.GetHashCode());
            z.mk.Box(wood, c + up * .3f, new Vector3(6f, .6f, 4f), rot); z.solid.Box(wood, c + up * .3f, new Vector3(6f, .6f, 4f), rot);
            foreach (var s in new[] { -1f, 1f }) { z.mk.Prism(wood, c + z.side * s * 2.8f - z.front * 1.8f, up, .1f, 4.2f, 6); ArtistBits.PostCollider(z.root, c + z.side * s * 2.8f - z.front * 1.8f, 4.2f, .12f); }
            for (var b = 0; b < 8; b++) z.mk.Box(WorldMaterials.Solid(Dye[(b + h) % Dye.Length], .3f), c - z.front * 1.8f + up * (.8f + b * .4f), new Vector3(5.5f, .4f, .04f), rot);
            z.mk.Prism(GlowM(Dye[h % Dye.Length], 1.6f), c - z.front * 1.75f + up * 2.4f, z.front, 1f, .03f, 16);
            StringLights(z.mk, c + z.side * -2.8f + up * 4f - z.front * 1.8f, c + z.side * 2.8f + up * 4f - z.front * 1.8f, Warm, 8);
        }
    }
}
