using System.Collections.Generic;
using UnityEngine;

namespace EverythingWorld.World
{
    /// Radiohead on the moor. Their hall is a concrete transmitter station: a dark room inside where song titles glow
    /// and change on the walls, and a wall of old television sets full of static. Around the moor, things from their
    /// records, evoked as places (never copies of the artwork):
    /// - the cubes: a grid of white cubes on a pale square, perfectly in line, all but one;
    /// - the sunken pool: a flooded reservoir with a drowned village under the water, lit windows down there, and a
    ///   stepped stone pyramid rising out of the middle; glowing fish swim through the air above it;
    /// - the motorway: a white elevated motorway that curves out over the moor and stops in mid-air, blue signs on it;
    /// - the word city: a square of coloured blocks like a city map, words on the tops;
    /// - the ice peaks: a cluster of sharp icy peaks, glowing faintly;
    /// - the grove: gnarled old trees covered in glowing eyes, and a pale figure standing among them who isn't quite
    ///   there;
    /// - the lonely road: an old car on the longest road (driven by MoorsLife).
    public partial class MoorsKit
    {
        Vector2 pool2; bool hasPool; const float PoolR = 15f;
        static readonly string[] Titles = { "EVERYTHING IN ITS RIGHT PLACE", "KARMA POLICE", "LET DOWN", "PARANOID ANDROID", "PYRAMID SONG", "VIDEOTAPE", "OPTIMISTIC", "MAN OF WAR", "ILL WIND", "THE BENDS", "CREEP", "2 + 2 = 5", "WEIRD FISHES", "JIGSAW FALLING INTO PLACE", "HOW TO DISAPPEAR COMPLETELY" };

        /// Called from ShapeGround before the artists are placed: find a spot for the sunken pool and dig it.
        void PickPool(IslandWorldData data, int index, List<Vector3> avoid)
        {
            var isl = data.layout.islands[index]; hasPool = false;
            bool Inside(Vector2 p, float m) { if (data.IslandAt(p.x, p.y) != isl) return false; for (var k = 0; k < 12; k++) { var a = k * Mathf.PI / 6f; if (data.IslandAt(p.x + Mathf.Cos(a) * m, p.y + Mathf.Sin(a) * m) != isl) return false; } return true; }
            for (var r = 80f; r <= 220f && !hasPool; r += 20f)
                for (var k = 0; k < 16 && !hasPool; k++)
                {
                    var a = k * Mathf.PI / 8f + .2f; var p = heart2 + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r;
                    if (!Inside(p, PoolR + 30f) || (hasHill && Vector2.Distance(p, hill2) < 110f)) continue;
                    hasPool = true; pool2 = p;
                }
            if (!hasPool) return;
            var floor = data.HeightAt(pool2.x, pool2.y) - 5f;
            for (var r = 0; r < data.Nz; r++)
            {
                var z = data.layout.depth - r * data.Step; if (Mathf.Abs(z - pool2.y) > PoolR + 14f) continue;
                for (var col = 0; col < data.Nx; col++)
                {
                    var x = col * data.Step; if (Mathf.Abs(x - pool2.x) > PoolR + 14f) continue;
                    var i = r * data.Nx + col; if (data.islandIds[i] != index) continue;
                    var d = Mathf.Max(Mathf.Abs(x - pool2.x), Mathf.Abs(z - pool2.y));
                    if (d < PoolR) data.heights[i] = floor;
                    else if (d < PoolR + 14f) data.heights[i] = Mathf.Lerp(floor + 5f, data.heights[i], Mathf.SmoothStep(0, 1, (d - PoolR) / 14f));
                }
            }
            avoid.Add(new Vector3(pool2.x, PoolR * 1.5f + 14f, pool2.y));
        }

        // ---------------------------------------------------------------- the marvels
        void Marvels(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var spots = new List<Vector3>();
            Vector3 Spot(float clear, float near = 300f)
            {
                for (var tries = 0; tries < 800; tries++)
                {
                    var px = ctx.Range(ctx.MinX, ctx.MaxX); var pz = ctx.Range(ctx.MinZ, ctx.MaxZ);
                    var cl = tries > 500 ? clear * .7f : clear;
                    if (!ctx.IsLand(px, pz, cl + 6f) || !Open(ctx, px, pz, cl)) continue;
                    if (hasPool && Vector2.Distance(new Vector2(px, pz), pool2) < PoolR * 1.5f + cl + 10f) continue;
                    if (hasHill && Vector2.Distance(new Vector2(px, pz), hill2) < 30f + cl) continue;
                    var ok = Vector2.Distance(new Vector2(px, pz), new Vector2(center.x, center.z)) < near;
                    foreach (var s in spots) if ((new Vector2(s.x, s.z) - new Vector2(px, pz)).magnitude < cl + 30f) ok = false;
                    if (!ok) continue;
                    var p = ctx.OnGround(px, pz); spots.Add(p); noGrass.Add(new Vector3(p.x, cl * .9f, p.z)); return p;
                }
                return Vector3.positiveInfinity;
            }
            if (hasPool) SunkenPool(ctx, mk, solid);
            var road = Spot(18f); if (!float.IsInfinity(road.x)) Motorway(ctx, mk, solid, road);
            var city = Spot(13f); if (!float.IsInfinity(city.x)) WordCity(ctx, mk, solid, city);
            var peaks = Spot(16f, 400f); if (!float.IsInfinity(peaks.x)) IcePeaks(ctx, mk, solid, peaks);
            var grove = Spot(12f, 400f); if (!float.IsInfinity(grove.x)) EyedGrove(ctx, mk, solid, grove);
        }

        void Mark(LandContext ctx, string name, Vector3 target, float dist, float lift = .25f)
        {
            var back = new Vector3(target.x - center.x, 0, target.z - center.z); if (back.sqrMagnitude < 1f) back = Vector3.forward; back.Normalize();
            var cam = ctx.OnGround(target.x - back.x * dist, target.z - back.z * dist); var look = target - cam; look.y = 0;
            var mark = new GameObject("Shot: " + name).transform; mark.SetParent(ctx.Root, false); mark.SetPositionAndRotation(cam, Quaternion.LookRotation(look.normalized + Vector3.up * lift));
        }

        /// The sunken pool: a flooded square reservoir with a stone rim, a drowned village on its floor (cottages, a
        /// church spire, windows still lit), a stepped stone pyramid rising out of the middle.
        void SunkenPool(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            var up = Vector3.up; var c = ctx.OnGround(pool2.x, pool2.y); var waterY = c.y + 3.6f;
            // the rim
            foreach (var d in new[] { Vector3.forward, Vector3.back, Vector3.left, Vector3.right })
            {
                var sd = Vector3.Cross(up, d); var rp = new Vector3(pool2.x, 0, pool2.y) + d * (PoolR + .5f); rp.y = waterY + .2f;
                mk.Box(stone, rp, new Vector3(1.2f, 1f, PoolR * 2f + 2.2f), Quaternion.LookRotation(sd)); solid.Box(stone, rp + up * .3f, new Vector3(1.2f, 1.6f, PoolR * 2f + 2.2f), Quaternion.LookRotation(sd));
                var inner = new Vector3(pool2.x, 0, pool2.y) + d * PoolR; mk.Box(wallStone, new Vector3(inner.x, (waterY + c.y) / 2f, inner.z), new Vector3(.4f, waterY - c.y + .4f, PoolR * 2f), Quaternion.LookRotation(sd));
            }
            // the drowned village
            var windows = new Material(WorldMaterials.Glow(new Color(.5f, .9f, .85f), .6f)) { name = "Drowned windows" }; life.drowned = windows;
            for (var k = 0; k < 7; k++)
            {
                var a = k * 51f * Mathf.Deg2Rad + .3f; var r = ctx.Range(7f, 11.5f); var hp = c + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * r; var face = (c - hp); face.y = 0; face.Normalize(); var hrot = Quaternion.LookRotation(face);
                mk.Box(stone, hp + up * 1.1f, new Vector3(2.6f, 2.2f, 2f), hrot); mk.GableRoof(slate, hp + up * 2.2f, new Vector3(2.4f, 0, 3f), .9f, Quaternion.LookRotation(Vector3.Cross(up, face)));
                mk.Box(windows, hp + up * 1.2f + face * 1.01f, new Vector3(.5f, .5f, .02f), hrot);
            }
            var spire = c + new Vector3(-9f, 0, 8f); mk.Box(stone, spire + up * 2f, new Vector3(1.6f, 4f, 1.6f), Quaternion.identity); mk.Cone(slate, spire + up * 4f, 1.1f, 3f, 4, 45f);
            mk.Box(windows, spire + up * 3f + Vector3.forward * .81f, new Vector3(.4f, .7f, .02f), Quaternion.identity);
            // the pyramid, rising out of the water
            for (var k = 0; k < 9; k++) { var s = 10f - k * 1.1f; mk.Box(WorldMaterials.Solid(new Color(.62f, .6f, .54f)), c + up * (k * 1.6f + .8f), new Vector3(s, 1.6f, s), Quaternion.Euler(0, 45f, 0)); }
            mk.Box(GlowM(new Color(.6f, .95f, 1f), 2.4f), c + up * 14.6f, new Vector3(.9f, .5f, .9f), Quaternion.Euler(0, 45f, 0));
            solid.Box(stone, c + up * 7.2f, new Vector3(10f, 14.4f, 10f), Quaternion.Euler(0, 45f, 0));
            halos.Add(c + up * 15f, 2.4f, new Color(.6f, .95f, 1f), .8f);
            // the water: a glassy dark sheet you can see the village through
            var water = Glass(ctx.Root, new Vector3(pool2.x, waterY - .05f, pool2.y), new Vector3(PoolR * 2f, .1f, PoolR * 2f), new Color(.08f, .22f, .28f, .45f));
            life.water = water; life.pool = new Vector3(pool2.x, waterY, pool2.y);
            ArtistBits.PointLight(ctx.Root, c + up * 2f, new Color(.4f, .9f, .85f), 18f, 1.2f);
            { var back = new Vector3(pool2.x - center.x, 0, pool2.y - center.z).normalized; var cam = new Vector3(pool2.x, waterY + 1.2f, pool2.y) - back * (PoolR + .5f); var mark = new GameObject("Shot: the sunken pool").transform; mark.SetParent(ctx.Root, false); mark.SetPositionAndRotation(cam, Quaternion.LookRotation(back + Vector3.down * .25f)); }
        }

        /// The motorway: a white elevated carriageway on round piers that curves out over the moor and stops in mid-air;
        /// white barriers, lamp posts, blue signs with song titles.
        void Motorway(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 c)
        {
            var up = Vector3.up; var face = new Vector3(center.x - c.x, 0, center.z - c.z).normalized; var side = Vector3.Cross(up, face); var deckY = c.y + 7f;
            var pts = new List<Vector3>();
            for (var k = 0; k <= 16; k++) { var t = k / 16f; var a = Mathf.Lerp(-70f, 40f, t) * Mathf.Deg2Rad; var p = c + (side * Mathf.Sin(a) + face * (Mathf.Cos(a) - 1f)) * 22f; p.y = deckY + t * 3f; pts.Add(p); }
            var blue = GlowM(new Color(.15f, .35f, .85f), 1f);
            for (var k = 0; k + 1 < pts.Count; k++)
            {
                var a = pts[k]; var b = pts[k + 1]; var d = b - a; var sd = Vector3.Cross(up, new Vector3(d.x, 0, d.z).normalized); var rot = Quaternion.LookRotation(d);
                mk.Box(white, (a + b) / 2f, new Vector3(8f, .6f, d.magnitude + .05f), rot); mk.Box(WorldMaterials.Solid(new Color(.75f, .76f, .78f)), (a + b) / 2f + up * .36f, new Vector3(7.6f, .02f, d.magnitude + .05f), rot);
                foreach (var s in new[] { -1f, 1f }) mk.Box(white, (a + b) / 2f + sd * s * 3.9f + up * .7f, new Vector3(.25f, .8f, d.magnitude + .05f), rot);
                mk.Box(GlowM(new Color(1f, 1f, 1f), 1f), (a + b) / 2f + up * .4f, new Vector3(.15f, .02f, d.magnitude * .5f), rot);
                if (k % 3 == 0) { var g = ctx.Ground(a.x, a.z); mk.Prism(white, new Vector3(a.x, g - .5f, a.z), up, .8f, a.y - g + .2f, 12); solid.Prism(white, new Vector3(a.x, g - .5f, a.z), up, .8f, a.y - g + .2f, 8); }
                if (k % 4 == 2) { var lp = a + sd * 3.9f + up * .7f; mk.Prism(steel, lp, up, .07f, 4f, 6); mk.Box(GlowM(Lamp, 2.2f), lp + up * 4f - sd * .5f, new Vector3(.25f, .1f, .6f), rot); halos.Add(lp + up * 3.9f - sd * .5f, 1.4f, Lamp, .7f); }
            }
            // the end, broken off: a ragged lip of concrete and bent rebar
            var end = pts[pts.Count - 1]; var ed = (end - pts[pts.Count - 2]).normalized;
            for (var k = 0; k < 6; k++) mk.Box(steel, end + ed * .3f + Vector3.Cross(up, ed) * ((k - 2.5f) * 1.2f), new Vector3(.06f, .06f, 1.4f), Quaternion.LookRotation(ed + up * (k % 2 == 0 ? .3f : -.2f)));
            // blue signs over the carriageway
            for (var k = 0; k < 2; k++)
            {
                var at = pts[4 + k * 6]; var d = pts[5 + k * 6] - at; var sd = Vector3.Cross(up, new Vector3(d.x, 0, d.z).normalized); var rot = Quaternion.LookRotation(-d);
                foreach (var s in new[] { -1f, 1f }) mk.Box(steel, at + sd * s * 3.6f + up * 3f, new Vector3(.2f, 5.2f, .2f), rot);
                mk.Box(blue, at + up * 4.8f, new Vector3(6.6f, 1.6f, .1f), rot);
                ArtistBits.Label(ctx.Root, k == 0 ? "LET DOWN" : "PARANOID ANDROID", at + up * 4.8f - d.normalized * .07f, -d.normalized, .32f, Color.white, TextAnchor.MiddleCenter);
            }
            Mark(ctx, "the motorway", pts[8] - up * 7f, 34f, .2f);
        }

        /// The word city: a square of coloured blocks of every height laid out like a city map, a word on each top.
        void WordCity(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 c)
        {
            var up = Vector3.up; var face = new Vector3(center.x - c.x, 0, center.z - c.z).normalized; var rot = Quaternion.LookRotation(face); var side = Vector3.Cross(up, face);
            Color[] cols = { new Color(.95f, .45f, .15f), new Color(.9f, .15f, .15f), new Color(.15f, .4f, .85f), new Color(.95f, .8f, .2f), new Color(.25f, .65f, .35f), new Color(.9f, .9f, .85f), new Color(.1f, .1f, .12f) };
            string[] words = { "2 + 2 = 5", "GO TO SLEEP", "WHERE I END", "A WOLF", "SAIL TO THE MOON", "BACKDRIFTS", "THE GLOOM", "MYXOMATOSIS", "SCATTERBRAIN", "A PUNCHUP", "I WILL", "THERE THERE" };
            var w = 0;
            for (var i = 0; i < 5; i++) for (var j = 0; j < 5; j++)
            {
                if ((i == 2) || (j == 2)) continue;   // the streets
                var bw = ctx.Range(3.2f, 4.2f); var bd = ctx.Range(3.2f, 4.2f); var bh = ctx.Range(1.6f, 6.5f);
                var p = c + side * ((i - 2f) * 5f) + face * ((j - 2f) * 5f) + up * (bh / 2f);
                var m = WorldMaterials.Solid(cols[(i * 3 + j * 5) % cols.Length], .3f);
                mk.Box(m, p, new Vector3(bw, bh, bd), rot); solid.Box(m, p, new Vector3(bw, bh, bd), rot);
                var ci = (i * 3 + j * 5) % cols.Length; var ink = ci == 3 || ci == 5 ? new Color(.08f, .08f, .08f) : Color.white;
                if (w < words.Length) { var wf = j < 2 ? -face : face; var sd = i < 2 ? -side : side; var wd = (i + j) % 2 == 0 ? wf : sd; var depth = wd == wf ? bd : bw; var text = words[w++]; ArtistBits.Label(ctx.Root, text, p + wd * (depth / 2f + .03f) + up * Mathf.Min(bh * .2f, 1f), wd, Mathf.Min(.42f, 3.4f / (text.Length * .62f + 1f)), ink, TextAnchor.MiddleCenter); }
            }
            noGrass.Add(new Vector3(c.x, 20f, c.z));   // the whole square, corners too: no hawthorns through the blocks
            mk.Box(WorldMaterials.Solid(new Color(.85f, .84f, .8f)), c + up * .03f, new Vector3(25f, .1f, 25f), rot);
            mk.Box(WorldMaterials.Solid(new Color(.3f, .3f, .32f)), c + up * .1f, new Vector3(2.4f, .04f, 25f), rot); mk.Box(WorldMaterials.Solid(new Color(.3f, .3f, .32f)), c + up * .1f + side * 0f, new Vector3(25f, .04f, 2.4f), rot);
            ArtistBits.PointLight(ctx.Root, c + up * 7f, new Color(1f, .9f, .8f), 18f, 1f);
            Mark(ctx, "the word city", c + up * 1f, 22f, -.15f);
        }

        /// The ice peaks: a cluster of sharp, pale-blue icy peaks, faintly glowing (red while "Everything In Its Right
        /// Place" plays).
        void IcePeaks(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 c)
        {
            var up = Vector3.up; var ice = new Material(WorldMaterials.Glow(new Color(.75f, .88f, 1f), .2f)) { name = "Ice peaks" }; life.ice = ice;
            for (var k = 0; k < 8; k++)
            {
                var a = k * 45f * Mathf.Deg2Rad + ctx.Range(-.2f, .2f); var r = k == 0 ? 0f : ctx.Range(5f, 12f); var p = ctx.OnGround(c.x + Mathf.Cos(a) * r, c.z + Mathf.Sin(a) * r);
                var h = k == 0 ? 34f : ctx.Range(12f, 26f); var rad = h * ctx.Range(.18f, .26f);
                mk.Cone(ice, p - up * .5f, rad, h, 4, ctx.Range(0, 90f)); mk.Cone(WorldMaterials.Solid(new Color(.95f, .97f, 1f)), p + up * h * .62f, rad * .38f, h * .38f + .05f, 4, ctx.Range(0, 90f));
                solid.Prism(ice, p - up * .5f, up, rad * .6f, h * .5f, 4);
            }
            noGrass.Add(new Vector3(c.x, 20f, c.z));   // the whole cluster: no hawthorns through the spires
            halos.Add(c + up * 10f, 10f, new Color(.75f, .88f, 1f), .2f);
            Mark(ctx, "the ice peaks", c + up * 10f, 45f, .2f);
        }

        /// The grove: gnarled old trees with many glowing eyes on their trunks and limbs, and a pale figure standing
        /// among them who flickers in and out (MoorsLife).
        void EyedGrove(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 c)
        {
            var up = Vector3.up; var bark = WorldMaterials.Solid(new Color(.28f, .25f, .22f)); var leaf = WorldMaterials.Solid(new Color(.25f, .34f, .24f)); var eye = GlowM(new Color(.9f, 1f, .7f), 2.4f);
            for (var k = 0; k < 7; k++)
            {
                var a = k * 51f * Mathf.Deg2Rad; var p = ctx.OnGround(c.x + Mathf.Cos(a) * ctx.Range(5f, 9f), c.z + Mathf.Sin(a) * ctx.Range(5f, 9f)); var q = p - up * .3f; var s = ctx.Range(.9f, 1.3f);
                for (var b = 0; b < 4; b++) { var nd = (up + new Vector3(ctx.Range(-.6f, .6f), 0, ctx.Range(-.6f, .6f))).normalized; mk.Prism(bark, q, nd, (.55f - b * .1f) * s, 2.4f * s, 7, .8f); for (var e = 0; e < 3; e++) mk.Blob(eye, q + nd * ctx.Range(.4f, 2f) * s + new Vector3(ctx.Range(-.4f, .4f), 0, ctx.Range(-.4f, .4f)), Vector3.one * .07f, ctx.Rng, 0f); q += nd * 2.3f * s; }
                for (var l = 0; l < 4; l++) soft.Blob(leaf, q + new Vector3(ctx.Range(-2f, 2f), ctx.Range(0f, 1.2f), ctx.Range(-2f, 2f)), new Vector3(2f, 1.3f, 2f) * s, ctx.Rng, .35f);
                ArtistBits.PostCollider(ctx.Root, p, 4f, .5f);
            }
            var fig = new MeshKit(); var pale = new Material(WorldMaterials.Glow(new Color(.85f, .9f, 1f), .9f)) { name = "The figure" };
            fig.Box(pale, new Vector3(0, .9f, 0), new Vector3(.45f, 1.1f, .25f), Quaternion.identity); fig.Blob(pale, new Vector3(0, 1.7f, 0), Vector3.one * .17f, ctx.Rng, 0f);
            foreach (var s in new[] { -.13f, .13f }) fig.Box(pale, new Vector3(s, .2f, 0), new Vector3(.14f, .5f, .14f), Quaternion.identity);
            var ft = fig.BuildSingle("The figure", life.transform, false).transform; ft.SetPositionAndRotation(c, Quaternion.LookRotation(new Vector3(center.x - c.x, 0, center.z - c.z).normalized)); life.figure = ft; life.figureM = pale;
            halos.Add(c + up * 1.2f, 2f, new Color(.85f, .9f, 1f), .3f);
            Mark(ctx, "the grove", c + up * 2f, 18f, .05f);
        }
    }
}
