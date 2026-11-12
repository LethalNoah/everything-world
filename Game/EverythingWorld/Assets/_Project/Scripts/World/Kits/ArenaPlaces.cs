using System.Collections.Generic;
using UnityEngine;
using static EverythingWorld.World.PlaceTools;

namespace EverythingWorld.World
{
    /// Arena Rock Ridge laid out on purpose (the build-land standard). The stadium, the roads and every band stay
    /// where they were; five more lamp-lit asphalt roads close the dead ends into a ring on the east side and reach
    /// the empty parts. Along them, with the people of the ridge: the Drive-In on the east lobe (a great white screen
    /// playing a concert film, cars to sit in, a snack shack); the Arcade (a neon hall to walk into, cabinets to play,
    /// an air hockey table); the Ballad Pool (a still round pool of floating lights that rise when you raise your
    /// lighter, benches round it); Backstage (a catering tent, road cases, a follow spot to swing, roadies); and
    /// Lighter Hill (a giant lighter on the south rise that flicks on, a bench with the whole ridge below).
    public partial class ArenaKit
    {
        static readonly Vector2 DriveInAt = new Vector2(7392, 3583), ArcadeAt = new Vector2(6752, 3612), PoolAt = new Vector2(6995, 3697), BackstageAt = new Vector2(7178, 3712), LighterAt = new Vector2(7146, 3478);
        static readonly Vector2[] EastWay = { new Vector2(7235, 3556), new Vector2(7290, 3572), new Vector2(7335, 3584), new Vector2(7362, 3585) };
        static readonly Vector2[] RingNorth = { new Vector2(7122, 3642), new Vector2(7150, 3682), new Vector2(7160, 3740), new Vector2(7140, 3785), new Vector2(7118, 3800) };
        static readonly Vector2[] RingSouth = { new Vector2(7172, 3522), new Vector2(7128, 3478), new Vector2(7120, 3430), new Vector2(7100, 3378), new Vector2(7084, 3346) };
        static readonly Vector2[] MidWay = { new Vector2(6810, 3606), new Vector2(6782, 3612), new Vector2(6768, 3613) };
        static readonly Vector2[] PoolWay = { new Vector2(6945, 3655), new Vector2(6968, 3680), new Vector2(6980, 3690) };
        static readonly (Vector2 at, float r)[] PlaceRings = { (DriveInAt, 22f), (ArcadeAt, 11f), (PoolAt, 14f), (BackstageAt, 14f), (LighterAt, 7f) };

        ArenaPlacesLife plife; System.Random prng; Transform rootT; LandContext ctx0; readonly List<Vector3> roadPts = new List<Vector3>();

        static readonly Color[] Tees = { new Color(.1f, .1f, .12f), new Color(.75f, .15f, .15f), new Color(.2f, .4f, .8f), new Color(.92f, .92f, .95f), new Color(.95f, .8f, .2f), new Color(.5f, .25f, .6f) };
        static readonly Color[] Jeans = { new Color(.25f, .35f, .6f), new Color(.15f, .2f, .35f), new Color(.1f, .1f, .12f), new Color(.6f, .65f, .75f) };
        static readonly Residents.Hat[] Lids = { Residents.Hat.None, Residents.Hat.Cap, Residents.Hat.None, Residents.Hat.Beanie };
        static readonly string[] FanTalk =
        {
            "Best show I've ever seen. It's also the only show. It never stops.", "They always save the big one for the encore. The encore is every twenty minutes.", "I've had this lighter up since the second verse.",
            "You can hear the chorus from anywhere on the ridge. Sing it. Nobody's judging.", "Don't stop believing. That's not advice, that's the bylaws.",
        };

        Residents.Look Fan(Residents.Tool tool = Residents.Tool.None, bool apron = false) { var l = Residents.Random(prng, Tees, Jeans, Lids, Tees); l.tool = tool; l.apron = apron; if (l.hat == Residents.Hat.None && prng.Next(2) == 0) l.longHair = true; return l; }
        Resident Folk(Vector3 feet, Vector3 face, Resident.Pose pose, string[] lines = null, Residents.Look look = null, Vector3[] route = null, float seatH = HumanScale.SeatHeight)
            => Residents.Make(plife.transform, feet, face, look ?? Fan(), pose, prng, lines ?? FanTalk, route, seatH);
        void Ring(Vector3 at, float r) => noGrass.Add(new Vector3(at.x, r, at.z));
        void Col(Vector3 c, Vector3 size, Quaternion rot) => BoxCol(rootT, c, size, rot);
        Vector3 G(Vector3 p) => ctx0.OnGround(p.x, p.z);

        /// A lit marquee board on two posts. Signs go on the +x side of a place's lane; the photo spot on the −x side.
        void Marquee(MeshKit mk, Vector3 at, Vector3 face, string text, float w = 3f, float size = .17f)
        {
            var up = Vector3.up; var g = G(at); var rot = Quaternion.LookRotation(face);
            foreach (var e in new[] { -1f, 1f }) mk.Box(dark, g + rot * Vector3.right * e * (w / 2f - .1f) + up * 1f, new Vector3(.12f, 2f, .12f), rot);
            mk.Box(white, g + up * 1.75f, new Vector3(w, .85f, .1f), rot); Col(g + up * 1.1f, new Vector3(w, 2.2f, .15f), rot);
            for (var k = 0; k <= 8; k++) mk.Blob(GlowM(Flame, 2.4f), g + up * 2.24f + rot * Vector3.right * (-w / 2f + k * w / 8f) + face * .06f, Vector3.one * .05f, ctx0.Rng, 0f);
            halos.Add(g + up * 1.8f + face * .3f, 1.4f, Flame, .4f);
            ArtistBits.Label(rootT, text, g + up * 1.75f + face * .06f, face, size, new Color(.1f, .1f, .14f), TextAnchor.MiddleCenter);
        }

        void MetalBench(MeshKit mk, Vector3 at, Vector3 face, float len = 2f)
        {
            var up = Vector3.up; var g = G(at); var rot = Quaternion.LookRotation(face); var side = Vector3.Cross(up, face);
            mk.Box(wood, g + up * .45f, new Vector3(len, .08f, .5f), rot); mk.Box(wood, g + up * .8f - face * .25f, new Vector3(len, .45f, .06f), rot); foreach (var e in new[] { -1f, 1f }) mk.Box(dark, g + up * .3f + side * e * (len / 2f - .15f) - face * .1f, new Vector3(.08f, .9f, .5f), rot);
            Col(g + up * .5f, new Vector3(len, 1f, .6f), rot); Interact.Bench(plife.transform, g + up * .5f - side * (len / 2f - .4f), g + up * .5f + side * (len / 2f - .4f), face);
        }

        void PlaceBend(LandContext ctx, List<Vector3> pts)
        {
            for (var i = 0; i < pts.Count; i++)
            {
                foreach (var a in ctx.Artists) { var r = Foot(a) + 7f; var v = new Vector2(pts[i].x - a.x, pts[i].z - a.z); var d = v.magnitude; if (d < r) { v = d > .01f ? v / d * r : new Vector2(r, 0); pts[i] = ctx.OnGround(a.x + v.x, a.z + v.y); } }
                foreach (var c in noGrass) { if (c.y < 4f) continue; var r = c.y + 3f; var v = new Vector2(pts[i].x - c.x, pts[i].z - c.z); var d = v.magnitude; if (d < r) { v = d > .01f ? v / d * r : new Vector2(r, 0); pts[i] = ctx.OnGround(c.x + v.x, c.z + v.y); } }
            }
        }

        List<Vector3> RidgeRoad(LandContext ctx, MeshKit mk, Vector2[] way, bool benchHalfWay = true)
        {
            Vector2 near = way[0]; var bd = 170f * 170f; foreach (var (p, w) in walkPoints) { if (w >= 400) continue; var d = (p.x - way[0].x) * (p.x - way[0].x) + (p.z - way[0].y) * (p.z - way[0].y); if (d < bd) { bd = d; near = new Vector2(p.x, p.z); } }
            var full = new List<Vector2> { near }; full.AddRange(way);
            var pts = Smooth(ctx, full, false, 2.5f); PlaceBend(ctx, pts); if (pts.Count < 2) return pts;
            Strip(mk, pts, asphalt, 4.4f, .07f);
            var line = WorldMaterials.Solid(new Color(.95f, .85f, .4f)); var up = Vector3.up;
            for (var i = 1; i < pts.Count; i += 2) { var a = pts[i - 1]; var b = pts[i]; mk.Box(line, (a + b) / 2f + up * .085f, new Vector3(.15f, .02f, Vector3.Distance(a, b) * .6f), Quaternion.LookRotation(b - a)); }
            var n = 0;
            for (var i = 5; i < pts.Count; i += 11) { var d = pts[i] - pts[i - 1]; d.y = 0; if (d.sqrMagnitude < .01f) continue; d.Normalize(); var lp = pts[i] + Vector3.Cross(up, d) * (n++ % 2 == 0 ? 3.1f : -3.1f); if (ctx.IsLand(lp.x, lp.z, 2f)) Lamp(ctx, mk, ctx.Root, ctx.OnGround(lp.x, lp.z)); }
            foreach (var p in pts) Remember(p, 700);
            roadPts.AddRange(pts);
            if (benchHalfWay && pts.Count > 26) { var i = pts.Count / 2; var d = pts[i + 1] - pts[i - 1]; d.y = 0; d.Normalize(); var sd = Vector3.Cross(up, d); var bp = pts[i] - sd * 4f; if (ctx.IsLand(bp.x, bp.z, 2f)) { MetalBench(mk, bp, sd); Ring(bp, 2f); } }
            return pts;
        }

        void Lane(LandContext ctx, MeshKit mk, Vector3 to, float stopShort = 0f)
        {
            var from = Nearest(roadPts, to); var d = new Vector2(to.x - from.x, to.z - from.z); var len = d.magnitude; if (len < stopShort + 3f) return; d /= len;
            var a = new Vector2(from.x, from.z) + d * 2.3f; var b = new Vector2(from.x, from.z) + d * (len - stopShort);
            var pts = Smooth(ctx, new[] { a, Vector2.Lerp(a, b, .5f), b }, false, 2f, 1f); if (pts.Count < 2) return;
            Strip(mk, pts, asphalt, 2.6f, .065f); foreach (var p in pts) Remember(p, 900);
        }

        void RidgePlaces(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            roadPts.Clear(); rootT = ctx.Root; ctx0 = ctx; prng = new System.Random(1981);
            plife = new GameObject("Arena places life").AddComponent<ArenaPlacesLife>(); plife.transform.SetParent(life.transform, false);
            foreach (var (at, r) in PlaceRings) noGrass.Add(new Vector3(at.x, r, at.y));
            RidgeRoad(ctx, mk, EastWay); RidgeRoad(ctx, mk, RingNorth); RidgeRoad(ctx, mk, RingSouth); RidgeRoad(ctx, mk, MidWay, false); RidgeRoad(ctx, mk, PoolWay, false);
            DriveIn(ctx, mk, solid); Arcade(ctx, mk, solid); BalladPool(ctx, mk, solid); Backstage(ctx, mk, solid); LighterHill(ctx, mk, solid);
        }

        /// A parked car with its lights on: sit in it.
        void Car(MeshKit mk, MeshKit solid, Vector3 at, Vector3 dir, Color paint, bool convertible)
        {
            var up = Vector3.up; var g = G(at); var rot = Quaternion.LookRotation(dir); var side = Vector3.Cross(up, dir); var body = WorldMaterials.Solid(paint, .7f);
            mk.Box(body, g + up * .65f, new Vector3(1.9f, .6f, 4.4f), rot); if (!convertible) { mk.Box(body, g + up * 1.5f - dir * .3f, new Vector3(1.7f, .08f, 1.9f), rot); foreach (var e in new[] { -1f, 1f }) foreach (var z in new[] { -1.2f, .6f }) mk.Box(body, g + up * 1.2f + side * e * .82f + dir * z, new Vector3(.07f, .6f, .07f), rot); }
            else mk.Box(GlowM(new Color(.7f, .85f, 1f), .3f), g + up * 1.15f + dir * .65f, new Vector3(1.7f, .5f, .05f), rot * Quaternion.Euler(-20f, 0, 0));
            mk.Box(dark, g + up * .9f - dir * .35f, new Vector3(1.6f, .12f, 1.5f), rot);
            foreach (var e in new[] { -1f, 1f }) { foreach (var z in new[] { -1.4f, 1.4f }) mk.Prism(dark, g + side * e * .86f + dir * z + up * .38f, side * e, .38f, .22f, 10); mk.Box(GlowM(new Color(1f, .95f, .8f), 2.4f), g + up * .7f + dir * 2.21f + side * e * .65f, new Vector3(.3f, .16f, .03f), rot); mk.Box(GlowM(new Color(1f, .15f, .1f), 1.6f), g + up * .7f - dir * 2.21f + side * e * .65f, new Vector3(.3f, .14f, .03f), rot); }
            solid.Box(body, g + up * .45f, new Vector3(1.9f, .9f, 4.4f), rot); foreach (var e in new[] { -.42f, .42f }) Interact.Seat(plife.transform, g + up * 1f - dir * .3f + side * e, dir, "Sit in the car");
        }

        // ---------------------------------------------------------------- the Drive-In
        void DriveIn(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var g = ctx.OnGround(DriveInAt.x, DriveInAt.y); var f = new PlaceFrame(g, FaceTo(g, Nearest(roadPts, g)));
            // the screen at the back of the lot, facing the road: white, glowing, a concert film in blocks of light
            const float SW = 20f, SH = 9f; var sc = f.P(0, 0, -15f); var sg = G(sc);
            foreach (var e in new[] { -1f, 1f }) { mk.Box(dark, sg + f.Right * e * (SW / 2f - 1.5f) + up * 3f - f.Fwd * .6f, new Vector3(.5f, 8f, .5f), f.r); ArtistBits.PostCollider(ctx.Root, sg + f.Right * e * (SW / 2f - 1.5f) - f.Fwd * .6f, 7f, .4f); }
            var screenM = new Material(WorldMaterials.Glow(new Color(.85f, .9f, 1f), .9f)) { name = "Drive-in screen" }; mk.Box(dark, sg + up * (3.2f + SH / 2f) - f.Fwd * .3f, new Vector3(SW + .8f, SH + .8f, .4f), f.r); mk.Box(screenM, sg + up * (3.2f + SH / 2f) - f.Fwd * .08f, new Vector3(SW, SH, .05f), f.r); Col(sg + up * (3.2f + SH / 2f) - f.Fwd * .3f, new Vector3(SW + .8f, SH + .8f, .4f), f.r);
            plife.screenM = screenM; plife.screenLight = ArtistBits.PointLight(ctx.Root, sg + up * 6f + f.Fwd * 6f, new Color(.8f, .88f, 1f), 40f, 2.2f);
            // the film: a singer, two guitarists, and stage lights, as moving blocks on the screen
            for (var k = 0; k < 5; k++) { var bm = new MeshKit(); bm.Box(k < 3 ? dark : GlowM(k == 3 ? new Color(1f, .4f, .5f) : new Color(.4f, .7f, 1f), 2.4f), Vector3.zero, k < 3 ? new Vector3(1.1f, k == 0 ? 3.6f : 3f, .04f) : new Vector3(2.6f, .5f, .04f), Quaternion.identity); if (k < 3) bm.Box(dark, new Vector3(0, k == 0 ? 2.2f : 1.9f, 0), new Vector3(.8f, .8f, .04f), Quaternion.identity); var go = bm.BuildSingle("Film " + k, plife.transform, false); var bp = sg + up * (3.2f + (k < 3 ? 2.2f : 7.6f)) + f.Right * (k < 3 ? (k - 1) * 5f : (k == 3 ? -5f : 5f)); go.transform.SetPositionAndRotation(bp, f.r); plife.film.Add((go.transform, bp, f.Right, k)); }
            // two rows of cars facing the screen, speaker posts between
            var n = 0; Color[] paint = { new Color(.75f, .15f, .15f), new Color(.2f, .4f, .8f), new Color(.95f, .8f, .2f), new Color(.85f, .85f, .88f), new Color(.2f, .55f, .45f), new Color(.9f, .5f, .2f), new Color(.15f, .15f, .2f) };
            for (var row = 0; row < 2; row++) for (var k = 0; k < 4; k++)
            {
                if (row == 1 && k == 1) continue;   // a gap in the back row to walk through
                var p = f.P(-9f + k * 6f + row * 1.5f, 0, -3f + row * 7f); Car(mk, solid, p, -f.Fwd, paint[n % paint.Length], n % 3 == 1); n++;
                var sp = G(p + f.Right * 1.7f); mk.Box(metal, sp + up * .6f, new Vector3(.06f, 1.2f, .06f), f.r); mk.Box(metal, sp + up * 1.25f, new Vector3(.22f, .28f, .14f), f.r); ArtistBits.PostCollider(ctx.Root, sp, 1.3f, .08f);
            }
            Folk(G(f.P(-9f + 6f + .45f, 0, -3f - .3f)) + up * .5f, -f.Fwd, Resident.Pose.Sit, new[] { "Shh. This is the part where he holds the note.", "Seen it four hundred times. He holds the note every time.", "Get in the next car, the speaker's better." }, Fan(), null, .5f);
            // the snack shack by the entrance: popcorn that glows, a counter
            var sh = new PlaceFrame(G(f.P(11f, 0, 10f)), -f.Right); mk.Box(white, sh.P(0, 1.4f, -1f), new Vector3(4f, 2.8f, 2.4f), sh.r); solid.Box(white, sh.P(0, 1.4f, -1f), new Vector3(4f, 2.8f, 2.4f), sh.r); for (var s = 0; s < 8; s++) mk.Box(WorldMaterials.Solid(s % 2 == 0 ? new Color(.85f, .15f, .15f) : new Color(.95f, .95f, .95f), .3f), sh.P(-1.75f + s * .5f, 2.95f, 0), new Vector3(.5f, .08f, 4.2f), sh.r);
            mk.Box(dark, sh.P(0, 1.7f, .22f), new Vector3(3.2f, 1.2f, .04f), sh.r); mk.Box(metal, sh.P(0, 1.05f, .5f), new Vector3(3.6f, .08f, .7f), sh.r); Col(sh.P(0, .55f, .5f), new Vector3(3.6f, 1.1f, .7f), sh.r);
            mk.Box(GlowM(new Color(1f, .9f, .5f), 2f), sh.P(-1f, 1.45f, .5f), new Vector3(.5f, .7f, .4f), sh.r); halos.Add(sh.P(-1f, 1.5f, .8f), 1f, new Color(1f, .9f, .5f), .6f); ArtistBits.Label(ctx.Root, "POPCORN · SODA · ENCORES", sh.P(0, 2.55f, .24f), sh.Fwd, .15f, new Color(.8f, .12f, .12f), TextAnchor.MiddleCenter);
            Folk(sh.P(.6f, 0, 1.6f), -sh.Fwd, Resident.Pose.Stand, new[] { "Large popcorn. It's a three-hour film about a four-minute song.", "Is this the queue? There's always a queue." }, Fan());
            for (var k = 0; k < 3; k++) { var pm = new MeshKit(); pm.Prism(WorldMaterials.Solid(k == 1 ? new Color(.95f, .95f, .95f) : new Color(.85f, .15f, .15f)), Vector3.zero, Vector3.up, .1f, .26f, 6, 1.4f); pm.Blob(WorldMaterials.Glow(new Color(1f, .93f, .6f), .3f), new Vector3(0, .3f, 0), Vector3.one * .13f, ctx.Rng, .3f); var go = pm.BuildSingle("Popcorn", plife.transform, false); go.transform.position = sh.P(.3f + k * .45f, 1.1f, .5f); Interact.Carry(go, "a tub of popcorn", .3f, g.y - 10f); }
            ArtistBits.PointLight(ctx.Root, sh.P(0, 2.4f, 1.5f), new Color(1f, .85f, .6f), 9f, 1.4f);
            Marquee(mk, f.P(7f, 0, 17f), f.Fwd, "RIDGE DRIVE-IN\nNOW SHOWING: THE ENCORE", 4.2f, .16f);
            Lane(ctx, mk, f.P(0, 0, 13f)); Ring(g, 20f);
            Shot(ctx, "the drive-in", f.P(-10f, 4f, 20f), sg + up * 6f);
        }

        // ---------------------------------------------------------------- the Arcade
        void Arcade(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var g = ctx.OnGround(ArcadeAt.x, ArcadeAt.y); var f = new PlaceFrame(g, FaceTo(g, Nearest(roadPts, g))); const float W = 12f, D = 8f, H = 3.4f, T = .3f, Door = HumanScale.DoorWidth + .5f;
            var fo = new PlaceFrame(g + up * .25f, f.Fwd); var wallM = WorldMaterials.Solid(new Color(.14f, .13f, .2f)); var carpet = WorldMaterials.Solid(new Color(.2f, .12f, .3f));
            mk.Box(stone, g - up * .35f, new Vector3(W + .6f, 1.2f, D + .6f), f.r);
            void Wall(Vector3 p, Vector3 s) { mk.Box(wallM, p, s, f.r); solid.Box(wallM, p, s, f.r); }
            mk.Box(carpet, fo.P(0, -.03f, 0), new Vector3(W - T, .1f, D - T), f.r); solid.Box(carpet, fo.P(0, -.15f, 0), new Vector3(W, .3f, D), f.r);
            Wall(fo.P(0, H / 2f, -D / 2f + T / 2f), new Vector3(W, H, T)); foreach (var e in new[] { -1f, 1f }) Wall(fo.P(e * (W / 2f - T / 2f), H / 2f, 0), new Vector3(T, H, D - T * 2f));
            var seg = (W - Door) / 2f; foreach (var e in new[] { -1f, 1f }) Wall(fo.P(e * (Door / 2f + seg / 2f), H / 2f, D / 2f - T / 2f), new Vector3(seg, H, T)); Wall(fo.P(0, H - .35f, D / 2f - T / 2f), new Vector3(Door, .7f, T));
            mk.Box(dark, fo.P(0, H + .1f, 0), new Vector3(W + .6f, .2f, D + .6f), f.r); solid.Box(dark, fo.P(0, H + .1f, 0), new Vector3(W + .6f, .2f, D + .6f), f.r);
            IndoorSpaces.Add(fo.P(0, H / 2f, 0), f.r, new Vector3(W - .7f, H, D - .7f));
            var cyan = new Color(.3f, .9f, 1f); var pink = new Color(1f, .35f, .8f);
            ArtistBits.Label(ctx.Root, "ARCADE", fo.P(0, H + 1f, D / 2f + .12f), f.Fwd, .7f, cyan, TextAnchor.MiddleCenter); mk.Box(dark, fo.P(0, H + 1f, D / 2f), new Vector3(6f, 1.5f, .14f), f.r); halos.Add(fo.P(0, H + 1f, D / 2f + .5f), 3f, cyan, .5f);
            foreach (var e in new[] { -1f, 1f }) { mk.Box(GlowM(pink, 2.4f), fo.P(e * (W / 2f), H / 2f, D / 2f + .03f), new Vector3(.12f, H, .06f), f.r); mk.Box(GlowM(pink, 2.4f), fo.P(e * (Door / 2f + .1f), 1.3f, D / 2f + .03f), new Vector3(.08f, 2.6f, .06f), f.r); } mk.Box(GlowM(cyan, 2.4f), fo.P(0, H + .05f, D / 2f + .33f), new Vector3(W, .1f, .06f), f.r);
            // cabinets along the back and side walls: press one and its screen flashes and it tells you how you did
            Color[] cab = { new Color(.9f, .2f, .2f), new Color(.2f, .5f, .95f), new Color(.95f, .8f, .2f), new Color(.3f, .85f, .5f), new Color(.8f, .3f, .9f), new Color(1f, .55f, .16f) };
            string[] games = { "WHEEL IN THE SKY", "REAPER RUN", "TORNADO ALLEY", "EYE OF THE TIGER", "MIDNIGHT TRAIN", "POWER CHORD" };
            string[] result = { "GAME OVER. High score: someone called ACE.", "You reach level 2. The music gets faster.", "NEW HIGH SCORE! Enter your initials: _ _ _", "So close. One more go.", "The screen says INSERT COIN. It doesn't mean it." };
            for (var k = 0; k < 6; k++)
            {
                var back = k < 4; var cf = back ? new PlaceFrame(fo.P(-3.6f + k * 1.6f, 0, -D / 2f + T + .45f), f.Fwd) : new PlaceFrame(fo.P(-W / 2f + T + .45f, 0, -.4f + (k - 4) * 1.7f), f.Right);
                mk.Box(WorldMaterials.Solid(cab[k], .4f), cf.P(0, .95f, 0), new Vector3(.8f, 1.9f, .8f), cf.r); mk.Box(dark, cf.P(0, 1.0f, .42f), new Vector3(.7f, .1f, .25f), cf.r * Quaternion.Euler(-15f, 0, 0)); Col(cf.P(0, .95f, .05f), new Vector3(.8f, 1.9f, .9f), cf.r);
                var sm = new Material(WorldMaterials.Glow(cab[(k + 2) % 6], .8f)) { name = "Arcade screen" }; var sk = new MeshKit(); sk.Box(sm, Vector3.zero, new Vector3(.6f, .5f, .02f), Quaternion.identity); var sgo = sk.BuildSingle("Arcade cabinet " + games[k], plife.transform, false); sgo.transform.SetPositionAndRotation(cf.P(0, 1.45f, .41f), cf.r);
                ArtistBits.Label(ctx.Root, games[k], cf.P(0, 1.82f, .41f), cf.Fwd, .055f, Ink, TextAnchor.MiddleCenter);
                var idx = plife.screens.Count; plife.screens.Add((sm, cab[(k + 2) % 6], -10f)); var kk = k; var plays = 0;
                var t = Interact.Toy(sgo, Toy.Kind.Custom, "Play " + games[k], new Vector3(.8f, 1.2f, .8f)); sgo.GetComponent<BoxCollider>().center = new Vector3(0, -.3f, .3f); t.onUse = _ => { plife.Play(idx); Say(plife.transform, cf.P(0, 2.3f, .5f), result[(kk + plays++) % result.Length], 4f); };
            }
            // air hockey in the middle: slide the puck
            var ah = fo.P(2.2f, 0, .8f); mk.Box(white, ah + up * .8f, new Vector3(1.3f, .1f, 2.4f), f.r); mk.Box(GlowM(cyan, 1f), ah + up * .86f, new Vector3(1.1f, .02f, 2.2f), f.r); mk.Box(dark, ah + up * .4f, new Vector3(1.1f, .8f, 2f), f.r); Col(ah + up * .45f, new Vector3(1.3f, .9f, 2.4f), f.r);
            var pk = new MeshKit(); pk.Prism(WorldMaterials.Glow(pink, 1.4f), Vector3.zero, Vector3.up, .09f, .03f, 10); var pgo = pk.BuildSingle("Air hockey puck", plife.transform, false); pgo.transform.position = ah + up * .88f; plife.puck = pgo.transform; plife.puckC = ah + up * .88f; plife.puckX = f.Right * .45f; plife.puckZ = f.Fwd * 1f;
            var pt = Interact.Toy(pgo, Toy.Kind.Custom, "Hit the puck", new Vector3(1.3f, .5f, 2.4f)); pt.onUse = _ => plife.Hit();
            Folk(ah - f.Fwd * 1.7f, f.Fwd, Resident.Pose.Work, new[] { "First to seven. I'm on six. I've been on six since '84.", "Hit it! No mercy!", "Winner stays on. I always stay on." }, Fan()).workRate = 1.6f;
            Folk(fo.P(-3.6f + 1.6f, 0, -D / 2f + T + 1.65f), -f.Fwd, Resident.Pose.Work, new[] { "Can't talk. Third life.", "There's a pattern. I nearly have the pattern.", "ACE is my initials. Yes, all of them." }, Fan()).workRate = 2.4f;
            // a change machine and a bench by the door
            mk.Box(metal, fo.P(W / 2f - T - .4f, .9f, 2.4f), new Vector3(.6f, 1.8f, .8f), f.r); mk.Box(GlowM(new Color(1f, .85f, .3f), 1.6f), fo.P(W / 2f - T - .72f, 1.3f, 2.4f), new Vector3(.03f, .4f, .5f), f.r); Col(fo.P(W / 2f - T - .4f, .9f, 2.4f), new Vector3(.6f, 1.8f, .8f), f.r);
            ArtistBits.PointLight(ctx.Root, fo.P(-2f, 2.7f, 0), pink, 12f, 3.2f, true); ArtistBits.PointLight(ctx.Root, fo.P(3f, 2.7f, 0), cyan, 12f, 3.2f, true); mk.Box(GlowM(cyan, 2f), fo.P(0, H - .3f, -D / 2f + T + .04f), new Vector3(W - 1f, .08f, .04f), f.r);
            MetalBench(mk, f.P(-4.4f, 0, D / 2f + 1.3f), f.Fwd);
            Lane(ctx, mk, f.P(0, 0, D / 2f + 1.5f)); Ring(g, 10f);
            Shot(ctx, "the arcade", f.P(-7f, 2.6f, D / 2f + 12f), fo.P(0, 2.6f, 0));
            Shot(ctx, "inside the arcade", fo.P(4.4f, 1.6f, D / 2f - 1.1f), fo.P(-2.5f, 1.2f, -D / 2f + 1f));
        }

        // ---------------------------------------------------------------- the Ballad Pool
        void BalladPool(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var g = ctx.OnGround(PoolAt.x, PoolAt.y); var f = new PlaceFrame(g, FaceTo(g, Nearest(roadPts, g))); const float R = 9f;
            mk.Prism(stone, g - up * .5f, up, R + 1.2f, .8f, 20); solid.Prism(stone, g - up * .5f, up, R + 1.2f, .5f, 20);
            var waterM = new Material(WorldMaterials.Glow(new Color(.08f, .14f, .32f), .25f)) { name = "Ballad pool" }; waterM.SetFloat("_Smoothness", .95f); mk.Prism(waterM, g + up * .31f, up, R, .03f, 20);
            // a low rim you can step over is no good: a kerb to keep you out of the water, with gaps for nothing
            for (var k = 0; k < 20; k++) { var a0 = k * Mathf.PI / 10f; var a1 = (k + 1) * Mathf.PI / 10f; var p0 = g + new Vector3(Mathf.Cos(a0), 0, Mathf.Sin(a0)) * (R + .2f); var p1 = g + new Vector3(Mathf.Cos(a1), 0, Mathf.Sin(a1)) * (R + .2f); mk.Box(white, (p0 + p1) / 2f + up * .45f, new Vector3(.4f, .3f, Vector3.Distance(p0, p1) + .05f), Quaternion.LookRotation(p1 - p0)); Col((p0 + p1) / 2f + up * .7f, new Vector3(.4f, 1.4f, Vector3.Distance(p0, p1) + .05f), Quaternion.LookRotation(p1 - p0)); }
            // floating lights, low and few until you raise your lighter
            var rng = new System.Random(77);
            for (var k = 0; k < 40; k++) { var a = (float)rng.NextDouble() * Mathf.PI * 2f; var r = Mathf.Sqrt((float)rng.NextDouble()) * (R - .8f); var lm = new MeshKit(); lm.Blob(WorldMaterials.Glow(k % 4 == 0 ? new Color(.6f, .8f, 1f) : Flame, 1.6f), Vector3.zero, Vector3.one * .09f, ctx.Rng, 0f); var go = lm.BuildSingle("Ballad light", plife.transform, false); var p = g + new Vector3(Mathf.Cos(a) * r, .42f, Mathf.Sin(a) * r); go.transform.position = p; plife.lights.Add((go.transform, p, (float)rng.NextDouble() * 6f)); }
            plife.poolLight = ArtistBits.PointLight(ctx.Root, g + up * 2.5f, Flame, 26f, 1f);
            var pgo = new GameObject("Lighter plinth"); pgo.transform.SetParent(plife.transform, false); var pp = G(f.P(0, 0, R + 2.6f)); pgo.transform.position = pp + up * 1.1f; mk.Prism(stone, pp, up, .35f, .9f, 8); mk.Prism(metal, pp + up * .9f, up, .08f, .22f, 6); mk.Blob(GlowM(Flame, 3f), pp + up * 1.25f, new Vector3(.06f, .12f, .06f), ctx.Rng, 0f); ArtistBits.PostCollider(ctx.Root, pp, .9f, .35f);
            Interact.Toy(pgo, Toy.Kind.Custom, "Raise your lighter", new Vector3(1f, 1.4f, 1f)).onUse = _ => { plife.raised = Time.time; Say(plife.transform, pp + up * 2.4f, "The lights lift off the water, one by one.", 5f); };
            for (var k = 0; k < 4; k++) { var d = Quaternion.Euler(0, k * 90f + 45f, 0) * f.Fwd; var bp = g + d * (R + 2.8f); MetalBench(mk, bp, -d, 2.2f); }
            var sway = G(g + Quaternion.Euler(0, 45f, 0) * f.Fwd * (R + 2.8f)); Folk(sway + up * .5f + Vector3.Cross(up, -(Quaternion.Euler(0, 45f, 0) * f.Fwd)) * .5f, -(Quaternion.Euler(0, 45f, 0) * f.Fwd), Resident.Pose.Sit, new[] { "This is where you come for the slow ones.", "Every power ballad ever written ends up in this pool. That's what the lights are.", "Raise the lighter. Go on. It's what it's for." }, Fan(), null, .5f);
            var cp = G(g + Quaternion.Euler(0, -110f, 0) * f.Fwd * (R + 2.4f)); var l1 = Fan(); var l2 = Fan(); l2.longHair = true; var cd = FaceTo(cp, g); var cs = Vector3.Cross(up, cd);
            Folk(cp - cs * .35f, cd, Resident.Pose.Stand, new[] { "We met at the first chorus.", "He cried at the key change. I saw." }, l1); Folk(cp + cs * .35f, cd, Resident.Pose.Stand, new[] { "I did not cry at the key change.", "It's our song. They're all our song." }, l2);
            Marquee(mk, f.P(6f, 0, R + 7f), f.Fwd, "THE BALLAD POOL", 3f);
            Lane(ctx, mk, f.P(0, 0, R + 3.6f)); Ring(g, R + 4f);
            Shot(ctx, "the ballad pool", f.P(-8f, 3.6f, R + 9f), g + up * .6f);
        }

        // ---------------------------------------------------------------- Backstage
        void Backstage(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var g = ctx.OnGround(BackstageAt.x, BackstageAt.y); var f = new PlaceFrame(g, FaceTo(g, Nearest(roadPts, g)));
            // the gate: a barrier and an ALL ACCESS board
            foreach (var e in new[] { -1f, 1f }) { var p = G(f.P(e * 3f, 0, 10f)); mk.Box(metal, p + up * .55f, new Vector3(2.4f, 1.1f, .08f), f.r); Col(p + up * .55f, new Vector3(2.4f, 1.1f, .15f), f.r); }
            // the catering tent: white canvas on poles, two long tables with benches, an urn
            var tc = f.P(-5f, 0, 0); foreach (var (x, z) in new[] { (-3.5f, -2.5f), (3.5f, -2.5f), (3.5f, 2.5f), (-3.5f, 2.5f) }) { var p = G(tc + f.r * new Vector3(x, 0, z)); mk.Prism(metal, p, up, .06f, 2.8f, 6); ArtistBits.PostCollider(ctx.Root, p, 2.8f, .08f); }
            mk.GableRoof(white, G(tc) + up * 2.8f, new Vector3(7.6f, 0, 5.6f), 1.3f, f.r);
            for (var k = 0; k < 9; k++) { var p = G(tc) + up * 2.7f + f.Right * (-3.4f + k * .85f) + f.Fwd * 2.5f; mk.Blob(GlowM(new Color(1f, .85f, .5f), 2.4f), p, Vector3.one * .08f, ctx.Rng, 0f); if (k % 3 == 1) halos.Add(p, .7f, new Color(1f, .8f, .5f), .6f); }
            foreach (var z in new[] { -1.1f, 1.1f })
            {
                var tp = G(tc + f.Fwd * z); mk.Box(wood, tp + up * .75f, new Vector3(5f, .07f, .8f), f.r); foreach (var e in new[] { -2.2f, 2.2f }) mk.Box(metal, tp + up * .37f + f.Right * e, new Vector3(.06f, .74f, .7f), f.r); Col(tp + up * .4f, new Vector3(5f, .8f, .8f), f.r);
                var bp = tp + f.Fwd * Mathf.Sign(z) * .85f; mk.Box(wood, bp + up * .45f, new Vector3(5f, .06f, .35f), f.r); foreach (var e in new[] { -2.2f, 2.2f }) mk.Box(metal, bp + up * .22f + f.Right * e, new Vector3(.06f, .45f, .3f), f.r); Col(bp + up * .25f, new Vector3(5f, .5f, .35f), f.r);
                for (var s = 0; s < 3; s++) if (!(z < 0 && s == 1)) Interact.Seat(plife.transform, bp + up * .5f + f.Right * (-1.6f + s * 1.6f), -f.Fwd * Mathf.Sign(z), "Sit at the catering table");
                for (var s = 0; s < 3; s++) mk.Prism(WorldMaterials.Solid(Paint[(s + (z > 0 ? 2 : 0)) % Paint.Length]), tp + up * .79f + f.Right * (-1.5f + s * 1.5f), up, .16f, .03f, 10);
            }
            Folk(G(tc - f.Fwd * 1.95f) + up * .5f, f.Fwd, Resident.Pose.Sit, new[] { "Load-in was at six. In 1983. We're nearly set up.", "Chili again. It's always chili. I love the chili.", "If it's got wheels or a jack plug, it's my problem." }, Fan(Residents.Tool.Mug), null, .5f);
            // road cases, stacked: some to shove
            for (var k = 0; k < 5; k++) { var p = G(f.P(3f + (k % 3) * 1.7f, 0, -4f + (k / 3) * 1.5f)); var s = new Vector3(1.4f, k == 1 ? 1.6f : .9f, 1f); mk.Box(dark, p + up * (s.y / 2f + .1f), s, f.r); mk.Box(metal, p + up * (s.y + .1f), new Vector3(s.x + .04f, .05f, s.z + .04f), f.r); mk.Box(metal, p + up * .3f, new Vector3(s.x + .04f, .05f, s.z + .04f), f.r); Col(p + up * (s.y / 2f + .1f), s, f.r); }
            // the follow spot on a stand: swing it and its beam sweeps the trees
            var sp = G(f.P(6f, 0, 3f)); mk.Prism(metal, sp, up, .07f, 1.5f, 6); for (var k = 0; k < 3; k++) mk.Box(metal, sp + up * .25f, new Vector3(.05f, .05f, 1.3f), Quaternion.Euler(0, k * 60f, 0)); ArtistBits.PostCollider(ctx.Root, sp, 1.5f, .12f);
            var sm = new MeshKit(); sm.Prism(dark, new Vector3(0, 0, -.5f), Vector3.forward, .28f, 1f, 10); sm.Prism(GlowM(Spot, 3f), new Vector3(0, 0, .5f), Vector3.forward, .25f, .03f, 10); var beamM = new Material(WorldMaterials.Glow(Spot, .5f)) { name = "Follow spot beam" }; sm.Prism(beamM, new Vector3(0, 0, .55f), Vector3.forward, .25f, 30f, 8, 6f, 0f, false);
            var sgo = sm.BuildSingle("Soft follow spot", plife.transform, false); sgo.transform.SetPositionAndRotation(sp + up * 1.8f, Quaternion.LookRotation((f.Fwd * .3f + f.Right + up * .35f).normalized));
            var stt = Interact.Toy(sgo, Toy.Kind.Custom, "Swing the follow spot", new Vector3(1f, 1f, 1.4f)); stt.onUse = _ => plife.SwingSpot(); plife.spot = sgo.transform; plife.spotBase = sgo.transform.rotation;
            Folk(G(sp - f.Right * 1.2f), f.Right, Resident.Pose.Stand, new[] { "Keep it on the singer. There's no singer back here? Keep it on the chili, then.", "Thirty years on this lamp. Never once seen the show; I'm always looking at the light." }, Fan());
            // a guitar tech at a rack of guitars
            var gr = G(f.P(-10f, 0, -4f)); mk.Box(dark, gr + up * .12f, new Vector3(3.2f, .24f, .7f), f.r); mk.Box(metal, gr + up * 1f - f.Fwd * .2f, new Vector3(3.2f, .05f, .05f), f.r); foreach (var e in new[] { -1.55f, 1.55f }) mk.Box(metal, gr + up * .6f + f.Right * e - f.Fwd * .2f, new Vector3(.05f, 1f, .05f), f.r); Col(gr + up * .6f, new Vector3(3.2f, 1.2f, .7f), f.r);
            for (var k = 0; k < 5; k++) { var gp = gr + f.Right * (-1.3f + k * .65f) + up * .3f; var gc = WorldMaterials.Solid(Paint[k % Paint.Length], .7f); var gq = f.r * Quaternion.Euler(-8f, 0, 0); mk.Box(gc, gp + up * .2f, new Vector3(.36f, .46f, .07f), gq); mk.Box(wood, gp + up * .8f - f.Fwd * .08f, new Vector3(.07f, .8f, .04f), gq); }
            var tech = Fan(Residents.Tool.Rod); Folk(G(gr + f.Fwd * 1.1f), -f.Fwd, Resident.Pose.Work, new[] { "Five guitars. He plays one. The other four are for throwing.", "E, A, D, G, B, E. Then he detunes it on purpose. Artists.", "Don't touch the white one. Okay, touch it. Don't tell him." }, tech).workRate = .8f;
            ArtistBits.PointLight(ctx.Root, G(tc) + up * 2.3f, new Color(1f, .85f, .6f), 14f, 1.6f); Lamp(ctx, mk, ctx.Root, G(f.P(9f, 0, -5f)));
            Marquee(mk, f.P(6.5f, 0, 12f), f.Fwd, "BACKSTAGE\nALL ACCESS (TONIGHT ONLY)", 4f, .15f);
            Lane(ctx, mk, f.P(0, 0, 6f)); Ring(g, 13f);
            Shot(ctx, "backstage", f.P(-11f, 3.6f, 13f), g + up * 1.2f);
        }

        // ---------------------------------------------------------------- Lighter Hill
        void LighterHill(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var g = ctx.OnGround(LighterAt.x, LighterAt.y); var f = new PlaceFrame(g, FaceTo(g, new Vector3(center.x, 0, center.z)));
            // a lighter as tall as a house: chrome case, a thumb wheel, a flame that catches when you flick it
            var b = G(f.P(-2.5f, 0, -2f)); mk.Box(metal, b + up * 2.4f, new Vector3(1.8f, 4.8f, .9f), f.r); solid.Box(metal, b + up * 2.4f, new Vector3(1.8f, 4.8f, .9f), f.r); mk.Box(WorldMaterials.Solid(new Color(.75f, .15f, .15f), .6f), b + up * 1.9f, new Vector3(1.84f, 3.6f, .94f), f.r);
            mk.Box(metal, b + up * 5.1f - f.Right * .3f, new Vector3(1f, .6f, .7f), f.r); mk.Prism(dark, b + up * 5.1f + f.Right * .55f - f.Fwd * .2f, f.Fwd, .3f, .4f, 12);
            var flameM = new Material(WorldMaterials.Glow(Flame, .05f)) { name = "Giant lighter flame" }; var fk = new MeshKit(); fk.Cone(flameM, Vector3.zero, .45f, 1.9f, 7); fk.Cone(flameM, new Vector3(0, 0, 0), .3f, -.3f, 7);
            var fgo = fk.BuildSingle("Soft lighter flame", plife.transform, false); fgo.transform.position = b + up * 5.5f - f.Right * .3f; plife.flameM = flameM; plife.flame = fgo.transform; plife.flameLight = ArtistBits.PointLight(ctx.Root, b + up * 6.6f - f.Right * .3f, Flame, 45f, 0f);
            var tgo = new GameObject("Giant lighter"); tgo.transform.SetParent(plife.transform, false); tgo.transform.position = b + up * 1.3f + f.Fwd * .6f;
            Interact.Toy(tgo, Toy.Kind.Custom, "Flick the lighter", new Vector3(1.8f, 2.2f, .8f)).onUse = _ => { plife.lit = !plife.lit; Say(plife.transform, b + up * 3.4f + f.Fwd * 1.2f, plife.lit ? "It catches. Somewhere in the stadium, ten thousand answer." : "You snap it shut.", 4f); };
            MetalBench(mk, f.P(1.6f, 0, .5f), f.Fwd, 2.4f); MetalBench(mk, f.P(4.6f, 0, -.6f), (f.Fwd - f.Right * .4f).normalized, 2f);
            Folk(G(f.P(-.2f, 0, 2.6f)), f.Fwd, Resident.Pose.Stand, new[] { "From up here it's just lights. That's all a show is, from far enough off. I like it better.", "Flick it. They can see it from the cheap seats.", "I hold mine up for the whole song. My arm's fine. Don't ask about my arm." }, Fan());
            Ring(g, 6f);
            Shot(ctx, "lighter hill", f.P(6f, 2.6f, -9f), b + up * 3.2f + f.Fwd * 4f);
        }
    }

    /// The Arena places' moving parts: the drive-in film, the arcade screens, the air hockey puck, the ballad lights,
    /// the follow spot, the giant lighter.
    public class ArenaPlacesLife : MonoBehaviour
    {
        public Material screenM, flameM; public Light screenLight, poolLight, flameLight; public Transform flame, puck, spot; public Quaternion spotBase;
        public List<(Transform t, Vector3 at, Vector3 right, int k)> film = new List<(Transform, Vector3, Vector3, int)>();
        public List<(Material m, Color c, float hit)> screens = new List<(Material, Color, float)>();
        public List<(Transform t, Vector3 at, float phase)> lights = new List<(Transform, Vector3, float)>();
        public Vector3 puckC, puckX, puckZ; Vector2 pv, pp; public float raised = -99f; public bool lit; float flameV, lift, swung = -99f;
        static readonly Color Flame = new Color(1f, .62f, .25f), Cool = new Color(.85f, .9f, 1f);

        public void Play(int i) { var s = screens[i]; s.hit = Time.time; screens[i] = s; }
        public void Hit() { var a = Random.value * Mathf.PI * 2f; pv = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 3.2f; if (Mathf.Abs(pv.y) < 1.5f) pv.y = Mathf.Sign(pv.y + .01f) * 2.4f; }
        public void SwingSpot() { swung = Time.time; }

        void Update()
        {
            var t = Time.time;
            // the film: the band sways, the stage lights slide, the whole screen flickers a little
            foreach (var (tr, at, right, k) in film) { if (k < 3) tr.position = at + right * Mathf.Sin(t * (.9f + k * .3f) + k * 2f) * .7f + Vector3.up * Mathf.Abs(Mathf.Sin(t * 3f + k)) * .12f; else tr.position = at + right * Mathf.Sin(t * .7f + k) * 3f; }
            if (screenM != null) { var fl = .8f + .2f * Mathf.PerlinNoise(t * 5f, 3f); screenM.SetColor("_EmissionColor", Cool * (.9f * fl)); if (screenLight != null) screenLight.intensity = 2.2f * fl; }
            for (var i = 0; i < screens.Count; i++) { var s = screens[i]; var age = t - s.hit; var v = age < 4f ? 1f + 3f * Mathf.Abs(Mathf.Sin(age * 12f)) * (1f - age / 4f) : .8f + .2f * Mathf.Sin(t * 2f + i); s.m.SetColor("_EmissionColor", s.c * (.8f * v)); }
            if (puck != null)
            {
                pp += pv * Time.deltaTime; if (Mathf.Abs(pp.x) > 1f) { pp.x = Mathf.Sign(pp.x); pv.x = -pv.x; } if (Mathf.Abs(pp.y) > 1f) { pp.y = Mathf.Sign(pp.y); pv.y = -pv.y; }
                pv *= Mathf.Exp(-Time.deltaTime * .35f); puck.position = puckC + puckX * pp.x + puckZ * pp.y;
            }
            // the ballad lights: bob on the water; rise for half a minute when the lighter goes up
            var want = t - raised < 30f ? 1f : 0f; lift = Mathf.MoveTowards(lift, want, Time.deltaTime * .25f);
            foreach (var (tr, at, ph) in lights) tr.position = at + Vector3.up * (Mathf.Sin(t * .8f + ph) * .04f + lift * (1.2f + 2.6f * Mathf.Abs(Mathf.Sin(ph * 3.3f))) * (.85f + .15f * Mathf.Sin(t * .5f + ph)));
            if (poolLight != null) poolLight.intensity = 1f + lift * 2.6f;
            if (spot != null) { var age = t - swung; var a = age < 8f ? Mathf.Sin(age * 1.4f) * 55f * (1f - age / 8f) : 0f; spot.rotation = Quaternion.AngleAxis(a, Vector3.up) * spotBase; }
            flameV = Mathf.MoveTowards(flameV, lit ? 1f : 0f, Time.deltaTime * 3f);
            if (flame != null) { var fl = .85f + .15f * Mathf.PerlinNoise(t * 8f, 0f); flame.localScale = new Vector3(1f, .05f + flameV * fl, 1f); if (flameM != null) flameM.SetColor("_EmissionColor", Flame * (.05f + flameV * 4.5f * fl)); if (flameLight != null) flameLight.intensity = flameV * 4f * fl; }
        }
    }
}
