using System.Collections.Generic;
using UnityEngine;
using static EverythingWorld.World.PlaceTools;

namespace EverythingWorld.World
{
    /// The rest of the Moors' places (see MoorsPlaces):
    /// - Moor Halt on the east strip: a little platform with a shelter, a signal to pull, a suitcase someone left, and
    ///   a two-car train that comes, waits and goes (the same for everyone: it runs on the clock);
    /// - the lookout on the north-west headland: a trig point, a telescope to swing round, walkers with a flask;
    /// - a stone circle whose stones light when you touch them (light them all and they sing with light);
    /// - a waiting room with no building: rows of chairs on the moor, NOW SERVING 3, a ticket machine;
    /// - an office desk alone in the heather, its screen still on, paper planes to throw;
    /// - TEA. JUST TEA.: a tea van by the sea with tables, a kite flying over it;
    /// - a boardwalk through a marsh where pale lights drift (keep to the boards);
    /// - a lone phone box that rings: answer it;
    /// - the last bench, at the far east tip, facing the sea.
    public partial class MoorsKit
    {
        void MorePlaces(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            Halt(ctx, mk, solid); Lookout(ctx, mk, solid); StoneCircle(ctx, mk, solid); WaitingRoom(ctx, mk, solid); Desk(ctx, mk, solid);
            TeaVan(ctx, mk, solid); Bog(ctx, mk, solid); RingingPhone(ctx, mk, solid); LastBench(ctx, mk, solid);
            foreach (var s in marvelSpots) Lane(ctx, mk, s, 14f);   // every strange thing on the moor has a way to it
        }

        void SignBoard(LandContext ctx, MeshKit mk, Vector3 at, Vector3 face, string text, float w = 2.2f, float h = .7f, float size = .15f)
        {
            var up = Vector3.up; var g = ctx.OnGround(at.x, at.z); var rot = Quaternion.LookRotation(face);
            mk.Prism(white, g - up * .2f, up, .06f, 1.8f, 6); mk.Box(white, g + up * 1.65f, new Vector3(w, h, .06f), rot); ArtistBits.PostCollider(ctx.Root, g, 1.7f, .1f);
            ArtistBits.Label(ctx.Root, text, g + up * (1.65f + size * .1f) + face * .04f, face, size, new Color(.1f, .1f, .12f), TextAnchor.MiddleCenter);
        }

        // Moor Halt and its train
        void Halt(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; if (trackPts.Count < 6) return;
            // the track: ballast, sleepers, two rails
            var ballast = WorldMaterials.Solid(new Color(.5f, .48f, .45f)); Strip(mk, trackPts, ballast, 3f, .14f, ctx);
            for (var i = 1; i < trackPts.Count; i++)
            {
                var a = trackPts[i - 1]; var b = trackPts[i]; var d = b - a; if (d.sqrMagnitude < .01f || d.sqrMagnitude > 16f) continue; var rot = Quaternion.LookRotation(d); var side = Vector3.Cross(up, d.normalized);
                mk.Box(wood, (a + b) / 2f + up * .2f, new Vector3(2.4f, .1f, .3f), rot);
                solid.Box(wood, (a + b) / 2f + up * .05f, new Vector3(2.6f, .6f, d.magnitude + .1f), rot);   // the track bed is solid: you can walk the line
                foreach (var e in new[] { -.75f, .75f }) mk.Box(steel, (a + b) / 2f + side * e + up * .3f, new Vector3(.08f, .12f, d.magnitude + .02f), rot);
                Remember(a, 400);
            }
            foreach (var end in new[] { trackPts[0], trackPts[trackPts.Count - 1] }) { mk.Box(redPaint, end + up * .8f, new Vector3(2.6f, 1f, .4f), Quaternion.LookRotation(FaceTo(trackPts[0], trackPts[trackPts.Count - 1]))); Col(end + up * .7f, new Vector3(2.6f, 1.4f, .5f), Quaternion.LookRotation(FaceTo(trackPts[0], trackPts[trackPts.Count - 1]))); }
            // the platform beside the track, nearest the halt's site
            var want = ctx.OnGround(HaltAt.x, HaltAt.y); var on = Nearest(trackPts, want); var i0 = trackPts.IndexOf(on);
            var tan = trackPts[Mathf.Min(i0 + 2, trackPts.Count - 1)] - trackPts[Mathf.Max(i0 - 2, 0)]; tan.y = 0; tan.Normalize();
            var roadSide = FaceTo(on, Nearest(roadPts, on)); var across = Vector3.Cross(up, tan); if (Vector3.Dot(across, roadSide) < 0f) across = -across;
            var f = new PlaceFrame(on + across * 3.6f, -across);   // on the platform, facing the track; x along it
            const float L = 22f, W = 3.4f, PH = .9f;
            mk.Box(concrete, f.P(0, PH / 2f - .3f, 0), new Vector3(L, PH + .6f, W), f.r); solid.Box(concrete, f.P(0, PH / 2f - .3f, 0), new Vector3(L, PH + .6f, W), f.r);
            mk.Box(white, f.P(0, PH + .01f, W / 2f - .2f), new Vector3(L, .02f, .15f), f.r);
            foreach (var e in new[] { -1f, 1f }) for (var k = 0; k < 4; k++) { var st = f.P(e * (L / 2f + .35f + k * .5f), 0, 0); mk.Box(concrete, st + up * ((PH - k * .225f) / 2f - .3f), new Vector3(.5f, PH - k * .225f + .6f, W), f.r); solid.Box(concrete, st + up * ((PH - k * .225f) / 2f - .3f), new Vector3(.5f, PH - k * .225f + .6f, W), f.r); }
            // the shelter with its bench, the name board, lamps
            var sh = f.P(-3f, PH, -W / 2f + .5f); mk.Box(maroon, sh + up * 1.2f - f.Fwd * .3f, new Vector3(5f, 2.4f, .1f), f.r); foreach (var e in new[] { -2.5f, 2.5f }) mk.Box(maroon, sh + up * 1.2f + f.Right * e + f.Fwd * .2f, new Vector3(.1f, 2.4f, 1.1f), f.r); mk.Box(slate, sh + up * 2.45f + f.Fwd * .3f, new Vector3(5.4f, .1f, 1.8f), f.r);
            solid.Box(maroon, sh + up * 1.2f - f.Fwd * .3f, new Vector3(5f, 2.4f, .15f), f.r);
            mk.Box(wood, sh + up * .45f + f.Fwd * .1f, new Vector3(4.4f, .07f, .45f), f.r); Interact.Bench(plife.transform, sh + up * .5f + f.Fwd * .1f - f.Right * 2f, sh + up * .5f + f.Fwd * .1f + f.Right * 2f, f.Fwd);
            Col(sh + up * .25f + f.Fwd * .1f, new Vector3(4.4f, .5f, .45f), f.r);
            var nb = f.P(5f, PH, -W / 2f + .3f); foreach (var e in new[] { -1.2f, 1.2f }) mk.Box(white, nb + f.Right * e + up * .9f, new Vector3(.1f, 1.8f, .1f), f.r); mk.Box(maroon, nb + up * 1.5f, new Vector3(2.8f, .6f, .08f), f.r);
            ArtistBits.Label(ctx.Root, "MOOR HALT", nb + up * 1.5f + f.Fwd * .05f, f.Fwd, .3f, new Color(.95f, .92f, .8f), TextAnchor.MiddleCenter);
            foreach (var e in new[] { -8f, 8.5f }) { var lp = f.P(e, PH, -W / 2f + .3f); mk.Prism(steel, lp, up, .06f, 3.2f, 6); mk.Blob(GlowM(Lamp, 2.2f), lp + up * 3.3f, Vector3.one * .18f, ctx.Rng, 0f); halos.Add(lp + up * 3.3f, 1.3f, Lamp, .7f); ArtistBits.PostCollider(ctx.Root, lp, 3.2f, .08f); }
            ArtistBits.PointLight(ctx.Root, f.P(0, PH + 3f, 0), Lamp, 16f, 1.3f);
            // a signal you can pull; a suitcase someone left; the stationmaster; someone waiting
            var sg = f.P(L / 2f + 3.5f, 0, 1f); sg = ctx.OnGround(sg.x, sg.z); mk.Prism(white, sg - up * .2f, up, .1f, 5f, 6); ArtistBits.PostCollider(ctx.Root, sg, 4.6f, .12f);
            var arm = new MeshKit(); arm.Box(redPaint, new Vector3(.7f, 0, 0), new Vector3(1.4f, .28f, .05f), Quaternion.identity); arm.Box(white, new Vector3(1.1f, 0, .03f), new Vector3(.15f, .28f, .02f), Quaternion.identity);
            var ago = arm.BuildSingle("Signal arm", plife.transform, false); ago.transform.SetPositionAndRotation(sg + up * 4.5f, Quaternion.LookRotation(across));
            var lever = new GameObject("Signal lever"); lever.transform.SetParent(plife.transform, false); lever.transform.position = sg + up * 1.1f;
            var lt = Interact.Toy(lever, Toy.Kind.Custom, "Pull the signal", new Vector3(.6f, 1.6f, .6f)); var armToy = ago.AddComponent<Toy>(); armToy.kind = Toy.Kind.Swing; armToy.axis = Vector3.forward; armToy.amount = -45f; armToy.seconds = .6f; lt.onUse = t => armToy.Use(null);
            var caseM = new MeshKit(); caseM.Box(WorldMaterials.Solid(new Color(.5f, .32f, .18f)), new Vector3(0, .22f, 0), new Vector3(.6f, .42f, .2f), Quaternion.identity); caseM.Box(brass, new Vector3(0, .46f, 0), new Vector3(.2f, .04f, .04f), Quaternion.identity);
            var cgo = caseM.BuildSingle("Suitcase", plife.transform, false); cgo.transform.SetPositionAndRotation(f.P(1.2f, PH + .02f, -.2f), f.r); Interact.Carry(cgo, "the suitcase", 2f, on.y - 6f);
            Folk(f.P(7.5f, PH, .2f), f.Fwd, Resident.Pose.Stand, new[] { "Next train's in a bit. It's always in a bit.", "Platform one. There's only the one.", "Mind the gap. It's more of a suggestion." }, new Residents.Look { top = new Color(.15f, .18f, .3f), bottom = new Color(.15f, .18f, .3f), hat = Residents.Hat.Cap, hatColor = new Color(.15f, .18f, .3f), skin = new Color(.75f, .55f, .4f), hair = new Color(.2f, .15f, .1f) });
            Folk(sh + up * .5f + f.Fwd * .1f - f.Right * 1.2f, f.Fwd, Resident.Pose.Sit, new[] { "I'm not going anywhere. I just like the sound.", "That's not my suitcase. It's been here longer than me." }, null, null, .5f);
            // the train: two maroon and cream cars with lit windows (MoorsPlacesLife runs it)
            var train = new GameObject("Moor train").transform; train.SetParent(plife.transform, false);
            for (var k = 0; k < 2; k++)
            {
                var car = new MeshKit(); car.Box(maroon, new Vector3(0, 1.3f, 0), new Vector3(2.5f, 1.2f, 9.4f), Quaternion.identity); car.Box(cream, new Vector3(0, 2.3f, 0), new Vector3(2.5f, .9f, 9.4f), Quaternion.identity); car.Box(slate, new Vector3(0, 2.85f, 0), new Vector3(2.3f, .25f, 9.2f), Quaternion.identity);
                car.Box(dark, new Vector3(0, .5f, 0), new Vector3(2f, .5f, 8.4f), Quaternion.identity);
                foreach (var e in new[] { -1.26f, 1.26f }) for (var w = 0; w < 5; w++) car.Box(GlowM(new Color(1f, .9f, .65f), 1.6f), new Vector3(e, 2.3f, -3.4f + w * 1.7f), new Vector3(.02f, .6f, 1.2f), Quaternion.identity);
                car.Box(GlowM(k == 0 ? new Color(1f, .95f, .8f) : new Color(1f, .2f, .15f), 2.4f), new Vector3(0, 1.3f, k == 0 ? 4.71f : -4.71f), new Vector3(.5f, .3f, .02f), Quaternion.identity);
                var go = car.BuildSingle("Train car", train, false); go.transform.localPosition = new Vector3(0, 0, k == 0 ? 4.9f : -4.9f);
            }
            plife.train = train; plife.track = trackPts.ToArray(); plife.haltIndex = i0;
            Lane(ctx, mk, f.P(-L / 2f - 2.4f, 0, 0)); Ring(f.o, 14f);
            Shot(ctx, "Moor Halt", f.P(L / 2f + 4f, 2.6f, -9f), f.P(0, 1.6f, 1f));
        }

        void Lookout(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var g = Site(ctx, LookoutAt, 8f);
            var toSea = Vector3.zero; for (var a = 0; a < 360; a += 15) { var d = Quaternion.Euler(0, a, 0) * Vector3.forward; if (ctx.Ground(g.x + d.x * 110f, g.z + d.z * 110f) < .3f) toSea += d; }
            if (toSea.sqrMagnitude < .01f) toSea = Vector3.forward; var f = new PlaceFrame(g, toSea.normalized);
            mk.Prism(copeStone, g - up * .4f, up, 5.5f, .55f, 14); solid.Prism(copeStone, g - up * .4f, up, 5.5f, .55f, 14);
            var gy = g + up * .15f; var fo = new PlaceFrame(gy, f.Fwd);
            mk.Prism(white, fo.P(-2.4f, 0, -1.6f), up, .35f, 1.3f, 4, .7f); ArtistBits.PostCollider(ctx.Root, fo.P(-2.4f, 0, -1.6f), 1.3f, .35f);   // the trig point
            mk.Prism(stone, fo.P(0, 0, 2.4f), up, .5f, .9f, 10); mk.Prism(brass, fo.P(0, .9f, 2.4f), up, .65f, .05f, 16); ArtistBits.PostCollider(ctx.Root, fo.P(0, 0, 2.4f), .95f, .6f);   // the toposcope
            ArtistBits.Label(ctx.Root, "you are here\nso is the weather", fo.P(0, .96f, 2.4f), up, .08f, new Color(.2f, .15f, .05f), TextAnchor.MiddleCenter).transform.rotation = Quaternion.LookRotation(-up, f.Fwd);
            // the telescope: swing it round
            var tp = fo.P(2.6f, 0, 2f); mk.Prism(steel, tp, up, .06f, 1.3f, 6); ArtistBits.PostCollider(ctx.Root, tp, 1.3f, .1f);
            var tel = new MeshKit(); tel.Prism(brass, new Vector3(0, 0, -.5f), Vector3.forward, .09f, 1f, 8, .8f); tel.Prism(dark, new Vector3(0, 0, .5f), Vector3.forward, .1f, .08f, 8);
            var tgo = tel.BuildSingle("Telescope", plife.transform, false); tgo.transform.SetPositionAndRotation(tp + up * 1.4f, Quaternion.LookRotation(f.Fwd - up * .1f));
            var tt = Interact.Toy(tgo, Toy.Kind.Spin, "Swing the telescope", new Vector3(.5f, .5f, 1.2f)); tt.amount = 360f; tt.seconds = 3f;
            // a stone bench facing the sea, a cairn with a lantern, two walkers
            mk.Box(stone, fo.P(-.4f, .25f, -2.6f), new Vector3(2.6f, .5f, .6f), f.r); Col(fo.P(-.4f, .25f, -2.6f), new Vector3(2.6f, .5f, .6f), f.r); Interact.Bench(plife.transform, fo.P(-1.5f, .5f, -2.6f), fo.P(.7f, .5f, -2.6f), f.Fwd);
            var cairn = fo.P(-4f, 0, 2.6f); cairn = ctx.OnGround(cairn.x, cairn.z); for (var k = 0; k < 5; k++) mk.Blob(k % 2 == 0 ? stone : paleStone, cairn + up * (.3f + k * .35f), new Vector3(.8f - k * .12f, .25f, .8f - k * .12f), ctx.Rng, .3f); mk.Blob(GlowM(Lamp, 2.4f), cairn + up * 2.1f, Vector3.one * .15f, ctx.Rng, 0f);
            Col(cairn + up * 1f, new Vector3(1.4f, 2f, 1.4f), Quaternion.identity); halos.Add(cairn + up * 2.1f, 1.4f, Lamp, .7f); ArtistBits.PointLight(ctx.Root, cairn + up * 2.6f, Lamp, 12f, 1.2f);
            var rain = new Residents.Look { top = new Color(1f, .82f, .15f), bottom = new Color(.2f, .22f, .28f), hat = Residents.Hat.Beanie, hatColor = new Color(.6f, .15f, .15f), skin = new Color(.9f, .72f, .6f), hair = new Color(.5f, .3f, .15f) };
            Folk(fo.P(1.5f, 0, 3.6f), f.Fwd, Resident.Pose.Stand, new[] { "On a clear day you can see the weather coming.", "Four hours' walk for this. Worth every blister." }, rain);
            Folk(fo.P(.4f, .5f, -2.6f), f.Fwd, Resident.Pose.Sit, new[] { "Flask of tea. Want some? It's mostly rain now.", "I come up to think. Then I don't. That's the good part." }, Look(Residents.Tool.Mug), null, .5f);
            Lane(ctx, mk, fo.P(0, 0, -6.5f)); Ring(g, 7f);
            Shot(ctx, "the lookout", fo.P(-7f, 2.2f, -9f), fo.P(1f, 1f, 3f));
        }

        void StoneCircle(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var g = Site(ctx, CircleAt, 12f); const int N = 9; const float R = 8.5f;
            for (var k = 0; k < N; k++)
            {
                var a = k * Mathf.PI * 2f / N; var d = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)); var p = ctx.OnGround(g.x + d.x * R, g.z + d.z * R); var h = 2.4f + (k * 7 % 5) * .35f;
                var glow = new Material(WorldMaterials.Glow(new Color(.55f, .8f, 1f), .25f)) { name = "Circle stone" };
                var st = new MeshKit(); st.Box(paleStone, new Vector3(0, h / 2f - .3f, 0), new Vector3(1.2f, h + .6f, .7f), Quaternion.Euler((k * 13 % 7) - 3f, 0, (k * 29 % 9) - 4f));
                st.Box(glow, new Vector3(0, h * .55f, -.37f), new Vector3(.5f, h * .5f, .02f), Quaternion.Euler((k * 13 % 7) - 3f, 0, (k * 29 % 9) - 4f));
                var go = st.BuildSingle("Standing stone", plife.transform, false); go.transform.SetPositionAndRotation(p, Quaternion.LookRotation(d));
                var bc = go.AddComponent<BoxCollider>(); bc.center = new Vector3(0, h / 2f, 0); bc.size = new Vector3(1.2f, h, .7f);
                var t = go.AddComponent<Toy>(); t.kind = Toy.Kind.Glow; t.verb = "Touch the stone"; t.glow = glow; t.glowColor = new Color(.55f, .8f, 1f) * .6f; plife.stones.Add((t, glow));
            }
            mk.Box(copeStone, g + up * .3f, new Vector3(2.4f, .6f, 1.3f), Quaternion.Euler(0, 30f, 0)); Col(g + up * .3f, new Vector3(2.4f, .6f, 1.3f), Quaternion.Euler(0, 30f, 0));
            Interact.Bench(plife.transform, g + up * .62f + Quaternion.Euler(0, 30f, 0) * new Vector3(-.8f, 0, 0), g + up * .62f + Quaternion.Euler(0, 30f, 0) * new Vector3(.8f, 0, 0), Quaternion.Euler(0, 30f, 0) * Vector3.forward);
            plife.circleLight = ArtistBits.PointLight(ctx.Root, g + up * 3f, new Color(.55f, .8f, 1f), 22f, .4f);
            Folk(ctx.OnGround(g.x + R + 2.5f, g.z + 1f), FaceTo(new Vector3(g.x + R + 2.5f, 0, g.z + 1f), g), Resident.Pose.Lean, new[] { "They hum if you touch them. Nobody knows the tune.", "Nine stones. Ten on a Tuesday. Don't count on a Tuesday.", "Light them all and something happens. Something nice, mind." });
            Lane(ctx, mk, ctx.OnGround(g.x + R + 4f, g.z)); Ring(g, R + 2f);
            Shot(ctx, "the stone circle", ctx.OnGround(g.x + 15f, g.z - 9f) + up * 2.2f, g + up * 1.5f);
        }

        void WaitingRoom(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var g = Site(ctx, WaitingAt, 10f); var f = new PlaceFrame(g, FaceTo(g, Nearest(roadPts, g)));
            var lo = g.y; foreach (var (x, z) in new[] { (-4f, -4f), (4f, -4f), (4f, 4f), (-4f, 4f) }) lo = Mathf.Min(lo, ctx.Ground(f.P(x, 0, z).x, f.P(x, 0, z).z));
            var top = g.y + .15f; mk.Box(WorldMaterials.Solid(new Color(.78f, .8f, .76f)), new Vector3(g.x, (lo + top) / 2f - .2f, g.z), new Vector3(9f, top - lo + .4f, 9f), f.r); solid.Box(concrete, new Vector3(g.x, (lo + top) / 2f - .2f, g.z), new Vector3(9f, top - lo + .4f, 9f), f.r);
            var fo = new PlaceFrame(new Vector3(g.x, top, g.z), f.Fwd);
            for (var x = 0; x < 9; x++) for (var z = 0; z < 9; z++) if ((x + z) % 2 == 0) mk.Box(WorldMaterials.Solid(new Color(.6f, .68f, .66f)), fo.P(-4f + x, .006f, -4f + z), new Vector3(1f, .01f, 1f), f.r);
            // three rows of plastic chairs facing the number
            var chairC = new[] { new Color(.9f, .5f, .15f), new Color(.25f, .45f, .6f) };
            for (var row = 0; row < 3; row++)
                for (var k = 0; k < 4; k++)
                {
                    var c = fo.P(-2.1f + k * 1.4f - (k > 1 ? -.4f : .4f) * 0f, 0, -2.6f + row * 1.6f); var m = WorldMaterials.Solid(chairC[(row + k) % 2]);
                    mk.Box(m, c + up * .45f, new Vector3(.5f, .06f, .5f), f.r); mk.Box(m, c + up * .8f - f.Fwd * .24f, new Vector3(.5f, .5f, .05f), f.r); foreach (var e in new[] { -.2f, .2f }) mk.Box(steel, c + up * .22f + f.Right * e, new Vector3(.04f, .44f, .44f), f.r);
                    Col(c + up * .5f - f.Fwd * .05f, new Vector3(.5f, 1f, .5f), f.r); Interact.Seat(plife.transform, c + up * .5f, f.Fwd, "Take a seat");
                }
            // NOW SERVING, on a pole; a ticket machine; a water cooler; a pot plant; a strip light on two posts
            var np = fo.P(0, 0, 3.6f); mk.Prism(steel, np, up, .06f, 2.6f, 6); mk.Box(dark, np + up * 2.5f, new Vector3(2.4f, 1f, .15f), f.r); ArtistBits.PostCollider(ctx.Root, np, 2.6f, .1f);
            ArtistBits.Label(ctx.Root, "NOW SERVING", np + up * 2.78f - f.Fwd * .09f, -f.Fwd, .14f, new Color(1f, .5f, .2f), TextAnchor.MiddleCenter);
            plife.serving = ArtistBits.Label(ctx.Root, "3", np + up * 2.36f - f.Fwd * .09f, -f.Fwd, .42f, new Color(1f, .3f, .15f), TextAnchor.MiddleCenter);
            var tm = fo.P(3.4f, 0, -3.6f); mk.Box(redPaint, tm + up * .6f, new Vector3(.4f, 1.2f, .4f), f.r); Col(tm + up * .6f, new Vector3(.4f, 1.2f, .4f), f.r);
            var tgo = new GameObject("Ticket machine"); tgo.transform.SetParent(plife.transform, false); tgo.transform.position = tm + up * 1f;
            var tt = Interact.Toy(tgo, Toy.Kind.Custom, "Take a ticket", new Vector3(.6f, 1.2f, .6f)); var n = 0; var tickets = new[] { "Your number is 47.", "Your number is 48. Don't lose it.", "Your number is 3. Oh. That's now.\n...No, false alarm.", "Out of tickets. Please wait." };
            tt.onUse = t => plife.Say(tm + up * 1.9f, tickets[n++ % tickets.Length]);
            var wc = fo.P(-3.6f, 0, -3.6f); mk.Box(white, wc + up * .5f, new Vector3(.4f, 1f, .4f), f.r); mk.Prism(WorldMaterials.Glow(new Color(.5f, .75f, 1f), .7f), wc + up * 1f, up, .17f, .5f, 10); Col(wc + up * .75f, new Vector3(.4f, 1.5f, .4f), f.r);
            var pp = fo.P(-3.6f, 0, 3.4f); mk.Prism(WorldMaterials.Solid(new Color(.6f, .35f, .25f)), pp, up, .25f, .4f, 8, 1.2f); soft.Blob(WorldMaterials.Solid(new Color(.25f, .5f, .25f)), pp + up * .9f, new Vector3(.4f, .6f, .4f), ctx.Rng, .3f); ArtistBits.PostCollider(ctx.Root, pp, .5f, .3f);
            foreach (var e in new[] { -4.3f, 4.3f }) { mk.Prism(steel, fo.P(e, 0, 0), up, .05f, 3.2f, 6); ArtistBits.PostCollider(ctx.Root, fo.P(e, 0, 0), 3.2f, .08f); }
            mk.Box(steel, fo.P(0, 3.2f, 0), new Vector3(8.7f, .06f, .12f), f.r); mk.Box(GlowM(new Color(.9f, .97f, 1f), 2.2f), fo.P(0, 3.12f, 0), new Vector3(5f, .08f, .2f), f.r);
            plife.strip = ArtistBits.PointLight(ctx.Root, fo.P(0, 2.8f, 0), new Color(.9f, .97f, 1f), 12f, 1.6f);
            Folk(fo.P(-2.1f, .5f, -2.6f + 1.6f), f.Fwd, Resident.Pose.Sit, new[] { "I'm number four. It's been on three a while.", "They'll call you. Probably.", "Lovely waiting room. No walls, but the chairs are good." }, null, null, .5f);
            Folk(fo.P(-2.1f + 2.8f, .5f, -2.6f), f.Fwd, Resident.Pose.Sit, new[] { "I forget what I'm waiting for. It'll come to me. That's the idea." }, null, null, .5f);
            Lane(ctx, mk, fo.P(0, 0, 7f)); Ring(g, 7f);
            Shot(ctx, "the waiting room", fo.P(6f, 2f, -8f), fo.P(0, 1.2f, 1f));
        }

        void Desk(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var g = Site(ctx, DeskAt, 10f); var f = new PlaceFrame(g, FaceTo(g, Nearest(roadPts, g)));
            var fo = new PlaceFrame(g + up * .1f, f.Fwd);
            mk.Box(WorldMaterials.Solid(new Color(.3f, .38f, .5f)), g + up * .02f, new Vector3(5f, .16f, 4.4f), f.r);   // a square of office carpet
            foreach (var (c, s) in new[] { (fo.P(0, .8f, -2.1f), new Vector3(5f, 1.6f, .1f)), (fo.P(-2.45f, .8f, -.6f), new Vector3(.1f, 1.6f, 3f)) }) { mk.Box(WorldMaterials.Solid(new Color(.7f, .72f, .7f)), c, s, f.r); solid.Box(concrete, c, s, f.r); }
            mk.Box(wood, fo.P(-.6f, .74f, -1.5f), new Vector3(2.6f, .06f, 1f), f.r); foreach (var e in new[] { -1.8f, .6f }) mk.Box(steel, fo.P(e, .37f, -1.5f), new Vector3(.06f, .74f, .9f), f.r); Col(fo.P(-.6f, .4f, -1.5f), new Vector3(2.6f, .8f, 1f), f.r);
            mk.Box(cream, fo.P(-.8f, 1.05f, -1.75f), new Vector3(.6f, .5f, .5f), f.r); mk.Box(GlowM(new Color(.4f, 1f, .5f), 1.3f), fo.P(-.8f, 1.07f, -1.49f), new Vector3(.46f, .36f, .02f), f.r);
            plife.screen = ArtistBits.Label(ctx.Root, "0 1 1 0\n1 0 0 1", fo.P(-.8f, 1.08f, -1.47f), f.Fwd, .07f, new Color(.05f, .2f, .08f), TextAnchor.MiddleCenter);
            mk.Box(cream, fo.P(-.8f, .79f, -1.2f), new Vector3(.5f, .03f, .18f), f.r); mk.Box(dark, fo.P(.2f, .8f, -1.6f), new Vector3(.22f, .08f, .25f), f.r);
            mk.Prism(dark, fo.P(-1.6f, .77f, -1.7f), up, .1f, .03f, 8); mk.Prism(steel, fo.P(-1.6f, .77f, -1.7f), (up + f.Right * .4f).normalized, .02f, .5f, 6); mk.Blob(GlowM(Lamp, 2.2f), fo.P(-1.4f, 1.25f, -1.7f), Vector3.one * .1f, ctx.Rng, 0f); halos.Add(fo.P(-1.4f, 1.25f, -1.7f), .9f, Lamp, .6f);
            ArtistBits.PointLight(ctx.Root, fo.P(-.8f, 1.8f, -.6f), new Color(.8f, 1f, .85f), 9f, 1.4f);
            // the swivel chair (sit at the desk), a filing cabinet, a coat stand, a water cooler-less corner
            var ch = fo.P(-.8f, 0, -.5f); mk.Prism(dark, ch, up, .3f, .05f, 5); mk.Prism(steel, ch, up, .04f, .45f, 6); mk.Box(dark, ch + up * .47f, new Vector3(.5f, .08f, .5f), f.r); mk.Box(dark, ch + up * .85f + f.Fwd * .24f, new Vector3(.46f, .6f, .06f), f.r);
            Interact.Seat(plife.transform, ch + up * .52f, -f.Fwd, "Sit at the desk");
            var cab = fo.P(1.8f, 0, -1.6f); mk.Box(steel, cab + up * .65f, new Vector3(.5f, 1.3f, .6f), f.r); for (var k = 0; k < 3; k++) mk.Box(dark, cab + up * (.25f + k * .42f) + f.Fwd * .31f, new Vector3(.2f, .03f, .02f), f.r); Col(cab + up * .65f, new Vector3(.5f, 1.3f, .6f), f.r);
            var cs = fo.P(2f, 0, .8f); mk.Prism(wood, cs, up, .04f, 1.8f, 6); mk.Box(new Material(WorldMaterials.Solid(new Color(.5f, .5f, .52f))), cs + up * 1.3f + f.Right * .15f, new Vector3(.12f, .9f, .4f), f.r); ArtistBits.PostCollider(ctx.Root, cs, 1.8f, .12f);
            // paper planes on the desk: pick one up and throw it
            for (var k = 0; k < 4; k++)
            {
                var pm = new MeshKit(); pm.Tri(white, new Vector3(0, 0, .16f), new Vector3(-.1f, 0, -.1f), new Vector3(.1f, 0, -.1f), Vector3.down); pm.Tri(white, new Vector3(0, 0, .16f), new Vector3(.1f, 0, -.1f), new Vector3(-.1f, 0, -.1f), Vector3.up); pm.Box(white, new Vector3(0, -.02f, 0), new Vector3(.01f, .04f, .24f), Quaternion.identity);
                var go = pm.BuildSingle("Paper plane", plife.transform, false); go.transform.SetPositionAndRotation(fo.P(-.1f + k * .22f, .8f, -1.25f), Quaternion.Euler(0, k * 30f, 0));
                var c = Interact.Carry(go, "a paper plane", .03f, g.y - 6f); var rb = c.GetComponent<Rigidbody>(); rb.linearDamping = 1.2f; rb.useGravity = true; c.GetComponent<BoxCollider>().size = new Vector3(.2f, .06f, .28f);
            }
            Folk(fo.P(1.2f, 0, 1.4f), -f.Fwd + f.Right * -.3f, Resident.Pose.Stand, new[] { "Just finishing something. Have been since 1997.", "The view's better than the old office. No ceiling tiles.", "My inbox? The wind takes most of it." }, new Residents.Look { top = new Color(.9f, .9f, .92f), bottom = new Color(.25f, .26f, .3f), skin = new Color(.9f, .72f, .6f), hair = new Color(.3f, .2f, .12f), tool = Residents.Tool.Mug });
            Lane(ctx, mk, fo.P(0, 0, 3.4f)); Ring(g, 5f);
            Shot(ctx, "the desk on the moor", fo.P(4f, 1.9f, 5f), fo.P(-.6f, .9f, -1.2f));
        }

        void TeaVan(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var g = Site(ctx, TeaAt, 6f);
            var toSea = Vector3.zero; for (var a = 0; a < 360; a += 15) { var d = Quaternion.Euler(0, a, 0) * Vector3.forward; if (ctx.Ground(g.x + d.x * 45f, g.z + d.z * 45f) < .3f) toSea += d; }
            if (toSea.sqrMagnitude < .01f) toSea = Vector3.forward; var f = new PlaceFrame(g, toSea.normalized);
            mk.Prism(WorldMaterials.Solid(new Color(.62f, .6f, .55f)), g - up * .35f, up, 10f, .45f, 18);   // a gravel pull-in
            // the van, side-on to the sea, its hatch open under an awning
            var v = ctx.OnGround(f.P(-5f, 0, -3f).x, f.P(-5f, 0, -3f).z) + up * .1f; var vr = Quaternion.LookRotation(f.Right); var green = WorldMaterials.Solid(new Color(.3f, .5f, .4f));
            mk.Box(cream, v + up * 1.9f, new Vector3(2.1f, 1.6f, 4.6f), vr); mk.Box(green, v + up * .78f, new Vector3(2.12f, .6f, 4.62f), vr); mk.Box(green, v + up * 1.15f + f.Right * 2.95f, new Vector3(1.9f, 1.3f, 1.2f), vr);
            foreach (var z in new[] { -1.5f, 2.6f }) foreach (var e in new[] { -1f, 1f }) mk.Prism(dark, v + f.Right * z + f.Fwd * e * 1f + up * .4f, f.Fwd * e, .4f, .2f, 10);
            mk.Box(GlowM(new Color(1f, .9f, .65f), 1.5f), v + up * 1.9f + f.Fwd * 1.06f, new Vector3(2.4f, 1f, .04f), f.r); mk.Box(wood, v + up * 1.35f + f.Fwd * 1.3f, new Vector3(2.6f, .06f, .5f), f.r);
            for (var k = 0; k < 5; k++) mk.Box(k % 2 == 0 ? green : cream, v + up * 2.75f + f.Fwd * 1.7f + f.Right * (-1.04f + k * .52f), new Vector3(.52f, .06f, 1.4f), f.r * Quaternion.Euler(12f, 0, 0));
            solid.Box(cream, v + up * 1.35f + f.Right * .5f, new Vector3(2.1f, 2.7f, 6f), vr);
            ArtistBits.Label(ctx.Root, "TEA. JUST TEA.", v + up * 2.45f + f.Fwd * 1.07f, f.Fwd, .22f, new Color(.2f, .35f, .28f), TextAnchor.MiddleCenter);
            Folk(v + f.Fwd * 2.1f + f.Right * 1.9f, f.Fwd, Resident.Pose.Lean, new[] { "Tea. Just tea. We tried coffee once.", "Milk's in. Sugar's a personal matter.", "Biscuits are for regulars. You can be a regular. Takes about a minute." }, Look(Residents.Tool.Mug, true));
            ArtistBits.PointLight(ctx.Root, v + up * 2.4f + f.Fwd * 2.6f, new Color(1f, .9f, .65f), 12f, 1.3f);
            // folding tables and chairs looking out to sea
            for (var k = 0; k < 3; k++)
            {
                var t = ctx.OnGround(f.P(-1f + k * 3.6f, 0, 4f).x, f.P(-1f + k * 3.6f, 0, 4f).z) + up * .1f; mk.Prism(white, t + up * .7f, up, .5f, .04f, 10); mk.Prism(steel, t, up, .04f, .7f, 6); ArtistBits.PostCollider(ctx.Root, t, .75f, .5f);
                mk.Box(white, t + up * .78f, new Vector3(.1f, .1f, .1f), f.r);
                foreach (var e in new[] { -1f, 1f }) { var c = t + f.Right * e * .95f - f.Fwd * .2f; var cm = WorldMaterials.Solid(e < 0 ? new Color(.3f, .5f, .4f) : new Color(.85f, .3f, .25f)); mk.Box(cm, c + up * .45f, new Vector3(.45f, .05f, .45f), f.r); mk.Box(cm, c + up * .75f - f.Fwd * .22f, new Vector3(.45f, .45f, .04f), f.r); Col(c + up * .45f - f.Fwd * .05f, new Vector3(.45f, .9f, .5f), f.r); Interact.Seat(plife.transform, c + up * .5f, f.Fwd, "Sit with a tea"); }
                if (k == 1) Folk(t + f.Right * .95f - f.Fwd * .2f + up * .5f, f.Fwd, Resident.Pose.Sit, new[] { "Best view on the moor. Don't tell the lookout.", "I come for the tea. I stay because standing up's a decision." }, Look(Residents.Tool.Mug), null, .5f);
            }
            // someone flying a kite
            var kf = ctx.OnGround(f.P(9f, 0, -1f).x, f.P(9f, 0, -1f).z); var flyer = Folk(kf, f.Fwd, Resident.Pose.Stand, new[] { "She's up. Been up since Tuesday. I daren't let go.", "Kite flying's ninety percent standing. I'm very good at standing." });
            var kite = new MeshKit(); kite.Tri(redPaint, new Vector3(0, .7f, 0), new Vector3(-.45f, 0, 0), new Vector3(.45f, 0, 0), Vector3.back); kite.Tri(GlowM(Signal, 1f), new Vector3(0, -.9f, 0), new Vector3(.45f, 0, 0), new Vector3(-.45f, 0, 0), Vector3.back);
            kite.Tri(redPaint, new Vector3(0, .7f, 0), new Vector3(.45f, 0, 0), new Vector3(-.45f, 0, 0), Vector3.forward); kite.Tri(GlowM(Signal, 1f), new Vector3(0, -.9f, 0), new Vector3(-.45f, 0, 0), new Vector3(.45f, 0, 0), Vector3.forward);
            var kgo = kite.BuildSingle("Kite", plife.transform, false); plife.kite = kgo.transform; plife.kiteHand = kf + up * 1.2f; plife.kiteWind = (f.Right + f.Fwd * .4f).normalized;
            var str = GameObject.CreatePrimitive(PrimitiveType.Cube); Object.Destroy(str.GetComponent<Collider>()); str.name = "Kite string"; str.transform.SetParent(plife.transform, false); str.GetComponent<Renderer>().sharedMaterial = white; plife.kiteString = str.transform;
            Lane(ctx, mk, f.P(0, 0, -10f)); Ring(g, 11f);
            Shot(ctx, "the tea van by the sea", f.P(8f, 2f, -10f), f.P(-1f, 1.4f, 2f));
        }

        void Bog(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var g = Site(ctx, BogAt, 5f); var f = new PlaceFrame(g, FaceTo(Nearest(roadPts, g), g));
            // dark pools among the reeds, and a zigzag boardwalk across
            var water = WorldMaterials.Glow(new Color(.12f, .2f, .28f), .35f); var reed = WorldMaterials.Solid(new Color(.5f, .52f, .3f));
            for (var k = 0; k < 9; k++) { var p = f.P((k * 37 % 30) - 15f, 0, 4f + (k * 53 % 32)); p = ctx.OnGround(p.x, p.z); if (!ctx.IsLand(p.x, p.z, 3f)) continue; mk.Prism(water, p + up * .04f, up, 2.5f + (k % 3), .03f, 10); for (var r = 0; r < 6; r++) { var q = p + Quaternion.Euler(0, r * 60f + k * 20f, 0) * Vector3.forward * (3f + (k % 3)); soft.Prism(reed, ctx.OnGround(q.x, q.z), up, .05f, 1.6f, 4); soft.Box(WorldMaterials.Solid(new Color(.35f, .25f, .15f)), ctx.OnGround(q.x, q.z) + up * 1.6f, new Vector3(.1f, .35f, .1f), Quaternion.identity); } }
            var way = new List<Vector3>(); for (var k = 0; k <= 8; k++) way.Add(f.P((k % 2 == 0 ? -3f : 3f), 0, k * 5f));
            for (var k = 0; k + 1 < way.Count; k++)
            {
                var a = way[k]; var b = way[k + 1]; if (!ctx.IsLand(b.x, b.z, 1f)) break;
                var ga = Mathf.Max(ctx.Ground(a.x, a.z), 1.6f) + .45f; var gb = Mathf.Max(ctx.Ground(b.x, b.z), 1.6f) + .45f; a.y = ga; b.y = gb; var d = b - a; var rot = Quaternion.LookRotation(d);
                mk.Box(wood, (a + b) / 2f, new Vector3(1.8f, .1f, d.magnitude + 1.6f), rot); solid.Box(wood, (a + b) / 2f - up * .15f, new Vector3(1.8f, .4f, d.magnitude + 1.6f), rot);
                foreach (var t in new[] { .1f, .9f }) foreach (var e in new[] { -1f, 1f }) { var p = Vector3.Lerp(a, b, t) + Vector3.Cross(up, d.normalized) * e * .8f; mk.Box(dark, p - up * .5f, new Vector3(.12f, 1.1f, .12f), rot); }
                Remember(a, 400); Remember((a + b) / 2f, 400); Remember(b, 400);
                if (k == 4) { var bp = (a + b) / 2f; mk.Box(wood, bp + up * .5f + Vector3.Cross(up, d.normalized) * .55f, new Vector3(.4f, .07f, 1.6f), rot); Interact.Bench(plife.transform, bp + up * .55f + Vector3.Cross(up, d.normalized) * .55f - d.normalized * .6f, bp + up * .55f + Vector3.Cross(up, d.normalized) * .55f + d.normalized * .6f, -Vector3.Cross(up, d.normalized)); }
            }
            // will-o'-the-wisps: pale lights that drift over the pools
            for (var k = 0; k < 6; k++)
            {
                var wm = new MeshKit(); wm.Blob(GlowM(k % 2 == 0 ? new Color(.6f, 1f, .8f) : new Color(.7f, .85f, 1f), 2.6f), Vector3.zero, Vector3.one * .16f, prng, .1f);
                var go = wm.BuildSingle("Wisp", plife.transform, false); var home = f.P((k * 41 % 26) - 13f, 1.2f, 6f + (k * 29 % 30)); home = new Vector3(home.x, Mathf.Max(ctx.Ground(home.x, home.z), 1.5f) + 1.1f, home.z); go.transform.position = home;
                if (k < 3) { var l = ArtistBits.PointLight(go.transform, home, k % 2 == 0 ? new Color(.6f, 1f, .8f) : new Color(.7f, .85f, 1f), 9f, 1.1f); l.transform.localPosition = Vector3.zero; }
                plife.wisps.Add((go.transform, home, k * 1.7f));
            }
            SignBoard(ctx, mk, f.P(-3.4f, 0, -2.5f), -f.Fwd, "KEEP TO THE BOARDS\n(the lights lie)", 2.4f, .7f, .13f);
            Folk(f.P(3.2f, 0, -2f), f.Fwd, Resident.Pose.Stand, new[] { "Seen a bittern. Or a carrier bag. Either's good.", "Follow the lights. Or don't. They don't mind.", "Third year of looking. I've nearly seen one." }, Look());
            Lane(ctx, mk, f.P(0, 0, -3f)); Ring(f.P(0, 0, 18f), 22f);
            Shot(ctx, "the marsh of lights", f.P(-5f, 2.2f, -7f), f.P(0, .8f, 16f));
        }

        void RingingPhone(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var g = Site(ctx, PhoneAt, 8f); var f = new PlaceFrame(g, FaceTo(g, Nearest(roadPts, g)));
            PhoneBox(ctx, mk, solid, g, f.Fwd);
            var ringM = new Material(WorldMaterials.Glow(new Color(1f, .9f, .6f), 1f)) { name = "Ringing" }; mk.Prism(ringM, g + up * 3.05f, up, .2f, .12f, 8); plife.ringM = ringM; plife.ringLight = ArtistBits.PointLight(ctx.Root, g + up * 3.4f, new Color(1f, .9f, .6f), 12f, 1f);
            var tgo = new GameObject("The phone"); tgo.transform.SetParent(plife.transform, false); tgo.transform.position = g + up * 1.3f + f.Fwd * .7f;
            var t = Interact.Toy(tgo, Toy.Kind.Custom, "Answer the phone", new Vector3(1.2f, 2.2f, .8f)); var n = 0;
            var calls = new[] { "\"...\"\nNobody there. Again.", "A voice says: \"It's due.\"\nThen the line goes.", "Just the wind. It sounds interested, though.", "\"Is that you?\" it asks.\nYou're not sure. You say yes.", "The speaking clock.\nIt's the same time it was." };
            t.onUse = _ => plife.Answer(g + up * 3.3f, calls[n++ % calls.Length]);
            LampBench(ctx, mk, f.P(0, 0, 6f), -f.Fwd);
            Lane(ctx, mk, f.P(0, 0, 3f)); Ring(g, 6f);
            Shot(ctx, "the phone box that rings", f.P(5f, 1.8f, 9f), g + up * 1.6f);
        }

        void LastBench(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var g = Site(ctx, EastEndAt, 3f);
            var toSea = Vector3.zero; for (var a = 0; a < 360; a += 15) { var d = Quaternion.Euler(0, a, 0) * Vector3.forward; if (ctx.Ground(g.x + d.x * 40f, g.z + d.z * 40f) < .3f) toSea += d; }
            if (toSea.sqrMagnitude < .01f) toSea = Vector3.right; var f = new PlaceFrame(g, toSea.normalized);
            LampBench(ctx, mk, g, f.Fwd);
            var pl = ctx.OnGround(f.P(-1.8f, 0, .4f).x, f.P(-1.8f, 0, .4f).z); mk.Box(copeStone, pl + up * .3f, new Vector3(.7f, .6f, .3f), f.r); mk.Box(brass, pl + up * .45f - f.Fwd * .16f, new Vector3(.5f, .25f, .02f), f.r); Col(pl + up * .3f, new Vector3(.7f, .6f, .3f), f.r);
            ArtistBits.Label(ctx.Root, "for everyone\nwho sat here", pl + up * .46f - f.Fwd * .18f, -f.Fwd, .05f, new Color(.2f, .15f, .05f), TextAnchor.MiddleCenter);
            Ring(g, 4f);
            Shot(ctx, "the last bench", f.P(-4f, 1.9f, -6f), g + up * .8f + f.Fwd * 6f);
        }
    }

    /// The Moors places' moving parts: the train on its clock, the chapel bell, the stone circle, the ringing phone,
    /// the waiting room's flickering light, the desk's screen, the kite, the wisps, and words that appear a while.
    public class MoorsPlacesLife : MonoBehaviour
    {
        public Transform train; public Vector3[] track = new Vector3[0]; public int haltIndex;
        public List<(Toy t, Material m)> stones = new List<(Toy, Material)>(); public Light circleLight, strip, ringLight;
        public Material ringM; public TextMesh serving, screen;
        public Transform kite, kiteString; public Vector3 kiteHand, kiteWind;
        public List<(Transform t, Vector3 home, float seed)> wisps = new List<(Transform, Vector3, float)>();

        float[] len = new float[0]; float total, haltAt; Transform bell; float bellT = -1f; Quaternion bellRest;
        TextMesh words; float wordsFor; float ringUntil, nextRing = 8f, screenT;
        static readonly Color StoneBlue = new Color(.55f, .8f, 1f);

        void Start()
        {
            if (track.Length > 1)
            {
                len = new float[track.Length]; for (var i = 1; i < track.Length; i++) { total += Vector3.Distance(track[i], track[i - 1]); len[i] = total; }
                haltAt = len[Mathf.Clamp(haltIndex, 0, track.Length - 1)];
            }
        }

        public void Ring(Transform b) { if (bell == null) { bell = b; bellRest = b.rotation; } bellT = 0f; }

        public void Say(Vector3 at, string text)
        {
            if (words == null) { words = ArtistBits.Label(transform, "", at, Vector3.forward, .16f, new Color(1f, 1f, .95f), TextAnchor.LowerCenter); words.gameObject.AddComponent<Billboard>(); }
            words.transform.position = at; words.text = text; words.gameObject.SetActive(true); wordsFor = 5.5f;
        }

        public void Answer(Vector3 at, string text) { Say(at, text); ringUntil = 0f; nextRing = Time.time + Random.Range(25f, 50f); }

        Vector3 OnTrack(float d, out Vector3 dir)
        {
            d = Mathf.Clamp(d, 0f, total - .01f); var i = 1; while (i < len.Length - 1 && len[i] < d) i++;
            var t = Mathf.InverseLerp(len[i - 1], len[i], d); dir = (track[i] - track[i - 1]).normalized; return Vector3.Lerp(track[i - 1], track[i], t);
        }

        void Update()
        {
            var t = Time.time; var dt = Time.deltaTime;
            // the train: out to one end, back to the halt, wait, on to the other end, wait; on the wall clock, so everyone sees it in the same place
            if (train != null && total > 30f)
            {
                const float Cycle = 150f; var c = (float)(System.DateTime.UtcNow.TimeOfDay.TotalSeconds % Cycle);
                float margin = 11f, a = margin, b = total - margin, h = Mathf.Clamp(haltAt, a + 5f, b - 5f), d;
                float Ease(float x) => x * x * (3f - 2f * x);
                if (c < 20f) d = a;                                             // waiting at the west end
                else if (c < 45f) d = Mathf.Lerp(a, h, Ease((c - 20f) / 25f));  // to the halt
                else if (c < 65f) d = h;                                        // at the platform
                else if (c < 90f) d = Mathf.Lerp(h, b, Ease((c - 65f) / 25f));  // to the east end
                else if (c < 105f) d = b;
                else if (c < 125f) d = Mathf.Lerp(b, h, Ease((c - 105f) / 20f));
                else if (c < 135f) d = h;
                else d = Mathf.Lerp(h, a, Ease((c - 135f) / 15f));
                var p = OnTrack(d, out var dir); train.SetPositionAndRotation(p + Vector3.up * .25f, Quaternion.LookRotation(dir));
            }
            if (bell != null && bellT >= 0f) { bellT += dt; bell.rotation = bellRest * Quaternion.Euler(Mathf.Sin(bellT * 7f) * 35f * Mathf.Exp(-bellT * .9f), 0, 0); if (bellT > 5f) { bellT = -1f; bell.rotation = bellRest; } }
            // the stone circle: every stone lit, and they all breathe together and the light swells
            if (stones.Count > 0)
            {
                var all = true; foreach (var (toy, _) in stones) if (!toy.On) all = false;
                if (all) foreach (var (_, m) in stones) m.SetColor("_EmissionColor", StoneBlue * (1.6f + Mathf.Sin(t * 1.6f) * 1.1f));
                if (circleLight != null) circleLight.intensity = Mathf.MoveTowards(circleLight.intensity, all ? 3f + Mathf.Sin(t * 1.6f) : .4f, dt * 2f);
            }
            // the phone rings now and then until someone answers
            if (ringM != null)
            {
                if (t > nextRing && ringUntil <= 0f) ringUntil = t + 12f;
                var ringing = ringUntil > t && Mathf.Repeat(t, 2.4f) < 1.2f && Mathf.Repeat(t * 9f, 1f) < .5f;
                if (ringUntil > 0f && t > ringUntil) { ringUntil = 0f; nextRing = t + Random.Range(20f, 45f); }
                ringM.SetColor("_EmissionColor", new Color(1f, .9f, .6f) * (ringing ? 4f : .5f)); if (ringLight != null) ringLight.intensity = ringing ? 2.6f : .8f;
            }
            if (strip != null) strip.intensity = Mathf.PerlinNoise(t * 9f, 3f) > .82f ? .5f : 1.6f;
            if (screen != null && t > screenT) { screenT = t + .7f; var s = new System.Text.StringBuilder(); for (var k = 0; k < 8; k++) { s.Append(Random.value < .5f ? '0' : '1'); s.Append(k == 3 ? '\n' : ' '); } screen.text = s.ToString(); }
            if (serving != null) serving.color = new Color(1f, .3f, .15f, Mathf.PerlinNoise(t * 5f, 9f) > .9f ? .3f : 1f);
            if (kite != null)
            {
                var at = kiteHand + kiteWind * (16f + Mathf.Sin(t * .4f) * 2f) + Vector3.up * (14f + Mathf.Sin(t * .7f) * 1.5f) + Vector3.Cross(Vector3.up, kiteWind) * Mathf.Sin(t * .9f) * 3f;
                kite.SetPositionAndRotation(at, Quaternion.LookRotation(kiteWind) * Quaternion.Euler(-25f, 0, Mathf.Sin(t * 1.3f) * 12f));
                if (kiteString != null) { var d = at - kiteHand; kiteString.SetPositionAndRotation(kiteHand + d / 2f, Quaternion.LookRotation(d)); kiteString.localScale = new Vector3(.015f, .015f, d.magnitude); }
            }
            foreach (var (w, home, seed) in wisps) if (w != null) w.position = home + new Vector3(Mathf.Sin(t * .23f + seed) * 4f, Mathf.Sin(t * .7f + seed * 2f) * .5f, Mathf.Cos(t * .19f + seed * 1.3f) * 4f);
            if (words != null && wordsFor > 0f) { wordsFor -= dt; if (wordsFor <= 0f) words.gameObject.SetActive(false); }
        }
    }
}
