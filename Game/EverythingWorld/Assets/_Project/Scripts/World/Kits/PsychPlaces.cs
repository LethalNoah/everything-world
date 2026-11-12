using System.Collections.Generic;
using UnityEngine;
using static EverythingWorld.World.PlaceTools;

namespace EverythingWorld.World
{
    /// The Psychedelic Wilds laid out on purpose (the build-land standard). Every artist and the Grateful Dead's world
    /// stay exactly where they were; the Rainbow Road is drawn round them: a great horseshoe from the Dead's north
    /// side round the north, east and south of the land and back to the Dead's south side, with three roads out to
    /// it from the spiral mushroom. Along it, places made to stop at, with the wanderers who live there:
    /// the Oasis (a deep blue pool under palms, a tea tent of carpets and cushions, hammocks); the Record Shack
    /// (records to throw like frisbees, a giant one turning on the roof); MOTORIK, a lone petrol station on the long
    /// straight east road, a car forever driving it; the Fuzz Field (giant pedals to stomp, a wall of amps that
    /// answers); the Love-In (painted vans round a fire, rugs to sit on); the Sound Garden (a ring of giant chimes to
    /// strike); the Light Show (a white dome full of drifting colour, beanbags); the Mirror Spiral (walk in to the
    /// prism at its heart); the Mushroom Tea House (a room inside a giant mushroom); and the High Point lookout.
    public partial class PsychKit
    {
        static readonly Vector2 OasisAt = new Vector2(5780, 3690), ShackAt = new Vector2(6040, 3832), MotorikAt = new Vector2(6360, 3540), FuzzAt = new Vector2(6470, 3320), LoveAt = new Vector2(6190, 3212),
            SoundAt = new Vector2(6010, 3275), DomeAt = new Vector2(6440, 3740), MirrorAt = new Vector2(6340, 3665), TeaAt = new Vector2(5940, 3862), HighAt = new Vector2(6412, 3585);
        static readonly Vector2[] Horseshoe =
        {
            new Vector2(5750, 3640), new Vector2(5820, 3740), new Vector2(5890, 3825), new Vector2(5990, 3835), new Vector2(6120, 3860), new Vector2(6280, 3860), new Vector2(6390, 3760),
            new Vector2(6395, 3600), new Vector2(6400, 3470), new Vector2(6430, 3340), new Vector2(6340, 3250), new Vector2(6180, 3240), new Vector2(6060, 3290), new Vector2(5960, 3330),
        };
        static readonly int[] Spokes = { 4, 7, 11 };
        static readonly (Vector2 at, float r)[] PlaceRings = { (OasisAt, 22f), (ShackAt, 10f), (MotorikAt, 13f), (FuzzAt, 15f), (LoveAt, 17f), (SoundAt, 12f), (DomeAt, 12f), (MirrorAt, 12f), (TeaAt, 10f), (HighAt, 8f) };
        const float RoadW = 5f;

        PsychPlacesLife plife; System.Random prng; Transform rootT;
        readonly List<Vector3> roadPts = new List<Vector3>(); readonly List<List<Vector3>> mainRoads = new List<List<Vector3>>();
        Material chromeM, canvasM, deepBlue;

        static readonly Color[] Bottoms = { new Color(.3f, .35f, .55f), new Color(.6f, .45f, .3f), new Color(.85f, .8f, .7f), new Color(.5f, .3f, .55f) };
        static readonly Residents.Hat[] Hats = { Residents.Hat.None, Residents.Hat.None, Residents.Hat.Straw, Residents.Hat.Bucket, Residents.Hat.Cowboy };
        static readonly string[] Wanderers =
        {
            "Have you seen the sky today? It's been at it again.", "The mushrooms only glow if you're polite.", "I set out for the shop in '67. Lovely walk so far.",
            "Follow the rainbow road. Or the other rainbow road. They agree, mostly.", "Hot, isn't it? It's a dry weird.",
        };

        Residents.Look Look(Residents.Tool tool = Residents.Tool.None, bool apron = false) { var l = Residents.Random(prng, Rainbow, Bottoms, Hats, Rainbow); l.tool = tool; l.apron = apron; l.longHair = prng.NextDouble() < .65; return l; }
        Resident Folk(Vector3 feet, Vector3 face, Resident.Pose pose, string[] lines = null, Residents.Look look = null, Vector3[] route = null, float seatH = HumanScale.SeatHeight)
            => Residents.Make(plife.transform, feet, face, look ?? Look(), pose, prng, lines ?? Wanderers, route, seatH);
        void Ring(Vector3 at, float r) => noGrass.Add(new Vector3(at.x, r, at.z));
        void Col(Vector3 c, Vector3 size, Quaternion rot) => BoxCol(rootT, c, size, rot);

        void Sign(LandContext ctx, MeshKit mk, Vector3 at, Vector3 face, string text, float w = 2.6f, float size = .17f)
        {
            var up = Vector3.up; var g = ctx.OnGround(at.x, at.z); var rot = Quaternion.LookRotation(face);
            mk.Prism(wood, g - up * .2f, up, .08f, 2f, 6); ArtistBits.PostCollider(ctx.Root, g, 1.9f, .1f);
            for (var k = 0; k < Rainbow.Length; k++) mk.Box(WorldMaterials.Solid(Rainbow[k]), g + up * (1.42f + k * .13f), new Vector3(w, .13f, .08f), rot);
            mk.Box(dark, g + up * 1.75f + face * .03f, new Vector3(w - .3f, .56f, .06f), rot); Col(g + up * 1.75f, new Vector3(w, .9f, .16f), rot);
            ArtistBits.Label(ctx.Root, text, g + up * 1.77f + face * .07f, face, size, Ink, TextAnchor.MiddleCenter);
        }

        void Cushion(MeshKit mk, Vector3 at, Vector3 face, int k, string verb = "Sit on the cushion")
        {
            mk.Blob(WorldMaterials.Solid(Rainbow[k % Rainbow.Length], .3f), at + Vector3.up * .2f, new Vector3(.55f, .2f, .55f), prng, .1f);
            Interact.Seat(plife.transform, at + Vector3.up * .36f, face, verb).seatHeight = .3f;
        }

        // ---------------------------------------------------------------- the Rainbow Road
        /// Pushes a road out of every artist's ground and clear of the spiral mushroom.
        void Bend(LandContext ctx, List<Vector3> pts)
        {
            for (var i = 0; i < pts.Count; i++)
                foreach (var a in ctx.Artists)
                {
                    var r = Foot(a) + (a.name == "Grateful Dead" ? 26f : 8f); var v = new Vector2(pts[i].x - a.x, pts[i].z - a.z); var d = v.magnitude;
                    if (d < r) { v = d > .01f ? v / d * r : new Vector2(r, 0); pts[i] = ctx.OnGround(a.x + v.x, a.z + v.y); }
                }
        }

        int roadWalk = 1;
        void RainbowRoad(LandContext ctx, MeshKit mk, List<Vector3> pts, bool lamps = true)
        {
            Bend(ctx, pts); if (pts.Count < 2) return;
            Strip(mk, pts, RoadW, .06f, roadWalk);
            var up = Vector3.up; var n = 0;
            if (lamps) for (var i = 9; i < pts.Count; i += 16)
            {
                var d = pts[i] - pts[i - 1]; d.y = 0; if (d.sqrMagnitude < .01f) continue; d.Normalize(); var side = Vector3.Cross(up, d) * (n % 2 == 0 ? 1f : -1f);
                var lp = pts[i] + side * (RoadW / 2f + 1.3f); var blocked = !ctx.IsLand(lp.x, lp.z, 2f); foreach (var ar in ctx.Artists) if (Vector2.Distance(new Vector2(lp.x, lp.z), new Vector2(ar.x, ar.z)) < Foot(ar) + 12f) blocked = true;
                if (!blocked) { if (n % 3 == 1) PathBear(ctx, mk, ctx.OnGround(lp.x, lp.z), -side, n); else LavaLamp(ctx, mk, ctx.OnGround(lp.x, lp.z), .5f, n % Rainbow.Length); Ring(lp, 1.5f); }
                n++;
            }
            foreach (var p in pts) Remember(p, roadWalk);
            roadWalk++; mainRoads.Add(pts); roadPts.AddRange(pts);
        }

        void RainbowRoads(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            chromeM = WorldMaterials.Solid(new Color(.82f, .84f, .88f), .9f); canvasM = WorldMaterials.Solid(new Color(.96f, .94f, .88f)); deepBlue = WorldMaterials.Glow(new Color(.1f, .25f, .65f), .6f);
            roadPts.Clear(); mainRoads.Clear(); roadWalk = 1; rootT = ctx.Root;
            plife = new GameObject("Wilds places life").AddComponent<PsychPlacesLife>(); plife.transform.SetParent(life.transform, false);
            prng = new System.Random(1967);
            RainbowRoad(ctx, mk, Smooth(ctx, Horseshoe, false));
            var c = new Vector2(center.x, center.z);
            foreach (var s in Spokes) { var to = Horseshoe[s]; var dir = (to - c).normalized; var side = new Vector2(-dir.y, dir.x); RainbowRoad(ctx, mk, Smooth(ctx, new[] { c + dir * 12f, Vector2.Lerp(c, to, .5f) + side * 10f, to }, false), false); }
            foreach (var (at, r) in PlaceRings) noGrass.Add(new Vector3(at.x, r, at.y));
            // the car that drives the long east straight, for ever
            var drive = new List<Vector3>(); foreach (var p in mainRoads[0]) if (p.x > 6340f && p.z > 3330f && p.z < 3770f) drive.Add(p + Vector3.up * .1f);
            if (drive.Count > 10)
            {
                var car = new MeshKit(); var paint = WorldMaterials.Solid(new Color(.9f, .5f, .15f));
                car.Box(paint, new Vector3(0, .6f, 0), new Vector3(1.7f, .5f, 4f), Quaternion.identity); car.Box(paint, new Vector3(0, 1.05f, -.2f), new Vector3(1.6f, .5f, 2f), Quaternion.identity); car.Box(dark, new Vector3(0, 1.07f, -.2f), new Vector3(1.62f, .34f, 1.7f), Quaternion.identity);
                foreach (var e in new[] { -1f, 1f }) { car.Blob(GlowM(new Color(1f, .95f, .8f), 2.4f), new Vector3(e * .6f, .62f, 2.02f), Vector3.one * .13f, prng, 0f); car.Blob(GlowM(new Color(1f, .2f, .15f), 2f), new Vector3(e * .6f, .62f, -2.02f), Vector3.one * .1f, prng, 0f); foreach (var z in new[] { -1.3f, 1.3f }) car.Prism(dark, new Vector3(e * .86f, .34f, z), Vector3.right * e, .34f, .2f, 10); }
                var cgo = car.BuildSingle("The car on the long road", plife.transform, false); cgo.transform.position = drive[0]; plife.car = cgo.transform; plife.drive = drive.ToArray();
            }
        }

        void Lane(LandContext ctx, MeshKit mk, Vector3 to, float stopShort = 0f)
        {
            var from = Nearest(roadPts, to); var d = new Vector2(to.x - from.x, to.z - from.z); var len = d.magnitude; if (len < stopShort + RoadW) return; d /= len;
            var a = new Vector2(from.x, from.z) + d * (RoadW / 2f + .1f); var b = new Vector2(from.x, from.z) + d * (len - stopShort); var side = new Vector2(-d.y, d.x);
            var pts = Smooth(ctx, new[] { a, Vector2.Lerp(a, b, .5f) + side * Mathf.Min(7f, len * .1f), b }, false, 2f, 1f);
            if (pts.Count < 2) return;
            Bend(ctx, pts); Strip(mk, pts, 2.6f, .05f, roadWalk + 2); foreach (var p in pts) Remember(p, 900);
        }

        void Places(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            Oasis(ctx, mk, solid); RecordShack(ctx, mk, solid); Motorik(ctx, mk, solid); FuzzField(ctx, mk, solid); LoveIn(ctx, mk, solid);
            SoundGarden(ctx, mk, solid); LightShow(ctx, mk, solid); MirrorSpiral(ctx, mk, solid); TeaHouse(ctx, mk, solid); HighPoint(ctx, mk, solid);
        }

        void Palm(LandContext ctx, MeshKit mk, Vector3 at, float h)
        {
            var up = Vector3.up; var g = ctx.OnGround(at.x, at.z); var lean = (up + new Vector3(Mathf.Sin(at.x), 0, Mathf.Cos(at.z)) * .15f).normalized;
            mk.Prism(wood, g - up * .3f, lean, .28f, h, 7, .6f); ArtistBits.PostCollider(ctx.Root, g, h * .7f, .3f); var top = g + lean * h;
            for (var k = 0; k < 7; k++) { var d = Quaternion.Euler(0, k * 51f, 0) * Vector3.forward; soft.Box(WorldMaterials.Solid(k % 2 == 0 ? new Color(.2f, .6f, .35f) : new Color(.3f, .7f, .3f)), top + d * 1.5f - up * .3f, new Vector3(.7f, .08f, 3f), Quaternion.LookRotation(d - up * .35f)); }
        }

        // the Oasis: a deep blue pool under palms, a tea tent of carpets and cushions, hammocks
        void Oasis(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var g = ctx.OnGround(OasisAt.x, OasisAt.y); var f = new PlaceFrame(g, FaceTo(g, Nearest(roadPts, g))); const float PR = 8f;
            mk.Prism(WorldMaterials.Solid(new Color(.98f, .9f, .7f)), g - up * .4f, up, PR + 1f, .48f, 22); mk.Prism(deepBlue, g + up * .09f, up, PR, .02f, 22); solid.Prism(sandM, g - up * .6f, up, PR + 1f, .72f, 22);
            ArtistBits.PointLight(ctx.Root, g + up * 2f, new Color(.3f, .55f, 1f), 16f, 1.4f);
            for (var k = 0; k < 7; k++) { var a = k * 51f + 20f; var p = g + Quaternion.Euler(0, a, 0) * Vector3.forward * (PR + 2.5f + (k % 2) * 1.5f); Palm(ctx, mk, p, 6.5f + (k % 3)); }
            // the tea tent: four poles, a striped roof, carpets, cushions round a low brass table
            var t = f.P(0, 0, PR + 9f); t = ctx.OnGround(t.x, t.z); var tf = new PlaceFrame(t + up * .05f, f.Fwd);
            foreach (var (x, z) in new[] { (-3f, -2.5f), (3f, -2.5f), (3f, 2.5f), (-3f, 2.5f) }) { mk.Prism(wood, tf.P(x, -.3f, z), up, .1f, 3.2f, 6); ArtistBits.PostCollider(ctx.Root, tf.P(x, 0, z), 3f, .12f); }
            for (var k = 0; k < 8; k++) mk.Box(WorldMaterials.Solid(Rainbow[k % Rainbow.Length], .3f), tf.P(-2.8f + k * .8f, 3f + Mathf.Sin(k * .45f) * .12f, 0), new Vector3(.8f, .06f, 5.8f), f.r);
            for (var k = 0; k < 3; k++) mk.Box(WorldMaterials.Solid(Bands[k * 2 % Bands.Length]), tf.P(-2f + k * 2f, .02f + k * .012f, 0), new Vector3(2.2f, .03f, 4.6f), f.r);
            mk.Prism(WorldMaterials.Solid(new Color(.85f, .68f, .3f), .85f), tf.P(0, .05f, 0), up, .6f, .3f, 12); ArtistBits.PostCollider(ctx.Root, tf.P(0, 0, 0), .4f, .6f);
            for (var k = 0; k < 6; k++) { var a = k * 60f; var p = tf.P(Mathf.Sin(a * Mathf.Deg2Rad) * 1.7f, 0, Mathf.Cos(a * Mathf.Deg2Rad) * 1.7f); if (k != 0) Cushion(mk, p, FaceTo(p, tf.o), k); }
            var pot = new MeshKit(); var brass = WorldMaterials.Solid(new Color(.85f, .68f, .3f), .85f); pot.Blob(brass, new Vector3(0, .12f, 0), new Vector3(.14f, .14f, .14f), prng, 0f); pot.Box(brass, new Vector3(0, .16f, .18f), new Vector3(.03f, .03f, .16f), Quaternion.Euler(-25f, 0, 0)); pot.Blob(brass, new Vector3(0, .28f, 0), Vector3.one * .04f, prng, 0f);
            var pgo = pot.BuildSingle("Teapot", plife.transform, false); pgo.transform.position = tf.P(0, .36f, 0); Interact.Carry(pgo, "the teapot", .5f, g.y - 6f).GetComponent<Rigidbody>().isKinematic = true;
            Folk(tf.P(0, 0, 1.7f), -f.Fwd, Resident.Pose.Sit, new[] { "Sit. The tea's been brewing since the last time you were here.", "Three cups: one for the road, one for the sky, one for you." }, Look(Residents.Tool.Mug), null, .1f);
            ArtistBits.PointLight(ctx.Root, tf.P(0, 2.4f, 0), new Color(1f, .8f, .5f), 11f, 1.3f); mk.Blob(GlowM(new Color(1f, .8f, .5f), 2f), tf.P(0, 2.7f, 0), Vector3.one * .14f, ctx.Rng, 0f);
            // hammocks between palms
            for (var k = 0; k < 2; k++)
            {
                var hc = f.P(-11f + k * 22f, 0, 2f); var d = Quaternion.Euler(0, 40f + k * 80f, 0) * Vector3.forward; var a = ctx.OnGround(hc.x - d.x * 3f, hc.z - d.z * 3f); var b = ctx.OnGround(hc.x + d.x * 3f, hc.z + d.z * 3f);
                Palm(ctx, mk, a, 7f); Palm(ctx, mk, b, 6.5f); var cloth = WorldMaterials.Solid(Rainbow[k * 3 + 1], .3f);
                for (var q = 0; q < 6; q++) { float t0 = .12f + q * .127f, t1 = t0 + .127f; var p0 = Vector3.Lerp(a, b, t0) + up * (1.3f - Mathf.Sin(t0 * Mathf.PI) * .7f); var p1 = Vector3.Lerp(a, b, t1) + up * (1.3f - Mathf.Sin(t1 * Mathf.PI) * .7f); mk.Box(cloth, (p0 + p1) / 2f, new Vector3(1f, .05f, Vector3.Distance(p0, p1) + .02f), Quaternion.LookRotation(p1 - p0)); Col((p0 + p1) / 2f, new Vector3(1f, .3f, Vector3.Distance(p0, p1)), Quaternion.LookRotation(p1 - p0)); }
                Interact.Seat(plife.transform, (a + b) / 2f + up * .72f, Vector3.Cross(up, d), "Lie in the hammock").seatHeight = .2f;
            }
            Folk(g + f.Right * (PR + 1.6f), -f.Right, Resident.Pose.Stand, new[] { "The water's blue all the way down. I checked. Twice.", "Mirage? No. Well. Maybe a bit." });
            Sign(ctx, mk, f.P(5f, 0, PR + 15f), f.Fwd, "THE OASIS");
            Lane(ctx, mk, f.P(0, 0, PR + 13f)); Ring(g, 20f);
            Shot(ctx, "the oasis", f.P(9f, 2.4f, PR + 20f), g + up * 1f + f.Fwd * 4f);
        }

        // the Record Shack: records to throw, a giant one turning on the roof
        void RecordShack(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var g = ctx.OnGround(ShackAt.x, ShackAt.y); var f = new PlaceFrame(g, FaceTo(g, Nearest(roadPts, g))); const float W = 6f, D = 4.6f, H = 3f;
            mk.Box(wood, g + up * (H / 2f - .3f), new Vector3(W, H + .6f, D), f.r); solid.Box(wood, g + up * H / 2f, new Vector3(W, H, D), f.r);
            for (var k = 0; k < 12; k++) mk.Box(WorldMaterials.Solid(Rainbow[k % Rainbow.Length]), f.P(-W / 2f + .25f + k * .5f, H / 2f + Mathf.Sin(k * .8f) * .3f, D / 2f + .02f), new Vector3(.5f, H - .7f, .04f), f.r);
            mk.Box(dark, f.P(1.8f, 1.1f, D / 2f + .05f), new Vector3(1f, 2.2f, .04f), f.r); mk.Box(GlowM(new Color(1f, .85f, .55f), 1.6f), f.P(-1f, 1.6f, D / 2f + .05f), new Vector3(2.4f, 1.1f, .04f), f.r);
            mk.Box(dark, f.P(0, H + .12f, .2f), new Vector3(W + .6f, .2f, D + 1.2f), f.r);
            var rec = new MeshKit(); rec.Prism(dark, Vector3.zero, Vector3.up, 2.2f, .1f, 24); rec.Prism(WorldMaterials.Solid(Rainbow[0]), new Vector3(0, .1f, 0), Vector3.up, .7f, .02f, 16); rec.Box(white, new Vector3(1.4f, .11f, 0), new Vector3(.9f, .01f, .06f), Quaternion.identity);
            var rgo = rec.BuildSingle("Giant record", plife.transform, false); rgo.transform.position = g + up * (H + .9f); plife.record = rgo.transform; mk.Prism(metal, g + up * (H + .2f), up, .1f, .7f, 6);
            ArtistBits.Label(ctx.Root, "RECORDS", f.P(0, H + .55f, D / 2f + .62f), f.Fwd, .5f, Rainbow[2], TextAnchor.MiddleCenter);
            // crates of records out front: pick one up and throw it
            for (var c = 0; c < 2; c++)
            {
                var cr = f.P(-2.2f + c * 1.5f, 0, D / 2f + 1.3f); cr = ctx.OnGround(cr.x, cr.z); mk.Box(wood, cr + up * .25f, new Vector3(.9f, .5f, .7f), f.r); Col(cr + up * .25f, new Vector3(.9f, .5f, .7f), f.r);
                for (var k = 0; k < 3; k++)
                {
                    var dk = new MeshKit(); dk.Prism(dark, Vector3.zero, Vector3.up, .26f, .02f, 14); dk.Prism(WorldMaterials.Solid(Rainbow[(c * 3 + k) % Rainbow.Length]), new Vector3(0, .02f, 0), Vector3.up, .09f, .005f, 10);
                    var go = dk.BuildSingle("Record", plife.transform, false); go.transform.SetPositionAndRotation(cr + up * (.56f + k * .03f) + f.Right * (-.2f + k * .2f), Quaternion.Euler(0, k * 40f, 0));
                    var cc = Interact.Carry(go, "a record", .15f, g.y - 6f); cc.GetComponent<BoxCollider>().size = new Vector3(.5f, .05f, .5f); var rb = cc.GetComponent<Rigidbody>(); rb.linearDamping = .5f; rb.angularDamping = .1f; rb.isKinematic = true;
                }
            }
            var ch = f.P(3.8f, 0, D / 2f + 1.6f); ch = ctx.OnGround(ch.x, ch.z); var cm = WorldMaterials.Solid(Rainbow[4], .3f); mk.Box(cm, ch + up * .25f, new Vector3(.9f, .5f, .9f), f.r); mk.Box(cm, ch + up * .8f - f.Fwd * .38f, new Vector3(.9f, .7f, .15f), f.r); Col(ch + up * .3f - f.Fwd * .38f, new Vector3(.9f, 1.4f, .2f), f.r);
            Interact.Seat(plife.transform, ch + up * .52f, f.Fwd, "Sit and listen");
            Folk(f.P(1.8f, 0, D / 2f + .8f), f.Fwd, Resident.Pose.Lean, new[] { "Everything's first pressing. I pressed them this morning.", "Go on, throw one. They come back. Records always come back.", "This one's just twenty minutes of a fridge. It's very good." }, Look());
            ArtistBits.PointLight(ctx.Root, f.P(0, 2.6f, D / 2f + 2f), new Color(1f, .85f, .55f), 12f, 1.3f);
            Lane(ctx, mk, f.P(0, 0, D / 2f + 3f)); Ring(g, 8f);
            Shot(ctx, "the record shack", f.P(4f, 1.8f, D / 2f + 10f), g + up * 2f);
        }

        // MOTORIK: a lone petrol station on the long straight road
        void Motorik(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var g = ctx.OnGround(MotorikAt.x, MotorikAt.y); var f = new PlaceFrame(g, FaceTo(g, Nearest(roadPts, g))); var cold = new Color(.75f, .9f, 1f);
            mk.Prism(WorldMaterials.Solid(new Color(.55f, .55f, .58f)), g - up * .4f, up, 10f, .46f, 20);
            foreach (var e in new[] { -3.5f, 3.5f }) { mk.Prism(chromeM, f.P(e, 0, 2f), up, .2f, 4.6f, 8); ArtistBits.PostCollider(ctx.Root, f.P(e, 0, 2f), 4.5f, .22f); }
            mk.Box(white, f.P(0, 4.75f, 2f), new Vector3(10f, .35f, 5f), f.r); mk.Box(WorldMaterials.Solid(Rainbow[1]), f.P(0, 4.75f, 4.51f), new Vector3(10f, .35f, .04f), f.r);
            foreach (var e in new[] { -2.5f, 0f, 2.5f }) mk.Box(GlowM(cold, 2.6f), f.P(e, 4.55f, 2f), new Vector3(.25f, .06f, 4f), f.r);
            ArtistBits.PointLight(ctx.Root, f.P(0, 4f, 2f), cold, 16f, 2f); halos.Add(f.P(0, 4.4f, 2f), 3f, cold, .35f);
            var pump = f.P(0, 0, 2f); mk.Box(WorldMaterials.Solid(Rainbow[1]), pump + up * .9f, new Vector3(.8f, 1.8f, .5f), f.r); mk.Box(GlowM(cold, 1.4f), pump + up * 1.4f + f.Fwd * .26f, new Vector3(.5f, .35f, .02f), f.r); Col(pump + up * .9f, new Vector3(.8f, 1.8f, .5f), f.r);
            var kiosk = f.P(-1f, 0, -4.5f); mk.Box(white, kiosk + up * 1.4f, new Vector3(5f, 2.8f, 3f), f.r); solid.Box(white, kiosk + up * 1.4f, new Vector3(5f, 2.8f, 3f), f.r); mk.Box(GlowM(new Color(1f, .9f, .7f), 1.5f), kiosk + up * 1.6f + f.Fwd * 1.51f, new Vector3(3f, 1.1f, .04f), f.r);
            ArtistBits.Label(ctx.Root, "MOTORIK", f.P(0, 4.76f, 4.54f), f.Fwd, .28f, Color.white, TextAnchor.MiddleCenter); ArtistBits.Label(ctx.Root, "open, for ever", kiosk + up * 2.5f + f.Fwd * 1.52f, f.Fwd, .16f, new Color(.2f, .2f, .25f), TextAnchor.MiddleCenter);
            // a vending machine that gives you something
            var vm = f.P(3f, 0, -3.4f); var vglow = new Material(WorldMaterials.Glow(Rainbow[4], .8f)) { name = "Vending" }; mk.Box(WorldMaterials.Solid(Rainbow[4]), vm + up * 1f, new Vector3(1f, 2f, .7f), f.r); mk.Box(vglow, vm + up * 1.2f + f.Fwd * .36f, new Vector3(.8f, 1.2f, .02f), f.r); Col(vm + up * 1f, new Vector3(1f, 2f, .7f), f.r);
            var vgo = new GameObject("Vending machine"); vgo.transform.SetParent(plife.transform, false); vgo.transform.position = vm + up * 1.1f + f.Fwd * .5f; var vn = 0;
            var vends = new[] { "Clunk. A can. It's empty. It's a nice can.", "Clunk. A can of 'Neu'. It tastes of 1972.", "Clunk. Nothing. The machine hums the same note, steadily.", "Clunk. Your coin back, and one more." };
            Interact.Toy(vgo, Toy.Kind.Custom, "Use the vending machine", new Vector3(1.1f, 2f, .6f)).onUse = _ => Say(vgo.transform, vm + up * 2.4f, vends[vn++ % vends.Length]);
            var bn = f.P(-5.5f, 0, 1f); mk.Box(chromeM, bn + up * .45f, new Vector3(.45f, .07f, 2f), f.r); Col(bn + up * .25f, new Vector3(.45f, .5f, 2f), f.r); Interact.Bench(plife.transform, bn + up * .5f - f.Fwd * .7f, bn + up * .5f + f.Fwd * .7f, f.Right);
            Folk(f.P(2f, 0, 1f), f.Fwd, Resident.Pose.Stand, new[] { "One car a day. Same car. Never stops.", "Steady. That's the whole job. Steady.", "The road's dead straight. People find that relaxing. Or they don't come back." },
                new Residents.Look { top = new Color(.25f, .4f, .65f), bottom = new Color(.25f, .4f, .65f), hat = Residents.Hat.Cap, hatColor = Rainbow[1], skin = new Color(.88f, .68f, .52f), hair = new Color(.6f, .45f, .25f) });
            Lane(ctx, mk, f.P(0, 0, 9f)); Ring(g, 11f);
            Shot(ctx, "the Motorik station", f.P(7f, 1.8f, 14f), f.P(0, 2.4f, 0));
        }

        // the Fuzz Field: giant pedals to stomp, a wall of amps that answers
        void FuzzField(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var g = ctx.OnGround(FuzzAt.x, FuzzAt.y); var f = new PlaceFrame(g, FaceTo(g, Nearest(roadPts, g)));
            var grille = new Material(WorldMaterials.Glow(new Color(1f, .45f, .15f), .2f)) { name = "Amp grilles" }; plife.grille = grille;
            for (var k = 0; k < 6; k++)
            {
                var a = f.P(-8.5f + k * 3.4f, 0, -6f - Mathf.Abs(k - 2.5f) * .5f); a = ctx.OnGround(a.x, a.z); var h = 3.2f + (k % 2) * 1.5f;
                mk.Box(dark, a + up * h / 2f, new Vector3(2.6f, h, 1.2f), f.r); solid.Box(dark, a + up * h / 2f, new Vector3(2.6f, h, 1.2f), f.r); mk.Box(grille, a + up * (h / 2f - .2f) + f.Fwd * .61f, new Vector3(2.2f, h - .9f, .02f), f.r); mk.Box(chromeM, a + up * (h - .3f) + f.Fwd * .62f, new Vector3(2.2f, .25f, .02f), f.r);
            }
            plife.ampLight = ArtistBits.PointLight(ctx.Root, f.P(0, 3f, -3f), new Color(1f, .5f, .2f), 20f, .6f);
            string[] names = { "FUZZ", "WAH", "PHASE", "ECHO", "DOOM" };
            for (var k = 0; k < 5; k++)
            {
                var p = f.P(-7f + k * 3.5f, 0, 2f); p = ctx.OnGround(p.x, p.z); var body = WorldMaterials.Solid(Rainbow[k], .5f); var led = new Material(WorldMaterials.Glow(new Color(1f, .15f, .1f), .15f)) { name = "Pedal LED" };
                var pd = new MeshKit(); pd.Box(body, new Vector3(0, .3f, 0), new Vector3(2f, .6f, 3f), Quaternion.Euler(-6f, 0, 0)); pd.Box(chromeM, new Vector3(0, .62f, -.7f), new Vector3(1.1f, .1f, 1f), Quaternion.Euler(-6f, 0, 0));
                foreach (var e in new[] { -.55f, 0f, .55f }) pd.Prism(dark, new Vector3(e, .68f, .9f), Vector3.up, .2f, .18f, 10); pd.Blob(led, new Vector3(0, .72f, .3f), Vector3.one * .12f, prng, 0f);
                var go = pd.BuildSingle("Giant pedal", plife.transform, false); go.transform.SetPositionAndRotation(p, f.r); var bc = go.AddComponent<BoxCollider>(); bc.center = new Vector3(0, .35f, 0); bc.size = new Vector3(2f, .7f, 3f);
                var t = go.AddComponent<Toy>(); t.kind = Toy.Kind.Glow; t.verb = "Stomp " + names[k]; t.glow = led; t.glowColor = new Color(1f, .15f, .1f); plife.pedals.Add(t);
                ArtistBits.Label(ctx.Root, names[k], p + f.r * new Vector3(0, .68f, -.1f), up, .3f, Color.white, TextAnchor.MiddleCenter).transform.rotation = Quaternion.LookRotation(-up, f.Fwd);
            }
            Folk(f.P(8f, 0, -2f), -f.Right, Resident.Pose.Lean, new[] { "Stomp them all. See what the amps think.", "It goes to eleven. Then it keeps going. We've lost people.", "I'm the roadie. There's no band. Never was. Great rig, though." }, Look());
            Sign(ctx, mk, f.P(-9f, 0, 6f), f.Fwd, "THE FUZZ FIELD");
            Lane(ctx, mk, f.P(0, 0, 7f)); Ring(g, 13f);
            Shot(ctx, "the fuzz field", f.P(5f, 2.2f, 13f), f.P(0, 1.6f, -3f));
        }

        // the Love-In: painted vans round a fire, rugs to sit on
        void LoveIn(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var g = ctx.OnGround(LoveAt.x, LoveAt.y); var f = new PlaceFrame(g, FaceTo(g, Nearest(roadPts, g)));
            for (var k = 0; k < 8; k++) mk.Blob(WorldMaterials.Solid(new Color(.5f, .45f, .42f)), g + Quaternion.Euler(0, k * 45f, 0) * Vector3.forward * 1f + up * .15f, new Vector3(.28f, .2f, .25f), ctx.Rng, .3f);
            mk.Blob(GlowM(new Color(1f, .55f, .2f), 2.6f), g + up * .6f, new Vector3(.45f, .7f, .45f), ctx.Rng, .3f); var fl = ArtistBits.PointLight(ctx.Root, g + up * 1.3f, new Color(1f, .6f, .25f), 15f, 2f); fl.gameObject.AddComponent<FireFlicker>().baseIntensity = 2f; halos.Add(g + up * .7f, 2.4f, new Color(1f, .55f, .2f), .7f); ArtistBits.PostCollider(ctx.Root, g, .9f, 1.1f);
            for (var k = 0; k < 4; k++) { var a = k * 90f + 135f; var p = g + f.r * (Quaternion.Euler(0, a, 0) * Vector3.forward) * 12f; if (Mathf.Abs(Mathf.DeltaAngle(a, 0f)) < 30f) continue; Camper(ctx, mk, solid, ctx.OnGround(p.x, p.z), k); }
            for (var k = 0; k < 5; k++)
            {
                var a = k * 72f + 36f; var d = f.r * (Quaternion.Euler(0, a, 0) * Vector3.forward); var p = ctx.OnGround(g.x + d.x * 4.2f, g.z + d.z * 4.2f);
                mk.Box(WorldMaterials.Solid(Bands[k % Bands.Length]), p + up * .03f, new Vector3(2f, .03f, 1.5f), Quaternion.LookRotation(d)); Cushion(mk, p - Vector3.Cross(up, d) * .5f, -d, k, "Sit by the fire");
                if (k == 1) Folk(p + Vector3.Cross(up, d) * .5f + up * .1f, -d, Resident.Pose.Sit, new[] { "We're all one, man. Except Dave. Dave's two.", "This song's about the sun. They're all about the sun." }, null, null, .1f);
            }
            foreach (var e in new[] { 1, 3 }) { var d = f.r * (Quaternion.Euler(0, e * 72f + 60f, 0) * Vector3.forward); Folk(ctx.OnGround(g.x + d.x * 7f, g.z + d.z * 7f), -d, Resident.Pose.Wave, new[] { "Dance! Or sway. Swaying counts.", "I've got flowers in my hair. I don't know who put them there." }); }
            for (var k = 0; k < 6; k++) { var a0 = k * 60f; var p0 = ctx.OnGround(g.x + Mathf.Cos(a0 * Mathf.Deg2Rad) * 8f, g.z + Mathf.Sin(a0 * Mathf.Deg2Rad) * 8f); mk.Prism(wood, p0 - up * .3f, up, .08f, 3.2f, 6); ArtistBits.PostCollider(ctx.Root, p0, 3f, .1f); var p1 = ctx.OnGround(g.x + Mathf.Cos((a0 + 60f) * Mathf.Deg2Rad) * 8f, g.z + Mathf.Sin((a0 + 60f) * Mathf.Deg2Rad) * 8f); for (var s = 1; s < 9; s++) { var t = s / 9f; mk.Blob(GlowM(Rainbow[(k + s) % Rainbow.Length], 2.2f), Vector3.Lerp(p0, p1, t) + up * (2.9f - Mathf.Sin(t * Mathf.PI) * .6f), Vector3.one * .1f, ctx.Rng, 0f); } }
            Sign(ctx, mk, f.P(-4f, 0, 14f), f.Fwd, "LOVE-IN (all day)");
            Lane(ctx, mk, f.P(0, 0, 10f)); Ring(g, 14f);
            Shot(ctx, "the love-in", f.P(6f, 2.2f, 16f), g + up * 1f);
        }

        // the Sound Garden: a ring of giant chimes to strike
        void SoundGarden(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var g = ctx.OnGround(SoundAt.x, SoundAt.y); var f = new PlaceFrame(g, FaceTo(g, Nearest(roadPts, g)));
            for (var k = 0; k < 9; k++)
            {
                var a = (k * 34f - 136f) * Mathf.Deg2Rad; var d = f.r * new Vector3(Mathf.Sin(a), 0, -Mathf.Cos(a)); var p = ctx.OnGround(g.x + d.x * 6.5f, g.z + d.z * 6.5f); var h = 2.2f + k * .45f; var col = Rainbow[k % Rainbow.Length];
                var glow = new Material(WorldMaterials.Glow(col, .35f)) { name = "Chime" };
                foreach (var e in new[] { -.6f, .6f }) mk.Prism(wood, p + Vector3.Cross(up, d) * e - up * .3f, up, .08f, h + .9f, 6); mk.Box(wood, p + up * (h + .5f), new Vector3(1.4f, .1f, .1f), Quaternion.LookRotation(d)); Col(p + up * (h / 2f + .3f), new Vector3(1.4f, h + .6f, .2f), Quaternion.LookRotation(d));
                var bar = new MeshKit(); bar.Prism(glow, new Vector3(0, -h + .3f, 0), Vector3.up, .16f, h - .5f, 8); bar.Box(dark, new Vector3(0, -.1f, 0), new Vector3(.03f, .3f, .03f), Quaternion.identity);
                var go = bar.BuildSingle("Giant chime", plife.transform, false); go.transform.SetPositionAndRotation(p + up * (h + .45f), Quaternion.LookRotation(d));
                var t = Interact.Toy(go, Toy.Kind.Custom, "Strike the chime", new Vector3(.9f, h, .9f)); go.GetComponent<BoxCollider>().center = new Vector3(0, -h / 2f, 0); var idx = plife.chimes.Count; plife.chimes.Add((go.transform, glow, col, -10f)); t.onUse = _ => plife.Strike(idx);
            }
            plife.chimeLight = ArtistBits.PointLight(ctx.Root, g + up * 3f, Color.white, 16f, .8f);
            mk.Prism(WorldMaterials.Solid(Bands[3]), g - up * .3f, up, 1.6f, .75f, 10); solid.Prism(sandM, g - up * .3f, up, 1.6f, .75f, 10); for (var k = 0; k < 4; k++) Interact.Seat(plife.transform, g + Quaternion.Euler(0, k * 90f, 0) * Vector3.forward * 1f + up * .5f, Quaternion.Euler(0, k * 90f, 0) * Vector3.forward, "Sit and listen");
            Folk(f.P(4f, 0, 5f), -f.Fwd, Resident.Pose.Wave, new[] { "Strike them low to high. Or don't. It's all a scale if you squint.", "I'm conducting. They don't know it, but I am." });
            Sign(ctx, mk, f.P(-5f, 0, 8f), f.Fwd, "THE SOUND GARDEN");
            Lane(ctx, mk, f.P(0, 0, 7f)); Ring(g, 10f);
            Shot(ctx, "the sound garden", f.P(3f, 2f, 13f), g + up * 2.4f);
        }

        // the Light Show: a white dome full of drifting colour, beanbags
        void LightShow(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var g = ctx.OnGround(DomeAt.x, DomeAt.y); var f = new PlaceFrame(g, FaceTo(g, Nearest(roadPts, g))); const float R = 7f; const int N = 12;
            mk.Prism(WorldMaterials.Solid(new Color(.2f, .15f, .3f)), g - up * .4f, up, R + .3f, .5f, N * 2); solid.Prism(sandM, g - up * .6f, up, R + .3f, .7f, N * 2);
            var fo = g + up * .1f;
            for (var k = 0; k < N; k++)
            {
                float a0 = k * 360f / N, a1 = (k + 1) * 360f / N; var door = Mathf.Abs(Mathf.DeltaAngle((a0 + a1) / 2f, 0f)) < 20f;
                for (var s = 0; s < 4; s++)
                {
                    float e0 = s * 22.5f * Mathf.Deg2Rad, e1 = (s + 1) * 22.5f * Mathf.Deg2Rad; if (door && s < 2) continue;
                    Vector3 P(float az, float el) => fo + f.r * (Quaternion.Euler(0, az, 0) * Vector3.forward) * Mathf.Cos(el) * R + up * Mathf.Sin(el) * R;
                    Vector3 q0 = P(a0, e0), q1 = P(a1, e0), q2 = P(a1, e1), q3 = P(a0, e1);
                    mk.Quad(canvasM, q0, q1, q2, q3, fo); mk.Quad(WorldMaterials.Solid(new Color(.25f, .2f, .35f)), q3, q2, q1, q0, fo + (q0 - fo) * 2f);
                }
                if (!door) { var d = f.r * (Quaternion.Euler(0, (a0 + a1) / 2f, 0) * Vector3.forward); Col(fo + d * (R - .3f) + up * 1.6f, new Vector3(R * .56f, 3.2f, .5f), Quaternion.LookRotation(d)); }
            }
            IndoorSpaces.Add(fo + up * 2.5f, f.r, new Vector3(R * 1.3f, 5f, R * 1.3f));
            // blobs of light drifting on the inside of the dome
            for (var k = 0; k < 7; k++) { var bm = new MeshKit(); bm.Blob(GlowM(Rainbow[k % Rainbow.Length], 2.2f), Vector3.zero, new Vector3(.9f, .7f, .9f) * (.7f + (k % 3) * .25f), prng, .35f); var go = bm.BuildSingle("Light blob", plife.transform, false); go.transform.position = fo + up * 4f; plife.blobs.Add((go.transform, k * 1.3f)); }
            plife.domeAt = fo; plife.domeR = R - .9f; plife.domeLight = ArtistBits.PointLight(ctx.Root, fo + up * 3.4f, Rainbow[4], 12f, 2.2f, true);
            for (var k = 0; k < 6; k++) { var a = k * 60f + 30f; var p = fo + Quaternion.Euler(0, a, 0) * Vector3.forward * 3.4f; mk.Blob(WorldMaterials.Solid(Rainbow[k], .3f), p + up * .3f, new Vector3(.7f, .32f, .7f), prng, .15f); Interact.Seat(plife.transform, p + up * .5f, FaceTo(p, fo), "Sink into the beanbag").seatHeight = .4f; }
            var pj = fo; mk.Prism(metal, pj, up, .06f, 1.1f, 6); mk.Box(dark, pj + up * 1.2f, new Vector3(.5f, .35f, .7f), f.r); ArtistBits.PostCollider(ctx.Root, pj, 1.2f, .3f);
            var pgo = new GameObject("Projector"); pgo.transform.SetParent(plife.transform, false); pgo.transform.position = pj + up * 1.2f; Interact.Toy(pgo, Toy.Kind.Custom, "Change the slide", new Vector3(.8f, .7f, .9f)).onUse = _ => plife.NextSlide();
            Folk(fo + Quaternion.Euler(0, 90f, 0) * Vector3.forward * 3.4f + up * .5f, Quaternion.Euler(0, -90f, 0) * Vector3.forward, Resident.Pose.Sit, new[] { "It's just oil and water and a lamp. Don't tell my eyes.", "I've been watching the purple one. It's up to something." }, null, null, .4f);
            Sign(ctx, mk, f.P(4f, 0, R + 3f), f.Fwd, "LIGHT SHOW (now, always)", 3.2f, .15f);
            Lane(ctx, mk, f.P(0, 0, R + 1f)); Ring(g, R + 1.5f);
            Shot(ctx, "the light show dome", f.P(-5f, 1.9f, R + 10f), fo + up * 3f);
            Shot(ctx, "inside the light show", fo + f.Fwd * 4.5f + up * 1.5f, fo + up * 3.2f - f.Fwd * 3f);
        }

        // the Mirror Spiral: walk in to the prism at its heart
        void MirrorSpiral(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var g = ctx.OnGround(MirrorAt.x, MirrorAt.y); var f = new PlaceFrame(g, FaceTo(g, Nearest(roadPts, g)));
            var mirror = WorldMaterials.Solid(new Color(.72f, .85f, .95f), .97f); Vector3 prev = default; var first = true;
            for (var t = 0f; t <= 2.4f * 360f; t += 18f)
            {
                var r = 10f - t / 360f * 3.3f; var d = f.r * (Quaternion.Euler(0, t, 0) * Vector3.forward); var p = ctx.OnGround(g.x + d.x * r, g.z + d.z * r);
                if (!first) { var mid = (prev + p) / 2f; var rot = Quaternion.LookRotation(p - prev); var len = Vector3.Distance(new Vector3(prev.x, 0, prev.z), new Vector3(p.x, 0, p.z)); var lo = Mathf.Min(prev.y, p.y) - .4f; var hi = Mathf.Max(prev.y, p.y) + 2.7f; mk.Box(mirror, new Vector3(mid.x, (lo + hi) / 2f, mid.z), new Vector3(.18f, hi - lo, len + .05f), rot); solid.Box(mirror, new Vector3(mid.x, (lo + hi) / 2f, mid.z), new Vector3(.25f, hi - lo, len), rot); if ((int)(t / 18f) % 4 == 0) mk.Box(GlowM(Rainbow[(int)(t / 72f) % Rainbow.Length], 2f), new Vector3(mid.x, hi + .06f, mid.z), new Vector3(.2f, .1f, len), rot); }
                prev = p; first = false;
            }
            var pr = new MeshKit(); pr.Prism(WorldMaterials.Glow(new Color(.9f, .95f, 1f), 1.6f), new Vector3(0, -.8f, 0), Vector3.up, .7f, 1.6f, 3); for (var k = 0; k < 6; k++) pr.Box(GlowM(Rainbow[k], 2.4f), Quaternion.Euler(0, k * 60f, 0) * new Vector3(0, -.2f + k * .05f, 1.1f), new Vector3(.08f, .04f, 1.4f), Quaternion.Euler(0, k * 60f, 0));
            var pgo = pr.BuildSingle("The prism", plife.transform, false); pgo.transform.position = g + up * 1.8f; var pt = Interact.Toy(pgo, Toy.Kind.Spin, "Spin the prism", new Vector3(1.6f, 2f, 1.6f)); pgo.GetComponent<BoxCollider>().center = new Vector3(0, -.6f, 0); pt.amount = 1080f; pt.seconds = 4f;
            mk.Prism(dark, g - up * .3f, up, .5f, 1.1f, 8); ArtistBits.PostCollider(ctx.Root, g, .9f, .5f); plife.prismLight = ArtistBits.PointLight(ctx.Root, g + up * 2.6f, Color.white, 13f, 2f);
            var entry = g + f.r * Vector3.forward * 11.5f; Folk(ctx.OnGround(entry.x + 2.5f, entry.z), FaceTo(entry, g), Resident.Pose.Stand, new[] { "I went in once. Met several of me. We don't talk now.", "Keep turning left. Or is it right. It's one of the two." });
            Sign(ctx, mk, ctx.OnGround(entry.x - 2.5f, entry.z + 1f), f.Fwd, "THE MIRROR SPIRAL");
            Lane(ctx, mk, entry); Ring(g, 11f);
            Shot(ctx, "the mirror spiral", f.P(8f, 6f, 17f), g + up * 1.5f);
        }

        // the Mushroom Tea House: a round room inside a giant mushroom
        void TeaHouse(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var g = ctx.OnGround(TeaAt.x, TeaAt.y); var f = new PlaceFrame(g, FaceTo(g, Nearest(roadPts, g))); const float R = 3.6f, H = 3.4f; const int N = 14;
            var lo = g.y; for (var k = 0; k < N; k++) lo = Mathf.Min(lo, ctx.Ground(g.x + Mathf.Cos(k * .45f) * (R + .5f), g.z + Mathf.Sin(k * .45f) * (R + .5f)));
            mk.Prism(stalk, new Vector3(g.x, lo - .3f, g.z), up, R + .4f, g.y - lo + .45f, N); solid.Prism(stalk, new Vector3(g.x, lo - .3f, g.z), up, R + .4f, g.y - lo + .43f, N); var fo = g + up * .15f;
            for (var k = 0; k < N; k++)
            {
                var a = k * 360f / N; if (Mathf.Abs(Mathf.DeltaAngle(a, 0f)) < 26f) continue;
                var d = f.r * (Quaternion.Euler(0, a, 0) * Vector3.forward); var rot = Quaternion.LookRotation(d); var w = 2f * Mathf.PI * R / N + .1f;
                mk.Box(stalk, fo + d * R + up * H / 2f, new Vector3(w, H, .35f), rot); solid.Box(stalk, fo + d * R + up * H / 2f, new Vector3(w, H, .35f), rot);
                if (k % 4 == 2) mk.Prism(GlowM(new Color(1f, .85f, .5f), 1.5f), fo + d * (R + .19f) + up * 1.9f, d, .45f, .03f, 12);
            }
            mk.Prism(WorldMaterials.Solid(new Color(1f, .95f, .85f)), fo + up * H, up, R + .2f, .2f, N); mk.Prism(capGlow != null && capGlow.Length > 0 ? capGlow[0] : GlowM(Rainbow[0], 1f), fo + up * (H + .2f), up, R + 3.2f, .25f, 18);
            mk.Cone(WorldMaterials.Solid(Rainbow[0]), fo + up * (H + .4f), R + 3.4f, 3.2f, 18); for (var k = 0; k < 9; k++) { var a = k * 40f; var d = Quaternion.Euler(0, a, 0) * Vector3.forward; mk.Blob(white, fo + up * (H + 1.3f + (k % 3) * .5f) + d * (3.6f - (k % 3) * 1f), new Vector3(.6f, .25f, .6f), ctx.Rng, 0f); }
            solid.Prism(stalk, fo + up * H, up, R + 2.6f, .6f, 12);
            IndoorSpaces.Add(fo + up * H / 2f, f.r, new Vector3(R * 1.6f, H, R * 1.6f));
            mk.Prism(WorldMaterials.Solid(Bands[2]), fo, up, R - .3f, .03f, N);
            for (var k = 0; k < 5; k++) { var a = (k * 52f + 76f); var d = f.r * (Quaternion.Euler(0, a, 0) * Vector3.forward); Cushion(mk, fo + d * (R - 1.1f), -d, k + 1); }
            mk.Prism(wood, fo, up, .7f, .4f, 10); ArtistBits.PostCollider(ctx.Root, fo, .45f, .7f); mk.Blob(GlowM(new Color(1f, .7f, .35f), 2.2f), fo + up * .58f, new Vector3(.16f, .16f, .16f), ctx.Rng, 0f); halos.Add(fo + up * .6f, .8f, new Color(1f, .7f, .35f), .6f);
            ArtistBits.PointLight(ctx.Root, fo + up * 2.4f, new Color(1f, .75f, .5f), 9f, 1.6f, true);
            var back = f.r * (Quaternion.Euler(0, 180f, 0) * Vector3.forward); Folk(fo + back * (R - 1.1f) + up * .36f, -back, Resident.Pose.Sit, new[] { "Mind your head on the gills.", "It grew like this. We just moved in and added a kettle.", "The cap glows when the music's good. So, mostly." }, Look(Residents.Tool.Mug), null, .3f);
            Sign(ctx, mk, f.P(3.5f, 0, R + 3f), f.Fwd, "MUSHROOM TEA HOUSE", 3f, .15f);
            Lane(ctx, mk, f.P(0, 0, R + 1f)); Ring(g, R + 4f);
            Shot(ctx, "the mushroom tea house", f.P(-4f, 1.8f, R + 10f), fo + up * 3f);
        }

        // the High Point: a bench and a kaleidoscope on the hill above the long road
        void HighPoint(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var g = Site(ctx, HighAt, 8f); var f = new PlaceFrame(g, FaceTo(g, center));
            mk.Box(wood, g + up * .45f, new Vector3(2.4f, .08f, .5f), f.r); foreach (var e in new[] { -1f, 1f }) mk.Box(wood, g + up * .22f + f.Right * e, new Vector3(.1f, .45f, .45f), f.r); Col(g + up * .3f, new Vector3(2.4f, .6f, .5f), f.r); Interact.Bench(plife.transform, g + up * .52f - f.Right * .9f, g + up * .52f + f.Right * .9f, f.Fwd);
            var tp = f.P(2.4f, 0, 1.2f); tp = ctx.OnGround(tp.x, tp.z); mk.Prism(metal, tp, up, .06f, 1.3f, 6); ArtistBits.PostCollider(ctx.Root, tp, 1.3f, .1f);
            var kal = new MeshKit(); kal.Prism(chromeM, new Vector3(0, 0, -.5f), Vector3.forward, .12f, 1f, 6); for (var s = 0; s < 6; s++) kal.Blob(GlowM(Rainbow[s], 2.4f), new Vector3(Mathf.Cos(s * 1.047f) * .2f, Mathf.Sin(s * 1.047f) * .2f, .52f), Vector3.one * .07f, prng, 0f);
            var kgo = kal.BuildSingle("Kaleidoscope", plife.transform, false); kgo.transform.SetPositionAndRotation(tp + up * 1.4f, Quaternion.LookRotation(f.Fwd - up * .15f)); var kt = Interact.Toy(kgo, Toy.Kind.Spin, "Turn the kaleidoscope", new Vector3(.6f, .6f, 1.2f)); kt.axis = Vector3.forward; kt.amount = 720f; kt.seconds = 3f;
            LavaLamp(ctx, mk, ctx.OnGround(f.P(-3f, 0, -1f).x, f.P(-3f, 0, -1f).z), .45f, 2);
            Sign(ctx, mk, f.P(-6f, 0, -1f), -f.Fwd, "THE HIGH POINT");
            Lane(ctx, mk, f.P(0, 0, -3f)); Ring(g, 6f);
            Shot(ctx, "the high point", f.P(-4f, 2f, -7f), g + up * .5f + f.Fwd * 30f);
        }
    }

    /// The Wilds places' moving parts: the car on the long road, the record on the roof, the light-show blobs, the
    /// chimes, the amps answering the pedals, the prism's light.
    public class PsychPlacesLife : MonoBehaviour
    {
        public Transform car, record; public Vector3[] drive = new Vector3[0];
        public List<(Transform t, float seed)> blobs = new List<(Transform, float)>(); public Vector3 domeAt; public float domeR; public Light domeLight, chimeLight, ampLight, prismLight;
        public List<(Transform t, Material m, Color c, float struck)> chimes = new List<(Transform, Material, Color, float)>();
        public List<Toy> pedals = new List<Toy>(); public Material grille;
        float[] len = new float[0]; float total; int slide;
        static readonly Color[] Rainbow = { new Color(1f, .3f, .4f), new Color(1f, .6f, .15f), new Color(1f, .9f, .2f), new Color(.35f, .9f, .4f), new Color(.25f, .65f, 1f), new Color(.65f, .4f, 1f) };

        void Start() { if (drive.Length > 1) { len = new float[drive.Length]; for (var i = 1; i < drive.Length; i++) { total += Vector3.Distance(drive[i], drive[i - 1]); len[i] = total; } } }
        public void Strike(int i) { var c = chimes[i]; c.struck = Time.time; chimes[i] = c; if (chimeLight != null) chimeLight.color = c.c; }
        public void NextSlide() { slide++; }

        void Update()
        {
            var t = Time.time;
            if (car != null && total > 20f)
            {
                var period = total / 9f * 2f; var u = (float)(System.DateTime.UtcNow.TimeOfDay.TotalSeconds % period) / period; var back = u > .5f; var d = (back ? 1f - (u - .5f) * 2f : u * 2f) * total;
                var i = 1; while (i < len.Length - 1 && len[i] < d) i++; var k = Mathf.InverseLerp(len[i - 1], len[i], d); var dir = (drive[i] - drive[i - 1]).normalized * (back ? -1f : 1f);
                car.SetPositionAndRotation(Vector3.Lerp(drive[i - 1], drive[i], k), Quaternion.LookRotation(dir));
            }
            if (record != null) record.rotation = Quaternion.Euler(0, t * 33f, 0);
            for (var i = 0; i < blobs.Count; i++)
            {
                var (b, s) = blobs[i]; if (b == null) continue; var az = t * (.11f + (i % 3) * .04f) + s * 2f; var el = .5f + Mathf.Sin(t * .17f + s) * .45f;
                b.position = domeAt + new Vector3(Mathf.Cos(az) * Mathf.Cos(el), Mathf.Sin(el), Mathf.Sin(az) * Mathf.Cos(el)) * domeR; b.localScale = Vector3.one * (1f + Mathf.Sin(t * .6f + s) * .25f);
            }
            if (domeLight != null) { var h = t * .08f + slide * .37f; domeLight.color = Color.Lerp(Rainbow[(int)h % 6], Rainbow[((int)h + 1) % 6], Mathf.Repeat(h, 1f)); }
            var loud = 0f;
            for (var i = 0; i < chimes.Count; i++)
            {
                var (tr, m, c, struck) = chimes[i]; var age = t - struck; var ring = age < 6f ? Mathf.Exp(-age * .7f) : 0f; loud = Mathf.Max(loud, ring);
                m.SetColor("_EmissionColor", c * (.35f + ring * 3.2f)); if (tr != null) tr.localRotation = Quaternion.Euler(Mathf.Sin(age * 9f) * 7f * ring, tr.localEulerAngles.y, 0);
            }
            if (chimeLight != null) chimeLight.intensity = .8f + loud * 3f;
            if (grille != null) { var on = 0; foreach (var p in pedals) if (p != null && p.On) on++; var k = on / Mathf.Max(1f, pedals.Count); var pulse = on > 0 ? 1f + Mathf.Sin(t * (4f + on * 2f)) * .25f : 1f; grille.SetColor("_EmissionColor", new Color(1f, .45f, .15f) * (.2f + k * 3f * pulse)); if (ampLight != null) ampLight.intensity = .6f + k * 3.5f * pulse; }
            if (prismLight != null) { var h = t * .3f; prismLight.color = Color.Lerp(Rainbow[(int)h % 6], Rainbow[((int)h + 1) % 6], Mathf.Repeat(h, 1f)); }
        }
    }
}
