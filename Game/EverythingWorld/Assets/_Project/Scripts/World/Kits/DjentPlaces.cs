using System.Collections.Generic;
using UnityEngine;
using static EverythingWorld.World.PlaceTools;

namespace EverythingWorld.World
{
    /// The Djent Spires laid out on purpose (the build-land standard), and given the contrast it lacked: the terrace
    /// treads are now deep blue slate and the risers pale ice (DjentKit.Ground), so the white stone, the crystal and
    /// the gold stand out against dark ground instead of pale on pale. Every terrace, path and artist stays where it
    /// was. One more fretboard path, the East Scale, runs down the empty east side from the north to the south. Along
    /// it, with the people of the Spires: the Polyrhythm Clock (gold rings turning three against four against five
    /// against seven; a lever to change the tempo); the Tuning Forks (eight of crystal, to strike); the Hex Garden
    /// (raked dark gravel, gold lines, a tea pavilion); and the Observatory (a white dome to walk into, a gold
    /// telescope, an orrery to turn).
    public partial class DjentKit
    {
        static readonly Vector2 ClockAt = new Vector2(6556, 5796), ForksAt = new Vector2(6606, 5702), GardenAt = new Vector2(6646, 5372), ObsAt = new Vector2(6588, 5282);
        static readonly Vector2[] EastScale = { new Vector2(6440, 5722), new Vector2(6490, 5758), new Vector2(6534, 5776), new Vector2(6578, 5748), new Vector2(6596, 5716), new Vector2(6612, 5640), new Vector2(6624, 5560), new Vector2(6628, 5470), new Vector2(6630, 5402), new Vector2(6614, 5332), new Vector2(6598, 5304) };
        static readonly (Vector2 at, float r)[] PlaceRings = { (ClockAt, 12f), (ForksAt, 9f), (GardenAt, 12f), (ObsAt, 11f) };

        DjentPlacesLife plife; System.Random prng; Transform rootT; LandContext ctx0; readonly List<Vector3> scaleP = new List<Vector3>();

        static readonly Color[] Robes = { new Color(.92f, .94f, .98f), new Color(.2f, .26f, .42f), new Color(.75f, .85f, .95f), new Color(.95f, .8f, .4f), new Color(.14f, .16f, .24f) };
        static readonly string[] SpireTalk =
        {
            "Count it in seven. No, the other seven.", "Everything here is in tune. It took a very long time and nobody is allowed to lean on anything.", "The citadel's a copy of itself, all the way down. I find that restful. Some people don't.",
            "It's not complicated. It's just exact. People confuse the two.", "You'll hear the coils crack when it gets loud. That's applause, more or less.",
        };

        Residents.Look Savant(Residents.Tool tool = Residents.Tool.None, bool apron = false) { var l = Residents.Random(prng, Robes, Robes, new[] { Residents.Hat.None, Residents.Hat.None, Residents.Hat.Beanie }, Robes); l.tool = tool; l.apron = apron; l.longHair = prng.Next(2) == 0; return l; }
        Resident Folk(Vector3 feet, Vector3 face, Resident.Pose pose, string[] lines = null, Residents.Look look = null, Vector3[] route = null, float seatH = HumanScale.SeatHeight)
            => Residents.Make(plife.transform, feet, face, look ?? Savant(), pose, prng, lines ?? SpireTalk, route, seatH);
        void Col(Vector3 c, Vector3 size, Quaternion rot) => BoxCol(rootT, c, size, rot);
        Vector3 G(Vector3 p) => ctx0.OnGround(p.x, p.z);
        void Ring(Vector3 at, float r) => noGrass.Add(new Vector3(at.x, r, at.z));

        public override void ShapeGround(IslandWorldData data, int index)
        {
            ShapeSpires(data, index);   // everything the land already did, unchanged
            foreach (var (at, r) in PlaceRings) Level(data, index, at, r, r + 12f);
        }

        /// A slate slab framed in gold like the land's own. Signs on the +x side of a place's path; the photo spot on the −x side.
        void FretSign(MeshKit mk, Vector3 at, Vector3 face, string text, float w = 3f, float size = .2f)
        {
            var up = Vector3.up; var g = G(at); var rot = Quaternion.LookRotation(face);
            mk.Box(slate, g + up * 1f, new Vector3(w, 1.6f, .25f), rot); Col(g + up * 1f, new Vector3(w, 2f, .3f), rot); foreach (var y in new[] { .22f, 1.78f }) mk.Box(fretM, g + up * y + face * .13f, new Vector3(w, .06f, .03f), rot); mk.Box(stoneLight, g + up * .1f, new Vector3(w + .3f, .2f, .5f), rot);
            ArtistBits.Label(rootT, text, g + up * 1f + face * .14f, face, size, Gold, TextAnchor.MiddleCenter);
        }

        /// A white stone bench with a gold edge.
        void HexBench(MeshKit mk, Vector3 at, Vector3 face, float len = 2.2f)
        {
            var up = Vector3.up; var g = G(at); var rot = Quaternion.LookRotation(face); var side = Vector3.Cross(up, face);
            mk.Box(stoneLight, g + up * .25f, new Vector3(len, .5f, .6f), rot); mk.Box(fretM, g + up * .5f + face * .28f, new Vector3(len, .03f, .04f), rot); Col(g + up * .25f, new Vector3(len, .5f, .6f), rot);
            Interact.Bench(plife.transform, g + up * .56f - side * (len / 2f - .45f), g + up * .56f + side * (len / 2f - .45f), face);
        }

        List<Vector3> FretPath(LandContext ctx, MeshKit mk, Vector2[] way, float w, bool lanterns)
        {
            var pts = PlaceTools.Smooth(ctx, way, false, 1.5f);
            for (var i = 0; i < pts.Count; i++) { foreach (var a in ctx.Artists) { var r = Foot(a) + 7f; var v = new Vector2(pts[i].x - a.x, pts[i].z - a.z); var d = v.magnitude; if (d < r) { v = d > .01f ? v / d * r : new Vector2(r, 0); pts[i] = ctx.OnGround(a.x + v.x, a.z + v.y); } } if (lanterns) foreach (var c in noGrass) { if (c.y < 8f) continue; var r = c.y + 3f; var v = new Vector2(pts[i].x - c.x, pts[i].z - c.z); var d = v.magnitude; if (d < r) { v = d > .01f ? v / d * r : new Vector2(r, 0); pts[i] = ctx.OnGround(c.x + v.x, c.z + v.y); } } }
            if (pts.Count < 2) return pts;
            Strip(mk, pts, slate, w, .2f, ctx); var up = Vector3.up;
            for (var i = 1; i < pts.Count - 1; i++) { var d = pts[i + 1] - pts[i - 1]; d.y = 0; if (d.sqrMagnitude < .01f) continue; d.Normalize(); var sd = Vector3.Cross(up, d); var fl = pts[i] - sd * (w / 2f - .1f); var fr = pts[i] + sd * (w / 2f - .1f); fl.y = ctx.Ground(fl.x, fl.z) + .22f; fr.y = ctx.Ground(fr.x, fr.z) + .22f; mk.Box(fretM, (fl + fr) / 2f, new Vector3(.12f, .05f, w - .2f), Quaternion.LookRotation(fr - fl)); if (i % 12 == 6) foreach (var s in new[] { -.6f, .6f }) mk.Prism(pearlM, pts[i] + up * .22f - d * .75f + sd * s, up, .16f, .04f, 10); }
            foreach (var p in pts) Remember(p, lanterns ? 700 : 900);
            if (lanterns) { var n = 0; for (var i = 10; i < pts.Count; i += 20) { var d = pts[i] - pts[i - 1]; d.y = 0; if (d.sqrMagnitude < .01f) continue; d.Normalize(); var lp = pts[i] + Vector3.Cross(up, d) * (n++ % 2 == 0 ? 1f : -1f) * (w / 2f + 1f); if (!InNoGrass(lp.x, lp.z, 1f)) Lantern(ctx, mk, ctx.OnGround(lp.x, lp.z)); } scaleP.AddRange(pts); }
            return pts;
        }

        void Fingerboard(LandContext ctx, MeshKit mk, Vector3 to)
        {
            var from = Nearest(scaleP, to); var a = new Vector2(from.x, from.z); var b = new Vector2(to.x, to.z); var d = b - a; if (d.magnitude < 5f) return; a += d.normalized * 1.8f;
            FretPath(ctx, mk, new[] { a, Vector2.Lerp(a, b, .5f), b }, 2.2f, false);
        }

        void SpirePlaces(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            scaleP.Clear(); rootT = ctx.Root; ctx0 = ctx; prng = new System.Random(2012);
            plife = new GameObject("Djent places life").AddComponent<DjentPlacesLife>(); plife.transform.SetParent(life.transform, false);
            foreach (var (at, r) in PlaceRings) noGrass.Add(new Vector3(at.x, r, at.y));
            // the East Scale starts from the nearest point of the land's own paths
            Vector2 near = EastScale[0]; var bd = 200f * 200f; foreach (var (p, w) in walkPoints) { if (w >= 400) continue; var d = (p.x - EastScale[0].x) * (p.x - EastScale[0].x) + (p.z - EastScale[0].y) * (p.z - EastScale[0].y); if (d < bd) { bd = d; near = new Vector2(p.x, p.z); } }
            var full = new List<Vector2> { near }; full.AddRange(EastScale); var scale = FretPath(ctx, mk, full.ToArray(), PathW, true);
            PolyClock(ctx, mk, solid); TuningForks(ctx, mk, solid); HexGarden(ctx, mk, solid); Observatory(ctx, mk, solid);
            // two benches along the Scale, and someone walking it, counting
            for (var k = 1; k <= 2; k++) { var i = scale.Count * k / 3; if (i < 2 || i > scale.Count - 3) continue; var d = scale[i + 1] - scale[i - 1]; d.y = 0; d.Normalize(); var sd = Vector3.Cross(Vector3.up, d); var bp = scale[i] - sd * 3.6f; if (ctx.IsLand(bp.x, bp.z, 2f) && !InNoGrass(bp.x, bp.z, 2f)) { HexBench(mk, bp, sd); Ring(bp, 2f); } }
            if (scale.Count > 60) { var route = new List<Vector3>(); for (var i = 20; i < scale.Count - 20; i += 14) route.Add(ctx.OnGround(scale[i].x + .9f, scale[i].z)); for (var i = route.Count - 2; i > 0; i--) route.Add(route[i]); if (route.Count > 3) Folk(route[0], FaceTo(route[0], route[1]), Resident.Pose.Walk, new[] { "...five, six, seven, one, two... sorry. If I stop counting the whole walk falls apart.", "Clock's north. Forks are after the clock. Then the garden, for when you've had enough of being correct.", "Eleven steps to a bar. I've checked. I check every day." }, Savant(), route.ToArray()); }
        }

        // ---------------------------------------------------------------- the Polyrhythm Clock
        void PolyClock(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var g = ctx.OnGround(ClockAt.x, ClockAt.y); var f = new PlaceFrame(g, FaceTo(g, Nearest(scaleP, g)));
            // a hexagonal dais of white stone inlaid with gold; over it four gold rings turning at 3, 4, 5 and 7 to the bar
            mk.Prism(stoneLight, g - up * .5f, up, 8f, .8f, 6, 1f, 30f); solid.Prism(stoneLight, g - up * .5f, up, 8f, .8f, 6, 1f, 30f); mk.Prism(fretM, g + up * .31f, up, 6.6f, .02f, 6, 1f, 30f); mk.Prism(stoneLight, g + up * .32f, up, 6.3f, .02f, 6, 1f, 30f); mk.Prism(fretM, g + up * .33f, up, 3.2f, .02f, 6, 1f, 30f); mk.Prism(slate, g + up * .34f, up, 3f, .02f, 6, 1f, 30f);
            var hub = g + up * 5.4f; mk.Prism(stoneLight, g + up * .3f, up, .5f, 3.4f, 6, .5f); var hc = new GameObject("Clock column"); hc.transform.SetParent(rootT, false); hc.transform.position = g + up * 2f; var hcc = hc.AddComponent<CapsuleCollider>(); hcc.radius = .5f; hcc.height = 3.6f; Crystal(mk, iceGlow, g + up * 3.7f, up, .32f, 1.2f); halos.Add(hub, 3f, Ice, .4f);
            int[] beats = { 3, 4, 5, 7 }; Color[] bc = { Gold, Ice, Gold, Cyan };
            for (var k = 0; k < 4; k++)
            {
                var R = 2.2f + k * 1.15f; var rk = new MeshKit(); var seg = 36; for (var s = 0; s < seg; s++) { var a0 = s * Mathf.PI * 2f / seg; var a1 = (s + 1) * Mathf.PI * 2f / seg; var p0 = new Vector3(Mathf.Cos(a0), 0, Mathf.Sin(a0)) * R; var p1 = new Vector3(Mathf.Cos(a1), 0, Mathf.Sin(a1)) * R; rk.Box(goldM, (p0 + p1) / 2f, new Vector3(.12f, .12f, Vector3.Distance(p0, p1) + .02f), Quaternion.LookRotation(p1 - p0)); }
                var beadM = new Material(WorldMaterials.Glow(bc[k], 1.6f)) { name = "Clock bead" }; for (var b = 0; b < beats[k]; b++) { var a = b * Mathf.PI * 2f / beats[k]; rk.Blob(beadM, new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * R, Vector3.one * .26f, prng, 0f); }
                var rgo = rk.BuildSingle("Soft clock ring " + beats[k], plife.transform, false); rgo.transform.position = hub; rgo.transform.rotation = Quaternion.Euler(k * 23f - 30f, k * 40f, k * 17f - 20f); plife.rings.Add((rgo.transform, beats[k], rgo.transform.rotation, beadM, bc[k]));
            }
            plife.clockLight = ArtistBits.PointLight(ctx.Root, hub - up * 1f, Gold, 24f, 1.8f);
            // the tempo lever on a gold plinth
            var lp = g + up * .32f + f.Fwd * 5.4f + f.Right * 2.4f; mk.Prism(goldM, lp, up, .35f, .9f, 6); ArtistBits.PostCollider(ctx.Root, lp, .9f, .35f); var lv = new MeshKit(); lv.Box(metal, new Vector3(0, .3f, 0), new Vector3(.06f, .6f, .06f), Quaternion.identity); lv.Blob(iceGlow, new Vector3(0, .65f, 0), Vector3.one * .1f, prng, 0f);
            var lgo = lv.BuildSingle("Tempo lever", plife.transform, false); lgo.transform.SetPositionAndRotation(lp + up * .9f, f.r * Quaternion.Euler(0, 0, 25f)); string[] tempo = { "Andante. The beads drift round and almost never meet.", "Allegro. Now they meet every few seconds: a little flash each time.", "Presto. A blur of gold. They meet constantly. It is very satisfying and slightly worrying." };
            var lt = Interact.Toy(lgo, Toy.Kind.Custom, "Change the tempo", new Vector3(.8f, 1f, .8f)); lt.onUse = _ => { plife.tempo = (plife.tempo + 1) % 3; lgo.transform.rotation = f.r * Quaternion.Euler(0, 0, 25f - plife.tempo * 25f); Say(plife.transform, lp + up * 2.4f, tempo[plife.tempo], 5f); };
            foreach (var a in new[] { 150f, 210f }) { var d = Quaternion.Euler(0, a, 0) * f.Fwd; HexBench(mk, g + d * 6.9f + up * .0f, -d); }
            Folk(g + up * .32f + f.Fwd * 4.6f - f.Right * 2.6f, -f.Fwd, Resident.Pose.Stand, new[] { "Three against four against five against seven. They all land together once every four hundred and twenty beats. I wait for it.", "Pull the lever. Faster is not harder. Faster is just sooner.", "People ask what time it tells. All of them. At once." }, Savant());
            FretSign(mk, f.P(6.5f, 0, 11f), f.Fwd, "THE POLYRHYTHM\nCLOCK", 3.2f, .2f);
            Fingerboard(ctx, mk, f.P(0, 0, 8f)); Ring(g, 11f);
            Shot(ctx, "the polyrhythm clock", f.P(-9f, 4f, 13f), hub - up * 1.5f);
        }

        // ---------------------------------------------------------------- the Tuning Forks
        void TuningForks(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var g = ctx.OnGround(ForksAt.x, ForksAt.y); var f = new PlaceFrame(g, FaceTo(g, Nearest(scaleP, g)));
            mk.Box(stoneLight, g - up * .35f, new Vector3(18f, 1f, 6f), f.r); solid.Box(stoneLight, g - up * .35f, new Vector3(18f, 1f, 6f), f.r); mk.Box(fretM, g + up * .16f + f.Fwd * 2.7f, new Vector3(18f, .02f, .12f), f.r);
            // eight crystal forks in a row, low to high, each a little shorter: strike one and it rings with light
            string[] notes = { "F#", "B", "E", "A", "D", "G", "B", "E" }; string[] ring = { "a note you feel in your knees", "a low hum, like a far-off engine being kind", "a clear, round note", "the note everything else is tuned to", "a bright note with a shimmer on top", "a high, glassy note", "a note like a wet finger on a glass", "so high you mostly see it" };
            for (var k = 0; k < 8; k++)
            {
                var h = 4.6f - k * .38f; var p = g + up * .15f + f.Right * (-7.3f + k * 2.08f) - f.Fwd * .6f; var fm = new Material(WorldMaterials.Glow(Color.Lerp(new Color(.45f, .7f, 1f), new Color(.85f, .97f, 1f), k / 7f), .5f)) { name = "Tuning fork" };
                mk.Prism(goldM, p, up, .28f, .5f, 6); mk.Prism(fm, p + up * .5f, up, .1f, h * .35f, 6); mk.Box(fm, p + up * (.5f + h * .35f), new Vector3(.7f, .14f, .14f), f.r); foreach (var e in new[] { -.3f, .3f }) mk.Prism(fm, p + up * (.5f + h * .35f) + f.Right * e, up, .08f, h * .65f, 6);
                ArtistBits.PostCollider(ctx.Root, p, h + .5f, .32f); ArtistBits.Label(ctx.Root, notes[k], p + up * .25f + f.Fwd * .3f, f.Fwd, .16f, new Color(.2f, .2f, .3f), TextAnchor.MiddleCenter);
                var fgo = new GameObject("Tuning fork " + notes[k]); fgo.transform.SetParent(plife.transform, false); fgo.transform.position = p + up * 1.4f + f.Fwd * .5f; var idx = plife.forks.Count; var fl = ArtistBits.PointLight(ctx.Root, p + up * (h * .8f) + f.Fwd * .8f, Ice, 8f, 0f); plife.forks.Add((fm, fl, -99f, Color.Lerp(new Color(.45f, .7f, 1f), new Color(.85f, .97f, 1f), k / 7f)));
                var kk = k; Interact.Toy(fgo, Toy.Kind.Custom, "Strike the " + notes[k] + " fork", new Vector3(1.2f, 2.4f, 1.2f)).onUse = _ => { plife.Strike(idx); Say(plife.transform, p + up * (h + 1.2f), notes[kk] + ": " + ring[kk] + ".", 4f); };
            }
            // the striker on a rest, to carry (it does nothing; it just feels right), a bench
            var st = new MeshKit(); st.Prism(goldM, new Vector3(0, .02f, -.25f), Vector3.forward, .02f, .5f, 6); st.Blob(WorldMaterials.Solid(new Color(.92f, .94f, .98f)), new Vector3(0, .02f, .28f), Vector3.one * .06f, prng, 0f); var sgo = st.BuildSingle("Fork striker", plife.transform, false); sgo.transform.position = g + up * .2f + f.Fwd * 1.6f; var sc = Interact.Carry(sgo, "the little gold striker", .3f, g.y - 10f); var sbc = sc.GetComponent<BoxCollider>(); if (sbc != null) { sbc.size = new Vector3(.14f, .1f, .66f); sbc.center = new Vector3(0, .03f, 0); }
            HexBench(mk, g + f.Fwd * 5.5f - f.Right * 3f, -f.Fwd); HexBench(mk, g + f.Fwd * 5.5f + f.Right * 3f, -f.Fwd);
            Folk(G(g + f.Fwd * 5.5f + f.Right * 3f) + up * .56f - f.Right * .5f, -f.Fwd, Resident.Pose.Sit, new[] { "Eight of them. Seven would be a normal amount. That's why there are eight.", "Strike the low one last. It needs the run-up.", "I come and sit when my own tuning's gone. An hour here and I'm back to pitch." }, Savant(), null, .56f);
            FretSign(mk, f.P(8f, 0, 9f), f.Fwd, "THE TUNING FORKS", 3.2f, .2f);
            Fingerboard(ctx, mk, f.P(0, 0, 4f)); Ring(g, 8f);
            Shot(ctx, "the tuning forks", f.P(-8f, 3f, 12f), g + up * 2.2f);
        }

        // ---------------------------------------------------------------- the Hex Garden
        void HexGarden(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var g = ctx.OnGround(GardenAt.x, GardenAt.y); var f = new PlaceFrame(g, FaceTo(g, Nearest(scaleP, g))); var gravel = WorldMaterials.Solid(new Color(.16f, .19f, .3f)); var rake = WorldMaterials.Solid(new Color(.24f, .28f, .42f));
            // a hexagon of dark raked gravel in a white kerb with gold corners: rings raked round three crystal "rocks"
            mk.Prism(stoneLight, g - up * .4f, up, 10.6f, .62f, 6, 1f, 30f); solid.Prism(stoneLight, g - up * .4f, up, 10.6f, .5f, 6, 1f, 30f); mk.Prism(gravel, g + up * .16f, up, 9.8f, .08f, 6, 1f, 30f);
            for (var k = 0; k < 6; k++) { var d = Quaternion.Euler(0, k * 60f + 30f, 0) * Vector3.forward; mk.Prism(goldM, g + d * 10.3f + up * .1f, up, .32f, .5f, 6); }
            Vector3[] rocks = { g + f.Right * -3.4f + f.Fwd * -2f, g + f.Right * 3.6f + f.Fwd * -3.4f, g + f.Right * 1f + f.Fwd * 3.6f };
            for (var k = 0; k < 3; k++) { var p = rocks[k] + up * .2f; Crystal(mk, k == 1 ? crystalDeep : crystal, p, (up + new Vector3(.15f * (k - 1), 0, .1f)).normalized, .5f + k * .12f, 2.2f + k * .6f); Crystal(mk, crystal, p + new Vector3(.6f, 0, .3f), (up + new Vector3(.3f, 0, .2f)).normalized, .28f, 1.2f, 20f); ArtistBits.PostCollider(ctx.Root, p, 2.2f, .7f); for (var r = 1; r <= 3; r++) { var seg = 18; for (var s = 0; s < seg; s++) { var a0 = s * Mathf.PI * 2f / seg; var a1 = (s + 1) * Mathf.PI * 2f / seg; var p0 = p + new Vector3(Mathf.Cos(a0), 0, Mathf.Sin(a0)) * (.9f + r * .55f); var p1 = p + new Vector3(Mathf.Cos(a1), 0, Mathf.Sin(a1)) * (.9f + r * .55f); if (Vector3.Distance(new Vector3(p0.x, g.y, p0.z), g) > 9f) continue; mk.Box(rake, (p0 + p1) / 2f + up * .05f, new Vector3(.07f, .03f, Vector3.Distance(p0, p1) + .02f), Quaternion.LookRotation(p1 - p0)); } } halos.Add(p + up * 1.4f, 1.6f, Ice, .4f); }
            for (var k = -6; k <= 6; k++) { var a = g + f.Right * (k * 1.2f) - f.Fwd * 7.2f; var b = g + f.Right * (k * 1.2f) + f.Fwd * 7.2f; var skip = false; foreach (var r in rocks) if (Mathf.Abs(Vector3.Dot(r - g, f.Right) - k * 1.2f) < 2.6f) skip = true; if (!skip && Mathf.Abs(k) < 6) mk.Box(rake, (a + b) / 2f + up * .25f, new Vector3(.07f, .03f, 13f - Mathf.Abs(k) * 1.1f), f.r); }
            // stepping stones across to the tea pavilion: a roof on four white posts, a low table, cushions, a gold kettle
            for (var k = 0; k < 6; k++) { var sp = g + f.Fwd * (8.6f - k * 1.9f) + f.Right * (5.4f + Mathf.Sin(k * 1.3f) * .5f) + up * .21f; mk.Prism(stoneLight, sp, up, .55f, .08f, 6, 1f, k * 20f); }
            var pv = g + f.Right * 5.6f - f.Fwd * 2.2f + up * .2f; mk.Prism(stoneLight, pv, up, 2.6f, .14f, 6, 1f, 30f); foreach (var a in new[] { 30f, 150f, 210f, 330f }) { var d = Quaternion.Euler(0, a, 0) * f.Fwd; mk.Prism(stoneLight, pv + d * 2.2f, up, .09f, 2.6f, 6); ArtistBits.PostCollider(ctx.Root, pv + d * 2.2f, 2.6f, .1f); }
            mk.Cone(slate, pv + up * 2.6f, 3.3f, 1.1f, 6, 30f); mk.Prism(goldM, pv + up * 3.7f, up, .1f, .5f, 6); mk.Prism(slate, pv + up * .14f, up, .7f, .3f, 6); var tcol = new GameObject("Tea table"); tcol.transform.SetParent(rootT, false); tcol.transform.position = pv + up * .3f; tcol.AddComponent<BoxCollider>().size = new Vector3(1.2f, .35f, 1.2f);
            mk.Prism(goldM, pv + up * .44f, up, .12f, .18f, 8, .8f); mk.Blob(iceGlow, pv + up * 2.3f, Vector3.one * .16f, prng, 0f); halos.Add(pv + up * 2.3f, 1.4f, Ice, .5f); ArtistBits.PointLight(ctx.Root, pv + up * 2.1f, new Color(.85f, .93f, 1f), 10f, 1.4f);
            for (var k = 0; k < 3; k++) { var d = Quaternion.Euler(0, k * 120f + 60f, 0) * f.Fwd; var c = pv + d * 1.35f; mk.Prism(WorldMaterials.Solid(k == 0 ? new Color(.2f, .26f, .42f) : new Color(.92f, .94f, .98f)), c + up * .14f, up, .4f, .14f, 6); if (k == 0) Folk(c + up * .3f, -d, Resident.Pose.Sit, new[] { "After all that counting, you want somewhere that's just lines.", "The gravel's raked every morning. By evening it's still perfect. Nobody dares.", "Tea. No time signature. Sit." }, Savant(Residents.Tool.Mug), null, .3f); else Interact.Seat(plife.transform, c + up * .34f, -d, "Sit on the cushion"); }
            for (var k = 0; k < 2; k++) { var cm = new MeshKit(); cm.Prism(WorldMaterials.Solid(new Color(.92f, .94f, .98f)), Vector3.zero, Vector3.up, .045f, .07f, 6, 1.2f); var go = cm.BuildSingle("Tea bowl", plife.transform, false); go.transform.position = pv + up * .45f + f.Right * (.3f - k * .6f) + f.Fwd * .25f; var c = Interact.Carry(go, "a bowl of tea", .2f, g.y - 10f); var bc = c.GetComponent<BoxCollider>(); if (bc != null) { bc.size = new Vector3(.12f, .09f, .12f); bc.center = new Vector3(0, .04f, 0); } }
            // the rake left leaning: use it and a few lines of gravel glow gold where you drew them
            var rk = g + f.Fwd * 9.4f - f.Right * 3f; mk.Box(stoneLight, rk + up * .9f, new Vector3(.05f, 1.8f, .05f), f.r * Quaternion.Euler(0, 0, 12f)); mk.Box(goldM, rk + up * .08f + f.Right * .18f, new Vector3(.6f, .06f, .06f), f.r); var rgo = new GameObject("Garden rake"); rgo.transform.SetParent(plife.transform, false); rgo.transform.position = rk + up * 1f;
            var lineM = new Material(WorldMaterials.Glow(Gold, 0f)) { name = "Raked gold" }; for (var k = 0; k < 5; k++) mk.Box(lineM, g + f.Fwd * (6.4f - k * .35f) - f.Right * 2f + up * .26f, new Vector3(5f, .03f, .08f), f.r * Quaternion.Euler(0, Mathf.Sin(k) * 4f, 0)); plife.rakeM = lineM;
            Interact.Toy(rgo, Toy.Kind.Custom, "Rake the gravel", new Vector3(.8f, 1.8f, .8f)).onUse = _ => { plife.raked = Time.time; Say(plife.transform, rk + up * 2.4f, "Five slow lines. For a moment they shine. Then they are just part of it.", 5f); };
            FretSign(mk, f.P(7.5f, 0, 13.5f), f.Fwd, "THE HEX GARDEN\nplease do nothing", 3.2f, .18f);
            Fingerboard(ctx, mk, f.P(0, 0, 10.6f)); Ring(g, 11f);
            Shot(ctx, "the hex garden", f.P(-9f, 5f, 14f), g + up * .6f);
        }

        // ---------------------------------------------------------------- the Observatory
        void Observatory(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var g = ctx.OnGround(ObsAt.x, ObsAt.y); var f = new PlaceFrame(g, FaceTo(g, Nearest(scaleP, g))); const float R = 6f, H = 3.4f, Door = HumanScale.DoorWidth + .4f; const int N = 12;
            // a twelve-sided drum of white stone with a gold band and a dark dome, a slit of light down it; walk in
            mk.Prism(stoneLight, g - up * .4f, up, R + .9f, .66f, N); solid.Prism(stoneLight, g - up * .4f, up, R + .9f, .5f, N); var fo = g + up * .26f; mk.Prism(slate, fo - up * .04f, up, R - .2f, .06f, N); mk.Prism(fretM, fo + up * .03f, up, 2.2f, .01f, 6); mk.Prism(slate, fo + up * .035f, up, 2.05f, .01f, 6);
            for (var k = 0; k < N; k++)
            {
                var a0 = (k - .5f) * Mathf.PI * 2f / N; var a1 = (k + .5f) * Mathf.PI * 2f / N; var d0 = f.r * new Vector3(Mathf.Sin(a0), 0, Mathf.Cos(a0)); var d1 = f.r * new Vector3(Mathf.Sin(a1), 0, Mathf.Cos(a1)); var p0 = fo + d0 * R; var p1 = fo + d1 * R; var mid = (p0 + p1) / 2f; var q = Quaternion.LookRotation(p1 - p0); var len = Vector3.Distance(p0, p1) + .04f;
                if (k == 0) { var sd = (p1 - p0).normalized; var side = (len - Door) / 2f; foreach (var e in new[] { -1f, 1f }) { var c = mid + sd * e * (Door / 2f + side / 2f); mk.Box(stoneLight, c + up * (H / 2f), new Vector3(.35f, H, side), q); solid.Box(stoneLight, c + up * (H / 2f), new Vector3(.35f, H, side), q); } mk.Box(stoneLight, mid + up * (H - .4f), new Vector3(.35f, .8f, Door), q); solid.Box(stoneLight, mid + up * (H - .4f), new Vector3(.35f, .8f, Door), q); mk.Box(fretM, mid + up * (H - .82f) + f.Fwd * .19f, new Vector3(.03f, .05f, Door), q); }
                else { mk.Box(stoneLight, mid + up * (H / 2f), new Vector3(.35f, H, len), q); solid.Box(stoneLight, mid + up * (H / 2f), new Vector3(.35f, H, len), q); if (k % 3 == 0) mk.Box(iceGlow, mid + up * 2f + (mid - fo).normalized * .19f, new Vector3(.03f, 1.2f, .5f), q); }
                mk.Box(goldM, mid + up * (H + .08f), new Vector3(.45f, .16f, len), q);
                // the dome: two rings of slate facets leaning in, open along the slit over the door's line
                if (k != 0 && k != N / 2) { var top0 = fo + up * (H + 2.6f) + (mid - fo).normalized * (R * .55f); mk.Box(slate, (mid + up * (H + .1f) + top0) / 2f, new Vector3(.18f, Vector3.Distance(mid + up * (H + .1f), top0) + .1f, len * .82f), Quaternion.LookRotation(Vector3.Cross(up, (mid - fo).normalized), (top0 - mid - up * (H + .1f)).normalized)); var top1 = fo + up * (H + 3.7f) + (mid - fo).normalized * .5f; mk.Box(slate, (top0 + top1) / 2f, new Vector3(.18f, Vector3.Distance(top0, top1) + .1f, len * .5f), Quaternion.LookRotation(Vector3.Cross(up, (mid - fo).normalized), (top1 - top0).normalized)); }
            }
            IndoorSpaces.Add(fo + up * (H / 2f), f.r, new Vector3(R * 1.5f, H, R * 1.5f));
            // inside: the telescope on its pier, pointing out through the slit; the orrery to turn; star charts; a stool
            var pier = fo - f.Fwd * 1.6f; mk.Prism(stoneLight, pier, up, .5f, 1.3f, 6); var pcc = new GameObject("Telescope pier"); pcc.transform.SetParent(rootT, false); pcc.transform.position = pier + up * .65f; var pc = pcc.AddComponent<CapsuleCollider>(); pc.radius = .5f; pc.height = 1.4f; var aim = (-f.Fwd * .5f + up).normalized; mk.Prism(goldM, pier + up * 1.3f - aim * 1.2f, aim, .3f, 4.4f, 10, .8f); mk.Prism(goldM, pier + up * 1.3f - aim * 1.5f, aim, .12f, .5f, 8); mk.Prism(metal, pier + up * 1.2f, up, .2f, .4f, 6);
            var tgo = new GameObject("Observatory telescope"); tgo.transform.SetParent(plife.transform, false); tgo.transform.position = pier + up * 1.2f + f.Fwd * 1.1f; string[] sky = { "A star that is really two stars, turning round each other in five-four.", "The citadel, close up: copies of copies. You lose count at the ninth.", "A very ordinary star. The astronomer has circled it on the chart and written \"mine\".", "Nothing, for a while. Then, exactly on the beat, a meteor." }; var sn = 0;
            Interact.Toy(tgo, Toy.Kind.Custom, "Look through the telescope", new Vector3(.9f, 1.2f, .9f)).onUse = _ => Say(plife.transform, pier + up * 3f + f.Fwd * .6f, sky[sn++ % sky.Length], 6f);
            var oc = fo + f.Right * 3.2f + f.Fwd * 1f; mk.Prism(slate, oc, up, .6f, .9f, 6); var occ = new GameObject("Orrery stand"); occ.transform.SetParent(rootT, false); occ.transform.position = oc + up * .45f; occ.AddComponent<BoxCollider>().size = new Vector3(1.1f, .9f, 1.1f);
            var om = new MeshKit(); om.Blob(goldGlow, new Vector3(0, .5f, 0), Vector3.one * .2f, prng, 0f); for (var k = 0; k < 4; k++) { var r = .35f + k * .22f; var a = k * 1.7f; om.Box(metal, new Vector3(Mathf.Cos(a) * r / 2f, .5f, Mathf.Sin(a) * r / 2f), new Vector3(.02f, .02f, r), Quaternion.LookRotation(new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)))); om.Blob(k % 2 == 0 ? iceGlow : crystalDeep, new Vector3(Mathf.Cos(a) * r, .5f, Mathf.Sin(a) * r), Vector3.one * (.05f + k * .015f), prng, 0f); }
            var ogo = om.BuildSingle("Orrery", plife.transform, false); ogo.transform.position = oc + up * .9f; var ot = Interact.Toy(ogo, Toy.Kind.Spin, "Turn the orrery", new Vector3(1.4f, 1.2f, 1.4f)); ogo.GetComponent<BoxCollider>().center = new Vector3(0, .5f, 0); ot.axis = Vector3.up; ot.amount = 720f; ot.seconds = 6f;
            for (var k = 0; k < 3; k++) { var a = (k + 7.5f) * Mathf.PI * 2f / N; var d = f.r * new Vector3(Mathf.Sin(a), 0, Mathf.Cos(a)); var p = fo + d * (R - .25f) + up * 1.8f; mk.Box(slate, p, new Vector3(1.6f, 1.2f, .04f), Quaternion.LookRotation(-d)); for (var s = 0; s < 9; s++) mk.Blob(s % 4 == 0 ? goldGlow : iceGlow, p - d * .03f + Quaternion.LookRotation(-d) * new Vector3(-.65f + (s * 37 % 13) / 10f, -.45f + (s * 53 % 9) / 10f, 0), Vector3.one * (.02f + (s % 3) * .012f), prng, 0f); }
            var st = fo - f.Right * 3f + f.Fwd * .5f; mk.Prism(stoneLight, st, up, .28f, .5f, 6); var stc = new GameObject("Stool"); stc.transform.SetParent(rootT, false); stc.transform.position = st + up * .25f; stc.AddComponent<BoxCollider>().size = new Vector3(.5f, .5f, .5f); Interact.Seat(plife.transform, st + up * .56f, f.Right, "Sit on the stool");
            Folk(fo - f.Right * 2f - f.Fwd * 2.6f, f.Fwd + f.Right * .4f, Resident.Pose.Stand, new[] { "The sky here keeps perfect time. It's the only thing in the Spires that doesn't have to practise.", "Look through. Tell me if it's still there. I worry.", "That star? I named it after a chord. The chord's better. The star's fine." }, Savant(Residents.Tool.Mug));
            ArtistBits.PointLight(ctx.Root, fo + up * 2.6f, new Color(.85f, .93f, 1f), 12f, 2.6f, true); halos.Add(fo + up * (H + 4.4f), 2.4f, Ice, .4f); mk.Blob(iceGlow, fo + up * (H + 3.9f), Vector3.one * .2f, prng, 0f);
            HexBench(mk, g + f.Fwd * (R + 2.6f) - f.Right * 3.4f, f.Fwd); Lantern(ctx, mk, G(g + f.Fwd * (R + 2.6f) + f.Right * 3.6f));
            FretSign(mk, f.P(6.5f, 0, R + 7f), f.Fwd, "THE OBSERVATORY", 3.2f, .2f);
            Fingerboard(ctx, mk, f.P(0, 0, R + 1.6f)); Ring(g, 10f);
            Shot(ctx, "the observatory", f.P(-8f, 3.4f, R + 12f), fo + up * 3.4f);
            Shot(ctx, "inside the observatory", fo + f.Fwd * (R - 1.6f) + up * 1.6f, pier + up * 2.2f);
        }
    }

    /// The Djent places' moving parts: the polyrhythm clock's rings, the forks ringing with light, the raked lines.
    public class DjentPlacesLife : MonoBehaviour
    {
        public List<(Transform t, int beats, Quaternion tilt, Material bead, Color c)> rings = new List<(Transform, int, Quaternion, Material, Color)>(); public int tempo; public Light clockLight; float phase;
        public List<(Material m, Light l, float hit, Color c)> forks = new List<(Material, Light, float, Color)>(); public Material rakeM; public float raked = -99f;
        static readonly Color Gold = new Color(1f, .8f, .32f);

        public void Strike(int i) { var f = forks[i]; f.hit = Time.time; forks[i] = f; }

        void Update()
        {
            var t = Time.time; var bpm = tempo == 0 ? 40f : tempo == 1 ? 90f : 180f; phase += Time.deltaTime * bpm / 60f / 4f;   // one bar per four beats
            var together = 0f;
            foreach (var (tr, beats, tilt, bead, c) in rings) { var turn = phase / beats; tr.rotation = tilt * Quaternion.Euler(0, turn * 360f, 0); var on = Mathf.Repeat(phase * beats / 4f, 1f); var flash = Mathf.Exp(-on * 6f); together += flash; bead.SetColor("_EmissionColor", c * (1.6f + flash * 3f)); }
            if (clockLight != null) clockLight.intensity = 1.8f + together * .5f;
            for (var i = 0; i < forks.Count; i++) { var (m, l, hit, c) = forks[i]; var a = t - hit; var ring = a < 6f ? Mathf.Exp(-a * .7f) * (.75f + .25f * Mathf.Sin(a * (30f + i * 6f))) : 0f; m.SetColor("_EmissionColor", c * (.5f + ring * 4f)); if (l != null) l.intensity = ring * 3f; }
            if (rakeM != null) { var a = t - raked; rakeM.SetColor("_EmissionColor", Gold * (a < 8f ? 2.4f * (1f - a / 8f) : 0f)); }
        }
    }
}
