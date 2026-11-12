using System.Collections.Generic;
using UnityEngine;
using static EverythingWorld.World.PlaceTools;

namespace EverythingWorld.World
{
    /// The Old Jazz Quarter's town (part of JazzKit): Cobble Square (the stopped clock, the news stand with the late
    /// edition to carry, umbrellas to borrow, a phone box that rings), the river and the stone bridge with its
    /// saxophonist, Lamp Street climbing the hill between tall dark houses (blinds, fire escapes, water tanks, white
    /// neon), and out of town the phone boxes and the parked cars with their lamps left on.
    public partial class JazzKit
    {
        JazzTownLife tlife; System.Random prng; Transform rootT; LandContext ctx0;

        static readonly Color[] Coats = { new Color(.62f, .6f, .56f), new Color(.2f, .2f, .23f), new Color(.4f, .4f, .43f), new Color(.12f, .12f, .14f), new Color(.75f, .74f, .72f) };
        static readonly Color[] Trousers = { new Color(.12f, .12f, .14f), new Color(.24f, .24f, .27f), new Color(.34f, .34f, .36f) };
        static readonly string[] TownTalk =
        {
            "It's always a little past midnight here. You get used to it. You get to like it.", "Rain's been coming down since the first set. Nobody minds. It keeps time.",
            "The Blue Mode's on the square. Go in quiet and sit near the bass.", "Top of the hill, it doesn't rain. Don't ask me how. Go and stand in it.", "Somebody's playing on the bridge again. Somebody's always playing on the bridge.",
        };

        void TownStart(LandContext ctx)
        {
            rootT = ctx.Root; ctx0 = ctx; prng = new System.Random(1957);
            tlife = new GameObject("Jazz town life").AddComponent<JazzTownLife>(); tlife.transform.SetParent(life.transform, false);
        }

        /// Everyone here is in black and white too: grey coats, grey faces, a hat against the rain.
        Residents.Look Local(Residents.Tool tool = Residents.Tool.None, bool apron = false)
        {
            var l = Residents.Random(prng, Coats, Trousers, new[] { Residents.Hat.Bucket, Residents.Hat.Bucket, Residents.Hat.Cap, Residents.Hat.None }, Trousers);
            var g = .34f + (float)prng.NextDouble() * .42f; l.skin = new Color(g, g, g * 1.02f); var h = .06f + (float)prng.NextDouble() * .3f; l.hair = new Color(h, h, h); l.tool = tool; l.apron = apron; return l;
        }

        Resident Folk(Vector3 feet, Vector3 face, Resident.Pose pose, string[] lines = null, Residents.Look look = null, Vector3[] route = null, float seatH = HumanScale.SeatHeight)
            => Residents.Make(tlife.transform, feet, face, look ?? Local(), pose, prng, lines ?? TownTalk, route, seatH);
        void Col(Vector3 c, Vector3 size, Quaternion rot) => BoxCol(rootT, c, size, rot);
        void Mark(Vector3 at, float r) => noGrass.Add(new Vector3(at.x, r, at.z));
        void Sign(Vector3 at, Vector3 face, string text, float size, Color c) => ArtistBits.Label(rootT, text, at, face, size, c, TextAnchor.MiddleCenter);
        /// A point by the river: 'al' metres along it, 'pe' metres across it (uphill positive), 'y' above the banks.
        Vector3 P(float al, float pe, float y = 0f) => streamC + R * al + U * pe + Vector3.up * y;

        Carryable Thing(MeshKit m, string name, string label, Vector3 at, Quaternion rot, Vector3 size, float mass, bool still = true)
        {
            var go = m.BuildSingle(name, tlife.transform, false); go.transform.SetPositionAndRotation(at, rot); var c = Interact.Carry(go, label, mass, at.y - 30f);
            var bc = c.GetComponent<BoxCollider>(); if (bc != null) { bc.size = size; bc.center = new Vector3(0, size.y / 2f, 0); }
            if (still) { var rb = go.GetComponent<Rigidbody>(); if (rb != null) rb.isKinematic = true; }
            return c;
        }

        /// Something to use that is only a spot in the air (a call, a look, a listen).
        Toy Spot(string name, Vector3 at, string verb, Vector3 size)
        {
            var go = new GameObject(name); go.transform.SetParent(tlife.transform, false); go.transform.position = at; return Interact.Toy(go, Toy.Kind.Custom, verb, size);
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

        // ---------------------------------------------------------------- the square
        void Square(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var c = ctx.OnGround(center.x, center.z);
            mk.Prism(cobble, c - up * .28f, up, 20f, .4f, 28); mk.Prism(stoneM, c + up * .125f, up, 9f, .01f, 24); mk.Prism(cobble, c + up * .13f, up, 8.6f, .01f, 24);
            for (var k = 0; k < 4; k++) { var d = Quaternion.Euler(0, k * 90f + 45f, 0) * U; StreetLamp(ctx, mk, c + d * 16.5f + up * .13f, -d, true); }
            // the stopped clock, off to one side so the middle (where you arrive) stays clear
            var ck = c - U * 8f + R * 6f + up * .13f; var cf = (U - R * .6f).normalized; var crot = Quaternion.LookRotation(cf);
            mk.Prism(iron, ck - up * .2f, up, .14f, 4.6f, 8, .7f); mk.Prism(iron, ck, up, .4f, .5f, 8, .6f); ArtistBits.PostCollider(ctx.Root, ck, 4.4f, .2f);
            foreach (var e in new[] { -1f, 1f }) { mk.Prism(iron, ck + up * 5.2f - cf * .2f * e, cf * e, .72f, .16f, 16); mk.Prism(WorldMaterials.Glow(new Color(.95f, .94f, .88f), .7f), ck + up * 5.2f + cf * .16f * e, cf * e, .62f, .02f, 16); halos.Add(ck + up * 5.2f + cf * .5f * e, 1.6f, Paper, .35f); }
            mk.Box(dark, ck + up * 5.4f + cf * .2f, new Vector3(.05f, .42f, .02f), crot); var hm = new MeshKit(); hm.Box(dark, new Vector3(0, .26f, 0), new Vector3(.04f, .52f, .02f), Quaternion.identity);
            var hand = hm.BuildSingle("Clock hand", tlife.transform, false); hand.transform.SetPositionAndRotation(ck + up * 5.2f + cf * .21f, crot * Quaternion.Euler(0, 0, -30f)); tlife.hand = hand.transform; tlife.handRot = crot;
            string[] time = { "Five past midnight. It was five past midnight an hour ago, too.", "The big hand twitches and thinks better of it.", "Still five past. The best sets start about now." }; var tn = 0;
            Spot("The stopped clock", ck + up * 1.4f, "Check the time", new Vector3(1.4f, 2.8f, 1.4f)).onUse = _ => Say(tlife.transform, ck + up * 3f, time[tn++ % time.Length], 4.5f);
            // the sign
            var sg = c + U * 6f - R * 15f + up * .13f; var sf = (R - U * .35f).normalized; foreach (var e in new[] { -2.6f, 2.6f }) { mk.Prism(iron, sg + Vector3.Cross(up, sf) * e - up * .2f, up, .09f, 3.9f, 6); ArtistBits.PostCollider(ctx.Root, sg + Vector3.Cross(up, sf) * e, 3.6f, .12f); }
            mk.Box(dark, sg + up * 3f, new Vector3(5.8f, 1.5f, .12f), Quaternion.LookRotation(sf)); mk.Box(white, sg + up * 2.2f + sf * .07f, new Vector3(5.2f, .04f, .02f), Quaternion.LookRotation(sf)); Sign(sg + up * 3.15f + sf * .08f, sf, "THE OLD JAZZ QUARTER", .36f, Paper); Sign(sg + up * 2.5f + sf * .08f, sf, "open late", .2f, Brass); halos.Add(sg + up * 3f + sf * .6f, 2.4f, Paper, .2f);
            // the news stand: the late edition to carry, umbrellas to borrow
            var ns = new PlaceFrame(c - U * 13f - R * 7f + up * .13f, (U + R * .5f).normalized);
            mk.Box(grey, ns.P(0, 1.2f, -.6f), new Vector3(3f, 2.4f, .2f), ns.r); foreach (var e in new[] { -1.5f, 1.5f }) mk.Box(grey, ns.P(e, 1.2f, 0), new Vector3(.14f, 2.4f, 1.3f), ns.r); mk.Box(dark, ns.P(0, 2.5f, .3f), new Vector3(3.4f, .1f, 2.2f), ns.r * Quaternion.Euler(8f, 0, 0)); mk.Box(grey, ns.P(0, .5f, .5f), new Vector3(2.8f, 1f, .5f), ns.r);
            Col(ns.P(0, 1.2f, -.6f), new Vector3(3f, 2.4f, .2f), ns.r); Col(ns.P(0, .5f, .5f), new Vector3(2.8f, 1f, .5f), ns.r); foreach (var e in new[] { -1.5f, 1.5f }) Col(ns.P(e, 1.2f, 0), new Vector3(.14f, 2.4f, 1.3f), ns.r);
            Sign(ns.P(0, 2.2f, .72f), ns.Fwd, "NEWS", .26f, Paper); mk.Blob(lampM, ns.P(0, 2.3f, .1f), new Vector3(.1f, .08f, .1f), prng, 0f); halos.Add(ns.P(0, 2.1f, .4f), 1.6f, Brass, .5f); ArtistBits.PointLight(ctx.Root, ns.P(0, 2f, .9f), Brass, 8f, 1.6f);
            for (var k = 0; k < 3; k++) { var pm = new MeshKit(); pm.Box(white, new Vector3(0, .03f, 0), new Vector3(.34f, .06f, .46f), Quaternion.identity); pm.Box(dark, new Vector3(0, .062f, .12f), new Vector3(.28f, .004f, .06f), Quaternion.identity); Thing(pm, "Newspaper", "the late edition", ns.P(-.9f + k * .9f, 1.01f, .5f), ns.r * Quaternion.Euler(0, k * 7f - 7f, 0), new Vector3(.34f, .08f, .46f), .2f); }
            Folk(ns.P(.3f, 0, -.1f), ns.Fwd, Resident.Pose.Stand, new[] { "Late edition. Same as the early edition, but it knows how the night turns out.", "Take an umbrella. Bring it back, or don't. They find their own way home.", "Front page: rain. Back page: more of the same, with a solo." }, Local(Residents.Tool.None, true));
            mk.Prism(iron, ns.P(2.2f, 0, .4f), up, .26f, .7f, 8, 1.1f); ArtistBits.PostCollider(ctx.Root, ns.P(2.2f, 0, .4f), .7f, .28f);
            for (var k = 0; k < 3; k++) { var um = new MeshKit(); um.Prism(iron, Vector3.zero, Vector3.up, .012f, 1f, 4); um.Cone(dark, new Vector3(0, .12f, 0), .09f, .72f, 8); um.Prism(grey, new Vector3(0, .98f, 0), Vector3.forward, .012f, .1f, 4); Thing(um, "Umbrella", "a black umbrella", ns.P(2.2f + (k - 1) * .12f, .12f, .4f + (k % 2) * .1f), ns.r * Quaternion.Euler(k * 5f - 5f, k * 40f, 6f), new Vector3(.2f, 1.05f, .2f), .4f); }
            // benches, a phone box, a car at the kerb, steam from the grating
            Bench(mk, c + U * 12f + R * 9f + up * .13f, (-U - R * .6f).normalized); Bench(mk, c - U * 3f - R * 16f + up * .13f, R); Bench(mk, c + R * 14f - U * 10f + up * .13f, (-R + U * .5f).normalized);
            PhoneBox(ctx, mk, c + U * 15f - R * 9f + up * .13f, (-U + R * .5f).normalized, 0);
            Sedan(ctx, mk, c - U * 16.5f + R * 3f + up * .13f, R, 0);
            var gr = c + R * 5f + U * 9f + up * .14f; mk.Box(iron, gr, new Vector3(1.2f, .03f, .8f), Quaternion.LookRotation(U)); for (var k = 0; k < 5; k++) mk.Box(dark, gr + up * .02f + R * (-.4f + k * .2f), new Vector3(.08f, .02f, .7f), Quaternion.LookRotation(U)); life.steam.Add(gr);
            var prr = new System.Random(5); Puddle(mk, c + R * 9f - U * 2f + up * .09f, 1.6f, prr); Puddle(mk, c - R * 6f + U * 11f + up * .09f, 1.3f, prr); Puddle(mk, c - R * 11f - U * 9f + up * .09f, 1.1f, prr);
            // someone to say hello, someone walking the ring under an umbrella
            Folk(c + U * 7f + R * 2.5f + up * .13f, (-U - R * .3f).normalized, Resident.Pose.Wave, new[] { "Evening. Well, morning. Nobody here's sure, and nobody's asking.", "The club's just there, under the red sign. Lamp Street goes over the bridge and up the hill.", "Mind the puddles. Or don't. Some of them have the best reflections in town." });
            var ring = new List<Vector3>(); for (var k = 0; k < 12; k++) ring.Add(c + Quaternion.Euler(0, k * 30f, 0) * U * 13f + up * .13f);
            Folk(ring[0], FaceTo(ring[0], ring[1]), Resident.Pose.Walk, new[] { "Can't sleep. Good town for it.", "I walk till the song changes. Then I walk some more." }, null, ring.ToArray()).speed = 1.1f;
            Mark(c, 21f);
            Shot(ctx, "the square", c - U * 8f - R * 13f + up * 2.4f, c + U * 30f + R * 6f + up * 5f);
        }

        // ---------------------------------------------------------------- the river and the bridge
        void River(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var along = Quaternion.LookRotation(R); var across = Quaternion.LookRotation(U); var L = streamL; const float bw = StreetW + 2.4f; var span = StreamHalf * 2f + 3f;
            mk.Box(waterM, P(0, 0, -1.5f), new Vector3(StreamHalf * 2f + .4f, .1f, L * 2f), along);
            foreach (var e in new[] { -1f, 1f })
            {
                // the stone walls of the banks, and a parapet along the top, open where the bridge lands
                mk.Box(stoneM, P(0, e * (StreamHalf + .35f), -1.5f), new Vector3(.7f, 3.2f, L * 2f + 1f), along);
                foreach (var s in new[] { -1f, 1f })
                {
                    var len = L - bw / 2f - .3f; var mid = s * (bw / 2f + .3f + len / 2f);
                    mk.Box(stoneM, P(mid, e * (StreamHalf + .35f), .5f), new Vector3(.5f, .9f, len), along); mk.Box(grey, P(mid, e * (StreamHalf + .35f), .98f), new Vector3(.66f, .1f, len), along); Col(P(mid, e * (StreamHalf + .35f), .6f), new Vector3(.5f, 1.3f, len), along);
                }
                mk.Box(stoneM, P(e * L, 0, -1.5f), new Vector3(StreamHalf * 2f + 1.4f, 3.2f, .7f), along); mk.Prism(dark, P(e * (L - .37f), 0, -1.4f), R * -e, 2.2f, .06f, 10); mk.Box(stoneM, P(e * L, 0, .5f), new Vector3(StreamHalf * 2f + 1.4f, .9f, .5f), along); Col(P(e * L, 0, .6f), new Vector3(StreamHalf * 2f + 1.4f, 1.3f, .5f), along);
            }
            // the bridge: a stone deck on an arch, parapets, a lamp at each corner
            mk.Box(cobble, P(0, 0, -.06f), new Vector3(bw, .44f, span), across); solid.Box(cobble, P(0, 0, -.06f), new Vector3(bw, .44f, span), across);
            foreach (var e in new[] { -1f, 1f })
            {
                mk.Box(stoneM, P(e * (bw / 2f - .25f), 0, .55f), new Vector3(.5f, .9f, span + 1f), across); mk.Box(grey, P(e * (bw / 2f - .25f), 0, 1.03f), new Vector3(.66f, .1f, span + 1.2f), across); Col(P(e * (bw / 2f - .25f), 0, .65f), new Vector3(.5f, 1.3f, span + 1f), across);
                mk.Box(stoneM, P(e * (bw / 2f - .2f), 0, -.9f), new Vector3(.4f, 1.3f, span), across); mk.Prism(dark, P(e * (bw / 2f + .02f), 0, -2.9f), R * e, StreamHalf - .3f, .05f, 14);
                foreach (var s in new[] { -1f, 1f }) StreetLamp(ctx, mk, P(e * (bw / 2f + .7f), s * (span / 2f + 1.2f), .12f), -R * e, s > 0);
            }
            for (var pe = -span / 2f - 2f; pe <= span / 2f + 2f; pe += 2f) Remember(P(0, pe, .18f), 1);
            // the saxophonist, leaning on the parapet, playing to the water
            var sp = P(-(bw / 2f - 1f), .6f, .17f); var sax = Local(); sax.hat = Residents.Hat.Bucket; sax.top = new Color(.14f, .14f, .16f);
            Folk(sp, -R, Resident.Pose.Stand, new[] { "(He doesn't stop. He nods at the river, as if it asked for this one.)", "\"She keeps the low notes,\" he says, between phrases. \"I get them back a bar later.\"", "\"Every night. Till the rain stops. So: every night.\"" }, sax);
            Sax(mk, sp - R * .42f + up * .72f, Quaternion.LookRotation(-R), brassM, .9f); halos.Add(sp - R * .5f + up * 1.2f, .9f, Brass, .4f); life.notes.Add(sp - R * .6f + up * 1.3f);
            string[] hear = { "A slow blues, the kind with room in it. The river takes the low notes downstream.", "He plays one long note and lets the rain finish the phrase.", "It might be St. Thomas, slowed right down until it's almost a lullaby.", "Two people on the far bank have stopped under one umbrella to listen." }; var hn = 0;
            Spot("The saxophonist on the bridge", P(0, 0, 1.2f), "Stop and listen", new Vector3(3f, 2.2f, 4f)).onUse = _ => { life.played = Time.time; Say(tlife.transform, sp + up * 2.6f, hear[hn++ % hear.Length], 5f); };
            // a little boat tied up under the wall, a lamp in it
            if (L > 30f) { var bo = P(-18f, -1.2f, -1.38f); mk.Box(grey, bo, new Vector3(1.2f, .34f, 3f), along); mk.Box(dark, bo + up * .18f, new Vector3(1.3f, .05f, 3.1f), along); mk.Blob(lampM, bo + up * .6f + R * 1.2f, Vector3.one * .1f, prng, 0f); mk.Prism(iron, bo + up * .2f + R * 1.2f, up, .02f, .4f, 4); halos.Add(bo + up * .6f + R * 1.2f, 1.2f, Brass, .5f); }
            Shot(ctx, "the bridge and the saxophonist", P(-12f, -(StreamHalf + 9f), 2.3f), P(0, 2f, 1.4f));
            Shot(ctx, "along the river", P(-L + 10f, -(StreamHalf + 2.2f), 2f), P(L, 2f, 1f));
        }

        // ---------------------------------------------------------------- Lamp Street
        void LampStreet(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var c = center; var top = hillD - Foot17 * .8f; var n = 0;
            foreach (var (a, b) in new[] { (19f, StreamOff - StreamHalf - 1.4f), (StreamOff + StreamHalf + 1.4f, top) })
            {
                var pts = new List<Vector3>(); for (var d = a; ; d += 2f) { var dd = Mathf.Min(d, b); pts.Add(ctx.OnGround(c.x + U.x * dd, c.z + U.z * dd)); if (dd >= b) break; }
                Strip(mk, pts, cobble, StreetW, .15f, ctx); foreach (var p in pts) Remember(p, 1);
                for (var i = 3; i < pts.Count; i += 8, n++) { var e = n % 2 == 0 ? 1f : -1f; var lp = pts[i] + R * e * (StreetW / 2f + .9f); if (InRiver(lp.x, lp.z, 2f)) continue; StreetLamp(ctx, mk, ctx.OnGround(lp.x, lp.z), -R * e, n % 2 == 0); }
            }
            Shot(ctx, "up lamp street", ctx.OnGround(c.x + U.x * 24f, c.z + U.z * 24f) + Vector3.up * 2f, ctx.OnGround(c.x + U.x * 110f, c.z + U.z * 110f) + Vector3.up * 6f);
            var hi = ctx.OnGround(c.x + U.x * (top - 6f), c.z + U.z * (top - 6f)); Shot(ctx, "the town from the hill", hi + Vector3.up * 4f - R * 3f, ctx.OnGround(c.x, c.z) + Vector3.up * 4f);
        }

        const float Foot17 = 17f;

        bool LaneNear(float x, float z, float within) { foreach (var (p, w) in walkPoints) { if (w < 10) continue; float dx = p.x - x, dz = p.z - z; if (dx * dx + dz * dz < within * within) return true; } return false; }

        /// The houses, built after the lanes so none stands in one: both sides of Lamp Street, and round the square.
        void Houses(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var i = 0; var built = 0; var off = StreetW / 2f + 2.6f + HouseD / 2f;
            bool Free(Vector3 p) => ctx.IsLand(p.x, p.z, 8f) && !InFootprint(ctx, p.x, p.z, 6.6f) && !LaneNear(p.x, p.z, 7.6f) && !InRiver(p.x, p.z, 6f);
            foreach (var e in new[] { 1f, -1f })
            for (var u = 29f; u <= hillD - 36f; u += HouseW + .6f, i++)
            {
                var p = center + U * u + R * e * off; if (!Free(p)) continue;
                House(ctx, mk, solid, new PlaceFrame(ctx.OnGround(p.x, p.z), -R * e), i); built++;
            }
            for (var k = 0; k < 12; k++, i++)
            {
                var d = Quaternion.Euler(0, k * 30f + 15f, 0) * U; if (Vector3.Dot(d, U) > .9f) continue;
                var p = center + d * (21.5f + HouseD / 2f); if (!Free(p)) continue;
                House(ctx, mk, solid, new PlaceFrame(ctx.OnGround(p.x, p.z), -d), i); built++;
            }
            Debug.Log($"The Old Jazz Quarter: {built} houses");
        }

        /// A tall dark house: a few windows lit behind their blinds, a door with a lamp over it, sometimes a fire
        /// escape, a water tank on the roof, a sign in white neon. It stands on a plinth, so it sits on a slope.
        void House(LandContext ctx, MeshKit mk, MeshKit solid, PlaceFrame f, int i)
        {
            var up = Vector3.up; const float W = HouseW, D = HouseD; var st = 2 + (i * 7 + 1) % 3; var H = st * 3.1f + .5f; var wall = WorldMaterials.Solid(Walls[i % Walls.Length]); var rng = new System.Random(i * 17 + 3);
            var glass = WorldMaterials.Solid(new Color(.09f, .1f, .13f), .9f); var fz = D / 2f;
            mk.Box(wall, f.P(0, (H - 3f) / 2f, 0), new Vector3(W, H + 3f, D), f.r); solid.Box(wall, f.P(0, (H - 3f) / 2f, 0), new Vector3(W, H + 3f, D), f.r); mk.Box(grey, f.P(0, H + .1f, 0), new Vector3(W + .3f, .24f, D + .3f), f.r);
            if (i % 3 == 0) mk.Box(dark, f.P(0, H + .9f, 0), new Vector3(W - 1.2f, 1.4f, D - 1.2f), f.r); mk.Box(wall, f.P(W / 2f - 1.5f, H + 1.2f, -1.4f), new Vector3(.8f, 2f, .8f), f.r);
            if (i % 5 == 2) { var tk = f.P(-W / 2f + 2.4f, H + .2f, -1f); foreach (var (x, zz) in new[] { (-.7f, -.7f), (.7f, -.7f), (.7f, .7f), (-.7f, .7f) }) mk.Prism(iron, tk + f.Right * x + f.Fwd * zz, up, .05f, 1.5f, 4); mk.Prism(grey, tk + up * 1.5f, up, 1.1f, 1.7f, 10); mk.Cone(dark, tk + up * 3.2f, 1.25f, .7f, 10); }
            // the door (a lamp over it, a step), the ground-floor window behind its blind
            var dx = (i % 2 == 0 ? -1f : 1f) * (W / 2f - 1.9f);
            mk.Box(dark, f.P(dx, 1.1f, fz + .02f), new Vector3(1.05f, 2.2f, .06f), f.r); mk.Box(grey, f.P(dx, 2.3f, fz + .03f), new Vector3(1.4f, .16f, .12f), f.r); mk.Box(stoneM, f.P(dx, .06f, fz + .4f), new Vector3(1.7f, .2f, .8f), f.r);
            mk.Blob(lampM, f.P(dx, 2.62f, fz + .16f), new Vector3(.1f, .12f, .1f), rng, 0f); halos.Add(f.P(dx, 2.6f, fz + .5f), 1.2f, Brass, .5f);
            Blind(mk, f, -dx * .5f, 1.75f, fz, 3f, 1.7f, (i * 5) % 3 != 0, glass);
            for (var s = 1; s < st; s++) for (var k = 0; k < 3; k++) Blind(mk, f, -W / 2f + 2f + k * 3f, s * 3.1f + 1.8f, fz, 1.3f, 1.7f, (i * 3 + k * 5 + s * 7) % 4 == 0, glass);
            if (i % 3 == 1 && st > 2)
            {   // a fire escape: a landing at each floor, ladders between
                for (var s = 1; s < st; s++) { var y = s * 3.1f + .7f; mk.Box(iron, f.P(0, y, fz + .6f), new Vector3(W - 3f, .05f, 1.1f), f.r); mk.Box(iron, f.P(0, y + 1f, fz + 1.14f), new Vector3(W - 3f, .05f, .05f), f.r); for (var q = 0; q < 6; q++) mk.Prism(iron, f.P(-(W - 3f) / 2f + q * (W - 3f) / 5f, y, fz + 1.14f), up, .02f, 1f, 3); if (s > 1) mk.Box(iron, f.P((s % 2 == 0 ? 1f : -1f) * 2f, y - 1.5f, fz + .7f), new Vector3(.5f, 3.3f, .05f), f.r * Quaternion.Euler(0, 0, s % 2 == 0 ? 18f : -18f)); }
                mk.Box(iron, f.P(2.4f, 2.9f, fz + .7f), new Vector3(.5f, 1.8f, .05f), f.r);
            }
            if (i % 4 == 0)
            {   // a sign down the corner in white neon
                string[] words = { "H\nO\nT\nE\nL", "B\nA\nR", "R\nO\nO\nM\nS", "C\nA\nF\nE" }; var w = words[(i / 4) % words.Length]; var lines = w.Length / 2 + 1; var sy = Mathf.Min(H - 1.2f, 3.2f + lines * .5f);
                var sp = f.P(W / 2f - .5f, sy, fz + .5f); mk.Box(dark, sp, new Vector3(.7f, lines * .62f + .4f, .16f), f.r); mk.Box(iron, sp - f.Fwd * .28f, new Vector3(.06f, .06f, .5f), f.r); foreach (var e in new[] { -1f, 1f }) mk.Box(WorldMaterials.Glow(Silver, 1.1f), sp + f.Right * e * .32f + f.Fwd * .09f, new Vector3(.03f, lines * .62f + .2f, .02f), f.r);
                ArtistBits.Label(rootT, w, sp + f.Fwd * .1f, f.Fwd, .44f, Silver, TextAnchor.MiddleCenter); halos.Add(sp + f.Fwd * .6f, 1.6f, Silver, .35f);
            }
            if (i % 6 == 3) { Folk(f.P(-dx, .02f, fz + .7f), f.Fwd, Resident.Pose.Lean, new[] { "Waiting for somebody. Not saying who. Not sure they're coming.", "You hear that horn? Third floor. He's been on the same eight bars since autumn.", "Stand under the lamp if you want to be seen. Stand next to it if you don't." }); }
            Mark(f.o, 6.6f);
        }

        /// A window: lit behind a venetian blind, or dark glass.
        void Blind(MeshKit mk, PlaceFrame f, float x, float y, float fz, float w, float h, bool lit, Material glass)
        {
            mk.Box(lit ? windowM : glass, f.P(x, y, fz + .02f), new Vector3(w, h, .06f), f.r); mk.Box(grey, f.P(x, y - h / 2f - .06f, fz + .06f), new Vector3(w + .2f, .1f, .16f), f.r);
            if (!lit) return; var n = Mathf.RoundToInt(h / .24f); for (var k = 0; k < n; k++) mk.Box(dark, f.P(x, y - h / 2f + (k + .5f) * h / n, fz + .06f), new Vector3(w, .07f, .02f), f.r);
            halos.Add(f.P(x, y, fz + .7f), Mathf.Max(w, h) * .6f, Paper, .22f);
        }

        /// A phone box, lit inside. It rings now and then; pick it up.
        void PhoneBox(LandContext ctx, MeshKit mk, Vector3 g, Vector3 face, int i)
        {
            var up = Vector3.up; var rot = Quaternion.LookRotation(face); var side = Vector3.Cross(up, face);
            mk.Box(dark, g + up * .06f, new Vector3(1.1f, .12f, 1.1f), rot); mk.Box(dark, g + up * 2.4f, new Vector3(1.16f, .3f, 1.16f), rot); foreach (var (x, zz) in new[] { (-1f, -1f), (1f, -1f), (1f, 1f), (-1f, 1f) }) mk.Box(dark, g + up * 1.2f + side * x * .5f + face * zz * .5f, new Vector3(.1f, 2.3f, .1f), rot);
            mk.Box(windowM, g + up * 1.3f - face * .5f, new Vector3(.9f, 1.9f, .04f), rot); foreach (var e in new[] { -1f, 1f }) mk.Box(windowM, g + up * 1.5f + side * e * .5f, new Vector3(.04f, 1.4f, .9f), rot);
            mk.Box(dark, g + up * 1.3f - face * .4f, new Vector3(.3f, .4f, .14f), rot); Sign(g + up * 2.4f + face * .6f, face, "TELEPHONE", .1f, Paper); halos.Add(g + up * 1.5f + face * .3f, 1.6f, Paper, .4f); Col(g + up * 1.25f, new Vector3(1.1f, 2.5f, 1.1f), rot);
            string[][] calls =
            {
                new[] { "\"...he's playing tonight. Second set. Don't tell him I told you.\" Click.", "A long silence, and somewhere behind it, very faint, a piano.", "\"Wrong number,\" says a tired voice. \"But stay on the line. Listen to this bridge.\"" },
                new[] { "\"Is that you? ...No. Well. If you see her, tell her the song's finished.\"", "Just rain on the line. Somewhere it's raining harder than here.", "\"Meet me where the lamp's out,\" says the voice. Every lamp is on." },
            }; var cn = 0; var set = calls[i % calls.Length];
            Spot("Phone box", g + up * 1.3f + face * .3f, "Pick up the phone", new Vector3(1.2f, 2.2f, 1.4f)).onUse = _ => Say(tlife.transform, g + up * 3f, set[cn++ % set.Length], 5f);
            noGrass.Add(new Vector3(g.x, 1.6f, g.z));
        }

        /// A long black car at the kerb with its lamps left on, light lying on the wet ground ahead of it.
        void Sedan(LandContext ctx, MeshKit mk, Vector3 g, Vector3 fwd, int i)
        {
            var up = Vector3.up; fwd.y = 0; fwd.Normalize(); var rot = Quaternion.LookRotation(fwd); var side = Vector3.Cross(up, fwd); var body = WorldMaterials.Solid(new Color(.06f, .06f, .07f), .9f); var rng = new System.Random(i + 40);
            mk.Box(body, g + up * .62f, new Vector3(1.75f, .62f, 4.7f), rot); mk.Box(body, g + up * 1.2f - fwd * .35f, new Vector3(1.55f, .62f, 2.2f), rot); mk.Box(WorldMaterials.Solid(new Color(.2f, .22f, .26f), .9f), g + up * 1.22f - fwd * .35f, new Vector3(1.58f, .4f, 1.9f), rot);
            mk.Blob(body, g + up * .8f + fwd * 1.7f, new Vector3(.86f, .34f, .9f), rng, .02f); mk.Box(WorldMaterials.Solid(Silver, .9f), g + up * .4f + fwd * 2.38f, new Vector3(1.8f, .12f, .08f), rot); mk.Box(WorldMaterials.Solid(Silver, .9f), g + up * .4f - fwd * 2.38f, new Vector3(1.8f, .12f, .08f), rot);
            foreach (var e in new[] { -1f, 1f }) { foreach (var s in new[] { -1.5f, 1.45f }) { mk.Prism(dark, g + up * .36f + side * e * .8f + fwd * s, side * e, .36f, .2f, 10); mk.Blob(body, g + up * .62f + side * e * .86f + fwd * s, new Vector3(.2f, .3f, .6f), rng, .02f); } mk.Blob(lampM, g + up * .72f + side * e * .6f + fwd * 2.36f, Vector3.one * .1f, rng, 0f); halos.Add(g + up * .72f + side * e * .6f + fwd * 2.5f, 1f, Brass, .6f); }
            var ahead = g + fwd * 4.6f; ahead = ctx.OnGround(ahead.x, ahead.z); if (Mathf.Abs(ahead.y - g.y) < .3f) mk.Prism(poolM, ahead + up * .17f, up, 1.6f, .012f, 12); Col(g + up * .8f, new Vector3(1.8f, 1.5f, 4.7f), rot); noGrass.Add(new Vector3(g.x, 3f, g.z));
        }
    }

    /// The town's small moving part: the stopped clock's big hand, which twitches and thinks better of it.
    public class JazzTownLife : MonoBehaviour
    {
        public Transform hand; public Quaternion handRot;

        void Update()
        {
            if (hand == null) return; var t = Time.time % 7f; var tw = t < .25f ? Mathf.Sin(t / .25f * Mathf.PI) * 5f : 0f;
            hand.rotation = handRot * Quaternion.Euler(0, 0, -30f - tw);
        }
    }
}
