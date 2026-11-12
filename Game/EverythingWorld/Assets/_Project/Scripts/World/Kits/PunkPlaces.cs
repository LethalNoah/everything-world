using System.Collections.Generic;
using UnityEngine;
using static EverythingWorld.World.PlaceTools;

namespace EverythingWorld.World
{
    /// The Punk Badlands laid out on purpose (the build-land standard). The cul-de-sac, the pool and every driveway
    /// stay where they were; four more cracked sidewalks under streetlights run out to the empty east, north,
    /// south-west and west. Along them, with the kids who never left: Badlands High's gym on prom night (a walk-in
    /// gym: streamers, a mirror ball, a punch table, bleachers, basketballs), Sal's Pizza (a walk-in parlour with
    /// booths, a jukebox and boxes of pizza to carry off), the TP'd House (a neat house buried in toilet paper,
    /// rolls to throw, its owner on the lawn with a hose) with the Legal Wall beside it (spray it and tags appear),
    /// and the Water Tower (stairs up to the catwalk, somewhere to sit with your legs over the edge).
    public partial class PunkKit
    {
        static readonly Vector2 GymAt = new Vector2(7540, 8332), PizzaAt = new Vector2(7318, 8560), HouseAt = new Vector2(7010, 7940), TowerAt = new Vector2(6922, 8200);
        static readonly Vector2[] EastWalk = { new Vector2(7420, 8250), new Vector2(7450, 8275), new Vector2(7480, 8300), new Vector2(7512, 8320) };
        static readonly Vector2[] NorthWalk = { new Vector2(7230, 8520), new Vector2(7255, 8532), new Vector2(7280, 8545), new Vector2(7300, 8553) };
        static readonly Vector2[] SouthWalk = { new Vector2(7050, 8200), new Vector2(7055, 8150), new Vector2(7060, 8100), new Vector2(7045, 8055), new Vector2(7030, 8010), new Vector2(7016, 7966) };
        static readonly Vector2[] WestWalk = { new Vector2(7010, 8235), new Vector2(6980, 8225), new Vector2(6955, 8215), new Vector2(6940, 8207) };
        static readonly (Vector2 at, float r)[] PlaceRings = { (GymAt, 15f), (PizzaAt, 9f), (HouseAt, 16f), (TowerAt, 10f) };

        PunkPlacesLife plife; System.Random prng; Transform rootT; LandContext ctx0; readonly List<Vector3> laneP = new List<Vector3>();

        static readonly Color[] Tees = { new Color(.1f, .1f, .12f), new Color(.85f, .2f, .25f), new Color(.95f, .95f, .93f), new Color(.2f, .2f, .22f), new Color(.3f, .75f, 1f), new Color(.55f, 1f, .3f), new Color(1f, .82f, .25f) };
        static readonly Color[] Shorts = { new Color(.25f, .3f, .45f), new Color(.15f, .15f, .17f), new Color(.6f, .55f, .4f), new Color(.5f, .15f, .15f) };
        static readonly Color[] Dye = { new Color(.15f, .1f, .08f), new Color(.9f, .85f, .3f), new Color(1f, .3f, .55f), new Color(.3f, .75f, 1f), new Color(.55f, 1f, .3f), new Color(.35f, .22f, .12f), new Color(.1f, .1f, .12f) };
        static readonly string[] KidTalk =
        {
            "It's been noon on the last day of school for about nine years. Nobody's complaining.", "My mom thinks I'm at Kyle's. Kyle's mom thinks he's at mine. We're both sixteen forever.", "We started a band. We've got a name, a logo and one and a half songs.",
            "I'm getting out of this town. Tomorrow. Probably tomorrow.", "Don't skate the big pool if Tony's there. He's not mean. He's just really good and it's depressing.",
        };

        Residents.Look Kid(Residents.Tool tool = Residents.Tool.None, bool apron = false) { var l = Residents.Random(prng, Tees, Shorts, new[] { Residents.Hat.None, Residents.Hat.Cap, Residents.Hat.Beanie, Residents.Hat.None }, Tees); l.hair = Dye[prng.Next(Dye.Length)]; l.tool = tool; l.apron = apron; return l; }
        Resident Folk(Vector3 feet, Vector3 face, Resident.Pose pose, string[] lines = null, Residents.Look look = null, Vector3[] route = null, float seatH = HumanScale.SeatHeight)
            => Residents.Make(plife.transform, feet, face, look ?? Kid(), pose, prng, lines ?? KidTalk, route, seatH);
        void Col(Vector3 c, Vector3 size, Quaternion rot) => BoxCol(rootT, c, size, rot);
        Vector3 G(Vector3 p) => ctx0.OnGround(p.x, p.z);
        void Ring(Vector3 at, float r) => noGrass.Add(new Vector3(at.x, r, at.z));

        public override void ShapeGround(IslandWorldData data, int index)
        {
            ShapeBadlands(data, index);   // everything the land already did, unchanged
            foreach (var (at, r) in PlaceRings) Level(data, index, at, r, r + 16f);
        }

        /// A green street sign on a pole, like the town sign. Signs on the +x side of a place's walk; the photo spot on the −x side.
        void StreetSign(MeshKit mk, Vector3 at, Vector3 face, string text, float size = .2f)
        {
            var up = Vector3.up; var g = G(at); var rot = Quaternion.LookRotation(face); var green = WorldMaterials.Solid(new Color(.15f, .45f, .25f));
            foreach (var e in new[] { -1.3f, 1.3f }) mk.Prism(metal, g + Vector3.Cross(up, face) * e - up * .2f, up, .06f, 3.2f, 6);
            mk.Box(green, g + up * 2.4f, new Vector3(3.4f, 1.2f, .08f), rot); mk.Box(white, g + up * 2.4f - face * .01f, new Vector3(3.5f, 1.3f, .05f), rot); Col(g + up * 1.6f, new Vector3(3.4f, 3.2f, .16f), rot);
            ArtistBits.Label(rootT, text, g + up * 2.4f + face * .06f, face, size, new Color(.96f, .96f, .93f), TextAnchor.MiddleCenter);
        }

        void SitCouch(MeshKit mk, Vector3 at, Vector3 face, int i)
        {
            var g = G(at); Couch(ctx0, mk, g, face, i); var side = Vector3.Cross(Vector3.up, face);
            Interact.Bench(plife.transform, g + Vector3.up * .58f - side * .5f + face * .1f, g + Vector3.up * .58f + side * .5f + face * .1f, face);
        }

        List<Vector3> Sidewalk(LandContext ctx, MeshKit mk, Vector2[] way)
        {
            var full = new List<Vector2>(); Vector2 near = way[0]; var bd = 260f * 260f;
            foreach (var (p, w) in walkPoints) { if (w >= 400) continue; var d = (p.x - way[0].x) * (p.x - way[0].x) + (p.z - way[0].y) * (p.z - way[0].y); if (d < bd) { bd = d; near = new Vector2(p.x, p.z); } }
            full.Add(near); full.AddRange(way);
            var pts = PlaceTools.Smooth(ctx, full, false, 2f);
            for (var i = 0; i < pts.Count; i++) { foreach (var a in ctx.Artists) { var r = Foot(a) + 6f; var v = new Vector2(pts[i].x - a.x, pts[i].z - a.z); var d = v.magnitude; if (d < r) { v = d > .01f ? v / d * r : new Vector2(r, 0); pts[i] = ctx.OnGround(a.x + v.x, a.z + v.y); } } }
            if (pts.Count < 2) return pts;
            Strip(mk, pts, concrete, PathW + .4f, .15f, ctx); foreach (var p in pts) Remember(p, 700);
            var up = Vector3.up;
            for (var i = 6; i < pts.Count; i += 11)
            {
                var d = pts[Mathf.Min(i + 1, pts.Count - 1)] - pts[i - 1]; d.y = 0; if (d.sqrMagnitude < .01f) continue; d.Normalize(); var s = (i / 11) % 2 == 0 ? 1f : -1f; var side = Vector3.Cross(up, d) * s;
                var lp = pts[i] + side * (PathW / 2f + .9f); if (InNoGrass(lp.x, lp.z, 1f) || InFootprint(ctx, lp.x, lp.z, 1f) || !ctx.IsLand(lp.x, lp.z, 2f)) continue; var g = ctx.OnGround(lp.x, lp.z);
                mk.Prism(metal, g - up * .2f, up, .09f, 5.6f, 6); mk.Box(metal, g + up * 5.4f - side * .6f, new Vector3(.08f, .08f, 1.2f), Quaternion.LookRotation(side)); mk.Box(GlowM(new Color(1f, .95f, .8f), 1.4f), g + up * 5.3f - side * 1.1f, new Vector3(.4f, .1f, .3f), Quaternion.LookRotation(side));
                ArtistBits.PostCollider(ctx.Root, g, 5.4f, .12f); life.streetlights.Add(g + up * 5.3f - side * 1.1f);
                // every other light has something left under it: a couch, a cart, a mailbox
                var k = i / 11; var op = pts[i] - side * (PathW / 2f + 2f); if (InNoGrass(op.x, op.z, 2f) || InFootprint(ctx, op.x, op.z, 2f) || !ctx.IsLand(op.x, op.z, 3f)) continue;
                if (k % 3 == 0) SitCouch(mk, op, side, k); else if (k % 3 == 1) Mailbox(ctx, mk, G(op - side * .8f), k); else Cart(ctx, mk, G(op), k * 50f, k % 2 == 0);
            }
            laneP.AddRange(pts); return pts;
        }

        void Drive(LandContext ctx, MeshKit mk, Vector3 to)
        {
            var from = Nearest(laneP, to); var d = new Vector2(to.x - from.x, to.z - from.z); var len = d.magnitude; if (len < 4f) return; d /= len;
            var a = new Vector2(from.x, from.z) + d * 1.7f; var b = new Vector2(to.x, to.z);
            var pts = PlaceTools.Smooth(ctx, new[] { a, Vector2.Lerp(a, b, .5f), b }, false, 2f, 1f); if (pts.Count < 2) return;
            Strip(mk, pts, concrete, 2.4f, .14f, ctx); foreach (var p in pts) Remember(p, 900);
        }

        void BadlandsPlaces(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            laneP.Clear(); rootT = ctx.Root; ctx0 = ctx; prng = new System.Random(1994);
            plife = new GameObject("Punk places life").AddComponent<PunkPlacesLife>(); plife.transform.SetParent(life.transform, false);
            foreach (var (at, r) in PlaceRings) noGrass.Add(new Vector3(at.x, r, at.y));
            Sidewalk(ctx, mk, EastWalk); Sidewalk(ctx, mk, NorthWalk); Sidewalk(ctx, mk, SouthWalk); Sidewalk(ctx, mk, WestWalk);
            Gym(ctx, mk, solid); Pizza(ctx, mk, solid); TpHouse(ctx, mk, solid); WaterTower(ctx, mk, solid);
        }

        // ---------------------------------------------------------------- Badlands High: the gym on prom night
        void Gym(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var g = ctx.OnGround(GymAt.x, GymAt.y); var f = new PlaceFrame(g, FaceTo(g, Nearest(laneP, g))); const float W = 14f, D = 22f, H = 6f, T = .4f, Door = 2.6f;
            var fo = new PlaceFrame(g + up * .25f, f.Fwd); var brick = WorldMaterials.Solid(new Color(.72f, .4f, .3f)); var floor = WorldMaterials.Solid(new Color(.85f, .65f, .4f)); var line = WorldMaterials.Solid(new Color(.9f, .25f, .2f));
            mk.Box(concrete, g - up * .35f, new Vector3(W + 3f, 1.2f, D + 6f), f.r); solid.Box(concrete, g - up * .35f, new Vector3(W + 3f, 1.2f, D + 6f), f.r);
            void Wall(Vector3 p, Vector3 s) { mk.Box(brick, p, s, f.r); solid.Box(brick, p, s, f.r); }
            mk.Box(floor, fo.P(0, -.03f, 0), new Vector3(W - T, .1f, D - T), f.r);
            Wall(fo.P(0, H / 2f, -D / 2f + T / 2f), new Vector3(W, H, T)); foreach (var e in new[] { -1f, 1f }) Wall(fo.P(e * (W / 2f - T / 2f), H / 2f, 0), new Vector3(T, H, D - T * 2f));
            var seg = (W - Door) / 2f; foreach (var e in new[] { -1f, 1f }) Wall(fo.P(e * (Door / 2f + seg / 2f), H / 2f, D / 2f - T / 2f), new Vector3(seg, H, T)); Wall(fo.P(0, (H + HumanScale.DoorHeight + .3f) / 2f, D / 2f - T / 2f), new Vector3(Door, H - HumanScale.DoorHeight - .3f, T));
            mk.Box(concreteDark, fo.P(0, H + .15f, 0), new Vector3(W + .5f, .3f, D + .5f), f.r); solid.Box(concreteDark, fo.P(0, H + .15f, 0), new Vector3(W + .5f, .3f, D + .5f), f.r);
            for (var k = 0; k < 5; k++) foreach (var e in new[] { -1f, 1f }) mk.Box(GlowM(new Color(1f, .8f, .9f), .8f), fo.P(e * (W / 2f + .02f), 4.4f, -D / 2f + 3f + k * 4f), new Vector3(.05f, 1.2f, 2.4f), f.r);
            ArtistBits.Label(ctx.Root, "BADLANDS HIGH\nGO TUMBLEWEEDS", fo.P(0, 4.6f, D / 2f + .03f), f.Fwd, .34f, new Color(.96f, .93f, .85f), TextAnchor.MiddleCenter);
            IndoorSpaces.Add(fo.P(0, H / 2f, 0), f.r, new Vector3(W - 1f, H, D - 1f));
            // the court under it all: centre circle, the keys, a hoop each end
            mk.Prism(line, fo.P(0, .03f, 0), up, 1.8f, .01f, 20); mk.Prism(floor, fo.P(0, .035f, 0), up, 1.65f, .01f, 20); mk.Box(line, fo.P(0, .03f, 0), new Vector3(W - 1f, .01f, .12f), f.r);
            foreach (var e in new[] { -1f, 1f }) { mk.Box(WorldMaterials.Solid(new Color(.3f, .45f, .75f)), fo.P(0, .03f, e * (D / 2f - 3.4f)), new Vector3(3.6f, .01f, 5.4f), f.r); var hp = fo.P(0, 0, e * (D / 2f - 1.1f)); if (e < 0) { mk.Box(white, hp + up * 3.9f, new Vector3(1.8f, 1.1f, .06f), f.r); mk.Box(line, hp + up * 3.75f + f.Fwd * .04f, new Vector3(.6f, .45f, .02f), f.r); for (var q = 0; q < 10; q++) { var an = q * Mathf.PI / 5f; mk.Box(line, hp + up * 3.4f + f.Fwd * .32f + f.Right * Mathf.Cos(an) * .23f + f.Fwd * Mathf.Sin(an) * .23f, new Vector3(.04f, .03f, .15f), f.r * Quaternion.Euler(0, -q * 36f, 0)); } } }
            // streamers and balloons, a mirror ball, coloured light
            for (var k = 0; k < 12; k++) { var c = Spray[k % Spray.Length]; var z = -D / 2f + 1.5f + k * (D - 3f) / 11f; for (var s = 0; s < 6; s++) { var x0 = -W / 2f + .6f + s * (W - 1.2f) / 6f; var sag = Mathf.Sin((s + .5f) / 6f * Mathf.PI) * 1f; mk.Box(WorldMaterials.Glow(c, .35f), fo.P(x0 + (W - 1.2f) / 12f, H - .4f - sag, z), new Vector3((W - 1.2f) / 6f + .05f, .02f, .16f), f.r * Quaternion.Euler(0, 0, (s < 3 ? -1f : 1f) * (10f - Mathf.Abs(s - 2.5f) * 3f))); } }
            for (var k = 0; k < 14; k++) { var a = k / 13f * Mathf.PI; var bp = fo.P(Mathf.Cos(a) * (Door / 2f + .5f), .3f + Mathf.Sin(a) * 3f, D / 2f - T - .5f); mk.Blob(WorldMaterials.Glow(Spray[k % Spray.Length], .5f), bp, Vector3.one * .28f, prng, .05f); }
            var ball = new MeshKit(); ball.Blob(WorldMaterials.Glow(new Color(.9f, .92f, 1f), 1.4f), Vector3.zero, Vector3.one * .45f, prng, .12f); ball.Prism(metal, new Vector3(0, .4f, 0), Vector3.up, .02f, 1f, 4); var bgo = ball.BuildSingle("Prom mirror ball", plife.transform, false); bgo.transform.position = fo.P(0, H - 1.5f, 0); plife.ball = bgo.transform; halos.Add(fo.P(0, H - 1.5f, 0), 1.4f, new Color(.9f, .9f, 1f), .35f);
            for (var k = 0; k < 4; k++) plife.promLights.Add(ArtistBits.PointLight(ctx.Root, fo.P(k % 2 == 0 ? -3.5f : 3.5f, 4.4f, k < 2 ? -5.5f : 5.5f), Spray[k], 14f, 2.6f, true));
            // the stage at the far end with the banner and the DJ; bleachers down one side; the punch table on the other
            mk.Box(woodDark, fo.P(0, .4f, -D / 2f + 2.2f), new Vector3(W - 2f, .8f, 3.4f), f.r); solid.Box(woodDark, fo.P(0, .4f, -D / 2f + 2.2f), new Vector3(W - 2f, .8f, 3.4f), f.r); for (var k = 0; k < 2; k++) { mk.Box(woodDark, fo.P(-W / 2f + 2.2f, .2f + k * .0f, -D / 2f + 4.1f + k * .3f), new Vector3(1.6f, .4f, .4f), f.r); solid.Box(woodDark, fo.P(-W / 2f + 2.2f, .2f, -D / 2f + 4.1f), new Vector3(1.6f, .4f, .4f), f.r); }
            mk.Box(white, fo.P(0, 4.3f, -D / 2f + T + .05f), new Vector3(9f, 1.3f, .04f), f.r); ArtistBits.Label(ctx.Root, "PROM NIGHT (AGAIN)\nCLASS OF FOREVER", fo.P(0, 4.3f, -D / 2f + T + .09f), f.Fwd, .36f, new Color(.85f, .2f, .45f), TextAnchor.MiddleCenter);
            mk.Box(dark, fo.P(0, 1.25f, -D / 2f + 2.4f), new Vector3(2.2f, .9f, .8f), f.r); Col(fo.P(0, 1.25f, -D / 2f + 2.4f), new Vector3(2.2f, .9f, .8f), f.r); foreach (var e in new[] { -1f, 1f }) { mk.Prism(dark, fo.P(e * .6f, 1.71f, -D / 2f + 2.4f), up, .3f, .03f, 14); mk.Box(dark, fo.P(e * 3.4f, 1.6f, -D / 2f + 1.6f), new Vector3(1f, 1.6f, .8f), f.r); mk.Prism(GlowM(Spray[e > 0 ? 0 : 2], 1.4f), fo.P(e * 3.4f, 1.8f, -D / 2f + 2.01f), f.Fwd, .3f, .03f, 12); Col(fo.P(e * 3.4f, 1.6f, -D / 2f + 1.6f), new Vector3(1f, 1.6f, .8f), f.r); }
            var dj = Kid(); dj.hat = Residents.Hat.Cap; Folk(fo.P(0, .8f, -D / 2f + 1.6f), f.Fwd, Resident.Pose.Work, new[] { "Next up: the slow one. Find somebody. Or stand by the punch like a normal person.", "I take requests. I play the same eleven songs. Both are true.", "Last song of the night! ...It's been the last song of the night since nine." }, dj).workRate = .7f;
            for (var row = 0; row < 3; row++) { var bc = fo.P(W / 2f - T - .5f - row * .7f, .25f + (2 - row) * .4f, 2f); mk.Box(wood, bc + up * ((2 - row) * .0f), new Vector3(.7f, .1f, 9f), f.r); solid.Box(wood, fo.P(W / 2f - T - .5f - row * .7f, (.3f + (2 - row) * .4f) / 2f, 2f), new Vector3(.7f, .3f + (2 - row) * .4f, 9f), f.r); if (row > 0) Interact.Bench(plife.transform, bc + up * .1f - f.Fwd * 3.5f, bc + up * .1f + f.Fwd * 3.5f, -f.Right); }
            Folk(fo.P(W / 2f - T - 1.9f, .35f, 4f), -f.Right, Resident.Pose.Sit, new[] { "I'm not not dancing. I'm waiting for the right song.", "I rented this tux. It's nine years overdue.", "She's over by the punch. I've been about to walk over there for ages." }, Kid(), null, .35f);
            var tb = new PlaceFrame(fo.P(-W / 2f + T + .8f, 0, 3f), f.Right); mk.Box(white, tb.P(0, .45f, 0), new Vector3(3f, .9f, .9f), tb.r); Col(tb.P(0, .45f, 0), new Vector3(3f, .9f, .9f), tb.r); mk.Prism(WorldMaterials.Glow(new Color(1f, .35f, .5f), .6f), tb.P(-.8f, .92f, 0), up, .32f, .22f, 12, 1.25f);
            for (var k = 0; k < 5; k++) { var m = new MeshKit(); m.Prism(WorldMaterials.Solid(new Color(.9f, .2f, .2f)), Vector3.zero, Vector3.up, .04f, .11f, 8, 1.3f); var go = m.BuildSingle("Punch cup", plife.transform, false); go.transform.position = tb.P(-.1f + k * .28f, .92f, .1f * (k % 2)); var c = Interact.Carry(go, "a cup of punch (somebody's definitely spiked it)", .15f, g.y - 12f); var bc = c.GetComponent<BoxCollider>(); if (bc != null) { bc.size = new Vector3(.1f, .12f, .1f); bc.center = new Vector3(0, .055f, 0); } }
            Folk(tb.P(.6f, 0, 1.1f), -tb.Fwd, Resident.Pose.Stand, new[] { "The punch is red. That's all anyone knows about it.", "He's over on the bleachers. He's been about to walk over here for ages.", "My dress took four months. This song is three minutes. Worth it." }, Kid());
            var teacher = Kid(Residents.Tool.Mug); teacher.top = new Color(.6f, .55f, .4f); teacher.bottom = new Color(.3f, .28f, .25f); teacher.hat = Residents.Hat.None; teacher.hair = new Color(.6f, .6f, .6f); Folk(fo.P(-W / 2f + T + .5f, 0, D / 2f - 2.2f), f.Right, Resident.Pose.Lean, new[] { "Leave room for the Holy Spirit. And for me, I'm walking through.", "I chaperoned this prom in '94. It's the same prom. I've checked.", "I know about the punch. I've decided not to know about the punch." }, teacher);
            for (var k = 0; k < 3; k++) { var m = new MeshKit(); m.Blob(WorldMaterials.Solid(new Color(.9f, .45f, .15f)), Vector3.zero, Vector3.one * .12f, prng, 0f); m.Box(dark, Vector3.zero, new Vector3(.245f, .01f, .245f), Quaternion.identity); var go = m.BuildSingle("Basketball", plife.transform, false); go.transform.position = fo.P(2.4f + k * .5f, .2f, D / 2f - 2.2f - k * .3f); var c = Interact.Carry(go, "a basketball", .5f, g.y - 12f); var bc = c.GetComponent<BoxCollider>(); if (bc != null) { bc.size = Vector3.one * .24f; bc.center = Vector3.zero; } }
            Interact.Toy(bgo, Toy.Kind.Custom, "Spin the mirror ball", new Vector3(1.6f, 5f, 1.6f)).onUse = _ => { plife.spun = Time.time; Say(plife.transform, fo.P(0, 2.6f, 0), "The whole gym turns to little squares of light. Somebody whoops.", 3.5f); };
            // outside: a flagpole, a bike rack, the bus that never leaves
            var fp = G(f.P(-5f, 0, D / 2f + 5f)); mk.Prism(metal, fp, up, .07f, 8f, 6); mk.Box(WorldMaterials.Solid(new Color(.85f, .2f, .25f)), fp + up * 7.3f + f.Right * .9f, new Vector3(1.8f, 1.1f, .03f), f.r); ArtistBits.PostCollider(ctx.Root, fp, 8f, .1f);
            var bus = new PlaceFrame(G(f.P(W / 2f + 4.5f, 0, 2f)), f.Fwd); var yellow = WorldMaterials.Solid(new Color(.98f, .75f, .15f)); mk.Box(yellow, bus.P(0, 1.6f, 0), new Vector3(2.5f, 2.3f, 9f), bus.r); mk.Box(yellow, bus.P(0, 1.1f, 5.1f), new Vector3(2.3f, 1.3f, 1.4f), bus.r); mk.Box(dark, bus.P(0, 1.3f, 0), new Vector3(2.54f, .12f, 9.02f), bus.r); mk.Box(GlowM(new Color(.7f, .85f, 1f), .5f), bus.P(0, 2.1f, 0), new Vector3(2.54f, .7f, 8f), bus.r); foreach (var (x, z) in new[] { (-1.2f, -3f), (1.2f, -3f), (-1.2f, 3.6f), (1.2f, 3.6f) }) mk.Prism(dark, bus.P(x - Mathf.Sign(x) * .15f, .5f, z), bus.Right * Mathf.Sign(x), .5f, .3f, 10); Col(bus.P(0, 1.4f, .6f), new Vector3(2.5f, 2.8f, 10.4f), bus.r); ArtistBits.Label(ctx.Root, "SCHOOL BUS", bus.P(-1.28f, 2.62f, 0), -bus.Right, .2f, new Color(.1f, .1f, .12f), TextAnchor.MiddleCenter);
            ArtistBits.PointLight(ctx.Root, f.P(0, 3.4f, D / 2f + 2.5f), new Color(1f, .7f, .85f), 14f, 1.6f);
            StreetSign(mk, f.P(7f, 0, D / 2f + 8f), f.Fwd, "BADLANDS HIGH\nprom tonight (every night)", .17f);
            Drive(ctx, mk, f.P(0, 0, D / 2f + 2.6f)); Ring(g, 15f);
            Shot(ctx, "the gym on prom night", f.P(-9f, 3.6f, D / 2f + 13f), fo.P(0, 3f, 0));
            Shot(ctx, "inside the prom", fo.P(3.5f, 1.7f, D / 2f - 1.6f), fo.P(-1f, 2.2f, -D / 2f + 2f));
        }

        // ---------------------------------------------------------------- Sal's Pizza
        void Pizza(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var g = ctx.OnGround(PizzaAt.x, PizzaAt.y); var f = new PlaceFrame(g, FaceTo(g, Nearest(laneP, g))); const float W = 10f, D = 8f, H = 3.3f, T = .3f, Door = HumanScale.DoorWidth + .5f;
            var fo = new PlaceFrame(g + up * .25f, f.Fwd); var cream = WorldMaterials.Solid(new Color(.93f, .88f, .75f)); var redM = WorldMaterials.Solid(new Color(.8f, .15f, .15f)); var warm = new Color(1f, .75f, .45f);
            mk.Box(concrete, g - up * .35f, new Vector3(W + 3f, 1.2f, D + 5f), f.r); solid.Box(concrete, g - up * .35f, new Vector3(W + 3f, 1.2f, D + 5f), f.r);
            void Wall(Vector3 p, Vector3 s) { mk.Box(cream, p, s, f.r); solid.Box(cream, p, s, f.r); }
            for (var x = 0; x < 9; x++) for (var z = 0; z < 7; z++) mk.Box((x + z) % 2 == 0 ? redM : white, fo.P(-W / 2f + T + .52f + x * 1.04f, -.02f, -D / 2f + T + .53f + z * 1.06f), new Vector3(1.04f, .08f, 1.06f), f.r);
            Wall(fo.P(0, H / 2f, -D / 2f + T / 2f), new Vector3(W, H, T)); foreach (var e in new[] { -1f, 1f }) Wall(fo.P(e * (W / 2f - T / 2f), H / 2f, 0), new Vector3(T, H, D - T * 2f));
            // front: the door to one side, a big window the other with the name lettered across it
            var dx = -W / 2f + 1.2f + Door / 2f; Wall(fo.P(-W / 2f + .6f, H / 2f, D / 2f - T / 2f), new Vector3(1.2f, H, T)); Wall(fo.P(dx, (H + HumanScale.DoorHeight) / 2f, D / 2f - T / 2f), new Vector3(Door, H - HumanScale.DoorHeight, T)); var wl = dx + Door / 2f; var ww = W / 2f - wl;
            Wall(fo.P(wl + ww / 2f, .45f, D / 2f - T / 2f), new Vector3(ww, .9f, T)); Wall(fo.P(wl + ww / 2f, H - .3f, D / 2f - T / 2f), new Vector3(ww, .6f, T)); Wall(fo.P(W / 2f - .15f, H / 2f, D / 2f - T / 2f), new Vector3(.3f, H, T)); mk.Box(GlowM(warm, .6f), fo.P(wl + ww / 2f - .15f, 1.8f, D / 2f - T / 2f), new Vector3(ww - .3f, 1.8f, .05f), f.r); solid.Box(cream, fo.P(wl + ww / 2f, 1.8f, D / 2f - T / 2f), new Vector3(ww, 1.8f, .1f), f.r);
            mk.Box(redM, fo.P(0, H + .12f, 0), new Vector3(W + .5f, .24f, D + .5f), f.r); solid.Box(redM, fo.P(0, H + .12f, 0), new Vector3(W + .5f, .24f, D + .5f), f.r); for (var s = 0; s < 10; s++) mk.Box(s % 2 == 0 ? redM : white, fo.P(-W / 2f + .5f + s * 1f, H - .25f, D / 2f + .6f), new Vector3(1f, .06f, 1.3f), f.r * Quaternion.Euler(18f, 0, 0));
            // the sign on the roof: a great slice, and SAL'S in bulbs
            var sg = fo.P(0, H + .24f, D / 2f - .6f); mk.Box(dark, sg + up * 1f, new Vector3(6f, 2f, .2f), f.r); ArtistBits.Label(ctx.Root, "SAL'S PIZZA", sg + up * 1.2f + f.Fwd * .12f, f.Fwd, .5f, new Color(1f, .35f, .25f), TextAnchor.MiddleCenter); ArtistBits.Label(ctx.Root, "by the slice  ·  since forever", sg + up * .45f + f.Fwd * .12f, f.Fwd, .16f, new Color(1f, .9f, .6f), TextAnchor.MiddleCenter); foreach (var e in new[] { -1f, 1f }) mk.Box(GlowM(new Color(1f, .3f, .2f), 2.4f), sg + up * (1f + e * .98f) + f.Fwd * .11f, new Vector3(6f, .06f, .04f), f.r); halos.Add(sg + up * 1f + f.Fwd * .5f, 2.6f, new Color(1f, .35f, .25f), .4f);
            IndoorSpaces.Add(fo.P(0, H / 2f, 0), f.r, new Vector3(W - .7f, H, D - .7f)); ArtistBits.PointLight(ctx.Root, fo.P(0, 2.8f, .5f), warm, 12f, 3f, true); ArtistBits.PointLight(ctx.Root, f.P(0, 2.6f, D / 2f + 2.5f), warm, 12f, 1.5f);
            // the counter and the oven along the back; Sal
            mk.Box(redM, fo.P(.8f, .5f, -D / 2f + 2.2f), new Vector3(W - 3f, 1f, .7f), f.r); mk.Box(white, fo.P(.8f, 1.02f, -D / 2f + 2.2f), new Vector3(W - 2.9f, .05f, .8f), f.r); Col(fo.P(.8f, .55f, -D / 2f + 2.2f), new Vector3(W - 2.9f, 1.1f, .8f), f.r);
            mk.Box(WorldMaterials.Solid(new Color(.5f, .3f, .25f)), fo.P(2.6f, 1.2f, -D / 2f + T + .5f), new Vector3(2.6f, 2.4f, 1f), f.r); mk.Box(GlowM(new Color(1f, .5f, .15f), 2.2f), fo.P(2.6f, 1.2f, -D / 2f + T + 1.01f), new Vector3(1.4f, .5f, .04f), f.r); Col(fo.P(2.6f, 1.2f, -D / 2f + T + .5f), new Vector3(2.6f, 2.4f, 1f), f.r); halos.Add(fo.P(2.6f, 1.2f, -D / 2f + T + 1.4f), .9f, new Color(1f, .5f, .15f), .5f);
            var sal = Kid(Residents.Tool.Spatula, true); sal.top = white.color; sal.hat = Residents.Hat.None; sal.hair = new Color(.25f, .2f, .18f); Folk(fo.P(.4f, 0, -D / 2f + 1.3f), f.Fwd, Resident.Pose.Work, new[] { "Two slices and a soda, three bucks. It was three bucks in '94 and it's three bucks now.", "You kids and your skateboards. ...Do a kickflip. Go on. Okay, that was pretty good.", "Somebody ordered forty pies to the water tower. I'm not carrying those up." }, sal).workRate = .8f;
            for (var k = 0; k < 5; k++)
            {
                var m = new MeshKit(); var size = new Vector3(.5f, .07f, .5f);
                if (k < 4) { m.Box(WorldMaterials.Solid(new Color(.92f, .88f, .8f)), new Vector3(0, .03f, 0), new Vector3(.5f, .06f, .5f), Quaternion.identity); m.Prism(redM, new Vector3(0, .061f, 0), Vector3.up, .14f, .004f, 12); }
                else { m.Prism(WorldMaterials.Solid(new Color(.95f, .75f, .3f)), new Vector3(0, 0, 0), Vector3.up, .16f, .03f, 3); m.Blob(redM, new Vector3(.02f, .035f, 0), new Vector3(.04f, .01f, .04f), prng, 0f); size = new Vector3(.28f, .05f, .28f); }
                var go = m.BuildSingle("Pizza", plife.transform, false); go.transform.SetPositionAndRotation(fo.P(-1.6f + (k < 4 ? 0 : 1.2f), 1.05f + (k < 4 ? k * .065f : 0), -D / 2f + 2.2f), f.r * Quaternion.Euler(0, k * 7f, 0)); var c = Interact.Carry(go, k < 4 ? "a pizza box (still warm)" : "a slice", k < 4 ? .4f : .15f, g.y - 12f); var bc = c.GetComponent<BoxCollider>(); if (bc != null) { bc.size = size; bc.center = new Vector3(0, size.y / 2f, 0); } var rb = go.GetComponent<Rigidbody>(); if (rb != null) rb.isKinematic = true;
            }
            // two booths under the window, a jukebox by the door
            for (var k = 0; k < 2; k++)
            {
                var bx = wl + 1.6f + k * 3.2f; if (bx > W / 2f - 1.6f) bx = W / 2f - 1.6f; var bz = D / 2f - T - 1.3f;
                mk.Box(wood, fo.P(bx, .75f, bz), new Vector3(1.1f, .06f, 1.6f), f.r); mk.Prism(metal, fo.P(bx, 0, bz), up, .08f, .75f, 6); Col(fo.P(bx, .4f, bz), new Vector3(1.1f, .8f, 1.6f), f.r);
                foreach (var e in new[] { -1f, 1f }) { mk.Box(redM, fo.P(bx + e * 1.05f, .25f, bz), new Vector3(.6f, .5f, 1.6f), f.r); mk.Box(redM, fo.P(bx + e * 1.3f, .8f, bz), new Vector3(.14f, 1.1f, 1.6f), f.r); Col(fo.P(bx + e * 1.1f, .5f, bz), new Vector3(.7f, 1f, 1.6f), f.r); Interact.Bench(plife.transform, fo.P(bx + e * 1f, .52f, bz - .4f), fo.P(bx + e * 1f, .52f, bz + .4f), -f.Right * e); }
                if (k == 0) { Folk(fo.P(bx - 1f, .52f, bz + .4f), f.Right, Resident.Pose.Sit, new[] { "We're writing the setlist. It's one song, twice.", "If you fold the slice you look like you know what you're doing.", "Our drummer's grounded. We're a bold new acoustic direction until Friday." }, Kid(), null, .52f); Folk(fo.P(bx + 1f, .52f, bz - .4f), -f.Right, Resident.Pose.Sit, new[] { "I drew our logo on the napkin. And the table. Sal says I've got to stop.", "This is our booth. It's had our names carved in it since before we were born, which is weird." }, Kid(), null, .52f); }
            }
            var jb = fo.P(-W / 2f + T + .55f, 0, D / 2f - T - 2.6f); var jm = new MeshKit(); jm.Box(WorldMaterials.Solid(new Color(.45f, .2f, .15f)), new Vector3(0, .65f, 0), new Vector3(.8f, 1.3f, .6f), Quaternion.identity); jm.Prism(WorldMaterials.Solid(new Color(.45f, .2f, .15f)), new Vector3(0, 1.3f, -.3f), Vector3.forward, .4f, .6f, 12); var jglow = new Material(GlowM(Spray[0], 1.6f)) { name = "Jukebox glow" }; jm.Box(jglow, new Vector3(0, 1.1f, .31f), new Vector3(.6f, .5f, .02f), Quaternion.identity); jm.Box(GlowM(Sun, 1.2f), new Vector3(0, .5f, .31f), new Vector3(.5f, .4f, .02f), Quaternion.identity);
            var jgo = jm.BuildSingle("Jukebox", plife.transform, false); jgo.transform.SetPositionAndRotation(jb, Quaternion.LookRotation(f.Right)); Col(jb + up * .8f, new Vector3(.7f, 1.6f, .9f), f.r); plife.jukeM = jglow; halos.Add(jb + up * 1.1f + f.Right * .6f, .8f, Spray[0], .4f);
            string[] juke = { "You kick it. It skips to the good part.", "It eats your quarter and plays the song it was going to play anyway.", "Three chords come out. Sal turns it up without looking.", "It plays the slow one. Everyone in the booth pretends not to know the words." }; var jn = 0;
            Interact.Toy(jgo, Toy.Kind.Custom, "Kick the jukebox", new Vector3(1.2f, 1.8f, 1.2f)).onUse = _ => { plife.kicked = Time.time; Say(plife.transform, jb + up * 2.2f, juke[jn++ % juke.Length], 4f); };
            // outside: a picnic table, bikes dumped on the ground, the delivery car
            var pt = G(f.P(4f, 0, D / 2f + 2.6f)); mk.Box(wood, pt + up * .75f, new Vector3(1.8f, .06f, .8f), f.r); foreach (var e in new[] { -1f, 1f }) { mk.Box(wood, pt + up * .45f + f.Fwd * e * .75f, new Vector3(1.8f, .06f, .3f), f.r); Interact.Bench(plife.transform, pt + up * .5f + f.Fwd * e * .75f - f.Right * .5f, pt + up * .5f + f.Fwd * e * .75f + f.Right * .5f, -f.Fwd * e); mk.Box(woodDark, pt + up * .37f + f.Right * e * .7f, new Vector3(.08f, .75f, 1.7f), f.r); } Col(pt + up * .4f, new Vector3(1.8f, .8f, 1.8f), f.r);
            Folk(G(f.P(-4.6f, 0, D / 2f + 1.6f)), f.Fwd, Resident.Pose.Lean, new[] { "I deliver. On a skateboard. It arrives cold and upside down but it arrives.", "Sal's had the same pie in the window since the nineties. It's load-bearing." }, Kid());
            StreetSign(mk, f.P(6.5f, 0, D / 2f + 7f), f.Fwd, "SAL'S PIZZA\n2 slices + soda  $3", .19f);
            Drive(ctx, mk, f.P(dx, 0, D / 2f + 2.2f)); Ring(g, 9f);
            Shot(ctx, "sal's pizza", f.P(-7f, 2.6f, D / 2f + 11f), fo.P(0, 2.4f, 0));
            Shot(ctx, "inside sal's", fo.P(dx, 1.6f, D / 2f - 1f), fo.P(2f, 1f, -D / 2f + 1.5f));
        }

        // ---------------------------------------------------------------- the TP'd House and the Legal Wall
        void TpHouse(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var g = ctx.OnGround(HouseAt.x, HouseAt.y); var f = new PlaceFrame(g, FaceTo(g, Nearest(laneP, g)));
            var lawn = WorldMaterials.Solid(new Color(.35f, .7f, .3f)); var siding = WorldMaterials.Solid(new Color(.75f, .85f, .9f)); var roofM = WorldMaterials.Solid(new Color(.4f, .3f, .3f)); var paper = WorldMaterials.Solid(new Color(.98f, .98f, .96f));
            // the one perfect lawn in the Badlands, a picket fence round it
            mk.Box(lawn, g + up * .02f - f.Right * 3f, new Vector3(20f, .3f, 18f), f.r);
            var hs = new PlaceFrame(g - f.Right * 4f - f.Fwd * 2f + up * .15f, f.Fwd); const float HW = 9f, HD = 7f, HH = 3f;
            mk.Box(siding, hs.P(0, HH / 2f, 0), new Vector3(HW, HH, HD), hs.r); solid.Box(siding, hs.P(0, HH / 2f, 0), new Vector3(HW, HH, HD), hs.r);
            foreach (var e in new[] { -1f, 1f }) mk.Box(roofM, hs.P(0, HH + 1f, e * HD / 4f), new Vector3(HW + .6f, .16f, HD / 2f + 1f), hs.r * Quaternion.Euler(-e * 30f, 0, 0)); foreach (var e in new[] { -1f, 1f }) mk.Prism(siding, hs.P(e * (HW / 2f - .05f) - e * .05f, HH, 0), hs.Right * e, HD / 2f * 1.0f, .1f, 3);
            mk.Box(WorldMaterials.Solid(new Color(.8f, .2f, .2f)), hs.P(0, 1.05f, HD / 2f + .02f), new Vector3(1f, 2.1f, .05f), hs.r); foreach (var e in new[] { -1f, 1f }) { mk.Box(GlowM(new Color(1f, .9f, .6f), .8f), hs.P(e * 2.8f, 1.6f, HD / 2f + .02f), new Vector3(1.5f, 1.2f, .05f), hs.r); mk.Box(white, hs.P(e * 2.8f, 1.6f, HD / 2f + .03f), new Vector3(.06f, 1.2f, .05f), hs.r); }
            mk.Box(concrete, hs.P(0, .1f, HD / 2f + .7f), new Vector3(2f, .2f, 1.4f), hs.r); mk.Box(WorldMaterials.Solid(new Color(.6f, .4f, .3f)), hs.P(2.8f, HH + 1.6f, -1f), new Vector3(.7f, 1.6f, .7f), hs.r);
            // toilet paper over everything: long strips down the roof and the walls, loops in the tree, a few on the lawn
            for (var k = 0; k < 18; k++) { var x = -HW / 2f + .4f + k * (HW - .8f) / 17f; var front = k % 2 == 0 ? 1f : -1f; var len = 1.4f + (k * 7 % 5) * .45f; mk.Box(paper, hs.P(x, HH + 1.05f + (front > 0 ? .0f : .0f), front * HD / 4f), new Vector3(.14f, .03f, HD / 2f + 1.2f), hs.r * Quaternion.Euler(-front * 30f, 0, 0)); mk.Box(paper, hs.P(x, HH + .05f - len / 2f, front * (HD / 2f + .62f)), new Vector3(.14f, len, .02f), hs.r * Quaternion.Euler(0, 0, (k % 3 - 1) * 4f)); }
            var tree = G(g + f.Right * 3.5f + f.Fwd * 3f); mk.Prism(WorldMaterials.Solid(new Color(.4f, .3f, .22f)), tree - up * .2f, up, .3f, 3.4f, 7, .7f); mk.Blob(WorldMaterials.Solid(new Color(.3f, .6f, .3f)), tree + up * 4.6f, new Vector3(2.6f, 2f, 2.6f), prng, .2f); ArtistBits.PostCollider(ctx.Root, tree, 3.4f, .32f);
            for (var k = 0; k < 10; k++) { var a = k * 36f; var d = Quaternion.Euler(0, a, 0) * Vector3.forward; var len = 1.5f + (k % 4) * .5f; mk.Box(paper, tree + up * (4.3f - len / 2f + .8f) + d * (1.6f + (k % 3) * .4f), new Vector3(.14f, len, .02f), Quaternion.Euler(0, a, (k % 3 - 1) * 6f)); mk.Box(paper, tree + up * (5.3f + (k % 2) * .5f) + d * .9f, new Vector3(.14f, .03f, 2.2f), Quaternion.Euler(-20f + k * 4f, a, 0)); }
            for (var k = 0; k < 6; k++) mk.Box(paper, G(g + f.Right * (-6f + k * 2.3f) + f.Fwd * (4f + (k % 3))) + up * .2f, new Vector3(.14f, .02f, 1.8f + k * .3f), Quaternion.Euler(0, k * 50f, 0));
            // picket fence along the front with a gap for the path; flamingos; a sprinkler that turns
            for (var k = 0; k < 40; k++) { var x = -13f + k * .5f; if (Mathf.Abs(x + 4f) < 1f) continue; mk.Box(white, g + f.Right * x + f.Fwd * 7f + up * .55f, new Vector3(.12f, .9f, .04f), f.r); } foreach (var (x0, x1) in new[] { (-13f, -5f), (-3f, 6.5f) }) { mk.Box(white, g + f.Right * ((x0 + x1) / 2f) + f.Fwd * 7f + up * .6f, new Vector3(x1 - x0, .08f, .06f), f.r); Col(g + f.Right * ((x0 + x1) / 2f) + f.Fwd * 7f + up * .5f, new Vector3(x1 - x0, 1f, .12f), f.r); }
            for (var k = 0; k < 3; k++) { var fp = g + f.Right * (-9.5f + k * .9f) + f.Fwd * (4.5f + (k % 2) * .6f) + up * .17f; mk.Prism(dark, fp, up, .015f, .7f, 4); mk.Blob(WorldMaterials.Solid(new Color(1f, .45f, .65f)), fp + up * .8f, new Vector3(.14f, .12f, .26f), prng, 0f); mk.Prism(WorldMaterials.Solid(new Color(1f, .45f, .65f)), fp + up * .85f + f.Fwd * .2f, (up + f.Fwd * .2f).normalized, .03f, .4f, 5); }
            var spm = new MeshKit(); spm.Prism(metal, Vector3.zero, Vector3.up, .04f, .3f, 6); spm.Box(metal, new Vector3(0, .3f, 0), new Vector3(.7f, .04f, .04f), Quaternion.identity); foreach (var e in new[] { -1f, 1f }) spm.Blob(WorldMaterials.Glow(new Color(.7f, .9f, 1f), .8f), new Vector3(e * .9f, .5f, 0), new Vector3(.5f, .2f, .12f), prng, .2f); var sgo = spm.BuildSingle("Sprinkler", plife.transform, false); sgo.transform.position = g + f.Right * .5f + f.Fwd * 4.5f + up * .17f; plife.sprinkler = sgo.transform;
            // the owner in a lawn chair with the hose; the culprit behind the tree
            var ch = g + f.Right * -1.5f + f.Fwd * 3.5f + up * .17f; mk.Box(WorldMaterials.Solid(new Color(.3f, .7f, .4f)), ch + up * .42f, new Vector3(.6f, .05f, .55f), f.r); mk.Box(WorldMaterials.Solid(new Color(.3f, .7f, .4f)), ch + up * .8f - f.Fwd * .3f, new Vector3(.6f, .7f, .05f), f.r * Quaternion.Euler(-12f, 0, 0)); foreach (var e in new[] { -1f, 1f }) mk.Box(metal, ch + up * .3f + f.Right * e * .3f, new Vector3(.03f, .6f, .6f), f.r);
            var owner = Kid(Residents.Tool.Hose); owner.top = new Color(.9f, .85f, .6f); owner.bottom = new Color(.6f, .55f, .4f); owner.hat = Residents.Hat.Bucket; owner.hatColor = new Color(.9f, .9f, .85f); owner.hair = new Color(.8f, .8f, .8f); Folk(ch + up * .44f, f.Fwd, Resident.Pose.Sit, new[] { "Every morning I take it all down. Every night it's back. I've started to admire the consistency.", "I know it's you. It's always been you. It was you in '94 as well.", "Stay off the lawn. ...Fine. Stay off the flamingos.", "You want to know a secret? I buy the toilet paper. Left a crate out. Keeps them off the car." }, owner, null, .44f);
            Folk(G(tree + f.Right * 1.2f - f.Fwd * .6f), -f.Right, Resident.Pose.Lean, new[] { "Shh. He can't see me. I'm behind a tree.", "It's not vandalism, it's decorating. It washes off in the rain. It never rains.", "Grab a roll. Over the roof in one throw and you're a legend." }, Kid());
            var crate = G(g + f.Right * 5.2f + f.Fwd * 5.4f); mk.Box(WorldMaterials.Solid(new Color(.3f, .5f, .8f)), crate + up * .2f, new Vector3(.8f, .4f, .6f), f.r); Col(crate + up * .2f, new Vector3(.8f, .4f, .6f), f.r);
            for (var k = 0; k < 8; k++) { var m = new MeshKit(); m.Prism(paper, new Vector3(0, -.06f, 0), Vector3.up, .07f, .12f, 10); m.Prism(WorldMaterials.Solid(new Color(.7f, .6f, .45f)), new Vector3(0, -.061f, 0), Vector3.up, .025f, .122f, 6); var go = m.BuildSingle("Toilet roll", plife.transform, false); go.transform.position = crate + up * (.48f + (k / 4) * .13f) + f.Right * (-.25f + (k % 4) * .17f); var c = Interact.Carry(go, "a toilet roll (throw it over the house)", .12f, g.y - 12f); var bc = c.GetComponent<BoxCollider>(); if (bc != null) { bc.size = new Vector3(.14f, .12f, .14f); bc.center = Vector3.zero; } var rb = go.GetComponent<Rigidbody>(); if (rb != null) rb.isKinematic = true; }
            ArtistBits.PointLight(ctx.Root, hs.P(0, 2.6f, HD / 2f + 2f), new Color(1f, .9f, .65f), 14f, 1.4f);

            // the Legal Wall, off to the side: grey until you spray it
            var wf = new PlaceFrame(G(g + f.Right * 12f + f.Fwd * 0f), -f.Right); const float WL = 12f, WH = 3f;
            mk.Box(WorldMaterials.Solid(new Color(.62f, .62f, .62f)), wf.P(0, WH / 2f, 0), new Vector3(WL, WH, .4f), wf.r); solid.Box(concrete, wf.P(0, WH / 2f - .5f, 0), new Vector3(WL, WH + 1f, .4f), wf.r); for (var k = 1; k < 6; k++) mk.Box(concreteDark, wf.P(0, k * .5f, -.205f), new Vector3(WL, .02f, .01f), wf.r);
            string[] words = { "NO FUTURE\n(maybe a little)", "KYLE WAS HERE", "SK8", "MOM I'M FINE", "THE BAND\nRULES", "CLASS OF\nFOREVER", "♥", "POP. EVERYONE" }; var tags = new List<GameObject>();
            for (var k = 0; k < 8; k++)
            {
                var tgo = new GameObject("Tag " + k); tgo.transform.SetParent(plife.transform, false); var c = Spray[k % Spray.Length]; var tm = new MeshKit(); var x = -WL / 2f + 1.2f + (k % 4) * 3.2f; var y = k < 4 ? 2.05f : .85f;
                tm.Blob(WorldMaterials.Glow(c, .5f), wf.P(x, y, -.2f), new Vector3(1.3f, .5f, .03f), prng, .25f); tm.Blob(WorldMaterials.Glow(Spray[(k + 3) % Spray.Length], .5f), wf.P(x + .5f, y - .2f, -.21f), new Vector3(.7f, .3f, .02f), prng, .3f); for (var q = 0; q < 3; q++) tm.Box(WorldMaterials.Glow(c, .5f), wf.P(x - .7f + q * .7f, y - .6f - q * .08f, -.21f), new Vector3(.05f, .4f + q * .12f, .01f), wf.r);
                tm.BuildSingle("Paint", tgo.transform, false); ArtistBits.Label(tgo.transform, words[k], wf.P(x, y, -.26f), -wf.Fwd, .2f, k % 2 == 0 ? new Color(.98f, .98f, .95f) : new Color(.08f, .08f, .1f), TextAnchor.MiddleCenter); tgo.SetActive(false); tags.Add(tgo);
            }
            var cans = wf.P(0, 0, -2.2f); mk.Box(wood, cans + up * .25f, new Vector3(1.2f, .5f, .8f), wf.r); Col(cans + up * .25f, new Vector3(1.2f, .5f, .8f), wf.r); for (var k = 0; k < 6; k++) { mk.Prism(Paint(k), cans + up * .5f + wf.Right * (-.4f + k * .16f), up, .04f, .2f, 8); mk.Prism(white, cans + up * .7f + wf.Right * (-.4f + k * .16f), up, .02f, .03f, 6); }
            var cgo = new GameObject("Spray cans"); cgo.transform.SetParent(plife.transform, false); cgo.transform.position = cans + up * .6f; var tn = 0;
            Interact.Toy(cgo, Toy.Kind.Custom, "Spray the wall", new Vector3(1.4f, .8f, 1f)).onUse = _ => { if (tn < tags.Count) { tags[tn++].SetActive(true); Say(plife.transform, wf.P(0, WH + .8f, 0), tn == tags.Count ? "Psssht. That's the wall full. It's the best it's ever looked." : "Psssht.", 2.6f); } else { foreach (var t in tags) t.SetActive(false); tn = 0; Say(plife.transform, wf.P(0, WH + .8f, 0), "A man from the city comes and paints it grey. He sighs. He leaves the cans.", 5f); } };
            ArtistBits.Label(ctx.Root, "LEGAL WALL\n(go on then)", wf.P(-WL / 2f + 1.4f, WH + .5f, -.1f), -wf.Fwd, .18f, new Color(.15f, .15f, .18f), TextAnchor.MiddleCenter); mk.Box(white, wf.P(-WL / 2f + 1.4f, WH + .5f, 0), new Vector3(2.4f, .9f, .08f), wf.r);
            SitCouch(mk, wf.P(3.5f, 0, -5.5f), wf.Fwd, 2);
            StreetSign(mk, f.P(8.5f, 0, 11f), f.Fwd, "MAPLE COURT\n(the nice house)  ·  LEGAL WALL", .15f);
            Drive(ctx, mk, g + f.Right * -4f + f.Fwd * 7.5f); Ring(g, 15f);
            Shot(ctx, "the tp'd house", f.P(-10f, 3.2f, 17f), hs.P(2f, 2.4f, 0));
            Shot(ctx, "the legal wall", wf.P(-3f, 2f, -9f), wf.P(0, 1.5f, 0));
        }

        // ---------------------------------------------------------------- the Water Tower
        void WaterTower(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var g = ctx.OnGround(TowerAt.x, TowerAt.y); var f = new PlaceFrame(g, FaceTo(g, Nearest(laneP, g))); const float PH = 7f, PW = 7.4f, TR = 2.3f, TH = 4.2f;
            var steel = WorldMaterials.Solid(new Color(.6f, .66f, .68f), .5f); var tank = WorldMaterials.Solid(new Color(.85f, .9f, .9f));
            mk.Prism(concreteDark, g - up * .3f, up, 8f, .4f, 14);
            // four legs with cross braces, the deck, the tank with its cone roof and a red light on top
            foreach (var (x, z) in new[] { (-1f, -1f), (1f, -1f), (1f, 1f), (-1f, 1f) }) { var b = f.P(x * 3.2f, 0, z * 3.2f); var t = f.P(x * 2.4f, PH, z * 2.4f); mk.Prism(steel, b - up * .2f, (t - b).normalized, .14f, (t - b).magnitude + .2f, 6); ArtistBits.PostCollider(ctx.Root, f.P(x * 2.9f, 0, z * 2.9f), PH, .2f); }
            for (var k = 0; k < 4; k++) { var d = f.r * (Quaternion.Euler(0, k * 90f, 0) * Vector3.forward); var sd = Vector3.Cross(up, d); foreach (var e in new[] { -1f, 1f }) { var a = g + d * 3f + sd * e * 3f + up * .6f; var b = g + d * 2.6f - sd * e * 2.6f + up * (PH - .8f); mk.Prism(steel, a, (b - a).normalized, .04f, (b - a).magnitude, 4); } }
            mk.Box(steel, f.P(0, PH, 0), new Vector3(PW, .2f, PW), f.r); solid.Box(steel, f.P(0, PH, 0), new Vector3(PW, .2f, PW), f.r);
            mk.Prism(tank, f.P(0, PH + .1f, 0), up, TR, TH, 16); mk.Cone(roofless(), f.P(0, PH + .1f + TH, 0), TR + .15f, 1.3f, 16); mk.Blob(GlowM(new Color(1f, .15f, .1f), 2.6f), f.P(0, PH + TH + 1.55f, 0), Vector3.one * .16f, prng, 0f); halos.Add(f.P(0, PH + TH + 1.55f, 0), 1f, new Color(1f, .2f, .15f), .5f); Col(f.P(0, PH + .1f + TH / 2f, 0), new Vector3(TR * 1.7f, TH, TR * 1.7f), f.r); Col(f.P(0, PH + .1f + TH / 2f, 0), new Vector3(TR * 1.7f, TH, TR * 1.7f), f.r * Quaternion.Euler(0, 45f, 0));
            foreach (var y in new[] { .8f, 3.4f }) mk.Prism(steel, f.P(0, PH + .1f + y, 0), up, TR + .03f, .12f, 16);
            // the town's name on the tank, and what the kids added
            ArtistBits.Label(ctx.Root, "THE BADLANDS", f.P(0, PH + 2.9f, TR + .04f), f.Fwd, .42f, new Color(.2f, .35f, .5f), TextAnchor.MiddleCenter); ArtistBits.Label(ctx.Root, "RULE!!", f.P(1.1f, PH + 2.15f, TR + .04f), f.Fwd, .34f, Hot, TextAnchor.MiddleCenter); ArtistBits.Label(ctx.Root, "class of forever", f.P(-.6f, PH + 1.5f, TR + .04f), f.Fwd, .2f, new Color(.1f, .1f, .12f), TextAnchor.MiddleCenter);
            ArtistBits.Label(ctx.Root, "I ♥ whoever\nreads this", f.P(0, PH + 2.2f, -TR - .04f), -f.Fwd, .24f, Sky * .7f, TextAnchor.MiddleCenter);
            // the stairs: one long steel flight up the front to a gap in the rail
            const int steps = 20; var rise = PH / steps; const float run = .5f; var s0 = f.P(2.6f, 0, PW / 2f + steps * run);
            for (var k = 0; k < steps; k++) { var c = s0 - f.Fwd * (k * run + run / 2f) + up * (rise * (k + .5f)); mk.Box(steel, c + up * (rise / 2f - .04f), new Vector3(1.5f, .08f, run + .02f), f.r); solid.Box(steel, c - up * (rise * k / 2f), new Vector3(1.5f, rise * (k + 1), run), f.r); }
            foreach (var e in new[] { -1f, 1f }) { var a = s0 + f.Right * e * .78f + up * 1f; var b = f.P(2.6f + e * .78f, PH + 1f, PW / 2f); mk.Prism(steel, a, (b - a).normalized, .035f, (b - a).magnitude, 4); for (var k = 0; k <= 4; k++) { var p = Vector3.Lerp(a, b, k / 4f); mk.Prism(steel, p - up * 1f, up, .03f, 1f, 4); } var mid = (a + b) / 2f - up * .5f; Col(mid, new Vector3(.08f, 1.1f, (b - a).magnitude), Quaternion.LookRotation((b - a).normalized)); }
            // the rail round the deck (open at the stair head and at the sitting edge's gaps are closed: you sit under the rail)
            for (var k = 0; k < 4; k++)
            {
                var d = f.r * (Quaternion.Euler(0, k * 90f, 0) * Vector3.forward); var sd = Vector3.Cross(up, d); var c = f.P(0, PH + .1f, 0) + d * (PW / 2f - .06f); var rot = Quaternion.LookRotation(d);
                mk.Box(steel, c + up * 1.05f, new Vector3(PW, .06f, .06f), rot); for (var q = 0; q <= 6; q++) mk.Prism(steel, c + sd * (-PW / 2f + q * PW / 6f), up, .03f, 1.05f, 4);
                if (k == 0) { Col(c + up * .55f - f.Right * (PW / 2f - (PW / 2f + 1.8f) / 2f), new Vector3(PW / 2f + 1.8f, 1.1f, .1f), rot); Col(c + up * .55f + f.Right * (PW / 2f - .12f), new Vector3(.24f, 1.1f, .1f), rot); } else Col(c + up * .55f, new Vector3(PW, 1.1f, .1f), rot);
            }
            // sit with your legs over the edge on the far side, looking out; a boombox, a crate, pizza boxes somebody carried up after all
            var edge = f.P(0, PH + .1f, -PW / 2f + .25f); for (var k = 0; k < 3; k++) Interact.Seat(plife.transform, edge + f.Right * (-2f + k * 1.6f) + up * .02f, -f.Fwd, "Sit with your legs over the edge");
            Folk(edge + f.Right * 2.6f + up * .02f, -f.Fwd, Resident.Pose.Sit, new[] { "You can see the whole town from up here. That's the problem and the point.", "Every band in the Badlands wrote their first song up here. It's the same song.", "I'm leaving, you know. Soon as this sunset's done. It's been noon for years, so." }, Kid(), null, .02f);
            var bb = f.P(-2.9f, PH + .1f, 1.6f); var bm = new MeshKit(); bm.Box(dark, new Vector3(0, .2f, 0), new Vector3(.7f, .4f, .22f), Quaternion.identity); var spk = new Material(GlowM(Lime, 1.2f)) { name = "Boombox light" }; foreach (var e in new[] { -1f, 1f }) bm.Prism(spk, new Vector3(e * .2f, .2f, .11f), Vector3.forward, .13f, .02f, 10); bm.Box(metal, new Vector3(0, .45f, 0), new Vector3(.5f, .03f, .03f), Quaternion.identity); var bgo = bm.BuildSingle("Boombox", plife.transform, false); bgo.transform.SetPositionAndRotation(bb, Quaternion.LookRotation(f.Right)); plife.boomM = spk;
            string[] tape = { "Click. The tape's a copy of a copy. It sounds like it was recorded inside a shoe. It's perfect.", "Click. Side B. Somebody's little brother talks over the intro.", "Click. It chews the tape, thinks about it, and carries on." }; var bn = 0;
            Interact.Toy(bgo, Toy.Kind.Custom, "Press play", new Vector3(.9f, .7f, .6f)).onUse = _ => { plife.played = Time.time; Say(plife.transform, bb + up * 1.4f, tape[bn++ % tape.Length], 4.5f); };
            ArtistBits.PointLight(ctx.Root, f.P(0, PH + 2.2f, -PW / 2f - 1f), new Color(1f, .85f, .6f), 14f, 1.4f); ArtistBits.PointLight(ctx.Root, f.P(0, 3f, 5f), new Color(1f, .9f, .7f), 14f, 1.2f);
            // at the foot: a couch, bikes, a sign nobody obeys
            SitCouch(mk, f.P(-5.5f, 0, 3f), f.Right, 1); var ns = G(f.P(4.6f, 0, PW / 2f + steps * run + 1.5f)); mk.Prism(metal, ns - up * .2f, up, .05f, 2f, 6); mk.Box(white, ns + up * 1.6f, new Vector3(1.3f, .7f, .05f), f.r); ArtistBits.Label(ctx.Root, "NO CLIMBING\n(this means you, Kyle)", ns + up * 1.6f + f.Fwd * .04f, f.Fwd, .1f, new Color(.8f, .15f, .15f), TextAnchor.MiddleCenter); ArtistBits.PostCollider(ctx.Root, ns, 2f, .08f);
            Drive(ctx, mk, s0 + f.Fwd * 1.5f); Ring(g, 10f);
            Shot(ctx, "the water tower", f.P(-12f, 5f, 16f), f.P(0, PH + 1f, 0));
            Shot(ctx, "up the water tower", f.P(2.6f, PH + 1.8f, PW / 2f - .8f), f.P(-1f, PH + 1f, -PW / 2f));
        }

        Material roofless() => WorldMaterials.Solid(new Color(.55f, .3f, .25f));
    }

    /// The Punk places' moving parts: the mirror ball and prom lights, the jukebox, the sprinkler, the boombox.
    public class PunkPlacesLife : MonoBehaviour
    {
        public Transform ball, sprinkler; public List<Light> promLights = new List<Light>(); public Material jukeM, boomM; public float spun = -99f, kicked = -99f, played = -99f;

        void Update()
        {
            var t = Time.time;
            if (ball != null) ball.Rotate(0, (20f + (t - spun < 6f ? 200f * (1f - (t - spun) / 6f) : 0f)) * Time.deltaTime, 0, Space.World);
            if (sprinkler != null) sprinkler.Rotate(0, 90f * Time.deltaTime, 0, Space.World);
            for (var i = 0; i < promLights.Count; i++) if (promLights[i] != null) promLights[i].intensity = 1.8f + 1.4f * Mathf.Sin(t * 1.3f + i * 1.7f) + (t - spun < 6f ? 1.5f : 0f);
            if (jukeM != null) { var k = t - kicked < 4f ? 3f : 1f; jukeM.SetColor("_EmissionColor", Color.HSVToRGB(Mathf.Repeat(t * .12f, 1f), .8f, 1f) * (.8f * k)); }
            if (boomM != null) boomM.SetColor("_EmissionColor", PunkKit.Lime * (t - played < 20f ? .6f + .9f * Mathf.Abs(Mathf.Sin(t * 6f)) : .3f));
        }
    }
}
