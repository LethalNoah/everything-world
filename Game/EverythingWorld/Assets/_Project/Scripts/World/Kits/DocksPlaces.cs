using System.Collections.Generic;
using UnityEngine;
using static EverythingWorld.World.PlaceTools;

namespace EverythingWorld.World
{
    /// The Post-Punk Docklands laid out on purpose (the build-land standard). The grid, the harbour and every corner
    /// stay where they were; three more asphalt streets with sodium lamps leave the grid for the dark north and south
    /// of the land. Along them, with the people of the docks: the Flyover (a concrete road in the air that ends in
    /// broken rebar; stairs up to a bench at the end of it, a strip-lit underpass beneath with a busker and an echo);
    /// the Pressing Plant (records coming down a belt, presses to pull, records to carry off and throw); the Social
    /// (a working men's club: a bar, darts, bingo, a stage with a tinsel curtain); and the Chippy above the south quay.
    public partial class DocksKit
    {
        static readonly Vector2 FlyAt = new Vector2(7492, 9334), PlantAt = new Vector2(7560, 9420), SocialAt = new Vector2(7422, 8798), ChippyAt = new Vector2(7082, 8870);
        static readonly Vector2[] NorthStreet = { new Vector2(7458, 9262), new Vector2(7474, 9296), new Vector2(7492, 9334), new Vector2(7524, 9374), new Vector2(7546, 9400) };
        static readonly Vector2[] SouthStreet = { new Vector2(7430, 8944), new Vector2(7426, 8880), new Vector2(7423, 8824) };
        static readonly Vector2[] QuayStreet = { new Vector2(7186, 8822), new Vector2(7142, 8842), new Vector2(7104, 8860) };
        static readonly (Vector2 at, float r)[] PlaceRings = { (PlantAt, 14f), (SocialAt, 12f), (ChippyAt, 9f) };

        DocksPlacesLife plife; System.Random prng; Transform rootT; LandContext ctx0; readonly List<Vector3> streetP = new List<Vector3>();

        static readonly Color[] Overcoats = { new Color(.2f, .2f, .22f), new Color(.3f, .28f, .26f), new Color(.25f, .3f, .35f), new Color(.4f, .38f, .34f), new Color(.15f, .16f, .2f), new Color(.45f, .2f, .18f) };
        static readonly Color[] Greys = { new Color(.15f, .15f, .17f), new Color(.25f, .25f, .28f), new Color(.2f, .22f, .3f) };
        static readonly Residents.Hat[] FlatCaps = { Residents.Hat.None, Residents.Hat.Cap, Residents.Hat.None, Residents.Hat.Beanie, Residents.Hat.None };
        static readonly string[] DockTalk =
        {
            "Grim, isn't it. I wouldn't live anywhere else.", "They said they'd knock the towers down. That was thirty years ago. The towers outlasted the people who said it.", "The last ship left in '84. That one in the harbour's just never been told.",
            "It doesn't rain here any more. You can tell it wants to.", "There's a band in every one of those blocks. You can feel it through the floor.",
        };

        Residents.Look Docker(Residents.Tool tool = Residents.Tool.None, bool apron = false) { var l = Residents.Random(prng, Overcoats, Greys, FlatCaps, Greys); l.tool = tool; l.apron = apron; return l; }
        Resident Folk(Vector3 feet, Vector3 face, Resident.Pose pose, string[] lines = null, Residents.Look look = null, Vector3[] route = null, float seatH = HumanScale.SeatHeight)
            => Residents.Make(plife.transform, feet, face, look ?? Docker(), pose, prng, lines ?? DockTalk, route, seatH);
        void Col(Vector3 c, Vector3 size, Quaternion rot) => BoxCol(rootT, c, size, rot);
        Vector3 G(Vector3 p) => ctx0.OnGround(p.x, p.z);
        void Ring(Vector3 at, float r) => noGrass.Add(new Vector3(at.x, r, at.z));

        public override void ShapeGround(IslandWorldData data, int index)
        {
            ShapeDocks(data, index);   // everything the land already did, unchanged
            foreach (var (at, r) in PlaceRings) Level(data, index, at, r, r + 14f);
        }

        /// Stencilled letters on a concrete slab. Signs on the +x side of a place's street; the photo spot on the −x side.
        void Stencil(MeshKit mk, Vector3 at, Vector3 face, string text, float w = 3f, float size = .2f)
        {
            var up = Vector3.up; var g = G(at); var rot = Quaternion.LookRotation(face);
            mk.Box(concrete, g + up * .9f, new Vector3(w, 1.8f, .3f), rot); Col(g + up * .9f, new Vector3(w, 1.8f, .3f), rot); mk.Box(yellow, g + up * .12f + face * .16f, new Vector3(w, .12f, .02f), rot);
            ArtistBits.Label(rootT, text, g + up * 1.05f + face * .16f, face, size, Ink, TextAnchor.MiddleCenter);
        }

        void SlabBench(MeshKit mk, MeshKit solid, Vector3 at, Vector3 face, float len = 2.2f)
        {
            var g = G(at); var rot = Quaternion.LookRotation(face); var side = Vector3.Cross(Vector3.up, face);
            mk.Box(concrete, g + Vector3.up * .25f, new Vector3(len, .5f, .6f), rot); solid.Box(concrete, g + Vector3.up * .25f, new Vector3(len, .5f, .6f), rot); Interact.Bench(plife.transform, g + Vector3.up * .56f - side * (len / 2f - .45f), g + Vector3.up * .56f + side * (len / 2f - .45f), face);
        }

        List<Vector3> DockStreet(LandContext ctx, MeshKit mk, Vector2[] way)
        {
            Vector2 near = way[0]; var bd = 160f * 160f; foreach (var (p, _) in walkPoints) { var d = (p.x - way[0].x) * (p.x - way[0].x) + (p.z - way[0].y) * (p.z - way[0].y); if (d < bd) { bd = d; near = new Vector2(p.x, p.z); } }
            var full = new List<Vector2> { near }; full.AddRange(way);
            var pts = PlaceTools.Smooth(ctx, full, false, 2.5f);
            for (var i = 0; i < pts.Count; i++) foreach (var a in ctx.Artists) { var r = Foot(a) + 7f; var v = new Vector2(pts[i].x - a.x, pts[i].z - a.z); var d = v.magnitude; if (d < r) { v = d > .01f ? v / d * r : new Vector2(r, 0); pts[i] = ctx.OnGround(a.x + v.x, a.z + v.y); } }
            if (pts.Count < 2) return pts;
            Strip(mk, pts, asphalt, 5f, .07f, ctx); foreach (var p in pts) Remember(p, 700);
            var up = Vector3.up; var n = 0;
            for (var i = 5; i < pts.Count; i += 13) { var d = pts[i] - pts[i - 1]; d.y = 0; if (d.sqrMagnitude < .01f) continue; d.Normalize(); var sd = Vector3.Cross(up, d) * (n++ % 2 == 0 ? 1f : -1f); var lp = pts[i] + sd * 3.6f; if (ctx.IsLand(lp.x, lp.z, 2f) && !InNoGrass(lp.x, lp.z, 2f) && Vector2.Distance(new Vector2(lp.x, lp.z), FlyAt) > 14f) SodiumLamp(ctx, mk, ctx.OnGround(lp.x, lp.z), -sd, n % 2 == 0); if (n % 2 == 1) Puddle(mk, pts[i] - sd * 1f + up * .09f, 1.2f); }
            streetP.AddRange(pts); return pts;
        }

        void Side(LandContext ctx, MeshKit mk, Vector3 to)
        {
            var from = Nearest(streetP, to); var d = new Vector2(to.x - from.x, to.z - from.z); var len = d.magnitude; if (len < 4f) return; d /= len;
            var a = new Vector2(from.x, from.z) + d * 2.6f; var b = new Vector2(to.x, to.z);
            var pts = PlaceTools.Smooth(ctx, new[] { a, Vector2.Lerp(a, b, .5f), b }, false, 2f, 1f); if (pts.Count < 2) return;
            Strip(mk, pts, asphalt, 3f, .065f, ctx); foreach (var p in pts) Remember(p, 900);
        }

        void DockPlaces(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            streetP.Clear(); rootT = ctx.Root; ctx0 = ctx; prng = new System.Random(1979);
            plife = new GameObject("Docklands places life").AddComponent<DocksPlacesLife>(); plife.transform.SetParent(life.transform, false);
            foreach (var (at, r) in PlaceRings) noGrass.Add(new Vector3(at.x, r, at.y)); noGrass.Add(new Vector3(FlyAt.x, 20f, FlyAt.y));
            var north = DockStreet(ctx, mk, NorthStreet); DockStreet(ctx, mk, SouthStreet); DockStreet(ctx, mk, QuayStreet);
            Flyover(ctx, mk, solid, north); PressingPlant(ctx, mk, solid); Social(ctx, mk, solid); Chippy(ctx, mk, solid);
        }

        // ---------------------------------------------------------------- the Flyover
        void Flyover(LandContext ctx, MeshKit mk, MeshKit solid, List<Vector3> street)
        {
            var up = Vector3.up; var g = Nearest(street, ctx.OnGround(FlyAt.x, FlyAt.y)); var along = Vector3.forward;
            for (var i = 1; i < street.Count; i++) if ((street[i] - g).sqrMagnitude < 9f) { along = street[i] - street[i - 1]; along.y = 0; along.Normalize(); break; }
            var f = new PlaceFrame(g, along); const float L = 36f, W = 9f, H = 6f; var y = g.y + H;   // the deck crosses the street at right angles
            Vector3 D(float x, float z, float h = 0f) => new Vector3(g.x, y + h, g.z) + f.Right * x + f.Fwd * z;
            mk.Box(concrete, D(0, 0, -.4f), new Vector3(L, .8f, W), f.r); solid.Box(concrete, D(0, 0, -.4f), new Vector3(L, .8f, W), f.r); mk.Box(asphalt, D(0, 0, .02f), new Vector3(L, .04f, W - 1.2f), f.r);
            foreach (var e in new[] { -1f, 1f }) { mk.Box(concrete, D(0, e * (W / 2f - .2f), .5f), new Vector3(L, 1f, .4f), f.r); solid.Box(concrete, D(0, e * (W / 2f - .2f), .5f), new Vector3(L, 1f, .4f), f.r); }
            for (var k = 0; k < 9; k++) mk.Box(yellow, D(-L / 2f + 2f + k * 4f, 0, .045f), new Vector3(1.8f, .01f, .14f), f.r);
            // the broken end: jagged slab, rebar sticking out, a warning light; a low wall so nobody walks off
            for (var k = 0; k < 6; k++) mk.Prism(rust, D(L / 2f, -3.2f + k * 1.3f, -.3f + (k % 2) * .25f), (f.Right + up * (.1f - k % 3 * .12f)).normalized, .03f, 1.2f + (k % 3) * .5f, 4);
            mk.Box(concrete, D(L / 2f - .3f, 0, .5f), new Vector3(.4f, 1f, W), f.r); solid.Box(concrete, D(L / 2f - .3f, 0, .7f), new Vector3(.4f, 1.6f, W), f.r); mk.Blob(blinkM, D(L / 2f - .3f, 0, 1.2f), Vector3.one * .16f, prng, 0f);
            mk.Box(concrete, D(-L / 2f + .3f, 0, .5f), new Vector3(.4f, 1f, W), f.r); solid.Box(concrete, D(-L / 2f + .3f, 0, .7f), new Vector3(.4f, 1.6f, W), f.r);
            // piers: pairs of slab columns, clear of the street
            foreach (var x in new[] { -14f, -6f, 6f, 14f }) foreach (var e in new[] { -1f, 1f }) { var foot = G(new Vector3(g.x, 0, g.z) + f.Right * x + f.Fwd * e * 2.8f); var hh = y - .8f - foot.y + .6f; mk.Box(darkConcrete, foot + up * (hh / 2f - .6f), new Vector3(1.2f, hh, 1.6f), f.r); solid.Box(darkConcrete, foot + up * (hh / 2f - .6f), new Vector3(1.2f, hh, 1.6f), f.r); }
            // stairs up at the west end: a straight concrete flight with a landing, then onto the deck
            const int steps = 20; var rise = H / steps; var s0 = new Vector3(g.x, g.y, g.z) - f.Right * (L / 2f - 3f) + f.Fwd * (W / 2f + 1.2f + steps * .34f);
            for (var k = 0; k < steps; k++) { var c = s0 - f.Fwd * (k * .34f) + up * (rise * (k + .5f)); mk.Box(concrete, c, new Vector3(1.8f, rise, .36f), f.r); solid.Box(concrete, c - up * (rise * k / 2f), new Vector3(1.8f, rise * (k + 1), .36f), f.r); }
            var land = s0 - f.Fwd * (steps * .34f + .5f) + up * (H - .2f); mk.Box(concrete, land, new Vector3(1.8f, .4f, 1.3f), f.r); solid.Box(concrete, land, new Vector3(1.8f, .4f, 1.3f), f.r);
            foreach (var e in new[] { -1f, 1f }) { mk.Box(steel, s0 - f.Fwd * (steps * .17f) + f.Right * e * .95f + up * (H / 2f + 1f), new Vector3(.05f, .05f, steps * .34f * 1.33f), Quaternion.LookRotation(-f.Fwd * (steps * .34f) + up * H)); }
            // up top: a bench at the broken end looking out over the harbour, a lamp that still works
            var bp = D(L / 2f - 2.4f, 0, .04f); mk.Box(concrete, bp + up * .25f, new Vector3(.6f, .5f, 2.4f), f.r); solid.Box(concrete, bp + up * .25f, new Vector3(.6f, .5f, 2.4f), f.r); Interact.Bench(plife.transform, bp + up * .56f - f.Fwd * .7f, bp + up * .56f + f.Fwd * .7f, f.Right);
            var lampAt = D(L / 2f - 6f, -W / 2f + .9f, 0); mk.Prism(darkConcrete, lampAt, up, .12f, 5f, 6); mk.Box(sodium, lampAt + up * 4.9f + f.Fwd * .8f, new Vector3(.3f, .06f, .7f), f.r); mk.Box(darkConcrete, lampAt + up * 5f + f.Fwd * .4f, new Vector3(.1f, .1f, 1f), f.r); lampHalos.Add(lampAt + up * 4.7f + f.Fwd * .8f, 2.6f, Sodium, .7f); life.lamps.Add(lampAt + up * 4.9f + f.Fwd * .8f); ArtistBits.PointLight(ctx.Root, lampAt + up * 4f + f.Fwd * .8f, Sodium, 14f, 1.5f); ArtistBits.PostCollider(ctx.Root, lampAt, 5f, .14f);
            Folk(D(L / 2f - 5f, 1.5f, .04f), f.Right, Resident.Pose.Stand, new[] { "They were going to take it all the way to the motorway. Then the money stopped, mid-air.", "Best view in the docks. You can see every lit window from here. I count them. Fourteen tonight.", "I come up to think. It's a road that goes nowhere. It's very good for that." }, Docker());
            // underneath: cold strip lights, a mural on two piers, a busker whose guitar case is open, the echo
            for (var k = 0; k < 4; k++) { var p = D(-9f + k * 6f, 0, -.84f); var sm = new Material(WorldMaterials.Glow(Cold, 2f)) { name = "Underpass strip" }; mk.Box(sm, p, new Vector3(2.4f, .06f, .2f), f.r); plife.strips.Add((sm, k * 1.7f)); if (k % 2 == 0) ArtistBits.PointLight(ctx.Root, p - up * 1.4f, Cold, 13f, 1.6f); }
            Color[] mural = { new Color(.85f, .2f, .2f), new Color(.95f, .8f, .2f), new Color(.2f, .4f, .8f), new Color(.9f, .9f, .9f), new Color(.1f, .1f, .1f) };
            foreach (var x in new[] { -6f, 6f }) { var foot = G(new Vector3(g.x, 0, g.z) + f.Right * x - f.Fwd * 2.8f); for (var k = 0; k < 9; k++) mk.Box(WorldMaterials.Solid(mural[(k + (x > 0 ? 2 : 0)) % mural.Length]), foot + up * (.6f + (k / 3) * 1.1f) - f.Right * Mathf.Sign(x) * .61f + f.Fwd * (-.5f + (k % 3) * .5f), new Vector3(.02f, 1f, .45f), f.r); }
            var bk = G(new Vector3(g.x, 0, g.z) + f.Right * 9.5f); var bl = Docker(Residents.Tool.Rod); Folk(bk, -f.Right, Resident.Pose.Work, new[] { "It's the acoustics. You play one note down here and it comes back with friends.", "Twelve pence so far tonight. And a button. I like the button.", "Clap. Go on. Everyone does. It never gets old." }, bl).workRate = 1.1f;
            mk.Box(black, bk - f.Right * 1.2f + up * .08f, new Vector3(.5f, .16f, 1.1f), f.r); mk.Box(WorldMaterials.Solid(new Color(.5f, .12f, .12f)), bk - f.Right * 1.2f + up * .17f, new Vector3(.4f, .02f, 1f), f.r); for (var k = 0; k < 4; k++) mk.Prism(yellow, bk - f.Right * 1.2f + up * .19f + f.Fwd * (-.3f + k * .2f), up, .03f, .01f, 6);
            var cgo = new GameObject("Underpass echo"); cgo.transform.SetParent(plife.transform, false); cgo.transform.position = new Vector3(g.x, g.y + 1.5f, g.z) - f.Right * 3.5f; string[] echo = { "CLAP. clap. clap.  clap.   clap.", "HELLO. hello. ello.  lo.", "Your footsteps go on walking a moment after you stop." }; var en = 0;
            Interact.Toy(cgo, Toy.Kind.Custom, "Clap", new Vector3(3f, 2.4f, 4f)).onUse = _ => { plife.clap = Time.time; Say(plife.transform, cgo.transform.position + up * 1.2f, echo[en++ % echo.Length], 4.5f); };
            foreach (var x in new[] { -10f, 10f }) ArtistBits.PointLight(ctx.Root, D(x, 0, -2.6f), Sodium, 16f, 2.2f);   // so the piers and the mural read
            Stencil(mk, new Vector3(g.x, 0, g.z) + f.Right * 4.5f + f.Fwd * 9f, f.Fwd, "THE FLYOVER\nno through road", 3.4f, .18f);
            Shot(ctx, "the flyover", new Vector3(g.x, g.y + 3f, g.z) - f.Right * 12f + f.Fwd * 22f, D(2f, 0, -1f));
            Shot(ctx, "on top of the flyover", D(-L / 2f + 4f, 0, 1.8f), D(L / 2f, 0, 1.2f));
        }

        // ---------------------------------------------------------------- the Pressing Plant
        void PressingPlant(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var g = ctx.OnGround(PlantAt.x, PlantAt.y); var f = new PlaceFrame(g, FaceTo(g, Nearest(streetP, g))); const float W = 16f, D = 11f, H = 5.5f, T = .4f, Door = 4f;
            var fo = new PlaceFrame(g + up * .25f, f.Fwd);
            mk.Box(darkConcrete, g - up * .35f, new Vector3(W + .8f, 1.2f, D + .8f), f.r);
            void Wall(Vector3 p, Vector3 s) { mk.Box(brick, p, s, f.r); solid.Box(brick, p, s, f.r); }
            mk.Box(concrete, fo.P(0, -.03f, 0), new Vector3(W - T, .1f, D - T), f.r); solid.Box(concrete, fo.P(0, -.15f, 0), new Vector3(W, .3f, D), f.r);
            Wall(fo.P(0, H / 2f, -D / 2f + T / 2f), new Vector3(W, H, T)); foreach (var e in new[] { -1f, 1f }) Wall(fo.P(e * (W / 2f - T / 2f), H / 2f, 0), new Vector3(T, H, D - T * 2f));
            var seg = (W - Door) / 2f; foreach (var e in new[] { -1f, 1f }) Wall(fo.P(e * (Door / 2f + seg / 2f), H / 2f, D / 2f - T / 2f), new Vector3(seg, H, T)); Wall(fo.P(0, H - .9f, D / 2f - T / 2f), new Vector3(Door, 1.8f, T));
            for (var s = 0; s < 4; s++) { var z = -D / 2f + (s + .5f) * D / 4f; mk.Box(darkConcrete, fo.P(0, H + .3f + (s % 2) * .0f, z), new Vector3(W + .4f, .2f, D / 4f + .1f), f.r * Quaternion.Euler(s % 2 == 0 ? 12f : -12f, 0, 0)); } solid.Box(darkConcrete, fo.P(0, H + .2f, 0), new Vector3(W, .4f, D), f.r);
            mk.Prism(darkBrick, fo.P(W / 2f - 1.6f, H, -D / 2f + 1.6f), up, .8f, 7f, 8, .8f); mk.Blob(blinkM, fo.P(W / 2f - 1.6f, H + 7.2f, -D / 2f + 1.6f), Vector3.one * .18f, prng, 0f);
            IndoorSpaces.Add(fo.P(0, H / 2f, 0), f.r, new Vector3(W - .9f, H, D - .9f));
            ArtistBits.Label(ctx.Root, "THE PRESSING PLANT", fo.P(0, H - .9f, D / 2f + .03f), f.Fwd, .34f, Ink, TextAnchor.MiddleCenter); foreach (var e in new[] { -1f, 1f }) { mk.Box(GlowM(Sodium, 1.4f), fo.P(e * (Door / 2f + seg / 2f), 3.4f, D / 2f + .02f), new Vector3(seg - 1.6f, 1.2f, .05f), f.r); mk.Box(steel, fo.P(e * (Door / 2f + seg / 2f), 3.4f, D / 2f + .05f), new Vector3(.08f, 1.2f, .04f), f.r); }
            // two presses along the back: pull the lever, the platen comes down, a record glows
            for (var k = 0; k < 2; k++)
            {
                var pc = fo.P(-4.5f + k * 4.2f, 0, -D / 2f + T + 1.2f); mk.Box(steel, pc + up * .5f, new Vector3(1.8f, 1f, 1.4f), f.r); foreach (var e in new[] { -.8f, .8f }) mk.Box(steel, pc + up * 1.7f + f.Right * e, new Vector3(.16f, 1.4f, .16f), f.r); mk.Box(steel, pc + up * 2.5f, new Vector3(1.9f, .3f, 1.4f), f.r); Col(pc + up * 1.3f, new Vector3(1.9f, 2.6f, 1.4f), f.r);
                var rm = new Material(WorldMaterials.Glow(new Color(1f, .5f, .15f), .2f)) { name = "Hot record" }; mk.Prism(rm, pc + up * 1.02f, up, .45f, .03f, 16);
                var pl = new MeshKit(); pl.Box(darkConcrete, Vector3.zero, new Vector3(1.4f, .3f, 1.2f), Quaternion.identity); pl.Prism(steel, new Vector3(0, .15f, 0), Vector3.up, .12f, .9f, 8); var pgo = pl.BuildSingle("Press platen", plife.transform, false); pgo.transform.SetPositionAndRotation(pc + up * 2.05f, f.r);
                var lv = new MeshKit(); lv.Box(steel, new Vector3(0, .35f, 0), new Vector3(.06f, .7f, .06f), Quaternion.identity); lv.Blob(WorldMaterials.Solid(new Color(.8f, .15f, .12f)), new Vector3(0, .75f, 0), Vector3.one * .09f, prng, 0f); var lgo = lv.BuildSingle("Press lever", plife.transform, false); lgo.transform.SetPositionAndRotation(pc + up * 1f + f.Right * 1.2f + f.Fwd * .5f, f.r * Quaternion.Euler(-25f, 0, 0));
                var idx = plife.presses.Count; plife.presses.Add((pgo.transform, pc + up * 2.05f, rm, -99f)); var lt = Interact.Toy(lgo, Toy.Kind.Swing, "Pull the press", new Vector3(.6f, 1.2f, .8f)); lt.axis = Vector3.right; lt.amount = 50f; lt.seconds = .5f; lt.onUse = _ => { plife.Press(idx); Say(plife.transform, pc + up * 3.4f, "Hsssss. THUNK. One more record in the world.", 3.5f); };
            }
            // the belt down the middle: records riding along it toward the door, sleeves at the end, crates of records to carry
            var beltA = fo.P(4.6f, .95f, -D / 2f + T + 1f); var beltB = fo.P(4.6f, .95f, 3f); mk.Box(black, (beltA + beltB) / 2f, new Vector3(1f, .1f, Vector3.Distance(beltA, beltB) + .6f), f.r); foreach (var t in new[] { 0f, .5f, 1f }) { var lp = Vector3.Lerp(beltA, beltB, t) - up * .5f; mk.Box(steel, lp, new Vector3(.9f, .9f, .12f), f.r); } Col((beltA + beltB) / 2f - up * .45f, new Vector3(1f, 1f, Vector3.Distance(beltA, beltB) + .6f), f.r);
            for (var k = 0; k < 6; k++) { var rk = new MeshKit(); rk.Prism(black, Vector3.zero, Vector3.up, .3f, .015f, 14); rk.Prism(WorldMaterials.Solid(k % 2 == 0 ? new Color(.85f, .2f, .2f) : new Color(.95f, .8f, .2f)), new Vector3(0, .016f, 0), Vector3.up, .1f, .003f, 10); var go = rk.BuildSingle("Soft belt record", plife.transform, false); plife.belt.Add((go.transform, k / 6f)); }
            plife.beltA = beltA + up * .06f; plife.beltB = beltB + up * .06f;
            var crate = fo.P(5f, 0, 4.4f); mk.Box(WorldMaterials.Solid(new Color(.45f, .36f, .26f)), crate + up * .25f, new Vector3(1.2f, .5f, .8f), f.r); Col(crate + up * .25f, new Vector3(1.2f, .5f, .8f), f.r);
            for (var k = 0; k < 5; k++) { var rk = new MeshKit(); rk.Prism(black, Vector3.zero, Vector3.up, .3f, .015f, 14); rk.Prism(WorldMaterials.Solid(new[] { new Color(.85f, .2f, .2f), new Color(.95f, .8f, .2f), new Color(.2f, .4f, .8f), new Color(.9f, .9f, .9f), new Color(.3f, .7f, .4f) }[k]), new Vector3(0, .016f, 0), Vector3.up, .1f, .003f, 10); var go = rk.BuildSingle("Record", plife.transform, false); go.transform.position = crate + up * (.52f + k * .025f) + f.Right * ((k % 2) * .1f - .05f); var c = Interact.Carry(go, "a record, still warm", .25f, g.y - 10f); var bc = c.GetComponent<BoxCollider>(); if (bc != null) { bc.size = new Vector3(.6f, .03f, .6f); bc.center = new Vector3(0, .01f, 0); } }
            Folk(fo.P(1.2f, 0, .5f), f.Fwd, Resident.Pose.Stand, new[] { "Five hundred copies. That's all anyone ever presses. Four hundred are under beds.", "Take one. Nobody's bought one since Thursday and it's not for want of quality.", "Pull the lever. Mind your fingers. I've got eight and I started with ten." }, Docker(Residents.Tool.Mug, true));
            Folk(fo.P(-6.5f, 0, 2.4f), f.Right, Resident.Pose.Work, new[] { "I do the sleeves. Black on black. You can't read it. That's the design.", "Catalogue number goes on the back. This one's also the catalogue number of the building." }, Docker()).workRate = .6f;
            mk.Box(WorldMaterials.Solid(new Color(.45f, .36f, .26f)), fo.P(-5.6f, .45f, 2.4f), new Vector3(.9f, .9f, 2f), f.r); Col(fo.P(-5.6f, .45f, 2.4f), new Vector3(.9f, .9f, 2f), f.r); for (var k = 0; k < 6; k++) mk.Box(k % 3 == 0 ? WorldMaterials.Solid(new Color(.85f, .82f, .75f)) : black, fo.P(-5.6f, .92f + k * .012f, 1.8f + (k % 3) * .45f), new Vector3(.62f, .01f, .62f), f.r * Quaternion.Euler(0, k * 7f, 0));
            ArtistBits.PointLight(ctx.Root, fo.P(-2.5f, 4.2f, 0), Sodium, 18f, 3.8f, true); ArtistBits.PointLight(ctx.Root, fo.P(4f, 4.2f, 1f), Cold, 16f, 3f, true);
            SodiumLamp(ctx, mk, G(f.P(-Door / 2f - 3f, 0, D / 2f + 2.2f)), f.Right, true); SlabBench(mk, solid, f.P(5f, 0, D / 2f + 1.6f), f.Fwd);
            Side(ctx, mk, f.P(0, 0, D / 2f + 1.5f)); Ring(g, 13f);
            Shot(ctx, "the pressing plant", f.P(-9f, 3f, D / 2f + 15f), fo.P(0, 3f, 0));
            Shot(ctx, "inside the pressing plant", fo.P(-1f, 1.7f, D / 2f - 1.2f), fo.P(1f, 1.2f, -D / 2f + 1.5f));
        }

        // ---------------------------------------------------------------- the Social
        void Social(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var g = ctx.OnGround(SocialAt.x, SocialAt.y); var f = new PlaceFrame(g, FaceTo(g, Nearest(streetP, g))); const float W = 12f, D = 9f, H = 3.4f, T = .3f, Door = HumanScale.DoorWidth + .3f;
            var fo = new PlaceFrame(g + up * .25f, f.Fwd); var carpet = WorldMaterials.Solid(new Color(.35f, .12f, .14f)); var woodM = WorldMaterials.Solid(new Color(.3f, .2f, .14f));
            mk.Box(darkConcrete, g - up * .35f, new Vector3(W + .6f, 1.2f, D + .6f), f.r);
            void Wall(Vector3 p, Vector3 s) { mk.Box(darkBrick, p, s, f.r); solid.Box(darkBrick, p, s, f.r); }
            mk.Box(carpet, fo.P(0, -.03f, 0), new Vector3(W - T, .1f, D - T), f.r); solid.Box(carpet, fo.P(0, -.15f, 0), new Vector3(W, .3f, D), f.r);
            Wall(fo.P(0, H / 2f, -D / 2f + T / 2f), new Vector3(W, H, T)); foreach (var e in new[] { -1f, 1f }) Wall(fo.P(e * (W / 2f - T / 2f), H / 2f, 0), new Vector3(T, H, D - T * 2f));
            var seg = (W - Door) / 2f; foreach (var e in new[] { -1f, 1f }) Wall(fo.P(e * (Door / 2f + seg / 2f), H / 2f, D / 2f - T / 2f), new Vector3(seg, H, T)); Wall(fo.P(0, H - .35f, D / 2f - T / 2f), new Vector3(Door, .7f, T));
            mk.Box(darkConcrete, fo.P(0, H + .1f, 0), new Vector3(W + .5f, .2f, D + .5f), f.r); solid.Box(darkConcrete, fo.P(0, H + .1f, 0), new Vector3(W + .5f, .2f, D + .5f), f.r);
            IndoorSpaces.Add(fo.P(0, H / 2f, 0), f.r, new Vector3(W - .7f, H, D - .7f));
            var red = new Color(1f, .2f, .15f); mk.Box(black, fo.P(0, H + .8f, D / 2f + .05f), new Vector3(5f, 1.1f, .12f), f.r); ArtistBits.Label(ctx.Root, "THE SOCIAL", fo.P(0, H + .8f, D / 2f + .13f), f.Fwd, .5f, red, TextAnchor.MiddleCenter); halos.Add(fo.P(0, H + .8f, D / 2f + .6f), 2.4f, red, .5f); foreach (var e in new[] { -1f, 1f }) mk.Box(GlowM(Window, 1.2f), fo.P(e * (Door / 2f + seg / 2f), 2f, D / 2f + .02f), new Vector3(1.6f, .8f, .05f), f.r);
            // the stage at the back: a tinsel curtain, a mic, a bingo machine
            var st = fo.P(-2.5f, 0, -D / 2f + T + 1.2f); mk.Box(woodM, st + up * .25f, new Vector3(5f, .5f, 2.2f), f.r); solid.Box(woodM, st + up * .25f, new Vector3(5f, .5f, 2.2f), f.r);
            for (var k = 0; k < 20; k++) { var tm = new Material(WorldMaterials.Glow(k % 3 == 0 ? new Color(.9f, .95f, 1f) : k % 3 == 1 ? new Color(1f, .8f, .4f) : new Color(1f, .5f, .6f), .6f)) { name = "Tinsel" }; mk.Box(tm, st + up * 1.8f - f.Fwd * .95f + f.Right * (-2.4f + k * .25f), new Vector3(.2f, 2.6f, .02f), f.r); plife.tinsel.Add((tm, k * .9f)); }
            var mp = st + up * .5f + f.Fwd * .3f; mk.Prism(steel, mp, up, .025f, 1.4f, 5); mk.Prism(steel, mp, up, .2f, .03f, 8); mk.Blob(dark, mp + up * 1.45f, Vector3.one * .07f, prng, 0f);
            var mgo = new GameObject("Club microphone"); mgo.transform.SetParent(plife.transform, false); mgo.transform.position = mp + up * 1.3f + f.Fwd * .4f; string[] mic = { "\"One-two. One-two.\" The room does not look up.", "\"...and this next one's a slow one.\" Someone at the back says \"they're all slow ones\".", "Feedback. A long, beautiful note. It may be the best thing played here this year." }; var mn = 0;
            Interact.Toy(mgo, Toy.Kind.Custom, "Speak into the mic", new Vector3(.8f, 1.2f, .8f)).onUse = _ => Say(plife.transform, mp + up * 2.4f, mic[mn++ % mic.Length], 5f);
            var bg = st + up * .5f + f.Right * 1.8f; mk.Box(steel, bg + up * .5f, new Vector3(.7f, 1f, .6f), f.r); var cage = new Material(WorldMaterials.Glow(new Color(1f, .9f, .5f), .8f)) { name = "Bingo cage" }; mk.Blob(cage, bg + up * 1.3f, Vector3.one * .32f, prng, 0f); plife.bingoM = cage;
            var bgo = new GameObject("Bingo machine"); bgo.transform.SetParent(plife.transform, false); bgo.transform.position = bg + up * 1.1f + f.Fwd * .5f; string[] calls = { "\"Two fat ladies: eighty-eight.\"", "\"Legs eleven.\" Somebody whistles, out of duty.", "\"Unlucky for some: thirteen.\"", "\"Top of the shop: ninety.\" Nobody has ninety. Nobody ever has ninety.", "\"Was she worth it? Seven and six: seventy-six.\"" }; var cn = 0;
            Interact.Toy(bgo, Toy.Kind.Custom, "Call a bingo number", new Vector3(.9f, 1.2f, .9f)).onUse = _ => { plife.bingo = Time.time; Say(plife.transform, bg + up * 2.4f, calls[cn++ % calls.Length], 4.5f); };
            // the bar down the right wall, stools, the steward; the dartboard on the left wall with darts to throw
            var bar = new PlaceFrame(fo.P(W / 2f - T - 1.3f, 0, .5f), -f.Right); mk.Box(woodM, bar.P(0, 1.05f, 0), new Vector3(5f, .1f, .7f), bar.r); mk.Box(darkBrick, bar.P(0, .5f, 0), new Vector3(4.8f, 1f, .5f), bar.r); Col(bar.P(0, .55f, 0), new Vector3(5f, 1.1f, .7f), bar.r); for (var k = 0; k < 3; k++) mk.Prism(GlowM(new Color(1f, .75f, .3f), 1.2f), bar.P(-1.5f + k * 1.5f, 1.1f, 0), up, .07f, .3f, 6);
            for (var k = 0; k < 3; k++) { var sp = bar.P(-1.6f + k * 1.6f, 0, 1f); mk.Prism(steel, sp, up, .05f, .7f, 5); mk.Prism(WorldMaterials.Solid(new Color(.5f, .1f, .1f)), sp + up * .7f, up, .22f, .08f, 10); ArtistBits.PostCollider(ctx.Root, sp, .75f, .18f); if (k != 1) Interact.Seat(plife.transform, sp + up * .8f, -bar.Fwd, "Sit at the bar"); else Folk(sp + up * .78f, -bar.Fwd, Resident.Pose.Sit, new[] { "Same stool since the strike. I've worn it to my shape. It's the only thing that has.", "Turn's on at nine. He does a Tom Jones and then he cries. It's a good night." }, Docker(Residents.Tool.Mug), null, .78f); }
            Folk(bar.P(0, 0, -.9f), bar.Fwd, Resident.Pose.Work, new[] { "Members only. Sign the book. The book's been full since '81. Go on in.", "Mild or bitter. Those are the drinks and also the moods.", "Darts are on the oche. Don't hit the plaque. That's the founder." }, Docker(Residents.Tool.Mug, true)).workRate = .4f;
            var db = fo.P(-W / 2f + T + .06f, 1.73f, 1.5f); mk.Prism(black, db, f.Right, .34f, .06f, 16); mk.Prism(WorldMaterials.Solid(new Color(.85f, .82f, .7f)), db + f.Right * .061f, f.Right, .26f, .005f, 16); mk.Prism(GlowM(new Color(1f, .2f, .15f), 1.4f), db + f.Right * .067f, f.Right, .12f, .005f, 12); mk.Prism(GlowM(new Color(.3f, .9f, .4f), 1.6f), db + f.Right * .073f, f.Right, .035f, .005f, 8); mk.Box(GlowM(Window, 1.6f), db + up * .6f + f.Right * .3f, new Vector3(.3f, .08f, .5f), f.r); Col(db, new Vector3(.2f, .9f, .9f), f.r);
            mk.Box(yellow, fo.P(-W / 2f + T + 2.4f, .03f, 1.5f), new Vector3(.06f, .01f, 1f), f.r);
            var shelf = fo.P(-W / 2f + T + 2.6f, 0, 2.4f); mk.Prism(woodM, shelf, up, .3f, .9f, 8); ArtistBits.PostCollider(ctx.Root, shelf, .9f, .3f);
            for (var k = 0; k < 3; k++) { var dm = new MeshKit(); dm.Prism(steel, new Vector3(0, 0, -.07f), Vector3.forward, .008f, .14f, 4); dm.Box(WorldMaterials.Solid(k == 0 ? new Color(.85f, .2f, .2f) : k == 1 ? new Color(.2f, .4f, .8f) : new Color(.9f, .8f, .2f)), new Vector3(0, 0, -.09f), new Vector3(.05f, .05f, .04f), Quaternion.Euler(0, 0, 45f)); var go = dm.BuildSingle("Dart", plife.transform, false); go.transform.SetPositionAndRotation(shelf + up * .95f + f.Right * (-.1f + k * .1f), Quaternion.LookRotation(-f.Right)); var c = Interact.Carry(go, "a dart", .1f, g.y - 10f); var bc = c.GetComponent<BoxCollider>(); if (bc != null) { bc.size = new Vector3(.06f, .06f, .2f); bc.center = Vector3.zero; } }
            // two tables with chairs; a couple who have run out of things to say and are fine with it
            for (var t = 0; t < 2; t++)
            {
                var tc = fo.P(-1.2f + t * 2.6f, 0, 1.8f + t * .6f); mk.Prism(steel, tc, up, .05f, .72f, 5); mk.Prism(woodM, tc + up * .72f, up, .5f, .05f, 10); ArtistBits.PostCollider(ctx.Root, tc, .78f, .4f);
                foreach (var e in new[] { -1f, 1f }) { var cp = tc + f.Right * e * .95f; var cq = Quaternion.LookRotation(-f.Right * e); mk.Box(woodM, cp + up * .45f, new Vector3(.42f, .05f, .42f), cq); mk.Box(woodM, cp + up * .75f + f.Right * e * .2f, new Vector3(.05f, .55f, .42f), f.r); mk.Box(woodM, cp + up * .22f, new Vector3(.36f, .45f, .36f), cq); Col(cp + up * .45f, new Vector3(.45f, .9f, .45f), cq); if (t == 0) Folk(cp + up * .5f, -f.Right * e, Resident.Pose.Sit, e < 0 ? new[] { "Forty years we've had this table.", "He's thinking. Leave him. He'll have finished by closing." } : new[] { "...", "Aye." }, Docker(e < 0 ? Residents.Tool.Mug : Residents.Tool.None), null, .5f); else Interact.Seat(plife.transform, cp + up * .52f, -f.Right * e, "Sit at the table"); }
            }
            ArtistBits.PointLight(ctx.Root, fo.P(0, 2.7f, 0), Window, 14f, 3.4f, true); ArtistBits.PointLight(ctx.Root, st + up * 2.6f + f.Fwd * 1f, new Color(1f, .5f, .6f), 9f, 1.6f, true);
            SodiumLamp(ctx, mk, G(f.P(-Door / 2f - 3f, 0, D / 2f + 2f)), f.Right, true);
            Side(ctx, mk, f.P(0, 0, D / 2f + 1.5f)); Ring(g, 11f);
            Shot(ctx, "the social", f.P(-8f, 2.6f, D / 2f + 12f), fo.P(0, 2.4f, 0));
            Shot(ctx, "inside the social", fo.P(-1f, 1.6f, D / 2f - 1.1f), fo.P(-1.5f, 1.3f, -D / 2f + 1.5f));
        }

        // ---------------------------------------------------------------- the Chippy
        void Chippy(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var g = ctx.OnGround(ChippyAt.x, ChippyAt.y); var f = new PlaceFrame(g, FaceTo(g, Nearest(streetP, g))); var tile = WorldMaterials.Solid(new Color(.85f, .86f, .84f)); var warm = new Color(1f, .85f, .55f);
            mk.Box(darkConcrete, g - up * .38f, new Vector3(10f, 1f, 9f), f.r); solid.Box(darkConcrete, g - up * .38f, new Vector3(10f, 1f, 9f), f.r); var fo = new PlaceFrame(g + up * .12f, f.Fwd);
            // a small tiled shop, open to the street: counter, fryer glowing behind, a neon fish over the door
            mk.Box(tile, fo.P(0, 1.6f, -3f), new Vector3(7f, 3.2f, .3f), f.r); solid.Box(tile, fo.P(0, 1.6f, -3f), new Vector3(7f, 3.2f, .3f), f.r); foreach (var e in new[] { -1f, 1f }) { mk.Box(brick, fo.P(e * 3.35f, 1.6f, -1.2f), new Vector3(.3f, 3.2f, 3.4f), f.r); solid.Box(brick, fo.P(e * 3.35f, 1.6f, -1.2f), new Vector3(.3f, 3.2f, 3.4f), f.r); }
            mk.Box(darkConcrete, fo.P(0, 3.3f, -1.2f), new Vector3(7.4f, .2f, 4f), f.r); mk.Box(black, fo.P(0, 3.8f, .7f), new Vector3(5.4f, .9f, .12f), f.r); ArtistBits.Label(ctx.Root, "FISH & CHIPS", fo.P(1f, 3.8f, .78f), f.Fwd, .36f, Cold, TextAnchor.MiddleCenter); halos.Add(fo.P(0, 3.8f, 1.2f), 2.4f, Cold, .5f);
            var fish = GlowM(new Color(1f, .5f, .3f), 2.4f); var fc = fo.P(-1.9f, 3.8f, .78f); mk.Blob(fish, fc, new Vector3(.36f, .16f, .03f), prng, 0f); mk.Cone(fish, fc - f.Right * .34f, .16f, -.24f, 3); mk.Blob(black, fc + f.Right * .2f + up * .04f + f.Fwd * .03f, Vector3.one * .03f, prng, 0f);
            mk.Box(steel, fo.P(0, 1.05f, -.3f), new Vector3(6f, .1f, .8f), f.r); mk.Box(tile, fo.P(0, .5f, -.3f), new Vector3(5.9f, 1f, .7f), f.r); Col(fo.P(0, .55f, -.3f), new Vector3(6f, 1.1f, .8f), f.r); mk.Box(GlowM(warm, .6f), fo.P(1.4f, 1.4f, -.3f), new Vector3(2.2f, .6f, .6f), f.r);
            mk.Box(steel, fo.P(-.5f, .9f, -2.3f), new Vector3(4f, .9f, .9f), f.r); mk.Box(GlowM(new Color(1f, .7f, .25f), 1.6f), fo.P(-.5f, 1.37f, -2.3f), new Vector3(3.6f, .04f, .6f), f.r); Col(fo.P(-.5f, .7f, -2.3f), new Vector3(4f, 1.4f, .9f), f.r);
            ArtistBits.PointLight(ctx.Root, fo.P(0, 2.6f, -1f), warm, 14f, 2.4f); halos.Add(fo.P(0, 1.6f, .8f), 3.2f, warm, .3f);
            Folk(fo.P(-.8f, 0, -1.2f), f.Fwd, Resident.Pose.Work, new[] { "Salt and vinegar? It's not a question, I've already done it.", "Chips and scraps. Fish is extra. Fish is always extra.", "Eat them on the wall. Mind the gulls. The gulls have a union." }, Docker(Residents.Tool.Spatula, true)).workRate = .9f;
            Folk(fo.P(1.2f, 0, 1.2f), -f.Fwd, Resident.Pose.Stand, new[] { "I'm not queuing, I'm waiting. There's a difference. I think.", "Best chips in the docks. Only chips in the docks." }, Docker()); Folk(fo.P(2.1f, 0, 1.9f), -f.Fwd, Resident.Pose.Stand, new[] { "He's waiting. I'm queuing.", "It's the smell. You come from three streets away without deciding to." }, Docker());
            for (var k = 0; k < 4; k++) { var cm = new MeshKit(); cm.Cone(WorldMaterials.Solid(new Color(.86f, .84f, .76f)), new Vector3(0, .22f, 0), .09f, -.22f, 6); cm.Blob(WorldMaterials.Glow(new Color(1f, .82f, .35f), .3f), new Vector3(0, .25f, 0), new Vector3(.09f, .06f, .09f), prng, .4f); var go = cm.BuildSingle("Cone of chips", plife.transform, false); go.transform.position = fo.P(-2.2f + k * .5f, 1.1f, -.2f); var c = Interact.Carry(go, "a cone of chips", .25f, g.y - 10f); var bc = c.GetComponent<BoxCollider>(); if (bc != null) { bc.size = new Vector3(.2f, .3f, .2f); bc.center = new Vector3(0, .15f, 0); } }
            // a low sea wall across the front to sit on, facing the black water; a lamp
            var seaward = FaceTo(g, g + new Vector3(-1f, 0, .2f) * 50f); var wc = G(g + seaward * 9f); var wq = Quaternion.LookRotation(seaward); mk.Box(concrete, wc + up * .3f, new Vector3(9f, .6f, .7f), wq); solid.Box(concrete, wc + up * .3f, new Vector3(9f, .6f, .7f), wq); var ws = Vector3.Cross(up, seaward);
            Interact.Bench(plife.transform, wc + up * .66f - ws * 3f, wc + up * .66f - ws * 1f, seaward); Interact.Bench(plife.transform, wc + up * .66f + ws * 1f, wc + up * .66f + ws * 3f, seaward);
            Folk(wc + up * .64f, seaward, Resident.Pose.Sit, new[] { "You can't see the sea at night. You can hear it deciding things.", "Chips on the wall, lighthouse going round. I've had worse evenings. I've had mostly worse evenings." }, Docker(), null, .64f);
            SodiumLamp(ctx, mk, G(f.P(-5.5f, 0, 5f)), f.Right, true);
            Side(ctx, mk, f.P(0, 0, 5f)); Ring(g, 9f);
            Shot(ctx, "the chippy", f.P(-5f, 2.2f, 11f), fo.P(0, 1.8f, -1f));
        }
    }

    /// The Docklands places' moving parts: the underpass strip lights (buzzing; they jump when you clap), the presses
    /// and the record belt, the Social's tinsel and bingo cage.
    public class DocksPlacesLife : MonoBehaviour
    {
        public List<(Material m, float phase)> strips = new List<(Material, float)>(), tinsel = new List<(Material, float)>(); public float clap = -99f, bingo = -99f; public Material bingoM;
        public List<(Transform t, Vector3 rest, Material rec, float hit)> presses = new List<(Transform, Vector3, Material, float)>(); public List<(Transform t, float u)> belt = new List<(Transform, float)>(); public Vector3 beltA, beltB;
        static readonly Color Cold = new Color(.55f, .7f, 1f), Hot = new Color(1f, .5f, .15f), Gold = new Color(1f, .9f, .5f);

        public void Press(int i) { var p = presses[i]; p.hit = Time.time; presses[i] = p; }

        void Update()
        {
            var t = Time.time; var ca = t - clap;
            foreach (var (m, ph) in strips) { var buzz = Mathf.PerlinNoise(t * 7f, ph) > .18f ? 1f : .3f; var echo = ca < 3f ? 1f + 2f * Mathf.Abs(Mathf.Sin(ca * 9f)) * (1f - ca / 3f) : 1f; m.SetColor("_EmissionColor", Cold * (2f * buzz * echo)); }
            for (var i = 0; i < presses.Count; i++) { var (tr, rest, rec, hit) = presses[i]; var a = t - hit; var down = a < .4f ? a / .4f : a < 1.4f ? 1f : a < 2.2f ? 1f - (a - 1.4f) / .8f : 0f; tr.position = rest - Vector3.up * down * .85f; rec.SetColor("_EmissionColor", Hot * (.2f + (a > .4f && a < 6f ? 3f * (1f - (a - .4f) / 5.6f) : 0f))); }
            for (var i = 0; i < belt.Count; i++) { var (tr, u) = belt[i]; u = Mathf.Repeat(u + Time.deltaTime * .06f, 1f); belt[i] = (tr, u); tr.position = Vector3.Lerp(beltA, beltB, u); tr.Rotate(0, 40f * Time.deltaTime, 0, Space.World); }
            foreach (var (m, ph) in tinsel) { m.SetColor("_EmissionColor", new Color(1f, .9f, .9f) * (.4f + .5f * Mathf.Max(0f, Mathf.Sin(t * 3f + ph)))); }
            if (bingoM != null) { var a = t - bingo; bingoM.SetColor("_EmissionColor", Gold * (.8f + (a < 3f ? 3f * Mathf.Abs(Mathf.Sin(a * 10f)) * (1f - a / 3f) : 0f))); }
        }
    }
}
