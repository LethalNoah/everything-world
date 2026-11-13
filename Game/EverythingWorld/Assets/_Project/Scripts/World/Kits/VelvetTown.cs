using System.Collections.Generic;
using UnityEngine;
using static EverythingWorld.World.PlaceTools;

namespace EverythingWorld.World
{
    /// The Velvet Rooftops' central place (part of VelvetKit): the Pool roof. A long turquoise pool you can wade
    /// through, loungers, a bar with drinks to carry, a small DJ booth to play, sofas round a fire bowl, palms in
    /// pots, the tall VELVET sign, and a telescope to look at the drowned city through.
    public partial class VelvetKit
    {
        VelvetTownLife tlife; System.Random prng; Transform rootT; LandContext ctx0;

        static readonly Color[] Tops = { new Color(.1f, .1f, .14f), new Color(.5f, .08f, .16f), new Color(.95f, .92f, .86f), new Color(.3f, .16f, .4f), new Color(.85f, .65f, .25f), new Color(.14f, .2f, .4f) };
        static readonly Color[] Bottoms = { new Color(.08f, .08f, .12f), new Color(.9f, .88f, .82f), new Color(.24f, .14f, .3f), new Color(.16f, .18f, .28f) };
        static readonly string[] TownTalk =
        {
            "It's two in the morning. It's been two in the morning for a while. Nobody's checked.", "The water came up years ago. We just moved the party to the roof.",
            "You can walk to every roof from here. Follow the glass.", "Don't look down. Well. Do. It's beautiful. All those windows still lit.", "Slow down. You're here now.",
        };

        void TownStart(LandContext ctx)
        {
            rootT = ctx.Root; ctx0 = ctx; prng = new System.Random(2016);
            tlife = new GameObject("Velvet town life").AddComponent<VelvetTownLife>(); tlife.transform.SetParent(life.transform, false);
        }

        Residents.Look Local(Residents.Tool tool = Residents.Tool.None, bool apron = false) { var l = Residents.Random(prng, Tops, Bottoms, new[] { Residents.Hat.None, Residents.Hat.None, Residents.Hat.Cap, Residents.Hat.None, Residents.Hat.Bucket }, Tops); l.tool = tool; l.apron = apron; return l; }
        Resident Folk(Vector3 feet, Vector3 face, Resident.Pose pose, string[] lines = null, Residents.Look look = null, Vector3[] route = null, float seatH = HumanScale.SeatHeight)
            => Residents.Make(tlife.transform, feet, face, look ?? Local(), pose, prng, lines ?? TownTalk, route, seatH);
        void Col(Vector3 c, Vector3 size, Quaternion rot) => BoxCol(rootT, c, size, rot);
        void Mark(Vector3 at, float r) => noGrass.Add(new Vector3(at.x, r, at.z));

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

        /// A cocktail in a glass, to carry.
        void Drink(Vector3 at, Color c, string label)
        {
            var gm = new MeshKit(); gm.Prism(GlowM(c, .7f), Vector3.zero, Vector3.up, .04f, .12f, 8, 1.4f); gm.Prism(steel, new Vector3(0, -.1f, 0), Vector3.up, .008f, .1f, 4); gm.Prism(steel, new Vector3(0, -.11f, 0), Vector3.up, .05f, .01f, 8); Thing(gm, "Drink", label, at + Vector3.up * .11f, Quaternion.identity, new Vector3(.1f, .24f, .1f), .15f, false);
        }

        // ---------------------------------------------------------------- the Pool roof
        void PoolRoof(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var c = ctx.OnGround(center.x, center.z); var rng = new System.Random(4);
            // the deck, with a line of light round its edge
            mk.Prism(neonM[1], c - up * .36f, up, 23.3f, .4f, 36); mk.Prism(deckM, c - up * .28f, up, 23f, .4f, 36); for (var k = 0; k < 16; k++) Remember(c + Quaternion.Euler(0, k * 22.5f, 0) * U * 22f, 0);
            // the pool, east of where you arrive: a long glowing slab of water you can wade through, steps of light round it
            var pc = c + R * 11f + up * .12f; mk.Box(white, pc + up * .02f, new Vector3(8.8f, .06f, 16.8f)); mk.Box(poolM, pc + up * .05f, new Vector3(8f, .04f, 16f)); for (var k = 0; k < 6; k++) mk.Box(GlowM(new Color(.6f, 1f, 1f), 1.4f), pc + up * .075f + U * (-6.5f + k * 2.6f) + R * Mathf.Sin(k * 1.9f) * 2f, new Vector3(2.4f, .01f, .1f), Quaternion.Euler(0, 20f + k * 13f, 0));
            halos.Add(pc + up * 1f, 9f, Aqua, .4f); ArtistBits.PointLight(ctx.Root, pc + up * 2f, new Color(.4f, .95f, 1f), 26f, 3.4f); ArtistBits.PointLight(ctx.Root, pc + up * 2f + U * 6f, new Color(.4f, .95f, 1f), 18f, 2f);
            string[] wade = { "Warm as a bath. The light comes up through the water and makes everybody look like a film.", "You stand in it to the ankles. Sixty floors down, under the sea, somebody's lamp is still on.", "A little wave goes across. It was you. Nobody minds." }; var wn = 0;
            Spot("The pool", pc + up * .8f, "Stand in the pool", new Vector3(7f, 1.6f, 15f)).onUse = _ => Say(tlife.transform, pc + up * 2.6f, wade[wn++ % wade.Length], 5f);
            // loungers down the far side of the pool, a side table and a drink between each pair
            for (var k = 0; k < 5; k++) { var lg = pc + R * 6.6f + U * (-6f + k * 3f); Lounger(mk, lg, -R, k % 2 == 0 ? velvet : white); if (k % 2 == 1) { var st = lg - U * 1.5f - R * .4f; mk.Prism(white, st, up, .2f, .44f, 8); ArtistBits.PostCollider(ctx.Root, st, .5f, .2f); Drink(st + up * .44f, k == 1 ? Rose : Aqua, k == 1 ? "something pink and cold" : "something blue with a flower in it"); } }
            Folk(pc + R * 6.6f + U * 0f + up * .44f, -R, Resident.Pose.Sit, new[] { "Mm. No. I'm not getting up. Ask me in an hour.", "I've been watching one window out there all night. Somebody keeps turning the lamp on and off. I think it's for me." }, null, null, .4f);
            // the bar, north: a lit counter, bottles on a shelf, drinks to carry
            var bar = new PlaceFrame(c + U * 15f - R * 2f + up * .12f, -U); mk.Box(dark, bar.P(0, .55f, 0), new Vector3(6f, 1.1f, .8f), bar.r); mk.Box(neonM[0], bar.P(0, .2f, .41f), new Vector3(5.8f, .06f, .02f), bar.r); mk.Box(neonM[2], bar.P(0, 1.12f, 0), new Vector3(6.2f, .05f, 1f), bar.r); Col(bar.P(0, .55f, 0), new Vector3(6.2f, 1.1f, 1f), bar.r);
            mk.Box(dark, bar.P(0, 1.3f, -1.5f), new Vector3(5f, 2.6f, .3f), bar.r); Col(bar.P(0, 1.3f, -1.5f), new Vector3(5f, 2.6f, .3f), bar.r); for (var s = 0; s < 2; s++) { mk.Box(neonM[1], bar.P(0, 1.4f + s * .7f, -1.33f), new Vector3(4.6f, .04f, .2f), bar.r); for (var k = 0; k < 10; k++) mk.Prism(GlowM(Neons[(k + s) % 5], .5f), bar.P(-2f + k * .44f, 1.42f + s * .7f, -1.3f), up, .05f, .3f + (k % 3) * .06f, 6); }
            Drink(bar.P(-2f, 1.15f, .1f), Gold, "something amber, neat"); Drink(bar.P(-1.2f, 1.15f, .1f), Rose, "something pink and cold"); Drink(bar.P(1.4f, 1.15f, .1f), Violet, "something violet that smokes a little");
            Folk(bar.P(.4f, 0, -.8f), bar.Fwd, Resident.Pose.Stand, new[] { "What are we drinking? Something slow, I think. Sit down, I'll bring it over.", "We don't close. There's no morning to close for.", "The ice comes up from the fortieth floor. Don't ask how." }, Local(Residents.Tool.None, true));
            for (var k = 0; k < 3; k++) { var st = bar.P(-2f + k * 1.8f, 0, 1.3f); mk.Prism(steel, st, up, .04f, .72f, 5); mk.Prism(velvet, st + up * .72f, up, .2f, .08f, 10); ArtistBits.PostCollider(ctx.Root, st, .7f, .1f); Interact.Seat(tlife.transform, st + up * .82f, -bar.Fwd, "Sit at the bar").seatHeight = .8f; }
            // the VELVET sign, tall, behind the bar
            NeonSign(rootT, mk, bar.P(0, 6.4f, -1.9f), bar.Fwd, "VELVET", 1.9f, 0, 9.4f, 2.8f, false); foreach (var e in new[] { -1f, 1f }) { mk.Prism(steel, bar.P(e * 4.2f, -.2f, -2f), up, .1f, 5.2f, 5); ArtistBits.PostCollider(ctx.Root, bar.P(e * 4.2f, 0, -2f), 5f, .12f); } ArtistBits.PointLight(ctx.Root, bar.P(0, 4.6f, 1.6f), Rose, 22f, 2.6f);
            // sofas round a fire bowl, south-west
            var fb = c - U * 12f - R * 9f + up * .12f; mk.Prism(concrete, fb, up, .9f, .5f, 12, 1.2f); mk.Prism(dark, fb + up * .5f, up, 1f, .05f, 12); mk.Cone(GlowM(new Color(1f, .55f, .15f), 2.2f), fb + up * .5f, .5f, 1f, 6); mk.Cone(GlowM(new Color(1f, .55f, .15f), 2.2f), fb + up * .5f + R * .26f, .3f, .7f, 5, 30f); halos.Add(fb + up * 1f, 2.6f, new Color(1f, .6f, .25f), .5f); ArtistBits.PointLight(ctx.Root, fb + up * 1.4f, new Color(1f, .6f, .3f), 16f, 2.6f); ArtistBits.PostCollider(ctx.Root, fb, .8f, 1f);
            for (var k = 0; k < 3; k++) { var d = Quaternion.Euler(0, k * 120f + 30f, 0) * U; Sofa(ctx.Root, mk, fb + d * 3.2f, -d, k == 1 ? white : velvet); }
            Folk(fb + Quaternion.Euler(0, 30f, 0) * U * 3.1f + R * .5f + up * .5f, -(Quaternion.Euler(0, 30f, 0) * U), Resident.Pose.Sit, new[] { "We were going to go somewhere after this. That was hours ago. This is the somewhere.", "Say nothing. Just sit. That's the whole plan." }, null, null, .5f);
            // a small DJ booth, south-east
            var dj = new PlaceFrame(c - U * 14f + R * 6f + up * .12f, (U - R * .3f).normalized); mk.Box(dark, dj.P(0, .55f, 0), new Vector3(2.6f, 1.1f, .8f), dj.r); mk.Box(neonM[3], dj.P(0, .3f, .41f), new Vector3(2.4f, .5f, .02f), dj.r); Col(dj.P(0, .55f, 0), new Vector3(2.6f, 1.1f, .8f), dj.r); foreach (var e in new[] { -.7f, .7f }) { mk.Prism(steel, dj.P(e, 1.12f, 0), up, .26f, .03f, 16); mk.Prism(neonM[e < 0 ? 0 : 1], dj.P(e, 1.15f, 0), up, .08f, .01f, 10); }
            string[] mix = { "You bring the next one in so slowly nobody notices the song has changed. That's the job.", "A long warm chord under a slow drum. Somebody on a lounger says \"oh, this one\".", "You drop the bass out. The whole roof holds its breath. You bring it back.", "Seventy beats a minute. Any slower and it would be the tide." }; var mn = 0;
            Spot("The decks", dj.P(0, 1.4f, .3f), "Play the next one", new Vector3(2.4f, .9f, 1.2f)).onUse = _ => { life.played = Time.time; Say(tlife.transform, dj.P(0, 2.8f, 0), mix[mn++ % mix.Length], 5f); };
            // a telescope on the west edge, to look at the drowned city
            var tl = c - R * 19f + U * 3f + up * .12f; for (var k = 0; k < 3; k++) mk.Prism(steel, tl + Quaternion.Euler(0, k * 120f, 0) * Vector3.forward * .4f, (up * 1.3f - Quaternion.Euler(0, k * 120f, 0) * Vector3.forward * .4f).normalized, .025f, 1.36f, 4); mk.Prism(Paint(Gold), tl + up * 1.4f + R * .4f, (-R + up * .2f).normalized, .08f, 1.1f, 8, 1.4f); ArtistBits.PostCollider(ctx.Root, tl, 1.5f, .3f);
            string[] look = { "A kitchen, forty floors down, under water, the light on. A kettle on the hob. Nobody.", "Someone on a far roof is looking back at you through another telescope. They wave.", "A lift, lit, going down past the waterline, and down, and down.", "A neon sign under the sea, still flickering: OPEN." }; var ln = 0;
            Spot("The telescope", tl + up * 1.5f, "Look through the telescope", new Vector3(1f, 1.2f, 1.4f)).onUse = _ => Say(tlife.transform, tl + up * 2.8f, look[ln++ % look.Length], 5.5f);
            // palms in pots, lamps, and somebody to say hello
            for (var k = 0; k < 8; k++) { var d = Quaternion.Euler(0, k * 45f + 22f, 0) * U; var p = c + d * 21f + up * .12f; if (k % 2 == 0) PotPalm(ctx, mk, p, 1.5f, rng); else Lamp(ctx, mk, p, true); }
            Folk(c - R * 5f - U * 4f + up * .12f, (R + U * .5f).normalized, Resident.Pose.Wave, new[] { "You made it up. Good. Take your shoes off, get a drink, there's no rush for anything.", "Every roof you can see has somebody's music on it. Follow the glass walks.", "The pool's warm. The sea isn't. Stay up here." });
            Mark(c, 24f);
            Shot(ctx, "the pool", c - R * 14f - U * 12f + up * 3f, pc + up * .5f + U * 2f);
            Shot(ctx, "the bar and the sign", c - U * 8f + R * 2f + up * 2f, bar.P(0, 4f, -1f));
            Shot(ctx, "the drowned city", c - R * 20f + up * 2.4f, c - R * 400f + up * 30f);
        }
    }

    /// The Velvet Rooftops' holder for things you can use and carry.
    public class VelvetTownLife : MonoBehaviour { }
}
