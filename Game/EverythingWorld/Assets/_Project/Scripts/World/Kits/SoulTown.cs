using System.Collections.Generic;
using UnityEngine;
using static EverythingWorld.World.PlaceTools;

namespace EverythingWorld.World
{
    /// The Soul Lowlands' town (part of SoulKit): the square, The Royal, the chapel, Main Street and its shops, River
    /// Road and the riverboat landing, with the people of the town in their Sunday best. Places to walk into: The
    /// Royal (a stage with a microphone to step up to, velvet seats), the chapel (pews, the choir, an organ), Sweet
    /// Soul Records (a turntable, records to carry off), the Luncheonette (counter stools, booths, a jukebox, pie),
    /// the barbershop (the chairs, the pole). At the landing, the Lowlands Belle: walk aboard, pull the whistle.
    public partial class SoulKit
    {
        SoulTownLife tlife; System.Random prng; Transform rootT; LandContext ctx0;

        static readonly Color[] Best = { new Color(.95f, .8f, .3f), new Color(.85f, .3f, .35f), new Color(.3f, .55f, .8f), new Color(.95f, .95f, .9f), new Color(.55f, .8f, .6f), new Color(.75f, .5f, .8f), new Color(.98f, .6f, .4f), new Color(.2f, .25f, .4f) };
        static readonly Color[] Slacks = { new Color(.2f, .2f, .28f), new Color(.45f, .35f, .28f), new Color(.85f, .82f, .75f), new Color(.3f, .3f, .32f), new Color(.5f, .2f, .25f) };
        static readonly Color[] HatC = { new Color(.95f, .93f, .85f), new Color(.2f, .2f, .25f), new Color(.85f, .3f, .35f), new Color(.6f, .45f, .3f) };
        static readonly string[] TownTalk =
        {
            "Evening! It's always evening here, and it's always a fine one.", "You hear that? Somebody on Main Street's always singing. Half the time it's all of us.",
            "The light does that every day about now. Gets in the windows like honey.", "If your foot's not tapping you might want to see a doctor.", "Sunday best, every day. You never know when the band's going to strike up.",
        };
        static readonly string[] PorchTalk =
        {
            "Come up on the porch, there's lemonade. There's always lemonade.", "I can hear The Royal from here when the horns get going.", "That swing's held three generations and a good many secrets.",
            "My grandmother sang in that chapel. Her grandmother too. The roof's been lifted clean off more than once.", "Sit a while. The song's not going anywhere.",
        };

        void TownStart(LandContext ctx)
        {
            rootT = ctx.Root; ctx0 = ctx; prng = new System.Random(1967);
            tlife = new GameObject("Soul town life").AddComponent<SoulTownLife>(); tlife.transform.SetParent(life.transform, false);
        }

        Residents.Look Best0(Residents.Tool tool = Residents.Tool.None, bool apron = false) { var l = Residents.Random(prng, Best, Slacks, new[] { Residents.Hat.None, Residents.Hat.Straw, Residents.Hat.Cap, Residents.Hat.None, Residents.Hat.Bucket }, HatC); l.tool = tool; l.apron = apron; return l; }
        Resident Folk(Vector3 feet, Vector3 face, Resident.Pose pose, string[] lines = null, Residents.Look look = null, Vector3[] route = null, float seatH = HumanScale.SeatHeight)
            => Residents.Make(tlife.transform, feet, face, look ?? Best0(), pose, prng, lines ?? TownTalk, route, seatH);
        void Col(Vector3 c, Vector3 size, Quaternion rot) => BoxCol(rootT, c, size, rot);
        void Mark(Vector3 at, float r) => noGrass.Add(new Vector3(at.x, r, at.z));

        /// A room to walk into: floor, three walls, a front wall with a doorway at dx, a flat roof. 'fo' sits on the floor.
        void Room(MeshKit mk, MeshKit solid, PlaceFrame fo, float W, float D, float H, float T, float dx, float door, Material wall, Material floor, Material roof)
        {
            void Wall(Vector3 p, Vector3 s) { mk.Box(wall, p, s, fo.r); solid.Box(wall, p, s, fo.r); }
            mk.Box(floor, fo.P(0, -.06f, 0), new Vector3(W, .12f, D), fo.r); solid.Box(floor, fo.P(0, -.5f, 0), new Vector3(W + 1f, 1f, D + 1f), fo.r); mk.Box(brickDark, fo.P(0, -.55f, 0), new Vector3(W + .3f, .9f, D + .3f), fo.r);
            Wall(fo.P(0, H / 2f, -D / 2f + T / 2f), new Vector3(W, H, T)); foreach (var e in new[] { -1f, 1f }) Wall(fo.P(e * (W / 2f - T / 2f), H / 2f, 0), new Vector3(T, H, D - T * 2f));
            float l = W / 2f + dx - door / 2f, r = W / 2f - dx - door / 2f;
            if (l > .05f) Wall(fo.P(-W / 2f + l / 2f, H / 2f, D / 2f - T / 2f), new Vector3(l, H, T)); if (r > .05f) Wall(fo.P(W / 2f - r / 2f, H / 2f, D / 2f - T / 2f), new Vector3(r, H, T));
            var dh = Mathf.Min(H - .4f, HumanScale.DoorHeight + .2f); Wall(fo.P(dx, (H + dh) / 2f, D / 2f - T / 2f), new Vector3(door, H - dh, T));
            mk.Box(roof, fo.P(0, H + .1f, 0), new Vector3(W + .4f, .2f, D + .4f), fo.r); solid.Box(roof, fo.P(0, H + .1f, 0), new Vector3(W + .4f, .2f, D + .4f), fo.r);
            IndoorSpaces.Add(fo.P(0, H / 2f, 0), fo.r, new Vector3(W - T * 2f - .2f, H, D - T * 2f - .2f));
        }

        void Sign(Vector3 at, Vector3 face, string text, float size, Color c) => ArtistBits.Label(rootT, text, at, face, size, c, TextAnchor.MiddleCenter);

        /// A thing to carry off, built from a small mesh.
        Carryable Thing(MeshKit m, string name, string label, Vector3 at, Quaternion rot, Vector3 size, float mass, bool still = true)
        {
            var go = m.BuildSingle(name, tlife.transform, false); go.transform.SetPositionAndRotation(at, rot); var c = Interact.Carry(go, label, mass, at.y - 30f);
            var bc = c.GetComponent<BoxCollider>(); if (bc != null) { bc.size = size; bc.center = new Vector3(0, size.y / 2f, 0); }
            if (still) { var rb = go.GetComponent<Rigidbody>(); if (rb != null) rb.isKinematic = true; }
            return c;
        }

        // ---------------------------------------------------------------- the square
        void Square(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var c = ctx.OnGround(center.x, center.z);
            mk.Prism(brick, c - up * .28f, up, 27f, .4f, 28); mk.Prism(brickDark, c - up * .27f, up, 7f, .42f, 20); mk.Prism(cream, c - up * .26f, up, 6.5f, .43f, 20);
            // a magnolia in a round planter in the middle, benches round it, the town clock
            foreach (var e in new[] { -1f, 1f })
            {   // (the middle is kept clear: it is where you arrive)
                var pc = c + N * e * 13f; mk.Prism(cream, pc, up, 3.4f, .7f, 14); mk.Prism(WorldMaterials.Solid(new Color(.4f, .3f, .2f)), pc + up * .6f, up, 3.1f, .14f, 14); Col(pc + up * .4f, new Vector3(5.6f, .9f, 5.6f), Quaternion.identity); Col(pc + up * .4f, new Vector3(5.6f, .9f, 5.6f), Quaternion.Euler(0, 45f, 0));
                Magnolia(ctx, mk, pc + up * .6f, 1.4f, prng);
                foreach (var s in new[] { -1f, 1f }) TownBench(mk, pc + E * s * 5.4f + up * .13f, E * s, 2.2f);
            }
            for (var k = 0; k < 8; k++) { var d = Quaternion.Euler(0, k * 45f, 0) * E; mk.Box(k % 2 == 0 ? gold : (Material)cream, c + d * 3.2f + up * .15f, new Vector3(.5f, .02f, 5.6f), Quaternion.LookRotation(d)); }   // a compass star in the brick where you arrive
            var ck = c - E * 13f - N * 13f + up * .13f; mk.Prism(iron, ck, up, .14f, 4.2f, 8); mk.Prism(iron, ck, up, .4f, .5f, 8, .5f); mk.Box(iron, ck + up * 4.7f, new Vector3(1.1f, 1.1f, .5f), Quaternion.LookRotation(E + N));
            foreach (var e in new[] { -1f, 1f }) mk.Prism(windowM, ck + up * 4.7f + (E + N).normalized * e * .26f, (E + N).normalized * e, .45f, .02f, 14); halos.Add(ck + up * 4.7f, 1.6f, Gold, .5f); ArtistBits.PostCollider(ctx.Root, ck, 4.4f, .2f);
            // globe lamps round the edge, bulbs strung from them to a tall pole by the planter
            var lampTops = new List<Vector3>();
            for (var k = 0; k < 8; k++) { var d = Quaternion.Euler(0, k * 45f + 22.5f, 0) * E; var lp = c + d * 24f + up * .13f; GlobeLamp(ctx, mk, lp, k % 2 == 0); lampTops.Add(lp + up * 4.3f); }
            for (var k = 0; k < 8; k++) BulbString(mk, lampTops[k], lampTops[(k + 1) % 8], k);
            // the welcome sign, facing where you arrive from (south-east corner), and people
            var sg = c + E * 16f - N * 16f + up * .13f; var sf = (-(E - N)).normalized; var srot = Quaternion.LookRotation(sf);
            foreach (var e in new[] { -2.2f, 2.2f }) { mk.Box(white, sg + Vector3.Cross(up, sf) * e + up * 1.5f, new Vector3(.2f, 3f, .2f), srot); ArtistBits.PostCollider(ctx.Root, sg + Vector3.Cross(up, sf) * e, 3f, .14f); }
            mk.Box(velvet, sg + up * 2.5f, new Vector3(5f, 1.5f, .12f), srot); mk.Box(gold, sg + up * 3.3f, new Vector3(5.2f, .12f, .16f), srot); mk.Box(gold, sg + up * 1.7f, new Vector3(5.2f, .12f, .16f), srot);
            Sign(sg + up * 2.7f + sf * .08f, sf, "THE SOUL LOWLANDS", .34f, Cream); Sign(sg + up * 2.15f + sf * .08f, sf, "come as you are  ·  stay for the chorus", .15f, Gold);
            Folk(c + E * 9f + N * 4f + up * .13f, -E, Resident.Pose.Wave); Folk(c - E * 6f + N * 11f + up * .13f, -N, Resident.Pose.Stand, new[] { "The Royal's got a show on tonight. The Royal's got a show on every night.", "Chapel doors are open. They don't close; I don't believe they've got a lock.", "Main Street's that way, down to the river. Take your time. Everybody does." });
            var stroll = new List<Vector3>(); for (var k = 0; k < 12; k++) stroll.Add(c + Quaternion.Euler(0, k * 30f, 0) * E * 17f + up * .13f);
            Folk(stroll[0], FaceTo(stroll[0], stroll[1]), Resident.Pose.Walk, new[] { "Once round the square before the show. It's the done thing.", "Lovely evening. They're all lovely. You stop being surprised and start being grateful." }, null, stroll.ToArray()).speed = .9f;
            Mark(c, 28f);
            Shot(ctx, "the square and the royal", c + E * 21f - N * 6f + up * 2.2f, c - E * 40f + N * 12f + up * 6f);
        }

        // ---------------------------------------------------------------- The Royal
        void Theatre(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var g = ctx.OnGround(theatre2.x, theatre2.y); var fo = new PlaceFrame(g + up * .3f, E); const float W = 22f, D = 26f, H = 9f, T = .5f, Door = 3.4f;
            var plaster = WorldMaterials.Solid(new Color(.93f, .84f, .66f)); var carpet = WorldMaterials.Solid(new Color(.45f, .1f, .16f));
            Room(mk, solid, fo, W, D, H, T, 0, Door, plaster, carpet, brickDark);
            // the front: a stepped parapet, the blade sign, the marquee with its bulbs
            mk.Box(plaster, fo.P(0, H + 1.2f, D / 2f - .3f), new Vector3(W, 2.4f, .6f), fo.r); mk.Box(plaster, fo.P(0, H + 3f, D / 2f - .3f), new Vector3(W * .55f, 1.4f, .6f), fo.r); mk.Box(gold, fo.P(0, H + 3.75f, D / 2f - .3f), new Vector3(W * .57f, .14f, .7f), fo.r); mk.Box(gold, fo.P(0, H + 2.45f, D / 2f - .28f), new Vector3(W + .2f, .12f, .7f), fo.r);
            var blade = fo.P(0, H + 1.6f, D / 2f + .9f); mk.Box(velvet, blade, new Vector3(1.9f, 8.4f, .7f), fo.r); foreach (var e in new[] { -1f, 1f }) { mk.Box(gold, blade + fo.Right * e * 1f, new Vector3(.1f, 8.6f, .76f), fo.r); for (var k = 0; k < 14; k++) mk.Blob(bulbM[k % 3], blade + fo.Right * e * 1.1f + up * (-4f + k * .6f) + fo.Fwd * .36f, Vector3.one * .09f, prng, 0f); }
            Sign(blade + fo.Fwd * .37f, fo.Fwd, "R\nO\nY\nA\nL", .95f, Gold); halos.Add(blade + fo.Fwd * 1.2f, 4.5f, Gold, .4f);
            var mq = fo.P(0, 4.5f, D / 2f + 2.2f); mk.Box(cream, mq, new Vector3(13f, 1.5f, 4.4f), fo.r); mk.Box(velvet, mq + up * .82f, new Vector3(13.2f, .16f, 4.6f), fo.r); mk.Box(velvet, mq - up * .82f, new Vector3(13.2f, .16f, 4.6f), fo.r);
            for (var k = 0; k < 22; k++) { mk.Blob(bulbM[k % 3], mq + fo.Right * (-6.3f + k * .6f) + fo.Fwd * 2.25f - up * .92f, Vector3.one * .1f, prng, 0f); mk.Blob(bulbM[(k + 1) % 3], mq + fo.Right * (-6.3f + k * .6f) + fo.Fwd * 2.25f + up * .92f, Vector3.one * .1f, prng, 0f); }
            for (var k = 0; k < 7; k++) foreach (var e in new[] { -1f, 1f }) mk.Blob(bulbM[k % 3], mq + fo.Right * e * 6.55f + fo.Fwd * (-1.8f + k * .65f) - up * .92f, Vector3.one * .1f, prng, 0f);
            for (var x = -5; x <= 5; x++) for (var z = -1; z <= 1; z++) mk.Blob(bulbM[(x + z + 12) % 3], mq + fo.Right * x * 1.1f + fo.Fwd * z * 1.3f - up * .78f, Vector3.one * .08f, prng, 0f);
            Sign(mq + fo.Fwd * 2.22f + up * .2f, fo.Fwd, "TONIGHT  ·  THE LOWLANDS SOUL REVUE", .38f, Velvet); Sign(mq + fo.Fwd * 2.22f - up * .36f, fo.Fwd, "all seats one song  ·  doors never close", .19f, Ink);
            foreach (var e in new[] { -1f, 1f }) { Sign(mq + fo.Right * e * 6.52f, fo.Right * e, "THE ROYAL", .5f, Velvet); mk.Box(iron, fo.P(e * 6f, 2.2f, D / 2f + 4.1f), new Vector3(.2f, 4.4f, .2f), fo.r); ArtistBits.PostCollider(ctx.Root, fo.P(e * 6f, 0, D / 2f + 4.1f), 4.4f, .14f); }
            halos.Add(mq + fo.Fwd * 3f, 6f, Gold, .35f); ArtistBits.PointLight(ctx.Root, mq + fo.Fwd * 1f - up * 1.6f, Gold, 18f, 2.4f);
            // poster cases beside the doors, the ticket booth to one side
            foreach (var e in new[] { -1f, 1f }) { mk.Box(gold, fo.P(e * 4.4f, 2f, D / 2f + .04f), new Vector3(1.9f, 2.7f, .08f), fo.r); mk.Box(WorldMaterials.Glow(Pastel[e > 0 ? 1 : 2], .7f), fo.P(e * 4.4f, 2f, D / 2f + .09f), new Vector3(1.6f, 2.4f, .04f), fo.r); Sign(fo.P(e * 4.4f, 2.5f, D / 2f + .13f), fo.Fwd, e > 0 ? "ONE NIGHT\nONLY" : "HELD OVER", .26f, Ink); Sign(fo.P(e * 4.4f, 1.4f, D / 2f + .13f), fo.Fwd, e > 0 ? "(every night)" : "by popular\ndemand", .14f, Velvet); }
            var tb = fo.P(-8.6f, 0, D / 2f + 1.6f); mk.Box(velvet, tb + up * .6f, new Vector3(1.8f, 1.2f, 1.8f), fo.r); mk.Box(gold, tb + up * 1.22f, new Vector3(1.9f, .06f, 1.9f), fo.r); mk.Box(cream, tb + up * 2.5f, new Vector3(1.9f, .3f, 1.9f), fo.r); foreach (var (x, z) in new[] { (-1f, -1f), (1f, -1f), (1f, 1f), (-1f, 1f) }) mk.Box(gold, tb + up * 1.8f + fo.Right * x * .85f + fo.Fwd * z * .85f, new Vector3(.1f, 1.2f, .1f), fo.r);
            Col(tb + up * 1.3f, new Vector3(1.8f, 2.6f, 1.8f), fo.r); Sign(tb + up * 2.5f + fo.Fwd * .96f, fo.Fwd, "TICKETS", .16f, Velvet);
            var seller = Best0(); seller.top = Velvet; seller.hat = Residents.Hat.Cap; seller.hatColor = Velvet; Folk(tb + up * .3f, fo.Fwd, Resident.Pose.Stand, new[] { "One ticket? Honey, put your money away. Walk right in.", "Best seats are wherever you end up sitting.", "The mic's open between acts. It's nearly always between acts." }, seller);
            // inside: the stage with its velvet curtain, a microphone in a spotlight, an upright piano; rows of velvet seats
            float sz = -D / 2f + T + 3.5f; mk.Box(woodDark, fo.P(0, .55f, sz), new Vector3(W - T * 2f, 1.1f, 7f), fo.r); solid.Box(woodDark, fo.P(0, .55f, sz), new Vector3(W - T * 2f, 1.1f, 7f), fo.r); mk.Box(gold, fo.P(0, 1.08f, sz + 3.5f), new Vector3(W - T * 2f, .08f, .1f), fo.r);
            foreach (var e in new[] { -1f, 1f }) for (var k = 0; k < 3; k++) { var st = fo.P(e * (W / 2f - T - 1.3f), .18f + k * .37f, sz + 3.5f + (2 - k) * .4f + .2f); mk.Box(woodDark, st, new Vector3(2.2f, .37f, .4f), fo.r); solid.Box(woodDark, st - up * (k * .185f), new Vector3(2.2f, .37f * (k + 1), .4f), fo.r); }
            for (var k = 0; k < 16; k++) mk.Box(k % 2 == 0 ? velvet : WorldMaterials.Solid(new Color(.5f, .08f, .15f)), fo.P(-W / 2f + T + .7f + k * 1.33f, H / 2f + .3f, -D / 2f + T + .25f), new Vector3(1.33f, H - 1.6f, .3f), fo.r);
            mk.Box(gold, fo.P(0, H - .9f, -D / 2f + T + .5f), new Vector3(W - T * 2f, .9f, .5f), fo.r); foreach (var e in new[] { -1f, 1f }) mk.Box(velvet, fo.P(e * (W / 2f - T - 1.2f), H / 2f, sz + 3.2f), new Vector3(2.2f, H - .4f, .3f), fo.r);
            var micAt = fo.P(0, 1.1f, sz + 1.6f); var mm = new MeshKit(); mm.Prism(iron, Vector3.zero, Vector3.up, .02f, 1.45f, 6); mm.Prism(iron, Vector3.zero, Vector3.up, .22f, .04f, 10); mm.Blob(WorldMaterials.Solid(new Color(.75f, .76f, .8f), .8f), new Vector3(0, 1.52f, 0), new Vector3(.07f, .1f, .07f), prng, 0f);
            var mgo = mm.BuildSingle("The Royal's microphone", tlife.transform, false); mgo.transform.position = micAt; tlife.spot = ArtistBits.PointLight(ctx.Root, micAt + up * 4.5f + fo.Fwd * 1.5f, new Color(1f, .93f, .75f), 14f, 1.2f, true);
            string[] sing = { "You step up. The spotlight finds you. Somebody in the dark says \"Go on, now!\"", "You hit the note. The piano player nods like he knew you would.", "The whole room answers your line back to you. That's the call and that's the response.", "A standing ovation. Granted, they were already standing." }; var sn = 0;
            Interact.Toy(mgo, Toy.Kind.Custom, "Step up to the microphone", new Vector3(.9f, 1.8f, .9f)).onUse = _ => { tlife.sang = Time.time; Say(tlife.transform, micAt + up * 2.6f, sing[sn++ % sing.Length], 5f); };
            var pn = fo.P(-6.4f, 1.1f, sz - .6f); mk.Box(woodDark, pn + up * .65f, new Vector3(1.6f, 1.3f, .65f), fo.r); mk.Box(white, pn + up * .78f + fo.Fwd * .36f, new Vector3(1.4f, .05f, .22f), fo.r); Col(pn + up * .65f, new Vector3(1.6f, 1.3f, .7f), fo.r);
            var pianist = Best0(); pianist.top = new Color(.2f, .2f, .28f); Folk(pn + fo.Fwd * 1f, -fo.Fwd, Resident.Pose.Work, new[] { "Call the tune. Any key. I've only got the one key but I play it in every key.", "Thirty years on this bench. The bench is younger than the stool it replaced.", "Go on, the mic's warm." }, pianist).workRate = 1.4f;
            for (var row = 0; row < 5; row++) foreach (var e in new[] { -1f, 1f })
            {
                var rc = fo.P(e * 5.4f, 0, -1.2f + row * 2.3f); mk.Box(velvet, rc + up * .42f, new Vector3(7.6f, .14f, .6f), fo.r); mk.Box(velvet, rc + up * .85f + fo.Fwd * .3f, new Vector3(7.6f, .8f, .12f), fo.r); mk.Box(gold, rc + up * 1.27f + fo.Fwd * .3f, new Vector3(7.6f, .05f, .14f), fo.r); mk.Box(woodDark, rc + up * .2f, new Vector3(7.6f, .4f, .5f), fo.r);
                Col(rc + up * .6f + fo.Fwd * .1f, new Vector3(7.6f, 1.2f, .8f), fo.r); Interact.Bench(tlife.transform, rc + up * .52f - fo.Right * 3.3f - fo.Fwd * .05f, rc + up * .52f + fo.Right * 3.3f - fo.Fwd * .05f, -fo.Fwd);
            }
            Folk(fo.P(4f, .52f, -1.25f), -fo.Fwd, Resident.Pose.Sit, new[] { "I saw everybody on that stage. Everybody. Some of them twice in one night.", "Shh. No, I'm joking, you can talk. You can sing along. That's the whole idea." }, null, null, .52f);
            Folk(fo.P(-6.5f, .52f, 3.35f), -fo.Fwd, Resident.Pose.Sit, new[] { "My feet hurt and I couldn't be happier.", "Save me that seat if I get up to dance. I'm going to get up to dance." }, null, null, .52f);
            foreach (var e in new[] { -1f, 1f }) { ArtistBits.PointLight(ctx.Root, fo.P(e * 6f, H - 2.2f, 3f), new Color(1f, .8f, .55f), 15f, .9f, true); mk.Blob(windowM, fo.P(e * (W / 2f - T - .15f), 4.2f, 2f), new Vector3(.12f, .4f, .25f), prng, 0f); mk.Blob(windowM, fo.P(e * (W / 2f - T - .15f), 4.2f, 8f), new Vector3(.12f, .4f, .25f), prng, 0f); }
            ArtistBits.PointLight(ctx.Root, fo.P(0, H - 2f, sz + 2f), new Color(1f, .7f, .5f), 16f, 1.2f, true);
            Mark(g, 22f); Remember(ctx.OnGround(g.x + E.x * 15f, g.z + E.z * 15f), 1);
            Shot(ctx, "inside the royal", fo.P(5f, 1.9f, D / 2f - 2f), fo.P(-1f, 2.6f, sz));
        }

        // ---------------------------------------------------------------- the chapel
        void Chapel(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var g = ctx.OnGround(chapel2.x, chapel2.y); var face = FaceTo(g, ctx.OnGround(center.x, center.z)); var fo = new PlaceFrame(g + up * .5f, face); const float W = 9f, D = 15f, H = 5f, T = .35f, Door = 2.4f;
            var roofM = WorldMaterials.Solid(new Color(.42f, .5f, .58f)); var pine = WorldMaterials.Solid(new Color(.72f, .56f, .36f));
            Room(mk, solid, fo, W, D, H, T, 0, Door, white, pine, white);
            foreach (var e in new[] { -1f, 1f }) { var rr = fo.r * Quaternion.Euler(0, 0, -e * 38f); mk.Box(roofM, fo.P(e * W / 4f, H + 1.95f, 0), new Vector3(W / 2f / Mathf.Cos(38f * Mathf.Deg2Rad) + .7f, .16f, D + 1f), rr); }
            foreach (var z in new[] { D / 2f - .1f, -D / 2f + .1f }) mk.Prism(white, fo.P(0, H + .2f, z), fo.Fwd, W / 2f, .12f, 3);
            // the steeple over the door, with its bell; steps up; the doors swung open
            var tw = fo.P(0, 0, D / 2f - 1.5f); mk.Box(white, tw + up * (H + 2.6f), new Vector3(2.8f, 5.2f, 2.8f), fo.r); mk.Box(roofM, tw + up * (H + 5.3f), new Vector3(3.2f, .2f, 3.2f), fo.r); mk.Cone(roofM, tw + up * (H + 5.4f), 1.7f, 4.2f, 4, 45f + fo.r.eulerAngles.y); mk.Box(gold, tw + up * (H + 10.2f), new Vector3(.1f, 1.1f, .1f), fo.r); mk.Box(gold, tw + up * (H + 10.35f), new Vector3(.6f, .1f, .1f), fo.r);
            foreach (var d in new[] { fo.Fwd, fo.Right, -fo.Right }) mk.Box(dark, tw + up * (H + 3.6f) + d * 1.41f, new Vector3(1.1f, 1.6f, .04f), Quaternion.LookRotation(d)); mk.Blob(gold, tw + up * (H + 3.5f) + fo.Fwd * .9f, new Vector3(.4f, .45f, .4f), prng, 0f);
            for (var k = 0; k < 3; k++) { var st = fo.P(0, -.5f + (k + .5f) * .17f, D / 2f + .3f + (2 - k) * .4f); mk.Box(white, st, new Vector3(Door + 1.4f, .17f, .4f), fo.r); solid.Box(white, st - up * (k * .085f), new Vector3(Door + 1.4f, .17f * (k + 1), .4f), fo.r); }
            foreach (var e in new[] { -1f, 1f }) { mk.Box(WorldMaterials.Solid(new Color(.55f, .2f, .2f)), fo.P(e * (Door / 2f + .55f), 1.1f, D / 2f + .55f), new Vector3(.08f, 2.2f, 1.1f), fo.r * Quaternion.Euler(0, e * 12f, 0)); Col(fo.P(e * (Door / 2f + .55f), 1.1f, D / 2f + .55f), new Vector3(.12f, 2.2f, 1.1f), fo.r * Quaternion.Euler(0, e * 12f, 0)); }
            // tall coloured windows down both sides (they shine both ways)
            Color[] glass = { new Color(1f, .8f, .35f), new Color(.4f, .65f, 1f), new Color(1f, .45f, .5f), new Color(.5f, .9f, .6f) };
            for (var k = 0; k < 4; k++) foreach (var e in new[] { -1f, 1f }) { var wm = new Material(WorldMaterials.Glow(glass[(k + (e > 0 ? 1 : 0)) % 4], 1.1f)) { name = "Chapel glass" }; tlife.glass.Add((wm, glass[(k + (e > 0 ? 1 : 0)) % 4])); var wp = fo.P(e * (W / 2f - T / 2f), 2.6f, -D / 2f + 2.6f + k * 3.1f); mk.Box(wm, wp, new Vector3(T + .06f, 2.6f, 1.1f), fo.r); mk.Prism(wm, wp + up * 1.3f - fo.Right * (T / 2f + .03f), fo.Right, .55f, T + .06f, 8); halos.Add(wp + fo.Right * e * .6f, 1.5f, glass[(k + (e > 0 ? 1 : 0)) % 4], .35f); }
            // pews, the choir on risers at the far end, the pulpit, the organ
            for (var row = 0; row < 4; row++) foreach (var e in new[] { -1f, 1f })
            {
                var pc = fo.P(e * 2.5f, 0, 4.2f - row * 2f); mk.Box(pine, pc + up * .44f, new Vector3(3f, .07f, .5f), fo.r); mk.Box(pine, pc + up * .8f + fo.Fwd * .25f, new Vector3(3f, .75f, .07f), fo.r); foreach (var q in new[] { -1.5f, 1.5f }) mk.Box(woodDark, pc + up * .5f + fo.Right * q + fo.Fwd * .1f, new Vector3(.07f, 1f, .6f), fo.r);
                Col(pc + up * .55f + fo.Fwd * .1f, new Vector3(3f, 1.1f, .65f), fo.r); Interact.Bench(tlife.transform, pc + up * .5f - fo.Right * 1.1f, pc + up * .5f + fo.Right * 1.1f, -fo.Fwd);
            }
            for (var k = 0; k < 3; k++) { var rs = fo.P(0, (k + 1) * .15f, -D / 2f + T + 2.6f - k * .8f); mk.Box(pine, rs, new Vector3(W - T * 2f - .6f, (k + 1) * .3f, .8f), fo.r); solid.Box(pine, rs, new Vector3(W - T * 2f - .6f, (k + 1) * .3f, .8f), fo.r); }
            Color[] robes = { new Color(.5f, .15f, .5f), new Color(.2f, .3f, .7f), new Color(.5f, .15f, .5f), new Color(.2f, .3f, .7f), new Color(.5f, .15f, .5f) };
            string[] choir = { "Oh, we lift the roof off about four times a service. They nail it back down Mondays.", "Sopranos to the left, altos to the right, everybody else: wherever the spirit puts you.", "You don't have to be able to sing. You just have to mean it.", "Clap on the two and the four, baby. The two and the four.", "We've been on the same hymn since noon and it only gets better." };
            for (var k = 0; k < 5; k++) { var l = Best0(); l.top = robes[k]; l.bottom = robes[k]; l.hat = Residents.Hat.None; var row = k % 2; Folk(fo.P(-2.8f + k * 1.4f, (row + 1) * .3f + .15f * row, -D / 2f + T + 2.6f - row * .8f), fo.Fwd, k % 2 == 0 ? Resident.Pose.Wave : Resident.Pose.Stand, new[] { choir[k], choir[(k + 2) % 5] }, l); }
            var pul = fo.P(-2.6f, 0, -D / 2f + T + 4.4f); mk.Box(pine, pul + up * .6f, new Vector3(.9f, 1.2f, .7f), fo.r); mk.Box(woodDark, pul + up * 1.24f, new Vector3(1f, .06f, .8f), fo.r * Quaternion.Euler(12f, 0, 0)); Col(pul + up * .6f, new Vector3(.9f, 1.2f, .7f), fo.r);
            var og = fo.P(2.7f, 0, -D / 2f + T + 4.6f); var om = new MeshKit(); om.Box(woodDark, new Vector3(0, .6f, 0), new Vector3(1.5f, 1.2f, .7f), Quaternion.identity); om.Box(white, new Vector3(0, .95f, .36f), new Vector3(1.3f, .05f, .2f), Quaternion.identity); for (var k = 0; k < 7; k++) om.Prism(gold, new Vector3(-.6f + k * .2f, 1.2f, -.15f), Vector3.up, .06f, 1f + (3 - Mathf.Abs(k - 3)) * .35f, 6);
            var ogo = om.BuildSingle("Chapel organ", tlife.transform, false); ogo.transform.SetPositionAndRotation(og, fo.r); Col(og + up * .6f, new Vector3(1.5f, 1.2f, .7f), fo.r);
            string[] organ = { "One big chord. The windows light up and the choir comes in like sunrise.", "You find the bass pedal. The whole floor hums in the key of yes.", "You play three notes. The choir makes it a song. They can make anything a song." }; var on = 0;
            Interact.Toy(ogo, Toy.Kind.Custom, "Play the organ", new Vector3(1.8f, 2.4f, 1.4f)).onUse = _ => { tlife.organ = Time.time; Say(tlife.transform, og + up * 3f, organ[on++ % organ.Length], 5f); };
            for (var k = 0; k < 3; k++) { var fm = new MeshKit(); fm.Prism(Paint(k + 1), new Vector3(0, .16f, 0), Vector3.forward, .16f, .01f, 10); fm.Box(wood, new Vector3(0, .0f, 0), new Vector3(.03f, .2f, .012f), Quaternion.identity); Thing(fm, "Church fan", "a paper church fan", fo.P(1.6f + k * .45f, .5f, 4.2f - 6f) + up * .02f, fo.r * Quaternion.Euler(80f, k * 30f, 0), new Vector3(.34f, .06f, .4f), .1f); }
            ArtistBits.PointLight(ctx.Root, fo.P(0, H - 1f, 1f), new Color(1f, .9f, .7f), 14f, .9f, true); ArtistBits.PointLight(ctx.Root, fo.P(0, 2.4f, D / 2f + 3f), Gold, 12f, 1.4f);
            Folk(fo.P(2.5f, .5f, 2.2f) - fo.Right * 1.1f, -fo.Fwd, Resident.Pose.Sit, new[] { "I come for the singing and stay for the singing.", "That organ's older than the town. They built the chapel round it." }, null, null, .5f);
            Folk(ctx.OnGround(fo.P(-3.6f, 0, D / 2f + 3.2f).x, fo.P(-3.6f, 0, D / 2f + 3.2f).z), face, Resident.Pose.Wave, new[] { "Welcome, welcome! Doors are open. Come in and get yourself a fan.", "You look like you could use a good song. Everybody could." });
            // a path from the square to the chapel steps
            var way = PlaceTools.Smooth(ctx, new[] { new Vector2(center.x, center.z) + new Vector2(-face.x, -face.z) * 27f, new Vector2(g.x, g.z) + new Vector2(face.x, face.z) * (D / 2f + 2f) }, false, 2f, 1f);
            if (way.Count > 1) { Strip(mk, way, brick, 3.4f, .15f, ctx); foreach (var p in way) Remember(p, 2); }
            Mark(g, 14f);
            Shot(ctx, "the chapel", fo.P(-7f, 2.4f, D / 2f + 13f), fo.P(0, 5f, 0));
            Shot(ctx, "inside the chapel", fo.P(1.2f, 1.7f, D / 2f - 1.2f), fo.P(0, 1.8f, -D / 2f + 2f));
        }

        // ---------------------------------------------------------------- Main Street
        void MainStreet(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var c = new Vector3(center.x, 0, center.z);
            var pts = new List<Vector3>(); for (var d = 26f; d <= StreetTo + 1f; d += 2f) pts.Add(ctx.OnGround(c.x + E.x * d, c.z + E.z * d));
            Strip(mk, pts, brick, StreetW, .16f, ctx); foreach (var p in pts) Remember(p, 3);
            for (var d = StreetFrom; d < StreetTo; d += 4f) mk.Box(brickDark, ctx.OnGround(c.x + E.x * d, c.z + E.z * d) + up * .18f, new Vector3(.3f, .02f, StreetW - .6f), Quaternion.LookRotation(E));
            // lamps down both sides, bulbs zigzagging across between them
            var tops = new List<Vector3>();
            for (var k = 0; k <= 5; k++) foreach (var e in new[] { -1f, 1f }) { var d = StreetFrom + 1f + k * 22f; var lp = ctx.OnGround(c.x + E.x * d + N.x * e * (StreetW / 2f + .6f), c.z + E.z * d + N.z * e * (StreetW / 2f + .6f)); GlobeLamp(ctx, mk, lp, k % 2 == 0 && e > 0); tops.Add(lp + up * 4.3f); }
            for (var k = 0; k + 3 < tops.Count; k += 2) { BulbString(mk, tops[k], tops[k + 3], k); BulbString(mk, tops[k + 1], tops[k + 2], k + 1); }
            // five lots a side
            Vector3 Lot(int k, float side, float depth) { var d = StreetFrom + 12f + k * 22f; return ctx.OnGround(c.x + E.x * d + N.x * side * (StreetW / 2f + 1.4f + depth / 2f), c.z + E.z * d + N.z * side * (StreetW / 2f + 1.4f + depth / 2f)); }
            RecordShop(ctx, mk, solid, Lot(0, 1, 9f), -N); Barbershop(ctx, mk, solid, Lot(1, 1, 7f), -N);
            Facade(ctx, mk, solid, Lot(2, 1, 8f), -N, 15f, 7.5f, "LOWLANDS HOTEL", "rooms  ·  a piano in the lobby", 0, true); Facade(ctx, mk, solid, Lot(3, 1, 8f), -N, 12f, 5f, "MISS ETTA'S HATS", "for Sunday, and every day is", 4, false); Facade(ctx, mk, solid, Lot(4, 1, 8f), -N, 12f, 5f, "GOLDEN CRUST BAKERY", "sweet potato pie", 3, false);
            Diner(ctx, mk, solid, Lot(0, -1, 8f), N); RadioStation(ctx, mk, solid, Lot(1, -1, 8f), N);
            Facade(ctx, mk, solid, Lot(2, -1, 8f), N, 12f, 5f, "CUE & CHALK", "pool  ·  dominoes  ·  opinions", 5, false); Facade(ctx, mk, solid, Lot(3, -1, 8f), N, 14f, 7.5f, "FINE TAILORING", "sharp as the horn section", 2, true); Facade(ctx, mk, solid, Lot(4, -1, 8f), N, 12f, 5f, "ICE CREAM & SODAS", "two straws", 1, false);
            // the corner group under the lamp, a couple strolling, benches
            var corner = ctx.OnGround(c.x + E.x * 76f + N.x * 3.2f, c.z + E.z * 76f + N.z * 3.2f) + up * .16f;
            string[][] doo = { new[] { "Doo-wop, doo-wah... you take the high part. No, higher. There it is.", "We've been on this corner since we were twelve. Same song. We nearly have it." }, new[] { "Bom, bom, bom... I'm the bass. Somebody has to be the ground everybody stands on.", "Bom. That's my whole part. I wouldn't trade it." }, new[] { "Oooooh... I do the oooh. It's harder than it looks.", "When the four of us lock in you can feel the lamp post hum. There's three of us. You're the fourth." } };
            for (var k = 0; k < 3; k++) { var l = Best0(); l.top = new Color(.2f, .25f, .4f); l.hat = Residents.Hat.None; Folk(corner + Quaternion.Euler(0, k * 120f, 0) * E * .9f, -(Quaternion.Euler(0, k * 120f, 0) * E), k == 1 ? Resident.Pose.Wave : Resident.Pose.Stand, doo[k], l); }
            var a = ctx.OnGround(c.x + E.x * 34f - N.x * 2.6f, c.z + E.z * 34f - N.z * 2.6f) + up * .16f; var b = ctx.OnGround(c.x + E.x * 130f - N.x * 2.6f, c.z + E.z * 130f - N.z * 2.6f) + up * .16f; var route = new List<Vector3>(); for (var k = 0; k <= 12; k++) { var p = Vector3.Lerp(a, b, k / 12f); route.Add(ctx.OnGround(p.x, p.z) + up * .16f); } for (var k = 12; k >= 0; k--) { var p = Vector3.Lerp(a, b, k / 12f) + N * 5.2f; route.Add(ctx.OnGround(p.x, p.z) + up * .16f); }
            Folk(route[0], E, Resident.Pose.Walk, new[] { "Down to the river and back. Best walk in the Lowlands and it's flat the whole way.", "I'm not window shopping. I'm window appreciating." }, null, route.ToArray()).speed = 1f;
            for (var k = 0; k < 3; k++) { var d = StreetFrom + 23f + k * 44f; var bp = ctx.OnGround(c.x + E.x * d + N.x * (StreetW / 2f + .5f) * (k % 2 == 0 ? -1f : 1f), c.z + E.z * d + N.z * (StreetW / 2f + .5f) * (k % 2 == 0 ? -1f : 1f)); TownBench(mk, bp + up * .1f, N * (k % 2 == 0 ? 1f : -1f)); }
            for (var d = StreetFrom; d <= StreetTo; d += 12f) Mark(ctx.OnGround(c.x + E.x * d, c.z + E.z * d), 17f);
            Shot(ctx, "main street", ctx.OnGround(c.x + E.x * 30f, c.z + E.z * 30f) + up * 2f, ctx.OnGround(c.x + E.x * 110f, c.z + E.z * 110f) + up * 3f);
        }

        /// A shopfront you can't go into: a painted wooden building with a tall false front, an awning, lit windows.
        void Facade(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 g, Vector3 face, float W, float H, string name, string under, int colour, bool upstairs)
        {
            var up = Vector3.up; var f = new PlaceFrame(g + up * .3f, face); const float D = 8f; var paint = Paint(colour); var trim = white;
            mk.Box(brickDark, g - up * .2f, new Vector3(W + .3f, 1.1f, D + .3f), f.r); mk.Box(paint, f.P(0, H / 2f, 0), new Vector3(W, H, D), f.r); solid.Box(paint, f.P(0, H / 2f - .5f, 0), new Vector3(W, H + 1f, D), f.r);
            mk.Box(paint, f.P(0, H + .8f, D / 2f - .2f), new Vector3(W, 1.6f, .4f), f.r); mk.Box(trim, f.P(0, H + 1.65f, D / 2f - .15f), new Vector3(W + .3f, .14f, .6f), f.r); mk.Box(trim, f.P(0, 3.45f, D / 2f + .03f), new Vector3(W, .14f, .1f), f.r);
            mk.Box(WorldMaterials.Solid(new Color(.35f, .22f, .18f)), f.P(0, 1.1f, D / 2f + .03f), new Vector3(1.1f, 2.2f, .08f), f.r); foreach (var e in new[] { -1f, 1f }) { mk.Box(windowM, f.P(e * (W / 4f + .3f), 1.7f, D / 2f + .03f), new Vector3(W / 2f - 2f, 1.9f, .06f), f.r); mk.Box(trim, f.P(e * (W / 4f + .3f), 1.7f, D / 2f + .05f), new Vector3(.08f, 1.9f, .06f), f.r); halos.Add(f.P(e * (W / 4f + .3f), 1.7f, D / 2f + .8f), 1.8f, Gold, .3f); }
            if (upstairs) for (var k = 0; k < 3; k++) { mk.Box(k == 1 ? (Material)windowM : WorldMaterials.Solid(new Color(.3f, .34f, .4f), .7f), f.P(-W / 3.2f + k * W / 3.2f, 5.3f, D / 2f + .03f), new Vector3(1.3f, 1.7f, .06f), f.r); mk.Box(trim, f.P(-W / 3.2f + k * W / 3.2f, 4.38f, D / 2f + .06f), new Vector3(1.6f, .1f, .16f), f.r); }
            var stripe = WorldMaterials.Solid(Color.Lerp(Pastel[(colour + 3) % Pastel.Length], Velvet, .5f)); var n = Mathf.RoundToInt(W / .9f);
            for (var s = 0; s < n; s++) mk.Box(s % 2 == 0 ? stripe : trim, f.P(-W / 2f + (s + .5f) * W / n, 3.05f, D / 2f + .85f), new Vector3(W / n, .06f, 1.8f), f.r * Quaternion.Euler(20f, 0, 0));
            Sign(f.P(0, H + .95f, D / 2f + .02f), f.Fwd, name, Mathf.Min(.42f, W * .5f / Mathf.Max(6, name.Length)), Ink); Sign(f.P(0, H + .4f, D / 2f + .02f), f.Fwd, under, .15f, Ink);
        }

        void RecordShop(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 g, Vector3 face)
        {
            var up = Vector3.up; var fo = new PlaceFrame(g + up * .3f, face); const float W = 12f, D = 9f, H = 3.7f, T = .3f, Door = 1.9f, dx = -3.4f; var paint = Paint(0); var lino = WorldMaterials.Solid(new Color(.3f, .25f, .22f));
            Room(mk, solid, fo, W, D, H, T, dx, Door, paint, lino, brickDark);
            mk.Box(paint, fo.P(0, H + 1f, D / 2f - .2f), new Vector3(W, 1.8f, .4f), fo.r); mk.Box(white, fo.P(0, H + 1.95f, D / 2f - .15f), new Vector3(W + .3f, .14f, .6f), fo.r); Sign(fo.P(0, H + 1.15f, D / 2f + .02f), fo.Fwd, "SWEET SOUL RECORDS", .42f, Velvet); Sign(fo.P(0, H + .55f, D / 2f + .02f), fo.Fwd, "45s  ·  LPs  ·  listen before you buy", .15f, Ink);
            // the big window: a glowing pane with a giant record in it
            mk.Box(windowM, fo.P(1.8f, 1.9f, D / 2f - T / 2f), new Vector3(6.4f, 2f, T + .04f), fo.r); mk.Prism(dark, fo.P(1.8f, 1.9f, D / 2f + .02f), fo.Fwd, .85f, .03f, 20); mk.Prism(velvet, fo.P(1.8f, 1.9f, D / 2f + .04f), fo.Fwd, .3f, .03f, 14); halos.Add(fo.P(1.8f, 1.9f, D / 2f + 1f), 2.6f, Gold, .35f);
            // bins of records down both walls and the middle; posters; the counter
            foreach (var (x, len) in new[] { (-W / 2f + T + .45f, 4.6f), (W / 2f - T - .45f, 6.6f), (.6f, 4.2f) })
            {
                var bc = fo.P(x, .5f, x > 0 && x < 1 ? -.2f : -.6f); mk.Box(wood, bc, new Vector3(.8f, 1f, len), fo.r); Col(bc, new Vector3(.8f, 1f, len), fo.r);
                for (var k = 0; k < Mathf.RoundToInt(len / .14f); k++) mk.Box(Paint(k * 7 + (int)(x * 3)), bc + up * .68f + fo.Fwd * (-len / 2f + .1f + k * .14f), new Vector3(.66f, .34f, .02f), fo.r * Quaternion.Euler(-8f, 0, 0));
            }
            for (var k = 0; k < 4; k++) { mk.Box(Paint(k + 2), fo.P(-3.6f + k * 2.4f, 2.4f, -D / 2f + T + .03f), new Vector3(1.3f, 1.3f, .04f), fo.r); mk.Prism(dark, fo.P(-3.6f + k * 2.4f, 2.4f, -D / 2f + T + .06f), fo.Fwd, .42f, .02f, 14); }
            var ct = fo.P(-3.2f, 0, -D / 2f + T + 1.5f); mk.Box(woodDark, ct + up * .5f, new Vector3(2.6f, 1f, .8f), fo.r); Col(ct + up * .5f, new Vector3(2.6f, 1f, .8f), fo.r);
            var keeper = Best0(); keeper.top = new Color(.85f, .3f, .35f); Folk(ct - fo.Fwd * .9f, fo.Fwd, Resident.Pose.Stand, new[] { "If you can hum it I can find it. If you can't hum it, describe how it made you feel.", "That one? Take it. Bring it back when you know all the words.", "B-sides are in the back. The B-sides are where the truth is." }, keeper);
            // the listening post: a turntable you can drop the needle on
            var tt = fo.P(3.4f, 0, -D / 2f + T + 1.1f); mk.Box(woodDark, tt + up * .45f, new Vector3(1.4f, .9f, .8f), fo.r); Col(tt + up * .45f, new Vector3(1.4f, .9f, .8f), fo.r);
            var tm = new MeshKit(); tm.Prism(dark, Vector3.zero, Vector3.up, .34f, .02f, 20); tm.Prism(velvet, new Vector3(0, .012f, 0), Vector3.up, .11f, .012f, 12); tm.Box(gold, new Vector3(.2f, .03f, 0), new Vector3(.03f, .01f, .12f), Quaternion.identity);
            var tgo = tm.BuildSingle("Record on the turntable", tlife.transform, false); tgo.transform.position = tt + up * .93f; tlife.record = tgo.transform;
            string[] needle = { "The needle drops. A crackle, a count-in, and the room gets warmer.", "Side two. The one nobody plays, and it's the best one.", "It skips right on the good bit. The shopkeeper taps the counter and it carries on.", "Horns. Then that voice. You stop pretending to browse." }; var nn = 0;
            Interact.Toy(tgo, Toy.Kind.Custom, "Drop the needle", new Vector3(1f, .6f, 1f)).onUse = _ => { tlife.needle = Time.time; Say(tlife.transform, tt + up * 2.2f, needle[nn++ % needle.Length], 5f); };
            for (var k = 0; k < 5; k++) { var rm = new MeshKit(); rm.Box(Paint(k + 3), new Vector3(0, .005f, 0), new Vector3(.32f, .01f, .32f), Quaternion.identity); rm.Prism(dark, new Vector3(0, .012f, 0), Vector3.up, .12f, .004f, 12); Thing(rm, "Record", k == 0 ? "a 45 with no label (the good one)" : "a record", ct + up * (1.02f + k * .012f) + fo.Right * (.6f - k * .02f), fo.r * Quaternion.Euler(0, k * 9f, 0), new Vector3(.32f, .03f, .32f), .12f); }
            Folk(fo.P(.6f, 0, 2.6f), -fo.Fwd, Resident.Pose.Stand, new[] { "I came in for one record in 1968. I'm still looking through the Ms.", "You ever hold a record and just know? No? Keep flipping." });
            ArtistBits.PointLight(ctx.Root, fo.P(0, H - .7f, 0), new Color(1f, .85f, .6f), 13f, 1f, true); Mark(g, 9f);
            Shot(ctx, "inside the record shop", fo.P(dx, 1.7f, D / 2f - 1f), fo.P(2.4f, 1.1f, -D / 2f + 1.5f));
        }

        void Barbershop(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 g, Vector3 face)
        {
            var up = Vector3.up; var fo = new PlaceFrame(g + up * .3f, face); const float W = 9f, D = 7f, H = 3.5f, T = .3f, Door = 1.9f, dx = 2.6f; var tile = WorldMaterials.Solid(new Color(.85f, .85f, .8f));
            Room(mk, solid, fo, W, D, H, T, dx, Door, white, tile, brickDark);
            mk.Box(white, fo.P(0, H + .9f, D / 2f - .2f), new Vector3(W, 1.6f, .4f), fo.r); mk.Box(velvet, fo.P(0, H + 1.75f, D / 2f - .15f), new Vector3(W + .3f, .14f, .6f), fo.r); Sign(fo.P(0, H + 1f, D / 2f + .02f), fo.Fwd, "DELUXE BARBERSHOP", .36f, Ink); Sign(fo.P(0, H + .5f, D / 2f + .02f), fo.Fwd, "cuts  ·  shaves  ·  harmony", .15f, Velvet);
            mk.Box(windowM, fo.P(-1.9f, 1.9f, D / 2f - T / 2f), new Vector3(4.2f, 1.9f, T + .04f), fo.r); halos.Add(fo.P(-1.9f, 1.9f, D / 2f + .9f), 2f, Gold, .3f);
            // the pole outside: red, white and blue, turning
            var pp = fo.P(W / 2f - .3f, 0, D / 2f + .35f); mk.Prism(iron, pp, up, .1f, .9f, 8); var pm = new MeshKit(); pm.Prism(white, Vector3.zero, Vector3.up, .13f, 1.1f, 10); for (var k = 0; k < 6; k++) pm.Box(k % 2 == 0 ? WorldMaterials.Solid(new Color(.8f, .15f, .2f)) : WorldMaterials.Solid(new Color(.2f, .35f, .75f)), new Vector3(Mathf.Cos(k * 1.05f) * .12f, .1f + k * .17f, Mathf.Sin(k * 1.05f) * .12f), new Vector3(.06f, .3f, .03f), Quaternion.Euler(0, -k * 60f, 30f)); pm.Blob(lampM, new Vector3(0, 1.22f, 0), Vector3.one * .15f, prng, 0f);
            var pgo = pm.BuildSingle("Barber pole", tlife.transform, false); pgo.transform.position = pp + up * .9f; tlife.pole = pgo.transform; ArtistBits.PostCollider(ctx.Root, pp, 2f, .15f);
            // inside: a mirror wall, two chairs to sit in, the waiting bench
            mk.Box(WorldMaterials.Glow(new Color(.8f, .9f, 1f), .5f), fo.P(-W / 2f + T + .03f, 1.9f, -.4f), new Vector3(.04f, 1.6f, 4.6f), fo.r); mk.Box(woodDark, fo.P(-W / 2f + T + .3f, .95f, -.4f), new Vector3(.5f, .08f, 4.8f), fo.r);
            for (var k = 0; k < 2; k++)
            {
                var ch = fo.P(-W / 2f + T + 1.8f, 0, -1.6f + k * 2.4f); mk.Prism(iron, ch, up, .3f, .1f, 10); mk.Prism(iron, ch, up, .07f, .5f, 6); mk.Box(velvet, ch + up * .58f, new Vector3(.7f, .16f, .7f), fo.r); mk.Box(velvet, ch + up * 1.05f + fo.Right * .32f, new Vector3(.12f, .9f, .7f), fo.r); foreach (var e in new[] { -1f, 1f }) mk.Box(iron, ch + up * .82f + fo.Fwd * e * .38f, new Vector3(.6f, .06f, .08f), fo.r);
                Col(ch + up * .5f, new Vector3(.8f, 1f, .8f), fo.r); Interact.Seat(tlife.transform, ch + up * .68f, -fo.Right, "Sit in the barber's chair");
            }
            var barber = Best0(Residents.Tool.None, true); barber.top = white.color; Folk(fo.P(-W / 2f + T + 2.9f, 0, -.4f), -fo.Right, Resident.Pose.Work, new[] { "Sit, sit. Little off the top, and you tell me everything.", "I've cut every head on Main Street. I know who sings flat. I'll never tell.", "A haircut's ten minutes. The conversation's an hour. That's what you're paying for." }, barber).workRate = 1.2f;
            var wb = fo.P(W / 2f - T - .35f, 0, -1.4f); mk.Box(wood, wb + up * .45f, new Vector3(.5f, .08f, 3f), fo.r); mk.Box(wood, wb + up * .25f, new Vector3(.4f, .5f, 2.8f), fo.r); Col(wb + up * .3f, new Vector3(.5f, .6f, 3f), fo.r); Interact.Bench(tlife.transform, wb + up * .52f - fo.Fwd * 1f, wb + up * .52f + fo.Fwd * 1f, -fo.Right);
            Folk(wb + up * .52f - fo.Fwd * 1f, -fo.Right, Resident.Pose.Sit, new[] { "I don't even need a cut. I come for the news.", "Four of us in here on a Saturday and you've got a quartet whether you want one or not." }, null, null, .52f);
            ArtistBits.PointLight(ctx.Root, fo.P(0, H - .7f, 0), new Color(1f, .92f, .75f), 11f, .8f, true); Mark(g, 8f);
            Shot(ctx, "the barbershop", fo.P(dx, 1.7f, D / 2f - .9f), fo.P(-W / 2f + 1.5f, 1.1f, -1f));
        }

        void Diner(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 g, Vector3 face)
        {
            var up = Vector3.up; var fo = new PlaceFrame(g + up * .3f, face); const float W = 14f, D = 8f, H = 3.5f, T = .3f, Door = 1.9f, dx = 4.6f; var paint = Paint(3); var chk = WorldMaterials.Solid(new Color(.2f, .2f, .22f));
            Room(mk, solid, fo, W, D, H, T, dx, Door, paint, cream, brickDark);
            for (var x = 0; x < 13; x++) for (var z = 0; z < 7; z++) if ((x + z) % 2 == 0) mk.Box(chk, fo.P(-W / 2f + T + .52f + x * 1.03f, .005f, -D / 2f + T + .53f + z * 1.05f), new Vector3(1.03f, .012f, 1.05f), fo.r);
            mk.Box(velvet, fo.P(0, H + .9f, D / 2f - .2f), new Vector3(W, 1.6f, .4f), fo.r); mk.Box(gold, fo.P(0, H + 1.75f, D / 2f - .15f), new Vector3(W + .3f, .14f, .6f), fo.r); Sign(fo.P(0, H + 1f, D / 2f + .02f), fo.Fwd, "THE LUNCHEONETTE", .44f, Cream); Sign(fo.P(0, H + .48f, D / 2f + .02f), fo.Fwd, "pie  ·  coffee  ·  the jukebox takes requests", .15f, Gold);
            mk.Box(windowM, fo.P(-2.4f, 1.9f, D / 2f - T / 2f), new Vector3(8.6f, 1.8f, T + .04f), fo.r); foreach (var q in new[] { -2.15f, 0f, 2.15f }) mk.Box(white, fo.P(-2.4f + q, 1.9f, D / 2f + .01f), new Vector3(.08f, 1.8f, T + .1f), fo.r); halos.Add(fo.P(-2.4f, 1.9f, D / 2f + 1f), 3f, Gold, .35f);
            // the counter along the back with stools, the cook behind it, pie under glass
            var cz = -D / 2f + T + 1.9f; mk.Box(velvet, fo.P(-1.4f, .5f, cz), new Vector3(9.4f, 1f, .7f), fo.r); mk.Box(white, fo.P(-1.4f, 1.03f, cz), new Vector3(9.6f, .06f, .9f), fo.r); Col(fo.P(-1.4f, .55f, cz), new Vector3(9.6f, 1.1f, .9f), fo.r);
            for (var k = 0; k < 6; k++) { var st = fo.P(-5.4f + k * 1.6f, 0, cz + 1f); mk.Prism(iron, st, up, .05f, .66f, 6); mk.Prism(velvet, st + up * .66f, up, .24f, .1f, 12); ArtistBits.PostCollider(ctx.Root, st, .7f, .2f); Interact.Seat(tlife.transform, st + up * .78f, -fo.Fwd, "Sit at the counter").seatHeight = .78f; }
            var cook = Best0(Residents.Tool.Spatula, true); cook.top = white.color; cook.hat = Residents.Hat.Cap; cook.hatColor = white.color; Folk(fo.P(-2.2f, 0, cz - 1f), fo.Fwd, Resident.Pose.Work, new[] { "Pie's sweet potato, pecan, or both. Say both.", "Coffee's been on since 1962. Same pot. It's got character.", "You want the special? The special is you sit down and I feed you." }, cook).workRate = 1f;
            Folk(fo.P(-5.4f, .78f, cz + 1f), -fo.Fwd, Resident.Pose.Sit, new[] { "I'll have what the jukebox is having.", "Third cup. I'm not tired, I just don't want to leave." }, Best0(Residents.Tool.Mug), null, .78f);
            mk.Box(WorldMaterials.Solid(new Color(.6f, .62f, .66f), .8f), fo.P(2.6f, 1.2f, -D / 2f + T + .35f), new Vector3(2.4f, 2.2f, .6f), fo.r); mk.Box(GlowM(new Color(1f, .6f, .3f), 1f), fo.P(2.6f, 1.3f, -D / 2f + T + .67f), new Vector3(1.4f, .5f, .03f), fo.r);
            for (var k = 0; k < 3; k++) { var pm = new MeshKit(); pm.Prism(WorldMaterials.Solid(new Color(.85f, .55f, .25f)), Vector3.zero, Vector3.up, .15f, .05f, 3); pm.Prism(white, new Vector3(0, -.012f, 0), Vector3.up, .2f, .012f, 10); Thing(pm, "Pie", k == 0 ? "a slice of sweet potato pie" : "a slice of pecan pie", fo.P(-3.8f + k * 1.6f, 1.08f, cz + .1f), fo.r * Quaternion.Euler(0, k * 40f, 0), new Vector3(.4f, .08f, .4f), .2f, false); }
            for (var k = 0; k < 2; k++) { var cm = new MeshKit(); cm.Prism(white, Vector3.zero, Vector3.up, .045f, .09f, 8, 1.15f); cm.Prism(WorldMaterials.Solid(new Color(.25f, .15f, .1f)), new Vector3(0, .08f, 0), Vector3.up, .04f, .012f, 8); Thing(cm, "Coffee", "a cup of coffee", fo.P(-4.6f + k * 3.2f, 1.07f, cz + .2f), fo.r, new Vector3(.12f, .1f, .12f), .15f, false); }
            // two booths under the window, a jukebox by the door
            for (var k = 0; k < 2; k++)
            {
                var bx = -5f + k * 3.6f; var bz = D / 2f - T - 1.1f; mk.Box(white, fo.P(bx, .75f, bz), new Vector3(1.1f, .06f, 1.5f), fo.r); mk.Prism(iron, fo.P(bx, 0, bz), up, .07f, .75f, 6); Col(fo.P(bx, .4f, bz), new Vector3(1.1f, .8f, 1.5f), fo.r);
                foreach (var e in new[] { -1f, 1f }) { mk.Box(velvet, fo.P(bx + e * 1.05f, .25f, bz), new Vector3(.6f, .5f, 1.5f), fo.r); mk.Box(velvet, fo.P(bx + e * 1.3f, .8f, bz), new Vector3(.14f, 1.1f, 1.5f), fo.r); Col(fo.P(bx + e * 1.1f, .5f, bz), new Vector3(.7f, 1f, 1.5f), fo.r); Interact.Bench(tlife.transform, fo.P(bx + e * 1f, .52f, bz - .35f), fo.P(bx + e * 1f, .52f, bz + .35f), -fo.Right * e); }
            }
            Folk(fo.P(-5f - 1f, .52f, D / 2f - T - 1.1f + .35f), fo.Right, Resident.Pose.Sit, new[] { "We've had this booth since our first date. That was forty years and one milkshake ago.", "Two straws. Always two straws." }, null, null, .52f);
            var jb = fo.P(W / 2f - T - .55f, 0, 1.2f); var jm = new MeshKit(); jm.Box(WorldMaterials.Solid(new Color(.5f, .22f, .16f)), new Vector3(0, .65f, 0), new Vector3(.8f, 1.3f, .6f), Quaternion.identity); jm.Prism(WorldMaterials.Solid(new Color(.5f, .22f, .16f)), new Vector3(0, 1.3f, -.3f), Vector3.forward, .4f, .6f, 12); var jg = new Material(GlowM(Gold, 1.6f)) { name = "Luncheonette jukebox" }; jm.Box(jg, new Vector3(0, 1.1f, .31f), new Vector3(.6f, .5f, .02f), Quaternion.identity); jm.Box(GlowM(new Color(.4f, .8f, 1f), 1.2f), new Vector3(0, .5f, .31f), new Vector3(.5f, .4f, .02f), Quaternion.identity);
            var jgo = jm.BuildSingle("Jukebox", tlife.transform, false); jgo.transform.SetPositionAndRotation(jb, Quaternion.LookRotation(-fo.Right)); Col(jb + up * .8f, new Vector3(.9f, 1.6f, .7f), fo.r); tlife.jukeM = jg; halos.Add(jb + up * 1.1f - fo.Right * .6f, .9f, Gold, .4f);
            string[] juke = { "A nickel drops. A bassline walks in like it owns the place.", "It plays the slow one. The cook turns the grill down so everybody can hear.", "B-7. It's always B-7. The whole counter sings the second verse.", "It plays one you forgot you knew every word to." }; var jn = 0;
            Interact.Toy(jgo, Toy.Kind.Custom, "Pick a song on the jukebox", new Vector3(1.2f, 1.8f, 1.2f)).onUse = _ => { tlife.juke = Time.time; Say(tlife.transform, jb + up * 2.3f, juke[jn++ % juke.Length], 4.5f); };
            ArtistBits.PointLight(ctx.Root, fo.P(-1f, H - .7f, 0), new Color(1f, .88f, .65f), 14f, 1f, true); Mark(g, 9f);
            Shot(ctx, "inside the luncheonette", fo.P(dx, 1.7f, D / 2f - 1f), fo.P(-3f, 1f, -D / 2f + 2f));
        }

        void RadioStation(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 g, Vector3 face)
        {
            var up = Vector3.up; var f = new PlaceFrame(g + up * .3f, face); const float W = 12f, D = 8f, H = 5f;
            Facade(ctx, mk, solid, g, face, W, H, "W-S-O-L  RADIO", "the sound of the Lowlands", 2, false);
            // the mast on the roof with its red light, ON AIR over the door, a horn speaker to listen at
            var mast = f.P(-3.5f, H, -1f); mk.Prism(iron, mast, up, .08f, 9f, 4); for (var k = 0; k < 4; k++) mk.Prism(iron, mast + up * (2f + k * 2f), (f.Right + up * .2f).normalized, .03f, 1.2f - k * .2f, 3); mk.Blob(GlowM(new Color(1f, .15f, .1f), 2.4f), mast + up * 9.1f, Vector3.one * .16f, prng, 0f); halos.Add(mast + up * 9.1f, 1.4f, new Color(1f, .2f, .15f), .6f);
            var onAir = new Material(GlowM(new Color(1f, .2f, .15f), 2f)) { name = "On air" }; tlife.onAir = onAir; mk.Box(dark, f.P(0, 2.75f, D / 2f + .06f), new Vector3(1.5f, .5f, .1f), f.r); mk.Box(onAir, f.P(0, 2.75f, D / 2f + .12f), new Vector3(1.3f, .34f, .02f), f.r); Sign(f.P(0, 2.75f, D / 2f + .14f), f.Fwd, "ON AIR", .18f, Cream);
            var sp = f.P(4.6f, 2.4f, D / 2f + .3f); var sm = new MeshKit(); sm.Cone(WorldMaterials.Solid(new Color(.6f, .62f, .66f), .7f), new Vector3(0, 0, .5f), .42f, -.6f, 12); sm.Box(iron, new Vector3(0, 0, -.12f), new Vector3(.16f, .16f, .3f), Quaternion.identity);
            var sgo = sm.BuildSingle("Radio horn", tlife.transform, false); sgo.transform.SetPositionAndRotation(sp, f.r * Quaternion.Euler(15f, 0, 0));
            string[] radio = { "\"W-S-O-L, eleven-sixty on your dial, and that was a record so good I'm playing it again.\"", "\"Dedication time: this one goes out to whoever's standing outside the station right now. Yes, you.\"", "\"The weather: golden. The forecast: golden. Here's the horn section with the details.\"", "\"Caller, you're on the air.\" \"I just wanted to say I love everybody.\" \"That's the format, baby.\"" }; var rn = 0;
            Interact.Toy(sgo, Toy.Kind.Custom, "Listen to the radio", new Vector3(1.4f, 1.6f, 1.6f)).onUse = _ => { tlife.radio = Time.time; Say(tlife.transform, sp + up * 1.2f, radio[rn++ % radio.Length], 5.5f); };
            Folk(ctx.OnGround(f.P(2.8f, 0, D / 2f + 1f).x, f.P(2.8f, 0, D / 2f + 1f).z) + up * .1f, f.Fwd, Resident.Pose.Lean, new[] { "I'm the night man. Also the day man. Station's just me and four hundred records.", "I put the speaker outside so Main Street never has to go without.", "Requests? Slip it under the door. If it's good I'll say your name on the air." });
        }

        // ---------------------------------------------------------------- River Road and the landing
        void RiverRoad(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var c = new Vector3(center.x, 0, center.z);
            // walk east from the end of Main Street until the water's edge
            var endD = StreetTo; while (endD < riverDist + 30f) { var q = c + E * (endD + 2f); if (!ctx.IsLand(q.x, q.z, 0f) || ctx.Ground(q.x, q.z) < waterY + 1.7f) break; endD += 2f; }
            var pts = new List<Vector3>(); for (var d = StreetTo; d <= endD; d += 2f) pts.Add(ctx.OnGround(c.x + E.x * d, c.z + E.z * d)); if (pts.Count < 2) return;
            Strip(mk, pts, brick, 5.5f, .16f, ctx); foreach (var p in pts) Remember(p, 4);
            var trng = new System.Random(77); var k0 = 0;
            for (var d = StreetTo + 12f; d < endD - 14f; d += 17f, k0++) foreach (var e in new[] { -1f, 1f })
            {
                var tp = ctx.OnGround(c.x + E.x * d + N.x * e * 6.5f, c.z + E.z * d + N.z * e * 6.5f); if (!ctx.IsLand(tp.x, tp.z, 2f)) continue; Oak(ctx, mk, tp, 1.15f, trng); Mark(tp, 2f);
                if (k0 % 2 == (e > 0 ? 0 : 1)) { var lp = ctx.OnGround(c.x + E.x * (d + 8f) + N.x * e * 3.6f, c.z + E.z * (d + 8f) + N.z * e * 3.6f); GlobeLamp(ctx, mk, lp); }
            }
            var mid = (StreetTo + endD) / 2f; TownBench(mk, ctx.OnGround(c.x + E.x * mid + N.x * 3.6f, c.z + E.z * mid + N.z * 3.6f), -N);
            if (!hasRiver) return;

            // the landing: a boardwalk out over the water, a ticket shack, lamps, and the Lowlands Belle alongside
            var L = ctx.OnGround(c.x + E.x * endD, c.z + E.z * endD); var deckY = Mathf.Max(L.y, waterY + 1.5f) + .15f; var o = new Vector3(L.x, deckY, L.z); var f = new PlaceFrame(o, E); var rot = f.r;   // f.Fwd = out over the water, f.Right = along the shore
            const float DL = 30f, DW = 9f;
            mk.Box(boardM, f.P(0, -.1f, DW / 2f - 1f), new Vector3(DL, .2f, DW + 2f), rot); solid.Box(boardM, f.P(0, -.6f, DW / 2f - 1f), new Vector3(DL, 1.2f, DW + 2f), rot);
            for (var k = 0; k < 30; k++) mk.Box(woodDark, f.P(-DL / 2f + .5f + k * 1f, .005f, DW / 2f - 1f), new Vector3(.04f, .01f, DW + 2f), rot);
            for (var k = 0; k <= 6; k++) foreach (var z in new[] { 1.5f, DW - .4f }) mk.Prism(woodDark, f.P(-DL / 2f + .4f + k * (DL - .8f) / 6f, -4f, z), up, .22f, 4f, 7);
            // rails on the water side, open where the gangway is; rails at both ends
            foreach (var (x0, x1) in new[] { (-DL / 2f, -3.2f), (3.2f, DL / 2f) }) { mk.Box(white, f.P((x0 + x1) / 2f, 1f, DW - .1f), new Vector3(x1 - x0, .08f, .1f), rot); for (var x = x0; x <= x1 + .01f; x += (x1 - x0) / 6f) mk.Box(white, f.P(x, .5f, DW - .1f), new Vector3(.1f, 1f, .1f), rot); Col(f.P((x0 + x1) / 2f, .55f, DW - .1f), new Vector3(x1 - x0, 1.1f, .14f), rot); }
            foreach (var e in new[] { -1f, 1f }) { mk.Box(white, f.P(e * (DL / 2f - .05f), 1f, DW / 2f + .6f), new Vector3(.1f, .08f, DW - 1.4f), rot); for (var k = 0; k < 4; k++) mk.Box(white, f.P(e * (DL / 2f - .05f), .5f, 1.8f + k * 2.3f), new Vector3(.1f, 1f, .1f), rot); Col(f.P(e * (DL / 2f - .05f), .55f, DW / 2f + .6f), new Vector3(.14f, 1.1f, DW - 1.4f), rot); }
            var tops = new List<Vector3>(); foreach (var x in new[] { -DL / 2f + 1f, -5f, 5f, DL / 2f - 1f }) { GlobeLamp(ctx, mk, f.P(x, 0, .6f), x < -5.5f || x > 5.5f); tops.Add(f.P(x, 4.3f, .6f)); }
            for (var k = 0; k + 1 < tops.Count; k++) BulbString(mk, tops[k], tops[k + 1], k);
            var sh = f.P(-9.5f, 0, 2.6f); mk.Box(Paint(2), sh + up * 1.3f, new Vector3(2.6f, 2.6f, 2.2f), rot); mk.Box(velvet, sh + up * 2.75f, new Vector3(3.2f, .16f, 2.8f), rot); mk.Box(dark, sh + up * 1.6f + f.Right * 1.31f, new Vector3(.04f, 1f, 1.4f), rot); Col(sh + up * 1.3f, new Vector3(2.6f, 2.6f, 2.2f), rot);
            Sign(sh + up * 2.3f + f.Right * 1.32f, f.Right, "RIVERBOAT", .2f, Ink); Sign(sh + up * .7f + f.Right * 1.32f, f.Right, "all aboard\nany time", .12f, Ink);
            Folk(sh + f.Right * 2.2f + up * .02f, f.Right, Resident.Pose.Stand, new[] { "The Belle doesn't go anywhere. She doesn't need to. The river comes to her.", "Walk aboard, pull the whistle. Everybody pulls the whistle.", "Evenings, the band sets up on the top deck and the whole river hears it." });
            TownBench(mk, f.P(8f, .02f, 1.4f), f.Fwd); TownBench(mk, f.P(11.5f, .02f, 1.4f), f.Fwd);
            Interact.Seat(tlife.transform, f.P(12.5f, .02f, DW + .7f), f.Fwd, "Sit on the edge of the landing"); var fisher = Best0(Residents.Tool.Rod); fisher.hat = Residents.Hat.Straw; Folk(f.P(-12.5f, .02f, DW + .7f), f.Fwd, Resident.Pose.Sit, new[] { "Catfish don't bite when the horns are playing. They're listening.", "I've got a line in the water and nowhere to be. That's wealth." }, fisher, null, .02f);
            Mark(L, 16f);

            // the Lowlands Belle: white, two decks, twin stacks, a red stern wheel that turns
            var b = new PlaceFrame(f.P(0, 0, DW + 1.4f + 4.5f), f.Right);   // b.Fwd = her bow (north along the shore), b.Right = toward the shore
            const float BL = 32f, BW = 9f; var red = WorldMaterials.Solid(new Color(.75f, .16f, .18f));
            mk.Box(white, b.P(0, -.8f, 0), new Vector3(BW, 1.6f, BL), b.r); solid.Box(white, b.P(0, -.8f, 0), new Vector3(BW, 1.6f, BL), b.r); mk.Box(red, b.P(0, -1.3f, 0), new Vector3(BW + .1f, .3f, BL + .1f), b.r); mk.Prism(white, b.P(0, -1.6f, BL / 2f), up, BW / 2f, 1.6f, 3, 1f, 30f);
            mk.Box(boardM, b.P(0, .02f, 0), new Vector3(BW - .2f, .04f, BL - .2f), b.r);
            // the gangway
            mk.Box(boardM, f.P(0, -.05f, DW + .7f), new Vector3(3f, .12f, 2.2f), rot); solid.Box(boardM, f.P(0, -.3f, DW + .7f), new Vector3(3f, .6f, 2.2f), rot); foreach (var e in new[] { -1.5f, 1.5f }) { mk.Box(white, f.P(e, .9f, DW + .7f), new Vector3(.08f, .08f, 2.2f), rot); Col(f.P(e, .5f, DW + .7f), new Vector3(.1f, 1f, 2.2f), rot); }
            // the cabin with lit windows, the upper deck on posts, the pilot house, the stacks
            const float CL = 17f, CW = 4.6f, CH = 3f; mk.Box(white, b.P(0, CH / 2f, 1f), new Vector3(CW, CH, CL), b.r); solid.Box(white, b.P(0, CH / 2f, 1f), new Vector3(CW, CH, CL), b.r);
            for (var k = 0; k < 7; k++) foreach (var e in new[] { -1f, 1f }) mk.Box(windowM, b.P(e * (CW / 2f + .02f), 1.7f, -6f + k * 2.3f), new Vector3(.05f, 1.1f, 1.3f), b.r);
            mk.Box(white, b.P(0, CH + .1f, 1f), new Vector3(BW - .4f, .2f, CL + 5f), b.r); solid.Box(white, b.P(0, CH + .1f, 1f), new Vector3(BW - .4f, .2f, CL + 5f), b.r);
            for (var k = 0; k < 6; k++) foreach (var e in new[] { -1f, 1f }) { var pp = b.P(e * (BW / 2f - .5f), 0, -9.5f + k * 4.2f); mk.Box(white, pp + up * CH / 2f, new Vector3(.12f, CH, .12f), b.r); ArtistBits.PostCollider(ctx.Root, pp, CH, .1f); }
            mk.Box(white, b.P(0, CH + 1.5f, 7f), new Vector3(3.2f, 2.4f, 3.4f), b.r); mk.Box(windowM, b.P(0, CH + 1.8f, 8.72f), new Vector3(2.6f, 1f, .05f), b.r); mk.Box(red, b.P(0, CH + 2.8f, 7f), new Vector3(3.8f, .2f, 4f), b.r);
            foreach (var e in new[] { -1f, 1f }) { var s = b.P(e * 1.2f, CH + .2f, 3.2f); mk.Prism(dark, s, up, .42f, 6.4f, 10); mk.Prism(gold, s + up * 6.2f, up, .56f, .4f, 10); mk.Prism(red, s + up * 4.6f, up, .44f, .3f, 10); }
            for (var k = 0; k < 2; k++) foreach (var e in new[] { -1f, 1f }) BulbString(mk, b.P(e * (BW / 2f - .3f), CH + 1.6f, -10.5f + k * 11.5f), b.P(e * (BW / 2f - .3f), CH + 1.6f, 1f + k * 11.5f), k);
            foreach (var e in new[] { -1f, 1f }) Sign(b.P(e * (BW / 2f + .03f), -.55f, 6f), b.Right * e, "LOWLANDS BELLE", .5f, new Color(.75f, .16f, .18f));
            // rails round the main deck, open at the gangway
            foreach (var (z0, z1) in new[] { (-BL / 2f + .2f, -1.7f), (1.7f, BL / 2f - .2f) }) { mk.Box(white, b.P(BW / 2f - .12f, 1f, (z0 + z1) / 2f), new Vector3(.08f, .08f, z1 - z0), b.r); Col(b.P(BW / 2f - .12f, .55f, (z0 + z1) / 2f), new Vector3(.12f, 1.1f, z1 - z0), b.r); }
            mk.Box(white, b.P(-BW / 2f + .12f, 1f, 0), new Vector3(.08f, .08f, BL - .4f), b.r); Col(b.P(-BW / 2f + .12f, .55f, 0), new Vector3(.12f, 1.1f, BL - .4f), b.r); foreach (var e in new[] { -1f, 1f }) Col(b.P(0, .55f, e * (BL / 2f - .15f)), new Vector3(BW, 1.1f, .12f), b.r);
            for (var k = 0; k <= 16; k++) { mk.Box(white, b.P(-BW / 2f + .12f, .5f, -BL / 2f + .3f + k * (BL - .6f) / 16f), new Vector3(.07f, 1f, .07f), b.r); if (Mathf.Abs(-BL / 2f + .3f + k * (BL - .6f) / 16f) > 1.7f) mk.Box(white, b.P(BW / 2f - .12f, .5f, -BL / 2f + .3f + k * (BL - .6f) / 16f), new Vector3(.07f, 1f, .07f), b.r); }
            // the stern wheel
            var wm = new MeshKit(); wm.Prism(iron, new Vector3(-3.4f, 0, 0), Vector3.right, .16f, 6.8f, 6); for (var k = 0; k < 10; k++) { var q = Quaternion.Euler(k * 36f, 0, 0); wm.Box(red, q * new Vector3(0, 2.3f, 0), new Vector3(6.6f, .9f, .08f), q); foreach (var e in new[] { -3.1f, 0f, 3.1f }) wm.Box(red, q * new Vector3(e, 1.2f, 0), new Vector3(.1f, 2.4f, .1f), q); }
            var wgo = wm.BuildSingle("Stern wheel", tlife.transform, false); wgo.transform.SetPositionAndRotation(b.P(0, .2f, -BL / 2f - 2.9f), b.r); tlife.wheel = wgo.transform; tlife.wheelRot = b.r; Col(b.P(0, .4f, -BL / 2f - 2.9f), new Vector3(7f, 5f, 5.4f), b.r);
            foreach (var e in new[] { -1f, 1f }) mk.Box(white, b.P(e * 3.6f, .3f, -BL / 2f - 1.6f), new Vector3(.3f, .5f, 3.4f), b.r);
            // on deck: the whistle cord, deck chairs at the bow, the captain, a couple dancing
            var wc = b.P(CW / 2f + .25f, 0, 4.4f); var whm = new MeshKit(); whm.Prism(WorldMaterials.Solid(new Color(.75f, .6f, .4f)), new Vector3(0, 1.2f, 0), Vector3.up, .015f, 1.6f, 4); whm.Blob(gold, new Vector3(0, 1.15f, 0), Vector3.one * .07f, prng, 0f);
            var whgo = whm.BuildSingle("Whistle cord", tlife.transform, false); whgo.transform.position = wc; mk.Prism(gold, b.P(1.2f, CH + 6.9f, 3.2f), up, .12f, .6f, 8);
            string[] toot = { "WHOOOO-OOOP! It rolls right down the river. Somebody on Main Street waves without looking.", "WHOOP-WHOOP! The captain tips his cap. \"That's the two-short. Means 'good evening'.\"", "WHOOOOOO... A long one. Every dog in the Lowlands joins in on the harmony." }; var tn = 0;
            Interact.Toy(whgo, Toy.Kind.Custom, "Pull the whistle", new Vector3(.8f, 2.6f, .8f)).onUse = _ => { tlife.whistle = Time.time; Say(tlife.transform, wc + up * 3.4f, toot[tn++ % toot.Length], 5f); };
            foreach (var e in new[] { -1f, 1f }) { var dc = b.P(e * 1.6f, 0, BL / 2f - 3f); mk.Box(Paint(e > 0 ? 1 : 4), dc + up * .4f, new Vector3(.7f, .08f, 1.3f), b.r * Quaternion.Euler(-14f, 0, 0)); mk.Box(wood, dc + up * .2f, new Vector3(.74f, .4f, 1f), b.r); Col(dc + up * .3f, new Vector3(.74f, .6f, 1.3f), b.r); Interact.Seat(tlife.transform, dc + up * .5f, b.Fwd, "Sit in the deck chair").seatHeight = .5f; }
            var cap = Best0(); cap.top = new Color(.2f, .25f, .4f); cap.bottom = white.color; cap.hat = Residents.Hat.Cap; cap.hatColor = white.color; Folk(b.P(2.9f, .04f, 9f), b.Right, Resident.Pose.Stand, new[] { "Welcome aboard the Belle. Mind the wheel, she's got opinions.", "Forty years on this river. It's about a quarter mile long and I know every inch.", "We cast off the minute the song ends. The song doesn't end." }, cap);
            Folk(b.P(-2.9f, .04f, -9f), b.Fwd, Resident.Pose.Wave, new[] { "They say you can't dance on a boat. They've never been on this boat.", "The deck goes up and down in three-four. You get used to it." });
            ArtistBits.PointLight(ctx.Root, b.P(3f, 2.4f, 0), Gold, 16f, 1.6f); ArtistBits.PointLight(ctx.Root, b.P(0, CH + 2.6f, -4f), Gold, 16f, 1.4f);
            tlife.belle = b.P(0, 6f, 0);
            Shot(ctx, "the landing and the riverboat", f.P(-13f, 3f, -7f), b.P(0, 3f, 2f));
            Shot(ctx, "aboard the lowlands belle", b.P(3f, 1.7f, -8f), b.P(1f, 2.4f, 10f));
        }
    }

    /// The town's moving parts: the stern wheel, the barber pole, the turntable, The Royal's spotlight, the chapel's
    /// windows when the organ plays, the jukebox, the ON AIR sign.
    public class SoulTownLife : MonoBehaviour
    {
        public Transform wheel, pole, record; public Quaternion wheelRot; public Light spot; public Material jukeM, onAir; public Vector3 belle;
        public List<(Material m, Color c)> glass = new List<(Material, Color)>(); public float sang = -99f, organ = -99f, needle = -99f, juke = -99f, radio = -99f, whistle = -99f; float turn;

        void Update()
        {
            var t = Time.time; var dt = Time.deltaTime;
            turn += dt * (14f + (t - whistle < 6f ? 30f : 0f)); if (wheel != null) wheel.rotation = wheelRot * Quaternion.Euler(turn, 0, 0);
            if (pole != null) pole.Rotate(0, 60f * dt, 0, Space.World);
            if (record != null && t - needle < 40f) record.Rotate(0, 200f * dt, 0, Space.World);
            if (spot != null) spot.intensity = Mathf.MoveTowards(spot.intensity, t - sang < 12f ? 5f : 1.2f, dt * 4f);
            var o = t - organ < 8f ? 1f + 2.2f * Mathf.Exp(-(t - organ) * .35f) : 1f; foreach (var (m, c) in glass) if (m != null) m.SetColor("_EmissionColor", c * (1.1f * o + .08f * Mathf.Sin(t * 1.3f + c.r * 9f)));
            if (jukeM != null) jukeM.SetColor("_EmissionColor", Color.HSVToRGB(Mathf.Repeat(.08f + .05f * Mathf.Sin(t * .7f), 1f), .7f, 1f) * (t - juke < 5f ? 2.6f : 1.1f));
            if (onAir != null) onAir.SetColor("_EmissionColor", new Color(1f, .2f, .15f) * (t - radio < 6f ? 2.4f + Mathf.Sin(t * 12f) * .6f : 1.3f));
        }
    }
}
