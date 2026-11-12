using System.Collections.Generic;
using UnityEngine;
using static EverythingWorld.World.PlaceTools;

namespace EverythingWorld.World
{
    /// The Funk Tropics' places and people (part of FunkKit): the arrival clearing, the One (the light-up path that
    /// joins everything), the Mothership and her stage (a synth to hit: the rim lights race; a dance floor; the band's
    /// people, one of them from further away than the rest), and Funk Street: the Pocket (a club to walk into: a bar
    /// with stools, a dance floor under a mirror ball, a bass to slap, drinks to carry), Big Collar Menswear, a rib
    /// shack, a long low car at the kerb that hops, and Rollerama at the end (an open-air rink under a mirror ball,
    /// skaters going round, skates to carry).
    public partial class FunkKit
    {
        FunkPlacesLife plife; System.Random prng; Transform rootT; LandContext ctx0;

        static readonly Color[] Threads = { new Color(.6f, .2f, .85f), new Color(1f, .5f, .15f), new Color(.95f, .8f, .2f), new Color(.2f, .75f, .55f), new Color(.95f, .25f, .5f), new Color(.92f, .9f, .95f), new Color(.2f, .6f, .95f) };
        static readonly Color[] Flares = { new Color(.3f, .15f, .4f), new Color(.85f, .82f, .9f), new Color(.5f, .2f, .15f), new Color(.15f, .3f, .4f), new Color(.9f, .7f, .2f) };
        static readonly string[] FunkTalk =
        {
            "Everything here lands on the one. Count it: ONE, two, three, four. Feel that? That's the whole religion.", "You don't walk through the Tropics. You strut. Try it. Lead with the shoulder.",
            "The plants keep time. If they stop pulsing, somebody's rushing.", "It's always night, it's always warm and the band's always about to come back on.", "Is that a new step or did you trip? Either way, do it again on purpose.",
        };

        void PlacesStart(LandContext ctx)
        {
            rootT = ctx.Root; ctx0 = ctx; prng = new System.Random(1977);
            plife = new GameObject("Funk places life").AddComponent<FunkPlacesLife>(); plife.transform.SetParent(life.transform, false); plife.rim = tileM;
        }

        Residents.Look Fly(Residents.Tool tool = Residents.Tool.None, bool apron = false) { var l = Residents.Random(prng, Threads, Flares, new[] { Residents.Hat.None, Residents.Hat.Bucket, Residents.Hat.Cap, Residents.Hat.None, Residents.Hat.Cowboy }, Threads); l.tool = tool; l.apron = apron; l.longHair = prng.Next(3) == 0; return l; }
        Resident Folk(Vector3 feet, Vector3 face, Resident.Pose pose, string[] lines = null, Residents.Look look = null, Vector3[] route = null, float seatH = HumanScale.SeatHeight)
            => Residents.Make(plife.transform, feet, face, look ?? Fly(), pose, prng, lines ?? FunkTalk, route, seatH);
        void Col(Vector3 c, Vector3 size, Quaternion rot) => BoxCol(rootT, c, size, rot);
        void Mark(Vector3 at, float r) => noGrass.Add(new Vector3(at.x, r, at.z));
        void Sign(Vector3 at, Vector3 face, string text, float size, Color c) => ArtistBits.Label(rootT, text, at, face, size, c, TextAnchor.MiddleCenter);

        Carryable Thing(MeshKit m, string name, string label, Vector3 at, Quaternion rot, Vector3 size, float mass, bool still = true)
        {
            var go = m.BuildSingle(name, plife.transform, false); go.transform.SetPositionAndRotation(at, rot); var c = Interact.Carry(go, label, mass, at.y - 30f);
            var bc = c.GetComponent<BoxCollider>(); if (bc != null) { bc.size = size; bc.center = new Vector3(0, size.y / 2f, 0); }
            if (still) { var rb = go.GetComponent<Rigidbody>(); if (rb != null) rb.isKinematic = true; }
            return c;
        }

        /// A neon sign: dark board, glowing border, glowing letters. Signs on the +x side of a place's way in.
        void NeonSign(MeshKit mk, Vector3 c, Vector3 face, float w, float h, string text, Color col, float size)
        {
            var rot = Quaternion.LookRotation(face); var up = Vector3.up; var side = Vector3.Cross(up, face); var g = GlowM(col, 2.2f);
            mk.Box(dark, c, new Vector3(w, h, .14f), rot); foreach (var e in new[] { -1f, 1f }) { mk.Box(g, c + up * e * (h / 2f - .06f) + face * .08f, new Vector3(w - .1f, .06f, .04f), rot); mk.Box(g, c + side * e * (w / 2f - .06f) + face * .08f, new Vector3(.06f, h - .1f, .04f), rot); }
            Sign(c + face * .09f, face, text, size, Color.Lerp(col, Color.white, .35f)); halos.Add(c + face * .6f, Mathf.Max(w, h) * .55f, col, .4f);
        }

        /// A log to sit on.
        void LogSeat(MeshKit mk, Vector3 g, Vector3 face, float len = 2.2f)
        {
            var up = Vector3.up; var side = Vector3.Cross(up, face); mk.Prism(trunk, g + up * .28f - side * len / 2f, side, .28f, len, 8); Col(g + up * .28f, new Vector3(len, .56f, .56f), Quaternion.LookRotation(face));
            Interact.Bench(plife.transform, g + up * .58f - side * (len / 2f - .4f), g + up * .58f + side * (len / 2f - .4f), face);
        }

        // ---------------------------------------------------------------- where you arrive
        void Arrival(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var c = ctx.OnGround(center.x, center.z);
            mk.Prism(paveDark, c - up * .26f, up, 13f, .36f, 22); for (var k = 0; k < 22; k++) { var d = Quaternion.Euler(0, k * 360f / 22f, 0) * toShip; mk.Blob(tileM[k % 4], c + d * 12.4f + up * .13f, new Vector3(.24f, .06f, .24f), prng, 0f); }
            mk.Prism(tileM[0], c + up * .1f, up, 2.2f, .02f, 16); mk.Prism(paveDark, c + up * .105f, up, 1.8f, .02f, 16); Sign(c + up * .14f, up, "1", 1.6f, Ink);
            // the sign beside the way to the ship, fruit lamps, logs, someone to say hello
            var sg = c + toShip * 9f + sideways * 9.5f; var sf = -(toShip + sideways).normalized; foreach (var e in new[] { -2.6f, 2.6f }) { mk.Prism(trunk, sg + Vector3.Cross(up, sf) * e - up * .2f, up, .14f, 4f, 6); ArtistBits.PostCollider(ctx.Root, sg + Vector3.Cross(up, sf) * e, 4f, .16f); }
            NeonSign(mk, sg + up * 3f, sf, 6f, 1.8f, "THE FUNK TROPICS", Purple, .4f); NeonSign(mk, sg + up * 1.6f, sf, 4.4f, .7f, "everything on the one", Orange, .18f);
            for (var k = 0; k < 4; k++) { var d = Quaternion.Euler(0, k * 90f + 45f, 0) * toShip; FruitLamp(ctx, mk, c + d * 11f, -d, true, k); }
            LogSeat(mk, c - sideways * 8f + toShip * 3f + up * .1f, sideways); LogSeat(mk, c + sideways * 8f - toShip * 3f + up * .1f, -sideways);
            Folk(c + toShip * 5f - sideways * 4f + up * .1f, -toShip, Resident.Pose.Wave, new[] { "You made it! Mothership's that way, lit up like a birthday. Funk Street's behind you.", "Follow the One. The lit path. It goes everywhere worth going.", "First time? Loosen the knees. The jungle can tell." });
            Mark(c, 14f);
            Shot(ctx, "the arrival and the one", c - toShip * 9f + sideways * 3f + up * 2.2f, c + toShip * 60f + up * 6f);
        }

        /// The One: light-up paving from the clearing to the ship one way and to Funk Street the other.
        void TheOne(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var c = new Vector3(center.x, 0, center.z); var shipD = Vector2.Distance(heart2, ship2) - SaucerR - 12f;
            foreach (var (from, to, dirn) in new[] { (12f, shipD, 1f), (12f, 26f, -1f) })
            {
                var pts = new List<Vector3>(); for (var d = from; d <= to + .1f; d += 2f) pts.Add(ctx.OnGround(c.x + toShip.x * d * dirn, c.z + toShip.z * d * dirn)); if (pts.Count < 2) continue;
                Strip(mk, pts, paveDark, 4.4f, .15f, ctx); foreach (var p in pts) Remember(p, 1);
                for (var i = 0; i < pts.Count; i++) { mk.Box(tileM[i % 4], pts[i] + up * .18f, new Vector3(1.5f, .03f, 1.5f), Quaternion.LookRotation(toShip)); if (i % 6 == 3) foreach (var e in new[] { -1f, 1f }) { var lp = pts[i] + sideways * e * 4.2f; if (!InNoGrass(lp.x, lp.z, .5f)) FruitLamp(ctx, mk, ctx.OnGround(lp.x, lp.z), -sideways * e, e > 0, i); } }
            }
        }

        // ---------------------------------------------------------------- the Mothership
        void Mothership(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var g = ctx.OnGround(ship2.x, ship2.y); var f = new PlaceFrame(g, -toShip);   // f.Fwd = toward the clearing you came from
            mk.Prism(paveDark, g - up * .26f, up, SaucerR + 9f, .36f, 26); for (var k = 0; k < 30; k++) { var d = Quaternion.Euler(0, k * 12f, 0) * toShip; mk.Blob(tileM[k % 4], g + d * (SaucerR + 8.4f) + up * .13f, new Vector3(.26f, .06f, .26f), prng, 0f); }
            // the saucer: her own object, so she can lift a little when the roof comes off
            var sm = new MeshKit(); float R = SaucerR;
            sm.Prism(chrome, new Vector3(0, -1.9f, 0), Vector3.up, R * .42f, 1.9f, 22, 2.35f); sm.Prism(dark, Vector3.zero, Vector3.up, R, .55f, 26); sm.Prism(chrome, new Vector3(0, .55f, 0), Vector3.up, R * .97f, 1.5f, 22, .48f); sm.Prism(dark, new Vector3(0, 2.05f, 0), Vector3.up, R * .46f, .3f, 18);
            var domeM = new Material(WorldMaterials.Glow(Cyan, 1.2f)) { name = "Mothership dome" }; sm.Blob(domeM, new Vector3(0, 2.6f, 0), new Vector3(R * .36f, 2.4f, R * .36f), prng, .02f);
            for (var k = 0; k < 28; k++) { var d = Quaternion.Euler(0, k * 360f / 28f, 0) * Vector3.forward; sm.Blob(tileM[k % 4], d * (R + .05f) + Vector3.up * .28f, Vector3.one * .34f, prng, 0f); }
            for (var k = 0; k < 12; k++) { var d = Quaternion.Euler(0, k * 30f, 0) * Vector3.forward; sm.Blob(tileM[(k + 2) % 4], d * R * .7f - Vector3.up * .9f, Vector3.one * .26f, prng, 0f); }
            var beamM = new Material(WorldMaterials.Glow(Gold, 1.6f)) { name = "Mothership belly" }; sm.Prism(beamM, new Vector3(0, -1.95f, 0), Vector3.up, R * .36f, .06f, 18);
            var sgo = sm.BuildSingle("Mothership", plife.transform, false); sgo.transform.SetPositionAndRotation(g + up * (SaucerH + 1.9f), f.r); life.ship = sgo.transform; life.shipHome = sgo.transform.position; life.dome = domeM; life.belly = beamM;
            life.bellyLight = ArtistBits.PointLight(ctx.Root, g + up * (SaucerH - 1.5f), Gold, 34f, 2.6f); for (var k = 0; k < 6; k++) halos.Add(g + Quaternion.Euler(0, k * 60f, 0) * toShip * R + up * (SaucerH + 2.2f), 4f, Neon[k % 4], .35f); halos.Add(g + up * (SaucerH + 4.5f), 7f, Cyan, .35f);
            // three legs down to the ground
            for (var k = 0; k < 3; k++) { var d = Quaternion.Euler(0, k * 120f + 180f, 0) * f.Fwd; var top = g + d * (R * .62f) + up * (SaucerH + .6f); var foot = g + d * (R * .95f); mk.Prism(chrome, foot, (top - foot).normalized, .26f, (top - foot).magnitude, 7); mk.Prism(dark, foot - up * .1f, up, 1.1f, .3f, 10); ArtistBits.PostCollider(ctx.Root, foot, 3f, .5f); }
            // the ramp let down in front, ending on the stage
            const float SW = 11f, SD = 7f, SH = 1.1f; var sc = f.P(0, 0, R * .55f + SD / 2f); mk.Box(paveDark, sc + up * SH / 2f, new Vector3(SW, SH, SD), f.r); solid.Box(paveDark, sc + up * SH / 2f, new Vector3(SW, SH, SD), f.r); mk.Box(tileM[1], sc + up * (SH + .01f) + f.Fwd * (SD / 2f - .08f), new Vector3(SW, .04f, .14f), f.r);
            var rTop = g + up * (SaucerH + .1f) + f.Fwd * (R * .28f); var rBot = sc + up * SH - f.Fwd * (SD / 2f - .6f); var rd = rBot - rTop; var rrot = Quaternion.LookRotation(rd.normalized, up); mk.Box(chrome, (rTop + rBot) / 2f, new Vector3(3.2f, .2f, rd.magnitude), rrot); solid.Box(chrome, (rTop + rBot) / 2f, new Vector3(3.2f, .3f, rd.magnitude), rrot);
            for (var k = 0; k < 9; k++) foreach (var e in new[] { -1f, 1f }) mk.Blob(tileM[k % 4], Vector3.Lerp(rTop, rBot, (k + .5f) / 9f) + f.Right * e * 1.5f + up * .16f, Vector3.one * .12f, prng, 0f);
            foreach (var e in new[] { -1f, 1f }) for (var k = 0; k < 3; k++) { var st = sc + f.Right * e * (SW / 2f + .4f + (2 - k) * .4f - .2f) + up * ((k + .5f) * SH / 3f) + f.Fwd * 1.5f; mk.Box(paveDark, st, new Vector3(.4f, SH / 3f, 2.4f), f.r); solid.Box(paveDark, st - up * (k * SH / 6f), new Vector3(.4f, SH / 3f * (k + 1), 2.4f), f.r); }
            // on the stage: the synth to hit, a drum kit, a mic, amps
            var so = sc + up * SH; var syn = so + f.Fwd * 1.6f - f.Right * 2.6f; var ym = new MeshKit(); ym.Box(dark, new Vector3(0, .85f, 0), new Vector3(1.6f, .2f, .6f), Quaternion.identity); ym.Box(white, new Vector3(0, .96f, .12f), new Vector3(1.45f, .03f, .26f), Quaternion.identity); ym.Box(dark, new Vector3(0, 1.15f, -.2f), new Vector3(1.6f, .5f, .12f), Quaternion.Euler(-20f, 0, 0)); for (var k = 0; k < 10; k++) ym.Blob(tileM[k % 4], new Vector3(-.65f + k * .145f, 1.2f + (k % 3) * .1f, -.13f), Vector3.one * .04f, prng, 0f); foreach (var e in new[] { -.7f, .7f }) ym.Box(chrome, new Vector3(e, .42f, 0), new Vector3(.05f, .84f, .5f), Quaternion.identity);
            var ygo = ym.BuildSingle("Mothership synth", plife.transform, false); ygo.transform.SetPositionAndRotation(syn, Quaternion.LookRotation(-f.Fwd)); Col(syn + up * .6f, new Vector3(1.6f, 1.2f, .6f), f.r);
            string[] squelch = { "Bwowww. The rim lights race round the ship and the whole clearing goes \"OOOH\".", "A squelchy bass note so fat it has its own gravity.", "You hold one key down. The ship hums it back to you an octave lower.", "Zap! Somebody in the trees shouts \"Make my funk the P-funk!\" and you don't ask who." }; var qn = 0;
            Interact.Toy(ygo, Toy.Kind.Custom, "Hit the synth", new Vector3(1.8f, 1.6f, 1.2f)).onUse = _ => { life.synth = Time.time; Say(plife.transform, syn + up * 2.6f, squelch[qn++ % squelch.Length], 4.5f); };
            var dk = so - f.Fwd * 1.2f + f.Right * 2.4f; mk.Prism(GlowM(Pink, .9f), dk + up * .3f, f.Fwd, .5f, .5f, 14); mk.Prism(white, dk + up * .3f + f.Fwd * .5f, f.Fwd, .47f, .02f, 14); foreach (var e in new[] { -1f, 1f }) { mk.Prism(GlowM(Pink, .9f), dk + f.Right * e * .9f + up * .55f, up, .3f, .25f, 12); mk.Prism(mirror, dk + f.Right * e * 1.2f + up * 1.25f - f.Fwd * .3f, up, .34f, .02f, 14); mk.Prism(chrome, dk + f.Right * e * 1.2f - f.Fwd * .3f, up, .015f, 1.25f, 4); } Col(dk + up * .6f, new Vector3(2.8f, 1.2f, 1.4f), f.r);
            mk.Prism(chrome, so + f.Fwd * 2.6f, up, .02f, 1.5f, 5); mk.Blob(mirror, so + f.Fwd * 2.6f + up * 1.55f, new Vector3(.07f, .1f, .07f), prng, 0f);
            foreach (var e in new[] { -1f, 1f }) { var am = so + f.Right * e * 4.6f - f.Fwd * 2f; mk.Box(dark, am + up * 1f, new Vector3(1.4f, 2f, .8f), f.r); for (var k = 0; k < 2; k++) mk.Prism(floraM[(k + (e > 0 ? 1 : 0)) % 3], am + up * (.55f + k * .95f) + f.Fwd * .41f, f.Fwd, .38f, .03f, 14); Col(am + up * 1f, new Vector3(1.4f, 2f, .8f), f.r); }
            ArtistBits.PointLight(ctx.Root, so + up * 3.4f + f.Fwd * 2f, Purple, 22f, 2.2f);
            var leader = Fly(); leader.top = new Color(.9f, .85f, .95f); leader.hat = Residents.Hat.Cowboy; leader.hatColor = Pink; Folk(so + f.Fwd * 1.2f + f.Right * .8f, f.Fwd, Resident.Pose.Wave, new[] { "We have returned to claim the dance floor! ...It's already claimed? Then we'll share. That's the funk.", "The ship runs on bass. Low end in, lift-off out.", "Citizens of the universe! Put a glide in your stride and a dip in your hip." }, leader);
            // a light-up dance floor in front, dancers, logs to watch from
            var df = sc + f.Fwd * (SD / 2f + 5.5f); for (var x = 0; x < 5; x++) for (var z = 0; z < 4; z++) mk.Box(tileM[(x + z * 3) % 4], df + f.Right * (-4f + x * 2f) + f.Fwd * (-3f + z * 2f) + up * .14f, new Vector3(1.9f, .04f, 1.9f), f.r);
            mk.Box(paveDark, df + up * .05f, new Vector3(10.6f, .14f, 8.6f), f.r);
            var visitor = Fly(); visitor.skin = new Color(.45f, .85f, .5f); visitor.top = new Color(.75f, .76f, .85f); visitor.bottom = new Color(.75f, .76f, .85f); visitor.hat = Residents.Hat.None;
            Folk(df + f.Right * 2f + up * .16f, -f.Fwd, Resident.Pose.Wave, new[] { "I'm with the band. From a little further away than the rest of them.", "On my world we have nine fingers and still can't play the bass like that.", "We came for one song. That was some time ago. Your night is very long and very good." }, visitor);
            Folk(df - f.Right * 2.6f + f.Fwd * 1f + up * .16f, -f.Fwd, Resident.Pose.Wave, new[] { "The floor lights up where the beat is. Step on the purple. NOW the orange.", "I haven't sat down since the ship landed." });
            Folk(df - f.Right * .2f - f.Fwd * 2f + up * .16f, f.Right, Resident.Pose.Wave, new[] { "Watch the hips. No, mine. I'm teaching.", "Tear the roof off? Honey, look up. There isn't one. We already did." });
            LogSeat(mk, df + f.Fwd * 7f - f.Right * 4f, -f.Fwd, 2.6f); LogSeat(mk, df + f.Fwd * 7f + f.Right * 4f, -f.Fwd, 2.6f); LogSeat(mk, df + f.Right * 9f + f.Fwd * 1f, -f.Right, 2.6f);
            Mark(g, SaucerR + 10f); Remember(ctx.OnGround(df.x + f.Fwd.x * 6f, df.z + f.Fwd.z * 6f), 1);
            Shot(ctx, "the mothership", df + f.Fwd * 16f - f.Right * 7f + up * 2.4f, g + up * (SaucerH + 1f));
            Shot(ctx, "on the mothership stage", so - f.Fwd * 2.6f + f.Right * 4f + up * 1.8f, df + up * 1f);
        }

        // ---------------------------------------------------------------- Funk Street
        void FunkStreet(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var c = new Vector3(center.x, 0, center.z); var along = -toShip;   // the street runs away from the ship
            var pts = new List<Vector3>(); for (var d = 24f; d <= 24f + StreetLen + 1f; d += 2f) pts.Add(ctx.OnGround(c.x + along.x * d, c.z + along.z * d));
            Strip(mk, pts, paveDark, StreetW, .16f, ctx); foreach (var p in pts) Remember(p, 2);
            for (var i = 0; i < pts.Count; i++) { if (i % 3 == 0) mk.Box(tileM[(i / 3) % 4], pts[i] + up * .19f, new Vector3(.2f, .03f, 1.6f), Quaternion.LookRotation(along)); foreach (var e in new[] { -1f, 1f }) mk.Box(tileM[(i + (e > 0 ? 2 : 0)) % 4], pts[i] + sideways * e * (StreetW / 2f - .12f) + up * .19f, new Vector3(.12f, .04f, 2f), Quaternion.LookRotation(along)); }
            for (var k = 0; k <= 4; k++) foreach (var e in new[] { -1f, 1f }) { var d = 26f + k * 23.5f; var lp = ctx.OnGround(c.x + along.x * d + sideways.x * e * (StreetW / 2f + .8f), c.z + along.z * d + sideways.z * e * (StreetW / 2f + .8f)); FruitLamp(ctx, mk, lp, -sideways * e, true, k + (e > 0 ? 1 : 3)); }
            Vector3 Lot(float d, float side, float depth) => ctx.OnGround(c.x + along.x * d + sideways.x * side * (StreetW / 2f + 1.6f + depth / 2f), c.z + along.z * d + sideways.z * side * (StreetW / 2f + 1.6f + depth / 2f));
            Pocket(ctx, mk, solid, Lot(48f, 1, 12f), -sideways);
            Shopfront(ctx, mk, Lot(80f, 1, 8f), -sideways, 12f, 5.5f, "BIG COLLAR MENSWEAR", "lapels by the yard", Orange, 0);
            Shopfront(ctx, mk, Lot(104f, 1, 8f), -sideways, 11f, 5f, "WAH-WAH GUITARS", "pedals  ·  strings  ·  attitude", Lime, 1);
            Shopfront(ctx, mk, Lot(40f, -1, 8f), sideways, 12f, 6f, "THE FUNKY CHICKEN", "wings  ·  waffles  ·  all night", Gold, 2);
            RibShack(ctx, mk, Lot(66f, -1, 5f), sideways);
            Shopfront(ctx, mk, Lot(96f, -1, 8f), sideways, 13f, 5.5f, "STAR CHILD RECORDS", "imports from quite far away", Cyan, 3);
            // a rack of hats and collars outside the menswear, to carry off
            var rack = Lot(80f, 1, 0f) - sideways * .2f + along * 3.5f; mk.Prism(chrome, rack, up, .04f, 1.8f, 5); mk.Box(chrome, rack + up * 1.7f, new Vector3(.04f, .04f, 1.6f), Quaternion.LookRotation(along)); ArtistBits.PostCollider(ctx.Root, rack, 1.8f, .1f);
            string[] hats = { "a wide-brimmed purple hat", "a gold lamé cap", "an extremely tall hat" };
            for (var k = 0; k < 3; k++) { var hm = new MeshKit(); var hc = WorldMaterials.Solid(k == 0 ? new Color(.5f, .2f, .75f) : k == 1 ? new Color(.9f, .72f, .25f) : new Color(.9f, .3f, .5f), .5f); hm.Prism(hc, Vector3.zero, Vector3.up, k == 0 ? .34f : .2f, .03f, 12); hm.Prism(hc, new Vector3(0, .03f, 0), Vector3.up, .15f, k == 2 ? .5f : .18f, 10, .9f); Thing(hm, "Hat", hats[k], rack + up * 1.76f + along * (-.6f + k * .6f), Quaternion.identity, new Vector3(.5f, k == 2 ? .55f : .22f, .5f), .2f); }
            Folk(rack - along * 1.6f - sideways * .6f, -sideways, Resident.Pose.Lean, new[] { "A collar's not wide enough until it's a hazard in a doorway.", "Try the tall hat. Taller. There you go. Now you're somebody.", "Platform soles: you get a better view and the ground gets more respect." });
            // the long low car at the kerb: sit in it, hit the switches
            var cg = ctx.OnGround(c.x + along.x * 62f + sideways.x * (StreetW / 2f - 1.4f), c.z + along.z * 62f + sideways.z * (StreetW / 2f - 1.4f)) + up * .18f; var cm = new MeshKit(); var paint = WorldMaterials.Solid(new Color(.42f, .14f, .62f), .85f); var goldM = WorldMaterials.Solid(new Color(.9f, .72f, .25f), .8f);
            cm.Box(paint, new Vector3(0, .5f, 0), new Vector3(1.9f, .5f, 5.6f), Quaternion.identity); cm.Box(goldM, new Vector3(0, .28f, 0), new Vector3(1.94f, .06f, 5.64f), Quaternion.identity); cm.Box(paint, new Vector3(0, .95f, -.5f), new Vector3(1.7f, .42f, 2.2f), Quaternion.identity); cm.Box(WorldMaterials.Solid(new Color(.15f, .18f, .25f), .9f), new Vector3(0, .97f, -.5f), new Vector3(1.73f, .3f, 2f), Quaternion.identity);
            foreach (var (x, z) in new[] { (-.95f, 1.8f), (.95f, 1.8f), (-.95f, -1.8f), (.95f, -1.8f) }) { cm.Prism(dark, new Vector3(x - Mathf.Sign(x) * .12f, .32f, z), Vector3.right * Mathf.Sign(x), .34f, .24f, 12); cm.Prism(white, new Vector3(x + Mathf.Sign(x) * .125f, .32f, z), Vector3.right * Mathf.Sign(x), .24f, .01f, 12); cm.Prism(goldM, new Vector3(x + Mathf.Sign(x) * .13f, .32f, z), Vector3.right * Mathf.Sign(x), .12f, .01f, 8); }
            foreach (var e in new[] { -1f, 1f }) { cm.Box(GlowM(new Color(1f, .95f, .8f), 1.2f), new Vector3(e * .65f, .55f, 2.81f), new Vector3(.4f, .18f, .03f), Quaternion.identity); cm.Box(GlowM(new Color(1f, .15f, .2f), 1.2f), new Vector3(e * .7f, .55f, -2.81f), new Vector3(.36f, .3f, .03f), Quaternion.identity); cm.Box(tileM[e > 0 ? 0 : 3], new Vector3(e * .97f, .2f, 0), new Vector3(.04f, .05f, 4.6f), Quaternion.identity); }
            var cgo = cm.BuildSingle("The long low car", plife.transform, false); cgo.transform.SetPositionAndRotation(cg, Quaternion.LookRotation(along)); plife.car = cgo.transform; plife.carHome = cg; Col(cg + up * .6f, new Vector3(1.9f, 1.2f, 5.6f), Quaternion.LookRotation(along)); halos.Add(cg + up * .2f, 3f, Purple, .35f);
            Interact.Seat(plife.transform, cg + up * .62f + along * .2f - Vector3.Cross(up, along) * .45f, along, "Sit in the long low car");
            var sw = new GameObject("Car switches"); sw.transform.SetParent(plife.transform, false); sw.transform.position = cg + up * 1f + along * 1.2f; string[] hop = { "The front end jumps. Then the back. Then she dances, a little, on three wheels.", "Hiss, clunk, BOUNCE. Somebody across the street nods very slowly.", "She drops low enough to scrape and throws sparks down Funk Street." }; var hn = 0;
            Interact.Toy(sw, Toy.Kind.Custom, "Hit the switches", new Vector3(2.4f, 1.4f, 2f)).onUse = _ => { plife.hop = Time.time; Say(plife.transform, cg + up * 2.4f, hop[hn++ % hop.Length], 4.5f); };
            Folk(cg - Vector3.Cross(up, along) * 1.8f + along * 2.2f, Vector3.Cross(up, along), Resident.Pose.Lean, new[] { "She's a '74. Forty coats of paint. Each one a slightly different purple.", "I don't drive her fast. I drive her slow so everybody gets a turn to look.", "Go on, hit the switches. Just don't touch the paint." });
            // someone strutting the street, a pair on the corner
            var a = ctx.OnGround(c.x + along.x * 30f + sideways.x * 2.4f, c.z + along.z * 30f + sideways.z * 2.4f) + up * .18f; var b = ctx.OnGround(c.x + along.x * 114f + sideways.x * 2.4f, c.z + along.z * 114f + sideways.z * 2.4f) + up * .18f; var route = new List<Vector3>(); for (var k = 0; k <= 12; k++) { var p = Vector3.Lerp(a, b, k / 12f); route.Add(ctx.OnGround(p.x, p.z) + up * .18f); } for (var k = 12; k >= 0; k--) { var p = Vector3.Lerp(a, b, k / 12f) - sideways * 4.8f; route.Add(ctx.OnGround(p.x, p.z) + up * .18f); }
            Folk(route[0], along, Resident.Pose.Walk, new[] { "Can't stop. I'm mid-strut. Walk with me.", "Up one side, down the other, slow. It's not exercise, it's a statement." }, null, route.ToArray()).speed = .85f;
            for (var d = 24f; d <= 24f + StreetLen; d += 12f) Mark(ctx.OnGround(c.x + along.x * d, c.z + along.z * d), 16f);
            Rollerama(ctx, mk, solid, ctx.OnGround(streetEnd2.x - dir2.x * 22f, streetEnd2.y - dir2.y * 22f), -along);
            Shot(ctx, "funk street", ctx.OnGround(c.x + along.x * 27f, c.z + along.z * 27f) + up * 2f, ctx.OnGround(c.x + along.x * 100f, c.z + along.z * 100f) + up * 3f);
        }

        /// A neon shopfront you can't go into: a dark building outlined in light, a glowing window, leaves over the roof.
        void Shopfront(LandContext ctx, MeshKit mk, Vector3 g, Vector3 face, float W, float H, string name, string under, Color col, int i)
        {
            var up = Vector3.up; var f = new PlaceFrame(g + up * .25f, face); const float D = 8f; var wall = WorldMaterials.Glow(Color.Lerp(new Color(.34f, .26f, .5f), col, .25f), .24f); var glow = GlowM(col, 2f);
            mk.Box(wall, f.P(0, H / 2f - .3f, 0), new Vector3(W, H + .6f, D), f.r); BoxCol(ctx.Root, f.P(0, H / 2f - .3f, 0), new Vector3(W, H + .6f, D), f.r);
            foreach (var e in new[] { -1f, 1f }) mk.Box(glow, f.P(e * (W / 2f - .05f), H / 2f, D / 2f + .03f), new Vector3(.08f, H, .05f), f.r); mk.Box(glow, f.P(0, H - .04f, D / 2f + .03f), new Vector3(W, .08f, .05f), f.r); mk.Box(glow, f.P(0, 3f, D / 2f + .03f), new Vector3(W, .06f, .05f), f.r);
            mk.Box(dark, f.P(-W / 2f + 1.5f, 1.1f, D / 2f + .03f), new Vector3(1.1f, 2.2f, .06f), f.r); var win = new Material(WorldMaterials.Glow(Color.Lerp(col, Color.white, .3f), 1.3f)) { name = "Funk window" }; mk.Box(win, f.P(1f, 1.6f, D / 2f + .03f), new Vector3(W - 4.4f, 1.9f, .06f), f.r); halos.Add(f.P(1f, 1.6f, D / 2f + 1f), 2.4f, col, .35f);
            NeonSign(mk, f.P(0, H - 1f, D / 2f + .12f), f.Fwd, W - 1.4f, 1.2f, name, col, Mathf.Min(.4f, (W - 2f) * .55f / Mathf.Max(6, name.Length))); Sign(f.P(1f, 2.8f, D / 2f + .08f), f.Fwd, under, .14f, Ink);
            var lr = new System.Random(i * 7 + 1); for (var k = 0; k < 5; k++) mk.Blob(k % 2 == 0 ? leaf : leafDark, f.P(-W / 2f + 1f + k * (W - 2f) / 4f, H + .3f, D / 2f - 1f - (k % 2)), new Vector3(1.6f, .5f, 1.4f), lr, .35f);
            for (var k = 0; k < 3; k++) mk.Box(floraM[(i + k) % 3], f.P(-W / 2f + .5f + k * (W - 1f) / 2f, H - 1f - k * .3f, D / 2f + .06f), new Vector3(.05f, 1.6f + k * .4f, .03f), f.r);
            ArtistBits.PointLight(ctx.Root, f.P(0, 2.6f, D / 2f + 2.4f), Color.Lerp(col, Color.white, .3f), 12f, 1.5f);
        }

        void RibShack(LandContext ctx, MeshKit mk, Vector3 g, Vector3 face)
        {
            var up = Vector3.up; var f = new PlaceFrame(g + up * .15f, face); var tin = WorldMaterials.Solid(new Color(.3f, .3f, .36f), .6f);
            mk.Box(WorldMaterials.Solid(new Color(.3f, .16f, .14f)), f.P(0, .55f, 0), new Vector3(4.2f, 1.1f, 1f), f.r); mk.Box(tin, f.P(0, 1.13f, 0), new Vector3(4.4f, .06f, 1.2f), f.r); Col(f.P(0, .55f, 0), new Vector3(4.4f, 1.1f, 1.2f), f.r);
            foreach (var e in new[] { -2.1f, 2.1f }) { mk.Prism(trunk, f.P(e, 0, -1.2f), up, .1f, 3f, 6); ArtistBits.PostCollider(ctx.Root, f.P(e, 0, -1.2f), 3f, .12f); } mk.Box(tin, f.P(0, 3f, -.2f), new Vector3(5f, .08f, 3.2f), f.r * Quaternion.Euler(-8f, 0, 0));
            var sm = f.P(-1.2f, 0, -1.4f); mk.Prism(dark, sm + up * .9f - f.Right * .9f, f.Right, .55f, 1.8f, 12); foreach (var e in new[] { -.6f, .6f }) mk.Prism(dark, sm + f.Right * e, up, .05f, .5f, 5); mk.Prism(dark, sm + up * 1.3f + f.Right * .7f, up, .1f, 1.3f, 6); mk.Blob(GlowM(Orange, 1.6f), sm + up * .9f + f.Fwd * .56f, new Vector3(.3f, .12f, .03f), prng, 0f); halos.Add(sm + up * 1f + f.Fwd * .8f, 1.4f, Orange, .5f); Col(sm + up * .8f, new Vector3(2f, 1.6f, 1.2f), f.r);
            NeonSign(mk, f.P(0, 3.6f, .4f), f.Fwd, 3.4f, .9f, "RIBS", Orange, .5f);
            var cook = Fly(Residents.Tool.Spatula, true); Folk(f.P(.8f, -.15f, -1f), f.Fwd, Resident.Pose.Work, new[] { "Low and slow. Same as the groove. You rush either one, you ruin it.", "Sauce is a secret. The secret is more sauce.", "Take a plate. You can't dance on an empty stomach. Well, you can. You shouldn't." }, cook).workRate = .9f;
            for (var k = 0; k < 3; k++) { var pm = new MeshKit(); pm.Prism(white, Vector3.zero, Vector3.up, .16f, .015f, 10); for (var r = 0; r < 3; r++) pm.Box(WorldMaterials.Solid(new Color(.5f, .2f, .12f)), new Vector3(-.06f + r * .06f, .035f, 0), new Vector3(.04f, .04f, .2f), Quaternion.identity); Thing(pm, "Ribs", "a plate of ribs", f.P(-.2f + k * .7f, 1.17f, .15f), f.r, new Vector3(.34f, .08f, .34f), .3f, false); }
            ArtistBits.PointLight(ctx.Root, f.P(0, 2.4f, .8f), Orange, 11f, 1.6f); LogSeat(mk, G0(f.P(0, 0, 3f)), -f.Fwd, 2.4f);
        }

        // ---------------------------------------------------------------- the Pocket
        void Pocket(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 g, Vector3 face)
        {
            var up = Vector3.up; var fo = new PlaceFrame(g + up * .3f, face); const float W = 16f, D = 12f, H = 4.6f, T = .35f, Door = 2.2f, dx = -5f; var wall = WorldMaterials.Glow(new Color(.46f, .24f, .66f), .3f); var floor = WorldMaterials.Glow(new Color(.3f, .2f, .44f), .16f);
            void Wall(Vector3 p, Vector3 s) { mk.Box(wall, p, s, fo.r); solid.Box(wall, p, s, fo.r); }
            mk.Box(floor, fo.P(0, -.06f, 0), new Vector3(W, .12f, D), fo.r); solid.Box(floor, fo.P(0, -.5f, 0), new Vector3(W + 1f, 1f, D + 1f), fo.r);
            Wall(fo.P(0, H / 2f, -D / 2f + T / 2f), new Vector3(W, H, T)); foreach (var e in new[] { -1f, 1f }) Wall(fo.P(e * (W / 2f - T / 2f), H / 2f, 0), new Vector3(T, H, D - T * 2f));
            float l = W / 2f + dx - Door / 2f, r = W / 2f - dx - Door / 2f; Wall(fo.P(-W / 2f + l / 2f, H / 2f, D / 2f - T / 2f), new Vector3(l, H, T)); Wall(fo.P(W / 2f - r / 2f, H / 2f, D / 2f - T / 2f), new Vector3(r, H, T)); Wall(fo.P(dx, (H + 2.4f) / 2f, D / 2f - T / 2f), new Vector3(Door, H - 2.4f, T));
            mk.Box(dark, fo.P(0, H + .1f, 0), new Vector3(W + .4f, .2f, D + .4f), fo.r); solid.Box(dark, fo.P(0, H + .1f, 0), new Vector3(W + .4f, .2f, D + .4f), fo.r); IndoorSpaces.Add(fo.P(0, H / 2f, 0), fo.r, new Vector3(W - T * 2f - .2f, H, D - T * 2f - .2f));
            // outside: the sign, neon round the door, a velvet rope and a doorman
            NeonSign(mk, fo.P(1.5f, H + 1.2f, D / 2f + .1f), fo.Fwd, 9f, 2f, "THE POCKET", Pink, .8f); Sign(fo.P(1.5f, H - .5f, D / 2f + .05f), fo.Fwd, "live tonight: whoever shows up", .2f, Ink);
            foreach (var e in new[] { -1f, 1f }) mk.Box(GlowM(Pink, 2f), fo.P(dx + e * (Door / 2f + .1f), 1.2f, D / 2f + .03f), new Vector3(.08f, 2.4f, .05f), fo.r); mk.Box(GlowM(Pink, 2f), fo.P(dx, 2.44f, D / 2f + .03f), new Vector3(Door + .3f, .08f, .05f), fo.r);
            for (var k = 0; k < 6; k++) mk.Blob(k % 2 == 0 ? leaf : leafDark, fo.P(-W / 2f + 1.4f + k * 2.6f, H + .4f, D / 2f - 1f - (k % 2) * 2f), new Vector3(1.8f, .5f, 1.6f), prng, .35f);
            var dm = Fly(); dm.top = new Color(.1f, .1f, .14f); dm.bottom = new Color(.1f, .1f, .14f); dm.hat = Residents.Hat.None; Folk(G0(fo.P(dx + 2.2f, 0, D / 2f + 1.2f)), fo.Fwd, Resident.Pose.Stand, new[] { "Name's not on the list. There's no list. Go on in.", "Only rule in the Pocket: don't rush the drummer.", "You'll know you're in the pocket when you stop counting." }, dm);
            ArtistBits.PointLight(ctx.Root, fo.P(dx, 2.6f, D / 2f + 2.6f), Pink, 12f, 1.6f);
            // inside: a low stage at the far end with a kit and a bass to slap; a dance floor under the mirror ball; the bar down one side
            float sz = -D / 2f + T + 2f; mk.Box(paveDark, fo.P(2f, .3f, sz), new Vector3(9f, .6f, 4f), fo.r); solid.Box(paveDark, fo.P(2f, .3f, sz), new Vector3(9f, .6f, 4f), fo.r); mk.Box(tileM[3], fo.P(2f, .61f, sz + 1.96f), new Vector3(9f, .03f, .1f), fo.r);
            mk.Box(paveDark, fo.P(2f, .15f, sz + 2.2f), new Vector3(2f, .3f, .4f), fo.r); solid.Box(paveDark, fo.P(2f, .15f, sz + 2.2f), new Vector3(2f, .3f, .4f), fo.r);
            var dk = fo.P(3.6f, .6f, sz - .4f); mk.Prism(GlowM(Purple, .8f), dk + up * .3f, fo.Fwd, .5f, .5f, 14); mk.Prism(white, dk + up * .3f + fo.Fwd * .5f, fo.Fwd, .47f, .02f, 14); foreach (var e in new[] { -1f, 1f }) { mk.Prism(GlowM(Purple, .8f), dk + fo.Right * e * .9f + up * .55f, up, .3f, .25f, 12); mk.Prism(mirror, dk + fo.Right * e * 1.2f + up * 1.25f - fo.Fwd * .3f, up, .34f, .02f, 14); } Col(dk + up * .6f, new Vector3(2.8f, 1.2f, 1.4f), fo.r);
            var bs = fo.P(.2f, .6f, sz + .4f); var bm = new MeshKit(); bm.Prism(chrome, Vector3.zero, Vector3.up, .02f, .9f, 5); bm.Box(WorldMaterials.Solid(new Color(.85f, .3f, .15f), .7f), new Vector3(0, .85f, .05f), new Vector3(.36f, .5f, .06f), Quaternion.identity); bm.Box(woodM(), new Vector3(0, 1.55f, .05f), new Vector3(.07f, .95f, .04f), Quaternion.identity); bm.Box(dark, new Vector3(0, 2.07f, .05f), new Vector3(.1f, .2f, .04f), Quaternion.identity);
            var bgo = bm.BuildSingle("The Pocket's bass", plife.transform, false); bgo.transform.SetPositionAndRotation(bs, fo.r); string[] slap = { "Thumb down, finger up. The room nods like it's been waiting for you.", "You slap the E. The glasses on the bar hum along.", "Four notes and a rest. The rest is the funkiest part.", "The drummer isn't here. You hear the hi-hat anyway." }; var sn = 0;
            Interact.Toy(bgo, Toy.Kind.Custom, "Slap the bass", new Vector3(.9f, 2.2f, .9f)).onUse = _ => { life.plucked = Time.time; plife.slapped = Time.time; Say(plife.transform, bs + up * 2.8f, slap[sn++ % slap.Length], 4.5f); };
            mk.Box(dark, fo.P(-.9f, 1.3f, sz - .9f), new Vector3(1.2f, 1.4f, .7f), fo.r); mk.Prism(floraM[2], fo.P(-.9f, 1.3f, sz - .54f), fo.Fwd, .42f, .03f, 14);
            var dfc = fo.P(2f, 0, 2f); for (var x = 0; x < 4; x++) for (var z = 0; z < 3; z++) mk.Box(tileM[(x + z * 3) % 4], dfc + fo.Right * (-2.7f + x * 1.8f) + fo.Fwd * (-1.8f + z * 1.8f) + up * .02f, new Vector3(1.7f, .04f, 1.7f), fo.r);
            var ball = new MeshKit(); ball.Blob(new Material(WorldMaterials.Glow(new Color(.9f, .92f, 1f), 1.3f)) { name = "Pocket mirror ball" }, Vector3.zero, Vector3.one * .5f, prng, .14f); ball.Prism(chrome, new Vector3(0, .45f, 0), Vector3.up, .02f, .6f, 4); var bgo2 = ball.BuildSingle("Mirror ball", plife.transform, false); bgo2.transform.position = dfc + up * (H - 1.1f); plife.ball = bgo2.transform; halos.Add(dfc + up * (H - 1.1f), 1.6f, Ink, .35f);
            for (var k = 0; k < 3; k++) plife.clubLights.Add(ArtistBits.PointLight(ctx.Root, dfc + up * 3.2f + fo.Right * (-3f + k * 3f), Neon[k * 2 % Neon.Length], 11f, 1.6f, true));
            ArtistBits.PointLight(ctx.Root, fo.P(-2f, H - .8f, 0), new Color(1f, .8f, .95f), 16f, 1.8f, true);
            for (var k = 0; k < 4; k++) { mk.Box(GlowM(Neon[k], 1.8f), fo.P(-W / 2f + T + 2f + k * 3.6f, H - .5f, -D / 2f + T + .03f), new Vector3(3f, .08f, .04f), fo.r); mk.Box(GlowM(Neon[(k + 2) % Neon.Length], 1.8f), fo.P(W / 2f - T - .03f, H - .5f - (k % 2) * .5f, -D / 2f + 1.6f + k * 2.6f), new Vector3(.04f, .08f, 2.2f), fo.r); }
            Folk(dfc + fo.Right * 1f + up * .04f, -fo.Fwd, Resident.Pose.Wave, new[] { "This floor's been open since the seventies and I've been on it since about then.", "If you're thinking about the step you're already late. Feel it." }); Folk(dfc - fo.Right * 1.6f + fo.Fwd * 1f + up * .04f, fo.Right, Resident.Pose.Wave, new[] { "They call it the Pocket because once you're in, you don't want out.", "My platform's two inches taller than his. It matters." });
            // the bar: stools, a bartender, glowing drinks to carry
            var bc = fo.P(-W / 2f + T + 1.3f, 0, .5f); var brot = Quaternion.LookRotation(fo.Right); mk.Box(WorldMaterials.Solid(new Color(.3f, .12f, .4f)), bc + up * .55f, new Vector3(6f, 1.1f, .7f), brot); mk.Box(tileM[0], bc + up * 1.11f + fo.Right * .3f, new Vector3(6f, .04f, .1f), brot); mk.Box(dark, bc + up * 1.12f, new Vector3(6.2f, .06f, .9f), brot); Col(bc + up * .55f, new Vector3(6.2f, 1.1f, .9f), brot);
            for (var k = 0; k < 4; k++) { var st = bc + fo.Right * .95f + fo.Fwd * (-2.1f + k * 1.4f); mk.Prism(chrome, st, up, .05f, .7f, 6); mk.Prism(GlowM(Purple, .6f), st + up * .7f, up, .24f, .1f, 12); ArtistBits.PostCollider(ctx.Root, st, .7f, .2f); Interact.Seat(plife.transform, st + up * .82f, -fo.Right, "Sit at the bar").seatHeight = .82f; }
            for (var k = 0; k < 7; k++) mk.Prism(WorldMaterials.Glow(Neon[k % Neon.Length], .9f), fo.P(-W / 2f + T + .2f, 1.7f + (k % 2) * .5f, -2.2f + k * .75f), up, .06f, .34f, 6, .5f);
            var bt = Fly(Residents.Tool.Mug, true); Folk(bc - fo.Right * .9f, fo.Right, Resident.Pose.Work, new[] { "What'll it be? They all glow. That's not a gimmick, that's the water round here.", "House special's called the One. It comes when you least expect it.", "I've heard every bass line there is from behind this bar. There are four. It's what you do with them." }, bt).workRate = .8f;
            for (var k = 0; k < 4; k++) { var gm = new MeshKit(); gm.Prism(WorldMaterials.Glow(Neon[(k * 2 + 1) % Neon.Length], 1.2f), Vector3.zero, Vector3.up, .045f, .15f, 8, 1.3f); gm.Prism(chrome, new Vector3(0, -.01f, 0), Vector3.up, .05f, .012f, 8); Thing(gm, "Drink", "a drink that glows", bc + up * 1.16f + fo.Fwd * (-1.6f + k * 1f) + fo.Right * .1f, Quaternion.identity, new Vector3(.12f, .16f, .12f), .15f, false); }
            // a booth along the front wall
            var bo = fo.P(4.6f, 0, D / 2f - T - .5f); mk.Box(WorldMaterials.Solid(new Color(.45f, .12f, .35f)), bo + up * .25f, new Vector3(5f, .5f, .7f), fo.r); mk.Box(WorldMaterials.Solid(new Color(.45f, .12f, .35f)), bo + up * .85f + fo.Fwd * .3f, new Vector3(5f, .8f, .14f), fo.r); Col(bo + up * .45f, new Vector3(5f, .9f, .8f), fo.r); Interact.Bench(plife.transform, bo + up * .52f - fo.Right * 2f, bo + up * .52f + fo.Right * 2f, -fo.Fwd);
            Folk(bo + up * .52f + fo.Right * 1.8f, -fo.Fwd, Resident.Pose.Sit, new[] { "I'm resting my feet, not my spirit.", "Best seat in the house. You can see the whole floor and nobody can see you can't dance." }, null, null, .52f);
            Mark(g, 11f);
            Shot(ctx, "inside the pocket", fo.P(dx, 1.7f, D / 2f - 1.2f), fo.P(2.6f, 1.2f, sz));
        }

        Material woodM() => WorldMaterials.Solid(new Color(.5f, .34f, .2f));

        // ---------------------------------------------------------------- Rollerama
        void Rollerama(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 g, Vector3 toStreet)
        {
            var up = Vector3.up; const float R = 15f; var f = new PlaceFrame(g, toStreet);
            mk.Prism(WorldMaterials.Solid(new Color(.16f, .12f, .22f), .8f), g - up * .25f, up, R, .4f, 30); mk.Prism(tileM[0], g + up * .16f, up, R - 1f, .02f, 30); mk.Prism(WorldMaterials.Solid(new Color(.16f, .12f, .22f), .8f), g + up * .165f, up, R - 1.25f, .02f, 30); mk.Prism(tileM[2], g + up * .17f, up, 5.2f, .02f, 22); mk.Prism(WorldMaterials.Solid(new Color(.16f, .12f, .22f), .8f), g + up * .175f, up, 4.95f, .02f, 22);
            // the rail, open toward the street and at the back; arches of light over it
            for (var k = 0; k < 24; k++)
            {
                var a = k * 15f; if (Mathf.Abs(Mathf.DeltaAngle(a, 0f)) < 16f || Mathf.Abs(Mathf.DeltaAngle(a, 180f)) < 10f) continue;
                var d = Quaternion.Euler(0, a, 0) * f.Fwd; var p = g + d * R; var tr = Quaternion.LookRotation(Vector3.Cross(up, d)); mk.Box(chrome, p + up * 1f, new Vector3(.08f, .08f, R * .27f), tr); mk.Prism(chrome, p, up, .05f, 1f, 5); mk.Blob(tileM[k % 4], p + up * 1.1f, Vector3.one * .1f, prng, 0f); Col(p + up * .55f, new Vector3(.12f, 1.1f, R * .27f), tr);
            }
            for (var k = 0; k < 4; k++) { var d = Quaternion.Euler(0, k * 90f + 45f, 0) * f.Fwd; var p = g + d * (R + .6f); mk.Prism(chrome, p, up, .12f, 6.5f, 6); ArtistBits.PostCollider(ctx.Root, p, 6f, .14f); var top = p + up * 6.5f; var mid = g + up * 9f; var prev = top; for (var s = 1; s <= 8; s++) { var q = Vector3.Lerp(top, mid, s / 8f) + up * Mathf.Sin(s / 8f * Mathf.PI) * .8f; mk.Prism(dark, prev, (q - prev).normalized, .03f, Vector3.Distance(prev, q), 4); mk.Blob(tileM[(s + k) % 4], q, Vector3.one * .16f, prng, 0f); prev = q; } }
            var ball = new MeshKit(); ball.Blob(new Material(WorldMaterials.Glow(new Color(.9f, .92f, 1f), 1.5f)) { name = "Rollerama mirror ball" }, Vector3.zero, Vector3.one * 1.1f, prng, .12f); var bgo = ball.BuildSingle("Rollerama mirror ball", plife.transform, false); bgo.transform.position = g + up * 7.8f; plife.rinkBall = bgo.transform; halos.Add(g + up * 7.8f, 5f, Ink, .4f);
            for (var k = 0; k < 4; k++) plife.rinkLights.Add(ArtistBits.PointLight(ctx.Root, g + Quaternion.Euler(0, k * 90f, 0) * f.Fwd * 7f + up * 4.5f, Neon[k], 20f, 1.8f));
            // skaters going round (fast), one wobbling in the middle
            string[][] sk = { new[] { "Can't stop! No, really, I can't stop. I never learned.", "Backwards is easier. You can't see what you're about to hit." }, new[] { "Couples' skate is next. It's always next.", "Stay on the outside lane if you're slow. I say that with love." }, new[] { "Forty laps a song. I've done the maths. It's more than that now.", "The trick is to skate on the one and glide on the two, three, four." } };
            for (var s = 0; s < 3; s++) { var rr = 8f + s * 2.2f; var route = new List<Vector3>(); for (var k = 0; k < 16; k++) route.Add(g + Quaternion.Euler(0, (s % 2 == 0 ? 1f : -1f) * k * 22.5f + s * 70f, 0) * f.Fwd * rr + up * .18f); Folk(route[0], FaceTo(route[0], route[1]), Resident.Pose.Walk, sk[s], null, route.ToArray()).speed = 3.4f - s * .5f; }
            Folk(g + f.Right * 1.5f + up * .18f, f.Fwd, Resident.Pose.Wave, new[] { "I'm fine! I'm fine. I'm just going to stand here in the middle where it's safe.", "Everybody else makes it look easy. I make it look honest." });
            // the booth by the way in: skates to carry; benches to lace up on
            var bo = g + f.Fwd * (R + 3.4f) + f.Right * 6.5f; mk.Box(WorldMaterials.Solid(new Color(.2f, .12f, .3f)), bo + up * 1.3f, new Vector3(3f, 2.6f, 2.2f), f.r); Col(bo + up * 1.3f, new Vector3(3f, 2.6f, 2.2f), f.r); mk.Box(dark, bo + up * 1.1f - f.Right * 1.52f, new Vector3(.04f, .08f, 1.9f), f.r); mk.Box(chrome, bo + up * 1.05f - f.Right * 1.8f, new Vector3(.6f, .06f, 2f), f.r);
            NeonSign(mk, bo + up * 3.1f - f.Right * .2f, -f.Right, 2.6f, .7f, "SKATES", Cyan, .28f);
            for (var k = 0; k < 4; k++) { var skm = new MeshKit(); var boot = WorldMaterials.Solid(k % 2 == 0 ? new Color(.95f, .95f, .9f) : new Color(.9f, .3f, .6f)); skm.Box(boot, new Vector3(0, .12f, 0), new Vector3(.1f, .12f, .26f), Quaternion.identity); skm.Box(boot, new Vector3(0, .24f, -.07f), new Vector3(.1f, .16f, .12f), Quaternion.identity); foreach (var (x, z) in new[] { (-.06f, .08f), (.06f, .08f), (-.06f, -.08f), (.06f, -.08f) }) skm.Prism(tileM[k % 4], new Vector3(x - .02f * Mathf.Sign(x), .035f, z), Vector3.right * Mathf.Sign(x), .035f, .03f, 8); Thing(skm, "Skate", "a roller skate (just the one)", bo + up * 1.09f - f.Right * 1.8f + f.Fwd * (-.7f + k * .46f), f.r * Quaternion.Euler(0, 90f, 0), new Vector3(.14f, .32f, .28f), .4f); }
            var kid = Fly(); Folk(bo - f.Right * 2.8f - f.Fwd * .2f, -f.Right, Resident.Pose.Lean, new[] { "Skates are free. Dignity's extra.", "We only rent them one at a time. Keeps people humble." }, kid);
            LogSeat(mk, g + f.Fwd * (R + 3f) - f.Right * 6f, -f.Fwd, 2.6f); LogSeat(mk, g - f.Right * (R + 2.6f), f.Right, 2.6f); LogSeat(mk, g + f.Right * (R + 2.6f) - f.Fwd * 4f, -f.Right, 2.6f);
            foreach (var e in new[] { -5f, 5f }) { mk.Prism(chrome, g + f.Fwd * (R + 1.5f) + f.Right * e, up, .12f, 5f, 6); ArtistBits.PostCollider(ctx.Root, g + f.Fwd * (R + 1.5f) + f.Right * e, 5f, .14f); } NeonSign(mk, g + f.Fwd * (R + 1.5f) + up * 5.2f, f.Fwd, 10.6f, 1.6f, "ROLLERAMA", Pink, .8f);
            Mark(g, R + 5f); Remember(ctx.OnGround(g.x + f.Fwd.x * (R + 2f), g.z + f.Fwd.z * (R + 2f)), 2);
            Shot(ctx, "rollerama", g + f.Fwd * (R + 11f) - f.Right * 8f + up * 3.4f, g + up * 3.5f);
        }
    }

    /// The Funk places' moving parts: the bass vines when plucked, the mirror balls, the car that hops, the club's lights.
    public class FunkPlacesLife : MonoBehaviour
    {
        public List<(Material m, Color c, float at)> vines = new List<(Material, Color, float)>(); public Material[] rim; public Transform ball, rinkBall, car; public Vector3 carHome;
        public List<Light> clubLights = new List<Light>(), rinkLights = new List<Light>(); public float hop = -99f, slapped = -99f;

        void Update()
        {
            var t = Time.time; var dt = Time.deltaTime;
            for (var i = 0; i < vines.Count; i++) { var (m, c, at) = vines[i]; if (m == null) continue; var a = t - at; m.SetColor("_EmissionColor", c * (.9f + (a < 3f ? 3f * Mathf.Exp(-a * 1.6f) * (.6f + .4f * Mathf.Sin(a * 40f)) : 0f))); }
            if (ball != null) ball.Rotate(0, 30f * dt, 0, Space.World); if (rinkBall != null) rinkBall.Rotate(0, 22f * dt, 0, Space.World);
            if (car != null) { var a = t - hop; var y = a < 3.2f ? Mathf.Abs(Mathf.Sin(a * 7f)) * .5f * Mathf.Exp(-a * .9f) : 0f; var tilt = a < 3.2f ? Mathf.Sin(a * 7f) * 7f * Mathf.Exp(-a * .9f) : 0f; car.position = carHome + Vector3.up * y; var e = car.eulerAngles; car.rotation = Quaternion.Euler(tilt, e.y, 0); }
            var beat = (float)(SharedChance.Now * 1.75 % 1.0);   // 105 beats a minute, the same for everyone
            var kick = Mathf.Pow(1f - beat, 3f);
            for (var i = 0; i < clubLights.Count; i++) if (clubLights[i] != null) clubLights[i].intensity = .7f + 1.6f * kick * ((i + Mathf.FloorToInt((float)(SharedChance.Now * 1.75))) % 3 == 0 ? 1f : .3f) + (t - slapped < 2f ? 1.2f : 0f);
            for (var i = 0; i < rinkLights.Count; i++) if (rinkLights[i] != null) rinkLights[i].intensity = 1f + 1.4f * kick * ((i + Mathf.FloorToInt((float)(SharedChance.Now * 1.75))) % 4 == 0 ? 1f : .25f);
        }
    }
}
