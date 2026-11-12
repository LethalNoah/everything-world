using System.Collections.Generic;
using UnityEngine;
using static EverythingWorld.World.PlaceTools;

namespace EverythingWorld.World
{
    /// The Fusion Isthmus' town (part of FusionKit): the square (a skate ledge, boards to carry), Brass Lane, the canal
    /// with its quays, railings, strings of lights and three bridges (the middle one the Piano Bridge), the rows of
    /// brick houses, and the places: the Rooftop (up the outside stair: a trio short of a drummer, so sit in; a bar;
    /// mismatched chairs), What Kinda Coffee (a café to walk into: a beat machine to tap, coffee and records to carry,
    /// a cat), Potted (plants to carry off), the houseboat (walk aboard), and buskers out on the corners.
    public partial class FusionKit
    {
        FusionTownLife tlife; System.Random prng; Transform rootT; LandContext ctx0;

        static readonly Color[] Knits = { new Color(.2f, .46f, .5f), new Color(.86f, .66f, .28f), new Color(.66f, .36f, .27f), new Color(.3f, .32f, .58f), new Color(.9f, .86f, .76f), new Color(.5f, .6f, .4f), new Color(.8f, .5f, .5f) };
        static readonly Color[] Denim = { new Color(.22f, .28f, .42f), new Color(.16f, .16f, .2f), new Color(.5f, .44f, .36f), new Color(.3f, .34f, .3f) };
        static readonly string[] TownTalk =
        {
            "Lovely evening. It's been this exact evening for a while now and nobody's complaining.", "There's a band on at the Rooftop. There's always a band on. Tonight they're short a drummer.",
            "The canal keeps time if you watch it. Slow four. Sometimes five, if it's feeling clever.", "You can hear three different tunes from here and somehow they're all in the same key.", "No rush. That's the town motto. It's on a sign somewhere; nobody's got round to putting it up.",
        };

        void TownStart(LandContext ctx)
        {
            rootT = ctx.Root; ctx0 = ctx; prng = new System.Random(1985);
            tlife = new GameObject("Fusion town life").AddComponent<FusionTownLife>(); tlife.transform.SetParent(life.transform, false);
        }

        Residents.Look Local(Residents.Tool tool = Residents.Tool.None, bool apron = false) { var l = Residents.Random(prng, Knits, Denim, new[] { Residents.Hat.None, Residents.Hat.Beanie, Residents.Hat.Cap, Residents.Hat.None, Residents.Hat.Bucket }, Knits); l.tool = tool; l.apron = apron; return l; }
        Resident Folk(Vector3 feet, Vector3 face, Resident.Pose pose, string[] lines = null, Residents.Look look = null, Vector3[] route = null, float seatH = HumanScale.SeatHeight)
            => Residents.Make(tlife.transform, feet, face, look ?? Local(), pose, prng, lines ?? TownTalk, route, seatH);
        void Col(Vector3 c, Vector3 size, Quaternion rot) => BoxCol(rootT, c, size, rot);
        void Mark(Vector3 at, float r) => noGrass.Add(new Vector3(at.x, r, at.z));
        void Sign(Vector3 at, Vector3 face, string text, float size, Color c) => ArtistBits.Label(rootT, text, at, face, size, c, TextAnchor.MiddleCenter);
        /// A point in the town: 'al' metres along the canal, 'pe' metres across it (north positive), 'y' above the quay.
        Vector3 P(float al, float pe, float y = 0f) => canalC + A * al + Nrm * pe + Vector3.up * y;

        Carryable Thing(MeshKit m, string name, string label, Vector3 at, Quaternion rot, Vector3 size, float mass, bool still = true)
        {
            var go = m.BuildSingle(name, tlife.transform, false); go.transform.SetPositionAndRotation(at, rot); var c = Interact.Carry(go, label, mass, at.y - 30f);
            var bc = c.GetComponent<BoxCollider>(); if (bc != null) { bc.size = size; bc.center = new Vector3(0, size.y / 2f, 0); }
            if (still) { var rb = go.GetComponent<Rigidbody>(); if (rb != null) rb.isKinematic = true; }
            return c;
        }

        /// Brass letters on a painted board.
        void BrassSign(MeshKit mk, Vector3 c, Vector3 face, float w, float h, string text, float size, Color board)
        {
            var rot = Quaternion.LookRotation(face); mk.Box(brassM, c - face * .02f, new Vector3(w + .16f, h + .16f, .08f), rot); mk.Box(WorldMaterials.Solid(board), c, new Vector3(w, h, .1f), rot); Sign(c + face * .06f, face, text, size, Brass); halos.Add(c + face * .5f, Mathf.Max(w, h) * .45f, Brass, .28f);
        }

        /// A room to walk into: floor, three walls, a front wall with a doorway at dx, a flat ceiling. 'fo' sits on the floor.
        void Room(MeshKit mk, MeshKit solid, PlaceFrame fo, float W, float D, float H, float T, float dx, float door, Material wall, Material floor)
        {
            void Wall(Vector3 p, Vector3 s) { mk.Box(wall, p, s, fo.r); solid.Box(wall, p, s, fo.r); }
            mk.Box(floor, fo.P(0, -.06f, 0), new Vector3(W, .12f, D), fo.r); solid.Box(floor, fo.P(0, -.5f, 0), new Vector3(W, 1f, D), fo.r);
            Wall(fo.P(0, H / 2f, -D / 2f + T / 2f), new Vector3(W, H, T)); foreach (var e in new[] { -1f, 1f }) Wall(fo.P(e * (W / 2f - T / 2f), H / 2f, 0), new Vector3(T, H, D - T * 2f));
            float l = W / 2f + dx - door / 2f, r = W / 2f - dx - door / 2f;
            if (l > .05f) Wall(fo.P(-W / 2f + l / 2f, H / 2f, D / 2f - T / 2f), new Vector3(l, H, T)); if (r > .05f) Wall(fo.P(W / 2f - r / 2f, H / 2f, D / 2f - T / 2f), new Vector3(r, H, T));
            var dh = HumanScale.DoorHeight + .2f; Wall(fo.P(dx, (H + dh) / 2f, D / 2f - T / 2f), new Vector3(door, H - dh, T));
            mk.Box(wall, fo.P(0, H + .1f, 0), new Vector3(W, .2f, D), fo.r); solid.Box(wall, fo.P(0, H + .1f, 0), new Vector3(W, .2f, D), fo.r);
            IndoorSpaces.Add(fo.P(0, H / 2f, 0), fo.r, new Vector3(W - T * 2f - .2f, H, D - T * 2f - .2f));
        }

        // ---------------------------------------------------------------- the square
        void Square(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var c = ctx.OnGround(center.x, center.z);
            mk.Prism(paving, c - up * .28f, up, 18f, .4f, 24); mk.Prism(brassM, c + up * .13f, up, 2.4f, .02f, 16); mk.Prism(paving, c + up * .135f, up, 2.1f, .02f, 16); for (var k = 0; k < 8; k++) { var d = Quaternion.Euler(0, k * 45f, 0) * A; mk.Box(brassM, c + d * 1.2f + up * .15f, new Vector3(.12f, .02f, 1.6f), Quaternion.LookRotation(d)); }
            // the sign, lamps, benches, pots
            var sg = c + A * 12f - Nrm * 11f + up * .13f; var sf = (-(A - Nrm)).normalized; foreach (var e in new[] { -2.4f, 2.4f }) { mk.Prism(iron, sg + Vector3.Cross(up, sf) * e - up * .2f, up, .1f, 3.6f, 6); ArtistBits.PostCollider(ctx.Root, sg + Vector3.Cross(up, sf) * e, 3.4f, .12f); }
            BrassSign(mk, sg + up * 2.8f, sf, 5.4f, 1.5f, "THE FUSION ISTHMUS", .36f, new Color(.12f, .34f, .38f)); Sign(sg + up * 1.75f + sf * .04f, sf, "no rush", .2f, Cream);
            for (var k = 0; k < 4; k++) { var d = Quaternion.Euler(0, k * 90f + 45f, 0) * A; BrassLamp(ctx, mk, c + d * 15f + up * .13f, -d, true); }
            Bench(mk, c - A * 11f - Nrm * 6f + up * .13f, (A + Nrm * .5f).normalized); Bench(mk, c + A * 10f + Nrm * 8f + up * .13f, (-A - Nrm * .6f).normalized); Bench(mk, c - A * 4f - Nrm * 13f + up * .13f, Nrm);
            var prr = new System.Random(4); for (var k = 0; k < 6; k++) { var d = Quaternion.Euler(0, k * 60f + 15f, 0) * A; Pot(mk, c + d * 16.5f + up * .13f, 1.6f, prr); }
            // the skate ledge: a low stone block to grind (or sit on), boards to carry, a skater going round
            var lg = c - A * 7f + Nrm * 5f + up * .13f; var lr = Quaternion.LookRotation((A + Nrm * .3f).normalized); mk.Box(stoneM, lg + up * .25f, new Vector3(.7f, .5f, 6f), lr); mk.Box(brassM, lg + up * .5f + (lr * Vector3.right) * .33f, new Vector3(.06f, .04f, 6f), lr); Col(lg + up * .25f, new Vector3(.7f, .5f, 6f), lr);
            Interact.Bench(tlife.transform, lg + up * .52f - (lr * Vector3.forward) * 2f, lg + up * .52f + (lr * Vector3.forward) * 2f, lr * Vector3.right);
            for (var k = 0; k < 3; k++) { var bm = new MeshKit(); bm.Box(WorldMaterials.Solid(Paints[(k + 2) % Paints.Length]), new Vector3(0, .09f, 0), new Vector3(.2f, .02f, .78f), Quaternion.identity); foreach (var e in new[] { -.26f, .26f }) foreach (var s in new[] { -.08f, .08f }) bm.Prism(white, new Vector3(s - .015f * Mathf.Sign(s), .035f, e), Vector3.right * Mathf.Sign(s), .035f, .03f, 8); Thing(bm, "Skateboard", "a skateboard", lg + (lr * Vector3.right) * (.9f + k * .35f) + (lr * Vector3.forward) * (-1.5f + k * 1.4f) + up * .02f, lr * Quaternion.Euler(0, k * 25f, 0), new Vector3(.24f, .12f, .8f), .6f); }
            var ring = new List<Vector3>(); for (var k = 0; k < 12; k++) ring.Add(c + Quaternion.Euler(0, k * 30f, 0) * A * 12.5f + up * .13f);
            Folk(ring[0], FaceTo(ring[0], ring[1]), Resident.Pose.Walk, new[] { "Watch this. ...Okay I didn't land it. Watch this.", "The ledge has got a brass edge. Somebody in this town understands." }, null, ring.ToArray()).speed = 2.6f;
            Folk(c + Nrm * 9f - A * 3f + up * .13f, -Nrm, Resident.Pose.Wave, new[] { "Evening! Canal's up Brass Lane, and the Rooftop's just over the Piano Bridge.", "You'll hear it before you see it. Follow the ride cymbal.", "First time? Take the stairs to the roof. Sit in if you can keep time." });
            Mark(c, 19f);
            // Brass Lane, from the square up to the south quay
            var lane = new List<Vector3>(); for (var d = 17f; d <= CanalOff - CanalHalf - QuayW + .5f; d += 2f) lane.Add(ctx.OnGround(c.x + Nrm.x * d, c.z + Nrm.z * d)); if (lane.Count > 1) { Strip(mk, lane, paving, 5f, .15f, ctx); foreach (var p in lane) Remember(p, 1); for (var i = 2; i < lane.Count; i += 5) BrassLamp(ctx, mk, ctx.OnGround(lane[i].x + A.x * (i % 2 == 0 ? 3.3f : -3.3f), lane[i].z + A.z * (i % 2 == 0 ? 3.3f : -3.3f)), A * (i % 2 == 0 ? -1f : 1f), true); }
            Shot(ctx, "the square", c - Nrm * 15f - A * 5f + up * 2.4f, c + Nrm * 40f + up * 5f);
        }

        // ---------------------------------------------------------------- the canal
        void Canal(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var along = Quaternion.LookRotation(A); var L = canalL; float[] bridges = { 0f, -72f, 72f }; float[] widths = { 5.4f, 3f, 3f };
            bool AtBridge(float al, float pad) { for (var k = 0; k < bridges.Length; k++) if (Mathf.Abs(al - bridges[k]) < widths[k] / 2f + pad && Mathf.Abs(bridges[k]) < L - 6f) return true; return false; }
            // the water, the brick walls, the stone coping
            mk.Box(waterM, P(0, 0, -1.25f), new Vector3(CanalHalf * 2f + .4f, .1f, L * 2f), along);
            foreach (var e in new[] { -1f, 1f })
            {
                mk.Box(brickDark, P(0, e * (CanalHalf + .3f), -1.4f), new Vector3(.6f, 2.9f, L * 2f + 1f), along); mk.Box(stoneM, P(0, e * (CanalHalf + .3f), .09f), new Vector3(.8f, .18f, L * 2f + 1f), along);
                mk.Box(paving, P(0, e * (CanalHalf + .6f + QuayW / 2f), .06f), new Vector3(QuayW, .14f, L * 2f + 20f), along);
                for (var al = -L - 8f; al <= L + 8f; al += 3f) Remember(P(al, e * (CanalHalf + 3.6f), .1f), e > 0 ? 3 : 2);
                mk.Box(brickDark, P(e * L, 0, -1.4f), new Vector3(CanalHalf * 2f + 1.2f, 2.9f, .7f), along); mk.Prism(dark, P(e * (L - .37f), 0, -1.3f), A * -e, 2.6f, .06f, 10); mk.Box(stoneM, P(e * L, 0, .09f), new Vector3(CanalHalf * 2f + 1.6f, .18f, .9f), along);
                // railings, open where the bridges land
                var start = -L - .4f;
                for (var al = -L - .4f; al <= L + .41f; al += .8f)
                {
                    var gap = AtBridge(al, .5f); var last = al + .8f > L + .41f;
                    if (!gap) { mk.Prism(iron, P(al, e * (CanalHalf + .62f), .1f), up, .03f, 1f, 4); }
                    if (gap || last) { var end = gap ? al - .8f : al; if (end - start > .5f) { var mid = (start + end) / 2f; mk.Box(iron, P(mid, e * (CanalHalf + .62f), 1.08f), new Vector3(.07f, .06f, end - start), along); mk.Box(iron, P(mid, e * (CanalHalf + .62f), .6f), new Vector3(.04f, .04f, end - start), along); Col(P(mid, e * (CanalHalf + .62f), .6f), new Vector3(.12f, 1.2f, end - start + .3f), along); } start = al + .8f; }
                }
                Col(P(e * (L + .3f), 0, .6f), new Vector3(CanalHalf * 2f + 1.6f, 1.2f, .12f), along); mk.Box(iron, P(e * (L + .3f), 0, 1.08f), new Vector3(CanalHalf * 2f + 1.4f, .06f, .07f), along);
            }
            // lamps on both quays with lights strung across the water between them
            var n = 0; for (var al = -L + 12f; al <= L - 12f; al += 20f, n++)
            {
                if (AtBridge(al, 3f)) continue;
                foreach (var e in new[] { -1f, 1f }) BrassLamp(ctx, mk, P(al, e * (CanalHalf + 1.5f), .12f), Nrm * e, n % 2 == 0);
                BulbString(mk, P(al, -(CanalHalf + 1.5f), 3.85f), P(al, CanalHalf + 1.5f, 3.85f), n);
            }
            // the Piano Bridge: a stone deck laid out as keys, a rail of keys each side
            var bw = widths[0]; var span = CanalHalf * 2f + 2.4f; mk.Box(stoneM, P(0, 0, -.05f), new Vector3(bw, .4f, span), Quaternion.LookRotation(Nrm)); solid.Box(stoneM, P(0, 0, -.05f), new Vector3(bw, .4f, span), Quaternion.LookRotation(Nrm));
            var keys = Mathf.RoundToInt(span / .5f); var keyM = new Material(WorldMaterials.Glow(new Color(.97f, .96f, .92f), .25f)) { name = "Piano Bridge keys" }; tlife.keysM = keyM;
            for (var k = 0; k < keys; k++) { var pe = -span / 2f + (k + .5f) * span / keys; mk.Box(keyM, P(0, pe, .17f), new Vector3(bw - .5f, .04f, span / keys - .05f), Quaternion.LookRotation(Nrm)); if (k % 7 != 2 && k % 7 != 6) mk.Box(dark, P(-bw * .16f, pe + span / keys / 2f, .2f), new Vector3(bw * .5f, .05f, span / keys * .55f), Quaternion.LookRotation(Nrm)); }
            foreach (var e in new[] { -1f, 1f }) { for (var k = 0; k <= keys; k++) { var pe = -span / 2f + k * span / keys; mk.Box(k % 2 == 0 ? white : dark, P(e * (bw / 2f - .1f), pe, .7f), new Vector3(.12f, 1f, .2f), Quaternion.LookRotation(Nrm)); } mk.Box(brassM, P(e * (bw / 2f - .1f), 0, 1.24f), new Vector3(.16f, .08f, span), Quaternion.LookRotation(Nrm)); Col(P(e * (bw / 2f - .1f), 0, .7f), new Vector3(.2f, 1.3f, span), Quaternion.LookRotation(Nrm)); }
            for (var pe = -span / 2f - 2f; pe <= span / 2f + 2f; pe += 2f) Remember(P(0, pe, .15f), 1);
            var bgo = new GameObject("Piano Bridge"); bgo.transform.SetParent(tlife.transform, false); bgo.transform.position = P(0, 0, .6f); var bc = bgo.AddComponent<BoxCollider>(); bc.isTrigger = true; bc.size = new Vector3(bw - .6f, 1.4f, span - 1f); tlife.bridgeAt = P(0, 0, .2f); tlife.bridgeHalf = span / 2f; tlife.bridgeAxis = Nrm;
            // two plank bridges
            for (var k = 1; k < 3; k++)
            {
                if (Mathf.Abs(bridges[k]) > L - 6f) continue; var al = bridges[k]; mk.Box(woodM, P(al, 0, .02f), new Vector3(widths[k], .24f, span), Quaternion.LookRotation(Nrm)); solid.Box(woodM, P(al, 0, .02f), new Vector3(widths[k], .24f, span), Quaternion.LookRotation(Nrm));
                foreach (var e in new[] { -1f, 1f }) { mk.Box(iron, P(al + e * (widths[k] / 2f - .06f), 0, 1.08f), new Vector3(.07f, .06f, span), Quaternion.LookRotation(Nrm)); for (var s = -span / 2f; s <= span / 2f + .01f; s += span / 8f) mk.Prism(iron, P(al + e * (widths[k] / 2f - .06f), s, .1f), up, .03f, 1f, 4); Col(P(al + e * (widths[k] / 2f - .06f), 0, .65f), new Vector3(.12f, 1.2f, span), Quaternion.LookRotation(Nrm)); }
            }
            // the houseboat, moored on the north side; a gangplank down from the quay
            var hb = -38f; if (hb > -L + 10f)
            {
                var o = P(hb, CanalHalf - 2f, 0); var teal = WorldMaterials.Solid(new Color(.14f, .36f, .4f));
                mk.Box(teal, o - up * .8f, new Vector3(3.4f, 1.1f, 13f), along); solid.Box(teal, o - up * .8f, new Vector3(3.4f, 1.1f, 13f), along); mk.Box(brassM, o - up * .28f, new Vector3(3.5f, .06f, 13.1f), along); mk.Box(woodM, o - up * .24f, new Vector3(3.2f, .04f, 12.8f), along);
                mk.Box(WorldMaterials.Solid(new Color(.3f, .32f, .58f)), o + up * .65f - A * 1.5f, new Vector3(2.8f, 1.8f, 5.4f), along); solid.Box(teal, o + up * .65f - A * 1.5f, new Vector3(2.8f, 1.8f, 5.4f), along); mk.Box(woodM, o + up * 1.6f - A * 1.5f, new Vector3(3f, .1f, 5.6f), along);
                for (var k = 0; k < 3; k++) mk.Box(windowM, o + up * .8f - A * (3.2f - k * 1.7f) - Nrm * 1.41f, new Vector3(.04f, .7f, .9f), along); var prr = new System.Random(9); for (var k = 0; k < 4; k++) Pot(mk, o + up * 1.65f - A * (3.4f - k * 1.2f) + Nrm * ((k % 2) * .8f - .4f), .9f, prr);
                Sign(o + up * .1f - Nrm * 1.72f + A * 3.5f, -Nrm, "THE ROOT NOTE", .26f, Brass);
                var gp = P(hb + 3.6f, CanalHalf - .1f, -.14f); mk.Box(woodM, gp, new Vector3(1.1f, .08f, 1.9f), Quaternion.LookRotation(Nrm) * Quaternion.Euler(-8f, 0, 0)); solid.Box(woodM, gp - up * .1f, new Vector3(1.1f, .26f, 2.2f), Quaternion.LookRotation(Nrm) * Quaternion.Euler(-8f, 0, 0));
                var dc = o + A * 4.4f - up * .22f; mk.Box(WorldMaterials.Solid(Paints[4]), dc + up * .4f, new Vector3(.7f, .08f, 1.2f), along * Quaternion.Euler(-14f, 0, 0)); mk.Box(woodM, dc + up * .2f, new Vector3(.74f, .4f, 1f), along); Interact.Seat(tlife.transform, dc + up * .5f, -Nrm, "Sit in the deck chair").seatHeight = .5f;
                Folk(o + A * 2.2f - up * .22f + Nrm * .4f, -Nrm, Resident.Pose.Sit, new[] { "I moved onto the boat for the quiet. Then the Rooftop opened. Now I've got the best seat in town and I never sleep.", "She doesn't go anywhere. Engine's a plant pot now.", "I practise up top at night. The water sends it back a beat late. It's like playing with a very patient friend." }, null, null, .3f);
                ArtistBits.PointLight(ctx.Root, o + up * 2.4f + A * 2f, Brass, 12f, 1.8f);
            }
            // a little rowing boat that drifts the length of the canal and back
            var rm = new MeshKit(); rm.Box(woodM, new Vector3(0, .12f, 0), new Vector3(1.2f, .36f, 3f), Quaternion.identity); rm.Box(WorldMaterials.Solid(Paints[2]), new Vector3(0, .32f, 0), new Vector3(1.3f, .06f, 3.1f), Quaternion.identity); rm.Blob(bulbM[0], new Vector3(0, .7f, 1.3f), Vector3.one * .12f, prng, 0f); rm.Prism(iron, new Vector3(0, .3f, 1.3f), Vector3.up, .02f, .4f, 4);
            var rgo = rm.BuildSingle("Rowing boat", tlife.transform, false); tlife.rowboat = rgo.transform; tlife.rowA = P(-L + 5f, -1.6f, -1.2f); tlife.rowB = P(L - 5f, -1.6f, -1.2f);
            Shot(ctx, "the canal and the piano bridge", P(-16f, -(CanalHalf + 4f), 2.2f), P(22f, 4f, 4f));
            Shot(ctx, "along the canal", P(-L + 14f, CanalHalf + 3.5f, 2f), P(L, 0, 2f));
        }

        // ---------------------------------------------------------------- the houses
        void Houses(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var back = CanalHalf + .6f + QuayW + HouseD / 2f; var i = 0;
            foreach (var e in new[] { 1f, -1f })
            for (var al = -canalL + 8f; al <= canalL - 7f; al += 12f, i++)
            {
                if (Mathf.Abs(al) < 7.5f) continue;                       // Brass Lane comes through here
                if (e > 0 && al > 7f && al < 29f) continue;               // the Rooftop
                if (e < 0 && Mathf.Abs(al + 16f) < 6.1f) continue;        // the café
                RowHouse(ctx, mk, solid, new PlaceFrame(P(al, e * back), -Nrm * e), i, e > 0 && Mathf.Abs(al + 16f) < 6.1f);
            }
            Rooftop(ctx, mk, solid, new PlaceFrame(P(18f, back), -Nrm));
            Cafe(ctx, mk, solid, new PlaceFrame(P(-16f, -back), Nrm));
            // Brass Lane carries on north between the houses to a little green with a tree
            var lane = new List<Vector3>(); for (var d = CanalHalf + QuayW; d <= back + HouseD / 2f + 30f; d += 2f) lane.Add(ctx.OnGround(canalC.x + Nrm.x * d, canalC.z + Nrm.z * d)); Strip(mk, lane, paving, 5f, .15f, ctx); foreach (var p in lane) Remember(p, 4);
            var green = lane[lane.Count - 1]; mk.Prism(paving, green - up * .25f, up, 7f, .38f, 16); PlaneTree(ctx, mk, green + up * .1f, 1.5f, new System.Random(3)); for (var k = 0; k < 3; k++) { var d = Quaternion.Euler(0, k * 120f + 60f, 0) * A; Bench(mk, green + d * 4.6f + up * .13f, d); } Mark(green, 8f);
            for (var k = 0; k < 2; k++) BrassLamp(ctx, mk, ctx.OnGround(lane[4 + k * 8].x + A.x * 3.2f, lane[4 + k * 8].z + A.z * 3.2f), -A, true);
            // tables by the water outside the Rooftop
            for (var k = 0; k < 2; k++) { var tp = P(10f + k * 6f, CanalHalf + 2.2f, .12f); mk.Prism(iron, tp, up, .04f, .72f, 5); mk.Prism(woodM, tp + up * .72f, up, .45f, .05f, 10); ArtistBits.PostCollider(ctx.Root, tp, .8f, .36f); Chair(mk, tp + A * .9f, -A, k); Chair(mk, tp - A * .9f, A, k + 4); }
            Folk(P(10f + .9f, CanalHalf + 2.2f, .62f), -A, Resident.Pose.Sit, new[] { "We come down here between sets. You can still hear it, and the chairs don't match either.", "That bass player's been holding one note for a minute and it's the best thing I've heard all day." }, Local(Residents.Tool.Mug), null, .5f);
            // a pair strolling the south quay
            var route = new List<Vector3>(); for (var al = -canalL + 6f; al <= canalL - 6f; al += 12f) route.Add(P(al, -(CanalHalf + 2.6f), .14f)); for (var al = canalL - 6f; al >= -canalL + 6f; al -= 12f) route.Add(P(al, -(CanalHalf + 5.4f), .14f));
            Folk(route[0], A, Resident.Pose.Walk, new[] { "Up one quay, over the bridge, down the other. By the time you're back the tune's changed.", "I count the houseboat's plants every evening. There's always one more." }, null, route.ToArray()).speed = .95f;
        }

        /// A narrow brick house on the quay: two or three storeys, lit windows with flower boxes, a door, sometimes a bike.
        void RowHouse(LandContext ctx, MeshKit mk, MeshKit solid, PlaceFrame f, int i, bool plantShop)
        {
            var up = Vector3.up; const float W = 11.6f; var st = 2 + (i * 7 % 3 == 0 ? 1 : 0); var H = st * 3.2f + .6f; var paint = WorldMaterials.Solid(Paints[i % Paints.Length]); var rng = new System.Random(i * 13 + 5);
            mk.Box(paint, f.P(0, H / 2f - .3f, 0), new Vector3(W, H + .6f, HouseD), f.r); solid.Box(paint, f.P(0, H / 2f - .3f, 0), new Vector3(W, H + .6f, HouseD), f.r); mk.Box(stoneM, f.P(0, H + .1f, HouseD / 2f - .1f), new Vector3(W + .2f, .24f, .5f), f.r);
            if (i % 2 == 0) foreach (var e in new[] { -1f, 1f }) mk.Box(brickDark, f.P(0, H + 1.1f, e * HouseD / 4f), new Vector3(W, .16f, HouseD / 2f + .7f), f.r * Quaternion.Euler(-e * 32f, 0, 0)); else mk.Box(brickDark, f.P(0, H + .5f, 0), new Vector3(W - .6f, .8f, HouseD - .6f), f.r);
            mk.Box(brickDark, f.P(W / 2f - 1.6f, H + 1.7f, -1f), new Vector3(.8f, 2f, .8f), f.r);
            // the door, the ground-floor window, a step, pots
            var dx = (i % 2 == 0 ? -1f : 1f) * (W / 2f - 2f); mk.Box(WorldMaterials.Solid(Paints[(i + 3) % Paints.Length] * .7f), f.P(dx, 1.1f, HouseD / 2f + .02f), new Vector3(1.05f, 2.2f, .06f), f.r); mk.Box(stoneM, f.P(dx, 2.3f, HouseD / 2f + .03f), new Vector3(1.4f, .16f, .1f), f.r); mk.Box(stoneM, f.P(dx, .08f, HouseD / 2f + .4f), new Vector3(1.6f, .16f, .7f), f.r); mk.Blob(brassM, f.P(dx + .34f, 1.1f, HouseD / 2f + .07f), Vector3.one * .05f, rng, 0f);
            mk.Box(windowM, f.P(-dx * .45f, 1.7f, HouseD / 2f + .02f), new Vector3(3.6f, 1.8f, .06f), f.r); mk.Box(white, f.P(-dx * .45f, 1.7f, HouseD / 2f + .04f), new Vector3(.08f, 1.8f, .06f), f.r); halos.Add(f.P(-dx * .45f, 1.7f, HouseD / 2f + .9f), 2f, Brass, .3f);
            Pot(mk, f.P(dx - 1f, .02f, HouseD / 2f + .5f), 1.1f, rng); if (i % 3 != 0) Pot(mk, f.P(dx + 1f, .02f, HouseD / 2f + .5f), .9f, rng, false);
            // upper windows, some lit, each with a box of flowers
            for (var s = 1; s < st; s++) for (var k = 0; k < 3; k++)
            {
                var wp = f.P(-W / 2f + 2.2f + k * 3.6f, s * 3.2f + 1.9f, HouseD / 2f + .02f); var lit = (i + k * 2 + s) % 3 != 0; mk.Box(lit ? windowM : glassDark, wp, new Vector3(1.3f, 1.7f, .06f), f.r); mk.Box(white, wp + up * .9f, new Vector3(1.5f, .1f, .1f), f.r);
                mk.Box(brickDark, wp - up * .98f + f.Fwd * .16f, new Vector3(1.4f, .22f, .3f), f.r); for (var q = 0; q < 3; q++) mk.Blob(q == 1 ? leafM : WorldMaterials.Glow(Bloom[(i + k + q) % Bloom.Length], .35f), wp - up * .8f + f.Fwd * .18f + f.Right * (-.4f + q * .4f), Vector3.one * .16f, rng, .3f);
            }
            if (i % 4 == 1)
            {   // a bicycle leaning by the door
                var b = f.P(-dx * .9f, .02f, HouseD / 2f + .5f); foreach (var e in new[] { -.5f, .5f }) { mk.Prism(dark, b + up * .34f + f.Right * e - f.Fwd * .02f, f.Fwd, .34f, .04f, 12); mk.Prism(WorldMaterials.Solid(Paints[i % 3]), b + up * .34f + f.Right * e - f.Fwd * .03f, f.Fwd, .28f, .06f, 10); }
                mk.Box(WorldMaterials.Solid(Paints[(i + 2) % Paints.Length]), b + up * .6f, new Vector3(1f, .05f, .05f), f.r * Quaternion.Euler(0, 0, 12f)); mk.Box(dark, b + up * .9f - f.Right * .35f, new Vector3(.24f, .05f, .12f), f.r); mk.Box(dark, b + up * .95f + f.Right * .5f, new Vector3(.05f, .05f, .46f), f.r); Col(b + up * .5f, new Vector3(1.7f, 1f, .3f), f.r);
            }
            if (plantShop)
            {   // Potted: shelves of plants on the pavement, a sign, the gardener; three to carry off
                BrassSign(mk, f.P(0, 3.3f, HouseD / 2f + .1f), f.Fwd, 4f, .9f, "POTTED", .4f, new Color(.16f, .34f, .26f));
                for (var s = 0; s < 2; s++) { mk.Box(woodM, f.P(-1.6f, .5f + s * .6f, HouseD / 2f + 1.2f + s * -.3f), new Vector3(4.2f, .06f, .5f), f.r); for (var k = 0; k < 6; k++) Pot(mk, f.P(-3.4f + k * .72f, .53f + s * .6f, HouseD / 2f + 1.2f + s * -.3f), .7f + (k % 3) * .15f, rng); } Col(f.P(-1.6f, .6f, HouseD / 2f + 1.1f), new Vector3(4.2f, 1.2f, .8f), f.r);
                string[] plants = { "a fern in a pot", "a small lemon tree", "a cactus (careful)" }; for (var k = 0; k < 3; k++) { var pm = new MeshKit(); pm.Prism(WorldMaterials.Solid(new Color(.72f, .42f, .3f)), Vector3.zero, Vector3.up, .14f, .24f, 8, 1.3f); pm.Blob(k == 2 ? leafDark : leafM, new Vector3(0, .46f, 0), k == 2 ? new Vector3(.1f, .26f, .1f) : new Vector3(.24f, .26f, .24f), rng, .3f); if (k == 1) pm.Blob(WorldMaterials.Glow(new Color(1f, .9f, .3f), .5f), new Vector3(.12f, .5f, .08f), Vector3.one * .06f, rng, 0f); Thing(pm, "Plant", plants[k], f.P(2.2f + k * .7f, .04f, HouseD / 2f + 1.1f), f.r, new Vector3(.4f, .7f, .4f), .5f); }
                Folk(f.P(4.6f, .02f, HouseD / 2f + 1.3f), f.Fwd, Resident.Pose.Work, new[] { "Take one. Talk to it. It likes minor sevenths.", "Everything in this town grows slow. That's why it grows well.", "The cactus is free to anyone who can name its chord." }, Local(Residents.Tool.Hose, true)).workRate = .6f;
            }
            Mark(f.o, 7f);
        }

        // ---------------------------------------------------------------- the Rooftop
        void Rooftop(LandContext ctx, MeshKit mk, MeshKit solid, PlaceFrame f)
        {
            var up = Vector3.up; const float W = 20f, H = 7f; var D = HouseD; var paint = WorldMaterials.Solid(new Color(.52f, .28f, .22f));
            mk.Box(paint, f.P(0, H / 2f - .3f, 0), new Vector3(W, H + .6f, D), f.r); solid.Box(paint, f.P(0, H / 2f - .3f, 0), new Vector3(W, H + .6f, D), f.r); mk.Box(woodM, f.P(0, H + .03f, 0), new Vector3(W - .5f, .08f, D - .5f), f.r);
            // the front: big lit windows, a door, the brass sign, an arrow to the stair
            for (var k = 0; k < 4; k++) { mk.Box(windowM, f.P(-6.6f + k * 4.4f, 1.8f, D / 2f + .02f), new Vector3(3f, 2.2f, .06f), f.r); mk.Box(k % 2 == 0 ? windowM : (Material)glassDark, f.P(-6.6f + k * 4.4f, 5f, D / 2f + .02f), new Vector3(2f, 1.6f, .06f), f.r); halos.Add(f.P(-6.6f + k * 4.4f, 1.8f, D / 2f + .9f), 2f, Brass, .3f); }
            BrassSign(mk, f.P(0, 3.55f, D / 2f + .1f), f.Fwd, 8f, 1.1f, "THE ROOFTOP", .5f, new Color(.12f, .34f, .38f)); Sign(f.P(W / 2f - 1.6f, 2.6f, D / 2f + .06f), f.Fwd, "up the stair  →", .16f, Cream);
            // the outside stair up the side nearest the lane, from the quay to the roof
            const int steps = 20; var rise = H / steps; var run = D / steps; var sx = W / 2f + 1f;
            for (var k = 0; k < steps; k++) { var sp = f.P(sx, rise * (k + .5f), D / 2f - (k + .5f) * run); mk.Box(iron, sp + up * (rise / 2f - .04f), new Vector3(1.7f, .08f, run + .02f), f.r); solid.Box(iron, sp - up * (rise * k / 2f), new Vector3(1.7f, rise * (k + 1), run), f.r); }
            var ra = f.P(sx + .86f, 1f, D / 2f); var rb = f.P(sx + .86f, H + 1f, -D / 2f); mk.Prism(brassM, ra, (rb - ra).normalized, .04f, (rb - ra).magnitude, 5); for (var k = 0; k <= 5; k++) mk.Prism(iron, Vector3.Lerp(ra, rb, k / 5f) - up * 1f, up, .03f, 1f, 4); Col((ra + rb) / 2f - up * .4f, new Vector3(.1f, 1.3f, (rb - ra).magnitude), Quaternion.LookRotation((rb - ra).normalized)); Col(f.P(sx, H + .6f, -D / 2f - .1f), new Vector3(1.9f, 1.2f, .12f), f.r); Col(f.P(sx + .9f, H + .6f, -D / 2f + .4f), new Vector3(.12f, 1.2f, 1.2f), f.r);
            Remember(f.P(sx, 0, D / 2f + 1f), 3);
            // the roof: a low brick parapet with a brass rail, open where the stair arrives
            var ro = new PlaceFrame(f.P(0, H + .07f, 0), f.Fwd);
            void Para(Vector3 a, Vector3 b) { var d = b - a; var len = d.magnitude; var rr = Quaternion.LookRotation(d.normalized); mk.Box(brick, (a + b) / 2f + up * .35f, new Vector3(.3f, .7f, len), rr); mk.Box(brassM, (a + b) / 2f + up * 1f, new Vector3(.06f, .06f, len), rr); for (var s = 0f; s <= len + .01f; s += len / Mathf.Max(1, Mathf.RoundToInt(len / 1.5f))) mk.Prism(brassM, a + d.normalized * s + up * .7f, up, .025f, .3f, 4); Col((a + b) / 2f + up * .55f, new Vector3(.3f, 1.1f, len), rr); }
            Para(ro.P(-W / 2f, 0, D / 2f), ro.P(W / 2f, 0, D / 2f)); Para(ro.P(-W / 2f, 0, -D / 2f), ro.P(W / 2f, 0, -D / 2f)); Para(ro.P(-W / 2f, 0, -D / 2f), ro.P(-W / 2f, 0, D / 2f)); Para(ro.P(W / 2f, 0, -D / 2f + 1.9f), ro.P(W / 2f, 0, D / 2f));
            var poles = new List<Vector3>(); foreach (var (x, z) in new[] { (-1f, 1f), (1f, 1f), (1f, -1f), (-1f, -1f) }) { var p = ro.P(x * (W / 2f - .4f), 0, z * (D / 2f - .4f)); mk.Prism(iron, p, up, .05f, 3.4f, 5); poles.Add(p + up * 3.3f); }
            for (var k = 0; k < 4; k++) BulbString(mk, poles[k], poles[(k + 1) % 4], k); BulbString(mk, poles[0], poles[2], 1); BulbString(mk, poles[1], poles[3], 2);
            ArtistBits.PointLight(ctx.Root, ro.P(-3f, 2.6f, 0), Brass, 16f, 2.4f); ArtistBits.PointLight(ctx.Root, ro.P(-8f, 2.6f, 0), new Color(1f, .7f, .5f), 12f, 1.8f); halos.Add(ro.P(0, 2.4f, 0), 6f, Brass, .22f);
            // the band, at the far end from the stair: keys, upright bass, and a drum kit nobody is sitting at
            mk.Box(WorldMaterials.Solid(new Color(.5f, .16f, .2f)), ro.P(-7f, .02f, 0), new Vector3(5.4f, .04f, 5f), ro.r);
            var kit = ro.P(-7.6f, .04f, -1.4f); var kitF = ro.Right; var km = new MeshKit(); var shell = WorldMaterials.Solid(new Color(.2f, .46f, .5f), .6f);
            km.Prism(shell, new Vector3(0, .3f, -.25f), Vector3.forward, .5f, .5f, 14); km.Prism(white, new Vector3(0, .3f, .25f), Vector3.forward, .47f, .02f, 14); foreach (var e in new[] { -1f, 1f }) { km.Prism(shell, new Vector3(e * .85f, .5f, -.3f), Vector3.up, .3f, .25f, 12); km.Prism(brassM, new Vector3(e * 1.15f, 1.2f, -.5f), Vector3.up, .34f, .02f, 14); km.Prism(iron, new Vector3(e * 1.15f, 0, -.5f), Vector3.up, .015f, 1.2f, 4); } km.Prism(shell, new Vector3(.3f, .62f, -.9f), Vector3.up, .22f, .14f, 12);
            var kgo = km.BuildSingle("The Rooftop's drum kit", tlife.transform, false); kgo.transform.SetPositionAndRotation(kit, Quaternion.LookRotation(kitF)); Col(kit + up * .5f, new Vector3(1.4f, 1f, 2.6f), ro.r);
            var stool = kit - kitF * 1.25f; mk.Prism(iron, stool, up, .04f, .5f, 5); mk.Prism(dark, stool + up * .5f, up, .2f, .08f, 10); Interact.Seat(tlife.transform, stool + up * .6f, kitF, "Sit at the drums").seatHeight = .6f;
            string[] sit = { "You come in on the one. The bass player grins without turning round.", "A little ghost note on the snare. The keys answer it. You're in.", "You drop to half-time. The whole roof leans back with you.", "Brushes. Barely there. Somebody at the bar says \"mm\" like they've tasted something." }; var sn = 0;
            Interact.Toy(kgo, Toy.Kind.Custom, "Sit in on the drums", new Vector3(2f, 1.4f, 2.6f)).onUse = _ => { life.played = Time.time; Say(tlife.transform, kit + up * 2.4f, sit[sn++ % sit.Length], 4.5f); };
            var ky = ro.P(-5.2f, .04f, 1.4f); mk.Box(WorldMaterials.Solid(new Color(.16f, .16f, .2f)), ky + up * .82f, new Vector3(1.4f, .16f, .5f), ro.r); mk.Box(white, ky + up * .91f + ro.Fwd * .08f, new Vector3(1.3f, .03f, .22f), ro.r); foreach (var e in new[] { -.6f, .6f }) mk.Box(iron, ky + up * .4f + ro.Right * e, new Vector3(.04f, .8f, .4f), ro.r); Col(ky + up * .5f, new Vector3(1.4f, 1f, .5f), ro.r);
            var keysP = Local(); keysP.hat = Residents.Hat.Beanie; Folk(ky - ro.Fwd * .8f, ro.Fwd, Resident.Pose.Work, new[] { "Drummer's stuck on the other side of the canal. He's always stuck on the other side of the canal.", "Sit in if you like. Keep it light. Lighter. There.", "I've been comping these two chords for an hour and I've only just started to understand them." }, keysP).workRate = 1.2f;
            var bs = ro.P(-8.6f, .04f, 1.6f); mk.Blob(woodM, bs + up * .8f, new Vector3(.34f, .6f, .16f), prng, .04f); mk.Box(dark, bs + up * 1.7f, new Vector3(.08f, 1.1f, .05f), ro.r); mk.Prism(iron, bs, up, .015f, .25f, 4);
            var bassist = Local(); Folk(bs + ro.Right * .6f, ro.Right, Resident.Pose.Stand, new[] { "One note. Right place. That's the job.", "You hear that space? I'm playing that. That's the hardest part." }, bassist);
            // the bar at the back, the tables with their mismatched chairs
            var bar = ro.P(3.5f, 0, -D / 2f + 1.1f); mk.Box(woodM, bar + up * .55f, new Vector3(4.6f, 1.1f, .7f), ro.r); mk.Box(brassM, bar + up * 1.12f, new Vector3(4.8f, .05f, .9f), ro.r); Col(bar + up * .55f, new Vector3(4.8f, 1.1f, .9f), ro.r);
            Folk(bar - ro.Fwd * .1f + ro.Right * 3f, -ro.Right, Resident.Pose.Stand, new[] { "What'll it be? Something long and slow, I'd say, looking at you.", "We don't do last orders. We do next orders.", "Mind the chairs. None of them match and one of them's a bit of a character." }, Local(Residents.Tool.Mug, true));
            for (var k = 0; k < 4; k++) { var gm = new MeshKit(); gm.Prism(WorldMaterials.Glow(k % 2 == 0 ? new Color(1f, .7f, .3f) : new Color(.5f, .9f, .85f), .6f), Vector3.zero, Vector3.up, .04f, .16f, 8, 1.2f); Thing(gm, "Drink", k % 2 == 0 ? "something amber over ice" : "something teal with mint", bar + up * 1.15f + ro.Right * (-1.6f + k * .9f) + ro.Fwd * .1f, Quaternion.identity, new Vector3(.1f, .18f, .1f), .15f, false); }
            var tn = 0; foreach (var (x, z) in new[] { (0f, 1.6f), (5.5f, 1.4f), (1.8f, -.6f) })
            {
                var tp = ro.P(x, 0, z); mk.Prism(iron, tp, up, .04f, .72f, 5); mk.Prism(woodM, tp + up * .72f, up, .45f, .05f, 10); mk.Blob(bulbM[tn % 3], tp + up * .85f, Vector3.one * .07f, prng, 0f); ArtistBits.PostCollider(ctx.Root, tp, .8f, .36f);
                Chair(mk, tp + ro.Right * .9f, -ro.Right, tn * 2); Chair(mk, tp - ro.Right * .9f, ro.Right, tn * 2 + 1);
                if (tn == 0) Folk(tp + ro.Right * .9f + up * .5f, -ro.Right, Resident.Pose.Sit, new[] { "Look at that sky. Teal going to ink. They time the second set for it.", "I only came up for one. That was three tunes ago. Each of them was the one." }, Local(Residents.Tool.Mug), null, .5f);
                if (tn == 1) Folk(tp - ro.Right * .9f + up * .5f, ro.Right, Resident.Pose.Sit, new[] { "My chair's a kitchen chair and his is from a cinema. Nobody knows how.", "From up here you can see who's coming over the Piano Bridge. It lights up when they do." }, null, null, .5f);
                tn++;
            }
            var prr = new System.Random(8); for (var k = 0; k < 5; k++) Pot(mk, ro.P(-W / 2f + 1f + k * 4.4f, 0, D / 2f - .7f), 1.3f, prr);
            Mark(f.o, 12f);
            Shot(ctx, "the rooftop from the quay", f.P(-9f, 2.2f, D / 2f + 13f), f.P(2f, H + 1f, 0));
            Shot(ctx, "on the rooftop", ro.P(8f, 1.8f, 2.6f), ro.P(-7f, 1.2f, 0));
        }

        // ---------------------------------------------------------------- the café
        void Cafe(LandContext ctx, MeshKit mk, MeshKit solid, PlaceFrame f)
        {
            var up = Vector3.up; const float W = 11.6f, H = 3.3f, T = .3f, Door = 1.8f, dx = 3.4f; var D = HouseD; var fo = new PlaceFrame(f.P(0, .16f, 0), f.Fwd); var wall = WorldMaterials.Solid(new Color(.5f, .42f, .36f)); var floor = WorldMaterials.Solid(new Color(.36f, .26f, .2f));
            Room(mk, solid, fo, W, D, H, T, dx, Door, wall, floor);
            // the storey above, and the front
            mk.Box(WorldMaterials.Solid(Paints[2]), fo.P(0, H + 1.9f, 0), new Vector3(W, 3.4f, D), fo.r); solid.Box(brick, fo.P(0, H + 1.9f, 0), new Vector3(W, 3.4f, D), fo.r); mk.Box(stoneM, fo.P(0, H + 3.7f, D / 2f - .1f), new Vector3(W + .2f, .24f, .5f), fo.r); for (var k = 0; k < 3; k++) mk.Box(k == 1 ? (Material)glassDark : windowM, fo.P(-3.6f + k * 3.6f, H + 2f, D / 2f + .02f), new Vector3(1.3f, 1.7f, .06f), fo.r);
            mk.Box(windowM, fo.P(-2.2f, 1.8f, D / 2f - T / 2f), new Vector3(5.6f, 1.9f, T + .04f), fo.r); foreach (var q in new[] { -1.4f, 1.4f }) mk.Box(white, fo.P(-2.2f + q, 1.8f, D / 2f + .01f), new Vector3(.08f, 1.9f, T + .1f), fo.r); halos.Add(fo.P(-2.2f, 1.8f, D / 2f + 1f), 2.6f, Brass, .32f);
            BrassSign(mk, fo.P(0, H + .35f, D / 2f + .1f), fo.Fwd, 7f, .8f, "WHAT KINDA COFFEE", .32f, new Color(.3f, .16f, .14f)); var stripe = WorldMaterials.Solid(new Color(.2f, .46f, .5f)); for (var s = 0; s < 10; s++) mk.Box(s % 2 == 0 ? stripe : white, fo.P(-W / 2f + .6f + s * 1.16f, 2.95f, D / 2f + .8f), new Vector3(1.16f, .05f, 1.7f), fo.r * Quaternion.Euler(18f, 0, 0));
            // the counter: an espresso machine, a pastry case, the barista
            var cz = -D / 2f + T + 1.6f; mk.Box(woodM, fo.P(-1.6f, .5f, cz), new Vector3(6f, 1f, .7f), fo.r); mk.Box(stoneM, fo.P(-1.6f, 1.03f, cz), new Vector3(6.2f, .06f, .9f), fo.r); Col(fo.P(-1.6f, .55f, cz), new Vector3(6.2f, 1.1f, .9f), fo.r);
            mk.Box(brassM, fo.P(-3.6f, 1.36f, cz), new Vector3(.9f, .6f, .6f), fo.r); mk.Box(GlowM(new Color(1f, .5f, .2f), 1.2f), fo.P(-3.6f, 1.2f, cz + .31f), new Vector3(.3f, .08f, .02f), fo.r); mk.Box(WorldMaterials.Glow(new Color(1f, .9f, .7f), .5f), fo.P(-.6f, 1.3f, cz), new Vector3(1.6f, .5f, .6f), fo.r);
            var barista = Local(Residents.Tool.Mug, true); Folk(fo.P(-2.2f, 0, cz - .9f), fo.Fwd, Resident.Pose.Work, new[] { "Flat white? Pull up a chair, it'll be a minute. Everything here's a minute.", "I pour to the tune that's on. Long track, tall drink.", "The cat's called Rhodes. He sits on the warm speaker." }, barista).workRate = .8f;
            for (var k = 0; k < 3; k++) { var cm = new MeshKit(); cm.Prism(white, Vector3.zero, Vector3.up, .045f, .09f, 8, 1.15f); cm.Prism(WorldMaterials.Solid(new Color(.3f, .18f, .12f)), new Vector3(0, .08f, 0), Vector3.up, .04f, .012f, 8); Thing(cm, "Coffee", "a flat white", fo.P(-2.6f + k * .9f, 1.07f, cz + .2f), fo.r, new Vector3(.12f, .1f, .12f), .15f, false); }
            var cr = new MeshKit(); cr.Blob(WorldMaterials.Solid(new Color(.85f, .62f, .3f)), new Vector3(0, .04f, 0), new Vector3(.13f, .05f, .07f), prng, .2f); Thing(cr, "Croissant", "a croissant", fo.P(-.6f, 1.07f, cz + .34f), fo.r, new Vector3(.26f, .1f, .14f), .1f, false);
            // the beat machine on a side table: tap the pads
            var bt = fo.P(W / 2f - T - .6f, 0, -1.2f); mk.Box(woodM, bt + up * .42f, new Vector3(.8f, .84f, 1.2f), fo.r); Col(bt + up * .42f, new Vector3(.8f, .84f, 1.2f), fo.r); var pm = new MeshKit(); pm.Box(dark, new Vector3(0, .04f, 0), new Vector3(.5f, .08f, .6f), Quaternion.identity);
            var pads = new Material[4]; for (var k = 0; k < 4; k++) pads[k] = new Material(GlowM(new[] { Teal, Brass, new Color(1f, .5f, .4f), Indigo }[k], .5f)) { name = "Beat pad " + k }; for (var k = 0; k < 16; k++) pm.Box(pads[(k % 4 + k / 4) % 4], new Vector3(-.16f + (k % 4) * .107f, .09f, -.2f + (k / 4) * .12f), new Vector3(.09f, .02f, .1f), Quaternion.identity);
            var pgo = pm.BuildSingle("Beat machine", tlife.transform, false); pgo.transform.SetPositionAndRotation(bt + up * .86f, fo.r * Quaternion.Euler(0, -90f, 0)); tlife.pads = pads;
            string[] tap = { "Boom, bap, boom-boom bap. The barista nods and froths the milk in time.", "You find a loop. Somebody at the next table starts humming a guitar line over it.", "A little swing on the hi-hats. That's it. That's the whole tune.", "You tap out a beat and leave it running. It's still going when you leave." }; var tn = 0;
            Interact.Toy(pgo, Toy.Kind.Custom, "Tap out a beat", new Vector3(.8f, .6f, .9f)).onUse = _ => { tlife.tapped = Time.time; life.played = Time.time; Say(tlife.transform, bt + up * 2.2f, tap[tn++ % tap.Length], 4.5f); };
            // a crate of records, two tables, the cat on the windowsill
            var rc = fo.P(W / 2f - T - .6f, 0, 1.6f); mk.Box(woodM, rc + up * .3f, new Vector3(.7f, .6f, 1f), fo.r); Col(rc + up * .3f, new Vector3(.7f, .6f, 1f), fo.r); for (var k = 0; k < 7; k++) mk.Box(WorldMaterials.Solid(Paints[k % Paints.Length]), rc + up * .7f + fo.Fwd * (-.36f + k * .12f), new Vector3(.56f, .34f, .02f), fo.r * Quaternion.Euler(-8f, 0, 0));
            for (var k = 0; k < 3; k++) { var rm = new MeshKit(); rm.Box(WorldMaterials.Solid(Paints[(k + 2) % Paints.Length]), new Vector3(0, .005f, 0), new Vector3(.32f, .01f, .32f), Quaternion.identity); rm.Prism(dark, new Vector3(0, .012f, 0), Vector3.up, .12f, .004f, 12); Thing(rm, "Record", k == 0 ? "a white-label 12 inch" : "a record", bt + up * (.86f + k * .012f) + fo.Fwd * .9f, fo.r * Quaternion.Euler(0, k * 11f, 0), new Vector3(.32f, .03f, .32f), .12f); }
            for (var k = 0; k < 2; k++) { var tp = fo.P(-4f + k * 3.4f, 0, 1.5f); mk.Prism(iron, tp, up, .04f, .72f, 5); mk.Prism(woodM, tp + up * .72f, up, .42f, .05f, 10); ArtistBits.PostCollider(ctx.Root, tp, .8f, .34f); Chair(mk, tp + fo.Right * .85f, -fo.Right, k * 2 + 1); Chair(mk, tp - fo.Right * .85f, fo.Right, k * 2 + 2); }
            Folk(fo.P(-4f + .85f, .5f, 1.5f), -fo.Right, Resident.Pose.Sit, new[] { "I've been 'just finishing this coffee' since the first chorus.", "Somebody's left a beat running on the machine. It's better than what I was working on." }, Local(Residents.Tool.Mug), null, .5f);
            var cat = fo.P(-3.6f, .92f, D / 2f - T - .26f); var catM = new MeshKit(); var fur = WorldMaterials.Solid(new Color(.3f, .3f, .34f)); catM.Blob(fur, new Vector3(0, .1f, 0), new Vector3(.2f, .11f, .3f), prng, .04f); catM.Blob(fur, new Vector3(0, .15f, .3f), Vector3.one * .1f, prng, 0f); foreach (var e in new[] { -.05f, .05f }) catM.Cone(fur, new Vector3(e, .22f, .3f), .035f, .07f, 4);
            var cgo = catM.BuildSingle("Rhodes the cat", tlife.transform, false); cgo.transform.SetPositionAndRotation(cat, fo.r * Quaternion.Euler(0, 90f, 0)); mk.Box(woodM, cat - up * .04f, new Vector3(1.2f, .06f, .4f), fo.r); string[] purr = { "He purrs in a low B flat. It goes with everything.", "One ear turns toward the beat machine. Approval.", "He stretches across the whole windowsill and becomes two bars longer." }; var pn = 0;
            Interact.Toy(cgo, Toy.Kind.Custom, "Stroke the cat", new Vector3(.7f, .5f, .8f)).onUse = _ => Say(tlife.transform, cat + up * 1f, purr[pn++ % purr.Length], 4f);
            ArtistBits.PointLight(ctx.Root, fo.P(-1f, H - .6f, 0), new Color(1f, .85f, .62f), 10f, 1.2f, true); Mark(f.o, 8f);
            Shot(ctx, "inside what kinda coffee", fo.P(dx, 1.7f, D / 2f - .9f), fo.P(-3f, 1f, -D / 2f + 1.6f));
        }

        /// A busker on a corner with an instrument and an open case.
        void Busker(LandContext ctx, MeshKit mk, Vector3 g, Vector3 face, int i)
        {
            var up = Vector3.up; var rot = Quaternion.LookRotation(face); string[][] lines =
            {
                new[] { "I'm working on a tune in seven. You can clap along if you can find the one.", "Coins are welcome. A nod's better." }, new[] { "Same four chords as everyone, but I play them in a different order.", "I've got a pedal for everything. I've got a pedal for the other pedals." },
                new[] { "This horn's older than the canal. Sounds like it too. I mean that kindly.", "Listen to the gap after the phrase. That's where the town joins in." }, new[] { "I only play the slow ones out here. The fast ones are for upstairs.", "You look like you play. Everybody here looks like they play." },
            };
            var kind = i % 3; var side = Vector3.Cross(up, face);
            if (kind == 0) { mk.Prism(iron, g + side * .7f, up, .02f, .9f, 4); mk.Prism(brassM, g + side * .7f + up * .9f, (up + face * .3f).normalized, .05f, .7f, 6, 2.2f); }
            else if (kind == 1) { mk.Blob(woodM, g + side * .7f + up * .5f, new Vector3(.22f, .3f, .08f), prng, .05f); mk.Box(dark, g + side * .7f + up * 1.2f, new Vector3(.06f, .9f, .04f), rot); }
            else { mk.Box(dark, g + side * .8f + up * .45f, new Vector3(.9f, .14f, .36f), rot); mk.Box(white, g + side * .8f + up * .53f + face * .06f, new Vector3(.84f, .02f, .16f), rot); foreach (var e in new[] { -.4f, .4f }) mk.Box(iron, g + side * (.8f + e) + up * .22f, new Vector3(.03f, .44f, .3f), rot); }
            mk.Box(dark, g + face * 1.1f + up * .05f, new Vector3(.9f, .1f, .36f), rot); mk.Box(WorldMaterials.Solid(new Color(.5f, .14f, .2f)), g + face * 1.1f + up * .11f, new Vector3(.84f, .02f, .3f), rot); for (var k = 0; k < 4; k++) mk.Blob(brassM, g + face * 1.1f + side * (-.3f + k * .2f) + up * .13f, new Vector3(.03f, .01f, .03f), prng, 0f);
            Folk(g, face, i % 2 == 0 ? Resident.Pose.Work : Resident.Pose.Stand, lines[i % lines.Length]); ArtistBits.PointLight(ctx.Root, g + up * 2.6f + face * 1f, Brass, 9f, 1.4f); noGrass.Add(new Vector3(g.x, 2.4f, g.z));
        }
    }

    /// The town's moving parts: the Piano Bridge's keys (they light as you cross), the beat pads, the rowing boat.
    public class FusionTownLife : MonoBehaviour
    {
        public Material keysM; public Vector3 bridgeAt, bridgeAxis, rowA, rowB; public float bridgeHalf; public Material[] pads; public Transform rowboat; public float tapped = -99f; float keyGlow;

        void Update()
        {
            var t = Time.time; var dt = Time.deltaTime; var cam = Camera.main;
            // the bridge lights while somebody is on it
            var on = false; if (cam != null) { var rel = cam.transform.position - bridgeAt; on = Mathf.Abs(Vector3.Dot(rel, bridgeAxis)) < bridgeHalf && Vector3.ProjectOnPlane(rel, bridgeAxis).magnitude < 4f; }
            keyGlow = Mathf.MoveTowards(keyGlow, on ? 1f : 0f, dt * 2f); if (keysM != null) keysM.SetColor("_EmissionColor", new Color(.97f, .96f, .92f) * (.25f + keyGlow * (1.1f + .3f * Mathf.Sin(t * 6f))));
            if (pads != null) { var beat = (float)(SharedChance.Now * 1.5 % 4.0); var live = t - tapped < 30f; for (var i = 0; i < pads.Length; i++) if (pads[i] != null) { var c = new[] { FusionKit.Teal, FusionKit.Brass, new Color(1f, .5f, .4f), FusionKit.Indigo }[i]; pads[i].SetColor("_EmissionColor", c * (.3f + (live && Mathf.FloorToInt(beat) == i ? 2.2f * (1f - beat % 1f) : 0f))); } }
            if (rowboat != null) { var u = Mathf.PingPong((float)(SharedChance.Now * .012 % 2.0), 1f); var p = Vector3.Lerp(rowA, rowB, Mathf.SmoothStep(0f, 1f, u)); rowboat.SetPositionAndRotation(p + Vector3.up * Mathf.Sin(t * 1.3f) * .03f, Quaternion.LookRotation((rowB - rowA).normalized) * Quaternion.Euler(0, 0, Mathf.Sin(t * .9f) * 2f)); }
        }
    }
}
