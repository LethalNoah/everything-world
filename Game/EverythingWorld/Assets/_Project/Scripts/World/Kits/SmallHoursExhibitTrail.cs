using System.Collections.Generic;
using UnityEngine;

namespace EverythingWorld.World
{
    /// The Synchronous World's trail (part of SmallHoursKit): from its gate on the ring, a long path sweeping round the
    /// edge of the place, twelve stations along it and 22 lanterns, warm gold at the start turning through rose and
    /// violet to blue and finally ice. While the song plays a light travels along it in time with the song, station
    /// by station; at the end frost runs back along the whole trail and every lantern freezes to ice.
    public partial class SmallHoursKit
    {
        readonly List<Vector3> trail = new List<Vector3>();
        readonly Vector3[] trailStation = new Vector3[12];
        readonly Vector3[] trailFace = new Vector3[12];

        static Color TrailColour(float u) => u < .35f ? Color.Lerp(new Color(1f, .62f, .25f), new Color(1f, .45f, .55f), u / .35f)
            : u < .7f ? Color.Lerp(new Color(1f, .45f, .55f), new Color(.55f, .45f, 1f), (u - .35f) / .35f)
            : Color.Lerp(new Color(.55f, .45f, 1f), new Color(.8f, .93f, 1f), (u - .7f) / .3f);

        void TrailPlan(LandContext ctx, Vector3 N, Vector3 F, Vector3 side, Vector3 C)
        {
            trail.Clear();
            var start = ringPiece[10]; var startR = Vector2.Distance(new Vector2(start.x, start.z), new Vector2(N.x, N.z));
            var phi0 = Mathf.Atan2(start.z - N.z, start.x - N.x);
            const float RT = 95f, Sweep = 130f * Mathf.Deg2Rad;
            List<Vector3> Try(float dir)
            {
                var pts = new List<Vector3>();
                for (var u = 0f; u <= 1.0001f; u += .008f)
                {
                    var r = Mathf.Lerp(startR + 7f, RT, Mathf.SmoothStep(0, 1, Mathf.Clamp01(u / .12f)));
                    var a = phi0 + dir * u * Sweep + Mathf.Sin(u * 9f) * .02f;
                    var p = new Vector3(N.x + Mathf.Cos(a) * r, 0, N.z + Mathf.Sin(a) * r);
                    if (!ctx.IsLand(p.x, p.z, 6f)) return null;
                    pts.Add(G(ctx, p));
                }
                return pts;
            }
            var path = Try(1f) ?? Try(-1f);
            if (path == null) { Debug.Log("The Small Hours: no room for the Synchronous World's trail"); return; }
            trail.AddRange(path);
            foreach (var p in trail) Remember(p);
            for (var j = 0; j < 12; j++)
            {
                var k = Mathf.Clamp(Mathf.RoundToInt((j + .5f) / 12f * (trail.Count - 1)), 1, trail.Count - 2);
                var along = trail[k + 1] - trail[k - 1]; along.y = 0; along.Normalize();
                var outward = Vector3.Cross(Vector3.up, along) * (j % 2 == 0 ? 1f : -1f);
                trailStation[j] = G(ctx, trail[k] + outward * 5.5f); trailFace[j] = -outward;
                exhibitSpots.Add((trailStation[j], 4.5f)); exhibitCentres.Add(trailStation[j]);
            }
        }

        void TrailBuild(LandContext ctx, Transform root, MeshKit mk, MeshKit solid, Transform labels)
        {
            if (trail.Count < 2) return;
            var id = TwelfthHouse[10].id; var up = Vector3.up;
            Ribbon(ctx, mk, WorldMaterials.Solid(new Color(.16f, .22f, .24f)), trail, 1.8f, .06f);
            // 22 lanterns: the light passes along them in time with the song; at the end each freezes to ice
            for (var k = 0; k < 22; k++)
            {
                var u = (k + .5f) / 22f; var i = Mathf.Clamp(Mathf.RoundToInt(u * (trail.Count - 1)), 1, trail.Count - 2);
                var along = trail[i + 1] - trail[i - 1]; along.y = 0; along.Normalize(); var sd = Vector3.Cross(up, along) * (k % 2 == 0 ? 1f : -1f);
                var post = G(ctx, trail[i] + sd * 1.4f);
                mk.Prism(darkWood, post, up, .06f, 2.2f, 5); ArtistBits.PostCollider(root, post, 2.2f, .1f);
                mk.Box(darkWood, post + up * 2.15f - sd * .25f, new Vector3(.05f, .05f, .5f), Quaternion.LookRotation(sd));
                var lamp = post + up * 1.8f - sd * .45f; var col = TrailColour(u);
                mk.Box(Cue(id, col, .4f, 3f, k / 22f, (k + 1.2f) / 22f), lamp, new Vector3(.16f, .24f, .16f), Quaternion.identity);
                mk.Box(Cue(id, Ice, 0f, 1.5f, .96f, 1f), lamp + up * .16f, new Vector3(.18f, .04f, .18f), Quaternion.identity);
                halos.Add(lamp, 1f, col, .55f);
            }
            for (var j = 0; j < 12; j++) { TrailStation(ctx, root, mk, solid, j, trailStation[j], trailFace[j], id); ExMark(ctx, root, "exhibit, trail station " + (j + 1), trailStation[j] + up * 1.3f, trailStation[j] + trailFace[j] * 7.5f + Vector3.Cross(up, trailFace[j]) * 2f); }
            ExMark(ctx, root, "exhibit, the Synchronous World's trail", trail[trail.Count / 2] + up * 1.5f, trail[trail.Count / 2 - 6] + up * 1f);
        }

        void TrailStation(LandContext ctx, Transform root, MeshKit mk, MeshKit solid, int j, Vector3 c, Vector3 face, string id)
        {
            var up = Vector3.up; var side = Vector3.Cross(up, face); var rot = Quaternion.LookRotation(face);
            float from = j / 12f, to = (j + 1) / 12f; var col = TrailColour((j + .5f) / 12f);
            Material Lit(float idle = .4f, float gain = 3f, Color? cc = null) => Cue(id, cc ?? col, idle, gain, from, to + .01f);
            switch (j)
            {
                case 0:   // a meadow of floating lights round a little piano
                    mk.Box(darkWood, c + up * .55f, new Vector3(1.2f, 1.1f, .5f), rot); solid.Box(darkWood, c + up * .55f, new Vector3(1.2f, 1.1f, .5f), rot);
                    mk.Box(Lit(), c + up * .8f + face * .26f, new Vector3(1f, .05f, .02f), rot);
                    for (var k = 0; k < 16; k++) mk.Blob(Lit(.5f), c + new Vector3(ctx.Range(-3f, 3f), ctx.Range(.6f, 2.6f), ctx.Range(-3f, 3f)), Vector3.one * .07f, ctx.Rng, 0f);
                    break;
                case 1:   // two lanterns spinning together in a bright little dance
                    var pair = Part(root, "Two lanterns", c + up * 1.8f, Quaternion.identity);
                    pair.mk.Box(iron, Vector3.zero, new Vector3(1.6f, .04f, .04f), Quaternion.identity);
                    foreach (var s in new[] { -.8f, .8f }) pair.mk.Box(Lit(.6f), new Vector3(s, -.25f, 0), new Vector3(.2f, .3f, .2f), Quaternion.identity);
                    Done(pair); Moves(pair.t, id, SmallHoursLife.Move.Spin, up, default, 40f, 0f, 1f, 3f);
                    mk.Prism(iron, c, up, .05f, 1.8f, 5); ArtistBits.PostCollider(root, c, 1.8f, .1f); break;
                case 2:   // a bench where one lantern slowly dims
                    Bench(mk, solid, c, face);
                    var lp = c + side * 1.1f; mk.Prism(iron, lp, up, .04f, 1.6f, 5); ArtistBits.PostCollider(root, lp, 1.6f, .1f);
                    mk.Box(Cue(id, col, 1.2f, -1f, from, to + .02f), lp + up * 1.7f, new Vector3(.18f, .26f, .18f), Quaternion.identity); break;
                case 3:   // a garden arch in clean white light
                    for (var k = 0; k < 11; k++) { var a = Mathf.PI - k * Mathf.PI / 10f; mk.Box(Lit(.5f, 2.5f, new Color(.95f, .95f, 1f)), c + side * Mathf.Cos(a) * 1.4f + up * (1.8f + Mathf.Sin(a) * 1.2f), new Vector3(.14f, .3f, .14f), rot * Quaternion.Euler(0, 0, a * Mathf.Rad2Deg)); }
                    foreach (var s in new[] { -1.4f, 1.4f }) { mk.Box(WorldMaterials.Solid(new Color(.9f, .9f, .88f)), c + side * s + up * .9f, new Vector3(.12f, 1.8f, .12f), rot); ArtistBits.PostCollider(root, c + side * s, 1.8f, .12f); }
                    for (var k = 0; k < 14; k++) mk.Blob(WorldMaterials.Solid(new Color(.45f, .6f, .45f)), c + side * ((k % 2 == 0 ? -1.4f : 1.4f)) + up * ctx.Range(.4f, 2.6f), new Vector3(.25f, .2f, .2f), ctx.Rng, .3f);
                    break;
                case 4:   // a small open-air stage, a crowd of lights swaying in threes, blazing hottest at the solo
                    mk.Box(darkWood, c - face * 1.2f + up * .3f, new Vector3(3f, .6f, 2f), rot); solid.Box(darkWood, c - face * 1.2f + up * .3f, new Vector3(3f, .6f, 2f), rot);
                    mk.Box(Cue(id, new Color(1f, .5f, .15f), .3f, 4f, from, to + .01f, 1.2f, 0f, .7f), c - face * 1.2f + up * .62f, new Vector3(2.8f, .02f, 1.8f), rot);
                    var crowd = Part(root, "Concert crowd", c + face * 2f, rot);
                    for (var k = 0; k < 15; k++) crowd.mk.Blob(Lit(.4f), new Vector3((k % 5 - 2f) * .6f, 1.4f + (k / 5) * .1f, (k / 5) * .6f), Vector3.one * .08f, ctx.Rng, 0f);
                    Done(crowd); Moves(crowd.t, id, SmallHoursLife.Move.Swing, Vector3.forward, default, 2.1f, from, to, 8f); break;
                case 5:   // a night road with a lone saxophone on a stand, in tired blue haze
                    var road = WorldMaterials.Solid(new Color(.2f, .21f, .24f)); mk.Box(road, G(ctx, c) + up * .03f, new Vector3(6f, .04f, 2.4f), Quaternion.LookRotation(side));
                    for (var k = -2; k <= 2; k++) mk.Box(WorldMaterials.Solid(new Color(.9f, .85f, .5f)), G(ctx, c + side * k * 1.2f) + up * .06f, new Vector3(.6f, .01f, .08f), Quaternion.LookRotation(side) * Quaternion.Euler(0, 90f, 0));
                    var sx = c + face * 1.6f; mk.Prism(iron, sx, up, .03f, .6f, 5); var brass = Lit(.4f, 2.5f, new Color(.85f, .65f, .3f));
                    mk.Prism(brass, sx + up * .6f, up, .06f, .7f, 8, 1.3f); mk.Prism(brass, sx + up * .55f, (face + up).normalized, .09f, .3f, 8, 1.6f);
                    halos.Add(c + up * 1.2f, 2.6f, new Color(.35f, .45f, 1f), .35f); halos.Add(sx + up * 1f, 1.2f, new Color(.85f, .65f, .3f), .5f); break;
                case 6:   // a stairway of piano keys climbing
                    for (var k = 0; k < 8; k++)
                    {
                        var p = c - face * (k * .55f) + up * (k * .28f + .14f);
                        mk.Box(WorldMaterials.Solid(k % 2 == 0 ? new Color(.95f, .95f, .93f) : new Color(.08f, .08f, .09f)), p, new Vector3(1.4f, .28f + k * .28f, .5f), rot);
                        solid.Box(darkWood, p - up * (k * .14f), new Vector3(1.4f, .28f + k * .28f, .5f), rot);
                        mk.Box(Cue(id, col, .1f, 3f, from, to + .01f, 1.6f, -k / 8f, .2f), p + up * (.15f + k * .14f) + face * .26f, new Vector3(1.2f, .04f, .02f), rot);
                    }
                    break;
                case 7:   // a painted pirate ship and a palm, only fronts: round the back there's nothing
                    var paint = WorldMaterials.Solid(new Color(.55f, .35f, .2f)); var back = WorldMaterials.Solid(new Color(.5f, .5f, .5f));
                    mk.Box(paint, c + up * .8f + face * .02f, new Vector3(4f, 1.2f, .04f), rot); mk.Box(back, c + up * .8f - face * .02f, new Vector3(4f, 1.2f, .03f), rot);
                    mk.Box(paint, c + up * 2.4f, new Vector3(.08f, 2f, .04f), rot); mk.Box(WorldMaterials.Solid(new Color(.92f, .9f, .85f)), c + up * 2.6f + face * .03f + side * .6f, new Vector3(1.1f, 1.2f, .03f), rot);
                    var palm = c + side * 2.8f; mk.Box(WorldMaterials.Solid(new Color(.5f, .38f, .22f)), palm + up * 1.3f, new Vector3(.25f, 2.6f, .04f), rot); mk.Box(WorldMaterials.Solid(new Color(.25f, .55f, .3f)), palm + up * 2.7f, new Vector3(1.6f, .5f, .04f), rot);
                    foreach (var s in new[] { -1.5f, 1.5f }) mk.Box(darkWood, c + side * s - face * .5f + up * .7f, new Vector3(.08f, 1.4f, .08f), Quaternion.LookRotation(face) * Quaternion.Euler(-30f, 0, 0));
                    solid.Box(paint, c + up * .9f, new Vector3(4f, 1.6f, .3f), rot);
                    mk.Box(Lit(.3f, 2f, new Color(1f, .75f, .3f)), c + up * 1.5f + face * .05f, new Vector3(3.6f, .06f, .02f), rot); break;
                case 8:   // a gothic iron gate and a broken chapel arch in black and red: the darkest station
                    var blk = WorldMaterials.Solid(new Color(.06f, .05f, .06f));
                    foreach (var s in new[] { -1.4f, 1.4f }) { mk.Box(blk, c + side * s + up * 2f, new Vector3(.35f, 4f, .35f), rot); ArtistBits.PostCollider(root, c + side * s, 4f, .3f); }
                    for (var k = 0; k < 7; k++) { var a = Mathf.PI - k * Mathf.PI / 9f; mk.Box(blk, c + side * Mathf.Cos(a) * 1.4f + up * (4f + Mathf.Sin(a) * 1.4f), new Vector3(.3f, .4f, .3f), rot * Quaternion.Euler(0, 0, a * Mathf.Rad2Deg)); }
                    for (var k = 0; k < 9; k++) { mk.Box(iron, c + side * ((k - 4f) * .28f) + face * 1.2f + up * 1.2f, new Vector3(.04f, 2.4f, .04f), rot); mk.Box(iron, c + side * ((k - 4f) * .28f) + face * 1.2f + up * 2.5f, new Vector3(.06f, .2f, .06f), rot * Quaternion.Euler(0, 0, 45f)); }
                    solid.Box(iron, c + face * 1.2f + up * 1.2f, new Vector3(2.6f, 2.4f, .1f), rot);
                    mk.Box(Cue(id, BloodRed, .2f, 3.5f, from, to + .01f, .35f, 0f, .4f), c + up * 2f - face * .1f, new Vector3(2.4f, 3.6f, .02f), rot); break;
                case 9:   // a disc of nothing, lights drifting over it with no centre
                    mk.Prism(WorldMaterials.Solid(new Color(.01f, .01f, .015f)), G(ctx, c) + up * .03f, up, 3f, .02f, 24);
                    var drift = Part(root, "No root", c + up * 1.5f, Quaternion.identity);
                    for (var k = 0; k < 12; k++) drift.mk.Blob(Lit(.25f, 2f), new Vector3(ctx.Range(-2.5f, 2.5f), ctx.Range(-.5f, 1f), ctx.Range(-2.5f, 2.5f)), Vector3.one * .06f, ctx.Rng, 0f);
                    Done(drift); Moves(drift.t, id, SmallHoursLife.Move.Spin, new Vector3(.4f, 1f, .2f).normalized, default, 6f, 0f, 1f, 2f); break;
                case 10:  // a single great beam planting into the ground beside a seated stone figure lit from within
                    var fig = c + side * 1.2f; var stoneM = WorldMaterials.Solid(new Color(.5f, .52f, .56f));
                    mk.Box(stoneM, fig + up * .25f, new Vector3(.9f, .5f, .9f), rot); mk.Blob(stoneM, fig + up * 1f, new Vector3(.45f, .55f, .35f), ctx.Rng, .1f); mk.Blob(stoneM, fig + up * 1.7f, Vector3.one * .25f, ctx.Rng, 0f);
                    solid.Box(stoneM, fig + up * .8f, new Vector3(1f, 1.6f, 1f), rot);
                    mk.Blob(Lit(.4f, 2.5f, new Color(1f, .9f, .7f)), fig + up * 1.05f + face * .3f, Vector3.one * .15f, ctx.Rng, 0f);
                    var beamP = Part(root, "The root lands", c - side * .8f + up * 6f, rot); beamP.mk.Box(WorldMaterials.Glow(Ice, 3f), Vector3.zero, new Vector3(.35f, 12f, .35f), Quaternion.identity); Done(beamP);
                    Moves(beamP.t, id, SmallHoursLife.Move.Show, default, default, .6f, from, 1f);
                    mk.Prism(Cue(id, Ice, .1f, 3f, from, 1f), G(ctx, c - side * .8f) + up * .03f, up, 1f, .02f, 16); break;
                case 11:  // a frozen lake where the Water Bearer pours water that turns to ice in mid-air
                    mk.Prism(WorldMaterials.Solid(new Color(.75f, .86f, .95f), .9f), G(ctx, c) + up * .03f, up, 3.6f, .03f, 24);
                    var wb = c - face * 1.6f; var robe = WorldMaterials.Solid(new Color(.75f, .78f, .85f));
                    mk.Prism(robe, wb, up, .45f, 1.5f, 8, .55f); mk.Blob(robe, wb + up * 1.8f, Vector3.one * .25f, ctx.Rng, 0f); solid.Box(robe, wb + up * .9f, new Vector3(.9f, 1.8f, .9f), rot);
                    var urn = wb + up * 1.5f + face * .5f; mk.Prism(WorldMaterials.Solid(new Color(.6f, .55f, .5f)), urn, (face + up * .3f).normalized, .2f, .4f, 10, .6f);
                    for (var k = 0; k < 10; k++) { var t = k / 9f; var p = urn + face * (.4f + t * 1.2f) - up * (t * t * 1.4f); mk.Box(Cue(id, Color.Lerp(new Color(.3f, .6f, 1f), Color.white, t), .4f, 3f, from, 1f), p, new Vector3(.12f, .18f, .12f), Quaternion.Euler(k * 20f, k * 30f, 0)); }
                    break;
            }
            // each station's own warm-to-cold glow on the ground
            mk.Prism(Cue(id, col, .035f, 1.5f, from, to + .01f), G(ctx, c) + up * .025f, up, j == 11 ? 3.9f : 3.2f, .01f, 20, 1f, 7f);
        }

        void Bench(MeshKit mk, MeshKit solid, Vector3 at, Vector3 facing)
        {
            var rot = Quaternion.LookRotation(facing);
            mk.Box(darkWood, at + Vector3.up * .45f, new Vector3(1.7f, .08f, .45f), rot);
            mk.Box(darkWood, at + Vector3.up * .78f - facing * .2f, new Vector3(1.7f, .32f, .06f), rot);
            foreach (var s in new[] { -1f, 1f }) mk.Box(darkWood, at + rot * new Vector3(s * .7f, .22f, 0), new Vector3(.08f, .45f, .4f), rot);
            solid.Box(darkWood, at + Vector3.up * .45f, new Vector3(1.7f, .9f, .5f), rot);
        }
    }
}
