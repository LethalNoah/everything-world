using System.Collections.Generic;
using UnityEngine;
using static EverythingWorld.World.PlaceTools;

namespace EverythingWorld.World
{
    /// The Roots Bay's places (part of RootsKit): the Yard (the great tree with a bench round it, a standpipe, a
    /// dominoes table, a fruit stall with mangoes and coconuts to carry, washing on a line); Bay Road (red earth and
    /// puddles, flags, painted board houses with people on the verandahs); and the beach (painted canoes, nets
    /// drying, fishing huts on stilts over the water with a plank walk out to one, a football).
    public partial class RootsKit
    {
        RootsTownLife tlife; System.Random prng; Transform rootT; LandContext ctx0;

        static readonly Color[] Tops = { new Color(.82f, .18f, .14f), new Color(1f, .8f, .2f), new Color(.14f, .6f, .28f), new Color(.96f, .93f, .84f), new Color(.12f, .5f, .62f), new Color(.95f, .5f, .2f), new Color(.2f, .2f, .22f) };
        static readonly Color[] Bottoms = { new Color(.2f, .26f, .4f), new Color(.5f, .42f, .3f), new Color(.9f, .86f, .76f), new Color(.16f, .16f, .18f) };
        static readonly string[] TownTalk =
        {
            "Morning! Rain's gone through. Everything smells new.", "Walk good. Mind the puddles, or don't, they're warm.", "Sound System's on the beach. You'll feel it in your chest before you hear it.",
            "Every little thing going to be all right. That's not me talking, that's the forecast.", "Sun come, rain come, sun come again. That's the whole calendar here.",
        };

        void TownStart(LandContext ctx)
        {
            rootT = ctx.Root; ctx0 = ctx; prng = new System.Random(1975);
            tlife = new GameObject("Roots Bay town life").AddComponent<RootsTownLife>(); tlife.transform.SetParent(life.transform, false);
        }

        Residents.Look Local(Residents.Tool tool = Residents.Tool.None, bool apron = false) { var l = Residents.Random(prng, Tops, Bottoms, new[] { Residents.Hat.None, Residents.Hat.Beanie, Residents.Hat.Straw, Residents.Hat.Cap, Residents.Hat.None }, new[] { Red, Gold, Green, new Color(.9f, .82f, .6f) }); l.tool = tool; l.apron = apron; return l; }
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

        /// A wooden stool to sit on.
        void Stool(MeshKit mk, Vector3 g, Vector3 face, int i)
        {
            mk.Prism(Paint(Paints[i % Paints.Length]), g + Vector3.up * .4f, Vector3.up, .2f, .06f, 8); for (var k = 0; k < 3; k++) mk.Prism(woodM, g + Quaternion.Euler(0, k * 120f, 0) * Vector3.forward * .14f, Vector3.up, .025f, .42f, 4); Interact.Seat(tlife.transform, g + Vector3.up * .48f, face).seatHeight = .44f;
        }

        // ---------------------------------------------------------------- the Yard
        void Yard(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var c = ctx.OnGround(center.x, center.z); var rng = new System.Random(3);
            mk.Prism(earthM, c - up * .28f, up, 20f, .4f, 28);
            // the great tree, to one side, a bench all the way round its trunk
            var tr = c - D * 9f + S * 7f + up * .12f; ShadeTree(ctx, mk, tr, 2.2f, rng); mk.Prism(Paint(Paints[2]), tr + up * .42f, up, 1.7f, .08f, 14); mk.Prism(woodM, tr, up, 1.5f, .42f, 14); ArtistBits.PostCollider(ctx.Root, tr, .5f, 1.7f);
            for (var k = 0; k < 4; k++) { var d = Quaternion.Euler(0, k * 90f + 30f, 0) * D; Interact.Seat(tlife.transform, tr + d * 1.4f + up * .5f, d, "Sit under the tree").seatHeight = .46f; }
            Folk(tr + Quaternion.Euler(0, 30f, 0) * D * 1.4f + up * .46f, Quaternion.Euler(0, 30f, 0) * D, Resident.Pose.Sit, new[] { "This tree was here before the road. Before the bay, I sometimes think.", "Sit. We used to sit right here, all of us, and somebody would make a fire. Good friends we've had. Good friends we've lost.", "You hear the bass from the beach? That's the island's heartbeat. Doctor says it's healthy." }, null, null, .46f);
            // a standpipe and a bucket; washing on a line
            var sp = c + D * 4f + S * 13f + up * .12f; mk.Prism(zinc, sp, up, .05f, 1f, 6); mk.Prism(zinc, sp + up * 1f, D, .04f, .3f, 5); mk.Prism(concreteOf(), sp - up * .05f + D * .3f, up, .6f, .12f, 10); mk.Prism(puddleM, sp + D * .3f + up * .08f, up, .5f, .01f, 10); ArtistBits.PostCollider(ctx.Root, sp, 1f, .1f);
            var bk = new MeshKit(); bk.Prism(zinc, Vector3.zero, Vector3.up, .16f, .28f, 8, 1.2f); bk.Prism(puddleM, new Vector3(0, .24f, 0), Vector3.up, .17f, .02f, 8); Thing(bk, "Bucket", "a bucket of water", sp + D * .9f + up * .02f, Quaternion.identity, new Vector3(.36f, .32f, .36f), .8f);
            var la = c - D * 14f - S * 11f + up * .12f; var lb = la + S * 7f + D * 1f; foreach (var g in new[] { la, lb }) { mk.Prism(woodM, g - up * .2f, up, .05f, 2.4f, 4); ArtistBits.PostCollider(ctx.Root, g, 2.2f, .07f); } mk.Prism(dark, la + up * 2.1f, (lb - la).normalized, .01f, (lb - la).magnitude, 3); for (var k = 0; k < 6; k++) mk.Box(Paint(Paints[(k * 3) % Paints.Length]), Vector3.Lerp(la, lb, .12f + k * .15f) + up * 1.7f, new Vector3(.06f, .8f - (k % 2) * .2f, .7f), Quaternion.LookRotation((lb - la).normalized));
            // the dominoes table: play
            var dt = c + D * 9f - S * 9f + up * .12f; mk.Box(plank, dt + up * .74f, new Vector3(1.1f, .06f, 1.1f), Quaternion.identity); foreach (var (x, zz) in new[] { (-.45f, -.45f), (.45f, -.45f), (.45f, .45f), (-.45f, .45f) }) mk.Prism(woodM, dt + new Vector3(x, 0, zz), up, .03f, .74f, 4); Col(dt + up * .4f, new Vector3(1.1f, .8f, 1.1f), Quaternion.identity); for (var k = 0; k < 8; k++) mk.Box(white, dt + up * .79f + new Vector3(-.35f + k * .1f, 0, Mathf.Sin(k * .9f) * .14f), new Vector3(.08f, .03f, .16f), Quaternion.Euler(0, k % 3 == 0 ? 90f : 0f, 0));
            for (var k = 0; k < 3; k++) { var d = Quaternion.Euler(0, k * 90f, 0) * D; Stool(mk, dt + d * .95f, -d, k); }
            Folk(dt + Quaternion.Euler(0, 270f, 0) * D * .95f, -(Quaternion.Euler(0, 270f, 0) * D), Resident.Pose.Stand, new[] { "(SLAM.) Six-love! Who's next? Sit down, sit down.", "You don't place the domino. You announce it.", "My partner signals with his eyebrow. Don't tell the other table." });
            string[] slam = { "You slam it down. The table jumps. That's the correct way; quiet dominoes is an insult.", "Double six. You stand up to play it. Everyone groans.", "You pass. An old man looks at you like you've passed on life itself." }; var sn = 0;
            Spot("Dominoes", dt + up * 1.1f, "Slam a domino", new Vector3(1.1f, .6f, 1.1f)).onUse = _ => { life.played = Time.time; Say(tlife.transform, dt + up * 2.2f, slam[sn++ % slam.Length], 4.5f); };
            // the fruit stall: mangoes and coconuts to carry
            var fs = new PlaceFrame(c + D * 14f + S * 6f + up * .12f, (-D - S * .3f).normalized); foreach (var (x, zz) in new[] { (-1.3f, -.7f), (1.3f, -.7f), (1.3f, .7f), (-1.3f, .7f) }) mk.Prism(woodM, fs.P(x, -.2f, zz), up, .05f, 2.5f, 4); for (var k = 0; k < 6; k++) mk.Box(Paint(new[] { Red, Gold, Green }[k % 3]), fs.P(-1.25f + k * .5f, 2.3f + (k % 2) * .01f, 0), new Vector3(.48f, .04f, 2f), fs.r * Quaternion.Euler(8f, 0, 0)); mk.Box(plank, fs.P(0, .8f, .2f), new Vector3(2.5f, .08f, .9f), fs.r); Col(fs.P(0, .45f, .2f), new Vector3(2.5f, .9f, .9f), fs.r); Sign(fs.P(0, 1.9f, .76f), fs.Fwd, "FRESH  ·  SWEET  ·  TODAY", .13f, Ink); mk.Box(Paint(Cream), fs.P(0, 1.9f, .72f), new Vector3(2.4f, .3f, .04f), fs.r);
            for (var k = 0; k < 3; k++) { var mm = new MeshKit(); mm.Blob(Paint(k == 1 ? new Color(.95f, .7f, .2f) : new Color(.9f, .45f, .2f)), new Vector3(0, .07f, 0), new Vector3(.08f, .07f, .1f), rng, .1f); Thing(mm, "Mango", "a ripe mango", fs.P(-.9f + k * .3f, .86f, .3f), Quaternion.identity, new Vector3(.16f, .15f, .2f), .2f, false); }
            for (var k = 0; k < 2; k++) { var cm = new MeshKit(); cm.Blob(Paint(new Color(.36f, .58f, .26f)), new Vector3(0, .14f, 0), Vector3.one * .14f, rng, .06f); cm.Prism(Paint(Red), new Vector3(.03f, .26f, 0), new Vector3(.3f, 1f, 0).normalized, .01f, .16f, 4); Thing(cm, "Coconut", "a jelly coconut with a straw", fs.P(.4f + k * .4f, .86f, .26f), Quaternion.identity, new Vector3(.28f, .3f, .28f), .4f, false); }
            for (var k = 0; k < 9; k++) mk.Blob(Paint(k % 3 == 0 ? new Color(.95f, .8f, .2f) : k % 3 == 1 ? new Color(.4f, .7f, .25f) : new Color(.9f, .3f, .2f)), fs.P(-1f + (k % 5) * .5f, .92f, -.1f - (k / 5) * .2f), Vector3.one * .08f, rng, .2f);
            Folk(fs.P(.2f, 0, -.6f), fs.Fwd, Resident.Pose.Stand, new[] { "Mango! Julie mango, the rain made them sweet. Take one.", "Jelly coconut. Cold. The straw is extra. I'm joking, take the straw.", "No rush. The fruit isn't going anywhere and neither are you." }, Local(Residents.Tool.None, true)); Mark(fs.o, 2.6f);
            // flags, the name on a painted board, a welcome
            var sg = c - D * 4f - S * 15f + up * .12f; var sf = (S + D * .3f).normalized; foreach (var e in new[] { -2.4f, 2.4f }) { mk.Prism(woodM, sg + Vector3.Cross(up, sf) * e - up * .2f, up, .09f, 3.6f, 5); ArtistBits.PostCollider(ctx.Root, sg + Vector3.Cross(up, sf) * e, 3.4f, .1f); }
            for (var k = 0; k < 3; k++) mk.Box(Paint(new[] { Red, Gold, Green }[k]), sg + up * (3.3f - k * .5f), new Vector3(5.4f, .5f, .1f), Quaternion.LookRotation(sf)); Sign(sg + up * 2.8f + sf * .07f, sf, "THE ROOTS BAY", .4f, Ink); Sign(sg + up * 2.3f + sf * .07f, sf, "one love", .22f, Cream);
            for (var k = 0; k < 4; k++) { var d = Quaternion.Euler(0, k * 90f + 45f, 0) * D; FlagPole(ctx, mk, c + d * 18f + up * .12f, Quaternion.Euler(0, 60f, 0) * d); }
            for (var k = 0; k < 3; k++) mk.Prism(puddleM, c + Quaternion.Euler(0, k * 130f + 70f, 0) * D * (8f + k * 3f) + up * .13f, up, 1.2f + k * .3f, .012f, 9);
            Folk(c + D * 6f + S * 2f + up * .12f, (-D - S * .2f).normalized, Resident.Pose.Wave, new[] { "Morning, morning! You just missed the rain. Look: it left you a rainbow.", "Straight down Bay Road to the beach. The Sound System's already warming up.", "Take a mango. Sit under the tree. Nobody here has an appointment." });
            Mark(c, 21f);
            Shot(ctx, "the yard", c + D * 15f - S * 12f + up * 2.6f, tr + up * 3f);
        }

        Material concreteOf() => WorldMaterials.Solid(new Color(.66f, .64f, .6f));

        // ---------------------------------------------------------------- Bay Road
        void BayRoad(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var pts = new List<Vector3>(); for (var d = 19f; d <= roadL + .01f; d += 2f) pts.Add(G(ctx, d, Mathf.Sin(d / 40f) * 2f)); if (pts.Count < 2) return;
            Strip(mk, pts, earthM, RoadW, .13f, ctx); foreach (var p in pts) Remember(p, 1);
            var rng = new System.Random(6); for (var i = 5; i < pts.Count; i += 6) mk.Prism(puddleM, pts[i] + S * ((float)rng.NextDouble() * 3.6f - 1.8f) + up * .145f, up, .6f + (float)rng.NextDouble() * .9f, .01f, 9);
            var route = new List<Vector3>(); for (var d = 24f; d <= roadL - 6f; d += 15f) route.Add(G(ctx, d, 1.8f + Mathf.Sin(d / 40f) * 2f) + up * .14f); for (var d = roadL - 6f; d >= 24f; d -= 15f) route.Add(G(ctx, d, -1.8f + Mathf.Sin(d / 40f) * 2f) + up * .14f);
            if (route.Count > 3) Folk(route[0], D, Resident.Pose.Walk, new[] { "Going down for the first tune of the day. Then up for breakfast. Then down again.", "Left, right, left, right. If you walk it in time you never get tired." }, null, route.ToArray()).speed = 1.1f;
            Shot(ctx, "down bay road to the rainbow", G(ctx, 21f, 0) + up * 2.4f, G(ctx, Mathf.Min(roadL, 140f), 0) + up * 14f);
        }

        /// The village, built after the lanes so nothing stands in one: painted board houses down both sides of Bay Road.
        void Village(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var i = 0; var built = 0; var rng = new System.Random(12);
            foreach (var e in new[] { 1f, -1f })
            for (var d = 30f + (e > 0 ? 0f : 5f); d <= roadL - 30f; d += 11f, i++)
            {
                var p = Wp(d, e * (RoadW / 2f + 6.4f) + Mathf.Sin(d / 40f) * 2f); if (!ctx.IsLand(p.x, p.z, 6f) || InFootprint(ctx, p.x, p.z, 5.4f) || LaneNear(p.x, p.z, 5.6f)) continue;
                var f = new PlaceFrame(ctx.OnGround(p.x, p.z), -S * e); var po = Shack(ctx, mk, solid, f, 5.6f, 4.2f, i);
                if (i % 3 == 0) { Stool(mk, po.P(1.6f, 0, 0), po.Fwd, i); Folk(po.P(-1.4f, 0, .1f), po.Fwd, Resident.Pose.Lean, new[] { "Good morning! Good morning. Good morning to you too.", "I painted it red last year. Gold the year before. Green's next. You have to be fair to all three.", "Rain washed the road for us. Saved me a job." }); }
                else if (i % 3 == 1) Guitar(mk, po.P(1.8f, 0, -.5f), po.r, Paint(new Color(.6f, .42f, .24f)), rng);
                else for (var k = 0; k < 3; k++) mk.Prism(Paint(new Color(.72f, .42f, .3f)), po.P(-1.8f + k * .5f, 0, .5f), Vector3.up, .16f, .3f, 8, 1.3f);
                Mark(f.o, 5f); built++;
            }
            Debug.Log($"The Roots Bay: {built} painted houses on Bay Road");
        }

        // ---------------------------------------------------------------- the beach
        void Beach(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var c = ctx.OnGround(beach2.x, beach2.y); var rng = new System.Random(77); var along = Quaternion.LookRotation(D);
            mk.Prism(sandM, c - up * .26f, up, 21f, .34f, 28); for (var k = 0; k < 8; k++) Remember(c + Quaternion.Euler(0, k * 45f, 0) * D * 11f, 1); Remember(c, 1);
            // painted canoes pulled up, a net drying on poles
            for (var k = 0; k < 3; k++) { var cp = c + D * (9f + k * 1.4f) + S * (4f + k * 5f); cp.y = ctx.Ground(cp.x, cp.z) + .1f; var cr = Quaternion.LookRotation((D + S * (k - 1) * .3f).normalized); mk.Box(Paint(Paints[k]), cp + up * .3f, new Vector3(1f, .5f, 5.2f), cr); mk.Box(woodM, cp + up * .5f, new Vector3(.8f, .12f, 4.8f), cr); mk.Box(Paint(Paints[(k + 1) % 3]), cp + up * .44f, new Vector3(1.04f, .14f, 5.24f), cr); foreach (var e in new[] { -1f, 1f }) mk.Prism(Paint(Paints[k]), cp + up * .3f + cr * Vector3.forward * e * 2.6f, cr * (Vector3.forward * e + Vector3.up * .5f).normalized, .34f, 1f, 4, .2f); Col(cp + up * .35f, new Vector3(1f, .7f, 5.4f), cr); Interact.Seat(tlife.transform, cp + up * .56f, cr * Vector3.right, "Sit in the canoe").seatHeight = .5f; }
            var na = c - S * 12f + D * 6f; var nb = na - S * 4f + D * 1f; na.y = ctx.Ground(na.x, na.z); nb.y = ctx.Ground(nb.x, nb.z); foreach (var g in new[] { na, nb }) { mk.Prism(woodM, g - up * .2f, up, .05f, 2.2f, 4); ArtistBits.PostCollider(ctx.Root, g, 2f, .07f); } for (var k = 0; k < 7; k++) mk.Prism(Paint(new Color(.36f, .5f, .5f)), Vector3.Lerp(na, nb, k / 6f) + up * 2f, -up, .01f, 1.5f, 3); for (var k = 0; k < 4; k++) mk.Prism(Paint(new Color(.36f, .5f, .5f)), na + up * (.7f + k * .4f), (nb - na).normalized, .01f, (nb - na).magnitude, 3);
            // fishing huts on stilts over the water; a plank walk out to the nearest
            var deckY = waterY + 1.7f; var h0 = c + D * 24f - S * 14f; for (var k = 0; k < 3; k++)
            {
                var hp = h0 + D * (k * 7f) - S * (k * 9f) + (k == 2 ? S * 40f : Vector3.zero); hp.y = deckY; if (ctx.Data.IslandAt(hp.x, hp.z) != null && ctx.Ground(hp.x, hp.z) > waterY + .4f) continue;
                foreach (var (x, zz) in new[] { (-1.8f, -1.6f), (1.8f, -1.6f), (1.8f, 1.6f), (-1.8f, 1.6f) }) mk.Prism(trunkM, new Vector3(hp.x, waterY - 2f, hp.z) + along * new Vector3(x, 0, zz), up, .12f, 4f, 5);
                mk.Box(plank, hp, new Vector3(5f, .16f, 4.4f), along); mk.Box(Paint(Paints[(k + 3) % Paints.Length]), hp + up * 1.3f - D * .3f, new Vector3(3.6f, 2.4f, 3f), along); mk.GableRoof(k == 1 ? rustM : zinc, hp + up * 2.5f - D * .3f, new Vector3(3.8f, 0, 4.4f), 1.1f, Quaternion.LookRotation(S)); mk.Box(dark, hp + up * 1.1f + D * 1.21f, new Vector3(.9f, 1.9f, .04f), along);
                if (k == 0) { solid.Box(plank, hp - up * .3f, new Vector3(5f, .7f, 4.4f), along); solid.Box(plank, hp + up * 1.3f - D * .3f, new Vector3(3.6f, 2.4f, 3f), along); var from = c + D * 16f - S * 8f; from.y = deckY; var mid = (from + hp + D * 2.2f + S * 0f) / 2f; var dir = hp + D * 2.2f - from; dir.y = 0; var len = dir.magnitude; var wr = Quaternion.LookRotation(dir.normalized); mk.Box(plank, mid, new Vector3(1.4f, .12f, len), wr); solid.Box(plank, mid - up * .3f, new Vector3(1.4f, .7f, len), wr); for (var q = 0f; q <= len; q += 3f) mk.Prism(trunkM, from + dir.normalized * q - up * 3.6f, up, .08f, 3.6f, 4); Folk(hp + D * 1.6f + S * 1.6f + up * .1f, D, Resident.Pose.Stand, new[] { "Caught three before the rain. The fourth one caught me looking and went home.", "From here you can stand right under the end of the rainbow. Doesn't make you rich. Makes you wet.", "Listen: the bass comes across the water a half second late. Two riddims for the price of one." }, Local(Residents.Tool.Rod)); }
            }
            // a football and two sticks for a goal
            foreach (var e in new[] { -1.4f, 1.4f }) { var gp = c + S * (10f + e) - D * 6f; gp.y = ctx.Ground(gp.x, gp.z); mk.Prism(woodM, gp - up * .2f, up, .04f, 1.6f, 4); ArtistBits.PostCollider(ctx.Root, gp, 1.4f, .06f); }
            var ball = new MeshKit(); ball.Blob(white, new Vector3(0, .12f, 0), Vector3.one * .12f, rng, 0f); for (var k = 0; k < 5; k++) ball.Blob(dark, new Vector3(0, .12f, 0) + Quaternion.Euler(k * 72f, k * 50f, 0) * Vector3.up * .1f, Vector3.one * .04f, rng, 0f); Thing(ball, "Football", "a football", c + S * 7f - D * 3f + up * .1f, Quaternion.identity, new Vector3(.25f, .25f, .25f), .4f, false);
            Mark(c, 22f);
            Shot(ctx, "the beach and the rainbow", c - D * 16f + S * 12f + up * 3f, c + D * 60f + up * 26f);
        }
    }

    /// The Roots Bay's holder for things you can use and carry.
    public class RootsTownLife : MonoBehaviour { }
}
