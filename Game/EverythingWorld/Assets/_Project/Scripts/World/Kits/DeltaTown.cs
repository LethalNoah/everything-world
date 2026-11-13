using System.Collections.Generic;
using UnityEngine;
using static EverythingWorld.World.PlaceTools;

namespace EverythingWorld.World
{
    /// The Delta Crossroads' places (part of DeltaKit): the two roads and the telegraph poles; the crossroads itself
    /// (the four-armed signpost, a rusting pickup, an old man on a crate, a long shadow lying on the road with nobody
    /// to cast it, and the thing that stands up when Robert Johnson plays); the railroad (the crossing, a water tower,
    /// a handcar); the river landing (a jetty, a ferryman, the wreck of a steamboat with one light still burning);
    /// the graveyard and its white church (ring the bell); the general store (rocking chairs, cold drinks, checkers);
    /// and a windpump turning out in the cotton.
    public partial class DeltaKit
    {
        DeltaTownLife tlife; System.Random prng; Transform rootT; LandContext ctx0;

        static readonly Color[] Shirts = { new Color(.3f, .38f, .56f), new Color(.62f, .3f, .24f), new Color(.86f, .82f, .7f), new Color(.5f, .42f, .3f), new Color(.24f, .26f, .3f), new Color(.72f, .62f, .4f) };
        static readonly Color[] Overalls = { new Color(.26f, .32f, .48f), new Color(.3f, .26f, .22f), new Color(.44f, .4f, .34f) };
        static readonly string[] TownTalk =
        {
            "Hot. Been hot since morning and it'll be hot till that storm makes up its mind.", "Storm's been sitting out west all day. Sits there most days. Some days it comes.",
            "You hear a guitar out in the cotton after dark, you keep walking.", "Juke's on the corner. River's east. Don't stand in the crossroads too long.", "Everybody here's either coming from somewhere or waiting on something.",
        };

        void TownStart(LandContext ctx)
        {
            rootT = ctx.Root; ctx0 = ctx; prng = new System.Random(1911);
            tlife = new GameObject("Delta town life").AddComponent<DeltaTownLife>(); tlife.transform.SetParent(life.transform, false);
        }

        Residents.Look Local(Residents.Tool tool = Residents.Tool.None, bool apron = false) { var l = Residents.Random(prng, Shirts, Overalls, new[] { Residents.Hat.Straw, Residents.Hat.Cowboy, Residents.Hat.Cap, Residents.Hat.None, Residents.Hat.Straw }, new[] { new Color(.8f, .72f, .5f), new Color(.3f, .26f, .22f), new Color(.6f, .56f, .5f) }); l.tool = tool; l.apron = apron; return l; }
        Resident Folk(Vector3 feet, Vector3 face, Resident.Pose pose, string[] lines = null, Residents.Look look = null, Vector3[] route = null, float seatH = HumanScale.SeatHeight)
            => Residents.Make(tlife.transform, feet, face, look ?? Local(), pose, prng, lines ?? TownTalk, route, seatH);
        void Col(Vector3 c, Vector3 size, Quaternion rot) => BoxCol(rootT, c, size, rot);
        void Mark(Vector3 at, float r) => noGrass.Add(new Vector3(at.x, r, at.z));
        void Sign(Vector3 at, Vector3 face, string text, float size, Color c) => ArtistBits.Label(rootT, text, at, face, size, c, TextAnchor.MiddleCenter);

        Carryable Thing(MeshKit m, string name, string label, Vector3 at, Quaternion rot, Vector3 size, float mass, bool still = true)
        {
            var go = m.BuildSingle(name, tlife.transform, false); go.transform.SetPositionAndRotation(at, rot); var c = Interact.Carry(go, label, mass, at.y - 30f);
            var bc = c.GetComponent<BoxCollider>(); if (bc != null) { bc.size = size; bc.center = new Vector3(0, size.y / 2f, 0); }
            if (still) { var rb = go.GetComponent<Rigidbody>(); if (rb != null) rb.isKinematic = true; }
            return c;
        }

        /// Something to use that is only a spot in the air (a knock, a look, a listen).
        Toy Spot(string name, Vector3 at, string verb, Vector3 size)
        {
            var go = new GameObject(name); go.transform.SetParent(tlife.transform, false); go.transform.position = at; return Interact.Toy(go, Toy.Kind.Custom, verb, size);
        }

        /// A crate to sit on.
        void Crate(MeshKit mk, Vector3 g, Vector3 face, bool seat = true)
        {
            var rot = Quaternion.LookRotation(face); mk.Box(plank, g + Vector3.up * .23f, new Vector3(.6f, .46f, .6f), rot); mk.Box(plankDark, g + Vector3.up * .23f, new Vector3(.64f, .08f, .64f), rot); Col(g + Vector3.up * .23f, new Vector3(.6f, .46f, .6f), rot);
            if (seat) Interact.Seat(tlife.transform, g + Vector3.up * .48f, face).seatHeight = .46f;
        }

        /// A room to walk into: floor, three walls, a front wall with a doorway at dx, a flat ceiling. 'fo' sits on the floor.
        void Room(MeshKit mk, MeshKit solid, PlaceFrame fo, float W, float D, float H, float T, float dx, float door, Material wall, Material floor)
        {
            void Wall(Vector3 p, Vector3 s) { mk.Box(wall, p, s, fo.r); solid.Box(wall, p, s, fo.r); }
            mk.Box(floor, fo.P(0, -.06f, 0), new Vector3(W, .12f, D), fo.r); solid.Box(floor, fo.P(0, -.5f, 0), new Vector3(W, 1f, D), fo.r);
            Wall(fo.P(0, H / 2f, -D / 2f + T / 2f), new Vector3(W, H, T)); foreach (var e in new[] { -1f, 1f }) Wall(fo.P(e * (W / 2f - T / 2f), H / 2f, 0), new Vector3(T, H, D - T * 2f));
            float l = W / 2f + dx - door / 2f, r = W / 2f - dx - door / 2f;
            if (l > .05f) Wall(fo.P(-W / 2f + l / 2f, H / 2f, D / 2f - T / 2f), new Vector3(l, H, T));
            if (r > .05f) Wall(fo.P(W / 2f - r / 2f, H / 2f, D / 2f - T / 2f), new Vector3(r, H, T));
            var dh = HumanScale.DoorHeight + .2f; Wall(fo.P(dx, (H + dh) / 2f, D / 2f - T / 2f), new Vector3(door, H - dh, T));
            mk.Box(wall, fo.P(0, H + .1f, 0), new Vector3(W, .2f, D), fo.r); solid.Box(wall, fo.P(0, H + .1f, 0), new Vector3(W, .2f, D), fo.r);
            IndoorSpaces.Add(fo.P(0, H / 2f, 0), fo.r, new Vector3(W - T * 2f - .2f, H, D - T * 2f - .2f));
        }

        // ---------------------------------------------------------------- the roads
        void Roads(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up;
            // the Highway, north and south, in one strip; the cross road in two, butting against it
            var hw = new List<Vector3>(); for (var u = -southL; u <= northL + .01f; u += 2f) hw.Add(G(ctx, u, 0)); Strip(mk, hw, dustM, RoadW, .12f, ctx); foreach (var p in hw) Remember(p, 1);
            var west = new List<Vector3>(); for (var r = -westL; r <= -RoadW / 2f + .01f; r += 2f) west.Add(G(ctx, 0, Mathf.Min(r, -RoadW / 2f))); west.Add(G(ctx, 0, -RoadW / 2f)); Strip(mk, west, dustM, RoadW, .12f, ctx); foreach (var p in west) Remember(p, 2);
            var east = new List<Vector3> { G(ctx, 0, RoadW / 2f) }; for (var r = RoadW / 2f + 2f; r <= eastL - 22f; r += 2f) east.Add(G(ctx, 0, r)); if (east.Count > 1) { Strip(mk, east, dustM, RoadW, .12f, ctx); foreach (var p in east) Remember(p, 2); }
            // wheel ruts, darker, down the middle of the Highway
            var rut = WorldMaterials.Solid(new Color(.72f, .6f, .4f)); foreach (var e in new[] { -1.1f, 1.1f }) { var pts = new List<Vector3>(); for (var u = -southL + 2f; u <= northL - 2f; u += 4f) { if (Mathf.Abs(u) < 6f || Mathf.Abs(u - RailOff) < 5f) { if (pts.Count > 1) Strip(mk, pts, rut, .5f, .135f, ctx); pts = new List<Vector3>(); continue; } pts.Add(G(ctx, u, e)); } if (pts.Count > 1) Strip(mk, pts, rut, .5f, .135f, ctx); }
            // telegraph poles up the east side of the Highway, wires sagging between them
            Vector3 prevA = default, prevB = default; var have = false;
            for (var u = -southL + 10f; u <= northL - 6f; u += 30f)
            {
                if (Mathf.Abs(u) < 12f || Mathf.Abs(u - RailOff) < 8f) { have = false; continue; }
                var g = G(ctx, u, RoadW / 2f + 2.6f); mk.Prism(trunkM, g - up * .3f, up, .11f, 7.4f, 5, .7f); mk.Box(trunkM, g + up * 6.5f, new Vector3(.1f, .1f, 1.8f), Quaternion.LookRotation(R)); ArtistBits.PostCollider(ctx.Root, g, 7f, .14f);
                foreach (var e in new[] { -.8f, .8f }) mk.Prism(WorldMaterials.Solid(new Color(.5f, .7f, .7f), .8f), g + up * 6.56f + R * e, up, .04f, .14f, 5);
                Vector3 a = g + up * 6.7f - R * .8f, b = g + up * 6.7f + R * .8f;
                if (have) foreach (var (p0, p1) in new[] { (prevA, a), (prevB, b) }) { var prev = p0; for (var s = 1; s <= 6; s++) { var t = s / 6f; var q = Vector3.Lerp(p0, p1, t) - up * Mathf.Sin(t * Mathf.PI) * 1.1f; mk.Prism(dark, prev, (q - prev).normalized, .012f, Vector3.Distance(prev, q), 3); prev = q; } }
                prevA = a; prevB = b; have = true; if ((int)(u / 30f) % 3 == 0) for (var k = 0; k < 3; k++) mk.Blob(dark, a + R * (.3f + k * .3f) + up * .1f, new Vector3(.06f, .09f, .12f), prng, 0f);
            }
            Shot(ctx, "down the highway", G(ctx, 56f, -1.5f) + up * 2.2f, G(ctx, -40f, 0) + up * 2f);
        }

        // ---------------------------------------------------------------- the crossroads
        void Crossroads(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var c = ctx.OnGround(center.x, center.z);
            // the signpost on the north-east corner: four arms
            var sp = G(ctx, 5.2f, 5.2f); mk.Prism(trunkM, sp - up * .3f, up, .1f, 4.3f, 5, .8f); ArtistBits.PostCollider(ctx.Root, sp, 4f, .14f);
            var arms = new[] { (U, "THE RAILROAD", 3.6f), (-U, "DOWN SOUTH", 3.3f), (R, "THE RIVER", 3f), (-R, "THE FIELDS", 2.7f) };
            foreach (var (d, text, h) in arms) { var rot = Quaternion.LookRotation(Vector3.Cross(up, d)); mk.Box(white, sp + up * h + d * .9f, new Vector3(1.7f, .26f, .05f), rot); foreach (var e in new[] { -1f, 1f }) Sign(sp + up * h + d * .9f + rot * Vector3.forward * e * .035f, rot * Vector3.forward * e, text, .14f, Ink); }
            // a rusting pickup on the south-east corner, a bare bulb over the north-west
            var pk = G(ctx, -8.5f, 8.5f); var pr = Quaternion.LookRotation((U + R * .4f).normalized); var pf = pr * Vector3.forward; var ps = pr * Vector3.right;
            mk.Box(rustM, pk + up * .75f, new Vector3(1.7f, .6f, 4.4f), pr); mk.Box(rustM, pk + up * 1.35f + pf * .5f, new Vector3(1.6f, .7f, 1.5f), pr); mk.Box(WorldMaterials.Solid(new Color(.2f, .22f, .24f), .8f), pk + up * 1.4f + pf * .5f, new Vector3(1.64f, .4f, 1.3f), pr); mk.Box(plankDark, pk + up * 1.15f - pf * 1.3f, new Vector3(1.6f, .3f, 1.7f), pr);
            foreach (var e in new[] { -1f, 1f }) foreach (var s in new[] { -1.4f, 1.4f }) mk.Prism(dark, pk + up * .38f + ps * e * .8f + pf * s, ps * e, .38f, .2f, 10); Col(pk + up * .9f, new Vector3(1.8f, 1.6f, 4.5f), pr); Mark(pk, 3f);
            Interact.Seat(tlife.transform, pk + up * 1.05f - pf * 2.1f, -pf, "Sit on the tailgate").seatHeight = .9f;
            BulbPole(ctx, mk, G(ctx, 6f, -6f), (R - U).normalized, true);
            // the old man on a crate, and a spare crate beside him
            var om = G(ctx, -6.5f, 5.2f); Crate(mk, om, -R, false); Crate(mk, om + U * 1.3f + R * .3f, -R);
            var old = Local(); old.hat = Residents.Hat.Straw; old.hair = new Color(.8f, .8f, .78f);
            Folk(om + up * .46f, -R, Resident.Pose.Sit, new[] { "Four ways to go and every one of them's the wrong one. Sit a while instead.", "They say a man can learn to play here, round midnight, if he don't mind who's teaching.", "That shadow on the west road? Been there since before the road. Don't nobody step on it.", "It ain't midnight. But some songs, it gets midnight early." }, old, null, .46f);
            // a long shadow lying down the west road, hat and all, with nobody to cast it
            var sh = G(ctx, 0, -7f) + up * .145f; mk.Box(shadowM, sh - R * 2.6f, new Vector3(.5f, .01f, 5.2f), Quaternion.LookRotation(R)); mk.Box(shadowM, sh - R * 5.6f, new Vector3(.9f, .01f, .9f), Quaternion.LookRotation(R)); mk.Box(shadowM, sh - R * 5.9f, new Vector3(1.7f, .01f, .24f), Quaternion.LookRotation(R)); mk.Box(shadowM, sh - R * 1f + U * .34f, new Vector3(.2f, .01f, 2.2f), Quaternion.LookRotation((R + U * .12f).normalized));
            // ...and what stands up at the head of it when Robert Johnson plays: tall, thin, a hat, no face
            var fm = new MeshKit(); fm.Box(shadowM, new Vector3(0, 1.5f, 0), new Vector3(.5f, 1.5f, .26f), Quaternion.identity); foreach (var e in new[] { -.14f, .14f }) fm.Box(shadowM, new Vector3(e, .4f, 0), new Vector3(.18f, .8f, .2f), Quaternion.identity); foreach (var e in new[] { -.34f, .34f }) fm.Box(shadowM, new Vector3(e, 1.4f, 0), new Vector3(.13f, 1.5f, .16f), Quaternion.identity);
            fm.Blob(shadowM, new Vector3(0, 2.5f, 0), new Vector3(.18f, .22f, .18f), prng, 0f); fm.Prism(shadowM, new Vector3(0, 2.62f, 0), Vector3.up, .46f, .03f, 10); fm.Prism(shadowM, new Vector3(0, 2.64f, 0), Vector3.up, .22f, .26f, 10, .85f);
            foreach (var e in new[] { -.07f, .07f }) fm.Blob(eyesM, new Vector3(e, 2.52f, .17f), Vector3.one * .025f, prng, 0f);
            var fgo = fm.BuildSingle("The one at the crossroads", life.transform, false); fgo.transform.SetPositionAndRotation(G(ctx, 0, -13f) + up * .1f, Quaternion.LookRotation(R)); fgo.transform.localScale = new Vector3(1f, .01f, 1f); fgo.isStatic = false; fgo.SetActive(false); life.figure = fgo.transform;
            // eyes out in the cotton, low, in pairs
            for (var k = 0; k < 7; k++) { var a = (k * 51f + 20f) * Mathf.Deg2Rad; var d = 24f + (k % 3) * 9f; var e = G(ctx, Mathf.Cos(a) * d, Mathf.Sin(a) * d); if (InFootprint(ctx, e.x, e.z, 1f)) continue; var look = FaceTo(e, c); var sd = Vector3.Cross(up, look); foreach (var q in new[] { -.09f, .09f }) mk.Blob(eyesM, e + up * .55f + sd * q, Vector3.one * .04f, prng, 0f); }
            string[] stand = { "The dust settles round your feet. Four roads. No wind. Something is deciding.", "You feel looked at, from the cotton, about knee high.", "Nothing happens. That's the frightening part." }; var sn = 0;
            Spot("The middle of the crossroads", c + up * 1.2f, "Stand in the crossroads", new Vector3(3f, 2.4f, 3f)).onUse = _ => Say(tlife.transform, c + up * 2.8f, stand[sn++ % stand.Length], 5f);
            Mark(c, 9f);
            Shot(ctx, "the crossroads", G(ctx, -15f, 13f) + up * 2.6f, G(ctx, 12f, -8f) + up * 2.4f);
            Shot(ctx, "the shadow on the west road", G(ctx, 3f, 4f) + up * 2f, G(ctx, 0, -12f) + up * 1.2f);
            // the windpump out in the cotton, turning
            var wp = G(ctx, 68f, -16f); foreach (var (x, zz) in new[] { (-1f, -1f), (1f, -1f), (1f, 1f), (-1f, 1f) }) { var foot = wp + new Vector3(x * 1.3f, -.2f, zz * 1.3f); var top = wp + new Vector3(x * .25f, 8f, zz * .25f); mk.Prism(steel, foot, (top - foot).normalized, .04f, (top - foot).magnitude, 4); }
            for (var k = 1; k < 4; k++) mk.Box(steel, wp + up * k * 2f, new Vector3(2.6f - k * .55f, .04f, 2.6f - k * .55f), Quaternion.identity); Col(wp + up * 4f, new Vector3(1.6f, 8f, 1.6f), Quaternion.identity);
            var bm = new MeshKit(); for (var k = 0; k < 12; k++) bm.Box(tin, Quaternion.Euler(0, 0, k * 30f) * new Vector3(0, .9f, 0), new Vector3(.34f, 1.1f, .02f), Quaternion.Euler(0, 0, k * 30f) * Quaternion.Euler(0, 24f, 0)); bm.Prism(steel, new Vector3(0, 0, -.1f), Vector3.forward, .14f, .2f, 8);
            var bgo = bm.BuildSingle("Windpump wheel", tlife.transform, false); bgo.transform.position = wp + up * 8.2f + R * .4f; tlife.wheel = bgo.transform; tlife.wheelRot = Quaternion.LookRotation(R); mk.Box(tin, wp + up * 8.2f - R * 1.2f, new Vector3(.03f, .7f, 1f), Quaternion.LookRotation(R));
            mk.Box(rustM, wp + R * 2.4f + up * .3f, new Vector3(1f, .6f, 2.4f), Quaternion.identity); mk.Box(WorldMaterials.Solid(new Color(.3f, .4f, .46f), .9f), wp + R * 2.4f + up * .58f, new Vector3(.86f, .04f, 2.26f), Quaternion.identity); Col(wp + R * 2.4f + up * .3f, new Vector3(1f, .6f, 2.4f), Quaternion.identity); Mark(wp, 4f);
        }

        // ---------------------------------------------------------------- the railroad
        void Railroad(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var gravel = WorldMaterials.Solid(new Color(.56f, .5f, .44f)); var along = Quaternion.LookRotation(R);
            var bed = new List<Vector3>(); for (var r = -railW; r <= railE + .01f; r += 2f) bed.Add(G(ctx, RailOff, r)); Strip(mk, bed, gravel, 4f, .16f, ctx);
            foreach (var e in new[] { -.72f, .72f }) { var rail = new List<Vector3>(); for (var r = -railW; r <= railE + .01f; r += 2f) rail.Add(G(ctx, RailOff + e, r)); Strip(mk, rail, steel, .12f, .36f, ctx); Strip(mk, rail, rustM, .2f, .26f, ctx); }
            for (var r = -railW + .8f; r <= railE; r += 1.6f) { if (Mathf.Abs(r) < RoadW / 2f) continue; var g = G(ctx, RailOff, r); mk.Box(plankDark, g + up * .22f, new Vector3(.28f, .12f, 2.4f), along); }
            // the crossing: planks between the rails, a crossbuck each side
            mk.Box(plankDark, G(ctx, RailOff, 0) + up * .3f, new Vector3(RoadW, .16f, 2.6f), along);
            foreach (var e in new[] { -1f, 1f }) { var g = G(ctx, RailOff + e * 4.4f, e * (RoadW / 2f + 1.4f)); mk.Prism(white, g - up * .3f, up, .08f, 3.9f, 5); ArtistBits.PostCollider(ctx.Root, g, 3.6f, .1f); foreach (var a in new[] { -30f, 30f }) mk.Box(white, g + up * 3.2f, new Vector3(2.2f, .24f, .05f), Quaternion.LookRotation(U) * Quaternion.Euler(0, 0, a)); Sign(g + up * 3.2f - U * e * .04f, -U * e, "RAIL  ROAD", .13f, Ink); }
            for (var r = -railW; r <= railE; r += 3f) if (Mathf.Abs(r) > RoadW) Remember(G(ctx, RailOff, r), 5);
            // a water tower by the line, east of the Highway
            var wt = G(ctx, RailOff - 9f, 22f); foreach (var (x, zz) in new[] { (-1f, -1f), (1f, -1f), (1f, 1f), (-1f, 1f) }) { mk.Prism(trunkM, wt + new Vector3(x * 1.5f, -.3f, zz * 1.5f), up, .12f, 5.4f, 5); ArtistBits.PostCollider(ctx.Root, wt + new Vector3(x * 1.5f, 0, zz * 1.5f), 5f, .14f); }
            mk.Prism(plank, wt + up * 5f, up, 2.4f, 3f, 12); mk.Cone(tin, wt + up * 8f, 2.7f, 1.2f, 12); for (var k = 0; k < 3; k++) mk.Prism(iron, wt + up * (5.4f + k * 1f), up, 2.44f, .08f, 12); mk.Box(iron, wt + up * 5.2f + U * 3.2f, new Vector3(.2f, .2f, 2.4f), Quaternion.LookRotation(U) * Quaternion.Euler(20f, 0, 0)); Mark(wt, 4f);
            // a handcar on the line, east; sit on it
            var hc = G(ctx, RailOff, Mathf.Min(railE - 14f, 34f)) + up * .4f; mk.Box(plank, hc + up * .2f, new Vector3(1.6f, .1f, 2.4f), along); foreach (var e in new[] { -1f, 1f }) foreach (var s in new[] { -.8f, .8f }) mk.Prism(steel, hc + U * e * .72f + R * s, U * e, .22f, .08f, 10);
            mk.Prism(iron, hc + up * .2f, up, .05f, 1f, 5); mk.Box(iron, hc + up * 1.2f, new Vector3(.06f, .06f, 2f), along * Quaternion.Euler(14f, 0, 0)); Col(hc + up * .3f, new Vector3(1.6f, .6f, 2.4f), along); Interact.Seat(tlife.transform, hc + up * .3f + U * .5f, -U, "Sit on the handcar").seatHeight = .3f;
            Shot(ctx, "the railroad crossing", G(ctx, RailOff - 16f, 9f) + up * 2.4f, G(ctx, RailOff + 4f, -30f) + up * 3f);
        }

        // ---------------------------------------------------------------- the landing and the wreck
        void Landing(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var lr = eastL - 14f; var g = G(ctx, 0, lr); var deckY = waterY + 1.75f; var along = Quaternion.LookRotation(R);
            // the last of the road, a plank jetty out over the water
            var last = new List<Vector3>(); for (var r = eastL - 22f; r <= lr + .01f; r += 2f) last.Add(G(ctx, 0, r)); if (last.Count > 1) { Strip(mk, last, dustM, RoadW, .12f, ctx); foreach (var p in last) Remember(p, 2); }
            const float jl = 24f; var jc = new Vector3(g.x + jl / 2f, deckY, g.z); mk.Box(plank, jc, new Vector3(3f, .16f, jl), along); solid.Box(plank, jc - up * .3f, new Vector3(3f, .7f, jl), along);
            for (var d = 0f; d <= jl; d += 4f) foreach (var e in new[] { -1f, 1f }) { var p = new Vector3(g.x + d, waterY - 2f, g.z + e * 1.5f); mk.Prism(trunkM, p, up, .12f, 4.4f + (d % 8f < 1f ? .6f : 0f), 5); }
            for (var d = 0f; d <= jl; d += 2f) Remember(new Vector3(g.x + d, deckY + .1f, g.z), 2);
            BulbPole(ctx, mk, new Vector3(g.x + 2f, deckY + .08f, g.z - 1.3f), U, true); BulbPole(ctx, mk, new Vector3(g.x + jl - 1f, deckY + .08f, g.z + 1.3f), -U, true);
            // cotton bales waiting for a boat that isn't coming, a flat ferry tied up, the ferryman fishing
            for (var k = 0; k < 4; k++) { var b = G(ctx, 5f + (k / 2) * 1.5f, lr - 6f - (k % 2) * 1.7f); mk.Box(cottonM, b + up * .5f, new Vector3(1.3f, 1f, 1.5f), Quaternion.Euler(0, k * 8f, 0)); foreach (var e in new[] { -.4f, .4f }) mk.Box(plankDark, b + up * .5f + R * e, new Vector3(.06f, 1.04f, 1.54f), Quaternion.Euler(0, k * 8f, 0)); Col(b + up * .5f, new Vector3(1.3f, 1f, 1.5f), Quaternion.Euler(0, k * 8f, 0)); }
            var fb = new Vector3(g.x + 10f, waterY + .25f, g.z - 3.6f); mk.Box(plankDark, fb, new Vector3(2.4f, .5f, 5f), along); mk.Box(plank, fb + up * .27f, new Vector3(2.2f, .05f, 4.8f), along); mk.Prism(trunkM, fb + up * .3f - R * 2f, (up + R * .3f).normalized, .04f, 3.4f, 4);
            var fe = new Vector3(g.x + jl - 1.2f, deckY + .08f, g.z - .6f); Crate(mk, fe, -U, false);
            Folk(fe + up * .46f, -U, Resident.Pose.Sit, new[] { "Ferry don't run. River's too low and I'm too old. I'll take you across in a story, though.", "That's the Evening Star out there. Ran aground in a storm like the one that's coming. Light's still on. Nobody's aboard.", "Catfish down there older than me. We got an understanding.", "Some nights you hear her whistle. She ain't got steam. She ain't got a whistle." }, Local(Residents.Tool.Rod), null, .46f);
            // the wreck: a steamboat aground and listing, stacks broken, one window still lit
            var wk = new PlaceFrame(new Vector3(g.x + 34f, waterY - .4f, g.z + 26f), (U + R * .35f).normalized); var tilt = wk.r * Quaternion.Euler(0, 0, 11f); var hull = WorldMaterials.Solid(new Color(.74f, .72f, .66f)); var deckM = WorldMaterials.Solid(new Color(.5f, .44f, .38f));
            Vector3 K(float x, float y, float z) => wk.o + tilt * new Vector3(x, y, z);
            mk.Box(WorldMaterials.Solid(new Color(.3f, .22f, .2f)), K(0, .6f, 0), new Vector3(8f, 1.8f, 26f), tilt); mk.Box(hull, K(0, 2.9f, -1f), new Vector3(7f, 2.8f, 19f), tilt); mk.Box(deckM, K(0, 4.4f, -1f), new Vector3(8f, .14f, 21f), tilt); mk.Box(hull, K(0, 5.7f, -2f), new Vector3(5.6f, 2.4f, 13f), tilt); mk.Box(deckM, K(0, 7f, -2f), new Vector3(6.6f, .14f, 15f), tilt); mk.Box(hull, K(0, 8.1f, 2f), new Vector3(3f, 2f, 3.4f), tilt); mk.Box(rustM, K(0, 9.2f, 2f), new Vector3(3.6f, .14f, 4f), tilt);
            for (var k = 0; k < 7; k++) { mk.Box(k == 4 ? (Material)bulbM : dark, K(-3.52f, 3f, -8f + k * 2.4f), new Vector3(.04f, 1.2f, .9f), tilt); mk.Box(dark, K(3.52f, 3f, -8f + k * 2.4f), new Vector3(.04f, 1.2f, .9f), tilt); }
            halos.Add(K(-4.2f, 3f, 1.6f), 2.2f, Bulb, .55f); ArtistBits.PointLight(ctx.Root, K(-4.6f, 3f, 1.6f), Bulb, 12f, 1.6f);
            mk.Prism(dark, K(-1.2f, 7f, 5f), tilt * Vector3.up, .5f, 6.4f, 8); mk.Prism(dark, K(-1.2f, 13.4f, 5f), tilt * Vector3.up, .66f, .5f, 8); mk.Prism(dark, K(1.2f, 7f, 5f), tilt * Vector3.up, .5f, 2.6f, 8); mk.Prism(dark, K(3.4f, 6.4f, 6f), tilt * new Vector3(.9f, .2f, .3f).normalized, .5f, 3.4f, 8);
            var wheelC = K(0, 1.6f, -14.4f); for (var k = 0; k < 10; k++) mk.Box(rustM, wheelC + tilt * (Quaternion.Euler(k * 36f, 0, 0) * new Vector3(0, 1.9f, 0)), new Vector3(6.4f, .08f, .8f), tilt * Quaternion.Euler(k * 36f, 0, 0)); foreach (var e in new[] { -3.2f, 3.2f }) mk.Prism(rustM, wheelC + tilt * new Vector3(e, 0, 0), tilt * Vector3.right * Mathf.Sign(e), 2.2f, .1f, 12);
            Sign(K(-3.56f, 5.9f, -2f), tilt * Vector3.left, "EVENING STAR", .5f, new Color(.3f, .22f, .2f)); Col(K(0, 3f, -1f), new Vector3(8f, 7f, 26f), tilt);
            Mark(g, 8f);
            Shot(ctx, "the landing and the wreck", G(ctx, -5f, lr - 12f) + up * 2.6f, K(0, 5f, 0));
        }

        // ---------------------------------------------------------------- the graveyard and the church
        void Graveyard(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var c = ctx.OnGround(grave2.x, grave2.y); var f = new PlaceFrame(c, U);
            // the lane down from the cross road
            var lane = new List<Vector3>(); for (var u = -RoadW / 2f; u >= -62f + 13f; u -= 2f) lane.Add(G(ctx, u, -128f)); if (lane.Count > 1) { Strip(mk, lane, dustM, PathW, .12f, ctx); foreach (var p in lane) Remember(p, 3); }
            // a low picket fence round it, open on the north side
            const float hw = 12f, hd = 10f; var picket = WorldMaterials.Solid(new Color(.8f, .78f, .7f));
            void Fence(Vector3 a, Vector3 b) { var d = b - a; var len = d.magnitude; var rr = Quaternion.LookRotation(d.normalized); mk.Box(picket, (a + b) / 2f + up * .7f, new Vector3(.05f, .08f, len), rr); mk.Box(picket, (a + b) / 2f + up * .35f, new Vector3(.05f, .08f, len), rr); var n = Mathf.RoundToInt(len / .5f); for (var k = 0; k <= n; k++) { var p = Vector3.Lerp(a, b, k / (float)n); p.y = ctx.Ground(p.x, p.z); mk.Box(picket, p + up * .5f, new Vector3(.1f, 1f + (k % 5 == 2 ? -.3f : 0f), .04f), rr * Quaternion.Euler(0, 0, (k * 37 % 7 - 3) * 1.5f)); } Col((a + b) / 2f + up * .5f, new Vector3(.12f, 1f, len), rr); }
            Fence(f.P(-hw, 0, -hd), f.P(hw, 0, -hd)); Fence(f.P(-hw, 0, -hd), f.P(-hw, 0, hd)); Fence(f.P(hw, 0, -hd), f.P(hw, 0, hd)); Fence(f.P(-hw, 0, hd), f.P(-1.6f, 0, hd)); Fence(f.P(1.6f, 0, hd), f.P(hw, 0, hd));
            // wooden crosses and round-topped boards, leaning every way; one has a jar of blue flowers
            var rng = new System.Random(27); var grey = WorldMaterials.Solid(new Color(.62f, .6f, .54f));
            for (var k = 0; k < 16; k++)
            {
                var p = f.P(-hw + 2f + (k % 4) * 2.5f + (float)rng.NextDouble() * .6f, 0, -hd + 2f + (k / 4) * 3.6f + (float)rng.NextDouble() * .8f); p.y = ctx.Ground(p.x, p.z); var lean = Quaternion.Euler((float)rng.NextDouble() * 16f - 8f, (float)rng.NextDouble() * 30f - 15f, (float)rng.NextDouble() * 20f - 10f);
                if (k % 3 == 0) { mk.Box(grey, p + lean * new Vector3(0, .5f, 0), new Vector3(.5f, 1f, .08f), lean); mk.Prism(grey, p + lean * new Vector3(0, 1f, -.04f), lean * Vector3.forward, .25f, .08f, 8); }
                else { mk.Box(grey, p + lean * new Vector3(0, .6f, 0), new Vector3(.09f, 1.3f, .09f), lean); mk.Box(grey, p + lean * new Vector3(0, .9f, 0), new Vector3(.7f, .09f, .09f), lean); }
                mk.Blob(WorldMaterials.Solid(new Color(.74f, .62f, .42f)), p + U * .9f + up * .06f, new Vector3(.45f, .1f, .9f), rng, .1f);
                if (k == 6) { mk.Prism(WorldMaterials.Solid(new Color(.7f, .8f, .86f), .9f), p + U * .4f, up, .07f, .2f, 6); for (var q = 0; q < 4; q++) mk.Blob(bottleM, p + U * .4f + up * (.32f + q * .03f) + Quaternion.Euler(0, q * 90f, 0) * Vector3.forward * .06f, Vector3.one * .045f, rng, .2f); }
            }
            DeadTree(ctx, mk, f.P(hw - 2.5f, 0, hd - 3f), 1.5f, rng); BottleTree(ctx, mk, f.P(hw - 3f, 0, -hd + 3f), 1.2f, rng);
            // the church, white, shut; a bell on a frame beside it
            var ch = new PlaceFrame(f.P(4.6f, 0, -2.5f), -R); const float W = 5f, D = 8f, H = 3.6f;
            mk.Box(white, ch.P(0, H / 2f, 0), new Vector3(W, H, D), ch.r); solid.Box(white, ch.P(0, H / 2f, 0), new Vector3(W, H, D), ch.r); mk.GableRoof(tin, ch.P(0, H, 0), new Vector3(W + .6f, 0, D + .6f), 2f, ch.r); foreach (var e in new[] { -1f, 1f }) mk.Prism(white, ch.P(0, H, e * (D / 2f - .02f)) + up * .0f, ch.Fwd * e, .02f, .02f, 3);
            mk.Box(white, ch.P(0, H + 2.4f, D / 2f - 1f), new Vector3(1.5f, 2.2f, 1.5f), ch.r); mk.Cone(tin, ch.P(0, H + 3.5f, D / 2f - 1f), 1.2f, 2.4f, 4, 45f); mk.Box(plankDark, ch.P(0, 1.15f, D / 2f + .02f), new Vector3(1.3f, 2.3f, .05f), ch.r); mk.Box(plank, ch.P(0, .1f, D / 2f + .5f), new Vector3(1.8f, .2f, .9f), ch.r);
            foreach (var e in new[] { -1f, 1f }) for (var k = 0; k < 2; k++) mk.Box(WorldMaterials.Solid(new Color(.2f, .24f, .34f), .8f), ch.P(e * (W / 2f + .01f), 2f, -1.8f + k * 3f), new Vector3(.04f, 1.6f, .7f), ch.r);
            var bl = ch.P(0, 0, D / 2f + 3.4f) + U * 3f; foreach (var e in new[] { -1f, 1f }) { mk.Prism(trunkM, bl + R * e * .8f - up * .2f, up, .09f, 3f, 5); ArtistBits.PostCollider(ctx.Root, bl + R * e * .8f, 2.8f, .1f); } mk.Box(trunkM, bl + up * 2.75f, new Vector3(2f, .12f, .12f), Quaternion.identity);
            var bell = new MeshKit(); bell.Prism(WorldMaterials.Solid(new Color(.5f, .42f, .24f), .7f), new Vector3(0, -.75f, 0), Vector3.up, .42f, .6f, 10, .45f); bell.Prism(iron, new Vector3(0, -.15f, 0), Vector3.up, .03f, .15f, 4); bell.Blob(iron, new Vector3(0, -.8f, 0), Vector3.one * .08f, rng, 0f);
            var bgo = bell.BuildSingle("The church bell", tlife.transform, false); bgo.transform.position = bl + up * 2.7f; tlife.bell = bgo.transform;
            string[] ring = { "One slow note goes out flat across the fields. Way off, a dog answers.", "The bell tolls. Out west the storm mutters back.", "It rings longer than it should, as if something far away had hold of the sound." }; var rn = 0;
            Interact.Toy(bgo, Toy.Kind.Custom, "Ring the bell", new Vector3(1.6f, 2.4f, 1.6f)).onUse = _ => { tlife.rung = Time.time; life.played = Time.time; Say(tlife.transform, bl + up * 3.6f, ring[rn++ % ring.Length], 5f); };
            var widow = Local(); widow.top = new Color(.1f, .1f, .12f); widow.bottom = new Color(.1f, .1f, .12f); widow.hat = Residents.Hat.None; widow.longHair = true;
            Folk(f.P(-4f, 0, 2f), -U, Resident.Pose.Stand, new[] { "I bring him blue flowers. He never liked any other colour.", "Half of these have no name on them. Guitar players, mostly. They went where the road went.", "Don't ring that bell after dark. It gets answered." }, widow);
            Mark(c, 15f);
            Shot(ctx, "the graveyard", f.P(-2f, 2.4f, hd + 9f), f.P(2f, 1.6f, -3f));
        }

        // ---------------------------------------------------------------- the general store
        void Store(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var c = ctx.OnGround(store2.x, store2.y); var f = new PlaceFrame(c, -U); var rng = new System.Random(19);
            var lane = new List<Vector3>(); for (var u = RoadW / 2f; u <= 22f - 3f - 2.6f; u += 2f) lane.Add(G(ctx, u, 96f)); if (lane.Count > 1) { Strip(mk, lane, dustM, PathW, .12f, ctx); foreach (var p in lane) Remember(p, 4); }
            var po = Shack(ctx, mk, solid, f, 9f, 6f, 2, false, true);
            mk.Box(white, f.P(0, 4.2f, 3.6f), new Vector3(6f, 1f, .08f), f.r); Sign(f.P(0, 4.35f, 3.66f), f.Fwd, "GENERAL STORE", .34f, Ink); Sign(f.P(0, 3.95f, 3.66f), f.Fwd, "ICE  ·  BAIT  ·  STRINGS", .16f, Rust);
            Rocker(mk, po.P(-3f, 0, 0), po.Fwd); Rocker(mk, po.P(-1.7f, 0, .1f), (po.Fwd + po.Right * .3f).normalized);
            var keeper = Local(Residents.Tool.None, true); keeper.hair = new Color(.7f, .7f, .68f);
            Folk(po.P(3.4f, 0, -.2f), po.Fwd, Resident.Pose.Lean, new[] { "Cold drinks in the chest. Strings behind the counter. Advice is free and worth it.", "Sold a fella a set of strings once, tall fella. Paid with a coin I couldn't spend. Still got it. Still cold.", "Checkers on the barrel if you've got an hour. Everybody's got an hour." }, keeper);
            // the ice chest with drinks to carry, the checkers barrel
            var ice = po.P(1.6f, 0, -.5f); mk.Box(WorldMaterials.Solid(new Color(.6f, .14f, .12f)), ice + up * .4f, new Vector3(1.2f, .8f, .6f), po.r); mk.Box(white, ice + up * .82f, new Vector3(1.24f, .05f, .64f), po.r); Col(ice + up * .4f, new Vector3(1.2f, .8f, .6f), po.r); Sign(ice + up * .45f + po.Fwd * .31f, po.Fwd, "COLD", .2f, Cream);
            for (var k = 0; k < 3; k++) { var bt = new MeshKit(); bt.Prism(WorldMaterials.Glow(k == 1 ? new Color(.3f, .5f, 1f) : new Color(.6f, .34f, .14f), .3f), Vector3.zero, Vector3.up, .04f, .16f, 6); bt.Prism(WorldMaterials.Glow(new Color(.6f, .34f, .14f), .3f), new Vector3(0, .16f, 0), Vector3.up, .018f, .08f, 5); Thing(bt, "Cold drink", k == 1 ? "a cold blue soda" : "a cold sarsaparilla", ice + up * .86f + po.Right * (-.36f + k * .36f), Quaternion.identity, new Vector3(.1f, .26f, .1f), .2f, false); }
            var br = G(ctx, 22f - 3f - 3.4f, 96f - 4.6f); mk.Prism(plank, br, up, .42f, .9f, 10); mk.Prism(iron, br + up * .2f, up, .44f, .05f, 10); mk.Prism(iron, br + up * .7f, up, .44f, .05f, 10); mk.Box(dark, br + up * .92f, new Vector3(.7f, .03f, .7f), Quaternion.identity); for (var k = 0; k < 8; k++) mk.Prism(k % 2 == 0 ? rustM : white, br + up * .94f + new Vector3(-.26f + (k % 4) * .17f, 0, -.2f + (k / 4) * .34f), up, .05f, .02f, 8); ArtistBits.PostCollider(ctx.Root, br, .9f, .44f);
            Crate(mk, br + R * .95f, -R); Crate(mk, br - R * .95f, R); string[] chk = { "You jump one. An old voice behind you says \"hm\", which is the worst thing it could say.", "King me. The barrel creaks approvingly.", "You take your time. Time's the one thing this porch has plenty of." }; var cn = 0;
            Spot("Checkers", br + up * 1.1f, "Make a move", new Vector3(.9f, .5f, .9f)).onUse = _ => Say(tlife.transform, br + up * 2.2f, chk[cn++ % chk.Length], 4.5f);
            // a gas pump gone to rust, a bulb over the porch
            var gp = G(ctx, 22f - 3f - 4f, 96f + 5.6f); mk.Box(rustM, gp + up * .9f, new Vector3(.6f, 1.8f, .4f), Quaternion.identity); mk.Prism(white, gp + up * 1.8f, up, .3f, .4f, 10); mk.Prism(dark, gp + up * 1.2f + R * .32f, -up, .02f, .9f, 4); Col(gp + up * .9f, new Vector3(.6f, 1.8f, .4f), Quaternion.identity);
            ArtistBits.PointLight(ctx.Root, po.P(0, 2f, 0), Bulb, 10f, 1.4f); mk.Blob(bulbM, po.P(0, 2.2f, 0), new Vector3(.07f, .1f, .07f), rng, 0f); halos.Add(po.P(0, 2.2f, 0), .9f, Bulb, .45f);
            Mark(c, 9f);
            Shot(ctx, "the general store", G(ctx, 4f, 96f - 9f) + up * 2.2f, f.P(0, 2.4f, 0));
        }
    }

    /// The Delta's small moving parts: the windpump's wheel, the church bell swinging after it is rung.
    public class DeltaTownLife : MonoBehaviour
    {
        public Transform wheel, bell; public Quaternion wheelRot; public float rung = -99f; float spin;

        void Update()
        {
            var t = Time.time; spin += Time.deltaTime * (40f + 50f * Shader.GetGlobalFloat("_EW_Wind"));
            if (wheel != null) wheel.rotation = wheelRot * Quaternion.Euler(0, 0, spin);
            if (bell != null) { var since = t - rung; bell.localRotation = Quaternion.Euler(0, 0, since < 6f ? Mathf.Sin(since * 4f) * 28f * (1f - since / 6f) : 0f); }
        }
    }
}
