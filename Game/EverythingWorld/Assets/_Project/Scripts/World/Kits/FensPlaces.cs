using System.Collections.Generic;
using UnityEngine;
using static EverythingWorld.World.PlaceTools;

namespace EverythingWorld.World
{
    /// The Shoegaze Fens laid out on purpose (the build-land standard). The cathedral, the fen and every island stay
    /// where they were; three more lamp-lit trails leave the water for the wide empty hills: east to the sea, north
    /// to the headland, south to the hollow. Along them, with the people of the fens (who mostly look at their shoes):
    /// the Pedalboard (five effects pedals the size of cars: stomp one and the whole hill's light takes the effect);
    /// the Swing at the End of the World (two swings on the last rise before the sea); the Glasshouse (a ruined iron
    /// conservatory, its panes still glowing, giant flowers inside); and the Last Dance (a floor of glowing tiles
    /// under a mirror ball hung from a broken arch, two couples who never stopped).
    public partial class FensKit
    {
        static readonly Vector2 PedalAt = new Vector2(9800, 7216), SwingAt = new Vector2(9936, 7228), GlassAt = new Vector2(9402, 7498), DanceAt = new Vector2(9355, 6792);
        static readonly Vector2[] EastTrail = { new Vector2(9612, 7150), new Vector2(9690, 7182), new Vector2(9760, 7196), new Vector2(9860, 7226), new Vector2(9920, 7230) };
        static readonly Vector2[] NorthTrail = { new Vector2(9462, 7418), new Vector2(9436, 7462), new Vector2(9414, 7484) };
        static readonly Vector2[] SouthTrail = { new Vector2(9402, 6925), new Vector2(9382, 6862), new Vector2(9362, 6815) };
        static readonly (Vector2 at, float r)[] PlaceRings = { (PedalAt, 14f), (SwingAt, 6f), (GlassAt, 11f), (DanceAt, 12f) };

        FensPlacesLife plife; System.Random prng; Transform rootT; LandContext ctx0; readonly List<Vector3> trailP = new List<Vector3>();

        static readonly Color[] Cardigans = { new Color(.84f, .65f, .7f), new Color(.65f, .61f, .78f), new Color(.3f, .28f, .36f), new Color(.9f, .88f, .92f), new Color(.5f, .42f, .6f), new Color(.2f, .2f, .26f) };
        static readonly Color[] Legs2 = { new Color(.2f, .2f, .26f), new Color(.3f, .28f, .36f), new Color(.45f, .4f, .5f) };
        static readonly string[] Gazing =
        {
            "Sorry, I was looking at my shoes. They're good shoes.", "It's loud here, but it's a soft loud. You stop hearing it and start standing in it.", "The dusk doesn't end. I checked. I've been checking for years.",
            "Somebody's left a pedal on somewhere. Somebody always has.", "You don't walk through the fen so much as let it go past you.",
        };

        Residents.Look Gazer(Residents.Tool tool = Residents.Tool.None) { var l = Residents.Random(prng, Cardigans, Legs2, new[] { Residents.Hat.None, Residents.Hat.None, Residents.Hat.Beanie }, Cardigans); l.tool = tool; l.longHair = prng.Next(3) != 0; return l; }
        Resident Folk(Vector3 feet, Vector3 face, Resident.Pose pose, string[] lines = null, Residents.Look look = null, Vector3[] route = null, float seatH = HumanScale.SeatHeight)
            => Residents.Make(plife.transform, feet, face, look ?? Gazer(), pose, prng, lines ?? Gazing, route, seatH);
        void Col(Vector3 c, Vector3 size, Quaternion rot) { var go = new GameObject("Box collider"); go.transform.SetParent(rootT, false); go.transform.SetPositionAndRotation(c, rot); go.AddComponent<BoxCollider>().size = size; go.isStatic = true; }
        Vector3 G(Vector3 p) => ctx0.OnGround(p.x, p.z);
        void Ring(Vector3 at, float r) => noGrass.Add(new Vector3(at.x, r, at.z));

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
            ShapeFen(data, index);   // everything the land already did, unchanged
            foreach (var (at, r) in PlaceRings) PlaceLevel(data, index, at, r, r + 12f);
        }

        /// A standing stone with words cut in it. Signs go on the +x side of a place's trail; the photo spot on the −x side.
        void WordStone(MeshKit mk, Vector3 at, Vector3 face, string text, Color glow, float size = .16f)
        {
            var up = Vector3.up; var g = G(at); var rot = Quaternion.LookRotation(face) * Quaternion.Euler(0, 0, 3f);
            mk.Box(stoneDark, g + up * .9f, new Vector3(1.9f, 2f, .35f), rot); Col(g + up * .9f, new Vector3(1.9f, 2f, .4f), rot); mk.Box(GlowM(glow, 2f), g + up * .25f + face * .18f, new Vector3(1.5f, .05f, .03f), rot);
            ArtistBits.Label(rootT, text, g + up * 1.15f + face * .19f, face, size, Color.Lerp(glow, Color.white, .5f), TextAnchor.MiddleCenter);
        }

        void StoneBench(MeshKit mk, Vector3 at, Vector3 face, float len = 2f)
        {
            var up = Vector3.up; var g = G(at); var rot = Quaternion.LookRotation(face); var side = Vector3.Cross(up, face);
            mk.Box(stoneM, g + up * .45f, new Vector3(len, .14f, .5f), rot); foreach (var e in new[] { -1f, 1f }) mk.Box(stoneDark, g + up * .2f + side * e * (len / 2f - .25f), new Vector3(.3f, .45f, .45f), rot);
            Col(g + up * .3f, new Vector3(len, .6f, .5f), rot); Interact.Bench(plife.transform, g + up * .54f - side * (len / 2f - .4f), g + up * .54f + side * (len / 2f - .4f), face);
        }

        List<Vector3> LampTrail(LandContext ctx, MeshKit mk, MeshKit solid, Vector2[] way, int id)
        {
            Vector2 near = way[0]; var bd = 200f * 200f; foreach (var (p, _) in walkPoints) { if (InMarsh(p.x, p.z, 2f)) continue; var d = (p.x - way[0].x) * (p.x - way[0].x) + (p.z - way[0].y) * (p.z - way[0].y); if (d < bd) { bd = d; near = new Vector2(p.x, p.z); } }
            var full = new List<Vector2> { near }; full.AddRange(way);
            var pts = PlaceTools.Smooth(ctx, full, false, 2.5f); pts.RemoveAll(p => InMarsh(p.x, p.z, -2f) && !OnIsland(p.x, p.z));
            for (var i = 0; i < pts.Count; i++) foreach (var c in noGrass) { if (c.y < 5.5f) continue; var r = c.y + 3f; var v = new Vector2(pts[i].x - c.x, pts[i].z - c.z); var d = v.magnitude; if (d < r) { v = d > .01f ? v / d * r : new Vector2(r, 0); pts[i] = ctx.OnGround(c.x + v.x, c.z + v.y); } }
            if (pts.Count < 3) return pts;
            Strip(mk, solid, pts, WorldMaterials.Solid(new Color(.5f, .44f, .46f)));
            foreach (var p in pts) Remember(p, id);
            var up = Vector3.up; var n = 0;
            for (var i = 5; i < pts.Count; i += 10) { var d = pts[i] - pts[i - 1]; d.y = 0; if (d.sqrMagnitude < .01f) continue; d.Normalize(); var lp = pts[i] + Vector3.Cross(up, d) * (n % 2 == 0 ? 2.2f : -2.2f); Lamp(ctx, mk, ctx.Root, ctx.OnGround(lp.x, lp.z), Glows[n % Glows.Length]); n++; }
            trailP.AddRange(pts);
            if (pts.Count > 34) { var i = pts.Count / 2; var d = pts[i + 1] - pts[i - 1]; d.y = 0; d.Normalize(); var sd = Vector3.Cross(up, d); var bp = pts[i] - sd * 3f; if (ctx.IsLand(bp.x, bp.z, 2f)) { StoneBench(mk, bp, sd); Ring(bp, 2f); } }
            return pts;
        }

        void Lane(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 to)
        {
            var from = Nearest(trailP, to); var d = new Vector2(to.x - from.x, to.z - from.z); var len = d.magnitude; if (len < 4f) return; d /= len;
            var a = new Vector2(from.x, from.z) + d * 1.2f; var b = new Vector2(to.x, to.z);
            var pts = PlaceTools.Smooth(ctx, new[] { a, Vector2.Lerp(a, b, .5f), b }, false, 2f, 1f); if (pts.Count < 3) return;
            Strip(mk, solid, pts, WorldMaterials.Solid(new Color(.5f, .44f, .46f))); foreach (var p in pts) Remember(p, 2900);
        }

        void HillPlaces(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            trailP.Clear(); rootT = ctx.Root; ctx0 = ctx; prng = new System.Random(1991);
            plife = new GameObject("Fens places life").AddComponent<FensPlacesLife>(); plife.transform.SetParent(life.transform, false);
            foreach (var (at, r) in PlaceRings) noGrass.Add(new Vector3(at.x, r, at.y));
            var east = LampTrail(ctx, mk, solid, EastTrail, 2000); LampTrail(ctx, mk, solid, NorthTrail, 2001); LampTrail(ctx, mk, solid, SouthTrail, 2002);
            Pedalboard(ctx, mk, solid); SwingAtTheEnd(ctx, mk, solid); Glasshouse(ctx, mk, solid); LastDance(ctx, mk, solid);
            // someone drifting along the east trail, not in any hurry
            if (east.Count > 24) { var route = new List<Vector3>(); for (var i = 6; i < east.Count - 6; i += 7) route.Add(ctx.OnGround(east[i].x + .7f, east[i].z)); for (var i = route.Count - 2; i > 0; i--) route.Add(route[i]); if (route.Count > 3) Folk(route[0], FaceTo(route[0], route[1]), Resident.Pose.Walk, new[] { "I'm walking to the sea. I've been walking to the sea since the first chorus.", "There's a swing at the end. You'll want the swing.", "Stomp the fuzz on your way. Everyone should, once." }, Gazer(), route.ToArray()).speed = .8f; }
        }

        // ---------------------------------------------------------------- the Pedalboard
        void Pedalboard(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var g = ctx.OnGround(PedalAt.x, PedalAt.y); var f = new PlaceFrame(g, FaceTo(g, Nearest(trailP, g)));
            // a board of dark wood laid on the hill, five pedals in a row, each the size of a small car, patch cables looping between
            mk.Box(black, g + up * .12f - f.Fwd * 1f, new Vector3(23f, .3f, 8f), f.r); solid.Box(black, g + up * .02f - f.Fwd * 1f, new Vector3(23f, .5f, 8f), f.r);
            string[] names = { "CHORUS", "REVERB", "FUZZ", "DELAY", "TREMOLO" }; Color[] cols = { new Color(.5f, .7f, 1f), new Color(.68f, .42f, 1f), new Color(1f, .35f, .7f), new Color(1f, .78f, .4f), new Color(.5f, 1f, .75f) };
            string[] says = { "CHORUS: the lights double, and drift apart a little.", "REVERB: everything swells and takes a long time to stop.", "FUZZ: the whole hill frays at the edges.", "DELAY: the light answers itself, down the row.", "TREMOLO: the dusk shivers, fast and even." };
            for (var k = 0; k < 5; k++)
            {
                var c = g + f.Right * (-8.8f + k * 4.4f) - f.Fwd * 1f + up * .27f; var body = WorldMaterials.Solid(Color.Lerp(cols[k], new Color(.2f, .2f, .25f), .35f), .6f);
                mk.Box(body, c + up * .7f, new Vector3(3.2f, 1.4f, 5f), f.r); solid.Box(body, c + up * .7f, new Vector3(3.2f, 1.4f, 5f), f.r);
                for (var s = 0; s < 3; s++) { mk.Prism(black, c + up * 1.4f - f.Fwd * 1.6f + f.Right * (-1f + s * 1f), up, .32f, .3f, 10); mk.Box(stoneM, c + up * 1.72f - f.Fwd * 1.45f + f.Right * (-1f + s * 1f), new Vector3(.06f, .03f, .3f), f.r * Quaternion.Euler(0, k * 40f + s * 70f, 0)); }
                ArtistBits.Label(ctx.Root, names[k], c + up * 1.42f - f.Fwd * .5f, up, .5f, Color.white, TextAnchor.MiddleCenter);
                var led = new Material(WorldMaterials.Glow(cols[k], .3f)) { name = "Pedal light" }; mk.Blob(led, c + up * 1.5f - f.Fwd * 2.2f, Vector3.one * .16f, prng, 0f);
                // the footswitch: a big chrome dome at the front you step up to and stomp
                var sw = new MeshKit(); sw.Prism(WorldMaterials.Solid(new Color(.8f, .8f, .85f), .9f), Vector3.zero, Vector3.up, .5f, .28f, 12, .8f); var sgo = sw.BuildSingle("Footswitch " + names[k], plife.transform, false); sgo.transform.position = c + up * 1.4f + f.Fwd * 1.5f;
                var idx = k; var t = Interact.Toy(sgo, Toy.Kind.Bob, "Stomp " + names[k], new Vector3(1.6f, 1.2f, 1.6f)); t.amount = -.12f; t.seconds = .35f; t.onUse = _ => { plife.Stomp(idx); Say(plife.transform, c + up * 3.2f, plife.on[idx] ? says[idx] : names[idx] + " off.", 4f); };
                plife.leds.Add((led, cols[k])); mk.Box(body, c + up * .2f + f.Fwd * 3f, new Vector3(2f, .4f, 1f), f.r); solid.Box(body, c + up * .2f + f.Fwd * 3f, new Vector3(2f, .4f, 1f), f.r);   // a step up to the switch
                if (k < 4) for (var s = 0; s <= 6; s++) { var u = s / 6f; soft.Blob(black, c + f.Right * (1.6f + u * 1.2f) - f.Fwd * 2.2f + up * (.6f + Mathf.Sin(u * Mathf.PI) * .9f), Vector3.one * .12f, prng, 0f); }
            }
            // a row of tall light posts behind the board: these are what the pedals play
            for (var k = 0; k < 9; k++) { var p = G(f.P(-12f + k * 3f, 0, -7.5f)); mk.Prism(iron, p, up, .07f, 5f, 6); var om = new Material(WorldMaterials.Glow(Lilac, 2f)) { name = "Pedalboard light" }; var ok = new MeshKit(); ok.Blob(om, Vector3.zero, Vector3.one * .38f, prng, 0f); var ogo = ok.BuildSingle("Soft pedalboard orb", plife.transform, false); ogo.transform.position = p + up * 5.2f; plife.orbs.Add((ogo.transform, om, p + up * 5.2f, k)); ArtistBits.PostCollider(ctx.Root, p, 5f, .1f); halos.Add(p + up * 5.2f, 1.8f, Lilac, .5f); }
            plife.boardLight = ArtistBits.PointLight(ctx.Root, g + up * 4f - f.Fwd * 3f, Lilac, 34f, 1.8f);
            // an amp to sit on, a guitar left leaning, two gazers
            var amp = G(f.P(13f, 0, 2f)); mk.Box(black, amp + up * .6f, new Vector3(1.6f, 1.2f, .8f), f.r); mk.Box(cloth, amp + up * .55f + f.Fwd * .41f, new Vector3(1.4f, .9f, .02f), f.r); Col(amp + up * .6f, new Vector3(1.6f, 1.2f, .8f), f.r); Interact.Seat(plife.transform, amp + up * 1.26f, f.Fwd, "Sit on the amp");
            Folk(G(f.P(-13f, 0, 3f)), f.Right, Resident.Pose.Stand, new[] { "I stand on the reverb for a whole song sometimes. You come out the other side different.", "All five at once. Do it. I'll watch from here. Well. I'll look at my shoes from here.", "The fuzz is the loud one. The delay is the sad one. Don't ask me why." }, Gazer());
            Folk(G(f.P(3f, 0, 5.5f)), -f.Fwd, Resident.Pose.Stand, new[] { "We came up from the fen to see what the noise was. We are the noise now.", "It's the same three chords. It's always been the same three chords. It's enough." }, Gazer());
            WordStone(mk, f.P(13.5f, 0, 8.5f), f.Fwd, "THE\nPEDALBOARD", HotPink);
            Lane(ctx, mk, solid, f.P(0, 0, 5f)); Ring(g, 13f);
            Shot(ctx, "the pedalboard", f.P(-13f, 6f, 15f), g + up * 1.6f - f.Fwd * 2f);
        }

        // ---------------------------------------------------------------- the Swing at the End of the World
        void SwingAtTheEnd(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var g = ctx.OnGround(SwingAt.x, SwingAt.y); var sea = FaceTo(Nearest(trailP, g), g); var f = new PlaceFrame(g, sea);   // you swing out toward the sea
            // a tall frame of old timber, two swings on long ropes, a lamp at each foot
            const float H = 5.2f; foreach (var e in new[] { -1f, 1f }) { foreach (var z in new[] { -1f, 1f }) { var foot = G(g + f.Right * e * 2.6f + f.Fwd * z * 1.5f); var top = g + f.Right * e * 2.4f + up * H; mk.Prism(wood, foot - up * .3f, (top - foot).normalized, .12f, Vector3.Distance(foot, top) + .3f, 6); } ArtistBits.PostCollider(ctx.Root, G(g + f.Right * e * 2.5f), H, .3f); Lamp(ctx, mk, ctx.Root, G(g + f.Right * e * 4f + f.Fwd * 1f), e < 0 ? HotPink : Violet, 2.2f); }
            mk.Prism(wood, g + up * H - f.Right * 2.6f, f.Right, .12f, 5.2f, 6);
            for (var k = 0; k < 2; k++)
            {
                var sm = new MeshKit(); foreach (var e in new[] { -.3f, .3f }) sm.Box(WorldMaterials.Solid(new Color(.75f, .68f, .58f)), new Vector3(e, -2.15f, 0), new Vector3(.03f, 4.3f, .03f), Quaternion.identity); sm.Box(wood, new Vector3(0, -4.3f, 0), new Vector3(.75f, .06f, .3f), Quaternion.identity);
                var sgo = sm.BuildSingle("Long swing", plife.transform, false); sgo.transform.SetPositionAndRotation(g + up * (H - .05f) + f.Right * (-1.1f + k * 2.2f), f.r); var sw = sgo.AddComponent<Sway>(); sw.axis = Vector3.right; sw.degrees = 30f; sw.speed = 1.9f; sw.decay = .16f; sw.seconds = 24f;
                var t = Interact.Toy(sgo, Toy.Kind.Custom, "Push the swing", new Vector3(1f, .5f, 1f)); sgo.GetComponent<BoxCollider>().center = new Vector3(0, -3.7f, 0); t.onUse = _ => sw.Push();
                var seat = Interact.Seat(sgo.transform, sgo.transform.position - up * 4.2f, f.Fwd, "Sit on the swing"); seat.transform.localPosition = new Vector3(0, -4.2f, 0);
            }
            StoneBench(mk, g - f.Fwd * 4f + f.Right * 3.5f, f.Fwd);
            Folk(G(g - f.Fwd * 4f + f.Right * 3.5f) + up * .54f - f.Right * .5f, f.Fwd, Resident.Pose.Sit, new[] { "You push off and for a second there's nothing under you but pink. Then you come back. That's the song, really.", "I come for the last light. It's always the last light.", "Both swings are free. They're always both free." }, Gazer(), null, .54f);
            WordStone(mk, g - f.Fwd * 7f + f.Right * 4.5f, -f.Fwd, "THE SWING\nAT THE END\nOF THE WORLD", Violet, .13f);
            Ring(g, 5f);
            Shot(ctx, "the swing at the end of the world", g - f.Fwd * 9f - f.Right * 4f + up * 3f, g + up * 2.6f + f.Fwd * 3f);
        }

        // ---------------------------------------------------------------- the Glasshouse
        void Glasshouse(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var g = ctx.OnGround(GlassAt.x, GlassAt.y); var f = new PlaceFrame(g, FaceTo(g, Nearest(trailP, g))); const float W = 9f, D = 14f, H = 4f;
            // a ruined iron conservatory: a stone plinth, white iron ribs, panes of pink, violet and blue still glowing where they survive
            mk.Box(stoneM, g + up * .05f, new Vector3(W + 1f, .5f, D + 1f), f.r); solid.Box(stoneM, g - up * .05f, new Vector3(W + 1f, .7f, D + 1f), f.r); var fo = new PlaceFrame(g + up * .3f, f.Fwd); var rib = WorldMaterials.Solid(new Color(.9f, .88f, .92f));
            for (var k = 0; k <= 7; k++)
            {
                var z = -D / 2f + k * D / 7f; foreach (var e in new[] { -1f, 1f }) { mk.Box(rib, fo.P(e * W / 2f, H / 2f, z), new Vector3(.12f, H, .12f), f.r); ArtistBits.PostCollider(ctx.Root, fo.P(e * W / 2f, 0, z), H, .1f); mk.Prism(rib, fo.P(e * W / 2f, H, z), (up * .8f - f.Right * e).normalized, .06f, W / 2f * 1.28f, 5); }
                if (k < 7) foreach (var e in new[] { -1f, 1f })
                {
                    var zc = z + D / 14f; mk.Box(rib, fo.P(e * W / 2f, H, zc), new Vector3(.08f, .08f, D / 7f), f.r); mk.Box(rib, fo.P(e * W / 2f, 1.2f, zc), new Vector3(.08f, .08f, D / 7f), f.r);
                    var broken = (k * 3 + (e > 0 ? 1 : 0)) % 4 == 0; if (!broken) { var c = Glows[(k + (e > 0 ? 2 : 0)) % Glows.Length]; var pane = new Material(WorldMaterials.Glow(c, .7f)) { name = "Glasshouse pane" }; mk.Box(pane, fo.P(e * W / 2f, 2.6f, zc), new Vector3(.04f, 2.7f, D / 7f - .2f), f.r); plife.panes.Add((pane, c, k * .7f + (e > 0 ? 1.3f : 0))); }
                    Col(fo.P(e * W / 2f, 1.3f, zc), new Vector3(.2f, 2.6f, D / 7f), f.r);
                }
            }
            mk.Box(rib, fo.P(0, H + W / 2f * .8f, 0), new Vector3(.1f, .1f, D), f.r); mk.Box(rib, fo.P(0, H / 2f, -D / 2f), new Vector3(W, .1f, .1f), f.r); for (var s = 0; s < 4; s++) { mk.Box(rib, fo.P(-W / 2f + W * (s + .5f) / 4f, H / 2f, -D / 2f), new Vector3(.08f, H, .08f), f.r); var c = Glows[(s + 1) % Glows.Length]; var pane = new Material(WorldMaterials.Glow(c, .7f)) { name = "Glasshouse pane" }; if (s != 2) { mk.Box(pane, fo.P(-W / 2f + W * (s + .5f) / 4f, 2.4f, -D / 2f), new Vector3(W / 4f - .2f, 3f, .04f), f.r); plife.panes.Add((pane, c, s * 1.1f)); } }
            Col(fo.P(0, H / 2f, -D / 2f), new Vector3(W, H, .2f), f.r);
            // giant glowing flowers down both sides of a tiled path, a dry fountain in the middle, two benches
            var tile = WorldMaterials.Solid(new Color(.75f, .7f, .78f)); mk.Box(tile, fo.P(0, .02f, 0), new Vector3(2f, .04f, D - 1f), f.r);
            for (var k = 0; k < 8; k++)
            {
                var e = k % 2 == 0 ? -1f : 1f; var p = fo.P(e * (2.6f + (k % 3) * .4f), 0, -5.5f + (k / 2) * 3.4f + (k % 2) * 1.2f); var h = 1.4f + (k % 3) * .7f; var c = Glows[k % Glows.Length];
                mk.Prism(leafM, p, (up + f.Right * e * -.1f).normalized, .07f, h, 5); ArtistBits.PostCollider(ctx.Root, p, h, .1f); var top = p + up * h - f.Right * e * .1f * h; var fm = new Material(WorldMaterials.Glow(c, 1.4f)) { name = "Glasshouse flower" };
                for (var s = 0; s < 6; s++) soft.Blob(fm, top + Quaternion.Euler(0, s * 60f, 0) * Vector3.forward * .32f + up * .05f, new Vector3(.22f, .1f, .22f), prng, .1f); soft.Blob(GlowM(Gold, 2.4f), top + up * .1f, Vector3.one * .13f, prng, 0f); halos.Add(top, 1.1f, c, .5f); plife.flowers.Add((fm, c, k * .9f));
                for (var s = 0; s < 3; s++) soft.Blob(leafM, p + Quaternion.Euler(0, s * 120f + k * 30f, 0) * Vector3.forward * .4f + up * (.3f + s * .25f), new Vector3(.35f, .06f, .2f), prng, .2f);
            }
            mk.Prism(stoneM, fo.P(0, 0, .5f), up, 1.2f, .45f, 10); mk.Prism(stoneDark, fo.P(0, .45f, .5f), up, .3f, 1f, 8); mk.Prism(stoneM, fo.P(0, 1.45f, .5f), up, .7f, .12f, 10); var fc = new GameObject("Fountain collider"); fc.transform.SetParent(rootT, false); fc.transform.position = fo.P(0, .5f, .5f); var fcc = fc.AddComponent<CapsuleCollider>(); fcc.radius = 1.2f; fcc.height = 1.2f;
            foreach (var z in new[] { -3.2f, 4.4f }) StoneBench(mk, fo.P(-1.9f, 0, z) - up * .3f, f.Right, 1.8f);
            // the watering can that never empties; the gardener
            var wc = new MeshKit(); var tin = WorldMaterials.Solid(new Color(.6f, .55f, .65f), .6f); wc.Prism(tin, Vector3.zero, Vector3.up, .14f, .28f, 8); wc.Prism(tin, new Vector3(.12f, .14f, 0), new Vector3(1f, .7f, 0).normalized, .03f, .35f, 5); wc.Box(tin, new Vector3(-.2f, .2f, 0), new Vector3(.03f, .2f, .04f), Quaternion.identity);
            var wgo = wc.BuildSingle("Watering can", plife.transform, false); wgo.transform.position = fo.P(1.6f, .05f, -2f); var wcar = Interact.Carry(wgo, "the watering can", .8f, g.y - 10f); var wbc = wcar.GetComponent<BoxCollider>(); if (wbc != null) { wbc.size = new Vector3(.5f, .35f, .3f); wbc.center = new Vector3(0, .17f, 0); }
            var gard = Gazer(Residents.Tool.Hose); gard.apron = true; Folk(fo.P(1.4f, 0, 2.6f), -f.Right, Resident.Pose.Work, new[] { "They only open when it's loud. Quiet songs, they sulk.", "The roof went in a storm before my time. The flowers prefer it. They can hear better.", "Sit a while. They glow a bit more when someone's listening with them." }, gard).workRate = .5f;
            ArtistBits.PointLight(ctx.Root, fo.P(0, 3f, -2f), Lilac, 16f, 1.6f); ArtistBits.PointLight(ctx.Root, fo.P(0, 3f, 4f), DustyPink, 16f, 1.6f);
            WordStone(mk, f.P(6.5f, 0, D / 2f + 4f), f.Fwd, "THE\nGLASSHOUSE", Lilac);
            Lane(ctx, mk, solid, f.P(0, 0, D / 2f + 1f)); Ring(g, 10f);
            Shot(ctx, "the glasshouse", f.P(-8f, 3.4f, D / 2f + 9f), fo.P(0, 2.2f, 0));
            Shot(ctx, "inside the glasshouse", fo.P(0, 1.6f, D / 2f - 1f), fo.P(0, 1.6f, -D / 2f));
        }

        // ---------------------------------------------------------------- the Last Dance
        void LastDance(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var g = ctx.OnGround(DanceAt.x, DanceAt.y); var f = new PlaceFrame(g, FaceTo(g, Nearest(trailP, g))); const int N = 6; const float T = 1.6f;
            // a floor of tiles, some lit, some dark, cracked at one corner; life chases a slow light across them
            mk.Box(stoneDark, g + up * .05f, new Vector3(N * T + .8f, .3f, N * T + .8f), f.r); solid.Box(stoneDark, g - up * .05f, new Vector3(N * T + .8f, .5f, N * T + .8f), f.r);
            for (var x = 0; x < N; x++) for (var z = 0; z < N; z++)
            {
                if (x == 0 && z == N - 1) continue; var c = (x + z) % 2 == 0 ? HotPink : Violet; var tm = new Material(WorldMaterials.Glow(c, .3f)) { name = "Dance tile" };
                mk.Box(tm, g + f.r * new Vector3((x - (N - 1) / 2f) * T, .21f, (z - (N - 1) / 2f) * T), new Vector3(T - .1f, .03f, T - .1f), f.r); plife.tiles.Add((tm, c, x, z));
            }
            // a broken stone arch over it, the mirror ball hanging from the keystone
            foreach (var e in new[] { -1f, 1f }) { var foot = G(g + f.Right * e * (N * T / 2f + 1.2f)); mk.Box(stoneM, foot + up * 3f, new Vector3(1f, 6.4f, 1f), f.r); Col(foot + up * 3f, new Vector3(1f, 6.4f, 1f), f.r); for (var s = 0; s < (e < 0 ? 5 : 3); s++) { var a = (s + .5f) / 6f * Mathf.PI * .5f; var r = N * T / 2f + 1.2f; mk.Box(stoneM, g + f.Right * e * Mathf.Cos(a) * r + up * (6f + Mathf.Sin(a) * 2.4f), new Vector3(1.5f, .8f, .9f), f.r * Quaternion.Euler(0, 0, e * Mathf.Rad2Deg * a)); } }
            var ballAt = g + up * 5.2f; mk.Box(iron, g + up * 6.9f, new Vector3(.04f, 2.6f, .04f), f.r);
            var bm = new MeshKit(); var mirror = new Material(WorldMaterials.Glow(new Color(.95f, .9f, 1f), .9f)) { name = "Mirror ball" }; bm.Blob(mirror, Vector3.zero, Vector3.one * .7f, prng, 0f); for (var s = 0; s < 14; s++) { var d = Quaternion.Euler(s * 47f, s * 83f, 0) * Vector3.forward; bm.Box(GlowM(Glows[s % Glows.Length], 2.6f), d * .68f, new Vector3(.16f, .16f, .04f), Quaternion.LookRotation(d)); }
            var bgo = bm.BuildSingle("Soft mirror ball", plife.transform, false); bgo.transform.position = ballAt; plife.ball = bgo.transform; plife.ballLight = ArtistBits.PointLight(ctx.Root, ballAt - up * .9f, Lilac, 26f, 2f); halos.Add(ballAt, 2.2f, Lilac, .5f);
            var sgo = new GameObject("Mirror ball cord"); sgo.transform.SetParent(plife.transform, false); var cp = G(g + f.Right * (N * T / 2f + .4f) + f.Fwd * 1.2f); sgo.transform.position = cp + up * 1.3f; mk.Box(iron, cp + up * .7f, new Vector3(.06f, 1.4f, .06f), f.r); mk.Blob(GlowM(Gold, 2.4f), cp + up * 1.45f, Vector3.one * .1f, prng, 0f); ArtistBits.PostCollider(ctx.Root, cp, 1.4f, .07f);
            Interact.Toy(sgo, Toy.Kind.Custom, "Spin the mirror ball", new Vector3(.7f, 1f, .7f)).onUse = _ => { plife.spun = Time.time; Say(plife.transform, cp + up * 2.4f, "The floor fills with slow, turning light.", 4f); };
            // two couples who never stopped; a chair for whoever is sitting this one out
            for (var k = 0; k < 2; k++) { var c = g + f.r * new Vector3(-1.6f + k * 3.4f, .23f, -.8f + k * 1.6f); var d = Quaternion.Euler(0, k * 70f + 20f, 0) * f.Right; var a = Gazer(); var b = Gazer(); Folk(c - d * .32f, d, Resident.Pose.Stand, new[] { "We're not really dancing. We're leaning, in time.", "It was the last song four hours ago. They keep playing it. We keep staying." }, a); Folk(c + d * .32f, -d, Resident.Pose.Stand, new[] { "Shh. This is the part.", "Spin the ball for us. It's better with the ball." }, b); }
            var ch = G(g - f.Right * (N * T / 2f + 1.8f) + f.Fwd * 2f); var cq = Quaternion.LookRotation(f.Right); mk.Box(wood, ch + up * .45f, new Vector3(.45f, .06f, .45f), cq); mk.Box(wood, ch + up * .8f - f.Right * .2f, new Vector3(.45f, .7f, .05f), cq); mk.Box(wood, ch + up * .22f, new Vector3(.4f, .45f, .4f), cq); Col(ch + up * .45f, new Vector3(.45f, .9f, .45f), cq); Interact.Seat(plife.transform, ch + up * .52f, f.Right, "Sit this one out");
            StoneBench(mk, g + f.Fwd * (N * T / 2f + 2.2f) - f.Right * 3.5f, -f.Fwd, 2.2f);
            WordStone(mk, f.P(6.5f, 0, N * T / 2f + 4.5f), f.Fwd, "THE\nLAST DANCE", HotPink);
            Lane(ctx, mk, solid, f.P(0, 0, N * T / 2f + 1f)); Ring(g, 11f);
            Shot(ctx, "the last dance", f.P(-8f, 3.6f, N * T / 2f + 9f), g + up * 2.6f);
        }
    }

    /// The Fens places' moving parts: the pedalboard's lights (each pedal changes how they move), the glasshouse's
    /// panes and flowers breathing, the dance floor and its mirror ball.
    public class FensPlacesLife : MonoBehaviour
    {
        public bool[] on = new bool[5]; public List<(Material m, Color c)> leds = new List<(Material, Color)>(); public List<(Transform t, Material m, Vector3 at, int k)> orbs = new List<(Transform, Material, Vector3, int)>(); public Light boardLight, ballLight;
        public List<(Material m, Color c, float phase)> panes = new List<(Material, Color, float)>(), flowers = new List<(Material, Color, float)>(); public List<(Material m, Color c, int x, int z)> tiles = new List<(Material, Color, int, int)>(); public Transform ball; public float spun = -99f;
        static readonly Color Lilac = new Color(.85f, .7f, 1f), Blue = new Color(.5f, .7f, 1f), Pink = new Color(1f, .35f, .7f), Gold = new Color(1f, .78f, .4f);
        float swell;

        public void Stomp(int i) { on[i] = !on[i]; }

        void Update()
        {
            var t = Time.time;
            for (var i = 0; i < leds.Count; i++) leds[i].m.SetColor("_EmissionColor", leds[i].c * (on[i] ? 4f : .3f));
            // chorus: colour drifts and the orbs sway apart; reverb: a long slow swell; fuzz: flicker; delay: a pulse runs down the row; tremolo: fast even shiver
            swell = Mathf.MoveTowards(swell, on[1] ? 1f : 0f, Time.deltaTime * .25f);
            foreach (var (tr, m, at, k) in orbs)
            {
                var c = Lilac; var v = 2f;
                if (on[0]) { c = Color.Lerp(Blue, Pink, .5f + .5f * Mathf.Sin(t * .8f + k * .7f)); tr.position = at + new Vector3(Mathf.Sin(t * 1.3f + k), Mathf.Sin(t * .9f + k * 2f) * .3f, Mathf.Cos(t * 1.1f + k)) * .35f; } else tr.position = at;
                v *= 1f + swell * (1.2f + 1.2f * Mathf.Sin(t * .5f));
                if (on[3]) { var u = Mathf.Repeat(t * .8f - k * .16f, 1.6f); v *= 1f + 2.5f * Mathf.Exp(-u * 5f); c = Color.Lerp(c, Gold, Mathf.Exp(-u * 5f)); }
                if (on[4]) v *= .55f + .45f * Mathf.Sin(t * 16f);
                if (on[2]) v *= .5f + Mathf.PerlinNoise(t * 22f, k * 3.1f) * 1.4f;
                m.SetColor("_EmissionColor", c * v);
            }
            if (boardLight != null) { var any = 0; foreach (var b in on) if (b) any++; boardLight.intensity = 1.8f + any * .5f + (on[4] ? Mathf.Sin(t * 16f) * .6f : 0f) + (on[2] ? Mathf.PerlinNoise(t * 22f, 0f) * .8f : 0f); }
            foreach (var (m, c, ph) in panes) m.SetColor("_EmissionColor", c * (.7f + .3f * Mathf.Sin(t * .6f + ph)));
            foreach (var (m, c, ph) in flowers) m.SetColor("_EmissionColor", c * (1.4f + .6f * Mathf.Sin(t * .9f + ph)));
            var age = t - spun; var fast = age < 30f ? 1f - age / 30f : 0f;
            if (ball != null) ball.Rotate(0, (12f + fast * 70f) * Time.deltaTime, 0, Space.World); if (ballLight != null) ballLight.intensity = 2f + fast * 2f;
            foreach (var (m, c, x, z) in tiles) { var w = .5f + .5f * Mathf.Sin(t * (.7f + fast * 2f) - (x + z) * .8f); var fleck = fast * Mathf.Max(0f, Mathf.Sin(t * 3f + x * 2.1f + z * 3.7f)); m.SetColor("_EmissionColor", c * (.3f + w * .9f + fleck * 2f)); }
        }
    }
}
