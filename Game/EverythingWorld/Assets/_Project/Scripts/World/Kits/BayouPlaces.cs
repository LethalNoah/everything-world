using System.Collections.Generic;
using UnityEngine;
using static EverythingWorld.World.PlaceTools;

namespace EverythingWorld.World
{
    /// The Southern Rock Bayou laid out on purpose (the build-land standard). Everything that was here stays where it
    /// was; three dirt roads with lanterns run out into the empty parts, each keeping to one bank (no new bridges):
    /// south down the west bank to the tip of the land, south down the east bank to the river mouth, and north up
    /// the narrow strip in the north-west. Along them, places to settle, with the people who live on the bayou:
    /// the Fish Camp at the south tip (a crawfish boil on a long table, a dock, lawn chairs, string lights);
    /// the General Store (a porch with rocking chairs, an ice chest, a tyre swing to push);
    /// the Smokehouse (a pit smoking, picnic tables, a horseshoe pit with horseshoes to throw);
    /// the Airboat (sit in it, spin its fan) on the east bank; the Church in the Wildwood (white boards, a bell to
    /// ring, dinner on the grounds); and the Still in the woods (a copper still, jugs to carry off, a sleeping hound).
    /// You can sit in the juke joint's rocking chairs too.
    public partial class BayouKit
    {
        static readonly Vector2 CampAt = new Vector2(5965, 2440), StoreAt = new Vector2(5948, 2610), SmokeAt = new Vector2(6348, 2640), AirboatAt = new Vector2(6205, 2520), ChurchAt = new Vector2(5850, 3056), StillAt = new Vector2(5748, 3296);
        static readonly Vector2[] WestBank = { new Vector2(5990, 2700), new Vector2(5985, 2620), new Vector2(6000, 2540), new Vector2(5985, 2470), new Vector2(5955, 2428) };
        static readonly Vector2[] EastBank = { new Vector2(6330, 2700), new Vector2(6330, 2640), new Vector2(6300, 2560), new Vector2(6260, 2500), new Vector2(6228, 2466) };
        static readonly Vector2[] NorthWest = { new Vector2(5880, 2900), new Vector2(5900, 2980), new Vector2(5860, 3050), new Vector2(5805, 3140), new Vector2(5785, 3220), new Vector2(5745, 3295) };
        static readonly (Vector2 at, float r)[] PlaceRings = { (CampAt, 15f), (StoreAt, 11f), (SmokeAt, 13f), (AirboatAt, 8f), (ChurchAt, 13f), (StillAt, 9f) };

        BayouPlacesLife plife; System.Random prng; Transform rootT;
        readonly List<Vector3> roadPts = new List<Vector3>(); readonly List<Vector3> glades = new List<Vector3>();

        static readonly Color[] Shirts = { new Color(.3f, .4f, .6f), new Color(.75f, .25f, .2f), new Color(.85f, .8f, .65f), new Color(.35f, .5f, .3f), new Color(.2f, .2f, .22f), new Color(.85f, .6f, .2f) };
        static readonly Color[] Jeans = { new Color(.25f, .35f, .55f), new Color(.2f, .28f, .45f), new Color(.5f, .42f, .3f) };
        static readonly Residents.Hat[] Hats = { Residents.Hat.Cap, Residents.Hat.Cap, Residents.Hat.Straw, Residents.Hat.None, Residents.Hat.Cowboy };
        static readonly string[] Neighbours =
        {
            "Evenin'. It's always evenin' here; saves on clocks.", "River's low. Stories are tall. Evens out.", "Set a while. Nothin' out there's goin' anywhere.",
            "Mind the gator. He minds you.", "That's a good song. They're all good songs. That's why we're still here.",
        };

        Residents.Look Look(Residents.Tool tool = Residents.Tool.None, bool apron = false) { var l = Residents.Random(prng, Shirts, Jeans, Hats, Shirts); l.tool = tool; l.apron = apron; return l; }
        Resident Folk(Vector3 feet, Vector3 face, Resident.Pose pose, string[] lines = null, Residents.Look look = null, Vector3[] route = null, float seatH = HumanScale.SeatHeight)
            => Residents.Make(plife.transform, feet, face, look ?? Look(), pose, prng, lines ?? Neighbours, route, seatH);
        void Ring(Vector3 at, float r) { noGrass.Add(new Vector3(at.x, r, at.z)); glades.Add(new Vector3(at.x, r + 10f, at.z)); }
        void Col(Vector3 c, Vector3 size, Quaternion rot) => BoxCol(rootT, c, size, rot);
        bool InGlade(float x, float z) { foreach (var g in glades) { float dx = x - g.x, dz = z - g.z; if (dx * dx + dz * dz < g.y * g.y) return true; } return false; }
        Vector3 Dry(LandContext ctx, Vector2 want, float inland) => Site(ctx, want, inland, (x, z) => !WaterAt(x, z, 6f, out _));

        void Painted(LandContext ctx, MeshKit mk, Vector3 at, Vector3 face, string text, float w = 2.4f, float size = .17f)
        {
            var up = Vector3.up; var g = ctx.OnGround(at.x, at.z); var rot = Quaternion.LookRotation(face);
            mk.Prism(greyWood, g - up * .2f, up, .08f, 2f, 6); mk.Box(white, g + up * 1.7f, new Vector3(w, .8f, .08f), rot * Quaternion.Euler(0, 0, 3f)); Col(g + up * 1f, new Vector3(w, 2f, .15f), rot);
            ArtistBits.Label(ctx.Root, text, g + up * 1.72f + face * .05f, face, size, new Color(.5f, .15f, .12f), TextAnchor.MiddleCenter).transform.rotation *= Quaternion.Euler(0, 0, -3f);
        }

        void LawnChair(LandContext ctx, MeshKit mk, Vector3 at, Vector3 face, int k)
        {
            var up = Vector3.up; var g = ctx.OnGround(at.x, at.z); var rot = Quaternion.LookRotation(face); var web = WorldMaterials.Solid(Shirts[k % Shirts.Length], .3f);
            mk.Box(web, g + up * .42f, new Vector3(.55f, .05f, .5f), rot); mk.Box(web, g + up * .78f - face * .25f, new Vector3(.55f, .6f, .05f), rot * Quaternion.Euler(-10f, 0, 0)); foreach (var e in new[] { -.28f, .28f }) mk.Box(metal, g + up * .32f + rot * Vector3.right * e, new Vector3(.03f, .64f, .5f), rot);
            Col(g + up * .45f - face * .1f, new Vector3(.6f, .9f, .6f), rot); Interact.Seat(plife.transform, g + up * .47f, face, "Sit in the lawn chair");
        }

        void PicnicTable(LandContext ctx, MeshKit mk, Vector3 at, Vector3 along)
        {
            var up = Vector3.up; var g = ctx.OnGround(at.x, at.z); var rot = Quaternion.LookRotation(along); var side = Vector3.Cross(up, along);
            mk.Box(plank, g + up * .75f, new Vector3(.9f, .06f, 2f), rot); foreach (var e in new[] { -.7f, .7f }) mk.Box(greyWood, g + up * .37f + along * e, new Vector3(.8f, .74f, .1f), rot);
            foreach (var s in new[] { -1f, 1f }) { mk.Box(plank, g + up * .45f + side * s * .7f, new Vector3(.3f, .05f, 2f), rot); Interact.Bench(plife.transform, g + up * .47f + side * s * .7f - along * .8f, g + up * .47f + side * s * .7f + along * .8f, -side * s); }
            Col(g + up * .4f, new Vector3(1.7f, .8f, 2f), rot);
        }

        // ---------------------------------------------------------------- the roads
        void Bend(LandContext ctx, List<Vector3> pts)
        {
            for (var i = 0; i < pts.Count; i++)
            {
                foreach (var a in ctx.Artists) { var r = Foot(a) + 8f; var v = new Vector2(pts[i].x - a.x, pts[i].z - a.z); var d = v.magnitude; if (d < r) { v = d > .01f ? v / d * r : new Vector2(r, 0); pts[i] = ctx.OnGround(a.x + v.x, a.z + v.y); } }
                foreach (var c in noGrass) { if (c.y < 4f) continue; var r = c.y + 4f; var v = new Vector2(pts[i].x - c.x, pts[i].z - c.z); var d = v.magnitude; if (d < r) { v = d > .01f ? v / d * r : new Vector2(r, 0); pts[i] = ctx.OnGround(c.x + v.x, c.z + v.y); } }
            }
        }

        void BankRoad(LandContext ctx, MeshKit mk, Vector2[] way)
        {
            Vector2 near = way[0]; var bd = 140f * 140f; foreach (var (p, _) in walkPoints) { var d = (p.x - way[0].x) * (p.x - way[0].x) + (p.z - way[0].y) * (p.z - way[0].y); if (d < bd) { bd = d; near = new Vector2(p.x, p.z); } }
            var full = new List<Vector2> { near }; full.AddRange(way);
            var pts = Smooth(ctx, full, false, 2.5f); Bend(ctx, pts); pts.RemoveAll(p => WaterAt(p.x, p.z, 2f, out _)); if (pts.Count < 2) return;
            Strip(mk, pts, WorldMaterials.Solid(new Color(.6f, .52f, .38f)), 3.6f, .05f);
            var up = Vector3.up; var n = 0;
            for (var i = 8; i < pts.Count; i += 16) { var d = pts[i] - pts[i - 1]; d.y = 0; if (d.sqrMagnitude < .01f) continue; d.Normalize(); var lp = pts[i] + Vector3.Cross(up, d) * (n++ % 2 == 0 ? 2.6f : -2.6f); if (ctx.IsLand(lp.x, lp.z, 2f) && !WaterAt(lp.x, lp.z, 2f, out _) && !InFootprint(ctx, lp.x, lp.z, 3f)) Lamp(ctx, mk, ctx.Root, ctx.OnGround(lp.x, lp.z)); }
            foreach (var p in pts) Remember(p, 700);
            roadPts.AddRange(pts);
        }

        void Lane(LandContext ctx, MeshKit mk, Vector3 to, float stopShort = 0f)
        {
            var from = Nearest(roadPts, to); var d = new Vector2(to.x - from.x, to.z - from.z); var len = d.magnitude; if (len < stopShort + 4f) return; d /= len;
            var a = new Vector2(from.x, from.z) + d * 1.9f; var b = new Vector2(from.x, from.z) + d * (len - stopShort); var side = new Vector2(-d.y, d.x);
            var pts = Smooth(ctx, new[] { a, Vector2.Lerp(a, b, .5f) + side * Mathf.Min(6f, len * .1f), b }, false, 2f, 1f); pts.RemoveAll(p => WaterAt(p.x, p.z, 1.5f, out _));
            if (pts.Count < 2) return;
            Strip(mk, pts, WorldMaterials.Solid(new Color(.52f, .45f, .33f)), 2.4f, .045f); foreach (var p in pts) Remember(p, 900);
        }

        void BayouPlaces(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            roadPts.Clear(); glades.Clear(); rootT = ctx.Root; prng = new System.Random(1973);
            plife = new GameObject("Bayou places life").AddComponent<BayouPlacesLife>(); plife.transform.SetParent(life.transform, false);
            foreach (var (at, r) in PlaceRings) noGrass.Add(new Vector3(at.x, r, at.y));
            BankRoad(ctx, mk, WestBank); BankRoad(ctx, mk, EastBank); BankRoad(ctx, mk, NorthWest);
            FishCamp(ctx, mk, solid); Store(ctx, mk, solid); Smokehouse(ctx, mk, solid); Airboat(ctx, mk, solid); Church(ctx, mk, solid); Still(ctx, mk, solid);
        }

        void Fire(LandContext ctx, MeshKit mk, Vector3 pit)
        {
            var up = Vector3.up; for (var k = 0; k < 8; k++) mk.Blob(WorldMaterials.Solid(new Color(.45f, .43f, .4f)), pit + Quaternion.Euler(0, k * 45f, 0) * Vector3.forward * .9f + up * .15f, new Vector3(.25f, .18f, .22f), ctx.Rng, .3f);
            mk.Blob(GlowM(new Color(1f, .55f, .2f), 2.6f), pit + up * .5f, new Vector3(.4f, .6f, .4f), ctx.Rng, .3f); var fl = ArtistBits.PointLight(ctx.Root, pit + up * 1.2f, new Color(1f, .6f, .25f), 13f, 2f); fl.gameObject.AddComponent<FireFlicker>().baseIntensity = 2f;
            halos.Add(pit + up * .6f, 2.2f, new Color(1f, .55f, .2f), .7f); ArtistBits.PostCollider(ctx.Root, pit, .8f, 1f);
        }

        // the Fish Camp: a crawfish boil, a dock, lawn chairs, string lights
        void FishCamp(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var g = Dry(ctx, CampAt, 8f);
            var toSea = Vector3.zero; for (var a = 0; a < 360; a += 15) { var d = Quaternion.Euler(0, a, 0) * Vector3.forward; if (ctx.Ground(g.x + d.x * 80f, g.z + d.z * 80f) < .3f) toSea += d; }
            if (toSea.sqrMagnitude < .01f) toSea = Vector3.back; var f = new PlaceFrame(g, toSea.normalized);
            // the long table under string lights, red with crawfish, a boil pot on a burner
            var t = ctx.OnGround(f.P(0, 0, 2f).x, f.P(0, 0, 2f).z); var tr = Quaternion.LookRotation(f.Right);
            mk.Box(plank, t + up * .8f, new Vector3(1.2f, .07f, 6f), tr); foreach (var e in new[] { -2.6f, 0f, 2.6f }) mk.Box(greyWood, t + up * .4f + f.Right * e, new Vector3(1f, .8f, .1f), tr); Col(t + up * .45f, new Vector3(1.2f, .9f, 6f), tr);
            mk.Box(white, t + up * .84f, new Vector3(1f, .01f, 5.6f), tr); for (var k = 0; k < 16; k++) mk.Blob(WorldMaterials.Solid(new Color(.85f, .2f, .12f)), t + up * .9f + f.Right * (-2.5f + k * .33f) + f.Fwd * (((k * 7) % 5) * .14f - .28f), new Vector3(.12f, .05f, .07f), ctx.Rng, .2f);
            for (var k = 0; k < 4; k++) mk.Prism(WorldMaterials.Solid(new Color(.95f, .85f, .3f)), t + up * .86f + f.Right * (-2f + k * 1.3f) + f.Fwd * .3f, f.Right, .05f, .3f, 6);
            foreach (var s in new[] { -1f, 1f }) { mk.Box(plank, t + up * .45f + f.Fwd * s * 1f, new Vector3(.3f, .05f, 5.6f), tr); mk.Box(greyWood, t + up * .22f + f.Fwd * s * 1f, new Vector3(.25f, .44f, 5f), tr); Col(t + up * .25f + f.Fwd * s * 1f, new Vector3(.3f, .5f, 5.6f), tr); if (s < 0) Interact.Bench(plife.transform, t + up * .5f + f.Fwd * s * 1f - f.Right * 2.4f, t + up * .5f + f.Fwd * s * 1f + f.Right * 2.4f, f.Fwd); else Interact.Bench(plife.transform, t + up * .5f + f.Fwd * s * 1f - f.Right * 2.4f, t + up * .5f + f.Fwd * s * 1f + f.Right * .6f, -f.Fwd); }
            Folk(t + up * .5f + f.Fwd * 1f + f.Right * 1.6f, -f.Fwd, Resident.Pose.Sit, new[] { "Pinch the tail, suck the head. Or just eat the corn; no shame.", "Crawfish are ready when they're ready. Sit." }, null, null, .5f);
            var pot = ctx.OnGround(f.P(-5f, 0, 1f).x, f.P(-5f, 0, 1f).z); mk.Prism(metal, pot + up * .45f, up, .5f, .8f, 12); mk.Prism(dark, pot, up, .35f, .45f, 6); mk.Blob(GlowM(new Color(1f, .5f, .2f), 2f), pot + up * .2f, new Vector3(.3f, .12f, .3f), ctx.Rng, .2f); ArtistBits.PostCollider(ctx.Root, pot, 1.2f, .55f);
            for (var k = 0; k < 3; k++) mk.Blob(WorldMaterials.Glow(new Color(.9f, .9f, .85f), .35f), pot + up * (1.5f + k * .5f) + new Vector3(Mathf.Sin(k * 2f) * .2f, 0, Mathf.Cos(k * 2f) * .2f), new Vector3(.35f, .2f, .35f) * (1f - k * .2f), ctx.Rng, .3f);
            Folk(pot + f.Right * -1.1f, f.Right, Resident.Pose.Work, new[] { "Secret's in the boil. And the boil's a secret.", "Forty pounds tonight. Feeds twelve, or my cousin." }, Look(Residents.Tool.Spatula, true)).workRate = .6f;
            for (var k = 0; k < 6; k++) { var a0 = k * 60f; var p0 = ctx.OnGround(g.x + Mathf.Cos(a0 * Mathf.Deg2Rad) * 9f, g.z + Mathf.Sin(a0 * Mathf.Deg2Rad) * 9f); mk.Prism(greyWood, p0 - up * .3f, up, .08f, 3.4f, 6); ArtistBits.PostCollider(ctx.Root, p0, 3.2f, .1f); var p1 = ctx.OnGround(g.x + Mathf.Cos((a0 + 60f) * Mathf.Deg2Rad) * 9f, g.z + Mathf.Sin((a0 + 60f) * Mathf.Deg2Rad) * 9f); for (var s = 1; s < 9; s++) { var u = s / 9f; var p = Vector3.Lerp(p0, p1, u) + up * (3.1f - Mathf.Sin(u * Mathf.PI) * .6f); mk.Blob(GlowM(new Color(1f, .8f, .45f), 2.2f), p, Vector3.one * .09f, ctx.Rng, 0f); if (s % 4 == 0) halos.Add(p, .6f, new Color(1f, .8f, .45f), .5f); } }
            ArtistBits.PointLight(ctx.Root, g + up * 3f + f.Fwd * 2f, new Color(1f, .8f, .5f), 16f, 1.4f);
            // a fire with lawn chairs, a dock toward the water with a jon boat
            var pit = ctx.OnGround(f.P(6f, 0, -3f).x, f.P(6f, 0, -3f).z); Fire(ctx, mk, pit);
            for (var k = 0; k < 4; k++) { var d = Quaternion.Euler(0, k * 80f + 20f, 0) * Vector3.forward; var p = pit + d * 2.6f; if (k == 2) Folk(ctx.OnGround(p.x, p.z) + up * .47f, -d, Resident.Pose.Sit, new[] { "Caught one this big. No, bigger. It gets bigger every time, that's how you know it's true.", "You hungry? You look hungry. You're hungry." }, Look(Residents.Tool.Mug), null, .45f); LawnChair(ctx, mk, p, -d, k); }
            var deck = Mathf.Max(ctx.Ground(f.P(0, 0, 9f).x, f.P(0, 0, 9f).z), 1.4f) + .4f;
            for (var z = 8f; z < 26f; z += 1f) { var p = f.P(0, 0, z); mk.Box(plank, new Vector3(p.x, deck, p.z), new Vector3(2.2f, .1f, .9f), f.r); if (((int)z) % 4 == 0) foreach (var e in new[] { -1f, 1f }) { var q = p + f.Right * e * 1f; mk.Box(greyWood, new Vector3(q.x, deck / 2f - .5f, q.z), new Vector3(.18f, deck + 1.4f, .18f), f.r); } }
            solid.Box(plank, new Vector3(f.P(0, 0, 17f).x, deck - .1f, f.P(0, 0, 17f).z), new Vector3(2.2f, .3f, 18f), f.r);
            var end = f.P(0, 0, 24.5f); end.y = deck; Interact.Bench(plife.transform, end + up * .1f - f.Right * .7f, end + up * .1f + f.Right * .7f, f.Fwd); Lamp(ctx, mk, ctx.Root, new Vector3(f.P(1.3f, 0, 9f).x, deck, f.P(1.3f, 0, 9f).z));
            Painted(ctx, mk, f.P(-7f, 0, -8f), -f.Fwd, "FISH CAMP\ny'all come eat", 2.6f);
            Lane(ctx, mk, f.P(0, 0, -7f)); Ring(g, 13f);
            Shot(ctx, "the fish camp", f.P(10f, 2.6f, -11f), g + up * 1f + f.Fwd * 3f);
        }

        // the General Store: a porch with rocking chairs, an ice chest, a tyre swing
        void Store(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var g = Dry(ctx, StoreAt, 8f); var f = new PlaceFrame(g, FaceTo(g, Nearest(roadPts, g))); const float W = 9f, D = 6f, H = 3.4f;
            var lo = g.y; foreach (var (x, z) in new[] { (-1f, -1f), (1f, -1f), (1f, 1.6f), (-1f, 1.6f) }) lo = Mathf.Min(lo, ctx.Ground(f.P(x * W / 2f, 0, z * D / 2f).x, f.P(x * W / 2f, 0, z * D / 2f).z));
            var fo = new PlaceFrame(new Vector3(g.x, g.y + .5f, g.z), f.Fwd);
            mk.Box(greyWood, new Vector3(g.x, (lo + fo.o.y) / 2f - .2f, g.z) + f.Fwd * 1.2f, new Vector3(W + .4f, fo.o.y - lo + .5f, D + 3f), f.r); solid.Box(greyWood, new Vector3(g.x, (lo + fo.o.y) / 2f - .1f, g.z) + f.Fwd * 1.2f, new Vector3(W + .4f, fo.o.y - lo + .2f, D + 3f), f.r);
            mk.Box(plank, fo.P(0, H / 2f, 0), new Vector3(W, H, D), f.r); solid.Box(plank, fo.P(0, H / 2f, 0), new Vector3(W, H, D), f.r);
            mk.Box(tin, fo.P(0, H + .15f, 1f), new Vector3(W + .8f, .12f, D + 3.6f), f.r * Quaternion.Euler(6f, 0, 0)); foreach (var e in new[] { -4.2f, 0f, 4.2f }) { mk.Box(greyWood, fo.P(e, 1.6f, D / 2f + 2.2f), new Vector3(.14f, 3.2f, .14f), f.r); ArtistBits.PostCollider(ctx.Root, fo.P(e, 0, D / 2f + 2.2f), 3.2f, .1f); }
            mk.Box(dark, fo.P(0, 1.1f, D / 2f + .02f), new Vector3(1.1f, 2.2f, .04f), f.r); foreach (var e in new[] { -2.8f, 2.8f }) mk.Box(GlowM(new Color(1f, .8f, .45f), 1.6f), fo.P(e, 1.7f, D / 2f + .02f), new Vector3(1.6f, 1.1f, .04f), f.r);
            mk.Box(WorldMaterials.Solid(new Color(.7f, .15f, .12f)), fo.P(0, H + 1f, D / 2f + .1f), new Vector3(5f, 1f, .1f), f.r); ArtistBits.Label(ctx.Root, "GENERAL STORE\nbait · ice · advice", fo.P(0, H + 1.02f, D / 2f + .17f), f.Fwd, .24f, new Color(.98f, .95f, .85f), TextAnchor.MiddleCenter);
            foreach (var e in new[] { -3f, -1.6f, 2.4f }) { var c = fo.P(e, 0, D / 2f + 1.2f); RockingChair(mk, solid, c, f.Fwd); if (e > 0) Folk(c + up * .5f, f.Fwd, Resident.Pose.Sit, new[] { "Got bait, ice, and advice. Advice is free; ice ain't.", "That swing's held since '72. Mostly.", "We close when the frogs start. They've started. Take your time." }, Look(Residents.Tool.Mug), null, .45f); }
            var ice = fo.P(3.8f, .5f, D / 2f + .7f); mk.Box(white, ice, new Vector3(1.2f, 1f, .7f), f.r); mk.Box(WorldMaterials.Solid(new Color(.2f, .45f, .75f)), ice + f.Fwd * .36f, new Vector3(.9f, .5f, .02f), f.r); Col(ice, new Vector3(1.2f, 1f, .7f), f.r);
            for (var k = 0; k < 3; k++) { var st = fo.P(0, -.15f - k * .16f, D / 2f + 2.9f + k * .35f); mk.Box(plank, st, new Vector3(2.4f, .16f, .4f), f.r); solid.Box(plank, st, new Vector3(2.4f, .16f, .4f), f.r); }
            ArtistBits.PointLight(ctx.Root, fo.P(0, 2.6f, D / 2f + 1.6f), new Color(1f, .8f, .5f), 12f, 1.5f);
            // the tyre swing from a live oak
            var oak = ctx.OnGround(f.P(-9f, 0, 3f).x, f.P(-9f, 0, 3f).z); mk.Prism(bark, oak - up * .3f, up, .7f, 7f, 8, .7f); ArtistBits.PostCollider(ctx.Root, oak, 5f, .75f); for (var k = 0; k < 6; k++) soft.Blob(leaf, oak + up * (7f + (k % 3)) + new Vector3(Mathf.Cos(k * 1.05f), 0, Mathf.Sin(k * 1.05f)) * 3f, Vector3.one * 3f, ctx.Rng, .3f);
            mk.Prism(bark, oak + up * 5.4f, f.Right, .25f, 4.6f, 6);
            var sw = new MeshKit(); sw.Box(rope, new Vector3(0, -1.9f, 0), new Vector3(.05f, 3.8f, .05f), Quaternion.identity); sw.Prism(dark, new Vector3(0, -4.1f, -.12f), Vector3.forward, .45f, .24f, 12);
            var sgo = sw.BuildSingle("Tyre swing", plife.transform, false); sgo.transform.SetPositionAndRotation(oak + up * 5.3f + f.Right * 3.6f, f.r); var sway = sgo.AddComponent<Sway>(); sway.degrees = 34f; sway.speed = 2.1f; sway.decay = .22f;
            var st0 = Interact.Toy(sgo, Toy.Kind.Custom, "Push the tyre swing", new Vector3(1.2f, 1.4f, 1.2f)); sgo.GetComponent<BoxCollider>().center = new Vector3(0, -4f, 0); st0.onUse = _ => sway.Push();
            Painted(ctx, mk, f.P(6.5f, 0, D / 2f + 6f), f.Fwd, "COLD DRINKS", 2f);
            Lane(ctx, mk, f.P(0, 0, D / 2f + 5f)); Ring(g, 10f);
            Shot(ctx, "the general store", f.P(-5f, 2f, D / 2f + 13f), fo.P(0, 2f, 0));
        }

        // the Smokehouse: a pit smoking, picnic tables, horseshoes to throw
        void Smokehouse(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var g = Dry(ctx, SmokeAt, 8f); var f = new PlaceFrame(g, FaceTo(g, Nearest(roadPts, g)));
            var sm = ctx.OnGround(f.P(-4f, 0, -4f).x, f.P(-4f, 0, -4f).z); mk.Prism(dark, sm + up * 1.1f - f.Right * 1.4f, f.Right, .7f, 2.8f, 12); mk.Box(dark, sm + up * 2.4f + f.Right * 1f, new Vector3(.25f, 1.6f, .25f), f.r); foreach (var e in new[] { -1f, 1f }) mk.Box(metal, sm + up * .3f + f.Right * e, new Vector3(.1f, .6f, 1f), f.r); Col(sm + up * 1f, new Vector3(3f, 2f, 1.5f), f.r);
            mk.Blob(GlowM(new Color(1f, .45f, .15f), 2f), sm + up * .9f - f.Right * 1.45f, new Vector3(.05f, .3f, .3f), ctx.Rng, .2f); for (var k = 0; k < 4; k++) mk.Blob(WorldMaterials.Glow(new Color(.85f, .85f, .82f), .3f), sm + up * (3.4f + k * .6f) + f.Right * (1f + k * .2f), new Vector3(.4f, .25f, .4f) * (1f + k * .2f), ctx.Rng, .3f);
            mk.Box(tin, sm + up * 3.3f + f.Fwd * .2f, new Vector3(5f, .1f, 3.6f), f.r * Quaternion.Euler(8f, 0, 0)); foreach (var (x, z) in new[] { (-2.3f, -1.4f), (2.3f, -1.4f), (2.3f, 1.8f), (-2.3f, 1.8f) }) { var p = ctx.OnGround((sm + f.r * new Vector3(x, 0, z)).x, (sm + f.r * new Vector3(x, 0, z)).z); mk.Box(greyWood, p + up * 1.6f, new Vector3(.14f, 3.3f, .14f), f.r); ArtistBits.PostCollider(ctx.Root, p, 3.2f, .1f); }
            Folk(sm + f.Fwd * 1.6f + f.Right * .4f, f.Fwd, Resident.Pose.Work, new[] { "Low and slow, like the river.", "Sauce is a secret. It's mostly patience.", "Brisket's been on since yesterday. Don't rush it; it can tell." }, Look(Residents.Tool.Spatula, true)).workRate = .5f;
            PicnicTable(ctx, mk, f.P(3f, 0, -1f), f.Right); PicnicTable(ctx, mk, f.P(3.5f, 0, 3.5f), f.Right);
            Folk(ctx.OnGround(f.P(3f, 0, -1.7f).x, f.P(3f, 0, -1.7f).z) + up * .47f + f.Right * .5f, f.Fwd, Resident.Pose.Sit, new[] { "Best in three counties. There's only the one county, but still.", "I'm on my second plate. Of the day. It's early." }, null, null, .45f);
            // the horseshoe pit: two stakes, four horseshoes to throw
            var s0 = ctx.OnGround(f.P(-7f, 0, 4f).x, f.P(-7f, 0, 4f).z); var s1 = ctx.OnGround(f.P(-7f, 0, 12f).x, f.P(-7f, 0, 12f).z);
            foreach (var s in new[] { s0, s1 }) { mk.Box(WorldMaterials.Solid(new Color(.75f, .68f, .5f)), s + up * .04f, new Vector3(1.6f, .06f, 1.6f), f.r); mk.Prism(metal, s, up, .03f, .5f, 6); ArtistBits.PostCollider(ctx.Root, s, .5f, .04f); }
            for (var k = 0; k < 4; k++)
            {
                var hm = new MeshKit(); var iron = WorldMaterials.Solid(k < 2 ? new Color(.75f, .2f, .15f) : new Color(.25f, .35f, .6f), .7f); for (var a = 0; a < 5; a++) { var an = (a * 45f - 90f) * Mathf.Deg2Rad; hm.Box(iron, new Vector3(Mathf.Cos(an) * .1f, 0, Mathf.Sin(an) * .1f), new Vector3(.04f, .025f, .09f), Quaternion.Euler(0, -a * 45f, 0)); }
                var go = hm.BuildSingle("Horseshoe", plife.transform, false); go.transform.position = s0 + up * .12f + f.Right * (.5f + k * .25f) - f.Fwd * .6f; var c = Interact.Carry(go, "a horseshoe", .6f, g.y - 6f); c.GetComponent<BoxCollider>().size = new Vector3(.26f, .05f, .26f);
            }
            Painted(ctx, mk, f.P(6f, 0, 8f), f.Fwd, "SMOKEHOUSE\nready when it's ready", 3f, .15f);
            ArtistBits.PointLight(ctx.Root, sm + up * 2.6f + f.Fwd * 2f, new Color(1f, .75f, .45f), 13f, 1.4f);
            Lane(ctx, mk, f.P(0, 0, 8f)); Ring(g, 12f);
            Shot(ctx, "the smokehouse", f.P(-3f, 2.2f, 13f), g + up * 1.4f - f.Fwd * 2f);
        }

        // the Airboat, up on the bank: sit in it, spin its fan
        void Airboat(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var g = Dry(ctx, AirboatAt, 5f);
            // point it at the nearest water
            var toWater = Vector3.zero; var bd = float.MaxValue; for (var a = 0; a < 360; a += 15) { var d = Quaternion.Euler(0, a, 0) * Vector3.forward; for (var r = 6f; r < 70f; r += 4f) if (WaterAt(g.x + d.x * r, g.z + d.z * r, 0f, out _)) { if (r < bd) { bd = r; toWater = d; } break; } }
            if (toWater.sqrMagnitude < .01f) toWater = Vector3.left; var f = new PlaceFrame(g + up * .1f, toWater); var hull = WorldMaterials.Solid(new Color(.75f, .76f, .74f), .6f);
            mk.Box(hull, f.P(0, .3f, 0), new Vector3(2.4f, .5f, 5f), f.r); mk.Box(hull, f.P(0, .45f, 2.8f), new Vector3(2.4f, .5f, .8f), f.r * Quaternion.Euler(-25f, 0, 0)); solid.Box(hull, f.P(0, .3f, .2f), new Vector3(2.4f, .6f, 5.6f), f.r);
            mk.Box(metal, f.P(0, 1.2f, -.6f), new Vector3(.1f, 1.4f, .1f), f.r); var seatM = WorldMaterials.Solid(new Color(.2f, .2f, .22f)); mk.Box(seatM, f.P(0, 1.95f, -.6f), new Vector3(.6f, .1f, .5f), f.r); mk.Box(seatM, f.P(0, 2.3f, -.85f), new Vector3(.6f, .6f, .08f), f.r); mk.Box(seatM, f.P(0, .75f, .9f), new Vector3(1.6f, .1f, .5f), f.r); mk.Box(seatM, f.P(0, 1.05f, .65f), new Vector3(1.6f, .5f, .08f), f.r);
            Interact.Bench(plife.transform, f.P(-.5f, .82f, .95f), f.P(.5f, .82f, .95f), f.Fwd);
            for (var k = 0; k < 10; k++) { var a = k * 36f * Mathf.Deg2Rad; var a2 = (k + 1) * 36f * Mathf.Deg2Rad; var p0 = f.P(Mathf.Cos(a) * 1.25f, 1.9f + Mathf.Sin(a) * 1.25f, -1.9f); var p1 = f.P(Mathf.Cos(a2) * 1.25f, 1.9f + Mathf.Sin(a2) * 1.25f, -1.9f); mk.Box(metal, (p0 + p1) / 2f, new Vector3(.05f, .05f, Vector3.Distance(p0, p1)), Quaternion.LookRotation(p1 - p0)); }
            Col(f.P(0, 1.9f, -1.9f), new Vector3(2.6f, 2.6f, .5f), f.r); Col(f.P(0, 1.6f, -.7f), new Vector3(.7f, 2f, .6f), f.r);
            var fan = new MeshKit(); fan.Prism(dark, new Vector3(0, 0, -.1f), Vector3.forward, .16f, .2f, 8); for (var k = 0; k < 3; k++) fan.Box(plank, Quaternion.Euler(0, 0, k * 120f) * new Vector3(0, .6f, 0), new Vector3(.2f, 1.1f, .04f), Quaternion.Euler(0, 0, k * 120f));
            var fgo = fan.BuildSingle("Airboat fan", plife.transform, false); fgo.transform.SetPositionAndRotation(f.P(0, 1.9f, -1.9f), f.r); var ft = Interact.Toy(fgo, Toy.Kind.Spin, "Spin the fan", new Vector3(2.4f, 2.4f, .6f)); ft.axis = Vector3.forward; ft.amount = 2160f; ft.seconds = 4f;
            Folk(f.P(2.6f, -.1f, 1f), -f.Right, Resident.Pose.Lean, new[] { "She's loud but she's honest.", "Gators wave back if you're polite.", "Tours leave when I feel like it. I ain't felt like it since spring." }, Look());
            Lamp(ctx, mk, ctx.Root, ctx.OnGround(f.P(-2.6f, 0, -2f).x, f.P(-2.6f, 0, -2f).z));
            Painted(ctx, mk, f.P(3f, 0, -4.5f), -f.Fwd, "AIRBOAT RIDES\n(ask)", 2.2f);
            Lane(ctx, mk, f.P(0, 0, -5f)); Ring(g, 6f);
            Shot(ctx, "the airboat", f.P(-6f, 1.9f, -7f), f.P(0, 1.2f, 0));
        }

        // the Church in the Wildwood: white boards, a bell to ring, dinner on the grounds
        void Church(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var g = Dry(ctx, ChurchAt, 9f); var f = new PlaceFrame(g, FaceTo(g, Nearest(roadPts, g))); const float W = 6.5f, D = 11f, H = 4.4f;
            mk.Box(white, g + up * (H / 2f - .4f), new Vector3(W, H + .8f, D), f.r); solid.Box(white, g + up * H / 2f, new Vector3(W, H, D), f.r); mk.GableRoof(tin, g + up * H, new Vector3(W + .6f, 0, D + .6f), 2.6f, f.r);
            mk.Box(white, f.P(0, H + 3f, D / 2f - 1.2f), new Vector3(1.8f, 2.4f, 1.8f), f.r); mk.Cone(tin, f.P(0, H + 4.2f, D / 2f - 1.2f), 1.5f, 2.6f, 4, 45f); foreach (var e in new[] { -1f, 1f }) mk.Box(dark, f.P(e * .91f, H + 3.2f, D / 2f - 1.2f), new Vector3(.02f, 1.2f, .9f), f.r);
            mk.Box(dark, f.P(0, 1.3f, D / 2f + .02f), new Vector3(1.5f, 2.6f, .04f), f.r); foreach (var z in new[] { -3f, 0f, 3f }) foreach (var e in new[] { -1f, 1f }) mk.Box(GlowM(new Color(1f, .82f, .5f), 1.3f), f.P(e * (W / 2f + .02f), 2.4f, z), new Vector3(.04f, 1.8f, .7f), f.r);
            var bell = new MeshKit(); bell.Prism(WorldMaterials.Solid(new Color(.8f, .65f, .3f), .85f), new Vector3(0, -.5f, 0), Vector3.up, .32f, .5f, 10, .5f); var bgo = bell.BuildSingle("Church bell", plife.transform, false); bgo.transform.SetPositionAndRotation(f.P(0, H + 3.6f, D / 2f - 1.2f), f.r); var bs = bgo.AddComponent<Sway>(); bs.degrees = 34f; bs.speed = 6f; bs.decay = .8f; bs.seconds = 6f;
            var rp = f.P(1.4f, 0, D / 2f + .3f); mk.Box(rope, rp + up * 3.4f, new Vector3(.04f, 6.6f, .04f), f.r); var pull = new GameObject("Bell rope"); pull.transform.SetParent(plife.transform, false); pull.transform.position = rp + up * 1.3f; Interact.Toy(pull, Toy.Kind.Custom, "Ring the bell", new Vector3(.5f, 1.6f, .5f)).onUse = _ => bs.Push();
            for (var k = 0; k < 3; k++) { var st = f.P(0, .12f - k * .0f, D / 2f + .5f + k * .4f); mk.Box(greyWood, st - up * (k * .0f), new Vector3(2.4f, .24f - k * .06f, .4f), f.r); }
            ArtistBits.PointLight(ctx.Root, f.P(0, 3f, D / 2f + 2.5f), new Color(1f, .85f, .55f), 13f, 1.4f); halos.Add(f.P(0, 3.2f, D / 2f + .3f), 1.6f, new Color(1f, .85f, .55f), .5f);
            // dinner on the grounds: two long tables under the trees, covered dishes
            foreach (var e in new[] { -1f, 1f }) { var t = f.P(e * 7f, 0, 2f); PicnicTable(ctx, mk, t, f.Fwd); var tg = ctx.OnGround(t.x, t.z); for (var k = 0; k < 4; k++) mk.Prism(WorldMaterials.Solid(Shirts[(k + (e > 0 ? 2 : 0)) % Shirts.Length]), tg + up * .78f + f.Fwd * (-.7f + k * .45f), up, .14f, .1f, 10); }
            Folk(f.P(-1.8f, 0, D / 2f + 1.8f), f.Fwd, Resident.Pose.Stand, new[] { "Dinner on the grounds after. Bring nothin', eat everything.", "Ring the bell if you like. The Lord don't mind, and neither do the neighbours; there ain't any.", "We sing the old ones. Slow. So you can keep up." }, new Residents.Look { top = new Color(.9f, .9f, .86f), bottom = new Color(.15f, .15f, .18f), skin = new Color(.55f, .38f, .26f), hair = new Color(.75f, .75f, .75f) });
            Folk(ctx.OnGround(f.P(7.7f, 0, 2.5f).x, f.P(7.7f, 0, 2.5f).z) + up * .47f, -f.Right, Resident.Pose.Sit, new[] { "Try the potato salad. No, the other potato salad.", "Third helping. It'd be rude not to." }, Look(Residents.Tool.Mug), null, .45f);
            Painted(ctx, mk, f.P(5f, 0, D / 2f + 7f), f.Fwd, "CHURCH IN THE WILDWOOD\nall welcome, come hungry", 3.6f, .14f);
            Lane(ctx, mk, f.P(0, 0, D / 2f + 5f)); Ring(g, 12f);
            Shot(ctx, "the church in the wildwood", f.P(-7f, 2f, D / 2f + 13f), g + up * 4f);
        }

        // the Still in the woods: a copper still, jugs to carry off, a sleeping hound
        void Still(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var g = Dry(ctx, StillAt, 6f); var f = new PlaceFrame(g, FaceTo(g, Nearest(roadPts, g))); var copper = WorldMaterials.Solid(new Color(.8f, .5f, .3f), .85f);
            mk.Prism(copper, g + up * .5f, up, .8f, 1.3f, 12, .8f); mk.Cone(copper, g + up * 1.8f, .65f, .8f, 12); mk.Prism(dark, g, up, .7f, .5f, 8); mk.Blob(GlowM(new Color(1f, .5f, .2f), 2.4f), g + up * .25f + f.Fwd * .6f, new Vector3(.3f, .16f, .1f), ctx.Rng, .2f);
            var pipe0 = g + up * 2.5f; var pipe1 = f.P(1.8f, 1.2f, 0); mk.Box(copper, (pipe0 + pipe1) / 2f, new Vector3(.06f, .06f, Vector3.Distance(pipe0, pipe1)), Quaternion.LookRotation(pipe1 - pipe0)); mk.Prism(plank, f.P(1.8f, 0, 0), up, .4f, 1.1f, 10); ArtistBits.PostCollider(ctx.Root, g, 2.4f, .85f); ArtistBits.PostCollider(ctx.Root, f.P(1.8f, 0, 0), 1.1f, .4f);
            var fl = ArtistBits.PointLight(ctx.Root, g + up * 1f + f.Fwd * 1.2f, new Color(1f, .6f, .3f), 10f, 1.8f); fl.gameObject.AddComponent<FireFlicker>().baseIntensity = 1.8f; halos.Add(g + up * .4f + f.Fwd * .7f, 1.4f, new Color(1f, .55f, .2f), .6f);
            for (var k = 0; k < 5; k++)
            {
                var jm = new MeshKit(); var clay = WorldMaterials.Solid(k % 2 == 0 ? new Color(.75f, .68f, .55f) : new Color(.5f, .35f, .22f)); jm.Prism(clay, Vector3.zero, Vector3.up, .16f, .3f, 10, .9f); jm.Prism(clay, new Vector3(0, .3f, 0), Vector3.up, .06f, .12f, 8); jm.Box(clay, new Vector3(.12f, .32f, 0), new Vector3(.08f, .1f, .03f), Quaternion.identity);
                var go = jm.BuildSingle("Jug", plife.transform, false); go.transform.position = f.P(-1.4f - (k % 3) * .4f, .05f, .6f + (k / 3) * .4f); Interact.Carry(go, "a jug of... water", .8f, g.y - 6f);
            }
            var dog = f.P(2.6f, 0, 2f); dog = ctx.OnGround(dog.x, dog.z); var fur = WorldMaterials.Solid(new Color(.55f, .38f, .22f)); mk.Blob(fur, dog + up * .2f, new Vector3(.5f, .2f, .3f), ctx.Rng, .1f); mk.Blob(fur, dog + up * .18f + f.Right * .5f, new Vector3(.2f, .15f, .18f), ctx.Rng, 0f); foreach (var e in new[] { -.12f, .12f }) mk.Box(WorldMaterials.Solid(new Color(.35f, .24f, .14f)), dog + up * .12f + f.Right * .5f + f.Fwd * e, new Vector3(.2f, .03f, .1f), f.r);
            var dgo = new GameObject("The hound"); dgo.transform.SetParent(plife.transform, false); dgo.transform.position = dog; var dn = 0; var dogs = new[] { "The hound lifts one ear. Puts it back.", "Thump. Thump. That's the tail. He's glad.", "He sighs like he's seen it all. He has; it's a small bayou." };
            Interact.Toy(dgo, Toy.Kind.Custom, "Pet the hound", new Vector3(1.3f, .7f, .9f)).onUse = _ => Say(dgo.transform, dog + up * .9f, dogs[dn++ % dogs.Length]);
            LawnChair(ctx, mk, f.P(-2.6f, 0, 2.6f), FaceTo(f.P(-2.6f, 0, 2.6f), g), 1);
            Folk(f.P(.4f, 0, 2.4f), f.Fwd, Resident.Pose.Stand, new[] { "This here's a... water purifier.", "Don't light a match near the good stuff.", "Take a jug. Go on. Sheriff's my brother, and he's the one who drinks it." }, Look(Residents.Tool.Mug));
            Lane(ctx, mk, f.P(0, 0, 5f)); Ring(g, 7f);
            Shot(ctx, "the still in the woods", f.P(4f, 1.8f, 8f), g + up * 1.2f);
        }
    }

    /// (The Bayou places move by themselves: swings and bells use Sway, the fan is a Toy.)
    public class BayouPlacesLife : MonoBehaviour { }
}
