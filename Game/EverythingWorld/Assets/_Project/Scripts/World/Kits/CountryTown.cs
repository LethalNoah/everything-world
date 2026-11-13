using System.Collections.Generic;
using UnityEngine;
using static EverythingWorld.World.PlaceTools;

namespace EverythingWorld.World
{
    /// The Country Plains' places (part of CountryKit): the Square (the water tower with the town's name, a flagpole,
    /// a gas pump, benches, a horse at a hitching rail, a lemonade stand with glasses to carry); Main Street
    /// (false-front stores along a gravel street: the honky-tonk with a jukebox to play, the general store, the diner,
    /// the feed and seed, the post office, a white church at the end); and the Bonfire Field (pickups in a ring with
    /// their tailgates down, a bonfire, hay bales to sit on, a guitar going round, string lights).
    public partial class CountryKit
    {
        CountryTownLife tlife; System.Random prng; Transform rootT; LandContext ctx0;

        static readonly Color[] Tops = { new Color(.7f, .2f, .18f), new Color(.3f, .4f, .6f), new Color(.92f, .88f, .8f), new Color(.5f, .56f, .4f), new Color(.86f, .7f, .44f), new Color(.24f, .22f, .22f), new Color(.6f, .3f, .3f) };
        static readonly Color[] Bottoms = { new Color(.26f, .34f, .52f), new Color(.22f, .28f, .44f), new Color(.5f, .42f, .3f), new Color(.3f, .3f, .32f) };
        static readonly string[] TownTalk =
        {
            "Evenin'. Sun's going down slow tonight. It always does out here.", "Bonfire's at the end of Main Street. Bring a chair or find a bale.", "Jukebox at the Rusty Boot only takes quarters and it only plays the good ones.",
            "Wheat's in. Now we wait for whatever comes next. Probably a song.", "Sit a spell. There's no rush. There's never been a rush.",
        };

        void TownStart(LandContext ctx)
        {
            rootT = ctx.Root; ctx0 = ctx; prng = new System.Random(1990);
            tlife = new GameObject("Country town life").AddComponent<CountryTownLife>(); tlife.transform.SetParent(life.transform, false);
        }

        Residents.Look Local(Residents.Tool tool = Residents.Tool.None, bool apron = false) { var l = Residents.Random(prng, Tops, Bottoms, new[] { Residents.Hat.Cowboy, Residents.Hat.Cap, Residents.Hat.Cowboy, Residents.Hat.Straw, Residents.Hat.None }, new[] { BarnRed, Denim, Wheat, Cream }); l.tool = tool; l.apron = apron; return l; }
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

        /// A bench to sit on.
        void Bench(Transform parent, MeshKit mk, Vector3 g, Vector3 face, float len = 2f)
        {
            var up = Vector3.up; var rot = Quaternion.LookRotation(face); var side = Vector3.Cross(up, face); for (var k = 0; k < 3; k++) mk.Box(plank, g + up * .46f + face * (-.16f + k * .16f), new Vector3(len, .05f, .13f), rot); for (var k = 0; k < 2; k++) mk.Box(plank, g + up * (.7f + k * .2f) - face * .26f, new Vector3(len, .13f, .05f), rot); foreach (var e in new[] { -1f, 1f }) mk.Box(woodM, g + up * .3f + side * e * (len / 2f - .15f), new Vector3(.08f, .6f, .5f), rot);
            BoxCol(parent, g + up * .45f, new Vector3(len, .9f, .5f), rot); Interact.Bench(tlife.transform, g + up * .52f - side * (len / 2f - .5f), g + up * .52f + side * (len / 2f - .5f), face);
        }

        // ---------------------------------------------------------------- the Square
        void TheSquare(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var c = ctx.OnGround(center.x, center.z); var rng = new System.Random(3);
            mk.Prism(gravel, c - up * .28f, up, 19f, .42f, 30); mk.Prism(Paint(new Color(.5f, .58f, .36f)), c + up * .14f, up, 8f, .02f, 24); for (var k = 0; k < 12; k++) Remember(c + Quaternion.Euler(0, k * 30f, 0) * D * 18f, 1);
            // the water tower: four legs, a round tank, the town's name
            var wt = c - D * 4f + S * 10f; const float H = 16f; foreach (var (x, zz) in new[] { (-1f, -1f), (1f, -1f), (1f, 1f), (-1f, 1f) }) { var foot = wt + new Vector3(x, 0, zz) * 2.2f; mk.Prism(tin, foot - up * .2f, (wt + up * H + new Vector3(x, 0, zz) * 1.6f - foot).normalized, .14f, H + .3f, 6); ArtistBits.PostCollider(ctx.Root, foot, 4f, .2f); }
            for (var h = 3f; h < H; h += 4f) mk.Prism(tin, wt + up * h, up, 2.1f - h * .03f, .1f, 10); mk.Prism(Paint(new Color(.84f, .84f, .82f)), wt + up * H, up, 3.2f, 4.2f, 16); mk.Cone(Paint(new Color(.84f, .84f, .82f)), wt + up * (H + 4.2f), 3.3f, 1.6f, 16); mk.Prism(tin, wt + up * (H - .1f), up, 3.5f, .16f, 16);
            var towerFace = (c - wt); towerFace.y = 0; towerFace.Normalize(); Sign(wt + up * (H + 2.1f) + towerFace * 3.25f, towerFace, "WHEATLEY", .9f, BarnRed); mk.Blob(lampM, wt + up * (H + 5.9f), Vector3.one * .2f, rng, 0f); halos.Add(wt + up * (H + 5.9f), 1.6f, new Color(1f, .4f, .3f), .5f);
            // the flagpole, the gas pump, the hitching rail and a horse
            var fp = c + D * 2f - S * 3f; mk.Prism(white, fp - up * .2f, up, .07f, 10f, 6); ArtistBits.PostCollider(ctx.Root, fp, 9f, .08f); for (var k = 0; k < 4; k++) mk.Box(Paint(k % 2 == 0 ? BarnRed : Cream), fp + up * (9.4f - k * .26f) + S * 1.1f, new Vector3(.02f, .26f, 2.2f), Quaternion.LookRotation(D)); mk.Box(Paint(Denim), fp + up * 9.27f + S * .55f, new Vector3(.03f, .52f, 1.1f), Quaternion.LookRotation(D));
            var gp = new PlaceFrame(c + D * 12f + S * 10f + up * .14f, (-D - S).normalized); mk.Box(Paint(BarnRed), gp.P(0, .9f, 0), new Vector3(.7f, 1.8f, .6f), gp.r); mk.Blob(white, gp.P(0, 2f, 0), new Vector3(.3f, .24f, .2f), rng, 0f); mk.Box(white, gp.P(0, 1.2f, .31f), new Vector3(.5f, .4f, .02f), gp.r); mk.Prism(dark, gp.P(.36f, 1f, 0), gp.Right, .03f, .3f, 4); Col(gp.P(0, .9f, 0), new Vector3(.7f, 1.8f, .6f), gp.r); Pickup(ctx.Root, mk, gp.P(2.6f, 0, .6f), gp.r * Quaternion.Euler(0, 90f, 0), new Color(.4f, .5f, .62f), false);
            var hr = new PlaceFrame(c - D * 10f - S * 8f + up * .14f, (D + S * .4f).normalized); foreach (var e in new[] { -1.4f, 1.4f }) { mk.Prism(woodM, hr.P(e, -.2f, 0), up, .08f, 1.4f, 5); ArtistBits.PostCollider(ctx.Root, hr.P(e, 0, 0), 1.2f, .1f); } mk.Prism(woodM, hr.P(-1.5f, 1.1f, 0), hr.Right, .06f, 3f, 5); Horse(ctx, mk, hr.P(.4f, 0, 1.2f), hr.Right, new Color(.44f, .3f, .2f));
            // the lemonade stand
            var ls = new PlaceFrame(c + D * 10f - S * 11f + up * .14f, (-D + S * .7f).normalized); mk.Box(plank, ls.P(0, .5f, 0), new Vector3(2f, 1f, .8f), ls.r); mk.Box(white, ls.P(0, 1.02f, 0), new Vector3(2.1f, .05f, .9f), ls.r); Col(ls.P(0, .5f, 0), new Vector3(2f, 1f, .8f), ls.r); foreach (var e in new[] { -.9f, .9f }) mk.Prism(woodM, ls.P(e, 1f, -.3f), up, .03f, 1.2f, 4); mk.Box(Paint(new Color(1f, .9f, .4f)), ls.P(0, 2.3f, -.3f), new Vector3(2.1f, .5f, .04f), ls.r); Sign(ls.P(0, 2.3f, -.27f), ls.Fwd, "LEMONADE 25¢", .16f, Ink);
            mk.Prism(glassM(), ls.P(-.5f, 1.05f, 0), up, .16f, .36f, 10); mk.Prism(Paint(new Color(1f, .92f, .5f)), ls.P(-.5f, 1.07f, 0), up, .15f, .26f, 10); for (var k = 0; k < 3; k++) { var gm = new MeshKit(); gm.Prism(glassM(), Vector3.zero, Vector3.up, .04f, .12f, 8); gm.Prism(Paint(new Color(1f, .92f, .5f)), new Vector3(0, .01f, 0), Vector3.up, .036f, .08f, 8); Thing(gm, "Lemonade", "a glass of lemonade, cold", ls.P(.1f + k * .26f, 1.06f, .1f), Quaternion.identity, new Vector3(.1f, .14f, .1f), .1f, false); }
            Folk(ls.P(0, 0, -.8f), ls.Fwd, Resident.Pose.Stand, new[] { "Lemonade's a quarter. I'll take a song instead.", "Fresh squeezed. By me. With my hands. Very carefully.", "You look thirsty. Everybody looks thirsty at golden hour." }, Local(Residents.Tool.None, true));
            // benches round the green, lamps round the edge
            for (var k = 0; k < 4; k++) { var d = Quaternion.Euler(0, k * 90f + 45f, 0) * D; Bench(ctx.Root, mk, c + d * 9.4f + up * .14f, -d); }
            Folk(c + Quaternion.Euler(0, 45f, 0) * D * 9.4f + up * .14f, -(Quaternion.Euler(0, 45f, 0) * D), Resident.Pose.Sit, new[] { "I've sat on this bench every evening for fifty years. Bench is older than me. Just.", "You hear that? Somebody's tuning a guitar down by the bonfire.", "Town's small. Sky's big. That's the trade." }, Local(Residents.Tool.None));
            for (var k = 0; k < 6; k++) { var d = Quaternion.Euler(0, k * 60f + 30f, 0) * D; PostLamp(ctx, mk, c + d * 17.4f + up * .14f, -d, k % 2 == 0); }
            Folk(c + D * 5f + S * 2f + up * .14f, (-D - S * .4f).normalized, Resident.Pose.Wave, new[] { "Well howdy! Welcome to Wheatley. Population: everybody who's here.", "Main Street's that way, runs right out to the bonfire. Can't miss it, it's the big fire.", "Stop in at the Rusty Boot. Tell 'em I sent you. They won't know who I am." });
            Mark(c, 19f);
            Shot(ctx, "the square", c + D * 14f - S * 9f + up * 2.4f, wt + up * 12f);
        }

        Material glassM() => WorldMaterials.Solid(new Color(.8f, .9f, .94f), .9f);

        /// A horse standing at rest.
        void Horse(LandContext ctx, MeshKit mk, Vector3 g, Vector3 face, Color c)
        {
            var up = Vector3.up; var m = Paint(c); face.y = 0; face.Normalize(); var side = Vector3.Cross(up, face); var rng = new System.Random(4);
            mk.Blob(m, g + up * 1.3f, new Vector3(.4f, .45f, 1f), rng, 0f); mk.Prism(m, g + up * 1.4f + face * .8f, (face + up * 1.3f).normalized, .2f, .8f, 6, .7f); mk.Box(m, g + up * 1.95f + face * 1.25f, new Vector3(.24f, .26f, .6f), Quaternion.LookRotation(face) * Quaternion.Euler(30f, 0, 0));
            foreach (var e in new[] { -1f, 1f }) foreach (var q in new[] { -.7f, .7f }) mk.Prism(m, g + up * 1.1f + face * q + side * e * .2f, -up, .07f, 1.1f, 4); mk.Prism(Paint(c * .5f), g + up * 1.3f - face * 1f, (-face - up).normalized, .06f, .7f, 4);
            BoxCol(ctx.Root, g + up * 1.2f, new Vector3(.8f, 2.2f, 2.4f), Quaternion.LookRotation(face));
        }

        // ---------------------------------------------------------------- Main Street
        void MainStreet(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var pts = new List<Vector3>(); for (var d = 18f; d <= streetL + .01f; d += 2f) pts.Add(G(ctx, d, 0)); if (pts.Count < 2) return;
            Strip(mk, pts, gravel, StreetW, .13f, ctx); foreach (var p in pts) Remember(p, 1);
            var n = 0; for (var d = 24f; d <= streetL - 4f; d += 12f, n++) { foreach (var e in new[] { -1f, 1f }) PostLamp(ctx, mk, G(ctx, d + (e > 0 ? 0f : 6f), e * (StreetW / 2f + .5f)), -S * e, (n + (e > 0 ? 0 : 1)) % 2 == 0); }
            var route = new List<Vector3>(); for (var d = 24f; d <= streetL - 6f; d += 15f) route.Add(G(ctx, d, 1.6f) + up * .16f); for (var d = streetL - 6f; d >= 24f; d -= 15f) route.Add(G(ctx, d, -1.6f) + up * .16f);
            if (route.Count > 3) Folk(route[0], D, Resident.Pose.Walk, new[] { "Down to the bonfire and back. My knees know the way.", "Evening. Evening. Evening. You say it to everybody. That's the rule." }, Local(Residents.Tool.None), route.ToArray()).speed = .8f;
            Shot(ctx, "down main street", G(ctx, 20f, 1f) + up * 2.2f, G(ctx, streetL + 30f, 0) + up * 4f);
        }

        static readonly string[] Stores = { "THE RUSTY BOOT", "GENERAL STORE", "DINER", "FEED & SEED", "POST OFFICE", "HARDWARE", "BARBER", "BAIT & TACKLE", "PHARMACY", "BAKERY" };

        /// The stores down Main Street, built after the lanes so nothing stands in one: false fronts on a boardwalk.
        void Storefronts(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var i = 0; var built = 0; var church = false;
            foreach (var e in new[] { 1f, -1f })
            for (var d = 28f + (e > 0 ? 0f : 5f); d <= streetL - 6f; d += 10.5f)
            {
                var p = Wp(d, e * (StreetW / 2f + 6.2f)); if (!ctx.IsLand(p.x, p.z, 6f) || InFootprint(ctx, p.x, p.z, 6f) || LaneNear(p.x, p.z, 6f)) continue;
                var f = new PlaceFrame(ctx.OnGround(p.x, p.z), -S * e);
                if (!church && d > streetL - 30f) { Church(ctx, mk, solid, f); church = true; }
                else { Store(ctx, mk, solid, f, i); i++; }
                built++;
            }
            Debug.Log($"The Country Plains: {built} buildings on Main Street");
        }

        /// A false-front store on a little boardwalk: a tall square front with the name across it, a window, a door, an awning, a bench.
        void Store(LandContext ctx, MeshKit mk, MeshKit solid, PlaceFrame f, int i)
        {
            var up = Vector3.up; float W = 8f, Dp = 7f, H = 4f; var wall = Paint(Paints[i % Paints.Length]); var trim = white; var name = Stores[i % Stores.Length]; var tonk = i == 0;
            mk.Box(wall, f.P(0, H / 2f - .3f, 0), new Vector3(W, H + .6f, Dp), f.r); solid.Box(wall, f.P(0, H / 2f - .3f, 0), new Vector3(W, H + .6f, Dp), f.r); mk.GableRoof(tin, f.P(0, H, -.4f), new Vector3(W + .2f, 0, Dp - .6f), 1.4f, f.r);
            mk.Box(wall, f.P(0, H + 1.1f, Dp / 2f - .1f), new Vector3(W, 2.2f, .2f), f.r); mk.Box(trim, f.P(0, H + 2.24f, Dp / 2f - .1f), new Vector3(W + .2f, .14f, .3f), f.r); mk.Box(Paint(Cream), f.P(0, H + .9f, Dp / 2f + .01f), new Vector3(W - 1f, .9f, .04f), f.r); Sign(f.P(0, H + .9f, Dp / 2f + .04f), f.Fwd, name, .32f, Ink);
            mk.Box(plank, f.P(0, .1f, Dp / 2f + 1.2f), new Vector3(W, .2f, 2.4f), f.r); solid.Box(plank, f.P(0, -.1f, Dp / 2f + 1.2f), new Vector3(W, .5f, 2.4f), f.r); mk.Box(tin, f.P(0, 3f, Dp / 2f + 1.2f), new Vector3(W, .06f, 2.4f), f.r * Quaternion.Euler(10f, 0, 0)); foreach (var e in new[] { -1f, 1f }) { mk.Prism(woodM, f.P(e * (W / 2f - .2f), .2f, Dp / 2f + 2.3f), up, .07f, 2.8f, 4); ArtistBits.PostCollider(rootT, f.P(e * (W / 2f - .2f), 0, Dp / 2f + 2.3f), 2.8f, .09f); }
            mk.Box(woodM, f.P(W * .26f, 1.25f, Dp / 2f + .04f), new Vector3(1.1f, 2.1f, .06f), f.r); mk.Box(windowM, f.P(-W * .18f, 1.6f, Dp / 2f + .04f), new Vector3(2.8f, 1.6f, .06f), f.r); mk.Box(trim, f.P(-W * .18f, 1.6f, Dp / 2f + .08f), new Vector3(.06f, 1.6f, .03f), f.r); halos.Add(f.P(-W * .18f, 1.6f, Dp / 2f + .7f), 1.8f, new Color(1f, .78f, .46f), .26f);
            Bench(rootT, mk, f.P(-W * .18f, .2f, Dp / 2f + .7f), f.Fwd, 2f);
            if (tonk)
            {
                // the honky-tonk: neon in the window, a jukebox out front to play
                mk.Box(GlowM(new Color(1f, .3f, .3f), 2f), f.P(-W * .18f, 2.1f, Dp / 2f + .09f), new Vector3(1.4f, .3f, .02f), f.r); var jb = f.P(-W * .4f, .2f, Dp / 2f + 1.6f); mk.Box(Paint(new Color(.5f, .2f, .16f)), jb + up * .8f, new Vector3(.9f, 1.6f, .6f), f.r); mk.Prism(GlowM(new Color(1f, .7f, .3f), 1.4f), jb + up * 1.5f + f.Fwd * .3f, f.Fwd, .42f, .03f, 14); mk.Box(GlowM(new Color(1f, .5f, .3f), 1.2f), jb + up * .8f + f.Fwd * .31f, new Vector3(.6f, .5f, .02f), f.r); Col(jb + up * .8f, new Vector3(.9f, 1.6f, .6f), f.r);
                string[] juke = { "You drop a quarter. The jukebox thinks about it, then picks a sad one. Correctly.", "Steel guitar. Somebody inside whoops. Somebody else starts a two-step.", "B-7. You don't know what B-7 is. It's perfect.", "Clunk. Crackle. A fiddle comes in, and the whole street sways a little." }; var jn = 0;
                Spot("The jukebox", jb + up * 1f + f.Fwd * .6f, "Drop a quarter in", new Vector3(1f, 1f, .8f)).onUse = _ => { life.played = Time.time; Say(tlife.transform, jb + up * 2.6f, juke[jn++ % juke.Length], 4.5f); };
                for (var k = 0; k < 2; k++) Folk(f.P(-1f + k * 2.4f, .2f, Dp / 2f + 1.5f), f.Fwd, k == 0 ? Resident.Pose.Wave : Resident.Pose.Lean, new[] { "Two-step lesson's at eight. Lesson is: just do it.", "Band's late. Band's always late. Jukebox never is.", "Grab a boot, find a partner, mind the spurs." }, Local(Residents.Tool.Mug));
                ArtistBits.PointLight(rootT, f.P(0, 2.6f, Dp / 2f + 1.8f), new Color(1f, .6f, .4f), 12f, 2f);
            }
            else if (i % 3 == 1) Folk(f.P(W * .26f + 1.2f, .2f, Dp / 2f + 1f), f.Fwd, Resident.Pose.Lean, new[] { "Open till the sun's down. So, a while yet.", "We've got one of everything and two of nothing.", "My granddaddy built this front. The back's newer. The back's 1950." }, Local(Residents.Tool.Broom, true));
            Mark(f.o, 6.4f);
        }

        /// A little white church with a steeple and a bell.
        void Church(LandContext ctx, MeshKit mk, MeshKit solid, PlaceFrame f)
        {
            var up = Vector3.up; float W = 7f, Dp = 11f, H = 4.6f;
            mk.Box(white, f.P(0, H / 2f - .3f, -1f), new Vector3(W, H + .6f, Dp), f.r); solid.Box(white, f.P(0, H / 2f - .3f, -1f), new Vector3(W, H + .6f, Dp), f.r); mk.GableRoof(Paint(new Color(.3f, .3f, .34f)), f.P(0, H, -1f), new Vector3(W + .5f, 0, Dp + .4f), 2.6f, f.r);
            var st = f.P(0, H, Dp / 2f - 2.2f); mk.Box(white, st + up * 1.6f, new Vector3(2f, 3.2f, 2f), f.r); mk.Cone(Paint(new Color(.3f, .3f, .34f)), st + up * 3.2f, 1.5f, 4.4f, 4, 45f); mk.Box(dark, st + up * 1.8f + f.Fwd * 1.01f, new Vector3(1f, 1.4f, .02f), f.r); mk.Prism(Paint(new Color(.8f, .66f, .3f)), st + up * 2f, -up, .3f, .5f, 8, 1.4f); mk.Box(Paint(new Color(.8f, .66f, .3f)), st + up * 8.2f, new Vector3(.1f, 1f, .1f), f.r); mk.Box(Paint(new Color(.8f, .66f, .3f)), st + up * 8.4f, new Vector3(.6f, .1f, .1f), f.r);
            mk.Box(woodM, f.P(0, 1.3f, Dp / 2f - 1f + .03f), new Vector3(1.6f, 2.6f, .06f), f.r); foreach (var e in new[] { -2.2f, 2.2f }) { mk.Box(windowM, f.P(e, 2f, Dp / 2f - 1f + .03f), new Vector3(.9f, 1.8f, .05f), f.r); halos.Add(f.P(e, 2f, Dp / 2f - .4f), 1.2f, new Color(1f, .8f, .5f), .3f); } for (var k = 0; k < 3; k++) mk.Box(white, f.P(0, -.1f + k * .2f, Dp / 2f - .4f + (2 - k) * .3f), new Vector3(2.4f, .2f, .4f), f.r);
            Mark(f.o, 7f);
        }

        // ---------------------------------------------------------------- the Bonfire Field
        void BonfireField(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var bc = G(ctx, streetL + 20f, 0); var rng = new System.Random(88);
            mk.Prism(Paint(new Color(.5f, .56f, .3f)), bc - up * .26f, up, 21f, .36f, 30); for (var k = 0; k < 12; k++) Remember(bc + Quaternion.Euler(0, k * 30f, 0) * D * 19f, 1); Remember(bc - D * 8f, 1);
            // the bonfire: a ring of stones, a cone of logs, the flames
            mk.Prism(Paint(new Color(.4f, .36f, .32f)), bc, up, 1.8f, .3f, 12); for (var k = 0; k < 12; k++) mk.Blob(Paint(new Color(.56f, .52f, .48f)), bc + Quaternion.Euler(0, k * 30f, 0) * D * 1.9f + up * .2f, Vector3.one * .28f, rng, .2f); for (var k = 0; k < 8; k++) { var d = Quaternion.Euler(0, k * 45f, 0) * D; mk.Prism(woodM, bc + d * 1.2f + up * .2f, (up * 2f - d).normalized, .12f, 2.2f, 5); }
            mk.Cone(fireM, bc + up * .3f, 1f, 2.6f, 7); mk.Cone(fireM, bc + up * .3f + S * .3f, .6f, 1.8f, 6, 30f); BoxCol(ctx.Root, bc + up * .8f, new Vector3(3.6f, 1.6f, 3.6f), Quaternion.identity); halos.Add(bc + up * 1.4f, 4f, Fire, .4f); ArtistBits.PointLight(ctx.Root, bc + up * 2f, new Color(1f, .6f, .3f), 24f, 3f); life.fireAt = bc + up * 2f;
            // hay bales and camp chairs round the fire; the pickups in a ring beyond, tailgates in
            for (var k = 0; k < 7; k++) { var d = Quaternion.Euler(0, k * 51f + 10f, 0) * D; if (k % 2 == 0) Bale(ctx, mk, bc + d * 5f, Vector3.Cross(up, d)); else { var cp = bc + d * 5f; mk.Box(Paint(new[] { Denim, BarnRed, new Color(.3f, .46f, .3f) }[k % 3]), cp + up * .45f, new Vector3(.6f, .06f, .5f), Quaternion.LookRotation(-d)); mk.Box(Paint(new[] { Denim, BarnRed, new Color(.3f, .46f, .3f) }[k % 3]), cp + up * .75f + d * .24f, new Vector3(.6f, .6f, .05f), Quaternion.LookRotation(-d)); foreach (var e in new[] { -1f, 1f }) mk.Prism(iron, cp + Vector3.Cross(up, d) * e * .28f, up, .015f, .45f, 3); Interact.Seat(tlife.transform, cp + up * .5f, -d, "Sit by the fire"); } }
            var cols = new[] { BarnRed, Denim, new Color(.8f, .8f, .78f), new Color(.3f, .4f, .26f), new Color(.2f, .2f, .2f), new Color(.62f, .5f, .34f) };
            for (var k = 0; k < 6; k++) { var d = Quaternion.Euler(0, k * 60f + 30f, 0) * D; if (Vector3.Angle(d, -D) < 35f) continue; Pickup(ctx.Root, mk, bc + d * 13f, Quaternion.LookRotation(d), cols[k]); }
            // string lights from truck to truck, on poles
            var poles = new List<Vector3>(); for (var k = 0; k < 8; k++) { var d = Quaternion.Euler(0, k * 45f, 0) * D; var pp = bc + d * 9f; mk.Prism(woodM, pp - up * .2f, up, .07f, 4.4f, 5); ArtistBits.PostCollider(ctx.Root, pp, 4.2f, .09f); poles.Add(pp + up * 4.1f); }
            for (var k = 0; k < 8; k++) { var a = poles[k]; var b = poles[(k + 1) % 8]; var prev = a; for (var q = 1; q <= 8; q++) { var t = q / 8f; var p = Vector3.Lerp(a, b, t) - up * Mathf.Sin(t * Mathf.PI) * .5f; mk.Prism(dark, prev, (p - prev).normalized, .01f, Vector3.Distance(prev, p), 3); if (q < 8) mk.Blob(lampM, p - up * .06f, Vector3.one * .07f, new System.Random(q), 0f); prev = p; } }
            // a guitar going round; people round the fire
            var gt = bc + Quaternion.Euler(0, 61f, 0) * D * 4.2f + up * .2f; mk.Blob(Paint(new Color(.66f, .42f, .24f)), gt + up * .3f, new Vector3(.3f, .38f, .1f), rng, 0f); mk.Box(woodM, gt + up * .9f, new Vector3(.06f, .8f, .04f), Quaternion.LookRotation(D));
            string[] strum = { "G, C, D. You play the three chords everybody knows and everybody sings.", "You play it slow. Somebody harmonises from across the fire. Nobody planned that.", "One more song. Then one more. The fire doesn't mind.", "A broken string. Doesn't matter. You keep going and the crickets cover for you." }; var sn = 0;
            Spot("The guitar by the fire", gt + up * .8f, "Play a song", new Vector3(.8f, 1f, .8f)).onUse = _ => { life.played = Time.time; Say(tlife.transform, gt + up * 2.2f, strum[sn++ % strum.Length], 4.5f); };
            string[][] talk = { new[] { "Best seat's whichever one's warmest.", "Pass the guitar. I know one. I know half of one." }, new[] { "Watch the sparks go up. You can wish on them, I think.", "Sun's down, fire's up. That's the whole schedule." }, new[] { "Tailgate's free. Sit. Somebody'll hand you a drink.", "I came for one song. That was three hours ago." } };
            for (var k = 0; k < 6; k++) { var d = Quaternion.Euler(0, k * 60f + 35f, 0) * D; Folk(bc + d * (k % 2 == 0 ? 3.4f : 7.4f), -d, k % 3 == 0 ? Resident.Pose.Stand : Resident.Pose.Wave, talk[k % 3]); }
            Mark(bc, 21f);
            Shot(ctx, "the bonfire", bc - D * 16f + S * 6f + up * 2.4f, bc + up * 1.6f);
        }
    }

    /// The Country Plains' holder for things you can use and carry.
    public class CountryTownLife : MonoBehaviour { }
}
