using System.Collections.Generic;
using UnityEngine;
using static EverythingWorld.World.PlaceTools;

namespace EverythingWorld.World
{
    /// The Small Hours laid out on purpose (the build-land standard), quietly: the lanes, the homes and the exhibit
    /// stay exactly as they were, and nothing new comes near the exhibit. Four places for the people who are also
    /// awake at 3 am, each joined to the nearest lane by a short wet-stone path under a street lamp: the Night Café
    /// (lit windows, booths, a counter, a radio turned low); the Corner Shop (open all hours, a newspaper rack); the
    /// Phone Box (it rings; you can answer); the Playground (swings and a roundabout, empty at this hour, yours).
    /// The lamp benches can be sat on. (The night bus stop is Steph.fm's own song place: none is added here.)
    public partial class SmallHoursKit
    {
        static readonly Vector2 CafeAt = new Vector2(9330, 5482), ShopAt = new Vector2(9230, 5958), BoxAt = new Vector2(9182, 5380), PlayAt = new Vector2(9302, 5752);
        static readonly (Vector2 at, float r)[] PlaceRings = { (CafeAt, 11f), (ShopAt, 9f), (BoxAt, 5f), (PlayAt, 11f) };

        SmallHoursPlacesLife plife; System.Random prng; Transform rootT; LandContext ctx0;

        static readonly Color[] Coats = { new Color(.3f, .32f, .4f), new Color(.45f, .38f, .34f), new Color(.25f, .35f, .38f), new Color(.5f, .45f, .55f), new Color(.6f, .55f, .45f), new Color(.35f, .28f, .35f) };
        static readonly Color[] Trousers = { new Color(.18f, .2f, .28f), new Color(.25f, .24f, .26f), new Color(.3f, .32f, .4f) };
        static readonly Residents.Hat[] NightHats = { Residents.Hat.None, Residents.Hat.Beanie, Residents.Hat.None, Residents.Hat.Cap };
        static readonly string[] Awake =
        {
            "Can't sleep either? It's a good hour for it. Nobody wants anything from you.", "Listen. Every one of those lit windows is somebody finishing a song.", "The drizzle never gets worse. It never stops, but it never gets worse.",
            "You're up late. Or early. I stopped keeping track.", "It's quiet, but it isn't lonely. That took me a while to tell apart.",
        };

        Residents.Look NightOwl(Residents.Tool tool = Residents.Tool.None, bool apron = false) { var l = Residents.Random(prng, Coats, Trousers, NightHats, Coats); l.tool = tool; l.apron = apron; return l; }
        Resident Folk(Vector3 feet, Vector3 face, Resident.Pose pose, string[] lines = null, Residents.Look look = null, Vector3[] route = null, float seatH = HumanScale.SeatHeight)
            => Residents.Make(plife.transform, feet, face, look ?? NightOwl(), pose, prng, lines ?? Awake, route, seatH);
        void Col(Vector3 c, Vector3 size, Quaternion rot) => BoxCol(rootT, c, size, rot);
        Vector3 G(Vector3 p) => ctx0.OnGround(p.x, p.z);

        static void PlaceLevel(IslandWorldData data, int index, Vector2 c, float flat, float blend)
        {
            var level = data.HeightAt(c.x, c.y);
            for (var r = 0; r < data.Nz; r++)
            {
                var z = data.layout.depth - r * data.Step; if (Mathf.Abs(z - c.y) > blend) continue;
                for (var col = 0; col < data.Nx; col++)
                {
                    var x = col * data.Step; if (Mathf.Abs(x - c.x) > blend) continue;
                    var i = r * data.Nx + col; if (data.islandIds[i] != index) continue;
                    var d = Vector2.Distance(new Vector2(x, z), c);
                    if (d < flat) data.heights[i] = level; else if (d < blend) data.heights[i] = Mathf.Lerp(level, data.heights[i], Mathf.SmoothStep(0, 1, (d - flat) / (blend - flat)));
                }
            }
        }

        public override void ShapeGround(IslandWorldData data, int index)
        {
            ShapeExhibit(data, index);   // everything the land already did, unchanged
            foreach (var (at, r) in PlaceRings) PlaceLevel(data, index, at, r, r + 12f);
        }

        Vector3 NearestLane(Vector3 to)
        {
            var best = to; var bd = float.MaxValue;
            foreach (var cell in pathCells.Values) foreach (var p in cell) { var d = (p.x - to.x) * (p.x - to.x) + (p.z - to.z) * (p.z - to.z); if (d < bd) { bd = d; best = p; } }
            return best;
        }

        /// A short wet-stone path from the nearest lane to a place's door, a street lamp where it arrives.
        void PathTo(LandContext ctx, MeshKit mk, Vector3 to, Vector3 lampSide)
        {
            var from = NearestLane(to); var d = new Vector2(to.x - from.x, to.z - from.z); var len = d.magnitude; if (len < 3f) return; d /= len;
            var a = new Vector2(from.x, from.z) + d * 1.3f; var b = new Vector2(to.x, to.z); var bend = new Vector2(-d.y, d.x) * Mathf.Min(6f, len * .08f);
            var pts = PlaceTools.Smooth(ctx, new[] { a, Vector2.Lerp(a, b, .5f) + bend, b }, false, 2f, 1f);
            for (var i = 0; i < pts.Count; i++) { foreach (var art in ctx.Artists) { var r = art.r + 3f; var v = new Vector2(pts[i].x - art.x, pts[i].z - art.z); var dd = v.magnitude; if (dd < r) { v = dd > .01f ? v / dd * r : new Vector2(r, 0); pts[i] = ctx.OnGround(art.x + v.x, art.z + v.y); } } }
            if (pts.Count < 2) return;
            Ribbon(ctx, mk, lane, pts, 2.2f, .07f); foreach (var p in pts) Remember(p);
            for (var i = 12; i < pts.Count - 6; i += 20) { var dir = pts[i + 1] - pts[i - 1]; dir.y = 0; dir.Normalize(); var sd = Vector3.Cross(Vector3.up, dir); var lp = pts[i] + sd * 2f; StreetLamp(ctx, mk, ctx.OnGround(lp.x, lp.z), -sd); }
            var end = to + lampSide; StreetLamp(ctx, mk, ctx.OnGround(end.x, end.z), -lampSide);
        }

        void NightPlaces(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            rootT = ctx.Root; ctx0 = ctx; prng = new System.Random(303);
            plife = new GameObject("Small Hours places life").AddComponent<SmallHoursPlacesLife>(); plife.transform.SetParent(life.transform, false);
            foreach (var (at, r) in PlaceRings) ctx.AddKeepout(at.x, at.y, r);
            NightCafe(ctx, mk, solid); CornerShop(ctx, mk, solid); PhoneBox(ctx, mk, solid); Playground(ctx, mk, solid);
        }

        // ---------------------------------------------------------------- the Night Café
        void NightCafe(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var g = ctx.OnGround(CafeAt.x, CafeAt.y); var f = new PlaceFrame(g, FaceTo(g, NearestLane(g))); const float W = 11f, D = 7f, H = 3.3f, T = .3f, Door = HumanScale.DoorWidth + .2f;
            var fo = new PlaceFrame(g + up * .25f, f.Fwd); var wallM = WorldMaterials.Solid(new Color(.3f, .34f, .4f)); var floorA = WorldMaterials.Solid(new Color(.75f, .72f, .66f)); var seatM = WorldMaterials.Solid(new Color(.6f, .22f, .2f)); var warm = new Color(1f, .82f, .55f);
            mk.Box(stone, g - up * .35f, new Vector3(W + .6f, 1.2f, D + .6f), f.r);
            void Wall(Vector3 p, Vector3 s) { mk.Box(wallM, p, s, f.r); solid.Box(wallM, p, s, f.r); }
            mk.Box(floorA, fo.P(0, -.03f, 0), new Vector3(W - T, .1f, D - T), f.r); solid.Box(floorA, fo.P(0, -.15f, 0), new Vector3(W, .3f, D), f.r);
            Wall(fo.P(0, H / 2f, -D / 2f + T / 2f), new Vector3(W, H, T)); foreach (var e in new[] { -1f, 1f }) Wall(fo.P(e * (W / 2f - T / 2f), H / 2f, 0), new Vector3(T, H, D - T * 2f));
            // the front: the door at the left end, one long lit window the rest of the way (the brightest thing in the land)
            var dx = -W / 2f + 1.2f + Door / 2f; Wall(fo.P(-W / 2f + .6f, H / 2f, D / 2f - T / 2f), new Vector3(1.2f, H, T)); Wall(fo.P(dx, H - .35f, D / 2f - T / 2f), new Vector3(Door, .7f, T));
            var wx0 = dx + Door / 2f; var ww = W / 2f - wx0; Wall(fo.P(wx0 + ww / 2f, .45f, D / 2f - T / 2f), new Vector3(ww, .9f, T)); Wall(fo.P(wx0 + ww / 2f, H - .3f, D / 2f - T / 2f), new Vector3(ww, .6f, T)); Wall(fo.P(wx0 + .2f, H / 2f, D / 2f - T / 2f), new Vector3(.4f, H, T)); Wall(fo.P(W / 2f - .2f, H / 2f, D / 2f - T / 2f), new Vector3(.4f, H, T));
            var glass = new Material(WorldMaterials.Glow(warm, .5f)) { name = "Cafe window" }; mk.Box(glass, fo.P(wx0 + ww / 2f, 1.8f, D / 2f - T / 2f), new Vector3(ww - .8f, 1.8f, .05f), f.r); solid.Box(wallM, fo.P(wx0 + ww / 2f, 1.8f, D / 2f - T / 2f), new Vector3(ww - .8f, 1.8f, .1f), f.r);
            mk.Box(slate, fo.P(0, H + .1f, 0), new Vector3(W + .8f, .2f, D + .8f), f.r); solid.Box(slate, fo.P(0, H + .1f, 0), new Vector3(W + .8f, .2f, D + .8f), f.r);
            IndoorSpaces.Add(fo.P(0, H / 2f, 0), f.r, new Vector3(W - .7f, H, D - .7f));
            var signC = new Color(1f, .5f, .45f); mk.Box(board, fo.P(1f, H + .75f, D / 2f + .1f), new Vector3(5f, 1f, .12f), f.r); ArtistBits.Label(ctx.Root, "OPEN LATE", fo.P(1f, H + .75f, D / 2f + .18f), f.Fwd, .5f, signC, TextAnchor.MiddleCenter); halos.Add(fo.P(1f, H + .75f, D / 2f + .6f), 2.6f, signC, .5f);
            halos.Add(fo.P(wx0 + ww / 2f, 1.8f, D / 2f + .5f), 3.5f, warm, .35f); ArtistBits.PointLight(ctx.Root, fo.P(wx0 + ww / 2f, 1.8f, D / 2f + 2.2f), warm, 14f, 1.4f);
            // inside: two booths under the window, a counter with stools along the back, a pie under glass, a radio
            for (var k = 0; k < 2; k++)
            {
                var bc = fo.P(wx0 + 1.9f + k * 3.6f, 0, D / 2f - T - 1.1f); mk.Box(wood, bc + up * .75f, new Vector3(1.1f, .06f, 1.6f), f.r); mk.Prism(iron, bc, up, .06f, .75f, 6); Col(bc + up * .4f, new Vector3(1.1f, .8f, 1.6f), f.r);
                foreach (var e in new[] { -1f, 1f }) { var sp = bc + f.Right * e * 1.15f; mk.Box(seatM, sp + up * .45f, new Vector3(.6f, .12f, 1.6f), f.r); mk.Box(seatM, sp + up * .95f + f.Right * e * .28f, new Vector3(.1f, 1.1f, 1.6f), f.r); mk.Box(board, sp + up * .2f, new Vector3(.55f, .4f, 1.5f), f.r); Col(sp + up * .5f, new Vector3(.65f, 1f, 1.6f), f.r); if (!(k == 0 && e > 0)) Interact.Seat(plife.transform, sp + up * .54f, -f.Right * e, "Slide into the booth"); }
                mk.Blob(WorldMaterials.Glow(warm, 1.6f), bc + up * 2.3f, Vector3.one * .12f, prng, 0f); mk.Box(iron, bc + up * 2.8f, new Vector3(.02f, .9f, .02f), f.r);
            }
            Folk(fo.P(wx0 + 1.9f + 1.15f, .54f, D / 2f - T - 1.1f), -f.Right, Resident.Pose.Sit, new[] { "Night shift at the bakery. I'm either very late or very early for this pie.", "I've got a tune in my head and no instrument. Worst feeling. Best feeling.", "She keeps the radio on so low you have to mean it to hear it." }, NightOwl(Residents.Tool.Mug), null, .54f);
            mk.Box(wood, fo.P(.6f, 1.02f, -D / 2f + 1.7f), new Vector3(6.6f, .08f, .7f), f.r); mk.Box(wallM, fo.P(.6f, .5f, -D / 2f + 1.7f), new Vector3(6.4f, 1f, .55f), f.r); Col(fo.P(.6f, .55f, -D / 2f + 1.7f), new Vector3(6.6f, 1.1f, .7f), f.r);
            for (var k = 0; k < 4; k++) { var sp = fo.P(-1.8f + k * 1.6f, 0, -D / 2f + 2.6f); mk.Prism(iron, sp, up, .05f, .7f, 6); mk.Prism(seatM, sp + up * .7f, up, .22f, .08f, 10); ArtistBits.PostCollider(ctx.Root, sp, .75f, .18f); Interact.Seat(plife.transform, sp + up * .8f, -f.Fwd, "Sit at the counter"); }
            mk.Prism(WorldMaterials.Solid(new Color(.8f, .6f, .35f)), fo.P(2.4f, 1.07f, -D / 2f + 1.7f), up, .28f, .1f, 12); mk.Prism(WorldMaterials.Glow(new Color(.9f, .95f, 1f), .3f), fo.P(2.4f, 1.07f, -D / 2f + 1.7f), up, .34f, .3f, 12, .8f);
            mk.Box(WorldMaterials.Solid(new Color(.75f, .75f, .8f), .8f), fo.P(-1.6f, 1.36f, -D / 2f + T + .4f), new Vector3(.7f, .6f, .5f), f.r); mk.Blob(WorldMaterials.Glow(new Color(1f, .4f, .3f), 2f), fo.P(-1.6f, 1.5f, -D / 2f + T + .67f), Vector3.one * .04f, prng, 0f); mk.Box(wood, fo.P(.6f, 1.02f, -D / 2f + T + .4f), new Vector3(6.6f, .06f, .5f), f.r);
            for (var k = 0; k < 3; k++) { var mm = new MeshKit(); mm.Prism(WorldMaterials.Solid(k == 1 ? new Color(.6f, .7f, .8f) : new Color(.92f, .9f, .85f)), Vector3.zero, Vector3.up, .06f, .11f, 8); mm.Box(WorldMaterials.Solid(new Color(.92f, .9f, .85f)), new Vector3(.08f, .055f, 0), new Vector3(.04f, .07f, .02f), Quaternion.identity); var go = mm.BuildSingle("Mug", plife.transform, false); go.transform.position = fo.P(-.4f + k * .9f, 1.07f, -D / 2f + 1.75f); var c = Interact.Carry(go, "a mug of tea", .3f, g.y - 10f); var bc2 = c.GetComponent<BoxCollider>(); if (bc2 != null) { bc2.size = new Vector3(.18f, .14f, .14f); bc2.center = new Vector3(0, .06f, 0); } }
            var rd = new MeshKit(); rd.Box(WorldMaterials.Solid(new Color(.5f, .35f, .28f)), new Vector3(0, .11f, 0), new Vector3(.36f, .22f, .14f), Quaternion.identity); rd.Box(WorldMaterials.Glow(warm, 1.2f), new Vector3(-.07f, .12f, .071f), new Vector3(.14f, .1f, .005f), Quaternion.identity);
            var rgo = rd.BuildSingle("Cafe radio", plife.transform, false); rgo.transform.SetPositionAndRotation(fo.P(3.4f, 1.06f, -D / 2f + T + .4f), f.r); string[] radio = { "Very quietly: a voice and one guitar, recorded in a bedroom.", "A request, read out: \"for whoever's still up.\"", "The song ends. The presenter lets the silence run a while before speaking." }; var rn = 0;
            Interact.Toy(rgo, Toy.Kind.Custom, "Turn the radio up a little", new Vector3(.6f, .5f, .5f)).onUse = _ => Say(plife.transform, fo.P(3.4f, 2f, -D / 2f + 1.2f), radio[rn++ % radio.Length], 5.5f);
            Folk(fo.P(.6f, 0, -D / 2f + .95f), f.Fwd, Resident.Pose.Work, new[] { "Tea's in the pot. Pie's under the glass. Take your time; I do.", "They come in at three with a chorus and no second verse. I've heard some good second verses in here.", "We don't close. Somebody's always nearly finished something." }, NightOwl(Residents.Tool.Mug, true)).workRate = .4f;
            ArtistBits.PointLight(ctx.Root, fo.P(0, 2.7f, 0), warm, 13f, 2.2f, true);
            PathTo(ctx, mk, f.P(dx, 0, D / 2f + 1.2f), -f.Right * 2.2f);
            Shot(ctx, "the night cafe", f.P(-6f, 2.4f, D / 2f + 11f), fo.P(1f, 2f, 0));
            Shot(ctx, "inside the night cafe", fo.P(-W / 2f + 1.6f, 1.6f, D / 2f - 1.2f), fo.P(2.5f, 1.1f, -1.2f));
        }

        // ---------------------------------------------------------------- the Corner Shop
        void CornerShop(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var g = ctx.OnGround(ShopAt.x, ShopAt.y); var f = new PlaceFrame(g, FaceTo(g, NearestLane(g))); var wallM = WorldMaterials.Solid(new Color(.42f, .36f, .36f)); var cool = new Color(.75f, .9f, 1f);
            mk.Box(stone, g - up * .38f, new Vector3(9f, 1f, 8f), f.r); solid.Box(stone, g - up * .38f, new Vector3(9f, 1f, 8f), f.r); var fo = new PlaceFrame(g + up * .12f, f.Fwd);
            // an open-fronted shop: three walls, a striped awning, everything inside lit cold and bright
            mk.Box(wallM, fo.P(0, 1.6f, -2.6f), new Vector3(7f, 3.2f, .3f), f.r); solid.Box(wallM, fo.P(0, 1.6f, -2.6f), new Vector3(7f, 3.2f, .3f), f.r); foreach (var e in new[] { -1f, 1f }) { mk.Box(wallM, fo.P(e * 3.35f, 1.6f, -.9f), new Vector3(.3f, 3.2f, 3.1f), f.r); solid.Box(wallM, fo.P(e * 3.35f, 1.6f, -.9f), new Vector3(.3f, 3.2f, 3.1f), f.r); }
            mk.Box(slate, fo.P(0, 3.3f, -1f), new Vector3(7.4f, .2f, 3.8f), f.r); for (var s = 0; s < 7; s++) mk.Box(WorldMaterials.Solid(s % 2 == 0 ? new Color(.25f, .4f, .35f) : new Color(.8f, .78f, .72f), .3f), fo.P(-3f + s * 1f, 2.95f, 1.5f), new Vector3(1f, .06f, 1.8f), f.r * Quaternion.Euler(14f, 0, 0));
            mk.Box(board, fo.P(0, 3.75f, .7f), new Vector3(4.4f, .7f, .1f), f.r); ArtistBits.Label(ctx.Root, "24 HR · NEWS · MILK", fo.P(0, 3.75f, .77f), f.Fwd, .26f, cool, TextAnchor.MiddleCenter); halos.Add(fo.P(0, 3.75f, 1.1f), 2f, cool, .4f);
            Color[] goods = { new Color(.95f, .5f, .45f), new Color(.5f, .8f, .95f), new Color(.95f, .85f, .45f), new Color(.6f, .9f, .6f), new Color(.85f, .6f, .9f) };
            for (var row = 0; row < 3; row++) { mk.Box(wood, fo.P(0, .7f + row * .75f, -2.25f), new Vector3(6.2f, .05f, .45f), f.r); for (var k = 0; k < 12; k++) mk.Box(WorldMaterials.Glow(goods[(k + row * 2) % goods.Length], .35f), fo.P(-2.85f + k * .52f, .9f + row * .75f, -2.25f), new Vector3(.36f, .34f, .3f), f.r); }
            mk.Box(wood, fo.P(1.4f, .5f, -.6f), new Vector3(3f, 1f, .7f), f.r); Col(fo.P(1.4f, .5f, -.6f), new Vector3(3f, 1f, .7f), f.r); mk.Box(WorldMaterials.Solid(new Color(.3f, .3f, .34f), .6f), fo.P(2.3f, 1.2f, -.6f), new Vector3(.5f, .4f, .45f), f.r);
            // the newspaper rack out front: read tomorrow's headline
            var nr = fo.P(-2.4f, 0, 1.2f); mk.Box(iron, nr + up * .6f, new Vector3(1.2f, 1.2f, .08f), f.r * Quaternion.Euler(-12f, 0, 0)); for (var k = 0; k < 3; k++) mk.Box(WorldMaterials.Solid(new Color(.86f, .84f, .78f)), nr + up * (.3f + k * .35f) + f.Fwd * (.1f - k * .07f), new Vector3(1f, .28f, .04f), f.r * Quaternion.Euler(-12f, 0, 0)); Col(nr + up * .6f, new Vector3(1.2f, 1.2f, .4f), f.r);
            var ngo = new GameObject("Newspaper rack"); ngo.transform.SetParent(plife.transform, false); ngo.transform.position = nr + up * .9f + f.Fwd * .3f; string[] news = { "TOMORROW'S PAPER: \"Local Musician Finishes Song; Starts Another\"", "\"Drizzle To Continue, Says Everyone\"", "\"Lost: one chorus. If found, hum it at the cafe.\"", "The crossword is already done, in pencil, wrongly." }; var nn = 0;
            Interact.Toy(ngo, Toy.Kind.Custom, "Read the headline", new Vector3(1.2f, 1.2f, .8f)).onUse = _ => Say(plife.transform, nr + up * 2f, news[nn++ % news.Length], 5.5f);
            // things to carry off: a pint of milk, crisps, a pack of batteries
            for (var k = 0; k < 4; k++) { var cm = new MeshKit(); string thing; if (k == 0) { cm.Box(WorldMaterials.Solid(new Color(.95f, .95f, .97f)), new Vector3(0, .11f, 0), new Vector3(.1f, .22f, .1f), Quaternion.identity); cm.Box(WorldMaterials.Solid(new Color(.3f, .45f, .8f)), new Vector3(0, .24f, 0), new Vector3(.05f, .04f, .05f), Quaternion.identity); thing = "a pint of milk"; } else if (k == 3) { cm.Box(WorldMaterials.Solid(new Color(.2f, .2f, .22f)), new Vector3(0, .05f, 0), new Vector3(.14f, .1f, .05f), Quaternion.identity); cm.Box(WorldMaterials.Solid(new Color(.85f, .7f, .3f)), new Vector3(0, .1f, 0), new Vector3(.14f, .02f, .05f), Quaternion.identity); thing = "batteries (for the four-track)"; } else { cm.Box(WorldMaterials.Solid(goods[k]), new Vector3(0, .12f, 0), new Vector3(.2f, .24f, .06f), Quaternion.Euler(0, 0, 4f)); thing = "a bag of crisps"; } var go = cm.BuildSingle("Shop thing", plife.transform, false); go.transform.SetPositionAndRotation(fo.P(.4f + k * .5f, 1.03f, -.6f), f.r); var c = Interact.Carry(go, thing, .3f, g.y - 10f); var bc = c.GetComponent<BoxCollider>(); if (bc != null) { bc.size = new Vector3(.22f, .26f, .14f); bc.center = new Vector3(0, .12f, 0); } }
            Folk(fo.P(1.4f, 0, -1.5f), f.Fwd, Resident.Pose.Stand, new[] { "Milk, bread, strings. I sell more guitar strings than milk.", "You're the fourth tonight. Busy for a Tuesday. Is it Tuesday?", "Take the paper. It's tomorrow's, so it's not much use yet." }, NightOwl(Residents.Tool.None, true));
            ArtistBits.PointLight(ctx.Root, fo.P(0, 2.6f, -.6f), cool, 14f, 2.4f); halos.Add(fo.P(0, 1.6f, .6f), 3.4f, cool, .3f);
            PathTo(ctx, mk, f.P(0, 0, 4.6f), f.Right * 3.6f);
            Shot(ctx, "the corner shop", f.P(-5f, 2.2f, 10f), fo.P(0, 1.8f, -1f));
        }

        // ---------------------------------------------------------------- the Phone Box
        void PhoneBox(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var g = ctx.OnGround(BoxAt.x, BoxAt.y); var f = new PlaceFrame(g, FaceTo(g, NearestLane(g))); var red = WorldMaterials.Solid(new Color(.6f, .12f, .12f), .4f); var lit = new Material(WorldMaterials.Glow(new Color(1f, .9f, .7f), .9f)) { name = "Phone box light" };
            mk.Box(stone, g + up * .04f, new Vector3(1.5f, .1f, 1.5f), f.r);
            foreach (var (x, z) in new[] { (-1f, -1f), (1f, -1f), (1f, 1f), (-1f, 1f) }) mk.Box(red, g + f.r * new Vector3(x * .5f, 1.25f, z * .5f), new Vector3(.1f, 2.4f, .1f), f.r);
            mk.Box(red, g + up * 2.5f, new Vector3(1.2f, .25f, 1.2f), f.r); mk.Box(lit, g + up * 2.33f, new Vector3(1f, .1f, 1f), f.r); mk.Box(lit, g + f.r * new Vector3(0, 1.3f, -.5f), new Vector3(.9f, 2.1f, .03f), f.r); foreach (var e in new[] { -1f, 1f }) mk.Box(lit, g + f.r * new Vector3(e * .5f, 1.3f, 0), new Vector3(.03f, 2.1f, .9f), f.r);
            solid.Box(red, g + f.r * new Vector3(0, 1.3f, -.5f), new Vector3(1.1f, 2.6f, .1f), f.r); foreach (var e in new[] { -1f, 1f }) solid.Box(red, g + f.r * new Vector3(e * .5f, 1.3f, 0), new Vector3(.1f, 2.6f, 1.1f), f.r);
            mk.Box(board, g + f.r * new Vector3(0, 1.3f, -.38f), new Vector3(.4f, .6f, .16f), f.r); mk.Box(board, g + f.r * new Vector3(-.12f, 1.45f, -.28f), new Vector3(.07f, .3f, .06f), f.r);
            plife.phoneM = lit; plife.phoneLight = ArtistBits.PointLight(ctx.Root, g + up * 2f + f.Fwd * .6f, new Color(1f, .9f, .7f), 9f, 1.4f); halos.Add(g + up * 1.5f, 1.8f, new Color(1f, .9f, .7f), .5f); plife.phoneAt = g;
            var pgo = new GameObject("Ringing phone"); pgo.transform.SetParent(plife.transform, false); pgo.transform.position = g + up * 1.3f + f.Fwd * .2f;
            string[] calls = { "A voice, far away: \"...is that you? I just wanted to play you something.\" And then a song down the line.", "Nobody speaks. Someone is playing a piano in another room. You listen until the pips go.", "\"Wrong number, sorry. ...Are you up as well? Good. Goodnight.\"", "The dialling tone, steady as a held note." }; var cn = 0;
            Interact.Toy(pgo, Toy.Kind.Custom, "Answer the phone", new Vector3(1f, 1.6f, 1f)).onUse = _ => { plife.answered = Time.time; Say(plife.transform, g + up * 3.1f, calls[cn++ % calls.Length], 7f); };
            var bp = G(f.P(2.6f, 0, .6f)); LampBenchSeat(ctx, mk, solid, bp, -f.Right);
            PathTo(ctx, mk, f.P(0, 0, 2f), -f.Right * 1.8f);
            Shot(ctx, "the phone box", f.P(-3.5f, 2f, 6.5f), g + up * 1.4f);
        }

        /// A plain bench you can sit on (no lamp of its own).
        void LampBenchSeat(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 p, Vector3 face)
        {
            var rot = Quaternion.LookRotation(face); var side = Vector3.Cross(Vector3.up, face); var up = Vector3.up;
            mk.Box(wood, p + up * .45f, new Vector3(1.8f, .08f, .5f), rot); mk.Box(wood, p + up * .8f - face * .22f, new Vector3(1.8f, .35f, .06f), rot); foreach (var s in new[] { -1f, 1f }) mk.Box(iron, p + side * s * .75f + up * .22f, new Vector3(.06f, .45f, .45f), rot);
            solid.Box(wood, p + up * .5f, new Vector3(1.8f, 1f, .6f), rot); Interact.Bench(plife.transform, p + up * .5f - side * .5f, p + up * .5f + side * .5f, face);
        }

        // ---------------------------------------------------------------- the Playground
        void Playground(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var g = ctx.OnGround(PlayAt.x, PlayAt.y); var f = new PlaceFrame(g, FaceTo(g, NearestLane(g))); var paintA = WorldMaterials.Solid(new Color(.55f, .3f, .3f)); var paintB = WorldMaterials.Solid(new Color(.3f, .42f, .55f)); var bark2 = WorldMaterials.Solid(new Color(.3f, .26f, .24f));
            mk.Prism(bark2, g - up * .05f, up, 9.5f, .12f, 16);
            // the swings: an A-frame, two seats that swing when pushed; you can sit on them
            var sc = G(f.P(-4f, 0, -2f)); foreach (var e in new[] { -1f, 1f }) { foreach (var z in new[] { -1f, 1f }) { var foot = sc + f.Right * e * 2f + f.Fwd * z * 1f; var top = sc + f.Right * e * 1.9f + up * 2.7f; mk.Prism(paintB, foot, (top - foot).normalized, .06f, Vector3.Distance(foot, top), 6); } ArtistBits.PostCollider(ctx.Root, sc + f.Right * e * 2f, 2.7f, .25f); }
            mk.Prism(paintB, sc + up * 2.7f - f.Right * 2f, f.Right, .06f, 4f, 6);
            for (var k = 0; k < 2; k++)
            {
                var sm = new MeshKit(); foreach (var e in new[] { -.25f, .25f }) sm.Box(iron, new Vector3(e, -1.05f, 0), new Vector3(.02f, 2.1f, .02f), Quaternion.identity); sm.Box(board, new Vector3(0, -2.1f, 0), new Vector3(.6f, .05f, .24f), Quaternion.identity);
                var sgo = sm.BuildSingle("Swing", plife.transform, false); sgo.transform.SetPositionAndRotation(sc + up * 2.68f + f.Right * (-.9f + k * 1.8f), f.r); var sw = sgo.AddComponent<Sway>(); sw.axis = Vector3.right; sw.degrees = 28f; sw.speed = 2.6f; sw.decay = .28f; sw.seconds = 14f;
                var t = Interact.Toy(sgo, Toy.Kind.Custom, "Push the swing", new Vector3(.9f, .5f, .9f)); sgo.GetComponent<BoxCollider>().center = new Vector3(0, -1.5f, 0); t.onUse = _ => sw.Push();
                var seat = Interact.Seat(sgo.transform, sgo.transform.position - up * 2.02f, f.Fwd, "Sit on the swing"); seat.GetComponent<Collider>().transform.localPosition = new Vector3(0, -2.02f, 0);
            }
            // the roundabout: spin it, or sit on it while someone else does
            var rc = G(f.P(3.5f, 0, -1.5f)); mk.Prism(iron, rc, up, .1f, .5f, 8); var rm = new MeshKit(); rm.Prism(paintA, new Vector3(0, 0, 0), Vector3.up, 1.5f, .1f, 12); for (var k = 0; k < 4; k++) { var d = Quaternion.Euler(0, k * 90f, 0) * Vector3.forward; rm.Box(paintB, d * 1.3f + Vector3.up * .5f, new Vector3(.05f, .9f, .05f), Quaternion.identity); rm.Box(paintB, d * .65f + Vector3.up * .92f, new Vector3(.05f, .05f, 1.3f), Quaternion.LookRotation(d)); }
            rm.Prism(paintB, Vector3.zero, Vector3.up, .08f, .95f, 6);
            var rgo = rm.BuildSingle("Roundabout", plife.transform, false); rgo.transform.position = rc + up * .45f; var rt = Interact.Toy(rgo, Toy.Kind.Spin, "Spin the roundabout", new Vector3(3.2f, 1.1f, 3.2f)); rgo.GetComponent<BoxCollider>().center = new Vector3(0, .55f, 0); rt.axis = Vector3.up; rt.amount = 1080f; rt.seconds = 9f;
            for (var k = 0; k < 4; k++) { var d = Quaternion.Euler(0, k * 90f + 45f, 0) * Vector3.forward; Interact.Seat(rgo.transform, rgo.transform.position + d * .95f + up * .16f, d, "Sit on the roundabout"); }
            // a slide and a see-saw, a bin, a bench under the one lamp
            var sl = G(f.P(0, 0, -6f)); foreach (var e in new[] { -.35f, .35f }) { mk.Box(paintB, sl + f.Right * (-1.6f) + f.Fwd * e + up * 1f, new Vector3(.06f, 2f, .06f), f.r); mk.Box(paintB, sl + f.Right * -2.1f + f.Fwd * e + up * 1f, new Vector3(.06f, 2.1f, .06f), f.r * Quaternion.Euler(0, 0, -14f)); }
            for (var k = 0; k < 5; k++) mk.Box(iron, sl + f.Right * (-2.35f + k * .12f) + up * (.3f + k * .34f), new Vector3(.06f, .04f, .7f), f.r); mk.Box(paintA, sl + f.Right * -1.6f + up * 1.95f, new Vector3(.8f, .06f, .8f), f.r); mk.Box(WorldMaterials.Solid(new Color(.6f, .62f, .68f), .8f), sl + f.Right * .2f + up * 1f, new Vector3(4f, .05f, .7f), f.r * Quaternion.Euler(0, 0, -28f)); Col(sl + f.Right * -.6f + up * 1f, new Vector3(4.6f, 2f, .8f), f.r);
            var ss = G(f.P(-1f, 0, 4f)); mk.Prism(iron, ss, up, .12f, .55f, 6); mk.Box(paintA, ss + up * .6f, new Vector3(3.6f, .07f, .3f), f.r * Quaternion.Euler(0, 0, 10f)); Col(ss + up * .5f, new Vector3(3.6f, .9f, .35f), f.r);
            var bp = G(f.P(6.5f, 0, 3.5f)); LampBench(ctx, mk, solid, bp, FaceTo(bp, g));
            Folk(bp + up * .5f + Vector3.Cross(up, FaceTo(bp, g)) * .45f, FaceTo(bp, g), Resident.Pose.Sit, new[] { "I come and sit when a song won't finish. The swings help. Don't ask me how.", "Go on the roundabout. There's nobody to see. That's what three in the morning is for.", "I learned to whistle on that slide. First tune I ever made up." }, NightOwl(), null, .5f);
            StreetLamp(ctx, mk, G(f.P(-7.5f, 0, -4.5f)), f.Right); ArtistBits.PointLight(ctx.Root, g + up * 4f, Sodium, 20f, .9f);
            PathTo(ctx, mk, f.P(0, 0, 9.5f), f.Right * 2.4f);
            Shot(ctx, "the playground", f.P(-7f, 3f, 12f), g + up * 1f);
        }
    }

    /// The Small Hours places' moving part: the phone box, which rings (its light pulsing) until you answer.
    public class SmallHoursPlacesLife : MonoBehaviour
    {
        public Material phoneM; public Light phoneLight; public Vector3 phoneAt; public float answered = -999f;
        static readonly Color Lit = new Color(1f, .9f, .7f);

        void Update()
        {
            if (phoneM == null) return;
            var t = Time.time; var quiet = t - answered < 45f;                    // after you answer it stays quiet for a while, then rings again
            var ring = !quiet && Mathf.Repeat(t, 6f) < 2.2f ? .5f + .5f * Mathf.Sin(t * 28f) : 0f;
            phoneM.SetColor("_EmissionColor", Lit * (.9f + ring * 1.6f)); if (phoneLight != null) phoneLight.intensity = 1.4f + ring * 1.4f;
        }
    }
}
