using System;
using System.Collections.Generic;
using UnityEngine;

namespace EverythingWorld.World
{
    /// Lanternwood's special places (WORLD_GUIDE.md and the owner's notes): a frozen lake you can walk on,
    /// the boathouse and pier for the biggest artist (a quiet place to sit alone), a treehouse with a fire
    /// pit for the next, snowmen that stand somewhere new each day, a fire circle to gather round, and a
    /// bench overlooking the sea.
    public partial class LanternwoodKit
    {
        // ---------------------------------------------------------------- the frozen lake
        bool hasLake;
        Vector3 lakeC;          // centre (y unused)
        float lakeR, lakeLevel, lakeP1, lakePhase2;
        string lakeHall;        // the artist whose boathouse stands on the shore

        /// The lake's shoreline radius in a given direction (a gently irregular circle).
        float LakeEdge(float angle) => lakeR * (1f + .16f * Mathf.Sin(3f * angle + lakeP1) + .09f * Mathf.Sin(5f * angle + lakePhase2));
        float LakeEdgeToward(Vector3 p) => LakeEdge(Mathf.Atan2(p.z - lakeC.z, p.x - lakeC.x));
        bool OnLake(Vector3 p, float margin = 0f) =>
            hasLake && Vector2.Distance(new Vector2(p.x, p.z), new Vector2(lakeC.x, lakeC.z)) < LakeEdgeToward(p) + margin;

        public override void ShapeGround(IslandWorldData data, int index)
        {
            hasLake = false;
            var isl = data.layout.islands[index];
            IslandWorldData.Artist hall = null;
            foreach (var a in data.layout.artists ?? new IslandWorldData.Artist[0])
                if (a.sceneId == isl.sceneId && a.tier == "hall" && (hall == null || a.songs > hall.songs)) hall = a;
            if (hall == null) return;
            lakeHall = hall.name;
            lakeP1 = 1.7f; lakePhase2 = 4.1f;
            var plaza = new Vector3(isl.labelX, 0, isl.labelZ);
            bool Usable(float x, float z)
            {
                var c = Mathf.RoundToInt(x / data.Step); var r = Mathf.RoundToInt((data.layout.depth - z) / data.Step);
                if (c < 0 || r < 0 || c >= data.Nx || r >= data.Nz || data.islandIds[r * data.Nx + c] != index) return false;
                if (data.HeightAt(x, z) < 1.4f) return false;
                if (Vector2.Distance(new Vector2(x, z), new Vector2(plaza.x, plaza.z)) < LandContext.PlazaRadius + 20f) return false;
                foreach (var a in data.layout.artists)
                    if (a.sceneId == isl.sceneId && a.name != hall.name && (new Vector2(x - a.x, z - a.z)).magnitude < a.r + 4f) return false;
                return true;
            }
            // try the hall's surroundings, keep the spot whose whole lake (and banks) is free land;
            // if a big lake doesn't fit anywhere, try a smaller one
            var best = -1f; var hallC = new Vector3(hall.x, 0, hall.z);
            foreach (var size in new[] { 40f, 34f, 28f, 23f })
            {
            lakeR = size; best = -1f;
            for (var k = 0; k < 24; k++)
            foreach (var dist in new[] { hall.r + lakeR * .35f, hall.r + lakeR * .6f, hall.r + lakeR * .9f })
            {
                var ang = k * Mathf.PI * 2f / 24f;
                var c = hallC + new Vector3(Mathf.Cos(ang), 0, Mathf.Sin(ang)) * dist;
                int ok = 0, all = 0;
                for (var dx = -lakeR - 8; dx <= lakeR + 8; dx += 6)
                for (var dz = -lakeR - 8; dz <= lakeR + 8; dz += 6)
                {
                    if (dx * dx + dz * dz > (lakeR + 8) * (lakeR + 8)) continue;
                    all++; if (Usable(c.x + dx, c.z + dz)) ok++;
                }
                var score = ok / (float)all;
                if (score > best) { best = score; lakeC = c; }
            }
            if (best >= .85f) break;
            }
            if (best < .85f) { Debug.Log($"Lanternwood: no room for the lake ({best:P0} free)"); return; }
            hasLake = true;
            // the ice sits at the lower side of the shore
            var rim = new List<float>();
            for (var k = 0; k < 48; k++)
            {
                var a = k * Mathf.PI * 2f / 48f; var e = LakeEdge(a);
                rim.Add(data.HeightAt(lakeC.x + Mathf.Cos(a) * e, lakeC.z + Mathf.Sin(a) * e));
            }
            rim.Sort();
            lakeLevel = Mathf.Max(1.8f, rim[rim.Count / 4]);
            // dig the basin and ease the banks down to the ice
            float step = data.Step, reach = lakeR * 1.3f + 14f;
            int c0 = Mathf.FloorToInt((lakeC.x - reach) / step), c1 = Mathf.CeilToInt((lakeC.x + reach) / step);
            int r0 = Mathf.FloorToInt((data.layout.depth - lakeC.z - reach) / step), r1 = Mathf.CeilToInt((data.layout.depth - lakeC.z + reach) / step);
            for (var r = Mathf.Max(0, r0); r <= Mathf.Min(data.Nz - 1, r1); r++)
            for (var c = Mathf.Max(0, c0); c <= Mathf.Min(data.Nx - 1, c1); c++)
            {
                var i = r * data.Nx + c;
                if (data.islandIds[i] != index) continue;
                float x = c * step, z = data.layout.depth - r * step;
                var d = Vector2.Distance(new Vector2(x, z), new Vector2(lakeC.x, lakeC.z));
                var edge = LakeEdge(Mathf.Atan2(z - lakeC.z, x - lakeC.x));
                if (d < edge) data.heights[i] = lakeLevel - .9f;
                else if (d < edge + 14f)
                {
                    var t = Mathf.SmoothStep(0f, 1f, (d - edge) / 14f);
                    data.heights[i] = Mathf.Lerp(lakeLevel + .15f, data.heights[i], t);
                }
            }
            Debug.Log($"Lanternwood: frozen lake by {lakeHall}'s plot, ice at {lakeLevel:0.0} m");
        }

        /// The ice itself (solid, so you can walk out onto it), with a few drifts of snow on it.
        void Lake(LandContext ctx, MeshKit mk)
        {
            if (!hasLake) return;
            var ice = new MeshKit();
            var center = new Vector3(lakeC.x, lakeLevel + .03f, lakeC.z);
            const int n = 72;
            for (var k = 0; k < n; k++)
            {
                float a0 = k * Mathf.PI * 2f / n, a1 = (k + 1) * Mathf.PI * 2f / n;
                var p0 = center + new Vector3(Mathf.Cos(a0), 0, Mathf.Sin(a0)) * (LakeEdge(a0) + 1.5f);
                var p1 = center + new Vector3(Mathf.Cos(a1), 0, Mathf.Sin(a1)) * (LakeEdge(a1) + 1.5f);
                ice.Tri(this.ice, center, p0, p1, center - Vector3.up);
            }
            ice.Build("Frozen lake", ctx.Root, true);
            for (var k = 0; k < 9; k++)
            {
                var a = ctx.Range(0, Mathf.PI * 2f); var d = LakeEdge(a) * ctx.Range(.25f, .85f);
                mk.Blob(snowTop, center + new Vector3(Mathf.Cos(a) * d, .02f, Mathf.Sin(a) * d), new Vector3(ctx.Range(1.5f, 4f), .12f, ctx.Range(1f, 2.5f)), ctx.Rng, .1f);
            }
            ctx.AddKeepout(lakeC.x, lakeC.z, lakeR * 1.3f);
            clear.Add(new Vector3(lakeC.x, lakeR * 1.35f, lakeC.z));
        }

        // ---------------------------------------------------------------- the boathouse and pier
        void Boathouse(LandContext ctx, MeshKit mk, MeshKit solid, MeshKit pictures, Transform labels, IslandWorldData.Artist a, List<Renderer> glows, GlowHalos halosOut)
        {
            var hallC = new Vector3(a.x, 0, a.z);
            var toHall = (hallC - new Vector3(lakeC.x, 0, lakeC.z)).normalized;
            var shoreR = LakeEdge(Mathf.Atan2(toHall.z, toHall.x));
            var shore = new Vector3(lakeC.x, 0, lakeC.z) + toHall * shoreR;
            const float w = 10f, d = 12f, wallH = 5f, t = .3f;
            var faceLake = -toHall;
            var rot = Quaternion.LookRotation(faceLake);
            var c = shore + toHall * (d / 2f + 2f);
            var (lo, hi) = Footprint(ctx, c, rot, w + .6f, d + .6f);
            var floor = Mathf.Max(hi, lakeLevel + .3f) + .2f;
            var weathered = WorldMaterials.Solid(new Color(.52f, .55f, .6f));
            var slate = WorldMaterials.Solid(new Color(.26f, .28f, .33f));
            Vector3 P(float x, float y, float z) => new Vector3(c.x, floor, c.z) + rot * new Vector3(x, y, z);
            void Wall(Vector3 at, Vector3 size) { mk.Box(weathered, at, size, rot); solid.Box(weathered, at, size, rot); }

            Floor(mk, solid, darkWood, stone, P(0, 0, 0), rot, w, d, floor, Mathf.Min(lo, lakeLevel));
            Ramp(ctx, mk, solid, darkWood, P(0, 0, -d / 2 - .2f), -(rot * Vector3.forward), floor, HumanScale.DoorWidth + .6f);
            Ramp(ctx, mk, solid, darkWood, P(0, 0, d / 2 + .2f), rot * Vector3.forward, floor, 4.2f);
            IndoorSpaces.Add(P(0, 3.5f, 0), rot, new Vector3(w, 7f, d));
            // lake side: a wide opening; land side: a door in the gable wall
            const float lakeDoor = 4.2f, lakeDoorH = 3.8f, door = HumanScale.DoorWidth, doorH = HumanScale.DoorHeight;
            foreach (var s in new[] { -1f, 1f })
            {
                Wall(P(s * w / 2, wallH / 2, 0), new Vector3(t, wallH, d));
                Wall(P(s * (lakeDoor / 2 + (w - lakeDoor) / 4), wallH / 2, d / 2), new Vector3((w - lakeDoor) / 2, wallH, t));
                Wall(P(s * (door / 2 + (w - door) / 4), wallH / 2, -d / 2), new Vector3((w - door) / 2, wallH, t));
                var wp = P(s * (w / 2 + .17f), 2.6f, -2f);
                mk.Box(WorldMaterials.Glow(LanternGlass, 1.2f), wp, new Vector3(.05f, 1.4f, 1.1f), rot);
                halosOut.Add(wp + rot * Vector3.right * s * .3f, 1.4f, LanternGlass, .5f);
            }
            Wall(P(0, (wallH + lakeDoorH) / 2, d / 2), new Vector3(lakeDoor, wallH - lakeDoorH, t));
            Wall(P(0, (wallH + doorH) / 2, -d / 2), new Vector3(door, wallH - doorH, t));
            mk.GableRoof(slate, P(0, wallH, 0), new Vector3(w + 1.4f, 0, d + 1.6f), 3.6f, rot, snowTop);
            foreach (var z in new[] { -d / 2, d / 2 })
                mk.Tri(weathered, P(-w / 2, wallH, z), P(w / 2, wallH, z), P(0, wallH + 3.6f, z), P(0, wallH, 0));
            // a round moon window high in the land-side gable
            var moon = WorldMaterials.Glow(new Color(.86f, .9f, 1f), 1.6f);
            var mc = P(0, wallH + 1.3f, -d / 2 - .18f);
            for (var k = 0; k < 16; k++)
            {
                float a0 = k * Mathf.PI * 2f / 16, a1 = (k + 1) * Mathf.PI * 2f / 16;
                mk.Tri(moon, mc, mc + rot * new Vector3(Mathf.Cos(a0), Mathf.Sin(a0), 0) * .9f, mc + rot * new Vector3(Mathf.Cos(a1), Mathf.Sin(a1), 0) * .9f, mc + rot * Vector3.forward);
            }
            halosOut.Add(mc - rot * Vector3.forward * .3f, 2.4f, new Color(.8f, .86f, 1f), .5f);
            // inside: the covers along one wall, a warm lantern
            ArtistBits.CoverWall(pictures, labels, ctx, a, P(-w / 2 + .2f, 2.3f, -.5f), rot * Vector3.right, 4, .66f, 15, true, Ink);
            ArtistBits.PointLight(labels.parent, P(0, 3.2f, 0), new Color(1f, .7f, .42f), 10f, 1.5f, true);
            // the pier: out over the ice toward the middle of the lake, a bench at the end
            var pierLen = Mathf.Min(32f, shoreR * .75f);
            // keep the whole pier inside the artist's area (and their radio station)
            while (pierLen > 10f && Vector2.Distance(new Vector2(shore.x + faceLake.x * (pierLen + 2f), shore.z + faceLake.z * (pierLen + 2f)), new Vector2(a.x, a.z)) > a.r - 2f)
                pierLen -= 1f;
            var deckY = lakeLevel + .75f;
            var start = shore - toHall * 0f; start.y = deckY;
            for (var s = 0f; s < pierLen; s += 3f)
            {
                var segC = start + faceLake * (s + 1.5f);
                mk.Box(darkWood, segC, new Vector3(2.8f, .18f, 3f), rot);
                solid.Box(darkWood, segC, new Vector3(2.8f, .3f, 3f), rot);
                foreach (var side in new[] { -1f, 1f })
                    mk.Prism(logs, segC + rot * Vector3.right * side * 1.25f - Vector3.up * 1.6f, Vector3.up, .12f, 1.6f, 5);
            }
            // a ramp from the bank up onto the pier
            var bank = shore + toHall * 3f; bank.y = ctx.Ground(bank.x, bank.z);
            var rampC = (bank + start) / 2f;
            var rampRot = Quaternion.LookRotation(start - bank);
            mk.Box(darkWood, rampC, new Vector3(2.8f, .18f, Vector3.Distance(bank, start) + .4f), rampRot);
            solid.Box(darkWood, rampC, new Vector3(2.8f, .3f, Vector3.Distance(bank, start) + .4f), rampRot);
            for (var s = 8f; s < pierLen - 2f; s += 9f)
            {
                var side = ((int)(s / 9f) % 2 == 0) ? 1f : -1f;
                var post = start + faceLake * s + rot * Vector3.right * side * 1.3f;
                glows.Add(ArtistBits.LanternPost(mk, labels.parent, post, -(rot * Vector3.right) * side, darkWood, iron, LanternGlass, 2.2f));
                ArtistBits.PointLight(labels.parent, post + Vector3.up * 1.6f, new Color(1f, .7f, .42f), 8f, 1.1f);
            }
            var end = start + faceLake * (pierLen - 1.2f);
            mk.Box(darkWood, end + faceLake * .6f, new Vector3(4f, .18f, 3f), rot);   // a wider end to sit on
            solid.Box(darkWood, end + faceLake * .6f, new Vector3(4f, .3f, 3f), rot);
            Bench(ctx, mk, solid, end, faceLake, end.y + .09f);
            // the plaque by the land-side door, and a path to it from the plot
            ArtistBits.Plaque(mk, solid, labels, a, Ground(ctx, P(2.6f, 0, -d / 2 - 2.6f)), -faceLake, darkWood, board, Ink, 1.7f);
            var trodden = WorldMaterials.Solid(new Color(.72f, .74f, .8f));
            var pts = new List<Vector3>();
            var from = Ground(ctx, hallC); var to = P(0, 0, -d / 2 - 1f);
            for (var f = 0f; f <= 1f; f += .08f) { var q = Vector3.Lerp(from, to, f); pts.Add(ctx.OnGround(q.x, q.z)); }
            Ribbon(ctx, mk, trodden, pts, 2.2f, .09f);
        }

        // ---------------------------------------------------------------- the treehouse and fire pit
        void Treehouse(LandContext ctx, MeshKit mk, MeshKit solid, MeshKit pictures, Transform labels, IslandWorldData.Artist a, Vector3 center, Vector3 front,
                       List<Renderer> glows, GlowHalos halosOut)
        {
            var rot = Quaternion.LookRotation(front);
            var right = rot * Vector3.right;
            const float plat = 12f, H = 5.5f, house = 8.5f, wallH = 4.4f;
            var baseY = ctx.Ground(center.x, center.z);
            var deckY = baseY + H;
            Vector3 P(float x, float y, float z) => new Vector3(center.x, deckY, center.z) + rot * new Vector3(x, y, z);
            var warm = WorldMaterials.Solid(new Color(.64f, .46f, .32f));
            void Both(Material m, Vector3 at, Vector3 size, Quaternion r) { mk.Box(m, at, size, r); solid.Box(m, at, size, r); }

            // four big birches carry the platform and grow up through it
            foreach (var (sx, sz) in new[] { (-1f, -1f), (1f, -1f), (-1f, 1f), (1f, 1f) })
            {
                var foot = P(sx * (plat / 2 - .4f), 0, sz * (plat / 2 - .4f)); foot.y = ctx.Ground(foot.x, foot.z) - .3f;
                var height = deckY - foot.y + 8f;
                mk.Prism(bark, foot, Vector3.up, .55f, height, 6, .6f);
                solid.Prism(bark, foot, Vector3.up, .55f, height, 6);
                for (var m = 0; m < 5; m++)
                {
                    var y = ctx.Range(.8f, height * .8f); var spin = Quaternion.Euler(0, ctx.Range(0, 360), 0);
                    mk.Box(barkMark, foot + Vector3.up * y + spin * Vector3.forward * .5f, new Vector3(.6f, .1f, .08f), spin);
                }
                var top = foot + Vector3.up * height;
                for (var m = 0; m < 3; m++)
                    mk.Blob(m == 0 ? leafGold : leafPale, top - Vector3.up * ctx.Range(1f, 3f) + new Vector3(ctx.Range(-1.5f, 1.5f), 0, ctx.Range(-1.5f, 1.5f)), new Vector3(2.6f, 2.4f, 2.6f), ctx.Rng);
                mk.Blob(snowTop, top, new Vector3(1.6f, .55f, 1.6f), ctx.Rng);
            }
            Both(darkWood, P(0, -.15f, 0), new Vector3(plat, .3f, plat), rot);                      // the deck
            // railings round the deck, open at the front for the ramp
            var bulb = WorldMaterials.Glow(new Color(1f, .78f, .45f), 2.2f);
            foreach (var (edge, alongX) in new[] { (-1f, true), (1f, true), (-1f, false), (1f, false) })
            {
                var isFront = alongX && edge > 0;
                for (var u = -plat / 2; u < plat / 2 - .1f; u += 1.5f)
                {
                    if (isFront && Mathf.Abs(u + .75f) < 1.6f) continue;              // the gap for the ramp
                    var mid = alongX ? P(u + .75f, .55f, edge * plat / 2) : P(edge * plat / 2, .55f, u + .75f);
                    var size = alongX ? new Vector3(1.5f, .08f, .08f) : new Vector3(.08f, .08f, 1.5f);
                    Both(darkWood, mid + Vector3.up * .5f, size, rot);
                    mk.Box(darkWood, mid, new Vector3(.1f, 1.1f, .1f), rot);
                    // string lights along the top rail
                    var lightAt = mid + Vector3.up * .62f;
                    mk.Blob(bulb, lightAt, Vector3.one * .07f, ctx.Rng, 0f);
                    halosOut.Add(lightAt, .6f, new Color(1f, .78f, .45f), .45f);
                }
            }
            // the ramp up from the ground at the front
            var top0 = P(0, 0, plat / 2);
            var foot0 = top0 + front * (H / Mathf.Tan(26f * Mathf.Deg2Rad));
            foot0.y = ctx.Ground(foot0.x, foot0.z);
            var rampRot = Quaternion.LookRotation(top0 - foot0);
            var rampLen = Vector3.Distance(top0, foot0) + .6f;
            Both(darkWood, (top0 + foot0) / 2f, new Vector3(2.6f, .2f, rampLen), rampRot);
            foreach (var s in new[] { -1f, 1f })
                Both(darkWood, (top0 + foot0) / 2f + right * s * 1.3f + rampRot * Vector3.up * .9f, new Vector3(.08f, .08f, rampLen), rampRot);
            // the house on the back half of the deck
            Vector3 Hp(float x, float y, float z) => P(x, y, z - plat / 2 + house / 2 + .5f);
            const float door = HumanScale.DoorWidth, doorH = HumanScale.DoorHeight;
            Both(warm, Hp(0, wallH / 2, -house / 2), new Vector3(house, wallH, .3f), rot);
            foreach (var s in new[] { -1f, 1f })
            {
                Both(warm, Hp(s * house / 2, wallH / 2, 0), new Vector3(.3f, wallH, house), rot);
                Both(warm, Hp(s * (door / 2 + (house - door) / 4), wallH / 2, house / 2), new Vector3((house - door) / 2, wallH, .3f), rot);
                var wp = Hp(s * (house / 2 + .17f), 2.3f, 0);
                var win = ArtistBits.GlassBox(labels.parent, wp, new Vector3(.06f, 1.2f, 1.6f), new Color(1f, .7f, .38f));
                win.transform.rotation = rot;
                glows.Add(win);
            }
            Both(warm, Hp(0, (wallH + doorH) / 2, house / 2), new Vector3(door, wallH - doorH, .3f), rot);
            IndoorSpaces.Add(Hp(0, 3f, 0), rot, new Vector3(house, 6f, house));
            mk.GableRoof(darkWood, Hp(0, wallH, 0), new Vector3(house + 1.4f, 0, house + 1.4f), 3f, rot * Quaternion.Euler(0, 90, 0), snowTop);
            foreach (var x in new[] { -house / 2, house / 2 })
                mk.Tri(warm, Hp(x, wallH, -house / 2), Hp(x, wallH, house / 2), Hp(x, wallH + 3f, 0), Hp(0, wallH, 0));
            ArtistBits.CoverWall(pictures, labels, ctx, a, Hp(0, 2.2f, -house / 2 + .2f), front, 5, .6f, 15, true, Ink);
            ArtistBits.PointLight(labels.parent, Hp(0, 3f, 0), new Color(1f, .72f, .45f), 9f, 1.6f, true);
            // the fire pit beside the tree, with log seats round it
            var fire = Ground(ctx, center + right * (plat / 2 + 7f) + front * 2f);
            FirePit(ctx, mk, solid, labels.parent, fire, 5, halosOut);
            // the plaque at the foot of the ramp
            ArtistBits.Plaque(mk, solid, labels, a, Ground(ctx, foot0 + front * 1.5f + right * 2.4f), front, darkWood, board, Ink, 1.7f);
        }

        /// A stone ring with a fire in it and log seats round it. Lights up the snow and flickers.
        void FirePit(LandContext ctx, MeshKit mk, MeshKit solid, Transform parent, Vector3 at, int seats, GlowHalos halosOut, float seatRadius = 3.6f)
        {
            for (var k = 0; k < 11; k++)
            {
                var a = k * Mathf.PI * 2f / 11;
                mk.Blob(stone, at + new Vector3(Mathf.Cos(a), .15f, Mathf.Sin(a)) * 1.25f, new Vector3(.35f, .25f, .35f), ctx.Rng);
            }
            for (var k = 0; k < 4; k++)
            {
                var a = k * Mathf.PI / 2 + .3f;
                mk.Prism(logs, at + new Vector3(Mathf.Cos(a), .1f, Mathf.Sin(a)) * .8f, new Vector3(-Mathf.Cos(a), .5f, -Mathf.Sin(a)), .12f, 1.1f, 5);
            }
            var flameA = WorldMaterials.Glow(new Color(1f, .55f, .2f), 4f);
            var flameB = WorldMaterials.Glow(new Color(1f, .8f, .35f), 5f);
            mk.Cone(flameA, at + Vector3.up * .2f, .55f, 1.3f, 5);
            mk.Cone(flameB, at + Vector3.up * .25f + new Vector3(.15f, 0, .1f), .3f, .9f, 5, 30f);
            halosOut.Add(at + Vector3.up * .9f, 3.6f, new Color(1f, .6f, .28f), 1.1f);
            var l = ArtistBits.PointLight(parent, at + Vector3.up * 1.4f, new Color(1f, .62f, .32f), 13f, 2.6f);
            l.gameObject.AddComponent<FireFlicker>();
            for (var k = 0; k < seats; k++)
            {
                var a = k * Mathf.PI * 2f / seats + .4f;
                var dir = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                var seat = at + dir * seatRadius; seat.y = ctx.Ground(seat.x, seat.z) + .25f;
                var along = Vector3.Cross(Vector3.up, dir);
                mk.Prism(logs, seat - along * 1f, along, .3f, 2f, 7);
                solid.Box(logs, seat, new Vector3(2f, .6f, .6f), Quaternion.LookRotation(dir));
            }
        }

        // ---------------------------------------------------------------- snowmen, the fire circle, the overlook
        /// Snowmen beside the paths. They stand somewhere new each day (the same for everyone that day),
        /// using their own random numbers so the rest of the land never changes.
        void Snowmen(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            if (pathPoints.Count == 0) return;
            var rng = new System.Random((int)SharedChance.Day * 977 + ctx.Island.sceneId);
            float R(float a, float b) => a + (float)rng.NextDouble() * (b - a);
            var scarves = new[] { new Color(.75f, .15f, .15f), new Color(.15f, .45f, .25f), new Color(.2f, .3f, .65f), new Color(.85f, .6f, .15f) };
            var coal = WorldMaterials.Solid(new Color(.08f, .08f, .09f));
            var carrot = WorldMaterials.Solid(new Color(.95f, .45f, .1f));
            var placed = 0;
            for (var tries = 0; tries < 200 && placed < 7; tries++)
            {
                var p = pathPoints[rng.Next(pathPoints.Count)];
                var side = new Vector3(R(-1, 1), 0, R(-1, 1)).normalized * R(4f, 7f);
                var at = p + side;
                if (!ctx.IsLand(at.x, at.z, 3f) || !ctx.IsFree(at.x, at.z, 2f) || OnLake(at, 2f)) continue;
                at.y = ctx.Ground(at.x, at.z);
                var facing = Quaternion.LookRotation(new Vector3(p.x - at.x, 0, p.z - at.z));
                var big = R(.85f, 1.1f);
                mk.Blob(snowTop, at + Vector3.up * .55f * big, new Vector3(.8f, .7f, .8f) * big, rng, .08f);
                mk.Blob(snowTop, at + Vector3.up * 1.45f * big, new Vector3(.55f, .5f, .55f) * big, rng, .08f);
                var head = at + Vector3.up * 2.12f * big;
                mk.Blob(snowTop, head, new Vector3(.38f, .38f, .38f) * big, rng, .06f);
                foreach (var s in new[] { -1f, 1f })
                    mk.Box(coal, head + facing * new Vector3(s * .13f, .08f, .33f) * big, Vector3.one * .07f, facing);
                mk.Prism(carrot, head + facing * new Vector3(0, -.02f, .32f) * big, facing * Vector3.forward, .06f, .35f, 5, .1f);
                foreach (var s in new[] { -1f, 1f })
                    mk.Prism(darkWood, at + Vector3.up * 1.55f * big + facing * Vector3.right * s * .45f, facing * new Vector3(s, .6f, 0), .03f, .9f, 4, .5f);
                mk.Box(WorldMaterials.Solid(scarves[rng.Next(scarves.Length)]), head - Vector3.up * .36f * big, new Vector3(.6f, .14f, .6f) * big, facing);
                solid.Box(snowTop, at + Vector3.up * 1f, new Vector3(1.4f, 2f, 1.4f), facing);
                clear.Add(new Vector3(at.x, 2.5f, at.z));
                placed++;
            }
        }

        /// A fire circle beside a path, a fair walk from the chapel: somewhere to gather.
        void FireCircle(LandContext ctx, MeshKit mk, MeshKit solid, Vector3 chapelFront)
        {
            foreach (var p in pathPoints)
            {
                var d = Vector2.Distance(new Vector2(p.x, p.z), new Vector2(chapelFront.x, chapelFront.z));
                if (d < 70f || d > 160f) continue;
                foreach (var dir in new[] { Vector3.right, Vector3.left, Vector3.forward, Vector3.back })
                {
                    var at = p + dir * 10f;
                    if (!ctx.IsLand(at.x, at.z, 8f) || !ctx.IsFree(at.x, at.z, 7f) || OnLake(at, 8f)) continue;
                    at.y = ctx.Ground(at.x, at.z);
                    FirePit(ctx, mk, solid, ctx.Root, at, 6, halos, 4.2f);
                    clear.Add(new Vector3(at.x, 9f, at.z));
                    ctx.AddKeepout(at.x, at.z, 7f);
                    return;
                }
            }
        }

        /// A bench on high ground near the shore, looking out to sea, with one lantern.
        void Overlook(LandContext ctx, MeshKit mk, MeshKit solid)
        {
            Vector3 best = default; var bestH = float.MinValue;
            for (var i = 0; i < 600; i++)
            {
                float x = ctx.Range(ctx.MinX, ctx.MaxX), z = ctx.Range(ctx.MinZ, ctx.MaxZ);
                if (!ctx.IsLand(x, z, 5f) || ctx.IsLand(x, z, 35f) || !ctx.IsFree(x, z, 4f) || OnLake(new Vector3(x, 0, z), 6f)) continue;
                var h = ctx.Ground(x, z);
                if (h > bestH) { bestH = h; best = new Vector3(x, h, z); }
            }
            if (bestH == float.MinValue) return;
            var outward = new Vector3(best.x - ctx.Plaza.x, 0, best.z - ctx.Plaza.z).normalized;
            Bench(ctx, mk, solid, best, outward);
            var side = Vector3.Cross(Vector3.up, outward);
            var post = Ground(ctx, best + side * 1.6f - outward * .4f);
            ArtistBits.LanternPost(mk, ctx.Root, post, -side, darkWood, iron, LanternGlass);
            halos.Add(post + Vector3.up * 2.15f - side * .65f, 1.3f, LanternGlass);
            ArtistBits.PointLight(ctx.Root, post + Vector3.up * 2f, new Color(1f, .7f, .42f), 8f, 1.1f);
            clear.Add(new Vector3(best.x, 6f, best.z));
            ctx.AddKeepout(best.x, best.z, 4f);
        }
    }
}
