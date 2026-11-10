using System.Collections.Generic;
using UnityEngine;

namespace EverythingWorld.World
{
    /// The Wilds' artists. The Grateful Dead have a whole district: the Wall of Sound (a towering wall of speakers
    /// behind a stage), a parade of giant dancing bears in every colour, a giant skull split red and blue by a
    /// lightning bolt, two terrapins making music, a rose garden with a skeleton sitting among the roses,
    /// Franklin's Tower with its bell, a floating Box of Rain, the Eyes of the World on a pedestal, and a painted
    /// bus. Queens of the Stone Age: a stretch of desert road with an old car, its radio glowing, under a broadcast
    /// tower, and a generator party (amps and drums round a fire, powered by a chugging generator). Tame Impala: a
    /// turning kaleidoscope wheel and a painted elephant. Psychedelic Porn Crumpets: a giant glowing tomato.
    /// Iron Butterfly: an iron butterfly. Everyone else: a flower sculpture, a tapestry tent, a mirror ball, an
    /// eye totem or a sun dial.
    public partial class PsychKit
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
            area.artistName = a.name; area.stationId = a.stationId; area.radius = a.r; area.glowColor = Rainbow[Mathf.Abs(a.name.GetHashCode()) % Rainbow.Length];
            var z = new Plot { ctx = ctx, root = root, labels = labels, mk = new MeshKit(), solid = new MeshKit(), pictures = new MeshKit(), a = a, c = ctx.OnGround(a.x, a.z), front = front, side = Vector3.Cross(Vector3.up, front), glows = new List<Renderer>(), area = area };
            switch (a.name)
            {
                case "Grateful Dead": Dead(z); break;
                case "Queens of the Stone Age": Queens(z); break;
                case "Tame Impala": Kaleidoscope(z); break;
                case "Psychedelic Porn Crumpets": Tomato(z); break;
                case "Iron Butterfly": Butterfly(z); break;
                default: Small(z); break;
            }
            var reach = Foot(a);
            Board(z, z.At(reach * .85f, -reach * .5f - 2.5f), front);
            ArtistBits.Plaque(z.mk, z.solid, labels, a, z.At(reach * .85f + 1.5f, reach * .5f + 2.5f), front, wood, dark, Ink, 1.6f);
            LavaLamp(ctx, z.mk, z.At(reach * .85f, reach * .5f + 5f), .45f, Mathf.Abs(a.name.GetHashCode() / 7) % Rainbow.Length);
            if (z.area.glowLight == null) z.area.glowLight = ArtistBits.PointLight(root, z.c + Vector3.up * 5f + front * 4f, area.glowColor, HasStation(a) ? 18f : 12f, 1.4f);
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
            z.mk.Box(dark, c - Vector3.up * .1f, new Vector3(w, size + .6f, .1f), rot); z.solid.Box(dark, c - Vector3.up * .1f, new Vector3(w, size + .6f, .2f), rot);
            for (var k = 0; k < n * 2; k++) z.mk.Box(WorldMaterials.Solid(Rainbow[k % Rainbow.Length]), c + Vector3.up * (size / 2f + .35f) + side * ((k + .5f) * w / (n * 2f) - w / 2f) - dir * .02f, new Vector3(w / (n * 2f), .12f, .12f), rot);
            ArtistBits.CoverWall(z.pictures, z.labels, z.ctx, z.a, c + dir * .055f + Vector3.up * .08f, dir, n, size, n, true, Ink);
        }

        Transform Mover(Plot z, string name, MeshKit k, Vector3 at, Quaternion rot)
        {
            var t = new GameObject(name).transform; t.SetParent(z.root, false);
            k.BuildSingle(name, t, false); t.SetPositionAndRotation(at, rot); return t;
        }

        // ---------------------------------------------------------------- the Grateful Dead
        void Dead(Plot z)
        {
            var up = Vector3.up; var ctx = z.ctx; var rot = Quaternion.LookRotation(z.front);
            // the Wall of Sound: a towering wall of speaker cabinets behind a stage
            var w = z.At(-20f, 0); var grille = WorldMaterials.Solid(new Color(.22f, .2f, .2f)); var cab = WorldMaterials.Solid(new Color(.12f, .1f, .1f));
            for (var x = -6; x <= 6; x++) for (var y = 0; y < 8; y++)
            {
                if (y > 5 && Mathf.Abs(x) > 3 + (7 - y)) continue;
                var p = w + z.side * (x * 2.1f) + up * (1f + y * 2f) - z.front * (Mathf.Abs(x) * .25f);
                z.mk.Box(cab, p, new Vector3(2f, 1.9f, 1.4f), rot);
                if ((x + y) % 2 == 0) { z.mk.Prism(grille, p + z.front * .7f + z.side * -.45f, z.front, .4f, .04f, 12); z.mk.Prism(grille, p + z.front * .7f + z.side * .45f, z.front, .4f, .04f, 12); }
                else z.mk.Box(grille, p + z.front * .71f, new Vector3(1.7f, 1.6f, .02f), rot);
            }
            z.solid.Box(cab, w + up * 8f, new Vector3(28f, 16f, 2f), rot);
            var stage = w + z.front * 5f;
            z.mk.Box(wood, stage + up * .7f, new Vector3(22f, 1.4f, 8f), rot); z.solid.Box(wood, stage + up * .7f, new Vector3(22f, 1.4f, 8f), rot);
            for (var k = 0; k < 4; k++) z.mk.Prism(WorldMaterials.Solid(new Color(.8f, .8f, .82f), .9f), stage + up * 1.4f + z.side * ((k - 1.5f) * 4.5f) + z.front * 2f, up, .04f, 1.6f, 6);
            z.mk.Prism(WorldMaterials.Solid(Rainbow[1]), stage + up * 1.6f - z.front * 1.5f, z.front, .5f, .5f, 14);
            for (var k = 0; k < 6; k++) z.mk.Prism(GlowM(Rainbow[k], 2.6f), w + up * 15.5f + z.side * ((k - 2.5f) * 3.4f) + z.front * 1f, (z.front - up).normalized, .4f, .5f, 10);
            halos.Add(stage + up * 6f + z.front * 3f, 9f, new Color(1f, .7f, .4f), .3f);
            ArtistBits.PointLight(z.root, stage + up * 7f + z.front * 6f, new Color(1f, .8f, .55f), 26f, 2.2f);
            ArtistBits.Label(z.labels, "THE WALL OF SOUND", w + up * 17.5f + z.front * 1f, z.front, .45f, Ink, TextAnchor.MiddleCenter);

            // the dancing bears, a parade of five in every colour
            var bears = new List<Transform>();
            for (var k = 0; k < 5; k++)
            {
                var at = z.At(6f + Mathf.Sin(k) * 2f, -14f + k * 7f);
                var bk = new MeshKit(); var fur = WorldMaterials.Solid(Rainbow[k % Rainbow.Length], .4f);
                bk.Blob(fur, up * 2.1f, new Vector3(1.1f, 1.4f, .9f), new System.Random(k), 0f);
                bk.Blob(fur, up * 3.9f, new Vector3(.8f, .75f, .75f), new System.Random(k + 9), 0f);
                foreach (var s in new[] { -1f, 1f }) { bk.Blob(fur, up * 4.6f + Vector3.right * s * .55f, Vector3.one * .28f, new System.Random(k + 20), 0f); bk.Box(fur, up * 3f + Vector3.right * s * 1.1f, new Vector3(.35f, 1.2f, .35f), Quaternion.Euler(0, 0, s * -35f)); }
                bk.Box(fur, up * .7f + Vector3.right * -.45f, new Vector3(.4f, 1.3f, .4f), Quaternion.Euler(20f, 0, 0));
                bk.Box(fur, up * .9f + Vector3.right * .5f + Vector3.forward * .4f, new Vector3(.4f, 1.1f, .4f), Quaternion.Euler(-50f, 0, 0));
                bk.Blob(WorldMaterials.Solid(new Color(.15f, .1f, .1f)), up * 3.8f + Vector3.forward * .72f, Vector3.one * .14f, new System.Random(k + 30), 0f);
                foreach (var s in new[] { -.25f, .25f }) bk.Blob(dark, up * 4.1f + Vector3.forward * .65f + Vector3.right * s, Vector3.one * .08f, new System.Random(k + 40), 0f);
                var t = Mover(z, "Dancing bear", bk, at, Quaternion.LookRotation(z.front));
                bears.Add(t); ArtistBits.PostCollider(z.root, at, 4f, .8f);
            }
            life.bears = bears.ToArray();

            // the skull split red and blue by a lightning bolt
            var sk = z.At(-4f, -24f); var bone = WorldMaterials.Solid(new Color(.95f, .94f, .9f)); var red = WorldMaterials.Solid(new Color(.85f, .12f, .15f)); var blue = WorldMaterials.Solid(new Color(.15f, .35f, .85f));
            z.mk.Box(dark, sk + up * 1f, new Vector3(4f, 2f, 3f), rot); z.solid.Box(dark, sk + up * 4f, new Vector3(5f, 8f, 3f), rot);
            var head = sk + up * 6f;
            z.mk.Blob(red, head - z.side * 1f + up * .6f, new Vector3(1.6f, 2.2f, 2f), ctx.Rng, 0f);
            z.mk.Blob(blue, head + z.side * 1f + up * .6f, new Vector3(1.6f, 2.2f, 2f), ctx.Rng, 0f);
            z.mk.Blob(bone, head - up * 1.3f, new Vector3(2.2f, 1.3f, 1.8f), ctx.Rng, 0f);
            foreach (var s in new[] { -1f, 1f }) z.mk.Blob(dark, head + z.front * 1.75f + z.side * s * .9f - up * .4f, new Vector3(.55f, .6f, .2f), ctx.Rng, 0f);
            z.mk.Blob(dark, head + z.front * 1.8f - up * 1.1f, new Vector3(.25f, .35f, .2f), ctx.Rng, 0f);
            for (var k = 0; k < 6; k++) z.mk.Box(dark, head + z.front * 1.75f - up * 2f + z.side * ((k - 2.5f) * .32f), new Vector3(.08f, .45f, .05f), rot);
            Vector3[] bolt = { new Vector3(.5f, 2.6f, 0), new Vector3(-.6f, .9f, 0), new Vector3(.4f, .7f, 0), new Vector3(-.4f, -1.2f, 0) };
            for (var k = 0; k < 3; k++) { var p0 = head + z.front * 2f + z.side * bolt[k].x + up * bolt[k].y; var p1 = head + z.front * 2f + z.side * bolt[k + 1].x + up * bolt[k + 1].y; z.mk.Box(white, (p0 + p1) / 2f, new Vector3(.5f, .2f, Vector3.Distance(p0, p1) + .2f), Quaternion.LookRotation(p1 - p0, z.front)); }
            halos.Add(head + z.front * 3f, 4f, Ink, .3f);

            // two terrapins making music, a banjo and a tambourine
            for (var k = 0; k < 2; k++)
            {
                var tp = z.At(16f, 14f + k * 6f); var shell = WorldMaterials.Solid(new Color(.3f, .55f, .3f), .5f); var skin = WorldMaterials.Solid(new Color(.6f, .75f, .4f));
                z.mk.Blob(shell, tp + up * 1.6f, new Vector3(1.6f, 1.2f, 1.4f), ctx.Rng, 0f);
                for (var s = 0; s < 6; s++) z.mk.Blob(WorldMaterials.Solid(Rainbow[(s + k * 3) % Rainbow.Length], .5f), tp + up * 2.2f + Quaternion.Euler(0, s * 60f, 0) * Vector3.forward * .9f, new Vector3(.4f, .15f, .4f), ctx.Rng, 0f);
                z.mk.Blob(skin, tp + up * 3.1f + z.front * .4f, new Vector3(.55f, .6f, .55f), ctx.Rng, 0f);
                if (k == 0) { z.mk.Prism(white, tp + up * 2f + z.front * 1.4f, z.front, .45f, .1f, 14); z.mk.Box(wood, tp + up * 2.6f + z.front * 1.45f + z.side * .7f, new Vector3(1.4f, .1f, .08f), rot * Quaternion.Euler(0, 0, 30f)); }
                else { z.mk.Prism(WorldMaterials.Solid(new Color(.85f, .7f, .3f), .8f), tp + up * 3f + z.front * 1.3f + z.side * .8f, z.front, .4f, .12f, 14); }
                z.solid.Box(shell, tp + up * 1.4f, new Vector3(3f, 2.8f, 2.8f), rot);
            }

            // a rose garden with a skeleton sitting among the roses
            var rg = z.At(-4f, 22f); var leaf = WorldMaterials.Solid(new Color(.2f, .45f, .22f)); var rose = WorldMaterials.Solid(new Color(.85f, .08f, .2f), .6f);
            for (var k = 0; k < 14; k++) { var p = ctx.OnGround(rg.x + ctx.Range(-6f, 6f), rg.z + ctx.Range(-6f, 6f)); soft.Blob(leaf, p + up * .6f, new Vector3(.8f, .6f, .8f), ctx.Rng, .3f); for (var r = 0; r < 4; r++) soft.Blob(rose, p + up * (1f + ctx.Range(0f, .3f)) + new Vector3(ctx.Range(-.6f, .6f), 0, ctx.Range(-.6f, .6f)), Vector3.one * .22f, ctx.Rng, .2f); }
            var sp = rg + up * .5f; var bones = WorldMaterials.Solid(new Color(.93f, .92f, .86f));
            z.mk.Box(wood, sp + up * .1f, new Vector3(2f, .3f, 1f), rot); z.solid.Box(wood, sp, new Vector3(2f, 1f, 1f), rot);
            z.mk.Box(bones, sp + up * 1.1f, new Vector3(.15f, 1.3f, .15f), rot);
            for (var r = 0; r < 4; r++) z.mk.Box(bones, sp + up * (.8f + r * .2f), new Vector3(.7f - r * .08f, .07f, .45f), rot);
            z.mk.Blob(bones, sp + up * 2.05f, new Vector3(.32f, .38f, .34f), ctx.Rng, 0f);
            foreach (var s in new[] { -.12f, .12f }) z.mk.Blob(dark, sp + up * 2.08f + z.front * .3f + z.side * s, Vector3.one * .07f, ctx.Rng, 0f);
            foreach (var s in new[] { -1f, 1f }) { z.mk.Box(bones, sp + up * 1.2f + z.side * s * .45f + z.front * .3f, new Vector3(.07f, .07f, .7f), rot); z.mk.Box(bones, sp + up * .35f + z.side * s * .2f + z.front * .5f, new Vector3(.08f, .08f, .9f), rot); }
            for (var r = 0; r < 3; r++) z.mk.Blob(rose, sp + up * 1.25f + z.front * .7f + z.side * ((r - 1) * .15f), Vector3.one * .16f, ctx.Rng, .2f);

            // Franklin's Tower with its bell
            var ft = z.At(-20f, 24f);
            z.mk.Prism(WorldMaterials.Solid(new Color(.75f, .68f, .6f)), ft - up * .3f, up, 2.2f, 14f, 8, .8f); z.solid.Prism(dark, ft, up, 2.2f, 14f, 8);
            for (var k = 0; k < 4; k++) z.mk.Box(dark, ft + up * 14.5f + Quaternion.Euler(0, k * 90f, 0) * Vector3.forward * 1.5f, new Vector3(.4f, 2.4f, .4f), Quaternion.identity);
            z.mk.Cone(WorldMaterials.Solid(new Color(.6f, .2f, .2f)), ft + up * 15.7f, 2.3f, 3f, 8);
            var bell = new Material(GlowM(new Color(1f, .8f, .35f), .6f)) { name = "Franklin's bell" };
            z.mk.Prism(bell, ft + up * 13.6f, up, .8f, 1.4f, 12, .55f);
            life.bell = bell; life.tower = ft + up * 14f;

            // the Box of Rain, floating, a little cloud over it
            var br = z.At(6f, 24f) + up * 6f;
            var bk2 = new MeshKit(); bk2.Box(WorldMaterials.Solid(new Color(.55f, .4f, .28f)), Vector3.zero, new Vector3(2.4f, 2.4f, 2.4f), Quaternion.Euler(10f, 30f, 5f));
            bk2.Box(WorldMaterials.Glow(new Color(.6f, .85f, 1f), 1f), Vector3.up * 1.21f, new Vector3(2f, .05f, 2f), Quaternion.Euler(10f, 30f, 5f));
            life.rainBox = Mover(z, "Box of Rain", bk2, br, Quaternion.identity);
            for (var k = 0; k < 4; k++) soft.Blob(WorldMaterials.Solid(new Color(.85f, .88f, .95f)), br + up * 4f + z.side * ((k - 1.5f) * 1.4f), new Vector3(1.4f, .9f, 1.2f), ctx.Rng, .3f);
            life.rainCloud = br + up * 3.4f;

            // the Eyes of the World on a pedestal
            var ey = z.At(-24f, -22f) + up * 4f;
            z.mk.Prism(white, ey - up * 4.3f, up, 1.2f, 3f, 10, .8f); z.solid.Prism(white, ey - up * 4.3f, up, 1.2f, 3f, 10);
            var ek = new MeshKit(); ek.Blob(WorldMaterials.Solid(new Color(.98f, .97f, .95f)), Vector3.zero, Vector3.one * 2.2f, new System.Random(3), 0f);
            var iris = new Material(GlowM(new Color(.25f, .6f, 1f), .6f)) { name = "Iris" };
            ek.Prism(iris, Vector3.forward * 1.9f, Vector3.forward, 1.05f, .3f, 18); ek.Prism(dark, Vector3.forward * 2.15f, Vector3.forward, .45f, .1f, 14);
            life.eye = Mover(z, "Eyes of the World", ek, ey, Quaternion.LookRotation(z.front)); life.iris = iris;
            halos.Add(ey + z.front * 2.5f, 3.5f, new Color(.3f, .6f, 1f), .4f);

            // the painted bus
            var bus = z.At(14f, -24f); var bdir = z.side; var br2 = Quaternion.LookRotation(bdir);
            for (var b = 0; b < 6; b++) z.mk.Box(WorldMaterials.Solid(Rainbow[b], .5f), bus + up * (1f + b * .35f), new Vector3(2.6f, .36f, 10f), br2);
            z.mk.Box(WorldMaterials.Solid(new Color(.2f, .3f, .4f), .9f), bus + up * 2.6f, new Vector3(2.62f, .9f, 8.6f), br2);
            foreach (var s in new[] { -1f, 1f }) foreach (var zz in new[] { -3.5f, 3.5f }) z.mk.Prism(dark, bus + Vector3.Cross(up, bdir) * s * 1.2f + bdir * zz + up * .55f, Vector3.Cross(up, bdir) * s, .55f, .3f, 12);
            z.solid.Box(dark, bus + up * 1.8f, new Vector3(2.7f, 3.6f, 10.2f), br2);
            life.dead = z.c;
        }

        // ---------------------------------------------------------------- Queens of the Stone Age
        void Queens(Plot z)
        {
            var up = Vector3.up; var ctx = z.ctx; var rot = Quaternion.LookRotation(z.side);
            // a stretch of desert road with a dashed line, an old car on it, its radio glowing
            var roadA = z.At(4f, -16f); var roadB = z.At(4f, 16f);
            var pts = new List<Vector3>(); for (var k = 0; k <= 10; k++) { var p = Vector3.Lerp(roadA, roadB, k / 10f); pts.Add(ctx.OnGround(p.x, p.z)); }
            for (var k = 0; k + 1 < pts.Count; k++) { z.mk.Quad(WorldMaterials.Solid(new Color(.18f, .17f, .2f), .3f), pts[k] - z.front * 3f + up * .06f, pts[k] + z.front * 3f + up * .06f, pts[k + 1] + z.front * 3f + up * .06f, pts[k + 1] - z.front * 3f + up * .06f, pts[k] - up); if (k % 2 == 0) z.mk.Box(WorldMaterials.Solid(new Color(.95f, .85f, .3f)), (pts[k] + pts[k + 1]) / 2f + up * .08f, new Vector3(.15f, .02f, 1.4f), Quaternion.LookRotation(pts[k + 1] - pts[k])); }
            var car = z.At(4f, -4f); var body = WorldMaterials.Solid(new Color(.75f, .1f, .1f), .8f); var crot = Quaternion.LookRotation(z.side);
            z.mk.Box(body, car + up * .8f, new Vector3(2f, .7f, 5f), crot); z.mk.Box(WorldMaterials.Solid(new Color(.2f, .25f, .3f), .9f), car + up * 1.4f + z.side * .4f, new Vector3(1.8f, .5f, .1f), crot * Quaternion.Euler(-30f, 0, 0));
            foreach (var s in new[] { -1f, 1f }) foreach (var zz in new[] { -1.6f, 1.6f }) z.mk.Prism(dark, car + z.front * s * .95f + z.side * zz + up * .42f, z.front * s, .42f, .28f, 12);
            var radio = new Material(GlowM(new Color(1f, .55f, .2f), 1.2f)) { name = "Car radio" };
            z.mk.Box(radio, car + up * 1.2f + z.side * .2f, new Vector3(.5f, .15f, .05f), crot);
            z.solid.Box(body, car + up * .9f, new Vector3(2.1f, 1.8f, 5.1f), crot);
            life.radio = radio;
            // a broadcast tower with a dish and a blinking red light
            var tw = z.At(-10f, 10f);
            for (var k = 0; k < 3; k++) z.mk.Box(metal, tw + Quaternion.Euler(0, k * 120f, 0) * Vector3.forward * .9f + up * 11f, new Vector3(.18f, 22f, .18f), Quaternion.identity);
            for (var y = 2f; y < 22f; y += 2.5f) z.mk.Prism(metal, tw + up * y, up, 1f, .1f, 3);
            z.mk.Prism(white, tw + up * 16f + z.front * .6f, (z.front + up * .3f).normalized, 1.6f, .3f, 16, .2f);
            var beacon = new Material(GlowM(new Color(1f, .1f, .1f), 2f)) { name = "Tower beacon" };
            z.mk.Blob(beacon, tw + up * 22.4f, Vector3.one * .4f, ctx.Rng, 0f);
            life.beacon = beacon; life.tower2 = tw + up * 22.4f;
            z.solid.Box(metal, tw + up * 11f, new Vector3(2f, 22f, 2f), Quaternion.identity);
            // the generator party: amps and a drum kit round a fire, cables back to a chugging generator
            var gp = z.At(-6f, -10f);
            for (var k = 0; k < 5; k++) { var d = Quaternion.Euler(0, k * 72f, 0) * Vector3.forward; var amp = gp + d * 4.5f; z.mk.Box(dark, amp + up * .8f, new Vector3(1.6f, 1.6f, .9f), Quaternion.LookRotation(-d)); z.mk.Box(WorldMaterials.Solid(new Color(.3f, .28f, .25f)), amp + up * .8f - d * .46f, new Vector3(1.4f, 1.3f, .02f), Quaternion.LookRotation(-d)); z.solid.Box(dark, amp + up * .8f, new Vector3(1.6f, 1.6f, .9f), Quaternion.LookRotation(-d)); }
            var gen = gp - z.side * 9f; z.mk.Box(WorldMaterials.Solid(new Color(.85f, .6f, .15f), .4f), gen + up * .7f, new Vector3(1.4f, 1.2f, 2f), rot); z.mk.Box(dark, gen + up * 1.4f, new Vector3(1.5f, .2f, 2.1f), rot); z.solid.Box(dark, gen + up * .7f, new Vector3(1.4f, 1.4f, 2f), rot);
            for (var k = 0; k < 10; k++) { var p = Vector3.Lerp(gen, gp, k / 10f); p = ctx.OnGround(p.x, p.z); z.mk.Box(dark, p + up * .05f + z.front * Mathf.Sin(k) * .4f, new Vector3(.08f, .05f, 1f), Quaternion.LookRotation(gp - gen)); }
            for (var k = 0; k < 8; k++) z.mk.Blob(WorldMaterials.Solid(new Color(.5f, .45f, .4f)), gp + Quaternion.Euler(0, k * 45f, 0) * Vector3.forward * 1.1f + up * .2f, new Vector3(.3f, .22f, .3f), ctx.Rng, .3f);
            var fl = ArtistBits.PointLight(z.root, gp + up * 1f, new Color(1f, .55f, .2f), 12f, 2f); fl.gameObject.AddComponent<FireFlicker>().baseIntensity = 2f;
            ArtistBits.PostCollider(z.root, gp, .6f, 1.4f);
            life.fires.Add(gp + up * .3f); life.generator = gen;
        }

        void Kaleidoscope(Plot z)
        {
            var up = Vector3.up; var c = z.At(-3f, 0); var rot = Quaternion.LookRotation(z.front);
            foreach (var s in new[] { -1f, 1f }) { z.mk.Box(metal, c + z.side * s * 3f + up * 4f, new Vector3(.3f, 8f, .3f), rot); ArtistBits.PostCollider(z.root, c + z.side * s * 3f, 8f, .2f); }
            var k = new MeshKit();
            for (var i = 0; i < 12; i++)
            {
                float a0 = i * Mathf.PI / 6f, a1 = (i + 1) * Mathf.PI / 6f;
                var p0 = new Vector3(Mathf.Cos(a0), Mathf.Sin(a0), 0) * 4f; var p1 = new Vector3(Mathf.Cos(a1), Mathf.Sin(a1), 0) * 4f;
                var m = WorldMaterials.Glow(Rainbow[i % Rainbow.Length], 1.3f);
                k.Tri(m, Vector3.zero, p0, p1, Vector3.back); k.Tri(m, Vector3.zero, p1 * .55f, p0 * .55f, Vector3.forward);
                k.Tri(m, Vector3.zero, p1, p0, Vector3.forward);
            }
            life.kaleidoscope = Mover(z, "Kaleidoscope", k, c + up * 8f, rot);
            halos.Add(c + up * 8f + z.front * 1.5f, 6f, new Color(.8f, .6f, 1f), .35f);
            // a painted elephant
            var el = z.At(4f, 9f); var hide = WorldMaterials.Solid(new Color(.55f, .55f, .65f));
            z.mk.Blob(hide, el + up * 2.6f, new Vector3(1.6f, 1.4f, 2.4f), z.ctx.Rng, 0f); z.mk.Blob(hide, el + up * 3.2f + z.side * -2.2f, Vector3.one * 1.1f, z.ctx.Rng, 0f);
            z.mk.Prism(hide, el + up * 2.8f + z.side * -3.1f, (-z.side - up).normalized, .3f, 2f, 8, .5f);
            foreach (var s in new[] { -1f, 1f }) { z.mk.Blob(WorldMaterials.Solid(Rainbow[s > 0 ? 0 : 3], .5f), el + up * 3.3f + z.side * -2f + z.front * s * 1f, new Vector3(.15f, .9f, .8f), z.ctx.Rng, 0f); foreach (var b in new[] { -1f, 1f }) z.mk.Prism(hide, el + z.side * b * 1.3f + z.front * s * .7f - up * .1f, up, .4f, 1.8f, 8); }
            for (var p = 0; p < 10; p++) z.mk.Blob(WorldMaterials.Solid(Rainbow[p % Rainbow.Length], .5f), el + up * (2.4f + Mathf.Sin(p) * .8f) + z.side * Mathf.Cos(p * 1.3f) * 1.4f + z.front * 1.42f * (p % 2 == 0 ? 1f : -1f), new Vector3(.3f, .3f, .05f), z.ctx.Rng, .2f);
            z.solid.Box(hide, el + up * 2f, new Vector3(3.4f, 4f, 6f), Quaternion.LookRotation(z.side));
        }

        void Tomato(Plot z)
        {
            var up = Vector3.up; var c = z.At(-3f, 0);
            z.mk.Prism(WorldMaterials.Solid(new Color(.9f, .85f, .7f)), c, up, 2.4f, 1.6f, 12); z.solid.Prism(dark, c, up, 2.4f, 1.6f, 12);
            var t = c + up * 4.2f;
            z.mk.Blob(GlowM(new Color(1f, .2f, .12f), 1.8f), t, new Vector3(2.6f, 2.2f, 2.6f), z.ctx.Rng, .1f);
            for (var k = 0; k < 5; k++) z.mk.Box(WorldMaterials.Solid(new Color(.2f, .6f, .25f)), t + up * 2.1f + Quaternion.Euler(0, k * 72f, 0) * Vector3.forward * .6f, new Vector3(.2f, .1f, 1.2f), Quaternion.Euler(-15f, k * 72f, 0));
            for (var k = 0; k < 12; k++) { var d = Quaternion.AngleAxis(k * 30f, z.front) * up; z.mk.Box(GlowM(new Color(1f, .9f, .4f), 2f), t + d * 4.2f + z.front * .3f, new Vector3(.2f, 1.6f, .1f), Quaternion.LookRotation(z.front, d)); }
            halos.Add(t + z.front * 2f, 6f, new Color(1f, .4f, .2f), .45f);
        }

        void Butterfly(Plot z)
        {
            var up = Vector3.up; var c = z.At(-3f, 0); var iron = WorldMaterials.Solid(new Color(.3f, .3f, .33f), .7f);
            z.mk.Prism(iron, c, up, .2f, 4f, 8); ArtistBits.PostCollider(z.root, c, 4f, .3f);
            var b = c + up * 5f; z.mk.Prism(iron, b - up * 1.2f, up, .25f, 2.4f, 8);
            foreach (var s in new[] { -1f, 1f })
            {
                z.mk.Tri(iron, b, b + z.side * s * 3.4f + up * 2f, b + z.side * s * 2.6f - up * .2f, b + z.front); z.mk.Tri(iron, b, b + z.side * s * 2.6f - up * .2f, b + z.side * s * 3.4f + up * 2f, b - z.front);
                z.mk.Tri(iron, b, b + z.side * s * 2.4f - up * .4f, b + z.side * s * 1.6f - up * 2.4f, b + z.front); z.mk.Tri(iron, b, b + z.side * s * 1.6f - up * 2.4f, b + z.side * s * 2.4f - up * .4f, b - z.front);
                z.mk.Blob(GlowM(Rainbow[s > 0 ? 1 : 4], 2f), b + z.side * s * 2.2f + up * .8f + z.front * .1f, Vector3.one * .4f, z.ctx.Rng, 0f);
            }
        }

        void Small(Plot z)
        {
            var up = Vector3.up; var ctx = z.ctx; var c = z.At(-2f, 0); var h = Mathf.Abs(z.a.name.GetHashCode()); var col = Rainbow[h % Rainbow.Length];
            switch (h % 5)
            {
                case 0: // a giant flower sculpture
                    z.mk.Prism(WorldMaterials.Solid(new Color(.3f, .6f, .3f)), c, up, .2f, 4.5f, 8); ArtistBits.PostCollider(z.root, c, 4.5f, .3f);
                    for (var k = 0; k < 8; k++) { var d = Quaternion.AngleAxis(k * 45f, z.front) * up; z.mk.Blob(WorldMaterials.Solid(Rainbow[(k + h) % Rainbow.Length], .5f), c + up * 5f + d * 1.3f, new Vector3(.9f, .9f, .25f), ctx.Rng, .1f); }
                    z.mk.Blob(GlowM(new Color(1f, .9f, .3f), 2f), c + up * 5f + z.front * .2f, Vector3.one * .7f, ctx.Rng, 0f);
                    break;
                case 1: // a tapestry tent
                    for (var k = 0; k < 6; k++) { var a0 = k * 60f; var d0 = Quaternion.Euler(0, a0, 0) * Vector3.forward * 3f; var d1 = Quaternion.Euler(0, a0 + 60f, 0) * Vector3.forward * 3f; if (k == 0) continue; z.mk.Tri(WorldMaterials.Solid(Rainbow[(k + h) % Rainbow.Length], .4f), c + d0, c + d1, c + up * 4.2f, c + up * 1f); }
                    z.solid.Prism(dark, c, up, 2.6f, 2f, 6);
                    ArtistBits.PointLight(z.root, c + up * 2f, col, 8f, 1.5f, true);
                    break;
                case 2: // a mirror ball on a pole
                    z.mk.Prism(metal, c, up, .12f, 5f, 8); ArtistBits.PostCollider(z.root, c, 5f, .2f);
                    z.mk.Blob(WorldMaterials.Solid(new Color(.9f, .9f, .95f), .98f), c + up * 6f, Vector3.one * 1.2f, ctx.Rng, 0f);
                    halos.Add(c + up * 6f, 2.5f, col, .5f);
                    break;
                case 3: // an eye totem
                    for (var k = 0; k < 4; k++) { var p = c + up * (1f + k * 1.6f); z.mk.Box(WorldMaterials.Solid(Rainbow[(k + h) % Rainbow.Length], .5f), p, new Vector3(1.4f, 1.5f, 1.4f), Quaternion.LookRotation(z.front)); z.mk.Blob(white, p + z.front * .72f, new Vector3(.45f, .3f, .1f), ctx.Rng, 0f); z.mk.Blob(dark, p + z.front * .78f, Vector3.one * .14f, ctx.Rng, 0f); }
                    z.solid.Box(dark, c + up * 3.3f, new Vector3(1.4f, 6.6f, 1.4f), Quaternion.LookRotation(z.front));
                    break;
                default: // a sun dial with a sun on top
                    z.mk.Prism(WorldMaterials.Solid(new Color(.85f, .75f, .6f)), c, up, 2f, .6f, 16); z.solid.Prism(dark, c, up, 2f, .6f, 16);
                    z.mk.Box(metal, c + up * 1.4f, new Vector3(.1f, 1.6f, 1.2f), Quaternion.LookRotation(z.side) * Quaternion.Euler(30f, 0, 0));
                    z.mk.Blob(GlowM(new Color(1f, .75f, .2f), 2.4f), c + up * 4f, Vector3.one * .9f, ctx.Rng, 0f);
                    halos.Add(c + up * 4f, 2.5f, new Color(1f, .75f, .2f), .5f);
                    break;
            }
        }
    }
}
