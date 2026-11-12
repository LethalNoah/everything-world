using System.Collections.Generic;
using UnityEngine;
using static EverythingWorld.World.PlaceTools;

namespace EverythingWorld.World
{
    /// Discotopia's places and people (part of DiscoKit): the Floor (the light-up dance floor that is the town square:
    /// a mirror ball over it, a DJ who takes requests, dancers, a white limousine to sit in, things to dress up in),
    /// the Esplanade (a white promenade to the lighthouse: benches facing the sea, a photo booth, an ice-cream cart),
    /// the Lighthouse (the mirror ball that sweeps the whole town with specks of light; a lever at its foot to spin it
    /// faster, a telescope) and the Belvedere (a raised terrace up a grand stair: little tables, a champagne tower to
    /// pour, glasses to carry, the view).
    public partial class DiscoKit
    {
        DiscoTownLife tlife; System.Random prng; Transform rootT; LandContext ctx0;

        static readonly Color[] Sequins = { new Color(.95f, .95f, 1f), new Color(1f, .8f, .3f), new Color(.85f, .88f, .95f), new Color(1f, .5f, .3f), new Color(.5f, .85f, .95f), new Color(.95f, .5f, .7f), new Color(.6f, .9f, .7f) };
        static readonly Color[] Flare = { new Color(.95f, .95f, 1f), new Color(.2f, .22f, .4f), new Color(.9f, .75f, .35f), new Color(.6f, .62f, .7f) };
        static readonly string[] TownTalk =
        {
            "You can dance, you can jive. That's not a suggestion, it's the bylaws.", "The lighthouse doesn't warn ships off. It invites them to the party.", "It's always Saturday night here. Friday was nice but we voted.",
            "Stand still a second and let the light find you. There. Now you're glamorous.", "Somebody's going to start a conga line before the song's out. It's usually me.",
        };
        static readonly string[] VillaTalk =
        {
            "We came for a weekend in 1977. The weekend's going very well.", "You can see the lighthouse from every window. You can't sleep, but why would you?", "Come by for a drink on the terrace. Bring your shoes; you'll want them for after.",
        };

        void TownStart(LandContext ctx)
        {
            rootT = ctx.Root; ctx0 = ctx; prng = new System.Random(1979);
            tlife = new GameObject("Disco town life").AddComponent<DiscoTownLife>(); tlife.transform.SetParent(life.transform, false);
        }

        Residents.Look Glam(Residents.Tool tool = Residents.Tool.None, bool apron = false) { var l = Residents.Random(prng, Sequins, Flare, new[] { Residents.Hat.None, Residents.Hat.None, Residents.Hat.Bucket, Residents.Hat.Cap }, Sequins); l.tool = tool; l.apron = apron; l.longHair = prng.Next(2) == 0; return l; }
        Resident Folk(Vector3 feet, Vector3 face, Resident.Pose pose, string[] lines = null, Residents.Look look = null, Vector3[] route = null, float seatH = HumanScale.SeatHeight)
            => Residents.Make(tlife.transform, feet, face, look ?? Glam(), pose, prng, lines ?? TownTalk, route, seatH);
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

        /// A white board with a gold frame and dark lettering.
        void WhiteSign(MeshKit mk, Vector3 c, Vector3 face, float w, float h, string text, float size, string under = null)
        {
            var rot = Quaternion.LookRotation(face); mk.Box(goldM, c - face * .02f, new Vector3(w + .2f, h + .2f, .1f), rot); mk.Box(marble, c, new Vector3(w, h, .12f), rot);
            Sign(c + face * .07f + Vector3.up * (under != null ? h * .14f : 0f), face, text, size, Ink); if (under != null) Sign(c + face * .07f - Vector3.up * h * .28f, face, under, size * .4f, new Color(.6f, .42f, .1f));
            halos.Add(c + face * .6f, Mathf.Max(w, h) * .5f, Gold, .3f);
        }

        // ---------------------------------------------------------------- the Floor
        void TheFloor(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var c = ctx.OnGround(center.x, center.z); var f = new PlaceFrame(c, toLight);
            mk.Prism(stone, c - up * .28f, up, 29f, .4f, 30); mk.Box(dark, c + up * .1f, new Vector3(FloorHalf * 2f + .8f, .1f, FloorHalf * 2f + .8f), f.r); mk.Box(goldM, c + up * .13f, new Vector3(FloorHalf * 2f + 1.2f, .03f, FloorHalf * 2f + 1.2f), f.r);
            // nine by nine light-up tiles
            for (var x = 0; x < 9; x++) for (var z = 0; z < 9; z++) mk.Box(tileM[(x * 3 + z * 5 + (x * z) % 3) % 4], f.P(-8f + x * 2f, .17f, -8f + z * 2f), new Vector3(1.86f, .04f, 1.86f), f.r);
            // four white arches from the corners to a ring over the middle; the mirror ball hangs from it
            var ring = c + up * 10f; var tops = new List<Vector3>();
            for (var k = 0; k < 4; k++)
            {
                var d = Quaternion.Euler(0, k * 90f + 45f, 0) * f.Fwd; var foot = c + d * 14.5f; var prev = foot;
                for (var s = 1; s <= 8; s++) { var t = s / 8f; var p = Vector3.Lerp(foot, ring, t) + up * Mathf.Sin(t * Mathf.PI) * 2.6f + up * t * 0f; p.y = Mathf.Lerp(foot.y, ring.y, Mathf.Sin(t * Mathf.PI * .5f)); mk.Prism(marble, prev, (p - prev).normalized, .22f, Vector3.Distance(prev, p) + .05f, 6); mk.Blob(tileM[(s + k) % 4], p + up * .28f, Vector3.one * .12f, prng, 0f); prev = p; }
                mk.Prism(marble, foot - up * .2f, up, .6f, .6f, 10); ArtistBits.PostCollider(ctx.Root, foot, 3f, .5f); tops.Add(foot);
            }
            mk.Prism(goldM, ring - up * .1f, up, 1.2f, .3f, 14); mk.Prism(chrome, ring - up * 1.6f, up, .03f, 1.5f, 4);
            var ball = new MeshKit(); var ballM = new Material(WorldMaterials.Glow(Silver, 1.7f)) { name = "The Floor's mirror ball" }; ball.Blob(ballM, Vector3.zero, Vector3.one * 1.3f, prng, .12f); var bgo = ball.BuildSingle("The Floor's mirror ball", tlife.transform, false); bgo.transform.position = ring - up * 2.8f; tlife.floorBall = bgo.transform; life.floorBallM = ballM;
            halos.Add(ring - up * 2.8f, 6f, Silver, .45f); life.floorLight = ArtistBits.PointLight(ctx.Root, ring - up * 4.5f, new Color(1f, .97f, .9f), 34f, 3f);
            // globe lamps round the edge, lights strung between them
            var lampTops = new List<Vector3>(); for (var k = 0; k < 8; k++) { var d = Quaternion.Euler(0, k * 45f, 0) * f.Fwd; var lp = c + d * 25f + up * .13f; GlobeLamp(ctx, mk, lp, true); lampTops.Add(lp + up * 4f); }
            for (var k = 0; k < 8; k++) LightString(mk, lampTops[k], lampTops[(k + 1) % 8], k);
            // the DJ booth on one side: white and gold, two decks, a wall of speakers behind
            var bo = new PlaceFrame(c - sideways * (FloorHalf + 3.2f) + up * .13f, sideways);
            mk.Box(marble, bo.P(0, .6f, 0), new Vector3(3.6f, 1.2f, 1.1f), bo.r); mk.Box(goldM, bo.P(0, 1.22f, 0), new Vector3(3.8f, .06f, 1.3f), bo.r); mk.Box(tileM[0], bo.P(0, .6f, .56f), new Vector3(3.2f, .5f, .02f), bo.r); Col(bo.P(0, .6f, 0), new Vector3(3.8f, 1.2f, 1.3f), bo.r);
            var decks = new MeshKit(); foreach (var e in new[] { -1f, 1f }) { decks.Prism(dark, new Vector3(e * .9f, 0, 0), Vector3.up, .4f, .03f, 16); decks.Prism(goldM, new Vector3(e * .9f, .02f, 0), Vector3.up, .12f, .02f, 10); } decks.Box(chrome, new Vector3(0, .03f, 0), new Vector3(.5f, .06f, .7f), Quaternion.identity);
            var dgo = decks.BuildSingle("The decks", tlife.transform, false); dgo.transform.SetPositionAndRotation(bo.P(0, 1.26f, 0), bo.r);
            string[] req = { "The DJ nods, flips a record without looking, and the whole floor turns gold.", "\"One more? For you: one more.\" It's the one you were hoping for.", "A drum fill, a string swoop, and every tile on the floor lights at once.", "\"This one goes out to the new arrival.\" Everybody turns and cheers. You have to dance now." }; var rn = 0;
            Interact.Toy(dgo, Toy.Kind.Custom, "Ask for one more song", new Vector3(3.6f, 1.2f, 2f)).onUse = _ => { life.request = Time.time; Say(tlife.transform, bo.P(0, 3f, 0), req[rn++ % req.Length], 5f); };
            foreach (var e in new[] { -1f, 1f }) { var sp = bo.P(e * 3.2f, 0, -1.2f); for (var k = 0; k < 2; k++) { mk.Box(marble, sp + up * (.8f + k * 1.6f), new Vector3(1.5f, 1.55f, .9f), bo.r); mk.Prism(tileM[(k + (e > 0 ? 1 : 3)) % 4], sp + up * (.8f + k * 1.6f) + bo.Fwd * .46f, bo.Fwd, .5f, .03f, 14); mk.Prism(dark, sp + up * (.8f + k * 1.6f) + bo.Fwd * .48f, bo.Fwd, .2f, .03f, 10); } Col(sp + up * 1.6f, new Vector3(1.5f, 3.2f, .9f), bo.r); }
            var dj = Glam(); dj.top = new Color(.95f, .95f, 1f); dj.hat = Residents.Hat.None; Folk(bo.P(0, 0, -.95f), bo.Fwd, Resident.Pose.Work, new[] { "Requests? Always. I've got them all. I've got the long version of all of them.", "I never play the same song twice. I play it four times. It's what the people want.", "Watch the floor when the strings come in. That's my favourite part of the job." }, dj).workRate = 1.2f;
            // a table of things to dress up in
            var tb = bo.P(-5.6f, 0, .4f); mk.Box(marble, tb + up * .8f, new Vector3(1.8f, .06f, .8f), bo.r); foreach (var e in new[] { -.8f, .8f }) mk.Box(goldM, tb + up * .4f + bo.Right * e, new Vector3(.06f, .8f, .7f), bo.r); Col(tb + up * .4f, new Vector3(1.8f, .8f, .8f), bo.r);
            var boa = new MeshKit(); for (var k = 0; k < 9; k++) boa.Blob(WorldMaterials.Solid(new Color(1f, .5f, .7f)), new Vector3(-.4f + k * .1f, .05f, Mathf.Sin(k * 1.2f) * .08f), Vector3.one * .07f, prng, .3f); Thing(boa, "Feather boa", "a feather boa", tb + up * .84f - bo.Right * .2f, bo.r, new Vector3(.9f, .14f, .3f), .15f);
            var tam = new MeshKit(); tam.Prism(WorldMaterials.Solid(new Color(.75f, .55f, .3f)), Vector3.zero, Vector3.up, .16f, .05f, 12); tam.Prism(marble, new Vector3(0, .04f, 0), Vector3.up, .14f, .012f, 12); for (var k = 0; k < 6; k++) tam.Blob(goldM, Quaternion.Euler(0, k * 60f, 0) * new Vector3(0, .025f, .16f), new Vector3(.03f, .02f, .01f), prng, 0f); Thing(tam, "Tambourine", "a tambourine", tb + up * .84f + bo.Right * .6f, bo.r, new Vector3(.34f, .07f, .34f), .2f);
            var gl = new MeshKit(); foreach (var e in new[] { -.08f, .08f }) for (var k = 0; k < 5; k++) { var d = Quaternion.Euler(0, 0, k * 72f) * Vector3.up; gl.Prism(GlowM(Gold, 1.2f), new Vector3(e, .06f, 0), d, .04f, .08f, 3, .1f); } gl.Box(goldM, new Vector3(0, .06f, -.01f), new Vector3(.08f, .012f, .012f), Quaternion.identity); Thing(gl, "Star glasses", "a pair of star-shaped glasses", tb + up * .84f - bo.Right * .75f, bo.r, new Vector3(.32f, .14f, .1f), .1f);
            // dancers on the floor, a pair circling it
            string[][] dance = { new[] { "Point at the ceiling. Point at the floor. You're now qualified.", "I've been spinning since the first chorus. If I stop the room keeps going." }, new[] { "Seventeen forever. Well, not forever. Until the song ends, and it doesn't.", "Feel the beat from the tambourine! There's one on the table. Go on." }, new[] { "This step's called the Lighthouse. You turn slowly and everybody admires you.", "My flares are so wide I get two extra steps every bar." }, new[] { "We met on this exact tile. The gold one. We've never left it.", "Don't mind us. We're having a moment." }, new[] { "Hands up if you know the words! Hands up if you don't! Same amount of fun!", "The floor lights up where you're about to step. It knows before you do." } };
            for (var k = 0; k < 5; k++) { var d = Quaternion.Euler(0, k * 72f + 20f, 0) * f.Fwd * (3f + (k % 2) * 3f); Folk(c + d + up * .2f, -d, Resident.Pose.Wave, dance[k]); }
            var stroll = new List<Vector3>(); for (var k = 0; k < 12; k++) stroll.Add(c + Quaternion.Euler(0, k * 30f, 0) * f.Fwd * 19.5f + up * .13f);
            Folk(stroll[0], FaceTo(stroll[0], stroll[1]), Resident.Pose.Walk, new[] { "One lap to be seen, then onto the floor. It's a system.", "The limousine's not waiting for anyone. It just likes being here." }, null, stroll.ToArray()).speed = .9f;
            for (var k = 0; k < 4; k++) { var d = Quaternion.Euler(0, k * 90f + 22f, 0) * f.Fwd; WhiteBench(mk, c + d * 21.5f + up * .13f, -d); }
            // the white limousine at the kerb on the other side
            var lf = new PlaceFrame(c + sideways * (FloorHalf + 5.5f) + up * .13f, toLight); var limo = WorldMaterials.Glow(new Color(.95f, .95f, 1f), .22f);
            mk.Box(limo, lf.P(0, .6f, 0), new Vector3(1.9f, .6f, 8.6f), lf.r); mk.Box(limo, lf.P(0, 1.1f, -.4f), new Vector3(1.75f, .5f, 5.6f), lf.r); mk.Box(navy, lf.P(0, 1.12f, -.4f), new Vector3(1.78f, .34f, 5.3f), lf.r); mk.Box(goldM, lf.P(0, .34f, 0), new Vector3(1.94f, .06f, 8.64f), lf.r);
            foreach (var (x, z) in new[] { (-.95f, 3f), (.95f, 3f), (-.95f, -3f), (.95f, -3f) }) { mk.Prism(dark, lf.P(x - Mathf.Sign(x) * .12f, .36f, z), lf.Right * Mathf.Sign(x), .36f, .24f, 12); mk.Prism(chrome, lf.P(x + Mathf.Sign(x) * .125f, .36f, z), lf.Right * Mathf.Sign(x), .2f, .01f, 10); }
            foreach (var e in new[] { -1f, 1f }) { mk.Box(GlowM(new Color(1f, .97f, .85f), 1.4f), lf.P(e * .65f, .65f, 4.31f), new Vector3(.4f, .2f, .03f), lf.r); mk.Box(GlowM(new Color(1f, .2f, .2f), 1.2f), lf.P(e * .7f, .65f, -4.31f), new Vector3(.36f, .2f, .03f), lf.r); }
            Col(lf.P(0, .8f, 0), new Vector3(1.9f, 1.6f, 8.6f), lf.r); Interact.Seat(tlife.transform, lf.P(-.9f, .7f, -1.4f), -lf.Right, "Sit in the limousine"); Interact.Seat(tlife.transform, lf.P(-.9f, .7f, .2f), -lf.Right, "Sit in the limousine");
            var ch = Glam(); ch.top = new Color(.15f, .16f, .28f); ch.bottom = new Color(.15f, .16f, .28f); ch.hat = Residents.Hat.Cap; ch.hatColor = new Color(.15f, .16f, .28f); Folk(lf.P(-1.8f, 0, 3.4f), -lf.Right, Resident.Pose.Stand, new[] { "Door's open. She's got a mirror ball in the back. A small one. Tasteful.", "I've driven everyone. Kings, queens, a man who said he was Napoleon. He tipped well.", "Where to? I'm joking. You're already here." }, ch);
            // the welcome sign
            var sg = c + toLight * 17f + sideways * 17f + up * .13f; var sf = -(toLight + sideways).normalized; foreach (var e in new[] { -3f, 3f }) { mk.Prism(marble, sg + Vector3.Cross(up, sf) * e - up * .2f, up, .14f, 4.4f, 6); ArtistBits.PostCollider(ctx.Root, sg + Vector3.Cross(up, sf) * e, 4.2f, .16f); }
            WhiteSign(mk, sg + up * 3.3f, sf, 6.6f, 2f, "DISCOTOPIA", .62f, "dancing nightly  ·  every night is the night"); for (var k = 0; k < 14; k++) mk.Blob(tileM[k % 4], sg + up * 4.45f + Vector3.Cross(up, sf) * (-3.2f + k * .49f) + sf * .1f, Vector3.one * .09f, prng, 0f);
            Mark(c, 30f);
            Shot(ctx, "the floor", c - toLight * 21f - sideways * 7f + up * 3f, c + up * 3.5f);
            Shot(ctx, "on the floor", c - toLight * 7f + sideways * 3f + up * 1.9f, c + toLight * 30f + up * 4f);
        }

        // ---------------------------------------------------------------- the Esplanade
        void Esplanade(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var c = new Vector3(center.x, 0, center.z); var end = lightDist - 13f;
            var pts = new List<Vector3>(); for (var d = 28f; d <= end; d += 2f) pts.Add(ctx.OnGround(c.x + toLight.x * d, c.z + toLight.z * d)); if (pts.Count < 2) return;
            Strip(mk, pts, marble, 6f, .16f, ctx); foreach (var p in pts) Remember(p, 1);
            for (var i = 0; i < pts.Count; i++)
            {
                if (i % 2 == 0) mk.Box(tileM[(i / 2) % 4], pts[i] + up * .19f, new Vector3(.7f, .03f, .7f), Quaternion.LookRotation(toLight) * Quaternion.Euler(0, 45f, 0));
                if (i % 4 == 2) foreach (var e in new[] { -1f, 1f }) { var bp = pts[i] + sideways * e * 3.4f; bp.y = ctx.Ground(bp.x, bp.z); mk.Prism(marble, bp - up * .1f, up, .14f, .8f, 8); mk.Blob(goldM, bp + up * .78f, Vector3.one * .16f, prng, 0f); ArtistBits.PostCollider(ctx.Root, bp, .8f, .16f); }
            }
            Vector3? prevL = null, prevR = null; var n = 0;
            for (var d = 36f; d < end - 6f; d += 20f, n++) foreach (var e in new[] { -1f, 1f })
            {
                var lp = ctx.OnGround(c.x + toLight.x * d + sideways.x * e * 4.2f, c.z + toLight.z * d + sideways.z * e * 4.2f); GlobeLamp(ctx, mk, lp, (n + (e > 0 ? 0 : 1)) % 2 == 0); var top = lp + up * 4f;
                if (e < 0) { if (prevL.HasValue) LightString(mk, prevL.Value, top, n); prevL = top; } else { if (prevR.HasValue) LightString(mk, prevR.Value, top, n + 2); prevR = top; }
            }
            n = 0; for (var d = 46f; d < end - 10f; d += 40f, n++) { var e = n % 2 == 0 ? 1f : -1f; WhiteBench(mk, ctx.OnGround(c.x + toLight.x * d + sideways.x * e * 5f, c.z + toLight.z * d + sideways.z * e * 5f), -sideways * e); }
            // the photo booth, a third of the way along
            var pb = new PlaceFrame(ctx.OnGround(c.x + toLight.x * 66f + sideways.x * 6.4f, c.z + toLight.z * 66f + sideways.z * 6.4f), -sideways);
            mk.Box(marble, pb.P(0, 1.2f, -.4f), new Vector3(1.6f, 2.4f, 1.2f), pb.r); mk.Box(goldM, pb.P(0, 2.46f, -.4f), new Vector3(1.8f, .1f, 1.4f), pb.r); mk.Box(navy, pb.P(0, 1.1f, .21f), new Vector3(.9f, 1.8f, .04f), pb.r); Col(pb.P(0, 1.2f, -.4f), new Vector3(1.6f, 2.4f, 1.2f), pb.r); Sign(pb.P(0, 2.2f, .22f), pb.Fwd, "PHOTOS", .16f, Ink);
            var flashM = new Material(GlowM(Pale, .4f)) { name = "Photo flash" }; mk.Box(flashM, pb.P(0, 2.02f, .21f), new Vector3(.9f, .12f, .03f), pb.r); tlife.flashM = flashM; tlife.flashLight = ArtistBits.PointLight(ctx.Root, pb.P(0, 1.8f, 1.2f), Color.white, 14f, 0f);
            var pgo = new GameObject("Photo booth"); pgo.transform.SetParent(tlife.transform, false); pgo.transform.position = pb.P(0, 1.2f, .6f); string[] snap = { "FLASH. Four pictures: surprised, laughing, blinking, perfect.", "FLASH. You look like an album cover.", "FLASH. Someone photobombs you. They look great too." }; var sn = 0;
            Interact.Toy(pgo, Toy.Kind.Custom, "Strike a pose", new Vector3(1.4f, 2f, 1.2f)).onUse = _ => { tlife.flash = Time.time; Say(tlife.transform, pb.P(0, 3.1f, 0), snap[sn++ % snap.Length], 4f); };
            // the ice-cream cart, two thirds of the way along
            var ic = new PlaceFrame(ctx.OnGround(c.x + toLight.x * Mathf.Min(end - 30f, 150f) - sideways.x * 6.4f, c.z + toLight.z * Mathf.Min(end - 30f, 150f) - sideways.z * 6.4f), sideways);
            mk.Box(marble, ic.P(0, .7f, 0), new Vector3(1.8f, .9f, 1f), ic.r); foreach (var e in new[] { -1f, 1f }) mk.Prism(goldM, ic.P(e * .95f, .4f, 0) - ic.Right * e * .04f, ic.Right * e, .4f, .08f, 12); mk.Prism(chrome, ic.P(0, 1.15f, 0), up, .03f, 1.5f, 5); for (var k = 0; k < 8; k++) mk.Prism(k % 2 == 0 ? goldM : marble, ic.P(0, 2.5f, 0), Quaternion.Euler(0, k * 45f, 0) * new Vector3(.55f, -.28f, 0), .5f, 1.1f, 3, .05f); Col(ic.P(0, .7f, 0), new Vector3(1.9f, 1.4f, 1.1f), ic.r);
            var vendor = Glam(Residents.Tool.None, true); Folk(ic.P(0, 0, -.9f), ic.Fwd, Resident.Pose.Stand, new[] { "Vanilla, lemon or glitter. The glitter's just vanilla, but it believes in itself.", "Eat it before the light gets to it. The light makes everything melt a little.", "Two scoops for the dancers. You're a dancer. I can tell by the shoes." }, vendor);
            Color[] scoops = { new Color(1f, .97f, .85f), new Color(1f, .9f, .4f), new Color(.95f, .6f, .75f) }; string[] flav = { "a vanilla cone", "a lemon cone", "a glitter cone" };
            for (var k = 0; k < 3; k++) { var cm = new MeshKit(); cm.Cone(WorldMaterials.Solid(new Color(.85f, .65f, .35f)), new Vector3(0, .16f, 0), .05f, -.16f, 8); cm.Blob(WorldMaterials.Glow(scoops[k], .3f), new Vector3(0, .2f, 0), Vector3.one * .065f, prng, .1f); Thing(cm, "Ice cream", flav[k], ic.P(-.5f + k * .5f, 1.16f, .2f), ic.r, new Vector3(.14f, .28f, .14f), .12f); }
            // a couple walking the whole length and back
            var a = ctx.OnGround(c.x + toLight.x * 34f + sideways.x * 1.6f, c.z + toLight.z * 34f + sideways.z * 1.6f); var b = ctx.OnGround(c.x + toLight.x * (end - 8f) + sideways.x * 1.6f, c.z + toLight.z * (end - 8f) + sideways.z * 1.6f); var route = new List<Vector3>();
            for (var k = 0; k <= 14; k++) { var p = Vector3.Lerp(a, b, k / 14f); route.Add(ctx.OnGround(p.x, p.z) + up * .18f); } for (var k = 14; k >= 0; k--) { var p = Vector3.Lerp(a, b, k / 14f) - sideways * 3.2f; route.Add(ctx.OnGround(p.x, p.z) + up * .18f); }
            Folk(route[0], toLight, Resident.Pose.Walk, new[] { "Out to the lighthouse and back before the next song. We never make it. The songs are too good.", "The sea's doing the hustle tonight. Look at it." }, null, route.ToArray()).speed = 1f;
            for (var d = 28f; d <= end; d += 10f) Mark(ctx.OnGround(c.x + toLight.x * d, c.z + toLight.z * d), 5f);
            Shot(ctx, "the esplanade", ctx.OnGround(c.x + toLight.x * 34f, c.z + toLight.z * 34f) + up * 2.2f, ctx.OnGround(c.x + toLight.x * lightDist, c.z + toLight.z * lightDist) + up * 12f);
        }

        // ---------------------------------------------------------------- the Lighthouse
        void Lighthouse(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var g = ctx.OnGround(light2.x, light2.y); var f = new PlaceFrame(g, -toLight);
            mk.Prism(stone, g - up * .28f, up, 13f, .4f, 22); mk.Prism(marble, g, up, 4.4f, .7f, 16); solid.Prism(marble, g - up * .3f, up, 4.4f, 1f, 16);
            // the tower: white, tapering, three gold bands, a gallery, and the mirror ball where the lamp would be
            mk.Prism(marble, g + up * .7f, up, 3.4f, TowerH, 16, .62f); solid.Prism(marble, g, up, 3.4f, TowerH, 12, .62f); for (var k = 1; k <= 3; k++) { var t = k / 4f; mk.Prism(goldM, g + up * (.7f + TowerH * t), up, 3.4f * Mathf.Lerp(1f, .62f, t) + .04f, .5f, 16); }
            mk.Box(dark, f.P(0, 1.9f, 3.42f), new Vector3(1.2f, 2.3f, .1f), f.r); mk.Prism(dark, f.P(0, 3.05f, 3.37f), f.Fwd, .6f, .1f, 8); for (var k = 0; k < 4; k++) mk.Box(windowM, g + up * (5f + k * 4.6f) + f.Fwd * (3.4f * Mathf.Lerp(1f, .62f, (4.3f + k * 4.6f) / TowerH) + .02f), new Vector3(.6f, 1f, .06f), f.r);
            var top = g + up * (TowerH + .7f); mk.Prism(marble, top, up, 3.3f, .3f, 16); for (var k = 0; k < 16; k++) { var d = Quaternion.Euler(0, k * 22.5f, 0) * f.Fwd; mk.Prism(goldM, top + d * 3.1f + up * .3f, up, .04f, .9f, 4); mk.Blob(tileM[k % 4], top + d * 3.1f + up * 1.25f, Vector3.one * .1f, prng, 0f); }
            mk.Prism(goldM, top + up * 1.2f, up, 3.14f, .06f, 16); mk.Prism(chrome, top + up * .3f, up, .5f, 1.2f, 8); mk.Cone(goldM, top + up * 7.3f, 1.4f, 1.3f, 12); for (var k = 0; k < 4; k++) mk.Prism(chrome, top + Quaternion.Euler(0, k * 90f + 45f, 0) * f.Fwd * 2.2f + up * .3f, (up * 7f - Quaternion.Euler(0, k * 90f + 45f, 0) * f.Fwd * .9f).normalized, .05f, 7.1f, 4);
            var ball = new MeshKit(); var ballM = new Material(WorldMaterials.Glow(Silver, 2.2f)) { name = "The Lighthouse mirror ball" }; ball.Blob(ballM, Vector3.zero, Vector3.one * 2.5f, prng, .1f); var bgo = ball.BuildSingle("The Lighthouse mirror ball", tlife.transform, false); bgo.transform.position = top + up * 4.2f;
            life.ball = bgo.transform; life.ballM = ballM; life.lightAt = top + up * 4.2f; life.lightLamp = ArtistBits.PointLight(ctx.Root, top + up * 4.2f, new Color(.95f, .97f, 1f), 120f, 3.4f); halos.Add(top + up * 4.2f, 16f, Silver, .55f); halos.Add(top + up * 4.2f, 7f, Pale, .6f);
            // at the foot: the lever, a telescope, the keeper, benches round the plinth
            var lv = f.P(2.6f, .7f, 3.2f); mk.Box(goldM, lv + up * .3f, new Vector3(.5f, .6f, .4f), f.r); Col(lv + up * .3f, new Vector3(.5f, .6f, .4f), f.r); var lm = new MeshKit(); lm.Prism(chrome, Vector3.zero, Vector3.up, .03f, .9f, 5); lm.Blob(GlowM(Amber, 1.6f), new Vector3(0, .95f, 0), Vector3.one * .1f, prng, 0f);
            var lgo = lm.BuildSingle("Lighthouse lever", tlife.transform, false); lgo.transform.SetPositionAndRotation(lv + up * .6f, f.r * Quaternion.Euler(-30f, 0, 0)); tlife.lever = lgo.transform; tlife.leverRot = f.r;
            string[] spin = { "The great ball spins up. Specks of light go racing over the town, the lawns, the sea, your shoes.", "Faster. Somewhere on the Floor a cheer goes up; they know somebody's at the lever.", "The whole night glitters. A ship out at sea changes course to come and dance." }; var spn = 0;
            Interact.Toy(lgo, Toy.Kind.Custom, "Throw the lever", new Vector3(.9f, 1.6f, .9f)).onUse = _ => { life.lever = Time.time; tlife.pulled = Time.time; Say(tlife.transform, lv + up * 2.6f, spin[spn++ % spin.Length], 5.5f); };
            var ts = g + toLight * 8.5f + sideways * 3f; ts.y = ctx.Ground(ts.x, ts.z) + .13f; mk.Prism(chrome, ts, up, .05f, 1.3f, 5); mk.Prism(chrome, ts, up, .3f, .05f, 8); mk.Prism(goldM, ts + up * 1.35f - toLight * .5f, (toLight + up * .15f).normalized, .08f, 1f, 8, 1.3f); ArtistBits.PostCollider(ctx.Root, ts, 1.3f, .1f);
            var tgo = new GameObject("Telescope"); tgo.transform.SetParent(tlife.transform, false); tgo.transform.position = ts + up * 1.3f; string[] look = { "A liner out at sea with every porthole lit. They've strung lights from the funnels. They're dancing too.", "The moon's reflection, cut into a thousand pieces by the mirror ball's light.", "On the far shore: a jungle, glowing purple. Something large and round is parked in it." }; var ln = 0;
            Interact.Toy(tgo, Toy.Kind.Custom, "Look through the telescope", new Vector3(1f, 1f, 1.4f)).onUse = _ => Say(tlife.transform, ts + up * 2.6f, look[ln++ % look.Length], 5.5f);
            var keeper = Glam(); keeper.top = new Color(.2f, .22f, .4f); keeper.hat = Residents.Hat.Cap; keeper.hatColor = Pale; Folk(f.P(-2.4f, .7f, 3.3f), f.Fwd, Resident.Pose.Lean, new[] { "I polish her every day. Four thousand little mirrors. I've named most of them.", "A normal lighthouse says 'keep away'. Ours says 'come on in, the music's lovely'.", "Throw the lever if you want. Everybody should, once." }, keeper);
            for (var k = 0; k < 3; k++) { var d = Quaternion.Euler(0, k * 70f + 110f, 0) * f.Fwd; WhiteBench(mk, g + d * 10f + up * .13f, d); }
            for (var k = 0; k < 6; k++) { var d = Quaternion.Euler(0, k * 60f + 30f, 0) * f.Fwd; GlobeLamp(ctx, mk, g + d * 12f + up * .13f, k % 2 == 0); }
            Mark(g, 14f); Remember(g + f.Fwd * 11f, 1);
            Shot(ctx, "the lighthouse", g - toLight * 26f + sideways * 9f + up * 3f, g + up * 16f);
        }

        // ---------------------------------------------------------------- the Belvedere
        void Belvedere(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var g = ctx.OnGround(belv2.x, belv2.y); var face = FaceTo(g, ctx.OnGround(center.x, center.z)); var f = new PlaceFrame(g, face); const float W = 24f, D = 14f, H = 4.2f;
            // the platform: white, with a row of dark arches along the front and a grand stair in the middle
            mk.Box(stone, f.P(0, H / 2f - .3f, 0), new Vector3(W, H + .6f, D), f.r); solid.Box(stone, f.P(0, H / 2f - .3f, 0), new Vector3(W, H + .6f, D), f.r); mk.Box(marble, f.P(0, H + .03f, 0), new Vector3(W + .4f, .14f, D + .4f), f.r); mk.Box(goldM, f.P(0, H - .12f, D / 2f + .02f), new Vector3(W + .2f, .12f, .08f), f.r);
            for (var k = 0; k < 6; k++) { var x = (k < 3 ? -W / 2f + 2.2f + k * 3.1f : W / 2f - 2.2f - (k - 3) * 3.1f); mk.Box(navy, f.P(x, 1.4f, D / 2f + .02f), new Vector3(2f, 2.8f, .06f), f.r); mk.Prism(navy, f.P(x, 2.8f, D / 2f - .03f), f.Fwd, 1f, .08f, 10); mk.Box(windowM, f.P(x, 1.2f, D / 2f + .05f), new Vector3(.9f, 1.4f, .04f), f.r); halos.Add(f.P(x, 1.4f, D / 2f + .8f), 1.4f, Gold, .3f); }
            const int steps = 12; const float sw = 6f, run = .46f; var rise = H / steps;
            for (var k = 0; k < steps; k++) { var st = f.P(0, rise * (k + .5f), D / 2f + (steps - k - .5f) * run); mk.Box(marble, st + up * (rise / 2f - .04f), new Vector3(sw, .08f, run + .02f), f.r); mk.Box(stone, st - up * (rise * k / 2f + .04f), new Vector3(sw, rise * (k + 1) - .08f, run), f.r); solid.Box(stone, st - up * (rise * k / 2f), new Vector3(sw, rise * (k + 1), run), f.r); if (k % 2 == 1) foreach (var e in new[] { -1f, 1f }) mk.Blob(tileM[k % 4], st + f.Right * e * (sw / 2f - .2f) + up * (rise / 2f + .08f), Vector3.one * .09f, prng, 0f); }
            foreach (var e in new[] { -1f, 1f }) { var a = f.P(e * (sw / 2f + .15f), .9f, D / 2f + steps * run); var b = f.P(e * (sw / 2f + .15f), H + .9f, D / 2f); mk.Prism(goldM, a, (b - a).normalized, .05f, (b - a).magnitude, 5); Col((a + b) / 2f - up * .45f, new Vector3(.1f, 1f, (b - a).magnitude), Quaternion.LookRotation((b - a).normalized)); mk.Prism(marble, f.P(e * (sw / 2f + .15f), 0, D / 2f + steps * run), up, .16f, 1f, 6); mk.Blob(lampM, f.P(e * (sw / 2f + .15f), 1.25f, D / 2f + steps * run), Vector3.one * .26f, prng, 0f); }
            // the balustrade round the top, open at the stair
            void Rail(Vector3 a, Vector3 b) { var d = b - a; var len = d.magnitude; var rr = Quaternion.LookRotation(d.normalized); mk.Box(marble, (a + b) / 2f + up * 1f, new Vector3(.16f, .12f, len), rr); for (var s = 0f; s <= len + .01f; s += len / Mathf.Max(1, Mathf.RoundToInt(len / .7f))) mk.Prism(marble, a + d.normalized * s, up, .06f, 1f, 5); Col((a + b) / 2f + up * .55f, new Vector3(.16f, 1.1f, len), rr); }
            var t0 = H + .1f; Rail(f.P(-W / 2f, t0, D / 2f), f.P(-sw / 2f - .2f, t0, D / 2f)); Rail(f.P(sw / 2f + .2f, t0, D / 2f), f.P(W / 2f, t0, D / 2f)); Rail(f.P(-W / 2f, t0, -D / 2f), f.P(W / 2f, t0, -D / 2f)); Rail(f.P(-W / 2f, t0, -D / 2f), f.P(-W / 2f, t0, D / 2f)); Rail(f.P(W / 2f, t0, -D / 2f), f.P(W / 2f, t0, D / 2f));
            var corners = new List<Vector3>(); foreach (var (x, z) in new[] { (-1f, 1f), (1f, 1f), (1f, -1f), (-1f, -1f) }) { var p = f.P(x * (W / 2f - .5f), t0, z * (D / 2f - .5f)); GlobeLamp(ctx, mk, p, true); corners.Add(p + up * 4f); }
            for (var k = 0; k < 4; k++) LightString(mk, corners[k], corners[(k + 1) % 4], k); LightString(mk, corners[0], corners[2], 1); LightString(mk, corners[1], corners[3], 3);
            // on top: little tables to sit at, the champagne tower, a waiter, guests
            var to = new PlaceFrame(f.P(0, t0, 0), face);
            foreach (var (x, z, i) in new[] { (-8f, 2.5f, 0), (-8f, -3f, 1), (8f, 2.5f, 2), (8f, -3f, 3), (-3.5f, -3.5f, 4), (3.5f, -3.5f, 5) })
            {
                var tp = to.P(x, 0, z); mk.Prism(marble, tp, up, .05f, .74f, 6); mk.Prism(marble, tp + up * .74f, up, .5f, .05f, 12); mk.Blob(tileM[i % 4], tp + up * .86f, Vector3.one * .07f, prng, 0f); ArtistBits.PostCollider(ctx.Root, tp, .8f, .4f);
                foreach (var e in new[] { -1f, 1f }) { var ch = tp + to.Right * e * .95f; mk.Prism(goldM, ch, up, .04f, .45f, 5); mk.Prism(marble, ch + up * .45f, up, .24f, .06f, 10); Interact.Seat(tlife.transform, ch + up * .52f, -to.Right * e); }
                if (i == 0) Folk(tp + to.Right * .95f + up * .52f, -to.Right, Resident.Pose.Sit, new[] { "From up here the Floor looks like a jewellery box somebody left open.", "I'm not dancing this one. I'm watching everyone else dance this one. It's a different pleasure." }, null, null, .52f);
                if (i == 3) Folk(tp - to.Right * .95f + up * .52f, to.Right, Resident.Pose.Sit, new[] { "To absent friends, and to present ones, and to this song in particular.", "The winner takes it all, they say. Up here we just share the bottle." }, Glam(Residents.Tool.Mug), null, .52f);
            }
            var ct = to.P(0, 0, 1.5f); mk.Prism(marble, ct, up, .9f, .8f, 14); mk.Prism(goldM, ct + up * .8f, up, .96f, .05f, 14); ArtistBits.PostCollider(ctx.Root, ct, .9f, .9f);
            var fizz = new Material(WorldMaterials.Glow(new Color(1f, .9f, .55f), 1f)) { name = "Champagne" }; tlife.fizzM = fizz; var cm = new MeshKit(); for (var lvl = 0; lvl < 4; lvl++) { var cnt = 4 - lvl; for (var k = 0; k < cnt * 3; k++) { var d = Quaternion.Euler(0, k * 360f / (cnt * 3), 0) * Vector3.forward * (cnt * .14f); cm.Cone(fizz, d + Vector3.up * (lvl * .2f + .14f), .08f, -.1f, 6); cm.Prism(chrome, d + Vector3.up * (lvl * .2f), Vector3.up, .012f, .06f, 3); } } cm.Cone(fizz, new Vector3(0, .94f, 0), .08f, -.1f, 6);
            var cgo = cm.BuildSingle("Champagne tower", tlife.transform, false); cgo.transform.position = ct + up * .86f; halos.Add(ct + up * 1.3f, 1.6f, Gold, .5f);
            string[] pour = { "You pour into the top glass. It fills, spills, and the whole tower runs gold from the top down.", "Pop. Somebody on the terrace says \"Ooh!\" in four-part harmony.", "It fizzes over the rim and catches the lighthouse on the way down." }; var pn = 0;
            Interact.Toy(cgo, Toy.Kind.Custom, "Pour the champagne", new Vector3(1.8f, 1.4f, 1.8f)).onUse = _ => { tlife.poured = Time.time; Say(tlife.transform, ct + up * 2.6f, pour[pn++ % pour.Length], 5f); };
            for (var k = 0; k < 4; k++) { var gm = new MeshKit(); gm.Cone(fizz, new Vector3(0, .2f, 0), .05f, -.1f, 6); gm.Prism(chrome, Vector3.zero, Vector3.up, .008f, .1f, 3); gm.Prism(chrome, Vector3.zero, Vector3.up, .035f, .008f, 6); Thing(gm, "Champagne glass", "a glass of champagne", to.P(-1.6f + k * .34f, .8f, -3.5f) + to.Right * (k < 2 ? -1.9f : 4.6f) * 0f + up * .0f + to.Right * (k < 2 ? -1.9f : 1.9f), Quaternion.identity, new Vector3(.1f, .22f, .1f), .1f); }
            var waiter = Glam(Residents.Tool.Mug); waiter.top = new Color(.95f, .95f, 1f); waiter.bottom = new Color(.12f, .12f, .2f); waiter.hat = Residents.Hat.None; Folk(to.P(2.2f, 0, 2.6f), -to.Right, Resident.Pose.Stand, new[] { "Champagne? It's included. Everything's included. It's that sort of night.", "The tower's been running since the seventies. We top it up between songs.", "Table for one, table for two or the whole terrace: all the same price, which is nothing." }, waiter);
            Folk(to.P(-5f, 0, D / 2f - .9f), to.Fwd, Resident.Pose.Lean, new[] { "I come up here to watch the lighthouse turn. Then I go down and turn a bit myself.", "Listen: you can hear the strings from here before you can hear the drums." });
            // glitter trees either side of the stair, a sign, the path from the Floor
            var trng = new System.Random(5); foreach (var e in new[] { -1f, 1f }) GlitterTree(ctx, mk, G0(f.P(e * (sw / 2f + 3f), 0, D / 2f + 3.4f)), 1.1f, trng);
            WhiteSign(mk, G0(f.P(sw / 2f + 6.5f, 0, D / 2f + steps * run + 2f)) + up * 2.2f, face, 4.2f, 1.3f, "THE BELVEDERE", .34f, "champagne  ·  the view"); foreach (var e in new[] { -1.6f, 1.6f }) mk.Prism(marble, G0(f.P(sw / 2f + 6.5f + e, 0, D / 2f + steps * run + 2f)) - up * .2f, up, .08f, 2f, 5);
            var way = PlaceTools.Smooth(ctx, new[] { new Vector2(center.x, center.z) + new Vector2(-face.x, -face.z) * 28f, new Vector2(g.x, g.z) + new Vector2(face.x, face.z) * (D / 2f + steps * run + .5f) }, false, 2f, 1f);
            if (way.Count > 1) { Strip(mk, way, marble, 4.2f, .15f, ctx); foreach (var p in way) Remember(p, 2); }
            Mark(g, 19f);
            Shot(ctx, "the belvedere", f.P(-11f, 3f, D / 2f + 19f), f.P(0, H + 1f, 0));
            Shot(ctx, "on the belvedere", to.P(9f, 1.8f, -5f), ctx.OnGround(center.x, center.z) + up * 4f);
        }
    }

    /// The town's moving parts: the Floor's mirror ball, the photo booth's flash, the lighthouse lever, the champagne.
    public class DiscoTownLife : MonoBehaviour
    {
        public Transform floorBall, lever; public Quaternion leverRot; public Material flashM, fizzM; public Light flashLight; public float flash = -99f, pulled = -99f, poured = -99f;

        void Update()
        {
            var t = Time.time; var dt = Time.deltaTime;
            if (floorBall != null) floorBall.Rotate(0, 26f * dt, 0, Space.World);
            var fa = t - flash; var fl = fa < .5f ? 1f - fa * 2f : 0f; if (flashM != null) flashM.SetColor("_EmissionColor", Color.white * (.3f + fl * 6f)); if (flashLight != null) flashLight.intensity = fl * 8f;
            if (lever != null) lever.rotation = leverRot * Quaternion.Euler(Mathf.Lerp(-30f, 30f, Mathf.Clamp01((t - pulled) * 4f)) * (t - pulled < 8f ? 1f : -1f), 0, 0);
            if (fizzM != null) fizzM.SetColor("_EmissionColor", new Color(1f, .9f, .55f) * (.65f + (t - poured < 6f ? 2.2f * Mathf.Exp(-(t - poured) * .5f) : 0f) + .1f * Mathf.Sin(t * 3f)));
        }
    }
}
