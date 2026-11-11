using System.Collections.Generic;
using UnityEngine;

namespace EverythingWorld.World
{
    /// Radiohead's district: a block of concrete rooms on the moor, walked through like an exhibition, each room built
    /// round one song or record (evoked as a place, never a copy of the artwork). A 3 by 3 grid of 19 m rooms:
    ///
    ///     back:   rain room  ·  mist room  ·  aquarium         (Paranoid Android · How to Disappear Completely · Weird Fishes)
    ///     middle: platform   ·  the cubes  ·  jigsaw court     (Let Down · Everything In Its Right Place · Jigsaw Falling Into Place)
    ///     front:  deep water ·  STATION    ·  wind room        (The Bends, Creep · Videotape · Ill Wind, Man of War, Optimistic)
    ///
    /// You enter the station (a brutalist block with a tower, slit windows glowing signal orange, song titles changing
    /// on the walls inside and a wall of televisions full of static). From there a ring of doorways leads through every
    /// room and back; the station's back door opens onto the open courtyard of white cubes, and the mist room has a
    /// door out onto the moor. MoorsLife moves everything (the tram, the rain, the fish, the ribbons, the pieces).
    public partial class MoorsKit
    {
        Vector2 district2; bool hasDistrict; Vector3 districtFront = Vector3.forward;
        const float Cell = 19f, RoomH = 7f, WallT = .5f;
        public const string Creep = "70LcF31zb1H0PyJoS1Sx1r", TheBends = "7oDFvnqXkXuiZa1sACXobj", LetDown = "2fuYa3Lx06QQJAm0MjztKr", Paranoid = "6LgJvl0Xdtc73RJ1mmpotq",
            Jigsaw = "0YJ9FWWHn9EfnN0lHwbzvV", IllWind = "7vuVUQV0dDnjXUyLPzJLPi", ManOfWar = "5HtNvKihacAXt34DDYYxBC", Optimistic = "1FoUsSi9BCTlNt2Vd7V8XA";

        /// Called from ShapeGround before the artists are placed: a flat-ish open spot near the arrival for the district.
        void PickDistrict(IslandWorldData data, int index, List<Vector3> avoid)
        {
            var isl = data.layout.islands[index]; hasDistrict = false;
            bool Inside(Vector2 p, float m) { if (data.IslandAt(p.x, p.y) != isl) return false; for (var k = 0; k < 16; k++) { var a = k * Mathf.PI / 8f; if (data.IslandAt(p.x + Mathf.Cos(a) * m, p.y + Mathf.Sin(a) * m) != isl) return false; } return true; }
            var best = float.MaxValue;
            for (var r = 80f; r <= 170f; r += 10f)
                for (var k = 0; k < 24; k++)
                {
                    var a = k * Mathf.PI / 12f; var p = heart2 + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r;
                    if (!Inside(p, 64f)) continue;
                    var clear = true; foreach (var v in avoid) if (Vector2.Distance(p, new Vector2(v.x, v.z)) < v.y + 52f) clear = false;
                    if (!clear) continue;
                    float lo = float.MaxValue, hi = float.MinValue;
                    for (var s = 0; s < 16; s++) for (var rr = 0f; rr <= 44f; rr += 11f) { var h = data.HeightAt(p.x + Mathf.Cos(s * Mathf.PI / 8f) * rr, p.y + Mathf.Sin(s * Mathf.PI / 8f) * rr); lo = Mathf.Min(lo, h); hi = Mathf.Max(hi, h); }
                    var score = (hi - lo) + r * .03f;
                    if (score < best) { best = score; district2 = p; hasDistrict = true; }
                }
            if (hasDistrict) avoid.Add(new Vector3(district2.x, 46f, district2.y));
        }

        Vector3 dc, dF, dS; Quaternion dRot; Plot dz;
        Vector3 D(float x, float y, float f) => dc + dS * x + Vector3.up * y + dF * f;
        Vector3 DistrictDoor => hasDistrict ? new Vector3(district2.x, 0, district2.y) + districtFront * (Cell * 1.5f + 3f) : Vector3.zero;

        void District(Plot z)
        {
            dz = z; dc = z.c; dF = z.front; dS = z.side; dRot = Quaternion.LookRotation(dF);
            var keep = concrete; concrete = WorldMaterials.Solid(new Color(.3f, .3f, .3f), .15f);
            const float C = Cell;
            // floors, one per room
            var floors = new Dictionary<(int, int), Material>
            {
                [(0, 1)] = slate, [(-1, 1)] = WorldMaterials.Solid(new Color(.05f, .12f, .26f), .9f), [(-1, 0)] = WorldMaterials.Solid(new Color(.66f, .66f, .64f)),
                [(-1, -1)] = WorldMaterials.Solid(new Color(.03f, .03f, .04f), .95f), [(0, -1)] = WorldMaterials.Solid(new Color(.93f, .93f, .92f)), [(1, -1)] = WorldMaterials.Solid(new Color(.06f, .07f, .09f), .8f),
                [(1, 0)] = WorldMaterials.Solid(new Color(.8f, .79f, .75f)), [(1, 1)] = WorldMaterials.Solid(new Color(.55f, .5f, .44f)), [(0, 0)] = WorldMaterials.Solid(new Color(.84f, .84f, .82f)),
            };
            foreach (var kv in floors)
            {
                var (i, j) = kv.Key;
                if (i == -1 && j == 0) continue;   // the platform lays its own floor (a raised platform and a lower track bed)
                Floor(kv.Value, i * C, j * C, C, C, .12f);
            }
            // the walls: four lines each way, three segments on each line; 0 solid, 1 doorway, 2 wide doorway, 3 window
            int[,] sideKinds = { { 0, 0, 0 }, { 1, 0, 1 }, { 1, 0, 1 }, { 0, 0, 3 } };      // [line x = -1.5C, -.5C, .5C, 1.5C][row j = -1, 0, 1]
            int[,] frontKinds = { { 0, 1, 0 }, { 1, 1, 1 }, { 1, 1, 1 }, { 0, 2, 0 } };     // [line f = -1.5C, -.5C, .5C, 1.5C][column i = -1, 0, 1]
            for (var l = 0; l < 4; l++)
                for (var s = 0; s < 3; s++)
                {
                    var x = (l - 1.5f) * C; var f0 = (s - 1.5f) * C; var f1 = (s - .5f) * C;
                    WallSeg(D(x, 0, f0), D(x, 0, f1), sideKinds[l, s], l == 0 ? -dS : l == 3 ? dS : Vector3.zero);
                    var ff = (l - 1.5f) * C; var x0 = (s - 1.5f) * C; var x1 = (s - .5f) * C;
                    WallSeg(D(x0, 0, ff), D(x1, 0, ff), frontKinds[l, s], l == 0 ? -dF : l == 3 ? dF : Vector3.zero);
                }
            // roofs over every room but the two open courts
            for (var i = -1; i <= 1; i++)
                for (var j = -1; j <= 1; j++)
                {
                    if ((i == 0 && j == 0) || (i == 1 && j == 0)) continue;
                    var rp = D(i * C, RoomH + .2f, j * C); var size = new Vector3(C + WallT, .4f, C + WallT);
                    z.mk.Box(concrete, rp, size, dRot); z.solid.Box(concrete, rp, size, dRot);
                    IndoorSpaces.Add(D(i * C, RoomH / 2f, j * C), dRot, new Vector3(C - WallT, RoomH, C - WallT));
                }
            Station(); DeepWater(); Platform(); RainRoom(); MistRoom(); Aquarium(); JigsawCourt(); WindRoom(); CubesCourt();
            concrete = keep;
            noGrass.Add(new Vector3(dc.x, C * 1.5f * 1.45f, dc.z));
        }

        void Floor(Material m, float x, float f, float w, float d, float top, float bottom = -.4f)
        {
            var c = D(x, (top + bottom) / 2f, f); var size = new Vector3(w, top - bottom, d);
            dz.mk.Box(m, c, size, dRot); dz.solid.Box(m, c, size, dRot);
        }

        /// One wall segment from a to b (extended half a wall at each end so corners close), with a doorway or a window
        /// in its middle; outer walls get rows of slit windows glowing signal orange on their outside.
        void WallSeg(Vector3 a, Vector3 b, int kind, Vector3 outward)
        {
            var up = Vector3.up; var u = b - a; u.y = 0; var L = u.magnitude; u /= L; a -= u * (WallT / 2f); L += WallT; var rot = Quaternion.LookRotation(u);
            const float y0 = -.85f;
            void Piece(float s0, float s1, float p0, float p1)
            {
                if (s1 - s0 < .05f || p1 - p0 < .05f) return;
                var m = a + u * ((s0 + s1) / 2f) + up * ((p0 + p1) / 2f); var size = new Vector3(WallT, p1 - p0, s1 - s0);
                dz.mk.Box(concrete, m, size, rot); dz.solid.Box(concrete, m, size, rot);
            }
            float g0 = 0f, g1 = 0f;
            if (kind == 0) Piece(0, L, y0, RoomH);
            else if (kind == 3)
            {
                Piece(0, L, y0, 1f); Piece(0, L, 5.2f, RoomH); Piece(0, 1.6f, 1f, 5.2f); Piece(L - 1.6f, L, 1f, 5.2f);
                var gm = a + u * (L / 2f) + up * 3.1f;
                Glass(dz.root, gm, new Vector3(.08f, 4.2f, L - 3.2f), new Color(.6f, .74f, .8f, .18f), rot);
                for (var k = 1; k < 4; k++) dz.mk.Box(steel, a + u * (1.6f + k * (L - 3.2f) / 4f) + up * 3.1f, new Vector3(.12f, 4.2f, .12f), rot);
                dz.solid.Box(steel, gm, new Vector3(.3f, 4.2f, L - 3.2f), rot);
                g0 = 1.6f; g1 = L - 1.6f;
            }
            else
            {
                var dw = kind == 2 ? 5f : 2.6f; var dh = kind == 2 ? 4.4f : 3.2f; var s0 = L / 2f - dw / 2f; var s1 = L / 2f + dw / 2f;
                Piece(0, s0, y0, RoomH); Piece(s1, L, y0, RoomH); Piece(s0, s1, dh, RoomH);
                g0 = s0; g1 = s1;
            }
            if (outward == Vector3.zero) return;
            // the slit windows on the outside
            var glow = GlowM(Signal, 1.8f);
            for (var s = 1.6f; s < L - 1.6f; s += 2.6f)
            {
                var row = Mathf.RoundToInt(s / 2.6f) % 2;
                if (g1 > g0 && s > g0 - 1f && s < g1 + 1f && row == 0) continue;
                dz.mk.Box(glow, a + u * s + up * (row == 0 ? 4.4f : 5.5f) + outward * (WallT / 2f + .03f), new Vector3(.04f, .22f, 1.5f), rot);
            }
        }

        void RoomSign(string text, Vector3 at, Vector3 facing)
        {
            var w = text.Length * .12f + .5f; var rot = Quaternion.LookRotation(facing);
            dz.mk.Box(dark, at, new Vector3(w, .5f, .04f), rot);
            ArtistBits.Label(dz.labels, text, at + facing * .03f, facing, .14f, Ink, TextAnchor.MiddleCenter);
        }

        void RoomMark(string name, float x, float f, float lookX, float lookF)
        {
            var at = D(x, .12f, f); var look = D(lookX, .12f, lookF) - at; look.y = 0;
            var mark = new GameObject("Shot: " + name).transform; mark.SetParent(dz.ctx.Root, false); mark.SetPositionAndRotation(at, Quaternion.LookRotation(look.normalized));
        }

        Light RoomLight(float x, float y, float f, Color c, float range, float intensity) => ArtistBits.PointLight(dz.root, D(x, y, f), c, range, intensity, true);

        // ---------------------------------------------------------------- the station (front middle)
        void Station()
        {
            const float C = Cell; var up = Vector3.up;
            // the tower over the station, set back from the front, with its name, a red ticker and slit windows
            var tc = D(0, RoomH + .4f + 5.5f, C - 1f); var tsize = new Vector3(C - 3f, 11f, C - 5f);
            dz.mk.Box(concrete, tc, tsize, dRot); dz.solid.Box(concrete, tc, tsize, dRot);
            var tf = C - 1f + tsize.z / 2f;
            ArtistBits.Label(dz.labels, "RADIOHEAD", D(0, 15.8f, tf + .05f), dF, 1.1f, Ink, TextAnchor.MiddleCenter);
            dz.mk.Box(SongGlow(TwoPlusTwo, new Color(1f, .12f, .08f), 2.6f), D(0, 14.3f, tf + .03f), new Vector3(12f, .35f, .04f), dRot);   // the red ticker blazes for 2 + 2 = 5
            halos.Add(D(0, 14.3f, tf + .6f), 4f, new Color(1f, .15f, .1f), .4f);
            var glow = GlowM(Signal, 1.8f);
            for (var y = 9f; y < 13.6f; y += 1.5f)
                for (var x = -6.4f; x <= 6.4f; x += 1.6f)
                    if (((int)(x * 10f) + (int)(y * 10f)) % 3 != 0) dz.mk.Box(glow, D(x, y, tf + .03f), new Vector3(1.1f, .22f, .04f), dRot);
            foreach (var s in new[] { -1f, 1f })
                for (var y = 9f; y < 17.6f; y += 1.5f)
                    for (var f = C - 6f; f <= C + 4f; f += 2.2f) dz.mk.Box(glow, D(s * (tsize.x / 2f + .03f), y, f), new Vector3(.04f, .22f, 1.4f), dRot);
            // on the tower roof: dishes and a mast with a warning light
            var roof = RoomH + .4f + 11f;
            for (var k = 0; k < 3; k++) { var dp = D(-5f + k * 4f, roof, C - 4f); dz.mk.Prism(steel, dp, up, .1f, 1.4f, 6); dz.mk.Prism(white, dp + up * 1.9f, (dF + up).normalized, 1.5f, .2f, 16, .35f); }
            var mast = D(5.5f, roof, C + 2f); dz.mk.Prism(steel, mast, up, .2f, 18f, 6); life.warning.Add(mast + up * 18.2f);
            // the canopy over the doorway, and the forecourt
            dz.mk.Box(concrete, D(0, 4.9f, C * 1.5f + 2.5f), new Vector3(9f, .45f, 5f), dRot);
            dz.mk.Box(GlowM(new Color(1f, .5f, .35f), 1.2f), D(0, 4.66f, C * 1.5f + 2.5f), new Vector3(7f, .02f, .25f), dRot);
            Floor(WorldMaterials.Solid(new Color(.5f, .5f, .49f)), 0, C * 1.5f + 6f, 22f, 11f, .06f, -.3f);
            foreach (var s in new[] { -1f, 1f }) StreetLamp(dz.ctx, dz.mk, D(s * 9f, .06f, C * 1.5f + 9f));
            halos.Add(D(0, 3f, C * 1.5f + 1.5f), 4f, new Color(1f, .5f, .35f), .35f);
            // inside: song titles on the walls (changed by MoorsLife), a wall of televisions full of static
            var words = new List<TextMesh>();
            var wc = new Color(1f, .5f, .38f); var h = WallT / 2f + .03f;
            words.Add(ArtistBits.Label(dz.labels, Titles[0], D(-C / 2f + h, 5.1f, C), dS, .38f, wc, TextAnchor.MiddleCenter));
            words.Add(ArtistBits.Label(dz.labels, Titles[3], D(C / 2f - h, 5.1f, C), -dS, .38f, wc, TextAnchor.MiddleCenter));
            words.Add(ArtistBits.Label(dz.labels, Titles[6], D(0, 5.8f, C * 1.5f - h), -dF, .42f, wc, TextAnchor.MiddleCenter));
            words.Add(ArtistBits.Label(dz.labels, Titles[9], D(0, 4.6f, C / 2f + h), dF, .38f, new Color(.6f, .8f, 1f), TextAnchor.MiddleCenter));
            life.words = words;
            var tv = new Material(WorldMaterials.Glow(new Color(.75f, .8f, .85f), .8f)) { name = "Television static" }; life.static_ = tv;
            var cabinet = WorldMaterials.Solid(new Color(.18f, .16f, .14f));
            foreach (var side in new[] { -1f, 1f })
            {
                for (var k = 0; k < 3; k++) for (var row = 0; row < 3; row++)
                {
                    var tp = D(side * (3.4f + k * 1.6f), .62f + row * 1.05f, C / 2f + WallT / 2f + .5f);
                    dz.mk.Box(cabinet, tp, new Vector3(1.5f, 1f, .9f), dRot);
                    dz.mk.Box(tv, tp + dF * .46f, new Vector3(1.15f, .74f, .02f), dRot);
                }
                dz.solid.Box(cabinet, D(side * 5f, 1.7f, C / 2f + WallT / 2f + .5f), new Vector3(5f, 3.3f, 1f), dRot);
            }
            RoomSign("VIDEOTAPE", D(-5f, 3.75f, C / 2f + WallT / 2f + 1f), dF);
            foreach (var s in new[] { -1f, 1f }) { var bp = D(s * 6f, .45f, C + 2f); dz.mk.Box(concrete, bp, new Vector3(1.2f, .5f, 5f), dRot); dz.solid.Box(concrete, bp, new Vector3(1.2f, .7f, 5f), dRot); }
            RoomLight(0, 5f, C, new Color(1f, .5f, .4f), 16f, 1.4f);
            RoomLight(0, 2f, C / 2f + 3f, new Color(.7f, .8f, 1f), 9f, 1.1f);
            RoomMark("the station", -7.5f, C * 1.5f - 2f, 2f, C / 2f);
            var mark = new GameObject("Shot: the district from outside").transform; mark.SetParent(dz.ctx.Root, false);
            var cam = D(16f, 0, C * 1.5f + 34f); cam = dz.ctx.OnGround(cam.x, cam.z); var look = D(0, 8f, C) - cam; mark.SetPositionAndRotation(cam, Quaternion.LookRotation(new Vector3(look.x, 0, look.z).normalized));
        }

        // ---------------------------------------------------------------- the deep water room (front left): The Bends, Creep
        void DeepWater()
        {
            const float C = Cell; var up = Vector3.up; float cx = -C, cf = C;
            dz.mk.Box(GlowM(new Color(.12f, .4f, .75f), .9f), D(cx, RoomH - .08f, cf), new Vector3(C - WallT - .2f, .06f, C - WallT - .2f), dRot);
            // light rippling on the ceiling as if from the surface far above: two sets of strips that MoorsLife slides
            var ripple = GlowM(new Color(.55f, .9f, 1f), 1.3f);
            for (var g = 0; g < 2; g++)
            {
                var mk = new MeshKit();
                for (var k = 0; k < 7; k++) mk.Box(ripple, new Vector3((k - 3f) * 2.1f + g * 1.05f, 0, 0), new Vector3(.28f, .03f, 16f), Quaternion.Euler(0, g == 0 ? 6f : -6f, 0));
                var t = mk.BuildSingle("Ceiling ripples", life.transform, false).transform; var at = D(cx, RoomH - .16f - g * .02f, cf); t.SetPositionAndRotation(at, dRot);
                life.ripples.Add(t); life.rippleBase.Add(at);
            }
            life.rippleSide = dS;
            // a small round stage with a lone microphone in a spotlight
            var stage = D(cx, .12f, cf - 2f); var wood2 = WorldMaterials.Solid(new Color(.2f, .16f, .14f), .5f);
            dz.mk.Prism(wood2, stage, up, 1.9f, .3f, 20); dz.solid.Prism(wood2, stage, up, 1.9f, .3f, 12);
            var micBase = stage + up * .3f; dz.mk.Prism(steel, micBase, up, .25f, .04f, 10); dz.mk.Prism(steel, micBase, up, .03f, 1.55f, 6);
            dz.mk.Box(steel, micBase + up * 1.5f + dF * .12f, new Vector3(.03f, .03f, .3f), Quaternion.LookRotation(dF + up * .4f)); dz.mk.Blob(dark, micBase + up * 1.6f + dF * .28f, new Vector3(.06f, .06f, .1f), dz.ctx.Rng, 0f);
            life.spotAt = stage + up * (RoomH - .4f); life.spotFloor = stage + up * .3f;
            life.bubbles = D(cx, .2f, cf);
            RoomLight(cx, 4f, cf, new Color(.3f, .6f, 1f), 15f, 1.3f);
            RoomSign("THE BENDS  ·  CREEP", D(-C / 2f - WallT / 2f - .03f, 2.3f, cf + 2.6f), -dS);
            RoomMark("the deep water room", -C / 2f - 2f, cf + 6.5f, cx, cf - 2f);
        }

        // ---------------------------------------------------------------- the platform (middle left): Let Down
        void Platform()
        {
            const float C = Cell; var up = Vector3.up; float cx = -C, cf = 0f;
            const float edge = -24f;
            Floor(WorldMaterials.Solid(new Color(.22f, .21f, .2f)), (-C * 1.5f + edge) / 2f, cf, edge + C * 1.5f, C, -.38f, -.8f);
            Floor(WorldMaterials.Solid(new Color(.66f, .66f, .64f)), (edge - C / 2f) / 2f, cf, -C / 2f - edge, C, .12f);
            dz.mk.Box(WorldMaterials.Solid(new Color(1f, .82f, .1f)), D(edge + .25f, .13f, cf), new Vector3(.3f, .04f, C - WallT), dRot);
            // the track: rails and sleepers, tunnel mouths at both ends
            var track = -26.1f;
            foreach (var s in new[] { -.72f, .72f }) dz.mk.Box(steel, D(track + s, -.3f, cf), new Vector3(.12f, .16f, C - WallT), dRot);
            for (var f = -8.5f; f <= 8.5f; f += 1f) dz.mk.Box(wood, D(track, -.36f, f), new Vector3(2.4f, .06f, .3f), dRot);
            var black = WorldMaterials.Solid(new Color(.01f, .01f, .012f));
            foreach (var s in new[] { -1f, 1f })
            {
                var wf = s * (C / 2f - WallT / 2f - .03f);
                dz.mk.Box(black, D(track, 1.6f, wf), new Vector3(3.8f, 4f, .04f), dRot);
                dz.mk.Box(concrete, D(track, 3.8f, wf - s * .1f), new Vector3(4.6f, .5f, .2f), dRot);
                foreach (var e in new[] { -2.15f, 2.15f }) dz.mk.Box(concrete, D(track + e, 1.6f, wf - s * .1f), new Vector3(.35f, 4.4f, .2f), dRot);
            }
            life.trackA = D(track, -.3f, C / 2f - WallT / 2f); life.trackB = D(track, -.3f, -C / 2f + WallT / 2f); life.trackRot = Quaternion.LookRotation(-dF);
            // benches, the departure board and a clock, strip lights
            foreach (var f in new[] { -4.5f, 4.5f })
            {
                var bp = D(-14f, .12f, f); dz.mk.Box(wood, bp + up * .45f, new Vector3(.6f, .08f, 2.4f), dRot); dz.mk.Box(wood, bp + up * .8f + dS * .28f, new Vector3(.08f, .6f, 2.4f), dRot);
                foreach (var e in new[] { -1f, 1f }) dz.mk.Box(steel, bp + up * .22f + dF * e, new Vector3(.5f, .44f, .08f), dRot);
                dz.solid.Box(wood, bp + up * .5f, new Vector3(.7f, 1f, 2.4f), dRot);
            }
            var board = D(-21f, 4.6f, 0f); dz.mk.Box(dark, board, new Vector3(.15f, .8f, 4.2f), dRot);
            foreach (var e in new[] { -1.6f, 1.6f }) dz.mk.Box(steel, board + up * 1.2f + dF * e, new Vector3(.05f, 1.6f, .05f), dRot);
            ArtistBits.Label(dz.labels, "NEXT:  LET DOWN", board + dS * .09f, dS, .26f, new Color(1f, .6f, .15f), TextAnchor.MiddleCenter);
            var clock = D(-21f, 4.6f, 4.4f); dz.mk.Prism(white, clock - dS * .05f, dS, .45f, .1f, 20); dz.mk.Box(dark, clock + dS * .06f + up * .15f, new Vector3(.02f, .3f, .04f), dRot); dz.mk.Box(dark, clock + dS * .06f + dF * .12f, new Vector3(.02f, .04f, .24f), dRot);
            for (var x = -22f; x <= -12f; x += 5f) dz.mk.Box(GlowM(new Color(.9f, .97f, 1f), 1.6f), D(x, RoomH - .1f, cf), new Vector3(.2f, .06f, 9f), dRot);
            RoomLight(-17f, 4f, cf, new Color(.85f, .95f, 1f), 16f, 1.2f);
            RoomSign("LET DOWN", D(cx + 2.6f, 2.3f, C / 2f - WallT / 2f - .03f), -dF);
            RoomMark("the platform", -11.5f, 7f, -24f, -3f);
        }

        // ---------------------------------------------------------------- the rain room (back left): Paranoid Android
        void RainRoom()
        {
            const float C = Cell; var up = Vector3.up; float cx = -C, cf = -C;
            var red = new Material(WorldMaterials.Glow(new Color(1f, .15f, .1f), 1f)) { name = "Rain room neon" }; life.redNeon = red;
            for (var k = 0; k < 28; k++) { var a0 = k * Mathf.PI * 2f / 28f; var a1 = (k + 1) * Mathf.PI * 2f / 28f; var p0 = D(cx + Mathf.Cos(a0) * 5f, RoomH - .2f, cf + Mathf.Sin(a0) * 5f); var p1 = D(cx + Mathf.Cos(a1) * 5f, RoomH - .2f, cf + Mathf.Sin(a1) * 5f); dz.mk.Box(red, (p0 + p1) / 2f, new Vector3(.18f, .1f, Vector3.Distance(p0, p1) + .02f), Quaternion.LookRotation(p1 - p0)); }
            var col = new Material(WorldMaterials.Glow(new Color(1f, .3f, .22f), .5f)) { name = "Rain room columns" }; life.rainColumns = col;
            for (var k = 0; k < 6; k++) { var a = k * 60f * Mathf.Deg2Rad; var p = D(cx + Mathf.Cos(a) * 2.6f, .12f, cf + Mathf.Sin(a) * 2.6f); dz.mk.Prism(col, p, up, .12f, 6.2f, 8); ArtistBits.PostCollider(dz.root, p, 6f, .15f); }
            // a lone chair in the middle, getting rained on
            var ch = D(cx, .12f, cf); var chair = WorldMaterials.Solid(new Color(.15f, .12f, .1f), .4f);
            dz.mk.Box(chair, ch + up * .48f, new Vector3(.5f, .06f, .5f), dRot); dz.mk.Box(chair, ch + up * .8f - dF * .22f, new Vector3(.5f, .6f, .06f), dRot);
            foreach (var s in new[] { -.21f, .21f }) foreach (var f in new[] { -.21f, .21f }) dz.mk.Box(chair, ch + up * .24f + dS * s + dF * f, new Vector3(.05f, .48f, .05f), dRot);
            dz.solid.Box(chair, ch + up * .5f, new Vector3(.55f, 1f, .55f), dRot);
            life.rainAt = D(cx, RoomH - .3f, cf);
            RoomLight(cx, 4f, cf, new Color(1f, .22f, .16f), 14f, 1.1f);
            RoomSign("PARANOID ANDROID", D(cx + 2.6f, 2.3f, -C / 2f - WallT / 2f - .03f), -dF);
            RoomMark("the rain room", cx + 6.5f, cf + 6.5f, cx - 1f, cf - 1f);
        }

        // ---------------------------------------------------------------- the mist room (back middle): How to Disappear Completely
        void MistRoom()
        {
            const float C = Cell; var up = Vector3.up; float cx = 0f, cf = -C;
            dz.mk.Box(GlowM(new Color(.95f, .96f, 1f), .5f), D(cx, RoomH - .08f, cf), new Vector3(C - WallT - .2f, .06f, C - WallT - .2f), dRot);
            var whiteWall = WorldMaterials.Solid(new Color(.9f, .9f, .9f));
            foreach (var s in new[] { -1f, 1f })
            {   // panels on the inner faces, either side of each doorway (no panel crosses a door)
                var x = s * (C / 2f - WallT / 2f - .03f); foreach (var e in new[] { -1f, 1f }) dz.mk.Box(whiteWall, D(x, 3.5f, cf + e * 5.3f), new Vector3(.04f, 6.8f, 7.6f), dRot);
                var f = cf + s * (C / 2f - WallT / 2f - .03f); foreach (var e in new[] { -1f, 1f }) dz.mk.Box(whiteWall, D(cx + e * 5.3f, 3.5f, f), new Vector3(7.6f, 6.8f, .04f), dRot);
            }
            for (var k = 0; k < 8; k++) { var a = k * 45f * Mathf.Deg2Rad; dz.mk.Box(GlowM(new Color(.9f, .95f, 1f), 2f), D(cx + Mathf.Cos(a) * 6f, .16f, cf + Mathf.Sin(a) * 6f), new Vector3(.5f, .08f, .5f), dRot); }
            var fig = new MeshKit(); var pale = new Material(WorldMaterials.Glow(new Color(.85f, .9f, 1f), .9f)) { name = "The figure in the mist" };
            fig.Box(pale, new Vector3(0, .9f, 0), new Vector3(.45f, 1.1f, .25f), Quaternion.identity); fig.Blob(pale, new Vector3(0, 1.7f, 0), Vector3.one * .17f, dz.ctx.Rng, 0f);
            foreach (var s in new[] { -.13f, .13f }) fig.Box(pale, new Vector3(s, .2f, 0), new Vector3(.14f, .5f, .14f), Quaternion.identity);
            var ft = fig.BuildSingle("The figure in the mist", life.transform, false).transform; ft.SetPositionAndRotation(D(cx, .12f, cf), Quaternion.LookRotation(dF)); life.mistFigure = ft; life.mistFigureM = pale;
            life.mistAt = D(cx, .2f, cf);
            RoomLight(cx, 4.5f, cf, new Color(.92f, .95f, 1f), 15f, 1.3f);
            RoomSign("HOW TO DISAPPEAR COMPLETELY", D(-C / 2f + WallT / 2f + .03f, 2.3f, cf + 2.9f), dS);
            RoomMark("the mist room", -C / 2f + 2f, cf + 6.5f, cx, cf);
        }

        // ---------------------------------------------------------------- the aquarium (back right): Weird Fishes / Arpeggi
        void Aquarium()
        {
            const float C = Cell; var up = Vector3.up; float cx = C, cf = -C;
            var cab = WorldMaterials.Solid(new Color(.08f, .08f, .1f)); var back = GlowM(new Color(.1f, .3f, .55f), .8f);
            void Tank(Vector3 c, Vector3 along, float len, float depth, Vector3 toWall)
            {
                var rot = Quaternion.LookRotation(along); var acr = Vector3.Cross(up, along);
                dz.mk.Box(cab, c + up * .52f, new Vector3(depth + .2f, .8f, len + .2f), rot);
                dz.mk.Box(back, c + up * 2.25f + toWall * (depth / 2f - .05f), new Vector3(.06f, 2.5f, len - .1f), Quaternion.LookRotation(along));
                Glass(dz.root, c + up * 2.25f, new Vector3(depth, 2.6f, len), new Color(.1f, .35f, .55f, .28f), rot);
                dz.solid.Box(cab, c + up * 1.6f, new Vector3(depth + .2f, 3f, len + .2f), rot);
                life.tanks.Add((c + up * 2.25f, along, acr, new Vector3(depth / 2f - .25f, 1.05f, len / 2f - .4f)));
            }
            Tank(D(C * 1.5f - WallT / 2f - 1f, .12f, cf), dF, 13f, 1.6f, dS);
            Tank(D(cx, .12f, -C * 1.5f + WallT / 2f + 1f), dS, 13f, 1.6f, -dF);
            // a trail of floor lights that play an arpeggio, from the mist room door to the jigsaw court door
            for (var k = 0; k < 6; k++) life.arp.Add(new Material(WorldMaterials.Glow(new Color(.3f, .8f, 1f), .2f)) { name = "Arpeggio " + k });
            var steps = new List<Vector3>();
            for (var k = 0; k < 8; k++) steps.Add(D(C / 2f + 1.5f + k * 1.1f, .135f, cf));
            for (var k = 0; k < 8; k++) steps.Add(D(C + .6f, .135f, cf + 1.4f + k * 1.1f));
            for (var k = 0; k < steps.Count; k++) dz.mk.Box(life.arp[k % 6], steps[k], new Vector3(.7f, .03f, .7f), dRot);
            RoomLight(cx, 3.5f, cf, new Color(.3f, .6f, 1f), 12f, .9f);
            RoomSign("WEIRD FISHES / ARPEGGI", D(C / 2f + WallT / 2f + .03f, 2.3f, cf + 2.9f), dS);
            RoomMark("the aquarium", C / 2f + 2f, cf + 6.5f, C * 1.5f, cf - 4f);
        }

        // ---------------------------------------------------------------- the jigsaw court (middle right, open): Jigsaw Falling Into Place
        void JigsawCourt()
        {
            const float C = Cell; var up = Vector3.up; float cx = C, cf = 0f;
            Color[] cols = { new Color(.9f, .15f, .12f), new Color(1f, .5f, .1f), new Color(1f, .85f, .15f), new Color(.2f, .7f, .3f), new Color(.15f, .45f, .9f), new Color(.5f, .25f, .75f), new Color(.92f, .92f, .9f) };
            var wx = C * 1.5f - WallT / 2f - .4f;
            dz.mk.Box(dark, D(wx + .15f, 3.55f, cf), new Vector3(.1f, 6.3f, 12.3f), dRot);
            var missing = new HashSet<(int, int)> { (1, 0), (3, 1), (4, 2), (0, 2) };
            var pieceRot = Quaternion.LookRotation(-dS);
            for (var i = 0; i < 6; i++) for (var j = 0; j < 3; j++)
            {
                var home = D(wx, .5f + 1f + j * 2f, cf - 5f + i * 2f); var m = WorldMaterials.Solid(cols[(i * 2 + j * 3) % cols.Length], .3f);
                if (missing.Contains((i, j)))
                {
                    var pk = new MeshKit(); Piece(pk, m, Vector3.zero, Quaternion.identity, i + j);
                    var t = pk.BuildSingle("Jigsaw piece", life.transform, false).transform;
                    var away = home - dS * (2.2f + (i % 2) * 1.4f) + up * ((j - 1f) * .9f + .6f) + dF * ((i % 3) - 1f) * .8f;
                    t.SetPositionAndRotation(away, pieceRot);
                    life.pieces.Add(t); life.pieceHome.Add(home); life.pieceAway.Add(away); life.pieceRot = pieceRot;
                    continue;
                }
                Piece(dz.mk, m, home, pieceRot, i + j);
            }
            dz.solid.Box(dark, D(wx, 3.5f, cf), new Vector3(.6f, 6.4f, 12.4f), dRot);
            // paint run down from the pieces across the floor
            for (var k = 0; k < 7; k++) dz.mk.Box(WorldMaterials.Solid(cols[k], .4f), D(wx - 3.6f, .14f, cf - 5.1f + k * 1.7f), new Vector3(6.6f - (k % 3) * 1.4f, .04f, 1.1f), dRot);
            var bp = D(cx - 3f, .12f, cf); dz.mk.Box(wood, bp + up * .45f, new Vector3(.7f, .1f, 3.6f), dRot); foreach (var e in new[] { -1.4f, 1.4f }) dz.mk.Box(concrete, bp + up * .2f + dF * e, new Vector3(.6f, .4f, .4f), dRot); dz.solid.Box(wood, bp + up * .3f, new Vector3(.7f, .6f, 3.6f), dRot);
            RoomLight(cx, 5f, cf, new Color(1f, .9f, .8f), 16f, 1f);
            RoomSign("JIGSAW FALLING INTO PLACE", D(cx + 3.2f, 2.3f, -C / 2f + WallT / 2f + .03f), dF);
            RoomMark("the jigsaw court", C / 2f + 2f, -6.5f, wx, 1f);
        }

        /// One jigsaw piece: a thick square with a knob on its right and a knob on top (or holes, every other piece).
        void Piece(MeshKit mk, Material m, Vector3 c, Quaternion rot, int parity)
        {
            var r = rot * Vector3.right; var u = rot * Vector3.up; var f = rot * Vector3.forward;
            mk.Box(m, c, new Vector3(1.86f, 1.86f, .3f), rot);
            if (parity % 2 == 0) { mk.Prism(m, c + r * .93f - f * .15f, f, .3f, .3f, 10); mk.Prism(m, c + u * .93f - f * .15f, f, .3f, .3f, 10); }
            else { mk.Prism(m, c - r * .93f - f * .15f, f, .3f, .3f, 10); mk.Prism(m, c - u * .93f - f * .15f, f, .3f, .3f, 10); }
        }

        // ---------------------------------------------------------------- the wind room (front right): Ill Wind, Man of War, Optimistic
        void WindRoom()
        {
            const float C = Cell; var up = Vector3.up; float cx = C, cf = C;
            // hanging white ribbons in clusters that sway (MoorsLife), walked through
            var ribbon = WorldMaterials.Solid(new Color(.95f, .95f, .93f), .3f);
            for (var g = 0; g < 6; g++)
            {
                var mk = new MeshKit(); var rng = new System.Random(500 + g);
                for (var k = 0; k < 22; k++) { var len = 2.6f + (float)rng.NextDouble() * 1.4f; mk.Box(ribbon, new Vector3(((float)rng.NextDouble() - .5f) * 3f, -len / 2f, ((float)rng.NextDouble() - .5f) * 3f), new Vector3(.16f, len, .015f), Quaternion.Euler(0, (float)rng.NextDouble() * 180f, 0)); }
                var t = mk.BuildSingle("Soft ribbons", life.transform, false).transform;
                var at = D(cx - 4.5f + (g % 3) * 4.5f, RoomH - .1f, cf - 3f + (g / 3) * 5f); t.SetPositionAndRotation(at, dRot);
                life.ribbons.Add(t);
            }
            life.windRot = dRot; life.windDir = dS;
            // a dinosaur skeleton in a glass case, a coat and hat on a stand
            var dino = D(cx + 5.5f, .12f, cf + 6.5f); var bone = WorldMaterials.Solid(new Color(.92f, .89f, .8f));
            dz.mk.Box(concrete, dino + up * .5f, new Vector3(3.6f, 1f, 1.6f), Quaternion.LookRotation(dS)); dz.solid.Box(concrete, dino + up * 1.3f, new Vector3(3.6f, 2.6f, 1.6f), Quaternion.LookRotation(dS));
            var sp = dino + up * 1.9f;
            for (var k = 0; k < 9; k++) { var p = sp + dS * (-1.2f + k * .28f) + up * Mathf.Sin(k * .4f) * .15f; dz.mk.Box(bone, p, new Vector3(.06f, .06f, .26f), Quaternion.LookRotation(dS)); if (k > 1 && k < 6) dz.mk.Box(bone, p - up * .2f, new Vector3(.4f, .35f, .03f), Quaternion.LookRotation(dS)); }
            dz.mk.Box(bone, sp + dS * 1.35f + up * .2f, new Vector3(.22f, .2f, .36f), Quaternion.LookRotation(dS + up * .3f));
            foreach (var s in new[] { -1f, 1f }) foreach (var e in new[] { -.6f, .5f }) dz.mk.Box(bone, sp + dS * e + dF * s * .15f - up * .45f, new Vector3(.05f, .75f, .05f), Quaternion.LookRotation(dS) * Quaternion.Euler(10f * s, 0, 0));
            Glass(dz.root, dino + up * 1.8f, new Vector3(3.6f, 1.6f, 1.6f), new Color(.8f, .9f, .95f, .12f), Quaternion.LookRotation(dS));
            var stand = D(cx - 6f, .12f, cf + 7f); dz.mk.Prism(wood, stand, up, .05f, 1.9f, 6); dz.mk.Prism(wood, stand, up, .3f, .05f, 8);
            var coat = WorldMaterials.Solid(new Color(.3f, .27f, .22f)); dz.mk.Box(coat, stand + up * 1.25f + dF * .12f, new Vector3(.55f, 1.1f, .2f), dRot);
            dz.mk.Prism(dark, stand + up * 1.9f, up, .32f, .04f, 14); dz.mk.Prism(dark, stand + up * 1.94f, up, .17f, .16f, 12, .85f);
            ArtistBits.PostCollider(dz.root, stand, 2f, .3f);
            RoomLight(cx, 4.5f, cf, new Color(1f, .97f, .92f), 15f, 1.1f);
            RoomSign("ILL WIND  ·  MAN OF WAR  ·  OPTIMISTIC", D(cx + 3.6f, 2.3f, C / 2f + WallT / 2f + .03f), dF);
            RoomMark("the wind room", C / 2f + 2f, cf - 6.5f, cx + 3f, cf + 4f);
        }

        // ---------------------------------------------------------------- the courtyard of cubes (middle, open): Everything In Its Right Place
        void CubesCourt()
        {
            var up = Vector3.up;
            var cubeM = new Material(WorldMaterials.Glow(new Color(.95f, .95f, .95f), .08f)) { name = "Right place cubes" }; life.cubeGlow = cubeM;
            for (var i = 0; i < 7; i++) for (var j = 0; j < 7; j++)
            {
                var p = D((i - 3f) * 2.3f, .12f + .6f, (j - 3f) * 2.3f);
                if (i == 4 && j == 2)
                {
                    var odd = new MeshKit(); odd.Box(cubeM, Vector3.zero, Vector3.one * 1.2f, Quaternion.identity);
                    var t = odd.BuildSingle("The cube out of place", life.transform, false).transform; life.oddCube = t; life.oddHome = p; life.oddRot = dRot; life.oddAway = p + dS * .8f - dF * .6f;
                    t.SetPositionAndRotation(life.oddAway, dRot * Quaternion.Euler(0, 23f, 0)); dz.solid.Box(white, p, Vector3.one * 1.6f, dRot); continue;
                }
                dz.mk.Box(cubeM, p, Vector3.one * 1.2f, dRot); dz.solid.Box(white, p, Vector3.one * 1.2f, dRot);
            }
            RoomLight(0, 6f, 0, new Color(1f, .95f, .9f), 18f, 1f);
            RoomSign("EVERYTHING IN ITS RIGHT PLACE", D(3.4f, 2.3f, Cell / 2f - WallT / 2f - .03f), -dF);
            var mark = new GameObject("Shot: the cubes").transform; mark.SetParent(dz.ctx.Root, false);
            var at = D(-8.7f, .12f, 8.7f); var look = D(6f, 0, -6f) - at; look.y = 0; mark.SetPositionAndRotation(at, Quaternion.LookRotation(look.normalized));
        }
    }
}
