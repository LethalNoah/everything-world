using System.Collections.Generic;
using UnityEngine;
using static EverythingWorld.World.PlaceTools;

namespace EverythingWorld.World
{
    /// The Prog Marches laid out on purpose (the build-land standard), lightly: the owner wants room left here for
    /// the prog still to come, so most of the south stays open. Every artist stays where they were. Three paved
    /// roads run out from the figure of eight: north to the tip of the land, east along the arm toward the Hub, and
    /// south up to the heights. Along them, places to stop, with the people who never left:
    /// the Listening Room (a hi-fi lounge: the sweet-spot armchair, drop the needle and the speakers glow); the
    /// Floating Isles at the north tip (stepping stones up to a bench on a rock that hangs over the sea); the Great
    /// Keyboard (keys the size of doors, each lights a pipe of colour); the Tape Loop (two reel-to-reels and a loop of
    /// tape running round the garden between them); the Ferry Jetty (a bench and a telescope trained on the Hub); the
    /// Tea Garden (a lawn inside a hedge, tea for four, croquet balls to knock about); and the Stair in Seven
    /// (four flights of seven steps round a tower to a bench in the sky).
    public partial class ProgKit
    {
        static readonly Vector2 ListenAt = new Vector2(7200, 4440), IslesAt = new Vector2(7155, 4572), KeysAt = new Vector2(7462, 4150), TapeAt = new Vector2(7690, 4236), JettyAt = new Vector2(7880, 4226),
            TeaAt = new Vector2(7402, 4298), StairAt = new Vector2(7225, 3965);
        static readonly Vector2[] NorthWay = { new Vector2(7170, 4224), new Vector2(7165, 4300), new Vector2(7150, 4400), new Vector2(7140, 4470), new Vector2(7150, 4545) };
        static readonly Vector2[] EastWay = { new Vector2(7322, 4204), new Vector2(7400, 4170), new Vector2(7480, 4172), new Vector2(7570, 4205), new Vector2(7690, 4252), new Vector2(7800, 4250), new Vector2(7862, 4232) };
        static readonly Vector2[] SouthWay = { new Vector2(7170, 4184), new Vector2(7178, 4110), new Vector2(7185, 4040), new Vector2(7215, 3990) };
        static readonly (Vector2 at, float r)[] PlaceRings = { (ListenAt, 10f), (IslesAt, 12f), (KeysAt, 12f), (TapeAt, 12f), (JettyAt, 8f), (TeaAt, 13f), (StairAt, 9f) };

        ProgPlacesLife plife; System.Random prng; Transform rootT;
        readonly List<Vector3> roadPts = new List<Vector3>(); readonly List<List<Vector3>> mainRoads = new List<List<Vector3>>();

        static readonly Color[] Coats = { new Color(.35f, .2f, .5f), new Color(.15f, .2f, .4f), new Color(.45f, .12f, .18f), new Color(.2f, .32f, .3f), new Color(.75f, .7f, .6f), new Color(.6f, .45f, .2f) };
        static readonly Color[] Legs = { new Color(.16f, .16f, .2f), new Color(.3f, .25f, .22f), new Color(.22f, .2f, .28f) };
        static readonly Residents.Hat[] Hats = { Residents.Hat.None, Residents.Hat.None, Residents.Hat.Bucket, Residents.Hat.Beanie };
        static readonly string[] Listeners =
        {
            "This next bit's in thirteen. You'll feel it in your knees.", "Side two is where it gets good. Side two is forty minutes.", "It's not long. It's thorough.",
            "The eclipse? It's been like that since the concept album.", "I came for one song. That was a while ago. It's still going.",
        };

        Residents.Look Look(Residents.Tool tool = Residents.Tool.None) { var l = Residents.Random(prng, Coats, Legs, Hats, Coats); l.tool = tool; l.longHair = prng.NextDouble() < .7; return l; }
        Resident Folk(Vector3 feet, Vector3 face, Resident.Pose pose, string[] lines = null, Residents.Look look = null, Vector3[] route = null, float seatH = HumanScale.SeatHeight)
            => Residents.Make(plife.transform, feet, face, look ?? Look(), pose, prng, lines ?? Listeners, route, seatH);
        void Ring(Vector3 at, float r) => noGrass.Add(new Vector3(at.x, r, at.z));
        void Col(Vector3 c, Vector3 size, Quaternion rot) => BoxCol(rootT, c, size, rot);

        void StoneSign(LandContext ctx, MeshKit mk, Vector3 at, Vector3 face, string text, float w = 2.6f, float size = .17f)
        {
            var up = Vector3.up; var g = ctx.OnGround(at.x, at.z); var rot = Quaternion.LookRotation(face);
            mk.Box(darkStone, g + up * .7f, new Vector3(w, 1.8f, .3f), rot); Col(g + up * .7f, new Vector3(w, 1.8f, .3f), rot);
            ArtistBits.Label(ctx.Root, text, g + up * 1.1f + face * .16f, face, size, new Color(1f, .8f, .5f), TextAnchor.MiddleCenter);
        }

        void SeatBench(LandContext ctx, MeshKit mk, Vector3 at, Vector3 face, float len = 2f)
        {
            var up = Vector3.up; var rot = Quaternion.LookRotation(face); var side = Vector3.Cross(up, face);
            mk.Box(stone, at + up * .25f, new Vector3(len, .5f, .55f), rot); Col(at + up * .25f, new Vector3(len, .5f, .55f), rot);
            Interact.Bench(plife.transform, at + up * .52f - side * (len / 2f - .35f), at + up * .52f + side * (len / 2f - .35f), face);
        }

        // ---------------------------------------------------------------- the roads
        void Bend(LandContext ctx, List<Vector3> pts)
        {
            for (var i = 0; i < pts.Count; i++)
                foreach (var a in ctx.Artists)
                {
                    var r = Foot(a) + 8f; var v = new Vector2(pts[i].x - a.x, pts[i].z - a.z); var d = v.magnitude;
                    if (d < r) { v = d > .01f ? v / d * r : new Vector2(r, 0); pts[i] = ctx.OnGround(a.x + v.x, a.z + v.y); }
                }
        }

        void PavedRoad(LandContext ctx, MeshKit mk, List<Vector3> pts)
        {
            Bend(ctx, pts); if (pts.Count < 2) return;
            Strip(mk, pts, paving, 3.4f, .2f, ctx);
            var up = Vector3.up; var n = 0;
            for (var i = 10; i < pts.Count; i += 20)
            {
                var d = pts[i] - pts[i - 1]; d.y = 0; if (d.sqrMagnitude < .01f) continue; d.Normalize(); var lp = pts[i] + Vector3.Cross(up, d) * (n++ % 2 == 0 ? 2.6f : -2.6f);
                if (ctx.IsLand(lp.x, lp.z, 2f) && !InFootprint(ctx, lp.x, lp.z, 3f)) { Lamp(ctx, mk, ctx.OnGround(lp.x, lp.z)); Ring(lp, 1f); }
            }
            foreach (var p in pts) Remember(p, 350);
            mainRoads.Add(pts); roadPts.AddRange(pts);
        }

        void MarchRoads(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            roadPts.Clear(); mainRoads.Clear(); rootT = ctx.Root;
            plife = new GameObject("Marches places life").AddComponent<ProgPlacesLife>(); plife.transform.SetParent(life.transform, false);
            prng = new System.Random(1973);
            foreach (var way in new[] { NorthWay, EastWay, SouthWay }) PavedRoad(ctx, mk, Smooth(ctx, way, false));
            foreach (var (at, r) in PlaceRings) noGrass.Add(new Vector3(at.x, r, at.y));
        }

        void Lane(LandContext ctx, MeshKit mk, Vector3 to, float stopShort = 0f)
        {
            var from = Nearest(roadPts, to); var d = new Vector2(to.x - from.x, to.z - from.z); var len = d.magnitude; if (len < stopShort + 3.4f) return; d /= len;
            var a = new Vector2(from.x, from.z) + d * 1.8f; var b = new Vector2(from.x, from.z) + d * (len - stopShort); var side = new Vector2(-d.y, d.x);
            var pts = Smooth(ctx, new[] { a, Vector2.Lerp(a, b, .5f) + side * Mathf.Min(6f, len * .1f), b }, false, 1.5f, 1f);
            if (pts.Count < 2) return;
            Bend(ctx, pts); Strip(mk, pts, WorldMaterials.Solid(new Color(.5f, .46f, .42f)), 2.2f, .192f, ctx); foreach (var p in pts) Remember(p, 400);
        }

        void Places(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            ListeningRoom(ctx, mk, solid); FloatingIsles(ctx, mk, solid); GreatKeyboard(ctx, mk, solid); TapeLoop(ctx, mk, solid); FerryJetty(ctx, mk, solid); TeaGarden(ctx, mk, solid); StairInSeven(ctx, mk, solid);
        }

        // the Listening Room: sit in the sweet spot, drop the needle
        void ListeningRoom(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var g = ctx.OnGround(ListenAt.x, ListenAt.y); var f = new PlaceFrame(g, FaceTo(g, Nearest(roadPts, g))); const float W = 8f, D = 7f, H = 3.4f, T = .35f, Door = HumanScale.DoorWidth;
            var amber = new Color(1f, .7f, .3f); var panel = WorldMaterials.Solid(new Color(.3f, .2f, .16f));
            float lo = g.y, hi = g.y; foreach (var (x, z) in new[] { (-1f, -1f), (1f, -1f), (1f, 1f), (-1f, 1f) }) { var h = ctx.Ground(f.P(x * W / 2f, 0, z * D / 2f).x, f.P(x * W / 2f, 0, z * D / 2f).z); lo = Mathf.Min(lo, h); hi = Mathf.Max(hi, h); }
            var fo = new PlaceFrame(new Vector3(g.x, hi + .2f, g.z), f.Fwd);
            mk.Box(darkStone, new Vector3(g.x, (lo + hi) / 2f - .1f, g.z), new Vector3(W + .3f, hi - lo + .7f, D + .3f), f.r);
            void Wall(Vector3 p, Vector3 s) { mk.Box(panel, p, s, f.r); solid.Box(panel, p, s, f.r); }
            mk.Box(WorldMaterials.Solid(new Color(.4f, .15f, .15f)), fo.P(0, -.03f, 0), new Vector3(W - T, .1f, D - T), f.r); solid.Box(panel, fo.P(0, -.15f, 0), new Vector3(W, .3f, D), f.r);
            Wall(fo.P(0, H / 2f, -D / 2f + T / 2f), new Vector3(W, H, T)); foreach (var e in new[] { -1f, 1f }) Wall(fo.P(e * (W / 2f - T / 2f), H / 2f, 0), new Vector3(T, H, D - T * 2f));
            var seg = (W - Door) / 2f; foreach (var e in new[] { -1f, 1f }) Wall(fo.P(e * (Door / 2f + seg / 2f), H / 2f, D / 2f - T / 2f), new Vector3(seg, H, T));
            Wall(fo.P(0, H + .15f, 0), new Vector3(W + .4f, .3f, D + .4f));
            foreach (var e in new[] { -1f, 1f }) mk.Box(GlowM(amber, 1.4f), fo.P(e * (W / 2f + .01f), 1.8f, .8f), new Vector3(.04f, 1.1f, 1.6f), f.r);
            IndoorSpaces.Add(fo.P(0, H / 2f, 0), f.r, new Vector3(W - .7f, H, D - .7f));
            var st = ctx.OnGround(f.P(0, 0, D / 2f + .7f).x, f.P(0, 0, D / 2f + .7f).z); mk.Box(darkStone, new Vector3(st.x, (st.y + fo.o.y) / 2f - .1f, st.z), new Vector3(Door + .6f, fo.o.y - st.y + .2f, 1.3f), f.r); solid.Box(darkStone, new Vector3(st.x, (st.y + fo.o.y) / 2f - .1f, st.z), new Vector3(Door + .6f, fo.o.y - st.y + .2f, 1.3f), f.r);
            // two great speakers on the back wall, their cones glowing
            var cone = new Material(WorldMaterials.Glow(amber, .5f)) { name = "Speaker cones" }; plife.cones = cone;
            foreach (var e in new[] { -1f, 1f }) { var sp = fo.P(e * 2.6f, 0, -D / 2f + T + .55f); mk.Box(dark, sp + up * 1.3f, new Vector3(1.3f, 2.6f, .9f), f.r); Col(sp + up * 1.3f, new Vector3(1.3f, 2.6f, .9f), f.r); mk.Prism(cone, sp + up * 1f + f.Fwd * .46f, f.Fwd, .45f, .03f, 16); mk.Prism(cone, sp + up * 2f + f.Fwd * .46f, f.Fwd, .22f, .03f, 12); }
            // the turntable between them: drop the needle
            var tt = fo.P(0, 0, -D / 2f + T + .5f); mk.Box(panel, tt + up * .45f, new Vector3(1.6f, .9f, .7f), f.r); Col(tt + up * .45f, new Vector3(1.6f, .9f, .7f), f.r);
            var plat = new MeshKit(); plat.Prism(dark, Vector3.zero, Vector3.up, .32f, .03f, 16); plat.Prism(WorldMaterials.Solid(Spectrum[1]), new Vector3(0, .03f, 0), Vector3.up, .1f, .005f, 10); plat.Box(chrome, new Vector3(.3f, .06f, .05f), new Vector3(.03f, .03f, .4f), Quaternion.Euler(0, 25f, 0));
            var pgo = plat.BuildSingle("Turntable", plife.transform, false); pgo.transform.position = tt + up * .92f; plife.platter = pgo.transform;
            Interact.Toy(pgo, Toy.Kind.Custom, "Drop the needle", new Vector3(.9f, .5f, .9f)).onUse = _ => plife.Needle();
            plife.roomLight = ArtistBits.PointLight(ctx.Root, fo.P(0, 2.4f, -1f), amber, 10f, 1.2f, true);
            // shelves of records down one side
            for (var sh = 0; sh < 4; sh++) for (var b = 0; b < 22; b++) mk.Box(WorldMaterials.Solid(Color.Lerp(Spectrum[(b * 5 + sh * 2) % Spectrum.Length], new Color(.2f, .2f, .2f), .45f)), fo.P(-W / 2f + T + .18f, .5f + sh * .6f, -1.8f + b * .16f), new Vector3(.3f, .5f, .12f), f.r);
            Col(fo.P(-W / 2f + T + .18f, 1.4f, -.1f), new Vector3(.4f, 2.6f, 3.8f), f.r);
            // the sweet-spot armchair, and a sofa behind it
            var leather = WorldMaterials.Solid(new Color(.35f, .18f, .12f)); var ac = fo.P(0, 0, .6f);
            mk.Box(leather, ac + up * .25f, new Vector3(.95f, .5f, .9f), f.r); mk.Box(leather, ac + up * .8f + f.Fwd * .38f, new Vector3(.95f, .8f, .16f), f.r); foreach (var e in new[] { -1f, 1f }) mk.Box(leather, ac + up * .5f + f.Right * e * .45f, new Vector3(.14f, .5f, .85f), f.r);
            Interact.Seat(plife.transform, ac + up * .52f, -f.Fwd, "Sit in the sweet spot"); Col(ac + up * .5f + f.Fwd * .38f, new Vector3(.95f, 1.2f, .2f), f.r);
            var so = fo.P(2.2f, 0, 2f); mk.Box(leather, so + up * .25f, new Vector3(.9f, .5f, 2.2f), f.r); mk.Box(leather, so + up * .75f + f.Right * .38f, new Vector3(.16f, .7f, 2.2f), f.r); Interact.Bench(plife.transform, so + up * .52f - f.Fwd * .7f, so + up * .52f + f.Fwd * .7f, -f.Right); Col(so + up * .5f + f.Right * .38f, new Vector3(.2f, 1.2f, 2.2f), f.r);
            Folk(so + up * .52f + f.Fwd * .55f, -f.Right, Resident.Pose.Sit, new[] { "Shh. Not during side two.", "You can hear the room they recorded it in. And the tea.", "Sweet spot's free. I gave it up in '78. Too much responsibility." }, Look(Residents.Tool.Mug));
            StoneSign(ctx, mk, f.P(Door / 2f + 2f, 0, D / 2f + 1.6f), f.Fwd, "THE LISTENING ROOM\nplease stay for the whole side", 3.6f, .14f);
            Lane(ctx, mk, f.P(0, 0, D / 2f + 2f)); Ring(g, 8f);
            Shot(ctx, "the listening room", f.P(5f, 1.8f, D / 2f + 11f), fo.P(0, 1.6f, 0));
            Shot(ctx, "inside the listening room", fo.P(2.6f, 1.6f, 2.8f), fo.P(-1f, 1.2f, -3f));
        }

        // the Floating Isles: stepping stones up to a bench on a rock hanging over the sea
        void FloatingIsles(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var g = Site(ctx, IslesAt, 8f);
            var toSea = Vector3.zero; for (var a = 0; a < 360; a += 15) { var d = Quaternion.Euler(0, a, 0) * Vector3.forward; if (ctx.Ground(g.x + d.x * 70f, g.z + d.z * 70f) < .3f) toSea += d; }
            if (toSea.sqrMagnitude < .01f) toSea = Vector3.forward; var f = new PlaceFrame(g, toSea.normalized);
            var rock = WorldMaterials.Solid(new Color(.3f, .28f, .36f)); var moss = WorldMaterials.Solid(new Color(.42f, .55f, .4f)); var fall = GlowM(new Color(.6f, .8f, 1f), 1.6f);
            // fixed: five stepping slabs rising to the bench isle
            for (var k = 0; k < 6; k++) { var p = f.P(Mathf.Sin(k * .9f) * 1.2f, .5f + k * .62f, 1.5f + k * 1.5f); mk.Blob(rock, p - up * .3f, new Vector3(.9f, .35f, .9f), ctx.Rng, .25f); mk.Prism(moss, p - up * .02f, up, .85f, .06f, 9); solid.Prism(rock, p - up * .5f, up, .95f, .54f, 9); mk.Box(fall, p - up * 1.2f, new Vector3(.05f, 1.2f, .05f), Quaternion.identity); }
            var isle = f.P(0, 4.3f, 12.5f); mk.Blob(rock, isle - up * 1.6f, new Vector3(3.6f, 1.8f, 3.6f), ctx.Rng, .3f); mk.Prism(moss, isle - up * .05f, up, 3.4f, .1f, 12); solid.Prism(rock, isle - up * 1.5f, up, 3.4f, 1.5f, 12);
            for (var k = 0; k < 5; k++) mk.Box(fall, isle + Quaternion.Euler(0, k * 72f, 0) * Vector3.forward * 2.6f - up * (2.6f + k * .3f), new Vector3(.08f, 3f + k * .6f, .08f), Quaternion.identity);
            SeatBench(ctx, mk, isle + f.Fwd * 1.6f, f.Fwd, 2.2f);
            var tr = isle - f.Fwd * 1.4f + f.Right * 1.6f; mk.Prism(dark, tr, up, .12f, 2.2f, 6); soft.Blob(GlowM(new Color(.6f, 1f, .7f), 1f), tr + up * 2.6f, Vector3.one * 1f, ctx.Rng, .3f); ArtistBits.PostCollider(ctx.Root, tr, 2.2f, .15f);
            ArtistBits.PointLight(ctx.Root, isle + up * 2.4f, new Color(.6f, .85f, 1f), 14f, 1.5f);
            for (var k = 0; k < 9; k++) { var a = k * 40f + 15f; if (Mathf.Abs(Mathf.DeltaAngle(a, 180f)) < 30f) continue; var d = f.r * (Quaternion.Euler(0, a, 0) * Vector3.forward); Col(isle + d * 3.3f + up * .5f, new Vector3(2.4f, 1f, .15f), Quaternion.LookRotation(d)); mk.Blob(rock, isle + d * 3.3f + up * .15f, new Vector3(.4f, .3f, .4f), ctx.Rng, .3f); }
            // drifting: four more isles that rise and fall slowly out over the water
            for (var k = 0; k < 4; k++)
            {
                var im = new MeshKit(); var s = 1.6f + (k % 3) * .7f; im.Blob(rock, new Vector3(0, -s * .5f, 0), new Vector3(s, s * .6f, s), prng, .3f); im.Prism(moss, Vector3.zero, Vector3.up, s * .95f, .08f, 10); im.Box(fall, new Vector3(s * .4f, -s * 1.6f, 0), new Vector3(.06f, s * 2f, .06f), Quaternion.identity);
                im.Prism(dark, new Vector3(-s * .3f, 0, 0), Vector3.up, .08f, 1.2f, 5); im.Blob(GlowM(Spectrum[(k * 2 + 3) % Spectrum.Length], 1.2f), new Vector3(-s * .3f, 1.5f, 0), Vector3.one * .6f, prng, .3f);
                var go = im.BuildSingle("Floating isle", plife.transform, false); var home = f.P(-9f + k * 6.5f, 8f + (k % 2) * 4f, 20f + (k % 3) * 5f); go.transform.position = home; plife.isles.Add((go.transform, home, k * 1.9f));
            }
            Folk(f.P(3f, 0, -1f), f.Fwd, Resident.Pose.Stand, new[] { "They float because nobody told them not to.", "It's the cover of something. I forget what. Everything, possibly." });
            StoneSign(ctx, mk, f.P(-3f, 0, 0), -f.Fwd, "THE FLOATING ISLES");
            Lane(ctx, mk, f.P(0, 0, -3f)); Ring(g, 7f);
            Shot(ctx, "the floating isles", f.P(-9f, 3f, -8f), isle + up * 2f);
        }

        // the Great Keyboard: keys the size of doors; each lights a pipe of colour
        void GreatKeyboard(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var g = ctx.OnGround(KeysAt.x, KeysAt.y); var f = new PlaceFrame(g + up * .1f, FaceTo(g, Nearest(roadPts, g))); const int N = 12; const float KW = 1.15f, KL = 4.6f;
            mk.Box(dark, f.P(0, .05f, -.4f), new Vector3(N * KW + .8f, .3f, KL + 2f), f.r); solid.Box(dark, f.P(0, -.05f, -.4f), new Vector3(N * KW + .8f, .5f, KL + 2f), f.r);
            for (var k = 0; k < N; k++)
            {
                var x = (k - (N - 1) / 2f) * KW; var col = Spectrum[k % Spectrum.Length]; var glow = new Material(WorldMaterials.Glow(col, .2f)) { name = "Organ pipe" };
                var h = 4f + (k * 5 % 7) * .9f; var pp = f.P(x, .2f, -KL / 2f - 1f); mk.Prism(chrome, pp, up, .32f, h, 10); mk.Prism(glow, pp + up * (h - 1.2f), up, .34f, 1.2f, 10); ArtistBits.PostCollider(ctx.Root, pp, h, .34f);
                var key = new MeshKit(); key.Box(WorldMaterials.Solid(new Color(.95f, .94f, .9f)), new Vector3(0, .12f, 0), new Vector3(KW - .08f, .24f, KL), Quaternion.identity); key.Box(WorldMaterials.Solid(col), new Vector3(0, .25f, KL / 2f - .3f), new Vector3(KW - .2f, .02f, .4f), Quaternion.identity);
                var go = key.BuildSingle("Great key", plife.transform, false); go.transform.SetPositionAndRotation(f.P(x, .2f, .6f), f.r);
                var sc = go.AddComponent<BoxCollider>(); sc.center = new Vector3(0, .12f, 0); sc.size = new Vector3(KW - .08f, .24f, KL);
                var trig = new GameObject("Key touch"); trig.transform.SetParent(go.transform, false); var idx = plife.pipes.Count; plife.pipes.Add((glow, col, -10f));
                var t = Interact.Toy(trig, Toy.Kind.Custom, "Play the key", new Vector3(KW - .1f, 1.2f, KL)); trig.GetComponent<BoxCollider>().center = new Vector3(0, .7f, 0); var keyT = go.transform; t.onUse = _ => plife.Note(idx, keyT);
                if (k % 7 != 2 && k % 7 != 6 && k < N - 1) mk.Box(dark, f.P(x + KW / 2f, .56f, -.6f), new Vector3(.6f, .3f, 2.6f), f.r);
            }
            plife.keyLight = ArtistBits.PointLight(ctx.Root, f.P(0, 4f, -1f), Color.white, 20f, 1f);
            SeatBench(ctx, mk, ctx.OnGround(f.P(0, 0, 6f).x, f.P(0, 0, 6f).z), -f.Fwd, 3f);
            Folk(f.P(N * KW / 2f + 2f, 0, 1f), -f.Right, Resident.Pose.Wave, new[] { "Play it with your feet. Everyone does, eventually.", "I've been working on a solo. It needs nine of me.", "Low keys on the left. Unless you're standing over there." },
                new Residents.Look { top = new Color(.35f, .2f, .5f), bottom = new Color(.16f, .16f, .2f), skin = new Color(.9f, .72f, .6f), hair = new Color(.75f, .7f, .55f), longHair = true });
            StoneSign(ctx, mk, f.P(-N * KW / 2f - 2.2f, 0, 4f), f.Fwd, "THE GREAT KEYBOARD");
            Lane(ctx, mk, f.P(0, 0, 8f)); Ring(g, 11f);
            Shot(ctx, "the great keyboard", f.P(6f, 3f, 12f), f.P(0, 2f, -2f));
        }

        // the Tape Loop: two reel-to-reels, and a loop of tape running round the garden between them
        void TapeLoop(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var g = ctx.OnGround(TapeAt.x, TapeAt.y); var f = new PlaceFrame(g, FaceTo(g, Nearest(roadPts, g)));
            var path = new List<Vector3>();
            foreach (var (x, z) in new[] { (-5f, -3f), (-6f, 3f), (0f, 5.5f), (6f, 3f), (5f, -3f) }) { var p = ctx.OnGround(f.P(x, 0, z).x, f.P(x, 0, z).z); path.Add(p + up * 1.5f); mk.Prism(chrome, p - up * .2f, up, .06f, 1.8f, 6); mk.Prism(chrome, p + up * 1.42f, up, .16f, .16f, 10); ArtistBits.PostCollider(ctx.Root, p, 1.7f, .1f); }
            var tape = WorldMaterials.Solid(new Color(.25f, .16f, .1f)); for (var k = 0; k < path.Count; k++) { var a = path[k]; var b = path[(k + 1) % path.Count]; mk.Box(tape, (a + b) / 2f, new Vector3(.02f, .12f, Vector3.Distance(a, b)), Quaternion.LookRotation(b - a)); }
            plife.tapePath = path.ToArray();
            for (var k = 0; k < 6; k++) { var bm = new MeshKit(); bm.Blob(GlowM(Spectrum[k], 2.6f), Vector3.zero, Vector3.one * .1f, prng, 0f); var go = bm.BuildSingle("Tape bead", plife.transform, false); go.transform.position = path[0]; plife.beads.Add(go.transform); }
            foreach (var e in new[] { -1, 1 })
            {
                var d = ctx.OnGround(f.P(e * 5f, 0, -3.6f).x, f.P(e * 5f, 0, -3.6f).z); mk.Box(WorldMaterials.Solid(new Color(.6f, .62f, .6f)), d + up * .7f, new Vector3(1.6f, 1.4f, .7f), f.r); Col(d + up * .7f, new Vector3(1.6f, 1.4f, .7f), f.r);
                mk.Box(GlowM(new Color(1f, .7f, .3f), 1.2f), d + up * .35f + f.Fwd * .36f, new Vector3(1f, .25f, .02f), f.r);
                foreach (var s in new[] { -.4f, .4f }) { var rm = new MeshKit(); rm.Prism(chrome, Vector3.zero, Vector3.forward, .34f, .04f, 14); for (var q = 0; q < 3; q++) rm.Box(dark, Quaternion.Euler(0, 0, q * 120f) * new Vector3(0, .17f, .03f), new Vector3(.1f, .2f, .02f), Quaternion.Euler(0, 0, q * 120f)); var rgo = rm.BuildSingle("Tape reel", plife.transform, false); rgo.transform.SetPositionAndRotation(d + up * 1f + f.Right * s + f.Fwd * .37f, f.r); plife.reels.Add(rgo.transform); }
                if (e < 0) { var bgo = new GameObject("Play button"); bgo.transform.SetParent(plife.transform, false); bgo.transform.position = d + up * .5f + f.Fwd * .5f; Interact.Toy(bgo, Toy.Kind.Custom, "Start the tape", new Vector3(1.6f, 1.4f, .6f)).onUse = _ => plife.TapeToggle(); }
            }
            plife.tapeLight = ArtistBits.PointLight(ctx.Root, g + up * 3f, new Color(1f, .75f, .4f), 14f, 1f);
            SeatBench(ctx, mk, ctx.OnGround(g.x, g.z), f.Fwd, 2.4f);
            Folk(f.P(-3f, 0, -4.4f), f.Fwd, Resident.Pose.Work, new[] { "It's the same eleven seconds. Listen: it's never the same.", "I'm on take four thousand. It's coming together." }, Look(Residents.Tool.Mug)).workRate = .4f;
            StoneSign(ctx, mk, f.P(4f, 0, 8f), f.Fwd, "THE TAPE LOOP");
            Lane(ctx, mk, f.P(0, 0, 8f)); Ring(g, 9f);
            Shot(ctx, "the tape loop", f.P(-4f, 2.2f, 12f), g + up * 1.2f);
        }

        // the Ferry Jetty: a bench and a telescope trained on the Hub
        void FerryJetty(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var g = Site(ctx, JettyAt, 4f);
            var toSea = Vector3.zero; for (var a = 0; a < 360; a += 15) { var d = Quaternion.Euler(0, a, 0) * Vector3.forward; if (ctx.Ground(g.x + d.x * 55f, g.z + d.z * 55f) < .3f) toSea += d; }
            if (toSea.sqrMagnitude < .01f) toSea = Vector3.right; var f = new PlaceFrame(g, toSea.normalized); var deck = Mathf.Max(g.y, 1.6f) + .5f; const float L = 26f;
            for (var z = 0f; z < L; z += 1f) { var p = f.P(0, 0, z); mk.Box(wood, new Vector3(p.x, deck, p.z), new Vector3(2.6f, .12f, .9f), f.r); if (((int)z) % 5 == 0) foreach (var e in new[] { -1f, 1f }) { var post = p + f.Right * e * 1.2f; mk.Box(dark, new Vector3(post.x, deck / 2f - .6f, post.z), new Vector3(.2f, deck + 1.6f, .2f), f.r); } }
            solid.Box(wood, new Vector3(f.P(0, 0, L / 2f).x, deck - .1f, f.P(0, 0, L / 2f).z), new Vector3(2.6f, .3f, L), f.r);
            foreach (var e in new[] { -1f, 1f }) Col(new Vector3(f.P(e * 1.25f, 0, L / 2f).x, deck + .5f, f.P(e * 1.25f, 0, L / 2f).z), new Vector3(.12f, 1f, L), f.r);
            var end = f.P(0, 0, L - 1.4f); end.y = deck;
            SeatBench(ctx, mk, end - f.Fwd * 1.5f - f.Right * .2f, f.Fwd, 1.8f);
            var tp = end + f.Right * .9f + f.Fwd * .4f; mk.Prism(chrome, tp, up, .05f, 1.3f, 6); ArtistBits.PostCollider(ctx.Root, tp, 1.3f, .1f);
            var tel = new MeshKit(); tel.Prism(brass, new Vector3(0, 0, -.5f), Vector3.forward, .08f, 1f, 8, .8f); var tgo = tel.BuildSingle("Telescope", plife.transform, false); tgo.transform.SetPositionAndRotation(tp + up * 1.4f, Quaternion.LookRotation(f.Fwd)); var tt = Interact.Toy(tgo, Toy.Kind.Spin, "Swing the telescope", new Vector3(.5f, .5f, 1.2f)); tt.amount = 360f; tt.seconds = 4f;
            Lamp(ctx, mk, new Vector3(f.P(-1f, 0, 2f).x, deck, f.P(-1f, 0, 2f).z));
            var boat = f.P(2.6f, 0, L - 6f); boat.y = .1f; mk.Box(WorldMaterials.Solid(new Color(.3f, .2f, .35f)), boat + up * .2f, new Vector3(1.4f, .6f, 4f), f.r); mk.Box(wood, boat + up * .45f, new Vector3(1.2f, .06f, .4f), f.r); mk.Blob(GlowM(new Color(1f, .7f, .3f), 2.2f), boat + up * 1.2f + f.Fwd * 1.6f, Vector3.one * .12f, ctx.Rng, 0f); mk.Box(dark, boat + up * .75f + f.Fwd * 1.6f, new Vector3(.04f, .9f, .04f), Quaternion.identity);
            Folk(new Vector3(f.P(.6f, 0, 6f).x, deck + .06f, f.P(.6f, 0, 6f).z), -f.Fwd, Resident.Pose.Lean, new[] { "The Hub's just across. I'll take you when the eclipse is over. So, no.", "Boat's fine. Sea's fine. I'm just not in a hurry.", "Have a look through the glass. That's everything, over there." }, Look());
            ArtistBits.Label(ctx.Root, "FERRY\n(pending)", new Vector3(f.P(-1.3f, 0, 1f).x, deck + 1.6f, f.P(-1.3f, 0, 1f).z), -f.Fwd, .16f, new Color(1f, .8f, .5f), TextAnchor.MiddleCenter);
            Lane(ctx, mk, f.P(0, 0, -3f)); Ring(g, 6f);
            Shot(ctx, "the ferry jetty", f.P(-7f, deck - g.y + 2f, 2f), new Vector3(end.x, deck + .5f, end.z));
        }

        // the Tea Garden: a lawn inside a hedge, tea for four, croquet
        void TeaGarden(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var g = ctx.OnGround(TeaAt.x, TeaAt.y); var f = new PlaceFrame(g, FaceTo(g, Nearest(roadPts, g))); const float S = 20f; var hedge = WorldMaterials.Solid(new Color(.14f, .26f, .16f));
            mk.Box(WorldMaterials.Solid(new Color(.42f, .6f, .36f)), g - up * .12f, new Vector3(S, .36f, S), f.r);
            foreach (var (x, z, sx, sz) in new[] { (0f, -S / 2f, S, 1f), (-S / 2f, 0f, 1f, S), (S / 2f, 0f, 1f, S), (-S / 4f - 1.2f, S / 2f, S / 2f - 2.4f, 1f), (S / 4f + 1.2f, S / 2f, S / 2f - 2.4f, 1f) }) { var c = f.P(x, .75f, z); mk.Box(hedge, c, new Vector3(sx, 1.5f, sz), f.r); solid.Box(hedge, c, new Vector3(sx, 1.5f, sz), f.r); }
            // the tea table
            var t = f.P(-4f, .06f, -3f); mk.Prism(WorldMaterials.Solid(new Color(.95f, .94f, .9f)), t + up * .7f, up, .8f, .05f, 14); mk.Prism(chrome, t, up, .06f, .7f, 6); ArtistBits.PostCollider(ctx.Root, t, .75f, .8f);
            mk.Blob(WorldMaterials.Solid(new Color(.9f, .85f, .95f)), t + up * .86f, new Vector3(.14f, .12f, .14f), ctx.Rng, 0f); for (var k = 0; k < 4; k++) mk.Prism(WorldMaterials.Solid(new Color(.95f, .9f, .8f)), t + up * .76f + Quaternion.Euler(0, k * 90f + 45f, 0) * Vector3.forward * .5f, up, .05f, .07f, 8);
            for (var k = 0; k < 4; k++)
            {
                var d = f.r * (Quaternion.Euler(0, k * 90f, 0) * Vector3.forward); var c = t + d * 1.35f; var rot = Quaternion.LookRotation(-d); var cm = WorldMaterials.Solid(new Color(.95f, .94f, .9f));
                mk.Box(cm, c + up * .45f, new Vector3(.5f, .05f, .5f), rot); mk.Box(cm, c + up * .8f + d * .23f, new Vector3(.5f, .6f, .04f), rot); Col(c + up * .45f + d * .1f, new Vector3(.5f, .9f, .5f), rot);
                if (k == 1) Folk(c + up * .5f, -d, Resident.Pose.Sit, new[] { "One lump or eleven-eight?", "We play croquet between movements. The movements are long. We play a lot of croquet." }, Look(Residents.Tool.Mug)); else Interact.Seat(plife.transform, c + up * .5f, -d, "Sit down to tea");
            }
            // croquet: hoops, balls to knock about, a peg
            foreach (var (x, z) in new[] { (2f, -5f), (6f, -1f), (4f, 4f), (-2f, 5f), (1f, 0f) }) { var h = f.P(x, .06f, z); foreach (var e in new[] { -.2f, .2f }) mk.Box(chrome, h + f.Right * e + up * .2f, new Vector3(.03f, .4f, .03f), f.r); mk.Box(chrome, h + up * .4f, new Vector3(.43f, .03f, .03f), f.r); }
            mk.Prism(WorldMaterials.Solid(Spectrum[0]), f.P(1f, .06f, 2f), up, .04f, .6f, 6);
            for (var k = 0; k < 4; k++)
            {
                var bm = new MeshKit(); bm.Blob(WorldMaterials.Solid(Spectrum[new[] { 0, 2, 4, 5 }[k]], .5f), Vector3.zero, Vector3.one * .14f, prng, 0f);
                var go = bm.BuildSingle("Croquet ball", plife.transform, false); go.transform.position = f.P(1.5f + k * 1.2f, .3f, -2f + (k % 2) * 1.5f);
                go.AddComponent<SphereCollider>().radius = .14f; var rb = go.AddComponent<Rigidbody>(); rb.mass = .5f; rb.linearDamping = .7f; rb.angularDamping = .7f; var c = go.AddComponent<Carryable>(); c.thing = "the croquet ball"; c.home = go.transform.position; c.lowest = g.y - 6f;
            }
            Folk(f.P(5f, .06f, 2f), -f.Right, Resident.Pose.Work, new[] { "Mind the hoops. They're in a time signature.", "I'm winning. Nobody else knows we're playing." }, Look(Residents.Tool.Rake)).workRate = .5f;
            Lamp(ctx, mk, f.P(-8f, 0, 8f)); Lamp(ctx, mk, f.P(8f, 0, -8f));
            StoneSign(ctx, mk, f.P(4.2f, 0, S / 2f + 2f), f.Fwd, "THE TEA GARDEN");
            Lane(ctx, mk, f.P(0, 0, S / 2f + 1f)); Ring(g, 14f);
            Shot(ctx, "the tea garden", f.P(3f, 3.2f, S / 2f + 9f), g + up * .6f);
        }

        // the Stair in Seven: four flights of seven steps round a tower to a bench in the sky
        void StairInSeven(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var g = Site(ctx, StairAt, 8f); var f = new PlaceFrame(g, FaceTo(g, center)); const float Core = 3.2f, SW = 1.5f, Rise = .3f; const int Per = 7;
            var lo = g.y; for (var k = 0; k < 8; k++) lo = Mathf.Min(lo, ctx.Ground(g.x + Mathf.Cos(k * .785f) * 5f, g.z + Mathf.Sin(k * .785f) * 5f));
            var top = g.y + 4 * Per * Rise + .3f; mk.Box(darkStone, new Vector3(g.x, (lo + top) / 2f - .2f, g.z), new Vector3(Core, top - lo + .4f, Core), f.r); solid.Box(darkStone, new Vector3(g.x, (lo + top) / 2f - .2f, g.z), new Vector3(Core, top - lo + .4f, Core), f.r);
            var n = 0;
            for (var side = 0; side < 4; side++)
            {
                var d = f.r * (Quaternion.Euler(0, side * 90f, 0) * Vector3.forward); var along = f.r * (Quaternion.Euler(0, side * 90f + 90f, 0) * Vector3.forward);
                for (var k = 0; k < Per; k++, n++)
                {
                    var y = g.y + (n + 1) * Rise; var c = g + d * (Core / 2f + SW / 2f) + along * (-Core / 2f - SW / 2f + (k + .5f) * ((Core + SW) / Per)); var w = (Core + SW) / Per + .02f;
                    var bottom = Mathf.Min(lo, ctx.Ground(c.x, c.z)) - .3f; mk.Box(n % Per == 0 ? WorldMaterials.Solid(Spectrum[side]) : stone, new Vector3(c.x, (bottom + y) / 2f, c.z), new Vector3(SW, y - bottom, w), Quaternion.LookRotation(along)); solid.Box(stone, new Vector3(c.x, (bottom + y) / 2f, c.z), new Vector3(SW, y - bottom, w), Quaternion.LookRotation(along));
                }
                // the corner landing at the end of this flight
                var cy = g.y + n * Rise; var corner = g + d * (Core / 2f + SW / 2f) + along * (Core / 2f + SW / 2f); var cb = Mathf.Min(lo, ctx.Ground(corner.x, corner.z)) - .3f;
                mk.Box(stone, new Vector3(corner.x, (cb + cy) / 2f, corner.z), new Vector3(SW, cy - cb, SW), f.r); solid.Box(stone, new Vector3(corner.x, (cb + cy) / 2f, corner.z), new Vector3(SW, cy - cb, SW), f.r);
            }
            var deck = new Vector3(g.x, top, g.z); mk.Box(stone, deck - up * .1f, new Vector3(Core + .3f, .2f, Core + .3f), f.r);
            SeatBench(ctx, mk, deck, f.Fwd, 1.8f); mk.Blob(GlowM(new Color(1f, .7f, .3f), 2.4f), deck + up * 1.6f - f.Fwd * 1.2f, Vector3.one * .16f, ctx.Rng, 0f); mk.Box(dark, deck + up * .8f - f.Fwd * 1.2f, new Vector3(.06f, 1.6f, .06f), Quaternion.identity); halos.Add(deck + up * 1.6f - f.Fwd * 1.2f, 1.4f, new Color(1f, .7f, .3f), .7f);
            ArtistBits.PointLight(ctx.Root, deck + up * 2.2f, new Color(1f, .75f, .4f), 14f, 1.4f);
            StoneSign(ctx, mk, f.P(5.5f, 0, 5f), f.Fwd, "THE STAIR IN SEVEN\n(count them)");
            Folk(f.P(-5f, 0, 4f), f.Fwd, Resident.Pose.Stand, new[] { "Seven, seven, seven, seven. People trip on the fifth. It's a good fifth.", "From the top you can see the whole figure of eight. It's an eight. I checked." });
            Lane(ctx, mk, f.P(0, 0, 7f)); Ring(g, 7f);
            Shot(ctx, "the stair in seven", f.P(9f, 3f, 12f), g + up * 4.5f);
        }
    }

    /// The Marches places' moving parts: the floating isles, the keyboard's pipes, the listening room's speakers, the
    /// tape loop.
    public class ProgPlacesLife : MonoBehaviour
    {
        public List<(Transform t, Vector3 home, float seed)> isles = new List<(Transform, Vector3, float)>();
        public List<(Material m, Color c, float struck)> pipes = new List<(Material, Color, float)>(); public Light keyLight, roomLight, tapeLight;
        public Material cones; public Transform platter; public Vector3[] tapePath = new Vector3[0]; public List<Transform> beads = new List<Transform>(), reels = new List<Transform>();
        float playing = -100f, tapeSpeed, tapeAt; bool tapeOn; Transform dipped; float dipT = -1f; Vector3 dipRest;
        static readonly Color Amber = new Color(1f, .7f, .3f);

        public void Note(int i, Transform key) { var p = pipes[i]; p.struck = Time.time; pipes[i] = p; if (keyLight != null) keyLight.color = p.c; if (dipT >= 0f && dipped != null) dipped.localPosition = dipRest; dipped = key; dipRest = key.localPosition; dipT = 0f; }
        public void Needle() { playing = Time.time; }
        public void TapeToggle() { tapeOn = !tapeOn; }

        void Update()
        {
            var t = Time.time; var dt = Time.deltaTime;
            foreach (var (tr, home, seed) in isles) if (tr != null) { tr.position = home + Vector3.up * Mathf.Sin(t * .25f + seed) * 1.6f + new Vector3(Mathf.Sin(t * .11f + seed * 2f), 0, Mathf.Cos(t * .13f + seed)) * 1.2f; tr.rotation = Quaternion.Euler(0, t * 3f + seed * 40f, 0); }
            var loud = 0f; for (var i = 0; i < pipes.Count; i++) { var (m, c, struck) = pipes[i]; var age = t - struck; var ring = age < 5f ? Mathf.Exp(-age * .8f) : 0f; loud = Mathf.Max(loud, ring); m.SetColor("_EmissionColor", c * (.2f + ring * 3.4f)); }
            if (keyLight != null) keyLight.intensity = 1f + loud * 3.5f;
            if (dipT >= 0f && dipped != null) { dipT += dt; dipped.localPosition = dipRest - Vector3.up * .1f * Mathf.Sin(Mathf.Clamp01(dipT / .35f) * Mathf.PI); if (dipT > .35f) { dipped.localPosition = dipRest; dipT = -1f; } }
            var on = t - playing < 40f; var pulse = on ? 1.6f + Mathf.Sin(t * 6f) * .5f + Mathf.Sin(t * 2.3f) * .4f : .5f;
            if (cones != null) cones.SetColor("_EmissionColor", Amber * pulse); if (roomLight != null) roomLight.intensity = on ? 1.2f + pulse * .5f : 1.2f; if (platter != null && on) platter.Rotate(0, 200f * dt, 0, Space.World);
            tapeSpeed = Mathf.MoveTowards(tapeSpeed, tapeOn ? 1f : 0f, dt); tapeAt += tapeSpeed * dt * 2.4f;
            foreach (var r in reels) if (r != null) r.Rotate(0, 0, tapeSpeed * 180f * dt, Space.Self);
            if (tapePath.Length > 1)
            {
                float total = 0f; for (var k = 0; k < tapePath.Length; k++) total += Vector3.Distance(tapePath[k], tapePath[(k + 1) % tapePath.Length]);
                for (var b = 0; b < beads.Count; b++)
                {
                    var d = Mathf.Repeat(tapeAt + b * total / beads.Count, total);
                    for (var k = 0; k < tapePath.Length; k++) { var a = tapePath[k]; var c = tapePath[(k + 1) % tapePath.Length]; var l = Vector3.Distance(a, c); if (d <= l) { beads[b].position = Vector3.Lerp(a, c, d / l); break; } d -= l; }
                }
            }
            if (tapeLight != null) tapeLight.intensity = 1f + tapeSpeed * 1.5f;
        }
    }
}
