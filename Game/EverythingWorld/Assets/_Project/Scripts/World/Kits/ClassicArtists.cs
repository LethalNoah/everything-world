using System.Collections.Generic;
using UnityEngine;

namespace EverythingWorld.World
{
    /// The Kingdom's legends, each evoked through things from its world (never a copied cover or logo), with a
    /// plaque by the road, covers on a board and a lamp:
    /// - The Beatles: a yellow submarine on the pond, a zebra crossing on their road, a field of strawberries
    ///   behind a gate, a blackbird on a post, a guitar on a stand.
    /// - Led Zeppelin: a silver airship moored to a mast, a stair of stones climbing into the sky to a glowing door
    ///   ("Stairway to Heaven"), a hermit with a lantern on a crag.
    /// - David Bowie: a giant lightning bolt and a rocket on its launch tower ("Space Oddity").
    /// - Jimi Hendrix: a wooden festival stage, amp stacks, a guitar on fire. The Rolling Stones: a stage with a
    ///   runway under spotlights. Elvis: a little white wedding chapel and a pink Cadillac. The Who: a mod target
    ///   and a pile of smashed guitars and a scooter. Cream: a burst of psychedelic petals round a white room.
    ///   Dire Straits: a giant steel resonator guitar and a stack of TVs. The Velvet Underground: a giant banana.
    ///   Queen: an opera stage under a crown with candelabras. The Beach Boys: a surf shack. The Doors: a lone
    ///   doorway in the field under a rain cloud. Everyone else: a guitar on a stand, a piano or a microphone.
    public partial class ClassicKit
    {
        class Spot
        {
            public LandContext ctx; public Transform root, labels; public MeshKit mk, solid, pictures;
            public IslandWorldData.Artist a; public Vector3 c, front, side; public List<Renderer> glows; public ArtistArea area;
            public Vector3 At(float fw, float rt) { var p = c + front * fw + side * rt; return ctx.OnGround(p.x, p.z); }
        }

        public override void BuildArtist(LandContext ctx, IslandWorldData.Artist a)
        {
            if (stoneM == null) MakeMaterials();
            var root = new GameObject("Artist: " + a.name).transform; root.SetParent(ctx.Root, false);
            var front = facing.TryGetValue(a.name, out var f) ? f : new Vector3(center.x - a.x, 0, center.z - a.z).normalized;
            if (front.sqrMagnitude < .01f) front = Vector3.forward;
            var labels = new GameObject("Labels").transform; labels.SetParent(root, false);
            var area = root.gameObject.AddComponent<ArtistArea>();
            area.artistName = a.name; area.stationId = a.stationId; area.radius = a.r; area.glowColor = new Color(1f, .85f, .55f);
            var z = new Spot { ctx = ctx, root = root, labels = labels, mk = new MeshKit(), solid = new MeshKit(), pictures = new MeshKit(), a = a, c = ctx.OnGround(a.x, a.z), front = front, side = Vector3.Cross(Vector3.up, front), glows = new List<Renderer>(), area = area };
            switch (a.name)
            {
                case "The Beatles": Beatles(z); break;
                case "Led Zeppelin": Zeppelin(z); break;
                case "David Bowie": Bowie(z); break;
                case "Jimi Hendrix": Hendrix(z); break;
                case "The Rolling Stones": Runway(z); break;
                case "Elvis Presley": Chapel(z); break;
                case "The Who": Mod(z); break;
                case "Cream": Psychedelic(z); break;
                case "Dire Straits": Resonator(z); break;
                case "The Velvet Underground": Banana(z); break;
                case "Queen": Opera(z); break;
                case "The Beach Boys": SurfShack(z); break;
                case "The Doors": Doorway(z); break;
                default: Small(z); break;
            }
            var reach = Foot(a);
            Board(z, z.At(reach * .8f, -reach * .5f - 2.5f), front);
            ArtistBits.Plaque(z.mk, z.solid, labels, a, z.At(reach * .8f + 1.5f, reach * .5f + 2.5f), front, wood, dark, Ink, 1.6f);
            z.glows.Add(Lamp(ctx, z.mk, root, z.At(reach * .8f, reach * .5f + 5f)));
            if (z.area.glowLight == null) z.area.glowLight = ArtistBits.PointLight(root, z.c + Vector3.up * 4f, new Color(1f, .9f, .7f), HasStation(a) ? 18f : 12f, 1f);
            area.glowRenderers = z.glows.ToArray();
            z.mk.Build("Area", root, false);
            z.solid.BuildCollider("Walls", root);
            z.pictures.Build("Covers", root, false);
            noGrass.Add(new Vector3(z.c.x, reach * .8f, z.c.z));
        }

        /// An electric guitar: a double-cut body, a long neck and a headstock, standing on (or leaning against)
        /// whatever 'at' is, facing 'face'. 'tilt' leans it back; 'broken' snaps the neck off and lays it beside.
        void Guitar(MeshKit mk, System.Random rng, Vector3 at, Vector3 face, Color paint, float scale = 1f, float tilt = 12f, bool broken = false)
        {
            var rot = Quaternion.LookRotation(face) * Quaternion.Euler(-tilt, 0, 0); var up = rot * Vector3.up; var side = rot * Vector3.right;
            var body = WorldMaterials.Solid(paint, .7f); var neck = WorldMaterials.Solid(new Color(.55f, .38f, .22f)); var s = scale;
            mk.Blob(body, at + up * .38f * s, new Vector3(.36f, .3f, .09f) * s, rng, 0f);
            mk.Blob(body, at + up * .66f * s + side * .02f * s, new Vector3(.27f, .2f, .085f) * s, rng, 0f);
            foreach (var h in new[] { -1f, 1f }) mk.Box(body, at + up * .86f * s + side * h * .13f * s, new Vector3(.08f, .2f, .17f) * s, rot);
            mk.Box(dark, at + up * .5f * s + face * .07f * s, new Vector3(.22f, .3f, .02f) * s, rot);          // pickguard
            if (broken)
            {
                var nd = Quaternion.AngleAxis(70f, face) * up;
                mk.Box(neck, at + up * .75f * s + side * .5f * s, new Vector3(.07f, .9f, .05f) * s, Quaternion.LookRotation(face, nd));
                return;
            }
            mk.Box(neck, at + up * 1.25f * s, new Vector3(.07f, .95f, .05f) * s, rot);
            mk.Box(dark, at + up * 1.82f * s, new Vector3(.11f, .22f, .04f) * s, rot);
            for (var k = 0; k < 3; k++) mk.Box(WorldMaterials.Solid(new Color(.85f, .86f, .88f), .9f), at + up * (1.75f + k * .07f) * s + side * .07f * s, new Vector3(.04f, .025f, .025f) * s, rot);
        }

        void Board(Spot z, Vector3 ground, Vector3 dir)
        {
            dir = Vector3.ProjectOnPlane(dir, Vector3.up).normalized;
            var rot = Quaternion.LookRotation(dir); var side = Vector3.Cross(Vector3.up, dir);
            var n = Mathf.Max(1, Mathf.Min(4, z.a.songList?.Length ?? 1)); const float size = .9f;
            var w = n * size * 1.25f + .4f;
            foreach (var s in new[] { -1f, 1f }) z.mk.Prism(wood, ground + side * s * (w / 2f - .1f) - dir * .05f, Vector3.up, .07f, 2.4f, 6);
            var c = ground + Vector3.up * 1.8f;
            z.mk.Box(dark, c - Vector3.up * .1f, new Vector3(w, size + .6f, .1f), rot);
            z.solid.Box(dark, c - Vector3.up * .1f, new Vector3(w, size + .6f, .2f), rot);
            ArtistBits.CoverWall(z.pictures, z.labels, z.ctx, z.a, c + dir * .055f + Vector3.up * .08f, dir, n, size, n, true, Ink);
        }

        // ---------------------------------------------------------------- The Beatles
        void Beatles(Spot z)
        {
            var ctx = z.ctx; var up = Vector3.up; var rot = Quaternion.LookRotation(z.front);
            // the zebra crossing on the road in
            for (var k = 0; k < 5; k++) z.mk.Box(white, z.At(Foot(z.a) * .85f + 4f, (k - 2f) * 1f) + up * .09f, new Vector3(.5f, .02f, 3f), rot);
            foreach (var s in new[] { -1f, 1f })
            {
                var post = z.At(Foot(z.a) * .85f + 4f, s * (RoadW / 2f + 1f));
                z.mk.Prism(WorldMaterials.Solid(new Color(.1f, .1f, .1f)), post, up, .06f, 2.4f, 6);
                z.mk.Blob(GlowM(new Color(1f, .75f, .2f), 2.4f), post + up * 2.55f, Vector3.one * .25f, ctx.Rng, 0f);
                halos.Add(post + up * 2.55f, 1f, new Color(1f, .75f, .3f), .8f);
            }
            // the yellow submarine, floating on the pond (or beached on the grass if there's no pond)
            var subAt = hasPond ? new Vector3(pondC.x, pondLevel - .2f, pondC.y) : z.At(-8f, 6f) + up * 1.2f;
            var sub = new GameObject("Yellow submarine").transform; sub.SetParent(z.root, false);
            var sk = new MeshKit(); var yellow = WorldMaterials.Solid(new Color(1f, .82f, .1f), .6f); var red = WorldMaterials.Solid(new Color(.85f, .2f, .15f));
            sk.Prism(yellow, new Vector3(0, 1.4f, -6f), Vector3.forward, 2f, 12f, 16);
            sk.Prism(yellow, new Vector3(0, 1.4f, 6f), Vector3.forward, 2f, 2.2f, 16, .25f);
            sk.Prism(yellow, new Vector3(0, 1.4f, -6f), Vector3.back, 2f, 1.6f, 16, .35f);
            sk.Box(yellow, new Vector3(0, 3.8f, 0), new Vector3(1.8f, 1.8f, 3.4f), Quaternion.identity);
            sk.Prism(red, new Vector3(0, 4.7f, .6f), Vector3.up, .15f, 2.2f, 8);
            sk.Box(red, new Vector3(0, 6.9f, 1f), new Vector3(.3f, .3f, .8f), Quaternion.identity);
            foreach (var s in new[] { -1f, 1f }) for (var k = 0; k < 4; k++) { sk.Prism(GlowM(new Color(.5f, .85f, 1f), 1.6f), new Vector3(s * 1.95f, 1.8f, -3.5f + k * 2.4f), Vector3.right * s, .4f, .1f, 12); sk.Prism(red, new Vector3(s * 1.93f, 1.8f, -3.5f + k * 2.4f), Vector3.right * s, .5f, .06f, 12); }
            for (var k = 0; k < 4; k++) sk.Box(red, new Vector3(0, 1.4f, -7.9f) + Quaternion.Euler(0, 0, k * 90f) * Vector3.up * .8f, new Vector3(.25f, 1.6f, .1f), Quaternion.Euler(0, 0, k * 90f));
            sk.BuildSingle("Submarine", sub, false);
            sub.position = subAt; sub.rotation = Quaternion.LookRotation(Quaternion.Euler(0, 40f, 0) * z.front);
            life.submarine = sub;
            z.solid.Box(yellow, subAt + up * 2.4f, new Vector3(4f, 5f, 16f), sub.rotation);
            // strawberry fields: rows behind a gate
            // Strawberry Fields: rows of strawberries in brown earth behind a red gate in a white fence, a sign over it
            var field = z.At(-6f, -16f); var leaf = WorldMaterials.Solid(new Color(.22f, .48f, .2f)); var berry = WorldMaterials.Solid(new Color(.95f, .12f, .18f), .8f);
            var soil = WorldMaterials.Solid(new Color(.42f, .3f, .2f)); var gateRed = WorldMaterials.Solid(new Color(.85f, .15f, .12f));
            for (var row = 0; row < 7; row++)
            {
                var rc = field + z.side * (row - 3f) * 1.8f;
                z.mk.Box(soil, ctx.OnGround(rc.x, rc.z) + up * .1f, new Vector3(1f, .3f, 14f), rot);
                for (var k = 0; k < 11; k++)
                {
                    var p = ctx.OnGround(rc.x + z.front.x * (k - 5f) * 1.25f, rc.z + z.front.z * (k - 5f) * 1.25f) + up * .25f;
                    soft.Blob(leaf, p + up * .2f, new Vector3(.5f, .3f, .5f), ctx.Rng, .3f);
                    soft.Blob(berry, p + up * .38f + z.side * .3f, new Vector3(.18f, .22f, .18f), ctx.Rng, .1f);
                    if (k % 2 == 0) soft.Blob(berry, p + up * .3f - z.side * .28f + z.front * .2f, new Vector3(.16f, .2f, .16f), ctx.Rng, .1f);
                }
            }
            noGrass.Add(new Vector3(field.x, 9f, field.z));
            var fenceA = field + z.front * 8f; var fenceRot = Quaternion.LookRotation(z.side);
            foreach (var s in new[] { -1f, 1f })
            {
                var seg = fenceA + z.side * s * 4.4f;
                for (var k = 0; k < 3; k++) z.mk.Box(white, seg + up * (.4f + k * .4f), new Vector3(.08f, .1f, 6.4f), fenceRot);
                for (var k = -3; k <= 3; k++) z.mk.Box(white, seg + z.side * k * 1f + up * .65f, new Vector3(.12f, 1.4f, .12f), fenceRot);
                z.solid.Box(white, seg + up * .7f, new Vector3(.2f, 1.4f, 6.4f), fenceRot);
            }
            foreach (var s in new[] { -1.3f, 1.3f }) { z.mk.Box(gateRed, fenceA + z.side * s + up * 1.1f, new Vector3(.2f, 2.2f, .2f), rot); }
            for (var k = 0; k < 4; k++) z.mk.Box(gateRed, fenceA + up * (.35f + k * .38f) + z.side * -.4f + z.front * .6f, new Vector3(.08f, .1f, 2.4f), Quaternion.LookRotation(Quaternion.Euler(0, -50f, 0) * z.side));
            z.mk.Box(gateRed, fenceA + up * 2.6f, new Vector3(3.4f, .7f, .1f), rot);
            ArtistBits.Label(z.labels, "STRAWBERRY FIELDS", fenceA + up * 2.6f + z.front * .07f, z.front, .26f, new Color(1f, .95f, .85f), TextAnchor.MiddleCenter);
            // a blackbird on a post
            var bp = z.At(4f, -7f); z.mk.Prism(wood, bp, up, .08f, 1.6f, 6);
            var bb = WorldMaterials.Solid(new Color(.06f, .06f, .07f), .6f);
            z.mk.Blob(bb, bp + up * 1.75f, new Vector3(.12f, .12f, .2f), ctx.Rng, 0f); z.mk.Blob(bb, bp + up * 1.88f + z.front * .15f, Vector3.one * .07f, ctx.Rng, 0f);
            z.mk.Box(WorldMaterials.Solid(new Color(1f, .7f, .1f)), bp + up * 1.88f + z.front * .26f, new Vector3(.03f, .03f, .1f), rot);
            // a guitar on a stand on a little rug, a sitar beside it
            var rug = z.At(1f, 3f); z.mk.Box(WorldMaterials.Solid(new Color(.6f, .25f, .35f)), rug + up * .03f, new Vector3(3f, .02f, 2f), rot);
            Guitar(z.mk, ctx.Rng, rug + up * .05f - z.side * .5f, z.front, new Color(.7f, .3f, .15f), 1.6f);
            Guitar(z.mk, ctx.Rng, rug + up * .05f + z.side * .5f, z.front, new Color(.15f, .15f, .17f), 1.6f, 14f);
            // a sitar lying on the rug: a round gourd and a long neck with a second gourd
            var gourd = WorldMaterials.Solid(new Color(.65f, .42f, .2f), .6f);
            z.mk.Blob(gourd, rug + up * .35f + z.front * .6f - z.side * 1.3f, new Vector3(.45f, .35f, .45f), ctx.Rng, 0f);
            z.mk.Box(gourd, rug + up * .35f + z.front * .6f + z.side * .1f, new Vector3(2.4f, .1f, .14f), Quaternion.LookRotation(z.front));
            z.mk.Blob(gourd, rug + up * .45f + z.front * .6f + z.side * 1.2f, Vector3.one * .18f, ctx.Rng, 0f);
            z.solid.Box(dark, rug + up * .7f, new Vector3(2.4f, 1.4f, 1f), rot);
            z.area.glowLight = ArtistBits.PointLight(z.root, rug + up * 3f, new Color(1f, .85f, .5f), 16f, 1.2f);
        }

        // ---------------------------------------------------------------- Led Zeppelin
        void Zeppelin(Spot z)
        {
            var ctx = z.ctx; var up = Vector3.up;
            // the mooring mast and the airship riding at it
            var mast = z.At(-6f, 8f);
            for (var k = 0; k < 4; k++) z.mk.Box(metal, mast + Quaternion.Euler(0, k * 90f + 45f, 0) * Vector3.forward * .9f + up * 9f, new Vector3(.2f, 18f, .2f), Quaternion.identity);
            for (var y = 2f; y < 18f; y += 2.5f) z.mk.Box(metal, mast + up * y, new Vector3(1.6f, .12f, 1.6f), Quaternion.identity);
            z.solid.Box(metal, mast + up * 9f, new Vector3(1.8f, 18f, 1.8f), Quaternion.identity);
            var ship = new GameObject("Airship").transform; ship.SetParent(z.root, false);
            var ak = new MeshKit(); var silver = WorldMaterials.Solid(new Color(.82f, .84f, .86f), .6f);
            const int n = 14; const float L = 40f, R = 5.5f;
            for (var i = 0; i < n; i++)
            {
                float t0 = i / (float)n, t1 = (i + 1) / (float)n;
                float R0 = R * Mathf.Sin(Mathf.Lerp(.08f, 1f, Mathf.Sqrt(t0)) * Mathf.PI * .5f + (t0 > .7f ? 0f : 0f)) * (1f - Mathf.Pow(Mathf.Max(0, t0 - .55f) / .45f, 2f) * .85f);
                float R1 = R * Mathf.Sin(Mathf.Lerp(.08f, 1f, Mathf.Sqrt(t1)) * Mathf.PI * .5f) * (1f - Mathf.Pow(Mathf.Max(0, t1 - .55f) / .45f, 2f) * .85f);
                const int sides = 16;
                for (var k = 0; k < sides; k++)
                {
                    float a0 = k * Mathf.PI * 2f / sides, a1 = (k + 1) * Mathf.PI * 2f / sides;
                    Vector3 P(float t, float r, float a) => new Vector3(Mathf.Cos(a) * r, Mathf.Sin(a) * r, t * L - L * .1f);
                    ak.Quad(silver, P(t0, R0, a0), P(t0, R0, a1), P(t1, R1, a1), P(t1, R1, a0), new Vector3(0, 0, (t0 + t1) * .5f * L - L * .1f));
                }
            }
            foreach (var d in new[] { Vector3.up, Vector3.down, Vector3.left, Vector3.right }) ak.Tri(silver, new Vector3(0, 0, L * .8f) + d * 1.5f, new Vector3(0, 0, L * .92f) + d * 1f, new Vector3(0, 0, L * .9f) + d * 5f, Vector3.Cross(d, Vector3.forward));
            foreach (var d in new[] { Vector3.up, Vector3.down, Vector3.left, Vector3.right }) ak.Tri(silver, new Vector3(0, 0, L * .8f) + d * 1.5f, new Vector3(0, 0, L * .92f) + d * 1f, new Vector3(0, 0, L * .9f) + d * 5f, -Vector3.Cross(d, Vector3.forward));
            ak.Box(dark, new Vector3(0, -R - .6f, L * .3f), new Vector3(2f, 1.4f, 7f), Quaternion.identity);
            for (var k = 0; k < 5; k++) ak.Box(GlowM(new Color(1f, .9f, .6f), 1.6f), new Vector3(1.01f, -R - .5f, L * .3f - 2.6f + k * 1.3f), new Vector3(.02f, .5f, .7f), Quaternion.identity);
            ak.BuildSingle("Hull", ship, false);
            ship.position = mast + up * 17f; ship.rotation = Quaternion.LookRotation(-z.side);
            life.airship = ship;
            // the stair to heaven: stones spiralling up from the ground into the sky to a glowing doorway
            // a spiral of stones you can climb (each step low enough to walk up) round a stone column, up to a
            // landing in the clouds with the glowing doorway and a bench looking out over the Kingdom
            var foot = z.At(4f, -12f);
            z.mk.Prism(stoneM, foot - up * .5f, up, 1.2f, 18f, 10, .7f);
            z.solid.Prism(stoneM, foot - up * .5f, up, 1.2f, 18f, 10);
            var steps = new List<Material>(); const int N = 60; const float Rise = .28f, Turn = 15f;
            for (var k = 0; k < N; k++)
            {
                var a = k * Turn; var d = Quaternion.Euler(0, a, 0) * z.front;
                var p = foot + d * 3.1f + up * (k * Rise + .14f);
                var m = new Material(GlowM(new Color(1f, .95f, .8f), .02f)) { name = "Stair stone" };
                m.SetColor("_BaseColor", new Color(.85f, .83f, .78f));
                steps.Add(m);
                var r = Quaternion.LookRotation(Vector3.Cross(up, d));
                z.mk.Box(m, p, new Vector3(1.3f, .28f, 3.6f), r * Quaternion.Euler(0, 90f, 0));
                z.solid.Box(m, p - up * .1f, new Vector3(1.4f, .5f, 3.6f), r * Quaternion.Euler(0, 90f, 0));
            }
            life.stairSteps = steps.ToArray();
            // the landing sits just past the last step, out from the column
            var topD = Quaternion.Euler(0, N * Turn, 0) * z.front; var landing = foot + topD * 7.4f + up * (N * Rise - .25f);
            var cloudM = WorldMaterials.Solid(new Color(.97f, .97f, 1f));
            z.mk.Prism(cloudM, landing - up * .4f, up, 4.2f, .5f, 16);
            z.solid.Prism(cloudM, landing - up * .4f, up, 4.2f, .5f, 16);
            for (var k = 0; k < 9; k++) { var d = Quaternion.Euler(0, k * 40f, 0) * Vector3.forward; if (Vector3.Dot(d, -topD) > .8f) continue; soft.Blob(cloudM, landing + d * 4.3f - up * .3f, new Vector3(1.6f, .8f, 1.6f), ctx.Rng, .3f); }
            var benchAt = landing + Vector3.Cross(up, topD) * 2.4f;
            z.mk.Box(wood, benchAt + up * .45f, new Vector3(1.8f, .08f, .5f), Quaternion.LookRotation(Vector3.Cross(up, topD)));
            z.solid.Box(wood, benchAt + up * .4f, new Vector3(1.9f, .8f, .6f), Quaternion.LookRotation(Vector3.Cross(up, topD)));
            var door = landing + topD * 3f + up * .1f;
            foreach (var s in new[] { -1f, 1f }) z.mk.Box(stoneM, door + Vector3.Cross(up, topD) * s * 1f + up * 1.5f, new Vector3(.3f, 3f, .3f), Quaternion.LookRotation(topD));
            z.mk.Box(stoneM, door + up * 3.1f, new Vector3(2.4f, .3f, .3f), Quaternion.LookRotation(topD));
            var doorGlow = new Material(GlowM(new Color(1f, .95f, .8f), 2f)) { name = "Heaven's door" };
            z.mk.Box(doorGlow, door + up * 1.5f, new Vector3(1.7f, 2.9f, .05f), Quaternion.LookRotation(topD));
            life.heavenDoor = doorGlow; life.stairTop = door + up * 1.5f;
            halos.Add(door + up * 1.5f + topD * 1.5f, 3f, new Color(1f, .95f, .8f), .8f);
            // the hermit with his lantern on a crag
            var crag = z.At(6f, -14f);
            z.mk.Blob(stoneM, crag + up * 1.6f, new Vector3(3f, 2.4f, 3f), ctx.Rng, .3f);
            z.solid.Box(stoneM, crag + up * 1.6f, new Vector3(4.5f, 3.6f, 4.5f), Quaternion.identity);
            var cloak = WorldMaterials.Solid(new Color(.35f, .3f, .3f));
            var hp = crag + up * 3.8f;
            z.mk.Prism(cloak, hp, up, .5f, 1.7f, 8, .3f);
            z.mk.Blob(cloak, hp + up * 1.8f, Vector3.one * .25f, ctx.Rng, 0f);
            z.mk.Prism(wood, hp + z.side * .7f - up * .1f, (up + z.side * .1f).normalized, .04f, 2.2f, 5);
            var lantern = hp + up * 1.4f + z.front * .6f;
            z.mk.Box(GlowM(new Color(1f, .85f, .5f), 3f), lantern, new Vector3(.25f, .35f, .25f), Quaternion.identity);
            halos.Add(lantern, 1.8f, new Color(1f, .85f, .5f), 1f);
            ArtistBits.PointLight(z.root, lantern, new Color(1f, .85f, .5f), 10f, 1.4f);
        }

        // ---------------------------------------------------------------- David Bowie
        void Bowie(Spot z)
        {
            var ctx = z.ctx; var up = Vector3.up;
            // a giant lightning bolt on a plinth, red with a blue edge
            var bp = z.At(-2f, -4f); var rot = Quaternion.LookRotation(z.front);
            z.mk.Box(white, bp + up * .5f, new Vector3(3f, 1f, 3f), rot); z.solid.Box(white, bp + up * 3f, new Vector3(3f, 6f, 1.6f), rot);
            Vector3[] bolt = { new Vector3(.6f, 7.5f, 0), new Vector3(-1.2f, 4.2f, 0), new Vector3(.3f, 4.4f, 0), new Vector3(-1f, 1f, 0) };
            var redB = WorldMaterials.Solid(new Color(.9f, .15f, .2f)); var blueB = WorldMaterials.Solid(new Color(.2f, .55f, .95f));
            for (var k = 0; k < 3; k++)
            {
                var a = bp + rot * bolt[k]; var b = bp + rot * bolt[k + 1]; var d = b - a;
                z.mk.Box(redB, (a + b) / 2f, new Vector3(1.1f, d.magnitude + .6f, .5f), Quaternion.LookRotation(z.front, d.normalized));
                z.mk.Box(blueB, (a + b) / 2f + z.side * .45f, new Vector3(.3f, d.magnitude + .6f, .55f), Quaternion.LookRotation(z.front, d.normalized));
            }
            // the rocket on its launch tower
            var pad = z.At(-4f, 9f);
            z.mk.Box(stoneM, pad + up * .2f, new Vector3(6f, .4f, 6f), Quaternion.identity);
            z.solid.Box(stoneM, pad + up * .2f, new Vector3(6f, .4f, 6f), Quaternion.identity);
            for (var y = 0f; y < 16f; y += 2f) { z.mk.Box(WorldMaterials.Solid(new Color(.85f, .3f, .2f)), pad + z.side * 2.4f + up * (y + 1.01f), new Vector3(.8f, 2f, .8f), Quaternion.identity); }
            z.solid.Box(metal, pad + z.side * 2.4f + up * 8f, new Vector3(.9f, 16f, .9f), Quaternion.identity);
            var rocket = new GameObject("Rocket").transform; rocket.SetParent(z.root, false);
            var rk = new MeshKit();
            rk.Prism(white, Vector3.zero + Vector3.up * 1f, Vector3.up, 1f, 9f, 16);
            rk.Cone(white, Vector3.up * 10f, 1f, 3f, 16);
            foreach (var d in new[] { Vector3.forward, Vector3.back, Vector3.left, Vector3.right }) { rk.Tri(redB, d * 1f + Vector3.up * 1f, d * 1f + Vector3.up * 4f, d * 2.4f, Vector3.Cross(d, Vector3.up)); rk.Tri(redB, d * 1f + Vector3.up * 1f, d * 1f + Vector3.up * 4f, d * 2.4f, -Vector3.Cross(d, Vector3.up)); }
            rk.Prism(GlowM(new Color(.5f, .8f, 1f), 1.6f), Vector3.up * 7.5f + Vector3.forward * .98f, Vector3.forward, .35f, .05f, 12);
            rk.BuildSingle("Rocket", rocket, false);
            rocket.position = pad + up * .4f;
            life.rocket = rocket; life.rocketPad = pad + up * .4f;
            z.area.glowLight = ArtistBits.PointLight(z.root, bp + up * 4f + z.front * 3f, new Color(.6f, .8f, 1f), 16f, 1.4f);
        }

        // ---------------------------------------------------------------- Jimi Hendrix
        void Hendrix(Spot z)
        {
            var ctx = z.ctx; var up = Vector3.up; var rot = Quaternion.LookRotation(z.front); var c = z.At(-4f, 0);
            z.mk.Box(wood, c + up * 1f, new Vector3(12f, 2f, 7f), rot); z.solid.Box(wood, c + up * 1f, new Vector3(12f, 2f, 7f), rot);
            foreach (var s in new[] { -1f, 1f }) foreach (var d in new[] { -1f, 1f }) { var p = c + z.side * s * 6f + z.front * d * 3.4f; z.mk.Box(metal, p + up * 4.5f, new Vector3(.25f, 9f, .25f), rot); ArtistBits.PostCollider(z.root, p, 9f, .2f); }
            z.mk.Box(metal, c + up * 9f, new Vector3(12.5f, .3f, .3f), rot);
            z.mk.Box(WorldMaterials.Solid(new Color(.7f, .6f, .45f)), c + up * 9.4f, new Vector3(12.5f, .6f, 7.4f), rot);
            foreach (var s in new[] { -1f, 1f }) for (var k = 0; k < 3; k++) { var amp = c + z.side * s * 4f - z.front * 2f + up * (2.6f + k * 1.2f); z.mk.Box(dark, amp, new Vector3(1.6f, 1.1f, .8f), rot); z.mk.Box(WorldMaterials.Solid(new Color(.75f, .7f, .6f)), amp + z.front * .41f, new Vector3(1.4f, .9f, .02f), rot); }
            // a guitar on fire at the front of the stage
            var g = c + up * 2f + z.front * 2.2f;
            Guitar(z.mk, ctx.Rng, g, z.front, new Color(.95f, .95f, .92f), 1.4f, 20f);
            life.guitarFire = g + up * .5f;
            var fl = ArtistBits.PointLight(z.root, g + up * 1.2f, new Color(1f, .55f, .2f), 10f, 2f); fl.gameObject.AddComponent<FireFlicker>().baseIntensity = 2f;
            halos.Add(g + up * 1f + z.front, 2f, new Color(1f, .55f, .2f), 1f);
            for (var k = 0; k < 6; k++) z.mk.Prism(GlowM(k % 2 == 0 ? new Color(.7f, .4f, 1f) : new Color(1f, .8f, .3f), 2.4f), c + up * 8.8f + z.side * ((k - 2.5f) * 2f), (z.front - up).normalized, .25f, .3f, 8);
            z.area.glowLight = ArtistBits.PointLight(z.root, c + up * 7f + z.front * 4f, new Color(.8f, .6f, 1f), 18f, 1.4f);
        }

        void Runway(Spot z)
        {
            var ctx = z.ctx;
            var up = Vector3.up; var rot = Quaternion.LookRotation(z.front); var c = z.At(-4f, 0);
            z.mk.Box(dark, c + up * .8f, new Vector3(10f, 1.6f, 6f), rot); z.solid.Box(dark, c + up * .8f, new Vector3(10f, 1.6f, 6f), rot);
            z.mk.Box(dark, c + up * .8f + z.front * 6f, new Vector3(2.4f, 1.6f, 6f), rot); z.solid.Box(dark, c + up * .8f + z.front * 6f, new Vector3(2.4f, 1.6f, 6f), rot);
            for (var k = 0; k < 6; k++) z.mk.Box(GlowM(new Color(1f, .3f, .3f), 2.4f), c + up * 1.62f + z.front * (3.5f + k * .9f) + z.side * (k % 2 == 0 ? 1.1f : -1.1f), new Vector3(.2f, .04f, .4f), rot);
            foreach (var s in new[] { -1f, 1f }) { var t = c + z.side * s * 7f; z.mk.Box(metal, t + up * 4f, new Vector3(.3f, 8f, .3f), rot); ArtistBits.PostCollider(z.root, t, 8f, .2f); z.mk.Prism(GlowM(new Color(1f, .95f, .8f), 2.4f), t + up * 8f, (z.front * 2f - up - z.side * s).normalized, .35f, .5f, 10); }
            // giant red lips with the tongue out, standing on the back of the stage
            var lipRed = WorldMaterials.Solid(new Color(.88f, .08f, .1f), .75f); var mouth = WorldMaterials.Solid(new Color(.12f, .02f, .03f)); var tongue = WorldMaterials.Solid(new Color(.95f, .3f, .35f), .7f);
            var lc = c + up * 6.2f - z.front * 2f; var lrot = Quaternion.LookRotation(z.front);
            Vector3 L(float x, float y) => lc + z.side * x + up * y;
            z.mk.Blob(mouth, L(0f, 0f), new Vector3(3f, 1.1f, .6f), ctx.Rng, 0f);
            for (var k = 0; k < 9; k++)                                  // upper lip: two humps with a dip in the middle
            {
                var x = (k - 4f) * .75f; var y = 1.05f + Mathf.Cos(x * 1.1f) * .35f * (1f - Mathf.Abs(x) / 4f) - (Mathf.Abs(x) < .5f ? .3f : 0f) - Mathf.Pow(Mathf.Abs(x) / 3.2f, 2f) * .9f;
                z.mk.Blob(lipRed, L(x, y), new Vector3(.62f, .55f - Mathf.Abs(x) * .07f, .75f), ctx.Rng, 0f);
            }
            for (var k = 0; k < 9; k++)                                  // fuller lower lip
            {
                var x = (k - 4f) * .72f; var y = -1.1f + Mathf.Pow(Mathf.Abs(x) / 3f, 2f) * 1.1f;
                z.mk.Blob(lipRed, L(x, y), new Vector3(.62f, .75f - Mathf.Abs(x) * .1f, .8f), ctx.Rng, 0f);
            }
            for (var k = 0; k < 5; k++)                                  // the tongue, lolling out and down
            {
                var t = k / 4f;
                z.mk.Blob(tongue, L(.3f + t * .4f, -.6f - t * 1.9f) + z.front * (.6f + t * .5f), new Vector3(1.1f - t * .15f, .55f, .35f), ctx.Rng, 0f);
            }
            z.mk.Box(tongue, L(.5f, -1.8f) + z.front * 1.12f, new Vector3(.07f, 1.4f, .02f), lrot);
            z.mk.Box(dark, c + up * 3.2f - z.front * 2f, new Vector3(.4f, 3.2f, .4f), lrot);
            z.solid.Box(lipRed, lc, new Vector3(6.4f, 4f, 1.4f), lrot);
            halos.Add(lc + z.front * 2.5f, 3.5f, new Color(1f, .3f, .3f), .5f);
            z.area.glowLight = ArtistBits.PointLight(z.root, c + up * 6f + z.front * 6f, new Color(1f, .9f, .8f), 16f, 1.6f);
        }

        void Chapel(Spot z)
        {
            var ctx = z.ctx; var up = Vector3.up; var rot = Quaternion.LookRotation(z.front); var c = z.At(-4f, 0);
            const float W = 6f, D = 8f, H = 4f;
            z.mk.Box(white, c + up * H / 2f - z.front * (D / 2f - .15f), new Vector3(W, H, .3f), rot);
            foreach (var s in new[] { -1f, 1f }) z.mk.Box(white, c + up * H / 2f + z.side * s * (W / 2f - .15f), new Vector3(.3f, H, D - .6f), rot);
            foreach (var s in new[] { -1f, 1f }) z.mk.Box(white, c + up * H / 2f + z.front * (D / 2f - .15f) + z.side * s * (W / 4f + .5f), new Vector3(W / 2f - 1f, H, .3f), rot);
            z.mk.Box(white, c + up * (H - .4f) + z.front * (D / 2f - .15f), new Vector3(2f, .8f, .3f), rot);
            z.solid.Box(white, c + up * H / 2f - z.front * (D / 2f - .15f), new Vector3(W, H, .3f), rot);
            foreach (var s in new[] { -1f, 1f }) z.solid.Box(white, c + up * H / 2f + z.side * s * (W / 2f - .15f), new Vector3(.3f, H, D), rot);
            z.mk.GableRoof(WorldMaterials.Solid(new Color(.55f, .55f, .6f)), c + up * H, new Vector3(D + .6f, 0, W + .6f), 2.4f, Quaternion.LookRotation(z.side));
            z.mk.Box(white, c + up * (H + 3.2f) + z.front * (D / 2f - 1f), new Vector3(1.2f, 2.4f, 1.2f), rot);
            z.mk.Cone(white, c + up * (H + 4.4f) + z.front * (D / 2f - 1f), .9f, 2.2f, 4, 45f);
            ArtistBits.Label(z.labels, "WEDDING CHAPEL", c + up * (H + .6f) + z.front * (D / 2f + .05f), z.front, .3f, new Color(1f, .45f, .7f), TextAnchor.MiddleCenter);
            halos.Add(c + up * (H + .6f) + z.front * (D / 2f + 1.2f), 1.8f, new Color(1f, .45f, .7f), .7f);
            ArtistBits.PointLight(z.root, c + up * 3f, new Color(1f, .85f, .7f), 10f, 1.4f, true);
            // the pink Cadillac out front
            VintageCar(ctx, z.mk, z.solid, z.At(4f, 4.5f), new Color(.95f, .55f, .7f));
        }

        void Mod(Spot z)
        {
            var ctx = z.ctx; var up = Vector3.up; var c = z.At(-5f, 0);
            Color[] rings = { new Color(.2f, .3f, .7f), new Color(.95f, .95f, .95f), new Color(.85f, .15f, .15f) };
            for (var k = 0; k < 3; k++) z.mk.Prism(WorldMaterials.Solid(rings[k]), c + up * 5f + z.front * (k * .06f), z.front, 4f - k * 1.3f, .1f, 32);
            foreach (var s in new[] { -1f, 1f }) { z.mk.Prism(metal, c + z.side * s * 2f - up * .3f, up, .12f, 3.5f, 6); ArtistBits.PostCollider(z.root, c + z.side * s * 2f, 3f, .15f); }
            // smashed guitars in a heap
            // a wall of amp stacks behind the target, one toppled forward, guitars smashed on the stage in front
            var stage = z.At(1f, -2f); var srot = Quaternion.LookRotation(z.front);
            z.mk.Box(dark, stage + up * .3f, new Vector3(9f, .6f, 6f), srot); z.solid.Box(dark, stage + up * .3f, new Vector3(9f, .6f, 6f), srot);
            var grill = WorldMaterials.Solid(new Color(.3f, .28f, .26f)); var gold = WorldMaterials.Solid(new Color(.85f, .75f, .45f), .8f);
            for (var k = 0; k < 4; k++)
            {
                if (k == 2) continue;
                var col = stage - z.front * 2.2f + z.side * ((k - 1.5f) * 2f);
                for (var h = 0; h < 3; h++) { var cab = col + up * (.6f + .7f + h * 1.4f); z.mk.Box(dark, cab, new Vector3(1.8f, 1.35f, .8f), srot); z.mk.Box(grill, cab + z.front * .41f, new Vector3(1.6f, 1.15f, .02f), srot); z.mk.Box(gold, cab + z.front * .42f + up * .5f, new Vector3(.4f, .1f, .02f), srot); }
                z.solid.Box(dark, col + up * (.6f + 2.1f), new Vector3(1.8f, 4.2f, .8f), srot);
            }
            var fallen = stage - z.front * .6f + z.side * 1f + up * 1f;
            z.mk.Box(dark, fallen, new Vector3(1.8f, .8f, 1.35f), srot * Quaternion.Euler(10f, 0, 0)); z.mk.Box(grill, fallen + up * .41f, new Vector3(1.6f, .02f, 1.15f), srot * Quaternion.Euler(10f, 0, 0));
            z.solid.Box(dark, fallen, new Vector3(1.8f, .9f, 1.4f), srot);
            for (var k = 0; k < 4; k++)
            {
                var gp = stage + up * .62f + z.front * (1.2f + (k % 2) * .8f) + z.side * ((k - 1.5f) * 1.6f);
                Guitar(z.mk, ctx.Rng, gp, Quaternion.Euler(0, 40f + k * 70f, 0) * z.front, Paint[(k + 1) % Paint.Length], 1.1f, 86f, true);
            }
            // a scooter with too many mirrors
            var sc = z.At(2f, 5f); var scrot = Quaternion.LookRotation(z.side);
            z.mk.Box(WorldMaterials.Solid(new Color(.65f, .8f, .7f), .7f), sc + up * .55f, new Vector3(.5f, .5f, 1.6f), scrot);
            foreach (var d in new[] { -.65f, .65f }) z.mk.Prism(dark, sc + z.side * d + up * .25f - z.front * .1f, z.front, .25f, .2f, 10);
            z.mk.Box(metal, sc + up * 1.1f + z.side * .7f, new Vector3(.08f, .7f, .08f), scrot);
            for (var k = 0; k < 6; k++) z.mk.Prism(metal, sc + up * (1.4f + k * .08f) + z.side * .7f + z.front * ((k - 2.5f) * .2f), up, .08f, .02f, 8);
            z.solid.Box(metal, sc + up * .6f, new Vector3(.6f, 1.2f, 1.8f), scrot);
            z.area.glowLight = ArtistBits.PointLight(z.root, c + up * 5f + z.front * 3f, new Color(.9f, .9f, 1f), 14f, 1.2f);
        }

        void Psychedelic(Spot z)
        {
            var ctx = z.ctx; var up = Vector3.up; var c = z.At(-3f, 0); var rot = Quaternion.LookRotation(z.front);
            // a white room, open at the front
            z.mk.Box(white, c + up * 1.5f - z.front * 1.5f, new Vector3(4f, 3f, .2f), rot);
            foreach (var s in new[] { -1f, 1f }) z.mk.Box(white, c + up * 1.5f + z.side * s * 1.9f, new Vector3(.2f, 3f, 2.8f), rot);
            z.mk.Box(white, c + up * 3.05f, new Vector3(4f, .2f, 3f), rot);
            z.solid.Box(white, c + up * 1.5f - z.front * 1.5f, new Vector3(4f, 3f, .2f), rot);
            foreach (var s in new[] { -1f, 1f }) z.solid.Box(white, c + up * 1.5f + z.side * s * 1.9f, new Vector3(.2f, 3f, 2.8f), rot);
            // round it, a burst of giant psychedelic petals in day-glo colours
            Color[] dayglo = { new Color(1f, .4f, .7f), new Color(1f, .8f, .1f), new Color(.3f, .85f, 1f), new Color(.6f, 1f, .3f), new Color(1f, .5f, .15f), new Color(.7f, .4f, 1f) };
            for (var k = 0; k < 14; k++)
            {
                var a = k * Mathf.PI * 2f / 14f; var d = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                if (Vector3.Dot(d, z.front) > .7f) continue;
                var p = c + d * 5f + up * .1f; var len = ctx.Range(3f, 5f);
                var dir = (d * .6f + up).normalized;
                z.mk.Prism(WorldMaterials.Solid(dayglo[k % dayglo.Length], .6f), p, dir, .5f, len, 6, 1.6f);
                z.mk.Blob(WorldMaterials.Solid(dayglo[(k + 2) % dayglo.Length], .6f), p + dir * len, Vector3.one * .7f, ctx.Rng, .2f);
                ArtistBits.PostCollider(z.root, p, 2f, .5f);
            }
        }

        void Resonator(Spot z)
        {
            var ctx = z.ctx; var up = Vector3.up; var c = z.At(-3f, 0); var rot = Quaternion.LookRotation(z.front);
            var steel = WorldMaterials.Solid(new Color(.85f, .87f, .9f), .95f);
            z.mk.Blob(steel, c + up * 3f, new Vector3(2.2f, 2.4f, .5f), ctx.Rng, 0f);
            z.mk.Blob(steel, c + up * 5.6f, new Vector3(1.8f, 1.7f, .48f), ctx.Rng, 0f);
            z.mk.Box(WorldMaterials.Solid(new Color(.25f, .18f, .12f)), c + up * 9.5f, new Vector3(.5f, 5f, .3f), rot);
            z.mk.Box(dark, c + up * 12.5f, new Vector3(.8f, 1.4f, .3f), rot);
            foreach (var y in new[] { 2.4f, 4.6f }) z.mk.Prism(WorldMaterials.Solid(new Color(.7f, .72f, .75f), .9f), c + up * y + z.front * .45f, z.front, .9f, .06f, 20);
            for (var k = 0; k < 6; k++) z.mk.Box(WorldMaterials.Glow(new Color(.95f, .95f, 1f), .8f), c + up * 7f + z.side * ((k - 2.5f) * .06f) + z.front * .5f, new Vector3(.015f, 11f, .015f), rot);
            z.solid.Box(steel, c + up * 4f, new Vector3(4.4f, 8f, 1f), rot);
            // a stack of TVs
            var tv = z.At(1f, 6f);
            for (var k = 0; k < 6; k++) { var p = tv + up * (.5f + (k / 2) * .9f) + z.side * ((k % 2) * 1.1f - .55f); z.mk.Box(dark, p, new Vector3(1f, .85f, .8f), rot); z.mk.Box(GlowM(new Color(.6f, .8f, 1f), 1.6f), p + z.front * .41f, new Vector3(.8f, .6f, .02f), rot); }
            z.solid.Box(dark, tv + up * 1.4f, new Vector3(2.2f, 2.8f, .8f), rot);
            z.area.glowLight = ArtistBits.PointLight(z.root, c + up * 5f + z.front * 4f, new Color(.9f, .95f, 1f), 16f, 1.2f);
        }

        void Banana(Spot z)
        {
            var ctx = z.ctx; var up = Vector3.up; var c = z.At(-3f, 0);
            z.mk.Box(white, c + up * 1.6f, new Vector3(8f, 3.2f, 3.4f), Quaternion.LookRotation(z.front));
            z.solid.Box(white, c + up * 1.6f, new Vector3(8f, 3.2f, 3.4f), Quaternion.LookRotation(z.front));
            var yel = WorldMaterials.Solid(new Color(1f, .88f, .2f), .5f);
            Vector3 B(float t) => c + up * (4.05f + (1f - Mathf.Sin(t * Mathf.PI)) * .5f) + z.side * Mathf.Lerp(-3.6f, 3.6f, t) + z.front * (Mathf.Sin(t * Mathf.PI) * .9f - .45f);
            for (var k = 0; k < 12; k++)
            {
                float t0 = k / 12f, t1 = (k + 1) / 12f; var r = 1.1f * Mathf.Sin(Mathf.Lerp(.15f, .85f, (t0 + t1) / 2f) * Mathf.PI);
                z.mk.Prism(yel, B(t0), (B(t1) - B(t0)).normalized, r, Vector3.Distance(B(t0), B(t1)) + .05f, 10);
            }
            z.mk.Prism(WorldMaterials.Solid(new Color(.3f, .25f, .1f)), B(0f) - (B(.05f) - B(0f)).normalized * .5f, (B(0f) - B(.05f)).normalized, .2f, .6f, 8);
        }

        void Opera(Spot z)
        {
            var ctx = z.ctx; var up = Vector3.up; var rot = Quaternion.LookRotation(z.front); var c = z.At(-4f, 0);
            var velvet = WorldMaterials.Solid(new Color(.55f, .1f, .15f)); var gilt = WorldMaterials.Solid(new Color(.9f, .75f, .3f), .8f);
            z.mk.Box(dark, c + up * .9f, new Vector3(10f, 1.8f, 6f), rot); z.solid.Box(dark, c + up * .9f, new Vector3(10f, 1.8f, 6f), rot);
            z.mk.Box(velvet, c + up * 5f - z.front * 2.8f, new Vector3(10f, 6.5f, .2f), rot);
            foreach (var s in new[] { -1f, 1f }) { z.mk.Box(gilt, c + up * 4.5f + z.side * s * 5f, new Vector3(.6f, 9f, .6f), rot); ArtistBits.PostCollider(z.root, c + z.side * s * 5f, 9f, .4f); }
            z.mk.Box(gilt, c + up * 9f, new Vector3(11f, .8f, .8f), rot);
            // the crown on top
            var crown = c + up * 10f;
            z.mk.Prism(gilt, crown, up, 1.6f, .8f, 12);
            for (var k = 0; k < 6; k++) { var d = Quaternion.Euler(0, k * 60f, 0) * Vector3.forward; z.mk.Cone(gilt, crown + up * .8f + d * 1.5f, .35f, 1.2f, 6); z.mk.Blob(WorldMaterials.Solid(k % 2 == 0 ? new Color(.85f, .15f, .2f) : new Color(.2f, .4f, .9f), .9f), crown + up * .4f + d * 1.62f, Vector3.one * .18f, ctx.Rng, 0f); }
            // candelabras either side, lit
            foreach (var s in new[] { -1f, 1f })
            {
                var cb = c + up * 1.8f + z.side * s * 3.5f + z.front * 1.5f;
                z.mk.Prism(gilt, cb, up, .06f, 1.6f, 6);
                for (var k = -1; k <= 1; k++) { var t = cb + up * 1.6f + z.side * k * .4f; z.mk.Prism(white, t, up, .04f, .25f, 6); z.mk.Blob(GlowM(new Color(1f, .75f, .4f), 2.6f), t + up * .32f, new Vector3(.03f, .06f, .03f), ctx.Rng, 0f); }
                halos.Add(cb + up * 2f, 1.2f, new Color(1f, .75f, .4f), .8f);
            }
            z.area.glowLight = ArtistBits.PointLight(z.root, c + up * 5f + z.front * 3f, new Color(1f, .8f, .5f), 16f, 1.6f);
        }

        void SurfShack(Spot z)
        {
            var ctx = z.ctx; var up = Vector3.up; var rot = Quaternion.LookRotation(z.front); var c = z.At(-3f, 0);
            z.mk.Box(wood, c + up * 1.4f, new Vector3(5f, 2.8f, 4f), rot); z.solid.Box(wood, c + up * 1.4f, new Vector3(5f, 2.8f, 4f), rot);
            z.mk.GableRoof(WorldMaterials.Solid(new Color(.8f, .7f, .45f)), c + up * 2.8f, new Vector3(4.6f, 0, 5.6f), 1.6f, Quaternion.LookRotation(z.side));
            Color[] boards = { new Color(.3f, .7f, .85f), new Color(1f, .6f, .3f), new Color(.95f, .9f, .6f), new Color(.9f, .35f, .4f) };
            for (var k = 0; k < 4; k++) z.mk.Box(WorldMaterials.Solid(boards[k], .7f), c + z.front * 2.2f + z.side * ((k - 1.5f) * .9f) + up * 1.3f, new Vector3(.55f, 2.6f, .08f), rot * Quaternion.Euler(-12f, 0, 0));
            foreach (var s in new[] { -1f, 1f }) { var tk = c + z.front * 3f + z.side * s * 3f; z.mk.Prism(wood, tk, up, .08f, 2f, 6); z.mk.Box(GlowM(new Color(1f, .6f, .2f), 2.6f), tk + up * 2.15f, new Vector3(.2f, .3f, .2f), Quaternion.identity); halos.Add(tk + up * 2.2f, 1f, new Color(1f, .6f, .2f), .9f); }
        }

        void Doorway(Spot z)
        {
            var ctx = z.ctx; var up = Vector3.up; var rot = Quaternion.LookRotation(z.front); var c = z.At(-3f, 0);
            foreach (var s in new[] { -1f, 1f }) { z.mk.Box(white, c + up * 1.6f + z.side * s * .9f, new Vector3(.2f, 3.2f, .3f), rot); }
            z.mk.Box(white, c + up * 3.25f, new Vector3(2f, .2f, .3f), rot);
            z.mk.Box(WorldMaterials.Solid(new Color(.3f, .2f, .15f)), c + up * 1.5f + z.side * .8f + z.front * .7f, new Vector3(.08f, 3f, 1.5f), rot * Quaternion.Euler(0, 60f, 0));
            z.mk.Box(GlowM(new Color(1f, .95f, .85f), 1.4f), c + up * 1.5f, new Vector3(1.6f, 3f, .04f), rot);
            halos.Add(c + up * 1.6f + z.front * 1.6f, 2.2f, new Color(1f, .95f, .85f), .7f);
            z.solid.Box(white, c + up * 1.6f, new Vector3(2.2f, 3.4f, .4f), rot);
            var cloud = WorldMaterials.Solid(new Color(.45f, .48f, .55f));
            for (var k = 0; k < 4; k++) z.mk.Blob(cloud, c + up * 9f + z.side * ((k - 1.5f) * 1.8f), new Vector3(1.6f, 1f, 1.4f), ctx.Rng, .3f);
            life.rainCloud = c + up * 8.4f;
        }

        void Small(Spot z)
        {
            var ctx = z.ctx; var up = Vector3.up; var rot = Quaternion.LookRotation(z.front); var c = z.At(-2f, 0);
            z.mk.Box(wood, c + up * .4f, new Vector3(5f, .8f, 4f), rot); z.solid.Box(wood, c + up * .4f, new Vector3(5f, .8f, 4f), rot);
            switch (Mathf.Abs(z.a.name.GetHashCode()) % 3)
            {
                case 0: // a guitar on a stand, an amp
                    Guitar(z.mk, ctx.Rng, c + up * .8f, z.front, Paint[Mathf.Abs(z.a.name.GetHashCode() / 3) % Paint.Length], 1.5f);
                    z.mk.Box(dark, c + up * 1.4f + z.side * 1.6f, new Vector3(1.2f, 1.2f, .7f), rot);
                    break;
                case 1: // an upright piano
                    z.mk.Box(dark, c + up * 1.6f, new Vector3(2.4f, 1.6f, .9f), rot);
                    z.mk.Box(white, c + up * 1.55f + z.front * .5f, new Vector3(2.2f, .06f, .3f), rot);
                    z.mk.Box(dark, c + up * 1.3f + z.front * 1.2f, new Vector3(1.2f, .1f, .5f), rot);
                    break;
                default: // a chrome microphone
                    z.mk.Prism(metal, c + up * .8f, up, .05f, 1.7f, 6);
                    z.mk.Blob(metal, c + up * 2.6f, new Vector3(.2f, .3f, .2f), ctx.Rng, 0f);
                    break;
            }
            var vinyl = WorldMaterials.Solid(new Color(.08f, .08f, .09f), .7f); var groove = WorldMaterials.Solid(new Color(.2f, .2f, .22f), .8f);
            var rc = c - z.front * 2.6f + up * 2.9f;
            z.mk.Prism(vinyl, rc - z.front * .15f, z.front, 3f, .3f, 32);
            z.mk.Prism(vinyl, rc + z.front * .15f, -z.front, 3f, .3f, 32);
            foreach (var s in new[] { 1f, -1f })
            {
                z.mk.Prism(groove, rc + z.front * s * .156f, z.front * s, 2.3f, .01f, 32);
                z.mk.Prism(WorldMaterials.Solid(Paint[Mathf.Abs(z.a.name.GetHashCode()) % 4], .5f), rc + z.front * s * .17f, z.front * s, 1f, .01f, 20);
                z.mk.Prism(dark, rc + z.front * s * .16f, z.front * s, .1f, .01f, 8);
            }
            z.solid.Box(vinyl, rc, new Vector3(5f, 5.8f, .5f), rot);
            z.mk.Prism(dark, c + z.side * 2.8f, up, .08f, 5f, 6);
            z.mk.Prism(GlowM(new Color(1f, .9f, .7f), 2.4f), c + z.side * 2.8f + up * 5f, (z.front - z.side * .4f - up).normalized, .3f, .4f, 8);
            halos.Add(c + z.side * 2.5f + up * 4.8f, 1.2f, new Color(1f, .9f, .7f), .7f);
        }
    }
}
