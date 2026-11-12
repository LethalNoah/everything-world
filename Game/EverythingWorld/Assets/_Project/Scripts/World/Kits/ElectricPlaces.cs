using System.Collections.Generic;
using UnityEngine;
using static EverythingWorld.World.PlaceTools;

namespace EverythingWorld.World
{
    /// The Electric Fields laid out on purpose (the build-land standard). The gate, the stages and the walkways stay
    /// where they were; four more straw walkways with coloured light poles reach the far fields a festival always
    /// has: south to the Silent Disco and on to the Secret Stage in the trees, north-east to the Carousel, north to
    /// Lost & Found, west to the Chai Tent. With festival people to talk to, and the hay bales to sit on.
    public partial class ElectricKit
    {
        static readonly Vector2 DiscoAt = new Vector2(9762, 6182), SecretAt = new Vector2(9684, 6092), CarouselAt = new Vector2(10060, 6864), LostAt = new Vector2(9878, 6866), ChaiAt = new Vector2(9545, 6446);
        static readonly Vector2[] SouthWay = { new Vector2(9728, 6302), new Vector2(9748, 6244), new Vector2(9756, 6206) };
        static readonly Vector2[] SecretWay = { new Vector2(9744, 6176), new Vector2(9722, 6136), new Vector2(9698, 6108) };
        static readonly Vector2[] NorthEastWay = { new Vector2(10082, 6756), new Vector2(10072, 6812), new Vector2(10064, 6842) };
        static readonly Vector2[] NorthWay = { new Vector2(9858, 6806), new Vector2(9872, 6846) };
        static readonly Vector2[] WestWay = { new Vector2(9588, 6502), new Vector2(9560, 6466) };
        static readonly (Vector2 at, float r)[] PlaceRings = { (DiscoAt, 13f), (SecretAt, 11f), (CarouselAt, 10f), (LostAt, 8f), (ChaiAt, 11f) };

        ElectricPlacesLife plife; System.Random prng; Transform rootT; LandContext ctx0; readonly List<Vector3> wayP = new List<Vector3>();

        static readonly Color[] FestTops = { new Color(.95f, .3f, .35f), new Color(1f, .78f, .25f), new Color(.3f, .7f, .95f), new Color(.55f, .85f, .4f), new Color(.8f, .45f, .9f), new Color(.95f, .95f, .9f), new Color(1f, .5f, .7f) };
        static readonly Color[] FestLegs = { new Color(.3f, .4f, .6f), new Color(.85f, .8f, .65f), new Color(.2f, .2f, .24f), new Color(.5f, .6f, .35f) };
        static readonly Residents.Hat[] FestHats = { Residents.Hat.Bucket, Residents.Hat.None, Residents.Hat.Straw, Residents.Hat.None, Residents.Hat.Cap, Residents.Hat.Cowboy };
        static readonly string[] FestTalk =
        {
            "Have you seen my friends? They were by the flag. Which flag. THE flag.", "Best weekend of my life. It's been the same weekend for ages. Still the best.", "I've got glitter in places glitter shouldn't know about.",
            "Who's on next? Doesn't matter. I'm going to cry at it anyway.", "The trick is to never go back to your tent. I don't remember where it is, so that's easy.",
        };

        Residents.Look Reveller(Residents.Tool tool = Residents.Tool.None, bool apron = false) { var l = Residents.Random(prng, FestTops, FestLegs, FestHats, FestTops); l.tool = tool; l.apron = apron; if (l.hat == Residents.Hat.None && prng.Next(2) == 0) l.longHair = true; return l; }
        Resident Folk(Vector3 feet, Vector3 face, Resident.Pose pose, string[] lines = null, Residents.Look look = null, Vector3[] route = null, float seatH = HumanScale.SeatHeight)
            => Residents.Make(plife.transform, feet, face, look ?? Reveller(), pose, prng, lines ?? FestTalk, route, seatH);
        void Col(Vector3 c, Vector3 size, Quaternion rot) { var go = new GameObject("Box collider"); go.transform.SetParent(rootT, false); go.transform.SetPositionAndRotation(c, rot); go.AddComponent<BoxCollider>().size = size; go.isStatic = true; }
        Vector3 G(Vector3 p) => ctx0.OnGround(p.x, p.z);
        void Ring(Vector3 at, float r) => noGrass.Add(new Vector3(at.x, r, at.z));

        /// A flag sign like the song places': a painted board on a pole. Signs on the +x side of a walkway; the photo spot on the −x side.
        void FlagSign(MeshKit mk, Vector3 at, Vector3 face, string text, Color c, float w = 2.6f, float size = .2f)
        {
            var up = Vector3.up; var g = G(at); var rot = Quaternion.LookRotation(face);
            mk.Prism(wood, g, up, .07f, 3f, 6); mk.Box(WorldMaterials.Solid(c, .3f), g + up * 2.3f, new Vector3(w, .9f, .08f), rot); Col(g + up * 1.5f, new Vector3(w, 3f, .14f), rot); for (var k = 0; k < 5; k++) mk.Blob(GlowM(Neon[k % Neon.Length], 2.2f), g + up * 2.82f + rot * Vector3.right * (-w / 2f + .2f + k * (w - .4f) / 4f) + face * .05f, Vector3.one * .06f, prng, 0f);
            halos.Add(g + up * 2.4f + face * .3f, 1.4f, c, .4f); ArtistBits.Label(rootT, text, g + up * 2.3f + face * .05f, face, size, Ink, TextAnchor.MiddleCenter);
        }

        /// Three bales in a row to sit on.
        void BaleSeats(MeshKit mk, MeshKit solid, Vector3 at, Vector3 face, int n = 3)
        {
            var up = Vector3.up; var rot = Quaternion.LookRotation(face); var side = Vector3.Cross(up, face);
            for (var k = 0; k < n; k++) { var p = G(at + side * ((k - (n - 1) / 2f) * 1.6f)); mk.Box(straw, p + up * .4f, new Vector3(1.5f, .8f, .9f), rot); solid.Box(straw, p + up * .4f, new Vector3(1.5f, .8f, .9f), rot); Interact.Seat(plife.transform, p + up * .86f, face, "Sit on the hay bale"); }
        }

        void StrawWay(LandContext ctx, MeshKit mk, Vector2[] way, bool joinNearest = true)
        {
            var full = new List<Vector2>();
            if (joinNearest) { Vector2 near = way[0]; var bd = 220f * 220f; foreach (var (p, _) in walkPoints) { var d = (p.x - way[0].x) * (p.x - way[0].x) + (p.z - way[0].y) * (p.z - way[0].y); if (d < bd) { bd = d; near = new Vector2(p.x, p.z); } } full.Add(near); }
            full.AddRange(way);
            var pts = PlaceTools.Smooth(ctx, full, false, 2.5f);
            for (var i = 0; i < pts.Count; i++) { foreach (var a in ctx.Artists) { var r = Foot(a) + 7f; var v = new Vector2(pts[i].x - a.x, pts[i].z - a.z); var d = v.magnitude; if (d < r) { v = d > .01f ? v / d * r : new Vector2(r, 0); pts[i] = ctx.OnGround(a.x + v.x, a.z + v.y); } } foreach (var c in noGrass) { if (c.y < 6f) continue; var r = c.y + 3f; var v = new Vector2(pts[i].x - c.x, pts[i].z - c.z); var d = v.magnitude; if (d < r) { v = d > .01f ? v / d * r : new Vector2(r, 0); pts[i] = ctx.OnGround(c.x + v.x, c.z + v.y); } } }
            if (pts.Count < 2) return;
            Strip(mk, pts, WorldMaterials.Solid(Trodden), PathW, .05f); foreach (var p in pts) Remember(p, 700);
            var up = Vector3.up; var n = 0;
            for (var i = 4; i < pts.Count; i += 9) { var d = pts[i] - pts[i - 1]; d.y = 0; if (d.sqrMagnitude < .01f) continue; d.Normalize(); var lp = pts[i] + Vector3.Cross(up, d) * (n % 2 == 0 ? 2.3f : -2.3f); LightPole(ctx, mk, ctx.OnGround(lp.x, lp.z), Neon[n % Neon.Length]); n++; }
            wayP.AddRange(pts);
        }

        void FieldPlaces(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            wayP.Clear(); rootT = ctx.Root; ctx0 = ctx; prng = new System.Random(2008);
            plife = new GameObject("Electric places life").AddComponent<ElectricPlacesLife>(); plife.transform.SetParent(life.transform, false);
            foreach (var (at, r) in PlaceRings) noGrass.Add(new Vector3(at.x, r, at.y));
            StrawWay(ctx, mk, SouthWay); StrawWay(ctx, mk, SecretWay, false); StrawWay(ctx, mk, NorthEastWay); StrawWay(ctx, mk, NorthWay); StrawWay(ctx, mk, WestWay);
            SilentDisco(ctx, mk, solid); SecretStage(ctx, mk, solid); Carousel(ctx, mk, solid); LostAndFound(ctx, mk, solid); ChaiTent(ctx, mk, solid);
        }

        // ---------------------------------------------------------------- the Silent Disco
        void SilentDisco(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var g = ctx.OnGround(DiscoAt.x, DiscoAt.y); var f = new PlaceFrame(g, FaceTo(g, Nearest(wayP, g)));
            // a round floor of dark boards ringed with poles and dim festoons; it only lights when you put the headphones on
            var floorM = new Material(WorldMaterials.Glow(new Color(.3f, .8f, 1f), .05f)) { name = "Silent disco floor" }; mk.Prism(dark, g - up * .15f, up, 9f, .35f, 20); solid.Prism(dark, g - up * .15f, up, 9f, .3f, 20);
            for (var r = 0; r < 3; r++) for (var k = 0; k < 6 + r * 4; k++) { var a = k * Mathf.PI * 2f / (6 + r * 4) + r * .3f; var tm = new Material(WorldMaterials.Glow(Neon[(k + r) % Neon.Length], .05f)) { name = "Disco tile" }; mk.Prism(tm, g + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * (2.2f + r * 2.6f) + up * .21f, up, 1f, .02f, 6); plife.discoTiles.Add((tm, Neon[(k + r) % Neon.Length], k + r * 5)); }
            for (var k = 0; k < 8; k++) { var p = G(g + Quaternion.Euler(0, k * 45f + 22f, 0) * Vector3.forward * 9.6f); mk.Prism(metal, p, up, .06f, 3.4f, 6); ArtistBits.PostCollider(ctx.Root, p, 3.4f, .08f); var bm = new Material(WorldMaterials.Glow(Neon[k % Neon.Length], .3f)) { name = "Disco pole light" }; mk.Blob(bm, p + up * 3.5f, Vector3.one * .2f, prng, 0f); plife.discoTiles.Add((bm, Neon[k % Neon.Length], k * 3)); }
            plife.discoLight = ArtistBits.PointLight(ctx.Root, g + up * 3.4f, Electric, 28f, .5f);
            // dancers in headphones, each to a different song
            string[] silent = { "(They can't hear you. They lift one earcup.) WHAT? OH. IT'S SO GOOD.", "(Dancing hard, to nothing you can hear.)", "(Mouths every word of a song that isn't playing.)", "Channel two is the one. Channel one is someone's dad's playlist.", "(Points at the headphone rack. Points at you. Points at the floor.)" };
            for (var k = 0; k < 8; k++) { var a = k * 47f * Mathf.Deg2Rad; var p = g + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * (2.4f + (k % 3) * 2f) + up * .2f; var l = Reveller(); l.hat = Residents.Hat.None; var r = Folk(p, FaceTo(p, g), Resident.Pose.Wave, silent, l); r.workRate = 1.4f + (k % 3) * .5f; mk.Box(GlowM(Neon[k % Neon.Length], 2.4f), p + up * 1.78f, new Vector3(.34f, .05f, .08f), Quaternion.LookRotation(FaceTo(p, g))); }
            // the headphone rack by the way in: put a pair on
            var rk = G(f.P(3.5f, 0, 10.5f)); mk.Box(wood, rk + up * 1f, new Vector3(2.4f, .08f, .3f), f.r); foreach (var e in new[] { -1.1f, 1.1f }) mk.Box(wood, rk + up * .5f + f.Right * e, new Vector3(.08f, 1.1f, .3f), f.r); Col(rk + up * .55f, new Vector3(2.4f, 1.1f, .4f), f.r);
            for (var k = 0; k < 5; k++) { var hp = rk + up * 1.2f + f.Right * (-.9f + k * .45f); mk.Box(dark, hp, new Vector3(.3f, .04f, .06f), f.r); foreach (var e in new[] { -.15f, .15f }) mk.Blob(GlowM(Neon[k % Neon.Length], 2.4f), hp + f.Right * e - up * .1f, Vector3.one * .08f, prng, 0f); }
            var hgo = new GameObject("Headphone rack"); hgo.transform.SetParent(plife.transform, false); hgo.transform.position = rk + up * 1.2f;
            Interact.Toy(hgo, Toy.Kind.Custom, "Put the headphones on", new Vector3(2.4f, 1f, 1f)).onUse = _ => { plife.listening = !plife.listening; Say(plife.transform, rk + up * 2.4f, plife.listening ? "The bass arrives all at once. The whole floor is in on it." : "You take them off. A field of people dancing to crickets.", 5f); };
            BaleSeats(mk, solid, f.P(-6f, 0, 11f), -f.Fwd);
            FlagSign(mk, f.P(7f, 0, 13.5f), f.Fwd, "SILENT DISCO\nshhh", Electric);
            Ring(g, 12f);
            Shot(ctx, "the silent disco", f.P(-9f, 5f, 15f), g + up * 1f);
        }

        // ---------------------------------------------------------------- the Secret Stage
        void SecretStage(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var g = ctx.OnGround(SecretAt.x, SecretAt.y); var f = new PlaceFrame(g, FaceTo(g, Nearest(wayP, g))); var leaf = WorldMaterials.Solid(new Color(.3f, .48f, .28f)); var barkM = WorldMaterials.Solid(new Color(.36f, .28f, .22f));
            // a ring of trees to hide it, fairy lights strung between them
            var tops = new List<Vector3>();
            for (var k = 0; k < 9; k++) { if (k == 0) continue; var p = G(g + Quaternion.Euler(0, k * 40f, 0) * f.Fwd * 9.5f); var h = 5f + (k % 3); mk.Prism(barkM, p - up * .3f, up, .35f, h + .3f, 7, .7f); ArtistBits.PostCollider(ctx.Root, p, h, .35f); soft.Blob(leaf, p + up * (h + 1.6f), new Vector3(3f, 2.4f, 3f), prng, .2f); tops.Add(p + up * 3.6f); }
            for (var k = 0; k + 1 < tops.Count; k++) { var a = tops[k]; var b = tops[k + 1]; for (var s = 0; s <= 10; s++) { var t = s / 10f; var q = Vector3.Lerp(a, b, t) - up * Mathf.Sin(t * Mathf.PI) * .7f; soft.Blob(GlowM(s % 2 == 0 ? new Color(1f, .82f, .55f) : Neon[(k + s) % Neon.Length], 2.4f), q, Vector3.one * .07f, prng, 0f); if (s == 5) halos.Add(q, .9f, new Color(1f, .82f, .55f), .6f); } }
            // a tiny stage of pallets, a painted backdrop, one performer with a guitar, an amp the size of a lunchbox
            var st = G(g - f.Fwd * 4f); mk.Box(wood, st + up * .25f, new Vector3(4.4f, .5f, 3f), f.r); solid.Box(wood, st + up * .25f, new Vector3(4.4f, .5f, 3f), f.r); for (var s = 0; s < 6; s++) mk.Box(WorldMaterials.Solid(Bunting[s % Bunting.Length], .3f), st + up * 1.9f - f.Fwd * 1.5f + f.Right * (-1.9f + s * .76f), new Vector3(.74f, 2.8f, .06f), f.r); Col(st + up * 1.9f - f.Fwd * 1.5f, new Vector3(4.6f, 2.8f, .15f), f.r);
            ArtistBits.Label(ctx.Root, "you found it", st + up * 2.9f - f.Fwd * 1.44f, f.Fwd, .34f, Ink, TextAnchor.MiddleCenter);
            var perf = Reveller(Residents.Tool.Rod); Folk(st + up * .5f + f.Fwd * .2f, f.Fwd, Resident.Pose.Work, new[] { "This one's a new one. They're all new ones. I make them up as you walk in.", "Twelve people have ever seen this stage. You're one of them now. Don't tell the others.", "Requests? I know two songs and I've just played both." }, perf).workRate = 1.3f;
            mk.Box(dark, st + up * .7f + f.Right * 1.6f, new Vector3(.5f, .4f, .3f), f.r); mk.Blob(GlowM(Hot, 2.4f), st + up * .82f + f.Right * 1.6f + f.Fwd * .16f, Vector3.one * .04f, prng, 0f);
            ArtistBits.PointLight(ctx.Root, st + up * 2.6f + f.Fwd * 1.5f, new Color(1f, .8f, .55f), 13f, 1.8f);
            BaleSeats(mk, solid, g + f.Fwd * .5f, -f.Fwd, 3); BaleSeats(mk, solid, g + f.Fwd * 3f + f.Right * .8f, -f.Fwd, 2);
            Folk(G(g + f.Fwd * .5f - f.Right * 1.6f) + up * .86f, -f.Fwd, Resident.Pose.Sit, new[] { "I only came in here to find a toilet. That was three hours ago. Best set of the weekend.", "Shh. This is the secret one." }, Reveller(Residents.Tool.Mug), null, .86f);
            // a mirror ball in a tree, a lost wellington on a branch
            var mb = tops[3] + up * .2f - (tops[3] - g).normalized * 1.6f; soft.Blob(GlowM(new Color(.9f, .95f, 1f), 1.6f), mb, Vector3.one * .4f, prng, 0f); halos.Add(mb, 1.6f, Electric, .5f);
            Ring(g, 10f);
            Shot(ctx, "the secret stage", f.P(-4f, 2.4f, 9f), st + up * 1.6f);
        }

        // ---------------------------------------------------------------- the Carousel
        void Carousel(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var g = ctx.OnGround(CarouselAt.x, CarouselAt.y); var f = new PlaceFrame(g, FaceTo(g, Nearest(wayP, g))); const float R = 5.2f;
            mk.Prism(wood, g - up * .2f, up, R + 1.2f, .45f, 20); solid.Prism(wood, g - up * .2f, up, R + 1.2f, .4f, 20); mk.Prism(WorldMaterials.Solid(new Color(.95f, .3f, .35f)), g + up * .25f, up, .9f, 4.6f, 12); var cc = new GameObject("Carousel hub"); cc.transform.SetParent(rootT, false); cc.transform.position = g + up * 2.4f; var hc = cc.AddComponent<CapsuleCollider>(); hc.radius = .9f; hc.height = 4.8f;
            // the turning part: a deck, a striped canopy, eight creatures on poles going up and down. Sit on one and ride.
            var cm = new MeshKit(); cm.Prism(WorldMaterials.Solid(new Color(.95f, .9f, .8f)), new Vector3(0, .25f, 0), Vector3.up, R, .15f, 20);
            for (var s = 0; s < 16; s++) { var a0 = s * Mathf.PI / 8f; var a1 = (s + 1) * Mathf.PI / 8f; var m = WorldMaterials.Solid(s % 2 == 0 ? new Color(.95f, .3f, .35f) : new Color(.96f, .93f, .86f), .3f); var p0 = new Vector3(Mathf.Cos(a0) * (R + .5f), 4.6f, Mathf.Sin(a0) * (R + .5f)); var p1 = new Vector3(Mathf.Cos(a1) * (R + .5f), 4.6f, Mathf.Sin(a1) * (R + .5f)); var top = new Vector3(0, 6.6f, 0); cm.Tri(m, top, p1, p0, new Vector3(0, 0, 0)); cm.Tri(m, top, p0, p1, new Vector3(0, 9f, 0)); cm.Blob(GlowM(Neon[s % Neon.Length], 2.4f), (p0 + p1) / 2f - Vector3.up * .15f, Vector3.one * .12f, prng, 0f); }
            cm.Blob(GlowM(new Color(1f, .82f, .5f), 2.6f), new Vector3(0, 6.8f, 0), Vector3.one * .3f, prng, 0f);
            var cgo = cm.BuildSingle("Carousel", plife.transform, false); cgo.transform.position = g; var deck = cgo.AddComponent<BoxCollider>(); deck.size = new Vector3(R * 1.45f, .16f, R * 1.45f); deck.center = new Vector3(0, .32f, 0); plife.carousel = cgo.transform;
            Color[] coats = { new Color(.96f, .95f, .9f), new Color(1f, .6f, .7f), new Color(.5f, .8f, 1f), new Color(1f, .85f, .4f), new Color(.7f, .95f, .6f), new Color(.85f, .6f, 1f), new Color(.96f, .95f, .9f), new Color(1f, .5f, .4f) };
            for (var k = 0; k < 8; k++)
            {
                var a = k * Mathf.PI / 4f; var d = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)); var tan = new Vector3(-d.z, 0, d.x); var hm = new MeshKit(); var coat = WorldMaterials.Solid(coats[k], .4f); var mane = GlowM(Neon[k % Neon.Length], 1.6f); var rot = Quaternion.LookRotation(tan);
                hm.Box(WorldMaterials.Solid(new Color(.85f, .7f, .3f), .8f), new Vector3(0, 1.6f, 0), new Vector3(.06f, 4.4f, .06f), Quaternion.identity); hm.Box(coat, new Vector3(0, 0, 0), new Vector3(.4f, .5f, 1.3f), Quaternion.identity);
                if (k % 4 == 3) { hm.Box(coat, new Vector3(0, .5f, .55f), new Vector3(.2f, 1.1f, .22f), Quaternion.Euler(10f, 0, 0)); hm.Box(coat, new Vector3(0, 1.05f, .78f), new Vector3(.2f, .2f, .4f), Quaternion.identity); }   // a giraffe now and then
                else { hm.Box(coat, new Vector3(0, .35f, .6f), new Vector3(.24f, .6f, .28f), Quaternion.Euler(-30f, 0, 0)); hm.Box(coat, new Vector3(0, .62f, .88f), new Vector3(.2f, .22f, .45f), Quaternion.Euler(15f, 0, 0)); hm.Box(mane, new Vector3(0, .5f, .46f), new Vector3(.06f, .55f, .16f), Quaternion.Euler(-30f, 0, 0)); if (k % 4 == 1) hm.Cone(GlowM(new Color(1f, .9f, .6f), 2.4f), new Vector3(0, .74f, .9f), .05f, .4f, 5); }
                foreach (var x in new[] { -.14f, .14f }) foreach (var z in new[] { -.45f, .45f }) hm.Box(coat, new Vector3(x, -.5f, z), new Vector3(.1f, .6f, .1f), Quaternion.Euler(z > 0 ? -18f : 18f, 0, 0)); hm.Box(mane, new Vector3(0, -.05f, -.72f), new Vector3(.08f, .5f, .08f), Quaternion.Euler(25f, 0, 0)); hm.Box(WorldMaterials.Solid(new Color(.75f, .2f, .25f)), new Vector3(0, .27f, -.05f), new Vector3(.42f, .06f, .5f), Quaternion.identity);
                var hgo = hm.BuildSingle("Carousel creature", cgo.transform, false); var rest = new Vector3(d.x * (R - 1.3f), 1.5f, d.z * (R - 1.3f)); hgo.transform.localPosition = rest; hgo.transform.localRotation = rot; plife.creatures.Add((hgo.transform, rest, k * .8f));
                var seat = Interact.Seat(hgo.transform, cgo.transform.position + rest + up * .34f, tan, k % 4 == 3 ? "Ride the giraffe" : k % 4 == 1 ? "Ride the unicorn" : "Ride the horse"); seat.transform.localPosition = new Vector3(0, .34f, -.05f);
            }
            ArtistBits.PointLight(ctx.Root, g + up * 3.6f + f.Fwd * 4f, new Color(1f, .82f, .55f), 22f, 2f);
            // the ticket booth and its keeper; bales to wait on
            var tb = G(f.P(4.5f, 0, R + 4f)); mk.Box(WorldMaterials.Solid(new Color(.3f, .7f, .95f)), tb + up * 1.2f, new Vector3(1.6f, 2.4f, 1.6f), f.r); solid.Box(wood, tb + up * 1.2f, new Vector3(1.6f, 2.4f, 1.6f), f.r); mk.Cone(WorldMaterials.Solid(new Color(1f, .78f, .25f)), tb + up * 2.4f, 1.4f, 1f, 4, 45f); mk.Box(GlowM(new Color(1f, .82f, .5f), 1.6f), tb + up * 1.5f - f.Right * .81f, new Vector3(.03f, .7f, 1f), f.r); ArtistBits.Label(ctx.Root, "RIDES\nfree, forever", tb + up * 2.1f - f.Right * .83f, -f.Right, .13f, new Color(.2f, .2f, .3f), TextAnchor.MiddleCenter);
            Folk(G(tb - f.Right * 1.6f), -f.Right, Resident.Pose.Stand, new[] { "Hop on while it's moving. It's always moving. That's the only rule and it's not a rule.", "The giraffe's the best seat. Everyone goes for the unicorn. Their loss.", "It plays one tune. I stopped hearing it years ago. I'd miss it though." }, Reveller(Residents.Tool.None, true));
            BaleSeats(mk, solid, f.P(-5f, 0, R + 4.5f), -f.Fwd);
            FlagSign(mk, f.P(7.5f, 0, R + 7f), f.Fwd, "THE CAROUSEL", Hot);
            Ring(g, 9f);
            Shot(ctx, "the carousel", f.P(-8f, 4f, R + 11f), g + up * 3f);
        }

        // ---------------------------------------------------------------- Lost & Found
        void LostAndFound(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var g = ctx.OnGround(LostAt.x, LostAt.y); var f = new PlaceFrame(g, FaceTo(g, Nearest(wayP, g)));
            // an open-fronted canvas tent, trestle tables of single things, each with a label
            foreach (var (x, z) in new[] { (-2.8f, -2f), (2.8f, -2f), (2.8f, 2f), (-2.8f, 2f) }) { var p = G(g + f.r * new Vector3(x, 0, z)); mk.Prism(wood, p, up, .07f, z < 0 ? 2.2f : 2.8f, 6); ArtistBits.PostCollider(ctx.Root, p, 2.5f, .08f); }
            mk.Box(canvas, g + up * 2.55f, new Vector3(6.2f, .08f, 4.6f), f.r * Quaternion.Euler(-8f, 0, 0)); mk.Box(canvas, g + up * 1.2f - f.Fwd * 2.05f, new Vector3(5.8f, 2.2f, .06f), f.r); Col(g + up * 1.2f - f.Fwd * 2.05f, new Vector3(5.8f, 2.4f, .15f), f.r); for (var s = 0; s < 7; s++) soft.Tri(WorldMaterials.Solid(Bunting[s % Bunting.Length]), g + up * 2.75f + f.Fwd * 2.3f + f.Right * (-2.8f + s * .8f), g + up * 2.75f + f.Fwd * 2.3f + f.Right * (-2f + s * .8f), g + up * 2.25f + f.Fwd * 2.3f + f.Right * (-2.4f + s * .8f), g);
            var tb = G(g + f.Fwd * .6f); mk.Box(wood, tb + up * .78f, new Vector3(4.6f, .06f, .9f), f.r); foreach (var e in new[] { -2f, 2f }) mk.Box(wood, tb + up * .39f + f.Right * e, new Vector3(.08f, .78f, .8f), f.r); Col(tb + up * .4f, new Vector3(4.6f, .82f, .9f), f.r);
            string[] things = { "one left wellington", "someone's phone (37 missed calls from \"Mum\")", "a tent pole, purpose unknown", "a bum bag full of glitter", "a single flip-flop", "a cuddly dinosaur" };
            for (var k = 0; k < 6; k++)
            {
                var m = new MeshKit(); var c = WorldMaterials.Solid(Bunting[k % Bunting.Length], .3f); var size = new Vector3(.3f, .3f, .3f);
                switch (k)
                {
                    case 0: m.Box(c, new Vector3(0, .2f, 0), new Vector3(.14f, .4f, .14f), Quaternion.identity); m.Box(c, new Vector3(0, .05f, .1f), new Vector3(.14f, .1f, .3f), Quaternion.identity); size = new Vector3(.2f, .42f, .34f); break;
                    case 1: m.Box(dark, new Vector3(0, .01f, 0), new Vector3(.09f, .02f, .17f), Quaternion.identity); m.Box(GlowM(Electric, 1.6f), new Vector3(0, .022f, 0), new Vector3(.08f, .003f, .15f), Quaternion.identity); size = new Vector3(.14f, .06f, .2f); break;
                    case 2: m.Prism(metal, new Vector3(-.4f, .02f, 0), Vector3.right, .02f, .8f, 5); size = new Vector3(.8f, .06f, .08f); break;
                    case 3: m.Blob(c, new Vector3(0, .07f, 0), new Vector3(.16f, .07f, .1f), prng, .1f); m.Blob(GlowM(Neon[2], 2f), new Vector3(0, .14f, 0), Vector3.one * .04f, prng, 0f); size = new Vector3(.34f, .16f, .22f); break;
                    case 4: m.Box(c, new Vector3(0, .015f, 0), new Vector3(.11f, .03f, .28f), Quaternion.identity); m.Box(dark, new Vector3(0, .05f, .06f), new Vector3(.08f, .04f, .02f), Quaternion.identity); size = new Vector3(.14f, .08f, .3f); break;
                    default: m.Blob(WorldMaterials.Solid(new Color(.5f, .8f, .5f)), new Vector3(0, .12f, 0), new Vector3(.12f, .12f, .18f), prng, .05f); m.Blob(WorldMaterials.Solid(new Color(.5f, .8f, .5f)), new Vector3(0, .28f, .14f), Vector3.one * .08f, prng, 0f); m.Cone(WorldMaterials.Solid(new Color(.5f, .8f, .5f)), new Vector3(0, .1f, -.16f), .06f, .2f, 4); size = new Vector3(.26f, .38f, .44f); break;
                }
                var go = m.BuildSingle("Lost thing", plife.transform, false); go.transform.SetPositionAndRotation(tb + up * .82f + f.Right * (-1.9f + k * .76f), f.r * Quaternion.Euler(0, k * 25f, 0)); var car = Interact.Carry(go, things[k], .4f, g.y - 10f); var bc = car.GetComponent<BoxCollider>(); if (bc != null) { bc.size = size; bc.center = new Vector3(0, size.y / 2f, 0); }
            }
            Folk(G(g - f.Fwd * .8f), f.Fwd, Resident.Pose.Stand, new[] { "Is it yours? No? Take it anyway. Nobody's ever come back for anything.", "Forty-one single wellies this year. Not one pair. It's a mystery and I've made my peace with it.", "If you've lost your friends, we don't keep those. Try the flag." }, Reveller(Residents.Tool.None, true));
            ArtistBits.PointLight(ctx.Root, g + up * 2.1f + f.Fwd * .5f, new Color(1f, .85f, .6f), 10f, 1.5f); mk.Blob(GlowM(new Color(1f, .82f, .5f), 2.4f), g + up * 2.2f + f.Fwd * .5f, Vector3.one * .12f, prng, 0f);
            FlagSign(mk, f.P(4.8f, 0, 5f), f.Fwd, "LOST & FOUND\n(mostly lost)", Lime, 2.6f, .17f);
            Ring(g, 7f);
            Shot(ctx, "lost and found", f.P(-4f, 2.2f, 8f), g + up * 1.2f);
        }

        // ---------------------------------------------------------------- the Chai Tent
        void ChaiTent(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var g = ctx.OnGround(ChaiAt.x, ChaiAt.y); var f = new PlaceFrame(g, FaceTo(g, Nearest(wayP, g))); var rugs = new[] { new Color(.75f, .25f, .3f), new Color(.85f, .6f, .25f), new Color(.4f, .3f, .6f), new Color(.25f, .5f, .5f) };
            // a big low tent open on one side: a centre pole, a ring of side poles, a patchwork roof; rugs, cushions, low tables, lanterns
            const float R = 7f; mk.Prism(wood, g, up, .12f, 5f, 8); var cp = new GameObject("Tent pole"); cp.transform.SetParent(rootT, false); cp.transform.position = g + up * 2.5f; var cpc = cp.AddComponent<CapsuleCollider>(); cpc.height = 5f; cpc.radius = .14f;
            for (var k = 0; k < 10; k++)
            {
                var a0 = k * Mathf.PI / 5f; var a1 = (k + 1) * Mathf.PI / 5f; var p0 = g + new Vector3(Mathf.Cos(a0), 0, Mathf.Sin(a0)) * R; var p1 = g + new Vector3(Mathf.Cos(a1), 0, Mathf.Sin(a1)) * R; var m = WorldMaterials.Solid(rugs[k % rugs.Length], .3f);
                mk.Tri(m, g + up * 5f, p1 + up * 2.4f, p0 + up * 2.4f, g); mk.Tri(m, g + up * 5f, p0 + up * 2.4f, p1 + up * 2.4f, g + up * 9f); var pg = G(p0); mk.Prism(wood, pg, up, .06f, 2.4f + (g.y - pg.y), 6); ArtistBits.PostCollider(ctx.Root, pg, 2.4f, .07f);
                var open = Vector3.Dot(((p0 + p1) / 2f - g).normalized, f.Fwd) > .5f; if (!open) { mk.Box(canvas, (p0 + p1) / 2f + up * 1.2f, new Vector3(.05f, 2.4f, Vector3.Distance(p0, p1)), Quaternion.LookRotation(p1 - p0)); Col((p0 + p1) / 2f + up * 1.2f, new Vector3(.15f, 2.4f, Vector3.Distance(p0, p1)), Quaternion.LookRotation(p1 - p0)); }
                var lan = Vector3.Lerp(g + up * 5f, p0 + up * 2.4f, .6f) - up * .5f; mk.Blob(GlowM(k % 2 == 0 ? new Color(1f, .7f, .35f) : new Color(1f, .5f, .6f), 2.2f), lan, new Vector3(.16f, .22f, .16f), prng, 0f); if (k % 2 == 0) halos.Add(lan, 1f, new Color(1f, .7f, .4f), .6f);
            }
            for (var k = 0; k < 5; k++) mk.Box(WorldMaterials.Solid(rugs[k % rugs.Length]), g + f.r * new Vector3(-3f + (k % 3) * 3f, .03f + k * .025f, -2.5f + (k / 3) * 4f), new Vector3(3.4f, .02f, 2.4f), f.r * Quaternion.Euler(0, k * 17f, 0));
            ArtistBits.PointLight(ctx.Root, g + up * 2.6f, new Color(1f, .7f, .4f), 16f, 1.8f);
            for (var t = 0; t < 2; t++)
            {
                var tc = g + f.r * new Vector3(-2.6f + t * 5.2f, 0, -.6f); mk.Prism(wood, tc, up, .9f, .34f, 10); var tcc = new GameObject("Low table"); tcc.transform.SetParent(rootT, false); tcc.transform.position = tc + up * .17f; tcc.AddComponent<BoxCollider>().size = new Vector3(1.6f, .34f, 1.6f); mk.Prism(WorldMaterials.Solid(new Color(.8f, .6f, .3f), .7f), tc + up * .34f, up, .14f, .26f, 8); mk.Blob(GlowM(new Color(1f, .7f, .35f), 2.4f), tc + up * .68f, Vector3.one * .06f, prng, 0f);
                for (var k = 0; k < 4; k++) { var d = Quaternion.Euler(0, k * 90f + 45f + t * 20f, 0) * Vector3.forward; var c = tc + d * 1.55f; mk.Blob(WorldMaterials.Solid(rugs[(k + t) % rugs.Length]), c + up * .16f, new Vector3(.45f, .16f, .45f), prng, .08f); var seated = t == 0 && k == 1 || t == 1 && k == 2; if (seated) Folk(c + up * .32f, -d, Resident.Pose.Sit, new[] { "I came in for one cup on Friday. It's very hard to get up off these cushions.", "It's cardamom. Or it's the music. Something's doing it.", "Nobody in here knows who's playing. That's why we're in here." }, Reveller(Residents.Tool.Mug), null, .32f); else Interact.Seat(plife.transform, c + up * .36f, -d, "Sit on the cushion"); }
                for (var k = 0; k < 2; k++) { var cm = new MeshKit(); cm.Prism(WorldMaterials.Solid(k == 0 ? new Color(.9f, .85f, .7f) : new Color(.6f, .75f, .8f)), Vector3.zero, Vector3.up, .05f, .08f, 8, 1.2f); var go = cm.BuildSingle("Chai cup", plife.transform, false); go.transform.position = tc + up * .35f + new Vector3(.4f - k * .8f, 0, .35f); var car = Interact.Carry(go, "a cup of chai", .2f, g.y - 10f); var bc = car.GetComponent<BoxCollider>(); if (bc != null) { bc.size = new Vector3(.14f, .1f, .14f); bc.center = new Vector3(0, .05f, 0); } }
            }
            // the urn at the back and whoever keeps it; a gong to strike
            var ur = g - f.Fwd * 4.6f; mk.Box(wood, ur + up * .4f, new Vector3(2.2f, .8f, .7f), f.r); Col(ur + up * .4f, new Vector3(2.2f, .8f, .7f), f.r); mk.Prism(WorldMaterials.Solid(new Color(.8f, .6f, .3f), .8f), ur + up * .8f, up, .3f, .7f, 10); mk.Cone(WorldMaterials.Solid(new Color(.8f, .6f, .3f), .8f), ur + up * 1.5f, .3f, .25f, 10);
            Folk(G(ur - f.Fwd * .9f), f.Fwd, Resident.Pose.Work, new[] { "Sit. You look like you've been standing since Thursday.", "It's free. Everything out here costs nine pounds except this.", "Strike the gong if you like. It means nothing. People find that restful." }, Reveller(Residents.Tool.Mug, true)).workRate = .4f;
            var gp = g + f.r * new Vector3(4.6f, 0, -3.2f); foreach (var e in new[] { -.7f, .7f }) mk.Box(wood, gp + f.Right * e + up * .9f, new Vector3(.08f, 1.8f, .08f), f.r); mk.Box(wood, gp + up * 1.8f, new Vector3(1.6f, .08f, .08f), f.r); Col(gp + up * .9f, new Vector3(1.6f, 1.8f, .2f), f.r);
            var gm = new MeshKit(); gm.Prism(WorldMaterials.Solid(new Color(.85f, .65f, .3f), .85f), new Vector3(0, -.75f, -.03f), Vector3.forward, .55f, .06f, 14); var ggo = gm.BuildSingle("Chai gong", plife.transform, false); ggo.transform.SetPositionAndRotation(gp + up * 1.76f, f.r); var gs = ggo.AddComponent<Sway>(); gs.axis = Vector3.right; gs.degrees = 12f; gs.speed = 5f; gs.decay = .7f; gs.seconds = 6f;
            var gl = ArtistBits.PointLight(ctx.Root, gp + up * 1.2f + f.Fwd * .8f, new Color(1f, .75f, .4f), 9f, 0f); var gt = Interact.Toy(ggo, Toy.Kind.Custom, "Strike the gong", new Vector3(1.3f, 1.3f, .8f)); ggo.GetComponent<BoxCollider>().center = new Vector3(0, -.75f, 0); gt.onUse = _ => { gs.Push(); plife.Flash(gl, 4f); Say(plife.transform, gp + up * 2.6f, "Bwwwooommm. Everyone in the tent breathes out.", 4f); };
            FlagSign(mk, f.P(6f, 0, 10f), f.Fwd, "CHAI TENT\nsit down, you", new Color(.85f, .5f, .2f), 2.4f, .18f);
            Ring(g, 10f);
            Shot(ctx, "the chai tent", f.P(-5f, 2.6f, 12f), g + up * 1.6f);
        }
    }

    /// The Electric places' moving parts: the silent disco (dark until you listen), the carousel, the gong's flash.
    public class ElectricPlacesLife : MonoBehaviour
    {
        public List<(Material m, Color c, int k)> discoTiles = new List<(Material, Color, int)>(); public Light discoLight; public bool listening; float heard;
        public Transform carousel; public List<(Transform t, Vector3 rest, float phase)> creatures = new List<(Transform, Vector3, float)>();
        readonly List<(Light l, float peak, float t0)> flashes = new List<(Light, float, float)>();
        public void Flash(Light l, float peak) { if (l == null) return; flashes.RemoveAll(f => f.l == l); flashes.Add((l, peak, Time.time)); }

        void Update()
        {
            var t = Time.time;
            heard = Mathf.MoveTowards(heard, listening ? 1f : 0f, Time.deltaTime * 1.5f);
            foreach (var (m, c, k) in discoTiles) { var beat = Mathf.Max(0f, Mathf.Sin(t * 8.4f + k * 1.3f)); m.SetColor("_EmissionColor", c * (.05f + heard * (.6f + beat * 2.6f))); }
            if (discoLight != null) { discoLight.intensity = .5f + heard * (1.6f + Mathf.Max(0f, Mathf.Sin(t * 8.4f)) * 1.4f); discoLight.color = Color.HSVToRGB(Mathf.Repeat(t * .12f, 1f), .6f, 1f); }
            if (carousel != null) carousel.Rotate(0, 22f * Time.deltaTime, 0, Space.World);
            foreach (var (tr, rest, ph) in creatures) tr.localPosition = rest + Vector3.up * Mathf.Sin(t * 1.6f + ph) * .35f;
            for (var i = flashes.Count - 1; i >= 0; i--) { var f = flashes[i]; var v = f.peak * Mathf.Exp(-(t - f.t0) * 1.6f); f.l.intensity = v; if (v < .02f) { f.l.intensity = 0f; flashes.RemoveAt(i); } }
        }
    }
}
