using System.Collections.Generic;
using UnityEngine;
using static EverythingWorld.World.PlaceTools;

namespace EverythingWorld.World
{
    /// The Folk Woods made to settle into (the build-land standard, done gently: this land was already full).
    /// Everything stays where it was. Three more lantern trails reach the quiet parts (north-east, north, south),
    /// and along them: the Fire Tower (climb the flights to the cabin at the top and sit over the whole wood); the
    /// Orchard (rows of apple trees, a basket of apples to carry off, a cider press to turn); the Beekeeper's Glade
    /// (white hives, a honey stall glowing amber); the Storytelling Ring (stumps round a little fire, a teller of
    /// tall tales); and the Woodcarver's Shed (carved animals, a whirligig to spin, a rocking horse to sit on).
    /// You can sit on the logs round every campfire, and a few folk keep the Great Tree company.
    public partial class FolkKit
    {
        static readonly Vector2 TowerAt = new Vector2(6080, 4502), OrchardAt = new Vector2(5950, 4398), BeesAt = new Vector2(5832, 4386), RingAt = new Vector2(5705, 4428), CarverAt = new Vector2(5598, 3928);
        static readonly Vector2[] NorthEast = { new Vector2(5825, 4315), new Vector2(5862, 4348), new Vector2(5925, 4375), new Vector2(6000, 4430), new Vector2(6062, 4488) };
        static readonly Vector2[] North = { new Vector2(5690, 4345), new Vector2(5698, 4390), new Vector2(5712, 4410) };
        static readonly Vector2[] South = { new Vector2(5640, 3990), new Vector2(5612, 3955), new Vector2(5605, 3945) };
        static readonly (Vector2 at, float r)[] PlaceRings = { (TowerAt, 9f), (OrchardAt, 16f), (BeesAt, 10f), (RingAt, 9f), (CarverAt, 9f) };

        FolkPlacesLife plife; System.Random prng; Transform rootT; readonly List<Vector3> roadPts = new List<Vector3>();

        static readonly Color[] Knits = { new Color(.6f, .3f, .22f), new Color(.75f, .65f, .4f), new Color(.3f, .42f, .32f), new Color(.35f, .4f, .55f), new Color(.85f, .8f, .68f), new Color(.55f, .35f, .45f) };
        static readonly Color[] Cords = { new Color(.35f, .28f, .22f), new Color(.28f, .32f, .4f), new Color(.45f, .4f, .3f) };
        static readonly Residents.Hat[] Hats = { Residents.Hat.None, Residents.Hat.None, Residents.Hat.Beanie, Residents.Hat.Straw, Residents.Hat.Cap };
        static readonly string[] Woodsfolk =
        {
            "Morning. It's always morning here; the mist likes it.", "Mind the train. It's small, but it means it.", "Every one of these songs is somebody's whole life in three minutes.",
            "The Giants were here before the songs. They're good listeners.", "Sit by a fire. Any fire. They're all ours.",
        };

        Residents.Look Look(Residents.Tool tool = Residents.Tool.None, bool apron = false) { var l = Residents.Random(prng, Knits, Cords, Hats, Knits); l.tool = tool; l.apron = apron; return l; }
        Resident Folk(Vector3 feet, Vector3 face, Resident.Pose pose, string[] lines = null, Residents.Look look = null, Vector3[] route = null, float seatH = HumanScale.SeatHeight)
            => Residents.Make(plife.transform, feet, face, look ?? Look(), pose, prng, lines ?? Woodsfolk, route, seatH);
        void Ring(Vector3 at, float r) => noGrass.Add(new Vector3(at.x, r, at.z));
        void Col(Vector3 c, Vector3 size, Quaternion rot) => BoxCol(rootT, c, size, rot);

        void Carved(LandContext ctx, MeshKit mk, Vector3 at, Vector3 face, string text, float w = 2.4f, float size = .17f)
        {
            var up = Vector3.up; var g = ctx.OnGround(at.x, at.z); var rot = Quaternion.LookRotation(face);
            foreach (var e in new[] { -1f, 1f }) mk.Prism(logM, g + rot * Vector3.right * e * (w / 2f - .15f) - up * .2f, up, .1f, 2f, 6);
            mk.Box(wood, g + up * 1.6f, new Vector3(w, .7f, .1f), rot); Col(g + up * 1f, new Vector3(w, 2f, .2f), rot);
            ArtistBits.Label(ctx.Root, text, g + up * 1.62f + face * .06f, face, size, Ink, TextAnchor.MiddleCenter);
        }

        void StumpSeat(LandContext ctx, MeshKit mk, Vector3 at, Vector3 face, float h = .5f)
        {
            var g = ctx.OnGround(at.x, at.z); mk.Prism(logM, g - Vector3.up * .2f, Vector3.up, .32f, h + .2f, 8); ArtistBits.PostCollider(ctx.Root, g, h, .32f);
            Interact.Seat(plife.transform, g + Vector3.up * (h + .05f), face, "Sit on the stump").seatHeight = h;
        }

        // ---------------------------------------------------------------- the trails
        void Bend(LandContext ctx, List<Vector3> pts)
        {
            for (var i = 0; i < pts.Count; i++)
            {
                foreach (var a in ctx.Artists) { var r = Foot(a) + 7f; var v = new Vector2(pts[i].x - a.x, pts[i].z - a.z); var d = v.magnitude; if (d < r) { v = d > .01f ? v / d * r : new Vector2(r, 0); pts[i] = ctx.OnGround(a.x + v.x, a.z + v.y); } }
                foreach (var c in noGrass) { if (c.y < 4f) continue; var r = c.y + 3f; var v = new Vector2(pts[i].x - c.x, pts[i].z - c.z); var d = v.magnitude; if (d < r) { v = d > .01f ? v / d * r : new Vector2(r, 0); pts[i] = ctx.OnGround(c.x + v.x, c.z + v.y); } }
            }
        }

        void LanternTrail(LandContext ctx, MeshKit mk, Vector2[] way)
        {
            Vector2 near = way[0]; var bd = 150f * 150f; foreach (var (p, _) in walkPoints) { var d = (p.x - way[0].x) * (p.x - way[0].x) + (p.z - way[0].y) * (p.z - way[0].y); if (d < bd) { bd = d; near = new Vector2(p.x, p.z); } }
            var full = new List<Vector2> { near }; full.AddRange(way);
            var pts = Smooth(ctx, full, false, 2f); Bend(ctx, pts); if (pts.Count < 2) return;
            Strip(mk, pts, WorldMaterials.Solid(new Color(.62f, .5f, .34f)), 2.6f, .05f, ctx);
            var up = Vector3.up; var n = 0;
            for (var i = 7; i < pts.Count; i += 13) { var d = pts[i] - pts[i - 1]; d.y = 0; if (d.sqrMagnitude < .01f) continue; d.Normalize(); var lp = pts[i] + Vector3.Cross(up, d) * (n++ % 2 == 0 ? 2f : -2f); if (ctx.IsLand(lp.x, lp.z, 2f)) Lantern(ctx, mk, ctx.OnGround(lp.x, lp.z)); }
            foreach (var p in pts) Remember(p, 700);
            roadPts.AddRange(pts);
        }

        void Lane(LandContext ctx, MeshKit mk, Vector3 to, float stopShort = 0f)
        {
            var from = Nearest(roadPts, to); var d = new Vector2(to.x - from.x, to.z - from.z); var len = d.magnitude; if (len < stopShort + 3f) return; d /= len;
            var a = new Vector2(from.x, from.z) + d * 1.4f; var b = new Vector2(from.x, from.z) + d * (len - stopShort);
            var pts = Smooth(ctx, new[] { a, Vector2.Lerp(a, b, .5f) + new Vector2(-d.y, d.x) * Mathf.Min(5f, len * .1f), b }, false, 2f, 1f); if (pts.Count < 2) return;
            Strip(mk, pts, WorldMaterials.Solid(new Color(.62f, .5f, .34f)), 2f, .045f, ctx); foreach (var p in pts) Remember(p, 900);
        }

        void WoodsPlaces(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            roadPts.Clear(); rootT = ctx.Root; prng = new System.Random(1971);
            plife = new GameObject("Folk places life").AddComponent<FolkPlacesLife>(); plife.transform.SetParent(life.transform, false);
            foreach (var (at, r) in PlaceRings) noGrass.Add(new Vector3(at.x, r, at.y));
            LanternTrail(ctx, mk, NorthEast); LanternTrail(ctx, mk, North); LanternTrail(ctx, mk, South);
            FireTower(ctx, mk, solid); Orchard(ctx, mk, solid); Bees(ctx, mk, solid); StoryRing(ctx, mk, solid); Carver(ctx, mk, solid);
            // folk at the Great Tree
            var c = ctx.OnGround(center.x, center.z); var route = new Vector3[10]; for (var k = 0; k < 10; k++) { var a = k * 36f * Mathf.Deg2Rad; route[k] = ctx.OnGround(c.x + Mathf.Cos(a) * 15f, c.z + Mathf.Sin(a) * 15f); }
            Folk(route[0], FaceTo(route[0], route[1]), Resident.Pose.Walk, null, null, route);
            for (var k = 0; k < 8; k++) { var a = (k * 45f + 20f) * Mathf.Deg2Rad; var p = new Vector3(c.x + Mathf.Cos(a) * 18f, 0, c.z + Mathf.Sin(a) * 18f); if (!Open(ctx, p.x, p.z, 1.5f)) continue; var q = p + Vector3.Cross(Vector3.up, FaceTo(p, c)) * 1.3f; Folk(ctx.OnGround(p.x, p.z), FaceTo(p, q), Resident.Pose.Stand, new[] { "She says the harmonica on the stump plays itself at dawn. I say it's the wind. We're both right.", "We meet here every morning. Nobody arranged it." }); Folk(ctx.OnGround(q.x, q.z), FaceTo(q, p), Resident.Pose.Stand, new[] { "Have you been round the lake yet? Take the bridge back.", "I knit. He whittles. The Tree does the rest." }, Look(Residents.Tool.Mug)); break; }
        }

        // the Fire Tower: four flights round a timber tower to a cabin with a bench over the whole wood
        void FireTower(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var g = Site(ctx, TowerAt, 8f); var f = new PlaceFrame(g, FaceTo(g, center)); const float Core = 3.4f, SW = 1.4f, Rise = .3f; const int Per = 10, Flights = 4;
            var lo = g.y; for (var k = 0; k < 8; k++) lo = Mathf.Min(lo, ctx.Ground(g.x + Mathf.Cos(k * .785f) * 5f, g.z + Mathf.Sin(k * .785f) * 5f));
            var top = g.y + Flights * Per * Rise + .3f;
            foreach (var (x, z) in new[] { (-1f, -1f), (1f, -1f), (1f, 1f), (-1f, 1f) }) { var p = f.P(x * Core / 2f, 0, z * Core / 2f); mk.Box(logM, new Vector3(p.x, (lo + top) / 2f, p.z), new Vector3(.3f, top - lo + .6f, .3f), f.r); solid.Box(logM, new Vector3(p.x, (lo + top) / 2f, p.z), new Vector3(.3f, top - lo + .6f, .3f), f.r); }
            for (var y = 3f; y < top - g.y; y += 3f) foreach (var s in new[] { 0, 1, 2, 3 }) { var d = f.r * (Quaternion.Euler(0, s * 90f, 0) * Vector3.forward); mk.Box(logM, g + d * (Core / 2f) + up * y, new Vector3(Core, .14f, .14f), Quaternion.LookRotation(d)); }
            var n = 0;
            for (var side = 0; side < Flights; side++)
            {
                var d = f.r * (Quaternion.Euler(0, side * 90f, 0) * Vector3.forward); var along = f.r * (Quaternion.Euler(0, side * 90f + 90f, 0) * Vector3.forward);
                for (var k = 0; k < Per; k++, n++)
                {
                    var y = g.y + (n + 1) * Rise; var c = g + d * (Core / 2f + SW / 2f) + along * (-Core / 2f - SW / 2f + (k + .5f) * ((Core + SW) / Per)); var w = (Core + SW) / Per + .02f;
                    mk.Box(wood, new Vector3(c.x, y - .06f, c.z), new Vector3(SW, .12f, w), Quaternion.LookRotation(along)); solid.Box(wood, new Vector3(c.x, y - .15f, c.z), new Vector3(SW, .3f, w + .05f), Quaternion.LookRotation(along));
                    Col(new Vector3(c.x, y + .5f, c.z) + d * (SW / 2f), new Vector3(w + .05f, 1f, .1f), Quaternion.LookRotation(d));   // the outer rail
                    if (k % 3 == 0) mk.Box(logM, new Vector3(c.x, y + .5f, c.z) + d * (SW / 2f), new Vector3(.08f, 1f, .08f), f.r);
                }
                var cy = g.y + n * Rise; var corner = g + d * (Core / 2f + SW / 2f) + along * (Core / 2f + SW / 2f);
                mk.Box(wood, new Vector3(corner.x, cy - .06f, corner.z), new Vector3(SW, .12f, SW), f.r); solid.Box(wood, new Vector3(corner.x, cy - .15f, corner.z), new Vector3(SW, .3f, SW), f.r);
                Col(new Vector3(corner.x, cy + .5f, corner.z) + d * (SW / 2f), new Vector3(SW, 1f, .1f), Quaternion.LookRotation(d)); Col(new Vector3(corner.x, cy + .5f, corner.z) + along * (SW / 2f), new Vector3(SW, 1f, .1f), Quaternion.LookRotation(along));
                mk.Box(logM, new Vector3(corner.x, (lo + cy) / 2f, corner.z) + (d + along) * (SW / 2f - .1f), new Vector3(.2f, cy - lo, .2f), f.r);
            }
            // the cabin at the top: a deck, a roof on four posts, a bench, a lantern
            var deck = new Vector3(g.x, top, g.z); mk.Box(wood, deck - up * .08f, new Vector3(Core + .2f, .16f, Core + .2f), f.r); solid.Box(wood, deck - up * .15f, new Vector3(Core + .2f, .3f, Core + .2f), f.r);
            mk.GableRoof(WorldMaterials.Solid(new Color(.3f, .4f, .32f)), deck + up * 2.6f, new Vector3(Core + 1.2f, 0, Core + 1.2f), 1.2f, f.r);
            foreach (var (x, z) in new[] { (-1f, -1f), (1f, -1f), (1f, 1f), (-1f, 1f) }) mk.Box(logM, deck + f.r * new Vector3(x * Core / 2f, 1.3f, z * Core / 2f), new Vector3(.2f, 2.6f, .2f), f.r);
            foreach (var s in new[] { 0, 1, 2 }) { var d = f.r * (Quaternion.Euler(0, s * 90f + 180f, 0) * Vector3.forward); Col(deck + d * (Core / 2f) + up * .5f, new Vector3(Core, 1f, .1f), Quaternion.LookRotation(d)); mk.Box(logM, deck + d * (Core / 2f) + up * 1f, new Vector3(Core, .1f, .1f), Quaternion.LookRotation(d)); }
            mk.Box(wood, deck + up * .45f - f.Fwd * .2f, new Vector3(1.8f, .07f, .45f), f.r); Interact.Bench(plife.transform, deck + up * .5f - f.Fwd * .2f - f.Right * .6f, deck + up * .5f - f.Fwd * .2f + f.Right * .6f, f.Fwd);
            mk.Box(GlowM(Warm, 2.2f), deck + up * 2.3f, new Vector3(.22f, .32f, .22f), Quaternion.identity); halos.Add(deck + up * 2.3f, 1.4f, Warm, .7f); ArtistBits.PointLight(ctx.Root, deck + up * 2f, Warm, 14f, 1.4f);
            Folk(f.P(-4.5f, 0, 4f), f.Fwd, Resident.Pose.Stand, new[] { "Forty steps. You can count the Giants from the top: one, two, big.", "No fires today. Just the friendly ones.", "On a still morning you can hear the train from up there, and somebody's guitar." },
                new Residents.Look { top = new Color(.35f, .45f, .3f), bottom = new Color(.3f, .32f, .25f), hat = Residents.Hat.Bucket, hatColor = new Color(.5f, .42f, .28f), skin = new Color(.75f, .55f, .4f), hair = new Color(.2f, .15f, .1f) });
            Carved(ctx, mk, f.P(4.5f, 0, 5f), f.Fwd, "FIRE TOWER\nforty steps", 2.4f);
            Lane(ctx, mk, f.P(0, 0, 6f)); Ring(g, 7f);
            Shot(ctx, "the fire tower", f.P(9f, 3f, 13f), g + up * 6f);
            Shot(ctx, "the view from the fire tower", deck + up * 1.5f - f.Fwd * .2f, deck + f.Fwd * 60f - up * 8f);
        }

        // the Orchard: rows of apple trees, a basket of apples, a cider press
        void Orchard(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var g = ctx.OnGround(OrchardAt.x, OrchardAt.y); var f = new PlaceFrame(g, FaceTo(g, Nearest(roadPts, g))); var apple = WorldMaterials.Solid(new Color(.85f, .15f, .12f)); var crown = WorldMaterials.Solid(new Color(.4f, .58f, .3f));
            for (var x = -1; x <= 1; x++) for (var z = 0; z < 3; z++)
            {
                var p = ctx.OnGround(f.P(x * 7f, 0, -4f - z * 6f).x, f.P(x * 7f, 0, -4f - z * 6f).z); mk.Prism(bark, p - up * .2f, up, .22f, 2.4f, 6); ArtistBits.PostCollider(ctx.Root, p, 2.4f, .25f);
                soft.Blob(crown, p + up * 3.4f, new Vector3(2.2f, 1.6f, 2.2f), ctx.Rng, .25f); for (var k = 0; k < 7; k++) soft.Blob(apple, p + up * (2.6f + (k % 3) * .5f) + Quaternion.Euler(0, k * 51f, 0) * Vector3.forward * 1.8f, Vector3.one * .12f, ctx.Rng, 0f);
            }
            // the basket of apples by the press
            var bk = f.P(2.2f, 0, 2f); bk = ctx.OnGround(bk.x, bk.z); mk.Prism(WorldMaterials.Solid(new Color(.7f, .55f, .35f)), bk, up, .4f, .35f, 10, 1.15f); ArtistBits.PostCollider(ctx.Root, bk, .35f, .45f);
            for (var k = 0; k < 6; k++) { var am = new MeshKit(); am.Blob(apple, Vector3.zero, Vector3.one * .1f, prng, .05f); am.Box(bark, new Vector3(0, .11f, 0), new Vector3(.02f, .05f, .02f), Quaternion.identity); var go = am.BuildSingle("Apple", plife.transform, false); go.transform.position = bk + up * (.42f + (k / 3) * .14f) + Quaternion.Euler(0, k * 120f, 0) * Vector3.forward * .16f; Interact.Carry(go, "an apple", .2f, g.y - 6f).GetComponent<Rigidbody>().isKinematic = true; }
            // the cider press: a frame, a tub, a screw you can turn
            var pr = f.P(-1.5f, 0, 2f); pr = ctx.OnGround(pr.x, pr.z); foreach (var e in new[] { -.8f, .8f }) mk.Box(logM, pr + f.Right * e + up * 1.1f, new Vector3(.18f, 2.2f, .18f), f.r); mk.Box(logM, pr + up * 2.15f, new Vector3(2f, .2f, .3f), f.r); mk.Prism(wood, pr, up, .6f, .8f, 12); Col(pr + up * 1.1f, new Vector3(2f, 2.2f, 1.2f), f.r);
            var sc = new MeshKit(); sc.Prism(metal, new Vector3(0, -1.1f, 0), Vector3.up, .06f, 1.3f, 6); sc.Box(wood, new Vector3(0, .2f, 0), new Vector3(1.4f, .08f, .08f), Quaternion.identity); var sgo = sc.BuildSingle("Cider press screw", plife.transform, false); sgo.transform.position = pr + up * 2.3f; var stt = Interact.Toy(sgo, Toy.Kind.Spin, "Turn the press", new Vector3(1.6f, .6f, 1.6f)); stt.amount = 720f; stt.seconds = 3f;
            foreach (var e in new[] { -3.4f, -4.4f }) { var b = ctx.OnGround(f.P(e, 0, 1.6f).x, f.P(e, 0, 1.6f).z); mk.Prism(wood, b, up, .42f, .9f, 10, .9f); ArtistBits.PostCollider(ctx.Root, b, .9f, .45f); }
            StumpSeat(ctx, mk, f.P(4f, 0, 3.4f), -f.Right); StumpSeat(ctx, mk, f.P(5.2f, 0, 2.4f), -f.Right);
            Folk(f.P(.6f, 0, 3.6f), f.Fwd, Resident.Pose.Stand, new[] { "Take an apple. Take two; the trees don't count.", "Cider's ready in the autumn. It's always nearly autumn here.", "Give the press a turn. It likes the company." }, Look(Residents.Tool.None, true));
            Lantern(ctx, mk, ctx.OnGround(f.P(-5.5f, 0, 4f).x, f.P(-5.5f, 0, 4f).z)); ArtistBits.PointLight(ctx.Root, g + up * 3f + f.Fwd * 2f, Warm, 13f, 1.2f);
            Carved(ctx, mk, f.P(6f, 0, 8f), f.Fwd, "THE ORCHARD", 2.2f);
            Lane(ctx, mk, f.P(0, 0, 7f)); Ring(g, 6f);
            Shot(ctx, "the orchard", f.P(-8f, 2.2f, 11f), g + up * 1.5f - f.Fwd * 4f);
        }

        // the Beekeeper's Glade
        void Bees(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var g = Site(ctx, BeesAt, 8f); var f = new PlaceFrame(g, FaceTo(g, Nearest(roadPts, g))); var hive = WorldMaterials.Solid(new Color(.96f, .95f, .9f));
            for (var k = 0; k < 6; k++)
            {
                var p = ctx.OnGround(f.P(-5f + k * 2f, 0, -4f - (k % 2) * 1.6f).x, f.P(-5f + k * 2f, 0, -4f - (k % 2) * 1.6f).z); mk.Box(logM, p + up * .2f, new Vector3(.7f, .4f, .7f), f.r);
                for (var s = 0; s < 2 + k % 2; s++) mk.Box(hive, p + up * (.6f + s * .42f), new Vector3(.6f, .4f, .6f), f.r); mk.Box(WorldMaterials.Solid(new Color(.5f, .52f, .5f)), p + up * (.6f + (2 + k % 2) * .42f - .16f), new Vector3(.7f, .08f, .7f), f.r); Col(p + up * .8f, new Vector3(.7f, 1.6f, .7f), f.r);
                for (var b = 0; b < 4; b++) mk.Blob(GlowM(new Color(1f, .8f, .2f), 1.6f), p + up * (1f + b * .3f) + Quaternion.Euler(0, b * 90f + k * 30f, 0) * Vector3.forward * (.7f + b * .15f), Vector3.one * .03f, ctx.Rng, 0f);
            }
            var st = ctx.OnGround(f.P(3f, 0, 1.5f).x, f.P(3f, 0, 1.5f).z); mk.Box(wood, st + up * .8f, new Vector3(2.2f, .08f, .9f), f.r); foreach (var e in new[] { -1f, 1f }) mk.Box(logM, st + up * .4f + f.Right * e, new Vector3(.1f, .8f, .8f), f.r); Col(st + up * .45f, new Vector3(2.2f, .9f, .9f), f.r);
            for (var k = 0; k < 6; k++) mk.Prism(GlowM(new Color(1f, .7f, .2f), 1.5f), st + up * .84f + f.Right * (-.8f + k * .32f), up, .08f, .2f, 8); halos.Add(st + up * 1f, 1.2f, new Color(1f, .7f, .2f), .5f);
            ArtistBits.Label(ctx.Root, "HONEY\n(leave a coin, or a song)", st + up * .5f + f.Fwd * .46f, f.Fwd, .09f, Ink, TextAnchor.MiddleCenter);
            Folk(f.P(-1f, 0, -1.6f), f.Fwd, Resident.Pose.Work, new[] { "They know me. They know you too, now. Don't worry, it's a good thing.", "Bees like a slow song. Don't we all.", "Clover honey, mostly. Bit of pine. Bit of banjo." },
                new Residents.Look { top = new Color(.96f, .95f, .9f), bottom = new Color(.96f, .95f, .9f), hat = Residents.Hat.Bucket, hatColor = new Color(.96f, .95f, .9f), skin = new Color(.88f, .68f, .52f), hair = new Color(.6f, .45f, .25f) }).workRate = .4f;
            StumpSeat(ctx, mk, f.P(5.2f, 0, 3f), -f.Right);
            ArtistBits.PointLight(ctx.Root, g + up * 2.6f, new Color(1f, .8f, .45f), 11f, 1.1f);
            Lane(ctx, mk, f.P(0, 0, 4f)); Ring(g, 7f);
            Shot(ctx, "the beekeeper's glade", f.P(5f, 1.8f, 9f), g + up * 1f - f.Fwd * 2f);
        }

        // the Storytelling Ring
        void StoryRing(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var g = Site(ctx, RingAt, 8f); var f = new PlaceFrame(g, FaceTo(g, Nearest(roadPts, g)));
            for (var k = 0; k < 8; k++) mk.Blob(stone, g + Quaternion.Euler(0, k * 45f, 0) * Vector3.forward * .8f + up * .15f, new Vector3(.25f, .18f, .22f), ctx.Rng, .3f);
            mk.Blob(GlowM(new Color(1f, .55f, .2f), 2.4f), g + up * .45f, new Vector3(.35f, .5f, .35f), ctx.Rng, .3f); var fl = ArtistBits.PointLight(ctx.Root, g + up * 1f, new Color(1f, .55f, .2f), 11f, 1.6f); fl.gameObject.AddComponent<FireFlicker>().baseIntensity = 1.6f; halos.Add(g + up * .5f, 1.8f, new Color(1f, .55f, .2f), .7f); ArtistBits.PostCollider(ctx.Root, g, .5f, .9f); life.fires.Add(g + up * .25f);
            for (var k = 0; k < 9; k++) { var a = k * 40f; if (k == 0) continue; var d = f.r * (Quaternion.Euler(0, a, 0) * -Vector3.forward); StumpSeat(ctx, mk, g + d * 4f, -d, .45f); }
            var td = f.r * -Vector3.forward; var tp = ctx.OnGround(g.x + td.x * 4f, g.z + td.z * 4f); mk.Prism(logM, tp - up * .2f, up, .5f, .85f, 9); ArtistBits.PostCollider(ctx.Root, tp, .65f, .5f);
            Folk(tp + up * .68f, -td, Resident.Pose.Sit, new[]
            {
                "Sit down, sit down. This one's true. Mostly.", "There was a train, see, that only ran when somebody was singing.", "And a ship. Big one. The lake wasn't always this small.",
                "My grandfather walked here from a song nobody remembers the words to.", "And that's why the Giants are so tall. ...I've lost my place. Start again?",
            }, new Residents.Look { top = new Color(.6f, .3f, .22f), bottom = new Color(.35f, .28f, .22f), skin = new Color(.75f, .55f, .4f), hair = new Color(.85f, .85f, .82f), longHair = true }, null, .65f);
            Carved(ctx, mk, f.P(4.5f, 0, 6f), f.Fwd, "STORIES\ntold nightly, and by day", 2.8f, .14f);
            Lane(ctx, mk, f.P(0, 0, 6f)); Ring(g, 6f);
            Shot(ctx, "the storytelling ring", f.P(-5f, 1.8f, 9f), g + up * .8f);
        }

        // the Woodcarver's Shed
        void Carver(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var g = Site(ctx, CarverAt, 8f); var f = new PlaceFrame(g, FaceTo(g, Nearest(roadPts, g)));
            foreach (var (x, z, h) in new[] { (-2.6f, -1.6f, 2.4f), (2.6f, -1.6f, 2.4f), (-2.6f, 1.6f, 3f), (2.6f, 1.6f, 3f) }) { var p = ctx.OnGround(f.P(x, 0, z).x, f.P(x, 0, z).z); mk.Box(logM, p + up * h / 2f, new Vector3(.2f, h, .2f), f.r); ArtistBits.PostCollider(ctx.Root, p, h, .12f); }
            mk.Box(WorldMaterials.Solid(new Color(.35f, .42f, .32f)), f.P(0, 2.8f, 0), new Vector3(6f, .12f, 4.2f), f.r * Quaternion.Euler(10f, 0, 0)); mk.Box(wood, f.P(0, 1.2f, -1.6f), new Vector3(4.98f, 2.4f, .1f), f.r); Col(f.P(0, 1.2f, -1.6f), new Vector3(5f, 2.4f, .15f), f.r);
            mk.Box(wood, f.P(-.6f, .9f, -1f), new Vector3(3f, .1f, .9f), f.r); foreach (var e in new[] { -2f, .8f }) mk.Box(logM, f.P(e, .45f, -1f), new Vector3(.12f, .9f, .8f), f.r); Col(f.P(-.6f, .5f, -1f), new Vector3(3f, 1f, .9f), f.r);
            // carved animals on the bench and round the shed: a bear, an owl, a fish
            var carved = WorldMaterials.Solid(new Color(.72f, .56f, .38f));
            var bear = ctx.OnGround(f.P(-4.5f, 0, 1.5f).x, f.P(-4.5f, 0, 1.5f).z); mk.Blob(carved, bear + up * .8f, new Vector3(.6f, .8f, .5f), ctx.Rng, .1f); mk.Blob(carved, bear + up * 1.75f, Vector3.one * .38f, ctx.Rng, .05f); foreach (var e in new[] { -.25f, .25f }) mk.Blob(carved, bear + up * 2.1f + f.Right * e, Vector3.one * .12f, ctx.Rng, 0f); ArtistBits.PostCollider(ctx.Root, bear, 2f, .6f);
            var owl = f.P(-1.4f, 1.15f, -1f); mk.Blob(carved, owl, new Vector3(.16f, .22f, .14f), ctx.Rng, 0f); foreach (var e in new[] { -.06f, .06f }) mk.Blob(GlowM(new Color(1f, .8f, .3f), 1.6f), owl + up * .08f + f.Right * e + f.Fwd * .13f, Vector3.one * .03f, ctx.Rng, 0f);
            mk.Box(carved, f.P(.2f, 1.02f, -1f), new Vector3(.5f, .14f, .08f), f.r * Quaternion.Euler(0, 20f, 0)); mk.Box(carved, f.P(.5f, 1.02f, -.9f), new Vector3(.08f, .2f, .04f), f.r * Quaternion.Euler(0, 20f, 0));
            // a whirligig to spin, a rocking horse to sit on
            var wp = ctx.OnGround(f.P(4.5f, 0, 2f).x, f.P(4.5f, 0, 2f).z); mk.Prism(logM, wp - up * .2f, up, .06f, 2.2f, 6); ArtistBits.PostCollider(ctx.Root, wp, 2f, .1f);
            var wh = new MeshKit(); for (var k = 0; k < 4; k++) wh.Box(WorldMaterials.Solid(new[] { new Color(.85f, .2f, .2f), new Color(.95f, .8f, .25f), new Color(.25f, .5f, .75f), new Color(.3f, .6f, .35f) }[k]), Quaternion.Euler(0, 0, k * 90f) * new Vector3(0, .4f, 0), new Vector3(.22f, .6f, .03f), Quaternion.Euler(0, 0, k * 90f) * Quaternion.Euler(0, 30f, 0)); wh.Prism(carved, new Vector3(0, 0, -.05f), Vector3.forward, .08f, .1f, 8);
            var wgo = wh.BuildSingle("Whirligig", plife.transform, false); wgo.transform.SetPositionAndRotation(wp + up * 2.1f + f.Fwd * .1f, f.r); var wt = Interact.Toy(wgo, Toy.Kind.Spin, "Spin the whirligig", new Vector3(1.4f, 1.4f, .5f)); wt.axis = Vector3.forward; wt.amount = 1440f; wt.seconds = 3.5f;
            var hp = ctx.OnGround(f.P(2f, 0, 3f).x, f.P(2f, 0, 3f).z); var hr = Quaternion.LookRotation(f.Right);
            mk.Box(carved, hp + up * .8f, new Vector3(.4f, .45f, 1.1f), hr); mk.Box(carved, hp + up * 1.2f + f.Right * .55f, new Vector3(.25f, .6f, .3f), hr * Quaternion.Euler(-25f, 0, 0)); mk.Box(carved, hp + up * 1.5f + f.Right * .75f, new Vector3(.22f, .25f, .45f), hr); foreach (var e in new[] { -.22f, .22f }) mk.Box(WorldMaterials.Solid(new Color(.75f, .2f, .18f)), hp + up * .12f + f.Fwd * e, new Vector3(.08f, .1f, 1.7f), hr); foreach (var z in new[] { -.4f, .4f }) mk.Box(carved, hp + up * .4f + f.Right * z, new Vector3(.3f, .5f, .1f), hr);
            Col(hp + up * .6f, new Vector3(.5f, 1.2f, 1.7f), hr); Interact.Seat(plife.transform, hp + up * 1.08f, f.Right, "Sit on the rocking horse").seatHeight = .8f;
            Folk(f.P(-.6f, 0, .1f), -f.Fwd, Resident.Pose.Work, new[] { "Every block's got an animal in it. I just let it out.", "The bear took a winter. The owl took an afternoon; owls are quick.", "Go on, have a sit on the horse. He's steadier than he looks." }, Look(Residents.Tool.Rake, true)).workRate = .9f;
            Lantern(ctx, mk, ctx.OnGround(f.P(-4.5f, 0, 4f).x, f.P(-4.5f, 0, 4f).z)); ArtistBits.PointLight(ctx.Root, f.P(0, 2.2f, 1f), Warm, 12f, 1.3f);
            Carved(ctx, mk, f.P(5.5f, 0, 6f), f.Fwd, "WOODCARVER", 2.2f);
            Lane(ctx, mk, f.P(0, 0, 5.5f)); Ring(g, 7f);
            Shot(ctx, "the woodcarver's shed", f.P(-3f, 1.8f, 9f), g + up * 1.2f);
        }
    }

    public class FolkPlacesLife : MonoBehaviour { }
}
