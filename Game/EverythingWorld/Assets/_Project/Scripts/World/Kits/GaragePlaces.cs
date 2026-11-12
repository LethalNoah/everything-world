using System.Collections.Generic;
using UnityEngine;
using static EverythingWorld.World.PlaceTools;

namespace EverythingWorld.World
{
    /// Garage Rock Row laid out on purpose (the build-land standard). The Row, the suburbs and every garage stay
    /// where they were. Two more roads with sodium lamps leave the grid for the dark, empty ends of the land: the
    /// Shore Road north to the bowling alley and the skate park, and the Hill Road south-west to the trailer park.
    /// And the Row itself gets its people: smokers outside the bar, someone waiting on a kerb, two walking home.
    public partial class GarageKit
    {
        static readonly Vector2 BowlAt = new Vector2(10126, 6134), SkateAt = new Vector2(10204, 6292), TrailerAt = new Vector2(9698, 5438);
        static readonly Vector2[] ShoreRoad = { new Vector2(10112, 6030), new Vector2(10132, 6070), new Vector2(10168, 6150), new Vector2(10194, 6220), new Vector2(10200, 6262) };
        static readonly Vector2[] HillRoad = { new Vector2(9716, 5690), new Vector2(9692, 5640), new Vector2(9674, 5562), new Vector2(9684, 5490), new Vector2(9694, 5462) };
        static readonly (Vector2 at, float r)[] PlaceRings = { (BowlAt, 13f), (SkateAt, 14f), (TrailerAt, 14f) };

        GaragePlacesLife plife; System.Random prng; Transform rootT; LandContext ctx0; readonly List<Vector3> roadP = new List<Vector3>();

        static readonly Color[] Jackets = { new Color(.12f, .12f, .14f), new Color(.3f, .22f, .18f), new Color(.2f, .3f, .45f), new Color(.5f, .15f, .15f), new Color(.85f, .82f, .75f), new Color(.3f, .4f, .3f) };
        static readonly Color[] Denims = { new Color(.2f, .28f, .45f), new Color(.12f, .12f, .14f), new Color(.3f, .32f, .36f) };
        static readonly Residents.Hat[] RowHats = { Residents.Hat.None, Residents.Hat.Cap, Residents.Hat.None, Residents.Hat.Beanie, Residents.Hat.None };
        static readonly string[] RowTalk =
        {
            "Band's on at eleven. Band's always on at eleven. It's always nearly eleven.", "You hear that? Third garage down. They've got a song. Just the one, but it's a song.", "Don't park by the casino. Don't ask me how I know.",
            "It rained an hour ago. Makes the neon look expensive.", "Everyone in this town is in a band or owes one money.",
        };

        Residents.Look Local(Residents.Tool tool = Residents.Tool.None, bool apron = false) { var l = Residents.Random(prng, Jackets, Denims, RowHats, Jackets); l.tool = tool; l.apron = apron; if (l.hat == Residents.Hat.None && prng.Next(3) == 0) l.longHair = true; return l; }
        Resident Folk(Vector3 feet, Vector3 face, Resident.Pose pose, string[] lines = null, Residents.Look look = null, Vector3[] route = null, float seatH = HumanScale.SeatHeight)
            => Residents.Make(plife.transform, feet, face, look ?? Local(), pose, prng, lines ?? RowTalk, route, seatH);
        void Col(Vector3 c, Vector3 size, Quaternion rot) { var go = new GameObject("Box collider"); go.transform.SetParent(rootT, false); go.transform.SetPositionAndRotation(c, rot); go.AddComponent<BoxCollider>().size = size; go.isStatic = true; }
        Vector3 G(Vector3 p) => ctx0.OnGround(p.x, p.z);
        void Ring(Vector3 at, float r) => noGrass.Add(new Vector3(at.x, r, at.z));

        static void PlaceLevel(IslandWorldData data, int index, Vector2 c, float flat, float blend)
        {
            var level = data.HeightAt(c.x, c.y);
            for (var r = 0; r < data.Nz; r++)
            {
                var z = data.layout.depth - r * data.Step; if (Mathf.Abs(z - c.y) > blend) continue;
                for (var col = 0; col < data.Nx; col++)
                {
                    var x = col * data.Step; if (Mathf.Abs(x - c.x) > blend) continue;
                    var i = r * data.Nx + col; if (data.islandIds[i] != index) continue;
                    var d = Vector2.Distance(new Vector2(x, z), c);
                    if (d < flat) data.heights[i] = level; else if (d < blend) data.heights[i] = Mathf.Lerp(level, data.heights[i], Mathf.SmoothStep(0, 1, (d - flat) / (blend - flat)));
                }
            }
        }

        public override void ShapeGround(IslandWorldData data, int index)
        {
            ShapeRow(data, index);   // everything the land already did, unchanged
            foreach (var (at, r) in PlaceRings) PlaceLevel(data, index, at, r, r + 14f);
        }

        void SodiumLamp(MeshKit mk, Vector3 g, Vector3 arm)
        {
            var up = Vector3.up; arm.y = 0; arm.Normalize(); mk.Prism(metal, g - up * .2f, up, .09f, 5.7f, 6); mk.Box(metal, g + up * 5.4f + arm * .6f, new Vector3(.08f, .08f, 1.3f), Quaternion.LookRotation(arm)); mk.Box(GlowM(Sodium, 2.6f), g + up * 5.32f + arm * 1.1f, new Vector3(.34f, .1f, .5f), Quaternion.LookRotation(arm));
            halos.Add(g + up * 5.2f + arm * 1.1f, 2.2f, Sodium, .9f); ArtistBits.PointLight(rootT, g + up * 4.8f + arm * 1.1f, Sodium, 15f, 1.4f); ArtistBits.PostCollider(rootT, g, 5.5f, .12f);
        }

        void RoadStrip(MeshKit mk, List<Vector3> pts, Material m, float w, float lift)
        {
            var n = pts.Count; var L = new Vector3[n]; var R = new Vector3[n];
            Vector3 Flat(Vector3 v) { v.y = 0; return v.normalized; }
            for (var i = 0; i < n; i++)
            {
                var a = i > 0 ? Flat(pts[i] - pts[i - 1]) : Flat(pts[1] - pts[0]); var b = i < n - 1 ? Flat(pts[i + 1] - pts[i]) : Flat(pts[n - 1] - pts[n - 2]);
                var t = (a + b).normalized; var side = Vector3.Cross(Vector3.up, t); var miter = 1f / Mathf.Max(.6f, Vector3.Dot(side, Vector3.Cross(Vector3.up, b)));
                L[i] = pts[i] + Vector3.up * lift - side * (w / 2f * miter); R[i] = pts[i] + Vector3.up * lift + side * (w / 2f * miter);
            }
            for (var i = 0; i + 1 < n; i++) mk.Quad(m, L[i], R[i], R[i + 1], L[i + 1], (pts[i] + pts[i + 1]) / 2f - Vector3.up);
        }

        List<Vector3> BackRoad(LandContext ctx, MeshKit mk, Vector2[] way)
        {
            var pts = PlaceTools.Smooth(ctx, way, false, 2.5f); if (pts.Count < 2) return pts;
            RoadStrip(mk, pts, asphalt, 6f, .07f); var up = Vector3.up; var line = WorldMaterials.Solid(new Color(.85f, .75f, .3f));
            for (var i = 1; i < pts.Count; i += 2) { var a = pts[i - 1]; var b = pts[i]; mk.Box(line, (a + b) / 2f + up * .085f, new Vector3(.14f, .02f, Vector3.Distance(a, b) * .55f), Quaternion.LookRotation(b - a)); }
            var n = 0; for (var i = 5; i < pts.Count; i += 12) { var d = pts[i] - pts[i - 1]; d.y = 0; if (d.sqrMagnitude < .01f) continue; d.Normalize(); var sd = Vector3.Cross(up, d) * (n++ % 2 == 0 ? 1f : -1f); var lp = pts[i] + sd * 4f; if (ctx.IsLand(lp.x, lp.z, 2f) && !InNoGrass(lp.x, lp.z, 3f)) SodiumLamp(mk, ctx.OnGround(lp.x, lp.z), -sd); }
            for (var i = 0; i < pts.Count; i += 2) noGrass.Add(new Vector3(pts[i].x, 5f, pts[i].z));
            roadP.AddRange(pts); return pts;
        }

        void Drive(LandContext ctx, MeshKit mk, Vector3 to)
        {
            var from = Nearest(roadP, to); var d = new Vector2(to.x - from.x, to.z - from.z); var len = d.magnitude; if (len < 5f) return; d /= len;
            var a = new Vector2(from.x, from.z) + d * 3.1f; var b = new Vector2(to.x, to.z);
            var pts = PlaceTools.Smooth(ctx, new[] { a, Vector2.Lerp(a, b, .5f), b }, false, 2f, 1f); if (pts.Count < 2) return;
            RoadStrip(mk, pts, asphalt, 4f, .065f); foreach (var p in pts) noGrass.Add(new Vector3(p.x, 3.5f, p.z));
        }

        void RowPlaces(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            roadP.Clear(); rootT = ctx.Root; ctx0 = ctx; prng = new System.Random(2001);
            plife = new GameObject("Garage places life").AddComponent<GaragePlacesLife>(); plife.transform.SetParent(life.transform, false);
            foreach (var (at, r) in PlaceRings) noGrass.Add(new Vector3(at.x, r, at.y));
            BackRoad(ctx, mk, ShoreRoad); BackRoad(ctx, mk, HillRoad);
            Bowling(ctx, mk, solid); SkatePark(ctx, mk, solid); TrailerPark(ctx, mk, solid);
            if (hasStreet) RowPeople(ctx, mk, solid);
        }

        // ---------------------------------------------------------------- people on the Row
        void RowPeople(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var side = Vector3.Cross(Vector3.up, streetDir); var kerb = StreetHalf + 1.6f; var up = Vector3.up;
            // two walking home down opposite pavements
            foreach (var s in new[] { -1f, 1f })
            {
                var route = new List<Vector3>(); for (var t = 16f; t < streetLen - 30f; t += 14f) route.Add(OnStreet(t, s * kerb) + up * .12f); if (s < 0) route.Reverse(); for (var i = route.Count - 2; i > 0; i--) route.Add(route[i]);
                if (route.Count > 3) Folk(route[0], FaceTo(route[0], route[1]), Resident.Pose.Walk, s < 0 ? new[] { "Walking it off. The gig, the argument, the chips. All of it.", "My amp's in the pawn shop again. I visit." } : new[] { "Last bus went at midnight. There is no bus. I just like saying it.", "You want the bowling alley, it's north, out the Shore Road. Open till the pins give up." }, Local(), route.ToArray());
            }
            // knots of people at the kerb every so often: smokers, someone sat on the kerb, a couple arguing quietly
            string[][] bits =
            {
                new[] { "We're on after these. We've been on after these for an hour.", "Got a light? No. Got a lighter? Also no. Got a song? Everyone's got a song." },
                new[] { "He says he's quitting the band. He says it every night. He's the only one who knows the bridge.", "I'm not arguing. I'm rehearsing an argument." },
                new[] { "Sat down for a minute in March.", "Kerb's dry-ish. Have a seat. Watch the cars. It's the same four cars." },
                new[] { "Doorman says it's full. There are six people in there. One of them's the band's mum.", "The neon buzzes in B flat. Half the songs in this town are in B flat because of it." },
            };
            var n = 0;
            for (var t = 34f; t < streetLen - 46f; t += 44f)
            {
                var s = n % 2 == 0 ? 1f : -1f; var c = OnStreet(t + 6f, s * (kerb + .6f)) + up * .12f; var b = bits[n % bits.Length];
                if (n % 4 == 2) { var kp = OnStreet(t + 6f, s * (StreetHalf + .25f)); Folk(kp + up * .16f, -side * s, Resident.Pose.Sit, b, Local(Residents.Tool.Mug), null, .16f); Interact.Seat(plife.transform, kp + streetDir * 1.2f + up * .2f, -side * s, "Sit on the kerb"); }
                else { Folk(c - streetDir * .5f, streetDir, n % 4 == 0 ? Resident.Pose.Lean : Resident.Pose.Stand, b, Local()); Folk(c + streetDir * .5f, -streetDir, Resident.Pose.Stand, b, Local()); }
                n++;
            }
        }

        // ---------------------------------------------------------------- the Bowling Alley
        void Bowling(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var g = ctx.OnGround(BowlAt.x, BowlAt.y); var f = new PlaceFrame(g, FaceTo(g, Nearest(roadP, g))); const float W = 12f, D = 20f, H = 4f, T = .3f, Door = HumanScale.DoorWidth + .6f;
            var fo = new PlaceFrame(g + up * .25f, f.Fwd); var wallM = WorldMaterials.Solid(new Color(.3f, .32f, .4f)); var laneM = WorldMaterials.Solid(new Color(.8f, .64f, .42f), .7f); var carpet = WorldMaterials.Solid(new Color(.25f, .12f, .3f));
            mk.Box(dark, g - up * .35f, new Vector3(W + .8f, 1.2f, D + .8f), f.r);
            void Wall(Vector3 p, Vector3 s) { mk.Box(wallM, p, s, f.r); solid.Box(wallM, p, s, f.r); }
            mk.Box(carpet, fo.P(0, -.03f, D / 2f - 3.5f), new Vector3(W - T, .1f, 7f - T), f.r); mk.Box(laneM, fo.P(0, -.03f, -3.5f), new Vector3(W - T, .1f, D - 7f), f.r); solid.Box(dark, fo.P(0, -.15f, 0), new Vector3(W, .3f, D), f.r);
            Wall(fo.P(0, H / 2f, -D / 2f + T / 2f), new Vector3(W, H, T)); foreach (var e in new[] { -1f, 1f }) Wall(fo.P(e * (W / 2f - T / 2f), H / 2f, 0), new Vector3(T, H, D - T * 2f));
            var seg = (W - Door) / 2f; foreach (var e in new[] { -1f, 1f }) Wall(fo.P(e * (Door / 2f + seg / 2f), H / 2f, D / 2f - T / 2f), new Vector3(seg, H, T)); Wall(fo.P(0, H - .5f, D / 2f - T / 2f), new Vector3(Door, 1f, T));
            mk.Box(dark, fo.P(0, H + .1f, 0), new Vector3(W + .6f, .2f, D + .6f), f.r); solid.Box(dark, fo.P(0, H + .1f, 0), new Vector3(W + .6f, .2f, D + .6f), f.r);
            IndoorSpaces.Add(fo.P(0, H / 2f, 0), f.r, new Vector3(W - .7f, H, D - .7f));
            // the sign: a giant pin and ball on the roof, the name in neon
            Sign(ctx, mk, fo.P(0, H + 1.3f, D / 2f + .2f), f.Fwd, "LUCKY STRIKE LANES", NeonC[0], 1f);
            var pin = fo.P(-4.6f, H + .2f, D / 2f - 1f); mk.Prism(WorldMaterials.Glow(new Color(.97f, .97f, .97f), .5f), pin, up, .55f, 1.6f, 10, .5f); mk.Blob(WorldMaterials.Glow(new Color(.97f, .97f, .97f), .5f), pin + up * 2.1f, new Vector3(.38f, .5f, .38f), prng, 0f); mk.Prism(GlowM(new Color(1f, .2f, .2f), 2.4f), pin + up * 1.45f, up, .3f, .14f, 10); halos.Add(pin + up * 1.4f, 2f, new Color(1f, .9f, .9f), .5f);
            // three lanes with gutters; ten pins at the end of each that really fall; balls on a rack to carry and throw
            for (var lane = 0; lane < 3; lane++)
            {
                var lx = -3.4f + lane * 3.4f; foreach (var e in new[] { -1f, 1f }) { mk.Box(dark, fo.P(lx + e * 1.25f, .03f, -3.5f), new Vector3(.3f, .06f, D - 7.4f), f.r); }
                for (var k = 0; k < 7; k++) mk.Box(GlowM(NeonC[(lane + k) % NeonC.Length], 1.6f), fo.P(lx, .03f, 2.6f - k * 1.9f), new Vector3(.08f, .01f, .4f), f.r);
                mk.Box(dark, fo.P(lx, 2.6f, -D / 2f + T + 1f), new Vector3(2.8f, 1.6f, .2f), f.r); ArtistBits.Label(ctx.Root, lane == 1 ? "STRIKE!" : lane == 0 ? "X  X  /  9" : "GUTTER  :(", fo.P(lx, 2.6f, -D / 2f + T + 1.12f), f.Fwd, .2f, NeonC[lane * 2], TextAnchor.MiddleCenter);
                var n = 0; for (var row = 0; row < 4; row++) for (var k = 0; k <= row; k++)
                {
                    var pm = new MeshKit(); var white = WorldMaterials.Solid(new Color(.96f, .96f, .96f), .6f); pm.Prism(white, Vector3.zero, Vector3.up, .06f, .22f, 8, .6f); pm.Blob(white, new Vector3(0, .3f, 0), new Vector3(.045f, .08f, .045f), prng, 0f); pm.Prism(WorldMaterials.Solid(new Color(.85f, .15f, .15f)), new Vector3(0, .2f, 0), Vector3.up, .042f, .03f, 8);
                    var go = pm.BuildSingle("Bowling pin", plife.transform, false); go.transform.position = fo.P(lx + (k - row / 2f) * .3f, .03f, -D / 2f + T + 2.6f - row * .27f); var c = Interact.Carry(go, "a bowling pin", .35f, g.y - 10f); var bc = c.GetComponent<BoxCollider>(); if (bc != null) { bc.size = new Vector3(.12f, .38f, .12f); bc.center = new Vector3(0, .19f, 0); } c.home = go.transform.position; plife.pins.Add(c); n++;
                }
            }
            var rack = fo.P(0, 0, 4.2f); mk.Box(metal, rack + up * .35f, new Vector3(3.4f, .1f, .5f), f.r); foreach (var e in new[] { -1.6f, 1.6f }) mk.Box(metal, rack + up * .18f + f.Right * e, new Vector3(.08f, .36f, .5f), f.r); Col(rack + up * .2f, new Vector3(3.4f, .4f, .5f), f.r);
            for (var k = 0; k < 5; k++) { var bm = new MeshKit(); bm.Blob(WorldMaterials.Solid(NeonC[k % NeonC.Length] * .7f, .8f), new Vector3(0, .16f, 0), Vector3.one * .16f, prng, 0f); var go = bm.BuildSingle("Bowling ball", plife.transform, false); go.transform.position = rack + up * .42f + f.Right * (-1.3f + k * .65f); var c = Interact.Carry(go, "a bowling ball", 5f, g.y - 10f); var bc = c.GetComponent<BoxCollider>(); if (bc != null) { bc.size = Vector3.one * .3f; bc.center = new Vector3(0, .16f, 0); } }
            // the reset button, a shoe counter, a bench, two regulars
            var rb = new MeshKit(); rb.Prism(GlowM(new Color(1f, .3f, .2f), 2.4f), Vector3.zero, Vector3.up, .16f, .08f, 10); var rgo = rb.BuildSingle("Pin reset button", plife.transform, false); rgo.transform.position = fo.P(W / 2f - T - .5f, 1.05f, 3.2f); mk.Box(metal, fo.P(W / 2f - T - .5f, .52f, 3.2f), new Vector3(.4f, 1.05f, .4f), f.r); Col(fo.P(W / 2f - T - .5f, .52f, 3.2f), new Vector3(.4f, 1.05f, .4f), f.r);
            Interact.Toy(rgo, Toy.Kind.Custom, "Reset the pins", new Vector3(.6f, .5f, .6f)).onUse = _ => { plife.ResetPins(); Say(plife.transform, fo.P(W / 2f - 1.6f, 2.2f, 3.2f), "Clunk. Thirty pins stand back up.", 3.5f); };
            mk.Box(wood, fo.P(-W / 2f + T + .5f, .55f, D / 2f - 2.6f), new Vector3(.9f, 1.1f, 3.4f), f.r); Col(fo.P(-W / 2f + T + .5f, .55f, D / 2f - 2.6f), new Vector3(.9f, 1.1f, 3.4f), f.r); for (var k = 0; k < 5; k++) mk.Box(WorldMaterials.Solid(k % 2 == 0 ? new Color(.8f, .2f, .2f) : new Color(.2f, .3f, .7f)), fo.P(-W / 2f + T + .5f, 1.16f, D / 2f - 3.9f + k * .6f), new Vector3(.3f, .12f, .14f), f.r);
            Folk(fo.P(-W / 2f + T + 1.4f, 0, D / 2f - 2.6f), f.Right, Resident.Pose.Stand, new[] { "Shoes are a dollar. I don't have change. Shoes are free.", "Lane two's warped. It's the best lane. It gives you a strike if it likes you.", "Throw it like you mean it. Hit reset when you're done; my knees are finished." }, Local(Residents.Tool.None, true));
            var bn = fo.P(3.4f, 0, D / 2f - 1.6f); mk.Box(WorldMaterials.Solid(new Color(.6f, .2f, .2f)), bn + up * .45f, new Vector3(3f, .12f, .5f), f.r); mk.Box(WorldMaterials.Solid(new Color(.6f, .2f, .2f)), bn + up * .85f + f.Fwd * .22f, new Vector3(3f, .6f, .08f), f.r); Col(bn + up * .5f, new Vector3(3f, 1f, .6f), f.r); Interact.Bench(plife.transform, bn + up * .52f - f.Right * 1f, bn + up * .52f + f.Right * .2f, -f.Fwd);
            Folk(bn + up * .52f + f.Right * 1.1f, -f.Fwd, Resident.Pose.Sit, new[] { "League night. The league is me and Dale. Dale's late.", "I bowled a 300 once. Nobody saw. I think about that a lot." }, Local(Residents.Tool.Mug), null, .52f);
            ArtistBits.PointLight(ctx.Root, fo.P(0, 3.2f, 4f), new Color(1f, .8f, .6f), 14f, 2.2f, true); ArtistBits.PointLight(ctx.Root, fo.P(0, 3.2f, -5f), NeonC[0], 16f, 2f, true);
            // the car park out front
            mk.Box(asphalt, g + f.Fwd * (D / 2f + 6f) + up * .04f, new Vector3(16f, .08f, 10f), f.r); Car(ctx, mk, solid, G(f.P(-5f, 0, D / 2f + 6f)), f.Right, Paint[1], false); Car(ctx, mk, solid, G(f.P(4.5f, 0, D / 2f + 7f)), -f.Right, Paint[4], false); SodiumLamp(mk, G(f.P(8.5f, 0, D / 2f + 10f)), -f.Right);
            Drive(ctx, mk, f.P(0, 0, D / 2f + 10f)); Ring(g, 16f);
            Shot(ctx, "lucky strike lanes", f.P(-9f, 3f, D / 2f + 15f), fo.P(0, 3f, 0));
            Shot(ctx, "inside the bowling alley", fo.P(2.5f, 1.6f, D / 2f - 1.2f), fo.P(-.5f, .8f, -D / 2f + 2f));
        }

        // ---------------------------------------------------------------- the Skate Park
        void SkatePark(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var g = ctx.OnGround(SkateAt.x, SkateAt.y); var f = new PlaceFrame(g, FaceTo(g, Nearest(roadP, g))); var conc = WorldMaterials.Solid(new Color(.5f, .5f, .52f), .5f); var concD = WorldMaterials.Solid(new Color(.36f, .36f, .4f), .5f);
            mk.Box(conc, g - up * .35f, new Vector3(26f, 1f, 22f), f.r); solid.Box(conc, g - up * .35f, new Vector3(26f, 1f, 22f), f.r); var fo = new PlaceFrame(g + up * .15f, f.Fwd);
            // two quarter pipes facing each other, a fun box with a rail, a ledge; graffiti in neon paint
            foreach (var e in new[] { -1f, 1f })
            {
                var qp = fo.P(e * 10.5f, 0, -2f); for (var s = 0; s < 6; s++) { var a = s / 6f; var y = (1f - Mathf.Cos(a * Mathf.PI * .5f)) * 2.4f; var x = e * (-3f + Mathf.Sin(a * Mathf.PI * .5f) * 3f); var a1 = (s + 1) / 6f; var y1 = (1f - Mathf.Cos(a1 * Mathf.PI * .5f)) * 2.4f; var x1 = e * (-3f + Mathf.Sin(a1 * Mathf.PI * .5f) * 3f); var c = qp + f.Right * (x + x1) / 2f + up * (y + y1) / 2f; var len = Mathf.Sqrt((x1 - x) * (x1 - x) + (y1 - y) * (y1 - y)); var tilt = Mathf.Atan2(y1 - y, (x1 - x) * e) * Mathf.Rad2Deg; mk.Box(conc, c, new Vector3(len + .05f, .14f, 9f), f.r * Quaternion.Euler(0, 0, tilt * e)); Col(c, new Vector3(len + .05f, .2f, 9f), f.r * Quaternion.Euler(0, 0, tilt * e)); }
                mk.Box(concD, qp + f.Right * e * 1f + up * 1.2f, new Vector3(2f, 2.4f, 9f), f.r); solid.Box(concD, qp + f.Right * e * 1f + up * 1.2f, new Vector3(2f, 2.4f, 9f), f.r); mk.Prism(metal, qp + up * 2.42f - f.Fwd * 4.5f, f.Fwd, .05f, 9f, 6);
                var tag = e < 0 ? "NO SLEEP" : "TURN IT UP"; ArtistBits.Label(ctx.Root, tag, qp + f.Right * e * 2.02f + up * 1.3f, f.Right * e, .5f, NeonC[e < 0 ? 4 : 0], TextAnchor.MiddleCenter);
            }
            var fb = fo.P(0, 0, 3f); mk.Box(concD, fb + up * .3f, new Vector3(5f, .6f, 2.4f), f.r); solid.Box(concD, fb + up * .3f, new Vector3(5f, .6f, 2.4f), f.r); foreach (var e in new[] { -1f, 1f }) { mk.Box(concD, fb + f.Right * e * 3.4f + up * .15f, new Vector3(2f, .12f, 2.4f), f.r * Quaternion.Euler(0, 0, -e * 17f)); Col(fb + f.Right * e * 3.4f + up * .15f, new Vector3(2f, .12f, 2.4f), f.r * Quaternion.Euler(0, 0, -e * 17f)); }
            mk.Box(metal, fb + up * 1f, new Vector3(4.4f, .06f, .06f), f.r); foreach (var e in new[] { -2f, 2f }) mk.Box(metal, fb + f.Right * e + up * .8f, new Vector3(.06f, .4f, .06f), f.r); Col(fb + up * .8f, new Vector3(4.4f, .5f, .1f), f.r);
            var lg = fo.P(-3f, 0, -7f); mk.Box(concD, lg + up * .25f, new Vector3(6f, .5f, .7f), f.r); solid.Box(concD, lg + up * .25f, new Vector3(6f, .5f, .7f), f.r); Interact.Bench(plife.transform, lg + up * .56f - f.Right * 2f, lg + up * .56f - f.Right * .5f, f.Fwd);
            // boards to carry and throw down; a boombox to press
            for (var k = 0; k < 3; k++) { var bm = new MeshKit(); bm.Box(WorldMaterials.Solid(NeonC[(k * 2) % NeonC.Length] * .8f), new Vector3(0, .08f, 0), new Vector3(.2f, .02f, .78f), Quaternion.identity); foreach (var z in new[] { -.25f, .25f }) foreach (var x in new[] { -.09f, .09f }) bm.Prism(WorldMaterials.Solid(new Color(.9f, .9f, .85f)), new Vector3(x - .02f * Mathf.Sign(x), .03f, z), Vector3.right * Mathf.Sign(x), .03f, .03f, 6); var go = bm.BuildSingle("Skateboard", plife.transform, false); go.transform.SetPositionAndRotation(fo.P(4f + k * .6f, .05f, -6.6f + k * .2f), f.r * Quaternion.Euler(0, k * 30f, 0)); var c = Interact.Carry(go, "a skateboard", .8f, g.y - 10f); var bc = c.GetComponent<BoxCollider>(); if (bc != null) { bc.size = new Vector3(.22f, .1f, .8f); bc.center = new Vector3(0, .05f, 0); } }
            var bx = lg + f.Right * 1.6f + up * .5f; var box = new MeshKit(); var speaker = new Material(WorldMaterials.Glow(NeonC[0], .4f)) { name = "Boombox" }; box.Box(dark, new Vector3(0, .16f, 0), new Vector3(.7f, .32f, .2f), Quaternion.identity); foreach (var e in new[] { -.2f, .2f }) box.Prism(speaker, new Vector3(e, .15f, .1f), Vector3.forward, .11f, .01f, 10); box.Box(metal, new Vector3(0, .36f, 0), new Vector3(.5f, .03f, .03f), Quaternion.identity);
            var bgo = box.BuildSingle("Boombox", plife.transform, false); bgo.transform.SetPositionAndRotation(bx, f.r); plife.boomM = speaker; plife.boomLight = ArtistBits.PointLight(ctx.Root, bx + up * .6f + f.Fwd * .6f, NeonC[0], 8f, .4f); string[] tape = { "Click. Two chords, fast, badly, perfectly.", "Click. Off. The wheels sound louder.", "Click. A drummer counts in and nobody waits for four." }; var tn = 0;
            Interact.Toy(bgo, Toy.Kind.Custom, "Press play on the boombox", new Vector3(.9f, .6f, .5f)).onUse = _ => { plife.boomOn = tn % 3 != 1; Say(plife.transform, bx + up * 1.4f, tape[tn++ % tape.Length], 4f); };
            // skaters: one rolling back and forth between the pipes, two watching
            var r0 = fo.P(-6.5f, 0, -3f); var r1 = fo.P(6.5f, 0, -3f); var sk = Local(); sk.hat = Residents.Hat.Beanie; Folk(r0, f.Right, Resident.Pose.Walk, new[] { "Can't stop. If I stop I have to try the big one.", "Watch this. No, wait. Watch THIS. No. Okay, don't watch." }, sk, new[] { r0, r1 }).speed = 3.6f;
            Folk(lg + up * .56f + f.Right * .4f, f.Fwd, Resident.Pose.Sit, new[] { "He's been about to drop in since nine.", "Take a board. They're nobody's. They just live here.", "We've got a band. We haven't got instruments. It's going really well." }, Local(), null, .56f);
            Folk(fo.P(8.5f, 0, 6f), -f.Fwd, Resident.Pose.Lean, new[] { "Cops come by at two. They just want to see the kickflip.", "That's my tag. The good one." }, Local());
            SodiumLamp(mk, G(f.P(-13.5f, 0, 9f)), f.Right); SodiumLamp(mk, G(f.P(13.5f, 0, -9f)), -f.Right);
            Drive(ctx, mk, f.P(0, 0, 11.5f)); Ring(g, 16f);
            Shot(ctx, "the skate park", f.P(-11f, 5f, 16f), g + up * 1f);
        }

        // ---------------------------------------------------------------- the Trailer Park
        void TrailerPark(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var g = ctx.OnGround(TrailerAt.x, TrailerAt.y); var f = new PlaceFrame(g, FaceTo(g, Nearest(roadP, g))); Color[] tc = { new Color(.75f, .78f, .8f), new Color(.55f, .7f, .7f), new Color(.82f, .76f, .6f), new Color(.7f, .6f, .65f) };
            // four trailers round a yard of gravel, string lights between them, a fire in an oil drum
            mk.Prism(WorldMaterials.Solid(new Color(.32f, .3f, .3f)), g - up * .05f, up, 9f, .12f, 16);
            var tops = new List<Vector3>();
            for (var k = 0; k < 4; k++)
            {
                var d = Quaternion.Euler(0, 45f + k * 90f, 0) * f.Fwd; if (k == 0) d = Quaternion.Euler(0, 60f, 0) * f.Fwd; var cp = G(g + d * 10.5f); var rot = Quaternion.LookRotation(Vector3.Cross(up, d)); var inward = -d; var body = WorldMaterials.Solid(tc[k], .5f);
                mk.Box(body, cp + up * 1.9f, new Vector3(2.8f, 2.6f, 8f), rot); solid.Box(body, cp + up * 1.6f, new Vector3(2.8f, 3.2f, 8f), rot); mk.Box(dark, cp + up * .45f, new Vector3(2.6f, .5f, 7.6f), rot); mk.Box(WorldMaterials.Solid(tc[k] * .7f, .5f), cp + up * 2.3f, new Vector3(2.84f, .3f, 8.04f), rot);
                var lit = k != 2; mk.Box(lit ? GlowM(k == 1 ? new Color(.5f, .7f, 1f) : new Color(1f, .8f, .5f), 1.5f) : dark, cp + up * 2.1f + inward * 1.41f + rot * Vector3.forward * 1.8f, new Vector3(1.6f, .9f, .03f), Quaternion.LookRotation(inward)); mk.Box(WorldMaterials.Solid(new Color(.3f, .3f, .34f)), cp + up * 1.75f + inward * 1.41f - rot * Vector3.forward * 1.5f, new Vector3(.8f, 2f, .03f), Quaternion.LookRotation(inward));
                for (var s = 0; s < 2; s++) { var st = cp + inward * (1.7f + s * .35f) - rot * Vector3.forward * 1.5f + up * (.52f - s * .26f); mk.Box(wood, st, new Vector3(.9f, .26f, .36f), Quaternion.LookRotation(inward)); solid.Box(wood, st, new Vector3(.9f, .26f, .36f), Quaternion.LookRotation(inward)); }
                if (lit) halos.Add(cp + up * 2.1f + inward * 1.8f + rot * Vector3.forward * 1.8f, 1.4f, k == 1 ? new Color(.5f, .7f, 1f) : new Color(1f, .8f, .5f), .5f);
                var pole = G(cp + inward * 2.6f + rot * Vector3.forward * 3.4f); mk.Prism(metal, pole, up, .05f, 3.2f, 5); ArtistBits.PostCollider(ctx.Root, pole, 3.2f, .07f); tops.Add(pole + up * 3.2f);
                // lawn flamingos, obviously
                for (var s = 0; s < 2; s++) { var fp = G(cp + inward * (2.4f + s * .5f) - rot * Vector3.forward * (3f + s * .4f)); var pink = WorldMaterials.Solid(new Color(1f, .45f, .65f)); mk.Prism(dark, fp, up, .015f, .5f, 4); mk.Blob(pink, fp + up * .58f, new Vector3(.13f, .1f, .2f), prng, 0f); mk.Prism(pink, fp + up * .6f + rot * Vector3.forward * .12f, (up + rot * Vector3.forward * .3f).normalized, .025f, .3f, 4); }
            }
            for (var k = 0; k < 4; k++) { var a = tops[k]; var b = tops[(k + 1) % 4]; for (var s = 1; s < 12; s++) { var t = s / 12f; var q = Vector3.Lerp(a, b, t) - up * Mathf.Sin(t * Mathf.PI) * .8f; mk.Blob(GlowM(NeonC[(s + k) % NeonC.Length], 2.4f), q, Vector3.one * .07f, prng, 0f); if (s % 4 == 2) halos.Add(q, .8f, NeonC[(s + k) % NeonC.Length], .5f); } }
            var drum = g + f.Right * 1f; mk.Prism(WorldMaterials.Solid(new Color(.3f, .2f, .16f)), drum, up, .4f, .9f, 10); mk.Prism(GlowM(new Color(1f, .5f, .15f), 3f), drum + up * .88f, up, .34f, .05f, 8); halos.Add(drum + up * 1.3f, 1.8f, new Color(1f, .55f, .2f), .9f); ArtistBits.PointLight(ctx.Root, drum + up * 1.6f, new Color(1f, .55f, .2f), 15f, 2f); ArtistBits.PostCollider(ctx.Root, drum, .9f, .4f); life.barrels.Add(drum + up * 1f);
            // lawn chairs round the fire, a cooler, a practice amp and a guitar: somebody's rehearsing out here
            Color[] web = { new Color(.3f, .6f, .4f), new Color(.8f, .3f, .3f), new Color(.3f, .4f, .8f), new Color(.85f, .7f, .3f) };
            for (var k = 0; k < 4; k++) { var d = Quaternion.Euler(0, k * 80f - 30f, 0) * f.Fwd; var c = G(drum + d * 2.4f); var rot = Quaternion.LookRotation(-d); var m = WorldMaterials.Solid(web[k]); mk.Box(m, c + up * .42f, new Vector3(.55f, .05f, .5f), rot); mk.Box(m, c + up * .8f + d * .25f, new Vector3(.55f, .6f, .05f), rot * Quaternion.Euler(-10f, 0, 0)); foreach (var e in new[] { -.27f, .27f }) mk.Box(metal, c + up * .22f + rot * Vector3.right * e, new Vector3(.03f, .44f, .5f), rot); Col(c + up * .45f, new Vector3(.6f, .9f, .6f), rot); if (k == 1 || k == 2) Folk(c + up * .48f, -d, Resident.Pose.Sit, k == 1 ? new[] { "Landlord says no drums after ten. So we do drums at nine fifty-nine, for a very long minute.", "Pull up a chair. Not that one. That one's Gary's. Gary's inside.", "We wrote that one out here. You can hear the generator in the recording." } : new[] { "Town's that way. We can hear every band from here, all at once. It's our favourite band.", "Kick the cooler, it opens." }, Local(k == 1 ? Residents.Tool.Rod : Residents.Tool.Mug), null, .48f); else Interact.Seat(plife.transform, c + up * .5f, -d, "Sit in the lawn chair"); }
            var cl = G(drum - f.Fwd * 2.6f + f.Right * 1.6f); mk.Box(WorldMaterials.Solid(new Color(.8f, .2f, .2f)), cl + up * .22f, new Vector3(.7f, .44f, .45f), f.r); mk.Box(WorldMaterials.Solid(new Color(.95f, .95f, .95f)), cl + up * .47f, new Vector3(.72f, .08f, .47f), f.r); Col(cl + up * .25f, new Vector3(.7f, .5f, .45f), f.r);
            for (var k = 0; k < 3; k++) { var cm = new MeshKit(); cm.Prism(WorldMaterials.Solid(NeonC[(k * 2 + 1) % NeonC.Length] * .8f, .7f), Vector3.zero, Vector3.up, .035f, .12f, 8); var go = cm.BuildSingle("Soda can", plife.transform, false); go.transform.position = cl + up * .52f + f.Right * (-.2f + k * .2f); var c = Interact.Carry(go, "a cold can of something", .2f, g.y - 10f); var bc = c.GetComponent<BoxCollider>(); if (bc != null) { bc.size = new Vector3(.09f, .13f, .09f); bc.center = new Vector3(0, .06f, 0); } }
            var amp = G(drum + f.Fwd * 3.6f - f.Right * 2.2f); mk.Box(dark, amp + up * .3f, new Vector3(.7f, .6f, .35f), f.r); mk.Blob(GlowM(new Color(1f, .3f, .2f), 2.4f), amp + up * .52f + f.Fwd * .18f + f.Right * .25f, Vector3.one * .03f, prng, 0f); Col(amp + up * .3f, new Vector3(.7f, .6f, .35f), f.r);
            SodiumLamp(mk, G(f.P(6f, 0, 15f)), -f.Right);
            Sign(ctx, mk, G(f.P(-6f, 0, 15.5f)) + up * 3.2f, f.Fwd, "SHADY ACRES", NeonC[2], .8f); mk.Prism(metal, G(f.P(-6f, 0, 15.5f)), up, .1f, 3f, 6); ArtistBits.PostCollider(ctx.Root, G(f.P(-6f, 0, 15.5f)), 3f, .12f);
            Drive(ctx, mk, f.P(0, 0, 9.5f)); Ring(g, 16f);
            Shot(ctx, "the trailer park", f.P(7f, 4f, 17f), g + up * 1.4f);
        }
    }

    /// The Garage places' moving parts: the bowling pins (stood back up by the reset button) and the skate park's boombox.
    public class GaragePlacesLife : MonoBehaviour
    {
        public List<Carryable> pins = new List<Carryable>(); public Material boomM; public Light boomLight; public bool boomOn;
        static readonly Color Cyan = new Color(.2f, .95f, 1f);

        public void ResetPins()
        {
            foreach (var p in pins) { if (p == null || p.Held) continue; var rb = p.GetComponent<Rigidbody>(); if (rb != null) { rb.linearVelocity = Vector3.zero; rb.angularVelocity = Vector3.zero; } p.transform.SetPositionAndRotation(p.home, Quaternion.identity); }
        }

        void Update()
        {
            if (boomM == null) return; var beat = boomOn ? Mathf.Max(0f, Mathf.Sin(Time.time * 11f)) : 0f;
            boomM.SetColor("_EmissionColor", Cyan * (.4f + beat * 3f)); if (boomLight != null) boomLight.intensity = .4f + beat * 1.6f;
        }
    }
}
