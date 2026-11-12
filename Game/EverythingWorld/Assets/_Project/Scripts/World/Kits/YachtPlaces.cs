using System.Collections.Generic;
using UnityEngine;
using static EverythingWorld.World.PlaceTools;

namespace EverythingWorld.World
{
    /// The Yacht Rock Riviera laid out on purpose (the build-land standard). The bay, the promenade and every artist
    /// stay where they were; four more pale stone roads with lamps and palms reach the quiet hills: west past the
    /// winery to the Lido, south to the tennis club, north to the studio, east to the sunset gazebo. Along them, with
    /// the Riviera's people: the Lido (a pool with loungers, a diving board, a beach ball, a cabana bar); the Tennis
    /// Club (a clay court, an umpire's chair, a ball machine, balls to throw); the Winery (a terrace of tables under a
    /// pergola, barrels, grapes); Smooth Sound Studios (a studio to walk into, a mixing desk with faders to push, gold
    /// records, a saxophonist who has been on take 41 for some time); and the Sunset Gazebo with its telescope.
    /// THE EAST MEADOW BEYOND THE GAZEBO IS LEFT EMPTY ON PURPOSE: it is the room kept for the owner's mini golf.
    public partial class YachtKit
    {
        static readonly Vector2 LidoAt = new Vector2(6005, 4216), TennisAt = new Vector2(6320, 4018), WineryAt = new Vector2(6128, 4048), StudioAt = new Vector2(6560, 4436), GazeboAt = new Vector2(6702, 4122);
        static readonly Vector2[] WestWay = { new Vector2(6190, 4132), new Vector2(6152, 4092), new Vector2(6098, 4120), new Vector2(6045, 4165), new Vector2(6020, 4195) };
        static readonly Vector2[] SouthWay = { new Vector2(6318, 4100), new Vector2(6316, 4070), new Vector2(6320, 4046) };
        static readonly Vector2[] NorthWay = { new Vector2(6552, 4372), new Vector2(6558, 4400), new Vector2(6560, 4420) };
        static readonly Vector2[] EastWay = { new Vector2(6682, 4150), new Vector2(6705, 4105), new Vector2(6735, 4075), new Vector2(6752, 4052) };
        static readonly (Vector2 at, float r)[] PlaceRings = { (LidoAt, 15f), (TennisAt, 19f), (WineryAt, 13f), (StudioAt, 11f), (GazeboAt, 6f) };

        YachtPlacesLife plife; System.Random prng; Transform rootT; LandContext ctx0; readonly List<Vector3> roadPts = new List<Vector3>();

        static readonly Color[] Polos = { new Color(.96f, .95f, .92f), new Color(1f, .6f, .55f), new Color(.45f, .7f, .9f), new Color(1f, .85f, .45f), new Color(.5f, .8f, .65f), new Color(.15f, .22f, .45f) };
        static readonly Color[] Slacks = { new Color(.95f, .93f, .88f), new Color(.85f, .75f, .6f), new Color(.15f, .22f, .45f), new Color(.9f, .5f, .45f) };
        static readonly Residents.Hat[] Lids = { Residents.Hat.None, Residents.Hat.Cap, Residents.Hat.Straw, Residents.Hat.None, Residents.Hat.Bucket };
        static readonly string[] Smooth70s =
        {
            "Lovely evening. It's always evening. That's the lovely part.", "The sun's been setting since '78. Nobody's in a hurry.", "Have you tried the bay? You don't swim in it. You just regard it.",
            "I came for a weekend. The saxophone solo hasn't finished yet.", "Everything here is in a major seventh.",
        };

        Residents.Look Smoothie(Residents.Tool tool = Residents.Tool.None, bool apron = false) { var l = Residents.Random(prng, Polos, Slacks, Lids, Polos); l.tool = tool; l.apron = apron; return l; }
        Resident Folk(Vector3 feet, Vector3 face, Resident.Pose pose, string[] lines = null, Residents.Look look = null, Vector3[] route = null, float seatH = HumanScale.SeatHeight)
            => Residents.Make(plife.transform, feet, face, look ?? Smoothie(), pose, prng, lines ?? Smooth70s, route, seatH);
        void Ring(Vector3 at, float r) => noGrass.Add(new Vector3(at.x, r, at.z));
        void Col(Vector3 c, Vector3 size, Quaternion rot) => BoxCol(rootT, c, size, rot);
        Vector3 G(Vector3 p) => ctx0.OnGround(p.x, p.z);

        /// A white board with navy letters. Signs go on the +x side of a place's lane; the photo spot on the −x side.
        void Placard(MeshKit mk, Vector3 at, Vector3 face, string text, float w = 2.6f, float size = .17f)
        {
            var up = Vector3.up; var g = G(at); var rot = Quaternion.LookRotation(face);
            foreach (var e in new[] { -1f, 1f }) mk.Box(white, g + rot * Vector3.right * e * (w / 2f - .1f) + up * .9f, new Vector3(.1f, 1.8f, .1f), rot);
            mk.Box(white, g + up * 1.55f, new Vector3(w, .7f, .08f), rot); mk.Box(navy, g + up * 1.92f, new Vector3(w, .06f, .1f), rot); Col(g + up * 1f, new Vector3(w, 2f, .15f), rot);
            ArtistBits.Label(rootT, text, g + up * 1.57f + face * .05f, face, size, Navy, TextAnchor.MiddleCenter);
        }

        void TeakBench(MeshKit mk, Vector3 at, Vector3 face, float len = 1.9f)
        {
            var up = Vector3.up; var g = G(at); var rot = Quaternion.LookRotation(face); var side = Vector3.Cross(up, face);
            mk.Box(teak, g + up * .45f, new Vector3(len, .08f, .5f), rot); mk.Box(teak, g + up * .8f - face * .25f, new Vector3(len, .5f, .06f), rot); foreach (var e in new[] { -1f, 1f }) mk.Box(white, g + up * .22f + side * e * (len / 2f - .15f), new Vector3(.08f, .45f, .45f), rot);
            Col(g + up * .5f, new Vector3(len, 1f, .6f), rot); Interact.Bench(plife.transform, g + up * .5f - side * (len / 2f - .4f), g + up * .5f + side * (len / 2f - .4f), face);
        }

        /// A round table with a parasol and chairs you can sit on.
        void CafeTable(MeshKit mk, Vector3 at, Color shade, int chairs = 2)
        {
            var up = Vector3.up; var g = G(at); mk.Prism(white, g, up, .06f, .72f, 6); mk.Prism(white, g + up * .72f, up, .55f, .05f, 12); ArtistBits.PostCollider(rootT, g, .8f, .45f);
            mk.Prism(white, g + up * .77f, up, .03f, 1.7f, 5); mk.Cone(WorldMaterials.Solid(shade, .3f), g + up * 2.3f, 1.5f, .55f, 8);
            for (var k = 0; k < chairs; k++) { var d = Quaternion.Euler(0, k * (360f / chairs) + 40f, 0) * Vector3.forward; var c = g + d * 1.05f; var rot = Quaternion.LookRotation(-d); mk.Box(white, c + up * .45f, new Vector3(.42f, .05f, .42f), rot); mk.Box(white, c + up * .75f + d * .2f, new Vector3(.42f, .55f, .04f), rot); mk.Box(white, c + up * .22f, new Vector3(.36f, .45f, .36f), rot); Col(c + up * .45f, new Vector3(.45f, .9f, .45f), rot); Interact.Seat(plife.transform, c + up * .5f, -d, "Sit at the table"); }
        }

        void PlaceBend(LandContext ctx, List<Vector3> pts)
        {
            for (var i = 0; i < pts.Count; i++)
            {
                foreach (var a in ctx.Artists) { var r = Foot(a) + 7f; var v = new Vector2(pts[i].x - a.x, pts[i].z - a.z); var d = v.magnitude; if (d < r) { v = d > .01f ? v / d * r : new Vector2(r, 0); pts[i] = ctx.OnGround(a.x + v.x, a.z + v.y); } }
                foreach (var c in noGrass) { if (c.y < 4f || c.y > 60f) continue; var r = c.y + 3f; var v = new Vector2(pts[i].x - c.x, pts[i].z - c.z); var d = v.magnitude; if (d < r) { v = d > .01f ? v / d * r : new Vector2(r, 0); pts[i] = ctx.OnGround(c.x + v.x, c.z + v.y); } }
            }
        }

        List<Vector3> StoneRoad(LandContext ctx, MeshKit mk, Vector2[] way)
        {
            Vector2 near = way[0]; var bd = 160f * 160f; foreach (var (p, _) in walkPoints) { var d = (p.x - way[0].x) * (p.x - way[0].x) + (p.z - way[0].y) * (p.z - way[0].y); if (d < bd) { bd = d; near = new Vector2(p.x, p.z); } }
            var full = new List<Vector2> { near }; full.AddRange(way);
            var pts = Smooth(ctx, full, false, 2f); PlaceBend(ctx, pts); pts.RemoveAll(p => InBay(p.x, p.z, 4f)); if (pts.Count < 2) return pts;
            Strip(mk, pts, stone, 3.4f, .06f);
            var up = Vector3.up; var n = 0;
            for (var i = 6; i < pts.Count; i += 11)
            {
                var d = pts[i] - pts[i - 1]; d.y = 0; if (d.sqrMagnitude < .01f) continue; d.Normalize(); var sd = Vector3.Cross(up, d) * (n % 2 == 0 ? 1f : -1f); var lp = pts[i] + sd * 2.4f;
                if (ctx.IsLand(lp.x, lp.z, 2f)) { if (n % 3 == 2) Palm(ctx, mk, ctx.OnGround(lp.x, lp.z), ctx.Range(7f, 9f)); else Lamp(ctx, mk, ctx.OnGround(lp.x, lp.z)); }
                n++;
            }
            foreach (var p in pts) Remember(p, 700);
            roadPts.AddRange(pts);
            if (pts.Count > 30) { var i = pts.Count / 2; var d = pts[i + 1] - pts[i - 1]; d.y = 0; d.Normalize(); var sd = Vector3.Cross(up, d); var bp = pts[i] - sd * 3.2f; if (ctx.IsLand(bp.x, bp.z, 2f)) { TeakBench(mk, bp, sd); Ring(bp, 2f); } }
            return pts;
        }

        void Lane(LandContext ctx, MeshKit mk, Vector3 to, float stopShort = 0f)
        {
            var from = Nearest(roadPts, to); var d = new Vector2(to.x - from.x, to.z - from.z); var len = d.magnitude; if (len < stopShort + 3f) return; d /= len;
            var a = new Vector2(from.x, from.z) + d * 1.8f; var b = new Vector2(from.x, from.z) + d * (len - stopShort);
            var pts = Smooth(ctx, new[] { a, Vector2.Lerp(a, b, .5f), b }, false, 2f, 1f); if (pts.Count < 2) return;
            Strip(mk, pts, stone, 2.4f, .055f); foreach (var p in pts) Remember(p, 900);
        }

        void RivieraPlaces(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            roadPts.Clear(); rootT = ctx.Root; ctx0 = ctx; prng = new System.Random(1979);
            plife = new GameObject("Riviera places life").AddComponent<YachtPlacesLife>(); plife.transform.SetParent(life.transform, false);
            foreach (var (at, r) in PlaceRings) noGrass.Add(new Vector3(at.x, r, at.y));
            StoneRoad(ctx, mk, WestWay); StoneRoad(ctx, mk, SouthWay); StoneRoad(ctx, mk, NorthWay); StoneRoad(ctx, mk, EastWay);
            Lido(ctx, mk, solid); TennisClub(ctx, mk, solid); Winery(ctx, mk, solid); Studio(ctx, mk, solid); Gazebo(ctx, mk, solid);
            if (hasBay) PromenadePeople(ctx, mk);
        }

        // ---------------------------------------------------------------- people of the promenade
        void PromenadePeople(LandContext ctx, MeshKit mk)
        {
            var up = Vector3.up; var ring = new List<Vector3>();
            for (var k = 0; k < 48; k++) { var a = k * Mathf.PI * 2f / 48f; var dx = Mathf.Cos(a); var dz = Mathf.Sin(a); var r = BayEdge(bayC.x + dx, bayC.y + dz) + PromW / 2f + 2.2f; var x = bayC.x + dx * r; var z = bayC.y + dz * r; if (!ctx.IsLand(x, z, 2f)) return; ring.Add(new Vector3(x, Mathf.Max(ctx.Ground(x, z), bayY + .6f) + .08f, z)); }
            // two strollers going opposite ways round the bay
            Folk(ring[0], FaceTo(ring[0], ring[1]), Resident.Pose.Walk, new[] { "Twice round the bay before dinner. It's always before dinner.", "Mind the piano. No, the other piano.", "That yacht? Mine. Well. I wave at it." }, Smoothie(), ring.ToArray()).speed = 1.1f;
            var back = new List<Vector3>(ring); back.Reverse(); for (var i = 0; i < back.Count; i++) back[i] += (back[i] - new Vector3(bayC.x, back[i].y, bayC.y)).normalized * 1.2f; var rot = new List<Vector3>(); for (var i = 0; i < back.Count; i++) rot.Add(back[(i + 20) % back.Count]);
            var l2 = Smoothie(); l2.longHair = true; Folk(rot[0], FaceTo(rot[0], rot[1]), Resident.Pose.Walk, new[] { "He walks one way, I walk the other. We've been married forty years.", "The light does this every night. I still stop for it." }, l2, rot.ToArray()).speed = 1f;
            // a gelato cart and a saxophonist where you arrive
            var toBay = new Vector3(bayC.x - center.x, 0, bayC.y - center.z).normalized; var side = Vector3.Cross(up, toBay);
            var cp = G(center + side * 9f - toBay * 2f); var cr = Quaternion.LookRotation(-side);
            mk.Box(white, cp + up * .75f, new Vector3(1.7f, .9f, .9f), cr); foreach (var e in new[] { -.6f, .6f }) mk.Prism(dark, cp + up * .3f + cr * new Vector3(e, 0, .46f), cr * Vector3.forward, .3f, .06f, 10); for (var s = 0; s < 5; s++) mk.Box(WorldMaterials.Solid(s % 2 == 0 ? new Color(1f, .5f, .55f) : new Color(.96f, .95f, .9f), .3f), cp + up * 2.2f + cr * new Vector3(-.72f + s * .36f, 0, 0), new Vector3(.36f, .05f, 1.3f), cr); foreach (var e in new[] { -.8f, .8f }) mk.Box(white, cp + up * 1.7f + cr * new Vector3(e, 0, 0), new Vector3(.04f, 1f, .04f), cr);
            for (var s = 0; s < 3; s++) mk.Blob(WorldMaterials.Solid(new[] { new Color(1f, .7f, .75f), new Color(.75f, .95f, .7f), new Color(.98f, .92f, .7f) }[s]), cp + up * 1.25f + cr * new Vector3(-.45f + s * .45f, 0, 0), Vector3.one * .16f, ctx.Rng, .1f);
            Col(cp + up * 1.1f, new Vector3(1.7f, 2.2f, 1.3f), cr); ArtistBits.Label(ctx.Root, "GELATO", cp + up * .8f + cr * Vector3.forward * .47f, cr * Vector3.forward, .16f, Navy, TextAnchor.MiddleCenter);
            Folk(G(cp - cr * Vector3.forward * 1.1f), cr * Vector3.forward, Resident.Pose.Stand, new[] { "Pistachio, strawberry, or smooth. Smooth is vanilla. We just call it smooth.", "No charge. It's the Riviera; somebody's father is paying.", "Take it to the pier. It tastes better over water." }, Smoothie(Residents.Tool.Spatula, true)); Ring(cp, 2.5f);
            var sp = G(center - side * 8f + toBay * 1f); var sax = Smoothie(Residents.Tool.Rod); sax.hat = Residents.Hat.None; sax.top = new Color(.15f, .22f, .45f);
            Folk(sp, FaceTo(sp, center), Resident.Pose.Work, new[] { "Same solo, forty years. It's a long song.", "You want the fast one? There isn't a fast one.", "Stand there a minute. I play better when someone's pretending not to listen." }, sax).workRate = .6f;
        }

        // ---------------------------------------------------------------- the Lido
        void Lido(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var g = ctx.OnGround(LidoAt.x, LidoAt.y); var f = new PlaceFrame(g, FaceTo(g, Nearest(roadPts, g))); const float PW = 14f, PD = 7f;
            // the deck with the pool sunk into it: four slabs round the water
            var tile = WorldMaterials.Solid(new Color(.93f, .9f, .84f)); var poolM = new Material(WorldMaterials.Glow(new Color(.25f, .8f, .9f), .7f)) { name = "Pool water" }; poolM.SetFloat("_Smoothness", .9f);
            void Slab(float x, float z, float w, float d) { mk.Box(tile, f.P(x, -.3f, z), new Vector3(w, 1f, d), f.r); solid.Box(tile, f.P(x, -.3f, z), new Vector3(w, 1f, d), f.r); }
            Slab(0, -(PD / 2f + 2f), PW + 8f, 4f); Slab(0, PD / 2f + 2f, PW + 8f, 4f); Slab(-(PW / 2f + 2f), 0, 4f, PD); Slab(PW / 2f + 2f, 0, 4f, PD);
            mk.Box(poolM, f.P(0, -.05f, 0), new Vector3(PW, .1f, PD), f.r); mk.Box(WorldMaterials.Solid(new Color(.5f, .8f, .9f)), f.P(0, -1.2f, 0), new Vector3(PW, .2f, PD), f.r); solid.Box(tile, f.P(0, -1.2f, 0), new Vector3(PW, .2f, PD), f.r);
            halos.Add(f.P(0, .6f, 0), 5f, new Color(.3f, .85f, .95f), .35f); ArtistBits.PointLight(ctx.Root, f.P(0, 1.2f, 0), new Color(.4f, .9f, 1f), 16f, 1.6f);
            // the diving board, the ladder
            mk.Box(white, f.P(-PW / 2f - 1f, .45f, 0), new Vector3(3.2f, .08f, .5f), f.r); mk.Box(metal, f.P(-PW / 2f - 1.8f, .22f, 0), new Vector3(.5f, .45f, .5f), f.r); solid.Box(white, f.P(-PW / 2f - 1f, .4f, 0), new Vector3(3.2f, .15f, .5f), f.r);
            foreach (var e in new[] { -.25f, .25f }) mk.Prism(metal, f.P(PW / 2f - .2f, -.6f, 2f + e), up, .03f, 1.5f, 5);
            // loungers along the far side, parasols between
            Color[] stripes = { new Color(1f, .6f, .55f), new Color(.45f, .7f, .9f), new Color(1f, .85f, .45f) };
            for (var k = 0; k < 5; k++)
            {
                var p = f.P(-6f + k * 3f, .2f, -PD / 2f - 2.2f); mk.Box(white, p + up * .3f, new Vector3(.7f, .08f, 1.6f), f.r); mk.Box(WorldMaterials.Solid(stripes[k % 3]), p + up * .36f, new Vector3(.62f, .05f, 1.5f), f.r); mk.Box(white, p + up * .6f - f.Fwd * .95f, new Vector3(.7f, .08f, .7f), f.r * Quaternion.Euler(-55f, 0, 0));
                Col(p + up * .3f, new Vector3(.7f, .6f, 1.9f), f.r); if (k != 2) Interact.Seat(plife.transform, p + up * .42f, f.Fwd, "Lie on the lounger");
                if (k % 2 == 1) { var pp = f.P(-6f + k * 3f + 1.5f, .2f, -PD / 2f - 2.8f); mk.Prism(white, pp, up, .04f, 2.4f, 5); mk.Cone(WorldMaterials.Solid(stripes[(k + 1) % 3], .3f), pp + up * 2.2f, 1.7f, .6f, 8); ArtistBits.PostCollider(ctx.Root, pp, 2.4f, .06f); }
            }
            Folk(f.P(0, .62f, -PD / 2f - 2.2f), f.Fwd, Resident.Pose.Sit, new[] { "I'm not asleep. I'm appreciating.", "The pool's heated. By what, nobody asks.", "Throw the ball in. The water does a lovely thing with the light." }, Smoothie(Residents.Tool.Mug), null, .42f);
            // the cabana bar
            var cb = new PlaceFrame(f.P(PW / 2f + 2.2f, .2f, PD / 2f + 2.4f), -f.Right);
            foreach (var (x, z) in new[] { (-1.6f, -1.2f), (1.6f, -1.2f), (1.6f, 1.2f), (-1.6f, 1.2f) }) { mk.Prism(white, cb.P(x, 0, z), up, .07f, 2.5f, 6); ArtistBits.PostCollider(ctx.Root, cb.P(x, 0, z), 2.5f, .08f); }
            for (var s = 0; s < 6; s++) mk.Box(WorldMaterials.Solid(s % 2 == 0 ? Navy : new Color(.96f, .95f, .9f), .3f), cb.P(-1.5f + s * .6f, 2.6f, 0), new Vector3(.6f, .06f, 3.2f), cb.r);
            mk.Box(teak, cb.P(0, 1.02f, .9f), new Vector3(3f, .08f, .6f), cb.r); mk.Box(white, cb.P(0, .5f, .9f), new Vector3(2.9f, 1f, .5f), cb.r); Col(cb.P(0, .55f, .9f), new Vector3(3f, 1.1f, .6f), cb.r);
            for (var k = 0; k < 3; k++) { mk.Prism(GlowM(new[] { new Color(1f, .5f, .6f), new Color(.5f, 1f, .7f), new Color(1f, .85f, .4f) }[k], 1.3f), cb.P(-.8f + k * .8f, 1.06f, .9f), up, .06f, .2f, 6, 1.4f); var sp = cb.P(-.9f + k * .9f, 0, 1.8f); mk.Prism(metal, sp, up, .04f, .7f, 5); mk.Prism(navy, sp + up * .7f, up, .2f, .07f, 10); ArtistBits.PostCollider(ctx.Root, sp, .75f, .18f); Interact.Seat(plife.transform, sp + up * .78f, -cb.Fwd, "Sit at the bar"); }
            Folk(cb.P(0, 0, 0), cb.Fwd, Resident.Pose.Work, new[] { "It comes with an umbrella. Everything comes with an umbrella.", "Piña colada? You and everybody getting caught in the rain.", "Tab's open. It's been open since '79." }, Smoothie(Residents.Tool.Mug, true)).workRate = .6f;
            ArtistBits.PointLight(ctx.Root, cb.P(0, 2.1f, .5f), new Color(1f, .8f, .55f), 9f, 1.3f);
            // a beach ball and two pool floats to throw about
            var ballM = new MeshKit(); ballM.Blob(WorldMaterials.Solid(new Color(1f, .45f, .4f)), Vector3.zero, Vector3.one * .3f, ctx.Rng, 0f); ballM.Blob(WorldMaterials.Solid(new Color(.96f, .95f, .9f)), new Vector3(0, .08f, 0), new Vector3(.27f, .26f, .27f), ctx.Rng, 0f);
            var bgo = ballM.BuildSingle("Beach ball", plife.transform, false); bgo.transform.position = f.P(3f, .55f, PD / 2f + 1.6f); Interact.Carry(bgo, "the beach ball", .3f, g.y - 8f);
            for (var k = 0; k < 2; k++) { var rm = new MeshKit(); var rc = WorldMaterials.Solid(k == 0 ? new Color(1f, .8f, .3f) : new Color(1f, .55f, .7f)); for (var s = 0; s < 10; s++) { var a = s * Mathf.PI / 5f; rm.Box(rc, new Vector3(Mathf.Cos(a) * .35f, 0, Mathf.Sin(a) * .35f), new Vector3(.2f, .16f, .26f), Quaternion.Euler(0, -s * 36f, 0)); } var go = rm.BuildSingle("Pool ring", plife.transform, false); go.transform.position = f.P(-2f + k * 1.2f, .35f, PD / 2f + 1.4f); var c = Interact.Carry(go, "a pool ring", .4f, g.y - 8f); c.GetComponent<BoxCollider>().size = new Vector3(.9f, .18f, .9f); }
            foreach (var e in new[] { -1f, 1f }) Palm(ctx, mk, G(f.P(e * (PW / 2f + 5.5f), 0, -PD / 2f - 4.5f)), 9f);
            Placard(mk, f.P(6f, 0, PD / 2f + 8f), f.Fwd, "THE LIDO\nno running, no rushing", 3f, .15f);
            Lane(ctx, mk, f.P(0, 0, PD / 2f + 4f)); Ring(g, 14f);
            Shot(ctx, "the lido", f.P(-9f, 3.5f, PD / 2f + 11f), g + up * .6f);
        }

        // ---------------------------------------------------------------- the Tennis Club
        void TennisClub(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var g = ctx.OnGround(TennisAt.x, TennisAt.y); var f = new PlaceFrame(g, FaceTo(g, Nearest(roadPts, g))); const float CL = 23.8f, CW = 11f;   // the court runs across the lane
            var clay = WorldMaterials.Solid(new Color(.82f, .45f, .3f)); var green = WorldMaterials.Solid(new Color(.3f, .5f, .35f));
            mk.Box(green, g - up * .42f, new Vector3(CL + 8f, 1f, CW + 7f), f.r); solid.Box(green, g - up * .42f, new Vector3(CL + 8f, 1f, CW + 7f), f.r); mk.Box(clay, g + up * .09f, new Vector3(CL, .02f, CW), f.r);
            var fo = new PlaceFrame(g + up * .1f, f.Fwd);
            foreach (var z in new[] { -CW / 2f, CW / 2f, -CW / 2f + 1.37f, CW / 2f - 1.37f }) mk.Box(white, fo.P(0, .012f, z), new Vector3(CL, .01f, .07f), f.r); foreach (var x in new[] { -CL / 2f, CL / 2f, -6.4f, 6.4f }) mk.Box(white, fo.P(x, .012f, 0), new Vector3(.07f, .01f, Mathf.Abs(x) > 7f ? CW : CW - 2.74f), f.r); mk.Box(white, fo.P(0, .012f, 0), new Vector3(12.8f, .01f, .07f), f.r);
            // the net
            foreach (var e in new[] { -1f, 1f }) { mk.Prism(metal, fo.P(0, 0, e * (CW / 2f + .5f)), up, .05f, 1.1f, 6); ArtistBits.PostCollider(ctx.Root, fo.P(0, 0, e * (CW / 2f + .5f)), 1.1f, .06f); }
            mk.Box(WorldMaterials.Solid(new Color(.2f, .22f, .25f)), fo.P(0, .5f, 0), new Vector3(.03f, .9f, CW + 1f), f.r); mk.Box(white, fo.P(0, .97f, 0), new Vector3(.05f, .07f, CW + 1f), f.r); Col(fo.P(0, .5f, 0), new Vector3(.1f, 1f, CW + 1f), f.r);
            // the umpire's chair: climb two steps, sit
            var uc = fo.P(0, 0, CW / 2f + 1.6f); foreach (var (x, z) in new[] { (-.4f, -.35f), (.4f, -.35f), (.4f, .35f), (-.4f, .35f) }) mk.Box(white, uc + f.r * new Vector3(x, .9f, z), new Vector3(.06f, 1.8f, .06f), f.r);
            mk.Box(white, uc + up * 1.8f, new Vector3(.9f, .06f, .8f), f.r); mk.Box(white, uc + up * 2.3f + f.Fwd * .38f, new Vector3(.9f, .9f, .05f), f.r); Col(uc + up * .9f, new Vector3(.9f, 1.8f, .8f), f.r); Interact.Seat(plife.transform, uc + up * 1.86f, -f.Fwd, "Sit in the umpire's chair");
            // two players, mid-rally for ever
            Folk(fo.P(-8.5f, 0, 1.5f), f.Right, Resident.Pose.Work, new[] { "Forty-love. It's been forty-love since lunch.", "I let him win the first set. In 1981.", "Out! ...Was it out? Call it out." }, new Residents.Look { skin = new Color(.85f, .65f, .5f), hair = new Color(.85f, .75f, .45f), top = new Color(.96f, .95f, .92f), bottom = new Color(.96f, .95f, .92f), hat = Residents.Hat.None, tool = Residents.Tool.Spatula }).workRate = 1.1f;
            Folk(fo.P(8.5f, 0, -2f), -f.Right, Resident.Pose.Work, new[] { "New balls, please. And a spritzer.", "It's all in the wrist. And the headband.", "He's never once beaten me. Don't tell him." }, new Residents.Look { skin = new Color(.6f, .42f, .32f), hair = new Color(.12f, .1f, .1f), top = new Color(.45f, .7f, .9f), bottom = new Color(.96f, .95f, .92f), hat = Residents.Hat.Cap, hatColor = new Color(.96f, .95f, .92f), tool = Residents.Tool.Spatula }).workRate = .9f;
            // the ball machine: press it and it lobs one out
            var bm = fo.P(CL / 2f + 2f, 0, -3f); var mm = new MeshKit(); mm.Box(navy, new Vector3(0, .5f, 0), new Vector3(.7f, .8f, .7f), Quaternion.identity); mm.Prism(metal, new Vector3(0, .9f, 0), new Vector3(-.6f, .5f, 0).normalized, .12f, .6f, 8); foreach (var e in new[] { -.3f, .3f }) mm.Prism(dark, new Vector3(0, .12f, e - .03f * Mathf.Sign(e)), Vector3.forward * Mathf.Sign(e), .12f, .06f, 8);
            var mgo = mm.BuildSingle("Ball machine", plife.transform, false); mgo.transform.SetPositionAndRotation(bm, f.r); Col(bm + up * .5f, new Vector3(.7f, 1f, .7f), f.r); plife.launchFrom = bm + up * 1.25f - f.Right * .7f; plife.launchDir = (-f.Right + up * .55f).normalized;
            var mt = Interact.Toy(mgo, Toy.Kind.Custom, "Press the ball machine", new Vector3(1f, 1.3f, 1f)); mgo.GetComponent<BoxCollider>().center = new Vector3(0, .6f, 0); mt.onUse = _ => plife.Launch();
            // a basket of balls to throw
            var bk = fo.P(-CL / 2f - 1.6f, 0, 3f); mk.Prism(metal, bk, up, .3f, .5f, 8, 1.2f); ArtistBits.PostCollider(ctx.Root, bk, .5f, .3f); var ballMat = WorldMaterials.Glow(new Color(.8f, .95f, .25f), .25f);
            for (var k = 0; k < 6; k++) { var tm = new MeshKit(); tm.Blob(ballMat, Vector3.zero, Vector3.one * .07f, ctx.Rng, 0f); var go = tm.BuildSingle("Tennis ball", plife.transform, false); go.transform.position = k < 3 ? bk + up * .6f + f.r * new Vector3((k - 1) * .13f, 0, 0) : fo.P(-CL / 2f - 1f + (k - 3) * .5f, .08f, 4.2f); var c = Interact.Carry(go, "a tennis ball", .2f, g.y - 8f); c.GetComponent<BoxCollider>().size = Vector3.one * .16f; plife.balls.Add(c); }
            // the little clubhouse veranda: a bench in the shade, a drinks table
            TeakBench(mk, fo.P(-5f, 0, CW / 2f + 2.4f), -f.Fwd, 2.2f); TeakBench(mk, fo.P(5f, 0, CW / 2f + 2.4f), -f.Fwd, 2.2f); CafeTable(mk, fo.P(-CL / 2f - 2.5f, 0, -2.5f), new Color(.96f, .95f, .9f));
            foreach (var (x, z) in new[] { (-1f, -1f), (1f, -1f), (1f, 1f), (-1f, 1f) }) Lamp(ctx, mk, fo.P(x * (CL / 2f + 3f), 0, z * (CW / 2f + 2.6f)));
            ArtistBits.PointLight(ctx.Root, fo.P(0, 5f, 0), new Color(1f, .9f, .75f), 26f, 1.3f);
            Placard(mk, f.P(8f, 0, CW / 2f + 7f), f.Fwd, "RIVIERA\nLAWN TENNIS CLUB", 3f, .15f);
            Lane(ctx, mk, f.P(0, 0, CW / 2f + 3.5f)); Ring(g, 18f);
            Shot(ctx, "the tennis club", f.P(-12f, 5f, CW / 2f + 12f), g + up * .5f);
        }

        // ---------------------------------------------------------------- the Winery
        void Winery(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var g = ctx.OnGround(WineryAt.x, WineryAt.y); var f = new PlaceFrame(g, FaceTo(g, Nearest(roadPts, g))); var terra = WorldMaterials.Solid(new Color(.78f, .42f, .28f));
            // the terrace under a pergola
            mk.Box(stone, g - up * .38f, new Vector3(14f, 1f, 10f), f.r); solid.Box(stone, g - up * .38f, new Vector3(14f, 1f, 10f), f.r); var fo = new PlaceFrame(g + up * .12f, f.Fwd);
            foreach (var x in new[] { -5.5f, 0f, 5.5f }) foreach (var z in new[] { -1.5f, 3.5f }) { mk.Prism(white, fo.P(x, 0, z), up, .14f, 2.9f, 8); ArtistBits.PostCollider(ctx.Root, fo.P(x, 0, z), 2.9f, .15f); }
            foreach (var z in new[] { -1.5f, 3.5f }) mk.Box(teak, fo.P(0, 2.95f, z), new Vector3(12.4f, .14f, .14f), f.r); for (var k = 0; k < 13; k++) mk.Box(teak, fo.P(-6f + k, 3.07f, 1f), new Vector3(.1f, .1f, 6.2f), f.r);
            var vine = WorldMaterials.Solid(new Color(.3f, .5f, .25f)); var grape = WorldMaterials.Solid(new Color(.45f, .2f, .5f));
            for (var k = 0; k < 16; k++) { var p = fo.P(-5.6f + (k * 7 % 12), 3.15f, -1.2f + (k * 5 % 6) * .9f); soft.Blob(vine, p, new Vector3(.7f, .16f, .6f), ctx.Rng, .25f); if (k % 2 == 0) soft.Blob(grape, p - up * .35f, new Vector3(.12f, .2f, .12f), ctx.Rng, .1f); }
            for (var k = 0; k < 9; k++) { var p = fo.P(-6f + k * 1.5f, 2.7f, 3.5f); mk.Blob(GlowM(new Color(1f, .85f, .55f), 2.2f), p, Vector3.one * .09f, ctx.Rng, 0f); if (k % 3 == 1) halos.Add(p, .7f, new Color(1f, .8f, .5f), .6f); }
            CafeTable(mk, fo.P(-3.4f, 0, 1f), new Color(.96f, .95f, .9f), 3); CafeTable(mk, fo.P(3.4f, 0, 1f), new Color(.96f, .95f, .9f), 3);
            // the cellar house behind: white walls, terracotta roof, a dark arched door, barrels
            mk.Box(white, fo.P(0, 1.7f, -5.5f), new Vector3(9f, 3.4f, 4f), f.r); solid.Box(white, fo.P(0, 1.7f, -5.5f), new Vector3(9f, 3.4f, 4f), f.r); mk.GableRoof(terra, fo.P(0, 3.4f, -5.5f), new Vector3(4.6f, 0, 9.6f), 1.5f, Quaternion.LookRotation(f.Right));
            mk.Box(dark, fo.P(0, 1.1f, -3.48f), new Vector3(1.4f, 2.2f, .04f), f.r); mk.Prism(dark, fo.P(0, 2.2f, -3.5f), f.Fwd, .7f, .04f, 10); foreach (var e in new[] { -3f, 3f }) mk.Box(GlowM(new Color(1f, .8f, .5f), 1.4f), fo.P(e, 1.9f, -3.48f), new Vector3(1.1f, 1.1f, .04f), f.r);
            for (var k = 0; k < 4; k++) { var bp = fo.P(-6.3f + (k % 2) * .0f, .5f + (k / 2) * .95f, -2.6f + (k % 2) * 1.05f - (k / 2) * .0f); if (k == 3) continue; mk.Prism(teak, bp - f.Right * .6f, f.Right, .5f, 1.2f, 10); mk.Prism(dark, bp - f.Right * .62f, f.Right, .52f, .06f, 10); mk.Prism(dark, bp + f.Right * .56f, f.Right, .52f, .06f, 10); }
            Col(fo.P(-6.3f, .9f, -2.1f), new Vector3(1.3f, 1.9f, 2.2f), f.r);
            // a barrel to tap, bunches of grapes in a crate to carry off
            var tb = fo.P(6f, 0, -2.2f); mk.Prism(teak, tb, up, .5f, 1.1f, 10); mk.Prism(dark, tb + up * .2f, up, .52f, .06f, 10); mk.Prism(dark, tb + up * .85f, up, .52f, .06f, 10); ArtistBits.PostCollider(ctx.Root, tb, 1.1f, .5f);
            var tgo = new GameObject("Wine barrel tap"); tgo.transform.SetParent(plife.transform, false); tgo.transform.position = tb + up * .8f + f.Fwd * .5f; string[] pour = { "A glass of the '79. It tastes like a key change.", "The rosé. It goes with everything, especially sunsets.", "Something red and unhurried." }; var pn = 0;
            Interact.Toy(tgo, Toy.Kind.Custom, "Tap the barrel", new Vector3(1f, 1f, .8f)).onUse = _ => Say(plife.transform, tb + up * 2f, pour[pn++ % pour.Length], 4.5f);
            mk.Box(teak, fo.P(4.2f, .2f, 3.6f), new Vector3(1f, .4f, .7f), f.r); Col(fo.P(4.2f, .2f, 3.6f), new Vector3(1f, .4f, .7f), f.r);
            for (var k = 0; k < 4; k++) { var gm = new MeshKit(); var gc = WorldMaterials.Solid(k % 2 == 0 ? new Color(.45f, .2f, .5f) : new Color(.6f, .75f, .3f)); for (var s = 0; s < 6; s++) gm.Blob(gc, new Vector3((s % 2) * .07f - .035f, -(s / 2) * .08f, (s % 3) * .04f), Vector3.one * .06f, ctx.Rng, 0f); var go = gm.BuildSingle("Grapes", plife.transform, false); go.transform.position = fo.P(3.9f + (k % 2) * .5f, .58f, 3.45f + (k / 2) * .3f); var c = Interact.Carry(go, "a bunch of grapes", .3f, g.y - 8f); c.GetComponent<BoxCollider>().size = new Vector3(.2f, .3f, .2f); }
            Folk(fo.P(4.6f, 0, -1.6f), f.Fwd, Resident.Pose.Stand, new[] { "We pick at sunset. So: constantly.", "Notes of oak, cherry, and Fender Rhodes.", "Sit. The first glass is free. The second glass is also free." }, Smoothie(Residents.Tool.Mug, true));
            Folk(fo.P(-3.4f, 0, 1f) + Quaternion.Euler(0, 40f, 0) * Vector3.forward * 1.05f + up * .5f, -(Quaternion.Euler(0, 40f, 0) * Vector3.forward), Resident.Pose.Sit, new[] { "I'm told there's a world outside the Riviera. I've seen the brochure.", "To the bay. Long may it shimmer." }, Smoothie(Residents.Tool.Mug), null, .5f);
            // four short rows of vines down the slope beside it
            for (var row = 0; row < 4; row++) for (var k = 0; k < 7; k++) { var p = G(f.P(-13f - row * 2.4f, 0, -6f + k * 2f)); mk.Box(teak, p + up * .7f, new Vector3(.07f, 1.4f, .07f), f.r); soft.Blob(vine, p + up * 1.2f, new Vector3(.4f, .45f, 1f), ctx.Rng, .25f); if (k == 0 || k == 6) ArtistBits.PostCollider(ctx.Root, p, 1.4f, .08f); }
            Ring(G(f.P(-16.5f, 0, 0)), 9f);
            ArtistBits.PointLight(ctx.Root, fo.P(0, 2.4f, 1f), new Color(1f, .8f, .55f), 15f, 1.5f);
            Placard(mk, f.P(8.5f, 0, 8f), f.Fwd, "THE WINERY\ntastings at sunset", 2.8f, .15f);
            Lane(ctx, mk, f.P(0, 0, 5.5f)); Ring(g, 11f);
            Shot(ctx, "the winery", f.P(-8f, 3f, 13f), fo.P(0, 1.6f, -1f));
        }

        // ---------------------------------------------------------------- Smooth Sound Studios
        void Studio(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var g = ctx.OnGround(StudioAt.x, StudioAt.y); var f = new PlaceFrame(g, FaceTo(g, Nearest(roadPts, g))); const float W = 11f, D = 8f, H = 3.4f, T = .3f, Door = HumanScale.DoorWidth + .2f;
            var fo = new PlaceFrame(g + up * .25f, f.Fwd); var wood = WorldMaterials.Solid(new Color(.5f, .34f, .22f)); var carpet = WorldMaterials.Solid(new Color(.7f, .4f, .25f));
            mk.Box(stone, g - up * .35f, new Vector3(W + .6f, 1.2f, D + .6f), f.r);
            void Wall(Vector3 p, Vector3 s) { mk.Box(white, p, s, f.r); solid.Box(white, p, s, f.r); }
            mk.Box(carpet, fo.P(0, -.03f, 0), new Vector3(W - T, .1f, D - T), f.r); solid.Box(carpet, fo.P(0, -.15f, 0), new Vector3(W, .3f, D), f.r);
            Wall(fo.P(0, H / 2f, -D / 2f + T / 2f), new Vector3(W, H, T)); foreach (var e in new[] { -1f, 1f }) Wall(fo.P(e * (W / 2f - T / 2f), H / 2f, 0), new Vector3(T, H, D - T * 2f));
            var seg = (W - Door) / 2f; foreach (var e in new[] { -1f, 1f }) Wall(fo.P(e * (Door / 2f + seg / 2f), H / 2f, D / 2f - T / 2f), new Vector3(seg, H, T)); Wall(fo.P(0, H - .35f, D / 2f - T / 2f), new Vector3(Door, .7f, T));
            mk.Box(wood, fo.P(0, H + .1f, 0), new Vector3(W + 1f, .2f, D + 1f), f.r); solid.Box(wood, fo.P(0, H + .1f, 0), new Vector3(W + 1f, .2f, D + 1f), f.r); mk.Box(wood, fo.P(0, 1.35f, -D / 2f + T + .03f), new Vector3(W - T * 2f - .1f, 2.3f, .05f), f.r);
            IndoorSpaces.Add(fo.P(0, H / 2f, 0), f.r, new Vector3(W - .7f, H, D - .7f));
            var pink = new Color(1f, .5f, .7f); ArtistBits.Label(ctx.Root, "SMOOTH SOUND STUDIOS", fo.P(0, H + .75f, D / 2f + .1f), f.Fwd, .34f, pink, TextAnchor.MiddleCenter); mk.Box(navy, fo.P(0, H + .75f, D / 2f), new Vector3(7f, 1f, .12f), f.r); halos.Add(fo.P(0, H + .75f, D / 2f + .4f), 2.4f, pink, .45f);
            var rec = new Material(WorldMaterials.Glow(new Color(1f, .15f, .1f), 1.6f)) { name = "Recording light" }; mk.Box(rec, fo.P(Door / 2f + .7f, 2.5f, D / 2f + .03f), new Vector3(.7f, .3f, .05f), f.r); plife.recM = rec;
            // the mixing desk along the left wall: faders to push, meters that jump
            var dk = new PlaceFrame(fo.P(-W / 2f + T + .9f, 0, -.5f), f.Right); mk.Box(wood, dk.P(0, .45f, 0), new Vector3(3.6f, .9f, 1.1f), dk.r); mk.Box(dark, dk.P(0, .95f, 0), new Vector3(3.5f, .12f, 1f), dk.r * Quaternion.Euler(-12f, 0, 0)); Col(dk.P(0, .5f, 0), new Vector3(3.6f, 1.1f, 1.1f), dk.r);
            for (var k = 0; k < 8; k++) { var mm = new Material(WorldMaterials.Glow(k < 6 ? new Color(.4f, 1f, .5f) : new Color(1f, .5f, .3f), .4f)) { name = "Meter" }; mk.Box(mm, dk.P(-1.4f + k * .4f, 1.35f, -.52f), new Vector3(.2f, .5f, .03f), dk.r); plife.meters.Add(mm); }
            mk.Box(dark, dk.P(0, 1.35f, -.56f), new Vector3(3.5f, .7f, .05f), dk.r);
            for (var k = 0; k < 4; k++)
            {
                var fm = new MeshKit(); fm.Box(k % 2 == 0 ? white : WorldMaterials.Solid(pink), Vector3.zero, new Vector3(.12f, .08f, .2f), Quaternion.identity); var fgo = fm.BuildSingle("Fader", plife.transform, false); var basePos = dk.P(-1.2f + k * .8f, 1.03f, .25f); fgo.transform.SetPositionAndRotation(basePos, dk.r);
                var idx = plife.faders.Count; plife.faders.Add((fgo.transform, basePos, -dk.Fwd * .5f, k % 2 == 0 ? 0f : 1f, k % 2 == 0 ? 0f : 1f));
                string[] chan = { "the Rhodes", "the backing vocals", "the sax", "the congas" }; var kk = k;
                Interact.Toy(fgo, Toy.Kind.Custom, "Push the fader", new Vector3(.5f, .5f, 1f)).onUse = _ => { var up2 = plife.Push(idx); Say(plife.transform, basePos + up * 1.2f, (up2 ? "More of " : "Less of ") + chan[kk] + ".", 2.5f); };
            }
            var chair = dk.P(0, 0, 1.3f); mk.Prism(metal, chair, up, .05f, .45f, 5); mk.Box(navy, chair + up * .48f, new Vector3(.5f, .08f, .5f), dk.r); mk.Box(navy, chair + up * .85f + dk.Fwd * .25f, new Vector3(.5f, .6f, .06f), dk.r); Col(chair + up * .4f, new Vector3(.5f, .9f, .5f), dk.r); Interact.Seat(plife.transform, chair + up * .54f, -dk.Fwd, "Sit at the desk");
            Folk(dk.P(1.5f, 0, 1.4f), -dk.Fwd, Resident.Pose.Stand, new[] { "Take forty-one was perfect. We're doing forty-two for the feel.", "Push a fader. You can't hurt it. It's all already too smooth.", "We've been mixing this one since the seventies. Nearly there." }, Smoothie(Residents.Tool.Mug));
            // the live room on the right: an electric piano, a saxophonist, a mic, a sofa; gold records on the back wall
            var ep = fo.P(2.4f, 0, -2f); mk.Box(dark, ep + up * .8f, new Vector3(1.5f, .14f, .5f), f.r); mk.Box(white, ep + up * .88f + f.Fwd * .1f, new Vector3(1.4f, .02f, .25f), f.r); foreach (var e in new[] { -.65f, .65f }) mk.Box(metal, ep + f.Right * e + up * .37f, new Vector3(.05f, .74f, .05f), f.r); Col(ep + up * .45f, new Vector3(1.5f, .9f, .5f), f.r);
            var st = ep + f.Fwd * .9f; mk.Prism(dark, st, up, .2f, .5f, 8); ArtistBits.PostCollider(ctx.Root, st, .5f, .2f); Interact.Seat(plife.transform, st + up * .56f, -f.Fwd, "Sit at the electric piano");
            var sx = fo.P(3.6f, 0, 1.2f); var saxLook = Smoothie(Residents.Tool.Rod); saxLook.top = new Color(.96f, .95f, .92f); Folk(sx, FaceTo(sx, fo.P(-2f, 0, 0)), Resident.Pose.Work, new[] { "One more, he says. He said that in '79.", "I only get eight bars. I make them count.", "Don't tell him, but I've been playing the same take every time." }, saxLook).workRate = .7f;
            var mc = sx + FaceTo(sx, fo.P(-2f, 0, 0)) * .8f; mk.Prism(metal, mc, up, .025f, 1.45f, 5); mk.Prism(metal, mc, up, .2f, .03f, 8); mk.Blob(dark, mc + up * 1.5f, Vector3.one * .07f, ctx.Rng, 0f);
            var sofa = fo.P(1.5f, 0, D / 2f - T - .6f); mk.Box(WorldMaterials.Solid(new Color(.85f, .6f, .35f)), sofa + up * .3f, new Vector3(2.4f, .5f, .8f), f.r); mk.Box(WorldMaterials.Solid(new Color(.85f, .6f, .35f)), sofa + up * .7f + f.Fwd * .32f, new Vector3(2.4f, .6f, .2f), f.r); Col(sofa + up * .45f, new Vector3(2.4f, .9f, .8f), f.r); Interact.Bench(plife.transform, sofa + up * .56f - f.Right * .7f, sofa + up * .56f + f.Right * .7f, -f.Fwd);
            for (var k = 0; k < 4; k++) { var rp = fo.P(.6f + k * 1.2f, 2.3f, -D / 2f + T + .08f); mk.Box(dark, rp, new Vector3(.75f, .75f, .03f), f.r); mk.Prism(WorldMaterials.Glow(new Color(1f, .8f, .3f), .5f), rp + f.Fwd * .02f, f.Fwd, .28f, .02f, 14); }
            ArtistBits.PointLight(ctx.Root, fo.P(0, 2.7f, 0), new Color(1f, .75f, .6f), 12f, 1.6f, true);
            Lamp(ctx, mk, G(f.P(-Door / 2f - 2f, 0, D / 2f + 1.6f))); TeakBench(mk, f.P(3.6f, 0, D / 2f + 1.3f), f.Fwd);
            Lane(ctx, mk, f.P(0, 0, D / 2f + 1.5f)); Ring(g, 10f);
            Shot(ctx, "smooth sound studios", f.P(-7f, 2.4f, D / 2f + 11f), fo.P(0, 2.2f, 0));
            Shot(ctx, "inside the studio", fo.P(3.8f, 1.6f, D / 2f - 1.2f), fo.P(-3f, 1.1f, -1.5f));
        }

        // ---------------------------------------------------------------- the Sunset Gazebo
        void Gazebo(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var g = ctx.OnGround(GazeboAt.x, GazeboAt.y); var toBay = FaceTo(g, new Vector3(bayC.x, 0, bayC.y)); var f = new PlaceFrame(g, toBay);
            mk.Prism(stone, g - up * .6f, up, 3.4f, .8f, 8); solid.Prism(stone, g - up * .6f, up, 3.4f, .8f, 8); var fo = g + up * .2f;
            for (var k = 0; k < 8; k++) { var d = Quaternion.Euler(0, k * 45f + 22.5f, 0) * Vector3.forward; mk.Prism(white, fo + d * 3f, up, .1f, 2.8f, 6); ArtistBits.PostCollider(ctx.Root, fo + d * 3f, 2.8f, .1f); }
            mk.Cone(WorldMaterials.Solid(new Color(.78f, .42f, .28f)), fo + up * 2.8f, 3.9f, 1.7f, 8, 22.5f); mk.Blob(GlowM(new Color(1f, .85f, .6f), 2.4f), fo + up * 2.5f, Vector3.one * .2f, ctx.Rng, 0f); halos.Add(fo + up * 2.5f, 1.2f, new Color(1f, .8f, .55f), .6f); ArtistBits.PointLight(ctx.Root, fo + up * 2.2f, new Color(1f, .8f, .55f), 10f, 1.4f);
            // a curved bench facing the bay and the sun, a brass telescope
            mk.Box(teak, fo + up * .45f - toBay * 1.6f, new Vector3(2.6f, .08f, .5f), f.r); mk.Box(teak, fo + up * .8f - toBay * 1.85f, new Vector3(2.6f, .5f, .06f), f.r); Col(fo + up * .5f - toBay * 1.7f, new Vector3(2.6f, 1f, .6f), f.r); Interact.Bench(plife.transform, fo + up * .5f - toBay * 1.6f - f.Right * .8f, fo + up * .5f - toBay * 1.6f + f.Right * .8f, toBay);
            var tp = fo + toBay * 2f + f.Right * 1.2f; mk.Prism(metal, tp, up, .04f, 1.3f, 5); var brass = WorldMaterials.Solid(new Color(.85f, .68f, .3f), .8f); mk.Prism(brass, tp + up * 1.35f - toBay * .4f, (toBay + up * .1f).normalized, .07f, .9f, 8, .7f); ArtistBits.PostCollider(ctx.Root, tp, 1.4f, .08f);
            var tgo = new GameObject("Telescope"); tgo.transform.SetParent(plife.transform, false); tgo.transform.position = tp + up * 1.3f - toBay * .4f; string[] see = { "A yacht, a long way out, going nowhere beautifully.", "The sun, a finger's width above the water. It has not moved.", "Someone on the far shore is looking back at you through a telescope." }; var sn = 0;
            Interact.Toy(tgo, Toy.Kind.Custom, "Look through the telescope", new Vector3(.8f, .8f, .8f)).onUse = _ => Say(plife.transform, tp + up * 2.2f, see[sn++ % see.Length], 5f);
            Ring(g, 5f);
            Shot(ctx, "the sunset gazebo", g - toBay * 9f + f.Right * 4f + up * 2.6f, g + up * 1.4f + toBay * 6f);
        }
    }

    /// The Riviera places' moving parts: the studio's faders and meters, the recording light, the ball machine.
    public class YachtPlacesLife : MonoBehaviour
    {
        public List<(Transform t, Vector3 basePos, Vector3 travel, float want, float at)> faders = new List<(Transform, Vector3, Vector3, float, float)>();
        public List<Material> meters = new List<Material>(); public Material recM;
        public List<Carryable> balls = new List<Carryable>(); public Vector3 launchFrom, launchDir; int next;

        public bool Push(int i) { var f = faders[i]; f.want = f.want > .5f ? 0f : 1f; faders[i] = f; return f.want > .5f; }

        /// The ball machine lobs the next tennis ball that nobody is holding.
        public void Launch()
        {
            for (var k = 0; k < balls.Count; k++)
            {
                var b = balls[(next + k) % balls.Count]; if (b == null || b.Held) continue; next = (next + k + 1) % balls.Count;
                var rb = b.GetComponent<Rigidbody>(); b.transform.position = launchFrom; if (rb != null) { rb.isKinematic = false; rb.linearVelocity = launchDir * 11f; rb.angularVelocity = Vector3.zero; }
                return;
            }
        }

        void Update()
        {
            var level = 0f;
            for (var i = 0; i < faders.Count; i++) { var f = faders[i]; f.at = Mathf.MoveTowards(f.at, f.want, Time.deltaTime * 2.5f); faders[i] = f; f.t.position = f.basePos + f.travel * f.at; level += f.at; }
            level = faders.Count > 0 ? level / faders.Count : 0f;
            for (var i = 0; i < meters.Count; i++) { var v = Mathf.PerlinNoise(Time.time * 3.1f, i * 7.3f) * (.3f + level * 1.4f); var c = i < 6 ? new Color(.4f, 1f, .5f) : new Color(1f, .5f, .3f); meters[i].SetColor("_EmissionColor", c * (.15f + v * 3f)); }
            if (recM != null) recM.SetColor("_EmissionColor", new Color(1f, .15f, .1f) * (Mathf.Sin(Time.time * 2f) > 0f ? 2.6f : .5f));
        }
    }
}
