using System.Collections.Generic;
using UnityEngine;

namespace EverythingWorld.World
{
    /// The Docklands' artists, each a corner of wet concrete off the streets, its covers pasted on a hoarding under a
    /// little strip light, a plaque and a sodium lamp. Every song has its own light (DocksSongs).
    /// - The Smiths: an old brick warehouse turned into a hall, its doors open on a small stage strewn with flowers under
    ///   a warm wash, a big sign across the front lit for "How Soon Is Now?"; outside, an old car with its headlights on
    ///   and its door open ("There Is a Light That Never Goes Out"), a wrought-iron cemetery gate with two lanterns
    ///   ("Pretty Girls Make Graves"), a bench alone under a lamp ("I Know It's Over"), a bedsit window with a lamp left on
    ///   ("Asleep"), and a bus stop sign ("Half a Person").
    /// - The Cure: a huge spider's web strung between two lamp posts, glowing ("Lullaby"), a wall of photographs pinned up
    ///   in rows, lit ("Pictures of You"), and a short jetty that ends at the black water under one lamp ("The End Of The
    ///   World").
    /// - Sonic Youth: a little skyline of towers whose windows light ("The Sprawl"), a stack of amps humming with a TV on
    ///   top ("Kool Thing"), a grate breathing pink steam ("Pink Steam").
    /// - Joy Division: a radio telescope dish on a concrete base, listening to the sky, its signal light pulsing
    ///   ("Disorder").
    /// - Maruja: a big brass saxophone, a lightning rod mast ("Thunder"), a burning drum ("Rage"), a flag on a tall pole
    ///   ("Saoirse"). Black Country, New Road: giant sunglasses on a billboard ("Sunglasses") and a little toy space
    ///   marine on a plinth ("Chaos Space Marine").
    /// - The rest: Madness a terraced house front with every window lit ("Our House") and a flight of steps going nowhere
    ///   ("One Step Beyond"); Melt-Banana a giant wrapped sweet; Nena a bunch of red balloons; a-ha a sketched frame;
    ///   Tears For Fears a globe; Talking Heads a pair of eyes on a sign; The Police a red lamp in a window; The Stranglers a
    ///   golden lamp; The Go-Go's a beach chair and umbrella on the concrete; Murray Head a chessboard; Future Islands a
    ///   little sailboat; Fontaines D.C. a scooter; Geese a subway entrance; The Smile a lit zero; Protomartyr a tall
    ///   antenna; Racing Mount Pleasant a post box; Haindling a melting pot; BLUESTEM an eye chart; Dexys a jukebox.
    public partial class DocksKit
    {
        class Plot
        {
            public LandContext ctx; public Transform root, labels; public MeshKit mk, solid, pictures;
            public IslandWorldData.Artist a; public Vector3 c, front, side; public List<Renderer> glows; public ArtistArea area;
            public Vector3 At(float fw, float rt) { var p = c + front * fw + side * rt; return ctx.OnGround(p.x, p.z); }
            public Quaternion Rot => Quaternion.LookRotation(front);
        }

        public override void BuildArtist(LandContext ctx, IslandWorldData.Artist a)
        {
            if (concrete == null) MakeMaterials();
            var root = new GameObject("Artist: " + a.name).transform; root.SetParent(ctx.Root, false);
            var front = facing.TryGetValue(a.name, out var f) ? f : new Vector3(center.x - a.x, 0, center.z - a.z).normalized;
            if (front.sqrMagnitude < .01f) front = Vector3.forward;
            var labels = new GameObject("Labels").transform; labels.SetParent(root, false);
            var area = root.gameObject.AddComponent<ArtistArea>();
            area.artistName = a.name; area.stationId = a.stationId; area.radius = a.r; area.glowColor = Sodium;
            var z = new Plot { ctx = ctx, root = root, labels = labels, mk = new MeshKit(), solid = new MeshKit(), pictures = new MeshKit(), a = a, c = ctx.OnGround(a.x, a.z), front = front, side = Vector3.Cross(Vector3.up, front), glows = new List<Renderer>(), area = area };
            z.mk.Prism(concrete, z.c - Vector3.up * .28f, Vector3.up, Foot(a) * .6f, .34f, 18);
            for (var k = 0; k < 3; k++) { var pa = k * 2.1f + a.name.Length; Puddle(z.mk, z.c + new Vector3(Mathf.Cos(pa), 0, Mathf.Sin(pa)) * Foot(a) * .35f + Vector3.up * .16f, .7f + k * .2f); }
            switch (a.name)
            {
                case "The Smiths": Smiths(z); break;
                case "The Cure": Cure(z); break;
                case "Sonic Youth": SonicYouth(z); break;
                case "Joy Division": JoyDivision(z); break;
                case "Maruja": Maruja(z); break;
                case "Black Country, New Road": BCNR(z); break;
                case "Madness": Madness(z); break;
                default: Small(z); break;
            }
            var reach = Foot(a);
            Hoarding(z, z.At(reach * .8f, -reach * .5f - 2.5f), front);
            ArtistBits.Plaque(z.mk, z.solid, labels, a, z.At(reach * .8f + 1.5f, reach * .5f + 2.5f), front, darkConcrete, concrete, Ink, 1.6f);
            SodiumLamp(ctx, z.mk, z.At(reach * .8f, reach * .5f + 5f), -z.side);
            if (z.area.glowLight == null) z.area.glowLight = ArtistBits.PointLight(root, z.c + Vector3.up * 5f + front * 5f, Sodium, HasStation(a) ? 20f : 13f, 1.4f);
            area.glowRenderers = z.glows.ToArray();
            z.mk.Build("Area", root, false);
            z.solid.BuildCollider("Walls", root);
            z.pictures.Build("Covers", root, false);
            noGrass.Add(new Vector3(z.c.x, reach * .9f, z.c.z));
            if (HasStation(a) || reach >= 11f)
            {
                var mark = new GameObject("Shot: " + a.name + " up close").transform; mark.SetParent(ctx.Root, false);
                var cam = z.At(reach * .55f, reach * .35f) + Vector3.up * 1.7f; var look = z.At(-reach * .35f, 0) + Vector3.up * 2.5f - cam; mark.SetPositionAndRotation(cam, Quaternion.LookRotation(look.normalized));
            }
        }

        /// The covers pasted on a plywood hoarding, flyers round them, a strip light on top.
        void Hoarding(Plot z, Vector3 ground, Vector3 dir)
        {
            dir = Vector3.ProjectOnPlane(dir, Vector3.up).normalized; var rot = Quaternion.LookRotation(dir); var side = Vector3.Cross(Vector3.up, dir);
            var n = Mathf.Max(1, Mathf.Min(6, z.a.songList?.Length ?? 1)); const float size = .9f; var w = n * size * 1.25f + 1.2f;
            var ply = WorldMaterials.Solid(new Color(.3f, .27f, .22f));
            var c = ground + Vector3.up * 1.7f;
            z.mk.Box(ply, c, new Vector3(w, 2.4f, .12f), rot); z.solid.Box(ply, c, new Vector3(w, 2.4f, .25f), rot);
            foreach (var s in new[] { -1f, 1f }) z.mk.Box(darkConcrete, ground + side * s * (w / 2f - .2f) + Vector3.up * .25f, new Vector3(.5f, .5f, .5f), rot);
            Flyers(z.mk, c + Vector3.up * .3f, dir, w, 1.6f, z.ctx.Rng);
            ArtistBits.CoverWall(z.pictures, z.labels, z.ctx, z.a, c + dir * .09f, dir, n, size, n, true, Ink);
            var strip = c + Vector3.up * 1.32f + dir * .3f; z.mk.Box(dark, strip, new Vector3(w - .6f, .12f, .3f), rot); z.mk.Box(GlowM(new Color(.85f, .92f, 1f), 2.2f), strip - Vector3.up * .07f, new Vector3(w - .8f, .03f, .2f), rot);
            halos.Add(strip + dir * .4f - Vector3.up * .3f, w * .35f, new Color(.85f, .92f, 1f), .25f);
        }

        // ---------------------------------------------------------------- The Smiths
        void Smiths(Plot z)
        {
            var up = Vector3.up; var rot = z.Rot; var ctx = z.ctx;
            // the hall: a brick warehouse, its big doors open on a little stage under a warm wash
            var h = z.At(-11f, 0); const float W = 16f, D = 11f, H = 7f, T = .4f, DoorW = 6f, DoorH = 4.6f;
            Vector3 P(float x, float y, float d) => h + z.side * x + up * y + z.front * d;
            void Wall(Vector3 at, Vector3 size) { z.mk.Box(brick, at, size, rot); z.solid.Box(brick, at, size, rot); }
            z.mk.Box(darkConcrete, P(0, .1f, 0), new Vector3(W, .2f, D), rot); z.solid.Box(darkConcrete, P(0, .1f, 0), new Vector3(W, .2f, D), rot);
            Wall(P(0, H / 2f, -D / 2f + T / 2f), new Vector3(W, H, T));
            foreach (var s in new[] { -1f, 1f }) Wall(P(s * (W / 2f - T / 2f), H / 2f, 0), new Vector3(T, H, D - 2f * T));
            var segW = (W - DoorW) / 2f; foreach (var s in new[] { -1f, 1f }) Wall(P(s * (DoorW / 2f + segW / 2f), H / 2f, D / 2f - T / 2f), new Vector3(segW, H, T));
            Wall(P(0, DoorH + (H - DoorH) / 2f, D / 2f - T / 2f), new Vector3(DoorW, H - DoorH, T));
            z.mk.GableRoof(darkConcrete, P(0, H, 0), new Vector3(D + .8f, 0, W + .8f), 2.6f, Quaternion.LookRotation(z.side));
            for (var k = 0; k < 6; k++) foreach (var s in new[] { -1f, 1f }) z.mk.Box(GlowM(Window, 1.1f), P(s * (W / 2f + .01f), 4.6f, -D / 2f + 1.5f + k * 1.6f), new Vector3(.03f, 1.6f, .9f), rot);
            // the sign across the front
            var sign = SongGlow(HowSoonIsNow, new Color(1f, .3f, .25f), 2.2f);
            z.mk.Box(dark, P(0, DoorH + 1.2f, D / 2f + .1f), new Vector3(W - 3f, 1.1f, .15f), rot);
            ArtistBits.Label(z.labels, "THE HALL", P(0, DoorH + 1.2f, D / 2f + .2f), z.front, .5f, new Color(1f, .4f, .32f), TextAnchor.MiddleCenter);
            z.mk.Box(sign, P(0, DoorH + .6f, D / 2f + .2f), new Vector3(W - 3f, .08f, .05f), rot); z.mk.Box(sign, P(0, DoorH + 1.8f, D / 2f + .2f), new Vector3(W - 3f, .08f, .05f), rot);
            halos.Add(P(0, DoorH + 1.2f, D / 2f + .8f), 4f, new Color(1f, .35f, .3f), .3f);
            // the stage inside: a low platform, flowers strewn across its front, two amps, a mic, a warm wash
            z.mk.Box(dark, P(0, .55f, -D / 2f + 2f), new Vector3(8f, .7f, 3f), rot); z.solid.Box(dark, P(0, .55f, -D / 2f + 2f), new Vector3(8f, .7f, 3f), rot);
            var stem = WorldMaterials.Solid(new Color(.25f, .45f, .22f)); var petals = new[] { new Color(1f, .45f, .55f), new Color(1f, .85f, .3f), new Color(.95f, .95f, .9f), new Color(.85f, .25f, .3f) };
            for (var k = 0; k < 14; k++) { var fp = P(-3.6f + k * .55f, .92f, -D / 2f + 3.3f - (k % 3) * .25f); var dirF = Quaternion.AngleAxis(-60f + (k % 5) * 25f, up) * z.side; z.mk.Box(stem, fp, new Vector3(.03f, .03f, 1f), Quaternion.LookRotation(dirF)); for (var j = 0; j < 4; j++) z.mk.Blob(WorldMaterials.Solid(petals[k % petals.Length]), fp + dirF * (.2f + j * .12f) + up * .04f, Vector3.one * .07f, ctx.Rng, 0f); }
            foreach (var s in new[] { -1f, 1f }) { var amp = P(s * 3f, 1.6f, -D / 2f + 1.2f); z.mk.Box(dark, amp, new Vector3(1.2f, 1.4f, .7f), rot); z.mk.Box(WorldMaterials.Solid(new Color(.2f, .19f, .17f)), amp + z.front * .36f, new Vector3(1f, 1.1f, .02f), rot); }
            z.mk.Prism(steel, P(0, .9f, -D / 2f + 2.6f), up, .025f, 1.6f, 6);
            var curtain = SongGlow(IKnowItsOver, new Color(.55f, .08f, .1f), .5f);
            for (var k = 0; k < 18; k++) z.mk.Box(curtain, P(-4.5f + k * .53f, 3.4f, -D / 2f + T + .08f + (k % 2) * .12f), new Vector3(.55f, 5.2f, .06f), rot * Quaternion.Euler(0, (k % 2 == 0 ? 18f : -18f), 0));
            var spot = GlowM(new Color(1f, .85f, .6f), 2.4f); foreach (var s in new[] { -2.5f, 0f, 2.5f }) { var sp = P(s, H - .8f, -D / 2f + 4.5f); z.mk.Box(dark, sp, new Vector3(.4f, .4f, .5f), rot * Quaternion.Euler(30f, 0, 0)); z.mk.Prism(spot, sp - z.front * .2f - up * .15f, (-z.front - up).normalized, .14f, .05f, 10); }
            ArtistBits.PointLight(z.root, P(0, 4.5f, -D / 2f + 4f), new Color(1f, .7f, .45f), 13f, 2f, true);
            IndoorSpaces.Add(P(0, H / 2f, 0), rot, new Vector3(W - 2f * T, H, D - 2f * T));
            for (var k = 0; k < 18; k++) { var t = k / 17f; z.mk.Blob(GlowM(Window, 2.4f), P(Mathf.Lerp(-W / 2f + 1f, W / 2f - 1f, t), H - .9f - Mathf.Sin(t * Mathf.PI) * .6f, 0), Vector3.one * .09f, ctx.Rng, 0f); }
            // There Is a Light That Never Goes Out: an old car, door open, headlights on, facing out into the night
            var car = z.At(-1f, -11f); var dir = (z.front + z.side * -.3f).normalized; var crot = Quaternion.LookRotation(dir); var csd = Vector3.Cross(up, dir); var paint = WorldMaterials.Solid(new Color(.12f, .18f, .28f), .7f);
            z.mk.Box(paint, car + up * .65f, new Vector3(1.8f, .65f, 4.4f), crot); z.mk.Box(paint, car + up * 1.2f - dir * .3f, new Vector3(1.6f, .55f, 2.2f), crot); z.mk.Box(glassDark, car + up * 1.2f - dir * .3f, new Vector3(1.62f, .4f, 2.1f), crot);
            foreach (var s in new[] { -1f, 1f }) foreach (var w in new[] { -1.4f, 1.4f }) z.mk.Prism(dark, car + csd * s * .9f + dir * w + up * .35f, csd * s, .35f, .2f, 10);
            var light = SongGlow(LightNeverGoesOut, new Color(1f, .95f, .78f), 3f);
            foreach (var s in new[] { -.6f, .6f }) { z.mk.Prism(light, car + up * .75f + dir * 2.2f + csd * s, dir, .16f, .05f, 10); halos.Add(car + up * .75f + dir * 2.6f + csd * s, 1.2f, new Color(1f, .95f, .78f), .7f); }
            z.mk.Box(GlowM(Window, 1.2f), car + up * 1.05f, new Vector3(1.4f, .05f, 1.6f), crot);
            ArtistBits.PointLight(z.root, car + up * .9f + dir * 5f, new Color(1f, .95f, .8f), 12f, 1.6f);
            z.solid.Box(paint, car + up * .8f, new Vector3(1.9f, 1.6f, 4.5f), crot);
            // Pretty Girls Make Graves: a wrought-iron cemetery gate between brick piers, a lantern on each
            var gate = z.At(-3f, 11.5f); var iron = WorldMaterials.Solid(new Color(.08f, .08f, .09f), .6f);
            foreach (var s in new[] { -1f, 1f }) { var pier = gate + z.side * s * 2.3f; z.mk.Box(brick, pier + up * 1.5f, new Vector3(.8f, 3f, .8f), rot); z.solid.Box(brick, pier + up * 1.5f, new Vector3(.8f, 3f, .8f), rot); var ln = pier + up * 3.3f; z.mk.Box(iron, ln, new Vector3(.35f, .5f, .35f), rot); z.mk.Box(SongGlow(PrettyGirls, new Color(1f, .75f, .4f), 2.4f), ln, new Vector3(.25f, .38f, .25f), rot); halos.Add(ln, 1.3f, new Color(1f, .75f, .4f), .6f); }
            for (var k = 0; k < 13; k++) { var x = (k - 6f) * .3f; var hh = 2.4f + Mathf.Cos(x * .8f) * .5f; z.mk.Box(iron, gate + z.side * x + up * hh / 2f, new Vector3(.04f, hh, .04f), rot); z.mk.Cone(iron, gate + z.side * x + up * hh, .06f, .2f, 4); }
            foreach (var y in new[] { .5f, 1.8f }) z.mk.Box(iron, gate + up * y, new Vector3(3.8f, .06f, .06f), rot);
            z.solid.Box(iron, gate + up * 1.4f, new Vector3(3.8f, 2.8f, .2f), rot);
            // I Know It's Over: a bench alone under a lamp
            var bn = z.At(4f, -14f); var brot = Quaternion.LookRotation(-z.side);
            z.mk.Box(WorldMaterials.Solid(new Color(.3f, .25f, .2f)), bn + up * .45f, new Vector3(1.8f, .08f, .45f), brot); z.mk.Box(WorldMaterials.Solid(new Color(.3f, .25f, .2f)), bn + up * .8f + z.side * .22f, new Vector3(1.8f, .4f, .05f), brot);
            foreach (var s in new[] { -.8f, .8f }) z.mk.Box(iron, bn + up * .22f + z.front * s, new Vector3(.4f, .45f, .06f), brot);
            z.solid.Box(iron, bn + up * .5f, new Vector3(1.9f, 1f, .6f), brot);
            var bl = bn + z.side * 1f + z.front * 1.2f; z.mk.Prism(iron, bl, up, .06f, 3f, 6); z.mk.Box(SongGlow(IKnowItsOver, new Color(1f, .7f, .4f), 2.6f), bl + up * 3.1f, new Vector3(.3f, .35f, .3f), brot); halos.Add(bl + up * 3f, 1.8f, new Color(1f, .7f, .4f), .7f); ArtistBits.PostCollider(z.root, bl, 3f, .1f);
            // Asleep: a bedsit window on the hall's side wall, a lamp left on behind the curtain
            var bw = P(W / 2f + .03f, 2.2f, 2.5f); var sideRot = Quaternion.LookRotation(z.side);
            z.mk.Box(SongGlow(Asleep, new Color(1f, .8f, .55f), 1.6f), bw, new Vector3(1.4f, 1.2f, .03f), sideRot); z.mk.Box(WorldMaterials.Solid(new Color(.55f, .3f, .3f)), bw + z.side * .02f + z.front * .45f, new Vector3(.45f, 1.2f, .02f), sideRot);
            z.mk.Box(concrete, bw - up * .7f + z.side * .12f, new Vector3(1.6f, .1f, .3f), sideRot); halos.Add(bw + z.side * .5f, 1.4f, new Color(1f, .8f, .55f), .5f);
            // Half a Person: a bus stop sign at the kerb
            var bus = z.At(7f, 9f); z.mk.Prism(steel, bus, up, .05f, 2.8f, 6); z.mk.Box(SongGlow(HalfAPerson, new Color(.85f, .9f, 1f), 1.4f), bus + up * 2.7f, new Vector3(.6f, .6f, .04f), rot);
            ArtistBits.Label(z.labels, "BUS", bus + up * 2.7f + z.front * .03f, z.front, .16f, new Color(.1f, .1f, .12f), TextAnchor.MiddleCenter); ArtistBits.PostCollider(z.root, bus, 2.8f, .1f);
            noGrass.Add(new Vector3(h.x, 11f, h.z));
            var mark = new GameObject("Shot: the Smiths' hall inside").transform; mark.SetParent(ctx.Root, false);
            var cam = P(1.5f, 1.7f, D / 2f - 1.2f); var look = P(0, 2f, -D / 2f + 1.6f) - cam; mark.SetPositionAndRotation(cam, Quaternion.LookRotation(look.normalized));
        }

        // ---------------------------------------------------------------- The Cure
        void Cure(Plot z)
        {
            var up = Vector3.up; var rot = z.Rot; var ctx = z.ctx;
            // Lullaby: a huge web strung between two lamp posts
            var wc = z.At(-7f, -4f) + up * 4.5f; var web = SongGlow(Lullaby, new Color(.8f, .85f, 1f), 1.8f);
            foreach (var s in new[] { -1f, 1f }) { var lp = z.At(-7f, -4f + s * 5f); SodiumLamp(ctx, z.mk, lp, -z.side * s); }
            var spokes = 12; var rings = 6;
            for (var k = 0; k < spokes; k++) { var a = k * Mathf.PI * 2f / spokes; var tip = wc + (z.side * Mathf.Cos(a) + up * Mathf.Sin(a)) * 3.6f; z.mk.Box(web, (wc + tip) / 2f, new Vector3(.025f, .025f, 3.6f), Quaternion.LookRotation(tip - wc, z.front)); }
            for (var r = 1; r <= rings; r++) for (var k = 0; k < spokes; k++)
            {
                float a0 = k * Mathf.PI * 2f / spokes, a1 = (k + 1) * Mathf.PI * 2f / spokes, rr = r * .58f;
                var p0 = wc + (z.side * Mathf.Cos(a0) + up * Mathf.Sin(a0)) * rr; var p1 = wc + (z.side * Mathf.Cos(a1) + up * Mathf.Sin(a1)) * rr;
                var sag = up * -.06f; z.mk.Box(web, (p0 + p1) / 2f + sag, new Vector3(.02f, .02f, Vector3.Distance(p0, p1)), Quaternion.LookRotation(p1 - p0, z.front));
            }
            foreach (var s in new[] { -1f, 1f }) { var anchor = z.At(-7f, -4f + s * 5f) + up * 6.2f; var edge = wc + z.side * s * 3.6f; z.mk.Box(web, (anchor + edge) / 2f, new Vector3(.02f, .02f, Vector3.Distance(anchor, edge)), Quaternion.LookRotation(anchor - edge)); }
            var spider = wc + up * 1.4f + z.side * .9f; z.mk.Blob(dark, spider, new Vector3(.3f, .25f, .3f), ctx.Rng, 0f); for (var k = 0; k < 8; k++) z.mk.Box(dark, spider + Quaternion.AngleAxis(k * 45f, z.front) * z.side * .35f, new Vector3(.03f, .03f, .5f), Quaternion.LookRotation(Quaternion.AngleAxis(k * 45f, z.front) * z.side));
            halos.Add(wc, 4f, new Color(.8f, .85f, 1f), .2f);
            // Pictures of You: a brick wall pinned with rows of photographs, lit
            var pw = z.At(-6f, 8f); var wrot = Quaternion.LookRotation(z.front);
            z.mk.Box(brick, pw + up * 1.6f, new Vector3(6f, 3.2f, .5f), wrot); z.solid.Box(brick, pw + up * 1.6f, new Vector3(6f, 3.2f, .5f), wrot);
            var photo = SongGlow(PicturesOfYou, new Color(1f, .93f, .82f), 1.2f); var rng = ctx.Rng;
            for (var r = 0; r < 4; r++) for (var k = 0; k < 8; k++) { var pp = pw + up * (.8f + r * .6f) + z.side * ((k - 3.5f) * .66f) + z.front * (.26f + (r * 8 + k) * .001f); z.mk.Box(photo, pp, new Vector3(.42f, .5f, .01f), wrot * Quaternion.Euler(0, 0, ((float)rng.NextDouble() - .5f) * 14f)); z.mk.Box(dark, pp + z.front * .01f, new Vector3(.32f, .3f, .005f), wrot * Quaternion.Euler(0, 0, ((float)rng.NextDouble() - .5f) * 14f)); }
            halos.Add(pw + up * 1.7f + z.front * .9f, 3.4f, new Color(1f, .93f, .82f), .2f);
            ArtistBits.PointLight(z.root, pw + up * 2.4f + z.front * 3f, new Color(1f, .9f, .8f), 9f, 1.2f);
            // The End Of The World: a short jetty out over a black pool, one lamp at its end
            var pool = z.At(-1f, -12.5f); var water = WorldMaterials.Solid(new Color(.02f, .03f, .05f), .98f);
            z.mk.Prism(water, pool - up * .2f, up, 3.4f, .32f, 20); z.mk.Prism(darkConcrete, pool - up * .3f, up, 3.7f, .38f, 20);
            var plank = WorldMaterials.Solid(new Color(.28f, .24f, .2f));
            for (var k = 0; k < 9; k++) z.mk.Box(plank, pool + up * .35f + z.front * (2.8f - k * .55f), new Vector3(1.2f, .08f, .45f), rot);
            var endL = pool + up * .35f - z.front * 1.8f; z.mk.Prism(steel, endL + z.side * .55f, up, .05f, 2.2f, 6); z.mk.Blob(SongGlow(EndOfTheWorld, new Color(.75f, .85f, 1f), 2.6f), endL + z.side * .55f + up * 2.3f, Vector3.one * .15f, ctx.Rng, 0f);
            halos.Add(endL + z.side * .55f + up * 2.2f, 1.4f, new Color(.75f, .85f, 1f), .6f); halos.Add(pool + up * .05f, 3f, new Color(.6f, .7f, 1f), .12f);
            z.solid.Prism(water, pool, up, 3.6f, 1f, 12);
        }

        // ---------------------------------------------------------------- Sonic Youth
        void SonicYouth(Plot z)
        {
            var up = Vector3.up; var rot = z.Rot; var ctx = z.ctx;
            // The Sprawl: a little skyline of towers, their windows lighting with the song
            var sk = z.At(-9f, 0); var lit = SongGlow(TheSprawl, Window, .9f);
            var heights = new[] { 5f, 8.5f, 6f, 11f, 4.5f, 7.5f };
            for (var k = 0; k < heights.Length; k++)
            {
                var p = sk + z.side * ((k - 2.5f) * 2.3f) - z.front * ((k % 2) * 1.5f); var hgt = heights[k];
                z.mk.Box(darkConcrete, p + up * hgt / 2f, new Vector3(1.9f, hgt, 1.9f), rot);
                for (var f = 0; f < Mathf.FloorToInt(hgt / 1.1f); f++) for (var b = 0; b < 2; b++) z.mk.Box((f + b + k) % 3 == 0 ? lit : windowsOff, p + up * (.7f + f * 1.1f) + z.front * .96f + z.side * (b - .5f) * .8f, new Vector3(.5f, .55f, .02f), rot);
            }
            z.solid.Box(darkConcrete, sk + up * 4f - z.front * .7f, new Vector3(14.5f, 8f, 3.6f), rot);
            halos.Add(sk + up * 5f + z.front * 2f, 7f, Window, .12f);
            // Kool Thing: a stack of amps with an old TV on top, its screen flickering
            var amps = z.At(-1.5f, -8f); var arot = Quaternion.LookRotation((z.front + z.side * .4f).normalized);
            for (var k = 0; k < 3; k++) { z.mk.Box(dark, amps + up * (.6f + k * 1.15f), new Vector3(1.6f, 1.1f, .8f), arot); z.mk.Box(WorldMaterials.Solid(new Color(.2f, .2f, .2f)), amps + up * (.6f + k * 1.15f) + arot * Vector3.forward * .41f, new Vector3(1.4f, .9f, .02f), arot); }
            var tv = amps + up * 4f; z.mk.Box(WorldMaterials.Solid(new Color(.25f, .2f, .15f)), tv, new Vector3(1f, .8f, .8f), arot);
            z.mk.Box(SongGlow(KoolThing, new Color(.6f, .9f, 1f), 2.2f), tv + arot * Vector3.forward * .41f, new Vector3(.75f, .55f, .02f), arot); halos.Add(tv + arot * Vector3.forward * .8f, 1.4f, new Color(.6f, .9f, 1f), .6f);
            z.solid.Box(dark, amps + up * 2f, new Vector3(1.7f, 4.4f, .9f), arot);
            // Pink Steam: a grate breathing pink steam
            var pg = z.At(1f, 7f); var grot = rot;
            z.mk.Box(SongGlow(PinkSteam, new Color(1f, .45f, .7f), 1.6f), pg + up * .07f, new Vector3(1.6f, .06f, 1.1f), grot); for (var k = 0; k < 6; k++) z.mk.Box(steel, pg + up * .14f + z.side * ((k - 2.5f) * .24f), new Vector3(.06f, .04f, 1.05f), grot);
            life.pinkSteam = pg + up * .2f;
        }

        // ---------------------------------------------------------------- Joy Division
        void JoyDivision(Plot z)
        {
            var up = Vector3.up; var rot = z.Rot; var ctx = z.ctx;
            var b = z.At(-4f, 0); z.mk.Box(concrete, b + up * 1.2f, new Vector3(3f, 2.4f, 3f), rot); z.solid.Box(concrete, b + up * 1.2f, new Vector3(3f, 2.4f, 3f), rot);
            z.mk.Box(steel, b + up * 3.2f, new Vector3(.6f, 1.6f, .6f), rot);
            var aim = (up * .8f - z.front * .6f).normalized; var dc = b + up * 4.4f;
            var white = WorldMaterials.Solid(new Color(.8f, .8f, .78f), .3f);
            z.mk.Prism(white, dc, aim, 3.6f, 1f, 24, .2f); z.mk.Prism(darkConcrete, dc + aim * .05f, aim, 3.4f, .05f, 24);
            foreach (var k in new[] { 0f, 120f, 240f }) { var leg = Quaternion.AngleAxis(k, aim) * Vector3.Cross(aim, z.side).normalized * 3f; z.mk.Box(steel, dc + aim * 1.4f + leg * .5f, new Vector3(.06f, .06f, Vector3.Distance(dc + leg, dc + aim * 2.8f)), Quaternion.LookRotation(dc + aim * 2.8f - (dc + leg))); }
            var sig = SongGlow(Disorder, new Color(.85f, .9f, 1f), 3f); z.mk.Blob(sig, dc + aim * 2.9f, Vector3.one * .25f, ctx.Rng, 0f); halos.Add(dc + aim * 2.9f, 2f, new Color(.85f, .9f, 1f), .7f);
            life.dish = dc + aim * 2.9f;
            // a ring of low white lights on the ground round the base, like a pulse spreading out
            for (var k = 0; k < 16; k++) { var a = k * Mathf.PI / 8f; var p = ctx.OnGround(b.x + Mathf.Cos(a) * 4.5f, b.z + Mathf.Sin(a) * 4.5f); z.mk.Box(sig, p + up * .06f, new Vector3(.4f, .05f, .12f), Quaternion.Euler(0, -a * Mathf.Rad2Deg, 0)); }
        }

        // ---------------------------------------------------------------- Maruja and BCNR
        void Maruja(Plot z)
        {
            var up = Vector3.up; var rot = z.Rot; var ctx = z.ctx; var brass = WorldMaterials.Solid(new Color(.75f, .55f, .2f), .85f);
            // a big brass saxophone standing on a concrete plinth
            var s = z.At(-6f, 0); z.mk.Box(concrete, s + up * .5f, new Vector3(2.4f, 1f, 2.4f), rot); z.solid.Box(concrete, s + up * 1.5f, new Vector3(2.4f, 3f, 2.4f), rot);
            var body = new List<Vector3> { s + up * 6.5f, s + up * 4.5f - z.side * .1f, s + up * 2.2f, s + up * 1.6f + z.side * .6f, s + up * 2.2f + z.side * 1.2f, s + up * 3f + z.side * 1.3f };
            for (var k = 0; k + 1 < body.Count; k++) { var r0 = .18f + k * .07f; z.mk.Prism(brass, body[k], body[k + 1] - body[k], r0, Vector3.Distance(body[k], body[k + 1]), 10, (r0 + .07f) / r0); }
            z.mk.Prism(SongGlow(Saoirse, new Color(1f, .8f, .4f), 2.4f), body[5], up, .65f, .05f, 14); halos.Add(body[5] + up * .3f, 1.6f, new Color(1f, .8f, .4f), .6f);
            for (var k = 0; k < 6; k++) z.mk.Prism(brass, s + up * (2.6f + k * .55f) + z.front * .2f, z.front, .1f, .08f, 8);
            z.mk.Box(brass, s + up * 6.8f - z.side * .3f, new Vector3(.08f, .08f, .9f), Quaternion.LookRotation(-z.side + up));
            ArtistBits.PointLight(z.root, s + up * 4f + z.front * 3f, new Color(1f, .75f, .4f), 10f, 1.4f);
            // Thunder: a lightning rod mast with a glass ball
            var m = z.At(-1f, -9f); z.mk.Prism(steel, m, up, .08f, 9f, 6); z.mk.Blob(SongGlow(Thunder, new Color(.7f, .8f, 1f), 2.6f), m + up * 6f, Vector3.one * .3f, ctx.Rng, 0f); halos.Add(m + up * 6f, 1.6f, new Color(.7f, .8f, 1f), .5f); ArtistBits.PostCollider(z.root, m, 9f, .15f);
            life.rod = m + up * 9f;
            // Rage: a burning drum
            Drum(ctx, z.mk, z.At(1f, 6f), true); z.mk.Blob(SongGlow(Rage, new Color(1f, .3f, .1f), 2.6f), z.At(1f, 6f) + up * 1.25f, new Vector3(.32f, .2f, .32f), ctx.Rng, .3f);
            // Saoirse: a flag on a tall pole
            var fp = z.At(-8f, 7f); z.mk.Prism(steel, fp, up, .07f, 8f, 6); ArtistBits.PostCollider(z.root, fp, 8f, .1f);
            var flag = SongGlow(Saoirse, new Color(.3f, .8f, .5f), 1.2f); for (var k = 0; k < 6; k++) z.mk.Box(flag, fp + up * 7.3f + z.side * (.3f + k * .35f) - up * Mathf.Sin(k * .8f) * .1f, new Vector3(.36f, 1f, .03f), Quaternion.LookRotation(z.front) * Quaternion.Euler(0, Mathf.Sin(k * 1.3f) * 15f, 0));
        }

        void BCNR(Plot z)
        {
            var up = Vector3.up; var rot = z.Rot; var ctx = z.ctx;
            // Sunglasses: a billboard with a giant pair of sunglasses on it
            var bb = z.At(-5f, 0); foreach (var s in new[] { -1f, 1f }) { z.mk.Box(steel, bb + z.side * s * 3f + up * 2.5f, new Vector3(.25f, 5f, .25f), rot); ArtistBits.PostCollider(z.root, bb + z.side * s * 3f, 5f, .2f); }
            z.mk.Box(WorldMaterials.Solid(new Color(.85f, .8f, .68f)), bb + up * 5.6f, new Vector3(7.5f, 3.4f, .2f), rot);
            var lens = SongGlow(Sunglasses, new Color(.4f, .55f, 1f), 1.4f); var frame = WorldMaterials.Solid(new Color(.05f, .05f, .05f), .8f);
            foreach (var s in new[] { -1f, 1f }) { z.mk.Prism(frame, bb + up * 5.6f + z.side * s * 1.6f + z.front * .1f, z.front, 1.25f, .08f, 18); z.mk.Prism(lens, bb + up * 5.6f + z.side * s * 1.6f + z.front * .15f, z.front, 1.1f, .05f, 18); }
            z.mk.Box(frame, bb + up * 5.9f + z.front * .15f, new Vector3(.9f, .15f, .08f), rot);
            foreach (var s in new[] { -1f, 1f }) { var l = bb + up * 7.6f + z.side * s * 2f + z.front * .7f; z.mk.Box(sodium, l, new Vector3(.4f, .1f, .3f), rot); halos.Add(l, 1.4f, Sodium, .5f); }
            // Chaos Space Marine: a little toy space marine on a plinth, its visor lit
            var tm = z.At(0f, 8f); z.mk.Box(concrete, tm + up * .5f, new Vector3(1.4f, 1f, 1.4f), rot); z.solid.Box(concrete, tm + up * .8f, new Vector3(1.4f, 1.6f, 1.4f), rot);
            var armour = WorldMaterials.Solid(new Color(.25f, .3f, .55f), .6f); var t = tm + up * 1f;
            foreach (var s in new[] { -.2f, .2f }) z.mk.Box(armour, t + z.side * s + up * .35f, new Vector3(.25f, .7f, .3f), rot);
            z.mk.Box(armour, t + up * 1f, new Vector3(.75f, .6f, .45f), rot); foreach (var s in new[] { -1f, 1f }) z.mk.Blob(armour, t + up * 1.25f + z.side * s * .5f, new Vector3(.3f, .25f, .3f), ctx.Rng, 0f);
            z.mk.Blob(armour, t + up * 1.55f, Vector3.one * .2f, ctx.Rng, 0f); z.mk.Box(SongGlow(ChaosSpaceMarine, new Color(1f, .2f, .15f), 3f), t + up * 1.57f + z.front * .19f, new Vector3(.25f, .06f, .02f), rot);
        }

        void Madness(Plot z)
        {
            var up = Vector3.up; var rot = z.Rot; var ctx = z.ctx;
            // Our House: a terraced house front, every window lit, a red door
            var h = z.At(-4f, 0); z.mk.Box(brick, h + up * 3f, new Vector3(6f, 6f, 4f), rot); z.solid.Box(brick, h + up * 3f, new Vector3(6f, 6f, 4f), rot);
            z.mk.GableRoof(darkConcrete, h + up * 6f, new Vector3(4.5f, 0, 6.3f), 2f, Quaternion.LookRotation(z.side)); z.mk.Box(darkBrick, h + up * 7.6f + z.side * 2f, new Vector3(.8f, 1.6f, .8f), rot);
            var lit = SongGlow(OurHouse, Window, 1.8f);
            foreach (var y in new[] { 1.6f, 4.4f }) foreach (var s in new[] { -1.6f, 1.6f }) { if (y < 2f && s < 0) continue; z.mk.Box(lit, h + up * y + z.front * 2.01f + z.side * s, new Vector3(1.4f, 1.2f, .02f), rot); }
            z.mk.Box(lit, h + up * 4.4f + z.front * 2.01f, new Vector3(.8f, 1f, .02f), rot);
            z.mk.Box(WorldMaterials.Solid(new Color(.6f, .1f, .1f)), h + up * 1.1f + z.front * 2.02f - z.side * 1.6f, new Vector3(1f, 2.1f, .03f), rot);
            halos.Add(h + up * 3f + z.front * 3f, 3.6f, Window, .25f);
            // One Step Beyond: a flight of steps going up to nothing
            var st = z.At(1f, 6f); for (var k = 0; k < 6; k++) z.mk.Box(concrete, st + up * (k * .3f + .15f) - z.front * (k * .35f), new Vector3(1.4f, .3f + k * .3f, .35f), rot);
            z.mk.Box(SongGlow(OneStepBeyond, new Color(1f, .85f, .5f), 2f), st + up * 1.86f - z.front * 1.75f, new Vector3(1.4f, .02f, .35f), rot);
            z.solid.Box(concrete, st + up * .9f - z.front * .9f, new Vector3(1.4f, 1.8f, 2.2f), rot);
        }


        // ---------------------------------------------------------------- the smaller corners
        void Small(Plot z)
        {
            var up = Vector3.up; var rot = z.Rot; var ctx = z.ctx; var c = z.At(-3f, 0);
            Color glow = Window;
            switch (z.a.name)
            {
                case "Melt-Banana":
                {
                    var w = SongGlow(CandyGun, new Color(1f, .4f, .8f), 1.4f); z.mk.Blob(w, c + up * 1.4f, new Vector3(1.4f, .9f, .9f), ctx.Rng, .05f);
                    foreach (var s in new[] { -1f, 1f }) z.mk.Cone(SongGlow(ShieldForYourEyes, new Color(.4f, 1f, .9f), 1.4f), c + up * 1.4f + z.side * s * 1.3f, .7f, .8f * s, 6);
                    z.solid.Box(dark, c + up * 1.4f, new Vector3(4f, 2f, 2f), rot); glow = new Color(1f, .4f, .8f); break;
                }
                case "Nena":
                {
                    var red = SongGlow(RedBalloons, new Color(1f, .15f, .15f), 1.6f);
                    for (var k = 0; k < 9; k++) { var bp = c + up * (3.2f + (k % 3) * .7f) + z.side * ((k % 3) - 1f) * .6f + z.front * ((k / 3) - 1f) * .5f; z.mk.Blob(red, bp, new Vector3(.35f, .42f, .35f), ctx.Rng, 0f); z.mk.Box(dark, (bp + c + up * 1f) / 2f, new Vector3(.01f, Vector3.Distance(bp, c + up * 1f), .01f), Quaternion.FromToRotation(Vector3.up, bp - (c + up * 1f))); }
                    z.mk.Box(concrete, c + up * .5f, new Vector3(.6f, 1f, .6f), rot); ArtistBits.PostCollider(z.root, c, 1f, .4f); life.balloonsFrom = c + up * 4f; glow = new Color(1f, .2f, .2f); break;
                }
                case "a-ha":
                {
                    var frame = WorldMaterials.Solid(new Color(.9f, .9f, .88f)); var line = SongGlow(TakeOnMe, new Color(.95f, .95f, 1f), 2f);
                    foreach (var y in new[] { .3f, 3.7f }) z.mk.Box(frame, c + up * y, new Vector3(3f, .15f, .15f), rot); foreach (var s in new[] { -1.5f, 1.5f }) z.mk.Box(frame, c + up * 2f + z.side * s, new Vector3(.15f, 3.5f, .15f), rot);
                    var pts = new[] { new Vector2(-1f, .9f), new Vector2(-.4f, 2.8f), new Vector2(.1f, 1.4f), new Vector2(.6f, 3.1f), new Vector2(1.1f, 1f) };
                    for (var k = 0; k + 1 < pts.Length; k++) { var a = c + z.side * pts[k].x + up * pts[k].y; var b = c + z.side * pts[k + 1].x + up * pts[k + 1].y; z.mk.Box(line, (a + b) / 2f, new Vector3(.05f, .05f, Vector3.Distance(a, b)), Quaternion.LookRotation(b - a)); }
                    z.solid.Box(frame, c + up * 2f, new Vector3(3.2f, 4f, .3f), rot); glow = Color.white; break;
                }
                case "Tears For Fears":
                {
                    z.mk.Box(concrete, c + up * .6f, new Vector3(1.4f, 1.2f, 1.4f), rot); var gl = SongGlow(RuleTheWorld, new Color(.45f, .75f, 1f), 1.2f);
                    z.mk.Blob(gl, c + up * 2.5f, Vector3.one * 1.2f, ctx.Rng, 0f); z.mk.Box(steel, c + up * 2.5f, new Vector3(.06f, 2.8f, .06f), Quaternion.Euler(0, 0, 23f)); z.solid.Box(concrete, c + up * 1.8f, new Vector3(2.4f, 3.6f, 2.4f), rot); glow = new Color(.45f, .75f, 1f); break;
                }
                case "Talking Heads":
                {
                    z.mk.Prism(steel, c, up, .1f, 4f, 6); z.mk.Box(dark, c + up * 4.3f, new Vector3(3f, 1.4f, .15f), rot); ArtistBits.PostCollider(z.root, c, 4f, .15f);
                    var eye = SongGlow(Crosseyed, new Color(1f, .95f, .85f), 2.2f);
                    foreach (var s in new[] { -1f, 1f }) { z.mk.Prism(eye, c + up * 4.3f + z.side * s * .7f + z.front * .08f, z.front, .45f, .04f, 14); z.mk.Prism(dark, c + up * 4.3f + z.side * (s * .7f + s * -.2f) + z.front * .12f, z.front, .18f, .03f, 10); }
                    break;
                }
                case "The Police":
                {
                    z.mk.Box(brick, c + up * 1.8f, new Vector3(3f, 3.6f, 2f), rot); z.solid.Box(brick, c + up * 1.8f, new Vector3(3f, 3.6f, 2f), rot);
                    z.mk.Box(SongGlow(Roxanne, new Color(1f, .1f, .08f), 2.4f), c + up * 2.2f + z.front * 1.01f, new Vector3(1.2f, 1.2f, .02f), rot); halos.Add(c + up * 2.2f + z.front * 1.6f, 1.8f, new Color(1f, .1f, .08f), .6f); glow = new Color(1f, .15f, .1f); break;
                }
                case "The Stranglers":
                {
                    z.mk.Prism(darkConcrete, c, up, .7f, .5f, 10); z.mk.Prism(WorldMaterials.Solid(new Color(.6f, .45f, .2f), .8f), c + up * .5f, up, .1f, 2f, 8);
                    z.mk.Cone(SongGlow(GoldenBrown, new Color(1f, .7f, .25f), 2.2f), c + up * 2.4f, .7f, .7f, 12); halos.Add(c + up * 2.3f, 1.8f, new Color(1f, .7f, .25f), .6f); ArtistBits.PostCollider(z.root, c, 2.6f, .7f); glow = new Color(1f, .7f, .25f); break;
                }
                case "The Go-Go's":
                {
                    var stripe = WorldMaterials.Solid(new Color(.95f, .5f, .6f)); z.mk.Prism(steel, c, up, .05f, 2.6f, 6);
                    z.mk.Cone(SongGlow(Vacation, new Color(1f, .55f, .65f), 1f), c + up * 2.4f, 1.6f, .6f, 10); z.mk.Box(stripe, c + up * .4f + z.front * 1.2f, new Vector3(.7f, .06f, 1.4f), rot * Quaternion.Euler(-25f, 0, 0)); ArtistBits.PostCollider(z.root, c, 2.5f, .5f); glow = new Color(1f, .55f, .65f); break;
                }
                case "Murray Head":
                {
                    z.mk.Box(concrete, c + up * .4f, new Vector3(3.4f, .8f, 3.4f), rot); z.solid.Box(concrete, c + up * .4f, new Vector3(3.4f, .8f, 3.4f), rot);
                    var wsq = SongGlow(Bangkok, new Color(1f, .9f, .75f), 1f);
                    for (var i = 0; i < 8; i++) for (var j = 0; j < 8; j++) z.mk.Box((i + j) % 2 == 0 ? wsq : dark, c + up * .81f + z.side * ((i - 3.5f) * .38f) + z.front * ((j - 3.5f) * .38f), new Vector3(.38f, .02f, .38f), rot);
                    z.mk.Prism(WorldMaterials.Solid(new Color(.9f, .88f, .8f)), c + up * .83f + z.side * .2f, up, .12f, .6f, 8, .4f); z.mk.Prism(dark, c + up * .83f - z.side * .6f + z.front * .4f, up, .12f, .7f, 8, .4f); break;
                }
                case "Future Islands":
                {
                    var hull = WorldMaterials.Solid(new Color(.85f, .85f, .82f)); z.mk.Box(hull, c + up * .5f, new Vector3(1.2f, .6f, 3.2f), rot); z.mk.Cone(hull, c + up * .5f + z.front * 1.6f, .6f, .9f, 4, 45f);
                    z.mk.Prism(steel, c + up * .8f, up, .05f, 4f, 6); z.mk.Tri(SongGlow(Sail, new Color(.95f, .9f, 1f), 1f), c + up * 4.7f, c + up * 1.1f, c + up * 1.1f - z.front * 1.6f, c + z.side); z.mk.Tri(SongGlow(Sail, new Color(.95f, .9f, 1f), 1f), c + up * 4.7f, c + up * 1.1f - z.front * 1.6f, c + up * 1.1f, c - z.side);
                    z.solid.Box(hull, c + up * .5f, new Vector3(1.3f, 1f, 3.6f), rot); glow = new Color(.9f, .9f, 1f); break;
                }
                case "Fontaines D.C.":
                {
                    var paint = WorldMaterials.Solid(new Color(.6f, .75f, .7f), .6f); var d = z.side; var r2 = Quaternion.LookRotation(d);
                    foreach (var s in new[] { -.6f, .6f }) z.mk.Prism(dark, c + d * s + up * .3f - z.front * .1f, z.front, .3f, .2f, 10);
                    z.mk.Box(paint, c + up * .6f - d * .2f, new Vector3(.5f, .5f, 1.1f), r2); z.mk.Box(paint, c + up * .9f + d * .55f, new Vector3(.5f, .9f, .2f), r2); z.mk.Box(dark, c + up * .9f - d * .3f, new Vector3(.45f, .12f, .7f), r2);
                    z.mk.Prism(SongGlow(RomanHoliday, new Color(1f, .95f, .8f), 2.4f), c + up * 1.3f + d * .66f, d, .1f, .05f, 8); z.solid.Box(paint, c + up * .6f, new Vector3(.6f, 1.2f, 1.8f), r2); break;
                }
                case "Geese":
                {
                    z.mk.Box(dark, c + up * .02f, new Vector3(2.4f, .04f, 3f), rot); foreach (var s in new[] { -1.2f, 1.2f }) { z.mk.Box(steel, c + z.side * s + up * .5f, new Vector3(.08f, 1f, 3f), rot); z.solid.Box(steel, c + z.side * s + up * .5f, new Vector3(.15f, 1f, 3f), rot); }
                    var gp = c + z.front * 1.6f; foreach (var s in new[] { -1.3f, 1.3f }) z.mk.Prism(steel, gp + z.side * s, up, .05f, 2.4f, 6);
                    z.mk.Blob(SongGlow(LongIslandCity, new Color(.4f, 1f, .5f), 2.4f), gp + z.side * 1.3f + up * 2.5f, Vector3.one * .22f, ctx.Rng, 0f); z.mk.Blob(SongGlow(LongIslandCity, new Color(.4f, 1f, .5f), 2.4f), gp - z.side * 1.3f + up * 2.5f, Vector3.one * .22f, ctx.Rng, 0f);
                    ArtistBits.Label(z.labels, "SUBWAY", gp + up * 2.2f, z.front, .2f, Ink, TextAnchor.MiddleCenter); glow = new Color(.4f, 1f, .5f); break;
                }
                case "The Smile":
                {
                    var zm = SongGlow(ZeroSum, new Color(.9f, .95f, 1f), 2.2f);
                    for (var k = 0; k < 24; k++) { var a0 = k * Mathf.PI / 12f; var a1 = (k + 1) * Mathf.PI / 12f; var p0 = c + up * 2.4f + z.side * Mathf.Cos(a0) * 1f + up * Mathf.Sin(a0) * 1.6f; var p1 = c + up * 2.4f + z.side * Mathf.Cos(a1) * 1f + up * Mathf.Sin(a1) * 1.6f; z.mk.Box(zm, (p0 + p1) / 2f, new Vector3(.16f, .16f, Vector3.Distance(p0, p1) + .02f), Quaternion.LookRotation(p1 - p0, z.front)); }
                    z.mk.Box(darkConcrete, c + up * .3f, new Vector3(2.6f, .6f, 1f), rot); z.solid.Box(darkConcrete, c + up * 2f, new Vector3(2.6f, 4f, 1f), rot); glow = Color.white; break;
                }
                case "Protomartyr":
                {
                    z.mk.Prism(steel, c, up, .25f, 12f, 4, .2f); for (var k = 1; k < 5; k++) z.mk.Box(steel, c + up * (k * 2.4f), new Vector3(2.4f - k * .4f, .08f, .08f), rot);
                    z.mk.Blob(SongGlow(SoundsWeCannotHear, new Color(1f, .15f, .1f), 3f), c + up * 12.2f, Vector3.one * .22f, ctx.Rng, 0f); life.blinkers.Add(c + up * 12.2f); ArtistBits.PostCollider(z.root, c, 12f, .3f); glow = new Color(1f, .15f, .1f); break;
                }
                case "Racing Mount Pleasant":
                {
                    var red = WorldMaterials.Solid(new Color(.6f, .08f, .08f), .5f); z.mk.Prism(red, c, up, .4f, 1.5f, 12); z.mk.Blob(red, c + up * 1.5f, new Vector3(.42f, .2f, .42f), ctx.Rng, 0f);
                    z.mk.Box(SongGlow(Emily, new Color(1f, .9f, .7f), 2f), c + up * 1.2f + z.front * .41f, new Vector3(.4f, .06f, .02f), rot); ArtistBits.PostCollider(z.root, c, 1.6f, .42f); break;
                }
                case "Haindling":
                {
                    z.mk.Prism(darkConcrete, c, up, .9f, .5f, 10); z.mk.Prism(dark, c + up * .5f, up, .8f, 1f, 14, 1.2f); z.mk.Prism(SongGlow(Schmelztiegel, new Color(1f, .45f, .1f), 2.6f), c + up * 1.46f, up, .9f, .05f, 14);
                    halos.Add(c + up * 1.8f, 1.8f, new Color(1f, .45f, .1f), .6f); life.fires.Add(c + up * 1.5f); ArtistBits.PostCollider(z.root, c, 1.5f, 1f); glow = new Color(1f, .45f, .1f); break;
                }
                case "BLUESTEM":
                {
                    z.mk.Prism(steel, c, up, .06f, 2f, 6); z.mk.Box(SongGlow(SoreEyes, new Color(.95f, .95f, .9f), 1.2f), c + up * 2.6f, new Vector3(1.2f, 1.8f, .08f), rot);
                    for (var k = 0; k < 5; k++) z.mk.Box(dark, c + up * (3.2f - k * .3f) + z.front * .05f, new Vector3(.8f - k * .13f, .14f - k * .02f, .01f), rot);
                    ArtistBits.PostCollider(z.root, c, 3.5f, .6f); break;
                }
                case "Dexys Midnight Runners":
                {
                    var wood = WorldMaterials.Solid(new Color(.45f, .28f, .15f), .6f); z.mk.Box(wood, c + up * .9f, new Vector3(1.4f, 1.8f, .8f), rot); z.mk.Prism(wood, c + up * 1.8f, z.front, .7f, .8f, 12);
                    z.mk.Box(SongGlow(ComeOnEileen, new Color(1f, .6f, .25f), 2.2f), c + up * 1.1f + z.front * .41f, new Vector3(1f, .8f, .02f), rot); z.mk.Prism(SongGlow(ComeOnEileen, new Color(1f, .3f, .6f), 2.2f), c + up * 1.8f + z.front * .42f, z.front, .6f, .03f, 12);
                    halos.Add(c + up * 1.4f + z.front * .9f, 1.6f, new Color(1f, .5f, .4f), .5f); z.solid.Box(wood, c + up * 1.2f, new Vector3(1.5f, 2.5f, .9f), rot); break;
                }
                default:
                {
                    // a shipping container with its doors open and a light inside
                    var cm = WorldMaterials.Solid(new Color(.3f, .4f, .45f), .3f); z.mk.Box(cm, c + up * 1.3f, new Vector3(2.45f, 2.55f, 6f), rot); z.solid.Box(cm, c + up * 1.3f, new Vector3(2.45f, 2.55f, 6f), rot);
                    z.mk.Box(GlowM(Window, 1.6f), c + up * 1.3f + z.front * 3.01f, new Vector3(2.2f, 2.3f, .02f), rot); break;
                }
            }
            halos.Add(c + up * 2f + z.front * 1f, 2f, glow, .25f);
        }
    }
}
