using System.Collections.Generic;
using UnityEngine;
using static EverythingWorld.World.PlaceTools;

namespace EverythingWorld.World
{
    /// The Nu Metal Quarry laid out on purpose (the build-land standard). The gate, the pit and the rim stay where
    /// they were; two more gravel haul roads with marker posts and sodium lamps run out over the empty south. Along
    /// them, with the crew: the Blast Bench (a sandbagged shelter on a hilltop, a plunger to push, a rock pile that
    /// goes up), the Lot (the staff car park: five cars from 2003 nosed up to the view, driver's seats to sit in, a
    /// stereo to turn up, a tailgate, CDs to throw), the Blue Hole (an old pit flooded turquoise: a rope swing, a
    /// jumping rock, a drowned digger) and the Crib (the crew's canteen cabin: tables, a tea urn, a radio that only
    /// gets one station).
    public partial class QuarryKit
    {
        static readonly Vector2 BlastAt = new Vector2(7871, 8233), LotAt = new Vector2(7850, 8080), HoleAt = new Vector2(7950, 7990), CribAt = new Vector2(8100, 8120);
        static readonly Vector2[] SouthHaul = { new Vector2(7925, 8370), new Vector2(7915, 8320), new Vector2(7905, 8275), new Vector2(7900, 8200), new Vector2(7895, 8150), new Vector2(7872, 8108) };
        static readonly Vector2[] HoleRoad = { new Vector2(7872, 8108), new Vector2(7888, 8050), new Vector2(7920, 8012), new Vector2(7936, 8002) };
        static readonly Vector2[] EastHaul = { new Vector2(8095, 8400), new Vector2(8090, 8360), new Vector2(8085, 8330), new Vector2(8080, 8260), new Vector2(8095, 8180), new Vector2(8099, 8142) };
        static readonly (Vector2 at, float r)[] PlaceRings = { (BlastAt, 8f), (LotAt, 15f), (HoleAt, 16f), (CribAt, 10f) };

        QuarryPlacesLife plife; System.Random prng; Transform rootT; LandContext ctx0; readonly List<Vector3> laneP = new List<Vector3>();

        static readonly Color[] HiVis = { new Color(1f, .8f, .1f), new Color(1f, .5f, .1f), new Color(.85f, .9f, .2f), new Color(.25f, .3f, .4f) };
        static readonly Color[] WorkTrousers = { new Color(.2f, .25f, .35f), new Color(.3f, .3f, .3f), new Color(.45f, .4f, .3f) };
        static readonly Color[] HardHats = { new Color(.95f, .8f, .15f), new Color(.95f, .95f, .95f), new Color(.9f, .4f, .1f) };
        static readonly string[] CrewTalk =
        {
            "Hard hats must be worn. Feelings must be big. Those are the two site rules.", "Radio only gets the one station. It only plays the one year. Nobody's complained since 2003.",
            "My first car had a tape adaptor with a wire hanging out of it. Best stereo I ever owned.", "You're meant to sing the chorus like you mean it. That's the whole job, really. That and the rocks.", "We dug the pit to see how far down it went. Turns out: all the way to the bridge.",
        };

        Residents.Look Crew(Residents.Tool tool = Residents.Tool.None) { var l = Residents.Random(prng, HiVis, WorkTrousers, new[] { Residents.Hat.Cap, Residents.Hat.Cap, Residents.Hat.Beanie, Residents.Hat.None }, HardHats); l.tool = tool; return l; }
        Resident Folk(Vector3 feet, Vector3 face, Resident.Pose pose, string[] lines = null, Residents.Look look = null, Vector3[] route = null, float seatH = HumanScale.SeatHeight)
            => Residents.Make(plife.transform, feet, face, look ?? Crew(), pose, prng, lines ?? CrewTalk, route, seatH);
        void Col(Vector3 c, Vector3 size, Quaternion rot) => BoxCol(rootT, c, size, rot);
        Vector3 G(Vector3 p) => ctx0.OnGround(p.x, p.z);
        void Ring(Vector3 at, float r) => noGrass.Add(new Vector3(at.x, r, at.z));

        public override void ShapeGround(IslandWorldData data, int index)
        {
            ShapeQuarry(data, index);   // everything the land already did, unchanged
            foreach (var (at, r) in PlaceRings) Level(data, index, at, r, r + 16f);
        }

        /// A yellow site sign on two posts. Signs on the +x side of a place's road; the photo spot on the −x side.
        void SiteSign(MeshKit mk, Vector3 at, Vector3 face, string text, float size = .19f)
        {
            var up = Vector3.up; var g = G(at); var rot = Quaternion.LookRotation(face);
            foreach (var e in new[] { -1.3f, 1.3f }) mk.Prism(steel, g + Vector3.Cross(up, face) * e - up * .2f, up, .06f, 3f, 6);
            mk.Box(yellow, g + up * 2.2f, new Vector3(3.4f, 1.3f, .08f), rot); mk.Box(dark, g + up * 2.2f - face * .01f, new Vector3(3.55f, 1.45f, .05f), rot); Col(g + up * 1.5f, new Vector3(3.4f, 3f, .16f), rot);
            ArtistBits.Label(rootT, text, g + up * 2.2f + face * .06f, face, size, new Color(.08f, .08f, .09f), TextAnchor.MiddleCenter);
        }

        void SodiumLamp(MeshKit mk, Vector3 g, Vector3 inward, bool light = false)
        {
            var up = Vector3.up; mk.Prism(wood, g - up * .2f, up, .12f, 5.4f, 6); mk.Box(wood, g + up * 5.1f + inward * .5f, new Vector3(.08f, .08f, 1f), Quaternion.LookRotation(inward));
            mk.Box(lampM, g + up * 4.95f + inward * .9f, new Vector3(.4f, .15f, .3f), Quaternion.LookRotation(inward)); halos.Add(g + up * 4.7f + inward * .9f, 1.8f, Sodium, .55f); ArtistBits.PostCollider(rootT, g, 5.2f, .14f);
            if (light) ArtistBits.PointLight(rootT, g + up * 4.2f + inward * 1.6f, Sodium, 16f, 1.5f);
        }

        void FireDrum(MeshKit mk, Vector3 g)
        {
            var up = Vector3.up; mk.Prism(rust, g - up * .05f, up, .42f, 1f, 10); mk.Prism(rustDark, g + up * .3f, up, .44f, .08f, 10); mk.Prism(rustDark, g + up * .75f, up, .44f, .08f, 10);
            mk.Blob(fireM, g + up * 1.1f, new Vector3(.32f, .35f, .32f), prng, .3f); halos.Add(g + up * 1.3f, 2.2f, Fire, .8f); life.drumAt.Add(g + up * 1.05f); ArtistBits.PostCollider(rootT, g, 1f, .44f);
        }

        void PlankBench(MeshKit mk, Vector3 at, Vector3 face, float len = 2.2f)
        {
            var up = Vector3.up; var g = G(at); var rot = Quaternion.LookRotation(face); var side = Vector3.Cross(up, face);
            mk.Box(wood, g + up * .46f, new Vector3(len, .08f, .45f), rot); foreach (var e in new[] { -1f, 1f }) mk.Box(concrete, g + up * .21f + side * e * (len / 2f - .25f), new Vector3(.4f, .42f, .4f), rot);
            Col(g + up * .25f, new Vector3(len, .5f, .45f), rot); Interact.Bench(plife.transform, g + up * .52f - side * (len / 2f - .45f), g + up * .52f + side * (len / 2f - .45f), face);
        }

        List<Vector3> HaulRoad(LandContext ctx, MeshKit mk, Vector2[] way, bool join)
        {
            var full = new List<Vector2>();
            if (join) { Vector2 near = way[0]; var bd = 260f * 260f; foreach (var (p, w) in walkPoints) { if (w >= 400 || InPit(p.x, p.z, 4f)) continue; var d = (p.x - way[0].x) * (p.x - way[0].x) + (p.z - way[0].y) * (p.z - way[0].y); if (d < bd) { bd = d; near = new Vector2(p.x, p.z); } } full.Add(near); }
            full.AddRange(way);
            var pts = PlaceTools.Smooth(ctx, full, false, 2f);
            for (var i = 0; i < pts.Count; i++) { foreach (var a in ctx.Artists) { var r = Foot(a) + 6f; var v = new Vector2(pts[i].x - a.x, pts[i].z - a.z); var d = v.magnitude; if (d < r) { v = d > .01f ? v / d * r : new Vector2(r, 0); pts[i] = ctx.OnGround(a.x + v.x, a.z + v.y); } } }
            if (pts.Count < 2) return pts;
            Strip(mk, pts, gravel, PathW + .8f, .15f, ctx); foreach (var p in pts) Remember(p, 700);
            var up = Vector3.up; var red = GlowM(new Color(1f, .25f, .15f), 1.4f);
            for (var i = 3; i < pts.Count; i += 5)
            {
                var d = pts[Mathf.Min(i + 1, pts.Count - 1)] - pts[i - 1]; d.y = 0; if (d.sqrMagnitude < .01f) continue; d.Normalize(); var side = Vector3.Cross(up, d);
                foreach (var e in new[] { -1f, 1f }) { var mp = pts[i] + side * e * (PathW / 2f + .9f); if (InNoGrass(mp.x, mp.z, .5f) || !ctx.IsLand(mp.x, mp.z, 2f)) continue; mp = ctx.OnGround(mp.x, mp.z); mk.Prism(yellow, mp - up * .1f, up, .06f, 1.1f, 5); ArtistBits.PostCollider(ctx.Root, mp, 1f, .08f); mk.Box(red, mp + up * .9f, new Vector3(.14f, .14f, .14f), Quaternion.LookRotation(d)); }
                if ((i / 5) % 4 == 1) { var s = (i / 20) % 2 == 0 ? 1f : -1f; var lp = pts[i] + side * s * (PathW / 2f + 1.6f); if (!InNoGrass(lp.x, lp.z, 1f) && !InFootprint(ctx, lp.x, lp.z, 1f) && ctx.IsLand(lp.x, lp.z, 2f)) SodiumLamp(mk, ctx.OnGround(lp.x, lp.z), -side * s); }
            }
            laneP.AddRange(pts); return pts;
        }

        void Spur(LandContext ctx, MeshKit mk, Vector3 to)
        {
            var from = Nearest(laneP, to); var d = new Vector2(to.x - from.x, to.z - from.z); var len = d.magnitude; if (len < 4f) return; d /= len;
            var a = new Vector2(from.x, from.z) + d * 2f; var b = new Vector2(to.x, to.z);
            var pts = PlaceTools.Smooth(ctx, new[] { a, Vector2.Lerp(a, b, .5f), b }, false, 2f, 1f); if (pts.Count < 2) return;
            Strip(mk, pts, gravel, 2.6f, .14f, ctx); foreach (var p in pts) Remember(p, 900);
        }

        void QuarryPlaces(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            laneP.Clear(); rootT = ctx.Root; ctx0 = ctx; prng = new System.Random(2003);
            plife = new GameObject("Quarry places life").AddComponent<QuarryPlacesLife>(); plife.transform.SetParent(life.transform, false);
            foreach (var (at, r) in PlaceRings) noGrass.Add(new Vector3(at.x, r, at.y));
            HaulRoad(ctx, mk, SouthHaul, true); HaulRoad(ctx, mk, HoleRoad, false); HaulRoad(ctx, mk, EastHaul, true);
            BlastBench(ctx, mk, solid); Lot(ctx, mk, solid); BlueHole(ctx, mk, solid); Crib(ctx, mk, solid);
        }

        // ---------------------------------------------------------------- the Blast Bench
        void BlastBench(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var g = ctx.OnGround(BlastAt.x, BlastAt.y); var toRoad = FaceTo(g, Nearest(laneP, g)); var f = new PlaceFrame(g, -toRoad);   // the shelter looks away from the road, out at the face
            mk.Prism(gravel, g - up * .3f, up, 7.5f, .42f, 12);
            // a three-sided shelter of corrugated sheet behind a wall of sandbags, a bench inside
            var sh = new PlaceFrame(g - f.Fwd * 1.5f + up * .12f, f.Fwd); var sheet = WorldMaterials.Solid(new Color(.5f, .55f, .55f), .5f);
            mk.Box(sheet, sh.P(0, 1.2f, -1.2f), new Vector3(4f, 2.4f, .08f), sh.r); solid.Box(sheet, sh.P(0, 1.2f, -1.2f), new Vector3(4f, 2.4f, .12f), sh.r); foreach (var e in new[] { -1f, 1f }) { mk.Box(sheet, sh.P(e * 2f, 1.2f, -.2f), new Vector3(.08f, 2.4f, 2f), sh.r); solid.Box(sheet, sh.P(e * 2f, 1.2f, -.2f), new Vector3(.12f, 2.4f, 2f), sh.r); }
            mk.Box(rust, sh.P(0, 2.5f, -.1f), new Vector3(4.4f, .08f, 2.8f), sh.r * Quaternion.Euler(-6f, 0, 0)); for (var k = 0; k < 9; k++) mk.Box(rustDark, sh.P(-2f + k * .5f, 2.56f, -.1f), new Vector3(.06f, .04f, 2.8f), sh.r * Quaternion.Euler(-6f, 0, 0));
            mk.Box(wood, sh.P(0, .46f, -.8f), new Vector3(3.4f, .08f, .5f), sh.r); foreach (var e in new[] { -1f, 1f }) mk.Box(concrete, sh.P(e * 1.4f, .22f, -.8f), new Vector3(.4f, .44f, .4f), sh.r); Col(sh.P(0, .25f, -.8f), new Vector3(3.4f, .5f, .5f), sh.r); Interact.Bench(plife.transform, sh.P(-1.2f, .52f, -.75f), sh.P(1.2f, .52f, -.75f), sh.Fwd);
            var bag = WorldMaterials.Solid(new Color(.7f, .64f, .5f)); for (var row = 0; row < 3; row++) for (var k = 0; k < 7 - row; k++) mk.Blob(bag, sh.P(-2.1f + row * .35f + k * .7f, .18f + row * .3f, 2.2f), new Vector3(.36f, .17f, .24f), prng, .08f); Col(sh.P(0, .5f, 2.2f), new Vector3(4.8f, 1f, .5f), sh.r);
            // the plunger on its box, wires running off over the edge, a red lamp on a pole, a flag
            var pb = sh.P(1.2f, 0, 1.2f); mk.Box(wood, pb + up * .25f, new Vector3(.5f, .5f, .4f), sh.r); mk.Box(dark, pb + up * .52f, new Vector3(.42f, .06f, .32f), sh.r); Col(pb + up * .3f, new Vector3(.5f, .6f, .4f), sh.r);
            var pm = new MeshKit(); pm.Prism(steel, Vector3.zero, Vector3.up, .025f, .5f, 6); pm.Box(WorldMaterials.Solid(new Color(.8f, .15f, .12f)), new Vector3(0, .5f, 0), new Vector3(.4f, .06f, .06f), Quaternion.identity); var pgo = pm.BuildSingle("Plunger", plife.transform, false); pgo.transform.SetPositionAndRotation(pb + up * .55f, sh.r); plife.plunger = pgo.transform; plife.plungerUp = pgo.transform.position;
            var target = G(g + f.Fwd * 38f); for (var k = 0; k < 10; k++) { var a = Vector3.Lerp(pb + up * .1f, target, k / 10f); var b = Vector3.Lerp(pb + up * .1f, target, (k + 1) / 10f); a = G(a) + up * .08f; b = G(b) + up * .08f; mk.Prism(WorldMaterials.Solid(new Color(.8f, .2f, .15f)), a, (b - a).normalized, .02f, (b - a).magnitude, 4); }
            var lp = G(sh.P(-2.8f, 0, 1.6f)); mk.Prism(steel, lp - up * .2f, up, .06f, 3.4f, 6); var siren = new Material(GlowM(new Color(1f, .15f, .1f), .6f)) { name = "Blast lamp" }; mk.Blob(siren, lp + up * 3.3f, Vector3.one * .2f, prng, 0f); ArtistBits.PostCollider(ctx.Root, lp, 3.4f, .08f); plife.sirenM = siren; plife.sirenLight = ArtistBits.PointLight(ctx.Root, lp + up * 3.3f, new Color(1f, .2f, .12f), 18f, 0f);
            var flag = G(sh.P(3.2f, 0, 1.4f)); mk.Prism(steel, flag - up * .2f, up, .04f, 4f, 5); mk.Box(WorldMaterials.Solid(new Color(.9f, .15f, .12f)), flag + up * 3.6f + f.Right * .45f, new Vector3(.9f, .6f, .03f), f.r); ArtistBits.PostCollider(ctx.Root, flag, 4f, .06f);
            // out at the face: a heap of shot rock, loose rocks on it that go up with the blast, a flash, dust
            mk.Cone(rock, target - up * .4f, 5f, 2.6f, 9); mk.Cone(rockDark, target + f.Right * 3.5f - up * .9f, 3.4f, 2.3f, 8); BoxCol(ctx.Root, target + up * .6f, new Vector3(6f, 2f, 6f), Quaternion.identity); noGrass.Add(new Vector3(target.x, 8f, target.z));
            for (var k = 0; k < 8; k++) { var m = new MeshKit(); var s = .18f + (k % 3) * .06f; m.Blob(k % 2 == 0 ? rock : rockDark, Vector3.zero, new Vector3(s, s * .8f, s * .9f), prng, .25f); var go = m.BuildSingle("Shot rock", plife.transform, false); go.transform.position = target + up * (2.5f + (k % 2) * .3f) + Quaternion.Euler(0, k * 45f, 0) * Vector3.forward * (.6f + (k % 3) * .5f); var c = Interact.Carry(go, "a lump of shot rock", 1.2f, g.y - 40f); var bc = c.GetComponent<BoxCollider>(); if (bc != null) { bc.size = Vector3.one * s * 1.7f; bc.center = Vector3.zero; } var rb = go.GetComponent<Rigidbody>(); if (rb != null) rb.isKinematic = true; plife.rocks.Add(c); }
            plife.blastAt = target + up * 2f; plife.flash = ArtistBits.PointLight(ctx.Root, target + up * 4f, new Color(1f, .8f, .5f), 70f, 0f); var dm = new MeshKit(); dm.Blob(WorldMaterials.Glow(new Color(.75f, .7f, .62f), .25f), Vector3.zero, new Vector3(4f, 3f, 4f), prng, .3f); var dgo = dm.BuildSingle("Blast dust", plife.transform, false); dgo.transform.position = target + up * 2f; dgo.SetActive(false); plife.dust = dgo.transform;
            string[] bang = { "\"FIRE IN THE HOLE!\" ...three, two, one.", "\"Heads down!\" ...three, two, one.", "The siren winds up. Somewhere a crow thinks better of it." }; var bn = 0;
            Interact.Toy(pgo, Toy.Kind.Custom, "Push the plunger", new Vector3(.9f, 1.2f, .9f)).onUse = _ => { if (plife.Fire()) Say(plife.transform, pb + up * 2.4f, bang[bn++ % bang.Length], 4f); };
            var firer = Crew(); firer.hat = Residents.Hat.Cap; firer.hatColor = new Color(.9f, .15f, .12f); firer.top = new Color(1f, .5f, .1f); Folk(sh.P(-1.2f, 0, 1.3f), f.Fwd, Resident.Pose.Lean, new[] { "I'm the shot-firer. Thirty years. Still jump every time.", "Push it if you like. It's only the practice heap. The real face goes Thursdays.", "Quiet, then loud, then louder. Quarrying's the same as the songs, really.", "The rocks come back down roughly where they went up. Roughly." }, firer);
            SiteSign(mk, g + toRoad * 9f + Vector3.Cross(up, toRoad) * 5f, toRoad, "BLAST BENCH\nwait for the siren", .18f);
            Spur(ctx, mk, g + toRoad * 5f); Ring(g, 8f);
            Shot(ctx, "the blast bench", g + toRoad * 11f - Vector3.Cross(up, toRoad) * 6f + up * 4f, g + f.Fwd * 8f + up * 1f);
        }

        // ---------------------------------------------------------------- the Lot
        void Car(MeshKit mk, PlaceFrame c, Color paint, int kind, bool doorOpen, out Material heads)
        {
            var up = Vector3.up; var body = WorldMaterials.Solid(paint, .5f); var glass = WorldMaterials.Solid(new Color(.25f, .32f, .38f), .8f); heads = new Material(GlowM(new Color(1f, .95f, .8f), .25f)) { name = "Headlights" };
            var pickup = kind == 2; var len = pickup ? 5f : 4.3f;
            mk.Box(body, c.P(0, .62f, 0), new Vector3(1.75f, .55f, len), c.r); mk.Box(dark, c.P(0, .36f, 0), new Vector3(1.7f, .12f, len - .2f), c.r);
            if (pickup) { mk.Box(body, c.P(0, 1.2f, .7f), new Vector3(1.65f, .65f, 1.5f), c.r); mk.Box(glass, c.P(0, 1.22f, .7f), new Vector3(1.68f, .45f, 1.3f), c.r); foreach (var e in new[] { -1f, 1f }) mk.Box(body, c.P(e * .82f, 1.05f, -1.4f), new Vector3(.08f, .35f, 2f), c.r); mk.Box(dark, c.P(0, .92f, -1.4f), new Vector3(1.56f, .04f, 2f), c.r); mk.Box(body, c.P(0, .9f, -2.7f), new Vector3(1.65f, .06f, .5f), c.r); }
            else { mk.Box(body, c.P(0, 1.18f, -.2f), new Vector3(1.6f, .58f, kind == 1 ? 2.6f : 2f), c.r); mk.Box(glass, c.P(0, 1.2f, -.2f), new Vector3(1.63f, .4f, kind == 1 ? 2.4f : 1.8f), c.r); mk.Box(glass, c.P(0, 1.15f, .9f), new Vector3(1.5f, .5f, .06f), c.r * Quaternion.Euler(-35f, 0, 0)); }
            foreach (var (x, z) in new[] { (-.85f, 1.35f), (.85f, 1.35f), (-.85f, -1.35f), (.85f, -1.35f) }) mk.Prism(dark, c.P(x - Mathf.Sign(x) * .1f, .34f, z * (len / 4.3f)), c.Right * Mathf.Sign(x), .34f, .22f, 10);
            foreach (var e in new[] { -1f, 1f }) { mk.Box(heads, c.P(e * .6f, .68f, len / 2f + .01f), new Vector3(.35f, .2f, .03f), c.r); mk.Box(GlowM(new Color(1f, .15f, .1f), .6f), c.P(e * .6f, .7f, -len / 2f - .01f), new Vector3(.35f, .16f, .03f), c.r); }
            Col(c.P(0, .75f, 0), new Vector3(1.75f, 1.5f, len), c.r);
            if (doorOpen) { mk.Box(body, c.P(-1.35f, .78f, .75f), new Vector3(.06f, .75f, 1f), c.r * Quaternion.Euler(0, -65f, 0)); Interact.Seat(plife.transform, c.P(-.85f, .62f, .25f), c.Fwd, "Sit in the driver's seat"); }
        }

        void Lot(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var g = ctx.OnGround(LotAt.x, LotAt.y); var f = new PlaceFrame(g, FaceTo(g, Nearest(laneP, g))); var fo = new PlaceFrame(g + up * .1f, f.Fwd);
            mk.Box(WorldMaterials.Solid(new Color(.3f, .3f, .31f)), g - up * .2f, new Vector3(24f, .5f, 16f), f.r); for (var k = 0; k <= 5; k++) mk.Box(WorldMaterials.Solid(new Color(.9f, .88f, .8f)), fo.P(-10f + k * 4f, .16f, -4f), new Vector3(.12f, .02f, 5.5f), f.r);
            // five cars nosed up to the far edge, looking out
            Color[] paints = { new Color(.55f, .1f, .12f), new Color(.65f, .68f, .7f), new Color(.15f, .25f, .45f), new Color(.75f, .7f, .5f), new Color(.12f, .3f, .2f) }; var stereoHeads = (Material)null; PlaceFrame stereoCar = default;
            for (var k = 0; k < 5; k++) { var cf = new PlaceFrame(fo.P(-8f + k * 4f, .06f, -4f + (k % 2) * .4f), -f.Fwd); Car(mk, cf, paints[k], k == 2 ? 2 : k % 2, k == 0 || k == 3, out var heads); plife.headlights.Add(heads); if (k == 3) { stereoHeads = heads; stereoCar = cf; } }
            // the one with the stereo: turn the key
            var dash = stereoCar.P(-.4f, 1f, .75f); var dm = new MeshKit(); var dashM = new Material(GlowM(new Color(.3f, .9f, 1f), .3f)) { name = "Dashboard" }; dm.Box(dashM, Vector3.zero, new Vector3(.5f, .12f, .05f), Quaternion.identity); var dgo = dm.BuildSingle("Car stereo", plife.transform, false); dgo.transform.SetPositionAndRotation(dash, stereoCar.r); plife.dashM = dashM; plife.stereoHeads = stereoHeads; plife.stereoLight = ArtistBits.PointLight(ctx.Root, stereoCar.P(0, 1f, 4f), new Color(1f, .95f, .8f), 16f, 0f);
            string[] cd = { "The CD skips in the second chorus, where it always skipped. You sing the skip.", "Track one. Windows down. The speakers are mostly rattle and it's perfect.", "A burned disc with MIX 4 on it in marker. Whoever made it was going through something.", "The tape adaptor wire swings off the dash. The bass is one big door panel." }; var cn = 0;
            Interact.Toy(dgo, Toy.Kind.Custom, "Turn the key and turn it up", new Vector3(1.6f, 1f, 1.6f)).onUse = _ => { plife.stereo = Time.time; Say(plife.transform, stereoCar.P(0, 2.3f, 0), cd[cn++ % cd.Length], 5f); };
            // the pickup's tailgate: sit on it, a cooler, cans; two of the crew
            var pk = new PlaceFrame(fo.P(0, .06f, -4f), -f.Fwd); Interact.Bench(plife.transform, pk.P(-.5f, .98f, -2.75f), pk.P(.5f, .98f, -2.75f), -pk.Fwd);
            Folk(pk.P(.55f, .96f, -2.75f), -pk.Fwd, Resident.Pose.Sit, new[] { "Knock-off was an hour ago. Nobody wants to be the first to start their engine.", "This truck's got two hundred thousand on it and one CD in it. Never needed another.", "You sit on a tailgate long enough, somebody tells you something true." }, Crew(Residents.Tool.Mug), null, .96f);
            var cooler = G(pk.P(1.6f, 0, -3.4f)); mk.Box(WorldMaterials.Solid(new Color(.2f, .4f, .75f)), cooler + up * .25f, new Vector3(.7f, .4f, .45f), f.r); mk.Box(PaleM(), cooler + up * .48f, new Vector3(.72f, .08f, .47f), f.r); Col(cooler + up * .27f, new Vector3(.7f, .5f, .45f), f.r);
            for (var k = 0; k < 4; k++) { var m = new MeshKit(); m.Prism(WorldMaterials.Solid(k % 2 == 0 ? new Color(.8f, .15f, .15f) : new Color(.75f, .78f, .8f), .7f), Vector3.zero, Vector3.up, .033f, .12f, 8); var go = m.BuildSingle("Can", plife.transform, false); go.transform.position = cooler + up * .53f + f.Right * (-.22f + k * .15f); var c = Interact.Carry(go, "a can of something cold", .15f, g.y - 30f); var bc = c.GetComponent<BoxCollider>(); if (bc != null) { bc.size = new Vector3(.08f, .12f, .08f); bc.center = new Vector3(0, .06f, 0); } }
            // CDs on a hood, to throw like you shouldn't
            var hood = new PlaceFrame(fo.P(-4f, .06f, -3.6f), -f.Fwd); for (var k = 0; k < 5; k++) { var m = new MeshKit(); m.Prism(WorldMaterials.Glow(new Color(.8f, .9f, 1f), .5f), Vector3.zero, Vector3.up, .06f, .004f, 14); m.Prism(dark, new Vector3(0, .001f, 0), Vector3.up, .012f, .004f, 8); var go = m.BuildSingle("CD", plife.transform, false); go.transform.position = hood.P(-.4f + k * .2f, .92f + k * .006f, 1.6f + (k % 2) * .15f); var c = Interact.Carry(go, k == 0 ? "a burned CD (MIX 4)" : "a scratched CD", .08f, g.y - 30f); var bc = c.GetComponent<BoxCollider>(); if (bc != null) { bc.size = new Vector3(.12f, .02f, .12f); bc.center = Vector3.zero; } var rb = go.GetComponent<Rigidbody>(); if (rb != null) rb.isKinematic = true; }
            Folk(hood.P(-1.15f, -.06f, 1.9f), hood.Right, Resident.Pose.Lean, new[] { "I'm not waiting for anybody. I'm just letting the song finish.", "You don't turn the engine off mid-chorus. That's how you get bad luck.", "First gig I ever went to, I drove four hours in this. Lost my voice by song three." }, Crew());
            // a floodlight tower, a drum fire, a bench facing the cars, a boom gate that's always up
            SodiumLamp(mk, G(fo.P(-11.5f, 0, 6.5f)), f.Right, true); SodiumLamp(mk, G(fo.P(11.5f, 0, -6.5f)), -f.Right, true); FireDrum(mk, G(fo.P(6f, 0, 3.5f))); PlankBench(mk, fo.P(8f, 0, 4.5f), -f.Right); PlankBench(mk, fo.P(-6f, 0, 5f), -f.Fwd, 2.6f);
            var gate = G(fo.P(2.4f, 0, 8.6f)); mk.Box(yellow, gate + up * .6f, new Vector3(.3f, 1.2f, .3f), f.r); mk.Box(WorldMaterials.Solid(new Color(.9f, .15f, .12f)), gate + up * 2.6f - f.Right * 1.3f, new Vector3(.1f, .1f, 4.4f), f.r * Quaternion.Euler(0, 90f, 55f)); ArtistBits.PostCollider(ctx.Root, gate, 1.2f, .25f);
            SiteSign(mk, fo.P(7f, 0, 10.5f), f.Fwd, "THE LOT\nstaff parking  ·  engines off, stereo on", .15f);
            Spur(ctx, mk, fo.P(0, 0, 8.5f)); Ring(g, 14f);
            Shot(ctx, "the lot", fo.P(-12f, 4f, 13f), fo.P(0, 1f, -3f));
        }

        Material PaleM() => WorldMaterials.Solid(new Color(.93f, .93f, .9f));

        // ---------------------------------------------------------------- the Blue Hole
        void BlueHole(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var g = ctx.OnGround(HoleAt.x, HoleAt.y); var f = new PlaceFrame(g, FaceTo(g, Nearest(laneP, g))); const float R = 11.5f;
            // the water: a still turquoise disc, lighter in the shallows; pale rock all round it
            var water = new Material(WorldMaterials.Glow(new Color(.15f, .8f, .85f), .55f)) { name = "Blue Hole water" }; plife.waterM = water;
            mk.Prism(WorldMaterials.Solid(new Color(.8f, .8f, .76f)), g - up * .25f, up, R + 3.5f, .3f, 22); mk.Prism(WorldMaterials.Glow(new Color(.5f, .95f, .9f), .4f), g + up * .06f, up, R, .02f, 22); mk.Prism(water, g + up * .08f, up, R - 2.2f, .02f, 20);
            halos.Add(g + up * .6f, R * .7f, new Color(.2f, .85f, .9f), .18f); ArtistBits.PointLight(ctx.Root, g + up * 2.5f, new Color(.3f, .9f, .95f), 26f, 1.8f);
            var pale = WorldMaterials.Solid(new Color(.86f, .85f, .8f));
            for (var k = 0; k < 22; k++) { var a = k * 360f / 22f + 6f; var d = Quaternion.Euler(0, a, 0) * f.Fwd; if (Mathf.Abs(Mathf.DeltaAngle(a, 0f)) < 28f) continue; var s = 1f + (k * 7 % 5) * .35f; var p = G(g + d * (R + 1.2f + (k % 3) * .5f)); mk.Blob(k % 3 == 0 ? rock : pale, p + up * s * .3f, new Vector3(s, s * .7f, s * .9f), prng, .28f); BoxCol(ctx.Root, p + up * s * .3f, new Vector3(s * 1.6f, s * 1.3f, s * 1.5f), Quaternion.identity); }
            // the back wall: a cut face three benches high, so it reads as an old pit
            for (var k = 0; k < 3; k++) { var wr = Quaternion.LookRotation(f.Fwd); var c = g - f.Fwd * (R + 3.5f + k * 2.2f) + up * (1.2f + k * 2.2f); mk.Box(k % 2 == 0 ? rock : rockDark, c, new Vector3(22f - k * 3f, 2.6f + k * .3f, 2.6f), wr); solid.Box(rock, c, new Vector3(22f - k * 3f, 2.6f + k * .3f, 2.6f), wr); }
            // the jumping rock: a slab out over the water with steps cut up the back
            var jr = g + f.Right * (R - 1.5f) - f.Fwd * 2f; var jrot = Quaternion.LookRotation(-f.Right); mk.Box(pale, jr + up * 1.1f, new Vector3(3f, 2.2f, 4.5f), jrot); solid.Box(pale, jr + up * 1.1f, new Vector3(3f, 2.2f, 4.5f), jrot);
            for (var k = 0; k < 6; k++) { var sp = jr + f.Right * (2.4f + (5 - k) * .4f) + up * ((k + .5f) * .366f / 1f); mk.Box(pale, sp - up * (k * .183f), new Vector3(1.6f, .366f * (k + 1), .4f), jrot); solid.Box(pale, sp - up * (k * .183f), new Vector3(1.6f, .366f * (k + 1), .4f), jrot); }
            Interact.Seat(plife.transform, jr + up * 2.22f - f.Right * 2f, -f.Right, "Sit on the jumping rock"); ArtistBits.Label(ctx.Root, "JUMP", jr + up * 1.4f - f.Right * 2.27f, -f.Right, .3f, new Color(.85f, .2f, .15f), TextAnchor.MiddleCenter);
            // the drowned digger: a boom and bucket reaching up out of the middle
            var dg = g - f.Right * 2.5f - f.Fwd * 2f; mk.Box(yellow, dg + up * .5f, new Vector3(2.4f, 1.2f, 3.2f), Quaternion.Euler(8f, 30f, 6f)); mk.Prism(yellow, dg + up * .8f, new Vector3(.3f, .9f, .25f).normalized, .22f, 4.4f, 4); var el = dg + new Vector3(.3f, .9f, .25f).normalized * 4.4f + up * .8f; mk.Prism(yellow, el, new Vector3(.5f, -.6f, .4f).normalized, .16f, 2.6f, 4); mk.Box(rust, el + new Vector3(.5f, -.6f, .4f).normalized * 2.8f, new Vector3(1f, .8f, .9f), Quaternion.Euler(20f, 40f, 0)); BoxCol(ctx.Root, dg + up * 1f, new Vector3(3f, 2.4f, 3.4f), Quaternion.Euler(0, 30f, 0));
            // the rope swing from a crane jib on the bank
            var mast = G(g - f.Right * (R + 1.8f) + f.Fwd * 2f); mk.Prism(rust, mast - up * .3f, up, .2f, 7f, 6); var tip = mast + up * 6.6f + f.Right * 5f; mk.Prism(rust, mast + up * 6.6f, f.Right, .12f, 5.2f, 4); mk.Prism(rustDark, mast + up * 3.6f, (tip - (mast + up * 3.6f)).normalized, .06f, (tip - (mast + up * 3.6f)).magnitude, 4); ArtistBits.PostCollider(ctx.Root, mast, 7f, .24f);
            var sm = new MeshKit(); sm.Prism(WorldMaterials.Solid(new Color(.75f, .68f, .5f)), new Vector3(0, -5.2f, 0), Vector3.up, .035f, 5.2f, 5); sm.Prism(dark, new Vector3(-.35f, -5.25f, 0), Vector3.right, .16f, .7f, 8); var sgo = sm.BuildSingle("Rope swing", plife.transform, false); sgo.transform.SetPositionAndRotation(tip, Quaternion.LookRotation(f.Fwd)); var sw = sgo.AddComponent<Sway>(); sw.axis = Vector3.right; sw.degrees = 34f; sw.speed = 1.7f; sw.decay = .14f; sw.seconds = 26f;
            var sgrab = new GameObject("Rope swing grab"); sgrab.transform.SetParent(plife.transform, false); sgrab.transform.position = tip - up * 4.6f; string[] swl = { "Out over the blue and back. Somebody on the bank shouts a score.", "The rope creaks. It has held worse than you.", "For a second at the top you weigh nothing and owe nobody." }; var sn = 0;
            Interact.Toy(sgrab, Toy.Kind.Custom, "Swing out over the water", new Vector3(1.6f, 3f, 1.6f)).onUse = _ => { sw.Push(); plife.splash = Time.time; Say(plife.transform, tip - up * 2.6f, swl[sn++ % swl.Length], 4.5f); };
            // an inner tube that bobs, towels and a radio on the bank, people
            var tm = new MeshKit(); for (var q = 0; q < 12; q++) { var an = q * Mathf.PI / 6f; tm.Blob(dark, new Vector3(Mathf.Cos(an) * .5f, 0, Mathf.Sin(an) * .5f), new Vector3(.2f, .17f, .2f), prng, 0f); } var tgo = tm.BuildSingle("Inner tube", plife.transform, false); tgo.transform.position = g + f.Right * 2.5f + f.Fwd * 3f + up * .22f; var tt = Interact.Toy(tgo, Toy.Kind.Bob, "Push the inner tube", new Vector3(1.6f, .8f, 1.6f)); tt.amount = -.1f; tt.seconds = 1.2f; plife.tube = tgo.transform; plife.tubeAt = tgo.transform.position;
            var bank = g + f.Fwd * (R + 1.6f); Color[] towels = { new Color(.9f, .3f, .25f), new Color(.95f, .8f, .2f), new Color(.3f, .5f, .85f) }; for (var k = 0; k < 3; k++) mk.Box(WorldMaterials.Solid(towels[k]), G(bank + f.Right * (-5f + k * 2f) - f.Fwd * .3f) + up * .1f, new Vector3(.9f, .03f, 1.8f), f.r * Quaternion.Euler(0, -10f + k * 12f, 0));
            Interact.Seat(plife.transform, G(bank + f.Right * -5f - f.Fwd * .3f) + up * .14f, -f.Fwd, "Sit on the towel"); Interact.Seat(plife.transform, G(bank + f.Right * -1f - f.Fwd * .3f) + up * .14f, -f.Fwd, "Sit on the towel");
            Folk(G(bank + f.Right * -3f - f.Fwd * .3f) + up * .14f, -f.Fwd, Resident.Pose.Sit, new[] { "Old number two pit. They hit a spring in '98 and it filled up over a weekend. Best thing the company ever did by accident.", "It's that blue because of the minerals. Or the sky. Or a truck they never got out. Depends who you ask.", "No swimming, says the sign. The sign's been in the water since 2001." }, Crew(), null, .14f);
            var fisher = Crew(Residents.Tool.Rod); fisher.hat = Residents.Hat.Bucket; fisher.hatColor = new Color(.5f, .55f, .4f); Folk(G(g + f.Right * (R + .2f) + f.Fwd * 5f), -f.Right, Resident.Pose.Stand, new[] { "Nothing lives in it. I'm not fishing. I'm standing near water with a stick.", "Caught a hubcap once. Threw it back. Wasn't in season." }, fisher);
            var ns = g + f.Fwd * (R - 3f) - f.Right * 4f; mk.Prism(steel, ns - up * .2f, up, .05f, 1.8f, 5); mk.Box(PaleM(), ns + up * 1.4f, new Vector3(1.4f, .7f, .05f), f.r * Quaternion.Euler(0, 0, 14f)); ArtistBits.Label(ctx.Root, "NO SWIMMING", ns + up * 1.42f + f.Fwd * .04f, f.Fwd, .13f, new Color(.8f, .15f, .12f), TextAnchor.MiddleCenter);
            PlankBench(mk, bank + f.Right * 4.5f + f.Fwd * .6f, -f.Fwd); FireDrum(mk, G(bank + f.Right * 7.5f + f.Fwd * 1f));
            SiteSign(mk, g + f.Fwd * (R + 9f) + f.Right * 6f, f.Fwd, "THE BLUE HOLE\nold no. 2 pit  ·  deep water", .17f);
            Spur(ctx, mk, g + f.Fwd * (R + 3f)); Ring(g, 16f);
            Shot(ctx, "the blue hole", g + f.Fwd * (R + 10f) - f.Right * 9f + up * 6f, g + up * .5f);
        }

        // ---------------------------------------------------------------- the Crib
        void Crib(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var g = ctx.OnGround(CribAt.x, CribAt.y); var f = new PlaceFrame(g, FaceTo(g, Nearest(laneP, g))); const float W = 11f, D = 5.4f, H = 2.9f, T = .2f, Door = HumanScale.DoorWidth + .4f;
            var fo = new PlaceFrame(g + up * .45f, f.Fwd); var cabin = WorldMaterials.Solid(new Color(.72f, .7f, .58f)); var lino = WorldMaterials.Solid(new Color(.5f, .55f, .5f)); var warm = new Color(1f, .85f, .6f);
            mk.Prism(gravel, g - up * .3f, up, 9.5f, .4f, 12);
            // a long portable cabin up on blocks, two steps to the door
            foreach (var (x, z) in new[] { (-4.5f, -2f), (4.5f, -2f), (-4.5f, 2f), (4.5f, 2f), (0f, -2f), (0f, 2f) }) mk.Box(concrete, f.P(x, .2f, z), new Vector3(.5f, .4f, .5f), f.r);
            mk.Box(lino, fo.P(0, -.05f, 0), new Vector3(W, .12f, D), f.r); solid.Box(lino, fo.P(0, -.25f, 0), new Vector3(W, .5f, D), f.r);
            void Wall(Vector3 p, Vector3 s) { mk.Box(cabin, p, s, f.r); solid.Box(cabin, p, s, f.r); }
            Wall(fo.P(0, H / 2f, -D / 2f + T / 2f), new Vector3(W, H, T)); foreach (var e in new[] { -1f, 1f }) Wall(fo.P(e * (W / 2f - T / 2f), H / 2f, 0), new Vector3(T, H, D - T * 2f));
            var dx = 2.6f; var lw = W / 2f + dx - Door / 2f; var rw = W / 2f - dx - Door / 2f; Wall(fo.P(-W / 2f + lw / 2f, .5f, D / 2f - T / 2f), new Vector3(lw, 1f, T)); Wall(fo.P(-W / 2f + lw / 2f, H - .35f, D / 2f - T / 2f), new Vector3(lw, .7f, T)); foreach (var x in new[] { -W / 2f + .2f, -W / 2f + lw / 2f, -W / 2f + lw - .2f }) Wall(fo.P(x, H / 2f, D / 2f - T / 2f), new Vector3(.4f, H, T));
            solid.Box(cabin, fo.P(-W / 2f + lw / 2f, 1.6f, D / 2f - T / 2f), new Vector3(lw, 1.2f, .08f), f.r); mk.Box(WorldMaterials.Glow(warm, .25f), fo.P(-W / 2f + lw / 2f, 2.1f, D / 2f - T / 2f), new Vector3(lw - .6f, .12f, .04f), f.r);
            Wall(fo.P(W / 2f - rw / 2f, H / 2f, D / 2f - T / 2f), new Vector3(rw, H, T)); Wall(fo.P(dx, (H + HumanScale.DoorHeight) / 2f, D / 2f - T / 2f), new Vector3(Door, H - HumanScale.DoorHeight, T));
            mk.Box(WorldMaterials.Solid(new Color(.6f, .6f, .55f)), fo.P(0, H + .08f, 0), new Vector3(W + .3f, .16f, D + .3f), f.r); solid.Box(concrete, fo.P(0, H + .08f, 0), new Vector3(W + .3f, .16f, D + .3f), f.r);
            for (var k = 0; k < 2; k++) { var sp = fo.P(dx, -.34f + k * .22f - .11f + .11f, D / 2f + .75f - k * .4f); mk.Box(steel, sp, new Vector3(Door + .3f, .08f, .42f), f.r); solid.Box(steel, sp - up * ((.11f + k * .22f) / 2f), new Vector3(Door + .3f, .22f + k * .22f, .42f), f.r); }
            ArtistBits.Label(ctx.Root, "THE CRIB", fo.P(dx, H - .32f, D / 2f + .02f), f.Fwd, .26f, new Color(.1f, .1f, .12f), TextAnchor.MiddleCenter);
            IndoorSpaces.Add(fo.P(0, H / 2f, 0), f.r, new Vector3(W - .5f, H, D - .5f)); ArtistBits.PointLight(ctx.Root, fo.P(-1.5f, 2.4f, 0), warm, 11f, 1.8f, true); ArtistBits.PointLight(ctx.Root, f.P(dx, 2.6f, D / 2f + 2.4f), Sodium, 12f, 1.4f); mk.Box(GlowM(warm, 1.2f), fo.P(-1.5f, H - .06f, 0), new Vector3(1.4f, .05f, .25f), f.r);
            // two tables with benches; a counter at the end with the urn, the radio, a fridge; a calendar that stopped
            for (var k = 0; k < 2; k++)
            {
                var tx = -3.6f + k * 3f; mk.Box(WorldMaterials.Solid(new Color(.85f, .82f, .75f)), fo.P(tx, .74f, -.4f), new Vector3(1.8f, .06f, .9f), f.r); foreach (var e in new[] { -1f, 1f }) mk.Prism(steel, fo.P(tx + e * .7f, 0, -.4f), up, .04f, .74f, 5); Col(fo.P(tx, .4f, -.4f), new Vector3(1.8f, .8f, .9f), f.r);
                foreach (var e in new[] { -1f, 1f }) { mk.Box(wood, fo.P(tx, .44f, -.4f + e * .85f), new Vector3(1.8f, .06f, .35f), f.r); foreach (var q in new[] { -1f, 1f }) mk.Prism(steel, fo.P(tx + q * .75f, 0, -.4f + e * .85f), up, .03f, .44f, 5); Interact.Bench(plife.transform, fo.P(tx - .5f, .5f, -.4f + e * .85f), fo.P(tx + .5f, .5f, -.4f + e * .85f), -f.Fwd * e); }
            }
            Folk(fo.P(-4.1f, .5f, -1.25f), f.Fwd, Resident.Pose.Sit, new[] { "Smoko's ten minutes. This one's been going since the first chorus.", "Somebody keeps writing lyrics on the roster. I'm not saying it's me. It scans, though.", "Tea's stewed, biscuits are soft, radio's stuck. Wouldn't change a thing." }, Crew(Residents.Tool.Mug), null, .5f);
            Folk(fo.P(-.1f, .5f, .45f), -f.Fwd, Resident.Pose.Sit, new[] { "I had frosted tips in 2002. There are no photographs. I've checked.", "My brother burned me a CD every birthday. Wrote the track list in his terrible handwriting. Still got them all.", "You know what holds up? All of it. I won't be taking questions." }, Crew(), null, .5f);
            var ct = new PlaceFrame(fo.P(W / 2f - T - .45f, 0, -.6f), -f.Right); mk.Box(WorldMaterials.Solid(new Color(.55f, .5f, .42f)), ct.P(0, .45f, 0), new Vector3(3f, .9f, .7f), ct.r); Col(ct.P(0, .45f, 0), new Vector3(3f, .9f, .7f), ct.r);
            mk.Prism(steel, ct.P(-1f, .9f, 0), up, .2f, .5f, 10); mk.Prism(dark, ct.P(-1f, 1.4f, 0), up, .21f, .05f, 10); mk.Box(GlowM(new Color(1f, .3f, .2f), 1.4f), ct.P(-1f, 1f, .2f), new Vector3(.05f, .05f, .02f), ct.r);
            var rm = new MeshKit(); rm.Box(WorldMaterials.Solid(new Color(.3f, .3f, .32f)), new Vector3(0, .14f, 0), new Vector3(.5f, .28f, .16f), Quaternion.identity); var dial = new Material(GlowM(Sodium, 1.2f)) { name = "Crib radio dial" }; rm.Box(dial, new Vector3(.08f, .19f, .085f), new Vector3(.26f, .08f, .01f), Quaternion.identity); rm.Prism(dark, new Vector3(-.15f, .14f, .08f), Vector3.forward, .08f, .01f, 10); rm.Prism(steel, new Vector3(.2f, .28f, 0), new Vector3(.3f, 1f, 0).normalized, .008f, .5f, 4); var rgo = rm.BuildSingle("Crib radio", plife.transform, false); rgo.transform.SetPositionAndRotation(ct.P(.1f, .9f, 0), ct.r); plife.dialM = dial;
            string[] radio = { "Static... then the one station. \"...and that was number one this week in 2003. Again.\"", "\"Traffic and weather together: there isn't any. Here's a power ballad.\"", "\"Caller, you're on the air.\" \"Yeah, can you play the one that goes WHOA-oh?\" \"Mate, that's all of them.\"", "You turn the dial all the way round. It's the same song on every number." }; var rn = 0;
            Interact.Toy(rgo, Toy.Kind.Custom, "Turn the dial", new Vector3(.9f, .8f, .8f)).onUse = _ => { plife.tuned = Time.time; Say(plife.transform, ct.P(.1f, 2.1f, 0), radio[rn++ % radio.Length], 5.5f); };
            mk.Box(PaleM(), fo.P(W / 2f - T - .45f, .9f, 1.6f), new Vector3(.7f, 1.8f, .7f), f.r); Col(fo.P(W / 2f - T - .45f, .9f, 1.6f), new Vector3(.7f, 1.8f, .7f), f.r);
            mk.Box(PaleM(), fo.P(-2f, 1.7f, -D / 2f + T + .02f), new Vector3(.7f, .9f, .02f), f.r); ArtistBits.Label(ctx.Root, "AUGUST\n2003", fo.P(-2f, 1.75f, -D / 2f + T + .04f), f.Fwd, .12f, new Color(.75f, .15f, .12f), TextAnchor.MiddleCenter); mk.Box(WorldMaterials.Solid(new Color(.3f, .4f, .3f)), fo.P(1.2f, 1.7f, -D / 2f + T + .02f), new Vector3(1.6f, 1f, .02f), f.r); ArtistBits.Label(ctx.Root, "ROSTER\nMon: rocks\nTue: rocks\nWed: feelings\nThu: blast\nFri: rocks", fo.P(1.2f, 1.7f, -D / 2f + T + .04f), f.Fwd, .07f, new Color(.95f, .95f, .9f), TextAnchor.MiddleCenter);
            // hard hats on hooks by the door to carry off, lunch boxes on the table
            for (var k = 0; k < 3; k++) { var m = new MeshKit(); var hc = WorldMaterials.Solid(HardHats[k], .4f); m.Blob(hc, new Vector3(0, .07f, 0), new Vector3(.15f, .1f, .17f), prng, 0f); m.Box(hc, new Vector3(0, .02f, .1f), new Vector3(.26f, .02f, .16f), Quaternion.identity); var go = m.BuildSingle("Hard hat", plife.transform, false); go.transform.SetPositionAndRotation(fo.P(dx + Door / 2f + .5f + k * .45f, 1.5f, D / 2f - T - .22f), f.r * Quaternion.Euler(0, 180f, 0)); var c = Interact.Carry(go, "a hard hat (must be worn)", .3f, g.y - 30f); var bc = c.GetComponent<BoxCollider>(); if (bc != null) { bc.size = new Vector3(.3f, .18f, .34f); bc.center = new Vector3(0, .08f, 0); } var rb = go.GetComponent<Rigidbody>(); if (rb != null) rb.isKinematic = true; mk.Box(steel, fo.P(dx + Door / 2f + .5f + k * .45f, 1.46f, D / 2f - T - .06f), new Vector3(.04f, .04f, .14f), f.r); }
            for (var k = 0; k < 3; k++) { var m = new MeshKit(); m.Box(WorldMaterials.Solid(k == 0 ? new Color(.2f, .4f, .75f) : k == 1 ? new Color(.75f, .2f, .2f) : new Color(.3f, .3f, .32f)), new Vector3(0, .09f, 0), new Vector3(.28f, .18f, .18f), Quaternion.identity); m.Box(steel, new Vector3(0, .2f, 0), new Vector3(.14f, .03f, .03f), Quaternion.identity); var go = m.BuildSingle("Lunch box", plife.transform, false); go.transform.SetPositionAndRotation(fo.P(-3.9f + k * 1.6f, .78f, -.4f), f.r * Quaternion.Euler(0, k * 25f, 0)); var c = Interact.Carry(go, "somebody's lunch box", .4f, g.y - 30f); var bc = c.GetComponent<BoxCollider>(); if (bc != null) { bc.size = new Vector3(.28f, .22f, .18f); bc.center = new Vector3(0, .1f, 0); } }
            // outside: a bench against the wall, a drum fire, someone on a break
            PlankBench(mk, f.P(-2.5f, 0, D / 2f + 1f), f.Fwd, 2.6f); FireDrum(mk, G(f.P(-6.5f, 0, D / 2f + 2.6f)));
            Folk(G(f.P(5.2f, 0, D / 2f + 1.2f)), f.Fwd, Resident.Pose.Lean, new[] { "I come out here for the quiet. Then I miss the radio and go back in.", "You can hear the Lot from here when the wind's right. Somebody's always got it turned up." }, Crew(Residents.Tool.Mug));
            SiteSign(mk, f.P(8f, 0, D / 2f + 7f), f.Fwd, "THE CRIB\ncrew room  ·  tea's on", .19f);
            Spur(ctx, mk, f.P(dx, 0, D / 2f + 2f)); Ring(g, 9.5f);
            Shot(ctx, "the crib", f.P(-7f, 2.8f, D / 2f + 10f), fo.P(0, 1.6f, 0));
            Shot(ctx, "inside the crib", fo.P(3.6f, 1.6f, 1.4f), fo.P(-3f, 1f, -.6f));
        }
    }

    /// The Quarry places' moving parts: the blast, the car stereo, the Blue Hole's water, the Crib's radio.
    public class QuarryPlacesLife : MonoBehaviour
    {
        public Transform plunger, dust, tube; public Vector3 plungerUp, blastAt, tubeAt; public Material sirenM, dashM, stereoHeads, waterM, dialM; public Light sirenLight, flash, stereoLight;
        public List<Carryable> rocks = new List<Carryable>(); public List<Material> headlights = new List<Material>(); public float stereo = -99f, splash = -99f, tuned = -99f; float fired = -99f; bool gone = true;
        static readonly Color Red = new Color(1f, .15f, .1f), Head = new Color(1f, .95f, .8f), Dash = new Color(.3f, .9f, 1f), Water = new Color(.15f, .8f, .85f);

        /// Starts the countdown. False if one is already running.
        public bool Fire() { if (Time.time - fired < 9f) return false; fired = Time.time; gone = false; return true; }

        void Update()
        {
            var t = Time.time; var a = t - fired;
            if (plunger != null) plunger.position = plungerUp - Vector3.up * (a < 6f ? .28f : .28f * Mathf.Clamp01(1f - (a - 6f)));
            var arming = a < 3f; if (sirenM != null) sirenM.SetColor("_EmissionColor", Red * (arming ? 1.5f + 1.5f * Mathf.Sin(t * 14f) : .4f)); if (sirenLight != null) sirenLight.intensity = arming ? 2f + 2f * Mathf.Sin(t * 14f) : 0f;
            if (!gone && a >= 3f)
            {
                gone = true; var rng = new System.Random((int)(t * 10f));
                foreach (var r in rocks) { if (r == null || r.Held) continue; var rb = r.GetComponent<Rigidbody>(); if (rb == null) continue; rb.isKinematic = false; if ((r.transform.position - blastAt).sqrMagnitude > 14f * 14f) continue; rb.linearVelocity = new Vector3((float)rng.NextDouble() * 8f - 4f, 9f + (float)rng.NextDouble() * 7f, (float)rng.NextDouble() * 8f - 4f); rb.angularVelocity = new Vector3(5f, 3f, 7f); }
            }
            var b = a - 3f; if (flash != null) flash.intensity = b > 0f && b < 1.2f ? 14f * Mathf.Exp(-b * 5f) : 0f;
            if (dust != null) { var on = b > 0f && b < 5f; if (dust.gameObject.activeSelf != on) dust.gameObject.SetActive(on); if (on) { dust.position = blastAt + Vector3.up * b * 1.6f; dust.localScale = Vector3.one * (.4f + b * .5f); } }
            var s = t - stereo < 40f; if (dashM != null) dashM.SetColor("_EmissionColor", Dash * (s ? 1.2f + .5f * Mathf.Abs(Mathf.Sin(t * 5f)) : .2f)); if (stereoHeads != null) stereoHeads.SetColor("_EmissionColor", Head * (s ? 1.8f : .16f)); if (stereoLight != null) stereoLight.intensity = s ? 1.8f : 0f;
            if (waterM != null) waterM.SetColor("_EmissionColor", Water * (.36f + .05f * Mathf.Sin(t * .8f) + (t - splash < 3f ? .25f * (1f - (t - splash) / 3f) : 0f)));
            if (dialM != null) dialM.SetColor("_EmissionColor", QuarryKit.Sodium * (t - tuned < 6f ? 1.6f + .8f * Mathf.Sin(t * 20f) : .8f));
        }
    }
}
