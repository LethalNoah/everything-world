using System.Collections.Generic;
using UnityEngine;
using static EverythingWorld.World.PlaceTools;

namespace EverythingWorld.World
{
    /// The Afrobeat Coast's places (part of AfroKit): the roundabout (the Big Drum to beat, a parked danfo bus to sit
    /// in, drummers, painted houses round it); Market Road (red earth, strings of coloured bulbs, a yellow bus going
    /// up and down, stalls of cloth, fruit, drums, grilling suya, cold drinks and records down both sides); and the
    /// beach at the bottom (three bonfires with logs to sit on, a drum circle to join, painted canoes, a football
    /// pitch in the sand with a ball to kick about).
    public partial class AfroKit
    {
        AfroTownLife tlife; System.Random prng; Transform rootT; LandContext ctx0;

        static readonly Color[] Tops = { new Color(.95f, .3f, .15f), new Color(1f, .8f, .15f), new Color(.1f, .6f, .35f), new Color(.2f, .35f, .8f), new Color(.65f, .25f, .65f), new Color(1f, .55f, .1f), new Color(.95f, .95f, .9f), new Color(.1f, .7f, .7f) };
        static readonly Color[] Bottoms = { new Color(.16f, .18f, .3f), new Color(.9f, .86f, .76f), new Color(.5f, .2f, .15f), new Color(.1f, .1f, .12f), new Color(.2f, .45f, .3f) };
        static readonly string[] TownTalk =
        {
            "You're welcome! Sun's going down, so now it starts properly.", "Follow the road to the water. You'll smell the fires before you see them.", "No wahala. Whatever it is: no wahala.",
            "The bus goes up, the bus goes down. If you miss it, another one is the same one.", "Everybody is somebody's cousin here. You too, now.",
        };

        void TownStart(LandContext ctx)
        {
            rootT = ctx.Root; ctx0 = ctx; prng = new System.Random(1977);
            tlife = new GameObject("Afro town life").AddComponent<AfroTownLife>(); tlife.transform.SetParent(life.transform, false); tlife.life = life;
        }

        Residents.Look Local(Residents.Tool tool = Residents.Tool.None, bool apron = false) { var l = Residents.Random(prng, Tops, Bottoms, new[] { Residents.Hat.None, Residents.Hat.Cap, Residents.Hat.None, Residents.Hat.Bucket, Residents.Hat.None }, Tops); l.tool = tool; l.apron = apron; return l; }
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

        /// Something to use that is only a spot in the air.
        Toy Spot(string name, Vector3 at, string verb, Vector3 size)
        {
            var go = new GameObject(name); go.transform.SetParent(tlife.transform, false); go.transform.position = at; return Interact.Toy(go, Toy.Kind.Custom, verb, size);
        }

        /// A danfo: a yellow minibus with two black stripes, built round its own middle, facing +z.
        void Danfo(MeshKit m, string slogan, Transform labels)
        {
            var yellow = WorldMaterials.Glow(new Color(1f, .78f, .1f), .22f); var glass = WorldMaterials.Solid(new Color(.16f, .2f, .26f), .9f);
            m.Box(yellow, new Vector3(0, 1.25f, 0), new Vector3(1.9f, 1.7f, 4.6f), Quaternion.identity); m.Box(yellow, new Vector3(0, 2.14f, -.1f), new Vector3(1.8f, .1f, 4.2f), Quaternion.identity);
            foreach (var y in new[] { .9f, 1.12f }) foreach (var e in new[] { -1f, 1f }) m.Box(dark, new Vector3(e * .96f, y, 0), new Vector3(.02f, .09f, 4.6f), Quaternion.identity);
            foreach (var e in new[] { -1f, 1f }) for (var k = 0; k < 3; k++) m.Box(glass, new Vector3(e * .96f, 1.65f, -1.4f + k * 1.3f), new Vector3(.03f, .56f, 1f), Quaternion.identity);
            m.Box(glass, new Vector3(0, 1.62f, 2.31f), new Vector3(1.6f, .64f, .03f), Quaternion.identity); m.Box(dark, new Vector3(0, .5f, 2.32f), new Vector3(1.9f, .2f, .08f), Quaternion.identity);
            foreach (var e in new[] { -1f, 1f }) { m.Blob(windowM, new Vector3(e * .66f, .86f, 2.32f), Vector3.one * .1f, prng, 0f); m.Blob(emberM, new Vector3(e * .7f, .9f, -2.32f), Vector3.one * .07f, prng, 0f); foreach (var s in new[] { -1.5f, 1.5f }) m.Prism(dark, new Vector3(e * .86f, .36f, s), Vector3.right * e, .36f, .2f, 10); }
            m.Box(rustM, new Vector3(0, 2.26f, -.4f), new Vector3(1.4f, .16f, 2.6f), Quaternion.identity); m.Blob(Paint(Cloth[0]), new Vector3(-.2f, 2.5f, -.8f), new Vector3(.5f, .2f, .6f), prng, .2f); m.Blob(Paint(Cloth[3]), new Vector3(.3f, 2.46f, .2f), new Vector3(.4f, .16f, .5f), prng, .2f);
            if (labels != null) { var t = ArtistBits.Label(labels, slogan, new Vector3(0, 1.26f, -2.32f), Vector3.back, .16f, Ink, TextAnchor.MiddleCenter); t.transform.localPosition = new Vector3(0, 1.26f, -2.32f); t.transform.localRotation = Quaternion.LookRotation(Vector3.forward); }
        }

        // ---------------------------------------------------------------- the roundabout
        void Roundabout(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var c = ctx.OnGround(center.x, center.z);
            mk.Prism(earth, c - up * .28f, up, 23f, .4f, 30);
            // the Big Drum on its island, east of where you arrive: beat it
            var di = c + R * 11f + up * .12f; mk.Prism(concrete, di - up * .1f, up, 4.6f, .5f, 20); solid.Prism(concrete, di - up * .4f, up, 4.6f, .8f, 14); mk.Prism(Paint(Paints[0]), di + up * .4f, up, 4.7f, .08f, 20); for (var k = 0; k < 10; k++) mk.Box(k % 2 == 0 ? dark : white, di + up * .2f + Quaternion.Euler(0, k * 36f, 0) * Vector3.forward * 4.62f, new Vector3(1.5f, .4f, .06f), Quaternion.Euler(0, k * 36f, 0));
            var dm = new MeshKit(); Drum(dm, Vector3.zero, 8f, WorldMaterials.Glow(new Color(.96f, .9f, .78f), .3f)); for (var k = 0; k < 3; k++) dm.Prism(Paint(Cloth[k]), new Vector3(0, .5f + k * 1.9f, 0), Vector3.up, k == 1 ? .96f : 1.3f, .12f, 10);
            var dgo = dm.BuildSingle("The Big Drum", tlife.transform, false); dgo.transform.position = di + up * .4f; ArtistBits.PostCollider(ctx.Root, di, 5f, 1.7f); tlife.drum = dgo.transform;
            string[] beat = { "DOOM. The whole hilltop feels it. Down the road, somebody answers on a smaller drum.", "Gun, go-do, gun. The drum says a sentence. A woman at a stall laughs and says the next one.", "You hit it and the bulbs over the road jump, all the way down to the sea.", "DOOM, da-DOOM. A bus horn joins in, exactly on the beat." }; var bn = 0;
            Interact.Toy(dgo, Toy.Kind.Custom, "Beat the Big Drum", new Vector3(4f, 5f, 4f)).onUse = _ => { life.played = Time.time; tlife.hit = Time.time; Say(tlife.transform, di + up * 6.6f, beat[bn++ % beat.Length], 5f); };
            for (var k = 0; k < 12; k++) { var d = Quaternion.Euler(0, k * 30f, 0) * Vector3.forward; mk.Blob(bulbM[k % 3], di + d * 4.4f + up * .62f, Vector3.one * .1f, prng, 0f); } halos.Add(di + up * 3f, 5f, Orange, .3f); ArtistBits.PointLight(ctx.Root, di + up * 3f + U * 4f, new Color(1f, .7f, .4f), 20f, 2.4f);
            // two drummers by it, and somebody to say welcome
            var d1 = di - R * 5.6f + U * 2.4f; Drum(mk, d1 + R * .5f, 1.4f); Folk(d1, R, Resident.Pose.Work, new[] { "Small drum talks, big drum answers. Hit the big one, I'll translate.", "My father played this corner. His father played it when it was a path." }).workRate = 1.6f;
            Folk(c - R * 4f + U * 6f + up * .12f, (-U + R * .3f).normalized, Resident.Pose.Wave, new[] { "You're welcome! Market Road is that way, straight down to the beach. You can't get lost, there's only down.", "The Shrine is just there, behind me. They've been on the same song since yesterday. It's a good song.", "Eat something. Dance a little. Then go and sit by a fire." });
            // the sign, lamps, benches
            var sg = c + U * 13f - R * 12f + up * .12f; var sf = (R - U).normalized; foreach (var e in new[] { -2.8f, 2.8f }) { mk.Prism(woodM, sg + Vector3.Cross(up, sf) * e - up * .2f, up, .1f, 4f, 6); ArtistBits.PostCollider(ctx.Root, sg + Vector3.Cross(up, sf) * e, 3.8f, .12f); }
            mk.Box(Paint(Paints[4]), sg + up * 3.1f, new Vector3(6.2f, 1.6f, .12f), Quaternion.LookRotation(sf)); mk.Box(Paint(Paints[0]), sg + up * 2.36f + sf * .07f, new Vector3(6.2f, .12f, .04f), Quaternion.LookRotation(sf)); Sign(sg + up * 3.3f + sf * .08f, sf, "THE AFROBEAT COAST", .4f, Cream); Sign(sg + up * 2.72f + sf * .08f, sf, "you are welcome", .22f, Gold);
            for (var k = 0; k < 5; k++) { var d = Quaternion.Euler(0, k * 72f + 20f, 0) * U; if (Vector3.Dot(d, -R) > .8f) continue; BulbPole(ctx, mk, c + d * 20f + up * .12f, k, true); }
            Bench(mk, c + U * 15f + R * 2f + up * .12f, -U, 1); Bench(mk, c - U * 16f + R * 9f + up * .12f, (U - R * .3f).normalized, 2); Bench(mk, c + R * 18.5f - U * 6f + up * .12f, -R, 5);
            // a danfo parked at the kerb with its door open: sit in
            var bp = c - U * 13f - R * 7f + up * .12f; var br = Quaternion.LookRotation(-R); var bm = new MeshKit(); Danfo(bm, "", null); var bgo = bm.BuildSingle("Parked danfo", ctx.Root, false); bgo.transform.SetPositionAndRotation(bp, br); Col(bp + up * 1.2f, new Vector3(1.9f, 2.2f, 4.6f), br);
            Sign(bp + up * 1.26f + R * 2.33f, R, "NO SHAKING", .16f, Ink); Interact.Seat(tlife.transform, bp + up * .9f + U * 1.1f, U, "Sit in the danfo's doorway").seatHeight = .7f;
            Folk(bp + U * 2.2f - R * 1.4f, U, Resident.Pose.Wave, new[] { "Beach! Beach! Beach! One more passenger and we go! (He has been saying this for an hour.)", "Enter with your change, o! ...I'm joking, it doesn't go anywhere. That one goes.", "Hold something. He drives like the song is ending." });
            Mark(c, 24f);
            Shot(ctx, "the roundabout", c - U * 17f - R * 12f + up * 2.6f, di + up * 3f);
        }

        // ---------------------------------------------------------------- Market Road
        void MarketRoad(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var pts = new List<Vector3>(); for (var d = 22f; d <= roadL + .01f; d += 2f) pts.Add(G(ctx, 0, -d)); if (pts.Count < 2) return;
            Strip(mk, pts, earth, RoadW, .14f, ctx); foreach (var p in pts) Remember(p, 1);
            // strings of coloured bulbs over the road, pole to pole
            var n = 0; for (var d = 30f; d <= roadL - 6f; d += 13f, n++)
            {
                var a = G(ctx, RoadW / 2f + .7f, -d); var b = G(ctx, -(RoadW / 2f + .7f), -d); foreach (var g in new[] { a, b }) { mk.Prism(woodM, g - up * .3f, up, .08f, 5f, 5, .8f); ArtistBits.PostCollider(ctx.Root, g, 4.6f, .1f); }
                BulbString(mk, a + up * 4.6f, b + up * 4.6f, n); if (n % 2 == 0) ArtistBits.PointLight(ctx.Root, (a + b) / 2f + up * 3.6f, new Color(1f, .68f, .4f), 16f, 2.2f);
            }
            // the bus that goes up and down: it stops for anyone in the road
            var bm = new MeshKit(); var bus = new GameObject("The danfo").transform; bus.SetParent(tlife.transform, false); Danfo(bm, "GOD'S TIME", bus); var body = bm.BuildSingle("Body", bus, false); body.transform.localPosition = Vector3.zero;
            var way = new List<Vector3>(); for (var d = 32f; d <= roadL - 10f; d += 4f) way.Add(G(ctx, -1.6f, -d) + up * .14f); tlife.bus = bus; tlife.way = way.ToArray(); if (way.Count > 0) bus.position = way[0];
            // people walking it
            var route = new List<Vector3>(); for (var d = 28f; d <= roadL - 8f; d += 14f) route.Add(G(ctx, 2.4f, -d) + up * .14f); for (var d = roadL - 8f; d >= 28f; d -= 14f) route.Add(G(ctx, 1.2f, -d) + up * .14f);
            if (route.Count > 3) { Folk(route[0], -R, Resident.Pose.Walk, new[] { "Down to the fire, up for food, down to the fire. That's the evening.", "I only came out for bread. That was the first song." }, null, route.ToArray()).speed = 1.2f; var r2 = new List<Vector3>(route); r2.Reverse(); Folk(r2[2], R, Resident.Pose.Walk, new[] { "Walk on the beat, it's easier. The road is on the beat.", "You see that sun? It waits for us. Every night it waits till the party's started." }, null, r2.ToArray()).speed = 1f; }
            Shot(ctx, "down market road to the sun", G(ctx, 0, -24f) + up * 2.4f, G(ctx, 0, -Mathf.Min(roadL, 140f)) + up * 3f);
            Shot(ctx, "among the stalls", G(ctx, -1.5f, -Mathf.Min(roadL * .5f, 96f)) + up * 1.9f, G(ctx, 5f, -Mathf.Min(roadL * .5f, 96f) - 16f) + up * 1.6f);
        }

        // ---------------------------------------------------------------- the stalls
        void Stalls(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var i = 0; var built = 0;
            foreach (var e in new[] { 1f, -1f })
            for (var d = 26f + (e > 0 ? 0f : 3f); d <= roadL - 10f; d += 6.6f, i++)
            {
                var p = Wp(e * (RoadW / 2f + 2.5f), -d); if (!ctx.IsLand(p.x, p.z, 4f) || InFootprint(ctx, p.x, p.z, 2.6f) || LaneNear(p.x, p.z, 4.4f)) continue;
                Stall(ctx, mk, new PlaceFrame(ctx.OnGround(p.x, p.z), -U * e), i); built++;
            }
            Debug.Log($"The Afrobeat Coast: {built} market stalls");
        }

        /// A market stall facing the road: four poles, a striped awning, a table of something, a few bulbs.
        void Stall(LandContext ctx, MeshKit mk, PlaceFrame f, int i)
        {
            var up = Vector3.up; var rng = new System.Random(i * 31 + 7); var kind = i % 6; var c1 = Paint(Cloth[i % 6]); var c2 = Paint(Cloth[(i + 3) % 6 == i % 6 ? 7 : (i * 5 + 1) % 6 == i % 6 ? 7 : (i * 5 + 1) % 6]);
            foreach (var (x, zz) in new[] { (-1.4f, -.9f), (1.4f, -.9f), (1.4f, 1f), (-1.4f, 1f) }) mk.Prism(woodM, f.P(x, -.4f, zz), up, .05f, zz > 0 ? 2.6f : 3f, 4); ArtistBits.PostCollider(ctx.Root, f.P(-1.4f, 0, 1f), 2.2f, .07f); ArtistBits.PostCollider(ctx.Root, f.P(1.4f, 0, 1f), 2.2f, .07f);
            for (var s = 0; s < 6; s++) mk.Box(s % 2 == 0 ? c1 : c2, f.P(-1.375f + s * .55f, 2.42f + (s % 2) * .012f, .1f), new Vector3(.53f, .04f, 2.5f), f.r * Quaternion.Euler(10f, 0, 0));
            mk.Box(woodM, f.P(0, .78f, .2f), new Vector3(2.6f, .08f, 1f), f.r); mk.Box(kind == 0 ? c2 : c1, f.P(0, .44f, .69f), new Vector3(2.6f, .7f, .03f), f.r); foreach (var x in new[] { -1.2f, 1.2f }) mk.Box(woodM, f.P(x, .37f, .2f), new Vector3(.08f, .74f, .9f), f.r); Col(f.P(0, .45f, .2f), new Vector3(2.6f, .9f, 1f), f.r);
            for (var k = 0; k < 4; k++) mk.Blob(bulbM[(i + k) % 3], f.P(-1.2f + k * .8f, 2.14f, 1.3f), Vector3.one * .08f, rng, 0f); if (i % 2 == 0) halos.Add(f.P(0, 2.1f, 1.4f), 1.5f, Gold, .32f);
            var top = f.P(0, .82f, .2f);
            switch (kind)
            {
                case 0:   // printed cloth: bolts on the table, lengths hanging at the back
                    for (var k = 0; k < 5; k++) mk.Box(Paint(Cloth[(i + k * 2) % Cloth.Length]), top + f.Right * (-1f + k * .5f) + up * .1f, new Vector3(.4f, .2f, .8f), f.r); for (var k = 0; k < 4; k++) { mk.Box(Paint(Cloth[(i + k) % 6]), f.P(-1.05f + k * .7f, 1.6f, -.86f), new Vector3(.6f, 1.5f, .03f), f.r); mk.Box(Paint(Cloth[(i + k + 2) % 6]), f.P(-1.05f + k * .7f, 1.6f, -.82f), new Vector3(.2f, 1.44f, .02f), f.r); } break;
                case 1:   // fruit: heaps of oranges and mangoes; one to carry
                    for (var h = 0; h < 3; h++) for (var k = 0; k < 7; k++) mk.Blob(GlowM(h == 1 ? new Color(.95f, .75f, .15f) : h == 0 ? new Color(1f, .5f, .1f) : new Color(.3f, .6f, .2f), .25f), top + f.Right * (-.9f + h * .9f) + Quaternion.Euler(0, k * 60f, 0) * Vector3.forward * (k == 6 ? 0f : .16f) + up * (k == 6 ? .2f : .08f), Vector3.one * .085f, rng, .1f);
                    var om = new MeshKit(); om.Blob(GlowM(new Color(1f, .5f, .1f), .3f), new Vector3(0, .08f, 0), Vector3.one * .085f, rng, .1f); Thing(om, "Orange", "an orange", top + f.Fwd * .38f + f.Right * .4f, Quaternion.identity, new Vector3(.17f, .17f, .17f), .2f, false); break;
                case 2:   // drums: tap one
                    for (var k = 0; k < 4; k++) Drum(mk, top + f.Right * (-.9f + k * .6f), .7f + (k % 2) * .25f); var dn = i; string[] tap = { "Ka-ka-KA. The seller taps the answer on the table without looking up.", "It talks when you squeeze it. Yours says something rude.", "A good one. You can tell: the goat across the road looked round." };
                    Spot("Stall drum", top + up * .5f, "Tap a drum", new Vector3(2.4f, .9f, 1f)).onUse = _ => { life.played = Time.time; Say(tlife.transform, top + up * 2.2f, tap[dn++ % tap.Length], 4.5f); }; break;
                case 3:   // suya: a grill glowing, skewers, smoke
                    mk.Box(iron, top + up * .1f, new Vector3(1.4f, .2f, .6f), f.r); mk.Box(emberM, top + up * .2f, new Vector3(1.3f, .02f, .5f), f.r); for (var k = 0; k < 6; k++) { mk.Box(woodM, top + up * .24f + f.Right * (-.5f + k * .2f), new Vector3(.02f, .02f, .7f), f.r); mk.Box(WorldMaterials.Solid(new Color(.5f, .24f, .14f)), top + up * .25f + f.Right * (-.5f + k * .2f), new Vector3(.07f, .05f, .4f), f.r); }
                    halos.Add(top + up * .4f, 1.2f, Orange, .5f); life.smoke.Add(top + up * .5f); var sm = new MeshKit(); sm.Box(woodM, new Vector3(0, .02f, 0), new Vector3(.02f, .02f, .5f), Quaternion.identity); sm.Box(WorldMaterials.Solid(new Color(.5f, .24f, .14f)), new Vector3(0, .03f, 0), new Vector3(.07f, .05f, .3f), Quaternion.identity); Thing(sm, "Suya", "a stick of suya", top + f.Right * 1f + f.Fwd * .2f, f.r, new Vector3(.1f, .08f, .5f), .1f, false); break;
                case 4:   // drinks: crates of bottles; one to carry
                    foreach (var x in new[] { -.8f, .1f }) { mk.Box(Paint(Cloth[0]), top + f.Right * x + up * .14f, new Vector3(.7f, .28f, .5f), f.r); for (var k = 0; k < 6; k++) mk.Prism(GlowM(k % 2 == 0 ? new Color(.5f, .25f, .1f) : new Color(.2f, .6f, .3f), .3f), top + f.Right * (x - .22f + (k % 3) * .22f) + f.Fwd * ((k / 3) * .2f - .1f) + up * .2f, up, .04f, .26f, 6); }
                    var bt = new MeshKit(); bt.Prism(GlowM(new Color(.5f, .25f, .1f), .3f), Vector3.zero, Vector3.up, .04f, .18f, 6); bt.Prism(GlowM(new Color(.5f, .25f, .1f), .3f), new Vector3(0, .18f, 0), Vector3.up, .018f, .08f, 5); Thing(bt, "Malt", "a cold malt", top + f.Right * .95f + f.Fwd * .2f, Quaternion.identity, new Vector3(.1f, .28f, .1f), .2f, false); break;
                default:  // records and cassettes, a radio playing
                    for (var k = 0; k < 9; k++) mk.Box(Paint(Cloth[(i + k) % Cloth.Length]), top + f.Right * (-1f + k * .2f) + up * .17f, new Vector3(.02f, .32f, .32f), f.r * Quaternion.Euler(0, 0, 8f)); mk.Box(dark, top + f.Right * .9f + up * .14f, new Vector3(.5f, .28f, .2f), f.r); foreach (var x in new[] { -.14f, .14f }) mk.Prism(tin, top + f.Right * (.9f + x) + up * .14f + f.Fwd * .1f, f.Fwd, .08f, .02f, 8);
                    var rm = new MeshKit(); rm.Box(Paint(Cloth[(i + 2) % 6]), new Vector3(0, .006f, 0), new Vector3(.31f, .012f, .31f), Quaternion.identity); rm.Prism(dark, new Vector3(0, .013f, 0), Vector3.up, .12f, .004f, 12); Thing(rm, "Record", "a record with no label", top + f.Fwd * .3f, f.r, new Vector3(.31f, .03f, .31f), .12f); break;
            }
            if (i % 3 == 0) { string[][] calls = { new[] { "Fine cloth! Feel it. No, feel it. Now you can't leave it.", "This pattern means 'my enemies are watching'. This one means 'let them watch'." }, new[] { "Sweet oranges! Sweeter than whoever you're thinking about.", "Take one for the road. Take one for the road back." }, new[] { "This drum talks. That one argues.", "Squeeze here, the note goes up. Like so. Now you're talking." }, new[] { "Suya! Pepper is free, regret is extra.", "It's ready when the smoke says so." }, new[] { "Cold one? They're all cold. The cold is the product.", "Sit, drink, watch the sun finish." }, new[] { "Originals only! This one was pressed in '78. Maybe '79. The sleeve is from something else.", "You want the song they're playing at the Shrine? Everybody wants that. It's forty minutes. Two records." } };
                Folk(f.P(.3f, 0, -.5f), f.Fwd, Resident.Pose.Stand, calls[kind], Local(Residents.Tool.None, true)); }
            Mark(f.o, 2.4f);
        }

        // ---------------------------------------------------------------- the houses round the roundabout
        void Houses(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var built = 0;
            for (var k = 0; k < 10; k++)
            {
                var d = Quaternion.Euler(0, k * 36f + 18f, 0) * U; if (Vector3.Dot(d, -R) > .86f) continue;
                var p = center + d * 29.5f; if (!ctx.IsLand(p.x, p.z, 8f) || InFootprint(ctx, p.x, p.z, 6.4f) || LaneNear(p.x, p.z, 7.4f)) continue;
                House(ctx, mk, solid, new PlaceFrame(ctx.OnGround(p.x, p.z), -d), k); built++;
            }
            Debug.Log($"The Afrobeat Coast: {built} houses");
        }

        /// A painted block house: a tin roof, shuttered windows lit, a bright door, an advert painted on the wall,
        /// sometimes a balcony or a black water tank. It stands on a plinth, so it sits on a slope.
        void House(LandContext ctx, MeshKit mk, MeshKit solid, PlaceFrame f, int i)
        {
            var up = Vector3.up; const float W = 9f, D = 7f; var st = 1 + (i * 3 + 1) % 2; var H = st * 3f + .4f; var wall = Paint(Paints[i % Paints.Length]); var trim = Paint(Paints[(i + 3) % Paints.Length]); var fz = D / 2f; var rng = new System.Random(i * 13 + 2);
            mk.Box(wall, f.P(0, (H - 3f) / 2f, 0), new Vector3(W, H + 3f, D), f.r); solid.Box(wall, f.P(0, (H - 3f) / 2f, 0), new Vector3(W, H + 3f, D), f.r); mk.Box(trim, f.P(0, .3f, fz + .02f), new Vector3(W, .7f, .04f), f.r);
            mk.Box(i % 3 == 0 ? rustM : tin, f.P(0, H + .5f, .3f), new Vector3(W + .8f, .08f, D + 1.4f), f.r * Quaternion.Euler(7f, 0, 0));
            var dx = (i % 2 == 0 ? -1f : 1f) * 2.6f; mk.Box(trim, f.P(dx, 1.1f, fz + .03f), new Vector3(1.05f, 2.2f, .06f), f.r); mk.Box(concrete, f.P(dx, .06f, fz + .5f), new Vector3(1.8f, .2f, 1f), f.r); mk.Blob(bulbM[i % 3], f.P(dx, 2.5f, fz + .2f), Vector3.one * .1f, rng, 0f); halos.Add(f.P(dx, 2.5f, fz + .5f), 1.2f, Gold, .4f);
            for (var s = 0; s < st; s++) for (var k = 0; k < 2; k++)
            {
                var wx = -dx * .2f + (k == 0 ? -1f : 1f) * 1.6f - dx * .4f; if (s == 0 && Mathf.Abs(wx - dx) < 1.4f) continue; var wp = f.P(wx, s * 3f + 1.7f, fz + .02f);
                mk.Box(windowM, wp, new Vector3(1.1f, 1.2f, .05f), f.r); foreach (var e in new[] { -1f, 1f }) mk.Box(Paint(new Color(.12f, .36f, .3f)), wp + f.Right * e * .82f + f.Fwd * .03f, new Vector3(.5f, 1.2f, .05f), f.r * Quaternion.Euler(0, e * 20f, 0));
            }
            if (st == 2) { mk.Box(concrete, f.P(0, 3.3f, fz + .6f), new Vector3(W - 1f, .14f, 1.2f), f.r); mk.Box(trim, f.P(0, 4.3f, fz + 1.16f), new Vector3(W - 1f, .06f, .06f), f.r); for (var k = 0; k <= 8; k++) mk.Prism(iron, f.P(-(W - 1f) / 2f + k * (W - 1f) / 8f, 3.36f, fz + 1.16f), up, .02f, .94f, 4); mk.Box(Paint(Cloth[(i + 1) % 6]), f.P(1.5f, 3.9f, fz + 1.2f), new Vector3(1.4f, .9f, .03f), f.r); }
            if (i % 3 == 1) { mk.Prism(dark, f.P(-W / 2f + 1.6f, H + .7f, -1.2f), up, .9f, 1.5f, 12); mk.Prism(dark, f.P(-W / 2f + 1.6f, H + 2.2f, -1.2f), up, .5f, .14f, 10); }
            string[] ads = { "COLD MINERALS\nSOLD HERE", "TAILOR\n& FASHION", "BARBING\nSALON", "PHONE\nCHARGING", "MAMA'S\nKITCHEN", "RECORDS\n& TAPES" }; mk.Box(Paint(Cream), f.P(-dx * .9f, st * 3f - .9f + (st == 1 ? 1.6f : 0f) - (st == 1 ? 1.7f : 0f), fz + .03f) + up * (st == 1 ? .9f : 0f), new Vector3(2.4f, 1f, .04f), f.r);
            ArtistBits.Label(rootT, ads[i % ads.Length], f.P(-dx * .9f, st * 3f - .9f + (st == 1 ? .8f : 0f), fz + .06f), f.Fwd, .2f, Ink, TextAnchor.MiddleCenter);
            if (i % 4 == 2) Folk(f.P(dx + 1.4f, .02f, fz + .8f), f.Fwd, Resident.Pose.Lean, new[] { "I'm not going down to the beach. I'm just standing where I can hear it.", "This house has been six colours. I liked the fourth one.", "My neighbour says I play my radio too loud. He says it by turning his up." });
            Mark(f.o, 6.4f);
        }

        // ---------------------------------------------------------------- the beach
        void Beach(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var c = ctx.OnGround(beach2.x, beach2.y); var rng = new System.Random(88);
            mk.Prism(sand, c - up * .26f, up, 21f, .34f, 28); for (var d = roadL; d <= roadL + 4f; d += 2f) Remember(G(ctx, 0, -d), 1); for (var k = 0; k < 8; k++) Remember(c + Quaternion.Euler(0, k * 45f, 0) * U * 12f, 1);
            // three bonfires with logs round them
            var fires = new[] { c + U * 9f - R * 3f, c - U * 10f - R * 1f, c - R * 13f + U * 1f }; var fi = 0;
            foreach (var fp in fires)
            {
                var g = fp + up * .08f; Bonfire(ctx, mk, g, fi == 2 ? 1.4f : 1.1f, rng);
                for (var k = 0; k < 4; k++) { var d = Quaternion.Euler(0, k * 90f + 30f + fi * 20f, 0) * U; var lg = g + d * 3f; var along = Vector3.Cross(up, d); mk.Prism(palmTrunk, lg + up * .22f - along * .9f, along, .22f, 1.8f, 7); Col(lg + up * .22f, new Vector3(.44f, .44f, 1.8f), Quaternion.LookRotation(along)); Interact.Bench(tlife.transform, lg + up * .46f - along * .5f, lg + up * .46f + along * .5f, -d); }
                if (fi == 0) Folk(g + Quaternion.Euler(0, 120f, 0) * U * 3f + up * .46f, FaceTo(g + Quaternion.Euler(0, 120f, 0) * U * 3f, g), Resident.Pose.Sit, new[] { "Sit. The fire doesn't mind who you came with.", "We watch it go into the water every night. Every night somebody says 'ah'. Tonight it can be you.", "My grandmother said the sun goes down to dance where we can't see. I believe her." }, null, null, .46f);
                if (fi == 2) Folk(g + Quaternion.Euler(0, 30f + 40f, 0) * U * 4.6f, FaceTo(g + Quaternion.Euler(0, 70f, 0) * U * 4.6f, g), Resident.Pose.Wave, new[] { "Come, come! There's room, there's always room!", "You dance like somebody who's thinking. Stop thinking!" });
                fi++;
            }
            // the drum circle: join in
            var dc = c + U * 1f + R * 6f + up * .08f; for (var k = 0; k < 5; k++) { var d = Quaternion.Euler(0, k * 72f, 0) * U; Drum(mk, dc + d * 1.5f, 1.2f + (k % 2) * .3f); }
            Folk(dc + Quaternion.Euler(0, 36f, 0) * U * 2.3f, FaceTo(dc + Quaternion.Euler(0, 36f, 0) * U * 2.3f, dc), Resident.Pose.Work, new[] { "Find the bell pattern. Kon-kon, ko-lo. Everything else hangs on that.", "Don't play loud. Play in the hole the others leave." }).workRate = 1.8f;
            Folk(dc + Quaternion.Euler(0, 180f, 0) * U * 2.3f, FaceTo(dc + Quaternion.Euler(0, 180f, 0) * U * 2.3f, dc), Resident.Pose.Work, new[] { "(She doesn't stop. She tips her head at the free drum.)", "Six against four. Your hands know. Let them." }).workRate = 2.2f;
            string[] join = { "You find a gap in the pattern and sit in it. The circle closes round you like you were always there.", "Ko-lo, kon-kon. Your hands are doing something your head can't count.", "The lead drum says something. Everybody answers, and one of the answers is you.", "The fires seem to lean in on the downbeat." }; var jn = 0;
            Spot("The drum circle", dc + up * .9f, "Join the drum circle", new Vector3(3.4f, 1.6f, 3.4f)).onUse = _ => { life.played = Time.time; Say(tlife.transform, dc + up * 2.6f, join[jn++ % join.Length], 5f); };
            // painted canoes pulled up on the sand
            for (var k = 0; k < 3; k++) { var cp = c - R * (17f + k * 1.2f) + U * (-9f + k * 8f); cp.y = ctx.Ground(cp.x, cp.z) + .1f; var cr = Quaternion.LookRotation((R + U * (k - 1) * .3f).normalized); mk.Box(Paint(Paints[(k * 2 + 1) % Paints.Length]), cp + up * .3f, new Vector3(1f, .5f, 5.4f), cr); mk.Box(woodM, cp + up * .5f, new Vector3(.8f, .12f, 5f), cr); mk.Box(Paint(Paints[(k * 2 + 4) % Paints.Length]), cp + up * .44f, new Vector3(1.04f, .14f, 5.44f), cr); foreach (var e in new[] { -1f, 1f }) mk.Prism(Paint(Paints[(k * 2 + 1) % Paints.Length]), cp + up * .3f + cr * Vector3.forward * e * 2.7f, cr * (Vector3.forward * e + Vector3.up * .5f).normalized, .34f, 1f, 4, .2f); Col(cp + up * .35f, new Vector3(1f, .7f, 5.6f), cr); Interact.Seat(tlife.transform, cp + up * .56f, cr * Vector3.right, "Sit in the canoe").seatHeight = .5f; }
            // a football pitch scratched in the sand: two goals, a ball to kick about
            var pc = c + U * 14f + R * 10f; pc.y = ctx.Ground(pc.x, pc.z) + .1f; foreach (var e in new[] { -1f, 1f }) { var gp = pc + R * e * 7f; gp.y = ctx.Ground(gp.x, gp.z) + .05f; foreach (var s in new[] { -1.3f, 1.3f }) { mk.Prism(white, gp + U * s - up * .2f, up, .05f, 1.9f, 5); ArtistBits.PostCollider(ctx.Root, gp + U * s, 1.7f, .07f); } mk.Box(white, gp + up * 1.66f, new Vector3(.08f, .08f, 2.7f), Quaternion.identity); }
            var ball = new MeshKit(); ball.Blob(white, new Vector3(0, .12f, 0), Vector3.one * .12f, rng, 0f); for (var k = 0; k < 5; k++) ball.Blob(dark, new Vector3(0, .12f, 0) + Quaternion.Euler(k * 72f, k * 50f, 0) * Vector3.up * .1f, Vector3.one * .04f, rng, 0f); Thing(ball, "Football", "a football", pc + up * .05f, Quaternion.identity, new Vector3(.25f, .25f, .25f), .4f, false);
            Folk(pc + R * 2f + U * 1.5f, -R, Resident.Pose.Wave, new[] { "Pass! Pass! ...Okay, shoot then!", "First to ten. We lost count at about forty.", "Barefoot only. House rule. Beach rule." });
            Mark(c, 22f);
            Shot(ctx, "the beach at sunset", c + R * 16f + U * 4f + up * 2.4f, c - R * 30f + up * 4f);
            Shot(ctx, "by the bonfires", fires[0] + R * 6f + U * 3f + up * 1.7f, fires[2] + up * 1.4f);
        }
    }

    /// The town's moving parts: the bus that goes up and down Market Road (it stops for anyone in its way, and
    /// hurries for "Rush"), and the Big Drum, which shivers when it is hit.
    public class AfroTownLife : MonoBehaviour
    {
        public AfroLife life; public Transform bus, drum; public Vector3[] way; public float hit = -99f; float along; int dir = 1; Vector3 drumAt; bool got;

        void Update()
        {
            var t = Time.time; var dt = Time.deltaTime; var cam = Camera.main;
            if (drum != null) { if (!got) { drumAt = drum.position; got = true; } var since = t - hit; drum.position = drumAt + Vector3.up * (since < .6f ? Mathf.Sin(since * 40f) * .05f * (1f - since / .6f) : 0f); }
            if (bus == null || way == null || way.Length < 2) return;
            var speed = 4.5f + (life != null ? life.rush * 7f : 0f); var total = (way.Length - 1) * 4f;
            var i = Mathf.Clamp(Mathf.FloorToInt(along / 4f), 0, way.Length - 2); var p = Vector3.Lerp(way[i], way[i + 1], along / 4f - i); var fwd = (way[i + 1] - way[i]).normalized * dir;
            var blocked = false; if (cam != null) { var rel = cam.transform.position - p; rel.y = 0; blocked = rel.magnitude < 9f && Vector3.Dot(rel.normalized, new Vector3(fwd.x, 0, fwd.z).normalized) > .2f; }
            if (!blocked) along += dir * speed * dt; if (along >= total) { along = total; dir = -1; } else if (along <= 0f) { along = 0f; dir = 1; }
            bus.SetPositionAndRotation(p + Vector3.up * Mathf.Abs(Mathf.Sin(t * 9f)) * .015f, Quaternion.Slerp(bus.rotation, Quaternion.LookRotation(fwd), dt * 3f));
        }
    }
}
