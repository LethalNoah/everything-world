using System.Collections.Generic;
using UnityEngine;

namespace EverythingWorld.World
{
    /// Echoflesh's "Moonhunter": an old pale villa at night behind an iron railing, as on the song's cover: two
    /// storeys under a dark hipped roof with chimneys, a little gable over the middle, warm orange light in some of
    /// the windows, a bare black tree beside it, stone gate pillars topped with glowing moon globes. The garden
    /// path from the gate to the door is a rhythm game's note highway: five coloured lanes ending in five fret
    /// buttons at the door. Inside, a couch faces a TV showing the same highway, two plastic guitar controllers
    /// lean on the couch, and a karaoke microphone stands ready on its stand. When "Moonhunter" plays, notes
    /// stream down the highway and the frets flash as they hit, every window in the house lights up, and a
    /// spotlight comes on over the microphone (SmallHoursLife).
    public partial class SmallHoursKit
    {
        const string MoonName = "Echoflesh", MoonhunterId = "28Rt2nyoq8XSTqtSHVu9Yl";
        public override (string song, string label, string artist)[] SpecialSongs => new[] { (MoonhunterId, "Moonhunter", MoonName) };
        static readonly Color[] FretColors = { new Color(.15f, 1f, .3f), new Color(1f, .15f, .15f), new Color(1f, .9f, .1f), new Color(.2f, .5f, 1f), new Color(1f, .55f, .1f) };

        void MoonhunterPlace(LandContext ctx, IslandWorldData.Artist a)
        {
            var root = new GameObject("Artist: " + a.name).transform; root.SetParent(ctx.Root, false);
            var F = facing.TryGetValue(a.name, out var f) ? f : new Vector3(center.x - a.x, 0, center.z - a.z).normalized;
            F.y = 0; if (F.sqrMagnitude < .01f) F = Vector3.forward; F.Normalize();
            var S = Vector3.Cross(Vector3.up, F); var up = Vector3.up; var rot = Quaternion.LookRotation(F);
            var mk = new MeshKit(); var solid = new MeshKit(); var pictures = new MeshKit();
            var labels = new GameObject("Labels").transform; labels.SetParent(root, false);
            var area = root.gameObject.AddComponent<ArtistArea>();
            area.artistName = a.name; area.stationId = a.stationId; area.radius = a.r; area.glowColor = WindowGlow;
            var glows = new List<Renderer>();
            Vector3 G(Vector3 p) => ctx.OnGround(p.x, p.z);
            var c = new Vector3(a.x, 0, a.z);

            // ---- the villa
            const float W = 14f, D = 10f, H1 = 3.8f, H2 = 3.3f, t = .3f;
            var flat = c - F * 3f; var (lo, hi) = Footprint(ctx, flat, rot, W, D);
            var fy = hi + .4f; var eave = fy + H1 + H2;
            var plaster = WorldMaterials.Solid(new Color(.84f, .82f, .76f)); var trim = WorldMaterials.Solid(new Color(.72f, .7f, .64f));
            var roofM = WorldMaterials.Solid(new Color(.24f, .22f, .24f));
            var unlit = new Color(.12f, .16f, .26f); var orange = new Color(1f, .45f, .12f);
            var darkWindows = new List<Material>();
            Material Window(bool lit)
            {
                var m = new Material(WorldMaterials.Glow(lit ? orange : unlit, lit ? 1.3f : .25f)) { name = lit ? "Lit window" : "Dark window" };
                if (!lit) darkWindows.Add(m);
                return m;
            }
            mk.Box(stone, new Vector3(flat.x, (lo + fy) / 2f - .2f, flat.z), new Vector3(W + .4f, fy - lo + .6f, D + .4f), rot);
            solid.Box(stone, new Vector3(flat.x, (lo + fy) / 2f - .2f, flat.z), new Vector3(W + .4f, fy - lo + .6f, D + .4f), rot);
            void Wall(Vector3 p, Vector3 s) { mk.Box(plaster, p, s, rot); solid.Box(plaster, p, s, rot); }
            var flatY = new Vector3(flat.x, 0, flat.z);
            // back and sides, full height
            Wall(flatY - F * (D / 2f - t / 2f) + up * (fy + (H1 + H2) / 2f), new Vector3(W, H1 + H2, t));
            foreach (var s in new[] { -1f, 1f }) Wall(flatY + S * s * (W / 2f - t / 2f) + up * (fy + (H1 + H2) / 2f), new Vector3(t, H1 + H2, D - 2f * t));
            // the front: a wide doorway in the middle on the ground floor, solid above
            var door = 2.6f; var fz = flatY + F * (D / 2f - t / 2f);
            foreach (var s in new[] { -1f, 1f }) Wall(fz + S * s * (door / 2f + (W - door) / 4f) + up * (fy + H1 / 2f), new Vector3((W - door) / 2f, H1, t));
            Wall(fz + up * (fy + 2.9f + (H1 - 2.9f) / 2f), new Vector3(door, H1 - 2.9f, t));
            Wall(fz + up * (fy + H1 + H2 / 2f), new Vector3(W, H2, t));
            // floors, the cornice between storeys, the band under the eaves, corner quoins
            mk.Box(darkWood, new Vector3(flat.x, fy - .05f, flat.z), new Vector3(W - .2f, .1f, D - .2f), rot);
            solid.Box(darkWood, new Vector3(flat.x, fy - .3f, flat.z), new Vector3(W, .5f, D), rot);
            mk.Box(plaster, new Vector3(flat.x, fy + H1, flat.z), new Vector3(W - .4f, .25f, D - .4f), rot);
            mk.Box(trim, flatY + up * (fy + H1) , new Vector3(W + .3f, .25f, D + .3f), rot);
            mk.Box(trim, flatY + up * (eave - .15f), new Vector3(W + .4f, .3f, D + .4f), rot);
            foreach (var s in new[] { -1f, 1f }) foreach (var b in new[] { -1f, 1f }) mk.Box(trim, flatY + S * s * (W / 2f) + F * b * (D / 2f) + up * (fy + (H1 + H2) / 2f), new Vector3(.5f, H1 + H2, .5f), rot);
            // windows: tall, with sills; warm light in some, as on the cover, dark blue glass in the rest
            void Win(Vector3 at, Vector3 n, bool lit)
            {
                var r = Quaternion.LookRotation(n);
                mk.Box(Window(lit), at, new Vector3(1.1f, 1.7f, .05f), r);
                mk.Box(trim, at - up * .95f + n * .06f, new Vector3(1.4f, .12f, .2f), r);
                mk.Box(trim, at + up * .95f + n * .04f, new Vector3(1.3f, .14f, .12f), r);
                mk.Box(darkWood, at + n * .03f, new Vector3(.06f, 1.7f, .03f), r);
                if (lit) halos.Add(at + n * .6f, 1.2f, orange, .7f);
            }
            float[] cols = { -5.4f, -3.2f, 3.2f, 5.4f };
            foreach (var x in cols) Win(fz + F * (t / 2f + .01f) + S * x + up * (fy + 1.9f), F, x > 0);
            float[] cols2 = { -5.4f, -3.2f, 0f, 3.2f, 5.4f };
            foreach (var x in cols2) Win(fz + F * (t / 2f + .01f) + S * x + up * (fy + H1 + 1.7f), F, x == 3.2f || x == 0f);
            foreach (var s in new[] { -1f, 1f })
                foreach (var z in new[] { -2.2f, 2.2f })
                {
                    Win(flatY + S * s * (W / 2f + .01f) + F * z + up * (fy + 1.9f), S * s, s > 0 && z > 0);
                    Win(flatY + S * s * (W / 2f + .01f) + F * z + up * (fy + H1 + 1.7f), S * s, false);
                }
            // the hipped roof
            var ry = eave + 3.6f; var hw = W / 2f + .5f; var hd = D / 2f + .5f; var ridge = hw - hd;
            var ec = flatY + up * eave; var inside = ec + up * .4f;
            Vector3 E(float sx, float fz2) => ec + S * sx + F * fz2;
            Vector3 R(float sx) => flatY + up * ry + S * sx;
            mk.Quad(roofM, E(-hw, hd), E(hw, hd), R(ridge), R(-ridge), inside);
            mk.Quad(roofM, E(hw, -hd), E(-hw, -hd), R(-ridge), R(ridge), inside);
            mk.Tri(roofM, E(-hw, -hd), E(-hw, hd), R(-ridge), inside);
            mk.Tri(roofM, E(hw, hd), E(hw, -hd), R(ridge), inside);
            mk.Quad(roofM, E(-hw, hd), E(hw, hd), E(hw, -hd), E(-hw, -hd), ec + up * 3f);   // soffit underneath
            // the little gable over the middle of the front, with a round window
            var gb = flatY + F * (D / 2f + .05f) + up * eave;
            mk.Tri(plaster, gb - S * 2.4f, gb + S * 2.4f, gb + up * 2.6f, gb - F * 1f);
            mk.GableRoof(roofM, gb - F * 1.2f + up * .02f, new Vector3(5.4f, 0, 3f), 2.7f, Quaternion.LookRotation(F));
            mk.Prism(Window(true), gb + up * 1f + F * .02f, F, .38f, .04f, 14);
            // chimneys
            foreach (var s in new[] { -1f, 1f }) { var ch = flatY + S * s * 3.6f - F * 1.6f; mk.Box(WorldMaterials.Solid(new Color(.55f, .5f, .48f)), ch + up * (ry - .2f), new Vector3(.8f, 2.6f, .8f), rot); mk.Box(trim, ch + up * (ry + 1.15f), new Vector3(1f, .2f, 1f), rot); }
            // steps up to the door
            var stepsN = Mathf.Max(1, Mathf.CeilToInt((fy - ctx.Ground(fz.x + F.x * 2f, fz.z + F.z * 2f)) / .25f));
            for (var k = 0; k < stepsN; k++)
            {
                var sp = fz + F * (.5f + (stepsN - k) * .45f); var gy = ctx.Ground(sp.x, sp.z);
                var top = fy - k * .25f;
                if (top < gy) break;
                mk.Box(stone, new Vector3(sp.x, (top + gy) / 2f - .2f, sp.z), new Vector3(door + 1f, top - gy + .4f, .5f + .02f), rot);
                solid.Box(stone, new Vector3(sp.x, (top + gy) / 2f - .2f, sp.z), new Vector3(door + 1f, top - gy + .4f, .5f), rot);
            }
            IndoorSpaces.Add(flatY + up * (fy + H1 / 2f), rot, new Vector3(W, H1, D));

            // ---- inside: the couch, the TV with the highway on it, two guitar controllers, the karaoke mic
            var back = flatY - F * (D / 2f - .6f) + up * fy;
            mk.Box(darkWood, back + up * .3f, new Vector3(3.2f, .6f, .7f), rot); solid.Box(darkWood, back + up * .3f, new Vector3(3.2f, .6f, .7f), rot);
            mk.Box(WorldMaterials.Solid(new Color(.05f, .05f, .06f)), back + up * 1.45f, new Vector3(2.6f, 1.5f, .12f), rot);
            var tv = new Material(WorldMaterials.Glow(new Color(.25f, .3f, .55f), 1.2f)) { name = "TV" };
            mk.Box(tv, back + up * 1.45f + F * .07f, new Vector3(2.4f, 1.32f, .02f), rot);
            for (var l = 0; l < 5; l++)
            {
                var lx = (l - 2f) * .26f;
                mk.Box(WorldMaterials.Glow(new Color(.8f, .85f, 1f), .8f), back + up * 1.45f + F * .085f + S * (lx * .6f), new Vector3(.015f, 1.2f, .01f), rot * Quaternion.Euler(0, 0, -lx * 25f));
                mk.Prism(WorldMaterials.Glow(FretColors[l], 2f), back + up * .92f + F * .085f + S * (lx * 1.1f), F, .07f, .01f, 10);
            }
            var couch = back + F * 4.2f; var crot = Quaternion.LookRotation(-F);
            var fabric = WorldMaterials.Solid(new Color(.35f, .2f, .22f));
            mk.Box(fabric, couch + up * .35f, new Vector3(3f, .5f, 1.1f), crot); mk.Box(fabric, couch + up * .85f + F * .45f, new Vector3(3f, .8f, .25f), crot);
            foreach (var s in new[] { -1.5f, 1.5f }) mk.Box(fabric, couch + up * .55f + S * s, new Vector3(.25f, .7f, 1.1f), crot);
            solid.Box(fabric, couch + up * .5f, new Vector3(3.2f, 1f, 1.2f), crot);
            for (var g = 0; g < 2; g++) Controller(mk, couch - F * .7f + S * (g == 0 ? -.8f : .8f) + up * .05f, -F, S, g == 0 ? new Color(.85f, .15f, .15f) : new Color(.1f, .1f, .12f));
            // the karaoke mic, by the couch, with a spotlight above it that waits for the song
            var mic = couch + S * 2.4f - F * .6f;
            mk.Prism(iron, mic, up, .035f, 1.6f, 6); mk.Box(iron, mic + up * .02f, new Vector3(.6f, .05f, .6f), rot);
            mk.Box(iron, mic + up * 1.62f, new Vector3(.05f, .05f, .35f), Quaternion.LookRotation(F + up * .4f));
            mk.Prism(WorldMaterials.Solid(new Color(.15f, .15f, .17f)), mic + up * 1.68f + F * .18f, (F + up * .5f).normalized, .07f, .18f, 10);
            mk.Blob(WorldMaterials.Solid(new Color(.75f, .75f, .78f), .9f), mic + up * 1.78f + F * .26f, Vector3.one * .1f, ctx.Rng, 0f);
            ArtistBits.PostCollider(root, mic, 1.8f, .25f);
            var spot = new GameObject("Karaoke spotlight").AddComponent<Light>(); spot.transform.SetParent(root, false);
            spot.type = LightType.Spot; spot.spotAngle = 40f; spot.range = 8f; spot.intensity = 0f; spot.color = new Color(.85f, .55f, 1f);
            spot.transform.SetPositionAndRotation(mic + up * (H1 - .3f), Quaternion.LookRotation(Vector3.down));
            ArtistBits.CoverWall(pictures, labels, ctx, a, flatY - S * (W / 2f - t - .03f) + up * (fy + 2.3f), S, 1, 1f, 1, true, Ink);
            ArtistBits.PointLight(root, flatY + up * (fy + H1 - .5f), orange, 10f, 1.2f, true);

            // ---- the yard: iron railing with stone pillars, the gate with moon globes, the bare tree
            var gateC = c + F * 14f; var fenceHalf = 13f;
            var railM = WorldMaterials.Solid(new Color(.1f, .1f, .12f), .5f); var pillarM = WorldMaterials.Solid(new Color(.6f, .58f, .54f));
            void Pillar(Vector3 at, float h, bool moon)
            {
                var g = G(at); mk.Box(pillarM, g + up * (h / 2f - .2f), new Vector3(.8f, h + .4f, .8f), rot); mk.Box(trim, g + up * (h + .1f), new Vector3(1f, .2f, 1f), rot);
                solid.Box(pillarM, g + up * h / 2f, new Vector3(.8f, h, .8f), rot);
                if (!moon) return;
                mk.Blob(WorldMaterials.Glow(new Color(1f, .9f, .62f), 1.1f), g + up * (h + .45f), Vector3.one * .3f, ctx.Rng, 0f);
                halos.Add(g + up * (h + .45f), 1.1f, new Color(1f, .88f, .55f), .6f);
            }
            void Railing(Vector3 a0, Vector3 a1)
            {
                var d = a1 - a0; var n = Mathf.CeilToInt(d.magnitude / .3f); var r = Quaternion.LookRotation(d);
                for (var k = 1; k < n; k++) { var p = G(Vector3.Lerp(a0, a1, k / (float)n)); mk.Box(railM, p + up * .9f, new Vector3(.04f, 1.8f, .04f), r); mk.Cone(railM, p + up * 1.8f, .06f, .18f, 4); }
                foreach (var y in new[] { .25f, 1.55f }) { var p0 = G(a0) + up * y; var p1 = G(a1) + up * y; mk.Box(railM, (p0 + p1) / 2f, new Vector3(.06f, .06f, Vector3.Distance(p0, p1)), Quaternion.LookRotation(p1 - p0)); }
                var m0 = G(a0); var m1 = G(a1); solid.Box(railM, (m0 + m1) / 2f + up * .9f, new Vector3(.15f, 1.8f, Vector3.Distance(m0, m1)), Quaternion.LookRotation(m1 - m0));
            }
            var gateHalf = 1.9f;
            foreach (var s in new[] { -1f, 1f })
            {
                Pillar(gateC + S * s * gateHalf, 2.6f, true);
                Railing(gateC + S * s * (gateHalf + .4f), gateC + S * s * (fenceHalf - .4f));
                Pillar(gateC + S * s * fenceHalf, 2.1f, false);
                Railing(gateC + S * s * fenceHalf - F * .4f, gateC + S * s * fenceHalf - F * 15f);
                // the gate leaves, standing open
                var hinge = G(gateC + S * s * (gateHalf - .3f)); var leaf = (S * -s * .55f + F * .85f).normalized;
                for (var k = 1; k < 6; k++) mk.Box(railM, hinge + leaf * (k * .3f) + up * 1f, new Vector3(.04f, 2f, .04f), Quaternion.LookRotation(leaf));
                foreach (var y in new[] { .3f, 1.7f }) mk.Box(railM, hinge + leaf * .85f + up * y, new Vector3(.06f, .06f, 1.7f), Quaternion.LookRotation(leaf));
            }
            BareTree(ctx, mk, G(c + S * 10.5f + F * 4f), 1.3f);
            ArtistBits.PostCollider(root, G(c + S * 10.5f + F * 4f), 6f, .6f);

            // ---- the note highway: the garden path from the gate to the door
            var hA = G(gateC - F * .5f); var hB = G(fz + F * (1f + stepsN * .45f + .6f));
            var len = Vector3.Distance(new Vector3(hA.x, 0, hA.z), new Vector3(hB.x, 0, hB.z));
            const float laneW = .7f; var hwW = laneW * 5f + .4f;
            var path = new List<Vector3>(); for (var k = 0; k <= 8; k++) path.Add(G(Vector3.Lerp(hA, hB, k / 8f)) + up * .05f);
            var deck = WorldMaterials.Solid(new Color(.05f, .05f, .08f), .6f);
            for (var k = 0; k + 1 < path.Count; k++) mk.Quad(deck, path[k] - S * hwW / 2f, path[k] + S * hwW / 2f, path[k + 1] + S * hwW / 2f, path[k + 1] - S * hwW / 2f, (path[k] + path[k + 1]) / 2f - up);
            var rail = new Material(WorldMaterials.Glow(new Color(.75f, .8f, 1f), 1.1f)) { name = "Highway edge" };
            for (var k = 0; k + 1 < path.Count; k++)
            {
                var p0 = path[k]; var p1 = path[k + 1]; var r = Quaternion.LookRotation(p1 - p0); var mid = (p0 + p1) / 2f;
                foreach (var e in new[] { -1f, 1f }) mk.Box(rail, mid + S * e * (hwW / 2f) + up * .04f, new Vector3(.1f, .06f, Vector3.Distance(p0, p1) + .02f), r);
                for (var l = 1; l < 5; l++) mk.Box(WorldMaterials.Glow(new Color(.5f, .55f, .75f), .5f), mid + S * ((l - 2.5f) * laneW) + up * .03f, new Vector3(.03f, .02f, Vector3.Distance(p0, p1) + .02f), r);
                mk.Box(WorldMaterials.Glow(new Color(.5f, .55f, .75f), .4f), p0 + up * .03f, new Vector3(hwW, .02f, .05f), r);     // beat lines
            }
            var frets = new Material[5];
            var strike = hB - (hB - hA).normalized * .9f;
            for (var l = 0; l < 5; l++)
            {
                frets[l] = new Material(WorldMaterials.Glow(FretColors[l], 1f)) { name = "Fret " + l };
                var fp = strike + S * ((l - 2f) * laneW);
                mk.Prism(WorldMaterials.Solid(new Color(.85f, .85f, .88f), .8f), fp, up, .32f, .1f, 16);
                mk.Prism(frets[l], fp + up * .1f, up, .25f, .04f, 16);
            }
            mk.Box(WorldMaterials.Glow(new Color(.9f, .9f, 1f), 1.4f), strike + up * .1f, new Vector3(hwW, .03f, .06f), rot);
            ctx.AddKeepout((hA.x + hB.x) / 2f, (hA.z + hB.z) / 2f, len / 2f + 2f);
            // the notes that stream down it while the song plays (hidden until then)
            var gems = new List<Transform>(); var lanes = new List<int>(); var rng = new System.Random(2016);
            for (var k = 0; k < 24; k++)
            {
                var l = rng.Next(5); var gk = new MeshKit();
                gk.Prism(WorldMaterials.Solid(new Color(.9f, .9f, .92f), .8f), Vector3.zero, Vector3.up, .3f, .1f, 14);
                gk.Prism(WorldMaterials.Glow(FretColors[l], 2.2f), Vector3.up * .1f, Vector3.up, .26f, .08f, 14);
                var gt = gk.BuildSingle("Note", root, false).transform; gt.gameObject.SetActive(false);
                gems.Add(gt); lanes.Add(l);
            }
            life.moonGems = gems.ToArray(); life.moonGemLanes = lanes.ToArray(); life.moonFrets = frets; life.moonFretColors = FretColors;
            life.moonA = hA; life.moonB = strike; life.moonSide = S; life.moonLane = laneW; life.moonWindows = darkWindows.ToArray();
            life.moonSpot = spot; life.moonTv = tv; life.moonId = MoonhunterId;

            ArtistBits.Plaque(mk, solid, labels, a, G(gateC + F * 1.5f + S * 4.5f), F, darkWood, board, Ink, 1.4f);
            glows.Add(ArtistBits.GlassBox(root, fz + F * .3f + up * (fy + 3.4f), new Vector3(.3f, .4f, .3f), orange));
            foreach (var g in glows) if (g != null) halos.Add(g.transform.position, 1.1f, orange, .8f);
            area.glowRenderers = glows.ToArray();
            mk.Build("Home", root, false);
            solid.BuildCollider("Walls", root);
            pictures.Build("Covers", root, false);
            ctx.AddKeepout(flat.x, flat.z, 9f);
        }

        /// A guitar game controller: a small plastic guitar body, a neck with five coloured fret buttons near the
        /// head, a strum bar, leaning against the couch.
        void Controller(MeshKit mk, Vector3 at, Vector3 face, Vector3 side, Color body)
        {
            var rot = Quaternion.LookRotation(face) * Quaternion.Euler(-15f, 0, 0); var up = rot * Vector3.up;
            mk.Box(WorldMaterials.Solid(body, .7f), at + up * .3f, new Vector3(.38f, .45f, .07f), rot);
            mk.Box(WorldMaterials.Solid(body, .7f), at + up * .52f + (rot * Vector3.right) * .12f, new Vector3(.16f, .18f, .07f), rot);
            mk.Box(WorldMaterials.Solid(new Color(.1f, .1f, .1f)), at + up * .9f, new Vector3(.07f, .7f, .05f), rot);
            mk.Box(WorldMaterials.Solid(new Color(.9f, .9f, .9f)), at + up * .32f + rot * Vector3.forward * .04f, new Vector3(.18f, .05f, .02f), rot);    // strum bar
            for (var l = 0; l < 5; l++) mk.Box(WorldMaterials.Glow(FretColors[l], 1.2f), at + up * (1.1f - l * .06f) + rot * Vector3.forward * .03f, new Vector3(.05f, .04f, .015f), rot);
        }

        /// A bare black tree with no leaves, its branches reaching up and out.
        void BareTree(LandContext ctx, MeshKit mk, Vector3 at, float s)
        {
            var barkM = WorldMaterials.Solid(new Color(.08f, .07f, .07f));
            void Branch(Vector3 from, Vector3 dir, float len, float r, int depth)
            {
                mk.Prism(barkM, from, dir, r, len, 6, .65f);
                if (depth == 0) return;
                var end = from + dir * len;
                var n = depth == 3 ? 4 : 3;
                for (var k = 0; k < n; k++)
                {
                    var d = (Quaternion.Euler(0, ctx.Range(0, 360f), 0) * Vector3.forward * ctx.Range(.4f, .9f) + dir * ctx.Range(.6f, 1.1f)).normalized;
                    Branch(end - dir * .1f, d, len * ctx.Range(.55f, .75f), r * .6f, depth - 1);
                }
            }
            Branch(at - Vector3.up * .3f, Vector3.up, 4.5f * s, .45f * s, 3);
        }
    }
}
