using UnityEngine;

namespace EverythingWorld.World
{
    /// More of the Backroads' places, out west and down the coast (see OddballPlaces):
    /// - a flying saucer crashed in a crop circle on the gold plateau (its pilot waving, a farmer and a cow staring);
    /// - BIG FRUIT, a roadside stand of giant fruit (oranges to pick up and throw);
    /// - a shoe tree (pairs of shoes hanging from it, a pile of shoes to throw up into it);
    /// - a living room on the beach (sofa, rug, lamp and a television facing the sea);
    /// - a fishing pier that stops halfway, a bench at its end;
    /// - a windmill on the south-east hill that turns backwards (crank it and it spins).
    public partial class OddballKit
    {
        void Ufo(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var g = Site(ctx, UfoAt, 24f); var f = new Frame(g, FaceTo(g, NearestLoop(g)));
            // the crop circle: rings of flattened wheat, pale against the standing gold
            var flatM = WorldMaterials.Solid(new Color(.98f, .9f, .6f));
            foreach (var (r0, r1) in new[] { (0f, 7f), (10f, 12f), (15f, 16.5f) })
                for (var k = 0; k < 32; k++)
                {
                    float a0 = k * Mathf.PI * 2f / 32f, a1 = (k + 1) * Mathf.PI * 2f / 32f;
                    Vector3 P(float a, float r) { var p = g + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * r; return ctx.OnGround(p.x, p.z) + up * .05f; }
                    if (r0 <= 0f) mk.Tri(flatM, g + up * .05f, P(a1, r1), P(a0, r1), g + up * -1f);
                    else mk.Quad(flatM, P(a0, r0), P(a0, r1), P(a1, r1), P(a1, r0), g - up);
                }
            // the saucer, nose down in the middle, a hatch you can open
            var saucer = WorldMaterials.Solid(new Color(.75f, .8f, .85f), .85f); var tilt = Quaternion.AngleAxis(18f, f.Right);
            var sc = g + up * 1.8f; var sd = tilt * up;
            mk.Prism(saucer, sc - sd * .4f, sd, 5f, .8f, 24, .35f); mk.Prism(saucer, sc - sd * .4f, -sd, 5f, .5f, 24, .6f);
            mk.Prism(GlowM(new Color(.5f, 1f, .6f), 2f), sc + sd * .4f, sd, 1.8f, 1f, 16, .4f);
            for (var k = 0; k < 10; k++) { var a = k * Mathf.PI / 5f; mk.Blob(GlowM(k % 2 == 0 ? new Color(.5f, 1f, .6f) : Flamingo, 2.4f), sc + tilt * new Vector3(Mathf.Cos(a) * 4.6f, 0, Mathf.Sin(a) * 4.6f), Vector3.one * .25f, ctx.Rng, 0f); }
            solid.Box(saucer, sc, new Vector3(9f, 1.6f, 9f), tilt * f.r);
            ArtistBits.PointLight(ctx.Root, sc + up * 3f, new Color(.5f, 1f, .6f), 20f, 1.8f);
            halos.Add(sc + sd * 1f, 3.5f, new Color(.5f, 1f, .6f), .5f);
            var hatch = new MeshKit(); hatch.Box(saucer, new Vector3(0, 0, .8f), new Vector3(1.4f, .1f, 1.6f), Quaternion.identity);
            var hgo = hatch.BuildSingle("Saucer hatch", plife.transform, false); hgo.transform.SetPositionAndRotation(sc + tilt * new Vector3(0, .5f, 2.2f), tilt * f.r);
            var ht = Interact.Toy(hgo, Toy.Kind.Swing, "Open the hatch", new Vector3(1.6f, .4f, 1.8f)); ht.axis = Vector3.right; ht.amount = -70f; hgo.GetComponent<BoxCollider>().center = new Vector3(0, 0, .8f);
            // its pilot, waving; a farmer and a cow staring at it; a space rock to pick up
            var alien = new Residents.Look { skin = new Color(.55f, .9f, .5f), hair = new Color(.55f, .9f, .5f), top = new Color(.75f, .8f, .85f), bottom = new Color(.75f, .8f, .85f), height = 1.3f };
            Folk(f.P(3f, 0, 7f), f.Fwd, Resident.Pose.Wave, new[] { "Take me to your... actually, where's the diner?", "Lovely planet. Bit loud. Mostly flamingos.", "I came for the music. Stayed for the pie." }, alien);
            Folk(f.P(-8f, 0, 13f), FaceTo(f.P(-8f, 0, 13f), g), Resident.Pose.Lean, new[] { "Third one this month.", "It parked on my best wheat. Didn't even say sorry." }, Look(Residents.Tool.Rake));
            Cow(ctx, mk, f.P(-11f, 0, 11f), FaceTo(f.P(-11f, 0, 11f), g));
            var rock = new MeshKit(); rock.Blob(GlowM(new Color(.6f, .4f, 1f), 1.4f), Vector3.zero, Vector3.one * .25f, prng, .3f);
            var rgo = rock.BuildSingle("Space rock", plife.transform, false); rgo.transform.position = f.P(2f, .3f, 5f); Interact.Carry(rgo, "the space rock", .6f, g.y - 5f);
            Lane(ctx, mk, f.P(0, 0, 18f), 0f); Ring(g, 18f);
            Shot(ctx, "the crashed saucer", f.P(-6f, 2.2f, 22f), sc);
        }

        void Cow(LandContext ctx, MeshKit mk, Vector3 at, Vector3 face)
        {
            var up = Vector3.up; var g = ctx.OnGround(at.x, at.z); var rot = Quaternion.LookRotation(face); var side = Vector3.Cross(up, face);
            var hide = WorldMaterials.Solid(new Color(.95f, .95f, .92f));
            mk.Box(hide, g + up * 1.1f, new Vector3(.9f, .8f, 1.9f), rot); mk.Box(dark, g + up * 1.3f + side * .3f, new Vector3(.4f, .4f, .6f), rot);
            mk.Box(hide, g + up * 1.4f + face * 1.1f, new Vector3(.5f, .5f, .6f), rot); mk.Box(Flamingo == Flamingo ? WorldMaterials.Solid(new Color(1f, .7f, .7f)) : hide, g + up * 1.25f + face * 1.45f, new Vector3(.45f, .25f, .15f), rot);
            foreach (var x in new[] { -.3f, .3f }) foreach (var z in new[] { -.7f, .7f }) mk.Box(hide, g + up * .35f + side * x + face * z, new Vector3(.15f, .7f, .15f), rot);
            BoxCol(ctx.Root, g + up * .9f, new Vector3(1f, 1.8f, 2.6f), rot);
        }

        void FruitStand(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var g = Site(ctx, FruitAt, 14f); var f = new Frame(g, FaceTo(g, NearestLoop(g)));
            // the stand: a counter under a striped awning, BIG FRUIT on the sign
            mk.Box(wood, f.P(0, .5f, 0), new Vector3(6f, 1f, 1.2f), f.r); solid.Box(wood, f.P(0, .5f, 0), new Vector3(6f, 1f, 1.2f), f.r);
            foreach (var e in new[] { -2.9f, 2.9f }) { mk.Box(woodDark, f.P(e, 1.5f, -.4f), new Vector3(.15f, 3f, .15f), f.r); ArtistBits.PostCollider(ctx.Root, f.P(e, 0, -.4f), 3f, .12f); }
            for (var k = 0; k < 7; k++) mk.Box(k % 2 == 0 ? stripeRed : white, f.P(-2.6f + k * .86f, 3.05f, .1f), new Vector3(.86f, .08f, 1.6f), f.r * Quaternion.Euler(14f, 0, 0));
            mk.Box(white, f.P(0, 3.7f, -.4f), new Vector3(4f, .9f, .1f), f.r); ArtistBits.Label(ctx.Root, "BIG FRUIT", f.P(0, 3.72f, -.33f), f.Fwd, .5f, new Color(.9f, .35f, .1f), TextAnchor.MiddleCenter);
            Folk(f.P(0, 0, -1.4f), f.Fwd, Resident.Pose.Wave, new[] { "Fresh today! Grew overnight. Kept growing.", "Oranges are free if you can throw one over the barn.", "The watermelon's got a door. Don't ask me why." }, Look(Residents.Tool.None, true));
            // giant fruit round it: a watermelon you can walk into, a peach, a pear, a pile of bananas
            var melon = f.P(-9f, 0, 3f); var mg = ctx.OnGround(melon.x, melon.z); var green = WorldMaterials.Solid(new Color(.2f, .5f, .2f)); var red = WorldMaterials.Solid(new Color(.95f, .3f, .35f));
            mk.Blob(green, mg + up * 2.2f, new Vector3(3.2f, 2.4f, 4f), ctx.Rng, 0f); for (var k = 0; k < 6; k++) mk.Box(WorldMaterials.Solid(new Color(.3f, .62f, .28f)), mg + up * 2.2f + f.Right * (-2.5f + k), new Vector3(.25f, 4.6f, 7.6f), f.r);
            mk.Box(red, mg + up * 1.6f + f.Fwd * 3.6f, new Vector3(2f, 3f, .3f), f.r); mk.Box(dark, mg + up * 1.4f + f.Fwd * 3.62f, new Vector3(1.4f, 2.6f, .3f), f.r);
            BoxCol(ctx.Root, mg + up * 2.2f - f.Fwd * .6f, new Vector3(5.6f, 4.4f, 5.6f), f.r);
            var peach = f.P(8f, 0, 2f); var pg = ctx.OnGround(peach.x, peach.z); mk.Blob(WorldMaterials.Solid(new Color(1f, .65f, .45f)), pg + up * 2.4f, Vector3.one * 2.5f, ctx.Rng, .05f); mk.Prism(woodDark, pg + up * 4.6f, up, .15f, 1f, 6); mk.Blob(kudzu, pg + up * 5.3f + f.Right * .6f, new Vector3(.8f, .2f, .4f), ctx.Rng, 0f);
            BoxCol(ctx.Root, pg + up * 2.2f, new Vector3(4.2f, 4.4f, 4.2f), Quaternion.identity);
            var pear = f.P(11f, 0, -3f); var prg = ctx.OnGround(pear.x, pear.z); mk.Blob(WorldMaterials.Solid(new Color(.75f, .85f, .3f)), prg + up * 1.6f, Vector3.one * 1.7f, ctx.Rng, 0f); mk.Blob(WorldMaterials.Solid(new Color(.75f, .85f, .3f)), prg + up * 3.6f, Vector3.one * 1f, ctx.Rng, 0f);
            BoxCol(ctx.Root, prg + up * 2f, new Vector3(3f, 4f, 3f), Quaternion.identity);
            for (var k = 0; k < 4; k++) mk.Prism(WorldMaterials.Solid(new Color(1f, .88f, .25f)), f.P(-5f + k * .9f, .5f, -3f), Quaternion.Euler(0, 0, 20f + k * 12f) * f.Right, .45f, 3.5f, 8, .5f);
            BoxCol(ctx.Root, f.P(-3.6f, .8f, -3f), new Vector3(4f, 1.6f, 1.4f), f.r);
            // a crate of oranges on the counter: pick one up and throw it; crates to sit on
            var orange = WorldMaterials.Solid(new Color(1f, .55f, .1f));
            for (var k = 0; k < 6; k++)
            {
                var om = new MeshKit(); om.Blob(orange, Vector3.zero, Vector3.one * .13f, prng, .05f);
                var go = om.BuildSingle("Orange", plife.transform, false); go.transform.position = f.P(-1.2f + (k % 3) * .3f, 1.15f, (k / 3) * .3f - .15f);
                Interact.Carry(go, "an orange", .25f, g.y - 5f);
            }
            for (var k = 0; k < 3; k++) { var c = f.P(-2f + k * 2f, 0, 4f); var cg = ctx.OnGround(c.x, c.z); mk.Box(wood, cg + up * .25f, new Vector3(.7f, .5f, .5f), f.r); BoxCol(ctx.Root, cg + up * .25f, new Vector3(.7f, .5f, .5f), f.r); Interact.Seat(plife.transform, cg + up * .5f, -f.Fwd, "Sit on the crate"); }
            ArtistBits.PointLight(ctx.Root, f.P(0, 3f, 2f), Sun, 14f, 1.2f);
            Lane(ctx, mk, f.P(0, 0, 8f), 0f); Ring(g, 14f);
            Shot(ctx, "the giant fruit stand", f.P(2f, 1.8f, 14f), f.P(0, 1.6f, 0));
        }

        void ShoeTree(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var g = Site(ctx, ShoeAt, 10f); var f = new Frame(g, FaceTo(g, NearestLoop(g))); const float H = 12f;
            mk.Prism(woodDark, g - up * .3f, up, .7f, H * .5f, 8, .6f); ArtistBits.PostCollider(ctx.Root, g, 5f, .8f);
            var branches = new System.Collections.Generic.List<(Vector3 a, Vector3 b)>();
            for (var k = 0; k < 7; k++)
            {
                var a = k * Mathf.PI * 2f / 7f; var from = g + up * (H * .45f + (k % 3) * .6f); var to = from + new Vector3(Mathf.Cos(a) * 4f, 2f + (k % 2) * 1.5f, Mathf.Sin(a) * 4f);
                mk.Prism(woodDark, from, (to - from).normalized, .25f, Vector3.Distance(from, to), 6, .5f); branches.Add((from, to));
                mk.Blob(k % 2 == 0 ? WorldMaterials.Solid(new Color(.3f, .52f, .22f)) : kudzu, to + up * .6f, Vector3.one * 2f, ctx.Rng, .3f);
            }
            // pairs of shoes dangling by their laces
            Color[] shoeC = { stripeRed.color, Sky, Sun, new Color(.95f, .95f, .92f), Flamingo, new Color(.2f, .2f, .22f), new Color(.4f, .7f, .4f) };
            var n = 0;
            foreach (var (a, b) in branches)
                for (var t = .35f; t < 1f; t += .22f)
                {
                    var hang = Vector3.Lerp(a, b, t); var drop = .6f + (n % 3) * .3f; var sm = WorldMaterials.Solid(shoeC[n % shoeC.Length]);
                    mk.Box(fenceM, hang - up * drop / 2f, new Vector3(.02f, drop, .02f), Quaternion.identity);
                    foreach (var e in new[] { -.12f, .12f }) mk.Box(sm, hang - up * (drop + .1f) + new Vector3(e, 0, 0), new Vector3(.1f, .12f, .28f), Quaternion.Euler(0, n * 37f, 0));
                    n++;
                }
            // a pile of old shoes to throw up into it
            for (var k = 0; k < 6; k++)
            {
                var sk = new MeshKit(); var sm = WorldMaterials.Solid(shoeC[(k + 2) % shoeC.Length]); sk.Box(sm, new Vector3(0, .06f, 0), new Vector3(.11f, .12f, .3f), Quaternion.identity); sk.Box(white, new Vector3(0, .01f, 0), new Vector3(.12f, .03f, .31f), Quaternion.identity);
                var go = sk.BuildSingle("Shoe", plife.transform, false); go.transform.SetPositionAndRotation(f.P(2.5f + (k % 3) * .3f, .1f, 1.5f + (k / 3) * .3f), Quaternion.Euler(0, k * 50f, 0));
                Interact.Carry(go, "a shoe", .4f, g.y - 5f);
            }
            var sign = f.P(-3f, 0, 3f); mk.Prism(woodDark, sign - up * .2f, up, .07f, 1.6f, 6); mk.Box(white, sign + up * 1.5f, new Vector3(1.8f, .6f, .06f), f.r); ArtistBits.PostCollider(ctx.Root, sign, 1.5f, .1f);
            ArtistBits.Label(ctx.Root, "THE SHOE TREE\nadd yours", sign + up * 1.55f + f.Fwd * .04f, f.Fwd, .14f, Ink, TextAnchor.MiddleCenter);
            Folk(f.P(3.5f, 0, 3.5f), FaceTo(f.P(3.5f, 0, 3.5f), g + up * 6f), Resident.Pose.Stand, new[] { "Threw my first pair up there in '98. Still waiting for them.", "Every shoe's got a story. Most of them are about feet." }, Look());
            Interact.Bench(plife.transform, f.P(-4f, .47f, -2f), f.P(-2.6f, .47f, -3.4f), FaceTo(f.P(-3.3f, 0, -2.7f), g));
            mk.Box(wood, f.P(-3.3f, .45f, -2.7f), new Vector3(1.8f, .06f, .45f), Quaternion.LookRotation(Vector3.Cross(up, FaceTo(f.P(-3.3f, 0, -2.7f), g)))); BoxCol(ctx.Root, f.P(-3.3f, .25f, -2.7f), new Vector3(1.8f, .5f, .45f), Quaternion.LookRotation(Vector3.Cross(up, FaceTo(f.P(-3.3f, 0, -2.7f), g))));
            ArtistBits.PointLight(ctx.Root, g + up * 4f + f.Fwd * 3f, Sun, 14f, 1.1f);
            Lane(ctx, mk, f.P(0, 0, 6f), 0f); Ring(g, 9f);
            Shot(ctx, "the shoe tree", f.P(3f, 1.8f, 12f), g + up * 6f);
        }

        void BeachRoom(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var g = Site(ctx, BeachAt, 4f);
            var toSea = Vector3.zero; for (var a = 0; a < 360; a += 15) { var d = Quaternion.Euler(0, a, 0) * Vector3.forward; if (ctx.Ground(g.x + d.x * 40f, g.z + d.z * 40f) < .3f) toSea += d; }
            if (toSea.sqrMagnitude < .01f) toSea = Vector3.right; var f = new Frame(g, toSea.normalized);
            mk.Box(WorldMaterials.Solid(new Color(.85f, .35f, .4f)), g + up * .03f, new Vector3(4f, .03f, 3f), f.r);
            var sofa = WorldMaterials.Solid(new Color(.45f, .6f, .75f));
            mk.Box(sofa, f.P(0, .3f, -1f), new Vector3(2.4f, .5f, .9f), f.r); mk.Box(sofa, f.P(0, .75f, -1.4f), new Vector3(2.4f, .7f, .25f), f.r); foreach (var e in new[] { -1.15f, 1.15f }) mk.Box(sofa, f.P(e, .55f, -1f), new Vector3(.25f, .55f, .9f), f.r);
            BoxCol(ctx.Root, f.P(0, .5f, -1.1f), new Vector3(2.5f, 1f, 1f), f.r);
            foreach (var e in new[] { -.65f, 0f, .65f }) Interact.Seat(plife.transform, f.P(e, .55f, -.9f), f.Fwd, "Sit on the sofa");
            // the television, facing the sea, showing nothing but sea
            mk.Box(woodDark, f.P(0, .35f, 2f), new Vector3(1.4f, .7f, .6f), f.r); mk.Box(dark, f.P(0, 1.05f, 2f), new Vector3(1.1f, .8f, .7f), f.r); mk.Box(GlowM(new Color(.4f, .7f, 1f), 1.2f), f.P(0, 1.05f, 1.64f), new Vector3(.9f, .6f, .02f), f.r);
            BoxCol(ctx.Root, f.P(0, .7f, 2f), new Vector3(1.4f, 1.4f, .7f), f.r);
            mk.Prism(metal, f.P(1.8f, 0, -1f), up, .03f, 1.6f, 6); mk.Prism(GlowM(Sun, 1.6f), f.P(1.8f, 1.6f, -1f), up, .28f, .35f, 10, .5f); ArtistBits.PostCollider(ctx.Root, f.P(1.8f, 0, -1f), 1.8f, .3f);
            ArtistBits.PointLight(ctx.Root, f.P(1.8f, 1.8f, -1f), Sun, 9f, 1.2f); halos.Add(f.P(1.8f, 1.75f, -1f), 1f, Sun, .4f);
            Folk(f.P(.65f, .55f, -.9f), f.Fwd, Resident.Pose.Sit, new[] { "Best show on. Never any adverts.", "Tide's the plot. It keeps coming back.", "Mind the rug, it's new. It's sand." }, Look(Residents.Tool.Mug));
            Lane(ctx, mk, f.P(0, 0, -6f), 0f); Ring(g, 6f);
            Shot(ctx, "the living room on the beach", f.P(-3f, 1.7f, -5f), f.P(0, .8f, 1f));
        }

        void Pier(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var g = Site(ctx, PierAt, 4f);
            var toSea = Vector3.zero; for (var a = 0; a < 360; a += 15) { var d = Quaternion.Euler(0, a, 0) * Vector3.forward; if (ctx.Ground(g.x + d.x * 45f, g.z + d.z * 45f) < .3f) toSea += d; }
            if (toSea.sqrMagnitude < .01f) toSea = Vector3.right; var f = new Frame(g, toSea.normalized);
            var deck = Mathf.Max(g.y, 1.6f) + .4f; const float L = 34f, W = 3f;
            for (var z = 0f; z < L; z += 1f)
            {
                var p = f.P(0, 0, z); mk.Box(wood, new Vector3(p.x, deck, p.z), new Vector3(W, .12f, .9f), f.r);
                if (((int)z) % 4 == 0) foreach (var e in new[] { -1f, 1f }) { var post = p + f.Right * e * (W / 2f - .1f); var gy = Mathf.Min(ctx.Ground(post.x, post.z), .3f) - 1f; mk.Box(woodDark, new Vector3(post.x, (gy + deck + 1f) / 2f, post.z), new Vector3(.22f, deck + 1f - gy, .22f), f.r); }
            }
            solid.Box(wood, new Vector3(f.P(0, 0, L / 2f).x, deck - .1f, f.P(0, 0, L / 2f).z), new Vector3(W, .3f, L), f.r);
            foreach (var e in new[] { -1f, 1f }) { var a = f.P(e * (W / 2f - .1f), 0, 0); var b = f.P(e * (W / 2f - .1f), 0, L - .5f); mk.Box(woodDark, (a + b) / 2f + up * (deck + .9f - (a.y + b.y) / 2f), new Vector3(.08f, .08f, L), f.r); BoxCol(ctx.Root, new Vector3((a.x + b.x) / 2f, deck + .5f, (a.z + b.z) / 2f), new Vector3(.12f, 1f, L), f.r); }
            // it just stops; a bench at the end facing the sea, a lamp, someone fishing
            var end = f.P(0, 0, L - 1.2f); end.y = deck;
            mk.Box(wood, end + up * .45f - f.Right * .4f, new Vector3(.45f, .06f, 1.6f), Quaternion.LookRotation(f.Right)); Interact.Bench(plife.transform, end + up * .5f - f.Right * .4f - f.Fwd * .7f + f.Right * 0f, end + up * .5f - f.Right * .4f + f.Fwd * .7f, f.Right);
            mk.Prism(metal, end + f.Right * 1.2f, up, .05f, 2.6f, 6); mk.Blob(GlowM(Sun, 1.8f), end + f.Right * 1.2f + up * 2.7f, Vector3.one * .2f, ctx.Rng, 0f); ArtistBits.PointLight(ctx.Root, end + up * 3f, Sun, 12f, 1.3f);
            Folk(end + f.Right * .6f - f.Fwd * 3f, f.Fwd, Resident.Pose.Work, new[] { "It was meant to go further. They ran out of 'further'.", "Caught a boot once. Then the other one. Now I'm waiting on a third." }, Look(Residents.Tool.Rod)).workRate = .3f;
            mk.Box(white, f.P(0, 0, 1f) + up * (deck - f.P(0, 0, 1f).y + 2.2f), new Vector3(2.4f, .5f, .06f), f.r);
            ArtistBits.Label(ctx.Root, "PIER (MOSTLY)", f.P(0, 0, 1f) + up * (deck - f.P(0, 0, 1f).y + 2.2f) - f.Fwd * .04f, -f.Fwd, .16f, Ink, TextAnchor.MiddleCenter);
            Lane(ctx, mk, f.P(0, 0, -5f), 0f); Ring(g, 8f);
            Shot(ctx, "the pier", f.P(-6f, deck - g.y + 1.7f, -4f), f.P(0, deck - g.y, L));
        }

        void Windmill(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var g = Site(ctx, MillAt, 10f); var f = new Frame(g, FaceTo(g, center)); const float H = 11f;
            mk.Prism(WorldMaterials.Solid(new Color(.95f, .92f, .85f)), g - up * .3f, up, 3f, H, 8, .6f); mk.Cone(stripeRed, g + up * (H - .3f), 2.1f, 2.4f, 8);
            ArtistBits.PostCollider(ctx.Root, g, H, 3f);
            mk.Box(woodDark, f.P(0, 1.2f, 2.85f), new Vector3(1.2f, 2.4f, .1f), f.r); mk.Box(glassM, f.P(0, 6f, 2.2f), new Vector3(.8f, .8f, .05f), f.r);
            // the sails, which turn the wrong way; crank them yourself
            var sails = new MeshKit(); sails.Prism(woodDark, Vector3.zero, Vector3.forward, .25f, .6f, 8);
            for (var k = 0; k < 4; k++) { var r = Quaternion.Euler(0, 0, k * 90f); sails.Box(woodDark, r * new Vector3(0, 3.5f, .6f), new Vector3(.18f, 7f, .1f), r); sails.Box(white, r * new Vector3(.55f, 4.2f, .62f), new Vector3(1f, 5.2f, .04f), r); }
            var sgo = sails.BuildSingle("Windmill sails", plife.transform, false); sgo.transform.SetPositionAndRotation(f.P(0, H - 1.6f, 2.3f), f.r); plife.sails = sgo.transform;
            var crank = new MeshKit(); crank.Prism(metal, Vector3.zero, Vector3.up, .08f, 1f, 6); crank.Box(stripeRed, new Vector3(0, 1f, .25f), new Vector3(.12f, .12f, .6f), Quaternion.identity);
            var cgo = crank.BuildSingle("Windmill crank", plife.transform, false); cgo.transform.position = f.P(1.8f, 0, 3.6f);
            var ct = Interact.Toy(cgo, Toy.Kind.Spin, "Crank the windmill", new Vector3(1f, 2f, 1f)); ct.amount = 720f; ct.seconds = 2f; ct.onUse = t => plife.CrankSails();
            cgo.GetComponent<BoxCollider>().center = new Vector3(0, .8f, 0);
            Folk(f.P(-2.5f, 0, 4.5f), f.Fwd, Resident.Pose.Lean, new[] { "Turns backwards. Un-grinds the flour. Very popular with bakers who've made a mistake.", "Wind's coming from the other side today. Same as always." }, Look(Residents.Tool.None));
            Interact.Bench(plife.transform, f.P(-1.5f, .47f, 7f), f.P(1.5f, .47f, 7f), f.Fwd); mk.Box(wood, f.P(0, .45f, 7f), new Vector3(3.2f, .06f, .45f), f.r); BoxCol(ctx.Root, f.P(0, .25f, 7f), new Vector3(3.2f, .5f, .45f), f.r);
            ArtistBits.PointLight(ctx.Root, f.P(0, 3f, 4f), Sun, 12f, 1.1f);
            Lane(ctx, mk, f.P(0, 0, 8f), 0f); Ring(g, 9f);
            Shot(ctx, "the backwards windmill", f.P(4f, 1.8f, 16f), g + up * 7f);
        }
    }
}
