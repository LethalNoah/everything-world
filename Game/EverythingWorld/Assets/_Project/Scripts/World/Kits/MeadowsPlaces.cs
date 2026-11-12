using System.Collections.Generic;
using UnityEngine;
using static EverythingWorld.World.PlaceTools;

namespace EverythingWorld.World
{
    /// The Bedroom Pop Meadows laid out on purpose (the build-land standard), gently: the loop, the bed and every
    /// daydream stay where they were. Three side tracks leave the loop, lined with fairy lights like it, for the empty
    /// east, west and south. At their ends, more bedroom things dreamed out into the field, with a few sleepy people:
    /// the Record Player (a turntable the size of a room: sit on the record and go slowly round); the Window Seat (a
    /// bedroom window standing alone in the grass, a moon in it, a cat on the sill); the Bubble Bath (a clawfoot tub
    /// you can sit in, bubbles drifting up, a rubber duck); the Sleepover (sleeping bags round a torch, pillows to
    /// throw); and the Notebook (open on the grass with its doodles glowing, a pencil to sit on, cocoa, a tape player).
    public partial class MeadowsKit
    {
        static readonly Vector2 PlayerAt = new Vector2(9150, 6556), WindowAt = new Vector2(9230, 6426), BathAt = new Vector2(8618, 6404), SleepAt = new Vector2(8412, 6352), NoteAt = new Vector2(8840, 6150);
        static readonly Vector2[] EastTrack = { new Vector2(9045, 6405), new Vector2(9095, 6450), new Vector2(9125, 6505), new Vector2(9140, 6535) };
        static readonly Vector2[] WindowTrack = { new Vector2(9095, 6450), new Vector2(9150, 6442), new Vector2(9195, 6432), new Vector2(9215, 6428) };
        static readonly Vector2[] WestTrack = { new Vector2(8700, 6335), new Vector2(8660, 6375), new Vector2(8600, 6392), new Vector2(8530, 6375), new Vector2(8470, 6360), new Vector2(8428, 6354) };
        static readonly Vector2[] SouthTrack = { new Vector2(8800, 6232), new Vector2(8826, 6195), new Vector2(8838, 6168) };
        static readonly (Vector2 at, float r)[] PlaceRings = { (PlayerAt, 11f), (WindowAt, 9f), (BathAt, 9f), (SleepAt, 9f), (NoteAt, 12f) };

        MeadowsPlacesLife plife; System.Random prng; Transform rootT; LandContext ctx0; readonly List<Vector3> trackP = new List<Vector3>();

        static readonly Color[] Pyjamas = { new Color(.75f, .7f, .9f), new Color(.95f, .75f, .8f), new Color(.7f, .85f, .9f), new Color(.95f, .9f, .7f), new Color(.75f, .9f, .78f), new Color(.92f, .88f, .94f) };
        static readonly string[] Sleepy =
        {
            "I've had the same song on for an hour. It keeps being exactly right.", "Nobody's told us to go to bed. I don't think anybody's going to.", "If you walk the loop enough times you stop thinking. That's the good part.",
            "The stars are stickers. Somebody put every one of them up by hand.", "You can stay as long as you like. That's the whole idea of the place.",
        };

        Residents.Look Dreamer(Residents.Tool tool = Residents.Tool.None) { var l = Residents.Random(prng, Pyjamas, Pyjamas, new[] { Residents.Hat.None, Residents.Hat.Beanie, Residents.Hat.None }, Pyjamas); l.tool = tool; if (prng.Next(2) == 0) l.longHair = true; return l; }
        Resident Folk(Vector3 feet, Vector3 face, Resident.Pose pose, string[] lines = null, Residents.Look look = null, Vector3[] route = null, float seatH = HumanScale.SeatHeight)
            => Residents.Make(plife.transform, feet, face, look ?? Dreamer(), pose, prng, lines ?? Sleepy, route, seatH);
        void Col(Vector3 c, Vector3 size, Quaternion rot) { var go = new GameObject("Box collider"); go.transform.SetParent(rootT, false); go.transform.SetPositionAndRotation(c, rot); go.AddComponent<BoxCollider>().size = size; go.isStatic = true; }
        Vector3 G(Vector3 p) => ctx0.OnGround(p.x, p.z);
        Material Soft(Color c) => WorldMaterials.Solid(c);

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

        /// A note pinned to a stick. Signs go on the +x side of a place's track; the photo spot on the −x side.
        void PinnedNote(MeshKit mk, Vector3 at, Vector3 face, string text, Color paper, float w = 1.8f, float size = .13f)
        {
            var up = Vector3.up; var g = G(at); var rot = Quaternion.LookRotation(face) * Quaternion.Euler(0, 0, 4f);
            mk.Box(wood, g + up * .7f, new Vector3(.07f, 1.4f, .07f), Quaternion.identity); mk.Box(Soft(paper), g + up * 1.5f, new Vector3(w, w * .62f, .04f), rot); mk.Blob(WorldMaterials.Glow(new Color(1f, .4f, .5f), 1.6f), g + up * (1.5f + w * .26f) + face * .04f, Vector3.one * .05f, prng, 0f);
            Col(g + up * .95f, new Vector3(w, 1.9f, .12f), rot); ArtistBits.Label(rootT, text, g + up * 1.48f + face * .03f, face, size, Ink, TextAnchor.MiddleCenter);
        }

        /// A big floor cushion you can sit on.
        void Cushion(MeshKit mk, Vector3 at, Color c, Vector3 face, float r = .55f)
        {
            var g = G(at); mk.Blob(Soft(c), g + Vector3.up * .2f, new Vector3(r, .22f, r), prng, .08f); Col(g + Vector3.up * .2f, new Vector3(r * 1.6f, .4f, r * 1.6f), Quaternion.identity); Interact.Seat(plife.transform, g + Vector3.up * .42f, face, "Sit on the cushion");
        }

        List<Vector3> FairyTrack(LandContext ctx, MeshKit mk, Vector2[] way)
        {
            var pts = PlaceTools.Smooth(ctx, way, false, 2f);
            for (var i = 0; i < pts.Count; i++) foreach (var a in ctx.Artists) { var r = a.r + 3f; var v = new Vector2(pts[i].x - a.x, pts[i].z - a.z); var d = v.magnitude; if (d < r) { v = d > .01f ? v / d * r : new Vector2(r, 0); pts[i] = ctx.OnGround(a.x + v.x, a.z + v.y); } }
            if (pts.Count < 2) return pts;
            Ribbon(ctx, mk, track, pts, 2.6f, .05f); foreach (var p in pts) Remember(p);
            // fairy lights on short stakes down both sides, like the loop's
            var up = Vector3.up;
            foreach (var e in new[] { -1f, 1f })
            {
                var line = new List<Vector3>();
                for (var i = 0; i < pts.Count; i += 3) { var d = pts[Mathf.Min(i + 1, pts.Count - 1)] - pts[Mathf.Max(i - 1, 0)]; d.y = 0; if (d.sqrMagnitude < .01f) continue; d.Normalize(); var sp = pts[i] + Vector3.Cross(up, d) * e * 1.7f; sp = ctx.OnGround(sp.x, sp.z); soft.Box(wood, sp + up * .45f, new Vector3(.04f, .9f, .04f), Quaternion.identity); line.Add(sp + up * (.85f - (i / 3 % 2) * .12f)); }
                if (line.Count > 1) FairyString(soft, line, FairyWarm);   // in the soft mesh: you walk through them, like the loop's
            }
            for (var i = 10; i < pts.Count; i += 22) halos.Add(pts[i] + up * 1f, 2.2f, FairyWarm, .22f);
            trackP.AddRange(pts);
            return pts;
        }

        void MeadowPlaces(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            trackP.Clear(); rootT = ctx.Root; ctx0 = ctx; prng = new System.Random(2019);
            plife = new GameObject("Meadows places life").AddComponent<MeadowsPlacesLife>(); plife.transform.SetParent(ctx.Root, false);
            foreach (var (at, r) in PlaceRings) ctx.AddKeepout(at.x, at.y, r);
            FairyTrack(ctx, mk, EastTrack); FairyTrack(ctx, mk, WindowTrack); var west = FairyTrack(ctx, mk, WestTrack); FairyTrack(ctx, mk, SouthTrack);
            RecordPlayer(ctx, mk, solid); WindowSeat(ctx, mk, solid); BubbleBath(ctx, mk, solid); Sleepover(ctx, mk, solid); Notebook(ctx, mk, solid);
            // someone pacing the west track with headphones on, the way you do
            if (west.Count > 20) { var route = new List<Vector3>(); for (var i = 4; i < west.Count - 4; i += 6) route.Add(ctx.OnGround(west[i].x, west[i].z + .8f)); for (var i = route.Count - 2; i > 0; i--) route.Add(route[i]); if (route.Count > 3) { var l = Dreamer(); l.hat = Residents.Hat.Beanie; Folk(route[0], FaceTo(route[0], route[1]), Resident.Pose.Walk, new[] { "Sorry, headphones. One sec. ...No, it's still the good bit.", "I do my best thinking going back and forth. I haven't thought of anything yet.", "There's a sleepover at the end of the track. They never sleep." }, l, route.ToArray()).speed = .9f; } }
        }

        // ---------------------------------------------------------------- the Record Player
        void RecordPlayer(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var g = ctx.OnGround(PlayerAt.x, PlayerAt.y); var f = new PlaceFrame(g, FaceTo(g, Nearest(trackP, g))); const float PW = 11f, PD = 9f, PH = 1.1f, R = 3.6f;
            var caseM = Soft(new Color(.86f, .74f, .86f)); mk.Box(caseM, g + up * (PH / 2f - .2f), new Vector3(PW, PH + .4f, PD), f.r); solid.Box(caseM, g + up * (PH / 2f - .2f), new Vector3(PW, PH + .4f, PD), f.r); var top = new PlaceFrame(g + up * PH, f.Fwd);
            for (var k = 0; k < 3; k++) { var sp = g + f.Fwd * (PD / 2f + .25f + k * .4f) + up * (PH - .37f - k * .37f - .18f); mk.Box(Soft(Cream), sp, new Vector3(2.4f, .37f, .42f), f.r); solid.Box(Soft(Cream), sp, new Vector3(2.4f, .37f, .42f), f.r); }
            // the record: turning slowly; sit on it and go round
            var rec = new MeshKit(); rec.Prism(Soft(Cream), new Vector3(0, 0, 0), Vector3.up, R + .25f, .12f, 28); rec.Prism(board, new Vector3(0, .12f, 0), Vector3.up, R, .06f, 28); foreach (var rr in new[] { R * .85f, R * .7f, R * .55f }) rec.Prism(Soft(new Color(.26f, .22f, .32f)), new Vector3(0, .181f, 0), Vector3.up, rr, .004f, 28);
            rec.Prism(WorldMaterials.Glow(new Color(1f, .7f, .8f), .6f), new Vector3(0, .185f, 0), Vector3.up, R * .34f, .01f, 20); rec.Box(Soft(Ink), new Vector3(.5f, .2f, 0), new Vector3(.5f, .01f, .08f), Quaternion.identity); rec.Prism(WorldMaterials.Solid(new Color(.8f, .8f, .85f), .8f), new Vector3(0, .18f, 0), Vector3.up, .1f, .25f, 8);
            var rgo = rec.BuildSingle("Giant record", plife.transform, false); rgo.transform.position = top.P(-1.2f, .05f, 0); var rc = rgo.AddComponent<BoxCollider>(); rc.size = new Vector3(R * 1.5f, .18f, R * 1.5f); rc.center = new Vector3(0, .09f, 0); plife.record = rgo.transform;
            for (var k = 0; k < 3; k++) { var a = k * 120f * Mathf.Deg2Rad; var sp = rgo.transform.position + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * (R * .72f) + up * .26f; Interact.Seat(rgo.transform, sp, new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)), "Sit on the record"); }
            // the tonearm, the speed switch, two speakers that breathe with the music
            var pivot = top.P(3.6f, 0, -2.6f); mk.Prism(WorldMaterials.Solid(new Color(.8f, .8f, .85f), .8f), pivot, up, .35f, .7f, 10); var tip = rgo.transform.position + (pivot - rgo.transform.position).normalized * (R * .8f) + up * .35f; mk.Prism(WorldMaterials.Solid(new Color(.8f, .8f, .85f), .8f), pivot + up * .6f, (tip - pivot - up * .6f).normalized, .08f, Vector3.Distance(pivot + up * .6f, tip), 6); mk.Box(Soft(Ink), tip, new Vector3(.3f, .25f, .5f), Quaternion.LookRotation(FaceTo(pivot, tip))); Col(pivot + up * .4f, new Vector3(.8f, .8f, .8f), f.r);
            var sw = new MeshKit(); sw.Box(WorldMaterials.Glow(FairyWarm, 1.6f), new Vector3(0, .12f, 0), new Vector3(.5f, .24f, .3f), Quaternion.identity); var sgo = sw.BuildSingle("Speed switch", plife.transform, false); sgo.transform.SetPositionAndRotation(top.P(3.9f, 0, 2.6f), f.r); mk.Box(Soft(Ink), top.P(3.9f, .01f, 2.6f), new Vector3(1.3f, .02f, .7f), f.r); ArtistBits.Label(ctx.Root, "33      45", top.P(3.9f, .03f, 3.05f), up, .12f, Cream, TextAnchor.MiddleCenter);
            Interact.Toy(sgo, Toy.Kind.Custom, "Flick the speed switch", new Vector3(.8f, .6f, .6f)).onUse = _ => { plife.fast = !plife.fast; Say(plife.transform, top.P(3.9f, 1.4f, 2.6f), plife.fast ? "45. The field goes round a little quicker." : "33. Slow again. Better.", 3.5f); };
            foreach (var e in new[] { -1f, 1f })
            {
                var sp = G(f.P(e * (PW / 2f + 2.6f), 0, -1.5f)); var sr = Quaternion.LookRotation(FaceTo(sp, g + f.Fwd * 12f)); mk.Box(Soft(new Color(.75f, .62f, .5f)), sp + up * 2f, new Vector3(2.4f, 4f, 1.8f), sr); solid.Box(wood, sp + up * 2f, new Vector3(2.4f, 4f, 1.8f), sr);
                var cm = new Material(WorldMaterials.Glow(new Color(.85f, .7f, 1f), .4f)) { name = "Speaker cone" }; mk.Prism(cm, sp + up * 1.4f + sr * Vector3.forward * .9f, sr * Vector3.forward, .85f, .05f, 14); mk.Prism(cm, sp + up * 3.1f + sr * Vector3.forward * .9f, sr * Vector3.forward, .4f, .05f, 12); plife.cones.Add(cm);
            }
            plife.speakerLight = ArtistBits.PointLight(ctx.Root, top.P(0, 3f, 1f), new Color(.9f, .75f, 1f), 22f, 1.4f);
            Cushion(mk, f.P(-3f, 0, PD / 2f + 3f), Pastel[1 % Pastel.Length], -f.Fwd); Cushion(mk, f.P(-1.4f, 0, PD / 2f + 3.6f), Pastel[3 % Pastel.Length], -f.Fwd);
            Folk(G(f.P(-3f, 0, PD / 2f + 3f)) + up * .42f, -f.Fwd, Resident.Pose.Sit, new[] { "Side A's twenty minutes. I've never turned it over. Side A's enough.", "Sit on it. You go round, the song goes round, it all goes round.", "Someone flicked it to 45 once. We don't talk about it." }, Dreamer(), null, .42f);
            PinnedNote(mk, f.P(4.5f, 0, PD / 2f + 4.5f), f.Fwd, "the record player\n(side A, forever)", Pastel[0]);
            Shot(ctx, "the record player", f.P(-8f, 5f, PD / 2f + 10f), top.P(0, .6f, 0));
        }

        // ---------------------------------------------------------------- the Window Seat
        void WindowSeat(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var g = ctx.OnGround(WindowAt.x, WindowAt.y); var f = new PlaceFrame(g, FaceTo(g, Nearest(trackP, g))); const float WW = 12f, WH = 8f;
            // a piece of bedroom wall standing in the grass: wallpaper, a big window, the night and a moon in it
            var paper = Soft(new Color(.72f, .66f, .86f)); var wc = f.P(0, 0, -3f);
            void Wall(float x, float y, float w, float h) { mk.Box(paper, wc + f.r * new Vector3(x, y, 0), new Vector3(w, h, .4f), f.r); solid.Box(paper, wc + f.r * new Vector3(x, y, 0), new Vector3(w, h, .4f), f.r); }
            Wall(-4.25f, WH / 2f - .3f, 3.5f, WH + .6f); Wall(4.25f, WH / 2f - .3f, 3.5f, WH + .6f); Wall(0, .45f, 4.96f, 1.5f); Wall(0, 7.1f, 4.96f, 1.8f);
            for (var k = 0; k < 14; k++) { var sx = -5.6f + (k * 37 % 113) / 10f; if (Mathf.Abs(sx) < 2.7f) sx += Mathf.Sign(sx + .01f) * 2.8f; mk.Blob(WorldMaterials.Glow(new Color(.85f, 1f, .7f), 1.2f), wc + f.r * new Vector3(Mathf.Clamp(sx, -5.7f, 5.7f), 1f + (k * 53 % 62) / 10f, .22f), Vector3.one * .07f, prng, 0f); }
            var night = new Material(WorldMaterials.Glow(new Color(.12f, .14f, .4f), .5f)) { name = "Window night" }; mk.Box(night, wc + f.r * new Vector3(0, 3.7f, -.1f), new Vector3(5f, 5f, .05f), f.r); solid.Box(paper, wc + f.r * new Vector3(0, 3.7f, 0), new Vector3(5f, 5f, .2f), f.r);
            var moonM = new Material(WorldMaterials.Glow(new Color(1f, .96f, .8f), 2.2f)) { name = "Window moon" }; mk.Prism(moonM, wc + f.r * new Vector3(1f, 4.6f, -.06f), f.Fwd, 1f, .03f, 20); plife.moonM = moonM; halos.Add(wc + f.r * new Vector3(1f, 4.6f, .5f), 2.6f, new Color(1f, .95f, .8f), .45f); plife.moonLight = ArtistBits.PointLight(ctx.Root, wc + f.r * new Vector3(0, 3.6f, 2.2f), new Color(.85f, .88f, 1f), 16f, 1.8f);
            for (var k = 0; k < 9; k++) mk.Blob(WorldMaterials.Glow(new Color(.9f, .95f, 1f), 2f), wc + f.r * new Vector3(-2.1f + (k * 29 % 40) / 10f, 1.7f + (k * 17 % 45) / 10f, -.05f), Vector3.one * .04f, prng, 0f);
            mk.Box(Soft(Cream), wc + f.r * new Vector3(0, 3.7f, .12f), new Vector3(.12f, 5f, .12f), f.r); mk.Box(Soft(Cream), wc + f.r * new Vector3(0, 3.7f, .12f), new Vector3(5f, .12f, .12f), f.r); foreach (var e in new[] { -1f, 1f }) { mk.Box(Soft(Cream), wc + f.r * new Vector3(e * 2.55f, 3.7f, .12f), new Vector3(.2f, 5.3f, .16f), f.r); mk.Box(Soft(Cream), wc + f.r * new Vector3(0, 3.7f + e * 2.55f, .12f), new Vector3(5.3f, .2f, .16f), f.r); }
            // curtains tied back, soft
            foreach (var e in new[] { -1f, 1f }) for (var s = 0; s < 4; s++) soft.Box(Soft(new Color(.95f, .8f, .86f)), wc + f.r * new Vector3(e * (2.3f + s * .28f), 3.9f - s * .1f, .35f), new Vector3(.3f, 5.6f - s * .5f, .12f), f.r * Quaternion.Euler(0, 0, e * (4f + s * 3f)));
            mk.Box(wood, wc + f.r * new Vector3(0, 6.75f, .35f), new Vector3(7f, .1f, .1f), f.r);
            // the window seat: a deep sill with cushions, a blanket, a lamp, a cat
            var seatM = Soft(new Color(.86f, .78f, .9f)); mk.Box(Soft(Cream), wc + f.r * new Vector3(0, .5f, 1.1f), new Vector3(5.2f, 1f, 1.8f), f.r); solid.Box(Soft(Cream), wc + f.r * new Vector3(0, .5f, 1.1f), new Vector3(5.2f, 1f, 1.8f), f.r); mk.Box(seatM, wc + f.r * new Vector3(0, 1.06f, 1.1f), new Vector3(5f, .14f, 1.6f), f.r);
            foreach (var e in new[] { -1f, 1f }) { mk.Blob(Soft(Pastel[(e < 0 ? 1 : 3) % Pastel.Length]), wc + f.r * new Vector3(e * 2.1f, 1.4f, .7f), new Vector3(.4f, .45f, .25f), prng, .1f); Interact.Seat(plife.transform, wc + f.r * new Vector3(e * 1.3f, 1.18f, 1.2f), -f.Right * e, "Curl up on the window seat"); }
            for (var k = 0; k < 2; k++) { var st = wc + f.r * new Vector3(0, .2f + k * .0f, 2.3f + k * .0f); mk.Box(Soft(Cream), st + f.Fwd * (k * .45f) + up * (.3f - k * .3f - .05f), new Vector3(2f, .34f, .45f), f.r); solid.Box(Soft(Cream), st + f.Fwd * (k * .45f) + up * (.3f - k * .3f - .05f), new Vector3(2f, .34f, .45f), f.r); }
            var cat = new MeshKit(); var fur = Soft(new Color(.3f, .27f, .35f)); cat.Blob(fur, new Vector3(0, .16f, 0), new Vector3(.3f, .17f, .2f), prng, .05f); cat.Blob(fur, new Vector3(.26f, .26f, 0), Vector3.one * .12f, prng, 0f); foreach (var e in new[] { -.06f, .06f }) cat.Cone(fur, new Vector3(.27f, .34f, e), .04f, .09f, 4); cat.Blob(fur, new Vector3(-.3f, .1f, .12f), new Vector3(.18f, .04f, .04f), prng, 0f);
            var cgo = cat.BuildSingle("Window cat", plife.transform, false); cgo.transform.SetPositionAndRotation(wc + f.r * new Vector3(0, 1.13f, .6f), f.r); string[] purr = { "The cat allows it.", "A purr like a very small amplifier.", "The cat looks at the moon, then at you, then at the moon." }; var pn = 0;
            Interact.Toy(cgo, Toy.Kind.Custom, "Stroke the cat", new Vector3(.8f, .5f, .5f)).onUse = _ => Say(plife.transform, wc + f.r * new Vector3(0, 2.3f, .8f), purr[pn++ % purr.Length], 4f);
            var lp = G(wc + f.r * new Vector3(-3.6f, 0, 1.4f)); mk.Prism(Soft(Cream), lp, up, .06f, 2.4f, 6); mk.Cone(WorldMaterials.Glow(FairyWarm, 1.5f), lp + up * 2.2f, .55f, .6f, 8); halos.Add(lp + up * 2.3f, 1.4f, FairyWarm, .6f); var lc = new GameObject("Lamp post collider"); lc.transform.SetParent(rootT, false); lc.transform.position = lp + up * 1.2f; var cc = lc.AddComponent<CapsuleCollider>(); cc.height = 2.4f; cc.radius = .1f;
            ArtistBits.PointLight(ctx.Root, lp + up * 2f + f.Fwd * .6f, FairyWarm, 9f, 1.2f);
            Folk(wc + f.r * new Vector3(1.3f, 1.18f, 1.2f), -f.Right, Resident.Pose.Sit, new[] { "I was going to do something today. I looked out of the window instead. No regrets.", "You can watch a whole evening go by from here and not owe it anything.", "That's not the real moon. It's better. It stays." }, Dreamer(Residents.Tool.Mug), null, .5f);
            PinnedNote(mk, f.P(5f, 0, 5f), f.Fwd, "the window seat", Pastel[2 % Pastel.Length], 1.5f);
            Shot(ctx, "the window seat", f.P(-5f, 3.2f, 9f), wc + up * 3.4f);
        }

        // ---------------------------------------------------------------- the Bubble Bath
        void BubbleBath(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var g = ctx.OnGround(BathAt.x, BathAt.y); var f = new PlaceFrame(g, FaceTo(g, Nearest(trackP, g))); const float L = 7f, W = 3.6f, H = 2f;
            var enamel = Soft(new Color(.97f, .96f, .98f)); var gold = WorldMaterials.Solid(new Color(.9f, .75f, .4f), .8f); var tubC = g + up * .7f;
            // the tub: four walls and a floor you can stand in, on four gold feet
            mk.Box(enamel, tubC + up * .1f, new Vector3(L, .2f, W), f.r); solid.Box(enamel, tubC + up * .1f, new Vector3(L, .2f, W), f.r);
            foreach (var e in new[] { -1f, 1f }) { mk.Box(enamel, tubC + f.r * new Vector3(0, H / 2f, e * (W / 2f)), new Vector3(L + .5f, H, .3f), f.r * Quaternion.Euler(e * -6f, 0, 0)); solid.Box(enamel, tubC + f.r * new Vector3(0, H / 2f, e * (W / 2f)), new Vector3(L + .5f, H, .3f), f.r); mk.Box(enamel, tubC + f.r * new Vector3(e * (L / 2f), H / 2f, 0), new Vector3(.3f, H, W), f.r * Quaternion.Euler(0, 0, e * 8f)); solid.Box(enamel, tubC + f.r * new Vector3(e * (L / 2f), H / 2f, 0), new Vector3(.3f, H, W), f.r); }
            mk.Box(enamel, tubC + f.r * new Vector3(0, H + .05f, 0), new Vector3(L + .9f, .12f, .0f), f.r);
            foreach (var x in new[] { -1f, 1f }) foreach (var z in new[] { -1f, 1f }) { var fp = g + f.r * new Vector3(x * (L / 2f - .6f), 0, z * (W / 2f - .3f)); mk.Blob(gold, fp + up * .35f, new Vector3(.35f, .4f, .35f), prng, .1f); var fc = new GameObject("Tub foot"); fc.transform.SetParent(rootT, false); fc.transform.position = fp + up * .35f; fc.AddComponent<SphereCollider>().radius = .38f; }
            mk.Prism(gold, tubC + f.r * new Vector3(-L / 2f - .1f, H, 0), up, .07f, 1f, 6); mk.Prism(gold, tubC + f.r * new Vector3(-L / 2f - .1f, H + 1f, 0), f.Right, .07f, .8f, 6); foreach (var e in new[] { -.5f, .5f }) mk.Blob(gold, tubC + f.r * new Vector3(-L / 2f - .1f, H + .3f, e), Vector3.one * .14f, prng, 0f);
            // bubbles heaped in the tub, a few always drifting up
            var bub = new Material(WorldMaterials.Glow(new Color(.95f, .9f, 1f), .7f)) { name = "Bath bubbles" };
            for (var k = 0; k < 26; k++) { var p = tubC + f.r * new Vector3(-L / 2f + .6f + (k * 37 % 58) / 10f, H * .62f + (k % 4) * .16f, -W / 2f + .5f + (k * 23 % 26) / 10f); soft.Blob(bub, p, Vector3.one * (.38f + (k % 3) * .14f), prng, 0f); }
            for (var k = 0; k < 12; k++) { var bm = new MeshKit(); bm.Blob(bub, Vector3.zero, Vector3.one * (.14f + (k % 3) * .06f), prng, 0f); var go = bm.BuildSingle("Soft drifting bubble", plife.transform, false); var p = tubC + f.r * new Vector3(-L / 2f + .8f + (k * 41 % 54) / 10f, H + .2f, -W / 2f + .6f + (k * 19 % 24) / 10f); go.transform.position = p; plife.bubbles.Add((go.transform, p, k * .77f)); }
            halos.Add(tubC + up * (H + .6f), 4f, new Color(.95f, .88f, 1f), .3f); ArtistBits.PointLight(ctx.Root, tubC + up * (H + 1.4f), new Color(.95f, .88f, 1f), 16f, 1.6f);
            // a stool to climb in by, two places to sit in the bubbles
            for (var k = 0; k < 3; k++) { var sp = g + f.Fwd * (W / 2f + .5f + k * .45f) + f.Right * 1.5f + up * (H + .7f - .5f - k * .55f - .27f); mk.Box(wood, sp, new Vector3(1.6f, .55f, .47f), f.r); solid.Box(wood, sp, new Vector3(1.6f, .55f, .47f), f.r); }
            foreach (var e in new[] { -1.6f, 1.2f }) Interact.Seat(plife.transform, tubC + f.r * new Vector3(e, .75f, 0), f.Right * Mathf.Sign(-e), "Sit in the bubbles");
            // the duck: one giant, bobbing; one small, to carry
            var duck = new MeshKit(); var yel = Soft(new Color(1f, .85f, .25f)); duck.Blob(yel, new Vector3(0, .5f, 0), new Vector3(.75f, .5f, .55f), prng, .04f); duck.Blob(yel, new Vector3(.5f, 1.05f, 0), Vector3.one * .36f, prng, 0f); duck.Box(Soft(new Color(1f, .5f, .2f)), new Vector3(.86f, 1f, 0), new Vector3(.26f, .08f, .22f), Quaternion.identity); duck.Cone(yel, new Vector3(-.7f, .6f, 0), .2f, .35f, 5); foreach (var e in new[] { -.14f, .14f }) duck.Blob(Soft(Ink), new Vector3(.72f, 1.16f, e), Vector3.one * .04f, prng, 0f);
            var dgo = duck.BuildSingle("Soft giant duck", plife.transform, false); var dp = tubC + f.r * new Vector3(2.1f, H * .75f, .5f); dgo.transform.SetPositionAndRotation(dp, f.r * Quaternion.Euler(0, 200f, 0)); plife.duck = dgo.transform; plife.duckAt = dp;
            var sd = new MeshKit(); sd.Blob(yel, new Vector3(0, .09f, 0), new Vector3(.13f, .09f, .1f), prng, 0f); sd.Blob(yel, new Vector3(.09f, .19f, 0), Vector3.one * .065f, prng, 0f); sd.Box(Soft(new Color(1f, .5f, .2f)), new Vector3(.16f, .18f, 0), new Vector3(.05f, .02f, .04f), Quaternion.identity);
            var sgo = sd.BuildSingle("Rubber duck", plife.transform, false); sgo.transform.position = G(f.P(-2f, 0, W / 2f + 2.2f)) + up * .05f; var dc = Interact.Carry(sgo, "the rubber duck", .2f, g.y - 10f); var dbc = dc.GetComponent<BoxCollider>(); if (dbc != null) { dbc.size = new Vector3(.3f, .28f, .22f); dbc.center = new Vector3(0, .13f, 0); }
            // a towel on a rail, a bath mat, someone reading on a stool
            var tr = G(f.P(-4.6f, 0, 2.6f)); foreach (var e in new[] { -.7f, .7f }) mk.Box(gold, tr + f.Right * e + up * .6f, new Vector3(.05f, 1.2f, .05f), f.r); mk.Box(gold, tr + up * 1.2f, new Vector3(1.5f, .05f, .05f), f.r); mk.Box(Soft(Pastel[2 % Pastel.Length]), tr + up * .8f, new Vector3(1.2f, .85f, .1f), f.r); Col(tr + up * .6f, new Vector3(1.5f, 1.2f, .2f), f.r);
            mk.Box(Soft(Pastel[0]), G(f.P(1.5f, 0, W / 2f + 2.6f)) + up * .04f, new Vector3(2f, .05f, 1.2f), f.r);
            var rs = G(f.P(4.6f, 0, 3f)); mk.Prism(wood, rs, up, .28f, .45f, 8); Col(rs + up * .25f, new Vector3(.55f, .5f, .55f), f.r); var gown = Dreamer(Residents.Tool.Mug); gown.top = new Color(.96f, .93f, .9f); gown.bottom = new Color(.96f, .93f, .9f);
            Folk(rs + up * .47f, FaceTo(rs, g), Resident.Pose.Sit, new[] { "I've been in twice. You go pruney and you don't even mind.", "The bubbles never go flat. I've stopped asking.", "Take the duck. It likes an outing." }, gown, null, .47f);
            PinnedNote(mk, f.P(4.5f, 0, W / 2f + 5.5f), f.Fwd, "the bubble bath\n(get in)", Pastel[4 % Pastel.Length], 1.6f);
            Shot(ctx, "the bubble bath", f.P(-6f, 3.6f, W / 2f + 9f), tubC + up * 1.4f);
        }

        // ---------------------------------------------------------------- the Sleepover
        void Sleepover(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var g = ctx.OnGround(SleepAt.x, SleepAt.y); var f = new PlaceFrame(g, FaceTo(g, Nearest(trackP, g)));
            // a big rug, sleeping bags like petals round a torch standing on its end
            mk.Prism(Soft(new Color(.8f, .72f, .88f)), g - up * .02f, up, 6.2f, .1f, 18); mk.Prism(Soft(new Color(.92f, .85f, .94f)), g - up * .0f, up, 4.4f, .1f, 18);
            var torchM = new Material(WorldMaterials.Glow(FairyWarm, 2.2f)) { name = "Sleepover torch" }; mk.Prism(Soft(Ink), g + up * .08f, up, .1f, .5f, 8); mk.Prism(torchM, g + up * .58f, up, .14f, .06f, 8); var beam = new Material(WorldMaterials.Glow(FairyWarm, .18f)) { name = "Torch beam" }; soft.Prism(beam, g + up * .64f, up, .12f, 3.2f, 8, 3.5f, 0f, false);
            plife.torchM = torchM; plife.beamM = beam; plife.torchLight = ArtistBits.PointLight(ctx.Root, g + up * 2.2f, FairyWarm, 16f, 1.8f); var tc = new GameObject("Torch collider"); tc.transform.SetParent(rootT, false); tc.transform.position = g + up * .3f; var tcc = tc.AddComponent<CapsuleCollider>(); tcc.height = .7f; tcc.radius = .18f;
            var tgo = new GameObject("Sleepover torch"); tgo.transform.SetParent(plife.transform, false); tgo.transform.position = g + up * .5f; Interact.Toy(tgo, Toy.Kind.Custom, "Hold the torch under your chin", new Vector3(.8f, 1f, .8f)).onUse = _ => { plife.spooky = !plife.spooky; Say(plife.transform, g + up * 2.2f, plife.spooky ? "\"...and when they looked in the wardrobe... it was just coats.\" Everyone screams anyway." : "Back to the nice light. Somebody exhales.", 5f); };
            string[][] talk =
            {
                new[] { "We said we'd stay up all night. That was four nights ago.", "Truth or dare. Truth: I like this song more than I let on." },
                new[] { "Pass the popcorn. No, don't throw... okay, throw it.", "I'm not tired. I'm just resting my whole self." },
                new[] { "Do the torch thing. Do the voice.", "If you hit me with a pillow I'm allowed to hit you back. It's the law." },
            };
            for (var k = 0; k < 5; k++)
            {
                var d = Quaternion.Euler(0, k * 72f + 20f, 0) * f.Fwd; var bp = G(g + d * 3.4f); var rot = Quaternion.LookRotation(d); var bag = Soft(Pastel[k % Pastel.Length]);
                mk.Blob(bag, bp + up * .16f, new Vector3(.5f, .18f, 1f), prng, .05f); mk.Blob(Soft(Cream), bp - d * 1.05f + up * .16f, new Vector3(.4f, .12f, .28f), prng, .08f); mk.Box(Soft(Cream), bp + up * .3f - d * .3f, new Vector3(.9f, .04f, .14f), rot); Col(bp + up * .15f - d * .1f, new Vector3(1f, .3f, 2.3f), rot);
                if (k < 3) Folk(bp - d * .3f + up * .3f, -d, Resident.Pose.Sit, talk[k], Dreamer(k == 1 ? Residents.Tool.Mug : Residents.Tool.None), null, .3f); else Interact.Seat(plife.transform, bp - d * .3f + up * .34f, -d, "Sit on the sleeping bag");
            }
            // pillows to throw, popcorn to carry, a stack of board games
            for (var k = 0; k < 5; k++) { var pm = new MeshKit(); pm.Blob(Soft(k % 2 == 0 ? Cream : Pastel[(k + 1) % Pastel.Length]), new Vector3(0, .12f, 0), new Vector3(.34f, .12f, .24f), prng, .08f); var go = pm.BuildSingle("Pillow", plife.transform, false); var d = Quaternion.Euler(0, k * 72f + 56f, 0) * f.Fwd; go.transform.SetPositionAndRotation(G(g + d * 2.1f) + up * .08f, Quaternion.Euler(0, k * 50f, 0)); var c = Interact.Carry(go, "a pillow", .25f, g.y - 10f); var bc = c.GetComponent<BoxCollider>(); if (bc != null) { bc.size = new Vector3(.66f, .24f, .46f); bc.center = new Vector3(0, .12f, 0); } }
            for (var k = 0; k < 2; k++) { var pm = new MeshKit(); pm.Prism(Soft(k == 0 ? new Color(.95f, .55f, .6f) : new Color(.6f, .75f, .95f)), Vector3.zero, Vector3.up, .16f, .14f, 10, 1.4f); pm.Blob(WorldMaterials.Glow(new Color(1f, .95f, .7f), .25f), new Vector3(0, .17f, 0), new Vector3(.2f, .08f, .2f), prng, .3f); var go = pm.BuildSingle("Popcorn bowl", plife.transform, false); go.transform.position = G(g + f.Right * (1.1f - k * 2.2f) + f.Fwd * .9f) + up * .1f; Interact.Carry(go, "a bowl of popcorn", .3f, g.y - 10f); }
            var gs = G(g - f.Fwd * 1.3f + f.Right * .6f); for (var k = 0; k < 3; k++) mk.Box(Soft(Pastel[(k * 2) % Pastel.Length]), gs + up * (.16f + k * .08f), new Vector3(.6f - k * .05f, .08f, .4f), Quaternion.Euler(0, k * 17f, 0));
            // fairy lights on four tall stakes round the rug
            var posts = new List<Vector3>(); for (var k = 0; k < 4; k++) { var p = G(g + Quaternion.Euler(0, k * 90f + 45f, 0) * f.Fwd * 6.6f); mk.Box(wood, p + up * 1.5f, new Vector3(.08f, 3f, .08f), Quaternion.identity); var pc = new GameObject("Stake collider"); pc.transform.SetParent(rootT, false); pc.transform.position = p + up * 1.5f; var pcc = pc.AddComponent<CapsuleCollider>(); pcc.height = 3f; pcc.radius = .08f; posts.Add(p + up * 2.95f); }
            for (var k = 0; k < 4; k++) FairyLights(mk, posts[k], posts[(k + 1) % 4], .25f, k % 2 == 0 ? FairyWarm : new Color(1f, .7f, .85f));
            PinnedNote(mk, f.P(5.5f, 0, 8.5f), f.Fwd, "SLEEPOVER\nno sleeping", Pastel[1 % Pastel.Length], 1.6f);
            Shot(ctx, "the sleepover", f.P(-6f, 3.2f, 10f), g + up * .6f);
        }

        // ---------------------------------------------------------------- the Notebook
        void Notebook(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var g = ctx.OnGround(NoteAt.x, NoteAt.y); var f = new PlaceFrame(g, FaceTo(g, Nearest(trackP, g))); const float PW = 7f, PD = 9.5f;
            // open flat on the grass, as big as a room: two cream pages you can walk on, ruled lines, doodles that glow
            var page = Soft(Cream); mk.Box(Soft(new Color(.55f, .5f, .75f)), g + up * .08f, new Vector3(PW * 2f + .8f, .16f, PD + .6f), f.r); solid.Box(page, g + up * .12f, new Vector3(PW * 2f + .8f, .24f, PD + .6f), f.r);
            foreach (var e in new[] { -1f, 1f }) { mk.Box(page, g + f.Right * e * (PW / 2f + .05f) + up * .2f, new Vector3(PW, .1f, PD), f.r); for (var k = 0; k < 9; k++) mk.Box(Soft(new Color(.7f, .78f, .92f)), g + f.r * new Vector3(e * (PW / 2f + .05f), .255f, -PD / 2f + .9f + k * 1f), new Vector3(PW - .6f, .006f, .04f), f.r); }
            for (var k = 0; k < 9; k++) mk.Prism(WorldMaterials.Solid(new Color(.8f, .8f, .85f), .8f), g + f.r * new Vector3(0, .3f, -PD / 2f + .9f + k * 1f) - f.Right * .3f, f.Right, .14f, .6f, 8);
            var pen = WorldMaterials.Glow(new Color(.55f, .5f, 1f), 1.5f); var pink = WorldMaterials.Glow(new Color(1f, .5f, .7f), 1.5f); var star = WorldMaterials.Glow(new Color(1f, .9f, .5f), 1.6f);
            var lp = new PlaceFrame(g + f.Right * -(PW / 2f + .05f) + up * .262f, f.Fwd);   // the left page: doodles
            void Line(PlaceFrame o, Material m, Vector2 a, Vector2 b) { var pa = o.P(a.x, 0, a.y); var pb = o.P(b.x, 0, b.y); mk.Box(m, (pa + pb) / 2f, new Vector3(.07f, .012f, Vector3.Distance(pa, pb) + .05f), Quaternion.LookRotation(pb - pa)); }
            Vector2 V(float x, float y) => new Vector2(x, y);
            Line(lp, pen, V(-2f, -3f), V(0f, -3f)); Line(lp, pen, V(0f, -3f), V(0f, -1.4f)); Line(lp, pen, V(0f, -1.4f), V(-2f, -1.4f)); Line(lp, pen, V(-2f, -1.4f), V(-2f, -3f)); Line(lp, pen, V(-2.3f, -1.4f), V(-1f, -.3f)); Line(lp, pen, V(-1f, -.3f), V(.3f, -1.4f)); Line(lp, pen, V(-1.3f, -3f), V(-1.3f, -2.2f)); Line(lp, pen, V(-1.3f, -2.2f), V(-.8f, -2.2f)); Line(lp, pen, V(-.8f, -2.2f), V(-.8f, -3f));   // a little house
            for (var s = 0; s < 5; s++) { var a0 = s * 144f * Mathf.Deg2Rad; var a1 = (s + 1) * 144f * Mathf.Deg2Rad; Line(lp, star, V(1.6f + Mathf.Sin(a0) * .9f, 2.4f + Mathf.Cos(a0) * .9f), V(1.6f + Mathf.Sin(a1) * .9f, 2.4f + Mathf.Cos(a1) * .9f)); }   // a star
            Line(lp, pink, V(-1.6f, 1.2f), V(-2.3f, 2f)); Line(lp, pink, V(-2.3f, 2f), V(-2f, 2.6f)); Line(lp, pink, V(-2f, 2.6f), V(-1.6f, 2.2f)); Line(lp, pink, V(-1.6f, 2.2f), V(-1.2f, 2.6f)); Line(lp, pink, V(-1.2f, 2.6f), V(-.9f, 2f)); Line(lp, pink, V(-.9f, 2f), V(-1.6f, 1.2f));   // a heart
            for (var s = 0; s < 6; s++) Line(lp, pen, V(.8f + s * .35f, -.6f + Mathf.Sin(s * 1.5f) * .25f), V(1.15f + s * .35f, -.6f + Mathf.Sin((s + 1) * 1.5f) * .25f));   // a squiggle
            halos.Add(lp.P(0, 1f, 0), 3.6f, new Color(.7f, .6f, 1f), .22f);
            ArtistBits.Label(ctx.Root, "things to do today:\n\n1. nothing\n2. listen to that one song again\n3. see 1", g + f.Right * (PW / 2f + .05f) + up * .27f + f.Fwd * .6f, up, .3f, new Color(.25f, .25f, .5f), TextAnchor.MiddleCenter);
            // the pencil lying across the corner: sit on it
            var p0 = G(f.P(PW + 1.8f, 0, -3f)); var pd = (f.Fwd + f.Right * -.25f).normalized; var pr = Quaternion.LookRotation(pd); var yellow = Soft(new Color(1f, .82f, .3f));
            mk.Prism(yellow, p0 + up * .45f, pd, .45f, 7f, 6); mk.Prism(Soft(new Color(.95f, .6f, .7f)), p0 + up * .45f - pd * .9f, pd, .45f, .9f, 6); mk.Prism(WorldMaterials.Solid(new Color(.8f, .8f, .85f), .8f), p0 + up * .45f - pd * .15f, pd, .47f, .25f, 6); mk.Prism(Soft(new Color(.9f, .78f, .6f)), p0 + up * .45f + pd * 7f, pd, .45f, 1.1f, 6, .1f); Col(p0 + up * .45f + pd * 3.4f, new Vector3(.85f, .9f, 9f), pr);
            Interact.Bench(plife.transform, p0 + up * .95f + pd * 1.5f, p0 + up * .95f + pd * 4.5f, -Vector3.Cross(up, pd));
            // a giant mug of cocoa, marshmallows glowing; cushions round it
            var mg = G(f.P(-PW - 2.6f, 0, 2.5f)); var mugM = Soft(Pastel[1 % Pastel.Length]); mk.Prism(mugM, mg, up, 1.5f, 2.4f, 14); solid.Prism(mugM, mg, up, 1.5f, 2.4f, 14); mk.Prism(Soft(new Color(.45f, .3f, .25f)), mg + up * 2.3f, up, 1.35f, .06f, 14); for (var k = 0; k < 5; k++) mk.Blob(WorldMaterials.Glow(new Color(1f, .95f, .95f), .9f), mg + up * 2.42f + new Vector3(Mathf.Cos(k * 1.3f), 0, Mathf.Sin(k * 1.3f)) * .6f, new Vector3(.25f, .16f, .25f), prng, .05f);
            for (var k = 0; k < 5; k++) { var a = (-40f + k * 45f) * Mathf.Deg2Rad; mk.Box(mugM, mg + f.Right * -(1.5f + .35f + Mathf.Sin(k * Mathf.PI / 4f) * .5f) + up * (.6f + k * .3f), new Vector3(.3f, .34f, .3f), f.r); }
            halos.Add(mg + up * 2.8f, 2f, new Color(1f, .9f, .85f), .4f); ArtistBits.PointLight(ctx.Root, mg + up * 3.2f, new Color(1f, .85f, .8f), 10f, 1.2f);
            Cushion(mk, mg + f.Fwd * 2.6f, Pastel[3 % Pastel.Length], -f.Fwd); Cushion(mk, mg + f.Fwd * 2.2f + f.Right * 1.9f, Pastel[0], (-f.Fwd - f.Right).normalized);
            Folk(G(mg + f.Fwd * 2.6f) + up * .42f, f.Fwd, Resident.Pose.Sit, new[] { "I write one line a day. Yesterday's was \"same as before, but nicer\".", "It's cocoa. It's always the right temperature. Don't ask how.", "Press play on the tape. Go on. It's a good one; I made it for someone." }, Dreamer(Residents.Tool.Mug), null, .42f);
            // the tape player: press play and the reels turn
            var tp = G(f.P(-2f, 0, PD / 2f + 2.8f)); var tr = f.r; var bodyM = Soft(new Color(.7f, .85f, .9f)); mk.Box(bodyM, tp + up * .5f, new Vector3(3f, 1f, 2f), tr); solid.Box(bodyM, tp + up * .5f, new Vector3(3f, 1f, 2f), tr); mk.Box(Soft(Ink), tp + up * 1.01f, new Vector3(2.2f, .02f, 1.2f), tr);
            for (var k = 0; k < 2; k++) { var rm = new MeshKit(); rm.Prism(Soft(Cream), Vector3.zero, Vector3.up, .38f, .05f, 12); for (var s = 0; s < 3; s++) rm.Box(Soft(Ink), Quaternion.Euler(0, s * 120f, 0) * new Vector3(.2f, .055f, 0), new Vector3(.2f, .01f, .08f), Quaternion.Euler(0, s * 120f, 0)); var rgo = rm.BuildSingle("Tape reel", plife.transform, false); rgo.transform.position = tp + up * 1.03f + tr * new Vector3(-.55f + k * 1.1f, 0, 0); plife.reels.Add(rgo.transform); }
            var pb = new MeshKit(); var playM = new Material(WorldMaterials.Glow(new Color(1f, .5f, .6f), .6f)) { name = "Play button" }; pb.Box(playM, new Vector3(0, .08f, 0), new Vector3(.5f, .16f, .3f), Quaternion.identity); var pgo = pb.BuildSingle("Play button", plife.transform, false); pgo.transform.SetPositionAndRotation(tp + up * 1f + tr * new Vector3(0, 0, .8f), tr); plife.playM = playM;
            string[] tape = { "Click. A hiss, then the first song, a little too slow, exactly right.", "Click. It stops. The field is very quiet for a second.", "Click. Side B. You don't know this one. You will." }; var tn = 0;
            Interact.Toy(pgo, Toy.Kind.Custom, "Press play", new Vector3(.8f, .6f, .6f)).onUse = _ => { plife.playing = tn % 3 != 1; Say(plife.transform, tp + up * 2.2f, tape[tn++ % tape.Length], 5f); };
            ArtistBits.PointLight(ctx.Root, g + up * 3f, new Color(.8f, .75f, 1f), 18f, 1.3f);
            PinnedNote(mk, f.P(4f, 0, PD / 2f + 4.5f), f.Fwd, "the notebook\n(private. read it)", Pastel[2 % Pastel.Length], 1.7f);
            Shot(ctx, "the notebook", f.P(-7f, 6f, PD / 2f + 9f), g + up * .4f);
        }
    }

    /// The Meadows places' moving parts: the record turning, speakers breathing, the window moon, bath bubbles and
    /// the duck, the sleepover torch, the tape reels.
    public class MeadowsPlacesLife : MonoBehaviour
    {
        public Transform record, duck; public bool fast, spooky, playing; public List<Material> cones = new List<Material>(); public Light speakerLight, moonLight, torchLight;
        public Material moonM, torchM, beamM, playM; public Vector3 duckAt; public List<(Transform t, Vector3 at, float phase)> bubbles = new List<(Transform, Vector3, float)>(); public List<Transform> reels = new List<Transform>();
        static readonly Color Warm = new Color(1f, .78f, .5f), Spook = new Color(.4f, 1f, .5f), Cone = new Color(.85f, .7f, 1f);
        float speed = 12f, spin, mix;

        void Update()
        {
            var t = Time.time; var dt = Time.deltaTime;
            speed = Mathf.MoveTowards(speed, fast ? 22f : 12f, dt * 6f);
            if (record != null) record.Rotate(0, speed * dt, 0, Space.World);
            var beat = .5f + .5f * Mathf.Sin(t * (fast ? 5.4f : 3.6f)); foreach (var m in cones) m.SetColor("_EmissionColor", Cone * (.3f + beat * .8f)); if (speakerLight != null) speakerLight.intensity = 1.2f + beat * .6f;
            if (moonM != null) { var glow = 2.2f + Mathf.Sin(t * .4f) * .3f; moonM.SetColor("_EmissionColor", new Color(1f, .96f, .8f) * glow); }
            foreach (var (tr, at, ph) in bubbles) { var u = Mathf.Repeat(t * .12f + ph, 1f); tr.position = at + Vector3.up * (u * 5f) + new Vector3(Mathf.Sin(t * .7f + ph * 5f), 0, Mathf.Cos(t * .5f + ph * 3f)) * (.3f + u * .8f); tr.localScale = Vector3.one * Mathf.Sin(u * Mathf.PI); }
            if (duck != null) { duck.position = duckAt + Vector3.up * Mathf.Sin(t * 1.1f) * .08f; }
            mix = Mathf.MoveTowards(mix, spooky ? 1f : 0f, dt * 2f); var tc = Color.Lerp(Warm, Spook, mix);
            if (torchM != null) torchM.SetColor("_EmissionColor", tc * 2.2f); if (beamM != null) beamM.SetColor("_EmissionColor", tc * .18f); if (torchLight != null) { torchLight.color = tc; torchLight.intensity = 1.8f - mix * .6f + (spooky ? Mathf.PerlinNoise(t * 9f, 0f) * .5f : 0f); }
            if (playing) spin += 140f * dt; foreach (var r in reels) r.rotation = Quaternion.Euler(0, spin, 0); if (playM != null) playM.SetColor("_EmissionColor", new Color(1f, .5f, .6f) * (playing ? 2.4f : .6f));
        }
    }
}
